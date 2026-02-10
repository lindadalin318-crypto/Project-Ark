# 实施计划：敌人 AI 基础框架 (Enemy AI Foundation)

---

- [ ] 1. 创建 `IDamageable` 接口和 `EnemyStatsSO` 数据结构
   - 在 `Scripts/Combat/` 下创建 `IDamageable.cs` 接口，定义 `TakeDamage(float, Vector2, float)` 和 `bool IsAlive`
   - 在 `Scripts/Combat/Enemy/` 下创建 `EnemyStatsSO.cs`（ScriptableObject），包含身份、生命、移动、攻击、攻击阶段、感知、脱战、表现等全部字段，使用 `[Header]` 分组和 `[Min]` 约束非法值
   - 添加 `[CreateAssetMenu(menuName = "ProjectArk/Enemy/EnemyStats")]`
   - _需求：1.1, 1.2, 3.1_

- [ ] 2. 创建 HFSM 状态机框架（纯 C# 类）
   - 在 `Scripts/Combat/Enemy/FSM/` 下创建 `IState.cs` 接口（`OnEnter`, `OnUpdate`, `OnExit`）
   - 创建 `StateMachine.cs` 纯 C# 类，实现 `Tick(float)`, `TransitionTo(IState)`, 只读 `CurrentState` 属性
   - 确保 `TransitionTo` 按顺序执行 `OnExit → 更新引用 → OnEnter`
   - 状态机支持嵌套：任何 `IState` 实现类可内部持有子 `StateMachine` 并在 `OnUpdate` 中 Tick 它
   - _需求：4.1, 4.2, 4.3, 4.4, 4.5_

- [ ] 3. 创建 `EnemyEntity` 躯壳层（Body）
   - 在 `Scripts/Combat/Enemy/` 下创建 `EnemyEntity.cs`（MonoBehaviour），实现 `IDamageable` 和 `IPoolable`
   - 通过 `[SerializeField]` 引用 `EnemyStatsSO`，Awake 时读取配置初始化运行时状态（HP, Poise 等）
   - 实现 `TakeDamage()`：扣血、Rigidbody2D 击退、SpriteRenderer 闪白协程、触发 `OnDamageTaken` 事件、HP≤0 触发 `OnDeath` + 死亡流程（禁用碰撞、回池）
   - 实现 `MoveTo(Vector2)` 和 `StopMovement()`：驱动 Rigidbody2D velocity
   - 实现 `OnGetFromPool()` / `OnReturnToPool()`：重置 HP、启用碰撞、清理状态
   - 添加 `[RequireComponent(typeof(Rigidbody2D))]` 和 `[RequireComponent(typeof(Collider2D))]`
   - _需求：2.1, 2.2, 2.3, 2.4, 2.5, 2.6_

- [ ] 4. 创建 `EnemyPerception` 感知系统
   - 在 `Scripts/Combat/Enemy/` 下创建 `EnemyPerception.cs`（MonoBehaviour）
   - 视觉感知：定频 0.2s 执行扇形检测（距离 + 角度 + Raycast LoS），更新 `CanSeePlayer` 和 `LastKnownPlayerPosition`
   - 听觉感知：订阅 `StarChartController` 的开火事件（需在 `StarChartController` 中添加 `public static event Action<Vector2, float> OnWeaponFired`），距离校验后更新 `HasHeardPlayer` 和 `LastKnownPlayerPosition`
   - 记忆衰减：失去视线后 `MemoryDuration` 秒后清除 `LastKnownPlayerPosition`，将 `HasTarget` 设为 false
   - 暴露公共属性：`HasTarget`, `LastKnownPlayerPosition`, `CanSeePlayer`, `DistanceToTarget`
   - _需求：5.1, 5.2, 5.3, 5.4, 5.5_

- [ ] 5. 在 `StarChartController` 中添加开火事件广播
   - 在 `StarChartController` 中添加 `public static event Action<Vector2, float> OnWeaponFired`，参数为（发射位置, 噪音半径）
   - 在每个 Spawn 方法（SpawnMatterProjectile / SpawnLightBeam / SpawnEchoWave / SpawnAnomalyEntity）中调用 `OnWeaponFired?.Invoke(position, noiseRadius)`
   - 噪音半径暂时使用固定值（如 15f），后续可从 SO 配置
   - _需求：5.2_

