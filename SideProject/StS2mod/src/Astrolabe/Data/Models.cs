using System.Text.Json.Serialization;

namespace Astrolabe.Data;

// ─────────────────────────────────────────────
// 卡牌数据模型
// ─────────────────────────────────────────────

/// <summary>
/// 单张卡牌的评分数据（来自 cards.json）。
/// </summary>
public class CardData
{
    [JsonPropertyName("card_id")]
    public string CardId { get; set; } = string.Empty;

    [JsonPropertyName("name_zh")]
    public string NameZh { get; set; } = string.Empty;

    [JsonPropertyName("name_en")]
    public string NameEn { get; set; } = string.Empty;

    [JsonPropertyName("character")]
    public string Character { get; set; } = string.Empty;

    /// <summary>综合基础评分（0-10）</summary>
    [JsonPropertyName("base_score")]
    public float BaseScore { get; set; }

    /// <summary>Tier 评级（S/A/B/C/D）</summary>
    [JsonPropertyName("tier")]
    public string Tier { get; set; } = "C";

    /// <summary>各构筑方案中的评分（key = path_id，value = 0-10）</summary>
    [JsonPropertyName("path_scores")]
    public Dictionary<string, float> PathScores { get; set; } = new();

    /// <summary>协同标签（如 "strength"、"poison"、"exhaust"）</summary>
    [JsonPropertyName("synergy_tags")]
    public List<string> SynergyTags { get; set; } = new();

    /// <summary>反协同标签（与这些标签的牌同组时降分）</summary>
    [JsonPropertyName("anti_synergy_tags")]
    public List<string> AntiSynergyTags { get; set; } = new();

    /// <summary>各 Act 的评分系数（1.0 = 无修正）</summary>
    [JsonPropertyName("act_scaling")]
    public ActScaling ActScaling { get; set; } = new();

    /// <summary>升级优先级（0-10，越高越值得优先升级）</summary>
    [JsonPropertyName("upgrade_priority")]
    public float UpgradePriority { get; set; }

    /// <summary>升级后的质变描述（中文）</summary>
    [JsonPropertyName("upgrade_delta_zh")]
    public string UpgradeDeltaZh { get; set; } = string.Empty;

    /// <summary>策略说明（中文）</summary>
    [JsonPropertyName("notes_zh")]
    public string NotesZh { get; set; } = string.Empty;
}

public class ActScaling
{
    [JsonPropertyName("act1")] public float Act1 { get; set; } = 1.0f;
    [JsonPropertyName("act2")] public float Act2 { get; set; } = 1.0f;
    [JsonPropertyName("act3")] public float Act3 { get; set; } = 1.0f;
    [JsonPropertyName("act4")] public float Act4 { get; set; } = 1.0f;

    public float ForAct(int act) => act switch
    {
        1 => Act1,
        2 => Act2,
        3 => Act3,
        _ => Act4
    };
}

// ─────────────────────────────────────────────
// 遗物数据模型
// ─────────────────────────────────────────────

public class RelicData
{
    [JsonPropertyName("relic_id")]
    public string RelicId { get; set; } = string.Empty;

    [JsonPropertyName("name_zh")]
    public string NameZh { get; set; } = string.Empty;

    [JsonPropertyName("name_en")]
    public string NameEn { get; set; } = string.Empty;

    [JsonPropertyName("tier")]
    public string Tier { get; set; } = "common";

    [JsonPropertyName("base_score")]
    public float BaseScore { get; set; }

    [JsonPropertyName("path_scores")]
    public Dictionary<string, float> PathScores { get; set; } = new();

    [JsonPropertyName("synergy_tags")]
    public List<string> SynergyTags { get; set; } = new();

    [JsonPropertyName("notes_zh")]
    public string NotesZh { get; set; } = string.Empty;
}

// ─────────────────────────────────────────────
// 构筑方案数据模型
// ─────────────────────────────────────────────

/// <summary>
/// 一套构筑方案的完整定义（来自 buildpaths.json）。
/// </summary>
public class BuildPathData
{
    [JsonPropertyName("path_id")]
    public string PathId { get; set; } = string.Empty;

