# Astrolabe Canonical Spec

> 更新时间：2026-03-17  
> 适用范围：`SideProject/StS2mod/src/Astrolabe`、`SideProject/StS2mod/data`、`SideProject/StS2mod/Docs`

---

## 1. 文档目的

这份文档是 `Astrolabe` 当前阶段的**现役架构真相源**。

它负责定义：

- 当前已经接通、正在运行的主链是什么
- `ModEntry / Core / Data / Engine / Hooks / UI` 各自负责什么
- 哪些链路属于**现役（Live）**，哪些只是**过渡（Transitional）**，哪些还只是**计划（Planned）**
- 当后续继续扩展 `AdviceEnvelope`、`AdviceContextBus`、`RunReplay` 时，应该把逻辑落在哪一层

它**不负责**：

- 记录未来想做什么、排期顺序是什么（那是 `Astrolabe_PackWin_Engine_TODO.md` 的职责）
- 定义卡牌 / 遗物 / Boss / 资源路径的 canonical ID 规则（那是 `STS2_Asset_ID_System.md` 的职责）
- 保存游戏逆向分析过程（那是 `STS2_Unpack_Report.md` 的职责）

一句话说，这份文档回答的是：

> **现在 Astrolabe 到底是怎么接起来的，谁该改什么，不该去哪里塞逻辑。**

---

## 2. 与其他文档的关系

| 文档 | 角色 | 什么时候看它 |
| --- | --- | --- |
| `Docs/Astrolabe_CanonicalSpec.md` | 当前现役主链与职责边界的唯一真相源 | 你要判断“逻辑该放哪”“现在谁说了算”时 |
| `Docs/Astrolabe_PackWin_Engine_TODO.md` | 路线图与未来建设清单 | 你要决定“下一步做什么”时 |
| `Docs/STS2_Asset_ID_System.md` | StS2 运行时 ID / 资源路径 / 归一化规范 | 你要处理 card/relic/boss ID 与数据对齐时 |
| `Docs/STS2_Unpack_Report.md` | 反编译与解包参考资料 | 你要追源码、补 Hook、核对 Godot / STS2 结构时 |
| `README.md` | 新人快速上手入口 | 你要跑起来项目时 |
| `PROGRESS.md` | 阶段进展快照 | 你要回看项目演进历史时 |
| `GDD.md` | 产品目标、体验设计、功能愿景 | 你要理解“为什么做”时 |

**约束：**

- 当前现役主链、owner、职责边界，以本文件为准。
- 未来排期、优先级、未实现项，以 `Astrolabe_PackWin_Engine_TODO.md` 为准。
- ID 规范、路径规范、归一化写法，以 `STS2_Asset_ID_System.md` 为准。

---

## 3. 当前现役主链

## 3.1 Bootstrap 主链

当前初始化流程：

```text
ModEntry.Initialize
  -> DataLoader.LoadAll
  -> BuildPathManager.Initialize
  -> DecisionRecorder.Initialize
  -> Register Hooks (CardReward / Map / Campfire / DeckUpgrade / Shop / Combat)
  -> harmony.PatchAll
  -> OverlayHUD.Initialize
```

### 现役 owner

- `ModEntry`：启动总入口，只负责**装配**，不负责策略计算
- `DataLoader`：静态 JSON 数据加载与查询
- `BuildPathManager`：全局构筑路线状态
- `DecisionRecorder`：统一决策样本写盘
- 各 `Hook`：运行时界面接线
- `OverlayHUD`：HUD 注入与显示层协调

---

## 3.2 非战斗建议主链（现役）

当前非战斗主链应理解为：

```text
Screen Hook
  -> RunStateReader
  -> AdvisorEngine
       -> BuildPathManager.GetActivePaths
       -> SharedDecisionContext.Create
       -> Specific Advisor / Internal Logic
       -> Advice DTO
       -> DecisionRecordFactory -> DecisionRecorder
  -> OverlayHUD / DeckUpgradeHook
```

当前已接入的非战斗决策点：

- `CardReward`
- `Map`
- `Campfire`
- `UpgradeSelection`
- `Shop`

### 非战斗链路语义

