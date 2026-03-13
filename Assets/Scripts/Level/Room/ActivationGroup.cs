using UnityEngine;
using ProjectArk.Core;

namespace ProjectArk.Level
{
    /// <summary>
    /// Groups GameObjects that should be activated/deactivated together
    /// based on whether the player is in the owning Room.
    /// 
    /// Minishoot's ActivationManager equivalent — extends the concept beyond
    /// just enemies to include decorations, lights, interactive objects,
    /// particle effects, etc. for performance optimization.
    /// 
    /// Place as a child of a Room. Members are either direct children of this
    /// GameObject, or explicitly assigned via the _members array.
    /// </summary>
    public class ActivationGroup : MonoBehaviour
    {
        // ──────────────────── Configuration ────────────────────

        [Header("Members")]
        [Tooltip("GameObjects managed by this group. If empty, all direct children are used.")]
        [SerializeField] private GameObject[] _members;

        [Header("Behavior")]
        [Tooltip("If true, group starts deactivated and only activates when the player enters the room.")]
        [SerializeField] private bool _startDeactivated = true;

        [Tooltip("If true, members stay active after first activation (never deactivate again). " +
                 "Useful for one-time reveals.")]
        [SerializeField] private bool _persistOnceActivated;

        // ──────────────────── Runtime State ────────────────────

        private bool _hasBeenActivated;
        private RoomManager _roomManager;
        private Room _parentRoom;

        // ──────────────────── Lifecycle ────────────────────

        private void Start()
        {
            _parentRoom = GetComponentInParent<Room>();
            if (_parentRoom == null)
            {
                Debug.LogWarning($"[ActivationGroup] {gameObject.name}: Not a child of a Room. " +
                                  "Will not respond to room enter/exit events.");
                return;
            }

            _roomManager = ServiceLocator.Get<RoomManager>();
            if (_roomManager != null)
            {
                _roomManager.OnCurrentRoomChanged += HandleRoomChanged;

                // Apply initial state
                if (_startDeactivated)
                {
                    SetMembersActive(false);
                }

                // If player is already in this room, activate
                if (_roomManager.CurrentRoom == _parentRoom)
                {
                    SetMembersActive(true);
                    _hasBeenActivated = true;
                }
            }
        }

        private void OnDestroy()
        {
            if (_roomManager != null)
            {
                _roomManager.OnCurrentRoomChanged -= HandleRoomChanged;
            }
        }

        // ──────────────────── Event Handlers ────────────────────

        private void HandleRoomChanged(Room newRoom)
        {
            if (_parentRoom == null) return;

            bool isInRoom = newRoom == _parentRoom;

            if (isInRoom)
            {
                SetMembersActive(true);
                _hasBeenActivated = true;
            }
            else if (!_persistOnceActivated || !_hasBeenActivated)
            {
                SetMembersActive(false);
            }
        }

        // ──────────────────── Member Management ────────────────────

        private void SetMembersActive(bool active)
        {
            if (_members != null && _members.Length > 0)
            {
                foreach (var member in _members)
                {
                    if (member != null) member.SetActive(active);
                }
            }
            else
            {
                // Default: use all direct children
                for (int i = 0; i < transform.childCount; i++)
                {
                    transform.GetChild(i).gameObject.SetActive(active);
                }
            }
        }

        // ──────────────────── Public API ────────────────────

        /// <summary>
        /// Force activate all members. Called externally for scripted events.
        /// </summary>
        public void ForceActivate()
        {
            SetMembersActive(true);
            _hasBeenActivated = true;
        }

        /// <summary>
        /// Force deactivate all members.
        /// </summary>
        public void ForceDeactivate()
        {
            SetMembersActive(false);
        }
    }
}
