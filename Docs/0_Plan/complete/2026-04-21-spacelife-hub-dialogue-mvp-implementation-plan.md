# SpaceLife Hub Dialogue MVP Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 构建一个只作用于 `SpaceLife` 飞船内部 Hub 的对话 MVP：支持 1 个船员 NPC + 1 个终端 / AI、世界进度 / 关系 / flag 条件、先对话再进服务，以及 `SO` authoring 到未来 `CSV -> SO` 的稳定升级缝。

**Architecture:** 新增 `Assets/Scripts/SpaceLife/Dialogue/` 作为独立 `DialogueDomain`，由它负责 graph 数据、入口解析、条件判断、effect 执行和统一 `ServiceExit`。`SpaceLife` 现有的 `DialogueUI` / `GiftUI` / `NPCController` 只做 Hub 交互与表现接入，不再自己按 index 跳节点或直接改关系；关系值与对话 flags 写回现有 `PlayerSaveData`，但不把 `SaveBridge` 反向耦进 `SpaceLife`。

**Tech Stack:** Unity 6000.3.7f1、C#、`ProjectArk.SpaceLife`、`ProjectArk.Level`、`ProjectArk.Core.Save`、`ServiceLocator`、`UniTask`、uGUI `CanvasGroup`、NUnit EditMode tests、`SampleScene`

---

## 1. MVP 范围与完成标准

### 1.1 这一版必须做到

- 只在 `SpaceLife` 飞船内部 Hub 工作，不扩到关卡内短对话或远程通讯
- 玩家与 **1 个船员 NPC** 交互时，直接进入对话，而不是先弹 `交谈 / 送礼 / 离开` 菜单
- 玩家与 **1 个终端 / AI** 交互时，走同一 `DialogueDomain`，只是表现样式不同
- 对话支持：
  - 世界进度 gating
  - 关系值 gating
  - 一次性 flag gating
  - “先聊 → 追问 → 进入服务” 的中等分支
- 对话内服务出口至少支持：
  - `OpenGift`
  - `OpenUpgrade`
  - `OpenIntel`
  - `TriggerRelationshipEvent`
- 关系值与对话 flag 能跨读档恢复
- authoring 先使用 `SO`，但 runtime 不再依赖数组 index

### 1.2 这一版明确不做

- 不做节点编辑器
- 不做 CSV 导入器
- 不做 PlayMode 自动化测试
- 不做完整任务系统
- 不做大量 NPC 铺量
- 不做复杂布尔表达式 / DSL
- 不让对话节点直接操作世界物件或场景开关

### 1.3 完成标准（满足以下 7 条视为功能完成）

- [ ] 船员 NPC 与终端 / AI 都能走统一 `DialogueDomain`
- [ ] graph 通过稳定 `OwnerId` + `NodeId` 跳转，不再使用数组 index
- [ ] 对话入口和选项能正确响应 `WorldStage / Relationship / Flags`
- [ ] 关系值变化和一次性台词状态能保存并在重新进场 / 读档后恢复
- [ ] `DialogueUI` 与 `GiftUI` 改为 `CanvasGroup` 控显隐，GameObject 保持 active
- [ ] `OpenGift` 真能进入现有 `GiftUI`；`OpenUpgrade / OpenIntel / TriggerRelationshipEvent` 至少能通过统一 `DialogueServiceRouter` 跳出并留下可接线入口
- [ ] `SampleScene` 中存在 1 个 NPC + 1 个终端的可玩样板切片

---

## 2. 文件落点与职责

### 2.1 新增运行时代码

- `Assets/Scripts/SpaceLife/Dialogue/DialogueEnums.cs`
  - `DialogueOwnerType`、`DialogueNodeType`、`DialogueConditionType`、`DialogueCompareOp`、`DialogueEffectType`、`DialogueServiceExitType`

- `Assets/Scripts/SpaceLife/Dialogue/DialogueGraphSO.cs`
  - 顶层 graph 资产，持有 `GraphId`、`OwnerId`、`EntryRules`、`Nodes`

- `Assets/Scripts/SpaceLife/Dialogue/DialogueEntryRuleData.cs`
  - graph 入口规则：入口 node、优先级、条件集合

- `Assets/Scripts/SpaceLife/Dialogue/DialogueNodeData.cs`
  - 单节点 authored data

