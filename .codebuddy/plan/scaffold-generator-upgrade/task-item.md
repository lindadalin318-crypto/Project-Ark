# 实施计划：ScaffoldToSceneGenerator 效率升级

- [ ] 1. 需求8：SanitizeName 空格处理 + EditorWindow 路径预览
   - 在 `SanitizeName()` 中追加 `.Replace(" ", "_")` 处理空格
   - 在 `OnGUI()` 的 Scaffold Data 字段下方，用 `EditorGUILayout.HelpBox` 或灰色 `GUIStyle` 显示 `Output: Assets/_Data/Level/{SanitizedName}/`，`_scaffold` 为 null 时不显示
   - _需求：8.1、8.2、8.3_

- [ ] 2. 需求2：房间尺寸 Fallback 与异常警告
   - 新增私有静态方法 `GetFallbackSize(RoomType type)` 返回各类型默认尺寸（Normal=20×15，Arena=30×20，Boss=40×30，Corridor=20×8，Shop=15×12，其余=20×15）
   - 在 `Generate()` 遍历房间时，检测 `Size.x <= 0 || Size.y <= 0`，调用 fallback 并输出 `Debug.LogWarning`
   - 收集所有使用了 fallback 的房间名，在生成报告 TODO Checklist 中追加列出
   - _需求：2.1、2.2、2.3_

- [ ] 3. 需求3：Door 位置自动推算（边缘吸附）
   - 新增私有方法 `ResolveDoorPosition(ScaffoldDoorConnection door, Vector2 roomSize)` 根据 `DoorDirection` 返回边缘局部坐标
   - 在 `CreateElementGO()` 处理 Door 类型时，若 `DoorPosition == Vector3.zero` 则调用该方法；`DoorDirection` 为 None 时保留原 fallback 并输出 Warning
   - IF `DoorPosition` 已有非零值则直接使用，不覆盖
   - _需求：3.1、3.2、3.3、3.4、3.5、3.6、3.7_

- [ ] 4. 需求4：自动创建标准 Tilemap 层级
   - 新增私有方法 `CreateTilemapHierarchy(GameObject roomGO)`，在房间 GO 下创建 `Tilemaps` 子对象，再在其下创建 `Tilemap_Ground`（sortingOrder=0）、`Tilemap_Wall`（sortingOrder=1）、`Tilemap_Decoration`（sortingOrder=2），每层添加 `Tilemap` + `TilemapRenderer` 组件
   - 在 `CreateRoomGO()` 末尾调用该方法
   - 更新生成报告中 Paint Tilemaps 条目文字为"Tilemap 层级已自动创建，直接选中对应层开始绘制"
   - _需求：4.1、4.2、4.3、4.4、4.5_

- [ ] 5. 需求5：EncounterSO 按 EnemyTypeID 自动填充敌人 Prefab
   - 在 `ScaffoldElement` 数据类中新增 `EnemyTypeID` 字段（string，可序列化）
   - 修改 `CreateEncounterSO()` / `CreateOrUpdateEncounterSO()`：收集房间内所有 EnemySpawn 元素的 `EnemyTypeID`，按类型分组；对每种类型用 `AssetDatabase.LoadAssetAtPath` 从 `Assets/_Prefabs/Enemies/{EnemyTypeID}.prefab` 加载 Prefab，找不到则 fallback 到 `Enemy_Rusher.prefab` 并输出 Warning；在同一 Wave 中为每种类型创建独立 Entry
   - 生成报告中追加每个房间的敌人类型汇总
   - _需求：5.1、5.2、5.3、5.4、5.5_

- [ ] 6. 需求6：增量更新模式（Update Existing 复选框）
   - 在 EditorWindow 中新增 `_updateExisting` bool 字段，`OnGUI()` 中在 Generate 按钮旁渲染 `Toggle("Update Existing", _updateExisting)`
   - 在 `Generate()` 开头，若 `_updateExisting == true`，按房间 DisplayName 查找场景中已存在的同名 GO；存在则跳过重建，仅调用 `UpdateExistingRoom()`（更新 RoomSO 引用、BoxCollider2D.size、Door 组件属性），保留 Tilemap 子层级
   - `_updateExisting == false` 时保持原有全量覆盖行为；增量更新完成后在 Console 输出被保留的房间名称列表
   - _需求：6.1、6.2、6.3、6.4、6.5_

- [ ] 7. 需求7：Gizmo 统一开关（Toggle Gizmos 按钮）
   - 在 EditorWindow 中新增 `_gizmosVisible` bool 字段（默认 true），`OnGUI()` 中在 Generate 按钮下方渲染按钮，文字根据状态显示 `Hide Gizmos` 或 `Show Gizmos`
   - 新增私有方法 `ToggleGizmos()`：遍历场景中所有 `SpriteRenderer`（sortingOrder==1）和名为 `Label` 的 `MeshRenderer`，统一设置 `enabled` 为目标状态；若未找到任何对象则输出 `Debug.Log("No Gizmo visuals found in scene")`
   - 点击按钮后翻转 `_gizmosVisible` 并调用 `ToggleGizmos()`
   - _需求：7.1、7.2、7.3、7.4、7.5、7.6_
