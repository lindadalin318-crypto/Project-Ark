# Level Module Canonical Spec

## 1. 文档目的

这份文档是 `Level` 模块的**现役架构规范**。

它只负责定义以下内容：

- 模块的现役架构与层次划分
- 核心类型与关键字段声明
- 运行时 authority 与数据流向
- Editor 工具链的职责边界
- 模块边界、跨模块依赖与校验基线

> **设计哲学基底**：以 Minishoot 的工程化关卡思维为骨架（标准化场景容器 + 门语义分级 + 节点类型分级 + 轻量验证工具），保留 Ark 的差异化优势（世界时钟 + 动态阶段 + 混合层间结构 + 类魂叙事氛围）。
>
> 参考分析见 `Docs/Reference/Level_Architecture_Synthesis_Minishoot_Silksong_TUNIC.md`。

### 1.1 本文档不负责

- 具体区域内容设计（房间表、敌人配置、叙事脚本）
- 美术规范（Tilemap 调色板、装饰标准、光照风格）
- 过程性变更记录（实现日志、版本追踪、阶段说明）

---

## 2. 适用范围

### 2.1 Runtime

- `Assets/Scripts/Level/` 全部子目录
  - `Room/` — `Room`、`RoomManager`、`Door`、`DoorTransitionController`
  - `Data/` — `RoomSO`、`EncounterSO`、`RoomVariantSO`、`WorldPhaseSO`、`WorldProgressStageSO`、`KeyItemSO` 与相关枚举
  - `Checkpoint/` — `Checkpoint`、`CheckpointManager`
  - `Progression/` — `Lock`、`KeyInventory`、`WorldProgressManager`
  - `WorldClock/` — `WorldClock`、`WorldPhaseManager`
  - `DynamicWorld/` — `ScheduledBehaviour`、`WorldEventTrigger`、`AmbienceController`、`BiomeTrigger`、`TilemapVariantSwitcher`
  - `Map/` — `MinimapManager`、`MapPanel`、`MapRoomData`、`MapConnection`
  - `Camera/` — `CameraDirector`、`CameraTrigger`
  - `Narrative/` — `NarrativeFallTrigger`
  - `GameFlow/` — `GameFlowManager`
  - `Hazard/` — `EnvironmentHazard`、`DamageZone`、`ContactHazard`、`TimedHazard`
  - `SaveBridge.cs`
- `Assets/Scripts/Core/LevelEvents.cs`

### 2.2 Editor

- `Assets/Scripts/Level/Editor/LevelArchitect/`
  - `LevelArchitectWindow`
  - `LevelSliceBuilder`
  - `RoomFactory`
  - `DoorWiringService`
  - `LevelValidator`
  - `PacingOverlayRenderer`
  - `RoomBlockoutRenderer`
  - `BlockoutModeHandler`
  - `BatchEditPanel`
  - `SceneScanner`
  - `ScaffoldSceneBinder`

### 2.3 Data Assets

- `Assets/_Data/Level/` 下所有关卡 SO 资产

### 2.4 Scenes

- `Assets/Scenes/SampleScene.unity`

### 2.5 Docs

- `Docs/2_Design/Level/Level_CanonicalSpec.md`
- `Docs/2_Design/Level/Level_WorkflowSpec.md`
- `Docs/Reference/Level_Architecture_Synthesis_Minishoot_Silksong_TUNIC.md`
- `Implement_rules.md`

### 2.6 Assembly Definitions

- `Assets/Scripts/Level/ProjectArk.Level.asmdef`
- `Assets/Scripts/Level/Editor/ProjectArk.Level.Editor.asmdef`

---

## 3. 模块设计原则

### 3.1 标准语法先于内容量

先定义房间标准件、连接语义和校验规则，再扩张内容规模。关卡的复杂度应该来自**标准件组合**，而不是来自特殊 case 的堆叠。

### 3.2 节奏闭环先于地图大小

