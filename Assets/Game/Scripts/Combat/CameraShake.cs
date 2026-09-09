using UnityEngine;
using ActionArena.Core;
using Cinemachine;

namespace ActionArena.Combat
{
    /// <summary>
    /// 相机震屏（Cinemachine Impulse 版）：订阅 <see cref="HitLandedEvent"/>，
    /// 命中时通过 <see cref="CinemachineImpulseSource"/> 发出冲击，由挂在
    /// Cinemachine 虚拟相机（FreeLook）上的 CinemachineImpulseListener 融合到镜头上。
    ///
    /// 挂到 Main Camera 上即可；ImpulseSource 组件会自动带上（RequireComponent）。
    /// ⚠️ 若 ImpulseSource 是自动补挂的（非手动添加），不走 Reset()，
    /// 其默认 m_ImpulseType=Legacy 且 m_RawSignal=null → 零信号、无震感。
    /// 因此 Awake 里强制把定义配成 Uniform+Bump（不依赖信号资产）。
    /// </summary>
    [RequireComponent(typeof(CinemachineImpulseSource))]
    public class CameraShake : MonoBehaviour
    {
        [Header("冲击力度（Impulse 速度，单位约=位移幅度）")]
        [SerializeField] private float _lightForce = 0.25f; // 普通命中
        [SerializeField] private float _heavyForce = 0.6f;  // 重击 / 击杀

        [Header("调试")]
        [SerializeField] private bool _debugLog = false;

        private CinemachineImpulseSource _impulse;

        private void Awake()
        {
            _impulse = GetComponent<CinemachineImpulseSource>();

            // 强制配置冲击定义：Uniform 类型 + Bump 包络，方向完全由调用时的速度向量决定，
            // 不依赖 Inspector 里的 Signal 资产（自动补挂的组件 Signal 恒为 null）。
            var def = _impulse.m_ImpulseDefinition;
            def.m_ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
            def.m_ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump;
            def.m_ImpulseDuration = 0.25f;
            def.m_ImpulseChannel = 1;          // 与 Listener 的 Channel Mask (0: default) 对应
            def.m_DissipationDistance = 500f;  // 源在相机上、监听者就在旁边，距离不是问题
            def.m_PropagationSpeed = 1000f;    // 近乎瞬时到达
            _impulse.m_DefaultVelocity = Vector3.down;
        }

        private void OnEnable()  => EventBus.Subscribe<HitLandedEvent>(OnHit);
        private void OnDisable() => EventBus.Unsubscribe<HitLandedEvent>(OnHit);

        private void OnHit(HitLandedEvent e)
        {
            float force = e.HeavyHit || e.Killed ? _heavyForce : _lightForce;

            // 显式速度向量：主要向下砸 + 少量水平随机，避免 Legacy 模式的方向歧义。
            Vector3 velocity = new Vector3(
                Random.Range(-0.35f, 0.35f),
                -1f,
                Random.Range(-0.35f, 0.35f)) * force;

            _impulse.GenerateImpulseWithVelocity(velocity);

            if (_debugLog)
                Debug.Log($"[CameraShake] HitLanded → impulse v={velocity} (heavy={e.HeavyHit}, killed={e.Killed})");
        }
    }
}
