# Project Ark × Minishoot Room Elements 借鉴短名单

> **文档目的**：把 `Minishoot` 中真正值得迁移到 `Project Ark` 的 room elements 收口成一份可执行短名单，服务于后续 `Level` 模块的 authoring 扩展、房间语法补强和内容密度提升。  
> **文档定位**：这是 **Reference**，不是现役规范；现役关卡 authority 仍以 `Docs/2_TechnicalDesign/Level/Level_CanonicalSpec.md`、`Docs/3_WorkflowsAndRules/LevelArchitect/Level_WorkflowSpec.md` 与运行时代码为准。  
> **来源依据**：`Minishoot` 解包项目脚本与 Prefab 结构、`Docs/7_Reference/GameAnalysis/Level_Architecture_Synthesis_Minishoot_Silksong_TUNIC.md`、当前 `Level` Canonical Spec、`示巴星` 功能需求。  
> **创建时间**：2026-04-15

---

## 1. 一句话结论

`Minishoot` 最值得 `Project Ark` 借的，不是第二套“大而全房间系统”，而是**一组能提高房间语法密度的小型标准件**。

这些标准件的价值在于：

- 它们能补足 `Transit / Combat / Reward / Safe / Boss` 房间之间的**局部交互层**。
- 它们更适合接到 Ark 已有的 `Room / Door / Encounter / WorldProgress / Ambience` 架构上，而不是另起炉灶。
- 它们能明显增强 `示巴星` 需要的 **探索回报、环境叙事、局部导演感、秘密发现感**。

---

## 2. 选择原则

本短名单只保留满足以下条件的元素：

1. **能提升房间语法密度**，而不是重复已有大系统。
2. **适合做成小型 authoring 件**，而不是要求新建第二套全局 manager。
3. **能映射到 Ark 现有 Level authority**：`Door`、`RoomFlagRegistry`、`WorldProgressManager`、`WorldEventTrigger`、`ScheduledBehaviour`、`AmbienceController`、`CameraDirector`。
4. **和示巴星需求贴合**：异星探索、环境互动、隐藏回报、局部导演、阶段性世界变化。
5. **优先考虑 MVP 可落性**：先让一个房间更有语法，再谈全区域泛化。

---

## 3. Top Shortlist（推荐优先级）

| 优先级 | Minishoot 元素 | 建议翻译为 Ark 标准件 | 价值判断 | 建议阶段 |
| --- | --- | --- | --- | --- |
| **P1** | `Unlocker -> Lock` | `RoomActivator` / `InteractionUnlocker` | 最直接提升房间回路与机关密度 | **MVP** |
| **P1** | `BridgeLocked + BridgeTile` | `BridgeSequence` / `PhasedWalkway` | 非常适合异星环境通路与回路开启 | **MVP** |
| **P1** | `AmbientTrigger + BiomeTrigger` | `AmbienceVolume` / `LocalBiomeVolume` | 强化局部气质切换与导演层 | **MVP** |
| **P1** | `HiddenArea` | `HiddenAreaMask` / `OccluderRevealVolume` | 低成本高收益的探索秘密件 | **MVP** |
| **P2** | `BossDoor / BossDoorFinal` | `CeremonyDoor` / `ProgressionSealDoor` | 强化章节门与 Boss 前厅仪式感 | **Phase 2** |
| **P2** | `InputPromptTrigger + LoreTabletInWorld` | `LoreInteractable` / `PromptVolume` | 补足 Reward / Safe 房的内容密度 | **Phase 2** |
| **P2** | `Location + ActivationManager` 的容器语法 | `Room authoring grammar` / `ActivationGroup` | 更像 authoring 标准化收益，不是单个元素 | **Phase 2** |

---

## 4. 最高优先级候选

## 4.1 远程解锁件：`Unlocker -> Lock`

### Minishoot 的价值

这类元素不是“碰一下就开门”的一次性触发，而是**一个可指向具体目标、可组成局部谜题回路的房间机关件**。

它解决的是：

- 玩家在房间内触发某个互动点后，远处某个锁、门、桥、封锁件发生变化。
- 关卡可以把“看见目标”与“触发目标”拆开，从而形成回路感、期待感和空间记忆。

### 为什么适合 Ark

Ark 已经有：

- `Door`
- `WorldProgressManager`
- `WorldEventTrigger`
- `KeyItemSO`
- `RoomFlagRegistry`
- `SaveBridge`

所以现在缺的不是“锁门系统”，而是一个**房间内细粒度激活件**。

### 适合示巴星的具体用法

- 控制台通电后，远处隔离门开启
- 激活古代装置后，侧路捷径解锁
- 完成小规模战斗后，隐藏收藏室开门
- 两个终端都启动后，主桥接通
- 特定 `WorldPhase` 达成后，房间内部封印解除

