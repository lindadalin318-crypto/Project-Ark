using System.Text.Json;
using System.Reflection;
using MegaCrit.Sts2.Core.Logging;

namespace Astrolabe.Data;

/// <summary>
/// 从 data/ 目录加载所有 JSON 数据库并缓存到静态属性。
/// 数据库文件在构建时被复制到输出目录的 data/ 子目录。
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

    public static Dictionary<string, CardData> Cards { get; private set; } = new();
    public static Dictionary<string, RelicData> Relics { get; private set; } = new();
    public static Dictionary<string, BuildPathData> BuildPaths { get; private set; } = new();
    public static Dictionary<string, BossData> Bosses { get; private set; } = new();
    public static Dictionary<string, EventData> Events { get; private set; } = new();

    // ── 加载入口 ────────────────────────────────────────────────────

    public static void LoadAll()
    {
        var dataDir = GetDataDirectory();
        _log.Info($"[DataLoader] Loading from: {dataDir}");

        Cards      = LoadDict<CardData>     (Path.Combine(dataDir, "cards.json"),      d => d.CardId);
        Relics     = LoadDict<RelicData>    (Path.Combine(dataDir, "relics.json"),     d => d.RelicId);
        BuildPaths = LoadDict<BuildPathData>(Path.Combine(dataDir, "buildpaths.json"), d => d.PathId);
        Bosses     = LoadDict<BossData>     (Path.Combine(dataDir, "bosses.json"),     d => d.BossId);
        Events     = LoadDict<EventData>    (Path.Combine(dataDir, "events.json"),     d => d.EventId);
    }

    // ── 辅助方法 ────────────────────────────────────────────────────

    /// <summary>
    /// 数据目录解析：优先查找 dll 同目录下的 data/，其次查找 Assembly 所在目录。
    /// </summary>
    private static string GetDataDirectory()
    {
        // 方式1：可执行文件同目录下的 data/
        var execDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".";
        var candidate = Path.Combine(execDir, "data");
        if (Directory.Exists(candidate))
            return candidate;

        // 方式2：当前工作目录下的 data/
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
            return new Dictionary<string, T>();
        }

        try
        {
            var json = File.ReadAllText(filePath);
            var list = JsonSerializer.Deserialize<List<T>>(json, _jsonOptions);
            if (list == null)
            {
                _log.Warn($"[DataLoader] Empty or null list in: {filePath}");
                return new Dictionary<string, T>();
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
            return new Dictionary<string, T>();
        }
    }

    // ── 便捷查询 ────────────────────────────────────────────────────

    public static CardData? GetCard(string cardId) =>
        Cards.TryGetValue(cardId, out var d) ? d : null;

    public static RelicData? GetRelic(string relicId) =>
        Relics.TryGetValue(relicId, out var d) ? d : null;

    public static BuildPathData? GetBuildPath(string pathId) =>
        BuildPaths.TryGetValue(pathId, out var d) ? d : null;

    public static BossData? GetBoss(string bossId) =>
        Bosses.TryGetValue(bossId, out var d) ? d : null;

    /// <summary>返回指定角色的所有构筑方案</summary>
    public static IEnumerable<BuildPathData> GetPathsForCharacter(string characterId) =>
        BuildPaths.Values.Where(p =>
            string.Equals(p.Character, characterId, StringComparison.OrdinalIgnoreCase));
}
