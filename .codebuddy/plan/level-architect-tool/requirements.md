# 需求文档 — Level Architect Tool（关卡建筑师）

## 引言

### 背景

Project Ark 目前有 3 个分散的关卡编辑器工具：
- **RoomBatchEditor**：批量修改场景中已有 Room 的属性（类型、楼层、RoomSO），但缺乏空间感知，本质上是一个列表+属性面板
- **LevelDesignerWindow**：基于 `LevelScaffoldData` SO 的可视化拓扑编辑器+房间内元素放置，但整个拓扑图画在 EditorWindow 的 OnGUI 里（不是 SceneView），操作体验粗糙；且 Scaffold → Generate → 场景后，**场景修改无法回写**到 Scaffold（单向数据流断裂）
- **ShebaLevelScaffolder**：硬编码的单关卡搭建脚本，无法复用到其他星球

三个工具之间缺乏联动，关卡策划的实际工作流是碎片化的：在 A 工具中规划拓扑 → 切到 B 工具生成场景 → 在 Inspector 里逐个调整 → 想批量改再切到 C 工具……效率极低。

### 目标

创建一个**统一的关卡建筑师工具（Level Architect Tool）**，将白膜搭建、拓扑编辑、房间配置、验证检查、场景生成整合为一体化工作流。核心设计理念：

1. **Scene-View-First**：拓扑和房间编辑直接在 SceneView 中进行，所见即所得
2. **双向同步**：Scaffold 数据 ↔ 场景 GameObject 双向绑定，修改任一端另一端自动同步
3. **智能自动化**：内置大量推导逻辑（如门自动配对、Confiner 自动适配、RoomSO 自动创建、EncounterSO 预设分配），减少手动配置
4. **房间模板系统**：预定义房间模板（Safe/Normal/Arena/Boss/Corridor 等），一键放置已预配置的完整房间
5. **验证驱动**：实时验证 + 一键修复，确保关卡配置始终正确

---

## 需求

### 需求 1：统一 Scene-View 画布

**用户故事：** 作为一名关卡策划，我希望直接在 Unity Scene View 中编辑关卡拓扑和房间内容，以便所见即所得地看到最终效果，不用在 EditorWindow 的抽象视图和场景之间来回切换。

#### 验收标准

1. WHEN 用户通过菜单 `Window > ProjectArk > Level Architect` 打开工具 THEN 系统 SHALL 在 Scene View 中激活一个 Overlay 工具栏（使用 Unity 2021+ 的 `EditorToolbarElement` 或 `OverlayAttribute`），并在 Scene View 左侧显示一个可折叠的控制面板
2. WHEN 工具激活时 THEN 系统 SHALL 在 Scene View 中为每个房间绘制带颜色编码的白膜矩形（Normal=蓝、Arena=橙、Boss=红、Safe=绿），并在矩形上方显示房间名称和状态图标
3. WHEN 用户在 Scene View 中拖拽房间白膜 THEN 系统 SHALL 实时更新该房间的世界坐标，并同步更新底层 `LevelScaffoldData` 数据（如果存在）和场景 Room GameObject 的 Transform
4. WHEN 用户将一个房间拖到另一个房间边缘附近（距离 < 可配置的吸附阈值）THEN 系统 SHALL 自动吸附对齐到目标房间的边缘，并高亮提示可创建连接
5. IF 当前场景中不存在 `LevelScaffoldData` 引用 THEN 系统 SHALL 提供"从场景扫描"功能，自动收集所有 Room GameObject 并构建 Scaffold 数据

### 需求 2：房间模板预设系统（Room Preset Library）

**用户故事：** 作为一名关卡策划，我希望有一套房间预设模板（如安全房、普通走廊、竞技场、Boss间），一键放置就能得到已预配置好 Collider/Confiner/SpawnPoints/Door Slots 的完整房间，以便我不用每次都从空白开始手动搭建。

