# 实施计划：飞船手感增强系统 (Ship Feel Enhancement)

> 基于 [requirements.md](./requirements.md) 生成。所有任务均在 `ProjectArk.Ship` 程序集内完成。

---

- [ ] 1. 扩展 `ShipStatsSO` — 移动曲线与 Dash 参数
   - 在 `Assets/Scripts/Ship/Data/ShipStatsSO.cs` 中新增以下字段（带 `[Header]`/`[Tooltip]`/`[Range]`/`[Min]` Attribute）：
     - **Movement 区域**：`AnimationCurve AccelerationCurve`、`AnimationCurve DecelerationCurve`、`float SharpTurnAngleThreshold`（默认 90°）、`float SharpTurnSpeedPenalty`（默认 0.7）、`float InitialBoostMultiplier`（默认 1.5）、`float InitialBoostDuration`（默认 0.05s）、`float MinMoveSpeedThreshold`（默认 0.1）
     - **Dash 区域**：`float DashSpeed`、`float DashDuration`（默认 0.15s）、`float DashCooldown`（默认 0.3s）、`float DashBufferWindow`（默认 0.15s）、`float DashExitSpeedRatio`（默认 0.5）、`bool DashIFrames`（默认 true）
     - **HitFeedback 区域**：`float HitStopDuration`（默认 0.05s）、`float IFrameDuration`（默认 1.0s）、`float IFrameBlinkInterval`（默认 0.1s）、`float ScreenShakeBaseIntensity`（默认 0.3）、`float ScreenShakeDamageScale`（默认 0.01）
   - 所有新增字段提供 public getter 属性，确保运行时每帧读取 SO 值（热调参）
   - 为 `AccelerationCurve` 和 `DecelerationCurve` 提供合理的默认曲线（`AnimationCurve.EaseInOut(0,0,1,1)`）
   - _需求：1.6, 2.7, 4.5, 6.1, 6.2, 6.4_

- [ ] 2. 创建 `ShipJuiceSettingsSO` — 视觉反馈参数 SO
   - 新建 `Assets/Scripts/Ship/Data/ShipJuiceSettingsSO.cs`
   - 字段包含：`float MoveTiltMaxAngle`（默认 15°）、`float SquashStretchIntensity`（默认 0.15）、`float SquashStretchDuration`（默认 0.1s）、`int DashAfterImageCount`（默认 3）、`float AfterImageFadeDuration`（默认 0.15s）、`float AfterImageAlpha`（默认 0.4）、`float EngineParticleMinSpeed`（NormalizedSpeed 阈值，默认 0.1）
   - 所有字段带 `[Tooltip]`、`[Range]`/`[Min]` 约束
   - _需求：5.6, 6.2, 6.4_

- [ ] 3. 重写 `ShipMotor` — 曲线驱动移动模型
   - 重构 `Assets/Scripts/Ship/Movement/ShipMotor.cs` 中的 `HandleMovement()` 方法：
     - 用 `AnimationCurve.Evaluate()` 替代线性 `MoveTowards`，t 值基于当前加/减速进度
     - 实现急转弯检测：计算 `Vector2.Angle(currentVelocity.normalized, input.normalized)`，超过 `SharpTurnAngleThreshold` 时应用 `SharpTurnSpeedPenalty` 速度惩罚
     - 实现首帧加速：追踪 `_timeSinceStartedMoving`，在 `InitialBoostDuration` 内对加速度应用 `InitialBoostMultiplier`
     - 新增 `float _accelerationProgress`（0→1）和 `float _decelerationProgress`（0→1）内部状态追踪曲线采样位置
   - 保留现有 `ApplyImpulse()` 接口和 `OnSpeedChanged` 事件，确保向后兼容
   - 新增 `bool IsDashing` 属性供 Dash 系统控制（Dash 激活时跳过正常移动逻辑）
   - 新增 `void SetVelocityOverride(Vector2 velocity)` 和 `void ClearVelocityOverride()` 方法供 Dash 使用
   - _需求：1.1, 1.2, 1.3, 1.4, 1.5, 7（向后兼容）_

