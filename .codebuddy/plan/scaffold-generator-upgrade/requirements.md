# 需求文档：ScaffoldToSceneGenerator 效率升级

## 引言

`ScaffoldToSceneGenerator.cs` 是 Project Ark 的一键关卡生成工具，目前已能生成房间 GO、Door、Checkpoint、EncounterSO 等基础结构。本次升级聚焦于消灭生成后仍需手动完成的琐碎步骤，核心诉求：**方便、快捷、尽量一步到位**。

本次实现以下 6 项改进（排除 #1 JSON直接导入 和 #9 Scene自动保存）：

---

## 需求

### 需求 2：房间尺寸 Fallback 与异常警告

**用户故事：** 作为关卡设计师，我希望当 ScaffoldRoom.Size 为零或异常时系统能自动使用合理的默认尺寸，以便避免生成 (0,0) 的不可用房间。

#### 验收标准

1. WHEN `ScaffoldRoom.Size` 的 x 或 y 分量 `<= 0` THEN 系统 SHALL 按 `RoomType` 使用预设默认尺寸（Normal=20×15，Arena=30×20，Boss=40×30，Corridor=20×8，Shop=15×12，其余=20×15）
2. WHEN 使用了 fallback 尺寸 THEN 系统 SHALL 在 Console 输出 Warning，格式为 `⚠️ Room '{DisplayName}' has zero/invalid Size, using fallback {w}×{h}`
3. WHEN 生成报告输出时 THEN 系统 SHALL 在 TODO Checklist 中列出所有使用了 fallback 尺寸的房间名称

---

### 需求 3：Door 位置自动推算（边缘吸附）

**用户故事：** 作为关卡设计师，我希望 Door 的位置能根据连接方向和房间尺寸自动推算到房间边缘，以便不再需要手动填写门坐标。

#### 验收标准

1. WHEN `ScaffoldDoorConnection.DoorPosition` 为 `Vector3.zero` 或未填写 THEN 系统 SHALL 根据 `DoorDirection` 和 `ScaffoldRoom.Size` 自动计算门在房间边缘的局部坐标
2. WHEN `DoorDirection` 为 Right THEN 门的 localPosition.x SHALL 等于 `Size.x * 0.5f`，localPosition.y SHALL 为 0
3. WHEN `DoorDirection` 为 Left THEN 门的 localPosition.x SHALL 等于 `-Size.x * 0.5f`，localPosition.y SHALL 为 0
4. WHEN `DoorDirection` 为 Up THEN 门的 localPosition.y SHALL 等于 `Size.y * 0.5f`，localPosition.x SHALL 为 0
5. WHEN `DoorDirection` 为 Down THEN 门的 localPosition.y SHALL 等于 `-Size.y * 0.5f`，localPosition.x SHALL 为 0
6. WHEN `DoorDirection` 为 None 或未定义 THEN 系统 SHALL 保留原有 fallback 逻辑（`-dir * 2`）并输出 Warning
7. IF `DoorPosition` 已有非零值 THEN 系统 SHALL 直接使用该值，不覆盖

---

### 需求 4：自动创建标准 Tilemap 层级

**用户故事：** 作为关卡设计师，我希望每个房间 GO 生成后自动包含标准的 Tilemap 子层级，以便进入 Unity 后可以直接开始绘制地图，无需手动添加。

#### 验收标准

1. WHEN 创建房间 GO 时 THEN 系统 SHALL 在其下自动创建名为 `Tilemaps` 的子 GameObject
2. WHEN 创建 `Tilemaps` 子对象时 THEN 系统 SHALL 在其下创建三个子层：`Tilemap_Ground`、`Tilemap_Wall`、`Tilemap_Decoration`
3. WHEN 创建每个 Tilemap 子层时 THEN 系统 SHALL 为其添加 `Tilemap` 组件和 `TilemapRenderer` 组件
4. WHEN 添加 `TilemapRenderer` 时 THEN `Tilemap_Ground` 的 sortingOrder SHALL 为 0，`Tilemap_Wall` SHALL 为 1，`Tilemap_Decoration` SHALL 为 2
5. WHEN 生成完成后 THEN TODO Checklist 中的第 1 条（Paint Tilemaps）SHALL 更新为提示"Tilemap 层级已自动创建，直接选中对应层开始绘制"

---

### 需求 5：EncounterSO 按元素类型自动填充敌人

