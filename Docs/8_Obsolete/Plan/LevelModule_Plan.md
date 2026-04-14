# 🏗️ 关卡模块搭建方案 — Level Module Architecture Plan

**文档版本**：v3.0  
**创建日期**：2026-02-12  
**最后更新**：2026-02-12  
**作者**：首席架构师 (AI)  
**项目**：静默方舟 (Project Ark)  

> **v3.0 更新**：对齐架构基建大修（UniTask + PrimeTween + ServiceLocator + 统一伤害管线 + SaveManager + AudioManager + CombatEvents 事件总线）后的技术栈，更新程序集规划、集成点、异步模式和存档集成。  
> **v2.0 更新**：加入多层结构（方案C：混合方案）和世界时钟与动态关卡（方案C：事件阶段+轻量循环周期）的完整设计。

---

## 一、当前项目状态总结

| 模块 | 状态 |
|------|------|
| 3C (移动/瞄准/输入) | ✅ 完成 |
| 星图编织系统 (武器/棱镜/光帆/伴星) | ✅ 代码完成，待编辑器资产 |
| 热量系统 | ✅ 完成 |
| 敌人 AI Phase 1-3 (HFSM + 4原型 + 恐惧/阵营/词缀/Boss) | ✅ 完成 |
| UI (星图面板 + HUD + 拖拽装备) | ✅ 完成 |
| 怪物图鉴 (P1+P2 共 26 种) | ✅ 完成 |
| 星图部件数据 (43 件) | ✅ 完成 |
| **架构基建大修** | ✅ 完成 |
| 　├ UniTask + PrimeTween（异步 + 补间） | ✅ 已集成，替代 Coroutine |
| 　├ ServiceLocator（依赖解析） | ✅ PoolManager/HeatSystem/EnemyDirector/AudioManager 已注册 |
| 　├ 统一伤害管线 (DamagePayload + DamageCalculator) | ✅ 元素抗性/格挡减伤 |
| 　├ SaveManager + PlayerSaveData | ✅ JSON 序列化，含 ProgressSaveData（VisitedRoomIDs/DefeatedBossIDs/Flags） |
| 　├ AudioManager（SFX 池化 + 音乐淡入淡出 + Mixer） | ✅ ServiceLocator 注册 |
| 　├ CombatEvents 跨程序集事件总线 | ✅ OnWeaponFired |
| 　└ 单元测试基础设施 | ✅ NUnit + Unity Test Framework |
| **关卡系统** | ❌ **完全空白** |

---

## 二、关卡模块核心需求分析

基于 GDD（银河恶魔城探索结构 + 类魂叙事）和示巴星关卡设计文档，关卡模块需要支撑以下体验：

```
关卡模块
├── 世界结构
│   ├── 星球 (Planet)
│   ├── 区域 (Zone)
│   └── 房间 (Room)
├── 房间系统
│   ├── 房间加载/卸载
│   ├── 房间内敌人配置
│   ├── 门/通道连接
│   └── 环境机关
├── 进度管理
│   ├── 锁钥系统
│   ├── 检查点 (Checkpoint)
│   ├── 地图探索记录
│   └── 物品拾取
└── 玩家生死循环
    ├── 死亡/重生
    └── 存档/读取
```

---

## 三、推荐架构方案：单场景 + Tilemap 房间

对于 2D Top-Down 银河恶魔城，**不推荐**多场景异步加载（开发复杂度高、Tilemap 跨场景管理困难）。  
推荐**单场景内用 Tilemap + Collider 划分房间**，配合 Cinemachine Confiner 实现镜头约束。

### 架构分层

```
┌─────────────────────────────────────────────────────────┐
│                   LevelManager (全局单例)                  │
│  负责关卡流程编排：加载→探索→Boss→通关→下一星球            │
└───────────────────────┬─────────────────────────────────┘
                        │
┌───────────────────────▼─────────────────────────────────┐
│            WorldClock + WorldPhaseManager                  │
│  游戏内时钟 · 阶段切换 · 事件广播 · 进度事件监听           │
└───────────────────────┬─────────────────────────────────┘
                        │
┌───────────────────────▼─────────────────────────────────┐
│               RoomManager (场景内管理器)                    │
│  追踪当前房间 · 触发房间事件 · 管理房间敌人激活/休眠        │
│  支持多层楼层(FloorLevel)切换 · 房间变体(Phase/Time)管理   │
└───────────────────────┬─────────────────────────────────┘
                        │
┌───────────────────────▼─────────────────────────────────┐
│  Room (单个房间)                                          │
│  ┌──────┐ ┌──────────┐ ┌──────────┐ ┌───────────┐     │
│  │ 边界  │ │ 敌人配置  │ │ 门/通道   │ │ 环境机关   │    │
│  │Bounds │ │SpawnData │ │DoorLink  │ │ Hazard    │     │
│  └──────┘ └──────────┘ └──────────┘ └───────────┘     │
│  ┌───────────────────┐ ┌───────────────────────────┐   │
│  │ 楼层标记 FloorLvl │ │ 房间变体 RoomVariant[]    │   │
│  └───────────────────┘ └───────────────────────────┘   │
└─────────────────────────────────────────────────────────┘
                        │
┌───────────────────────▼─────────────────────────────────┐
│  进度系统 (Progression)                                   │
│  CheckpointSystem · LockKeySystem · MapReveal · Pickup   │
└─────────────────────────────────────────────────────────┘
                        │
┌───────────────────────▼─────────────────────────────────┐
│  动态世界层 (Dynamic World)                               │
│  ScheduledBehaviour · WorldEventTrigger · TilemapVariant  │
└─────────────────────────────────────────────────────────┘
```

