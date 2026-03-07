# 实施计划：SAIL 列内自由移动修复

- [ ] 1. 修复 `SlotCellView.ComputeDropValidity()` 中 SAIL 的空间计算逻辑
   - 在 `HasSpaceForItem != null`（Legacy SAIL 分支）中，检测是否为 SAIL 列内拖动（`payload.Source == DragSource.Slot` 且 payload.Item 是 `LightSailSO` 且已在 SailLayer 中）
   - 若是列内拖动，调用 `sailLayer.FreeSpace + draggedSail.SlotSize > 0` 排除自身后判断空间，而非直接调用 `HasSpaceForItem(payload.Item)`
   - 同时处理 no-op 检测：若目标 anchor 与当前 anchor 相同，则 `isReplace = false`，`previewState = DropPreviewState.Valid`（或保持原位不变）
   - _需求：2.1、2.2、2.3_

- [ ] 2. 修复 `DragDropManager.ExecuteDrop()` 中 SAIL 列内移动的 Unequip-then-Equip 逻辑
   - 在 `DropTargetIsSailColumn` 分支中，检测当前 payload 是否来自 SAIL 列（`payload.Source == DragSource.Slot` 且 `payload.Item is LightSailSO`）
   - 若是 SAIL→SAIL 列内移动，先检查是否为 no-op（目标 anchor 与当前 anchor 相同），若是则直接 return 不做任何操作
   - 若不是 no-op，先调用 `_controller.UnequipLightSail(sail)` 从原格子移除，再调用 `EquipToTrack(item, null)` 放入新格子
   - 若来自 Inventory（非 SAIL 列），保持现有逻辑不变
   - _需求：1.1、1.2、3.1、3.2_

- [ ] 3. 验证并更新 `EquipToTrack()` 的 `LightSailSO` 分支（防御性检查）
   - 检查 `EquipToTrack` 中 `LightSailSO` 分支是否已有 Unequip 自身的逻辑；若没有，添加防御性 Unequip 调用，确保即使上层漏掉也不会产生重复
   - 确认驱逐目标格占用者（`sailOccupant != null && !ReferenceEquals(sailOccupant, sail)`）的逻辑仍然正确
   - _需求：1.1、1.4、3.2_

- [ ] 4. 追加实现日志到 `ImplementationLog.md`
   - 记录修改的文件：`SlotCellView.cs`、`DragDropManager.cs`（可能含 `EquipToTrack` 所在文件）
   - 记录 bug 根本原因、修复方案和验收结果
   - _需求：1.1、1.2、2.1、3.1_
