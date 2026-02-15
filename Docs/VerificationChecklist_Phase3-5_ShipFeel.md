# 验收清单：Level Phase 3-6 + 飞船手感增强系统

**版本：** v2.0
**涵盖范围：**
- Level Phase 3 (L10-L12)：EncounterSystem、ArenaController、Hazard System
- Level Phase 4 (L13-L15)：Minimap/Map System、Save Integration、Sheba Scaffolder
- Level Phase 5 (L16-L19)：Multi-Floor、Enhanced Layer Transition、Narrative Fall Placeholder
- **Level Phase 6 (L20-L27)：WorldClock、WorldPhaseManager、ScheduledBehaviour、WorldEventTrigger、TilemapVariantSwitcher、AmbienceController、Room Variant**
- Ship Feel Enhancement：曲线移动、Dash、受击反馈、视觉 Juice

> **使用说明**：本文档为分步操作指南。每个步骤都标注了"在哪里操作"（Unity 菜单栏 / Project 窗口 / Hierarchy / 场景中），请按照指示在对应的 Unity 面板中操作。

---

## 总清单（Quick Checklist）

在每项前打勾表示通过。先做**编译 & 资产检查**（无需进入 Play Mode），再做**功能验证**（Play Mode）。

### A. 编译 & 资产检查（不进入 Play Mode）

- [ ] **A1** Unity Console 无红色编译错误
- [ ] **A2** `ShipActions.inputactions` 中 Ship map 可见 `ToggleMap` action（M 键 / Gamepad Select）
- [ ] **A3** `ProjectArk.Level.asmdef` 引用列表含 `Unity.TextMeshPro` 和 `Unity.InputSystem`
- [ ] **A4** `Assets/Scripts/Level/Editor/ProjectArk.Level.Editor.asmdef` 存在且 `includePlatforms = ["Editor"]`
- [ ] **A5** 菜单 `ProjectArk > Scaffold Sheba Level` 可见
- [ ] **A6** 菜单 `ProjectArk > Ship > Create Ship Feel Assets (All)` 可见
- [ ] **A7** `Assets/_Data/Ship/DefaultShipStats.asset` 存在，含 Dash / HitFeedback / Curves 字段
- [ ] **A8** `Assets/_Data/Ship/DefaultShipJuiceSettings.asset` 存在（若不存在，执行 A6 创建）

### B. Level Phase 3 — 战斗房间逻辑

- [ ] **B1** EncounterSO 资产可通过 `Create > ProjectArk > Level > Encounter` 创建
- [ ] **B2** Arena/Boss 房间 RoomSO 引用了 EncounterSO 且 WaveCount > 0
- [ ] **B3** Arena/Boss 房间 GameObject 上有 `ArenaController` 组件
- [ ] **B4** Room 子物体中有 `EnemySpawner`，SpawnPoints 至少 1 个
- [ ] **B5** 进入 Arena 房 → 门锁定 → 延迟后刷怪 → 全清后解锁
- [ ] **B6** 已清除的 Arena 再次进入不再触发遭遇
- [ ] **B7** DamageZone 按 `_tickInterval` 持续扣血
- [ ] **B8** ContactHazard 首次接触伤害，冷却期内不再伤害
- [ ] **B9** TimedHazard 周期开关 Collider，激活时伤害

### C. Level Phase 4 — 地图 & 存档集成

- [ ] **C1** MinimapManager 场景中存在并注册到 ServiceLocator
- [ ] **C2** MapPanel Prefab 创建完毕，含 ScrollRect + Content + 楼层 Tab 容器
- [ ] **C3** MapRoomWidget Prefab 创建完毕（含 Background/FogOverlay/CurrentHighlight Image）
- [ ] **C4** MapConnectionLine Prefab 创建完毕（含 Image）
- [ ] **C5** MinimapHUD 角落叠加层配置完毕
- [ ] **C6** M 键可打开/关闭全屏地图
- [ ] **C7** 进入新房间后地图标记为已访问
- [ ] **C8** SaveBridge 场景中存在，存档后 VisitedRoomIDs 正确持久化
- [ ] **C9** 死亡重生后已访问房间不丢失

### D. Level Phase 5 — 多楼层 & 增强层间转场

- [ ] **D1** 楼层不同的房间在地图上分 Tab 显示
- [ ] **D2** 通过 IsLayerTransition 的门时有粒子/SFX/相机缩放
- [ ] **D3** 目标房间有 _ambientMusic 时 BGM 交叉淡入
- [ ] **D4** NarrativeFallTrigger 场景中可放置，触发后传送到目标房间

### E. Level Phase 5 — Sheba 关卡脚手架

- [ ] **E1** 执行 `ProjectArk > Scaffold Sheba Level` 成功
- [ ] **E2** 场景中生成 12 个 Room 含 BoxCollider2D + PolygonCollider2D + Room 组件
- [ ] **E3** `Assets/_Data/Level/Rooms/Sheba/` 下生成 12 个 RoomSO
- [ ] **E4** `Assets/_Data/Level/Encounters/Sheba/` 下生成 4-5 个 EncounterSO
- [ ] **E5** 每个 Room 之间有双向 Door 连接
- [ ] **E6** Console 输出 10 步配置清单

### G. Level Phase 6 — 世界时钟 & 动态关卡

- [ ] **G1** 菜单 `ProjectArk > Level > Phase 6: Setup All (Assets + Scene)` 可见
- [ ] **G2** `Assets/_Data/Level/WorldPhases/` 下有 4 个 WorldPhaseSO 资产
- [ ] **G3** 场景中有 WorldClock、WorldPhaseManager、AmbienceController 组件
- [ ] **G4** WorldPhaseManager 的 `_phases` 数组有 4 个元素且引用正确
- [ ] **G5** Play Mode 中 Console 输出阶段切换日志
- [ ] **G6** WorldClock 时间持续递增，周期完成后重置
- [ ] **G7** ScheduledBehaviour 组件按阶段启用/禁用目标 GO
- [ ] **G8** Door(Locked_Schedule) 在配置的阶段自动开/关
- [ ] **G9** TilemapVariantSwitcher 可切换子 Tilemap 变体
- [ ] **G10** WorldEventTrigger 在达到指定 WorldStage 时触发效果
- [ ] **G11** 存档后 WorldClockTime / CurrentPhaseIndex 正确持久化
- [ ] **G12** AmbienceController 阶段切换时驱动后处理渐变（如已配置 Volume）
- [ ] **G13** Room 变体按阶段切换环境子物体和遭遇配置

### F. 飞船手感增强系统

- [ ] **F1** ShipDash 组件已挂到飞船 Prefab
- [ ] **F2** ShipVisualJuice 组件已配置 `_visualChild`（视觉子物体 Transform）
- [ ] **F3** ShipEngineVFX 组件已配置 `_engineParticles`（引擎粒子 ParticleSystem）
- [ ] **F4** DashAfterImageSpawner 已配置 `_afterImagePrefab` 和 `_shipSpriteRenderer`
- [ ] **F5** CinemachineImpulseSource 已挂到相机并注册到 HitFeedbackService
- [ ] **F6** 起步有加速推力感，松手有惯性滑行
- [ ] **F7** 急转弯（>90°）明显减速
- [ ] **F8** Space 键 Dash 冲刺，冷却约 0.3s
- [ ] **F9** Dash 期间无敌，结束后有惯性延续
- [ ] **F10** 受伤时 HitStop（短暂顿帧）+ 屏幕震动 + 无敌帧闪烁
- [ ] **F11** 横移时飞船视觉倾斜
- [ ] **F12** 引擎粒子随速度变化
- [ ] **F13** Dash 时出现半透明残影

