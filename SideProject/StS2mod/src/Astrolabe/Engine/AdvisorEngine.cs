using Astrolabe.Core;
using Astrolabe.Data;

namespace Astrolabe.Engine;

/// <summary>
/// 顾问引擎统一入口。
/// 所有场景的建议生成均通过此类调用，内部委托给对应的专用 Advisor。
/// </summary>
public static class AdvisorEngine
{
    // ── 选牌建议 ─────────────────────────────────────────────────────

    public static CardRewardAdvice AnalyzeCardReward(
        IReadOnlyList<string> candidateCardIds,
        RunSnapshot snapshot)
    {
        var activePaths = BuildPathManager.GetActivePaths();
        var context = SharedDecisionContext.Create(snapshot, activePaths);
        var cardAdvices = CardAdvisor.Analyze(candidateCardIds, context);

        return new CardRewardAdvice
        {
            CardAdvices  = cardAdvices,
            ActivePaths  = activePaths,
            SkipNote     = BuildSkipNote(cardAdvices, snapshot),
        };
    }

    // ── 地图路线建议 ──────────────────────────────────────────────────

    public static MapAdvice AnalyzeMapRoutes(RunSnapshot snapshot)
    {
        var activePaths = BuildPathManager.GetActivePaths();
        return MapAdvisor.Analyze(snapshot, activePaths);
    }

    // ── 篝火决策建议 ──────────────────────────────────────────────────

    public static CampfireAdvice AnalyzeCampfire(RunSnapshot snapshot)
    {
        var activePaths = BuildPathManager.GetActivePaths();
        var context = SharedDecisionContext.Create(snapshot, activePaths);

        if (context.HPRatio < 0.40f)
        {
            return new CampfireAdvice
            {
                RecommendedAction = CampfireAction.Rest,
                Reason = $"HP偏低（{snapshot.HP}/{snapshot.MaxHP}），优先恢复30% HP",
                UpgradeTargetCardId = null,
            };
        }

        string? upgradeTarget = FindBestUpgradeTarget(context.PrimaryPathData, snapshot);
        if (upgradeTarget != null)
        {
            var cardData = DataLoader.GetCard(upgradeTarget);
            return new CampfireAdvice
            {
                RecommendedAction   = CampfireAction.Upgrade,
                Reason              = $"推荐升级「{cardData?.NameZh ?? upgradeTarget}」：{cardData?.UpgradeDeltaZh ?? "核心牌"}",
                UpgradeTargetCardId = upgradeTarget,
            };
        }

        if (context.NeedsPurge && context.HPRatio >= 0.70f)
        {
            return new CampfireAdvice
            {
                RecommendedAction = CampfireAction.Upgrade,
                Reason = "当前血量安全，但牌组仍偏厚；若篝火没有关键升级，可优先把后续路线资源留给商店删牌",
            };
        }

        return new CampfireAdvice
        {
            RecommendedAction = CampfireAction.Rest,
            Reason = "无紧迫升级目标，休息保值",
        };
    }

    // ── 商店建议 ─────────────────────────────────────────────────────

