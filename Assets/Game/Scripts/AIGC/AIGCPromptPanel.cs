using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ActionArena.AIGC
{
    /// <summary>
    /// 运行时 AIGC 面板：按 _toggleKey 开关，输入 prompt → 生成预览 → 套用到玩家。
    /// 面板打开时 TimeScale=0 暂停游戏，方便输入不被打断。
    ///
    /// 挂载约定：组件挂在【常驻 GO】上（如 "AIGCPanelHost"，始终 Active），
    /// 面板视觉挂在【子物体】上并拖到 _panel。开关的是子物体，组件本体保持 Active 才能收 Update。
    /// </summary>
    public class AIGCPromptPanel : MonoBehaviour
    {
        [Header("面板视觉（开关这个子物体）")]
        [Tooltip("留空 = 自动取第一个子物体")]
        [SerializeField] private GameObject _panel;

        [Header("UI 控件（全部可选，没拖的会跳过）")]
        [SerializeField] private TMP_InputField _input;
        [SerializeField] private Button _generateBtn;
        [SerializeField] private Button _applyBtn;
        [SerializeField] private RawImage _preview;
        [SerializeField] private TMP_Text _status;
        [Tooltip("模型选择下拉框（留空 = 一直用默认 anime）。Inspector 里把选项配成：二次元 / 写实风 / 纹理图案")]
        [SerializeField] private TMP_Dropdown _modelDropdown;

        [Header("逻辑")]
        [SerializeField] private AIGCSkinApplier _applier;
        [SerializeField] private string _baseUrl = RuntimeAIGCClient.DefaultBaseUrl;
        [Tooltip("开/关面板的按键（默认 G）")]
        [SerializeField] private KeyCode _toggleKey = KeyCode.G;
        [Tooltip("LCM 快速模式（~0.75s/张），运行时演示建议开")]
        [SerializeField] private bool _fastMode = true;
        [Tooltip("下拉框选项 → 模型 key 映射，顺序须与下拉选项一致（anime/realistic/texture）")]
        [SerializeField] private string[] _modelKeys = { "anime", "realistic", "texture" };

        [Header("调试")]
        [Tooltip("✅ 勾上 = 离线模式：点「生成」造一张随机色块图（不联网），用于验证 UI/暂停/换皮管线。验完关掉再连真实服务。")]
        [SerializeField] private bool _offlineMode = false;

        private Texture2D _last;
        private bool _busy;
        private bool _open;
        private string _modelKey = "anime"; // 当前选中的模型 key（随下拉框同步）

        private void Awake()
        {
            if (_panel == null && transform.childCount > 0)
                _panel = transform.GetChild(0).gameObject;
            SetOpen(false);
        }

        private void Start()
        {
            if (_generateBtn != null) _generateBtn.onClick.AddListener(OnGenerate);
            if (_applyBtn != null) _applyBtn.onClick.AddListener(OnApply);
            SetupModelDropdown();
        }

        private void SetupModelDropdown()
        {
            if (_modelDropdown == null) return; // 没配下拉框 = 一直用默认 anime
            // 下拉框初始值对齐到 _modelKey，变更时同步到 _modelKey
            int idx = System.Array.IndexOf(_modelKeys, _modelKey);
            if (idx >= 0) _modelDropdown.value = idx;
            _modelDropdown.onValueChanged.AddListener(i =>
            {
                if (i >= 0 && i < _modelKeys.Length) _modelKey = _modelKeys[i];
            });
        }

        private void Update()
        {
            // Escape 始终能关面板（即便正在输入框里打字）
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape) && _open)
            {
                SetOpen(false);
                return;
            }
            // 切换键：仅当输入框【未获焦】时才响应，避免打字（如 "green" 的 g）误触开关
            if (UnityEngine.Input.GetKeyDown(_toggleKey) && !InputFieldFocused)
                SetOpen(!_open);
        }

        private bool InputFieldFocused => _input != null && _input.isFocused;

        private void SetOpen(bool open)
        {
            _open = open;
            if (_panel != null) _panel.SetActive(open);
            Time.timeScale = open ? 0f : 1f; // 打开暂停，关闭恢复
            if (open)
                SetStatus(_fastMode
                    ? "输入英文描述，点「生成」（如：red ninja armor）"
                    : "输入英文描述，点「生成」（约 10-30 秒）");
        }

        public async void OnGenerate()
        {
            if (_busy) return;
            string prompt = _input != null ? _input.text : string.Empty;
            if (string.IsNullOrWhiteSpace(prompt)) { SetStatus("请先输入描述"); return; }

            _busy = true;
            SetStatus(_offlineMode ? "（离线）生成中…"
                : (_fastMode ? "快速生成中…（约 1 秒）" : "生成中…（约 10-30 秒）"));
            if (_generateBtn != null) _generateBtn.interactable = false;

            try
            {
                if (_offlineMode)
                {
                    await Task.Delay(300); // 模拟生成耗时
                    _last = MakeMockTexture(prompt.Trim());
                }
                else
                {
                    _last = await RuntimeAIGCClient.GenerateImage(
                        _baseUrl, prompt.Trim(), _fastMode, seed: null, model: _modelKey);
                }
                if (_preview != null) _preview.texture = _last;
                SetStatus(_offlineMode ? "（离线）生成完成！点「套用」换皮" : "生成完成！点「套用」换皮");
            }
            catch (Exception e)
            {
                _last = null;
                SetStatus("失败：" + e.Message);
            }
            finally
            {
                _busy = false;
                if (_generateBtn != null) _generateBtn.interactable = true;
            }
        }

        public void OnApply()
        {
            if (_last == null) { SetStatus("还没生成图，先点「生成」"); return; }
            if (_applier == null) { SetStatus("没指定 AIGCSkinApplier"); return; }
            _applier.Apply(_last);
            SetStatus("已套用到玩家！");
        }

        /// <summary>离线模式：根据 prompt 哈希造一张纯色 256×256 贴图（不同 prompt → 不同颜色），用于验证管线。</summary>
        private static Texture2D MakeMockTexture(string prompt)
        {
            int h = prompt == null ? 0 : prompt.GetHashCode();
            Color c = Color.HSVToRGB((Mathf.Abs(h) % 360) / 360f, 0.85f, 0.95f);
            var tex = new Texture2D(256, 256, TextureFormat.RGBA32, false);
            Color[] px = new Color[256 * 256];
            for (int i = 0; i < px.Length; i++) px[i] = c;
            tex.SetPixels(px);
            tex.Apply();
            tex.name = "Mock_" + (prompt ?? "");
            return tex;
        }

        private void SetStatus(string msg)
        {
            if (_status != null) _status.text = msg;
            Debug.Log("[AIGCPanel] " + msg);
        }
    }
}
