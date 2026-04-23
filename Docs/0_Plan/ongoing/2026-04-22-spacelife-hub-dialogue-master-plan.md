# SpaceLife Hub Dialogue — Master Plan

> **文档类型：** Master Plan（主控计划，统摄多份 Phase Plan）
>
> **前置文档：**
> - 设计稿：`Docs/2_TechnicalDesign/SpaceLife/SpaceLife_HubDialogue_SystemDesign.md`
> - 旧 MVP Plan（已被本文取代）：`Docs/0_Plan/ongoing/2026-04-21-spacelife-hub-dialogue-mvp-implementation-plan.md`
>
> **创建时间：** 2026-04-22
>
> **状态：** Active（Phase 0 + Phase 1 + Phase 2 代码层已完成，Phase 3 即将进入；Phase 2 的 Prefab Apply 与 Override Audit 菜单待用户在 Editor 中实机执行验证）
>
> **当前版本：** v1.1（2026-04-22 方案 B，在 v1.0 基础上吸收 4 项审计高优遗漏 P4-P7；详见 §8.5 与 §14）
>
> **统摄范围：** Hub 对话 MVP 的全部实现工作 —— Domain、UI、Coordinator、Scene、SO、Validator、治理约束
>
> **为什么要 Master Plan，而不是继续沿用 04-21 那份 MVP Plan？**
>
> 04-21 MVP Plan 是**按 Task 粒度的实施清单**（Task 1 → Task 8），它能回答“做什么”，但不能回答以下问题：
>
> - 谁是 UI Prefab 的权威？谁是 Scene 接线的权威？谁是旧原型的权威？
> - 旧 `DialogueUI` / `NPCInteractionUI` / `SpaceLifeSetupWindow` 的命运是什么？
> - 哪些事项必须做，哪些可以降级延后？为什么？
> - 架构边界和 treatment 如果再踩坑，防御在哪一层？
>
> Master Plan 要先钉死这些**治理边界**和**范围裁剪**，再把执行细节分派给 Phase Plan（其中 Phase 2/3 会直接沿用旧 Plan 的 Task 清单）。
>
> ---

## 0. Master Plan 与 Phase Plan 的职责分工

| 层级 | 文档 | 回答的问题 |
|---|---|---|
| **Master Plan** | 本文 | 范围、治理边界、authority matrix、phase 拆分、降级决策 |
| **Phase 0 Plan** | 本文 §4 内联 | 设计决策锁定、Authority Matrix 建立 |
| **Phase 1 Plan** | 本文 §5 内联 | Domain 层补丁：IPresenter 接口、HubLock 引用计数、必做 Validator 之一 + 审计高优 4 项（方案 B） |
| **Phase 2 Plan** | 本文 §6 内联 + 沿用 04-21 Plan Task 5 | UI Prefab 从 0 建 + `DialogueUI`/`GiftUI` Presenter 重构 |
| **Phase 3 Plan** | 本文 §7 内联 + 沿用 04-21 Plan Task 6 / 7 + 必做 Validator 补齐 | 接线 Coordinator + Scene 样板 + SO 占位 |
| **Phase 4 Plan** | 本文 §8 内联 | 文档收口 + 旧 Plan 归档 + SetupWindow 冻结声明 |
| **Phase 5 Plan（可选）** | 本文 §9 内联 | SetupWindow 推翻重写 + Silent Fail 全扫（时间够就做） |

> 旧 04-21 MVP Plan 中的 **Task 1/2/3/4（Domain 基建 + 存档）** 在本 Master Plan 下的定位：
>
> - Task 1（asmdef + save schema + NpcId）：并入 **Phase 1**
> - Task 2（DialogueGraphSO / DialogueDatabaseSO / Enums + 数据库 tests）：并入 **Phase 1**
> - Task 3（Runner / ConditionEvaluator / EffectExecutor + runner tests）：并入 **Phase 1**
> - Task 4（RelationshipManager 持久化 + FlagStore tests）：并入 **Phase 1**
> - Task 5（UI 重构）→ **Phase 2**
> - Task 6（NPC / Terminal 接线）→ **Phase 3**
> - Task 7（SO 内容 + Scene 样板）→ **Phase 3**
> - Task 8（文档 + 日志）→ **Phase 4**

---

## 1. Master Goal

> **在 SpaceLife 飞船内部 Hub 建立一套独立、可扩展、无 UI owner 双轨的对话系统，支持 1 个船员 NPC + 1 个终端/AI 的可玩垂直切片，并沉淀出可防御未来 UI 类系统踩坑的治理约束。**

这句话里有两条线：

1. **功能线**：Hub 对话 MVP 能玩（上层 Coordinator、Runner、Presenter、SO 内容、Scene 样板全部闭环）
2. **治理线**：不让 SpaceLife 再次出现 UI owner 双轨、Silent fail、Scene override 漂移、SetActive 初始化顺序坑

两条线都完成，才叫 Master Plan 完成。只完成功能线，会在 3 个月后再出现一次同类踩坑；只完成治理线，本轮交付不了可玩切片。

---

## 2. 范围裁剪决策（Q7 6 必做 + 2 降级）

> Q7 评估的 8 个候选条目，决策如下：

| # | 条目 | 决策 | 落地 Phase |
|---|---|---|---|
| 1 | UI Prefab 体系从 0 建（细粒度 + 零 override + 首次脚本生成） | ✅ **必做** | Phase 2 |
| 2 | `DialogueUI` / `GiftUI` / `NPCInteractionUI` Presenter 重构（CanvasGroup + 消费 ViewModel + 删旧 UI） | ✅ **必做** | Phase 2 |
| 3 | `NPCController` / `TerminalDialogueInteractor` 接线 Coordinator | ✅ **必做** | Phase 3 |
| 4 | `SpaceLifeSetupWindow` 推翻重写（Audit/Preview/Apply 三模式） | ⚠️ **降级** | Phase 4 声明冻结 + Phase 5 可选重写 |
| 5 | Scene 样板切片（1 NPC + 1 Terminal） | ✅ **必做** | Phase 3 |
| 6 | SO 内容占位资产（空 graph 骨架） | ✅ **必做** | Phase 3 |
| 7 | Authority Matrix 专章 | ✅ **必做** | Phase 0 |
| 8 | Validator & Audit 工具 | ⚠️ **拆档** | **必做档（Phase 1/3）** + 可选档（Phase 5） |

### 2.1 降级条目 4 的安全性论证

