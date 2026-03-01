using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PrimeTween;

namespace ProjectArk.UI
{
    /// <summary>
    /// Gatling-column Loadout switcher.
    /// Displays the current Loadout name + "X/N" counter and ▲/▼ navigation buttons.
    /// MVP: single-Loadout mode — buttons are disabled; interface is ready for multi-Loadout expansion.
    /// </summary>
    public class LoadoutSwitcher : MonoBehaviour
    {
        [Header("Navigation Buttons")]
        [SerializeField] private Button _prevButton;
        [SerializeField] private Button _nextButton;

        [Header("Drum Counter")]
        [SerializeField] private TMP_Text _drumCounterLabel;

        [Header("Loadout Card")]
        [SerializeField] private RectTransform _loadoutCard;

        /// <summary> Fired when the active Loadout index changes. </summary>
        public event Action<int> OnLoadoutChanged;

        private int _currentIndex;
        private int _totalCount = 1;

        // Slide animation height offset (px) for Loadout card transition
        private const float SlideOffset = 60f;
        private Sequence _slideSequence;

        private void Awake()
        {
            if (_prevButton != null)
                _prevButton.onClick.AddListener(OnPrevClicked);

            if (_nextButton != null)
                _nextButton.onClick.AddListener(OnNextClicked);
        }

        private void Start()
        {
            // MVP: single Loadout — disable navigation
            SetLoadoutCount(1);
        }

        // =====================================================================
        // Public API
        // =====================================================================

        /// <summary>
        /// Set the total number of available Loadouts and refresh the display.
        /// </summary>
        public void SetLoadoutCount(int count)
        {
            _totalCount = Mathf.Max(1, count);
            _currentIndex = Mathf.Clamp(_currentIndex, 0, _totalCount - 1);
            UpdateDisplay();
            UpdateButtonState();
        }

        /// <summary>
        /// Switch to a specific Loadout index and play the card slide animation.
        /// </summary>
        public void SwitchTo(int index, bool slideUp = true)
        {
            if (index < 0 || index >= _totalCount) return;
            if (index == _currentIndex) return;

            _currentIndex = index;
            UpdateDisplay();
            PlaySlideAnimation(slideUp);
            OnLoadoutChanged?.Invoke(_currentIndex);
        }

        /// <summary> Current active Loadout index (0-based). </summary>
        public int CurrentIndex => _currentIndex;

        // =====================================================================
        // Private helpers
        // =====================================================================

        private void OnPrevClicked()
        {
            int next = (_currentIndex - 1 + _totalCount) % _totalCount;
            SwitchTo(next, slideUp: false);
        }

        private void OnNextClicked()
        {
            int next = (_currentIndex + 1) % _totalCount;
            SwitchTo(next, slideUp: true);
        }

        private void UpdateDisplay()
        {
            if (_drumCounterLabel != null)
                _drumCounterLabel.text = $"LOADOUT #{_currentIndex + 1}  ·  {_currentIndex + 1}/{_totalCount}";
        }

        private void UpdateButtonState()
        {
            bool canSwitch = _totalCount > 1;
            if (_prevButton != null) _prevButton.interactable = canSwitch;
            if (_nextButton != null) _nextButton.interactable = canSwitch;
        }

        private void PlaySlideAnimation(bool slideUp)
        {
            if (_loadoutCard == null) return;

            _slideSequence.Stop();

            float fromY = slideUp ? -SlideOffset : SlideOffset;
            float toY   = 0f;

            // Snap to start position, then tween to rest
            _loadoutCard.anchoredPosition = new Vector2(_loadoutCard.anchoredPosition.x, fromY);

            _slideSequence = Sequence.Create(useUnscaledTime: true)
                .Group(Tween.UIAnchoredPositionY(_loadoutCard, endValue: toY,
                    duration: 0.25f, ease: Ease.OutCubic, useUnscaledTime: true));
        }

        private void OnDestroy()
        {
            _slideSequence.Stop();
        }
    }
}
