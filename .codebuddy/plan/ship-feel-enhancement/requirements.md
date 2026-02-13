# 需求文档：飞船手感增强系统 (Ship Feel Enhancement)

## 引言

本功能旨在将飞船的操控手感从当前的"基础可用"水平提升至"令人上瘾"的水平。参考 Celeste（1000+ 行移动代码的精细调控）、Minishoot' Adventures（同为 Twin-Stick 飞船射击）、Hyper Light Drifter（Dash 手感标杆）、以及 Galactic Glitch（Twin-Stick 飞船射击 + 物理驱动手感 + Boost/Dodge 双用途设计），对飞船的移动模型、闪避系统、受击反馈和视觉 Juice 进行全面升级。

### 当前状态分析

| 维度 | 现状 | 问题 |
|------|------|------|
| **移动模型** | 线性加减速（`Vector2.MoveTowards`），3 个参数 | 手感平淡，缺乏曲线感，方向切换无惯性，没有转弯减速 |
| **瞄准旋转** | 匀速 `MoveTowardsAngle`，1 个参数 | 缺少旋转补偿（急转弯时的速度惩罚/奖励）|
| **闪避/Dash** | 输入绑定已有 (`ShipActions.inputactions`)，代码**未实现** | 玩家没有核心防御/位移技能 |
| **受击反馈** | 白闪 + 击退力，无无敌帧 | 连续受击无法反应，缺少屏幕震动/顿帧 |
| **视觉 Juice** | 几乎为零 | 无速度拖尾、无移动倾斜、无转向挤压拉伸、无引擎粒子 |
| **可调参数** | ShipStatsSO 仅 6 个参数 | 远不足以支撑精细的手感调控 |

### 参考游戏锚定

- **移动模型** → Minishoot' Adventures + Galactic Glitch：飞船加速有"推力感"，减速有"滑行感"，急转弯有轻微减速。Galactic Glitch 的飞船有明显的物理惯性和动量感，高速移动时转向有「漂移」质感
- **Dash/Boost** → Hyper Light Drifter + Galactic Glitch：Hyper Light Drifter 的短距离无敌冲刺+明确冷却为基础框架；Galactic Glitch 的 Boost/Dodge 双用途设计为关键参考——Dash 不仅是防御闪避，也是进攻性位移工具（快速接近/拉开距离/重新定位），冷却极短（"mercifully brief cooldown"）鼓励频繁使用而非保守憋技能
- **受击反馈** → Hollow Knight：顿帧(HitStop) + 屏幕震动 + 无敌帧 + 击退
- **Juice** → Celeste + Galactic Glitch：角色移动时的倾斜/挤压拉伸、粒子拖尾、速度线。Galactic Glitch 在高速运动和碰撞时有非常强的粒子和视觉冲击反馈

---

## 需求

### 需求 1：高级移动曲线系统

**用户故事：** 作为一名玩家，我希望飞船的加速、减速和转向有丰富的动态手感（如推力感、惯性滑行、转弯减速），以便操控飞船时感觉像在驾驶一艘真实的太空飞行器而非一个数字方块。

#### 验收标准

1. WHEN 玩家按下移动键 THEN ShipMotor SHALL 使用可配置的 `AnimationCurve` 进行非线性加速（替代当前的线性 `MoveTowards`），使起步有"推力爆发"感。
2. WHEN 玩家释放移动键 THEN ShipMotor SHALL 使用独立的减速曲线进行非线性减速，使滑行有"阻力渐增"的自然感。
3. WHEN 玩家急转弯（当前移动方向与输入方向夹角 > 配置阈值，如 90°）THEN ShipMotor SHALL 应用一个速度惩罚乘数（如 0.7x），使急转弯时飞船明显减速后再加速回来，增强操控的重量感。
4. WHEN 玩家进行小幅转向（夹角 < 配置阈值）THEN ShipMotor SHALL 平滑过渡方向而不施加速度惩罚，保持流畅感。
5. WHEN ShipMotor 的速度从零开始增加 THEN 系统 SHALL 在前几帧（可配置的 `InitialBoostDuration`，如 0.05s）施加一个额外的加速度倍率（如 1.5x），实现 Celeste 风格的"首帧加速"让起步更灵敏。
6. IF `ShipStatsSO` 中的移动相关参数少于 10 个可调项 THEN 系统 SHALL 扩展参数列表以包含：加速曲线、减速曲线、急转弯阈值角度、急转弯速度惩罚、首帧加速倍率、首帧加速持续时间、最小移动速度阈值。

### 需求 2：Dash/Boost 闪避系统

