using Astrolabe.Engine;
using Godot;

namespace Astrolabe.UI;

/// <summary>
/// 战斗出牌建议面板。
/// 
/// 布局：
///   ┌─────────────────────────────────────────────────────────────────┐
///   │  底部建议栏（屏幕下方，手牌上方）                               │
///   │  [⚠ 危险！敌人将造成14伤害]  [建议出牌: ①防御 → ②打击 → ③...]  │
///   └─────────────────────────────────────────────────────────────────┘
/// 
///   手牌上方每张牌叠加出牌序号徽章（①②③ 绿色，✗ 灰色不建议）
/// </summary>
public class CombatAdvicePanel : Control
{
    // ── 颜色常量 ──────────────────────────────────────────────────────
    private static readonly Color BgColor       = new(0f,    0f,    0f,    0.78f);
    private static readonly Color WarningColor  = new(0.96f, 0.26f, 0.21f, 1f);
    private static readonly Color SafeColor     = new(0.30f, 0.69f, 0.31f, 1f);
    private static readonly Color SummaryColor  = new(1f,    1f,    0.40f, 1f);
    private static readonly Color OrderBg1      = new(0.18f, 0.65f, 0.22f, 0.92f);  // 第一优先：深绿
    private static readonly Color OrderBg2      = new(0.13f, 0.59f, 0.95f, 0.92f);  // 第二优先：蓝
    private static readonly Color OrderBg3      = new(1.00f, 0.76f, 0.03f, 0.92f);  // 第三优先：黄
    private static readonly Color OrderBgLow    = new(0.40f, 0.40f, 0.40f, 0.70f);  // 不建议：灰

    // ── 底部建议栏 ─────────────────────────────────────────────────────
    private Panel?  _bottomBar;
    private Label?  _warningLabel;
    private Label?  _summaryLabel;
    private Label?  _orderLabel;

    // ── 手牌上方的序号徽章（最多10张手牌）─────────────────────────────
    private readonly List<CardOrderBadge> _badges = new();

    // 徽章放在底部建议栏下方，屏幕最底部
    // 建议栏 Y=790, H=64 → 底边 Y=854；徽章 H=28，紧贴底部
    private const float BADGE_Y = 856f;

    public CombatAdvicePanel()
    {
        SetPosition(Vector2.Zero);
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;

        BuildBottomBar();
    }

