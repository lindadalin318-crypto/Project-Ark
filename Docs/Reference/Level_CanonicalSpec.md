# Level Module Canonical Spec

## 1. 文档目的

这份文档是关卡模块重构的**唯一目标架构规范**。

它负责定义：

- 重构后的关卡模块长什么样（目标架构）
- 核心数据结构的精确定义（WorldGraphSO / RoomNode / ConnectionEdge / GateID）
- 运行时数据的权威流向（谁生产、谁消费）
- 模块边界与跨模块依赖
- Editor 工具链的职责划分（轻量版 Authority）
- 从当前架构到目标架构的迁移策略
- 每个 Batch 的验收标准

> **设计哲学基底**：以 Minishoot 的工程化关卡思维为骨架（标准化场景容器 + 显式世界图 + 门语义分级 + 节点类型分级），保留 Ark 的差异化优势（世界时钟 + 动态阶段 + 混合层间结构 + 类魂叙事氛围）。
>
> 完整的参考分析见 `Docs/Reference/Level_Architecture_Synthesis_Minishoot_Silksong_TUNIC.md`。

> **本文档不负责**：
>
> - 具体关卡内容设计（示巴星房间表、敌人配置、叙事脚本）
> - 美术规范（Tilemap 调色板、环境装饰标准）
> - GDD 级别的玩家体验描述
>
> 本文档优先级高于 `LevelModulePlan.md`（v3.0）中与本文档冲突的内容。`LevelModulePlan.md` 作为历史参考保留。

---

## 2. 适用范围

当前纳入重构治理的范围：

### Runtime

- `Assets/Scripts/Level/` 全部子目录
  - `Room/` — Room, RoomManager, Door, DoorTransitionController, ArenaController, WaveSpawnStrategy, OpenEncounterTrigger, ActivationGroup, HiddenAreaMask
  - `Data/` — RoomSO, EncounterSO, RoomType, EncounterMode, DoorState, RoomVariantSO, WorldPhaseSO, WorldProgressStageSO, KeyItemSO
  - `Checkpoint/` — Checkpoint, CheckpointManager
  - `Progression/` — Lock, KeyInventory, WorldProgressManager
  - `WorldClock/` — WorldClock, WorldPhaseManager
  - `DynamicWorld/` — ScheduledBehaviour, WorldEventTrigger, AmbienceController, BiomeTrigger, TilemapVariantSwitcher
  - `Map/` — MinimapManager, MapPanel, MapRoomData, MapConnection, MapConnectionLine
  - `Camera/` — CameraDirector, CameraTrigger
  - `Narrative/` — NarrativeFallTrigger
  - `GameFlow/` — GameFlowManager
  - `Hazard/` — EnvironmentHazard, DamageZone, ContactHazard, TimedHazard
  - `SaveBridge.cs`
- `Assets/Scripts/Core/LevelEvents.cs` — 跨程序集事件总线

### Editor

- `Assets/Scripts/Level/Editor/` 全部文件（19 个 .cs）

### Data Assets

- `Assets/_Data/Level/` — 所有 SO 资产

### Scenes

- `Assets/Scenes/SampleScene.unity` — 关卡实例

### Docs

- `Docs/Reference/Level_CanonicalSpec.md` — 本文档（权威）
- `Docs/Reference/Level_Architecture_Synthesis_Minishoot_Silksong_TUNIC.md` — 参考分析
- `Docs/LevelModulePlan.md` — 历史计划（v3.0，降级为参考）

### Assembly Definitions

- `Assets/Scripts/Level/ProjectArk.Level.asmdef`
- `Assets/Scripts/Level/Editor/ProjectArk.Level.Editor.asmdef`

---

## 3. 模块设计哲学

### 3.1 核心原则：先定义关卡语法，再大规模生产内容

> 来源：Minishoot 最值得学习的不是某个神来之笔，而是它的**工程化关卡思维**——世界分块清楚、连接规则清楚、房间节奏清楚、导演工具轻量、状态持久稳定、性能策略内建。

关卡模块的首要目标不是"做出很多房间"，而是：

1. **定义一套标准件**（8-12 种），让每种关卡设计意图都有对应的标准化部件
2. **复杂度来自组合**，不来自定制——标准件在不同空间关系中的组合方式产生丰富体验
3. **让下一个房间比上一个房间更容易搭**——迭代速度是最重要的架构指标

### 3.2 Ark 的差异化优势（不可丢弃）

以下系统在 Minishoot 中不存在或更简单，是 Ark 的独有维度：

| 系统 | Ark 独有点 | 在重构中的定位 |
|------|-----------|---------------|
| 世界时钟 + 动态阶段 | 周期性环境变化（辐射潮/平静期/风暴期/寂静时） | 保留并增强，作为房间变体系统的驱动源 |
| 混合层间结构 | 日常淡黑传送 + 极少数叙事级无缝掉落 | 保留，不改动 |
| 类魂叙事氛围 | 世界观不靠文本，靠空间改写和环境叙事 | 重构应让环境叙事件更容易被 author |
| 热量 + 散热战斗核心 | 战斗 = 散热窗口 | 关卡节奏设计必须考虑热量压力曲线 |
| 开放骚扰 + 封门清算双模遭遇 | 比 Minishoot 更完善（宽限期、despawn、respawn） | 保留，不重构 |