- `Hook` 负责**拿到游戏界面/运行时对象**
- `RunStateReader` 负责**读游戏真实状态并做快照**
- `AdvisorEngine` 负责**统一进入口与埋点收口**
- 专项 advisor / 内部求解逻辑负责**算建议**
- `DecisionRecordFactory + DecisionRecorder` 负责**把建议沉淀为统一样本**
- `OverlayHUD` / `DeckUpgradeHook` 负责**把建议呈现给玩家**

---

## 3.3 战斗建议支线（现役）

当前战斗链路应理解为：

```text
CombatHook
  -> CombatStateReader / CombatSnapshot
  -> CombatAdvisor
  -> OverlayHUD.ShowCombatAdvice
```

### 战斗链路语义

- 战斗链路目前**独立于多路线构筑建议**
- 它回答的是“这回合现在怎么打更好”，不是“这张牌符合哪条构筑路线”
- 未来它会和 `RunStrategyState` 汇合，但**当前还没有统一总线**

---

## 3.4 Telemetry / 评测链（现役）

当前已经现役的评测基建：

```text
AdvisorEngine
  -> DecisionRecordFactory
  -> DecisionRecorder
  -> telemetry/decision-records/*.jsonl
```

### 当前已成立的事实

- `DecisionRecord` 已经是非战斗主链的**统一记录载体**
- `summary / why / confidence / risk / alternatives` 已经有第一版最小闭环
- 当前已经有：
  - 非战斗主链第一版 `AdviceEnvelope`
  - 非战斗建议生成时前置分配的 `traceId`
  - `CardReward / Campfire / UpgradeSelection / Shop` 的玩家选择回写
  - `Campfire -> DeckUpgrade` 的同 trace 跨界面桥接
- 当前还**没有**：
  - 全场景玩家选择回写
  - “玩家选择 -> 后续结果”闭环
  - `RunReplay` 样本回放

---

## 3.5 当前尚未现役的能力

以下名字可以在 TODO 中出现，但**不要误判为已经落地**：

- `AdviceContextBus`
- `RunStrategyState`
- `DecisionTrace`
- `RunReplay`
- `EventHook` / `EventAdvisor`
- `ShopAdvicePanel`
- 真实地图图搜索引擎
- 商店组合求解器
- 战斗求解器 v1

这些仍属于**计划能力**，不是当前真相源的一部分。

---

## 4. 模块职责边界

| 模块 | 当前 owner / 典型文件 | 负责什么 | 不负责什么 |
| --- | --- | --- | --- |
| `ModEntry` | `ModEntry.cs` | 启动装配、注册、初始化顺序 | 业务评分、UI 细节、数据推导 |
| `Core` | `RunStateReader.cs`、`CombatStateReader.cs`、`Snapshots.cs`、`DecisionRecord*.cs` | 快照读取、通用运行时模型、决策记录模型 | 具体某一界面的策略评分 |
| `Data` | `Models.cs`、`DataLoader.cs` | JSON 模型、查询、ID 归一化后的数据访问 | 运行时 UI 逻辑、战斗/地图策略 |
| `Engine` | `BuildPathManager.cs`、`AdvisorEngine.cs`、`CardAdvisor.cs`、`MapAdvisor.cs`、`SharedDecisionContext.cs`、`CombatAdvisor.cs` | 策略计算、跨模块推断、统一 advice 入口 | 直接操作 Godot UI、直接抓界面节点 |
| `Hooks` | `CardRewardHook.cs`、`MapScreenHook.cs`、`CampfireHook.cs`、`DeckUpgradeHook.cs`、`ShopHook.cs`、`CombatHook.cs` | 对接游戏事件、抓取运行时对象、把数据送进引擎/UI | 把复杂 heuristics 写死在 Hook 内 |
| `UI` | `OverlayHUD.cs`、各 `*Panel.cs` | HUD 注入、建议显示、面板切换 | 重新推导 BuildPath、重新计算建议 |
| `Docs` | 本文件、`TODO`、`STS2_Asset_ID_System.md` | 规范、路线图、参考资料沉淀 | 替代代码成为隐藏逻辑 |

---

## 5. 当前状态分类

## 5.1 Live（现役）

满足以下条件之一即可视为 `Live`：

- 已经在 `ModEntry.Initialize()` 中初始化或注册
- 已经被现役 Hook / HUD / Advisor 主链直接调用
- 已经产生产线级运行时行为或日志样本

当前主要 `Live` 对象：

