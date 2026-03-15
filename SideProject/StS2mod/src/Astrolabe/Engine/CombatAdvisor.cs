using Astrolabe.Data;

namespace Astrolabe.Engine;

/// <summary>
/// 战斗出牌建议引擎。
/// 
/// 设计原则：启发式评分（非穷举），优先级规则：
///   1. 如果敌人本回合会打出致命伤害 → 优先出防御牌
///   2. 如果敌人本回合不攻击 → 优先出伤害/增益牌
///   3. 在能量限制内，按"性价比"排序出牌
///   4. 标注最优出牌序号（1=先打，2=次之…）
/// </summary>
public static class CombatAdvisor
{
    // 前向模拟性能阈值：可出牌 ≤ 此值时枚举全排列，否则退化为贪心
    private const int FORWARD_SIM_THRESHOLD = 6;

    /// <summary>
    /// 根据当前战斗快照生成出牌建议。
    /// ≤6 张可出牌时使用前向模拟找最优序列；否则退化为贪心排序。
    /// </summary>
    public static CombatAdvice Analyze(CombatSnapshot snap)
    {
        var advice = new CombatAdvice
        {
            EnemyWarnings = BuildEnemyWarnings(snap),
        };

        if (snap.HandCards.Count == 0)
        {
            advice.SummaryText = "手牌为空";
            return advice;
        }

        // 为每张手牌计算基础分（用于贪心 fallback 和 UI reason 展示）
        var scored = snap.HandCards
            .Select((card, idx) => ScoreCard(card, idx, snap))
            .ToList();

        // 筛选"可出"的牌（能量够）
        var playable = scored
            .Where(s => s.Score > 0f)
            .ToList();

        if (playable.Count == 0)
        {
            advice.CardAdvices = scored.OrderBy(s => s.HandIndex).ToList();
            advice.SummaryText = snap.CurrentEnergy == 0 ? "能量耗尽，结束回合" : "无可出牌";
            return advice;
        }

        // 决定是否前向模拟
        int[] bestOrder;
        if (playable.Count <= FORWARD_SIM_THRESHOLD)
        {
            bestOrder = ForwardSimulate(playable, snap);
        }
        else
        {
            // 贪心：按评分排序，顺序分配，X费最后
            bestOrder = GreedyOrder(playable, snap);
        }

        // 把模拟结果写回 PlayOrder
        // bestOrder[i] = playable 列表中第 i 个元素的出牌序号（0 = 不出）
        for (int i = 0; i < playable.Count; i++)
            playable[i].PlayOrder = bestOrder[i];

        // 不可出的牌 PlayOrder = 0（ScoreCard 已设置 -1，重置为 0 让 UI 正常）
        foreach (var s in scored.Where(s => s.Score <= 0f))
            s.PlayOrder = 0;

        // 按原始手牌顺序返回（方便 UI 按位置渲染）
        advice.CardAdvices = scored.OrderBy(s => s.HandIndex).ToList();
        advice.SummaryText = BuildSummaryText(advice, snap);

        return advice;
    }

    // ── 前向模拟：枚举全排列 ────────────────────────────────────────────

    /// <summary>
    /// 枚举 playable 牌的全部出牌排列，取 SimulateSequence 得分最高的序列，
    /// 返回每张牌对应的 PlayOrder（0 = 不出）。
    /// </summary>
    private static int[] ForwardSimulate(List<CombatCardScore> playable, CombatSnapshot snap)
    {
        int n = playable.Count;
        int[] bestOrder  = new int[n];
        float bestScore  = float.MinValue;
        int[] currentPerm = Enumerable.Range(0, n).ToArray();

        // 枚举全排列（Heap's algorithm 风格，但用 indices）
        // 实际上枚举所有有效出牌序列（能量不够则截断）
        foreach (var perm in GetPermutations(n))
        {
            float seqScore = SimulateSequence(perm, playable, snap, out int[] orders);
            if (seqScore > bestScore)
            {
                bestScore = seqScore;
                Array.Copy(orders, bestOrder, n);
            }
        }

        return bestOrder;
    }

