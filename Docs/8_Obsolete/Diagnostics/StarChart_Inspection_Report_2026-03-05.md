# StarChart Bug 检验报告

> **日期**: 2026-03-05 14:45
> **检查范围**: DiagnosticReport_StarChart_Bugs.md 中列出的全部 4 个 Bug + 截图反映的额外问题
> **状态**: 🔴 大部分 Bug 未修复

---

## 一、修复状态总览

| Bug | 诊断报告状态 | **实际代码状态** | 结论 |
|-----|-------------|-----------------|------|
| **Bug 1** — 背包 L 形部件显示为实心矩形 | ⏳ 待修复 | ✅ **已修复** | `bgImage.color = Color.clear` ✓；empty cell `raycastTarget = false` ✓ |
| **Bug 2** — Ghost 双重渲染 (icon + cell grid) | ✅ 已完成 | ✅ **已修复** | `_iconImage.color = Color.clear` ✓ |
| **Bug 3** — Track overlay empty cell 多一格 | ⏳ 待修复 | ✅ **已修复** | `ItemOverlayView.Setup()` 中 empty cell `color = Color.clear` + `raycastTarget = false` ✓ |
| **Bug 4** — FreeSpace 计算错误 / Track 容量系统 | ⏳ 待修复 | ✅ **已修复** | `SlotLayer<T>` 重写为动态 `Cols`/`Rows`/`Capacity`；`FreeSpace = Capacity - UsedSpace` ✓ |

### 代码层面 Bug 1-4 的核心修复确认 ✅

1. **`SlotLayer.cs`** — `GRID_COLS`/`GRID_ROWS`/`GRID_SIZE` 常量已全部删除，替换为 `Cols`(动态) / `Rows`(=2) / `Capacity` / `FreeSpace = Capacity - UsedSpace`。`TryUnlockColumn()` 方法存在。✅
2. **`WeaponTrack.cs`** — `SetLayerCols()` 方法存在，接受 `coreCols`/`prismCols` 参数。✅
3. **`TrackSaveData` (SaveData.cs)** — `CoreLayerCols = 1` 和 `PrismLayerCols = 1` 字段已添加。✅
4. **`StarChartController.cs`** — `ExportTrack` 保存 `CoreLayerCols`/`PrismLayerCols`，`ImportTrack` 调用 `SetLayerCols`。✅
5. **`InventoryItemView.cs`** — `bgImage.color = Color.clear`，`BuildShapePreview()` empty cell `raycastTarget = false`。✅
6. **`ItemOverlayView.cs`** — empty cell `color = Color.clear`，`raycastTarget = false`。✅
7. **`DragGhostView.cs`** — `_iconImage.color = Color.clear`，`_iconImage.sprite = null`。✅

---

## 二、截图反映的仍存在的问题

尽管代码层面的 Bug 1-4 核心修复 **已正确完成**，截图中仍然存在 **6 个明显异常**，以下逐一分析根因：

---

### 🔴 问题 A：背包中部件重叠（Boomerang 和 Pulse Wave 叠在一起）

**现象**: 截图中背包区域，Boomerang 和 Pulse Wave 部件在视觉上重叠，占据了同一个区域。

**根因分析**:

`InventoryView.cs` 的自定义 packing 算法 (`TryFindAnchorForShape`) 正确地使用 shape cells 而非 bounding box 做 occupancy 标记（`MarkOccupiedByShape` 只标记 active cells）。

但问题在于：**视觉定位使用 bounding box（`anchoredPosition` 基于 anchor col/row），而 occupancy 只标记 active cells**。

对于 L 形部件：
- occupancy 只标记 3 个 cells
- 但 `RectTransform.sizeDelta` 是 bounding box（2×2 = 4 cells 大小）
- **空洞格虽然 occupancy 上没有被标记（允许其他部件放入），但 RectTransform 在视觉上仍然覆盖了那个空间**

当另一个部件被 packing 算法放到 L 形的空洞格时，**两个部件的 RectTransform 在视觉上重叠**。

然而仔细看截图中的重叠 —— Boomerang（Prism，1×1 shape）和 Pulse Wave（Prism，[4] count）似乎并非 L 形部件。这更可能是 **Boomerang 和 Pulse Wave 的 `_shape` 字段缺失/默认为 `Shape1x1`（枚举值 0）导致多个部件被放到同一个网格位置**。

