using UnityEngine;
using UnityEngine.UI;

namespace ActionArena.UI
{
    /// <summary>
    /// 飘字单项：被对象池取出激活后自动上浮 + 渐隐。
    /// 挂在飘字 prefab 根节点（其子节点放 Text 组件）。
    /// </summary>
    public class FloatingText : MonoBehaviour
    {
        [SerializeField] private float _riseSpeed = 1.4f;
        [SerializeField] private float _fadeDuration = 0.7f;
        [SerializeField] private float _horizontalJitter = 0.3f;

        private Text _text;
        private Color _baseColor;
        private float _t;
        private float _xDrift;

        private void Awake()
        {
            _text = GetComponentInChildren<Text>();
            if (_text != null) _baseColor = _text.color;
        }

        private void OnEnable()
        {
            _t = 0f;
            _xDrift = (Random.value - 0.5f) * 2f * _horizontalJitter;
            if (_text != null) _text.color = _baseColor;
        }

        private void Update()
        {
            transform.position += (Vector3.up * _riseSpeed + Vector3.right * _xDrift) * Time.deltaTime;

            _t += Time.deltaTime;
            if (_text != null)
            {
                float a = Mathf.Clamp01(1f - _t / _fadeDuration);
                _text.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, a);
            }
        }
    }
}
