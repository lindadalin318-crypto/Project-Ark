using System.Text;
using ProjectArk.Combat;

namespace ProjectArk.UI
{
    /// <summary>
    /// Static utility that builds the stats text block for <see cref="ItemTooltipView"/>.
    /// Each item type produces a different set of attribute rows with ▲/▼ arrows.
    /// </summary>
    public static class TooltipContentBuilder
    {
        /// <summary>
        /// Build the stats text for the given item.
        /// Returns a multi-line string with ▲/▼ arrows and values.
        /// </summary>
        public static string BuildStatsText(StarChartItemSO item)
        {
            if (item == null) return string.Empty;

            var sb = new StringBuilder();

            switch (item)
            {
                case StarCoreSO core:
                    BuildCoreStats(sb, core);
                    break;

                case PrismSO prism:
                    BuildPrismStats(sb, prism);
                    break;

                case LightSailSO sail:
                    BuildSailStats(sb, sail);
                    break;

                case SatelliteSO sat:
                    BuildSatelliteStats(sb, sat);
                    break;
            }

            // All types: append HEAT if HeatCost > 0
            if (item.HeatCost > 0f)
            {
                if (sb.Length > 0) sb.AppendLine();
                sb.Append($"HEAT  ▲ {item.HeatCost:F1}");
            }

            return sb.ToString();
        }

        // ── Core ─────────────────────────────────────────────────────────────

        private static void BuildCoreStats(StringBuilder sb, StarCoreSO core)
        {
            sb.AppendLine($"DAMAGE    ▲ {core.BaseDamage:F0}");
            sb.AppendLine($"FIRE RATE ▲ {core.FireRate:F1}/s");
            sb.AppendLine($"SPEED     ▲ {core.ProjectileSpeed:F0}");
            // Heat is handled by the shared block below, skip here
        }

        // ── Prism ─────────────────────────────────────────────────────────────

        private static void BuildPrismStats(StringBuilder sb, PrismSO prism)
        {
            var mods = prism.StatModifiers;
            if (mods == null || mods.Length == 0)
            {
                sb.Append("No stat modifiers");
                return;
            }

            for (int i = 0; i < mods.Length; i++)
            {
                var mod = mods[i];
                string statName = GetStatName(mod.Stat);
                string arrow;
                string valueStr;

                if (mod.Operation == ModifierOperation.Add)
                {
                    arrow = mod.Value >= 0f ? "▲" : "▼";
                    valueStr = mod.Value >= 0f
                        ? $"+{mod.Value:F1}"
                        : $"{mod.Value:F1}";
                }
                else // Multiply
                {
                    arrow = mod.Value >= 1f ? "▲" : "▼";
                    valueStr = $"×{mod.Value:F2}";
                }

                if (i < mods.Length - 1)
                    sb.AppendLine($"{statName,-12} {arrow} {valueStr}");
                else
                    sb.Append($"{statName,-12} {arrow} {valueStr}");
            }
        }

        // ── Light Sail ────────────────────────────────────────────────────────

        private static void BuildSailStats(StringBuilder sb, LightSailSO sail)
        {
            // Show condition + effect description (no arrows)
            if (!string.IsNullOrEmpty(sail.ConditionDescription))
            {
                sb.AppendLine($"WHEN: {sail.ConditionDescription}");
            }
            if (!string.IsNullOrEmpty(sail.EffectDescription))
            {
                sb.Append(sail.EffectDescription);
            }
            else
            {
                sb.Append("No effect description");
            }
        }

        // ── Satellite ─────────────────────────────────────────────────────────

        private static void BuildSatelliteStats(StringBuilder sb, SatelliteSO sat)
        {
            if (!string.IsNullOrEmpty(sat.TriggerDescription))
                sb.AppendLine($"TRIGGER  {sat.TriggerDescription}");

            if (!string.IsNullOrEmpty(sat.ActionDescription))
                sb.AppendLine($"ACTION   {sat.ActionDescription}");

            sb.Append($"COOLDOWN ▲ {sat.InternalCooldown:F1}s");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static string GetStatName(WeaponStatType stat)
        {
            return stat switch
            {
                WeaponStatType.Damage          => "DAMAGE",
                WeaponStatType.FireRate        => "FIRE RATE",
                WeaponStatType.ProjectileSpeed => "SPEED",
                WeaponStatType.Lifetime        => "LIFETIME",
                WeaponStatType.Spread          => "SPREAD",
                WeaponStatType.Knockback       => "KNOCKBACK",
                WeaponStatType.RecoilForce     => "RECOIL",
                WeaponStatType.ProjectileCount => "COUNT",
                WeaponStatType.ProjectileSize  => "SIZE",
                WeaponStatType.HeatCost        => "HEAT",
                _                              => stat.ToString().ToUpper()
            };
        }
    }
}
