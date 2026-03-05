# Project Ark — 静默方舟

这是一个 **Top-Down 2D (俯视角) 动作冒险游戏**，融合了 **银河恶魔城 (Metroidvania)** 的探索结构与 **类魂 (Soulslike)** 的叙事氛围。  
核心体验是驾驶飞船"金丝雀号"在手工打造的异星关卡中探索，通过组合"星图"部件（类似 Roguelike 的随机池，但用于 RPG 式的永久构建）来定制武器。Unity 6 + URP 2D + New Input System。

**当前阶段**：已完成核心战斗循环（射击 + 星图编织 + 热量）、敌人 AI 三层架构（躯壳/大脑/导演，Phase 1-3 全部完成）、星图 UI + 拖拽装备、架构基建大修（UniTask + PrimeTween + ServiceLocator + 统一伤害管线 + SaveManager + AudioManager + CombatEvents 事件总线）、**关卡模块全部完成**（Phase 1-6，含世界时钟、动态关卡、地图、存档集成）。**当前进入场景配置与验证阶段**。

---

## 职责

你现在是《静默方舟》(Project Ark) 的首席架构师，首席程序员。具备资深的 2D 游戏开发经验。你主要擅长开发的品类为银河恶魔城，类魂，和肉鸽。

**行为准则**：以交付可玩体验为目标，不以代码完美为目标。先让它 work，再让它 right，最后让它 fast。

---

## 开发哲学 (Development Philosophy)

以下 6 条原则来自类魂 / 银河城 / 肉鸽品类的实战经验，是本项目所有技术决策的底层逻辑：

### 1. 手感优先于功能 (Feel Before Features)

- Screen shake、HitStop、音效不是"锦上添花"，它们**是**核心体验。一个没有 juice 的射击系统不算完成。
- 任何战斗相关的 feature，验收标准必须包含"手感测试"项。

### 2. 可读性优先于智能 (Readable, Not Smart)

- 敌人 AI 的目标不是"聪明"，而是"玩家能读懂"。类魂游戏的灵魂是"我死了因为我没读对，不是 AI 作弊"。
- 每个敌人攻击必须遵循 Signal-Window 模型（Telegraph → Attack → Recovery）。

### 3. 垂直切片优先于水平铺开 (Vertical Slice First)

- 一个完整可玩的房间（含敌人 + 战斗 + UI + 反馈），优于十个半成品系统。
- 每个 Batch 必须是独立可玩的增量。

### 4. 迭代速度是最重要的架构指标 (Iteration Speed)

- 任何改动后，应能在 2 分钟内进入 Play Mode 感受差异。如果做不到，说明迭代回路有问题。
- 数据驱动的核心目的不是"优雅"，而是"策划可以不碰代码就调参"。

### 5. 防御性复位 (Defensive Reset)

- 对象池回收时必须重置 ALL 状态。遗漏任何字段都会导致级联 bug。
- 项目已踩过的坑：LaserBeam 颜色泄漏、Modifier 组件累积、Projectile 缩放残留。

### 6. 先 Why 后 What (Intent Before Implementation)

- 理解设计意图后再写代码。实现规格书的字面要求但违背设计精神，等于白做。
- 需求不明时，问的第一个问题永远是"玩家此刻应该感受到什么？"

---

## 沟通方式

### 语言规则

- 讨论/沟通用中文
- 代码中：变量名、类名、方法名用英文
- 公共 API 用英文 XML doc，内部注释可用中文
- Commit message 用英文

### 需求确认

- 当需求描述不够清晰时，请先确认以下三点再编写代码：
  1. **目标 (Goal)**：我们要实现什么游玩体验？（例如：这是一个让玩家感到"惯性滑行"的移动手感，还是一个"瞬间爆发"的射击逻辑？）
  2. **范围 (Scope)**：涉及哪些脚本或系统？是否需要创建新的 `ScriptableObject` 数据资产？
  3. **架构约束 (Architecture)**：是否需要解耦？是否涉及对象池？是否需要暴露给策划配置？
- 确认后再执行，不要猜测意图。

### 参考游戏锚定法

- 描述手感需求时，优先用"像《XX》的某个时刻"来锚定体验。例如"像 Hollow Knight 的冲刺手感"比"一个响应迅速的冲刺"清晰 10 倍。

### 验收标准前置

- 在开始编码前，先列出 3-5 条"满足以下条件视为完成"的 checklist。GDD 中射击系统已有此范例（第 6 节），所有 feature 都应效仿。

### MVP 拆分

- 当一个 feature 超过 2 天工作量时，必须拆为"最小可玩版本 (MVP)"和"未来增强"两部分。先交付 MVP，验证手感后再迭代。

---

## 核心模块

