# 实施计划：HTML JSON → LevelScaffoldData 导入脚本

> 脚本路径：`Assets/Scripts/Level/Editor/HtmlScaffoldImporter.cs`
> 命名空间：`ProjectArk.Level.Editor`

---

- [ ] 1. 定义 HTML JSON 中间反序列化数据类
   - 在 `HtmlScaffoldImporter.cs` 顶部定义以下内部类（`[Serializable]`），用于将 HTML JSON 反序列化为 C# 对象：
     - `HtmlLevelData`：顶层容器，包含 `List<HtmlRoom> rooms`、`List<HtmlConnection> connections`、`List<HtmlDoorLink> doorLinks`
     - `HtmlRoom`：`string id, string name, string type, int floor, float[] position, float[] size, List<HtmlElement> elements`，以及可选的 `string comment`（用于过滤注释对象）
     - `HtmlConnection`：`string from, string to, string fromDir, string toDir`，以及可选的 `string comment`
     - `HtmlDoorLink`：`string roomId, string entryDir, int doorIndex`
     - `HtmlElement`：`string type, float[] position`
   - 由于 `JsonUtility` 无法处理混有 comment 对象的异构数组，使用 `Newtonsoft.Json`（Unity 内置 `com.unity.nuget.newtonsoft-json`）或 .NET 内置 `System.Text.Json` 进行反序列化；如果项目已引入 Newtonsoft 则优先使用
   - _需求：8.5、边界情况 5_

- [ ] 2. 实现 EditorWindow 框架与 UI 布局
   - 创建 `HtmlScaffoldImporterWindow : EditorWindow`
   - 添加菜单入口 `[MenuItem("Window/ProjectArk/Import HTML Scaffold JSON")]`
   - EditorWindow 包含以下字段和控件：
     - `string _jsonFilePath`：JSON 文件路径，配合 `EditorUtility.OpenFilePanel("Select HTML Scaffold JSON", "", "json")`
     - `string _outputAssetPath`：输出路径，默认 `Assets/_Data/Level/Scaffolds/`，配合 `EditorUtility.SaveFilePanel`
     - `float _gridScale`：缩放因子，默认 `1.0f`
     - `Import` 按钮，当 `_jsonFilePath` 为空或文件不存在时禁用（`GUI.enabled = false`）
   - `OnGUI()` 中绘制所有控件
   - _需求：1.1、1.2、1.3、1.4_

- [ ] 3. 实现 JSON 解析与 comment 对象过滤
   - 在点击 Import 时读取 JSON 文件文本 `File.ReadAllText(_jsonFilePath)`
   - 使用 JSON 库反序列化为 `HtmlLevelData`
   - 过滤 `rooms` 列表：移除所有 `comment != null` 或 `id == null` 的条目
   - 过滤 `connections` 列表：移除所有 `comment != null` 或 `from == null` 的条目
   - 如果 JSON 解析失败则调用 `EditorUtility.DisplayDialog("Import Error", ...)` 并 return
   - _需求：2.2、3.2、7.2、边界情况 1_

- [ ] 4. 实现房间数据映射逻辑
   - 创建 `LevelScaffoldData` asset 实例（`ScriptableObject.CreateInstance<LevelScaffoldData>()`）
   - 维护 `Dictionary<string, ScaffoldRoom> htmlIdToRoom` 映射和 `Dictionary<string, string> htmlIdToGuid` 映射
   - 遍历过滤后的 `HtmlRoom` 列表，对每个房间：
     - 创建 `ScaffoldRoom`，通过反射或 public setter 设置：
       - `DisplayName = htmlRoom.name`
       - `RoomType` = 字符串映射（`"normal"→Normal, "arena"→Arena, "boss"→Boss, "safe"→Safe`，未知→Normal + 警告）
       - `Position = new Vector3(htmlRoom.position[0] * _gridScale, -htmlRoom.position[1] * _gridScale, 0)`（y 翻转）
       - `Size = new Vector2(htmlRoom.size[0] * _gridScale, htmlRoom.size[1] * _gridScale)`
     - 记录 `htmlIdToRoom[htmlRoom.id] = scaffoldRoom` 和 `htmlIdToGuid[htmlRoom.id] = scaffoldRoom.RoomID`
     - 调用 `scaffoldData.AddRoom(scaffoldRoom)`
   - _需求：2.1、2.3、2.4_

- [ ] 5. 实现元素类型映射与位置转换
   - 在房间遍历循环内，对每个 `HtmlRoom.elements` 中的 `HtmlElement`：
     - 创建 `ScaffoldElement`，设置 `ElementType` 按映射：
       - `"spawn"→PlayerSpawn, "enemy"→EnemySpawn, "checkpoint"→Checkpoint, "door"→Door, "chest"→CrateWooden, "npc"→Checkpoint`
       - 未知类型→`Hazard` + 警告
     - 计算 `LocalPosition`：
       - `localX = (element.position[0] - roomW/2) * _gridScale`
       - `localY = -(element.position[1] - roomH/2) * _gridScale`
       - `LocalPosition = new Vector3(localX, localY, 0)`
     - 如果类型为 `"door"`，调用 `element.EnsureDoorConfigExists()`
     - 如果类型为 `"npc"`，在元素 ID 后追加标记或输出日志 `"NPC placeholder mapped to Checkpoint"`
     - 调用 `scaffoldRoom.AddElement(element)`
   - 维护映射占位警告计数器 `int placeholderMappingCount`
   - _需求：4.1、4.2、4.3、4.4_

