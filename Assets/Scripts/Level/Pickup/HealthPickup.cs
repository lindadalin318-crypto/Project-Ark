using UnityEngine;
using ProjectArk.Core;
using ProjectArk.Ship;

namespace ProjectArk.Level
{
    /// <summary>
    /// Pickup that restores a flat amount of HP to the player ship.
    /// </summary>
    public class HealthPickup : PickupBase
    {
        [Header("Heal")]
        [Tooltip("Amount of HP restored on pickup.")]
        [SerializeField] private float _healAmount = 25f;

        protected override void OnPickedUp(GameObject player)
        {
            var health = ServiceLocator.Get<ShipHealth>();
            if (health != null)
            {
                health.Heal(_healAmount);
                Debug.Log($"[HealthPickup] Healed {_healAmount} HP");
            }
            else
            {
                Debug.LogWarning("[HealthPickup] ShipHealth not found in ServiceLocator.");
            }
        }
    }
}