---

## 详细配置与验证步骤

---

### 第一步：编译检查

1. 打开 **Unity Editor**，等待编译完成
2. 在 **Console 窗口**中查看，确认 **零红色错误**
3. 如有 `CS0246` / `CS0117` / `CS4014` 等错误，记录并报告

---

### 第二步：运行 Ship Feel Assets 创建工具

> 此步骤确保飞船手感相关的 SO 资产存在

1. 在 **Unity 菜单栏**中，点击 `ProjectArk` → `Ship` → `Create Ship Feel Assets (All)`
2. 观察 **Console 窗口**，确认输出创建成功日志
3. 在 **Project 窗口**中确认以下资产存在：
   - `Assets/_Data/Ship/DefaultShipStats.asset` — 选中该文件展开 Inspector，确认包含：
     - **Movement — Curves & Feel** 区：`_accelerationCurve`、`_decelerationCurve`、`_sharpTurnAngleThreshold`(90)、`_sharpTurnSpeedPenalty`(0.7)、`_initialBoostMultiplier`(1.5)
     - **Dash** 区：`_dashSpeed`(30)、`_dashDuration`(0.15)、`_dashCooldown`(0.3)、`_dashBufferWindow`(0.15)、`_dashExitSpeedRatio`(0.5)、`_dashIFrames`(true)
     - **Hit Feedback** 区：`_hitStopDuration`(0.05)、`_iFrameDuration`(1)、`_iFrameBlinkInterval`(0.1)、`_screenShakeBaseIntensity`(0.3)
   - `Assets/_Data/Ship/DefaultShipJuiceSettings.asset` — 展开 Inspector，确认包含：
     - `_moveTiltMaxAngle`(15)、`_squashStretchIntensity`(0.15)
     - `_dashAfterImageCount`(3)、`_afterImageFadeDuration`(0.15)、`_afterImageAlpha`(0.4)
     - `_engineBaseEmissionRate`(20)、`_engineDashEmissionMultiplier`(3)

---

### 第三步：配置飞船 Prefab

> **前置条件**：飞船 Prefab 已有 `InputHandler`、`ShipMotor`、`ShipAiming`、`ShipHealth`
>
> **本步骤操作位置**：在 **Project 窗口**（编辑 Prefab）+ **Hierarchy**（放置到场景后）中

完成本步骤后，飞船 Prefab 的 Hierarchy 和组件分布如下（供参考）：

```
Ship (根 GameObject)                         ← 物理 + 逻辑层
│  ● Rigidbody2D
│  ● BoxCollider2D
│  ● InputHandler          (已有)
│  ● ShipMotor             (已有)
│  ● ShipAiming            (已有)
│  ● ShipHealth            (已有)
│  ● ShipDash              ← 【新增】
│  ● ShipVisualJuice       ← 【新增】
│  ● ShipEngineVFX         ← 【新增】
│  ● DashAfterImageSpawner ← 【新增】
│
├── ShipVisual (子 GameObject)               ← 纯视觉层（Juice 操作此节点）
│      ● SpriteRenderer
│
└── EngineExhaust (子 GameObject)            ← 引擎尾焰粒子
       ● ParticleSystem
```

> **为什么新增的 4 个组件全部放在根 GameObject 上？**
> 因为它们都用了 `[RequireComponent(typeof(ShipMotor))]` 或 `[RequireComponent(typeof(ShipDash))]`，
> Unity 要求它们必须与依赖组件在同一个 GameObject 上。

#### 3-1. 创建 ShipVisual 视觉子物体

> 如果飞船的 SpriteRenderer 已经在根 GameObject 上，需要分离到子物体，
> 这样 `ShipVisualJuice` 才能对视觉做倾斜/缩放而不影响物理碰撞体。

1. 在 **Ship (根)** 下右键 → Create Empty，命名为 `ShipVisual`
2. 把根上的 `SpriteRenderer` 移到 `ShipVisual` 上（拖拽组件标题右键 → Move to `ShipVisual`，或重建）
3. 检查其他脚本（如 `ShipAiming`）若直接引用了根的 SpriteRenderer，更新引用指向 `ShipVisual`

#### 3-2. 创建 EngineExhaust 引擎尾焰粒子

1. 在 **Ship (根)** 下右键 → Create Empty，命名为 `EngineExhaust`
2. 选中 `EngineExhaust` → Add Component → `Particle System`
3. 推荐初始设置（后续可调）：

   | 参数 | 推荐值 |
   |------|--------|
   | Shape | Cone |
   | Start Size | 0.1 ~ 0.3 |
   | Start Lifetime | 0.3 |
   | Emission → Rate over Time | 20 |
   | Renderer → Sorting Layer | 与飞船一致或更低 |

#### 3-3. 创建 Dash 残影 Prefab（独立资产，不在飞船 Hierarchy 内）

1. 在 Project 窗口新建 Prefab：`Assets/_Prefabs/Ship/DashAfterImage.prefab`
2. 打开 Prefab，在根 GameObject 上添加：
   - `SpriteRenderer`（**Sprite 留空**，运行时由代码从飞船拷贝）
   - `DashAfterImage` 脚本
3. 保存 Prefab 后关闭

#### 3-4. 在飞船根 GameObject 上添加 4 个新组件

回到飞船 Prefab，选中 **Ship (根 GameObject)**，依次 Add Component：

**组件 ①：`ShipDash`**

| Inspector 字段 | 赋值 |
|---------------|------|
| `_stats` | `Assets/_Data/Ship/DefaultShipStats.asset` |

> 注：`ShipMotor`、`InputHandler`、`ShipHealth` 由代码自动 GetComponent，无需手动赋值。

**组件 ②：`ShipVisualJuice`**

| Inspector 字段 | 赋值 |
|---------------|------|
| `_visualChild` | 拖入步骤 3-1 创建的 **ShipVisual** 子物体（Transform） |
| `_juiceSettings` | `Assets/_Data/Ship/DefaultShipJuiceSettings.asset` |

**组件 ③：`ShipEngineVFX`**

| Inspector 字段 | 赋值 |
|---------------|------|
| `_engineParticles` | 拖入步骤 3-2 创建的 **EngineExhaust** 子物体上的 ParticleSystem |
| `_juiceSettings` | `Assets/_Data/Ship/DefaultShipJuiceSettings.asset` |

**组件 ④：`DashAfterImageSpawner`**

| Inspector 字段 | 赋值 |
|---------------|------|
| `_afterImagePrefab` | 步骤 3-3 创建的 `DashAfterImage.prefab`（从 Project 窗口拖入） |
| `_shipSpriteRenderer` | **ShipVisual** 子物体上的 `SpriteRenderer`（从 Hierarchy 拖入） |
| `_juiceSettings` | `Assets/_Data/Ship/DefaultShipJuiceSettings.asset` |
| `_stats` | `Assets/_Data/Ship/DefaultShipStats.asset` |

