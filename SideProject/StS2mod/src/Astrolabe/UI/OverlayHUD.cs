using Astrolabe.Engine;
using Godot;
using HarmonyLib;
using MegaCritLogger = MegaCrit.Sts2.Core.Logging.Logger;
using LogType = MegaCrit.Sts2.Core.Logging.LogType;

namespace Astrolabe.UI;

/// <summary>
/// Overlay HUD 根节点管理器。
/// 负责在游戏根节点上注入 CanvasLayer，并协调各子面板的显示/隐藏。
/// 
/// 架构：
///   Game Root Node
///     └── AstrolabeCanvasLayer (Layer=100, 始终置顶)
///           ├── BuildPathPanel      常驻方案卡片
///           ├── CardAdvicePanel     选牌界面标签
///           ├── MapAdvicePanel      地图路线 HUD
///           └── CampfireAdvicePanel 篝火建议
/// 
/// ⚠️ 游戏根节点的访问方式需反编译 sts2.dll 后确认。
/// </summary>
public static class OverlayHUD
{
    private static readonly MegaCritLogger _log = new("Astrolabe.OverlayHUD", LogType.Generic);

    // Godot CanvasLayer 实例（程序化创建，挂在游戏场景树上）
    private static CanvasLayer? _canvasLayer;

    // 子面板
    private static BuildPathPanel?      _buildPathPanel;
    private static CardAdvicePanel?     _cardAdvicePanel;
    private static MapAdvicePanel?      _mapAdvicePanel;
    private static CampfireAdvicePanel? _campfirePanel;

    // HUD 全局开关
    private static bool _isVisible = true;

    // ── 初始化 ───────────────────────────────────────────────────────

    public static void Initialize()
    {
        // 通过 Harmony Hook 游戏根节点的 _Ready 方法来注入 CanvasLayer
        // TODO: 确认游戏根节点的类名（预估：MainScene / GameRoot / NGameRoot）
        //
        // 注意：这里不能在 Initialize 阶段直接创建 Godot Node，
        // 因为 Godot 引擎此时可能还未完成场景树初始化。
        // 正确的时机是在游戏根节点 _Ready() Postfix 中创建。
        _log.Info("[OverlayHUD] Initialized. Canvas layer will be injected when root scene is ready.");
    }

    /// <summary>
    /// 在游戏根节点 _Ready() 后调用此方法创建并挂载 CanvasLayer。
    /// 应在 Harmony Postfix 中调用。
    /// </summary>
    public static void InjectCanvasLayer(Node gameRootNode)
    {
        if (_canvasLayer != null)
        {
            _log.Warn("[OverlayHUD] CanvasLayer already exists, skipping injection.");
            return;
        }

        try
        {
            // 创建 CanvasLayer（始终置顶，覆盖游戏所有 UI）
            _canvasLayer = new CanvasLayer
            {
                Name  = "AstrolabeHUD",
                Layer = 100,  // 高层级确保显示在游戏UI上方
            };
            gameRootNode.AddChild(_canvasLayer);

            // 创建各子面板
            _buildPathPanel  = new BuildPathPanel();
            _cardAdvicePanel = new CardAdvicePanel();
            _mapAdvicePanel  = new MapAdvicePanel();
            _campfirePanel   = new CampfireAdvicePanel();

            _canvasLayer.AddChild(_buildPathPanel);
            _canvasLayer.AddChild(_cardAdvicePanel);
            _canvasLayer.AddChild(_mapAdvicePanel);
            _canvasLayer.AddChild(_campfirePanel);

            // 初始状态：仅方案卡片常驻，其他隐藏
            _buildPathPanel.Show();
            _cardAdvicePanel.Hide();
            _mapAdvicePanel.Hide();
            _campfirePanel.Hide();

            _log.Info("[OverlayHUD] CanvasLayer injected successfully into game root.");
        }
        catch (Exception ex)
        {
            _log.Error($"[OverlayHUD] Failed to inject CanvasLayer: {ex.Message}");
        }
    }

    // ── 各界面的 HUD 刷新接口 ────────────────────────────────────────

    public static void ShowCardRewardAdvice(CardRewardAdvice advice)
    {
        if (!_isVisible || _cardAdvicePanel == null) return;

        _buildPathPanel?.UpdatePaths(advice.ActivePaths);
        _cardAdvicePanel.UpdateAdvice(advice);
        _cardAdvicePanel.Show();
        _mapAdvicePanel?.Hide();
        _campfirePanel?.Hide();

        _log.Info($"[OverlayHUD] Card reward advice shown. Skip: {advice.ShouldSkip}");
    }

    public static void ShowMapAdvice(MapAdvice advice)
    {
        if (!_isVisible || _mapAdvicePanel == null) return;

        _mapAdvicePanel.UpdateAdvice(advice);
        _mapAdvicePanel.Show();
        _cardAdvicePanel?.Hide();
        _campfirePanel?.Hide();
    }

    public static void ShowCampfireAdvice(CampfireAdvice advice)
    {
        if (!_isVisible || _campfirePanel == null) return;

        _campfirePanel.UpdateAdvice(advice);
        _campfirePanel.Show();
        _cardAdvicePanel?.Hide();
        _mapAdvicePanel?.Hide();
    }

    public static void ShowShopAdvice(ShopAdvice advice)
    {
        // TODO: 商店 HUD 面板（Phase 2 实现）
        _log.Info($"[OverlayHUD] Shop advice: {advice.PurchasePriority.Count} items evaluated.");
    }

    /// <summary>
    /// 切换 HUD 全局可见性（快捷键 Tab 调用）。
    /// </summary>
    public static void ToggleVisibility()
    {
        _isVisible = !_isVisible;
        _canvasLayer?.SetVisible(_isVisible);
        _log.Info($"[OverlayHUD] Visibility toggled: {_isVisible}");
    }
}
