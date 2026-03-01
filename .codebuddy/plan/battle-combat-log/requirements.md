# 需求文档：战斗 Console Log 系统

## 引言

在陀螺战斗（spinning-top.html）进行期间，通过浏览器 `console` 实时输出结构化的战斗日志，帮助开发者和策划人员观察每场战斗的完整过程。日志涵盖战斗初始化信息、关键战斗事件（碰撞、元素状态、干预技能）、逐秒快照，以及战斗结束时的完整统计摘要（含总时长）。所有日志均以 `[BattleLog]` 前缀标识，方便在 DevTools 中过滤。

---

## 需求

### 需求 1：战斗开始日志

**用户故事：** 作为一名战斗策划，我希望在战斗开始时看到双方陀螺的初始配置信息，以便快速了解本场战斗的起始条件。

#### 验收标准

1. WHEN 战斗倒计时结束、`startBattleLoop()` 被调用 THEN 系统 SHALL 在 console 输出一条 `console.group('[BattleLog] ⚔️ 战斗开始')` 分组日志。
2. WHEN 战斗开始日志输出 THEN 系统 SHALL 包含以下字段：敌人名称/原型、玩家初始 RPM、敌人初始 RPM、玩家自旋方向、敌人自旋方向、玩家装备零件列表（AR/WD/SG/BB 槽位名称）、战斗计时器上限（秒）。
3. WHEN 战斗开始日志输出完毕 THEN 系统 SHALL 调用 `console.groupEnd()` 关闭分组。
4. WHEN 战斗尚未开始（`G.battle.running === false`）THEN 系统 SHALL NOT 输出战斗开始日志。

---

### 需求 2：碰撞事件日志

**用户故事：** 作为一名战斗策划，我希望每次陀螺碰撞时都有日志记录，以便分析碰撞频率和 RPM 变化趋势。

#### 验收标准

1. WHEN `checkTopCollision` 检测到两陀螺发生碰撞（`impact < 0`）THEN 系统 SHALL 输出一条 `console.log('[BattleLog] 💥 碰撞')` 日志。
2. WHEN 碰撞日志输出 THEN 系统 SHALL 包含以下字段：碰撞发生时的战斗已用时（秒，保留1位小数）、碰撞后玩家 RPM（取整）、碰撞后敌人 RPM（取整）、双方自旋方向是否相反（oppositeSpins）。
3. WHEN 同一帧内发生多次碰撞检测 THEN 系统 SHALL 仅在 `impact < 0` 条件成立时输出，避免重复日志。

---

### 需求 3：元素状态触发日志

**用户故事：** 作为一名战斗策划，我希望每次元素状态效果被施加时有日志，以便验证元素系统的触发逻辑是否正确。

#### 验收标准

1. WHEN `applyElementEffect` 成功为目标施加元素状态 THEN 系统 SHALL 输出一条 `console.log('[BattleLog] 🔮 元素状态')` 日志。
2. WHEN 元素状态日志输出 THEN 系统 SHALL 包含以下字段：元素类型（fire/ice/poison/thunder/magnet）、目标方（"玩家" 或 "敌人"）、持续帧数、当前毒素层数（仅 poison 类型）。
3. WHEN 元素状态为 poison 且叠加层数增加 THEN 系统 SHALL 在日志中标注新的叠加层数。

---

### 需求 4：干预技能使用日志

**用户故事：** 作为一名战斗策划，我希望玩家每次使用干预技能（冲刺/制动/激活）时有日志，以便分析玩家的操作时机。

#### 验收标准

1. WHEN 玩家成功触发冲刺（`doThrust`）THEN 系统 SHALL 输出 `console.log('[BattleLog] ⚡ 干预: 冲刺')` 并附带使用时的战斗已用时和剩余次数。
2. WHEN 玩家成功触发制动（`doBrake`）THEN 系统 SHALL 输出 `console.log('[BattleLog] 🛑 干预: 制动')` 并附带使用时的战斗已用时、RPM 回复量和剩余次数。
3. WHEN 玩家成功触发激活（`doActivate`）THEN 系统 SHALL 输出 `console.log('[BattleLog] ✨ 干预: 激活')` 并附带使用时的战斗已用时和激活的零件名称。
4. IF 干预技能因冷却或次数耗尽而失败 THEN 系统 SHALL NOT 输出干预日志（仅记录成功使用）。

---

### 需求 5：逐秒战斗快照日志

**用户故事：** 作为一名战斗策划，我希望每隔一定时间输出一次双方状态快照，以便观察 RPM 衰减曲线和位置变化趋势。

#### 验收标准

1. WHEN 战斗运行中每经过 5 秒 THEN 系统 SHALL 输出一条 `console.log('[BattleLog] 📊 快照')` 日志。
2. WHEN 快照日志输出 THEN 系统 SHALL 包含以下字段：当前战斗已用时（秒）、玩家 RPM 及其占 maxRpm 的百分比、敌人 RPM 及其占 maxRpm 的百分比、玩家当前活跃元素状态列表、敌人当前活跃元素状态列表。
3. WHEN 战斗结束时距上次快照不足 5 秒 THEN 系统 SHALL NOT 补输一次快照（结束摘要已涵盖最终状态）。

---

### 需求 6：战斗结束摘要日志

**用户故事：** 作为一名战斗策划，我希望战斗结束时输出一份完整的统计摘要，尤其是战斗总时长，以便评估战斗节奏是否符合设计预期。

#### 验收标准

1. WHEN `endBattle` 或 `endBattleTimeout` 被调用 THEN 系统 SHALL 在 console 输出一条 `console.group('[BattleLog] 🏁 战斗结束')` 分组摘要。
2. WHEN 结束摘要输出 THEN 系统 SHALL 包含以下字段：
   - **战斗总时长**（秒，保留1位小数，格式如 `47.3s`）
   - 胜负结果（玩家胜/玩家败/平局）
   - 结束方式（ringout / burst / timeout）
   - 玩家最终 RPM 及其占 maxRpm 的百分比
   - 敌人最终 RPM 及其占 maxRpm 的百分比
   - 战斗期间总碰撞次数
   - 玩家干预技能使用次数（冲刺/制动/激活各自计数）
   - 玩家触发元素状态次数（按类型分类）
3. WHEN 结束摘要输出完毕 THEN 系统 SHALL 调用 `console.groupEnd()` 关闭分组。
4. WHEN 战斗因超时平局结束（`endBattleTimeout`）THEN 系统 SHALL 在结束方式字段标注 `timeout`，胜负结果标注 `平局`。

---

### 需求 7：日志数据收集器（BattleLogger）

**用户故事：** 作为一名开发者，我希望所有战斗日志数据由一个统一的收集器对象管理，以便日后扩展（如导出 JSON、接入分析工具）。

#### 验收标准

1. WHEN 战斗开始 THEN 系统 SHALL 初始化一个 `_battleLogger` 对象，包含：战斗开始时间戳（`performance.now()`）、碰撞计数器、干预使用计数器（冲刺/制动/激活）、元素状态触发计数器（按类型）、上次快照时间戳。
2. WHEN 战斗结束 THEN 系统 SHALL 通过 `_battleLogger` 计算战斗总时长 = `(performance.now() - startTimestamp) / 1000`。
3. WHEN 战斗结束后 THEN 系统 SHALL 将 `_battleLogger` 保留在全局作用域，以便开发者在 DevTools console 中直接访问 `_battleLogger` 查看原始数据。
4. IF 新战斗开始 THEN 系统 SHALL 重置 `_battleLogger`，不保留上一场战斗的数据。
