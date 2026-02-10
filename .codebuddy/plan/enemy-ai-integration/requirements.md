# 需求文档 — Enemy AI 系统集成

## 引言

Project Ark 已实现了完整的敌人 AI 代码框架，包括：
- **EnemyEntity**（躯壳层）：HP、移动、受伤闪白、死亡、对象池生命周期
- **EnemyBrain**（大脑层）：外层 HFSM（Idle → Chase → Engage → Return）
- **EngageState 内层子状态机**：Telegraph → Attack → Recovery（信号-窗口攻击模型）
- **EnemyPerception**（感知系统）：视锥+LoS 视觉检测、武器开火听觉检测、记忆衰减
- **EnemyStatsSO**（数据驱动配置）：全部数值字段已定义
- **Rusher 原型数据**：`EnemyStats_Rusher.asset` 已创建并填写完整
- **Rusher Prefab**：`Enemy_Rusher.prefab` 已组装（含 Rigidbody2D + Collider2D + SpriteRenderer + 三大脚本）

然而，这些代码尚未真正接入游戏循环。目前缺失三个关键环节：
1. **玩家飞船无法被攻击** — 飞船未实现 `IDamageable` 接口，敌人 `AttackSubState.TryHitPlayer()` 中的 `GetComponent<IDamageable>()` 永远返回 null
2. **没有敌人生成机制** — Prefab 存在但从未在场景中实例化，既没有手动放置也没有 Spawner
3. **缺少完整闭环验证** — 需要在场景中配置至少一个可运行的敌人实例，验证 感知→追击→攻击→受伤→死亡 的完整循环

---

## 需求

### 需求 1：玩家飞船实现 IDamageable 接口

**用户故事：** 作为一名玩家，我希望敌人的攻击能够对我的飞船造成伤害，以便战斗系统形成双向交互的完整循环。

#### 验收标准

1. WHEN 飞船 GameObject 上存在实现了 `IDamageable` 的组件 THEN `AttackSubState.TryHitPlayer()` 中的 `GetComponent<IDamageable>()` SHALL 返回非 null 引用。

2. WHEN 敌人的 `AttackSubState` 在 active hitbox 阶段检测到玩家 THEN 系统 SHALL 调用飞船的 `TakeDamage(damage, knockbackDir, knockbackForce)` 方法，扣减飞船 HP。

3. WHEN 飞船受到伤害 THEN 系统 SHALL 提供视觉反馈（受击闪白），让玩家明确感知到被击中。

4. WHEN 飞船 HP 降至 0 THEN 系统 SHALL 触发死亡流程（禁用控制、播放视觉反馈）；具体死亡后果（重生/Game Over）暂不实现，仅做标记和事件通知。

5. IF 飞船处于死亡状态 THEN 系统 SHALL 忽略后续的 `TakeDamage` 调用（防止重复死亡）。

6. WHEN 飞船受到带有 knockbackForce > 0 的伤害 THEN 系统 SHALL 通过 `Rigidbody2D.AddForce(Impulse)` 对飞船施加击退力，该击退力与飞船的正常移动叠加（利用现有 `ShipMotor` 的减速逻辑自然衰减）。

7. WHEN 新建飞船受伤组件 THEN 该组件 SHALL 遵循项目架构原则：数据驱动（HP 最大值等参数放在 `ShipStatsSO` 或新建独立 SO 中），事件通知（`OnDamageTaken`/`OnDeath` 事件供 UI 和其他系统订阅）。

---

### 需求 2：敌人生成管理器（EnemySpawner）

**用户故事：** 作为一名开发者，我希望有一个敌人生成管理器，以便能够在运行时从对象池中动态生成敌人、控制刷怪逻辑，而不需要手动在场景中放置。

#### 验收标准

1. WHEN 场景中存在 EnemySpawner 组件 THEN 该 Spawner SHALL 持有对敌人 Prefab 的引用，并通过 `GameObjectPool` 进行对象池管理（遵循项目禁止运行时 Instantiate/Destroy 的原则）。

2. WHEN Spawner 激活 THEN 系统 SHALL 按照配置（初始数量、刷新间隔、最大同时存活数）在指定的生成点位置生成敌人实例。

3. WHEN 从对象池取出敌人实例 THEN 系统 SHALL 调用 `EnemyBrain.ResetBrain(spawnPosition)` 重置 AI 状态，确保敌人从 IdleState 开始全新的行为循环。

4. WHEN 敌人死亡（`EnemyEntity.OnDeath` 事件触发）THEN Spawner SHALL 更新存活计数，并在条件满足时从对象池中补充新的敌人。

5. IF Spawner 配置了多个生成点 THEN 系统 SHALL 支持在不同 Transform 位置生成敌人。

6. WHEN EnemySpawner 需要配置 THEN 所有数值参数（最大存活数、刷怪间隔、预热数量、池上限）SHALL 通过 ScriptableObject 或 `[SerializeField]` Inspector 字段暴露，不允许 hardcode。

---

### 需求 3：场景测试配置与完整闭环验证

**用户故事：** 作为一名开发者，我希望在 SampleScene 中配置好至少一个可运行的敌人，以便验证从 感知→追击→攻击→玩家受伤→敌人受伤→敌人死亡 的完整战斗循环。

#### 验收标准

1. WHEN 进入 Play Mode THEN 场景中 SHALL 存在至少一个由 EnemySpawner 管理的 Rusher 敌人实例（或直接放置的 Prefab 实例作为临时方案）。

2. WHEN 敌人生成后 THEN 系统 SHALL 从 IdleState 开始运行 → 玩家进入视锥范围后转 ChaseState → 进入攻击范围后进入 EngageState（Telegraph → Attack → Recovery）→ 玩家脱离 LeashRange 后进入 ReturnState → 回到 Idle，**整个状态流转 SHALL 可通过 Editor 上方的 OnGUI Debug 标签观察到**。

3. WHEN 玩家子弹击中敌人 THEN 敌人 SHALL 受到伤害（HP 减少）、显示受击闪白反馈、受到击退力。

4. WHEN 敌人攻击命中玩家 THEN 玩家飞船 SHALL 受到伤害（HP 减少）、显示受击闪白反馈、受到击退力。

5. WHEN 敌人 HP 降至 0 THEN 敌人 SHALL 执行死亡流程（禁用碰撞、停止移动、归还对象池或失活）。

6. IF Physics2D 碰撞矩阵中 Enemy(Layer 8) 与 Player(Layer 6) 之间存在碰撞 THEN 系统 SHALL 确保两者不会产生物理推挤（需要确认碰撞矩阵设置或使用 Trigger 而非实体碰撞）。

7. WHEN 飞船开火 THEN `StarChartController.OnWeaponFired` 事件 SHALL 被广播，敌人感知系统能够通过听觉检测到该事件并做出反应（已实现，此条用于回归验证）。
