using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PrimeTween;
using ProjectArk.Combat;

namespace ProjectArk.UI
{
    /// <summary>
    /// Controls the Loadout switcher UI: prev/next navigation, drum counter flip animation,
    /// card slide+scale animation, and Loadout management (rename/delete/save).
    /// </summary>
    public class LoadoutSwitcher : MonoBehaviour
    {
        [Header("Navigation Buttons")]
        [SerializeField] private Button _prevButton;
        [SerializeField] private Button _nextButton;

        [Header("Drum Counter (flip animation)")]
        [Tooltip("Container for the drum flip effect. Must have perspective via Canvas 3D.")]
        [SerializeField] private RectTransform _drumContainer;
        [SerializeField] private TMP_Text _drumFront;   // currently visible number
        [SerializeField] private TMP_Text _drumBack;    // incoming number (hidden behind)

        [Header("Loadout Card")]
        [SerializeField] private RectTransform _loadoutCard;

        [Header("Loadout Management")]
        [SerializeField] private Button _renameButton;
        [SerializeField] private Button _deleteButton;
        [SerializeField] private Button _saveButton;
        [SerializeField] private TMP_InputField _renameInputField;  // inline rename input
        [SerializeField] private TMP_Text _loadoutNameLabel;        // displays current loadout name

        [Header("Status Bar (for SAVED feedback)")]
        [SerializeField] private StatusBarView _statusBar;

        /// <summary> Fired when the active Loadout index changes. </summary>
        public event Action<int> OnLoadoutChanged;

        /// <summary> Fired when the user renames the current Loadout. </summary>
        public event Action<int, string> OnLoadoutRenamed;

        /// <summary> Fired when the user deletes the current Loadout. </summary>
        public event Action<int> OnLoadoutDeleted;

        /// <summary> Fired when the user saves the current Loadout config. </summary>
        public event Action<int> OnLoadoutSaved;

        private int _currentIndex;
        private int _totalCount = 1;
        private string[] _loadoutNames;

        // Slide animation height offset (px) for Loadout card transition
        private const float SlideOffset = 60f;
        private Sequence _slideSequence;

        // Drum flip animation
        private const float DrumFlipDuration = 0.28f;
        private Sequence _drumSequence;

        // Prevent ConfirmRename from being called twice when Enter is pressed
        // (onSubmit + onDeselect both fire on Enter key)
        private bool _isConfirmingRename;

        private void Awake()
        {
            if (_prevButton != null)
                _prevButton.onClick.AddListener(OnPrevClicked);

            if (_nextButton != null)
                _nextButton.onClick.AddListener(OnNextClicked);

            if (_renameButton != null)
                _renameButton.onClick.AddListener(OnRenameClicked);

            if (_deleteButton != null)
                _deleteButton.onClick.AddListener(OnDeleteClicked);

            if (_saveButton != null)
                _saveButton.onClick.AddListener(OnSaveClicked);

            // Rename input: confirm on submit or deselect
            if (_renameInputField != null)
            {
                _renameInputField.onSubmit.AddListener(ConfirmRename);
                _renameInputField.onDeselect.AddListener(ConfirmRename);
                // CLAUDE.md: use CanvasGroup to hide, not SetActive
                var cg = _renameInputField.GetComponent<CanvasGroup>();
                if (cg == null) cg = _renameInputField.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
                cg.interactable = false;
                cg.blocksRaycasts = false;
            }

            // Initialize drum back to invisible
            if (_drumBack != null)
            {
                var cg = _drumBack.GetComponent<CanvasGroup>();
                if (cg == null) cg = _drumBack.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
            }
        }

        // NOTE: SetLoadoutCount is called externally by StarChartPanel.Bind() with the correct count.
        // Do NOT call SetLoadoutCount(1) here — it would override the multi-loadout setup.

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

            // Initialize names array if needed
            if (_loadoutNames == null || _loadoutNames.Length != _totalCount)
            {
                var newNames = new string[_totalCount];
                for (int i = 0; i < _totalCount; i++)
                    newNames[i] = (_loadoutNames != null && i < _loadoutNames.Length)
                        ? _loadoutNames[i]
                        : $"LOADOUT #{i + 1}";
                _loadoutNames = newNames;
            }

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

