using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;
using PrimeTween;
using ProjectArk.Core;
using ProjectArk.Level;

namespace ProjectArk.UI
{
    /// <summary>
    /// Area title pop-up display (Minishoot "AreaTitleDisplay" equivalent).
    /// 
    /// Monitors LevelEvents.OnRoomEntered and displays the room's display name
    /// as a cinematic title card on first visit. Uses CanvasGroup for fade animation.
    /// 
    /// Place on a Canvas with a CanvasGroup + TMP_Text child.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class AreaTitleDisplay : MonoBehaviour
    {
        // ──────────────────── Configuration ────────────────────

        [Header("UI References")]
        [Tooltip("Text element displaying the area name.")]
        [SerializeField] private TMP_Text _titleText;

        [Tooltip("Optional subtitle text (e.g., 'Floor B2' or a tagline).")]
        [SerializeField] private TMP_Text _subtitleText;

        [Header("Animation")]
        [Tooltip("Duration (seconds) for the title to fade in.")]
        [SerializeField] private float _fadeInDuration = 0.5f;

        [Tooltip("Duration (seconds) the title stays visible.")]
        [SerializeField] private float _displayDuration = 2.5f;

        [Tooltip("Duration (seconds) for the title to fade out.")]
        [SerializeField] private float _fadeOutDuration = 0.8f;

        [Tooltip("Vertical slide distance (in UI units) during fade in/out.")]
        [SerializeField] private float _slideDistance = 30f;

        [Header("Behavior")]
        [Tooltip("If true, only shows title on first visit to each room.")]
        [SerializeField] private bool _firstVisitOnly = true;

        // ──────────────────── Runtime State ────────────────────

        private CanvasGroup _canvasGroup;
        private RectTransform _rectTransform;
        private Vector2 _originalAnchoredPosition;
        private HashSet<string> _shownRoomIDs = new();
        private CancellationTokenSource _displayCts;
        private RoomManager _roomManager;

        // ──────────────────── Lifecycle ────────────────────

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _rectTransform = GetComponent<RectTransform>();
            _originalAnchoredPosition = _rectTransform.anchoredPosition;

            // Start hidden
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        private void Start()
        {
            if (_firstVisitOnly)
            {
                LevelEvents.OnRoomFirstVisit += HandleRoomFirstVisit;
            }
            else
            {
                LevelEvents.OnRoomEntered += HandleRoomEntered;
            }

            _roomManager = ServiceLocator.Get<RoomManager>();
        }

        private void OnDestroy()
        {
            LevelEvents.OnRoomFirstVisit -= HandleRoomFirstVisit;
            LevelEvents.OnRoomEntered -= HandleRoomEntered;
            CancelDisplay();
        }

        // ──────────────────── Event Handlers ────────────────────

        private void HandleRoomFirstVisit(string roomID)
        {
            ShowTitleForRoom(roomID);
        }

        private void HandleRoomEntered(string roomID)
        {
            if (_firstVisitOnly && _shownRoomIDs.Contains(roomID)) return;
            ShowTitleForRoom(roomID);
        }

        // ──────────────────── Display Logic ────────────────────

        private void ShowTitleForRoom(string roomID)
        {
            if (_roomManager == null) return;

            var room = _roomManager.CurrentRoom;
            if (room == null || room.Data == null) return;

            string displayName = room.Data.DisplayName;
            if (string.IsNullOrEmpty(displayName)) return;

            // Skip safe/corridor rooms unless they have explicit display names
            if (room.NodeType == RoomNodeType.Transit) return;

            _shownRoomIDs.Add(roomID);

            // Set text
            if (_titleText != null)
            {
                _titleText.text = displayName;
            }

            if (_subtitleText != null)
            {
                int floor = room.Data.FloorLevel;
                if (floor < 0)
                    _subtitleText.text = $"B{Mathf.Abs(floor)}F";
                else if (floor > 0)
                    _subtitleText.text = $"{floor}F";
                else
                    _subtitleText.text = string.Empty;
            }

            // Play animation
            CancelDisplay();
            _displayCts = new CancellationTokenSource();
            PlayTitleAnimation(_displayCts.Token).Forget();
        }

        private async UniTaskVoid PlayTitleAnimation(CancellationToken token)
        {
            try
            {
                // Reset position
                _rectTransform.anchoredPosition = _originalAnchoredPosition + Vector2.down * _slideDistance;

                // ── Fade In + Slide Up ──
                _ = Tween.Custom(0f, 1f, _fadeInDuration, useUnscaledTime: true,
                    onValueChange: v =>
                    {
                        if (_canvasGroup != null) _canvasGroup.alpha = v;
                    },
                    ease: Ease.OutCubic);

                _ = Tween.UIAnchoredPosition(
                    _rectTransform,
                    _originalAnchoredPosition,
                    _fadeInDuration,
                    ease: Ease.OutCubic,
                    useUnscaledTime: true);

                int fadeInMs = Mathf.RoundToInt(_fadeInDuration * 1000f);
                await UniTask.Delay(fadeInMs, cancellationToken: token);

                // ── Hold ──
                int holdMs = Mathf.RoundToInt(_displayDuration * 1000f);
                await UniTask.Delay(holdMs, cancellationToken: token);

                // ── Fade Out ──
                _ = Tween.Custom(1f, 0f, _fadeOutDuration, useUnscaledTime: true,
                    onValueChange: v =>
                    {
                        if (_canvasGroup != null) _canvasGroup.alpha = v;
                    },
                    ease: Ease.InCubic);

                int fadeOutMs = Mathf.RoundToInt(_fadeOutDuration * 1000f);
                await UniTask.Delay(fadeOutMs, cancellationToken: token);
            }
            catch (System.OperationCanceledException)
            {
                // Display was cancelled — ensure hidden
                if (_canvasGroup != null)
                    _canvasGroup.alpha = 0f;
            }
        }

        private void CancelDisplay()
        {
            if (_displayCts != null)
            {
                _displayCts.Cancel();
                _displayCts.Dispose();
                _displayCts = null;
            }
        }
    }
}
