# 需求文档：敌人 AI 基础框架 (Enemy AI Foundation)

## 引言

本文档定义《静默方舟》Phase 1 敌人 AI 基础框架的需求。目标是构建一套 **数据驱动、模块化** 的敌人系统，使用 **分层状态机 (HFSM)** 作为大脑层的实现方式，配合 GDD 中定义的三层架构（躯壳 Body / 大脑 Brain / 导演 Director）。

本阶段聚焦于：
1. 敌人实体的物理层（生命、移动、受击反馈）
2. HFSM 框架（通用状态机 + 类魂攻击子状态机）
3. 基础感知系统（视觉 + 听觉）
4. 数据驱动配置（EnemyStatsSO）
5. 第一个可玩原型：莽夫型 (The Rusher)

**Phase 1 不包含**：导演系统（攻击令牌）、阵营系统、恐惧值、多种敌人原型。这些属于后续阶段。

### 架构总览

```
┌─────────────────────────────────────────────────┐
│                 EnemyDirector                    │  ← Phase 2
│        (攻击令牌池, 全局协调, 声波传播)              │
└──────────────────────┬──────────────────────────┘
                       │ 协调
┌──────────────────────▼──────────────────────────┐
│              AI Brain (HFSM)                     │  ← Phase 1
│  ┌─────────────────────────────────────────┐    │
│  │  外层状态机 (战术层)                       │    │
│  │  Idle ──→ Chase ──→ Engage ──→ Return   │    │
│  └─────────────────┬───────────────────────┘    │
│                    │                             │
│  ┌─────────────────▼───────────────────────┐    │
│  │  内层状态机 (攻击层)                       │    │
│  │  Telegraph → Attack → Recovery          │    │
│  └─────────────────────────────────────────┘    │
└──────────────────────┬──────────────────────────┘
                       │ 指令
┌──────────────────────▼──────────────────────────┐
│           EnemyEntity (躯壳层)                    │  ← Phase 1
│  移动执行 · HP/韧性 · 受击反馈 · 对象池            │
└─────────────────────────────────────────────────┘
```

---

## 需求

### 需求 1：敌人数据配置 (EnemyStatsSO)

**用户故事：** 作为一名策划，我希望通过 ScriptableObject 配置敌人的所有数值参数，以便在不修改代码的情况下调整敌人的属性和行为。

#### 验收标准

1.1 WHEN 策划在 Unity 编辑器中选择 Create > ProjectArk > Enemy > EnemyStats THEN 系统 SHALL 创建一个新的 `EnemyStatsSO` 资产实例，包含以下可编辑字段：
   - **身份**：`EnemyName` (string), `EnemyID` (string)
   - **生命**：`MaxHP` (float), `Poise`/韧性 (float)
   - **移动**：`MoveSpeed` (float), `RotationSpeed` (float)
   - **攻击**：`AttackDamage` (float), `AttackRange` (float), `AttackCooldown` (float)
   - **攻击阶段时长**：`TelegraphDuration` (float), `AttackActiveDuration` (float), `RecoveryDuration` (float)
   - **感知**：`SightRange` (float), `SightAngle` (float), `HearingRange` (float)
   - **脱战**：`LeashRange` (float，超过此距离放弃追击), `MemoryDuration` (float，失去视线后记忆玩家位置的时长)
   - **视觉**：`HitFlashDuration` (float), `BaseColor` (Color)

1.2 IF `EnemyStatsSO` 资产中某个数值字段为非法值（如 MaxHP ≤ 0）THEN 编辑器 SHALL 在 Inspector 中显示警告提示（通过 `[Min]` 属性或自定义 Editor）。

---

### 需求 2：敌人实体躯壳层 (EnemyEntity)

**用户故事：** 作为一名程序员，我希望有一个统一的敌人实体组件处理生命值、移动执行和受击反馈，以便大脑层只需发出高层指令，不关心物理细节。

#### 验收标准

2.1 WHEN 敌人 Prefab 被实例化（或从对象池取出）THEN `EnemyEntity` SHALL 从引用的 `EnemyStatsSO` 读取配置并初始化所有运行时状态（HP = MaxHP，Poise = MaxPoise 等）。

2.2 WHEN `EnemyEntity.TakeDamage(float damage, Vector2 knockbackDir, float knockbackForce)` 被调用 THEN 系统 SHALL：
   - 扣除 HP
   - 对 Rigidbody2D 施加击退力
   - 播放受击闪白（SpriteRenderer 颜色脉冲）
   - 触发 `OnDamageTaken` 事件（传递 damage, currentHP）
   - IF HP ≤ 0 THEN 触发 `OnDeath` 事件并执行死亡流程（禁用碰撞、播放死亡效果、回池/销毁）

2.3 WHEN `EnemyEntity.MoveTo(Vector2 direction)` 被调用 THEN 系统 SHALL 以 `EnemyStatsSO.MoveSpeed` 的速度设置 Rigidbody2D 的 velocity，方向为 `direction`（已归一化）。

2.4 WHEN `EnemyEntity.StopMovement()` 被调用 THEN 系统 SHALL 将 Rigidbody2D velocity 设为 Vector2.zero。

