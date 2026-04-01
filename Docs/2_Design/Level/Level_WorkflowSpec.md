# 关卡工作流权威规范 (Level Workflow Canonical Spec)

> **文档目的**：定义《静默方舟》关卡搭建的完整工作流、数据类型、工具链、文件清单和已有资产目录。  
> **权威声明**：Unity 关卡模块代码是唯一权威来源，HTML 设计器等外部工具是消费者。  
> **版本**：v1.0 | 2026-04-01  
> **维护者**：首席架构师  

---

## 目录

1. [工作流总览](#1-工作流总览)
2. [路径 A：HTML 设计器导入](#2-路径-a-html-设计器导入)
3. [路径 B：SceneView 手绘](#3-路径-b-sceneview-手绘)
4. [Build 阶段通用操作](#4-build-阶段通用操作)
5. [验证与测试](#5-验证与测试)
6. [核心枚举定义](#6-核心枚举定义)
7. [ScriptableObject 数据类型](#7-scriptableobject-数据类型)
8. [Room 标准层级结构](#8-room-标准层级结构)
9. [Editor 工具链清单](#9-editor-工具链清单)
10. [Runtime 脚本清单](#10-runtime-脚本清单)
11. [已有数据资产目录](#11-已有数据资产目录)
12. [文档清单](#12-文档清单)
13. [JSON 导入格式规范](#13-json-导入格式规范)
14. [已知限制与待办](#14-已知限制与待办)

---

## 1. 工作流总览

```
路径 A: HTML 设计器导入          路径 B: SceneView 手绘
   ┌───────────────────┐         ┌───────────────────┐
   │ LevelDesigner.html │         │ Level Architect    │
   │ → 节点图规划      │         │ → Blockout 模式    │
   │ → Export JSON      │         │ → 拖拽绘制矩形    │
   └─────────┬─────────┘         └─────────┬─────────┘
             │                             │
             │  Import JSON                │  AutoConnect
             │                             │
             ▼                             ▼
   ┌─────────────────────────────────────────────────┐
   │        场景中有标准化 Room GameObject 结构        │
   │  (Room + RoomSO + Door 对 + SpawnPoint + 标准子节点)  │
   └─────────────────────────┬───────────────────────┘
                             │
                    Build Tab 精修
              (移动/改属性/批量编辑/补连)
                             │
                    Overlays 可视化验证节奏
              (Pacing / Critical Path / Lock-Key / Connection)
                             │
                    Validate Tab 8 条规则检查
                             │
                    Quick Play 手感验证
```

**核心原则：**
- 先搭结构、跑通手感，再填内容、调数值
- 一个房间只有一个主职责（`RoomNodeType`）
- 数据驱动：所有配置放 ScriptableObject，不 hardcode

---

## 2. 路径 A：HTML 设计器导入

**适合：** 需要规划拓扑关系后再落地场景的情况

### 步骤

1. **打开工具**
   - Unity 菜单：`ProjectArk > Level > Authority > Level Architect`
   - Design Tab → 点击 "🌐 Open LevelDesigner.html"
   - 浏览器中打开 `Tools/LevelDesigner.html`

2. **设计关卡**
   - 从左侧拖拽房间预设到画布（10 种 RoomNodeType）
   - 选中房间 → 右侧属性面板修改 type / name / floor / size
   - 选中两个房间 → 右键创建连接 → 设置 connectionType
   - 设置 Level Name

3. **导出 JSON**
   - 点击 💾 Export File → 保存 `.json` 文件到 `Docs/3_LevelDesigns/` 目录

4. **导入到场景**
   - Level Architect → Design Tab → "📂 Import LevelDesigner JSON..."
   - 选择 JSON 文件 → 确认对话框
   - **自动生成：** Room GO + 标准子节点 + RoomSO 资产 + Door 对 + SpawnPoint

5. **切换到 Build Tab 继续精修**（→ 见第 4 节）

### 权威文件

| 文件 | 说明 |
|------|------|
| `Tools/LevelDesigner.html` | 浏览器端节点图设计工具（132KB） |
| `Assets/Scripts/Level/Editor/LevelArchitect/LevelSliceBuilder.cs` | JSON → 场景构建器（权威消费端） |

---

## 3. 路径 B：SceneView 手绘

**适合：** 不需要提前规划、边画边改的快速原型

### 步骤

1. **打开工具**
   - Unity 菜单：`ProjectArk > Level > Authority > Level Architect`
   - Build Tab

2. **创建内置预设**（首次使用）
   - Build Tab → "Create Built-in Presets"
   - 生成 5 个 `RoomPresetSO`：Safe / Normal / Arena / Boss / Corridor

3. **Blockout 模式绘制**
   - 模式切换为 "Blockout"
   - **▭ Room 笔刷**：拖拽绘制矩形房间
   - **═ Corridor 笔刷**：拖拽绘制走廊（宽度固定 3f，自动判断水平/垂直）
   - **Shift + 拖拽** = Chain Draw（从已有房间边缘链式生长）
   - 松开鼠标自动调用 `AutoConnectAllAdjacent` 连线相邻房间

4. **Connect 模式手动补连**
   - 模式切换为 "Connect"
   - 从一个房间拖向另一个 → 自动创建双向 Door 对 + SpawnPoint 对
   - 层间连接（FloorLevel 不同时）自动设置 `Ceremony = Layer`

---

## 4. Build 阶段通用操作

两条路径汇合后，都在 Build Tab 里精修：

### Select 模式

| 操作 | 快捷键/方式 |
|------|------------|
| 选中房间 | 鼠标左键点击 |
| 多选 | Shift + 点击 |
| 框选 | 鼠标拖拽框选 |
| 移动房间 | 拖动选中房间（边缘自动吸附 Snap） |
| 删除房间 | Delete 键 |
| 查看/修改单房间属性 | 侧边面板（NodeType / Floor / Encounter / Size / Door 数） |
| 批量编辑 | 多选时显示 Batch Edit 面板（批量改 NodeType / FloorLevel / Size） |

### 右键菜单

- Set as Entry / Boss / Arena / Safe / Transit
- Assign EncounterSO
- Save as Preset
- Copy / Paste Config

### 楼层过滤

- Build Tab → Floor Level 过滤器（All / G / +1 / -1 / -2 / -3）
- 非当前楼层的房间变暗 (alpha 0.15)

### 添加房间

- 侧边面板 → 点预设按钮 → 直接在 SceneView 中心放置

### 可视化叠层 (Overlays)

| 叠层 | 功能 |
|------|------|
| **Pacing Overlay** | 房间按 NodeType 着色 + 战斗强度标注 + 门状态图标 |
| **Critical Path** | BFS 求 Entry → Boss 最短路，黄色粗线高亮 |
| **Lock-Key Graph** | 按 KeyID 彩色标注所有锁门位置 |
| **Connection Types** | 按 ConnectionType 着色连线 + 右上角图例 |

---

## 5. 验证与测试

### Validate Tab（8 条规则）

| 规则 | 检查内容 | Auto-Fix |
|------|----------|----------|
| 1 | Room 缺 RoomSO | ✅ |
| 2 | Room 缺 BoxCollider2D | ✅ |
| 3 | Room 缺 CameraConfiner | ✅ |
| 4 | Room 缺标准子节点（Tilemaps/Elements/Encounters 等） | ✅ |
| 5 | Door 双向连接完整性 / TargetSpawnPoint 为 null | ❌ |
| 6 | PlayerLayer 未配置 | ❌ |
| 7 | Arena/Boss 房间缺 ArenaController / EncounterSO / EnemySpawner | ❌ |
| 8 | 孤立房间（无 Door 连接） | ❌ |

- "Auto-Fix All" 一键修复可修复项
- 不可修复项显示 "→" 跳转到目标对象
- SceneView 中有 Error 的房间显示红色 ⚠

### Quick Play

- Build Tab → "Quick Play ▶"
- 自动检查 RoomManager 和 DoorTransitionController 是否存在
- 缺少时自动创建临时版本（`_QuickPlay_XXX`）
- 直接进入 Play Mode

---

## 6. 核心枚举定义

### 6.1 RoomNodeType — 房间节奏类型

> 定义文件：`Assets/Scripts/Level/Data/RoomNodeType.cs`

| 值 | 语义 | 设计含义 | HTML 别名 |
|----|------|---------|-----------|
| `Transit` | 过路 | 保持移动，连接相邻区域，不承载太重信息 | `corridor` |
| `Pressure` | 压力 | 1-3 组开放敌人，边打边走，增加消耗和警惕 | `normal` |
| `Resolution` | 清算 | 封闭战斗验证玩家是否掌握规则，EncounterClose + 波次 | `arena` |
| `Reward` | 回报 | Checkpoint、宝箱、景观房，紧张后缓冲 | `puzzle` |
| `Anchor` | 锚点/地标 | 明显地标、特殊相机、独特 Biome | `narrative` |
| `Loop` | 回路/捷径 | 解锁门、反向通路、回旧 Checkpoint 的捷径 | — |
| `Hub` | 枢纽 | 多条路径交汇点，大区域中心 | — |
| `Threshold` | 门槛/章节界 | 进入后世界发生不可逆变化，Boss 前厅 | — |
| `Safe` | 安全区 | 完全没有敌意的休息区，Safe Room / 商店 | `safe` |
| `Boss` | Boss | Boss 竞技场，独立演出流程 | `boss` |

**设计原则**：不要说"我要做 8 个房间"，而要说"我要 2 个 Transit + 2 个 Pressure + 1 个 Resolution + 1 个 Reward + 1 个 Anchor + 1 个 Loop"。

### 6.2 ConnectionType — 连接语义类型

> 定义文件：`Assets/Scripts/Level/Data/ConnectionType.cs`

| 值 | 语义 | 玩家心理 | HTML 别名 |
|----|------|---------|-----------|
| `Progression` | 推进 | "我还能往前走吗？" | `normal`, `one_way` |
| `Return` | 回返/捷径 | "我如果回头，会不会更快？" | — |
| `Ability` | 能力门 | "先记住，以后拿到能力再回来" | `locked`, `secret` |
| `Challenge` | 挑战入口 | "你现在有没有资格进入？" | — |
| `Identity` | Biome 切换 | "你刚刚进入了什么类型的地方？" | — |
| `Scheduled` | 时间控制门 | "现在是不是正确的时间？"（Ark 特有） | `tidal` |

### 6.3 TransitionCeremony — 门过渡仪式感

> 定义文件：`Assets/Scripts/Level/Data/TransitionCeremony.cs`

| 值 | 说明 | 演出 |
|----|------|------|
| `None` | 无过渡 | 瞬间切换（同房间隐藏通道） |
| `Standard` | 标准过渡 | 短淡黑 0.3s |
| `Layer` | 层间过渡 | 长淡黑 0.5s + 下坠粒子 + 环境音切换 |
| `Boss` | Boss 门 | 特写演出 + 禁用玩家 + 独立音效 |
| `Heavy` | 重型门 | 多段联动 + 震屏 + 粒子 + 长演出 |

### 6.4 DoorState — 门状态

> 定义文件：`Assets/Scripts/Level/Room/DoorState.cs`

| 值 | 说明 |
|----|------|
| `Open` | 开放通行 |
| `Locked_Combat` | 战斗锁定，清场后解锁 |
| `Locked_Key` | 钥匙锁定，需要 `KeyItemSO` |
| `Locked_Ability` | 能力锁定，需要特定光帆 |
| `Locked_Schedule` | 时间锁定，由 `WorldPhaseManager` 控制 |

### 6.5 EncounterMode — 遭遇模式

> 定义文件：`Assets/Scripts/Level/Data/EncounterMode.cs`

| 值 | 说明 |
|----|------|
| `Closed` | 封门清算：门锁定，必须清全部波次 |
| `Open` | 开放骚扰：门不锁，玩家可脱战 |

---

## 7. ScriptableObject 数据类型

### 7.1 RoomSO — 房间元数据

> 定义文件：`Assets/Scripts/Level/Data/RoomSO.cs`  
> 创建菜单：`ProjectArk/Level/Room`  
> 存放路径：`Assets/_Data/Level/Rooms/`

| 字段 | 类型 | 说明 |
|------|------|------|
| `_roomID` | `string` | 唯一 ID，存档/地图键 |
| `_displayName` | `string` | UI 显示名 |
| `_floorLevel` | `int` | 楼层（0=地面，负=地下） |
| `_nodeType` | `RoomNodeType` | 节奏类型 |
| `_mapIcon` | `Sprite` | 小地图图标 |
| `_ambientMusic` | `AudioClip` | 房间专属 BGM |
| `_encounter` | `EncounterSO` | 战斗配置 |

### 7.2 EncounterSO — 遭遇配置

> 定义文件：`Assets/Scripts/Level/Data/EncounterSO.cs`  
> 创建菜单：`ProjectArk/Level/Encounter`  
> 存放路径：`Assets/_Data/Level/Encounters/`

| 字段 | 类型 | 说明 |
|------|------|------|
| `_mode` | `EncounterMode` | Closed / Open |
| `_waves` | `EnemyWave[]` | 波次列表 |

**EnemyWave 结构：**
| 字段 | 类型 | 说明 |
|------|------|------|
| `DelayBeforeWave` | `float` | 波次前延迟（秒） |
| `Entries` | `EnemySpawnEntry[]` | 敌人条目列表 |

**EnemySpawnEntry 结构：**
| 字段 | 类型 | 说明 |
|------|------|------|
| `EnemyPrefab` | `GameObject` | 敌人 Prefab |
| `Count` | `int` | 数量 |

### 7.3 CheckpointSO — 存档点配置

> 定义文件：`Assets/Scripts/Level/Data/CheckpointSO.cs`  
> 创建菜单：`ProjectArk/Level/Checkpoint`  
> 存放路径：`Assets/_Data/Level/Checkpoints/`

| 字段 | 类型 | 说明 |
|------|------|------|
| `_checkpointID` | `string` | 唯一 ID |
| `_displayName` | `string` | UI 显示名 |
| `_restoreHP` | `bool` | 是否回满 HP |
| `_restoreHeat` | `bool` | 是否重置热量 |
| `_activationSFX` | `AudioClip` | 激活音效 |

### 7.4 KeyItemSO — 钥匙配置

> 定义文件：`Assets/Scripts/Level/Data/KeyItemSO.cs`  
> 创建菜单：`ProjectArk/Level/Key Item`  
> 存放路径：`Assets/_Data/Level/Keys/`

| 字段 | 类型 | 说明 |
|------|------|------|
| `_keyID` | `string` | 唯一 ID |
| `_displayName` | `string` | UI 显示名 |
| `_icon` | `Sprite` | 物品图标 |
| `_description` | `string` | 物品描述 |

### 7.5 WorldPhaseSO — 世界时间阶段

> 定义文件：`Assets/Scripts/Level/Data/WorldPhaseSO.cs`  
> 创建菜单：`ProjectArk/Level/World Phase`  
> 存放路径：`Assets/_Data/Level/WorldPhases/`

| 字段 | 类型 | 说明 |
|------|------|------|
| `_phaseName` | `string` | 阶段名 |
| `_startTime` / `_endTime` | `float (0..1)` | 归一化时间区间 |
| `_ambientColor` | `Color` | 后处理色调 |
| `_phaseBGM` | `AudioClip` | 阶段 BGM |
| `_applyLowPassFilter` | `bool` | 是否低通滤波 |
| `_enemyDamageMultiplier` | `float` | 敌人伤害倍率 |
| `_enemyHealthMultiplier` | `float` | 敌人血量倍率 |
| `_hiddenPathsVisible` | `bool` | 是否显示隐藏路径 |

### 7.6 WorldProgressStageSO — 世界进度里程碑

> 定义文件：`Assets/Scripts/Level/Data/WorldProgressStageSO.cs`  
> 创建菜单：`ProjectArk/Level/World Progress Stage`  
> 存放路径：`Assets/_Data/Level/WorldStages/`

| 字段 | 类型 | 说明 |
|------|------|------|
| `_stageIndex` | `int` | 阶段索引 (0=初始) |
| `_stageName` | `string` | 阶段名 |
| `_requiredBossIDs` | `string[]` | 需要击败的 Boss |
| `_unlockDoorIDs` | `string[]` | 到达后解锁的门 |

### 7.7 RoomVariantSO — 房间变体（时间阶段驱动）

> 定义文件：`Assets/Scripts/Level/Data/RoomVariantSO.cs`  
> 创建菜单：`ProjectArk/Level/Room Variant`

| 字段 | 类型 | 说明 |
|------|------|------|
| `_variantName` | `string` | 变体名 |
| `_activePhaseIndices` | `int[]` | 激活的阶段索引 |
| `_overrideEncounter` | `EncounterSO` | 覆盖遭遇配置 |
| `_environmentIndex` | `int` | 激活的环境子节点索引 |

### 7.8 RoomAmbienceSO — 房间级氛围预设

> 定义文件：`Assets/Scripts/Level/Data/RoomAmbienceSO.cs`  
> 创建菜单：`ProjectArk/Level/Room Ambience`

| 字段 | 类型 | 说明 |
|------|------|------|
| `_presetName` | `string` | 预设名 |
| `_ambientColorOverride` | `Color` | 颜色覆盖 (alpha=0 不覆盖) |
| `_vignetteIntensityOverride` | `float` | 暗角覆盖 (负=不覆盖) |
| `_bgmOverride` | `AudioClip` | BGM 覆盖 |
| `_applyLowPass` | `bool` | 低通滤波 |
| `_particlePrefab` | `ParticleSystem` | 粒子预制件 |
| `_transitionDuration` | `float` | 过渡时长 |

### 7.9 RoomPresetSO — 房间预设模板

> 定义文件：`Assets/Scripts/Level/Data/RoomPresetSO.cs`  
> 创建菜单：`ProjectArk/Level/Room Preset`

用于 `RoomFactory` 一键创建标准化房间。内置 5 个预设：Safe / Normal / Arena / Boss / Corridor。

### 7.10 LevelScaffoldData — 关卡骨架数据

> 定义文件：`Assets/Scripts/Level/Data/LevelScaffoldData.cs`  
> 创建菜单：`ProjectArk/Level/Level Scaffold`  
> 存放路径：`Assets/_Data/Level/Scaffolds/`

存储关卡设计阶段的全部房间布局、连接关系和内部元素。可序列化为 JSON。

---

## 8. Room 标准层级结构

每个 Room GameObject 必须按以下层级组织：

```
Room_[RoomID]                           ← Room 组件 + Collider2D
├── Tilemaps/                           ← 所有 Tilemap 层
│   ├── Ground                          ← 地面层
│   ├── Wall                            ← 碰撞墙体
│   ├── Decoration_BG                   ← 背景装饰
│   ├── Decoration_FG                   ← 前景装饰
│   ├── Hazard                          ← 危险区域
│   └── Variant_[Name]                  ← 可选变体层
│
├── Elements/                           ← 功能物件
│   ├── Door_[方向]_[目标]              ← Door 组件
│   ├── Checkpoint_[ID]                 ← Checkpoint 组件
│   ├── NPC_[名称]                      ← NPC
│   ├── Chest_[ID]                      ← 宝箱/拾取物
│   └── Lock_[条件]                     ← 锁钥物件
│
├── Encounters/                         ← 战斗内容
│   ├── EncounterClose_[ID]             ← ArenaController
│   │   ├── SpawnPoint_01
│   │   └── Arena_Boundary
│   ├── EncounterOpen_[ID]              ← OpenEncounterTrigger
│   └── Hazard_[类型]                   ← EnvironmentHazard
│
├── Decoration/                         ← 纯氛围
│   ├── Ambient_Particles
│   ├── Lights/
│   └── Props/
│
├── Triggers/                           ← 触发器层
│   ├── CameraTrigger_[效果]
│   ├── BiomeTrigger_[氛围名]
│   ├── HiddenAreaMask_[区域名]
│   └── WorldEventTrigger_[ID]
│
└── ActivationGroups/                   ← 性能分组
    ├── AG_FarDecor
    ├── AG_NearInteract
    └── AG_Performance
```

---

## 9. Editor 工具链清单

> 所有文件位于 `Assets/Scripts/Level/Editor/LevelArchitect/`

| 文件 | 类名 | 职责 |
|------|------|------|
| `LevelArchitectWindow.cs` | `LevelArchitectWindow` | 主窗口，三 Tab（Design/Build/Validate）+ SceneView 工具栏 |
| `BlockoutModeHandler.cs` | `BlockoutModeHandler` | Blockout 模式：矩形/走廊笔刷、Chain Draw |
| `RoomBlockoutRenderer.cs` | `RoomBlockoutRenderer` | SceneView 渲染 + Select 模式交互（移动/框选/Snap/删除） |
| `DoorWiringService.cs` | `DoorWiringService` | 双向门自动连线、共享边检测、Connect 模式拖拽 |
| `PacingOverlayRenderer.cs` | `PacingOverlayRenderer` | 4 种叠层渲染 + BFS + 图例 |
| `LevelValidator.cs` | `LevelValidator` | 8 条规则检查 + Auto-Fix |
| `RoomFactory.cs` | `RoomFactory` | 标准子节点创建、5 个内置预设 |
| `LevelSliceBuilder.cs` | `LevelSliceBuilder` | JSON → 场景构建 + RoomSO/EncounterSO/CheckpointSO 自动生成 |
| `BatchEditPanel.cs` | `BatchEditPanel` | 多选批量编辑 + 右键菜单 |
| `ScaffoldSceneBinder.cs` | `ScaffoldSceneBinder` | 场景 → ScaffoldData 单向同步 |
| `SceneScanner.cs` | `SceneScanner` | 逆向扫描场景生成 ScaffoldData（迁移工具） |

### 程序集定义

| 文件 | 说明 |
|------|------|
| `ProjectArk.Level.asmdef` | Runtime 程序集 |
| `ProjectArk.Level.Editor.asmdef` | Editor 程序集（依赖 Runtime） |

---

## 10. Runtime 脚本清单

### Room 核心 (`Scripts/Level/Room/`)

| 文件 | 类名 | 职责 |
|------|------|------|
| `Room.cs` | `Room` | 房间主组件（边界/状态/RoomSO 引用/进入退出事件） |
| `RoomManager.cs` | `RoomManager` | 全局房间管理（当前房间/切换/字典缓存） |
| `Door.cs` | `Door` | 门组件（目标房间/SpawnPoint/GateID/ConnectionType/DoorState） |
| `DoorTransitionController.cs` | `DoorTransitionController` | 门过渡演出（淡黑/粒子/相机/音效） |
| `ArenaController.cs` | `ArenaController` | 封门战斗控制（锁门/波次/解锁） |
| `WaveSpawnStrategy.cs` | `WaveSpawnStrategy` | 波次生成策略 |
| `OpenEncounterTrigger.cs` | `OpenEncounterTrigger` | 开放遭遇触发（区域 aggro + 脱战冷却） |
| `ActivationGroup.cs` | `ActivationGroup` | 基于距离的批量启停 |
| `DestroyableObject.cs` | `DestroyableObject` | 可破坏环境物（持久状态） |
| `RoomFlagRegistry.cs` | `RoomFlagRegistry` | 房间级持久状态标志位 |
| `HiddenAreaMask.cs` | `HiddenAreaMask` | 隐藏区域遮罩（进入后揭示） |

### Camera (`Scripts/Level/Camera/`)

| 文件 | 类名 | 职责 |
|------|------|------|
| `CameraDirector.cs` | `CameraDirector` | 主相机控制（跟随/过渡/Confiner 切换） |
| `CameraTrigger.cs` | `CameraTrigger` | 触发相机行为（缩放/锁定/Dolly） |
| `RoomCameraConfiner.cs` | `RoomCameraConfiner` | 房间边界 Confiner 组件 |

### Checkpoint (`Scripts/Level/Checkpoint/`)

| 文件 | 类名 | 职责 |
|------|------|------|
| `Checkpoint.cs` | `Checkpoint` | 存档点组件（激活/回复/动画） |
| `CheckpointManager.cs` | `CheckpointManager` | 全局存档点管理 |

### Progression (`Scripts/Level/Progression/`)

| 文件 | 类名 | 职责 |
|------|------|------|
| `Lock.cs` | `Lock` | 锁组件（需要 KeyItemSO 解锁） |
| `KeyInventory.cs` | `KeyInventory` | 钥匙持有状态 |
| `WorldProgressManager.cs` | `WorldProgressManager` | 世界进度管理（里程碑/解锁） |

### Map (`Scripts/Level/Map/`)

| 文件 | 类名 | 职责 |
|------|------|------|
| `MinimapManager.cs` | `MinimapManager` | 小地图数据管理（从 Door 推导邻接关系） |
| `MinimapHUD.cs` | `MinimapHUD` | 小地图 UI 渲染 |
| `MapPanel.cs` | `MapPanel` | 大地图面板 |
| `MapRoomWidget.cs` | `MapRoomWidget` | 地图上单个房间的 UI Widget |
| `MapRoomData.cs` | `MapRoomData` | 地图房间数据缓存 |
| `MapConnection.cs` | `MapConnection` | 地图连接数据 |
| `MapConnectionLine.cs` | `MapConnectionLine` | 地图连线 UI |

### WorldClock (`Scripts/Level/WorldClock/`)

| 文件 | 类名 | 职责 |
|------|------|------|
| `WorldClock.cs` | `WorldClock` | 世界时钟（归一化时间 + 倍速） |
| `WorldPhaseManager.cs` | `WorldPhaseManager` | 世界阶段管理（评估当前阶段/事件广播） |

### DynamicWorld (`Scripts/Level/DynamicWorld/`)

| 文件 | 类名 | 职责 |
|------|------|------|
| `AmbienceController.cs` | `AmbienceController` | 全局后处理/BGM 响应 WorldPhase 切换 |
| `BiomeTrigger.cs` | `BiomeTrigger` | 区域氛围触发器（覆盖全局氛围） |
| `ScheduledBehaviour.cs` | `ScheduledBehaviour` | 基于 WorldPhase 的启停行为 |
| `TilemapVariantSwitcher.cs` | `TilemapVariantSwitcher` | Tilemap 变体切换 |
| `WorldEventTrigger.cs` | `WorldEventTrigger` | 世界事件触发器 |

### Hazard (`Scripts/Level/Hazard/`)

| 文件 | 类名 | 职责 |
|------|------|------|
| `EnvironmentHazard.cs` | `EnvironmentHazard` | 环境危险基类 |
| `ContactHazard.cs` | `ContactHazard` | 接触伤害 |
| `DamageZone.cs` | `DamageZone` | 伤害区域 |
| `TimedHazard.cs` | `TimedHazard` | 周期性危险 |

### Pickup (`Scripts/Level/Pickup/`)

| 文件 | 类名 | 职责 |
|------|------|------|
| `PickupBase.cs` | `PickupBase` | 拾取物基类 |
| `HealthPickup.cs` | `HealthPickup` | 血量回复 |
| `HeatPickup.cs` | `HeatPickup` | 热量回复 |
| `KeyPickup.cs` | `KeyPickup` | 钥匙拾取 |

### 其他

| 文件 | 类名 | 职责 |
|------|------|------|
| `GameFlowManager.cs` | `GameFlowManager` | 全局游戏流程（死亡/重生/加载） |
| `NarrativeFallTrigger.cs` | `NarrativeFallTrigger` | 叙事跌落触发（层间演出） |
| `SaveBridge.cs` | `SaveBridge` | 关卡系统 ↔ SaveManager 桥接 |

---

## 11. 已有数据资产目录

### 11.1 示巴星房间 — `Assets/_Data/Level/` 两个来源

#### 早期手工资产 (`Rooms/Sheba/` — 12 个)

| 资产名 | 说明 |
|--------|------|
| `sheba_entrance` | 入口 |
| `sheba_hub` | 枢纽 |
| `sheba_safe_01` | 安全区 |
| `sheba_arena_01` | 竞技场 1 |
| `sheba_arena_02` | 竞技场 2 |
| `sheba_boss` | Boss 房 |
| `sheba_boss_antechamber` | Boss 前室 |
| `sheba_key_chamber` | 钥匙密室 |
| `sheba_corridor_a` | 走廊 A |
| `sheba_corridor_b` | 走廊 B |
| `sheba_corridor_c` | 走廊 C |
| `sheba_underground_01` | 地下区域 |

#### JSON 导入生成资产 (`示巴星___ACT1+ACT2_(Z1a_Z2d)/`)

**Rooms (38 个 RoomSO)：**

| 资产名 | 说明 |
|--------|------|
| `坠机点_残骸区_Data` | 坠机点起始 |
| `坠机点_东侧碎片带_Data` | 东侧探索 |
| `坠机点_南侧裂缝_Data` | 南侧探索 |
| `战后余波_碎片检查_Data` | 碎片检查区 |
| `战后余波_道德选择_Data` | 道德选择点 |
| `战后余波_支线碎片密室_[F=-1]_Data` | 地下支线密室 |
| `涟漪验证_战斗应用_Data` | 战斗验证 |
| `涟漪验证_回音路_Data` | 回音路 |
| `涟漪奖励_密室_Data` | 奖励密室 |
| `涟漪获取_隐藏跌落点_Data` | 隐藏跌落 |
| `首棱镜_竞技场_Data` | 首棱镜竞技场 |
| `频率标记点_Data` | 频率标记 |
| `觉醒战场_主竞技场_Data` | 主竞技场 |
| `觉醒战场_侧路_Data` | 侧路 |
| `觉醒战场_上层观景台_[F=1]_Data` | 上层观景台 |
| `音叉林_中层主路_Data` | 音叉林主路 |
| `音叉林_上层树冠_[F=1]_Data` | 树冠上层 |
| `音叉林_捷径悬崖_Data` | 捷径悬崖 |
| `管风琴走廊_入口_Data` | 管风琴入口 |
| `管风琴走廊_上层平台_[F=1]_Data` | 上层平台 |
| `管风琴走廊_上层秘密_[F=1]_Data` | 上层秘密 |
| `管风琴走廊_缕拉远程援助_Data` | 缕拉援助点 |
| `缕拉首次目击点_Data` | 缕拉首次目击 |
| `缕拉首次面对面_Data` | 缕拉首次面对面 |
| `花园_安全区_Data` | 安全区 |
| `声之走廊_Data` | 声之走廊 |
| `冰雕广场_Data` | 冰雕广场 |
| `冰雕广场_温暖冰雕_Data` | 温暖冰雕 |
| `冰雕广场_地下隐穴_[F=-1]_Data` | 地下隐穴 |
| `雪原_可选探索角落_Data` | 可选探索 |
| `世界时钟预兆_退潮点_Data` | 世界时钟预兆 |
| `叹息深谷_入口_Data` | 深谷入口 |
| `叹息墙_前室_Data` | Boss 前室 |
| `叹息墙_Boss竞技场_Data` | Boss 竞技场 |
| `叹息墙_Boss后奖励_Data` | Boss 后奖励 |
| `下行通道_管壁走廊_Data` | 下行通道 |
| `ACT1出口_峡谷入口_Data` | ACT1 出口 |
| `ACT2出口_枢纽入口_Data` | ACT2 出口 |

**Encounters (17 个 EncounterSO)：**

| 资产名 | 说明 |
|--------|------|
| `坠机点_东侧碎片带_[DEFAULT]_Encounter` | |
| `坠机点_南侧裂缝_[DEFAULT]_Encounter` | |
| `战后余波_碎片检查_[DEFAULT]_Encounter` | |
| `涟漪验证_战斗应用_[DEFAULT]_Encounter` | |
| `首棱镜_竞技场_[DEFAULT]_Encounter` | |
| `觉醒战场_主竞技场_[DEFAULT]_Encounter` | |
| `觉醒战场_侧路_[DEFAULT]_Encounter` | |
| `觉醒战场_上层观景台_[F=1]_[DEFAULT]_Encounter` | |
| `音叉林_中层主路_[DEFAULT]_Encounter` | |
| `音叉林_上层树冠_[F=1]_[DEFAULT]_Encounter` | |
| `管风琴走廊_入口_[DEFAULT]_Encounter` | |
| `管风琴走廊_上层平台_[F=1]_[DEFAULT]_Encounter` | |
| `声之走廊_[DEFAULT]_Encounter` | |
| `叹息深谷_入口_[DEFAULT]_Encounter` | |
| `叹息墙_前室_[DEFAULT]_Encounter` | |
| `叹息墙_Boss竞技场_[DEFAULT]_Encounter` | |
| `下行通道_管壁走廊_[DEFAULT]_Encounter` | |

**Checkpoints (19 个 CheckpointSO)：**

| 资产名 |
|--------|
| `CP_坠机点_残骸区` |
| `CP_战后余波_碎片检查` |
| `CP_战后余波_道德选择` |
| `CP_涟漪奖励_密室` |
| `CP_首棱镜_竞技场` |
| `CP_频率标记点` |
| `CP_音叉林_中层主路` |
| `CP_管风琴走廊_入口` |
| `CP_管风琴走廊_缕拉远程援助` |
| `CP_缕拉首次目击点` |
| `CP_缕拉首次面对面` |
| `CP_花园_安全区` |
| `CP_冰雕广场` |
| `CP_冰雕广场_温暖冰雕` |
| `CP_世界时钟预兆_退潮点` |
| `CP_叹息墙_Boss竞技场` |
| `CP_叹息墙_Boss后奖励` |
| `CP_ACT1出口_峡谷入口` |
| `CP_ACT2出口_枢纽入口` |

### 11.2 其他资产

| 路径 | 数量 | 说明 |
|------|------|------|
| `Keys/Key_AccessAlpha.asset` | 1 | Alpha 权限钥匙 |
| `Keys/Key_BossGate.asset` | 1 | Boss 门钥匙 |
| `WorldPhases/Phase_0_RadiationTide.asset` | — | 辐射潮阶段 |
| `WorldPhases/Phase_1_CalmPeriod.asset` | — | 平静期 |
| `WorldPhases/Phase_2_StormPeriod.asset` | — | 风暴期 |
| `WorldPhases/Phase_3_SilentHour.asset` | — | 寂静时分 |
| `WorldStages/Stage_0_Initial.asset` | — | 初始进度 |
| `WorldStages/Stage_1_PostGuardian.asset` | — | 击败守护者后 |
| `Scaffolds/示巴星___ACT1+ACT2__Z1a→Z2d_.asset` | 1 | 关卡骨架数据 |
| `Encounters/EncounterSO.asset` | 1 | 通用遭遇模板 |

---

## 12. 文档清单

### 权威文档（当前正在维护）

| 文件 | 说明 |
|------|------|
| `Docs/2_Design/Level/Level_WorkflowSpec.md` | **本文档** — 工作流、类型定义、资产目录 |
| `Docs/2_Design/Level/Level_CanonicalSpec.md` | 关卡模块运行时规范 |
| `Docs/2_Design/Level/LevelModulePlan.md` | 关卡模块开发计划（Phase 1-6） |

### 关卡设计数据

| 文件 | 说明 |
|------|------|
| `Docs/3_LevelDesigns/Sheba/ShebaRoomGrammar.md` | 示巴星房间语法规范（房间类型/标准件/检查表） |
| `Docs/3_LevelDesigns/Sheba/Sheba_ACT1_ACT2.json` | 示巴星 ACT1+ACT2 完整拓扑 JSON (38 房间) |
| `Docs/3_LevelDesigns/Sheba/Sheba_FirstSlice.json` | 示巴星首切片 JSON |
| `Docs/3_LevelDesigns/Sheba/Sheba_First15Min.json` | 示巴星前 15 分钟 JSON（占位） |

### GDD

| 文件 | 说明 |
|------|------|
| `Docs/1_GDD/Sheba/关卡心流与节奏控制-2.csv` | 关卡心流 CSV（当前版本） |

### 参考

| 文件 | 说明 |
|------|------|
| `Docs/7_Reference/GameAnalysis/Level_Architecture_Synthesis_Minishoot_Silksong_TUNIC.md` | 关卡架构综合分析 |

---

## 13. JSON 导入格式规范

### LevelSliceBuilder 期望的 JSON 结构

```json
{
  "levelName": "示巴星_ACT1",
  "rooms": [
    {
      "id": "room_001",
      "name": "坠机点_残骸区",
      "type": "reward",
      "x": 0, "y": 0,
      "width": 30, "height": 20,
      "floor": 0,
      "connections": [
        {
          "targetId": "room_002",
          "connectionType": "progression",
          "fromEdge": "right",
          "toEdge": "left"
        }
      ]
    }
  ]
}
```

### type 字段映射

**规范字符串（推荐）：** `transit` | `pressure` | `resolution` | `reward` | `anchor` | `loop` | `hub` | `threshold` | `safe` | `boss`

**HTML 遗留别名（兼容）：** `normal`→Pressure | `arena`→Resolution | `narrative`→Anchor | `puzzle`→Reward | `corridor`→Transit

### connectionType 字段映射

**规范字符串（推荐）：** `progression` | `return` | `ability` | `challenge` | `identity` | `scheduled`

**HTML 遗留别名（兼容）：** `normal`→Progression | `tidal`→Scheduled | `locked`→Ability | `one_way`→Progression | `secret`→Ability

### fromEdge / toEdge 方向值

支持：`left` | `right` | `top` | `bottom`（默认 `right`/`left`）

---

## 14. 已知限制与待办

| 项目 | 状态 | 说明 |
|------|------|------|
| EncounterSO 默认为空壳 | ⚠️ | 所有 `[DEFAULT]_Encounter` 资产只有结构，未配置具体波次和敌人 Prefab |
| 示巴星场景未实际搭建 | ❌ | JSON 已导入生成 RoomSO + EncounterSO，但场景 Room GO 未搭建 |
| Prefab 缺失 | ⚠️ | 无 Door/Checkpoint/Lock/Hazard/Pickup 等 Prefab（场景中直接创建 GO） |
| Play Mode 闭环验证 | ❌ | 未做过完整的 Play Mode 走通验证 |
| `Sheba_First15Min.json` 为空 | ⚠️ | 占位文件，未填充内容 |

---

*本文档由关卡工作流实况扫描自动生成，以 Unity 代码为唯一权威源。*