#### 3-5. 配置屏幕震动（ScreenShake）

> 这一步在**场景中的相机 GameObject** 上操作，不在飞船 Prefab 内。

**在哪里操作**：在 **Hierarchy** 中找到场景的相机

1. 在 **Hierarchy** 中找到你的主相机（通常是 `Main Camera` 或 `CinemachineCamera` 相关的 GameObject）
2. 选中该相机 GameObject → Add Component → `Cinemachine Impulse Source`（添加后会自动出现 Impulse 配置面板）
3. 选中该相机 GameObject → Add Component → `ImpulseSourceRegistrar`（位于 `Assets/Scripts/Ship/Combat/ImpulseSourceRegistrar.cs`，无需配置任何字段，启动时自动注册）

---

### 第四步：配置关卡场景（Phase 3 验证准备）

> **前置条件**：你需要一个**已有房间和门的测试场景**。
> - 如果你已经跑通过 Phase 1/2（有 Room + Door 的场景），直接在现有场景上操作
> - 如果是全新开始，建议先执行**第五步（Sheba Scaffolder）** 自动生成 12 个房间，再回到这里
>
> **本步骤目标**：在已有的房间基础上，增加**战斗遭遇**（敌人波次）和**环境危害**功能

---

#### 4A. 创建 EncounterSO 资产（敌人波次配置）

> **EncounterSO 是什么？** 它定义一个房间内的战斗遭遇——有几波敌人、每波什么敌人、每波几个。
> Arena 房间通过引用 EncounterSO 来知道该刷什么怪。

**在哪里操作**：在 **Project 窗口**中

1. 在 **Project 窗口**中导航到 `Assets/_Data/Level/Encounters/`（如果目录不存在，右键创建文件夹）
2. 在该目录下右键 → `Create` → `ProjectArk` → `Level` → `Encounter`
3. 命名为如 `TestArenaEncounter`
4. 选中该 `.asset` 文件，在 **Inspector** 中配置波次：

   **Waves 数组**（点击 `+` 添加 2 个元素）：

   | 波次 | `DelayBeforeWave` | `Entries` |
   |------|-------------------|-----------|
   | Wave 0 | 0（立即开始） | 点 `+` 添加 1 个 Entry：`EnemyPrefab` = 你的敌人 Prefab，`Count` = 2 |
   | Wave 1 | 1.5（上一波清完后等 1.5 秒） | 点 `+` 添加 1 个 Entry：`EnemyPrefab` = 你的敌人 Prefab，`Count` = 3 |

   > **关键**：`EnemyPrefab` 必须是有效的敌人 Prefab（带 `EnemyEntity` + `EnemyBrain` 子类组件）。
   > 如果你还没有敌人 Prefab，参考 `Assets/_Prefabs/Enemies/` 目录。

---

#### 4B. 创建 RoomSO 资产（房间数据配置）

> **RoomSO 是什么？** 它是房间的"身份证"——定义房间 ID、名称、类型（Normal/Arena/Boss/Safe）、
> 关联的遭遇数据等。每个 Room GameObject 都必须引用一个 RoomSO。
>
> 如果你用了 Sheba Scaffolder（第五步），RoomSO 已经自动创建好了，可以**跳到下面的"修改已有 RoomSO"部分**。

**从零创建 RoomSO**（在 **Project 窗口**中操作）：

1. 导航到 `Assets/_Data/Level/Rooms/`
2. 右键 → `Create` → `ProjectArk` → `Level` → `Room`
3. 命名为如 `TestArenaRoom`
4. 选中该 `.asset`，在 **Inspector** 中配置：

   | RoomSO 字段 | 值 | 说明 |
   |-------------|------|------|
   | `_roomID` | `test_arena` | 唯一标识符，不能与其他房间重复 |
   | `_displayName` | `测试竞技场` | 地图上显示的名字 |
   | `_floorLevel` | 0 | 楼层编号（0 = 地面层） |
   | `_type` | **Arena** | 关键！必须选 Arena 或 Boss，否则不会触发战斗 |
   | `_encounter` | 拖入步骤 4A 的 `TestArenaEncounter.asset` | 关联遭遇数据 |
   | `_mapIcon` | （可选） | 地图上的图标 |
   | `_ambientMusic` | （可选） | 房间背景音乐 |

**修改已有 RoomSO**（如果 Scaffolder 已创建）：

1. 在 **Project 窗口**中找到 `Assets/_Data/Level/Rooms/Sheba/` 下的某个 `.asset`
2. 选中它，在 Inspector 中把 `_type` 改为 **Arena**
3. 把 `_encounter` 字段拖入步骤 4A 创建的 EncounterSO

---

#### 4C. 创建 / 配置 Arena 房间的 GameObject

> **Room GameObject 是什么？** 它是场景中代表一个房间的实际 GameObject，
> 包含碰撞体（定义房间范围）、`Room` 组件（引用 RoomSO）、门、刷怪点等。
>
> 如果你用了 Sheba Scaffolder，Room GameObject 已经自动创建好了，跳到**"在已有 Room 上添加 Arena 功能"**。

**从零创建 Room GameObject**（在 **Hierarchy** 中操作）：

1. 在 **Hierarchy** 中右键 → Create Empty，命名为 `Room_TestArena`
2. 选中它 → Add Component → `Room`（会自动添加 `BoxCollider2D`）
3. 调整 `BoxCollider2D` 大小覆盖房间区域，勾选 **Is Trigger**
4. 在 Inspector 中配置 `Room` 组件：

   | Room 字段 | 赋值 |
   |-----------|------|
   | `_data` | 拖入步骤 4B 创建的 `TestArenaRoom.asset`（RoomSO） |
   | `_confinerBounds` | 拖入自身的 BoxCollider2D（或另建一个 PolygonCollider2D 用于相机边界） |
   | `_spawnPoints` | 见下方"创建刷怪点" |
   | `_playerLayer` | 下拉菜单选 `Player` |

5. **创建刷怪点**：在 `Room_TestArena` 下右键 → Create Empty，命名为 `SpawnPoint_1`，摆放到房间内部合适位置。再创建 `SpawnPoint_2`。把它们拖入 Room 的 `_spawnPoints` 数组。

6. **创建 EnemySpawner**：在 `Room_TestArena` 下右键 → Create Empty，命名为 `EnemySpawner`，Add Component → `EnemySpawner`，配置：

   | EnemySpawner 字段 | 赋值 |
   |-------------------|------|
   | `_spawnPoints` | 拖入上面的 SpawnPoint_1、SpawnPoint_2 |
   | `_poolPrewarmCount` | 5 |
   | `_poolMaxSize` | 10 |
   | 其余字段 | 保持默认（Arena 模式下由 WaveSpawnStrategy 接管，不使用 loop 字段） |

**在已有 Room 上添加 Arena 功能**：

如果 Room GameObject 已存在（来自 Scaffolder 或之前的 Phase），只需：