- [ ] 6. 实现连接数据转换（全局 connections → 房间内嵌 ScaffoldDoorConnection）
   - 遍历过滤后的 `HtmlConnection` 列表，对每个连接：
     - 查找 `from` 和 `to` 在 `htmlIdToRoom` 中的对应房间，不存在则跳过 + 警告 + `skippedCount++`
     - 创建 `ScaffoldDoorConnection`：
       - `TargetRoomID = htmlIdToGuid[connection.to]`
       - `DoorDirection` = 方向映射：`"east"→(1,0), "west"→(-1,0), "north"→(0,1), "south"→(0,-1)`
       - `DoorPosition` = 房间边缘中点计算（基于房间 Size 和方向）：
         - east: `(size.x/2, 0, 0)`
         - west: `(-size.x/2, 0, 0)`
         - north: `(0, size.y/2, 0)`
         - south: `(0, -size.y/2, 0)`
     - 查找 from 和 to 房间的 `floor` 值（从 `HtmlRoom` 中获取），如果不同则 `IsLayerTransition = true`
     - 调用 `fromRoom.AddConnection(connection)`
   - 维护 `skippedConnectionCount` 计数器
   - _需求：3.1、3.3、3.4、边界情况 2、3_

- [ ] 7. 实现 doorLinks 绑定逻辑
   - 遍历 `HtmlDoorLink` 列表，对每个条目：
     - 通过 `roomId` 查找 `htmlIdToRoom` 中的房间，未找到则警告 + 跳过
     - 如果 `doorIndex == -1`，输出信息日志 "No door element binding for room {roomId}" + 跳过
     - 在该房间的 `Elements` 中筛选类型为 `Door` 的元素列表
     - 如果 `doorIndex` 超出 Door 元素数量，警告 + 跳过
     - 获取第 `doorIndex` 个 Door 元素
     - 在该房间的 `Connections` 中查找 `DoorDirection` 与 `entryDir` 方向匹配的连接
     - 将 Door 元素的 `BoundConnectionID` 设为匹配连接的 `ConnectionID`
   - _需求：5.1、5.2、5.3_

- [ ] 8. 实现 Floor 层级处理
   - 在房间遍历完成后，统计所有房间的 `floor` 值频率 `Dictionary<int, int> floorCounts`
   - 如果所有房间 `floor` 相同，设置 `_floorLevel` 为该值（通过 `SerializedObject` 操作）
   - 如果存在多个不同 `floor` 值：
     - 取频率最高的作为 `_floorLevel`
     - 输出警告列出所有 floor 及其房间数量
     - 遍历非主 floor 的房间，在 `DisplayName` 后追加 `[F={floor}]`
   - 通过 `SerializedObject` 设置 `_floorLevel`（因为该字段是 private，无 public setter）
   - _需求：6.1、6.2、6.3_

- [ ] 9. 实现导入验证、日志输出与 Undo 支持
   - 在导入开始时调用 `Undo.RegisterCreatedObjectUndo(scaffoldData, "Import HTML Scaffold")`
   - 导入完成后调用 `AssetDatabase.CreateAsset(scaffoldData, outputPath)` + `AssetDatabase.SaveAssets()`
   - 在 Console 打印摘要日志，格式如：
     ```
     [HtmlScaffoldImporter] Import Complete:
       - Rooms: 38
       - Connections: 84
       - Elements: 52
       - Skipped connections: 0
       - Placeholder mappings (chest→CrateWooden, npc→Checkpoint): 3
       - Floor levels: {0: 25 rooms, 1: 8 rooms, -1: 5 rooms}
       - Output: Assets/_Data/Level/Scaffolds/Sheba_ACT1_ACT2.asset
     ```
   - 调用 `EditorUtility.DisplayDialog("Import Success", ...)` 显示成功提示
   - `Selection.activeObject = scaffoldData` 自动选中新创建的 asset
   - _需求：7.1、7.2、7.3_

- [ ] 10. 集成验证与 ImplementationLog 记录
   - 确认脚本文件在 `Assets/Scripts/Level/Editor/` 目录下，命名空间 `ProjectArk.Level.Editor`
   - 确认 `#if UNITY_EDITOR` 包裹或放在 Editor 程序集定义范围内（已有 `ProjectArk.Level.Editor.asmdef`，无需额外 `#if`）
   - 检查 `ProjectArk.Level.Editor.asmdef` 是否已引用 JSON 序列化库的程序集（如需 Newtonsoft 则确认引用）
   - 在 `Docs/ImplementationLog/ImplementationLog.md` 追加本次实现日志
   - _需求：8.1、8.2、8.3、8.4、8.5_
