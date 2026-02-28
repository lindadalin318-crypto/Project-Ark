# StarChart UI Review — 修复任务清单

## 实施计划

- [ ] 1. 修复 `UICanvasBuilder.cs` 编译错误（P0）
   - 定位 `BuildTrackView()` 中引用 `labelTmp` 的代码行
   - 将 `labelTmp` 替换为正确的局部变量名（新 4 列布局中对应的 `TextMeshProUGUI` 变量）
   - 验证编译通过，`Build UI Canvas` 菜单项可正常执行
   - _需求：Issue 4_

- [ ] 2. 修复 `FlyBackAnimator.SkipAll()` 集合并发修改崩溃（P0）
   - 在 `SkipAll()` 中，先将 `_activeAnimations` 拷贝到临时列表，再调用 `Complete()`
   - 或改为先 `_activeAnimations.Clear()`，再对临时列表逐一调用 `Stop()`（不触发 OnComplete）
   - 验证：在飞行动画进行中开始新拖拽，不抛出 `InvalidOperationException`
   - _需求：Bug 4（FlyBackAnimator）_

- [ ] 3. 修复 `InventoryItemView` 所有 PrimeTween 动画缺少 `useUnscaledTime: true`（P1）
   - 检查 `OnPointerEnter`、`OnPointerExit`、`OnBeginDrag`、`OnEndDrag` 中所有 `Tween.*` 调用
   - 为每个调用补充 `useUnscaledTime: true` 参数
   - 验证：`Time.timeScale = 0` 时打开星图，hover 放大/缩小动画正常播放
   - _需求：Bug 5_

- [ ] 4. 修复 `DragDropManager.CleanUp()` 直接赋值 alpha 覆盖 PrimeTween 动画（P1）
   - 将 `CleanUp()` 中 `_sourceCanvasGroup.alpha = 1f` 改为条件判断：仅当 source 不是 `InventoryItemView`（即不会触发 `OnEndDrag` 动画）时才直接赋值
   - 或改为：`CleanUp()` 不直接设置 alpha，改由 `InventoryItemView.OnEndDrag` 统一负责恢复
   - 验证：拖拽结束后背包格子 alpha 渐变恢复（而非跳变）
   - _需求：Issue 6_

- [ ] 5. 修复 Slot → 空白区域拖拽静默失败（P1）
   - 在 `DragDropManager.EndDrag()` 中，当 `success == true` 但 `DropTargetValid == false` 时，补充调用 `StatusBar.ShowMessage("Cannot place here")` 或类似提示
   - 或在 `SlotCellView.OnEndDrag` 中检测拖拽未落点的情况并给出反馈
   - 验证：从 Slot 拖到空白区域后，StatusBar 显示提示信息，部件回到原位
   - _需求：Bug 1_

- [ ] 6. 修复 `SlotCellView.OnPointerExit()` 导致 Ghost 边框闪烁（P2）
   - 在 `OnPointerExit` 中，不立即清除 `DropTargetTrack`，改为用 `LateUpdate` 或单帧延迟检查：若下一帧 `DropTargetTrack` 已被新的 `OnPointerEnter` 重新设置，则不清除
   - 或改为：`DropTargetTrack` 改为按 `TrackView` 粒度管理（而非 `SlotCellView` 粒度），同一 TrackView 内移动不触发清除
   - 验证：在同一轨道内快速移动鼠标，Ghost 边框颜色不闪烁
   - _需求：Bug 3_

- [ ] 7. 清理 `DragDropManager` 冗余字段 `_ghost`（P2）
   - 删除私有字段 `_ghost`，将所有 `_ghost.xxx` 调用替换为 `_ghostView.xxx`
   - 删除 `Bind()` 中 `_ghost = _ghostView` 赋值语句
   - 验证：编译通过，拖拽功能正常
   - _需求：Issue 5_

- [ ] 8. 为 `TrackView._controller == null` 添加警告日志（P2）
   - 在 `RefreshSailCell()` 和 `RefreshSatCells()` 开头，当 `_controller == null` 时调用 `Debug.LogWarning($"[TrackView] _controller is null, SAIL/SAT slots will be empty")`
   - 验证：在 Inspector 中不连线 controller 时，Console 出现警告
   - _需求：Issue 1_
