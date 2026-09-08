using System.Collections.Generic;
using UnityEngine;

namespace ActionArena.Pooling
{
    /// <summary>
    /// 通用 GameObject 对象池：预分配 + 按需扩容。
    /// 命中特效、飘字等高频生成物统一走池，避免运行时 Instantiate/Destroy 的 GC 与卡顿。
    ///
    /// 用法：
    ///   var hit = _pool.Get(pos, rot);           // 取出（已激活）
    ///   _pool.Release(hit);                      // 手动回收
    ///   _pool.GetAndAutoRelease(pos, rot, 0.5f); // 取出并 0.5s 后自动回收
    /// </summary>
    public class ObjectPool : MonoBehaviour
    {
        [SerializeField] private GameObject _prefab;
        [SerializeField] private int _initialSize = 8;
        [SerializeField] private int _maxSize = 64;
        [Tooltip("池实例的父节点（留空则自动创建）")]
        [SerializeField] private Transform _parent;

        private readonly Stack<GameObject> _available = new();

        private void Awake()
        {
            if (_parent == null)
            {
                var root = new GameObject($"{name}_PoolRoot");
                root.transform.SetParent(transform, false);
                _parent = root.transform;
            }
            for (int i = 0; i < _initialSize; i++)
                _available.Push(CreateInstance());
        }

        public GameObject Get(Vector3 position, Quaternion rotation)
        {
            GameObject obj = _available.Count > 0 ? _available.Pop() : CreateInstance();
            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(true);
            return obj;
        }

        public void Release(GameObject obj)
        {
            if (obj == null) return;
            obj.SetActive(false);
            if (_available.Count < _maxSize)
                _available.Push(obj);
            else
                Destroy(obj);
        }

        /// <summary>取出并延迟自动回收（命中特效/飘字常用）。</summary>
        public GameObject GetAndAutoRelease(Vector3 pos, Quaternion rot, float lifetime)
        {
            GameObject obj = Get(pos, rot);
            var handle = obj.GetComponent<PooledLifetime>() ?? obj.AddComponent<PooledLifetime>();
            handle.BeginLifetime(this, lifetime);
            return obj;
        }

        private GameObject CreateInstance()
        {
            GameObject obj = Instantiate(_prefab, _parent);
            obj.SetActive(false);
            return obj;
        }
    }
}