任何可玩切片都必须能组织出：**进入 → 施压 → 证明 → 回报 → 回路 → 收束**。

### 3.3 场景对象与 SO 分权

- **Scene 中的 `Room` / `Door`** 负责空间事实与连接事实
- **SO** 负责元数据、节奏类型、遭遇配置、阶段配置
- **运行时状态** 不写回 SO，统一交给运行时组件与存档桥接层

### 3.4 Editor 工具只做 authoring 与 audit

Editor 工具负责创建、连线、可视化、校验、导入导出；**不接管运行时 authority**。

---

## 4. 现役架构总览

### 4.1 五层模型

```text
┌─────────────────────────────────────────────────────────────┐
│ Layer 5: 连接拓扑层 (Connection Topology)                  │
│ Door._targetRoom / Door._targetSpawnPoint                  │
│ Scene 中 Door 直接定义房间连接关系                         │
└───────────────────────────┬─────────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────────┐
│ Layer 4: 房间容器层 (Room Container)                       │
│ Room + RoomSO + 标准子节点语法 + CameraConfiner            │
└───────────────────────────┬─────────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────────┐
│ Layer 3: 连接缝合层 (Connection / Transition)              │
│ GateID + ConnectionType + TransitionCeremony               │
│ DoorTransitionController 负责过渡执行                       │
└───────────────────────────┬─────────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────────┐
│ Layer 2: 节奏内容层 (Pacing Content)                       │
│ Encounter + Hazard + Lock + Checkpoint + DestroyableObject │
│ RoomFlagRegistry 负责房间级细粒度状态                      │
└───────────────────────────┬─────────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────────┐
│ Layer 1: 导演引导层 (Director / Guidance)                  │
│ CameraTrigger + BiomeTrigger + AmbienceController          │
│ WorldClock + WorldPhaseManager + WorldProgressManager      │
└─────────────────────────────────────────────────────────────┘
```

### 4.2 Authority 矩阵

| 层 | 真相源 | 生产者 | 主要消费者 | 说明 |
|----|--------|--------|-----------|------|
| Layer 5 连接拓扑 | Scene 中 `Door` 目标引用 | Authoring + `DoorWiringService` | `DoorTransitionController`、`MinimapManager` | Door 是连接关系的唯一真相源 |
| Layer 4 房间容器 | Scene 中 `Room` + `RoomSO` | `RoomFactory` + 手工精修 | `RoomManager`、`CameraDirector`、`MinimapManager` | `Room` 管空间，`RoomSO` 管元数据 |
| Layer 3 连接语义 | `Door._gateID` / `_connectionType` / `_ceremony` | Authoring | Overlay、过渡演出、进度系统 | 语义层不替代目标引用 |
| Layer 2 节奏内容 | 场景子节点 + 运行时组件 | Authoring + 运行时系统 | `Room`、`RoomManager`、`SaveBridge` | 互动件状态由 `RoomFlagRegistry` 管理 |
| Layer 1 导演引导 | 场景触发器 + SO + 世界状态 | Authoring + 世界管理器 | `CameraDirector`、`AmbienceController`、动态世界系统 | 驱动节奏感、氛围和阶段变化 |

---

## 5. 核心声明

### 5.1 `RoomNodeType` — 房间主体验类型

```csharp
namespace ProjectArk.Level
{
    /// <summary>
    /// 房间的主体验类型。
    /// 只回答“玩家进入这个房间后，主要在做什么？”。
    /// </summary>
    public enum RoomNodeType
    {
        Transit = 0,
        Combat = 1,
        Arena = 2,
        Reward = 3,
        Safe = 4,
        Boss = 5
    }
}
```

**语义要求**：

- `Transit`：过路与连接，主体验是通过，不承载强事件
- `Combat`：开放战斗，主体验是边移动边打，流程不断
- `Arena`：封闭清算，主体验是锁门清场或波次验证
- `Reward`：奖励与缓冲，主体验是拿到资源、信息或探索回报
- `Safe`：完整安全区，主体验是休整，不存在持续敌意
- `Boss`：Boss 竞技场，主体验是完整 Boss 战流程

