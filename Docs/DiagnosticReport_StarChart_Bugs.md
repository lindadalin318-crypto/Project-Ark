# StarChart Track 显示 — Bug 诊断报告

> 日期: 2026-03-05
> 状态: 待修复
> 方案选定: **方案 A — 背包与 Track 规则统一（L 形空洞格可嵌入 1×1 部件）**

---

## 设计意图总纲（方案 A）

背包和 Track 遵循**同一套规则**：

| 规则 | 说明 |
|------|------|
| **占位基于 shape cells** | L 形只标记 3 个 active cells 为 occupied，空洞格不标记 |
| **空洞格可嵌入** | 其他 1×1 部件可以放入 L 形的空洞格（与 Track 行为一致） |
| **视觉基于 bounding box** | L 形卡片的 RectTransform 是 2×2，但背景透明，只有 active cells 着色 |
| **空洞格透明且可交互** | empty cell 视觉透明，raycast 穿透，不阻挡其他部件的放置 |

这与 HTML 原型（`StarChartUIPrototype.html`）的精神一致——俄罗斯方块式堆积，但比原型更进一步：**空洞格允许嵌入**，最大化背包利用率，与 Track 规则统一，玩家只需学一套规则。

---

## Bug 1：L 形部件在背包中视觉错误（实心方块而非 L 形）

### 现象

L 形部件在背包中显示为**实心 2×2 方块**（4 格全部着色），而非正确的"3 格着色 + 1 格透明空洞"。同时，L 形卡片的透明空洞区域**无法被其他部件放入**（应该可以）。

### 根因

**问题 A：`bgImage` 不透明**

`InventoryItemView.Setup()` 中 `bgImage.color` 被设为不透明色，导致整个 2×2 卡片背景被填满，空洞格的 `Color.clear` cell 被背景色遮住，视觉上看起来是实心方块。

**问题 B：RectTransform 阻挡 raycast**

L 形卡片的 RectTransform 是 2×2 bounding box 大小。即使空洞格视觉透明，该区域仍然属于 `InventoryItemView` 的 GameObject，会响应 raycast，导致其他部件**无法拖拽到空洞格**（交互被阻挡）。

**问题 C：occupancy 与 RectTransform 不一致（次要）**

`MarkOccupiedByShape()` 只标记 3 个 shape cells，但 RectTransform 物理覆盖 4 格。数据层允许放置，但交互层（raycast）阻挡了放置。

### 正确的设计目标（方案 A）

| 层 | 正确行为 |
|----|---------|
| **RectTransform 尺寸** | 2×2 bounding box（L 形 = 2×2，保持不变） |
| **背景 bgImage** | `Color.clear`（完全透明，不填充背景） |
| **active shape cells** | 着色（3 格，L 形的实际格子） |
| **empty shape cells** | `Color.clear` + **`raycastTarget = false`**（透明且不阻挡交互） |
| **occupancy 标记** | 只标记 3 个 shape cells（空洞格不标记，允许其他部件嵌入） |

### 修复方向

1. `InventoryItemView.Setup()` → 确保 `bgImage.color = Color.clear`
2. `InventoryItemView.BuildShapePreview()` → empty cell 的 Image 设置 `raycastTarget = false`
3. `InventoryView.MarkOccupiedByShape()` → 保持只标记 shape cells（3 格），**不改动**

---

## Bug 2：拖拽 Ghost 出现两个东西（正确形状 + 长方形）

### 现象

拖拽 L 形部件时，Ghost 同时显示一个正确的 L 形 cell grid 和一个覆盖整个 bounding box 的矩形 icon。

### 根因

`DragGhostView.Show()` 中存在**双重渲染**：

```csharp
public void Show(StarChartItemSO item)
{
    SetShape(item.Shape);  // ← 创建 _cellImages（正确的 L 形状）

    if (_iconImage != null)
    {
        _iconImage.sprite = item.Icon;
        _iconImage.color = new Color(1f, 1f, 1f, _ghostAlpha); // ← 这是那个长方形！
    }
}
```

- **`_iconImage`**：Ghost 的 stretch Image 子节点，RectTransform 拉伸到整个 Ghost 大小 → 对 L 形显示为 **2×2 矩形**
- **`_cellImages`**（`RebuildShapeGrid` 创建的）：只在 3 个 active cell 位置渲染 → 正确的 **L 形状**

