using UnityEngine;
using ProjectArk.Core;
using ProjectArk.Core.Audio;
using ProjectArk.Ship;

namespace ProjectArk.Level
{
    /// <summary>
    /// Lock component that blocks a door until the player has the required key.
    /// Can be placed on the Door itself or on a separate nearby GameObject.
    /// Player enters trigger range + presses Interact → checks KeyInventory → unlocks or shows hint.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Lock : MonoBehaviour
    {
        // ──────────────────── Configuration ────────────────────

        [Header("Lock")]
        [Tooltip("The key required to open this lock.")]
        [SerializeField] private KeyItemSO _requiredKey;

        [Tooltip("The door to unlock when the key is used.")]
        [SerializeField] private Door _targetDoor;

        [Tooltip("If true, the key is consumed (removed from inventory) on unlock.")]
        [SerializeField] private bool _consumeKey;

        [Header("Player Detection")]
        [Tooltip("Layer mask for the player ship.")]
        [SerializeField] private LayerMask _playerLayer;

        [Header("Feedback")]
        [Tooltip("Sound played on successful unlock.")]
        [SerializeField] private AudioClip _unlockSFX;

        [Tooltip("Sound played when the player lacks the required key.")]
        [SerializeField] private AudioClip _lockedSFX;

        // ──────────────────── Runtime State ────────────────────

        private bool _playerInRange;
        private bool _isUnlocked;

        // ──────────────────── Lifecycle ────────────────────

        private void Awake()
        {
            // Validate
            if (_requiredKey == null)
                Debug.LogError($"[Lock] {gameObject.name}: RequiredKey is not assigned!");
            if (_targetDoor == null)
                Debug.LogError($"[Lock] {gameObject.name}: TargetDoor is not assigned!");

            // Ensure trigger
            var col = GetComponent<Collider2D>();
            if (!col.isTrigger)
            {
                col.isTrigger = true;
                Debug.LogWarning($"[Lock] {gameObject.name}: Collider was not set as trigger. Auto-fixed.");
            }
        }

        private void OnEnable()
        {
            var inputHandler = ServiceLocator.Get<InputHandler>();
            if (inputHandler != null)
                inputHandler.OnInteractPerformed += HandleInteract;
        }

        private void OnDisable()
        {
            var inputHandler = ServiceLocator.Get<InputHandler>();
            if (inputHandler != null)
                inputHandler.OnInteractPerformed -= HandleInteract;
        }

        // ──────────────────── Player Detection ────────────────────

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isUnlocked) return;
            if (!IsPlayerLayer(other.gameObject)) return;
            _playerInRange = true;

            // TODO: Show "Press E to unlock" or "Requires XX Key" prompt
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsPlayerLayer(other.gameObject)) return;
            _playerInRange = false;

            // TODO: Hide prompt
        }

        private bool IsPlayerLayer(GameObject obj)
        {
            return (_playerLayer.value & (1 << obj.layer)) != 0;
        }

        // ──────────────────── Interaction ────────────────────

        private void HandleInteract()
        {
            if (!_playerInRange || _isUnlocked) return;

            var inventory = ServiceLocator.Get<KeyInventory>();
            if (inventory == null)
            {
                Debug.LogError("[Lock] KeyInventory not found in ServiceLocator!");
                return;
            }

            if (inventory.HasKey(_requiredKey.KeyID))
            {
                Unlock(inventory);
            }
            else
            {
                OnLockFailed();
            }
        }

        private void Unlock(KeyInventory inventory)
        {
            _isUnlocked = true;

            // Consume key if configured
            if (_consumeKey)
                inventory.RemoveKey(_requiredKey.KeyID);

            // Open the door
            if (_targetDoor != null)
                _targetDoor.SetState(DoorState.Open);

            // Audio feedback
            var audio = ServiceLocator.Get<AudioManager>();
            if (audio != null && _unlockSFX != null)
                audio.PlaySFX2D(_unlockSFX);

            Debug.Log($"[Lock] Unlocked {gameObject.name} with key: {_requiredKey.KeyID}");

            // Disable the lock component (no longer needed)
            enabled = false;

            // TODO: Visual feedback (door opens animation, lock disappears)
        }

        private void OnLockFailed()
        {
            // Audio feedback
            var audio = ServiceLocator.Get<AudioManager>();
            if (audio != null && _lockedSFX != null)
                audio.PlaySFX2D(_lockedSFX);

            Debug.Log($"[Lock] Missing key: {_requiredKey.DisplayName} ({_requiredKey.KeyID})");

            // TODO: Show "Requires XX Key" UI notification
        }
    }
}