- `ModEntry`
- `DataLoader`
- `RunStateReader` / `CombatStateReader`
- `BuildPathManager`
- `SharedDecisionContext`
- `AdvisorEngine`
- 第一版 `AdviceEnvelope`
- `CardAdvisor` / `MapAdvisor` / `CombatAdvisor`
- `DecisionRecordFactory` / `DecisionRecorder`
- `CardRewardHook` / `MapScreenHook` / `CampfireHook` / `DeckUpgradeHook` / `ShopHook` / `CombatHook`
- `OverlayHUD` 与现有已实现面板

## 5.2 Transitional（过渡态）

满足以下条件可视为 `Transitional`：

- 当前确实在工作，但只是为了给未来统一架构过桥
- 后续存在明确的收束/替换目标
- 若继续扩散，会提升系统混乱度

当前主要 `Transitional` 对象：

- `DeckUpgradeHook._pendingCards`
- `DeckUpgradeHook._pendingCampfireEnvelope`
- `CardRewardHook._pendingOptions`
- 其他 `Hook` 内短生命周期 `_pendingXxx` 缓存
- `SharedDecisionContext` 作为“统一上下文总线前的临时共享上下文”
- `CampfireHook -> DeckUpgradeHook` 这种跨界面桥接

**规则：** 这些过渡机制可以继续用，但不应无节制扩散。

## 5.3 Planned（计划中）

以下属于已经命名、但尚未成为现役真相源的计划能力：

- `AdviceContextBus`
- `RunStrategyState`
- `DecisionTrace`
- `RunReplay`
- `EventHook` / `EventAdvisor`
- `ShopAdvicePanel`
- 真实地图路径搜索
- 商店组合求解
- 战斗 solver

## 5.4 Reference（参考）

以下内容可以帮助理解项目，但不应被当成“当前现役架构真相”：

- `README.md`
- `PROGRESS.md`
- `GDD.md`
- `STS2_Unpack_Report.md`
- 代码内历史注释或 TODO 段落

---

## 6. 当前 Authority Matrix（谁说了算）

| 范围 | 唯一权威入口 | 禁止的并行写入者 / 误用 |
| --- | --- | --- |
| 运行时快照读取 | `RunStateReader` / `CombatStateReader` | Hook 自己拼一套平行快照；UI 反向读场景对象做业务判断 |
| 构筑路线状态 | `BuildPathManager` | 各 Advisor 各自维护一份分叉版活跃路径 |
| 非战斗统一进入口 | `AdvisorEngine` | Hook 直接跳过引擎各自算建议 |
| 非战斗共享推导上下文 | `SharedDecisionContext`（直到 `AdviceContextBus` 落地前） | 在多个 Hook 内继续堆 `_pendingXxx` 做长期上下文 |
| 决策日志 | `DecisionRecordFactory` + `DecisionRecorder` | 各模块各写各的私有日志格式 |
| HUD 注入与显示切换 | `OverlayHUD` | Hook 直接创建自己的散装 UI 根节点 |
| ID / canonical 归一化规则 | `STS2_Asset_ID_System.md` + `IdNormalizer` | 在单个 advisor 内临时再造一套 ID 写法 |

---

## 7. 实施规则

## 7.1 Hook 只负责“接”，不要负责“想”

Hook 层应该做的是：

- 找到正确的 STS2 / Godot 对象
- 把运行时状态转成快照或候选集
- 调用 `AdvisorEngine` 或 `CombatAdvisor`
- 把结果交给 `OverlayHUD` 或对应注入点

**不要**在 Hook 里继续堆：

- 复杂评分逻辑
- 长生命周期全局状态
- 多模块共享策略判断

## 7.2 Engine 负责决策，UI 负责表达

- `Engine` 的输出应该是 advice / record / strategy data
- `UI` 只消费这些结果并负责呈现
- 不允许让 `OverlayHUD` 重新推导 BuildPath、重新猜风险、重新做排序

## 7.3 新的共享上下文，优先收束到统一入口

在 `AdviceContextBus` 尚未落地前：

- 非战斗共享推导优先收束到 `SharedDecisionContext`
- 只有**纯界面切换所需的极短生命周期数据**才允许放在 Hook 的 `_pendingXxx`
- 如果某个 `_pendingXxx` 需要跨越多个界面或多个生命周期，那它就已经不该继续待在 Hook 里

## 7.4 `DecisionRecord` 是当前第一版统一日志协议

