using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using PrimeTween;
using ProjectArk.Combat;

namespace ProjectArk.UI
{
    /// <summary>
    /// Represents a single type column (SAIL / PRISM / CORE / SAT) in a <see cref="TrackView"/>.
    /// Holds a 2×2 grid of <see cref="SlotCellView"/> cells and handles per-column highlight/hover.
    /// Extracted as a top-level class to ensure stable Unity GUID serialization.
    /// </summary>
    public class TypeColumn : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private TMP_Text _columnLabel;
        [SerializeField] private Image _columnDot;
        [SerializeField] private Image _columnBorder;

        /// <summary> 4 cells in row-major order: [0]=top-left, [1]=top-right, [2]=bottom-left, [3]=bottom-right </summary>
        [SerializeField] private SlotCellView[] _cells = new SlotCellView[4];

        private SlotType _slotType;
        private Color _typeColor;
        private Color _dimColor;
        private Tween _borderTween;

        /// <summary> The slot type this column represents. </summary>
        public SlotType SlotType => _slotType;

        /// <summary> All 4 cells in this column. </summary>
        public SlotCellView[] Cells => _cells;

        /// <summary>
        /// The RectTransform of the grid container that holds the 4 cells.
        /// Used by TrackView to parent ItemOverlayView instances.
        /// Returns the parent of cells[0], or this transform if cells[0] is null.
        /// </summary>
        public RectTransform GridContainer
        {
            get
            {
                if (_cells != null && _cells.Length > 0 && _cells[0] != null)
                    return _cells[0].transform.parent as RectTransform;
                return transform as RectTransform;
            }
        }

        /// <summary> Initialize column identity and colors. </summary>
        public void Initialize(SlotType slotType, Color typeColor, TrackView ownerTrack)
        {
            _slotType = slotType;
            _typeColor = typeColor;
            _dimColor = new Color(typeColor.r, typeColor.g, typeColor.b, 0.18f);

            // Label
            if (_columnLabel != null)
            {
                _columnLabel.text = slotType switch
                {
                    SlotType.LightSail => "SAIL",
                    SlotType.Prism     => "PRISM",
                    SlotType.Core      => "CORE",
                    SlotType.Satellite => "SAT",
                    _                  => slotType.ToString().ToUpper()
                };
                _columnLabel.color = typeColor;
            }

            // Dot
            if (_columnDot != null)
                _columnDot.color = typeColor;

            // Border default dim
            if (_columnBorder != null)
                _columnBorder.color = _dimColor;

            // Wire up cells
            for (int i = 0; i < _cells.Length; i++)
            {
                if (_cells[i] == null) continue;
                _cells[i].SlotType = slotType;
                _cells[i].CellIndex = i;
                _cells[i].OwnerTrack = ownerTrack;
                _cells[i].OwnerColumn = this;
                _cells[i].SetThemeColor(typeColor);
            }
        }

        // ── Hover border animation ──────────────────────────────────────

        public void OnPointerEnter(PointerEventData eventData)
        {
            _borderTween.Stop();
            if (_columnBorder != null)
                _borderTween = Tween.Color(_columnBorder, endValue: _typeColor,
                    duration: 0.15f, ease: Ease.OutQuad, useUnscaledTime: true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _borderTween.Stop();
            if (_columnBorder != null)
                _borderTween = Tween.Color(_columnBorder, endValue: _dimColor,
                    duration: 0.25f, ease: Ease.OutQuad, useUnscaledTime: true);
        }

        // ── Highlight helpers ───────────────────────────────────────────

        /// <summary> Highlight the column border as a valid drop target. </summary>
        public void SetDropHighlight(bool valid)
        {
            _borderTween.Stop();
            if (_columnBorder != null)
                _columnBorder.color = valid ? StarChartTheme.HighlightValid : StarChartTheme.HighlightInvalid;
        }

        /// <summary> Restore border to dim color. </summary>
        public void ClearDropHighlight()
        {
            _borderTween.Stop();
            if (_columnBorder != null)
                _borderTween = Tween.Color(_columnBorder, endValue: _dimColor,
                    duration: 0.2f, ease: Ease.OutQuad, useUnscaledTime: true);
        }

        /// <summary>
        /// Set the column border to reflect the current drop preview state.
        /// Valid=green, Replace=orange, Invalid=red, None=restore candidate pulse.
        /// </summary>
        public void SetDropPreview(DropPreviewState state)
        {
            _borderTween.Stop();
            if (_columnBorder == null) return;

            Color borderColor = state switch
            {
                DropPreviewState.Valid   => StarChartTheme.HighlightValid,
                DropPreviewState.Replace => StarChartTheme.HighlightReplace,
                DropPreviewState.Invalid => StarChartTheme.HighlightInvalid,
                _                        => _dimColor
            };
            _columnBorder.color = borderColor;
        }

        /// <summary>
        /// Show or clear the "drop candidate" breathing pulse highlight.
        /// Called when a drag begins/ends to indicate this column accepts the dragged type.
        /// </summary>
        public void SetDropCandidate(bool active)
        {
            _borderTween.Stop();
            if (_columnBorder == null) return;

            if (active)
            {
                // Breathing pulse: dim → typeColor → dim, looping
                _borderTween = Tween.Color(_columnBorder,
                    startValue: _dimColor,
                    endValue: new Color(_typeColor.r, _typeColor.g, _typeColor.b, 0.6f),
                    duration: 0.7f,
                    ease: Ease.InOutSine,
                    useUnscaledTime: true,
                    cycles: -1,
                    cycleMode: CycleMode.Yoyo);
            }
            else
            {
                _columnBorder.color = _dimColor;
            }
        }

        /// <summary> Set highlight on a range of cells (for multi-size items). </summary>
        public void SetCellHighlight(int startIndex, int count, bool valid, bool isReplace = false)
        {
            for (int i = startIndex; i < startIndex + count && i < _cells.Length; i++)
            {
                if (_cells[i] == null) continue;
                if (isReplace)
                    _cells[i].SetReplaceHighlight();
                else
                    _cells[i].SetHighlight(valid);
            }
        }

        /// <summary> Clear all cell highlights in this column. </summary>
        public void ClearAllCellHighlights()
        {
            foreach (var cell in _cells)
                cell?.ClearHighlight();
        }

        private void OnDestroy()
        {
            _borderTween.Stop();
        }
    }
}
