# 需求文档：SlotLayer 行列双向解锁重构

## 引言

当前 `SlotLayer<T>` 的数据结构存在语义错位问题：

- **现状**：`FIXED_ROWS = 2`（行固定为2），`Cols` 可解锁（1~4），初始 `Cols=1` → 2格纵向排列
- **视觉**：UI 的 `GridLayoutGroup` 使用 `FixedColumnCount=2`，实际渲染为**横向2格**
- **问题**：数据坐标系（行=纵向）与视觉坐标系（列=横向）存在语义错位，导致 Machine Gun（Shape1x2H，需要横向2格）无法正确放入初始状态的 Core 区域

**目标**：重构 `SlotLayer<T>` 使其数据结构与视觉表现完全一致：
- 初始状态 = 横向2格（`Cols=2, Rows=1`）
- 行（Rows）和列（Cols）都可独立解锁
- 数据坐标系与视觉坐标系完全对齐（col=横向，row=纵向）
- 最大 `MaxCols=4, MaxRows=4`（最大16格）

---

## 需求

### 需求 1：SlotLayer 数据结构重构

**用户故事：** 作为开发者，我希望 `SlotLayer<T>` 的行列语义与视觉完全一致，以便数据坐标 `(col, row)` 直接对应视觉上的（横向列，纵向行）。

#### 验收标准

1. WHEN `SlotLayer` 以默认参数构造 THEN 系统 SHALL 初始化为 `Cols=2, Rows=1`（横向2格）
2. WHEN 调用 `TryUnlockCol()` THEN 系统 SHALL 将 `Cols` 增加1（最大 `MaxCols=4`），向右扩展
3. WHEN 调用 `TryUnlockRow()` THEN 系统 SHALL 将 `Rows` 增加1（最大 `MaxRows=4`），向下扩展
4. IF `Cols >= MaxCols` THEN `TryUnlockCol()` SHALL 返回 `false` 且不修改状态
5. IF `Rows >= MaxRows` THEN `TryUnlockRow()` SHALL 返回 `false` 且不修改状态
6. WHEN 解锁行或列 THEN 系统 SHALL 保留已装备物品的位置不变，新增格子为空
7. WHEN 调用 `InBounds(col, row)` THEN 系统 SHALL 检查 `col < Cols && row < Rows`（矩形边界）
8. WHEN 调用 `Capacity` THEN 系统 SHALL 返回 `Cols * Rows`

### 需求 2：删除 FIXED_ROWS 常量

**用户故事：** 作为开发者，我希望移除 `FIXED_ROWS = 2` 这个硬编码常量，以便行数可以动态解锁。

#### 验收标准

1. WHEN 重构完成 THEN 系统 SHALL 不再存在 `FIXED_ROWS` 常量
2. WHEN 重构完成 THEN 系统 SHALL 新增 `MAX_COLS = 4` 和 `MAX_ROWS = 4` 常量
3. WHEN 重构完成 THEN 系统 SHALL 保留 `MAX_COLS` 常量（值不变）
4. IF 旧代码引用 `FIXED_ROWS` THEN 编译器 SHALL 报错（强制迁移）

### 需求 3：构造函数参数更新

**用户故事：** 作为开发者，我希望 `SlotLayer` 构造函数接受 `initialCols` 和 `initialRows` 两个参数，以便灵活初始化不同区域的格子布局。

#### 验收标准

1. WHEN 以 `new SlotLayer<T>()` 构造 THEN 系统 SHALL 默认 `initialCols=2, initialRows=1`
2. WHEN 以 `new SlotLayer<T>(initialCols: 2, initialRows: 1)` 构造 THEN 系统 SHALL 初始化为横向2格
3. WHEN 以 `new SlotLayer<T>(initialCols: 1, initialRows: 4)` 构造 THEN 系统 SHALL 初始化为纵向4格（SAIL 区域）
4. WHEN 构造参数超出最大值 THEN 系统 SHALL 自动 Clamp 到合法范围

### 需求 4：存档数据结构更新

**用户故事：** 作为开发者，我希望存档中同时保存每个 Layer 的 `Cols` 和 `Rows`，以便正确恢复解锁状态。

#### 验收标准