**验证**: 检查 asset 文件：
- `ShebaP_TwinSplit.asset` — **没有 `_shape` 字段** → 默认值 `0` = `Shape1x1`（但 `_slotSize: 1`）
- `FractalPrism_TwinSplit.asset` — `_shape: 1` = `Shape1x2H`（水平 2 格），`_slotSize: 2` ✓

**大多数 Prism/Core asset 文件都没有 `_shape` 字段**，只在 3 个 asset 中找到了该字段。这意味着绝大多数部件的 `Shape` 默认为 `Shape1x1`，即使它们的 `_slotSize > 1`。

**🔴 这不是 Bug — 但需要确认**：如果所有 `_slotSize = 1` 的部件确实都是 1×1，那 packing 算法应该能正确处理。重叠的原因可能是 **背包 `_gridColumns = 8`，但 cell 大小和窗口尺寸不匹配，导致部件在小窗口下视觉重叠**。

**实际最可能原因**: `InventoryItemView` 的形状预览 cells 使用了 `cellGo.transform.SetAsFirstSibling()`，把 shape cells 插入到 sibling 列表最前面。但如果 `_contentParent` 使用了绝对定位（`anchoredPosition`），**多个 InventoryItemView 的绝对位置可能因为 `_cellSize`/`_cellGap` 计算与实际 GridLayout 不匹配而发生重叠**。

**结论**: 需要在 Play Mode 中具体检查背包网格的 cell 尺寸是否与 `_cellSize = 80` 匹配，以及是否有部件的 `_shape` 配置不正确。

---

### 🔴 问题 B：Track 格子数量不一致（有的 2 格，有的 4 格）

**现象**: 截图中 PRIMARY track 的 PRISM 显示 2×2=4 格，CORE 也显示 2×2=4 格；但 SECONDARY track 有些列显示 2 格，有些显示 4 格。

**根因分析**:

1. **`WeaponTrack` 构造时默认 `initialCols = 1`**（`SlotLayer` 构造函数默认值）→ 初始状态 = 1×2 = 2 格
2. **`SetLayerCols` 从存档读取**，如果存档中有 `CoreLayerCols=2, PrismLayerCols=2`，则变成 2×2=4 格
3. **如果没有存档**（新游戏或测试），默认 `CoreLayerCols=1, PrismLayerCols=1` → 2 格

**问题关键**: `TypeColumn._cells = new SlotCellView[4]`（硬编码 4 个 cell），`UICanvasBuilder` 也创建 4 个 cell。

`TrackView.RefreshColumn()` 的逻辑：
```csharp
int unlockedCount = layer != null ? layer.Rows * layer.Cols : 0;
for (int i = 0; i < cells.Length; i++)
    cells[i].gameObject.SetActive(i < unlockedCount);
```

**这里使用了 `gameObject.SetActive(false)` 来隐藏未解锁的格子！违反了 CLAUDE.md 第 11 条**（uGUI 面板禁止用 SetActive 控制显隐）。更关键的是，**不同 track 可能有不同的存档数据导致解锁列数不同**，截图中的不一致正是因此。

**但截图中 PRIMARY track 全部 4 格可见，SECONDARY track 也全部 4 格可见** — 如果存档中都设为 `Cols=2`，这是正确的。

**🟡 真正的问题**: 如果是新建场景测试（没有存档），`Cols` 默认为 1（2 格），但 `UICanvasBuilder` 创建了 4 个 cell，`TrackView` 通过 `SetActive(false)` 隐藏多余的 2 个。**这导致 SetActive 违规 + 视觉上只有 2 格**。

---

### 🔴 问题 C：Twin Split (1×2H，水平 2 格) 放不进 Prism 的两个格子

**现象**: Twin Split 是 `_shape: 1` = `Shape1x2H`（水平 2 列 × 1 行），但无法放入 Prism track 的两个相邻格子。

**根因分析**:

`FractalPrism_TwinSplit.asset`:
- `_slotSize: 2`
- `_shape: 1` → `Shape1x2H` → cells: `(0,0), (1,0)` → 需要 2 列 × 1 行

