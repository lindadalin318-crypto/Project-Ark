# Level Architect 当前支持元素权威矩阵

**更新时间：** 2026-04-13 15:44  
**适用范围：** 当前仓库中的 `Level Architect` Unity Editor 工具链  
**文档定位：** 这份文档回答的是：**`Level Architect` 现在到底能直接搭什么、改什么、看什么、验证什么。**

---

## 1. 文档定位与 authority 说明

这份文档是 `Docs/6_Diagnostics` 下关于 **`Level Architect` 当前支持 authoring 能力** 的现役参考表。

它的用途不是重复介绍 `Level` 模块全部运行时系统，而是专门回答以下问题：

- 当前 `Level Architect` **能直接创建** 哪些元素？
- 当前 `Level Architect` **能直接编辑** 哪些语义？
- 哪些对象只是 **validator / overlay 可见**，但还**不能直接 author**？
- 哪些对象虽然**运行时代码已支持**，但 `Level Architect` 还**没有开放入口**？

### 1.1 文档 authority 边界

这份文档的结论以以下脚本为直接依据：

- `LevelArchitectWindow.cs`
- `RoomFactory.cs`
- `BatchEditPanel.cs`
- `DoorWiringService.cs`
- `LevelRuntimeAssistFactory.cs`
- `LevelValidator.cs`
- `RoomNodeType.cs`
- `ConnectionType.cs`

### 1.2 冲突处理规则

如果这份文档和代码行为冲突：

- **代码是最终真相源**
- 本文档必须在发现冲突的同一轮更新
- 后续新增 `Level Architect` 按钮、入口、starter 或 authoring 语义时，必须同步回写本文档

### 1.3 状态标签说明

| 状态 | 含义 |
| --- | --- |
| **可直接创建** | `Level Architect` 当前有明确按钮/入口，可直接在场景里创建 |
| **可直接编辑** | `Level Architect` 当前有明确 UI，可直接修改现有对象的核心字段 |
| **引导式起点** | 可以创建 starter / scaffold，但仍需作者手动补 SO、key、phase 等业务配置 |
| **仅诊断/显示** | 当前只在 overlay、inspector 或 validator 中可见，不提供直接创建/编辑入口 |
| **运行时支持未开放** | 代码或运行时已支持，但 `Level Architect` 还没有 authoring 入口 |
| **不计入当前支持** | 字段存在、概念存在，或旧链路残留，但不能算作当前可交付的 authoring 能力 |

---

## 2. 当前顶层工作面

> **重要：当前代码中的三工作面是 `Build / Quick Edit / Validate`，不是旧文档里仍可能残留的 `Design / Build / Validate`。**

| 工作面 | 当前状态 | 主要职责 |
| --- | --- | --- |
| **Build** | **现役主工作面** | 白盒建房、切换 `Select / Blockout / Connect`、Quick Play、Validation Slice |
| **Quick Edit** | **现役主工作面** | 搜房、单房 Inspector、连接 Inspector、starter 补件、批量维护 |
| **Validate** | **现役主工作面** | 跑 `Validate All`、查看结果、逐项 Fix、Auto-Fix |

### 2.1 Build 工作面里的 Tool Mode

| Tool Mode | 当前状态 | 用途 |
| --- | --- | --- |
| `Select` | 可直接使用 | 选房、查看当前 authoring 上下文 |
| `Blockout` | 可直接使用 | 白盒建房 / 放房 |
| `Connect` | 可直接使用 | 拖拽连接房间，创建 Door 链路 |

### 2.2 Build / Quick Edit 共用的非 authoring 辅助项

| 项目 | 当前状态 | 说明 |
| --- | --- | --- |
| Floor Level Filter | 仅诊断/显示 | 过滤当前楼层显示，不是内容本身 |
| `Pacing Overlay` | 仅诊断/显示 | 用于读节奏结构，不创建对象 |
| `Critical Path` | 仅诊断/显示 | 用于看主路径，不创建对象 |
| `Lock-Key Graph` | 仅诊断/显示 | 用于看门锁关系，不创建对象 |
| `Connection Types` Overlay | 仅诊断/显示 | 用于看连接语义着色，不创建对象 |

