using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PrimeTween;
using ProjectArk.Combat;

namespace ProjectArk.UI
{
    /// <summary>
    /// Handles the "fly back to inventory" animation when items are evicted by a forced replace.
    /// Creates a temporary clone that flies from the slot position to the inventory cell,
    /// then bounces the target cell on landing.
    /// All animations use unscaled time (compatible with paused state).
    /// </summary>
    public static class FlyBackAnimator
    {
        private static readonly List<Sequence> _activeAnimations = new();

        /// <summary>
        /// Fly an item icon from <paramref name="from"/> to <paramref name="to"/>.
        /// Creates a temporary Image clone, animates it, then calls <paramref name="onComplete"/>.
        /// </summary>
        /// <param name="from">World-space source RectTransform (the evicted slot).</param>
        /// <param name="to">World-space target RectTransform (the inventory cell).</param>
        /// <param name="item">The evicted item (used for icon/color).</param>
        /// <param name="canvasRoot">Root canvas for creating the clone.</param>
        /// <param name="onComplete">Called after the animation finishes (triggers landing bounce).</param>
        public static void FlyTo(
            RectTransform from,
            RectTransform to,
            StarChartItemSO item,
            Canvas canvasRoot,
            Action onComplete = null)
        {
            if (from == null || to == null || canvasRoot == null) return;

            // Create a temporary clone Image under the canvas root
            var cloneGO = new GameObject("FlyClone", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            cloneGO.transform.SetParent(canvasRoot.transform, false);

            var cloneRect = cloneGO.GetComponent<RectTransform>();
            cloneRect.sizeDelta = from.sizeDelta;

            // Position clone at source world position
            Vector2 fromScreenPos = RectTransformUtility.WorldToScreenPoint(canvasRoot.worldCamera, from.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRoot.transform as RectTransform, fromScreenPos,
                canvasRoot.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvasRoot.worldCamera,
                out var fromLocal);
            cloneRect.localPosition = fromLocal;

            // Set icon
            var cloneImage = cloneGO.GetComponent<Image>();
            if (item.Icon != null)
            {
                cloneImage.sprite = item.Icon;
                cloneImage.color = Color.white;
            }
            else
            {
                cloneImage.sprite = null;
                cloneImage.color = StarChartTheme.GetTypeColor(item.ItemType);
            }

            var cloneCanvasGroup = cloneGO.GetComponent<CanvasGroup>();
            cloneCanvasGroup.blocksRaycasts = false;
            cloneCanvasGroup.interactable = false;

            // Compute target local position
            Vector2 toScreenPos = RectTransformUtility.WorldToScreenPoint(canvasRoot.worldCamera, to.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRoot.transform as RectTransform, toScreenPos,
                canvasRoot.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvasRoot.worldCamera,
                out var toLocal);

            // Animate: move + shrink + fade out over 350ms
            var seq = Sequence.Create()
                .Group(Tween.LocalPosition(cloneRect,
                    endValue: new Vector3(toLocal.x, toLocal.y, 0f),
                    duration: 0.35f, ease: Ease.InQuad, useUnscaledTime: true))
                .Group(Tween.Alpha(cloneCanvasGroup,
                    endValue: 0f, duration: 0.35f, ease: Ease.InQuad, useUnscaledTime: true))
                .Group(Tween.Scale(cloneRect,
                    endValue: Vector3.one * 0.6f, duration: 0.35f, ease: Ease.InQuad, useUnscaledTime: true))
                .ChainCallback(() =>
                {
                    UnityEngine.Object.Destroy(cloneGO);
                    onComplete?.Invoke();

                    // Landing bounce on target cell
                    if (to != null)
                    {
                        Tween.Scale(to, endValue: Vector3.one * 1.12f,
                            duration: 0.05f, ease: Ease.OutQuad, useUnscaledTime: true)
                            .OnComplete(() =>
                                Tween.Scale(to, endValue: Vector3.one,
                                    duration: 0.05f, ease: Ease.OutBounce, useUnscaledTime: true));
                    }
                });

            _activeAnimations.Add(seq);

            // Auto-remove from list when done
            seq.OnComplete(() => _activeAnimations.Remove(seq));
        }

        /// <summary>
        /// Immediately skip (complete) all in-flight fly-back animations.
        /// Call when a new drag begins.
        /// </summary>
        public static void SkipAll()
        {
            // Copy to a temporary list first to avoid InvalidOperationException:
            // Complete() triggers OnComplete callbacks which call _activeAnimations.Remove().
            var toComplete = new List<Sequence>(_activeAnimations);
            _activeAnimations.Clear();
            foreach (var seq in toComplete)
            {
                seq.Complete();
            }
        }
    }
}