    /// <summary>
    /// 模拟一种出牌排列，返回该序列的总收益分。
    /// orders[i] = playable[i] 的出牌序号（0=跳过）。
    /// </summary>
    private static float SimulateSequence(
        int[] perm,
        List<CombatCardScore> playable,
        CombatSnapshot snap,
        out int[] orders)
    {
        int n         = playable.Count;
        orders        = new int[n];
        int energy    = snap.CurrentEnergy;
        int strength  = snap.PlayerStrength;
        int cardsPlayed = snap.CardsPlayedThisTurn;
        float totalScore  = 0f;
        int playOrder = 1;
        int xCostIdx  = -1;  // X费牌索引，最后处理

        // 第一遍：非 X 费牌按 perm 顺序出
        for (int step = 0; step < perm.Length; step++)
        {
            int i = perm[step];
            var cs   = playable[i];
            var card = cs.Card;

            if (card.Cost == -1)
            {
                xCostIdx = i;  // X费牌留到最后
                continue;
            }

            if (card.Cost > energy) continue;  // 能量不足跳过

            energy    -= card.Cost;
            cardsPlayed++;

            // 建模效果
            totalScore += SimCardValue(card, snap, strength, cardsPlayed);

            // Power牌：后续攻击牌力量 +Effect（简化：+2 或 +3）
            if (card.CardType == "Power" && (card.Tags.Contains("buff") || card.Tags.Contains("strength")))
                strength += 2;  // 保守估算（Inflame是+2，DevilForm是持续）

            orders[i] = playOrder++;
        }

        // 第二遍：X 费牌（用剩余能量）
        if (xCostIdx >= 0 && energy > 0)
        {
            var xCard = playable[xCostIdx].Card;
            totalScore += energy * 2.5f + (DataLoader.GetCard(xCard.CardId)?.BaseScore ?? InferBaseScore(xCard));
            orders[xCostIdx] = playOrder++;
        }

        return totalScore;
    }

    /// <summary>
    /// 计算单张牌在当前模拟状态下的贡献分。
    /// （与 ScoreCard 不同：只计算出牌收益，不含"建议权重"逻辑）
    /// </summary>
    private static float SimCardValue(CombatCardInfo card, CombatSnapshot snap, int strength, int cardsPlayed)
    {
        var data       = DataLoader.GetCard(card.CardId);
        float baseScore = data?.BaseScore ?? InferBaseScore(card);

        // 威胁评估
        bool enemyAttacking  = snap.EnemyIntents.Any(e => e.WillAttack);
        int  totalIncoming   = snap.EnemyIntents.Sum(e => e.AttackDamage);
        float incomingMult   = snap.PlayerVulnerable > 0 ? 1.5f : 1.0f;
        int effectiveIncoming = (int)(totalIncoming * incomingMult);
        int netDamage        = effectiveIncoming - snap.CurrentBlock;
        bool isLethal        = netDamage >= snap.CurrentHP;
        bool isDangerous     = netDamage > snap.CurrentHP * 0.4f;

        // 敌人易伤
        int   enemyVuln     = snap.GetEnemyPowerTotal("VULNERABLE");
        float enemyVulnMult = enemyVuln > 0 ? 1.5f : 1.0f;

        // 玩家虚弱
        float weakPenalty   = snap.PlayerWeak > 0 ? 0.75f : 1.0f;
        float strengthMult  = strength > 0 ? 1f + strength * 0.15f : 1f;

        return card.CardType switch
        {
            "Attack" => baseScore * strengthMult * weakPenalty * enemyVulnMult
                        * (enemyAttacking && (isLethal || isDangerous) ? 0.8f : 1f),

            "Skill" when card.Tags.Contains("block") =>
                baseScore * (isLethal ? 2.5f : isDangerous ? 2.0f : enemyAttacking ? 1.0f : 0.5f),

            "Power" => baseScore * 1.5f
                        + snap.HandCards.Count(c => c.CardType == "Attack") * 1.5f,

            _ => baseScore * 0.8f,
        };
    }

