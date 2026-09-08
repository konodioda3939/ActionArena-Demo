#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor.SceneManagement;
using TMPro;

namespace ActionArena.EditorTools
{
    /// <summary>
    /// 一键把 Canvas 下的 HUD 元素排到屏幕四角，并设置 CanvasScaler。
    /// 菜单：Tools ▸ ActionArena ▸ Layout HUD
    /// 非破坏性：只改锚点 / 位置 / 尺寸 / 字号 / 对齐 / 颜色，不动任何脚本引用，可 Ctrl+Z 撤销。
    /// 元素按名字找（ScoreText / WaveText / EnemiesText / HealthText / HealthBarBG / HealthFill）。
    /// </summary>
    public static class HUDLayoutTool
    {
        [MenuItem("Tools/ActionArena/Layout HUD")]
        public static void LayoutHUD()
        {
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[HUDLayout] 场景里没有 Canvas。先建一个 Canvas 再运行。");
                return;
            }

            Undo.RegisterCompleteObjectUndo(canvas.gameObject, "Layout HUD");

            // —— 1. CanvasScaler：1920x1080，随屏幕缩放 ——
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                Undo.RecordObject(scaler, "Layout HUD");
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
            }

            // —— 2. 四个文本 + 血条背景 + 血条填充 ——
            LayoutText(canvas, "ScoreText",   minMax(0, 1),   new Vector2(0, 1),  new Vector2(40, -40),  new Vector2(500, 90), 56, TextAlignmentOptions.TopLeft,     "分数 0");
            LayoutText(canvas, "WaveText",    minMax(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(500, 90), 56, TextAlignmentOptions.Top,         "第 1 波");
            LayoutText(canvas, "EnemiesText", minMax(1, 1),   new Vector2(1, 1),  new Vector2(-40, -40), new Vector2(500, 90), 56, TextAlignmentOptions.TopRight,    "剩余 0");
            LayoutText(canvas, "HealthText",  minMax(0, 0),   new Vector2(0, 0),  new Vector2(40, 95),   new Vector2(360, 55), 40, TextAlignmentOptions.BottomLeft,  "100 / 100");

            // 血条背景：左下，深色底
            Image bg = FindImage(canvas, "HealthBarBG");          // Canvas 的直接子物体
            if (bg != null)
            {
                Undo.RecordObject(bg, "Layout HUD");
                var rt = bg.rectTransform;
                rt.anchorMin = new Vector2(0, 0);
                rt.anchorMax = new Vector2(0, 0);
                rt.pivot = new Vector2(0, 0);
                rt.anchoredPosition = new Vector2(40, 40);
                rt.sizeDelta = new Vector2(400, 40);
                if (bg.sprite == null)
                    bg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                bg.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);
                bg.type = Image.Type.Simple;
            }

            // 血条填充：铺满背景，Filled/Horizontal/Left 不变
            Image fill = FindImage(canvas, "HealthBarBG/HealthFill"); // 嵌在背景下的子物体
            if (fill != null)
            {
                Undo.RecordObject(fill, "Layout HUD");
                var rt = fill.rectTransform;
                rt.anchorMin = new Vector2(0, 0);
                rt.anchorMax = new Vector2(1, 1); // 拉伸铺满背景
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = Vector2.zero;      // 大小 = 背景大小
                if (fill.sprite == null)
                    fill.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Horizontal;
                fill.fillOrigin = 0; // Left
                fill.fillAmount = 1f;
                fill.color = new Color(0.24f, 0.96f, 0.24f, 1f);
            }

