# Level 房间元素现役 Finding

---

## 文档目的

本文件是 `Level` 模块房间元素的**现役 finding / 最新诊断入口**，用于持续沉淀当前模块的**发现、证据和结论**。

它重点回答两个问题：

- **我们当前定义上的房间元素有哪些？**
- **哪些已经在 Unity 中进入现役消费链，并且真的被场景实装了？**

从现在开始，这份文档采用**持续回写**方式维护：

- **新增 finding 直接回写本文件**
- **不再为同一模块继续滚动拆出新的按日期命名 Markdown**
- **需要保留历史结构化快照时，继续保留独立 CSV / 附件**

当前结论来自 3 层信息的交叉核对：

- **规范层**：`Level_CanonicalSpec.md`、`Level_WorkflowSpec.md`
- **运行时 / 编辑器链路**：`Room`、`RoomManager`、`SaveBridge`、`LevelValidator`、`LevelDesigner.html` 等脚本与工具
- **当前 Unity 场景实况**：`SampleScene` 的真实挂载统计与层级检查

补充：结构化筛选快照仍保留为 `Docs/6_Diagnostics/Level_RoomElements_Findings_2026-04-10.csv`，用于回看当日统计与矩阵筛选。

---

## 结论先说

**当前 `Level` 模块的房间元素体系在规范层已经收口完成，但 `SampleScene` 的 authoring 落地仍处于混合态 / 过渡态。**

当前最关键的结论有 7 条：

- **`SampleScene` 当前确实已有 17 个 `Room` 实例**，所以 `Level` 不是“只有代码没有场景”的状态。
- **只有 2/17 个房间完整具备 `Navigation / Elements / Encounters / Hazards / Decoration / Triggers / CameraConfiner` 标准根节点**，说明现役场景还没有全面迁入标准房间语法。
- 当前场景里真正已经铺开的主流房间元素集中在：**`Door`、`Checkpoint`、`Lock`、`PickupBase`、`EnemySpawner`、`ArenaController`、`EnvironmentHazard`**。
- **`DestroyableObject`、`OpenEncounterTrigger`、`BiomeTrigger`、`HiddenAreaMask`、`ScheduledBehaviour`、`ActivationGroup`、`WorldEventTrigger`、`CameraTrigger`** 等元素，代码侧已经具备或基本具备，但当前 `SampleScene` 里**尚未铺开**。
- **`Room` / `ArenaController` / `OpenEncounterTrigger` 的 encounter owner 已进一步收口**：现役口径是“`Room` 负责 room-level 入口、`ArenaController` 只做 ceremony、`OpenEncounterTrigger` 只做局部 open encounter”；同时 `_respawnOnReenter`、`RoomState.Locked`、无消费者的房间/遭遇弱事件已从现役链路中清除。
- **`LevelScaffoldData.rooms[].elements[]` 和 `ScaffoldElementType` 不是 Unity 当前运行时实装 authority**，不能把它们误判成“已经可被场景导入消费的现役房间元素”。
- **`LevelDesigner.html` 现已在 UX 层明确收口为“拓扑规划 + JSON 导入源”**：`elements[]` 与 `Zone / ACT / 张力 / Beat / 时长` 当前仍只应视为设计快照 / 设计备注，不会自动生成场景对象。
- **`Checkpoint` 链虽然已经进入现役场景，但配置还没有完全收尾**：当前已知有 3 个 `LevelValidator` 问题需要补。

---

## 验证口径

为了避免把“脚本存在”“系统支持”“场景已挂载”混成一层，本轮使用了以下 4 个判断维度：

- **规范定义**：是否已经被现役文档正式定义为房间元素家族的一部分
- **运行时消费**：是否进入 `Room` / `RoomManager` / `SaveBridge` / `WorldProgressManager` / `CameraDirector` 等现役链路
- **编辑器护栏**：是否已进入 `LevelValidator` 或明确的 authoring 约束
- **场景实例**：`SampleScene` 中当前是否真的有实例、是否已经铺开

