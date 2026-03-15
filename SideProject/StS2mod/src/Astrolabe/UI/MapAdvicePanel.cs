using Astrolabe.Engine;
using Godot;

namespace Astrolabe.UI;

/// <summary>
/// 地图路线建议面板，在地图界面右侧显示各方案的路线建议。
/// 
/// 布局：
///   右侧固定面板（屏幕右边缘）：
///   ┌──────────────────────────┐
///   │ [路线规划]               │
///   │ [方案A · 力量战]         │
///   │   精英→商店→精英         │
///   │   理由：需要关键遗物      │
///   │ [方案B · 无限流]         │
///   │   问号→篝火→商店         │
///   │   理由：删牌精简牌组      │
///   │ ⚠ 全局提示              │
///   └──────────────────────────┘
/// </summary>
public class MapAdvicePanel : Control
{
    private static readonly Color[] PathColors = {
        new(0.298f, 0.686f, 0.314f),  // 绿
        new(0.129f, 0.588f, 0.953f),  // 蓝
        new(1.000f, 0.757f, 0.027f),  // 黄
    };

    private VBoxContainer? _container;
    private Label?         _globalNoteLabel;

    public override void _Ready()
    {
        // 右侧固定定位
        Position = new Vector2(1580, 200);
        SetSize(new Vector2(320, 400));

        // 背景
        var bg = new StyleBoxFlat { BgColor = new Color(0f, 0f, 0f, 0.75f) };
        var panel = new Panel();
        panel.AddThemeStyleboxOverride("panel", bg);
        panel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(panel);

        // 标题
        var title = new Label
        {
            Text                = "★ 星象仪路线规划",
            Position            = new Vector2(12, 10),
            Size                = new Vector2(296, 24),
        };
        title.AddThemeColorOverride("font_color", new Color(1f, 1f, 0.4f));
        title.AddThemeFontSizeOverride("font_size", 14);
        AddChild(title);

        // 路线建议容器
        _container = new VBoxContainer
        {
            Position = new Vector2(12, 40),
            Size     = new Vector2(296, 320),
        };
        AddChild(_container);

        // 全局提示（如HP警告）
        _globalNoteLabel = new Label
        {
            Position     = new Vector2(12, 365),
            Size         = new Vector2(296, 28),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        _globalNoteLabel.AddThemeColorOverride("font_color", new Color(1f, 0.6f, 0.2f));
        _globalNoteLabel.AddThemeFontSizeOverride("font_size", 12);
        AddChild(_globalNoteLabel);
    }

    public void UpdateAdvice(MapAdvice advice)
    {
        if (_container == null) return;

        // 清空旧内容
        foreach (Node child in _container.GetChildren())
            child.QueueFree();

        // 为每个方案路线建议创建一个卡片
        for (int i = 0; i < advice.PathRoutes.Count; i++)
        {
            var route = advice.PathRoutes[i];
            var color = PathColors[i % PathColors.Length];
            var card  = CreateRouteCard(route, color);
            _container.AddChild(card);
        }

        // 全局提示
        if (_globalNoteLabel != null)
        {
            _globalNoteLabel.Text = advice.GlobalNote;
            _globalNoteLabel.Visible = !string.IsNullOrEmpty(advice.GlobalNote);
        }
    }

    private Control CreateRouteCard(PathRouteAdvice route, Color pathColor)
    {
        var card = new VBoxContainer();
        card.AddThemeConstantOverride("separation", 2);

        // 方案名称
        var nameLabel = new Label { Text = $"[{route.PathName}]" };
        nameLabel.AddThemeColorOverride("font_color", pathColor);
        nameLabel.AddThemeFontSizeOverride("font_size", 13);
        card.AddChild(nameLabel);

        // 路线建议文字
        var recLabel = new Label
        {
            Text         = route.Recommendation,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        recLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.85f));
        recLabel.AddThemeFontSizeOverride("font_size", 11);
        card.AddChild(recLabel);

        // 间隔
        var spacer = new Control { CustomMinimumSize = new Vector2(0, 8) };
        card.AddChild(spacer);

        return card;
    }
}

// ── 篝火建议面板 ────────────────────────────────────────────────────────

public class CampfireAdvicePanel : Control
{
    private Label? _actionLabel;
    private Label? _reasonLabel;
    private Label? _upgradeHintLabel;

    public override void _Ready()
    {
        // 屏幕右侧居中显示
        Position = new Vector2(1400, 350);
        SetSize(new Vector2(300, 130));

        var bg = new StyleBoxFlat { BgColor = new Color(0f, 0f, 0f, 0.8f) };
        var panel = new Panel();
        panel.AddThemeStyleboxOverride("panel", bg);
        panel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(panel);

        var title = new Label
        {
            Text     = "篝火建议",
            Position = new Vector2(12, 8),
            Size     = new Vector2(276, 22),
        };
        title.AddThemeColorOverride("font_color", new Color(1f, 0.8f, 0.2f));
        title.AddThemeFontSizeOverride("font_size", 14);
        AddChild(title);

        _actionLabel = new Label
        {
            Position = new Vector2(12, 34),
            Size     = new Vector2(276, 28),
        };
        _actionLabel.AddThemeFontSizeOverride("font_size", 16);
        AddChild(_actionLabel);

        _reasonLabel = new Label
        {
            Position     = new Vector2(12, 65),
            Size         = new Vector2(276, 40),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        _reasonLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.85f));
        _reasonLabel.AddThemeFontSizeOverride("font_size", 12);
        AddChild(_reasonLabel);

        _upgradeHintLabel = new Label
        {
            Position = new Vector2(12, 108),
            Size     = new Vector2(276, 18),
        };
        _upgradeHintLabel.AddThemeColorOverride("font_color", new Color(0.6f, 1f, 0.6f));
        _upgradeHintLabel.AddThemeFontSizeOverride("font_size", 11);
        AddChild(_upgradeHintLabel);
    }

    public void UpdateAdvice(CampfireAdvice advice)
    {
        if (_actionLabel == null) return;

        bool isUpgrade = advice.RecommendedAction == CampfireAction.Upgrade;

        _actionLabel.Text = advice.RecommendedAction switch
        {
            CampfireAction.Upgrade => "★ 推荐：升级",
            CampfireAction.Rest    => "★ 推荐：休息",
            CampfireAction.Smith   => "★ 推荐：锻造",
            CampfireAction.Recall  => "○ 推荐：回忆",
            _                     => "○ 无建议",
        };
        _actionLabel.AddThemeColorOverride("font_color",
            isUpgrade ? new Color(0.4f, 0.8f, 1f) : new Color(0.3f, 0.8f, 0.3f));

        if (_reasonLabel != null)
            _reasonLabel.Text = advice.Reason;

        if (_upgradeHintLabel != null && advice.UpgradeTargetCardId != null)
        {
            _upgradeHintLabel.Text    = $"→ 优先升级：{advice.UpgradeTargetCardId}";
            _upgradeHintLabel.Visible = true;
        }
        else if (_upgradeHintLabel != null)
        {
            _upgradeHintLabel.Visible = false;
        }
    }
}