**用户故事：** 作为一名玩家，我希望飞船拥有一个短距离无敌冲刺技能（同时兼具 Boost 加速功能），以便在密集弹幕中有精确的防御手段，也能用作进攻性位移（快速接近/拉开距离/重新定位），闪避操作本身也提供极强的爽快感。

> **设计理念（参考 Galactic Glitch）**：Dash 不仅仅是"紧急逃命按钮"，更是一个高频使用的核心机动技能。极短的冷却鼓励玩家积极使用——用 Dash 冲进敌群侧翼、用 Dash 穿越弹幕后立刻反击、用 Dash 快速重新定位到有利射击角度。

#### 验收标准

1. WHEN 玩家按下 Dash 按钮 AND Dash 不在冷却中 THEN 系统 SHALL 沿当前移动输入方向（若无输入则沿飞船朝向）执行一次快速冲刺，持续 `DashDuration`（默认 ~0.15s），冲刺期间飞船速度为 `DashSpeed`（默认为 MoveSpeed 的 3 倍）。
2. WHEN Dash 激活期间 THEN ShipHealth SHALL 进入无敌状态（`IsInvulnerable = true`），忽略所有伤害和击退。
3. WHEN Dash 结束 THEN ShipMotor SHALL 保留一部分 Dash 动量作为"出 Dash 速度"（可配置的 `DashExitSpeedRatio`，默认 ~0.5，即 Dash 结束后以 Dash 速度的 50% 继续移动），然后使用减速曲线过渡回正常速度，使 Dash 感觉有"惯性延续"而非硬切停。
4. WHEN Dash 结束 THEN 系统 SHALL 启动 `DashCooldown`（默认 ~0.3s，参考 Galactic Glitch 的极短冷却）冷却计时器，冷却期间无法再次 Dash。
5. IF 玩家在 Dash 冷却结束前 `DashBufferWindow`（默认 ~0.15s）内按下 Dash THEN 系统 SHALL 缓冲该输入，在冷却结束后自动执行 Dash（输入缓冲）。
6. WHEN Dash 激活 THEN 系统 SHALL 播放视觉反馈：残影/拖尾效果、速度线粒子，并广播 `OnDashStarted` 和 `OnDashEnded` 事件供其他系统订阅。
7. IF ShipStatsSO THEN 系统 SHALL 包含以下 Dash 参数：`DashSpeed`、`DashDuration`、`DashCooldown`、`DashBufferWindow`、`DashExitSpeedRatio`、`DashIFrames`（是否提供无敌帧的开关）。
8. WHEN Dash 激活 AND 飞船在 Dash 途中与碰撞体接触 THEN 系统 SHALL 不中断 Dash 移动（Dash 期间忽略普通碰撞减速，但不穿墙），保证 Dash 的流畅感和可靠性。

### 需求 3：输入缓冲与宽容机制

**用户故事：** 作为一名玩家，我希望游戏对我的操作输入有一定的容错性（如提前按键仍能生效），以便在高强度战斗中不会因为几帧的时机差异而导致操作失败的挫败感。

#### 验收标准

1. WHEN 玩家在 Dash 冷却结束前的 `DashBufferWindow` 时间窗口内按下 Dash THEN 系统 SHALL 缓冲该输入并在冷却结束时自动执行。
2. WHEN 玩家在受击硬直/无敌帧结束前按下移动键 THEN 系统 SHALL 缓冲移动意图，硬直结束后立即恢复移动而不丢失输入。
3. WHEN 玩家在任何不可操作状态（Dash 中/受击中/过场中）结束前的 `InputBufferWindow`（默认 ~0.1s）内按下开火键 THEN 系统 SHALL 缓冲该输入并在状态解除后立即执行。
4. IF 缓冲窗口超时 THEN 系统 SHALL 自动清除缓冲的输入，不执行过期的操作。

### 需求 4：受击反馈强化

**用户故事：** 作为一名玩家，我希望飞船受到攻击时有强烈的视觉/听觉/物理反馈（顿帧、屏幕震动、无敌帧），以便我能清楚感知"我被打了"，同时有时间反应。

#### 验收标准

