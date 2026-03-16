using Astrolabe.Core;
using Astrolabe.Data;
using MegaCrit.Sts2.Core.Logging;

namespace Astrolabe.Engine;

/// <summary>
/// 管理当前 Run 中所有构筑方案的可行性状态。
/// 这是「星象仪」的核心差异化特性——多路线并行 + 动态收束。
/// 
/// 可行性公式：
///   Viability = CoreCardMatch × 0.5 + SynergyRelics × 0.3 + DeckShape × 0.2
/// 
/// 状态流转：
///   Active (>=20%)  → 正常展示
///   Dominant (>=70%) → 高亮强调
///   Fading (<20%)   → 淡出/隐藏
/// </summary>
public static class BuildPathManager
{
    private static readonly Logger _log = new("Astrolabe.BuildPathManager", LogType.Generic);

    // 可行性阈值常量
    public const float DOMINANT_THRESHOLD  = 0.70f; // 确立（高亮）
    public const float ACTIVE_THRESHOLD    = 0.20f; // 活跃（正常展示）
    public const float MAX_ACTIVE_PATHS    = 3;      // 最多同时展示的方案数

    // 当前所有方案的可行性评分（key = path_id）
    private static readonly Dictionary<string, PathState> _pathStates = new();

    // 当前角色 ID（用于过滤）
    private static string _currentCharacterId = string.Empty;

    // ── 初始化 ───────────────────────────────────────────────────────

    public static void Initialize()
    {
        _pathStates.Clear();
        _log.Info("[BuildPathManager] Initialized.");
    }

    /// <summary>
    /// 根据当前快照更新所有方案的可行性评分。
    /// 在每次关键决策界面打开时调用。
    /// </summary>
    public static void UpdateViability(RunSnapshot snapshot)
    {
        if (snapshot == null || string.IsNullOrEmpty(snapshot.CharacterId))
            return;

        // 角色切换时重新加载方案池
        if (!string.Equals(_currentCharacterId, snapshot.CharacterId, StringComparison.OrdinalIgnoreCase))
        {
            LoadPathsForCharacter(snapshot.CharacterId);
            _currentCharacterId = snapshot.CharacterId;
        }

        // 更新每个方案的可行性
        foreach (var (pathId, state) in _pathStates)
        {
            var pathData = DataLoader.GetBuildPath(pathId);
            if (pathData == null) continue;

            var previousViability = state.Viability;
            state.Viability = ComputeViability(pathData, snapshot);
            state.Trend = state.Viability > previousViability ? ViabilityTrend.Rising
                        : state.Viability < previousViability ? ViabilityTrend.Falling
                        : ViabilityTrend.Stable;
        }

        _log.Info($"[BuildPathManager] Viability updated for {_pathStates.Count} paths.");
    }

    // ── 查询接口 ─────────────────────────────────────────────────────

    /// <summary>返回所有可行性 >= ACTIVE_THRESHOLD 的方案（最多 MAX_ACTIVE_PATHS 个，按可行性降序）</summary>
    public static List<PathState> GetActivePaths()
    {
        return _pathStates.Values
            .Where(s => s.Viability >= ACTIVE_THRESHOLD)
            .OrderByDescending(s => s.Viability)
            .Take((int)MAX_ACTIVE_PATHS)
            .ToList();
    }

    /// <summary>返回可行性最高的方案（主推方案）</summary>
    public static PathState? GetPrimaryPath()
        => GetActivePaths().FirstOrDefault();

    /// <summary>返回指定方案的当前状态</summary>
    public static PathState? GetPathState(string pathId)
        => _pathStates.TryGetValue(pathId, out var s) ? s : null;

    /// <summary>返回当前所有方案的可行性快照（调试/HUD用）</summary>
    public static IReadOnlyDictionary<string, PathState> AllStates => _pathStates;

    // ── 可行性计算核心 ───────────────────────────────────────────────

    /// <summary>
    /// 计算单个方案对当前牌组状态的可行性（0-1）。
    /// </summary>
    public static float ComputeViability(BuildPathData path, RunSnapshot snapshot)
    {
        var coreCardScore  = ComputeCoreCardMatch(path, snapshot);
        var relicScore     = ComputeRelicSynergy(path, snapshot);
        var deckShapeScore = ComputeDeckShape(path, snapshot);

        return coreCardScore * 0.5f + relicScore * 0.3f + deckShapeScore * 0.2f;
    }

