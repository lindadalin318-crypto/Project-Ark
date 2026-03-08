# 实施计划：SlotLayer 行列双向解锁重构

## 文件影响范围

| 文件 | 改动类型 |
|------|---------|
| `Assets/Scripts/Combat/StarChart/SlotLayer.cs` | 核心重构 |
| `Assets/Scripts/Combat/StarChart/WeaponTrack.cs` | 初始化 + 新增 SetLayerRows |
| `Assets/Scripts/Core/Save/SaveData.cs` | 新增 `*Rows` 字段 |
| `Assets/Scripts/Combat/Tests/SlotLayerTests.cs` | 更新 + 新增测试 |

> `ItemShapeHelper.cs` 和 `TrackView.cs` **无需修改**：
> - `ItemShapeHelper` 的坐标系已正确（col=横向，row=纵向），`FitsInGrid(shape, col, row, gridCols, gridRows)` 签名不变
> - `TrackView.RefreshColumn` 使用 `layer.Capacity`（= `Cols * Rows`），逻辑自动适配

---

- [ ] 1. 重构 `SlotLayer<T>` 核心数据结构
   - 删除 `FIXED_ROWS = 2` 常量，新增 `MAX_ROWS = 4` 常量（保留 `MAX_COLS = 4`）
   - 将 `Rows` 从 `=> FIXED_ROWS` 改为 `{ get; private set; }`，初始值由构造函数设置
   - 将内部 `_grid` 从 `new T[FIXED_ROWS, MAX_COLS]` 改为 `new T[MAX_ROWS, MAX_COLS]`
   - 构造函数签名改为 `SlotLayer(int initialCols = 2, int initialRows = 1)`，两个参数均 Clamp 到合法范围
   - 新增 `TryUnlockRow()` 方法：`Rows < MAX_ROWS` 时 `Rows++` 返回 `true`，否则返回 `false`
   - 将现有 `TryUnlockColumn()` 保留不变（已正确实现）
   - _需求：1.1、1.2、1.3、1.4、1.5、1.6、1.7、1.8、2.1、2.2、3.1、3.2、3.3、3.4_

- [ ] 2. 更新 `WeaponTrack` 初始化与行解锁支持
   - 将构造函数中三个 Layer 的初始化改为 `new SlotLayer<T>(initialCols: 2, initialRows: 1)`
   - 更新 `SetLayerCols` 中的 Clamp 逻辑：最小值从 `1` 改为 `2`（新的最小合法列数）
   - 新增 `SetLayerRows(int coreRows, int prismRows, int satRows)` 方法，逻辑与 `SetLayerCols` 对称（逐步调用 `TryUnlockRow` 直到达到目标行数）
   - _需求：5.1、5.2、5.3_

- [ ] 3. 更新 `SaveData.cs` 存档结构
   - 在 `TrackSaveData` 中新增三个字段：`CoreLayerRows = 1`、`PrismLayerRows = 1`、`SatLayerRows = 1`
   - 将 `CoreLayerCols`、`PrismLayerCols`、`SatLayerCols` 的默认值从 `1` 改为 `2`（反映新的初始状态）
   - 在 `LoadoutSlotSaveData` 中新增 `SailLayerRows = 1` 字段
   - 更新各字段的 XML 注释，说明旧存档迁移规则（`Cols=0` 时 Clamp 到 `2`，`Rows=0` 时 Clamp 到 `1`）
   - _需求：4.1、4.2、4.3、4.4_

- [ ] 4. 更新 `SlotLayerTests.cs` 测试套件
   - 4.1 更新现有测试以反映新初始状态（`Cols=2, Rows=1`）
      - `InitialCapacity_Is2_With1Col` → 重命名为 `InitialCapacity_Is2_With2Cols1Row`，断言 `Cols=2, Rows=1, Capacity=2`
      - `TryUnlockColumn_IncreasesCapacityBy2` → 更新断言：解锁后 `Cols=3, Capacity=3`（而非旧的 `Cols=2, Capacity=4`）
      - `TryUnlockColumn_AtMaxCols_ReturnsFalse` → 更新初始化为 `initialCols: MAX_COLS`，断言 `Capacity = MAX_COLS * 1`
      - `TryUnlockColumn_MaxCapacity_Is8` → 更新为 `MaxCapacity_Is8_WithMaxColsAndInitialRow`，断言 `Capacity=4`（4列×1行）
      - `InitialCols_Constructor_SetsCorrectCapacity` → 更新断言：`initialCols:2` → `Capacity=2`，`initialCols:4` → `Capacity=4`
      - `EquipTwoSize1_FillsInitialGrid` → 更新注释，初始2格仍然是2格（`Cols=2, Rows=1`）
      - `Overflow_ReturnsFalse` → 初始仍是2格，逻辑不变，更新注释
      - 删除对 `FIXED_ROWS` 的所有引用，改为直接使用 `layer.Rows`
      - _需求：8.1、8.2_
   - 4.2 新增行解锁相关测试
      - 新增 `TryUnlockRow_IncreasesRows`：验证 `Rows` 从 `1` 增加到 `2`，`Capacity` 从 `2` 增加到 `4`
      - 新增 `TryUnlockRow_AtMaxRows_ReturnsFalse`：验证 `initialRows: MAX_ROWS` 时返回 `false`
      - 新增 `TryUnlockCol_And_Row_MaxCapacity_Is16`：解锁到 `MAX_COLS=4, MAX_ROWS=4`，验证 `Capacity=16`
      - 新增 `UnlockRow_NewCellsAreEmpty_OldItemsUnaffected`：解锁行后旧物品不受影响，新行为空
      - _需求：8.3、8.4_
