# 实施计划：StarChart UI 完善（对齐 HTML 原型）

- [ ] 1. 扩展 `StarChartItemSO` 数据模型：新增 `ItemShape` 枚举与形状坐标系统
   - 在 `StarChartEnums.cs` 中新增 `ItemShape` 枚举（`Shape1x1 / Shape1x2H / Shape2x1V / ShapeL / Shape2x2`）
   - 在 `StarChartItemSO.cs` 中新增 `[SerializeField] ItemShape _shape` 字段及 `Shape` 属性
   - 新增静态工具类 `ItemShapeHelper`，提供 `GetCells(ItemShape)` 返回相对坐标偏移数组，`GetBounds(ItemShape)` 返回 `(cols, rows)` bounding box
   - _需求：1.2_

- [ ] 2. 重构 `WeaponTrack` / `SlotLayer<T>` 数据层：支持 2D 矩阵网格
   - 将 `SlotLayer<T>` 的内部存储从一维 `_items[]` 改为 `_grid[row, col]` 二维数组（2行×N列）
   - 更新 `FreeSpace`、`TryPlace(item, col, row)`、`Remove(col, row)`、`GetAt(col, row)` 等 API，基于形状坐标计算占用格子
   - 新增 `CanPlace(item, anchorCol, anchorRow)` 方法，返回 `(bool canPlace, List<(col,row)> evictList)`
   - _需求：1.1、1.5_

- [ ] 3. 重构 `TrackView` + `SlotCellView`：渲染覆盖层（Overlay）跨格部件
   - `SlotCellView` 新增 `SetOverlayAnchor(StarChartItemSO, ItemShape)` 方法：根据 bounding box 动态调整 RectTransform 尺寸，跨越多格渲染图标
   - `SlotCellView` 新增 `SetOverlaySpanned()` 方法：被覆盖的次格显示着色背景，不显示图标
   - `TrackView.Refresh()` 改为按 2D 矩阵遍历，主格调用 `SetOverlayAnchor`，次格调用 `SetOverlaySpanned`
   - _需求：1.3_

- [ ] 4. 升级 `DragDropManager` + `DragGhostView`：Shape 感知的拖拽预览与三态高亮
   - `DragDropManager` 的 `OnPointerEnter` 逻辑改为调用 `CanPlace()` 获取 evictList，据此判断 Valid / Replace / Invalid 三态
   - `TrackView.SetShapeHighlight(anchorCol, anchorRow, ItemShape, DropPreviewState)` 高亮所有被覆盖格子（绿/橙/红）
   - `DragGhostView.SetShape(ItemShape)` 根据 bounding box 调整 Ghost 宽高，并在 Ghost 内部渲染半透明形状格子网格
   - `DragGhostView.SetDropState()` 中替换提示文字改为 `↺ 替换 N 个部件`（N 来自 evictList.Count），Ghost 边框颜色切换使用 PrimeTween ≤80ms 过渡
   - _需求：1.4、1.6、7.1、7.2、7.3、7.4、7.5_

- [ ] 5. 升级 `InventoryItemView`：库存缩略图叠加形状预览
   - `InventoryItemView` 新增 `_shapePreviewContainer`（子 GameObject），内含按 bounding box 动态生成的半透明格子网格
   - `InventoryItemView.Bind(StarChartItemSO)` 时调用 `ItemShapeHelper.GetBounds()` 生成形状预览格子
   - _需求：1.7_

- [ ] 6. 新增 Loadout 管理 UI：RENAME / DELETE / SAVE CONFIG 按钮
   - `UICanvasBuilder.BuildLoadoutSection()` 新增三个按钮节点（RENAME / DELETE / SAVE CONFIG）
   - `LoadoutSwitcher` 新增 `OnRenameClicked`、`OnDeleteClicked`、`OnSaveClicked` 事件处理：RENAME 弹出 `TMP_InputField` 内联编辑；DELETE 调用 `StarChartController.RemoveLoadout()` 并切换到相邻 Loadout；SAVE CONFIG 调用 `SaveManager` 持久化并通过 `StatusBarView.ShowMessage("SAVED")` 显示 0.8s 反馈
   - DELETE 按钮在 Loadout 数量 ≤ 1 时设为 `interactable = false`（灰色）
   - _需求：2.1、2.2、2.3、2.4、2.5_

- [ ] 7. 实现 Drum Counter 翻牌动画
   - `LoadoutSwitcher` 中将 `_drumCounterLabel`（单个 TMP_Text）改为双层结构：`_drumFront`（当前数字）+ `_drumBack`（新数字），两者叠放在同一容器内
   - 新增 `PlayDrumFlip(int newValue, bool goingUp)` 方法：用 PrimeTween `Tween.LocalEulerAngles` 驱动 `_drumFront` 绕 X 轴翻出（0→-90deg），同时 `_drumBack` 从对应方向翻入（90→0deg 或 -90→0deg），总时长 ≤ 300ms
   - 在 `SwitchLoadout()` 调用时传入方向参数触发翻牌
   - _需求：3.1、3.2、3.3、3.4、3.5_

- [ ] 8. 为 Loadout Card 切换动画添加 Scale 纵深感
   - 在 `LoadoutSwitcher.PlaySlideAnimation()` 中，退出动画并行添加 `Tween.Scale(card, Vector3.one, new Vector3(0.88f, 0.88f, 1f), duration)`
   - 进入动画并行添加 `Tween.Scale(card, new Vector3(0.88f, 0.88f, 1f), Vector3.one, duration)`
   - 动画完成回调中强制 `card.localScale = Vector3.one` 防止累积误差
   - _需求：4.1、4.2、4.3、4.4_

- [ ] 9. 修正库存过滤器按钮顺序，统一枚举数据源
   - 在 `StarChartEnums.cs`（或新建 `StarChartConstants.cs`）中定义 `FilterOrder` 静态数组：`[All, LightSail, Prism, Core, Satellite]`
   - `UICanvasBuilder.BuildInventoryFilters()` 和 `InventoryView.BuildFilterButtons()` 均改为遍历 `FilterOrder` 生成按钮，不再硬编码顺序
   - _需求：8.1、8.2_

- [ ] 10. 补全状态栏提示文字，新增拖拽槽位时的动态切换
   - `StatusBarView` 的默认待机文字常量改为：`"EQUIPPED {0}/10 · INVENTORY {1} ITEMS · DRAG TO EQUIP · CLICK TO INSPECT · DRAG SLOT TO INVENTORY TO UNEQUIP"`
   - `DragDropManager` 在 `OnBeginDrag`（来源为已装备槽位）时调用 `StatusBarView.ShowPersistent("DRAG TO INVENTORY TO UNEQUIP")`；`OnEndDrag` 时调用 `StatusBarView.RestoreDefault()`
   - _需求：9.1、9.2、9.3_
