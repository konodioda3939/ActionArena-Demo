using System;
using System.Collections.Generic;

namespace ActionArena.Core
{
    /// <summary>
    /// 所有游戏事件的标记接口。事件用 struct 承载，零分配。
    /// </summary>
    public interface IGameEvent { }

    /// <summary>
    /// 轻量事件总线：用 C# 事件解耦游戏逻辑与表现层（UI、特效、镜头）。
    ///
    /// 设计动机：GameManager/Player 不直接引用 HUD/VFX，
    /// 而是发布事件，由表现层自行订阅。这样战斗逻辑可独立测试，UI 可自由替换。
    ///
    /// 用法：
    ///   // 发布
    ///   EventBus.Raise(new EnemyKilledEvent { Score = 100 });
    ///   // 订阅（在 OnEnable 注册、OnDisable 注销，避免生命周期泄漏）
    ///   private void OnEnable()  => EventBus.Subscribe&lt;EnemyKilledEvent&gt;(OnKilled);
    ///   private void OnDisable() => EventBus.Unsubscribe&lt;EnemyKilledEvent&gt;(OnKilled);
    ///   private void OnKilled(EnemyKilledEvent e) { ... }
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, Delegate> _handlers = new();

        public static void Subscribe<T>(Action<T> handler) where T : struct, IGameEvent
        {
            if (handler == null) return;
            Type t = typeof(T);
            _handlers[t] = _handlers.TryGetValue(t, out Delegate existing)
                ? Delegate.Combine(existing, handler)
                : handler;
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : struct, IGameEvent
        {
            if (handler == null) return;
            Type t = typeof(T);
            if (!_handlers.TryGetValue(t, out Delegate existing)) return;

            Delegate removed = Delegate.Remove(existing, handler);
            if (removed == null) _handlers.Remove(t);
            else _handlers[t] = removed;
        }

        public static void Raise<T>(T evt) where T : struct, IGameEvent
        {
            if (_handlers.TryGetValue(typeof(T), out Delegate existing) && existing is Action<T> action)
                action.Invoke(evt);
        }

        /// <summary>清空所有订阅（场景重载/测试用，正常运行不需要调）。</summary>
        public static void Clear() => _handlers.Clear();
    }
}
