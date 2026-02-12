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
        /// Raise the OnWeaponFired event. Call from StarChartController.
        /// </summary>
        public static void RaiseWeaponFired(Vector2 position, float noiseRadius)
        {
            OnWeaponFired?.Invoke(position, noiseRadius);
        }
    }
}