两者叠加，用户看到双重渲染。

### 修复方向

对非 1×1 形状，隐藏 `_iconImage`（或将其缩小到锚点 cell 区域），icon 只显示在锚点格子内而非拉伸到整个 bounding box：

```csharp
// DragGhostView.Show() 中
var bounds = ItemShapeHelper.GetBounds(item.Shape);
bool isMultiCell = bounds.x > 1 || bounds.y > 1;
if (_iconImage != null)
    _iconImage.gameObject.SetActive(!isMultiCell); // 多格形状隐藏 icon，由 cell grid 表达形状
```

---

## Bug 3：L 形部件放到 Track 上导致格子多一个

### 现象

L 形部件放置到 Track 后，视觉上覆盖了 4 个格子（bounding box）而非 3 个（实际 shape cells）。

### 根因

`TrackView.RefreshColumn()` 存在双重渲染：

1. **第一层**：遍历网格，对每个有物品的 cell 调用 `SetOverlay()` — 标记 3 个 cell（正确）
2. **第二层**：为每个唯一物品创建 `ItemOverlayView`

`ItemOverlayView.Setup()` 第 119-143 行创建 `bounds.x * bounds.y` 个子 Image — 对 L 形就是 **4 个 cell**（含 1 个透明空洞）。overlay 的 RectTransform 覆盖整个 bounding box 区域，视觉上多出 1 格。

**本质与 Bug 1 相同**：empty cell 没有设置 `raycastTarget = false`，且 overlay 背景不透明。

### 修复方向

与 Bug 1 统一：
1. `ItemOverlayView.Setup()` → empty cell 的 Image 设置 `raycastTarget = false` 且 `color = Color.clear`
2. overlay 背景 `bgImage.color = Color.clear`

---

## Bug 4：Twin Splits 放置位置错误 / 凭空多格（最严重）

### 现象

- Twin Splits 放在 Prism Track 左下角格子 → 最终占用右上和左下（位置偏移）
- 放在右下格子 → 最终占用右下和凭空多出的第 5 格

### 根因 — 三个独立问题

#### 原因 A：`FreeSpace` 计算错误（核心 Bug）

`SlotLayer.cs` 第 56-57 行：

```csharp
public int FreeSpace => GRID_COLS - UsedSpace;  // BUG: GRID_COLS = 3
```

网格是 **3×2 = 6 格**（`GRID_SIZE = 6`），但 `FreeSpace` 错误地用了 `GRID_COLS = 3`。

影响链：
- 放置 Twin Splits（SlotSize=2）后：`UsedSpace=2`, `FreeSpace=3-2=1`
- 放第二个 Twin Splits：`HasSpaceForItem` 判断 `FreeSpace < SlotSize` → `1 < 2` → **返回 false**
- 但实际还有 4 个空位！

#### 原因 B：错误触发替换逻辑

`SlotCellView.ComputeDropValidity()` 第 453 行：

```csharp
bool hasSpace = OwnerTrack.HasSpaceForItem(payload.Item, SlotType == SlotType.Core);
bool valid = typeMatch && hasSpace;
isReplace = typeMatch && !hasSpace && IsOccupied;  // hasSpace=false → isReplace=true（错误！）
```

`FreeSpace` 错误 → `hasSpace=false` → 错误触发替换逻辑 → 位置偏移。

#### 原因 C：`EvictBlockingItems` 也受 `FreeSpace` 影响

`DragDropManager.cs` 第 280 行：

```csharp
while (layer.FreeSpace < newItem.SlotSize && layer.Items.Count > 0)
{
    var victim = layer.Items[layer.Items.Count - 1];
    track.UnequipPrism(victim as PrismSO);
}
```

`FreeSpace` 被错误计算 → 错误驱逐不该驱逐的物品 → 产生"凭空多格"和位置错乱。

### 修复方向 — Track 容量动态解锁系统

#### 设计目标

| 参数 | 值 |
|------|-----|
| 初始容量 | **2 格**（1 列 × 2 行） |
| 每次解锁 | +1 列（+2 格） |
| 最大容量 | **8 格**（4 列 × 2 行） |
| 布局方式 | **2D M×N**，行数固定为 2，列数动态增长 |
| 容量存储 | **存档**（`TrackSaveData`），每条 Track 独立存储 |

解锁阶段一览：

