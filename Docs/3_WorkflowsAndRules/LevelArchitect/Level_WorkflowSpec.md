# 关卡搭建工作流手册 (Level Workflow Spec)

> **文档目的**：指导《静默方舟》关卡在 Unity Editor 中的实际搭建流程，让设计入口、场景落地、验证方式和职责边界保持一致。
> 
> **文档定位**：这是一份操作手册，回答“现在怎么搭、怎么验、哪里是权威”。

---

## 1. 文档定位与 authority

### 1.1 这份文档负责什么

这份文档只负责四件事：

1. 说明搭建入口：`Level Architect` 作为唯一现役白盒与精修工作面
2. 说明通用流程：结构搭建 → 语义标注 → Overlay 检查 → Validate → Quick Play → 完整试玩
3. 说明 authoring 约束：哪些层级是工具链默认要求，哪些数据由谁持有 authority
4. 说明验证边界：不同验证手段各自验证什么，不验证什么

### 1.2 这份文档不负责什么

- 运行时架构与长期模块边界定义
- C# 代码的完整字段真相源
- 区域内容设计稿与关卡节奏策划文本
- 过程性历史说明与版本追溯

### 1.3 Authority 表

| 主题 | 真相源 | 说明 |
|------|--------|------|
| 房间空间布局 | Scene 中的 `Room` GameObject | 房间位置、尺寸、边界、子节点结构以场景为准 |
| 房间连接拓扑 | Scene 中 `Door._targetRoom` / `_targetSpawnPoint` | `Door` 是显式门型连接关系 authority；非门型 trap / fall / teleport 不自动进入这层拓扑 |
| 房间元数据 | `RoomSO` / `EncounterSO` / `CheckpointSO` / `WorldPhaseSO` 等 SO | 非空间配置、节奏类型、遭遇配置以 SO 为准 |
| Authoring 工作面 | `Level Architect`（`Build / Quick Edit / Validate`） | 当前唯一现役搭建与精修入口 |
| 架构规则与模块边界 | `Level_CanonicalSpec.md` | 架构、声明、authority 规范 |

### 1.4 一句话原则

**`WorkflowSpec` 负责“怎么搭”，`CanonicalSpec` 负责“谁说了算”。**

---

## 2. 工作流总览

关卡搭建当前只有一条现役入口路径：

```text
Level Architect / Build Tab → Blockout / Connect → Quick Edit → Scene 落地

统一收口流程：
结构搭建 Pass → 语义标注 Pass → Overlay 检查 → Validate → Quick Play → 完整 Play Mode 验收
```

### 2.1 核心原则

- **先跑通结构，再填内容**：先让玩家能进、能走、能回，再补战斗、锁钥和氛围
- **先明确房间职责，再堆装饰**：先定 `RoomNodeType` 与 `ConnectionType`
- **Quick Play 只是 smoke test**：它只验证结构，不等于完整验收
- **切片必须形成节奏闭环**：至少能读出“进入 → 施压 → 证明 → 回报 / 回路 / 收束”主链

### 2.2 最小可玩切片定义

一个切片至少满足以下条件，才算完成第一版：

- 关键房间之间可通过 Door 正常往返
- 关键 Door 具有有效的 `_targetRoom` 与 `_targetSpawnPoint`
- 主房间能明确标注 `RoomNodeType`
- `Validate` 没有阻塞型错误
- 至少完成一次 `Quick Play` 和一次完整 Play Mode 走图

---

## 3. 5 分钟快速上手

### 3.1 什么时候直接进入 `Level Architect`

当前所有现役关卡搭建都应直接进入 **`Level Architect` 主链**，尤其适用于：

- 需要在 Unity 里直接白盒迭代
- 需要边画边试，快速验证结构手感
- 需要把房间、连接、语义补件与验证收口在同一工作面

### 3.2 默认建议

- **新区域 / 新章节**：直接从 `Build` 起手，先搭最小可玩骨架
- **局部重做 / 单切片验证**：优先用 `Validation Slice` 或局部 `Blockout`
- **复杂项目中后期**：仍然沿 `Build → Quick Edit → Validate` 同一主链推进，不再额外维护外部导入分支

