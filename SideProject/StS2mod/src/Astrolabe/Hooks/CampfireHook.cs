using System.Reflection;
using Astrolabe.Core;
using Astrolabe.Engine;
using Astrolabe.UI;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;

namespace Astrolabe.Hooks;

/// <summary>
/// Hook 篝火（休息地点）界面，触发篝火决策建议并更新 HUD。
///
/// 已通过 ILSpy 确认：
///   - 节点类名：MegaCrit.Sts2.Core.Nodes.Rooms.NRestSiteRoom
///   - 入口方法：_Ready()（Godot Node 生命周期）
///   - 选项列表：Options 属性（IReadOnlyList&lt;RestSiteOption&gt;）
///   - 访问方式：NRestSiteRoom.Instance → NRun.Instance?.RestSiteRoom
///
/// 从 AutoSlay 的 RestSiteRoomHandler 确认的路径：
///   /root/Game/RootSceneContainer/Run/RoomContainer/RestSiteRoom
/// </summary>
public static class CampfireHook
{
    private static readonly Logger _log = new("Astrolabe.CampfireHook", LogType.Generic);

    public static void Register(Harmony harmony)
    {
        try
        {
            var roomType = typeof(NRestSiteRoom);
            var readyMethod = roomType.GetMethod("_Ready",
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
                _log.Warn("[CampfireHook] Invalid snapshot, skipping advice.");
                return;
            }

            BuildPathManager.UpdateViability(snapshot);
            var advice = AdvisorEngine.AnalyzeCampfire(snapshot);
            OverlayHUD.ShowCampfireAdvice(advice);

            _log.Info($"[CampfireHook] Campfire advice: {advice.RecommendedAction} ({advice.Reason})");
        }
        catch (Exception ex)
        {
            _log.Error($"[CampfireHook] OnRestSiteReady failed: {ex.Message}");
        }
    }
}
