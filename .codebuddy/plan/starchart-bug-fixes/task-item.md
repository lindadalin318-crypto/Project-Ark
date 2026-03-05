# 实施计划：StarChart UI Bug 修复

- [ ] 1. 修复 `SlotLayer` 动态容量系统（核心数据层）
   - 删除 `GRID_COLS`、`GRID_ROWS`、`GRID_SIZE` 三个硬编码常量
   - 新增 `Rows = 2`（固定）、`Cols`（运行时动态）、`Capacity = Rows * Cols` 属性
   - 新增 `TryUnlockColumn()` 方法：`Cols < MAX_COLS(4)` 时 `Cols++` 并扩容 grid 数组，返回 `bool`
   - 修复 `FreeSpace` 计算：`FreeSpace = Capacity - UsedSpace`
   - 构造函数接收初始列数参数，默认值 `1`（初始 1×2 = 2 格）
   - _需求：3.1、3.2、3.6、3.7、3.8、3.9_

- [ ] 2. 更新存档数据结构（持久化层）
   - 在 `TrackSaveData.cs` 中新增 `CoreLayerCols`、`PrismLayerCols` 字段，默认值 `= 1`
   - 在 `WeaponTrack.cs` 的 `Save()` 方法中写入列数，`Load()` 方法中读取并传给 `SlotLayer` 构造函数
   - 确保旧存档（缺失字段）加载时使用默认值 `1`，不抛异常
   - _需求：3.3、3.4_

- [ ] 3. 更新 UI 层常量引用（`SlotCellView` / `TrackView`）
   - 将 `SlotCellView.cs` 和 `TrackView.cs` 中所有对 `GRID_COLS` / `GRID_ROWS` / `GRID_SIZE` 的引用替换为从 `layer.Cols` / `layer.Rows` / `layer.Capacity` 读取
   - 确保 Track UI 在容量变化后能正确刷新格子数量
   - _需求：3.9_

- [ ] 4. 修复 `InventoryItemView` 背包形状渲染（Bug 1）
   - 在 `Setup()` 中将 `bgImage.color` 设为 `Color.clear`（加 null check）
   - 在 `BuildShapePreview()` 中，对 empty cell 设置 `img.color = Color.clear` 且 `img.raycastTarget = false`
   - 保持 1×1 部件原有渲染逻辑不变（无 empty cell，无需特殊处理）
   - _需求：1.1、1.2、1.3、1.6_

- [ ] 5. 修复 `ItemOverlayView` Track Overlay 形状渲染（Bug 3）
   - 在 `Setup()` 中将 `bgImage.color` 设为 `Color.clear`（加 null check）
   - 遍历 bounding box 时，对 empty cell 设置 `color = Color.clear` 且 `raycastTarget = false`
   - 确保 Overlay 视觉格子数量与 shape cells 数量严格一致
   - _需求：2.1、2.2、2.4、2.5_

- [ ] 6. 验证放置原子性与交互正确性
   - 确认 `SlotLayer.TryPlace()` 实现两阶段逻辑：先检查所有 shape cells 空闲，全部通过才标记
   - 验证 L 形空洞格（`raycastTarget = false`）不阻挡 1×1 部件的拖拽放置命中检测
   - 验证 `EvictBlockingItems` 在 `FreeSpace` 足够时不被触发
   - _需求：1.4、1.5、2.3、3.5、3.6、3.7_

- [ ] 7. 更新并补充 `SlotLayerTests` 单元测试
   - 将现有测试中 `GRID_SIZE` 断言替换为 `layer.Capacity`
   - 新增测试：初始容量为 2（1 列 × 2 行）
   - 新增测试：`TryUnlockColumn()` 一次后 `Capacity` 变为 4
   - 新增测试：达到最大列数（4 列）后 `TryUnlockColumn()` 返回 `false`，`Capacity` 保持 8
   - 新增测试：解锁后新列格子可正常放置/移除部件，旧列数据不受影响
   - _需求：4.1、4.2、4.3、4.4_
