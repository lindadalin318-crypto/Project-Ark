using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using MegaCrit.Sts2.Core.Logging;

namespace Astrolabe.Data;

/// <summary>
/// 从 data/ 目录加载所有 JSON 数据库并缓存到静态属性。
/// 卡牌数据采用双源架构：
/// - <c>cards.core.json</c>：客观事实层
/// - <c>cards.advisor.json</c>：顾问先验层
/// 运行时将两者合并为统一的 <see cref="CardData"/> 视图，供业务层透明消费。
/// </summary>
public static class DataLoader
{
    private static readonly Logger _log = new("Astrolabe.DataLoader", LogType.Generic);

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    // ── 公开缓存 ────────────────────────────────────────────────────

    public static Dictionary<string, CardCoreData> CardCore { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
    public static Dictionary<string, CardAdvisorData> CardAdvisorEntries { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
    public static Dictionary<string, CardData> Cards { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
    public static Dictionary<string, RelicData> Relics { get; private set; } = new();
    public static Dictionary<string, BuildPathData> BuildPaths { get; private set; } = new();
    public static Dictionary<string, BossData> Bosses { get; private set; } = new();
    public static Dictionary<string, EventData> Events { get; private set; } = new();

    // ── 加载入口 ────────────────────────────────────────────────────

    public static void LoadAll()
    {
        var dataDir = GetDataDirectory();
        _log.Info($"[DataLoader] Loading from: {dataDir}");

        CardCore = LoadDict<CardCoreData>(
            Path.Combine(dataDir, "cards.core.json"),
            d =>
            {
                IdNormalizer.NormalizeCardCore(d);
                return d.CardId;
            });

        CardAdvisorEntries = LoadDict<CardAdvisorData>(
            Path.Combine(dataDir, "cards.advisor.json"),
            d =>
            {
                IdNormalizer.NormalizeCardAdvisor(d);
                return d.CardId;
            });

        Cards = MergeCards(CardCore, CardAdvisorEntries);

        Relics = LoadDict<RelicData>(
            Path.Combine(dataDir, "relics.json"),
            d =>
            {
                IdNormalizer.NormalizeRelic(d);
                return d.RelicId;
            });

        BuildPaths = LoadDict<BuildPathData>(
            Path.Combine(dataDir, "buildpaths.json"),
            d =>
            {
                IdNormalizer.NormalizeBuildPath(d);
                return d.PathId;
            });

        Bosses = LoadDict<BossData>(
            Path.Combine(dataDir, "bosses.json"),
            d =>
            {
                IdNormalizer.NormalizeBoss(d);
                return d.BossId;
            });

        Events = LoadDict<EventData>(
            Path.Combine(dataDir, "events.json"),
            d => IdNormalizer.NormalizePathId(d.EventId));
    }

    // ── 辅助方法 ────────────────────────────────────────────────────

    /// <summary>
    /// 数据目录解析：优先查找 dll 同目录下的 data/，其次查找 Assembly 所在目录。
    /// </summary>
    private static string GetDataDirectory()
    {
        var execDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".";
        var candidate = Path.Combine(execDir, "data");
        if (Directory.Exists(candidate))
            return candidate;

        candidate = Path.Combine(Directory.GetCurrentDirectory(), "data");
        if (Directory.Exists(candidate))
            return candidate;

        throw new DirectoryNotFoundException(
            $"[DataLoader] Cannot find data/ directory. Searched:\n" +
            $"  {Path.Combine(execDir, "data")}\n" +
            $"  {Path.Combine(Directory.GetCurrentDirectory(), "data")}");
    }

    private static Dictionary<string, T> LoadDict<T>(string filePath, Func<T, string> keySelector)
    {
        if (!File.Exists(filePath))
        {
            _log.Warn($"[DataLoader] File not found, skipping: {filePath}");
            return new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var json = File.ReadAllText(filePath);
            var list = TryParseList<T>(json, filePath);
            if (list == null)
            {
                _log.Warn($"[DataLoader] Empty or null list in: {filePath}");
                return new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
            }

            var dict = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in list)
            {
                var key = keySelector(item);
                if (string.IsNullOrEmpty(key))
                {
                    _log.Warn($"[DataLoader] Item with empty key in: {filePath}, skipping.");
                    continue;
                }

                dict[key] = item;
            }

            _log.Info($"[DataLoader] Loaded {dict.Count} entries from {Path.GetFileName(filePath)}");
            return dict;
        }
        catch (Exception ex)
        {
            _log.Error($"[DataLoader] Failed to load {filePath}: {ex.Message}");
            return new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// 兼容两种 JSON 根格式：
    ///   1. 裸数组：[{ ... }, ...]
    ///   2. 包装对象：{ "version": "...", "cards": [{ ... }] }
    ///      包装对象时，自动找第一个 array 类型的属性值作为数据列表。
    /// </summary>
    private static List<T>? TryParseList<T>(string json, string filePath)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.ValueKind == JsonValueKind.Array)
            return JsonSerializer.Deserialize<List<T>>(json, _jsonOptions);

        if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in root.EnumerateObject())
            {
                if (prop.Value.ValueKind != JsonValueKind.Array)
                    continue;

                try
                {
                    var list = JsonSerializer.Deserialize<List<T>>(prop.Value.GetRawText(), _jsonOptions);
                    if (list != null && list.Count > 0)
                    {
                        _log.Info($"[DataLoader] Parsed wrapped array from field '{prop.Name}' in {Path.GetFileName(filePath)}");
                        return list;
                    }
                }
                catch (Exception ex)
                {
                    _log.Warn($"[DataLoader] Field '{prop.Name}' in {Path.GetFileName(filePath)} failed: {ex.Message}");
                }
            }
        }

        return null;
    }