            int prevIndex = _currentIndex;
            _currentIndex = index;
            bool goingUp = index > prevIndex;

            PlayDrumFlip(_currentIndex + 1, goingUp);
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
            // Update loadout name label
            if (_loadoutNameLabel != null && _loadoutNames != null && _currentIndex < _loadoutNames.Length)
                _loadoutNameLabel.text = _loadoutNames[_currentIndex];

            // Drum front shows current number (updated by flip animation or directly)
            if (_drumFront != null)
                _drumFront.text = $"{_currentIndex + 1}/{_totalCount}";
        }

        private void UpdateButtonState()
        {
            bool canSwitch = _totalCount > 1;
            if (_prevButton != null) _prevButton.interactable = canSwitch;
            if (_nextButton != null) _nextButton.interactable = canSwitch;

            // DELETE is disabled when only 1 loadout remains
            if (_deleteButton != null)
                _deleteButton.interactable = _totalCount > 1;
        }

        // =====================================================================
        // Drum Counter Flip Animation (Task 7)
        // =====================================================================

        /// <summary>
        /// Play a mechanical drum-flip animation for the counter.
        /// goingUp=true: old number flips up-out, new number flips up-in.
        /// goingUp=false: old number flips down-out, new number flips down-in.
        /// </summary>
        private void PlayDrumFlip(int newValue, bool goingUp)
        {
            if (_drumFront == null || _drumBack == null) return;

            _drumSequence.Stop();

            // Prepare back label with new value
            _drumBack.text = $"{newValue}/{_totalCount}";

            // Get or create CanvasGroups
            var frontCg = _drumFront.GetComponent<CanvasGroup>() ?? _drumFront.gameObject.AddComponent<CanvasGroup>();
            var backCg  = _drumBack.GetComponent<CanvasGroup>()  ?? _drumBack.gameObject.AddComponent<CanvasGroup>();

            // Reset positions
            _drumFront.rectTransform.localEulerAngles = Vector3.zero;
            _drumBack.rectTransform.localEulerAngles  = new Vector3(goingUp ? 90f : -90f, 0f, 0f);
            frontCg.alpha = 1f;
            backCg.alpha  = 1f;

            float half = DrumFlipDuration * 0.5f;

            // Front flips out, back flips in simultaneously
            _drumSequence = Sequence.Create(useUnscaledTime: true)
                .Group(Tween.LocalEulerAngles(_drumFront.rectTransform,
                    startValue: Vector3.zero,
                    endValue: new Vector3(goingUp ? -90f : 90f, 0f, 0f),
                    duration: DrumFlipDuration, ease: Ease.InQuad, useUnscaledTime: true))
                .Group(Tween.LocalEulerAngles(_drumBack.rectTransform,
                    startValue: new Vector3(goingUp ? 90f : -90f, 0f, 0f),
                    endValue: Vector3.zero,
                    duration: DrumFlipDuration, ease: Ease.OutQuad, useUnscaledTime: true))
                .ChainCallback(() =>
                {
                    // Swap: front now shows new value, back is hidden
                    _drumFront.text = _drumBack.text;
                    _drumFront.rectTransform.localEulerAngles = Vector3.zero;
                    _drumBack.rectTransform.localEulerAngles  = new Vector3(goingUp ? 90f : -90f, 0f, 0f);
                });
        }

        // =====================================================================
        // Loadout Card Slide + Scale Animation (Task 8)
        // =====================================================================

