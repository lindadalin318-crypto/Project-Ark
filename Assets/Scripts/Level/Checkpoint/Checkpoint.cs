using UnityEngine;
using ProjectArk.Core;
using ProjectArk.Core.Audio;
using ProjectArk.Ship;
using ProjectArk.Heat;

namespace ProjectArk.Level
{
    /// <summary>
    /// Scene checkpoint component. Player enters trigger range then presses Interact
    /// to activate the checkpoint. On activation: optionally restores HP/heat,
    /// notifies CheckpointManager, and plays feedback.
    /// 
    /// Place as a child of a Room or standalone in the scene.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Checkpoint : MonoBehaviour
    {
        // ──────────────────── Configuration ────────────────────

        [Header("Data")]
        [Tooltip("Checkpoint configuration SO.")]
        [SerializeField] private CheckpointSO _data;

        [Header("Player Detection")]
        [Tooltip("Layer mask for the player ship.")]
        [SerializeField] private LayerMask _playerLayer;

        [Header("Visuals")]
        [Tooltip("SpriteRenderer for visual feedback (activated/deactivated state).")]
        [SerializeField] private SpriteRenderer _spriteRenderer;

        [Tooltip("Color when the checkpoint is inactive.")]
        [SerializeField] private Color _inactiveColor = new Color(0.4f, 0.4f, 0.4f, 1f);

        [Tooltip("Color when the checkpoint is the active spawn point.")]
        [SerializeField] private Color _activeColor = new Color(1f, 0.9f, 0.3f, 1f);

        // ──────────────────── Runtime State ────────────────────

        private bool _playerInRange;
        private bool _isActivated;

        // ──────────────────── Public Properties ────────────────────

        /// <summary> Checkpoint configuration data. </summary>
        public CheckpointSO Data => _data;

        /// <summary> Whether this checkpoint is currently the active respawn point. </summary>
        public bool IsActivated => _isActivated;

        /// <summary> World position of this checkpoint (spawn point). </summary>
        public Vector3 SpawnPosition => transform.position;

        // ──────────────────── Lifecycle ────────────────────

        private void Awake()
        {
            // Validate
            if (_data == null)
                Debug.LogError($"[Checkpoint] {gameObject.name}: CheckpointSO is not assigned!");

            // Ensure trigger
            var col = GetComponent<Collider2D>();
            if (!col.isTrigger)
            {
                col.isTrigger = true;
                Debug.LogWarning($"[Checkpoint] {gameObject.name}: Collider was not set as trigger. Auto-fixed.");
            }

            // Auto-find SpriteRenderer if not assigned
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            // Start in inactive visual state
            UpdateVisuals();
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
            if (!IsPlayerLayer(other.gameObject)) return;
            _playerInRange = true;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsPlayerLayer(other.gameObject)) return;
            _playerInRange = false;
        }

        private bool IsPlayerLayer(GameObject obj)
        {
            return (_playerLayer.value & (1 << obj.layer)) != 0;
        }

        // ──────────────────── Interaction ────────────────────

        private void HandleInteract()
        {
            if (!_playerInRange) return;
            Activate();
        }

        /// <summary>
        /// Activate this checkpoint. Restores HP/heat, notifies manager, plays SFX.
        /// Can be called externally (e.g., auto-activate on first visit).
        /// </summary>
        public void Activate()
        {
            if (_data == null) return;

            // ── Restore resources ──
            if (_data.RestoreHP)
            {
                var health = ServiceLocator.Get<ShipHealth>();
                if (health != null) health.ResetHealth();
            }

            if (_data.RestoreHeat)
            {
                var heat = ServiceLocator.Get<HeatSystem>();
                if (heat != null) heat.ResetHeat();
            }

            // ── Notify CheckpointManager ──
            var manager = ServiceLocator.Get<CheckpointManager>();
            if (manager != null)
            {
                manager.ActivateCheckpoint(this);
            }
            else
            {
                Debug.LogWarning("[Checkpoint] CheckpointManager not found in ServiceLocator!");
            }

            // ── Audio feedback ──
            if (_data.ActivationSFX != null)
            {
                var audio = ServiceLocator.Get<AudioManager>();
                if (audio != null)
                    audio.PlaySFX2D(_data.ActivationSFX);
            }

            Debug.Log($"[Checkpoint] Activated: {_data.CheckpointID} ({_data.DisplayName})");
        }

        // ──────────────────── Visual State ────────────────────

        /// <summary>
        /// Mark this checkpoint as the currently active respawn point.
        /// Called by CheckpointManager when switching checkpoints.
        /// </summary>
        public void SetActivated(bool active)
        {
            _isActivated = active;
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (_spriteRenderer == null) return;
            _spriteRenderer.color = _isActivated ? _activeColor : _inactiveColor;
        }
    }
}
