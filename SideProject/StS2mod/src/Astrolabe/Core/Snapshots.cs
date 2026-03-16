namespace Astrolabe.Core;

/// <summary>
/// 跑分全局状态快照（战斗外任意时刻可构建）。
/// 所有字段使用游戏内 canonical ID（以 `ModelId.Entry` / `BossEncounter.Id.Entry` 为准），便于序列化和测试。
/// </summary>

public class RunSnapshot
{
    public int HP { get; set; }
    public int MaxHP { get; set; }
    public int Gold { get; set; }
    public int Floor { get; set; }
    public int Act { get; set; }

    /// <summary>完整牌库中所有牌的 ID（含升级标记，如 `STRIKE_IRONCLAD+`）</summary>

    public List<string> DeckCardIds { get; set; } = new();

    /// <summary>当前持有的遗物 ID 列表（有序，反映获取顺序）</summary>
    public List<string> RelicIds { get; set; } = new();

    /// <summary>当前持有的药水 ID 列表</summary>
    public List<string> PotionIds { get; set; } = new();

    /// <summary>当前角色 ID（如 "IRONCLAD"、"SILENT"、"DEFECT"）</summary>

    public string CharacterId { get; set; } = string.Empty;

    /// <summary>当前 Act 已分配的 Boss ID（`BossEncounter.Id.Entry`，用于路线规划时的针对性建议）</summary>

    public string? ActBossId { get; set; }

    /// <summary>快照创建时间（调试用）</summary>
    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;

    public bool IsValid => !string.IsNullOrEmpty(CharacterId) && MaxHP > 0;
}

/// <summary>
/// 战斗内实时状态快照（每次出牌后重建）。
/// 继承 RunSnapshot，追加战斗专属字段。
/// </summary>
public class CombatSnapshot : RunSnapshot
{
    /// <summary>玩家当前能量</summary>
    public int Energy { get; set; }

    /// <summary>玩家最大能量（本回合）</summary>
    public int MaxEnergy { get; set; }

    /// <summary>玩家当前格挡值</summary>
    public int Block { get; set; }

    /// <summary>当前手牌 ID 列表</summary>
    public List<string> HandCardIds { get; set; } = new();

    /// <summary>当前抽牌堆 ID 列表（顺序不定）</summary>
    public List<string> DrawPileCardIds { get; set; } = new();

    /// <summary>当前弃牌堆 ID 列表</summary>
    public List<string> DiscardPileCardIds { get; set; } = new();

    /// <summary>当前 Buff/Debuff 列表（玩家）</summary>
    public List<StatusEffect> PlayerStatuses { get; set; } = new();

    /// <summary>战场上的所有敌人状态</summary>
    public List<EnemyState> Enemies { get; set; } = new();

    /// <summary>当前回合数</summary>
    public int TurnNumber { get; set; }

    /// <summary>是否为玩家回合</summary>
    public bool IsPlayerTurn { get; set; }
}

/// <summary>
/// 单个敌人的战斗状态。
/// </summary>
public class EnemyState
{
    public string EnemyId { get; set; } = string.Empty;
    public string EnemyName { get; set; } = string.Empty;
    public int HP { get; set; }
    public int MaxHP { get; set; }
    public int Block { get; set; }

    /// <summary>当前意图类型（"Attack" / "Defend" / "Buff" / "Debuff" / "Unknown"）</summary>
    public string Intent { get; set; } = "Unknown";

    /// <summary>攻击意图的伤害数值（非攻击意图时为 0）</summary>
    public int IntentDamage { get; set; }

    /// <summary>攻击次数（多段攻击；普通单段为 1）</summary>
    public int IntentTimes { get; set; } = 1;

    /// <summary>总意图伤害 = IntentDamage × IntentTimes</summary>
    public int TotalIntentDamage => IntentDamage * IntentTimes;

    /// <summary>敌人当前 Buff/Debuff 列表</summary>
    public List<StatusEffect> Statuses { get; set; } = new();
}

/// <summary>
/// Buff / Debuff 效果（玩家或敌人均使用）。
/// </summary>
public class StatusEffect
{
    public string StatusId { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public int Stacks { get; set; }

    /// <summary>true = 正面效果（Buff），false = 负面效果（Debuff）</summary>
    public bool IsPositive { get; set; }
}
