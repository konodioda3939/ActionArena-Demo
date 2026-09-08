using UnityEngine;

namespace ActionArena.Core
{
    /// <summary>
    /// 泛型 MonoBehaviour 单例基类。
    ///
    /// 挂在场景中的唯一实例上，提供全局访问入口。子类通过 <see cref="Instance"/> 取用。
    /// 不自动跨场景持久化；需要时在 <see cref="OnSingletonAwake"/> 内调用 DontDestroyOnLoad。
    ///
    /// 用法：
    ///   public class GameManager : SingletonBehaviour&lt;GameManager&gt; { ... }
    ///   GameManager.Instance.StartGame();
    /// </summary>
    public abstract class SingletonBehaviour<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static bool _isQuitting;

        /// <summary>全局唯一实例。访问时若不存在会尝试查找并给出警告。</summary>
        public static T Instance
        {
            get
            {
                if (_isQuitting) return null;
                if (_instance == null)
                {
                    _instance = FindObjectOfType<T>();
                    if (_instance == null)
                        Debug.LogWarning($"[Singleton] 场景中未找到 {typeof(T).Name}。" +
                                         $"请确保该组件已放置在场景中。");
                }
                return _instance;
            }
        }

        /// <summary>实例是否存在（不触发查找）。订阅事件前先判断，避免空引用。</summary>
        public static bool HasInstance => _instance != null && !_isQuitting;

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
                OnSingletonAwake();
            }
            else if (_instance != this)
            {
                Debug.LogWarning($"[Singleton] {typeof(T).Name} 已存在实例，重复实例被销毁。");
                Destroy(gameObject);
            }
        }

        protected virtual void OnApplicationQuit() => _isQuitting = true;

        protected virtual void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        /// <summary>单例完成注册后的初始化回调，子类重写以替代 Awake（确保此时 Instance 已就绪）。</summary>
        protected virtual void OnSingletonAwake() { }
    }
}