凡是重要的非战斗决策点，如果已经进入主链，就应优先考虑：

- 是否需要落 `DecisionRecord`
- 是否应该通过 `DecisionRecordFactory` 统一格式
- 是否需要为后续 `traceId / 玩家选择 / 结果回写` 预留字段

不要再为单个模块发明新的孤立日志结构，除非只是临时 debug dump。

## 7.5 ID 处理必须回到统一规范

凡是涉及：

- 卡牌 ID
- 遗物 ID
- Boss ID
- 升级态 `+`
- 路径查询 ID

都应回到：

- `STS2_Asset_ID_System.md`
- `IdNormalizer`
- `DataLoader`

而不是在单个 Advisor 内手写特判字符串。

## 7.6 新增决策点时的推荐落地顺序

新增 `Event`、`BossRelic`、`Chest`、`Neow` 这类决策点时，优先遵循：

```text
Hook / Screen capture
  -> Snapshot / candidate extraction
  -> AdvisorEngine or dedicated advisor entry
  -> Advice DTO
  -> DecisionRecordFactory / Recorder
  -> OverlayHUD or native option injection
```

如果新增功能绕开了这条链，后续就很容易变成“某个界面自己一套规则、自己一套日志、自己一套 UI”。

---

## 8. 当前已知架构债（2026-03-17）

1. **第一版 `AdviceEnvelope` 已落地，但闭环仍不完整**  
   现在非战斗主链已经共用 `AdviceEnvelope + DecisionRecord + traceId`，但玩家选择回写、结果追踪、战斗链统一协议仍未完成。

2. **`traceId -> 玩家选择 -> 后续结果` 闭环仍未完整**  
   当前已经能回答主要非战斗场景里“推荐后玩家选了什么”，但还不能完整回答“之后有没有变强”，地图等剩余场景也还没全部接上。

3. **`_pendingXxx` 过渡桥接仍存在**  
   `Campfire -> DeckUpgrade` 这种跨界面桥接已经证明需求真实存在，但当前承载方式还是临时态。

4. **战斗链路和非战斗链路还没有共用同一战略脑**  
   `CombatAdvisor` 仍是独立支线，尚未读到整局层面的风险与资源规划。

5. **地图建议还不是图搜索**  
   当前 `MapAdvisor` 是文本/节点价值级别，不是完整路径枚举与剪枝。

6. **商店展示层仍不完整**  
   `ShopAdvice` 已可计算和记录，但 `ShopAdvicePanel` 还未成为现役显示链的一部分。

---

## 9. 变更验收清单

以后凡是改 `Astrolabe` 主链，至少自问：

1. 这次改动属于 `Core / Data / Engine / Hooks / UI` 哪一层？有没有越层？
2. 当前决策点是否走了统一入口，而不是自己绕开 `AdvisorEngine` / `CombatAdvisor`？
3. 共享上下文是收束到了 `SharedDecisionContext` / 未来总线，还是又散进新的 `_pendingXxx`？
4. 新建议是否有统一日志落点？
5. 新增的 ID / 名称 / lookup 规则是否复用了 `IdNormalizer` 与既有规范？
6. 这次改动后，下一位维护者能否快速回答“这段逻辑在哪里、谁负责、为什么在这一层”？

---

## 10. 为什么现在就需要这份文档

因为 `Astrolabe` 已经不再是一个只有几份原型脚本的小实验：

- 已经有 `README / PROGRESS / GDD / TODO / ID规范 / 逆向报告 / 代码注释` 多个信息源
- 已经有非战斗主链、战斗支线、日志基建、跨界面桥接、HUD 注入、多模块协作
- 接下来还会继续长出 `AdviceEnvelope`、`AdviceContextBus`、`RunReplay`、事件/BossRelic/商店组合求解

如果没有一份“当前真相源”来明确：

- 现役主链是什么
- 哪些只是过渡机制
- 哪层应该承接新逻辑
- 哪个文档回答哪个问题

那后面规模一大，就很容易出现：

- 文件越来越多，但不知道该看哪一份
- 同一种逻辑在 Hook / Engine / UI 三层都各写一点
- TODO 同时承担路线图和真相源，最后两边都失真
- 想改一个点时，先花大量时间考古“到底谁说了算”

这份文档的价值，不是增加文档数量，而是**提前把“之后会乱”的地方收口**。