---

## 四、实现计划（按优先级排序）

### Phase 1: 基础结构 (P0 — 必须先做)

| 批次 | 内容 | 说明 |
|------|------|------|
| **L1** | **Room 数据定义** | `RoomSO`（轻量元数据）— 只存非空间信息：RoomID / DisplayName / FloorLevel / MapIcon / RoomType(Normal/Arena/Boss/Safe) / EncounterSO引用。**不存**边界/门列表/Tilemap/生成点位置（这些由场景实体管理）|
| **L2** | **Room 运行时** | `Room` MonoBehaviour — 引用 `RoomSO` 获取元数据；自身持有 BoxCollider2D Trigger（边界）、Transform[] SpawnPoints、Door[] Doors；`RoomState`（未发现/已进入/已清理/已锁定）。场景即数据：空间布局全在场景中所见即所得 |
| **L3** | **RoomManager** | ServiceLocator 注册的管理器 — 追踪 `CurrentRoom`，广播 `LevelEvents.OnRoomEntered`/`OnRoomCleared`（Core 层静态事件总线，供 UI/Save/任何层消费），管理房间敌人激活/休眠（性能），房间切换时通知 `EnemyDirector` 清空令牌 |
| **L4** | **Door / Passage** | `Door` 组件 — 两端 `Room` 引用、开/关状态、锁定条件；支持"清怪开门"和"钥匙开门"两种模式 |
| **L5** | **Camera Confiner** | 利用 Cinemachine 2D Confiner 将相机限制在当前房间边界内，房间切换时平滑过渡 |

### Phase 2: 进度系统 (P0)

| 批次 | 内容 | 说明 |
|------|------|------|
| **L6** | **CheckpointSystem** | `Checkpoint` 组件 — 交互激活、通过 `ServiceLocator.Get<ShipHealth>()` 恢复 HP + `ServiceLocator.Get<HeatSystem>().ResetHeat()` 恢复热量、设置重生点；`CheckpointManager`（ServiceLocator 注册）管理活跃检查点 + 触发 `SaveManager.Save()` |
| **L7** | **LockKeySystem** | `KeyItem` SO + `Lock` 组件 — 数据驱动的锁钥矩阵；支持彩色钥匙卡、Boss掉落钥匙、能力门（需要特定光帆） |
| **L8** | **ItemPickup** | `PickupBase` 抽象类 + 子类（StarChartPickup/HealthPickup/KeyPickup）— 掉落物系统 |
| **L9** | **Death & Respawn** | `GameFlowManager` — 通过 `ServiceLocator.Get<ShipHealth>()` 订阅 `OnDeath`；使用 `async UniTaskVoid` 编排死亡演出（PrimeTween 淡黑 + AudioManager 音效）→ 重置房间敌人 → 在 `CheckpointManager` 活跃点重生 → `SaveManager.Save()` |
| **L9.5** | **WorldProgressManager**（从 Phase 6 提前） | 监听 `LevelEvents.OnBossDefeated` / `OnKeyItemObtained` 等里程碑事件，管理不可逆大阶段（如"Boss存活期"→"Boss击杀后"）。P0 必需：驱动"击杀 Boss → 永久开门"这一核心银河恶魔城进度机制。使用已有 `ProgressSaveData.Flags` + `DefeatedBossIDs` 持久化 |

### Phase 3: 战斗房间逻辑 (P1)

| 批次 | 内容 | 说明 |
|------|------|------|
| **L10** | **EncounterSystem** | `EnemySpawner` + `WaveSpawnStrategy` — 房间的 `EnemySpawner` 注入 `WaveSpawnStrategy`，由 `EncounterSO` 配置波次（Wave 1: 3×Crawler, Wave 2: 2×Drone+1×Loader）；追踪存活数；全清后回调 `Room` 触发奖励/开门 |
| **L11** | **Arena Room** | 进入时锁门→播放警报→逐波刷怪→全清后开门+掉落奖励；支持 Boss 房间变体 |
| **L12** | **Hazard System** | `EnvironmentHazard` 基类 + 具体实现（酸液池/激光栅栏/钻头陷阱/地雷）。使用 `DamagePayload(DamageType.Fire/Ice/...)` 通过统一伤害管线对 `IDamageable` 造伤（享受元素抗性/格挡减伤），独立于敌人 AI |

### Phase 4: 地图与探索 (P2)

| 批次 | 内容 | 说明 |
|------|------|------|
| **L13** | **Minimap / Map** | 房间拓扑小地图，已探索房间可见，未探索为迷雾；支持多楼层切换显示 |
| **L14** | **SaveSystem 集成** | 已有 `SaveManager` + `PlayerSaveData`（含 `ProgressSaveData`）。Level 模块需扩展：补充 `WorldClockTime`/`WorldStage` 字段、在检查点/Boss击杀/房间首次进入时调用 `SaveManager.Save()`、`GameFlowManager` 启动时调用 `SaveManager.Load()` 恢复进度 |
| **L15** | **示巴星关卡布局** | 用 Tilemap 实际搭建示巴星的房间网络，填充怪物配置，完成首个可玩关卡 |