### 3.3 节奏闭环优先于内容量

> 来源：Minishoot 分析 1.26 — "一个切片是否成立，取决于它有没有完成一条节奏闭环"

任何可玩切片必须完成：**进入 → 施压 → 证明 → 回报 → 回路 → 收束**。

只要这 6 件事完整，哪怕地图不大，玩家也会觉得"这是一段真的游戏内容"。

---

## 4. 目标架构总览

### 4.1 五层模型

重构后的关卡模块遵循 Minishoot 式的五层模型，适配 Ark 的单场景 + Tilemap 架构：

```
┌─────────────────────────────────────────────────────────────┐
│  Layer 5: 世界图谱层 (World Graph)                           │
│  WorldGraphSO — 显式房间网络 + 连接语义 + 离线校验            │
└───────────────────────────┬─────────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────────┐
│  Layer 4: 房间容器层 (Room Container)                        │
│  Room — 标准化子节点语法 + 节点类型语义 + 变体支持            │
└───────────────────────────┬─────────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────────┐
│  Layer 3: 连接缝合层 (Connection / Transition)               │
│  Door — GateID + ConnectionType + 仪式感分级 + 过渡演出      │
└───────────────────────────┬─────────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────────┐
│  Layer 2: 节奏内容层 (Pacing Content)                        │
│  Encounter(Open/Close) + Hazard + Lock + Checkpoint          │
│  + DestroyableObject + InteractableObject                    │
└───────────────────────────┬─────────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────────┐
│  Layer 1: 导演引导层 (Director / Guidance)                    │
│  CameraTrigger + BiomeTrigger + AmbienceController           │
│  + WorldClock + WorldPhaseManager + WorldProgressManager     │
└─────────────────────────────────────────────────────────────┘
```

### 4.2 与当前架构的对应关系

| 目标层 | 当前已有 | 重构内容 |
|--------|---------|---------|
| Layer 5 世界图谱 | ❌ 不存在（MinimapManager 运行时动态推导邻接） | **新建** WorldGraphSO + RoomNode + ConnectionEdge |
| Layer 4 房间容器 | ✅ Room + RoomManager + RoomSO | **升级** 子节点语法规范 + 节点类型语义化 |
| Layer 3 连接缝合 | ✅ Door + DoorTransitionController + DoorState | **升级** GateID + ConnectionType + 仪式感分级 |
| Layer 2 节奏内容 | ✅ 遭遇系统完整、Hazard 完整、Lock/Checkpoint 完整 | **新增** RoomFlagRegistry + DestroyableObject + 统一解锁器 |
| Layer 1 导演引导 | ✅ CameraDirector + CameraTrigger + BiomeTrigger + WorldClock 全套 | **无需重构**，保留 |

---

## 5. 核心数据结构定义

### 5.1 WorldGraphSO — 世界图谱

```csharp
namespace ProjectArk.Level
{
    /// <summary>
    /// 显式的世界房间网络图谱。定义所有房间节点和它们之间的连接关系。
    /// 这是关卡拓扑结构的 Single Source of Truth。
    /// </summary>
    [CreateAssetMenu(menuName = "ProjectArk/Level/World Graph")]
    public class WorldGraphSO : ScriptableObject
    {
        [SerializeField] private string _graphName;
        [SerializeField] private RoomNodeData[] _rooms;
        [SerializeField] private ConnectionEdge[] _connections;

        public string GraphName => _graphName;
        public ReadOnlySpan<RoomNodeData> Rooms => _rooms;
        public ReadOnlySpan<ConnectionEdge> Connections => _connections;

        /// <summary>按 RoomID 查找节点（Editor 用，运行时应缓存为字典）</summary>
        public RoomNodeData? FindRoom(string roomID) { ... }

        /// <summary>获取指定房间的所有出边</summary>
        public IEnumerable<ConnectionEdge> GetOutgoingConnections(string roomID) { ... }

        /// <summary>获取指定房间的所有入边</summary>
        public IEnumerable<ConnectionEdge> GetIncomingConnections(string roomID) { ... }
    }
}
```

### 5.2 RoomNodeData — 房间节点数据

```csharp
namespace ProjectArk.Level
{
    /// <summary>
    /// 世界图谱中的一个房间节点。
    /// 这是纯拓扑数据，不包含空间信息（空间信息由场景中的 Room MonoBehaviour 持有）。
    /// </summary>
    [System.Serializable]
    public struct RoomNodeData
    {
        [Tooltip("必须与场景中 Room 的 RoomSO.RoomID 一致")]
        public string RoomID;

        [Tooltip("房间在关卡节奏中的职责类型")]
        public RoomNodeType NodeType;

        [Tooltip("该房间暴露的所有命名入口")]
        public string[] GateIDs;

        [Tooltip("Editor 备注，不影响运行时")]
        public string DesignerNote;
    }
}
```