#### 验收标准

1. WHEN 用户在 Level Architect 面板中点击"Add Room"下拉菜单 THEN 系统 SHALL 显示可用的房间预设列表，每个预设包含缩略图预览、名称、默认尺寸和描述
2. WHEN 用户选择一个预设并点击 Scene View 中的位置放置 THEN 系统 SHALL 创建一个完整的 Room GameObject，其中包含：BoxCollider2D（Trigger、尺寸匹配）、CameraConfiner 子对象（PolygonCollider2D、Ignore Raycast 层）、SpawnPoints 子容器（根据房间类型预设 2~6 个点位）、以及一个自动创建的 RoomSO 资产
3. WHEN 预设类型为 Arena 或 Boss THEN 系统 SHALL 额外创建 ArenaController 组件、EnemySpawner 子对象，并从预设库中分配默认的 EncounterSO（用户可后续修改）
4. IF 用户想自定义预设 THEN 系统 SHALL 支持将任何已配置好的 Room 右键"保存为预设"，存储到 `Assets/_Data/Level/RoomPresets/` 目录下作为 ScriptableObject

### 需求 3：智能门连接系统（Auto Door Wiring）

**用户故事：** 作为一名关卡策划，我希望门的连接能被智能自动化处理——当我把两个房间对齐时门自动创建并双向配对，以便我不用手动创建 Door GameObject、配置 TargetRoom/TargetSpawnPoint/PlayerLayer 等一大堆字段。

#### 验收标准

1. WHEN 两个房间的白膜矩形共享一条边（在吸附阈值内对齐）THEN 系统 SHALL 自动在共享边的中点创建一对双向 Door GameObject，并完成所有序列化字段的配置（`_targetRoom`、`_targetSpawnPoint`、`_initialState=Open`、`_playerLayer`）
2. WHEN 用户在 Scene View 中从一个房间的边缘拖拽连接线到另一个房间 THEN 系统 SHALL 创建门连接，并自动推导门的位置（拖拽起点边缘的最近点）和朝向（指向目标房间）
3. WHEN 两个连接的房间的 FloorLevel 不同 THEN 系统 SHALL 自动将门标记为 `_isLayerTransition = true`
4. WHEN 用户移动或删除一个房间 THEN 系统 SHALL 自动更新或断开所有与该房间关联的门连接，并在控制台提示受影响的连接列表
5. IF 用户需要手动控制门的状态（如 Locked_Key、Locked_Combat）THEN 系统 SHALL 在 Scene View 中点击门图标时弹出快捷属性编辑器，允许设置门状态、所需钥匙 ID 等

### 需求 4：双向数据同步（Scaffold ↔ Scene Binding）

**用户故事：** 作为一名关卡策划，我希望在 Scene View 中对房间做的任何修改（位置、大小、连接、属性）都能自动反映到 LevelScaffoldData 中，反之亦然，以便数据始终一致，不用担心"场景和配置对不上"的问题。

#### 验收标准

1. WHEN 用户在 Scene View 中修改了 Room GameObject 的位置或大小 THEN 系统 SHALL 自动更新对应的 `ScaffoldRoom.Position` 和 `ScaffoldRoom.Size`，并标记 `LevelScaffoldData` 为 dirty
2. WHEN 用户在 Inspector 中修改了 `LevelScaffoldData` 的房间数据 THEN 系统 SHALL 自动更新对应的场景 Room GameObject
3. WHEN Level Architect 工具激活时 THEN 系统 SHALL 自动建立 ScaffoldRoom.RoomID → Scene Room 的映射表，并在每帧的 SceneView.duringSceneGui 回调中检测差异
4. IF 检测到场景中存在未在 Scaffold 中登记的 Room THEN 系统 SHALL 高亮该房间为黄色并提示"未注册房间"，提供"注册到 Scaffold"的快捷按钮
5. IF 检测到 Scaffold 中存在但场景中找不到对应 GameObject 的 Room THEN 系统 SHALL 在控制面板中列出"缺失房间"，提供"生成到场景"或"从 Scaffold 移除"的选项