### Phase 5: 多层结构 (P2)

| 批次 | 内容 | 说明 |
|------|------|------|
| **L16** | **FloorLevel 数据扩展** | `RoomSO` 添加 `int FloorLevel` 字段；`Door` 添加 `bool IsLayerTransition` 标记，区分普通门和层间通道 |
| **L17** | **层间过渡演出** | 层间通道使用区别于普通门的过渡效果（PrimeTween 更长淡黑 + 下坠/上升粒子 + `AudioManager.PlaySFX()` 环境音效切换 + BGM crossfade）；参考 `WeavingStateTransition.cs` 的 UniTask + PrimeTween 过渡模式 |
| **L18** | **小地图楼层切换** | 小地图 UI 增加楼层切换按钮/标签，显示当前楼层高亮，支持查看已探索的其他楼层 |
| **L19** | **叙事级无缝掉落（可选）** | 用于极少数关键叙事时刻（如首次发现裂隙），通过 Timeline/关卡脚本实现真正的无缝垂直过渡演出 |

### Phase 6: 世界时钟与动态关卡 (P2)

| 批次 | 内容 | 说明 |
|------|------|------|
| **L20** | **WorldClock** | 游戏内时钟核心 — 可配置周期长度（如20分钟现实时间=一个星球自转周期）、时间流速、暂停/恢复；广播 `OnTimeChanged` 事件 |
| **L21** | **WorldPhaseManager + WorldPhaseSO** | 定义时间阶段列表（如辐射潮/平静期/风暴期/寂静时）；监听 WorldClock 判断当前阶段；阶段切换时广播 `OnPhaseChanged` 事件 |
| ~~L22~~ | ~~WorldProgressManager~~ | ⬆️ **已提前至 Phase 2 (L9.5)**，因为它是"Boss击杀→永久开门"的 P0 依赖 |
| **L23** | **ScheduledBehaviour** | 通用时间驱动组件 — 挂在任何 GameObject 上，配置"在 Phase X 时启用/禁用"；用于 NPC 交易时间、大门定时开关、敌人夜间增强、隐藏通道显现等 |
| **L24** | **WorldEventTrigger** | 进度事件驱动的永久变化组件 — 监听 WorldProgressManager 的大阶段切换，触发不可逆的世界改变（新区域开放、NPC 迁移、地形变化）|
| **L25** | **Room 多变体支持** | `Room` 支持持有多套 SpawnConfig/环境配置（按时间阶段或世界阶段切换）；`RoomVariantSO` 数据定义 |
| **L26** | **Tilemap 变体切换** | 预制多版本 Tilemap（如塌陷前/塌陷后），事件触发时禁用旧版本启用新版本，实现关卡结构性改变 |
| **L27** | **全局氛围系统** | 阶段切换时驱动后处理 Volume 渐变（PrimeTween）、环境粒子启停、BGM crossfade（`AudioManager.PlayMusic()` 已支持淡入淡出）、低通滤波（`AudioManager.ApplyLowPassFilter()`），营造时间流逝的视觉/听觉反馈 |

---

## 五、关键架构决策

### 1. 房间划分方案 — Collider Bounds + Trigger

```
每个房间 = 一个 Trigger Collider2D (BoxCollider2D, isTrigger=true)
玩家进入 → RoomManager.OnRoomEntered(room) → 
  激活本房间敌人 → 休眠远处房间敌人 → 更新相机 Confiner
```

### 2. 敌人生成改造 — 策略模式重构

> **决策**：改造现有 `EnemySpawner` 为策略模式，而非新建独立类。代码复用最大化，单一入口。

将 `EnemySpawner` 拆为 `EnemySpawner`（上下文）+ `ISpawnStrategy`（策略接口）：

```
ISpawnStrategy
├── LoopSpawnStrategy     // 原有行为：死亡后延迟重生、循环刷怪（沙盒/调试场景用）
└── WaveSpawnStrategy     // 新增：EncounterSO 驱动、多波次、多 Prefab 类型、波次间延迟
```

**改造要点**：
- `EnemySpawner` 保留对象池管理（`GameObjectPool`）、精英词缀、生成点轮询等通用逻辑
- 把"何时生成、生成什么、生成多少"的决策抽到 `ISpawnStrategy` 中
- `WaveSpawnStrategy` 接受 `EncounterSO` 配置，波次间延迟使用 `async UniTaskVoid` + `UniTask.Delay()`
- 订阅 `EnemyEntity.OnDeath` 追踪波次存活数，全清后通知 `Room` 触发事件（开门/掉落）
- `EnemyDirector` 令牌系统**无需改动**——Room 切换时由 `RoomManager` 通知 Director 清空令牌即可
- `LoopSpawnStrategy` 封装原有逻辑，保持向后兼容

### 3. 门/通道设计 — 双向引用 + 状态机

```
enum DoorState { Open, Locked_Combat, Locked_Key, Locked_Ability, Locked_Schedule }
// Locked_Schedule: 由世界时钟阶段控制开关（如"平静期"开、"风暴期"关）
```