    /// <summary>
    /// 核心牌匹配度：当前牌组中拥有多少个核心卡（0-1）。
    /// </summary>
    private static float ComputeCoreCardMatch(BuildPathData path, RunSnapshot snapshot)
    {
        if (path.CoreCards.Count == 0) return 0.5f;

        int matched = 0;
        foreach (var coreCardId in path.CoreCards)
        {
            // 支持升级变体（如 "LIMIT_BREAK" 匹配 "LIMIT_BREAK+"）

            bool hasCoreCard = snapshot.DeckCardIds.Any(id =>
                string.Equals(id, coreCardId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(id, coreCardId + "+", StringComparison.OrdinalIgnoreCase));

            if (hasCoreCard) matched++;
        }

        return (float)matched / path.CoreCards.Count;
    }

    /// <summary>
    /// 遗物协同度：当前持有多少个关键遗物（0-1）。
    /// </summary>
    private static float ComputeRelicSynergy(BuildPathData path, RunSnapshot snapshot)
    {
        if (path.KeyRelics.Count == 0) return 0.5f;

        int matched = snapshot.RelicIds
            .Count(r => path.KeyRelics.Any(kr =>
                string.Equals(r, kr, StringComparison.OrdinalIgnoreCase)));

        // 关键遗物拥有越多越好，但超过1/3就已经很好了
        float ratio = (float)matched / path.KeyRelics.Count;
        return Math.Min(1.0f, ratio * 2.5f); // 拥有40%关键遗物 → 可行性100%
    }

    /// <summary>
    /// 牌组形态匹配度：当前牌组大小是否符合方案的理想范围（0-1）。
    /// </summary>
    private static float ComputeDeckShape(BuildPathData path, RunSnapshot snapshot)
    {
        if (snapshot.DeckCardIds.Count == 0) return 0.5f;

        var ideal = path.IdealDeckSize;
        int deckSize = snapshot.DeckCardIds.Count;

        if (deckSize >= ideal.Min && deckSize <= ideal.Max)
            return 1.0f; // 在理想范围内

        // 偏离理想范围，计算惩罚
        int deviation = deckSize < ideal.Min
            ? ideal.Min - deckSize
            : deckSize - ideal.Max;

        // 每偏离1张牌损失10%，最低0
        return Math.Max(0.0f, 1.0f - deviation * 0.10f);
    }

    // ── 内部辅助 ─────────────────────────────────────────────────────

    private static void LoadPathsForCharacter(string characterId)
    {
        _pathStates.Clear();
        var paths = DataLoader.GetPathsForCharacter(characterId).ToList();

        foreach (var path in paths)
        {
            _pathStates[path.PathId] = new PathState
            {
                PathId     = path.PathId,
                NameZh     = path.NameZh,
                Viability  = 0.33f,  // 初始均等可行性
                Trend      = ViabilityTrend.Stable,
            };
        }

        _log.Info($"[BuildPathManager] Loaded {_pathStates.Count} paths for character '{characterId}'.");
    }
}

// ── 方案状态数据类 ────────────────────────────────────────────────────

/// <summary>
/// 单个构筑方案的实时运行时状态（非数据库定义，是引擎计算的结果）。
/// </summary>
public class PathState
{
    public string PathId   { get; set; } = string.Empty;
    public string NameZh   { get; set; } = string.Empty;

    /// <summary>当前可行性（0-1）</summary>
    public float Viability { get; set; }

    /// <summary>可行性百分比（0-100）</summary>
    public float ViabilityPercent => Viability * 100f;

    /// <summary>相比上次更新的趋势</summary>
    public ViabilityTrend Trend { get; set; }

    /// <summary>是否为确立状态（可行性 >= 70%）</summary>
    public bool IsDominant  => Viability >= BuildPathManager.DOMINANT_THRESHOLD;

    /// <summary>是否为活跃状态（可行性 >= 20%）</summary>
    public bool IsActive    => Viability >= BuildPathManager.ACTIVE_THRESHOLD;

    /// <summary>是否正在淡出（可行性 < 20%）</summary>
    public bool IsFading    => Viability < BuildPathManager.ACTIVE_THRESHOLD;
}

public enum ViabilityTrend { Rising, Stable, Falling }
