using UnityEngine;

namespace ProjectArk.Level
{
    /// <summary>
    /// Manages multiple Tilemap variants under a single parent.
    /// Each direct child represents one variant (e.g., "Before_Collapse", "After_Collapse").
    /// Disables all children except the active variant.
    /// 
    /// Typically invoked by WorldEventTrigger via UnityEvent to switch permanently,
    /// or by ScheduledBehaviour for phase-based swaps.
    /// </summary>
    public class TilemapVariantSwitcher : MonoBehaviour
    {
        // ──────────────────── Configuration ────────────────────

        [Header("Variants")]
        [Tooltip("Index of the default variant to show on Awake. Each direct child = one variant (index 0, 1, 2...).")]
        [SerializeField] private int _defaultVariantIndex;

        // ──────────────────── Runtime State ────────────────────

        private int _currentVariantIndex = -1;

        // ──────────────────── Public Properties ────────────────────

        /// <summary> Index of the currently active variant (-1 if none). </summary>
        public int CurrentVariantIndex => _currentVariantIndex;

        /// <summary> Total number of variants (direct child count). </summary>
        public int VariantCount => transform.childCount;

        // ──────────────────── Lifecycle ────────────────────

        private void Awake()
        {
            SwitchToVariant(_defaultVariantIndex);
        }

        // ──────────────────── Public API ────────────────────

        /// <summary>
        /// Switch to a specific variant by index. Disables all children, then enables the target.
        /// Safe to call from UnityEvent (inspector-friendly).
        /// </summary>
        /// <param name="variantIndex">0-based index matching direct child order.</param>
        public void SwitchToVariant(int variantIndex)
        {
            if (transform.childCount == 0)
            {
                Debug.LogWarning($"[TilemapVariantSwitcher] {gameObject.name}: No child variants found.");
                return;
            }

            if (variantIndex < 0 || variantIndex >= transform.childCount)
            {
                Debug.LogError($"[TilemapVariantSwitcher] {gameObject.name}: Variant index {variantIndex} out of range (0..{transform.childCount - 1}).");
                return;
            }

            // Disable all
            for (int i = 0; i < transform.childCount; i++)
            {
                transform.GetChild(i).gameObject.SetActive(false);
            }

            // Enable target
            transform.GetChild(variantIndex).gameObject.SetActive(true);
            _currentVariantIndex = variantIndex;

            Debug.Log($"[TilemapVariantSwitcher] {gameObject.name}: Switched to variant {variantIndex} ('{transform.GetChild(variantIndex).name}')");
        }

        /// <summary>
        /// Switch to the next variant (wraps around).
        /// Useful for cycling through variants.
        /// </summary>
        public void SwitchToNextVariant()
        {
            int next = (_currentVariantIndex + 1) % transform.childCount;
            SwitchToVariant(next);
        }
    }
}
