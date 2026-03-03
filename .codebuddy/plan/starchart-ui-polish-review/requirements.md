# 需求文档：StarChart UI 完善模块 Code Review

## 引言

本次 Code Review 针对 StarChart UI 完善工作（对齐 HTML 原型，差距项 1/2/3/4/7/8/9）的全部实现代码进行系统性审查。审查范围包括新建文件 `ItemShapeHelper.cs` 和修改文件 `SlotLayer.cs`、`LoadoutSwitcher.cs`、`DragGhostView.cs`、`StatusBarView.cs`、`StarChartEnums.cs`、`StarChartItemSO.cs`、`TrackView.cs`、`DragDropManager.cs`、`SlotCellView.cs`、`InventoryItemView.cs`。

Review 分两阶段进行：**Stage 1 规格合规性**（实现是否符合需求文档）→ **Stage 2 代码质量**（实现是否做得正确）。

---

## 需求

### 需求 1：规格合规性审查（Stage 1）

**用户故事：** 作为项目负责人，我希望确认所有需求文档中的验收标准都已被正确实现，以便在进入代码质量审查前排除功能缺失或误解。

#### 验收标准

1. WHEN 审查需求1（2D Shape系统）THEN 审查者 SHALL 逐条验证 7 条验收标准是否在代码中有对应实现。
2. WHEN 审查需求2（Loadout管理UI）THEN 审查者 SHALL 验证 RENAME/DELETE/SAVE CONFIG 三个按钮逻辑及边界保护是否完整。
3. WHEN 审查需求3（Drum Counter翻牌动画）THEN 审查者 SHALL 验证 PrimeTween 驱动、方向感知、≤300ms 时长是否满足。
4. WHEN 审查需求4（Scale纵深感）THEN 审查者 SHALL 验证 0.88→1.0 Scale 与位移并行、无累积误差是否满足。
5. WHEN 审查需求7（三态高亮）THEN 审查者 SHALL 验证绿/橙/红三色、Ghost边框PrimeTween过渡≤80ms、`↺ 替换 N 个部件`文字是否满足。
6. WHEN 审查需求8（过滤器顺序）THEN 审查者 SHALL 验证 `InventoryFilter` 枚举是否作为单一数据源被正确使用。
7. WHEN 审查需求9（状态栏文字）THEN 审查者 SHALL 验证完整提示文字、`ShowPersistent`/`RestoreDefault` 动态切换是否满足。

---

### 需求 2：代码质量审查（Stage 2）

**用户故事：** 作为项目负责人，我希望确认实现代码符合 CLAUDE.md 架构原则和代码规范，以便维持项目整体代码质量标准。

#### 验收标准

1. WHEN 审查 `ItemShapeHelper.cs` THEN 审查者 SHALL 验证零GC设计、边界情况处理、命名规范是否符合项目标准。
2. WHEN 审查 `SlotLayer<T>.cs` THEN 审查者 SHALL 验证2D矩阵API正确性、`CanPlace` 返回值语义、`TryEquip` 向后兼容性、潜在的并发/重入问题。
3. WHEN 审查 `LoadoutSwitcher.cs` THEN 审查者 SHALL 验证事件卫生（OnDestroy取消订阅）、`OnDeleteClicked` 数组越界风险、`ConfirmRename` 双触发问题、`_drumBack` CanvasGroup 初始化时机。
4. WHEN 审查 `DragGhostView.cs` THEN 审查者 SHALL 验证 `_replaceHintLabel.gameObject.SetActive` 违反CLAUDE.md第11条、`RebuildShapeGrid` 锚点设置正确性。
5. WHEN 审查 `StatusBarView.cs` THEN 审查者 SHALL 验证 `_idleText` 格式占位符 `{0}/{1}` 是否被正确填充、`_isPersistent` 字段是否被 `ShowMessage` 正确处理。
6. WHEN 审查 `TrackView.cs` THEN 审查者 SHALL 验证 `SetShapeHighlight` 中 `SlotLayer<StarChartItemSO>.GRID_COLS` 硬编码类型参数的正确性。
7. IF 发现 Critical 或 Major 问题 THEN 审查者 SHALL 提供具体的修复代码示例。
