# 需求文档：ShipStateController — 状态机驱动的飞船参数系统

## 引言

Project Ark 当前的飞船系统（`ShipMotor` / `ShipAiming` / `ShipBoost` / `ShipDash`）使用**硬编码的状态分支**：Boost 参数散落在 `ShipBoost.cs` 里，Dash 参数散落在 `ShipDash.cs` 里，`ShipStatsSO` 用多个独立字段分别存储各状态参数。这与 Galactic Glitch（GG）的原版架构存在根本差异。

GG 的真实架构（经 `dump.cs` + `Player.prefab` 反编译确认）：
- `PlayerShipState` 枚举定义所有状态（Blue/Dodge/Boost/MainAttack/MainAttackFire 等）
- `StateData`（Serializable class）封装每个状态的完整物理参数包（linearDrag / moveAcceleration / maxMoveSpeed / angularAcceleration / maxRotationSpeed / colliders / animatorBool / minTime / maxTime）
- `Player` 持有 `List<StateData> states`，通过 `TryToState()` / `ToStateForce()` 切换状态，调用 `StateData.Apply()` 一次性写入所有参数

**目标**：在 Project Ark 中实现一个独立的 `ShipStateController` MonoBehaviour，完整复刻 GG 的状态机驱动参数系统，并与现有的 `ShipMotor` / `ShipAiming` / `ShipBoost` / `ShipDash` / `ShipView` 对接，同时保持 Project Ark 的 Twin-Stick 控制模型不变。

**需要的状态（排除 GG 中 Project Ark 暂不实现的功能）**：
- `Normal`（对应 GG Blue=0）：默认飞行状态
- `Dash`（对应 GG Dodge=2）：闪避无敌帧期间
- `Boost`（对应 GG Boost=3）：Boost 加速期间
- `MainAttack`（对应 GG MainAttack=6）：武器蓄力/攻击期间（预留，当前可配置但不强制触发）
- `MainAttackFire`（对应 GG MainAttackFire=10）：武器开火瞬间（预留）

---

## 需求

### 需求 1：ShipStateData — 可序列化的状态参数包

**用户故事：** 作为开发者，我希望每个飞船状态的所有物理参数都封装在一个可序列化的数据结构中，以便在 Inspector 中直观配置，并在运行时一次性应用。

#### 验收标准

1. WHEN 创建 `ShipStateData` 时 THEN 系统 SHALL 包含以下字段：`state`（ShipShipState 枚举）、`animatorTrigger`（string，驱动 Animator）、`minTime`（float，最短持续时间）、`linearDrag`、`angularDrag`、`moveAcceleration`、`maxMoveSpeed`、`angularAcceleration`、`maxRotationSpeed`、`colliders`（Collider2D[]，Dash 无敌帧期间禁用）
2. WHEN `ShipStateData.Apply()` 被调用 THEN 系统 SHALL 将所有物理参数写入 `Rigidbody2D` 和 `ShipMotor` / `ShipAiming`，并根据 `isCollidersEnable` 参数启用/禁用 `colliders` 数组中的碰撞体
3. IF `animatorTrigger` 不为空 THEN 系统 SHALL 在 Apply 时触发对应的 Animator 参数（SetBool 或 SetTrigger）
4. WHEN `ShipStateData.Disable()` 被调用 THEN 系统 SHALL 恢复碰撞体为启用状态（对应 GG `StateData.Disable()`）

---

### 需求 2：ShipShipState 枚举

**用户故事：** 作为开发者，我希望有一个清晰的枚举定义所有飞船状态，以便在代码中安全地引用状态，避免魔法字符串。

#### 验收标准

1. WHEN 定义 `ShipShipState` 枚举 THEN 系统 SHALL 包含：`Normal = 0`、`Dash = 2`、`Boost = 3`、`MainAttack = 6`、`MainAttackFire = 10`（数值与 GG `PlayerShipState` 完全对齐，便于日后对照）
2. IF 未来需要新增状态 THEN 系统 SHALL 只需在枚举中添加新值并在 `DefaultShipStats.asset` 中配置对应 `ShipStateData`，无需修改状态机逻辑

---

### 需求 3：ShipStateController — 状态机核心

**用户故事：** 作为开发者，我希望有一个独立的 `ShipStateController` MonoBehaviour 管理所有状态切换逻辑，以便各子系统（ShipBoost/ShipDash/武器系统）只需调用统一接口，不再各自维护状态分支。

#### 验收标准

1. WHEN `ShipStateController` 初始化 THEN 系统 SHALL 从 `ShipStatsSO`（或独立的 `ShipStateDataSO`）读取 `ShipStateData[]` 数组，并在 Awake 时应用 `Normal` 状态
2. WHEN `TryToState(ShipShipState)` 被调用 THEN 系统 SHALL 检查 `isCanChangeState` 锁和当前状态的 `minTime`，若满足条件则切换状态并调用 `StateData.Apply()`
3. WHEN `ToStateForce(ShipShipState)` 被调用 THEN 系统 SHALL 忽略所有锁，立即切换状态（对应 GG `ToStateForce`，用于 Dash/Boost 强制切换）
4. WHEN 状态切换发生 THEN 系统 SHALL 触发 `OnStateChanged(ShipShipState prev, ShipShipState next)` 事件，供 `ShipView` / VFX 系统订阅
5. WHEN `IsInState(ShipShipState)` 被查询 THEN 系统 SHALL 返回当前状态是否匹配（替代现有的 `IsBoosting` / `IsDashing` 分散属性）
6. IF 请求切换到当前已处于的状态 THEN 系统 SHALL 忽略该请求（幂等性）