**Prism layer 初始 `Cols = 1`**（默认值）→ 只有 1 列 × 2 行 = 2 格。

`Shape1x2H` 需要 **2 列** 但 Prism 只有 **1 列** → `FitsInGrid` 返回 `false` → 无法放置！

即使 `Cols = 2`（通过存档或解锁），那么是 2 列 × 2 行 = 4 格。`Shape1x2H` 需要 (0,0)+(1,0)，在 2×2 网格中可以放在第 0 行或第 1 行。应该能放。

**🔴 核心问题**: `HasSpaceForItem` 调用 `FindFirstAnchor`，它在层中寻找一个不需要驱逐的 anchor。如果 **`Cols = 1`**（未解锁），`Shape1x2H` 永远放不进去，因为它需要 2 列。

**验证**: 截图中 PRISM 显示 2×2 格子（4 格）。如果确实是 `Cols=2`，那么 Twin Split 应该能放入。但如果 **`FindFirstAnchor` 扫描所有位置都因为其他部件占用而返回 false**，那也放不进。

另一个可能：**Prism track 已有其他部件占据了两列的顶行**，导致 Twin Split 找不到连续的 2 列空位。

**🟡 需要在 Play Mode 中验证实际的 Cols 值和占用情况**。

---

### 🔴 问题 D：拖拽时 hover 在 track 上出现 icon

**现象**: 拖拽 Ghost 时，`_iconImage.color = Color.clear` 确实在 `DragGhostView.Show()` 中设置了。但 hover 在 track cell 上时，**SlotCellView.SetOverlay()** 或 **SlotCellView.SetItem()** 在 cell 上显示了 icon。

这不是 Ghost 上的 icon，而是 **track cell 自身的 icon 在拖拽预览时被设置了**。

**根因分析**:

`SlotCellView.OnPointerEnter()` 在拖拽 hover 时调用 `ComputeDropValidity`，设置了 highlight 但 **不应该修改 cell 的 icon**。

然而 `TrackView.RefreshColumn()` 在 Refresh 时调用 `cells[cellIndex].SetOverlay(item, isPrimary: false)`，这会设置 `_iconImage.enabled = false`（secondary cell）。但 primary cell 会调用 `cells[cellIndex].SetOverlay(item, isPrimary: true)`，此时 `_iconImage.enabled = true` 并显示 icon。

**🔴 问题**: 实际上用户描述的是「拖拽 Ghost 时 hover 在 track 上又出现了 icon」。这可能是指：

1. **ItemOverlayView 已经在 track 上显示了 item icon**（通过 `ItemOverlayView.Setup()` 中的 `_iconImage`），这是正常行为
2. 或者用户说的是 **Ghost 上的 icon 在 hover 时被重新显示**

如果是后者，`DragGhostView.SetDropState()` 只修改 `_borderImage` 颜色和 `_replaceHintLabel`，**不会修改 `_iconImage`**。所以 Ghost 上不应该重新出现 icon。

**最可能的原因**: 用户看到的「icon」不是 Ghost 上的，而是 **`ItemOverlayView` 在 track 上的 icon image**（`_iconImage` 设置为 item.Icon），覆盖在 cell 上。当 Ghost 飘到 track 上方时，Ghost 的 cell grid 和 overlay 的 icon **叠加**在一起，视觉上像是「hover 时出现了 icon」。

**但更可能的根因是**: 查看 `SlotCellView.SetOverlay()` — 当 `isPrimary: false` 时 `_iconImage.enabled = false`（正确）。`TrackView.RefreshColumn()` 对非 anchor cell 传入 `isPrimary: false`。但对 **anchor cell 也传入了 `isPrimary: false`**！

```csharp
cells[cellIndex].SetOverlay(item, isPrimary: false); // hide placeholder, no icon
```

所以 track cell 的 icon 应该是被隐藏的。那 icon 来自哪里？**来自 `ItemOverlayView`**！

`ItemOverlayView.Setup()` 第 163-184 行创建了 `_iconImage` 并设置 `item.Icon`。这个 icon 居中在 anchor cell 上。这是**正常的装备显示行为**。

**但如果用户拖拽时，这个已装备的 overlay icon 仍然可见（因为 overlay 没有在拖拽时隐藏），加上 Ghost 的 cell grid，看起来就像"hover 时出现了 icon"**。