- `Assets/Scripts/SpaceLife/Dialogue/DialogueChoiceData.cs`
  - 单选项 authored data

- `Assets/Scripts/SpaceLife/Dialogue/DialogueConditionData.cs`
  - 条件描述数据

- `Assets/Scripts/SpaceLife/Dialogue/DialogueEffectData.cs`
  - effect 描述数据

- `Assets/Scripts/SpaceLife/Dialogue/DialogueDatabaseSO.cs`
  - graph 数据库，按 `OwnerId` 查 graph；避免场景里每个对象各自拖一份 graph

- `Assets/Scripts/SpaceLife/Dialogue/DialogueContext.cs`
  - 运行时上下文快照：`OwnerId`、`WorldStage`、`RelationshipValue`、`Flags`

- `Assets/Scripts/SpaceLife/Dialogue/DialogueNodeViewModel.cs`
  - UI 消费的只读显示模型：speaker、text、choices、service affordance

- `Assets/Scripts/SpaceLife/Dialogue/DialogueServiceExit.cs`
  - 统一出口结果对象

- `Assets/Scripts/SpaceLife/Dialogue/DialogueFlagStore.cs`
  - 对 `PlayerSaveData.Progress.Flags` 做读写与 key 管理，避免各处手写遍历

- `Assets/Scripts/SpaceLife/Dialogue/DialogueConditionEvaluator.cs`
  - 纯逻辑条件判断

- `Assets/Scripts/SpaceLife/Dialogue/DialogueEffectExecutor.cs`
  - 执行 `SetFlag / ClearFlag / AddRelationship / EmitServiceExit / EndDialogue`

- `Assets/Scripts/SpaceLife/Dialogue/DialogueRunner.cs`
  - graph 入口解析、节点切换、选项决议、session state 持有者

- `Assets/Scripts/SpaceLife/Dialogue/DialogueServiceRouter.cs`
  - 统一处理 `DialogueServiceExit`
  - `OpenGift` → 现有 `GiftUI`
  - `OpenUpgrade / OpenIntel / TriggerRelationshipEvent` → UnityEvent / stub hook

- `Assets/Scripts/SpaceLife/Dialogue/SpaceLifeDialogueCoordinator.cs`
  - Hub 层总控：组装 context、启动 runner、驱动 UI、锁定输入、接收 service exit

- `Assets/Scripts/SpaceLife/TerminalDialogueInteractor.cs`
  - 终端 / AI 交互入口，复用 `Interactable`

### 2.2 新增测试文件

- `Assets/Scripts/SpaceLife/Tests/ProjectArk.SpaceLife.Tests.asmdef`
  - `SpaceLife` EditMode test assembly

- `Assets/Scripts/SpaceLife/Tests/DialogueDatabaseTests.cs`
  - ownerId 查 graph、缺 graph 报错、重复 ownerId 守卫

- `Assets/Scripts/SpaceLife/Tests/DialogueRunnerTests.cs`
  - 入口规则选择、node 跳转、条件 gating、service exit

- `Assets/Scripts/SpaceLife/Tests/DialogueFlagStoreTests.cs`
  - flag 读写、重复 key 覆盖、clear 行为

- `Assets/Scripts/SpaceLife/Tests/RelationshipManagerPersistenceTests.cs`
  - 关系值保存/恢复、默认起始关系、按 `NpcId` roundtrip

### 2.3 计划修改的现有代码

- `Assets/Scripts/SpaceLife/ProjectArk.SpaceLife.asmdef`
  - 增加 `ProjectArk.Level` 引用，让 `SpaceLifeDialogueCoordinator` 能读 `WorldProgressManager`

- `Assets/Scripts/Core/Save/SaveData.cs`
  - 在 `ProgressSaveData` 中新增关系值持久化结构

- `Assets/Scripts/SpaceLife/Data/NPCDataSO.cs`
  - 新增稳定 `NpcId`
  - 保留旧 flat list 字段作为迁移窗口，但不再作为新系统主 authoring 宿主

- `Assets/Scripts/SpaceLife/RelationshipManager.cs`
  - 改为按 `NpcId` 持久化关系值
  - 加载 / 保存到 `PlayerSaveData`

