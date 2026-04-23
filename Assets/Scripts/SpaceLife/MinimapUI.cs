using System.Collections.Generic;
using ProjectArk.Core;
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

        [Header("Room Navigation")]
        [SerializeField] private Transform _roomButtonsContainer;
        [SerializeField] private GameObject _roomButtonPrefab;
        [SerializeField] private Color _currentRoomColor = Color.yellow;
        [SerializeField] private Color _adjacentRoomColor = Color.white;
        [SerializeField] private Color _disabledRoomColor = Color.gray;

        private SpaceLifeRoomManager _roomManager;
        private SpaceLifeManager _spaceLifeManager;
        private CanvasGroup _minimapCanvasGroup;
        private readonly List<GameObject> _roomButtons = new List<GameObject>();

        private void Awake()
        {
            // uGUI 面板显隐统一走 CanvasGroup（见 Implement_rules.md §12.6 R5 + CLAUDE.md 陷阱表）
            // 禁止 SetActive(false)：会推迟 Awake、可能被序列化为 inactive，导致首次 Open 时面板被 Awake 内逻辑瞬间关闭。
            _minimapCanvasGroup = EnsureCanvasGroup(_minimapPanel);
            SetPanelVisible(false);
        }

        private void Start()
        {
            _roomManager = ServiceLocator.Get<SpaceLifeRoomManager>();
            _spaceLifeManager = ServiceLocator.Get<SpaceLifeManager>();

            if (_roomManager != null)
            {
                _roomManager.OnRoomChanged += OnRoomChanged;
                OnRoomChanged(_roomManager.CurrentRoom);
            }

            if (_spaceLifeManager != null)
            {
                _spaceLifeManager.OnEnterSpaceLife += OnEnterSpaceLife;
                _spaceLifeManager.OnExitSpaceLife += OnExitSpaceLife;
            }
        }

        private void OnEnterSpaceLife()
        {
            SetPanelVisible(true);
            RefreshRoomButtons();
        }

        private void OnExitSpaceLife()
        {
            SetPanelVisible(false);
        }

        private void SetPanelVisible(bool visible)
        {
            if (_minimapCanvasGroup == null)
            {
                return;
            }

            _minimapCanvasGroup.alpha = visible ? 1f : 0f;
            _minimapCanvasGroup.interactable = visible;
            _minimapCanvasGroup.blocksRaycasts = visible;
        }

        private static CanvasGroup EnsureCanvasGroup(GameObject panel)
        {
            if (panel == null)
            {
                Debug.LogError("[MinimapUI] _minimapPanel is not assigned. Minimap show/hide will silently no-op.");
                return null;
            }

            CanvasGroup group = panel.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = panel.AddComponent<CanvasGroup>();
            }

            return group;
        }

        private void OnRoomChanged(SpaceLifeRoom room)
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

            RefreshRoomButtons();
        }

        private void RefreshRoomButtons()
        {
            ClearRoomButtons();

            if (_roomManager == null || _roomButtonsContainer == null || _roomButtonPrefab == null)
                return;

            SpaceLifeRoom currentRoom = _roomManager.CurrentRoom;
            if (currentRoom == null) return;

            HashSet<SpaceLifeRoom> adjacentRooms = new HashSet<SpaceLifeRoom>();
            foreach (var door in currentRoom.Doors)
            {
                if (door.ConnectedRoom != null)
                {
                    adjacentRooms.Add(door.ConnectedRoom);
                }
            }

            CreateRoomButton(currentRoom, true);

            foreach (var adjacentRoom in adjacentRooms)
            {
                CreateRoomButton(adjacentRoom, false);
            }
        }

        private void CreateRoomButton(SpaceLifeRoom room, bool isCurrentRoom)
        {
            GameObject buttonObj = Instantiate(_roomButtonPrefab, _roomButtonsContainer);
            _roomButtons.Add(buttonObj);

            Text buttonText = buttonObj.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = room.RoomName;
                buttonText.color = isCurrentRoom ? _currentRoomColor : _adjacentRoomColor;
            }

            Image buttonImage = buttonObj.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = isCurrentRoom ? _currentRoomColor : _adjacentRoomColor;
            }

            Button button = buttonObj.GetComponent<Button>();
            if (button != null && !isCurrentRoom)
            {
                SpaceLifeRoom targetRoom = room;
                button.onClick.AddListener(() => NavigateToRoom(targetRoom));
            }
        }

        private void NavigateToRoom(SpaceLifeRoom targetRoom)
        {
            if (_roomManager == null || targetRoom == null)
                return;

            Debug.Log($"[MinimapUI] Navigate to room: {targetRoom.RoomName}");
        }

        private void ClearRoomButtons()
        {
            foreach (var button in _roomButtons)
            {
                if (button != null)
                    Destroy(button);
            }
            _roomButtons.Clear();
        }

        private void OnDestroy()
        {
            if (_roomManager != null)
            {
                _roomManager.OnRoomChanged -= OnRoomChanged;
            }

            if (_spaceLifeManager != null)
            {
                _spaceLifeManager.OnEnterSpaceLife -= OnEnterSpaceLife;
                _spaceLifeManager.OnExitSpaceLife -= OnExitSpaceLife;
            }

            ClearRoomButtons();
        }
    }
}