- [ ] 6. 实现莽夫型基础 AI 状态集
   - 在 `Scripts/Combat/Enemy/States/` 下创建以下状态类（均为纯 C# 类实现 `IState`）：
     - `IdleState`：保持静止，检测 `HasTarget` 为 true 时转移到 ChaseState
     - `ChaseState`：调用 `EnemyEntity.MoveTo()` 直线追击 `LastKnownPlayerPosition`；距离 < AttackRange → EngageState；距离 > LeashRange 或 HasTarget=false → ReturnState
     - `EngageState`：持有内层子状态机，包含 TelegraphSubState / AttackSubState / RecoverySubState
     - `ReturnState`：移动回出生点，到达后转 IdleState 并恢复满血
   - 所有状态通过构造函数接收 `EnemyBrain` 引用以访问 Entity 和 Perception
   - _需求：6.1, 6.2, 6.3, 6.4_

- [ ] 7. 实现 Engage 内层攻击子状态机（信号-窗口模型）
   - `TelegraphSubState`：停止移动、播放前摇视觉信号（SpriteRenderer 颜色变红），计时 `TelegraphDuration` 后转 AttackSubState
   - `AttackSubState`：生成伤害 Hitbox（临时 OverlapCircle 或碰撞体），不可转向（Commitment），计时 `AttackActiveDuration` 后转 RecoverySubState
   - `RecoverySubState`：硬直不动，计时 `RecoveryDuration` 后退出 EngageState，由外层根据距离决定转 Chase 或 Idle
   - Hitbox 使用 `Physics2D.OverlapCircle` + LayerMask 检测玩家，命中调用玩家的 `IDamageable`（若已实现）或 `Debug.Log`
   - _需求：6.3_

- [ ] 8. 创建 `EnemyBrain` 大脑层（组装 HFSM）
   - 在 `Scripts/Combat/Enemy/` 下创建 `EnemyBrain.cs`（MonoBehaviour），引用 `EnemyEntity` 和 `EnemyPerception`
   - 在 `Start()` 中构建外层 StateMachine，注册 IdleState / ChaseState / EngageState / ReturnState
   - 在 `Update()` 中调用 `_stateMachine.Tick(Time.deltaTime)`
   - 记录出生点 `_spawnPosition` 供 ReturnState 使用
   - 暴露公共属性供状态访问：`Entity`, `Perception`, `Stats`, `SpawnPosition`, `StateMachine`
   - _需求：8.2, 8.3_

- [ ] 9. 集成子弹系统 — 将三种投射物的 placeholder 替换为 `IDamageable` 调用
   - `Projectile.OnTriggerEnter2D`：替换 TODO 注释为 `other.GetComponent<IDamageable>()?.TakeDamage(_damage, direction, _knockback)`
   - `LaserBeam.Fire`：替换 placeholder `Debug.Log` 为 `hit.collider.GetComponent<IDamageable>()?.TakeDamage(_damage, dir, _knockback)`
   - `EchoWave.OnTriggerEnter2D`：替换 placeholder `Debug.Log` 为 `other.GetComponent<IDamageable>()?.TakeDamage(_damage, direction, _knockback)`
   - 保留 Layer 过滤逻辑不变，确保不影响现有碰撞行为
   - _需求：7.1, 7.2, 7.3, 7.4_

- [ ] 10. 创建莽夫型敌人 Prefab + EnemyStatsSO 资产 + 场景集成测试
   - 通过代码（或 Editor 脚本）创建 `EnemyStats_Rusher.asset`（ScriptableObject），配置莽夫型参数：高 MoveSpeed、短 TelegraphDuration、短 AttackRange、长 RecoveryDuration
   - 创建敌人 Prefab（代码生成或手动），包含组件：EnemyEntity + EnemyBrain + EnemyPerception + Rigidbody2D (Dynamic, GravityScale=0, FreezeRotationZ) + CircleCollider2D + SpriteRenderer
   - 设置 Prefab 所在 Layer 为 "Enemy"
   - 确保进入 Play Mode 后敌人从 Idle 开始，能感知玩家、追击、攻击、脱战回归
   - 更新 `ImplementationLog.md` 记录本次全部修改
   - _需求：8.1, 8.2, 8.3_
