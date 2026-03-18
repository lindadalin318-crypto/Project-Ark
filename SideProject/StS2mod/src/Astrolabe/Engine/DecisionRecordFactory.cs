using Astrolabe.Core;
using Astrolabe.Data;

namespace Astrolabe.Engine;

/// <summary>
/// 将现有 Advice DTO 转成统一的 <see cref="DecisionRecord"/>。
/// Phase 1 先覆盖非战斗主链，先把决策样本稳定落盘。
/// </summary>
public static class DecisionRecordFactory
{
    public static DecisionRecord CreateCardRewardRecord(
        string traceId,
        RunSnapshot snapshot,
        IReadOnlyList<PathState> activePaths,
        IReadOnlyList<string> candidateCardIds,
        CardRewardAdvice advice)
    {
        var rankedCards = advice.CardAdvices
            .Select(card =>
            {
                float score = ComputeCardCompositeScore(card);
                return (
                    card.CardId,
                    Label: string.IsNullOrWhiteSpace(card.CardNameZh) ? card.CardId : card.CardNameZh,
                    Score: score,
                    Rating: card.OverallRating.ToString(),
                    card.OverallReason);
            })
            .OrderByDescending(entry => entry.Score)
            .ToList();

        if (rankedCards.Count == 0)
        {
            rankedCards = candidateCardIds
                .Select(cardId =>
                {
                    var card = DataLoader.GetCard(cardId);
                    return (
                        CardId: cardId,
                        Label: card?.NameZh ?? cardId,
                        Score: 0f,
                        Rating: "Unknown",
                        OverallReason: "当前没有生成可用评分。"
                    );
                })
                .ToList();
        }

        var recommendedIds = advice.ShouldSkip
            ? new List<string> { "skip" }
            : rankedCards.Take(1).Select(entry => entry.CardId).ToList();

        var alternativeIds = advice.ShouldSkip
            ? rankedCards.Take(2).Select(entry => entry.CardId).ToList()
            : rankedCards.Skip(1).Take(2).Select(entry => entry.CardId).ToList();

        float confidence = advice.ShouldSkip
            ? 0.62f
            : ComputeConfidence(rankedCards.Select(entry => entry.Score).ToList(), 0.58f);

        string summary = advice.ShouldSkip
            ? "建议跳过本次卡牌奖励"
            : rankedCards.Count > 0
                ? $"建议优先拿「{rankedCards[0].Label}」"
                : "当前没有明确的选牌推荐";

        string why = advice.ShouldSkip
            ? advice.SkipNote ?? "当前候选都不足以改善整局质量。"
            : rankedCards.FirstOrDefault().OverallReason ?? "当前没有明确的推荐理由。";

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["candidateCount"] = candidateCardIds.Count.ToString(),
        };

        if (!string.IsNullOrWhiteSpace(advice.SkipNote))
            metadata["skipNote"] = advice.SkipNote!;