### 需求 5：关卡节奏可视化与标注系统

**用户故事：** 作为一名关卡策划，我希望能在拓扑视图上一目了然地看到关卡的节奏设计（战斗强度、安全区分布、关键路径、锁钥依赖），以便评估整体关卡体验是否合理。

#### 验收标准

1. WHEN 用户启用"节奏叠加层（Pacing Overlay）" THEN 系统 SHALL 在每个房间白膜上叠加显示：战斗强度色阶（基于 EncounterSO 的总敌人数/波次数计算）、房间类型图标、门锁状态图标（🔓/🔑/⚔️）
2. WHEN 用户启用"关键路径（Critical Path）" THEN 系统 SHALL 用粗线高亮从入口房间到 Boss 房间的最短路径（基于门连接的 BFS），并用虚线显示所有可选分支路径
3. WHEN 用户启用"锁钥依赖图（Lock-Key Graph）" THEN 系统 SHALL 用有色箭头显示钥匙获取点 → 锁门之间的依赖关系（例如：红钥匙在 Room_A → 红锁门在 Room_D，用红色箭头连接）
4. WHEN 用户悬停在某个房间上 THEN 系统 SHALL 显示该房间的信息浮窗：房间名、类型、楼层、遭遇配置摘要（X波/Y敌人）、关联门列表、RoomSO 路径

### 需求 6：一键验证与自动修复

**用户故事：** 作为一名关卡策划，我希望工具能自动检查所有常见配置问题并提供一键修复，以便我不用逐个房间排查为什么某个门走不通或某个房间没有触发。

#### 验收标准

1. WHEN 用户点击"Validate All" THEN 系统 SHALL 执行以下验证并报告结果：
   - 每个 Room 是否有 RoomSO 引用
   - 每个 Room 的 BoxCollider2D 是否为 Trigger
   - 每个 Room 是否有 CameraConfiner 子对象且在 Ignore Raycast 层
   - 每对门连接是否双向完整（A→B 有门，B→A 也有门）
   - Arena/Boss 房间是否有 ArenaController 和 EncounterSO
   - 所有 Door 的 `_playerLayer` 是否已配置
   - 所有 Door 的 `_targetSpawnPoint` 是否存在
   - 是否存在孤立房间（没有任何门连接的房间）
2. WHEN 验证发现可自动修复的问题 THEN 系统 SHALL 在问题旁显示"Auto-Fix"按钮；点击后自动执行修复（如：创建缺失的 CameraConfiner、补充反向 Door、修复 Layer 设置）
3. WHEN 验证发现无法自动修复的问题 THEN 系统 SHALL 显示"Go To"按钮，点击后在 Scene View 中聚焦到问题对象并选中
4. WHEN 工具激活时 THEN 系统 SHALL 在后台持续运行轻量级验证（仅检查致命问题如缺失 RoomSO），并在 Scene View 中对有问题的房间标记红色感叹号图标

### 需求 7：快速白膜搭建模式（Blockout Mode）

**用户故事：** 作为一名关卡策划，我希望能在几分钟内搭建出一个可 Play-Test 的白膜关卡原型，以便快速验证关卡节奏和空间感，不用一开始就操心美术细节。

#### 验收标准