        private void PlaySlideAnimation(bool slideUp)
        {
            if (_loadoutCard == null) return;

            _slideSequence.Stop();

            float fromY = slideUp ? -SlideOffset : SlideOffset;
            const float duration = 0.25f;

            // Snap to start position and scale
            _loadoutCard.anchoredPosition = new Vector2(_loadoutCard.anchoredPosition.x, fromY);
            _loadoutCard.localScale = new Vector3(0.88f, 0.88f, 1f);

            // Slide to rest + scale to 1.0 in parallel
            _slideSequence = Sequence.Create(useUnscaledTime: true)
                .Group(Tween.UIAnchoredPositionY(_loadoutCard, endValue: 0f,
                    duration: duration, ease: Ease.OutCubic, useUnscaledTime: true))
                .Group(Tween.Scale(_loadoutCard, endValue: Vector3.one,
                    duration: duration, ease: Ease.OutCubic, useUnscaledTime: true))
                .ChainCallback(() =>
                {
                    // Ensure no cumulative error
                    _loadoutCard.localScale = Vector3.one;
                    _loadoutCard.anchoredPosition = new Vector2(_loadoutCard.anchoredPosition.x, 0f);
                });
        }

        // =====================================================================
        // Loadout Management (Task 6)
        // =====================================================================

        private void OnRenameClicked()
        {
            if (_renameInputField == null) return;

            // Show inline input field
            var cg = _renameInputField.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 1f;
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }

            string currentName = (_loadoutNames != null && _currentIndex < _loadoutNames.Length)
                ? _loadoutNames[_currentIndex]
                : $"LOADOUT #{_currentIndex + 1}";

            _renameInputField.text = currentName;
            _renameInputField.Select();
            _renameInputField.ActivateInputField();
        }

        private void ConfirmRename(string newName)
        {
            // Guard: onSubmit + onDeselect both fire when Enter is pressed,
            // causing a double-invoke. Skip the second call.
            if (_isConfirmingRename) return;
            _isConfirmingRename = true;

            if (_renameInputField == null)
            {
                _isConfirmingRename = false;
                return;
            }

            // Hide input field
            var cg = _renameInputField.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 0f;
                cg.interactable = false;
                cg.blocksRaycasts = false;
            }

            if (!string.IsNullOrWhiteSpace(newName))
            {
                newName = newName.Trim().ToUpper();
                if (_loadoutNames != null && _currentIndex < _loadoutNames.Length)
                    _loadoutNames[_currentIndex] = newName;

                UpdateDisplay();
                OnLoadoutRenamed?.Invoke(_currentIndex, newName);
            }

            _isConfirmingRename = false;
        }

        private void OnDeleteClicked()
        {
            if (_totalCount <= 1) return; // protected by button interactable, but double-check

            int deletedIndex = _currentIndex;
            _totalCount--;

            // Shift names array
            if (_loadoutNames != null)
            {
                var newNames = new string[_totalCount];
                int ni = 0;
                for (int i = 0; i <= _totalCount; i++)
                {
                    if (i == deletedIndex) continue;
                    newNames[ni++] = _loadoutNames[i];
                }
                _loadoutNames = newNames;
            }

            // Switch to adjacent loadout
            _currentIndex = Mathf.Clamp(_currentIndex, 0, _totalCount - 1);
            UpdateDisplay();
            UpdateButtonState();
            OnLoadoutDeleted?.Invoke(deletedIndex);
        }

        private void OnSaveClicked()
        {
            OnLoadoutSaved?.Invoke(_currentIndex);

            // Show "SAVED" feedback via status bar
            if (_statusBar != null)
                _statusBar.ShowMessage("SAVED", StarChartTheme.HighlightValid, 0.8f);
        }

        private void OnDestroy()
        {
            _slideSequence.Stop();
            _drumSequence.Stop();

            // Event hygiene: remove all button listeners (CLAUDE.md 架构原则 — 事件卫生)
            _prevButton?.onClick.RemoveAllListeners();
            _nextButton?.onClick.RemoveAllListeners();
            _renameButton?.onClick.RemoveAllListeners();
            _deleteButton?.onClick.RemoveAllListeners();
            _saveButton?.onClick.RemoveAllListeners();
            if (_renameInputField != null)
            {
                _renameInputField.onSubmit.RemoveAllListeners();
                _renameInputField.onDeselect.RemoveAllListeners();
            }
        }
    }
}
