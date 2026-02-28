# 需求文档：StarChart UI 冗余代码清理

## 引言

StarChart UI 经历了 Phase A/B/C 三轮迭代开发和一轮 Review 修复，积累了若干冗余代码：
注释掉的 Tooltip 系统残留、已废弃的旧版 Builder 存根、死代码事件、调试日志、以及遗留兼容属性。
本次清理目标是在**不改变任何运行时行为**的前提下，删除所有冗余内容，使代码库清晰、精简、整洁。

---

## 需求

### 需求 1：删除 StarChartPanel.cs 中的注释代码与调试日志

**用户故事：** 作为开发者，我希望 `StarChartPanel.cs` 中不存在注释掉的 Tooltip 代码块和调试日志，以便代码意图清晰、无噪音。

#### 验收标准

1. WHEN 查看 `StarChartPanel.cs` THEN 文件中 SHALL NOT 包含任何 `// [Header("Tooltip")]`、`_itemTooltipView`、`HandleCellPointerEntered`、`HandleCellPointerExited`、`HandleInventoryItemPointerEntered`、`HandleInventoryItemPointerExited` 的注释代码块
2. WHEN 查看 `StarChartPanel.cs` THEN 文件中 SHALL NOT 包含 `// TODO: 光帆/伴星专用槽位` 注释
3. WHEN 查看 `StarChartPanel.cs` THEN `RefreshAll()`、`HandleInventoryItemSelected()`、`HandleCellClicked()` 中 SHALL NOT 包含 `Debug.Log` 调用
4. WHEN 查看 `StarChartPanel.cs` THEN `Bind()` 方法中被注释掉的 `OnCellPointerEntered/Exited` 订阅行 SHALL 被完全删除（不保留注释行）

---

### 需求 2：删除 TrackView.cs 中的死代码事件

**用户故事：** 作为开发者，我希望 `TrackView.cs` 中不存在从未被外部消费的事件声明，以便减少 API 表面积和维护负担。

#### 验收标准

1. WHEN 查看 `TrackView.cs` THEN `OnCellPointerEntered` 和 `OnCellPointerExited` 事件声明 SHALL 被删除
2. WHEN 查看 `TrackView.cs` THEN `Awake()` 中所有 `OnCellPointerEntered?.Invoke` 和 `OnCellPointerExited?.Invoke` 的订阅转发行 SHALL 被删除
3. IF `OnCellPointerEntered/Exited` 被删除 THEN 编译 SHALL 无错误（确认无其他引用）

---

### 需求 3：删除 SlotCellView.cs 中的遗留兼容属性

**用户故事：** 作为开发者，我希望 `SlotCellView.cs` 中不存在标注为 "Legacy compatibility" 的 `IsCoreCell` 属性，以便消除混淆的双重 API。

#### 验收标准

1. WHEN 查看 `SlotCellView.cs` THEN `IsCoreCell` 属性 SHALL 被完全删除
2. IF `IsCoreCell` 被删除 THEN 编译 SHALL 无错误（确认无其他引用）

---

### 需求 4：删除 InventoryView.cs 中的死代码事件与调试日志

**用户故事：** 作为开发者，我希望 `InventoryView.cs` 中不存在从未被外部消费的 Pointer 事件和调试日志，以便代码简洁。

#### 验收标准

1. WHEN 查看 `InventoryView.cs` THEN `OnItemPointerEntered` 和 `OnItemPointerExited` 事件声明 SHALL 被删除
2. WHEN 查看 `InventoryView.cs` THEN `Refresh()` 中的 `view.OnPointerEntered` 和 `view.OnPointerExited` 订阅转发行 SHALL 被删除
3. WHEN 查看 `InventoryView.cs` THEN `Bind()` 和 `Refresh()` 中的所有 `Debug.Log` / `Debug.LogWarning` 调用 SHALL 被删除
4. IF 上述内容被删除 THEN 编译 SHALL 无错误

---

### 需求 5：删除废弃的 StarChart169Builder.cs

**用户故事：** 作为开发者，我希望项目中不存在只打印"请用新工具"日志的废弃 Editor 脚本，以便菜单栏整洁、无误导性入口。

#### 验收标准

1. WHEN 查看 `Assets/Scripts/UI/Editor/` 目录 THEN `StarChart169Builder.cs` 及其 `.meta` 文件 SHALL 被删除
2. WHEN Unity 编辑器菜单 THEN `ProjectArk/Build 16:9 Star Chart UI` 菜单项 SHALL 不再存在
3. IF 文件被删除 THEN 编译 SHALL 无错误

---

### 需求 6：清理 DragDropManager.cs 中的 Linq 依赖与冗余重置

**用户故事：** 作为开发者，我希望 `DragDropManager.cs` 中不使用 `System.Linq`，并且 `CleanUp()` 中不重置从未被外部读取的字段，以便减少不必要的依赖和代码噪音。

#### 验收标准

1. WHEN 查看 `DragDropManager.cs` THEN `ShowReplaceMessage()` 中的 `System.Linq.Enumerable.Select` 调用 SHALL 被替换为等效的手写循环或 `string.Join` + 手动构建
2. WHEN 查看 `DragDropManager.cs` THEN 文件顶部 SHALL NOT 包含 `using System.Linq` 或内联 `System.Linq.` 调用
3. WHEN 查看 `DragDropManager.cs` THEN `DropTargetIsCoreLayer` 字段 SHALL 保留（内部 `ExecuteDrop` 仍在使用），但确认其访问修饰符为 `public` 是合理的（供 SlotCellView 设置）
