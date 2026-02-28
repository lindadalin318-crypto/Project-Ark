# 实施计划：StarChart UI Phase B — 数据模型视图层重构

## 待办事项

- [ ] 1. 扩展 `SlotCellView.cs`：新增 `SlotType` 枚举和 SAIL/SAT 类型匹配
   - 新增 `SlotType` 枚举（`Core / Prism / LightSail / Satellite`），替换 `IsCoreCell` bool
   - `OnPointerEnter` 中的类型匹配逻辑扩展为 4 种类型
   - 新增 `HasSpaceForItem()` 委托，由 `TrackView` 在初始化时注入（支持 SAIL/SAT 的空间检测）
   - 新增 `IsOccupied` 属性（槽位有部件时为 true），用于替换高亮判断
   - `OnPointerEnter` 中：类型匹配 + 空位 → 绿色；类型匹配 + 有部件 → 橙色；类型不匹配 → 红色
   - _需求：4.1、4.2、4.3、5.1、5.2、5.3、5.4_

- [ ] 2. 扩展 `TrackView.cs`：新增 SAIL/SAT 槽位列
   - 新增 `[SerializeField] SlotCellView _sailCell` 和 `[SerializeField] SlotCellView[] _satCells`（2格）
   - 新增 `[SerializeField] TMP_Text _sailLabel` 和 `[SerializeField] TMP_Text _satLabel`
   - `Bind()` 中注入 `StarChartController` 引用（用于读取 SAIL/SAT 装备状态）
   - `Refresh()` 中刷新 SAIL/SAT 槽位（调用 `controller.GetEquippedLightSail()` / `GetEquippedSatellites()`）
   - `HasSpaceForItem()` 扩展支持 SAIL/SAT 类型
   - `SetMultiCellHighlight()` 扩展支持 SAIL/SAT 槽位
   - _需求：1.1、1.2、1.3、1.4、1.5、1.6、5.1_

- [ ] 3. 扩展 `DragDropManager.cs`：强制替换逻辑
   - `EquipToTrack()` 中：若 `TryEquip` 失败（空间不足），先调用 `EvictBlockingItems()` 卸载阻碍部件，再重试
   - 新增 `EvictBlockingItems(StarChartItemSO newItem, WeaponTrack track, bool isCoreLayer)` 方法：找出所有阻碍放置的部件并卸载
   - 新增 `EvictedItems` 列表，记录本次替换中被顶出的部件（供 Phase C 飞回动画使用）
   - 替换成功后调用 `_panel.StatusBar.ShowMessage("REPLACED: ...", orange, 3f)`
   - 扩展 `EquipToTrack()` 支持 SAIL/SAT 类型（调用 `controller.EquipLightSail()` / `EquipSatellite()`）
   - _需求：3.1、3.2、3.3、3.4、3.5、3.6、3.7、2.1、2.2_

- [ ] 4. 扩展 `StarChartPanel.cs`：暴露 StatusBar 和 Controller 给 DragDropManager
   - 新增 `public StatusBarView StatusBar => _statusBar` 属性
   - 新增 `public StarChartController Controller => _controller` 属性
   - `EquipItem()` 中的强制替换逻辑委托给 `DragDropManager`（避免重复实现）
   - _需求：3.3_

- [ ] 5. 更新 `UICanvasBuilder.cs`：生成 4 列 TrackView 布局
   - `BuildTrackView()` 中新增 SAIL 列（1格）和 SAT 列（2格）
   - 4 列横向排列，每列宽度 25%，列间细分隔线
   - 自动连线 `_sailCell`、`_satCells`、`_sailLabel`、`_satLabel` 字段
   - _需求：6.1、6.2、6.3_