    private static Dictionary<string, CardData> MergeCards(
        IReadOnlyDictionary<string, CardCoreData> coreCards,
        IReadOnlyDictionary<string, CardAdvisorData> advisorCards)
    {
        var merged = new Dictionary<string, CardData>(StringComparer.OrdinalIgnoreCase);
        var keys = new HashSet<string>(coreCards.Keys, StringComparer.OrdinalIgnoreCase);
        keys.UnionWith(advisorCards.Keys);

        foreach (var key in keys)
        {
            coreCards.TryGetValue(key, out var core);
            advisorCards.TryGetValue(key, out var advisor);

            var card = new CardData
            {
                CardId = key,
                NameZh = core?.NameZh ?? string.Empty,
                NameEn = core?.NameEn ?? string.Empty,
                Character = !string.IsNullOrWhiteSpace(core?.Character)
                    ? core!.Character
                    : advisor?.Character ?? string.Empty,
                Cost = core?.Cost ?? 1,
                Type = core?.Type ?? "Skill",
                Rarity = core?.Rarity ?? "Common",
                Target = core?.Target ?? string.Empty,
                Effects = core?.Effects ?? new CardEffectSet(),
                Flags = core?.Flags ?? new CardFlagsData(),
                BaseScore = advisor?.BaseScore ?? 0f,
                Tier = advisor?.Tier ?? "C",
                PathScores = advisor?.PathScores ?? new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase),
                LegacyPathScores = advisor?.LegacyPathScores ?? new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase),
                SynergyTags = advisor?.SynergyTags ?? new List<string>(),
                AntiSynergyTags = advisor?.AntiSynergyTags ?? new List<string>(),
                ActScaling = advisor?.ActScaling ?? new List<float> { 1f, 1f, 1f },
                UpgradePriority = advisor?.UpgradePriority ?? "medium",
                UpgradeDeltaZh = advisor?.UpgradeDeltaZh ?? string.Empty,
                NotesZh = advisor?.NotesZh ?? string.Empty,
                Advisor = advisor?.Advisor ?? new CardAdvisorMetadata(),
            };

            IdNormalizer.NormalizeCard(card);
            merged[card.CardId] = card;
        }

        _log.Info($"[DataLoader] Merged {merged.Count} card records from cards.core.json + cards.advisor.json");
        return merged;
    }

    // ── 便捷查询 ────────────────────────────────────────────────────

    public static CardData? GetCard(string cardId) =>
        Cards.TryGetValue(IdNormalizer.NormalizeLookupId(cardId), out var d) ? d : null;

    public static RelicData? GetRelic(string relicId) =>
        Relics.TryGetValue(IdNormalizer.NormalizeLookupId(relicId), out var d) ? d : null;

    public static BuildPathData? GetBuildPath(string pathId) =>
        BuildPaths.TryGetValue(IdNormalizer.NormalizePathId(pathId), out var d) ? d : null;

    public static BossData? GetBoss(string bossId) =>
        Bosses.TryGetValue(IdNormalizer.NormalizeLookupId(bossId), out var d) ? d : null;

    /// <summary>返回指定角色的所有构筑方案</summary>
    public static IEnumerable<BuildPathData> GetPathsForCharacter(string characterId)
    {
        string normalizedCharacterId = IdNormalizer.NormalizeCharacterId(characterId);
        return BuildPaths.Values.Where(p =>
            string.Equals(p.Character, normalizedCharacterId, StringComparison.OrdinalIgnoreCase));
    }
}