- [ ] 4. 实现 `ShipDash` 组件 — Dash/Boost 核心逻辑
   - 新建 `Assets/Scripts/Ship/Movement/ShipDash.cs`
   - 职责：监听 InputHandler 的 Dash 输入 → 执行 Dash → 管理冷却 → 管理输入缓冲
   - Dash 执行流程（`async UniTaskVoid`）：
     1. 确定 Dash 方向（移动输入方向优先，无输入时用 ShipAiming.FacingDirection）
     2. 广播 `OnDashStarted` 事件
     3. 设置 `ShipMotor.IsDashing = true`，通过 `SetVelocityOverride` 施加 Dash 速度
     4. 如果 `DashIFrames` 开启，通知 ShipHealth 进入无敌状态
     5. 等待 `DashDuration`（UniTask.Delay）
     6. Dash 结束：计算出口速度 = `DashSpeed * DashExitSpeedRatio`，通过 `ClearVelocityOverride` + `ApplyImpulse` 保留动量
     7. `ShipMotor.IsDashing = false`，广播 `OnDashEnded`
     8. 启动冷却计时器
   - 输入缓冲：记录最后一次 Dash 按键时间，冷却结束时检查是否在 `DashBufferWindow` 内
   - 使用 `destroyCancellationToken` 管理异步生命周期
   - 读取 InputHandler 中的 Dash Action（需要在 InputHandler 中新增 Dash 事件暴露）
   - _需求：2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 2.8, 3.1_

- [ ] 5. 扩展 `InputHandler` — Dash 输入 + 输入缓冲基础设施
   - 在 `Assets/Scripts/Ship/Input/InputHandler.cs` 中：
     - 新增 `_dashAction` 字段，从 "Ship" ActionMap 中查找 "Dash" Action
     - 新增 `event Action OnDashPressed` 事件
     - 在 `OnEnable`/`OnDisable` 中正确注册/取消 Dash Action 回调
   - 新建 `Assets/Scripts/Ship/Input/InputBuffer.cs`：
     - 通用输入缓冲工具类：`Record(string actionName)` 记录时间戳，`Consume(string actionName, float windowSeconds)` 消费缓冲
     - `Tick()` 每帧清理过期缓冲
     - 供 ShipDash 和其他系统使用
   - _需求：3.1, 3.2, 3.3, 3.4_

- [ ] 6. 强化 `ShipHealth` — 无敌帧 + HitStop + 屏幕震动
   - 修改 `Assets/Scripts/Ship/Combat/ShipHealth.cs`：
     - 新增 `bool IsInvulnerable` 属性（private set），在 `TakeDamage()` 入口处检查，为 true 时提前返回
     - 新增 `SetInvulnerable(bool value)` 公共方法（供 ShipDash 调用）
     - 实现无敌帧系统：受伤后 `IsInvulnerable = true`，持续 `IFrameDuration`，期间精灵 alpha 以 `IFrameBlinkInterval` 频率交替闪烁（`async UniTaskVoid IFrameBlinkAsync()`）
     - 取消当前闪烁的 CancellationTokenSource 管理（与已有的 `_flashCts` 模式一致）
   - 新建 `Assets/Scripts/Ship/Combat/HitFeedbackService.cs`：
     - 静态或单例服务，提供 `TriggerHitStop(float duration)` 和 `TriggerScreenShake(float intensity)` 方法
     - HitStop 实现：`Time.timeScale = 0` → `UniTask.Delay(duration, ignoreTimeScale: true)` → `Time.timeScale = 1`
     - ScreenShake 实现：通过 Cinemachine `CinemachineImpulseSource.GenerateImpulse(intensity)` 触发
     - 在 `ShipHealth.TakeDamage()` 中调用 `HitFeedbackService`
   - _需求：4.1, 4.2, 4.3, 4.4, 4.5, 2.2_

