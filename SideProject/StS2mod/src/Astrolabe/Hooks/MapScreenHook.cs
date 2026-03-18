using System.Reflection;
using Astrolabe.Core;
using Astrolabe.Engine;
using Astrolabe.UI;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;

namespace Astrolabe.Hooks;

/// <summary>
/// Hook 地图界面（NMapScreen），触发路线规划建议并更新 HUD。
///
/// 已通过 ILSpy 确认：
///   - 类名：MegaCrit.Sts2.Core.Nodes.Screens.Map.NMapScreen
///   - 在 AutoSlay 的 MapScreenHandler 中可见访问模式：
///     RunManager.Instance.DebugOnlyGetState() → RunState.VisitedMapCoords
///   - 地图界面本身通过 /root/Game/RootSceneContainer/Run 路径访问
/// 
/// Hook 策略：
///   - Postfix NMapScreen._Ready() 以便在地图完全加载后触发建议
///   - 同时订阅 RunManager.RoomEntered 事件（在 _Ready 中注册）
/// </summary>
public static class MapScreenHook
{
    private static readonly Logger _log = new("Astrolabe.MapScreenHook", LogType.Generic);

    public static void Register(Harmony harmony)
    {
        try
        {
            var screenType = typeof(NMapScreen);
            var readyMethod = screenType.GetMethod("_Ready",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (readyMethod != null)
            {
                var postfixMethod = typeof(MapScreenHook).GetMethod(
                    nameof(OnMapScreenReady),
                    BindingFlags.Static | BindingFlags.NonPublic);
                harmony.Patch(readyMethod, postfix: new HarmonyMethod(postfixMethod));
                _log.Info("[MapScreenHook] Patched NMapScreen._Ready");
            }
            else
            {
                _log.Error("[MapScreenHook] Cannot find _Ready on NMapScreen");
            }
        }
        catch (Exception ex)
        {
            _log.Error($"[MapScreenHook] Register failed: {ex.Message}");
        }
    }

    [HarmonyPostfix]
    private static void OnMapScreenReady(NMapScreen __instance)
    {
        try
        {
            _log.Info("[MapScreenHook] Map screen opened.");

            RunSnapshot snapshot = RunStateReader.Capture();
            if (!snapshot.IsValid)
            {
                _log.Warn("[MapScreenHook] Invalid snapshot, skipping advice.");
                return;
            }

            BuildPathManager.UpdateViability(snapshot);
            var envelope = AdvisorEngine.AnalyzeMapRoutes(snapshot);

            // 注入 CanvasLayer（首次触发时执行，之后幂等）
            // 必须在 ShowMapAdvice 之前注入，否则面板引用为 null
            OverlayHUD.EnsureInjected(__instance);
            OverlayHUD.ShowMapAdvice(envelope);

            _log.Info($"[MapScreenHook] Map advice generated. Floor: {snapshot.Floor}, Act: {snapshot.Act}");
        }
        catch (Exception ex)
        {
            _log.Error($"[MapScreenHook] OnMapScreenReady failed: {ex.Message}");
        }
    }
}
