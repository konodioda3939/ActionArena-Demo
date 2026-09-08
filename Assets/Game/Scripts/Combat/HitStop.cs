using System.Collections;
using UnityEngine;
using ActionArena.Core;

namespace ActionArena.Combat
{
    /// <summary>
    /// 命中顿帧（Hit Stop）：命中瞬间短暂降低 timeScale，制造"打击凝固感"，动作游戏打击感的核心。
    /// 订阅 <see cref="HitLandedEvent"/> 自动触发；重击 → 更长顿帧。
    /// 用 <see cref="WaitForSecondsRealtime"/> 恢复，确保恢复计时不受被压低的 timeScale 影响。
    /// </summary>
    public class HitStop : MonoBehaviour
    {
        [SerializeField] private float _lightDuration = 0.06f;  // 普通命中顿帧时长
        [SerializeField] private float _heavyDuration = 0.12f;  // 重击 / 致命顿帧时长
        [SerializeField] private float _hitTimeScale = 0.08f;   // 顿帧期间的时间流速

        private Coroutine _co;

        private void OnEnable()  => EventBus.Subscribe<HitLandedEvent>(OnHit);
        private void OnDisable() => EventBus.Unsubscribe<HitLandedEvent>(OnHit);

        private void OnHit(HitLandedEvent e)
        {
            if (_co != null) StopCoroutine(_co);
            _co = StartCoroutine(DoHitStop(e.HeavyHit || e.Killed ? _heavyDuration : _lightDuration));
        }

        private IEnumerator DoHitStop(float duration)
        {
            Time.timeScale = _hitTimeScale;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = 1f;
            _co = null;
        }

        private void OnApplicationQuit()
        {
            // 防止编辑器停止 Play 时 timeScale 卡在低值
            Time.timeScale = 1f;
        }
    }
}