---

## 4. 当前唯一工作流：`Level Architect / Build Tab → 白盒搭建`

### 4.1 适用场景

适合“直接在 Unity 中搭出可玩骨架，并立即进入验证”的工作方式。

### 4.2 操作步骤

1. **打开工具**
   - Unity 菜单：`ProjectArk > Level > Authority > Level Architect`
   - 进入 `Build` Tab

2. **准备房间预设或直接起一个最小验证切片**
   - 点击 **`Create Built-in Presets`** 生成内置 `RoomPresetSO`
   - 若这轮目标是先验证 authoring 闭环，可直接使用 **`5-Room Validation Slice`** / **`Create + Validate`** / **`Create + Quick Play`** 作为起手模板

3. **Blockout 模式白盒**
   - 切到 `Blockout`
   - 使用房间/走廊笔刷放置基础房间
   - 利用自动吸附和相邻自动连线快速形成主链
   - 若是从 `5-Room Validation Slice` 起手，可在现成闭环上继续扩图，而不是每次从空场景重搭

4. **Connect 模式补语义连接**
   - 切到 `Connect`
   - 从一个房间拖到另一个房间补建门对
   - 用于修正自动连线未覆盖的结构，或手动增加特殊连接

5. **Select / Quick Edit 模式精修**
   - 调整尺寸、楼层、房间类型、遭遇配置
   - 用侧边面板或批量编辑进行整理
   - 使用 `Room ID` / `Display Name` 与 `Stable Rename` 保持房间命名按语义可读，而不是靠时间戳或位置记忆
   - 若当前进入静态墙 authoring 阶段，优先使用 `Geometry Authoring` 区块在 `Navigation/Geometry/OuterWalls` 或 `Navigation/Geometry/InnerWalls` 下创建标准 Tilemap 画布；它只负责准备宿主与标准碰撞链，不自动生成墙形
   - 若当前进入语义补件阶段，可直接用 `Runtime Assist` / `Starter Objects` 入口补 `Checkpoint`、`OpenEncounterTrigger`、`HiddenAreaMask`、`BiomeTrigger`、`ScheduledBehaviour`、`WorldEventTrigger`，连接 inspector 里则可直接补 `Lock` starter；其中隐藏遮挡 reveal 仍归 `Triggers` 的导演件，不进入实体墙 authoring 主链
   - `Door` 型通路需要与几何开口协作；trap / fall / teleport 这类非门型跨房间转移不走 `Door` 门洞规则，通常归入 `Triggers` 或对应玩法层

### 4.3 路径特点

- 迭代快
- 离手感最近
- 更适合小范围重做、切片打磨、结构改版

### 4.4 风险提醒

- 只搭结构、不补语义，最终会得到“能通但节奏不清楚”的地图
- 只看 SceneView 白盒、不做验证，会高估当前完成度

### 4.5 历史材料处理原则

- 若文档、笔记或历史截图描述了当前工作面之外的额外搭建路径，应统一视为历史材料，而不是现役操作说明
- 当前团队沟通、验收与教学都应只围绕 `Level Architect` 主链展开

---

## 6. 通用 Build 流程

沿 `Level Architect` 主链落到场景后，建议按下面 4 个阶段推进。

### 6.1 Phase 1：结构搭建 Pass

目标：确认“房间作为空间结构是否成立”。

**这一阶段要完成：**

- 房间位置、尺寸、楼层关系正确
- 每个新房间都应带有 `Navigation/Geometry` 骨架；静态几何墙统一 author 在 `Navigation/Geometry/OuterWalls` 与 `Navigation/Geometry/InnerWalls`
- `Navigation/Geometry` 应保持 marker-only，只挂 `RoomGeometryRoot`，不要把玩法脚本或状态组件塞在这个根上
- 需要标准墙宿主时，优先在 `Quick Edit` 的 `Geometry Authoring` 区块创建 `OuterWalls_Main` / `InnerWalls_Main` starter，再手工绘制墙形
- 外轮廓主墙优先放在 `OuterWalls`，并使用统一的 `Tilemap + TilemapCollider2D (+ Static Rigidbody2D + CompositeCollider2D)` 组合
- 主路线与回路的 `Door` 已打通，且 `Door` 型通路应与几何开口协作，不能直接压在填充墙 Tile 上
- `CameraConfiner`、`BoxCollider2D`、标准根节点存在
- 至少可从入口穿过一条有效主链