---

## 3. Room authoring 支持矩阵

## 3.1 房间创建入口

| 能力 | 当前状态 | 入口 | 备注 |
| --- | --- | --- | --- |
| Built-in Preset 建房 | **可直接创建** | `Build > Add Room` | 需要先有内置预设 |
| `Create Built-in Presets` | **可直接创建** | `Build` | 生成官方预设集合 |
| `Seed 5-Room Slice` | **可直接创建** | `Build > Validation Slice` | 自动建 `Safe → Transit → Combat → Reward → Return Transit` |
| `Create + Validate` | **可直接创建** | `Build > Validation Slice` | 建切片后立即跑验证 |
| `Create + Quick Play` | **可直接创建** | `Build > Validation Slice` | 建切片后立即做结构 smoke test |
| Duplicate Room | **可直接创建** | `Quick Edit > Room Inspector` | 会复制 `Room` 与 `RoomSO`，并清掉旧 door authoring 方便重接 |
| Save as Preset | **可直接创建** | `Quick Edit > Room Inspector` | 将当前房间保存为新的 `RoomPresetSO` |

## 3.2 当前内置 Room Preset 集合

| Preset | NodeType | 默认尺寸 | 备注 |
| --- | --- | --- | --- |
| `Safe Room` | `Safe` | `15 × 12` | 安全房模板 |
| `Transit Room` | `Transit` | `20 × 15` | 过路房模板 |
| `Combat Room` | `Combat` | `22 × 16` | 开放战斗房模板 |
| `Reward Room` | `Reward` | `18 × 12` | 回报房模板 |
| `Arena Room` | `Arena` | `25 × 20` | 会带 Arena / combat authoring 预期 |
| `Boss Room` | `Boss` | `35 × 25` | Boss 房模板 |
| `Corridor` | `Transit` | `15 × 3` | 狭长 Transit 特化模板，不是独立 `RoomNodeType` |

## 3.3 当前正式支持的 RoomNodeType

| Room Type | 当前状态 | 来源 | 备注 |
| --- | --- | --- | --- |
| `Transit` | **可直接创建 + 可直接编辑** | 预设 / Inspector | 主连接房 |
| `Combat` | **可直接创建 + 可直接编辑** | 预设 / Inspector | 开放战斗房 |
| `Arena` | **可直接创建 + 可直接编辑** | 预设 / Inspector | 竞技场房 |
| `Reward` | **可直接创建 + 可直接编辑** | 预设 / Inspector | 回报房 |
| `Safe` | **可直接创建 + 可直接编辑** | 预设 / Inspector | 安全房 |
| `Boss` | **可直接创建 + 可直接编辑** | 预设 / Inspector | Boss 房 |

### 3.3.1 不应误算为当前 Room Type 的项

| 项目 | 当前状态 | 说明 |
| --- | --- | --- |
| `Corridor` | **不计入独立房型** | 当前只是 `Transit` 的预设名，不是独立枚举值 |
| `normal / narrative / puzzle` 等旧字符串 | **不计入现役房型** | 旧链路或历史 alias，不应当成当前 `Level Architect` 官方房型词法 |

## 3.4 当前 Room Inspector 可直接编辑字段