### 5.3 RoomNodeType — 房间节点类型

```csharp
namespace ProjectArk.Level
{
    /// <summary>
    /// 房间在关卡节奏中的职责类型。
    /// 来源：Minishoot 分析 1.21 的 6 种节点分级 + Ark 扩展。
    /// 
    /// 设计原则：不要说"我要做 8 个房间"，而要说"我要 2 个 Transit + 2 个 Pressure + 
    /// 1 个 Resolution + 1 个 Reward + 1 个 Anchor + 1 个 Loop"。
    /// </summary>
    public enum RoomNodeType
    {
        /// <summary>过路节点：保持移动，连接相邻区域，不承载太重信息。可能有少量 EncounterOpen。</summary>
        Transit,

        /// <summary>压力节点：不打断流程但增加消耗和警惕。1-3 组开放敌人，可边打边走。</summary>
        Pressure,

        /// <summary>清算节点：用封闭战斗验证玩家是否掌握当前区段规则。EncounterClose + 波次/Boss。</summary>
        Resolution,

        /// <summary>回报节点：紧张后给认知/资源/氛围缓冲。Checkpoint、宝箱、景观房。</summary>
        Reward,

        /// <summary>锚点节点：帮助玩家建立脑内地图。明显地标、特殊 CameraTrigger、独特 Biome。</summary>
        Anchor,

        /// <summary>回路节点：把单向推进转化成结构性掌控。解锁门、反向通路、回旧 Checkpoint 的捷径。</summary>
        Loop,

        /// <summary>枢纽节点：多条路径的交汇点，提供导航选择。通常是大区域的中心。</summary>
        Hub,

        /// <summary>门槛节点：章节边界，进入后世界发生不可逆变化。Boss 前厅、关键剧情触发点。</summary>
        Threshold,

        /// <summary>安全节点：完全没有敌意的休息区。Safe Room、商店。</summary>
        Safe,

        /// <summary>Boss 节点：Boss 竞技场，独立演出流程。</summary>
        Boss
    }
}
```

### 5.4 ConnectionEdge — 连接边

```csharp
namespace ProjectArk.Level
{
    /// <summary>
    /// 世界图谱中的一条连接边。
    /// 每条边描述从一个房间的某个 Gate 到另一个房间的某个 Gate 的单向连接。
    /// 双向门应建模为两条方向相反的边。
    /// </summary>
    [System.Serializable]
    public struct ConnectionEdge
    {
        [Tooltip("起点房间 ID")]
        public string FromRoomID;

        [Tooltip("起点 Gate ID（如 'left1', 'door_boss', 'shortcut_south'）")]
        public string FromGateID;

        [Tooltip("终点房间 ID")]
        public string ToRoomID;

        [Tooltip("终点 Gate ID")]
        public string ToGateID;

        [Tooltip("连接的语义类型")]
        public ConnectionType Type;

        [Tooltip("是否为层间连接（不同 FloorLevel）")]
        public bool IsLayerTransition;

        [Tooltip("Editor 备注")]
        public string DesignerNote;
    }
}
```

### 5.5 ConnectionType — 连接语义类型

```csharp
namespace ProjectArk.Level
{
    /// <summary>
    /// 门/通道连接的语义类型。
    /// 来源：Minishoot 分析 1.23 的 5 类连接关系。
    /// 
    /// 每次连接都在回答一个问题：
    /// - Progression: "我还能往前走吗？"
    /// - Return: "我如果回头，会不会更快？"
    /// - Ability: "这里先记住，以后拿到能力再回来。"
    /// - Challenge: "你现在有没有资格进入更高压内容？"
    /// - Identity: "你刚刚进入了什么类型的地方？"
    /// </summary>
    public enum ConnectionType
    {
        /// <summary>推进连接：主路线推进，通常连接压力段或新区域</summary>
        Progression,

        /// <summary>回返连接：捷径，重新连接旧安全点</summary>
        Return,

        /// <summary>能力连接：银河城能力门，暂时性视觉钩子</summary>
        Ability,

        /// <summary>挑战连接：Arena/Boss 前厅/支线挑战房</summary>
        Challenge,

        /// <summary>身份连接：Biome 切换、空间章节分割</summary>
        Identity,

        /// <summary>时间连接：由 WorldPhase 控制开关（Ark 特有）</summary>
        Scheduled
    }
}
```

### 5.6 GateID 命名规范

GateID 是 Door 组件上的唯一标识字符串，用于在 WorldGraphSO 中精确定位连接端点。

**命名规则**：

```
{方位}_{编号}     — 标准门：left_1, right_1, top_1, bottom_1
{语义}_{编号}     — 语义门：boss_entrance, shortcut_south, rift_down_1
{特殊}            — 特殊门：elevator_up, fall_trigger
```

**约束**：

