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
        var cardAdvices = CardAdvisor.Analyze(candidateCardIds, snapshot, activePaths);

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
        var primaryPath = activePaths.FirstOrDefault();
        var pathData    = primaryPath != null ? DataLoader.GetBuildPath(primaryPath.PathId) : null;

        // 决策逻辑：HP < 40% 时休息；否则查看方案的升级优先队列
        float hpRatio = snapshot.MaxHP > 0 ? (float)snapshot.HP / snapshot.MaxHP : 1f;

        if (hpRatio < 0.40f)
        {
            return new CampfireAdvice
            {
                RecommendedAction = CampfireAction.Rest,
                Reason = $"HP偏低（{snapshot.HP}/{snapshot.MaxHP}），优先恢复30% HP",
                UpgradeTargetCardId = null,
            };
        }

        // 找到最高优先级的升级目标（核心牌中还未升级的）
        string? upgradeTarget = FindBestUpgradeTarget(pathData, snapshot);
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
        var advice = new ShopAdvice
        {
            ActivePaths      = activePaths,
            GoldBudget       = snapshot.Gold,
            PurchasePriority = new List<ShopPurchaseAdvice>(),
            RemoveAdvice     = BuildRemoveAdvice(snapshot, activePaths),
        };

        // 为每张在售卡牌评分
        foreach (var card in shopItems.Cards)
        {
            var cardAdvices = CardAdvisor.Analyze(
                new[] { card.CardId },
                snapshot,
                activePaths);

            var cardAdvice = cardAdvices.FirstOrDefault();
            if (cardAdvice == null) continue;

            advice.PurchasePriority.Add(new ShopPurchaseAdvice
            {
                ItemId      = card.CardId,
                ItemType    = ShopItemType.Card,
                NameZh      = cardAdvice.CardNameZh,
                Price       = card.Price,
                OverallRating = cardAdvice.OverallRating,
                Reason      = cardAdvice.OverallReason,
            });
        }

        // 按综合评级排序
        advice.PurchasePriority.Sort((a, b) => b.OverallRating.CompareTo(a.OverallRating));

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
        // 如果所有候选牌都不适合当前方案，建议跳过
        bool allWeak = advices.All(a =>
            a.OverallRating == CardRating.Skip ||
            a.OverallRating == CardRating.Weak);

        if (allWeak)
            return "当前三张牌均不适合活跃方案，建议跳过（不选牌）";

        return null;
    }

    private static RemoveCardAdvice? BuildRemoveAdvice(RunSnapshot snapshot, List<PathState> activePaths)
    {
        if (snapshot.DeckCardIds.Count == 0) return null;

        var primaryPath = activePaths.FirstOrDefault();
        var pathData = primaryPath != null ? DataLoader.GetBuildPath(primaryPath.PathId) : null;

        // 找到基础牌（打击/防御）中得分最低的，推荐删除
        var basicCards = snapshot.DeckCardIds
            .Where(id => id is "strike" or "defend" or "strike+" or "defend+")
            .ToList();

        if (basicCards.Count > 0)
        {
            return new RemoveCardAdvice
            {
                RecommendedCardId = basicCards[0],
                Reason = "删除基础牌以精简牌组，提高关键牌抽到率",
            };
        }

        return null;
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