| 字段 / 动作 | 当前状态 | 入口 | 备注 |
| --- | --- | --- | --- |
| `RoomSO` 赋值 | **可直接编辑** | `Quick Edit > Room Inspector` | 可切换房间数据资产 |
| `Room ID` | **可直接编辑** | `Quick Edit > Room Inspector` | 直接改 authored id |
| `Display Name` | **可直接编辑** | `Quick Edit > Room Inspector` | 可单独改显示名 |
| `Node Type` | **可直接编辑** | `Quick Edit > Room Inspector` | 改后会同步必要的 scene state |
| `Floor Level` | **可直接编辑** | `Quick Edit > Room Inspector` | 改后会同步 door authoring |
| `Encounter` | **可直接编辑** | `Quick Edit > Room Inspector` | 直接给 `RoomSO` 赋 `EncounterSO` |
| `Size` | **可直接编辑** | `Quick Edit > Room Inspector` | 同步改 `BoxCollider2D` 与 `CameraConfiner` |
| `Set Entry` | **可直接编辑** | `Build / Quick Edit` | 设置 `RoomManager._startingRoom` |
| `Stable Rename` | **可直接编辑** | `Build / Quick Edit` | 按 nodeType + floorLevel 生成稳定命名 |
| `Duplicate` | **可直接创建** | `Quick Edit > Room Inspector` | 在场景中复制房间 |
| `Save Preset` | **可直接创建** | `Quick Edit > Room Inspector` | 保存当前房间为预设 |
| `Focus` | 仅诊断/显示 | `Quick Edit > Room Inspector` | Scene 聚焦 |
| `Select Connected` | 仅诊断/显示 | `Quick Edit > Room Inspector` | 选中相连房间 |
| `Context` | **可直接编辑** | `Quick Edit > Room Inspector` | 打开上下文菜单进行快捷批量操作 |

## 3.5 当前 Batch / Context Menu 支持项

| 能力 | 当前状态 | 备注 |
| --- | --- | --- |
| 批量改 `Node Type` | **可直接编辑** | 多选房间可批量改 |
| 批量改 `Floor Level` | **可直接编辑** | 多选房间可批量改 |
| 批量改 `Size` | **可直接编辑** | 多选房间可批量改 |
| `Apply All` | **可直接编辑** | 一次应用上述三类 |
| `Copy Room Config / Paste Room Config` | **可直接编辑** | 复制 nodeType / floorLevel / size |
| `Auto-Connect Adjacent` | **可直接创建** | 批量自动连接相邻房间 |
| `Assign EncounterSO...` | **可直接编辑** | 单房 context menu 可用 |
| `Set as Entry Room` | **可直接编辑** | 单房 context menu 可用 |
| `Set as Boss / Arena / Combat / Reward / Safe / Transit` | **可直接编辑** | 单房 context menu 可用 |
| `Resize to Default (...)` | **可直接编辑** | 快速套用默认尺寸 |

### 3.5.1 当前不要误判为已支持的 Room 批量能力

| 项目 | 当前状态 | 说明 |
| --- | --- | --- |
| 批量 `RoomSO` 应用 | **不计入当前支持** | `BatchEditPanel` 有 `_batchRoomSO` 字段，但当前没有实际 `ApplyBatchRoomSO` 流程 |
| 批量 `EncounterSO` 应用 | **不计入当前支持** | 当前只有单房赋值入口，没有正式批量入口 |
| 批量 `Set Entry` | **不计入当前支持** | `Entry` 语义本身就是单一入口房，不是批量能力 |

## 3.6 当前 Room 级别存在但未开放 authoring 入口的字段

| 字段 / 系统 | 当前状态 | 说明 |
| --- | --- | --- |
| `RoomVariantSO[] _variants` | **运行时支持未开放** | `Room` 运行时支持 phase variant，但 `Level Architect` 当前没有 UI 入口 |
| `_variantEnvironments` | **运行时支持未开放** | 当前无编辑入口 |
| `RoomSO.MapIcon` | **运行时支持未开放** | 当前 `Level Architect` 未暴露 |
| `RoomSO.AmbientMusic` | **运行时支持未开放** | 当前 `Level Architect` 未暴露，运行时可被 `DoorTransitionController` 消费 |

---

## 4. Connection authoring 支持矩阵

## 4.1 连接创建入口

| 能力 | 当前状态 | 入口 | 备注 |
| --- | --- | --- | --- |
| Connect 模式拖拽连房 | **可直接创建** | `Build > Tool Mode: Connect` | 从一个房间拖到另一个房间，自动建门对 |
| Auto-Connect Adjacent | **可直接创建** | `Batch / Context` | 自动连接共享边的房间 |
| Validation Slice 自动连门 | **可直接创建** | `Build > Validation Slice` | 建 5 房切片时自动连接 |
| 删除连接 | **可直接编辑** | `Quick Edit > Connection Inspector` | 会删除这对房间间的 door link |