**SetupWindow 的核心破坏力来自两点：**

- Rebuild 会破坏手工接线 → **被 Phase 2 "UI Prefab 零 override" 架构消除**（场景只保留允许的 override，其余来自 prefab）
- Silent fail（找不到对象默默跳过）→ **被 Phase 3 必做 Validator 之一（Coordinator 依赖 Audit）消除**

因此 Phase 1-3 做完后，SetupWindow 的"烂"只剩下"用着不爽"，不再是"会再次引爆架构"。代价只是在 Phase 4 写一条**"禁止在 Phase 1-3 期间使用 SetupWindow Rebuild"**的执行约束即可。

### 2.2 Validator 拆档明细

**必做档（Phase 1/3 完成，保护可玩闭环）：**

- ✅ `DialogueDatabaseValidator`（Phase 1 Task 2 自带）—— 遍历 `DialogueDatabaseSO` 中所有 `DialogueGraphSO`，检查 `Choice.NextNodeId` / `Node.NextNodeId` / `EntryRule.EntryNodeId` 的 NodeId 引用完整性
- ✅ **UI Prefab Override Audit**（Phase 2 收尾） —— 检查 `SampleScene` 中 `DialogueUI` / `GiftUI` 的 prefab 实例是否有非法字段 override（对齐 "零 override" 约束）
- ✅ **Coordinator Dependency Audit**（Phase 3 收尾） —— Play Mode 启动时，`SpaceLifeDialogueCoordinator.Awake()` 检查能否 Resolve `IDialoguePresenter` / `IGiftPresenter` / `DialogueFlagStore` / `DialogueDatabaseSO` 等依赖，缺失立即 `Debug.LogError`（对齐 Implement_rules.md §3.5 "禁止 silent no-op"）

**可选档（Phase 5 收尾，时间不够直接砍）：**

- ⏸️ Silent Fail 全项目扫描（大工程，且随 Authority Matrix 生效边际收益递减）
- ⏸️ Scene Override 漂移周期性 Audit（延后到漂移实际发生时再做）

---

## 3. 总工期估算

> **口径说明：** 本表为方案 B（Master Plan + 2026-04-22 全模块审计的 4 项高优治理遗漏）定稿版。原 Q7 6 必做 + 2 降级版本工期见 §14 变更日志 v1.0。

| Phase | 工期 | 是否可并行 |
|---|---|---|
| Phase 0 | 0.5 天 | 串行（前置） |
| Phase 1 | **1.5-1.8 天**（+0.8d：审计 P4-P7 补丁） | 串行 |
| Phase 2 | 2-3 天 | 串行 |
| Phase 3 | 1-1.5 天 | 串行 |
| Phase 4 | 0.5 天 | 串行 |
| **必做合计** | **5.5-7.3 天**（取整 **6-7 天**） | — |
| Phase 5（可选） | +1-2 天 | 串行 |
| **含 Phase 5** | **7-9 天** | — |

相比 Q7 全选方案（8-10 天）节省约 **20-30% 工期**，不牺牲架构治理质量，并一次性吸收 4 项审计发现的高优遗漏。

---

## 4. Phase 0 — 设计决策 + Authority Matrix

**目标：** 在一切开工前，先把"谁是权威"钉死。一旦 Authority Matrix 写进 Implement_rules，所有后续 Phase 都必须遵守。

### 4.1 Deliverable 1：补完 SpaceLife Authority Matrix

在 `Implement_rules.md` 中追加 `§12. SpaceLife / Dialogue 模块规则`（结构参照 `§2. Ship / VFX 模块规则`），包含：

#### Authority Matrix（五层权威表）

| 范围 | 对象 / 引用类型 | 唯一权威入口 | 明确禁止的并行写入者 | 备注 |
|---|---|---|---|---|
| **Domain 数据** | `DialogueGraphSO` / `DialogueDatabaseSO` | 手工 authoring in `Assets/_Data/SpaceLife/Dialogue/` | Runtime 不得修改 SO、Editor 工具不得自动生成 graph 内容 | 运行时污染禁止写回 authored asset |
| **Runtime 状态** | session state / context / flags / relationship | `DialogueRunner`（session） + `DialogueFlagStore`（flags） + `RelationshipManager`（relationship） | UI 直接修改业务状态、NPC 脚本自己按 index 选台词 | UI 只消费 ViewModel |
| **UI Prefab 结构** | `DialogueUI.prefab` / `GiftUI.prefab` | **唯一一个 Editor 构建器**（Phase 2 创建，暂定 `SpaceLifeUIPrefabBuilder`）只做 Apply | `SpaceLifeSetupWindow`、Scene 手工拼装、Runtime fallback | 本 Master Plan 明确禁止 Setup Window 参与 UI Prefab 构建 |
| **Scene-only 接线** | `SpaceLifeDialogueCoordinator` 在 `SampleScene` 中的引用拖线（UI 实例、Router、Database） | 手工拖线（Phase 3），或唯一一个 Scene Binder（若 Phase 5 实现） | Runtime 自动 Find、SetupWindow 自动回填 | Scene-only 引用必须写进 Scene Override 白名单 |
| **Debug / Setup 工具** | `SpaceLifeSetupWindow` 等 Editor 工具 | 只允许 Audit/Preview（Phase 4 限定） | 在 Play Mode / OnValidate / Awake 中隐式接管主链 | Phase 4 明确降权，Phase 5 可选重写 |

#### Scene Override 白名单（Phase 0 初稿，Phase 3 定稿）

**允许的 Override：**
- `SampleScene` 中 `SpaceLifeDialogueCoordinator` 对场景 UI 实例 / Router / Database 的拖线引用
- `DialogueUI` / `GiftUI` 实例的 `Canvas.sortingOrder`（若需要）

**禁止的 Override：**
- `DialogueUI` / `GiftUI` prefab 内部子节点结构、CanvasGroup 初始值、RectTransform 布局
- `DialogueUI` 的 Presenter 脚本序列化引用
- 任何数值字段

**处理规则：** 若发现禁止项 override 漂移，不得用 Runtime fallback 补救；必须：
1. 记录是哪个 Phase / Builder 应负责
2. 清理错误 override
3. 补 Validator 防御

### 4.2 Deliverable 2：锁定 Phase 0 设计决策（7 条）