2.5 `EnemyEntity` SHALL 实现 `IPoolable` 接口，在 `OnGetFromPool` 中重置所有运行时状态，在 `OnReturnToPool` 中清理引用。

2.6 `EnemyEntity` SHALL 实现 `IDamageable` 接口，以便子弹系统（`Projectile.OnTriggerEnter2D`）通过此接口造成伤害，无需知道具体的敌人类型。

---

### 需求 3：伤害接口 (IDamageable)

**用户故事：** 作为一名程序员，我希望有一个通用的受伤接口，以便子弹、激光、震荡波等各种攻击源都能统一地对敌人造成伤害。

#### 验收标准

3.1 WHEN 任何攻击源需要对目标造成伤害 THEN 它 SHALL 通过 `IDamageable` 接口调用，接口定义：
   - `void TakeDamage(float damage, Vector2 knockbackDirection, float knockbackForce)`
   - `bool IsAlive { get; }`

3.2 WHEN `Projectile` 碰撞到带有 `IDamageable` 组件的对象 THEN `Projectile.OnTriggerEnter2D` SHALL 调用 `IDamageable.TakeDamage()`，传入子弹的 Damage 和 Knockback 参数。

3.3 WHEN `LaserBeam` 命中带有 `IDamageable` 组件的对象 THEN `LaserBeam` SHALL 调用 `IDamageable.TakeDamage()`。

3.4 WHEN `EchoWave` 扩展碰撞到带有 `IDamageable` 组件的对象 THEN `EchoWave` SHALL 调用 `IDamageable.TakeDamage()`。

---

### 需求 4：分层状态机框架 (HFSM Framework)

**用户故事：** 作为一名程序员，我希望有一个轻量级的通用 HFSM 框架，以便所有敌人 AI 大脑都能基于它构建，支持外层战术状态和内层攻击子状态机。

#### 验收标准

4.1 WHEN 创建一个新状态 THEN 程序员 SHALL 继承自 `IState` 接口，该接口定义：
   - `void OnEnter()` — 进入状态时调用
   - `void OnUpdate(float deltaTime)` — 每帧调用
   - `void OnExit()` — 离开状态时调用

4.2 WHEN `StateMachine.Tick(float deltaTime)` 被调用 THEN 系统 SHALL 调用当前状态的 `OnUpdate(deltaTime)`。

4.3 WHEN `StateMachine.TransitionTo(IState newState)` 被调用 THEN 系统 SHALL 依次执行：当前状态 `OnExit()` → 更新当前状态引用 → 新状态 `OnEnter()`。

4.4 WHEN 一个状态需要包含子状态机（如 Engage 状态内部有 Telegraph/Attack/Recovery）THEN 该状态 SHALL 能够持有一个子 `StateMachine` 实例，在自身的 `OnUpdate` 中 Tick 子状态机。

4.5 `StateMachine` SHALL 暴露 `CurrentState` 属性（只读），用于调试和外部查询。

---

### 需求 5：基础感知系统 (Perception System)

**用户故事：** 作为一名策划，我希望敌人能通过视觉和听觉发现玩家，以便创造出"被发现"的紧张感和利用隐蔽的策略空间。

#### 验收标准

5.1 **视觉感知**：WHEN 玩家进入敌人的 `SightRange` 范围内 AND 玩家位于敌人面朝方向的 `SightAngle`（扇形半角）范围内 AND 敌人与玩家之间没有墙体遮挡（2D Raycast 检测） THEN 感知系统 SHALL 将 `CanSeePlayer` 设为 true 并记录 `LastKnownPlayerPosition`。

5.2 **听觉感知**：WHEN 玩家开火（`StarChartController` 触发开火事件）THEN 系统 SHALL 广播一个携带位置和半径的听觉事件。IF 敌人在听觉事件的传播半径内 THEN 敌人的 `HasHeardPlayer` SHALL 被设为 true，并记录声源位置为 `LastKnownPlayerPosition`。

5.3 **记忆衰减**：WHEN 敌人失去视线（`CanSeePlayer` 变为 false）AND 经过 `MemoryDuration` 秒后仍未重新获得视线或听觉 THEN 感知系统 SHALL 清除 `LastKnownPlayerPosition` 并将 `HasTarget` 设为 false。

5.4 感知检测 SHALL 不使用每帧 `Physics2D.OverlapCircle`，而是采用：
   - 视觉：定频检测（如每 0.2 秒一次）以节省性能
   - 听觉：事件订阅 + 距离校验模式

5.5 感知系统 SHALL 将检测结果暴露为公共属性，供 HFSM 状态的转移条件查询：
   - `bool HasTarget` — 是否有有效目标
   - `Vector2 LastKnownPlayerPosition` — 最后已知玩家位置
   - `bool CanSeePlayer` — 当前帧是否能看到玩家
   - `float DistanceToTarget` — 到目标的距离

---

### 需求 6：基础 AI 状态集（莽夫原型 The Rusher）

**用户故事：** 作为一名玩家，我希望遇到一种会直线冲向我并近距离攻击的基础敌人，以便在早期关卡中学习躲避和反击的核心节奏。