    private void BuildBottomBar()
    {
        // 底部建议栏：屏幕底部，手牌上方
        _bottomBar = new Panel
        {
            Position = new Vector2(0, 790),
            Size     = new Vector2(1920, 64),
        };
        var bg = new StyleBoxFlat { BgColor = BgColor };
        _bottomBar.AddThemeStyleboxOverride("panel", bg);
        _bottomBar.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(_bottomBar);

        // 警告标签（左侧）
        _warningLabel = new Label
        {
            Position            = new Vector2(16, 10),
            Size                = new Vector2(480, 44),
            VerticalAlignment   = VerticalAlignment.Center,
            AutowrapMode        = TextServer.AutowrapMode.Off,
            ClipText            = true,
        };
        _warningLabel.AddThemeFontSizeOverride("font_size", 15);
        _warningLabel.AddThemeColorOverride("font_color", WarningColor);
        _bottomBar.AddChild(_warningLabel);

        // 出牌顺序标签（中间）
        _orderLabel = new Label
        {
            Position            = new Vector2(510, 10),
            Size                = new Vector2(700, 44),
            VerticalAlignment   = VerticalAlignment.Center,
            AutowrapMode        = TextServer.AutowrapMode.Off,
        };
        _orderLabel.AddThemeFontSizeOverride("font_size", 14);
        _orderLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.9f));
        _bottomBar.AddChild(_orderLabel);

        // 总结标签（右侧）
        _summaryLabel = new Label
        {
            Position            = new Vector2(1230, 10),
            Size                = new Vector2(670, 44),
            VerticalAlignment   = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right,
            AutowrapMode        = TextServer.AutowrapMode.Off,
        };
        _summaryLabel.AddThemeFontSizeOverride("font_size", 14);
        _summaryLabel.AddThemeColorOverride("font_color", SummaryColor);
        _bottomBar.AddChild(_summaryLabel);
    }

    // ── 公开更新接口 ──────────────────────────────────────────────────

    public void UpdateAdvice(CombatAdvice advice)
    {
        UpdateBottomBar(advice);
        UpdateCardBadges(advice);
    }

    private void UpdateBottomBar(CombatAdvice advice)
    {
        if (_warningLabel == null) return;

        // 敌人警告
        if (advice.EnemyWarnings.Count > 0)
        {
            var topWarn = advice.EnemyWarnings.OrderByDescending(w => w.AttackDamage).First();
            _warningLabel.Text = $"⚠ {topWarn.WarningText}";
            _warningLabel.AddThemeColorOverride("font_color",
                topWarn.WillPierce ? WarningColor : SafeColor);
        }
        else
        {
            _warningLabel.Text = "✓ 敌人本回合不攻击";
            _warningLabel.AddThemeColorOverride("font_color", SafeColor);
        }

        // 出牌顺序
        if (_orderLabel != null)
        {
            string[] symbols = { "①", "②", "③", "④", "⑤", "⑥", "⑦", "⑧", "⑨", "⑩" };
            var ordered = advice.CardAdvices
                .Where(c => c.PlayOrder > 0)
                .OrderBy(c => c.PlayOrder)
                .Select(c =>
                {
                    string sym = c.PlayOrder <= symbols.Length ? symbols[c.PlayOrder - 1] : c.PlayOrder.ToString();
                    return $"{sym}{c.Card.CardNameZh}";
                })
                .ToList();

            _orderLabel.Text = ordered.Count > 0
                ? "出牌: " + string.Join(" → ", ordered)
                : "无建议出牌";
        }

        // 总结
        if (_summaryLabel != null)
            _summaryLabel.Text = advice.SummaryText;
    }

    private void UpdateCardBadges(CombatAdvice advice)
    {
        // 清除旧徽章
        foreach (var b in _badges) b.QueueFree();
        _badges.Clear();

        int cardCount = advice.CardAdvices.Count;
        if (cardCount == 0) return;

        // 手牌在屏幕下方中央排列，估算总跨度约为 min(cardCount*130, 900)px
        // 居中对齐屏幕（1920px 宽），徽章等距对应每张手牌底部
        float handSpan    = Math.Min(cardCount * 130f, 900f);
        float cardSpacing = cardCount > 1 ? handSpan / (cardCount - 1) : 0f;
        float totalWidth  = (cardCount - 1) * cardSpacing;
        float startX      = (1920f - totalWidth) / 2f;

        for (int i = 0; i < Math.Min(cardCount, 10); i++)
        {
            var score = advice.CardAdvices[i];
            float x = startX + i * cardSpacing;

            var badge = new CardOrderBadge();
            badge.SetOrder(score.PlayOrder, score.Card.CardNameZh);
            badge.Position = new Vector2(x - 24f, BADGE_Y);
            AddChild(badge);
            _badges.Add(badge);
        }
    }
}

// ── 出牌序号徽章 ──────────────────────────────────────────────────────

/// <summary>叠加在每张手牌上方的圆形序号标记</summary>
public class CardOrderBadge : Control
{
    private static readonly Color[] OrderColors =
    {
        new(0.18f, 0.65f, 0.22f, 0.92f),  // ①绿
        new(0.13f, 0.59f, 0.95f, 0.92f),  // ②蓝
        new(1.00f, 0.76f, 0.03f, 0.92f),  // ③黄
        new(0.91f, 0.46f, 0.00f, 0.92f),  // ④橙
        new(0.60f, 0.20f, 0.80f, 0.92f),  // ⑤紫
    };
    private static readonly string[] OrderSymbols = { "①", "②", "③", "④", "⑤", "⑥", "⑦", "⑧", "⑨", "⑩" };

    private ColorRect? _bg;
    private Label?     _label;

    public CardOrderBadge()
    {
        SetSize(new Vector2(48f, 28f));
        MouseFilter = MouseFilterEnum.Ignore;

        _bg = new ColorRect
        {
            Size        = new Vector2(48f, 28f),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(_bg);

        _label = new Label
        {
            Size                = new Vector2(48f, 28f),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment   = VerticalAlignment.Center,
            MouseFilter         = MouseFilterEnum.Ignore,
        };
        _label.AddThemeColorOverride("font_color", Colors.White);
        _label.AddThemeFontSizeOverride("font_size", 15);
        AddChild(_label);
    }

    public void SetOrder(int playOrder, string cardName)
    {
        if (_bg == null || _label == null) return;

        if (playOrder <= 0)
        {
            // 不建议出牌：灰色 X
            _bg.Color   = new Color(0.3f, 0.3f, 0.3f, 0.55f);
            _label.Text = "✗";
            _label.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
        }
        else
        {
            int idx     = Math.Clamp(playOrder - 1, 0, OrderColors.Length - 1);
            _bg.Color   = OrderColors[idx];
            _label.Text = playOrder <= OrderSymbols.Length ? OrderSymbols[playOrder - 1] : playOrder.ToString();
            _label.AddThemeColorOverride("font_color", Colors.White);
        }
    }
}
