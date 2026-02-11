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
