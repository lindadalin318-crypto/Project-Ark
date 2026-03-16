# 杀戮尖塔2 Mod 策划案
# 「星象仪」(Astrolabe) — 智能决策顾问

**版本**: v0.3  
**日期**: 2026-03-14  
**作者**: 预研阶段

---

## 一、核心愿景

### 一句话定位

> 「星象仪」是一个实时顾问 Mod，它在玩家做出每一个关键决策时，以 HUD 形式浮于游戏表面，通过箭头和说明呈现当前情境下 **最多 3 套并行的最优构筑路线**，并随玩家的自主选择逐渐收束为最终方案。

### 设计哲学

- **教练而非作弊**：不自动帮玩家做选择，只展示「推荐」和「为什么」
- **多路线并行**：一个 Run 的早期提供多套强力构筑方向，玩家越往后走，方案越收束——这是本 Mod 的核心特色
- **上下文感知**：建议基于当前整个跑分状态（牌组、遗物、HP、层数、手牌、敌人意图），而非孤立的卡牌评分
- **可解释性优先**：每条建议都附带简短理由，帮助玩家理解策略逻辑，而非盲从
- **实时响应**：战斗内每出一张牌后立即重算，地图/选牌/商店界面打开时立即呈现

### ⚠️ 核心设计边界（v0.3 澄清）

**多方案建议（3套流派）仅在以下场景显示**：
- 战斗结束选牌（Card Reward）
- 事件中选牌/选遗物
- 商店中购买牌/遗物

**战斗场景仅显示单一最优解**：
- 战斗中不做流派判断，不展示多方案对比
- 只根据**当前实际牌组**，给出本回合最优出牌顺序
- 目标是「用手里的牌打出最好效果」，而非「这张牌符合哪个流派」
- 左上角流派标签（力量战/无限流）在战斗中保留，作为路线提醒参考，但不参与战斗建议逻辑

---

## 二、功能范围（按优先级排列）

### Phase 1 — MVP（核心功能，最高优先级）

#### 1.1 选牌建议（Card Reward Advisor）

**触发时机**：战斗结束后，卡牌奖励界面弹出时。

**核心机制**：多方案并行展示（详见第三章）

**显示内容**：
- 顶部显示当前活跃的 **1~3 套方案**（方案卡片，可折叠）
- 每张可选卡牌上叠加各方案的推荐标签：`[方案A ★]` / `[方案B ★]` / `[跳过]`
- 同一张牌可能对不同方案有不同评级
- 悬停卡牌时，弹出该牌在所有方案下的详细分析

**理由面板示例**：
```
「旋风斩」在各方案中的评价：
────────────────────────────────────
[方案A · 力量战]    ★★★ 核心牌  ← 选此方案必选
  理由：旋风斩 × 力量之戒 = 爆发核心，AOE 补充
  
[方案B · 无限流]    ✗ 不适合
  理由：高能量消耗与无限循环冲突
  
[方案C · 防御反伤]  ✓ 可选（非核心）
  理由：提供一定输出但非构筑重点
────────────────────────────────────
```

**评估依据**（规则引擎）：
1. 当前已有卡牌 → 各方案可行性评分
2. 候选牌与各方案的协同系数
3. 当前牌组缺少的功能（伤害/防御/过牌/消耗）
4. 卡牌社区胜率评分（来自深度调研的 cards.json）
5. 当前层数适配性（Act 1 不推荐高消耗复杂卡）
6. 距离下一个商店/篝火的层数（影响当前选择的「可纠错空间」）

#### 1.2 地图路线规划（Map Route Advisor）

**触发时机**：打开地图界面时（实时规划，始终可见）。

**核心机制**：地图上同时展示 1~3 条并行推荐路线，每条对应一套构筑方案。

**显示内容**：
- 每条推荐路线用不同颜色高亮连线（方案A绿色 / 方案B蓝色 / 方案C黄色）
- 每个节点上显示对应方案的推荐图标（🟢🔵🟡）
- 方案已收束为1套时，只显示1条推荐路线
- 右侧常驻「路线顾问面板」，显示各方案的路线逻辑说明