    /// <summary>
    /// 贪心排序（>6张可出牌时退化方案）：先按评分排序，能量优先分配，X费最后。
    /// </summary>
    private static int[] GreedyOrder(List<CombatCardScore> playable, CombatSnapshot snap)
    {
        int n         = playable.Count;
        int[] orders  = new int[n];
        int energy    = snap.CurrentEnergy;
        int order     = 1;

        // 先处理非 X 费牌（按评分降序）
        var nonX = playable
            .Select((s, i) => (score: s, idx: i))
            .Where(p => p.score.Card.Cost != -1)
            .OrderByDescending(p => p.score.Score);

        foreach (var (s, i) in nonX)
        {
            if (s.Card.Cost <= energy)
            {
                energy -= s.Card.Cost;
                orders[i] = order++;
            }
        }

        // 再处理 X 费牌
        var xCards = playable
            .Select((s, i) => (score: s, idx: i))
            .Where(p => p.score.Card.Cost == -1);

        foreach (var (s, i) in xCards)
        {
            if (energy > 0)
            {
                orders[i] = order++;
                energy     = 0;
            }
        }

        return orders;
    }

    /// <summary>生成 0..n-1 的所有排列（递归）</summary>
    private static IEnumerable<int[]> GetPermutations(int n)
    {
        var arr = Enumerable.Range(0, n).ToArray();
        return GeneratePerms(arr, 0);
    }

    private static IEnumerable<int[]> GeneratePerms(int[] arr, int start)
    {
        if (start == arr.Length - 1)
        {
            yield return (int[])arr.Clone();
            yield break;
        }

        for (int i = start; i < arr.Length; i++)
        {
            (arr[start], arr[i]) = (arr[i], arr[start]);
            foreach (var p in GeneratePerms(arr, start + 1))
                yield return p;
            (arr[start], arr[i]) = (arr[i], arr[start]);
        }
    }

    // ── 单牌评分 ──────────────────────────────────────────────────────