| # | 决策 | 依据 |
|---|---|---|
| D1 | 保留 `DialogueUI` 组件名，重构为 Presenter（不新建第二套面板） | 04-21 Plan §3.1 —— 避免双轨 UI authority |
| D2 | `RelationshipManager` 直接读写 `PlayerSaveData`，不改 `SaveBridge` | 04-21 Plan §3.2 —— 避免 `Level → SpaceLife` 反向耦合 |
| D3 | `Upgrade` / `Intel` / `RelationshipEvent` 统一走 `DialogueServiceRouter`，不在节点里 `OpenXXXUI()` | 04-21 Plan §3.3 —— 对话只产出结果，不持有别系统逻辑 |
| D4 | 新 graph 不复用 `NPCDataSO.DialogueNodes`，另建 `DialogueGraphSO` | 04-21 Plan §3.4 —— 旧结构 index-based、NPC 专属 |
| D5 | MVP 隐藏不满足条件的选项，不做 disabled reason | 04-21 Plan §3.5 —— 减少 Presenter 复杂度 |
| D6 | **UI Prefab 体系从 0 建，不复用 SetupWindow 现有产物** | 本轮 Q7 决策 —— 避免把旧 UI owner 双轨带入新系统 |
| D7 | **SetupWindow 在 Phase 1-3 期间冻结，禁止执行 Rebuild** | 本轮 Q7 决策 —— 用执行约束替代推翻重写 |

### 4.3 Phase 0 验收

- [x] `Implement_rules.md` 中 `§12. SpaceLife / Dialogue 模块规则` 写入并通过自查
- [x] Authority Matrix（五层）完整，每行权威入口唯一
- [x] Scene Override 白名单初稿已建（Phase 3 会定稿）
- [x] 7 条设计决策在文档中锁定
- [x] `Docs/0_Plan/ongoing/README.md` 更新，将 04-21 Plan 标记为"被 Master Plan 统摄"

**工期：** 0.5 天 — **Phase 0 完成于 2026-04-22 16:58**

---

## 5. Phase 1 — Domain 层打补丁（含必做 Validator 之一）

**目标：** 把 04-21 Plan 的 Task 1/2/3/4 执行完，并额外补 3 个 Domain 补丁。

### 5.1 沿用 04-21 Plan 的 4 个 Task

直接执行 04-21 Plan 的以下 Task，不重复编写清单；若发现不一致，以本 Master Plan 为准：

- **Task 1**：asmdef + save schema + NpcId —— 见 04-21 Plan §4 Task 1
- **Task 2**：authored data 真相源（含 `DialogueDatabaseValidator` 三个最小测试） —— 见 04-21 Plan §4 Task 2
- **Task 3**：Runner / ConditionEvaluator / EffectExecutor + runner tests —— 见 04-21 Plan §4 Task 3
- **Task 4**：`RelationshipManager` 持久化 + `DialogueFlagStore` tests —— 见 04-21 Plan §4 Task 4

### 5.2 新增 Phase 1 补丁（7 条）

在 Task 1-4 执行过程中/完成后，额外加入：

> **P1-P3**：来自 Q7 决策的原始补丁集合
> **P4-P7**：来自 2026-04-22 全模块审计的高优治理项（方案 B 新增）

#### 补丁 P1：Domain 不持有 UI 接口，改由 Core 定义 `IDialoguePresenter` / `IGiftPresenter`

**文件：**
- Create: `Assets/Scripts/SpaceLife/Dialogue/IDialoguePresenter.cs`
- Create: `Assets/Scripts/SpaceLife/Dialogue/IGiftPresenter.cs`

**动作：**
- [ ] `IDialoguePresenter`：暴露 `ShowNode(DialogueNodeViewModel vm)`、`HideDialogue()`、`event Action<string> OnChoiceSelected`
- [ ] `IGiftPresenter`：暴露 `ShowGiftUI(NpcId npc)`、`HideGiftUI()`、`event Action OnGiftFinished`
- [ ] `DialogueUI` / `GiftUI` 在 Phase 2 重构时实现这两个接口
- [ ] `SpaceLifeDialogueCoordinator`（Phase 3 创建）通过 `ServiceLocator.Get<IDialoguePresenter>()` 获取，不直接引用 `DialogueUI` 类型

**理由：** 避免 Domain / Coordinator 直接耦合具体 UI 实现，为 Phase 5 可能的 UI 替换留活口。

#### 补丁 P2：HubLock 改为引用计数（不是 bool）

**文件：**
- Modify: `Assets/Scripts/SpaceLife/SpaceLifeManager.cs`

**动作：**
- [ ] `SpaceLifeManager` 内部改为 `private int _hubLockCount;`
- [ ] 对外暴露 `AcquireHubLock(object owner)` / `ReleaseHubLock(object owner)`（用 `HashSet<object>` 防重复 acquire / 防 release 不存在的 owner）
- [ ] `_hubLockCount > 0` 时才锁住 `PlayerController2D` / `PlayerInteraction`
- [ ] 输出 `Debug.LogError` 当检测到：release 不存在的 owner / 同一 owner 重复 acquire

**理由：** MVP 只有 1 NPC + 1 Terminal 时 bool 也够用，但 Phase 5 之后 Gift 嵌套、Intel 叠加等场景会让 bool lock 立刻崩溃。引用计数的代价就是一个 `HashSet`，值得在 Phase 1 建好。

#### 补丁 P3：`DialogueDatabaseValidator` 必做档收口

**文件：**
- 强化 Task 2 中已计划的 `DialogueDatabaseTests`

**动作：**
- [ ] 在 `DialogueDatabaseSO` 上增加 `ValidateDatabase()` 方法，返回 `List<string>`（错误列表）
- [ ] 检查项：
  - `GraphId` 唯一
  - `OwnerId` 唯一
  - 每个 `DialogueGraphSO` 内 `NodeId` 唯一
  - `Choice.NextNodeId` / `Node.NextNodeId` / `EntryRule.EntryNodeId` 都能在 `Nodes` 中找到
  - 每个 `Graph` 至少有 1 条 `EntryRule`
- [ ] 在 `DialogueDatabaseTests` 中覆盖以上检查项
- [ ] 提供 Editor 菜单 `ProjectArk > Validate Dialogue Database`，输出错误清单到 Console

**理由：** 对齐 Master Plan §2.2 必做 Validator 档第 1 条；这是 Phase 3 内容 authoring 时防 authoring 手滑的第一道防线。

#### 补丁 P4：抽取 `ShipInputActionLocator` 统一消除 `AssetDatabase.FindAssets` fallback