**边界说明**：

- `RoomNodeType` 只描述**主体验**，不再承载 `Anchor / Loop / Hub / Threshold` 这类结构身份。
- 结构身份后续应放入 `WorldGraph` 标签、连接语义或额外元数据，而不是继续塞进单一 `RoomNodeType` 枚举。

### 5.2 `ConnectionType` — 连接语义类型

```csharp
namespace ProjectArk.Level
{
    /// <summary>
    /// 门/通道连接的语义类型。
    /// </summary>
    public enum ConnectionType
    {
        Progression,
        Return,
        Ability,
        Challenge,
        Identity,
        Scheduled
    }
}
```

**语义要求**：

- `Progression`：主路线推进
- `Return`：回返 / 捷径兑现
- `Ability`：能力门 / 暂时阻断
- `Challenge`：挑战段 / Arena / Boss 前厅
- `Identity`：Biome 或章节气质切换
- `Scheduled`：受世界阶段控制的连接

### 5.3 `GateID` 命名规范

`GateID` 是 `Door` 上的命名 ID，用于工具连线、进度语义和可视化标注。

```text
{方位}_{编号}   -> left_1, right_1, top_1, bottom_1
{语义}_{编号}   -> boss_entrance, shortcut_south, rift_down_1
{特殊名称}      -> elevator_up, fall_trigger
```

**约束**：

- 同一个 `Room` 内的 `GateID` 必须唯一
- `GateID` 服务于工具链和语义表达，不替代 Door 的目标引用
- 依赖 `GateID` 的 authoring 内容必须通过 `LevelValidator` 与人工走图共同复核

### 5.4 `Door` 关键字段

```csharp
[Header("Gate & Connection")]
[SerializeField] private string _gateID;
[SerializeField] private ConnectionType _connectionType = ConnectionType.Progression;

[Header("Transition")]
[SerializeField] private TransitionCeremony _ceremony = TransitionCeremony.Standard;
```

**Authority**：

- `_targetRoom` / `_targetSpawnPoint`：空间连接 authority
- `_gateID`：命名语义 authority
- `_connectionType`：连线语义 authority
- `_ceremony`：过渡表现 authority

### 5.5 `TransitionCeremony` — 过渡仪式感分级

```csharp
namespace ProjectArk.Level
{
    /// <summary>
    /// 门过渡的仪式感等级。
    /// </summary>
    public enum TransitionCeremony
    {
        None,
        Standard,
        Layer,
        Boss,
        Heavy
    }
}
```

**分级要求**：

- `None`：隐藏通路或无感切换
- `Standard`：普通门的短淡黑过渡
- `Layer`：层间切换，带更强的空间感
- `Boss`：Boss 前后门的独立演出
- `Heavy`：重型门、多段反馈、大动作转换

### 5.6 `RoomFlagRegistry` — 房间级细粒度状态

```csharp
namespace ProjectArk.Level
{
    /// <summary>
    /// 房间级 Flag 系统。
    /// </summary>
    public class RoomFlagRegistry : MonoBehaviour
    {
        public void SetFlag(string roomID, string flagKey, bool value = true) { ... }
        public bool GetFlag(string roomID, string flagKey) { ... }
        public IReadOnlyDictionary<string, bool> GetRoomFlags(string roomID) { ... }
        public void WriteToSaveData(ProgressSaveData data) { ... }
        public void ReadFromSaveData(ProgressSaveData data) { ... }
    }
}
```

**职责**：

- 记录房间内可破坏物、隐藏区、永久开门、机关状态
- 与 `SaveBridge` 对接，把细粒度空间状态持久化到存档
- 不承担 SO 配置职责，不承担场景结构职责

---

## 6. 房间标准化语法

### 6.1 最小 `Room` 层级