    private static CombatCardScore ScoreCard(CombatCardInfo card, int handIndex, CombatSnapshot snap)
    {
        float score = 0f;
        var reasons = new List<string>();

        var cardData    = DataLoader.GetCard(card.CardId);
        float baseScore = cardData?.BaseScore ?? InferBaseScore(card);

        bool enemyAttacking      = snap.EnemyIntents.Any(e => e.WillAttack);
        int  totalIncoming       = snap.EnemyIntents.Sum(e => e.AttackDamage);
        // 玩家易伤时受到更多伤害（×1.5），影响威胁判断
        float incomingMult       = snap.PlayerVulnerable > 0 ? 1.5f : 1.0f;
        int  effectiveIncoming   = (int)(totalIncoming * incomingMult);
        int  netDamage           = effectiveIncoming - snap.CurrentBlock;
        bool isDangerous         = netDamage > snap.CurrentHP * 0.4f;
        bool isLethal            = netDamage >= snap.CurrentHP;

        // ── 玩家 Buff/Debuff 对攻击牌的乘数 ─────────────────────────────
        // 力量：每点力量让攻击牌额外打 1 点（多次攻击倍增）
        float strengthBonus = snap.PlayerStrength > 0 ? 1f + snap.PlayerStrength * 0.15f : 1f;
        // 虚弱：攻击伤害 ×0.75
        float weakPenalty   = snap.PlayerWeak > 0 ? 0.75f : 1f;
        // 荆棘：敌人每次攻击都会受到反伤，防御牌让敌人多打→多吃荆棘
        float thornsBonus   = snap.PlayerThorns > 0 ? 1f + snap.PlayerThorns * 0.1f : 1f;
        // 愤怒：每出攻击牌获得力量，攻击牌连出收益递增
        float enrageBonus   = snap.PlayerEnrage > 0 ? 1f + snap.PlayerEnrage * 0.1f * snap.CardsPlayedThisTurn : 1f;

        // ── 能量不足：直接排除 ────────────────────────────────────────
        if (card.Cost > snap.CurrentEnergy && card.Cost != -1)
        {
            return new CombatCardScore { Card = card, HandIndex = handIndex, Score = -1f, Reasons = new() { "能量不足" } };
        }

        switch (card.CardType)
        {
            // ── Attack ───────────────────────────────────────────────
            case "Attack":
                // 敌人易伤：攻击伤害 ×1.5，攻击牌价值提升
                int enemyVulnerable = snap.GetEnemyPowerTotal("VULNERABLE");
                float enemyVulnMult = enemyVulnerable > 0 ? 1.5f : 1.0f;

                // 基础分 × 力量加成 × 虚弱惩罚 × 愤怒加成 × 敌人易伤
                score += baseScore * strengthBonus * weakPenalty * enrageBonus * enemyVulnMult;

                if (!enemyAttacking)
                {
                    score *= 1.3f;
                    reasons.Add(snap.PlayerStrength > 0
                        ? $"进攻（力量+{snap.PlayerStrength}）"
                        : "进攻");
                }
                else if (isLethal || isDangerous)
                {
                    score *= 0.8f;
                    reasons.Add("攻击（危险局）");
                }
                else
                {
                    reasons.Add(snap.PlayerStrength > 0
                        ? $"攻击（力量+{snap.PlayerStrength}）"
                        : "攻击");
                }

                if (snap.PlayerWeak > 0)    reasons.Add($"虚弱×{snap.PlayerWeak}↓");
                if (enemyVulnerable > 0)    reasons.Add($"敌人易伤×{enemyVulnerable}↑");

                // vulnerable_apply：本牌施加易伤，手牌里还有攻击牌时超值（后续攻击×1.5）
                if (card.Tags.Contains("vulnerable_apply"))
                {
                    int remainingAttacks = snap.HandCards.Count(c => c.CardType == "Attack" && c.CardId != card.CardId);
                    if (remainingAttacks > 0)
                    {
                        score += remainingAttacks * 2.0f;
                        reasons.Add($"施加易伤（后续{remainingAttacks}张受益）");
                    }
                }

                // multi_hit：多段命中，敌人有荆棘时每段都受反伤，降低此牌价值
                int enemyThorns = snap.GetEnemyPowerTotal("THORNS");
                if (card.Tags.Contains("multi_hit") && enemyThorns > 0)
                {
                    score -= enemyThorns * 1.5f;
                    reasons.Add($"多段×荆棘{enemyThorns}↓");
                }

                // Power 牌在手时，攻击牌排到 Power 之后（Power先出收益更高）
                if (snap.HasPowerInHand) score -= 2f;
                break;

            // ── Skill ────────────────────────────────────────────────
            case "Skill":
                // 0费技能：出牌不消耗能量，几乎无损失，轻微加分
                if (card.Cost == 0) score += 1.5f;

                bool isBlock    = card.Tags.Contains("block");
                bool isBuff     = card.Tags.Contains("buff") || card.Tags.Contains("strength");
                bool isDrawCard = card.Tags.Contains("draw");

                if (isBlock)
                {
                    if (isLethal)
                    {
                        score += baseScore * 2.5f;
                        reasons.Add("🔴防御（致命）");
                    }
                    else if (isDangerous)
                    {
                        score += baseScore * 2.0f;
                        reasons.Add("⚠防御（危险）");
                    }
                    else if (!enemyAttacking)
                    {
                        score += baseScore * 0.5f;
                        reasons.Add("防御（无威胁）");
                    }
                    else
                    {
                        score += baseScore * 1.0f;
                        reasons.Add("防御");
                    }

                    // 荆棘加成：让敌人多打一次即额外反伤
                    if (snap.PlayerThorns > 0)
                    {
                        score += snap.PlayerThorns * thornsBonus;
                        reasons.Add($"荆棘+{snap.PlayerThorns}");
                    }
                    // 玩家易伤时防御牌紧迫度提升（受伤更多）
                    if (snap.PlayerVulnerable > 0)
                    {
                        score *= 1.2f;
                        reasons.Add($"易伤×{snap.PlayerVulnerable}↑");
                    }
                }
                else if (isBuff)
                {
                    // 增益技能：越早出越好（后续攻击牌受益）
                    score += baseScore * 1.2f;
                    score += snap.HandCards.Count(c => c.CardType == "Attack") * 0.5f; // 手牌攻击牌越多越值得先出增益
                    reasons.Add("增益");
                }
                else if (isDrawCard)
                {
                    // 摸牌技能：抽牌堆少时价值高（续航）
                    float drawBonus = snap.DrawPileCount < 5 ? 2f : 0.5f;
                    score += baseScore * 0.9f + drawBonus;
                    reasons.Add("摸牌");
                }
                else
                {
                    score += baseScore * 0.8f;
                }
                break;

            // ── Power ────────────────────────────────────────────────
            case "Power":
                // 强化牌：手牌中攻击牌越多，先出 Power 收益越高
                int attacksInHand = snap.HandCards.Count(c => c.CardType == "Attack" && c.Cost <= snap.CurrentEnergy - card.Cost);
                score += baseScore * 1.5f;
                score += attacksInHand * 1.5f;
                // 已有力量时，再叠 Power 收益递增
                if (snap.PlayerStrength > 0) score += snap.PlayerStrength * 0.5f;
                reasons.Add($"强化（+{attacksInHand}张攻击受益）");
                break;

            default:
                score += baseScore;
                break;
        }

        // ── X 费牌：能量越多越强，排在普通牌之后（先出其他牌再X费）────
        if (card.Cost == -1)
        {
            // X费牌的价值 = 能量 * 系数，但要减去已计划花费的能量
            score = snap.CurrentEnergy * 2.5f + baseScore;
            score -= 1f; // 轻微惩罚让其他牌先出，最后再X
            reasons.Add($"X费（当前{snap.CurrentEnergy}点）");
        }

        // ── 费用惩罚：高费低收益牌 ───────────────────────────────────
        if (card.Cost >= 3 && baseScore < 6f) score *= 0.7f;

        // ── strike_synergy：连击流加成（手牌 Strike 类牌越多越值）──────
        if (card.Tags.Contains("strike_synergy"))
        {
            int strikeCount = snap.HandCards.Count(c => c.Tags.Contains("strike_synergy"));
            if (strikeCount > 1) score += (strikeCount - 1) * 0.5f;
        }

        return new CombatCardScore
        {
            Card      = card,
            HandIndex = handIndex,
            Score     = score,
            Reasons   = reasons,
        };
    }