**过渡演出方式**（参考 `WeavingStateTransition.cs` 的 UniTask + PrimeTween 模式）：
```
Door transition: 玩家走到门口 → async UniTaskVoid:
  PrimeTween 淡黑(0.2s) → 传送玩家到目标入口点 → 
  RoomManager.SetCurrentRoom() → 更新 Cinemachine Confiner →
  PrimeTween 淡入(0.2s) → AudioManager.PlaySFX(门开音效)

Layer transition: 玩家进入裂隙/升降梯 → async UniTaskVoid:
  PrimeTween 更长淡黑(0.5s) → 下坠/上升粒子特效 → 
  AudioManager 环境音效切换 + BGM crossfade →
  传送到目标层房间 → PrimeTween 淡入(0.5s)
```

### 4. 程序集规划

```
Assets/Scripts/Level/ProjectArk.Level.asmdef
  引用: ProjectArk.Core, ProjectArk.Combat, ProjectArk.Ship, 
        ProjectArk.Enemy, ProjectArk.Heat, ProjectArk.Core.Audio,
        UniTask, PrimeTween.Runtime
  包含: Room, RoomManager, Door, Checkpoint, LockKey, Encounter, Hazard, GameFlow,
        WorldClock, WorldPhase, WorldProgress, ScheduledBehaviour, WorldEventTrigger
```

> **注意**：Level 需要引用 Enemy 程序集以访问 `EnemyEntity.OnAnyEnemyDeath` 和 `EnemyDirector` API。
> 引用 Heat 以在 Checkpoint 中恢复热量。引用 Core.Audio 以使用 `AudioManager` 播放转场/氛围音效。
> 引用 UniTask + PrimeTween 遵循项目异步纪律（禁止新增 Coroutine）。

### 5. 数据驱动

- `RoomSO`（轻量）— 房间元数据（ID/名称/楼层/地图图标/房间类型/EncounterSO引用）。**不含**边界/门列表/Tilemap 等空间数据（由场景 Room MonoBehaviour 管理）
- `EncounterSO` — 战斗遭遇波次数据（引用 `EnemyStatsSO` + Prefab + 波次间延迟）
- `LevelLayoutSO` — 星球级别的房间拓扑关系（可选，地图系统用）
- `WorldPhaseSO` — 世界时间阶段定义（阶段名/起止时间/环境参数/氛围配置）
- `RoomVariantSO` — 房间变体数据（不同阶段下的敌人配置/Tilemap 引用/环境参数）
- `KeyItemSO` — 钥匙/解锁物品定义（ID/显示名/图标/描述）
- `CheckpointSO` — 检查点配置（是否恢复 HP/Heat/可选对话触发）

> 所有 SO 资产存放于 `Assets/_Data/Level/` 下对应子目录，遵循项目数据驱动原则。

### 6. 异步模式规范

Level 模块中所有异步操作（房间过渡淡黑、死亡演出、波次延迟等）必须遵循项目异步纪律：

```csharp
// ✅ 正确：UniTask + CancellationTokenSource
private CancellationTokenSource _transitionCts;

private async UniTaskVoid TransitionToRoom(Room targetRoom, Door door)
{
    _transitionCts?.Cancel();
    _transitionCts?.Dispose();
    _transitionCts = new CancellationTokenSource();
    var token = _transitionCts.Token;
    
    // 淡黑：PrimeTween
    _ = Tween.Custom(0f, 1f, 0.3f, useUnscaledTime: true,
        onValueChange: v => _fadeImage.color = new Color(0, 0, 0, v));
    await UniTask.Delay(300, cancellationToken: token);
    
    // 传送玩家
    _ship.transform.position = door.TargetSpawnPoint.position;
    RoomManager.SetCurrentRoom(targetRoom);
    
    // 淡入
    _ = Tween.Custom(1f, 0f, 0.3f, useUnscaledTime: true,
        onValueChange: v => _fadeImage.color = new Color(0, 0, 0, v));
}

// ❌ 禁止：新增 Coroutine
// private IEnumerator TransitionCoroutine() { ... }
```

### 7. ServiceLocator 集成规范

Level 模块的管理器级组件在 Awake 注册、OnDestroy 注销：

```csharp
// RoomManager, CheckpointManager, GameFlowManager, WorldClock 等
private void Awake()
{
    ServiceLocator.Register(this);
}

private void OnDestroy()
{
    ServiceLocator.Unregister(this);
}
```

消费已有服务（禁止 FindAnyObjectByType）：

```csharp
var poolManager = ServiceLocator.Get<PoolManager>();
var heatSystem = ServiceLocator.Get<HeatSystem>();
var audioManager = ServiceLocator.Get<AudioManager>();
var enemyDirector = ServiceLocator.Get<EnemyDirector>();
```

---

## 六、多层结构设计（方案 C：混合方案）

### 决策背景

虽然本项目是 Top-Down View，但我们希望支持**垂直多层结构**——玩家可以掉进裂隙进入地底层、坐升降梯到达上层设施等。这为银河恶魔城式探索增加了一个全新维度，也让关卡空间感更加丰富。

### 方案评估

我们评估了三种方案后选定 **方案 C（混合方案）**：

