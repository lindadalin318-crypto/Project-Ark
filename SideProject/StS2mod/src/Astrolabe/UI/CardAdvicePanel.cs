using Astrolabe.Engine;
using Godot;

namespace Astrolabe.UI;

/// <summary>
/// 选牌建议面板，在卡牌奖励界面叠加显示。
/// 
/// 显示内容：
///   1. 每张候选牌上方叠加评级标签（颜色+图标）
///   2. 底部显示顾问说明区域（各方案的简要理由）
///   3. 如果建议跳过，显示"建议跳过"提示
/// </summary>
public class CardAdvicePanel : Control
{
    private static readonly Color CorePickColor    = new(0.298f, 0.686f, 0.314f); // 绿
    private static readonly Color GoodPickColor    = new(0.400f, 0.800f, 1.000f); // 浅蓝
    private static readonly Color SituationalColor = new(1.000f, 0.757f, 0.027f); // 黄
    private static readonly Color WeakColor        = new(0.620f, 0.620f, 0.620f); // 灰
    private static readonly Color SkipColor        = new(0.957f, 0.263f, 0.212f); // 红

    // 底部顾问说明区域
    private Panel?     _advicePanel;
    private Label?     _adviceTitle;
    private Label?     _adviceBody;
    private Label?     _skipLabel;

    // 标签悬浮提示（每张卡一个，位置需要和游戏卡牌坐标对齐）
    private readonly List<CardRatingBadge> _badges = new();

    public override void _Ready()
    {
        // 面板覆盖整个屏幕（坐标 0,0）
        SetPosition(Vector2.Zero);
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore; // 不拦截鼠标事件

        BuildAdviceBottomPanel();
    }

    private void BuildAdviceBottomPanel()
    {
        // 底部顾问说明区（屏幕下方）
        _advicePanel = new Panel
        {
            Position = new Vector2(400, 850),
            Size     = new Vector2(1120, 120),
        };
        var panelBg = new StyleBoxFlat { BgColor = new Color(0f, 0f, 0f, 0.8f) };
        _advicePanel.AddThemeStyleboxOverride("panel", panelBg);
        AddChild(_advicePanel);

        _adviceTitle = new Label
        {
            Position = new Vector2(16, 10),
            Size     = new Vector2(1088, 24),
        };
        _adviceTitle.AddThemeColorOverride("font_color", new Color(1f, 1f, 0f, 1f));
        _adviceTitle.AddThemeFontSizeOverride("font_size", 15);
        _advicePanel.AddChild(_adviceTitle);

        _adviceBody = new Label
        {
            Position   = new Vector2(16, 38),
            Size       = new Vector2(1088, 70),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        _adviceBody.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.9f, 1f));
        _adviceBody.AddThemeFontSizeOverride("font_size", 13);
        _advicePanel.AddChild(_adviceBody);

        // 跳过提示标签（正中显示）
        _skipLabel = new Label
        {
            Position            = new Vector2(760, 810),
            Size                = new Vector2(400, 36),
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        _skipLabel.AddThemeColorOverride("font_color", SkipColor);
        _skipLabel.AddThemeFontSizeOverride("font_size", 18);
        _skipLabel.Hide();
        AddChild(_skipLabel);
    }

    /// <summary>
    /// 更新显示内容（由 OverlayHUD 在拦截到选牌界面后调用）。
    /// </summary>
    public void UpdateAdvice(CardRewardAdvice advice)
    {
        // 清除旧标签
        foreach (var badge in _badges)
            badge.QueueFree();
        _badges.Clear();

        if (advice.CardAdvices.Count == 0) return;

        // 在每张候选牌的大概位置创建评级徽章
        // 三张牌的X坐标（1920宽，三张牌居中）—— 需要根据实际游戏布局校准
        float[] cardCenterXs = { 560f, 960f, 1360f };
        float cardY = 480f; // 卡牌Y中心位置（估算）

        for (int i = 0; i < Math.Min(advice.CardAdvices.Count, 3); i++)
        {
            var cardAdvice = advice.CardAdvices[i];
            var badge = CreateBadge(cardAdvice, cardCenterXs[i], cardY);
            _badges.Add(badge);
            AddChild(badge);
        }

        // 更新底部说明文字
        UpdateBottomPanel(advice);

        // 跳过提示
        if (advice.ShouldSkip && _skipLabel != null)
        {
            _skipLabel.Text = $"⚠ {advice.SkipNote}";
            _skipLabel.Show();
        }
        else
        {
            _skipLabel?.Hide();
        }
    }