- `Assets/Scripts/SpaceLife/NPCController.cs`
  - 不再自己按关系值选 `DialogueLine`
  - 改为把 `NpcId + NPCDataSO + 自身引用` 交给 `SpaceLifeDialogueCoordinator`

- `Assets/Scripts/SpaceLife/DialogueUI.cs`
  - 重构成新 presenter，但保留现有组件入口，减少场景重绑成本
  - 改为 `CanvasGroup` 控显隐，接收 `DialogueNodeViewModel`

- `Assets/Scripts/SpaceLife/GiftUI.cs`
  - 改为 `CanvasGroup` 控显隐
  - 继续作为 `OpenGift` 的具体服务 UI

- `Assets/Scripts/SpaceLife/SpaceLifeManager.cs`
  - 增加对话模态锁 API，统一锁定 `PlayerController2D` / `PlayerInteraction`

### 2.4 计划创建 / 修改的 authored data 与场景

- Create asset: `Assets/_Data/SpaceLife/Dialogue/DialogueDatabase.asset`
- Create asset: `Assets/_Data/SpaceLife/Dialogue/Graphs/NPC_Engineer_HubDialogue.asset`
- Create asset: `Assets/_Data/SpaceLife/Dialogue/Graphs/Terminal_ShipAI_HubDialogue.asset`
- Modify scene: `Assets/Scenes/SampleScene.unity`
  - 放入 1 个船员 NPC 样板
  - 放入 1 个终端 / AI 样板
  - 放入新的 `SpaceLifeDialogueCoordinator` / `DialogueServiceRouter`

### 2.5 本轮不应该动的文件

- `Assets/Scripts/Level/SaveBridge.cs`
  - 避免引入 `Level -> SpaceLife` 反向依赖

- `Assets/Scripts/Level/Room/RoomFlagRegistry.cs`
  - Hub 对话 flags 不走 room flag 语义

- `Assets/Scripts/SpaceLife/NPCInteractionUI.cs`
  - 本轮不再作为正式入口；保留作旧原型兼容，不继续扩功能

---

## 3. 关键设计决策（开工前先钉死）

### 3.1 为什么保留 `DialogueUI` 组件名而不是新建第二套面板 owner

因为 `SpaceLife` 现有 setup 工具和场景入口已经认 `DialogueUI`。本轮应复用这个挂点并重构其职责，而不是在场景里再加一个“看起来更先进”的第二套对话面板，造成双轨 UI authority。

### 3.2 为什么 `RelationshipManager` 直接写 `SaveManager`，而不是改 `SaveBridge`

因为 `SaveBridge` 在 `ProjectArk.Level` 程序集里，而 `RelationshipManager` 在 `ProjectArk.SpaceLife`。如果让 `SaveBridge` 直接读取 `RelationshipManager`，就会把 `Level` 反向耦到 `SpaceLife`。本轮更稳的做法是：

- `SaveData` 扩充关系值字段
- `RelationshipManager` 自己负责从同一份 `PlayerSaveData` 读写关系值
- `SaveBridge.SaveAll()` 只要不清空这个字段，就会自然保留关系数据

### 3.3 为什么 `Upgrade / Intel / RelationshipEvent` 用 `DialogueServiceRouter`，而不是直接在节点里 `OpenXXXUI()`

因为我们这轮在做对话系统，不是在同时实现升级、情报和关系事件 UI。用统一 router 可以保证：

- 对话系统只产出结果，不直接持有别的系统逻辑
- `Gift` 先接现有 `GiftUI`
- 其他服务通过 `UnityEvent` 或 stub 先留稳定出口，不阻塞 Hub 对话闭环

### 3.4 为什么新 graph 不继续复用 `NPCDataSO.DialogueNodes`

因为那个结构天生是：

- index-based
- NPC 专属
- 不适用于 terminal / AI
- 选项 effect 只有 relationship delta

本轮正确方向是：

- `NPCDataSO` 继续持有角色基础资料、礼物偏好、`NpcId`
- 正式对话内容迁到 `DialogueGraphSO`

### 3.5 为什么 MVP 默认隐藏不满足条件的选项，而不是做 disabled reason

