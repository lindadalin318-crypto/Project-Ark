# Implementation Log — Project Ark

---

## MS1: 飞船基础移动 + Twin-Stick 瞄准

### 1. 项目结构搭建 — 2026-02-07 01:49

**新建文件：**
- `Assets/Scripts/Core/ProjectArk.Core.asmdef` — Core 程序集，根模块，无外部依赖
- `Assets/Scripts/Ship/ProjectArk.Ship.asmdef` — Ship 程序集，引用 Core + Unity.InputSystem
- `Assets/Scripts/UI/ProjectArk.UI.asmdef` — UI 程序集，引用 Core

**目的：** 用 Assembly Definition 划分模块边界，强制解耦，加速编译。

**技术：** Unity Assembly Definition，模块化架构。

---

### 2. ShipStatsSO — 飞船数据配置 — 2026-02-07 01:49

**新建文件：**
- `Assets/Scripts/Ship/Data/ShipStatsSO.cs`
- `Assets/_Data/Ship/DefaultShipStats.asset`（SO 实例）

**内容：** ScriptableObject 数据容器，暴露 4 个手感参数：
- `MoveSpeed` = 12（最大移动速度）
- `Acceleration` = 45（加速度）
- `Deceleration` = 25（减速度，低于加速度以产生惯性滑行）
- `RotationSpeed` = 720（转向速度 °/s）

**目的：** 数据驱动，所有数值集中配置，无需改代码即可调参。

**技术：** ScriptableObject, `[CreateAssetMenu]`, 数据驱动模式。

---

### 3. ShipActions.inputactions — 输入配置 — 2026-02-07 01:49

**新建文件：**
- `Assets/Input/ShipActions.inputactions`

**内容：** 两个 Action Map：
- **Ship Map：** Move（WASD/左摇杆）、AimPosition（鼠标位置）、AimStick（右摇杆）、Fire、Dash、Interact（Hold 0.3s）、Pause
- **UI Map：** Navigate、Submit、Cancel、Point、Click、ScrollWheel

**控制方案：** Keyboard&Mouse / Gamepad 双方案

**目的：** 统一管理所有输入绑定，支持键鼠和手柄。

**技术：** New Input System 1.18.0, 2DVector Composite (mode=2 DigitalNormalized 防止对角线超速), StickDeadzone Processor。

---

### 4. InputHandler — 输入适配器 — 2026-02-07 01:49

**新建文件：**
- `Assets/Scripts/Ship/Input/InputHandler.cs`

**内容：** MonoBehaviour，读取 New Input System 并暴露处理后的数据：
- `MoveInput` — 归一化移动向量
- `AimWorldPosition` — 世界空间瞄准位置
- `AimDirection` — 瞄准方向（归一化）
- `HasAimInput` — 是否有瞄准输入
- `OnDeviceSwitched` 事件 — 键鼠/手柄切换通知

**目的：** 隔离输入层，上层系统（ShipMotor、ShipAiming）不直接接触 Input System API。

**技术：** New Input System `InputActionAsset` 手动绑定, `Camera.ScreenToWorldPoint` 鼠标坐标转换, 设备热切换检测。

---

### 5. ShipMotor — 物理移动 — 2026-02-07 01:49

**新建文件：**
- `Assets/Scripts/Ship/Movement/ShipMotor.cs`

**内容：** MonoBehaviour，FixedUpdate 中控制 Rigidbody2D.linearVelocity：
- 有输入时：向目标速度加速（步长 = Acceleration * dt）
- 无输入时：保持方向匀减速（步长 = Deceleration * dt）
- 强制最大速度上限防止超速
- 暴露 `OnSpeedChanged(float normalized)` 事件供 VFX/音效订阅

**Rigidbody2D 配置要求：** Dynamic, Gravity=0, Damping=0, Interpolate, Freeze Rotation Z

**目的：** 实现 Minishoot 风格的不对称惯性移动（快启动、慢滑行）。

**技术：** Rigidbody2D `linearVelocity`（Unity 6 新 API）, `Vector2.MoveTowards`, 不对称加减速。

---

### 6. ShipAiming — 旋转瞄准 — 2026-02-07 01:49

**新建文件：**
- `Assets/Scripts/Ship/Aiming/ShipAiming.cs`

**内容：** MonoBehaviour，LateUpdate 中旋转飞船朝向瞄准方向：
- `Atan2` 计算目标角度，-90° 偏移对齐 Sprite 朝上（+Y = forward）
- `RotationSpeed > 0` 时用 `MoveTowardsAngle` 匀速旋转
- `RotationSpeed ≈ 0` 时瞬间转向
- 暴露 `OnAimAngleChanged(float degrees)` 事件

**目的：** Twin-Stick 瞄准，飞船独立于移动方向旋转朝向目标。

**技术：** `Mathf.MoveTowardsAngle`（匀速旋转，非 Slerp）, `Quaternion.Euler`, LateUpdate 保证在移动后执行。

---

### 7. Ship Prefab 组装 — 2026-02-07 17:03

**新建文件：**
- `Assets/_Prefabs/Ship/Ship.prefab`

**内容：** 飞船预制体，挂载组件：Rigidbody2D + InputHandler + ShipMotor + ShipAiming

**目的：** 场景中可直接实例化的完整飞船对象。

---

## 杂项

### Art 文件夹结构 — 2026-02-07 17:30

**新建目录：**
- `Assets/Art/Sprites/{Ship, Enemies, Projectiles, Environment, Items, UI}/`
- `Assets/Art/Animations/{Ship, Enemies, Effects}/`
- `Assets/Art/Tiles/{Tilesets, Palettes}/`
- `Assets/Art/VFX/`
- `Assets/Art/Materials/`

**目的：** 为美术资源建立分类存放结构。

---

### Docs 文件夹 — 2026-02-07 17:35

**新建目录：**
- `Docs/Design/` — 系统设计文档
- `Docs/GDD/` — 游戏策划案
- `Docs/ImplementationLog/` — 实现日志（本文件）

**目的：** 项目文档集中管理，与 Assets 分离。

---

## 射击系统核心框架（基于 GDD.md）

### 8. Core 对象池系统 — 2026-02-07 18:00

**新建文件：**
- `Assets/Scripts/Core/Pool/IPoolable.cs` — 池化接口：`OnGetFromPool()`, `OnReturnToPool()`
- `Assets/Scripts/Core/Pool/GameObjectPool.cs` — 预制体池，包装 `UnityEngine.Pool.ObjectPool`
- `Assets/Scripts/Core/Pool/PoolManager.cs` — 单例池注册中心，按 prefab 管理所有池
- `Assets/Scripts/Core/Pool/PoolReference.cs` — 挂在池对象上，提供 `ReturnToPool()` 自回收

**内容：** 通用对象池基础设施。GameObjectPool 负责实例化/回收/预热，自动调用 IPoolable 回调。PoolManager 提供全局访问点，按 prefab InstanceID 索引。PoolReference 让池中对象能自行回池。

**目的：** 满足架构原则"运行时生成的对象必须使用对象池"，为子弹、特效等提供底层支持。

**技术：** `UnityEngine.Pool.ObjectPool<GameObject>`, Singleton 模式, `DontDestroyOnLoad`, `IPoolable` 接口回调。

---

### 9. InputHandler 扩展 Fire 输入 — 2026-02-07 18:00

**修改文件：**
- `Assets/Scripts/Ship/Input/InputHandler.cs`

**内容：** 添加 Fire action 读取：
- `_fireAction` 字段绑定 `ShipActions` 中的 Fire action
- `IsFireHeld` (bool) 属性 — 射击按钮是否按住
- `OnFirePressed` / `OnFireReleased` 事件
- `performed`/`canceled` 回调自动更新状态

**目的：** 保持 InputHandler 作为唯一输入适配器的职责，WeaponSystem 不直接绑定 Input Action。

**技术：** `InputAction.performed`/`canceled` 回调，事件驱动。

---

### 10. ShipMotor 扩展后坐力接口 — 2026-02-07 18:00

**修改文件：**
- `Assets/Scripts/Ship/Movement/ShipMotor.cs`

**内容：** 添加 `ApplyImpulse(Vector2 impulse)` 公共方法，直接修改 `linearVelocity`。

**目的：** 为武器后坐力提供接口。冲量会被现有减速逻辑自然消化。

**技术：** 直接 `linearVelocity +=`，利用已有减速模型衰减。

---

### 11. Combat 程序集 + WeaponStatsSO — 2026-02-07 18:00

**新建文件：**
- `Assets/Scripts/Combat/ProjectArk.Combat.asmdef` — 战斗程序集，引用 Core + Ship
- `Assets/Scripts/Combat/Data/WeaponStatsSO.cs`

**内容：** 武器数据 ScriptableObject，包含：
- 射击参数：FireRate(5/s), Spread(0°)
- 弹道参数：BaseDamage(10), ProjectileSpeed(20), Lifetime(2.0s), Knockback(1.0)
- 后坐力：RecoilForce(0.5)
- 预制体引用：ProjectilePrefab, MuzzleFlashPrefab, ImpactVFXPrefab
- 音频引用：FireSound, HitSound, FireSoundPitchVariance(0.1)

**目的：** 数据驱动武器配置，未来星图系统修正数值时只需包装此 SO。

**技术：** ScriptableObject, `[CreateAssetMenu]`, 单向程序集依赖 (Combat → Ship → Core)。

---

### 12. FirePoint + IProjectileModifier — 2026-02-07 18:00

**新建文件：**
- `Assets/Scripts/Combat/Weapon/FirePoint.cs` — 炮口位置标记组件
- `Assets/Scripts/Combat/Projectile/IProjectileModifier.cs` — 星图扩展接口

**内容：** FirePoint 是极简标记组件，暴露 `Position` 和 `Direction`。IProjectileModifier 定义三个钩子：`OnProjectileSpawned`/`OnProjectileUpdate`/`OnProjectileHit`。

**目的：** FirePoint 解耦炮口位置与飞船逻辑。IProjectileModifier 为未来星图系统（追踪导弹、回旋镖等）预留扩展点。

**技术：** 标记组件模式, 策略模式接口。

---

### 13. Projectile 子弹系统 — 2026-02-07 18:00

**新建文件：**
- `Assets/Scripts/Combat/Projectile/Projectile.cs`

**内容：** MonoBehaviour + IPoolable，负责：
- `Initialize()` 接收方向/速度/伤害等参数，设置 `linearVelocity`
- `Update()` 倒计时生命周期，调用 modifier 钩子
- `OnTriggerEnter2D` 处理碰撞（回池 + 生成命中特效）
- IPoolable 回调清理 TrailRenderer 和状态
- 暴露 `Direction`/`Speed`/`Damage`/`Knockback` 供 modifier 读写

**目的：** 实体投射物，直线飞行，碰撞即销毁（回池），支持星图行为修改。

**技术：** Kinematic Rigidbody2D + Trigger Collider, `linearVelocity`, IPoolable, PoolReference 自回收。

---

### 14. PooledVFX 池化特效 — 2026-02-07 18:00

**新建文件：**
- `Assets/Scripts/Combat/VFX/PooledVFX.cs`

**内容：** MonoBehaviour + IPoolable，激活时播放 ParticleSystem，播放完毕自动通过 PoolReference 回池。

**目的：** 炮口焰和命中特效的通用池化容器，避免 Instantiate/Destroy。

**技术：** ParticleSystem 生命周期检测, IPoolable + PoolReference 自动回收。

---

### 15. WeaponSystem 武器核心 — 2026-02-07 18:00

**新建文件：**
- `Assets/Scripts/Combat/Weapon/WeaponSystem.cs`

**内容：** 核心射击编排器，Update 中检查 `IsFireHeld + 冷却计时`：
- 从 ShipAiming 读取 `FacingDirection` 作为弹道方向
- 从 FirePoint 读取生成位置
- 应用 Spread 随机偏转
- 从 PoolManager 获取子弹实例并 `Initialize()`
- 生成炮口焰（池化）
- 调用 `ShipMotor.ApplyImpulse()` 施加后坐力
- `AudioSource.PlayOneShot()` + 音调随机化
- 触发 `OnWeaponFired` 事件

**目的：** GDD 核心循环的完整实现——按下即射、按住连发、后坐力、VFX、音效。

**技术：** 冷却计时器模式, 对象池, 事件驱动, `RequireComponent` 依赖声明。

---

## 星图编织系统 Batch 1：热量系统 + 星图数据基础（基于 GDD.md 第二部分）

### 16. Heat 程序集 + HeatStatsSO — 2026-02-08 00:00

**新建文件：**
- `Assets/Scripts/Heat/ProjectArk.Heat.asmdef` — 热量程序集，引用 Core
- `Assets/Scripts/Heat/Data/HeatStatsSO.cs` — 热量配置 SO

**内容：** 新建独立热量模块。HeatStatsSO 包含：MaxHeat(100), CoolingRate(15/s), OverheatDuration(3.0s), OverheatThreshold(1.0)。派生属性 `OverheatHeatValue` = MaxHeat * Threshold。

**目的：** 数据驱动的热量配置，与飞船数据分离，独立程序集保持模块边界。

**技术：** Assembly Definition, ScriptableObject, `[CreateAssetMenu]`。

---

### 17. HeatSystem — 热量状态管理 — 2026-02-08 00:00

**新建文件：**
- `Assets/Scripts/Heat/HeatSystem.cs`

**内容：** MonoBehaviour，挂在 Ship 上，管理热量状态机（Normal ↔ Overheated）：
- `AddHeat(float)` — 增加热量，检查过热触发
- `ReduceHeat(float)` / `ResetHeat()` — 减热/清零（为未来星图部件预留）
- `CanFire()` — 快速检查是否可开火
- Normal 状态：被动散热 `coolingRate * dt`
- Overheated 状态：3 秒倒计时后清零热量恢复
- 事件：`OnHeatChanged(float normalized)`, `OnOverheated`, `OnCooldownComplete`

**目的：** GDD 第 4 节热量/过热机制的完整实现。限制无限射击，增加战术决策。

**技术：** 状态机模式, 事件驱动, `Mathf.Clamp`。

---

### 18. WeaponSystem 热量集成 — 2026-02-08 00:00

**修改文件：**
- `Assets/Scripts/Combat/Data/WeaponStatsSO.cs` — 添加 `_heatCostPerShot`(5f) 字段
- `Assets/Scripts/Combat/Weapon/WeaponSystem.cs` — 集成热量检查和消耗
- `Assets/Scripts/Combat/ProjectArk.Combat.asmdef` — 添加 `ProjectArk.Heat` 引用

**内容：**
- WeaponStatsSO 新增 Heat header 和 `HeatCostPerShot` 属性
- WeaponSystem 添加 `[SerializeField] HeatSystem _heatSystem`（可选依赖）
- Update 中增加 `_heatSystem == null || _heatSystem.CanFire()` 守卫
- Fire 末尾调用 `_heatSystem?.AddHeat(heatCostPerShot)`
- 同时清理了之前的调试诊断日志

**目的：** 射击消耗热量，过热时自动停火。HeatSystem 为可选依赖，不挂则无限射击（向后兼容）。

**技术：** 可选依赖模式（SerializeField + null 检查），null 条件运算符 `?.`。

---

### 19. 星图数据层 — 枚举与基类 — 2026-02-08 00:00

**新建文件：**
- `Assets/Scripts/Combat/StarChart/StarChartEnums.cs` — 所有星图相关枚举
- `Assets/Scripts/Combat/StarChart/StarChartItemSO.cs` — 星图部件抽象基类

**内容：**
- 枚举：`StarChartItemType`(Core/Prism/LightSail/Satellite), `CoreFamily`(Matter/Light/Echo/Anomaly), `PrismFamily`(Fractal/Rheology/Tint), `ModifierOperation`(Add/Multiply), `WeaponStatType`(10 种可修改属性)
- StarChartItemSO：抽象基类，包含 DisplayName, Description, Icon, SlotSize(1-3), HeatCost, 抽象属性 `ItemType`

**目的：** 建立星图系统的类型体系，为所有部件提供统一的数据基类。

**技术：** 枚举类型, 抽象类, ScriptableObject 继承。

---

### 20. StarCoreSO — 星核数据 — 2026-02-08 00:00

**新建文件：**
- `Assets/Scripts/Combat/StarChart/StarCoreSO.cs`

**内容：** 继承 StarChartItemSO，包含完整的发射器参数：Family, ProjectilePrefab, FireRate, BaseDamage, ProjectileSpeed, Lifetime, Spread, Knockback, RecoilForce, VFX/Audio 引用。与现有 WeaponStatsSO 字段对应。

**目的：** 星核是发射源原型，4 个家族（实相/光谱/波动/异象）决定子弹的根本行为方式。Batch 2 管线重构时将替代 WeaponStatsSO。

**技术：** ScriptableObject 继承, `[CreateAssetMenu]`。

---

### 21. StatModifier + PrismSO — 棱镜数据 — 2026-02-08 00:00

**新建文件：**
- `Assets/Scripts/Combat/StarChart/StatModifier.cs` — 数值修改结构体
- `Assets/Scripts/Combat/StarChart/PrismSO.cs` — 棱镜 SO

**内容：** StatModifier 是 `[Serializable]` 结构体，包含 Stat(枚举)、Operation(Add/Multiply)、Value。PrismSO 包含 Family(分形/流变/晕染)、StatModifier 数组、可选的行为注入预制体。

**目的：** 棱镜是"垂直注入"修正器，效果平均分配给同轨道下方的所有星核。运行时应用逻辑在 Batch 2 实现。

**技术：** `[Serializable]` struct, ScriptableObject 继承, 数据驱动修正器模式。

---

### 22. LightSailSO + SatelliteSO — 光帆和伴星数据骨架 — 2026-02-08 00:00

**新建文件：**
- `Assets/Scripts/Combat/StarChart/LightSailSO.cs` — 光帆 SO
- `Assets/Scripts/Combat/StarChart/SatelliteSO.cs` — 伴星 SO

**内容：**
- LightSailSO：ConditionDescription, EffectDescription（策划可读文本）。全机唯一引擎插槽。
- SatelliteSO：TriggerDescription, ActionDescription, InternalCooldown(0.5s)。IF-THEN 事件响应器。

**目的：** 定义 SO 骨架。光帆监听飞船状态提供增益（多普勒效应、擦弹引擎等），伴星是自动化模组（复仇之月、清道夫等）。运行时逻辑在 Batch 3 实现。

**技术：** ScriptableObject 继承, `[TextArea]` 描述字段。

---

### 23. 清理调试日志 — 2026-02-08 00:00

**修改文件：**
- `Assets/Scripts/Ship/Input/InputHandler.cs` — 移除 `Debug.Log("[InputHandler] Fire pressed")`
- `Assets/Scripts/Combat/Weapon/WeaponSystem.cs` — 移除启动诊断块和 Fire() 中的调试 warning

**目的：** 移除上次射击系统调试时临时添加的日志，保持代码整洁。

---

## 星图编织系统 Batch 2：槽位架构 + 发射管线重构

### 24. ProjectileParams + Projectile 重构 — 2026-02-08 14:00

**新建文件：**
- `Assets/Scripts/Combat/StarChart/ProjectileParams.cs` — 轻量 readonly struct

**修改文件：**
- `Assets/Scripts/Combat/Projectile/Projectile.cs` — 新 Initialize 重载

**内容：** ProjectileParams 是 readonly struct（Damage, Speed, Lifetime, Knockback, ImpactVFXPrefab），替代 WeaponStatsSO 作为 Projectile.Initialize 参数。Projectile 新增 `_impactVFXPrefab` 字段替代 `_weaponStats` 引用。旧 Initialize(WeaponStatsSO) 标 `[Obsolete]` 并内部转调新版。

**目的：** 解耦 Projectile 对 WeaponStatsSO 的依赖，使星图管线可以传递棱镜修正后的数值。

**技术：** readonly struct（零分配）, `[Obsolete]` 向后兼容过渡, FromWeaponStats 工厂方法。

---

### 25. SlotLayer 槽位管理 — 2026-02-08 14:00

**新建文件：**
- `Assets/Scripts/Combat/StarChart/SlotLayer.cs` — 泛型槽位层

**内容：** `SlotLayer<T> where T : StarChartItemSO`，3 格固定容量。TryEquip 查找连续空闲格子，Unequip 释放占用，支持 SlotSize 1-3 的部件。bool[] 跟踪占用状态。

**目的：** GDD 第 3 节"三明治结构"的数据层实现。防止大部件超出容量、处理 Size=2/3 的连续占位逻辑。

**技术：** 泛型约束, 连续空间查找算法, 纯 C# 类（非 MonoBehaviour）。

---

### 26. FiringSnapshot 快照数据 — 2026-02-08 14:00

**新建文件：**
- `Assets/Scripts/Combat/StarChart/FiringSnapshot.cs` — CoreSnapshot + TrackFiringSnapshot

**内容：**
- `CoreSnapshot`：单个核心修正后的完整参数（数值 + prefab + 音效 + IProjectileModifier 列表），提供 `ToProjectileParams()` 转换。
- `TrackFiringSnapshot`：一个轨道的齐射快照（CoreSnapshot 列表 + 总热量/后坐力/冷却/弹数）。

**目的：** 作为 SnapshotBuilder 的输出和 StarChartController 的输入，在快照构建和消费之间传递不可变数据。

**技术：** 值对象模式, 不可变快照。

---

### 27. SnapshotBuilder 快照构建器 — 2026-02-08 14:00

**新建文件：**
- `Assets/Scripts/Combat/StarChart/SnapshotBuilder.cs` — 静态工具类

**内容：** `Build(cores, prisms)` 静态方法实现 GDD 第 5 节发射管线的 Step 2~4：
- 聚合所有棱镜 StatModifier（Multiply 累乘，Add 累加）
- 对每个核心：`result = (base * totalMul) + (totalAdd / coreCount)`
- 收集 Tint 棱镜的 IProjectileModifier
- 计算总热量（核心修正值 + 棱镜自身 flat）、总后坐力、最慢冷却
- 弹幕硬上限：>20 颗截断，多余转伤害加成（5%/颗）

**目的：** 棱镜修正算法核心——Add 平均分配（GDD 要求 +10/2核心=各+5），Multiply 全额应用。

**技术：** 静态复用字典（避免 GC），比例裁剪算法，硬上限保护。

---

### 28. WeaponTrack 武器轨道 — 2026-02-08 14:00

**新建文件：**
- `Assets/Scripts/Combat/Weapon/WeaponTrack.cs` — 纯 C# 类

**内容：** 持有 `SlotLayer<StarCoreSO>` + `SlotLayer<PrismSO>` 双层。提供 EquipCore/EquipPrism/Unequip 方法（dirty 标记 → 下次 TryFire 时重建快照）。Tick(dt) 更新冷却计时。TryFire() 返回缓存的 TrackFiringSnapshot 并设置冷却。

**目的：** 每个轨道是独立的射击通道（主/副），拥有独立冷却和槽位配置。快照缓存避免每帧重建。

**技术：** 脏标记 + 缓存模式, 委托 PoolManager 管理池, 纯 C# 类。

---

### 29. InputHandler 副武器输入 — 2026-02-08 14:00

**修改文件：**
- `Assets/Input/ShipActions.inputactions` — 新增 `FireSecondary` action
- `Assets/Scripts/Ship/Input/InputHandler.cs` — 新增副武器输入

**内容：**
- ShipActions：新增 FireSecondary 按钮绑定（Mouse/rightButton + Gamepad/leftTrigger）
- InputHandler：新增 `IsSecondaryFireHeld` 属性, `OnSecondaryFirePressed`/`OnSecondaryFireReleased` 事件

**目的：** 支持双轨道（主/副武器）的独立输入。

**技术：** New Input System action 扩展。

---

### 30. StarChartController 星图控制器 — 2026-02-08 14:00

**新建文件：**
- `Assets/Scripts/Combat/StarChart/StarChartController.cs` — 顶层 MonoBehaviour

**修改文件：**
- `Assets/Scripts/Combat/Weapon/WeaponSystem.cs` — 类标 `[Obsolete]`
- `Assets/Scripts/Combat/Data/WeaponStatsSO.cs` — 类标 `[Obsolete]`

**内容：** 替代 WeaponSystem 的新编排器。管理两个 WeaponTrack（Primary/Secondary），Update 中检查输入 + 热量 → ExecuteFire 遍历 CoreSnapshot → 生成弹头/炮口焰/音效 → 后坐力 + 热量结算 → 事件通知。[SerializeField] 暴露 debug 装备数组用于 Inspector 测试。

**目的：** GDD 第 3.3 节"齐射"逻辑和第 5 节发射管线的完整实现。所有核心同帧开火，棱镜修正管线通过 SnapshotBuilder 完成。

**技术：** MonoBehaviour 编排器, 组合模式（WeaponTrack 作为子组件），事件驱动, 对象池, `[RequireComponent]`。

---

## MS2 Batch 3: 光帆框架 + 伴星运行时

### 31. StarChartContext 依赖上下文 — 2026-02-08 20:00

**新建文件：**
- `Assets/Scripts/Combat/StarChart/StarChartContext.cs` — readonly struct 依赖包

**内容：** 打包 ShipMotor/ShipAiming/InputHandler/HeatSystem/StarChartController/Transform 为单一注入点，供光帆和伴星 Behavior 访问所有 Ship 系统。

**目的：** 避免 Behavior 自行 GetComponent，统一依赖注入。

**技术：** readonly struct（零分配），依赖注入模式。

---

### 32. LightSailSO + SatelliteSO 运行时字段 — 2026-02-08 20:00

**修改文件：**
- `Assets/Scripts/Combat/StarChart/LightSailSO.cs` — 新增 `_behaviorPrefab` 字段
- `Assets/Scripts/Combat/StarChart/SatelliteSO.cs` — 新增 `_behaviorPrefab` 字段

**内容：** 添加 `[SerializeField] GameObject _behaviorPrefab` + 公开属性，用于关联运行时行为 Prefab。

**目的：** SO 数据层与运行时行为桥接。遵循 PrismSO 的 `ProjectileModifierPrefab` 先例。

**技术：** 策略模式（Prefab 持有具体 Behavior 子类）。

---

### 33. ProjectileParams + WeaponTrack 扩展 — 2026-02-08 20:00

**修改文件：**
- `Assets/Scripts/Combat/StarChart/ProjectileParams.cs` — 新增 `WithDamageMultiplied(float)` 方法
- `Assets/Scripts/Combat/Weapon/WeaponTrack.cs` — 新增 `ResetCooldown()` 方法

**内容：** ProjectileParams 新增零分配的 buff 应用便捷方法；WeaponTrack 暴露冷却重置 API 供光帆/伴星调用。

**目的：** 光帆 buff 通过 ref 传递 readonly struct 修改弹头参数；ResetCooldown 支持擦弹引擎等机制。

**技术：** readonly struct 值拷贝（零 GC），公开 API 扩展。

---

### 34. LightSailBehavior + LightSailRunner — 2026-02-08 20:10

**新建文件：**
- `Assets/Scripts/Combat/StarChart/LightSail/LightSailBehavior.cs` — 抽象 MonoBehaviour 基类
- `Assets/Scripts/Combat/StarChart/LightSail/LightSailRunner.cs` — 纯 C# 生命周期管理器

**内容：** LightSailBehavior 定义 Initialize/Tick/ModifyProjectileParams/OnDisabled/OnEnabled/Cleanup 生命周期。LightSailRunner 负责实例化 BehaviorPrefab 为 Ship 子物体、订阅 HeatSystem 过热事件（过热时禁用 buff）、Tick 驱动、Dispose 清理。

**目的：** 光帆运行时框架。抽象基类 + Runner 分离，遵循 WeaponTrack 纯 C# 先例。

**技术：** 抽象 MonoBehaviour，纯 C# Runner，事件订阅（OnOverheated/OnCooldownComplete），策略模式。

---

### 35. SatelliteBehavior + SatelliteRunner — 2026-02-08 20:10

**新建文件：**
- `Assets/Scripts/Combat/StarChart/Satellite/SatelliteBehavior.cs` — 抽象 MonoBehaviour 基类
- `Assets/Scripts/Combat/StarChart/Satellite/SatelliteRunner.cs` — 纯 C# 生命周期管理器

**内容：** SatelliteBehavior 实现 IF-THEN 模式：EvaluateTrigger(context)→bool + Execute(context)。SatelliteRunner 管理 Prefab 实例化、内部冷却计时（InternalCooldown）、触发评估-执行调度。

**目的：** 伴星运行时框架。支持轮询型（检查状态）和事件驱动型（设 flag → 下帧检查）两种触发模式。

