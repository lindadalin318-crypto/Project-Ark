using Astrolabe.Core;
using Astrolabe.Data;

namespace Astrolabe.Engine;

/// <summary>
/// 为选牌界面中的候选卡牌生成推荐评级。
/// 对每张候选牌在每个活跃方案下分别评分，返回多维度建议。
/// </summary>
public static class CardAdvisor
{
    /// <summary>
    /// 为候选卡牌列表生成建议。
    /// </summary>
    /// <param name="candidateCardIds">候选卡牌 ID 列表（来自游戏奖励界面）</param>
    /// <param name="snapshot">当前跑分状态</param>
    /// <param name="activePaths">当前活跃的构筑方案（来自 BuildPathManager）</param>
    public static List<CardAdvice> Analyze(
        IReadOnlyList<string> candidateCardIds,
        RunSnapshot snapshot,
        IReadOnlyList<PathState> activePaths)
    {
        var results = new List<CardAdvice>();

        foreach (var cardId in candidateCardIds)
        {
            var cardData = DataLoader.GetCard(cardId);
            var advice = new CardAdvice
            {
                CardId = cardId,
                CardNameZh = cardData?.NameZh ?? cardId,
            };

            if (cardData == null)
            {
                // 数据库中未找到，给出中性评级
                advice.OverallRating = CardRating.Neutral;
                advice.OverallReason = "数据库中未找到此牌，无法评估。";
                results.Add(advice);
                continue;
            }

            // 为每个活跃方案生成该牌的评分
            foreach (var pathState in activePaths)
            {
                var pathData = DataLoader.GetBuildPath(pathState.PathId);
                if (pathData == null) continue;

                var score = ComputeCardScore(cardData, pathData, snapshot);
                var rating = ScoreToRating(score);
                var reason = BuildReason(cardData, pathData, snapshot, score);

                advice.PathRatings[pathState.PathId] = new CardPathRating
                {
                    PathId     = pathState.PathId,
                    PathNameZh = pathState.NameZh,
                    Score      = score,
                    Rating     = rating,
                    Reason     = reason,
                };
            }

            // 综合评级：取主推方案的评级
            var primaryPathId = activePaths.FirstOrDefault()?.PathId;
            if (primaryPathId != null && advice.PathRatings.TryGetValue(primaryPathId, out var primary))
            {
                advice.OverallRating = primary.Rating;
                advice.OverallReason = primary.Reason;
            }
            else if (advice.PathRatings.Count > 0)
            {
                // 取得分最高的方案作为综合
                var best = advice.PathRatings.Values.OrderByDescending(r => r.Score).First();
                advice.OverallRating = best.Rating;
                advice.OverallReason = best.Reason;
            }
            else
            {
                advice.OverallRating = CardRating.Neutral;
                advice.OverallReason = "无活跃构筑方案。";
            }

            results.Add(advice);
        }

        return results;
    }

    // ── 评分计算 ──────────────────────────────────────────────────────

    /// <summary>
    /// 计算某张牌在某个方案下的综合评分（0-10）。
    /// </summary>
    private static float ComputeCardScore(CardData card, BuildPathData path, RunSnapshot snapshot)
    {
        // 1. 从数据库获取该牌在此方案中的基础评分
        float baseScore = card.PathScores.TryGetValue(path.PathId, out var ps) ? ps : card.BaseScore;

        // 2. Act 适配系数
        float actScaling = card.GetActScore(snapshot.Act);

        // 3. 牌组大小系数
        float deckSizeMultiplier = ComputeDeckSizeMultiplier(card, snapshot.DeckCardIds.Count, path);

        // 4. 协同遗物加成（持有协同遗物时小幅加分）
        float relicBonus = ComputeRelicBonus(card, snapshot.RelicIds);

        // 5. 反协同惩罚（当前牌组/遗物与此牌冲突时扣分）
        float antiSynergyPenalty = ComputeAntiSynergyPenalty(card, snapshot);

        float finalScore = (baseScore * actScaling * deckSizeMultiplier + relicBonus) - antiSynergyPenalty;
        return Math.Clamp(finalScore, 0f, 10f);
    }