#### 验收标准

6.1 **IdleState**：WHEN 敌人处于 Idle 状态 AND `HasTarget` 为 false THEN 敌人 SHALL 保持静止（或执行简单的待机动画）。WHEN `HasTarget` 变为 true THEN 系统 SHALL 转移到 Chase 状态。

6.2 **ChaseState**：WHEN 敌人处于 Chase 状态 THEN 敌人 SHALL 以 `MoveSpeed` 直线移动向 `LastKnownPlayerPosition`。
   - IF `DistanceToTarget < AttackRange` THEN 转移到 Engage 状态。
   - IF `DistanceToTarget > LeashRange` THEN 转移到 Return 状态。
   - IF `HasTarget` 变为 false（记忆衰减完毕）THEN 转移到 Return 状态。

6.3 **EngageState**（包含攻击子状态机）：
   - **TelegraphSubState**：WHEN 进入前摇阶段 THEN 敌人 SHALL 停止移动，播放前摇视觉信号（如闪红/蓄力动画），持续 `TelegraphDuration` 秒后自动转移到 AttackSubState。
   - **AttackSubState**：WHEN 进入出招阶段 THEN 敌人 SHALL 生成伤害判定区域（Hitbox），持续 `AttackActiveDuration` 秒。在此阶段敌人不可转向（Commitment）。THEN 自动转移到 RecoverySubState。
   - **RecoverySubState**：WHEN 进入恢复阶段 THEN 敌人 SHALL 处于硬直中（不移动、不攻击），持续 `RecoveryDuration` 秒。此为玩家反击窗口。THEN 退出 Engage，根据距离返回 Chase 或 Idle。

6.4 **ReturnState**：WHEN 敌人处于 Return 状态 THEN 敌人 SHALL 移动回出生点（Spawn Position）。WHEN 到达出生点（距离 < 0.5）THEN 转移到 Idle 状态并恢复满血。

---

### 需求 7：子弹系统集成

**用户故事：** 作为一名玩家，我希望我的子弹能实际对敌人造成伤害，以便验证完整的"射击→命中→敌人反应"战斗循环。

#### 验收标准

7.1 WHEN `Projectile.OnTriggerEnter2D` 检测到碰撞目标 AND 目标 GameObject 上存在 `IDamageable` 组件 THEN `Projectile` SHALL 调用 `target.TakeDamage(_damage, direction, _knockback)`，其中 direction 为子弹飞行方向。

7.2 WHEN `LaserBeam` 的 Raycast 命中目标 AND 目标上存在 `IDamageable` THEN `LaserBeam` SHALL 调用 `TakeDamage()`。

7.3 WHEN `EchoWave` 的扩展碰撞体接触目标 AND 目标上存在 `IDamageable` THEN `EchoWave` SHALL 调用 `TakeDamage()`（每个目标每次波动仅命中一次）。

7.4 子弹碰撞不改变已有的 Layer 过滤逻辑（仍忽略 Player 层和自身层）。敌人 SHALL 位于 "Enemy" Layer 上。

---

### 需求 8：敌人 Prefab 与场景集成

**用户故事：** 作为一名策划，我希望能在场景中放置敌人并立即测试，以便快速验证AI行为和战斗手感。

#### 验收标准

8.1 WHEN 在 Unity 编辑器中将敌人 Prefab 拖入场景 THEN Prefab SHALL 包含以下组件：
   - `EnemyEntity`（躯壳层）
   - `EnemyBrain`（大脑层，持有 HFSM）
   - `EnemyPerception`（感知系统）
   - `Rigidbody2D`（Dynamic, Gravity Scale = 0, Freeze Rotation Z）
   - `Collider2D`（Circle 或 Capsule）
   - `SpriteRenderer`

8.2 `EnemyBrain` SHALL 通过 `[SerializeField]` 引用一个 `EnemyStatsSO` 资产，在 Awake 时构建 HFSM 并注册所有状态和转移条件。

8.3 WHEN 进入 Play Mode THEN 场景中的敌人 SHALL 自动开始在 Idle 状态运行，等待玩家进入感知范围。

---

## 边界情况与技术约束

### 性能约束
- 感知系统的视觉检测频率不超过 5Hz（每 0.2 秒一次），避免大量敌人时的物理查询开销
- 听觉事件采用广播 + 距离过滤，不使用物理碰撞体
- 敌人实体在运行时必须使用对象池管理（复用已有 `PoolManager`）

### 架构约束
- 遵循项目命名空间规范 `ProjectArk.Enemy` [[memory:am1psy6v]]
- 所有数值配置通过 `EnemyStatsSO` (ScriptableObject)，禁止 hardcode [[memory:u9is78gd]]
- 系统间通过 C# event 通信（`OnDamageTaken`, `OnDeath` 等），禁止跨系统直接引用
- HFSM 框架为纯 C# 类（非 MonoBehaviour），可单元测试

### Layer 设置
- 敌人使用 "Enemy" Layer
- 子弹碰撞矩阵需配置：Player 层子弹与 Enemy 层碰撞，忽略 Player 层自身