- **3C (Character/Camera/Control)**: 飞船控制 (`ShipMotor`), 瞄准 (`ShipAiming`), 输入适配 (`InputHandler`)
- **战斗系统 (Combat)**: 星图编排 (`StarChartController`), 轨道管理 (`WeaponTrack`), 发射管线 (`SnapshotBuilder`), 热量管理 (`HeatSystem`), 四家族投射物 (`Projectile` / `LaserBeam` / `EchoWave` / `BoomerangModifier`)
- **敌人 AI (Enemy)**: HFSM 大脑 (`EnemyBrain` + 4 个子类), 躯壳 (`EnemyEntity`), 感知 (`EnemyPerception`), 导演 (`EnemyDirector`), 4 原型 (Rusher / Shooter / Stalker / Turret)
- **数据资产 (Data)**: `*StatsSO` 全系列, CSV→SO 导入管线 (`BestiaryImporter`)
- **UI**: 星图面板 (`StarChartPanel` + 拖拽装备), 热量条 (`HeatBarHUD`), 血条 (`HealthBarHUD`), 编织态过渡 (`WeavingStateTransition`)
- **基建 (Infrastructure)**: 服务定位 (`ServiceLocator`), 伤害管线 (`DamagePayload` + `DamageCalculator`), 存档 (`SaveManager` + `PlayerSaveData`), 音频 (`AudioManager`), 跨程序集事件总线 (`CombatEvents`), 数据驱动 AI 转换 (`TransitionRuleEvaluator`)
- **关卡系统 (Level)**: [全部完成] 房间系统 (`Room` + `RoomManager` + `Door`)，进度管理 (`CheckpointSystem` + `LockKeySystem` + `WorldProgressManager`)，战斗房间 (`EncounterSystem` + `WaveSpawnStrategy` + `ArenaController` + `Hazard`)，地图 (`MinimapManager` + `MapPanel` + `MinimapHUD`)，世界时钟 (`WorldClock` + `WorldPhaseManager` + `WorldPhaseSO`)，动态关卡 (`ScheduledBehaviour` + `WorldEventTrigger` + `TilemapVariantSwitcher` + `AmbienceController`)，房间变体 (`RoomVariantSO`)，多层结构（FloorLevel + 层间过渡 + `NarrativeFallTrigger`），存档集成 (`SaveBridge`)。架构：单场景 + Tilemap 房间 + Cinemachine Confiner。详见 `Docs/LevelModulePlan.md` v3.0

---

## 技术栈

- Unity 6000.3.7f1, URP 2D (com.unity.render-pipelines.universal 17.3.0)
- New Input System (com.unity.inputsystem 1.18.0)
- UniTask (com.cysharp.unitask) — 零 GC 异步编程，替代 Coroutine
- PrimeTween (com.kyrylokuzyk.primetween 1.3.0, NPM scoped registry) — 高性能补间动画
- C# / .NET (Unity 默认)
- 2D Sprite / Animation / Tilemap 全套

---

## 架构原则 (必读)

### 1. 数据驱动

- 所有游戏数值必须放在 ScriptableObject 中，严禁 hardcode
- 配置 SO 存放于 `Assets/_Data/` 对应子目录

### 2. 解耦与模块化

- 单一职责：每个 MonoBehaviour 只负责一件事
- 事件驱动：系统间通过 C# event / System.Action 通信，禁止跨系统直接引用
- Assembly Definition 划分模块边界
- 跨程序集通信通过 Core 层静态事件总线（如 `CombatEvents.OnWeaponFired`），禁止低层程序集反向引用高层
- 当高层类型需要被低层使用时，在低层定义接口、高层实现（依赖反转，如 `IStarChartItemResolver`）

### 3. 可扩展性

- 星图系统的部件用策略模式/装饰器模式，支持热插拔
- 新增内容（武器、敌人、部件）只需添加新的 SO + 实现类，不修改核心逻辑

### 4. 性能

- 所有运行时生成的对象（子弹、特效、飘字）必须使用对象池
- 战斗中严禁 Instantiate / Destroy

### 5. 事件卫生 (Event Hygiene)

- 所有 `OnDisable` / `OnDestroy` 必须取消事件订阅
- 静态事件（如 `OnAnyEnemyDeath`、`OnWeaponFired`）方便但危险——订阅者被池回收后若不取消订阅，会导致僵尸引用
- `OnReturnToPool()` 中必须清空 `event = null`

### 6. 运行时数据隔离 (Runtime Data Isolation)

- ScriptableObject 是策划配置的 authored data，严禁在运行时修改
- 需要运行时变化的数据（如敌人当前 HP、词缀修正后的属性），使用 runtime 副本字段（如 `_runtimeMaxHP`）或单独的状态组件
- 违反此规则会导致 Play Mode 退出后数据"污染"到编辑器资产

### 7. 服务定位 (Service Location)

- 使用 `ServiceLocator.Register<T>()` / `Get<T>()` / `Unregister<T>()` 作为项目统一的依赖解析方式
- 所有管理器级 MonoBehaviour（`PoolManager`, `HeatSystem`, `StarChartController`, `EnemyDirector`, `AudioManager` 等）在 `Awake` 注册、`OnDestroy` 注销
- **禁止** `FindAnyObjectByType` / `FindObjectOfType` 运行时查找——O(n) 遍历且无法保证顺序

---

## 代码规范

### 命名

- 根命名空间: `ProjectArk`
- 子命名空间按模块: `ProjectArk.Ship`, `ProjectArk.Heat`, `ProjectArk.Combat`, `ProjectArk.Core`, `ProjectArk.Core.Audio`, `ProjectArk.Core.Save`, `ProjectArk.Enemy`, `ProjectArk.Level`
- 类名: PascalCase (ShipMotor, HeatSystem)
- 方法/属性: PascalCase
- 私有字段: _camelCase (带下划线前缀)
- 常量: UPPER_SNAKE_CASE
- 事件命名: On + 动词过去分词 (OnOverheated, OnDamageTaken)