只有当一个元素同时通过“代码可跑”和“场景已挂”两个条件时，才把它视为**当前现役可直接验证的房间元素**。

---

## 1. 当前定义上的房间元素分类

为了让后续 `Level` authoring、评审和验证更直观，现役 `Level` 文档已经把房间元素分类统一收口为**六大玩法家族 + 一类基础设施件**。

核心思路是：

- 六大类继续保留，作为 **顶层玩法元素分类**
- 再额外单列一类 **基础设施件**，避免把 `SpawnPoint`、`CameraConfiner` 这类运行支撑对象混进玩法元素里
- 代码类名保持具体，**分类标签可以更短、更人话、更适合 authoring**

### 1.1 现役分类（中文 / 英文）

| 中文分类 | 英文标签 | 历史旧术语（兼容说明） | 一句话判断标准 |
| --- | --- | --- | --- |
| **通路件** | **Path** | `Door / Gate` | 它是不是在控制玩家怎么去别的地方？ |
| **交互件** | **Interact** | `Interact Anchor` | 玩家是不是要主动碰它、按它、拿它？ |
| **状态件** | **Stateful** | `Persistent Room Element` | 房间是不是需要记住它已经变了？ |
| **战斗件** | **Combat** | `Encounter Element` | 它是不是在决定什么时候开打、怎么打、什么时候结束？ |
| **环境机关件** | **Environment** | `Hazard Element` | 它是不是在持续改变玩家的通行、生存或移动条件？ |
| **导演件** | **Directing** | `Trigger / Director` | 它是不是在控制镜头、氛围、相位或演出状态？ |
| **基础设施件** | **Infrastructure** | 无 | 它是不是在支撑房间系统运转，而不是直接提供玩法对象？ |

### 1.2 为什么英文命名可以保持简洁

**可以，而且现役分类层就应该用这种简洁英文。**

这里要区分两层命名：

- **分类名 / authoring 标签**：可以短，目标是好读、好记、好筛选，例如 `Path`、`Interact`、`Combat`
- **代码类名 / 具体组件名**：保持具体，目标是语义精确，例如 `OpenEncounterTrigger`、`WorldEventTrigger`、`RoomCameraConfiner`

也就是说：

- `Path` / `Interact` / `Stateful` / `Combat` / `Environment` / `Directing`
- 适合做：文档标题、表格列、Inspector 分组、Validator 输出、Level 设计沟通

而不是用它们去替代所有具体组件名。

### 1.3 按新命名重新看当前元素落位

- **通路件（`Path`）**：`Door`
- **交互件（`Interact`）**：`Checkpoint`、`Lock`、`PickupBase` 派生物
- **状态件（`Stateful`）**：`DestroyableObject`
- **战斗件（`Combat`）**：`OpenEncounterTrigger`、`ArenaController`、`EnemySpawner`
- **环境机关件（`Environment`）**：`EnvironmentHazard` 及其子类
- **导演件（`Directing`）**：`BiomeTrigger`、`HiddenAreaMask`、`ScheduledBehaviour`、`ActivationGroup`、`WorldEventTrigger`、`CameraTrigger`

### 1.4 需要单列出来的基础设施件

以下对象很重要，但更适合被理解为**房间基础设施**，而不是玩法六类的一部分：

- **`SpawnPoint`**
- **`CameraConfiner`**

它们的职责是支撑房间系统运转，而不是直接提供一个玩法对象。

### 1.5 当前的占位 / 特例

- **`NarrativeFallTrigger`**：当前仍应视为占位中的叙事触发器，不应算现役可交付房间元素

---

## 2. 运行时消费链的真实分层

### 2.1 `Room.cs` 代码实查：主动收集 vs 直接依赖

这次专门按 `Room.cs` 的真实实现，把“主动收集”和“直接依赖”拆开记，避免后续把 `Room` 会扫描到的对象、显式 Inspector 依赖、以及 `Room` 的下游消费者混成一层。

#### 2.1.1 `CollectSceneReferences()` 当前主动收集的元素

