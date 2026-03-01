# 实施计划：星图异形部件拖拽系统 & Tooltip 悬停详情卡

---

- [ ] 1. 扩展 DragGhostView：Shape-Aware Ghost 渲染
   - 修改 `DragGhostView.cs`，新增 `SetShape(int slotSize)` 方法
   - 根据 `slotSize` 动态调整 Ghost RectTransform 高度（1/2/3 格 × 单格高度 + 间距）
   - 在 Ghost 内部生成对应数量的半透明格子网格 Image（青色，与 SlotCellView 风格一致）
   - 图标和名称标签叠加在格子网格之上居中显示
   - _需求：1.1、1.2、1.3、1.4、1.5_

- [ ] 2. 扩展 DragDropManager：拖拽开始时传入 SlotSize
   - 修改 `DragDropManager.BeginDrag()` 调用链，将 `StarChartItemSO.SlotSize` 传给 `DragGhostView.SetShape()`
   - 拖拽开始时对所有类型匹配的 `TypeColumnView` 触发 `SetDropCandidate(true)` 呼吸脉冲高亮
   - 拖拽结束/取消时调用 `SetDropCandidate(false)` 清除所有候选列高亮
   - _需求：2.4、2.5_

- [ ] 3. 实现拖拽预览高亮（Drop Preview Highlight）
   - 在 `TypeColumnView` 中新增 `SetDropPreview(DropPreviewState state)` 方法，支持 Valid / Replace / Invalid / None 四种状态，对应绿/橙/红/无高亮
   - 在 `DragDropManager.OnDragHover()` 中判断目标列类型匹配性和空间充足性，调用对应预览状态
   - 更新 Ghost 边框颜色与预览状态同步；Replace 状态时在 Ghost 顶部显示 `↺ REPLACE N` 文字
   - _需求：2.1、2.2、2.3_

- [ ] 4. 实现强制替换放置逻辑（Force Replace Drop）
   - 修改 `DragDropManager.ExecuteDrop()`：当目标槽位类型匹配但空间不足时，执行强制替换流程
   - 顶出旧部件：调用 `WeaponTrack.Unequip()`，触发旧部件从放置点飞回库存区域的 PrimeTween 动画（带 landing bounce）
   - 装入新部件：调用 `WeaponTrack.Equip()`，新部件覆盖层播放 snap-in 弹入动画（scale 1.18 → 0.96 → 1.0）
   - 同一 TypeColumn 内槽位间拖拽视为 no-op，不触发替换
   - 替换成功后 StatusBar 显示 `REPLACED: 旧部件 → 新部件`（橙色，3秒）
   - _需求：3.1、3.2、3.3、3.4、3.5_

- [ ] 5. 重写 ItemTooltipView：布局与动画
   - 重写 `ItemTooltipView.cs`，移除现有 Coroutine，改用 `UniTask` + `PrimeTween`
   - 实现 `ShowTooltip(StarChartItemSO item, bool isEquipped, string equippedLocation)` 和 `HideTooltip()` 方法
   - 150ms 延迟显示（`UniTask.Delay`），0.08s PrimeTween 淡入淡出（CanvasGroup.alpha）
   - 实现屏幕边界检测：Tooltip 超出右/下边界时自动翻转到鼠标左侧/上方
   - 拖拽期间屏蔽显示（订阅 DragDropManager 拖拽开始/结束事件）
   - _需求：4.1、4.2、4.6、4.7、4.8、4.9_

- [ ] 6. 实现 Tooltip 内容填充：各类型属性列表
   - 新增 `TooltipContentBuilder` 静态工具类，根据 `StarChartItemSO` 子类型生成属性行列表
   - `StarCoreSO`：DAMAGE / FIRE RATE / SPEED / HEAT 四行（▲/▼ 箭头 + 数值）
   - `PrismSO`：遍历 `StatModifiers`，Add 显示 `+/-值`，Multiply 显示 `×值`，正值 ▲ 负值 ▼
   - `LightSailSO`：显示 `EffectDescription` 文字行（无箭头）
   - `SatelliteSO`：TRIGGER / ACTION / COOLDOWN 三行
   - 所有类型：`HeatCost > 0` 时末尾追加 HEAT ▲ 行
   - 已装备时显示 `✓ EQUIPPED · PRIMARY/SECONDARY · 类型` 状态标签；未装备时显示操作提示文字
   - _需求：4.3、4.4、4.5、5.1、5.2、5.3、5.4、5.5_

- [ ] 7. 集成 Tooltip 到 StarChartPanel、InventoryItemView、SlotCellView
   - `StarChartPanel` 持有 `ItemTooltipView` 引用，暴露 `ShowTooltip()` / `HideTooltip()` 公共方法
   - `InventoryItemView` 实现 `IPointerEnterHandler` / `IPointerExitHandler`，调用 `StarChartPanel.ShowTooltip()` / `HideTooltip()`
   - `SlotCellView`（已装备状态）实现同上，传入装备位置信息（PRIMARY/SECONDARY + 类型）
   - _需求：6.1、6.2、6.3_

- [ ] 8. UICanvasBuilder 连线 & 实现日志
   - 修改 `UICanvasBuilder`，在 StarChartPanel 下创建 `ItemTooltipView` GameObject 并完成所有 SerializeField 字段连线
   - 追加 `ImplementationLog.md`，记录本次所有新建/修改文件路径、技术方案和验收结果
   - _需求：6.4_