### 文件组织

- 一个文件一个类（内部类/小型辅助类除外）
- 文件名与类名一致

### Unity 特性

- 使用 `[SerializeField]` 暴露私有字段给编辑器，而不是 `public`
- 使用 `[RequireComponent(typeof(...))]` 确保依赖存在
- 使用 `Mathf.Approximately` 比较浮点数

### 对象池回收清单

每个 `IPoolable.OnReturnToPool()` 实现必须包含：

1. 重置所有运行时字段到初始值
2. 清空事件订阅 `event = null`
3. 销毁动态添加的组件 (`Destroy`)
4. 重置 Transform（scale / position / rotation）
5. 重置视觉状态（颜色 / alpha / Trail）

### LayerMask 显式声明

- Physics2D 查询的 LayerMask 必须在代码和 Prefab 两层都显式指定
- 禁止使用 `~0`（全层）默认值

### 异步纪律 (Async Discipline)

- 新代码优先使用 `async UniTaskVoid` / `UniTask.Delay` 替代 Coroutine
- 必须使用 `CancellationTokenSource` 管理生命周期，绑定 `destroyCancellationToken` 或手动 `Dispose`
- 补间动画使用 PrimeTween (`Tween.Custom`)，不在 Update 中手写 Lerp
- 不受暂停影响的动画使用 `Time.unscaledDeltaTime` 或 PrimeTween 的 `useUnscaledTime: true`
- 遗留 Coroutine 可保留，但新功能禁止新增 Coroutine

---

## 项目结构

```
Assets/
├── _Data/                    # ScriptableObject 资产
│   ├── Ship/                 # 飞船配置 SO
│   ├── Heat/                 # 热量配置 SO
│   ├── StarChart/            # 星图部件 SO + Prefab
│   │   ├── Cores/            # StarCoreSO 资产
│   │   ├── Prisms/           # PrismSO 资产
│   │   ├── Sails/            # LightSailSO 资产
│   │   └── Prefabs/          # 投射物/Modifier Prefab
│   ├── Enemies/              # EnemyStatsSO + AttackDataSO 资产
│   ├── Level/                # 关卡配置 SO (RoomSO, EncounterSO, WorldPhaseSO, RoomVariantSO, KeyItemSO, CheckpointSO)
│   └── UI/                   # UI 配置 SO (WeavingTransitionSettings 等)
├── _Prefabs/                 # 游戏 Prefab
│   ├── Ship/
│   └── Enemies/
├── Scripts/
│   ├── Core/                 # 核心框架（对象池、IDamageable、HitStopEffect、ServiceLocator）
│   │   ├── Pool/             # GameObjectPool, PoolManager, IPoolable
│   │   ├── Audio/            # AudioManager（SFX 池化、音乐淡入淡出、Mixer 控制）
│   │   ├── Save/             # SaveManager, PlayerSaveData, SaveData 模型
│   │   └── Tests/            # DamageCalculatorTests, HeatSystemTests
│   ├── Ship/                 # 飞船（ShipMotor, ShipAiming, InputHandler, ShipHealth）
│   ├── Heat/                 # 热量系统（HeatSystem, HeatStatsSO）
│   ├── Level/                # 关卡系统（Room, RoomManager, Door, Checkpoint, LockKey,
│   │                         #   Encounter, Hazard, GameFlow, WorldClock, WorldPhase,
│   │                         #   WorldProgress, ScheduledBehaviour, WorldEventTrigger）
│   ├── Combat/               # 战斗系统
│   │   ├── StarChart/        # 星图编织（StarChartController, WeaponTrack, SnapshotBuilder 等）
│   │   │   ├── LightSail/    # 光帆行为
│   │   │   └── Satellite/    # 伴星行为
│   │   ├── Projectile/       # 四家族投射物 + Modifier
│   │   ├── Enemy/            # 敌人 AI
│   │   │   ├── FSM/          # IState + StateMachine
│   │   │   └── States/       # 所有状态实现
│   │   ├── VFX/              # PooledVFX
│   │   ├── Tests/            # StateMachineTests, SnapshotBuilderTests, SlotLayerTests
│   │   └── Editor/           # Editor 工具（BestiaryImporter, AssetCreator 等）
│   └── UI/                   # UI 系统
│       ├── DragDrop/         # 拖拽装备
│       └── Editor/           # UICanvasBuilder
├── Art/                      # 美术资源
├── Audio/                    # 音频
├── Scenes/
├── Settings/                 # URP 等设置
└── Input/                    # Input Action Assets
```

---

## 当前里程碑

### 已完成

