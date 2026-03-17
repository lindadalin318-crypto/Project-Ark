using System.Reflection;
using Astrolabe.Core;
using Astrolabe.Data;
using Astrolabe.Engine;
using Astrolabe.UI;

using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Rewards;

namespace Astrolabe.Hooks;

/// <summary>
/// Hook 卡牌奖励界面（NCardRewardSelectionScreen）。
/// 
/// 已通过 ILSpy 确认：
///   - 类名：MegaCrit.Sts2.Core.Nodes.Screens.CardSelection.NCardRewardSelectionScreen
///   - 入口方法：_Ready()（Godot Node 生命周期，界面显示时调用）
///   - 卡牌列表：私有字段 _options（IReadOnlyList&lt;CardCreationResult&gt;）
///     通过静态方法 ShowScreen(options, extraOptions) 传入，用 Reflection 读取
///   - 也可 Hook CardReward.ShowPickScreen() 提前捕获数据
/// </summary>
public static class CardRewardHook
{
    private static readonly Logger _log = new("Astrolabe.CardRewardHook", LogType.Generic);
    private static readonly Dictionary<NCardRewardSelectionScreen, CardRewardSession> ActiveSessions = new();

    // 用于缓存最近一次 ShowScreen 的候选牌，在 _Ready postfix 中使用
    private static IReadOnlyList<CardCreationResult>? _pendingOptions;