**节点评分因子**：
| 节点类型 | 评估逻辑 |
|----------|----------|
| 精英战 | 根据当前 HP + 牌组强度评估可打性；高 HP + 强牌组 → 推荐 |
| 商店 | 根据金币量 + 牌组删牌需求 + 当前方案所需关键遗物决定优先级 |
| 篝火 | 根据当前 HP 和关键牌升级需求决定「休息」vs「升级」倾向 |
| 问号事件 | 提示当前层数的平均事件价值（正面事件率 ~40%） |
| Boss | 展示 Boss 机制摘要 + 当前各方案的针对性评估 |

**路线评分逻辑**：
```
路线总分 = Σ(节点期望价值 × 方案权重) × HP可持续系数

HP可持续系数：
  HP > 70%：1.2（可以冒险打精英）
  HP 40-70%：1.0（正常评估）
  HP < 40%：0.6（精英战大幅降权，商店/篝火升权）
```

#### 1.3 篝火决策建议（Campfire Advisor）

**触发时机**：进入篝火界面时。

**显示内容**：
- 推荐操作旁显示 ★ 标记 + 简短理由
- 若推荐升级，明确指出推荐升级哪张牌（基于各方案的核心牌）

**决策逻辑**：
```
if HP < MaxHP × 0.4:
    → 推荐「休息」（恢复 30% HP）
elif 当前方案存在升级后质变的核心牌:
    → 推荐「升级」+ 指出推荐牌（如「歼灭」「旋风斩」）
else:
    → 推荐「休息」（保值）
```

**升级优先级排序**（基于当前方案）：
1. 方案核心输出卡（升级后质变的卡，如「歼灭」「毒刺」）
2. 方案核心防御卡（「格挡+」「壁垒」等）
3. 过牌引擎（「过激」「战斗突进」等）
4. 其他

---

### Phase 2 — 进阶功能（中优先级）

#### 2.1 商店购买建议（Shop Advisor）

**触发时机**：进入商店界面。

**显示内容**：
- 每件商品（卡牌/遗物/药水）旁显示推荐评级
- 各方案各自的「购买优先级」排序
- 金币分配建议框（综合考虑当前所有商品）

**金币分配建议框示例**：
```
当前金币：238
当前方案：[方案A · 力量战] [方案B · 无限流]
─────────────────────────────────────────
[方案A 推荐购买顺序]
  ① 删除「防御」(-75g)    ← 牌组密度优化，强烈推荐
  ② 购买「战斗专注」(-150g) ← 力量战核心，必拿
  剩余：13g → 建议保留

[方案B 推荐购买顺序]
  ① 删除「打击」(-75g)    ← 无限流牌组精简
  ② 购买「感化」(-120g)   ← 消耗引擎
  剩余：43g → 可考虑买药水
─────────────────────────────────────────
```

#### 2.2 事件选择建议（Event Advisor）

**触发时机**：进入问号事件界面。

**显示内容**：
- 每个事件选项旁显示推荐评级 + 各方案的不同倾向
- 对于风险型选项（如「拿遗物但失血」），显示期望值计算
- 遗物依赖型选项：检测玩家是否持有相关遗物

**事件数据库覆盖**：
- 所有已知事件的选项效果（精确或概率）
- 每个选项的期望价值评分
- 风险标注（HP损失 / 负面 Debuff / 牌组污染）
- 遗物/卡牌依赖性标注

#### 2.3 战斗内走牌建议（Combat Advisor）

> 📌 Phase 2 实现，不在 MVP 范围内。

**设计原则**：战斗顾问与多方案系统完全解耦。战斗中只问「现在怎么打最好」，不问「这符合哪个流派」。

**触发时机**：
- 每个回合开始时（给出本回合最优出牌顺序建议）
- 每出一张牌后（实时重算剩余手牌的最优顺序）

**显示内容**：
- 手牌下方显示出牌顺序编号徽章（①②③... / ✗不建议）
- 底部 HUD 栏显示：
  ```
  ⚠ 敌人将造成 14 伤害（穿透 6）   出牌: ①防御 → ②打击 → ③重击   建议先出：防御
  ```

**决策评估维度**：
1. 能否本回合击杀（最高优先级）
2. 敌人下回合伤害是否超过当前 HP / 格挡（防御紧急度，三级：致命/危险/安全）
3. 出牌顺序耦合：Power 牌先出（后续攻击受益）、X 费牌最后出（消耗剩余全部能量）
4. 抽牌堆/弃牌堆状态（摸牌牌在牌堆快耗尽时优先级提升）
5. 本回合已出牌数（combo 类卡牌联动加成）

