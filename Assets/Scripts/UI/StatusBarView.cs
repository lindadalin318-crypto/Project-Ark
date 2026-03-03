using TMPro;
using UnityEngine;
using PrimeTween;

namespace ProjectArk.UI
{
    /// <summary>
    /// Bottom status bar that shows transient notification messages.
    /// Messages fade in (150ms) then fade out after a configurable duration.
    /// New messages immediately interrupt any ongoing animation.
    /// </summary>
    public class StatusBarView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _label;

        /// <summary> Default idle text shown when no notification is active. </summary>
        /// <remarks>
        /// Supports two format placeholders:
        ///   {0} = equipped item count
        ///   {1} = inventory item count
        /// Call <see cref="SetCounts"/> to update these values.
        /// </remarks>
        [SerializeField] private string _idleText =
            "EQUIPPED {0}/10  ·  INVENTORY {1} ITEMS  ·  DRAG TO EQUIP  ·  CLICK TO INSPECT  ·  DRAG SLOT TO INVENTORY TO UNEQUIP";

        private Sequence _animSequence;
        private bool _isPersistent;  // true while showing a persistent (non-fading) message

        // Counts for idle text format placeholders
        private int _equippedCount;
        private int _inventoryCount;

        private void Awake()
        {
            ShowIdle();
        }

        /// <summary>
        /// Update the equipped/inventory counts used in the idle text placeholders.
        /// Immediately refreshes the idle text if no persistent message is active.
        /// </summary>
        public void SetCounts(int equipped, int inventory)
        {
            _equippedCount = equipped;
            _inventoryCount = inventory;
            if (!_isPersistent)
                ShowIdle();
        }

        /// <summary>
        /// Display a notification message with the given color.
        /// Fades in over 150ms, holds for <paramref name="duration"/> seconds, then fades out.
        /// </summary>
        public void ShowMessage(string text, Color color, float duration = 3f)
        {
            if (_label == null) return;

            // Interrupt any ongoing animation
            _animSequence.Stop();

            _label.text = text;

            // Start from alpha 0, fade in → hold → fade out
            var startColor = new Color(color.r, color.g, color.b, 0f);
            _label.color = startColor;

            _animSequence = Sequence.Create(useUnscaledTime: true)
                .Chain(Tween.Color(_label, endValue: color, duration: 0.15f,
                    ease: Ease.OutQuad, useUnscaledTime: true))
                .ChainDelay(duration)
                .Chain(Tween.Color(_label, endValue: new Color(color.r, color.g, color.b, 0f),
                    duration: 0.5f, ease: Ease.InQuad, useUnscaledTime: true))
                .ChainCallback(ShowIdle);
        }

        /// <summary>
        /// Show a persistent status message that stays until RestoreDefault() is called.
        /// Does not auto-fade. Used for drag-state hints.
        /// </summary>
        public void ShowPersistent(string text, Color? color = null)
        {
            if (_label == null) return;
            _animSequence.Stop();
            _isPersistent = true;
            _label.text = text;
            _label.color = color ?? StarChartTheme.StatusIdle;
        }

        /// <summary> Restore idle state immediately, clearing any persistent message. </summary>
        public void RestoreDefault()
        {
            _isPersistent = false;
            ShowIdle();
        }

        /// <summary> Restore idle state immediately. </summary>
        public void ShowIdle()
        {
            if (_label == null) return;
            _animSequence.Stop();
            // Fill in {0} = equippedCount, {1} = inventoryCount
            _label.text = string.Format(_idleText, _equippedCount, _inventoryCount);
            _label.color = StarChartTheme.StatusIdle;
        }

        /// <summary>
        /// Set text and color instantly with no animation.
        /// Use this for persistent status text (e.g. equipped count) where
        /// a fade-in / fade-out cycle is not desired and a zero-duration
        /// tween would trigger a PrimeTween warning.
        /// </summary>
        public void SetText(string text, Color color)
        {
            if (_label == null) return;
            _animSequence.Stop();
            _label.text = text;
            _label.color = color;
        }

        private void OnDestroy()
        {
            _animSequence.Stop();
        }
    }
}