**常用工具：**

- `Blockout` / `Connect` / `Select`
- `Floor Level` 过滤器
- `Create Built-in Presets`
- `5-Room Validation Slice`
- `Stable Rename` / `Room ID` / `Display Name`
- 右键菜单 / Batch Edit

### 6.2 Phase 2：语义标注 Pass

目标：把“能走的图”变成“可读的关卡”。

**这一阶段要补齐：**

- `RoomNodeType`
- `ConnectionType`
- `TransitionCeremony`
- `EncounterSO`
- Entry / Boss / Safe 等关键节点语义
- 需要时用 `Runtime Assist` 创建标准 starter，再手动补齐 `CheckpointSO`、`KeyItemSO`、`EncounterSO`、`RoomAmbienceSO`，以及 `HiddenAreaMask` 所需的遮挡美术与 reveal footprint 等业务配置

**判断标准：**

如果你还只能说“这是个大房间 / 小房间 / 长走廊”，说明还停留在结构层；只有当你能说“这是一个 `Combat` 通往 `Arena` 的推进段”，语义层才算完成。

### 6.3 Phase 3：Overlay 可视化检查

`Build` Tab 提供 4 类叠层：

| 叠层 | 作用 | 适合回答的问题 |
|------|------|----------------|
| `Pacing Overlay` | 用房间颜色与标注检查节奏分布 | 一段切片是不是全是一个味道？ |
| `Critical Path` | 高亮主路径 | 入口到核心目标的最短路径是否清晰？ |
| `Lock-Key Graph` | 查看锁钥关系 | 锁门和回报是否成闭环？ |
| `Connection Types` | 按连接语义着色 | 这里是推进、回返，还是能力门 / 时间门？ |

**注意**：`Overlay` 是读图工具，不替代 `Validate`。

### 6.4 Phase 4：Validate → Quick Play → 完整试玩

最后三步的职责分别是：

1. **`Validate`**：查缺失引用、结构错误、接线错误
2. **`Quick Play`**：做一轮结构 smoke test
3. **完整 Play Mode 手动验收**：验证战斗、过渡、地图、存档、世界时钟等真实联动

---

## 7. `Overlay`、`Validate`、`Quick Play`、完整试玩的边界

### 7.1 四种验证手段分别验证什么

| 手段 | 主要目的 | 不负责什么 |
|------|----------|------------|
| `Overlay` | 看节奏分布、主路径、连接语义、锁钥关系 | 不判断配置是否完整 |
| `Validate` | 查房间结构、Door 引用、必要组件、战斗房配置 | 不验证玩家实际体验是否成立 |
| `Quick Play` | 快速进 Play Mode，验证结构是否能跑 | 不保证完整游戏上下文已就绪 |
| 完整 Play Mode 验收 | 验证真实游玩闭环和跨系统联动 | 成本最高，不能替代前面三步 |

### 7.2 `LevelValidator` 检查项

| 检查项 | 级别 | Auto-Fix |
|--------|------|----------|
| Room 缺 `RoomSO` | Error | ✅ |
| Room 缺 `BoxCollider2D` / Collider 未设为 Trigger | Error / Warning | ✅ |
| 缺 `CameraConfiner` 或层级不正确 | Warning | ✅ |
| 缺标准根节点：`Navigation / Elements / Encounters / Hazards / Decoration / Triggers` | Warning | ✅ |
| Door 非双向连接 | Error | ✅ |
| Door 位于非 `Navigation` 推荐根下 | Warning | ✅ |
| Door 落在填充静态墙 Tile 内 | Warning | ❌ |
| `Arena` / `Boss` 房缺 `ArenaController` | Warning | ✅ |
| 非 `Arena` / `Boss` 房误挂 `ArenaController` | Warning | ❌ |
| `Arena` / `Boss` 房缺 `EncounterSO` | Warning | ❌ |
| `Arena` / `Boss` 房缺 `EnemySpawner` | Warning | ✅ |
| `RoomSO Encounter` 使用非 `Closed` 模式 | Warning | ❌ |
| `OpenEncounterTrigger` 使用非 `Open` 模式 | Warning | ❌ |
| `RoomSO Encounter` 与 `OpenEncounterTrigger` 混用 | Warning | ❌ |
| Door 的 `_playerLayer` 未配置 | Warning | ✅ |
| Door 的 `_targetSpawnPoint` 为空 | Error | ❌ |
| 房间孤立（没有 Door 连接） | Info | ❌ |

