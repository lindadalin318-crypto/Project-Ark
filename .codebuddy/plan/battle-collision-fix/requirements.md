# 需求文档：战斗碰撞修复 + 不规则弹射系统

## 引言

当前战斗存在两个根本性 Bug 导致整场战斗零碰撞：

1. **碰撞检测距离错误**：`collisionDist = 28px`，但每个陀螺 `radius = 18px`，两圆相切距离应为 36px。28 < 36 意味着两个陀螺可以物理上"穿过"彼此而不触发任何碰撞，这是最高优先级的 Bug。

2. **轨道扰动区间失效**：`orbitDistanceMin = 55px`，`orbitDistanceMax = 143px`。两个陀螺从对角出发（初始距离约 330px），被 `bowlForce` 向心力拉向中心后，实际绕圈距离往往 **< 55px**（近距离绕圈），完全不在检测区间内，导致扰动从未触发。

3. **不规则弹射缺失**：真实战斗陀螺有菱角和凸起，碰撞时会产生不规则的随机偏转，使轨迹更多变、战斗更刺激。当前碰撞完全是理想弹性碰撞，弹射方向过于可预测。

本次修复同时解决以上三个问题，目标是让每场战斗都能产生真实的碰撞交互。

---

## 需求

### 需求 1：修正碰撞检测距离

**用户故事：** 作为玩家，我希望两个陀螺在视觉上接触时能真实发生碰撞，以便战斗有实质性的物理交互而不是互相穿透。

#### 验收标准

1. WHEN 两个陀螺中心距离 ≤ `(p.radius + e.radius)` 时 THEN 系统 SHALL 触发碰撞检测（当前两个陀螺 radius 均为 18，故阈值为 36px）
2. WHEN `collisionDist` 参数被修改 THEN 系统 SHALL 使用新值作为碰撞触发阈值，不得硬编码
3. IF 两个陀螺半径不同 THEN 系统 SHALL 使用 `p.radius + e.radius` 作为动态碰撞距离，而非固定常量
4. WHEN 碰撞发生时 THEN 系统 SHALL 将两个陀螺分离至恰好不重叠（overlap 修正逻辑保持不变）

---

### 需求 2：扩大并修正轨道扰动检测区间

**用户故事：** 作为玩家，我希望当两个陀螺长时间绕圈而不碰撞时，系统能有效检测并打破这种僵局，以便战斗不会变成无聊的消耗战。

#### 验收标准

1. WHEN 两个陀螺距离在 `orbitDistanceMin` 到 `orbitDistanceMax` 之间持续 `orbitDetectFrames` 帧 THEN 系统 SHALL 触发轨道扰动
2. `orbitDistanceMin` SHALL 修改为 `ARENA.radius * 0.05`（约 11px），覆盖近距离绕圈场景
3. `orbitDistanceMax` SHALL 修改为 `ARENA.radius * 0.85`（约 187px），覆盖远距离绕圈场景
4. `orbitBreakImpulse` SHALL 修改为 `0.45`（原 0.22），确保冲量足够推动陀螺相向运动
5. `orbitDetectFrames` SHALL 修改为 `120`（原 180，约 2 秒），更快响应僵局
6. `orbitBreakCooldown` SHALL 修改为 `180`（原 300，约 3 秒），允许更频繁的扰动
7. WHEN 轨道扰动触发时 THEN BattleLog SHALL 记录当前距离、冲量值和稳定帧数

---

### 需求 3：不规则弹射角度偏转（菱角效果）

**用户故事：** 作为玩家，我希望碰撞后陀螺的弹射方向有一定随机性，以便轨迹更多变、战斗更刺激，模拟真实陀螺菱角造成的不规则弹射。

#### 验收标准

1. WHEN 碰撞发生（非冷却跳过）时 THEN 系统 SHALL 在碰撞法线方向上叠加一个随机角度偏转，范围为 `±irregularDeflectAngle`（默认 ±20°）
2. 随机偏转 SHALL 对双方陀螺独立计算（各自随机，不共享同一偏转值）
3. `irregularDeflectAngle` SHALL 作为 ARENA 常量暴露，默认值 `20`（度），方便调参
4. WHEN 偏转角度为 0 时 THEN 系统 SHALL 退化为原有的理想弹性碰撞行为（向下兼容）
5. 随机偏转 SHALL 在物理冲量计算完成后叠加，不影响 RPM 伤害计算
6. WHEN 冷却跳过碰撞时 THEN 系统 SHALL 仍然应用随机偏转（物理弹射不受冷却影响）