**不在战斗中显示的内容**：
- ❌ 多方案对比（力量战 vs 无限流 vs 通用）
- ❌ 流派归属标注
- ❌ 长期构筑建议

**数据读取需求**：
- 玩家当前能量 / 最大能量
- 当前手牌（完整列表，含费用/类型/标签）
- 抽牌堆数量 / 弃牌堆数量 / 消耗牌堆数量
- 本回合已出牌数
- 当前格挡值 / HP
- 敌人：当前意图 / 意图伤害数值

---

### Phase 3 — 长期扩展（低优先级）

#### 3.1 跑分总结分析（Run Analyzer）

跑分结束后，展示：
- 关键决策点回顾（「第3层篝火选了升级，但建议是休息」）
- 各方案收束历程（「方案B在第8层因缺少关键遗物被淘汰」）
- 胜率估算曲线（每个决策点后的胜率变化）
- 本局最优/最差决策 Top 5

#### 3.2 LLM 接入模式（AI 顾问增强）

- 将游戏状态序列化为结构化 JSON
- 发送给本地 LLM（Ollama）或云端 API（Claude/GPT）
- LLM 负责复杂情境的自然语言分析（规则引擎覆盖不到的边缘 case）
- 以对话气泡形式显示 AI 的建议和解释
- 延迟目标：< 3 秒（本地）/ < 2 秒（云端）

---

## 三、核心特色：多方案并行 + 收束机制

> 这是「星象仪」区别于所有现有辅助 Mod 的核心设计，必须优先实现。

### 3.1 什么是「方案」

一个「方案」(BuildPath) 是：
- 一个**构筑流派标签**（如「力量战」「无限流」「毒流」「防御反伤」）
- 一套**核心卡牌列表**（该流派的必要卡牌）
- 一套**遗物优先级**（该流派最受益的遗物）
- 一套**地图路线偏好**（如「毒流需要商店删基础牌」「力量战可以多打精英」）
- 一个**当前可行性分数**（0-100，基于现有牌组与该方案的匹配度）

### 3.2 方案数量的动态变化

```
Run 开始（第 1 层）：
  → 呈现 3 套方案（该职业最常见的 3 个强力构筑方向）
  → 每套方案可行性初始相近

随着玩家选择：
  → 每次选牌/选遗物后，重新计算各方案可行性
  → 可行性 < 20% 的方案自动「淡出」（标灰，不再主动推荐）
  → 可行性 > 70% 的方案进入「确立」状态（高亮强调）

Run 后期（第 3 幕）：
  → 通常已收束为 1-2 套方案
  → 若仍有 2 套，以更高可行性者为主推
```

### 3.3 方案可行性评分

```
BuildPath.Viability = (
    CoreCardMatch × 0.5   // 已有核心牌数量 / 总核心牌数量
  + SynergyRelics × 0.3   // 持有协同遗物数量 / 理想遗物数量
  + DeckShape    × 0.2    // 牌组形态与方案要求的匹配度
) × 100
```

### 3.4 方案内冲突检测

当玩家的选择同时契合多套方案时，检测两方案是否存在**核心冲突**：
```
示例冲突：
  「无限流」需要精简牌组（< 15 张）
  「机器人充能流」需要保留卡牌以充能球
  → 若玩家牌组已有 20+ 张，无限流可行性大幅下降
```

### 3.5 「方案卡片」UI 组件

界面顶部常驻区域（可折叠），展示所有活跃方案：

```
┌──────────────────────────────────────────────────────┐
│ [方案A · 力量战]  ████████░░ 80%   ▲ 上升           │
│ [方案B · 无限流]  █████░░░░░ 48%   → 持平           │
│ [方案C · 防御反伤] ██░░░░░░░ 22% 🔘 逐渐淡出        │
└──────────────────────────────────────────────────────┘
```

---

## 四、技术架构

### 4.1 整体架构图

