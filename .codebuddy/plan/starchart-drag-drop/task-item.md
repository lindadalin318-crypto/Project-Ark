# 实施计划：星图 UI 拖拽装备系统

- [ ] 1. 移除旧的点选交互逻辑
   - 删除 `selectedInvItem` 状态变量及所有引用
   - 移除库存物品的 `click` 事件监听（装备逻辑）
   - 移除轨道槽位的 `click` 事件监听（放置逻辑）
   - 移除 `equipItem()`、`unequipSlot()` 等点选触发函数
   - _需求：1、2、3（替换前置）_

- [ ] 2. 建立拖拽状态管理器
   - 新增 `dragState` 对象，包含字段：`isDragging`、`sourceType`（`inventory` / `slot`）、`item`、`sourceSlotId`、`sourceLoadout`
   - 新增 `startDrag()`、`endDrag()`、`cancelDrag()` 三个核心函数管理状态生命周期
   - 在 `endDrag` / `cancelDrag` 中统一清理所有高亮、ghost、cursor 状态
   - _需求：5.2、5.5_

- [ ] 3. 实现 Ghost 拖拽元素
   - 在 `<body>` 末尾创建 `#drag-ghost` 元素，默认 `display:none`，`position:fixed`，`pointer-events:none`，`z-index:9999`
   - 添加对应 CSS：半透明背景、物品图标、类型色边框、`grabbing` 光标
   - 在 `mousemove` 事件中更新 ghost 位置（鼠标坐标 + 偏移 12px）
   - _需求：4.1、4.4、4.5、4.6_

- [ ] 4. 实现库存物品拖拽发起
   - 在 `renderInventory()` 中为每个物品元素绑定 `mousedown` 事件
   - `mousedown` 时记录起始坐标，`mousemove` 超过 4px 阈值后正式触发 `startDrag()`
   - 拖拽进行中将库存原物品设为半透明占位（`opacity: 0.35`）
   - _需求：1.1、1.7_

- [ ] 5. 实现轨道槽位拖拽发起
   - 在 `renderSlot()` 中为已装备槽位绑定 `mousedown` 事件
   - `startDrag()` 时将槽位立即显示为空槽（预清空视觉），记录原始数据用于取消回滚
   - _需求：2.1_

- [ ] 6. 实现槽位悬停高亮反馈
   - 在所有槽位元素上绑定 `mouseenter` / `mouseleave` 事件
   - 拖拽进行中：类型匹配 → 绿色边框发光（`.slot-drop-valid`）；类型不匹配 → 红色边框（`.slot-drop-invalid`）；悬停离开 → 清除高亮
   - 同步更新 ghost 元素边框颜色（绿/红）
   - _需求：1.2、1.3、4.2、4.3_

- [ ] 7. 实现 mouseup 放置逻辑（三种情形）
   - 在 `window` 上监听 `mouseup`，用 `document.elementFromPoint()` 检测释放目标
   - **情形 A（库存→空槽）**：装备物品，从库存移除，播放嵌入动画（`scale 1.15→1.0`）
   - **情形 B（库存→占用槽 / 槽→槽交换）**：原槽物品退回库存，新物品装入槽位
   - **情形 C（槽→库存区域）**：槽位清空，物品加回库存
   - **情形 D（无效区域）**：调用 `cancelDrag()` 回滚所有状态
   - _需求：1.4、1.5、2.2、2.3、3.1、3.2、3.3、3.4_

- [ ] 8. 实现 Edge Cases 防护
   - 监听 `keydown` 事件，`Escape` 键触发 `cancelDrag()`
   - 在 `startDrag()` 入口检查 `isAnimating`，若为 `true` 则直接 `return`
   - 在 `window` 的 `blur` 事件中触发 `cancelDrag()`，防止鼠标移出窗口丢失 mouseup
   - _需求：5.1、5.2、5.5_

- [ ] 9. 更新库存渲染逻辑
   - 修改 `renderInventory()`：过滤掉当前 Loadout 所有槽位中已装备的物品，只渲染未装备物品
   - 当过滤后库存为空时，渲染 `"ALL ITEMS EQUIPPED"` 占位文字
   - 确保过滤器（SAIL/PRISM/CORE/SAT）与新逻辑兼容
   - _需求：6.1、6.2、6.3、6.4_