| 解锁阶段 | Cols | Rows | 总格子数 | 视觉 |
|---------|------|------|---------|------|
| 初始 | 1 | 2 | 2 | 1×2 |
| 解锁 1 | 2 | 2 | 4 | 2×2（当前 UI 显示的） |
| 解锁 2 | 3 | 2 | 6 | 3×2 |
| 解锁 3 | 4 | 2 | 8 | 4×2（最大） |

---

#### 改动 1：`SlotLayer<T>` — 删除 const，改为运行时 Capacity

**删除**以下三个常量（`GRID_COLS = 3` 是 1D→2D 迁移遗留值，与当前设计不符）：

```csharp
// 删除：
// public const int GRID_COLS = 3;
// public const int GRID_ROWS = 2;
// public const int GRID_SIZE = 6;
```

**新增**：

```csharp
public const int MAX_COLS = 4;       // 最大列数（4×2=8 格）
public const int FIXED_ROWS = 2;     // 行数固定为 2

public int Cols { get; private set; }           // 当前已解锁列数（1~4）
public int Rows => FIXED_ROWS;                  // 行数固定
public int Capacity => Cols * Rows;             // 当前总格子数

// 构造函数接受初始列数（默认 1 = 初始 2 格）
public SlotLayer(int initialCols = 1)
{
    Cols = Mathf.Clamp(initialCols, 1, MAX_COLS);
    _grid = new T[Rows, Cols];
}

// 解锁一列（+2 格），由游戏进度系统调用
public bool TryUnlockColumn()
{
    if (Cols >= MAX_COLS) return false;
    Cols++;
    var newGrid = new T[Rows, Cols];
    // 复制旧数据
    for (int r = 0; r < Rows; r++)
        for (int c = 0; c < Cols - 1; c++)
            newGrid[r, c] = _grid[r, c];
    _grid = newGrid;
    return true;
}

// FreeSpace 修复（原 GRID_COLS → Capacity）
public int FreeSpace => Capacity - UsedSpace;
```

---

#### 改动 2：`TrackSaveData` — 新增容量字段

```csharp
[Serializable]
public class TrackSaveData
{
    public List<string> CoreIDs  = new();
    public List<string> PrismIDs = new();

    // 新增：各 Layer 已解锁的列数（默认 1 = 初始 2 格）
    public int CoreLayerCols  = 1;
    public int PrismLayerCols = 1;
}
```

---

#### 改动 3：`WeaponTrack` — 构造时传入容量，暴露解锁 API

```csharp
// 构造时从存档读取列数
public WeaponTrack(TrackId id, int coreLayerCols = 1, int prismLayerCols = 1)
{
    _coreLayer  = new SlotLayer<StarCoreSO>(initialCols: coreLayerCols);
    _prismLayer = new SlotLayer<PrismSO>(initialCols: prismLayerCols);
}

// 解锁 API（由游戏进度系统调用）
public bool UnlockCoreColumn()  => _coreLayer.TryUnlockColumn();
public bool UnlockPrismColumn() => _prismLayer.TryUnlockColumn();
```

---

#### 改动 4：UI 层 — 从 `layer.Cols` / `layer.Rows` 读取（替换常量引用）

以下文件中所有引用 `GRID_COLS` / `GRID_ROWS` / `GRID_SIZE` 的地方，改为从 `layer.Cols` / `layer.Rows` / `layer.Capacity` 读取：

- `SlotCellView.cs`（第 496-497 行附近）
- `TrackView.cs`（第 228、230、236、395、397、445 行附近）

---

#### 改动 5：`SlotLayerTests.cs` — 更新测试

```csharp
// 修复前（硬编码 GRID_SIZE）
Assert.AreEqual(SlotLayer<StarCoreSO>.GRID_SIZE, layer.FreeSpace);

// 修复后（使用 Capacity）
Assert.AreEqual(layer.Capacity, layer.FreeSpace);

// 新增：容量解锁测试
[Test]
public void TryUnlockColumn_IncreasesCapacityBy2()
{
    var layer = new SlotLayer<StarCoreSO>(initialCols: 1); // 初始 2 格
    Assert.AreEqual(2, layer.Capacity);
    layer.TryUnlockColumn();
    Assert.AreEqual(4, layer.Capacity);
}

[Test]
public void TryUnlockColumn_CannotExceedMaxCols()
{
    var layer = new SlotLayer<StarCoreSO>(initialCols: 4); // 已满
    bool result = layer.TryUnlockColumn();
    Assert.IsFalse(result);
    Assert.AreEqual(8, layer.Capacity);
}
```