    // ── 敌人警告 ──────────────────────────────────────────────────────

    private static List<EnemyWarning> BuildEnemyWarnings(CombatSnapshot snap)
    {
        var warnings = new List<EnemyWarning>();
        foreach (var enemy in snap.EnemyIntents)
        {
            if (!enemy.WillAttack) continue;

            int net = enemy.AttackDamage - snap.CurrentBlock;
            string warn = net > 0
                ? $"{enemy.EnemyName}将造成 {enemy.AttackDamage} 伤害（穿透 {net}）"
                : $"{enemy.EnemyName}将造成 {enemy.AttackDamage} 伤害（格挡抵消）";

            warnings.Add(new EnemyWarning
            {
                EnemyName    = enemy.EnemyName,
                AttackDamage = enemy.AttackDamage,
                WillPierce   = net > 0,
                WarningText  = warn,
            });
        }
        return warnings;
    }

    /// <summary>
    /// cards.json 中无记录时，根据稀有度+类型+费用推断基础分，
    /// 保证稀有牌比普通牌得分高，避免所有未知牌均为 5f。
    /// </summary>
    private static float InferBaseScore(CombatCardInfo card)
    {
        float score = card.Rarity switch
        {
            "Rare"    => 8f,
            "Uncommon"=> 6f,
            "Ancient" => 9f,
            "Basic"   => 3f,
            _         => 4f,  // Common / Status / Token 等
        };

        score += card.CardType switch
        {
            "Power" =>  1f,
            "Skill" =>  0f,
            _       => -0.5f, // Attack
        };

        // 高费低收益惩罚
        if (card.Cost >= 3) score -= 1f;

        return Math.Max(score, 1f);
    }