1. WHEN 飞船受到伤害 THEN 系统 SHALL 触发 HitStop（时间暂停），暂停 `HitStopDuration`（默认 ~0.05s）帧，让玩家明确感知到命中。
2. WHEN 飞船受到伤害 THEN 系统 SHALL 触发屏幕震动（Camera Shake），震动强度与伤害量成正比（可配置基础强度和伤害缩放系数），通过 Cinemachine Impulse 实现。
3. WHEN 飞船受到伤害 THEN ShipHealth SHALL 进入无敌帧状态，持续 `IFrameDuration`（默认 ~1.0s），期间飞船精灵闪烁（alpha 交替 0.3/1.0，频率可配置）以提供视觉提示。
4. WHEN 无敌帧激活期间飞船再次受到伤害 THEN ShipHealth SHALL 忽略该伤害（`TakeDamage` 提前返回）。
5. IF ShipStatsSO THEN 系统 SHALL 包含以下受击参数：`HitStopDuration`、`IFrameDuration`、`IFrameBlinkInterval`、`ScreenShakeBaseIntensity`、`ScreenShakeDamageScale`。

### 需求 5：移动 Juice 视觉反馈

**用户故事：** 作为一名玩家，我希望飞船在移动时有丰富的视觉反馈（倾斜、粒子拖尾、挤压拉伸），以便仅通过视觉就能感受到速度和加速度，增强驾驶的沉浸感。

#### 验收标准

1. WHEN 飞船处于移动状态 THEN 系统 SHALL 根据移动方向对飞船精灵施加轻微的视觉倾斜（向移动方向倾斜，最大角度可配置，如 ±15°），通过单独的视觉子物体旋转实现（不影响物理旋转和瞄准）。
2. WHEN 飞船速度 > 最小阈值 THEN 系统 SHALL 激活引擎尾焰粒子效果，粒子发射率和尺寸随 `NormalizedSpeed` 缩放。
3. WHEN 飞船速度从高速突然降低（如松手减速或撞墙）THEN 系统 SHALL 对飞船精灵施加短暂的 Squash 效果（X 轴压缩、Y 轴拉伸），增强减速感。
4. WHEN 飞船从静止开始加速 THEN 系统 SHALL 对飞船精灵施加短暂的 Stretch 效果（X 轴拉伸、Y 轴压缩），增强起步的动态感。
5. WHEN Dash 激活 THEN 系统 SHALL 在飞船身后生成 2~4 个短暂的残影（半透明副本），残影快速淡出。
6. IF 所有视觉效果参数 THEN 系统 SHALL 通过 `ShipJuiceSettingsSO`（新的 ScriptableObject）集中管理，与 `ShipStatsSO` 分离，方便美术独立调参。

### 需求 6：参数化与可调性

**用户故事：** 作为一名开发者/设计师，我希望所有手感相关参数都集中在 ScriptableObject 中并带有清晰的 Tooltip，以便无需修改代码即可快速迭代手感调整。

#### 验收标准

1. WHEN 开发者修改 ShipStatsSO 中的任意手感参数 THEN 系统 SHALL 在下一帧/下一物理步即刻生效（运行时热调参），无需重启 Play Mode。
2. IF ShipStatsSO 中新增了移动曲线（AnimationCurve）参数 THEN 系统 SHALL 在 Inspector 中以可视化曲线编辑器呈现，方便直观调整。
3. WHEN 任意手感参数被修改 THEN 系统 SHALL 不影响其他模块的正常运行（参数变化被限制在 Ship 模块内部）。
4. IF 手感参数可能导致异常行为（如 DashDuration <= 0）THEN 系统 SHALL 使用 `[Min]` 或 `[Range]` Attribute 在 Inspector 层面阻止非法值。

---

## 技术约束

1. **异步纪律**：Dash 计时器、无敌帧闪烁等使用 `async UniTaskVoid` + `destroyCancellationToken`，不使用 Coroutine。
2. **对象池**：Dash 残影使用对象池管理，不在运行时 Instantiate/Destroy。
3. **事件卫生**：所有新增事件（OnDashStarted/OnDashEnded/OnIFrameStarted/OnIFrameEnded）遵循 OnDisable 取消订阅规范。
4. **数据驱动**：所有数值通过 SO 配置，禁止 hardcode。
5. **程序集边界**：所有新增代码位于 `ProjectArk.Ship` 程序集内，通过 Core 层事件与其他模块通信。
6. **性能**：HitStop 通过 `Time.timeScale` 实现时需注意 UI 动画使用 `unscaledTime`；或使用 per-object 暂停方案避免影响全局。
7. **向后兼容**：已有的 `ShipMotor.ApplyImpulse()` 接口保留，新增功能不破坏现有武器后坐力等使用者。

## 不在范围内（Out of Scope）

- 飞船升级/属性成长系统（属于 StarChart 模块）
- 音效实现（GDD 已有规划，但本次聚焦代码手感层）
- 特定关卡环境对移动的影响（如逆风/低重力，属于 Level 模块）
- 联机同步相关的手感补偿
- Galactic Glitch 的重力抓取/物理投掷机制（属于独立的新能力系统，不在本次手感增强范围内）