---

## 根因总结

| Bug | 根因 | 严重度 | 修复难度 |
|-----|------|--------|----------|
| **Bug 1** | `bgImage` 不透明（空洞格被填满）；empty cell 未设 `raycastTarget=false`（阻挡嵌入交互） | 🟡 中 | 🟢 易 |
| **Bug 2** | `_iconImage` 拉伸到 bounding box 与 shape cell grid 双重渲染 | 🟡 中 | 🟢 易 |
| **Bug 3** | `ItemOverlayView` empty cell 未透明/未关闭 raycast（同 Bug 1） | 🟡 中 | 🟢 易 |
| **Bug 4** | `FreeSpace = GRID_COLS - UsedSpace`（应为 `GRID_SIZE`），触发错误替换和驱逐逻辑 | 🔴 高 | 🟢 易 |

---

## Gap 确认

**用户描述完全准确，不是沟通问题。** 这些 bug 是实现细节遗漏：

1. **设计意图误解（已澄清）**：背包的正确设计是俄罗斯方块式——L 形占 2×2 bounding box，空洞格透明。**方案 A 选定**：空洞格可被 1×1 部件嵌入，与 Track 规则完全统一，玩家只需学一套规则。当前实现的问题是 `bgImage` 不透明 + empty cell 阻挡 raycast，而非 occupancy 逻辑错误。

2. **FreeSpace bug**：从 1D→2D 网格迁移时的遗留问题。`FreeSpace = GRID_COLS - UsedSpace` 是旧的 1D 逻辑（1×3 网格），迁移到 2D（3×2 网格）后没有同步更新，导致空间判断、替换逻辑全部错误。

---

## 修复计划

| 优先级 | 修复项 | 涉及文件 | 关键改动 | 状态 |
|--------|--------|----------|----------|------|
| **P0** | Track 容量动态解锁系统 | `SlotLayer.cs`、`WeaponTrack.cs`、`TrackSaveData.cs`、`SlotCellView.cs`、`TrackView.cs`、`SlotLayerTests.cs` | 删除 `GRID_COLS/ROWS/SIZE` const；改为运行时 `Cols`/`Rows`/`Capacity`；新增 `TryUnlockColumn()`；存档新增容量字段 | ⏳ 待修复 |
| **P1** | Ghost 隐藏 `_iconImage`（非 1×1 形状） | `DragGhostView.cs` | `_iconImage.color = Color.clear` | ✅ 已完成 |
| **P1** | 背包 bgImage 透明 + empty cell 关闭 raycast | `InventoryItemView.cs` | `bgImage.color = Color.clear`；empty cell `raycastTarget = false` | ⏳ 待修复 |
| **P1** | Track overlay empty cell 透明 + 关闭 raycast | `ItemOverlayView.cs` | empty cell `color = Color.clear`；`raycastTarget = false` | ⏳ 待修复 |

---

## 修复实施记录

### ✅ Bug 2 — DragGhostView 双重渲染（已完成 2026-03-05 12:31）

**修改文件**：`Assets/Scripts/UI/DragDrop/DragGhostView.cs`

**改动内容**（`Show()` 方法）：

```csharp
// 修复前：根据 item.Icon 设置 sprite + alpha → 拉伸矩形叠在 cell grid 上
if (item.Icon != null)
{
    _iconImage.sprite = item.Icon;
    _iconImage.color = new Color(1f, 1f, 1f, _ghostAlpha);
}
else
{
    var baseColor = StarChartTheme.GetTypeColor(item.ItemType);
    _iconImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, _ghostAlpha);
}

// 修复后：始终清空 icon，形状完全由 cell grid 表达
_iconImage.sprite = null;
_iconImage.color = Color.clear;
```

**验收结果**：拖拽时只显示 cell grid（L 形就是 L 形），与背包中部件视觉形态一致。

---

### ⏳ Bug 4 — Track 容量动态解锁系统（待修复）

**修改文件**：
- `Assets/Scripts/StarChart/SlotLayer.cs`
- `Assets/Scripts/StarChart/WeaponTrack.cs`
- `Assets/Scripts/Core/Save/TrackSaveData.cs`（或存档数据所在文件）
- `Assets/Scripts/UI/StarChart/SlotCellView.cs`
- `Assets/Scripts/UI/StarChart/TrackView.cs`
- `Assets/Scripts/Combat/Tests/SlotLayerTests.cs`

