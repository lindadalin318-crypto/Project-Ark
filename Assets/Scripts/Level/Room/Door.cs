using UnityEngine;
using PrimeTween;
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

        [Header("Gate & Connection")]
        [Tooltip("此门的命名入口 ID（如 'left_1', 'boss_entrance'）。用于工具链连线和 Pacing 可视化。")]
        [SerializeField] private string _gateID;

        [Tooltip("此连接的语义类型。用于地图可视化和 Pacing 分析。")]
        [SerializeField] private ConnectionType _connectionType = ConnectionType.Progression;

        [Header("State")]
        [Tooltip("Initial state when the scene loads.")]
        [SerializeField] private DoorState _initialState = DoorState.Open;

        [Header("Transition")]
        [Tooltip("过渡仪式感等级。None=无演出，Standard=标准淡入淡出，Layer=层间过渡，Boss=Boss房间，Heavy=特殊重量级演出。")]
        [SerializeField] private TransitionCeremony _ceremony = TransitionCeremony.Standard;

        [Header("Lock")]
        [Tooltip("If set, this door requires a key to open. Used by Lock component / UI tooltip.")]
        [SerializeField] private string _requiredKeyID;

        [Header("Schedule (for Locked_Schedule doors)")]
        [Tooltip("Phase indices during which this door opens. Only relevant if initialState is Locked_Schedule.")]
        [SerializeField] private int[] _openDuringPhases;

        [Header("Visuals")]
        [Tooltip("SpriteRenderer for the door visual. If null, tries GetComponent.")]
        [SerializeField] private SpriteRenderer _spriteRenderer;

        [Tooltip("Color when door is open.")]
        [SerializeField] private Color _openColor = Color.green;

        [Tooltip("Color when door is locked (combat).")]
        [SerializeField] private Color _combatLockedColor = new Color(1f, 0.3f, 0.3f, 1f);

        [Tooltip("Color when door is locked (key).")]
        [SerializeField] private Color _keyLockedColor = new Color(1f, 0.8f, 0.2f, 1f);

        [Tooltip("Color when door is locked (ability).")]
        [SerializeField] private Color _abilityLockedColor = new Color(0.6f, 0.3f, 1f, 1f);

        [Tooltip("Color when door is locked (schedule).")]
        [SerializeField] private Color _scheduleLockedColor = new Color(0.3f, 0.7f, 1f, 1f);

        [Tooltip("Duration of color transition when state changes.")]
        [SerializeField] private float _colorTransitionDuration = 0.3f;

        [Header("Player Detection")]
        [Tooltip("Layer mask for the player ship.")]
        [SerializeField] private LayerMask _playerLayer;

        // ──────────────────── Runtime State ────────────────────

        private DoorState _currentState;
        private bool _playerInRange;
        private bool _isTransitioning;
        // Static cooldown shared across all Door instances.
        // After any transition completes, all doors ignore OnTriggerStay2D for a short window,
        // preventing the destination door from immediately re-teleporting the player back.
        private static float _globalTransitionCooldownUntil;

        // ──────────────────── Public Properties ────────────────────

        /// <summary> The room this door leads to. </summary>
        public Room TargetRoom => _targetRoom;

        /// <summary> Spawn point in the target room. </summary>
        public Transform TargetSpawnPoint => _targetSpawnPoint;

        /// <summary> Current door state. </summary>
        public DoorState CurrentState => _currentState;

        /// <summary> Required key ID for this door (empty = no key needed). </summary>
        public string RequiredKeyID => _requiredKeyID;

        /// <summary> 此门的命名入口 ID。用于工具链连线和 Pacing 可视化。 </summary>
        public string GateID => _gateID;

        /// <summary> 此连接的语义类型。 </summary>
        public ConnectionType ConnectionType => _connectionType;

        /// <summary> 过渡仪式感等级。 </summary>
        public TransitionCeremony Ceremony => _ceremony;

        // ──────────────────── Lifecycle ────────────────────

        private void Awake()
        {
            _currentState = _initialState;

            // Auto-find SpriteRenderer if not assigned
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }

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

            // Apply initial visual
            ApplyVisualForState(_currentState, immediate: true);
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
            // Enter always triggers immediately (player actively walked in)
            TryAutoTransition();
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            // 当玩家被传送到门的 Trigger 内时，OnTriggerEnter2D 在转场期间触发会被拒绝，
            // 转场结束后 Stay 会重新尝试，避免"卡门"问题
            if (!_playerInRange) return;
            if (!IsPlayerLayer(other.gameObject)) return;
            // Cooldown guard: skip Stay triggers for a short window after a transition completes,
            // preventing the reverse door from immediately re-teleporting the player back.
            if (Time.unscaledTime < _globalTransitionCooldownUntil) return;
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
            controller.TransitionThroughDoor(this, () =>
            {
                _isTransitioning = false;
                // Set a global cooldown so OnTriggerStay2D on ANY door (especially the reverse door
                // at the destination) won't fire immediately if the spawn point lands inside its collider.
                _globalTransitionCooldownUntil = Time.unscaledTime + 1f;
            });
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

            // Visual feedback
            ApplyVisualForState(newState, immediate: false);
        }

        /// <summary>
        /// Reset the global transition cooldown. Call this when the scene is unloaded
        /// (e.g., from DoorTransitionController.OnDestroy) to prevent cross-scene pollution.
        /// </summary>
        public static void ResetGlobalTransitionCooldown()
        {
            _globalTransitionCooldownUntil = 0f;
        }

        // ──────────────────── Visual Feedback ────────────────────

        private void ApplyVisualForState(DoorState state, bool immediate)
        {
            if (_spriteRenderer == null) return;

            Color targetColor = GetColorForState(state);

            if (immediate)
            {
                _spriteRenderer.color = targetColor;
            }
            else
            {
                Color currentColor = _spriteRenderer.color;
                var sr = _spriteRenderer; // capture for closure

                _ = Tween.Custom(0f, 1f, _colorTransitionDuration,
                    onValueChange: t =>
                    {
                        if (sr != null)
                            sr.color = Color.Lerp(currentColor, targetColor, t);
                    },
                    ease: Ease.InOutSine);
            }
        }

        private Color GetColorForState(DoorState state)
        {
            return state switch
            {
                DoorState.Open => _openColor,
                DoorState.Locked_Combat => _combatLockedColor,
                DoorState.Locked_Key => _keyLockedColor,
                DoorState.Locked_Ability => _abilityLockedColor,
                DoorState.Locked_Schedule => _scheduleLockedColor,
                _ => Color.white
            };
        }
    }
}