```
┌─────────────────────────────────────────────────────────────┐
│                      游戏进程 (STS2)                          │
│  ┌──────────────┐    ┌─────────────────────────────────┐    │
│  │  游戏逻辑    │    │         Astrolabe Mod            │    │
│  │  (sts2.dll)  │◄───│  ┌─────────────────────────┐   │    │
│  │              │    │  │   Harmony Hook Layer     │   │    │
│  │  CardReward  │    │  │  (界面Open / 出牌 / 抽牌) │   │    │
│  │  MapScreen   │    │  └────────────┬────────────┘   │    │
│  │  ShopScreen  │    │               │                 │    │
│  │  EventScreen │    │  ┌────────────▼────────────┐   │    │
│  │  CombatState │    │  │   Game State Reader     │   │    │
│  │  RunData     │    │  │  RunSnapshot +          │   │    │
│  └──────────────┘    │  │  CombatSnapshot (实时)  │   │    │
│                       │  └────────────┬────────────┘   │    │
│                       │               │                 │    │
│                       │  ┌────────────▼────────────┐   │    │
│                       │  │   Multi-Path Advisor    │   │    │
│                       │  │  ┌────────────────────┐ │   │    │
│                       │  │  │ BuildPath Manager  │ │   │    │
│                       │  │  │ (方案可行性计算)    │ │   │    │
│                       │  │  └────────────────────┘ │   │    │
│                       │  │  ┌────────────────────┐ │   │    │
│                       │  │  │ Rule Engine        │ │   │    │
│                       │  │  │ (cards/relics/      │ │   │    │
│                       │  │  │  events/bosses.json)│ │   │    │
│                       │  │  └────────────────────┘ │   │    │
│                       │  └────────────┬────────────┘   │    │
│                       │               │                 │    │
│                       │  ┌────────────▼────────────┐   │    │
│                       │  │   Overlay HUD Layer     │   │    │
│                       │  │  (Godot CanvasLayer)    │   │    │
│                       │  │  方案卡片 / 箭头 / 标签  │   │    │
│                       │  └─────────────────────────┘   │    │
│                       └─────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────┘
```

### 4.2 技术栈

| 组件 | 技术 |
|------|------|
| 开发语言 | C# (.NET 9.0) |
| 引擎 | Godot 4.5.1 Mono |
| 运行时补丁 | Harmony 2（游戏自带 `0Harmony.dll`） |
| Mod 框架 | 纯 Harmony（不强依赖 BaseLib，降低版本耦合） |
| UI 层 | Godot `CanvasLayer`（始终置顶，覆盖全屏） |
| 数据存储 | JSON 文件（cards / relics / events / bosses / buildpaths） |
| 构建工具 | Visual Studio / .NET CLI |
| 可选：LLM 接口 | HttpClient → Ollama REST API（Phase 3） |

### 4.3 关键 Harmony Hook 点

```csharp
// 以下为伪代码，实际类名需通过反编译 sts2.dll 确认

// ── 战斗外 Hook ──────────────────────────────────────

// 卡牌奖励界面弹出
[HarmonyPatch(typeof(CardRewardScreen), "Open")]
static void Postfix(CardRewardScreen __instance, List<CardData> cards)
{
    var snapshot = RunStateReader.Capture();
    BuildPathManager.UpdateViability(snapshot);
    OverlayHUD.ShowCardRewardAdvisor(cards, BuildPathManager.ActivePaths);
}

// 地图界面打开
[HarmonyPatch(typeof(MapScreen), "Open")]
static void Postfix(MapScreen __instance)
{
    var snapshot = RunStateReader.Capture();
    OverlayHUD.ShowMapAdvisor(MapState.Current, BuildPathManager.ActivePaths, snapshot);
}

// 商店界面打开
[HarmonyPatch(typeof(ShopScreen), "Open")]
static void Postfix(ShopScreen __instance)
{
    var snapshot = RunStateReader.Capture();
    OverlayHUD.ShowShopAdvisor(ShopState.Current, BuildPathManager.ActivePaths, snapshot);
}

// 篝火界面打开
[HarmonyPatch(typeof(CampfireScreen), "Open")]
static void Postfix(CampfireScreen __instance)
{
    var snapshot = RunStateReader.Capture();
    OverlayHUD.ShowCampfireAdvisor(snapshot, BuildPathManager.ActivePaths);
}

// ── 战斗内 Hook（Phase 2）────────────────────────────

// 每次出一张牌后（实时重算）
[HarmonyPatch(typeof(CardUseAction), "Use")]
static void Postfix(CardUseAction __instance)
{
    var combatSnapshot = CombatStateReader.Capture();
    var advice = CombatAdvisor.Recalculate(combatSnapshot);
    OverlayHUD.UpdateCombatAdvisor(advice);
}

// 每个回合开始时
[HarmonyPatch(typeof(GameActionManager), "GetNextAction")]
static void Postfix() // 需确认具体方法名
{
    if (CombatStateReader.IsPlayerTurnStart())
    {
        var combatSnapshot = CombatStateReader.Capture();
        OverlayHUD.ShowTurnStartAdvice(CombatAdvisor.AnalyzeTurn(combatSnapshot));
    }
}
```

