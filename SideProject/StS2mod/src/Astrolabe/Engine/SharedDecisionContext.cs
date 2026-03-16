using System;
using System.Collections.Generic;
using System.Linq;
using Astrolabe.Core;
using Astrolabe.Data;

namespace Astrolabe.Engine;

/// <summary>
/// 为 Card / Shop / Campfire 等场景共享的运行时决策上下文。
/// 统一封装当前 Run 的路线状态、牌组形态、遗物、Boss 与功能缺口，
/// 避免每个 Advisor 重复从 <see cref="RunSnapshot"/> 重新拼装分析数据。
/// </summary>
internal sealed class SharedDecisionContext
{
    public RunSnapshot Snapshot { get; init; } = new();
    public IReadOnlyList<PathState> ActivePaths { get; init; } = Array.Empty<PathState>();
    public PathState? PrimaryPath { get; init; }
    public BuildPathData? PrimaryPathData { get; init; }
    public List<DeckCardEntry> DeckEntries { get; init; } = new();
    public List<RelicData> Relics { get; init; } = new();
    public BossData? CurrentBoss { get; init; }
    public Dictionary<string, int> BaseCardCounts { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public int StarterCardCount { get; init; }
    public int AttackCardCount { get; init; }
    public int DrawCardCount { get; init; }
    public int BlockCardCount { get; init; }
    public int ScalingCardCount { get; init; }
    public int AoeCardCount { get; init; }
    public int IdealDeckMax { get; init; }
    public int OverflowCards { get; init; }
    public float HPRatio { get; init; }
    public bool WantsThinDeck { get; init; }
    public bool NeedsFrontloadDamage { get; init; }
    public bool NeedsDraw { get; init; }
    public bool NeedsBlock { get; init; }
    public bool NeedsScaling { get; init; }
    public bool NeedsAoe { get; init; }
    public bool NeedsPurge { get; init; }

    public int DeckSize => DeckEntries.Count;

    public int GetBaseCardCount(string cardId)
        => BaseCardCounts.TryGetValue(IdNormalizer.NormalizeLookupId(cardId), out var count) ? count : 0;

    public static SharedDecisionContext Create(RunSnapshot snapshot, IReadOnlyList<PathState> activePaths)
    {
        var primaryPath = activePaths.FirstOrDefault();
        var primaryPathData = primaryPath != null ? DataLoader.GetBuildPath(primaryPath.PathId) : null;

        var deckEntries = snapshot.DeckCardIds
            .Select(runtimeId =>
            {
                string normalizedRuntimeId = IdNormalizer.NormalizeModelId(runtimeId);
                string baseId = IdNormalizer.NormalizeLookupId(runtimeId);
                return new DeckCardEntry
                {
                    RuntimeCardId = normalizedRuntimeId,
                    BaseCardId = baseId,
                    IsUpgraded = normalizedRuntimeId.EndsWith("+", StringComparison.Ordinal),
                    Card = DataLoader.GetCard(normalizedRuntimeId),
                };
            })
            .ToList();

        var baseCounts = deckEntries
            .GroupBy(entry => entry.BaseCardId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

        float hpRatio = snapshot.MaxHP > 0 ? (float)snapshot.HP / snapshot.MaxHP : 1f;
        int deckSize = deckEntries.Count;
        int starterCount = deckEntries.Count(entry => IdNormalizer.IsStarterStrikeOrDefend(entry.RuntimeCardId));
        int attackCount = deckEntries.Count(entry => entry.Card != null && CardAdvisor.IsAttackCard(entry.Card));
        int drawCount = deckEntries.Count(entry => entry.Card != null && CardAdvisor.IsDrawCard(entry.Card));
        int blockCount = deckEntries.Count(entry => entry.Card != null && CardAdvisor.IsBlockCard(entry.Card));
        int scalingCount = deckEntries.Count(entry => entry.Card != null && CardAdvisor.IsScalingCard(entry.Card));
        int aoeCount = deckEntries.Count(entry => entry.Card != null && CardAdvisor.IsAoeCard(entry.Card));

        int idealDeckMax = primaryPathData?.IdealDeckSize.Max ?? 18;
        int overflowCards = Math.Max(0, deckSize - idealDeckMax);
        bool wantsThinDeck = primaryPathData?.IdealDeckSize.Max <= 14;

        bool needsFrontloadDamage = snapshot.Act == 1 && attackCount < Math.Max(5, Math.Max(3, deckSize / 4));
        bool needsDraw = drawCount < Math.Max(2, deckSize / 8)
            || (wantsThinDeck && drawCount < Math.Max(2, deckSize / 7));
        bool needsBlock = blockCount < Math.Max(4, deckSize / 5)
            || (hpRatio < 0.50f && blockCount < Math.Max(5, deckSize / 4));
        bool needsScaling = snapshot.Act >= 2 && scalingCount < Math.Max(2, deckSize / 10);
        bool needsAoe = snapshot.Act <= 2 && aoeCount == 0;
        bool needsPurge = overflowCards > 0
            || starterCount >= Math.Max(4, deckSize / 3)
            || (wantsThinDeck && deckSize > Math.Max(10, idealDeckMax - 1));

        return new SharedDecisionContext
        {
            Snapshot = snapshot,
            ActivePaths = activePaths,
            PrimaryPath = primaryPath,
            PrimaryPathData = primaryPathData,
            DeckEntries = deckEntries,
            Relics = snapshot.RelicIds
                .Select(DataLoader.GetRelic)
                .Where(relic => relic != null)
                .Cast<RelicData>()
                .ToList(),
            CurrentBoss = string.IsNullOrWhiteSpace(snapshot.ActBossId) ? null : DataLoader.GetBoss(snapshot.ActBossId),
            BaseCardCounts = baseCounts,
            StarterCardCount = starterCount,
            AttackCardCount = attackCount,
            DrawCardCount = drawCount,
            BlockCardCount = blockCount,
            ScalingCardCount = scalingCount,
            AoeCardCount = aoeCount,
            IdealDeckMax = idealDeckMax,
            OverflowCards = overflowCards,
            HPRatio = hpRatio,
            WantsThinDeck = wantsThinDeck,
            NeedsFrontloadDamage = needsFrontloadDamage,
            NeedsDraw = needsDraw,
            NeedsBlock = needsBlock,
            NeedsScaling = needsScaling,
            NeedsAoe = needsAoe,
            NeedsPurge = needsPurge,
        };
    }
}

internal sealed class DeckCardEntry
{
    public string RuntimeCardId { get; init; } = string.Empty;
    public string BaseCardId { get; init; } = string.Empty;
    public bool IsUpgraded { get; init; }
    public CardData? Card { get; init; }
}