1. 确认 Room 组件的 `_data` 引用的 RoomSO 的 `_type` = **Arena**，且 `_encounter` 已赋值
2. 确认子物体中有 `EnemySpawner`，`_spawnPoints` 至少有 1 个 Transform
3. 继续下一步添加 `ArenaController`

---

#### 4D. 添加 ArenaController 组件

> **ArenaController 是什么？** 它是 Arena/Boss 房间的战斗控制器——
> 玩家进入房间时自动锁门、刷怪、全清后解锁。
> 它必须和 `Room` 组件在**同一个 GameObject** 上（`[RequireComponent(typeof(Room))]`）。

**在哪里操作**：在 **Hierarchy** 中选中 Arena 房间的 GameObject

1. 选中 Arena 房间的 Room GameObject
2. Add Component → `ArenaController`
3. Inspector 配置：

   | ArenaController 字段 | 推荐值 | 说明 |
   |---------------------|--------|------|
   | `_preEncounterDelay` | 1.5 | 锁门后等多久开始刷怪（秒） |
   | `_postClearDelay` | 1.0 | 全清后等多久解锁门（秒） |
   | `_alarmSFX` | （可选，先留空） | 锁门时播放的警报音效 |
   | `_victorySFX` | （可选，先留空） | 全清时播放的胜利音效 |
   | `_rewardPrefab` | （可选） | 全清后在房间中心生成的奖励 Prefab |
   | `_rewardOffset` | (0, 0, 0) | 奖励生成位置的偏移 |

> **完成后的 Hierarchy 结构应该是**：
> ```
> Room_TestArena                ← Room + BoxCollider2D + ArenaController
> ├── SpawnPoint_1              ← 空 Transform，标记刷怪位置
> ├── SpawnPoint_2              ← 空 Transform
> ├── EnemySpawner              ← EnemySpawner 组件
> └── Door_ToXxx (如果有门)     ← Door 组件
> ```

#### 4E. Hazard 配置（测试用）

> **Hazard 是什么？** 环境危害——不是敌人，而是固定在场景中的伤害源（酸液地板、激光栅栏、间歇性陷阱等）。
> 它们通过 Collider2D Trigger 检测玩家碰撞，自动造成伤害。
>
> **Layer 说明**：Hazard 可以放在 `Default` 层或自定义的 `Hazard` 层。
> 关键是确保该 Layer 与 `Player` Layer 在碰撞矩阵中互相勾选（见下方 Physics2D 碰撞矩阵部分）。
>
> **创建位置**：在 **Hierarchy** 中的场景里创建，建议放在房间内部用于测试。

**Hazard ①：DamageZone**（持续伤害区域，如酸液地板）

1. 在 **Hierarchy** 中，右键 → Create Empty，命名为 `TestDamageZone`
2. 选中该 GameObject → Add Component → `BoxCollider2D`，勾选 **Is Trigger**，调整大小覆盖区域
3. Add Component → `DamageZone`
4. Inspector 配置：

   | 字段 | 值 |
   |------|----|
   | `_damage` | 5 |
   | `_tickInterval` | 0.5（每 0.5 秒伤害一次） |
   | `_targetLayer` | Player |

**Hazard ②：ContactHazard**（接触伤害，如激光栅栏）

1. 在 **Hierarchy** 中，右键 → Create Empty，命名为 `TestContactHazard`
2. 选中该 GameObject → Add Component → `BoxCollider2D`，勾选 **Is Trigger**
3. Add Component → `ContactHazard`
4. Inspector 配置：

   | 字段 | 值 |
   |------|----|
   | `_damage` | 10 |
   | `_hitCooldown` | 1.0（碰一次后 1 秒内不再伤害） |
   | `_targetLayer` | Player |

**Hazard ③：TimedHazard**（周期开关，如间歇性激光）

1. 在 **Hierarchy** 中，右键 → Create Empty，命名为 `TestTimedHazard`
2. 选中该 GameObject → Add Component → `BoxCollider2D`，勾选 **Is Trigger**
3. Add Component → `TimedHazard`
4. （可选）在该 GameObject 上添加 `SpriteRenderer`，将其赋给 `_visual` 字段，运行时会自动淡入淡出表示开关状态
5. Inspector 配置：

   | 字段 | 值 |
   |------|----|
   | `_damage` | 8 |
   | `_activeDuration` | 2.0（激活 2 秒） |
   | `_inactiveDuration` | 3.0（关闭 3 秒） |
   | `_targetLayer` | Player |
   | `_visual` | （可选）上面的 SpriteRenderer |

**Physics2D 碰撞矩阵**：
1. 在 Unity 菜单栏中，点击 `Edit` → `Project Settings` → `Physics 2D`
2. 在 **Layer Collision Matrix** 区域中
3. 找到 Hazard 所在的 Layer（如 "Hazard" 或你创建的自定义 Layer）与 "Player" Layer 的交叉单元格
4. 确认该交叉单元格**已勾选**（否则 Trigger 不会触发）

---

### 第五步：执行 Sheba Level Scaffolder

> **Sheba Scaffolder 是什么？** 一个编辑器工具，一键自动生成示巴星关卡的 12 个房间——
> 包括场景中的 Room GameObject、RoomSO 资产、EncounterSO 资产、和双向 Door 连接。
> 执行后你会得到一个**骨架关卡**，然后按照清单完成手动配置即可。

**在哪里操作**：在 **Unity 菜单栏** + **Project 窗口** + **Hierarchy** 中

#### 5-1. 执行 Scaffolder

1. 在 **Unity 菜单栏**中，点击 `ProjectArk` → `Scaffold Sheba Level`
2. 确认弹窗提示 → 点击 "Scaffold"
3. 等待完成（约 2-5 秒），然后检查结果：

   **在 Hierarchy 中**：
   - 出现 `--- Sheba Level ---` 父物体
   - 下面挂有 12 个 `Room_sheba_*`（如 `Room_sheba_entrance`、`Room_sheba_arena_1` 等）
   - 每个 Room 之间有自动创建的 Door 子物体

   **在 Project 窗口中**：
   - `Assets/_Data/Level/Rooms/Sheba/` 下有 12 个 `.asset`（RoomSO）
   - `Assets/_Data/Level/Encounters/Sheba/` 下有 4-5 个 `.asset`（EncounterSO，但**敌人 Prefab 为空**，需要手动填）

   **在 Console 中**：
   - 输出一份 10 步配置清单，列出所有需要手动完成的配置项

#### 5-2. Scaffolder 后续手动配置

> Scaffolder 只创建骨架，以下内容**必须手动完成**才能让关卡可玩。

**① 设置 Player Layer**（在 **Hierarchy** 中操作）：
- 逐个选中每个 `Room_sheba_*` GameObject，在 Inspector 中把 `Room` 组件的 `_playerLayer` 设为 `Player`
- 逐个选中每个 Door 子物体，把 `Door` 组件的 `_playerLayer` 也设为 `Player`
- 或者：使用搜索框搜 `Room_sheba`，全选后批量操作

