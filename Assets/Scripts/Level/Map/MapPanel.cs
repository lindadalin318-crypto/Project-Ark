using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using ProjectArk.Core;

namespace ProjectArk.Level
{
    /// <summary>
    /// Full-screen map UI panel. Toggled with M key (ToggleMap action).
    /// Shows all rooms on the current floor as colored rectangles, connections as lines,
    /// fog for unvisited rooms, and a player icon on the current room.
    /// Supports panning via ScrollRect and floor tab switching.
    /// 
    /// Place on a Canvas under a persistent UI root.
    /// </summary>
    public class MapPanel : MonoBehaviour
    {
        // ──────────────────── Configuration ────────────────────

        [Header("Input")]
        [SerializeField] private InputActionAsset _inputActions;

        [Header("Layout")]
        [Tooltip("The ScrollRect container for panning/zooming the map.")]
        [SerializeField] private ScrollRect _scrollRect;

        [Tooltip("Content transform inside the ScrollRect where room widgets are placed.")]
        [SerializeField] private RectTransform _mapContent;

        [Tooltip("Container for the floor tab buttons.")]
        [SerializeField] private RectTransform _floorTabContainer;

        [Header("Prefabs")]
        [Tooltip("Prefab for a single room node widget.")]
        [SerializeField] private MapRoomWidget _roomWidgetPrefab;

        [Tooltip("Prefab for a connection line between rooms.")]
        [SerializeField] private MapConnectionLine _connectionLinePrefab;

        [Tooltip("Prefab for a floor tab button.")]
        [SerializeField] private Button _floorTabPrefab;

        [Header("Player Icon")]
        [Tooltip("Image showing the player's current position on the map.")]
        [SerializeField] private RectTransform _playerIcon;

        [Header("Zoom")]
        [Tooltip("Scale of world-space coordinates to map UI coordinates.")]
        [SerializeField] private float _worldToMapScale = 5f;

        [Tooltip("Minimum zoom level.")]
        [SerializeField] private float _minZoom = 0.5f;

        [Tooltip("Maximum zoom level.")]
        [SerializeField] private float _maxZoom = 3f;

        [Tooltip("Zoom speed multiplier for scroll wheel.")]
        [SerializeField] private float _zoomSpeed = 0.1f;

        // ──────────────────── Runtime State ────────────────────

        private InputAction _toggleMapAction;
        private InputAction _scrollAction;
        private MinimapManager _minimapManager;

        private readonly Dictionary<string, MapRoomWidget> _widgetLookup = new();
        private readonly List<MapConnectionLine> _connectionLines = new();
        private readonly List<Button> _floorTabs = new();

        private int _displayedFloor;
        private float _currentZoom = 1f;
        private bool _isOpen;

        // ──────────────────── Lifecycle ────────────────────

        private void Awake()
        {
            if (_inputActions != null)
            {
                var shipMap = _inputActions.FindActionMap("Ship");
                _toggleMapAction = shipMap?.FindAction("ToggleMap");

                var uiMap = _inputActions.FindActionMap("UI");
                _scrollAction = uiMap?.FindAction("ScrollWheel");
            }

            gameObject.SetActive(false);
        }

        private void Start()
        {
            _minimapManager = ServiceLocator.Get<MinimapManager>();

            if (_minimapManager != null)
            {
                _minimapManager.OnMapDataChanged += OnMapDataChanged;
            }
        }

        private void OnEnable()
        {
            if (_toggleMapAction != null)
                _toggleMapAction.performed += OnToggleMapPerformed;

            if (_scrollAction != null)
                _scrollAction.performed += OnScrollPerformed;

            LevelEvents.OnFloorChanged += HandleFloorChanged;
        }

        private void OnDisable()
        {
            if (_toggleMapAction != null)
                _toggleMapAction.performed -= OnToggleMapPerformed;

            if (_scrollAction != null)
                _scrollAction.performed -= OnScrollPerformed;

            LevelEvents.OnFloorChanged -= HandleFloorChanged;
        }

