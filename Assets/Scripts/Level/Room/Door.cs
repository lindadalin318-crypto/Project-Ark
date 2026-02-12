using UnityEngine;
using ProjectArk.Core;

namespace ProjectArk.Level
{
    /// <summary>
    /// A door/passage connecting two rooms. Handles player detection and
    /// delegates transition execution to DoorTransitionController.
    /// 
    /// Place as a child of a Room GameObject. Door pairs are one-way references:
    /// each Door knows its target room and spawn point.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class Door : MonoBehaviour
    {
        // ──────────────────── Configuration ────────────────────

        [Header("Connection")]
        [Tooltip("The room this door leads to.")]
        [SerializeField] private Room _targetRoom;

        [Tooltip("Position where the player appears after passing through this door.")]
        [SerializeField] private Transform _targetSpawnPoint;

        [Header("State")]
        [Tooltip("Initial state when the scene loads.")]
        [SerializeField] private DoorState _initialState = DoorState.Open;

        [Header("Transition")]
        [Tooltip("If true, uses longer transition effect (for floor/layer changes).")]
        [SerializeField] private bool _isLayerTransition;

        [Header("Player Detection")]
        [Tooltip("Layer mask for the player ship.")]
        [SerializeField] private LayerMask _playerLayer;

        // ──────────────────── Runtime State ────────────────────

        private DoorState _currentState;
        private bool _playerInRange;
        private bool _isTransitioning;

        // ──────────────────── Public Properties ────────────────────

        /// <summary> The room this door leads to. </summary>
        public Room TargetRoom => _targetRoom;

        /// <summary> Spawn point in the target room. </summary>
        public Transform TargetSpawnPoint => _targetSpawnPoint;

        /// <summary> Current door state. </summary>
        public DoorState CurrentState => _currentState;

        /// <summary> Whether this is a layer transition (longer fade). </summary>
        public bool IsLayerTransition => _isLayerTransition;

        // ──────────────────── Lifecycle ────────────────────

        private void Awake()
        {
            _currentState = _initialState;

            // Validate
            if (_targetRoom == null)
                Debug.LogError($"[Door] {gameObject.name}: TargetRoom is not assigned!");
            if (_targetSpawnPoint == null)
                Debug.LogError($"[Door] {gameObject.name}: TargetSpawnPoint is not assigned!");

            // Ensure trigger
            var col = GetComponent<Collider2D>();
            if (!col.isTrigger)
            {
                col.isTrigger = true;
                Debug.LogWarning($"[Door] {gameObject.name}: Collider was not set as trigger. Auto-fixed.");
            }

            // Door needs its own Rigidbody2D to receive OnTriggerEnter2D independently
            // from its parent Room. Without it, the child Collider merges into the parent's
            // static body and only the parent gets trigger callbacks.
            var rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;
        }

        // ──────────────────── Player Detection ────────────────────

        private void OnTriggerEnter2D(Collider2D other)
        {
            Debug.Log($"[Door] {gameObject.name}: OnTriggerEnter2D fired by '{other.gameObject.name}' (Layer: {LayerMask.LayerToName(other.gameObject.layer)})");

            if (!IsPlayerLayer(other.gameObject))
            {
                Debug.Log($"[Door] {gameObject.name}: Layer check failed. Expected mask={_playerLayer.value}, got layer={other.gameObject.layer}");
                return;
            }

            _playerInRange = true;
            Debug.Log($"[Door] {gameObject.name}: Player detected. State={_currentState}, IsTransitioning={_isTransitioning}");

            // Auto-transition for Open doors when player enters trigger
            if (_currentState == DoorState.Open && !_isTransitioning)
            {
                TryTransition();
            }
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

        // ──────────────────── Transition ────────────────────

        private void TryTransition()
        {
            Debug.Log($"[Door] {gameObject.name}: TryTransition called. State={_currentState}");

            if (_currentState != DoorState.Open)
            {
                Debug.Log($"[Door] {gameObject.name}: Aborted — door not Open (state={_currentState})");
                return;
            }
            if (_targetRoom == null || _targetSpawnPoint == null)
            {
                Debug.LogError($"[Door] {gameObject.name}: Aborted — TargetRoom={_targetRoom}, TargetSpawnPoint={_targetSpawnPoint}");
                return;
            }

            var controller = ServiceLocator.Get<DoorTransitionController>();
            if (controller == null)
            {
                Debug.LogError("[Door] DoorTransitionController not found in ServiceLocator!");
                return;
            }

            Debug.Log($"[Door] {gameObject.name}: Starting transition to {_targetRoom.RoomID}");
            _isTransitioning = true;
            controller.TransitionThroughDoor(this, () => _isTransitioning = false);
        }

        // ──────────────────── State API ────────────────────

        /// <summary>
        /// Change the door state. Called by Room (combat lock/unlock) or external systems.
        /// </summary>
        public void SetState(DoorState newState)
        {
            if (_currentState == newState) return;

            var oldState = _currentState;
            _currentState = newState;

            Debug.Log($"[Door] {gameObject.name}: {oldState} → {newState}");

            // TODO: Visual feedback (change sprite/color based on state)
        }
    }
}