**② 为 EncounterSO 分配敌人 Prefab**（在 **Project 窗口**中操作）：
- 打开 `Assets/_Data/Level/Encounters/Sheba/` 下的每个 `.asset`
- 在 Inspector 中找到 `Waves` → `Entries` → `EnemyPrefab` 字段
- 从 `Assets/_Prefabs/Enemies/` 中拖入有效的敌人 Prefab（必须带 `EnemyEntity` + `EnemyBrain`）

**③ 放置 Checkpoint（存档点）**（在 **Hierarchy** 中操作）：
- 找到类型为 Safe 的房间（如 `Room_sheba_safe_hub`）
- 在该 Room 下右键 → Create Empty，命名为 `Checkpoint_Hub`
- 选中 → Add Component → `Checkpoint`（会自动添加 `Collider2D`），Inspector 配置：

   | Checkpoint 字段 | 赋值 |
   |-----------------|------|
   | `_data` | 需要先创建 CheckpointSO：在 Project 窗口 → 右键 → `Create` → `ProjectArk` → `Level` → `Checkpoint`，填写 `_checkpointID` 和 `_displayName`，然后拖入此处 |
   | `_playerLayer` | 下拉菜单选 `Player` |
   | `_spriteRenderer` | （可选）如果有视觉表现，拖入子物体的 SpriteRenderer |

> 玩家进入 Checkpoint 触发区后按 **Interact 键** 激活存档点。激活后会恢复 HP/Heat（取决于 CheckpointSO 配置）。

**④ 放置玩家出生点**：
- 在 `Room_sheba_entrance` 下创建空 GameObject，命名为 `PlayerSpawn`
- 把飞船 Prefab 的初始位置设在这里（或在 `GameFlowManager` 中引用此 Transform）

**⑤ 放置钥匙拾取物**（在 **Hierarchy** 中操作）：
- 在 `Room_sheba_key_chamber` 下创建 GameObject，添加 `KeyPickup` 组件
- 配置 `KeyItemSO`（从 `Create > ProjectArk > Level > Key Item` 创建）

**⑥ 调整房间大小**：
- 选中各 Room，调整 `BoxCollider2D`（房间触发区域）和 `PolygonCollider2D`（相机边界）的大小
- 在 Scene View 中拖动编辑点来适配你的 Tilemap 美术

---

### 第六步：配置 Level Phase 4-5 UI 组件（一键生成）

> **概览**：使用 MapUIBuilder 一键生成所有 Map UI Prefab 和场景层级。
> 只需运行两个菜单命令，所有 Prefab、场景层级和字段连线都会自动完成。

#### 6A. 一键创建 Prefab（自动）

1. 菜单 → `ProjectArk > Map > Build Map UI Prefabs`
2. 确认 Console 日志显示 3 个 Prefab 创建成功
3. 在 Project 窗口确认 `Assets/_Prefabs/UI/Map/` 下出现：
   - `MapRoomWidget.prefab` — 房间节点 UI（含 Background/IconOverlay/CurrentHighlight/FogOverlay/Label 子物体，已自动连线）
   - `MapConnectionLine.prefab` — 连接线 UI（含 Image，已自动连线）
   - `FloorTabButton.prefab` — 楼层 Tab 按钮（含 Button + Label，80x30）

#### 6B. 一键创建场景层级（自动）

1. 菜单 → `ProjectArk > Map > Build Map UI Scene`
2. 确认 Console 日志显示 MapPanel 和 MinimapHUD 创建成功
3. 在 Hierarchy 中确认：

   **MapPanel**（全屏地图，默认隐藏）：
   ```
   Canvas (已有或自动创建)
   └── MapPanel (CanvasGroup + MapPanel 脚本, inactive)
       ├── Background (暗色半透明全屏)
       ├── ScrollView (ScrollRect, 支持拖拽平移)
       │   └── Viewport (Mask)
       │       └── MapContent (2000x2000, 房间/连接线容器)
       ├── FloorTabBar (HorizontalLayoutGroup, 楼层切换)
       └── PlayerIcon (黄色菱形, 玩家位置标记)
   ```
   
   **MinimapHUD**（角落小地图）：
   ```
   Canvas
   └── MinimapHUD (MinimapHUD 脚本, 右下角 200x200)
       ├── Border (边框效果)
       ├── Background (暗色半透明)
       ├── Content (带 Mask 的内容区域)
       └── FloorLabel (楼层文字 "F0")
   ```

4. 所有字段已自动连线：
   - `_inputActions` → ShipActions.inputactions（自动查找）
   - `_scrollRect` / `_mapContent` / `_floorTabContainer` / `_playerIcon` → 场景子物体
   - `_roomWidgetPrefab` / `_connectionLinePrefab` / `_floorTabPrefab` → 步骤 6A 的 Prefab
   - MinimapHUD 的 `_content` / `_background` / `_floorLabel` / Prefab 引用 → 全部自动连线

> 如果 InputActionAsset 未自动找到（Console 警告），请手动赋值 `Assets/Input/ShipActions.inputactions` 到 MapPanel 的 `_inputActions`。

#### 6C. 手动微调（可选）

- 调整 MinimapHUD 的锚点/大小（默认右下角 200x200，间距 10px）
- 为 MinimapHUD 创建更小版本的 MapRoomWidget Prefab（当前与全屏地图共用）
- 调整 MapPanel 的 `_worldToMapScale`（默认 5）以适配你的关卡尺寸

#### 6D. 确认场景管理器 GameObject

> 以下管理器组件需要挂在场景中的 GameObject 上。

**在哪里操作**：在 **Hierarchy** 中（可以放在同一个 `Managers` 空物体下，也可分开）

1. 在场景中创建一个空的 GameObject，命名为 `Managers`（如果已存在则跳过）
2. 选中 `Managers` GameObject，根据下表添加或确认组件：

| 组件 | 是否新增 | Inspector 配置 |
|------|---------|---------------|
| `MinimapManager` | **新增 (Phase 4)** | `_saveSlot` = 0 |
| `SaveBridge` | **新增 (Phase 4)** | `_saveSlot` = 0 |
| `RoomManager` | 已有 | 无需改动 |
| `GameFlowManager` | 已有 | 无需改动（代码已自动调 `SaveBridge.LoadAll()`） |
| `CheckpointManager` | 已有 | 无需改动（已改为通过 SaveBridge 存档） |
| `WorldProgressManager` | 已有 | 无需改动（已改为通过 SaveBridge 存档） |

> **已有** 表示前面的 Phase 已经创建过，只需确认还在。

### 第七步：Play Mode 功能验证

> 本步骤需要在 **Unity Play Mode** 中进行测试。

#### 7A. 飞船手感（优先级最高，可独立验证）