    public static ShopAdvice AnalyzeShop(ShopItems shopItems, RunSnapshot snapshot)
    {
        var activePaths = BuildPathManager.GetActivePaths();
        var context = SharedDecisionContext.Create(snapshot, activePaths);
        var advice = new ShopAdvice
        {
            ActivePaths      = activePaths,
            GoldBudget       = snapshot.Gold,
            PurchasePriority = new List<ShopPurchaseAdvice>(),
            RemoveAdvice     = BuildRemoveAdvice(context),
        };

        foreach (var card in shopItems.Cards)
        {
            var cardAdvice = CardAdvisor.Analyze(new[] { card.CardId }, context).FirstOrDefault();
            if (cardAdvice == null)
                continue;

            advice.PurchasePriority.Add(new ShopPurchaseAdvice
            {
                ItemId = card.CardId,
                ItemType = ShopItemType.Card,
                NameZh = cardAdvice.CardNameZh,
                Price = card.Price,
                OverallRating = cardAdvice.OverallRating,
                Score = cardAdvice.PathRatings.Values.Count > 0
                    ? cardAdvice.PathRatings.Values.Max(r => r.Score)
                    : 0f,
                Reason = cardAdvice.OverallReason,
            });
        }

        foreach (var relic in shopItems.Relics)
        {
            var relicAdvice = EvaluateRelicPurchase(relic, context);
            if (relicAdvice != null)
                advice.PurchasePriority.Add(relicAdvice);
        }

        advice.PurchasePriority.Sort((a, b) =>
        {
            int ratingCompare = a.OverallRating.CompareTo(b.OverallRating);
            if (ratingCompare != 0)
                return ratingCompare;

            int scoreCompare = b.Score.CompareTo(a.Score);
            if (scoreCompare != 0)
                return scoreCompare;

            return a.Price.CompareTo(b.Price);
        });

        return advice;
    }

    // ── 内部辅助 ─────────────────────────────────────────────────────

    private static string? FindBestUpgradeTarget(BuildPathData? pathData, RunSnapshot snapshot)
    {
        if (pathData == null || snapshot.DeckCardIds.Count == 0)
            return null;

        // 按优先级顺序检查，找到第一个还未升级的核心牌
        foreach (var cardId in pathData.CampfireUpgrades)
        {
            bool hasUpgraded   = snapshot.DeckCardIds.Any(id =>
                id.Equals(cardId + "+", StringComparison.OrdinalIgnoreCase));
            bool hasUnupgraded = snapshot.DeckCardIds.Any(id =>
                id.Equals(cardId, StringComparison.OrdinalIgnoreCase));

            if (hasUnupgraded && !hasUpgraded)
                return cardId;
        }

        return null;
    }

    private static string? BuildSkipNote(List<CardAdvice> advices, RunSnapshot snapshot)
    {
        if (advices.Count == 0)
            return null;

        bool allWeak = advices.All(a =>
            a.OverallRating == CardRating.Skip ||
            a.OverallRating == CardRating.Weak);

        if (!allWeak)
            return null;

        return snapshot.DeckCardIds.Count >= 18
            ? "当前牌组已偏厚，且三张牌都不能明显补强主路线，建议跳过保持密度"
            : "当前三张牌都不构成有效补强，建议跳过等更关键组件";
    }

    private static RemoveCardAdvice? BuildRemoveAdvice(SharedDecisionContext context)
    {
        if (context.DeckEntries.Count == 0)
            return null;

        return CardAdvisor.AnalyzeRemove(context);
    }

    private static ShopPurchaseAdvice? EvaluateRelicPurchase(ShopRelicItem item, SharedDecisionContext context)
    {
        var relicData = DataLoader.GetRelic(item.RelicId);
        if (relicData == null)
            return null;

        float score = relicData.BaseScore;

        if (context.PrimaryPathData != null && relicData.PathScores.TryGetValue(context.PrimaryPathData.PathId, out var pathScore))
            score = Math.Max(score, pathScore);

        if (context.PrimaryPathData != null && context.PrimaryPathData.KeyRelics.Contains(relicData.RelicId))
            score += 1.2f;

        if (relicData.SynergyTags.Any(tag => MatchesContextNeed(tag, context)))
            score += 0.8f;

        if (context.Relics.Any(existing => string.Equals(existing.RelicId, relicData.RelicId, StringComparison.OrdinalIgnoreCase)))
            score -= 1.2f;

        score = Math.Clamp(score, 0f, 10f);

        return new ShopPurchaseAdvice
        {
            ItemId = item.RelicId,
            ItemType = ShopItemType.Relic,
            NameZh = relicData.NameZh,
            Price = item.Price,
            Score = score,
            OverallRating = CardAdvisor.ScoreToRating(score),
            Reason = BuildRelicPurchaseReason(relicData, context),
        };
    }

