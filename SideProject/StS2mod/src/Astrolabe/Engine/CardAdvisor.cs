using System;
using System.Collections.Generic;
using System.Linq;
using Astrolabe.Core;
using Astrolabe.Data;

namespace Astrolabe.Engine;

/// <summary>
/// 为选牌界面中的候选卡牌生成推荐评级。
/// 架构分为三层：
/// 1. 特征提取：从当前 Run 快照提炼牌组缺口、重复度、路线压力等上下文。
/// 2. 评分计算：按路线对候选牌进行分项加减分，保留 factor 级明细。
/// 3. 理由生成：把最关键的正负因子压缩成 UI 可显示的一段中文说明。
/// </summary>
public static class CardAdvisor
{
    private const float SCORE_MIN = 0f;
    private const float SCORE_MAX = 10f;

    private static readonly HashSet<string> DrawTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "draw",
    };

    private static readonly HashSet<string> ScalingTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "strength",
        "strength_scaling",
        "block_scaling",
        "burn_apply",
        "poison",
        "poison_apply",
        "frost",
        "lightning",
        "orb",
        "divinity",
        "retain",
        "stance",
    };

    private static readonly HashSet<string> AoeTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "aoe",
        "aoe_damage",
    };

    private static readonly HashSet<string> ScalingApplyKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "strength",
        "dexterity",
        "metallicize",
        "plated_armor",
        "ritual",
        "barricade",
        "dark_embrace",
        "feel_no_pain",
        "corruption",
        "demon_form",
        "juggernaut",
        "inferno",
        "rupture",
    };

    private static readonly HashSet<string> SmallDeckPressureTags = new(StringComparer.OrdinalIgnoreCase)

    {
        "deck_thinner_anti",
    };

    private static readonly HashSet<string> SmallDeckSupportTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "draw",
        "exhaust",
        "retain",
    };

    /// <summary>
    /// 为候选卡牌列表生成建议。
    /// </summary>
    public static List<CardAdvice> Analyze(
        IReadOnlyList<string> candidateCardIds,
        RunSnapshot snapshot,
        IReadOnlyList<PathState> activePaths)
    {
        var context = SharedDecisionContext.Create(snapshot, activePaths);
        return Analyze(candidateCardIds, context);
    }

    internal static List<CardAdvice> Analyze(
        IReadOnlyList<string> candidateCardIds,
        SharedDecisionContext context)
    {
        var results = new List<CardAdvice>(candidateCardIds.Count);

        foreach (var candidateCardId in candidateCardIds)
        {
            string runtimeCardId = IdNormalizer.NormalizeModelId(candidateCardId);
            var cardData = DataLoader.GetCard(runtimeCardId);
            var advice = new CardAdvice
            {
                CardId = runtimeCardId,
                CardNameZh = cardData?.NameZh ?? runtimeCardId,
            };

            if (cardData == null)
            {
                advice.OverallRating = CardRating.Neutral;
                advice.OverallReason = "数据库中未找到此牌，无法评估。";
                results.Add(advice);
                continue;
            }

            foreach (var pathState in context.ActivePaths)
            {
                var pathData = DataLoader.GetBuildPath(pathState.PathId);
                if (pathData == null)
                    continue;

                var evaluation = EvaluateCardForPath(cardData, pathData, pathState, context);
                advice.PathRatings[pathState.PathId] = new CardPathRating
                {
                    PathId = pathState.PathId,
                    PathNameZh = pathState.NameZh,
                    Score = evaluation.Score,
                    Rating = evaluation.Rating,
                    Reason = evaluation.Reason,
                };
            }

            ApplyOverallRating(advice, context.ActivePaths);
            results.Add(advice);
        }

        return results;
    }

    /// <summary>
    /// 为商店删牌给出更可信的删除对象。
    /// </summary>
    public static RemoveCardAdvice? AnalyzeRemove(RunSnapshot snapshot, IReadOnlyList<PathState> activePaths)
    {
        if (snapshot == null || snapshot.DeckCardIds.Count == 0)
            return null;

        var context = SharedDecisionContext.Create(snapshot, activePaths);
        return AnalyzeRemove(context);
    }

    internal static RemoveCardAdvice? AnalyzeRemove(SharedDecisionContext context)
    {
        if (context.DeckEntries.Count == 0)
            return null;

        var bestCandidate = context.DeckEntries
            .Select(entry => EvaluateRemoval(entry, context.PrimaryPathData, context.PrimaryPath, context))
            .OrderBy(result => result.KeepScore)
            .FirstOrDefault();

        if (bestCandidate == null)
            return null;

        return new RemoveCardAdvice
        {
            RecommendedCardId = bestCandidate.RuntimeCardId,
            Reason = bestCandidate.Reason,
        };
    }

    private static CardPathEvaluation EvaluateCardForPath(
        CardData card,
        BuildPathData path,
        PathState pathState,
        SharedDecisionContext context)
    {
        float score = card.PathScores.TryGetValue(path.PathId, out var pathScore)
            ? pathScore
            : card.BaseScore;

        var factors = new List<CardScoreFactor>();

        AddFactor(factors, ref score, ComputePathMomentumDelta(pathState),
            pathState.IsDominant ? $"{path.NameZh}已基本成型" : $"{path.NameZh}当前仍是活跃路线");

        if (path.CoreCards.Contains(card.CardId))
            AddFactor(factors, ref score, 1.8f, $"是{path.NameZh}核心组件");

        if (path.CampfireUpgrades.Contains(card.CardId))
            AddFactor(factors, ref score, 0.6f, "升级收益明确，后续篝火好转化");

        AddFactor(factors, ref score, ComputeActDelta(card, context.Snapshot.Act),
            BuildActReason(card, context.Snapshot.Act));

        AddFactor(factors, ref score, ComputePickupWindowDelta(card, context.Snapshot.Act),
            BuildPickupWindowReason(card, context.Snapshot.Act));

        AddFactor(factors, ref score, ComputeNeedDelta(card, context),
            BuildNeedReason(card, context));

        AddFactor(factors, ref score, ComputeRelicDelta(card, context),
            BuildRelicReason(card, context));

        AddFactor(factors, ref score, ComputeDuplicateDelta(card, context),
            BuildDuplicateReason(card, context));

        AddFactor(factors, ref score, ComputeDeckPressureDelta(card, path, context),
            BuildDeckPressureReason(card, path, context));

        AddFactor(factors, ref score, ComputeBossDelta(card, path, context),
            BuildBossReason(card, path, context));

        AddFactor(factors, ref score, ComputeUpgradeTimingDelta(card, context),
            BuildUpgradeTimingReason(card, context));

        score = Math.Clamp(score, SCORE_MIN, SCORE_MAX);
        return new CardPathEvaluation
        {
            Score = score,
            Rating = ScoreToRating(score),
            Reason = BuildReason(card, path, factors),
        };
    }

    private static void ApplyOverallRating(CardAdvice advice, IReadOnlyList<PathState> activePaths)
    {
        if (advice.PathRatings.Count == 0)
        {
            advice.OverallRating = CardRating.Neutral;
            advice.OverallReason = "无活跃构筑方案。";
            return;
        }

        CardPathRating? selected = null;
        foreach (var pathState in activePaths)
        {
            if (!advice.PathRatings.TryGetValue(pathState.PathId, out var current))
                continue;

            if (selected == null || current.Score > selected.Score + 0.01f)
            {
                selected = current;
                continue;
            }

            if (selected != null && Math.Abs(current.Score - selected.Score) < 0.01f && pathState.IsDominant)
                selected = current;
        }

        selected ??= advice.PathRatings.Values.OrderByDescending(r => r.Score).First();
        advice.OverallRating = selected.Rating;
        advice.OverallReason = selected.Reason;
    }

    private static float ComputePathMomentumDelta(PathState pathState)
        => Math.Clamp((pathState.Viability - 0.35f) * 2.2f, -0.5f, 1.0f);

    private static float ComputeActDelta(CardData card, int act)
    {
        float actScore = card.GetActScore(act);
        return Math.Clamp((actScore - 5f) * 0.35f, -1.25f, 1.25f);
    }

    private static string? BuildActReason(CardData card, int act)
    {
        float delta = ComputeActDelta(card, act);
        if (delta >= 0.35f)
            return act switch
            {
                1 => "当前 Act 更看重这类前期即战力",
                2 => "当前 Act 开始需要更高质量中期牌",
                _ => "当前 Act 更能兑现这张牌的上限",
            };

        if (delta <= -0.35f)
            return act switch
            {
                1 => "当前 Act 容易显得偏慢",
                2 => "当前 Act 收益窗口一般",
                _ => "当前 Act 才拿这张牌会偏晚",
            };

        return null;
    }

    private static float ComputePickupWindowDelta(CardData card, int act)
    {
        var windows = card.Advisor?.PickupWindows;
        if (windows == null || windows.Count == 0)
            return 0f;

        bool matches = windows.Any(window => MatchesPickupWindow(window, act));
        if (matches)
            return 0.55f;

        return windows.Count > 0 ? -0.45f : 0f;
    }

    private static string? BuildPickupWindowReason(CardData card, int act)
    {
        float delta = ComputePickupWindowDelta(card, act);
        if (Math.Abs(delta) < 0.05f)
            return null;

        return delta > 0f
            ? "正处于这张牌的强势拿牌窗口"
            : "当前阶段拿这张牌会有些偏早或偏晚";
    }

    private static float ComputeNeedDelta(CardData card, SharedDecisionContext context)
    {
        float delta = 0f;

        if (context.NeedsFrontloadDamage && IsAttackCard(card))
            delta += 0.85f;

        if (context.NeedsDraw && IsDrawCard(card))
            delta += 1.2f;

        if (context.NeedsBlock && IsBlockCard(card))
            delta += 1.0f;

        if (context.NeedsScaling && IsScalingCard(card))
            delta += 1.0f;

        if (context.NeedsAoe && IsAoeCard(card))
            delta += 0.7f;

        return Math.Min(delta, 2.4f);
    }

    private static string? BuildNeedReason(CardData card, SharedDecisionContext context)
    {
        var reasons = new List<string>();
        if (context.NeedsFrontloadDamage && IsAttackCard(card))
            reasons.Add("补前期伤害节奏");
        if (context.NeedsDraw && IsDrawCard(card))
            reasons.Add("补足过牌缺口");
        if (context.NeedsBlock && IsBlockCard(card))
            reasons.Add("缓解当前防御不足");
        if (context.NeedsScaling && IsScalingCard(card))
            reasons.Add("补成长能力");
        if (context.NeedsAoe && IsAoeCard(card))
            reasons.Add("补群体处理手段");
        return reasons.Count > 0 ? string.Join("、", reasons) : null;
    }

    private static float ComputeRelicDelta(CardData card, SharedDecisionContext context)
    {
        float bonus = 0f;
        foreach (var relic in context.Relics)
        {
            if (relic.SynergyTags.Count == 0)
                continue;

            if (relic.SynergyTags.Any(tag => HasTag(card, tag)))
                bonus += 0.45f;
        }

        return Math.Min(bonus, 1.35f);
    }

    private static string? BuildRelicReason(CardData card, SharedDecisionContext context)
    {
        var matchedRelics = context.Relics
            .Where(relic => relic.SynergyTags.Any(tag => HasTag(card, tag)))
            .Select(relic => relic.NameZh)
            .Distinct()
            .Take(2)
            .ToList();

        if (matchedRelics.Count == 0)
            return null;

        return $"与现有遗物协同（{string.Join("/", matchedRelics)}）";
    }

    private static float ComputeDuplicateDelta(CardData card, SharedDecisionContext context)
    {
        int copies = context.GetBaseCardCount(card.CardId);
        if (copies <= 0)
            return 0f;

        float penalty = copies switch
        {
            1 => -0.45f,
            2 => -0.9f,
            _ => -1.35f,
        };

        string policy = card.Advisor?.DuplicatePolicy ?? "neutral";
        penalty *= policy switch
        {
            "pair_ok" => copies == 1 ? 0.3f : 0.75f,
            "avoid_duplicates" => 1.45f,
            "singleton" => 1.7f,
            "stackable" => 0.35f,
            "multi_copy" => 0.35f,
            _ => 1f,
        };

        if (IdNormalizer.IsStarterStrikeOrDefend(card.CardId) && context.Snapshot.Act >= 2)
            penalty -= 0.4f;

        return penalty;
    }

    private static string? BuildDuplicateReason(CardData card, SharedDecisionContext context)
    {
        int copies = context.GetBaseCardCount(card.CardId);
        if (copies <= 0)
            return null;

        string policy = card.Advisor?.DuplicatePolicy ?? "neutral";
        return policy switch
        {
            "pair_ok" when copies == 1 => "这张牌允许带一对，第二张仍可接受",
            "stackable" or "multi_copy" => $"已有 {copies} 张同名牌，但该牌允许多张叠价值",
            "avoid_duplicates" or "singleton" => $"这张牌更偏单卡组件，已有 {copies} 张后继续拿会发虚",
            _ => copies == 1
                ? "牌组里已有同名牌，边际收益开始下降"
                : $"已有 {copies} 张同名牌，继续拿会稀释抽牌质量",
        };
    }

    private static float ComputeDeckPressureDelta(CardData card, BuildPathData path, SharedDecisionContext context)
    {
        int overflow = Math.Max(0, context.DeckSize - path.IdealDeckSize.Max);
        bool smallDeckPath = path.IdealDeckSize.Max <= 14;
        string pressure = card.Advisor?.DeckPressure ?? "neutral";

        float delta = 0f;
        if (overflow > 0)
            delta -= Math.Min(1.6f, overflow * 0.18f);

        if (smallDeckPath)
        {
            if (AddsDeckPressure(card) || pressure is "high" or "thickener")
                delta -= 1.0f;

            if (SupportsSmallDeck(card) || pressure is "low" or "cycler")
                delta += 0.55f;
        }

        if (context.NeedsPurge && pressure is "high" or "thickener")
            delta -= 0.45f;

        if (!smallDeckPath && context.StarterCardCount >= 5 && IdNormalizer.IsStarterStrikeOrDefend(card.CardId))
            delta -= 0.9f;

        return Math.Clamp(delta, -2.2f, 0.9f);
    }

    private static string? BuildDeckPressureReason(CardData card, BuildPathData path, SharedDecisionContext context)
    {
        int overflow = Math.Max(0, context.DeckSize - path.IdealDeckSize.Max);
        bool smallDeckPath = path.IdealDeckSize.Max <= 14;
        string pressure = card.Advisor?.DeckPressure ?? "neutral";

        if (smallDeckPath && (AddsDeckPressure(card) || pressure is "high" or "thickener"))
            return "小牌组路线不想继续接会自我增殖或拖慢循环的牌";

        if (overflow > 0)
            return $"当前牌组已超出{path.NameZh}理想厚度";

        if (context.NeedsPurge && pressure is "high" or "thickener")
            return "当前牌组已经偏厚，这张牌会继续增加删牌压力";

        if (context.StarterCardCount >= 5 && IdNormalizer.IsStarterStrikeOrDefend(card.CardId))
            return "起始牌占比已高，继续拿基础牌收益很低";

        if (smallDeckPath && (SupportsSmallDeck(card) || pressure is "low" or "cycler"))
            return "小牌组路线欢迎低费/过牌/保留类循环组件";

        return null;
    }

    private static float ComputeBossDelta(CardData card, BuildPathData path, SharedDecisionContext context)
    {
        if (context.CurrentBoss == null)
            return 0f;

        float delta = 0f;
        if (context.CurrentBoss.CounterCards.Contains(card.CardId))
            delta += 1.0f;

        if (path.BadAgainstBosses.Contains(context.CurrentBoss.BossId) ||
            context.CurrentBoss.DangerousToPaths.Contains(path.PathId))
        {
            if (context.CurrentBoss.CounterCards.Contains(card.CardId))
                delta += 0.45f;
        }
        else if (path.GoodAgainstBosses.Contains(context.CurrentBoss.BossId) && context.CurrentBoss.CounterCards.Contains(card.CardId))
        {
            delta += 0.2f;
        }

        return Math.Min(delta, 1.45f);
    }

    private static string? BuildBossReason(CardData card, BuildPathData path, SharedDecisionContext context)
    {
        if (context.CurrentBoss == null || !context.CurrentBoss.CounterCards.Contains(card.CardId))
            return null;

        if (path.BadAgainstBosses.Contains(context.CurrentBoss.BossId) ||
            context.CurrentBoss.DangerousToPaths.Contains(path.PathId))
        {
            return $"能补当前 Boss（{context.CurrentBoss.NameZh}）对策";
        }

        return $"对当前 Boss（{context.CurrentBoss.NameZh}）有针对性";
    }

    private static float ComputeUpgradeTimingDelta(CardData card, SharedDecisionContext context)
    {
        float delta = 0f;

        if (context.Snapshot.Act >= 2 && card.UpgradePriorityScore >= 8f)
            delta += 0.45f;

        if (context.Snapshot.Act == 1 && IsHighCostSlowCard(card) && context.HPRatio < 0.50f)
            delta -= 0.6f;

        return delta;
    }

    private static string? BuildUpgradeTimingReason(CardData card, SharedDecisionContext context)
    {
        float delta = ComputeUpgradeTimingDelta(card, context);
        if (delta >= 0.35f)
            return "后续有较高升级转化率";

        if (delta <= -0.35f)
            return "当前血量与节奏不太适合拿慢牌";

        return null;
    }

    private static RemovalEvaluation EvaluateRemoval(
        DeckCardEntry entry,
        BuildPathData? primaryPathData,
        PathState? primaryPathState,
        SharedDecisionContext context)
    {
        var card = entry.Card;
        float keepScore = card?.BaseScore ?? 4f;
        var reasons = new List<string>();

        if (card != null && primaryPathData != null)
            keepScore = card.PathScores.TryGetValue(primaryPathData.PathId, out var pathScore) ? pathScore : card.BaseScore;

        if (primaryPathState != null)
            keepScore += ComputePathMomentumDelta(primaryPathState) * 0.5f;

        if (entry.IsUpgraded)
        {
            keepScore += 2.0f;
            reasons.Add("已升级");
        }

        if (primaryPathData != null && primaryPathData.CoreCards.Contains(entry.BaseCardId))
        {
            keepScore += 2.5f;
            reasons.Add("当前主路线核心牌");
        }

        if (card != null)
        {
            if (context.NeedsDraw && IsDrawCard(card))
                keepScore += 1.2f;
            if (context.NeedsBlock && IsBlockCard(card))
                keepScore += 1.0f;
            if (context.NeedsScaling && IsScalingCard(card))
                keepScore += 0.8f;

            if (card.Advisor?.RemovePriority is float removePriority)
            {
                keepScore -= Math.Clamp(removePriority, 0f, 10f) * 0.35f;
                if (removePriority >= 6f)
                    reasons.Add("数据库标记为高优先删牌");
            }
        }

        if (IdNormalizer.IsStarterStrikeOrDefend(entry.RuntimeCardId))
        {
            keepScore -= 2.6f;
            reasons.Add("起始牌后期边际收益低");
        }

        int copies = context.GetBaseCardCount(entry.BaseCardId);
        if (copies > 1)
        {
            float duplicatePenalty = Math.Abs(ComputeDuplicateDelta(card ?? new CardData { CardId = entry.BaseCardId }, context));
            keepScore -= Math.Min(1.8f, duplicatePenalty + 0.2f);
            reasons.Add($"已有 {copies} 张同名牌");
        }

        if (card != null)
        {
            string pressure = card.Advisor?.DeckPressure ?? "neutral";
            if (context.NeedsPurge && pressure is "high" or "thickener")
            {
                keepScore -= 0.8f;
                reasons.Add("会继续加重牌组厚度");
            }

            if (card.Rarity.Equals("Basic", StringComparison.OrdinalIgnoreCase) && context.Snapshot.Act >= 2)
                keepScore -= 0.5f;
        }

        if (context.NeedsPurge && IdNormalizer.IsStarterStrikeOrDefend(entry.RuntimeCardId))
            keepScore -= 0.6f;

        string reason = reasons.Count > 0
            ? string.Join("；", reasons) + "，优先移除以提升牌组密度"
            : "当前边际收益最低，可优先删除以提升抽牌质量";

        return new RemovalEvaluation
        {
            RuntimeCardId = entry.RuntimeCardId,
            KeepScore = keepScore,
            Reason = reason,
        };
    }

    internal static CardRating ScoreToRating(float score) => score switch
    {
        >= 8.5f => CardRating.CorePick,
        >= 6.5f => CardRating.GoodPick,
        >= 4.5f => CardRating.Situational,
        >= 2.5f => CardRating.Weak,
        _ => CardRating.Skip,
    };

    private static string BuildReason(CardData card, BuildPathData path, List<CardScoreFactor> factors)
    {
        var positive = factors
            .Where(f => f.Delta > 0.15f)
            .OrderByDescending(f => f.Delta)
            .Take(2)
            .Select(f => f.Message)
            .ToList();

        var negative = factors
            .Where(f => f.Delta < -0.15f)
            .OrderBy(f => f.Delta)
            .Take(1)
            .Select(f => $"但{f.Message}")
            .ToList();

        var parts = new List<string>();
        parts.AddRange(positive);
        parts.AddRange(negative);

        if (parts.Count == 0)
            parts.Add("当前收益中性，更多取决于路线收束情况");

        if (!string.IsNullOrWhiteSpace(card.NotesZh))
            parts.Add(card.NotesZh.Trim().TrimEnd('。', '！', '？'));

        return $"{path.NameZh}：{string.Join("；", parts)}。";
    }

    private static void AddFactor(List<CardScoreFactor> factors, ref float score, float delta, string? message)
    {
        if (Math.Abs(delta) < 0.05f || string.IsNullOrWhiteSpace(message))
            return;

        score += delta;
        factors.Add(new CardScoreFactor(delta, message));
    }

    internal static bool IsAttackCard(CardData card)
        => card.Type.Equals("Attack", StringComparison.OrdinalIgnoreCase)
           || GetBaseProfile(card).Damage.GetValueOrDefault() > 0
           || HasAdvisorRole(card, "attack")
           || HasAdvisorRole(card, "damage")
           || HasTag(card, "attack");

    internal static bool IsDrawCard(CardData card)
        => GetBaseProfile(card).Draw.GetValueOrDefault() > 0
           || HasAdvisorRole(card, "draw")
           || HasAnyTag(card, DrawTags);

    internal static bool IsBlockCard(CardData card)
        => card.CardId.StartsWith("DEFEND_", StringComparison.OrdinalIgnoreCase)
           || GetBaseProfile(card).Block.GetValueOrDefault() > 0
           || HasTag(card, "block_scaling")
           || ContainsText(card.NotesZh, "格挡");

    internal static bool IsScalingCard(CardData card)
        => card.Type.Equals("Power", StringComparison.OrdinalIgnoreCase)
           || GetBaseProfile(card).ScaleFrom.Count > 0
           || card.Flags.UpgradesHand == true
           || card.Flags.UpgradesDeck == true
           || HasAnyApplyKey(card, ScalingApplyKeys)
           || HasAdvisorRole(card, "scaling")
           || HasAnyTag(card, ScalingTags);

    internal static bool IsAoeCard(CardData card)
        => string.Equals(card.Target, "AllEnemies", StringComparison.OrdinalIgnoreCase)
           || HasAdvisorRole(card, "aoe")
           || HasAnyTag(card, AoeTags);

    private static bool SupportsSmallDeck(CardData card)
        => card.Cost == 0
           || IsDrawCard(card)
           || card.Flags.Exhaust == true
           || card.Flags.Retain == true
           || HasAdvisorRole(card, "consistency")
           || HasAnyTag(card, SmallDeckSupportTags);

    private static bool AddsDeckPressure(CardData card)
        => card.Flags.SelfReplicate == true
           || HasAnyTag(card, SmallDeckPressureTags);

    private static CardEffectProfile GetBaseProfile(CardData card)
        => card.Effects?.Base ?? new CardEffectProfile();

    private static bool HasAdvisorRole(CardData card, string role)
        => card.Advisor?.Roles?.Any(existing => existing.Equals(role, StringComparison.OrdinalIgnoreCase)) == true;

    private static bool HasAnyApplyKey(CardData card, HashSet<string> keys)
        => GetBaseProfile(card).Apply.Keys.Any(keys.Contains);

    private static bool MatchesPickupWindow(string window, int act)
    {
        string normalized = window.Trim().ToLowerInvariant();
        return normalized switch
        {
            "act1" or "early" => act <= 1,
            "act2" or "mid" => act == 2,
            "act3" or "late" or "act4" => act >= 3,
            _ => false,
        };
    }

    private static bool IsHighCostSlowCard(CardData card)
        => card.Cost >= 2
           && card.Type.Equals("Power", StringComparison.OrdinalIgnoreCase)
           && !IsDrawCard(card);

    private static bool HasTag(CardData card, string tag)
        => card.SynergyTags.Any(existing => existing.Equals(tag, StringComparison.OrdinalIgnoreCase));

    private static bool HasAnyTag(CardData card, HashSet<string> tags)
        => card.SynergyTags.Any(tags.Contains);

    private static bool ContainsText(string source, string value)
        => !string.IsNullOrWhiteSpace(source)
           && source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;

}

