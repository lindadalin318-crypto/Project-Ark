# 需求文档：战斗弧线增强 + 物理模拟完善

## 引言

当前陀螺战斗存在两个核心问题：
1. **战斗过快结束**：连续碰撞可在 1 秒内耗尽 RPM，玩家没有反应时间，体验极差。
2. **物理模拟不完整**：Magnus 切向偏转仅在对向自旋时生效，同向自旋缺乏切向效果；进动效应也仅在碰撞瞬间施加，缺乏持续性。

本次增强包含两个方向：
- **「战斗弧线三件套」**：方案 D（对角出发）+ 方案 B（碰撞冷却帧）+ 方案 A（濒死保护期），确保每场战斗有「开场 → 拉锯 → 收尾」三个可感知阶段。
- **物理模拟完善**：修复 Magnus 效应覆盖范围，增加同向自旋的切向偏转，使碰撞轨迹更符合真实陀螺物理。

---

## 需求

### 需求 1：对角出发入场（方案 D 简化版）

**用户故事：** 作为玩家，我希望战斗开始时双方陀螺从竞技场边缘对角位置出发，以便有 1~2 秒的「相向而行」开场动势，而非瞬间碰撞。

#### 验收标准

1. WHEN 战斗开始 THEN 玩家陀螺 SHALL 出现在竞技场边缘左下角方向（距中心约 ARENA.radius * 0.75 处），敌人陀螺 SHALL 出现在右上角方向（对角位置）。
2. WHEN 双方陀螺出现在边缘 THEN 碗形向心力 SHALL 自然引导两者向中心滑入，无需额外逻辑。
3. IF 玩家设置了发射角度和力度 THEN 初始速度方向 SHALL 仍基于 launchAngle 和 launchPower 计算，仅起始位置改变。
4. WHEN 陀螺从边缘出发 THEN 首次碰撞时间 SHALL 不早于战斗开始后 0.8 秒（由物理自然保证，无需硬编码计时器）。

---

### 需求 2：碰撞冷却帧（方案 B）

**用户故事：** 作为玩家，我希望每次碰撞后有约 0.5 秒的冷却间隔，以便防止连续帧叠加伤害导致战斗瞬间结束。

#### 验收标准

1. WHEN 两陀螺发生碰撞 THEN 系统 SHALL 为双方各自设置 30 帧（约 0.5 秒）的碰撞冷却计数器 `_collisionCooldown`。
2. WHEN 某陀螺的 `_collisionCooldown > 0` THEN `checkTopCollision` SHALL 跳过 RPM 伤害计算，但物理弹开（速度反弹）仍正常执行。
3. WHEN 每帧 physicsStep 执行 THEN `_collisionCooldown` SHALL 递减 1，直到归零。
4. IF 两陀螺均处于冷却期 THEN 碰撞检测 SHALL 仍执行物理分离（防止穿模），但不扣 RPM。
5. WHEN 冷却期内发生碰撞 THEN BattleLog SHALL 输出 `[冷却中-跳过伤害]` 标记，方便调试。

---

### 需求 3：濒死保护期（方案 A）

**用户故事：** 作为玩家，我希望当某方 RPM 跌破 15% 时进入 4 秒濒死保护期，以便给双方最后的逆转机会，增加战斗戏剧感。

#### 验收标准

1. WHEN 任意陀螺的 `rpm / maxRpm < 0.15` THEN 系统 SHALL 为该陀螺设置 `_dyingProtection = true` 并记录保护开始时间戳。
2. WHEN 陀螺处于濒死保护期 THEN 自然 RPM 衰减速率 SHALL 降低至正常值的 30%（即 `decayRate * 0.3`）。
3. WHEN 陀螺处于濒死保护期且受到碰撞伤害 THEN RPM 伤害 SHALL 降低至正常值的 40%。
4. WHEN 濒死保护期持续满 4 秒 THEN 保护期 SHALL 结束，`_dyingProtection = false`，恢复正常衰减和伤害。
5. IF 保护期结束后 `rpm / maxRpm` 仍 < 0.15 THEN 正常 Burst 结算逻辑 SHALL 继续生效（不再重新触发保护）。
6. WHEN 濒死保护期激活 THEN 保护期 SHALL 只触发一次（每场战斗每个陀螺最多一次），`_dyingProtectionUsed = true` 标记防止重复触发。
7. WHEN 濒死保护期激活 THEN BattleLog SHALL 输出保护期开始/结束的日志。

---

### 需求 4：Magnus 效应完善（物理模拟）

**用户故事：** 作为开发者，我希望 Magnus 切向偏转对同向自旋也生效（方向相反），以便碰撞轨迹更符合真实陀螺物理，同时让同向/对向自旋产生明显不同的碰撞手感。

#### 验收标准

1. WHEN 两陀螺碰撞且自旋方向相同（same-spin） THEN 系统 SHALL 施加切向冲量，方向与对向自旋相反（`pTangentSign` 取反），幅度为对向自旋的 60%（`magnusTangentRatio * 0.6`）。
2. WHEN 两陀螺碰撞且自旋方向相反（opposite-spin） THEN 现有 Magnus 逻辑 SHALL 保持不变（`magnusTangentRatio` 全量）。
3. WHEN 任意陀螺 RPM < 20% THEN Magnus 切向冲量 SHALL 衰减至 30%（现有逻辑，同向自旋同样适用）。
4. WHEN 同向自旋碰撞 THEN 视觉上两陀螺 SHALL 产生可见的弧线偏转，而非纯直线弹开。

---

### 需求 5：BattleLog 更新

**用户故事：** 作为开发者，我希望 BattleLog 能记录新增机制的关键事件，以便调试和验证三件套的实际效果。

#### 验收标准

1. WHEN 濒死保护期激活/结束 THEN BattleLog SHALL 输出包含陀螺身份（玩家/敌人）、当前 RPM 百分比、保护期时长的日志。
2. WHEN 碰撞冷却期内触发碰撞检测 THEN BattleLog SHALL 输出 `[冷却中-跳过伤害]` 及剩余冷却帧数。
3. WHEN 战斗结束摘要输出 THEN BattleLog SHALL 额外包含：总碰撞次数（含冷却跳过次数）、濒死保护触发次数、首次碰撞时间戳。
