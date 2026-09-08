using UnityEngine;

namespace ActionArena.Core
{
    public enum GameState
    {
        Boot,       // 启动（场景加载中）
        Menu,       // 主菜单
        Playing,    // 游戏中
        Paused,     // 暂停
        GameOver    // 结束
    }

    /// <summary>
    /// 游戏核心状态机单例。只负责：游戏阶段流转、暂停（timeScale）、计分。
    ///
    /// 刻意不直接引用 WaveManager / UIManager，避免核心层反向依赖表现层。
    /// 其他模块通过订阅 <see cref="GameStateChangedEvent"/> 响应阶段变化。
    /// </summary>
    public class GameManager : SingletonBehaviour<GameManager>
    {
        [Header("调试")]
        [Tooltip("勾选后启动即进入 Playing（跳过主菜单，方便迭代玩法）。")]
        [SerializeField] private bool _startInPlaying = true;

        public GameState State { get; private set; } = GameState.Boot;
        public int Score { get; private set; }

        /// <summary>当前是否处于可游玩状态（用于冻结输入/敌人 AI）。</summary>
        public bool IsPlaying => State == GameState.Playing;

        protected override void OnSingletonAwake()
        {
            // 进主菜单前先归零（Play 模式下按 _startInPlaying 直接开局）
            Score = 0;
            if (_startInPlaying)
                ChangeState(GameState.Playing);
            else
                ChangeState(GameState.Menu);
        }

        /// <summary>切换游戏阶段。会同步 timeScale 并广播事件。</summary>
        public void ChangeState(GameState newState)
        {
            if (State == newState) return;
            State = newState;

            // 暂停冻结世界；其余状态恢复正常流逝（HitStop 会临时改 timeScale，见 HitStop.cs）
            Time.timeScale = (State == GameState.Paused) ? 0f : 1f;

            EventBus.Raise(new GameStateChangedEvent { NewState = State });
            Debug.Log($"[GameManager] → {State}");
        }

        public void StartGame()
        {
            Score = 0;
            EventBus.Raise(new ScoreChangedEvent { Score = 0 });
            ChangeState(GameState.Playing);
        }

        public void Pause()   { if (State == GameState.Playing) ChangeState(GameState.Paused); }
        public void Resume()  { if (State == GameState.Paused)  ChangeState(GameState.Playing); }

        public void GameOver()
        {
            if (State == GameState.GameOver) return;
            ChangeState(GameState.GameOver);
            EventBus.Raise(new PlayerDiedEvent());
        }

        /// <summary>加分（仅在 Playing 状态生效）。</summary>
        public void AddScore(int amount)
        {
            if (!IsPlaying) return;
            Score += amount;
            EventBus.Raise(new ScoreChangedEvent { Score = Score });
        }
    }
}
