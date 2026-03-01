# 需求文档：Slot 尺寸放大

## 引言

当前星图面板中，Tracks（装备槽）和背包（Inventory）的 slot 尺寸偏小，视觉上难以辨认图标内容。本需求将两者的 slot 统一放大一倍，并保持两者尺寸一致，同时维持原有的整体布局结构不变。

---

## 需求

### 需求 1：Track Slot 放大一倍

**用户故事：** 作为玩家，我希望 Tracks 区域的装备槽更大，以便更清晰地看到已装备的星图部件图标。

#### 验收标准

1. WHEN UICanvasBuilder 重建 UI THEN 系统 SHALL 将 TypeColumn 内 GridContainer 的 `cellSize` 从 `(40, 40)` 改为 `(80, 80)`。
2. WHEN Track slot 放大后 THEN 系统 SHALL 保持 2×2 的网格布局（constraintCount = 2）不变。
3. WHEN Track slot 放大后 THEN 系统 SHALL 保持 PrimaryTrackView / SecondaryTrackView 的锚点比例布局不变。

---

### 需求 2：Inventory Slot 放大并与 Track Slot 保持一致

**用户故事：** 作为玩家，我希望背包中的物品格子与装备槽大小一致，以便获得统一的视觉体验。

#### 验收标准

1. WHEN UICanvasBuilder 重建 UI THEN 系统 SHALL 将 InventoryView 的 GridLayoutGroup `cellSize` 从 `(44, 44)` 改为 `(80, 80)`。
2. WHEN Inventory slot 放大后 THEN 系统 SHALL 将列数（constraintCount）从 12 调整为 8，以适应新的 slot 尺寸，防止内容溢出。
3. WHEN Inventory slot 放大后 THEN 系统 SHALL 保持 `spacing (2, 2)` 和 `padding (6, 6, 6, 6)` 不变。

---

### 需求 3：InventoryItemView Prefab 尺寸同步更新

**用户故事：** 作为开发者，我希望 InventoryItemView Prefab 的根节点尺寸与 GridLayoutGroup 的 cellSize 保持一致，以便拖拽和渲染正确。

#### 验收标准

1. WHEN UICanvasBuilder 重建 InventoryItemView Prefab THEN 系统 SHALL 将根节点 `sizeDelta` 从 `(44, 44)` 改为 `(80, 80)`。
2. WHEN InventoryItemView 尺寸更新后 THEN 系统 SHALL 保持内部所有子元素的锚点比例不变（图标、名称标签、类型点等均使用相对锚点，无需手动调整）。

---

### 需求 4：布局整体结构保持不变

**用户故事：** 作为开发者，我希望放大 slot 后整体面板布局（LoadoutSection / InventorySection 的比例、锚点、分隔线位置）保持不变，以便不引入额外的布局回归。

#### 验收标准

1. WHEN slot 尺寸修改完成后 THEN 系统 SHALL 不修改任何 Section 级别的锚点、比例或分隔线位置。
2. WHEN slot 尺寸修改完成后 THEN 系统 SHALL 不修改 TrackView、TypeColumn 的锚点比例。
3. IF InventoryView 的 ScrollRect 存在 THEN 系统 SHALL 保持竖向滚动功能正常（ContentSizeFitter 自动适应新高度）。
