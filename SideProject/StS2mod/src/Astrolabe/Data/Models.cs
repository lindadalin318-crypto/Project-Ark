using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Astrolabe.Data;

// ─────────────────────────────────────────────
// 卡牌数据模型
// ─────────────────────────────────────────────

/// <summary>
/// Astrolabe 运行时使用的合并卡牌视图。
/// 由 <c>cards.core.json</c>（事实层）与 <c>cards.advisor.json</c>（顾问层）合并而成，
/// 对上层保持单对象读取体验，避免业务层感知底层拆分。
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

    [JsonPropertyName("cost")]
    public int Cost { get; set; } = 1;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "Skill";

    [JsonPropertyName("rarity")]
    public string Rarity { get; set; } = "Common";

    [JsonPropertyName("target")]
    public string Target { get; set; } = string.Empty;

    [JsonPropertyName("effects")]
    public CardEffectSet Effects { get; set; } = new();

    [JsonPropertyName("flags")]
    public CardFlagsData Flags { get; set; } = new();

    /// <summary>综合基础评分（0-10）</summary>
    [JsonPropertyName("base_score")]
    public float BaseScore { get; set; }

    /// <summary>Tier 评级（S/A/B/C/D）</summary>
    [JsonPropertyName("tier")]
    public string Tier { get; set; } = "C";

    /// <summary>各构筑方案中的评分（key = path_id，value = 0-10）</summary>
    [JsonPropertyName("path_scores")]
    public Dictionary<string, float> PathScores { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>保留旧版历史流派评分，仅用于审计与迁移，不参与当前 advisor 计算。</summary>
    [JsonPropertyName("legacy_path_scores")]
    public Dictionary<string, float> LegacyPathScores { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>协同标签（如 "strength"、"poison"、"exhaust"）</summary>
    [JsonPropertyName("synergy_tags")]
    public List<string> SynergyTags { get; set; } = new();

    /// <summary>反协同标签（与这些标签的牌同组时降分）</summary>
    [JsonPropertyName("anti_synergy_tags")]
    public List<string> AntiSynergyTags { get; set; } = new();

    /// <summary>各 Act 的评分（数组格式 [act1, act2, act3]）</summary>
    [JsonPropertyName("act_scaling")]
    public List<float> ActScaling { get; set; } = new() { 1f, 1f, 1f };

    /// <summary>获取指定 Act 的评分（1-indexed）</summary>
    public float GetActScore(int act)
    {
        int idx = Math.Clamp(act - 1, 0, ActScaling.Count - 1);
        return ActScaling.Count > 0 ? ActScaling[idx] : 1f;
    }

    /// <summary>升级优先级（"low" / "medium" / "high"）</summary>
    [JsonPropertyName("upgrade_priority")]
    public string UpgradePriority { get; set; } = "medium";

    /// <summary>将升级优先级文字转为 0-10 数值</summary>
    public float UpgradePriorityScore => UpgradePriority?.ToLowerInvariant() switch
    {
        "high"   => 8f,
        "medium" => 5f,
        "low"    => 2f,
        _         => 5f,
    };

    /// <summary>升级后的质变描述（中文）</summary>
    [JsonPropertyName("upgrade_delta_zh")]
    public string UpgradeDeltaZh { get; set; } = string.Empty;

    /// <summary>策略说明（中文）</summary>
    [JsonPropertyName("notes_zh")]
    public string NotesZh { get; set; } = string.Empty;

    /// <summary>
    /// advisor 侧新增的结构化先验字段。
    /// 当前阶段允许部分为空，逐步替代对 `notes_zh` 的文本猜测。
    /// </summary>
    [JsonPropertyName("advisor")]
    public CardAdvisorMetadata Advisor { get; set; } = new();
}

/// <summary>
/// <c>cards.core.json</c> 中的单卡事实层数据。
/// </summary>
public class CardCoreData
{
    [JsonPropertyName("card_id")]
    public string CardId { get; set; } = string.Empty;

    [JsonPropertyName("name_zh")]
    public string NameZh { get; set; } = string.Empty;

    [JsonPropertyName("name_en")]
    public string NameEn { get; set; } = string.Empty;

    [JsonPropertyName("character")]
    public string Character { get; set; } = string.Empty;

    [JsonPropertyName("cost")]
    public int Cost { get; set; } = 1;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "Skill";

    [JsonPropertyName("rarity")]
    public string Rarity { get; set; } = "Common";

    [JsonPropertyName("target")]
    public string Target { get; set; } = string.Empty;

    [JsonPropertyName("effects")]
    public CardEffectSet Effects { get; set; } = new();

    [JsonPropertyName("flags")]
    public CardFlagsData Flags { get; set; } = new();
}

/// <summary>
/// <c>cards.advisor.json</c> 中的单卡顾问层数据。
/// </summary>
public class CardAdvisorData
{
    [JsonPropertyName("card_id")]
    public string CardId { get; set; } = string.Empty;

    [JsonPropertyName("character")]
    public string Character { get; set; } = string.Empty;

    [JsonPropertyName("base_score")]
    public float BaseScore { get; set; }

    [JsonPropertyName("tier")]
    public string Tier { get; set; } = "C";

    [JsonPropertyName("path_scores")]
    public Dictionary<string, float> PathScores { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("legacy_path_scores")]
    public Dictionary<string, float> LegacyPathScores { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("synergy_tags")]
    public List<string> SynergyTags { get; set; } = new();

    [JsonPropertyName("anti_synergy_tags")]
    public List<string> AntiSynergyTags { get; set; } = new();

    [JsonPropertyName("act_scaling")]
    public List<float> ActScaling { get; set; } = new() { 1f, 1f, 1f };

    [JsonPropertyName("upgrade_priority")]
    public string UpgradePriority { get; set; } = "medium";

    [JsonPropertyName("upgrade_delta_zh")]
    public string UpgradeDeltaZh { get; set; } = string.Empty;

    [JsonPropertyName("notes_zh")]
    public string NotesZh { get; set; } = string.Empty;

    [JsonPropertyName("advisor")]
    public CardAdvisorMetadata Advisor { get; set; } = new();
}

public class CardEffectSet
{
    [JsonPropertyName("base")]
    public CardEffectProfile Base { get; set; } = new();

    [JsonPropertyName("upgraded")]
    public CardEffectProfile Upgraded { get; set; } = new();
}

public class CardEffectProfile
{
    [JsonPropertyName("damage")]
    public int? Damage { get; set; }

    [JsonPropertyName("block")]
    public int? Block { get; set; }

    [JsonPropertyName("draw")]
    public int? Draw { get; set; }

    [JsonPropertyName("energy_gain")]
    public int? EnergyGain { get; set; }

    [JsonPropertyName("hits")]
    public int? Hits { get; set; }

    [JsonPropertyName("discard")]
    public int? Discard { get; set; }

    [JsonPropertyName("exhaust_count")]
    public int? ExhaustCount { get; set; }

    [JsonPropertyName("hp_cost")]
    public int? HpCost { get; set; }

    [JsonPropertyName("apply")]
    public Dictionary<string, int> Apply { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("consume")]
    public Dictionary<string, int> Consume { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("create_cards")]
    public List<string> CreateCards { get; set; } = new();

    [JsonPropertyName("add_status")]
    public Dictionary<string, int> AddStatus { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("scale_from")]
    public List<string> ScaleFrom { get; set; } = new();
}

public class CardFlagsData
{
    [JsonPropertyName("exhaust")]
    public bool? Exhaust { get; set; }

    [JsonPropertyName("ethereal")]
    public bool? Ethereal { get; set; }

    [JsonPropertyName("retain")]
    public bool? Retain { get; set; }

    [JsonPropertyName("innate")]
    public bool? Innate { get; set; }

    [JsonPropertyName("x_cost")]
    public bool? XCost { get; set; }

    [JsonPropertyName("self_replicate")]
    public bool? SelfReplicate { get; set; }

    [JsonPropertyName("upgrades_hand")]
    public bool? UpgradesHand { get; set; }

    [JsonPropertyName("upgrades_deck")]
    public bool? UpgradesDeck { get; set; }
}

public class CardAdvisorMetadata
{
    [JsonPropertyName("roles")]
    public List<string> Roles { get; set; } = new();

    [JsonPropertyName("duplicate_policy")]
    public string DuplicatePolicy { get; set; } = "neutral";

    [JsonPropertyName("deck_pressure")]
    public string DeckPressure { get; set; } = "neutral";

    [JsonPropertyName("pickup_windows")]
    public List<string> PickupWindows { get; set; } = new();

    [JsonPropertyName("upgrade_spike")]
    public float? UpgradeSpike { get; set; }

    [JsonPropertyName("remove_priority")]
    public float? RemovePriority { get; set; }
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
        _ => Act4,
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

    [JsonPropertyName("character")]
    public string Character { get; set; } = "Shared";

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
