using UnityEngine;
using ProjectArk.Combat;

namespace ProjectArk.UI
{
    /// <summary>
    /// Centralized color theme for the Star Chart UI.
    /// All views must obtain colors from here — no hardcoded hex values elsewhere.
    /// </summary>
    public static class StarChartTheme
    {
        // ── Background & Structure ──────────────────────────────────────────────
        /// <summary> Deep background: #0b1120 </summary>
        public static readonly Color BgPanel = new Color(0.043f, 0.067f, 0.125f, 0.97f);

        /// <summary> Track area background: #0f1828 </summary>
        public static readonly Color BgTrack = new Color(0.059f, 0.094f, 0.157f, 0.92f);

        /// <summary> Deep background: #0d1117, alpha 0.95 (legacy alias) </summary>
        public static readonly Color BgDeep = new Color(0.051f, 0.067f, 0.090f, 0.95f);

        /// <summary> Panel border / divider line: dim white </summary>
        public static readonly Color Border = new Color(1f, 1f, 1f, 0.12f);

        /// <summary> Corner bracket decoration: cyan #22d3ee, 2px L-shape </summary>
        public static readonly Color CornerBracket = new Color(0.133f, 0.827f, 0.933f, 0.85f);

        // ── Accent ─────────────────────────────────────────────────────────────
        /// <summary> Primary accent cyan: #00e5ff </summary>
        public static readonly Color Cyan = new Color(0f, 0.898f, 1f, 1f);

        /// <summary> Dim cyan for secondary text </summary>
        public static readonly Color CyanDim = new Color(0f, 0.898f, 1f, 0.45f);

        /// <summary> Status green (online indicator) </summary>
        public static readonly Color StatusGreen = new Color(0.2f, 0.9f, 0.4f, 1f);

        // ── Item Type Colors ────────────────────────────────────────────────────
        /// <summary> CORE blue: #60a5fa </summary>
        public static readonly Color CoreColor = new Color(0.376f, 0.647f, 0.980f, 1f);

        /// <summary> PRISM purple: #a78bfa </summary>
        public static readonly Color PrismColor = new Color(0.655f, 0.545f, 0.980f, 1f);

        /// <summary> SAIL green: #34d399 </summary>
        public static readonly Color SailColor = new Color(0.204f, 0.827f, 0.600f, 1f);

        /// <summary> SAT yellow: #fbbf24 </summary>
        public static readonly Color SatColor = new Color(0.984f, 0.749f, 0.141f, 1f);

        // ── Type Color Dim variants (for backgrounds, low-alpha fills) ──────────
        public static Color CoreColorDim   => new Color(CoreColor.r,  CoreColor.g,  CoreColor.b,  0.18f);
        public static Color PrismColorDim  => new Color(PrismColor.r, PrismColor.g, PrismColor.b, 0.18f);
        public static Color SailColorDim   => new Color(SailColor.r,  SailColor.g,  SailColor.b,  0.18f);
        public static Color SatColorDim    => new Color(SatColor.r,   SatColor.g,   SatColor.b,   0.18f);

        // ── Slot Cell States ────────────────────────────────────────────────────
        /// <summary> Empty slot background </summary>
        public static readonly Color SlotEmpty    = new Color(0.10f, 0.12f, 0.16f, 0.85f);

        /// <summary> Spanned (secondary) slot background </summary>
        public static readonly Color SlotSpanned  = new Color(0.12f, 0.14f, 0.20f, 0.70f);

        /// <summary> Valid drop highlight (green) </summary>
        public static readonly Color HighlightValid   = new Color(0.2f, 0.85f, 0.4f, 0.85f);

        /// <summary> Invalid drop highlight (red) </summary>
        public static readonly Color HighlightInvalid = new Color(0.85f, 0.2f, 0.2f, 0.85f);

        /// <summary> Replace drop highlight (orange) </summary>
        public static readonly Color HighlightReplace = new Color(1f, 0.55f, 0.1f, 0.85f);

        // ── Status Bar ──────────────────────────────────────────────────────────
        /// <summary> Default idle text color (very dim) </summary>
        public static readonly Color StatusIdle  = new Color(1f, 1f, 1f, 0.30f);

        /// <summary> Normal message color </summary>
        public static readonly Color StatusNormal = Color.white;

        /// <summary> Error / failure message color </summary>
        public static readonly Color StatusError = new Color(1f, 0.35f, 0.35f, 1f);

        // ── Inventory ──────────────────────────────────────────────────────────
        /// <summary> Equipped badge / border green </summary>
        public static readonly Color EquippedGreen = new Color(0.2f, 0.85f, 0.4f, 0.55f);

        /// <summary> Selected item border cyan glow </summary>
        public static readonly Color SelectedCyan = Cyan;

        // ── API ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the full-opacity theme color for the given item type.
        /// </summary>
        public static Color GetTypeColor(StarChartItemType type)
        {
            return type switch
            {
                StarChartItemType.Core      => CoreColor,
                StarChartItemType.Prism     => PrismColor,
                StarChartItemType.LightSail => SailColor,
                StarChartItemType.Satellite => SatColor,
                _                           => Color.white,
            };
        }

        /// <summary>
        /// Returns the dim (low-alpha background fill) variant for the given item type.
        /// </summary>
        public static Color GetTypeColorDim(StarChartItemType type)
        {
            return type switch
            {
                StarChartItemType.Core      => CoreColorDim,
                StarChartItemType.Prism     => PrismColorDim,
                StarChartItemType.LightSail => SailColorDim,
                StarChartItemType.Satellite => SatColorDim,
                _                           => new Color(1f, 1f, 1f, 0.18f),
            };
        }
    }
}