因为 disabled UI 需要额外的锁定文案、颜色规范、可读性与键盘导航逻辑。MVP 先隐藏不满足条件的 choice，减少第一版 presenter 复杂度；后续如果确实需要“我知道你现在还不能选”，再补 disabled affordance。

---

## 4. 分任务实施顺序

### Task 1: 先搭好程序集、测试装配与保存骨架

**Files:**
- Modify: `Assets/Scripts/SpaceLife/ProjectArk.SpaceLife.asmdef`
- Create: `Assets/Scripts/SpaceLife/Tests/ProjectArk.SpaceLife.Tests.asmdef`
- Modify: `Assets/Scripts/Core/Save/SaveData.cs`
- Modify: `Assets/Scripts/SpaceLife/Data/NPCDataSO.cs`

- [ ] 给 `ProjectArk.SpaceLife.asmdef` 增加 `ProjectArk.Level` 引用，只允许 **单向** `SpaceLife -> Level`
- [ ] 新建 `ProjectArk.SpaceLife.Tests.asmdef`，引用：
  - `ProjectArk.SpaceLife`
  - `ProjectArk.Core`
  - `ProjectArk.Level`
  - `UnityEngine.TestRunner`
  - `UnityEditor.TestRunner`
- [ ] 在 `ProgressSaveData` 中新增 `RelationshipValues`（或等价命名）的持久化列表结构；元素至少包含：`NpcId`、`Value`
- [ ] 在 `NPCDataSO` 中新增稳定 `NpcId`，并加最小防御：为空时在 inspector / runtime 输出错误，不允许继续依赖 `NpcName`
- [ ] 保留旧 `DialogueNodes` / `EntryIndex` 字段不删，但在注释中明确：**旧原型兼容用，不再作为新系统主链**
- [ ] 先跑一次完整编译，确认 asmdef 调整没有引入循环引用

**Run:** `cd /Users/dada/Documents/GitHub/Project-Ark && dotnet build Project-Ark.slnx`

**Expected:** `Build succeeded.`

**完成标志：** `SpaceLife` 新测试装配能编译，save schema 里已经有关系值落点，NPC 拥有稳定 ID。

---

### Task 2: 建立 `DialogueDomain` 的 authored data 真相源

**Files:**
- Create: `Assets/Scripts/SpaceLife/Dialogue/DialogueEnums.cs`
- Create: `Assets/Scripts/SpaceLife/Dialogue/DialogueGraphSO.cs`
- Create: `Assets/Scripts/SpaceLife/Dialogue/DialogueEntryRuleData.cs`
- Create: `Assets/Scripts/SpaceLife/Dialogue/DialogueNodeData.cs`
- Create: `Assets/Scripts/SpaceLife/Dialogue/DialogueChoiceData.cs`
- Create: `Assets/Scripts/SpaceLife/Dialogue/DialogueConditionData.cs`
- Create: `Assets/Scripts/SpaceLife/Dialogue/DialogueEffectData.cs`
- Create: `Assets/Scripts/SpaceLife/Dialogue/DialogueDatabaseSO.cs`
- Create: `Assets/Scripts/SpaceLife/Tests/DialogueDatabaseTests.cs`

- [ ] 先写 `DialogueDatabaseTests`，覆盖 3 个最小行为：
  - 按 `OwnerId` 能取到 graph
  - 重复 `OwnerId` 会报错 / 拒绝
  - graph 中缺失 `NodeId` 时能被 guard 抓住
- [ ] `DialogueGraphSO` 顶层字段至少包含：`GraphId`、`OwnerId`、`OwnerType`、`EntryRules`、`Nodes`
- [ ] `DialogueNodeData` 用 `NodeId`，不允许再保留 `NextLineIndex`
- [ ] `DialogueChoiceData` 字段至少包含：`ChoiceId`、`RawText`、`Conditions`、`NextNodeId`、`Effects`、`ExitType`、`ExitPayload`
- [ ] `DialogueConditionData` 先只支持：
  - `WorldStage`
  - `RelationshipValue`
  - `FlagPresent`
  - `FlagAbsent`
- [ ] `DialogueEffectData` 先只支持：
  - `SetFlag`
  - `ClearFlag`
  - `AddRelationship`
  - `EmitServiceExit`
  - `EndDialogue`