    private static float ComputeDeckSizeMultiplier(CardData card, int deckSize, BuildPathData path)
    {
        var ideal = path.IdealDeckSize;

        // 需要小牌组的方案（无限流）：牌组越小，评分越高；否则反之
        bool isSmallDeckPath = ideal.Max <= 14;

        if (isSmallDeckPath)
        {
            // 如果这张牌与小牌组有反协同，且牌组已经很大，降分
            if (card.AntiSynergyTags.Contains("small-deck") && deckSize > ideal.Max)
                return 0.6f;
            return 1.0f;
        }
        else
        {
            // 普通牌组大小的方案，牌组过大时标准牌略降分（稀释效应）
            if (deckSize > ideal.Max + 5)
                return 0.85f;
            return 1.0f;
        }
    }

    private static float ComputeRelicBonus(CardData card, List<string> ownedRelicIds)
    {
        // 持有与此牌有协同的遗物时，小幅加分
        float bonus = 0f;
        foreach (var relicId in ownedRelicIds)
        {
            var relicData = DataLoader.GetRelic(relicId);
            if (relicData == null) continue;

            // 遗物的 SynergyTags 与卡牌的 SynergyTags 有交集时加分
            bool hasSynergy = relicData.SynergyTags.Any(t => card.SynergyTags.Contains(t));
            if (hasSynergy) bonus += 0.3f;
        }
        return Math.Min(bonus, 1.5f); // 上限 1.5 分
    }

    private static float ComputeAntiSynergyPenalty(CardData card, RunSnapshot snapshot)
    {
        float penalty = 0f;

        // 已有大量牌时，废牌（如打击/防御）扣分
        if (card.AntiSynergyTags.Contains("small-deck") && snapshot.DeckCardIds.Count > 18)
            penalty += 2f;

        return penalty;
    }

    private static CardRating ScoreToRating(float score) => score switch
    {
        >= 8.5f => CardRating.CorePick,   // ★★★ 核心牌，强烈推荐
        >= 6.5f => CardRating.GoodPick,   // ★★  好牌，推荐
        >= 4.0f => CardRating.Situational, // ★   视情况
        >= 2.0f => CardRating.Weak,       // ○   弱
        _       => CardRating.Skip,       // ✗   跳过
    };

    private static string BuildReason(CardData card, BuildPathData path, RunSnapshot snapshot, float score)
    {
        if (score >= 8.5f)
            return $"{path.NameZh}核心牌。{card.NotesZh}";

        if (score >= 6.5f)
            return $"适合{path.NameZh}。{card.NotesZh}";

        if (score >= 4.0f)
        {
            if (path.CampfireUpgrades.Contains(card.CardId))
                return $"当前方案可带，篝火优先升级。{card.NotesZh}";
            return $"与{path.NameZh}有一定协同，但非核心。{card.NotesZh}";
        }

        return $"不适合当前{path.NameZh}构筑。{card.NotesZh}";
    }
}

// ── 建议数据类 ────────────────────────────────────────────────────────

public class CardAdvice
{
    public string CardId      { get; set; } = string.Empty;
    public string CardNameZh  { get; set; } = string.Empty;

    /// <summary>综合推荐评级（基于主推方案）</summary>
    public CardRating OverallRating { get; set; }

    /// <summary>综合推荐理由</summary>
    public string OverallReason { get; set; } = string.Empty;

    /// <summary>各方案对此牌的独立评级（key = path_id）</summary>
    public Dictionary<string, CardPathRating> PathRatings { get; set; } = new();
}

public class CardPathRating
{
    public string PathId     { get; set; } = string.Empty;
    public string PathNameZh { get; set; } = string.Empty;
    public float  Score      { get; set; }
    public CardRating Rating { get; set; }
    public string Reason     { get; set; } = string.Empty;
}

public enum CardRating
{
    CorePick,    // ★★★ 核心牌（得分 >= 8.5）
    GoodPick,    // ★★  好牌（6.5-8.5）
    Situational, // ★   视情况（4.0-6.5）
    Weak,        // ○   弱（2.0-4.0）
    Skip,        // ✗   跳过（<2.0）
    Neutral,     // —   无评级（数据库未收录）
}