| # | 测试项 | 操作 | 预期 |
|---|--------|------|------|
| F6 | 加速曲线 | WASD 起步 | 有推力加速感，非瞬间满速 |
| F6 | 减速惯性 | 松开方向键 | 有明显滑行后减速 |
| F7 | 急转弯 | 高速移动中反向 | 速度明显降低 |
| F8 | Dash | 按 Space | 短距离冲刺，~0.3s 后可再次 Dash |
| F9 | Dash 无敌 | Dash 穿过伤害区 | 不受伤害 |
| F9 | Dash 动量 | Dash 结束后 | 有一定惯性延续 |
| F8 | 输入缓冲 | 冷却期内按 Space | 冷却结束后自动执行 Dash |
| F10 | HitStop | 被敌人攻击 | 短暂顿帧 |
| F10 | 屏幕震动 | 被攻击 | 相机抖动（需配置 CinemachineImpulseSource） |
| F10 | 无敌帧 | 被攻击后 ~1s 内再次接触敌人 | 不受伤害，精灵闪烁 |
| F11 | 移动倾斜 | 横向移动 | 视觉子物体有微小倾斜 |
| F12 | 引擎粒子 | 移动 / Dash | 尾焰粒子，Dash 时更强 |
| F13 | Dash 残影 | Dash | 身后出现 3 个半透明残影并淡出 |

#### 7B. Level Phase 3 — 战斗房间

| # | 测试项 | 操作 | 预期 |
|---|--------|------|------|
| B5 | Arena 遭遇流程 | 进入 Arena 房 | 门锁定 → (1.5s) → 敌人生成 → 全清 → (1s) → 门解锁 |
| B5 | 波次推进 | 杀完第 1 波 | Console: `Spawning wave 2/X`，延迟后第 2 波生成 |
| B6 | 已清除跳过 | 再次进入已清除 Arena | 不触发遭遇，门保持 Open |
| B7 | DamageZone | 站在酸液区 | 每 0.5s 扣 5 HP |
| B8 | ContactHazard | 接触激光栅栏 | 扣 10 HP，1s 内再碰不扣 |
| B9 | TimedHazard | 等待周期切换 | 激活 2s → 关闭 3s → 激活，激活时碰到扣血 |

#### 7C. Level Phase 4 — 地图系统

| # | 测试项 | 操作 | 预期 |
|---|--------|------|------|
| C6 | 打开全屏地图 | 按 M 键 | MapPanel 全屏显示 |
| C6 | 关闭全屏地图 | 再按 M 键 | MapPanel 关闭 |
| C7 | 房间访问标记 | 进入新房间 | 地图上该房间从 "迷雾" 变为可见 |
| — | MinimapHUD 刷新 | 进入新房间 | 角落小地图以当前房间为中心刷新 |
| — | 滚轮缩放 | 全屏地图中滚轮 | 地图内容缩放 |
| C8 | 存档持久化 | 激活存档点后退出再进入 | 已访问房间仍然显示 |

#### 7D. Level Phase 5 — 多楼层 & 层间转场

| # | 测试项 | 操作 | 预期 |
|---|--------|------|------|
| D1 | 楼层 Tab | 访问不同楼层房间后按 M | 地图顶部出现楼层 Tab（F0 / F-1 等） |
| D2 | 层间转场增强 | 通过 IsLayerTransition 门 | 更长淡入淡出 + 粒子 + SFX + 相机缩放（需配置字段） |
| D3 | BGM 交叉淡入 | 目标房间 RoomSO 有 _ambientMusic | 音乐平滑过渡（需配置 AudioClip） |

---

### 第八步：层间转场增强配置（可选，Phase 5 细化）

> 层间转场效果通过 **`DoorTransitionController`** 组件控制。

**在哪里找到或创建 DoorTransitionController：**

1. 在 **Hierarchy** 中找到场景中的 `Managers` GameObject（或创建一个空的 GameObject 命名为 `TransitionController`）
2. 如果该 GameObject 上没有 `DoorTransitionController` 组件，选中它 → Add Component → `DoorTransitionController`
3. 以下字段**均为可选**——不配置时转场只使用基础淡入淡出（和普通 Door 一样）。
4. 在 Inspector 中配置：

| Inspector 字段 | 推荐值 | 说明 |
|---------------|--------|------|
| `_layerTransitionParticles` | 在该 GO 下创建子物体，挂 ParticleSystem，拖入 | 层间转场时播放的粒子效果（如下落/上升） |
| `_layerTransitionSFX` | Project 中的 AudioClip | 转场音效（rumble / whoosh），从 Project 窗口拖入 |
| `_bgmCrossfadeDuration` | 1.5 | BGM 交叉淡入时长（秒） |
| `_layerZoomOutAmount` | 2 | 转场时相机缩放幅度（正交 Size 增加量） |
| `_layerZoomDuration` | 0.3 | 缩放动画时长（秒） |

> **配合 RoomSO 设置 BGM**：要实现楼层间 BGM 切换，需要在目标楼层的 `RoomSO` 资产中设置 `_ambientMusic` 字段（拖入 AudioClip）。
> 当玩家通过 `IsLayerTransition = true` 的门进入该房间时，`DoorTransitionController` 会自动交叉淡入新 BGM。

---

### 第九步：NarrativeFallTrigger 验证（占位符）

> 当前为**占位符实现**，仅验证脚本可放置和基本传送功能。
> 完整的叙事坠落动画（相机跟随、粒子特效、Timeline 等）将在后续版本实现。

**在哪里操作**：在 **Hierarchy** 中的场景里

1. 在场景中创建空 GameObject，命名为 `TestFallTrigger`
2. Add Component → `BoxCollider2D`，勾选 **Is Trigger**，放在想触发坠落的位置
3. Add Component → `NarrativeFallTrigger`
4. Inspector 配置：

   | 字段 | 赋值 |
   |------|------|
   | `_targetRoom` | Hierarchy → 目标楼层的 Room GameObject |
   | `_landingPoint` | Hierarchy → 目标 Room 下的某个子 Transform（着陆点位置） |
   | `_playerLayer` | Layer 下拉菜单选 `Player` |

5. **Play Mode 验证**：
   - 飞船进入触发区 → Console 输出 `PLACEHOLDER — Fall sequence triggered`
   - 飞船被瞬间传送到目标房间的 `_landingPoint` 位置
   - 这是**正确的占位行为**，不会有动画效果

---

### 第十步：配置 Phase 6 — 世界时钟与动态关卡（一键生成 + 手动配置）

> **概览**：Phase 6 包含世界时钟（时间循环）、阶段管理（辐射潮/平静期/风暴期/寂静时）、
> 定时行为（ScheduledBehaviour）、永久世界变化（WorldEventTrigger）、Tilemap 变体切换、
> 全局氛围控制（后处理+粒子+BGM+低通滤波）和房间变体系统。
>
> **一键创建** 可以完成 80% 的配置。剩余 20% 是需要手动赋值的美术/音频资源。

---

#### 10A. 一键创建 Phase 6 资产和场景管理器（自动）

