# 实施计划 — Enemy AI 系统集成

## 前置上下文

- `IDamageable` 接口已定义于 `Scripts/Combat/IDamageable.cs`，签名：`TakeDamage(float, Vector2, float)` + `IsAlive`
- `EnemyEntity` 已完整实现 `IDamageable` + `IPoolable`，可作为玩家受伤组件的参考模板
- `ShipMotor`（`Scripts/Ship/Movement/ShipMotor.cs`）拥有 `Rigidbody2D`，已有 `ApplyImpulse(Vector2)` 方法可用于击退
- `ShipStatsSO`（`Scripts/Ship/Data/ShipStatsSO.cs`）目前只有移动参数（MoveSpeed/Acceleration/Deceleration/RotationSpeed），需扩展 HP 字段
- `GameObjectPool` 使用 `Get(Vector3, Quaternion)` / `Return(GameObject)` API，自动处理 `IPoolable` 回调和 `PoolReference` 挂载
- `EnemyBrain.ResetBrain(Vector2)` 已实现，可在对象池取出时重置 AI 状态
- `AttackSubState.TryHitPlayer()` 使用 `Physics2D.OverlapCircleAll` + `LayerMask("Player")` 检测玩家并调用 `GetComponent<IDamageable>()`
- `EnemyEntity.OnDeath` 事件已定义，死亡时自动通过 `PoolReference.ReturnToPool()` 回池

---

## 任务清单

- [ ] 1. 扩展 `ShipStatsSO` 添加生存相关字段
   - 在 `ShipStatsSO.cs` 中新增 `[Header("Survival")]` 区域，添加 `_maxHP`（float，默认 100）、`_hitFlashDuration`（float，默认 0.1）字段及其公共属性
   - 不影响现有移动参数，向后兼容 `DefaultShipStats.asset`（新字段使用 Unity 默认值）
   - _需求：1.7_

- [ ] 2. 创建 `ShipHealth` 组件实现 `IDamageable` 接口
   - 新建 `Scripts/Ship/Combat/ShipHealth.cs`，命名空间 `ProjectArk.Ship`
   - 实现 `IDamageable.TakeDamage(float, Vector2, float)` 和 `IDamageable.IsAlive`
   - 内部维护 `_currentHP`，从 `ShipStatsSO.MaxHP` 读取初始值
   - `TakeDamage` 逻辑：死亡状态直接 return → 扣减 HP → 通过 `Rigidbody2D.AddForce(Impulse)` 施加击退 → 协程受击闪白（参考 `EnemyEntity.HitFlashCoroutine` 实现） → 触发 `OnDamageTaken` 事件 → HP ≤ 0 调用 `Die()`
   - `Die()` 逻辑：标记 `_isDead = true`、禁用 `InputHandler`（停止玩家控制）、触发 `OnDeath` 事件（暂不实现重生/GameOver）
   - 使用 `[RequireComponent(typeof(Rigidbody2D))]`，通过 `GetComponent` 获取 `SpriteRenderer`
   - 暴露 `OnDamageTaken(float damage, float currentHP)` 和 `OnDeath` 两个 `Action` 事件
   - _需求：1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7_

- [ ] 3. 在飞船 Prefab 上挂载 `ShipHealth` 组件
   - 在 SampleScene 中找到飞船 GameObject，添加 `ShipHealth` 组件
   - 确保飞船 GameObject 的 Layer 为 "Player"（Layer 6），使 `AttackSubState` 的 `LayerMask("Player")` 能正确检测到
   - 确认飞船已有 `SpriteRenderer` 供闪白反馈使用
   - _需求：1.1, 1.3_