- 同一个 Room 内的 GateID **必须唯一**
- GateID 在 WorldGraphSO 中作为 ConnectionEdge 的端点标识
- LevelValidator 必须能检出 GateID 重复和不匹配

### 5.7 Door 升级字段

在现有 `Door` 组件基础上新增的字段：

```csharp
// 新增字段（在现有 Door.cs 基础上追加）
[Header("World Graph Integration")]
[SerializeField] private string _gateID;           // 命名入口 ID
[SerializeField] private ConnectionType _connectionType = ConnectionType.Progression;

[Header("Ceremony")]
[SerializeField] private TransitionCeremony _ceremony = TransitionCeremony.Standard;
```

### 5.8 TransitionCeremony — 过渡仪式感分级

```csharp
namespace ProjectArk.Level
{
    /// <summary>
    /// 门过渡的仪式感等级。不同等级对应不同的过渡演出时长、VFX、镜头行为。
    /// </summary>
    public enum TransitionCeremony
    {
        /// <summary>无过渡，瞬间切换（同房间内的隐藏通道等）</summary>
        None,

        /// <summary>标准过渡：短淡黑（0.3s），适用于大部分普通门</summary>
        Standard,

        /// <summary>层间过渡：长淡黑（0.5s）+ 下坠粒子 + 环境音切换</summary>
        Layer,

        /// <summary>Boss 门过渡：特写演出 + 禁用玩家 + 独立音效</summary>
        Boss,

        /// <summary>重型门过渡：多段联动 + 震屏 + 粒子 + 长演出</summary>
        Heavy
    }
}
```

### 5.9 RoomFlagRegistry — 房间级持久状态

```csharp
namespace ProjectArk.Level
{
    /// <summary>
    /// 通用的房间级 Flag 系统。
    /// 用于追踪细粒度的空间状态："这个隐藏区被发现了"、"这个火炬被点亮了"、
    /// "这个可破坏物被打碎了"、"这个特定门被永久打开了"。
    /// 
    /// 通过 SaveBridge 持久化到 ProgressSaveData.Flags（key 格式：room_{roomID}_{flagKey}）。
    /// </summary>
    public class RoomFlagRegistry : MonoBehaviour  // ServiceLocator 注册
    {
        /// <summary>设置 Flag（自动持久化）</summary>
        public void SetFlag(string roomID, string flagKey, bool value = true) { ... }

        /// <summary>查询 Flag</summary>
        public bool GetFlag(string roomID, string flagKey) { ... }

        /// <summary>获取某房间的所有已设置 Flag</summary>
        public IReadOnlyDictionary<string, bool> GetRoomFlags(string roomID) { ... }

        /// <summary>写入存档</summary>
        public void WriteToSaveData(ProgressSaveData data) { ... }

        /// <summary>从存档恢复</summary>
        public void ReadFromSaveData(ProgressSaveData data) { ... }
    }
}
```

---

## 6. 房间标准化子节点语法

### 6.1 规范定义

重构后，每个 `Room` GameObject 下必须遵循以下固定 Hierarchy 结构：

```
Room_XXX (Room MonoBehaviour + BoxCollider2D Trigger)
├── Navigation/          — 门、出生点、GateID 标记
│   ├── Door_left_1
│   ├── Door_right_1
│   ├── SpawnPoint_left_1
│   └── SpawnPoint_right_1
├── Elements/            — 功能物件（锁、解锁器、拾取物、可破坏物、开关、NPC）
│   ├── Lock_RedDoor
│   ├── Pickup_HealthOrb
│   └── Destroyable_CrystalWall
├── Encounters/          — 遭遇触发器（Open/Close）
│   ├── OpenEncounter_Patrol
│   └── ClosedEncounter_Wave
├── Hazards/             — 环境危害（DamageZone、ContactHazard、TimedHazard）
│   ├── AcidPool_01
│   └── LaserFence_01
├── Decoration/          — 纯氛围（不影响 gameplay 的视觉/粒子/光效）
│   ├── Ambient_Particles
│   └── Background_Detail
└── Triggers/            — 导演触发器（Camera、Biome、Activation、HiddenArea）
    ├── CameraTrigger_Zoom
    ├── BiomeTrigger_Cave
    └── HiddenAreaMask_Secret
```

### 6.2 子节点职责矩阵

| 子节点 | 包含内容 | 查询者 | 激活策略 |
|--------|---------|--------|---------|
| `Navigation/` | Door, SpawnPoint, LayerTransitionMarker | RoomManager, DoorTransitionController, MinimapManager | 始终激活 |
| `Elements/` | Lock, KeyPickup, DestroyableObject, Interactable, NPC | Room (Awake 收集), RoomFlagRegistry | 按房间进出 + 持久状态 |
| `Encounters/` | OpenEncounterTrigger, ArenaController + EnemySpawner | Room, RoomManager | 按房间进出 |
| `Hazards/` | EnvironmentHazard 子类 | 独立运行（物理触发） | 按房间进出 |
| `Decoration/` | ParticleSystem, SpriteRenderer, Light2D | 无逻辑查询 | 按房间进出（性能优化） |
| `Triggers/` | CameraTrigger, BiomeTrigger, ActivationGroup, HiddenAreaMask | CameraDirector, AmbienceController | 始终激活（触发器必须在玩家进入前就就绪） |

