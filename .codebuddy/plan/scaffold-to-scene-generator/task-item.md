# 实施计划：一键 Scaffold-to-Scene 关卡生成器

- [ ] 1. 创建 `ScaffoldToSceneGenerator.cs` 文件骨架与 EditorWindow 入口
   - 在 `Assets/Scripts/Level/Editor/` 下创建 `ScaffoldToSceneGenerator.cs`
   - 使用 `#if UNITY_EDITOR` 守卫，namespace `ProjectArk.Level.Editor`
   - 实现 `EditorWindow` 子类，菜单路径 `Window/ProjectArk/Generate Level From Scaffold`
   - 提供 `LevelScaffoldData` ObjectField 槽位和 "Generate" 按钮
   - 点击 "Generate" 时弹出确认对话框（提示房间数、资产数、覆盖风险）
   - 定义生成统计计数器结构体（roomCount, doorCount, roomSOCount, encounterSOCount, checkpointSOCount）
   - 添加 `EnsureDirectory` 和 `SanitizeName` 工具方法（参考 `ShebaLevelScaffolder.EnsureDirectory`）
   - _需求：1.1, 1.2, 1.3, 1.4_

- [ ] 2. 实现 Undo Group 包装与异常回滚机制
   - 在 Generate 入口方法中设置 `Undo.SetCurrentGroupName` + `Undo.GetCurrentGroup`
   - 用 try/catch/finally 包裹整个生成流程
   - catch 中调用 `Undo.RevertAllDownToGroup(undoGroup)` 回滚
   - finally 中调用 `Undo.CollapseUndoOperations(undoGroup)` 合并撤销操作
   - 参考 `LevelGenerator.GenerateLevel` 中的 Undo 模式
   - _需求：1.6, 8.1, 8.2_

- [ ] 3. 实现 Room GameObject 生成（Code-First，不依赖 Prefab）
   - 创建根节点 `--- {LevelName} ---`，用 `Undo.RegisterCreatedObjectUndo` 注册
   - 遍历 `scaffold.Rooms`，每个 `ScaffoldRoom` 创建 GameObject（名称=`_displayName`）
   - 添加 `Room` 组件 + `BoxCollider2D`（isTrigger=true, size=`_size`）
   - 设置 position 为 `_position`
   - 创建 `CameraConfiner` 子物体：Layer=2(Ignore Raycast)，`PolygonCollider2D`（isTrigger=false），顶点匹配房间边界矩形（hw=size.x/2, hh=size.y/2）
   - 通过 `SerializedObject` 设置 `Room._confinerBounds` 指向 PolygonCollider2D
   - 通过 `SerializedObject` 设置 `Room._playerLayer`（`LayerMask.GetMask("Player")`），如 "Player" Layer 不存在输出 Warning
   - 构建 `Dictionary<string, (GameObject go, Room room)>` 供后续使用
   - _需求：2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 2.8_

- [ ] 4. 实现 RoomSO 资产自动创建与关联
   - 对每个 ScaffoldRoom 创建/加载 RoomSO，路径 `Assets/_Data/Level/Rooms/{sanitizedDisplayName}_Data.asset`
   - 如果路径已有同名资产则 `AssetDatabase.LoadAssetAtPath` 加载并更新
   - 通过 `SerializedObject` 设置 `_roomID`、`_displayName`、`_floorLevel`、`_type`
   - 将 RoomSO 赋值给 Room 组件的 `_data` 字段
   - 自动创建 `Assets/_Data/Level/Rooms/` 目录（如不存在）
   - _需求：3.1, 3.2, 3.3, 3.4, 3.5_

- [ ] 5. 实现 Scaffold Element 实体化（PlayerSpawn、EnemySpawn、Checkpoint、占位物）
   - 遍历每个 ScaffoldRoom 的 `_elements` 数组
   - `PlayerSpawn`：创建空 GO `PlayerSpawn`，localPosition=element._localPosition
   - `EnemySpawn`：创建空 GO `EnemySpawn_{index}`，localPosition=element._localPosition，收集到列表供后续绑定 `_spawnPoints`
   - `Checkpoint`：创建 GO + `Checkpoint` 组件 + `BoxCollider2D`(isTrigger=true, 2×2)，设置 `_playerLayer`；创建 `CheckpointSO` 资产（保存到 `Assets/_Data/Level/Checkpoints/`），配置 `_checkpointID`、`_displayName`、`_restoreHP=true`、`_restoreHeat=true`，赋值给 Checkpoint 组件
   - `Door`（有 `_boundConnectionID`）：先跳过，在任务 6 中处理
   - 其他类型（Wall/WallCorner/CrateWooden/CrateMetal/Hazard）：创建空占位 GO，名称标注类型名
   - 将收集到的 EnemySpawn Transform 列表通过 `SerializedObject` 赋值给 `Room._spawnPoints`
   - _需求：5.1, 5.2, 5.3, 5.5, 7.3, 7.4_

