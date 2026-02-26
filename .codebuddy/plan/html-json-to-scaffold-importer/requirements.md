# 需求文档：HTML JSON → LevelScaffoldData 导入脚本

## 引言

LevelDesigner.html 是一个浏览器端的关卡可视化编辑器，用于快速搭建银河恶魔城式关卡拓扑结构。它导出的 JSON 格式与 Unity 端的 `LevelScaffoldData` ScriptableObject 存在多项结构性差异。本功能旨在编写一个 Unity Editor 导入脚本（`HtmlScaffoldImporter`），将 HTML JSON 文件一键转换为 `LevelScaffoldData` `.asset` 文件，使策划可以无缝地从浏览器设计转入 Unity 编辑。

### 两端格式差异概要

| 维度 | HTML JSON | Unity LevelScaffoldData |
|------|----------|------------------------|
| 顶层结构 | `rooms` + `connections` + `doorLinks` | `_levelName` + `_floorLevel` + `_rooms`（连接内嵌） |
| 房间ID | 人类可读字符串 `"Z1a_crash_site"` | GUID `"ec695eb6-..."` |
| 房间类型 | 字符串 `"safe"/"normal"/"arena"/"boss"` | 枚举 `RoomType` (Normal=0, Arena=1, Boss=2, Safe=3) |
| 房间位置 | `[x, y]` 网格单位整数 | `Vector3 {x, y, 0}` 世界坐标 |
| 房间尺寸 | `[w, h]` 网格单位 | `Vector2 {x, y}` |
| 连接模型 | 全局 `connections` 数组（from/to/fromDir/toDir） | 每个房间内嵌 `_connections: List<ScaffoldDoorConnection>` |
| 元素类型 | 字符串 `"spawn"/"enemy"/"chest"/"npc"/"door"/"checkpoint"` | 枚举 `ScaffoldElementType` (Wall=0, WallCorner=1, CrateWooden=2, CrateMetal=3, Door=4, Checkpoint=5, PlayerSpawn=6, EnemySpawn=7, Hazard=8) |
| 元素位置 | `[x, y]` 相对房间左上角的网格偏移 | `Vector3 _localPosition` 相对房间**中心**的世界偏移 |
| 门配置 | `doorLinks` 数组 | `ScaffoldDoorElementConfig` + `_boundConnectionID` |
| Floor | 每个房间独立 `floor` 字段 | ScriptableObject 全局 `_floorLevel`（只有一个值） |

---

## 需求

### 需求 1：EditorWindow 入口与文件选择

**用户故事：** 作为一名关卡策划，我希望通过 Unity Editor 菜单打开导入窗口，选择 HTML JSON 文件并指定输出路径，以便一键完成导入。

#### 验收标准

1. WHEN 用户点击菜单 `Window > ProjectArk > Import HTML Scaffold JSON` THEN 系统 SHALL 打开一个 EditorWindow。
2. WHEN 导入窗口打开 THEN 系统 SHALL 显示以下控件：
   - JSON 文件路径选择按钮（打开文件对话框，过滤 `.json`）
   - 输出 `.asset` 路径选择按钮（SavePanel，默认 `Assets/_Data/Level/Scaffolds/`）
   - 网格单位到世界单位的缩放因子输入框（默认值 `1.0`，即 1 网格 = 1 世界单位）
   - "Import" 按钮
3. IF JSON 文件路径为空或不存在 THEN 系统 SHALL 禁用 Import 按钮并显示提示。
4. WHEN 用户点击 Import THEN 系统 SHALL 执行完整的转换流程并在 Console 打印结果摘要。

### 需求 2：房间数据映射

**用户故事：** 作为一名关卡策划，我希望 HTML JSON 中的每个房间都被正确转换为 `ScaffoldRoom`，包括 ID、名称、类型、位置和尺寸，以便 Unity 端拓扑与 HTML 设计一致。

#### 验收标准

1. WHEN 导入处理一个 HTML 房间对象 THEN 系统 SHALL 创建一个 `ScaffoldRoom`，其中：
   - `_roomID` 设为新生成的 GUID
   - `_displayName` 设为 HTML JSON 的 `name` 字段
   - `_roomType` 根据 `type` 字符串映射：`"normal"→Normal`, `"arena"→Arena`, `"boss"→Boss`, `"safe"→Safe`
   - `_position` 设为 `Vector3(x * scale, -y * scale, 0)`（HTML 的 y 轴向下，Unity 向上，需翻转）
   - `_size` 设为 `Vector2(w * scale, h * scale)`