## 4.2 当前正式支持的 ConnectionType

| Connection Type | 当前状态 | 来源 | 备注 |
| --- | --- | --- | --- |
| `Progression` | **可直接创建 + 可直接编辑** | Connect / Inspector | 主推进连接 |
| `Return` | **可直接创建 + 可直接编辑** | Connect / Inspector | 回返 / 捷径 |
| `Ability` | **可直接创建 + 可直接编辑** | Connect / Inspector | 能力门 |
| `Challenge` | **可直接创建 + 可直接编辑** | Connect / Inspector | 挑战门 |
| `Identity` | **可直接创建 + 可直接编辑** | Connect / Inspector | 身份 / 章节切换 |
| `Scheduled` | **可直接创建 + 可直接编辑** | Connect / Inspector | 时间 / phase 连接 |

## 4.3 当前 Connection Inspector 可直接操作项

| 字段 / 动作 | 当前状态 | 备注 |
| --- | --- | --- |
| `Connection Type` | **可直接编辑** | 改一个 door 会同步 reciprocal door |
| `Delete` | **可直接编辑** | 删除当前连接 |
| `Flip Direction` | **可直接编辑** | 仅双向连接可用，本质是切换当前 inspector 关注的方向 |
| `Make Return` | **可直接编辑** | 快速将连接语义设为 `Return` |
| `Recalc Landing` | **可直接编辑** | 重算门位置、仪式感和 landing |
| `Focus To Room` | 仅诊断/显示 | 聚焦目标房间 |
| `Select Door` | 仅诊断/显示 | 选中当前 door |
| `Select Target Spawn` | 仅诊断/显示 | 选中目标落点 |
| `Select Reverse Door` | 仅诊断/显示 | 选中 reciprocal door |
| `Create Lock Starter` | **引导式起点** | 会在 owner room 下创建 `Lock` starter 并把门切到 `Locked_Key` |

## 4.4 当前连接上可见但不可直接 author 的字段

| 字段 | 当前状态 | 说明 |
| --- | --- | --- |
| `Gate ID` | **仅诊断/显示** | 当前在 Connection Inspector 中显示，但无直接编辑入口 |
| `Ceremony` | **仅诊断/显示** | 当前显示；主要由 `DoorWiringService` 按楼层关系自动同步 |
| `Direction` | **仅诊断/显示** | 由几何关系推断 |
| `Directionality` | **仅诊断/显示** | 由是否存在 reverse door 推断 |
| `Target Landing` | **仅诊断/显示** | 显示目标落点，不直接编辑数值 |
| `Reverse Link` | **仅诊断/显示** | 显示 reciprocal door 名称 |

## 4.5 当前 Door 运行时支持但未开放的 authoring 字段

| 字段 / 系统 | 当前状态 | 说明 |
| --- | --- | --- |
| `_requiredKeyID` | **运行时支持未开放** | 当前通过 `Lock` starter 间接进入，但无直接 door 字段编辑 UI |
| `_openDuringPhases` | **运行时支持未开放** | `Locked_Schedule` 相关时间门字段当前无 UI |
| `_initialState` 全量编辑 | **运行时支持未开放** | 当前只有 `Create Lock Starter` 会把目标门改为 `Locked_Key` |
| `_ceremony` 手动指定 | **运行时支持未开放** | 当前主要依赖自动同步，不提供手工 authoring 入口 |

---

## 5. Room Elements / Starter Objects 支持矩阵

> 这里列的是 **`Level Architect` 当前能直接补的 room element 起点**，而不是 `Level` 模块全部运行时元素。

## 5.1 当前已开放的 Room Runtime Assist