`Room` 在 `Awake()` 中调用 `CollectSceneReferences()`，当前**主动扫描 / 缓存**的是：

- **`Door`**：优先从 `Navigation` 子树收集；若该根不存在或没有结果，再回退到整个 `Room` 子树
- **`EnemySpawner`**：优先从 `Encounters` 子树取第一个；找不到再回退到整个 `Room` 子树
- **`OpenEncounterTrigger`**：优先从 `Encounters` 子树收集；否则回退到整个 `Room` 子树
- **`DestroyableObject`**：优先从 `Elements` 子树收集；否则回退到整个 `Room` 子树
- **`SpawnPoint`**：按固定优先级收集：
  1. `Encounters/SpawnPoints` 的直接子物体
  2. `Navigation` 子树下所有命名以 `SpawnPoint_` 开头的节点
  3. Inspector 里预先序列化的 `_spawnPoints`

**这里最重要的口径是：** 当前 `Room` 主动收集链只有这 5 类，不包括 `Checkpoint`、`Lock`、`PickupBase`、`EnvironmentHazard`、`BiomeTrigger` 等其它房间元素。

#### 2.1.2 不是主动收集，但属于 `Room` 的直接依赖

以下对象不是 `CollectSceneReferences()` 扫出来的，但仍然是 `Room.cs` 的**直接依赖 / 直接持有配置**：

- **`RoomSO` (`_data`)**：房间元数据 authority，提供 `RoomID`、`RoomNodeType`、默认 `EncounterSO`
- **`Collider2D` (`_confinerBounds`)**：镜头边界引用，走 Inspector 显式接线，不是运行时自动搜索
- **`RoomVariantSO[]` (`_variants`)**：相位驱动的房间变体配置
- **`GameObject[]` (`_variantEnvironments`)**：变体环境子物体开关目标
- **`LayerMask` (`_playerLayer`)**：房间 Trigger 的玩家层过滤
- **`BoxCollider2D`**：通过 `[RequireComponent(typeof(BoxCollider2D))]` + `Awake()` 自检强制存在，并被修正为 `isTrigger = true`

换句话说，**`CameraConfiner` 不应再被记成 `Room` 主动收集元素**；更准确的说法是：`Room` 直接依赖一个显式接线的 `ConfinerBounds` 引用，而 `RoomCameraConfiner` 运行时再去读取它。

#### 2.1.3 `Room` 运行时直接调度 / 直接调用的类型与系统

除了场景元素，`Room.cs` 当前还直接依赖这些运行时类型 / 系统：

- **`EncounterSO`**：来自 `RoomSO` 或 `RoomVariantSO.OverrideEncounter`，其中 `RoomSO Encounter` 现役语义为 room-owned `Closed` encounter
- **`WaveSpawnStrategy`**：由 `Room` 统一创建 room-owned encounter；`OpenEncounterTrigger` 仍为局部 open encounter 单独创建。`ArenaController` 不再直接 `new WaveSpawnStrategy`
- **`RoomManager`**：通过 `ServiceLocator.Get<RoomManager>()` 在清房时直接通知 `NotifyRoomCleared(this)`
- **`WorldPhaseManager`**：通过 `ServiceLocator.Get<WorldPhaseManager>()` 在启动时拉当前 phase
- **`LevelEvents.OnPhaseChanged`**：`Room` 直接订阅，用来切换变体
- **`DoorState` / `RoomState`**：`Room` 直接读写门状态，并只维护 `Undiscovered / Entered / Cleared` 三态房间状态

因此，如果严格按“`Room.cs` 自己显式出现并直接调用/订阅”的标准来算，`Room` 的直接依赖不只是一组场景组件，还包括这条**相位切换 + 遭遇完成 + 清房通知**的运行时主链。

#### 2.1.4 当前不应误算成 `Room` 直接依赖的对象

以下对象虽然和房间体系密切相关，但**不应记成 `Room.cs` 当前直接依赖**：