### 建议映射

- **建议标准件名**：`RoomActivator` / `InteractionUnlocker`
- **目标对象**：`Door`、`Hazard`、`ScheduledBehaviour`、`WorldEventTrigger`、`ActivationGroup`
- **状态落点**：`RoomFlagRegistry` + `SaveBridge`
- **条件系统**：优先复用 `KeyItemSO` / 进度条件，不要再造第二套 key/flag 体系

### MVP 定义

先只做：

- 一个交互点
- 一个或多个目标
- 支持一次性触发
- 支持存档恢复
- 支持基础反馈（音效 / VFX / 小地图可选提示）

### 未来增强

- 条件组合（需要多终端）
- 镜头拉取 / 指向反馈
- 与世界阶段联动
- 可逆状态与重置语义

---

## 4.2 分段显现通路：`BridgeLocked + BridgeTile`

### Minishoot 的价值

它不是简单的“通路开 / 关”，而是**一段空间被逐格激活**。

这种设计非常适合银河城，因为它同时提供：

- 明确的前后对比
- 强烈的空间反馈
- 记忆点与仪式感
- 可作为奖励、捷径或世界变化的可视证据

### 为什么适合 Ark

Ark 的异星环境很适合做：

- 金属桥逐段展开
- 能量步道逐格点亮
- 虫洞骨架桥生成
- 腐蚀海 / 深渊 / 黑水上的临时通路
- 特定 `WorldPhase` 才存在的相位桥

### 建议映射

- **建议标准件名**：`BridgeSequence` / `PhasedWalkway`
- **主要联动**：`ScheduledBehaviour`、`WorldPhaseManager`、`WorldEventTrigger`、`Hazard`
- **表现实现**：逐段 Sprite / Tile / Collider 开启，不建议一开始就做过重的骨骼或 Timeline 方案

### MVP 定义

先只做：

- 线性一段桥
- 逐格激活
- 带基础反馈（粒子 / 声音 / 震动）
- 可持久化“已开启”状态

### 未来增强

- 非线性桥形
- 相位显隐桥
- 倒计时临时桥
- 与隐藏房 / 追逐段联动

---

## 4.3 局部气质体积：`AmbientTrigger + BiomeTrigger`

### Minishoot 的价值

这类元素把“进入某处就切换局部环境状态”做成了非常轻的小件，而不是靠一个笨重的区域总控来处理一切。

### 为什么适合 Ark

Ark 已经有：

- `AmbienceController`
- `BiomeTrigger`
- `WorldClock`
- `WorldPhaseManager`
- `TilemapVariantSwitcher`

所以这里最有价值的不是“补系统”，而是补**更细粒度的 authoring 语法**。

### 适合示巴星的具体用法

- 进入腐化带时雾与低频嗡鸣增强
- 靠近古代祭坛时混响与环境光变化
- 进入断层风口时风噪上升
- 通过门槛后切入危险区声景
- 靠近事件点时叙事氛围逐步压迫

### 建议映射

- **建议标准件名**：`AmbienceVolume` / `LocalBiomeVolume`
- **运行时消费**：继续由 `AmbienceController`、`CameraDirector`、`TilemapVariantSwitcher` 负责
- **作者心智模型**：房间里放“体积件”，不要每次都手工接一堆一次性脚本

### MVP 定义

先只做：

- 进入 / 离开体积时切一组 ambience preset
- 支持优先级
- 支持基础淡入淡出

### 未来增强

- 与 `WorldPhase` 叠层
- 与镜头 bias / shake / vignette 联动
- 对隐藏区 / Boss 前厅给特殊局部导演

---

## 4.4 隐藏区揭示件：`HiddenArea`

### Minishoot 的价值

`HiddenArea` 通过“进入后前景淡出”的方式，让秘密区域既**先被遮住**，又**在发现瞬间有奖励感**。

### 为什么适合 Ark

这类元素特别适合 Ark 的探索调性：

- 岩壁背后的裂隙
- 孢子幕帘后的静室
- 机械残骸后的补给角
- 遮挡后的小祭坛 / Lore 点 / 资源点

更关键的是，`Level_CanonicalSpec` 的导演层里已经明确出现了 `HiddenAreaMask` 词汇，这说明它与现役方向天然一致。

### 建议映射

- **建议标准件名**：`HiddenAreaMask` / `OccluderRevealVolume`
- **核心职责**：控制遮挡层显隐，不承担奖励逻辑本身
- **可选联动**：小地图揭示、`CameraTrigger`、`LoreInteractable`、秘密标记

### MVP 定义

先只做：

- 一个遮挡层
- 一个 reveal volume
- 进入淡出、离开恢复
- 支持仅视觉 reveal，不强绑其他系统

### 未来增强