**背景：** 审计发现 `PlayerController2D.cs` / `PlayerInteraction.cs` / `SpaceLifeInputHandler.cs` 三处重复使用 `#if UNITY_EDITOR` + `AssetDatabase.FindAssets("ShipActions t:InputActionAsset")` 作为 InputActionAsset 的 fallback。这类模糊查找违反 `Implement_rules.md §3.8 "硬编码治理"`。

**文件：**
- Create: `Assets/Scripts/SpaceLife/Editor/ShipInputActionLocator.cs`（Editor-only helper）
- Modify: `Assets/Scripts/SpaceLife/PlayerController2D.cs`
- Modify: `Assets/Scripts/SpaceLife/PlayerInteraction.cs`
- Modify: `Assets/Scripts/SpaceLife/SpaceLifeInputHandler.cs`

**动作：**
- [ ] 抽取 `ShipInputActionLocator.FindShipActionsAsset()`（Editor-only），内部集中维护"如何定位 `ShipActions.inputactions`"的唯一实现
- [ ] 三处 Runtime 消费者通过 `#if UNITY_EDITOR` 调用 Locator，**不得**再自己写 `AssetDatabase.FindAssets`
- [ ] 失败时走 `Debug.LogError` + 明确报错（对齐 `Implement_rules.md §3.5 "禁止 silent no-op"`）

**理由：** 同类违规在 3 个文件重复是典型的"第二真相源"债务。统一后未来 InputActionAsset 路径变更只改 1 处。

#### 补丁 P5：`PlayerInteraction.Interact` 键位迁移到 "SpaceLife" ActionMap

**背景：** 审计发现 `PlayerInteraction.cs` 在 `ShipActions` 的 "Ship" ActionMap 中读取 `Interact` 键，与 SpaceLife 的 "SpaceLife" ActionMap 边界模糊。玩家在 Hub 中按 E 时，输入事件走的是 Ship map 而不是 SpaceLife map，这与 `SpaceLifeInputHandler` 的 map 切换语义不一致。

**文件：**
- Modify: `Assets/Input/ShipActions.inputactions`（在 "SpaceLife" map 中新增 `Interact` action，Binding: `<Keyboard>/e`）
- Modify: `Assets/Scripts/SpaceLife/PlayerInteraction.cs`

**动作：**
- [ ] 在 `ShipActions.inputactions` 的 "SpaceLife" ActionMap 中新增 `Interact` action
- [ ] `PlayerInteraction` 改为从 "SpaceLife" map 读 `Interact`
- [ ] "Ship" map 中原 `Interact` 如无其他消费者则删除；如仍被飞船交互（若有）消费，保留但两边独立
- [ ] 验证 Hub 模式下按 E 仍能触发交互

**理由：** ActionMap 的语义边界应与模式边界一致（Ship 模式读 Ship map、SpaceLife 模式读 SpaceLife map），避免未来 Rebind UI 或多键位冲突时定位困难。

#### 补丁 P6：`MinimapUI` 改 `CanvasGroup` 控显隐（删 `SetActive` 违规）

**背景：** 审计发现 `MinimapUI.cs` 第 32/50/58 行三处 `_minimapPanel.SetActive(...)`，违反项目"uGUI 面板统一用 CanvasGroup"规则（见 CLAUDE.md 常见陷阱表）。虽然历史上未引爆 Awake 推迟陷阱，但仍是类型违规。

**文件：**
- Modify: `Assets/Scripts/SpaceLife/MinimapUI.cs`

**动作：**
- [ ] 给 `_minimapPanel` 所在 GameObject 添加 `CanvasGroup`
- [ ] `Awake()` 中初始化 CanvasGroup（alpha=0 / interactable=false / blocksRaycasts=false），**禁止** `SetActive(false)`
- [ ] Show/Hide 改走 CanvasGroup alpha + interactable + blocksRaycasts
- [ ] 场景中 `_minimapPanel` GameObject 保持常 active

**理由：** 与 `DialogueUIPresenter` / `GiftUIPresenter` 的显隐方式保持一致，避免"SpaceLife 模块内仍有面板用 SetActive"成为新一轮踩坑源。

#### 补丁 P7：`NPCDataSO` Legacy 字段正式声明弃用

**背景：** 审计发现 `NPCDataSO` 中 `DialogueLine[] _dialogueNodes` + `GetNodeAt()` + `GetEntryLine()` + `DefaultEntryIndex` / `FriendlyEntryIndex` / `BestFriendEntryIndex` 是旧原型 API。全项目搜索无任何 Runtime 消费者（`DialogueUI.ShowDialogue` 的 Legacy 走线将在 Phase 2 删除），属于"定义型 API 污染"。

**文件：**
- Modify: `Assets/Scripts/SpaceLife/Data/NPCDataSO.cs`

**动作：**
- [ ] 在 `_dialogueNodes` 字段与 `GetNodeAt` / `GetEntryLine` / 三个 `*EntryIndex` 属性上添加 `[System.Obsolete("Legacy prototype API. Use DialogueGraphSO instead. Will be removed after Phase 2.", false)]`
- [ ] 不在本阶段删除字段，避免存档数据破坏；留到 Phase 4 或后续存档迁移时处理
- [ ] 在字段上方追加明确注释，标注"预计退役时间：Phase 2 完成 + 存档迁移后"

**理由：** 对齐 `Implement_rules.md §3.11 "迁移纪律"`——Legacy 路径必须写清"计划何时删"。`[Obsolete]` 会在使用处产生编译警告，逼迫任何新代码远离 Legacy API。

### 5.3 Phase 1 验收

- [x] 04-21 Plan Task 1-4 所有验收标准通过（含 EditMode tests）
- [x] `IDialoguePresenter` / `IGiftPresenter` 接口定义完成，但暂无实现（Phase 2 实现）
- [x] `SpaceLifeManager.AcquireHubLock / ReleaseHubLock` 可用，bool 版本已废弃
- [x] `DialogueDatabaseSO.ValidateDatabase()` + Editor 菜单可用
- [x] `ShipInputActionLocator` 抽取完成，三处消费者不再各自 `AssetDatabase.FindAssets`
- [x] `PlayerInteraction.Interact` 迁移到 "SpaceLife" ActionMap，Hub 按 E 交互验证通过
- [x] `MinimapUI` 改 `CanvasGroup`，无 `SetActive` 调用
- [x] `NPCDataSO` Legacy 字段与 Legacy 方法全部带 `[Obsolete]` 标注
- [x] `dotnet build Project-Ark.slnx` 通过（允许 `[Obsolete]` 产生的 warning）

