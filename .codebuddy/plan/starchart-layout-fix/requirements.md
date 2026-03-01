# 需求文档：StarChart UI 布局修复

## 引言

当前 Unity 中的 StarChartPanel 与 HTML 原型（`StarChartUIPrototype.html`）存在三处明显偏差：
1. **面板尺寸/位置**：面板没有铺满全屏（实际上原型也是 980×700 居中，但 Canvas 缩放设置导致在不同分辨率下显示异常）
2. **布局混乱**：LoadoutCard 内的 CardHeader 占比过大，TracksArea 被压缩；Primary/Secondary 轨道的列标题（SAIL/PRISM/CORE/SAT）位置错乱
3. **背包格子尺寸不统一**：InventoryView 的 GridLayoutGroup cellSize 为 (100, 120)，与原型的 44×44px inv-slot 差距悬殊

目标：通过修改 `UICanvasBuilder.cs` 中的布局参数，使 Unity 中的 StarChartPanel 与 HTML 原型完全一致。

---

## 需求

### 需求 1：面板整体尺寸与 Canvas 缩放

**用户故事：** 作为开发者，我希望 StarChartPanel 在 1920×1080 参考分辨率下以 980×700 居中显示，并在其他分辨率下等比缩放，以便与 HTML 原型视觉一致。

#### 验收标准

1. WHEN Canvas CanvasScaler 的 referenceResolution 为 1920×1080、matchWidthOrHeight 为 0.5 THEN StarChartPanel 在 1920×1080 下显示为 980×700 居中
2. WHEN 游戏窗口分辨率不是 1920×1080 THEN StarChartPanel SHALL 等比缩放，不出现拉伸或裁剪
3. IF StarChartPanel 的 anchorMin/anchorMax 均为 (0.5, 0.5)、pivot 为 (0.5, 0.5)、anchoredPosition 为 (0, 0) THEN 面板 SHALL 始终居中显示

---

### 需求 2：LoadoutCard 内部比例修正

**用户故事：** 作为开发者，我希望 LoadoutCard 内的 CardHeader 高度与 TracksArea 高度比例与 HTML 原型一致，以便轨道区域有足够空间显示 2×2 格子。

#### 验收标准

1. WHEN LoadoutCard 构建完成 THEN CardHeader SHALL 占 LoadoutCard 高度的约 12%（anchorMin.y = 0.88，anchorMax.y = 1.0）
2. WHEN LoadoutCard 构建完成 THEN TracksArea SHALL 占 LoadoutCard 高度的约 88%（anchorMin.y = 0，anchorMax.y = 0.87）
3. WHEN TracksArea 构建完成 THEN PrimaryTrackView SHALL 占上半部分（y: 0.52 → 1.0），SecondaryTrackView SHALL 占下半部分（y: 0.0 → 0.49），中间有 1px 分割线
4. WHEN TrackView 构建完成 THEN 每个 TypeColumn 的 GridContainer SHALL 使用 cellSize (40, 40)，spacing (2, 2)，与 HTML 原型 `--slot-size: 40px` 一致

---

### 需求 3：InventoryView 背包格子尺寸统一

**用户故事：** 作为开发者，我希望 InventoryView 的背包格子尺寸与 HTML 原型的 `--inv-slot-size: 44px` 一致，以便背包区域整齐美观。

#### 验收标准

1. WHEN InventoryView 的 GridLayoutGroup 构建完成 THEN cellSize SHALL 为 (44, 44)，与 HTML 原型 `--inv-slot-size: 44px` 一致
2. WHEN InventoryView 的 GridLayoutGroup 构建完成 THEN spacing SHALL 为 (2, 2)，与 HTML 原型 `--slot-gap: 2px` 一致
3. WHEN InventoryView 的 GridLayoutGroup 构建完成 THEN constraintCount SHALL 根据实际宽度自动适配（不固定为 4 列），或调整为合理列数使格子不溢出
4. IF InventoryView 宽度约为 980 × 0.59 ≈ 578px THEN constraintCount 为 8 列（578 / (44+2) ≈ 12，取合理值）时格子 SHALL 整齐排列

---

### 需求 4：LoadoutSection 与 InventorySection 高度比例

**用户故事：** 作为开发者，我希望 LoadoutSection（上半）与 InventorySection（下半）的高度比例与 HTML 原型的 `flex: 1` 各占 50% 一致。

#### 验收标准

1. WHEN StarChartPanel 构建完成 THEN LoadoutSection SHALL 占面板高度约 55%（y: 0.377 → 0.935），与原型 `flex: 1` 上半一致
2. WHEN StarChartPanel 构建完成 THEN InventorySection SHALL 占面板高度约 33%（y: 0.04 → 0.375），与原型 `flex: 1` 下半一致
3. WHEN StarChartPanel 构建完成 THEN Header SHALL 占顶部 6%（y: 0.94 → 1.0），StatusBar SHALL 占底部 4%（y: 0.0 → 0.038）
