using UnityEngine;
using Unity.Cinemachine;
using ProjectArk.Core;

namespace ProjectArk.Level
{
    /// <summary>
    /// Bridges the room system with Cinemachine's CinemachineConfiner2D.
    /// Hard confine is now an opt-in room policy rather than the default exploration camera behavior.
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

            if (_roomManager.CurrentRoom != null)
            {
                ApplyRoomBounds(_roomManager.CurrentRoom);
            }
            else
            {
                ClearConfiner();
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
            if (newRoom == null)
            {
                ClearConfiner();
                return;
            }

            ApplyRoomBounds(newRoom);
        }

        // ──────────────────── Core Logic ────────────────────

        private void ApplyRoomBounds(Room room)
        {
            if (_confiner == null || room == null)
            {
                return;
            }

            if (!room.UsesHardCameraConfine)
            {
                ClearConfiner();
                Debug.Log($"[RoomCameraConfiner] Room '{room.RoomID}' uses {room.CameraPolicy}. Hard confine cleared.");
                return;
            }

            var bounds = room.ConfinerBounds;
            if (bounds == null)
            {
                Debug.LogWarning($"[RoomCameraConfiner] Room '{room.RoomID}' is HardConfine but has no ConfinerBounds assigned. Camera will remain unconstrained.");
                ClearConfiner();
                return;
            }

            _confiner.enabled = true;
            _confiner.BoundingShape2D = bounds;
            _confiner.InvalidateBoundingShapeCache();

            Debug.Log($"[RoomCameraConfiner] Applied hard confiner bounds for room: {room.RoomID}");
        }

        private void ClearConfiner()
        {
            if (_confiner == null)
            {
                return;
            }

            _confiner.BoundingShape2D = null;
            _confiner.InvalidateBoundingShapeCache();
            _confiner.enabled = false;
        }
    }
}

