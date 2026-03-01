# 实施计划：战斗 Console Log 系统

- [ ] 1. 创建 `_battleLogger` 数据收集器并在战斗开始时初始化
   - 在 `spinning-top.html` 中声明全局 `_battleLogger` 对象，包含 `startTimestamp`、`collisionCount`、`thrustCount`、`brakeCount`、`activateCount`、`elementCounts`（按类型）、`lastSnapshotTime` 字段
   - 在 `startBattleLoop()` 调用处重置并初始化 `_battleLogger`，记录 `performance.now()` 作为开始时间戳
   - _需求：7.1、7.4_

- [ ] 2. 实现战斗开始日志（需求 1）
   - 在 `startBattleLoop()` 内、战斗循环启动后，调用 `console.group` 输出双方初始配置：敌人名称、玩家/敌人初始 RPM、自旋方向、玩家零件列表（AR/WD/SG/BB）、计时器上限
   - 输出完毕后调用 `console.groupEnd()`
   - _需求：1.1、1.2、1.3、1.4_

- [ ] 3. 实现碰撞事件日志（需求 2）
   - 在 `checkTopCollision` 函数内 `impact < 0` 条件成立处，调用 `console.log` 输出已用时、碰撞后双方 RPM、oppositeSpins 标志
   - 同步将 `_battleLogger.collisionCount` 自增
   - _需求：2.1、2.2、2.3、7.1_

- [ ] 4. 实现元素状态触发日志（需求 3）
   - 在 `applyElementEffect` 函数成功施加状态后，调用 `console.log` 输出元素类型、目标方、持续帧数；poison 类型额外输出当前叠加层数
   - 同步将 `_battleLogger.elementCounts[type]` 自增
   - _需求：3.1、3.2、3.3、7.1_

- [ ] 5. 实现干预技能使用日志（需求 4）
   - 在 `doThrust`、`doBrake`、`doActivate` 函数成功执行路径末尾，分别调用 `console.log` 输出已用时及对应附加字段（剩余次数 / RPM 回复量 / 零件名称）
   - 同步将 `_battleLogger` 中对应计数器自增
   - _需求：4.1、4.2、4.3、4.4、7.1_

- [ ] 6. 实现逐 5 秒快照日志（需求 5）
   - 在 `physicsStep`（或主战斗 tick）中，检测 `(performance.now() - _battleLogger.lastSnapshotTime) >= 5000` 时输出快照日志：已用时、双方 RPM 及百分比、双方活跃元素状态列表，并更新 `lastSnapshotTime`
   - _需求：5.1、5.2、5.3_

- [ ] 7. 实现战斗结束摘要日志（需求 6）
   - 在 `endBattle` 和 `endBattleTimeout` 函数中，调用 `console.group` 输出完整摘要：总时长（`(performance.now() - startTimestamp)/1000`）、胜负结果、结束方式、双方最终 RPM 及百分比、总碰撞次数、干预使用次数（三项）、元素触发次数（按类型）
   - 输出完毕后调用 `console.groupEnd()`；`endBattleTimeout` 路径标注 `timeout` 和 `平局`
   - _需求：6.1、6.2、6.3、6.4、7.2、7.3_