**技术：** 模板方法模式（EvaluateTrigger/Execute），内部冷却防触发循环。

---

### 36. StarChartController Batch 3 集成 — 2026-02-08 20:20

**修改文件：**
- `Assets/Scripts/Combat/StarChart/StarChartController.cs` — 集成光帆/伴星 Runner

**内容：** 替换旧占位字段为 `_debugLightSail`/`_debugSatellites[]`。Awake 中创建 StarChartContext，Start 中初始化 Runner。Update 中 Tick 光帆/伴星。SpawnProjectile 中调用 `_lightSailRunner?.ModifyProjectileParams(ref parms)` 应用光帆 buff。OnDestroy 中 Dispose 所有 Runner。移除 Batch 2 诊断日志。

**目的：** 将光帆和伴星框架完整集成到星图控制器中。

**技术：** 纯 C# Runner 组合，ref 参数零分配 buff 传递，可空安全 (`?.`)。

---

### 37. SpeedDamageSail 测试实现 — 2026-02-08 20:20

**新建文件：**
- `Assets/Scripts/Combat/StarChart/LightSail/SpeedDamageSail.cs` — 简化版多普勒效应

**内容：** 继承 LightSailBehavior。Tick 中读取 NormalizedSpeed → 计算伤害乘数（0 速 = ×1.0，满速 = ×1.5）。ModifyProjectileParams 中通过 WithDamageMultiplied 应用。OnDisabled 时重置乘数。

**目的：** 验证光帆框架完整工作流（实例化 → Tick → Buff 应用 → 过热禁用/恢复）。

**技术：** 线性插值 buff，readonly struct 值拷贝。

---

## Batch 4: Star Chart UI + 热量 HUD

### 38. UI Assembly 配置 + StarChartInventorySO — 2026-02-08 21:00

**修改文件：**
- `Assets/Scripts/UI/ProjectArk.UI.asmdef` — 添加 Combat, Heat, InputSystem, TMP 引用

**新建文件：**
- `Assets/Scripts/UI/StarChartInventorySO.cs` — 原型库存 SO，持有玩家拥有的星图部件列表

**内容：** 扩展 UI 程序集依赖以访问战斗和热量系统。创建 StarChartInventorySO 提供按类型过滤的库存数据源。

**目的：** 为 UI 层建立编译基础和数据源。

**技术：** ScriptableObject 数据驱动，LINQ OfType 泛型过滤。

---

### 39. HeatBarHUD 常驻热量条 — 2026-02-08 21:05

**新建文件：**
- `Assets/Scripts/UI/HeatBarHUD.cs` — 常驻热量条 HUD

**内容：** 通过 Bind(HeatSystem) 注入引用，订阅 OnHeatChanged/OnOverheated/OnCooldownComplete 事件。填充量 + Gradient 渐变色 + 过热红色闪烁动画（sin 波 alpha）。OnDestroy 取消订阅。

**目的：** 战斗中始终可见的热量状态指示。

**技术：** 事件驱动 UI 更新，Time.unscaledDeltaTime 动画（兼容 timeScale=0）。

---

### 40. SlotCellView 格子视图 — 2026-02-08 21:10

**新建文件：**
- `Assets/Scripts/UI/SlotCellView.cs` — 单格子 UI 原子构件

**内容：** 支持四种状态：SetItem（显示图标）、SetEmpty（空格）、SetSpannedBy（被大型部件占用）、SetHighlight（绿/红高亮）。Button.onClick 委托 OnClicked 事件。

**目的：** 轨道视图和库存视图的通用格子构件。

**技术：** uGUI Image + Button，颜色状态机。

---

### 41. TrackView 轨道展示 — 2026-02-08 21:15

**新建文件：**
- `Assets/Scripts/UI/TrackView.cs` — 单武器轨道视图

**内容：** 三明治布局：3 棱镜格 + 3 星核格。Bind(WeaponTrack) 后订阅 OnLoadoutChanged，Refresh 根据 Items 和 SlotSize 映射到 SlotCellView。支持轨道选中高亮。

**目的：** 可视化武器轨道的当前装备配置。

**技术：** 事件驱动刷新，SlotSize 到格子索引的线性映射。

---

### 42. InventoryItemView + InventoryView — 2026-02-08 21:20

**新建文件：**
- `Assets/Scripts/UI/InventoryItemView.cs` — 单库存物品卡片
- `Assets/Scripts/UI/InventoryView.cs` — 可滚动库存格子 + 类型过滤

**内容：** InventoryItemView 显示图标/名称/装备标记。InventoryView 从 StarChartInventorySO 动态实例化卡片，支持 All/Cores/Prisms/Sails/Satellites 过滤标签。通过外部注入的 Func 检查装备状态。

**目的：** 让玩家浏览和选择可装备的星图部件。

**技术：** 动态 Instantiate UI 预制体，Func 委托解耦装备检查。

---

### 43. ItemDetailView 物品详情 — 2026-02-08 21:25

**新建文件：**
- `Assets/Scripts/UI/ItemDetailView.cs` — 物品详情面板

**内容：** ShowItem 显示名称/描述/属性/装备按钮。根据 SO 类型显示特有属性（Core: 伤害/射速, Prism: 修正列表, Sail: 效果描述, Satellite: 触发/动作描述）。按钮文字 EQUIP/UNEQUIP 自动切换。

**目的：** 装备操作的信息确认界面。

**技术：** switch 模式匹配，StringBuilder 属性构建。

---

### 44. StarChartPanel 面板编排器 — 2026-02-08 21:30

**新建文件：**
- `Assets/Scripts/UI/StarChartPanel.cs` — 星图面板根控制器

**内容：** 编排 TrackView×2 + InventoryView + ItemDetailView。Bind 注入 StarChartController 和 InventorySO。Open/Close 控制显示。选中轨道 → 选中物品 → 点击装备/卸载的完整交互流。支持所有四种部件类型的装备/卸载。

**目的：** Star Chart 面板的交互逻辑核心。

**技术：** 事件链编排，IsItemEquipped 遍历双轨道检查。

---

### 45. UIManager 面板开关 + 暂停 — 2026-02-08 21:35

**新建文件：**
- `Assets/Scripts/UI/UIManager.cs` — 顶层 UI 控制器

**内容：** 监听 Pause action（ESC/Start），Toggle 开关面板。打开时禁用 Fire/FireSecondary action + Time.timeScale=0。关闭时恢复。Start 中 FindAnyObjectByType 查找 StarChartController 和 HeatSystem 并绑定子视图。

**目的：** 游戏内菜单入口和暂停控制。

**技术：** InputAction 单独禁用/启用，Time.timeScale 暂停。

---

### 46. StarChartController 运行时装备 API — 2026-02-08 21:40

**修改文件：**
- `Assets/Scripts/Combat/StarChart/StarChartController.cs`

**内容：** 新增 6 个公开方法供 UI 调用：EquipLightSail, UnequipLightSail, EquipSatellite, UnequipSatellite, GetEquippedLightSail, GetEquippedSatellites。新增 OnLightSailChanged 和 OnSatellitesChanged 事件。内部新增 _equippedLightSailSO 和 _equippedSatelliteSOs 跟踪字段，debug 初始化时同步填充。

**目的：** 让 UI 能在运行时动态装备/卸载光帆和伴星。

**技术：** Runner Dispose + 重建模式，事件通知 UI 刷新。

---

## Batch 5: 具体部件实现（各家族 Core / Prism）

### 47. StarChartController 发射管线重构 — 家族分发 + 均匀扇形散布 — 2026-02-09 00:00

**修改文件：**
- `Assets/Scripts/Combat/StarChart/StarChartController.cs`

**内容：**
- 修改 `ExecuteFire()`：当 `ProjectileCount > 1` 时，散布逻辑从 `Random.Range(-Spread, Spread)` 改为 `[-Spread, +Spread]` 均匀等分
- 重构 `SpawnProjectile()` 为 `switch(coreSnap.Family)` 家族分发模式，调用各家族私有方法：`SpawnMatterProjectile()` / `SpawnLightBeam()` / `SpawnEchoWave()` / `SpawnAnomalyEntity()`
- default 分支打印 `Debug.LogWarning` 并 fallback 到 Matter
- 投射物生成后应用 `ProjectileSize`：`bulletObj.transform.localScale = Vector3.one * coreSnap.ProjectileSize`

**目的：** 让 4 种 CoreFamily 拥有各自独立的发射分支，支持均匀扇形散布（Fractal 棱镜需要）。

**技术：** switch 分发模式，均匀角度等分算法，策略模式扩展点。

---

### 48. LaserBeam — Light 家族即时命中激光 — 2026-02-09 00:00

**新建文件：**
- `Assets/Scripts/Combat/Projectile/LaserBeam.cs`

**内容：** MonoBehaviour + IPoolable。`Fire()` 方法执行 `Physics2D.Raycast` 从炮口沿方向检测命中（最大射程 = Speed × Lifetime）。使用 `LineRenderer` 渲染光束，持续约 0.1 秒后淡出并通过 PoolReference 回池。命中时调用 `IProjectileModifier.OnProjectileHit`。

**目的：** Light 家族的瞬间命中射击风格，不创建 Rigidbody2D 物理投射物。

**技术：** Physics2D.Raycast, LineRenderer 渐隐动画, IPoolable 对象池回收。

---

### 49. EchoWave — Echo 家族震荡波 AOE — 2026-02-09 00:00

**新建文件：**
- `Assets/Scripts/Combat/Projectile/EchoWave.cs`

**内容：** MonoBehaviour + IPoolable。`Initialize()` 设置膨胀参数。Update 中 `CircleCollider2D.radius` 按 `ProjectileSpeed` 线性膨胀。OnTriggerEnter2D 使用 `HashSet<Collider2D>` 去重（同一波次每敌人仅命中一次）。Spread > 0 时缩减为扇形波（角度检测限制触发范围）。穿墙特性通过碰撞层设置实现。超过 Lifetime 自动回池。

**目的：** Echo 家族的 AOE 扩散攻击风格，穿透墙壁，适合近距离群体控制。

**技术：** CircleCollider2D 动态膨胀, HashSet 去重, 扇形角度检测, IPoolable。

---

### 50. BoomerangModifier — Anomaly 家族回旋镖行为 — 2026-02-09 00:00

**新建文件：**
- `Assets/Scripts/Combat/Projectile/BoomerangModifier.cs`

**内容：** MonoBehaviour + IProjectileModifier。`OnProjectileSpawned` 记录发射者位置和初始方向。`OnProjectileUpdate` 实现去程减速 → 反转 → 回程加速的运动曲线，覆盖 `Projectile.Direction` 和 `Rigidbody2D.linearVelocity`。`OnProjectileHit` 使用两个 HashSet 分别跟踪去程和回程命中，每程每敌人各允许命中一次。设置 `ShouldDestroyOnHit = false` 实现穿透。返回发射者附近（< 1 单位）或 Lifetime 耗尽时回池。

**目的：** Anomaly 家族的自定义运动轨迹，实现回旋镖战术。

**技术：** IProjectileModifier 钩子, 运动曲线覆盖, 双 HashSet 去重。

---

### 51. Projectile 扩展 — 穿透与缩放重置 — 2026-02-09 00:00

**修改文件：**
- `Assets/Scripts/Combat/Projectile/Projectile.cs`

**内容：**
- 添加 `ShouldDestroyOnHit` 公共属性（默认 true），供 modifier 覆盖
- 修改 `OnTriggerEnter2D`：当 `ShouldDestroyOnHit == false` 时只执行 modifier 回调 + VFX，不回池
- 添加 `ForceReturnToPool()` 公共方法，供 BoomerangModifier 主动回池
- 在 `OnReturnToPool()` 中添加 `transform.localScale = Vector3.one` 重置缩放
- 在 `OnReturnToPool()` 中重置 `ShouldDestroyOnHit = true`

**目的：** 支持 Anomaly 回旋镖（穿透不销毁）和 Rheology 棱镜（ProjectileSize 缩放后回池重置）。

**技术：** 属性驱动行为开关, 回池状态重置。

---

### 52. SlowOnHitModifier — Tint 棱镜减速效果占位 — 2026-02-09 00:00

**新建文件：**
- `Assets/Scripts/Combat/Projectile/SlowOnHitModifier.cs`

**内容：** MonoBehaviour + IProjectileModifier。`OnProjectileHit` 检测碰撞体，当前为占位实现（`Debug.Log` 标记命中和预期减速效果）。`[SerializeField]` 暴露 `SlowPercent`(30%) 和 `Duration`(2s) 参数。`OnProjectileSpawned` 和 `OnProjectileUpdate` 为空实现。

**目的：** Tint 家族状态注入框架，待敌人系统完成后替换为实际 debuff 逻辑。

**技术：** IProjectileModifier 接口, 占位实现模式。

---

### 53. BounceModifier — Rheology 棱镜反弹效果 — 2026-02-09 00:00

**新建文件：**
- `Assets/Scripts/Combat/Projectile/BounceModifier.cs`

**内容：** MonoBehaviour + IProjectileModifier。`OnProjectileHit` 检测碰撞是否为墙壁层，若是则使用 `Vector2.Reflect` 计算反射方向，更新 `Projectile.Direction` 和 `Rigidbody2D.linearVelocity`，递减反弹计数。`[SerializeField]` 暴露 `MaxBounces`(3) 参数。反弹次数用尽时允许正常销毁。

**目的：** Rheology 棱镜的弹性反弹效果，让子弹在墙壁间弹射。

**技术：** Vector2.Reflect 反射算法, IProjectileModifier 接口, 计数器限制。

---

### 54. StarCoreSO 扩展 — Anomaly 家族 Modifier Prefab 链接 — 2026-02-09 00:00

**修改文件：**
- `Assets/Scripts/Combat/StarChart/StarCoreSO.cs` — 添加 `_anomalyModifierPrefab` 字段和 `AnomalyModifierPrefab` 属性
- `Assets/Scripts/Combat/StarChart/SnapshotBuilder.cs` — 在 `BuildCoreSnapshot()` 中传递 `AnomalyModifierPrefab` 到 `CoreSnapshot`
- `Assets/Scripts/Combat/StarChart/FiringSnapshot.cs` — `CoreSnapshot` 已包含 `AnomalyModifierPrefab` 字段

**内容：** 在 StarCoreSO 中添加 Anomaly 家族专用的 Modifier Prefab 引用字段，通过 SnapshotBuilder 管线传递到 CoreSnapshot，最终在 `SpawnAnomalyEntity()` 中使用。

**目的：** 完成 Anomaly 家族从 SO 配置到运行时行为注入的完整数据链路。

**技术：** SerializeField + 管线传递, 家族专用可选字段。

---

### 55. WeaponTrack 多家族池预热 — 2026-02-09 00:10

**修改文件：**
- `Assets/Scripts/Combat/Weapon/WeaponTrack.cs`

**内容：** 重构 `InitializePools()` 方法，根据 `CoreFamily` 分支预热不同容量的对象池：
- Matter: 20/50（高频子弹）
- Light: 5/20（短命 LineRenderer）
- Echo: 5/15（少量并发震荡波）
- Anomaly: 10/30（投射物池 + AnomalyModifierPrefab 池）
- 炮口焰池统一 5/20

**目的：** 避免运行时首次生成时的卡顿，按家族特性合理分配池容量。

**技术：** switch(CoreFamily) 分支, PoolManager.GetPool 预热。

---

### 56. Batch5AssetCreator — Editor 一键资产创建工具 — 2026-02-09 00:15

**新建文件：**
- `Assets/Scripts/Combat/Editor/Batch5AssetCreator.cs` — Editor 工具脚本
- `Assets/Scripts/Combat/Editor/ProjectArk.Combat.Editor.asmdef` — Editor-only 程序集

**内容：** 通过 Unity 菜单 `ProjectArk > Create Batch 5 Test Assets` 一键自动创建：
- 6 个 Prefab（Projectile_Matter, LaserBeam_Light, EchoWave_Echo, Modifier_Boomerang, Modifier_SlowOnHit, Modifier_Bounce）
- 4 个 StarCoreSO 资产（MatterCore_StandardBullet, LightCore_BasicLaser, EchoCore_BasicWave, AnomalyCore_Boomerang）
- 3 个 PrismSO 资产（FractalPrism_TwinSplit, RheologyPrism_Accelerate, TintPrism_FrostSlow）
- 目录自动创建于 `Assets/_Data/StarChart/` 下

**目的：** 解决 Prefab 和 SO 资产无法通过代码文本创建的问题，提供自动化资产生成工具。

**技术：** SerializedObject/SerializedProperty 反射设置私有字段, PrefabUtility.SaveAsPrefabAsset, AssetDatabase, Editor-only 程序集。

---

## Batch 6: 星图编织态交互体验 (Star Chart Weaving State IxD)

### 57. WeavingTransitionSettingsSO — 过渡配置 SO — 2026-02-09 12:45

**新建文件：**
- `Assets/Scripts/UI/WeavingTransitionSettingsSO.cs`

**内容：** ScriptableObject 数据容器，集中管理编织态过渡的所有可配置参数：
- Timing：进入时长 (0.35s)、退出时长 (0.25s)、AnimationCurve
- Camera：战斗态/编织态正交尺寸 (5 / 3)
- Vignette：战斗态/编织态强度 (0.1 / 0.5)
- DoF：焦距、焦点距离
- Audio：进入/退出音效 AudioClip

**目的：** 数据驱动，设计师可在 Inspector 中独立调整过渡参数，支持多套预设切换。

**技术：** ScriptableObject, `[CreateAssetMenu]`, 数据驱动模式。

---

### 58. WeavingStateTransition — 编织态过渡控制器 — 2026-02-09 12:45

**新建文件：**
- `Assets/Scripts/UI/WeavingStateTransition.cs`

**内容：** 编织态过渡的唯一编排器 MonoBehaviour，负责：
- 镜头推拉：协程驱动 `Camera.orthographicSize` 在战斗值/编织值之间平滑插值
- 后处理氛围：URP Volume 的 DepthOfField (启用/禁用) 和 Vignette (intensity 渐变)
- 镜头锁定：过渡期间将相机位置锁定在飞船中心（保持 Z 偏移）
- 音效播放：进入/退出时 `PlayOneShot`，AudioSource 设 `ignoreListenerPause = true`
- 全部使用 `Time.unscaledDeltaTime` 驱动，timeScale=0 下正常运行
- 快速切换防冲突：新协程启动前先 `StopCoroutine` 取消进行中的过渡
- 双数据源：优先读取 `WeavingTransitionSettingsSO`，为 null 时使用内联默认值

**公共 API：**
- `EnterWeavingState()` — 战斗态 → 编织态过渡
- `ExitWeavingState()` — 编织态 → 战斗态过渡

**目的：** 一个脚本统一编排镜头、后处理、音效三条过渡线，避免分散管理。

**技术：** 协程 + AnimationCurve, URP Volume TryGet<T>, Time.unscaledDeltaTime, 可选 SO 依赖 (fallback 模式)。

---

### 59. UIParallaxEffect — UI 视差微动 — 2026-02-09 12:45

**新建文件：**
- `Assets/Scripts/UI/UIParallaxEffect.cs`

**内容：** 挂在星图面板根节点的 MonoBehaviour，实现：
- 鼠标位置相对屏幕中心归一化 (-1,1)，乘最大偏移 (±15px) 并取反（视差=反向位移）
- `Vector2.Lerp` 平滑跟随，使用 `Time.unscaledDeltaTime`
- 手柄支持：`Gamepad.current?.rightStick.ReadValue()` 优先使用
- `OnDisable` 时自动 `ResetOffset()` 清零偏移
- `OnEnable` 时快照当前 `anchoredPosition` 作为基准原点

**目的：** 增加编织态 UI 的 3D 悬浮感和精致度。

**技术：** RectTransform anchoredPosition 偏移, New Input System Gamepad API, unscaledDeltaTime。

---

### 60. UIManager 集成 WeavingStateTransition — 2026-02-09 12:45

**修改文件：**
- `Assets/Scripts/UI/UIManager.cs`

**内容：**
- 新增 `[SerializeField] private WeavingStateTransition _weavingTransition` 字段
- `OpenPanel()` 中在 `Time.timeScale = 0` 之后调用 `_weavingTransition?.EnterWeavingState()`
- `ClosePanel()` 中在 `Time.timeScale = 1` 之前调用 `_weavingTransition?.ExitWeavingState()`
- 使用 null-conditional 确保为可选依赖，未配置时静默跳过

**目的：** 将过渡效果接入现有面板开关流程，零侵入式集成。

**技术：** 可选依赖模式 (null-conditional `?.`), SerializeField 编辑器连线。

---

### 25. UICanvasBuilder 编辑器脚本 — UI 自动搭建工具 — 2026-02-09 12:52

**新建文件：**
- `Assets/Scripts/UI/Editor/UICanvasBuilder.cs` — 编辑器工具，自动搭建完整 UI Canvas 层级

**内容：**
- 菜单 `ProjectArk > Build UI Canvas`：一键创建完整 UI 层级结构
  - Canvas (Screen Space Overlay, 1920×1080 参考分辨率)
  - EventSystem (如场景中缺失则自动创建)
  - HeatBarHUD (FillImage + OverheatFlash + Label，自动连线)
  - StarChartPanel (含 UIParallaxEffect，自动连线)
    - PrimaryTrackView / SecondaryTrackView (各含 3 PrismCell + 3 CoreCell)
    - InventoryView (过滤按钮栏 + ScrollRect 网格布局)
    - ItemDetailView (Icon + Name + Description + Stats + ActionButton)
  - UIManager (含 WeavingStateTransition 组件，自动连线)
- 菜单 `ProjectArk > Create InventoryItemView Prefab`：创建库存卡片预制体
- 自动查找并关联 InputActionAsset、StarChartInventorySO
- 自动创建 PlayerInventory.asset 并填充已有的 StarCoreSO/PrismSO/LightSailSO/SatelliteSO
- 所有 SerializeField 引用通过 SerializedObject API 自动连线

**目的：** 消除最大的编辑器配置缺口——UI Canvas 层级手动搭建工作量（约 30-60 分钟），一键完成。

**技术：** Unity Editor MenuItem, SerializedObject 反射连线, PrefabUtility, Undo 支持, 自动资产发现。

---

### Batch 7: 编辑器/资产批量配置 — 2026-02-09 16:10

**修改的文件：**
- `Assets/_Data/StarChart/Prisms/RheologyPrism_Accelerate.asset` — 连线 `_projectileModifierPrefab` → Modifier_Bounce.prefab
- `Assets/_Data/StarChart/Cores/MatterCore_StandardBullet.asset` — `_heatCost: 0 → 5`
- `Assets/_Data/StarChart/Cores/LightCore_BasicLaser.asset` — `_heatCost: 0 → 4`
- `Assets/_Data/StarChart/Cores/EchoCore_BasicWave.asset` — `_heatCost: 0 → 12`
- `Assets/_Data/StarChart/Cores/AnomalyCore_Boomerang.asset` — `_heatCost: 0 → 8`
- `ProjectSettings/TagManager.asset` — 添加 Layer 10: EchoWave
- `ProjectSettings/Physics2DSettings.asset` — 更新碰撞矩阵: Player↔PlayerProjectile❌, Player↔EchoWave❌, EchoWave↔Wall❌, EchoWave↔EchoWave❌, EchoWave↔PlayerProjectile❌, PlayerProjectile↔PlayerProjectile❌

**新建的文件：**
- `Assets/_Data/UI/WeavingTransitionSettings.asset` — WeavingTransitionSettingsSO 数据资产 (默认参数)
- `Assets/Settings/GlobalVolumeProfile.asset` — URP Volume Profile (Vignette + DepthOfField Override)