    private static string BuildSummaryText(CombatAdvice advice, CombatSnapshot snap)
    {
        int totalIncoming    = snap.EnemyIntents.Sum(e => e.AttackDamage);
        float incomingMult   = snap.PlayerVulnerable > 0 ? 1.5f : 1.0f;
        int effectiveIncoming = (int)(totalIncoming * incomingMult);
        int netDamage        = effectiveIncoming - snap.CurrentBlock;
        var topCard          = advice.CardAdvices.FirstOrDefault(c => c.PlayOrder == 1);

        // 状态提示前缀
        var statusParts = new List<string>();
        if (snap.PlayerStrength > 0)                            statusParts.Add($"力量+{snap.PlayerStrength}");
        if (snap.PlayerWeak > 0)                                statusParts.Add($"虚弱×{snap.PlayerWeak}");
        if (snap.PlayerVulnerable > 0)                         statusParts.Add($"易伤×{snap.PlayerVulnerable}");
        if (snap.PlayerThorns > 0)                              statusParts.Add($"荆棘{snap.PlayerThorns}");
        int enemyVuln = snap.GetEnemyPowerTotal("VULNERABLE");
        if (enemyVuln > 0)                                      statusParts.Add($"敌易伤×{enemyVuln}");
        string statusStr = statusParts.Count > 0 ? $"[{string.Join(" ", statusParts)}] " : "";

        if (netDamage >= snap.CurrentHP)
            return $"{statusStr}🔴致命！{effectiveIncoming}伤害穿透{netDamage}，必须防御";

        if (netDamage > snap.CurrentHP * 0.4f)
            return $"{statusStr}⚠ 危险！穿透{netDamage}，优先防御";

        if (topCard != null)
            return $"{statusStr}建议先出：{topCard.Card.CardNameZh}";

        return snap.CurrentEnergy == 0 ? "能量耗尽，结束回合" : "分析中…";
    }
}

// ── 数据类 ────────────────────────────────────────────────────────────

/// <summary>战斗快照：由 CombatHook 在每次回合开始/出牌后实时构建</summary>
public class CombatSnapshot
{
    public List<CombatCardInfo>  HandCards          { get; set; } = new();
    public int                   CurrentEnergy      { get; set; }
    public int                   MaxEnergy          { get; set; }
    public int                   CurrentHP          { get; set; }
    public int                   MaxHP              { get; set; }
    public int                   CurrentBlock       { get; set; }
    public List<EnemyIntentInfo> EnemyIntents       { get; set; } = new();
    public int                   RoundNumber        { get; set; }

    // ── 牌堆状态（影响 X 费牌/循环/消耗评分）──────────────────────────
    /// <summary>抽牌堆剩余张数</summary>
    public int DrawPileCount       { get; set; }
    /// <summary>弃牌堆张数（接近0时下回合需洗牌）</summary>
    public int DiscardPileCount    { get; set; }
    /// <summary>消耗牌堆张数</summary>
    public int ExhaustPileCount    { get; set; }
    /// <summary>本回合已出牌张数（影响 combo 计数类卡牌）</summary>
    public int CardsPlayedThisTurn { get; set; }
    /// <summary>当前手牌中是否还有防御类牌（避免重复推荐防御）</summary>
    public bool HasBlockInHand     { get; set; }
    /// <summary>当前手牌中是否有强化（Power）牌</summary>
    public bool HasPowerInHand     { get; set; }

    // ── 运行时 Buff/Debuff（遗物效果已反映在这里）─────────────────────
    /// <summary>玩家当前所有 Power，遗物效果体现为对应数值（如力量之戒 → Strength.Amount）</summary>
    public List<PowerInfo> PlayerPowers { get; set; } = new();
    /// <summary>所有存活敌人当前所有 Power（虚弱/易伤等）</summary>
    public List<EnemyPowerInfo> EnemyPowers { get; set; } = new();