| 方案 | 思路 | 优点 | 缺点 |
|------|------|------|------|
| **A: Layer as Room** | 不同层的房间在世界空间中物理分离（如地底层整体偏移 Y -500），层间通过特殊 Door 传送 | 零额外架构改动、每层独立主题、完全复用现有 Room/Door 系统 | 过渡时有短暂黑屏（不是无缝掉落感）；看不到层间重叠透视效果 |
| **B: Seamless Vertical** | 地底层 Tilemap 就在地表层下方（物理位置对应），玩家真的向下掉 | 极其沉浸、有真实"掉下去了"的感觉 | Tilemap 渲染排序复杂、地底布局受限于地表位置、相机过渡逻辑复杂、多层嵌套管理困难 |
| **✅ C: 混合方案** | 99% 用方案 A（淡黑传送），极少数叙事关键时刻用方案 B（无缝掉落演出） | 兼得两者优点：日常简洁可靠，关键时刻震撼 | 无缝掉落部分需要一次性关卡脚本/Timeline，但不需要做成通用系统 |

### 技术实现

#### 日常层间过渡（方案 A 部分）

```
地表走廊 (Room_Surface_01, FloorLevel=0)
     │
     │  ← 裂隙入口（Door, IsLayerTransition=true）
     ▼
地底洞穴 (Room_Underground_01, FloorLevel=-1)
     │
     │  ← 隧道出口（Door, IsLayerTransition=true）
     ▼
地底深处 (Room_Underground_02, FloorLevel=-2)
```

- 不同楼层的房间在同一个 Unity Scene 中，但在世界坐标上**物理分离**（如地底层整体偏移 Y -500 units）
- 对 RoomManager 来说，层间传送和普通过门**完全一样**——它不关心房间的世界坐标位置
- 每一层可以有**完全不同的 Tilemap 主题/色调/环境机关/音乐**
- 层间过渡使用区别于普通门的演出效果（更长淡黑 + 下坠粒子 + 环境音效切换）

#### 叙事级无缝掉落（方案 B 部分，少数时刻）

- 用于极少数**叙事关键时刻**（如首次发现裂隙、Boss 击破地板、剧情坍塌）
- 不做成通用系统，而是**一次性关卡脚本/Timeline 演出**
- 实现方式：地底 Tilemap 就在地表下方 → 裂隙处有洞 → 玩家下坠 → Cinemachine Confiner 暂时放大 → 着陆后切换到地底层 Confiner
- 给玩家 **"哇"** 的瞬间体验

### 对架构的影响

| 组件 | 是否需要改 | 改什么 |
|------|----------|--------|
| `Room` | ⬜ 不改 | 房间不在乎自己在哪一层 |
| `RoomSO` | 🔲 小改 | 添加 `int FloorLevel` 字段，标记属于哪一层 |
| `Door/Passage` | 🔲 小改 | 添加 `bool IsLayerTransition` 标记，可能配合不同过渡动画 |
| `RoomManager` | ⬜ 不改 | 它只追踪 "当前房间"，不关心层 |
| `Minimap` | 🔲 需要考虑 | 地图需要"楼层切换"功能 |
| **Camera Confiner** | ⬜ 不改 | 每个房间本来就有自己的 Confiner 边界 |

### 设计运用

在我们的星球设定里，多层结构可以用于：
- **示巴星的结晶裂隙** — 地表有一条巨大的裂缝，掉下去是一个全新的结晶洞穴网络（FloorLevel -1）
- **废弃太空船坞** — 主层是船坞大厅，向下通往被遗忘的维修通道和货仓区
- **星核深处** — 击败中期 Boss 后，地面塌陷，开启通往星球核心区的路径（FloorLevel -2）

---

## 七、世界时钟与动态关卡设计（方案 C：事件阶段 + 轻量循环周期）

### 决策背景

我们希望关卡会随着游戏内时间变化并发生结构上的改变——某些 AI 在特定时间开始交易、某个大门在特定时间点打开、某些事件在特定时间窗口内触发。这让世界感觉是"活的"而不是一成不变的布景。

### 方案评估

| 方案 | 思路 | 适合 | 优点 | 缺点 |
|------|------|------|------|------|
| **A: 纯循环日夜** | 每 X 分钟一个周期，NPC 有日程表 | 生存/种田/日夜循环类（星露谷、泰拉瑞亚） | 世界有呼吸感，玩家学习规律 | 银河恶魔城+类魂中少见，可能打断探索节奏 |
| **B: 纯事件驱动** | 玩家成就/行为驱动世界变化（击败Boss→新区域） | 银河恶魔城/类魂（空洞骑士、黑暗之魂） | 完美契合探索叙事 | 没有"等到时间到门就开"的感觉 |
| **✅ C: 混合** | 大框架事件驱动 + 小周期时间循环 | 我们的项目 | 兼得两者：进度感+活世界感 | 需要两套系统，但复杂度可控 |

### 混合时间模型详解