        private void OnDestroy()
        {
            if (_minimapManager != null)
                _minimapManager.OnMapDataChanged -= OnMapDataChanged;
        }

        // ──────────────────── Input Handlers ────────────────────

        private void OnToggleMapPerformed(InputAction.CallbackContext ctx)
        {
            if (_isOpen)
                Close();
            else
                Open();
        }

        private void OnScrollPerformed(InputAction.CallbackContext ctx)
        {
            if (!_isOpen) return;

            Vector2 scroll = ctx.ReadValue<Vector2>();
            float delta = scroll.y * _zoomSpeed;
            _currentZoom = Mathf.Clamp(_currentZoom + delta, _minZoom, _maxZoom);

            if (_mapContent != null)
                _mapContent.localScale = Vector3.one * _currentZoom;
        }

        // ──────────────────── Public API ────────────────────

        /// <summary> Open the full-screen map. </summary>
        public void Open()
        {
            if (_minimapManager == null)
                _minimapManager = ServiceLocator.Get<MinimapManager>();

            _isOpen = true;
            gameObject.SetActive(true);

            _displayedFloor = _minimapManager?.CurrentFloor ?? 0;
            RebuildMap();
            RebuildFloorTabs();
            CenterOnCurrentRoom();
        }

        /// <summary> Close the full-screen map. </summary>
        public void Close()
        {
            _isOpen = false;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Switch the displayed floor and rebuild the map.
        /// </summary>
        public void ShowFloor(int floor)
        {
            _displayedFloor = floor;
            RebuildMap();
            UpdateFloorTabHighlights();
        }

        // ──────────────────── Map Building ────────────────────

        private void RebuildMap()
        {
            ClearMap();

            if (_minimapManager == null) return;

            // Get rooms for current floor
            var rooms = _minimapManager.GetRoomNodes(_displayedFloor);
            var connections = _minimapManager.GetConnections(_displayedFloor);

            // Draw connection lines first (behind room widgets)
            foreach (var conn in connections)
            {
                DrawConnection(conn);
            }

            // Draw room widgets
            foreach (var roomData in rooms)
            {
                CreateRoomWidget(roomData);
            }

            // Update player icon
            UpdatePlayerIcon();
        }

        private void CreateRoomWidget(MapRoomData data)
        {
            if (_roomWidgetPrefab == null || _mapContent == null) return;

            var widget = Instantiate(_roomWidgetPrefab, _mapContent);
            widget.Setup(data);

            // Position based on world coordinates scaled to map space
            var rt = widget.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = WorldToMap(data.WorldCenter);
                rt.sizeDelta = data.WorldSize * _worldToMapScale;
            }

            _widgetLookup[data.RoomID] = widget;
        }

        private void DrawConnection(MapConnection conn)
        {
            if (_connectionLinePrefab == null || _mapContent == null) return;

            var fromData = _minimapManager.GetRoomData(conn.FromRoomID);
            var toData = _minimapManager.GetRoomData(conn.ToRoomID);
            if (fromData == null || toData == null) return;

            var line = Instantiate(_connectionLinePrefab, _mapContent);
            line.transform.SetAsFirstSibling(); // Lines behind widgets

            Vector2 fromPos = WorldToMap(fromData.Value.WorldCenter);
            Vector2 toPos = WorldToMap(toData.Value.WorldCenter);
            line.Setup(fromPos, toPos, conn.IsLayerTransition);

            _connectionLines.Add(line);
        }

        private void ClearMap()
        {
            foreach (var kvp in _widgetLookup)
            {
                if (kvp.Value != null)
                    Destroy(kvp.Value.gameObject);
            }
            _widgetLookup.Clear();

            foreach (var line in _connectionLines)
            {
                if (line != null)
                    Destroy(line.gameObject);
            }
            _connectionLines.Clear();
        }

        // ──────────────────── Floor Tabs ────────────────────

