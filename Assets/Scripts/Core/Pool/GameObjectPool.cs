using UnityEngine;
using UnityEngine.Pool;

namespace ProjectArk.Core
{
    /// <summary>
    /// Prefab-based object pool wrapping <see cref="UnityEngine.Pool.ObjectPool{T}"/>.
    /// Handles instantiation, activation, IPoolable callbacks, and PoolReference stamping.
    /// </summary>
    public class GameObjectPool
    {
        private readonly GameObject _prefab;
        private readonly Transform _parent;
        private readonly ObjectPool<GameObject> _pool;

        /// <summary>
        /// Creates a new pool for the given prefab.
        /// </summary>
        /// <param name="prefab">The prefab to instantiate.</param>
        /// <param name="parent">Parent transform for inactive instances.</param>
        /// <param name="initialSize">Number of instances to pre-warm.</param>
        /// <param name="maxSize">Maximum pool capacity. Excess items are destroyed.</param>
        public GameObjectPool(GameObject prefab, Transform parent, int initialSize = 10, int maxSize = 100)
        {
            _prefab = prefab;
            _parent = parent;

            _pool = new ObjectPool<GameObject>(
                createFunc: CreateInstance,
                actionOnGet: OnGetInstance,
                actionOnRelease: OnReleaseInstance,
                actionOnDestroy: OnDestroyInstance,
                collectionCheck: false,
                defaultCapacity: initialSize,
                maxSize: maxSize
            );

            // 预热
            var prewarm = new GameObject[initialSize];
            for (int i = 0; i < initialSize; i++)
                prewarm[i] = _pool.Get();
            for (int i = 0; i < initialSize; i++)
                _pool.Release(prewarm[i]);
        }

        /// <summary>
        /// Retrieves an instance from the pool, positioned and rotated as specified.
        /// Position is set BEFORE activation to prevent TrailRenderer artifacts.
        /// </summary>
        public GameObject Get(Vector3 position, Quaternion rotation)
        {
            var instance = _pool.Get(); // OnGetInstance 中不再激活
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.SetActive(true);   // 定位后再激活，避免 Trail 跳线

            var poolables = instance.GetComponents<IPoolable>();
            for (int i = 0; i < poolables.Length; i++)
                poolables[i].OnGetFromPool();

            return instance;
        }

        /// <summary>
        /// Returns an instance to the pool.
        /// </summary>
        public void Return(GameObject instance)
        {
            _pool.Release(instance);
        }

        private GameObject CreateInstance()
        {
            var instance = Object.Instantiate(_prefab, _parent);
            instance.SetActive(false);

            // 确保有 PoolReference 用于自回收
            var poolRef = instance.GetComponent<PoolReference>();
            if (poolRef == null)
                poolRef = instance.AddComponent<PoolReference>();
            poolRef.OwnerPool = this;

            return instance;
        }

        private void OnGetInstance(GameObject instance)
        {
            // 激活和 IPoolable 回调移至 Get() 方法中
            // 确保先定位再激活，防止 TrailRenderer 跳线
        }

        private void OnReleaseInstance(GameObject instance)
        {
            var poolables = instance.GetComponents<IPoolable>();
            for (int i = 0; i < poolables.Length; i++)
                poolables[i].OnReturnToPool();

            instance.SetActive(false);
            instance.transform.SetParent(_parent);
        }

        private void OnDestroyInstance(GameObject instance)
        {
            Object.Destroy(instance);
        }
    }
}
