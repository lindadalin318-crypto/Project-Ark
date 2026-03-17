using Astrolabe.Engine;
using Godot;

namespace Astrolabe.UI;

/// <summary>
/// 常驻方案卡片面板，显示在屏幕顶部。
/// 实时展示所有活跃方案的可行性进度条和趋势箭头。
/// 
/// 布局（1920x1080 基准）：
///   ┌─────────────────────────────────────────────────────┐
///   │ [方案A · 力量战]  ████████░░ 80% ▲                  │
///   │ [方案B · 无限流]  █████░░░░░ 48% →                  │
///   │ [方案C · 防御反伤] ██░░░░░░░ 22% ▼  (淡色)          │
///   └─────────────────────────────────────────────────────┘
/// </summary>
public class BuildPathPanel : Control
{
    // 三套方案的颜色（A=绿, B=蓝, C=黄）
    private static readonly Color[] PathColors = {
        new(0.298f, 0.686f, 0.314f),  // #4CAF50 绿
        new(0.129f, 0.588f, 0.953f),  // #2196F3 蓝
        new(1.000f, 0.757f, 0.027f),  // #FFC107 黄
    };
    private static readonly Color FadingColor  = new(0.620f, 0.620f, 0.620f, 0.5f); // 淡出色
    private static readonly Color TextColor    = new(1f, 1f, 1f, 0.95f);
    private static readonly Color BgColor      = new(0f, 0f, 0f, 0.7f);

    // 面板容器
    private VBoxContainer? _container;

    // 各方案行的引用（最多3行）
    private readonly PathRow[] _rows = new PathRow[3];

    public BuildPathPanel()
    {
        // 面板定位：左上角，避开 HP栏 + 遗物栏（约 140px）
        SetPosition(new Vector2(8, 148));
        SetSize(new Vector2(200, 90));

        // 背景面板
        var bg = new ColorRect
        {
            Color         = BgColor,
            AnchorLeft    = 0,
            AnchorTop     = 0,
            AnchorRight   = 1,
            AnchorBottom  = 1,
            MouseFilter   = MouseFilterEnum.Ignore,
        };
        AddChild(bg);

        // 方案行容器
        _container = new VBoxContainer();
        _container.SetPosition(new Vector2(6, 6));
        _container.SetSize(new Vector2(188, 78));
        AddChild(_container);

        // 创建3行方案显示
        for (int i = 0; i < 3; i++)
        {
            _rows[i] = new PathRow();
            _container.AddChild(_rows[i]);
            _rows[i].Hide(); // 初始隐藏
        }
    }

    /// <summary>更新方案显示（每次界面变化时调用）</summary>
    public void UpdatePaths(IReadOnlyList<PathState> activePaths)
    {
        // 隐藏所有行
        foreach (var row in _rows)
            row.Hide();

        // 重新填充
        for (int i = 0; i < Math.Min(activePaths.Count, 3); i++)
        {
            var path = activePaths[i];
            var color = path.IsFading ? FadingColor : PathColors[i % PathColors.Length];
            _rows[i].SetData(path, color);
            _rows[i].Show();
        }

        // 根据活跃方案数调整面板高度
        SetSize(new Vector2(200, 10 + Math.Min(activePaths.Count, 3) * 28));
    }
}

// ── 单行方案显示组件 ────────────────────────────────────────────────────

public class PathRow : HBoxContainer
{
    private Label?         _nameLabel;
    private ProgressBar?   _progressBar;
    private Label?         _percentLabel;
    private Label?         _trendLabel;

    public PathRow()
    {
        SetCustomMinimumSize(new Vector2(0, 28));

        _nameLabel = new Label
        {
            CustomMinimumSize = new Vector2(120, 0),
            VerticalAlignment = VerticalAlignment.Center,
        };
        _nameLabel.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f, 0.95f));
        _nameLabel.AddThemeFontSizeOverride("font_size", 13);
        AddChild(_nameLabel);

        _progressBar = new ProgressBar
        {
            CustomMinimumSize   = new Vector2(100, 16),
            MaxValue            = 100,
            Value               = 0,
            ShowPercentage      = false,
            SizeFlagsHorizontal = SizeFlags.Expand,
        };
        AddChild(_progressBar);

        _percentLabel = new Label
        {
            CustomMinimumSize   = new Vector2(40, 0),
            VerticalAlignment   = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        _percentLabel.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f, 0.8f));
        _percentLabel.AddThemeFontSizeOverride("font_size", 12);
        AddChild(_percentLabel);

        _trendLabel = new Label
        {
            CustomMinimumSize = new Vector2(20, 0),
            VerticalAlignment = VerticalAlignment.Center,
        };
        _trendLabel.AddThemeFontSizeOverride("font_size", 13);
        AddChild(_trendLabel);
    }

    public void SetData(PathState state, Color color)
    {
        if (_nameLabel != null)
        {
            _nameLabel.Text = state.NameZh;
            _nameLabel.AddThemeColorOverride("font_color",
                state.IsFading ? new Color(0.6f, 0.6f, 0.6f, 0.5f) : new Color(1f, 1f, 1f, 0.95f));
        }

        if (_progressBar != null)
        {
            _progressBar.Value = state.ViabilityPercent;
            // 通过修改 StyleBox 来设置进度条颜色
            var fillStyle = new StyleBoxFlat { BgColor = color };
            _progressBar.AddThemeStyleboxOverride("fill", fillStyle);
        }

        if (_percentLabel != null)
            _percentLabel.Text = $"{state.ViabilityPercent:F0}%";

        if (_trendLabel != null)
        {
            _trendLabel.Text = state.Trend switch
            {
                ViabilityTrend.Rising  => "▲",
                ViabilityTrend.Falling => "▼",
                _                     => "→",
            };
            _trendLabel.AddThemeColorOverride("font_color", state.Trend switch
            {
                ViabilityTrend.Rising  => new Color(0.298f, 0.686f, 0.314f),
                ViabilityTrend.Falling => new Color(0.957f, 0.263f, 0.212f),
                _                     => new Color(0.8f, 0.8f, 0.8f),
            });
        }
    }
}