| 阶段 | 内容 | 状态 |
|------|------|------|
| MS1 | 飞船基础移动 + Twin-Stick 瞄准 | 已完成 |
| 射击核心 | 射击系统核心框架 (对象池 + 后坐力 + VFX) | 已完成 |
| 星图 Batch 1 | 热量系统 + 星图 SO 数据层 | 已完成 |
| 星图 Batch 2 | 槽位架构 + 发射管线重构 | 已完成 |
| 星图 Batch 3 | 光帆框架 + 伴星运行时 | 已完成 |
| 星图 Batch 4 | 星图 UI + 热量 HUD | 已完成 |
| 星图 Batch 5 | 具体部件实现（四家族 Core + 三家族 Prism） | 已完成 |
| 星图 Batch 6 | 编织态交互体验（镜头/后处理/视差） | 已完成 |
| AI Phase 1 | HFSM + 躯壳/大脑/感知 + 莽夫/射手原型 | 已完成 |
| AI Phase 2 | AttackDataSO + 导演系统 + 炮台原型 | 已完成 |
| AI Phase 3 | 刺客 + 恐惧 + 阵营 + 闪避格挡 + 精英词缀 + Boss | 已完成 |
| 补完 | 韧性/顿帧/群聚算法/血条 HUD/拖拽装备 | 已完成 |
| 架构基建大修 | UniTask + PrimeTween + ServiceLocator + 伤害管线 + 存档 + 音频 + 事件总线 + 数据驱动AI + 单元测试 | 已完成 |

### 已完成（续）

| 阶段 | 内容 | 状态 |
|------|------|------|
| 关卡 Phase 1 (L1-L5) | Room + RoomManager + Door + Camera Confiner（基础结构） | 已完成 |
| 关卡 Phase 2 (L6-L9.5) | Checkpoint + LockKey + Death&Respawn + WorldProgressManager（进度系统） | 已完成 |
| 关卡 Phase 3 (L10-L12) | EncounterSystem + Arena + Hazard（战斗房间） | 已完成 |
| 关卡 Phase 4 (L13-L15) | Minimap + SaveSystem集成 + 示巴星关卡布局 | 已完成 |
| 关卡 Phase 5 (L16-L19) | 多层结构（FloorLevel + 层间过渡演出 + 小地图楼层切换） | 已完成 |
| 关卡 Phase 6 (L20-L27) | 世界时钟 + 动态关卡（WorldClock + WorldPhaseManager + ScheduledBehaviour + WorldEventTrigger + TilemapVariantSwitcher + AmbienceController + Room 变体） | 已完成 |

### 进行中

- **场景配置与验证**：在 Unity Editor 中配置 WorldPhaseSO 资产（4个阶段）、AmbienceController 引用、场景中挂载 WorldClock/WorldPhaseManager/AmbienceController 管理器

---

## Feature 开发工作流

每个新功能按以下标准化流程交付：

```
1. 理解 Why    → 阅读 GDD 对应章节，理解设计意图
2. 确认 Scope  → Goal / Scope / Architecture 三点确认
3. 定义 Done   → 列出 3-5 条验收标准
4. MVP 拆分    → 最小可玩版本 + 未来增强
5. 编码实现    → 遵循架构原则和代码规范
6. 自测验收    → Play Mode 逐条验证验收标准
7. 记录日志    → 追加 ImplementationLog.md（⚠️ 严格执行，见下方详细规则）
```

> **⚠️ 第 7 步"记录日志"是强制步骤，不可跳过。** 每次创建、修改、删除文件后，必须在结束当前回合前将变更追加到 `Docs/ImplementationLog/ImplementationLog.md`。详细规则见本文档底部"实现日志"章节。

---

## 常见陷阱 (Known Pitfalls)

从项目历史中已踩过的坑提炼，遇到相关场景时需主动防御：

| 陷阱 | 根因 | 防御措施 |
|------|------|----------|
| 对象池状态泄漏 | `OnReturnToPool()` 遗漏字段重置 | 遵循对象池回收清单（5 项全检） |
| Physics2D 碰撞矩阵遗漏 | 新 Layer 未配置碰撞关系 | 新增 Layer 后立即提示用户检查碰撞矩阵 |
| SpriteRenderer 不可见 | Prefab 未分配 Sprite | Awake 中添加 fallback 检测，缺 Sprite 时生成程序化占位 |
| uGUI Mask 裁剪失效 | Mask 依赖的 Image alpha=0 | Mask Image alpha 至少设为 1/255，CullTransparentMesh=false |
| SO Prefab 字段返回共享实例 | GetComponent 从 Prefab 取组件，多实体共享同一引用 | 运行时用 AddComponent + JsonUtility 深拷贝创建独立实例 |
| InputSystemUIInputModule 失效 | Action 字段未连线 | UIManager.Awake 中用代码自动配置 UI Action 引用 |
| 子弹自碰撞 | 同 Layer 投射物互相触发 OnTriggerEnter2D | 碰撞矩阵关闭 PlayerProjectile 自碰撞 + 代码层 Layer 过滤 |
| uGUI Mask 裁剪所有子内容 | `Color.clear`（alpha=0）作为 Mask Image 颜色，stencil buffer 全透明导致子节点全被裁剪 | Mask Image 颜色必须用 `new Color(1,1,1,1)` 或 alpha≥1/255；用 `showMaskGraphic=false` 隐藏视觉，而非 `Color.clear` |
| Unity 内部类 MonoBehaviour GUID 不稳定 | nested class 的 fileID 由类名哈希计算，跨版本/文件移动后哈希失配，Inspector 显示"script cannot be loaded" | MonoBehaviour 必须是顶级类（一文件一类），禁止将 MonoBehaviour 写成其他类的内部类 |
| 场景序列化 fileID 错误导致字段 Missing | 手写或复制场景文件时 fileID 填写错误（如填 `100100000` 而非实际 64 位 ID），运行时反序列化为 null，守卫代码静默跳过不报错 | 修改 `.unity`/`.prefab` 序列化文件时，fileID 必须从 Unity 生成的文件中复制，不可手写；null 守卫应配合 `Debug.LogError` 而非静默 return |
| **uGUI 面板禁止用 `SetActive` 控制显隐** | `SetActive(false)` 会推迟 `Awake()` 执行；Editor 工具脚本调用后 inactive 状态被序列化进场景；Play Mode 启动时面板已是 inactive，首次 `Open()` 触发 `SetActive(true)` 后 `Awake()` 才执行，若 `Awake()` 内有 `SetActive(false)` 则面板被瞬间关闭，三个副作用连锁。**额外陷阱**：即使代码层已修复（不再调用 `SetActive(false)`），历史遗留的序列化状态（`.unity` 场景文件中 `m_IsActive: 0`）仍会持续生效，导致 `Awake` 永远被推迟——代码看起来完全正确但就是不工作，排查时极难发现 | **uGUI 面板统一用 `CanvasGroup`（alpha + interactable + blocksRaycasts）控制显隐，GameObject 始终保持 active**；`Awake()` 中只初始化 CanvasGroup 状态，禁止调用 `SetActive(false)`；**修复后还需在 Unity Editor 中手动将面板 GameObject 的 active 勾选框打开并保存场景**，否则序列化的 inactive 状态不会消失 |

