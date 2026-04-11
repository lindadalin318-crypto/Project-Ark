# Level-RoomRuntimeChain-Hardening

## 文档定位

本文件是 `Level` 模块当前活跃专项 **`Room` 运行时消费链收口** 的执行计划。

它负责维护：

- 本轮链路收口目标
- 范围边界
- 完成标准
- 当前状态
- 工作拆分
- 风险与注意事项
- 关联文档

它不替代以下真相源：

- `Docs/2_Design/Level/Level_CanonicalSpec.md`
- `Docs/2_Design/Level/Level_WorkflowSpec.md`
- `Docs/6_Diagnostics/Level_RoomElements_Findings.md`
- `Docs/5_ImplementationLog/ImplementationLog.md`

一句话原则：

> 本文件回答“`Room` 运行时消费链先修什么、怎么收口、做到什么算稳”。

---

## 当前目标

> **把 `Room` 运行时消费链从“主干合理但 owner 分叉、清房语义偏松、离房停怪不完整”的状态，收口到“owner 单一、状态一致、离房可正确复位、authoring/validator 口径同步”的状态。**

本轮优先解决：

- `Room` / `ArenaController` / `OpenEncounterTrigger` 三套 encounter owner 并行分叉
- `OpenEncounterTrigger` 过早把局部遭遇完成升级为整个 `Room` cleared
- 离房停怪 / 离区 despawn 链对对象池敌人不完整
- `Room` / `RoomManager` / `ArenaController` 之间的门解锁与清房 authority 重叠
- `EncounterMode`、`_respawnOnReenter`、弱消费事件等死数据 / 半接入状态长期滞留

---

## 范围

### In Scope

- `Assets/Scripts/Level/Room/Room.cs`
- `Assets/Scripts/Level/Room/RoomManager.cs`
- `Assets/Scripts/Level/Room/OpenEncounterTrigger.cs`
- `Assets/Scripts/Level/Room/ArenaController.cs`
- `Assets/Scripts/Level/Room/WaveSpawnStrategy.cs`
- `Assets/Scripts/Combat/Enemy/EnemySpawner.cs`
- `Assets/Scripts/Level/Camera/RoomCameraConfiner.cs`（只检查消费边界，不做功能扩张）
- `Assets/Scripts/Level/Editor/LevelArchitect/LevelValidator.cs`（补本轮需要的 authoring / runtime 护栏）
- `Docs/2_Design/Level/Level_WorkflowSpec.md`
- `Docs/6_Diagnostics/Level_RoomElements_Findings.md`
- `SampleScene` 中用于代表性验证的房间链路

### Out of Scope

- `Level` 全模块的大重构
- 小地图、存档、世界时钟等非阻塞本轮收口的问题重写
- 大规模场景 authoring 迁移到标准根节点结构
- 新建完整通用 encounter framework 或全新中间层体系
- 为了“更优雅”而重写已经工作正常的下游消费者（如 `RoomCameraConfiner`）

---

## 完成标准

1. **单一 encounter owner**：普通房、`Arena`、`OpenEncounter` 的运行时入口有清晰主驱动，不再各自偷偷 `new WaveSpawnStrategy`
2. **清房语义收口**：局部遭遇完成不会再错误升级为整个房间 cleared；`Room` cleared 的确权入口唯一
3. **停怪 / despawn 完整**：离房、离区、重置时，预放置敌人与对象池生成敌人都能按预期停用或回收
4. **门与状态 authority 清楚**：门解锁、房间状态推进、清房回调不再多点重复决策
5. **死数据与弱链路处理完成**：未接入或误导性的字段 / 事件要么正式接入，要么删除 / 降级 / 写清保留理由
6. **验证闭环成立**：`LevelValidator`、文档口径与代表性 Play Mode 验证结果一致，不再靠口头记忆维持语义

---

## 当前状态

- **状态**：进行中（A0/A1/A2/A3/A4 已完成；A5 已完成 validator / 文档回写，待代表房间 Play Mode 验证）
- **现状判断**：主骨架可保留，已从“plan 冻结”推进到“代码与文档基本收口，剩余工作集中在现场验证与最终 Gate 判定”的阶段
- **已确认合理的部分**：`RoomManager` 作为当前房间 authority、`Room` 作为本地编排者、`RoomCameraConfiner` 作为下游 adapter，这三层边界成立
- **本轮已完成**：`RoomManager` 已成为整房 `Room cleared` / combat door unlock 的唯一 authority；`OpenEncounterTrigger` 已退回局部 encounter owner，不再把局部完成升级为整房 cleared；`EnemySpawner` 已补活体追踪与 `StopAndDespawnActiveEnemies()`；`Room` 已成为 room-level combat 单入口；`ArenaController` 已退回 ceremony orchestrator 并补离房/重置取消链；`LevelValidator` 已补 `OpenEncounterTrigger`、`ArenaController` 与 `EncounterMode` 护栏；`_respawnOnReenter`、`RoomState.Locked`、无消费者的房间/遭遇弱事件已完成清理；`Level_WorkflowSpec` 与 `Diagnostics` 已同步到当前 owner 口径
- **复核结论（2026-04-11 10:38）**：按 Step 定义严格核对后，A3 虽未单独记账，但其目标已由现役代码满足；A5 仍不能算“完全完成”，因为其完成标准中的代表房间 Play Mode 验证尚未形成正式记录。
- **当前剩余高优先级问题**：完成代表房间 Play Mode 验证并回写结果；若验证发现新的 authoring 漏口，再决定是否继续裁剪少量非房间主链弱事件
- **当前有利条件**：`SampleScene` 里 `OpenEncounterTrigger` 还未铺开，适合先收口语义再大规模 authoring