- 永久揭示
- 发现后写入秘密状态
- 与 minimap / reward 链联动

---

## 5. 第二优先级候选

## 5.1 仪式门：`BossDoor / BossDoorFinal`

### 价值

这类门的价值不在“是否能开”，而在**开门过程是否有章节感和仪式感**。

### 适合 Ark 的原因

Ark 的类魂气质很需要：

- 大型封印门
- 星图认证门
- 古代主控舱门
- Boss 前厅封锁门

### 建议映射

- **建议标准件名**：`CeremonyDoor` / `ProgressionSealDoor`
- **系统挂接**：`Door` + `WorldProgressManager` + `CameraDirector` + `DoorTransitionController`
- **注意点**：不要做第二种普通 Door；它应是 `Door` 的语义增强层

### 阶段建议

放在 `Phase 2` 更合适。先把基础探索语法补起来，再做高仪式感门。

---

## 5.2 轻交互点：`InputPromptTrigger + LoreTabletInWorld`

### 价值

这类元素能快速提升 `Reward / Safe / Secret` 房间的内容密度，让房间不是只有战斗和拾取。

### 适合 Ark 的原因

很适合承载：

- 黑匣子残片
- 异星碑文
- 研究终端
- 祈祷节点
- 守夜人注释点

### 建议映射

- **建议标准件名**：`LoreInteractable` / `PromptVolume`
- **注意**：保持轻量，优先接现有 UI / 存档，不要做成重量级对话系统

### 阶段建议

适合作为 `Reward / Safe` 房的小型增香件，而不是当前 Level 主链优先级第一位。

---

## 5.3 容器语法借鉴：`Location + ActivationManager`

### 价值

这不是单个 room element，而是一套**场景 authoring 语法**：

- `Elements`
- `Encounters`
- `Decoration`
- 若干可统一激活 / 停用的分组

### 为什么对 Ark 重要

Ark 当前的方向就是“标准语法先于内容量”。因此这类借鉴更适合进入：

- `Level Architect` 的 builder 默认结构
- `Room` 标准子节点约定
- 统一 `ActivationGroup` / `PreventDeactivation` 语义

### 结论

它更像 `Level` authoring 标准化工程的一部分，不建议把它误当成某个单独 prefab 功能来做。

---

## 6. 不建议直接搬的项

| Minishoot 元素 | 不建议直接搬的原因 | Ark 建议 |
| --- | --- | --- |
| `EncounterOpen / EncounterClose` | Ark 已有 `Combat / Arena` 节奏分化和 `EncounterSystem`，重复搬系统收益低 | 借 authoring 细节，不搬系统本体 |
| `Checkpoint / TriggerCheckpointOverworld` | Ark 已有自己的 checkpoint 与 save 流程 | 只参考轻量交互感 |
| `HudTrigger` | 太 one-off，容易变成散乱特例 | 并入更通用的区域 UI / 导演触发语义 |
| `RaceCheckpoint` | 和当前主线体验不贴 | 若未来有追逐/竞速再回收 |
| `Buyable` | 依赖经济与商店循环是否立项 | 先不进入当前优先级 |

---

## 7. 建议的 MVP 落地顺序

如果只做一个最小可玩批次，建议顺序如下：

### Batch A：先补探索语法

1. `RoomActivator`
2. `HiddenAreaMask`
3. `AmbienceVolume`

理由：

- 开发成本相对可控
- 对 `Reward / Transit / Secret` 房收益很高
- 不强依赖复杂战斗或大型演出链

### Batch B：再补空间回路感

4. `BridgeSequence`
5. `CeremonyDoor`

理由：

- 更偏中高成本 authoring / 演出件
- 更适合在前一批标准件跑顺后再做

---

## 8. 对 Level 架构的映射原则

这些参考件进入 Ark 时，应遵守以下边界：

1. **不新增第二套世界状态系统**：状态继续走 `WorldProgressManager`、`RoomFlagRegistry`、`SaveBridge`。
2. **不新增第二套房间 authority**：仍由 `Room` / `Door` / `CameraDirector` / `AmbienceController` 消费。
3. **优先做 authoring 小件**：避免再做一个“大关卡导演器”。
4. **先让一个房间变好玩**：先在代表房间验证 feel，再考虑批量铺开。
5. **命名按 Ark 语义落地**：不要机械沿用 `Minishoot` 脚本名。

---

## 9. 最终建议

如果只保留一句执行建议：

> **优先把 `Unlocker`、`Bridge`、`AmbientTrigger`、`HiddenArea` 这四类翻译成 Ark 自己的标准 room elements；它们最能在不打破现有 Level 架构的前提下，显著提升房间密度与探索回报。**

而 `BossDoor`、`LoreInteractable`、`ActivationGroup` 适合作为下一轮增强，不需要和第一批绑死上线。