**实际完成：2026-04-22 20:40，dotnet build 0 错误通过（28 警告，全部为预存在的 `FindFirstObjectByType` 老 API 警告 + 本次 P7 刻意添加的 `CS0618` Obsolete 警告）。**

**工期：** 1.5-1.8 天（方案 B 在原 1 天基础上 +0.8 天补入审计高优治理项）

---

## 6. Phase 2 — UI Prefab 体系从 0 建 + Presenter 重构

**目标：** 建立全新的 UI Prefab 体系，把 `DialogueUI` / `GiftUI` 重构为 Presenter，并**删除** `NPCInteractionUI`（不再作为兼容层保留）。

### 6.1 UI Prefab 体系设计

#### 6.1.1 Prefab 文件结构

```
Assets/_Prefabs/UI/SpaceLife/
  - DialogueUI.prefab              (顶层 Canvas-root UI，CanvasGroup 控显隐)
  - GiftUI.prefab                  (顶层 Canvas-root UI，CanvasGroup 控显隐)
  - SpaceLifeUIRoot.prefab         (场景级 UI 根节点，持有 DialogueUI / GiftUI 实例)
```

#### 6.1.2 细粒度 + 零 override 约束

- 所有 Prefab 内部结构、尺寸、字体、颜色、CanvasGroup 初始值都由 **唯一一个 Editor 构建器** 生成（首次脚本生成，后续靠 Inspector 手工编辑或重新执行 Builder）
- 场景中 **禁止** override Prefab 的任何字段，除了 §4.1 Scene Override 白名单列出的项
- Prefab 上挂载的 `DialogueUIPresenter` / `GiftUIPresenter` 脚本序列化字段 **全部来自 Prefab 内部子节点**（Canvas / Text / Button / CanvasGroup），不得来自场景

#### 6.1.3 唯一 Builder：`SpaceLifeUIPrefabBuilder`

**文件：**
- Create: `Assets/Scripts/SpaceLife/Editor/SpaceLifeUIPrefabBuilder.cs`

**动作：**
- [ ] Editor 菜单 `ProjectArk > SpaceLife > Build UI Prefabs (Apply)` —— 生成/刷新 3 个 Prefab
- [ ] Editor 菜单 `ProjectArk > SpaceLife > Audit UI Prefabs` —— 只检查，不写入
- [ ] Apply 模式：`AssetDatabase.CreateAsset` + `PrefabUtility.SaveAsPrefabAsset`
- [ ] Audit 模式：列出当前 Prefab 状态与预期是否一致
- [ ] **禁止** 在 `OnValidate` / `Awake` / Play Mode 中隐式触发
- [ ] **禁止** 自动修改场景中的 Prefab 实例

### 6.2 Presenter 重构

#### 6.2.1 `DialogueUI` → `DialogueUIPresenter`

**文件：**
- Modify: `Assets/Scripts/SpaceLife/DialogueUI.cs` → 重命名为 `DialogueUIPresenter.cs`
- Modify: `Assets/Scripts/SpaceLife/ProjectArk.SpaceLife.asmdef`（如需增加依赖）

**动作：**
- [ ] 实现 `IDialoguePresenter`
- [ ] `Awake()`：初始化 `CanvasGroup`（alpha=0 / interactable=false / blocksRaycasts=false），**禁止** `SetActive(false)`
- [ ] `ShowNode(vm)`：设置 speaker / text / choices，然后 `SetVisible(true)`
- [ ] `HideDialogue()`：`SetVisible(false)`
- [ ] 点击 choice 时触发 `OnChoiceSelected?.Invoke(choiceId)`
- [ ] 删除所有直接修改关系值 / 直接打开 GiftUI / 按 index 选下一句的代码
- [ ] `OnDestroy()`：取消所有事件订阅（对齐架构原则 §5 事件卫生）

#### 6.2.2 `GiftUI` → `GiftUIPresenter`

**文件：**
- Modify: `Assets/Scripts/SpaceLife/GiftUI.cs` → 重命名为 `GiftUIPresenter.cs`

**动作：**
- [ ] 实现 `IGiftPresenter`
- [ ] `Awake()`：初始化 `CanvasGroup`，**禁止** `SetActive(false)`
- [ ] `ShowGiftUI(npc)` / `HideGiftUI()` 用 CanvasGroup 控显隐
- [ ] Gift 操作完成后触发 `OnGiftFinished?.Invoke()`

#### 6.2.3 `NPCInteractionUI` 删除（不是兼容保留）

**文件：**
- Delete: `Assets/Scripts/SpaceLife/NPCInteractionUI.cs`
- Delete: `Assets/Scripts/SpaceLife/NPCInteractionUI.cs.meta`
- 清理场景中所有引用

**理由：** 04-21 Plan 原计划保留为兼容层，但 Master Plan §4 Q7 决策 D6 要求"从 0 建 UI Prefab 体系"，保留旧 UI 会直接破坏"零 override"和"唯一 UI owner"。删比改干净。

### 6.3 UI Prefab Override Audit（必做 Validator 档第 2 条）

**文件：**
- Create: `Assets/Scripts/SpaceLife/Editor/SpaceLifeUIOverrideAudit.cs`

**动作：**
- [ ] Editor 菜单 `ProjectArk > SpaceLife > Audit UI Prefab Overrides`
- [ ] 遍历 `SampleScene` 中所有 `DialogueUIPresenter` / `GiftUIPresenter` 实例
- [ ] 用 `PrefabUtility.GetPropertyModifications` 检查 override 列表
- [ ] 白名单内的项（见 §4.1）允许，白名单外的项输出 `Debug.LogError`

### 6.4 Phase 2 验收

- [x] `SpaceLifeUIPrefabBuilder` 已实现 Apply + Audit 双模式，生成目标为 `Assets/_Prefabs/UI/SpaceLife/` 下 3 个 Prefab（**代码层完成，待 Editor 中 Apply 执行验证 Prefab 实际落盘**）
- [x] `DialogueUIPresenter` / `GiftUIPresenter` 实现 `IDialoguePresenter` / `IGiftPresenter`
- [x] `NPCInteractionUI.cs` 已删除，场景无残留引用
- [x] `DialogueUIPresenter.Awake()` 无 `SetActive(false)`
- [x] `SpaceLifeUIOverrideAudit` 菜单已实现（`ProjectArk > Space Life > Audit UI Prefab Overrides`），白名单仅放行 `m_SortingOrder` + `m_Name`（**代码层完成，待 Editor 中对当前场景运行验证无非法 override**）
- [x] `dotnet build Project-Ark.slnx` 通过（0 错误，新代码 0 新警告）