- [ ] `DialogueDatabaseSO` 使用列表 + 内部索引缓存即可，MVP 不做花哨编辑器
- [ ] graph / node / choice 的所有 ID 都使用字符串，不使用引用链或数组序号

**Run:** 在 Unity EditMode Test Runner 中运行 `ProjectArk.SpaceLife.Tests` 里的 `DialogueDatabaseTests`

**Expected:** 3 个测试全部通过；无 index-based 字段残留在新 graph 主链

**完成标志：** `SO` authored data 已经具备长期稳定形态，后续可直接喂给 runner。

---

### Task 3: 实现纯逻辑 runner、condition evaluator 与统一 service exit

**Files:**
- Create: `Assets/Scripts/SpaceLife/Dialogue/DialogueContext.cs`
- Create: `Assets/Scripts/SpaceLife/Dialogue/DialogueNodeViewModel.cs`
- Create: `Assets/Scripts/SpaceLife/Dialogue/DialogueServiceExit.cs`
- Create: `Assets/Scripts/SpaceLife/Dialogue/DialogueFlagStore.cs`
- Create: `Assets/Scripts/SpaceLife/Dialogue/DialogueConditionEvaluator.cs`
- Create: `Assets/Scripts/SpaceLife/Dialogue/DialogueEffectExecutor.cs`
- Create: `Assets/Scripts/SpaceLife/Dialogue/DialogueRunner.cs`
- Create: `Assets/Scripts/SpaceLife/Tests/DialogueRunnerTests.cs`

- [ ] 先写 `DialogueRunnerTests`，至少覆盖以下用例：
  - 同一 owner 在 `WorldStage=0` 和 `WorldStage=1` 时会进不同入口
  - 关系不足时某 choice 不出现
  - 选择某个 choice 后能跳到 `NextNodeId`
  - 选择某个 choice 后触发 `OpenGift` / `OpenIntel` service exit
  - `SetFlag` 影响后续入口选择
- [ ] `DialogueContext` 只放 runner 需要读的快照，不放 `MonoBehaviour` 引用
- [ ] `DialogueFlagStore` 封装 `PlayerSaveData.Progress.Flags` 的 `Get / Set / Clear`，禁止在 runner 里手写 `for` 循环扫 flags
- [ ] `DialogueConditionEvaluator` 做成纯函数风格，便于 EditMode tests
- [ ] `DialogueEffectExecutor` 只做有限集合 effect，不直接打开 UI
- [ ] `DialogueRunner` 负责：
  - `StartDialogue(ownerId, context)`
  - 解析入口
  - 返回当前 `DialogueNodeViewModel`
  - `Choose(choiceId)`
  - 产出 `DialogueServiceExit`
- [ ] 若 graph / node / choice 缺失，输出 `Debug.LogError`，禁止 silent no-op

**Run:** Unity EditMode Test Runner → `ProjectArk.SpaceLife.Tests/DialogueRunnerTests`

**Expected:** 所有 runner 逻辑测试通过；不需要场景即可验证分支逻辑

**完成标志：** `DialogueDomain` 已经成为可测试的内容状态机，不依赖 UI 或 NPC 脚本。

---

### Task 4: 关系值与 dialogue flags 持久化收口

**Files:**
- Modify: `Assets/Scripts/SpaceLife/RelationshipManager.cs`
- Modify: `Assets/Scripts/Core/Save/SaveData.cs`
- Create: `Assets/Scripts/SpaceLife/Tests/DialogueFlagStoreTests.cs`
- Create: `Assets/Scripts/SpaceLife/Tests/RelationshipManagerPersistenceTests.cs`

- [ ] 先写 `DialogueFlagStoreTests`，验证：
  - 新 flag 可写入
  - 同 key 再写会覆盖而不重复追加
  - `ClearFlag` 后读取为 false
- [ ] 先写 `RelationshipManagerPersistenceTests`，验证：
  - 未存档时读到 `NPCDataSO.StartingRelationship`
  - 已存档时按 `NpcId` 读取保存值
  - `ChangeRelationship` 后再次读取能看到更新值
- [ ] `RelationshipManager` 内部改为按 `NpcId` 读写，而不是靠 `NPCDataSO` 实例引用做 save key
- [ ] `RelationshipManager` 提供最小持久化 API：
  - `LoadFromSave()`
  - `SaveToSaveData(PlayerSaveData data)` 或等价内部 helper
