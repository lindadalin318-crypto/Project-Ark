# 实施计划：战斗高光时刻（Bullet Time + 镜头聚焦）

- [ ] 1. 添加高光时刻状态管理与参数配置
   - 在 ARENA 常量区新增 `highlightCooldown`（4s）、`highlightSlowScale`（0.2）、`highlightSlowInDuration`（0.15s）、`highlightSlowOutDuration`（0.2s）、`highlightSlowHoldDuration`（0.6s）、`highlightTriggerDistance`（80px）等参数
   - 新增 `highlightState` 对象，包含：`active`、`cooldownTimer`、`scheduledTimes[]`、`pendingIndex`、`pendingWaitTimer`、`timeScale`、`cameraZoom`、`cameraOffsetX`、`cameraOffsetY`
   - _需求：3.1、3.2、5.3_

- [ ] 2. 实现保底触发时间点预分配逻辑
   - 在战斗开始（`startBattle`）时调用 `scheduleHighlightTimes()`，随机生成 2～3 个时间点，分别落在前期 / 中期 / 后期各一个区间内
   - 每帧检测当前战斗时间是否到达下一个预分配时间点，到达后进入"等待距离"状态，最多等待 3 秒，超时跳过
   - _需求：1.1、1.2、1.3_

- [ ] 3. 实现伤害触发检测逻辑
   - 在碰撞伤害计算完成后，判断本次伤害值是否 ≥ 敌方当前 RPM × 10%
   - 若满足条件且不在冷却期内，则调用 `triggerHighlight()`
   - _需求：2.1、2.2_

- [ ] 4. 实现 Bullet Time（timeScale 插值）逻辑
   - 实现 `triggerHighlight()`：设置 `highlightState.active = true`，启动 timeScale 从 1.0 → 0.2 的线性插值（0.15s 内完成）
   - 持续 0.6s（真实时间）后，启动 timeScale 从 0.2 → 1.0 的线性插值（0.2s 内完成），恢复后标记结束并启动冷却计时
   - 在游戏主循环 `update()` 中，将所有物理位移乘以 `highlightState.timeScale`，实现全局时间缩放
   - _需求：3.1、3.2、3.3_

- [ ] 5. 实现镜头聚焦（Canvas scale + translate）逻辑
   - 高光时刻触发时，计算两陀螺中心点 `(cx, cy)`，计算目标缩放比例使两陀螺占屏幕宽度 70%～80%
   - 在 0.15s 内平滑插值 `cameraZoom`、`cameraOffsetX`、`cameraOffsetY` 到目标值
   - 在 Canvas `draw()` 函数开头应用 `ctx.save() → ctx.translate(cx, cy) → ctx.scale(zoom) → ctx.translate(-cx, -cy)`，慢动作期间每帧实时更新中心点跟踪两陀螺位置
   - 慢动作结束时同步将镜头参数插值还原到默认值（zoom=1, offset=0）
   - _需求：4.1、4.2、4.3、4.4、4.5_

- [ ] 6. 实现效果协调与边界保护
   - 在 `endBattle()` 中强制重置 `timeScale = 1.0`、`cameraZoom = 1.0`、`cameraOffset = (0,0)`，清空所有高光状态
   - 高光时刻 `active` 期间，在轨道扰动检测逻辑入口处添加 `if (highlightState.active) return` 跳过扰动
   - 每局战斗开始时调用 `resetHighlightState()` 重置全部状态
   - _需求：5.1、5.2、5.3、5.4_
