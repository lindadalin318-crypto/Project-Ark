# 需求文档：ScaffoldToSceneGenerator 生成关卡飞船被阻挡问题修复

## 引言

通过 `ScaffoldToSceneGenerator.cs` 生成的 `--- 示巴星 · ACT1+ACT2 (Z1a→Z2d) ---` 关卡中，飞船进入任意 Room 时会被卡在房间边缘，无法正常进入。而通过旧版 `ShebaLevelScaffolder.cs` 生成的 `--- Sheba Level ---` 关卡则工作正常。

### 根因分析（已完成代码审查）

通过对比两套生成器的代码，发现以下 **3 个潜在根因**，按可能性从高到低排列：

---

### 根因 1（最高可能性）：`_playerLayer` 字段写入方式错误

**`ScaffoldToSceneGenerator.cs`** 中：
```csharp
serialized.FindProperty("_playerLayer").intValue = playerLayerMask;
```
`LayerMask.GetMask("Player")` 返回的是 **bitmask**（例如 Layer 8 → 值为 256），而 `_playerLayer` 是 `LayerMask` 类型，其 `intValue` 应该直接存 bitmask。

**但问题在于**：`LayerMask.GetMask()` 返回的是 bitmask，而 `SerializedProperty.intValue` 对 `LayerMask` 字段存储的也是 bitmask，**理论上是正确的**。

然而，如果 `"Player"` Layer 在 Project Settings 中不存在，`LayerMask.GetMask("Player")` 返回 **0**，导致 `_playerLayer.value == 0`，`IsPlayerLayer()` 永远返回 `false`，Room 的 `OnTriggerEnter2D` 永远不触发，飞船无法被识别为进入房间，**RoomManager 不会切换当前房间**。

这不会直接导致"被卡"，但会导致 Room 系统失效。

---

### 根因 2（最高可能性）：`CameraConfiner` 的 `PolygonCollider2D` 是实体碰撞体

**两套生成器都将 `CameraConfiner` 子对象设置为 Layer 2（Ignore Raycast）**，且 `polyCol.isTrigger = false`（实体碰撞体）。

**关键问题**：`Ignore Raycast` Layer 只忽略射线检测，**不忽略 Physics2D 碰撞**。如果 Physics2D 碰撞矩阵中 `Ignore Raycast` 与 `Player` 之间的碰撞**没有被关闭**，这个实体 `PolygonCollider2D` 就会像一堵墙一样包围整个房间，阻止飞船进入。

**为什么 Sheba Level 正常？** 因为 `ShebaLevelScaffolder` 是旧版工具，生成的场景可能已经手动修复过，或者 Physics2D 矩阵在那之后被修改过。

---

### 根因 3（次要可能性）：Room GO 的 Layer 设置为 `RoomBounds`

`ScaffoldToSceneGenerator` 将 Room GO 的 Layer 设置为 `RoomBounds`（这是修复补丁加入的），而 `ShebaLevelScaffolder` 没有设置任何特殊 Layer（使用 Default）。

如果 Physics2D 碰撞矩阵中 `RoomBounds` 与 `Player` 之间的碰撞**没有被关闭**，Room GO 上的 `BoxCollider2D`（虽然是 `isTrigger = true`）不会产生物理阻挡，但如果 Room GO 上还有其他非 Trigger 碰撞体，就会阻挡。

---

## 需求

### 需求 1：修复 CameraConfiner PolygonCollider2D 的物理阻挡

**用户故事：** 作为一名关卡设计师，我希望 CameraConfiner 的 PolygonCollider2D 不会对飞船产生物理阻挡，以便飞船可以自由进出任意房间。

#### 验收标准

1. WHEN `ScaffoldToSceneGenerator` 生成 Room GO 时，THEN 系统 SHALL 将 `CameraConfiner` 子对象的 `PolygonCollider2D` 设置为 `isTrigger = true`，或将其 Layer 设置为一个与 Player 无碰撞的专用 Layer（如 `CameraOnly`）。
2. IF `CameraConfiner` 的 `PolygonCollider2D` 为实体碰撞体（`isTrigger = false`），THEN 系统 SHALL 确保其所在 Layer 在 Physics2D 碰撞矩阵中与 `Player` Layer 的碰撞被禁用。
3. WHEN 飞船进入任意由 `ScaffoldToSceneGenerator` 生成的房间时，THEN 飞船 SHALL 不被任何不可见的碰撞体阻挡。

---

### 需求 2：修复 `_playerLayer` 字段的正确赋值

**用户故事：** 作为一名开发者，我希望生成的 Room 和 Door 组件的 `_playerLayer` 字段被正确赋值，以便 `OnTriggerEnter2D` 能正确识别玩家飞船。

#### 验收标准

1. WHEN `ScaffoldToSceneGenerator` 生成 Room GO 时，THEN 系统 SHALL 将 `_playerLayer` 字段赋值为正确的 `LayerMask` bitmask（`LayerMask.GetMask("Player")`）。
2. IF `"Player"` Layer 不存在于 Project Settings，THEN 系统 SHALL 在 Console 输出明确的 Warning，并提示用户添加该 Layer。
3. WHEN 飞船进入房间的 `BoxCollider2D` Trigger 区域时，THEN `Room.OnPlayerEntered` 事件 SHALL 被触发，`RoomManager` SHALL 切换当前房间。

---

### 需求 3：提供批量修复工具，修复已生成场景中的问题

**用户故事：** 作为一名开发者，我希望有一个一键修复工具，能批量修复场景中已生成的所有 Room 的碰撞体问题，以便不需要重新生成整个关卡。

#### 验收标准

1. WHEN 开发者执行批量修复工具时，THEN 系统 SHALL 遍历场景中所有 `Room` 组件，将其 `CameraConfiner` 子对象的 `PolygonCollider2D` 设置为 `isTrigger = true`。
2. WHEN 批量修复完成时，THEN 系统 SHALL 在 Console 输出修复了多少个 Room 的报告。
3. IF 某个 Room 的 `CameraConfiner` 已经是正确状态，THEN 系统 SHALL 跳过该 Room（不重复修改）。