### 6.3 Room.cs Awake 自动收集

重构后 `Room.Awake()` 将按子节点路径自动收集：

```csharp
// Room.cs (伪代码)
private void Awake()
{
    // 自动从 Navigation/ 收集
    _doors = GetComponentsInChildren<Door>(navigationRoot);
    _spawnPoints = CollectTransforms(navigationRoot, "SpawnPoint_*");

    // 自动从 Encounters/ 收集
    _arenaController = GetComponentInChildren<ArenaController>(encountersRoot);
    _openEncounters = GetComponentsInChildren<OpenEncounterTrigger>(encountersRoot);

    // 自动从 Elements/ 收集
    _destroyables = GetComponentsInChildren<DestroyableObject>(elementsRoot);

    // 现有收集逻辑保持兼容
}
```

### 6.4 兼容策略

- **已有房间**不需要立即重构子节点结构
- `Room.Awake()` 保留现有的 `GetComponentsInChildren<Door>()` 作为 fallback
- 新建房间由 `RoomFactory` 自动生成标准子节点结构
- `LevelValidator` 检测不符合规范的房间并输出警告（不阻塞）

---

## 7. 运行时数据流

### 7.1 权威数据流向图

```
WorldGraphSO (设计时 · 离线数据)
    │
    │ 启动时加载
    ▼
RoomManager (运行时 · 缓存为字典)
    │
    ├── Room.OnPlayerEntered → RoomManager.EnterRoom()
    │       │
    │       ├── 激活/休眠敌人
    │       ├── 通知 EnemyDirector 清空令牌
    │       ├── 通知 MinimapManager.MarkVisited()
    │       └── 广播 LevelEvents.OnRoomEntered
    │
    ├── Door.OnPlayerEnteredDoor → DoorTransitionController
    │       │
    │       ├── 从 Door._gateID 定位目标（优先 WorldGraphSO，fallback 到 _targetRoom）
    │       ├── 按 TransitionCeremony 执行过渡演出
    │       └── 传送玩家 → RoomManager.EnterRoom()
    │
    └── MinimapManager (消费者)
            │
            ├── 优先从 WorldGraphSO 读取邻接关系（新路径）
            └── Fallback 从场景 Door 运行时推导（兼容旧路径）

WorldClock (运行时 · 每帧)
    │
    └── OnTimeChanged → WorldPhaseManager
                            │
                            └── OnPhaseChanged → ScheduledBehaviour / Door / AmbienceController / Room (变体切换)

WorldProgressManager (运行时 · 里程碑事件)
    │
    └── OnWorldStageChanged → WorldEventTrigger / Door / 永久世界改变

RoomFlagRegistry (运行时 · 细粒度状态)
    │
    └── SaveBridge ↔ ProgressSaveData.Flags
```

### 7.2 关键数据流规则

1. **WorldGraphSO 是拓扑真相源**：运行时 RoomManager/MinimapManager 从 WorldGraphSO 读取房间连接关系，不自行推导
2. **Room 是空间真相源**：房间边界、生成点位置、Confiner 碰撞体等空间信息由场景中的 Room MonoBehaviour 持有
3. **RoomSO 是元数据真相源**：RoomID、显示名、楼层、类型等非空间元数据由 ScriptableObject 持有
4. **RoomFlagRegistry 是细粒度状态真相源**：房间内互动件的持久状态（破坏/点亮/发现）通过 Registry 管理
5. **LevelEvents 是跨模块通信渠道**：所有跨系统通知走静态事件总线，禁止跨系统直接引用

---

## 8. 模块边界与跨模块依赖

### 8.1 Level 模块对外依赖

| 外部模块 | 依赖方式 | 具体依赖点 |
|---------|---------|-----------|
| `ProjectArk.Core` | 程序集引用 | ServiceLocator, PoolManager, DamagePayload, IDamageable, LevelEvents, CombatEvents |
| `ProjectArk.Core.Audio` | 程序集引用 | AudioManager（过渡音效、BGM crossfade） |
| `ProjectArk.Core.Save` | 程序集引用 | SaveManager, PlayerSaveData, ProgressSaveData |
| `ProjectArk.Combat` | 程序集引用 | EnemySpawner（WaveSpawnStrategy 注入） |
| `ProjectArk.Ship` | 程序集引用 | ShipHealth（死亡事件）, InputHandler（过渡禁用输入）, HeatSystem（Checkpoint 恢复） |
| `ProjectArk.Enemy` | 程序集引用 | EnemyEntity.OnAnyEnemyDeath, EnemyDirector（令牌清空） |
| `ProjectArk.Heat` | 程序集引用 | HeatSystem（Checkpoint 恢复热量） |

### 8.2 Level 模块对外暴露

