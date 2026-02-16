# 实施计划 — Level Architect Tool

> 基于需求文档 `requirements.md`，以下是按逻辑递进顺序排列的编码任务清单。

---

- [ ] 1. 搭建 Level Architect 基础框架与 SceneView 集成
   - 创建 `LevelArchitectWindow.cs`（EditorWindow），作为工具的主入口，注册 `SceneView.duringSceneGui` 回调
   - 实现 SceneView Overlay 工具栏（使用 `[Overlay]` 或 `EditorToolbarElement`），包含模式切换按钮（Select / Blockout / Connect）
   - 实现 SceneView 左侧可折叠控制面板（使用 `Handles.BeginGUI` / `GUILayout` 在 SceneView 内绘制）
   - 注册菜单项 `Window > ProjectArk > Level Architect`
   - 管理工具激活/停用状态，激活时注入 SceneView 回调，停用时清理
   - _需求：1.1, 1.2_

- [ ] 2. 实现房间白膜渲染与交互系统
   - 在 SceneView 的 `duringSceneGui` 回调中，遍历场景所有 `Room` 组件，用 `Handles.DrawSolidRectangleWithOutline` 绘制颜色编码矩形（Normal=蓝、Arena=橙、Boss=红、Safe=绿），矩形范围取自 `BoxCollider2D.size`
   - 在矩形上方用 `Handles.Label` 显示房间名称（`Room.RoomID`）和状态图标
   - 实现房间选择：鼠标点击检测（`HandleUtility.GUIPointToWorldRay` + 矩形命中测试），选中房间高亮描边
   - 实现房间拖拽移动：选中后拖拽更新 `Transform.position`，支持 Undo（`Undo.RecordObject`）
   - 实现吸附对齐：拖拽时检测与其他房间边缘距离 < 阈值（默认 0.5 单位），自动吸附并高亮共享边
   - 实现框选多个房间（拖拽矩形选区）
   - _需求：1.2, 1.3, 1.4, 8.1_

- [ ] 3. 实现 RoomPresetSO 房间模板预设系统
   - 创建 `RoomPresetSO.cs`（ScriptableObject），字段包含：预设名称、描述、默认尺寸（`Vector2`）、房间类型（`RoomType`）、SpawnPoint 数量、是否包含 ArenaController/EnemySpawner、关联的默认 EncounterSO 引用、缩略图预览（`Texture2D`）
   - 创建 `RoomFactory.cs`（静态工具类），`CreateRoomFromPreset(RoomPresetSO preset, Vector3 position)` 方法：创建 Room GameObject + BoxCollider2D(Trigger) + CameraConfiner 子对象(PolygonCollider2D, Ignore Raycast) + SpawnPoints 子容器 + 自动创建 RoomSO 资产保存到 `Assets/_Data/Level/Rooms/`
   - 对 Arena/Boss 预设额外创建 ArenaController 组件和 EnemySpawner 子对象，分配默认 EncounterSO
   - 创建 5 个内置预设 SO 资产：`Preset_Safe`、`Preset_Normal`、`Preset_Arena`、`Preset_Boss`、`Preset_Corridor`，保存到 `Assets/_Data/Level/RoomPresets/`
   - 在控制面板中实现 "Add Room" 下拉菜单，列出所有 `RoomPresetSO` 资产（通过 `AssetDatabase.FindAssets` 扫描），点击后进入放置模式（鼠标光标变为十字，点击 SceneView 位置创建房间）
   - 实现右键 Room → "Save as Preset" 功能，将当前 Room 的配置序列化为新的 `RoomPresetSO`
   - _需求：2.1, 2.2, 2.3, 2.4_