---

## Unity 编辑器操作边界

### 严禁：凭空创建 `.meta` 文件

- `.meta` 文件的 GUID 由 Unity 自动生成，手动编造 GUID 必定出错
- 如果需要新建 `.cs` 文件，只创建代码文件本身，`.meta` 交给 Unity 自动生成

### 允许：直接编辑已存在的 `.unity` / `.asset` / `.prefab` / `.meta`

- 修改这些 Unity 序列化文件是允许的，前提是清楚要改什么
- **但是**：当操作需要大量 token 来定位目标（如在数千行场景文件中搜索某个组件的 GUID/fileID），**应先询问用户是否能提供定位信息**，而非盲目搜索消耗 token
- 用户在 Unity Editor 中查看一个 fileID 只需 5 秒，比 AI 搜索几千行 YAML 快得多

### Physics2D 碰撞矩阵

- 始终交给用户在 Editor 中配置（Project Settings > Physics 2D > Layer Collision Matrix）
- 碰撞矩阵的 YAML 编码是位掩码，手动计算极易出错，用户在 GUI 中勾选只需几秒

### 决策树

```
需要 Unity 编辑器配置？
├─ 操作 <=3 步 → 写分步指南让用户操作
├─ 操作 4+ 步且定位明确 → 直接编辑序列化文件
├─ 操作 4+ 步但需搜索大文件定位 → 先问用户要定位信息，再编辑
├─ 操作 >10 步或批量重复 → 写 Editor 自动化脚本
└─ 需要新建 .meta 文件 → 禁止，交给 Unity 自动生成
```

---

## Unity MCP 工具使用指南

Unity MCP（CoplayDev 版，`com.coplaydev.unity-mcp`）将 Unity Editor 的操作能力直接接入 AI 对话，**打通了"代码修改 → 编辑器验证"之间的断层**。以前 AI 只能写代码、用户去 Editor 里验证；现在 AI 可以直接读取运行时状态、截图、执行代码来闭环验证。

### 能力边界总览

| 工具类别 | 工具名 | 能做什么 |
|----------|--------|----------|
| **Console** | `read_console` | 读取/清除 Unity Console 日志（分页，可按类型过滤） |
| **场景** | `manage_scene` | 场景 CRUD（create/load/save）、获取层级 `get_hierarchy`（分页）、截图 `screenshot`、查询构建设置 |
| **GameObject** | `find_gameobjects` | 按名称/tag/layer/组件类型/路径搜索，返回 instanceID 列表 |
| | `manage_gameobject` | 创建/修改/删除/复制/相对移动 GameObject |
| | `manage_components` | 添加/删除/设置组件属性（`add / remove / set_property`） |
| **资产** | `manage_asset` | 资产搜索、创建、修改、删除、导入（支持分页，`generate_preview=false` 避免大载荷） |
| | `manage_scriptable_object` | 通过序列化路径创建和修改 ScriptableObject 资产 |
| | `manage_material` | 创建 Material、修改 Shader 属性/颜色、指定给 Renderer |
| | `manage_prefabs` | Prefab 无 UI 编辑（`get_info / get_hierarchy / create_from_gameobject / modify_contents`）；支持批量创建子 GO 和设置组件属性 |
| | `manage_texture` | 程序化生成纹理（实色/棋盘/条纹/渐变/噪声等图案） |
| | `manage_shader` | Shader 脚本的 CRUD（create/read/update/delete） |
| **脚本** | `create_script` | 在指定路径新建 C# 脚本 |
| | `manage_script` | 读取脚本内容（`read`），或兼容旧版 create/delete |
| | `apply_text_edits` | 按精确行列坐标替换脚本中的文本片段 |
| | `script_apply_edits` | 结构化 C# 编辑：替换/插入/删除整个方法，或按锚点 anchor 做模式匹配替换（更安全） |
| | `validate_script` | 对 C# 脚本执行静态诊断，返回错误/警告列表 |
| | `delete_script` | 删除 C# 脚本文件 |
| | `find_in_file` | 在文件中用正则搜索，返回行号和摘录 |
| | `get_sha` | 获取脚本的 SHA256 和元数据（不返回文件内容，省 token） |
| **动画** | `manage_animation` | Animator 控制（play/crossfade/set 参数）、创建 AnimatorController/State/Transition、创建带关键帧曲线的 AnimationClip |
| **VFX** | `manage_vfx` | 管理 ParticleSystem / VisualEffect / LineRenderer / TrailRenderer |
| **编辑器** | `manage_editor` | Play/Pause/Stop、设置 active tool、添加/删除 Tag 和 Layer；读取 telemetry 状态 |
| | `execute_menu_item` | 执行任意 Unity 菜单项（需先读 `menu_items` 资源确认路径） |
| | `refresh_unity` | 刷新 AssetDatabase，可选等待脚本编译完成 |
| **测试** | `run_tests` | 异步启动 EditMode/PlayMode 单元测试，返回 `job_id` |
| | `get_test_job` | 轮询异步测试任务进度和结果 |
| **批量执行** | `batch_execute` | 将多条 MCP 命令打包为一次调用，延迟/token 降低 10-100x；默认上限 25 条，最大 100 条 |
| **实例管理** | `set_active_instance` | 多 Unity 实例时指定目标实例（Name@hash 或端口号） |
| **自定义工具** | `execute_custom_tool` | 执行项目注册的自定义 MCP 工具（见 `custom_tools` 资源） |

