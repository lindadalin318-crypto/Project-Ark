# StarChart UI Phase A/B/C — 全面 Review 报告

## 引言

本文档是对 StarChart UI Phase A、B、C 实现的全面代码审查报告。
审查范围：`TrackView.cs`、`SlotCellView.cs`、`DragDropManager.cs`、`StarChartPanel.cs`、`DragGhostView.cs`、`StatusBarView.cs`、`FlyBackAnimator.cs`、`InventoryItemView.cs`、`InventoryView.cs`、`StarChartTheme.cs`、`UICanvasBuilder.cs`。

---

## 🔴 严重 Bug（会导致运行时崩溃或功能完全失效）

### Bug 1：`DragDropManager.ExecuteDrop()` — 从 Slot 拖到 Slot 时 `DropTargetTrack == null` 判断永远不成立

**位置**：`DragDropManager.cs` → `ExecuteDrop()` 第 `DropTargetTrack == null` 分支

**问题**：
```csharp
else if (source == DragSource.Slot)
{
    var sourceTrack = CurrentPayload.SourceTrack;

    if (DropTargetTrack == null)          // ← 此分支意图是"拖到背包区域卸装"
    {
        UnequipFromTrack(item, sourceTrack);
    }
    ...
}
```
当用户从 Slot 拖到背包区域时，`InventoryView.OnDrop` 会先调用 `ExecuteUnequipDrop()` 再调用 `EndDrag(false)`，而 `EndDrag(false)` 内部因 `success=false` 不会再调用 `ExecuteDrop()`。但如果用户从 Slot 拖到空白区域（非 InventoryView），`EndDrag(true)` 会被调用，此时 `DropTargetTrack == null` 且 `DropTargetValid == false`，导致 `ExecuteDrop()` 根本不会被调用（因为 `success && DropTargetValid` 为 false）。**实际上这个分支永远不会被执行**，Slot→空白区域的拖拽会静默失败，没有任何反馈。

**影响**：从 Slot 拖到空白区域时无法卸装，且无任何错误提示。

---

### Bug 2：`DragDropManager.EquipToTrack()` — SAIL/SAT 装备时不检查 `DropTargetSlotType`，导致 Primary/Secondary 轨道混淆

**位置**：`DragDropManager.cs` → `EquipToTrack()` → `LightSailSO` / `SatelliteSO` 分支

**问题**：
```csharp
case LightSailSO sail:
    var existingSail = _controller?.GetEquippedLightSail();
    ...
    _controller?.EquipLightSail(sail);  // ← 直接调用 controller，忽略了 track 参数
    break;
```
`EquipToTrack(item, track)` 接收了 `track` 参数，但 SAIL/SAT 分支完全忽略了它，直接调用 `_controller`。这意味着无论用户把 SAIL 拖到 Primary 还是 Secondary 的 SAIL 槽，结果都一样（因为 SAIL/SAT 是全局的，不区分轨道）。

**影响**：目前 SAIL/SAT 是全局唯一的（不区分轨道），所以功能上没有 bug，但如果未来扩展为每轨道独立 SAIL/SAT，这里会出问题。**当前可接受，标记为技术债。**

---

### Bug 3：`SlotCellView.OnPointerExit()` — 清除高亮时会清除所有轨道的高亮，包括当前悬停的目标

**位置**：`SlotCellView.cs` → `OnPointerExit()`

**问题**：
```csharp
if (mgr.DropTargetTrack == OwnerTrack?.Track)
{
    mgr.DropTargetTrack = null;
    ...
}
```
当鼠标从一个 Cell 快速移动到同一 TrackView 的另一个 Cell 时，`OnPointerExit` 先触发，清除了 `DropTargetTrack`，然后 `OnPointerEnter` 才触发。在这个极短的间隙内，`DropTargetTrack` 为 null，Ghost 会短暂显示 `None` 状态（无边框），造成视觉闪烁。

