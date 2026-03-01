# 实施计划：StarChart UI 布局重构

- [ ] 1. 重构 `TrackView.cs`：从线性槽位改为 4 列 TypeColumn 数据模型
   - 新增 `TypeColumn` 内部类/结构，持有类型标识、4 个 `SlotCellView` 引用（2×2）
   - 移除原有线性 `_cells` 列表，改为 `_columns[4]` 数组
   - 更新 `Refresh()`、`RefreshSailCell()` 等方法，按列分组写入 cell 数据
   - 更新 `GetCellAt()` / `FindCellForItem()` 等查询方法适配新索引结构
   - _需求：1.1、1.3、1.6、1.7_

- [ ] 2. 新增 `SlotCellView` 的 Item Overlay 渲染逻辑
   - 在 `SlotCellView` 中新增 `SetOverlay(StarChartItemSO item, Vector2Int size)` 方法
   - 空 cell 显示半透明 "+" 占位符；有物品时显示 Item Overlay（图标 + 类型色边框）
   - 多格物品（SlotSize=2）由 Overlay 跨 cell 覆盖，单 cell 不重复渲染图标
   - _需求：1.4、1.5、1.6、1.7_

- [ ] 3. 新增 `LoadoutSwitcher.cs` 组件（Gatling 列逻辑）
   - 创建 `Assets/Scripts/UI/LoadoutSwitcher.cs`
   - SerializeField：▲ Button、▼ Button、DrumCounter TMP_Text、LoadoutCard RectTransform
   - MVP：单 Loadout 模式下禁用按钮；预留 `SwitchTo(int index)` 接口供后续多 Loadout 扩展
   - 鼓形计数器显示 "LOADOUT #1 · 1/1"
   - _需求：2.1、2.4、2.5_

- [ ] 4. 重构 `StarChartPanel.cs`：面板整体结构改为 Loadout 卡片模式
   - 新增 `[SerializeField] LoadoutSwitcher _loadoutSwitcher` 引用
   - 新增 `[SerializeField] RectTransform _loadoutCard` 引用（用于滑动动画）
   - `Open()` / `Close()` 动画序列更新：加入 LoadoutCard 滑入/滑出动画（PrimeTween，`useUnscaledTime: true`）
   - `RefreshAll()` 改为通过 LoadoutSwitcher 刷新当前 Loadout 的 PRIMARY + SECONDARY 轨道
   - _需求：3.1、3.2、3.3、3.4、3.5_

- [ ] 5. 重构 `UICanvasBuilder.cs`：生成新布局层级
   - 更新 `BuildStarChartPanel()` 方法，生成新层级：`Header / LoadoutSection(GatlingCol + LoadoutCard) / SectionDivider / InventorySection / StatusBar`
   - `BuildTrackView()` 改为生成 4 个 TypeColumn，每列含列头（TMP_Text + 彩色圆点 Image）+ 2×2 GridContainer（4 个 SlotCellView）
   - `BuildGatlingCol()` 新方法：生成 ▲ Button、DrumCounter TMP_Text、▼ Button、LOADOUT 标签，挂载 `LoadoutSwitcher` 组件并连线
   - 保持幂等性（`GetOrCreate` 模式），执行完成后自动连线所有 SerializeField
   - _需求：5.1、5.2、5.3、5.4、5.5_

- [ ] 6. 应用视觉风格：颜色、Header、角落装饰、状态栏
   - 在 `UICanvasBuilder` 中新增 `ApplyTheme()` 方法，统一设置所有 Image/TMP_Text 颜色
   - 面板背景 `#0b1120`，轨道区 `#0f1828`，类型色 SAIL/PRISM/CORE/SAT 按规范
   - Header：青色脉冲圆点（Animator 或 PrimeTween 循环）+ 标题文字 + "SYSTEM ONLINE" 徽章 + 关闭按钮
   - 四角 L 形装饰：4 个 Image 组件，青色 `#22d3ee`，2px 边框，20×20px
   - 状态栏格式：`EQUIPPED X/10 · INVENTORY X ITEMS · DRAG TO EQUIP · CLICK TO INSPECT`
   - _需求：4.1、4.2、4.3、4.4、4.5、4.6_

- [ ] 7. 适配拖拽系统：`DragDropManager.cs` 支持 TypeColumn 目标检测
   - 拖拽开始时，高亮所有与物品类型匹配的 TypeColumn（发光动画，PrimeTween）
   - 悬停时调用 `TypeColumn.PreviewDrop(item)` 返回 `DropPreviewResult`（可放置/替换/越界/类型不匹配），对应绿/橙/红高亮
   - 释放时调用 `TypeColumn.CommitDrop(item)`，触发 snap-in 动画；替换时触发 fly-back 动画并更新状态栏
   - 从已装备 Overlay 拖拽到背包区域时执行卸装
   - _需求：6.1、6.2、6.3、6.4、6.5、6.6_

- [ ] 8. 类型列边框 hover 动画与 cell 交互反馈
   - `TypeColumn` 实现 `IPointerEnterHandler` / `IPointerExitHandler`，hover 时边框从 dim 色渐变到亮色（PrimeTween，`useUnscaledTime: true`）
   - `SlotCellView` 在拖拽悬停时播放缩放脉冲动画（scale 1.0 → 1.05 → 1.0）
   - _需求：1.8、6.2_

- [ ] 9. 执行 `Build UI Canvas` 验收并记录实现日志
   - 在 Unity Editor 中执行 `ProjectArk > Build UI Canvas`，进入 Play Mode 逐条验证验收标准
   - 验证：4 列布局正确显示、颜色正确、Loadout 切换器存在、拖拽高亮正常、fly-back 动画触发
   - 追加实现日志到 `Docs/ImplementationLog/ImplementationLog.md`
   - _需求：1–6 全部_
