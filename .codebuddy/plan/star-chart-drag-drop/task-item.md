# 实施计划：星图拖拽装备 (Star Chart Drag & Drop)

- [ ] 1. 创建拖拽数据载体 `DragPayload` 和全局拖拽管理器 `DragDropManager`
   - 新建 `Assets/Scripts/UI/DragDrop/DragPayload.cs`：轻量数据类，持有 `StarChartItemSO Item`、拖拽来源枚举（Inventory / Slot）、来源轨道引用 `WeaponTrack SourceTrack`（从槽位拖出时非空）
   - 新建 `Assets/Scripts/UI/DragDrop/DragDropManager.cs`（MonoBehaviour，单例，挂在 Canvas 上）：
     - 管理全局拖拽状态：`bool IsDragging`、`DragPayload CurrentPayload`
     - 持有对 `StarChartPanel` 的引用（通过 `Bind()` 注入），用于调用装备/卸载/刷新逻辑
     - 提供 `BeginDrag(DragPayload)`、`EndDrag(bool success)` 和 `CancelDrag()` 公共方法
     - `CancelDrag()` 方法将在面板关闭时由 `StarChartPanel.Close()` 调用，清理幽灵并恢复所有视觉状态
     - 所有逻辑不依赖 `Time.deltaTime`，天然兼容 `timeScale=0`
   - _需求：1.1, 1.5, 5.1, 5.2, 边界情况-面板关闭_

- [ ] 2. 创建拖拽幽灵视图 `DragGhostView`
   - 新建 `Assets/Scripts/UI/DragDrop/DragGhostView.cs`（MonoBehaviour）：
     - 持有一个 `Image` 组件，显示被拖拽物品的图标/占位色块（复用 `InventoryItemView` 中的 `GetPlaceholderColor` 逻辑）
     - `Show(StarChartItemSO item)` 方法：设置图标并将 alpha 设为半透明（如 0.7）
     - `FollowPointer()` 方法：在 `OnDrag` 回调中调用，将自身 RectTransform 的位置设为鼠标位置（使用 `RectTransformUtility.ScreenPointToLocalPointInRectangle`）
     - `Hide()` 方法：禁用 GameObject
   - 新建 `Assets/_Prefabs/UI/DragGhost.prefab`：Canvas 下的 UI 元素，挂载 `DragGhostView`，设置 `CanvasGroup.blocksRaycasts = false`（不遮挡下方的 drop target 射线检测），`Canvas.overrideSorting = true` 且 sortingOrder 足够高
   - _需求：1.1, 1.3, 1.4, 5.1_

- [ ] 3. 为 `InventoryItemView` 添加拖拽源能力
   - 修改 `Assets/Scripts/UI/InventoryItemView.cs`：
     - 实现 `IBeginDragHandler`、`IDragHandler`、`IEndDragHandler` 接口
     - `OnBeginDrag`：检查 `Item.ItemType` 是否为 Core 或 Prism（LightSail/Satellite 不启动拖拽），调用 `DragDropManager.Instance.BeginDrag(payload)`，降低自身 CanvasGroup alpha 至 0.4
     - `OnDrag`：调用 `DragGhostView.FollowPointer()`
     - `OnEndDrag`：如果拖拽未被 drop target 消费，调用 `DragDropManager.Instance.CancelDrag()`，恢复自身 alpha 至 1.0
   - 为 `InventoryItemView` prefab 添加 `CanvasGroup` 组件（用于控制 alpha）
   - _需求：1.1, 1.2, 1.5, 边界情况-LightSail/Satellite_

- [ ] 4. 为 `SlotCellView` 添加拖放目标能力
   - 修改 `Assets/Scripts/UI/SlotCellView.cs`：
     - 实现 `IDropHandler`、`IPointerEnterHandler`、`IPointerExitHandler` 接口
     - 新增公共属性 `bool IsCoreCell`（由 `TrackView` 在初始化时设置），用于类型匹配判断
     - 新增公共属性 `int CellIndex`（0-2），由 `TrackView` 设置，用于 SlotSize>1 的多格高亮
     - 新增公共属性 `TrackView OwnerTrack`，由 `TrackView` 设置
     - `OnPointerEnter`：当 `DragDropManager.IsDragging` 时，执行类型匹配校验（Core→CoreCell, Prism→PrismCell）和空间校验（通过 `OwnerTrack.Track` 访问 `SlotLayer.FreeSpace`），调用 `SetHighlight(valid)` 显示绿/红高亮；若 SlotSize>1 则通知 `OwnerTrack` 高亮相邻格子
     - `OnPointerExit`：调用 `ClearHighlight()`，清除相邻格子高亮
     - `OnDrop`：若高亮为有效（绿色），通知 `DragDropManager.Instance.EndDrag(true)` 并传递目标轨道和格子信息
   - _需求：2.1, 2.2, 2.3, 2.4, 2.5, 2.6_