- **自治元素**：`Checkpoint`、`Lock`、`PickupBase`、`EnvironmentHazard`、`BiomeTrigger`、`HiddenAreaMask`、`ScheduledBehaviour`、`ActivationGroup`、`WorldEventTrigger`、`CameraTrigger`
- **`Room` 的下游消费者**：`MinimapManager`、`RoomCameraConfiner`、`RoomFactory`
- **编辑器工具**：`LevelValidator`、`DoorWiringService`、`LevelArchitectWindow`

这些对象里，有的是组件自治，有的是别的系统来读取 `Room` 暴露的数据；**它们依赖 `Room`，不等于 `Room` 直接依赖它们。**

### 2.2 不一定进 `Room` 主链，但已能独立工作的元素

以下元素不一定由 `Room.CollectSceneReferences()` 主动收集，但它们已经具备独立运行逻辑，可以直接挂在场景中使用：

- **`Checkpoint`**：走 `CheckpointManager` + `SaveBridge`
- **`Lock`**：走 `Door` + `KeyInventory`
- **`PickupBase`** 派生物：组件自治
- **`EnvironmentHazard`** 子类：组件自治
- **`BiomeTrigger`**：环境 / 音频 / 后处理切换
- **`HiddenAreaMask`**：隐藏区域表现控制
- **`ScheduledBehaviour`**：世界相位驱动
- **`ActivationGroup`**：房间进入 / 离开时激活控制
- **`WorldEventTrigger`**：世界进度驱动
- **`CameraTrigger`**：镜头导演链

### 2.3 已经接入状态通道 / 持久化链的元素

这批元素的价值更高，因为它们不仅“能工作”，还已经接入状态系统：

- **`DestroyableObject`** → `RoomFlagRegistry` → `SaveBridge`
- **`Checkpoint`** → `CheckpointManager` → `SaveBridge`
- **`WorldEventTrigger`** → 世界进度 / 自持久化链

---

## 3. `SampleScene` 当前实际挂载结果

### 3.1 房间整体情况

- **房间实例数**：17 个 `Room`
- **完整标准根节点覆盖率**：2 / 17

这说明当前场景不是空壳，但也说明现役房间 authoring 还没有全面迁到规范定义的标准结构。

### 3.2 当前场景里已经有实例的元素

- **`Door`**：大量
- **`Checkpoint`**：2
- **`Lock`**：1
- **`PickupBase` 派生物**：2
- **`ArenaController`**：1
- **`EnemySpawner`**：2
- **`EnvironmentHazard`**：1

这批元素构成了当前 `SampleScene` 的主流可验证集合。

### 3.3 当前场景里实例数为 0 的元素

- **`DestroyableObject`**：0
- **`OpenEncounterTrigger`**：0
- **`BiomeTrigger`**：0
- **`HiddenAreaMask`**：0
- **`ScheduledBehaviour`**：0
- **`ActivationGroup`**：0
- **`WorldEventTrigger`**：0
- **`CameraTrigger`**：0
- **`NarrativeFallTrigger`**：0

因此，这批元素当前只能算**系统已支持，但场景内容尚未铺开**。

---

## 4. 一个非常关键的发现：现役场景仍是混合 authoring 态

按现役规范，一个标准房间应具备以下根节点：

- `Navigation`
- `Elements`
- `Encounters`
- `Hazards`
- `Decoration`
- `Triggers`
- `CameraConfiner`

但实际场景检查显示：

- **只有 2 个房间完整满足这套结构**
- 其余 **15 个房间**仍有不同程度的缺失
- 像 `Room_Start` 这类旧房间，很多元素仍然是**直接挂在 `Room` 根下**
- `--- Sheba Level ---` 这一批新房间更接近标准语法，但整体还没有完全统一

所以当前 `Level` 的真实状态不是“规范缺失”，而是：

- **规范已经有了**
- **系统已经支持了**
- **现役场景 authoring 还没有完全迁过去**

这也是为什么代码里你会看到很完整的元素家族，但场景里真正稳定落地的只是一部分。

---

## 5. 逐元素结论矩阵（使用更清晰分类）

