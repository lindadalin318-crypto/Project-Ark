# 需求文档：星图拖拽装备 (Star Chart Drag & Drop)

## 引言

当前星图面板的装备流程为：点击库存物品 → 查看详情面板 → 点击 EQUIP 按钮，共需三步操作。本功能旨在增加**拖拽交互**，允许玩家直接从库存区拖拽物品到轨道（Track）的对应槽位上完成装备，提供更直觉、高效的操作体验。

原有的"点击→详情→装备"流程将继续保留，拖拽为**补充交互方式**，而非替代。

### 现有架构概要

| 组件 | 职责 |
|------|------|
| `InventoryItemView` | 库存卡片，持有 `StarChartItemSO` 引用，当前仅支持点击 |
| `InventoryView` | 库存容器，实例化卡片，支持类型过滤 |
| `SlotCellView` | 轨道内单个格子，支持 SetItem/SetEmpty/SetHighlight 等状态 |
| `TrackView` | 单轨道视图（3 棱镜格 + 3 星核格），订阅 `WeaponTrack.OnLoadoutChanged` |
| `StarChartPanel` | 顶层编排器，管理装备/卸载逻辑 |
| `SlotLayer<T>` | 数据层，3 格容量，连续空间查找 + SlotSize 1-3 |
| `WeaponTrack` | 纯 C# 类，持有 CoreLayer + PrismLayer，提供 EquipCore/EquipPrism API |

### 类型约束

- **星核 (StarCoreSO)** → 只能放入轨道的 **Core 层**（下层）
- **棱镜 (PrismSO)** → 只能放入轨道的 **Prism 层**（上层）
- **光帆 (LightSailSO) / 伴星 (SatelliteSO)** → 专用槽位（当前 TODO，本次暂不涉及拖拽）

---

## 需求

### 需求 1：从库存拖拽物品

**用户故事：** 作为一名玩家，我希望能够从库存区拖起一个星图部件，以便直观地将其拖放到轨道上完成装备。

#### 验收标准

1. WHEN 玩家在库存区的 `InventoryItemView` 上**按下并拖动**鼠标 THEN 系统 SHALL 创建一个半透明的**拖拽幽灵 (Drag Ghost)**，跟随鼠标指针移动。
2. WHEN 拖拽开始 THEN 原始库存卡片 SHALL 保持在原位，仅降低不透明度（如 alpha=0.4）以示"正在拖拽"。
3. WHEN 拖拽进行中 THEN 拖拽幽灵 SHALL 显示被拖拽物品的图标（或类型色块占位符，与库存卡片一致）。
4. WHEN 拖拽过程中 THEN 拖拽幽灵 SHALL 始终渲染在所有面板 UI 元素之上（最高 SortingOrder）。
5. WHEN 玩家松开鼠标且拖拽幽灵**不在**任何有效目标上方 THEN 系统 SHALL 取消拖拽，恢复原始卡片不透明度，销毁幽灵。

---

### 需求 2：轨道槽位作为拖放目标

**用户故事：** 作为一名玩家，我希望将拖拽中的部件放到轨道槽位上时，系统能自动判断是否可装备并完成装备操作，以减少操作步骤。

#### 验收标准

1. WHEN 拖拽物品悬停在 `SlotCellView` 上方 AND 该槽位属于匹配类型的层（StarCoreSO → Core 层, PrismSO → Prism 层） AND 该层有足够的连续空间容纳物品的 SlotSize THEN 系统 SHALL 将**整段目标格子**高亮为**绿色（有效）**。
2. WHEN 拖拽物品悬停在 `SlotCellView` 上方 AND 类型不匹配或空间不足 THEN 系统 SHALL 将目标格子高亮为**红色（无效）**。
3. WHEN 玩家在**有效高亮**的格子上松开鼠标 THEN 系统 SHALL 调用 `WeaponTrack.EquipCore/EquipPrism` 完成装备，并触发 `RefreshAll()` 刷新所有视图。
4. WHEN 玩家在**无效高亮**的格子上松开鼠标 THEN 系统 SHALL 取消拖拽，不执行任何装备操作。
5. WHEN 拖拽物品离开某个 `SlotCellView` 区域 THEN 系统 SHALL 清除该格子的高亮状态，恢复原始视觉。
6. IF 被拖拽物品的 SlotSize > 1 THEN 系统 SHALL 高亮从悬停格子开始的连续 SlotSize 个格子（预览占位效果）。