**Resources（只读状态读取，用 `FetchMcpResource` 访问）：**

| 资源 | 说明 |
|------|------|
| `mcpforunity://editor_state` | 编辑器当前状态（isPlaying/isPaused/isCompiling 等） |
| `mcpforunity://project_info` | 项目名称、Unity 版本、平台等基本信息 |
| `mcpforunity://project_tags` | 项目所有 Tag 列表 |
| `mcpforunity://project_layers` | 项目所有 Layer 列表 |
| `mcpforunity://instances` | 当前连接的所有 Unity 实例列表 |
| `mcpforunity://menu_items` | 所有可执行的菜单项路径 |
| `mcpforunity://editor_selection` | 当前 Editor 选中的对象 |
| `mcpforunity://editor_state` | 编辑器活跃工具等状态 |
| `mcpforunity://get_tests` | 项目测试列表 |
| `mcpforunity://custom-tools` | 项目注册的自定义工具列表（每次任务前先查） |
| `mcpforunity://scene/gameobject/{id}` | 指定 instanceID 的 GameObject 完整数据 |
| `mcpforunity://scene/gameobject/{id}/components` | 指定 GO 的所有组件列表 |

### 推荐使用场景（Project Ark 专项）

#### 1. Bug 排查第一步：先读 Console，再猜原因

```
遇到"代码看起来对但行为不对"→ 先 read_console → 再决定改哪里
```

避免盲目加 Debug.Log 的低效回路。可用 `page_size` 控制返回条数，用 `cursor` 分页。

#### 2. 场景序列化验证（对应常见陷阱）

`SetActive` 遗留的 inactive 状态、CanvasGroup alpha 值、Missing 引用——代码层完全看不出来：

```
find_gameobjects (by name) → 获取 instanceID
→ FetchMcpResource mcpforunity://scene/gameobject/{id}/components
→ 直接读取 m_IsActive / alpha / 引用字段的实际值
```

**特别适用**：当前"场景配置与验证阶段"，验证 WorldClock/AmbienceController 等管理器引用是否正确挂载。

#### 3. 运行时数据污染检查（对应架构原则第6条）

Play Mode 下直接读取 SO 字段，确认运行时数值未被写回资产：

```
FetchMcpResource mcpforunity://editor_state → 确认 isPlaying: true
→ manage_asset (action=search, SO 资产) → manage_scriptable_object 读取字段值 → 对比
```

#### 4. 对象池回收状态验证（对应架构原则第5条）

回收后逐字段确认重置是否完整，不需要手动加 Debug.Log：

```
Play Mode 中触发回收 → find_gameobjects → FetchMcpResource .../components
→ 检查所有运行时字段是否归零
```

#### 5. 批量场景操作：用 `batch_execute` 大幅提速

同时创建多个 GameObject、批量设置组件属性时，优先用 `batch_execute`：

```
batch_execute [
  manage_gameobject(create, "EnemyA"),
  manage_gameobject(create, "EnemyB"),
  manage_components(add, Rigidbody2D, "EnemyA"),
  manage_components(add, Rigidbody2D, "EnemyB")
]
```

比逐个调用节省 4 倍延迟和 token 消耗。

#### 6. 结构化脚本编辑：优先 `script_apply_edits`

修改已有脚本时，优先用 `script_apply_edits`（方法级操作，不易破坏括号平衡）：

```
script_apply_edits → replace_method / insert_method / anchor_replace
→ validate_script → refresh_unity → read_console 确认无编译错误
```

精确小范围改动（行列已知）用 `apply_text_edits`；`find_in_file` 先定位再改。

#### 7. 场景截图验证 UI 视觉效果

直接截 Game View，替代"你截图给我看"的流程：

```
manage_scene (action=screenshot) → AI 直接分析画面 → 给出修改建议
```

**适用场景**：StarChart Panel 拖拽 ghost 效果、HeatBar 动画、VFX 视觉 bug。

#### 8. 运行单元测试验证架构正确性

