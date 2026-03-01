# 实施计划：StarChart UI 全面排查与修复

- [ ] 1. 修复初始化 Bug：面板默认隐藏 + CanvasGroup 防穿透
   - 在 `StarChartPanel.Awake()` 中将 CanvasGroup 初始化为 `alpha=0, interactable=false, blocksRaycasts=false`，并调用 `gameObject.SetActive(false)`
   - 在 `UIManager.Start()` 中确保 `Bind()` 之后无论 controller 是否为 null 都执行 `panel.gameObject.SetActive(false)`
   - 在 `UICanvasBuilder` 构建完成后对 StarChartPanel 执行 `SetActive(false)` 并初始化 CanvasGroup
   - _需求：1.1、1.2、1.3、1.4_

- [ ] 2. 修复面板无法点击：Open/Close 正确切换 CanvasGroup 状态
   - `Open()` 动画完成回调中设置 `interactable=true, blocksRaycasts=true`
   - `Close()` 动画开始时立即设置 `interactable=false, blocksRaycasts=false`，动画完成后 `SetActive(false)`
   - 确认 `UIManager.ConfigureUIInputModule()` 在 `Start()` 中被调用，InputSystemUIInputModule 正确连线
   - _需求：2.1、2.2、2.3、2.4_

- [ ] 3. 修复布局锚点：对齐 HTML 原型的比例分区
   - 在 `UICanvasBuilder.BuildStarChartSection()` 中调整各区域锚点：Header 6%、LoadoutSection 55%（GatlingCol 6.5% + LoadoutCard 93.5%）、InventorySection 38%、StatusBar 4%
   - 修正 `BuildTrackView()` 中 TypeColumn 的锚点（SAIL 0-24%、PRISM 25-49%、CORE 51-74%、SAT 75-99%）
   - 修正过滤按钮顺序为 ALL → SAILS → PRISMS → CORES → SATS
   - _需求：3.1、3.2、3.3、3.4_

- [ ] 4. 对齐视觉风格：颜色、Header 装饰、角落 Bracket
   - 核查 `StarChartTheme.cs` 中所有颜色常量与原型 CSS 变量一致（BgPanel `#0b1120`、BgTrack `#0f1828`、Cyan `#67e8f9`、SAIL `#34d399`、PRISM `#a78bfa`、CORE `#60a5fa`、SAT `#fbbf24`）
   - 在 `UICanvasBuilder.BuildHeader()` 中添加：青色脉冲圆点（PrimeTween alpha 循环）、SYSTEM ONLINE 徽章、关闭按钮 hover 变红效果
   - 确认 `BuildCornerBrackets()` 生成四角 L 形装饰并正确连线
   - 更新状态栏默认文字为 `EQUIPPED 0/10 · INVENTORY X ITEMS · HOVER TO INSPECT · DRAG ITEM TO SLOT`
   - _需求：4.1、4.2、4.3、4.4、4.5、3.5_

- [ ] 5. 完善 ItemDetailView：属性列表 + 装备状态标签
   - 在 `ItemDetailView.ShowItem()` 中渲染：物品图标、名称、类型标签（带类型颜色）、分隔线、属性列表（↑↓ 箭头 + 数值）、描述文字、装备状态标签（绿色 EQUIPPED / 灰色 NOT EQUIPPED）
   - 操作按钮根据装备状态显示 `EQUIP` 或 `UNEQUIP`
   - SlotCellView 的 `OnPointerEnter` 在非拖拽状态下触发 `StarChartPanel.SelectAndShowItem()`
   - _需求：5.1、5.2、5.3、5.4_

- [ ] 6. 完善背包：已装备角标 + 过滤状态保持
   - 在 `InventoryItemView` 中添加绿色 `✓` 角标 Image，当 `isEquipped=true` 时显示
   - `InventoryView.Refresh()` 刷新时保持当前 `_activeFilter` 不重置
   - 确认 ScrollRect 垂直滚动正常（Content 的 `ContentSizeFitter` 设置为 `VerticalFit=PreferredSize`）
   - _需求：7.1、7.2、7.3、7.4_

- [ ] 7. 完善拖拽系统：TypeColumn 高亮 + Ghost 状态颜色
   - 确认 `DragDropManager.HighlightMatchingColumns()` 在 `BeginDrag` 时正确调用，拖拽开始时匹配列边框高亮
   - 在 `SlotCellView.OnPointerEnter` 拖拽状态下，根据 `HasSpaceForItem` 结果调用 `TypeColumn.SetDropHighlight(valid)` 并更新 Ghost 边框颜色
   - 确认 `CancelDrag()` 路径下所有高亮被清除，Ghost 隐藏，物品飞回背包
   - _需求：6.1、6.2、6.3、6.4、6.5_

- [ ] 8. UICanvasBuilder 幂等性与构建完整性验收
   - 确认 `BuildStarChartSection()` 末尾执行 `panel.gameObject.SetActive(false)` 并初始化 CanvasGroup
   - 确认重复执行 `Build UI Canvas` 时先销毁旧 Canvas 再重建（幂等性）
   - 在构建完成日志中列出所有已连线组件（TrackView × 2、TypeColumn × 8、SlotCellView × 32、LoadoutSwitcher、InventoryView、ItemDetailView、StatusBarView、DragDropManager）
   - _需求：8.1、8.2、8.3、8.4、8.5_