- [ ] 在 `SetRelationship / ChangeRelationship` 成功后写回 `SaveManager`，使用与当前 save slot 一致的读写策略
- [ ] `DialogueFlagStore` 也采用同一 save slot，避免 dialogue flags 和关系值落到不同槽位
- [ ] 不修改 `SaveBridge`；确认 `SaveBridge.SaveAll()` 不会清空新增关系值字段

**Run:**
- `cd /Users/dada/Documents/GitHub/Project-Ark && dotnet build Project-Ark.slnx`
- Unity EditMode Test Runner → `DialogueFlagStoreTests`、`RelationshipManagerPersistenceTests`

**Expected:** 编译通过；关系值 / flag 纯逻辑测试通过

**完成标志：** 对话状态真正能跨会话保存，不再只是运行时内存状态。

---

### Task 5: 重构 Hub UI 为 `CanvasGroup` 模态 presenter，并引入 service router

**Files:**
- Modify: `Assets/Scripts/SpaceLife/DialogueUI.cs`
- Modify: `Assets/Scripts/SpaceLife/GiftUI.cs`
- Modify: `Assets/Scripts/SpaceLife/SpaceLifeManager.cs`
- Create: `Assets/Scripts/SpaceLife/Dialogue/DialogueServiceRouter.cs`

- [ ] `DialogueUI` 保留现有组件入口，但职责改为 presenter：
  - 接收 `DialogueNodeViewModel`
  - 显示 speaker、text、choices
  - 将 choice 回传给 coordinator
- [ ] `DialogueUI` 显隐改成 `CanvasGroup`：
  - `Awake()` 初始化 alpha / interactable / blocksRaycasts
  - 禁止 `SetActive(false)` 作为主链
- [ ] `GiftUI` 也改为 `CanvasGroup` 控显隐，因为它会作为 `OpenGift` 的正式出口被继续使用
- [ ] `DialogueUI` 的“无选项节点”处理改为：
  - 若 node type 是普通 line，则生成“继续”按钮推进到显式下一步或结束
  - 不再一律直接 `CloseDialogue()`
- [ ] `DialogueServiceRouter` 负责：
  - `OpenGift` → `GiftUI.ShowGiftUI(...)`
  - `OpenUpgrade` → 调用序列化 `UnityEvent`
  - `OpenIntel` → 调用序列化 `UnityEvent`
  - `TriggerRelationshipEvent` → 调用序列化 `UnityEvent`
- [ ] 在 `SpaceLifeManager` 中补一个明确的模态锁 API，例如 `SetHubInteractionLocked(bool)`：
  - 锁 `PlayerController2D`
  - 锁 `PlayerInteraction`
  - 保留 UI 输入可用

**Run:** `cd /Users/dada/Documents/GitHub/Project-Ark && dotnet build Project-Ark.slnx`

**Expected:** 编译通过；`DialogueUI` / `GiftUI` 不再依赖 GameObject active 状态才能正常工作

**完成标志：** Hub 对话真正具备稳定模态壳，且不会再次踩 `SetActive(false)` 的旧坑。

---

### Task 6: 把 NPC / 终端入口接到新 coordinator，而不是旧 index-based 原型

**Files:**
- Create: `Assets/Scripts/SpaceLife/Dialogue/SpaceLifeDialogueCoordinator.cs`
- Create: `Assets/Scripts/SpaceLife/TerminalDialogueInteractor.cs`
- Modify: `Assets/Scripts/SpaceLife/NPCController.cs`
- Modify: `Assets/Scripts/SpaceLife/PlayerInteraction.cs`（仅当需要为模态态处理输入）
- Modify: `Assets/Scripts/SpaceLife/SpaceLifeManager.cs`

- [ ] `SpaceLifeDialogueCoordinator` 注册到 `ServiceLocator`
- [ ] coordinator 在 `StartDialogueFromNpc(NPCController npc)` 时：
  - 读取 `npc.NPCData.NpcId`
  - 读取当前关系值
  - 读取 `WorldProgressManager.CurrentWorldStage`
  - 读取 `DialogueFlagStore`
  - 构造 `DialogueContext`
  - 启动 `DialogueRunner`
  - 打开 `DialogueUI`
  - 调用 `SetHubInteractionLocked(true)`
