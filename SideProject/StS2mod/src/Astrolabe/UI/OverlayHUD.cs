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
    private static CombatAdvicePanel?   _combatPanel;

    // HUD 全局开关
    private static bool _isVisible = true;

    // ── 初始化 ───────────────────────────────────────────────────────

    public static void Initialize()
    {
        _log.Info("[OverlayHUD] Initialized. Canvas layer will be injected when root scene is ready.");
    }

    /// <summary>
    /// 从任意场景树中的 Node 向上找到根节点，确保 CanvasLayer 只注入一次。
    /// 在任意 Hook 的 Postfix 中调用（需要 __instance 是有效的场景树节点）。
    /// </summary>
    public static void EnsureInjected(Node anySceneNode)
    {
        if (_canvasLayer != null) return;  // 已注入

        try
        {
            // 向上遍历到根节点（SceneTree.Root）
            var root = anySceneNode.GetTree()?.Root;
            if (root == null)
            {
                _log.Warn("[OverlayHUD] Cannot get scene tree root.");
                return;
            }
            InjectCanvasLayer(root);
        }
        catch (Exception ex)
        {
            _log.Error($"[OverlayHUD] EnsureInjected failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 创建并挂载 CanvasLayer 到指定父节点。
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
            _combatPanel     = new CombatAdvicePanel();

            _canvasLayer.AddChild(_buildPathPanel);
            _canvasLayer.AddChild(_cardAdvicePanel);
            _canvasLayer.AddChild(_mapAdvicePanel);
            _canvasLayer.AddChild(_campfirePanel);
            _canvasLayer.AddChild(_combatPanel);

            // 初始状态：仅方案卡片常驻，其他隐藏
            _buildPathPanel.Show();
            _cardAdvicePanel.Hide();
            _mapAdvicePanel.Hide();
            _campfirePanel.Hide();
            _combatPanel.Hide();

            // 立即用当前方案数据刷新 BuildPathPanel
            var paths = BuildPathManager.GetActivePaths();
            if (paths.Count > 0)
                _buildPathPanel.UpdatePaths(paths);

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

    public static void ShowCombatAdvice(CombatAdvice advice)
    {
        if (!_isVisible || _combatPanel == null) return;

        _combatPanel.UpdateAdvice(advice);
        _combatPanel.Show();
        _log.Info($"[OverlayHUD] Combat advice shown: {advice.SummaryText}");
    }

    public static void HideCombatAdvice()
    {
        _combatPanel?.Hide();
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
