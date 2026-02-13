using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectArk.Core;

namespace ProjectArk.Level
{
    /// <summary>
    /// Corner minimap overlay (HUD element). Shows a small window centered on
    /// the current room with only nearby rooms visible.
    /// Auto-scrolls as player moves between rooms. Can be hidden via settings.
    /// 
    /// Place on a UI Canvas anchored to a screen corner.
    /// </summary>
    public class MinimapHUD : MonoBehaviour
    {
        // ──────────────────── Configuration ────────────────────

        [Header("Layout")]
        [Tooltip("Container transform for the minimap room widgets.")]
        [SerializeField] private RectTransform _content;

        [Tooltip("Background image for the minimap border.")]
        [SerializeField] private Image _background;

        [Header("Scale")]
        [Tooltip("World-to-minimap scale factor (smaller = more rooms visible).")]
        [SerializeField] private float _worldToMinimapScale = 2f;

        [Tooltip("Maximum visible radius from the current room (world units).")]
        [SerializeField] private float _visibleRadius = 30f;

        [Header("Floor Label")]
        [Tooltip("Text label showing the current floor.")]
        [SerializeField] private TMP_Text _floorLabel;

        [Header("Prefabs")]
        [Tooltip("Prefab for a minimap room widget (smaller version).")]
        [SerializeField] private MapRoomWidget _miniRoomWidgetPrefab;

        [Tooltip("Prefab for a minimap connection line.")]
        [SerializeField] private MapConnectionLine _miniConnectionLinePrefab;

        // ──────────────────── Runtime State ────────────────────

        private MinimapManager _minimapManager;
        private readonly Dictionary<string, MapRoomWidget> _widgets = new();
        private readonly List<MapConnectionLine> _lines = new();
        private bool _isVisible = true;

        // ──────────────────── Lifecycle ────────────────────

        private void Start()
        {
            _minimapManager = ServiceLocator.Get<MinimapManager>();

            if (_minimapManager != null)
            {
                _minimapManager.OnMapDataChanged += Rebuild;
            }

            Rebuild();
        }

        private void OnEnable()
        {
            LevelEvents.OnFloorChanged += HandleFloorChanged;
            LevelEvents.OnRoomEntered += HandleRoomEntered;
        }

        private void OnDisable()
        {
            LevelEvents.OnFloorChanged -= HandleFloorChanged;
            LevelEvents.OnRoomEntered -= HandleRoomEntered;
        }

        private void OnDestroy()
        {
            if (_minimapManager != null)
                _minimapManager.OnMapDataChanged -= Rebuild;
        }

        // ──────────────────── Public API ────────────────────

        /// <summary> Toggle minimap visibility. </summary>
        public void SetVisible(bool visible)
        {
            _isVisible = visible;
            gameObject.SetActive(visible);
        }

        // ──────────────────── Rebuild ────────────────────

        private void Rebuild()
        {
            Clear();

            if (_minimapManager == null || _content == null) return;

            int floor = _minimapManager.CurrentFloor;
            string currentID = _minimapManager.CurrentRoomID;

            // Update floor label
            if (_floorLabel != null)
            {
                _floorLabel.text = GetFloorLabel(floor);
            }

            var currentData = _minimapManager.GetRoomData(currentID);
            Vector2 center = currentData?.WorldCenter ?? Vector2.zero;

            // Get rooms on this floor
            var rooms = _minimapManager.GetRoomNodes(floor);

            // Draw connections first (behind rooms)
            var connections = _minimapManager.GetConnections(floor);
            foreach (var conn in connections)
            {
                DrawConnection(conn, center);
            }

            // Draw room widgets (only nearby rooms)
            foreach (var roomData in rooms)
            {
                float dist = Vector2.Distance(roomData.WorldCenter, center);
                if (dist > _visibleRadius) continue;

                CreateMiniWidget(roomData, center);
            }
        }

        private void CreateMiniWidget(MapRoomData data, Vector2 mapCenter)
        {
            if (_miniRoomWidgetPrefab == null) return;

            var widget = Instantiate(_miniRoomWidgetPrefab, _content);
            widget.Setup(data);

            var rt = widget.GetComponent<RectTransform>();
            if (rt != null)
            {
                Vector2 offset = (data.WorldCenter - mapCenter) * _worldToMinimapScale;
                rt.anchoredPosition = offset;
                rt.sizeDelta = data.WorldSize * _worldToMinimapScale * 0.5f;
            }

            _widgets[data.RoomID] = widget;
        }

        private void DrawConnection(MapConnection conn, Vector2 mapCenter)
        {
            if (_miniConnectionLinePrefab == null || _content == null) return;

            var fromData = _minimapManager.GetRoomData(conn.FromRoomID);
            var toData = _minimapManager.GetRoomData(conn.ToRoomID);
            if (fromData == null || toData == null) return;

            // Only draw if at least one endpoint is within visible radius
            float fromDist = Vector2.Distance(fromData.Value.WorldCenter, mapCenter);
            float toDist = Vector2.Distance(toData.Value.WorldCenter, mapCenter);
            if (fromDist > _visibleRadius && toDist > _visibleRadius) return;

            var line = Instantiate(_miniConnectionLinePrefab, _content);
            line.transform.SetAsFirstSibling();

            Vector2 fromPos = (fromData.Value.WorldCenter - mapCenter) * _worldToMinimapScale;
            Vector2 toPos = (toData.Value.WorldCenter - mapCenter) * _worldToMinimapScale;
            line.Setup(fromPos, toPos, conn.IsLayerTransition);

            _lines.Add(line);
        }

        private void Clear()
        {
            foreach (var kvp in _widgets)
            {
                if (kvp.Value != null)
                    Destroy(kvp.Value.gameObject);
            }
            _widgets.Clear();

            foreach (var line in _lines)
            {
                if (line != null)
                    Destroy(line.gameObject);
            }
            _lines.Clear();
        }

        // ──────────────────── Event Handlers ────────────────────

        private void HandleRoomEntered(string roomID)
        {
            if (_isVisible)
                Rebuild();
        }

        private void HandleFloorChanged(int newFloor)
        {
            if (_isVisible)
                Rebuild();
        }

        // ──────────────────── Utility ────────────────────

        private static string GetFloorLabel(int floor)
        {
            return floor switch
            {
                0 => "F0",
                > 0 => $"F{floor}",
                _ => $"F{floor}"
            };
        }
    }
}