- [ ] 5. 为 `TrackView` 添加拖拽辅助方法和自动选中逻辑
   - 修改 `Assets/Scripts/UI/TrackView.cs`：
     - 实现 `IPointerEnterHandler` 接口
     - `OnPointerEnter`：当 `DragDropManager.IsDragging` 时，触发 `OnTrackSelected` 事件，使 `StarChartPanel` 自动切换 `_selectedTrack`
     - 新增 `SetMultiCellHighlight(int startIndex, int count, bool isCoreLayer, bool valid)` 方法：为 SlotSize>1 的物品高亮连续多个格子
     - 新增 `ClearAllHighlights()` 方法：清除所有格子的高亮状态
     - 在初始化阶段为每个 `SlotCellView` 设置 `IsCoreCell`、`CellIndex`、`OwnerTrack` 属性
   - _需求：3.1, 3.2, 2.6_

- [ ] 6. 在 `DragDropManager` 中实现装备/卸载执行逻辑
   - 在 `DragDropManager.cs` 中实现 `ExecuteDrop()` 方法：
     - 根据 `DragPayload.Source` 判断操作类型：
       - Inventory → Slot：调用 `WeaponTrack.EquipCore/EquipPrism`，若为 StarCoreSO 则调用 `InitializePools()`
       - Slot → 另一个 Slot（跨轨道）：先从 `SourceTrack` 卸载，再在目标轨道装备
       - Slot → Inventory 区域：调用 `WeaponTrack.UnequipCore/UnequipPrism`
     - 操作完成后调用 `StarChartPanel.RefreshAll()`（需将 `RefreshAll` 改为 `public` 或通过事件通知）
     - 自动选中刚装备的物品到详情面板
   - 确保每次只允许一个拖拽操作进行
   - _需求：2.3, 4.1, 4.2, 4.3, 6.2, 6.4, 边界情况-快速连续拖放_

- [ ] 7. 为 `SlotCellView` 添加拖拽源能力（已装备物品拖出）
   - 修改 `Assets/Scripts/UI/SlotCellView.cs`：
     - 实现 `IBeginDragHandler`、`IDragHandler`、`IEndDragHandler` 接口
     - `OnBeginDrag`：仅当 `DisplayedItem != null` 时启动拖拽，创建 `DragPayload`（Source=Slot, SourceTrack=当前轨道），调用 `DragDropManager.BeginDrag()`
     - `OnDrag`/`OnEndDrag`：与 `InventoryItemView` 类似的幽灵跟随和取消逻辑
   - _需求：6.1, 6.3_

- [ ] 8. 为 `InventoryView` 添加拖放目标能力（接收槽位拖出的物品）
   - 修改 `Assets/Scripts/UI/InventoryView.cs`：
     - 实现 `IDropHandler` 接口
     - `OnDrop`：当 `DragPayload.Source == Slot` 时，通知 `DragDropManager` 执行卸载操作
   - _需求：6.2_

- [ ] 9. 在 `StarChartPanel` 中集成拖拽系统并处理面板关闭清理
   - 修改 `Assets/Scripts/UI/StarChartPanel.cs`：
     - 在 `Bind()` 中初始化 `DragDropManager`（调用 `DragDropManager.Instance.Bind(this, _controller)`）
     - 将 `RefreshAll()` 改为 `public`（或提供 `RequestRefresh()` 公共方法）供 `DragDropManager` 调用
     - 新增 `public void SelectAndShowItem(StarChartItemSO item)` 方法，设置 `_selectedItem` 并更新详情面板
     - 在 `Close()` 方法中调用 `DragDropManager.Instance.CancelDrag()` 确保拖拽中关闭面板时清理幽灵
   - _需求：4.1, 4.3, 边界情况-面板关闭_

- [ ] 10. 更新 ImplementationLog 记录本次功能实现
   - 在 `Docs/ImplementationLog/ImplementationLog.md` 追加记录
     - 标题含功能名称和确切时间
     - 列出所有新建/修改的文件路径
     - 简述拖拽交互的技术实现（uGUI EventSystem 拖拽接口 + 单例管理器模式）
   - _需求：全部_