- [ ] 6. 实现 Door 双向连接生成（基于 connections + boundConnectionID 双路径）
   - **路径A — 基于 connections 数组**：遍历每个 ScaffoldRoom 的 `_connections`，用 `HashSet<string>` 记录已处理的连接对（`min(A,B)+max(A,B)`）避免重复
   - 对每条 connection：在源房间创建 `Door_to_{targetDisplayName}` 子物体，添加 `Door` + `BoxCollider2D`(isTrigger=true, 3×3)，localPosition=`_doorPosition`
   - 在目标房间创建 SpawnPoint（位置取反向 connection 的 `_doorPosition`，或基于 `_doorDirection` 反方向偏移2单位）
   - 通过 `SerializedObject` 设置 `_targetRoom`、`_targetSpawnPoint`、`_isLayerTransition`、`_initialState=Open`、`_playerLayer`
   - 同时创建反向门（目标房间→源房间），复用已创建的 SpawnPoint
   - **路径B — 基于 element._boundConnectionID**：对 `ScaffoldElementType.Door` 元素，查找对应 connection 并应用 `_doorConfig`（`_initialState`、`_requiredKeyID`、`_openDuringPhases`）
   - 如果 boundConnectionID 指向的 connection 已在路径A中处理，则找到对应 Door 组件，覆盖 DoorConfig 字段
   - _需求：4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 4.7, 5.4, 7.2_

- [ ] 7. 实现 Arena/Boss 房间战斗配置（ArenaController + EnemySpawner + EncounterSO）
   - 对 `RoomType.Arena`(1) 和 `RoomType.Boss`(2) 房间：在 Room GO 上添加 `ArenaController` 组件
   - 创建 `EnemySpawner` 子物体 + `EnemySpawner` 组件，将房间中所有 EnemySpawn 的 Transform 绑定到 `_spawnPoints`
   - 创建 `EncounterSO` 资产（路径 `Assets/_Data/Level/Encounters/{sanitizedRoomName}_[DEFAULT]_Encounter.asset`）
   - Arena：1波 × 3个 Enemy_Rusher（delay=0s）
   - Boss：2波（第1波 2个 delay=0s，第2波 3个 delay=1.5s）
   - 从 `Assets/_Prefabs/Enemies/Enemy_Rusher.prefab` 加载 Prefab，如不存在则 EnemyPrefab=null + Warning
   - 将 EncounterSO 赋值给 `RoomSO._encounter`
   - 通过 `SerializedObject` 操作 EncounterSO 的 `_waves` 数组（参考 `ShebaLevelScaffolder.CreateEncounterSOAssets` 的实现模式）
   - _需求：6.1, 6.2, 6.3, 6.4, 6.5, 6.6, 6.7, 6.8_

- [ ] 8. 实现 Normal 房间有 EnemySpawn 时的战斗配置
   - 对 `RoomType.Normal`(0) 房间：检查其 elements 是否包含 `ScaffoldElementType.EnemySpawn`
   - 如有：创建 `EnemySpawner` 子物体并绑定 SpawnPoints（不添加 ArenaController）
   - 创建 EncounterSO：1波 × 2个 Enemy_Rusher（比 Arena 更少）
   - 赋值给 RoomSO._encounter
   - 如无 EnemySpawn 元素：跳过，不创建 EnemySpawner 或 EncounterSO
   - _需求：9.1, 9.2, 9.3, 9.4_

- [ ] 9. 实现生成完成后的验证报告和 TODO Checklist
   - 输出生成摘要：房间总数、门总数（含双向）、RoomSO 数、EncounterSO 数、CheckpointSO 数
   - 检查 Arena/Boss 房间中是否缺少 EnemySpawn 元素，输出 Warning
   - 输出 TODO checklist（Tilemap、替换 [DEFAULT] Encounter、Boss Prefab、装饰/光照/粒子、Checkpoint SpriteRenderer、Physics2D 碰撞矩阵）
   - 调用 `AssetDatabase.SaveAssets()` + `AssetDatabase.Refresh()`
   - _需求：1.5, 8.3, 8.4_

- [ ] 10. 集成测试与实现日志
   - 使用示巴星 scaffold asset（`Assets/_Data/Level/Scaffolds/示巴星___ACT1+ACT2__Z1a→Z2d_.asset`）进行端到端验证
   - 确认 37 个 Room GameObject 正确生成
   - 确认双向 Door 连接正常（门的 _targetRoom 和 _targetSpawnPoint 不为 null）
   - 确认 Arena/Boss 房间有 ArenaController + EnemySpawner + EncounterSO
   - 确认 Ctrl+Z 能撤销整个生成操作
   - 追加 `Docs/ImplementationLog/ImplementationLog.md` 实现日志
   - _需求：1.3, 1.6, 6.1, 8.1_