```
┌─────────────────────────────────────────────────────────────────┐
│  大框架：事件驱动阶段（单向进度，不可逆）                         │
│                                                                   │
│  阶段 1: Boss 存活期                                              │
│    → 击败 Boss A → 进入 阶段 2                                    │
│  阶段 2: Boss A 已击杀                                            │
│    → 某些门永久打开、某些 NPC 出现、某些区域开放                    │
│    → 获得关键道具 → 进入 阶段 3                                    │
│  阶段 3: 核心区域开放                                              │
│    → 精英怪开始出现、难度提升、新商人入驻                           │
│  ...                                                              │
└─────────────────────────────────────────────────────────────────┘
        │
        │  每个大阶段内部运行着：
        ▼
┌─────────────────────────────────────────────────────────────────┐
│  小周期：轻量时间循环（周期性，可重复）                            │
│                                                                   │
│  星球自转周期 = 15~20 分钟现实时间                                 │
│  ├── "辐射潮" (00:00~05:00)                                      │
│  │    → 某些区域有辐射伤害，NPC 躲起来，特殊敌人出没               │
│  ├── "平静期" (05:00~12:00)                                      │
│  │    → NPC 出来交易，某些门打开，最佳探索窗口                     │
│  ├── "风暴期" (12:00~18:00)                                      │
│  │    → 敌人增强，特殊掉落，环境视觉/音频变化                      │
│  └── "寂静时" (18:00~24:00)                                      │
│       → 隐藏通道出现，稀有 NPC 交易，低能见度                      │
└─────────────────────────────────────────────────────────────────┘
```

### 系统架构

```
┌─────────────────────────────────────────────┐
│              WorldClock (全局单例)             │
│  当前时间 · 时间流速 · 暂停/恢复              │
│  事件: OnTimeChanged, OnCycleCompleted        │
└────────────────────┬────────────────────────┘
                     │
┌────────────────────▼────────────────────────┐
│           WorldPhaseManager                   │
│  定义阶段列表（辐射潮/平静期/风暴期/寂静时）   │
│  监听 WorldClock → 判断当前属于哪个阶段        │
│  阶段切换时广播 OnPhaseChanged 事件            │
└────────────────────┬────────────────────────┘
                     │
┌────────────────────▼────────────────────────┐
│          WorldProgressManager                 │
│  监听进度事件 (Boss击杀/道具获取/累计击杀)     │
│  管理大阶段 (不可逆，单向进度)                 │
│  大阶段切换时广播 OnWorldStageChanged 事件     │
└────────────────────┬────────────────────────┘
                     │
        ┌────────────┴────────────────┐
        │                             │
┌───────▼─────────────────┐  ┌───────▼──────────────────┐
│ ScheduledBehaviour (组件) │  │ WorldEventTrigger (组件)  │
│ 挂在任何 GameObject 上    │  │ 监听大阶段切换            │
│ 配置: 在Phase X 时启用/禁用│  │ 触发永久性世界变化        │
│                           │  │                           │
│ 用途:                     │  │ 用途:                     │
│ - NPC 交易时间窗口        │  │ - Boss击杀后新区域开放    │
│ - 大门定时开关            │  │ - 获得关键道具后地形变化  │
│ - 敌人夜间增强            │  │ - 里程碑触发NPC迁移       │
│ - 隐藏通道显现            │  │ - 精英怪开始出现          │
│ - 商人出没                │  │ - Tilemap 变体切换        │
└───────────────────────────┘  └────────────────────────────┘
```

### "结构上的改变"实现层级

关卡的动态变化按复杂度分为四个层级：

#### 层级 1：门/通道的开关（简单）
```
某扇门在"平静期"打开，在"风暴期"关闭
→ Door 组件监听 WorldPhaseManager.OnPhaseChanged
→ 切换 DoorState.Open / DoorState.Locked_Schedule
```

#### 层级 2：区域内容的变化（中等）
```
白天：房间里有 3 只商人NPC + 2 只护卫
夜晚：商人消失，替换为 5 只掠食者敌人
→ Room 持有多套 SpawnConfig（日间配置/夜间配置）
→ 阶段切换时替换激活的配置
```

#### 层级 3：地形/拓扑的变化（复杂但可做）
```
击败 Boss 后，地面塌陷，出现通往新区域的裂隙
→ 预先在场景中放好两套 Tilemap（塌陷前/塌陷后），初始时"塌陷后"版本禁用
→ 触发事件时：播放演出 → 禁用旧 Tilemap → 启用新 Tilemap → 更新碰撞体
```

#### 层级 4：全局氛围变化（视觉/音频）
```
风暴期：屏幕加后处理滤镜（色调偏暗/加噪点）、环境粒子（飞沙走石）、BGM 切换
→ WorldPhaseManager.OnPhaseChanged 触发 →
  PrimeTween: 后处理 Volume 参数渐变 (Tween.Custom) →
  粒子系统启停 →
  AudioManager.PlayMusic(stormBGM, fadeDuration: 2f) →
  AudioManager.ApplyLowPassFilter(800f, fadeDuration: 1f)  // 可选：风暴期闷声效果
```

### 对架构的影响

