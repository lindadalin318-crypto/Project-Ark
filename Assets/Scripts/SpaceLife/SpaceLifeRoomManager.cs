using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using ProjectArk.Core;
using UnityEngine;

namespace ProjectArk.SpaceLife
{
    public class SpaceLifeRoomManager : MonoBehaviour
    {
        [Header("Rooms")]
        [SerializeField] private List<SpaceLifeRoom> _rooms = new List<SpaceLifeRoom>();
        [SerializeField] private SpaceLifeRoom _startingRoom;

        [Header("Camera")]
        [SerializeField] private Camera _roomCamera;
        [SerializeField] private float _cameraMoveSpeed = 5f;
        [SerializeField] private bool _smoothCameraTransition = true;

        private SpaceLifeRoom _currentRoom;
        private CancellationTokenSource _cameraMoveCts;

        public SpaceLifeRoom CurrentRoom => _currentRoom;
        public List<SpaceLifeRoom> AllRooms => _rooms;

        public event Action<SpaceLifeRoom> OnRoomChanged;

        private void Awake()
        {
            ServiceLocator.Register(this);
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
            SpaceLifeRoom[] foundRooms = FindObjectsByType<SpaceLifeRoom>(FindObjectsSortMode.None);
            _rooms.AddRange(foundRooms);
            Debug.Log($"[SpaceLifeRoomManager] Found {_rooms.Count} rooms");
        }

        public void SetCurrentRoom(SpaceLifeRoom room)
        {
            if (room == null || room == _currentRoom) return;

            _currentRoom = room;
            UpdateCameraTarget();
            OnRoomChanged?.Invoke(_currentRoom);

            Debug.Log($"[SpaceLifeRoomManager] Entered room: {_currentRoom.RoomName}");
        }

        private void UpdateCameraTarget()
        {
            if (_roomCamera == null || _currentRoom == null) return;

            if (_currentRoom.CameraTarget != null)
            {
                if (_smoothCameraTransition)
                {
                    CancelCameraMove();
                    _cameraMoveCts = new CancellationTokenSource();
                    SmoothMoveCameraAsync(_currentRoom.CameraTarget.position, _cameraMoveCts.Token).Forget();
                }
                else
                {
                    Vector3 targetPos = _currentRoom.CameraTarget.position;
                    targetPos.z = _roomCamera.transform.position.z;
                    _roomCamera.transform.position = targetPos;
                }
            }
        }

        private async UniTaskVoid SmoothMoveCameraAsync(Vector3 targetPosition, CancellationToken ct)
        {
            targetPosition.z = _roomCamera.transform.position.z;

            try
            {
                while (Vector3.Distance(_roomCamera.transform.position, targetPosition) > 0.01f)
                {
                    if (ct.IsCancellationRequested) return;

                    _roomCamera.transform.position = Vector3.Lerp(
                        _roomCamera.transform.position,
                        targetPosition,
                        _cameraMoveSpeed * Time.deltaTime
                    );
                    await UniTask.Yield(ct);
                }

                _roomCamera.transform.position = targetPosition;
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void CancelCameraMove()
        {
            if (_cameraMoveCts != null)
            {
                _cameraMoveCts.Cancel();
                _cameraMoveCts.Dispose();
                _cameraMoveCts = null;
            }
        }

        public SpaceLifeRoom GetRoomByName(string name)
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

        public SpaceLifeRoom GetRoomByType(SpaceLifeRoomType type)
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
            CancelCameraMove();
            ServiceLocator.Unregister(this);
        }
    }
}