**影响**：拖拽时在同一轨道内移动鼠标会有 Ghost 边框颜色闪烁。

---

### Bug 4：`FlyBackAnimator.FlyTo()` — `seq.OnComplete()` 在 `SkipAll()` 调用 `Complete()` 后不一定触发

**位置**：`FlyBackAnimator.cs` → `SkipAll()`

**问题**：
```csharp
public static void SkipAll()
{
    for (int i = _activeAnimations.Count - 1; i >= 0; i--)
    {
        _activeAnimations[i].Complete();  // ← PrimeTween Complete() 会触发 OnComplete 回调
    }
    _activeAnimations.Clear();
}
```
调用 `Complete()` 后，PrimeTween 会触发 `OnComplete` 回调（包括 `_activeAnimations.Remove(seq)`），然后 `SkipAll()` 又调用 `_activeAnimations.Clear()`。这会导致在 `Complete()` 的回调中修改正在被迭代的列表，可能引发 `InvalidOperationException`。

**影响**：在飞行动画进行中开始新拖拽时，可能抛出集合修改异常。

---

### Bug 5：`InventoryItemView.OnPointerEnter/Exit` — hover scale 动画未使用 `useUnscaledTime: true`

**位置**：`InventoryItemView.cs` → `OnPointerEnter()` / `OnPointerExit()`

**问题**：
```csharp
Tween.Scale(transform, endValue: Vector3.one * 1.06f, duration: 0.12f, ease: Ease.OutQuad);
// ← 缺少 useUnscaledTime: true
```
星图面板在游戏暂停（`Time.timeScale = 0`）时打开，所有动画必须使用 `useUnscaledTime: true`，否则动画不会播放。Phase C 需求明确要求此约束。

**影响**：游戏暂停时打开星图，背包格子 hover 动画完全失效（无放大效果）。

---

## 🟡 中等问题（功能缺失或逻辑不完整）

### Issue 1：`TrackView.Refresh()` — SAIL/SAT 刷新依赖 `_controller`，但 `_controller` 可能为 null

**位置**：`TrackView.cs` → `RefreshSailCell()` / `RefreshSatCells()`

**问题**：
```csharp
private void RefreshSailCell()
{
    if (_sailCell == null) return;
    _sailCell.SetThemeColor(StarChartTheme.SailColor);
    var sail = _controller?.GetEquippedLightSail();  // ← null-conditional，静默失败
    ...
}
```
当 `Bind()` 未传入 `controller`（或传入 null）时，SAIL/SAT 槽位永远显示为空，没有任何警告。

**影响**：如果 `Bind()` 调用时 controller 为 null，SAIL/SAT 槽位静默显示为空，难以调试。

---

### Issue 2：`DragDropManager.ShowReplaceMessage()` — 在 `EvictedItems` 被填充之前调用

**位置**：`DragDropManager.cs` → `EquipToTrack()` → SAIL 分支

**问题**：
```csharp
case LightSailSO sail:
    var existingSail = _controller?.GetEquippedLightSail();
    if (existingSail != null)
    {
        EvictedItems.Add((existingSail, track));
        _controller.UnequipLightSail();
        ShowReplaceMessage(item);  // ← 此时 EvictedItems 已有数据，OK
    }
    _controller?.EquipLightSail(sail);
    break;
```
SAIL 分支逻辑正确。但 `ShowReplaceMessage` 内部使用 `EvictedItems`，而 Core/Prism 分支中 `ShowReplaceMessage` 在 `EvictBlockingItems` 之后调用，也是正确的。**此 Issue 实际上是误报，逻辑正确。**

---

### Issue 3：`SlotCellView` — `_flashSequence` 字段未初始化，直接调用 `.Stop()` 可能有问题

**位置**：`SlotCellView.cs` → `SetItem()` / `SetEmpty()`