        return new DecisionRecord
        {
            TraceId = traceId,
            Kind = DecisionKind.CardReward,
            Source = "AdvisorEngine.AnalyzeCardReward",
            Snapshot = snapshot,
            ActivePaths = ClonePaths(activePaths),
            Candidates = rankedCards
                .Select(entry => new DecisionCandidate
                {
                    OptionId = entry.CardId,
                    Label = entry.Label,
                    Category = "card",
                    Score = entry.Score,
                    Rating = entry.Rating,
                    Reason = entry.OverallReason,
                })
                .ToList(),
            RecommendedOptionIds = recommendedIds,
            Summary = summary,
            Why = why,
            Confidence = confidence,
            RiskLevel = BuildRiskLevel(confidence),
            AlternativeOptionIds = alternativeIds,
            Metadata = metadata,
        };
    }

    public static DecisionRecord CreateMapRecord(
        string traceId,
        RunSnapshot snapshot,
        IReadOnlyList<PathState> activePaths,
        MapAdvice advice)
    {
        var candidates = advice.PathRoutes
            .Select(route => new DecisionCandidate
            {
                OptionId = route.PathId,
                Label = string.IsNullOrWhiteSpace(route.PathName) ? route.PathId : route.PathName,
                Category = "map-route",
                Reason = route.Recommendation,
            })
            .ToList();

        float confidence = ComputeConfidence(
            activePaths.Select(path => path.Viability * 10f).ToList(),
            candidates.Count <= 1 ? 0.72f : 0.56f);

        return new DecisionRecord
        {
            TraceId = traceId,
            Kind = DecisionKind.MapRoute,
            Source = "AdvisorEngine.AnalyzeMapRoutes",
            Snapshot = snapshot,
            ActivePaths = ClonePaths(activePaths),
            Candidates = candidates,
            RecommendedOptionIds = candidates.Take(1).Select(candidate => candidate.OptionId).ToList(),
            Summary = string.IsNullOrWhiteSpace(advice.GlobalNote)
                ? "已生成地图路线建议"
                : advice.GlobalNote,
            Why = candidates.FirstOrDefault()?.Reason
                ?? advice.GlobalNote
                ?? "当前没有更细的路线说明。",
            Confidence = confidence,
            RiskLevel = BuildRiskLevel(confidence),
            AlternativeOptionIds = candidates.Skip(1).Take(2).Select(candidate => candidate.OptionId).ToList(),
            Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["routeCount"] = candidates.Count.ToString(),
                ["globalNote"] = advice.GlobalNote,
            },
        };
    }

    public static DecisionRecord CreateCampfireRecord(
        string traceId,
        RunSnapshot snapshot,
        IReadOnlyList<PathState> activePaths,
        IReadOnlyCollection<string> availableOptionIds,
        CampfireAdvice advice)
    {
        string recommendedOptionId = ToCampfireOptionId(advice.RecommendedAction);
        float confidence = ComputeCampfireConfidence(snapshot, advice);

        var candidates = availableOptionIds
            .Select(optionId => new DecisionCandidate
            {
                OptionId = optionId,
                Label = ToCampfireOptionLabel(optionId),
                Category = "campfire-option",
                Rating = string.Equals(optionId, recommendedOptionId, StringComparison.OrdinalIgnoreCase)
                    ? "Recommended"
                    : "Available",
                Reason = string.Equals(optionId, recommendedOptionId, StringComparison.OrdinalIgnoreCase)
                    ? advice.Reason
                    : "当前可执行的篝火动作。",
            })
            .ToList();

        return new DecisionRecord
        {
            TraceId = traceId,
            Kind = DecisionKind.Campfire,
            Source = "AdvisorEngine.AnalyzeCampfire",
            Snapshot = snapshot,
            ActivePaths = ClonePaths(activePaths),
            Candidates = candidates,
            RecommendedOptionIds = string.IsNullOrWhiteSpace(recommendedOptionId)
                ? new List<string>()
                : new List<string> { recommendedOptionId },
            Summary = $"建议篝火执行「{ToCampfireOptionLabel(recommendedOptionId)}」",
            Why = advice.Reason,
            Confidence = confidence,
            RiskLevel = BuildRiskLevel(confidence),
            AlternativeOptionIds = candidates
                .Where(candidate => !string.Equals(candidate.OptionId, recommendedOptionId, StringComparison.OrdinalIgnoreCase))
                .Select(candidate => candidate.OptionId)
                .Take(2)
                .ToList(),
            Metadata = BuildCampfireMetadata(availableOptionIds, advice),
        };
    }

    public static DecisionRecord CreateUpgradeSelectionRecord(
        string traceId,
        RunSnapshot snapshot,
        IReadOnlyList<PathState> activePaths,
        IReadOnlyList<string> candidateCardIds,
        UpgradeSelectionAdvice? advice,
        string source = "AdvisorEngine.AnalyzeUpgradeSelection",
        IReadOnlyDictionary<string, string>? extraMetadata = null)
    {
        float confidence = advice == null ? 0.48f : 0.76f;

        var candidates = candidateCardIds
            .Select(cardId =>
            {
                var card = DataLoader.GetCard(cardId);
                bool isRecommended = advice != null
                    && string.Equals(cardId, advice.TargetCardId, StringComparison.OrdinalIgnoreCase);

                return new DecisionCandidate
                {
                    OptionId = cardId,
                    Label = card?.NameZh ?? cardId,
                    Category = "upgrade-card",
                    Rating = isRecommended ? "Recommended" : "Available",
                    Reason = isRecommended
                        ? advice!.Reason
                        : "当前可升级候选。",
                };
            })
            .ToList();

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["candidateCount"] = candidateCardIds.Count.ToString(),
        };

        MergeMetadata(metadata, extraMetadata);

        return new DecisionRecord
        {
            TraceId = traceId,
            Kind = DecisionKind.UpgradeSelection,
            Source = source,
            Snapshot = snapshot,
            ActivePaths = ClonePaths(activePaths),
            Candidates = candidates,
            RecommendedOptionIds = advice == null || string.IsNullOrWhiteSpace(advice.TargetCardId)
                ? new List<string>()
                : new List<string> { advice.TargetCardId },
            Summary = advice?.SummaryText ?? "当前没有明确的升级目标",
            Why = advice?.Reason ?? "现有上下文还不足以稳定锁定单一升级对象。",
            Confidence = confidence,
            RiskLevel = BuildRiskLevel(confidence),
            AlternativeOptionIds = candidates
                .Where(candidate => advice == null || !string.Equals(candidate.OptionId, advice.TargetCardId, StringComparison.OrdinalIgnoreCase))
                .Select(candidate => candidate.OptionId)
                .Take(2)
                .ToList(),
            Metadata = metadata,
        };
    }

    public static DecisionRecord CreateShopRecord(
        string traceId,
        RunSnapshot snapshot,
        IReadOnlyList<PathState> activePaths,
        ShopItems shopItems,
        ShopAdvice advice)
    {
        var candidates = advice.PurchasePriority
            .Select(item => new DecisionCandidate
            {
                OptionId = BuildShopOptionId(item.ItemType, item.ItemId),
                Label = string.IsNullOrWhiteSpace(item.NameZh) ? item.ItemId : item.NameZh,
                Category = item.ItemType.ToString().ToLowerInvariant(),
                Score = item.Score,
                Rating = item.OverallRating.ToString(),
                Reason = item.Reason,
                Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["price"] = item.Price.ToString(),
                },
            })
            .ToList();

        float confidence = ComputeConfidence(
            advice.PurchasePriority.Select(item => item.Score).ToList(),
            candidates.Count == 0 ? 0.42f : 0.60f);

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["goldBudget"] = advice.GoldBudget.ToString(),
            ["cardCount"] = shopItems.Cards.Count(card => !card.IsSold).ToString(),
            ["relicCount"] = shopItems.Relics.Count(relic => !relic.IsSold).ToString(),
            ["potionCount"] = shopItems.Potions.Count(potion => !potion.IsSold).ToString(),
        };

        if (advice.RemoveAdvice != null)
        {
            metadata["removeCardId"] = advice.RemoveAdvice.RecommendedCardId;
            metadata["removeReason"] = advice.RemoveAdvice.Reason;
        }

        return new DecisionRecord
        {
            TraceId = traceId,
            Kind = DecisionKind.Shop,
            Source = "AdvisorEngine.AnalyzeShop",
            Snapshot = snapshot,
            ActivePaths = ClonePaths(activePaths),
            Candidates = candidates,
            RecommendedOptionIds = candidates.Take(1).Select(candidate => candidate.OptionId).ToList(),
            Summary = advice.Summary,
            Why = candidates.FirstOrDefault()?.Reason
                ?? advice.RemoveAdvice?.Reason
                ?? "当前没有明确的购买建议。",
            Confidence = confidence,
            RiskLevel = BuildRiskLevel(confidence),
            AlternativeOptionIds = candidates.Skip(1).Take(2).Select(candidate => candidate.OptionId).ToList(),
            Metadata = metadata,
        };
    }

    public static DecisionRecord CreatePlayerChoiceRecord<T>(
        AdviceEnvelope<T> envelope,
        RunSnapshot snapshot,
        string? playerChoiceId,
        string source,
        IReadOnlyDictionary<string, string>? extraMetadata = null)
    {
        var metadata = new Dictionary<string, string>(envelope.Metadata, StringComparer.OrdinalIgnoreCase)
        {
            ["recordStage"] = "player-choice",
        };

        if (!string.IsNullOrWhiteSpace(envelope.ParentTraceId))
            metadata["parentTraceId"] = envelope.ParentTraceId!;

        if (!string.IsNullOrWhiteSpace(playerChoiceId))
            metadata["playerChoiceId"] = playerChoiceId!;

        MergeMetadata(metadata, extraMetadata);

        var effectiveRecommendedOptionIds = (recommendedChoiceIds ?? envelope.RecommendedOptionIds)
            .Where(optionId => !string.IsNullOrWhiteSpace(optionId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        bool? followedAdvice = null;
        if (!string.IsNullOrWhiteSpace(playerChoiceId))
        {
            var choiceIds = SplitChoiceIds(playerChoiceId!);
            followedAdvice = choiceIds.Any(choiceId =>
                effectiveRecommendedOptionIds.Any(recommendedId =>
                    string.Equals(recommendedId, choiceId, StringComparison.OrdinalIgnoreCase)));
        }

        string summary = string.IsNullOrWhiteSpace(playerChoiceId)
            ? envelope.Summary
            : $"{envelope.Summary} / 玩家选择：{playerChoiceId}";

        return new DecisionRecord
        {
            TraceId = envelope.TraceId,
            Kind = envelope.Kind,
            Source = source,
            Snapshot = snapshot,
            RecommendedOptionIds = effectiveRecommendedOptionIds,
            Summary = summary,
            Why = envelope.Why,
            Confidence = envelope.Confidence,
            RiskLevel = envelope.RiskLevel,
            AlternativeOptionIds = new List<string>(envelope.AlternativeOptionIds),
            PlayerChoiceId = playerChoiceId,
            PlayerFollowedAdvice = followedAdvice,
            Metadata = metadata,
        };
    }

    private static List<DecisionPathStateSnapshot> ClonePaths(IReadOnlyList<PathState> activePaths)
    {
        return activePaths
            .Select(path => new DecisionPathStateSnapshot
            {
                PathId = path.PathId,
                NameZh = path.NameZh,
                Viability = path.Viability,
                Trend = path.Trend.ToString(),
            })
            .ToList();
    }

    private static List<string> SplitChoiceIds(string rawChoiceId)
    {
        return rawChoiceId
            .Split(new[] { '|', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(choiceId => choiceId.Trim())
            .Where(choiceId => !string.IsNullOrWhiteSpace(choiceId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static float ComputeCardCompositeScore(CardAdvice advice)
    {
        float pathScore = advice.PathRatings.Count == 0
            ? 0f
            : advice.PathRatings.Values.Max(rating => rating.Score);

        return pathScore + GetCardRatingBonus(advice.OverallRating);
    }

    private static float GetCardRatingBonus(CardRating rating)
    {
        return rating switch
        {
            CardRating.CorePick => 4f,
            CardRating.GoodPick => 3f,
            CardRating.Situational => 2f,
            CardRating.Neutral => 1.5f,
            CardRating.Weak => 1f,
            CardRating.Skip => 0f,
            _ => 0f,
        };
    }

    private static float ComputeConfidence(IReadOnlyList<float> orderedScores, float baseline)
    {
        if (orderedScores.Count == 0)
            return Math.Clamp(baseline, 0.35f, 0.95f);

        if (orderedScores.Count == 1)
            return Math.Clamp(Math.Max(baseline, 0.72f), 0.35f, 0.95f);

        float maxScore = orderedScores.Max();
        float secondScore = orderedScores
            .OrderByDescending(score => score)
            .Skip(1)
            .FirstOrDefault();
        float gap = Math.Max(0f, maxScore - secondScore);

        return Math.Clamp(baseline + gap / 10f, 0.35f, 0.95f);
    }

    private static float ComputeCampfireConfidence(RunSnapshot snapshot, CampfireAdvice advice)
    {
        if (advice.RecommendedAction is CampfireAction.Rest && snapshot.MaxHP > 0)
        {
            float hpRatio = snapshot.HP / (float)snapshot.MaxHP;
            if (hpRatio < 0.40f)
                return 0.88f;
        }

        if (!string.IsNullOrWhiteSpace(advice.UpgradeTargetCardId))
            return 0.80f;

        return 0.66f;
    }

    private static string BuildRiskLevel(float confidence)
    {
        if (confidence >= 0.80f)
            return "low";

        if (confidence >= 0.60f)
            return "medium";

        return "high";
    }

    private static Dictionary<string, string> BuildCampfireMetadata(
        IReadOnlyCollection<string> availableOptionIds,
        CampfireAdvice advice)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["availableOptions"] = string.Join(",", availableOptionIds),
        };

        if (!string.IsNullOrWhiteSpace(advice.UpgradeTargetCardId))
            metadata["upgradeTargetCardId"] = advice.UpgradeTargetCardId!;

        if (!string.IsNullOrWhiteSpace(advice.UpgradeTargetCardNameZh))
            metadata["upgradeTargetCardNameZh"] = advice.UpgradeTargetCardNameZh!;

        return metadata;
    }

    private static void MergeMetadata(
        Dictionary<string, string> metadata,
        IReadOnlyDictionary<string, string>? extraMetadata)
    {
        if (extraMetadata == null)
            return;

        foreach (var pair in extraMetadata)
        {
            metadata[pair.Key] = pair.Value;
        }
    }

    private static string ToCampfireOptionId(CampfireAction action)
    {
        return action switch
        {
            CampfireAction.Upgrade => "SMITH",
            CampfireAction.Smith => "SMITH",
            CampfireAction.Rest => "HEAL",
            CampfireAction.Recall => "RECALL",
            _ => string.Empty,
        };
    }

    private static string ToCampfireOptionLabel(string optionId)
    {
        return optionId.ToUpperInvariant() switch
        {
            "SMITH" => "锻造",
            "HEAL" => "休息",
            "RECALL" => "回忆",
            _ => optionId,
        };
    }

    private static string BuildShopOptionId(ShopItemType itemType, string itemId)
    {
        return $"{itemType.ToString().ToLowerInvariant()}:{itemId}";
    }
}
