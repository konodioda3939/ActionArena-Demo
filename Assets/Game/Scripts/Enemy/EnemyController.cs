using UnityEngine;
using UnityEngine.AI;
using ActionArena.Core;
using ActionArena.Combat;

namespace ActionArena.Enemy
{
    public enum EnemyState { Chase, Attack, Dead }

    /// <summary>
    /// 敌人 AI 状态机：Chase（NavMesh 追击）→ Attack（进入攻击范围）→ Dead。
    /// 攻击命中窗口逻辑与 PlayerCombat 一致：定时器控制 HitBox 启停。
    /// 死亡时停 NavMesh、播死亡动画、交给 EnemyHealth 延迟销毁。
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyController : MonoBehaviour
    {
        [Header("战斗参数")]
        [SerializeField] private float _attackRange = 1.8f;
        [SerializeField] private float _attackCooldown = 1.6f;
        [SerializeField] private float _attackDuration = 0.8f;
        [SerializeField] private float _hitEnableDelay = 0.2f;
        [SerializeField] private float _hitActiveDuration = 0.2f;
        [SerializeField] private float _stopDistance = 1.4f;

        [Header("引用")]
        [SerializeField] private HitBox _hitBox;
        [SerializeField] private Animator _animator;
        [SerializeField] private EnemyHealth _health;

        private NavMeshAgent _agent;
        private EnemyState _state = EnemyState.Chase;
        private Transform _player;
        private float _lastAttackTime;
        private float _attackStartTime;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int DeadHash = Animator.StringToHash("Dead");

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _agent.stoppingDistance = _stopDistance;
            if (_health == null) _health = GetComponent<EnemyHealth>();
            if (_animator == null) _animator = GetComponentInChildren<Animator>();
        }

        /// <summary>生成时由 EnemySpawner 注入玩家目标。</summary>
        public void Initialize(Transform player)
        {
            _player = player;
            _state = EnemyState.Chase;
        }

        private void Update()
        {
            if (_health != null && _health.IsDead) { EnterDead(); return; }

            if (!(GameManager.HasInstance && GameManager.Instance.IsPlaying))
            {
                _agent.isStopped = true;
                return;
            }
            // 兜底：未被 EnemySpawner.Initialize 注入时，自动按 Tag 找玩家。
            // 方便场景里手放的测试敌人也能追击；正式生成时 Spawner 仍会显式注入（覆盖即可）。
            if (_player == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) _player = p.transform;
                else return;
            }

            switch (_state)
            {
                case EnemyState.Chase:  TickChase(); break;
                case EnemyState.Attack: TickAttack(); break;
            }

            _animator?.SetFloat(SpeedHash, _agent.velocity.magnitude);
        }

        private void TickChase()
        {
            _agent.isStopped = false;
            _agent.SetDestination(_player.position);

            float dist = Vector3.Distance(transform.position, _player.position);
            if (dist <= _attackRange && Time.time - _lastAttackTime >= _attackCooldown)
                BeginAttack();
        }

        private void BeginAttack()
        {
            _state = EnemyState.Attack;
            _lastAttackTime = Time.time;
            _attackStartTime = Time.time;
            _agent.isStopped = true;
            _animator?.SetTrigger(AttackHash);
        }

        private void TickAttack()
        {
            float t = Time.time - _attackStartTime;
            float hitEnd = _hitEnableDelay + _hitActiveDuration;

            if (t >= _hitEnableDelay && t < hitEnd)
            {
                if (_hitBox != null && !_hitBox.IsActive) _hitBox.Enable();
            }
            else if (t >= hitEnd)
            {
                if (_hitBox != null && _hitBox.IsActive) _hitBox.Disable();
            }

            if (t >= _attackDuration)
            {
                _state = EnemyState.Chase;
                if (_hitBox != null && _hitBox.IsActive) _hitBox.Disable();
            }
        }

        private void EnterDead()
        {
            if (_state == EnemyState.Dead) return;
            _state = EnemyState.Dead;
            _agent.isStopped = true;
            _agent.enabled = false;
            _animator?.SetBool(DeadHash, true);
            if (_hitBox != null && _hitBox.IsActive) _hitBox.Disable();
        }
    }
}
