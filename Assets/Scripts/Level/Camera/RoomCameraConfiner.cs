using UnityEngine;
using Unity.Cinemachine;
using ProjectArk.Core;

namespace ProjectArk.Level
{
    /// <summary>
    /// Bridges the room system with Cinemachine's CinemachineConfiner2D.
    /// Subscribes to RoomManager.OnCurrentRoomChanged and updates the confiner's
    /// bounding shape to match the new room's bounds.
    /// 
    /// Attach to the same GameObject as the CinemachineCamera, or any persistent object.
    /// Drag the CinemachineConfiner2D component reference in the Inspector.
    /// </summary>
    public class RoomCameraConfiner : MonoBehaviour
    {
        // ──────────────────── Configuration ────────────────────

        [Header("References")]
        [Tooltip("The CinemachineConfiner2D component on the virtual camera.")]
        [SerializeField] private CinemachineConfiner2D _confiner;

        // ──────────────────── Lifecycle ────────────────────

        private RoomManager _roomManager;

        private void Start()
        {
            _roomManager = ServiceLocator.Get<RoomManager>();

            if (_roomManager == null)
            {
                Debug.LogError("[RoomCameraConfiner] RoomManager not found in ServiceLocator!");
                return;
            }

            if (_confiner == null)
            {
                Debug.LogError("[RoomCameraConfiner] CinemachineConfiner2D reference is not assigned!");
                return;
            }

            _roomManager.OnCurrentRoomChanged += HandleRoomChanged;

            // Apply initial room bounds if a room is already set
            if (_roomManager.CurrentRoom != null)
            {
                ApplyRoomBounds(_roomManager.CurrentRoom);
            }
        }

        private void OnDestroy()
        {
            if (_roomManager != null)
            {
                _roomManager.OnCurrentRoomChanged -= HandleRoomChanged;
            }
        }

        // ──────────────────── Event Handler ────────────────────

        private void HandleRoomChanged(Room newRoom)
        {
            if (newRoom == null) return;
            ApplyRoomBounds(newRoom);
        }

        // ──────────────────── Core Logic ────────────────────

        private void ApplyRoomBounds(Room room)
        {
            if (_confiner == null) return;

            var bounds = room.ConfinerBounds;
            if (bounds == null)
            {
                Debug.LogWarning($"[RoomCameraConfiner] Room '{room.RoomID}' has no ConfinerBounds assigned. Camera will be unconstrained.");
                return;
            }

            _confiner.BoundingShape2D = bounds;
            _confiner.InvalidateBoundingShapeCache();

            Debug.Log($"[RoomCameraConfiner] Updated confiner bounds to room: {room.RoomID}");
        }
    }
}