- [ ] 在对话结束或 service exit 跳出后，恢复 `SetHubInteractionLocked(false)`
- [ ] `NPCController.OnInteract()` 不再优先走 `NPCInteractionUI`
- [ ] `NPCController.StartDialogue()` 改为委托给 coordinator，不再自己调用 `_npcData.GetEntryLine(...)`
- [ ] `TerminalDialogueInteractor` 复用 `Interactable`，只需要序列化：
  - `OwnerId`
  - `OwnerType = Terminal`
  - 可选 display name
- [ ] 如果 `GiftUI` 被 service router 打开，gift 完成后应允许安全返回正常 Hub 控制

**Run:**
- `cd /Users/dada/Documents/GitHub/Project-Ark && dotnet build Project-Ark.slnx`
- 手工在 `SampleScene` 中点 NPC / Terminal，确认都能进入对话而不是旧菜单

**Expected:** NPC 和 Terminal 都走 coordinator；旧 `NPCInteractionUI` 不再挡在正式链路前面

**完成标志：** Hub 入口全部打到新 `DialogueDomain`，旧原型退到兼容层。

---

### Task 7: 创建 `SO` 内容资产并在 `SampleScene` 做 1 NPC + 1 Terminal 样板切片

**Files:**
- Create asset: `Assets/_Data/SpaceLife/Dialogue/DialogueDatabase.asset`
- Create asset: `Assets/_Data/SpaceLife/Dialogue/Graphs/NPC_Engineer_HubDialogue.asset`
- Create asset: `Assets/_Data/SpaceLife/Dialogue/Graphs/Terminal_ShipAI_HubDialogue.asset`
- Modify scene: `Assets/Scenes/SampleScene.unity`

- [ ] 在 `DialogueDatabase.asset` 中登记 2 份 graph：
  - `engineer_hub`
  - `ship_ai_terminal`
- [ ] NPC graph 至少 author 以下路径：
  - 默认寒暄
  - 关系足够时出现私人追问
  - 其中一个 choice 进入 `OpenGift`
  - 至少一个 choice 通过 `SetFlag` 标记“首次见面已读”
- [ ] Terminal graph 至少 author 以下路径：
  - 默认系统播报
  - `WorldStage >= 1` 时出现新情报节点
  - 至少一个 choice 进入 `OpenIntel`
  - 至少一个 choice 进入 `OpenUpgrade`
- [ ] 在 `SampleScene` 中放一个 NPC 样板和一个 terminal 样板，并把 `OwnerId` 对上 graph
- [ ] 给 `SpaceLifeDialogueCoordinator` 拖好 `DialogueDatabase`、`DialogueUI`、`DialogueServiceRouter`
- [ ] 若 `OpenUpgrade / OpenIntel / TriggerRelationshipEvent` 当前还没有正式 UI，就先把 router 的 UnityEvent 接到清晰的 `Debug.Log` 占位回调，确保链路完整可见

**Run:** Play Mode 手工验证

**Expected:** 两个入口都可玩；至少一次关系变化、一次 flag 写入、一次世界进度 gating 能被观察到

**完成标志：** 这套对话系统不再只是“代码能编译”，而是已经有一个可玩的飞船内垂直切片。

---

### Task 8: 回归验证、文档收口与日志记录

**Files:**
- Modify: `Docs/2_TechnicalDesign/SpaceLife/SpaceLife_HubDialogue_SystemDesign.md`
- Modify: `Docs/5_ImplementationLog/ImplementationLog.md`
- Modify: `Docs/0_Plan/ongoing/README.md`

- [ ] 回头更新技术设计稿，把真正落地的 runtime 文件名、save 方案、service router 口径补成现役真相源
- [ ] 记录本轮新增 / 修改文件、目的与技术取舍到 `ImplementationLog.md`
- [ ] 若计划已经执行完，再把本计划从 `ongoing/` 移到 `complete/`；若未执行完，更新 `ongoing/README.md` 说明它是当前活跃 plan
- [ ] 最终编译一次 + 跑 SpaceLife tests 一次 + 做一轮手工 Play Mode 验收

