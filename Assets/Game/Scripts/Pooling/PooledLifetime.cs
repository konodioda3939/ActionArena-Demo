using System.Collections;
using UnityEngine;

namespace ActionArena.Pooling
{
    /// <summary>
    /// 挂在池化对象上，提供"取出后 N 秒自动回收到池"。
    /// 配合 <see cref="ObjectPool.GetAndAutoRelease"/> 使用。
    /// </summary>
    public class PooledLifetime : MonoBehaviour
    {
        private Coroutine _co;
        private ObjectPool _pool;

        public void BeginLifetime(ObjectPool pool, float lifetime)
        {
            _pool = pool;
            if (_co != null) StopCoroutine(_co);
            _co = StartCoroutine(DelayedRelease(lifetime));
        }

        private IEnumerator DelayedRelease(float lifetime)
        {
            yield return new WaitForSeconds(lifetime);
            _pool?.Release(gameObject);
            _co = null;
        }

        private void OnDisable()
        {
            // 对象被提前回收/失活时，停掉挂起的回收协程，避免重复 Release
            if (_co != null)
            {
                StopCoroutine(_co);
                _co = null;
            }
        }
    }
}
