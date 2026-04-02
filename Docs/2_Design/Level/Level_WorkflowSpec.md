# 关卡搭建工作流手册 (Level Workflow Spec)

> **文档目的**：指导《静默方舟》关卡在 Unity Editor 中的实际搭建流程，让设计入口、场景落地、验证方式和职责边界保持一致。
> 
> **文档定位**：这是一份操作手册，回答“现在怎么搭、怎么验、哪里是权威”。

---

## 1. 文档定位与 authority

### 1.1 这份文档负责什么

这份文档只负责四件事：

1. 说明搭建入口：`LevelDesigner.html` 导入，或 `Level Architect` 直接白盒
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
| 房间连接拓扑 | Scene 中 `Door._targetRoom` / `_targetSpawnPoint` | Door 是连接关系 authority |
| 房间元数据 | `RoomSO` / `EncounterSO` / `CheckpointSO` / `WorldPhaseSO` 等 SO | 非空间配置、节奏类型、遭遇配置以 SO 为准 |
| 外部拓扑草图 | `LevelDesigner.html` 导出的 JSON | 只作为导入源 |
| 编辑期快照 | `LevelScaffoldData` | 只承担归档 / 导出，不驱动运行时 |
| 架构规则与模块边界 | `Level_CanonicalSpec.md` | 架构、声明、authority 规范 |

### 1.4 一句话原则

**`WorkflowSpec` 负责“怎么搭”，`CanonicalSpec` 负责“谁说了算”。**

---

## 2. 工作流总览

关卡搭建有两条入口路径：