1. WHEN 保存游戏 THEN 系统 SHALL 在 `TrackSaveData` 中存储 `CoreLayerCols`, `CoreLayerRows`, `PrismLayerCols`, `PrismLayerRows`, `SatLayerCols`, `SatLayerRows`
2. WHEN 加载旧存档（无 `*Rows` 字段）THEN 系统 SHALL 将缺失的 `Rows` 字段默认为 `1`（向后兼容）
3. WHEN 加载旧存档（`CoreLayerCols=1`）THEN 系统 SHALL 将其迁移为 `CoreLayerCols=2`（因为旧的1列=新的2列初始状态）
4. WHEN 保存 SAIL 层 THEN 系统 SHALL 在 `LoadoutSlotSaveData` 中存储 `SailLayerCols` 和 `SailLayerRows`

### 需求 5：WeaponTrack 初始化更新

**用户故事：** 作为开发者，我希望 `WeaponTrack` 使用新的构造参数初始化各 Layer，以便初始状态正确反映横向2格布局。

#### 验收标准

1. WHEN `WeaponTrack` 构造 THEN 系统 SHALL 以 `initialCols=2, initialRows=1` 初始化 `CoreLayer`、`PrismLayer`、`SatLayer`
2. WHEN 调用 `SetLayerCols(coreCols, prismCols, satCols)` THEN 系统 SHALL 仍然有效（向后兼容）
3. WHEN 新增 `SetLayerRows(coreRows, prismRows, satRows)` THEN 系统 SHALL 支持行数的批量设置

### 需求 6：UI 层适配

**用户故事：** 作为开发者，我希望 `TrackView` 的 `RefreshColumn` 方法正确使用 `layer.Cols` 和 `layer.Rows` 计算格子可见性，以便 UI 与数据完全同步。

#### 验收标准

1. WHEN `RefreshColumn` 执行 THEN 系统 SHALL 使用 `layer.Rows * layer.Cols` 计算 `unlockedCount`（逻辑不变）
2. WHEN `layer.Cols=2, layer.Rows=1` THEN 系统 SHALL 显示2个格子（横向左右）
3. WHEN `layer.Cols=2, layer.Rows=2` THEN 系统 SHALL 显示4个格子（2×2方形）
4. WHEN `cellIndex = row * layer.Cols + col` THEN 系统 SHALL 正确映射到 `GridLayoutGroup` 的渲染顺序（行优先，与 `FixedColumnCount=2` 一致）

### 需求 7：ItemShape 坐标系对齐

**用户故事：** 作为开发者，我希望 `ItemShape` 的坐标系与新的 `SlotLayer` 坐标系完全一致，以便 Machine Gun（Shape1x2H）能正确放入初始横向2格的 Core 区域。

#### 验收标准

1. WHEN `ItemShape.Shape1x2H` 放入 `Cols=2, Rows=1` 的 Layer THEN 系统 SHALL 允许放置（占用 `(0,0)` 和 `(1,0)`）
2. WHEN `ItemShape.Shape2x1V` 放入 `Cols=1, Rows=2` 的 Layer THEN 系统 SHALL 允许放置（占用 `(0,0)` 和 `(0,1)`）
3. WHEN `ItemShapeHelper.GetCells(Shape1x2H)` THEN 系统 SHALL 返回 `[(0,0), (1,0)]`（横向偏移）
4. WHEN `ItemShapeHelper.FitsInGrid(shape, anchorCol, anchorRow, cols, rows)` THEN 系统 SHALL 使用新的 `cols/rows` 参数正确检查边界

### 需求 8：单元测试更新

**用户故事：** 作为开发者，我希望更新 `SlotLayerTests.cs` 以反映新的初始状态（`Cols=2, Rows=1`），以便测试套件保持绿色。

#### 验收标准

1. WHEN 运行 `SlotLayerTests` THEN 系统 SHALL 所有测试通过
2. WHEN `InitialCapacity_Is2_With1Col` 测试 THEN 系统 SHALL 更新为 `InitialCapacity_Is2_With2Cols1Row`
3. WHEN 新增 `TryUnlockRow_IncreasesRows` 测试 THEN 系统 SHALL 验证行解锁功能
4. WHEN 新增 `TryUnlockCol_And_Row_MaxCapacity_Is16` 测试 THEN 系统 SHALL 验证最大容量为16格