| 元素 | 当前状态 | 挂点 | 说明 |
| --- | --- | --- | --- |
| `Checkpoint` | **引导式起点** | `Elements` | 会创建 collider + `Checkpoint` 组件；仍需手动指定 `CheckpointSO` |
| `OpenEncounterTrigger` | **引导式起点** | `Encounters` | 会创建 trigger、子 `EnemySpawner`、`SpawnPoints`；仍需手动指定 `EncounterSO` |
| `BiomeTrigger` | **引导式起点** | `Triggers` | 会创建触发器；仍需手动指定 `RoomAmbienceSO` |
| `ScheduledBehaviour` | **引导式起点** | `Triggers` | 会创建 `ScheduledTarget` 子物体；仍需手动补 active phases / target |
| `WorldEventTrigger` | **引导式起点** | `Triggers` | 会创建基础对象；仍需补世界阶段与效果 |
| `ContactHazard` | **引导式起点** | `Hazards` | 会创建 trigger collider + `ContactHazard` 组件；创建后仍需手动调伤害、击退和命中冷却。 |
| `DamageZone` | **引导式起点** | `Hazards` | 会创建 trigger collider + `DamageZone` 组件；创建后仍需手动调伤害、tick 间隔和覆盖范围。 |
| `TimedHazard` | **引导式起点** | `Hazards` | 会创建 trigger collider + `TimedHazard` 组件；创建后仍需手动调激活周期、命中冷却和视觉同步。 |
| `Lock` Starter | **引导式起点** | `Elements` | 连接级入口；会绑定当前 door，并把 door 初始状态改为 `Locked_Key` |

## 5.2 当前 validator 已覆盖，但未开放直接创建按钮的元素

| 元素 | 当前状态 | 说明 |

| --- | --- | --- |
| `HiddenAreaMask` | **运行时支持未开放** | `LevelValidator` 会检查，但 `Level Architect` 当前没有 starter 按钮 |
| `ActivationGroup` | **运行时支持未开放** | validator 已覆盖根节点与成员检查，但无创建入口 |
| `DestroyableObject` | **运行时支持未开放** | validator 已覆盖 preferred root，但无创建入口 |

## 5.3 当前系统存在，但不应算作 Level Architect 已支持 authoring 的元素

| 元素 | 当前状态 | 说明 |

| --- | --- | --- |
| `PickupBase` 派生物 | **运行时支持未开放** | 当前无统一 starter / inspector authoring 入口 |
| `CameraTrigger` | **运行时支持未开放** | 当前无 starter / validator 双向收口 |
| `NarrativeFallTrigger` | **不计入当前支持** | 仍应视为占位 / 半实现 |

---

## 7. 当前“支持搭建元素”总表

> 这张表用于后续新增元素时快速对照：**只有落在“可直接创建 / 可直接编辑 / 引导式起点”三列里的项，才算当前 `Level Architect` 已支持搭建。**

| 分类 | 元素 / 语义 | 当前状态 | 当前入口 |
| --- | --- | --- | --- |
| Workbench | `Build` | **可直接使用** | `LevelArchitectWindow` |
| Workbench | `Quick Edit` | **可直接使用** | `LevelArchitectWindow` |
| Workbench | `Validate` | **可直接使用** | `LevelArchitectWindow` |
| Tool Mode | `Select / Blockout / Connect` | **可直接使用** | `Build` |
| Room | `Transit / Combat / Arena / Reward / Safe / Boss` | **可直接创建 + 可直接编辑** | 预设 / Inspector |
| Room | `Corridor` 预设 | **可直接创建** | 预设按钮 |
| Room | Validation Slice | **可直接创建** | `Build` |
| Room | `RoomSO / Room ID / Display Name / Node Type / Floor Level / Encounter / Size` | **可直接编辑** | `Quick Edit > Room Inspector` |
| Room | `Set Entry / Stable Rename / Duplicate / Save Preset` | **可直接编辑 / 创建** | `Build / Quick Edit` |
| Connection | `Progression / Return / Ability / Challenge / Identity / Scheduled` | **可直接创建 + 可直接编辑** | Connect / Inspector |
| Connection | 删除 / Make Return / Recalc Landing | **可直接编辑** | `Connection Inspector` |
| Connection | Lock starter | **引导式起点** | `Connection Assist` |
| Element | `Checkpoint` | **引导式起点** | `Runtime Assist` |
| Element | `OpenEncounterTrigger` | **引导式起点** | `Runtime Assist` |
| Element | `BiomeTrigger` | **引导式起点** | `Runtime Assist` |
| Element | `ScheduledBehaviour` | **引导式起点** | `Runtime Assist` |
| Element | `WorldEventTrigger` | **引导式起点** | `Runtime Assist` |
| Element | `ContactHazard` | **引导式起点** | `Runtime Assist` |
| Element | `DamageZone` | **引导式起点** | `Runtime Assist` |
| Element | `TimedHazard` | **引导式起点** | `Runtime Assist` |
| Overlay | `Pacing / Critical Path / Lock-Key / Connection Types` | **仅诊断/显示** | Build / Quick Edit |