- [ ] 4. 创建 `EnemySpawner` 组件
   - 新建 `Scripts/Combat/Enemy/EnemySpawner.cs`，命名空间 `ProjectArk.Combat.Enemy`
   - Inspector 暴露字段：`_enemyPrefab`（GameObject）、`_spawnPoints`（Transform[]）、`_maxAlive`（int，默认 3）、`_spawnInterval`（float，默认 5）、`_initialSpawnCount`（int，默认 1）、`_poolPrewarmCount`（int，默认 5）、`_poolMaxSize`（int，默认 10）
   - `Start()` 中创建 `GameObjectPool`（使用自身 transform 作为 parent），然后调用初始生成逻辑
   - 提供 `SpawnEnemy()` 方法：从池中 `Get()` → 设置位置（从 `_spawnPoints` 中随机选取或循环选取）→ 调用 `EnemyBrain.ResetBrain(position)` → 订阅 `EnemyEntity.OnDeath` 事件 → 更新 `_aliveCount`
   - 敌人死亡回调：`_aliveCount--`，启动协程在 `_spawnInterval` 秒后尝试补充生成（如果 `_aliveCount < _maxAlive`）
   - 注意：`EnemyEntity.OnReturnToPool()` 会清空事件订阅者，因此每次从池中取出时需重新订阅 `OnDeath`
   - _需求：2.1, 2.2, 2.3, 2.4, 2.5, 2.6_

- [ ] 5. 配置 Physics2D 碰撞矩阵确保 Enemy/Player 物理交互正确
   - 在 `ProjectSettings/Physics2DSettings.asset` 中确认：
     - Enemy(Layer 8) vs Player(Layer 6)：**关闭**碰撞（防止物理推挤；攻击通过 OverlapCircle 检测，不依赖碰撞）
     - Enemy(Layer 8) vs PlayerProjectile(Layer 7)：**开启**碰撞（子弹需命中敌人）
     - EnemyProjectile(如未来有 Layer 9) vs Player(Layer 6)：预留
   - 确认 Enemy Prefab 上 Collider2D 的 `isTrigger` 设置与子弹的 `OnTriggerEnter2D` 检测机制一致
   - _需求：3.6_

- [ ] 6. 在 SampleScene 中配置 EnemySpawner 并设置生成点
   - 在场景中创建空 GameObject "EnemySpawner"，挂载 `EnemySpawner` 组件
   - 创建 1~2 个空 GameObject 作为 SpawnPoint，放置在玩家飞船视野可及的合理位置
   - 将 `Enemy_Rusher.prefab` 拖入 Spawner 的 `_enemyPrefab` 字段
   - 配置 `_initialSpawnCount = 1`、`_maxAlive = 2`、`_spawnInterval = 5`（初期测试用低值）
   - _需求：2.2, 3.1_

- [ ] 7. 为 `Projectile.cs` 补充对 Enemy Layer 的伤害对接验证
   - 确认 `Projectile.OnTriggerEnter2D` 中对 Layer 8 (Enemy) 的碰撞响应：`GetComponent<IDamageable>()?.TakeDamage(...)` 路径完整可用
   - 如果当前代码中存在 Layer 过滤（之前修复子弹自碰撞时添加的），确保 Enemy layer 不被误过滤
   - 验证 `_damage`、`_knockbackForce` 等参数来自 `StarCoreSO` 并正确传递
   - _需求：3.3_

- [ ] 8. 集成测试与 Debug 验证
   - Play Mode 下验证完整战斗循环：
     1. 敌人从 IdleState 开始 → 玩家进入视锥 → ChaseState → 进入攻击范围 → EngageState（Telegraph → Attack → Recovery）
     2. 敌人 Attack 阶段命中玩家 → `ShipHealth.TakeDamage()` 被调用 → 飞船闪白 + HP 扣减
     3. 玩家射击敌人 → `EnemyEntity.TakeDamage()` → 敌人闪白 + HP 扣减
     4. 敌人 HP 归零 → 死亡流程 → 回池 → Spawner 计数更新 → 延迟后补充新敌人
     5. 玩家脱离 LeashRange → ReturnState → 回到 Idle
   - 通过 `EnemyBrain.OnGUI` Debug 标签观察状态流转是否正确
   - 通过 Console 日志确认无 NullReferenceException 或其他运行时错误
   - 修复集成过程中发现的任何问题
   - _需求：3.1, 3.2, 3.3, 3.4, 3.5, 3.7_
