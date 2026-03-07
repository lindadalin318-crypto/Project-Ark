# 需求文档：Track 内部 Slot 自由移动

## 引言

当前星图面板中，已装备在 Track 上的部件（Core / Prism / SAT / SAIL）无法通过拖拽移动到同一 Track 的其他空闲 Slot 中。根本原因有两处：

1. `DragDropManager.ExecuteDrop()` 中对 `sourceTrack == DropTargetTrack` 的情况直接 no-op（什么都不做）。
2. `SlotCellView.ComputeDropValidity()` 在计算 `hasSpace` 时，没有将"被拖动的部件自身"从占用空间中排除，导致同 Track 内移动时 `hasSpace = false`，进而 `isReplace = true`（错误地认为需要替换），而非 `valid = true`。

本需求旨在修复上述两处 Bug，使 Track 上的部件可以自由拖拽到同一 Track 内位置允许的任意其他 Slot。

---

## 需求

### 需求 1：同 Track 内部 Slot 移动

**用户故事：** 作为一名玩家，我希望可以将已装备在 Track 上的部件拖拽到同一 Track 的其他空闲 Slot，以便自由调整部件的位置布局。

#### 验收标准

1. WHEN 玩家从 Track 上的某个 Slot 拖拽一个部件，并悬停到同一 Track 的另一个空闲 Slot 时，THEN 系统 SHALL 显示绿色 Valid 高亮（而非红色 Invalid 或橙色 Replace）。
2. WHEN 玩家将部件放置到同一 Track 的另一个合法 Slot 时，THEN 系统 SHALL 先从原位置卸载该部件，再在新位置装备该部件。
3. WHEN 玩家将部件放置到同一 Track 的另一个已被其他部件占用的 Slot 时，THEN 系统 SHALL 执行强制替换（evict 原占用部件，飞回库存），与跨 Track 替换行为一致。
4. WHEN 玩家将部件拖拽到同一 Track 的原始 Slot（即放回原位）时，THEN 系统 SHALL 执行 no-op（不改变任何状态），或等效地先卸载再装回原位（视觉上无变化）。
5. WHEN 玩家从 Track 上拖拽部件时，THEN `ComputeDropValidity` SHALL 将被拖动部件自身从 `hasSpace` 计算中排除（即临时假设该部件已被卸载，再计算剩余空间）。

### 需求 2：跨 Track 移动保持正确行为

**用户故事：** 作为一名玩家，我希望将部件从一条 Track 拖到另一条 Track 时，行为与现有逻辑保持一致，不受本次修复影响。

#### 验收标准

1. WHEN 玩家将部件从 Primary Track 拖到 Secondary Track 的空闲 Slot 时，THEN 系统 SHALL 先从 Primary Track 卸载，再装备到 Secondary Track（现有行为不变）。
2. WHEN 玩家将部件从 Primary Track 拖到 Secondary Track 的已占用 Slot 时，THEN 系统 SHALL 执行强制替换（现有行为不变）。

### 需求 3：SAIL 同列内移动

**用户故事：** 作为一名玩家，我希望已装备的 SAIL 部件也可以在 SAIL 列内的不同 Slot 之间自由移动（当 SAIL 列有多个 Slot 时）。

#### 验收标准

1. WHEN SAIL 列有多个 Slot 且玩家拖拽已装备的 SAIL 到同列另一个空闲 Slot 时，THEN 系统 SHALL 先卸载原位置，再装备到新位置。
2. WHEN `ComputeDropValidity` 处理 SAIL 类型时，SHALL 同样将被拖动的 SAIL 自身从 `hasSpace` 计算中排除。