```text
Room_[ID]
├── Navigation
│   ├── Doors
│   └── SpawnPoints
├── Elements
├── Encounters
│   ├── SpawnPoints          (可选)
│   ├── EnemySpawner         (战斗房常见)
│   └── ArenaController      (`Arena` / `Boss` 常见)
├── Hazards
├── Decoration
├── Triggers
└── CameraConfiner
```

### 6.2 子节点职责矩阵

| 子节点 | 内容 | 查询者 | 激活策略 |
|--------|------|--------|---------|
| `Navigation` | Door、进出点、层间导航点 | `Room`、`RoomManager`、`DoorTransitionController`、`MinimapManager` | 始终激活 |
| `Elements` | 锁、拾取物、互动件、可破坏物、NPC | `Room`、`RoomFlagRegistry` | 按房间激活 + 持久状态 |
| `Encounters` | 遭遇触发器、波次生成、Arena 骨架 | `Room`、`RoomManager` | 按房间激活 |
| `Hazards` | 环境伤害与危险物 | 物理系统 / 触发系统 | 按房间激活 |
| `Decoration` | 纯视觉内容 | 无逻辑查询 | 按房间激活 |
| `Triggers` | Camera、Biome、事件、隐藏区域触发器 | `CameraDirector`、`AmbienceController`、世界系统 | 始终激活 |
| `CameraConfiner` | 镜头边界 | `Room`、`CameraDirector` | 始终激活 |

### 6.3 Authoring 约束

- 所有可通行门默认放在 `Navigation/Doors`
- 导航生成点默认放在 `Navigation/SpawnPoints`
- 战斗生成点优先放在 `Encounters/SpawnPoints`
- `CameraConfiner` 必须作为房间根下的独立子节点存在
- `Triggers` 用于在玩家进入前就必须就绪的感知类对象
- 房间结构扩展可以增加额外内容层，但不得替代标准根节点职责

### 6.4 新增房间元素接入协议（严格版）

新增任何房间元素前，必须先在设计说明、任务描述或实现文档中回答以下 6 项：

| 维度 | 必须回答的问题 | 现役收口点 |
|------|----------------|------------|
| 顶层分类 | 它属于哪一类现役房间元素分类 | 先复用现有分类，再考虑新增大类 |
| 场景挂点 | 它放在 `Navigation / Elements / Encounters / Hazards / Triggers / CameraConfiner` 的哪一层 | Scene 是空间 authority |
| 运行时 owner | 谁驱动它的启停、交互或状态切换 | 组件自身、`Room`、`RoomManager`、`WorldPhaseManager` 或其他管理器 |
| 状态通道 | 它是会话态、房间持久态，还是世界级进度态 | 房间级走 `RoomFlagRegistry`，世界级走对应 manager |
| 触发来源 | 它由重叠触发、交互键、房间切换、世界阶段、世界进度中的哪一种驱动 | 只能有一个主驱动 |
| Validator 契约 | 缺什么会坏，哪些字段/层级必须校验 | 阻塞型问题进入 `LevelValidator` |

**协议要求：**

1. **先归类，后实现**：新增元素默认必须先归入现役房间元素分类，不能直接发明新的“特殊 case 大类”。
2. **空间事实留在 Scene**：位置、朝向、层级、目标引用等空间事实只存在于场景对象；只有当元素需要复用配置或策划调参时，才引入 SO。
3. **房间级持久化统一走 `RoomFlagRegistry`**：凡是“同一个房间内被打开 / 打碎 / 触发后应长期保持”的状态，一律写入 `RoomFlagRegistry`，并通过 `SaveBridge` 进入存档；禁止把运行时结果写回 SO。
4. **只在需要时扩 `Room` 的收集链**：当前 `Room` 只主动收集 `Door`、`EnemySpawner`、`OpenEncounterTrigger`、`DestroyableObject` 和 SpawnPoint。只有当新元素需要被 `Room` / `RoomManager` 查询、重置或统一调度时，才允许扩展 `CollectSceneReferences()`；否则应保持组件自治。
5. **按主触发机制决定挂点**：导航事实进 `Navigation`，互动件进 `Elements`，战斗编排进 `Encounters`，环境伤害进 `Hazards`，必须在玩家进房前就绪的感知/导演类对象进 `Triggers`。
6. **编辑期 schema 按需扩展**：只有当新元素需要参与 `LevelDesigner.html` JSON、Overlay 或批量 authoring 工具时，才扩展编辑期 schema；纯场景手工 authoring 的运行时细节，不应反向发明脱离 Scene 主链的中间模型。

