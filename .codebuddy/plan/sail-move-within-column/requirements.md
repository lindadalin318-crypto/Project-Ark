# 需求文档：SAIL 列内自由移动修复

## 引言

当前 SAIL（光帆）部件在共享 SAIL 列中存在拖拽移动 bug：将一个已装备的 SAIL 从一个格子拖动到同列的另一个格子时，会出现**两个重复的 SAIL 图标**。

**根本原因（已通过代码分析确认）：**

1. **`DragDropManager.ExecuteDrop()`**：当 `DropTargetIsSailColumn == true` 时，直接调用 `EquipToTrack(item, null)`，但没有先判断该 sail 是否已在 SAIL 列中（即 SAIL→SAIL 列内移动的情况），也没有先执行 `UnequipLightSail(sail)` 将其从原位置移除。

2. **`EquipToTrack()` 的 `LightSailSO` 分支**：直接调用 `_controller.EquipLightSail(sail, anchorCol, anchorRow)` 而不先 Unequip，导致同一个 sail 同时存在于两个格子。

3. **`ComputeDropValidity()` 的 SAIL 空间计算**：计算 `hasSpace` 时没有排除被拖动的 sail 自身占用的空间，导致从 SAIL 列内拖动时空间判断可能不准确（与之前 Track 内移动 bug 同类问题）。

---

## 需求

### 需求 1：SAIL 列内移动时先 Unequip 再 Equip

**用户故事：** 作为玩家，我希望将已装备的 SAIL 从一个格子拖动到同列的另一个格子时，SAIL 能正确移动而不出现重复，以便自由调整 SAIL 的位置。

#### 验收标准

1. WHEN 玩家将已装备的 SAIL 从 SAIL 列的格子 A 拖动到格子 B THEN 系统 SHALL 先将 SAIL 从格子 A 移除，再将其放入格子 B，最终只有一个 SAIL 显示在格子 B。
2. WHEN 玩家将已装备的 SAIL 拖动到其当前所在的同一格子（原位放下）THEN 系统 SHALL 执行 no-op，SAIL 保持在原位不发生任何变化。
3. WHEN 玩家将 SAIL 从 SAIL 列拖动到另一个有空位的格子 THEN 系统 SHALL 显示绿色高亮（Valid）表示可以放置。
4. WHEN 玩家将 SAIL 从 SAIL 列拖动到另一个已被其他 SAIL 占据的格子 THEN 系统 SHALL 显示橙色高亮（Replace）并在放下后驱逐原占据者。

---

### 需求 2：SAIL 空间计算排除自身

**用户故事：** 作为玩家，我希望拖动已装备的 SAIL 时，系统能正确判断目标格子是否可放置，以便获得准确的视觉反馈（绿色/橙色/红色高亮）。

#### 验收标准

1. WHEN 玩家拖动一个已在 SAIL 列中的 SAIL 悬停到 SAIL 列的空格子上 THEN 系统 SHALL 在空间计算中排除该 SAIL 自身占用的空间，正确显示绿色高亮（Valid）。
2. WHEN 玩家拖动一个已在 SAIL 列中的 SAIL 悬停到 SAIL 列的另一个已占用格子上 THEN 系统 SHALL 正确显示橙色高亮（Replace）。
3. IF SAIL 列只有一个格子且该格子已被当前拖动的 SAIL 占据 THEN 系统 SHALL 在悬停到该格子时显示 no-op 状态（不触发 Replace 也不触发 Valid 的重复放置）。

---

### 需求 3：保持现有跨来源行为不变

**用户故事：** 作为玩家，我希望从背包（Inventory）拖动 SAIL 到 SAIL 列的行为保持不变，以便现有功能不受影响。

#### 验收标准

1. WHEN 玩家从背包拖动 SAIL 到 SAIL 列的空格子 THEN 系统 SHALL 正常装备该 SAIL，行为与修复前一致。
2. WHEN 玩家从背包拖动 SAIL 到 SAIL 列的已占用格子 THEN 系统 SHALL 驱逐原占据者并装备新 SAIL，行为与修复前一致。
3. WHEN 玩家将已装备的 SAIL 拖动到背包区域 THEN 系统 SHALL 正常卸装该 SAIL，行为与修复前一致。
