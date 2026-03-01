# 实施计划：轨道共振破坏（方案 F）

- [ ] 1. 在 ARENA 常量区块中添加轨道扰动参数
   - 在现有 `ARENA` 常量对象中新增 5 个可调参数：`orbitDetectFrames`（默认 180）、`orbitDistanceMin`（竞技场半径 × 0.25）、`orbitDistanceMax`（竞技场半径 × 0.65）、`orbitBreakImpulse`（默认 0.22）、`orbitBreakCooldown`（默认 300）
   - _需求：3.1_

- [ ] 2. 初始化轨道检测状态变量
   - 在战斗状态对象（或 battle 作用域）中新增：`_orbitStableFrames`（连续稳定帧计数器，初始 0）、`_orbitBreakCooldown`（冷却帧倒计时，初始 0）
   - 确保战斗开始时两个变量均重置为 0
   - _需求：1.1、3.1_

- [ ] 3. 实现每帧距离采样与稳定轨道判定逻辑
   - 在主战斗更新循环（`updateBattle` 或等效函数）中，每帧计算双方陀螺的欧氏距离
   - 若距离在 `orbitDistanceMin`～`orbitDistanceMax` 区间内，则 `_orbitStableFrames++`；否则重置为 0
   - 若 `_orbitBreakCooldown > 0`，每帧 `-1`，并跳过稳定帧累计
   - _需求：1.1、1.2、1.3_

- [ ] 4. 实现濒死保护期豁免检查
   - 在稳定帧累计逻辑前，检查双方陀螺是否有任意一方处于濒死保护期（`_deathProtectionActive === true`）
   - 若是，则跳过本帧的稳定帧累计（不重置，也不累加），保持计数器冻结
   - _需求：1.4、4.4_

- [ ] 5. 实现轨道扰动冲量施加与限速
   - 当 `_orbitStableFrames >= ARENA.orbitDetectFrames` 时，计算从各自陀螺指向对方的单位向量
   - 对双方陀螺的 `vx/vy` 分别叠加 `单位向量 × ARENA.orbitBreakImpulse`
   - 施加后检查合速度是否超过正常最大速度的 1.5 倍，若超过则等比缩放限速
   - 重置 `_orbitStableFrames = 0`，设置 `_orbitBreakCooldown = ARENA.orbitBreakCooldown`
   - _需求：2.1、2.2、2.3、2.5、4.2_

- [ ] 6. 在 BattleLog 中记录扰动事件
   - 扰动冲量施加时，向 BattleLog 输出一条日志，格式为：`🌀 轨道扰动 | 已用时: Xs | 双方距离: Ypx | 冲量: Z | 稳定帧数: N`
   - _需求：2.4_

- [ ] 7. 在战斗结算日志中汇总扰动触发次数
   - 维护一个 `_orbitBreakCount` 计数器，每次触发扰动时 `+1`
   - 在战斗结束的 BattleLog 汇总区块中追加一行：`轨道扰动触发次数: N`
   - _需求：2.4_