internal sealed class CardPathEvaluation
{
    public float Score { get; init; }
    public CardRating Rating { get; init; }
    public string Reason { get; init; } = string.Empty;
}

internal sealed class CardScoreFactor
{
    public CardScoreFactor(float delta, string message)
    {
        Delta = delta;
        Message = message;
    }

    public float Delta { get; }
    public string Message { get; }
}

internal sealed class RemovalEvaluation
{
    public string RuntimeCardId { get; init; } = string.Empty;
    public float KeepScore { get; init; }
    public string Reason { get; init; } = string.Empty;
}

public class CardAdvice
{
    public string CardId { get; set; } = string.Empty;
    public string CardNameZh { get; set; } = string.Empty;

    /// <summary>综合推荐评级（基于加权后的最佳活跃路线）</summary>
    public CardRating OverallRating { get; set; }

    /// <summary>综合推荐理由</summary>
    public string OverallReason { get; set; } = string.Empty;

    /// <summary>各方案对此牌的独立评级（key = path_id）</summary>
    public Dictionary<string, CardPathRating> PathRatings { get; set; } = new();
}

public class CardPathRating
{
    public string PathId { get; set; } = string.Empty;
    public string PathNameZh { get; set; } = string.Empty;
    public float Score { get; set; }
    public CardRating Rating { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public enum CardRating
{
    CorePick,
    GoodPick,
    Situational,
    Weak,
    Skip,
    Neutral,
}
