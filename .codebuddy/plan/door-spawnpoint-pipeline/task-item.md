# 实施计划：门元素位置 → SpawnPoint 管线打通

- [ ] 1. `ScaffoldDoorConnection` 新增 `_spawnOffset` / `SpawnOffset` 字段
   - 在 `Assets/Scripts/Level/Data/LevelScaffoldData.cs` 的 `ScaffoldDoorConnection` 类中新增序列化字段 `[SerializeField] private Vector3 _spawnOffset;`
   - 新增公开属性 `public Vector3 SpawnOffset { get => _spawnOffset; set => _spawnOffset = value; }`
   - 默认值为 `Vector3.zero`（零向量），确保旧 .asset 向后兼容
   - _需求：2.4_

- [ ] 2. `LevelDesigner.html` 导出 doorLink 时附带 `spawnOffset`
   - 修改 `getExportData()` 函数中 `doorLinks` 的 `.map()` 逻辑
   - 通过 `doorLink.doorIndex` 在对应房间的 `elements` 数组中找到门元素
   - 将门元素的 `[x / GRID_SIZE, y / GRID_SIZE]` 作为 `spawnOffset` 字段写入导出对象
   - 若 `doorIndex` 无效或门元素不存在，则不包含 `spawnOffset` 字段
   - _需求：1.1、1.2、1.3_

- [ ] 3. `HtmlScaffoldImporter.cs` 解析 `spawnOffset` 字段
   - 3.1 在 `HtmlDoorLink` 反序列化类中新增 `public float[] SpawnOffset;` 字段
   - 3.2 在 Step 5 的 `foreach (var link in htmlData.DoorLinks)` 循环中，当 `matchingConn != null` 时，检查 `link.SpawnOffset` 是否非空且长度 ≥ 2
   - 3.3 若存在，执行坐标转换（HTML左上角原点 → Unity房间中心原点）：参照已有的 `ConvertElementPosition` 方法（或同等逻辑），将 `link.SpawnOffset` 转为 Unity 本地坐标 `Vector3`，赋值给 `matchingConn.SpawnOffset`
   - 3.4 新增计数器 `spawnOffsetApplied`，导入完成后日志输出应用了自定义 spawnOffset 的 doorLink 数量
   - _需求：2.1、2.2、2.3_

- [ ] 4. `ScaffoldToSceneGenerator.cs` Phase 4 使用 `SpawnOffset` 生成 SpawnPoint
   - 4.1 **正向门的 SpawnPoint**（`fwdSpawn`，放在 target 房间中）：当前使用 `reverseSpawnPos`（来自 `FindReverseDoorPosition`）。修改为：先检查反向 connection 的 `SpawnOffset`，若 `!= Vector3.zero` 则使用它作为 `fwdSpawn.transform.localPosition`，否则回退到现有的 `FindReverseDoorPosition` 逻辑
   - 4.2 **反向门的 SpawnPoint**（`revSpawn`，放在 source 房间中）：当前使用 `conn.DoorPosition`。修改为：先检查正向 connection（`conn`）的 `SpawnOffset`，若 `!= Vector3.zero` 则使用它作为 `revSpawn.transform.localPosition`，否则回退到现有的 `conn.DoorPosition` 逻辑
   - 4.3 添加日志输出，标明哪些 SpawnPoint 使用了自定义 SpawnOffset
   - _需求：3.1、3.2、3.3、3.4_

- [ ] 5. 记录实现日志
   - 追加 `Docs/ImplementationLog/ImplementationLog.md`，记录本次修改的所有文件、内容简述、目的和技术方案
   - _项目强制要求：ImplementationLog_