### 6.5 现役房间元素分类（六大玩法家族 + 基础设施件）

| 中文分类 | 英文标签 | 历史旧术语（兼容） | 代表类型 | 默认挂点 | 运行时 authority | 状态通道 | Validator 关注点 |
|---------|----------|--------------------|----------|----------|------------------|----------|------------------|
| **通路件** | `Path` | `Door / Gate` | `Door`、层间过渡门、时间门 | `Navigation` | `Door._targetRoom` / `_targetSpawnPoint` + 语义字段 | `Door` 本体 + 世界阶段 / 进度系统 | 双向连接、出生点、`_playerLayer`、门控字段 |
| **交互件** | `Interact` | `Interact Anchor` | `Lock`、`Checkpoint`、`PickupBase` 派生物 | `Elements` | 组件自身 + 可选 SO / manager | 会话态或 manager 持有 | 触发器、`_playerLayer`、必需引用、输入订阅 |
| **状态件** | `Stateful` | `Persistent Room Element` | `DestroyableObject`、房间内永久机关、一次性揭示物 | `Elements` | 场景组件 + `RoomFlagRegistry` flag | `RoomFlagRegistry` ↔ `SaveBridge` | 父 `Room`、flag key、持久状态一致性 |
| **战斗件** | `Combat` | `Encounter Element` | `OpenEncounterTrigger`、`ArenaController`、`EnemySpawner` | `Encounters` | `EncounterSO` + 运行时波次策略 | 房间战斗状态 / 房间清除状态 | `EncounterSO`、Spawner、房型语义匹配 |
| **环境机关件** | `Environment` | `Hazard Element` | `EnvironmentHazard` 及其子类 | `Hazards` | 危险物组件自身 | 通常为会话态；若永久关闭再接 `RoomFlagRegistry` | 碰撞器、Layer、伤害配置、可选 owner room |
| **导演件** | `Directing` | `Trigger / Director` | `BiomeTrigger`、`HiddenAreaMask`、`ScheduledBehaviour`、`ActivationGroup`、`WorldEventTrigger` | `Triggers`（或显式扩展内容层） | 世界事件、`RoomManager`、`AmbienceController` 或组件自身 | 会话态 / 世界阶段态 / 世界进度态 | 触发器、phase 配置、target 引用、manager 依赖 |
| **基础设施件** | `Infrastructure` | 无 | `SpawnPoint`、`CameraConfiner` | `Navigation/SpawnPoints`、`CameraConfiner` | `Room`、`Door`、`CameraDirector` 等房间运行链 | 跟随房间运行链，不作为独立玩法状态件 | 位置语义、引用完整性、`CameraConfiner` 物理设置 |

**补充规则：**

- 这套分类是 **authoring / validator / 文档沟通口径**，不是具体组件类名替代，也不是运行时 owner 声明。
- `ActivationGroup`、`ScheduledBehaviour` 这类“控制其他对象”的组件，可以放在独立扩展层，但它们不能替代标准根节点职责。
- `HiddenAreaMask` 这类靠玩家触发感知的遮罩类对象，若需要在玩家进入房间前就可生效，应优先归入 `Triggers`。
- 若一个新元素同时满足多个分类，以**主驱动 authority + 默认挂点** 为准；不要让同一个元素同时挂两条主流程。
- 历史旧术语只保留为兼容说明；后续新增文档与工具输出统一使用新分类名。

