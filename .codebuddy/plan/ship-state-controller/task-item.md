# 实施计划：ShipStateController — 状态机驱动的飞船参数系统

- [ ] 1. 新建 `ShipShipState` 枚举和 `ShipStateData` 数据结构
   - 在 `Assets/Scripts/Ship/Data/` 下新建 `ShipShipState.cs`，定义枚举值 Normal=0 / Dash=2 / Boost=3 / MainAttack=6 / MainAttackFire=10（对齐 GG PlayerShipState）
   - 在同目录新建 `ShipStateData.cs`（`[System.Serializable]`），包含所有物理参数字段（linearDrag / angularDrag / moveAcceleration / maxMoveSpeed / angularAcceleration / maxRotationSpeed / minTime / animatorTrigger / colliders）
   - 实现 `Apply(Rigidbody2D rb, ShipMotor motor, ShipAiming aiming, Animator animator)` 和 `Disable()` 方法
   - _需求：1.1、1.2、1.3、1.4、2.1_

- [ ] 2. 重构 `ShipStatsSO.cs` — 新增状态数据数组
   - 在 `ShipStatsSO` 中新增 `[SerializeField] ShipStateData[] _stateDataArray` 字段
   - 新增 `GetStateData(ShipShipState state)` 方法，找不到时回退 Normal 并输出 `Debug.LogWarning`
   - 保留现有所有独立参数字段（`_angularAcceleration` 等）不删除，作为向后兼容 fallback
   - _需求：4.1、4.2、4.3_

- [ ] 3. 新建 `ShipStateController.cs` — 状态机核心 MonoBehaviour
   - 在 `Assets/Scripts/Ship/` 下新建 `ShipStateController.cs`
   - 实现 `TryToState(ShipShipState)`（检查 `isCanChangeState` 锁 + `minTime`）和 `ToStateForce(ShipShipState)`（忽略锁，立即切换）
   - 实现 `IsInState(ShipShipState)` 查询方法和 `CurrentStateData` 属性
   - 暴露 `event Action<ShipShipState, ShipShipState> OnStateChanged` 事件
   - Awake 时从 `ShipStatsSO` 读取状态数组，应用 Normal 状态
   - 幂等性：切换到当前状态时直接 return
   - _需求：3.1、3.2、3.3、3.4、3.5、3.6_

- [ ] 4. 修改 `ShipBoost.cs` — 通过 Controller 切换 Boost 状态
   - 获取 `ShipStateController` 引用（`[RequireComponent]` 或 `GetComponent`）
   - 在 `ExecuteBoostAsync()` 开始时调用 `_stateController.ToStateForce(ShipShipState.Boost)`
   - 在 Boost 结束时调用 `_stateController.ToStateForce(ShipShipState.Normal)`
   - 移除 `ShipBoost` 内部直接修改 `Rigidbody2D.drag` / `maxMoveSpeed` 的代码
   - _需求：5.2、5.4_

- [ ] 5. 修改 `ShipDash.cs` — 通过 Controller 切换 Dash 状态
   - 在 `ExecuteDashAsync()` 开始时调用 `_stateController.ToStateForce(ShipShipState.Dash)`
   - 在 Dash 结束时调用 `_stateController.ToStateForce(ShipShipState.Normal)`
   - 移除 `ShipDash` 内部直接修改碰撞体启用状态的代码（改由 `ShipStateData.Apply/Disable` 管理）
   - _需求：5.1_

- [ ] 6. 修改 `ShipAiming.cs` — 从 Controller 读取角加速度参数
   - 将 `_stats.AngularAcceleration` / `_stats.BoostAngularAcceleration` 的读取改为从 `_stateController.CurrentStateData.angularAcceleration` 读取
   - 若 `_stateController` 为 null，回退到直接读 `_stats` 字段（向后兼容）
   - _需求：5.3、5.5_

- [ ] 7. 修改 `ShipMotor.cs` — 从 Controller 读取速度和阻力参数
   - 将 `linearDrag` / `maxMoveSpeed` 的读取改为从 `_stateController.CurrentStateData` 读取
   - 移除 `EnterBoostState()` / `ExitBoostState()` 方法（参数切换已由 Controller 统一管理）
   - 若 `_stateController` 为 null，回退到直接读 `_stats` 字段（向后兼容）
   - _需求：5.4、5.5_

- [ ] 8. 修改 `ShipView.cs` — 订阅 `OnStateChanged` 统一驱动 VFX
   - 在 `OnEnable` 订阅 `_stateController.OnStateChanged`，在 `OnDisable` 取消订阅
   - 根据新状态分发 VFX：Boost→BoostStart 视觉，Normal（从 Boost 退出）→BoostEnd 视觉，Dash→Dash 视觉
   - 移除对 `ShipBoost.OnBoostStarted` / `ShipBoost.OnBoostEnded` / `ShipDash.OnDashStarted` 等旧事件的订阅
   - _需求：6.1、6.2、6.3、6.4_

- [ ] 9. 更新 `DefaultShipStats.asset` — 配置三个状态的默认数值
   - 在 `_stateDataArray` 中添加 Normal / Boost / Dash 三条 `ShipStateData` 记录
   - Normal：linearDrag=3, moveAcceleration=20, maxMoveSpeed=8, angularAcceleration=800, maxRotationSpeed=360
   - Boost：linearDrag=2.5, maxMoveSpeed=9, angularAcceleration=400, maxRotationSpeed=360, minTime=0.2
   - Dash：linearDrag=1.7, moveAcceleration=12, maxMoveSpeed=4, angularAcceleration=200, maxRotationSpeed=180, minTime=0.15
   - _需求：7.1、7.2、7.3_

- [ ] 10. 将 `ShipStateController` 挂载到 `Ship.prefab` 并验收
   - 在 `Ship.prefab` 的根节点添加 `ShipStateController` 组件，绑定 `ShipStatsSO` 引用
   - 将 Dash 状态的 `colliders` 数组绑定到飞船的主碰撞体（无敌帧期间禁用）
   - Play Mode 验收：Normal→Boost 切换时 linearDrag/maxMoveSpeed 正确变化；Dash 时碰撞体禁用；VFX 正确触发
   - _需求：3.1、1.2、6.1_
