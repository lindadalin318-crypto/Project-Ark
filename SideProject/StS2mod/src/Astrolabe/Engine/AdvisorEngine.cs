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

    public static AdviceEnvelope<CardRewardAdvice> AnalyzeCardReward(
        IReadOnlyList<string> candidateCardIds,
        RunSnapshot snapshot)
    {
        var activePaths = BuildPathManager.GetActivePaths();
        var context = SharedDecisionContext.Create(snapshot, activePaths);
        var cardAdvices = CardAdvisor.Analyze(candidateCardIds, context);

        var advice = new CardRewardAdvice
        {
            CardAdvices  = cardAdvices,
            ActivePaths  = activePaths,
            SkipNote     = BuildSkipNote(cardAdvices, snapshot),
        };

        string traceId = DecisionTraceId.Create(DecisionKind.CardReward);
        var record = DecisionRecordFactory.CreateCardRewardRecord(
            traceId,
            snapshot,
            activePaths,
            candidateCardIds,
            advice);

        DecisionRecorder.Record(record);
        return AdviceEnvelope<CardRewardAdvice>.FromRecord(record, advice);
    }

    // ── 地图路线建议 ──────────────────────────────────────────────────

    public static AdviceEnvelope<MapAdvice> AnalyzeMapRoutes(RunSnapshot snapshot)
    {
        var activePaths = BuildPathManager.GetActivePaths();
        var advice = MapAdvisor.Analyze(snapshot, activePaths);

        string traceId = DecisionTraceId.Create(DecisionKind.MapRoute);
        var record = DecisionRecordFactory.CreateMapRecord(
            traceId,
            snapshot,
            activePaths,
            advice);

        DecisionRecorder.Record(record);
        return AdviceEnvelope<MapAdvice>.FromRecord(record, advice);
    }

    // ── 篝火决策建议 ──────────────────────────────────────────────────

    public static AdviceEnvelope<CampfireAdvice> AnalyzeCampfire(
        RunSnapshot snapshot,
        IReadOnlyCollection<string>? availableOptionIds = null)
    {
        var activePaths = BuildPathManager.GetActivePaths();
        var context = SharedDecisionContext.Create(snapshot, activePaths);
        var availableOptions = NormalizeCampfireOptionIds(availableOptionIds);
        bool hasAvailability = availableOptions.Count > 0;
        bool canRest = !hasAvailability || availableOptions.Contains("HEAL");
        bool canUpgrade = !hasAvailability || availableOptions.Contains("SMITH");
        bool canRecall = availableOptions.Contains("RECALL");

        var bestUpgrade = FindBestUpgradeTarget(context);
        CampfireAdvice advice;

        if (context.HPRatio < 0.40f && canRest)
        {
            advice = new CampfireAdvice
            {
                RecommendedAction = CampfireAction.Rest,
                Reason = $"HP偏低（{snapshot.HP}/{snapshot.MaxHP}），当前应优先休息保命",
            };
        }
        else if (canUpgrade && bestUpgrade != null)
        {
            advice = BuildUpgradeAdvice(bestUpgrade);
        }
        else if (!canUpgrade && bestUpgrade != null && canRest)
        {
            advice = new CampfireAdvice
            {
                RecommendedAction = CampfireAction.Rest,
                Reason = $"本次篝火无法锻造，但高价值升级目标是「{bestUpgrade.CardNameZh}」，先休息并把升级留给后续窗口",
                UpgradeTargetCardId = bestUpgrade.CardId,
                UpgradeTargetCardNameZh = bestUpgrade.CardNameZh,
            };
        }
        else if (canRest && (context.HPRatio < 0.60f || context.NeedsBlock))
        {
            advice = new CampfireAdvice
            {
                RecommendedAction = CampfireAction.Rest,
                Reason = "当前血量或防御厚度还不够稳，休息的容错更高",
            };
        }
        else if (canRecall)
        {
            advice = new CampfireAdvice
            {
                RecommendedAction = CampfireAction.Recall,
                Reason = "当前没有明确的休息/升级收益，可考虑回忆保留关键资源窗口",
            };
        }
        else if (canRest)
        {
            advice = new CampfireAdvice
            {
                RecommendedAction = CampfireAction.Rest,
                Reason = context.NeedsPurge
                    ? "当前更需要商店删牌而不是硬接低价值升级，先休息保值"
                    : "无紧迫升级目标，休息保值",
            };
        }
        else
        {
            advice = new CampfireAdvice
            {
                RecommendedAction = canUpgrade ? CampfireAction.Upgrade : CampfireAction.Rest,
                Reason = bestUpgrade != null
                    ? $"当前可选动作有限，若继续推进可优先处理「{bestUpgrade.CardNameZh}」的升级"
                    : "当前可选动作有限，建议保持默认选择并观察后续路线资源",
                UpgradeTargetCardId = bestUpgrade?.CardId,
                UpgradeTargetCardNameZh = bestUpgrade?.CardNameZh,
            };
        }

        string traceId = DecisionTraceId.Create(DecisionKind.Campfire);
        var record = DecisionRecordFactory.CreateCampfireRecord(
            traceId,
            snapshot,
            activePaths,
            availableOptions,
            advice);

        DecisionRecorder.Record(record);
        return AdviceEnvelope<CampfireAdvice>.FromRecord(record, advice);
    }

    // ── 升级选牌建议 ─────────────────────────────────────────────────

    public static AdviceEnvelope<UpgradeSelectionAdvice>? AnalyzeUpgradeSelection(
        IReadOnlyList<string> candidateCardIds,
        RunSnapshot snapshot)
    {
        if (candidateCardIds.Count == 0)
            return null;

        var activePaths = BuildPathManager.GetActivePaths();
        var context = SharedDecisionContext.Create(snapshot, activePaths);
        var candidateBaseIds = candidateCardIds
            .Select(IdNormalizer.NormalizeLookupId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var bestUpgrade = FindBestUpgradeTarget(context, candidateBaseIds);
        string traceId = DecisionTraceId.Create(DecisionKind.UpgradeSelection);
        if (bestUpgrade == null)
        {
            var emptyRecord = DecisionRecordFactory.CreateUpgradeSelectionRecord(
                traceId,
                snapshot,
                activePaths,
                candidateCardIds,
                null);

            DecisionRecorder.Record(emptyRecord);
            return null;
        }

        string deltaText = string.IsNullOrWhiteSpace(bestUpgrade.UpgradeDeltaZh)
            ? "升级后能稳定提高这张牌的战斗转化"
            : bestUpgrade.UpgradeDeltaZh;

        var advice = new UpgradeSelectionAdvice
        {
            TargetCardId = bestUpgrade.CardId,
            TargetCardNameZh = bestUpgrade.CardNameZh,
            SummaryText = $"星象仪推荐：优先升级「{bestUpgrade.CardNameZh}」",
            Reason = $"{bestUpgrade.Reason}；{deltaText}",
        };

        var record = DecisionRecordFactory.CreateUpgradeSelectionRecord(
            traceId,
            snapshot,
            activePaths,
            candidateCardIds,
            advice);

        DecisionRecorder.Record(record);
        return AdviceEnvelope<UpgradeSelectionAdvice>.FromRecord(record, advice);
    }

    // ── 商店建议 ─────────────────────────────────────────────────────

    public static AdviceEnvelope<ShopAdvice> AnalyzeShop(ShopItems shopItems, RunSnapshot snapshot)
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

        string traceId = DecisionTraceId.Create(DecisionKind.Shop);
        var record = DecisionRecordFactory.CreateShopRecord(
            traceId,
            snapshot,
            activePaths,
            shopItems,
            advice);

        DecisionRecorder.Record(record);
        return AdviceEnvelope<ShopAdvice>.FromRecord(record, advice);
    }

    // ── 内部辅助 ─────────────────────────────────────────────────────

    private static HashSet<string> NormalizeCampfireOptionIds(IReadOnlyCollection<string>? optionIds)
    {
        var normalized = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (optionIds == null)
            return normalized;

        foreach (var optionId in optionIds)
        {
            if (string.IsNullOrWhiteSpace(optionId))
                continue;

            normalized.Add(optionId.Trim().ToUpperInvariant());
        }

        return normalized;
    }

    private static CampfireAdvice BuildUpgradeAdvice(CampfireUpgradeCandidate candidate)
    {
        string deltaText = string.IsNullOrWhiteSpace(candidate.UpgradeDeltaZh)
            ? "升级后能稳定提高这张牌的战斗转化"
            : candidate.UpgradeDeltaZh;

        return new CampfireAdvice
        {
            RecommendedAction = CampfireAction.Upgrade,
            Reason = $"推荐升级「{candidate.CardNameZh}」：{candidate.Reason}；{deltaText}",
            UpgradeTargetCardId = candidate.CardId,
            UpgradeTargetCardNameZh = candidate.CardNameZh,
        };
    }

    private static CampfireUpgradeCandidate? FindBestUpgradeTarget(
        SharedDecisionContext context,
        HashSet<string>? allowedBaseIds = null)
    {
        return context.DeckEntries
            .Where(entry => !entry.IsUpgraded && entry.Card != null)
            .Where(entry => allowedBaseIds == null || allowedBaseIds.Contains(entry.BaseCardId))
            .Select(entry => EvaluateUpgradeCandidate(entry, context))
            .OrderByDescending(candidate => candidate.Score)
            .FirstOrDefault();
    }

    private static CampfireUpgradeCandidate EvaluateUpgradeCandidate(DeckCardEntry entry, SharedDecisionContext context)
    {
        var card = entry.Card!;
        float score = card.UpgradePriorityScore;
        var reasons = new List<string>();

        if (context.PrimaryPathData != null)
        {
            if (context.PrimaryPathData.CampfireUpgrades.Contains(entry.BaseCardId))
            {
                score += 2.4f;
                reasons.Add($"是{context.PrimaryPathData.NameZh}的优先升级位");
            }
            else if (context.PrimaryPathData.CoreCards.Contains(entry.BaseCardId))
            {
                score += 1.2f;
                reasons.Add("属于当前主路线核心组件");
            }
        }

        if (context.NeedsBlock && CardAdvisor.IsBlockCard(card))
        {
            score += 0.9f;
            reasons.Add("能直接补当前防御缺口");
        }

        if (context.NeedsDraw && CardAdvisor.IsDrawCard(card))
        {
            score += 0.75f;
            reasons.Add("升级后能改善过牌节奏");
        }

        if (context.NeedsScaling && CardAdvisor.IsScalingCard(card))
        {
            score += 0.75f;
            reasons.Add("补足中后期成长能力");
        }

        if (context.NeedsFrontloadDamage && CardAdvisor.IsAttackCard(card))
        {
            score += 0.6f;
            reasons.Add("能增强当前前场伤害");
        }

        if (context.NeedsAoe && CardAdvisor.IsAoeCard(card))
        {
            score += 0.55f;
            reasons.Add("能补群体处理手段");
        }

        if (IdNormalizer.IsStarterStrikeOrDefend(entry.BaseCardId))
        {
            score -= 2.6f;
        }
        else if (card.Rarity.Equals("Basic", StringComparison.OrdinalIgnoreCase))
        {
            score -= 1.1f;
        }

        if (context.Snapshot.Act == 1 && context.HPRatio < 0.45f && card.Cost >= 2 && !CardAdvisor.IsBlockCard(card))
            score -= 0.45f;

        string reason = reasons.Count > 0
            ? string.Join("；", reasons.Take(2))
            : "是当前牌组里升级收益最稳的一张牌";

        return new CampfireUpgradeCandidate
        {
            CardId = entry.BaseCardId,
            CardNameZh = string.IsNullOrWhiteSpace(card.NameZh) ? entry.BaseCardId : card.NameZh,
            Score = score,
            Reason = reason,
            UpgradeDeltaZh = card.UpgradeDeltaZh,
        };
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
    public CampfireAction RecommendedAction       { get; set; }
    public string         Reason                  { get; set; } = string.Empty;
    public string?        UpgradeTargetCardId     { get; set; }
    public string?        UpgradeTargetCardNameZh { get; set; }
}

public class UpgradeSelectionAdvice
{
    public string TargetCardId { get; set; } = string.Empty;
    public string TargetCardNameZh { get; set; } = string.Empty;
    public string SummaryText { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

internal sealed class CampfireUpgradeCandidate
{
    public string CardId { get; init; } = string.Empty;
    public string CardNameZh { get; init; } = string.Empty;
    public float Score { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string UpgradeDeltaZh { get; init; } = string.Empty;
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