| 暴露内容 | 消费者 | 暴露方式 |
|---------|--------|---------|
| `LevelEvents.OnRoomEntered` | UI / Save / Audio | Core 层静态事件 |
| `LevelEvents.OnRoomCleared` | UI / Save | Core 层静态事件 |
| `LevelEvents.OnBossDefeated` | UI / Save / WorldProgress | Core 层静态事件 |
| `LevelEvents.OnCheckpointActivated` | UI / Save | Core 层静态事件 |
| `LevelEvents.OnPhaseChanged` | UI / Audio / VFX | Core 层静态事件 |
| `LevelEvents.OnWorldStageChanged` | UI / Save | Core 层静态事件 |
| `RoomManager` (via ServiceLocator) | 任何需要当前房间信息的系统 | ServiceLocator.Get |
| `WorldClock` (via ServiceLocator) | UI 时钟显示 | ServiceLocator.Get |

### 8.3 禁止的依赖方向

- **禁止** Level 模块引用 UI 程序集（通过事件解耦）
- **禁止** Core 层引用 Level 程序集（LevelEvents 放在 Core 层）
- **禁止** 运行时使用 `FindAnyObjectByType` / `FindObjectOfType`

---

## 9. Editor 工具链职责划分

### 9.1 工具矩阵

| 工具 | 职责 | 不负责 |
|------|------|--------|
| `LevelArchitectWindow` | 主 Editor 窗口，统一入口 | 不直接修改场景，委托给子服务 |
| `RoomFactory` | 创建 Room GameObject 时自动生成标准子节点结构 | 不负责已有房间的子节点补全 |
| `DoorWiringService` | Door 的自动连线（双向 _targetRoom + _targetSpawnPoint） | 不负责 GateID 分配（手动） |
| `LevelValidator` | 全局校验：WorldGraphSO 一致性、GateID 匹配、孤立房间、缺失引用、子节点规范 | 不修复问题，只报告 |
| `PacingOverlayRenderer` | Scene View 可视化：房间节点类型颜色 + 连接类型颜色 + 节奏位置 | 只读，不写入 |
| `RoomBlockoutRenderer` | 房间白盒布局可视化 | 只读，不写入 |
| `ScaffoldToSceneGenerator` | 从 HTML 脚手架生成场景 | 一次性工具 |
| `LevelAssetCreator` | 批量创建 SO 资产 | 不负责运行时 |

### 9.2 新增工具（待实现）

| 工具 | 职责 | 优先级 |
|------|------|--------|
| `WorldGraphEditor` | WorldGraphSO 的 Inspector 可视化编辑器（节点拖拽 + 连线） | Batch 1 |
| `RoomNodeMigrator` | 将已有房间的旧 RoomType 映射到新 RoomNodeType | Batch 2 |
| `RoomHierarchyAuditor` | 审计房间子节点是否符合标准语法 | Batch 2 |

### 9.3 工具使用原则

- **同一类任务只保留一个官方入口**
- 工具默认为 **Audit / Preview** 模式，显式操作才 **Apply**
- 所有工具执行后必须输出"改了什么 / 没改什么 / 缺了什么"
- 禁止工具在 `OnValidate`、`Awake`、`OnEnable`、Play Mode 启动流程中隐式写回资产

---

## 10. 迁移策略

### 10.1 总体原则

**增量升级，不推倒重来。**

理由：

1. 遭遇系统（OpenEncounterTrigger + ArenaController + WaveSpawnStrategy）已比 Minishoot 更完善——不动
2. 世界时钟 + 动态世界是 Ark 差异化优势——不动
3. 存档桥 + ServiceLocator + LevelEvents 事件总线干净——不动
4. Editor 工具链已经很强——只升级，不重写
5. CameraDirector + CameraTrigger + BiomeTrigger——不动

**真正需要改的只有 3 件事 + 3 件新增事：**

| 类别 | 内容 |
|------|------|
| 改动 | `Room.cs` 增加子节点语法收集 |
| 改动 | `Door.cs` 增加 GateID + ConnectionType + TransitionCeremony |
| 改动 | `MinimapManager.cs` 优先从 WorldGraphSO 读邻接 |
| 新增 | `WorldGraphSO` + `RoomNodeData` + `ConnectionEdge` + 相关枚举 |
| 新增 | `RoomFlagRegistry` 房间级持久状态 |
| 新增 | `LevelValidator` 升级（WorldGraphSO 校验规则） |

### 10.2 RoomType 迁移映射

| 旧 RoomType | 新 RoomNodeType | 说明 |
|-------------|----------------|------|
| `Normal` | `Transit` 或 `Pressure` | 需逐房间判断 |
| `Arena` | `Resolution` | 封门清算 |
| `Boss` | `Boss` | 保持 |
| `Safe` | `Safe` | 保持 |
| `Corridor` | `Transit` | 过路 |
| `Shop` | `Safe` 或 `Reward` | 视具体功能 |
| `Hub` | `Hub` | 保持 |
| `Gate` | `Threshold` | 章节门槛 |