| Validate | `Validate All / Fix / Auto-Fix` | **可直接使用** | `Validate` |

---

## 8. 当前明确不应计入“Level Architect 已支持搭建”的项

以下项目即使在代码、validator、overlay 或运行时里出现，也**不应在团队沟通中说成“Level Architect 已经支持了”**：

| 项目 | 原因 |
| --- | --- |
| `rooms[].elements[]` 自动导入 | 当前不会自动生成场景对象 |
| `HiddenAreaMask` / `ActivationGroup` / `DestroyableObject` 的 starter 创建 | 当前无 authoring 按钮 |
| `PickupBase` / `CameraTrigger` / `NarrativeFallTrigger` 的统一工具化 authoring | 当前无正式入口 |

| `GateID / Ceremony / RequiredKeyID / openDuringPhases` 的 Inspector 直接编辑 | 目前只有部分显示或间接改写，没有正式字段编辑 UI |
| `RoomVariantSO` / `_variantEnvironments` authoring | 运行时有，编辑器当前未开放 |
| 旧 `Design` Tab 说法 | 当前代码现役是 `Build / Quick Edit / Validate` |

---

## 9. 维护规则（后续新增元素时必须更新）

以后只要发生以下任一情况，就必须同步更新本文档：

1. `Level Architect` 新增一个 **可点击的创建入口**
2. `Room Inspector` 或 `Connection Inspector` 新增一个 **可编辑字段**
3. `LevelRuntimeAssistFactory` 新增一个 **starter object**
4. `LevelValidator` 新增覆盖项，但工具仍未开放入口，需要把它明确标为 **仅诊断/显示** 或 **运行时支持未开放**

### 9.1 推荐更新顺序

- 先改代码
- 再改本文档
- 最后按 `Docs/5_ImplementationLog/README.md` 的规则回写当月实现日志

### 9.2 推荐沟通口径

以后团队讨论时，建议统一用下面三句话之一，避免语义混乱：


- **“这个元素 `Level Architect` 已支持直接 author。”**
- **“这个元素目前只有 starter / scaffold，仍需手工补业务配置。”**
- **“这个元素运行时已支持，但 `Level Architect` 还没开放入口。”**

---

## 10. 最终结论

**截至当前代码状态，`Level Architect` 已经稳定支持：**

- 房间骨架搭建
- 六类现役房型创建与编辑
- 连接创建与连接语义编辑
- 一批最关键的 starter objects（`Checkpoint / OpenEncounterTrigger / BiomeTrigger / ScheduledBehaviour / WorldEventTrigger / Lock`）
- overlay 诊断与 `Validate` 护栏闭环

**但它还没有完全覆盖整个 `Level` 模块的全部房间元素 authoring。** 当前更准确的说法是：

- **房间与连接 authoring 已进入现役可用状态**
- **starter objects 已覆盖第一批高频 runtime 元素**
- **仍有一批运行时元素只进入了 validator / 运行时，而尚未进入 `Level Architect` 的直接 authoring 面板**

这份矩阵之后应作为：**新增 `Level Architect` 能力时的回写清单、评审基线和协作沟通词法表。**