- [ ] 7. 实现 `ShipVisualJuice` — 移动倾斜 + Squash/Stretch
   - 新建 `Assets/Scripts/Ship/VFX/ShipVisualJuice.cs`
   - 该组件挂在飞船 GameObject 上，操控一个**视觉子物体**（SpriteRenderer 所在的 child Transform），不影响物理 Transform 和瞄准
   - 功能：
     - **移动倾斜**：根据横向移动分量（垂直于飞船朝向）计算倾斜角度，`Mathf.LerpAngle` 平滑过渡到目标角度，最大 `MoveTiltMaxAngle` 度
     - **Squash/Stretch**：监听 `ShipMotor.OnSpeedChanged`，检测速度突变（加速/减速）→ 用 PrimeTween 对子物体 localScale 做短暂 squash 或 stretch 动画
     - **Dash Stretch**：监听 `ShipDash.OnDashStarted` → 在 Dash 方向上做强烈 stretch
   - 所有参数从 `ShipJuiceSettingsSO` 读取
   - _需求：5.1, 5.3, 5.4_

- [ ] 8. 实现 `ShipEngineVFX` — 引擎尾焰粒子
   - 新建 `Assets/Scripts/Ship/VFX/ShipEngineVFX.cs`
   - 在飞船子物体上挂载 ParticleSystem（引擎尾焰），该组件控制其发射率和尺寸：
     - 订阅 `ShipMotor.OnSpeedChanged` → `NormalizedSpeed` < 阈值时停止发射，> 阈值时 `emissionRate` 和 `startSize` 与速度成正比
     - Dash 激活时临时提高粒子发射率/尺寸（爆发效果），Dash 结束后恢复
   - 参数从 `ShipJuiceSettingsSO` 读取
   - _需求：5.2_

- [ ] 9. 实现 `DashAfterImage` — Dash 残影系统
   - 新建 `Assets/Scripts/Ship/VFX/DashAfterImage.cs`（残影单体组件，挂在池化 Prefab 上）
     - 接收初始位置/旋转/Sprite → 设置 SpriteRenderer → alpha 从 `AfterImageAlpha` 淡出到 0 → 淡出完成后回收入对象池
     - 淡出使用 PrimeTween（`Tween.Alpha`）
   - 新建 `Assets/Scripts/Ship/VFX/DashAfterImageSpawner.cs`（挂在飞船上）
     - 订阅 `ShipDash.OnDashStarted` → 在 Dash 持续期间每隔固定间距生成一个残影（从 PoolManager 获取）
     - 残影数量由 `ShipJuiceSettingsSO.DashAfterImageCount` 控制
   - 残影 Prefab 通过 PoolManager 预热和回收（遵循对象池规范）
   - _需求：5.5, 技术约束 2（对象池）_

- [ ] 10. 集成测试 + 默认 SO 资产创建
   - 创建默认 SO 资产文件：
     - `Assets/_Data/Ship/DefaultShipStats.asset`（更新已有资产，补充新字段的默认值）
     - `Assets/_Data/Ship/DefaultShipJuiceSettings.asset`（新建）
   - 验证清单（Play Mode 逐条验证）：
     - ✅ 加速曲线：起步有推力爆发感，首帧加速可感知（需求 1.1, 1.5）
     - ✅ 减速曲线：松手后有滑行惯性，减速非线性（需求 1.2）
     - ✅ 急转弯：90°+ 转向时明显减速（需求 1.3, 1.4）
     - ✅ Dash：按下后快速冲刺，方向正确，冷却 0.3s 可再次使用（需求 2.1, 2.4）
     - ✅ Dash 无敌：冲刺期间不受伤（需求 2.2）
     - ✅ Dash 动量保留：冲刺结束后有惯性延续（需求 2.3）
     - ✅ Dash 输入缓冲：冷却结束前按 Dash 可自动执行（需求 2.5, 3.1）
     - ✅ HitStop：受伤瞬间有短暂顿帧（需求 4.1）
     - ✅ 屏幕震动：受伤时摄像机抖动，强度随伤害缩放（需求 4.2）
     - ✅ 无敌帧：受伤后 1s 内不再受伤，精灵闪烁（需求 4.3, 4.4）
     - ✅ 移动倾斜：横移时飞船视觉倾斜（需求 5.1）
     - ✅ 引擎粒子：移动时有尾焰，速度越快越强（需求 5.2）
     - ✅ Dash 残影：冲刺时身后留下半透明残影（需求 5.5）
     - ✅ 热调参：Play Mode 中修改 SO 参数即时生效（需求 6.1）
     - ✅ `ApplyImpulse()` 仍正常工作（技术约束 7）
   - 记录 ImplementationLog.md
   - _需求：6.1, 6.3, 技术约束 7_
