# 「星象仪」(Astrolabe) — 开发进度与规划

**版本**: v0.4  
**更新日期**: 2026-03-15  

---

## 一、项目概览

「星象仪」是一个为《杀戮尖塔2》(Slay the Spire 2) 制作的实时决策顾问 Mod。  
游戏基于 Godot 4.5.1 C# (.NET 9.0) 构建，Mod 使用 HarmonyLib 进行运行时 Hook。

**核心定位**：以 HUD 形式浮于游戏表面，在每个关键决策节点给出带理由的推荐，涵盖战斗出牌、选牌、地图规划、商店购买等场景。

---

## 二、已完成内容

### 2.1 基础设施

| 模块 | 文件 | 状态 |
|------|------|------|
| Mod 入口 | `ModEntry.cs` | ✅ 完成 |
| HarmonyLib 注册 | `ModEntry.cs` | ✅ 完成 |
| PCK 生成与部署 | `build.ps1` + Godot 导出 | ✅ 完成 |
| Mod 在游戏内正常显示 | Settings → Mods | ✅ 验证通过 |
| 数据加载器 | `Data/DataLoader.cs` | ✅ 完成 |
| 数据模型 | `Data/Models.cs` | ✅ 完成 |

### 2.2 游戏数据（静态 JSON）

| 文件 | 内容 | 状态 |
|------|------|------|
| `data/cards.json` | 战士全卡（~75张）含 `base_score`/`tier`/`synergy_tags`/流派评分 | ✅ 完成（已校正评分）|
| `data/relics.json` | 遗物数据（部分覆盖） | ⚠️ 部分完成 |
| `data/buildpaths.json` | 4条流派路线定义（力量战/无限流/格挡流/燃烧流） | ✅ 完成 |
| `data/bosses.json` | Boss 机制摘要 | ⚠️ 部分完成 |
| `data/events.json` | 事件选项数据 | ⚠️ 框架完成，数据待补充 |

> **数据来源**：通过 ILSpy 反编译 `sts2.dll` 获取，反编译文件存放在 `decompiled/` 目录。

### 2.3 Hook 层（游戏事件订阅）

| Hook | 文件 | 触发时机 | 状态 |
|------|------|----------|------|
| 选牌奖励 | `Hooks/CardRewardHook.cs` | 战斗结束后选牌界面 | ✅ 完成 |
| 地图界面 | `Hooks/MapScreenHook.cs` | 打开地图时 | ✅ 完成 |
| 战斗系统 | `Hooks/CombatHook.cs` | 回合开始/每次出牌后 | ✅ 完成 |
| 篝火界面 | `Hooks/CampfireHook.cs` | 进入篝火 | ✅ 框架完成 |
| 商店界面 | `Hooks/ShopHook.cs` | 进入商店 | ✅ 框架完成 |

**CombatHook 实时事件**：
- 订阅 `CombatManager.TurnStarted` / `TurnEnded`
- 订阅 `pcs.Hand.ContentsChanged` / `pcs.EnergyChanged` / `pcs.Hand.CardRemoved`
- 使用 `ProcessFrame` + `OneShot` 实现防抖动（debounce），避免每帧重算

### 2.4 引擎层（决策逻辑）

#### CombatAdvisor（战斗出牌建议）— 核心模块，已深度实现

**快照数据（CombatSnapshot）读取内容**：
- 手牌（CardId / 名称 / 类型 / 费用 / Tags / 稀有度）
- 当前能量 / 最大能量
- 当前 HP / 格挡
- 敌人意图列表（是否攻击 / 伤害数值）
- 抽牌堆 / 弃牌堆 / 消耗堆数量
- 本回合已出牌数
- **玩家 Powers**（力量/虚弱/易伤/荆棘/愤怒，通过 `Creature.Powers` 直接读取游戏运行时值，遗物效果已自动反映）
- **敌人 Powers**（虚弱/易伤等，支持多敌人汇总 `GetEnemyPowerTotal`）

**ScoreCard 评分维度**：

| 维度 | 实现方式 |
|------|----------|
| 威胁评估 | 三档：致命（净伤≥HP）/ 危险（净伤>HP×40%）/ 安全 |
| 玩家力量加成 | `strengthBonus = 1 + strength × 0.15` |
| 玩家虚弱惩罚 | 攻击牌 ×0.75 |
| 玩家易伤紧迫 | 防御牌额外 ×1.2，有效传入伤害 ×1.5 |
| 荆棘协同 | 防御牌让敌人多打次数 × 荆棘反伤 |
| 愤怒（Enrage）| 已出牌数越多攻击牌加成越大 |
| **敌人易伤** | 攻击牌 ×1.5，reason 标注"敌人易伤×N↑" |
| **敌人荆棘** | 多段命中牌（`multi_hit` tag）降分 |
| vulnerable_apply | 施加易伤的牌，后续每张攻击牌 +2 分 |
| strike_synergy | 手牌 Strike 类牌越多，互相加分 |
| 0 费技能 | 固定 +1.5（出牌无能量损耗） |
| Power 先出 | 攻击牌扣 2 分（Power 先出后续攻击受益更多） |
| X 费牌 | 排在最后，能量越多分越高 |
| 稀有度兜底 | `InferBaseScore`：Rare=8 / Uncommon=6 / Ancient=9 / Basic=3 |