**🟡 结论**: 如果用户是从 track 拖出一个已装备部件，`ItemOverlayView` 不会在拖拽开始时隐藏（因为 Refresh 没有被调用）。Ghost 和 overlay 同时可见 = 视觉重叠。需要在 `BeginDrag` 时触发 track Refresh 来移除 overlay。

---

### 🟡 问题 E：`TrackView.RefreshColumn` 使用 `SetActive(false)` 隐藏锁定格子

**代码位置**: `TrackView.cs` 第 243 行

```csharp
cells[i].gameObject.SetActive(i < unlockedCount);
```

**违反**: CLAUDE.md 第 11 条 — uGUI 面板禁止用 `SetActive` 控制显隐。

**影响**: 
- `SetActive(false)` 会推迟 `Awake()` 执行
- Editor 序列化 inactive 状态
- 应改为 CanvasGroup alpha=0 控制

---

### 🟡 问题 F：`ShebaP_TwinSplit.asset` 缺失 `_shape` 字段

**代码位置**: `Assets/_Data/StarChart/Prisms/ShebaP_TwinSplit.asset`

该 asset 文件中 **没有 `_shape` 字段**，而 `_slotSize: 1`。Unity 序列化默认值 → `_shape = 0 = Shape1x1`。

这意味着 `ShebaP_TwinSplit`（名叫 "Twin Split"）实际上是一个 **1×1 部件**，不是用户期望的 1×2H 水平部件。

对比：`FractalPrism_TwinSplit.asset` 有 `_shape: 1`（Shape1x2H）且 `_slotSize: 2`。

**这可能是用户困惑的来源** — 背包中有两个 "Twin Split"，一个是 1×1，一个是 1×2H，但名字相同。

---

## 三、完整文件审计

| 文件 | 诊断报告中的修复 | 代码验证结果 |
|------|-----------------|-------------|
| `SlotLayer.cs` | 删除 const，改为动态 Capacity | ✅ 完成 — `FIXED_ROWS=2`, `MAX_COLS=4`, 运行时 `Cols`/`Capacity`/`FreeSpace` |
| `WeaponTrack.cs` | 构造接受 cols，暴露 unlock API | ✅ 完成 — `SetLayerCols()` 存在 |
| `TrackSaveData` (SaveData.cs) | 新增 CoreLayerCols/PrismLayerCols | ✅ 完成 — 字段已添加，默认值 1 |
| `StarChartController.cs` | Export/Import 容量 | ✅ 完成 — Export 保存 Cols，Import 调用 SetLayerCols |
| `InventoryItemView.cs` | bgImage 透明 + empty cell raycast off | ✅ 完成 |
| `ItemOverlayView.cs` | empty cell 透明 + raycast off | ✅ 完成 |
| `DragGhostView.cs` | 隐藏 _iconImage | ✅ 完成 |
| `SlotCellView.cs` | GRID_COLS 引用改为 layer.Cols | ✅ 完成 — 使用 `OwnerTrack.Track?.CoreLayer?.Cols` |
| `TrackView.cs` | GRID_COLS 引用改为 layer.Cols | ✅ 完成 — 使用 `layer.Rows * layer.Cols` |
| `SlotLayerTests.cs` | 更新断言 | ❓ 未检查 — 需要单独验证 |

---

## 四、截图中问题的根因定位