| 组件 | 是否需要改 | 改什么 |
|------|----------|--------|
| `Room` | 🔲 扩展 | 支持多套 SpawnConfig（按阶段切换）|
| `RoomSO` | 🔲 扩展 | 添加 `RoomVariant[]`（不同阶段/事件后的房间变体）|
| `Door` | 🔲 扩展 | 新增 `Locked_Schedule` 状态，支持时间驱动开关 |
| **新增 WorldClock** | ✅ 新系统 | 游戏内时钟，时间流逝、暂停 |
| **新增 WorldPhaseManager** | ✅ 新系统 | 周期阶段定义、切换、广播 |
| **新增 WorldProgressManager** | ✅ 新系统 | 大阶段管理、进度事件监听 |
| **新增 ScheduledBehaviour** | ✅ 新组件 | 通用的"在某阶段时做X"组件 |
| **新增 WorldEventTrigger** | ✅ 新组件 | 进度事件驱动的永久变化 |
| `RoomManager` | ⬜ 不改 | 它不关心时间，只追踪当前房间 |
| `EnemyDirector` | ⬜ 不改 | 敌人配置由 Room 提供，Director 只管调度 |

---

## 八、与现有系统的集成点

### 已有基础设施（直接复用）

| 现有系统 | 集成方式 | 获取方式 |
|---------|----------|----------|
| `ServiceLocator` | Level 管理器注册自己；消费其他服务 | 静态调用 `ServiceLocator.Get<T>()` |
| `PoolManager` | Encounter 波次从池中获取敌人实例 | `ServiceLocator.Get<PoolManager>()` |
| `EnemyDirector` | 继续工作，令牌池作用域可改为"当前房间"；Room 切换时清空令牌 | `ServiceLocator.Get<EnemyDirector>()` |
| `EnemyEntity` | 监听 `OnDeath` 追踪波次存活数；`OnAnyEnemyDeath` 追踪全局击杀 | 实例事件 + 静态事件 |
| `ShipHealth.OnDeath` | `GameFlowManager` 订阅此事件触发重生流程 | `ServiceLocator.Get<ShipHealth>()` |
| `HeatSystem` | 检查点可选恢复热量 (`ResetHeat()`) | `ServiceLocator.Get<HeatSystem>()` |
| `AudioManager` | 房间过渡音效、氛围 BGM 切换、低通滤波 | `ServiceLocator.Get<AudioManager>()` |
| `SaveManager` | 存读关卡进度（已有 `PlayerSaveData.Progress` 含 VisitedRoomIDs / DefeatedBossIDs / Flags） | 静态调用 `SaveManager.Save/Load()` |
| `DamagePayload` | Hazard 系统构造 `DamagePayload(DamageType.Fire/Ice/...)` 对 `IDamageable` 造伤 | 直接构造 struct |
| `CombatEvents` | 保持现有战斗事件不变（`OnWeaponFired` 等） | 静态事件总线 |
| `LevelEvents`（**新增于 Core 层**） | 与 `CombatEvents` 平行的关卡事件总线：`OnRoomEntered(string)`、`OnRoomCleared(string)`、`OnBossDefeated(string)`、`OnCheckpointActivated(string)`、`OnWorldStageChanged(int)` — Level 发布，UI/Save/任何层消费 | 静态事件总线 |
| `StarChartController` | 无需改动，拾取星图部件通过 `ItemPickup` → `StarChartInventorySO` | 不直接引用 |

### 需要改造的现有系统

| 系统 | 改造内容 |
|------|----------|
| `EnemySpawner` | 重构为策略模式：抽取 `ISpawnStrategy` 接口，原有循环刷怪逻辑封装为 `LoopSpawnStrategy`，新增 `WaveSpawnStrategy`（EncounterSO 驱动、多波次）。Spawner 本体保留池管理/精英词缀/生成点等通用逻辑 |
| `SaveManager / PlayerSaveData` | `ProgressSaveData` 已预留 `VisitedRoomIDs`/`DefeatedBossIDs`/`Flags`，但需扩展：新增 `LastCheckpointID`（已有于 `PlayerStateSaveData`）、`WorldClockTime`、`WorldStage` 字段 |
| `Core` 程序集 | 新增 `LevelEvents.cs` 静态事件总线（与 `CombatEvents.cs` 平行），定义关卡事件。`CombatEvents` 保持不变，职责不混淆 |

### 新增系统

| 系统 | 说明 |
|------|------|
| `RoomManager` | 追踪当前房间，广播事件，ServiceLocator 注册 |
| `GameFlowManager` | 死亡/重生编排，订阅 `ShipHealth.OnDeath`，UniTask 异步演出 |
| `CheckpointManager` | 管理活跃检查点，ServiceLocator 注册 |
| `WorldClock` | 游戏内时钟，ServiceLocator 注册，被 SaveManager 序列化 |
| `WorldPhaseManager` | 周期阶段管理，监听 WorldClock，广播 `OnPhaseChanged` |
| `WorldProgressManager` | 大阶段管理，监听里程碑事件（CombatEvents），广播 `OnWorldStageChanged` |

---

## 九、玩家体验描述

> 以下是按照当前方案，玩家在关卡中的完整体验流程描述。

### 镜头行为
- 镜头始终跟随飞船移动（Cinemachine Follow Camera）
- 当飞船进入一个房间后，镜头被 **Cinemachine Confiner** 约束在该房间的矩形边界内
- 如果房间较小，镜头会固定在房间中心（飞船在房间内移动但镜头不动）
- 如果房间较大，镜头跟随飞船但不超出房间边界（到达房间边缘时镜头停住，飞船继续往前）
- 穿过门/通道进入下一个房间时，镜头平滑过渡到新房间的 Confiner 边界