    // ── 便捷查询（避免 Advisor 内部重复遍历）─────────────────────────
    /// <summary>玩家当前力量值（0 = 无力量）</summary>
    public int PlayerStrength   => PlayerPowers.FirstOrDefault(p => p.PowerId == "STRENGTH")?.Amount ?? 0;
    /// <summary>玩家当前虚弱层数（影响攻击输出）</summary>
    public int PlayerWeak       => PlayerPowers.FirstOrDefault(p => p.PowerId == "WEAKENED")?.Amount ?? 0;
    /// <summary>玩家当前易伤层数（被攻击时受到更多伤害）</summary>
    public int PlayerVulnerable => PlayerPowers.FirstOrDefault(p => p.PowerId == "VULNERABLE")?.Amount ?? 0;
    /// <summary>玩家荆棘值（敌人攻击时反伤，防御牌间接产生反伤收益）</summary>
    public int PlayerThorns     => PlayerPowers.FirstOrDefault(p => p.PowerId == "THORNS")?.Amount ?? 0;
    /// <summary>玩家当前愤怒层数（出攻击牌获得力量）</summary>
    public int PlayerEnrage     => PlayerPowers.FirstOrDefault(p => p.PowerId == "ENRAGE")?.Amount ?? 0;

    /// <summary>取任意敌人的某个 Power 之和（多敌人时汇总）</summary>
    public int GetEnemyPowerTotal(string powerId) =>
        EnemyPowers.Where(e => e.PowerId == powerId).Sum(e => e.Amount);
}

/// <summary>玩家单个 Power 快照（PowerModel 运行时数据）</summary>
public class PowerInfo
{
    public string PowerId   { get; set; } = string.Empty;
    /// <summary>本地化名称（如"力量"）</summary>
    public string NameZh    { get; set; } = string.Empty;
    public int    Amount    { get; set; }
    /// <summary>是否为负面效果</summary>
    public bool   IsDebuff  { get; set; }
}

/// <summary>敌人 Power 快照（多敌人时每条记录含所属敌人名）</summary>
public class EnemyPowerInfo
{
    public string EnemyName { get; set; } = string.Empty;
    public string PowerId   { get; set; } = string.Empty;
    public string NameZh    { get; set; } = string.Empty;
    public int    Amount    { get; set; }
    public bool   IsDebuff  { get; set; }
}

public class CombatCardInfo
{
    public string       CardId      { get; set; } = string.Empty;
    public string       CardNameZh  { get; set; } = string.Empty;
    public string       CardType    { get; set; } = string.Empty;  // Attack/Skill/Power
    public int          Cost        { get; set; }
    public List<string> Tags        { get; set; } = new();
    /// <summary>稀有度字符串：Basic/Common/Uncommon/Rare/Ancient 等</summary>
    public string       Rarity      { get; set; } = "Common";
}

public class EnemyIntentInfo
{
    public string EnemyName    { get; set; } = string.Empty;
    public bool   WillAttack   { get; set; }
    public int    AttackDamage { get; set; }
    public string IntentDesc   { get; set; } = string.Empty;
}

/// <summary>单张手牌的出牌建议评分</summary>
public class CombatCardScore
{
    public CombatCardInfo Card      { get; set; } = new();
    public int            HandIndex { get; set; }
    public float          Score     { get; set; }
    /// <summary>建议出牌顺序（1=优先，0=不建议）</summary>
    public int            PlayOrder { get; set; }
    public List<string>   Reasons   { get; set; } = new();
}

public class CombatAdvice
{
    public List<CombatCardScore> CardAdvices    { get; set; } = new();
    public List<EnemyWarning>    EnemyWarnings  { get; set; } = new();
    public string                SummaryText    { get; set; } = string.Empty;
}

public class EnemyWarning
{
    public string EnemyName    { get; set; } = string.Empty;
    public int    AttackDamage { get; set; }
    public bool   WillPierce   { get; set; }
    public string WarningText  { get; set; } = string.Empty;
}