### 4.4 游戏状态快照

#### RunSnapshot（跑分全局状态）

```csharp
public class RunSnapshot
{
    // 基础状态
    public int CurrentHP;
    public int MaxHP;
    public int Gold;
    public int Floor;
    public int Act;

    // 牌组
    public List<CardData> Deck;       // 完整牌库
    public List<CardData> DrawPile;   // 当前抽牌堆（战斗中）
    public List<CardData> DiscardPile;// 弃牌堆（战斗中）

    // 遗物 / 药水
    public List<RelicData> Relics;
    public List<PotionData> Potions;

    // 地图
    public EnemyData ActBoss;
    public List<MapNode> AvailableNodes;
    public MapNode CurrentNode;
}
```

#### CombatSnapshot（战斗内实时状态）

```csharp
public class CombatSnapshot
{
    // 玩家战斗状态
    public int Energy;              // 当前能量
    public int MaxEnergy;
    public int CurrentHP;
    public int CurrentBlock;
    public List<CardData> Hand;     // 当前手牌
    public List<BuffData> PlayerBuffs;

    // 敌人状态（支持多敌人）
    public List<EnemyState> Enemies;
}

public class EnemyState
{
    public string EnemyId;
    public string EnemyName;
    public int HP;
    public int MaxHP;
    public int Block;
    public EnemyIntent Intent;      // 当前意图（攻击/防御/Buff...）
    public int IntentDamage;        // 攻击意图的伤害数值
    public int IntentTimes;         // 攻击次数（多段攻击）
    public List<BuffData> Buffs;
}
```

---

## 五、顾问引擎设计

### 5.1 BuildPath 数据结构

每套构筑方案在 `buildpaths.json` 中定义：

```json
{
  "path_id": "ironclad_strength",
  "name": "力量战",
  "character": "ironclad",
  "description": "通过堆叠力量值实现指数级爆伤，核心是快速获取足够力量并保证出牌速度",
  "core_cards": ["limit_break", "whirlwind", "offering", "battle_trance"],
  "key_relics": ["paper_fury", "dead_branch", "red_skull"],
  "ideal_deck_size": { "min": 10, "max": 18 },
  "map_preference": {
    "elite_weight": 1.4,
    "shop_weight": 1.0,
    "campfire_upgrade_priority": ["limit_break", "whirlwind", "offering"]
  },
  "counters": {
    "boss_weaknesses": ["hexaghost"],
    "boss_strengths": ["the_guardian", "time_eater"]
  }
}
```

### 5.2 卡牌评分矩阵

每张卡牌的数据已拆为 `cards.core.json`（事实层）与 `cards.advisor.json`（顾问层）；下面示例主要展示顾问层字段：

```json

{
  "card_id": "whirlwind",
  "name": "旋风斩",
  "character": "ironclad",
  "base_score": 8.5,
  "tier": "S",
  "path_scores": {
    "ironclad_strength": 10,
    "ironclad_infinite": 2,
    "ironclad_defense": 5
  },
  "synergy_tags": ["strength", "aoe", "energy-scaling"],
  "anti_synergy_tags": ["fragile", "small-deck"],
  "act_scaling": { "act1": 0.8, "act2": 1.0, "act3": 1.1 },
  "upgrade_priority": 8,
  "upgrade_delta": "伤害翻倍，质变级升级",
  "notes_zh": "力量战核心输出，与力量之戒组合可实现千伤；无限流不适用；防御流偶尔带一张"
}
```

最终卡牌推荐分 = `path_scores[activePath] × act_scaling × deck_size_modifier × relic_bonus`

### 5.3 cards.core.json / cards.advisor.json 数据采集策略


> 数据采集阶段需联网深度调研，打包后运行时完全离线。