**前向模拟（ForwardSimulate）**：
- ≤6 张可出牌时枚举全排列（最多 6!=720 种）
- `SimulateSequence` 建模每张牌的实际收益（Attack 伤害累积 / Block 格挡累积 / Power 后续力量提升）
- 取总收益最高序列分配 PlayOrder
- >6 张可出牌时退化为贪心排序（性能保障）

**Tag 打标系统（BuildCardTags）**：

| Tag | 来源 | 精度 |
|-----|------|------|
| `block` | `card.GainsBlock`（游戏原生虚方法） | ✅ 精确 |
| `draw` | `DynamicVars.ContainsKey("Cards")` | ✅ 精确 |
| `exhaust` | `card.Keywords.Contains(CardKeyword.Exhaust)` | ✅ 精确 |
| `buff` | `DynamicVars.ContainsKey("StrengthPower/DexterityPower")` | ✅ 精确（全职业通用） |
| `synergy_tags` | `cards.json` 手动标注（34种） | ⚠️ 部分标注 |

> **关键调研结论**：`PowerVar<T>` 的字典 key = `typeof(T).Name`，因此 `"StrengthPower"` 精确覆盖所有力量增益牌，无需字符串 ID 匹配。

#### CardAdvisor（选牌建议）

- 基于 `buildpaths.json` 的流派可行性评分
- 读取当前牌组，计算每张候选牌与各流派的协同系数
- 状态：✅ 基础框架完成，评分精度待提升

#### MapAdvisor（地图路线建议）

- 读取地图节点类型（精英/商店/篝火/问号）
- 结合当前 HP 和流派方案计算节点价值
- 状态：✅ 基础框架完成

#### BuildPathManager（流派方案管理）

- 维护最多 3 套并行方案（`PathState`）
- 每次选牌/选遗物后重新计算可行性
- 可行性 <20% 自动淡出
- 状态：✅ 核心逻辑完成

### 2.5 UI 层

| 组件 | 文件 | 状态 |
|------|------|------|
| HUD 容器 | `UI/OverlayHUD.cs` | ✅ 完成，`CanvasLayer` 注入正常 |
| 流派标签面板 | `UI/BuildPathPanel.cs` | ✅ 完成，位置已校正（不遮挡遗物） |
| 地图建议面板 | `UI/MapAdvicePanel.cs` | ✅ 完成 |
| 选牌建议面板 | `UI/CardAdvicePanel.cs` | ✅ 完成 |
| 战斗建议面板 | `UI/CombatAdvicePanel.cs` | ✅ 完成 |

**CombatAdvicePanel 细节**：
- 底部建议栏：显示出牌顺序（①防御 →②打击）+ 摘要文字
- 手牌徽章：每张手牌下方显示圆形序号（①②③），位置在手牌最下方
- 敌人警告：显示伤害数值和穿透量
- 玩家状态前缀：`[力量+N 易伤×N 敌易伤×N]`

### 2.6 关键技术调研结论

| 调研内容 | 结论 |
|---------|------|
| 奖励牌生成机制 | **纯随机**，无流派偏向，`CardFactory.CreateForReward` → `rng.NextItem(sameRarityCards)` |
| 遗物效果感知方式 | 通过读取 `Creature.Powers` 间接感知，已含遗物加成（如力量之戒 → StrengthPower.Amount） |
| `CardTag` 枚举范围 | 仅 6 个值（Strike/Defend/Minion/OstyAttack/Shiv），无 Buff/Block/Draw 等 |
| `DynamicVarSet` 结构 | 实现 `IReadOnlyDictionary<string,DynamicVar>`，支持 `ContainsKey` |
| buff 类牌识别方案 | `DynamicVars.ContainsKey("StrengthPower")` 全职业精确覆盖 |

---

## 三、当前已知问题 / 技术债

| 问题 | 严重度 | 备注 |
|------|--------|------|
| `relics.json` 覆盖率低 | 中 | 遗物效果对流派评分影响大，待补充 |
| `events.json` 数据未完整填充 | 低 | 事件 Advisor 暂未上线 |
| CombatHook 的 `TryGetAttackDamage` 边缘情况 | 低 | 多段攻击意图的总伤害计算可能不准 |
| cards.json 仅覆盖战士 | 中 | 其他职业上线需补充 |
| `SimulateSequence` 的 Power 效果建模简化 | 低 | 现仅建模 +2 力量（保守估算），DemonForm 等强力 Power 可能被低估 |
| synergy_tags 中 34 种 tag 只有 7 种被 ScoreCard 消费 | 中 | 数据已有但评分逻辑未全部接入 |

---

## 四、未来规划

### Phase A — 近期（下 2-3 个 session）

#### A1. CombatAdvisor 继续深化