**工期：** 2-3 天

---

## 7. Phase 3 — 接线 + Scene 样板 + SO 占位 + 必做 Validator 收尾

**目标：** 让"玩家在 SampleScene 按 E 就能跟 NPC / Terminal 对话"真正可玩。

### 7.1 沿用 04-21 Plan Task 6（NPC / Terminal 接线）

直接执行 04-21 Plan §4 Task 6，但做以下**适配调整**：

| 04-21 Plan 中的做法 | Master Plan 调整 |
|---|---|
| `SpaceLifeDialogueCoordinator` 持有 `DialogueUI` 类型引用 | 改为通过 `ServiceLocator.Get<IDialoguePresenter>()` 获取 |
| `NPCController` 改为委托 Coordinator | 保持不变 |
| `TerminalDialogueInteractor` 新建 | 保持不变 |
| "`OnInteract()` 不再优先走 `NPCInteractionUI`" | 升级为："`NPCInteractionUI` 已删除，不存在此选项" |

### 7.2 沿用 04-21 Plan Task 7（SO 内容 + Scene 样板）

直接执行 04-21 Plan §4 Task 7，但做以下**适配调整**：

| 04-21 Plan 中的做法 | Master Plan 调整 |
|---|---|
| `DialogueDatabase.asset` + 2 份 graph | 保持不变，但 graph 内容先做**空骨架**（每条 path 1-2 句占位文本），让 Phase 3 重点在"接线通 + Validator 过"，而非"内容填充" |
| 场景中拖 `DialogueUI` 实例 | 改为：从 `Assets/_Prefabs/UI/SpaceLife/DialogueUI.prefab` 拖入实例 |

### 7.3 新增 Phase 3 补丁：Coordinator Dependency Audit（必做 Validator 档第 3 条）

**文件：**
- Modify: `Assets/Scripts/SpaceLife/Dialogue/SpaceLifeDialogueCoordinator.cs`

**动作：**
- [ ] `Awake()` 中按顺序 Resolve：`IDialoguePresenter` / `IGiftPresenter` / `DialogueFlagStore` / `RelationshipManager` / `DialogueDatabaseSO` / `WorldProgressManager`
- [ ] 任一 Resolve 失败 → `Debug.LogError` 列出缺失项 + 禁用自身（`enabled = false`），不允许 silent 启动
- [ ] 成功后 Register 自身到 `ServiceLocator`
- [ ] `OnDestroy()` 中 `Unregister`

**对齐：** Implement_rules.md §3.5 "禁止 silent no-op"

### 7.4 Scene Override 白名单定稿

完成 Scene 样板后，回头更新 Phase 0 §4.1 的 Scene Override 白名单初稿为正式版本，并把"禁止项"清单写进 Implement_rules.md。

### 7.5 Phase 3 验收

- [ ] `SampleScene` 中存在 1 NPC + 1 Terminal 样板，按 E 能进入对话
- [ ] `DialogueDatabase.asset` 含 2 份 graph（空骨架也算通过）
- [ ] `SpaceLifeDialogueCoordinator.Awake()` 缺失依赖时报 `Debug.LogError`（手工测试：把任一引用拖空）
- [ ] `ProjectArk > Validate Dialogue Database` 菜单运行无错
- [ ] `ProjectArk > SpaceLife > Audit UI Prefab Overrides` 菜单运行无错
- [ ] Scene Override 白名单定稿写入 Implement_rules.md
- [ ] Play Mode 手工走通：NPC 对话分支、Terminal 条件选项、`OpenGift` 进入 GiftUI、flag 写入恢复、关系值存档恢复
- [ ] `dotnet build Project-Ark.slnx` 通过

**工期：** 1-1.5 天

---

## 8. Phase 4 — 文档收口 + 旧 Plan 归档 + SetupWindow 冻结声明

**目标：** 把本轮做成的事写进技术真相源，把旧 Plan 归档，并用执行约束替代 SetupWindow 重写。

### 8.1 文档收口

**文件：**
- Modify: `Docs/2_TechnicalDesign/SpaceLife/SpaceLife_HubDialogue_SystemDesign.md`
- Modify: `Docs/5_ImplementationLog/ImplementationLog.md`
- Modify: `Implement_rules.md`（§12 SpaceLife / Dialogue 模块）

**动作：**
- [ ] 把实际落地的 runtime 文件名 / save 方案 / `IDialoguePresenter` 接口路径 / `SpaceLifeUIPrefabBuilder` 口径写回设计稿，让它成为现役真相源
- [ ] `ImplementationLog.md` 追加本轮所有变更（按 Phase 分段，每 Phase 一段）
- [ ] `Implement_rules.md` §12 补充"踩坑总结"（至少写入 "`SetActive(false)` 初始化顺序坑" + "UI owner 双轨" + "Silent fail"）
- [ ] `Implement_rules.md` §12 补充"Phase 4 验收"条款

### 8.2 旧 Plan 归档

**动作：**
- [ ] 将 `Docs/0_Plan/ongoing/2026-04-21-spacelife-hub-dialogue-mvp-implementation-plan.md` 移动到 `Docs/0_Plan/complete/`
- [ ] 将 `Docs/0_Plan/ongoing/2026-04-22-spacelife-hub-dialogue-master-plan.md`（本文）也移动到 `Docs/0_Plan/complete/`
- [ ] 更新 `Docs/0_Plan/ongoing/README.md`

### 8.3 SetupWindow 冻结声明

**文件：**
- Modify: `Implement_rules.md` §12
- Modify: `Assets/Scripts/SpaceLife/Editor/SpaceLifeSetupWindow.cs` 顶部注释

**动作：**
- [ ] `Implement_rules.md` §12 加入执行约束：
  > **`SpaceLifeSetupWindow` 在 Phase 1-3 期间冻结。禁止执行 Rebuild 操作。若 Phase 5 未启动，此冻结状态长期生效，直至 Phase 5 完成推翻重写。**
- [ ] `SpaceLifeSetupWindow.cs` 顶部加入代码注释：
  ```csharp
  // ⚠️ FROZEN until Phase 5 (Master Plan 2026-04-22 §9). DO NOT RUN REBUILD.
  // UI Prefab authority is now owned by SpaceLifeUIPrefabBuilder.
  // See Implement_rules.md §12 for details.
  ```