**问题**：
```csharp
private Sequence _flashSequence;  // 默认值为 default(Sequence)

public void SetItem(StarChartItemSO item)
{
    _flashSequence.Stop();  // ← 在未初始化的 Sequence 上调用 Stop()
    ...
}
```
PrimeTween 的 `Sequence` 是结构体，默认值为 `default`。在未初始化的 `Sequence` 上调用 `.Stop()` 是否安全取决于 PrimeTween 的实现。根据 PrimeTween 文档，对 invalid/default Sequence 调用 Stop() 是安全的（no-op），但这是隐式依赖，应加注释说明。

**影响**：低风险，但代码意图不清晰。

---

### Issue 4：`UICanvasBuilder.BuildTrackView()` — `WireField(trackView, "_trackLabel", labelTmp)` 引用了未定义的 `labelTmp`

**位置**：`UICanvasBuilder.cs` → `BuildTrackView()` 新增的 4 列布局代码

**问题**：在新增的 4 列布局代码中，`WireField(trackView, "_trackLabel", labelTmp)` 引用了 `labelTmp`，但新代码中没有定义 `labelTmp`（这是旧代码中的变量名）。这会导致编译错误。

**影响**：**编译失败**，UICanvasBuilder 无法使用。

---

### Issue 5：`DragDropManager` — `_ghost` 字段与 `_ghostView` 字段重复

**位置**：`DragDropManager.cs`

**问题**：
```csharp
[SerializeField] private DragGhostView _ghostView;
...
private DragGhostView _ghost;  // ← 冗余字段

public void Bind(...)
{
    if (_ghostView != null)
    {
        _ghost = _ghostView;  // ← 直接赋值，_ghost 和 _ghostView 始终相同
        _ghost.Hide();
    }
}
```
`_ghost` 和 `_ghostView` 始终指向同一个对象，`_ghost` 是多余的。代码中混用两个字段（有些地方用 `_ghost`，有些用 `_ghostView`），增加维护成本。

**影响**：代码质量问题，无运行时 bug。

---

### Issue 6：`InventoryView.OnDrop()` — 调用 `EndDrag(false)` 后 `CleanUp()` 会将 `_sourceCanvasGroup.alpha` 恢复为 1，但动画版本（`InventoryItemView`）已经用 PrimeTween 在 `OnEndDrag` 中恢复

**位置**：`InventoryView.cs` → `OnDrop()` + `DragDropManager.cs` → `CleanUp()`

**问题**：
```csharp
// InventoryView.OnDrop:
mgr.ExecuteUnequipDrop();
mgr.EndDrag(false);  // ← EndDrag 内部调用 CleanUp()，CleanUp 直接设 alpha=1f

// InventoryItemView.OnEndDrag:
Tween.Alpha(CachedCanvasGroup, endValue: 1f, duration: 0.08f, ...);  // ← 动画恢复
```
`CleanUp()` 直接设置 `_sourceCanvasGroup.alpha = 1f`（无动画），而 `InventoryItemView.OnEndDrag` 用 PrimeTween 动画恢复。两者都会执行，`CleanUp()` 的直接赋值会覆盖 PrimeTween 的动画起始状态，导致动画失效（直接跳到 1f）。

**影响**：拖拽结束时背包格子 alpha 恢复动画失效，直接跳变而非渐变。

---

### Issue 7：`FlyBackAnimator.TriggerFlyBackAnimations()` — 使用 Ghost 位置作为飞行起点，但 Ghost 在 `CleanUp()` 中已被隐藏

**位置**：`DragDropManager.cs` → `TriggerFlyBackAnimations()`

**问题**：
```csharp
private void TriggerFlyBackAnimations()
{
    var ghostRect = _ghost?.GetComponent<RectTransform>();
    foreach (var (evictedItem, _) in EvictedItems)
    {
        var fromRect = ghostRect ?? _inventoryRect;
        FlyBackAnimator.FlyTo(fromRect, _inventoryRect, evictedItem, _rootCanvas);
    }
}
```
`TriggerFlyBackAnimations()` 在 `ExecuteDrop()` 末尾调用，此时 `CleanUp()` 尚未执行（`CleanUp()` 在 `EndDrag()` 中调用，`ExecuteDrop()` 是 `EndDrag()` 的子调用）。但 Ghost 的 `RectTransform.position` 是 Ghost 当前的屏幕位置，这是正确的起点。**此 Issue 实际上逻辑正确。**

