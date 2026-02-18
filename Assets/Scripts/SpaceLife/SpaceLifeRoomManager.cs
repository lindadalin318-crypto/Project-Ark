using System;
using System.Collections.Generic;
using PrimeTween;
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
        private Tween _cameraTween;

        public SpaceLifeRoom CurrentRoom => _currentRoom;
        public List<SpaceLifeRoom> AllRooms => _rooms;

        public event Action<SpaceLifeRoom> OnRoomChanged;

        private void Awake()
        {
            ServiceLocator.Register(this);
        }

        private void Start()
        {
            if (_startingRoom != null)
            {
                SetCurrentRoom(_startingRoom);
            }
        }

        /// <summary>
        /// Called by SpaceLifeRoom.OnEnable to self-register into the room list.
        /// </summary>
        public void RegisterRoom(SpaceLifeRoom room)
        {
            if (room != null && !_rooms.Contains(room))
            {
                _rooms.Add(room);
            }
        }

        /// <summary>
        /// Called by SpaceLifeRoom.OnDisable to self-unregister from the room list.
        /// </summary>
        public void UnregisterRoom(SpaceLifeRoom room)
        {
            _rooms.Remove(room);
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
                Vector3 targetPos = _currentRoom.CameraTarget.position;
                targetPos.z = _roomCamera.transform.position.z;

                if (_smoothCameraTransition)
                {
                    if (_cameraTween.isAlive) _cameraTween.Stop();

                    float distance = Vector3.Distance(_roomCamera.transform.position, targetPos);
                    float duration = Mathf.Max(0.1f, distance / _cameraMoveSpeed);

                    _cameraTween = Tween.Position(_roomCamera.transform, targetPos, duration,
                        ease: Ease.InOutQuad);
                }
                else
                {
                    _roomCamera.transform.position = targetPos;
                }
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
            if (_cameraTween.isAlive) _cameraTween.Stop();
            OnRoomChanged = null;
            ServiceLocator.Unregister(this);
        }
    }
}
