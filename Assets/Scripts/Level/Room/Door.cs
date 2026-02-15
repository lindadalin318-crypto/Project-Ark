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

        [Header("Lock")]
        [Tooltip("If set, this door requires a key to open. Used by Lock component / UI tooltip.")]
        [SerializeField] private string _requiredKeyID;

        [Header("Schedule (for Locked_Schedule doors)")]
        [Tooltip("Phase indices during which this door opens. Only relevant if initialState is Locked_Schedule.")]
        [SerializeField] private int[] _openDuringPhases;

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

        /// <summary> Required key ID for this door (empty = no key needed). </summary>
        public string RequiredKeyID => _requiredKeyID;

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
        }

        private void Start()
        {
            // Subscribe to phase changes for schedule-locked doors
            if (_initialState == DoorState.Locked_Schedule)
            {
                LevelEvents.OnPhaseChanged += HandlePhaseChanged;

                // Apply initial state from current phase
                var phaseManager = ServiceLocator.Get<WorldPhaseManager>();
                if (phaseManager != null && phaseManager.CurrentPhaseIndex >= 0)
                {
                    EvaluateSchedule(phaseManager.CurrentPhaseIndex);
                }
            }
        }

        private void OnDestroy()
        {
            if (_initialState == DoorState.Locked_Schedule)
            {
                LevelEvents.OnPhaseChanged -= HandlePhaseChanged;
            }
        }

        // ──────────────────── Schedule Logic ────────────────────

        private void HandlePhaseChanged(int phaseIndex, string phaseName)
        {
            if (_initialState != DoorState.Locked_Schedule) return;
            EvaluateSchedule(phaseIndex);
        }

        private void EvaluateSchedule(int currentPhaseIndex)
        {
            bool shouldOpen = IsOpenDuringPhase(currentPhaseIndex);

            if (shouldOpen && _currentState == DoorState.Locked_Schedule)
            {
                SetState(DoorState.Open);
            }
            else if (!shouldOpen && _currentState == DoorState.Open && _initialState == DoorState.Locked_Schedule)
            {
                SetState(DoorState.Locked_Schedule);
            }
        }

        private bool IsOpenDuringPhase(int phaseIndex)
        {
            if (_openDuringPhases == null || _openDuringPhases.Length == 0)
                return false;

            for (int i = 0; i < _openDuringPhases.Length; i++)
            {
                if (_openDuringPhases[i] == phaseIndex)
                    return true;
            }

            return false;
        }

        // ──────────────────── Player Detection ────────────────────

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsPlayerLayer(other.gameObject)) return;
            _playerInRange = true;
            TryAutoTransition();
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            // 当玩家被传送到门的 Trigger 内时，OnTriggerEnter2D 在转场期间触发会被拒绝，
            // 转场结束后 Stay 会重新尝试，避免"卡门"问题
            if (!_playerInRange) return;
            if (!IsPlayerLayer(other.gameObject)) return;
            TryAutoTransition();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsPlayerLayer(other.gameObject)) return;
            _playerInRange = false;
            _hasLoggedMissingKey = false; // 离开后重置，下次进入重新提示
        }

        private bool _hasLoggedMissingKey;

        private void TryAutoTransition()
        {
            if (_isTransitioning) return;

            // 全局转场保护：另一扇门的转场还在进行中时不触发
            var controller = ServiceLocator.Get<DoorTransitionController>();
            if (controller != null && controller.IsTransitioning) return;

            // ── 钥匙检查：Locked_Key 状态自动验证玩家背包 ──
            if (_currentState == DoorState.Locked_Key && !string.IsNullOrEmpty(_requiredKeyID))
            {
                var inventory = ServiceLocator.Get<KeyInventory>();
                if (inventory != null && inventory.HasKey(_requiredKeyID))
                {
                    // 有钥匙 → 自动解锁
                    Debug.Log($"[Door] {gameObject.name}: 持有钥匙 '{_requiredKeyID}'，门自动解锁！");
                    SetState(DoorState.Open);
                    _hasLoggedMissingKey = false;
                }
                else
                {
                    // 没有钥匙 → 提示一次（避免 Stay 每帧刷屏）
                    if (!_hasLoggedMissingKey)
                    {
                        Debug.Log($"[Door] {gameObject.name}: 需要钥匙 '{_requiredKeyID}' 才能通过！");
                        _hasLoggedMissingKey = true;
                    }
                    return;
                }
            }

            if (_currentState != DoorState.Open) return;

            TryTransition();
        }

        private bool IsPlayerLayer(GameObject obj)
        {
            return (_playerLayer.value & (1 << obj.layer)) != 0;
        }

        // ──────────────────── Transition ────────────────────

        private void TryTransition()
        {
            if (_currentState != DoorState.Open) return;
            if (_targetRoom == null || _targetSpawnPoint == null) return;

            var controller = ServiceLocator.Get<DoorTransitionController>();
            if (controller == null)
            {
                Debug.LogError("[Door] DoorTransitionController not found in ServiceLocator!");
                return;
            }

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
