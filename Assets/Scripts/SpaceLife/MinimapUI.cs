
using UnityEngine;
using UnityEngine.UI;

namespace ProjectArk.SpaceLife
{
    public class MinimapUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject _minimapPanel;
        [SerializeField] private Text _currentRoomText;
        [SerializeField] private Image _currentRoomIcon;

        private void Start()
        {
            if (_minimapPanel != null)
                _minimapPanel.SetActive(false);

            if (RoomManager.Instance != null)
            {
                RoomManager.Instance.OnRoomChanged += OnRoomChanged;
                OnRoomChanged(RoomManager.Instance.CurrentRoom);
            }

            if (SpaceLifeManager.Instance != null)
            {
                SpaceLifeManager.Instance.OnEnterSpaceLife += OnEnterSpaceLife;
                SpaceLifeManager.Instance.OnExitSpaceLife += OnExitSpaceLife;
            }
        }

        private void OnEnterSpaceLife()
        {
            if (_minimapPanel != null)
                _minimapPanel.SetActive(true);
        }

        private void OnExitSpaceLife()
        {
            if (_minimapPanel != null)
                _minimapPanel.SetActive(false);
        }

        private void OnRoomChanged(Room room)
        {
            if (room == null) return;

            if (_currentRoomText != null)
            {
                _currentRoomText.text = room.RoomName;
            }

            if (_currentRoomIcon != null && room.Icon != null)
            {
                _currentRoomIcon.sprite = room.Icon;
                _currentRoomIcon.gameObject.SetActive(true);
            }
            else if (_currentRoomIcon != null)
            {
                _currentRoomIcon.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (RoomManager.Instance != null)
            {
                RoomManager.Instance.OnRoomChanged -= OnRoomChanged;
            }

            if (SpaceLifeManager.Instance != null)
            {
                SpaceLifeManager.Instance.OnEnterSpaceLife -= OnEnterSpaceLife;
                SpaceLifeManager.Instance.OnExitSpaceLife -= OnExitSpaceLife;
            }
        }
    }
}