| 元素 | 清晰分类 | 英文标签 | 默认挂点 | 运行时 owner / 入口 | 进入 `Room` 主链 | `LevelValidator` 覆盖 | `SampleScene` 实例 | 当前判断 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `Door` | 通路件 | `Path` | `Navigation` | `Room + RoomManager + Door` | 是 | 是 | 大量 | **现役成熟** |
| `SpawnPoint` | 基础设施件 | `Infrastructure` | `Navigation/SpawnPoints` + `Encounters/SpawnPoints` | `Room + Door + RoomManager` | 是 | 部分 | 未单独统计 | **现役基础设施** |
| `CameraConfiner` | 基础设施件 | `Infrastructure` | `CameraConfiner` | `RoomCameraConfiner + CameraDirector` | 是 | 部分 | 未单独统计 | **现役基础设施** |
| `Checkpoint` | 交互件 | `Interact` | `Elements` | `Checkpoint + CheckpointManager` | 否 | 是 | 2 | **现役成熟，但配置未完全收尾** |
| `Lock` | 交互件 | `Interact` | `Elements` | `Lock + Door + KeyInventory` | 否 | 是 | 1 | **现役成熟** |
| `PickupBase` | 交互件 | `Interact` | `Elements` | `PickupBase` 派生物 | 否 | 否 | 2 | **现役成熟，但未纳入官方护栏** |
| `DestroyableObject` | 状态件 | `Stateful` | `Elements` | `DestroyableObject + RoomFlagRegistry` | 是 | 是 | 0 | **代码已支持，但场景未铺开** |
| `OpenEncounterTrigger` | 战斗件 | `Combat` | `Encounters` | `OpenEncounterTrigger + EnemySpawner` | 是 | 是 | 0 | **代码已支持，但场景未铺开；现役语义为局部 `Open` encounter owner** |
| `ArenaController` | 战斗件 | `Combat` | `Encounters / room` | `Room + ArenaController + RoomManager` | 部分 | 是 | 1 | **现役成熟；当前只负责 Arena/Boss ceremony，不再直接持有刷怪 owner** |
| `EnemySpawner` | 战斗件 | `Combat` | `Encounters` | `Room / OpenEncounterTrigger` | 是 | 是 | 2 | **现役成熟；由 room-level 或 local open encounter owner 驱动** |
| `EnvironmentHazard` | 环境机关件 | `Environment` | `Hazards` | `EnvironmentHazard` 子类 | 否 | 是 | 1 | **现役成熟** |
| `BiomeTrigger` | 导演件 | `Directing` | `Triggers` | `BiomeTrigger + AmbienceController` | 否 | 是 | 0 | **代码已支持，但场景未铺开** |
| `HiddenAreaMask` | 导演件 | `Directing` | `Triggers` | `HiddenAreaMask` | 否 | 是 | 0 | **代码已支持，但场景未铺开** |
| `ScheduledBehaviour` | 导演件 | `Directing` | `Triggers` | `ScheduledBehaviour + WorldPhaseManager` | 否 | 是 | 0 | **代码已支持，但场景未铺开** |
| `ActivationGroup` | 导演件 | `Directing` | `Triggers / ActivationGroups` | `ActivationGroup + RoomManager.OnCurrentRoomChanged` | 否 | 是 | 0 | **代码已支持，但场景未铺开** |
| `WorldEventTrigger` | 导演件 | `Directing` | `Triggers` | `WorldEventTrigger + WorldProgressManager` | 否 | 是 | 0 | **代码已支持，但场景未铺开** |
| `CameraTrigger` | 导演件 | `Directing` | `Triggers` | `CameraTrigger + CameraDirector` | 否 | 否 | 0 | **代码已支持，但场景未铺开** |
| `NarrativeFallTrigger` | 叙事特例 | `Narrative` | `Triggers` 或 custom | `NarrativeFallTrigger` | 否 | 否 | 0 | **占位 / 半实现** |

---

## 6. 哪些不能算“当前 Unity 实际可消费并实装”的现役房间元素

### 6.1 编辑期 schema，不是运行时 authority

