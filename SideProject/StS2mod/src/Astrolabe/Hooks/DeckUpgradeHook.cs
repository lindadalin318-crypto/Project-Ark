using System.Reflection;
using Astrolabe.Core;
using Astrolabe.Data;
using Astrolabe.Engine;
using Astrolabe.UI;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;

namespace Astrolabe.Hooks;

/// <summary>
/// Hook 牌组升级选择界面（NDeckUpgradeSelectScreen），
/// 在界面打开时把 Astrolabe 的推荐升级目标直接映射到真实可点击的卡牌上。
/// </summary>
public static class DeckUpgradeHook
{
    private const string RecommendationBadgeName = "AstrolabeUpgradeBadge";

    private static readonly Logger _log = new("Astrolabe.DeckUpgradeHook", LogType.Generic);
    private static readonly Dictionary<NDeckUpgradeSelectScreen, List<NGridCardHolder>> HighlightedHolders = new();
    private static readonly Dictionary<NDeckUpgradeSelectScreen, UpgradeSelectionSession> ActiveSessions = new();

    private static IReadOnlyList<CardModel>? _pendingCards;
    private static AdviceEnvelope<CampfireAdvice>? _pendingCampfireEnvelope;

    public static void Register(Harmony harmony)
    {
        try
        {
            var screenType = typeof(NDeckUpgradeSelectScreen);
            var showMethod = screenType.GetMethod("ShowScreen", BindingFlags.Static | BindingFlags.Public);
            var readyMethod = screenType.GetMethod("_Ready", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var exitTreeMethod = screenType.GetMethod("_ExitTree", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (showMethod != null)
            {
                var showPostfix = typeof(DeckUpgradeHook).GetMethod(nameof(OnShowScreen), BindingFlags.Static | BindingFlags.NonPublic);
                harmony.Patch(showMethod, postfix: new HarmonyMethod(showPostfix));
                _log.Info("[DeckUpgradeHook] Patched NDeckUpgradeSelectScreen.ShowScreen");
            }
            else
            {
                _log.Error("[DeckUpgradeHook] Cannot find ShowScreen on NDeckUpgradeSelectScreen");
            }

            if (readyMethod != null)
            {
                var readyPostfix = typeof(DeckUpgradeHook).GetMethod(nameof(OnScreenReady), BindingFlags.Static | BindingFlags.NonPublic);
                harmony.Patch(readyMethod, postfix: new HarmonyMethod(readyPostfix));
                _log.Info("[DeckUpgradeHook] Patched NDeckUpgradeSelectScreen._Ready");
            }
            else
            {
                _log.Error("[DeckUpgradeHook] Cannot find _Ready on NDeckUpgradeSelectScreen");
            }

            if (exitTreeMethod != null)
            {
                var exitPostfix = typeof(DeckUpgradeHook).GetMethod(nameof(OnScreenExitTree), BindingFlags.Static | BindingFlags.NonPublic);
                harmony.Patch(exitTreeMethod, postfix: new HarmonyMethod(exitPostfix));
                _log.Info("[DeckUpgradeHook] Patched NDeckUpgradeSelectScreen._ExitTree");
            }
        }
        catch (Exception ex)
        {
            _log.Error($"[DeckUpgradeHook] Register failed: {ex.Message}");
        }
    }

    public static void CacheCampfireAdvice(AdviceEnvelope<CampfireAdvice> envelope)
    {
        _pendingCampfireEnvelope = envelope;
    }

    public static void ClearCampfireContext()
    {
        _pendingCampfireEnvelope = null;
    }

    [HarmonyPostfix]
    private static void OnShowScreen(IReadOnlyList<CardModel> cards)
    {
        _pendingCards = cards;
    }

    [HarmonyPostfix]
    private static void OnScreenReady(NDeckUpgradeSelectScreen __instance)
    {
        try
        {
            OverlayHUD.EnsureInjected(__instance);

            var cards = ResolveCards(__instance);
            if (cards == null || cards.Count == 0)
            {
                _log.Warn("[DeckUpgradeHook] No cards resolved for upgrade screen.");
                return;
            }

            RunSnapshot snapshot = RunStateReader.Capture();
            if (!snapshot.IsValid)
            {
                _log.Warn("[DeckUpgradeHook] Invalid snapshot, skipping upgrade recommendation.");
                return;
            }

            if (!ShouldUseCampfireSmithAssist())
            {
                _log.Info("[DeckUpgradeHook] Current upgrade screen is not in campfire Smith context, skipping strong guidance.");
                return;
            }

            var candidateCardIds = cards
                .Select(ToRuntimeCardId)
                .ToList();

            var envelope = ResolveUpgradeAdvice(candidateCardIds, snapshot, _pendingCampfireEnvelope);
            if (envelope == null)
            {
                _log.Info("[DeckUpgradeHook] No upgrade recommendation generated for current Smith candidates.");
                return;
            }

            ApplyRecommendation(__instance, cards, envelope.Payload);
            ActiveSessions[__instance] = new UpgradeSelectionSession(snapshot, candidateCardIds, envelope);
            _log.Info($"[DeckUpgradeHook] Recommended upgrade target: {envelope.Payload.TargetCardNameZh} ({envelope.Payload.TargetCardId}) / Trace: {envelope.TraceId}");
        }
        catch (Exception ex)
        {
            _log.Error($"[DeckUpgradeHook] OnScreenReady failed: {ex.Message}");
        }
        finally
        {
            _pendingCards = null;
        }
    }

    [HarmonyPostfix]
    private static void OnScreenExitTree(NDeckUpgradeSelectScreen __instance)
    {
        try
        {
            if (!ActiveSessions.TryGetValue(__instance, out var session))
                return;

            RunSnapshot exitSnapshot = RunStateReader.Capture();
            if (!exitSnapshot.IsValid)
                return;

            string? chosenCardId = InferUpgradedCard(session, exitSnapshot);
            if (string.IsNullOrWhiteSpace(chosenCardId))
            {
                _log.Info($"[DeckUpgradeHook] Upgrade screen closed without detectable deck diff. Trace: {session.Envelope.TraceId}");
                return;
            }

            var record = DecisionRecordFactory.CreatePlayerChoiceRecord(
                session.Envelope,
                exitSnapshot,
                chosenCardId,
                source: "DeckUpgradeHook.OnScreenExitTree",
                extraMetadata: new Dictionary<string, string>
                {
                    ["screen"] = "deck-upgrade",
                    ["choiceInference"] = "deck-upgrade-diff",
                });

            DecisionRecorder.Record(record);
            _log.Info($"[DeckUpgradeHook] Player upgrade recorded. Trace: {session.Envelope.TraceId}, Choice: {chosenCardId}, Followed: {record.PlayerFollowedAdvice}");
        }
        catch (Exception ex)
        {
            _log.Error($"[DeckUpgradeHook] OnScreenExitTree failed: {ex.Message}");
        }
        finally
        {
            CleanupRecommendation(__instance);
            ActiveSessions.Remove(__instance);
        }
    }

    private static IReadOnlyList<CardModel>? ResolveCards(NDeckUpgradeSelectScreen screen)
    {
        if (_pendingCards != null && _pendingCards.Count > 0)
            return _pendingCards;

        var field = typeof(NCardGridSelectionScreen).GetField("_cards", BindingFlags.Instance | BindingFlags.NonPublic);
        return field?.GetValue(screen) as IReadOnlyList<CardModel>;
    }

    private static bool ShouldUseCampfireSmithAssist()
    {
        if (_pendingCampfireEnvelope == null)
            return false;

        if (_pendingCampfireEnvelope.Payload.RecommendedAction is not (CampfireAction.Upgrade or CampfireAction.Smith))
            return false;

        return NRestSiteRoom.Instance != null
            && GodotObject.IsInstanceValid(NRestSiteRoom.Instance)
            && NRestSiteRoom.Instance.IsVisibleInTree();
    }

    private static AdviceEnvelope<UpgradeSelectionAdvice>? ResolveUpgradeAdvice(
        IReadOnlyList<string> candidateCardIds,
        RunSnapshot snapshot,
        AdviceEnvelope<CampfireAdvice>? campfireEnvelope)
    {
        if (campfireEnvelope != null && !string.IsNullOrWhiteSpace(campfireEnvelope.Payload.UpgradeTargetCardId))
        {
            string targetBaseId = IdNormalizer.NormalizeLookupId(campfireEnvelope.Payload.UpgradeTargetCardId);
            bool targetVisible = candidateCardIds.Any(cardId => string.Equals(
                IdNormalizer.NormalizeLookupId(cardId),
                targetBaseId,
                StringComparison.OrdinalIgnoreCase));

            if (targetVisible)
            {
                string targetName = string.IsNullOrWhiteSpace(campfireEnvelope.Payload.UpgradeTargetCardNameZh)
                    ? campfireEnvelope.Payload.UpgradeTargetCardId
                    : campfireEnvelope.Payload.UpgradeTargetCardNameZh;

                var advice = new UpgradeSelectionAdvice
                {
                    TargetCardId = campfireEnvelope.Payload.UpgradeTargetCardId,
                    TargetCardNameZh = targetName,
                    SummaryText = $"星象仪推荐：优先升级「{targetName}」",
                    Reason = campfireEnvelope.Payload.Reason,
                };

                var record = DecisionRecordFactory.CreateUpgradeSelectionRecord(
                    campfireEnvelope.TraceId,
                    snapshot,
                    BuildPathManager.GetActivePaths(),
                    candidateCardIds,
                    advice,
                    source: "DeckUpgradeHook.ResolveCampfireUpgradeSelection",
                    extraMetadata: new Dictionary<string, string>
                    {
                        ["bridgeOrigin"] = "campfire",
                        ["campfireRecommendedAction"] = campfireEnvelope.Payload.RecommendedAction.ToString(),
                    });

                DecisionRecorder.Record(record);
                return AdviceEnvelope<UpgradeSelectionAdvice>.FromRecord(record, advice);
            }
        }

        return AdvisorEngine.AnalyzeUpgradeSelection(candidateCardIds, snapshot);
    }

    private static void ApplyRecommendation(
        NDeckUpgradeSelectScreen screen,
        IReadOnlyList<CardModel> cards,
        UpgradeSelectionAdvice advice)
    {
        var grid = ResolveGrid(screen);
        if (grid == null)
        {
            _log.Warn("[DeckUpgradeHook] Failed to resolve NCardGrid from screen.");
            return;
        }

        string targetBaseId = IdNormalizer.NormalizeLookupId(advice.TargetCardId);
        var matchedHolders = new List<NGridCardHolder>();

        foreach (var card in cards)
        {
            if (!string.Equals(IdNormalizer.NormalizeLookupId(card.Id.Entry), targetBaseId, StringComparison.OrdinalIgnoreCase))
                continue;

            var holder = grid.GetCardHolder(card);
            if (holder == null)
                continue;

            HighlightHolder(holder);
            matchedHolders.Add(holder);
        }

        if (matchedHolders.Count == 0)
        {
            _log.Warn($"[DeckUpgradeHook] Could not match recommended card {advice.TargetCardId} to any holder.");
            return;
        }

        HighlightedHolders[screen] = matchedHolders;
        FocusRecommendedHolder(matchedHolders[0]);
        AppendRecommendationText(screen, advice, matchedHolders.Count > 1, autoFocusScheduled: true);
    }

    private static NCardGrid? ResolveGrid(NDeckUpgradeSelectScreen screen)
    {
        var field = typeof(NCardGridSelectionScreen).GetField("_grid", BindingFlags.Instance | BindingFlags.NonPublic);
        return field?.GetValue(screen) as NCardGrid;
    }

    private static void HighlightHolder(NGridCardHolder holder)
    {
        if (holder.CardNode == null)
            return;

        holder.CardNode.CardHighlight.Modulate = NCardHighlight.gold;
        holder.CardNode.CardHighlight.AnimShow();
        holder.CardNode.ActivateRewardScreenGlow();

        if (holder.GetNodeOrNull<Control>(RecommendationBadgeName) != null)
            return;

        var badge = new Panel
        {
            Name = RecommendationBadgeName,
            Position = new Vector2(10, 8),
            Size = new Vector2(76, 22),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        badge.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(1f, 0.82f, 0.25f, 0.94f),
        });

        var label = new Label
        {
            Text = "★ 推荐",
            Position = new Vector2(8, 2),
            Size = new Vector2(60, 18),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        label.AddThemeColorOverride("font_color", new Color(0.08f, 0.08f, 0.08f));
        label.AddThemeFontSizeOverride("font_size", 11);

        badge.AddChild(label);
        holder.AddChild(badge);
    }

    private static void FocusRecommendedHolder(NGridCardHolder holder)
    {
        Callable.From(() =>
        {
            if (!GodotObject.IsInstanceValid(holder) || !holder.IsInsideTree())
                return;

            var focusTarget = GodotObject.IsInstanceValid(holder.Hitbox)
                ? (Control)holder.Hitbox
                : holder;

            focusTarget.GrabFocus();
        }).CallDeferred();
    }

    private static void AppendRecommendationText(
        NDeckUpgradeSelectScreen screen,
        UpgradeSelectionAdvice advice,
        bool matchedMultipleCopies,
        bool autoFocusScheduled)
    {
        var field = typeof(NDeckUpgradeSelectScreen).GetField("_infoLabel", BindingFlags.Instance | BindingFlags.NonPublic);
        var infoLabel = field?.GetValue(screen);
        if (infoLabel == null)
            return;

        var textProperty = infoLabel.GetType().GetProperty("Text");
        if (textProperty == null || !textProperty.CanRead || !textProperty.CanWrite)
            return;

        string existingText = textProperty.GetValue(infoLabel) as string ?? string.Empty;
        string suffix = matchedMultipleCopies ? "（同名牌已全部标记）" : string.Empty;
        string focusHint = autoFocusScheduled ? "\n已自动定位到推荐牌，可直接确认或改选。" : string.Empty;
        string recommendationText = $"★ 星象仪推荐：优先升级「{advice.TargetCardNameZh}」{suffix}{focusHint}\n理由：{advice.Reason}";

        if (existingText.Contains(recommendationText, StringComparison.Ordinal))
            return;

        textProperty.SetValue(infoLabel,
            string.IsNullOrWhiteSpace(existingText)
                ? recommendationText
                : existingText + "\n" + recommendationText);
    }

    private static void CleanupRecommendation(NDeckUpgradeSelectScreen screen)
    {
        if (!HighlightedHolders.TryGetValue(screen, out var holders))
            return;

        foreach (var holder in holders)
        {
            if (!GodotObject.IsInstanceValid(holder))
                continue;

            holder.GetNodeOrNull<Control>(RecommendationBadgeName)?.QueueFree();

            if (holder.CardNode == null)
                continue;

            holder.CardNode.CardHighlight.Modulate = NCardHighlight.playableColor;
            holder.CardNode.CardHighlight.AnimHideInstantly();
            holder.CardNode.KillRarityGlow();
        }

        HighlightedHolders.Remove(screen);
    }

    private static string? InferUpgradedCard(UpgradeSelectionSession session, RunSnapshot exitSnapshot)
    {
        var beforeCounts = BuildCounts(session.EntrySnapshot.DeckCardIds);
        var afterCounts = BuildCounts(exitSnapshot.DeckCardIds);

        foreach (var candidateCardId in session.CandidateCardIds)
        {
            string baseId = IdNormalizer.NormalizeLookupId(candidateCardId);
            string runtimeBaseId = IdNormalizer.NormalizeModelId(baseId);
            string upgradedId = runtimeBaseId + "+";

            if (GetCount(afterCounts, upgradedId) > GetCount(beforeCounts, upgradedId) &&
                GetCount(afterCounts, runtimeBaseId) < GetCount(beforeCounts, runtimeBaseId))
            {
                return runtimeBaseId;
            }
        }

        return null;
    }

    private static Dictionary<string, int> BuildCounts(IEnumerable<string> ids)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var rawId in ids)
        {
            string id = IdNormalizer.NormalizeModelId(rawId);
            if (string.IsNullOrWhiteSpace(id))
                continue;

            counts[id] = GetCount(counts, id) + 1;
        }

        return counts;
    }

    private static int GetCount(IReadOnlyDictionary<string, int> counts, string id)
        => counts.TryGetValue(id, out int count) ? count : 0;

    private static string ToRuntimeCardId(CardModel card)
        => IdNormalizer.NormalizeModelId(card.IsUpgraded ? card.Id.Entry + "+" : card.Id.Entry);

    private sealed class UpgradeSelectionSession
    {
        public UpgradeSelectionSession(
            RunSnapshot entrySnapshot,
            IReadOnlyList<string> candidateCardIds,
            AdviceEnvelope<UpgradeSelectionAdvice> envelope)
        {
            EntrySnapshot = entrySnapshot;
            CandidateCardIds = candidateCardIds;
            Envelope = envelope;
        }

        public RunSnapshot EntrySnapshot { get; }
        public IReadOnlyList<string> CandidateCardIds { get; }
        public AdviceEnvelope<UpgradeSelectionAdvice> Envelope { get; }
    }
}