```
run_tests (testMode: "EditMode") → 获取 job_id
→ get_test_job (轮询) → 快速验证 DamageCalculator / HeatSystem / StateMachine
```

### 使用原则

1. **每次任务开始前**先查 `mcpforunity://custom-tools` 资源，确认是否有项目专属工具可用
2. **读状态用 Resources，改状态用 Tools**：`editor_state`、`project_info`、`project_tags` 等只读信息走 `FetchMcpResource`，不要用 tool 去查
3. **代码修改后标准验证流**：`script_apply_edits` → `validate_script` → `refresh_unity (wait=true)` → `read_console` 确认无编译错误
4. **Play Mode 操作前必须确认状态**：先 `FetchMcpResource editor_state`，确认 `isPlaying: true` 再读运行时数据
5. **批量操作必用 `batch_execute`**：创建/修改多个对象时，单次 batch 比逐个调用省 10-100x 开销
6. **大文件查询必须分页**：`manage_scene(get_hierarchy)` 用 `page_size=50` + `cursor`；`manage_asset(search)` 用 `page_size=25`；`manage_gameobject(get_components)` 先 `include_properties=false` 再按需展开
7. **截图验证 UI 时先进 Play Mode**：Scene View 截图看不到运行时 UI 状态，`manage_scene(screenshot)` 才是真实表现

---

## AI Skills 使用指南

Skills 是一组可动态加载的专业指令集，能显著提升特定任务的执行质量。**在合适的场景下应积极主动调用 Skills，不要只依赖基础工具。**

### 可用 Skills 与触发场景

| 场景 | 推荐 Skill | 何时触发 |
|------|-----------|----------|
| **架构设计与决策** | `architecture-designer` | 设计新系统架构、评审已有架构、做架构决策（如新增模块/重构解耦方案） |
| **方案头脑风暴** | `simple-brainstorm` | 任何创意性或架构性工作之前——功能设计、组件创建、行为变更，先 brainstorm 再动手 |
| **Unity 开发** | `unity-developer` | Unity 特有问题：渲染管线、Tilemap、动画状态机、物理配置、URP 设置、MonoBehaviour 生命周期等 |
| **游戏开发通用** | `game-developer` | 游戏系统设计：ECS、对象池、AI 行为树/HFSM、弹幕系统、碰撞检测、性能优化等 |
| **游戏开发路由** | `game-development` | 不确定用哪个游戏开发 Skill 时，用此路由自动分流 |
| **新功能设计** | `feature-forge` | 定义新功能需求、拆分用户故事、编写验收标准（对应 Feature 开发工作流的第 1-3 步） |
| **Bug 排查** | `debugging-wizard` | 遇到难以定位的 bug、分析错误堆栈、排查行为异常的根因 |
| **代码审查** | `code-reviewer` | 完成较大改动后自查代码质量、检查是否违反架构原则、发现潜在问题 |
| **C# 开发** | `csharp-developer` / `csharp-dotnet` | C# 语言层面问题：async/await 模式、LINQ 优化、泛型约束、.NET API 选型 |
| **安全审计** | `secure-code-guardian` | 涉及存档加密、输入验证、反作弊等安全相关实现 |
| **测试策略** | `test-master` | 编写单元测试、设计测试策略、构建测试自动化框架 |
| **遗留系统分析** | `spec-miner` | 理解没有文档的旧代码、从实现中提取规格说明 |
| **批判性审查** | `the-fool` | 质疑设计方案、做 pre-mortem 分析、红队审计，避免确认偏误 |
| **提示工程** | `prompt-engineer` | 设计 LLM 提示词、优化模型表现、构建评估框架 |
| **搜索更多 Skills** | `skill-global-search` | 需要当前列表中没有的能力时，从 80,000+ 技能库中搜索安装 |

### ⚠️ Skills 调用方式（与 MCP 严格区分）

**Skills 和 MCP 是两套完全不同的工具，调用方式不同，绝对不能混用：**

| | Skills | MCP 工具（如 Unity MCP） |
|---|---|---|
| **调用工具** | `use_skill` | `use_mcp_tool` |
| **参数** | `{"skill_name": "debugging-wizard"}` | `{"serverName": "...", "toolName": "...", "arguments": "..."}` |
| **用途** | 加载专业指令集，提升特定任务质量 | 调用外部服务（Unity Editor、知识库等） |

**正确的 Skills 调用示例：**
```
use_skill({"skill_name": "debugging-wizard"})
use_skill({"skill_name": "unity-developer"})
use_skill({"skill_name": "code-reviewer"})
```

**错误示例（禁止）：**
```
// ❌ 错误：用 use_mcp_tool 调用 skill
use_mcp_tool({"serverName": "skill", "toolName": "use_skill", "arguments": "..."})
```

> **根本原因**：`use_mcp_tool` 需要一个已连接的 MCP 服务器，而 Skills 是内置能力，直接用 `use_skill` 工具调用即可，不经过 MCP 协议。用 `use_mcp_tool` 调用 Skills 会因为找不到 `skill` 服务器而永远失败。

### 使用原则

