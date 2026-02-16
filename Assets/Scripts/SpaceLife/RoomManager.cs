
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectArk.SpaceLife
{
    public class RoomManager : MonoBehaviour
    {
        public static RoomManager Instance { get; private set; }

        [Header("Rooms")]
        [SerializeField] private List<Room> _rooms = new List<Room>();
        [SerializeField] private Room _startingRoom;

        [Header("Camera")]
        [SerializeField] private Camera _roomCamera;
        [SerializeField] private float _cameraMoveSpeed = 5f;
        [SerializeField] private bool _smoothCameraTransition = true;

        private Room _currentRoom;

        public Room CurrentRoom => _currentRoom;
        public List<Room> AllRooms => _rooms;

        public event Action<Room> OnRoomChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (_rooms.Count == 0)
            {
                FindAllRooms();
            }

            if (_startingRoom != null)
            {
                SetCurrentRoom(_startingRoom);
            }
        }

        private void FindAllRooms()
        {
            Room[] foundRooms = FindObjectsByType<Room>(FindObjectsSortMode.None);
            _rooms.AddRange(foundRooms);
            Debug.Log($"[RoomManager] Found {_rooms.Count} rooms");
        }

        public void SetCurrentRoom(Room room)
        {
            if (room == null || room == _currentRoom) return;

            _currentRoom = room;
            UpdateCameraTarget();
            OnRoomChanged?.Invoke(_currentRoom);

            Debug.Log($"[RoomManager] Entered room: {_currentRoom.RoomName}");
        }

        private void UpdateCameraTarget()
        {
            if (_roomCamera == null || _currentRoom == null) return;

            if (_currentRoom.CameraTarget != null)
            {
                if (_smoothCameraTransition)
                {
                    StartCoroutine(SmoothMoveCameraCoroutine(_currentRoom.CameraTarget.position));
                }
                else
                {
                    Vector3 targetPos = _currentRoom.CameraTarget.position;
                    targetPos.z = _roomCamera.transform.position.z;
                    _roomCamera.transform.position = targetPos;
                }
            }
        }

        private System.Collections.IEnumerator SmoothMoveCameraCoroutine(Vector3 targetPosition)
        {
            targetPosition.z = _roomCamera.transform.position.z;

            while (Vector3.Distance(_roomCamera.transform.position, targetPosition) > 0.01f)
            {
                _roomCamera.transform.position = Vector3.Lerp(
                    _roomCamera.transform.position,
                    targetPosition,
                    _cameraMoveSpeed * Time.deltaTime
                );
                yield return null;
            }

            _roomCamera.transform.position = targetPosition;
        }

        public Room GetRoomByName(string name)
        {
            foreach (var room in _rooms)
            {
                if (room.RoomName == name)
                {
                    return room;
                }
            }
            return null;
        }

        public Room GetRoomByType(RoomType type)
        {
            foreach (var room in _rooms)
            {
                if (room.Type == type)
                {
                    return room;
                }
            }
            return null;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}