**策略**：旧 `RoomType` 枚举保留为 `[Obsolete]`，`RoomSO` 新增 `RoomNodeType` 字段，两者并存过渡期后删除旧枚举。

### 10.3 Door 迁移策略

1. `Door.cs` 新增 `_gateID`、`_connectionType`、`_ceremony` 字段，默认值为空/Progression/Standard
2. 已有 Door 的 `_targetRoom` + `_targetSpawnPoint` 引用保留，作为 WorldGraphSO 未配置时的 fallback
3. `DoorTransitionController` 优先用 WorldGraphSO 查目标，fallback 到 `_targetRoom`
4. 完成 WorldGraphSO 全配置后，`_targetRoom` 引用可选择性保留或清除

### 10.4 MinimapManager 迁移策略

1. `MinimapManager.GatherSceneData()` 增加分支：如果 WorldGraphSO 存在且有效，优先从中读取邻接数据
2. 如果 WorldGraphSO 不存在或为空，fallback 到当前的运行时 Door 推导逻辑
3. 完成迁移后，运行时推导逻辑标记为 `[Obsolete]`

---

## 11. Batch 执行计划与验收标准

### Batch 1：世界图谱 + 门语义升级（骨架层）

**目标**：让关卡连接关系从"运行时推导"变成"显式数据 + 运行时校验"

**任务清单**：

| ID | 任务 | 涉及文件 |
|----|------|---------|
| B1.1 | 新建 `WorldGraphSO` + `RoomNodeData` + `ConnectionEdge` 数据结构 | 新建 3 个 .cs |
| B1.2 | 新建 `RoomNodeType` + `ConnectionType` + `TransitionCeremony` 枚举 | 新建 3 个 .cs |
| B1.3 | `Door.cs` 增加 `_gateID` + `_connectionType` + `_ceremony` 字段 | 修改 Door.cs |
| B1.4 | `DoorTransitionController.cs` 支持按 GateID 定位目标（优先 WorldGraphSO，fallback 到 _targetRoom） | 修改 DoorTransitionController.cs |
| B1.5 | `LevelValidator` 新增 WorldGraphSO 校验规则 | 修改 LevelValidator.cs |
| B1.6 | `MinimapManager` 适配：优先从 WorldGraphSO 读邻接关系 | 修改 MinimapManager.cs |
| B1.7 | 创建示巴星切片的 `WorldGraphSO` 资产（7 房间 + 连接关系） | 新建 .asset |

**验收标准**：

- [ ] `WorldGraphSO` 资产在 Inspector 中可编辑，描述 7 个房间和它们的连接
- [ ] `Door` 组件在 Inspector 中显示 GateID、ConnectionType、TransitionCeremony 字段
- [ ] `LevelValidator` 能检出"WorldGraphSO 中的 RoomID 在场景中不存在"
- [ ] `LevelValidator` 能检出"GateID 不匹配"（图谱定义了但场景中 Door 没有）
- [ ] `LevelValidator` 能检出"孤立房间"（没有任何 ConnectionEdge）
- [ ] 地图系统（MinimapManager + MapPanel）仍然正常工作
- [ ] 编译零错误，现有功能无回退
- [ ] `dotnet build Project-Ark.slnx` 通过

### Batch 2：房间语法标准化（内容层）

**目标**：让每个 Room 都遵循固定的子节点结构规范

**任务清单**：

| ID | 任务 | 涉及文件 |
|----|------|---------|
| B2.1 | 新建 `RoomNodeType` 替代旧 `RoomType`，`RoomSO` 新增 `_nodeType` 字段 | 修改 RoomSO.cs，新建枚举 |
| B2.2 | `Room.Awake()` 按子节点路径自动收集（兼容旧结构 fallback） | 修改 Room.cs |
| B2.3 | `RoomFactory` 创建房间时自动生成标准子节点结构 | 修改 RoomFactory.cs |
| B2.4 | `LevelValidator` 新增房间子节点规范检查 | 修改 LevelValidator.cs |
| B2.5 | `PacingOverlayRenderer` 升级：用 RoomNodeType 颜色可视化 | 修改 PacingOverlayRenderer.cs |

**验收标准**：

- [ ] 新建房间自动带 `Navigation/Elements/Encounters/Hazards/Decoration/Triggers` 子层
- [ ] `LevelValidator` 能检出"房间缺少标准子节点"（警告级别，不阻塞）
- [ ] Scene View 中可通过 `PacingOverlayRenderer` 看到不同 RoomNodeType 的颜色区分
- [ ] 已有房间不受影响（Awake fallback 正常工作）
- [ ] 编译零错误，现有功能无回退

### Batch 3：房间级持久状态 + 互动件标准化（状态层）

**目标**：让房间内的互动件能写入持久状态，形成"空间被改写"的体验

**任务清单**：