- [ ] 在 `SpaceLifeSetupWindow.OnGUI()` 最顶部加一个红色 `EditorGUILayout.HelpBox`，警告用户"此工具已冻结，正式 UI 构建请使用 `ProjectArk > SpaceLife > Build UI Prefabs`"

### 8.4 Phase 4 验收

- [ ] 设计稿已同步到现役真相源
- [ ] ImplementationLog 完整
- [ ] 旧 Plan + 本 Master Plan 归档
- [ ] SetupWindow 冻结声明（Implement_rules + 代码注释 + Editor 警告）三处全部就位

**工期：** 0.5 天

---

## 8.5 审计发现的额外治理项（方案 B 选择依据）

> 本节记录 2026-04-22 对 SpaceLife 模块完整审计后，采纳"方案 B：Master Plan + 4 项高优遗漏"的决策依据与权衡。

### 背景

Master Plan v1.0（Q7 决策版）落定后，在进入 Phase 0 前，对整个 SpaceLife 模块执行了一次完整审计。审计覆盖 Runtime / Editor / Data / Tests 共 ~42 个文件 ~6,500 行代码，结论摘要：

- **Dialogue 子系统（Runner/Coordinator/Router/Evaluator）架构达标，不需要重构**
- **SpaceLifeManager / RoomManager / RelationshipManager / GiftInventory 健康**
- **Data 层（DialogueDatabaseSO / DialogueGraphSO）是项目 SO 标杆**
- 主要债务集中在 UI Presenter 层、Editor Bootstrap 工具链
- **治理成熟度（按 Implement_rules.md 13 项条款对照）约 15%**

审计结论与 Master Plan v1.0 的 6 必做 + 2 降级方案**高度吻合**，但发现 4 项 Master Plan 未明确覆盖、但属于高优债务的治理项。

### 4 项高优遗漏（对应 Phase 1 补丁 P4-P7）

| # | 债务 | 工期 | 对应补丁 | 违反规则 |
|---|---|---|---|---|
| 1 | `AssetDatabase.FindAssets("ShipActions t:InputActionAsset")` 在 3 个文件重复 | 0.3d | P4 | §3.8 硬编码治理 |
| 2 | `PlayerInteraction.Interact` 从 "Ship" ActionMap 读键（跨 map 读键） | 0.2d | P5 | ActionMap 语义边界模糊 |
| 3 | `MinimapUI` 使用 `SetActive` 控显隐（3 处） | 0.2d | P6 | CanvasGroup 统一规则 |
| 4 | `NPCDataSO` Legacy 字段未声明弃用 | 0.1d | P7 | §3.11 迁移纪律 |

**合计新增工期：+0.8 天**（Phase 1 工期由 1 天升至 1.5-1.8 天）

### 方案对比（四选一）

| 方案 | 工期 | 取舍 |
|---|---|---|
| A：按 Master Plan v1.0 原样推进 | 5-6d | 保守，但审计发现的 4 项高优遗漏留到 Future Work |
| **B：Master Plan + 4 项高优遗漏** | **6-7d** | **一次性清理审计发现全部高优问题，性价比最高** |
| C：扩大到全模块硬编码治理（含 Debug.Log 降级 / Camera fallback 统一 / `_saveSlot` 抽取） | 8-10d | 过度治理，收益递减 |
| D：推翻重构整个 SpaceLife | 15-20d | Dialogue 子系统达标 → 纯破坏价值 |

### 选择方案 B 的 4 条理由

1. **边际成本极低，边际收益高**：+0.8d 解决 3 个违规文件 + 1 个 ActionMap 语义债 + 1 个 SO Legacy 警告，比 Phase 5 可选档（1-2d）性价比更高
2. **与 Phase 1 Domain 层打补丁天然契合**：P4-P7 全部是"Domain/Runtime 基础设施补丁"，属于 Phase 1 的职责范围
3. **避免"治理半途"**：若推迟到 Future Work，审计报告的结论会随时间流失；趁热打铁一次性清理
4. **不破坏 Master Plan 主节奏**：Phase 2-5 的工期与目标完全不变，只在 Phase 1 局部扩容

### 方案 B 不吸收的项（接受现状或留 Future Work）

**审计发现的中优遗漏（不进 Master Plan）：**
- `SpaceLifeManager` 散落的 `Debug.Log` 降级（~20 处）
- `SpaceLifeInputHandler` 的 Debug.Log 清理
- `_saveSlot` 三处硬编码统一到 `SaveSlotContext`

**审计发现的低优遗漏（接受现状）：**
- MB 集成测试零覆盖（MVP 阶段可忍）
- `Camera.main` fallback（项目通病，非 SpaceLife 专属）

这些项不影响 Master Plan Done 判定，留到未来独立 Plan 处理。

---

## 9. Phase 5（可选） — SetupWindow 推翻重写 + Silent Fail 全扫

**触发条件：** Phase 1-4 完成且**仍有预算时间**时才启动。若时间不够，本 Phase 直接砍。

### 9.1 SetupWindow 推翻重写

**目标：** 把 `SpaceLifeSetupWindow` 改造为 Audit / Preview / Apply 三模式明确分离，对齐 Implement_rules.md §3.14 "Builder 执行模式规则"。

**动作：**
- [ ] 拆分为 3 个菜单：`Audit` / `Preview` / `Apply`，不再使用 "Rebuild" 含糊命名
- [ ] 所有写入操作只能在 `Apply` 模式下发生
- [ ] `Audit` / `Preview` 模式输出"将会改哪些字段"的预览列表
- [ ] 移除所有 silent no-op 分支，缺依赖必报 `Debug.LogError`
- [ ] 解除 Phase 4 的冻结声明

**工期：** 1 天

### 9.2 Silent Fail 全项目扫描

**目标：** 建立一个 Editor 工具遍历 SpaceLife 模块所有 `if (x == null) return;` 形态代码，输出清单供人工审查。

**动作：**
- [ ] Create: `Assets/Scripts/SpaceLife/Editor/SilentFailScanner.cs`
- [ ] 用 Roslyn 或 Regex 扫描 `*.cs` 文件
- [ ] 输出"可疑 silent return"清单（文件+行号）
- [ ] 人工审查后对每条决定：改为 `LogError` / 添加注释说明为何合法 / 保持不动

**工期：** 1 天