- [ ] **`cards.json` baseScore 数据扩充**：当前仅覆盖战士，逐步补全其他职业
- [ ] **更多 synergy_tags 接入 ScoreCard**：
  - `burn_apply` + 手牌 `burn_scaling` 协同加分
  - `block_to_damage`（BodySlam 型）：当前格挡值越高得分越高
  - `x_cost`：已有基础，细化多能量时的曲线
  - `hp_cost`：当前 HP 充裕时加分（避免低血量出血牌）
- [ ] **SimulateSequence 精化**：DemonForm 等 Power 的真实力量增益量（读 `DynamicVars["StrengthPower"]`）
- [ ] **多敌人支持**：当前 `netDamage` 只汇总所有敌人，分别处理多敌人的意图优先级

#### A2. CardAdvisor 评分精化

- [ ] 补全当前牌组的「功能缺口」检测（缺防御/缺摸牌/缺过牌引擎）
- [ ] Act 权重：Act1 偏向通用牌，Act3 偏向高收益专精牌
- [ ] 删牌建议：商店界面识别「应该优先删的起始牌」

#### A3. 数据补全

- [ ] `relics.json` 补全常见遗物的流派评分和场景效果
- [ ] `events.json` 补全全部事件数据（约 50+ 个事件）
- [ ] `cards.json` 扩展至沉默者、机器人职业

---

### Phase B — 中期

#### B1. 商店建议（ShopAdvisor）

- 金币分配建议（买牌 vs 删牌 vs 遗物 vs 药水）
- 遗物评分：结合当前流派和 buildpaths.json 中的 `relic_priority`
- 删牌建议：识别「牌组密度过高」「基础打击/防御应优先删除」的情况

#### B2. 篝火建议（CampfireAdvisor）

- 休息 vs 升级决策
- 升级优先级排序（基于方案核心牌 + 升级后质变程度）
- 当前 `CampfireHook.cs` 框架已存在，需接入 BuildPathManager

#### B3. 事件建议（EventAdvisor）

- 接入 `events.json` 数据
- 风险型选项的期望值计算
- 遗物/卡牌依赖性检测

#### B4. UI 优化

- 面板折叠/展开（避免遮挡游戏内容）
- 透明度调节
- 建议原因的详细展开（悬停查看）

---

### Phase C — 长期

#### C1. 更多职业支持

- 沉默者（毒流 / 刺客 / Shiv 流）
- 机器人（能球流 / 充能流）
- 观察者（姿态流）
- 渎神者（新职业，待游戏正式发布数据）

#### C2. Run 总结分析

- 关键决策点回顾（「第3层篝火选升级，但建议休息」）
- 方案收束历程可视化
- 胜率估算曲线

#### C3. LLM 接入模式（可选）

- 将游戏状态序列化为结构化 JSON
- 对接本地 LLM（Ollama）或云端 API
- 覆盖规则引擎无法处理的复杂边缘 case
- 以对话气泡形式呈现自然语言建议

---

## 五、技术架构图

```
游戏进程
  └─ HarmonyLib Patch
       ├─ CombatHook          → CombatSnapshot → CombatAdvisor → CombatAdvicePanel
       ├─ CardRewardHook       → RunSnapshot    → CardAdvisor   → CardAdvicePanel
       ├─ MapScreenHook        → MapSnapshot    → MapAdvisor    → MapAdvicePanel
       ├─ CampfireHook         → RunSnapshot    → (CampfireAdvisor) → ...
       └─ ShopHook             → RunSnapshot    → (ShopAdvisor)     → ...
                                                      ↑
                                              BuildPathManager
                                              (维护3套并行方案 + 可行性评分)
                                                      ↑
                                              DataLoader
                                              (cards.json / relics.json /
                                               buildpaths.json / events.json)
```

---

## 六、文件结构

```
StS2mod/
├── GDD.md                    # 完整策划案
├── PROGRESS.md               # 本文件（进度 + 规划）
├── README.md                 # 快速入门
├── build.ps1                 # 构建 + 部署脚本
├── data/
│   ├── cards.json            # 卡牌数据（战士完整，其他职业待补）
│   ├── relics.json           # 遗物数据（部分）
│   ├── buildpaths.json       # 流派路线定义
│   ├── bosses.json           # Boss 机制（部分）
│   └── events.json           # 事件数据（框架）
├── decompiled/               # ILSpy 反编译产物（只读参考）
│   ├── all_cards_ns/sts2.decompiled.cs   # 完整反编译（~14MB）
│   └── ...                   # 各模块单独提取文件
└── src/Astrolabe/
    ├── ModEntry.cs           # Mod 入口
    ├── Core/                 # 游戏状态读取（RunStateReader / CombatStateReader）
    ├── Data/                 # 数据加载 + 模型
    ├── Engine/               # 决策引擎
    │   ├── AdvisorEngine.cs  # 总调度
    │   ├── BuildPathManager.cs  # 流派方案管理
    │   ├── CardAdvisor.cs    # 选牌建议
    │   ├── CombatAdvisor.cs  # 战斗出牌建议（核心）
    │   └── MapAdvisor.cs     # 地图路线建议
    ├── Hooks/                # HarmonyLib Hook
    └── UI/                   # HUD 组件（Godot CanvasLayer）
```

---

*最后更新：2026-03-15*
