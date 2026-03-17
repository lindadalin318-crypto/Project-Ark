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

            var advice = AdvisorEngine.AnalyzeCardReward(candidateCardIds, snapshot);
            OverlayHUD.ShowCardRewardAdvice(advice);

            _pendingOptions = null; // 清除缓存
        }
        catch (Exception ex)
        {
            _log.Error($"[CardRewardHook] OnScreenReady failed: {ex.Message}");
        }
    }
}