---

### 需求 4：ShipStatsSO 重构 — 状态数据数组化

**用户故事：** 作为开发者，我希望 `ShipStatsSO` 包含一个 `ShipStateData[]` 数组替代现有的分散参数字段，以便在 Inspector 中统一管理所有状态的参数，并与 GG 的 `StateData` 结构完全对齐。

#### 验收标准

1. WHEN 重构 `ShipStatsSO` THEN 系统 SHALL 新增 `ShipStateData[] stateDataArray` 字段，包含 Normal / Dash / Boost 三个状态的默认配置（数值完全对齐 GG 原版）
2. WHEN 保留向后兼容 THEN 系统 SHALL 保留现有的独立参数字段（`_angularAcceleration` 等）作为 `Normal` 状态的 fallback，直到所有调用方迁移完成后再移除
3. IF `stateDataArray` 中找不到某个状态 THEN 系统 SHALL 回退到 `Normal` 状态数据并输出 `Debug.LogWarning`

---

### 需求 5：现有子系统对接 — ShipBoost / ShipDash / ShipAiming

**用户故事：** 作为开发者，我希望现有的 `ShipBoost`、`ShipDash`、`ShipAiming` 通过 `ShipStateController` 切换状态，而不是各自直接修改 `Rigidbody2D` 参数，以便消除重复的参数管理代码。

#### 验收标准

1. WHEN `ShipDash.ExecuteDashAsync()` 开始 THEN 系统 SHALL 调用 `ShipStateController.ToStateForce(Dash)`，结束时调用 `ToStateForce(Normal)`
2. WHEN `ShipBoost.ExecuteBoostAsync()` 开始 THEN 系统 SHALL 调用 `ShipStateController.ToStateForce(Boost)`，结束时调用 `ToStateForce(Normal)`
3. WHEN `ShipAiming` 读取角加速度参数 THEN 系统 SHALL 从 `ShipStateController.CurrentStateData` 读取，而非直接读 `_stats.AngularAcceleration` / `_stats.BoostAngularAcceleration`
4. WHEN `ShipMotor` 读取速度上限和 linearDrag THEN 系统 SHALL 从 `ShipStateController.CurrentStateData` 读取，移除 `EnterBoostState()` / `ExitBoostState()` 的手动参数覆盖逻辑
5. IF `ShipStateController` 不存在（向后兼容场景）THEN 系统 SHALL 回退到直接读取 `ShipStatsSO` 字段，不崩溃

---

### 需求 6：ShipView 对接 — 状态驱动 VFX

**用户故事：** 作为开发者，我希望 `ShipView` 订阅 `ShipStateController.OnStateChanged` 事件来驱动 VFX，而不是分别订阅 `ShipBoost.OnBoostStarted` / `ShipDash.OnDashStarted` 等多个事件，以便统一 VFX 触发入口。

#### 验收标准

1. WHEN `ShipStateController.OnStateChanged` 触发且新状态为 `Boost` THEN `ShipView` SHALL 执行 Boost 视觉效果（glow ramp up / trail / thruster pulse）
2. WHEN `ShipStateController.OnStateChanged` 触发且新状态为 `Normal`（从 Boost 退出）THEN `ShipView` SHALL 执行 Boost 结束视觉效果（glow ramp down / trail stop）
3. WHEN `ShipStateController.OnStateChanged` 触发且新状态为 `Dash` THEN `ShipView` SHALL 执行 Dash 视觉效果（i-frame flicker / dodge sprite ghost）
4. IF `ShipView` 同时订阅了旧事件（`OnBoostStarted` 等）THEN 系统 SHALL 在迁移完成后移除旧订阅，避免重复触发

---

### 需求 7：默认状态数据配置（GG 原版数值）

**用户故事：** 作为开发者，我希望 `DefaultShipStats.asset` 中预配置的状态数据完全对齐 GG 原版数值，以便开箱即用地复刻 GG Glitch 飞船的手感。

#### 验收标准

1. WHEN 配置 `Normal` 状态 THEN 系统 SHALL 使用：linearDrag=3, moveAcceleration=20（mass=1 适配值）, maxMoveSpeed=8, angularAcceleration=800（mass=1 适配值）, maxRotationSpeed=360（mass=1 适配值）
2. WHEN 配置 `Boost` 状态 THEN 系统 SHALL 使用：linearDrag=2.5, maxMoveSpeed=9, angularAcceleration=400（mass=1 适配值）, maxRotationSpeed=360, minTime=0.2s
3. WHEN 配置 `Dash` 状态 THEN 系统 SHALL 使用：linearDrag=1.7, moveAcceleration=12, maxMoveSpeed=4, angularAcceleration=200（mass=1 适配值）, maxRotationSpeed=180, minTime=0.15s（无敌帧时长）
4. IF 参数需要调整 THEN 系统 SHALL 只需在 Inspector 中修改 `ShipStateData` 数组，无需改代码
