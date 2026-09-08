using System.Collections;
using UnityEngine;
using ActionArena.Core;

namespace ActionArena.Combat
{
    /// <summary>
    /// 简易相机震屏：订阅 <see cref="HitLandedEvent"/>，命中时给主相机叠加噪声位置抖动并随时间衰减。
    ///
    /// MVP 不依赖 Cinemachine（避免强依赖）。挂到 Main Camera 上即可。
    /// 第 2 周接入 Cinemachine 后，建议改用 CinemachineImpulseSource（更专业的相机噪声融合），
    /// 届时本脚本可移除或仅作回退。
    /// </summary>
    public class CameraShake : MonoBehaviour
    {
        [SerializeField] private float _lightAmplitude = 0.06f;
        [SerializeField] private float _heavyAmplitude = 0.16f;
        [SerializeField] private float _duration = 0.18f;
        [SerializeField] private float _frequency = 28f; // 噪声采样频率

        private Transform _cam;
        private Vector3 _originalLocalPos;
        private Coroutine _co;

        private void Awake()
        {
            _cam = Camera.main != null ? Camera.main.transform : transform;
            _originalLocalPos = _cam.localPosition;
        }

        private void OnEnable()  => EventBus.Subscribe<HitLandedEvent>(OnHit);
        private void OnDisable() => EventBus.Unsubscribe<HitLandedEvent>(OnHit);

        private void OnHit(HitLandedEvent e)
        {
            if (_co != null) StopCoroutine(_co);
            _co = StartCoroutine(DoShake(e.HeavyHit || e.Killed ? _heavyAmplitude : _lightAmplitude));
        }

        private IEnumerator DoShake(float amplitude)
        {
            float t = 0f;
            while (t < _duration)
            {
                t += Time.unscaledDeltaTime;
                float decay = 1f - (t / _duration);
                Vector3 offset = new Vector3(
                    Mathf.PerlinNoise(t * _frequency, 0f) - 0.5f,
                    Mathf.PerlinNoise(0f, t * _frequency) - 0.5f,
                    0f) * amplitude * decay * 2f;
                _cam.localPosition = _originalLocalPos + offset;
                yield return null;
            }
            _cam.localPosition = _originalLocalPos;
            _co = null;
        }
    }
}