    private static bool MatchesContextNeed(string tag, SharedDecisionContext context)
    {
        return tag switch
        {
            "draw" => context.NeedsDraw,
            "block" or "block_gain" or "block_scaling" => context.NeedsBlock,
            "strength" or "strength_scaling" or "scaling" => context.NeedsScaling,
            "aoe" or "aoe_damage" => context.NeedsAoe,
            _ => false,
        };
    }

    private static string BuildRelicPurchaseReason(RelicData relicData, SharedDecisionContext context)
    {
        var reasons = new List<string>();

        if (context.PrimaryPathData != null && context.PrimaryPathData.KeyRelics.Contains(relicData.RelicId))
            reasons.Add($"是{context.PrimaryPathData.NameZh}关键遗物");

        if (relicData.SynergyTags.Any(tag => MatchesContextNeed(tag, context)))
            reasons.Add("能直接补当前牌组缺口");

        if (reasons.Count == 0 && !string.IsNullOrWhiteSpace(relicData.NotesZh))
            reasons.Add(relicData.NotesZh.Trim().TrimEnd('。', '！', '？'));

        if (reasons.Count == 0)
            reasons.Add("对当前路线是稳定提升");

        return string.Join("；", reasons);
    }

}

// ── 建议数据类 ────────────────────────────────────────────────────────

public class CardRewardAdvice
{
    public List<CardAdvice> CardAdvices { get; set; } = new();
    public List<PathState>  ActivePaths { get; set; } = new();
    /// <summary>如果建议跳过（不选牌），此字段包含原因</summary>
    public string? SkipNote { get; set; }
    public bool ShouldSkip  => SkipNote != null;
}

public class CampfireAdvice
{
    public CampfireAction RecommendedAction   { get; set; }
    public string         Reason              { get; set; } = string.Empty;
    public string?        UpgradeTargetCardId { get; set; }
}

public enum CampfireAction { Rest, Upgrade, Smith, Recall }

public class ShopAdvice
{
    public List<PathState>          ActivePaths      { get; set; } = new();
    public int                      GoldBudget       { get; set; }
    public List<ShopPurchaseAdvice> PurchasePriority { get; set; } = new();
    public RemoveCardAdvice?        RemoveAdvice     { get; set; }
    /// <summary>用于日志的一行总结</summary>
    public string Summary => PurchasePriority.Count > 0
        ? $"Top pick: {PurchasePriority[0].ItemId} ({PurchasePriority[0].OverallRating})"
        : "No purchase recommended";
}

public class ShopPurchaseAdvice
{
    public string       ItemId        { get; set; } = string.Empty;
    public ShopItemType ItemType      { get; set; }
    public string       NameZh        { get; set; } = string.Empty;
    public int          Price         { get; set; }
    public float        Score         { get; set; }
    public CardRating   OverallRating { get; set; }
    public string       Reason        { get; set; } = string.Empty;
}

public enum ShopItemType { Card, Relic, Potion }

public class RemoveCardAdvice
{
    public string RecommendedCardId { get; set; } = string.Empty;
    public string Reason            { get; set; } = string.Empty;
}

/// <summary>商店商品数据（由 ShopHook.BuildShopItems 构建）</summary>
public class ShopItems
{
    public List<ShopCardItem>   Cards   { get; set; } = new();
    public List<ShopRelicItem>  Relics  { get; set; } = new();
    public List<ShopPotionItem> Potions { get; set; } = new();
}

public class ShopCardItem
{
    public string CardId  { get; set; } = "";
    public int    Price   { get; set; }
    public bool   IsSale  { get; set; }
    public bool   IsSold  { get; set; }
}

public class ShopRelicItem
{
    public string RelicId { get; set; } = "";
    public int    Price   { get; set; }
    public bool   IsSold  { get; set; }
}

public class ShopPotionItem
{
    public string PotionId { get; set; } = "";
    public int    Price    { get; set; }
    public bool   IsSold   { get; set; }
}