---

## 7. 运行时 authority 与数据流


### 7.1 数据流向图

```text
Door._targetRoom / Door._targetSpawnPoint
    │
    ├── DoorTransitionController 读取目标并执行过渡
    └── MinimapManager 从场景 Door 推导邻接关系

Room / RoomSO
    │
    ├── RoomManager 管理房间进入、激活与缓存
    ├── CameraDirector 读取房间边界与镜头限制
    └── Encounter / Hazard / Trigger 在房间语境内运作

WorldClock
    │
    └── WorldPhaseManager
            └── ScheduledBehaviour / Door / AmbienceController / RoomVariant

WorldProgressManager
    │
    └── WorldEventTrigger / Door / 永久世界改变

RoomFlagRegistry
    │
    └── SaveBridge ↔ ProgressSaveData.Flags
```

### 7.2 关键规则

1. **Door 是连接拓扑真相源**：传送与地图连线都从 Door 读事实
2. **Room 是空间真相源**：房间边界、导航点、镜头限制存在于场景对象
3. **RoomSO 是房间元数据真相源**：ID、名称、楼层、节点类型、附加配置来自 SO
4. **RoomFlagRegistry 是细粒度状态真相源**：交互件的持久状态统一收口到 Registry
5. **LevelEvents 是跨模块通知渠道**：跨系统通信走事件，不走直接引用

---

## 8. 模块边界与跨模块依赖

### 8.1 对外依赖

| 外部模块 | 依赖方式 | 依赖点 |
|---------|---------|-------|
| `ProjectArk.Core` | 程序集引用 | `ServiceLocator`、`IDamageable`、`DamagePayload`、`LevelEvents` |
| `ProjectArk.Core.Audio` | 程序集引用 | `AudioManager` |
| `ProjectArk.Core.Save` | 程序集引用 | `SaveManager`、`PlayerSaveData`、`ProgressSaveData` |
| `ProjectArk.Combat` | 程序集引用 | `EnemySpawner`、遭遇注入链 |
| `ProjectArk.Ship` | 程序集引用 | 玩家过渡输入、死亡与复位链 |
| `ProjectArk.Enemy` | 程序集引用 | `EnemyDirector`、敌人死亡广播 |
| `ProjectArk.Heat` | 程序集引用 | Checkpoint 恢复热量 |

### 8.2 对外暴露

| 暴露内容 | 消费者 | 暴露方式 |
|---------|--------|---------|
| `LevelEvents.OnRoomEntered` | UI / Save / Audio | Core 静态事件 |
| `LevelEvents.OnRoomCleared` | UI / Save | Core 静态事件 |
| `LevelEvents.OnBossDefeated` | UI / Save / WorldProgress | Core 静态事件 |
| `LevelEvents.OnCheckpointActivated` | UI / Save | Core 静态事件 |
| `LevelEvents.OnPhaseChanged` | UI / Audio / VFX | Core 静态事件 |
| `LevelEvents.OnWorldStageChanged` | UI / Save | Core 静态事件 |
| `RoomManager` | 任何需要当前房间信息的系统 | `ServiceLocator.Get` |
| `WorldClock` | UI / 事件系统 | `ServiceLocator.Get` |

### 8.3 禁止的依赖方向

- 禁止 `Level` 模块直接引用 UI 程序集
- 禁止 Core 层反向依赖 `Level` 程序集
- 禁止运行时使用 `FindAnyObjectByType` / `FindObjectOfType`
- 禁止把 Editor 快照或导入源当成运行时 authority

---

## 9. Editor 工具链 authority

### 9.1 工具矩阵