### 9.3 Phase 5 验收

- [ ] SetupWindow 三模式分离，冻结声明解除
- [ ] Silent Fail 扫描清单输出并人工审查过
- [ ] Implement_rules.md §12 更新 SetupWindow 新状态

**工期：** 1-2 天

---

## 10. 风险与防御

### 风险 1：Phase 2 UI Prefab Builder 本身变成新的 silent-fail 源

**症状：** Apply 模式跑完，Prefab 结构看似正确，但某字段没写入（被 Unity 序列化系统吃掉）。  
**防御：** Phase 2 §6.1.3 要求 `SpaceLifeUIPrefabBuilder.Audit` 模式在 Apply 之前/之后都能跑，且 Audit 检测到任何差异都要 `Debug.LogError`。

### 风险 2：Phase 3 接线时场景 Prefab 实例被意外 override

**症状：** 从 Prefab 拖入场景后，Unity 自动把某些字段标为 override（如 RectTransform 尺寸）。  
**防御：** Phase 3 §7.5 验收时必须跑 `Audit UI Prefab Overrides`，白名单外的 override 一律清回。

### 风险 3：Phase 1 补丁 P1（IPresenter 接口）未能让 Phase 2 Presenter 真正解耦

**症状：** `DialogueUIPresenter` 虽然实现了 `IDialoguePresenter`，但内部仍然直接调用 `GiftUI` 类型，导致 Phase 5 替换 UI 时仍需改 Presenter。  
**防御：** Phase 2 §6.2.1 代码审查时用 `Find References` 确认 `DialogueUIPresenter` 不直接引用任何其他 Presenter 的具体类型，只走 `IGiftPresenter` / `DialogueServiceRouter`。

### 风险 4：Phase 4 SetupWindow 冻结声明被忽视

**症状：** 有人（AI 或人）在 Phase 4 后仍然运行 SetupWindow.Rebuild，破坏 Phase 2 建立的 UI Prefab authority。  
**防御：** Phase 4 §8.3 三处声明（Implement_rules + 代码注释 + Editor 警告 HelpBox）必须全部就位，缺一不可。

### 风险 5：Phase 5 被无限延后，SetupWindow 长期冻结但代码仍在

**症状：** Phase 4 结束后，Phase 5 因优先级排队一直没做，`SpaceLifeSetupWindow.cs` 变成"僵尸工具"。  
**防御：** Phase 4 验收时明确写入"冻结状态长期合法"（不是"临时"），接受它作为稳定状态；若未来真的要做 Phase 5，再启动新 Master Plan。**"长期冻结"不是失败，是合理结果。**

---

## 11. 执行顺序总览

严格按以下顺序执行，不要跳步：

```
Phase 0 (0.5d)
   ↓ Authority Matrix + 7 设计决策锁定
Phase 1 (1.5-1.8d)
   ↓ Domain 层：Task 1-4 + 7 补丁 (P1-P3 治理 + P4-P7 审计遗漏) + Validator 第 1 条
Phase 2 (2-3d)
   ↓ UI Prefab 从 0 建 + Presenter 重构 + 删 NPCInteractionUI + Validator 第 2 条
Phase 3 (1-1.5d)
   ↓ 接线 + Scene 样板 + SO 占位 + Validator 第 3 条
Phase 4 (0.5d)
   ↓ 文档收口 + 归档 + SetupWindow 冻结声明
Phase 5 (可选, 1-2d)
   SetupWindow 推翻重写 + Silent Fail 全扫
```

**必做合计：** 6-7 天（方案 B）
**含 Phase 5：** 7-9 天

---

## 12. 最终完成标志（Master Plan Done）

当且仅当以下 **11 条** 全部达成，才视为 Master Plan 完成（方案 B 定稿）：

### 功能线（5 条）

- [ ] `SampleScene` 中 1 NPC + 1 Terminal 可玩
- [ ] 对话分支能正确响应 `WorldStage` / `RelationshipValue` / `Flags`
- [ ] `OpenGift` 能进入 `GiftUIPresenter`，其他 3 个 service exit 通过 Router 留下可接线占位
- [ ] 关系值与对话 flag 跨存档恢复
- [ ] `DialogueUIPresenter` / `GiftUIPresenter` 用 `CanvasGroup` 控显隐，GameObject 始终 active

### 治理线（6 条）

- [ ] Implement_rules.md §12 SpaceLife / Dialogue 模块规则已写入，含 Authority Matrix 五层 + Scene Override 白名单 + 踩坑总结
- [ ] 3 个必做 Validator（`DialogueDatabaseValidator` / `UI Prefab Override Audit` / `Coordinator Dependency Audit`）全部可用
- [ ] `NPCInteractionUI` 已删除，UI owner 唯一
- [ ] `SpaceLifeSetupWindow` 冻结声明三处就位
- [ ] 旧 04-21 Plan + 本 Master Plan 归档到 `Docs/0_Plan/complete/`
- [ ] **审计高优 4 项治理完成**：`ShipInputActionLocator` 抽取 / `PlayerInteraction` 键位迁移到 "SpaceLife" map / `MinimapUI` 改 `CanvasGroup` / `NPCDataSO` Legacy 字段 `[Obsolete]` 标注（方案 B 新增）

---

## 13. 后续工作（Master Plan 完成后）

本 Master Plan 完成后，以下工作可开启独立 Plan：

- `CSV -> SO` importer
- 终端 / AI 与 NPC 的表现 profile 分离
- 远程通讯入口
- 关卡内短对话入口
- disabled choice affordance
- 本地化 / text key
- Phase 5 SetupWindow 重写（若未在本轮执行）
- Silent Fail 全项目扫描推广至其他模块

在这之前，**不要**把 Hub 对话 MVP 扩成半套叙事工具链。

---

## 14. 变更日志

| 日期 | 版本 | 变更 |
|---|---|---|
| 2026-04-22 | v1.0 | Master Plan 创建，Q7 决策采用"6 必做 + 2 降级"方案（必做合计 5-6d，含 Phase 5: 6-8d） |
| 2026-04-22 | v1.1 | 采纳方案 B：在 v1.0 基础上吸收 2026-04-22 SpaceLife 模块完整审计发现的 4 项高优遗漏（P4-P7）。Phase 1 工期 1d → 1.5-1.8d，必做合计 5-6d → **6-7d**，含 Phase 5: 6-8d → **7-9d**。最终完成标志治理线由 5 条增至 6 条。详见 §8.5。 |
