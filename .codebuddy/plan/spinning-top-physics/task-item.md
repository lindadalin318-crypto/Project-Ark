# 实施计划 — 陀螺物理模拟改进

- [ ] 1. 扩展 ARENA 常量，添加物理参数字段
   - 在 `ARENA` 对象中新增：`linearDamping`（0.985）、`linearDampingLow`（0.97）、`bowlForce`（0.0008）、`bowlForceDeadZone`（30）、`restitution`（0.85）、`magnusTangentRatio`（0.3）
   - 所有后续步骤的魔法数字均引用此处字段
   - _需求：6.1、6.2_

- [ ] 2. 实现平移速度自然衰减
   - 在物理步进函数（每帧更新 `vx`/`vy` 的位置）中，对双方陀螺的 `vx`/`vy` 乘以 `ARENA.linearDamping`
   - IF RPM < 30% maxRpm THEN 改用 `ARENA.linearDampingLow`
   - 若零件存在 `decayMult` 属性，将其同时作用于平移衰减系数
   - _需求：1.1、1.2、1.3、1.4_

- [ ] 3. 实现碗形向心力
   - 在物理步进函数中，计算每个陀螺到竞技场中心的距离向量
   - IF dist > `ARENA.bowlForceDeadZone` THEN 施加 `dist * ARENA.bowlForce` 的向心加速度
   - IF RPM < 40% maxRpm THEN 向心力系数 ×1.5
   - _需求：2.1、2.2、2.3、2.4_

- [ ] 4. 修正碰撞冲量公式（替换 `impact * 1.4` 魔法系数）
   - 以 RPM 作为质量代理（`mA = player.rpm`, `mB = enemy.rpm`），使用标准弹性碰撞公式计算法向冲量
   - 弹性系数使用 `ARENA.restitution`（0.85），确保碰撞后总动能不增加
   - IF 两陀螺自旋方向相同 THEN 法向冲量 ×0.8，并对双方各扣除少量 RPM（研磨损耗）
   - _需求：3.1、3.2、3.4_

- [ ] 5. 实现 Magnus 切向偏转效果
   - 碰撞时计算碰撞法线的垂直切向量
   - IF 两陀螺自旋方向相反 THEN 对双方各施加 `法向冲量 * ARENA.magnusTangentRatio` 的切向速度，方向由各自 `spinDir` 决定
   - IF RPM < 20% maxRpm THEN Magnus 效果 ×0.3
   - _需求：3.3、4.1、4.2、4.3_

- [ ] 6. 汉化元素状态反馈文字
   - 搜索所有 `BURN!`、`FROZEN!`、`VENOM!`、`SHOCKED!`、`MAGNET!` 字符串
   - 替换为对应中文：`燃烧！`、`冻结！`、`中毒！`、`中毒 x{n}！`、`麻痹！`、`磁力！`
   - _需求：5.1、5.2、5.3、5.4、5.5、5.6_