**数据来源（优先级排序）**：
1. **STS2 社区 Tier List**（Reddit r/slaythespire、Steam 讨论区）— 主观评级
2. **STS1 历史数据参照**（跨版本迁移规律）— 基线参考
3. **高端玩家攻略视频/文章**（Twitch 主播、YouTube 高阶教学）— 流派定义
4. **游戏内数值推算**（通过反编译确认精确数值）— 客观数值校正
5. **社区 Discord 的 STS2 攻略频道**— 版本 meta 跟踪

**采集字段**：
- 各职业各卡的 Tier 评级（S/A/B/C/D）
- 每张卡在各流派中的具体地位（核心/辅助/可带/不带）
- 升级优先级评分
- 常见误区（「这张牌看起来弱但实际上在XX流派里很强」）

### 5.4 遗物数据库

```json
{
  "relic_id": "paper_fury",
  "name": "纸质愤怒",
  "tier": "boss",
  "base_score": 9.0,
  "path_scores": {
    "ironclad_strength": 10,
    "ironclad_infinite": 6,
    "ironclad_defense": 4
  },
  "notes_zh": "力量战最强 Boss 遗物，每场战斗力量 +3，配合旋风斩爆伤极高"
}
```

### 5.5 Boss 数据库

```json
{
  "boss_id": "the_hexaghost",
  "name": "六魂幽灵",
  "act": 1,
  "core_mechanics": ["debuff_burn", "aoe_pattern"],
  "dangerous_to": ["ironclad_strength"],
  "weak_against": ["poison", "block_heavy"],
  "counter_cards": ["sentinel", "metallicize"],
  "notes_zh": "力量战在此 Boss 表现差，燃烧削弱输出；防御流或毒流更稳"
}
```

---

## 六、HUD 设计规范

### 6.1 设计原则

- **浮于游戏表面**：通过 Godot CanvasLayer 实现始终置顶的 HUD 层
- **箭头+说明**：关键推荐以箭头指向目标卡牌/节点，说明文字显示在旁边
- **非侵入式**：信息密度克制，不遮挡游戏核心内容
- **可折叠**：所有 HUD 面板均可一键折叠/展开（快捷键 `Tab` 默认切换）
- **颜色语言**（三方案对应三色体系）：
  - 方案 A：绿色 `#4CAF50`
  - 方案 B：蓝色 `#2196F3`
  - 方案 C：黄色 `#FFC107`
  - 警告/不推荐：红色 `#F44336`
  - 信息提示：灰色 `#9E9E9E`
- **信息层级**：
  - 一级（始终可见）：颜色高亮 + 图标
  - 二级（鼠标悬停）：简短标签 + 方案归属
  - 三级（点击展开）：完整理由面板

### 6.2 各界面 HUD 布局

#### 卡牌奖励界面

```
┌────────────────────────────────────────────────────────┐
│ [方案A · 力量战] ████████░░ 80%  [方案B · 无限流] ████░ 45% │  ← 常驻方案卡片
├────────────────────────────────────────────────────────┤
│              卡牌奖励                                   │
│   ┌──────────┐     ┌──────────┐     ┌──────────┐       │
│   │  旋风斩  │     │  感化    │     │  格挡    │       │
│   │  [A ★★★] │     │  [B ★★]  │     │  [A✓ B✓] │       │
│   │  ↑绿色框 │     │  ↑蓝色框 │     │          │       │
│   └──────────┘     └──────────┘     └──────────┘       │
│                                                         │
│  ┌────────────────── 顾问说明 ──────────────────────┐  │
│  │ [方案A] 推荐「旋风斩」— 力量战核心输出，与当前  │  │
│  │          力量之戒形成爆发组合                   │  │
│  │ [方案B] 推荐「感化」— 无限流消耗引擎，过牌核心  │  │
│  └───────────────────────────────────────────────┘  │
└────────────────────────────────────────────────────────┘
```

#### 地图界面

```
地图上叠加：
  绿色连线 + 🟢 图标 → 方案A推荐路线
  蓝色连线 + 🔵 图标 → 方案B推荐路线
  黄色连线 + 🟡 图标 → 方案C推荐路线

右侧常驻路线面板（可折叠）：
  ┌────────────────────────┐
  │ [路线规划]             │
  │ [方案A] 精英→精英→商店 │
  │   理由：力量战需要遗物  │
  │ [方案B] 问号→篝火→商店 │
  │   理由：删牌精简牌组    │
  └────────────────────────┘
```

#### 战斗内 HUD（Phase 2）

