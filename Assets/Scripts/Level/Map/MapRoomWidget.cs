using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectArk.Level
{
    /// <summary>
    /// UI element representing a single room node on the map.
    /// Used by both MapPanel (full-screen) and MinimapHUD (corner).
    /// Displays a colored rectangle based on RoomType, an optional icon, and a "current" highlight ring.
    /// </summary>
    public class MapRoomWidget : MonoBehaviour
    {
        // ──────────────────── Serialized References ────────────────────

        [Header("Visual")]
        [SerializeField] private Image _background;
        [SerializeField] private Image _iconOverlay;
        [SerializeField] private Image _currentHighlight;
        [SerializeField] private Image _fogOverlay;
        [SerializeField] private TMP_Text _labelText;

        // ──────────────────── Type Colors ────────────────────

        private static readonly Color COLOR_NORMAL = new(0.35f, 0.55f, 0.75f, 0.85f);
        private static readonly Color COLOR_ARENA = new(0.85f, 0.35f, 0.35f, 0.85f);
        private static readonly Color COLOR_BOSS = new(0.95f, 0.20f, 0.20f, 0.95f);
        private static readonly Color COLOR_SAFE = new(0.30f, 0.80f, 0.40f, 0.85f);
        private static readonly Color COLOR_FOG = new(0.15f, 0.15f, 0.20f, 0.90f);
        private static readonly Color COLOR_HIGHLIGHT = new(1f, 0.85f, 0.25f, 1f);

        // ──────────────────── State ────────────────────

        private MapRoomData _data;

        public string RoomID => _data.RoomID;

        // ──────────────────── Public API ────────────────────

        /// <summary>
        /// Configure this widget with room data.
        /// </summary>
        public void Setup(MapRoomData data)
        {
            _data = data;
            Refresh();
        }

        /// <summary>
        /// Update visual state (e.g., when visited/current status changes).
        /// </summary>
        public void Refresh()
        {
            // Background color based on room type
            if (_background != null)
            {
                _background.color = GetTypeColor(_data.Type);
            }

            // Icon overlay
            if (_iconOverlay != null)
            {
                if (_data.MapIcon != null && _data.IsVisited)
                {
                    _iconOverlay.sprite = _data.MapIcon;
                    _iconOverlay.enabled = true;
                }
                else
                {
                    _iconOverlay.enabled = false;
                }
            }

            // Current room highlight
            if (_currentHighlight != null)
            {
                _currentHighlight.enabled = _data.IsCurrent;
                _currentHighlight.color = COLOR_HIGHLIGHT;
            }

            // Fog overlay for unvisited rooms
            if (_fogOverlay != null)
            {
                _fogOverlay.enabled = !_data.IsVisited;
                _fogOverlay.color = COLOR_FOG;
            }

            // Label
            if (_labelText != null)
            {
                if (_data.IsVisited && !string.IsNullOrEmpty(_data.DisplayName))
                {
                    _labelText.text = _data.DisplayName;
                    _labelText.enabled = true;
                }
                else
                {
                    _labelText.enabled = false;
                }
            }
        }

        /// <summary>
        /// Update the IsCurrent flag and refresh.
        /// </summary>
        public void SetCurrent(bool isCurrent)
        {
            _data.IsCurrent = isCurrent;
            if (_currentHighlight != null)
                _currentHighlight.enabled = isCurrent;
        }

        /// <summary>
        /// Update visited status and refresh.
        /// </summary>
        public void SetVisited(bool isVisited)
        {
            _data.IsVisited = isVisited;
            Refresh();
        }

        // ──────────────────── Helpers ────────────────────

        private static Color GetTypeColor(RoomType type)
        {
            return type switch
            {
                RoomType.Arena => COLOR_ARENA,
                RoomType.Boss => COLOR_BOSS,
                RoomType.Safe => COLOR_SAFE,
                _ => COLOR_NORMAL
            };
        }
    }
}