**场景修改 (SampleScene.unity)：**
- 新增 Global Volume GameObject (id: 300000001)，挂载 Volume 组件，引用 GlobalVolumeProfile
- UIManager 对象添加 AudioSource 组件 (id: 300000004, PlayOnAwake=false)
- WeavingStateTransition 连线: `_postProcessVolume` → Global Volume, `_sfxSource` → AudioSource, `_settings` → WeavingTransitionSettings.asset
- EventSystem: StandaloneInputModule → InputSystemUIInputModule (New Input System)
- HeatBarHUD.FillImage: `m_RaycastTarget: 1 → 0`
- HeatBarHUD._heatGradient: 白→白 → 绿(#33CC4C)→黄(#FFD700)→红(#FF3333)

**目的：** 批量完成所有已实现功能的编辑器配置，使项目可以直接 Play 测试。

**技术：** 直接编辑 Unity YAML 序列化文件 (.asset/.unity)，通过查找 PackageCache 中的 .cs.meta 文件获取正确的 script guid。

---

### Input System 整理 — 删除冗余 Player Action Map (2026-02-09 16:28)

**删除的文件：**
- `Assets/InputSystem_Actions.inputactions` — Unity 自动生成的默认 Input Actions，含 "Player" + "UI" Action Map，无任何代码/场景/预制体引用
- `Assets/InputSystem_Actions.inputactions.meta`

**保留的文件：**
- `Assets/Input/ShipActions.inputactions` — 项目实际使用的 Input Actions，含 "Ship" + "UI" 两个 Action Map，被 SampleScene、Ship.prefab、UIManager 引用

**目的：** 采用方案 B（独立 Ship Action Map），每个游戏状态一个 Action Map（Ship=飞行战斗，UI=菜单/星图），遵循 Unity New Input System 最佳实践和项目模块化架构原则。删除无人引用的默认 Player Action Map 文件，消除歧义。

---

### 修复星图 UI 点击无响应 — InputSystemUIInputModule Action 连线 (2026-02-09 16:42)

**问题：** 打开星图面板后，所有 UI 按钮（轨道选择、库存物品、详情面板）点击无响应，hover 也无效。

**根因：** 场景中 EventSystem 的 `InputSystemUIInputModule` 组件虽然已存在且引用了 `ShipActions.inputactions`，但其所有 UI Action 字段（`m_PointAction`、`m_LeftClickAction`、`m_MoveAction` 等）均为 `{fileID: 0}`（空引用）。这导致模块不知道鼠标指针位置，无法执行 GraphicRaycaster 射线检测，Button.onClick 永远不会被触发。

**修改的文件：**
- `Assets/Scripts/UI/UIManager.cs` — 在 `Awake()` 中新增 `ConfigureUIInputModule()` 方法，自动查找场景中的 `InputSystemUIInputModule`，将其 `point`/`leftClick`/`scrollWheel`/`move`/`submit`/`cancel` 属性连接到 `ShipActions.inputactions` 的 "UI" Action Map 中对应的 Action，并确保 UI Map 始终启用

**技术：** 使用 `InputActionReference.Create()` 在运行时动态创建 Action 引用，避免依赖 Unity YAML 序列化中的哈希值配置。`uiMap.Enable()` 确保 UI Action Map 在游戏全程保持激活。

---

### 修复星图库存卡片不可见 — ScrollArea Mask Image Alpha (2026-02-09 23:24)

**问题：** 打开星图面板后，库存区域完全空白，看不到任何物品卡片。日志显示 10 个卡片已正确创建（`instantiated 10 items, contentParent.childCount=10`），Setup 也成功执行。

**根因：** ScrollArea 对象上的 `Mask` 组件依赖同 GameObject 上的 `Image` 组件来确定裁剪区域。该 Image 的颜色被设为 `{r:0, g:0, b:0, a:0}`（完全透明），同时 `CanvasRenderer.m_CullTransparentMesh = 1`。这导致 Image 被 CanvasRenderer 判定为完全透明而剔除渲染，Mask 因此没有可渲染像素作为裁剪区域，所有子物体（包括 10 个卡片）被完全裁掉。

**修改的文件：**
- `Assets/Scenes/SampleScene.unity` — ScrollArea (fileID: 1074280193) 的两个组件：
  - Image (fileID: 1074280196): `m_Color.a: 0 → 0.00392`（1/255，肉眼不可见但 Mask 有效）, `r/g/b: 0 → 1`（白色基底）
  - CanvasRenderer (fileID: 1074280197): `m_CullTransparentMesh: 1 → 0`（禁止剔除低透明度网格）

**目的：** 让 Mask 组件有可渲染的 Image 像素来定义裁剪区域，同时保持视觉上不可见（`m_ShowMaskGraphic: 0` + 极低 alpha）。

**技术：** Unity uGUI Mask 组件依赖 Graphic（通常是 Image）的渲染像素区域做模板裁剪。alpha=0 + CullTransparentMesh=1 会导致 Image 完全不参与渲染，Mask 裁剪区域退化为零。

---

### Fix: Dual-Core Projectile Self-Collision (2026-02-10 01:16)

**问题：** 同时装备两个 StarCore（如 BasicBullet + BasicBullet2）时，子弹射程极短，呈螺旋状停留在飞船周围。单独装备任一核心时子弹正常飞行。

**根因：** `OnTriggerEnter2D` 没有任何 Layer 过滤逻辑。两个核心在同一位置同时发射子弹，子弹均位于 PlayerProjectile (Layer 7)，而 Physics2D 碰撞矩阵中 Layer 7 与 Layer 7 之间碰撞为开启状态。两颗子弹的 Trigger Collider 互相重叠，立即触发 `OnTriggerEnter2D` → `ReturnToPool()`，导致子弹在出生后 1-2 帧内就被回收。

**修改的文件：**
- `ProjectSettings/Physics2DSettings.asset` — 碰撞矩阵修改：
  - Row 6 (Player): `fffffffb` → `ffffff7b` — 关闭 Player 与 PlayerProjectile 的碰撞
  - Row 7 (PlayerProjectile): `7ffffffb` → `7fffff3b` — 关闭 PlayerProjectile 自碰撞 + 与 Player 的碰撞
- `Assets/Scripts/Combat/Projectile/Projectile.cs` — OnTriggerEnter2D 添加 Layer 过滤：
  - 忽略同 Layer（其他子弹）和 Player Layer 的碰撞
  - 缓存 `LayerMask.NameToLayer("Player")` 到静态字段避免运行时字符串查找
  - 清理所有之前的调试日志代码（`_debugFrameCount`、`FixedUpdate` 诊断、`Init/Update/OnGetFromPool` 日志）

**目的：** 防止玩家子弹之间互相碰撞导致瞬间回收，同时防止子弹与玩家飞船碰撞。

**技术：** 双重保险策略——Physics2D 碰撞矩阵层面关闭不需要的 Layer 间碰撞（性能最优，物理引擎直接跳过检测），代码层面在 `OnTriggerEnter2D` 中添加防御性过滤（防止未来 Layer 配置意外变更时的兜底）。

---

### 🔧 Remap: 星图切换键 ESC → Tab — 2026-02-10 11:30

**变更说明：** 将星图面板的切换按键从 ESC 改为 Tab，ESC 键预留给未来的暂停菜单功能。同时将 Input Action 和代码中的命名从 `Pause` 重命名为 `ToggleStarChart`，语义更明确。

**修改的文件：**
- `Assets/Input/ShipActions.inputactions` —
  - Ship action map 中 `Pause` action 重命名为 `ToggleStarChart`
  - 键盘绑定路径从 `<Keyboard>/escape` 改为 `<Keyboard>/tab`
  - 手柄绑定 `<Gamepad>/start` 保持不变，action 引用同步更新
- `Assets/Scripts/UI/UIManager.cs` —
  - `_pauseAction` 字段重命名为 `_toggleStarChartAction`
  - `FindAction("Pause")` 改为 `FindAction("ToggleStarChart")`
  - 回调方法 `OnPausePerformed` 重命名为 `OnToggleStarChartPerformed`

---

### ✨ Feature: 星图拖拽装备 (Star Chart Drag & Drop) — 2026-02-10 12:10

**功能说明：** 为星图面板新增拖拽交互，玩家可以直接从库存区拖拽星核/棱镜到轨道槽位完成装备，也可以从槽位拖出到库存区卸载，或跨轨道移动。原有"点击→详情→EQUIP"流程保留，拖拽为补充交互方式。

**核心机制：**
- 基于 uGUI EventSystem 的 `IBeginDragHandler` / `IDragHandler` / `IEndDragHandler` / `IDropHandler` / `IPointerEnterHandler` / `IPointerExitHandler` 接口
- 单例 `DragDropManager` 管理全局拖拽状态、幽灵视图、装备/卸载执行
- 半透明 `DragGhostView` 跟随鼠标，`CanvasGroup.blocksRaycasts = false` 确保不遮挡 drop target
- 类型校验（Core→Core层, Prism→Prism层）+ 空间校验（SlotLayer.FreeSpace ≥ SlotSize）
- 悬停时绿色/红色高亮反馈，SlotSize>1 支持多格高亮预览
- 拖拽进入 TrackView 区域自动切换选中轨道
- 所有逻辑不依赖 Time.deltaTime，天然兼容 timeScale=0
- LightSail/Satellite 不参与拖拽（当前无对应拖放目标）
- 面板关闭时自动取消进行中的拖拽并清理幽灵

**新建文件：**
- `Assets/Scripts/UI/DragDrop/DragPayload.cs` — 拖拽数据载体（DragSource 枚举 + 轻量数据类）
- `Assets/Scripts/UI/DragDrop/DragDropManager.cs` — 全局拖拽管理器单例（状态管理、装备/卸载执行）
- `Assets/Scripts/UI/DragDrop/DragGhostView.cs` — 半透明拖拽幽灵视图

**修改文件：**
- `Assets/Scripts/UI/InventoryItemView.cs` — 实现 IBeginDragHandler/IDragHandler/IEndDragHandler，添加 CanvasGroup alpha 控制
- `Assets/Scripts/UI/SlotCellView.cs` — 实现 IDropHandler/IPointerEnterHandler/IPointerExitHandler（拖放目标）+ IBeginDragHandler/IDragHandler/IEndDragHandler（已装备物品拖出）；新增 IsCoreCell/CellIndex/OwnerTrack 属性
- `Assets/Scripts/UI/TrackView.cs` — 实现 IPointerEnterHandler 自动选中；Awake 中初始化 cell 属性；新增 HasSpaceForItem/SetMultiCellHighlight/ClearAllHighlights 方法
- `Assets/Scripts/UI/InventoryView.cs` — 实现 IDropHandler 接收从槽位拖出的物品完成卸载
- `Assets/Scripts/UI/StarChartPanel.cs` — 集成 DragDropManager（Bind/Close 清理）；新增 RefreshAllViews/SelectAndShowItem 公共方法

**技术：** uGUI EventSystem 拖拽接口 + 单例管理器模式 + 策略化校验（类型+空间）。

---

### 🔧 Refactor: DragDropManager 去除 Instantiate — 2026-02-10 12:42

**修改文件：**
- `Assets/Scripts/UI/DragDrop/DragDropManager.cs` — `_ghostPrefab` 字段重命名为 `_ghostView`（语义从"预制体"变为"场景实例直接引用"）；`Bind()` 中移除 `Instantiate()` 调用，改为直接赋值 `_ghost = _ghostView`

**目的：** DragGhost 作为 StarChartPanel 子对象全场唯一，无需在运行时 Instantiate 拷贝。简化代码，减少运行时开销。

**技术：** 场景内直接引用模式（零 Instantiate / 零 Prefab），SerializeField 绑定场景实例。

---

### 🐛 Bugfix: StarChartPanel._dragDropManager 空引用修复 — 2026-02-10 12:48

**修改文件：**
- `Assets/Scenes/SampleScene.unity` — StarChartPanel 组件的 `_dragDropManager` 字段从 `{fileID: 0}`（空引用）修复为 `{fileID: 1315625636}`（DragDropManager 组件实例）

**问题：** DragDropManager 组件已正确挂载在 StarChartPanel GameObject 上，但 StarChartPanel 的 `[SerializeField] _dragDropManager` 字段未在 Inspector 中拖入赋值，导致运行时为 null，`Bind()` 不会执行，拖拽功能整体失效。

**原因：** 在 Inspector 中添加 DragDropManager 组件后，忘记将其拖入 StarChartPanel 的 Drag Drop Manager 字段完成连线。

---

### HeatBarHUD 数值显示增强 — 2026-02-10 12:52

**修改文件：**
- `Assets/Scripts/Heat/HeatSystem.cs` — 新增 `MaxHeat` 公开属性，暴露最大热量值供 UI 读取
- `Assets/Scripts/UI/HeatBarHUD.cs` — Label 从静态文本改为动态数值显示

**内容：** Heat 条标签在正常状态下显示 `HEAT(当前值/最大值)` 格式（如 `HEAT(45/100)`），过热状态仍显示 `OVERHEATED`。热量值每次变化时实时更新标签文本。

**目的：** 让玩家在战斗中能直观看到精确的热量数值，而非仅依赖进度条估算。

**技术：** 事件驱动更新，`F0` 格式化取整显示，通过 HeatSystem.MaxHeat 属性访问 HeatStatsSO 配置值。

---

### 🐛 Bugfix: 四家族 Core 发射管线全面修复 — 2026-02-10 13:05

**修改文件：**
- `Assets/Scripts/Combat/StarChart/StarChartController.cs` — **SpawnAnomalyEntity()** 重写：将 Prefab 组件引用改为运行时 AddComponent + JsonUtility 拷贝，确保每个投射物拥有独立的 BoomerangModifier 实例；增加 Modifiers null 安全处理
- `Assets/Scripts/Combat/Projectile/Projectile.cs` — **OnReturnToPool()** 增加动态添加的 Modifier 组件清理（Destroy），防止组件在对象池复用时泄漏累积
- `Assets/Scripts/Combat/Projectile/EchoWave.cs` — **Awake()** 增加 LayerMask 零值 fallback（Enemy/Wall）；增加程序化圆形 Sprite 生成（64x64 白色填充圆），解决 SpriteRenderer 无 Sprite 时完全不可见的问题
- `Assets/Scripts/Combat/Projectile/LaserBeam.cs` — **Awake()** 增加 hitMask 零值 fallback（Enemy+Wall）；调整默认 beam 参数使光束更易观察（duration 0.1→0.15s, fade 0.05→0.1s, startWidth 0.15→0.2, endWidth 0.05→0.08）
- `Assets/Scripts/Combat/StarChart/SnapshotBuilder.cs` — **CollectTintModifiers()** 从可能返回 null 改为始终返回非空 List，消除下游 NullReferenceException 隐患
- `Assets/_Data/StarChart/Prefabs/EchoWave_Echo.prefab` — Layer 从 7(PlayerProjectile) 改为 10(EchoWave)；_enemyMask 从 ~0(所有层) 改为 256(Enemy)；_wallMask 从 0 改为 512(Wall)
- `Assets/_Data/StarChart/Prefabs/LaserBeam_Light.prefab` — Layer 从 0(Default) 改为 7(PlayerProjectile)；_hitMask 从 ~0(所有层) 改为 768(Enemy+Wall)

**问题摘要：**
1. **Anomaly/Boomerang (P0):** `SpawnAnomalyEntity` 从 Prefab 资产上 `GetComponent<IProjectileModifier>()` 获取引用，导致所有投射物共享同一个 Modifier 实例（状态互相覆盖）；且 `coreSnap.Modifiers` 可能为 null 导致 NRE
2. **Echo/Shockwave (P0):** EchoWave Prefab 的 `_enemyMask=~0` 命中所有层（含玩家）；SpriteRenderer 无 Sprite 导致扩展波完全不可见；Prefab Layer 错误
3. **Light/Laser (P1):** LaserBeam Prefab 的 `_hitMask=~0` 射线命中所有层；Prefab Layer=0(Default) 而非 PlayerProjectile；beam 持续时间过短难以观察
4. **SnapshotBuilder (P1):** `CollectTintModifiers()` 在无 Tint 棱镜时返回 null，被 Anomaly 分支直接调用 `.Contains()` 引发 NRE

**技术：** Anomaly modifier 使用 AddComponent + JsonUtility.FromJsonOverwrite 实现运行时组件拷贝（保持序列化字段值与 Prefab 一致）。EchoWave 使用 Texture2D 程序化生成 64x64 白色填充圆作为 SpriteRenderer fallback。所有 LayerMask 在代码和 Prefab 两层均做了正确初始化。

---

### 🐛 Bugfix: LaserBeam 对象池复用后累积透明度衰减 — 2026-02-10 13:12

**修改文件：**
- `Assets/Scripts/Combat/Projectile/LaserBeam.cs` — **OnReturnToPool()** 增加 LineRenderer startColor/endColor 重置为 Color.white

**问题根因：** `OnReturnToPool()` 只重置了 width 未重置 color。fade 阶段将 alpha 降至 ≈0 后，对象回池；下次 `Fire()` 调用时 `_initialStartColor` / `_initialEndColor` 从 LineRenderer 读取的是上次残留的 alpha≈0 值，导致每次发射 alpha 递减，3 次后完全不可见。

**技术：** 在 OnReturnToPool 中将 LineRenderer 颜色重置为 `Color.white`（完全不透明），确保下次 Fire() 读取到正确的初始 alpha=1。

---

### 🧹 Legacy Code & Asset Cleanup — 2026-02-10 13:59

**目的：** 移除被 StarChart 系统完全取代的 legacy 代码文件、过时 SO 资产和空目录，整理资产目录结构。

**删除的代码文件：**
- `Assets/Scripts/Combat/Weapon/WeaponSystem.cs` (+.meta) — 标记 `[Obsolete]`，已被 StarChartController 完全替代
- `Assets/Scripts/Combat/Data/WeaponStatsSO.cs` (+.meta) — 标记 `[Obsolete]`，已被 StarCoreSO 替代

**清除的 legacy 兼容桥：**
- `Assets/Scripts/Combat/Projectile/Projectile.cs` — 移除 `[Obsolete]` 的 `Initialize(Vector2, WeaponStatsSO, ...)` 重载方法及注释中的 WeaponStatsSO 引用
- `Assets/Scripts/Combat/StarChart/ProjectileParams.cs` — 移除 `FromWeaponStats(WeaponStatsSO)` 静态方法及 `#pragma warning disable/restore CS0618`

**删除的 legacy 资产：**
- `Assets/_Data/Weapons/DefaultWeaponStats.asset` (+.meta) — 旧 WeaponStatsSO 实例，无引用
- `Assets/_Prefabs/Projectiles/BasicBullet.prefab` (+.meta) — 旧子弹 Prefab，已被 Projectile_Matter.prefab 替代

**修正 Prefab 引用：**
- `Assets/_Data/StarChart/Cores/StarCore.asset` — `_projectilePrefab` 从 BasicBullet.prefab 改指向 Projectile_Matter.prefab
- `Assets/_Data/StarChart/Cores/TestCore_FastBullet.asset` — 同上

**迁移的 SO 资产（保留 GUID）：**
- `_Data/Weapons/StarCore.asset` → `_Data/StarChart/Cores/StarCore.asset`
- `_Data/Weapons/TestCore_FastBullet.asset` → `_Data/StarChart/Cores/TestCore_FastBullet.asset`
- `_Data/Weapons/TestSpeedSail.asset` → `_Data/StarChart/Sails/TestSpeedSail.asset`（新建 Sails/ 目录）

**迁移的代码文件（保留 GUID，命名空间不变）：**
- `Scripts/Combat/Weapon/WeaponTrack.cs` → `Scripts/Combat/StarChart/WeaponTrack.cs`
- `Scripts/Combat/Weapon/FirePoint.cs` → `Scripts/Combat/StarChart/FirePoint.cs`

**删除的空目录：**
- `Assets/Scripts/Combat/Weapon/` (+.meta)
- `Assets/Scripts/Combat/Data/` (+.meta)
- `Assets/_Data/Weapons/` (+.meta)
- `Assets/_Prefabs/Projectiles/` (+.meta)
- `Assets/_Prefabs/Effects/` (+.meta)
- `Assets/_Data/Enemies/` (+.meta)

**技术：** 文件迁移通过文件系统 Move 操作连同 .meta 文件一起移动，保留 Unity GUID 引用不断裂。StarCoreSO 的 _projectilePrefab 字段通过修改 .asset YAML 中的 fileID 和 guid 完成 Prefab 引用切换。

---

### 🧹 删除早期 Legacy StarCoreSO 资产 — 2026-02-10 14:12

**问题：** 库存中显示 "Basic Bullet" (StarCore.asset) 和 "Basic Bullet2" (TestCore_FastBullet.asset) 两个早期手动创建的 StarCoreSO 资产。这些资产缺少 Batch 5 新增的 `_anomalyModifierPrefab` 序列化字段，且功能已被 Batch 5 自动创建的 `MatterCore_StandardBullet.asset` 完全替代。装备这些旧核心会导致射击功能异常。

**删除的资产文件：**
- `Assets/_Data/StarChart/Cores/StarCore.asset` (+.meta) — "Basic Bullet"，GUID: e804D3b5
- `Assets/_Data/StarChart/Cores/TestCore_FastBullet.asset` (+.meta) — "Basic Bullet2"，GUID: Ff2B3C4E

**修改的文件：**
- `Assets/_Data/StarChart/PlayerInventory.asset` — 从 `_ownedItems` 中移除上述两个旧 GUID 引用（10 项 → 8 项）
- `Assets/_Prefabs/Ship/Ship.prefab` — `_debugPrimaryCores` 从旧双核心改为单个 MatterCore_StandardBullet；`_debugSecondaryCores` 清空（原引用 TestCore_FastBullet）

**目的：** 消除库存中不可用的 legacy 核心，修复因旧 SO 资产字段不完整导致的射击失效问题。

**技术：** 直接编辑 Unity YAML 序列化文件中的 GUID 引用列表，删除前确认全项目无其他引用。

---

### 🐛 修复 Standard Bullet 不可见问题 — 2026-02-10 14:18

**问题：** Standard Bullet (MatterCore) 装备后射击时 heat 正常变化，但看不到子弹飞出。Laser 等其他家族正常。

**根因：** `Projectile_Matter.prefab` 在 Batch 5 由 `Batch5AssetCreator.cs` 自动创建时，`SpriteRenderer` 只设置了颜色 (1, 0.9, 0.3) 但**未分配 Sprite** (`m_Sprite: {fileID: 0}`, `m_WasSpriteAssigned: 0`)。没有 Sprite 的 SpriteRenderer 不会渲染任何像素，导致子弹虽然存在且在移动，但视觉上完全不可见。

**修改的文件：**
- `Assets/_Data/StarChart/Prefabs/Projectile_Matter.prefab` — SpriteRenderer 的 `m_Sprite` 引用改为 `bullet_0` sprite (来自 `Art/Sprites/Projectiles/bullet.png`)；`m_Size` 调整为 (0.08, 0.2)；`m_WasSpriteAssigned` 设为 1。
- `Assets/Scripts/Combat/Projectile/Projectile.cs` — `Awake()` 中新增 fallback 逻辑：若 SpriteRenderer 存在但 sprite 为 null，自动生成程序化圆形 sprite（与 EchoWave.cs 同款），防止未来新建 Matter prefab 时遗漏 sprite 分配。

**目的：** 让物理子弹在屏幕上可见，同时建立防御性 fallback 机制。

**技术：** 直接编辑 prefab YAML 中 SpriteRenderer 的 sprite 引用（fileID + GUID 指向 `bullet.png` 的 `bullet_0` sub-sprite）；Projectile.cs 使用静态缓存的程序化 Texture2D 生成 fallback sprite，零额外开销。

---

### 🔧 复刻旧 BasicBullet 视觉效果到 Standard Bullet — 2026-02-10 14:30

**问题：** 修复 sprite 不可见后，Standard Bullet 视觉效果仍"远不如"旧版 BasicBullet：无拖尾效果、颜色不对（黄色 vs 旧版白色）。

**根因分析（通过 Git 历史对比）：**
- 旧 `BasicBullet.prefab`（已删除）有 **TrailRenderer** 组件：`time=0.15s`，宽度从 `0.085` 锥形渐细到 `0`，颜色白色 `alpha=1 → alpha=0` 淡出
- 旧 SpriteRenderer 颜色为**白色** `(1, 1, 1, 1)`
- Batch 5 自动创建的 `Projectile_Matter.prefab` **完全缺失 TrailRenderer**，且 SpriteRenderer 颜色被设为黄色 `(1, 0.9, 0.3)`

**修改的文件：**
- `Assets/Scripts/Combat/Projectile/Projectile.cs` — `Awake()` 中新增 TrailRenderer 自动配置逻辑：若 prefab 没有 TrailRenderer 则动态添加并调用 `ConfigureTrail()` 方法。新增 `ConfigureTrail()` 静态方法，参数完全复刻旧 BasicBullet：`time=0.15`、宽度曲线 `0.085→0`、白色 alpha 淡出、使用 SpriteRenderer 相同材质（URP 兼容）。
- `Assets/_Data/StarChart/Prefabs/Projectile_Matter.prefab` — SpriteRenderer 颜色从黄色 `(1, 0.9, 0.3)` 恢复为白色 `(1, 1, 1, 1)`，与旧 BasicBullet 一致。

**目的：** 让 Standard Bullet 的视觉效果（拖尾、颜色）与旧版 BasicBullet 完全一致。

**技术：** TrailRenderer 在 `Awake()` 中动态创建配置而非手写 YAML，避免复杂的 prefab 序列化编辑且自带 fallback 保护。Trail 使用 SpriteRenderer 的 `sharedMaterial` 确保在 URP 2D 管线下正确渲染。

---

## Tint Modifier 安全实例化 — 2026-02-10 15:20

**背景：** Tint 家族棱镜的 `IProjectileModifier`（如 `BounceModifier`、`SlowOnHitModifier`）在 `SnapshotBuilder` 中直接从 prefab 上 `GetComponent<IProjectileModifier>()` 获取组件实例，导致所有核心和所有子弹共享同一个 prefab 引用。有状态的 modifier（如 `BounceModifier._remainingBounces`）会在多颗子弹间产生竞态污染。修复方案将 Tint modifier 统一为与 Anomaly 家族相同的运行时实例化策略（`AddComponent` + `JsonUtility.FromJsonOverwrite`），确保每颗子弹拥有独立的 modifier 实例。

**修改的文件：**
- `Assets/Scripts/Combat/StarChart/FiringSnapshot.cs` — `CoreSnapshot.Modifiers`（`List<IProjectileModifier>`）重命名为 `TintModifierPrefabs`（`List<GameObject>`），存储 prefab 引用而非组件实例。
- `Assets/Scripts/Combat/StarChart/SnapshotBuilder.cs` — `CollectTintModifiers()` 重命名为 `CollectTintModifierPrefabs()`，返回类型从 `List<IProjectileModifier>` 改为 `List<GameObject>`，收集 `PrismSO.ProjectileModifierPrefab` 引用。`BuildCoreSnapshot()` 参数和赋值相应更新。
- `Assets/Scripts/Combat/StarChart/StarChartController.cs` — 新增 `InstantiateModifiers(GameObject targetObj, List<GameObject> prefabs)` 共享工具方法，遍历 prefab 列表对每个 `IProjectileModifier` 组件执行 `AddComponent` + `JsonUtility.FromJsonOverwrite` 创建独立实例。`SpawnMatterProjectile()`、`SpawnLightBeam()`、`SpawnEchoWave()` 改为调用 `InstantiateModifiers()` 获取独立 modifier 列表。`SpawnAnomalyEntity()` 重构为统一使用 `InstantiateModifiers()`，分别处理 Tint 和 Anomaly prefab 后合并结果。
- `Assets/Scripts/Combat/Projectile/LaserBeam.cs` — `OnReturnToPool()` 新增动态 modifier 组件清理逻辑（遍历 `_modifiers` 列表，对 `MonoBehaviour` 类型调用 `Destroy`），防止对象池复用时组件泄漏。
- `Assets/Scripts/Combat/Projectile/EchoWave.cs` — `OnReturnToPool()` 新增相同的动态 modifier 组件清理逻辑。

**目的：** 消除 Tint modifier 共享引用导致的有状态 modifier 竞态污染问题，统一四大家族（Matter/Light/Echo/Anomaly）的 modifier 注入策略为运行时实例化。

**技术：** `AddComponent` + `JsonUtility.FromJsonOverwrite` 运行时深拷贝模式（与 Anomaly 家族一致）。`OnReturnToPool()` 中通过 `is MonoBehaviour mb` 模式匹配识别动态组件并 `Destroy`，与 `Projectile.OnReturnToPool()` 已有的清理模式保持一致。

---

## 敌人 AI 基础框架 (Enemy AI Foundation) — 2026-02-10 16:08

### 概述

实现《静默方舟》Phase 1 敌人 AI 基础框架，使用分层状态机 (HFSM) 作为大脑层，配合 GDD 定义的三层架构（躯壳 Body / 大脑 Brain / 导演 Director）。本阶段完成躯壳层、大脑层（HFSM）、基础感知系统、数据驱动配置和第一个可玩原型：莽夫型 (The Rusher)。

### 新建文件

**核心接口与数据：**
- `Assets/Scripts/Combat/IDamageable.cs` — 通用伤害接口，定义 `TakeDamage(float, Vector2, float)` 和 `bool IsAlive`，供子弹/激光/震荡波等攻击源统一调用
- `Assets/Scripts/Combat/Enemy/EnemyStatsSO.cs` — 敌人数据配置 ScriptableObject，包含身份/生命/移动/攻击/攻击阶段/感知/脱战/表现等全部字段，使用 `[Header]` 分组和 `[Min]` 约束

**HFSM 状态机框架：**
- `Assets/Scripts/Combat/Enemy/FSM/IState.cs` — 状态接口，定义 `OnEnter`/`OnUpdate`/`OnExit` 三个生命周期方法
- `Assets/Scripts/Combat/Enemy/FSM/StateMachine.cs` — 纯 C# 状态机类，支持 `Initialize`/`Tick`/`TransitionTo`，转移顺序 OnExit→更新引用→OnEnter，支持嵌套（子状态机）

**躯壳层：**
- `Assets/Scripts/Combat/Enemy/EnemyEntity.cs` — MonoBehaviour，实现 `IDamageable` + `IPoolable`。处理 HP/受击反馈（闪白）/击退/死亡流程/移动执行/对象池生命周期

**感知系统：**
- `Assets/Scripts/Combat/Enemy/EnemyPerception.cs` — MonoBehaviour，实现视觉（扇形 + LoS Raycast，5Hz 定频）和听觉（订阅 `StarChartController.OnWeaponFired` 事件 + 距离校验）感知，支持记忆衰减

**大脑层：**
- `Assets/Scripts/Combat/Enemy/EnemyBrain.cs` — MonoBehaviour，持有外层 HFSM，在 Start 构建状态机，Update 中 Tick。暴露 Entity/Perception/Stats/SpawnPosition 供状态访问。Editor 模式下 OnGUI 显示当前状态名

**外层战术状态：**
- `Assets/Scripts/Combat/Enemy/States/IdleState.cs` — 待机状态，检测 HasTarget 转 Chase
- `Assets/Scripts/Combat/Enemy/States/ChaseState.cs` — 追击状态，直线追击目标，距离判断转 Engage/Return
- `Assets/Scripts/Combat/Enemy/States/EngageState.cs` — 交战状态，持有内层攻击子状态机
- `Assets/Scripts/Combat/Enemy/States/ReturnState.cs` — 回归状态，移动回出生点并恢复满血

**内层攻击子状态（信号-窗口模型）：**
- `Assets/Scripts/Combat/Enemy/States/TelegraphSubState.cs` — 前摇：停止移动，sprite 变红警告，计时后转 Attack
- `Assets/Scripts/Combat/Enemy/States/AttackSubState.cs` — 出招：OverlapCircle 生成伤害区域，不可转向（Commitment），通过 IDamageable 对玩家造成伤害
- `Assets/Scripts/Combat/Enemy/States/RecoverySubState.cs` — 恢复：硬直（玩家反击窗口），计时后标记攻击循环完成

**Editor 工具：**
- `Assets/Scripts/Combat/Editor/EnemyAssetCreator.cs` — 菜单 `ProjectArk > Create Rusher Enemy Assets`，一键创建 EnemyStats_Rusher.asset 并在 Console 输出 Prefab 组装指南

### 修改文件

**子弹系统 IDamageable 集成：**
- `Assets/Scripts/Combat/Projectile/Projectile.cs` — `OnTriggerEnter2D` 中的 TODO 注释替换为 `GetComponent<IDamageable>()?.TakeDamage()` 调用
- `Assets/Scripts/Combat/Projectile/LaserBeam.cs` — `Fire()` 中的 placeholder `Debug.Log` 替换为 `IDamageable.TakeDamage()` 调用
- `Assets/Scripts/Combat/Projectile/EchoWave.cs` — `OnTriggerEnter2D` 中的 placeholder `Debug.Log` 替换为 `IDamageable.TakeDamage()` 调用

**开火事件广播：**
- `Assets/Scripts/Combat/StarChart/StarChartController.cs` — 新增 `public static event Action<Vector2, float> OnWeaponFired`，在 SpawnMatterProjectile/SpawnLightBeam/SpawnEchoWave/SpawnAnomalyEntity 中广播（位置 + 15f 噪音半径），供敌人听觉感知订阅

### 架构说明

```
┌─────────────────────────────────────────────────┐
│              AI Brain (HFSM)                     │
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
│           EnemyEntity (躯壳层)                    │
│  移动执行 · HP/韧性 · 受击反馈 · 对象池            │
└─────────────────────────────────────────────────┘
```

**设计原则：**
- 数据驱动：所有数值通过 EnemyStatsSO 配置，禁止 hardcode
- HFSM 纯 C# 类（非 MonoBehaviour），可单元测试，零 GC
- 信号-窗口攻击模型：Telegraph(读取信号) → Attack(承诺窗口) → Recovery(惩罚窗口)
- 子弹通过 IDamageable 接口统一造伤，解耦攻击源与目标类型
- 感知系统事件驱动（听觉）+ 定频轮询（视觉 5Hz），平衡性能与响应性

**技术：** HFSM 分层状态机, IDamageable 接口多态, Physics2D.OverlapCircle 扇形检测, C# event 事件通信, ScriptableObject 数据驱动, IPoolable 对象池集成。

### Prefab 组装指南（需手动在 Editor 中完成）

1. 创建空 GameObject `Enemy_Rusher`，设置 Layer 为 `Enemy`（需在 TagManager 中创建）
2. 添加组件：`EnemyEntity` + `EnemyBrain` + `EnemyPerception`
3. Rigidbody2D：Dynamic, Gravity Scale=0, Freeze Rotation Z, Interpolate
4. CircleCollider2D：Radius=0.4
5. SpriteRenderer：分配占位 sprite
6. 拖入 `EnemyStats_Rusher` SO 到 EnemyEntity 和 EnemyPerception 的 `_stats` 字段
7. EnemyPerception：Player Mask=Player 层, Obstacle Mask=Wall 层
8. Physics2D 碰撞矩阵：PlayerProjectile↔Enemy=ON, Player↔Enemy=ON
9. 保存为 Prefab 到 `Assets/_Prefabs/Enemies/Enemy_Rusher.prefab`

---

## Enemy AI 系统集成 — 玩家受伤 + 敌人生成闭环 (2026-02-10 23:30)

### 概要

将已实现的 Enemy AI 代码框架正式接入游戏循环，完成三个关键缺失环节：
1. 玩家飞船实现 `IDamageable` 接口，使敌人攻击能造成伤害
2. 创建 `EnemySpawner` 组件，通过对象池管理敌人运行时生成
3. 场景配置与完整战斗循环验证

### 新建文件

- `Assets/Scripts/Ship/Combat/ShipHealth.cs` — 玩家飞船生命值组件，实现 `IDamageable` 接口
  - 命名空间 `ProjectArk.Ship`，使用 `[RequireComponent(typeof(Rigidbody2D))]`
  - 从 `ShipStatsSO.MaxHP` 读取初始 HP（数据驱动，不硬编码）
  - `TakeDamage(float, Vector2, float)` 流程：死亡判断 → 扣减 HP → Rigidbody2D.AddForce(Impulse) 击退 → 协程闪白 → 触发 OnDamageTaken 事件 → HP ≤ 0 调用 Die()
  - `Die()` 流程：标记 `_isDead = true` → 禁用 InputHandler → 触发 OnDeath 事件（Game Over/重生逻辑暂未实现，预留事件订阅点）
  - 暴露 `OnDamageTaken(float damage, float currentHP)` 和 `OnDeath` 两个 Action 事件供 UI/其他系统订阅
  - 提供 `ResetHealth()` 公共方法用于未来重生/关卡重启

- `Assets/Scripts/Combat/Enemy/EnemySpawner.cs` — 敌人生成管理器
  - 命名空间 `ProjectArk.Combat.Enemy`
  - Inspector 暴露字段：`_enemyPrefab`(GameObject)、`_spawnPoints`(Transform[])、`_maxAlive`(int, 默认3)、`_spawnInterval`(float, 默认5s)、`_initialSpawnCount`(int, 默认1)、`_poolPrewarmCount`(int, 默认5)、`_poolMaxSize`(int, 默认10)
  - Start() 中创建 `GameObjectPool` 并执行初始生成（`_initialSpawnCount` 个，不超过 `_maxAlive`）
  - `SpawnEnemy()` 流程：检查存活上限 → Round-robin 选取 SpawnPoint → Pool.Get() → EnemyBrain.ResetBrain() 重置 AI → 订阅 EnemyEntity.OnDeath → 更新 _aliveCount
  - 敌人死亡回调：_aliveCount-- → 协程延迟 _spawnInterval 秒后补充生成
  - 注意：EnemyEntity.OnReturnToPool() 会清空事件订阅者，因此每次从池中取出时重新订阅 OnDeath
  - Editor Gizmos：选中时绘制绿色连线 + 圆圈标记刷怪点

### 修改文件

- `Assets/Scripts/Ship/Data/ShipStatsSO.cs` — 新增 `[Header("Survival")]` 区域
  - 添加 `_maxHP`(float, 默认100) 和 `_hitFlashDuration`(float, 默认0.1s) 字段
  - 添加对应公共属性 `MaxHP` 和 `HitFlashDuration`
  - 不影响现有 Movement/Aiming 参数，向后兼容 `DefaultShipStats.asset`

### 场景配置（手动在 Unity Editor 中完成）

- 飞船 GameObject 挂载 `ShipHealth` 组件，绑定 `DefaultShipStats` SO
- 创建 `EnemySpawner` GameObject 挂载 EnemySpawner 组件，含 `SpawnPoint_1`(-8,-5)、`SpawnPoint_2`(8,5) 两个子物体
- EnemySpawner 配置：enemyPrefab=Enemy_Rusher.prefab, maxAlive=3, spawnInterval=5s, initialSpawnCount=1, poolPrewarm=5, poolMax=10

### 问题修复

- 修复 `.meta` 文件 GUID 格式错误：`IDamageable.cs.meta`、`ShipHealth.cs.meta`、`EnemySpawner.cs.meta` 三个文件的 GUID 包含非法破折号（手动编造导致），删除后由 Unity 重新生成正确格式的 meta 文件

### Play Mode 验证结果（Editor Log 确认）

- ✅ 零编译错误（`error CS` 搜索无结果）
- ✅ 三个 meta GUID 问题已修复（`does not have a valid GUID` 搜索无结果）
- ✅ EnemySpawner 正常生成敌人：`[EnemySpawner] Spawned enemy at (8.00, 5.00, 0.00). Alive: 1/3`
- ✅ 完整伤害链路通畅：`Projectile.OnTriggerEnter2D` → `EnemyEntity.TakeDamage` → `EnemyEntity.Die` → `EnemySpawner.OnEnemyDied`
- ✅ 重生机制运作：死亡后延迟重生 `[EnemySpawner] Spawned enemy at (-8.00, -5.00, 0.00). Alive: 1/3`
- ✅ 无运行时 NullReferenceException
- ✅ Asset Pipeline 正常：scripts=1544, non-scripts=3163

### 架构说明

```
┌──────────────────────────────────────────────────────────┐
│                    EnemySpawner                           │
│  GameObjectPool → Get/Return → EnemyBrain.ResetBrain()   │
│  OnDeath 订阅 → _aliveCount 管理 → 延迟重生协程          │
└───────────────────────┬──────────────────────────────────┘
                        │ 生成
┌───────────────────────▼──────────────────────────────────┐
│               Enemy (Rusher)                              │
│  EnemyBrain(HFSM) → EngageState → AttackSubState         │
│                                      │                    │
│                          Physics2D.OverlapCircleAll       │
│                          LayerMask("Player")              │
│                                      │                    │
│                          GetComponent<IDamageable>()      │
│                                      │                    │
│                          TakeDamage(dmg, dir, force)      │
└──────────────────────────────────────┬───────────────────┘
                                       │ 攻击
┌──────────────────────────────────────▼───────────────────┐
│               Player Ship                                 │
│  ShipHealth : IDamageable                                 │
│  HP 扣减 → 击退(Impulse) → 闪白 → OnDamageTaken          │
│  HP ≤ 0 → Die() → 禁用 InputHandler → OnDeath            │
│                                                           │
│  ◄─── Projectile.OnTriggerEnter2D ───►                   │
│  子弹命中敌人 → EnemyEntity.TakeDamage → 敌人死亡 → 回池   │
└──────────────────────────────────────────────────────────┘
```

**双向伤害循环：** 敌人通过 `AttackSubState.TryHitPlayer()` + `Physics2D.OverlapCircleAll` 检测 Player Layer → `IDamageable.TakeDamage()` 伤害飞船；飞船通过 `Projectile.OnTriggerEnter2D` 检测 Enemy Layer → `IDamageable.TakeDamage()` 伤害敌人。两个方向统一使用 `IDamageable` 接口，完全解耦。

**技术：** IDamageable 接口多态, GameObjectPool 对象池, C# event 事件通信, Rigidbody2D.AddForce(Impulse) 击退, 协程闪白反馈, ScriptableObject 数据驱动, Round-robin 刷怪点选择。

---

## 飞船血条 HUD (Ship Health Bar UI) — 2026-02-11 13:30

### 新建文件

- `Assets/Scripts/UI/HealthBarHUD.cs` — 飞船血条 HUD 组件

**内容：** 遵循与 HeatBarHUD 相同的 `Bind()` 事件驱动模式。通过 `Bind(ShipHealth)` 注入引用，订阅 `OnDamageTaken` 和 `OnDeath` 事件。功能包括：
- 填充条（`Image.Filled`）+ `Gradient` 渐变色（绿→黄→红）
- 受击闪烁动画（红色闪光叠加层，淡出）
- 数值标签（`HP {current}/{max}`）
- 低血量脉冲警告（HP ≤ 30% 时填充条 alpha 脉冲）
- 死亡状态显示 `"DESTROYED"` 文字

### 修改文件

- `Assets/Scripts/UI/UIManager.cs` — 新增 `_healthBarHUD` 字段，`Start()` 中自动查找 `ShipHealth` 并调用 `_healthBarHUD.Bind(shipHealth)`
- `Assets/Scripts/UI/Editor/UICanvasBuilder.cs` — `BuildUICanvas()` 中新增 Step 2b，自动创建 HealthBarHUD 层级（Background + FillImage + DamageFlash + Label），自动连线所有 SerializeField 引用

### UI 布局

| 元素 | 锚点位置 | 说明 |
|------|---------|------|
| HealthBarHUD | 左上 (0.02, 0.92) ~ (0.28, 0.97) | 始终可见 |
| HeatBarHUD | 底部居中 (0.3, 0) ~ (0.7, 0.05) | 始终可见 |

**目的：** 战斗中显示飞船生命值状态，与热量条共同构成核心战斗 HUD。

**技术：** 事件驱动 UI 更新（C# event 订阅），Gradient 渐变色映射，`Time.unscaledDeltaTime` 动画（兼容 timeScale=0），Bind/Unbind 生命周期管理。

---

## HUD Gradient 修复 & UICanvasBuilder 完善 — 2026-02-11 14:09

### 修改文件

- `Assets/Scripts/UI/Editor/UICanvasBuilder.cs`

**问题：** HeatBarHUD 的 `_heatGradient` 和 HealthBarHUD 的 `_healthGradient` 字段类型为 `Gradient`（值类型），无法用 `WireField`（基于 `objectReferenceValue`）注入，导致 Builder 创建的 HUD 渐变效果缺失——填充条始终保持初始颜色不变。

**修复内容：**
1. 新增 `WireGradient()` helper 方法，使用 `SerializedProperty.gradientValue` 正确注入 `Gradient` 值
2. 新增 `CreateHeatGradient()`：绿(0%) → 黄(50%) → 红(100%)，用于热量条
3. 新增 `CreateHealthGradient()`：红(0%) → 黄(40%) → 绿(100%)，用于血条
4. 在 HeatBarHUD 和 HealthBarHUD 的 Wire 阶段分别调用注入

**补充说明：** StarChartPanel 在 Builder 的 Step 6 中被 `SetActive(false)` 是刻意设计，星图面板默认隐藏，由 `UIManager.Toggle()` 控制开关。

**技术：** `SerializedProperty.gradientValue` API（Unity Editor only），`GradientColorKey`/`GradientAlphaKey` 程序化创建渐变。

---

## 莽夫近战修复 + 射手型敌人实现 (2026-02-11 14:16)

### 问题修复：莽夫型敌人近战攻击不造成伤害

**根因分析：** Ship Prefab 根 GameObject 缺少 `Collider2D`，导致 `AttackSubState.TryHitPlayer()` 中的 `Physics2D.OverlapCircleAll` 无法检测到玩家。`ShipHealth` 虽已实现 `IDamageable`，但物理系统需要 Collider 才能被 OverlapCircle 捕获。

**修复文件：**
- `Assets/Scripts/Ship/Combat/ShipHealth.cs` — 添加 `[RequireComponent(typeof(Collider2D))]`，确保 Ship 必有碰撞体

### 新增：射手型敌人 (Shooter)

射手型是远程攻击型敌人，与莽夫型形成战术互补：莽夫冲锋近战施压，射手保持距离输出弹幕。

**行为状态图：**
```
Idle → [发现目标] → Chase → [进入 PreferredRange] → Shoot (Telegraph→Burst Fire→Recovery)
                                                        ↓ 玩家过近 (< RetreatRange)
                                                    Retreat (边退边保持朝向)
                                                        ↓ 恢复安全距离
                                                      Shoot (继续射击)
                                                        ↓ 超出 LeashRange / 丢失目标
                                                      Return → Idle
```

**新建文件：**
| 文件 | 用途 |
|------|------|
| `Assets/Scripts/Combat/Enemy/EnemyProjectile.cs` | 敌人子弹组件（检测 Player 层，忽略 Enemy 层，对象池回收） |
| `Assets/Scripts/Combat/Enemy/ShooterBrain.cs` | 射手型大脑层（继承 EnemyBrain，override BuildStateMachine 组装远程 HFSM） |
| `Assets/Scripts/Combat/Enemy/States/ShootState.cs` | 远程攻击状态（Telegraph 红闪→Burst 连射→Recovery 硬直，内嵌子状态机） |
| `Assets/Scripts/Combat/Enemy/States/RetreatState.cs` | 后撤状态（远离玩家至 PreferredRange，超出 LeashRange 转 Return） |

**修改文件：**
| 文件 | 变更 |
|------|------|
| `Assets/Scripts/Combat/Enemy/EnemyStatsSO.cs` | 新增 Ranged Attack 字段组：ProjectilePrefab, ProjectileSpeed, ProjectileDamage, ProjectileKnockback, ProjectileLifetime, ShotsPerBurst, BurstInterval, PreferredRange, RetreatRange |
| `Assets/Scripts/Combat/Enemy/EnemyBrain.cs` | 字段改 protected、方法改 virtual，支持 ShooterBrain 继承 |
| `Assets/Scripts/Combat/Enemy/States/ChaseState.cs` | 添加 ShooterBrain 多态分支：射手型进入 ShootState 而非 EngageState |
| `Assets/Scripts/Combat/Editor/EnemyAssetCreator.cs` | 新增 `Create Shooter Enemy Assets` 菜单项 + `CreateEnemyProjectilePrefab()` 辅助方法 |

**射手型 SO 预设数值（EnemyStats_Shooter）：**
| 分类 | 字段 | 值 | 设计意图 |
|------|------|----|----------|
| Health | MaxHP | 40 | 脆皮，鼓励玩家优先击杀 |
| Movement | MoveSpeed | 3.5 | 比莽夫慢，无法轻易逃脱 |
| Ranged | ProjectileSpeed | 10 | 中速弹，可闪避 |
| | ShotsPerBurst | 3 | 三连发，间隔 0.2s |
| | PreferredRange | 10 | 理想射击距离 |
| | RetreatRange | 5 | 玩家逼近此距离时后撤 |
| Perception | SightRange | 16 | 远视距补偿远程定位 |
| | SightAngle | 90° | 宽视锥，更容易发现玩家 |
| Visual | BaseColor | (0.2, 0.4, 0.9) | 冷蓝色，与莽夫红色区分 |

**架构决策：**
- `ShooterBrain` 继承 `EnemyBrain` 而非独立实现，复用 Idle/Chase/Return 共享状态
- `EnemyProjectile` 独立于玩家 `Projectile`，避免星图修改器系统的不必要耦合
- `ShootState` 内嵌 Telegraph→Burst→Recovery 子状态机，复用信号-窗口设计模式
- `ChaseState` 通过 `brain is ShooterBrain` 多态判断决定转入 Shoot 还是 Engage

**技术：** 继承 + 虚方法 override（EnemyBrain→ShooterBrain），PoolManager 对象池（EnemyProjectile），Physics2D.OverlapCircleAll + LayerMask（碰撞检测）。

---

### StarChart Component Data — 示巴星 & 机械坟场部件设计写入 (2026-02-11 15:19)

根据 `StarChartPlanning.csv` 对示巴星（新手期）和机械坟场（拓展期）的规划，完成了4个星图部件数据表的批量写入。

**修改文件：**
| 文件 | 变更 |
|------|------|
| `Docs/DataTables/StarChart_StarCores.csv` | 新增7个星核 (ID 1006–1012)：散弹、棱光射线、脉冲新星、转管炮、布雷器、裂变光束、余震 |
| `Docs/DataTables/StarChart_Prisms.csv` | 新增12个棱镜 (ID 2006–2017)：连射、重弹、远射、三连发、冲击、轻载、灼烧、反弹、齐射、减速弹、微缩、腐蚀 |
| `Docs/DataTables/StarChart_LightSails.csv` | 新增4个光帆 (ID 3005–3008)：标准航行帆、斥候帆、重装帆、脉冲帆 |
| `Docs/DataTables/StarChart_Satellites.csv` | 新增2个伴星 (ID 4005–4006)：自动机炮、拾取磁铁 |

**示巴星新增（10件）：**
- StarCores ×1：实相·风暴散射 (近距离扇形散射×5)
- Prisms ×6：连射/重弹/远射/三连发/冲击/轻载（基础数值类，教会玩家棱镜 trade-off）
- LightSails ×2：标准航行帆(无效果基准线) / 斥候帆(高速加伤)
- Satellites ×1：自动机炮(OnInterval 自动开火)

**机械坟场新增（15件）：**
- StarCores ×6：棱光射线/脉冲新星/转管炮(2格)/布雷器/裂变光束(2格三叉)/余震（元素类，流派分化）
- Prisms ×6：灼烧(DoT)/反弹(碰墙3次)/齐射(重叠双弹)/减速弹/微缩/腐蚀(降防)（手感类，空间控制维度）
- LightSails ×2：重装帆(站桩减伤,禁冲刺) / 脉冲帆(冲刺造伤)
- Satellites ×1：拾取磁铁(OnAlways 吸引掉落物)

**设计哲学：**
- 示巴星部件全部为简单数值修正，降低新手认知负荷
- 机械坟场引入攻防流派分化：重装帆(阵地战) vs 脉冲帆(游击战)
- 数值以撕裂者 DPS(120) 为基准锚定，2格武器提供约110%效率但占用更多槽位
- 棱镜 AddHeat 可为负值（轻载/微缩），作为低复杂度选项的奖励机制

**部件总数：** StarCores 12 / Prisms 17 / LightSails 8 / Satellites 6 = 共43件

---

## Enemy_Bestiary.CSV 配置表重设计 — 2026-02-11 16:55

将 `Enemy_Bestiary.csv` 从 15 列扩展为 **50 列**的完整配置表，使其成为敌人数据的唯一权威数据源 (Single Source of Truth)，支持 CSV → SO 自动化导入管线。

### 新建文件

| 文件 | 说明 |
|------|------|
| `Assets/Scripts/Combat/Editor/BestiaryImporter.cs` | Editor 工具脚本，提供 `ProjectArk > Import Enemy Bestiary` 菜单项，一键从 CSV 批量生成/更新 `EnemyStatsSO` 资产 |

### 修改文件

| 文件 | 变更 |
|------|------|
| `Assets/Scripts/Combat/Enemy/EnemyStatsSO.cs` | 新增 10 个字段：5 个抗性 (`Resist_Physical/Fire/Ice/Lightning/Void`, Range 0~1)、`DropTableID` (string)、`PlanetID` (string)、`SpawnWeight` (float)、`BehaviorTags` (List\<string\>)；新增 `using System.Collections.Generic` |
| `Docs/DataTables/Enemy_Bestiary.csv` | 从 15 列重建为 50 列（12 个分组 A~L），已有 6 行敌人数据完整迁移并补填缺失数值 |

### 配置表结构（50 列 × 12 分组）

- **A. 身份与元数据** (7列)：ID, InternalName, DisplayName, Rank, AI_Archetype, FactionID, PlanetID
- **B. 生命与韧性** (2列)：MaxHP, MaxPoise
- **C. 移动** (2列)：MoveSpeed, RotationSpeed
- **D. 近战攻击** (4列)：AttackDamage, AttackRange, AttackCooldown, AttackKnockback
- **E. 攻击阶段** (3列)：TelegraphDuration, AttackActiveDuration, RecoveryDuration
- **F. 远程攻击** (9列)：ProjectilePrefab ~ RetreatRange
- **G. 感知** (3列)：SightRange, SightAngle, HearingRange
- **H. 栓绳与记忆** (2列)：LeashRange, MemoryDuration
- **I. 抗性** (5列)：Resist_Physical/Fire/Ice/Lightning/Void
- **J. 奖励与掉落** (2列)：ExpReward, DropTableID
- **K. 视觉反馈** (5列)：HitFlashDuration, BaseColor_R/G/B/A, PrefabPath
- **L. 行为标签与设计备注** (5列)：BehaviorTags, SpawnWeight, DesignIntent, PlayerCounter, Description_Note

### BestiaryImporter 功能

- **菜单入口**：`ProjectArk > Import Enemy Bestiary`
- **CSV 解析**：支持逗号分隔、引号转义（双引号 `""` 语法）
- **字段映射**：显式映射 30+ 个 SO 字段，自动跳过纯策划列 (Rank, AI_Archetype, FactionID, ExpReward, DesignIntent, PlayerCounter, *_Note)
- **特殊处理**：BaseColor_RGBA 四列合并为 Color、ProjectilePrefab 路径转 GameObject 引用、BehaviorTags 分号分隔拆分为 List\<string\>
- **空字段策略**：CSV 为空时保留 SO 默认值不覆盖
- **报告**：导入完成后弹窗汇总 Created / Updated / Skipped 数量及耗时

### 数据迁移

- 6 行已有敌人 (ID 5001–5006) 完整迁移
- 旧列名映射：`MaxHealth` → `MaxHP`、`Poise` → `MaxPoise`、`AggroRange` → `SightRange`、`Description` → `Description_Note`
- 缺失数值参考 `EnemyAssetCreator.cs` 中 Rusher/Shooter 预设补填

---

## Enemy_Bestiary 怪物数据填充 — P1 示巴星 & P2 机械坟场 (2026-02-11 17:15)

### 概述

完成 `Enemy_Bestiary.csv` 的完整怪物数据填充，覆盖 P1 示巴星（废弃矿坑）和 P2 机械坟场（剧毒沼泽）两个星球的全部怪物。从 6 行原型数据扩展至 26 行完整配置。

### 修改文件

| 文件 | 变更内容 |
|------|----------|
| `Docs/DataTables/Enemy_Bestiary.csv` | 完善 5001–5006 共 6 行已有数据（更新 DisplayName、补齐 DesignIntent/PlayerCounter/Description_Note 等策划列）；新增 5007–5026 共 20 行怪物数据 |

### P1 示巴星怪物（ID 5001–5014，共 14 种）

**已有数据完善（5001–5006）：**
- 5001 深渊爬行者：确认为最基础 Minion，EXP=5，无抗性
- 5002 装甲爬行者：Elite 定位，物理抗性 0.3，EXP=20
- 5003 更名为"工蜂无人机"：匹配 EnemyPlanning 规划，Ranged_Kiter 远程基础单位
- 5004 更名为"天花板钻头"：Stationary_Turret 固定型，MoveSpeed=0，垂直攻击
- 5005 暗影潜伏者：Ambusher 刺客型，添加 Invisible 标签，虚空抗性 0.3
- 5006 更名为"锈蚀女王"：Boss 定位，多阶段 AI，SuperArmor 霸体

**新增怪物（5007–5014）：**
- 5007 酸蚀爬行者 (Minion)：死亡留酸液，战场分割者
- 5008 自爆蜱虫 (Minion)：HP=15 极脆 + Speed=7 极快 + 自爆伤害 30
- 5009 重型装载机 (Defense)：正面无敌 FrontShield，HP=100，绕背教学怪
- 5010 晶体甲虫 (Specialist)：反射激光 ReflectLaser，武器克制教学
- 5011 修理博比特 (Support)：零攻击纯治疗 Healer，击杀优先级教学
- 5012 暴走工头 (Elite)：HP=180 霸体冲撞 + 召唤爬行者，P1 综合考题
- 5013 盗矿地精 (Gimmick)：Speed=8 逃跑型，零攻击高 EXP=50，贪婪测试
- 5014 挖掘者 9000 (Mini-Boss)：HP=1500 游荡型，冲撞+落石+激光扫射

### P2 机械坟场怪物（ID 5015–5026，共 12 种）

**数值升级原则：** 同级别比 P1 提升 30%–50%

- 5015 机械猎犬 (Minion)：Melee_Flanker 侧翼包抄 + 死亡自爆
- 5016 狙击炮塔 (Ranged)：Stationary_Turret 不可移动，射程=20 极远，伤害=40
- 5017 迫击炮手 (Ranged)：Ranged_Lobber 抛物线榴弹 + 燃烧区域 AreaDenial
- 5018 方阵兵 (Defense)：Shield_Wall 结对激光墙 LaserWall;Paired
- 5019 磁力钩锁 (Specialist)：Ranged_Utility 强制拉近 ForcedPull + 晕眩 Stun
- 5020 干扰水晶 (Specialist)：Stationary_Aura 不可移动，封印技能 SkillJam
- 5021 隐形猎手 (Specialist)：Ambusher 光学迷彩 + 背刺暴击 CritStrike
- 5022 感应地雷 (Hazard)：Stationary_Trap HP=10 极脆，爆炸伤害=35，可引诱怪物踩踏
- 5023 反应装甲兵 (Specialist)：Counter_Attacker 受击反弹导弹 DamageReflect，射速惩罚
- 5024 电磁处刑者 (Elite)：Melee_Teleporter 瞬移连击 + 沉默 Silence，HP=150
- 5025 焚化炉 (Boss)：HP=3000 全屏高温 DOT + 召唤废料雨，DPS 检测
- 5026 游荡者 (Mini-Boss)：HP=1200 狙击风筝 + 逃跑回血 Regen，反向风筝体验

### 设计哲学

**P1 示巴星 — 教学生态：**
- 炮灰层（爬行者/蜱虫）→ 远程层（无人机/钻头）→ 防御层（装载机）→ 特化层（甲虫/博比特）→ 精英层（工头）→ Boss 层（锈蚀女王/挖掘者）
- 每种怪物承担一个明确的教学职责，循序渐进引导玩家掌握战斗语法

**P2 机械坟场 — 组合升级：**
- 引入"组合威胁"设计（磁力钩锁+自爆怪、方阵兵+迫击炮后排）
- 引入"流派检测"设计（反应装甲兵惩罚高射速、干扰水晶封印技能流）
- Boss 哲学差异化（焚化炉=DPS 检测 vs 游荡者=机动性检测）

### 数值规范遵循

- 信号-窗口模型：前摇 ≥ 判定窗口 ≥ 后摇
- 感知规则：HearingRange > SightRange，Boss SightAngle=360
- 抗性规则：Minion 无抗性、Elite 特色抗性、Boss 均匀中等抗性+主题弱点
- EXP 阶梯：Minion 5–15 / Elite 20–50 / Mini-Boss 100–200 / Boss 500+
- Boss/Mini-Boss：LeashRange=999（不脱战）、MemoryDuration=999（不遗忘）

---

## Phase 1 遗留项补完：韧性 (Poise) + 顿帧 (HitStop) + 群聚算法 (Boids) — 2026-02-11 19:30

### 概述

补完敌人 AI Phase 1 的三个遗留项，完善战斗手感与多敌人体验。

### 新建文件

- `Assets/Scripts/Core/HitStopEffect.cs` — 全局顿帧效果单例
  - 通过 `[RuntimeInitializeOnLoadMethod]` 自动创建持久 `[HitStop]` 对象
  - 静态 API `HitStopEffect.Trigger(float duration)` 供全局调用
  - 暂停 `Time.timeScale` 指定真实时间秒后恢复，使用 `Time.unscaledDeltaTime` 计时
  - 安全保护：星图面板已暂停时跳过、重叠调用取最长、销毁时恢复 timeScale

- `Assets/Scripts/Combat/Enemy/States/StaggerState.cs` — 韧性击破硬直状态
  - 敌人停止移动，sprite 变黄色（与前摇红色区分）
  - 水平震动视觉反馈（`Sin(Time * 40) * 0.06` 强度）
  - 持续 `EnemyStatsSO.StaggerDuration` 秒后重置韧性，转 Chase/Idle
  - 退出时恢复位置和颜色

### 修改文件

| 文件 | 变更 |
|------|------|
| `Assets/Scripts/Combat/Enemy/EnemyStatsSO.cs` | 新增 `[Header("Poise & Stagger")]` 区域，添加 `StaggerDuration`(float, 默认1.0s) 字段 |
| `Assets/Scripts/Combat/Enemy/EnemyEntity.cs` | **韧性系统**：`TakeDamage()` 中增加 poise 削减逻辑（伤害值=韧性伤害），poise ≤ 0 时标记 `IsStaggered` 并触发 `OnPoiseBroken` 事件 + 韧性击破时调用 `HitStopEffect.Trigger(0.08f)`；`Die()` 中调用 `HitStopEffect.Trigger(0.06f)`；新增 `ResetPoise()` 公开方法；新增 `OnPoiseBroken` 事件、`IsStaggered` 属性、`CurrentPoise` 属性；**群聚算法**：新增 `GetSeparationForce(float radius, float strength)` 方法，使用 `Physics2D.OverlapCircleAll` + Enemy Layer 检测邻近敌人，返回反距离加权推离向量；`OnReturnToPool()` 中新增 `OnPoiseBroken = null` 和 `IsStaggered = false` 清理 |
| `Assets/Scripts/Combat/Enemy/EnemyBrain.cs` | 新增 `StaggerState` 公开属性；`BuildStateMachine()` 中创建 `_staggerState` 实例并调用 `SubscribePoiseBroken()`；新增 `SubscribePoiseBroken()` 订阅 Entity 事件；新增 `ForceStagger()` 方法（检查 `SuperArmor` BehaviorTag 豁免）；新增 `OnDisable()` 取消订阅 |
| `Assets/Scripts/Combat/Enemy/States/ChaseState.cs` | 移动方向从纯追击改为追击方向 + `GetSeparationForce()` 混合（权重 0.3），防止多只近战敌人重叠堆积 |

### 系统设计

**韧性 (Poise) 系统：**
```
受击 → 伤害值同时削减 HP 和 Poise
  └→ Poise ≤ 0 → IsStaggered=true → OnPoiseBroken 事件
       └→ EnemyBrain.ForceStagger()
            ├→ 有 SuperArmor 标签 → 直接 ResetPoise()，不进入硬直
            └→ 无 SuperArmor → TransitionTo(StaggerState)
                 └→ StaggerDuration 秒后 → ResetPoise() → Chase/Idle
```

**顿帧 (HitStop) 时机与强度：**
| 触发事件 | 冻结时长 | 体感 |
|---------|---------|------|
| 韧性击破 | 0.08s | 明显顿挫，"打碎盔甲"的权重感 |
| 击杀 | 0.06s | 中等顿挫，"致命一击"的终结感 |

**群聚算法 (Boids Separation)：**
- 检测半径 1.5 单位内同 Layer 邻居
- 反距离加权：越近推力越大
- 混合权重 0.3：追击意图为主，分散为辅
- 结果：多只莽夫追击时呈扇形散开而非直线叠加

**技术：** `[RuntimeInitializeOnLoadMethod]` 自动单例创建, `Time.unscaledDeltaTime` 顿帧计时, C# event 事件驱动韧性击破, Physics2D.OverlapCircleAll Boids 邻居检测, BehaviorTags 字符串查询 SuperArmor 豁免。

---

## Bug Fix：敌人重叠堆叠修复 — 2026-02-11 20:15

### 问题
多只 Rusher 进入攻击状态（Engage/Telegraph）后停止移动，堆叠在同一坐标。原因：Boids 分离力仅在 `ChaseState` 中应用，一旦离开追击状态就完全失效；且距离 ≈ 0 时 `GetSeparationForce` 直接跳过（`if (dist < 0.001f) continue`），完美重叠时推力为零。

### 修改文件

| 文件路径 | 变更说明 |
|---------|---------|
| `Assets/Scripts/Combat/Enemy/EnemyEntity.cs` | 新增 `FixedUpdate()` + `ResolveOverlap()` 方法：使用 `Physics2D.OverlapCircleAll` 检测 `MIN_ENEMY_DISTANCE`(0.9) 范围内同 Layer 邻居，直接通过 `_rigidbody.position += push` 消解重叠（每人各承担一半重叠量）；完全重叠时生成随机方向避免死锁。修复 `GetSeparationForce` 中距离 ≈ 0 的处理：改为随机方向推开而非跳过。 |
| `Assets/Scripts/Combat/Enemy/States/ChaseState.cs` | `SEPARATION_WEIGHT` 从 0.3 提升至 0.6，在追击阶段就更积极地分散。 |

### 设计要点
- **FixedUpdate 位置消解** vs 力/速度方案：因为 `MoveTo()` 每帧直接设置 `linearVelocity`，AddForce 会被覆盖；直接修改 `_rigidbody.position` 是唯一在所有状态下可靠生效的方式。
- **双保险架构**：ChaseState 中的 Boids 方向混合（预防性扇形散开） + FixedUpdate 中的硬性重叠消解（兜底，任何状态生效）。

---

## Phase 2 完整实现：AttackDataSO + 导演系统 + 炮台原型 — 2026-02-11 21:00

### 概览

Phase 2 包含三大模块，按依赖顺序实现：
- **Phase 2A**：AttackDataSO 数据驱动攻击系统 + HitboxResolver 碰撞检测
- **Phase 2B**：EnemyDirector 导演系统（攻击令牌 + OrbitState 环绕等待）
- **Phase 2C**：Turret 炮台原型（激光 + 蓄力弹双变体）

### Phase 2A — AttackDataSO + Hitbox 系统

#### 新建文件

| 文件路径 | 说明 |
|---------|------|
| `Assets/Scripts/Combat/Enemy/AttackDataSO.cs` | ScriptableObject 定义单个攻击模式：`AttackType` 枚举 (Melee/Projectile/Laser) + `HitboxShape` 枚举 (Circle/Box/Cone) + 信号窗口三阶段时长 + 伤害/击退 + 碰撞形状参数 + 远程弹丸/激光参数 + 权重选择 + 视觉配置 |
| `Assets/Scripts/Combat/Enemy/HitboxResolver.cs` | 静态工具类，NonAlloc 共享缓冲区。`Resolve()` 根据 `HitboxShape` 执行不同 Physics2D 查询：Circle (`OverlapCircleNonAlloc`)、Box (`OverlapBoxNonAlloc`)、Cone (Circle + 角度过滤)。附带 Editor Gizmo 绘制 |

#### 修改文件

| 文件路径 | 变更说明 |
|---------|---------|
| `Assets/Scripts/Combat/Enemy/EnemyStatsSO.cs` | 新增 `AttackDataSO[] Attacks` 数组 + `HasAttackData` 属性 + `SelectRandomAttack()` 加权随机选择方法 |
| `Assets/Scripts/Combat/Enemy/States/EngageState.cs` | 新增 `SelectedAttack` 属性，`OnEnter()` 中执行 `SelectRandomAttack()`，子状态通过 `_engage.SelectedAttack` 读取；`OnExit()` 归还导演令牌 |
| `Assets/Scripts/Combat/Enemy/States/TelegraphSubState.cs` | 时长和颜色从 `SelectedAttack` 读取（null 时回退 legacy 字段） |
| `Assets/Scripts/Combat/Enemy/States/AttackSubState.cs` | 使用 `HitboxResolver.Resolve()` 替换原 `OverlapCircleAll`；伤害/击退从 `SelectedAttack` 读取；增加 legacy NonAlloc 缓冲区 |
| `Assets/Scripts/Combat/Enemy/States/RecoverySubState.cs` | 后摇时长从 `SelectedAttack` 读取 |
| `Assets/Scripts/Combat/Enemy/States/ShootState.cs` | 新增 `SelectProjectileAttack()` 筛选 Projectile 类型 AttackDataSO；所有阶段时长/弹丸参数优先从 AttackDataSO 读取；`OnExit()` 归还导演令牌 |

**设计要点**：全部修改保持 **100% 向后兼容** —— 当 `Attacks[]` 为空时，所有状态自动回退到 `EnemyStatsSO` 的遗留平面字段。

### Phase 2B — 导演系统 (EnemyDirector + OrbitState)

#### 新建文件

| 文件路径 | 说明 |
|---------|------|
| `Assets/Scripts/Combat/Enemy/EnemyDirector.cs` | 全局单例协调器。`_maxAttackTokens` (默认 2) 限制同时攻击敌人数；`HashSet<EnemyBrain>` 追踪令牌持有者；`RequestToken()` / `ReturnToken()` / `HasToken()` API；`LateUpdate()` 自动清理死亡/禁用的 Brain；无 Director 时全体自由攻击（向后兼容）；Editor 顶角显示 token 使用量 |
| `Assets/Scripts/Combat/Enemy/States/OrbitState.cs` | 等待令牌时的行为：以 `AttackRange * OrbitRadiusMultiplier` 为半径绕玩家环行；随机 CW/CCW 方向；混合 Boids 分离力（权重 0.5）；每 0.4s 重新请求令牌（非逐帧）；获得令牌后转入 Engage/Shoot |

#### 修改文件

| 文件路径 | 变更说明 |
|---------|---------|
| `Assets/Scripts/Combat/Enemy/EnemyBrain.cs` | 新增 `OrbitState` 属性；`BuildStateMachine()` 实例化 `_orbitState`；新增 `ReturnDirectorToken()` 公共方法；`ForceStagger()` 和 `OnDisable()` 中自动归还令牌；`ResetBrain()` 归还令牌；Debug GUI 显示 `[T]` 标记 |
| `Assets/Scripts/Combat/Enemy/ShooterBrain.cs` | Debug GUI 增加令牌状态显示 |
| `Assets/Scripts/Combat/Enemy/States/ChaseState.cs` | 进入攻击范围时先 `RequestToken()` —— 通过则转 Engage/Shoot，拒绝则转 OrbitState；无 Director 时直接攻击（兼容） |

**设计要点**：
- **电影感轮流单挑**：最多 2 敌人同时攻击，其余环绕助威
- **零分配**：`HashSet` O(1) 查找，`OrbitState` 令牌检查 0.4s 节流
- **健壮性**：死亡/禁用/硬直/池回收均自动归还令牌，`LateUpdate` 兜底清理

### Phase 2C — 炮台原型 (Turret)

#### 新建文件

| 文件路径 | 说明 |
|---------|------|
| `Assets/Scripts/Combat/Enemy/EnemyLaserBeam.cs` | 敌方激光束。`Fire()` 执行 Raycast + LineRenderer 渲染 + IDamageable 伤害；`ShowAimLine()` / `HideAimLine()` 显示瞄准线（Lock 阶段视觉预警）；支持持续光束模式（LaserDuration）；淡出效果；`IPoolable` 对象池支持 |
| `Assets/Scripts/Combat/Enemy/TurretBrain.cs` | 继承 EnemyBrain，**不调用** `base.BuildStateMachine()`（无 Chase/Engage/Return/Orbit）；4 个专用状态：Scan → Lock → Attack → Cooldown；`SelectAttackForCycle()` 从 AttackDataSO[] 选择攻击；支持韧性击破/硬直 |
| `Assets/Scripts/Combat/Enemy/States/TurretScanState.cs` | 扫描状态：缓慢旋转扫视（ScanRotationSpeed°/s），检测到目标 → Lock |
| `Assets/Scripts/Combat/Enemy/States/TurretLockState.cs` | 锁定状态：快速追踪目标，显示瞄准线（EnemyLaserBeam.ShowAimLine），LockOnDuration 后 → Attack；丢失目标 → Scan |
| `Assets/Scripts/Combat/Enemy/States/TurretAttackState.cs` | 攻击状态：根据 AttackDataSO.Type 分发——Laser 型调用 `EnemyLaserBeam.Fire()`，Projectile 型从对象池发射弹丸；完成 → Cooldown |
| `Assets/Scripts/Combat/Enemy/States/TurretCooldownState.cs` | 冷却状态：AttackCooldown 倒计时，完成后根据目标可见性 → Lock 或 Scan |

#### Editor 工具扩展

| 文件路径 | 变更说明 |
|---------|---------|
| `Assets/Scripts/Combat/Editor/EnemyAssetCreator.cs` | 新增 `CreateAttackDataAssets()` 菜单项（创建 RusherMelee + ShooterBurst AttackDataSO）；新增 `CreateTurretLaserAssets()` + `CreateTurretCannonAssets()` 菜单项（创建 EnemyStatsSO + AttackDataSO + 完整 Prefab，含子物体 LineRenderer + EnemyLaserBeam）；新增 `CreateTurretGameObject()` 共享 Prefab 构建方法 |

### 完整文件清单

**新建文件 (11)**:
1. `Assets/Scripts/Combat/Enemy/AttackDataSO.cs`
2. `Assets/Scripts/Combat/Enemy/HitboxResolver.cs`
3. `Assets/Scripts/Combat/Enemy/EnemyDirector.cs`
4. `Assets/Scripts/Combat/Enemy/EnemyLaserBeam.cs`
5. `Assets/Scripts/Combat/Enemy/TurretBrain.cs`
6. `Assets/Scripts/Combat/Enemy/States/OrbitState.cs`
7. `Assets/Scripts/Combat/Enemy/States/TurretScanState.cs`
8. `Assets/Scripts/Combat/Enemy/States/TurretLockState.cs`
9. `Assets/Scripts/Combat/Enemy/States/TurretAttackState.cs`
10. `Assets/Scripts/Combat/Enemy/States/TurretCooldownState.cs`

**修改文件 (9)**:
1. `Assets/Scripts/Combat/Enemy/EnemyStatsSO.cs`
2. `Assets/Scripts/Combat/Enemy/EnemyBrain.cs`
3. `Assets/Scripts/Combat/Enemy/ShooterBrain.cs`
4. `Assets/Scripts/Combat/Enemy/States/EngageState.cs`
5. `Assets/Scripts/Combat/Enemy/States/TelegraphSubState.cs`
6. `Assets/Scripts/Combat/Enemy/States/AttackSubState.cs`
7. `Assets/Scripts/Combat/Enemy/States/RecoverySubState.cs`
8. `Assets/Scripts/Combat/Enemy/States/ShootState.cs`
9. `Assets/Scripts/Combat/Enemy/States/ChaseState.cs`
10. `Assets/Scripts/Combat/Editor/EnemyAssetCreator.cs`

**技术**：数据驱动 (AttackDataSO)、策略模式 (AttackType 分发)、NonAlloc 物理查询、HashSet O(1) 令牌管理、环形运动 (OrbitState)、Raycast + LineRenderer 激光、IPoolable 对象池。

---

## Phase 3 完整实现：刺客 + 恐惧 + 阵营 + 闪避格挡 + 精英词缀 + Boss — 2026-02-11 23:00

### 概述
Phase 3 实现了 6 个子系统，构建了完整的高级敌人 AI 层：
- **3A** 刺客原型 (Stalker)
- **3B** 恐惧系统 (Fear)
- **3C** 阵营系统 (Faction)
- **3D** 闪避/格挡 AI (Dodge/Block)
- **3E** 精英词缀系统 (Elite Affix)
- **3F** 多阶段 Boss 控制器

### 新建文件

#### 3A: 刺客原型
1. `Assets/Scripts/Combat/Enemy/StalkerBrain.cs` — 刺客大脑，覆盖 BuildStateMachine()，4 个专属状态 + 透明度管理
2. `Assets/Scripts/Combat/Enemy/States/StealthState.cs` — 隐身状态：低 alpha，无目标时缓慢飘回出生点
3. `Assets/Scripts/Combat/Enemy/States/FlankState.cs` — 迂回状态：利用 Dot Product 判定玩家背后弧，移向背后位置
4. `Assets/Scripts/Combat/Enemy/States/StalkerStrikeState.cs` — 突袭状态：快速显形 → 单次近战 → 短暂僵直 → 脱离
5. `Assets/Scripts/Combat/Enemy/States/DisengageState.cs` — 脱离状态：加速远离 + 渐隐回隐身

#### 3B: 恐惧系统
6. `Assets/Scripts/Combat/Enemy/EnemyFear.cs` — 恐惧组件：累积恐惧值，订阅 OnAnyEnemyDeath，超阈值触发逃跑
7. `Assets/Scripts/Combat/Enemy/States/FleeState.cs` — 逃跑状态：远离玩家，加速移动，计时/距离退出

#### 3D: 闪避/格挡
8. `Assets/Scripts/Combat/Enemy/ThreatSensor.cs` — 威胁感知：5Hz NonAlloc 扫描来袭投射物，Dot Product 判断朝向
9. `Assets/Scripts/Combat/Enemy/States/DodgeState.cs` — 闪避状态：垂直于威胁方向侧闪
10. `Assets/Scripts/Combat/Enemy/States/BlockState.cs` — 格挡状态：面向威胁举盾，减伤

#### 3E: 精英词缀
11. `Assets/Scripts/Combat/Enemy/EnemyAffixSO.cs` — 词缀 SO：统计乘数 + 行为标签 + 特殊效果枚举
12. `Assets/Scripts/Combat/Enemy/EnemyAffixController.cs` — 词缀控制器：运行时应用词缀，处理特殊效果 (爆炸/反伤/狂暴等)

#### 3F: 多阶段 Boss
13. `Assets/Scripts/Combat/Enemy/BossPhaseDataSO.cs` — Boss 阶段 SO：HP 阈值、攻击模式、统计修正、过渡效果
14. `Assets/Scripts/Combat/Enemy/BossController.cs` — Boss 控制器：监听伤害事件，按 HP 阈值触发阶段转换
15. `Assets/Scripts/Combat/Enemy/States/BossTransitionState.cs` — Boss 过渡状态：无敌 + 视觉脉冲 + HitStop

### 修改文件

1. `Assets/Scripts/Combat/Enemy/EnemyEntity.cs`
   - 新增 `OnAnyEnemyDeath` 全局静态事件，Die() 中广播
   - 新增 `MoveAtSpeed(direction, speed)` 方法（变速移动）
   - 新增 `IsBlocking`、`BlockDamageReduction` 属性，TakeDamage 中应用格挡减伤
   - 新增 `IsInvulnerable` 属性，TakeDamage 中无敌检查
   - 新增 `_runtimeMaxHP`、`_runtimeDamageMultiplier`、`_runtimeSpeedMultiplier` 运行时统计
   - 新增 `ApplyAffixMultipliers()` 方法
   - `MoveTo()` 应用 `_runtimeSpeedMultiplier`
   - `OnReturnToPool()` 重置所有新状态

2. `Assets/Scripts/Combat/Enemy/EnemyStatsSO.cs`
   - 新增 `FactionID` 字段
   - 新增恐惧系统字段：`FearThreshold`、`FearFromAllyDeath`、`FearFromPoiseBroken`、`FearDecayRate`、`FleeDuration`
   - 新增闪避/格挡字段：`DodgeSpeed`、`DodgeDuration`、`BlockDamageReduction`、`BlockDuration`、`ThreatDetectionRadius`

3. `Assets/Scripts/Combat/Enemy/EnemyBrain.cs`
   - 新增 `FleeState`、`DodgeState`、`BlockState` 状态实例和属性
   - 新增 `ForceFleeCheck()` 方法（恐惧系统调用）
   - 新增 `ForceTransition(BossPhaseDataSO)` 方法（Boss 控制器调用）
   - 新增 `CheckThreatResponse()` 在 Update 中检查威胁感知并触发闪避/格挡

4. `Assets/Scripts/Combat/Enemy/EnemyPerception.cs`
   - 完全重写：新增 `TargetType` 枚举、`CurrentTargetType`、`CurrentTargetEntity` 属性
   - `LastKnownPlayerPosition` → `LastKnownTargetPosition`（旧名保留为别名）
   - 新增 `PerformFactionScan()` 方法：NonAlloc 扫描敌方阵营实体
   - 玩家始终优先（PerformVisionCheck > PerformFactionScan）
   - 记忆衰减更新：支持阵营目标存活刷新

5. `Assets/Scripts/Combat/Enemy/States/ChaseState.cs` — `LastKnownPlayerPosition` → `LastKnownTargetPosition`
6. `Assets/Scripts/Combat/Enemy/States/OrbitState.cs` — 同上
7. `Assets/Scripts/Combat/Enemy/States/ShootState.cs` — 同上
8. `Assets/Scripts/Combat/Enemy/States/TurretLockState.cs` — 同上
9. `Assets/Scripts/Combat/Enemy/States/RetreatState.cs` — 同上
10. `Assets/Scripts/Combat/Enemy/States/FlankState.cs` — 同上
11. `Assets/Scripts/Combat/Enemy/States/FleeState.cs` — 同上
12. `Assets/Scripts/Combat/Enemy/States/DisengageState.cs` — 同上

13. `Assets/Scripts/Combat/Enemy/EnemySpawner.cs`
    - 新增精英词缀生成：`_possibleAffixes`、`_eliteChance`、`_maxAffixCount` 字段
    - 新增 `TryApplyAffixes()` 方法

14. `Assets/Scripts/Combat/Editor/EnemyAssetCreator.cs`
    - 新增 `CreateStalkerAssets()` 菜单（SO + AttackData + Prefab）
    - 新增 `CreateAffixAssets()` 菜单（5 个精英词缀）

### 目的
完成 Phase 3 全部高级敌人 AI 系统，使战斗系统具备：
- 多样化敌人行为（隐身刺客、恐惧逃跑、阵营内斗）
- 动态战斗反应（闪避来袭投射物、举盾格挡）
- 可扩展的精英变体系统（词缀运行时堆叠）
- Boss 多阶段战斗机制（HP 阈值触发、攻击模式切换、无敌过渡）

**技术**：HFSM 子类化 (StalkerBrain)、事件驱动恐惧传播 (静态事件 + 距离判定)、NonAlloc 阵营/威胁扫描、运行时统计覆写 (AffixController)、Dot Product 几何判定 (后方弧/威胁朝向)、策略模式词缀效果、数据驱动 Boss 阶段 (BossPhaseDataSO)。

---

## 架构基建大修 — Architecture Infrastructure Overhaul

> 以下 Phase 1–6C 为一次性架构改进，解决进入关卡构建阶段前识别出的 9 项架构短板。

---

### Phase 1: UniTask + PrimeTween 集成 — 2025-06-XX

#### 新建/修改文件

1. `Packages/manifest.json`
   - 新增 `com.cysharp.unitask` (GitHub git URL)
   - 新增 `com.kyrylokuzyk.primetween` (初始为 git URL，后改为 NPM scoped registry)

2. `Assets/Scripts/UI/ProjectArk.UI.asmdef` — 新增 `UniTask`、`PrimeTween.Runtime` 引用

3. `Assets/Scripts/Combat/ProjectArk.Combat.asmdef` — 新增 `UniTask` 引用

4. `Assets/Scripts/Ship/ProjectArk.Ship.asmdef` — 新增 `UniTask` 引用

5. `Assets/Scripts/UI/WeavingStateTransition.cs`
   - 将协程过渡重构为 `async UniTaskVoid` + `PrimeTween.Tween.Custom`
   - 新增 `CancellationTokenSource` 管理，`CancelTransition()` 方法

6. `Assets/Scripts/Combat/Enemy/EnemySpawner.cs`
   - `RespawnAfterDelay` 从协程迁移为 `async UniTaskVoid` + `UniTask.Delay`

7. `Assets/Scripts/Combat/Enemy/EnemyEntity.cs`
   - `HitFlashCoroutine` 迁移为 `HitFlashAsync`，使用 `UniTask.Delay` + `CancellationTokenSource`

8. `Assets/Scripts/Ship/Combat/ShipHealth.cs`
   - `HitFlashCoroutine` 迁移为 `HitFlashAsync`，同上

#### 目的
用 UniTask 替代 Coroutine 实现零 GC 异步编程；用 PrimeTween 替代手写 Lerp 实现高性能补间动画。

**技术**：UniTask (`async UniTaskVoid`, `UniTask.Delay`, `UniTask.Yield`)、PrimeTween (`Tween.Custom`)、`CancellationTokenSource` 生命周期管理。

---

### Phase 2: ServiceLocator 轻量依赖注入 — 2025-06-XX

#### 新建/修改文件

1. `Assets/Scripts/Core/ServiceLocator.cs` *(新建)*
   - 静态泛型 Service Locator：`Register<T>`, `Get<T>`, `Unregister<T>`, `Clear()`

2. `Assets/Scripts/Core/Pool/PoolManager.cs` — Awake 注册 / OnDestroy 注销 ServiceLocator

3. `Assets/Scripts/Combat/Enemy/EnemyDirector.cs` — 同上

4. `Assets/Scripts/Heat/HeatSystem.cs` — 同上

5. `Assets/Scripts/Combat/StarChart/StarChartController.cs` — 同上；`InitializeAllPools` 从 ServiceLocator 获取 PoolManager

6. `Assets/Scripts/UI/UIManager.cs` — 替换所有 `FindAnyObjectByType` 为 `ServiceLocator.Get<T>()`

#### 目的
消除 `FindAnyObjectByType` 的 O(n) 运行时查找和直接单例引用，建立统一的服务解析入口。

**技术**：静态泛型字典 (`Dictionary<Type, object>`)、Register/Unregister 生命周期与 MonoBehaviour.Awake/OnDestroy 绑定。

---

### Phase 3: 统一伤害管线 — 2025-06-XX

#### 新建/修改文件

1. `Assets/Scripts/Core/DamageType.cs` *(新建)* — `enum DamageType { Physical, Fire, Ice, Lightning, Void }`

2. `Assets/Scripts/Core/DamagePayload.cs` *(新建)* — 不可变结构体封装伤害事件全部数据

3. `Assets/Scripts/Core/IResistant.cs` *(新建)* — 元素抗性接口

4. `Assets/Scripts/Core/IBlockable.cs` *(新建)* — 格挡接口

5. `Assets/Scripts/Core/DamageCalculator.cs` *(新建)* — 集中式伤害计算：抗性 → 格挡 → 最终伤害

6. `Assets/Scripts/Core/IDamageable.cs` — 新增 `TakeDamage(DamagePayload)` 重载，旧签名标记 `[Obsolete]`

7. `Assets/Scripts/Combat/StarChart/StarCoreSO.cs` — 新增 `_damageType` 字段

8. `Assets/Scripts/Combat/StarChart/FiringSnapshot.cs` — `CoreSnapshot` 新增 `DamageType`

9. `Assets/Scripts/Combat/StarChart/ProjectileParams.cs` — 新增 `DamageType` 字段

10. `Assets/Scripts/Combat/StarChart/SnapshotBuilder.cs` — `BuildCoreSnapshot` 填充 `DamageType`

11. `Assets/Scripts/Combat/Projectile/Projectile.cs` — 存储 `DamageType`，碰撞时构造 `DamagePayload`

12. `Assets/Scripts/Combat/Projectile/LaserBeam.cs` — 同上

13. `Assets/Scripts/Combat/Projectile/EchoWave.cs` — 同上

14. `Assets/Scripts/Combat/Enemy/States/AttackSubState.cs` — 使用 `DamagePayload(DamageType.Physical)`

15. `Assets/Scripts/Combat/Enemy/States/StalkerStrikeState.cs` — 同上

16. `Assets/Scripts/Combat/Enemy/EnemyProjectile.cs` — 同上

17. `Assets/Scripts/Combat/Enemy/EnemyLaserBeam.cs` — 同上

18. `Assets/Scripts/Combat/Enemy/EnemyAffixController.cs` — 爆炸 / 反弹词缀使用 `DamagePayload`

19. `Assets/Scripts/Combat/Enemy/EnemyEntity.cs` — 实现 `IResistant` + `IBlockable`，`TakeDamage` 调用 `DamageCalculator.Calculate`

20. `Assets/Scripts/Ship/Combat/ShipHealth.cs` — 新增 `TakeDamage(DamagePayload)` 重载

#### 目的
建立从发射到落点的完整伤害管线，支持元素抗性、格挡减伤、伤害来源追踪。

**技术**：`readonly struct DamagePayload`、策略式接口 (`IResistant`, `IBlockable`)、集中计算器模式 (`DamageCalculator`)、渐进式迁移 (`[Obsolete]` 旧 API)。

---

### Phase 4A: 存档系统数据模型 — 2025-06-XX

#### 新建/修改文件

1. `Assets/Scripts/Core/Save/SaveData.cs` *(新建)*
   - `PlayerSaveData`, `StarChartSaveData`, `TrackSaveData`, `InventorySaveData`, `ProgressSaveData`, `SaveFlag`, `PlayerStateSaveData`

2. `Assets/Scripts/Core/Save/SaveManager.cs` *(新建)*
   - 静态工具类：`Save`, `Load`, `Delete`, `HasSave`，支持自动备份

3. `Assets/Scripts/Combat/StarChart/StarChartController.cs`
   - 新增 `ExportToSaveData()` / `ImportFromSaveData()` 方法

4. `Assets/Scripts/UI/StarChartInventorySO.cs`
   - 新增 `FindCore`, `FindPrism`, `FindLightSail`, `FindSatellite` 查找方法

#### 目的
建立序列化安全的存档数据模型和 I/O 管理器，为后续关卡进度存储奠定基础。

**技术**：`JsonUtility` 序列化、`Application.persistentDataPath`、自动 `.bak` 备份、按槽位存档。

---

### Phase 4B: 音频架构 — 2025-06-XX

#### 新建/修改文件

1. `Assets/Scripts/Core/Audio/` *(新建目录)*

2. `Assets/Scripts/Core/Audio/ProjectArk.Core.Audio.asmdef` *(新建)* — 音频程序集定义

3. `Assets/Scripts/Core/Audio/AudioManager.cs` *(新建)*
   - SFX 池化播放、音乐淡入淡出、Mixer 音量/低通滤波控制
   - ServiceLocator 注册/注销

4. `Assets/Scripts/UI/ProjectArk.UI.asmdef` — 新增 `ProjectArk.Core.Audio` 引用

5. `Assets/Scripts/Combat/ProjectArk.Combat.asmdef` — 新增 `ProjectArk.Core.Audio` 引用

#### 目的
建立集中式音频管理，支持 SFX 池化（避免运行时 AudioSource 创建/销毁）和 Mixer 控制。

**技术**：AudioSource 对象池、`AudioMixer.SetFloat` 对数音量映射、`Mathf.Log10` dB 转换。

---

### Phase 5: Combat 程序集拆分 + 事件总线 — 2025-06-XX

#### 新建/修改文件

1. `Assets/Scripts/Combat/Enemy/ProjectArk.Enemy.asmdef` *(新建)*
   - 将 Enemy 子目录独立为 `ProjectArk.Enemy` 程序集

2. `Assets/Scripts/Core/CombatEvents.cs` *(新建)*
   - 静态事件总线：`OnWeaponFired` 事件，解耦 `EnemyPerception` 与 `StarChartController`

3. `Assets/Scripts/Combat/Enemy/EnemyPerception.cs`
   - 订阅 `CombatEvents.OnWeaponFired` 替代 `StarChartController.OnWeaponFired`

4. `Assets/Scripts/Combat/StarChart/StarChartController.cs`
   - 使用 `CombatEvents.RaiseWeaponFired()` 替代直接事件

5. `Assets/Scripts/Combat/Editor/ProjectArk.Combat.Editor.asmdef`
   - 新增 `ProjectArk.Enemy` 引用

#### 目的
打破 Combat ↔ Enemy 循环依赖，通过 Core 层事件总线实现跨程序集通信。

**技术**：Assembly Definition 拆分、静态事件总线模式 (`CombatEvents`)。

---

### Phase 6A: 数据驱动状态转换 — 2025-06-XX

#### 新建/修改文件

1. `Assets/Scripts/Combat/StateTransitionRule.cs` *(新建)*
   - `TransitionCondition` 枚举、`EnemyStateType` 枚举、`StateTransitionRule` 可序列化类

2. `Assets/Scripts/Combat/Enemy/TransitionRuleEvaluator.cs` *(新建)*
   - 静态工具类：评估转换规则、将 `EnemyStateType` 解析为 `IState` 实例

3. `Assets/Scripts/Combat/Enemy/EnemyStatsSO.cs`
   - 新增 `_transitionOverrides` 数组

4. `Assets/Scripts/Combat/Enemy/States/ChaseState.cs`
   - 优先检查数据驱动规则，无匹配时 fallback 到硬编码逻辑

#### 目的
允许策划在 SO Inspector 中配置 AI 状态转换规则，无需修改代码即可调整行为。

**技术**：数据驱动规则评估、策略模式状态解析、`StateTransitionRule` SO 可序列化数组。

---

### Phase 6B: 单元测试基础设施 — 2025-06-XX

#### 新建/修改文件

1. `Assets/Scripts/Core/Tests/ProjectArk.Core.Tests.asmdef` *(新建)*

2. `Assets/Scripts/Combat/Tests/ProjectArk.Combat.Tests.asmdef` *(新建)*

3. `Assets/Scripts/Core/Tests/DamageCalculatorTests.cs` *(新建)*
   - 抗性、格挡、边界情况测试

4. `Assets/Scripts/Combat/Tests/StateMachineTests.cs` *(新建)*
   - OnEnter/OnExit 顺序、Tick、状态切换验证

5. `Assets/Scripts/Combat/Tests/SnapshotBuilderTests.cs` *(新建)*
   - 棱镜修正聚合 (Add/Multiply)、弹丸计数上限

6. `Assets/Scripts/Combat/Tests/SlotLayerTests.cs` *(新建)*
   - 装备/卸下逻辑、槽位尺寸、占用验证

7. `Assets/Scripts/Core/Tests/HeatSystemTests.cs` *(新建)*
   - ServiceLocator 功能测试

#### 目的
建立单元测试基础设施，为核心系统提供回归保护。

**技术**：NUnit、Unity Test Framework、运行时创建 ScriptableObject 实例、反射设置私有字段。

---

## Bug 修复：PrimeTween 包解析 + CS0246 + CS0117 + CS4014 + CanvasGroup — 2025-06-XX

### 修改文件

1. `Packages/manifest.json`
   - PrimeTween 从失效的 git URL (`nicktmv/PrimeTween.git`) 改为 NPM scoped registry (`registry.npmjs.org`, 版本 `1.3.0`)
   - 新增 `scopedRegistries` 配置块

2. `Assets/Scripts/Combat/StarChart/IStarChartItemResolver.cs` *(新建)*
   - 接口定义 `FindCore`, `FindPrism`, `FindLightSail`, `FindSatellite`
   - 解决 Combat 程序集无法引用 UI 程序集中 `StarChartInventorySO` 的循环依赖

3. `Assets/Scripts/Combat/StarChart/StarChartController.cs`
   - `ImportFromSaveData` 和 `ImportTrack` 参数从 `StarChartInventorySO` 改为 `IStarChartItemResolver`

4. `Assets/Scripts/UI/StarChartInventorySO.cs`
   - 实现 `IStarChartItemResolver` 接口

5. `Assets/Scripts/Combat/Tests/SnapshotBuilderTests.cs`
   - `PrismFamily.Stat`（不存在）→ `PrismFamily.Rheology`（正确枚举值）

6. `Assets/Scripts/UI/WeavingStateTransition.cs`
   - PrimeTween `Tween.Custom` 返回值添加 `_ =` 显式丢弃，消除 CS4014 警告

7. `Assets/Scripts/UI/Editor/UICanvasBuilder.cs`
   - `CreateInventoryItemViewPrefab()` 在 `AddComponent<InventoryItemView>()` 前显式添加 `CanvasGroup`

### 目的
修复架构大修后的编译错误和运行时警告：包解析失败、跨程序集循环依赖、枚举值不存在、异步返回值未处理、Prefab 缺少必需组件。

**技术**：NPM scoped registry、依赖反转原则 (DIP)、显式 discard (`_ =`)、`[RequireComponent]` 预防。

---

## CLAUDE.md 架构大修同步更新 — 2026-02-12 17:33

### 修改文件

1. `CLAUDE.md`

### 内容

将架构基建大修引入的所有新系统、新模式、新依赖同步更新到 CLAUDE.md，确保后续 AI 对话使用正确的技术栈和架构模式。同时强化 ImplementationLog 写入规则。

具体变更：
- **技术栈**：新增 UniTask、PrimeTween
- **核心模块**：新增基建 (Infrastructure) 条目（ServiceLocator、DamagePayload、SaveManager、AudioManager、CombatEvents、TransitionRuleEvaluator）
- **架构原则 §2 解耦与模块化**：新增跨程序集事件总线规则、依赖反转 (IStarChartItemResolver) 示例
- **架构原则 §7 服务定位**：新增 ServiceLocator 使用规范，禁止 FindAnyObjectByType
- **代码规范 > 异步纪律**：替换旧"协程纪律"为 UniTask/PrimeTween/CTS 规范
- **代码规范 > 命名**：新增 `ProjectArk.Core.Audio`、`ProjectArk.Core.Save`、`ProjectArk.Enemy` 命名空间
- **项目结构**：新增 `Core/Audio/`、`Core/Save/`、`Core/Tests/`、`Combat/Tests/` 目录
- **当前里程碑**：新增"架构基建大修"已完成行
- **实现日志**：升级为"严格执行"规则，新增执行时机说明（单阶段/多阶段/Bug 修复），在 Feature 开发工作流第 7 步添加强调标注

### 目的
CLAUDE.md 是 AI 对话的"ground truth"。架构大修后未同步更新会导致后续对话使用过时模式（如 Coroutine 而非 UniTask、FindAnyObjectByType 而非 ServiceLocator）。同时强化日志写入规则防止再次遗漏。

**技术**：文档维护。

---

## Level Module Phase 1 — 基础房间系统 (L1-L5) — 2026-02-12 23:30

### 新建文件

| 文件 | 说明 |
|------|------|
| `Assets/Scripts/Core/LevelEvents.cs` | Core 层静态关卡事件总线（与 CombatEvents 平行）。事件：OnRoomEntered/OnRoomExited/OnRoomCleared/OnBossDefeated/OnCheckpointActivated/OnWorldStageChanged，每个事件配对 RaiseXxx() 方法 |
| `Assets/Scripts/Level/ProjectArk.Level.asmdef` | 新程序集定义。引用：Core, Combat, Ship, Enemy, Heat, Core.Audio, UniTask, PrimeTween, Unity.Cinemachine |
| `Assets/Scripts/Level/Data/RoomType.cs` | 房间类型枚举：Normal / Arena / Boss / Safe |
| `Assets/Scripts/Level/Data/RoomSO.cs` | 轻量房间元数据 SO（只存非空间信息：RoomID / DisplayName / FloorLevel / MapIcon / RoomType / EncounterSO 引用）。空间数据由场景 Room MonoBehaviour 管理 |
| `Assets/Scripts/Level/Data/EncounterSO.cs` | 战斗遭遇波次数据 SO：EnemyWave[]（每波含 EnemySpawnEntry[] + DelayBeforeWave），EnemySpawnEntry 含 EnemyPrefab + Count |
| `Assets/Scripts/Level/Room/RoomState.cs` | 房间运行时状态枚举：Undiscovered / Entered / Cleared / Locked |
| `Assets/Scripts/Level/Room/Room.cs` | 房间运行时 MonoBehaviour。引用 RoomSO 元数据；持有 BoxCollider2D Trigger（玩家检测）、Collider2D confinerBounds（摄像机约束）、Transform[] spawnPoints、Door[] doors。OnTriggerEnter2D/Exit2D 检测玩家进出并触发事件。提供 ActivateEnemies/DeactivateEnemies、LockAllDoors/UnlockCombatDoors 辅助方法 |
| `Assets/Scripts/Level/Room/DoorState.cs` | 门状态枚举：Open / Locked_Combat / Locked_Key / Locked_Ability / Locked_Schedule |
| `Assets/Scripts/Level/Room/Door.cs` | 门组件。持有 TargetRoom + TargetSpawnPoint 双向引用，DoorState 状态机。OnTriggerEnter2D 检测玩家并在 Open 状态下通过 DoorTransitionController 触发过渡 |
| `Assets/Scripts/Level/Room/DoorTransitionController.cs` | 门过渡控制器（ServiceLocator 注册）。使用 async UniTaskVoid + PrimeTween 实现淡黑→传送→淡入过渡。支持普通门（0.3s）和层间过渡（0.5s）两种时长。过渡期间禁用玩家输入，绑定 CancellationTokenSource + destroyCancellationToken |
| `Assets/Scripts/Level/Camera/RoomCameraConfiner.cs` | 摄像机房间约束桥接。订阅 RoomManager.OnCurrentRoomChanged，更新 CinemachineConfiner2D.BoundingShape2D 并调用 InvalidateBoundingShapeCache() |
| `Assets/_Data/Level/Rooms/` | RoomSO 资产存放目录（空） |
| `Assets/_Data/Level/Encounters/` | EncounterSO 资产存放目录（空） |

### 修改文件

| 文件 | 变更 |
|------|------|
| `Assets/Scripts/Combat/Enemy/EnemyDirector.cs` | 新增 `ReturnAllTokens()` 公共方法，清空所有攻击令牌。供 RoomManager 在房间切换时调用，防止跨房间令牌泄漏 |
| `Assets/Scripts/Ship/Input/InputHandler.cs` | 新增 `OnInteractPerformed` 事件 + `_interactAction` 字段。在 Awake 中查找 Ship/Interact action，在 OnEnable/OnDisable 中订阅/取消订阅 performed 回调。为门交互和未来检查点/NPC 交互提供输入支持 |

### 目的

搭建关卡模块 Phase 1 基础结构（L1-L5），从零建立房间系统骨架：
- **L1**：LevelEvents 事件总线 + ProjectArk.Level 程序集 + RoomSO/EncounterSO 数据层
- **L2**：Room 运行时组件（Trigger 检测 + 状态管理 + 敌人激活/休眠）
- **L3**：RoomManager 管理器（ServiceLocator 注册、房间追踪、事件广播、Director 令牌清理、Arena/Boss 自动锁门）
- **L4**：Door 组件 + DoorTransitionController（UniTask + PrimeTween 异步淡黑过渡）+ InputHandler Interact 暴露
- **L5**：RoomCameraConfiner（Cinemachine 3.x CinemachineConfiner2D 集成，房间切换时自动更新摄像机约束边界）

### 技术

- 命名空间：`ProjectArk.Level`（新程序集）、`ProjectArk.Core`（LevelEvents）
- 模式：静态事件总线（LevelEvents）、ServiceLocator 注册（RoomManager/DoorTransitionController）、UniTask + PrimeTween 异步过渡、CancellationTokenSource 生命周期管理
- 数据驱动：RoomSO（轻量元数据）+ EncounterSO（波次配置），场景即空间数据
- Cinemachine 3.x：CinemachineConfiner2D.BoundingShape2D + InvalidateBoundingShapeCache()

### 用户手动步骤

完成代码后需要在 Unity 编辑器中执行：
1. Physics2D 碰撞矩阵：为 Room Trigger 配置新 Layer（仅与 Player 碰撞）
2. 场景搭建：创建 CinemachineCamera + CinemachineConfiner2D、RoomManager GameObject、DoorTransitionController + Canvas/FadeImage、3 个测试 Room（Tilemap + BoxCollider2D + PolygonCollider2D + Door）
3. 创建测试 RoomSO 资产并拖入 Room 组件

---

## UICanvasBuilder 幂等重构 + DoorTransitionController 集成 — 2026-02-12 20:30

### 修改文件

| 文件 | 变更 | 目的 |
|------|------|------|
| `Assets/Scripts/UI/ProjectArk.UI.asmdef` | 添加 `ProjectArk.Level` 引用 | 使 UI Editor 脚本能引用 Level 模块类型 (DoorTransitionController) |
| `Assets/Scripts/UI/Editor/UICanvasBuilder.cs` | 重构为幂等 + 新增 DoorTransitionController 段 | 见下方详细说明 |

### UICanvasBuilder 改动详情

**1. 幂等创建（防重复）**
- `FindOrCreateCanvas()`: 先通过 UIManager 查找已有 Canvas，再通过 sortingOrder=10 匹配，均未找到才创建新 Canvas
- 每个 Section (HeatBarHUD / HealthBarHUD / StarChartPanel / UIManager / DoorTransitionController) 均先用 `GetComponentInChildren<T>(true)` 检查是否已存在
- 已存在的 Section 打印 skip 日志并跳过，不会重复创建

**2. 代码结构重构**
- 将每个 Section 的创建逻辑提取为独立私有方法（`BuildHeatBarSection` / `BuildHealthBarSection` / `BuildStarChartSection` / `BuildUIManagerSection` / `BuildDoorTransitionSection`）
- 主 `BuildUICanvas()` 方法简化为约 50 行的编排器，可读性大幅提升

**3. 新增 DoorTransitionController 段 (Step 6)**
- 创建 "FadeOverlay" 作为 Canvas 最后一个子物体（`SetAsLastSibling` 确保渲染在最顶层）
- 添加全屏 Image（黑色 alpha=0，默认不阻挡 raycast）
- 添加 DoorTransitionController 组件并自动连线 `_fadeImage`
- 用户不再需要手动创建 FadeOverlay 和 DoorTransitionController

### 技术

- 利用 `GetComponentInChildren<T>(true)` 的 includeInactive 参数确保即使 StarChartPanel 被隐藏也能被找到
- `FindObjectsByType<Canvas>(FindObjectsInactive.Include, ...)` 搜索包含未激活的 Canvas
- Section Builder 方法返回创建的组件引用，供后续 Section 连线使用（如 UIManager 需要 HeatBarHUD 等引用）

---

## 关卡模块 Phase 2 — 进度系统全量实现 — 2026-02-13 09:04

### 概述

实现 Level Module Phase 2 的全部 5 个子系统：检查点 (L6)、物品拾取 (L8)、锁钥 (L7)、死亡重生 (L9)、世界进度 (L9.5)。同时修复 Phase 1 git 回退后遗失的 InputHandler ServiceLocator 注册，以及为 ShipHealth 添加 Heal() 方法。

### 前置修复

| 文件 | 变更 | 目的 |
|------|------|------|
| `Assets/Scripts/Ship/Input/InputHandler.cs` | 添加 `using ProjectArk.Core;`，在 `Awake()` 中 `ServiceLocator.Register(this)`，新增 `OnDestroy()` 取消注册 | Phase 1 验证时已添加但被 git reset 回退，DoorTransitionController 和所有 Phase 2 系统均依赖此注册 |
| `Assets/Scripts/Ship/Combat/ShipHealth.cs` | 新增 `Heal(float amount)` 公共方法 | HealthPickup (L8) 需要按固定量恢复 HP |

### L6: CheckpointSystem（检查点系统）

| 文件 | 类型 | 目的 |
|------|------|------|
| `Assets/Scripts/Level/Data/CheckpointSO.cs` | **新建** | 检查点配置 SO：CheckpointID、DisplayName、RestoreHP/RestoreHeat 开关、ActivationSFX |
| `Assets/Scripts/Level/Checkpoint/Checkpoint.cs` | **新建** | 场景检查点组件：Trigger 检测 + Interact 激活，恢复 HP/热量，通知 CheckpointManager，Sprite 颜色视觉反馈 |
| `Assets/Scripts/Level/Checkpoint/CheckpointManager.cs` | **新建** | 检查点管理器：ServiceLocator 注册，追踪活跃检查点/重生位置，广播 LevelEvents.RaiseCheckpointActivated，自动存档 |

**设计要点**：
- Checkpoint 在 OnEnable/OnDisable 订阅 InputHandler.OnInteractPerformed，玩家进入范围后按 Interact 激活
- CheckpointManager.GetCheckpointRoom() 通过 GetComponentInParent<Room>() 找到检查点所在房间，供 GameFlowManager 死亡重生使用
- 激活检查点时自动调用 SaveManager.Save() 持久化

### L8: ItemPickup（物品拾取系统）

| 文件 | 类型 | 目的 |
|------|------|------|
| `Assets/Scripts/Level/Pickup/PickupBase.cs` | **新建** | 抽象拾取基类：auto vs interact 拾取模式，PrimeTween bob 浮动动画，拾取后缩小消失动画，UniTask 异步 |
| `Assets/Scripts/Level/Pickup/KeyPickup.cs` | **新建** | 钥匙拾取：OnPickedUp → KeyInventory.AddKey() |
| `Assets/Scripts/Level/Pickup/HealthPickup.cs` | **新建** | 血量回复拾取：OnPickedUp → ShipHealth.Heal() |
| `Assets/Scripts/Level/Pickup/HeatPickup.cs` | **新建** | 热量清空拾取：OnPickedUp → HeatSystem.ResetHeat() |

**设计要点**：
- PickupBase 用 PrimeTween.LocalPositionY 做无限循环 bob 动画，拾取后 Tween.Scale 缩到 0 再 SetActive(false)
- _autoPickup=true 时碰到自动拾取，false 时需要外部调用 TryInteractPickup()
- 所有 Pickup 都兼容对象池（OnEnable 重置 consumed/scale/position）

### L7: LockKeySystem（锁钥系统）

| 文件 | 类型 | 目的 |
|------|------|------|
| `Assets/Scripts/Level/Data/KeyItemSO.cs` | **新建** | 钥匙 SO：KeyID、DisplayName、Icon、Description |
| `Assets/Scripts/Level/Progression/KeyInventory.cs` | **新建** | 玩家钥匙背包：HashSet<string> 存储，ServiceLocator 注册，序列化到 ProgressSaveData.Flags（key_ 前缀） |
| `Assets/Scripts/Level/Progression/Lock.cs` | **新建** | 锁组件：Trigger + Interact 检测，检查 KeyInventory.HasKey()，成功 → Door.SetState(Open) + 音效；失败 → 提示音 |
| `Assets/Scripts/Level/Room/Door.cs` | **修改** | 新增 `[SerializeField] string _requiredKeyID` 字段和 `RequiredKeyID` 属性，供 Lock/UI 查询 |

**设计要点**：
- KeyInventory 用 ProgressSaveData.Flags 持久化（key 格式: `key_{keyID}`），与通用 flag 系统共存
- Lock 组件可选消耗钥匙（_consumeKey），解锁后自动 disabled
- Door 新增 RequiredKeyID 为可选元数据，Lock 组件自身持有完整解锁逻辑

### L9: Death & Respawn（死亡与重生）

| 文件 | 类型 | 目的 |
|------|------|------|
| `Assets/Scripts/Level/GameFlow/GameFlowManager.cs` | **新建** | 游戏流程管理器：订阅 ShipHealth.OnDeath，异步编排死亡→黑屏→传送→重生序列 |
| `Assets/Scripts/Level/Room/Room.cs` | **修改** | 新增 `ResetEnemies()` 方法：关闭所有敌人 → 重置房间状态 → 解锁战斗门 → 重新激活 |

**死亡→重生序列（async UniTaskVoid）**：
1. 禁用输入
2. 死亡音效
3. PrimeTween 淡黑 (0.5s)
4. 黑屏停留 (1s)
5. 获取重生位置 (CheckpointManager.GetRespawnPosition)
6. 传送玩家 + 清零速度
7. 切换到检查点房间 (RoomManager.EnterRoom)
8. 重置 HP + 热量
9. 重置当前房间敌人 (Room.ResetEnemies)
10. 重生音效
11. 淡入 (0.5s)
12. 恢复输入
13. 存档

**设计要点**：
- 复用与 DoorTransitionController 相同的 fade Image（可共享或独立分配）
- CancellationTokenSource 绑定 destroyCancellationToken 防止对象销毁后继续执行
- Room.ResetEnemies() 会将 Cleared 状态回退到 Entered，允许重新触发战斗锁门

### L9.5: WorldProgressManager（世界进度管理器）

| 文件 | 类型 | 目的 |
|------|------|------|
| `Assets/Scripts/Level/Data/WorldProgressStageSO.cs` | **新建** | 世界进度阶段 SO：StageIndex、StageName、RequiredBossIDs、UnlockDoorIDs |
| `Assets/Scripts/Level/Progression/WorldProgressManager.cs` | **新建** | 世界进度管理器：订阅 LevelEvents.OnBossDefeated，数据驱动阶段推进，广播 OnWorldStageChanged |
| `Assets/Scripts/Core/Save/SaveData.cs` | **修改** | ProgressSaveData 新增 `int WorldStage` 字段 |

**设计要点**：
- WorldProgressStageSO 定义阶段推进条件（RequiredBossIDs 全部满足才升级）
- 阶段推进是单向的（irreversible），按顺序检查（遇到未满足条件即停止）
- UnlockDoorIDs 目前仅日志输出，完整实现需要 Door 注册表（留作后续扩展）

### 新增文件夹结构

```
Assets/Scripts/Level/
├── Checkpoint/
│   ├── Checkpoint.cs
│   └── CheckpointManager.cs
├── Pickup/
│   ├── PickupBase.cs
│   ├── KeyPickup.cs
│   ├── HealthPickup.cs
│   └── HeatPickup.cs
├── Progression/
│   ├── KeyInventory.cs
│   ├── Lock.cs
│   └── WorldProgressManager.cs
├── GameFlow/
│   └── GameFlowManager.cs
├── Data/
│   ├── CheckpointSO.cs    (new)
│   ├── KeyItemSO.cs        (new)
│   └── WorldProgressStageSO.cs (new)
└── ... (existing Phase 1 files)
```

### 技术

- **UniTask**：GameFlowManager 的死亡重生序列使用 async UniTaskVoid + CancellationTokenSource，零 GC
- **PrimeTween**：PickupBase bob 动画（无限循环 Yoyo），拾取消失动画（Scale InBack），GameFlowManager 淡入淡出
- **ServiceLocator**：CheckpointManager、KeyInventory、GameFlowManager、WorldProgressManager 全部注册，InputHandler 补注册
- **LevelEvents 静态事件总线**：OnCheckpointActivated（L6）、OnBossDefeated（L9.5 订阅）、OnWorldStageChanged（L9.5 广播）
- **SaveManager 集成**：检查点激活自动存档，世界进度通过 ProgressSaveData.WorldStage 和 DefeatedBossIDs 持久化，钥匙通过 Flags 持久化

### 编辑器配置清单（需用户在 Unity Editor 中完成）

1. **Physics2D 碰撞矩阵**：确保 Checkpoint/Pickup/Lock 的 Trigger 层与 Player 层碰撞
2. **场景 GameObject**：创建 CheckpointManager、KeyInventory、GameFlowManager、WorldProgressManager 空 GameObject 并挂载对应脚本
3. **GameFlowManager**：拖入 FadeImage 引用（可与 DoorTransitionController 共享）
4. **创建 SO 资产**：在 `Assets/_Data/Level/Checkpoints/` 和 `Assets/_Data/Level/Keys/` 下创建 CheckpointSO 和 KeyItemSO 资产
5. **Checkpoint Prefab**：Collider2D (Trigger) + SpriteRenderer + Checkpoint 组件，配置 PlayerLayer 和 CheckpointSO 引用
6. **Pickup Prefab**：Collider2D (Trigger) + SpriteRenderer + 对应 Pickup 子类组件
7. **Lock Prefab / 配置**：在需要锁定的门附近放置 Lock 组件，引用 KeyItemSO 和 Door

---

## LevelAssetCreator 一键创建 Phase 2 SO 资产 — 2026-02-13 09:22

### 新建文件

| 文件 | 目的 |
|------|------|
| `Assets/Scripts/Level/Editor/ProjectArk.Level.Editor.asmdef` | Level 模块 Editor 脚本程序集定义（仅 Editor 平台），引用 ProjectArk.Level + ProjectArk.Core |
| `Assets/Scripts/Level/Editor/LevelAssetCreator.cs` | 一键创建 Phase 2 全部 SO 资产的编辑器工具 |

### 菜单项

| 菜单路径 | 功能 |
|----------|------|
| `ProjectArk > Level > Create Phase 2 Assets (All)` | 一键创建所有 Checkpoint + Key + WorldStage 资产 |
| `ProjectArk > Level > Create Checkpoint Assets` | 仅创建 CheckpointSO 资产 |
| `ProjectArk > Level > Create Key Item Assets` | 仅创建 KeyItemSO 资产 |
| `ProjectArk > Level > Create World Progress Stage Assets` | 仅创建 WorldProgressStageSO 资产 |

### 创建的资产

| 资产路径 | 内容 |
|---------|------|
| `Assets/_Data/Level/Checkpoints/Checkpoint_Start.asset` | 起始锚点，恢复 HP+热量 |
| `Assets/_Data/Level/Checkpoints/Checkpoint_Corridor.asset` | 走廊锚点，恢复 HP+热量 |
| `Assets/_Data/Level/Checkpoints/Checkpoint_Combat.asset` | 战斗区锚点，仅恢复 HP（不恢复热量） |
| `Assets/_Data/Level/Keys/Key_AccessAlpha.asset` | Alpha 通行证（测试用钥匙） |
| `Assets/_Data/Level/Keys/Key_BossGate.asset` | 核心门钥（Boss 区域钥匙） |
| `Assets/_Data/Level/WorldStages/Stage_0_Initial.asset` | 初始阶段（无条件） |
| `Assets/_Data/Level/WorldStages/Stage_1_PostGuardian.asset` | Guardian Boss 击败后解锁 |

### 技术

- 遵循 EnemyAssetCreator 的幂等模式：已存在的资产跳过，不会重复创建
- 使用 SerializedObject + FindProperty 写入私有 `[SerializeField]` 字段
- 所有音效/图标引用留空，等美术资源就绪后手动分配

---

## Door 转场卡门修复 + OnTriggerStay2D — 2026-02-13 10:00

### 修改文件

| 文件 | 变更 | 目的 |
|------|------|------|
| `Assets/Scripts/Level/Room/Door.cs` | 新增 `OnTriggerStay2D` 和 `TryAutoTransition()` 方法 | 修复传送后"卡门"问题 |

### 问题

玩家从 Door_ToCoridor 传送到 Room_Corridor 后，SpawnPoint 恰好在 Door_ToStart 的 Trigger 范围内。`OnTriggerEnter2D` 在转场尚未结束时触发，被 `DoorTransitionController.IsTransitioning` 拒绝。转场结束后玩家一直在 Trigger 内，`OnTriggerEnter2D` 不会再次触发，导致门永远无法通过。

### 修复

- 新增 `OnTriggerStay2D`：每个 FixedUpdate 帧检测，如果玩家仍在范围内且转场已结束，重新尝试过渡
- 提取 `TryAutoTransition()` 统一入口：检查 Door 自身状态 + `DoorTransitionController.IsTransitioning` 全局锁
- 性能影响可忽略：仅在玩家处于 Door Trigger 内时触发，方法体全是 O(1) 检查

---

## Door 钥匙自动检查 — 2026-02-13 10:30

### 修改文件

| 文件 | 变更 | 目的 |
|------|------|------|
| `Assets/Scripts/Level/Room/Door.cs` | `TryAutoTransition()` 中集成 `Locked_Key` 自动钥匙验证逻辑 | 玩家碰到锁定门时自动检查背包，有钥匙即解锁并传送，无钥匙 Console 提示 |

### 行为变更

- **无钥匙碰门**：Console 输出 `需要钥匙 'xxx' 才能通过！`（单次提示，离开后重进才会再提示，避免 Stay 每帧刷屏）
- **有钥匙碰门**：Console 输出 `持有钥匙 'xxx'，门自动解锁！` → `SetState(Open)` → 立刻触发传送
- **Open 状态门**：碰到直接传送（不变）
- 新增 `_hasLoggedMissingKey` 标记：`OnTriggerExit2D` 时重置

### 设计决策

将钥匙验证逻辑从 Lock 组件迁入 Door 本身。原因：
1. 玩家期望碰到门自动处理，不需要额外按 Interact
2. Door 已持有 `_requiredKeyID` 字段，天然拥有所需信息
3. 减少配置复杂度（不需要额外挂 Lock 组件 + 配置 Collider）
4. Lock 组件仍保留用于更复杂的场景（如远离门的独立开关）

### 技术

- `ServiceLocator.Get<KeyInventory>()` 查询玩家钥匙背包
- `_hasLoggedMissingKey` 防止 `OnTriggerStay2D` 每帧输出日志

---

## Level Module Phase 3 — 战斗房间逻辑 — 2026-02-13 11:15

### L10: EncounterSystem（波次生成系统）

#### 新建文件

| 文件 | 内容 | 目的 |
|------|------|------|
| `Assets/Scripts/Combat/Enemy/ISpawnStrategy.cs` | 策略接口，定义 `Initialize/Start/OnEnemyDied/IsEncounterComplete/Reset` | 将生成行为抽象为可插拔策略，解耦 EnemySpawner 与具体逻辑 |
| `Assets/Scripts/Combat/Enemy/LoopSpawnStrategy.cs` | 循环刷怪策略（封装原有 EnemySpawner 逻辑） | 向后兼容：维持固定存活数，死亡后延迟重生，`IsEncounterComplete` 永远为 false |
| `Assets/Scripts/Level/Room/WaveSpawnStrategy.cs` | EncounterSO 驱动的波次生成策略 | 逐波生成敌人，当前波全灭后延迟 `DelayBeforeWave` 秒启动下一波，全部波次完成触发 `OnEncounterComplete` 事件 |

#### 修改文件

| 文件 | 变更 | 目的 |
|------|------|------|
| `Assets/Scripts/Combat/Enemy/EnemySpawner.cs` | 完全重构为策略模式上下文 | 保留通用基建（生成点轮询、精英词缀、对象池），新增 `SetStrategy()`/`StartStrategy()`/`SpawnFromPool(prefab)`/`ResetSpawner()` 公共 API，新增 `OnEncounterComplete` 事件 |
| `Assets/Scripts/Level/Room/Room.cs` | 重写 `ActivateEnemies()` 集成 WaveSpawnStrategy | 有 EncounterSO + EnemySpawner 且未清除 → 创建并启动 WaveSpawnStrategy；否则回退到激活预放置子对象。新增 `HandleEncounterComplete()` → 调用 `RoomManager.NotifyRoomCleared()`。`ResetEnemies()` 增加 spawner 和策略重置逻辑 |

#### 架构决策

- `WaveSpawnStrategy` 放在 `ProjectArk.Level` 程序集（依赖 `EncounterSO`），`ISpawnStrategy`/`LoopSpawnStrategy` 放在 `ProjectArk.Combat`（与 `EnemySpawner` 同级）。避免 Level→Combat 循环引用。
- `EnemySpawner` 只认识 `ISpawnStrategy` 接口，不依赖具体策略实现类。
- 多 Prefab 的池管理通过 `PoolManager.Instance.GetPool(prefab)` 按需创建，单 Prefab legacy 模式复用 `_legacyPool`。

#### 技术

- Strategy 模式（ISpawnStrategy → LoopSpawnStrategy / WaveSpawnStrategy）
- UniTask 异步波次间延迟 + CancellationTokenSource 生命周期管理
- PoolManager.GetPool() 按 Prefab InstanceID 索引，支持多种敌人 Prefab 共存

---

### L11: ArenaController（竞技场编排器）

#### 新建文件

| 文件 | 内容 | 目的 |
|------|------|------|
| `Assets/Scripts/Level/Room/ArenaController.cs` | Arena/Boss 房间战斗编排器 | 完整的遭遇序列：锁门→警报音效→预延迟→启动 WaveSpawnStrategy→全清→后延迟→解锁门→胜利音效→可选奖励掉落→标记 Cleared |

#### 修改文件

| 文件 | 变更 | 目的 |
|------|------|------|
| `Assets/Scripts/Level/Room/RoomManager.cs` | `EnterRoom()` 中 Arena/Boss 分支改为检测 ArenaController | 有 ArenaController 则委托其编排（包含锁门），无则回退到仅锁门（向后兼容） |

#### 设计

- `ArenaController` 挂在 Arena/Boss 类型的 Room 上，`[RequireComponent(typeof(Room))]`
- 全异步流程用 `async UniTaskVoid`，绑定 `destroyCancellationToken`
- Cleared 房间再次进入不重复触发（`BeginEncounter()` 检查 `RoomState.Cleared`）
- 音效通过 `ServiceLocator.Get<AudioManager>().PlaySFX2D()` 播放
- 奖励暂用 `Instantiate`（非战斗循环内，允许），后续可改对象池

#### 技术

- UniTask 异步遭遇序列
- 事件驱动：WaveSpawnStrategy.OnEncounterComplete → ArenaController.HandleWavesCleared
- ServiceLocator 获取 AudioManager、RoomManager

---

### L12: Hazard System（环境机关系统）

#### 新建文件

| 文件 | 内容 | 目的 |
|------|------|------|
| `Assets/Scripts/Level/Hazard/EnvironmentHazard.cs` | 抽象基类 | 统一伤害/击退/目标层配置，通过 `IDamageable.TakeDamage(DamagePayload)` 走统一伤害管线 |
| `Assets/Scripts/Level/Hazard/DamageZone.cs` | 持续伤害区域（酸液池/辐射区） | `OnTriggerStay2D` + `_tickInterval` 定时器，`Dictionary<int, float>` 追踪每目标下次伤害时间 |
| `Assets/Scripts/Level/Hazard/ContactHazard.cs` | 接触伤害（激光栅栏/电弧） | `OnTriggerEnter2D` 首次接触立即伤害 + `_hitCooldown` 冷却，`OnTriggerStay2D` 处理冷却期内进入的目标 |
| `Assets/Scripts/Level/Hazard/TimedHazard.cs` | 周期性开关机关（钻头陷阱/间歇激光） | `Update()` 中按 `_activeDuration`/`_inactiveDuration` 循环切换 Collider 启用状态，激活时按 ContactHazard 逻辑伤害，支持 `_startDelay` 和 `SpriteRenderer` alpha 视觉同步 |

#### 架构决策

- 继承层次：`EnvironmentHazard`（abstract）→ `DamageZone` / `ContactHazard` / `TimedHazard`
- 所有 Hazard 均在 `ProjectArk.Level` 程序集，引用 `ProjectArk.Core`（DamagePayload/IDamageable/DamageType）
- `EnvironmentHazard` 的 `Awake()` 自动修复非 trigger 的 Collider2D
- 每种 Hazard 的冷却/伤害追踪使用 `Dictionary<int, float>`（InstanceID → expiry time），`OnTriggerExit2D` / `OnDisable` 清理

#### 技术

- Template Method 模式（基类 `EnvironmentHazard` 提供 `ApplyDamage()`/`IsValidTarget()`，子类实现触发逻辑）
- `DamagePayload` 结构体走统一伤害管线
- `LayerMask _targetLayer` 显式声明目标层（遵循项目规范，禁止 `~0`）
- `TimedHazard` 的 `Update()` 使用模运算实现周期循环，避免额外计时器

---

## Level Phase 4-5: Map / Save / Multi-Floor / Enhanced Transitions

**实施时间：** 当前会话
**阶段：** Level Phase 4 (Map & Exploration) + Phase 5 (Multi-Floor Structure)
**前置依赖：** Phase 1-3 (L1-L12) 全部完成，架构基建大修完成

---

### L13A: MinimapManager + MapRoomData + LevelEvents Extensions

**文件：**

| 路径 | 操作 | 用途 |
|------|------|------|
| `Assets/Scripts/Core/LevelEvents.cs` | 修改 | 新增 `OnRoomFirstVisit(string)` 和 `OnFloorChanged(int)` 事件 |
| `Assets/Scripts/Level/Map/MapRoomData.cs` | 新建 | 轻量房间地图数据结构体（RoomID、世界坐标、楼层、类型、访问状态） |
| `Assets/Scripts/Level/Map/MapConnection.cs` | 新建 | 房间连接结构体（FromRoomID、ToRoomID、中点、是否层间） |
| `Assets/Scripts/Level/Map/MinimapManager.cs` | 新建 | ServiceLocator 注册的地图数据管理器。场景初始化时收集所有 Room，通过 Door 引用构建邻接图，跟踪已访问房间集合。提供 API：GetRoomNodes/GetConnections/IsVisited/MarkVisited/CurrentFloor/GetFloorsDiscovered |
| `Assets/Scripts/Level/ProjectArk.Level.asmdef` | 修改 | 添加 `Unity.TextMeshPro` 和 `Unity.InputSystem` 引用 |

**技术要点：**
- MinimapManager 在 `Start()` 中通过 `FindObjectsByType<Room>` 收集场景数据
- 邻接图通过 Door.TargetRoom 引用构建，使用 HashSet 去重双向连接
- 楼层变化检测由 MinimapManager.HandleRoomChanged 驱动，触发 LevelEvents.OnFloorChanged
- 已访问房间从 SaveManager 加载/保存

---

### L13B: MapPanel + MapRoomWidget + MapConnectionLine + Input Action

**文件：**

| 路径 | 操作 | 用途 |
|------|------|------|
| `Assets/Scripts/Level/Map/MapRoomWidget.cs` | 新建 | 单个房间节点 UI 组件。根据 RoomType 着色（Normal/Arena/Boss/Safe），显示图标覆盖、当前高亮环、未访问迷雾 |
| `Assets/Scripts/Level/Map/MapConnectionLine.cs` | 新建 | 房间连接线 UI。使用拉伸 Image 绘制线段，支持层间连接差异化样式 |
| `Assets/Scripts/Level/Map/MapPanel.cs` | 新建 | 全屏地图面板。M 键（ToggleMap action）切换，ScrollRect 支持平移，滚轮缩放，楼层 Tab 切换，玩家图标定位 |
| `Assets/Input/ShipActions.inputactions` | 修改 | Ship map 新增 ToggleMap Button action，绑定 Keyboard/M 和 Gamepad/select |

**技术要点：**
- MapPanel 独立处理输入（不通过 UIManager），因为地图不需要暂停游戏
- 世界坐标到地图坐标使用可配置 _worldToMapScale 缩放因子
- 连接线通过 `transform.SetAsFirstSibling()` 渲染在房间节点下方
- 楼层 Tab 使用闭包捕获 floor index 绑定 onClick

---

### L13C: MinimapHUD Corner Overlay

**文件：**

| 路径 | 操作 | 用途 |
|------|------|------|
| `Assets/Scripts/Level/Map/MinimapHUD.cs` | 新建 | 屏幕角落小地图叠加层。以当前房间为中心显示可见半径内的房间，自动跟随房间切换刷新，显示楼层标签 |

**技术要点：**
- 使用可配置 `_visibleRadius`（世界单位）过滤远距离房间
- 每次进入新房间时完全重建（Rebuild），因为小地图元素数量少，性能开销可接受
- 订阅 `LevelEvents.OnRoomEntered` 和 `LevelEvents.OnFloorChanged` 触发刷新

---

### L14: SaveBridge + Save System Integration

**文件：**

| 路径 | 操作 | 用途 |
|------|------|------|
| `Assets/Scripts/Level/SaveBridge.cs` | 新建 | 集中式存档收集器。从 MinimapManager/KeyInventory/WorldProgressManager/ShipHealth/CheckpointManager 收集数据调用 SaveManager.Save；加载时反向分发到各子系统 |
| `Assets/Scripts/Level/Checkpoint/CheckpointManager.cs` | 修改 | `SaveProgress` 方法委托给 SaveBridge.SaveAll()，保留 fallback 直接存档 |
| `Assets/Scripts/Level/GameFlow/GameFlowManager.cs` | 修改 | `Start()` 中调用 SaveBridge.LoadAll() 加载存档；死亡重生存档委托给 SaveBridge |
| `Assets/Scripts/Level/Progression/WorldProgressManager.cs` | 修改 | `SaveProgress` 方法委托给 SaveBridge.SaveAll()，保留 fallback |
| `Assets/Scripts/Level/Room/RoomManager.cs` | 修改 | `EnterRoom()` 中首次访问时调用 MinimapManager.MarkVisited()；新增 `CurrentFloor` 便利属性 |

**技术要点：**
- SaveBridge 注册到 ServiceLocator，各子系统通过 `ServiceLocator.Get<SaveBridge>()` 调用
- 所有修改保留 fallback 逻辑（SaveBridge 不存在时回退到直接存档），确保向后兼容
- SaveBridge.LoadAll() 在 GameFlowManager.Start() 中最先执行，确保子系统在激活前获得存档数据

---

### L15: ShebaLevelScaffolder Editor Tool

**文件：**

| 路径 | 操作 | 用途 |
|------|------|------|
| `Assets/Scripts/Level/Editor/ProjectArk.Level.Editor.asmdef` | 新建 | Editor-only 程序集定义，includePlatforms = ["Editor"] |
| `Assets/Scripts/Level/Editor/ShebaLevelScaffolder.cs` | 新建 | 菜单 `ProjectArk > Scaffold Sheba Level`。创建 12 个 Room GameObject（含 BoxCollider2D、PolygonCollider2D confiner、Room 组件、SpawnPoints），匹配的 RoomSO 和 EncounterSO 资产，双向 Door 连接 |

**技术要点：**
- 12 间房间布局：Entrance → Hub → 双分支走廊 → Arena×2 → Key Chamber → Rest Station → Boss Antechamber → Boss + Underground 层
- 使用 SerializedObject 设置私有字段（遵循不直接修改 YAML 的原则）
- EncounterSO 自动分配不同的波次配置（Patrol_Light/Mixed_Medium/Arena_Heavy/Boss）
- 双向门连接：每个 DoorDef 同时在源和目标房间创建对应的 Door + SpawnPoint
- 执行完毕后在 Console 输出 10 步验收清单

---

### L16+L18: Floor Awareness + Minimap Floor Switching

**文件：** 已在 L13A-C 中前置实现

- `MinimapManager.CurrentFloor` / `GetFloorsDiscovered()` / `GetRoomNodes(int floor)` — L13A
- `RoomManager.CurrentFloor` 便利属性 — L14
- `LevelEvents.OnFloorChanged(int)` — L13A（由 MinimapManager 触发）
- `MapPanel` 楼层 Tab 栏 — L13B
- `MinimapHUD` 楼层标签 + 自动切换 — L13C

---

### L17: Enhanced Layer Transition

**文件：**

| 路径 | 操作 | 用途 |
|------|------|------|
| `Assets/Scripts/Level/Data/RoomSO.cs` | 修改 | 新增 `_ambientMusic` AudioClip 字段（per-room BGM 覆盖） |
| `Assets/Scripts/Level/Room/DoorTransitionController.cs` | 修改 | 新增层间转场增强效果：粒子特效、SFX、BGM crossfade、相机 zoom-out/snap-back |

**技术要点：**
- 粒子方向基于 currentFloor vs targetFloor 判断上升/下降
- 相机 zoom 使用 PrimeTween.Custom 动画 orthographicSize
- BGM crossfade 通过 AudioManager.PlayMusic(clip, fadeDuration) 实现
- 所有新增字段标记为 Optional（[SerializeField] 可空），不影响现有非层间转场

---

### L19: NarrativeFallTrigger Placeholder

**文件：**

| 路径 | 操作 | 用途 |
|------|------|------|
| `Assets/Scripts/Level/Narrative/NarrativeFallTrigger.cs` | 新建 | 叙事坠落触发器占位符。含 SerializeField（TargetRoom、LandingPoint、ParticlePrefab、Timeline、SFX、相机参数），TriggerFall() 方法含 9 步 TODO 注释描述完整实现。当前仅执行最小功能：传送 + 换房间 |

**技术要点：**
- 严格遵循计划：仅占位，不实现完整功能
- TriggerFall() 的 TODO 注释列出完整 9 步实现方案（供未来叙事过场开发参考）
- 支持 PlayableAsset（Timeline）引用，为未来 Timeline 集成预留接口
- ResetTrigger() 方法用于死亡/重生后重置触发器状态

---

## Ship Feel Enhancement — 飞船手感增强系统 — 2026-02-13 15:27

### 概述

全面升级飞船操控手感，涵盖曲线驱动移动、Dash/闪避系统、受击反馈（HitStop + 屏幕震动 + 无敌帧）、以及视觉 Juice（倾斜、Squash/Stretch、引擎粒子、Dash 残影）。所有参数通过 ScriptableObject 暴露，支持 Play Mode 热调参。

### 新建文件

| 路径 | 用途 |
|------|------|
| `Assets/Scripts/Ship/Data/ShipJuiceSettingsSO.cs` | 视觉 Juice 参数 SO（倾斜角度、Squash/Stretch 强度、残影数量、引擎粒子阈值） |
| `Assets/Scripts/Ship/Movement/ShipDash.cs` | Dash/闪避核心逻辑（async UniTaskVoid、输入缓冲、冷却、无敌帧、出口动量保留） |
| `Assets/Scripts/Ship/Input/InputBuffer.cs` | 通用输入缓冲工具类（Record/Consume/Peek/Clear） |
| `Assets/Scripts/Ship/Combat/HitFeedbackService.cs` | 静态服务：HitStop（Time.timeScale 冻结）+ ScreenShake（Cinemachine Impulse） |
| `Assets/Scripts/Ship/VFX/ShipVisualJuice.cs` | 移动倾斜 + Squash/Stretch（操控视觉子物体，PrimeTween 补间） |
| `Assets/Scripts/Ship/VFX/ShipEngineVFX.cs` | 引擎尾焰粒子控制（发射率/尺寸随速度缩放，Dash 爆发） |
| `Assets/Scripts/Ship/VFX/DashAfterImage.cs` | 残影单体组件（池化 Prefab，PrimeTween Alpha 淡出，IPoolable） |
| `Assets/Scripts/Ship/VFX/DashAfterImageSpawner.cs` | 残影生成器（监听 ShipDash 事件，等间距从对象池生成残影） |

### 修改文件

| 路径 | 变更内容 |
|------|----------|
| `Assets/Scripts/Ship/Data/ShipStatsSO.cs` | 新增 Movement Curves（AccelerationCurve、DecelerationCurve、SharpTurn、InitialBoost）、Dash 参数（Speed/Duration/Cooldown/Buffer/ExitRatio/IFrames）、HitFeedback 参数（HitStop/IFrame/ScreenShake） |
| `Assets/Scripts/Ship/Movement/ShipMotor.cs` | 重写为曲线驱动移动：AnimationCurve.Evaluate 加减速、急转弯惩罚、首帧 Boost、IsDashing 属性、SetVelocityOverride/ClearVelocityOverride API |
| `Assets/Scripts/Ship/Input/InputHandler.cs` | 新增 `_dashAction` + `OnDashPressed` 事件 + OnEnable/OnDisable 回调绑定 |
| `Assets/Scripts/Ship/Combat/ShipHealth.cs` | 新增 `IsInvulnerable` + `SetInvulnerable()` + `IFrameBlinkAsync()` 闪烁无敌帧 + TakeDamage 集成 HitStop/ScreenShake |
| `Assets/Scripts/Ship/ProjectArk.Ship.asmdef` | 新增 `Unity.Cinemachine` + `PrimeTween.Runtime` 程序集引用 |

### 技术要点

- **曲线驱动移动**：`AnimationCurve.Evaluate(progress)` 替代线性 MoveTowards，progress 基于当前速度/最大速度比值
- **急转弯惩罚**：`Vector2.Angle()` 检测输入方向 vs 当前速度方向，超过阈值（默认 90°）时施加速度乘数惩罚
- **Dash 异步流程**：`async UniTaskVoid` + `destroyCancellationToken` 生命周期管理，无 Coroutine
- **输入缓冲**：通用 `InputBuffer` 类，基于 `Time.unscaledTime` 时间戳 + 窗口消费机制
- **HitStop**：`Time.timeScale = 0` + `UniTask.Delay(ignoreTimeScale: true)` 实现帧冻结
- **ScreenShake**：`CinemachineImpulseSource.GenerateImpulse(intensity)` 需外部注册
- **无敌帧闪烁**：SpriteRenderer alpha 交替 1.0/0.3，CancellationTokenSource 管理可取消
- **残影系统**：PoolManager 对象池 + PrimeTween.Tween.Alpha 淡出 + IPoolable 回收清理
- **视觉子物体分离**：ShipVisualJuice 操控 child Transform，不干扰 physics/aiming
- **向后兼容**：保留 `ApplyImpulse()` 接口和 `OnSpeedChanged` 事件

### Play Mode 验证清单

- [ ] 加速曲线：起步有推力爆发感
- [ ] 减速曲线：松手后有滑行惯性
- [ ] 急转弯：90°+ 转向时明显减速
- [ ] Dash：按下后快速冲刺，冷却 0.3s 可再次使用
- [ ] Dash 无敌：冲刺期间不受伤
- [ ] Dash 动量保留：冲刺结束后有惯性延续
- [ ] Dash 输入缓冲：冷却结束前按 Dash 可自动执行
- [ ] HitStop：受伤瞬间有短暂顿帧
- [ ] 屏幕震动：受伤时摄像机抖动
- [ ] 无敌帧：受伤后 1s 内不再受伤，精灵闪烁
- [ ] 移动倾斜：横移时飞船视觉倾斜
- [ ] 引擎粒子：移动时有尾焰
- [ ] Dash 残影：冲刺时身后留下半透明残影
- [ ] 热调参：Play Mode 中修改 SO 参数即时生效
- [ ] `ApplyImpulse()` 仍正常工作

---

## 杂项

### CreateAssetMenu menuName 统一 — 2026-02-13 15:40

**修改文件：**
- `Assets/Scripts/Level/Data/CheckpointSO.cs`
- `Assets/Scripts/Level/Data/EncounterSO.cs`
- `Assets/Scripts/Level/Data/KeyItemSO.cs`
- `Assets/Scripts/Level/Data/RoomSO.cs`
- `Assets/Scripts/Level/Data/WorldProgressStageSO.cs`

**内容：** 将 `[CreateAssetMenu]` 的 `menuName` 参数从 `"Project Ark/Level/..."` (带空格) 统一为 `"ProjectArk/Level/..."` (无空格)。

**目的：** 消除 Unity Editor Project 右键 Create 菜单中出现两个分组（"ProjectArk" 和 "Project Ark"）的问题，统一到 `ProjectArk` 分组下。

**技术：** `[CreateAssetMenu]` attribute menuName 字符串修改。

---

### 飞船手感 SO 资产一键导入脚本 — 2026-02-13 15:42

**新建文件：**
- `Assets/Scripts/Ship/Editor/ProjectArk.Ship.Editor.asmdef` — Ship Editor 程序集定义（Editor-only，引用 ProjectArk.Ship）
- `Assets/Scripts/Ship/Editor/ShipFeelAssetCreator.cs` — 一键创建/更新 ShipStatsSO 和 ShipJuiceSettingsSO 资产

**内容：**
- `ShipFeelAssetCreator` 提供 3 个菜单入口：
  - `ProjectArk > Ship > Create Ship Feel Assets (All)` — 一键创建所有资产
  - `ProjectArk > Ship > Create Ship Stats Asset` — 仅创建/更新 ShipStatsSO
  - `ProjectArk > Ship > Create Ship Juice Settings Asset` — 仅创建 ShipJuiceSettingsSO
- 幂等设计：已存在的资产不会重复创建
- 对已存在的 `DefaultShipStats.asset`，会智能填充新增字段（Dash/HitFeedback/Curves）的默认值，同时保留用户已手动调整的旧字段值
- 所有参数通过 `SerializedProperty` 精确设置，与 SO 中的 `[SerializeField]` 字段一一对应

**目的：** 让用户无需手动在 Inspector 中逐个配置新增的 20+ 个参数，一键即可生成完整配置的 SO 资产。

**技术：** `UnityEditor.MenuItem` + `SerializedObject` / `SerializedProperty` API + `AssetDatabase.CreateAsset` + 幂等检查。

---

### DashAfterImage PrimeTween API 修复 — 2026-02-13 16:00

**修改文件：**
- `Assets/Scripts/Ship/VFX/DashAfterImage.cs`

**内容：** 将 `Tween.Alpha(_spriteRenderer, ...)` 替换为 `Tween.Custom(this, startAlpha, 0f, ...)` + `.OnComplete(this, target => target.ReturnToPool())`。

**目的：** 修复编译错误 CS1739。PrimeTween 的 `Tween.Alpha()` 仅支持 UI Graphic 组件，不支持 SpriteRenderer，且无 `onComplete` 命名参数。

**技术：** 使用 `Tween.Custom<T>` 零 GC 重载手动插值 SpriteRenderer.color.a，配合零 GC `OnComplete(target, callback)` 链式调用触发回池。

---

### 批量修复 CS0618 + CS4014 编译器警告 — 2026-02-13 16:15

**修改文件（CS0618 — Physics2D NonAlloc 弃用，共 11 处）：**
- `Assets/Scripts/Combat/Enemy/HitboxResolver.cs` — 3 处（Circle、Box、Cone）
- `Assets/Scripts/Combat/Enemy/EnemyPerception.cs` — 1 处（FactionScan）
- `Assets/Scripts/Combat/Enemy/States/AttackSubState.cs` — 1 处（Legacy fallback）
- `Assets/Scripts/Combat/Enemy/EnemyAffixController.cs` — 2 处（Explosive、Reflect）
- `Assets/Scripts/Combat/Enemy/ThreatSensor.cs` — 1 处（ProjectileScan）
- `Assets/Scripts/Combat/Enemy/States/StalkerStrikeState.cs` — 1 处（Legacy fallback）
- `Assets/Scripts/Combat/Enemy/EnemyEntity.cs` — 2 处（ResolveOverlap、GetSeparationForce）

**内容：** 将所有 `Physics2D.OverlapCircleNonAlloc(pos, radius, buffer, layerMask)` 替换为 `Physics2D.OverlapCircle(pos, radius, contactFilter, buffer)`，`OverlapBoxNonAlloc` 同理。每个类新增一个 `static ContactFilter2D` 字段，调用前通过 `SetLayerMask()` 配置。

**目的：** Unity 6 将 `NonAlloc` 后缀 API 标记为 `[Obsolete]`，新版 `OverlapCircle`/`OverlapBox` 的 `ContactFilter2D + Collider2D[]` 重载是官方替代，功能等价且同样零 GC。

**技术：** `ContactFilter2D.SetLayerMask(int)` + `Physics2D.OverlapCircle(Vector2, float, ContactFilter2D, Collider2D[])` / `Physics2D.OverlapBox(...)` 缓冲区重载。

---

**修改文件（CS4014 — 未 await 的 PrimeTween 调用，共 3 处）：**
- `Assets/Scripts/Level/Pickup/PickupBase.cs` — `Tween.Scale` fire-and-forget
- `Assets/Scripts/Level/GameFlow/GameFlowManager.cs` — 2 处 `Tween.Custom` fade 补间

**内容：** 在返回值前加 `_ =` 显式丢弃，告知编译器这是有意为之的 fire-and-forget 调用。

**目的：** 消除 CS4014 警告。PrimeTween 补间动画不需要 await（由引擎 Update 驱动），但 C# 编译器会对未 await 的异步返回值发出警告。

**技术：** C# discard `_ =` 模式。

---

### 验收清单文档可读性优化 — 2026-02-13 16:30

**修改文件：**
- `Docs/VerificationChecklist_Phase3-5_ShipFeel.md`

**内容：**
- 第三步（飞船 Prefab 配置）：新增完整 Hierarchy 树形图，说明 4 个新组件为何必须放在根 GO 上（RequireComponent 依赖），将原始密集表格拆分为 5 个子步骤（3-1 到 3-5），每个组件独立表格逐字段说明赋值来源
- 第四步 4B（Arena 配置）：拆分为 RoomSO / Room GO / ArenaController 三层，表格化字段配置
- 第四步 4C（Hazard 配置）：拆分 3 种 Hazard 各自独立步骤，每种含完整的创建+配置表格
- 第六步（Phase 4-5 UI）：每个子步骤增加概览说明（"这是什么"、"放在哪里"），Inspector 字段表格增加"拖入来源"列明确 Hierarchy vs Project 窗口，6F 管理器表格增加"挂在哪"和"是否新增"列
- 第八步（层间转场）：增加组件定位说明和 BGM 配合说明
- 第九步（NarrativeFallTrigger）：表格化 Inspector 配置

**目的：** 提升配置文档可读性，消除"组件该挂在哪个 GameObject 上"和"字段值从哪里拖入"的歧义。

**技术：** 纯文档重构，无代码变更。

---

### ImpulseSourceRegistrar 自动注册脚本 — 2026-02-13 16:40

**新建文件：**
- `Assets/Scripts/Ship/Combat/ImpulseSourceRegistrar.cs`

**内容：** MonoBehaviour，`[RequireComponent(typeof(CinemachineImpulseSource))]`，`Start()` 中自动调用 `HitFeedbackService.RegisterImpulseSource()`。

**目的：** 消除手动在初始化脚本中注册 ImpulseSource 的步骤，挂到 CinemachineCamera 上即可自动完成注册。

**技术：** `[RequireComponent]` + `GetComponent<CinemachineImpulseSource>()` + 静态服务注册。

---

### 验收清单文档二次优化（补全前置步骤） — 2026-02-13 17:00

**修改文件：**
- `Docs/VerificationChecklist_Phase3-5_ShipFeel.md`

**内容：**
- **第四步（Phase 3 关卡配置）**：全面重写，拆分为 4A~4E 五个子步骤
  - 4A（EncounterSO）：增加"EncounterSO 是什么"解释，详细说明 Waves 数组如何点 `+` 添加
  - 4B（RoomSO）：新增"RoomSO 是什么"解释，区分"从零创建"和"修改 Scaffolder 已创建的"两条路径，表格化全部字段
  - 4C（Room GameObject）：新增"Room GameObject 是什么"解释，从零创建的完整步骤（含刷怪点、EnemySpawner），已有 Room 的快捷路径
  - 4D（ArenaController）：独立为单独子步骤，解释"ArenaController 是什么"和 RequireComponent 依赖关系，附完成后的 Hierarchy 树形图
  - 4E（Hazard）：增加"Hazard 是什么"和 Layer 使用说明
- **第五步（Sheba Scaffolder）**：增加 Scaffolder 功能解释，后续手动配置拆分为 6 个编号子项，每项标注操作位置和具体字段

**目的：** 解决"Arena 是什么、从哪来、怎么配"等前置知识缺失问题，确保从零开始的用户也能按照文档独立完成配置。

**技术：** 纯文档重构，无代码变更。

---

### 修复 CameraConfiner PolygonCollider2D 阻挡飞船 — 2026-02-13 17:20

**修改文件：**
- `Assets/Scripts/Level/Editor/ShebaLevelScaffolder.cs`

**内容：**
- 修复 `CreateRoomGameObjects()` 中 CameraConfiner 子物体的 Layer：从 `Default`(0) 改为 `Ignore Raycast`(2)
- 新增 `[MenuItem("ProjectArk/Fix CameraConfiner Layers")]` 一键修复菜单，批量将场景中所有已存在的 CameraConfiner 子物体的 Layer 设为 `Ignore Raycast`

**目的：** CameraConfiner 的 PolygonCollider2D 设计上仅供 Cinemachine Confiner 读取边界，但因为 `isTrigger = false` 且 Layer = Default，Physics2D 将其视为实体碰撞墙壁，导致飞船被推到房间外无法进入。改为 `Ignore Raycast` 层后该碰撞体不再参与任何物理碰撞。

**技术：** `GameObject.layer = 2`（Ignore Raycast 内置层），Editor MenuItem + `Undo.RecordObject` 批量修复。

---

### MapUIBuilder — Map UI 一键生成工具

**文件：**

| 路径 | 操作 | 用途 |
|------|------|------|
| `Assets/Scripts/Level/Editor/MapUIBuilder.cs` | 新建 | 两个 MenuItem：`ProjectArk > Map > Build Map UI Prefabs`（创建 3 个 Prefab）和 `ProjectArk > Map > Build Map UI Scene`（创建 MapPanel + MinimapHUD 场景层级并连线所有引用） |
| `Assets/Scripts/Level/Editor/ProjectArk.Level.Editor.asmdef` | 修改 | 添加 `Unity.TextMeshPro` 和 `Unity.InputSystem` 引用 |

**技术要点：**
- 遵循 UICanvasBuilder 的模式：`WireField(Component, string, Object)` 绑定 SerializeField，`PrefabUtility.SaveAsPrefabAsset()` 保存 Prefab
- 创建 3 个 Prefab：MapRoomWidget（含 Background/IconOverlay/CurrentHighlight/FogOverlay/Label 5 个子物体）、MapConnectionLine（含 Image）、FloorTabButton（含 Button + Label）
- MapPanel 场景层级：CanvasGroup + ScrollRect（Viewport + MapContent）+ FloorTabBar（HorizontalLayoutGroup）+ PlayerIcon（菱形旋转 45°）
- MinimapHUD 场景层级：锚定右下角 200×200，含 Background + Border + 带 Mask 的 Content + FloorLabel
- 自动查找 ShipActions InputActionAsset 并赋值
- 幂等设计：已存在的 Prefab / 组件不会重复创建

---

### Level Phase 6: 世界时钟与动态关卡系统 (L20-L27) — 2026-02-15 14:30

**新建文件：**

| 路径 | 用途 |
|------|------|
| `Assets/Scripts/Level/WorldClock/WorldClock.cs` | L20: 游戏内时钟核心，ServiceLocator 注册，可配置周期长度(默认1200s)、时间倍速、暂停恢复。每帧广播 `LevelEvents.OnTimeChanged(float normalizedTime)`，周期完成时广播 `OnCycleCompleted(int cycleCount)` |
| `Assets/Scripts/Level/WorldClock/WorldPhaseManager.cs` | L21: 世界阶段管理器，ServiceLocator 注册，持有 `WorldPhaseSO[]` 阶段定义列表，监听 `OnTimeChanged` 判定当前阶段，阶段切换时广播 `LevelEvents.OnPhaseChanged(int, string)` |
| `Assets/Scripts/Level/Data/WorldPhaseSO.cs` | L21: 世界时间阶段 SO 定义，含 PhaseName、StartTime/EndTime(归一化0..1)、AmbientColor、PhaseBGM、低通滤波器开关、敌人伤害/生命倍率、隐藏通道可见性。支持跨午夜时间范围 |
| `Assets/Scripts/Level/DynamicWorld/ScheduledBehaviour.cs` | L23: 通用阶段驱动组件，配置 `_activePhaseIndices[]` 指定在哪些阶段启用目标 GameObject。支持反转逻辑。用于 NPC 交易窗口、定时门、隐藏通道等 |
| `Assets/Scripts/Level/DynamicWorld/WorldEventTrigger.cs` | L24: 进度事件驱动的永久世界变化组件。监听 `OnWorldStageChanged`，当达到 `_requiredWorldStage` 时一次性触发：启用/禁用指定 GameObject + UnityEvent 回调。通过 `ProgressSaveData.Flags` 持久化触发状态 |
| `Assets/Scripts/Level/DynamicWorld/TilemapVariantSwitcher.cs` | L26: Tilemap 变体切换器，管理多个子 Tilemap（如塌陷前/塌陷后），`SwitchToVariant(int)` 公共 API 供 UnityEvent 调用 |
| `Assets/Scripts/Level/Data/RoomVariantSO.cs` | L25: 房间变体 SO 定义，含 VariantName、`ActivePhaseIndices[]`（激活阶段）、`OverrideEncounter`（覆盖怪物配置）、`EnvironmentIndex`（环境子物体索引） |
| `Assets/Scripts/Level/DynamicWorld/AmbienceController.cs` | L27: 全局氛围控制器，ServiceLocator 注册。监听 `OnPhaseChanged`，驱动 URP Volume 后处理渐变（Vignette + ColorAdjustments via PrimeTween）、环境粒子启停、BGM crossfade（AudioManager）、低通滤波器开关 |

**修改文件：**

| 路径 | 变更 |
|------|------|
| `Assets/Scripts/Core/LevelEvents.cs` | 新增 `OnTimeChanged(float)`、`OnCycleCompleted(int)`、`OnPhaseChanged(int, string)` 三组事件 + Raise 方法 |
| `Assets/Scripts/Core/Save/SaveData.cs` | `ProgressSaveData` 新增 `WorldClockTime`(float)、`WorldClockCycle`(int)、`CurrentPhaseIndex`(int) 三个字段 |
| `Assets/Scripts/Level/SaveBridge.cs` | `CollectProgressData` 新增 WorldClock + WorldPhaseManager 数据收集；`DistributeProgressData` 新增 WorldClock 时间恢复 + WorldPhaseManager 阶段恢复 |
| `Assets/Scripts/Level/Room/Door.cs` | 新增 `_openDuringPhases[]` 配置字段；新增 `Start()`/`OnDestroy()` 订阅 `OnPhaseChanged`；新增 `EvaluateSchedule()` 在阶段切换时自动在 `Open`/`Locked_Schedule` 间切换 |
| `Assets/Scripts/Level/Room/Room.cs` | 新增 `_variants[]` (RoomVariantSO) + `_variantEnvironments[]` 配置字段；新增 `Start()`/`OnDestroy()` 订阅 `OnPhaseChanged`；新增 `ApplyVariantForPhase()` 按阶段切换遭遇配置和环境子物体；`ActivateEnemies()` 改用 `ActiveEncounter` 属性支持变体覆盖 |
| `Assets/Scripts/Level/ProjectArk.Level.asmdef` | 新增 `Unity.RenderPipelines.Core.Runtime`、`Unity.RenderPipelines.Universal.Runtime` 引用（AmbienceController 需要访问 Volume/Vignette/ColorAdjustments） |

**技术要点：**
- 所有管理器遵循 ServiceLocator 注册模式（Awake 注册、OnDestroy 注销）
- 事件卫生：所有 `OnPhaseChanged`/`OnWorldStageChanged` 订阅均在 `OnDestroy` 中取消
- 异步纪律：AmbienceController 使用 `async UniTaskVoid` + `CancellationTokenSource` + `destroyCancellationToken` 链接
- PrimeTween 模式：后处理渐变使用 `Tween.Custom(..., useUnscaledTime: true, ease: Ease.InOutSine)` 与 DoorTransitionController 一致
- 数据驱动：WorldPhaseSO 支持跨午夜时间范围（`ContainsTime` 方法处理 start > end 的环绕情况）
- 运行时数据隔离：WorldPhaseSO 仅被读取，运行时状态由 WorldPhaseManager 管理
- WorldEventTrigger 持久化：通过 `ProgressSaveData.Flags` 以 key-value 形式存储触发状态，确保保存/加载后不重复触发

**目的：** 完成关卡模块 Phase 6（L20-L27），实现混合时间模型——事件驱动大阶段 + 轻量循环小周期。为"活的世界"体验提供完整系统支撑：定时门/NPC/隐藏通道（ScheduledBehaviour）、Boss击杀→永久世界变化（WorldEventTrigger）、Tilemap结构改变（TilemapVariantSwitcher）、全局氛围渐变（AmbienceController）。

---

### Phase6AssetCreator 一键配置工具 + 验收清单 Phase 6 章节 — 2026-02-15 15:30

**新建文件：**

| 路径 | 用途 |
|------|------|
| `Assets/Scripts/Level/Editor/Phase6AssetCreator.cs` | 三个 MenuItem：`Phase 6: Create World Clock Assets`（创建 4 个 WorldPhaseSO）、`Phase 6: Build Scene Managers`（创建 WorldClock/WorldPhaseManager/AmbienceController 管理器并连线）、`Phase 6: Setup All`（一键执行以上两步） |

**修改文件：**

| 路径 | 变更 |
|------|------|
| `Docs/VerificationChecklist_Phase3-5_ShipFeel.md` | 版本升级到 v2.0。新增 G 分组（Phase 6 共 13 项检查）、第十步（Phase 6 详细配置，含 10A-10H 八个子步骤）、第十一步（Phase 6 Play Mode 验证，含 5 个子表）、常见问题排查追加 10 条 Phase 6 相关条目 |

**技术要点：**
- Phase6AssetCreator 遵循 LevelAssetCreator 的模式：`SerializedObject` + `FindProperty` 设置 SO 字段
- 幂等设计：已存在的资产和组件不会重复创建
- 4 个 WorldPhaseSO 的时间范围按 GDD 定义分配：辐射潮(0~0.208)、平静期(0.208~0.5)、风暴期(0.5~0.75)、寂静时(0.75~1.0)
- 场景管理器创建为 Managers 的子物体，WorldPhaseManager 自动连线 4 个 SO

**目的：** 提供一键配置工具降低 Phase 6 系统的配置门槛；在验收清单中提供完整的分步操作指南，覆盖所有 Phase 6 组件的配置和验证。

---

### RoomBatchEditor 批量房间编辑器 — 2026-02-15 22:57

**新建/修改文件：**

| 路径 | 操作 | 用途 |
|------|------|------|
| `Assets/Scripts/Level/Editor/RoomBatchEditor.cs` | 修改 | 批量房间编辑器工具，提供房间列表管理、批量属性编辑、拓扑可视化、验证检查、JSON导入/导出功能 |

**功能特性：**

1. **房间列表管理**
   - 自动发现场景中所有Room组件（包含inactive）
   - 支持多选房间进行批量操作
   - 提供搜索功能（按房间名称或RoomID过滤）
   - 显示房间总数和选中数量

2. **批量属性编辑**
   - 批量修改RoomSO引用（使用SerializedObject安全修改私有`_data`字段）
   - 批量修改房间类型（Normal/Arena/Boss/Safe）
   - 批量修改楼层级别（FloorLevel）
   - 支持Undo/Redo（使用Undo.RecordObjects）
   - 自动保存AssetDatabase

3. **拓扑可视化**
   - 网格布局显示所有房间节点
   - 按房间类型颜色区分（Normal=蓝，Arena=红，Boss=深红，Safe=绿）
   - 支持鼠标滚轮缩放和平移
   - 选中房间高亮显示（黄色边框）

4. **验证检查**
   - 检查缺失RoomSO引用（Error级别）
   - 检查BoxCollider2D是否为Trigger（Warning级别）
   - 检查CameraConfiner是否在Ignore Raycast层（Warning级别）
   - 支持定位到问题房间

5. **JSON导入/导出**
   - 导出房间数据为JSON（名称、RoomID、类型、楼层、位置、是否有遭遇）
   - 导入JSON数据（仅数据导入，不创建GameObjects）
   - 使用JsonUtility序列化

6. **快捷操作**
   - 在Scene视图中聚焦选中房间
   - 在Hierarchy中选中房间
   - 激活/禁用选中房间

**技术要点：**
- 命名规范：常量使用PascalCase（WindowTitle、MenuPath），私有字段使用_camelCase
- 使用SerializedObject和SerializedProperty安全修改私有[SerializeField]字段
- 正确使用GUI.matrix并通过try/finally恢复状态
- 遵循项目架构原则：数据驱动、事件卫生、运行时数据隔离
- Editor-only代码，使用#if UNITY_EDITOR条件编译
- 遵循项目代码规范：XML文档注释、正确的命名空间（ProjectArk.Level.Editor）

**目的：** 提升关卡编辑效率，允许策划/开发者一次性对多个房间进行批量操作，避免逐个手动修改的繁琐过程。同时提供验证功能帮助发现常见配置错误。

---

## 可视化关卡编辑器（Visual Level Designer）

### 68. LevelElementLibrary — 关卡元素库 — 2026-02-15

**新建文件：**
- `Assets/Scripts/Level/Data/LevelElementLibrary.cs`

**内容：** ScriptableObject 数据容器，定义关卡设计时可使用的所有元素Prefab：
- Room Elements: `_roomTemplate`（房间模板Prefab）
- Wall Elements: `_wallBasic`（基础墙）、`_wallCorner`（墙角）
- Prop Elements: `_crateWooden`（木箱）、`_crateMetal`（金属箱）
- Door Elements: `_doorBasic`（基础门）
- Checkpoint Elements: `_checkpoint`（存档点）
- Spawn Points: `_playerSpawnPoint`（玩家出生点）、`_enemySpawnPoint`（敌人出生点）

**目的：** 统一管理所有可放置的关卡元素Prefab，让LevelDesignerWindow可以动态获取元素库。

**技术：** ScriptableObject, `[CreateAssetMenu]`, 数据驱动模式。

---

### 69. LevelScaffoldData — 关卡脚手架数据 — 2026-02-15

**新建文件：**
- `Assets/Scripts/Level/Data/LevelScaffoldData.cs`

**内容：**
- **LevelScaffoldData**：ScriptableObject，包含：
  - `_levelName`：关卡名称
  - `_floorLevel`：楼层级别
  - `_rooms`：List<ScaffoldRoom> 房间列表
  
- **ScaffoldRoom**：Serializable类，单个房间数据：
  - `_roomID`：唯一ID（Guid生成）
  - `_displayName`：显示名称
  - `_roomType`：房间类型（RoomType枚举）
  - `_position`：世界坐标位置
  - `_size`：房间尺寸（宽×高）
  - `_connections`：List<ScaffoldDoorConnection> 门连接列表
  - `_roomSO`：关联的RoomSO（可选，自动生成）
  
- **ScaffoldDoorConnection**：Serializable类，门连接数据：
  - `_targetRoomID`：目标房间ID
  - `_doorPosition`：门在房间内的位置
  - `_doorDirection`：门朝向（指向目标房间）
  - `_isLayerTransition`：是否为层过渡（更长的淡入淡出）

**目的：** 将关卡设计数据与Unity场景分离，支持保存/加载，为可视化编辑器提供数据结构。

**技术：** ScriptableObject, `[Serializable]` 类, Guid唯一ID生成, 数据驱动设计。

---

### 70. LevelGenerator — 关卡生成器 — 2026-02-15

**新建文件：**
- `Assets/Scripts/Level/Editor/LevelGenerator.cs`

**内容：** 静态工具类，从LevelScaffoldData生成Unity场景对象：
1. **GenerateLevel()**：主入口，接收scaffold、elementLibrary、可选parentTransform
2. **CreateLevelRoot()**：创建关卡根GameObject（`--- {LevelName} ---`格式）
3. **GenerateRooms()**：遍历scaffold.Rooms生成所有Room对象
4. **GenerateSingleRoom()**：生成单个Room对象，设置位置、大小、Collider
5. **SetupRoomCollider()**：配置BoxCollider2D为Trigger，设置size和offset
6. **SetupRoomData()**：配置Room组件的_data引用，自动创建RoomSO（如果为空）
7. **CreateRoomSO()**：创建新的RoomSO资产并保存到Assets/_Data/Level/Rooms/
8. **SetupDoorConnections()**：设置所有房间之间的门连接
9. **CreateDoor()**：创建Door对象，配置目标房间和生成点
10. **CreateSpawnPoint()**：在目标房间创建玩家生成点

**技术要点：**
- 完整的Undo/Redo支持（Undo.SetCurrentGroupName, Undo.RegisterCreatedObjectUndo）
- 使用PrefabUtility.InstantiatePrefab实例化Prefab
- 使用SerializedObject和SerializedProperty安全修改私有字段
- 异常处理 + 自动回滚（Undo.RevertAllDownToGroup）
- 自动创建目录和保存AssetDatabase
- Editor-only代码，放在Editor文件夹

**目的：** 一键将脚手架数据转换为完整的Unity场景，包括Room、Door、Collider、RoomSO等所有必要组件，无需手动配置。

---

### 71. LevelDesignerWindow — 可视化关卡设计器 — 2026-02-15

**新建文件：**
- `Assets/Scripts/Level/Editor/LevelDesignerWindow.cs`

**内容：** EditorWindow，完整的可视化关卡编辑器，包含以下功能模块：

1. **设置面板**
   - Level Scaffold：选择/创建关卡脚手架数据
   - Element Library：选择关卡元素库
   - New Scaffold：一键创建新的LevelScaffoldData资产

2. **房间列表**
   - Add Room：添加新房间
   - 房间展开/折叠：显示/隐藏房间详情
   - 房间编辑：修改名称、类型、位置、大小、RoomSO
   - 连接管理：添加/编辑/删除门连接
   - 删除房间：带确认对话框

3. **拓扑视图（核心功能）**
   - 网格背景：50px网格线
   - 房间节点：彩色矩形表示房间，按RoomType着色
   - 连接线：蓝色线条表示门连接
   - 拖拽移动：鼠标左键拖拽房间移动位置
   - 缩放控制：Zoom滑块（0.25x - 2x）
   - 视图重置：Reset View按钮
   - 标签显示：房间名称标签

4. **Scene视图集成**
   - 房间Gizmo：在Scene视图中绘制房间WireCube
   - 选中高亮：选中的房间显示白色边框和标签
   - 位置同步：拓扑视图和Scene视图位置实时同步

5. **一键生成**
   - Generate to Scene：确认对话框后调用LevelGenerator
   - 完整Undo：所有生成操作可撤销

**技术要点：**
- EditorWindow生命周期（OnEnable/OnDisable）
- SceneView.duringSceneGui事件订阅
- Handles绘制API（DrawSolidRectangleWithOutline, DrawLine, Label）
- GUI事件处理（MouseDown, MouseDrag, MouseUp）
- 世界坐标 ↔ 拓扑坐标转换（WorldToTopology/TopologyToWorld）
- Undo系统集成（Undo.RecordObject）
- EditorUtility.DisplayDialog确认对话框
- EditorUtility.SaveFilePanelInProject文件保存面板

**工作流：**
1. 创建LevelElementLibrary并配置所有元素Prefab
2. 创建LevelScaffoldData
3. 在LevelDesignerWindow中选择Scaffold和Library
4. 点击"Add Room"添加房间，或在拓扑视图中拖拽
5. 配置房间属性（名称、类型、位置、大小）
6. 添加门连接，配置目标房间和位置
7. 点击"Generate to Scene"一键生成到Unity场景
8. 生成的场景包含完整的Room、Door、Collider、RoomSO

**目的：** 提供"搭积木"式的关卡编辑体验，让策划/设计师可以直观地布置房间、连接门，然后一键生成完整的Unity场景，所有组件自动配置完成。

---

## 修复与优化

### 72. RoomBatchEditor 过时API修复 — 2026-02-16 17:30

**修改文件：**
- `Assets/Scripts/Level/Editor/RoomBatchEditor.cs`

**内容：** 修复过时API警告 CS0618：
- 将 `FindObjectsOfType<Room>()` 替换为 `FindObjectsByType<Room>(FindObjectsInactive.Include, FindObjectsSortMode.None)`
- Unity 6.0+ 推荐使用新的 `FindObjectsByType` API 替代已弃用的 `FindObjectsOfType`

**目的：** 消除编译警告，使用Unity推荐的最新API，确保代码的长期可维护性。

**技术：** `Object.FindObjectsByType`, `FindObjectsInactive`, `FindObjectsSortMode`

---

### 73. LevelDesignerWindow GUI布局错误修复 — 2026-02-16 17:32

**修改文件：**
- `Assets/Scripts/Level/Editor/LevelDesignerWindow.cs`

**内容：**
1. **修复CS1061错误**：`GUI.BeginScrollView()` 直接返回 Rect，不需要访问 `.position` 属性
2. **修复CS0117错误**：RoomType 枚举值不匹配，移除了不存在的 `Treasure`, `Rest`, `Entrance`, `Exit`，保留 `Normal`, `Arena`, `Boss`, `Safe`
3. **修复CS0649警告**：`_topologyScrollPosition` 正确赋值

**目的：** 解决编译错误，使LevelDesignerWindow能够正常编译和运行。

---

### 74. LevelDesignerWindow GUI布局优化 — 2026-02-16 17:35

**修改文件：**
- `Assets/Scripts/Level/Editor/LevelDesignerWindow.cs`

**内容：**
1. **修复GetLastRect错误**：移除了在布局组刚开始时调用 `GUILayoutUtility.GetLastRect()` 的代码，改用 `GUIStyle` 的 `normal.background` 属性设置背景颜色
2. **新增MakeTex辅助方法**：创建纯色纹理用于GUI背景样式
3. **房间初始位置优化**：第一个房间从 (0, 0, 0) 开始，后续房间按 3×3 网格自动排列
4. **新增Focus All Rooms按钮**：一键聚焦到所有房间
5. **空房间提示**：当没有房间时显示提示文字
6. **Reset View修复**：正确重置滚动位置

**目的：** 修复Unity GUI布局错误 "You cannot call GetLast immediately after beginning a group"，并优化用户体验，让房间更容易被看到和操作。

**技术：** `GUIStyle`, `Texture2D`, `EditorGUILayout.VerticalScope`, Unity IMGUI布局系统

---

### 75. LevelDesignerWindow 拓扑视图缩放修复 — 2026-02-16 17:40

**修改文件：**
- `Assets/Scripts/Level/Editor/LevelDesignerWindow.cs`

**内容：**
1. **问题分析**：房间尺寸太小（20×15 像素），在拓扑视图中几乎看不见
2. **添加拓扑缩放系数**：引入 `topologyScale = 10f`，将世界坐标单位放大 10 倍以像素显示
3. **更新绘制逻辑**：`roomWidth = Size.x * topologyScale * zoomLevel`
4. **更新拖拽逻辑**：鼠标位置判断和拖拽 delta 计算都使用相同的 `topologyScale`

**目的：** 让房间在拓扑视图中可见且易于操作，修复"按下 Add Room 后看不到房间"的问题。

**技术：** 坐标缩放转换、Unity Handles 绘制、鼠标事件处理

---
