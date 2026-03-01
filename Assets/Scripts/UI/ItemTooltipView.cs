using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PrimeTween;
using ProjectArk.Combat;
using UnityEngine.InputSystem;

namespace ProjectArk.UI
{
    /// <summary>
    /// Floating tooltip card that appears after a 150ms hover delay.
    /// Uses UniTask for delay management and PrimeTween for fade animation.
    /// Supports screen boundary detection and drag-time suppression.
    /// Must remain active (CanvasGroup alpha=0) — never SetActive(false).
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class ItemTooltipView : MonoBehaviour
    {
        [Header("Layout References")]
        [SerializeField] private Image _iconImage;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _typeText;
        [SerializeField] private TMP_Text _statsText;
        [SerializeField] private TMP_Text _descriptionText;
        [SerializeField] private TMP_Text _equippedStatusText;
        [SerializeField] private TMP_Text _actionHintText;
        [SerializeField] private Image _typeBadgeBackground;
        [SerializeField] private RectTransform _containerRect;

        [Header("Settings")]
        [SerializeField] private float _showDelay = 0.15f;
        [SerializeField] private Vector2 _offset = new Vector2(14f, -14f);
        [SerializeField] private float _fadeDuration = 0.08f;
        [SerializeField] private float _tooltipWidth = 280f;
        [SerializeField] private float _tooltipHeight = 230f;

        private CanvasGroup _canvasGroup;
        private Canvas _rootCanvas;
        private bool _isVisible;
        private bool _isDragSuppressed;

        // UniTask cancellation token for show delay
        private CancellationTokenSource _showCts;

        // PrimeTween handle for fade
        private Tween _fadeTween;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            // Walk up to find the root (top-level) Canvas
            var c = GetComponentInParent<Canvas>();
            _rootCanvas = c != null ? c.rootCanvas : null;

            // Start hidden via CanvasGroup — never SetActive(false)
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
            _isVisible = false;

            // Subscribe to drag events for suppression
            // (DragDropManager is a singleton, subscribe after it's ready)
        }

        private void OnEnable()
        {
            // Subscribe to DragDropManager events if available
            // We poll IsDragging in Show() instead of event subscription
            // to avoid ordering issues with singleton initialization.
        }

        private void OnDestroy()
        {
            _showCts?.Cancel();
            _showCts?.Dispose();
        }

        // =====================================================================
        // Public API
        // =====================================================================

        /// <summary>
        /// Show tooltip for the given item after a 150ms delay.
        /// </summary>
        /// <param name="item">The item to display.</param>
        /// <param name="isEquipped">Whether the item is currently equipped.</param>
        /// <param name="equippedLocation">e.g. "PRIMARY · CORE" or empty string.</param>
        public void ShowTooltip(StarChartItemSO item, bool isEquipped, string equippedLocation = "")
        {
            if (item == null) return;

            // Cancel any pending show
            CancelPendingShow();

            // Start new delayed show
            _showCts = new CancellationTokenSource();
            ShowAfterDelayAsync(item, isEquipped, equippedLocation, _showCts.Token).Forget();
        }

        /// <summary>
        /// Immediately hide the tooltip.
        /// </summary>
        public void HideTooltip()
        {
            CancelPendingShow();

            if (!_isVisible) return;

            _fadeTween.Stop();
            _fadeTween = Tween.Alpha(_canvasGroup, endValue: 0f,
                duration: _fadeDuration, ease: Ease.OutQuad, useUnscaledTime: true)
                .OnComplete(() =>
                {
                    _isVisible = false;
                    _canvasGroup.blocksRaycasts = false;
                    _canvasGroup.interactable = false;
                });
        }

        /// <summary>
        /// Suppress tooltip display during drag operations.
        /// </summary>
        public void SetDragSuppressed(bool suppressed)
        {
            _isDragSuppressed = suppressed;
            if (suppressed)
                HideTooltip();
        }

        // =====================================================================
        // Internal
        // =====================================================================

        private async UniTaskVoid ShowAfterDelayAsync(
            StarChartItemSO item,
            bool isEquipped,
            string equippedLocation,
            CancellationToken ct)
        {
            await UniTask.Delay((int)(_showDelay * 1000), ignoreTimeScale: true, cancellationToken: ct);

            if (ct.IsCancellationRequested) return;

            // Suppress during drag
            if (_isDragSuppressed || (DragDropManager.Instance != null && DragDropManager.Instance.IsDragging))
                return;

            PopulateContent(item, isEquipped, equippedLocation);
            PositionNearMouse();

            _isVisible = true;
            _canvasGroup.blocksRaycasts = false; // tooltip never blocks raycasts
            _canvasGroup.interactable = false;

            _fadeTween.Stop();
            _canvasGroup.alpha = 0f;
            _fadeTween = Tween.Alpha(_canvasGroup, endValue: 1f,
                duration: _fadeDuration, ease: Ease.OutQuad, useUnscaledTime: true);
        }

