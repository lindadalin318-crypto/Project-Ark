# 需求文档：拖拽放置高亮系统（Drag Placement Highlight System）

## 引言

当前星图面板的拖拽系统（`DragDropManager` + `SlotCellView`）缺乏动态放置预览反馈：
- 拖拽悬停时，目标格子没有颜色区分（合法/非法/替换）
- `ItemOverlayView` 在拖拽开始时不会立即隐藏，导致 Ghost + overlay 双重渲染
- 没有 Shape-aware 的高亮——多格部件悬停时，所有占用格子应同时高亮

借鉴 **Cholopol Unity Tetris Grid Inventory System** 的核心思路：
> 拖拽悬停时，动态在目标格子上生成绿/红 highlight tiles，而不是依赖静态 overlay。

结合 **Backpack Monsters** 的 Icon/Shape 解耦哲学（已实现），本功能完整实现拖拽放置的视觉反馈层。

---

## 需求

### 需求 1：Shape-Aware 放置高亮

**用户故事：** 作为玩家，我希望拖拽部件悬停到 Track 格子时，所有将被占用的格子同时高亮显示，以便我能直观判断部件的放置位置和形状。

#### 验收标准

1. WHEN 玩家拖拽部件悬停到 Track 的 `SlotCellView` 上 THEN 系统 SHALL 在所有将被该部件占用的格子上显示高亮 overlay（基于 `ItemShapeHelper.GetCells()` 计算）
2. WHEN 放置合法（`DropTargetValid == true` 且无替换）THEN 系统 SHALL 将高亮颜色设为绿色（`StarChartTheme.HighlightValid`，alpha ≈ 0.55）
3. WHEN 放置会触发强制替换（`DropTargetIsReplace == true`）THEN 系统 SHALL 将高亮颜色设为橙色/黄色（`StarChartTheme.HighlightReplace`，alpha ≈ 0.55）
4. WHEN 放置非法（`DropTargetValid == false`）THEN 系统 SHALL 将高亮颜色设为红色（`StarChartTheme.HighlightInvalid`，alpha ≈ 0.45）
5. WHEN 鼠标离开所有 Track 格子 THEN 系统 SHALL 立即清除所有高亮 overlay
6. WHEN 拖拽结束（成功或取消）THEN 系统 SHALL 立即清除所有高亮 overlay

---

### 需求 2：高亮 Tile 独立于 ItemOverlayView

**用户故事：** 作为玩家，我希望拖拽时的高亮预览与已装备部件的 overlay 完全独立，以便两者不会互相干扰或产生视觉重叠。

#### 验收标准

1. WHEN 拖拽开始（`BeginDrag` 调用）THEN 系统 SHALL 将被拖拽部件的 `ItemOverlayView` 的 `CanvasGroup.alpha` 设为 0（不销毁，保留 drop target 功能）
2. WHEN 拖拽结束（`CleanUp` 调用）THEN 系统 SHALL 恢复所有 `ItemOverlayView` 的 `CanvasGroup.alpha` 为 1（通过 `RefreshAllViews` 重建）
3. WHEN 高亮 tiles 显示时 THEN 系统 SHALL 将高亮 tiles 渲染在 `ItemOverlayView` 之上（更高的 sibling index 或更高的 Canvas sortingOrder）
4. IF 高亮 tiles 与现有 `ItemOverlayView` 的格子重叠 THEN 系统 SHALL 仍然正确显示高亮颜色（不被 overlay 遮挡）

---

### 需求 3：高亮 Tile 的生命周期管理

**用户故事：** 作为开发者，我希望高亮 tiles 有明确的创建/销毁生命周期，以便不产生内存泄漏或残留的 GameObject。

#### 验收标准

1. WHEN 高亮需要更新（悬停新格子或状态变化）THEN 系统 SHALL 复用已有的高亮 tile GameObject（仅更新颜色），而非每帧销毁重建
2. WHEN 拖拽结束或取消 THEN 系统 SHALL 销毁（或隐藏）所有高亮 tile GameObject
3. WHEN 同一帧内连续触发 `OnPointerEnter` 和 `OnPointerExit` THEN 系统 SHALL 不产生闪烁（高亮更新应在同一帧内完成）
4. IF 拖拽中途切换悬停目标 THEN 系统 SHALL 先清除旧高亮，再显示新高亮，不留残影

---

### 需求 4：高亮与 Ghost 的视觉协调

**用户故事：** 作为玩家，我希望拖拽 Ghost 和放置高亮在视觉上协调一致，以便我能清晰区分"我拿着什么"和"我要放在哪里"。

#### 验收标准

1. WHEN 高亮显示时 THEN Ghost 的 `DragGhostView` SHALL 同步更新边框颜色（绿/橙/红，已有 `SetDropState` 接口）
2. WHEN 高亮颜色为绿色 THEN Ghost 边框 SHALL 同步为绿色
3. WHEN 高亮颜色为红色 THEN Ghost 边框 SHALL 同步为红色
4. WHEN 高亮颜色为橙色（替换）THEN Ghost 边框 SHALL 同步为橙色，并显示替换数量提示

---

### 需求 5：新增 `StarChartTheme` 高亮颜色常量

**用户故事：** 作为开发者，我希望高亮颜色统一定义在 `StarChartTheme` 中，以便所有高亮相关代码使用同一套颜色规范。

#### 验收标准

1. IF `StarChartTheme.HighlightValid` 未定义 THEN 系统 SHALL 新增该常量（建议值：`#4CAF50` alpha 0.55，绿色）
2. IF `StarChartTheme.HighlightInvalid` 未定义 THEN 系统 SHALL 新增该常量（建议值：`#F44336` alpha 0.45，红色）
3. IF `StarChartTheme.HighlightReplace` 未定义 THEN 系统 SHALL 新增该常量（建议值：`#FF9800` alpha 0.55，橙色）
4. WHEN 高亮 tiles 使用颜色时 THEN 系统 SHALL 只引用 `StarChartTheme` 中的常量，禁止 hardcode 颜色值

---

### 需求 6：Sail 和 Satellite 格子的高亮支持

**用户故事：** 作为玩家，我希望拖拽 Sail 或 Satellite 部件时，对应的格子也有高亮反馈，以便我知道部件会放在哪里。

#### 验收标准

1. WHEN 拖拽 `LightSailSO` 悬停到 Sail 列的格子 THEN 系统 SHALL 高亮该格子（绿色或橙色，取决于是否已有 Sail 装备）
2. WHEN 拖拽 `SatelliteSO` 悬停到 Sat 列的格子 THEN 系统 SHALL 高亮该格子（绿色或橙色，取决于 Sat 槽是否已满）
3. WHEN 拖拽 Core/Prism 部件悬停到 Sail/Sat 列 THEN 系统 SHALL 显示红色高亮（类型不匹配）