### 关卡规模与结构
- 每个星球 = 一个 Unity Scene
- 每个星球由 **20~40 个房间** 组成，通过门/通道相连
- 房间大小不一：小型战斗间（1~2 屏）、中型走廊（3~4 屏）、大型 Boss 竞技场（4~6 屏）
- 整个星球地图是一个**互联的网络**，银河恶魔城风格，不是线性流程
- 有捷径（Shortcut）连接远距离房间，解锁后可快速通行

### 房间边界与过渡
- 玩家飞船**不会**碰到关卡的"世界边缘"——因为每个房间四周都是墙壁
- 房间之间通过 **门/通道** 连接
- 飞到门的位置 → 短暂淡黑过渡 → 出现在下一个房间的入口点
- 墙壁是实心碰撞体，飞船撞到就停，不会穿墙

### 遭遇小兵
- 进入一个普通房间：房间内有预置的敌人群
- 敌人在玩家进入房间时被激活，开始正常 AI 行为（巡逻/感知/追击/攻击）
- **可以脱离仇恨**：每个敌人有 `LeashRange`（脱战距离），飞出这个距离后敌人会放弃追击，返回巡逻点
- 但在同一个房间内通常跑不远（房间边界限制），所以房间内的敌人基本会持续追击
- 穿过门进入下一个房间后，上一个房间的敌人**不会跟过来**（不同房间的敌人互相隔离）
- 离开的房间中未击杀的敌人会继续存在（除非房间类型是"竞技场"且已清理过）

### 遭遇 Boss
- Boss 位于专用的 **Boss 竞技场房间**
- 进入 Boss 房间 → 触发剧情/对话 → 大门关闭（Locked_Combat）→ Boss 战开始
- Boss 使用多阶段系统（BossController + BossPhaseDataSO）：
  - 第一阶段正常攻击模式
  - HP 降到阈值 → 无敌过渡演出 → 切换攻击模式/增加招式
- **不能逃跑**：Boss 房间门在战斗期间锁死
- 击败 Boss → 掉落关键钥匙/星图部件 → Boss 房间门解锁 → 开启通往新区域的路径

### 战斗竞技场房间
- 某些房间标记为"Arena"（竞技场）
- 进入竞技场 → 门锁死 → 警报音效 → 敌人分波次出现
- Wave 1 全部清除 → Wave 2 出现 → ... → 最后一波全清 → 门解锁 + 掉落奖励
- 竞技场一旦通关，再次进入时不会重新触发（已清理状态）

### 检查点
- 散布在关卡各处的固定位置
- 交互激活后：恢复 HP、恢复热量、设置重生点
- 死亡后从最后激活的检查点重生
- 重生时：检查点所在房间的敌人全部重置，但已通关的竞技场/Boss 房间不重置

### 锁钥系统
- 某些门需要钥匙（彩色卡/Boss 掉落物/特定能力）才能打开
- 需要回溯（Backtracking）——典型银河恶魔城结构
- 例：需要"红色晶钥"才能打开红门 → 红色晶钥在另一个区域的 Boss 掉落

### 多层探索体验
- 某些房间的地面有**裂隙入口**，飞船靠近后提示交互
- 交互后 → 更长的淡黑过渡 + 下坠粒子特效 + 环境音效切换 → 出现在地底层房间
- 地底层是一个**完全独立的房间网络**，有自己的主题（如幽暗结晶洞穴）、敌人类型和探索路线
- 地底层也有上升通道可以回到地表——有些是电梯/传送门，有些是快捷通道
- 在极少数叙事关键时刻（如首次发现裂隙），会触发**无缝掉落演出**——飞船真的向下坠落，镜头跟随，创造"哇"的瞬间
- 小地图支持楼层切换，可以查看每一层的已探索区域

### 动态世界体验
- 星球有一个**自转周期**（约 15~20 分钟现实时间一个完整循环）
- 不同时段环境会发生明显变化：
  - **辐射潮**：屏幕泛红、环境粒子密集、某些走廊出现辐射伤害区域、NPC 躲起来不交易
  - **平静期**：最佳探索/交易窗口、NPC 出来摆摊、某些定时门打开
  - **风暴期**：敌人增强、特殊掉落率提升、视觉噪点加重、BGM 变得紧张
  - **寂静时**：能见度降低但隐藏通道出现、稀有 NPC 只在此时段出没
- 玩家可以学习这些规律来优化自己的探索策略（"辐射潮快结束了，等一下再去交易"）
- **击败 Boss / 获得关键道具后**，世界会发生**永久性变化**：
  - 新区域开放（之前锁死的门永久解锁）
  - 新 NPC 入驻（商人/情报贩子出现在特定房间）
  - 地形改变（某处塌陷，出现新的通往地底层的入口）
  - 敌人生态变化（新种类出现、旧种类消失或增强）

---

## 十、从哪里开始

**建议从 Phase 1（L1-L5）开始**：

1. **Room + RoomManager 是一切的基石** — 没有房间概念，敌人配置、门、检查点都无从谈起
2. **立即获得可感知的游戏体验升级** — 从"一块空旷场地里有几只怪"变成"有房间、有门、有探索感"
3. **Phase 1 不需要美术资产** — 纯白色 Tilemap + 基础碰撞体就能验证
4. **与现有代码冲突极小** — 几乎全是新增模块，只有 `EnemySpawner` 需要小幅改造
