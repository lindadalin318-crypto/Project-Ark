using System.Reflection;
using Astrolabe.Core;
using Astrolabe.Engine;
using Astrolabe.UI;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;

namespace Astrolabe.Hooks;

/// <summary>
/// Hook 篝火（休息地点）界面，触发篝火决策建议并更新 HUD，
/// 并在玩家实际选择篝火动作时按同一 trace 记录选择结果。
/// </summary>
public static class CampfireHook
{
    private static readonly Logger _log = new("Astrolabe.CampfireHook", LogType.Generic);
    private static readonly Dictionary<NRestSiteRoom, CampfireSession> ActiveSessions = new();

    public static void Register(Harmony harmony)
    {
        try
        {
            var roomType = typeof(NRestSiteRoom);
            var readyMethod = roomType.GetMethod("_Ready",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var exitTreeMethod = roomType.GetMethod("_ExitTree",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var afterSelectingMethod = roomType.GetMethod("AfterSelectingOption",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (readyMethod != null)
            {
                var postfixMethod = typeof(CampfireHook).GetMethod(
                    nameof(OnRestSiteReady),
                    BindingFlags.Static | BindingFlags.NonPublic);
                harmony.Patch(readyMethod, postfix: new HarmonyMethod(postfixMethod));
                _log.Info("[CampfireHook] Patched NRestSiteRoom._Ready");
            }
            else
            {
                _log.Error("[CampfireHook] Cannot find _Ready on NRestSiteRoom");
            }

            if (afterSelectingMethod != null)
            {
                var selectionPostfix = typeof(CampfireHook).GetMethod(
                    nameof(OnAfterSelectingOption),
                    BindingFlags.Static | BindingFlags.NonPublic);
                harmony.Patch(afterSelectingMethod, postfix: new HarmonyMethod(selectionPostfix));
                _log.Info("[CampfireHook] Patched NRestSiteRoom.AfterSelectingOption");
            }
            else
            {
                _log.Error("[CampfireHook] Cannot find AfterSelectingOption on NRestSiteRoom");
            }

            if (exitTreeMethod != null)
            {
                var exitPostfix = typeof(CampfireHook).GetMethod(
                    nameof(OnRestSiteExitTree),
                    BindingFlags.Static | BindingFlags.NonPublic);
                harmony.Patch(exitTreeMethod, postfix: new HarmonyMethod(exitPostfix));
                _log.Info("[CampfireHook] Patched NRestSiteRoom._ExitTree");
            }
        }
        catch (Exception ex)
        {
            _log.Error($"[CampfireHook] Register failed: {ex.Message}");
        }
    }

    [HarmonyPostfix]
    private static void OnRestSiteReady(NRestSiteRoom __instance)
    {
        try
        {
            _log.Info("[CampfireHook] Rest site room ready.");

            RunSnapshot snapshot = RunStateReader.Capture();
            if (!snapshot.IsValid)
            {
                ActiveSessions.Remove(__instance);
                DeckUpgradeHook.ClearCampfireContext();
                _log.Warn("[CampfireHook] Invalid snapshot, skipping advice.");
                return;
            }

            OverlayHUD.EnsureInjected(__instance);

            var availableOptionIds = CollectAvailableOptionIds(__instance);
            BuildPathManager.UpdateViability(snapshot);
            var envelope = AdvisorEngine.AnalyzeCampfire(snapshot, availableOptionIds);
            var advice = envelope.Payload;

            if (advice.RecommendedAction is CampfireAction.Upgrade or CampfireAction.Smith)
                DeckUpgradeHook.CacheCampfireAdvice(envelope);
            else
                DeckUpgradeHook.ClearCampfireContext();

            ActiveSessions[__instance] = new CampfireSession(snapshot, envelope);
            OverlayHUD.ShowCampfireAdvice(envelope);

            _log.Info($"[CampfireHook] Campfire advice: {advice.RecommendedAction} ({advice.Reason}) / Trace: {envelope.TraceId} / Options: {string.Join(",", availableOptionIds)}");
        }
        catch (Exception ex)
        {
            _log.Error($"[CampfireHook] OnRestSiteReady failed: {ex.Message}");
        }
    }

    [HarmonyPostfix]
    private static void OnAfterSelectingOption(NRestSiteRoom __instance, RestSiteOption option)
    {
        try
        {
            if (!ActiveSessions.TryGetValue(__instance, out var session) || session.HasRecordedChoice)
                return;

            string choiceId = option.OptionId?.Trim().ToUpperInvariant() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(choiceId))
                return;

            RunSnapshot snapshot = RunStateReader.Capture();
            if (!snapshot.IsValid)
                snapshot = session.EntrySnapshot;

            var record = DecisionRecordFactory.CreatePlayerChoiceRecord(
                session.Envelope,
                snapshot,
                choiceId,
                source: "CampfireHook.OnAfterSelectingOption",
                extraMetadata: new Dictionary<string, string>
                {
                    ["screen"] = "campfire",
                    ["choiceInference"] = "selected-option",
                });

            DecisionRecorder.Record(record);
            session.HasRecordedChoice = true;

            if (!string.Equals(choiceId, "SMITH", StringComparison.OrdinalIgnoreCase))
                DeckUpgradeHook.ClearCampfireContext();

            _log.Info($"[CampfireHook] Player campfire choice recorded. Trace: {session.Envelope.TraceId}, Choice: {choiceId}, Followed: {record.PlayerFollowedAdvice}");
        }
        catch (Exception ex)
        {
            _log.Error($"[CampfireHook] OnAfterSelectingOption failed: {ex.Message}");
        }
    }

    [HarmonyPostfix]
    private static void OnRestSiteExitTree(NRestSiteRoom __instance)
    {
        ActiveSessions.Remove(__instance);
    }

    private static IReadOnlyList<string> CollectAvailableOptionIds(NRestSiteRoom room)
    {
        var available = new List<string>();
        foreach (var option in room.Options)
        {
            if (option == null || !option.IsEnabled || string.IsNullOrWhiteSpace(option.OptionId))
                continue;

            available.Add(option.OptionId.Trim().ToUpperInvariant());
        }

        return available;
    }

    private sealed class CampfireSession
    {
        public CampfireSession(RunSnapshot entrySnapshot, AdviceEnvelope<CampfireAdvice> envelope)
        {
            EntrySnapshot = entrySnapshot;
            Envelope = envelope;
        }

        public RunSnapshot EntrySnapshot { get; }
        public AdviceEnvelope<CampfireAdvice> Envelope { get; }
        public bool HasRecordedChoice { get; set; }
    }
}