- [ ] 4. 实现智能门连接系统（Auto Door Wiring）
   - 创建 `DoorWiringService.cs`（Editor 静态工具类），核心方法：
     - `AutoConnectRooms(Room roomA, Room roomB)`：计算两房间共享边中点，创建一对 Door 子对象（各自作为所属 Room 的子节点），自动配置 `_targetRoom`、`_targetSpawnPoint`（在目标房间的对称位置创建 SpawnPoint）、`_playerLayer`（默认 Player 层）、`_initialState = Open`
     - `DisconnectRooms(Room roomA, Room roomB)`：删除两房间之间的 Door 对
     - `UpdateDoorPositions(Room room)`：当房间移动/调整大小后，重新计算所有关联门的位置
   - 当两个房间的 FloorLevel 不同时自动设置 `_isLayerTransition = true`
   - 在 SceneView 中绘制连接线模式：从房间边缘拖拽连接线到目标房间，松开后调用 `AutoConnectRooms`
   - 当房间被删除或移动导致原有邻接关系断裂时，自动断开门连接并 `Debug.Log` 提示
   - 在 SceneView 中点击门图标弹出内联属性编辑器（`DoorState`、`_requiredKeyID`、`_isLayerTransition`）
   - 所有创建/删除操作均通过 `Undo.RegisterCreatedObjectUndo` / `Undo.DestroyObjectImmediate` 支持撤销
   - _需求：3.1, 3.2, 3.3, 3.4, 3.5_

- [ ] 5. 实现 Scaffold ↔ Scene 双向数据同步
   - 创建 `ScaffoldSceneBinder.cs`（Editor 类），负责维护 `Dictionary<string, Room>` 的 RoomID → Scene Room 映射表
   - 在 Level Architect 激活时，自动扫描场景中所有 `Room` 组件和当前关联的 `LevelScaffoldData`，建立映射
   - Scene → Scaffold 同步：在 `duringSceneGui` 中检测 Room Transform/BoxCollider2D 变化，同步更新对应 `ScaffoldRoom.Position` / `ScaffoldRoom.Size`，调用 `EditorUtility.SetDirty` 标记 SO
   - Scaffold → Scene 同步：通过 `ScriptableObject` 的 `OnValidate` 或定时轮询（每 0.5s），检测 `LevelScaffoldData` 字段变化并同步到场景 Room
   - 未注册房间（场景有、Scaffold 无）：高亮黄色 + 提供 "Register to Scaffold" 按钮
   - 缺失房间（Scaffold 有、场景无）：在面板中列出 + 提供 "Generate to Scene" / "Remove from Scaffold" 按钮
   - _需求：4.1, 4.2, 4.3, 4.4, 4.5_

- [ ] 6. 实现快速白膜搭建模式（Blockout Mode）
   - 在 Overlay 工具栏中添加 "Blockout" 模式按钮，激活后显示笔刷工具条
   - 实现矩形房间笔刷：鼠标按下拖拽定义矩形范围，实时绘制半透明预览框，松开后调用 `RoomFactory.CreateRoomFromPreset`（使用 Normal 预设），尺寸取自拖拽范围
   - 实现走廊笔刷：固定窄宽（默认 3 单位），拖拽定义长度，创建 Corridor 类型房间
   - 实现链式绘制：按住 Shift 从已有房间边缘拖拽，创建新房间并自动调用 `DoorWiringService.AutoConnectRooms` 连接
   - 绘制完成后自动检测新房间与相邻已有房间的共享边，触发自动门连接
   - 实现 "Quick Play" 按钮：检测场景中是否存在 `RoomManager` / `DoorTransitionController`，缺失则临时创建，然后 `EditorApplication.EnterPlaymode()`
   - _需求：7.1, 7.2, 7.3, 7.4, 7.5_