---

## 🟢 Phase 需求覆盖度检查

### Phase A 覆盖度

| 需求 | 状态 | 备注 |
|------|------|------|
| 1.1 深色背景 | ✅ | StarChartTheme.BgDeep |
| 1.2 四角 L 形装饰 | ✅ | UICanvasBuilder 生成 |
| 1.3 Header 栏 | ✅ | UICanvasBuilder 生成 |
| 1.4 Status Bar | ✅ | StatusBarView.cs |
| 1.5 上下分割 55/40 | ✅ | UICanvasBuilder 锚点 |
| 2.1-2.7 Track 视觉 | ✅ | TrackView + SlotCellView |
| 3.1-3.6 背包视觉 | ✅ | InventoryView + InventoryItemView |
| 4.1-4.3 颜色主题 | ✅ | StarChartTheme.cs |
| 5.1-5.4 UICanvasBuilder | ⚠️ | **Bug 4：labelTmp 编译错误** |
| 6.1-6.5 StatusBar 通知 | ✅ | StatusBarView.cs |

### Phase B 覆盖度

| 需求 | 状态 | 备注 |
|------|------|------|
| 1.1-1.6 4 列布局 | ✅ | TrackView 4 列完整 |
| 2.1-2.5 SAIL/SAT 拖拽 | ✅ | DragDropManager 支持 |
| 3.1-3.7 强制替换 | ✅ | EvictBlockingItems 实现 |
| 4.1-4.5 三色预览 | ✅ | SlotCellView + DragGhostView |
| 5.1-5.4 SlotCellView 扩展 | ✅ | SlotType 枚举完整 |
| 6.1-6.3 UICanvasBuilder 4 列 | ⚠️ | **Bug 4：labelTmp 编译错误** |

### Phase C 覆盖度

| 需求 | 状态 | 备注 |
|------|------|------|
| 1.1-1.6 飞回动画 | ⚠️ | FlyBackAnimator 实现，但 **Bug 4（SkipAll 集合修改）** |
| 2.1-2.5 Ghost 增强 | ✅ | DragGhostView 完整 |
| 3.1-3.3 槽位装备动画 | ✅ | SlotCellView flash/fade |
| 4.1-4.6 背包格子动画 | ⚠️ | **Bug 5：hover 动画缺 useUnscaledTime** |
| 5.1-5.3 StatusBar 动画 | ✅ | StatusBarView 淡入淡出 |
| 6.1-6.3 面板开关动画 | ✅ | StarChartPanel Open/Close |

---

## 需要修复的问题汇总（按优先级）

### P0 — 必须修复（编译/崩溃）
1. **Bug 4（UICanvasBuilder）**：`labelTmp` 未定义，编译失败
2. **Bug 4（FlyBackAnimator）**：`SkipAll()` 在迭代中修改集合，可能崩溃

### P1 — 应该修复（功能缺失）
3. **Bug 5（InventoryItemView）**：hover 动画缺 `useUnscaledTime: true`，暂停时失效
4. **Issue 6（DragDropManager CleanUp）**：直接赋值 alpha 覆盖 PrimeTween 动画
5. **Bug 1（ExecuteDrop）**：Slot→空白区域拖拽静默失败，无反馈

### P2 — 建议修复（体验/代码质量）
6. **Bug 3（SlotCellView）**：同轨道内移动鼠标时 Ghost 边框闪烁
7. **Issue 5（DragDropManager）**：`_ghost` / `_ghostView` 冗余字段
8. **Issue 1（TrackView）**：`_controller == null` 时缺少警告日志
