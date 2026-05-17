using System;
using UnityEngine;

namespace ProjectArk.Core
{
    /// <summary>
    /// Global static combat events. Lives in Core so both Combat and Enemy assemblies
    /// can subscribe/publish without circular dependencies.
    /// </summary>
    public static class CombatEvents
    {
        /// <summary>
        /// Broadcast when any weapon fires. Used by enemy auditory perception.
        /// Params: (firePosition, noiseRadius).
        /// Published by StarChartController, consumed by EnemyPerception.
        /// </summary>
        public static event Action<Vector2, float> OnWeaponFired;

        /// <summary>
        /// Broadcast when a player-owned physical projectile is spawned.
        /// Params: (spawnPosition, fireDirection). Used by HyperWind cyclone direction inheritance.
        /// </summary>
        public static event Action<Vector2, Vector2> OnPlayerProjectileFired;

        /// <summary>
        /// Raise the OnWeaponFired event. Call from StarChartController.
        /// </summary>
        public static void RaiseWeaponFired(Vector2 position, float noiseRadius)
        {
            OnWeaponFired?.Invoke(position, noiseRadius);
        }

        /// <summary>
        /// Raise when a player physical projectile is spawned. Direction is normalized when possible.
        /// </summary>
        public static void RaisePlayerProjectileFired(Vector2 position, Vector2 direction)
        {
            Vector2 normalizedDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
            OnPlayerProjectileFired?.Invoke(position, normalizedDirection);
        }

    }
}
