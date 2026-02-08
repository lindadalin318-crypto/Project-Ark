using System.Collections.Generic;
using UnityEngine;

namespace ProjectArk.Core
{
    /// <summary>
    /// Central registry for all object pools. Singleton.
    /// Access via <c>PoolManager.Instance.GetPool(prefab)</c>.
    /// Place on a persistent GameObject in the scene.
    /// </summary>
    public class PoolManager : MonoBehaviour
    {
        public static PoolManager Instance { get; private set; }

        // 按 prefab InstanceID 索引池
        private readonly Dictionary<int, GameObjectPool> _pools = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Returns (or creates) a pool for the given prefab.
        /// </summary>
        /// <param name="prefab">The prefab to pool.</param>
        /// <param name="initialSize">Pre-warm count (only used on first creation).</param>
        /// <param name="maxSize">Max capacity (only used on first creation).</param>
        public GameObjectPool GetPool(GameObject prefab, int initialSize = 10, int maxSize = 100)
        {
            int key = prefab.GetInstanceID();

            if (_pools.TryGetValue(key, out var pool))
                return pool;

            // 创建该 prefab 专属的父节点
            var parentObj = new GameObject($"Pool_{prefab.name}");
            parentObj.transform.SetParent(transform);

            pool = new GameObjectPool(prefab, parentObj.transform, initialSize, maxSize);
            _pools[key] = pool;

            return pool;
        }
    }
}