**Run:**
- `cd /Users/dada/Documents/GitHub/Project-Ark && dotnet build Project-Ark.slnx`
- Unity EditMode Test Runner → `ProjectArk.SpaceLife.Tests`

**Expected:** 编译成功；新增 EditMode tests 通过；ImplementationLog 已记录

**完成标志：** 代码、样板场景、技术设计与日志四条线全部闭环。

---

## 5. 手工验收清单（Play Mode）

### 5.1 船员 NPC 链

- [ ] 接近 NPC，按交互后直接进入对话，不出现旧 `NPCInteractionUI`
- [ ] 初始关系下只看到默认寒暄
- [ ] 通过赠礼或对话 effect 提升关系后，重新对话能看到新分支
- [ ] 首次见面 flag 写入后，旧开场白不再重复

### 5.2 Terminal / AI 链

- [ ] 接近终端后进入相同底层 runner
- [ ] world stage 未满足时看不到后续情报入口
- [ ] `WorldStage` 达标后出现新节点或新选项

### 5.3 服务出口链

- [ ] `OpenGift` 能进入 `GiftUI`
- [ ] `OpenUpgrade` 会通过 router 触发明确出口
- [ ] `OpenIntel` 会通过 router 触发明确出口
- [ ] `TriggerRelationshipEvent` 会通过 router 触发明确出口

### 5.4 存档恢复链

- [ ] 对话写入的 flag 在重新进入场景后仍成立
- [ ] 关系值在重新进入场景后仍成立
- [ ] 关系值恢复后，入口分支也恢复为正确状态

### 5.5 UI / 模态链

- [ ] 对话开始时玩家不能继续走动或乱触发别的交互
- [ ] 对话结束后控制恢复
- [ ] `DialogueUI` / `GiftUI` 不会因 inactive 初始状态导致 `Awake()` 延迟和“第一次打开就自闭”的老问题

---

## 6. 执行顺序建议

推荐严格按以下顺序推进，不要跳步：

1. Task 1 → 先把程序集、save schema、NPC 稳定 ID 建好
2. Task 2 → 先把 authored data 真相源立起来
3. Task 3 → 先把 runner 做成可测纯逻辑
4. Task 4 → 再接持久化
5. Task 5 → 再重构 UI / 模态壳
6. Task 6 → 再接 NPC / Terminal 入口
7. Task 7 → 最后做内容和场景切片
8. Task 8 → 收文档、日志和归档

不要反过来先摆场景、再补 domain；那样只会把旧原型继续拖长。

---

## 7. 风险与防御

### 风险 1：`SpaceLife` 再次出现 UI owner 双轨

**症状：** 旧 `DialogueUI`、新 presenter、旧 `NPCInteractionUI` 同时都能开面板。  
**防御：** 只保留一个正式对话入口：`SpaceLifeDialogueCoordinator -> DialogueUI`。

### 风险 2：关系值能变但不能存

**症状：** 送礼后立刻生效，但切场景就丢。  
**防御：** `RelationshipManagerPersistenceTests` 必须先写；保存键严格使用 `NpcId`。

### 风险 3：graph authoring 又滑回 index-based

**症状：** 作者开始手填 `第 3 行跳第 8 行`。  
**防御：** 新 graph 数据模型不提供 index 字段；所有跳转都只能填 `NodeId`。

### 风险 4：服务出口变成新一轮硬编码入口

**症状：** `NPCController`、`DialogueUI`、`TerminalDialogueInteractor` 各自开 UI。  
**防御：** 一律通过 `DialogueServiceRouter`；未知 exit type 直接报错。

### 风险 5：继续踩 `SetActive(false)` 的 uGUI 老坑

**症状：** 第一次打开 UI 闪一下又关、或 `Awake()` 顺序异常。  
**防御：** 正式主链统一用 `CanvasGroup`；若旧场景对象历史上被序列化成 inactive，必须在 Editor 里手动勾回 active 再保存场景。

---

## 8. 本计划完成后的下一步

当本计划全部完成后，再开启下一份计划处理以下增强项：

- `CSV -> SO` importer
- 终端 / AI 与 NPC 的表现 profile 分离
- 远程通讯入口
- 关卡内短对话入口
- disabled choice affordance
- 本地化 / text key

在这之前，不要提前把 MVP 做成半套叙事工具链。