| ID | 任务 | 涉及文件 |
|----|------|---------|
| B3.1 | 新建 `RoomFlagRegistry`（ServiceLocator 注册 + SaveBridge 集成） | 新建 .cs，修改 SaveBridge.cs |
| B3.2 | 新建 `DestroyableObject` 基类（IDamageable + RoomFlagRegistry + 死亡联动） | 新建 .cs |
| B3.3 | `Door` 增加 `TransitionCeremony` 行为差异（Boss/Heavy 门独立演出） | 修改 Door.cs + DoorTransitionController.cs |
| B3.4 | `LevelEvents` 补充 `OnRoomFlagChanged` 事件 | 修改 LevelEvents.cs |

**验收标准**：

- [ ] 打碎一个 `DestroyableObject` → 重进房间仍然是碎的
- [ ] Boss 门打开有独立演出（区别于普通门的 0.3s 淡黑）
- [ ] `RoomFlagRegistry` 存档后正确恢复
- [ ] 编译零错误，现有功能无回退

### Batch 4：节奏验证工具 + 示巴星首切片落地（验证层）

**目标**：用工具辅助搭建和验证切片节奏

**任务清单**：

| ID | 任务 | 涉及文件 |
|----|------|---------|
| B4.1 | `PacingOverlayRenderer` 升级：显示 ConnectionType 连线颜色 | 修改 PacingOverlayRenderer.cs |
| B4.2 | 示巴星 7 房间切片搭建（R01-R07） | 场景配置 |
| B4.3 | 闭环验证：进入 → 施压 → 证明 → 回报 → 回路 → 收束 | Play Mode 测试 |

**验收标准**：

- [ ] Scene View 中可视化显示完整的房间节点类型 + 连接语义颜色
- [ ] 7 个房间全部可进入、可通过、门正确连接
- [ ] 至少一条回路捷径被打开后永久生效
- [ ] 至少一场封门清算正确运行
- [ ] Checkpoint 正确保存和恢复
- [ ] 节奏闭环完整：进入 → 施压 → 证明 → 回报 → 回路 → 收束

---

## 12. 校验规则

每次改动后至少检查：

1. `dotnet build Project-Ark.slnx` 编译通过
2. `LevelValidator` 无 Error 级别报告（Warning 可接受）
3. WorldGraphSO 中的所有 RoomID 在场景中有对应 Room
4. WorldGraphSO 中的所有 GateID 在对应 Room 的 Door 上有匹配
5. 没有孤立房间（至少有一条 ConnectionEdge）
6. MinimapManager 仍能正确构建拓扑图
7. SaveBridge 存读流程不中断
8. 已有遭遇系统（Open/Close）不受影响

---

## 13. 与其他文档的关系

| 文档 | 职责 | 与本文档的关系 |
|------|------|--------------|
| `Level_CanonicalSpec.md`（本文档） | 目标架构定义、数据结构、迁移策略、验收标准 | 权威 |
| `Level_Architecture_Synthesis_Minishoot_Silksong_TUNIC.md` | 参考游戏的关卡架构分析 | 设计输入，不直接约束实现 |
| `LevelModulePlan.md` (v3.0) | 历史实现计划 | 降级为参考，冲突时以本文档为准 |
| `Implement_rules.md` | 模块级实现规则 / 协作约束 | 后续需补充 Level 模块规则段落 |
| `Docs/ImplementationLog/ImplementationLog.md` | 实现日志 | 每次改动后追加 |

---

## 14. 附录：示巴星切片房间表（参考）

> 来源：Minishoot 分析 1.25，翻译为 Ark 可执行版本。此表为设计参考，实际搭建时可根据需要调整。

| 房间 ID | RoomNodeType | 设计目标 | 核心内容 | 验证体验 |
|---------|-------------|---------|---------|---------|
| `SH-R01` | `Safe` (入口) | 建立基调 | 坠机残骸、可读地标、低敌压 | "我掉进了一个死寂但有秩序的世界" |
| `SH-R02` | `Pressure` | 引入静默压力 | 低声场、过热上升、少量可绕敌人 | "这里的安静不是安全，是风险" |
| `SH-R03` | `Anchor` | 建立记忆点 | 冰雕/花路/音叉树/远景中的缕拉影子 | "这个世界有历史，也有某个存在在看着我" |
| `SH-R04` | `Resolution` | 验证核心 loop | 锁门、1-2 波敌人、战斗散热明显 | "主动制造噪音反而让我活下来" |
| `SH-R05` | `Reward` | 给认知奖励 | 小 Lore、短安全窗、工具获取 | "我不只是打赢了，我学会了一件事" |
| `SH-R06` | `Loop` | 银河城兑现 | 开门回返、缩短路线 | "世界被我永久改了一点" |
| `SH-R07` | `Safe` (收束) | 形成段落句号 | Checkpoint / Safe Room | "这是完整的一段旅程" |

---

## 变更记录

| 日期 | 版本 | 内容 |
|------|------|------|
| 2026-03-22 | v1.0 | 初始版本：基于 Minishoot 分析建立目标架构、核心数据结构、迁移策略、4 个 Batch 验收标准 |