### MVP 与后续增强

#### MVP（本专项要完成）

- 收口 `encounter owner`
- 修正 `Room cleared` 语义与门解锁 authority
- 补齐 pooled enemy 的离房 / 离区停用链
- 清理本轮已确认的死字段、弱事件和误导性 fallback
- 把 validator / 文档 / 代表房间验证同步到同一口径

#### 未来增强（不并入本轮）

- 若后续需要，再设计统一的 `EncounterDriver` / `IEncounterOwner` 抽象
- 批量把更多房间迁到标准 authoring 根节点结构
- 再评估 `visited`、地图、存档等更广义的状态 authority 收口

---

## 工作拆分总览

| 步骤 | 名称 | 目标 | 产出 | 通过标准 |
| --- | --- | --- | --- | --- |
| A0 | 冻结生命周期口径 | 先写清 encounter / clear / despawn / unlock 的唯一语义 | 术语与 owner 冻结结论 | 团队能明确回答“谁有权启动、谁有权清房、谁负责回收” |
| A1 | 补齐停怪与回收链 | 让离房 / 离区 / reset 对预放置与对象池敌人都生效 | 敌人生命周期收口结果 | 代表房间中不会残留失控活体敌人 |
| A2 | 收口 encounter owner | 把 encounter 启动与完成链收口到清晰主驱动 | 统一入口与调用边界 | 不再有三套入口各自管理一场遭遇 |
| A3 | 收口清房与门 authority | 让 room cleared、door unlock、room state 推进回到唯一确权链 | 状态推进链整改结果 | 不再出现重复解锁、重复确权或局部遭遇升级整房 cleared |
| A4 | 清理死数据与弱链路 | 处理未接入字段、弱消费事件、误导性 fallback | 删除/保留清单与代码收口结果 | 每个遗留项都能回答“留着干嘛、何时删” |
| A5 | 同步 validator 与文档 | 把运行时收口同步到 authoring 护栏和现役文档 | Validator 规则、文档修订、验证记录 | 文档、护栏、运行时结果三者不再打架 |
| G | Gate L 验收 | 检查 6 条完成标准是否全部满足 | 验收结论 | 全部通过后才退出本专项 |

---

## 分步执行细则

## Step A0 — 冻结生命周期口径

### 目标

先把本轮真正要收口的 4 条运行时语义钉死：

1. **谁有权启动 encounter**
2. **谁有权宣布 room cleared**
3. **谁有权解锁 combat doors**
4. **谁负责离房 / 离区 / reset 时的敌人停用与回收**

### 要做什么

- 把 `Room`、`RoomManager`、`ArenaController`、`OpenEncounterTrigger` 的职责边界写成一句话口径
- 明确 `OpenEncounterTrigger` 是“局部遭遇 owner”还是“整房清房入口”的候选，禁止继续语义摇摆
- 明确 `RoomCameraConfiner` 仍然只是消费者，不并入本轮主驱动整改
- 把本轮 MVP 与未来增强分开，避免顺手长出新的抽象层

### 完成标准

- 评审时不再出现“这件事也许谁都能做”的回答
- 后续代码修改都能对照唯一 owner 口径推进

---

## Step A1 — 补齐停怪与回收链

### 目标

优先修复当前最实际的体验 / 一致性风险：**离房停怪、离区 despawn、reset 回收 对对象池敌人无效或不完整。**

### 要做什么

- 盘清 `Room.DeactivateEnemies()`、`OpenEncounterTrigger` 离区处理、`EnemySpawner.ResetSpawner()`、波次敌人实际 parent / 生命周期之间的断点
- 让预放置敌人与 pooled enemy 都进入统一的停用 / 回收协议
- 明确“停止刷怪”和“回收已生成怪”不是一回事，避免继续只 reset strategy 不处理活体实例
- 对重进房间、重新触发、离区退场这三种路径分别验证

### 完成标准

- 离房后不会残留上一房间生成的活体敌人继续存在或参与战斗
- 离区后 open encounter 不会只停刷怪、不清活体
- 回收逻辑不依赖对象刚好挂在 `_spawnPoints` 子树下

---

## Step A2 — 收口 encounter owner

