# 实施计划：槽位层统一化（SAT / Core / Prism / SAIL）

- [ ] 1. 修改 `WeaponTrack.cs`：SAT 层改为动态列扩展
   - 将 `_satLayer = new SlotLayer<SatelliteSO>(initialCols: 2)` 改为 `initialCols: 1`（与 Core/Prism 默认行为一致）
   - 在 `SetLayerCols(int coreCols, int prismCols)` 方法中新增 `satCols` 参数，并添加 SAT 层的 `TryUnlockColumn()` 循环（与 Core/Prism 的扩展逻辑完全一致）
   - _需求：1.1、1.2_

- [ ] 2. 修改 `TrackView.cs`：新增 `_debugSatCols` Inspector 字段
   - 在 `[Header("Debug: Slot Counts (0 = use save data)")]` 下新增 `[SerializeField] [Range(0, 4)] private int _debugSatCols = 0;`
   - 修改 `ApplyDebugSlotCounts()` 方法：将 `_track.SetLayerCols(coreCols, prismCols)` 改为 `_track.SetLayerCols(coreCols, prismCols, satCols)`，satCols 同样遵循"0 = 使用存档数据"逻辑
   - _需求：1.3、1.4、3.1_

- [ ] 3. 修改 `StarChartController.cs`：SAIL 改用 `SlotLayer<LightSailSO>`
   - 将原有的单引用字段（`_equippedLightSail`）替换为 `SlotLayer<LightSailSO> _sailLayer`（`initialCols: 1`）
   - 修改 `EquipLightSail(LightSailSO sail)` 为 `EquipLightSail(LightSailSO sail, int anchorCol = 0, int anchorRow = 0)`，内部调用 `_sailLayer.TryPlace(sail, anchorCol, anchorRow)`
   - 修改 `UnequipLightSail()` / `UnequipLightSail(LightSailSO sail)` 调用 `_sailLayer.Unequip(sail)`
   - 修改 `GetEquippedLightSail()` 返回 `_sailLayer.Items.Count > 0 ? _sailLayer.Items[0] : null`（向后兼容）
   - 新增 `SailLayer` 属性暴露 `SlotLayer<LightSailSO>`，供 `StarChartPanel` 和 `DragDropManager` 使用
   - 新增 `SetSailLayerCols(int cols)` 方法，内部循环调用 `_sailLayer.TryUnlockColumn()`
   - _需求：2.1、2.2、2.3_

- [ ] 4. 修改 `StarChartPanel.cs`：SAIL Inspector 字段统一化 + 刷新逻辑适配 SlotLayer
   - 将 `[Range(1, 4)] private int _sailSlotCount = 1` 替换为 `[Range(0, 4)] private int _debugSailCols = 0`
   - 修改 `Bind()` 中的 `HasSpaceForItem` delegate：改为检查 `_controller.SailLayer.FreeSpace > 0`
   - 修改 `RefreshSharedSailColumn()`：将 `int unlockedCount = _sailSlotCount` 改为从 `_controller.SailLayer` 读取 `Rows * Cols`，并遍历 SlotLayer 的每个位置显示对应 SAIL（与 `TrackView.RefreshColumn` 逻辑一致）
   - 在 `Bind()` 末尾添加：若 `_debugSailCols > 0` 则调用 `_controller.SetSailLayerCols(_debugSailCols)`
   - _需求：2.4、2.5、2.6、3.2、3.3_

- [ ] 5. 修改 `SlotCellView.cs`：SAIL 的 `ComputeDropValidity` 支持多格 anchor 计算
   - 在 SAIL 类型的 drop validity 判断中，参考 SAT/Core/Prism 的 anchor 计算逻辑，根据 `CellIndex` 计算正确的 `hoverCol` 和 `hoverRow`（而非始终返回 `Vector2Int.zero`）
   - _需求：2.7_

- [ ] 6. 修改 `DragDropManager.cs`：SAIL drop 逻辑适配 SlotLayer 位置
   - 在 SAIL 的 `ExecuteEquipDrop()` 中，将 `_controller.EquipLightSail(sail)` 改为 `_controller.EquipLightSail(sail, DropTargetAnchorCol, DropTargetAnchorRow)`，使 SAIL 能放置到正确的 SlotLayer 位置
   - 在 SAIL 的 drop validity 检查中，将 `GetEquippedLightSail() == null` 改为检查 `SailLayer.CanPlace(sail, anchorCol, anchorRow)`，支持多格满格时显示"NO SPACE"
   - _需求：2.7、2.8_

- [ ] 7. 修改所有调用 `SetLayerCols(coreCols, prismCols)` 的地方（存档导入）
   - 搜索 `SetLayerCols` 的所有调用点（预期在 `StarChartController.cs` 的 `ImportTrack` 方法中）
   - 将调用改为 `SetLayerCols(coreCols, prismCols, satCols)`，satCols 从存档数据中读取（若无则默认 1）
   - _需求：1.2、4.1、4.2、4.3_

- [ ] 8. 编译验证 + 存档兼容性检查
   - 确认所有编译错误为零（重点检查 `SetLayerCols` 签名变更的影响范围）
   - 在 Inspector 中验证：`TrackView` 显示 `_debugCoreCols`、`_debugPrismCols`、`_debugSatCols` 三个字段；`StarChartPanel` 显示 `_debugSailCols` 字段
   - 验证旧存档（不含 SAT/SAIL 列数）加载时不崩溃，默认使用 `initialCols: 1`
   - _需求：3.1、3.2、4.1_