以下对象不应被视为当前 Unity 运行时实装主链：

- **`LevelScaffoldData.rooms[].elements[]`**
- **`ScaffoldElementType`**

原因很明确：

- `LevelDesigner_JSON_Field_Matrix.csv` 已明确写明：`rooms[].elements[].type` 与 `rooms[].elements[].position` 当前**不会被 Unity 导入**
- `LevelSliceBuilder` 当前只会创建空的 `Elements` 根节点，**不会根据这批元素数据自动生成场景对象**

因此，它们目前更像：

- **设计快照**
- **工具侧 schema**
- **编辑期模型**

而不是现役 Scene authoring 的 authority。

### 6.2 占位 / 半实现元素

- **`NarrativeFallTrigger`** 当前应视为 placeholder
- 它还不是完整的叙事 cinematic 元素，不应被纳入现役可交付房间元素集合

---

## 7. 当前暴露出来的实际配置问题

本轮运行 `LevelValidator` 后，当前场景已知问题共 **3 个**，全部集中在 `Checkpoint` 链：

- **`Checkpoint_Hub` collider 不是 Trigger**
- **`Checkpoint_Hub` 缺 `CheckpointSO`**
- **`Checkpoint_Start` 缺 `CheckpointSO`**

这说明：

- `Checkpoint` 已经是**现役场景的一部分**
- 但它的场景配置还没有完全收尾
- 如果后续进入“场景配置与验证阶段”的收尾工作，`Checkpoint` 应该是优先修的对象之一

---

## 8. 最终判断

### 从模块设计角度

**房间元素体系已经完整。** 以“六大玩法家族 + 一类基础设施件”来理解当前 `Level` 是成立的，模块已经具备一套清晰、可继续收口的现役 authoring 词法。

### 从运行时消费角度

**真正被 `Room` 主链强消费的仍是少数核心元素，但必须区分“主动收集”和“直接依赖”两层。** 当前更准确的口径是：

- **主动收集链**：`Door`、`EnemySpawner`、`OpenEncounterTrigger`、`DestroyableObject`、`SpawnPoint`
- **直接但非收集依赖**：`RoomSO`、`ConfinerBounds`、`RoomVariantSO`、`WaveSpawnStrategy`、`RoomManager`、`WorldPhaseManager`、`LevelEvents.OnPhaseChanged`

### 从 `SampleScene` 的现役内容角度

**当前最成熟、最适合继续验证的房间元素集合是：**

- `Door`
- `Checkpoint`
- `Lock`
- `PickupBase`
- `EnemySpawner`
- `ArenaController`
- `EnvironmentHazard`

### 从 authoring 成熟度角度

**当前场景仍然是“可玩，但未完全规范化”的过渡态。**

- 规范已经到位
- 系统已经支持
- 护栏已经开始收口
- 工具侧边界提示也已开始收口
- 但场景 authoring 还没有完全迁到标准根节点结构

因此，如果下一步继续做 `Level` 模块验证，最有价值的方向不是再去泛泛梳理脚本，而是：

- **继续补齐现役场景的标准根节点 authoring**
- **把已经有代码支持但未铺开的元素逐步放进 `SampleScene` 或测试房间验证**
- **优先修掉 `Checkpoint` 的已知配置问题**

---

## 9. 建议的后续验证优先级

建议按以下顺序继续推进：

1. **先修 `Checkpoint` 的 3 个已知配置问题**
2. **把 `Door / Checkpoint / Lock / Pickup / Arena / Spawner / Hazard` 这批现役成熟元素做一轮完整房间验证**
3. **开始挑一个代表房间，把根节点结构迁到标准语法**
4. **再选一批“代码已支持但未铺开”的元素做场景试装**：
   - `OpenEncounterTrigger`
   - `DestroyableObject`
   - `BiomeTrigger`
   - `CameraTrigger`
   - `ScheduledBehaviour`
   - `WorldEventTrigger`

这样能更快把 `Level` 从“系统已完成”推进到“场景 authoring 也真正完成”。
