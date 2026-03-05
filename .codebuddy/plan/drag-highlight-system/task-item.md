# 实施计划：拖拽放置高亮系统

## 现状说明

代码审查后确认：
- `StarChartTheme.HighlightValid/Invalid/Replace` **已存在** → 需求 5 已满足，无需新增
- `DragGhostView.SetDropState()` **已存在** → 需求 4 的 Ghost 同步接口已就绪
- `SlotCellView.SetShapeHighlight()` 当前直接修改 `_backgroundImage.color`，会污染格子原始状态，需改为独立 tile 层
- `ItemOverlayView` 在拖拽开始时不隐藏 → 需求 2.1 待实现

---

- [ ] 1. 新建 `DragHighlightLayer.cs` — 独立高亮 tile 管理器
   - 在 Track 的 Grid 容器上挂载，负责创建/复用/清除高亮 tile GameObject
   - 提供 `ShowHighlight(anchorCol, anchorRow, ItemShape, DropPreviewState, SlotType)` 和 `ClearHighlight()` 两个公共方法
   - 高亮 tiles 使用 `ItemIconRenderer.BuildShapeCellsAbsolute()` 构建，渲染在所有 `ItemOverlayView` 之上（`SetAsLastSibling`）
   - 复用策略：缓存上一次的 tile 数组，仅在 shape/anchor/state 变化时重建，否则只更新颜色
   - _需求：1.1、1.2、1.3、1.4、3.1、3.4_

- [ ] 2. 在 `TrackView` 中集成 `DragHighlightLayer`
   - `TrackView` 持有 `DragHighlightLayer` 引用（`[SerializeField]` 或 `GetComponent`）
   - 将现有 `SetShapeHighlight(col, row, shape, state, isCoreLayer)` 方法改为委托给 `DragHighlightLayer.ShowHighlight()`
   - 新增 `ClearShapeHighlight()` 方法，委托给 `DragHighlightLayer.ClearHighlight()`
   - _需求：1.1、2.3、2.4、3.3_

- [ ] 3. 修改 `SlotCellView.OnPointerEnter` — 调用新高亮层，不再直接改 `_backgroundImage`
   - `OnPointerEnter` 中将 `SetHighlight(valid)` / `SetReplaceHighlight()` 替换为调用 `OwnerTrack.ClearShapeHighlight()` + `OwnerTrack.SetShapeHighlight()`
   - `OnPointerExit` 中调用 `OwnerTrack.ClearShapeHighlight()`（替换现有的 `ClearHighlight()` 直接调用）
   - 保留 `SetHighlight` / `ClearHighlight` 方法供非拖拽场景使用（如键盘导航）
   - _需求：1.1、1.2、1.3、1.4、3.3、3.4_

- [ ] 4. 修改 `SlotCellView.OnPointerExit` — 延迟清除改为同帧清除
   - 移除现有的 `ClearDropTargetNextFrame` 协程（一帧延迟导致闪烁）
   - 改为同帧直接调用 `OwnerTrack.ClearShapeHighlight()`
   - 在 `DragDropManager` 中新增 `ClearAllHighlights()` 方法，遍历所有 TrackView 调用 `ClearShapeHighlight()`
   - _需求：1.5、3.2、3.3_

- [ ] 5. 修改 `DragDropManager.BeginDrag` — 拖拽开始时隐藏被拖部件的 overlay
   - 当 `payload.Source == DragSource.Slot` 时，找到对应 `ItemOverlayView` 并将其 `CanvasGroup.alpha = 0`（不销毁）
   - 记录被隐藏的 overlay 引用，供 `CleanUp` 时恢复
   - _需求：2.1_

- [ ] 6. 修改 `DragDropManager.CleanUp` — 拖拽结束时恢复 overlay 并清除所有高亮
   - 调用 `ClearAllHighlights()` 清除所有 TrackView 的高亮 tiles
   - 恢复步骤 5 中隐藏的 overlay（`CanvasGroup.alpha = 1`），或直接调用 `RefreshAllViews()` 重建
   - _需求：1.6、2.2、3.2_

- [ ] 7. 为 Sail / Sat 列的 `SlotCellView` 补充高亮支持
   - `SlotCellView.OnPointerEnter` 中，当 `SlotType == LightSail` 或 `SlotType == Satellite` 时，调用 `ApplySingleHighlight` 并同步更新 Ghost 状态
   - Sail：已有 Sail 装备 → 橙色（Replace），无装备 → 绿色（Valid），类型不匹配 → 红色（Invalid）
   - Sat：槽位已满 → 橙色（Replace），有空槽 → 绿色（Valid），类型不匹配 → 红色（Invalid）
   - `OnPointerExit` 时调用 `ClearHighlight()` 恢复格子原始颜色
   - _需求：6.1、6.2、6.3_

- [ ] 8. 验收测试（Play Mode 手动验证）
   - 拖拽 Core 部件悬停到 Core 列：所有占用格子绿色高亮，Ghost 边框绿色
   - 拖拽 L 形 Prism 悬停到已有部件的格子：橙色高亮（Replace），Ghost 显示"↺ 替换 N 个部件"
   - 拖拽 Core 部件悬停到 Prism 列：红色高亮，Ghost 边框红色
   - 鼠标离开格子：高亮立即消失，无残影，无闪烁
   - 从 Track 拖出已装备部件：原 overlay 立即隐藏（alpha=0），Ghost 正常显示
   - 拖拽结束（成功/取消）：所有高亮清除，overlay 恢复
   - _需求：1.1-1.6、2.1-2.4、3.1-3.4、4.1-4.4、6.1-6.3_
