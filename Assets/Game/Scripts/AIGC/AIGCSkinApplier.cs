using UnityEngine;

namespace ActionArena.AIGC
{
    /// <summary>
    /// 把生成的贴图套到目标 Renderer（玩家身体）上。
    /// 用 <c>renderer.material</c>（实例），不污染原始材质资源 / 不影响复用同材质的敌人。
    /// </summary>
    public class AIGCSkinApplier : MonoBehaviour
    {
        [Tooltip("要换皮的 Renderer（留空 = 自动取本物体下所有 Renderer）")]
        [SerializeField] private Renderer[] _renderers;

        private Renderer[] Targets =>
            (_renderers != null && _renderers.Length > 0)
                ? _renderers
                : (_renderers = GetComponentsInChildren<Renderer>(true));

        /// <summary>把一张 Texture2D 套到所有目标 Renderer 的主贴图（_MainTex）。</summary>
        public void Apply(Texture2D tex)
        {
            if (tex == null) return;
            tex.filterMode = FilterMode.Bilinear;

            int n = 0;
            foreach (var r in Targets)
            {
                if (r == null) continue;
                // 用 r.materials（复数）覆盖【所有】子材质；r.material（单数）只换第 0 个，
                // 多材质模型（身体+衣服+腰带）的其余槽会保留旧贴图/旧颜色 → 残留原来的红。
                foreach (var mat in r.materials)
                {
                    mat.mainTexture = tex; // 等价 SetTexture("_MainTex"/"_BaseMap", tex)；r.materials 取的是实例，不污染资源

                    // ⚠️ mainTexture 只换「贴图」，不碰材质的纯色 tint。
                    // Standard 的 _Color / URP 的 _BaseColor 会和贴图【相乘】：
                    // 若原 tint 是红色，换新贴图后整张仍偏红（红色「透」出来）。
                    // 清成白色 = 不再相乘，贴图原色直接显示。
                    if (mat.HasProperty("_Color"))     mat.SetColor("_Color", Color.white);     // Built-in Standard
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white); // URP Lit
                    n++;
                }
            }
            Debug.Log($"[AIGCSkin] 已套用生成贴图到 {n} 个材质。");
        }
    }
}