```
手牌上方叠加出牌顺序编号：① ② ③ ...

右侧浮动面板：
  ┌──────────────────────────────┐
  │ [本回合建议]                  │
  │ ① 战斗突进 → 抽2张牌         │
  │ ② 旋风斩   → 核心输出        │
  │ ③ 格挡     → 防御收尾        │
  │                              │
  │ 预计伤害：48  预计格挡：15    │
  │ ⚠ 敌人下回合攻击 24，需格挡  │
  └──────────────────────────────┘
```

#### 商店界面

```
每件商品旁显示标注（各方案颜色）：
  卡牌「战斗专注」  → [A ★★★] 力量战核心  ← 绿色标签
  遗物「纸质愤怒」  → [A ★★★] Boss 遗物推荐
  卡牌「感化」      → [B ★★] 无限流核心

底部金币分配建议框（可折叠）
```

### 6.3 UI 开关设置面板

```
[星象仪设置]
─────────────────────────────
[功能开关]
☑ 选牌建议
☑ 地图路线规划
☑ 篝火建议
☑ 商店建议（Phase 2）
☑ 事件建议（Phase 2）
☐ 战斗走牌建议（Phase 2）

[显示模式]
○ 完整模式（显示理由 + 方案卡片）
● 精简模式（仅显示颜色图标）
○ 隐藏（禁用全部）

[快捷键]
Tab → 折叠/展开所有 HUD 面板
```

---

## 七、数据库规划

### 7.1 文件结构

```
Astrolabe/
├── data/
│   ├── cards.core.json     // 全职业卡牌事实层 + 结构化效果字段
│   ├── cards.advisor.json  // 全职业卡牌顾问层 + 流派权重/协同先验
│   ├── relics.json         // 遗物评分 + 流派协同

│   ├── events.json         // 所有事件选项期望值
│   ├── bosses.json         // Boss 机制 + 针对建议
│   └── buildpaths.json     // 构筑方案定义（每个职业 3-4 套）
└── ...
```

### 7.2 buildpaths.json — 各职业初始方案池

**战士（Ironclad）**：
- 力量战（Strength Build）
- 无限流（Infinite Combo）
- 防御反伤（Block/Thorns）

**猎手（Hunter/Silent）**：
- 毒刀流（Poison）
- 无限华丽流（Finisher Infinite）
- 闪避流（Evade/Shiv）

**机器人（Defect）**：
- 冰球防御（Frost Orb）
- 闪电爆发（Lightning Burst）
- 混合球（All Orb）

**观者（Watcher）**：
- 神格流（Divinity Rush）
- 保留流（Retain）
- 奇迹循环（Miracle Cycle）

> 每个职业初始展示 3 套方案，随 Run 进展动态收束。

---

## 八、开发路线图

### 阶段一：技术验证（1-2 周）

**目标**：验证 Harmony Hook + 游戏状态读取 + CanvasLayer UI 叠加。

**产出**：
- [ ] 反编译 `sts2.dll`（ILSpy），整理关键类名列表
- [ ] 实现基础 Hook：卡牌奖励界面 Open 事件
- [ ] 实现 `RunSnapshot` 构建（牌组 + 遗物 + HP + 金币）
- [ ] 实现 `CombatSnapshot` 构建（手牌 + 能量 + 敌人意图）
- [ ] 在界面上叠加 Debug 文本标签验证 CanvasLayer 可行性

**成功标准**：卡牌奖励界面弹出时，屏幕某处显示「已读取：[牌组] 17张 / [遗物] 力量之戒 / [候选牌] 旋风斩、歼灭、格挡」。

### 阶段二：MVP 核心功能（3-4 周）

**目标**：实现选牌建议 + 地图规划 + 篝火建议，含多方案机制。

**产出**：
- [ ] `buildpaths.json` v1（战士全部方案 + 猎手全部方案）
- [ ] `cards.core.json` / `cards.advisor.json` v1（战士 + 猎手全卡，含深度调研评分）
- [ ] `BuildPathManager` 实现（方案可行性计算 + 收束逻辑）

- [ ] 选牌顾问 UI（多方案卡片 + 颜色标签 + 悬停面板）
- [ ] 地图规划 UI（多色连线 + 路线说明面板）
- [ ] 篝火建议 UI

**成功标准**：力量战流派下，选牌界面正确推荐旋风斩并标注力量战归属；同时无限流方案标注「不适合」。