            EditorUtility.SetDirty(canvas.gameObject);
            Debug.Log("[HUDLayout] 完成：分数(左上) 波次(中上) 敌人(右上) 血条+血量(左下)。CanvasScaler=1920x1080 随屏缩放。");
        }

        /// <summary>
        /// 用系统黑体(SimHei)生成一个【动态】TMP 字体资源，赋给 Canvas 下所有 TMP 文本，
        /// 并设为项目默认字体。动态模式 = 用到任何中文都会自动补字形，无需预生成字符表。
        /// 菜单：Tools ▸ ActionArena ▸ Setup Chinese Font
        /// </summary>
        [MenuItem("Tools/ActionArena/Setup Chinese Font (SimHei)")]
        public static void SetupChineseFont()
        {
            const string srcFont = @"C:\Windows\Fonts\simhei.ttf";
            const string fontsDir = "Assets/Game/Fonts";
            const string ttfPath = fontsDir + "/SimHei.ttf";
            const string sdfPath = fontsDir + "/SimHei SDF.asset";

            if (!System.IO.File.Exists(srcFont))
            {
                Debug.LogError($"[HUDFont] 系统字体不存在：{srcFont}（换台机器可能路径不同）");
                return;
            }

            System.IO.Directory.CreateDirectory(fontsDir);

            // 1. 复制 TTF 进项目（已存在则跳过，避免覆盖正在使用的资源）
            if (!System.IO.File.Exists(ttfPath))
            {
                System.IO.File.Copy(srcFont, ttfPath);
                AssetDatabase.ImportAsset(ttfPath, ImportAssetOptions.ForceUpdate);
            }
            AssetDatabase.Refresh();
            Font unityFont = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
            if (unityFont == null)
            {
                Debug.LogError($"[HUDFont] TTF 导入后仍读不到 Font：{ttfPath}");
                return;
            }

            // 2. 生成动态 TMP 字体资源（已有则复用，避免重复生成）
            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(sdfPath);
            if (fontAsset == null)
            {
                fontAsset = TMP_FontAsset.CreateFontAsset(unityFont);
                fontAsset.name = "SimHei SDF";
                fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic; // 关键：动态补字形
                AssetDatabase.CreateAsset(fontAsset, sdfPath);
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
                foreach (var tex in fontAsset.atlasTextures)
                    AssetDatabase.AddObjectToAsset(tex, fontAsset);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            // 3. 赋给 Canvas 下所有 TMP 文本
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            int count = 0;
            if (canvas != null)
            {
                foreach (var t in canvas.GetComponentsInChildren<TMP_Text>(true))
                {
                    Undo.RecordObject(t, "Setup Chinese Font");
                    t.font = fontAsset;
                    EditorUtility.SetDirty(t);
                    count++;
                }
                EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            }

            // 4. 设为项目默认字体（以后新建的 TMP 文本自动用中文）
            if (TMP_Settings.instance != null)
            {
                var so = new SerializedObject(TMP_Settings.instance);
                so.FindProperty("m_defaultFontAsset").objectReferenceValue = fontAsset;
                so.FindProperty("m_defaultFontAssetPath").stringValue = sdfPath;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(TMP_Settings.instance);
            }

            Debug.Log($"[HUDFont] 完成：SimHei 动态字体已生成并赋给 {count} 个 TMP 文本，并设为项目默认字体。");
        }

        // ---- helpers ----

        private static void LayoutText(Canvas canvas, string name, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, float fontSize, TextAlignmentOptions align, string placeholder)
        {
            TMP_Text t = FindTMP(canvas, name);
            if (t == null)
            {
                Debug.LogWarning($"[HUDLayout] 没找到名为 \"{name}\" 的 TMP 文本，跳过。");
                return;
            }
            Undo.RecordObject(t, "Layout HUD");
            var rt = t.rectTransform;
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            t.enableAutoSizing = false;
            t.fontSize = fontSize;
            t.alignment = align;
            t.overflowMode = TextOverflowModes.Overflow;
            t.text = placeholder;
            EditorUtility.SetDirty(t);
        }

        private static TMP_Text FindTMP(Canvas canvas, string name)
            => canvas.transform.Find(name)?.GetComponent<TMP_Text>();

        private static Image FindImage(Canvas canvas, string path)
            => canvas.transform.Find(path)?.GetComponent<Image>();

        private static Vector2 minMax(float x, float y) => new Vector2(x, y);
    }
}
#endif