| 工具 | 职责 | 不负责 |
|------|------|--------|
| `LevelArchitectWindow` | `Design / Build / Validate / Quick Play` 统一入口 | 不承担运行时 authority |
| `LevelSliceBuilder` | 从 `LevelDesigner.html` JSON 导入 Room / RoomSO / Door 骨架 | 不作为运行时真相源 |
| `RoomFactory` | 创建标准房间根节点、导航节点、相机边界与战斗骨架 | 不替代已有场景的语义设计 |
| `DoorWiringService` | Door 双向目标引用连线 | 不规划节奏语义 |
| `LevelValidator` | 结构、引用、门连接、战斗房配置校验 | 不替代完整试玩 |
| `PacingOverlayRenderer` | 房间类型与连接语义可视化 | 只读，不写入 |
| `RoomBlockoutRenderer` | 白盒布局显示 | 只读，不写入 |
| `BlockoutModeHandler` | 白盒编辑交互 | 不承担数据权威 |
| `BatchEditPanel` | 批量编辑房间基础属性 | 不修改运行时逻辑 |
| `SceneScanner` | 扫描房间、门与场景结构 | 不自动修复设计语义 |
| `ScaffoldSceneBinder` | 将 Scene 骨架同步到 `LevelScaffoldData` 快照 | 不反向驱动 Scene |

### 9.2 工具执行原则

- 同一类任务只保留一个官方入口
- 工具默认优先 `Audit / Preview`，显式操作才 `Apply`
- 工具执行后必须说明：改了什么、没改什么、缺了什么
- 禁止在 `OnValidate`、`Awake`、`OnEnable`、Play Mode 启动流程中隐式写回资产

---

## 10. 校验基线

每次对 `Level` 模块做出结构性改动后，至少要满足：

1. `dotnet build Project-Ark.slnx` 通过
2. `LevelValidator` 没有阻塞型 `Error`
3. 每个 `Room` 内 `GateID` 唯一
4. `Door._targetRoom` 与 `Door._targetSpawnPoint` 非空
5. `MinimapManager` 能正确推导邻接关系
6. `SaveBridge` 的存读链不中断
7. `Quick Play` 仅作为结构 smoke test，完整试玩仍需手动验证

### 10.1 新增元素的 `LevelValidator` 接入规则

当一个新元素进入**标准 authoring 流程**后，只要满足以下任一条件，就必须扩展 `LevelValidator`：

- 缺失引用会直接导致流程中断、存档失效、门链断开、战斗无法启动或交互静默失效
- 元素依赖固定根节点、固定层级、固定 LayerMask、固定命名约定
- 元素与特定 `RoomNodeType`、`ConnectionType`、世界阶段或世界进度存在必需搭配关系
- Editor 工具会自动创建它的骨架，因此工具链必须能检测“骨架已生成但语义未补齐”的状态

**严重度约束：**

- **`Error`**：缺失后会破坏可玩主链、存档链、关键交互链
- **`Warning`**：功能还能运行，但语义不完整、反馈缺失、推荐接线缺失
- **`Info`**：可读性、组织性、命名一致性问题

**Auto-Fix 边界：**

- 可以自动修：标准根节点、Trigger 勾选、标准 Layer、可推导的空骨架
- 不可以自动猜：`EncounterSO`、目标门、关键 SO 资产、flag key 语义、策划意图字段

---

## 11. 相关文档


| 文档 | 职责 |
|------|------|
| `Docs/2_Design/Level/Level_CanonicalSpec.md` | Level 模块现役架构、声明、authority 规范 |
| `Docs/2_Design/Level/Level_WorkflowSpec.md` | 关卡搭建与验证的操作手册 |
| `Docs/Reference/Level_Architecture_Synthesis_Minishoot_Silksong_TUNIC.md` | 参考游戏分析与设计输入 |
| `Docs/3_LevelDesigns/` | 区域拓扑设计稿与 JSON 输入 |
| `Implement_rules.md` | 模块级实现约束与协作规则 |
| `Docs/5_ImplementationLog/ImplementationLog.md` | 文档与实现变更留痕 |