2. WHEN 处理包含 `"comment"` 键的 JSON 对象 THEN 系统 SHALL 跳过该对象（它是注释标记，不是房间数据）。
3. 系统 SHALL 维护一个 `Dictionary<string, string>` 映射，记录 HTML ID → GUID 的对应关系，供后续连接转换使用。
4. IF `type` 字符串不在已知映射中 THEN 系统 SHALL 默认映射为 `RoomType.Normal` 并输出警告。

### 需求 3：连接数据转换

**用户故事：** 作为一名关卡策划，我希望 HTML 全局 `connections` 数组被正确转换为每个房间的内嵌 `ScaffoldDoorConnection`，以便 Unity 端的门连接拓扑完整。

#### 验收标准

1. WHEN 导入处理 `connections` 数组中的一个连接 `{from, to, fromDir, toDir}` THEN 系统 SHALL：
   - 在 `from` 对应的 `ScaffoldRoom` 的 `_connections` 中添加一个 `ScaffoldDoorConnection`
   - `_connectionID` 设为新 GUID
   - `_targetRoomID` 设为 `to` 对应的 GUID（通过映射表查找）
   - `_doorPosition` 设为根据 `fromDir` 方向计算的房间边缘中点位置（相对房间中心的局部坐标）
   - `_doorDirection` 设为方向向量：`"east"→(1,0)`, `"west"→(-1,0)`, `"north"→(0,1)`, `"south"→(0,-1)`
2. WHEN 处理包含 `"comment"` 键的连接对象 THEN 系统 SHALL 跳过该对象。
3. IF `from` 或 `to` 的房间 ID 在映射表中不存在 THEN 系统 SHALL 跳过该连接并输出警告。
4. WHEN 两个房间的 `floor` 值不同且存在连接 THEN 系统 SHALL 将该 `ScaffoldDoorConnection` 的 `_isLayerTransition` 设为 `true`。

### 需求 4：元素类型映射

**用户故事：** 作为一名关卡策划，我希望 HTML 中的每个元素（spawn、enemy、chest、npc、door、checkpoint）都被合理映射为 Unity 的 `ScaffoldElement`，以便关卡内容完整保留。

#### 验收标准

1. WHEN 导入处理一个 HTML 元素 THEN 系统 SHALL 按以下映射创建 `ScaffoldElement`：
   - `"spawn"` → `ScaffoldElementType.PlayerSpawn`
   - `"enemy"` → `ScaffoldElementType.EnemySpawn`
   - `"checkpoint"` → `ScaffoldElementType.Checkpoint`
   - `"door"` → `ScaffoldElementType.Door`
   - `"chest"` → `ScaffoldElementType.CrateWooden`（最接近的可用类型，作为占位）
   - `"npc"` → `ScaffoldElementType.Checkpoint`（作为占位标记，并在 `_elementID` 或日志中标注为 NPC 来源）
2. WHEN 转换元素位置 THEN 系统 SHALL 将 HTML 的 `[ex, ey]`（相对房间左上角偏移）转换为相对房间中心的 `Vector3`：
   - `localX = (ex - roomW/2) * scale`
   - `localY = -(ey - roomH/2) * scale`（y 轴翻转）
   - `localZ = 0`
3. IF 元素 type 字符串不在已知映射中 THEN 系统 SHALL 默认映射为 `ScaffoldElementType.Hazard` 并输出警告。
4. WHEN 元素类型为 `"door"` THEN 系统 SHALL 同时调用 `EnsureDoorConfigExists()` 为其创建默认的 `ScaffoldDoorElementConfig`。

### 需求 5：doorLinks 转换

**用户故事：** 作为一名关卡策划，我希望 HTML 的 `doorLinks` 数据被转换为 Door 元素的 `_boundConnectionID` 和 `ScaffoldDoorElementConfig`，以便门的行为逻辑正确传递。

#### 验收标准

1. WHEN 导入处理一个 `doorLink` 条目 `{roomId, entryDir, doorIndex}` THEN 系统 SHALL：
   - 通过 `roomId` 在映射表中查找对应的 `ScaffoldRoom`
   - 在该房间的 `_elements` 中找到类型为 `Door` 的第 `doorIndex` 个元素
   - 在该房间的 `_connections` 中查找 `_doorDirection` 与 `entryDir` 方向匹配的连接
   - 将 Door 元素的 `_boundConnectionID` 设为匹配连接的 `_connectionID`
2. IF `doorIndex` 为 `-1` THEN 系统 SHALL 将其解释为"该房间无门元素，但连接存在"，仅输出信息日志，不创建绑定。
3. IF `doorIndex` 超出该房间 Door 元素数量 THEN 系统 SHALL 输出警告并跳过。

