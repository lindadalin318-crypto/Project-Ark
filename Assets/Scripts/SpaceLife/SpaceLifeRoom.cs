using System.Collections.Generic;
using ProjectArk.Core;
using UnityEngine;

namespace ProjectArk.SpaceLife
{
    public class SpaceLifeRoom : MonoBehaviour
    {
        [Header("Room Info")]
        [SerializeField] private string _roomName = "房间";
        [SerializeField] private SpaceLifeRoomType _roomType = SpaceLifeRoomType.Generic;
        [SerializeField] private Sprite _roomIcon;

        [Header("Bounds")]
        [SerializeField] private Collider2D _roomBounds;
        [SerializeField] private Transform _cameraTarget;

        [Header("Connections")]
        [SerializeField] private List<SpaceLifeDoor> _doors = new List<SpaceLifeDoor>();

        private PlayerController2D _cachedPlayer;

        public string RoomName => _roomName;
        public SpaceLifeRoomType Type => _roomType;
        public Sprite Icon => _roomIcon;
        public Collider2D Bounds => _roomBounds;
        public Transform CameraTarget => _cameraTarget;
        public List<SpaceLifeDoor> Doors => _doors;

        private SpaceLifeRoomManager _roomManager;

        private void Awake()
        {
            if (_roomBounds == null)
                _roomBounds = GetComponent<Collider2D>();

            if (_cameraTarget == null)
                _cameraTarget = transform;
        }

        private void OnEnable()
        {
            _roomManager = ServiceLocator.Get<SpaceLifeRoomManager>();
            if (_roomManager != null)
            {
                _roomManager.RegisterRoom(this);
            }
        }

        private void Start()
        {
            _cachedPlayer = ServiceLocator.Get<PlayerController2D>();
        }

        private void OnDisable()
        {
            if (_roomManager != null)
            {
                _roomManager.UnregisterRoom(this);
            }
        }

        public bool IsPlayerInRoom()
        {
            if (_roomBounds == null || _cachedPlayer == null) return false;

            return _roomBounds.bounds.Contains(_cachedPlayer.transform.position);
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
}
