using Astrolabe.Core;
using Astrolabe.Data;

namespace Astrolabe.Engine;

/// <summary>
/// 为地图路线提供建议，基于当前跑分状态和活跃构筑方案计算节点评分。
/// </summary>
public static class MapAdvisor
{
    // 节点基础期望值（社区经验数据）
    private static readonly Dictionary<NodeType, float> BaseNodeValues = new()
    {
        { NodeType.Elite,    18f },  // 含遗物奖励
        { NodeType.Shop,     12f },  // 含删牌价值
        { NodeType.Campfire, 10f },  // 休息/升级
        { NodeType.Event,     6f },  // 含负面风险后的期望
        { NodeType.Monster,   3f },  // 普通战斗
        { NodeType.Boss,      0f },  // 必经，不参与路线评分
    };

    public static MapAdvice Analyze(RunSnapshot snapshot, IReadOnlyList<PathState> activePaths)
    {
        var advice = new MapAdvice();

        float hpFactor = ComputeHpFactor(snapshot);

        // 为各活跃方案生成推荐路线
        foreach (var path in activePaths)
        {
            var pathData = DataLoader.GetBuildPath(path.PathId);
            var routeAdvice = new PathRouteAdvice
            {
                PathId   = path.PathId,
                PathName = path.NameZh,
            };

            // 评估当前可走的节点
            // TODO: 实际节点数据从 MapStateReader 获取，此处使用占位说明
            routeAdvice.Recommendation = BuildRouteRecommendation(pathData, snapshot, hpFactor);
            advice.PathRoutes.Add(routeAdvice);
        }

        advice.GlobalNote = BuildGlobalNote(snapshot, hpFactor);
        return advice;
    }

    private static float ComputeHpFactor(RunSnapshot snapshot)
    {
        if (snapshot.MaxHP <= 0) return 1.0f;
        float hpRatio = (float)snapshot.HP / snapshot.MaxHP;

        return hpRatio switch
        {
            > 0.70f => 1.2f,  // 高HP：可以冒险打精英
            > 0.40f => 1.0f,  // 中等HP：正常评估
            _       => 0.6f,  // 低HP：精英战大幅降权
        };
    }

    public static float ScoreNode(NodeType nodeType, BuildPathData? pathData, RunSnapshot snapshot, float hpFactor)
    {
        float baseValue = BaseNodeValues.TryGetValue(nodeType, out var v) ? v : 0f;

        // 方案对节点的权重偏好
        float pathWeight = nodeType switch
        {
            NodeType.Elite when pathData != null => pathData.EliteWeight,
            NodeType.Shop  when pathData != null => pathData.ShopWeight,
            _                                    => 1.0f,
        };

        // 精英战：低HP时大幅降权
        if (nodeType == NodeType.Elite)
            return baseValue * pathWeight * hpFactor;

        return baseValue * pathWeight;
    }

    private static string BuildRouteRecommendation(BuildPathData? pathData, RunSnapshot snapshot, float hpFactor)
    {
        if (pathData == null) return "无路线建议。";

        var suggestions = new List<string>();

        // 商店需求判断
        bool needsShop = NeedsShop(snapshot, pathData);
        if (needsShop)
            suggestions.Add("优先经过商店（删牌/购买关键牌）");

        // HP 状态建议
        if (snapshot.MaxHP > 0 && (float)snapshot.HP / snapshot.MaxHP < 0.4f)
            suggestions.Add("HP偏低，优先经过篝火休息，避免精英战");
        else if (hpFactor >= 1.2f)
            suggestions.Add("HP充足，可以考虑打精英获取遗物");

        // Boss 针对性
        if (!string.IsNullOrEmpty(snapshot.ActBossId))
        {
            var bossData = DataLoader.GetBoss(snapshot.ActBossId);
            if (bossData != null)
            {
                bool isBadMatchup = bossData.DangerousToPaths.Contains(pathData.PathId);
                if (isBadMatchup)
                    suggestions.Add($"注意：{bossData.NameZh}克制{pathData.NameZh}，考虑调整构筑");
            }
        }

        return suggestions.Count > 0
            ? string.Join(" / ", suggestions)
            : $"按{pathData.NameZh}方案正常推进。";
    }

    private static bool NeedsShop(RunSnapshot snapshot, BuildPathData pathData)
    {
        // 牌组超过理想上限：需要删牌
        bool deckTooLarge = snapshot.DeckCardIds.Count > pathData.IdealDeckSize.Max;
        // 缺少关键牌且金币充足
        bool missingCoreCards = pathData.CoreCards
            .Any(c => !snapshot.DeckCardIds.Any(d =>
                d.Equals(c, StringComparison.OrdinalIgnoreCase) ||
                d.Equals(c + "+", StringComparison.OrdinalIgnoreCase)));

        return deckTooLarge || (missingCoreCards && snapshot.Gold >= 100);
    }

    private static string BuildGlobalNote(RunSnapshot snapshot, float hpFactor)
    {
        if (hpFactor < 0.6f)
            return $"⚠ 当前 HP 较低 ({snapshot.HP}/{snapshot.MaxHP})，优先寻找篝火回血";
        if (snapshot.Act == 3)
            return "Act3：准备面对最终 Boss，优先补强薄弱点";
        return string.Empty;
    }
}

// ── 地图建议数据类 ─────────────────────────────────────────────────────

public class MapAdvice
{
    public List<PathRouteAdvice> PathRoutes { get; set; } = new();
    public string GlobalNote { get; set; } = string.Empty;
}

public class PathRouteAdvice
{
    public string PathId        { get; set; } = string.Empty;
    public string PathName      { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
}

public enum NodeType
{
    Monster,
    Elite,
    Shop,
    Campfire,
    Event,
    Boss,
    Treasure,
}