- [ ] 7. 实现一键验证与自动修复系统
   - 创建 `LevelValidator.cs`（Editor 类），定义 `ValidationResult`（severity, message, target Object, canAutoFix, fixAction）
   - 实现 8 项验证规则（均作为独立方法，返回 `List<ValidationResult>`）：
     - Room 缺少 RoomSO 引用
     - Room 的 BoxCollider2D 未设为 Trigger
     - Room 缺少 CameraConfiner 子对象或 CameraConfiner 不在 Ignore Raycast 层
     - 门连接不双向（A→B 有门但 B→A 没有）
     - Arena/Boss 房间缺少 ArenaController 或 EncounterSO
     - Door 的 `_playerLayer` 未配置（值为 0）
     - Door 的 `_targetSpawnPoint` 为 null
     - 孤立房间（无任何门连接）
   - 可自动修复的问题实现 Auto-Fix 回调（如：创建 CameraConfiner、补反向 Door、设置 Trigger/Layer）
   - 不可修复的问题提供 "Go To" 按钮（`SceneView.FrameSelected` + `Selection.activeObject`）
   - 在控制面板中显示 "Validate All" 按钮，结果以列表形式展示（图标 + 消息 + 操作按钮）
   - 后台轻量验证：工具激活时每帧检查致命问题（缺失 RoomSO），在 SceneView 对应房间上叠加红色感叹号
   - _需求：6.1, 6.2, 6.3, 6.4_

- [ ] 8. 实现批量操作与高级编辑面板
   - 在控制面板中实现批量属性编辑区域：当框选多个房间时显示，可批量修改 RoomType、FloorLevel、RoomSO、默认大小
   - 实现 "Apply to Selected" 按钮，将属性修改应用到所有选中房间，使用 `Undo.RecordObjects` 支持撤销
   - 实现右键上下文菜单：设为入口房间 / Boss房间、分配 EncounterSO（弹出 SO 选择器 `EditorGUIUtility.ShowObjectPicker`）、调整大小到预设默认值、复制/粘贴房间配置
   - 实现楼层视图切换：下拉选择 FloorLevel，非选中楼层房间半透明渲染（修改 Handles 颜色 alpha）
   - _需求：8.1, 8.2, 8.3, 8.4_

- [ ] 9. 实现关卡节奏可视化叠加层
   - 在控制面板中添加 Overlay 开关组：Pacing Overlay / Critical Path / Lock-Key Graph
   - Pacing Overlay：读取每个房间关联 RoomSO 的 EncounterSO，计算总敌人数/波次数，映射为颜色色阶（绿→黄→红），叠加在白膜矩形上；绘制门锁状态图标（Open=无图标, Locked_Key=🔑, Locked_Combat=⚔️）
   - Critical Path：基于门连接构建邻接图，BFS 求入口房间到 Boss 房间的最短路径，用粗实线（`Handles.DrawAAPolyLine` width=5）高亮；其余路径用虚线（`Handles.DrawDottedLine`）
   - Lock-Key Graph：扫描所有 Door 的 `_requiredKeyID`，扫描场景中所有 KeyPickup（或 RoomSO 标记），用有色箭头（`Handles.DrawLine` + `Handles.ConeHandleCap`）连接钥匙所在房间→锁门所在房间
   - 房间悬停信息浮窗：检测鼠标悬停房间，用 `Handles.BeginGUI` 绘制信息卡片（名称、类型、楼层、遭遇摘要、门列表、RoomSO 路径）
   - _需求：5.1, 5.2, 5.3, 5.4_

- [ ] 10. 实现场景反向扫描与旧工具迁移
   - 在控制面板中实现 "Scan Scene" 按钮：收集所有 `Room` 组件，为每个创建 `ScaffoldRoom`（从 `Transform.position` 取位置、`BoxCollider2D.size` 取大小）；扫描所有 `Door` 组件重建 `ScaffoldDoorConnection`；缺少 RoomSO 的自动创建并保存
   - 扫描结果存为新的 `LevelScaffoldData` 资产，弹出 `EditorUtility.SaveFilePanelInProject` 让用户选择路径
   - 在 `LevelArchitectWindow` 激活时检测 `RoomBatchEditor` / `LevelDesignerWindow` 是否打开（`EditorWindow.HasOpenInstances<T>()`），如果是则弹出对话框提示切换到新工具
   - 旧工具代码标记 `[Obsolete("Use LevelArchitectWindow instead")]`，但不删除，保留兼容性
   - _需求：9.1, 9.2, 9.3, 10.1, 10.2, 10.3_