| # | 截图问题描述 | 根因 | 严重度 | 修复建议 |
|---|-------------|------|--------|---------|
| **A** | 背包部件重叠（Boomerang + Pulse Wave） | 可能是 `_cellSize=80` 与实际 UI 尺寸不匹配，或 L 形空洞格的 RectTransform 覆盖导致视觉重叠 | 🔴 高 | Play Mode 检查 `_cellSize` 和 `_gridColumns` 与实际窗口匹配；确认部件 `_shape` 配置 |
| **B** | Track 格子数不一致 | 存档中 `CoreLayerCols`/`PrismLayerCols` 不同导致；或新游戏默认 Cols=1 只显示 2 格 | 🟡 中 | 确认存档数据；或在测试时手动 `SetLayerCols(2,2)` |
| **C** | Twin Split 放不进 Prism 2 格 | `Shape1x2H` 需要 2 列，但 Prism layer 可能只有 1 列（Cols=1）| 🔴 高 | 确认 Prism Cols ≥ 2；或改 Twin Split 为 `Shape2x1V`（垂直 2 格可放入 1 列） |
| **D** | 拖拽 hover 时 track 上出现 icon | `ItemOverlayView` 的 icon 在拖拽期间未隐藏，与 Ghost cell grid 叠加 | 🟡 中 | 拖拽从 track 开始时，先 Refresh track（移除 overlay）；或在 overlay 上设置 alpha=0.3 |
| **E** | RefreshColumn 使用 SetActive | 违反 CLAUDE.md 第 11 条 | 🟡 中 | 改用 CanvasGroup.alpha=0 隐藏锁定格子 |
| **F** | ShebaP_TwinSplit 缺失 _shape | asset 配置遗漏 | 🟢 低 | 在 asset 中添加 `_shape: 1` 或确认该变体确实是 1×1 |

---

## 五、结论与优先级建议

### ✅ 已确认修复（代码层面正确）
1. Bug 2: Ghost 双重渲染 → icon 已清空
2. Bug 4: FreeSpace 计算 → 动态 Capacity 系统已实现
3. Bug 1: 背包 empty cell → bgImage 透明 + raycast off
4. Bug 3: Track overlay empty cell → 透明 + raycast off

### 🔴 仍需修复（截图中可见）
| 优先级 | 问题 | 建议的修复方式 |
|--------|------|---------------|
| **P0** | 问题 C — Twin Split 放不进 Prism | ① 确认 Prism `Cols` 值；② 如果 Cols=1 且 shape=1x2H，要么解锁到 Cols=2 要么改 shape 为 2x1V |
| **P0** | 问题 A — 背包部件重叠 | 检查 `_cellSize=80` 是否与 UICanvasBuilder 创建的实际 cell 尺寸匹配；可能需要在 `InventoryView.Refresh()` 中从 `_itemPrefab` 读取实际尺寸 |
| **P1** | 问题 D — 拖拽 hover 时 overlay icon 可见 | `DragDropManager.BeginDrag()` 中如果 source=Slot，应立即调用 `OwnerTrack.Refresh()` 移除 overlay |
| **P1** | 问题 E — SetActive 隐藏格子 | 改为 CanvasGroup 控制 |
| **P2** | 问题 B — Track 格子数不一致 | 这是设计预期行为（动态解锁），确认存档数据是否正确即可 |
| **P2** | 问题 F — ShebaP_TwinSplit 缺 _shape | 确认设计意图，必要时补上 |

---

## 六、关键发现

### 发现 1：代码修复与运行时行为脱节

所有 4 个 Bug 的代码修复在源码层面 **已正确完成**。截图中仍然存在问题，说明：
- **存档数据**可能未更新（旧存档的 `CoreLayerCols`/`PrismLayerCols` 为 0，被 clamp 到 1）
- **UICanvasBuilder** 创建的 4 个 cell 与 `Cols=1` 的数据不匹配
- **asset 文件**中部分部件的 `_shape` 字段缺失

### 发现 2：Shape 与 SlotSize 的关系不清晰

`_slotSize` 和 `_shape` 是两个独立字段。`SlotSize` 现在由 `ItemShapeHelper.GetCells().Count` 隐式定义（3 个 cell = SlotSize 3），但 `_slotSize` 字段可能仍被某些代码使用（如 `FreeSpace < newItem.SlotSize`）。如果 `_slotSize` 与 `GetCells().Count` 不一致，会导致空间判断错误。

**建议**: 统一 `SlotSize` 的来源，从 `ItemShapeHelper.GetCells(Shape).Count` 派生，废弃手动设置的 `_slotSize` 字段。

### 发现 3：测试时需要正确的存档环境

新游戏启动时 `Cols=1`（2 格），必须通过游戏进度解锁到 `Cols=2`（4 格）才能看到完整的 2×2 网格。当前没有 UI 或 Debug 命令来解锁 Track 列，导致测试困难。

**建议**: 添加 Debug 命令 `[RuntimeInitializeOnLoadMethod]` 或 Editor 按钮，一键将所有 Track 解锁到 `Cols=2` 或 `Cols=4`。
