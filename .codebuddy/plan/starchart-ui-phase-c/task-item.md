# 实施计划：StarChart UI Phase C — 交互动画与体验打磨

## 待办事项

- [ ] 1. 增强 `StatusBarView.cs`：淡入动画
   - `ShowMessage()` 中先将 alpha 设为 0，再用 PrimeTween 淡入（150ms，`Ease.OutQuad`）
   - 新消息打断旧动画时，立即停止所有 Tween，从 alpha 0 重新淡入
   - _需求：5.1、5.2、5.3_

- [ ] 2. 增强 `DragGhostView.cs`：Ghost 状态颜色 + 出现/消失动画
   - 新增 `SetDropState(DropPreviewState state)` 方法（`Valid / Replace / Invalid / None`）
   - 根据 state 更新 Ghost 边框颜色（绿/橙/红）
   - 新增 `_replaceHintLabel`（`TMP_Text`），`Replace` 状态时显示 `↺ REPLACE`
   - `Show()` 时播放 scale 0.8 → 1.0 弹出动画（80ms，`Ease.OutBack`）
   - `Hide()` 时播放 scale 1.0 → 0.0 收缩动画（80ms，`Ease.InQuad`），动画结束后 `SetActive(false)`
   - _需求：2.1、2.2、2.3、2.4、2.5_

- [ ] 3. 增强 `SlotCellView.cs`：装备/卸载动画
   - `SetItem()` 时播放背景闪烁（当前色 → 白色 → 当前色，150ms，`Ease.OutQuad`）
   - `SetEmpty()` 时播放背景淡出（当前色 → SlotEmpty，100ms，`Ease.OutQuad`）
   - _需求：3.1、3.2、3.3_

- [ ] 4. 增强 `InventoryItemView.cs`：选中边框淡入/淡出动画
   - `SetSelected(true)` 时边框 alpha 0 → 1 淡入（100ms）
   - `SetSelected(false)` 时边框 alpha 1 → 0 淡出（100ms）
   - 拖拽开始时 CanvasGroup alpha 1 → 0.4（80ms）
   - 拖拽结束时 CanvasGroup alpha 0.4 → 1.0（80ms）
   - _需求：4.3、4.4、4.5、4.6_

- [ ] 5. 新建 `FlyBackAnimator.cs`：部件飞回背包动画系统
   - 静态方法 `FlyTo(RectTransform from, RectTransform to, StarChartItemSO item, Action onComplete)`
   - 创建飞行克隆体（`Image`），从 `from` 世界坐标飞向 `to` 世界坐标
   - PrimeTween Sequence：位移（350ms，`Ease.InQuad`）+ 同步 alpha 1 → 0 + scale 1 → 0.6
   - 动画结束后销毁克隆体，调用 `onComplete`（触发落地弹跳）
   - 落地弹跳：目标格子 scale 1.0 → 1.12 → 1.0（100ms，`Ease.OutBounce`）
   - 维护 `_activeAnimations` 列表，`SkipAll()` 方法立即跳过所有进行中动画
   - _需求：1.1、1.2、1.3、1.4、1.5、1.6_

- [ ] 6. 将 `FlyBackAnimator` 接入 `DragDropManager.cs`
   - 强制替换后，对每个被顶出的部件调用 `FlyBackAnimator.FlyTo()`
   - 飞行期间隐藏背包中对应格子（`CanvasGroup.alpha = 0`），动画结束后恢复
   - 新拖拽开始时调用 `FlyBackAnimator.SkipAll()`
   - _需求：1.5、1.6_

- [ ] 7. 增强 `StarChartPanel.cs`：面板开关动画
   - `Open()` 中：先 `SetActive(true)`，再播放 scale 0.95 → 1.0 + alpha 0 → 1（200ms，`Ease.OutQuad`）
   - `Close()` 中：先播放 scale 1.0 → 0.95 + alpha 1 → 0（150ms，`Ease.InQuad`），动画结束后 `SetActive(false)`
   - 动画期间 `CanvasGroup.interactable = false`，结束后恢复
   - _需求：6.1、6.2、6.3_

- [ ] 8. 更新 `UICanvasBuilder.cs`：为 Ghost 新增 `_replaceHintLabel` 和边框 Image
   - Ghost 节点新增 `BorderImage`（`Image`，`Image.Type.Sliced`，初始颜色 `Color.clear`）
   - Ghost 节点新增 `ReplaceHintLabel`（`TMP_Text`，初始隐藏）
   - 自动连线到 `DragGhostView` 的新字段
   - _需求：2.2_