**改动摘要**：

1. **`SlotLayer.cs`**：删除 `GRID_COLS=3`、`GRID_ROWS=2`、`GRID_SIZE=6` 三个 const（`GRID_COLS=3` 是 1D→2D 迁移遗留值，与设计不符）；改为运行时 `Cols`（动态）、`Rows=2`（固定）、`Capacity = Cols * Rows`；新增 `TryUnlockColumn()` 方法；修复 `FreeSpace => Capacity - UsedSpace`

2. **`WeaponTrack.cs`**：构造函数接受 `coreLayerCols` / `prismLayerCols` 参数（默认 1，即初始 2 格）；暴露 `UnlockCoreColumn()` / `UnlockPrismColumn()` API

3. **`TrackSaveData.cs`**：新增 `CoreLayerCols = 1` 和 `PrismLayerCols = 1` 字段，存档时持久化容量

4. **`SlotCellView.cs` / `TrackView.cs`**：所有 `GRID_COLS` / `GRID_ROWS` / `GRID_SIZE` 引用改为从 `layer.Cols` / `layer.Rows` / `layer.Capacity` 读取

5. **`SlotLayerTests.cs`**：`FreeSpace == GRID_SIZE` 断言改为 `FreeSpace == layer.Capacity`；新增容量解锁测试

**验收标准**：
- [ ] 新游戏开始时，每条 Track 显示 1×2 = 2 格
- [ ] 解锁后，Track 扩展为 2×2 = 4 格（当前 UI 显示的状态）
- [ ] 最大解锁后，Track 为 4×2 = 8 格
- [ ] 容量存入存档，重新加载游戏后容量正确恢复
- [ ] Twin Splits 放在左下角 → 占用正确的 2 格，不偏移
- [ ] 放置第二个 Twin Splits 时不触发错误替换逻辑
- [ ] `EvictBlockingItems` 不再错误驱逐物品
- [ ] `SlotLayerTests` 全部通过
---

### ⏳ Bug 1 — 背包 L 形部件视觉错误（待修复）

**修改文件**：`Assets/Scripts/UI/Inventory/InventoryItemView.cs`

**改动内容**：

```csharp
// 1. Setup() 中 — bgImage 改为透明
bgImage.color = Color.clear;  // 原来是不透明色

// 2. BuildShapePreview() 中 — empty cell 关闭 raycast
foreach (var cell in shapeCells)
{
    bool isActive = item.Shape.IsOccupied(cell.x, cell.y);
    var img = cell.GetComponent<Image>();
    if (!isActive)
    {
        img.color = Color.clear;
        img.raycastTarget = false;  // ← 新增：空洞格不阻挡交互
    }
}
```

**验收标准**：
- [ ] L 形部件在背包中显示为 3 格着色 + 1 格透明（非实心 2×2）
- [ ] 其他 1×1 部件可以拖拽到 L 形的空洞格并成功放置
- [ ] 1×1 部件放入空洞格后，背包 occupancy 正确（L 形 3 格 + 1×1 的 1 格 = 4 格全满）

---

### ⏳ Bug 3 — Track Overlay empty cell 视觉错误（待修复）

**修改文件**：`Assets/Scripts/UI/StarChart/ItemOverlayView.cs`

**改动内容**（`Setup()` 方法，第 119-143 行附近）：

```csharp
// 遍历 bounding box 内所有 cell 时，对 empty cell 处理：
for (int row = 0; row < bounds.y; row++)
{
    for (int col = 0; col < bounds.x; col++)
    {
        bool isActive = item.Shape.IsOccupied(col, row);
        var cellImg = CreateCellImage(col, row);  // 创建 cell Image
        if (!isActive)
        {
            cellImg.color = Color.clear;           // 透明
            cellImg.raycastTarget = false;         // 不阻挡交互
        }
    }
}

// overlay 背景也需要透明
if (bgImage != null)
    bgImage.color = Color.clear;
```

**验收标准**：
- [ ] L 形部件放到 Track 后，视觉上只覆盖 3 个格子（非 4 个）
- [ ] Track 上 L 形的空洞格可以放入其他 1×1 部件
- [ ] 格子数量与放置前一致，不多出额外格子