    public static void Register(Harmony harmony)
    {
        try
        {
            // Hook 1: NCardRewardSelectionScreen._Ready()
            // 卡牌奖励界面节点就绪时触发，此时 _options 已被赋值
            var screenType = typeof(NCardRewardSelectionScreen);
            var readyMethod = screenType.GetMethod("_Ready",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var exitTreeMethod = screenType.GetMethod("_ExitTree",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (readyMethod != null)
            {
                var readyPostfix = typeof(CardRewardHook).GetMethod(
                    nameof(OnScreenReady),
                    BindingFlags.Static | BindingFlags.NonPublic);
                harmony.Patch(readyMethod, postfix: new HarmonyMethod(readyPostfix));
                _log.Info("[CardRewardHook] Patched NCardRewardSelectionScreen._Ready");
            }
            else
            {
                _log.Error("[CardRewardHook] Cannot find _Ready on NCardRewardSelectionScreen");
            }

            if (exitTreeMethod != null)
            {
                var exitPostfix = typeof(CardRewardHook).GetMethod(
                    nameof(OnScreenExitTree),
                    BindingFlags.Static | BindingFlags.NonPublic);
                harmony.Patch(exitTreeMethod, postfix: new HarmonyMethod(exitPostfix));
                _log.Info("[CardRewardHook] Patched NCardRewardSelectionScreen._ExitTree");
            }

            // Hook 2: NCardRewardSelectionScreen.ShowScreen() — 静态方法
            // 在界面实例化前捕获候选牌列表（更早一步）
            var showMethod = screenType.GetMethod("ShowScreen",
                BindingFlags.Static | BindingFlags.Public);

            if (showMethod != null)
            {
                var showPostfix = typeof(CardRewardHook).GetMethod(
                    nameof(OnShowScreen),
                    BindingFlags.Static | BindingFlags.NonPublic);
                harmony.Patch(showMethod, postfix: new HarmonyMethod(showPostfix));
                _log.Info("[CardRewardHook] Patched NCardRewardSelectionScreen.ShowScreen");
            }
        }
        catch (Exception ex)
        {
            _log.Error($"[CardRewardHook] Register failed: {ex.Message}");
        }
    }

    // ── ShowScreen 的 Postfix：捕获候选牌列表 ─────────────────────────────

    /// <summary>
    /// NCardRewardSelectionScreen.ShowScreen(options, extraOptions) 调用后触发。
    /// 缓存候选牌列表以便 _Ready 中使用。
    /// </summary>
    [HarmonyPostfix]
    private static void OnShowScreen(IReadOnlyList<CardCreationResult> options)
    {
        _pendingOptions = options;
    }

    // ── _Ready 的 Postfix：界面就绪后触发顾问 ─────────────────────────────

    /// <summary>
    /// NCardRewardSelectionScreen._Ready() 之后执行。
    /// 此时界面完全初始化，可以安全读取 _options 字段。
    /// </summary>
    [HarmonyPostfix]
    private static void OnScreenReady(NCardRewardSelectionScreen __instance)
    {
        try
        {
            // 首次触发时注入 CanvasLayer
            OverlayHUD.EnsureInjected(__instance);

            // 方案 A：使用 ShowScreen 捕获的缓存（推荐）
            IReadOnlyList<CardCreationResult>? options = _pendingOptions;

            // 方案 B：Reflection 读取私有字段 _options（降级备案）
            if (options == null || options.Count == 0)
            {
                var field = typeof(NCardRewardSelectionScreen).GetField(
                    "_options",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                options = field?.GetValue(__instance) as IReadOnlyList<CardCreationResult>;
            }

            if (options == null || options.Count == 0)
            {
                _log.Warn("[CardRewardHook] No card options found in reward screen.");
                return;
            }

            List<string> candidateCardIds = options
                .Select(r => IdNormalizer.NormalizeModelId(
                    r.Card.IsUpgraded ? r.Card.Id.Entry + "+" : r.Card.Id.Entry))
                .ToList();

            _log.Info($"[CardRewardHook] Card reward screen opened: {string.Join(", ", candidateCardIds)}");

            RunSnapshot snapshot = RunStateReader.Capture();
            BuildPathManager.UpdateViability(snapshot);

            var envelope = AdvisorEngine.AnalyzeCardReward(candidateCardIds, snapshot);
            OverlayHUD.ShowCardRewardAdvice(envelope);

            ActiveSessions[__instance] = new CardRewardSession(snapshot, candidateCardIds, envelope);
            _pendingOptions = null; // 清除缓存
        }
        catch (Exception ex)
        {
            _log.Error($"[CardRewardHook] OnScreenReady failed: {ex.Message}");
        }
    }

    [HarmonyPostfix]
    private static void OnScreenExitTree(NCardRewardSelectionScreen __instance)
    {
        try
        {
            if (!ActiveSessions.TryGetValue(__instance, out var session))
                return;

            RunSnapshot exitSnapshot = RunStateReader.Capture();
            if (!exitSnapshot.IsValid)
                return;

            string choiceId = InferPlayerChoice(session, exitSnapshot);
            var record = DecisionRecordFactory.CreatePlayerChoiceRecord(
                session.Envelope,
                exitSnapshot,
                choiceId,
                source: "CardRewardHook.OnScreenExitTree",
                extraMetadata: new Dictionary<string, string>
                {
                    ["screen"] = "card-reward",
                    ["choiceInference"] = choiceId == "skip" ? "deck-diff-skip" : "deck-diff-added-card",
                });

            DecisionRecorder.Record(record);
            _log.Info($"[CardRewardHook] Player choice recorded. Trace: {session.Envelope.TraceId}, Choice: {choiceId}, Followed: {record.PlayerFollowedAdvice}");
        }
        catch (Exception ex)
        {
            _log.Error($"[CardRewardHook] OnScreenExitTree failed: {ex.Message}");
        }
        finally
        {
            ActiveSessions.Remove(__instance);
        }
    }

    private static string InferPlayerChoice(CardRewardSession session, RunSnapshot exitSnapshot)
    {
        var beforeCounts = BuildCounts(session.EntrySnapshot.DeckCardIds);
        var afterCounts = BuildCounts(exitSnapshot.DeckCardIds);

        foreach (var candidateCardId in session.CandidateCardIds)
        {
            if (GetCount(afterCounts, candidateCardId) > GetCount(beforeCounts, candidateCardId))
                return candidateCardId;
        }

        return "skip";
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

    private sealed class CardRewardSession
    {
        public CardRewardSession(
            RunSnapshot entrySnapshot,
            IReadOnlyList<string> candidateCardIds,
            AdviceEnvelope<CardRewardAdvice> envelope)
        {
            EntrySnapshot = entrySnapshot;
            CandidateCardIds = candidateCardIds;
            Envelope = envelope;
        }

        public RunSnapshot EntrySnapshot { get; }
        public IReadOnlyList<string> CandidateCardIds { get; }
        public AdviceEnvelope<CardRewardAdvice> Envelope { get; }
    }
}
