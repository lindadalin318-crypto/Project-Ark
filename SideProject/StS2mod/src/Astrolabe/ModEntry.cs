using Astrolabe.Data;
using Astrolabe.Engine;
using Astrolabe.Hooks;
using Astrolabe.UI;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCritLogger = MegaCrit.Sts2.Core.Logging.Logger;
using LogType = MegaCrit.Sts2.Core.Logging.LogType;

namespace Astrolabe;

/// <summary>
/// Mod 入口点。游戏 modding loader 通过 [ModInitializer] attribute 找到此类并调用 Initialize()。
/// 必须继承 Godot.Node（partial class），才能获得场景树上下文，支持 CanvasLayer 注入。
/// </summary>
[ModInitializer(nameof(Initialize))]
public partial class ModEntry : Node
{
    public const string MOD_ID = "Astrolabe";

    public static MegaCritLogger Logger { get; } = new(MOD_ID, LogType.Generic);

    public static void Initialize()
    {
        Logger.Info("=== Astrolabe v0.1.0 initializing ===");

        // 1. 加载数据库（cards / relics / buildpaths / bosses / events）
        try
        {
            DataLoader.LoadAll();
            Logger.Info($"[Astrolabe] Data loaded: {DataLoader.Cards.Count} cards, {DataLoader.BuildPaths.Count} build paths");
        }
        catch (Exception ex)
        {
            Logger.Error($"[Astrolabe] Failed to load data: {ex.Message}");
        }

        // 2. 初始化多方案引擎
        BuildPathManager.Initialize();

        // 3. 注册所有 Harmony Hook（分为手动注册和自动扫描两类）
        var harmony = new Harmony(MOD_ID);

        CardRewardHook.Register(harmony);
        MapScreenHook.Register(harmony);
        CampfireHook.Register(harmony);
        ShopHook.Register(harmony);

        // 自动扫描：扫描程序集内所有标记了 [HarmonyPatch] 的类
        harmony.PatchAll();
        Logger.Info("[Astrolabe] Harmony patches applied.");

        // 4. 初始化 HUD 层（在游戏场景树就绪后注入 CanvasLayer）
        OverlayHUD.Initialize();

        Logger.Info("=== Astrolabe initialized successfully ===");
    }
}