**用户故事：** 作为关卡设计师，我希望 EncounterSO 能根据 ScaffoldElement 中携带的敌人类型信息自动填充对应 Prefab，以便不再需要手动替换 [DEFAULT] EncounterSO 中的敌人。

#### 验收标准

1. WHEN `ScaffoldElement.ElementType == EnemySpawn` 且 `ScaffoldElement.EnemyTypeID` 非空 THEN 系统 SHALL 尝试从 `Assets/_Prefabs/Enemies/{EnemyTypeID}.prefab` 加载对应 Prefab
2. WHEN 同一房间内存在多种 `EnemyTypeID` THEN 系统 SHALL 在 EncounterSO 的同一 Wave 中创建多个 Entry，每种类型一个 Entry
3. WHEN 指定路径的 Prefab 不存在 THEN 系统 SHALL fallback 到 `Enemy_Rusher.prefab` 并输出 Warning：`⚠️ EnemyPrefab '{EnemyTypeID}' not found, falling back to Enemy_Rusher`
4. IF `ScaffoldElement.EnemyTypeID` 为空 THEN 系统 SHALL 保持原有行为（使用 Enemy_Rusher）
5. WHEN 生成报告输出时 THEN 系统 SHALL 列出每个房间使用的敌人类型汇总

---

### 需求 6：增量更新模式（保护已有 Tilemap 内容）

**用户故事：** 作为关卡设计师，我希望在修改了少量房间数据后重新生成时，已经绘制好的 Tilemap 内容不会被清空，以便保护已有的美术工作成果。

#### 验收标准

1. WHEN EditorWindow GUI 显示时 THEN 系统 SHALL 在 Generate 按钮旁提供 `Update Existing` 复选框（默认关闭）
2. WHEN `Update Existing` 为 true 且场景中已存在同名房间 GO THEN 系统 SHALL 跳过重建该房间 GO，仅更新其 RoomSO、BoxCollider2D.size、Door 组件属性
3. WHEN `Update Existing` 为 true THEN 系统 SHALL 保留房间 GO 下已有的 Tilemap 子层级及其绘制内容，不删除不重建
4. WHEN `Update Existing` 为 false THEN 系统 SHALL 保持原有全量覆盖行为
5. WHEN 增量更新完成后 THEN Console SHALL 输出每个被跳过重建（保留）的房间名称列表

---

### 需求 7：Gizmo 统一开关

**用户故事：** 作为关卡设计师，我希望能一键显示或隐藏所有 Gizmo 可视化组件，以便在调试时快速查看布局，发布前一键关闭而无需逐个删除。

#### 验收标准

1. WHEN EditorWindow GUI 显示时 THEN 系统 SHALL 在 Generate 按钮下方提供 `Toggle Gizmos` 按钮
2. WHEN 点击 `Toggle Gizmos` 时 THEN 系统 SHALL 遍历场景中所有 Gizmo 子对象（名为 `Label` 的 TextMesh 和挂有 `SpriteRenderer` 且 sortingOrder==1 的组件）
3. WHEN Gizmo 当前为显示状态 THEN `Toggle Gizmos` SHALL 将所有 Gizmo 的 `SpriteRenderer.enabled` 和 `MeshRenderer.enabled` 设为 false
4. WHEN Gizmo 当前为隐藏状态 THEN `Toggle Gizmos` SHALL 将所有 Gizmo 的 `SpriteRenderer.enabled` 和 `MeshRenderer.enabled` 设为 true
5. WHEN `Toggle Gizmos` 执行后 THEN 按钮文字 SHALL 更新为当前状态（`Hide Gizmos` 或 `Show Gizmos`）
6. IF 场景中没有任何 Gizmo 对象 THEN 系统 SHALL 在 Console 输出 `No Gizmo visuals found in scene`

---

### 需求 8：SanitizeName 空格处理与路径预览

**用户故事：** 作为关卡设计师，我希望 LevelName 中的空格也被替换为下划线，并在工具窗口中预览生成路径，以便确认 asset 存放位置正确。

#### 验收标准

1. WHEN `SanitizeName` 处理包含空格的字符串时 THEN 系统 SHALL 将空格替换为 `_`
2. WHEN `_scaffold` 字段被赋值时 THEN EditorWindow SHALL 在 Scaffold Data 字段下方显示灰色小字预览路径，格式为 `Output: Assets/_Data/Level/{SanitizedName}/`
3. WHEN `_scaffold` 为 null 时 THEN 路径预览 SHALL 不显示
