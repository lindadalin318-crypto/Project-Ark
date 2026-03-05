# 需求文档：StarChart UI Bug 修复

## 引言

本文档描述 StarChart（星图）系统中已确认的 4 个 Bug 的修复需求。这些 Bug 影响玩家在背包和 Track（武器轨道）中放置、拖拽、查看星图部件的核心体验。

**设计基准（方案 A）**：背包与 Track 遵循同一套占位规则——占位基于 shape cells（非 bounding box），L 形等非矩形部件的空洞格透明且可被其他 1×1 部件嵌入，玩家只需学一套规则。

**已完成**：Bug 2（DragGhostView 双重渲染）已于 2026-03-05 修复。

---

## 需求

### 需求 1：背包中非矩形部件正确显示形状（Bug 1）

**用户故事：** 作为玩家，我希望背包中的 L 形等非矩形部件只显示其实际占用的格子（着色），空洞格透明，以便我能直观看到部件的真实形状，并将其他小部件嵌入空洞格。

#### 验收标准

1. WHEN 玩家打开背包 AND 背包中存在 L 形部件 THEN 系统 SHALL 只对 L 形的 3 个 active shape cells 着色，空洞格显示为完全透明（`Color.clear`）。
2. WHEN L 形部件被渲染 THEN 系统 SHALL 将 `bgImage`（卡片背景）设为 `Color.clear`，不填充整个 bounding box 背景。
3. WHEN L 形部件的空洞格被渲染 THEN 系统 SHALL 将该 cell 的 `Image.raycastTarget` 设为 `false`，使其不阻挡拖拽交互。
4. WHEN 玩家将 1×1 部件拖拽到 L 形的空洞格位置 THEN 系统 SHALL 允许放置（occupancy 数据层该格为空）。
5. WHEN 1×1 部件成功放入 L 形空洞格后 THEN 系统 SHALL 正确标记该格为 occupied，背包 occupancy 数据与视觉一致。
6. IF 部件形状为 1×1（单格）THEN 系统 SHALL 保持原有渲染逻辑不变（无空洞格，无需特殊处理）。

---

### 需求 2：Track 上非矩形部件 Overlay 正确显示形状（Bug 3）

**用户故事：** 作为玩家，我希望将 L 形部件放置到 Track 后，Track 上的 Overlay 只覆盖部件实际占用的格子，空洞格保持透明可交互，以便我能继续将其他部件放入空洞格，与背包行为一致。

#### 验收标准

1. WHEN 玩家将 L 形部件放置到 Track THEN 系统 SHALL 只在 3 个 active shape cells 上渲染 Overlay 着色，空洞格 Overlay cell 设为 `Color.clear`。
2. WHEN Track Overlay 的 empty cell 被渲染 THEN 系统 SHALL 将该 cell 的 `Image.raycastTarget` 设为 `false`。
3. WHEN L 形部件已在 Track 上 THEN 系统 SHALL 允许玩家将 1×1 部件拖拽到 L 形的空洞格并成功放置。
4. WHEN Track Overlay 的背景 `bgImage` 被渲染 THEN 系统 SHALL 将其设为 `Color.clear`，不填充整个 bounding box。
5. WHEN 玩家查看 Track THEN 系统 SHALL 确保 Overlay 视觉格子数量与部件 shape cells 数量一致（L 形 = 3 格，不多出额外格子）。

---

### 需求 3：Track 容量动态解锁系统（Bug 4 — 核心修复）

**用户故事：** 作为玩家，我希望每条 Track 从初始 2 格开始，随游戏进度逐步解锁更多格子（最多 8 格），并且放置部件时位置准确不偏移，以便我能感受到成长感并正确配置武器。

#### 验收标准