### 需求 6：Floor 层级处理

**用户故事：** 作为一名关卡策划，我希望导入脚本能正确处理多 floor 房间，即使 `LevelScaffoldData` 只有一个全局 `_floorLevel`，以便不丢失层级信息。

#### 验收标准

1. WHEN 导入的 JSON 中所有房间 `floor` 值相同 THEN 系统 SHALL 将 `LevelScaffoldData._floorLevel` 设为该值。
2. WHEN 导入的 JSON 中存在多个不同 `floor` 值 THEN 系统 SHALL：
   - 将 `_floorLevel` 设为出现次数最多的 `floor` 值
   - 在 Console 输出警告：列出所有不同的 floor 值以及各自包含的房间数量
   - 在每个非主 floor 的房间 `_displayName` 后追加 `[F={floor}]` 标记以保留信息
3. WHEN 两个连接房间的 `floor` 不同 THEN 系统 SHALL 将对应 `ScaffoldDoorConnection._isLayerTransition` 设为 `true`（与需求 3.4 一致）。

### 需求 7：导入验证与日志

**用户故事：** 作为一名关卡策划，我希望导入完成后能看到清晰的结果报告，包括成功统计和所有警告/错误，以便快速确认导入质量。

#### 验收标准

1. WHEN 导入成功完成 THEN 系统 SHALL 在 Console 打印摘要，包含：
   - 总房间数
   - 总连接数（ScaffoldDoorConnection 总数）
   - 总元素数
   - 被跳过的连接数（因房间 ID 未找到等）
   - 元素类型映射警告数（chest→CrateWooden, npc→Checkpoint 等占位映射的次数）
   - 涉及的 floor 层级列表
2. WHEN 导入过程中遇到致命错误（如 JSON 解析失败） THEN 系统 SHALL 显示 `EditorUtility.DisplayDialog` 错误对话框并中止操作，不创建任何 asset。
3. 系统 SHALL 支持 Undo：整个导入操作可通过 Ctrl+Z 一步撤销。

### 需求 8：与现有工具链集成

**用户故事：** 作为一名开发者，我希望导入脚本符合项目架构规范，与 LevelArchitectWindow 和 LevelGenerator 等现有工具链无缝配合，以便导入的 asset 可以直接用于后续流程。

#### 验收标准

1. 脚本 SHALL 放置在 `Assets/Scripts/Level/Editor/` 目录下，命名空间为 `ProjectArk.Level.Editor`。
2. 脚本 SHALL 使用 `#if UNITY_EDITOR` 包裹，确保不会打入运行时包。
3. 导入生成的 `LevelScaffoldData` asset SHALL 可以直接拖入 `LevelArchitectWindow` 的 Scaffold Data 字段使用。
4. 导入生成的 `LevelScaffoldData` asset SHALL 可以直接被 `LevelGenerator.GenerateLevel()` 处理生成场景对象。
5. 脚本 SHALL 使用 Unity 内置的 `JsonUtility` 或 `System.Text.Json`（如需嵌套解析）进行 JSON 反序列化，不引入第三方 JSON 库依赖。

---

## 边界情况与技术备注

1. **comment 对象过滤**：HTML JSON 中 `rooms` 和 `connections` 数组内混有 `{"comment": "..."}` 对象，必须在所有处理阶段跳过。
2. **双向连接**：HTML JSON 用两条 connection 记录表示双向连接（A→B + B→A），导入后每个房间各持有一个 `ScaffoldDoorConnection`，这符合 Unity 端的设计。
3. **单向连接**：HTML JSON 中只有一条 A→B 记录即为单向通行，导入时只在 A 房间添加连接。
4. **坐标系差异**：HTML JSON 使用屏幕坐标系（y 向下），Unity 使用世界坐标系（y 向上），需要翻转 y 轴。
5. **JSON 解析**：因为 `JsonUtility` 不支持任意 JSON 结构（需要预定义类），导入脚本需要定义中间数据类（`HtmlLevelData`, `HtmlRoom`, `HtmlConnection`, `HtmlDoorLink`, `HtmlElement`）来反序列化 HTML JSON。但由于 rooms 数组中混有 comment 对象，可能需要使用 `MiniJSON` 或手动解析 `JsonDocument`（.NET 内置）来处理。
6. **ScaffoldElementType 枚举缺口**：HTML 的 `chest` 和 `npc` 类型在 Unity 枚举中无直接对应，使用最接近类型作为占位并在日志中明确标注。
