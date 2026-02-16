
using UnityEngine;

namespace ProjectArk.SpaceLife
{
    public class Room : MonoBehaviour
    {
        [Header("Room Info")]
        [SerializeField] private string _roomName = "房间";
        [SerializeField] private RoomType _roomType = RoomType.Generic;
        [SerializeField] private Sprite _roomIcon;

        [Header("Bounds")]
        [SerializeField] private Collider2D _roomBounds;
        [SerializeField] private Transform _cameraTarget;

        public string RoomName => _roomName;
        public RoomType Type => _roomType;
        public Sprite Icon => _roomIcon;
        public Collider2D Bounds => _roomBounds;
        public Transform CameraTarget => _cameraTarget;

        private void Awake()
        {
            if (_roomBounds == null)
                _roomBounds = GetComponent<Collider2D>();

            if (_cameraTarget == null)
                _cameraTarget = transform;
        }

        public bool IsPlayerInRoom()
        {
            if (_roomBounds == null) return false;

            PlayerController2D player = FindFirstObjectByType<PlayerController2D>();
            if (player == null) return false;

            return _roomBounds.bounds.Contains(player.transform.position);
        }

        private void OnDrawGizmosSelected()
        {
            if (_roomBounds != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireCube(_roomBounds.bounds.center, _roomBounds.bounds.size);
            }
        }
    }

    public enum RoomType
    {
        Generic,
        CommandCenter,
        Cockpit,
        StarChartRoom,
        MedicalBay,
        Engineering,
        Kitchen,
        Lounge,
        Bedroom,
        Storage
    }
}