```text
路径 A：HTML 拓扑规划
LevelDesigner.html → Export JSON → LevelSliceBuilder 导入 → Scene 落地

路径 B：Unity 内直接白盒
Level Architect / Build Tab → Blockout / Connect → Scene 落地

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

### 3.1 什么时候走路径 A

优先走 **路径 A：`HTML → JSON → Scene`**，如果你更需要：

- 先规划大拓扑，再进 Unity 落地
- 先看房间关系、分层、节奏分布
- 先和设计/GDD 对齐地图结构

### 3.2 什么时候走路径 B

优先走 **路径 B：`Blockout → Connect → Scene`**，如果你更需要：

- 在 Unity 里直接白盒迭代
- 边画边试，快速找手感
- 已经知道要做什么，只想尽快产出可玩骨架

### 3.3 默认建议

- **新区域 / 新章节**：优先路径 A
- **局部重做 / 单切片验证**：优先路径 B
- **复杂项目中后期**：A 做大结构，B 做局部细化，最后统一进入验证流程

---

## 4. 路径 A：`LevelDesigner.html → JSON → Scene`

### 4.1 适用场景

适合“先把拓扑和节奏画清楚，再落进 Unity”的工作方式。

### 4.2 操作步骤

1. **打开工具**
   - Unity 菜单：`ProjectArk > Level > Authority > Level Architect`
   - 在 `Design` Tab 点击 **`🌐 Open LevelDesigner.html`**
   - 浏览器会打开 `Tools/LevelDesigner.html`

2. **在浏览器里规划房间与连接**
   - 放置房间节点
   - 设定房间类型、命名、楼层、尺寸
   - 创建连接并指定 `connectionType`
   - 填写 `Level Name`

3. **导出 JSON**
   - 点击 **`💾 Export File`**
   - 建议保存到 `Docs/3_LevelDesigns/` 对应目录

4. **导入到 Unity 场景**
   - 回到 `Design` Tab
   - 点击 **`📂 Import LevelDesigner JSON...`**
   - 导入后由 `LevelSliceBuilder` 自动创建：
     - 场景中的 `Room` 根对象
     - `RoomSO` 资产
     - Door 对与 SpawnPoint 对
     - 标准房间子节点骨架

5. **切换到 `Build` Tab 做精修**

### 4.3 路径边界

- JSON 是导入源，不是运行时 authority
- 导入完成后，继续演化的是 Scene 中的 `Room` / `Door` 和相关 SO
- 如果导入后又手改了房间位置、尺寸、Door 接线，以 Scene 当前状态为准

### 4.4 JSON 导入约束

- `LevelSliceBuilder` 读取的是顶层 `connections[]`
- 房间位置与尺寸字段使用 `position: [x, y]` / `size: [w, h]`
- 连接方向字段使用 `fromDir` / `toDir`
- 方向值使用 `east / west / north / south`

JSON 结构示例见本文附录。

---

## 5. 路径 B：`Level Architect / Build Tab → 白盒搭建`

### 5.1 适用场景

适合“直接在 Unity 中搭出可玩骨架”的工作方式。

### 5.2 操作步骤

1. **打开工具**
   - Unity 菜单：`ProjectArk > Level > Authority > Level Architect`
   - 进入 `Build` Tab

2. **准备房间预设（首次使用时）**
   - 点击 **`Create Built-in Presets`**
   - 生成内置 `RoomPresetSO`

3. **Blockout 模式白盒**
   - 切到 `Blockout`
   - 使用房间/走廊笔刷放置基础房间
   - 利用自动吸附和相邻自动连线快速形成主链

4. **Connect 模式补语义连接**
   - 切到 `Connect`
   - 从一个房间拖到另一个房间补建门对
   - 用于修正自动连线未覆盖的结构，或手动增加特殊连接

5. **Select 模式精修**
   - 调整尺寸、楼层、房间类型、遭遇配置
   - 用侧边面板或批量编辑进行整理

### 5.3 路径特点

- 迭代快
- 离手感最近
- 更适合小范围重做、切片打磨、结构改版

### 5.4 风险提醒

- 只搭结构、不补语义，最终会得到“能通但节奏不清楚”的地图
- 只看 SceneView 白盒、不做验证，会高估当前完成度

---

## 6. 通用 Build 流程

无论走路径 A 还是路径 B，落到场景后都建议按下面 4 个阶段推进。

### 6.1 Phase 1：结构搭建 Pass

目标：确认“房间作为空间结构是否成立”。

**这一阶段要完成：**

- 房间位置、尺寸、楼层关系正确
- 主路线与回路的 Door 已打通
- `CameraConfiner`、`BoxCollider2D`、标准根节点存在
- 至少可从入口穿过一条有效主链

**常用工具：**

- `Blockout` / `Connect` / `Select`
- `Floor Level` 过滤器
- `Create Built-in Presets`
- 右键菜单 / Batch Edit

### 6.2 Phase 2：语义标注 Pass

目标：把“能走的图”变成“可读的关卡”。

**这一阶段要补齐：**

- `RoomNodeType`
- `ConnectionType`
- `TransitionCeremony`
- `EncounterSO`
- Entry / Boss / Safe / Threshold 等关键节点语义

**判断标准：**

如果你还只能说“这是个大房间 / 小房间 / 长走廊”，说明还停留在结构层；只有当你能说“这是一个 `Pressure` 通往 `Resolution` 的推进段”，语义层才算完成。

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
| `Resolution` / `Boss` 房缺 `ArenaController` | Warning | ✅ |
| `Resolution` / `Boss` 房缺 `EncounterSO` | Warning | ❌ |
| `Resolution` / `Boss` 房缺 `EnemySpawner` | Warning | ✅ |
| Door 的 `_playerLayer` 未配置 | Warning | ✅ |
| Door 的 `_targetSpawnPoint` 为空 | Error | ❌ |
| 房间孤立（没有 Door 连接） | Info | ❌ |

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
| `Navigation` | Door、进入点、离开点等空间导航结构 |
| `Elements` | 锁、拾取物、互动件、可破坏物、NPC |
| `Encounters` | 遭遇触发器、`ArenaController`、`EnemySpawner` |
| `Hazards` | 环境伤害与危险物 |
| `Decoration` | 纯视觉 / 氛围内容 |
| `Triggers` | Camera、Biome、事件、隐藏区域等触发器 |
| `CameraConfiner` | 当前房间镜头边界 |

### 8.3 可扩展内容层

`Tilemaps`、`Variant_xxx`、`ActivationGroups` 等内容层可以存在，但不能替代标准根节点职责。

### 8.4 新增房间元素接入 SOP（严格版）

每次新增一个房间元素，都按以下顺序收口：

1. **先选模板家族**
   - 先判断它属于 `Door / Gate`、`Interact Anchor`、`Persistent Room Element`、`Encounter Element`、`Hazard Element`、`Trigger / Director` 中的哪一类。
   - 如果现役模板能覆盖，就不要新开“特殊逻辑分支”。

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
   - 能自给自足的触发器、互动件、环境控制器，应优先保持自治。

6. **决定要不要扩编辑期模型**
   - 只有当该元素需要参与 `LevelScaffoldData`、HTML 导入导出、Overlay 或批量搭建工具时，才扩编辑期 schema。
   - 纯场景手工 authoring 的细节，不要为了“看起来统一”强行塞进 `ScaffoldElementType`。

7. **最后补校验与验收**
   - 需要进入标准工作流的元素，必须同步补 `LevelValidator`。
   - 至少跑一轮：`Validate` → `Quick Play` → 对应系统的完整 Play Mode 手测。

### 8.5 六类模板落位速查

| 模板家族 | 常见代表 | 默认挂点 | 必须先确认的接入点 |
|---------|----------|----------|--------------------|
| `Door / Gate` | `Door`、层间门、阶段门 | `Navigation` | 目标房间、出生点、连接语义、阶段/进度门控 |
| `Interact Anchor` | `Lock`、`Checkpoint`、拾取物 | `Elements` | 触发器、`_playerLayer`、输入链、必需 SO / 目标引用 |
| `Persistent Room Element` | `DestroyableObject`、永久机关、一次性揭示物 | `Elements` | 父 `Room`、flag key、`RoomFlagRegistry`、`SaveBridge` |
| `Encounter Element` | `OpenEncounterTrigger`、`ArenaController`、`EnemySpawner` | `Encounters` | `EncounterSO`、Spawner、重置链、房型语义 |
| `Hazard Element` | `EnvironmentHazard` 子类 | `Hazards` | 伤害配置、Layer、Collider、是否需要持久关闭 |
| `Trigger / Director` | `BiomeTrigger`、`HiddenAreaMask`、`ScheduledBehaviour`、`ActivationGroup`、`WorldEventTrigger` | `Triggers`（或显式扩展层） | 事件源、target 引用、phase/progress 依赖、预激活要求 |

---

## 9. `LevelScaffoldData` 的定位


### 9.1 它是什么

`LevelScaffoldData` 是：

- 编辑期快照
- 场景信息的归档 / 导出载体
- `Scene → Scaffold` 单向同步目标

### 9.2 它不是什么

它不是：

- 运行时 authority
- Door 拓扑真相源
- JSON 导入的必经中间层
- Scene 的持续反向驱动器

### 9.3 同步方向

`ScaffoldSceneBinder` 的同步方向是：

**Scene → Scaffold**

- 场景是 source of truth
- `ScaffoldData` 是 snapshot / export target
- 不做 `Scaffold → Scene` 自动回写

### 9.4 什么时候值得用它

适合用于：

- 给已搭好的 Scene 做结构归档
- 记录房间位置、尺寸与部分骨架信息
- 作为后续工具链扩展的数据落脚点

如果只是想快速导入 JSON 或直接搭房间，没有必要先创建 `LevelScaffoldData`。

---

## 10. 常见工作节奏建议

### 10.1 推荐节奏

1. 用路径 A 或 B 先出骨架
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
- 把 JSON 或 `LevelScaffoldData` 当成运行时长期 authority
- 先堆美术 / 装饰，再决定房间在节奏里是什么

---

## 11. `LevelSliceBuilder` JSON Schema

### 11.1 基本结构

```json
{
  "levelName": "示巴星_ACT1",
  "rooms": [
    {
      "id": "room_001",
      "name": "坠机点_残骸区",
      "type": "reward",
      "floor": 0,
      "position": [0, 0],
      "size": [30, 20],
      "zoneId": "sheba",
      "act": "ACT1",
      "tension": 1,
      "beatName": "intro"
    }
  ],
  "connections": [
    {
      "from": "room_001",
      "to": "room_002",
      "fromDir": "east",
      "toDir": "west",
      "connectionType": "progression"
    }
  ]
}
```

### 11.2 `rooms[]` 字段说明

| 字段 | 说明 |
|------|------|
| `id` | 房间唯一 ID |
| `name` | 房间显示名称 |
| `type` | 房间节奏类型 |
| `floor` | 楼层 |
| `position` | `[x, y]`，网格单位 |
| `size` | `[w, h]`，网格单位 |
| `zoneId` / `act` / `tension` / `beatName` | 可携带的附加设计信息 |

### 11.3 `connections[]` 字段说明

| 字段 | 说明 |
|------|------|
| `from` / `to` | 连接起点房间 / 终点房间 ID |
| `fromDir` / `toDir` | 门朝向，使用 `east / west / north / south` |
| `connectionType` | 连接语义类型 |

### 11.4 `type` 规范字符串

- `transit`
- `pressure`
- `resolution`
- `reward`
- `anchor`
- `loop`
- `hub`
- `threshold`
- `safe`
- `boss`

### 11.5 `connectionType` 规范字符串

- `progression`
- `return`
- `ability`
- `challenge`
- `identity`
- `scheduled`

---

## 12. 相关文档索引

| 文档 | 职责 |
|------|------|
| `Docs/2_Design/Level/Level_CanonicalSpec.md` | Level 模块现役架构、声明、authority 规范 |
| `Implement_rules.md` | 模块级实现约束与协作规则 |
| `Docs/3_LevelDesigns/` | 各区域拓扑设计稿与 JSON 输入 |
| `Tools/LevelDesigner.html` | 浏览器侧拓扑设计工具 |
| `Docs/5_ImplementationLog/ImplementationLog.md` | 文档与实现变更留痕 |
