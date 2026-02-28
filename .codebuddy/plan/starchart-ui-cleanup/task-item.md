# 实施计划：StarChart UI 冗余代码清理

- [ ] 1. 清理 `StarChartPanel.cs`
   - 删除所有注释掉的 Tooltip 代码块（`_itemTooltipView` 字段声明、`HandleCellPointerEntered/Exited`、`HandleInventoryItemPointerEntered/Exited` 方法）
   - 删除 `Bind()` 中被注释掉的 `OnCellPointerEntered/Exited` 订阅行
   - 删除 `// TODO: 光帆/伴星专用槽位` 注释
   - 删除 `RefreshAll()`、`HandleInventoryItemSelected()`、`HandleCellClicked()` 中的所有 `Debug.Log` 调用
   - _需求：1.1、1.2、1.3、1.4_

- [ ] 2. 清理 `TrackView.cs` 死代码事件
   - 删除 `OnCellPointerEntered` 和 `OnCellPointerExited` 事件声明
   - 删除 `Awake()` 中对应的订阅转发逻辑
   - 确认无其他文件引用这两个事件（grep 验证）
   - _需求：2.1、2.2、2.3_

- [ ] 3. 清理 `SlotCellView.cs` 遗留兼容属性
   - 删除标注为 "Legacy compatibility" 的 `IsCoreCell` 属性
   - 确认无其他文件引用 `IsCoreCell`（grep 验证）
   - _需求：3.1、3.2_

- [ ] 4. 清理 `InventoryView.cs` 死代码事件与调试日志
   - 删除 `OnItemPointerEntered` 和 `OnItemPointerExited` 事件声明
   - 删除 `Refresh()` 中 `view.OnPointerEntered` 和 `view.OnPointerExited` 的订阅转发行
   - 删除 `Bind()` 和 `Refresh()` 中所有 `Debug.Log` / `Debug.LogWarning` 调用
   - _需求：4.1、4.2、4.3、4.4_

- [ ] 5. 删除废弃的 `StarChart169Builder.cs`
   - 删除 `Assets/Scripts/UI/Editor/StarChart169Builder.cs` 文件
   - 同步删除对应的 `.meta` 文件（如存在）
   - _需求：5.1、5.2、5.3_

- [ ] 6. 清理 `DragDropManager.cs` 中的 Linq 依赖
   - 将 `ShowReplaceMessage()` 中的 `System.Linq` 调用替换为手写循环构建字符串
   - 确认文件顶部无 `using System.Linq` 残留
   - _需求：6.1、6.2_

- [ ] 7. 追加实现日志到 `ImplementationLog.md`
   - 记录本次清理涉及的所有修改文件、删除文件及清理内容摘要
   - _需求：全部_
