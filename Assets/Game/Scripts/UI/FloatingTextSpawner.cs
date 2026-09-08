using UnityEngine;
using UnityEngine.UI;
using ActionArena.Core;
using ActionArena.Pooling;

namespace ActionArena.UI
{
    /// <summary>
    /// 飘字生成器：订阅 <see cref="HitLandedEvent"/>，在命中点上方生成伤害数字飘字。
    /// 通过 <see cref="ObjectPool"/> 复用飘字 prefab，命中密集时也不产生 GC。
    /// </summary>
    public class FloatingTextSpawner : MonoBehaviour
    {
        [SerializeField] private ObjectPool _pool;
        [SerializeField] private Vector3 _offset = new Vector3(0f, 1.6f, 0f);
        [SerializeField] private float _lifetime = 0.7f;
        [SerializeField] private Camera _worldCamera;

        private void OnEnable()  => EventBus.Subscribe<HitLandedEvent>(OnHit);
        private void OnDisable() => EventBus.Unsubscribe<HitLandedEvent>(OnHit);

        private void Start()
        {
            if (_worldCamera == null) _worldCamera = Camera.main;
        }

        private void OnHit(HitLandedEvent e)
        {
            if (_pool == null) return;

            Vector3 pos = e.HitPoint + _offset;
            GameObject obj = _pool.GetAndAutoRelease(pos, Quaternion.identity, _lifetime);

            var text = obj.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.text = Mathf.RoundToInt(e.Damage).ToString();
                text.color = e.HeavyHit ? new Color(1f, 0.35f, 0.35f) : Color.white;
            }

            // 飘字始终面向相机（世界空间 UI）
            if (_worldCamera != null)
                obj.transform.forward = _worldCamera.transform.forward;
        }
    }
}
