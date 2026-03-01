# 实施计划：Slot 尺寸放大

- [ ] 1. 修改 UICanvasBuilder.cs — Track Slot 尺寸
   - 找到 TypeColumn 内 GridContainer 的 `cellSize` 设置，将 `(40, 40)` 改为 `(80, 80)`
   - 确认 `constraintCount = 2` 保持不变
   - 确认 PrimaryTrackView / SecondaryTrackView 的锚点比例代码未被触碰
   - _需求：1.1、1.2、1.3_

- [ ] 2. 修改 UICanvasBuilder.cs — Inventory Slot 尺寸与列数
   - 找到 InventoryView GridLayoutGroup 的 `cellSize` 设置，将 `(44, 44)` 改为 `(80, 80)`
   - 将 `constraintCount` 从 `12` 改为 `8`
   - 确认 `spacing (2, 2)` 和 `padding (6, 6, 6, 6)` 保持不变
   - _需求：2.1、2.2、2.3_

- [ ] 3. 修改 UICanvasBuilder.cs — InventoryItemView Prefab 根节点尺寸
   - 找到 InventoryItemView Prefab 根节点的 `sizeDelta` 设置，将 `(44, 44)` 改为 `(80, 80)`
   - 确认内部子元素（图标、名称标签、类型点）均使用相对锚点，无需额外调整
   - _需求：3.1、3.2_
