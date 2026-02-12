using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectArk.Core
{
    /// <summary>
    /// Lightweight static service registry. Services register themselves in Awake()
    /// and unregister in OnDestroy(). Consumers call Get&lt;T&gt;() to retrieve services.
    /// Replaces scattered singletons, FindAnyObjectByType, and static Instance patterns
    /// with a single, predictable service resolution point.
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new();

        /// <summary>
        /// Register a service instance. Only one instance per type is allowed.
        /// Call in Awake() of the service MonoBehaviour.
        /// </summary>
        public static void Register<T>(T service) where T : class
        {
            var type = typeof(T);
            if (_services.ContainsKey(type))
            {
                Debug.LogWarning($"[ServiceLocator] Service '{type.Name}' is already registered. Overwriting.");
            }
            _services[type] = service;
        }

        /// <summary>
        /// Retrieve a registered service. Returns null if not found.
        /// </summary>
        public static T Get<T>() where T : class
        {
            return _services.TryGetValue(typeof(T), out var service) ? (T)service : null;
        }

        /// <summary>
        /// Unregister a service. Call in OnDestroy() of the service MonoBehaviour.
        /// Only removes if the registered instance matches (prevents stale unregister).
        /// </summary>
        public static void Unregister<T>(T service) where T : class
        {
            var type = typeof(T);
            if (_services.TryGetValue(type, out var existing) && ReferenceEquals(existing, service))
            {
                _services.Remove(type);
            }
        }

        /// <summary>
        /// Clear all registered services. Call during scene transitions if needed.
        /// </summary>
        public static void Clear()
        {
            _services.Clear();
        }
    }
}
