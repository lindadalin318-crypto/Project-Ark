# 需求文档：StarChart UI Phase B — 数据模型视图层重构

## 引言

Phase A 完成了视觉样式对齐。Phase B 的目标是**重构视图层以支持原型中的 4 列类型布局**（SAIL / PRISM / CORE / SAT），并实现**强制替换**交互逻辑（拖拽时顶掉已有部件）。

**Phase B 的核心约束**：
- 不改变 `WeaponTrack` 的底层数据结构（仍保持 `CoreLayer` + `PrismLayer` 两层）
- 不改变 `SlotLayer<T>` 的 `GRID_SIZE = 3`
- SAIL 和 SAT 类型的槽位通过 `StarChartController` 的现有 API 管理（`EquipLightSail` / `EquipSatellite`）
- 强制替换逻辑在 `DragDropManager` 层实现，不修改 `SlotLayer` 的 `TryEquip`

**不在 Phase B 范围内**：
- 飞回背包动画（Phase C）
- 加特林切换动画（Phase C）
- 背包网格 Shape 系统（当前背包保持单格卡片）

---

## 需求

### 需求 1：TrackView 扩展为 4 列类型布局

**用户故事：** 作为玩家，我希望每个轨道区域显示 SAIL / PRISM / CORE / SAT 四列，以便清晰识别各类型槽位。

#### 验收标准

1. WHEN 轨道区域渲染 THEN 系统 SHALL 在每个 `TrackView` 内显示 4 列：SAIL（绿）/ PRISM（紫）/ CORE（蓝）/ SAT（黄），每列顶部有类型标签
2. WHEN SAIL 列渲染 THEN 系统 SHALL 显示 1 个槽位（`StarChartController.GetEquippedLightSail()` 的结果），空时显示 `+`
3. WHEN SAT 列渲染 THEN 系统 SHALL 显示最多 2 个槽位（`StarChartController.GetEquippedSatellites()` 的结果），空时显示 `+`
4. WHEN PRISM 列渲染 THEN 系统 SHALL 显示 3 个槽位（`WeaponTrack.PrismLayer`），与现有逻辑一致
5. WHEN CORE 列渲染 THEN 系统 SHALL 显示 3 个槽位（`WeaponTrack.CoreLayer`），与现有逻辑一致
6. WHEN 轨道区域渲染 THEN 系统 SHALL 4 列横向排列，每列宽度均等，列间有细分隔线

---

### 需求 2：SAIL / SAT 槽位的拖拽装备支持

**用户故事：** 作为玩家，我希望能将 SAIL 和 SAT 类型的部件拖拽到对应槽位进行装备。

#### 验收标准

1. WHEN 拖拽 `LightSailSO` 到 SAIL 槽位 THEN 系统 SHALL 调用 `StarChartController.EquipLightSail()` 完成装备
2. WHEN 拖拽 `SatelliteSO` 到 SAT 槽位 THEN 系统 SHALL 调用 `StarChartController.EquipSatellite()` 完成装备
3. WHEN 拖拽 SAIL/SAT 到错误类型槽位 THEN 系统 SHALL 显示红色无效高亮，拒绝放置
4. WHEN SAIL 槽位已有部件且拖入新 SAIL THEN 系统 SHALL 触发强制替换（需求 3）
5. WHEN SAT 槽位已满（2个）且拖入新 SAT THEN 系统 SHALL 触发强制替换（需求 3）

---

### 需求 3：强制替换交互逻辑

**用户故事：** 作为玩家，我希望将部件拖到已有部件的槽位时，能直接替换已有部件，而不是被拒绝。

#### 验收标准

1. WHEN 拖拽部件悬停在已有部件的槽位上 THEN 系统 SHALL 显示橙色替换高亮（`StarChartTheme.HighlightReplace`），而非红色无效高亮
2. WHEN 拖拽部件放置到已有部件的槽位上 THEN 系统 SHALL 先卸载已有部件，再装备新部件
3. WHEN 强制替换发生 THEN 系统 SHALL 在 StatusBar 显示 `REPLACED: [旧部件名] → [新部件名]`，橙色文字
4. WHEN 强制替换发生 THEN 系统 SHALL 刷新所有视图（TrackView + InventoryView）
5. WHEN 拖拽部件大小 > 目标槽位剩余空间 THEN 系统 SHALL 卸载所有阻碍放置的部件（可能替换多个），再装备新部件
6. WHEN 拖拽部件从背包拖到轨道 THEN 系统 SHALL 支持强制替换（不仅限于同类型替换）
7. WHEN 拖拽部件从轨道拖到另一轨道 THEN 系统 SHALL 支持强制替换

---

### 需求 4：拖拽悬停三色预览系统

**用户故事：** 作为玩家，我希望拖拽时能通过颜色预览判断放置结果，以便做出正确决策。

#### 验收标准

1. WHEN 拖拽部件悬停在空槽位上（类型匹配） THEN 系统 SHALL 显示绿色高亮（`StarChartTheme.HighlightValid`）
2. WHEN 拖拽部件悬停在已有部件的槽位上（类型匹配） THEN 系统 SHALL 显示橙色高亮（`StarChartTheme.HighlightReplace`）
3. WHEN 拖拽部件悬停在类型不匹配的槽位上 THEN 系统 SHALL 显示红色高亮（`StarChartTheme.HighlightInvalid`）
4. WHEN 拖拽结束或取消 THEN 系统 SHALL 清除所有高亮，恢复槽位原始状态
5. WHEN 拖拽部件悬停在对应类型的列上 THEN 系统 SHALL 对该列所有槽位显示候选高亮（低透明度边框发光）

---

### 需求 5：SlotCellView 扩展支持 SAIL/SAT 类型

**用户故事：** 作为开发者，我希望 `SlotCellView` 能处理所有 4 种类型的槽位，以便统一管理。

#### 验收标准

1. WHEN `SlotCellView` 初始化 THEN 系统 SHALL 支持 `StarChartItemType.LightSail` 和 `StarChartItemType.Satellite` 的类型匹配检测
2. WHEN 拖拽 `LightSailSO` 到 SAIL 槽位 THEN 系统 SHALL 正确识别类型匹配并显示绿色/橙色高亮
3. WHEN 拖拽 `SatelliteSO` 到 SAT 槽位 THEN 系统 SHALL 正确识别类型匹配并显示绿色/橙色高亮
4. WHEN `SlotCellView` 作为 SAIL/SAT 槽位 THEN 系统 SHALL 通过 `SlotType` 枚举区分槽位类型（Core / Prism / LightSail / Satellite）

---

### 需求 6：UICanvasBuilder 更新支持 4 列布局

**用户故事：** 作为开发者，我希望运行 `Build UI Canvas` 后能生成包含 4 列类型布局的完整 TrackView。

#### 验收标准

1. WHEN 运行 `Build UI Canvas` THEN 系统 SHALL 为每个 TrackView 生成 4 列（SAIL 1格 / PRISM 3格 / CORE 3格 / SAT 2格）
2. WHEN 运行 `Build UI Canvas` THEN 系统 SHALL 自动连线所有新增的 SAIL/SAT 槽位引用
3. WHEN 运行 `Build UI Canvas` THEN 系统 SHALL 保持幂等性（重复运行不创建重复节点）