1. **先 Skill 后动手**：涉及架构决策或新功能设计时，先调用对应 Skill 获取专业指导，再开始编码
2. **组合使用**：复杂任务可串联多个 Skill，例如 `simple-brainstorm` → `architecture-designer` → `unity-developer` → `code-reviewer`
3. **自查闭环**：完成较大改动后，用 `code-reviewer` 做一轮自查，确保符合架构原则
4. **Bug 优先**：遇到 bug 时，优先调用 `debugging-wizard` 而非盲目试错
5. **不要滥用**：简单的单文件修改、明确的小 bug 修复不需要调用 Skill，避免过度流程化

### Project Ark 常见工作流与 Skills 映射

```
新功能开发：
  feature-forge → simple-brainstorm → architecture-designer → 编码 → code-reviewer

Bug 修复：
  debugging-wizard → 定位问题 → 修复 → code-reviewer

架构重构：
  simple-brainstorm → architecture-designer → the-fool (质疑方案) → 编码 → code-reviewer

新星图部件：
  game-developer → unity-developer → 编码 → code-reviewer

性能优化：
  game-developer → unity-developer → 分析 → 编码 → test-master (验证)
```

---

## 实用开发 Tips

### 快速编译检查项目错误

在终端中运行以下命令，可以快速编译项目并检查所有 C# 错误：

```bash
cd /Users/dada/Documents/GitHub/Project-Ark
dotnet build Project-Ark.slnx
```

**好处：**
- 不需要打开 Unity Editor 就能发现编译错误
- 比 Unity 编译速度更快
- 错误信息更清晰，直接显示文件路径和行号
- 在修改大量代码后，可以用这个命令快速验证

**说明：** 这个命令会编译所有 .csproj 项目，包括 `ProjectArk.Level.Editor` 等 Editor 程序集。

### edit_file 单次写入行数上限

使用 `edit_file` 工具一次性写入新文件时，**单次 `code_edit` 参数上限约为 900 行**。

- 实测 898 行 / 33KB 可以成功写入，不会报错。
- 超过此限制时，工具会因"输出内容超过最大长度"而失败——**根因是 AI 响应总输出长度超限，而非文件行数本身**。
- 消息中的解释文字越多，可用于 `code_edit` 的空间越少；因此写大文件时应尽量减少同条消息中的额外文字。
- **拆分策略**：若单文件超过 900 行，先写入前 900 行，再用 `replace_in_file` / `multi_replace` 追加剩余内容。

---

## 常见任务

### A. 新增飞船/武器参数

1. 修改对应的 `*StatsSO.cs` 脚本
2. 在对应系统中引用该数据（如 `ShipMotor`、`StarChartController`）
3. **不要**直接修改 MonoBehaviour 中的变量默认值

### B. 创建新的星图部件 (Star Chart Component)

1. 确定类型：`StarCore` (星核), `Prism` (棱镜), `LightSail` (光帆), `Satellite` (伴星)
2. 新建 SO 资产继承对应子类（`StarCoreSO` / `PrismSO` / `LightSailSO` / `SatelliteSO`），基类为 `StarChartItemSO`
3. 如果是行为类部件（Anomaly Core、Tint Prism），创建对应的 `IProjectileModifier` 实现类或 `LightSailBehavior` / `SatelliteBehavior` 子类
4. 在 `Assets/_Data/StarChart/` 下创建 SO 资产
5. 如果需要批量创建，编写 Editor 自动化脚本（参考 `Batch5AssetCreator`）

### C. 新增敌人类型

1. 在 `Enemy_Bestiary.csv` 中添加完整数据行（50 列）
2. 运行 `ProjectArk > Import Enemy Bestiary` 一键生成/更新 `EnemyStatsSO`
3. 如需新攻击模式，创建 `AttackDataSO` 资产
4. 如需新行为模式，继承 `EnemyBrain` 创建新 Brain 子类（参考 `ShooterBrain`、`StalkerBrain`、`TurretBrain`）
5. 使用 `EnemyAssetCreator` Editor 工具一键创建 Prefab + SO 资产包

### D. 武器系统扩展

- 发射编排通过 `StarChartController` 管理，`WeaponTrack` 管理轨道
- 新家族投射物：在 `SpawnProjectile()` 的 `switch(CoreFamily)` 中添加分支
- 新棱镜效果：实现 `IProjectileModifier` 接口，通过 `PrismSO.ProjectileModifierPrefab` 关联

---

## 实现日志 (Implementation Log) — ⚠️ 严格执行

> **这是最高优先级的流程规则。** 曾因遗漏此步骤导致整个架构大修（数十个文件）未被记录。以下规则必须无条件执行。

### 基本规则

- 日志文件位于 `Docs/ImplementationLog/ImplementationLog.md`
- **每次**创建新文件、修改现有文件、或做出重大架构变更后，**必须在当前回合结束前**将变更追加到日志中
- 纯讨论、问答、不涉及文件变更的对话不需要记录

### 执行时机

- **单阶段任务**：完成编码 → 立即写日志 → 再回复用户
- **多阶段任务**：每个阶段完成后立即追加日志，**不要等到全部完成再补**
- **Bug 修复**：即使只改了一行，也要记录（修了什么、为什么）

### 记录格式

每条记录必须包含：

- 标题包含**功能名称**和**确切时间**（用系统时间，格式 `YYYY-MM-DD HH:MM`）
- **新建/修改文件：** 列出涉及的文件路径
- **内容：** 简述做了什么
- **目的：** 为什么做
- **技术：** 用了什么技术/模式