**当前补充：**

- `LevelValidator` 还会检查 `BiomeTrigger`、`HiddenAreaMask`、`ScheduledBehaviour`、`ActivationGroup`、`WorldEventTrigger` 的基础引用与推荐挂点。
- 几何开口校验当前只约束 `Door` 这类显式门型通路；`NarrativeFallTrigger` 等非门型跨房间转移不会因为“没有门洞”被误报。
- `CameraTrigger` 当前**尚未进入同等级 validator 收口**；镜头导演区的层级、player layer 与局部镜头效果仍需要人工走查。

### 7.3 `Quick Play` 的准确定位

`Quick Play` 会：

- 检查场景中是否存在 `RoomManager`
- 检查场景中是否存在 `DoorTransitionController`
- 缺少时自动创建临时对象（`_QuickPlay_RoomManager` / `_QuickPlay_DoorTransition`）
- 直接进入 Play Mode

因此它的定位是：

**`Quick Play = 结构 smoke test`**

它适合快速确认：

- Door 能不能进
- 房间之间能不能切过去
- 基础结构是否能在 Play Mode 跑起来

它不等于以下内容已经完成：

- 起始出生点与切片上下文完整
- 存档、Checkpoint、WorldClock、WorldPhase、DynamicWorld 都已接好
- 战斗节奏、资源曲线、地图信息已经合理

### 7.4 完整 Play Mode 验收建议

如果一个切片要进入可交付试玩状态，至少人工走一遍：

- 入口 → 主路线 → 回报 / 回路 → 收束
- 至少一场实际遭遇（如适用）
- 至少一次门过渡或层间过渡
- Checkpoint / Save / Respawn（如该切片覆盖这些系统）
- 若涉及世界阶段，则验证 `WorldClock / WorldPhaseManager / ScheduledBehaviour`

### 7.5 新增元素的 `Validator` 接入规则

当你把一个新元素加入到**标准搭建流程**时，必须同步判断它是否进入 `LevelValidator`：

| 情况 | 严重度 | Auto-Fix 原则 |
|------|--------|---------------|
| 缺失后会让主链不可玩（门断链、交互失效、遭遇无法启动、存档链断） | `Error` | 不自动猜语义字段，只报错并阻断 |
| 功能可跑，但缺少推荐接线、Layer、Trigger、标准挂点 | `Warning` | 可自动补结构默认值 |
| 只是命名、组织、可读性不一致 | `Info` | 通常不 Auto-Fix |

**操作原则：**

1. 新元素如果有**必填引用**，就必须有对应校验。
2. 新元素如果要求固定挂点（如必须在 `Encounters` 或 `Triggers`），就必须有层级校验。
3. 新元素如果依赖房间类型、世界阶段或世界进度，就必须有语义校验，而不是只检查组件是否存在。
4. `LevelValidator` 只自动修**结构默认值**，不替代策划/设计判断。

---

## 8. 标准 authoring 结构


### 8.1 `Room` 的官方最小结构

```text
Room_[ID]
├── Navigation
│   ├── Geometry            (`RoomGeometryRoot`，marker-only)
│   │   ├── OuterWalls
│   │   └── InnerWalls
│   ├── Doors
│   └── SpawnPoints
├── Elements
├── Encounters
│   ├── SpawnPoints          (可选)
│   └── EnemySpawner         (战斗房常见)
├── Hazards
├── Decoration
├── Triggers
└── CameraConfiner
```

### 8.2 结构含义