### 阶段三：完善数据与 Phase 2（3-4 周）

**目标**：补全数据库，实现商店/事件建议。

**产出**：
- [ ] `cards.core.json` / `cards.advisor.json` 补全全职业（机器人 + 观者）

- [ ] `relics.json` v1（全遗物）
- [ ] `events.json` v1（全事件）
- [ ] `bosses.json` v1（全 Boss）
- [ ] 商店购买建议（含金币分配面板）
- [ ] 事件选择建议
- [ ] 设置面板（功能开关 + 显示模式）
- [ ] 中文/英文双语支持

### 阶段四：战斗内建议（Phase 2 续，2-3 周）

**目标**：实现战斗内走牌建议。

**产出**：
- [ ] `CombatAdvisor` 规则引擎（基于启发式评分，非穷举）
- [ ] 手牌出牌顺序编号 HUD
- [ ] 右侧战斗建议浮动面板
- [ ] 实时 Hook（每出一张牌后重算）

### 阶段五：发布与迭代

- [ ] 发布到 Nexus Mods（STS2 分区）
- [ ] 收集社区反馈，修正评分偏差
- [ ] 实验性 LLM 接入功能（Phase 3）

---

## 九、风险评估

| 风险 | 可能性 | 影响 | 应对方案 |
|------|--------|------|----------|
| 游戏更新导致类名变更，Hook 失效 | 中 | 高 | Hook 层与业务层解耦；提供版本号锁定说明 |
| `sts2.dll` 反编译后关键类名难以定位 | 中 | 高 | 参考 BaseLib-StS2 源码辅助定位；社区 Discord 求助 |
| Godot CanvasLayer 坐标系与游戏 UI 不一致 | 低 | 中 | 参考现有 STS2 mod 的 UI 实现；早期技术验证专门测试 |
| 战斗内建议的出牌顺序评估准确度不足 | 高 | 中 | 明确标注「参考建议，非绝对最优」；Phase 2 后期迭代 |
| cards.core.json / cards.advisor.json 数据量巨大，调研耗时高 | 高 | 中 | MVP 先覆盖战士 + 猎手，其他职业分批追加 |

| 多方案收束逻辑复杂，边缘 case 多 | 中 | 中 | 设定最低可行性阈值（< 20% 即淡出），简化逻辑 |
| 游戏开发商反对此类辅助 Mod | 低 | 高 | MegaCrit 对 mod 社区一向开放；「教练」定位非作弊 |

---

## 十、核心差异化

相比直接看攻略/Tier List，「星象仪」的价值在于：

1. **多路线并行**：不是「最优解」，而是「你的这个 Run 目前有 3 条强力路可以走，我帮你追踪每条路的可行性」
2. **实时收束感**：玩家能直观看到自己的选择如何让某条路越来越强、另一条路逐渐暗淡——这本身就是一种游戏反馈体验
3. **上下文感知**：不是「旋风斩是好牌」，而是「基于你当前牌组和遗物，旋风斩在方案A里是核心，在方案B里不适合」
4. **战斗实时指导（Phase 2）**：在最高频的决策场景（每回合出牌）给出实时建议，实打实降低操作门槛
5. **全局视角**：选牌时会考虑后续路线（「距商店还有 1 步，建议跳过选牌去商店删牌」）
6. **可学习性**：每条建议带理由，新手能从中学习策略逻辑

---

## 十一、参考资料

- [BaseLib-StS2 GitHub](https://github.com/Alchyr/BaseLib-StS2) — Mod 框架参考
- [ModTemplate-StS2 GitHub](https://github.com/Alchyr/ModTemplate-StS2) — Mod 项目模板
- [sts2_example_mod](https://github.com/lamali292/sts2_example_mod) — 示例 Mod（.NET 9 + Godot 4.5.1）
- [STS1 InfoMod2](https://github.com/casey-c/infomod2) — STS1 信息 Mod 参考实现
- [Harmony 2 文档](https://harmony.pardeike.net/) — 运行时补丁框架
- STS2 游戏路径：`F:\SteamLibrary\steamapps\common\Slay the Spire 2`
- 关键 DLL：`data_sts2_windows_x86_64\sts2.dll`（C# .NET 9，用 ILSpy 反编译）
- 日志路径：`%AppData%\Roaming\SlayTheSpire2\Player.log`