    private CardRatingBadge CreateBadge(CardAdvice cardAdvice, float centerX, float centerY)
    {
        var badge = new CardRatingBadge();
        badge.SetRating(cardAdvice.OverallRating, cardAdvice.CardNameZh);

        // 将徽章放在卡牌顶部中心
        badge.Position = new Vector2(centerX - 50f, centerY - 220f);
        return badge;
    }

    private void UpdateBottomPanel(CardRewardAdvice advice)
    {
        if (_advicePanel == null) return;

        // 找出推荐评级最高的牌
        var bestCard = advice.CardAdvices
            .OrderBy(a => a.OverallRating) // 枚举值越小越好（CorePick=0）
            .FirstOrDefault();

        if (bestCard == null) return;

        // 标题：指出推荐哪张牌
        if (_adviceTitle != null)
        {
            _adviceTitle.Text = bestCard.OverallRating switch
            {
                CardRating.CorePick    => $"★★★ 推荐选「{bestCard.CardNameZh}」",
                CardRating.GoodPick    => $"★★ 推荐选「{bestCard.CardNameZh}」",
                CardRating.Situational => $"★ 可选「{bestCard.CardNameZh}」（视情况）",
                _                     => "✗ 三张牌均不适合当前方案",
            };
        }

        // 正文：各方案的详细说明
        if (_adviceBody != null)
        {
            var lines = new List<string>();
            foreach (var (pathId, pathRating) in bestCard.PathRatings)
            {
                lines.Add($"[{pathRating.PathNameZh}] {RatingToIcon(pathRating.Rating)} {pathRating.Reason}");
            }
            _adviceBody.Text = string.Join("\n", lines);
        }
    }

    private static string RatingToIcon(CardRating rating) => rating switch
    {
        CardRating.CorePick    => "★★★",
        CardRating.GoodPick    => "★★",
        CardRating.Situational => "★",
        CardRating.Weak        => "○",
        CardRating.Skip        => "✗",
        _                      => "—",
    };

    private Color RatingToColor(CardRating rating) => rating switch
    {
        CardRating.CorePick    => CorePickColor,
        CardRating.GoodPick    => GoodPickColor,
        CardRating.Situational => SituationalColor,
        CardRating.Weak        => WeakColor,
        CardRating.Skip        => SkipColor,
        _                      => WeakColor,
    };
}

// ── 卡牌评级徽章组件 ────────────────────────────────────────────────────

/// <summary>
/// 卡牌上方叠加的评级徽章（颜色框 + 星级图标）。
/// </summary>
public class CardRatingBadge : Control
{
    private static readonly Color CorePickBg  = new(0.298f, 0.686f, 0.314f, 0.85f);
    private static readonly Color GoodPickBg  = new(0.129f, 0.588f, 0.953f, 0.85f);
    private static readonly Color SituBg      = new(1.000f, 0.757f, 0.027f, 0.85f);
    private static readonly Color WeakBg      = new(0.400f, 0.400f, 0.400f, 0.70f);
    private static readonly Color SkipBg      = new(0.957f, 0.263f, 0.212f, 0.85f);

    private ColorRect? _bg;
    private Label?     _iconLabel;
    private Label?     _textLabel;

    public override void _Ready()
    {
        SetSize(new Vector2(100, 36));
        MouseFilter = MouseFilterEnum.Ignore;

        _bg = new ColorRect
        {
            Size        = new Vector2(100, 36),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(_bg);

        _iconLabel = new Label
        {
            Position            = new Vector2(4, 4),
            Size                = new Vector2(28, 28),
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        _iconLabel.AddThemeColorOverride("font_color", Colors.White);
        _iconLabel.AddThemeFontSizeOverride("font_size", 14);
        AddChild(_iconLabel);

        _textLabel = new Label
        {
            Position            = new Vector2(34, 4),
            Size                = new Vector2(62, 28),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment   = VerticalAlignment.Center,
            AutowrapMode        = TextServer.AutowrapMode.Off,
            ClipText            = true,
        };
        _textLabel.AddThemeColorOverride("font_color", Colors.White);
        _textLabel.AddThemeFontSizeOverride("font_size", 12);
        AddChild(_textLabel);
    }

    public void SetRating(CardRating rating, string cardName = "")
    {
        if (_bg == null || _iconLabel == null || _textLabel == null) return;

        (_bg.Color, _iconLabel.Text, _textLabel.Text) = rating switch
        {
            CardRating.CorePick    => (CorePickBg, "★★★", "核心"),
            CardRating.GoodPick    => (GoodPickBg, "★★",  "推荐"),
            CardRating.Situational => (SituBg,     "★",   "可选"),
            CardRating.Weak        => (WeakBg,     "○",   "弱"),
            CardRating.Skip        => (SkipBg,     "✗",   "跳过"),
            _                     => (WeakBg,     "—",   "未知"),
        };
    }
}