| 节点 | 职责 |
|------|------|
| `Navigation` | Door、进入点、离开点，以及 `Navigation/Geometry` 下的静态几何宿主 |
| `Elements` | 锁、拾取物、互动件、可破坏物、NPC |
| `Encounters` | 遭遇触发器、`OpenEncounterTrigger`、`EnemySpawner` |
| `Hazards` | 环境伤害与危险物 |
| `Decoration` | 纯视觉 / 氛围内容 |
| `Triggers` | Camera、Biome、事件、隐藏区域等触发器 |
| `CameraConfiner` | 当前房间镜头边界 |

### 8.2.1 战斗件 authoring owner 口径（现役）

- **`Room.ActivateEnemies()` 是 room-level combat 的唯一运行时入口**；不要再从外部直接拼第二套 arena / 普通房启动链。
- **`RoomSO.Encounter` = room-owned encounter 配置**，现役语义统一按 `EncounterSO.Mode = Closed` authoring。
- **`ArenaController` = `Arena` / `Boss` 房的 ceremony orchestrator**，只负责锁门、延迟、奖励与清房时机；它不是第二个刷怪 owner。
- **`OpenEncounterTrigger` = 局部 open encounter owner**，使用独立 `EncounterSO`，并按 `EncounterSO.Mode = Open` authoring；不要和 `RoomSO Encounter` 混用在同一房间里。

### 8.3 可扩展内容层

`Tilemaps`、`Variant_xxx`、`ActivationGroups` 等内容层可以存在，但不能替代标准根节点职责。

### 8.4 新增房间元素接入 SOP（严格版）

每次新增一个房间元素，都按以下顺序收口：

1. **先选房间元素分类**
   - 先判断它属于 `Path`、`Interact`、`Stateful`、`Combat`、`Environment`、`Directing`、`Infrastructure` 中的哪一类。
   - 如果现役分类能覆盖，就不要新开“特殊逻辑分支”。

2. **再定场景挂点**
   - `Navigation`：导航事实、门、进出点
   - `Elements`：互动件、锁、拾取物、持久化机关
   - `Encounters`：遭遇触发、Spawner、Arena 骨架
   - `Hazards`：环境伤害与危险物
   - `Triggers`：必须在玩家进房前就绪的感知、导演、阶段触发器

3. **明确运行时 owner**
   - 先决定是谁驱动它：组件自身、`Room`、`RoomManager`、`WorldPhaseManager`、`WorldProgressManager`、`AmbienceController` 等。
   - 一个元素只能有一个主驱动 authority，避免同时被多个系统接管。

4. **明确状态通道**
   - 只是本次进入房间有效：本地会话态即可
   - 离开房间 / 读档后也要保留：接 `RoomFlagRegistry`
   - 属于章节推进或世界常量变化：接世界级 manager，而不是塞进房间 flag

5. **决定要不要接 `Room` 主链**
   - 只有当元素需要被 `Room` / `RoomManager` 查询、重置、批量调度时，才扩 `Room.CollectSceneReferences()`。
   - 能自给自足的触发器、交互件、环境控制器，应优先保持自治。

6. **决定要不要扩编辑期模型**
   - 只有当该元素需要参与 `Level Architect` overlay、validator 或批量搭建工具时，才扩编辑期 schema。
   - 纯场景手工 authoring 的细节，不要为了“看起来统一”额外发明脱离 Scene 主链的中间快照层。

7. **最后补校验与验收**
   - 需要进入标准工作流的元素，必须同步补 `LevelValidator`。
   - 至少跑一轮：`Validate` → `Quick Play` → 对应系统的完整 Play Mode 手测。

### 8.5 房间元素分类落位速查

