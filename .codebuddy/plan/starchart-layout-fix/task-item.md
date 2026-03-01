# 实施计划：StarChart UI 布局修复

## 目标
修改 `UICanvasBuilder.cs` 中的布局参数，使 Unity 中的 StarChartPanel 与 HTML 原型完全一致。

---

- [ ] 1. 修正 StarChartPanel 整体区域高度比例（Header / LoadoutSection / InventorySection / StatusBar）
   - HTML 原型：Header 固定 42px、LoadoutSection flex:1、SectionDivider 1px、InventorySection flex:1、StatusBar 固定 22px
   - 面板总高 700px，计算比例：Header ≈ 6%（42/700）、StatusBar ≈ 3.1%（22/700）、两个 flex:1 区域各占剩余 50%
   - 修改 `BuildStarChartSection` 中各区域的 `SetAnchors` 调用：
     - Header: `(0, 0.94) → (1, 1.0)`（42px / 700px ≈ 6%）
     - LoadoutSection: `(0, 0.47) → (1, 0.935)`（约 47% 高度）
     - SectionDivider: `(0.01, 0.465) → (0.99, 0.468)`
     - InventorySection: `(0, 0.031) → (1, 0.463)`（约 43% 高度）
     - StatusBar: `(0, 0) → (1, 0.031)`（22px / 700px ≈ 3.1%）
   - _需求：4.1、4.2、4.3_

- [ ] 2. 修正 LoadoutCard 内部 CardHeader 与 TracksArea 比例
   - HTML 原型：lc-header 固定高度（约 28px，含 padding），PRIMARY track 和 SECONDARY track 各 flex:1
   - LoadoutCard 高度 ≈ 700 × 0.465 ≈ 325px，lc-header 约占 8.6%（28/325）
   - 修改 `BuildStarChartSection` 中 CardHeader 和 TracksArea 的锚点：
     - CardHeader: `(0, 0.91) → (1, 1.0)`（约 9%）
     - TracksArea: `(0, 0) → (1, 0.90)`（约 90%）
     - PrimaryTrackView: `(0, 0.52) → (1, 1.0)`（上半 48%）
     - SecondaryTrackView: `(0, 0) → (1, 0.49)`（下半 49%）
   - _需求：2.1、2.2、2.3_

- [ ] 3. 修正 InventorySection 布局：移除 ItemDetailView 分割，改为全宽 InventoryView
   - HTML 原型：InventorySection 只有 inv-bar（filter 按钮）+ inventory-area（全宽 grid），没有右侧 ItemDetailView
   - 将 InventoryView 改为全宽：`SetAnchors(inventoryGo, new Vector2(0.01f, 0f), new Vector2(0.99f, 1f))`
   - 将 ItemDetailView 改为隐藏（`SetActive(false)`）或保留在面板外（不影响布局）
   - FilterBar 高度比例调整为约 20%（inv-bar 34px / InventorySection 高度）
   - _需求：3.1、3.2_

- [ ] 4. 修正 InventoryView GridLayoutGroup 背包格子尺寸
   - HTML 原型：`--inv-slot-size: 44px`，`--slot-gap: 2px`，16 列 CSS grid
   - 修改 `BuildStarChartSection` 中 GridLayoutGroup 参数：
     - `gridLayout.cellSize = new Vector2(44, 44)`
     - `gridLayout.spacing = new Vector2(2, 2)`
     - `gridLayout.constraintCount = 12`（全宽 980 × 0.98 ≈ 960px，960 / (44+2) ≈ 20，取合理值 12 避免溢出）
   - 同步修改 `CreateInventoryItemViewPrefab` 中 `rootRect.sizeDelta = new Vector2(44, 44)`
   - _需求：3.1、3.2、3.3_

- [ ] 5. 修正 TrackView 中 TrackLabel 宽度（固定 64px → 锚点比例）
   - HTML 原型：`.track-block-label { width: 64px; flex-shrink: 0 }`，track-rows 占剩余宽度
   - LoadoutCard 宽度 ≈ 980 × 0.93 = 911px，64px / 911px ≈ 7%
   - 当前已是 8%，差异不大，但需确认 ColumnHeader 在 TrackView 内的位置正确
   - 修改 `BuildTrackView` 中 TrackLabel 锚点为 `(0, 0) → (0.07, 1.0)`
   - 修改 4 列 TypeColumn 起始 x 从 0.09 调整为 0.08，确保紧贴 TrackLabel
   - _需求：2.3_

- [ ] 6. 执行 Build UI Canvas 并在 ImplementationLog.md 中追加记录
   - 在 Unity Editor 中执行菜单 `ProjectArk > Build UI Canvas` 重建 UI
   - 在 `Docs/ImplementationLog/ImplementationLog.md` 中追加本次修改记录
   - _需求：1.1、1.2、1.3、2.1、2.2、2.3、2.4、3.1、3.2、3.3、4.1、4.2、4.3_