1. WHEN 用户进入"Blockout Mode" THEN 系统 SHALL 提供一个简化的快速放置工具条，包含：矩形房间笔刷（点击拖拽定义尺寸）、L形房间笔刷、走廊笔刷（固定窄宽、可拖拽长度）
2. WHEN 用户用房间笔刷在 Scene View 中拖拽绘制 THEN 系统 SHALL 实时显示白膜预览，松开鼠标后创建完整的 Room GameObject（包含所有必要组件和自动创建的 RoomSO）
3. WHEN 两个通过笔刷绘制的房间相邻（共享边）THEN 系统 SHALL 自动在共享边创建门连接（复用需求 3 的 Auto Door Wiring 逻辑）
4. WHEN 用户按住 Shift 并从已有房间的边缘拖拽 THEN 系统 SHALL 创建一个新的相连房间（自动连接门），实现"链式绘制"
5. IF 用户完成白膜搭建后想进入 Play Mode 测试 THEN 系统 SHALL 提供"Quick Play"按钮，自动确保 RoomManager/DoorTransitionController 等必要管理器存在，并在缺失时临时创建

### 需求 8：批量操作与高级编辑

**用户故事：** 作为一名关卡策划，我希望能批量选择多个房间进行统一操作（改类型、改楼层、分配遭遇预设、调整大小），以便处理大型关卡时不用逐个修改。

#### 验收标准

1. WHEN 用户在 Scene View 中框选多个房间 THEN 系统 SHALL 高亮所有选中房间，并在侧面板中显示批量编辑属性面板
2. WHEN 用户在批量面板中修改属性并点击"Apply" THEN 系统 SHALL 将更改应用到所有选中房间，支持 Undo/Redo
3. WHEN 用户右键一个或多个选中房间 THEN 系统 SHALL 显示上下文菜单，包含：设为入口房间、设为 Boss 房间、分配遭遇预设（从 EncounterSO 列表选择）、调整大小到模板默认值、复制房间配置、粘贴房间配置
4. WHEN 用户选择"楼层视图切换" THEN 系统 SHALL 按 FloorLevel 分层显示房间，未选择楼层的房间半透明化，方便聚焦编辑特定楼层

### 需求 9：从场景反向扫描与关卡导入

**用户故事：** 作为一名关卡策划，我希望对于已经通过 ShebaLevelScaffolder 或手动搭建的场景，工具能自动扫描现有 Room/Door 并构建完整的 Scaffold 数据和拓扑映射，以便无缝迁移到新工具。

#### 验收标准

1. WHEN 用户点击"Scan Scene" THEN 系统 SHALL 收集场景中所有 `Room` 组件，为每个 Room 创建对应的 `ScaffoldRoom` 条目（从 Room 的 Transform.position/BoxCollider2D.size 推导位置和大小），并扫描所有 `Door` 组件重建连接关系
2. WHEN 扫描完成 THEN 系统 SHALL 将结果保存为新的 `LevelScaffoldData` 资产，并提示用户选择保存路径
3. IF 场景中的 Room 缺少 RoomSO THEN 系统 SHALL 在扫描过程中自动为其创建 RoomSO 并保存到 `Assets/_Data/Level/Rooms/` 目录

### 需求 10：整合替代现有工具

**用户故事：** 作为开发者，我希望新的 Level Architect Tool 完全涵盖现有 RoomBatchEditor、LevelDesignerWindow 和 ShebaLevelScaffolder 的所有功能，以便后续可以废弃旧工具，减少代码维护负担。

#### 验收标准

1. WHEN Level Architect Tool 完成开发 THEN 系统 SHALL 覆盖以下现有功能：
   - RoomBatchEditor 的：房间搜索/筛选、多选、批量属性编辑、JSON 导入导出、验证检查
   - LevelDesignerWindow 的：拓扑视图、房间创建/删除、门连接管理、房间内元素放置、Scene 生成
   - ShebaLevelScaffolder 的：一键搭建含 RoomSO/EncounterSO/Door 的完整关卡
2. WHEN 新工具完成且通过验收 THEN 旧的 `RoomBatchEditor.cs`、`LevelDesignerWindow.cs`、`ShebaLevelScaffolder.cs` SHALL 被标记为 `[Obsolete]` 并在后续版本移除
3. WHEN 新工具激活时 THEN 系统 SHALL 检测旧工具窗口是否打开，如果是则提示用户切换到新工具
