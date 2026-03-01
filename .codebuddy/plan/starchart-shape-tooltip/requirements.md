# 需求文档：星图异形部件拖拽系统 & Tooltip 悬停详情卡

## 引言

本文档描述两个功能的实现需求，目标是对齐 `StarChartUIPrototype.html` 原型的交互体验：

1. **异形部件拖拽系统（Shape-Aware Drag & Drop）**：在拖拽过程中，Ghost 元素根据部件的 `SlotSize` 动态调整形状，槽位显示实时预览高亮（绿色/橙色/红色），放置时支持强制替换并触发飞回动画。
2. **Tooltip 悬停详情卡**：鼠标悬停在库存物品或已装备部件上 150ms 后，在鼠标旁弹出浮动详情卡，包含属性列表、装备状态标签和操作提示。

### 架构约束

- Unity 侧 `SlotLayer` 是线性 1D 容量（3格），`SlotSize` 为 1-3 整数，**不引入 2D shape 坐标数组**，保持与现有战斗系统的兼容性。
- 形状感知仅在 **UI 层**实现：Ghost 尺寸、槽位预览高亮均基于 `SlotSize` 推导，不修改 `StarChartItemSO` 数据结构。
- 新代码使用 `UniTask` 替代 `Coroutine`，补间使用 `PrimeTween`，遵循 CLAUDE.md 异步纪律。
- `ItemTooltipView` 已存在但功能不完整，需重写/扩展而非新建。

---

## 需求

### 需求 1：Shape-Aware Ghost（形状感知拖拽幽灵）

**用户故事：** 作为玩家，我希望拖拽部件时 Ghost 能直观显示部件的占用大小，以便我能准确判断部件能否放入目标槽位。

#### 验收标准

1. WHEN 玩家开始拖拽一个 `SlotSize=1` 的部件 THEN Ghost SHALL 显示为 1×1 单格尺寸（与单个 SlotCellView 等宽等高）。
2. WHEN 玩家开始拖拽一个 `SlotSize=2` 的部件 THEN Ghost SHALL 显示为 2×1 竖向双格尺寸（高度为两个 SlotCellView 加间距）。
3. WHEN 玩家开始拖拽一个 `SlotSize=3` 的部件 THEN Ghost SHALL 显示为 3×1 竖向三格尺寸（高度为三个 SlotCellView 加间距）。
4. WHEN Ghost 尺寸变化时 THEN Ghost 内部 SHALL 渲染对应数量的半透明格子网格（active 格子青色半透明，与 SlotCellView 视觉风格一致）。
5. WHEN Ghost 显示时 THEN Ghost 的图标和名称标签 SHALL 居中显示在格子网格之上。

---

### 需求 2：拖拽预览高亮（Drop Preview Highlight）

**用户故事：** 作为玩家，我希望拖拽部件悬停在槽位上时能看到实时的放置预览，以便我能在松手前确认放置结果。

#### 验收标准

1. WHEN 拖拽中鼠标悬停在类型匹配且有足够空间的槽位列上 THEN 目标槽位 SHALL 显示绿色高亮（`preview-valid`），Ghost 边框变为绿色。
2. WHEN 拖拽中鼠标悬停在类型匹配但空间不足（会触发强制替换）的槽位列上 THEN 目标槽位 SHALL 显示橙色高亮（`preview-replace`），Ghost 边框变为橙色，Ghost 顶部显示 `↺ REPLACE N` 提示文字。
3. WHEN 拖拽中鼠标悬停在类型不匹配的槽位列上 THEN Ghost 边框 SHALL 变为红色（`preview-invalid`），不显示槽位高亮。
4. WHEN 拖拽开始时 THEN 所有类型匹配的 TypeColumn SHALL 显示呼吸脉冲高亮（`drop-candidate`），提示可放置区域。
5. WHEN 拖拽结束或取消时 THEN 所有预览高亮和候选列高亮 SHALL 立即清除。

---

### 需求 3：强制替换放置（Force Replace Drop）

**用户故事：** 作为玩家，我希望直接将新部件拖入已有部件的槽位，系统自动顶出旧部件，无需手动先卸载。

#### 验收标准

