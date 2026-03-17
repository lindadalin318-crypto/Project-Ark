using System;
using System.Collections.Generic;
using System.Linq;

namespace Astrolabe.Data;

/// <summary>
/// 统一 Astrolabe 侧的 StS2 运行时 ID 规范。
/// 游戏内 canonical ID 以 <c>ModelId.Entry</c> 为准，格式为 <c>UPPER_SNAKE_CASE</c>。
/// </summary>
public static class IdNormalizer
{
    // 仅用于兼容旧版静态 JSON 中残留的 legacy `path_id` / `path_scores` 命名，
    // 当前 canonical 路径 ID 仍以 `ironclad_strength` 这类新命名为准。
    private static readonly Dictionary<string, string> LegacyPathIdMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["strength_build"] = "ironclad_strength",
        ["exhaust_build"] = "ironclad_infinite",
        ["block_build"] = "ironclad_defense",
        ["poison_build"] = "silent_poison",
        ["infinite_build"] = "silent_infinite",
        ["shiv_build"] = "silent_evade",
        ["evade_build"] = "silent_evade",
        ["discard_build"] = "silent_infinite",
        ["frost_build"] = "defect_frost",
        ["lightning_build"] = "defect_lightning",
        ["mixed_build"] = "defect_mixed",
        ["orb_build"] = "defect_mixed",
        ["divinity_build"] = "watcher_divinity",
        ["retain_build"] = "watcher_retain",
        ["miracle_build"] = "watcher_miracle",
    };

    private static readonly HashSet<string> StarterCardIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "STRIKE_IRONCLAD",
        "DEFEND_IRONCLAD",
        "STRIKE_SILENT",
        "DEFEND_SILENT",
        "STRIKE_DEFECT",
        "DEFEND_DEFECT",
        "STRIKE_WATCHER",
        "DEFEND_WATCHER",
    };

    public static string NormalizeModelId(string? rawId, bool preserveUpgradeSuffix = true)
    {
        if (string.IsNullOrWhiteSpace(rawId))
            return string.Empty;

        string trimmed = rawId.Trim();
        bool hasUpgradeSuffix = preserveUpgradeSuffix && trimmed.EndsWith('+');
        string baseId = hasUpgradeSuffix ? trimmed[..^1] : trimmed;

        return Slugify(baseId) + (hasUpgradeSuffix ? "+" : string.Empty);
    }

    public static string NormalizeLookupId(string? rawId)
        => NormalizeModelId(StripUpgradeSuffix(rawId), preserveUpgradeSuffix: false);

    public static string NormalizeCharacterId(string? rawId)
        => NormalizeModelId(rawId, preserveUpgradeSuffix: false);

    public static string NormalizePathId(string? rawId)
        => string.IsNullOrWhiteSpace(rawId)
            ? string.Empty
            : rawId.Trim().Replace('-', '_').ToLowerInvariant();

    public static string StripUpgradeSuffix(string? rawId)
        => string.IsNullOrWhiteSpace(rawId)
            ? string.Empty
            : rawId.TrimEnd('+');

    public static bool IsStarterStrikeOrDefend(string? rawId)
        => StarterCardIds.Contains(NormalizeLookupId(rawId));

    public static void NormalizeCardCore(CardCoreData card)
    {
        if (card == null)
            return;

        card.CardId = NormalizeLookupId(card.CardId);
        card.Character = NormalizeCharacterId(card.Character);
        NormalizeCreatedCards(card.Effects?.Base);
        NormalizeCreatedCards(card.Effects?.Upgraded);
    }

    public static void NormalizeCardAdvisor(CardAdvisorData card)
    {
        if (card == null)
            return;

        string inferredCharacter = InferCharacterFromPathScores(card.PathScores.Keys);
        card.Character = NormalizeCharacterId(string.IsNullOrWhiteSpace(card.Character) ? inferredCharacter : card.Character);
        card.CardId = NormalizeLookupId(card.CardId);
        card.PathScores = NormalizePathScores(card.PathScores);
        card.LegacyPathScores = NormalizeLoosePathScores(card.LegacyPathScores);
        card.Advisor.Roles = NormalizeStringList(card.Advisor.Roles);
        card.Advisor.PickupWindows = NormalizeStringList(card.Advisor.PickupWindows, toLowerInvariant: true);
    }

    public static void NormalizeCard(CardData card)
    {
        if (card == null)
            return;

        string inferredCharacter = InferCharacterFromPathScores(card.PathScores.Keys);
        card.Character = NormalizeCharacterId(string.IsNullOrWhiteSpace(card.Character) ? inferredCharacter : card.Character);
        card.CardId = NormalizeLookupId(card.CardId);
        card.PathScores = NormalizePathScores(card.PathScores);
        card.LegacyPathScores = NormalizeLoosePathScores(card.LegacyPathScores);
        card.SynergyTags = NormalizeStringList(card.SynergyTags, toLowerInvariant: true);
        card.AntiSynergyTags = NormalizeStringList(card.AntiSynergyTags, toLowerInvariant: true);
        card.Advisor.Roles = NormalizeStringList(card.Advisor.Roles);
        card.Advisor.PickupWindows = NormalizeStringList(card.Advisor.PickupWindows, toLowerInvariant: true);
        NormalizeCreatedCards(card.Effects?.Base);
        NormalizeCreatedCards(card.Effects?.Upgraded);
    }

    public static void NormalizeRelic(RelicData relic)
    {
        if (relic == null)
            return;

        if (!string.IsNullOrWhiteSpace(relic.Character) && !string.Equals(relic.Character, "Shared", StringComparison.OrdinalIgnoreCase))
            relic.Character = NormalizeCharacterId(relic.Character);
        else if (string.Equals(relic.Character, "Shared", StringComparison.OrdinalIgnoreCase))
            relic.Character = "Shared";

        relic.RelicId = NormalizeLookupId(relic.RelicId);
        relic.PathScores = NormalizePathScores(relic.PathScores);
    }

    public static void NormalizeBuildPath(BuildPathData path)
    {
        if (path == null)
            return;

        path.PathId = NormalizePathId(path.PathId);
        path.Character = NormalizeCharacterId(path.Character);
        path.CoreCards = path.CoreCards.Select(NormalizeLookupId).Where(id => !string.IsNullOrEmpty(id)).ToList();
        path.KeyRelics = path.KeyRelics.Select(NormalizeLookupId).Where(id => !string.IsNullOrEmpty(id)).ToList();
        path.CampfireUpgrades = path.CampfireUpgrades.Select(NormalizeLookupId).Where(id => !string.IsNullOrEmpty(id)).ToList();
        path.GoodAgainstBosses = path.GoodAgainstBosses.Select(NormalizeLookupId).Where(id => !string.IsNullOrEmpty(id)).ToList();
        path.BadAgainstBosses = path.BadAgainstBosses.Select(NormalizeLookupId).Where(id => !string.IsNullOrEmpty(id)).ToList();
    }

    public static void NormalizeBoss(BossData boss)
    {
        if (boss == null)
            return;

        boss.BossId = NormalizeLookupId(boss.BossId);
        boss.CounterCards = boss.CounterCards.Select(NormalizeLookupId).Where(id => !string.IsNullOrEmpty(id)).ToList();
        boss.DangerousToPaths = boss.DangerousToPaths.Select(NormalizePathId).Where(id => !string.IsNullOrEmpty(id)).ToList();
    }

    private static Dictionary<string, float> NormalizePathScores(Dictionary<string, float>? pathScores)
    {
        if (pathScores == null || pathScores.Count == 0)
            return new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        var normalized = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        foreach (var (rawKey, score) in pathScores)
        {
            string key = NormalizePathId(rawKey);
            if (LegacyPathIdMap.TryGetValue(key, out string? remapped))
                key = remapped;

            if (key == "burn_build")
                continue;

            normalized[key] = score;
        }

        return normalized;
    }

    private static Dictionary<string, float> NormalizeLoosePathScores(Dictionary<string, float>? pathScores)
    {
        if (pathScores == null || pathScores.Count == 0)
            return new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        var normalized = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        foreach (var (rawKey, score) in pathScores)
        {
            string key = NormalizePathId(rawKey);
            normalized[key] = score;
        }

        return normalized;
    }

    private static void NormalizeCreatedCards(CardEffectProfile? profile)
    {
        if (profile == null || profile.CreateCards.Count == 0)
            return;

        profile.CreateCards = profile.CreateCards
            .Select(NormalizeLookupId)
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<string> NormalizeStringList(IEnumerable<string>? values, bool toLowerInvariant = false)
    {
        if (values == null)
            return new List<string>();

        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => toLowerInvariant ? value.Trim().ToLowerInvariant() : value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string InferCharacterFromPathScores(IEnumerable<string> keys)
    {
        var normalizedKeys = keys.Select(NormalizePathId).ToList();

        if (normalizedKeys.Any(key => key.StartsWith("ironclad_", StringComparison.OrdinalIgnoreCase)) ||
            normalizedKeys.Any(key => key is "strength_build" or "exhaust_build" or "block_build" or "burn_build"))
            return "IRONCLAD";

        if (normalizedKeys.Any(key => key.StartsWith("silent_", StringComparison.OrdinalIgnoreCase)) ||
            normalizedKeys.Any(key => key is "poison_build" or "infinite_build" or "shiv_build" or "evade_build" or "discard_build"))
            return "SILENT";

        if (normalizedKeys.Any(key => key.StartsWith("defect_", StringComparison.OrdinalIgnoreCase)) ||
            normalizedKeys.Any(key => key is "frost_build" or "lightning_build" or "mixed_build" or "orb_build"))
            return "DEFECT";

        if (normalizedKeys.Any(key => key.StartsWith("watcher_", StringComparison.OrdinalIgnoreCase)) ||
            normalizedKeys.Any(key => key is "divinity_build" or "retain_build" or "miracle_build"))
            return "WATCHER";

        return string.Empty;
    }

    private static string Slugify(string value)
    {
        Span<char> buffer = stackalloc char[value.Length * 2];
        int index = 0;

        for (int i = 0; i < value.Length; i++)
        {
            char current = value[i];
            if (char.IsWhiteSpace(current) || current == '-')
            {
                if (index > 0 && buffer[index - 1] != '_')
                    buffer[index++] = '_';
                continue;
            }

            if (current == '_')
            {
                if (index > 0 && buffer[index - 1] != '_')
                    buffer[index++] = '_';
                continue;
            }

            bool shouldInsertUnderscore = i > 0 && char.IsUpper(current) &&
                (char.IsLower(value[i - 1]) || char.IsDigit(value[i - 1]));
            if (shouldInsertUnderscore && index > 0 && buffer[index - 1] != '_')
                buffer[index++] = '_';

            if (char.IsLetterOrDigit(current))
                buffer[index++] = char.ToUpperInvariant(current);
        }

        return new string(buffer[..index]).Trim('_');
    }
}