1. 在 **Unity 菜单栏**中，点击 `ProjectArk` → `Level` → `Phase 6: Setup All (Assets + Scene)`
2. 等待弹窗确认 → 点击 "OK"
3. 检查结果：

   **在 Project 窗口中**（`Assets/_Data/Level/WorldPhases/`）：

   | 资产文件 | 阶段名 | 时间范围（归一化） | 氛围色 | 特殊效果 |
   |---------|--------|-------------------|--------|---------|
   | `Phase_0_RadiationTide.asset` | Radiation Tide（辐射潮） | 0.000 ~ 0.208 | 偏红 | — |
   | `Phase_1_CalmPeriod.asset` | Calm Period（平静期） | 0.208 ~ 0.500 | 暖白 | 敌人伤害/血量 ×0.9 |
   | `Phase_2_StormPeriod.asset` | Storm Period（风暴期） | 0.500 ~ 0.750 | 偏蓝灰 | 低通滤波 800Hz，敌人 ×1.3/×1.2 |
   | `Phase_3_SilentHour.asset` | Silent Hour（寂静时） | 0.750 ~ 1.000 | 偏紫暗 | 隐藏通道可见 |

   **在 Hierarchy 中**：

   ```
   Managers
   ├── ... (已有的管理器)
   ├── WorldClock           ← WorldClock 组件，周期 1200s，倍速 1x
   ├── WorldPhaseManager    ← WorldPhaseManager 组件，_phases 数组已连接 4 个 SO
   └── AmbienceController   ← AmbienceController 组件（Volume/粒子待手动赋值）
   ```

> 如果 `Managers` GameObject 不存在，工具会自动创建。
> 如果组件已存在，工具会跳过不重复创建（幂等设计）。

---

#### 10B. 手动配置 AmbienceController（可选，提升体验）

> AmbienceController 的后处理和粒子引用需要手动赋值。不赋值也不影响其他功能运行。

**在哪里操作**：在 **Hierarchy** 中选中 `Managers/AmbienceController` GameObject

| Inspector 字段 | 赋值方式 | 说明 |
|---------------|---------|------|
| `_postProcessVolume` | 从 Hierarchy 拖入场景中的 **Global Volume** | 用于 Vignette 和 ColorAdjustments 渐变。如果场景中没有 Volume，创建一个：Hierarchy → 右键 → Volume → Global Volume |
| `_phaseParticles` | 设置数组大小为 4，每个元素对应一个阶段的 ParticleSystem | 索引 0 = 辐射潮粒子，1 = 平静期（可为空），2 = 风暴期粒子，3 = 寂静时粒子。不需要的阶段留空即可 |
| `_postProcessTransitionDuration` | 2.0（默认） | 后处理渐变时长 |
| `_bgmCrossfadeDuration` | 2.0（默认） | BGM 交叉淡入时长 |

**为 WorldPhaseSO 配置 BGM**（可选）：

1. 在 **Project 窗口**选中 `Assets/_Data/Level/WorldPhases/Phase_2_StormPeriod.asset`
2. 在 Inspector 中将你的风暴 BGM AudioClip 拖入 `_phaseBGM` 字段
3. 对其他阶段重复此操作（每个阶段可以有不同的 BGM，也可以留空表示不切换）

---

#### 10C. 配置 ScheduledBehaviour（定时行为）

> 用于让 NPC、门、通道等按世界阶段自动启用/禁用。

**在哪里操作**：在 **Hierarchy** 中，选中需要按时间控制的 GameObject

**示例：平静期 NPC 交易**

1. 在房间内创建一个 NPC GameObject（或任意你想控制的对象）
2. 在该 GameObject 上（或其父物体上）Add Component → `ScheduledBehaviour`
3. Inspector 配置：

   | 字段 | 值 | 说明 |
   |------|----|------|
   | `_activePhaseIndices` | `[1]`（数组大小 1，值为 1） | 索引 1 = Calm Period，只在平静期显示 |
   | `_targetGameObject` | 拖入 NPC 的 GameObject（或留空表示控制自身子物体） | 被控制的目标 |
   | `_invertLogic` | false | false = 在指定阶段启用。true 则反转 |

**示例：风暴期隐藏通道**

   | 字段 | 值 | 说明 |
   |------|----|------|
   | `_activePhaseIndices` | `[3]`（索引 3 = Silent Hour） | 只在寂静时显示 |
   | `_targetGameObject` | 拖入隐藏通道 GameObject | |
   | `_invertLogic` | false | |

---

#### 10D. 配置 Door Locked_Schedule（定时门）

> 让门按世界阶段自动开关。例如某扇门只在"平静期"打开。

**在哪里操作**：在 **Hierarchy** 中选中一个 Door GameObject

1. 在 Door 组件的 Inspector 中：
   - 将 `_initialState` 设为 **Locked_Schedule**
   - 展开 `_openDuringPhases` 数组，设置大小为 1，值为 `1`（Calm Period）
2. Play Mode 验证：
   - 当世界时间进入平静期 → 门自动变为 Open
   - 当世界时间离开平静期 → 门自动变为 Locked_Schedule

> **注意**：只有 `_initialState = Locked_Schedule` 的门才会响应阶段变化。普通门不受影响。

---

#### 10E. 配置 WorldEventTrigger（永久世界变化）

> 用于 Boss 击杀后开放新区域、地形塌陷等不可逆变化。

**在哪里操作**：在 **Hierarchy** 中创建新 GameObject

1. 创建空 GameObject，命名如 `PostBoss_OpenNewArea`
2. Add Component → `WorldEventTrigger`
3. Inspector 配置：

   | 字段 | 值 | 说明 |
   |------|----|------|
   | `_requiredWorldStage` | 1 | 当 WorldProgressManager 达到 stage 1（击杀 Boss）时触发 |
   | `_enableOnTrigger` | 拖入要开放的新区域 GameObject（如被禁用的新通道） | 触发后启用 |
   | `_disableOnTrigger` | 拖入要移除的旧障碍 GameObject（如挡路的石块） | 触发后禁用 |
   | `_onTriggered` | （可选）点 `+` 添加 UnityEvent 回调 | 可调用 TilemapVariantSwitcher.SwitchToVariant |
   | `_persistenceKey` | 留空（默认用 gameObject.name）或填自定义 key | 存档持久化标识 |

> **持久化**：WorldEventTrigger 的触发状态通过 `ProgressSaveData.Flags` 保存。
> 关闭游戏再打开，已触发的变化不会丢失。

---

#### 10F. 配置 TilemapVariantSwitcher（Tilemap 变体）

> 用于结构性地形变化（如塌陷前/塌陷后的两套 Tilemap）。

**在哪里操作**：在 **Hierarchy** 中

1. 创建一个空 GameObject，命名如 `TerrainVariants`
2. 在其下创建两个子物体，分别放置不同的 Tilemap：
   ```
   TerrainVariants
   ├── BeforeCollapse    ← 子物体 0：塌陷前的 Tilemap
   └── AfterCollapse     ← 子物体 1：塌陷后的 Tilemap（初始禁用）
   ```
3. 在 `TerrainVariants` 上 Add Component → `TilemapVariantSwitcher`
4. Inspector 配置：
   - `_defaultVariantIndex` = `0`（默认显示 BeforeCollapse）
5. 配合 WorldEventTrigger 使用：
   - 在 WorldEventTrigger 的 `_onTriggered` UnityEvent 中，点 `+`
   - 拖入 `TerrainVariants` GameObject
   - 选择函数 `TilemapVariantSwitcher > SwitchToVariant`
   - 参数填 `1`（切换到 AfterCollapse）

---

#### 10G. 配置 Room 变体（可选）

> 让房间在不同世界阶段有不同的敌人配置和环境。

**在哪里操作**：在 **Hierarchy** 中选中一个 Room GameObject + **Project 窗口**创建资产