| 中文分类 | 英文标签 | 常见代表 | 默认挂点 | 必须先确认的接入点 |
|---------|----------|----------|----------|--------------------|
| **通路件** | `Path` | `Door`、层间门、阶段门（仅限显式门型通路） | `Navigation` | 目标房间、出生点、连接语义、阶段/进度门控、几何开口协作 |
| **交互件** | `Interact` | `Lock`、`Checkpoint`、拾取物 | `Elements` | 触发器、`_playerLayer`、输入链、必需 SO / 目标引用 |
| **状态件** | `Stateful` | `DestroyableObject`、永久机关、一次性揭示物 | `Elements` | 父 `Room`、flag key、`RoomFlagRegistry`、`SaveBridge` |
| **战斗件** | `Combat` | `OpenEncounterTrigger`、`ArenaController`、`EnemySpawner` | `Encounters` | `EncounterSO.Mode`、Spawner、重置链、房型语义 |
| **环境机关件** | `Environment` | `EnvironmentHazard` 子类 | `Hazards` | 伤害配置、Layer、Collider、是否需要持久关闭 |
| **导演件** | `Directing` | `BiomeTrigger`、`HiddenAreaMask`、`ScheduledBehaviour`、`ActivationGroup`、`WorldEventTrigger`、`NarrativeFallTrigger` | `Triggers`（或显式扩展层） | 事件源、target 引用、phase/progress 依赖、预激活要求 |
| **基础设施件** | `Infrastructure` | `SpawnPoint`、`CameraConfiner`（可选） | `Navigation/SpawnPoints`、`CameraConfiner` | 房间入口落点、可选硬镜头边界、与 `Room` / `Door` / `CameraDirector` 的协作关系 |

- `Door` = 显式门型通路；只有它这类对象才默认参与连接拓扑、目标出生点和几何开口协作。
- trap / fall / teleport 式 room transfer 若不依赖门洞，默认按 `Directing` / `Triggers` 处理；不要为了过 validator 强行伪装成 `Door`。

---

## 9. 设计快照与现役 authoring 的边界

### 9.1 当前唯一 authoring authority

当前关卡 authoring 的唯一现役主链是：

- Scene 中的 `Room` / `Door` / 相关子节点结构
- `RoomSO` / `EncounterSO` / 其他业务 SO
- `Level Architect` 的 `Build / Quick Edit / Validate` 工作面

### 9.2 不再保留的中间层

以下口径不再作为现役工作流的一部分：

- 外部 `HTML → JSON → Scene` 导入链
- 依赖额外快照文件驱动房间骨架生成的双轨流程
- 把设计草图长期保留为 authoring authority 的做法

### 9.3 现役原则

- 任何房间结构改动都应直接落在 Scene 中
- 任何房间语义配置都应落在 `RoomSO` / 相关 SO 或现役 inspector 中
- 若需要设计讨论材料，应把它视为辅助参考，而不是工具链 authority

也就是说：

- **JSON 负责“把骨架带进来”**
- **Scene + SO 负责“之后它真正怎么活”**

### 9.6 兼容性说明

- `RoomNodeType` 字符串已经严格收口到 `transit / combat / arena / reward / safe / boss`
- `connectionType` 仍保留少量旧 alias 归一兼容，便于迁移旧 JSON，但新增设计稿应统一写现役字符串

---

## 10. 常见工作节奏建议

### 10.1 推荐节奏

1. 先用 `Level Architect / Build` 出骨架
2. 做一轮结构 Pass
3. 做一轮语义 Pass
4. 开 `Overlay` 看主链和节奏分布
5. 跑 `Validate` 清结构错误
6. 跑一次 `Quick Play`
7. 进完整 Play Mode 手动走一遍

### 10.2 不推荐的做法

- 只做白盒，不补语义
- 只看 Overlay，不跑 Validate
- 只跑 Quick Play，就当切片已经验收完成
- 把 JSON 设计稿或其他编辑期草图当成运行时长期 authority
- 先堆美术 / 装饰，再决定房间在节奏里是什么

---

## 11. 相关文档索引

| 文档 | 职责 |
|------|------|
| `Docs/2_TechnicalDesign/Level/Level_CanonicalSpec.md` | Level 模块现役架构、声明、authority 规范 |
| `Implement_rules.md` | 模块级实现约束与协作规则 |
| `Docs/6_Diagnostics/Verification_Checklist.md` | 编辑期 / 运行期验收清单 |
| `Docs/6_Diagnostics/LevelArchitect_SupportedElements_Matrix.md` | `Level Architect` 当前支持能力矩阵 |
| `Docs/5_ImplementationLog/README.md` | 文档与实现变更留痕 |