1. WHEN 玩家将部件放置到已有部件的槽位（类型匹配，空间不足）THEN 系统 SHALL 自动顶出旧部件并装入新部件。
2. WHEN 部件被顶出时 THEN 被顶出的部件 SHALL 触发飞回动画（从放置点飞向库存区域，带 landing bounce）。
3. WHEN 放置成功时 THEN 新部件的覆盖层 SHALL 播放 snap-in 弹入动画（scale 1.18 → 0.96 → 1.0）。
4. WHEN 放置成功时 THEN StatusBar SHALL 显示替换提示（`REPLACED: 旧部件 → 新部件`，橙色，3秒）。
5. IF 拖拽来源是已装备槽位 AND 目标是同一 TypeColumn THEN 系统 SHALL 视为无操作（no-op），不触发替换。

---

### 需求 4：Tooltip 悬停详情卡（完整重写）

**用户故事：** 作为玩家，我希望悬停在任意部件上时能看到完整的属性详情卡，以便我能在不打断操作流的情况下了解部件数据。

#### 验收标准

1. WHEN 鼠标悬停在库存物品或已装备部件覆盖层上超过 150ms THEN Tooltip SHALL 在鼠标右下方弹出（偏移 +14px, +14px）。
2. WHEN Tooltip 即将超出屏幕边界时 THEN Tooltip SHALL 自动翻转到鼠标左侧或上方，确保始终完整显示在屏幕内。
3. WHEN Tooltip 显示时 THEN Tooltip SHALL 包含：图标、名称、类型标签（带类型颜色）、属性列表（带 ▲/▼ 箭头和数值）、描述文字。
4. WHEN 显示的部件已装备时 THEN Tooltip SHALL 额外显示装备状态标签（`✓ EQUIPPED · PRIMARY/SECONDARY · CORE/PRISM/SAIL/SAT`）。
5. WHEN 显示的部件未装备时 THEN Tooltip SHALL 显示操作提示文字（`Drag to a slot to equip`）；已装备时显示（`Drag to move or drag to inventory to unequip`）。
6. WHEN 鼠标离开部件时 THEN Tooltip SHALL 立即隐藏（无延迟）。
7. WHEN 拖拽操作开始时 THEN Tooltip SHALL 立即隐藏，且在拖拽期间不显示。
8. WHEN 鼠标从一个部件快速移动到另一个部件时 THEN Tooltip SHALL 先隐藏再以 150ms 延迟重新显示新内容（不闪烁）。
9. WHEN Tooltip 显示/隐藏时 THEN SHALL 使用 PrimeTween 实现 0.08s 淡入淡出动画（替换现有 Coroutine 实现）。

---

### 需求 5：Tooltip 属性列表内容

**用户故事：** 作为玩家，我希望 Tooltip 的属性列表能准确反映各类型部件的关键数值，以便我能做出有效的配装决策。

#### 验收标准

1. WHEN 显示 `StarCoreSO` 时 THEN 属性列表 SHALL 包含：DAMAGE（▲）、FIRE RATE（▲）、SPEED（▲）、HEAT（▲/▼）。
2. WHEN 显示 `PrismSO` 时 THEN 属性列表 SHALL 包含：所有 `StatModifiers` 条目，Add 类型显示 `+/-数值`，Multiply 类型显示 `×数值`，正值 ▲ 负值 ▼。
3. WHEN 显示 `LightSailSO` 时 THEN 属性列表 SHALL 显示 EffectDescription 文字（无箭头）。
4. WHEN 显示 `SatelliteSO` 时 THEN 属性列表 SHALL 包含：TRIGGER、ACTION、COOLDOWN 三行。
5. WHEN 部件 HeatCost > 0 时 THEN 所有类型 SHALL 在属性列表末尾显示 HEAT ▲ 数值。

---

### 需求 6：Tooltip 与 StarChartPanel 集成

**用户故事：** 作为开发者，我希望 Tooltip 能被 StarChartPanel 统一管理，以便各 View 组件能方便地触发显示/隐藏。

#### 验收标准

1. WHEN `StarChartPanel` 初始化时 THEN SHALL 持有 `ItemTooltipView` 的引用，并通过 `ShowTooltip(item, isEquipped, equippedLocation)` / `HideTooltip()` 方法对外暴露。
2. WHEN `InventoryItemView` 的 `OnPointerEnter` 触发时 THEN SHALL 调用 `StarChartPanel.ShowTooltip()`。
3. WHEN `SlotCellView`（已装备状态）的 `OnPointerEnter` 触发时（非拖拽中）THEN SHALL 调用 `StarChartPanel.ShowTooltip()`，传入装备位置信息。
4. WHEN `UICanvasBuilder` 重建 Canvas 时 THEN SHALL 在 StarChartPanel 下创建 `ItemTooltipView` GameObject 并完成字段连线。