1. 先创建 RoomVariantSO 资产：
   - Project 窗口 → 右键 → `Create` → `ProjectArk` → `Level` → `Room Variant`
   - 命名如 `Room_Hub_NightVariant`
   - Inspector 配置：

   | 字段 | 值 | 说明 |
   |------|----|------|
   | `_variantName` | "Night Config" | 变体名称 |
   | `_activePhaseIndices` | `[2, 3]`（风暴期和寂静时） | 在这些阶段激活此变体 |
   | `_overrideEncounter` | （可选）拖入一个夜间专用 EncounterSO | 覆盖默认怪物配置 |
   | `_environmentIndex` | 0 | 启用 Room 的 `_variantEnvironments[0]` 子物体 |

2. 在 Room GameObject 的 Inspector 中：
   - 展开 `_variants` 数组，拖入上面创建的 RoomVariantSO
   - 展开 `_variantEnvironments` 数组，拖入对应的环境子物体（如不同的灯光/装饰 GameObject）

> 当世界阶段切换到匹配的 phase 时，Room 会自动激活对应变体的环境子物体并使用变体的遭遇配置。

---

#### 10H. 快速测试建议

> 默认周期是 1200 秒（20 分钟），太长不便于测试。

**加速测试方法**：

1. 在 Hierarchy 选中 `Managers/WorldClock`
2. 在 Inspector 中将 `_cycleDuration` 临时改为 `60`（1 分钟完整周期）
3. 或者将 `_timeSpeed` 改为 `20`（20 倍速，1 分钟跑完 20 分钟的周期）
4. 进入 Play Mode，观察 Console 日志：
   - `[WorldPhaseManager] Phase changed: Radiation Tide → Calm Period (index 1)` — 阶段切换
   - `[WorldClock] Cycle completed` — 周期完成

---

### 第十一步：Phase 6 Play Mode 功能验证

#### 11A. 世界时钟基础

| # | 测试项 | 操作 | 预期 |
|---|--------|------|------|
| G5 | 阶段切换日志 | 加速时钟后等待 | Console 依次输出 4 个阶段的切换日志 |
| G6 | 时间递增 | 观察 WorldClock Inspector | `CurrentTime` 持续增长，到达 CycleDuration 后重置 |
| G6 | 周期完成 | 等待完整周期 | Console 输出 `[WorldClock]` cycle completed 日志 |

#### 11B. 阶段驱动功能

| # | 测试项 | 操作 | 预期 |
|---|--------|------|------|
| G7 | ScheduledBehaviour | 配置一个只在 Calm 显示的 GO | GO 在平静期出现，其他阶段消失 |
| G8 | Door Locked_Schedule | 配置定时门 | 门在配置的阶段自动开/关 |
| G13 | Room 变体 | 配置房间变体 | 阶段切换时环境子物体自动切换 |

#### 11C. 永久世界变化

| # | 测试项 | 操作 | 预期 |
|---|--------|------|------|
| G10 | WorldEventTrigger | 手动调用 `LevelEvents.RaiseWorldStageChanged(1)` 或击杀 Boss | 配置的 GO 启用/禁用 |
| G9 | TilemapVariantSwitcher | 由 WorldEventTrigger 调用 | 子 Tilemap 从变体 0 切换到变体 1 |
| G10 | 持久化 | 触发后存档 → 重新加载 | 触发状态保持，不重复触发 |

#### 11D. 氛围系统

| # | 测试项 | 操作 | 预期 |
|---|--------|------|------|
| G12 | 后处理渐变 | 阶段切换（需配置 Volume） | Vignette 强度 + 色调过滤渐变 |
| G12 | BGM 切换 | 阶段切换（需配置 PhaseBGM） | 音乐交叉淡入 |
| G12 | 低通滤波 | 进入风暴期 | 声音变闷（低通滤波 800Hz） |
| G12 | 粒子切换 | 阶段切换（需配置 _phaseParticles） | 对应阶段的环境粒子播放/停止 |

#### 11E. 存档集成

| # | 测试项 | 操作 | 预期 |
|---|--------|------|------|
| G11 | 时钟存档 | 在某阶段激活存档点 → 退出重进 | WorldClock 恢复到存档时的时间 |
| G11 | 阶段存档 | 同上 | WorldPhaseManager 恢复到存档时的阶段 |

---

## 常见问题排查

| 现象 | 可能原因 | 解决方案 |
|------|---------|---------|
| 编译错误 `CS0246: TextMeshPro` | Level.asmdef 缺少 TMP 引用 | 确认 `ProjectArk.Level.asmdef` 有 `Unity.TextMeshPro` |
| 编译错误 `InputAction` | Level.asmdef 缺少 InputSystem 引用 | 确认有 `Unity.InputSystem` |
| M 键无反应 | MapPanel 未配置 `_inputActions` | 赋值 ShipActions.inputactions |
| MapPanel 打开后空白 | 未配置 Prefab 引用 | 检查 `_roomWidgetPrefab` / `_connectionLinePrefab` |
| 地图无房间 | MinimapManager 未添加到场景 | 确认有 MinimapManager 组件 |
| Dash 无反应 | ShipDash 未挂到飞船 | 添加 ShipDash 并赋值 `_stats` |
| 无屏幕震动 | CinemachineImpulseSource 未注册 | 见第三步第 6 项 |
| Arena 不锁门 | RoomSO type 不是 Arena/Boss | 检查 RoomSO `_type` |
| Arena 不刷怪 | EncounterSO 的 Entry.EnemyPrefab 为空 | 赋值有效的敌人 Prefab |
| 存档无效 | SaveBridge 未在场景中 | 添加 SaveBridge 到管理器 GO |
| Scaffolder 菜单不可见 | Editor asmdef 配置问题 | 确认 `ProjectArk.Level.Editor.asmdef` 引用正确 |
| Phase 6 菜单不可见 | Editor asmdef 缺少引用 | 确认 `ProjectArk.Level.Editor.asmdef` 引用 `ProjectArk.Level` |
| WorldPhaseManager 无阶段 | 未运行资产创建工具 | 先运行 `ProjectArk > Level > Phase 6: Create World Clock Assets` |
| 阶段不切换 | WorldClock 未在场景中 | 确认 Managers 下有 WorldClock 组件 |
| 阶段切换太慢看不到 | 默认 1200s 周期 | 临时把 `_cycleDuration` 改为 60，或 `_timeSpeed` 改为 20 |
| AmbienceController 无效果 | Volume 未赋值 | 将场景 Global Volume 拖入 `_postProcessVolume` 字段 |
| ScheduledBehaviour 无反应 | `_activePhaseIndices` 未填 | 确认数组有值且对应正确的阶段索引 |
| Door Locked_Schedule 不开关 | `_initialState` 不是 Locked_Schedule | 必须设为 Locked_Schedule 才会响应阶段 |
| WorldEventTrigger 重复触发 | 存档 Flag 丢失 | 确认 SaveBridge 在场景中且正常保存 |
| Room 变体不切换 | `_variants` 数组为空 | 需创建 RoomVariantSO 并拖入 Room 的 `_variants` |