    [JsonPropertyName("name_zh")]
    public string NameZh { get; set; } = string.Empty;

    [JsonPropertyName("name_en")]
    public string NameEn { get; set; } = string.Empty;

    [JsonPropertyName("character")]
    public string Character { get; set; } = string.Empty;

    [JsonPropertyName("description_zh")]
    public string DescriptionZh { get; set; } = string.Empty;

    /// <summary>核心卡牌 ID 列表（拥有越多，方案可行性越高）</summary>
    [JsonPropertyName("core_cards")]
    public List<string> CoreCards { get; set; } = new();

    /// <summary>关键遗物 ID 列表</summary>
    [JsonPropertyName("key_relics")]
    public List<string> KeyRelics { get; set; } = new();

    /// <summary>理想牌组大小范围</summary>
    [JsonPropertyName("ideal_deck_size")]
    public DeckSizeRange IdealDeckSize { get; set; } = new();

    /// <summary>精英战节点权重（1.0 = 默认）</summary>
    [JsonPropertyName("elite_weight")]
    public float EliteWeight { get; set; } = 1.0f;

    /// <summary>商店节点权重</summary>
    [JsonPropertyName("shop_weight")]
    public float ShopWeight { get; set; } = 1.0f;

    /// <summary>篝火升级优先卡 ID 列表（按优先级排序）</summary>
    [JsonPropertyName("campfire_upgrades")]
    public List<string> CampfireUpgrades { get; set; } = new();

    /// <summary>对此方案有利的 Boss ID</summary>
    [JsonPropertyName("good_against_bosses")]
    public List<string> GoodAgainstBosses { get; set; } = new();

    /// <summary>对此方案不利的 Boss ID</summary>
    [JsonPropertyName("bad_against_bosses")]
    public List<string> BadAgainstBosses { get; set; } = new();
}

public class DeckSizeRange
{
    [JsonPropertyName("min")] public int Min { get; set; } = 10;
    [JsonPropertyName("max")] public int Max { get; set; } = 20;
}

// ─────────────────────────────────────────────
// Boss 数据模型
// ─────────────────────────────────────────────

public class BossData
{
    [JsonPropertyName("boss_id")]
    public string BossId { get; set; } = string.Empty;

    [JsonPropertyName("name_zh")]
    public string NameZh { get; set; } = string.Empty;

    [JsonPropertyName("act")]
    public int Act { get; set; }

    [JsonPropertyName("core_mechanics")]
    public List<string> CoreMechanics { get; set; } = new();

    [JsonPropertyName("dangerous_to_paths")]
    public List<string> DangerousToPaths { get; set; } = new();

    [JsonPropertyName("counter_cards")]
    public List<string> CounterCards { get; set; } = new();

    [JsonPropertyName("notes_zh")]
    public string NotesZh { get; set; } = string.Empty;
}

// ─────────────────────────────────────────────
// 事件数据模型
// ─────────────────────────────────────────────

public class EventData
{
    [JsonPropertyName("event_id")]
    public string EventId { get; set; } = string.Empty;

    [JsonPropertyName("name_zh")]
    public string NameZh { get; set; } = string.Empty;

    [JsonPropertyName("act")]
    public int Act { get; set; }

    [JsonPropertyName("options")]
    public List<EventOption> Options { get; set; } = new();
}

public class EventOption
{
    [JsonPropertyName("option_id")]
    public string OptionId { get; set; } = string.Empty;

    [JsonPropertyName("description_zh")]
    public string DescriptionZh { get; set; } = string.Empty;

    /// <summary>期望价值评分（-10 到 10，负值表示预期为负面）</summary>
    [JsonPropertyName("expected_value")]
    public float ExpectedValue { get; set; }

    /// <summary>是否为危险选项（可能损失 HP / 负面 Debuff）</summary>
    [JsonPropertyName("is_risky")]
    public bool IsRisky { get; set; }

    /// <summary>此选项需要持有特定遗物才有价值（为空则无条件评估）</summary>
    [JsonPropertyName("requires_relic")]
    public string? RequiresRelic { get; set; }

    [JsonPropertyName("notes_zh")]
    public string NotesZh { get; set; } = string.Empty;
}