### 目标

把当前分叉的 encounter 启动链收口到**一个清晰主驱动模型**，但不为了“通用化”而立刻引入过重框架。

### 要做什么

- 明确普通房、`Arena` 房、`OpenEncounter` 三类遭遇的统一入口关系
- 避免 `Room`、`ArenaController`、`OpenEncounterTrigger` 三处继续各自直接创建 `WaveSpawnStrategy`
- 如果短期不能完全统一实现，也必须统一“谁发起、谁接完成回调、谁通知 `RoomManager`”的协议
- 保留 `ArenaController` 这种仪式化 encounter orchestrator，但让它与整房 owner 边界清晰

### 完成标准

- 代码中不再需要靠注释提醒“这里不要再调 `Room.ActivateEnemies()` 否则会和另一套链打架”
- 一场 encounter 的启动、完成、清房通知链可以用一条图讲清楚

---

## Step A3 — 收口清房与门 authority

### 目标

让 `Room cleared` 的确权入口唯一，避免局部遭遇完成、门解锁、房间状态推进互相串写。

### 要做什么

- 明确 `NotifyRoomCleared` 是否仍由 `RoomManager` 统一确权；若保留，则其它链路只上报、不直接确权
- 修正 `OpenEncounterTrigger` 的完成语义，避免一个局部 trigger 清完就把整个房间标成 cleared
- 收口 `ArenaController` 与 `RoomManager` 之间的 combat door unlock 重叠
- 重新审视 `RoomState` 的职责，避免继续混入“visited”这类更适合其他系统持有的语义

### 完成标准

- `Room cleared` 只有一个最终 authority
- combat door unlock 只有一个最终决策入口
- 局部遭遇和整房清房的语义边界能被明确验证

---

## Step A4 — 清理死数据与弱链路

### 目标

把“看起来像系统一部分、实际没真正接上”的东西处理掉，减少误导和后续考古成本。

### 优先处理对象

- `EncounterMode`
- `OpenEncounterTrigger._respawnOnReenter`
- `RoomState.Locked`
- 弱消费或无消费者的 `LevelEvents` 事件
- 已失去必要性的 fallback / 兼容分支

### 处理原则

- **真要用**：正式接入运行时链路并补验证
- **短期还要留**：写清保留理由、owner、退役条件
- **已经无价值**：直接删除，不继续堆历史残影

### 完成标准

- 关键字段和事件都能回答“谁在读、什么时候生效、为什么还存在”
- 不再保留对当前设计意图无帮助的假入口或死语义

---

## Step A5 — 同步 validator 与文档

### 目标

让运行时收口结果进入 authoring 护栏和现役文档，而不是只留在口头结论里。

### 要做什么

- 根据新链路补 `LevelValidator` 的必要检查项或 warning 文案
- 同步 `Level_WorkflowSpec.md` 中房间元素接入 SOP / owner 口径
- 回写 `Level_RoomElements_Findings.md` 的最终结论，避免诊断文档落后于实现
- 做一轮代表性房间 Play Mode 验证，至少覆盖：普通 encounter、`Arena`、离房 / 回房、门状态变化

### 完成标准

- validator 能帮忙提前发现本轮修复过的问题类型
- 文档与代码的 owner 说法一致
- 代表性验证能证明链路收口不是纸面结论

---

## Gate L — 本专项验收门槛

在归档或切出下一专项前，必须逐条确认：

- **Encounter owner 单一**：是否已不存在三套并行 owner 各自起一场 encounter
- **清房语义清晰**：是否局部 encounter 不再越权升级整房 cleared
- **停怪 / 回收完整**：是否离房 / 离区 / reset 都能正确处理 pooled enemy
- **门与状态 authority 清楚**：是否 door unlock 与 room state 推进不再重复决策
- **死数据已处理**：是否没有继续保留误导性字段 / 事件 / fallback
- **验证闭环成立**：是否 validator、文档、Play Mode 结果一致

若任一项未通过，本专项继续停留在 `ongoing/`。

---

## 风险与注意事项

- 本专项的目标是**收口 owner 与生命周期**，不是顺手做 `Level` 全模块重构
- 若一开始就引入过重的通用化抽象，容易把“收口问题”做成“架构实验”
- `SampleScene` 当前 authoring 仍是混合态，代码链路修正后仍需要用代表房间做真实验证
- 对象池敌人的停用 / 回收改动必须额外小心，避免引入新的状态泄漏
- 任何对 `RoomState` 的调整，都要先确认不会误伤地图、存档、门和 UI 的现有消费口径

---

## 关联文档

- `Docs/0_Plan/ProjectPlan.md`
- `Docs/2_Design/Level/Level_CanonicalSpec.md`
- `Docs/2_Design/Level/Level_WorkflowSpec.md`
- `Docs/6_Diagnostics/Level_RoomElements_Findings.md`
- `Implement_rules.md`
- `Docs/5_ImplementationLog/ImplementationLog.md`
