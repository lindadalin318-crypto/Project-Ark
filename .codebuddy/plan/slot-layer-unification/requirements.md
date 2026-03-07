# 需求文档：槽位层统一化（SAT / Core / Prism / SAIL）

## 引言

当前星图面板中，Core 和 Prism 已经通过 `SlotLayer<T>.TryUnlockColumn()` 支持动态列扩展，并在 `TrackView` 的 Inspector 中提供 `_debugCoreCols` / `_debugPrismCols` 调控字段（Range 0–4，0 = 使用存档数据）。

然而 SAT 和 SAIL 目前存在以下不一致：

- **SAT**：`WeaponTrack` 构造函数中硬编码 `initialCols: 2`（固定 2×2 = 4 格），无法通过 Inspector 调控，也没有对应的 `_debugSatCols` 字段。
- **SAIL**：使用独立的 `_sailSlotCount`（Range 1–4）字段控制 UI 格子数，但底层没有 `SlotLayer<LightSailSO>`，仍是单引用逻辑（`GetEquippedLightSail()` 返回单个对象），与 Core/Prism/SAT 的 SlotLayer 架构不一致。

本次需求目标：
1. 让 SAT 与 Core/Prism 完全一致——支持 `SetLayerCols` 扩展，并在 `TrackView` Inspector 中提供 `_debugSatCols` 调控字段。
2. 让 SAIL 也采用 `SlotLayer<LightSailSO>` 架构，支持多格扩展，并在 `StarChartPanel` Inspector 中提供 `_debugSailCols` 调控字段（与 SAT/Core/Prism 保持一致的调控方式）。

---

## 需求

### 需求 1：SAT 支持动态列扩展（与 Core/Prism 保持一致）

**用户故事：** 作为开发者，我希望 SAT 槽位层能像 Core/Prism 一样通过 `SetLayerCols` 动态扩展列数，并在 `TrackView` Inspector 中提供调控字段，以便在不修改代码的情况下快速调整 SAT 格子数量。

#### 验收标准

1. WHEN `WeaponTrack` 构造时，THEN SAT 层 SHALL 默认以 `initialCols: 1` 初始化（与 Core/Prism 默认行为一致），而非硬编码 `initialCols: 2`。
2. WHEN 调用 `WeaponTrack.SetLayerCols(coreCols, prismCols, satCols)` 时，THEN 系统 SHALL 同时扩展 SAT 层的列数到 `satCols`（范围 1–4）。
3. WHEN `TrackView` 的 `_debugSatCols` 字段在 Inspector 中被设置为 1–4 时，THEN 系统 SHALL 在 `ApplyDebugSlotCounts()` 中将该值应用到 `_track.SatLayer`，覆盖存档数据。
4. WHEN `_debugSatCols` 为 0 时，THEN 系统 SHALL 使用存档数据中的 SAT 列数（不覆盖）。
5. WHEN SAT 层列数变化时，THEN `RefreshColumn` SHALL 正确显示/隐藏对应格子（与 Core/Prism 的 `RefreshColumn` 逻辑完全一致）。

---

### 需求 2：SAIL 采用 SlotLayer 架构（与 SAT/Core/Prism 保持一致）

**用户故事：** 作为开发者，我希望 SAIL 也使用 `SlotLayer<LightSailSO>` 管理槽位，支持多格扩展，并在 `StarChartPanel` Inspector 中提供 `_debugSailCols` 调控字段，以便 SAIL 的架构与其他部件类型保持一致。

#### 验收标准

1. WHEN `StarChartController` 初始化时，THEN 系统 SHALL 创建一个 `SlotLayer<LightSailSO>`（`initialCols: 1`）来管理 SAIL 槽位，替代原有的单引用字段。
2. WHEN 调用 `EquipLightSail(sail, anchorCol, anchorRow)` 时，THEN 系统 SHALL 将 SAIL 放入 SlotLayer 的指定位置，而非覆盖单引用。
3. WHEN 调用 `UnequipLightSail(sail)` 时，THEN 系统 SHALL 从 SlotLayer 中移除该 SAIL。
4. WHEN `StarChartPanel` 的 `_debugSailCols` 字段在 Inspector 中被设置为 1–4 时，THEN 系统 SHALL 调用 `SetSailLayerCols(n)` 扩展 SAIL 层列数，并刷新 UI。
5. WHEN `_debugSailCols` 为 0 时，THEN 系统 SHALL 使用存档数据中的 SAIL 列数（不覆盖）。
6. WHEN SAIL 层列数变化时，THEN `RefreshSharedSailColumn` SHALL 正确显示/隐藏对应格子（与 `RefreshColumn` 逻辑一致）。
7. WHEN 拖拽 SAIL 到任意已解锁格子时，THEN 系统 SHALL 将 SAIL 放置到对应的 SlotLayer 位置（而非始终放到 cells[0]）。
8. IF SAIL 的 SlotLayer 已满，THEN 系统 SHALL 拒绝新的 SAIL 放入，并显示"NO SPACE"提示。

---

### 需求 3：Inspector 调控字段统一规范

**用户故事：** 作为开发者，我希望所有部件类型（Core/Prism/SAT/SAIL）的 Inspector 调控字段遵循统一的命名和行为规范，以便在 Inspector 中快速定位和调整。

#### 验收标准

1. WHEN 查看 `TrackView` Inspector 时，THEN 系统 SHALL 在 `[Header("Debug: Slot Counts (0 = use save data)")]` 下显示三个字段：`_debugCoreCols`、`_debugPrismCols`、`_debugSatCols`，均为 `[Range(0, 4)]`。
2. WHEN 查看 `StarChartPanel` Inspector 时，THEN 系统 SHALL 在 `[Header("Shared SAIL Column")]` 下显示 `_debugSailCols` 字段（`[Range(0, 4)]`），替代原有的 `_sailSlotCount`（`[Range(1, 4)]`）。
3. WHEN 所有调控字段值为 0 时，THEN 系统 SHALL 统一表示"使用存档数据"，行为与 Core/Prism 的 `_debugCoreCols = 0` 一致。

---

### 需求 4：存档兼容性

**用户故事：** 作为开发者，我希望 SAT 和 SAIL 的槽位层扩展与现有存档系统兼容，不破坏已有的存档数据。

#### 验收标准

1. WHEN 读取旧存档（不含 SAT/SAIL 列数信息）时，THEN 系统 SHALL 默认使用 `initialCols: 1`，不崩溃。
2. WHEN `SetLayerCols` 被调用时，THEN 系统 SHALL 只扩展列数（不缩减），与 Core/Prism 的现有行为一致。
3. IF SAT 层当前有装备物品，THEN 扩展列数 SHALL 不影响已装备物品的位置。