---

### 需求 3：自动选中目标轨道

**用户故事：** 作为一名玩家，我希望拖拽物品到某个轨道时系统能自动选中该轨道，以避免我还需要先手动点击选中轨道。

#### 验收标准

1. WHEN 拖拽物品悬停进入某个 `TrackView` 的区域 THEN 系统 SHALL 自动将该轨道设为当前选中轨道（`_selectedTrack`）。
2. WHEN 轨道自动切换后 THEN 系统 SHALL 更新两个轨道的选中边框高亮。

---

### 需求 4：拖拽后的状态同步

**用户故事：** 作为一名玩家，我希望拖拽装备成功后所有面板都同步更新，以获得一致的视觉反馈。

#### 验收标准

1. WHEN 拖拽装备成功完成 THEN 系统 SHALL 刷新轨道视图（TrackView）、库存视图（InventoryView 的装备标记）、详情面板（如果当前选中物品就是刚装备的物品）。
2. WHEN 拖拽装备成功完成 AND 被装备的是 StarCoreSO THEN 系统 SHALL 调用 `WeaponTrack.InitializePools()` 预热对象池。
3. WHEN 拖拽装备成功 THEN 系统 SHALL 在详情面板中显示刚装备的物品信息（自动选中该物品）。

---

### 需求 5：timeScale=0 兼容性

**用户故事：** 作为一名玩家，我希望在星图面板打开时（游戏暂停状态）拖拽功能仍然正常工作。

#### 验收标准

1. IF 游戏处于 `Time.timeScale == 0` 状态 THEN 拖拽交互的所有环节（开始、移动、悬停高亮、放置、取消）SHALL 正常运行。
2. 拖拽过程中所有动画或移动 SHALL 使用 `Time.unscaledDeltaTime` 或不依赖 deltaTime 的方式。

---

### 需求 6：已装备物品从槽位拖出卸载（扩展）

**用户故事：** 作为一名玩家，我希望能从轨道槽位拖出已装备的物品到库存区域完成卸载，形成完整的拖拽交互闭环。

#### 验收标准

1. WHEN 玩家在轨道的 `SlotCellView`（已有物品）上按下并拖动 THEN 系统 SHALL 以相同的拖拽幽灵机制开始拖出操作。
2. WHEN 拖拽物品从槽位拖到库存区域并松开 THEN 系统 SHALL 调用 `WeaponTrack.UnequipCore/UnequipPrism` 完成卸载并刷新所有视图。
3. WHEN 拖拽物品从槽位拖出但松开位置既不在库存区也不在其他有效槽位 THEN 系统 SHALL 取消操作，物品保留在原槽位。
4. WHEN 拖拽物品从一个轨道的槽位拖到**另一个轨道**的对应类型槽位 THEN 系统 SHALL 从原轨道卸载并在目标轨道装备（跨轨道移动）。

---

## 边界情况与约束

| 场景 | 预期行为 |
|------|----------|
| 拖拽 LightSailSO 或 SatelliteSO | 当前不支持拖拽（没有对应的拖放目标槽位），拖拽不启动或立即取消 |
| 拖拽 SlotSize=3 的部件到仅剩 1 格空间的层 | 红色高亮，松开后不装备 |
| 拖拽已装备的物品 | 按需求 6 处理，支持拖出卸载 |
| 快速连续拖放 | 每次只允许一个拖拽操作进行中 |
| 拖拽过程中面板被关闭（ESC/Tab） | 立即取消当前拖拽，清理幽灵 |