        private void RebuildFloorTabs()
        {
            ClearFloorTabs();

            if (_minimapManager == null || _floorTabPrefab == null || _floorTabContainer == null) return;

            var floors = _minimapManager.GetFloorsDiscovered();
            if (floors.Count <= 1) return; // Don't show tabs for single floor

            foreach (int floor in floors)
            {
                var tab = Instantiate(_floorTabPrefab, _floorTabContainer);
                int floorCapture = floor; // Closure capture
                tab.onClick.AddListener(() => ShowFloor(floorCapture));

                var label = tab.GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    label.text = GetFloorLabel(floor);
                }

                _floorTabs.Add(tab);
            }

            UpdateFloorTabHighlights();
        }

        private void ClearFloorTabs()
        {
            foreach (var tab in _floorTabs)
            {
                if (tab != null)
                    Destroy(tab.gameObject);
            }
            _floorTabs.Clear();
        }

        private void UpdateFloorTabHighlights()
        {
            if (_minimapManager == null) return;

            var floors = _minimapManager.GetFloorsDiscovered();
            for (int i = 0; i < _floorTabs.Count && i < floors.Count; i++)
            {
                var tab = _floorTabs[i];
                bool isCurrentFloor = floors[i] == _displayedFloor;

                // Highlight current floor tab
                var colors = tab.colors;
                colors.normalColor = isCurrentFloor
                    ? new Color(0.3f, 0.7f, 1f, 1f)
                    : new Color(0.25f, 0.25f, 0.3f, 1f);
                tab.colors = colors;
            }
        }

        // ──────────────────── Player Icon ────────────────────

        private void UpdatePlayerIcon()
        {
            if (_playerIcon == null || _minimapManager == null) return;

            string currentID = _minimapManager.CurrentRoomID;
            if (string.IsNullOrEmpty(currentID)) return;

            var roomData = _minimapManager.GetRoomData(currentID);
            if (roomData == null) return;

            bool onDisplayedFloor = roomData.Value.FloorLevel == _displayedFloor;
            _playerIcon.gameObject.SetActive(onDisplayedFloor);

            if (onDisplayedFloor)
            {
                _playerIcon.anchoredPosition = WorldToMap(roomData.Value.WorldCenter);
            }
        }

        private void CenterOnCurrentRoom()
        {
            if (_scrollRect == null || _minimapManager == null || _mapContent == null) return;

            string currentID = _minimapManager.CurrentRoomID;
            if (string.IsNullOrEmpty(currentID)) return;

            var roomData = _minimapManager.GetRoomData(currentID);
            if (roomData == null) return;

            // Calculate normalized position to center on the current room
            Vector2 mapPos = WorldToMap(roomData.Value.WorldCenter);
            Vector2 contentSize = _mapContent.rect.size * _currentZoom;
            Vector2 viewportSize = _scrollRect.viewport.rect.size;

            if (contentSize.x > viewportSize.x)
            {
                float normalizedX = Mathf.Clamp01((mapPos.x + contentSize.x * 0.5f) / contentSize.x);
                _scrollRect.horizontalNormalizedPosition = normalizedX;
            }

            if (contentSize.y > viewportSize.y)
            {
                float normalizedY = Mathf.Clamp01((mapPos.y + contentSize.y * 0.5f) / contentSize.y);
                _scrollRect.verticalNormalizedPosition = normalizedY;
            }
        }

        // ──────────────────── Event Handlers ────────────────────

        private void OnMapDataChanged()
        {
            if (_isOpen)
            {
                RebuildMap();
            }
        }

        private void HandleFloorChanged(int newFloor)
        {
            // Auto-switch to new floor when map is open
            if (_isOpen)
            {
                ShowFloor(newFloor);
            }
        }

        // ──────────────────── Utility ────────────────────

        private Vector2 WorldToMap(Vector2 worldPos)
        {
            return worldPos * _worldToMapScale;
        }

        private static string GetFloorLabel(int floor)
        {
            return floor switch
            {
                0 => "Surface (F0)",
                > 0 => $"Upper (F{floor})",
                _ => $"Underground (F{floor})"
            };
        }
    }
}