1. WHEN 新游戏开始 THEN 系统 SHALL 将每条 Track 的每个 Layer（Core/Prism）初始化为 1 列 × 2 行 = 2 格。
2. WHEN 游戏进度系统调用解锁 API THEN 系统 SHALL 将对应 Layer 增加 1 列（+2 格），最大不超过 4 列 × 2 行 = 8 格。
3. WHEN 玩家保存游戏 THEN 系统 SHALL 将每条 Track 每个 Layer 的当前列数（`CoreLayerCols`、`PrismLayerCols`）写入存档。
4. WHEN 玩家加载存档 THEN 系统 SHALL 从存档读取列数并正确恢复 Track 容量，UI 显示与存档一致。
5. WHEN 玩家将 Twin Splits（2 格部件）放置到 Track 左下角 THEN 系统 SHALL 将其放置在正确的 2 个格子，不发生位置偏移。
6. WHEN Track 已有部件且仍有空余格子 THEN 系统 SHALL 正确计算 `FreeSpace = Capacity - UsedSpace`，不错误触发替换逻辑。
7. WHEN `FreeSpace` 足够放置新部件 THEN 系统 SHALL 不触发 `EvictBlockingItems`，不错误驱逐已放置的部件。
8. WHEN Layer 已达最大容量（4 列）THEN 系统 SHALL 拒绝进一步解锁并返回 `false`。
9. WHEN 代码引用 Track 网格尺寸 THEN 系统 SHALL 从运行时 `layer.Cols` / `layer.Rows` / `layer.Capacity` 读取，不使用硬编码常量 `GRID_COLS` / `GRID_ROWS` / `GRID_SIZE`。

---

### 需求 4：单元测试覆盖容量解锁逻辑（Bug 4 — 测试）

**用户故事：** 作为开发者，我希望 `SlotLayer` 的容量解锁逻辑有完整的单元测试覆盖，以便在未来修改时快速发现回归问题。

#### 验收标准

1. WHEN 运行 `SlotLayerTests` THEN 系统 SHALL 通过所有现有测试（`FreeSpace` 断言从 `GRID_SIZE` 改为 `layer.Capacity`）。
2. WHEN 调用 `TryUnlockColumn()` 一次 THEN 测试 SHALL 验证 `Capacity` 从 2 增加到 4（1 列 → 2 列）。
3. WHEN Layer 已达最大列数（4 列）时调用 `TryUnlockColumn()` THEN 测试 SHALL 验证返回 `false` 且 `Capacity` 保持 8 不变。
4. WHEN 解锁列后放置部件 THEN 测试 SHALL 验证新列的格子可以正常放置和移除部件，旧列数据不受影响。

---

## 非功能性需求

| 类别 | 要求 |
|------|------|
| **性能** | `TryUnlockColumn()` 的网格扩容操作（数组复制）在 Track 最大规模（4×2）下不产生可感知卡顿 |
| **存档兼容性** | 新增的 `CoreLayerCols` / `PrismLayerCols` 字段需有默认值（`= 1`），确保旧存档加载时不报错、不丢失数据 |
| **规则统一性** | 背包与 Track 的 empty cell 处理逻辑（透明 + 关闭 raycast）必须一致，不允许两套不同规则 |
| **原子性** | 部件放置操作必须是原子的：先检查所有 shape cells 是否空闲，全部通过才标记，任意一格失败则整体拒绝 |

---

## 错误处理

| 场景 | 期望行为 |
|------|----------|
| 存档中 `CoreLayerCols` 缺失（旧存档） | 使用默认值 `1`，不抛异常 |
| `TryUnlockColumn()` 在已满时被调用 | 返回 `false`，不修改状态，不抛异常 |
| 部件 shape cells 超出 Track 当前边界 | `TryPlace()` 返回 `false`，拒绝放置，不修改 grid 状态 |
| `bgImage` 为 null（未赋值） | 空值守卫（null check），不抛 NullReferenceException |

---

## 涉及文件

| 文件 | Bug | 改动类型 |
|------|-----|----------|
| `Assets/Scripts/UI/Inventory/InventoryItemView.cs` | Bug 1 | 修改 |
| `Assets/Scripts/UI/StarChart/ItemOverlayView.cs` | Bug 3 | 修改 |
| `Assets/Scripts/StarChart/SlotLayer.cs` | Bug 4 | 修改（删除 const，新增动态容量） |
| `Assets/Scripts/StarChart/WeaponTrack.cs` | Bug 4 | 修改（构造函数 + 解锁 API） |
| `Assets/Scripts/Core/Save/TrackSaveData.cs` | Bug 4 | 修改（新增容量字段） |
| `Assets/Scripts/UI/StarChart/SlotCellView.cs` | Bug 4 | 修改（替换常量引用） |
| `Assets/Scripts/UI/StarChart/TrackView.cs` | Bug 4 | 修改（替换常量引用） |
| `Assets/Scripts/Combat/Tests/SlotLayerTests.cs` | Bug 4 | 修改（更新断言 + 新增测试） |
