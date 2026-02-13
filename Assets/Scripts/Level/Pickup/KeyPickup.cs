using UnityEngine;
using ProjectArk.Core;

namespace ProjectArk.Level
{
    /// <summary>
    /// Pickup that grants the player a key item.
    /// On pickup, adds the key to KeyInventory.
    /// </summary>
    public class KeyPickup : PickupBase
    {
        [Header("Key Item")]
        [Tooltip("The key item SO granted on pickup.")]
        [SerializeField] private KeyItemSO _keyItem;

        protected override void OnPickedUp(GameObject player)
        {
            if (_keyItem == null)
            {
                Debug.LogError($"[KeyPickup] {gameObject.name}: KeyItemSO is not assigned!");
                return;
            }

            var inventory = ServiceLocator.Get<KeyInventory>();
            if (inventory != null)
            {
                inventory.AddKey(_keyItem.KeyID);
                Debug.Log($"[KeyPickup] Picked up key: {_keyItem.DisplayName} ({_keyItem.KeyID})");
            }
            else
            {
                Debug.LogError("[KeyPickup] KeyInventory not found in ServiceLocator!");
            }

            // TODO: Show UI pickup notification (e.g., "Obtained: Access Card Alpha")
        }
    }
}
