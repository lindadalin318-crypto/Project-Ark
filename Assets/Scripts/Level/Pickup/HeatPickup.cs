using UnityEngine;
using ProjectArk.Core;
using ProjectArk.Heat;

namespace ProjectArk.Level
{
    /// <summary>
    /// Pickup that fully resets the player's heat to zero.
    /// </summary>
    public class HeatPickup : PickupBase
    {
        protected override void OnPickedUp(GameObject player)
        {
            var heat = ServiceLocator.Get<HeatSystem>();
            if (heat != null)
            {
                heat.ResetHeat();
                Debug.Log("[HeatPickup] Heat reset to 0");
            }
            else
            {
                Debug.LogWarning("[HeatPickup] HeatSystem not found in ServiceLocator.");
            }
        }
    }
}