        private void CancelPendingShow()
        {
            if (_showCts != null)
            {
                _showCts.Cancel();
                _showCts.Dispose();
                _showCts = null;
            }
        }

        private void Update()
        {
            if (_isVisible)
                PositionNearMouse();
        }

        private void PositionNearMouse()
        {
            var rect = _containerRect != null ? _containerRect : (RectTransform)transform;
            if (rect == null) return;

            if (Mouse.current == null) return;
            Vector2 mousePos = Mouse.current.position.ReadValue();
            float w = _tooltipWidth;
            float h = _tooltipHeight;

            // Default: right-below mouse
            float x = mousePos.x + _offset.x;
            float y = mousePos.y + _offset.y;

            // Flip horizontally if overflowing right edge
            if (x + w > Screen.width)
                x = mousePos.x - w - Mathf.Abs(_offset.x);

            // Flip vertically if overflowing bottom edge
            if (y - h < 0f)
                y = mousePos.y + Mathf.Abs(_offset.y) + h;

            // Clamp to screen
            x = Mathf.Clamp(x, 0f, Screen.width - w);
            y = Mathf.Clamp(y, h, Screen.height);

            // --- DIAGNOSTIC: log once per show ---
            Debug.Log($"[Tooltip] mousePos={mousePos} targetScreen=({x},{y}) " +
                      $"rect={rect.name} rootCanvas={(_rootCanvas != null ? _rootCanvas.name : "NULL")} " +
                      $"renderMode={(_rootCanvas != null ? _rootCanvas.renderMode.ToString() : "N/A")} " +
                      $"Screen={Screen.width}x{Screen.height}");

            // Convert screen position to world position and apply directly.
            // Using world position bypasses anchor/pivot offsets entirely,
            // so the tooltip always tracks the mouse regardless of nesting depth.
            if (_rootCanvas != null)
            {
                Camera cam = _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                    ? null : _rootCanvas.worldCamera;

                var canvasRect = _rootCanvas.transform as RectTransform;
                if (canvasRect != null &&
                    RectTransformUtility.ScreenPointToWorldPointInRectangle(
                        canvasRect, new Vector2(x, y), cam, out var worldPoint))
                {
                    Debug.Log($"[Tooltip] worldPoint={worldPoint} rect.position before={rect.position}");
                    rect.position = worldPoint;
                    Debug.Log($"[Tooltip] rect.position after={rect.position}");
                }
                else
                {
                    Debug.LogWarning($"[Tooltip] ScreenPointToWorldPointInRectangle FAILED. canvasRect={canvasRect}");
                }
            }
            else
            {
                Debug.LogWarning("[Tooltip] _rootCanvas is NULL, using raw screen coords fallback");
                rect.position = new Vector3(x, y, 0f);
            }
        }

        private void PopulateContent(StarChartItemSO item, bool isEquipped, string equippedLocation)
        {
            // Icon
            if (_iconImage != null)
            {
                if (item.Icon != null)
                {
                    _iconImage.sprite = item.Icon;
                    _iconImage.color = Color.white;
                }
                else
                {
                    _iconImage.sprite = null;
                    _iconImage.color = StarChartTheme.GetTypeColor(item.ItemType);
                }
            }

            // Name
            if (_nameText != null)
                _nameText.text = item.DisplayName;

            // Type label with color
            if (_typeText != null)
            {
                _typeText.text = GetTypeLabel(item.ItemType);
                _typeText.color = StarChartTheme.GetTypeColor(item.ItemType);
            }

            // Type badge background
            if (_typeBadgeBackground != null)
            {
                var tc = StarChartTheme.GetTypeColor(item.ItemType);
                _typeBadgeBackground.color = new Color(tc.r, tc.g, tc.b, 0.2f);
            }

            // Stats text (built by TooltipContentBuilder)
            if (_statsText != null)
                _statsText.text = TooltipContentBuilder.BuildStatsText(item);

            // Description
            if (_descriptionText != null)
                _descriptionText.text = item.Description;

            // Equipped status
            if (_equippedStatusText != null)
            {
                if (isEquipped && !string.IsNullOrEmpty(equippedLocation))
                {
                    _equippedStatusText.text = $"✓ EQUIPPED · {equippedLocation}";
                    _equippedStatusText.color = StarChartTheme.EquippedGreen;
                    _equippedStatusText.gameObject.SetActive(true);
                }
                else
                {
                    _equippedStatusText.gameObject.SetActive(false);
                }
            }

            // Action hint
            if (_actionHintText != null)
            {
                _actionHintText.text = isEquipped
                    ? "Drag to move or drag to inventory to unequip"
                    : "Drag to a slot to equip";
                _actionHintText.color = StarChartTheme.CyanDim;
            }
        }

        private static string GetTypeLabel(StarChartItemType type)
        {
            return type switch
            {
                StarChartItemType.Core      => "CORE",
                StarChartItemType.Prism     => "PRISM",
                StarChartItemType.LightSail => "LIGHT SAIL",
                StarChartItemType.Satellite => "SATELLITE",
                _                           => "UNKNOWN"
            };
        }
    }
}
