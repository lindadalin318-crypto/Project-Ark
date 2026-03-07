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

---

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

## 飞船操作手感优化

### 76. 飞船操作手感全面优化 — 2026-02-16 17:45

**修改文件：**
- `Assets/Scripts/Ship/Data/ShipStatsSO.cs`
- `Assets/Scripts/Ship/Movement/ShipMotor.cs`
- `Assets/_Data/Ship/DefaultShipStats.asset`

**内容：**

### 一、参数调整（立即生效）

| 参数 | 原值 | 新值 | 目的 |
|------|------|------|------|
| `MoveSpeed` | 12 | 14 | 整体速度更快，更有爽快感 |
| `Acceleration` | 45 | 70 | 加速更快，响应更直接 |
| `Deceleration` | 25 | 55 | 减速更快，停止更干脆 |
| `SharpTurnAngleThreshold` | 90° | 120° | 放宽转向惩罚触发条件 |
| `SharpTurnSpeedPenalty` | 0.7 | 0.9 | 减少转向速度惩罚 |
| `InitialBoostMultiplier` | 1.5 | 2.2 | 初始爆发更强 |
| `InitialBoostDuration` | 0.05s | 0.12s | 初始爆发持续更长 |

### 二、新增参数（更精细控制）

1. **`DirectionChangeSnappiness` (0.65)**
   - 范围：0.0 ~ 1.0
   - 0 = 非常滑（保持原方向）
   - 1 = 立即转向（无惯性）
   - 0.65 = 平衡值，有惯性但可控

2. **`MinSpeedForDirectionChange` (0.5)**
   - 最低速度阈值，低于此速度时不应用方向混合
   - 防止低速时抖动

### 三、曲线优化

**加速度曲线**：
- 起始点 (0, 1.0) = 初始响应快
- 中点 (0.3, 1.2) = 中期加速快
- 终点 (1.0, 0.6) = 接近最高速时平缓

**减速曲线**：
- 起始点 (0, 1.3) = 松开摇杆立即快速减速
- 中点 (0.4, 1.0) = 中期减速平稳
- 终点 (1.0, 0.7) = 低速时自然停止

### 四、代码改进

在 `ShipMotor.HandleMovement()` 中新增：
- 方向变化平滑混合（Lerp）
- 基于 `DirectionChangeSnappiness` 的可控转向

---

## LevelDesigner Door 配置自动化

### 77. LevelScaffoldData Door 配置扩展 — 2026-02-16 18:00

**修改文件：**
- `Assets/Scripts/Level/Data/LevelScaffoldData.cs`

**内容：**
1. **新增 `ScaffoldDoorElementConfig` 类**：专门存储 Door 元素的配置
   - `InitialState`：门的初始状态（Open / Locked_Combat / Locked_Key 等）
   - `RequiredKeyID`：需要的钥匙 ID
   - `OpenDuringPhases`：开门阶段（Locked_Schedule 用）
2. **扩展 `ScaffoldElement` 类**：
   - 新增 `DoorConfig` 字段，仅在 Door 类型时使用
   - 新增 `BoundConnectionID` 字段，绑定到 ScaffoldDoorConnection
   - 新增 `EnsureDoorConfigExists()` 方法，确保配置对象存在
3. **扩展 `ScaffoldDoorConnection` 类**：
   - 新增 `ConnectionID` 字段（GUID），用于 Door 元素绑定
4. **所有新增字段都有 `[Tooltip]` 和 `[SerializeField]`，符合 C# 编码规范**

**目的：** 实现 Door 元素与 Room Connection 的数据绑定，为后续一键放置 Door 功能做准备。

---

### 78. LevelDesignerWindow 一键放置 Door — 2026-02-16 18:05

**修改文件：**
- `Assets/Scripts/Level/Editor/LevelDesignerWindow.cs`

**内容：**
1. **Connection 编辑器新增 Place Door 按钮**：
   - 当 Connection 已配置 TargetRoom 时，按钮可用
   - 点击后自动创建 Door 元素，绑定到该 Connection
   - Door 的位置自动设为 Connection 的 DoorPosition
   - Door 的旋转自动从 Connection 的 DoorDirection 计算
   - 自动初始化 DoorConfig
2. **Connection 编辑器新增 Remove Door 按钮**：
   - 当 Door 已放置时显示 ✓ 提示
   - 点击可以移除绑定的 Door 元素
3. **Door Position 同步功能**：
   - 当修改 Connection 的 DoorPosition 时，自动同步到绑定的 Door 元素
4. **Room Elements 模式 Door 配置面板**：
   - 选中 Door 元素后，显示 Door Configuration 区域
   - 可编辑 Initial State、Required Key ID、Open During Phases
5. **新增辅助方法**：
   - `FindBoundDoor()`：查找绑定到 Connection 的 Door 元素
   - `PlaceDoorForConnection()`：一键放置 Door
   - `SyncDoorPosition()`：同步 Door 位置

**目的：** 让 Door 配置变得超级简单，只需在 Connection 面板点一下按钮，所有 Door 配置（包括 TargetRoom、SpawnPoint、InitialState 等）都会自动生成。

**技术：** Unity IMGUI、Undo 系统、Scaffold 数据绑定、自动旋转计算（Vector2.SignedAngle）

---

### 79. LevelGenerator Door 自动配置 — 2026-02-16 18:10

**修改文件：**
- `Assets/Scripts/Level/Editor/LevelGenerator.cs`

**内容：**
1. **移除旧的 SetupDoorConnections 流程**：不再单独生成 Door，改为由元素系统统一处理
2. **更新 GenerateSingleElement 方法**：
   - 新增 `scaffold` 和 `roomMap` 参数
   - 当元素是 Door 且绑定了 Connection 时，自动调用 SetupDoorFromBinding
3. **新增 SetupDoorFromBinding 方法**：
   - 查找绑定的 ScaffoldDoorConnection
   - 自动配置 Door 组件的所有字段：
     - `_targetRoom`：从 Connection 的 TargetRoomID 查找
     - `_targetSpawnPoint`：调用 CreateSpawnPoint 自动生成
     - `_isLayerTransition`：继承自 Connection
     - `_initialState`：从 DoorConfig 读取
     - `_requiredKeyID`：从 DoorConfig 读取
     - `_openDuringPhases`：从 DoorConfig 读取
4. **新增 GetRoomID 辅助方法**：根据 Room 组件反向查找 RoomID
5. **更新 GenerateRoomElements 调用**：传入正确的参数

**目的：** 实现 Door 生成时的全自动配置，不需要任何手动操作，所有属性从 Scaffold 数据自动读取并赋值。

**技术：** Unity SerializedObject 访问私有字段、Undo 系统、Room 组件与 Scaffold 数据映射

---

### 80. RoomSO 下拉栏选择 — 2026-02-16 18:15

**修改文件：**
- `Assets/Scripts/Level/Editor/LevelDesignerWindow.cs`

**内容：**
1. **新增 `DrawRoomSOPopup` 方法**：
   - 使用 `AssetDatabase.FindAssets("t:RoomSO")` 自动检索所有 RoomSO 资源
   - 加载所有找到的 RoomSO 并收集它们的名字
   - 生成下拉选项列表，第一个选项是 `(None)`
2. **替换 `ObjectField` 为 `EditorGUILayout.Popup`**：
   - 从原来的拖拽赋值改为点击下拉选择
   - 选择后自动更新 ScaffoldRoom.RoomSO
   - 支持 Undo/Redo
3. **保持原有功能**：`(None)` 选项对应 null 值，与旧行为一致

**目的：** 让 RoomSO 选择更直观、更快捷，不需要再去 Project 窗口找文件拖过来。

**技术：** `AssetDatabase.FindAssets`、`AssetDatabase.LoadAssetAtPath`、`EditorGUILayout.Popup`、Undo 系统

---

### 81. 房间自动吸附对齐功能 — 2026-02-16 18:20

**修改文件：**
- `Assets/Scripts/Level/Editor/LevelDesignerWindow.cs`

**内容：**
1. **新增 `SnapToOtherRooms` 方法**：
   - `snapThreshold = 1f`：吸附阈值 1 个单位
   - 计算每个房间的四个边缘（左、右、上、下）
   - 当拖拽房间的边缘靠近其他房间的边缘时（小于阈值），自动吸附
   - 支持四个方向的吸附：右对左、左对右、下对上、上对下
2. **修改拖拽逻辑**：
   - 在 `MouseDrag` 事件中，先计算原始拖拽位置
   - 调用 `SnapToOtherRooms` 获得吸附后的位置
   - 将房间位置设为吸附后的位置
3. **保持流畅体验**：
   - 只在鼠标拖拽时应用吸附
   - 吸附后立即生效，不需要松开鼠标
   - Undo/Redo 正常工作

**目的：** 让房间对齐变得超级简单，不需要手动微调位置，靠近边缘会自动吸过去，方便建造房间网。

**技术：** 边缘碰撞检测、距离阈值判断、坐标自动对齐

---

### 82. Element 形状配置与 Q/W/E/R 变换工具 — 2026-02-16 18:30

**修改文件：**
- `Assets/Scripts/Level/Data/LevelScaffoldData.cs`
- `Assets/Scripts/Level/Editor/LevelDesignerWindow.cs`

**内容：**

#### 一、LevelScaffoldData.cs 修改
1. **新增 `ElementGizmoShape` 枚举**：
   - `Square`：正方形
   - `Circle`：圆形
   - `Diamond`：菱形
2. **扩展 `ScaffoldElement` 类**：
   - 新增 `_gizmoShape` 字段，默认值 `Square`
   - 新增 `GizmoShape` 公共属性
   - 新增 `SetDefaultGizmoShapeForType()` 方法，根据元素类型设置默认形状
     - Wall/WallCorner/CrateWooden/CrateMetal/Door → Square
     - Checkpoint/PlayerSpawn/EnemySpawn/Hazard → Circle
3. **修改 `ElementType` setter**：设置类型时自动调用 `SetDefaultGizmoShapeForType()`

#### 二、LevelDesignerWindow.cs 修改
1. **新增 `ElementTransformTool` 枚举**：
   - `Select`：选择模式
   - `Move`：移动模式
   - `Rotate`：旋转模式
   - `Scale`：缩放模式
2. **新增工具条 UI**：
   - 在 Room Elements 面板顶部添加 Q/W/E/R 四个按钮的 Toolbar
3. **新增快捷键支持**：
   - `Q` → Select
   - `W` → Move
   - `E` → Rotate
   - `R` → Scale
4. **修改 `DrawRoomCanvasElements` 方法**：
   - 根据 `GizmoShape` 绘制不同形状
   - Square：`Handles.DrawSolidRectangleWithOutline`
   - Circle：`Handles.DrawSolidDisc`
   - Diamond：`Handles.DrawSolidPolygon` 绘制四边形
5. **新增 `DrawElementTransformGizmo` 方法**：
   - Move 模式：绘制红/绿方向箭头
   - Rotate 模式：绘制圆环
   - Scale 模式：绘制红/绿方向手柄
6. **修改 `HandleRoomCanvasInput` 方法**：
   - 支持快捷键切换工具
   - 拖拽时根据当前工具执行不同操作
   - Move：直接修改 LocalPosition
   - Rotate：通过鼠标与元素中心的夹角计算旋转
   - Scale：通过鼠标位移计算缩放，最小 0.1 防止负数
7. **Element Properties 面板新增 Gizmo Shape 下拉栏**：
   - 可以随时手动切换元素的显示形状
8. **修改 `AddElementToRoom` 方法**：
   - 新增元素时调用 `SetDefaultGizmoShapeForType()`

**目的：**
- 让不同类型的元素默认用合适的形状显示（Wall 是方的，Spawn 是圆的）
- 提供和 Unity Editor 一样的变换工具体验（Q/W/E/R 快捷键）
- 可以直观地拖拽移动、旋转、缩放元素

**技术：** Unity IMGUI、Handles 绘制、Keyboard Input、Undo 系统、向量夹角计算（Vector2.SignedAngle）

---

### 83. 房间吸附功能优化 — 2026-02-16 18:45

**修改文件：**
- `Assets/Scripts/Level/Editor/LevelDesignerWindow.cs`

**内容：**
1. **增大默认吸附阈值**：从 1 个单位 → 3 个单位
2. **添加可配置参数**：新增 `_roomSnapThreshold` 字段，默认值 3f
3. **UI 面板新增设置**：在 Settings 面板添加 "Room Snap Settings" 部分
   - 滑块：可在 0.5f 到 10f 之间调整阈值
   - HelpBox：说明 "Higher = easier to snap"
4. **修改吸附逻辑**：`SnapToOtherRooms` 方法使用 `_roomSnapThreshold` 替代硬编码值

**目的：**
- 原来 1 个单位的阈值太小，在 Topology View 中只有 10 像素，很难感受到
- 现在默认 3 个单位（30 像素），吸附更明显
- 用户可以根据自己的喜好调整阈值

**技术：** Unity IMGUI、EditorGUILayout.Slider、Mathf.Abs 距离检测

---

### 73. Level Architect Tool — 完整实现（Task 1-10）— 2026-02-16 10:56

**新建文件：**

| 文件路径 | 操作 | 简述 |
|---|---|---|
| `Assets/Scripts/Level/Editor/LevelArchitect/LevelArchitectWindow.cs` | 新建 | 主入口EditorWindow，管理工具模式(Select/Blockout/Connect)、SceneView工具栏、侧边面板、各子系统集成 |
| `Assets/Scripts/Level/Editor/LevelArchitect/RoomBlockoutRenderer.cs` | 新建 | 房间白膜渲染（颜色编码矩形）、鼠标交互（选择/拖拽/吸附/框选）、门图标和连接线、悬停信息浮窗 |
| `Assets/Scripts/Level/Data/RoomPresetSO.cs` | 新建 | ScriptableObject房间预设模板（名称/尺寸/房间类型/SpawnPoint数量/ArenaController配置） |
| `Assets/Scripts/Level/Editor/LevelArchitect/RoomFactory.cs` | 新建 | 从预设创建完整Room GameObject、自动创建子对象（Confiner/SpawnPoints/Spawner）、自动生成RoomSO资产 |
| `Assets/Scripts/Level/Editor/LevelArchitect/DoorWiringService.cs` | 新建 | 自动检测房间共享边缘、创建双向门连接对、SceneView连接模式交互、门位置更新 |
| `Assets/Scripts/Level/Editor/LevelArchitect/ScaffoldSceneBinder.cs` | 新建 | 维护ScaffoldRoom到场景Room的映射、双向同步Scene↔Scaffold变化 |
| `Assets/Scripts/Level/Editor/LevelArchitect/BlockoutModeHandler.cs` | 新建 | 矩形和走廊笔刷工具、拖拽绘制预览、链式绘制(Shift)、Quick Play、内置预设创建 |
| `Assets/Scripts/Level/Editor/LevelArchitect/LevelValidator.cs` | 新建 | 8项验证规则、自动修复、轻量级SceneView叠加检查、Error/Warning/Info分级 |
| `Assets/Scripts/Level/Editor/LevelArchitect/BatchEditPanel.cs` | 新建 | 多选房间批量属性编辑、右键上下文菜单、Copy/Paste配置、批量RoomType/FloorLevel/Size调整 |
| `Assets/Scripts/Level/Editor/LevelArchitect/PacingOverlayRenderer.cs` | 新建 | Pacing Overlay（战斗强度色阶）、Critical Path（BFS最短路径）、Lock-Key Graph（锁钥依赖） |
| `Assets/Scripts/Level/Editor/LevelArchitect/SceneScanner.cs` | 新建 | 反向扫描场景Room/Door构建LevelScaffoldData、自动创建缺失RoomSO |

**修改文件：**

| 文件路径 | 操作 | 简述 |
|---|---|---|
| `Assets/Scripts/Level/Editor/RoomBatchEditor.cs` | 修改 | 添加[Obsolete]标记 |
| `Assets/Scripts/Level/Editor/LevelDesignerWindow.cs` | 修改 | 添加[Obsolete]标记 |
| `Assets/Scripts/Level/Editor/ShebaLevelScaffolder.cs` | 修改 | 添加[Obsolete]标记 |

**内容简述：**
全新的统一关卡编辑工具，替代原有3个分散工具（RoomBatchEditor/LevelDesignerWindow/ShebaLevelScaffolder）。10个模块：
1. **基础框架** — EditorWindow + SceneView.duringSceneGui集成，模式切换工具栏
2. **白膜渲染** — 颜色编码房间矩形、选择/拖拽/吸附/框选交互
3. **预设系统** — RoomPresetSO + RoomFactory一键创建标准化房间
4. **智能门连接** — 共享边检测、双向Door自动创建、SceneView拖线连接
5. **双向同步** — ScaffoldData ↔ Scene Room实时同步
6. **白膜搭建** — 矩形/走廊笔刷、链式绘制、Quick Play
7. **验证修复** — 8项规则检查、Auto-Fix、SceneView红色警告叠加
8. **批量编辑** — 多选属性修改、右键菜单、Copy/Paste配置
9. **节奏可视化** — 战斗强度色阶、BFS关键路径、锁钥依赖图
10. **场景扫描** — 反向导入已有场景、旧工具[Obsolete]标记

**目的：** 提供Scene-View-First的关卡设计体验，让策划能在几分钟内完成关卡白膜搭建、自动配置门连接、一键验证修复错误、直观查看关卡节奏，大幅提升关卡制作效率。

**技术：** EditorWindow/SceneView集成、Handles绘制系统、SerializedObject批量编辑、BFS图搜索、ScriptableObject数据驱动、Undo撤销支持、AssetDatabase资产管理。

---

## 星图系统设计文档 — 2026-02-16

**新建文件：**
- `Docs/StarChartDesign.md`

**内容：**
完整的星图系统设计文档，包含：
- 系统概述与核心概念（星核、棱镜、光帆、伴星、轨道）
- 轨道布局设计（16:9横版适配、主副轨道背包式布局）
- 拖拽装备系统
- 物品说明浮窗（鼠标hover显示，无卸载按钮）
- 主副轨道自动选中（鼠标靠近自动激活）
- 武器轨道组合切换（上下按钮，加特林旋转视觉效果）
- 未来扩展预留（格子数随游戏进度增加）

**目的：** 将星图系统的完整设计需求整理成可执行的文档，为后续实现提供明确的指导。

**技术：** Markdown文档、需求规格化、交互设计。

---

## 太空生活系统设计文档 — 2026-02-16

**新建文件：**
- `Docs/SpaceLifeSystemDesign.md`

**内容：**
完整的太空生活系统设计文档，包含：
- 系统概述（双视角切换：战斗视角 ↔ 飞船内视角）
- 视角切换机制（Tab键切换，C键打开星图）
- 飞船内空间布局（主空间网状连接，6个船员房间）
- 房间类型（驾驶室、星图室、船员休息室、医疗室、厨房、储物室）
- 2D横版角色移动（WASD移动，空格键跳跃）
- NPC互动系统（对话、送礼、关系值）
- 编辑器工具（一键配置向导）

**目的：** 将太空生活系统的完整设计需求整理成可执行的文档，为后续实现提供明确的指导。

**技术：** Markdown文档、需求规格化、游戏设计。

---

## 太空生活系统完整实现 — 2026-02-16

**新建文件（23个）：**

**Runtime (16个)：**
1. `Assets/Scripts/SpaceLife/SpaceLifeManager.cs` — 单例管理器，处理模式切换、玩家生成、相机切换
2. `Assets/Scripts/SpaceLife/PlayerController2D.cs` — 2D角色移动控制器（Rigidbody2D，跳跃、地面检测、动画）
3. `Assets/Scripts/SpaceLife/PlayerInteraction.cs` — 玩家互动组件（查找最近可互动、E键互动）
4. `Assets/Scripts/SpaceLife/SpaceLifeInputHandler.cs` — 太空生活输入处理器
5. `Assets/Scripts/SpaceLife/Room.cs` — 房间组件（玩家检测、房间状态）
6. `Assets/Scripts/SpaceLife/RoomManager.cs` — 房间管理器（查找所有房间）
7. `Assets/Scripts/SpaceLife/Door.cs` — 门组件（传送玩家、钥匙验证）
8. `Assets/Scripts/SpaceLife/Interactable.cs` — 可互动对象组件（互动提示、范围检测）
9. `Assets/Scripts/SpaceLife/NPCController.cs` — NPC控制器
10. `Assets/Scripts/SpaceLife/RelationshipManager.cs` — 关系管理器（单例，关系值存储与事件）
11. `Assets/Scripts/SpaceLife/DialogueUI.cs` — 对话UI（打字机效果、选项选择）
12. `Assets/Scripts/SpaceLife/NPCInteractionUI.cs` — NPC综合互动UI
13. `Assets/Scripts/SpaceLife/GiftInventory.cs` — 礼物库存
14. `Assets/Scripts/SpaceLife/GiftUI.cs` — 送礼UI
15. `Assets/Scripts/SpaceLife/MinimapUI.cs` — 小地图UI
16. `Assets/Scripts/SpaceLife/SpaceLifeQuickSetup.cs` — 快速设置脚本

**Data (3个)：**
17. `Assets/Scripts/SpaceLife/Data/NPCDataSO.cs` — NPC数据ScriptableObject
18. `Assets/Scripts/SpaceLife/Data/ItemSO.cs` — 物品数据ScriptableObject
19. `Assets/Scripts/SpaceLife/Data/DialogueData.cs` — 对话数据结构

**Editor (4个)：**
20. `Assets/Scripts/SpaceLife/Editor/SpaceLifeSetupWindow.cs` — 设置向导窗口（分Phase 1-5，一键配置）
21. `Assets/Scripts/SpaceLife/Editor/SpaceLifeMenuItems.cs` — 快捷菜单工具
22. `Assets/Scripts/SpaceLife/Editor/ProjectArk.SpaceLife.Editor.asmdef` — Editor程序集定义
23. `Assets/Scripts/SpaceLife/ProjectArk.SpaceLife.asmdef` — Runtime程序集定义

**内容简述：**
完整的太空生活系统实现，包括：
1. **核心管理器** — SpaceLifeManager单例，处理模式切换、玩家生成、相机切换
2. **2D角色移动** — Rigidbody2D物理移动、跳跃、地面检测、动画控制
3. **房间系统** — Room/RoomManager/Door组件
4. **NPC系统** — NPCController/NPCDataSO/RelationshipManager
5. **对话系统** — DialogueUI带打字机效果
6. **送礼系统** — GiftInventory/GiftUI
7. **编辑器工具** — SpaceLifeSetupWindow分阶段向导、SpaceLifeMenuItems快捷菜单

**目的：** 完整实现太空生活系统，提供双视角切换、2D角色移动、NPC互动等核心功能。

**技术：** Unity MonoBehaviour、ScriptableObject、单例模式、事件驱动、Rigidbody2D物理、PrimeTween动画、UniTask异步。

---

## Unity 废弃 API 更新：FindObjectOfType → FindFirstObjectByType — 2026-02-16

### 概述
将 Space Life 系统中所有废弃的 Unity API 调用更新到现代替代方案，消除编译警告并确保与 Unity 最新版本的兼容性。

### 修改文件

| 文件路径 | 变更说明 |
|---------|---------|
| `Assets/Scripts/SpaceLife/Room.cs` | `FindObjectOfType&lt;PlayerController2D&gt;()` → `FindFirstObjectByType&lt;PlayerController2D&gt;()` |
| `Assets/Scripts/SpaceLife/PlayerInteraction.cs` | `FindObjectsOfType&lt;Interactable&gt;()` → `FindObjectsByType&lt;Interactable&gt;(FindObjectsSortMode.None)` |
| `Assets/Scripts/SpaceLife/Door.cs` | `FindObjectOfType&lt;PlayerController2D&gt;()` → `FindFirstObjectByType&lt;PlayerController2D&gt;()` |
| `Assets/Scripts/SpaceLife/RoomManager.cs` | `FindObjectsOfType&lt;Room&gt;()` → `FindObjectsByType&lt;Room&gt;(FindObjectsSortMode.None)` |
| `Assets/Scripts/SpaceLife/Interactable.cs` | `FindObjectOfType&lt;PlayerController2D&gt;()` → `FindFirstObjectByType&lt;PlayerController2D&gt;()` |
| `Assets/Scripts/SpaceLife/Editor/SpaceLifeSetupWindow.cs` | 更新 14 处 `FindObjectOfType` 调用为 `Object.FindFirstObjectByType`；修复 `SetupPhase` 枚举访问修饰符 (internal → public)；修复隐式数组类型错误 (显式声明 `UnityEngine.MonoBehaviour[]`) |
| `Assets/Scripts/SpaceLife/Editor/SpaceLifeMenuItems.cs` | 更新 `FindObjectOfType` 调用为 `Object.FindFirstObjectByType`；添加缺失的 `using ProjectArk.SpaceLife.Data;` 命名空间引用 |

### 目的
消除所有 CS0618 废弃 API 警告，确保项目与 Unity 6 及未来版本的兼容性。同时修复了在更新过程中暴露的编译错误（命名空间缺失、访问修饰符不一致、隐式类型数组）。

### 技术
- 单对象查找：`Object.FindObjectOfType&lt;T&gt;()` → `Object.FindFirstObjectByType&lt;T&gt;()`
- 多对象查找（无序）：`Object.FindObjectsOfType&lt;T&gt;()` → `Object.FindObjectsByType&lt;T&gt;(FindObjectsSortMode.None)`
- 现代 Unity API 迁移，向后兼容保持行为一致性

---

## 太空生活系统实现检查清单 — 2026-02-16

**新建文件：**
- `Docs/SpaceLifeImplementationChecklist.md`

**内容：**
完整的太空生活系统实现检查报告，包含：
- 整体进度概览（总体完成度约85%）
- 详细实现检查（核心管理器、2D角色移动、房间系统、NPC系统、对话系统、关系系统、送礼系统、双视角切换、输入处理、编辑器工具）
- Unity API更新状态
- 待完成功能清单
- 文件清单（23个已创建文件）
- 总结与下一步建议

**目的：**
系统地检查太空生活系统的实现状态，对比设计文档，列出所有已实现和缺失的功能，为后续开发提供清晰的指导。

**技术：**
Markdown文档、系统检查清单、状态报告。

---

## SpaceLife 模块 New Input System 统一迁移 — 2026-02-16

### 新建/修改文件：
| 文件路径 | 变更说明 |
|---------|---------|
| `Assets/Input/ShipActions.inputactions` | 新增 ToggleSpaceLife 和 SpaceLifeJump 两个 Action，添加相应键盘/手柄绑定 |
| `Assets/Scripts/SpaceLife/ProjectArk.SpaceLife.asmdef` | 添加 Unity.InputSystem 程序集引用，添加 rootNamespace |
| `Assets/Scripts/SpaceLife/SpaceLifeInputHandler.cs` | 重构为使用 New Input System，移除旧 Input 类 |
| `Assets/Scripts/SpaceLife/PlayerInteraction.cs` | 重构为使用 New Input System，移除旧 Input 类 |
| `Assets/Scripts/SpaceLife/PlayerController2D.cs` | 重构为使用 New Input System，移除旧 Input 类 |

### 内容：
将 SpaceLife 模块的所有输入处理统一迁移到 New Input System，与项目其他模块保持一致：
1. 在 ShipActions.inputactions 中新增 ToggleSpaceLife Action（绑定 Tab 键）
2. 在 ShipActions.inputactions 中新增 SpaceLifeJump Action（绑定 W/↑/Space/Gamepad 按钮）
3. 复用 Ship map 中已有的 Move Action 和 Interact Action
4. 重构 SpaceLifeInputHandler 使用 ToggleSpaceLife Action
5. 重构 PlayerInteraction 使用 Interact Action
6. 重构 PlayerController2D 使用 Move Action 和 SpaceLifeJump Action
7. 更新 ProjectArk.SpaceLife.asmdef 添加 Unity.InputSystem 引用

### 目的：
确保 SpaceLife 模块与项目整体架构一致，使用 CLAUDE.md 中明确要求的 New Input System 技术栈，避免新旧输入系统混用导致的潜在问题。

### 技术：
Unity New Input System、InputActionAsset、InputAction、事件驱动输入处理。

---

## SpaceLife 模块完整重构 — 2026-02-17 11:10

### 新建文件：
| 文件路径 | 变更说明 |
|---------|---------|
| `Assets/Scripts/SpaceLife/TransitionUI.cs` | 过渡动画系统（打字机效果 + 淡入淡出） |

### 修改文件：
| 文件路径 | 变更说明 |
|---------|---------|
| `Assets/Scripts/SpaceLife/SpaceLifeManager.cs` | ServiceLocator + PoolManager + AudioManager + TransitionUI + 异步过渡 |
| `Assets/Scripts/SpaceLife/PlayerController2D.cs` | Top-Down 移动 + 加速度/减速度曲线 |
| `Assets/Scripts/SpaceLife/DialogueUI.cs` | UniTask 打字机效果 + CancellationToken |
| `Assets/Scripts/SpaceLife/RoomManager.cs` | UniTask 相机移动 + CancellationToken |
| `Assets/Scripts/SpaceLife/MinimapUI.cs` | 房间导航按钮动态生成 |
| `Assets/Scripts/SpaceLife/Room.cs` | 新增 Doors 列表属性 |
| `Assets/Scripts/SpaceLife/Door.cs` | 新增 ConnectedRoom 属性 |
| `Assets/Scripts/SpaceLife/RelationshipManager.cs` | Singleton → ServiceLocator |
| `Assets/Scripts/SpaceLife/GiftInventory.cs` | Singleton → ServiceLocator |
| `Assets/Scripts/SpaceLife/GiftUI.cs` | Singleton → ServiceLocator |
| `Assets/Scripts/SpaceLife/NPCInteractionUI.cs` | Singleton → ServiceLocator |
| `Assets/Scripts/SpaceLife/NPCController.cs` | ServiceLocator 引用更新 |
| `Assets/Scripts/SpaceLife/Interactable.cs` | 缓存引用替代 Find |
| `Assets/Scripts/SpaceLife/SpaceLifeInputHandler.cs` | ServiceLocator |
| `Assets/Scripts/SpaceLife/ProjectArk.SpaceLife.asmdef` | 添加 UniTask + ProjectArk.Core.Audio 引用 |
| `Assets/Scripts/SpaceLife/Editor/SpaceLifeSetupWindow.cs` | 完全重写，自动创建 Prefab 和场景结构 |
| `ProjectArk.SpaceLife.csproj` | 添加 ProjectArk.Core.Audio 项目引用 |

### 内容：

#### Phase 1: 架构合规重构
- 迁移所有 Singleton 到 ServiceLocator 模式
- 移除所有 FindAnyObjectByType/FindFirstObjectByType 调用
- 使用缓存引用或 ServiceLocator.Get 替代

#### Phase 2: PlayerController2D 优化
- 改为 Top-Down 2D 移动（gravityScale = 0）
- 添加加速度/减速度曲线，提升手感
- 移除跳跃功能（根据用户反馈）

#### Phase 3: Coroutine → UniTask 迁移
- DialogueUI 打字机效果改用 UniTask
- RoomManager 相机移动改用 UniTask
- 添加 CancellationToken 支持取消操作

#### Phase 4: 集成核心系统
- PoolManager: 玩家 Spawn 使用对象池
- AudioManager: 添加进入/退出音效支持

#### Phase 5: 过渡动画和小地图
- TransitionUI: 打字机效果 + 淡入淡出
- MinimapUI: 动态生成房间导航按钮

#### Bug 修复
1. **看不到 PlayerCharacter**: 一键生成菜单现在自动创建 Player2D_Prefab 并赋值
2. **Tab 无法退出**: SpaceLifeInputHandler 独立于 _shipRoot，进入时启用、退出时禁用

### 目的：
1. 统一架构风格，与项目整体 ServiceLocator 模式保持一致
2. 提升性能，移除 O(n) 的 Find 调用
3. 现代化异步处理，使用 UniTask 替代 Coroutine
4. 集成项目核心系统（PoolManager、AudioManager）
5. 修复用户反馈的两个关键 Bug

### 技术：
- ServiceLocator 依赖注入模式
- UniTask 异步编程 + CancellationToken
- GameObjectPool 对象池
- Top-Down 2D 移动物理
- Unity Editor 脚本（PrefabUtility、SerializedObject）
- 程序集定义（asmdef）引用管理

---

## SpaceLife Setup 窗口增强 — 2026-02-17 11:23

### 修改文件：
| 文件路径 | 变更说明 |
|---------|---------|
| `Assets/Scripts/SpaceLife/Editor/SpaceLifeSetupWindow.cs` | 智能检测 + 补齐引用 + Player 可见性修复 |

### 内容：

#### 问题修复
1. **Player 不可见**：Player Prefab 添加 `SpriteRenderer` 组件（使用 Unity 内置 Knob sprite 作为占位符）
2. **重复创建检测**：已存在对象时不再跳过，而是检查并补齐缺失引用

#### 新增功能
- `EnsureManagerReferences()` 方法：统一检查并补齐 SpaceLifeManager 的所有引用
  - `_spaceLifePlayerPrefab`
  - `_spaceLifeSceneRoot`
  - `_spaceLifeSpawnPoint`
  - `_spaceLifeCamera`
  - `_spaceLifeInputHandler`

#### 行为改进
- 已存在 SpaceLifeManager → 检查引用并补齐
- 已存在 Player Prefab → 自动赋值给 Manager
- 已存在 SpaceLifeScene → 检查子对象并补齐缺失的 SpawnPoint/Camera
- 已存在 SpaceLifeInputHandler → 自动赋值给 Manager

### 目的：
1. 修复 Player 在 SpaceLife 模式下不可见的问题
2. 支持增量 Setup，避免重复创建对象
3. 自动补齐缺失的引用，减少手动配置

### 技术：
- SpriteRenderer 组件、Unity 内置资源
- SerializedObject 属性检查与赋值
- Editor 脚本增量检测逻辑

---

## SpaceLife 类重命名避免冲突 — 2026-02-17 11:35

### 新建文件：
| 文件路径 | 变更说明 |
|---------|---------|
| `Assets/Scripts/SpaceLife/SpaceLifeRoom.cs` | 原 Room.cs 重命名 |
| `Assets/Scripts/SpaceLife/SpaceLifeRoomManager.cs` | 原 RoomManager.cs 重命名 |
| `Assets/Scripts/SpaceLife/SpaceLifeDoor.cs` | 原 Door.cs 重命名 |

### 删除文件：
| 文件路径 | 变更说明 |
|---------|---------|
| `Assets/Scripts/SpaceLife/Room.cs` | 重命名为 SpaceLifeRoom.cs |
| `Assets/Scripts/SpaceLife/RoomManager.cs` | 重命名为 SpaceLifeRoomManager.cs |
| `Assets/Scripts/SpaceLife/Door.cs` | 重命名为 SpaceLifeDoor.cs |

### 修改文件：
| 文件路径 | 变更说明 |
|---------|---------|
| `Assets/Scripts/SpaceLife/MinimapUI.cs` | 更新引用 SpaceLifeRoom/SpaceLifeRoomManager |
| `Assets/Scripts/SpaceLife/SpaceLifeQuickSetup.cs` | 更新引用 SpaceLifeRoomManager |
| `Assets/Scripts/SpaceLife/Editor/SpaceLifeMenuItems.cs` | 更新菜单创建逻辑 |
| `Assets/Scripts/SpaceLife/Editor/SpaceLifeSetupWindow.cs` | 更新 Setup 窗口创建逻辑 |
| `ProjectArk.SpaceLife.csproj` | 更新文件引用 |

### 内容：
将 SpaceLife 模块中的房间相关类重命名，添加 `SpaceLife` 前缀，避免与 `Level/Room` 模块中的同名类冲突：

| 原类名 | 新类名 | 说明 |
|--------|--------|------|
| `Room` | `SpaceLifeRoom` | 太空生活房间 |
| `RoomManager` | `SpaceLifeRoomManager` | 太空生活房间管理器 |
| `RoomType` | `SpaceLifeRoomType` | 太空生活房间类型枚举 |
| `Door` | `SpaceLifeDoor` | 太空生活门 |

### 目的：
1. 解决 `Level/Room/RoomManager.cs` 与 `SpaceLife/RoomManager.cs` 的命名冲突
2. 保持两个模块独立，避免 ServiceLocator 注册冲突
3. 清晰区分不同模块的职责

### 技术：
- 类重命名、枚举重命名
- 程序集内引用更新
- Unity Editor 菜单更新

---

## 删除 One-Click Quick Setup 并合并功能 — 2026-02-17 11:40

### 删除功能：
- 移除 `ProjectArk/Space Life/🚀 One-Click Quick Setup (Visible!)` 菜单项
- 删除相关方法：`CreateSpaceLifeSystem`, `CreateManagers`, `CreatePlayerPrefab`, `CreateSceneContent`, `CreateNPC`, `CreateUI`, `ConnectEverything`

### 合并到 Setup Wizard：
| 功能 | 实现位置 |
|------|----------|
| 可见 Player (青色方块) | `CreatePlayerPrefab()` 使用 `CreateSquareSprite(Color.cyan)` |
| 可见 NPC (彩色方块) | `CreateDemoNPCs()` 为每个 NPC 添加 SpriteRenderer |
| 可见 Background | 新增 `CreateBackground()` 方法 |

### 修改文件：
| 文件路径 | 变更说明 |
|---------|---------|
| `Assets/Scripts/SpaceLife/Editor/SpaceLifeMenuItems.cs` | 删除 One-Click Setup，保留工具方法 |
| `Assets/Scripts/SpaceLife/Editor/SpaceLifeSetupWindow.cs` | 添加 Background 创建，更新 Player/NPC 可视化 |

### 目的：
1. 统一 Setup 入口，避免用户困惑
2. Setup Wizard 具备智能检测和补齐引用功能
3. 所有创建的对象都可见，便于调试

### 技术：
- `CreateSquareSprite(Color)` 生成纯色 Sprite
- SpriteRenderer.drawMode = Tiled 实现可平铺背景
- sortingOrder 控制渲染层级

---

## Setup Wizard 完整性修复 — 2026-02-17 11:45

### 问题修复：

| 问题 | 解决方案 |
|------|----------|
| SpaceLifeInputHandler 缺少 InputActionAsset | 新增 `EnsureInputHandlerReferences()` 自动查找并分配 |
| Player Prefab 缺少 InputActionAsset | 创建 Prefab 时自动分配给 PlayerController2D 和 PlayerInteraction |
| SpaceLifeManager 缺少 _shipRoot 引用 | `EnsureManagerReferences()` 自动查找 Ship.InputHandler 并赋值 |

### 修改文件：
| 文件路径 | 变更说明 |
|---------|---------|
| `Assets/Scripts/SpaceLife/Editor/SpaceLifeSetupWindow.cs` | 添加 InputActionAsset 自动分配逻辑 |

### 新增方法：
```csharp
private void EnsureInputHandlerReferences(SpaceLifeInputHandler handler)
{
    // 自动查找并分配 InputActionAsset
}
```

### 自动连接的引用：
| 组件 | 字段 | 来源 |
|------|------|------|
| SpaceLifeManager | _spaceLifePlayerPrefab | Assets/Scripts/SpaceLife/Prefabs/Player2D_Prefab.prefab |
| SpaceLifeManager | _spaceLifeSceneRoot | GameObject.Find("SpaceLifeScene") |
| SpaceLifeManager | _spaceLifeSpawnPoint | SpaceLifeScene/SpawnPoint |
| SpaceLifeManager | _spaceLifeCamera | SpaceLifeScene/SpaceLifeCamera |
| SpaceLifeManager | _spaceLifeInputHandler | FindFirstObjectByType<SpaceLifeInputHandler>() |
| SpaceLifeManager | _shipRoot | FindFirstObjectByType<Ship.InputHandler>().gameObject |
| SpaceLifeInputHandler | _inputActions | AssetDatabase.FindAssets("ShipActions") |
| PlayerController2D (Prefab) | _inputActions | 同上 |
| PlayerInteraction (Prefab) | _inputActions | 同上 |

### 技术：
- SerializedObject + SerializedProperty 运行时赋值
- AssetDatabase.FindAssets 查找资源
- Object.FindFirstObjectByType 查找场景对象

---

## 输入系统 ActionMap 冲突修复 — 2026-02-17 11:50

### 问题：
飞船和 SpaceLife 人物都无法控制

### 根本原因：
1. `Ship/Input/InputHandler` 在 `OnDisable` 时调用 `shipMap.Disable()`
2. `PlayerController2D` 和 `SpaceLifeInputHandler` 依赖同一个 `Ship` ActionMap
3. 当 `SpaceLifeManager` 禁用 `Ship/Input/InputHandler` 时，整个 `Ship` ActionMap 被禁用
4. 导致所有依赖该 ActionMap 的组件都无法接收输入

### 解决方案：
让需要输入的组件在 `OnEnable` 时主动启用 ActionMap

### 修改文件：
| 文件路径 | 变更说明 |
|---------|---------|
| `Assets/Scripts/SpaceLife/PlayerController2D.cs` | OnEnable 时检查并启用 Ship ActionMap |
| `Assets/Scripts/SpaceLife/SpaceLifeInputHandler.cs` | OnEnable 时检查并启用 Ship ActionMap |

### 修改代码：
```csharp
// PlayerController2D.OnEnable()
var shipMap = _inputActions.FindActionMap("Ship");
if (shipMap != null && !shipMap.enabled)
{
    shipMap.Enable();
}
```

### 技术：
- InputActionMap.enabled 检查状态
- 多组件共享 ActionMap 时的启用策略

---

## Player Prefab 更新机制 & 飞船输入修复 — 2026-02-17 11:55

### 问题：
1. Player2D Prefab 缺少 SpriteRenderer（已存在的 Prefab 不会更新）
2. 飞船 WASD 不可用

### 根本原因：
1. Setup Wizard 在 Prefab 已存在时直接返回，不检查/更新组件
2. `SpaceLifeInputHandler` 在游戏开始时处于启用状态，干扰了 `Ship/Input/InputHandler`

### 解决方案：

#### 1. 新增 `UpdatePlayerPrefabComponents()` 方法
检查并补齐已存在 Prefab 的缺失组件：
- SpriteRenderer + Sprite
- Rigidbody2D (gravityScale = 0)
- CapsuleCollider2D
- PlayerController2D + InputActionAsset
- PlayerInteraction + InputActionAsset

#### 2. SpaceLifeManager.Start() 禁用 SpaceLifeInputHandler
```csharp
if (_spaceLifeInputHandler != null)
    _spaceLifeInputHandler.enabled = false;
```

### 修改文件：
| 文件路径 | 变更说明 |
|---------|---------|
| `Assets/Scripts/SpaceLife/Editor/SpaceLifeSetupWindow.cs` | 新增 UpdatePlayerPrefabComponents 方法 |
| `Assets/Scripts/SpaceLife/SpaceLifeManager.cs` | Start 时禁用 SpaceLifeInputHandler |

### 输入系统状态流程：
```
游戏启动:
  - Ship/Input/InputHandler: enabled (ActionMap 启用)
  - SpaceLifeInputHandler: disabled
  - 飞船 WASD 可用 ✓

进入 SpaceLife:
  - Ship/Input/InputHandler: disabled (ActionMap 禁用)
  - SpaceLifeInputHandler: enabled (OnEnable 重新启用 ActionMap)
  - PlayerController2D: enabled (OnEnable 重新启用 ActionMap)
  - SpaceLife 人物 WASD 可用 ✓

退出 SpaceLife:
  - SpaceLifeInputHandler: disabled
  - PlayerController2D: disabled
  - Ship/Input/InputHandler: enabled (OnEnable 重新启用 ActionMap)
  - 飞船 WASD 可用 ✓
```

### 技术：
- EditorUtility.SetDirty + AssetDatabase.SaveAssets 保存 Prefab 更改
- 组件启用/禁用顺序控制输入系统状态

---

## Prefab 存放位置修正 — 2026-02-17 12:00

### 问题：
SpaceLife Prefab 存放在 `Assets/Scripts/SpaceLife/Prefabs/`，不符合项目规范

### 项目规范 (CLAUDE.md)：
```
Assets/
├── _Prefabs/                 # 游戏 Prefab
│   ├── Ship/
│   └── Enemies/
```

### 修改：
将 Prefab 路径从 `Assets/Scripts/SpaceLife/Prefabs/` 改为 `Assets/_Prefabs/SpaceLife/`

### 修改文件：
| 文件路径 | 变更说明 |
|---------|---------|
| `Assets/Scripts/SpaceLife/Editor/SpaceLifeSetupWindow.cs` | 更新 prefabPath 和 folderPath |

### 新路径：
```
Assets/_Prefabs/SpaceLife/Player2D_Prefab.prefab
```

### 技术：
- AssetDatabase.IsValidFolder 检查文件夹是否存在
- AssetDatabase.CreateFolder 创建嵌套文件夹

---

## Tab 无法进入 SpaceLife 修复 — 2026-02-17 12:05

### 问题：
按 Tab 无法进入 SpaceLife 模式

### 根本原因：
`SpaceLifeInputHandler` 在游戏开始时被禁用（为了不干扰飞船输入），所以无法接收 Tab 输入

### 解决方案：
让 `Ship/Input/InputHandler` 也监听 `ToggleSpaceLife` action，通过事件通知 `SpaceLifeManager`

### 修改文件：
| 文件路径 | 变更说明 |
|---------|---------|
| `Assets/Scripts/Ship/Input/InputHandler.cs` | 添加 OnToggleSpaceLifePerformed 事件 |
| `Assets/Scripts/SpaceLife/SpaceLifeManager.cs` | 订阅 Ship InputHandler 的事件 |

### 新增代码：
```csharp
// InputHandler.cs
public event Action OnToggleSpaceLifePerformed;
private InputAction _toggleSpaceLifeAction;

// OnEnable
if (_toggleSpaceLifeAction != null)
    _toggleSpaceLifeAction.performed += OnToggleSpaceLifeActionPerformed;

// SpaceLifeManager.cs Start()
_shipInputHandler.OnToggleSpaceLifePerformed += ToggleSpaceLife;
```

### 输入系统架构：
```
飞船模式:
  Ship/Input/InputHandler (enabled)
    └── 监听 ToggleSpaceLife → 触发 OnToggleSpaceLifePerformed 事件
    └── SpaceLifeManager 订阅事件 → 调用 ToggleSpaceLife()

SpaceLife 模式:
  SpaceLifeInputHandler (enabled)
    └── 监听 ToggleSpaceLife → 直接调用 SpaceLifeManager.ToggleSpaceLife()
```

### 技术：
- 事件订阅模式解耦输入处理
- 双入口确保两种模式都能切换

---

## 输入系统全面修复 — 2026-02-17 13:00

### 问题：
1. 按 Tab 无反应
2. 飞船只能旋转，WASD 移动无效
3. Console 报错 `Action map must be contained in state`
4. `[ServiceLocator] Get: InputHandler = NOT FOUND`

### 根本原因：
1. **场景中缺少 `Ship/Input/InputHandler` 组件** - 这是 Tab 切换和飞船移动的核心组件
2. **OnDisable 时禁用已无效的 Action** - 当 ActionMap 被禁用后再尝试禁用单个 Action 会报错

### 修复内容：

#### 1. 修复 OnDisable 安全检查
三个组件都需要检查 ActionMap 状态后再禁用 Action：

```csharp
// PlayerController2D.OnDisable()
if (_moveAction != null && _inputActions != null)
{
    var shipMap = _inputActions.FindActionMap("Ship");
    if (shipMap != null && shipMap.enabled)
    {
        _moveAction.Disable();
    }
}
```

同样修复应用于：
- `PlayerController2D.cs`
- `PlayerInteraction.cs`
- `SpaceLifeInputHandler.cs`

#### 2. Setup Wizard 状态检查增强
新增 `Ship/InputHandler` 状态检查：

```csharp
bool hasShipInputHandler = Object.FindFirstObjectByType<ProjectArk.Ship.InputHandler>() != null;
DrawStatusItem("Ship/InputHandler (CRITICAL)", hasShipInputHandler);

if (!hasShipInputHandler)
{
    EditorGUILayout.HelpBox("Ship/InputHandler is MISSING! This is required for Tab toggle to work.", MessageType.Error);
}
```

#### 3. 添加全面调试日志
- `InputHandler.Awake()` - 检查 _inputActions 是否为 null
- `InputHandler.OnEnable()` - 确认 ActionMap 已启用
- `SpaceLifeManager.Start()` - 确认订阅事件成功
- `ServiceLocator.Register/Get` - 追踪服务注册和获取

### 修改文件：
| 文件路径 | 变更说明 |
|---------|---------|
| `Assets/Scripts/SpaceLife/PlayerController2D.cs` | OnDisable 安全检查 |
| `Assets/Scripts/SpaceLife/PlayerInteraction.cs` | OnDisable 安全检查 |
| `Assets/Scripts/SpaceLife/SpaceLifeInputHandler.cs` | OnDisable 安全检查 |
| `Assets/Scripts/Ship/Input/InputHandler.cs` | 添加 null 检查和调试日志 |
| `Assets/Scripts/SpaceLife/SpaceLifeManager.cs` | 添加调试日志 |
| `Assets/Scripts/Core/ServiceLocator.cs` | 添加注册/获取日志 |
| `Assets/Scripts/SpaceLife/Editor/SpaceLifeSetupWindow.cs` | 检查 Ship/InputHandler 状态 |

### 用户需要确认：
**场景中必须有 `Ship/Input/InputHandler` 组件！**

这个组件通常在 Ship Prefab 上，由 `ShipMotor` 或 `ShipDash` 的 `RequireComponent` 自动添加。

### 技术：
- InputAction.Disable() 前检查 ActionMap.enabled
- ServiceLocator 调试日志追踪服务生命周期
- Editor 状态检查提示用户缺失的关键组件

---

## SpaceLife 模块 Bug 修复与架构清理 — 2026-02-17 21:08

### 概述
修复 SpaceLife 模块 3 大核心 Bug 并完成输入系统架构解耦，共涉及 8 个任务。

### 修改文件：
| 文件路径 | 变更说明 |
|---------|---------|
| `Assets/Input/ShipActions.inputactions` | 新增独立的 `SpaceLife` ActionMap（Move/Interact/ToggleSpaceLife）；移除多余的 `SpaceLifeJump` Action 及其 4 条绑定 |
| `Assets/Scripts/SpaceLife/SpaceLifeInputHandler.cs` | 从 Ship ActionMap 切换到 SpaceLife ActionMap；OnEnable/OnDisable 操作独立 Map 不再干扰 Ship 输入 |
| `Assets/Scripts/SpaceLife/PlayerController2D.cs` | 从 Ship ActionMap 切换到 SpaceLife ActionMap；Enable/Disable 独立不影响 Ship |
| `Assets/Scripts/SpaceLife/SpaceLifeManager.cs` | 增强序列化引用 fallback 自动获取逻辑（_spaceLifeInputHandler/FindFirstObjectByType、_mainCamera/Camera.main、_shipInputHandler/FindFirstObjectByType fallback）；EnterSpaceLife 和 ToggleSpaceLife 入口添加前置条件检查；所有错误日志增加具体组件名+修复建议 |
| `Assets/Scripts/SpaceLife/Editor/SpaceLifeSetupWindow.cs` | 新增 Scene Health Check 面板（检查所有关键组件 ✅/❌ 状态）；新增 "Add Ship to Scene" 按钮（从 Prefab 实例化）；新增 "Auto-Wire References" 按钮（自动填充 SpaceLifeManager 可推导的序列化引用） |

### 内容简述：
1. **输入系统解耦（B3/B6）**：在 ShipActions.inputactions 中新增独立的 `SpaceLife` ActionMap，包含 Move（WASD 4方向+方向键+Gamepad摇杆）、Interact（E键+Gamepad Y）、ToggleSpaceLife（Tab+Gamepad Back）。SpaceLifeInputHandler 和 PlayerController2D 完全切换到 SpaceLife Map，与 Ship Map 零耦合。
2. **移除 SpaceLifeJump（B5）**：从 Ship ActionMap 删除冗余的 SpaceLifeJump Action 和 4 条绑定（W/↑/Space/Gamepad South）。SpaceLife 确认使用 4 方向移动不需要跳跃。
3. **序列化引用修复（B4）**：SpaceLifeManager.Start() 中为 _spaceLifeInputHandler、_mainCamera、_shipInputHandler 增加运行时 fallback 自动获取+Warning 日志。
4. **防御性增强（B1/B2）**：ToggleSpaceLife 和 EnterSpaceLife 入口增加 _spaceLifePlayerPrefab/_spaceLifeCamera/_spaceLifeSceneRoot 的 null 前置条件检查，失败时打印具体原因。
5. **Editor 工具增强**：SpaceLifeSetupWindow 新增 Scene Health Check 面板，含一键添加 Ship 到场景和 Auto-Wire References 功能。

### 目的：
修复 SpaceLife 模块无法通过 Tab 进入、输入系统 Ship/SpaceLife 互相干扰、序列化引用缺失导致静默失败等问题，建立清晰的输入架构边界。

### 技术方案：
- 方案 A（独立 ActionMap）：在同一 InputActionAsset 中新增 SpaceLife ActionMap，避免共享 Ship Map 的 Enable/Disable 互相干扰
- SpaceLifeManager 使用 ServiceLocator + FindFirstObjectByType 双重 fallback 策略
- Editor 工具使用 SerializedObject API 检查和自动填充序列化引用
- PrefabUtility.InstantiatePrefab 用于一键添加 Ship 到场景

---

## SpaceLife Player2D 可见性修复 + 胶囊 Sprite — 2026-02-18 01:05

### 修改文件：
- `Assets/Scripts/SpaceLife/Editor/SpaceLifeMenuItems.cs` — 新增 `CreateCapsuleSprite` 方法；`CreatePlayerController` 菜单项添加 SpriteRenderer + 白色胶囊 sprite
- `Assets/Scripts/SpaceLife/Editor/SpaceLifeSetupWindow.cs` — `CreatePlayerPrefab` 和 `UpdatePlayerPrefabComponents` 改用白色胶囊 sprite（替换原来的 cyan 方形）
- `Assets/Scripts/SpaceLife/PlayerController2D.cs` — 新增 `Start()` 方法，防御性隐藏：非 SpaceLife 模式时自动 `SetActive(false)`

### 内容简述：
1. **CreateCapsuleSprite**：程序化生成 32×64 像素白色胶囊形状 Sprite（半圆顶+矩形身体+半圆底），用于 Player2D 的默认视觉
2. **Setup Wizard / Menu Items**：所有创建 Player2D 的入口（Wizard Phase 1、Create Player Controller 菜单、Update 检查）统一使用白色胶囊 sprite 替代原来的 cyan 方形
3. **Player2D 可见性**：在 `PlayerController2D.Start()` 中通过 `ServiceLocator.Get<SpaceLifeManager>()` 检查当前模式，如果不在 SpaceLife 模式则自动隐藏 gameObject，防止 Player2D 在飞船操作期间意外显示

### 目的：
- 修复 Setup Wizard 创建的 Player2D 缺少 SpriteRenderer / 使用错误形状的问题
- 修复 Player2D 在飞船操作期间仍然可见的 bug

### 技术方案：
- 程序化纹理生成（Texture2D + Sprite.Create）绘制胶囊形状，利用圆心距离判断像素是否在胶囊边界内
- 防御性 Start() 检查利用 ServiceLocator 查询 SpaceLifeManager 状态，确保 Player2D 只在 SpaceLife 模式激活时可见

---

## SpaceLife + StarChart 模块 CLAUDE.md 代码规范合规修复 — 2026-02-18 10:15

### 修改文件：
- `Assets/Scripts/SpaceLife/PlayerInteraction.cs` — 替换 FindObjectsByType 为 Trigger 检测模式（OnTriggerEnter2D/Exit2D + \_nearbyInteractables 列表）
- `Assets/Scripts/SpaceLife/SpaceLifeRoomManager.cs` — 移除 FindAllRooms()/FindObjectsByType，改为 RegisterRoom/UnregisterRoom 自注册模式；手写 Lerp 相机平移替换为 PrimeTween.Position；添加 PrimeTween 依赖
- `Assets/Scripts/SpaceLife/SpaceLifeRoom.cs` — 添加 OnEnable/OnDisable 自注册到 RoomManager
- `Assets/Scripts/SpaceLife/SpaceLifeManager.cs` — 移除 FindFirstObjectByType fallback，改为 ServiceLocator-only；OnDestroy 添加 OnEnterSpaceLife/OnExitSpaceLife = null 事件清理
- `Assets/Scripts/SpaceLife/Data/NPCDataSO.cs` — public 字段改为 [SerializeField] private + PascalCase 只读属性
- `Assets/Scripts/SpaceLife/Data/ItemSO.cs` — public 字段改为 [SerializeField] private + PascalCase 只读属性
- `Assets/Scripts/SpaceLife/Data/DialogueData.cs` — DialogueLine/DialogueOption public 字段改为 [SerializeField] private + PascalCase 只读属性
- `Assets/Scripts/SpaceLife/NPCController.cs` — 更新所有 NPCDataSO/DialogueLine 字段引用为新属性名
- `Assets/Scripts/SpaceLife/RelationshipManager.cs` — 更新 NPCDataSO 引用；OnDestroy 添加 OnRelationshipChanged = null；移除 RelationshipLevel 枚举
- `Assets/Scripts/SpaceLife/DialogueUI.cs` — 更新 DialogueLine/DialogueOption 引用；OnDestroy 添加 OnDialogueEnd = null
- `Assets/Scripts/SpaceLife/GiftUI.cs` — 更新 ItemSO 引用；OnDestroy 添加 OnGiftGiven = null
- `Assets/Scripts/SpaceLife/GiftInventory.cs` — 更新 ItemSO 引用；OnDestroy 添加 OnInventoryChanged = null
- `Assets/Scripts/SpaceLife/TransitionUI.cs` — FadeInAsync/FadeOutAsync 手写 Lerp 替换为 PrimeTween.Custom + ToUniTask
- `Assets/Scripts/SpaceLife/Interactable.cs` — 运行时 Instantiate/Destroy 指示器改为 Awake 预创建 + SetActive 切换
- `Assets/Scripts/Combat/StarChart/StarChartController.cs` — 动态 AddComponent<AudioSource> 改为 [RequireComponent] + GetComponent；OnDestroy 添加 OnTrackFired/OnLightSailChanged/OnSatellitesChanged = null

### 新建文件：
- `Assets/Scripts/SpaceLife/SpaceLifeRoomType.cs` — 从 SpaceLifeRoom.cs 提取的枚举
- `Assets/Scripts/SpaceLife/RelationshipLevel.cs` — 从 RelationshipManager.cs 提取的枚举
- `Assets/Scripts/SpaceLife/Data/NPCRole.cs` — 从 NPCDataSO.cs 提取的枚举
- `Assets/Scripts/SpaceLife/ProjectArk.SpaceLife.asmdef` — 新增 PrimeTween.Runtime 引用
- `Assets/Scripts/SpaceLife/Editor/ProjectArk.SpaceLife.Editor.asmdef` — 新增 rootNamespace 字段

### 内容简述：
全面修复 SpaceLife 模块（20个文件）和 StarChart 模块的 CLAUDE.md 代码规范违规项，共覆盖 9 个需求类别。

### 目的：
确保 SpaceLife 和 StarChart 模块完全符合 CLAUDE.md 架构原则和代码规范，消除所有运行时 Find* 调用、public SO 字段、缺失的事件清理、手写 Lerp 补间、动态 AddComponent、枚举与类同文件等技术债务。

### 技术方案：
1. **禁止运行时 Find***：PlayerInteraction 改为 Trigger 碰撞检测 + 列表维护；RoomManager 改为自注册模式；SpaceLifeManager 移除 fallback
2. **SO 数据封装**：NPCDataSO/ItemSO/DialogueData 所有 public 字段改为 `[SerializeField] private` + PascalCase 只读属性（IReadOnlyList 暴露集合）
3. **一文件一类**：SpaceLifeRoomType、RelationshipLevel、NPCRole 枚举各自独立文件
4. **事件卫生**：所有 event/Action 在 OnDestroy 中 `= null` 清空，防止内存泄漏
5. **PrimeTween 替代手写 Lerp**：TransitionUI 的 fade 用 Tween.Custom + useUnscaledTime:true；RoomManager 相机用 Tween.Position
6. **[RequireComponent] 替代动态 AddComponent**：StarChartController 的 AudioSource
7. **Interactable 指示器预创建**：Awake 中创建或引用，SetActive 切换，避免运行时 Instantiate/Destroy

---

## 示巴星前15分钟关卡设计三方案 — 2026-02-18 21:07

### 修改文件：
- `Docs/GDD/示巴星 (鸣钟者)/关卡心流与节奏控制.csv`（追加内容）

### 内容简述：
在关卡心流与节奏控制文档末尾追加了示巴星前15分钟的三个详细关卡设计方案及对比总结：
1. **方案一：紧张生存型 (Countdown Escape)** — 高压开场，倒计时逃跑+追击战+压力破墙，心流曲线 8→7→6→2（倒U型）
2. **方案二：探索发现型 (Quiet Discovery)** — 低张力开场，自由探索+听觉解谜+NPC道德选择，心流曲线 2→4→5→6（上升型）
3. **方案三：叙事沉浸型 (Echoes of Silence)** — 叙事驱动，纯氛围醒来+声音=色彩世界观+壁画渐进战斗，心流曲线 1→3→4→5（缓升型）
4. **三方案对比矩阵** — 从开场张力、武器获取方式、教学风格等9个维度横向对比
5. **推荐混合方案思路** — 方案三开场 + 方案一压力 + 方案二选择

### 目的：
为示巴星的前15分钟体验提供多样化的设计选项，支持团队讨论和决策。每个方案针对不同玩家类型（硬核/探索/叙事），并附带心流曲线可视化和风险评估。

### 技术方案：
纯设计文档工作，无代码变更。方案设计基于已有的怪物与机关表、叙事演出表、地图锁钥矩阵等文档数据，确保所有引用的敌人、机关、星图部件均与现有设计一致。

---

## 示巴星前一小时关卡心流五方案（5-10分钟颗粒度）— 2026-02-19 01:40

### 修改文件：
- `Docs/GDD/示巴星 (鸣钟者)/关卡心流与节奏控制3.csv`（重建并写入完整内容）

### 内容简述：
1. 依据示巴星现有文档（锁钥矩阵、怪物机关、叙事演出、资产清单）重建 `关卡心流与节奏控制3.csv` 为可直接评审的统一心流表。
2. 在同一表结构下输出 5 套“前1小时”方案（方案A-E），每套拆分为 8 个时段，单段时长控制在 5-10 分钟。
3. 每个时段完整填写 9 列信息：时段、区域、乐章、张力、目标、障碍、关键获取、能力验证、POIs，确保与现有 Z1-Z4 与 L-02/L-03/L-04/L-07 锁钥语义一致。
4. 五套方案分别覆盖不同体验目标：高压生存、探索发现、叙事沉浸、速攻推进、新手友好，便于制作阶段A/B测试与团队评审选型。

### 目的：
为示巴星首小时提供可直接落地的节奏设计备选集，在不改动核心世界观与系统约束的前提下，支持快速比较不同玩家体验路径和难度曲线。

### 技术方案：
- 文档驱动策划：沿用既有 CSV 表头与字段定义，不新增字段，保证与现有流程兼容。
- 锁钥一致性校验：方案中的获取与验证严格映射已有机制（涟漪、相位、穿透、捷径电梯）。
- 节奏分层设计：通过张力曲线与"高压-释放"段落配比，形成可执行的首小时心流模板。

---

## 示巴星前一小时关卡设计五方案（全新版） — 2026-02-19 10:32

### 修改文件：
- `Docs/GDD/示巴星 (鸣钟者)/关卡心流与节奏控制.csv`（替换旧的前15分钟方案，追加全新的前一小时5套方案）

### 内容简述：
在关卡心流与节奏控制文档中，将旧的前15分钟3方案区域替换为全新的前一小时（60min）5套详细关卡设计方案及对比总结：

1. **方案一：钟摆型 (The Pendulum)** — 张力在高低(8↔3)之间规律摆动，心流曲线：8-3-8-3-8-3-8
2. **方案二：洋葱层型 (The Onion)** — 复杂度单调递增(移动→战斗→叙事→综合→情感→道德)，心流曲线：1-4-5-7-6-9
3. **方案三：双螺旋型 (Double Helix)** — 战斗线(红)和叙事线(蓝)交替主导融合为合奏，心流曲线：4-2-7-3-8-4-7
4. **方案四：潮汐型 (The Tide)** — 波浪一波比一波高最终海啸，心流曲线：3-2-5-4-7-5-9
5. **方案五：拼图型 (The Jigsaw)** — 独立拼图碎片最后全景拼合，心流曲线：4-6-3-7-4-9
6. **五方案12维对比矩阵 + 推荐混合方案**

### 目的：
为示巴星的前一小时体验提供5套多样化的设计选项，支持团队讨论和决策。

### 技术方案：
纯设计文档工作，无代码变更。方案基于已有怪物与机关表、叙事演出表、地图锁钥矩阵设计。

---

## 示巴星前一小时关卡心流五方案（第三批）— 2026-02-19 11:06

### 新建文件：
- `Docs/GDD/示巴星 (鸣钟者)/关卡心流与节奏控制6.csv`（新建）

### 内容简述：
创建第三批示巴星前一小时关卡设计方案，与前两批（共10个方案）形成差异化互补。本次新增5套全新方案：

1. **方案一：螺旋上升型 (The Ascent)** — 每次战斗后回落休息，但每次休息起点都比上次更高，心流曲线：3→6→4→7→5→8→9
2. **方案二：峰谷对称型 (The Mirror)** — 前30分钟上升，30分钟顶峰(9/10)，后30分钟镜像下降，心流曲线：2→4→6→9→6→4→2
3. **方案三：心跳型 (The Heartbeat)** — 模拟心跳节律，收缩期(高压)/舒张期(低压)交替，中间有独特的"暂停"(1/10)，心流曲线：7→3→8→1→9→5→10
4. **方案四：阶梯型 (The Staircase)** — 稳定平台期(3-5)与剧烈跳跃期(8-9)交替，心流曲线：3→8→4→8→5→9
5. **方案五：暗夜行者型 (The Dawn)** — 从黑暗走向光明，开局最高张力(9/10)持续下降到结尾(2/10)，心流曲线：9→7→5→4→3→2

每套方案均包含：完整的时段划分（5-10分钟颗粒度）、区域设置、张力值、核心目标、障碍、获取物品、能力验证、POIs等详细字段。同时提供了三批方案总览（共15个方案）和推荐混合方案思路。

### 目的：
为示巴星前一小时提供更多样化的心流设计选项，覆盖螺旋成长、对称释放、心跳节奏、稳定递进、情感逆转等不同体验目标，支持团队评审选型或A/B测试。

### 技术方案：
纯设计文档工作，无代码变更。方案设计基于已有的怪物与机关表、叙事演出表、地图锁钥矩阵等文档数据，确保所有引用的敌人、机关、星图部件均与现有设计一致。

---

## 示巴星完整5小时关卡体验设计 — 2026-02-20 11:10

### 修改文件：
- `Docs/GDD/示巴星 (鸣钟者)/关卡心流与节奏控制-2.csv`（全面重写，63行→205行）

### 内容简述：
将原有的60分钟双螺旋型方案扩展为完整的5小时（300分钟）三幕五章关卡体验设计。文件包含：

**结构框架**：整体架构概览、星图部件获取规划（13件）、敌人图鉴（13种示巴星特化敌人）

**五章关卡流程**：
- **ACT 1 坠落与觉醒 (0:00-50:00)**：6个节点，双武器教学+基础战斗+认知转折(敌人=走调碎片)+首棱镜
- **ACT 2 叹息的峡谷 (50:00-90:00)**：4个节点，涟漪获取+四重用途+坚盾歌者+叹息墙Boss
- **ACT 3 回音枢纽 (90:00-190:00)**：7个节点，渐强渐弱系统+破碎守夜人(可安抚Boss)+光帆净化+指挥家登场+中期合唱考验
- **ACT 4 孵化深渊 (190:00-250:00)**：5个节点，孵化洞穴道德重压+静音走廊极限生存+叙事核弹(大静默真相:听觉猎手)+调律帆
- **ACT 5 哭泣钟楼 (250:00-300:00)**：3个节点，垂直攀登+永恒终止式+最终Boss:星际即兴曲(音乐会式Boss战+双结局)

**总结**：全程心流曲线(27个节点)、设计哲学6条、6星球体系定位矩阵(12维度)

### 目的：
完成示巴星作为第一个星球的完整4-6小时关卡体验设计，确保故事引人入胜、生态惊奇、敌人有趣、能力循序渐进、道德系统逐步深化，同时为后续5个星球预留成长空间。

### 技术方案：
纯设计文档工作，无代码变更。使用Python脚本分4批写入CSV（框架+ACT1→ACT2→ACT3→ACT4+ACT5+总结），确保UTF-8编码正确。所有敌人/星图部件引用与EnemyPlanning.csv和StarChartPlanning.csv保持一致。

---

## 示巴星 NPC 缕拉(Lyra)设计 + 关卡心流/机制全面优化 2026-02-21 10:45

### 新建文件：
- `Docs/GDD/示巴星 (鸣钟者)/NPC-鸣钟者角色设定.md` — 完整的NPC角色设定文档

### 修改文件：
- `Docs/GDD/示巴星 (鸣钟者)/关卡心流与节奏控制-2.csv` — 全面重建(171行)，整合所有NPC+机制改进
- `Docs/GDD/示巴星 (鸣钟者)/叙事与遭遇演出表.csv` — 增加NPC缕拉叙事触发点L-01到L-08，更新N-12

### 内容简述：

#### 1. NPC 缕拉(Lyra)角色设计【最高优先级】
- **角色**：最后的调音师——共鸣王朝第七声部（即兴段），拒绝大静默封印的自由歌者
- **视觉**：淡金色小型鸣钟者，琥珀色核心，左臂有大静默黑色裂纹（持续歌唱抵抗侵蚀）
- **核心关系**：索尔（Sol）——第一声部/基音/缕拉的爱人，选择沉默保护所有人
- **5幕故事弧**：
  - ACT1: 间接线索(温暖冰雕Z1b)+足迹(花路Z1d)+首次远距目击(Z1f)
  - ACT2: 远程援助(Z2a)+首次面对面(Z2b从恐惧到好奇)+频率标记buff(Z2c)
  - ACT3: 共情#1索尔雕像(Z3a)+安抚Boss辅助(Z3c)+飞翔快乐(Z3d-bis)+共情#2追捕记忆(Z3e)+合唱指挥(Z3g)
  - ACT4: 摇篮曲消逝预兆(Z4a)+共情#3索尔遗言→停止歌唱→半身黑化(Z4c)+最后请求(Z4d)
  - ACT5: 虚弱指挥+第七声部注入新歌+索尔苏醒+上船(Do-Sol约定)
- **3个共情触发点**：#1索尔雕像(95:00) #2追捕记忆(165:00) #3索尔遗言(235:00)
- **可选增益**：歌唱者跟随buff/安抚容错提高/合唱效率+50%/幼体稳定/高频脉冲/歌唱者增益x3

#### 2. 关卡心流CSV全面重建(171行)
- 修复编码问题（原文件混合编码无法解析→重建为UTF-8）
- **27个关卡节点**（含新增Z3d-bis天穹回廊）覆盖310分钟完整体验
- 心流曲线：ACT1(4→2→7→4→3→6) ACT2(5→3→5→9) ACT3(4→7→8→4→3→8→7→9) ACT4(6→8→3→7) ACT5(8→9→10)

#### 3. 七项机制改进
1. **涟漪=独立交互工具**：长按专用键发射，不占武器槽位，定位为探索工具非战斗武器
2. **世界时钟深度铺垫**：ACT2预兆(Z2c退潮现象+Z2d Boss战DPS窗口)→ACT3正式引入(3+强依赖场景)→ACT4崩坏(剥夺感)
3. **Z4a噪音累积槽**：替代硬潜行，渐进后果(30%骚动→60%局部发狂→100%全面爆发)，停火恢复，涟漪不加噪音
4. **Z4b完美闪避降热**：Dash穿过攻击=排热15-20%，蒸汽涟漪视觉反馈，与共鸣水晶形成主动vs被动互补
5. **Z3c Boss安抚简化**：4水晶边缘放置+8-10秒保持(非3秒)+走位陆续点亮+缕拉辅助延长到10秒
6. **Z5c视觉电报系统**：高音=蓝色上方/低音=红色地面/和弦=金色边框，视觉早于音频0.5-1秒，Signal-Window模式
7. **Z3d-bis减压阀**：天穹回廊12分钟，零战斗零新机制，张力3/10，纯跑酷+缕拉最快乐时刻

#### 4. 叙事触发点扩展
- 在叙事与遭遇演出表中增加L-01到L-08共8个NPC缕拉专属触发点
- 更新N-12终焉触发点以反映缕拉+索尔的结局演出

### 目的：
1. 通过NPC缕拉为示巴星注入情感核心，确保玩家在结束探索时有足够动力将她带上金丝雀号
2. 修复ACT3信息过载、世界时钟铺垫不足、硬潜行割裂等心流隐患
3. 优化涟漪槽位冲突、Boss安抚操作难度、热量系统死锁、最终Boss可读性等机制问题

### 技术方案：
纯设计文档工作，无代码变更。使用Python脚本分5批写入CSV（ACT1→ACT2→ACT3→ACT4→ACT5+总结），UTF-8编码。NPC角色设定独立为.md文档。叙事触发点通过replace_in_file直接编辑CSV。所有设计与EnemyPlanning.csv/StarChartPlanning.csv/地图逻辑与锁钥矩阵.csv保持一致。

---

## 世界观圣经框架 (World Bible Framework) — 2026-02-23 11:22

### 新建文件：
- `Docs/GDD/WorldBible.md`

### 内容简述：
创建《静默方舟》完整世界观设定框架文档（World Bible v1.0），共九篇章：
1. **宇宙本体论**：大静默机制（熵的强制归零）、侵蚀三阶段、宇宙听觉猎手
2. **文明图谱**：人类文明（巴别塔事件）+ 六大异类种族完整设定（十七年蝉/鸣钟者/因果倒置者/活体图书馆/宿主乘客/数学家）+ 种族关系矩阵
3. **星球生态**：三大星球（克洛诺斯/示巴/欧几里得）的环境、种族配对、核心冲突与玩家抉择
4. **核心角色**：金丝雀号三人组（舰长/伊恩/守夜人）+ 示巴星NPC缕拉完整故事弧 + 大指挥家
5. **核心主题与哲学**：三组核心矛盾、清算电影基调、理解度系统、因果透支机制
6. **视听美学宪章**：视觉/听觉/UI美学总纲与禁忌
7. **结局哲学**：飞升概念、六族逻辑大合奏、示巴星结局分支
8. **术语表**：18个核心术语定义
9. **参考圣经**：叙事/角色/美学/游戏设计四类参考作品

### 目的：
将散落在GDD.md、静默方舟.md、示巴星V2.md、NPC角色设定.md、关卡心流CSV等多个文档中的世界观信息整合为单一权威参考源，建立"宪法级"框架，确保所有子文档与其保持一致。

### 技术方案：
纯设计文档工作，无代码变更。综合分析所有现有设计文档后提炼核心世界观要素，按逻辑层次组织为九篇章结构。文档作为所有后续世界观相关设计的终极参考。

---

## 转换星球构建框架文档 — 2026-02-23 11:35

### 新建文件：
- `Docs/GDD/星球构建框架.md`

### 内容简述：
将 `Docs/GDD/星球构建框架.docx` 文件提取并转换为 Markdown 格式的文档 `Docs/GDD/星球构建框架.md`。该文档深度构建了游戏世界观的六个层次：
1. 第一层：物理与环境层（核心公理、空间拓扑、大气氛围等）
2. 第二层：生物与生态层（生理基质、感官逻辑、生态位等）
3. 第三层：文明与历史层（社会逻辑、建筑风格、历史断层等）

---

## 示巴星 (Sheba) 完整设定圣经文档 — 2026-02-23 18:30

### 新建文件：
- `Docs/GDD/示巴星 (鸣钟者)/ShebaPlanetBible.md`

### 内容简述：
创建示巴星完整设定圣经文档（约3000+行），作为示巴星一切设计的"宪法级"终极参考源。整合并扩展 WorldBible.md、示巴星V2.md、NPC-鸣钟者角色设定.md 等已有设定，与关卡心流CSV和地图锁钥矩阵保持一致。文档包含七篇：

1. **第一篇：星球物理与天文** — 声学显形定律公式化描述、热力学反转机制、天文参数、气象系统（声波风暴/静默潮/晶化雾）、世界时钟规则
2. **第二篇：鸣钟者文明** — 生理学（压电晶体外壳/声学凝胶内质）、七声部社会结构、日常生活细节、经济技术体系、七章史完整历史
3. **第三篇：区域地理与生态** — Z1-Z6六大区域详述、声学食物链、18种物种图谱（含8种非敌对生物）、6种植被/菌类、大静默生态破坏路径
4. **第四篇：缕拉完整人物设定** — 基础档案、3200年个人史、多维性格画像、5种标志性旋律库、关系网（6组NPC关系）、5幕故事弧、3个共情触发点、buff辅助表
5. **第五篇：NPC图谱** — 索尔(Sol)、大指挥家(Grand Conductor)、破碎守夜人(Broken Watchman)、痛苦长老(Elder of Sorrow)、巨型休眠鸣钟者(Sleeping Giant)、歌唱爬行者(Singing Crawlers)、黑水之母(Mother of Black Water)、活体图书馆观察者(Living Archive Observer)、失调工匠(Detuned Artisan)、回声体(Echo Remnants) — 共10个NPC完整设定页
6. **第六篇：文明遗迹与考古学** — Z1-Z6遗迹目录(21处)、7条克拉德尼文铭文(含原文与翻译)、4段黑匣子录音、七章史交叉引用
7. **第七篇：感官与美学规范** — 色彩字典(含Hex值)、6种核心材质库、Z1-Z6环境音效三层结构、6种光源类型与区域光照氛围

附带完整术语表（40+条目）。

### 目的：
为示巴星关卡提供完整的世界观、NPC、生态、美学参考，确保后续所有子文档（关卡设计表、美术参考板、音效设计稿、对话脚本）的一致性。

### 技术方案：
Markdown设定文档，整合已有设定+大幅扩展。参考源：WorldBible.md、示巴星V2.md、NPC-鸣钟者角色设定.md、关卡心流与节奏控制-2.csv、怪物与机关.csv、叙事与遭遇演出表.csv、地图逻辑与锁钥矩阵.csv。
4. 第四层：冲突与矛盾层（敌意逻辑、玩家定位、反派具象化等）
5. 第五层：干涉与救赎层（干涉机制、世界演变、认知重构等）
6. 第六层：交互与元系统层（界面叙事、元游戏成长、感官反馈等）
以及增补层：时间动态层（宏观周期、生态调度）和听觉架构层（声学物理、功能性音频）。

### 目的：
为星球构建提供一套规范化的深度架构指南，帮助设计在物理、生物、文明、冲突等多维度保持统一性和深度，并便于在项目中直接查阅和版本控制。

### 技术方案：
通过执行已有的 Python 脚本 `convert_docx.py`，解析 `.docx` 文件的 `word/document.xml`，提取文本内容与加粗样式等格式，最终将其转换为标准的 Markdown 文件输出。

---

## GDD 文档：世界构成框架 — 2026-02-23 23:34

### 新建文件：
- `Docs/GDD/世界构成框架.md`

### 内容简述：
创建世界构成框架元方法论文档，汇集构建 WorldBible（宇宙级）和 ShebaPlanetBible（星球级）过程中使用的所有维度、要素和思考工具。文档包含：

- **第零层：核心支柱与基调** — 高概念、设计支柱、情感基调声明、禁忌清单
- **第一层：物理与宇宙** — 核心公理、物理法则、天文参数、气象系统、科技边界、世界时钟、公理→机制映射
- **第二层：生态与生物** — 物种图谱、食物链/能量循环、植被/菌类/矿物、变异/感染视觉规范
- **第三层：文明与历史** — 种族百科、派系关系矩阵、经济系统、编年史
- **第四层：地理与空间** — 宏观地图、区域详述模板、建筑学/聚落逻辑、关键地标POI
- **第五层：角色** — 主角设定、核心NPC完整设定页模板、反派设计原则、补充NPC、跨星球NPC设计原则
- **第六层：遗迹与考古** — 遗迹目录模板、铭文/收集物、Lore五层传递方法论、音频日志
- **第七层：感官与美学** — 色彩字典、材质库、光照指南、音效设计指南、音乐风格指南、视觉禁忌清单
- **第八层：主题与哲学** — 核心矛盾、叙事基调、道德灰度框架、结局哲学
- **第九层：术语与命名** — 术语表规范、命名法则（按种族/层级）
- **第十层：一致性与交叉引用** — 一致性检查清单、子文档索引、参考圣经

附录包含：宇宙级vs星球级文档分工表、文档编写顺序建议、12条常见陷阱避坑指南、7条质量自检问题。

所有要素均以问题驱动形式呈现，附带资深世界架构师的心得和指导意见。

### 目的：
作为元方法论文档，为未来任何星球/世界的设定工作提供可复用的通用框架。确保后续克洛诺斯星、欧几里得星等PlanetBible的编写遵循一致的维度和质量标准。

### 技术方案：
Markdown方法论文档，综合WorldBible.md（2145行）和ShebaPlanetBible.md（2060行）的实际构建经验，提炼为通用框架。

---

## 删除"宇宙听觉猎手"概念 — 2026-02-24 00:49

### 修改文件：
- `Docs/GDD/WorldBible.md` — 删除1.3章节（宇宙听觉猎手定义）、修改过热机制描述、删除术语表条目
- `Docs/GDD/示巴星 (鸣钟者)/ShebaPlanetBible.md` — 替换13处"听觉猎手"引用为"大静默"相关表述、删除术语表条目
- `Docs/GDD/示巴星 (鸣钟者)/NPC-鸣钟者角色设定.md` — 替换4处引用（章节标题、指挥家态度、索尔遗言录音）
- `Docs/GDD/示巴星 (鸣钟者)/示巴星 V1.md` — 替换1处引用（真相揭示段落）
- `Docs/GDD/示巴星 (鸣钟者)/示巴星 V2.md` — 替换1处引用（结局真相描述）
- `Docs/GDD/示巴星 (鸣钟者)/关卡心流与节奏控制-2.csv` — 替换3处引用（叙事核弹描述、索尔遗言、验证列）

### 内容简述：
彻底删除"宇宙听觉猎手 (Auditory Predators / Cosmic Listener)"这一概念。该概念与"大静默"功能完全重叠——两者都是"追踪信息噪音源并将其归零"的宇宙级威胁。删除后，所有叙事中原由"听觉猎手"承担的威胁角色统一由"大静默"本身直接承担。

### 目的：
消除概念冗余。大静默作为宇宙熵增现象本身就具备"侵蚀信息噪音源"的特性，无需额外引入一个"执行者"实体。简化世界观层级，让威胁模型更清晰：大静默 = 唯一的宇宙级外部威胁。

### 技术方案：
全文搜索替换，逐处根据上下文调整措辞确保叙事自洽。关键改写包括：
- "被听觉猎手追踪" → "被大静默感知/侵蚀"
- "听觉猎手已接近" → "大静默已逼近"
- "引来听觉猎手" → "加速大静默的侵蚀"
- "听觉猎手找不到你" → "大静默触及不到你"
涉及6个文件共约22处修改，已通过全目录grep验证无残留。

---

## 新增终焉者种族概念储备 — 2026-02-25 15:30

### 修改文件：
- `Docs/GDD/WorldBible.md` — 在第二篇文明图谱新增 VII. 终焉者条目；在 1.4.2 种族能力边界表新增终焉者一行

### 内容简述：
新增第七个种族"终焉者 (The Concluders)"作为概念储备条目（标注 ⚠️ 待深化）。核心概念：以死亡为圆满而非终止的文明——每个个体携带"存在方程"，生命是逐步求解的过程，方程解出即完成，强制杀死等于污染而非解答。包含总览表、生存逻辑、战斗机制（完成催化/未竟枷锁/共情协议终极形态）、创伤细节。

### 目的：
填补现有六族对"死亡假设"这一人类认知底层盲区的覆盖空白。终焉者是唯一不恐惧大静默的文明，在叙事上能产生独特张力；其 Boss 战机制（推断并帮助完成存在方程）是共情协议系统最深层表达，与支柱 I"理解即武器"直接对位。

### 技术方案：
在数学家条目之后、2.3 种族关系矩阵之前插入完整条目。1.4.2 能力边界表同步补录，保持文档内部一致性。

---

## 示巴星 ACT1+ACT2 关卡 JSON 生成 + LevelDesigner 空格平移功能 — 2026-02-25 20:00

### 新建文件：
- `Docs/LevelDesigns/Sheba_ACT1_ACT2.json` — 示巴星 ACT1+ACT2 完整关卡布局

### 修改文件：
- `Tools/LevelDesigner.html` — 空格键平移画布（Figma 风格）+ 多项 Bug 修复

### 内容简述：

#### 1. 示巴星 ACT1+ACT2 关卡 JSON
根据 `关卡心流与节奏控制-2.csv` 节点 Z1a～Z2d（0:00～90:00，10个节点）生成可直接导入 LevelDesigner.html 的布局文件：
- **29 个房间**：safe / normal / arena / boss 四种类型，含 Z1d 隐藏墓穴（floor -1）、Z2a 上层平台（floor 1）、Z2d Boss 竞技场（22×13 最大尺寸）
- **28 条连接**：主线路径 + 侧路分岔，完整覆盖 ACT1 六节点与 ACT2 四节点
- **元素配置**：生成点、检查点、NPC（缕拉5幕弧触发点）、宝箱、敌人、门，与叙事与遭遇演出表、锁钥矩阵保持一致

#### 2. 空格键平移画布（Figma 风格）
- 按住 `Space` 光标变 grab，拖拽时变 grabbing，松开恢复正常
- 引入 `#world` 容器包裹所有房间 div，`transform: translate(panX, panY)` 整体平移
- SVG 连线层与 `#world` 同步 `transform`，坐标系始终对齐
- 网格背景（canvas）用 `panX/Y % GRID_SIZE` 取模计算偏移，实现无限滚动网格视觉
- 输入框聚焦时空格不触发平移

#### 3. Bug 修复（共 6 项）
1. **连线起点偏移**：`startConnection` 初始化 `mouseX/Y` 时漏减 `panX/Y`
2. **元素 drop 坐标重复偏移**：`addElementToRoom` 传参时 `x + panX` 重复计算，改为直接传 world 坐标
3. **pan 模式下误触发房间拖拽**：`setupRoomEvents mousedown` 加 `if (isPanMode) return` 拦截
4. **pan mousedown 事件时序**：改为 capture 阶段注册（`addEventListener(..., true)`）
5. **连线消失**：SVG 误放入 `#world`（宽高 0 容器）→ 移回 `canvas-area` 直接子级，`applyPan` 同步 SVG transform
6. **拖拽元素到画布无反应**：给 `#world` 加 `dragover` 监听；改为坐标 hit-test 自动检测落点所在房间

### 目的：
提升 LevelDesigner 工具可用性，支持大型关卡（29 个房间）的平移浏览；交付示巴星 ACT1+ACT2 完整关卡布局供策划评审。

### 技术方案：
- Pan 系统：`#world` + SVG 双层同步 translate，canvas 网格 modulo 偏移
- 坐标系统一为 world 空间：`worldX = screenX - canvasRect.left - panX`
- 元素 drop：`world.addEventListener('dragover')` + 坐标 hit-test fallback

---

## LevelDesigner 连接线方向与选中功能 — 2026-02-26 00:50

### 修改文件：
- `Tools/LevelDesigner.html`

### 内容简述：
重构 LevelDesigner HTML 工具中的连接线系统，支持单向/双向门配置与连接线交互操作：

1. **单向/双向连接逻辑**：从房间A拉到房间B创建一条A→B单向连接；再从B拉回A创建B→A单向连接，两条记录共存即表示双向互通。同方向不允许重复创建。
2. **箭头方向渲染**：每条连接线末端带SVG箭头（`<marker>`），指向目标房间。双向连接时两条线各自带箭头，并施加垂直偏移避免重叠。
3. **连接线可选中**：SVG连接线添加透明宽14px的hitarea层接收鼠标事件，点击选中后高亮为蓝色，属性面板显示连接详情（类型、来源、目标、方向）。
4. **连接线可删除**：选中连接线后可通过属性面板按钮或Delete/Backspace键删除。双向连接可选择只删单向或删除整对。
5. **属性面板联动**：选中房间时显示连接列表（标注单向/双向）；选中连接时显示连接属性面板；点击空白取消所有选中。

### 目的：
让关卡设计师能直观配置门的通行方向（单向门/双向门），为后续对接项目 Door 系统做准备。

### 技术方案：
- 连接数据结构保持 `{from, to, fromDir, toDir}` 不变，通过是否存在反向记录判断双向
- SVG 使用 `<marker>` 定义箭头，`<line>` 绘制可见线段与隐形点击区域
- 双向连接的两条线施加±4px垂直偏移，避免视觉重叠
- `selectedConnectionIndex` 全局状态管理选中的连接线索引
- 选中房间与选中连接互斥，切换时自动取消对方的选中状态

---

## LevelDesigner 连接点→门元素关联（doorLink）功能 — 2026-02-26 01:15

### 修改文件：
- `Tools/LevelDesigner.html`

### 内容简述：
新增"门关联"（doorLink）功能，允许从房间边缘的连接点拖拽到另一个房间内的"门"元素上，建立关联关系：

1. **交互方式**：从房间连接点（红点）拖拽，松开在目标房间的"🚪门"元素上即可创建 doorLink。拖拽过程中门元素会高亮发光提示可连接。仅门元素可作为目标，其他房间元素不响应。
2. **虚线可视化**：doorLink 渲染为橙色虚线（`stroke-dasharray: 6,4`），从连接点到门元素位置，末端有小圆点标记门的位置。
3. **选中与删除**：doorLink 可点击选中（蓝色高亮），属性面板显示连接点来源和目标门信息，支持删除按钮和 Delete/Backspace 键删除。
4. **位置联动**：拖拽房间移动、调整大小、拖拽门元素位置时，虚线实时更新。删除房间时自动清理相关 doorLinks。
5. **数据持久化**：doorLinks 数组纳入 JSON 导出/导入、本地保存/加载、画布清空。

### 目的：
让关卡设计师能直观指定"从某个连接进入后，玩家在哪个门元素位置生成"，为项目中 Door spawn point 系统的配置做准备。

### 技术方案：
- 新增 `doorLinks` 全局数组，每条记录 `{fromRoomId, fromDir, targetRoomId, targetDoorIndex}`
- 在 mouseup 的 isConnecting 分支中，优先检测 `document.elementFromPoint` 是否命中 `element-door` 类型的房间元素，命中则创建 doorLink 而非普通连接
- SVG 虚线渲染通过 `renderDoorLinks()` 函数独立管理，与 `renderConnections()` 分离
- 门元素拖拽期间高亮提示通过 CSS 类 `door-link-highlight` 实现（`box-shadow + scale`）
- `selectedDoorLinkIndex` 管理选中状态，与 `selectedConnectionIndex` 和 `selectedRoom` 三者互斥

---

## LevelDesigner 自动生成门元素 + doorLink 功能 — 2026-02-26 01:15

### 修改文件：
- `Tools/LevelDesigner.html`

### 内容简述：
当从房间A拉连接线到房间B创建连接时，自动在目标房间B中生成一个"🚪门"元素并自动创建 doorLink 关联：

1. **自动生成门元素**：创建房间连接时，自动在目标房间内靠近入口方向的边缘处放置门元素（12% 内缩偏移），无需手动放置。
2. **自动创建 doorLink**：门元素生成后，自动创建一条从源连接点到目标门元素的 doorLink 虚线。
3. **位置推算**：`calculateDoorPosition(room, entryDir)` 根据入口方向（west/east/north/south）计算门在目标房间内的合理位置（靠近入口边缘的中点，12% inset）。
4. **删除联动**：删除连接时自动清理关联的 doorLinks（`cleanupDoorLinksForConnection`）。删除连接对（双向）时清理双向 doorLinks。

### 目的：
减少手动配置工作量——创建房间连接后门和 spawn point 自动就位，设计师只需微调位置即可。

### 技术方案：
- 新增 `calculateDoorPosition(room, entryDir)` 函数，基于 entryDir 返回 `{x, y}` 相对于房间的本地坐标
- 在 mouseup 的正常连接创建分支中，`connections.push` 后立即调用 `targetRoom.elements.push({ type: 'door', ... })` 和 `doorLinks.push({...})`
- `deleteSelectedConnection` 和 `deleteConnectionPair` 中新增 `cleanupDoorLinksForConnection(conn)` 调用

---

## LevelDesigner 严重 Bug 修复：mousemove 代码块合并错误 — 2026-02-26 01:03

### 修改文件：
- `Tools/LevelDesigner.html`

### 内容简述：
修复 mousemove 事件处理器中 `isDraggingRoom` 与 `isResizing` 两个代码块被错误合并到同一 if 块内的致命 bug。

### 目的：
修复页面完全无法交互的致命错误（拖拽、选择、连接等所有功能均失效）。

### 技术方案：
- **根因**：之前的编辑不慎将 `isResizing` 的整段逻辑错误地拼接到了 `isDraggingRoom` 块内部，导致 `const dx` / `const dy` / `const div` 在同一块作用域中被重复声明，触发 JavaScript `SyntaxError`，整个 `<script>` 标签无法执行。
- **修复**：将 `isDraggingRoom` 块正确关闭（`}`），并将 resize 代码恢复到独立的 `if (isResizing && selectedRoom) { ... }` 块中。

---

## LevelDesigner 连接线箭头精准接触 + 选中/删除功能恢复 — 2026-02-26 01:23

### 修改文件：
- `Tools/LevelDesigner.html`

### 内容简述：
重新实现连接线的 SVG 箭头渲染（箭头尖端精确接触房间边缘），并恢复之前丢失的连接线选中、删除、属性面板等功能。

### 目的：
1. 修复箭头不接触房间边缘的视觉问题（用户反馈"看着难受"）
2. 恢复在之前编辑过程中意外丢失的连接线交互功能

### 技术方案：
- **SVG marker 箭头**：定义 `<marker>` 元素（红色/蓝色），关键参数 `refX=markerWidth(10)`，使箭头尖端精确定位在 line 终点
- **line 终点缩短**：将 line 终点从房间边缘往回缩 `arrowLen(10px)`，这样 marker 的箭头体自然填充这段距离，尖端正好接触边缘
- **双向连接偏移**：双向连接的两条线做 ±4px 垂直偏移避免重叠
- **hitarea 选中层**：每条连接线额外绘制透明 14px 宽的 hitarea `<line>` 接收鼠标点击
- **连接属性面板**：选中连接后右侧面板显示类型/来源/目标/方向，提供删除按钮
- **Delete/Backspace 快捷键**：支持键盘删除选中的连接线或房间
- **同方向去重**：连接创建时检查是否已存在 A→B 的同方向连接
- **连接列表增强**：选中房间时连接列表标注 (单向)/(双向) 类型，使用 →/←/⟷ 符号

---

## LevelDesigner Bug修复：点击空白处未取消房间选中状态 — 2026-02-26 11:28

### 修改文件：
- `Tools/LevelDesigner.html`

### 内容简述：
修复了点击画布空白区域后，已选中房间的连接点（connection points）仍然显示的问题。

### 目的：
连接点应仅在房间被选中（selected）或鼠标悬浮（hover）时显示，点击空白处取消选中后连接点应隐藏。

### 技术方案：
- **根因**：`canvasArea` 的 click 事件中，判断"点击空白处"的条件仅检查了 `e.target === canvasArea || e.target === gridCanvas`，遗漏了 SVG 层（`connections-svg`）和世界容器（`world`）。由于 DOM 层叠结构为 `canvas-area > grid-canvas + connections-svg + world`，点击空白处时 `e.target` 可能命中 SVG 或 world 元素，导致取消选中逻辑未执行。
- **修复**：将判断条件扩展为 `e.target === canvasArea || e.target === gridCanvas || e.target === svg || e.target === world`，覆盖所有非房间的空白区域元素。

---

## LevelDesigner Bug修复：连接线箭头未接触房间边缘 — 2026-02-26 11:30

### 修改文件：
- `Tools/LevelDesigner.html`

### 内容简述：
修复了连接线箭头尖端与房间视觉边缘之间存在明显间距、未精确接触的问题。

### 目的：
箭头尖端应精确接触房间的视觉边缘（border 外缘），而非停留在 content 边缘内侧。

### 技术方案：
两处根因，两处修复：

1. **连接点坐标未计算 border 偏移**：
   - `getConnectionPointPosition` 返回的坐标基于 `room.x/y/width/height`（content 区域边缘），但房间 DOM 使用 `content-box` 模型 + `border: 2px`，视觉边缘比 content 边缘外扩 2px。
   - **修复**：为四个方向的连接点坐标各加上 `border = 2` 的偏移量（north: `y - 2`, south: `y + height + 2`, east: `x + width + 2`, west: `x - 2`）。

2. **线段被错误缩短导致箭头远离终点**：
   - 原代码将可见线段终点缩短了 `arrowLen = 10px`，而 SVG marker 的 `refX=10` 让箭头尖端锚定在缩短后的线段终点，导致箭头尖端实际位于 `toPos - 10px` 处。
   - **修复**：移除线段缩短逻辑，直接以 `toPos`（房间边缘）为线段终点。marker `refX=markerWidth` 已保证箭头尖端对齐线段终点，箭头体自然向后延伸，无需手动缩短。

---

### LevelDesigner 自动生成门元素 + doorLink 功能实现 — 2026-02-26 12:11

**修改文件：**
- `Tools/LevelDesigner.html`

**内容简述：**
实现了连接线创建时自动在目标房间生成门元素（door）并建立 doorLink 关联的完整功能。

**目的：**
当两个房间通过连接点建立连接时，自动在目标房间的入口方向附近生成一个门元素，并通过橙色虚线（doorLink）将源房间连接点与目标房间门元素关联起来，实现连接→门的自动化工作流。

**技术方案：**

1. **数据结构**：
   - 全局 `doorLinks` 数组，每个元素包含 `{ fromRoomId, fromDir, targetRoomId, targetDoorIndex }`
   - 全局 `selectedDoorLinkIndex` 用于选中交互

2. **CSS 样式**：
   - `.door-link-line`：橙色虚线（stroke-dasharray: 6,4），选中时变蓝变粗
   - `.door-link-hitarea`：14px 宽透明点击区域
   - `.door-link-dot`：橙色小圆点标记门端位置
   - `.room-element.door-link-highlight`：门元素高亮效果（橙色发光+缩放）

3. **核心函数**：
   - `calculateDoorPosition(room, entryDir)`：根据入口方向计算门元素在房间内的位置（12%内缩）
   - `renderDoorLinks()`：SVG 渲染所有 doorLink 虚线、点击区域、端点圆点
   - `selectDoorLink(idx)` / `deselectDoorLink()`：选中/取消选中交互，互斥房间/连接选中
   - `showDoorLinkProperties(idx)`：在属性面板显示 doorLink 详情
   - `deleteSelectedDoorLink()`：删除选中的 doorLink
   - `cleanupDoorLinksForConnection(conn)`：删除连接时联动清理对应的 doorLinks

4. **自动生成逻辑**：
   - 在 mouseup isConnecting 分支中，创建连接后自动调用 `calculateDoorPosition` 在目标房间生成门元素
   - 同时创建 doorLink 将源连接点与新门元素关联

5. **交互支持**：
   - 点击虚线选中 doorLink，属性面板显示详情和删除按钮
   - Delete/Backspace 键删除选中的 doorLink
   - 选中 doorLink/连接/房间三者互斥
   - 删除连接或删除房间时联动清理相关 doorLinks

6. **数据持久化**：
   - `getExportData()` 导出 doorLinks
   - `importJsonData()` 导入 doorLinks 并渲染
   - `saveToLocal()` / `loadFromLocal()` 保存/加载 doorLinks
   - `clearAll()` 清空 doorLinks

7. **实时更新**：
   - 房间拖拽、缩放时同步更新 doorLinks 渲染
   - 画布点击空白处取消 doorLink 选中

---

## LevelDesigner 补全 doorLink 手动关联交互 — 2026-02-26 12:20

### 修改文件：
- `Tools/LevelDesigner.html`

### 内容简述：
补全 doorLink 功能中缺失的两项手动关联交互：

1. **拖拽过程门元素高亮提示**：在 `mousemove` 的 `isConnecting` 分支中，通过 `document.elementFromPoint` 实时检测鼠标下方是否为门元素（`element-door`），命中时添加 `door-link-highlight` CSS class（橙色发光 + scale 1.2），离开时移除。仅对非源房间的门元素响应。
2. **拖拽到已有门元素创建 doorLink**：在 `mouseup` 的 `isConnecting` 分支中，优先检测 `elementFromPoint` 是否命中 `element-door` 类型的房间元素。命中时仅创建 doorLink（不创建普通连接线），含重复检测防止同一源→目标门的重复关联。未命中门元素时走原有逻辑（创建连接线 + 自动生成门 + 自动 doorLink）。

### 目的：
补全 doorLink 功能的手动关联模式——允许用户从连接点拖拽到已有的门元素上建立关联关系，与自动关联模式互补，提供更灵活的关卡设计工作流。

### 技术方案：
- **mousemove 高亮**：每帧 `elementFromPoint` 检测 → 移除旧 `.door-link-highlight` → 命中门元素且非源房间则添加 class
- **mouseup 优先级分支**：Priority 1 检测门元素 → 创建 doorLink only；Priority 2 走原有 `closest('.room')` → 创建连接 + 自动门 + 自动 doorLink
- **防重复**：`doorLinks.some()` 检查 fromRoomId + fromDir + targetRoomId + targetDoorIndex 四元组唯一性
- **清理**：mouseup 开头统一移除残留的 `door-link-highlight` class

---

## LevelDesigner doorLink 语义修正（虚线起点改为目标房间入口连接点） — 2026-02-26 12:27

### 修改文件：
- `Tools/LevelDesigner.html`

### 内容简述：
修正 doorLink 的数据结构与渲染语义，使虚线从目标房间的入口连接点画到房间内的门元素，而非从源房间的连接点画过去。

1. **数据结构重构**：`doorLink` 从 `{fromRoomId, fromDir, targetRoomId, targetDoorIndex}` 改为 `{roomId, entryDir, doorIndex}`，语义变为"从 entryDir 方向进入 roomId 房间 → 出生在门 #doorIndex 位置"。
2. **渲染修正**：`renderDoorLinks()` 起点从 `getConnectionPointPosition(fromRoom, fromDir)` 改为 `getConnectionPointPosition(room, entryDir)`，虚线在同一个房间内部从入口边缘连到门元素。
3. **属性面板更新**：显示"所在房间"、"入口方向"、"门元素序号"三个字段，并新增含义说明文案。
4. **全链路字段名同步**：更新了 doorLinks.push（两处）、renderDoorLinks、showDoorLinkProperties、cleanupDoorLinksForConnection、deleteRoom、importJsonData 共 7 处引用。

### 目的：
doorLink 的正确语义是"从某方向进入此房间后的出生点位置"，虚线应在目标房间内部从入口连接点连到门元素，而不是跨房间从源房间连过来。

### 技术方案：
- 新字段 `roomId` = 门所在房间（即连接的目标房间 B）
- 新字段 `entryDir` = 入口方向（即 `toDir`，`getOppositeDirection(connectFromDir)`）
- 新字段 `doorIndex` = 房间 elements 数组中的门元素索引
- `renderDoorLinks` 中 `fromPos = getConnectionPointPosition(room, dl.entryDir)`，`toPos = room.x + doorEl.x, room.y + doorEl.y`
- `cleanupDoorLinksForConnection` 匹配条件改为 `dl.roomId === conn.to && dl.entryDir === conn.toDir`
- `deleteRoom` 清理条件简化为 `dl.roomId !== room.id`

---

## LevelDesigner 修复 doorLink 手动关联无法命中门元素 — 2026-02-26 12:38

### 修改文件：
- `Tools/LevelDesigner.html`

### 内容简述：
修复从连接点拖拽到另一个房间的门元素上无法建立 doorLink 的问题。

1. **根因**：`document.elementFromPoint` 只返回最顶层元素，SVG 层（`z-index: 5`）和临时连接线覆盖在门元素上方，导致鼠标释放时无法命中 18x18px 的门元素 div。
2. **修复方案**：将 `document.elementFromPoint`（单数）改为 `document.elementsFromPoint`（复数），穿透所有层级查找门元素。
3. **临时连接线屏蔽**：为 `.temp-connection-line` 添加 `pointer-events: none`，确保拖拽线不截获鼠标事件。
4. **高亮检测同步修复**：mousemove 中的门元素高亮检测也从 `elementFromPoint` 改为 `elementsFromPoint`，确保拖拽过程中门元素能正确高亮发光提示。

### 目的：
确保手动关联模式（从连接点拖拽到已有门元素上建立 doorLink）能正常工作。

### 技术方案：
- mouseup 中：`const allTargets = document.elementsFromPoint(e.clientX, e.clientY)`，用 `allTargets.find()` 分别查找 `doorTarget`（门元素优先）和 `target`（房间元素回退）
- mousemove 中：同样使用 `elementsFromPoint` 穿透查找门元素进行高亮
- CSS `.temp-connection-line` 新增 `pointer-events: none`

---

## LevelDesigner 全面代码审查与Bug修复 — 2026-02-26 12:46

### 修改文件：
- `Tools/LevelDesigner.html`

### 内容简述：
对 LevelDesigner.html 进行全面代码审查，修复4个Bug并清理2处冗余代码：

1. **Bug修复 — updateRoomProperty ID同步**：修改房间ID时未同步更新 `doorLinks` 数组中的 `roomId` 引用，导致重命名后 doorLink 虚线断裂。新增 `doorLinks.forEach(dl => { if (dl.roomId === oldId) dl.roomId = value; })` 同步逻辑。
2. **Bug修复 — updateRoomSize 缺少 renderDoorLinks**：通过属性面板手动调整房间尺寸后，连接线会更新但 doorLink 虚线不跟随更新。在 `renderConnections()` 后补充 `renderDoorLinks()` 调用。
3. **Bug修复 — deleteRoom doorLinks 清理不完整**：删除房间时直接过滤 connections，但未先调用 `cleanupDoorLinksForConnection` 清理关联的 doorLinks（那些因连接自动创建在目标房间中的 doorLinks）。改为先遍历相关 connections 调用 cleanup，再 filter 删除。
4. **Bug修复 — loadFromLocal 状态重置缺失**：从本地加载数据后未重置 `selectedRoom`、`selectedConnectionIndex`、`selectedDoorLinkIndex` 和属性面板显示状态，导致加载后可能出现残留选中状态。补充完整的状态重置逻辑。
5. **冗余清理 — getOppositeDirection**：该函数定义后未在任何地方被调用，直接删除。
6. **冗余清理 — Canvas 网格系统**：`<canvas id="grid-canvas">` 及关联的 `gridCanvas`、`ctx`、`drawGrid()`、`resizeCanvas()` 完全冗余——canvas 设置为 `display:none`，网格效果已由 CSS `background-image` 实现。删除全部 canvas 相关代码（HTML元素、CSS规则、JS变量和函数），`applyPan()` 中的 `drawGrid()` 调用替换为 `canvasArea.style.backgroundPosition` 更新以实现网格跟随平移。同步清理 click 事件中对已删除 `gridCanvas` 变量的引用。

### 目的：
确保 LevelDesigner 工具代码逻辑正确、无隐藏Bug、无冗余代码，提升可维护性。

### 技术方案：
- `updateRoomProperty` 的 `id` 分支新增 doorLinks roomId 同步
- `updateRoomSize` 末尾追加 `renderDoorLinks()` 调用
- `deleteRoom` 开头先收集 relatedConns 并逐个 `cleanupDoorLinksForConnection`，再 filter 删除 connections
- `loadFromLocal` 在解析数据后、渲染前重置三个选中状态变量和属性面板 display
- 删除 `getOppositeDirection` 函数定义（4行）
- 删除 canvas 相关代码约40行（`gridCanvas`/`ctx` 变量、`resizeCanvas`/`drawGrid` 函数、`window.resize` 监听、HTML `<canvas>` 元素、CSS `#grid-canvas` 规则）
- `applyPan` 中 `drawGrid()` 替换为 `canvasArea.style.backgroundPosition = \`${panX}px ${panY}px\``

---

## LevelDesigner 房间元素选中与删除功能 — 2026-02-26 13:04

### 修改文件：
- `Tools/LevelDesigner.html`

### 内容简述：
为 LevelDesigner 工具中的房间元素（spawn、door、enemy、chest、checkpoint、npc）添加选中和删除功能：

1. **元素选中状态管理**：新增 `selectedElementRoomId` 和 `selectedElementIndex` 全局变量，`clearElementSelection()` 清除函数。与 `selectedRoom`、`selectedConnectionIndex`、`selectedDoorLinkIndex` 三者互斥——选中任一类型自动取消其他类型的选中状态。
2. **点击选中交互**：在 mouseup 事件中区分元素拖拽与点击（位移 < 5px 判定为点击），点击时调用 `selectElement(roomId, index)` 选中元素。选中元素添加 `.element-selected` CSS 类呈现蓝色边框+发光高亮。
3. **属性面板联动**：`showElementProperties(roomId, elementIndex)` 在属性面板中显示元素类型（带 emoji）、所属房间、坐标位置、序号，并提供"🗑️ 删除元素"按钮。点击画布空白区域时清除选中并恢复默认面板。
4. **元素删除逻辑**：`deleteSelectedElement()` 从房间 `elements` 数组中 splice 移除元素。删除 door 类型元素时自动清理所有匹配的 doorLinks，并修正同房间内后续 doorLinks 的 `doorIndex` 索引偏移。
5. **多触发方式**：支持属性面板删除按钮和 Delete/Backspace 快捷键。键盘删除优先级：元素 → 连接线 → doorLink → 房间。
6. **边界情况**：deleteRoom 时若选中元素属于该房间则自动清除选中；clearAll 和 loadFromLocal 重置元素选中状态变量。

### 目的：
让关卡设计师能够选中查看房间内元素的属性，并通过多种方式删除不需要的元素，包括正确处理门元素删除时的 doorLink 联动清理。

### 技术方案：
- `selectedElementRoomId`（string|null）+ `selectedElementIndex`（int，-1 表示无选中）管理选中状态
- `.element-selected` CSS：`outline: 2px solid #2196F3; box-shadow: 0 0 8px rgba(33,150,243,0.6); transform: scale(1.15)`
- mouseup 中 `Math.hypot(dx, dy) < 5` 区分拖拽与点击
- `selectElement()` 内部调用所有其他 deselect 逻辑确保互斥
- `deleteSelectedElement()` 对 door 类型执行 `doorLinks.filter()` 清除 + `doorLinks.forEach()` 索引修正
- `selectRoom()`、`selectConnection()`、`selectDoorLink()` 入口处调用 `clearElementSelection()`
- keydown 处理中 `selectedElementIndex >= 0` 优先级最高

---

## LevelDesigner.html Bug 修复与功能完善 — 2026-02-26 13:32

### 修改文件：
- `Tools/LevelDesigner.html`

### 内容简述：
对 LevelDesigner 关卡编辑工具进行全面代码审查后，修复了 6 类 Bug 并清理了冗余代码：

1. **importJsonData 元素选中状态重置遗漏**：JSON 导入函数中未重置 `selectedElementRoomId` 和 `selectedElementIndex`，导致导入后可能出现幽灵选中引用。追加了这两个变量的重置以及 `hideConnectionProperties()` 调用。
2. **getExportData() 值拷贝问题**：`connections` 和 `doorLinks` 使用直接引用导出而非值拷贝。改为 `.map()` 创建独立的浅拷贝对象，确保导出数据不受后续编辑污染。
3. **元素位置显示像素 → 网格单位**：`showElementProperties` 中的坐标从 `Math.round(el.x)` 改为 `(el.x / GRID_SIZE).toFixed(1)`，标签更新为"位置(网格)"，与导出 JSON 中的单位一致。
4. **连接属性面板房间名称补全**：`showConnectionProperties` 中"从/到"字段从仅显示房间名改为 `名称 (ID)` 格式，方便快速识别连接关系。
5. **选中状态互斥 — 空白点击修复**：重构画布空白区域 click 事件处理，确保点击空白时：直接重置所有选中状态变量（room/connection/doorLink/element），最后调用 `hideConnectionProperties()` 统一恢复属性面板为默认提示文本。
6. **冗余代码清理**：移除未使用的 `.canvas-area.space-ready` CSS 类；清理 `renderConnections` 中无效的 `.connection-arrow` selector 引用。

### 目的：
消除全面审查中发现的 Bug，确保工具的数据导入导出健壮性、UI 信息一致性、选中状态互斥正确性，以及代码精简可维护。

### 技术方案：
- `importJsonData` 状态重置区域追加 `selectedElementRoomId = null; selectedElementIndex = -1; hideConnectionProperties();`
- `getExportData()` 中 `connections: connections.map(c => ({from, to, fromDir, toDir}))` 和 `doorLinks: doorLinks.map(d => ({roomId, entryDir, doorIndex}))`
- `showElementProperties` 坐标转换 `el.x / GRID_SIZE` 保留一位小数
- `showConnectionProperties` 中 `fromName = fromRoom ? \`${fromRoom.name} (${conn.from})\` : conn.from`
- 画布空白点击：避免调用 `deselectConnection/deselectDoorLink` 中间函数（它们内部会独立调用 hideConnectionProperties），改为直接变量赋值 + 最后统一调用 `hideConnectionProperties()`
- 移除 CSS `.canvas-area.space-ready { cursor: grab; }` 和 JS 中 `.connection-arrow` 无效引用

---

## 关卡设计：示巴星 ACT1+ACT2 银河恶魔城式关卡重构

### 示巴星 ACT1+ACT2 关卡布局全面重构 — 2026-02-26 13:46

**修改文件：**
- `Docs/LevelDesigns/Sheba_ACT1_ACT2.json` — 完全重写

**内容简述：**
将原线性"一本道"关卡（28个房间，全east→west连接）重构为银河恶魔城式网状拓扑布局（38个房间）。覆盖ACT1 Z1a~Z1f + ACT2 Z2a~Z2d共10个心流节点，严格对齐CSV张力曲线4→2→7→4→3→6→5→3→5→9。

**关键设计特性：**
1. **网状拓扑**：多路径分支（Z1a起点提供东向+南向两条路径）、分支汇聚点（Z1e+Z1f双路汇入）
2. **垂直层级**：3个floor层（-1地下/0地面/1上层），Z1b隐藏地穴(floor=-1)、Z1c上层观景台(floor=1)、Z1d音叉林上层(floor=1)、Z2a上层平台(floor=1)
3. **3个环路**：Z1c侧路↔Z1b地穴、Z1d花园↔Z1f下行通道、Z2c回音路↔世界时钟预兆
4. **2条单向捷径**：Z1d悬崖→Z1b可选角落（跌落）、Z2b隐藏跌落→Z2c深谷入口（跌落）
5. **4种房间类型**：safe(16)、normal(18)、arena(3)、boss(1)
6. **6种元素类型**：spawn(1)、checkpoint(13)、enemy(39)、chest(23)、npc(13)、door(4)
7. **84条连接**（大部分双向A↔B）+ **5个doorLinks**
8. **Boss竞技场**：20×14（满足≥18×12要求）

**拓扑概念：**
```
ACT1:
Z1a(坠机点)→东→Z1b(冰雕广场)→东→Z1c(觉醒战场)→东→Z1d(音叉林)→花园
     ↓南                  ↕地穴    ↕侧路↔地穴(环1)     ↕上层  ↓悬崖→Z1b(捷径1)
Z1e(战后余波)→东→Z1f(下行通道)→首棱镜→缕拉目击→ACT1出口
                  ↑ Z1d花园直连(环2)

ACT2:
Z2a(管风琴走廊)→Z2a缕拉援助→↓南→Z2b(声之走廊)→涟漪奖励→缕拉面对面
     ↕上层平台+秘密                   ↓南               ↓隐藏跌落→Z2c(捷径2)
                        Z2c(深谷入口)→↓南→Z2c战斗→Z2c回音路
                             ↕世界时钟←→↕回音路(环3)→频率标记→Z2d前室→Boss→奖励→出口
```

**目的：**
从线性走廊改为银河恶魔城式网状探索，让玩家体验分支选择、垂直层叠、捷径回路和延迟回报。保持CSV心流节奏不变。

**技术方案：**
- JSON格式完全对齐LevelDesigner.html的getExportData()输出：rooms(id/name/type/floor/position/size/elements) + connections(from/to/fromDir/toDir) + doorLinks(roomId/entryDir/doorIndex)
- 双向连接：每条通道2条connection（A→B + B→A）
- 单向连接：捷径/跌落仅1条connection（A→B）
- comment字段用于人类可读的区域分隔标注（导入时被忽略但保留在JSON中）
- 位置坐标使用上下左右四方向展开，非线性排列

---

## HTML Scaffold JSON → LevelScaffoldData 导入工具 — 2026-02-26 14:12

**新建文件：**
- `Assets/Scripts/Level/Editor/HtmlScaffoldImporter.cs` — EditorWindow + 导入管线 + 嵌入式MiniJSON解析器

**内容简述：**
实现了完整的 HTML LevelDesigner JSON → Unity `LevelScaffoldData` ScriptableObject 一键导入脚本。

功能包括：
1. **EditorWindow UI**：菜单入口 `Window > ProjectArk > Import HTML Scaffold JSON`，文件选择（JSON输入 + .asset输出）、网格缩放因子
2. **JSON 解析**：内嵌轻量级递归下降 MiniJSON 解析器（因项目用 .NET Framework 配置级别，System.Text.Json 不可用），自动过滤 comment 对象
3. **房间映射**：HTML `id/name/type/floor/position/size` → `ScaffoldRoom`（RoomType枚举映射 + y轴翻转 + 缩放）
4. **元素映射**：`spawn→PlayerSpawn, enemy→EnemySpawn, checkpoint→Checkpoint, door→Door, chest→CrateWooden(占位), npc→Checkpoint(占位)`，坐标从房间左上角偏移转换为房间中心偏移
5. **连接转换**：全局 `connections[]` → 每个房间内嵌 `ScaffoldDoorConnection`，方向映射 + 房间边缘中点定位 + 跨楼层标记 `IsLayerTransition`
6. **doorLinks 绑定**：Door 元素的 `BoundConnectionID` 绑定到匹配方向的连接
7. **Floor 层级**：多楼层检测、主楼层自动选择、非主楼层房间名称追加 `[F=n]` 标记
8. **Undo 支持**：整个导入操作可 Ctrl+Z 撤销
9. **详尽日志**：Console 摘要（房间/连接/元素/跳过/占位映射/楼层分布）+ 成功对话框 + 自动选中创建的 asset

**目的：**
让策划可以从浏览器端 LevelDesigner.html 的可视化设计无缝导入 Unity，生成可直接用于 LevelArchitectWindow 和 LevelGenerator 的 ScaffoldData asset。

**技术方案：**
- 嵌入式 MiniJSON 递归下降解析器（Dictionary/List模式），兼容 Unity .NET Framework 配置
- SerializedObject 操作 private 字段（`_levelName`, `_floorLevel`）
- 所有公共 API 字段通过 public setter 设置
- 无任何第三方依赖，完全自包含

---

## HtmlScaffoldImporter Bug Fix: SerializedObject 覆盖 _rooms — 2026-02-26 14:57

**修改文件：**
- `Assets/Scripts/Level/Editor/HtmlScaffoldImporter.cs`

**内容简述：**
修复了导入 HTML Scaffold JSON 后生成的 .asset 文件中 `_rooms: []` 为空的关键 Bug。

**原因：**
`ExecuteImport()` 在 Step 2 创建了 `SerializedObject(scaffoldData)` 并设置 `_levelName`，此时 `_rooms` 为空。Step 3-5 通过 C# API（`AddRoom`/`AddConnection`）直接向底层对象添加了所有房间和连接。但 Step 6 调用 `so.ApplyModifiedPropertiesWithoutUndo()` 时，`SerializedObject` 将其初始快照（`_rooms = []`）写回对象，覆盖了所有已添加的房间数据。

**修复方案：**
在 `ApplyModifiedPropertiesWithoutUndo()` 之前插入 `so.Update()` 刷新 `SerializedObject` 的快照，使其同步底层对象的最新数据（包含所有房间）。然后重新设置 `_levelName` 和 `_floorLevel` 两个 SerializedProperty 字段，最后 Apply。

**目的：**
确保导入后的 .asset 文件包含完整的房间、连接和元素数据，可被 Level Architect 和 LevelGenerator 正常使用。

**技术方案：**
- `SerializedObject.Update()` → 重设 SerializedProperty → `ApplyModifiedPropertiesWithoutUndo()` 的正确三步序列化流程

---

## ScaffoldToSceneGenerator — 一键 Scaffold → 可玩关卡生成器 — 2026-02-26 15:30

### 新建文件

| 文件 | 目的 |
|------|------|
| `Assets/Scripts/Level/Editor/ScaffoldToSceneGenerator.cs` | 从 LevelScaffoldData 一键生成完整可玩关卡的 EditorWindow 工具 |

### 内容简述

实现了一个完整的 EditorWindow 工具 `ScaffoldToSceneGenerator`，通过菜单 `Window > ProjectArk > Generate Level From Scaffold` 打开。用户拖入 `LevelScaffoldData` 资产后点击 Generate 按钮，即可一键生成完整的场景关卡。

### 7 阶段生成管线

| 阶段 | 功能 | 关键实现 |
|------|------|----------|
| Phase 1 | Room GameObject 生成 | 遍历 scaffold.Rooms，每房间创建 GO + `Room` + `BoxCollider2D`(trigger) + `CameraConfiner` 子物体（Layer=IgnoreRaycast, `PolygonCollider2D` 匹配房间边界）。通过 `SerializedObject` 设置 `_confinerBounds` 和 `_playerLayer`。 |
| Phase 2 | RoomSO 资产创建与关联 | 为每个房间创建/更新 RoomSO（`Assets/_Data/Level/Rooms/{name}_Data.asset`），设置 `_roomID`、`_displayName`、`_floorLevel`、`_type`，赋值给 Room._data。 |
| Phase 3 | Element 实体化 | PlayerSpawn → 空 GO；EnemySpawn → 空 GO + 收集到列表；Checkpoint → GO + `Checkpoint` 组件 + `BoxCollider2D`(2×2) + `CheckpointSO` 资产；Door → 跳过（Phase 4）；其他 → 占位 GO。最后绑定 EnemySpawn 列表到 `Room._spawnPoints`。 |
| Phase 4 | Door 双向连接 | 用 `HashSet<string>` 去重连接对。每条 connection 创建正向门 + 反向门（各含 `Door` + `BoxCollider2D`(3×3) + SpawnPoint）。通过 `SerializedObject` 设置 `_targetRoom`、`_targetSpawnPoint`、`_isLayerTransition`、`_initialState`、`_playerLayer`。Door 元素的 `_boundConnectionID` 额外叠加 `DoorConfig`（`_initialState`、`_requiredKeyID`、`_openDuringPhases`）。 |
| Phase 5 | Arena/Boss 战斗配置 | Arena/Boss 房间添加 `ArenaController` + `EnemySpawner` 子物体。创建 `EncounterSO`（Arena: 1波×3敌人；Boss: 2波 2+3敌人 delay=1.5s），赋值给 `RoomSO._encounter`。 |
| Phase 6 | Normal 房间战斗 | Normal 房间如有 EnemySpawn → 创建 `EnemySpawner`（不加 ArenaController） + EncounterSO（1波×2敌人），赋值给 RoomSO。无 EnemySpawn 则跳过。 |
| Phase 7 | 验证报告 | Console 输出统计（房间/门/RoomSO/EncounterSO/CheckpointSO 数量）、Arena/Boss 缺 EnemySpawn 警告、6项 TODO checklist。 |

### 技术方案

- **Code-First 模式**：不依赖任何外部 Prefab 或 LevelElementLibrary，所有 Component 通过 `AddComponent<T>()` + `SerializedObject` 配置私有字段
- **Undo 全量支持**：`Undo.SetCurrentGroupName` + try/catch/finally 包裹，异常时 `RevertAllDownToGroup`，完成后 `CollapseUndoOperations`，Ctrl+Z 一键撤销全部
- **双向门去重**：canonical pair key（`min(A,B)|max(A,B)`）+ HashSet 避免重复创建门
- **DoorConfig 叠加**：Phase 4 先基于 connections 生成所有门（默认 Open），再在 `ApplyDoorConfigFromElements` 中根据 element._boundConnectionID 查找对应 Door 覆盖 DoorConfig 字段
- **默认敌人 Prefab**：从 `Assets/_Prefabs/Enemies/Enemy_Rusher.prefab` 加载，不存在则 Warning + null
- **Player Layer 安全**：`LayerMask.GetMask("Player")` 若返回 0 则 Warning 但不中断

### 目的

让关卡设计师从 HTML Scaffold JSON → LevelScaffoldData → 完整可玩关卡，实现全自动化管线。生成的关卡包含完整的房间结构、双向门连接、ArenaController 战斗编排、EnemySpawner 刷怪配置、CheckpointSO 存档点，只需后续补充 Tilemap 绘制和敌人 Prefab 替换即可进入 Play Mode。

---

## ScaffoldToSceneGenerator — Room Layer 修复 — 2026-02-26 17:01

### 修改文件

| 文件 | 修改内容 |
|------|----------|
| `Assets/Scripts/Level/Editor/ScaffoldToSceneGenerator.cs` | `CreateRoomGameObject` 中为 Room GO 设置 Layer = `RoomBounds` |

### 问题

生成的 Room GameObject 使用默认 Layer（Default），导致飞船被卡在房间外无法进入。

### 修复

在 `CreateRoomGameObject` 中，创建 `roomGO` 后立即设置 Layer：

```csharp
int roomBoundsLayer = LayerMask.NameToLayer("RoomBounds");
if (roomBoundsLayer >= 0)
    roomGO.layer = roomBoundsLayer;
else
    Debug.LogWarning(...); // 提示用户在 Project Settings 中添加 RoomBounds Layer
```

### 前提

项目 Tags and Layers 中必须存在名为 **`RoomBounds`** 的 Layer，否则生成时会输出 Warning 并回退到 Default Layer。

---

## BugFix: ScaffoldToSceneGenerator CameraConfiner 实体碰撞阻挡飞船 — 2026-02-26 22:42

### 修改文件

- `Assets/Scripts/Level/Editor/ScaffoldToSceneGenerator.cs`

### 问题描述

通过 `ScaffoldToSceneGenerator` 生成的关卡（示巴星 ACT1+ACT2）中，飞船进入任意 Room 时被卡在房间边缘，无法正常进入。旧版 `ShebaLevelScaffolder` 生成的 Sheba Level 正常。

### 根因

`CreateRoomGameObject` 方法中，`CameraConfiner` 子对象的 `PolygonCollider2D` 被设置为 `isTrigger = false`（实体碰撞体）。`Ignore Raycast`（Layer 2）只忽略射线检测，不忽略 Physics2D 碰撞，导致该实体 PolygonCollider2D 像一堵墙一样包围整个房间，阻止飞船进入。

### 修复

将 `CreateRoomGameObject` 中 `PolygonCollider2D` 的 `isTrigger` 从 `false` 改为 `true`：

```csharp
// Before
polyCol.isTrigger = false;

// After
polyCol.isTrigger = true;
```

### 影响范围

仅影响 `ScaffoldToSceneGenerator` 生成的新关卡。已生成的场景需手动将各 Room 下 `CameraConfiner/Po
lygonCollider2D` 的 Is Trigger 勾选为 true，或重新生成关卡。

---

## Feature: ScaffoldToSceneGenerator 元素可视化 Gizmo — 2026-02-26 23:04

### 修改文件

- `Assets/Scripts/Level/Editor/ScaffoldToSceneGenerator.cs`

### 内容简述

为 `ScaffoldToSceneGenerator` 生成的每个房间元素 GO 添加编辑期可视化：彩色 `SpriteRenderer` 色块 + `TextMesh` 文字标签，方便关卡设计师在 Scene 视图中直观识别各元素类型和位置。

### 目的

当前生成的元素 GO（`PlayerSpawn`、`EnemySpawn`、`Wall` 等）在 Scene 视图中是空 GameObject，无任何视觉表示，开发者无法直观区分。

### 技术方案

1. **新增 `GetElementGizmoColor(ScaffoldElementType)`**：静态辅助方法，按元素类型返回对应 `Color`（黄/红/绿/灰/深灰/橙/蓝灰/紫/青，其余白色）。

2. **新增 `AddGizmoVisuals(GameObject, string, Color)`**：通用辅助方法，在目标 GO 上：
   - 添加 `SpriteRenderer`，使用 `Resources.GetBuiltinResource<Sprite>("Sprites/Default")` 内置白色方块 Sprite，设置颜色与 `sortingOrder = 1`
   - 创建子 GO `"Label"`，添加 `TextMesh`，设置文字内容、`fontSize=12`、`anchor=MiddleCenter`、`alignment=Center`、白色字体、`localPosition=(0,0.6,0)`、`localScale=(0.1,0.1,0.1)`，`MeshRenderer.sortingOrder = 2`

3. **修改 `CreateElementGO()`**：增加可选参数 `ScaffoldElementType type` 和 `string labelOverride`，方法末尾调用 `AddGizmoVisuals()`；更新 Phase 3 中所有调用处传入对应 `elem.ElementType`。

4. **Phase 4 Door/SpawnPoint 可视化**：在 `GenerateDoors()` 中，对 `Door_to_XXX` GO 添加青色可视化，对 `SpawnPoint_from_XXX` GO 添加浅蓝色可视化。

5. **TODO Checklist 提示**：在 `Generate()` 末尾的 Console 输出中追加提示，提醒开发者发布前隐藏/删除 Gizmo 可视化组件。

---

## ScaffoldToSceneGenerator — Door/SpawnPoint Gizmo 尺寸匹配碰撞体 — 2026-02-26 23:18

**修改文件：**
- `Assets/Scripts/Level/Editor/ScaffoldToSceneGenerator.cs`

**内容简述：**
为 `AddGizmoVisuals()` 增加可选 `Vector2 size` 参数，使 SpriteRenderer 色块的世界空间大小可配置，并在 Door 和 SpawnPoint 的调用处传入对应尺寸。

**目的：**
让开发者在 Scene 视图中能直观看到 Door 触发区域的实际范围（3×3 单位），知道飞船何时进入了传送检测区域。

**技术方案：**
1. `AddGizmoVisuals(go, label, color, size)` 新增 `size` 参数（默认 `Vector2.one`），通过设置 `go.transform.localScale = (size.x, size.y, 1)` 让 SpriteRenderer 色块与碰撞体等大。
2. Label 子 GO 的 `localPosition.y` 和 `localScale` 做反向补偿（除以 parent scale），保证文字大小不随色块缩放而变形。
3. Door GO 调用时传入 `new Vector2(3f, 3f)`，与 `BoxCollider2D.size = (3,3)` 完全对齐。
4. SpawnPoint GO 调用时传入 `new Vector2(1.5f, 1.5f)`，提供清晰的落点可视范围。

---

## LevelDesigner.html 效率升级（7大功能） — 2026-02-27 00:31

**修改文件：**
- `Tools/LevelDesigner.html`

**内容简述：**
对浏览器端关卡可视化编辑工具进行全面效率升级，新增7项功能，覆盖数据扩展、类型扩展、快捷键、可视化辅助和向后兼容修复。

**目的：**
核心诉求：方便、快捷、尽量一步到位，减少整个制作关卡流程中琐碎的步骤。让设计师在 LevelDesigner.html 中完成更多配置，减少在 Unity 中手动补充的工作量。

**技术方案：**

1. **房间属性扩展（任务1）**：`createRoom()` 新增 `zoneId`、`act`、`tension`、`beatName`、`timeRange` 字段；属性面板新增对应输入控件（Zone ID 文本框、ACT 下拉 ACT1-ACT5、张力滑块 1-10、叙事乐章文本框、时长估算文本框）；`getExportData()` 仅在字段非空时输出对应 key，保持 JSON 简洁。

2. **类型扩展（任务2）**：新增房间类型 `narrative`（蓝紫色）、`puzzle`（青色）、`corridor`（灰色）；新增元素类型 `tidal_door`（潮汐门）、`resonance_crystal`（共鸣水晶）、`lore_audio`（Lore 音频日志）、`black_water`（黑水区域）；更新 `getTypeLabel()` 和 `getElementIcon()` 函数及对应 CSS 样式。

3. **快捷键支持（任务3）**：实现 `undoStack`（最大50条）+ `pushUndoState()` 快照机制，在所有状态变更前保存快照；`undo()` 恢复快照并重新渲染；`duplicateRoom()` 深拷贝选中房间（含所有属性和元素，不含连接关系），ID 自动添加 `_copy` 后缀；`keydown` 事件支持 `Ctrl+Z`/`Ctrl+D`/`Ctrl+S`/`Escape`，焦点在输入框时不触发。

4. **ACT 分组框（任务4）**：`ACT_COLORS` 常量定义5个 ACT 的半透明颜色；`renderActGroups()` 按 ACT 分组计算包围盒（+20px padding），动态创建 `.act-group-box` div（z-index 低于房间），左上角显示 ACT 标签；房间数 >50 时节流（16ms）；在拖拽移动、属性修改、导入、撤销、创建/删除房间后均调用。

5. **心流张力折线图（任务5）**：右侧面板新增 `<canvas>` 元素（高度80px）；`renderTensionChart()` 收集有 `tension > 0` 的房间，按 Zone ID 字母数字排序（Z1a < Z1b < Z2a），绘制折线图；节点显示 Zone ID 标签和张力数值；相邻节点张力差 >4 时连线高亮为橙色；每次 `updateAsciiPreview()` 末尾自动调用。

6. **连接线语义类型（任务6）**：`connections` 对象新增 `connectionType` 字段（默认 `normal`）；选中连接线时属性面板显示类型下拉（`normal`/`tidal`/`locked`/`one_way`）；`renderConnections()` 根据类型选择对应 SVG stroke 颜色（tidal=蓝绿、locked=黄色、one_way=橙色）；`getExportData()` 输出 `connectionType` 字段。

7. **向后兼容修复（任务7）**：`importJsonData()` 补充重置 `selectedElementRoomId`/`selectedElementIndex`；导入时为旧格式 JSON 的 room 补全新字段默认值（`zoneId: ''`、`act: ''`、`tension: 0` 等）；connections 导入时补全 `connectionType: 'normal'`；`getExportData()` 中 connections/doorLinks 改为值拷贝（`.map()` 展开字段）。

---

## ScaffoldToSceneGenerator — Asset 按 LevelName 分文件夹存放 — 2026-02-27 00:47

**修改文件：**
- `Assets/Scripts/Level/Editor/ScaffoldToSceneGenerator.cs`

**内容简述：**
移除三个固定常量目录（`ROOM_DATA_DIR`/`ENCOUNTER_DIR`/`CHECKPOINT_DIR`），改为在 `Generate()` 开始时根据 `_scaffold.LevelName` 动态计算每次生成的专属子文件夹路径，并存入实例字段供各阶段方法使用。

**目的：**
每次生成不同关卡时，所有产出的 RoomSO、EncounterSO、CheckpointSO 资产都自动归入 `Assets/_Data/Level/{LevelName}/` 下的对应子目录，避免多关卡资产混放在同一扁平目录中造成混乱，方便管理和版本控制。

**技术方案：**
- 删除 `ROOM_DATA_DIR`、`ENCOUNTER_DIR`、`CHECKPOINT_DIR` 三个 `const string` 常量
- 新增 `LEVEL_DATA_ROOT = "Assets/_Data/Level"` 根目录常量
- 新增三个实例字段 `_roomDataDir`、`_encounterDir`、`_checkpointDir`
- 在 `Generate()` try 块最开始，用 `SanitizeName(_scaffold.LevelName)` 计算安全名称，拼出三条路径并调用 `EnsureDirectory()` 确保目录存在
- `CreateOrUpdateRoomSO()`、`CreateCheckpointElement()`、`SetupArenaBossCombat()`、`SetupNormalRoomCombat()` 中的路径字符串均改为引用对应实例字段

生成结果示例（LevelName = "示巴星_Z1"）：
```
Assets/_Data/Level/示巴星_Z1/
  Rooms/       ← RoomSO assets
  Encounters/  ← EncounterSO assets
  Checkpoints/ ← CheckpointSO assets
```

---

## ScaffoldToSceneGenerator — 效率升级（需求2-8）— 2026-02-27 01:15

**修改文件：**
- `Assets/Scripts/Level/Editor/ScaffoldToSceneGenerator.cs`

**内容简述：**
对 ScaffoldToSceneGenerator 进行 6 项效率升级，消灭生成后仍需手动完成的琐碎步骤。

**目的：**
让关卡设计师在一键生成后能直接进入绘制/调试阶段，减少重复手动操作。

**技术方案：**

1. **需求8 — SanitizeName 空格处理 + 路径预览**
   - `SanitizeName()` 追加 `.Replace(" ", "_")` 处理空格字符
   - `OnGUI()` 在 Scaffold Data 字段下方用 `EditorGUILayout.HelpBox` 显示 `Output: Assets/_Data/Level/{SanitizedName}/`，`_scaffold` 为 null 时不显示

2. **需求2 — 房间尺寸 Fallback 与异常警告**
   - 新增 `GetFallbackSize(RoomType)` 方法，按类型返回默认尺寸（Normal=20×15，Arena=30×20，Boss=40×30，Corridor=20×8，Shop=15×12）
   - `Generate()` 遍历房间时检测 `Size.x <= 0 || Size.y <= 0`，自动 fallback 并输出 `Debug.LogWarning`
   - 生成报告 TODO Checklist 中列出所有使用了 fallback 的房间名

3. **需求3 — Door 位置自动推算（边缘吸附）**
   - 新增 `ResolveDoorPosition(ScaffoldDoorConnection, Vector2)` 方法，根据 `DoorDirection` 返回房间边缘局部坐标
   - `DoorPosition == Vector3.zero` 时自动调用；已有非零值则直接使用不覆盖
   - `DoorDirection` 为 None 时保留原 fallback 并输出 Warning

4. **需求4 — 自动创建标准 Tilemap 层级**
   - 新增 `CreateTilemapHierarchy(GameObject)` 方法，在房间 GO 下创建 `Tilemaps` 子对象
   - 自动创建三层：`Tilemap_Ground`（sortingOrder=0）、`Tilemap_Wall`（sortingOrder=1）、`Tilemap_Decoration`（sortingOrder=2），每层附加 `Tilemap` + `TilemapRenderer` 组件
   - `CreateRoomGO()` 末尾自动调用；生成报告提示"直接选中对应层开始绘制"

5. **需求5 — EncounterSO 按 EnemyTypeID 自动填充敌人 Prefab**
   - `ScaffoldElement` 新增 `EnemyTypeID`（string）字段
   - `CreateEncounterSO()` 收集房间内所有 EnemySpawn 的 `EnemyTypeID`，从 `Assets/_Prefabs/Enemies/{EnemyTypeID}.prefab` 加载 Prefab，找不到则 fallback 到 `Enemy_Rusher.prefab` 并输出 Warning
   - 同一 Wave 中为每种类型创建独立 Entry；生成报告列出每个房间的敌人类型汇总

6. **需求6 — 增量更新模式（Update Existing 复选框）**
   - EditorWindow 新增 `_updateExisting` bool 字段，Generate 按钮旁渲染 Toggle
   - `Update Existing = true` 时：按 DisplayName 查找已存在的同名 GO，存在则跳过重建，仅更新 RoomSO 引用、BoxCollider2D.size、Door 组件属性，保留 Tilemap 子层级
   - 增量更新完成后 Console 输出被保留的房间名称列表

7. **需求7 — Gizmo 统一开关（Toggle Gizmos 按钮）**
   - EditorWindow 新增 `_gizmosVisible` bool 字段（默认 true），Generate 按钮下方渲染 Toggle Gizmos 按钮
   - 新增 `ToggleGizmos()` 方法：遍历场景中所有 `SpriteRenderer`（sortingOrder==1）和名为 `Label` 的 `MeshRenderer`，统一设置 `enabled`
   - 按钮文字根据状态动态显示 `Hide Gizmos` / `Show Gizmos`；场景中无 Gizmo 对象时输出提示

---

## ScaffoldToSceneGenerator — 所有元素 Gizmo Sprite 匹配碰撞体大小 — 2026-02-26 23:24

**修改文件：**
- `Assets/Scripts/Level/Editor/ScaffoldToSceneGenerator.cs`

**内容简述：**
新增 `GetElementGizmoSize()` 辅助方法，统一管理各元素类型的 Gizmo sprite 尺寸；修复 `CreateElementGO` 和 `CreateCheckpointElement` 未传 size 导致 sprite 始终为 1×1 的问题。

**目的：**
让一键生成的所有元素（Door、Checkpoint、PlayerSpawn、EnemySpawn 等）的 SpriteRenderer 色块在 Scene 视图中与其 BoxCollider2D 触发区域完全重叠，方便开发者直观判断检测范围。

**技术方案：**
新增 `GetElementGizmoSize(ScaffoldElementType)` 方法，返回各类型对应的 `Vector2` 尺寸：
- `Door` → `(3, 3)`，匹配 `BoxCollider2D.size = (3,3)`
- `Checkpoint` → `(2, 2)`，匹配 `BoxCollider2D.size = (2,2)`
- `PlayerSpawn` / `EnemySpawn` → `(1.5, 1.5)`，无碰撞体但提供清晰落点范围
- 其余 → `(1, 1)` 默认

`CreateElementGO` 调用 `AddGizmoVisuals` 时自动传入 `GetElementGizmoSize(type)`；`CreateCheckpointElement` 也在添加 BoxCollider2D 后立即调用 `AddGizmoVisuals` 并传入 `(2,2)`。

---

## LevelDesigner — 自定义元素功能 — 2026-02-27 09:30

**修改文件：**
- `Tools/LevelDesigner.html`

**内容简述：**
在左侧面板新增「自定义元素」区块，允许设计师自由定义名称和颜色，创建自定义元素预设并拖入画布使用，支持 localStorage 持久化、属性面板编辑、JSON 导出/导入向后兼容。

**目的：**
满足设计师标注项目特有自定义标记点（特殊触发器、剧情锚点、测试标记等）的需求，无需修改代码即可扩展元素类型。

**技术方案：**
1. **数据结构**：新增 `customElementPresets` 数组（每项含 `label`、`color`），通过 `saveCustomPresets()` / `loadCustomPresets()` 与 `localStorage` 同步（key: `leveldesigner_custom_elements`）
2. **HTML/CSS**：左侧面板底部新增区块，含名称输入框、颜色选择器（`<input type="color">`）、添加按钮；自定义预设项样式使用动态颜色（左侧色条 + 半透明背景），悬停显示 × 删除按钮
3. **拖拽**：自定义预设项通过 `dataTransfer` 携带 `elementType=custom`、`customLabel`、`customColor`；`addElementToRoom` 扩展参数接收并存入元素对象
4. **渲染**：`renderRoom` 中 `type === 'custom'` 时使用 `customColor` 作为背景色，显示截断后的 `customLabel`（超3字符加省略号），CSS 类 `.element-custom` 使用圆角矩形区别于内置圆形图标
5. **属性面板**：`showElementProperties` 对 custom 类型额外渲染名称输入框和颜色选择器，`updateCustomElementField()` 实时更新元素数据并刷新画布图标
6. **JSON 兼容**：`getExportData` 对 custom 元素输出 `customLabel`/`customColor` 字段；`importJsonData` 反序列化时向后兼容（旧数据无 custom 字段时不报错）
7. **辅助函数**：新增 `hexToRgba(hex, alpha)` 将十六进制颜色转 rgba，`escapeHtml(str)` 防 XSS

---

## BugFix: DialogueLine 序列化深度超限修复 — 2026-02-27 09:07

**修改文件：**
- `Assets/Scripts/SpaceLife/Data/DialogueData.cs`
- `Assets/Scripts/SpaceLife/Data/NPCDataSO.cs`
- `Assets/Scripts/SpaceLife/NPCController.cs`
- `Assets/Scripts/SpaceLife/DialogueUI.cs`

**问题描述：**
Unity 报错 `Serialization depth limit 10 exceeded at DialogueLine._speakerAvatar`，原因是 `DialogueLine` 内联包含 `List<DialogueOption>`，`DialogueOption` 又内联包含 `DialogueLine`，形成无限递归序列化循环。

**技术方案：**
1. **DialogueData.cs**：`DialogueOption` 移除 `DialogueLine _nextLine` 字段，改为 `int _nextLineIndex`（-1 表示结束对话），彻底打破序列化循环
2. **NPCDataSO.cs**：将原三个独立 `List<DialogueLine>`（default/friendly/bestFriend）改为统一的 `List<DialogueLine> _dialogueNodes` 节点池，加入三个入口 index 字段（`_defaultEntryIndex`/`_friendlyEntryIndex`/`_bestFriendEntryIndex`），提供 `GetEntryLine(int relationship)` 和 `GetNodeAt(int index)` 辅助方法
4. **NPCController.cs**：`GetAppropriateDialogue` 改为调用 `_npcData.GetEntryLine(CurrentRelationship)`，移除冗余的 `using System.Linq`
4. **DialogueUI.cs**：`ShowDialogue` 缓存 `_currentNPCData`，`OnOptionSelected` 通过 `_currentNPCData.GetNodeAt(option.NextLineIndex)` 查找下一行，`CloseDialogue` 清空 `_currentNPCData`

---

## BugFix: Door 碰撞体过大 + 反复横跳修复 — 2026-02-27 09:19

**修改文件：**
- `Assets/Scripts/Level/Editor/ScaffoldToSceneGenerator.cs`
- `Assets/Scripts/Level/Room/Door.cs`

**问题描述：**
ScaffoldToSceneGenerator 生成的 Door BoxCollider2D 尺寸为 3×3，对于走廊等小房间几乎占满整个通道，导致玩家进入房间后立刻触发传送。同时，传送落点若恰好在目标房间反向门的碰撞体内，`OnTriggerStay2D` 会立刻再次触发反向传送，造成反复横跳。

**技术方案：**
1. **ScaffoldToSceneGenerator.cs**：将所有 Door 的 `BoxCollider2D.size` 从 `(3f, 3f)` 缩小至 `(1.5f, 1.5f)`，同步更新 Gizmo 可视化尺寸和 `GetDefaultGizmoSize` 中的 Door 默认值（共 5 处）
2. **Door.cs**：新增 `static float _globalTransitionCooldownUntil` 静态冷却时间戳，所有 Door 实例共享；`TryTransition` 的回调中设置 `_globalTransitionCooldownUntil = Time.unscaledTime + 1f`；`OnTriggerStay2D` 中加入冷却检查，冷却期间跳过 Stay 触发，防止目标房间反向门立刻反向传送玩家

---

## Bug Fix: EnemySpawner 一键生成后不产出敌人 — 2026-02-27 10:30

**修改文件：**
- `Assets/Scripts/Level/Editor/ScaffoldToSceneGenerator.cs`
- `Assets/Scripts/Level/Room/RoomManager.cs`

**问题描述：**
一键生成关卡后进入 Play Mode，所有房间（Arena/Boss/Normal）的 EnemySpawner 均不产出敌人。

**根本原因（双重 Bug）：**

**Bug 1（主因）：EncounterSO `_waves` 数组为空**
`CreateEncounterSO` 和 `CreateEncounterSO_Normal` 方法在 `AssetDatabase.CreateAsset()` 创建新资产后，通过 `SerializedObject` 写入 `_waves` 数据，但只调用了 `ApplyModifiedPropertiesWithoutUndo()` 而**没有调用 `EditorUtility.SetDirty(so)`**。导致修改只存在于内存中，`AssetDatabase.SaveAssets()` 时无法识别该资产为脏，数据不写入磁盘，最终 `.asset` 文件中 `_waves: []`。
- `HasEncounter` 返回 `false`（`WaveCount == 0`），`ArenaController.BeginEncounter()` 直接 return
- `Room.ActivateEnemies()` 虽然调用了 `StartWaveEncounter()`，但 `WaveSpawnStrategy` 因 `WaveCount == 0` 立刻触发 `OnEncounterComplete`，房间被标记为 Cleared

**Bug 2（次因）：Arena/Boss 房间双重启动策略冲突**
`RoomManager.EnterRoom()` 对 Arena/Boss 房间同时调用了 `_currentRoom.ActivateEnemies()`（创建策略 A 并立即 StartStrategy）和 `arena.BeginEncounter()`（调用 `SetStrategy(B)`，内部先 `Reset()` 策略 A，CancellationToken 被取消）。策略 A 的异步任务被取消，敌人消失。

**技术方案：**
1. **ScaffoldToSceneGenerator.cs**：在 `CreateEncounterSO` 和 `CreateEncounterSO_Normal` 的 `ApplyModifiedPropertiesWithoutUndo()` 后添加 `EditorUtility.SetDirty(so)`，确保 `_waves` 数据在 `SaveAssets()` 时正确持久化
2. **RoomManager.cs**：将 `ActivateEnemies()` 移入 `else` 分支，Arena/Boss 房间完全交由 `ArenaController.BeginEncounter()` 管理，不再提前启动策略

**操作说明：**
修复后需重新一键生成关卡（旧的 `_waves: []` 资产需重新生成才能获得正确数据）。

---

## Bug Fix: LevelDesigner.html 拖拽功能全面修复 — 2026-02-27 11:20

**修改文件：**
- `Tools/LevelDesigner.html`

**问题描述：**
LevelDesigner.html 中所有拖拽操作完全失效——无论是从左侧面板拖拽房间类型到画布，还是拖拽元素到房间内，均无法触发 drop 事件。

**根本原因（3 个 Bug）：**

**Bug 1（主因）：`.room` 元素缺少 `dragover` 的 `preventDefault()`**
HTML5 Drag & Drop API 要求 drop 目标的所有祖先元素在 `dragover` 事件中调用 `e.preventDefault()`，否则浏览器认为该区域不接受 drop。`canvasArea` 和 `world` 已有 `dragover` 监听，但 `.room` 元素（`world` 的子元素）没有。当鼠标拖拽到 `.room` 上方时，`.room` 成为最顶层命中元素，其默认行为阻止了 drop，导致 `canvasArea` 的 `drop` 事件不触发。

**Bug 2（次因）：`mousedown` 注释不清晰，`e.preventDefault()` 位置需说明**
`setupRoomEvents` 中 `mousedown` 末尾的 `e.preventDefault()` 仅在房间拖动路径执行（其他分支均提前 return），不影响 HTML5 drag & drop 流程，但缺少注释说明，容易误判为干扰源。

**Bug 3（精度问题）：`drop` 事件中目标房间检测依赖坐标范围计算**
当拖拽图标有偏移时，world-space 坐标可能偏移，导致 `rooms.find()` 找不到正确的目标房间。

**技术方案：**
1. **`setupRoomEvents` 添加 `dragover` 监听器**：在函数开头为 `div` 添加 `div.addEventListener('dragover', (e) => { e.preventDefault(); })`，确保整条冒泡链路（`.room` → `world` → `canvasArea`）全部允许 drop
2. **`mousedown` 添加注释**：明确说明 `e.preventDefault()` 仅用于防止文本选中，不调用 `stopPropagation`，不干扰 HTML5 drag 事件冒泡
3. **`drop` 事件精确命中检测**：将目标房间检测替换为 `document.elementFromPoint(e.clientX, e.clientY)` + `closest('.room')` 精确命中，坐标范围计算作为回退方案

---

## Bug Fix: LevelDesigner.html 拖拽功能修复 v3（#connections-svg 拦截 Drag 事件）— 2026-02-27 11:42

**修改文件：**
- `Tools/LevelDesigner.html`

**问题描述：**
上轮修复后拖拽仍然失效。从左侧面板拖拽房间类型/元素到画布，drop 事件依然不触发。

**根本原因：**
`#connections-svg`（`position: absolute; width: 100%; height: 100%; z-index: 5`）覆盖了整个画布区域，成为浏览器 drag 命中测试的最顶层元素。

关键误解：CSS `pointer-events: none` **只对鼠标事件（click/mousemove 等）有效，对 HTML5 Drag & Drop 事件（dragover/drop）完全无效**。

因此拖拽链路断裂：
1. 浏览器 drag 命中 → `#connections-svg`（z-index:5）
2. `#connections-svg` 没有 `dragover` 的 `e.preventDefault()`
3. 浏览器认为该区域不接受 drop → `canvasArea` 的 `drop` 事件永远不触发

历史上 `gridCanvas` 存在时，`gridCanvas` 是顶层元素，它通过 `canvasArea` 的冒泡获得了 `preventDefault()`，所以拖拽正常。删除 `gridCanvas` 后，SVG 成为顶层，问题暴露。

**技术方案：**
在 `canvasArea` 和 `world` 的 `dragover` 监听器旁边，添加一行：
```js
svg.addEventListener('dragover', (e) => { e.preventDefault(); });
```

**完整 dragover 链路（修复后）：**
`preset-item` → `#connections-svg`（新增）→ `#world` → `.room` → `canvasArea`

**验收：**
- `canvasArea`（第 1371 行）✅
- `world`（第 1372 行）✅
- `svg`（第 1373 行）✅ 新增
- `.room` in `setupRoomEvents`（第 1502 行）✅

---

## LevelDesigner Bug修复：点击空白处未取消房间选中状态（回归修复） — 2026-02-27 12:49

### 修改文件：
- `Tools/LevelDesigner.html`

### 内容简述：
修复了点击画布空白区域后，已选中房间的连接点（connection points）仍然显示的问题（回归 bug）。

### 目的：
连接点应仅在房间被选中（selected）或鼠标悬浮（hover）时显示，点击空白处取消选中后连接点应隐藏。

### 技术方案：
- **根因**：`canvasArea` 的 click handler 使用**白名单模式**检查 `e.target`（仅匹配 `canvasArea`、`svg`、`world` 三个元素）。但 `#world` 的 CSS 为 `width: 0; height: 0`（作为 transform 定位容器），导致其永远不会成为 `e.target`。此外，SVG 内部子元素（如 hitarea 线条附近区域、marker polygon 边缘）也可能成为 `e.target`，无法被白名单覆盖。
- **修复**：改用**黑名单模式**——使用 `e.target.closest('.room')` 检查点击是否在房间内部。只要点击目标不在 `.room`、`.toolbar`、`.instructions` 内，就执行取消选中逻辑。这种方式更健壮，不受 DOM 结构变化影响。

---

## LevelDesigner 优化：DoorLink虚线指向门元素icon正中心 — 2026-02-27 13:30

### 修改文件：
- `Tools/LevelDesigner.html`

### 内容简述：
修复了连接点（connection point）到门元素（door element）之间的虚线（doorLink dashed line）终点未指向门元素icon正中心的问题。

### 目的：
提升视觉美观度，让虚线精确地指向门元素icon的中心而非其左上角。

### 技术方案：
- **根因**：`renderDoorLinks` 函数中，虚线终点坐标 `(toX, toY)` 使用 `room.x + doorEl.x` / `room.y + doorEl.y`，这对应门元素 div 的**左上角**而非视觉中心。
- **修复**：加上元素尺寸的中心偏移。`.room-element` CSS 为 `width: 18px; height: 18px; border: 2px`，总显示尺寸 22×22px，中心偏移为 11px。在 `toX` 和 `toY` 上各加 `ELEMENT_CENTER_OFFSET = 11`。

---

## 门元素位置 → SpawnPoint 管线打通 — 2026-02-27 15:14

### 新建文件：
- 无

### 修改文件：
- `Assets/Scripts/Level/Data/LevelScaffoldData.cs`
- `Tools/LevelDesigner.html`
- `Assets/Scripts/Level/Editor/HtmlScaffoldImporter.cs`
- `Assets/Scripts/Level/Editor/ScaffoldToSceneGenerator.cs`

### 内容简述：
实现了门元素位置自动导出为 SpawnOffset，并在一键生成场景时将其应用为玩家出生点（SpawnPoint）位置的完整管线。

### 目的：
此前 SpawnPoint 始终生成在房间边缘中点（`DoorPosition`），不受 LevelDesigner 中门元素实际位置影响。本次修改使 LevelDesigner 中精心摆放的门元素位置能直接决定穿过该门后的出生位置，实现"设计即所得"。

### 技术方案：
1. **`LevelScaffoldData.cs` — `ScaffoldDoorConnection` 新增字段**：添加 `[SerializeField] private Vector3 _spawnOffset` 和公开属性 `SpawnOffset`（get/set）。默认值 `Vector3.zero`，确保旧 .asset 文件向后兼容。
2. **`LevelDesigner.html` — 导出 doorLink 附带 spawnOffset**：修改 `getExportData()` 中 `doorLinks.map()` 逻辑，通过 `doorLink.doorIndex` 在对应房间的 `elements[]` 中查找门元素，将其 `[x / GRID_SIZE, y / GRID_SIZE]` 写入 `spawnOffset` 字段。若门元素不存在或 doorIndex 无效则不包含该字段。
3. **`HtmlScaffoldImporter.cs` — 解析 spawnOffset 并坐标转换**：
   - `HtmlDoorLink` 类新增 `float[] SpawnOffset` 字段。
   - JSON 解析时检查 `spawnOffset` 数组并转为 `float[]`。
   - Step 5 doorLinks 处理循环中，当 `SpawnOffset` 非空时执行 HTML→Unity 坐标转换（左上角原点→房间中心原点、y 轴翻转、乘以 gridScale），赋值给 `matchingConn.SpawnOffset`。
   - 新增 `spawnOffsetApplied` 计数器，导入日志中报告应用数量。
4. **`ScaffoldToSceneGenerator.cs` — Phase 4 使用 SpawnOffset 生成 SpawnPoint**：
   - 提前查找 `reverseConn`（原在 WireDoor 前才查找），复用于 SpawnOffset 检查和 IsLayerTransition。
   - **正向 SpawnPoint（fwdSpawn，target 房间内）**：优先使用 `reverseConn.SpawnOffset`（若非零），否则回退 `FindReverseDoorPosition` 边缘中点逻辑。
   - **反向 SpawnPoint（revSpawn，source 房间内）**：优先使用 `conn.SpawnOffset`（若非零），否则回退 `conn.DoorPosition` 边缘中点逻辑。
   - 使用自定义 SpawnOffset 时输出日志。
5. **向后兼容**：旧 JSON（无 spawnOffset 字段）正常导入，SpawnOffset 默认为零向量，回退到旧的边缘中点逻辑。旧 .asset 文件中新字段自动为零向量。

---

## Bug 修复：GameObjectPool 已销毁池化实例引发 MissingReferenceException — 2026-02-27 16:15

### 修改文件：
- `Assets/Scripts/Core/Pool/GameObjectPool.cs`
- `Assets/Scripts/SpaceLife/SpaceLifeManager.cs`

### 内容简述：
修复了 `GameObjectPool.Get()` 中当池化实例被外部销毁时（如场景卸载而 PoolManager 因 DontDestroyOnLoad 仍存活）抛出 `MissingReferenceException` 的问题。同时在 `SpaceLifeManager.ReturnPlayerToPool()` 中添加了防御性空检查，防止将已销毁对象归还对象池。

### 目的：
防止对象池实例被外部销毁后进入 SpaceLife 模式时崩溃。

### 技术方案：
1. **`GameObjectPool.Get()`**：从内部 `ObjectPool<GameObject>` 取出实例后，添加 Unity 空检查（`instance == null`）。若底层 GameObject 已被销毁，则通过 `CreateInstance()` 重新创建新实例后再定位并激活。
2. **`SpaceLifeManager.ReturnPlayerToPool()`**：在调用 `_playerPool.Return()` 或 `Destroy()` 前添加二次 Unity 空检查，防范 `_currentPlayer` 在 C# 层非空但 Unity 对象已被销毁的情况（原有的 early return 仅检查 C# null，不检测 Unity 销毁状态）。

---

## UI 原型：星图 UI v2 — 2026-02-27 16:43

### 修改文件：
- `Docs/StarChartUIPrototype.html`

### 内容简述：
重写星图 UI HTML 原型，使其与 `StarChartUIDesign.md` 对齐。应用了大量结构与交互变更。

### 目的：
提供一个精确的、可交互的浏览器原型，用于 UI 讨论和迭代，之后再在 Unity `UICanvasBuilder.cs` 中实现。

### 技术方案：
1. **布局重构**：左列 = 加特林切换器（▲/▼ + 配装名 + 计数器）。右侧 = 单个配装卡片，包含 PRIMARY 和 SECONDARY 两条轨道纵向堆叠（匹配设计文档 §一）。
2. **加特林旋转动画**：CSS `perspective rotateX` 关键帧 — `rotate-out-up/down`（280ms）+ `rotate-in-down/up`（320ms）模拟转管手感。切换后 `loadout-section` 附带机械抖动。
3. **轨道槽位结构**：每条轨道为单行平铺混合 CORE+PRISM 槽位（空槽显示类型标签）。装备时强制匹配槽位类型，不匹配则抖动反馈。
4. **背包**：56×56px 槽位，2px 间距，flex-wrap — 匹配设计文档紧凑背包布局规格。980px 面板宽度下可容纳 10–12 列。
5. **悬停高亮**：轨道块 `mouseenter` 时以 `inset 2px 0 0 cyan` 边框 + 背景色调高亮。
6. **扫描线 + 扫描光束**：静态扫描线叠加层 + 动画移动光束（`scanBeam` 关键帧，4秒循环）。
7. **已装备槽位呼吸光效**：已装备槽位的 `::before` 伪元素上使用 `slotBreath` 关键帧动画。
8. **键盘快捷键**：Q/E + PageUp/PageDown 切换配装；Esc 清除选中。
9. **操作按钮**：保存配置 / 重命名（prompt）/ 删除（最后一个配装有保护机制）。
10. **工具提示**：150ms 延迟，跟随鼠标，智能边缘避让，显示已装备位置标签。

---

## UI 原型：星图 UI v2.1 — 轨道多行布局 & 类型配色 — 2026-02-27 16:53

### 修改文件：
- `Docs/StarChartUIPrototype.html`

### 内容简述：
更新 HTML 原型：PRIMARY 轨道改为双行布局；所有槽位类型拥有独立配色标识；背包筛选按钮顺序与轨道从左到右的类型顺序一致（SAIL→PRISM→CORE→SAT）。

### 变更细节：
1. **PRIMARY 轨道 → 双行**：第 0 行 = SAIL 槽位 + SAT 槽位（从左到右）。第 1 行 = PRISM 槽位 + CORE 槽位（从左到右）。SECONDARY 轨道保持单行（SAIL→PRISM→CORE→SAT）。
2. **类型配色系统**：新增 CSS 变量 `--col-sail`（#34d399 绿）、`--col-prism`（#a78bfa 紫）、`--col-core`（#60a5fa 蓝）、`--col-sat`（#fbbf24 黄），含暗色/发光变体。每种槽位类型拥有独立的背景、边框和悬停发光效果。
3. **行类别标签**：每组槽位带有彩色圆点 + 类型名称标签（`trl-sail/prism/core/sat` 类），便于用户即时识别所属类型。
4. **槽位类型标签颜色**：`.slot-type-label` 现继承对应类型的强调色，不再使用统一的暗白色。
5. **筛选按钮颜色**：每个筛选按钮（SAILS/PRISMS/CORES/SATS）使用对应类型强调色；激活态使用彩色背景 + 边框。
6. **筛选顺序**：从 ALL/CORES/PRISMS/SAILS/SATS → ALL/SAILS/PRISMS/CORES/SATS（从左到右与轨道布局一致）。
7. **数据结构**：装配槽位从扁平数组重构为 `{ sail:[], sat:[], prism:[], core:[] }` 每轨道。`renderTrack` 替换为 `renderSlotRow(trackName, rowType, slots)`。`allSlots()` 辅助函数用于展平装备/卸装逻辑。

---

## 星图 UI — Secondary Track 与 Primary 对齐 (2026-02-28 09:33)

### 修改文件：
- `Tools/StarChartUIPrototype.html`

### 内容简述：
使 SECONDARY 轨道的槽位数量和布局与 PRIMARY 轨道在所有 3 套配置中完全一致。

### 目的：
此前 SECONDARY 槽位较少（sail×1, prism×2, core×1, sat×1），而 PRIMARY 为（sail×2, sat×1, prism×3, core×1）。这种不一致使 SECONDARY 显得较弱且视觉不平衡。现在两条轨道共享相同的槽位结构。

### 技术方案：
1. **数据**：更新所有 3 套配置的 `secondary` 槽位数组，使其与 `primary` 数量一致：`sail×2, sat×1, prism×3, core×1`。已装备的物品在适用处予以保留。
2. **渲染顺序**：将 `renderAllTracks()` 中 secondary 的调用顺序从 `sail→prism→core→sat` 改为 `sail→sat→prism→core`，与 primary 的渲染顺序一致。
3. **数据键顺序**：Secondary 数据对象现与 primary 使用相同键顺序（`sail→sat→prism→core`），保持一致性。

---

## 星图 UI — 配装栏/背包布局再平衡 (2026-02-28 09:36)

### 修改文件：
- `Tools/StarChartUIPrototype.html`

### 内容简述：
缩小了配装栏区域，放大了背包面板。统一了配装栏与背包的槽位尺寸，使已装备部件与背包部件视觉大小一致。

### 目的：
此前配装栏槽位（64px）比背包槽位（56px）更大，造成视觉不一致。配装栏区域占用过多垂直空间，导致背包网格空间不足。用户要求缩小配装栏、放大背包，并统一部件尺寸。

### 技术方案：
1. **统一槽位尺寸**：`--slot-size` 和 `--inv-slot-size` 均设为 **52px**（分别从 64px 和 56px 调整）。
2. **面板高度**：从 620px 增加到 **700px**，为背包提供更多垂直空间。
3. **配装栏压缩**：缩小 loadout-section 内边距（8px→4px）、loadout-card 内边距（8px→4px）、间距（6px→3px）、track-block 间距（8px→6px，内边距 4px→2px）、track-block-label 宽度（80px→64px）、track-col 内边距（4px→2px，间距 3px→2px）、gatling-col 宽度（64px→56px）。
4. **字体缩放**：轨道标签 `.tb-name` 10px→9px、`.tb-bind` 9px→8px、`.slot-icon` 24px→20px、空槽 '+' 20px→16px。
5. **背包增强**：增大 `.inventory-area` 内边距（上 6px→8px，下 8px→10px），提供更多呼吸空间。

---

## 星图 UI — 配装栏/背包 50-50 布局 + 槽位缩小 (2026-02-28 09:52)

### 修改文件：
- `Tools/StarChartUIPrototype.html`

### 内容简述：
将配装栏与背包面板改为 50/50 等分布局，缩小配装栏槽位尺寸。轨道列保持原有单行 4 列排列（SAIL → PRISM → CORE → SAT）。

### 目的：
此前背包面板过大、配装栏与背包比例失调。用户要求两个区域 55 开（各占一半）。

### 技术方案：
1. **50/50 等分布局**：`.loadout-section` 从 `flex-shrink:0`（自然高度）改为 `flex:1`，与 `.inventory-section`（同为 `flex:1`）实现等分。两者均添加 `min-height:0` 和 `overflow:hidden` 防止内容溢出。
2. **轨道列保持单行 4 列**：`.track-rows` 维持 `flex-direction:row`，`.track-col` 维持 `flex:1`，列顺序 SAIL → PRISM → CORE → SAT 不变。
3. **槽位尺寸缩小**：`--slot-size` 从 52px 缩小到 **40px**，`--inv-slot-size` 从 52px 缩小到 **44px**。
4. **track-block 弹性填充**：添加 `flex:1` + `min-height:0` + `align-items:stretch`，使 PRIMARY 和 SECONDARY 在 loadout-card 内均匀分配高度。

---

## 星图 UI — Loadout 切换动画改为贴纸滑动 (2026-02-28 09:57)

### 修改文件：
- `Tools/StarChartUIPrototype.html`

### 内容简述：
将 Loadout 切换动画从 3D rotateX 滚筒旋转效果改为 2D translateY + opacity 贴纸滑动效果。切换到下一个 loadout 时，当前卡片向上滑出并渐隐，新卡片从下方滑入并渐显；切换到上一个 loadout 时方向相反。

### 目的：
用户描述 Loadout 切换效果应仿佛从正上方俯视擀面杖上的贴纸滚动——贴纸向上/下位移并渐隐，另一张从对向出现。原 3D rotateX 效果偏"翻转"而非"贴纸滑动"，需替换为纯 2D 位移+透明度动画。

### 技术方案：
1. **移除旧 3D 动画**：删除 `cylinderRollOutForward/InForward/OutBackward/InBackward` 四个 `@keyframes`（基于 `rotateX`），以及 `perspective`、`transform-style:preserve-3d`、`backface-visibility:hidden` 等 3D 属性。同时移除更早期的 `rotateOutUp/InDown/OutDown/InUp` 四个旧动画。
2. **新增贴纸滑动动画**：四个新 `@keyframes`：
   - `slideOutUp`：`translateY(0) → translateY(-40%)` + `opacity 1→0`（next 退出）
   - `slideInFromBelow`：`translateY(40%) → translateY(0)` + `opacity 0→1`（next 进入）
   - `slideOutDown`：`translateY(0) → translateY(40%)` + `opacity 1→0`（prev 退出）
   - `slideInFromAbove`：`translateY(-40%) → translateY(0)` + `opacity 0→1`（prev 进入）
3. **CSS 类映射不变**：`.roll-out-forward` / `.roll-in-forward` / `.roll-out-backward` / `.roll-in-backward` 类名保留，JS 切换逻辑零改动。
4. **时长微调**：动画时长从 0.28s 缩短至 0.26s，JS `DURATION` 常量同步调整为 260ms，使滑动更干脆。

---

## 星图 UI — 拖拽装备系统 (2026-02-28 13:22)

### 修改文件：
- `Tools/StarChartUIPrototype.html`

### 内容简述：
将星图 UI 原型的物品交互方式从点选（click-to-select + click-to-equip）完全替换为拖拽（drag & drop）系统。

### 目的：
实现更直觉化的背包式交互体验，用户可直接拖拽物品到轨道槽位装备，或从槽位拖回库存区域卸载。

### 技术方案：

**1. 移除旧点选逻辑：**
- 删除 `state.selectedInvItem` 状态字段
- 移除 `onInvClick()`、`onSlotClick()` 函数
- 移除库存物品和槽位的 `click` 事件监听

**2. 拖拽状态管理器（`dragState` 对象）：**
- 字段：`isDragging`、`sourceType`（`inventory`/`slot`）、`item`、`sourceSlotId`、`sourceLoadout`、`startX/Y`、`pendingEl/Item/Source`（预阈值状态）
- `startDrag()`：检查 `isAnimating` 保护，设置状态，显示 Ghost
- `endDrag()`：清理所有状态、高亮、Ghost、cursor
- `cancelDrag()`：回滚槽位预清空，调用 `endDrag()`

**3. Ghost 拖拽元素：**
- `#drag-ghost` 固定定位，`pointer-events:none`，`z-index:9999`
- 跟随鼠标（偏移 +14px），显示物品图标和短名称
- 颜色状态：默认青色边框 → 有效目标绿色（`.ghost-valid`）→ 无效目标红色（`.ghost-invalid`）

**4. 拖拽发起（4px 阈值）：**
- 库存物品：`mousedown` 记录起始坐标，`mousemove` 超过 4px 后触发 `startDrag()`，原物品设为半透明占位（`.dragging-source`）
- 已装备槽位：同上，`startDrag()` 时立即预清空槽位数据并重新渲染，记录原始数据用于取消回滚

**5. 槽位高亮反馈（`mousemove` 中实时计算）：**
- 类型匹配 → `.drop-valid`（绿色边框发光 + scale 1.08）
- 类型不匹配 → `.drop-invalid`（红色边框）
- 库存区域（从槽位拖出时）→ `.drop-target-inv`（绿色内阴影）

**6. mouseup 放置逻辑（四种情形）：**
- **情形 A（库存→空槽）**：装备物品，播放 `snap-in` 弹入动画（scale 1.18→0.96→1.0）
- **情形 B（库存→占用槽 / 槽→槽交换）**：原槽物品自动退回库存，新物品装入
- **情形 C（槽→库存区域）**：槽位已预清空，物品回到库存（重新渲染即可）
- **情形 D（无效区域/类型不匹配）**：调用 `cancelDrag()` 回滚，播放 shake 动画

**7. Edge Cases 防护：**
- `Escape` 键触发 `cancelDrag()`
- `startDrag()` 入口检查 `isAnimating`，动画中禁止拖拽
- `window.blur` 事件触发 `cancelDrag()`，防止鼠标移出窗口丢失 mouseup
- Loadout 切换时调用 `endDrag()` 清理进行中的拖拽

**8. 库存渲染逻辑更新：**
- `renderInventory()` 过滤掉当前 Loadout 所有槽位中已装备的物品
- 库存为空时显示 `"ALL ITEMS EQUIPPED"` 占位文字
- 过滤器（SAIL/PRISM/CORE/SAT）与新逻辑兼容（先按类型过滤，再过滤已装备）
- 移除库存物品的 `is-equipped` 标记（已装备物品不再显示在库存中）

---

## 星图 UI — 拖拽候选槽高亮 (2026-02-28 13:28)

### 修改文件：
- `Tools/StarChartUIPrototype.html`

### 内容简述：
从库存拖拽物品时，立即高亮所有类型匹配的槽位（绿色 pulse 动画），让用户一眼看到哪里可以放置。

### 技术方案：
- 新增 `.slot.drop-candidate` CSS 类：绿色边框 + `candidatePulse` 呼吸动画（1.1s 循环）
- `startDrag()` 中调用 `highlightCandidateSlots(item.type)`，遍历所有 `.slot` 元素，对 `data-row === itemType` 的槽位添加 `.drop-candidate`
- `endDrag()` 中调用 `clearCandidateSlots()` 清除所有候选高亮
- `mousemove` 中悬停到匹配槽时叠加 `.drop-valid`（更强的高亮 + scale 1.1 + 无 pulse），离开后 `.drop-valid` 被清除，候选槽恢复 pulse 状态
- 类型不匹配槽悬停时仍显示 `.drop-invalid`（红色）

---

## 星图 UI — Loadout 贴纸滑动动画增加深度缩放 (2026-02-28 10:02)

### 修改文件：
- `Tools/StarChartUIPrototype.html`

### 内容简述：
在 Loadout 切换的贴纸滑动动画中加入 `scale` 变换，退出时从 `scale(1)` 缩小到 `scale(0.88)`，进入时从 `scale(0.88)` 放大到 `scale(1)`，模拟贴纸沿擀面杖弧面滑动时向画面深处隐去/从深处浮现的透视感。位移幅度从 40% 微调至 35%，配合缩放达到更自然的视觉比例。

### 目的：
用户反馈纯位移+渐隐缺少"向画面深处隐去"的感觉，需要加入微缩放来模拟擀面杖弧面的透视纵深。

### 技术方案：
1. 四个 `@keyframes` 的 `transform` 属性从单纯 `translateY` 改为 `translateY + scale` 复合变换。
2. 缩放比例 `0.88`：足够感知到纵深，又不至于过度夸张。
3. 位移幅度从 `40%` 调整为 `35%`，因为缩放本身已提供额外的视觉退场效果，不需要过大位移。

---

## 星图 UI — 网格背包 & 异形部件系统 (2026-02-28 13:53)

### 修改文件：
- `Tools/StarChartUIPrototype.html`

### 内容简述：
将星图 UI 原型从单格槽位系统全面重构为 Backpack Battles 风格的网格背包系统，支持异形部件（1×1、1×2、2×1、L形、2×2）在 2×2 Track 网格中的拖拽装备与卸载。

### 目的：
实现计划文档 `.codebuddy/plan/grid-inventory/` 中定义的全部 9 项任务，为后续 Unity 实现提供可交互的 UI 原型参考。

### 技术方案：

**任务1 — 数据模型重构：**
- 为每个 `ITEM` 添加 `shape: [[col,row],...]` 坐标数组，支持 5 种形状
- Track 槽位从 `[{type, itemId}]` 数组改为 `{ grid: [[null,null],[null,null]] }` 2×2 矩阵
- 新增辅助函数：`getShapeBounds(shape)`、`shapeExceedsGrid()`、`canPlace()`、`clearItemFromGrid()`、`writeItemToGrid()`
- 初始 Loadout 数据用 `placeItem()` 辅助函数预填充

**任务2 — Track 2×2 网格渲染：**
- `renderTrackTypeGrid()` 将每个类型列渲染为 `display:grid; grid-template-columns: repeat(2, var(--slot-size))` 的 4 格网格
- 每个 `.track-cell` 携带 `data-track`、`data-type-key`、`data-col`、`data-row` 属性
- 空格子显示淡色 `+` 占位符，被占用格子透明度为 0（被覆盖层遮盖）

**任务3 — 部件覆盖层渲染：**
- `renderItemOverlay()` 在网格容器上用绝对定位叠加覆盖层
- 覆盖层尺寸 = `bounds.cols × CELL_SIZE + (cols-1) × CELL_GAP`，精确覆盖 shape bounding box
- 覆盖层内部渲染 shape 形状格子（非矩形填充），显示部件图标（居中）、名称（底部）
- 类型颜色边框 + 呼吸光晕动画，hover 时 scale(1.04)
- `mousedown` 发起卸载拖拽（Task 8）

**任务4 — 16列库存网格：**
- `renderInventory()` 使用 `display:grid; grid-template-columns: repeat(16, var(--inv-slot-size))`
- 每个部件用 `grid-column: span {cols}; grid-row: span {rows}` 按 shape bounding box 渲染
- 部件内部显示 shape 形状预览（半透明色块）+ 图标 + 名称

**任务5 — Shape-aware Ghost：**
- `showGhost()` 根据 `getShapeBounds(item.shape)` 动态计算 Ghost 宽高
- Ghost 内部渲染 shape 格子网格（active 格子显示青色半透明，empty 格子透明）
- Ghost 根据预览状态切换 `.ghost-valid`（绿色）/ `.ghost-invalid`（红色）边框

**任务6 — 碰撞检测与预览高亮：**
- `mousemove` 检测鼠标下方的 `.track-cell`，以其 `data-col/row` 为锚点
- `applyPreview()` 遍历 `item.shape` 所有偏移格子，调用 `canPlace()` 检测越界和占用
- 全部通过 → 预览格子加 `.preview-valid`（绿色）；任意失败 → `.preview-invalid`（红色）
- 拖拽开始时对匹配类型的整列 `.track-col` 添加 `.drop-candidate-col` 呼吸高亮

**任务7 — mouseup 放置逻辑：**
- 仅在 `dragState.previewValid === true` 时执行放置
- 检测被顶出的部件（displaced items），先清空其格子，再写入新部件
- 支持交换场景：被顶出部件自动回到库存
- 放置成功后对覆盖层播放 `snapIn` 动画（scale 1.18 → 0.96 → 1.0，150ms）

**任务8 — 卸载拖拽：**
- 覆盖层 `mousedown` 时：`sourceType = 'overlay'`，记录 `{ track, typeKey, anchorCol, anchorRow }`
- 拖拽阈值（4px）超过后：`clearItemFromGrid()` 清空格子，`renderAllTracks()` 刷新显示
- 拖拽结束未放置到有效位置 → `cancelDrag()` 恢复原位（`writeItemToGrid()` 重写格子）

**任务9 — Edge Cases 防护：**
- `Escape` 键：调用 `cancelDrag()`，overlay 来源时恢复原位
- `window.blur`：同 cancelDrag
- `isAnimating` 锁：Loadout 切换动画期间 `startDrag()` 直接返回
- `shapeExceedsGrid()` 越界检测集成在 `canPlace()` 中，`applyPreview()` 自动拒绝越界放置
- 类型不匹配时 `applyPreview()` 不调用（直接显示 ghost-invalid），不会写入错误数据

---

## Track 网格强制替换 + 飞回动画 — 2026-02-28 14:47

### 修改文件
- `Tools/StarChartUIPrototype.html`

### 内容简述
在星图 UI 原型中实现 Track 网格强制替换功能及被顶出部件飞回库存动画。

### 目的
玩家可直接将新部件拖入已有部件的格子，系统自动顶出旧部件送回库存，无需手动卸载。

### 技术方案

**任务1 — 放置逻辑重构（移除占用即拒绝）：**
- `mouseup` 放置分支：移除原 `canPlace()` 检测，改为遍历 `item.shape` 收集 `evictedIds`（Set）
- 对每个被顶出的部件调用 `clearItemFromGrid()` 清除其在 grid 中的**所有**格子（含未被新部件覆盖的格子）
- 唯一拒绝条件：`shapeExceedsGrid()` 越界 或 类型不匹配

**任务2 — 库存回收：**
- 顶出部件的 grid 数据清除后，`renderInventory()` 自动将其重新渲染到库存末尾（因为 `isEquippedInCurrentLoadout()` 返回 false）
- 覆盖层 DOM 由 `renderAllTracks()` 重新渲染时自动移除旧 overlay、恢复空格 `+` 占位符

**任务3 — 三色预览高亮系统：**
- 新增 `.track-cell.preview-replace`（橙色）CSS 样式
- 新增 `#drag-ghost.ghost-replace`（橙色边框）CSS 样式
- 新增 `.ghost-replace-hint` 文字样式（橙色，绝对定位在 ghost 顶部）
- `applyPreview()` 重构：越界 → 红色；有占用不越界 → 橙色；全空 → 绿色
- `updateGhostHint(count, isInvalid)` 函数：替换时在 Ghost 上显示 `↺ 替换 N 个部件`

**任务4 — 自身冲突处理：**
- 同 Track 内移动：`mousemove` 阶段已 `clearItemFromGrid()` 预清除，`mouseup` 时 grid 中不含自身 id，`evictedIds` 不会包含自身
- 跨 Track 移动：`mouseup` 中检测 `src.track !== track || src.typeKey !== typeKey` 时额外清除来源 grid

**任务5 — 飞回库存动画：**
- `activeFlightAnimations[]` 全局列表管理所有进行中的飞行动画
- `skipAllFlightAnimations()` 在 `startDrag()` 时调用，立即移除所有克隆体并恢复库存可见性
- `flyItemToInventory(itemId, sourceRect)` 函数：
  - 用 `getBoundingClientRect()` 获取来源 overlay 坐标（在 `renderAllTracks()` 前捕获）
  - 创建 `fixed` 定位的 `.fly-clone` DOM，初始位置与 overlay 完全重合
  - 计算 `dx/dy/scale` 使克隆体飞向库存目标格子
  - 通过 CSS `transition: transform 350ms ease-in, opacity 350ms ease-in` 执行飞行
  - 多部件顶出时每个克隆体错开 50ms stagger
  - 飞行期间库存真实 DOM `opacity: 0`，动画结束后恢复并播放 `landBounce`（scale 1.2→1.0，150ms）

**任务6 — Edge Cases 防护：**
- `assertGridDomSync()` 函数：遍历所有 track/typeKey，对比 grid 数据与 DOM overlay，发现不一致时 `console.warn`
- 强制替换成功后和 `cancelDrag()` 后均调用 `assertGridDomSync()`
- `Escape` 键 → `cancelDrag()` → 数据回滚 + 断言
- `window.blur` → `cancelDrag()` → 数据回滚
- 通知栏：替换成功显示 `REPLACED: [新部件名] ↔ [被替换部件名, ...]`

---

## Bug Fix: 强制替换锚点计算错误 + grabOffset偏移 — 2026-02-28 15:14

### 修改文件
- `Tools/StarChartUIPrototype.html`

### Bug 1 — 多格部件无法放置（锚点计算错误）
**根因**：`mousemove` 中调用 `applyPreview(trackName, typeKey, col, row)` 时传入的是**鼠标悬停的格子坐标**（如 `[1,1]`），而非部件锚点。对于 2×2 部件，`shapeExceedsGrid([[0,0],[1,0],[0,1],[1,1]], 1, 1)` 会返回 `true`（`1+1=2 > 1`），导致被误判为越界，`previewTarget` 始终为 null，mouseup 时无法放置。

**修复**：将 `applyPreview(trackName, typeKey, col, row)` 改为 `applyPreview(trackName, typeKey, 0, 0)`。由于所有形状都从 `[0,0]` 开始，锚点固定为左上角是正确的。

### Bug 2 — Ghost 拖拽时位置偏移
**根因**：`grabOffsetX/Y` 已经正确记录了鼠标在元素内的相对位置，确认两处（overlay mousedown 和 bindInvItemDrag）都使用 `e.clientX - rect.left` / `e.clientY - rect.top`，保持鼠标抓住哪个位置 ghost 就跟随那个位置，无偏移。

---

## Bug Fix: Overlay 遮挡 Cell 导致拖拽无法替换已有部件 — 2026-02-28 15:23

### 修改文件
- `Tools/StarChartUIPrototype.html`

### 根本原因
Track 上已有部件时，`.item-overlay` 元素覆盖在 `.track-cell` 上方。`mousemove` 和 `mouseup` 中用 `elementFromPoint()` 获取鼠标下方元素时，返回的是 overlay 而非 cell，导致 `cellEl` 为 null，预览不触发，`previewTarget` 始终为 null，mouseup 时无法放置。

**结果**：从背包拖部件到已有部件的 Track 上，永远无法替换——这正是用户反馈"不能用"的根本原因。

### 修复方案

**mousemove**：不再只检查 `cellEl`，同时检查 `overlayEl = target?.closest('.item-overlay')`。从 cellEl 或 overlayEl 中取 `trackName`/`typeKey`，只要任一有效就触发 `applyPreview`。

**mouseup**：判断条件从 `if (cellEl && dragState.previewTarget)` 改为 `if ((cellEl || overlayDropEl) && dragState.previewTarget)`，确保鼠标松开在 overlay 上时也能正确放置。

### 覆盖场景
- 1格部件拖到有1格部件的Track → 替换 ✓
- 4格部件拖到有2格部件的Track → 顶掉2格部件 ✓
- 2格部件拖到有1格部件的Track → 顶掉1格部件 ✓
- 任意部件拖到空Track → 正常放置 ✓

---

## StarChart UI Phase A — 视觉样式与布局对齐 — 2026-02-28 16:06

### 新建文件
- `Assets/Scripts/UI/StarChartTheme.cs` — 颜色主题静态类，提供所有主题色常量和 `GetTypeColor()` API
- `Assets/Scripts/UI/StatusBarView.cs` — 底部通知栏组件，PrimeTween Sequence 实现延迟淡出

### 修改文件
- `Assets/Scripts/UI/SlotCellView.cs` — 移除 hardcode 颜色字段，改用 StarChartTheme；新增 `SetThemeColor()`、`_placeholderLabel`（`+` 占位符）、`SetReplaceHighlight()`
- `Assets/Scripts/UI/TrackView.cs` — 新增 `_prismLabel`/`_coreLabel` 类型标签字段；`Bind()` 中设置青色/紫色/蓝色；`RefreshLayer()` 增加 `layerType` 参数并调用 `SetThemeColor()`
- `Assets/Scripts/UI/InventoryItemView.cs` — 新增 `_typeDot`/`_equippedBorder` 字段；`Setup()` 设置类型色点和装备边框；`SetSelected()` 改用 `StarChartTheme.SelectedCyan`；悬停时 PrimeTween scale 1.06 动画
- `Assets/Scripts/UI/InventoryView.cs` — `SetFilter()` 调用 `UpdateFilterButtonStyles()`；新增过滤按钮激活/非激活态颜色切换
- `Assets/Scripts/UI/StarChartPanel.cs` — 新增 `_statusBar` 字段；装备/卸载成功失败时调用 `ShowMessage()`
- `Assets/Scripts/UI/Editor/UICanvasBuilder.cs` — 全面重构 `BuildStarChartSection()`：深色背景、四角括号、Header 栏、上55%/下40%/底部4% 布局、StatusBar；重构 `BuildTrackView()`：类型标签、StarChartTheme 颜色；重构 `BuildSlotCell()`：占位符标签；更新 `CreateInventoryItemViewPrefab()`：新增 typeDot/equippedBorder

### 目的
将 Unity StarChart 面板的视觉样式与 `StarChartUIPrototype.html` 原型对齐，实现深色科技感配色、四色类型主题、Header/StatusBar 通知系统，不改变现有数据模型和交互逻辑。

### 技术方案
- `StarChartTheme` 静态类集中管理所有颜色常量，所有 View 通过 `GetTypeColor(itemType)` 获取类型色，禁止 hardcode
- `StatusBarView` 使用 PrimeTween `Sequence.ChainDelay + Chain(Tween.Alpha)` 实现延迟淡出，`_fadeTween.Stop()` 打断旧动画
- `UICanvasBuilder` 新增 `BuildCornerBrackets()` 和 `BuildHeader()` 辅助方法，`BuildStarChartSection()` 完全重写为原型对齐的层级结构
- 运行 `ProjectArk > Build UI Canvas` 后一键生成完整层级，幂等操作

---

# StarChart UI Phase B — 数据模型视图层重构 (2026-02-28 22:00)

### 新建文件
- 无

### 修改文件
- `Assets/Scripts/UI/SlotCellView.cs` — 新增 `SlotType` 枚举（Core/Prism/LightSail/Satellite）替换 `IsCoreCell` bool；新增 `IsOccupied` 属性；新增 `HasSpaceForItem` 委托（SAIL/SAT 空间检测）；`OnPointerEnter` 扩展为三色高亮逻辑（绿/橙/红）；调用 `mgr.UpdateGhostDropState()` 同步 Ghost 边框颜色；`OnBeginDrag` 移除类型限制（支持所有类型拖拽）
- `Assets/Scripts/UI/TrackView.cs` — 新增 `_sailCell`/`_satCells`/`_sailLabel`/`_satLabel` 字段；`Bind()` 新增 `StarChartController` 参数；`Refresh()` 新增 `RefreshSailCell()`/`RefreshSatCells()` 刷新 SAIL/SAT 槽位；`HasSpaceForItem()` 扩展支持 LightSail/Satellite；`SetMultiCellHighlight()` 新增 `isReplace` 参数；`ClearAllHighlights()` 覆盖 SAIL/SAT 槽位
- `Assets/Scripts/UI/DragDrop/DragDropManager.cs` — 新增 `DropTargetSlotType`/`DropTargetIsReplace` 属性；新增 `EvictedItems` 列表；新增 `EvictBlockingItems()` 方法（强制替换时卸载阻碍部件）；`EquipToTrack()` 扩展支持 LightSail/Satellite；新增 `UpdateGhostDropState()` 方法；`BeginDrag()` 调用 `FlyBackAnimator.SkipAll()`；新增 `ShowReplaceMessage()` 显示橙色替换通知
- `Assets/Scripts/UI/StarChartPanel.cs` — 新增 `StatusBar` 属性（暴露给 DragDropManager）；新增 `Controller` 属性；`Bind()` 中将 controller 传给 TrackView

### 目的
重构视图层支持 4 列类型布局（SAIL/PRISM/CORE/SAT），实现强制替换交互逻辑（拖拽时顶掉已有部件），完善三色拖拽预览系统。

### 技术方案
- `SlotType` 枚举替代 `IsCoreCell` bool，统一 4 种槽位类型的类型匹配逻辑
- SAIL/SAT 槽位通过 `HasSpaceForItem` 委托注入空间检测，不修改 `SlotLayer<T>` 底层结构
- 强制替换在 `DragDropManager.EvictBlockingItems()` 层实现，逐个卸载阻碍部件直到空间足够
- Ghost 边框颜色通过 `UpdateGhostDropState(DropPreviewState)` 实时同步

---

# StarChart UI Phase C — 交互动画与体验打磨 (2026-02-28 22:00)

### 新建文件
- `Assets/Scripts/UI/FlyBackAnimator.cs` — 静态动画系统，`FlyTo()` 创建飞行克隆体从槽位飞向背包（350ms InQuad，位移+缩小+淡出），落地弹跳（scale 1.0→1.12→1.0，100ms），`SkipAll()` 立即跳过所有进行中动画

### 修改文件
- `Assets/Scripts/UI/StatusBarView.cs` — `ShowMessage()` 改为先 alpha 0 淡入（150ms OutQuad），hold duration 后淡出（500ms InQuad）；新消息打断旧动画从 alpha 0 重新淡入；使用 `Sequence` 替代 `Tween`
- `Assets/Scripts/UI/DragDrop/DragGhostView.cs` — 新增 `DropPreviewState` 枚举；新增 `_borderImage`/`_replaceHintLabel` 字段；新增 `SetDropState()` 方法（绿/橙/红边框 + `↺ REPLACE` 提示）；`Show()` 播放 scale 0.8→1.0 弹出动画（80ms OutBack）；`Hide()` 播放 scale 1.0→0 收缩动画（80ms InQuad）后 `SetActive(false)`
- `Assets/Scripts/UI/SlotCellView.cs` — `SetItem()` 播放背景闪烁（白色→类型色，150ms OutQuad）；`SetEmpty()` 播放背景淡出（→SlotEmpty，100ms OutQuad）；所有动画使用 `useUnscaledTime: true`
- `Assets/Scripts/UI/InventoryItemView.cs` — `SetSelected()` 改为 PrimeTween 边框淡入淡出（100ms OutQuad）；`OnBeginDrag()` 移除类型限制（支持所有类型）；拖拽开始/结束时 alpha 动画（0.4/1.0，80ms）
- `Assets/Scripts/UI/DragDrop/DragDropManager.cs` — 新增 `_rootCanvas` 字段；`Bind()` 缓存 `_inventoryRect`；`ExecuteDrop()` 后调用 `TriggerFlyBackAnimations()`；新增 `TriggerFlyBackAnimations()` 方法
- `Assets/Scripts/UI/StarChartPanel.cs` — 新增 `_panelCanvasGroup` 字段；`Open()` 播放 scale 0.95→1.0 + alpha 0→1（200ms OutQuad），动画期间 `interactable=false`；`Close()` 播放 scale 1.0→0.95 + alpha 1→0（150ms InQuad），动画结束后 `SetActive(false)`

### 目的
为关键交互添加动画反馈，提升手感和视觉体验，与原型的动态效果对齐。

### 技术方案
- 所有动画使用 PrimeTween，`useUnscaledTime: true`（星图面板在暂停状态下打开）
- `FlyBackAnimator` 为静态类，维护 `_activeAnimations` 列表，`SkipAll()` 调用 `Sequence.Complete()` 立即跳过
- Ghost 动画通过 `_scaleTween` 句柄管理，`Show()`/`Hide()` 调用前先 `Stop()` 防止冲突
- 面板开关动画通过 `CanvasGroup.interactable` 防止动画期间误操作



---

## StarChart UI Review — Bug修复 2026-02-28 22:30

### 新建/修改文件
- `Assets/Scripts/UI/UICanvasBuilder.cs` — 修复 `BuildTrackView()` 中 `labelTmp` 未定义的编译错误，替换为正确的局部变量名
- `Assets/Scripts/UI/DragDrop/FlyBackAnimator.cs` — 修复 `SkipAll()` 在迭代集合时调用 `Complete()` 触发 `OnComplete` 回调导致集合被修改的崩溃；改为先拷贝到临时列表再迭代
- `Assets/Scripts/UI/InventoryItemView.cs` — 为 `OnPointerEnter`、`OnPointerExit`、`OnBeginDrag`、`OnEndDrag` 中所有 `Tween.*` 调用补充 `useUnscaledTime: true`
- `Assets/Scripts/UI/DragDrop/DragDropManager.cs` — 修复 `CleanUp()` 直接赋值 `alpha = 1f` 覆盖 PrimeTween 动画的问题，改为仅当 source 不是 `InventoryItemView` 时才直接赋值；修复 Slot→空白区域拖拽静默失败，补充 StatusBar 提示；删除冗余字段 `_ghost`，统一使用 `_ghostView`
- `Assets/Scripts/UI/SlotCellView.cs` — 修复 `OnPointerExit()` 立即清除 `DropTargetTrack` 导致同轨道内移动时 Ghost 边框闪烁；改为按 TrackView 粒度管理，同一 TrackView 内移动不触发清除
- `Assets/Scripts/UI/TrackView.cs` — 在 `RefreshSailCell()` 和 `RefreshSatCells()` 开头添加 `_controller == null` 时的 `Debug.LogWarning` 警告

### 目的
修复 StarChart UI Phase A/B/C 全面 Review 中发现的 P0/P1/P2 级别 Bug 和代码质量问题，确保功能完整性、运行时稳定性和代码可维护性。

### 技术方案
- **FlyBackAnimator 崩溃**：`SkipAll()` 改为 `var copy = new List<Sequence>(_activeAnimations); _activeAnimations.Clear();` 后对 copy 逐一调用 `Complete()`，避免回调中修改正在迭代的集合
- **useUnscaledTime**：星图面板在 `Time.timeScale = 0` 时打开，所有 PrimeTween 动画必须加 `useUnscaledTime: true`，否则暂停状态下动画完全失效
- **CleanUp alpha 冲突**：通过 `source is InventoryItemView` 类型判断区分两种 source，`InventoryItemView` 的 alpha 恢复由其自身 `OnEndDrag` 的 PrimeTween 动画负责，`CleanUp()` 不干预
- **Slot→空白区域**：在 `EndDrag(true)` 且 `DropTargetValid == false` 时调用 `_statusBar.ShowMessage("No valid drop target")`，并触发 Ghost 飞回原位
- **Ghost 闪烁**：`OnPointerExit` 改为检查 `OwnerTrack` 是否与当前 `DropTargetTrack` 所属 TrackView 相同，仅在离开整个 TrackView 时才清除，消除同轨道内移动的闪烁帧
- **_ghost 冗余**：删除 `_ghost` 私有字段及 `Bind()` 中的赋值，全部替换为 `_ghostView`，减少维护歧义

## StarChart UI 冗余代码清理 — 2026-02-28 22:21

### 修改文件
- `Assets/Scripts/UI/StarChartPanel.cs` — 删除注释掉的 Tooltip 代码块（`_itemTooltipView` 字段、`HandleCellPointerEntered/Exited`、`HandleInventoryItemPointerEntered/Exited` 方法）；删除 `Bind()` 中注释掉的 `OnCellPointerEntered/Exited` 订阅行；删除 `// TODO: 光帆/伴星专用槽位` 注释；删除 `RefreshAll()`、`HandleInventoryItemSelected()`、`HandleCellClicked()` 中的 `Debug.Log` 调用
- `Assets/Scripts/UI/TrackView.cs` — 删除 `OnCellPointerEntered` 和 `OnCellPointerExited` 事件声明；删除 `Awake()` 中所有 Cell 的 `OnPointerEntered`/`OnPointerExited` 订阅转发行（Core/Prism/SAIL/SAT 共 5 处）
- `Assets/Scripts/UI/SlotCellView.cs` — 删除标注为 "Legacy compatibility" 的 `IsCoreCell` 属性（getter/setter 共 4 行）
- `Assets/Scripts/UI/InventoryView.cs` — 删除 `OnItemPointerEntered` 和 `OnItemPointerExited` 事件声明；删除 `Refresh()` 中的 `view.OnPointerEntered`/`view.OnPointerExited` 订阅转发行；删除 `Bind()` 中的 `Debug.Log`、`Refresh()` 中的 `Debug.LogWarning`（空检查）和末尾的 `Debug.Log`（统计）
- `Assets/Scripts/UI/DragDrop/DragDropManager.cs` — 将 `ShowReplaceMessage()` 中的 `System.Linq.Enumerable.Select` 替换为手写 `StringBuilder` 循环，消除对 `System.Linq` 的依赖

### 删除文件
- `Assets/Scripts/UI/Editor/StarChart169Builder.cs` — 废弃的旧版 Builder 存根（仅打印"请用新工具"日志），连同 `.meta` 文件一并删除

### 目的
在不改变任何运行时行为的前提下，清除 StarChart UI Phase A/B/C 三轮迭代后积累的所有冗余代码，使代码库清晰、精简、整洁。

### 技术方案
- 所有删除均经过 grep 全局验证，确认无其他文件引用被删除的符号（`OnCellPointerEntered/Exited`、`IsCoreCell`、`OnItemPointerEntered/Exited`）
- `System.Linq` 替换为 `System.Text.StringBuilder` 手写循环，符合项目"无不必要依赖"原则
- `StarChart169Builder.cs` 的 `.meta` 文件已同步删除，避免 Unity 产生孤立 GUID 警告

---

## Fix: PrimeTween Sequence useUnscaledTime Mismatch — 2026-03-01 00:19

### 修改文件
- `Assets/Scripts/UI/StarChartPanel.cs`
- `Assets/Scripts/UI/SlotCellView.cs`
- `Assets/Scripts/UI/FlyBackAnimator.cs`
- `Assets/Scripts/UI/StatusBarView.cs`

### 内容简述
修复 PrimeTween `Sequence.Create()` 与子 Tween `useUnscaledTime` 不一致导致的运行时 LogError。

### 目的
消除打开/关闭星图面板、装备物品、飞回动画、状态栏通知时控制台输出的 `useUnscaledTime was ignored` 错误。

### 技术方案
- PrimeTween 的 Sequence 会强制将自身的 `useUnscaledTime` 覆盖所有子 Tween，父子不一致时抛出错误
- 所有 4 个文件中的 `Sequence.Create()` 均缺少 `useUnscaledTime: true` 参数，而子 Tween 都传了 `useUnscaledTime: true`
- 修复方式：统一为 `Sequence.Create(useUnscaledTime: true)`，使父 Sequence 与所有子动画保持一致
- UI 动画使用 unscaled time 是正确行为——星图面板打开时游戏处于暂停状态（`Time.timeScale = 0`）

---

## StarChart UI 布局重构（对齐 HTML 原型）— 2026-03-01 00:28

### 新建文件
- `Assets/Scripts/UI/LoadoutSwitcher.cs`

### 修改文件
- `Assets/Scripts/UI/StarChartPanel.cs`
- `Assets/Scripts/UI/StarChartTheme.cs`
- `Assets/Scripts/UI/Editor/UICanvasBuilder.cs`

### 内容简述
将 StarChart UI 布局从旧的线性槽位结构重构为对齐 HTML 原型的新布局，主要变更：

1. **`LoadoutSwitcher.cs`（新建）**：Gatling 列组件，包含 ▲/▼ 按钮、DrumCounter（`TMP_Text`，显示 "LOADOUT #1 · 1/1"）、LOADOUT 标签。MVP 单 Loadout 模式下禁用按钮；预留 `SwitchTo(int index)` 接口供后续多 Loadout 扩展。Loadout 切换时对 LoadoutCard 播放滑动动画（PrimeTween，`useUnscaledTime: true`）。

2. **`StarChartPanel.cs`（修改）**：新增 `[SerializeField] LoadoutSwitcher _loadoutSwitcher` 和 `[SerializeField] RectTransform _loadoutCard` 引用；`Open()` 动画序列加入 LoadoutCard 从下方滑入动画（OutBack，0.25s）；`Bind()` 中初始化 LoadoutSwitcher；`OnDestroy()` 中取消订阅 `OnLoadoutChanged`。

3. **`StarChartTheme.cs`（修改）**：新增 `BgPanel`（`#0b1120`）和 `BgTrack`（`#0f1828`）颜色；更新 `CornerBracket` 为青色 `#22d3ee`（原为白色低透明度）。

4. **`UICanvasBuilder.cs`（重构）**：
   - `BuildStarChartSection`：布局改为 Header / LoadoutSection(GatlingCol + LoadoutCard) / SectionDivider / InventorySection / StatusBar；面板背景改用 `BgPanel`；自动连线 `_loadoutSwitcher` 和 `_loadoutCard`。
   - `BuildTrackView`：完全重写，改为生成 4 个 `TypeColumn` 组件（每列含列头 + 2×2 GridContainer），连线到 `_sailColumn/_prismColumn/_coreColumn/_satColumn`；移除旧的 `_sailCell/_prismCells/_coreCells/_satCells` 字段引用。
   - 新增 `BuildTypeColumn`：生成单个 TypeColumn（列头 dot+label + 2×2 GridLayout + 4 个 SlotCellView），连线 `_columnLabel/_columnDot/_columnBorder/_cells`。
   - 新增 `BuildGatlingCol`：生成 ▲ Button、DrumCounter TMP_Text、▼ Button、LOADOUT 标签，挂载并连线 `LoadoutSwitcher`。
   - 新增 `BuildLoadoutCardHeader`：生成 ◈ 图标 + Loadout 名称 + 副标题。
   - `BuildHeader`：更新标题为 "CANARY — STAR CHART CALIBRATION SYSTEM"，新增 "SYSTEM ONLINE" 徽章（绿色）。
   - 状态栏文字更新为 "EQUIPPED 0/10 · INVENTORY 0 ITEMS · DRAG TO EQUIP · CLICK TO INSPECT"。

### 目的
对齐 `Tools/StarChartUIPrototype.html` 原型的布局架构，实现 4 列 TypeColumn 2×2 网格、Loadout 切换器（Gatling 列）、深色科幻视觉风格。

### 技术方案
- `TypeColumn` 作为 `MonoBehaviour` 挂载在独立 GameObject 上，通过 `UICanvasBuilder` 的 `WireField` 自动连线到 `TrackView` 的 `_sailColumn/_prismColumn/_coreColumn/_satColumn` 字段
- `LoadoutSwitcher` 通过 `event Action<int> OnLoadoutChanged` 与 `StarChartPanel` 解耦通信
- 所有动画使用 PrimeTween + `useUnscaledTime: true`，兼容游戏暂停状态
- `UICanvasBuilder` 保持幂等性（`GetOrCreate` 模式），重复执行不创建重复对象
- `TrackView.TypeColumn` 已实现 `IPointerEnterHandler/IPointerExitHandler`，hover 时边框从 dim 色渐变到亮色（Task 8 已完成）
- `DragDropManager` 已支持 TypeColumn 目标检测（Task 7 已完成）

### 验收状态
- ✅ Task 1：TrackView 已有完整 TypeColumn 4列结构（代码已存在，无需修改）
- ✅ Task 2：SlotCellView 已有完整 overlay 渲染逻辑（代码已存在，无需修改）
- ✅ Task 3：LoadoutSwitcher.cs 新建完成
- ✅ Task 4：StarChartPanel.cs 添加 LoadoutSwitcher 引用和 LoadoutCard 动画
- ✅ Task 5：UICanvasBuilder.cs 重构完成，生成新布局层级
- ✅ Task 6：视觉风格对齐（BgPanel/BgTrack/CornerBracket/Header/StatusBar）
- ✅ Task 7：DragDropManager 已支持 TypeColumn（代码已存在，无需修改）
- ✅ Task 8：TypeColumn hover 动画已实现（代码已存在，无需修改）
- ⚠️ Task 9：需在 Unity Editor 中执行 `ProjectArk > Build UI Canvas` 进行 Play Mode 验收

---

## StarChart UI 布局重构 — 完整实现（Phase 2）— 2026-03-01 00:27

### 新建/修改文件
- `Assets/Scripts/UI/TrackView.cs` ← **完全重写**
- `Assets/Scripts/UI/SlotCellView.cs` ← **修改**（新增 SetOverlay + 拖拽脉冲动画）
- `Assets/Scripts/UI/LoadoutSwitcher.cs` ← **新建**
- `Assets/Scripts/UI/StarChartPanel.cs` ← **完全重写**
- `Assets/Scripts/UI/DragDrop/DragDropManager.cs` ← **修改**（新增 HighlightMatchingColumns）

### 内容简述

#### 1. TrackView.cs — 完全重写
- 引入 `TypeColumn` 内部类（`MonoBehaviour`），持有 `_columnLabel`、`_columnDot`、`_columnBorder`、`_cells[4]`
- `TypeColumn.Initialize(slotType, typeColor, ownerTrack)` 统一初始化列标识和颜色
- `TypeColumn` 实现 `IPointerEnterHandler/IPointerExitHandler`，hover 时边框从 dim 色渐变到亮色（PrimeTween，`useUnscaledTime: true`）
- `TypeColumn.SetDropHighlight(bool)` / `ClearDropHighlight()` 供拖拽系统调用
- `TrackView` 改为持有 `_sailColumn/_prismColumn/_coreColumn/_satColumn` 四个 `TypeColumn` 引用
- `Refresh()` 按列分组刷新：`RefreshColumn<T>` 处理 Core/Prism，`RefreshSailColumn`/`RefreshSatColumn` 处理 SAIL/SAT
- 新增 `GetColumn(SlotType)` / `GetAllCells()` / `SetColumnDropHighlight(SlotType, bool)` 公共 API
- SAT 列容量从 2 扩展为 4（2×2 网格）

#### 2. SlotCellView.cs — 新增功能
- 新增 `SetOverlay(StarChartItemSO item, bool isPrimary)` 方法：主 cell 显示图标，次 cell 仅显示着色背景
- 新增 `_pulseTween` 字段：拖拽悬停时播放 scale 1.0→1.05 脉冲动画（`useUnscaledTime: true`）
- `OnPointerEnter` 在拖拽状态下触发脉冲；`OnPointerExit` 恢复 scale
- `OnDestroy` 中正确 Stop `_pulseTween`

#### 3. LoadoutSwitcher.cs — 新建
- `[SerializeField]` 字段：`_prevButton`、`_nextButton`、`_drumCounterLabel`、`_loadoutCard`
- `SetLoadoutCount(int)` 设置 Loadout 总数，MVP 单 Loadout 时禁用按钮
- `SwitchTo(int index, bool slideUp)` 切换 Loadout，播放 LoadoutCard 滑动动画
- `event Action<int> OnLoadoutChanged` 供 StarChartPanel 订阅
- DrumCounter 格式：`"LOADOUT #1  ·  1/1"`

#### 4. StarChartPanel.cs — 完全重写
- 新增 `[SerializeField] LoadoutSwitcher _loadoutSwitcher` 和 `[SerializeField] RectTransform _loadoutCard`
- `Open()` 动画序列：面板 scale+alpha + LoadoutCard 从下方滑入（`UIAnchoredPositionY`，OutCubic，0.25s）
- `Close()` 动画序列：面板 scale+alpha + LoadoutCard 向下滑出（InQuad，0.15s）
- `RefreshAll()` 新增 `UpdateStatusBar()` 调用，格式：`"EQUIPPED X/10 · INVENTORY X ITEMS · DRAG TO EQUIP · CLICK TO INSPECT"`
- `Bind()` 中初始化 LoadoutSwitcher（`SetLoadoutCount(1)`）并订阅 `OnLoadoutChanged`
- `OnDestroy()` 中取消订阅 `OnLoadoutChanged`

#### 5. DragDropManager.cs — 新增 TypeColumn 高亮
- `BeginDrag()` 末尾调用 `HighlightMatchingColumns(payload.Item, true)`
- `CleanUp()` 末尾调用 `HighlightMatchingColumns(CurrentPayload.Item, false)`
- 新增 `HighlightMatchingColumns(StarChartItemSO item, bool highlight)` 方法：
  - 通过 `_panel.GetComponentsInChildren<TrackView>()` 找到所有轨道视图
  - 对每个 TrackView 调用 `GetColumn(matchType).SetDropHighlight(true/false)`

### 目的
完整实现 StarChart UI 布局重构计划（task-item.md）的全部 9 个任务，使 Unity 实现对齐 `Tools/StarChartUIPrototype.html` 原型的布局架构和交互逻辑。

### 技术方案
- `TypeColumn` 作为 `MonoBehaviour` 挂载在独立 GameObject 上，通过 `UICanvasBuilder.WireField` 自动连线
- 所有 PrimeTween 动画统一使用 `useUnscaledTime: true`，兼容游戏暂停状态（`Time.timeScale = 0`）
- `LoadoutSwitcher` 通过 `event Action<int> OnLoadoutChanged` 与 `StarChartPanel` 解耦通信
- 拖拽高亮通过 `DragDropManager → TrackView.GetColumn → TypeColumn.SetDropHighlight` 链路实现
- 验收步骤：Unity Editor 执行 `ProjectArk > Build UI Canvas` → Play Mode 逐条验证

---

## StarChart UI 布局重构 — 字段名修复 — 2026-03-01 00:39

### 修改文件
- `Assets/Scripts/UI/Editor/UICanvasBuilder.cs`

### 内容简述
修复 `BuildGatlingCol` 方法中 `WireField` 调用的字段名，对齐 `LoadoutSwitcher.cs` 的实际 `SerializeField` 字段名：

| 旧字段名（错误） | 新字段名（正确） |
|---|---|
| `_btnUp` | `_prevButton` |
| `_btnDown` | `_nextButton` |
| `_drumCounter` | `_drumCounterLabel` |

### 目的
`LoadoutSwitcher.cs` 已有完整实现（含 `SetLoadoutCount`、`SwitchTo`、`PlaySlideAnimation`），字段名与上一轮 `BuildGatlingCol` 中的 `WireField` 调用不一致，会导致运行时连线失败（字段保持 null）。

### 技术方案
`WireField` 通过反射按字段名查找 `SerializeField`，字段名必须与 C# 源码中的私有字段名完全一致。修复后三个字段均可正确连线。

### 验收状态
- ✅ `_prevButton` / `_nextButton` / `_drumCounterLabel` 字段名与 `LoadoutSwitcher.cs` 完全匹配
- ✅ `TrackView._sailColumn/_prismColumn/_coreColumn/_satColumn` 字段名验证通过
- ✅ `StarChartPanel._loadoutSwitcher/_loadoutCard/_panelCanvasGroup` 字段名验证通过
- ✅ `TypeColumn._columnLabel/_columnDot/_columnBorder/_cells` 字段名验证通过
- ✅ 无旧字段残留（`_sailLabel/_prismLabel/_coreCells/_satCells` 等已完全移除）
- ⚠️ 仍需在 Unity Editor 中执行 `ProjectArk > Build UI Canvas` 进行最终 Play Mode 验收




---

## SpinningTop Bug 修复 — 2026-03-01 00:51

### 修改文件
- `SideProject/spinning-top.html`

### 内容简述
对照 GDD（SpinningTopGDD.md）审查 spinning-top.html 全部 4431 行代码，发现并修复 6 处 Bug：

| # | 位置 | 修复内容 |
|---|------|---------|
| 1 | `showEventScreen` | `ev.name` → `ev.title`（EVENTS_DB 字段名不匹配，导致事件标题显示 undefined） |
| 2 | `resolveEventChoice` | `choice.resolve(G)` → `choice.effect(G)`（字段名不匹配，点击事件选项时 TypeError 崩溃） |
| 3 | `generateIntel` | `enemy.decayMult/arDmg/baseRpm` → `enemy.stats.decayMult/dmgMult/maxRpm`（顶层字段不存在，情报全部显示错误；同时补充 isElite/isBoss 标识逻辑） |
| 4 | `getRewardPart` | `'uncommon'` → `'rare'/'epic'`（PARTS_DB 中不存在 uncommon 稀有度，Boss 奖励池始终为空；同时过滤掉 consumable 类型） |
| 5 | `applyElementEffect` | `G.loadout.ar`（对象）→ 取 `.id` 后再查 PARTS_DB；`G.loadout.sg` 同理修复（元素攻击环效果从不触发） |
| 6 | 粒子渲染 patch | `getElementById('battle-canvas')` → `_arenaCanvas/_arenaCtx`（canvas id 不存在，元素粒子特效从不渲染） |
| 7 | `showBattleAnnouncement` | 挂载目标 `#arena-wrap\|#battle-screen` → `#battle-canvas-wrap`（实际 HTML id，Boss 阶段公告无法显示） |
| 8 | Phase 7 重复代码块 | 经验证文件中无重复，无需修改 |

### 目的
确保 spinning-top.html 所有游戏流程（标题→底座选择→地图→战斗→结算→事件→商店→Boss→通关/Game Over）均可无 Bug 运行。

### 技术方案
- 全部采用精确 `replace_in_file` / `multi_replace` 定点修复，不影响其他逻辑
- `generateIntel` 重写为防御性写法（`const st = enemy.stats || {}`），避免 null 访问
- `applyElementEffect` 中 arId/sgId 取值统一为 `typeof obj === 'string' ? obj : obj.id` 兼容两种存储格式

---

## StarChart UI 布局重构 — UICanvasBuilder 幂等性与 CanvasGroup 初始化 — 2026-03-01 01:01

### 修改文件
- `Assets/Scripts/UI/Editor/UICanvasBuilder.cs`

### 内容简述
完成任务8：修复 `BuildStarChartSection` 中 `panelCG`（CanvasGroup）添加后缺少初始化的问题。

**修复前**：
```csharp
var panelCG = panelGo.AddComponent<CanvasGroup>();
// 无初始化 → alpha=1, interactable=true, blocksRaycasts=true（Unity 默认值）
```

**修复后**：
```csharp
var panelCG = panelGo.AddComponent<CanvasGroup>();
panelCG.alpha = 0f;
panelCG.interactable = false;
panelCG.blocksRaycasts = false;
```

同时确认幂等性逻辑完整：
- `BuildUICanvas()` 主方法对每个 Section 均先 `GetComponentInChildren<T>()` 检查，已存在则跳过构建并打印 `skipping` 日志
- `FindOrCreateCanvas()` 三段式查找策略（UIManager → sortingOrder=10 → 新建）确保不重复创建 Canvas
- Step 5 `starChartPanel.gameObject.SetActive(false)` 确保构建完成后面板默认隐藏

### 目的
确保通过 `ProjectArk > Build UI Canvas` 构建的 StarChartPanel，在 Play Mode 启动时 CanvasGroup 处于完全隐藏且不拦截输入的初始状态，与 `StarChartPanel.Awake()` 中的初始化逻辑保持一致。

### 技术方案
- CanvasGroup 三属性同步初始化：`alpha=0`（不可见）、`interactable=false`（不响应输入）、`blocksRaycasts=false`（不拦截射线）
- `Open()` 动画完成回调中恢复三属性；`Close()` 动画完成回调中重置三属性
- 幂等性通过 `GetComponentInChildren<T>(includeInactive: true)` 实现，重复执行 Build 不会产生重复节点

### 验收状态
- ✅ CanvasGroup 初始化三属性均已设置
- ✅ 幂等性逻辑完整（每个 Section 均有 skip 保护）
- ✅ `SetActive(false)` 在 Build 完成后执行
- ✅ 与 `StarChartPanel.Awake()` 初始化逻辑一致

---

## Bug Fix: C 键无法打开 StarChart — 2026-03-01 01:10

### 修改文件
- `Assets/Scripts/UI/UIManager.cs`

### 根本原因
`UIManager.Awake()` 通过 `FindActionMap("Ship")` + `FindAction("ToggleStarChart")` 拿到了 action 引用，但**从未调用 `shipMap.Enable()`**。`ConfigureUIInputModule()` 只 Enable 了 `UI` map，`Ship` map 始终处于 disabled 状态，导致 C 键的 `performed` 事件永远不触发。

### 修复内容
1. 在 `Awake()` 中 `FindActionMap("Ship")` 之后立即调用 `shipMap.Enable()`
2. 新增 `_inputActions == null` 和 `shipMap == null` 的 null 保护，避免 NullReferenceException 导致整个初始化链断掉

### 验收
- Play Mode 按 C 键 → StarChart 面板正常打开/关闭
- Console 无 NullReferenceException

---

## Bug Fix: StarChartController & EnemyBrain NullReferenceException — 2026-03-01 01:20

### 修改文件
- `Assets/Scripts/Combat/StarChart/StarChartController.cs`
- `Assets/Scripts/Combat/Enemy/EnemyBrain.cs`

### 根本原因

**Bug 1 — StarChartController.cs:196**
`Update()` 中直接访问 `_primaryTrack`、`_secondaryTrack`、`_inputHandler`，但这些字段在 `Awake()` 中初始化。若 `Awake()` 因依赖组件缺失（如 `ShipMotor`/`ShipAiming` 未挂载）中途抛出异常，`_primaryTrack`/`_secondaryTrack` 可能未完成赋值。同时 `_satelliteRunners[i].Tick(dt)` 未做 null 元素保护。

**Bug 2 — EnemyBrain.cs:90**
`Update()` 中直接调用 `_stateMachine.Tick(Time.deltaTime)`，但 `_stateMachine` 在 `Start()` 的 `BuildStateMachine()` 中赋值。若 `_entity.Stats`（Inspector 未赋值的 SO）为 null，`Awake()` 中 `_stats = _entity.Stats` 返回 null，后续 State 构造函数访问 `_stats` 时抛出异常，导致 `_stateMachine` 永远未被赋值，`Update()` 第90行就 NullReferenceException。

### 修复内容

**StarChartController.Update()**
1. 方法开头加 `if (_primaryTrack == null || _secondaryTrack == null) return;` 保护
2. satellite 循环改为 `_satelliteRunners[i]?.Tick(dt)` 防止 null 元素
3. 开火逻辑前加 `if (_inputHandler == null) return;` 保护

**EnemyBrain.Update()**
1. `CheckThreatResponse()` 调用前加 `if (_stateMachine == null) return;` 保护，防止 `BuildStateMachine()` 未完成时 Tick 崩溃

### 验收
- Play Mode 进入场景，Console 无 NullReferenceException
- 武器正常开火，敌人 AI 正常运行

---

## Bug Fix: 按 C 键无法打开星图面板 — 2026-03-01 01:25

### 修改文件
- `Assets/Scripts/Ship/Input/InputHandler.cs`

### 根本原因

`InputHandler.OnDisable()` 中调用了 `shipMap.Disable()`，将整个 Ship ActionMap 禁用。由于 `UIManager` 的 `_toggleStarChartAction` 属于同一个 Ship ActionMap，ActionMap 被禁用后 `ToggleStarChart` Action 也随之失效，导致按 C 键完全没有响应。

触发时机：任何导致 `InputHandler` 组件 `OnDisable()` 被调用的操作（如 GameObject 被 `SetActive(false)`、场景切换等）都会把整个 Ship map 关掉。

### 修复内容

**InputHandler.OnDisable()**
- 移除 `shipMap.Disable()` 调用
- 保留所有事件取消订阅逻辑（`IsFireHeld`/`IsSecondaryFireHeld` 重置保留）
- 添加 `if (_inputActions == null) return;` 保护
- 所有 Action 取消订阅改为 null 条件调用（`?.`）防止 Awake 未完成时 OnDisable 崩溃
- 添加注释说明 Ship ActionMap 生命周期由 UIManager 统一管理

### 架构说明

Ship ActionMap 的 Enable/Disable 生命周期应由 `UIManager.Awake()` 统一管理（已调用 `shipMap.Enable()`）。`InputHandler` 只负责订阅/取消订阅自己关心的 Action 事件，不应干预 ActionMap 的启用状态。

### 验收
- Play Mode 按 C 键正常打开/关闭星图面板
- 飞船移动/射击输入正常
- Console 无报错

---

## Bug Fix: C 键无法打开星图面板（根因修复）— 2026-03-01 01:31

### 修改文件
- `Assets/Scripts/UI/UIManager.cs`

### 根本原因

`UIManager.Start()` 中在调用 `_starChartPanel.Bind(...)` 之后，紧接着调用了 `_starChartPanel.gameObject.SetActive(false)`。

这行代码是**多余且有害的**：
1. `StarChartPanel.Awake()` 已经自行调用 `gameObject.SetActive(false)` 完成初始隐藏，无需 UIManager 重复操作
2. 若 `StarChartPanel` 是 UIManager 所在 Canvas 的子节点，`SetActive(false)` 会触发 Canvas 层级上的 `OnDisable()` 回调，导致 `UIManager.OnDisable()` 被意外执行，从而取消订阅 `_toggleStarChartAction.performed`，之后 C 键永远无响应

### 修复内容

移除 `UIManager.Start()` 中的 `_starChartPanel.gameObject.SetActive(false)` 调用，并添加注释说明原因，防止未来被误加回来。

### 验收
- Play Mode 按 C 键正常打开/关闭星图面板
- Console 无 NullReferenceException
- 飞船移动/射击输入正常

---

## spinning-top.html 全面汉化 — 2026-03-01 10:45

### 修改文件
- `SideProject/spinning-top.html`

### 内容简述
将 HTML 陀螺竞技场游戏的全部用户可见文字从英文汉化为中文，覆盖 7 个模块共计约 200+ 处字符串替换。

### 汉化范围

**Task 1 — HTML 静态结构**
- 标题界面按钮：NEW GAME→新游戏、CONTINUE→继续游戏、TUTORIAL→教程
- 地图界面：NODE MAP→节点地图
- 准备界面三栏：INTEL→情报、LOADOUT→配置、INVENTORY→背包
- 商店界面标题、刷新按钮、返回按钮
- 发射界面标签、战斗界面干预按钮（THRUST/BRAKE/ACTIVATE）
- 结算界面统计标签、跑局结束界面标签
- 状态栏标签（HP/GOLD/FLOOR/NODE）

**Task 2 — PARTS_DB（零件数据库）**
- 全部 28 个零件的 `name` 和 `desc` 字段汉化
- 涵盖 AR 攻击环、WD 重量盘、SG 旋转齿轮、BB 刀刃底座四类

**Task 3 — ENEMIES_DB（敌人数据库）**
- 全部 13 个敌人的 `name`、`desc`、`intelPool` 字段汉化
- 涵盖普通/精英/Boss 三个等级

**Task 4 — EVENTS_DB（事件数据库）**
- 全部 5 个事件的 `title`、`desc`、`choices` 字段汉化
- 包含选项 `text`、`hint` 及 `effect` 回调中的结果消息

**Task 5 — JavaScript 动态 UI 字符串**
- 底座选择弹窗（选择你的底座 / 衰减 / 风格）
- 战斗结果弹窗（出界！/ 爆裂胜利！/ 胜利！/ 失败…）
- 干预技能反馈（⚡ 冲刺！/ 🛑 制动！/ 没有可激活的零件！）
- Canvas 结果文字（出界！/ 爆裂胜利！/ 胜利！/ 失败…）
- 商店购买成功弹窗（HP已恢复 / 购买成功！/ 刷新 30G）
- 背包出售按钮（Sell XXG → 出售 XXG）
- 空背包提示（背包中没有零件）
- 零件槽标签（攻击环 / 重量盘 / 旋转齿轮 / 刀刃底座）
- 空槽占位（— empty — → — 空 —）
- 零件选择弹窗（没有零件 / 卸下 / 取消）
- 商店物品类型（CONSUMABLE → 消耗品）
- 稀有度徽章（common/rare/epic/legendary → 普通/稀有/史诗/传说）
- 发射步骤文字（第一步 — 设定角度 / 第二步 — 设定力度 / 发射中…）
- TIME OUT 弹窗（时间到 — 平局）
- Game Over 弹窗（游戏结束 / 你的陀螺已倒下）
- Settlement Screen 胜负标题（出界！/ 爆裂胜利！/ 胜利 / 失败）
- Run-End Screen 标题（🏆 冠军！/ 💀 落败）
- 敌人预览 Floor 文字（第 X 层）

**Task 6 — TUTORIAL_STEPS（教程系统）**
- 全部 5 个教程步骤的 `title` 和 `body` 字段汉化
- 教程弹窗标题格式：Tutorial (X/5) → 教程 (X/5)
- 按钮文字：Next → → 下一步 →、Skip → 跳过、Start Playing! → 开始游戏！

**Task 7 — 地图节点标签与 generateIntel**
- 节点类型标签：Battle→战斗、Elite→精英、Shop→商店、Event→事件、BOSS→首领
- `generateIntel` 函数全部硬编码字符串汉化
- 原型标签映射：Aggressor→进攻型、Survivor→生存型、Trapper→陷阱型
- 情报条目（衰减缓慢/迅速、攻击力强/弱、精英敌人、首领等）

### 技术方案
- 保持所有代码内部 key（rarity、slot、archetype、NodeType 等）不变，仅替换用户可见的显示字符串
- 稀有度显示通过 `rarityMap` 对象在渲染时动态映射，不修改数据层
- 原型标签通过 `archetypeMap` 对象在 `generateIntel` 中映射

### 验收标准
- 游戏全程无英文用户可见字符串（console.log 除外）
- 所有弹窗、按钮、标签、数据库条目均显示中文
- 游戏逻辑功能不受影响

---

## Fix UICanvasBuilder BuildGatlingCol NullReferenceException — 2026-03-01 10:42

### Modified Files
- `Assets/Scripts/UI/Editor/UICanvasBuilder.cs`

### Summary
Fixed NullReferenceException at line 1013 in `BuildGatlingCol` method caused by adding both `Image` and `TextMeshProUGUI` components to the same `DrumCounter` GameObject. Both inherit from `UnityEngine.UI.Graphic`, so Unity silently returns null on the second `AddComponent` call. Moved the TMP text to a child `Label` object under `DrumCounter`.

### Purpose
Unblock the `ProjectArk → Build UI Canvas` editor menu command so that UIManager and full StarChart UI can be properly built into the scene.

### Technical Approach
- Root cause: `Image` and `TextMeshProUGUI` both extend `Graphic`; Unity forbids two `Graphic` on the same GameObject.
- Fix: Created a child `Label` GameObject under `DrumCounter` to host the `TextMeshProUGUI`, keeping the `Image` background on the parent.

---

## StarChart Debug Code Rollback — 2026-03-01 10:50

### Modified Files
- `Assets/Scripts/UI/UIManager.cs`
- `Assets/Scripts/Ship/Input/InputHandler.cs`

### Summary
Rolled back diagnostic/debug code that was added during the multi-round StarChart "C key not working" investigation. The root cause turned out to be a missing UIManager component in the scene (resolved by rebuilding via `Build UI Canvas`), so all diagnostic instrumentation is now obsolete.

### What Was Rolled Back
- **UIManager.cs**: Removed all `Debug.Log` statements with `[UIManager]` prefix (Awake, OnEnable, Start, OnDisable, OnToggleStarChartPerformed). Removed the entire `Update()` method which contained frame-polling diagnostics and a raw-keyboard fallback.
- **InputHandler.cs**: Removed all `Debug.Log` statements with `[InputHandler]` prefix (Awake, OnEnable, ToggleSpaceLife). Restored `shipMap.Disable()` in `OnDisable()` — InputHandler should stop receiving input when disabled; UIManager re-enables the ActionMap via its own `shipMap.Enable()`.

### What Was Preserved (Intentionally Kept)
- **UIManager.cs**: `shipMap.Enable()` call in Awake (correct architectural responsibility), `ConfigureUIInputModule()` method (ensures InputSystemUIInputModule is wired up), `SetActive(false)` prevention comment in Start, `Debug.LogError`/`Debug.LogWarning` for real configuration errors.
- **InputHandler.cs**: `if (x != null)` null-check pattern replacing `?.performed` (fixes real CS0079 compile error).
- **StarChartController.cs**: Null protection guards in `Update()` (line 196) — defensive programming against missing dependencies.
- **EnemyBrain.cs**: `_stateMachine == null` guards — defensive programming against `BuildStateMachine` failure.
- **UICanvasBuilder.cs**: DrumCounter Image/TMP child object separation — fixes real bug where two `Graphic` components can't coexist on one GameObject.

### Purpose
Clean up Console noise and remove dead diagnostic code while preserving all meaningful bug fixes and architectural improvements made during the investigation.

### Technical Approach
- Selective rollback: evaluated each change individually against "was this diagnostic-only or a real fix?" criteria.
- Restored `shipMap.Disable()` with explanatory comment about UIManager's role in re-enabling the ActionMap.
- Kept `Debug.LogError`/`Debug.LogWarning` for genuine configuration problems (null `_inputActions`, missing ActionMaps).

---

## Bug Fix: Star Chart panel invisible on open (screen freeze) — 2026-03-01 10:54

### Modified Files
- `Assets/Scripts/UI/UIManager.cs`
- `Assets/Scripts/UI/StarChartPanel.cs`
- `Assets/Scripts/UI/StatusBarView.cs`

### Summary
Fixed the screen-freeze bug when pressing C to open the Star Chart panel. The game appeared to freeze because `Time.timeScale = 0` was set **before** `StarChartPanel.Open()`, causing PrimeTween to start animations on not-yet-active GameObjects in the hierarchy. The panel's alpha stayed at 0 (invisible) while the game was paused.

### Root Cause
1. **Timing order in `UIManager.OpenPanel()`**: `Time.timeScale = 0f` was called before `_starChartPanel?.Open()`. Although panel tweens used `useUnscaledTime: true`, the `SetActive(true)` call and subsequent `RefreshAll()` triggered child component initializations while the hierarchy was still settling, producing PrimeTween "Tween is started on GameObject that is not active in hierarchy" warnings for StarChartPanel, StatusLabel, and LoadoutCard.
2. **Zero-duration tween in `UpdateStatusBar()`**: Called `ShowMessage(..., duration: 0f)` which created a `ChainDelay(0)` triggering "Tween duration (0) <= 0" warning.

### Fix
- **`UIManager.OpenPanel()`**: Reordered to open the panel first, then set `Time.timeScale = 0f`, then trigger the weaving transition. This ensures all GameObjects are active in hierarchy when PrimeTween creates its animation sequences.
- **`StarChartPanel.UpdateStatusBar()`**: Replaced `ShowMessage(..., 0f)` with new `SetText()` method that sets text/color instantly without animation.
- **`StatusBarView.SetText()`**: New public method for instant text updates with no tween, avoiding the zero-duration warning.

---

## spinning-top.html 完整性修复（Bug Fix + 全面汉化）— 2026-03-01 11:00

### 修改文件
- `SideProject/spinning-top.html`

### 内容简述
对 `spinning-top.html` 进行了全面的 Bug 修复与 UI 文字汉化，共修复 9 处问题，涵盖游戏流程阻断和英文残留两大类。

### 修复详情

**任务 1 — 修复新游戏流程阻断（最高优先级）**
- 根本原因：Phase 8 中通过 monkey-patch `hideModal` 来触发底座选择弹窗，但教程每个"下一步"按钮的 `onclick` 都会先调用 `hideModal()`，导致两个弹窗互相覆盖，表现为点击"新游戏"页面无反应。
- 修复方案：移除对 `hideModal` 的 monkey-patch，改为在 `showTutorial` 函数内的"跳过"按钮和最后一步"开始游戏！"按钮的 `onClick` 回调中直接调用 `_origShowBaseSelection()`。

**任务 2 — 汉化干预技能 HUD 按钮文字**
- `updateInterventionHUD` 函数中：`Thrust` → `冲刺`，`Brake` → `制动`，`Activate` → `激活`。

**任务 3 — 汉化商店 HP 恢复物品及"继续游戏"无存档提示**
- `generateShopItems`：`HP Restore` → `HP 恢复`，描述改为 `恢复 1 点生命。上限为 3。`
- `btn-continue` click 事件：`No Save` → `无存档`，`No active run found. Start a new game!` → `没有进行中的游戏，请开始新游戏！`

**任务 4 — 汉化 Boss 阶段公告和 Canvas 超时文字**
- `showBattleAnnouncement`：`PHASE 2` → `第二阶段`，`PHASE 3` → `第三阶段`，`CHAOS CHAMPION` → `混沌冠军`。
- `endBattleTimeout` Canvas fillText：`TIME OUT` → `时间到`，`DRAW — Both tops still spinning` → `平局 — 双方陀螺均存活`。

**任务 5 — 汉化零件激活效果和碰撞反馈文字**
- `patchPartActivations`：`Next hit deals +400 RPM damage!` → `下次碰撞造成 +400 RPM 伤害！`，`Next collision absorbed!` → `下次碰撞将被吸收！`，`+300 RPM restored!` → `+300 RPM 已恢复！`，`Enemy slammed to wall!` → `敌方被击飞至边界！`。
- `checkTopCollision` patch：`💥 BURST HIT!` → `💥 爆裂命中！`，`🛑 HIT ABSORBED!` → `🛡️ 命中已吸收！`。

**任务 6 — 修复 Settlement Screen 和 Run-End Screen 初始占位文字**
- `id="settlement-title"` 和 `id="runend-title"` 的初始文字内容由英文 `VICTORY` 改为空字符串，避免页面加载时显示英文残留。

### 目的
修复导致游戏无法正常启动的关键 Bug，并完成全部 UI 文字的中文本地化，提升游戏体验一致性。

### 技术方案
- 新游戏流程修复采用"在回调源头直接调用目标函数"的方式，避免 monkey-patch 全局函数带来的副作用。
- 汉化均为字符串直接替换，不影响逻辑结构。

### Technical Approach
- Panel tweens all use `useUnscaledTime: true` so they animate correctly even after `Time.timeScale = 0` is set.
- Weaving transition also uses unscaled time and is safe to start after timeScale change.

---

## Bug Fix: PrimeTween "not active in hierarchy" warning on StarChartPanel.Open — 2026-03-01 11:00

### Modified Files
- `Assets/Scripts/UI/StarChartPanel.cs`

### Summary
Eliminated the PrimeTween "Tween is started on GameObject that is not active in hierarchy: StarChartPanel" warning that persisted even after the OpenPanel reorder fix. The warning was caused by PrimeTween checking `activeInHierarchy` in the same frame as `SetActive(true)`, before Unity had fully propagated the hierarchy activation.

### Root Cause
When `gameObject.SetActive(true)` is called and a Tween is created in the same synchronous call stack, PrimeTween's internal check sees the target as not yet fully active in the hierarchy. Unity's `activeInHierarchy` may not update until the end of the frame for freshly-activated objects that were previously `SetActive(false)` in `Awake()`.

### Fix
- Split `Open()` into synchronous setup (SetActive, RefreshAll, set alpha=0, fire OnOpened) and an async `PlayOpenAnimationAsync()` method.
- `PlayOpenAnimationAsync()` uses `await UniTask.Yield(PlayerLoopTiming.Update, destroyCancellationToken)` to defer Tween creation by one frame, ensuring the hierarchy is fully active when PrimeTween starts.
- Added `using Cysharp.Threading.Tasks` import.
- Fire-and-forget pattern via `.Forget()` per project async conventions.

---

## Bug Fix: PrimeTween "not active in hierarchy" — Round 2 (Canvas.ForceUpdateCanvases) — 2026-03-01 11:04

### Modified Files
- `Assets/Scripts/UI/StarChartPanel.cs`

### Summary
The previous fix (UniTask.Yield one frame) did not resolve the warning because `activeInHierarchy` was still `false` after the yield — indicating the panel was being deactivated between `Open()` and the next frame. Root cause: Unity defers Canvas hierarchy propagation; `activeInHierarchy` is not guaranteed to be `true` in the same frame as `SetActive(true)` when the object was previously inactive.

### Fix
- Reverted the async `PlayOpenAnimationAsync` approach.
- Restored synchronous Tween creation inside `Open()`.
- Added `Canvas.ForceUpdateCanvases()` immediately after `gameObject.SetActive(true)` and before any Tween creation. This forces Unity to immediately propagate the `SetActive(true)` through the Canvas hierarchy, ensuring `activeInHierarchy == true` when PrimeTween checks it.
- Removed the now-unused `using Cysharp.Threading.Tasks` import.
- Removed the dead `PlayOpenAnimationAsync()` method.

---

## Bug Fix: StarChart UI Layout — Primary/Secondary 上下堆叠 + 面板固定尺寸 — 2026-03-01 11:10

### Modified Files
- `Assets/Scripts/UI/Editor/UICanvasBuilder.cs`

### Summary
StarChart UI 布局与 HTML 原型不符，主要有两个问题：
1. `TracksArea` 内 `PrimaryTrackView` 和 `SecondaryTrackView` 是**左右分割**（各占50%宽度），但原型中应为**上下堆叠**（各占50%高度）。
2. 面板使用 `SetStretch` 铺满全屏，但原型是 980×700 固定尺寸居中显示。
3. `TrackLabel`（PRIMARY [LMB]）与 `ColumnHeader`（SAIL/PRISM/CORE/SAT）都在顶部，重叠显示。

### Fix
- `BuildStarChartSection`：将面板改为 980×700 固定尺寸，锚点居中 (0.5, 0.5)。
- `TracksArea`：Primary 改为上半 `(0,0.52)~(1,1)`，Secondary 改为下半 `(0,0)~(1,0.49)`，中间加水平分割线。
- `BuildTrackView`：将 `TrackLabel` 移到左侧固定列 `(0,0)~(0.08,1)`，4 列 SAIL/PRISM/CORE/SAT 从 x=0.09 开始，与原型的 `track-block-label + track-rows` 布局一致。

---

## Cleanup: 移除无效的 Canvas.ForceUpdateCanvases() 修复 — 2026-03-01 11:19

### Modified Files
- `Assets/Scripts/UI/StarChartPanel.cs`
- `Assets/Scripts/UI/UIManager.cs`

### Summary
发现 StarChartPanel 画面停滞的真正根因是 UICanvasBuilder 在 Build 后调用 `starChartPanel.gameObject.SetActive(false)`，导致面板在场景中默认 inactive。之前针对 PrimeTween "not active in hierarchy" 警告的多轮修复中，`Canvas.ForceUpdateCanvases()` 是无效的（它只刷新 Canvas 布局，无法解决 inactive 问题），属于冗余代码。

### 保留的必要修复
- `UIManager.OpenPanel()`：先调用 `Open()` 再设置 `Time.timeScale = 0f`（确保 Tween 在 timeScale=0 前创建）
- `StarChartPanel.Awake()`：`gameObject.SetActive(false)` 防御性保护
- `StatusBarView.SetText()`：避免 duration=0 触发 PrimeTween 警告的干净 API

---

## SideProject: 陀螺物理模拟改进 — 2026-03-01 11:30

### Modified Files
- `SideProject/spinning-top.html`

### Summary
对 spinning-top.html 的战斗物理系统进行全面改进，解决陀螺运动"像冰壶"、竞技场无向心力、碰撞能量不守恒、无 Magnus 效果等核心缺陷，并汉化元素状态反馈文字。

### 技术方案

**1. ARENA 常量扩展（需求 6）**
新增 6 个物理参数字段，所有魔法数字集中管理：
- `linearDamping: 0.985` — 正常平移衰减系数
- `linearDampingLow: 0.97` — 低 RPM 时加强衰减
- `bowlForce: 0.0008` — 碗形向心力系数
- `bowlForceDeadZone: 30` — 中心死区半径（px）
- `restitution: 0.85` — 碰撞弹性系数
- `magnusTangentRatio: 0.3` — Magnus 切向冲量比例

**2. 平移速度自然衰减（需求 1）**
在 `physicsStep()` 的位置更新后立即对 `vx`/`vy` 乘以衰减系数。RPM < 30% maxRpm 时切换为 `linearDampingLow`（0.97），模拟快倒陀螺的不稳定抖动。零件 `decayMult` 属性同时作用于平移衰减。

**3. 碗形向心力（需求 2）**
在 `physicsStep()` 的 Friction decay 之前计算陀螺到竞技场中心的距离向量，距离 > 30px 时施加 `dist × bowlForce` 的向心加速度。RPM < 40% maxRpm 时向心力系数 ×1.5，模拟快倒陀螺更容易滑向中心。

**4. 标准弹性碰撞公式（需求 3）**
替换原 `impact * 1.4` 魔法系数，改用以 RPM 为质量代理的标准弹性碰撞公式：
`J = -(1 + restitution) × relVn / (1/mP + 1/mE)`
同向自旋时法向冲量 ×0.8（研磨减弱弹射），确保总动能不增加。同时清理了不再使用的 `pRatio`/`eRatio`/`totalRpm`/`totalMass` 变量。

**5. Magnus 切向偏转（需求 4）**
碰撞法线的垂直切向量 `(tx, ty) = (-ny, nx)`。仅在两陀螺自旋方向相反时生效，切向冲量 = `|normalJ| × magnusTangentRatio`，方向由各自 `spinDir`（'right'→正，'left'→负）决定。RPM < 20% maxRpm 时 Magnus 效果 ×0.3。

**6. 元素状态反馈汉化（需求 5）**
- `🔥 BURN!` → `🔥 燃烧！`
- `❄️ FROZEN!` → `❄️ 冻结！`
- `☠️ VENOM!` → `☠️ 中毒！` / `☠️ VENOM x{n}!` → `☠️ 中毒 x{n}！`
- `⚡ SHOCKED!` → `⚡ 麻痹！`
- `🧲 MAGNETIZED!` → `🧲 磁力！`

### 删除的冗余修复
- `StarChartPanel.Open()` 中的 `Canvas.ForceUpdateCanvases()` 调用及其注释（无效且有额外性能开销）
- `UIManager.OpenPanel()` 中过于冗长的历史调试注释（精简为简洁说明）

---

## StarChart UI 布局修复 — 对齐 HTML 原型 (2026-03-01 11:32)

### 修改文件
- `Assets/Scripts/UI/Editor/UICanvasBuilder.cs`

### 内容简述
对 `BuildStarChartSection` 和 `BuildTrackView` 中的所有布局锚点参数进行系统性修正，使 Unity 中的 StarChartPanel 与 `Tools/StarChartUIPrototype.html` 完全一致。

### 目的
消除之前因布局参数不准确导致的：LoadoutSection 过高、InventorySection 被压缩、背包格子过大、TrackLabel 偏宽等视觉问题。

### 技术方案

**1. 整体高度比例（基于面板 700px）**
- Header: `y 0.940 → 1.0`（42px，6%）
- DividerH: `y 0.938 → 0.940`
- LoadoutSection: `y 0.487 → 0.938`（317.5px，45.4%，flex:1）
- SectionDivider: `y 0.485 → 0.487`（1px）
- InventorySection: `y 0.031 → 0.485`（317.5px，45.4%，flex:1）
- StatusBar: `y 0.0 → 0.031`（22px，3.1%）

**2. LoadoutCard 内部比例**
- CardHeader: `y 0.91 → 1.0`（约 28px，8.8%）
- TracksArea: `y 0.0 → 0.90`（剩余 90%）

**3. InventorySection 全宽布局**
- InventoryView 改为全宽：`x 0.01 → 0.99`（原为左 60%）
- ItemDetailView 设为 `SetActive(false)`（HTML 原型无右侧详情面板分割）

**4. 背包格子尺寸**
- `GridLayoutGroup.cellSize`: `(100, 120)` → `(44, 44)`（HTML: `--inv-slot-size: 44px`）
- `GridLayoutGroup.spacing`: `(6, 6)` → `(2, 2)`（HTML: `--slot-gap: 2px`）
- `GridLayoutGroup.constraintCount`: `4` → `12`（全宽约 960px，12列合适）
- `InventoryItemView` prefab `sizeDelta`: `(100, 120)` → `(44, 44)`

**5. TrackLabel 宽度与 TypeColumn 起始位置**
- TrackLabel: `x 0 → 0.08` → `x 0 → 0.07`（HTML: 64px fixed，64/911px ≈ 7%）
- DividerLabel: `x 0.079 → 0.081` → `x 0.069 → 0.071`
- TypeColumn 起始 x: `0.09` → `0.08`，列间距均匀分布（SAIL 0.08-0.29，PRISM 0.30-0.51，CORE 0.52-0.73，SAT 0.74-0.99）

---

## 战斗 Console Log 系统 — 2026-03-01 11:43

### 修改文件
- `SideProject/spinning-top.html`

### 内容简述
在陀螺战斗（spinning-top.html）中实现了完整的战斗 Console Log 系统，所有日志以 `[BattleLog]` 前缀标识，方便在 DevTools 中过滤。

### 目的
帮助战斗策划和开发者在浏览器 DevTools 中实时观察每场战斗的完整过程，尤其是战斗总时长、碰撞频率、RPM 衰减曲线和玩家操作时机。

### 技术方案

**1. `_battleLogger` 数据收集器（需求 7）**
- 在 `_lastTime` 声明旁新增全局 `_battleLogger` 对象和 `_initBattleLogger()` 初始化函数
- 字段：`startTimestamp`、`collisionCount`、`thrustCount`、`brakeCount`、`activateCount`、`elementCounts`（按类型）、`lastSnapshotTime`
- 在 `startBattleLoop()` 开头调用 `_initBattleLogger()` 重置，保证每场战斗数据独立

**2. 战斗开始日志（需求 1）**
- 在 `startBattleLoop()` 中，`requestAnimationFrame` 调用前输出 `console.group('[BattleLog] ⚔️ 战斗开始')`
- 包含：敌人名称/原型、玩家/敌人初始 RPM 及自旋方向、玩家零件列表（AR/WD/SG/BB）、计时器上限

**3. 碰撞事件日志（需求 2）**
- 在 `checkTopCollision` 的 `impact < 0` 分支末尾（`return true` 前）插入日志
- 输出：已用时、碰撞后双方 RPM、oppositeSpins 标志；同步自增 `collisionCount`

**4. 元素状态触发日志（需求 3）**
- 在 `applyElementEffect` 的 5 个 case（fire/ice/poison/thunder/magnet）各自 `break` 前插入日志
- poison 类型区分新增和叠加，额外输出叠加层数；同步自增 `elementCounts[type]`

**5. 干预技能使用日志（需求 4）**
- 在 `doThrust`、`doBrake`、`doActivate` 函数末尾（成功路径）插入日志
- doThrust：已用时 + 剩余次数；doBrake：已用时 + RPM 回复量 + 剩余次数；doActivate：已用时 + 激活零件名称
- 同步自增对应计数器

**6. 逐 5 秒快照日志（需求 5）**
- 在 `battleLoop` 的 `renderArena()` 之后、`timeLeft -= dt` 之前检测 `performance.now() - lastSnapshotTime >= 5000`
- 输出：已用时、双方 RPM 及百分比、双方活跃元素状态列表

**7. 战斗结束摘要日志（需求 6）**
- 在 `endBattle` 和 `endBattleTimeout` 的 `stopBattleLoop()` 调用后立即输出 `console.group('[BattleLog] 🏁 战斗结束')`
- 包含：总时长（`(performance.now() - startTimestamp)/1000`）、胜负结果、结束方式、双方最终 RPM 及百分比、总碰撞次数、干预使用次数（三项）、元素触发次数（按类型）
- `endBattleTimeout` 路径标注 `timeout` 和 `平局`

## 星图多 Loadout 系统 — 2026-03-01 11:54

### 新建文件
- `Assets/Scripts/Combat/StarChart/LoadoutSlot.cs`

### 修改文件
- `Assets/Scripts/Combat/StarChart/StarChartController.cs`
- `Assets/Scripts/Core/Save/SaveData.cs`
- `Assets/Scripts/UI/StarChartPanel.cs`

### 内容简述
实现星图面板 3 套独立 Loadout 配置的完整切换系统。

### 目的
玩家可在星图面板通过 ▲/▼ 按钮在 3 套装备配置间自由切换，每套配置拥有完全独立的 Primary/Secondary Track、LightSail 和 Satellites 数据，切换时 UI 同步刷新，存档持久化全部 3 套配置。

### 技术方案
1. **LoadoutSlot.cs**：新建纯 C# 类，封装单套装备配置（PrimaryTrack、SecondaryTrack、EquippedLightSailSO、EquippedSatelliteSOs），提供 `Clear()` 方法。
2. **StarChartController 重构**：
   - 用 `LoadoutSlot[3]` + `LightSailRunner[3]` + `List<SatelliteRunner>[3]` 替换原有单套字段
   - `PrimaryTrack`/`SecondaryTrack`/`GetEquippedLightSail()`/`GetEquippedSatellites()` 均通过 `ActiveSlot` 属性代理到当前激活槽位
   - 新增 `SwitchLoadout(int index)`：Dispose 旧槽位 Runner → 切换索引 → RebuildSlotRunners → InitializeAllPools
   - Debug Loadout 数据只加载到槽位 0
   - `ExportToSaveData` 序列化全部 3 个槽位；`ImportFromSaveData` 支持新格式（Loadouts 列表）和旧格式（自动迁移到槽位 0）
3. **SaveData.cs 扩展**：新增 `LoadoutSlotSaveData` 类；`StarChartSaveData` 添加 `List<LoadoutSlotSaveData> Loadouts` 字段，旧字段标记 `[Obsolete]` 保留用于迁移。
4. **StarChartPanel 联动**：`SetLoadoutCount(1)` → `SetLoadoutCount(3)`；`HandleLoadoutChanged` 实现完整切换逻辑（SwitchLoadout → TrackView.Bind → UpdateTrackSelection → InventoryView.Refresh → UpdateStatusBar）；`IsItemEquipped` 注释更新为"只检查当前激活槽位"。

---

## Bug Fix: LoadoutSwitcher.Start() 覆盖多Loadout设置 — 2026-03-01 12:00

### 修改文件
- `Assets/Scripts/UI/LoadoutSwitcher.cs`

### 内容简述
移除 `LoadoutSwitcher.Start()` 中硬编码的 `SetLoadoutCount(1)` 调用。

### 目的
`Start()` 中的 `SetLoadoutCount(1)` 会在运行时覆盖 `StarChartPanel.Bind()` 中设置的 `SetLoadoutCount(3)`，导致 ▲/▼ 按钮被禁用，Loadout 切换功能失效。

### 技术方案
删除 `Start()` 方法体，改为注释说明 `SetLoadoutCount` 由外部 `StarChartPanel.Bind()` 调用。`UICanvasBuilder` 已正确 Wire 所有字段（`_loadoutSwitcher`、`_loadoutCard`、`_prevButton`、`_nextButton`、`_drumCounterLabel`），无需额外修改。

---

## Feature: 竞技盘碗形倾斜增强（视觉+物理） — 2026-03-01 12:00

### 修改文件
- `SideProject/spinning-top.html`

### 内容简述
从视觉和物理两个维度同步增强竞技盘的碗形倾斜表现：
1. **ARENA 常量扩展**：新增 `bowlForceQuadratic`（0.000003）、`perspectiveRatio`（0.65）、`contourCount`（4），`bowlForce` 从 0.0008 提升至 0.0015。
2. **碗形透视视觉渲染**：重写 `renderArena()`，使用 `ctx.scale(1, perspectiveRatio)` 将正圆压缩为椭圆；多层径向渐变（边缘深棕 `#1e1408` → 中心暖沙 `#c8a87a`）模拟碗壁光照；4 条同心椭圆等高线（半透明白色）表达坡度层次；12 点方向高光弧 + 6 点方向阴影弧强化立体感；亮色 rim highlight 描边模拟碗口厚度。
3. **陀螺透视映射**：修改 `drawTop()`，距中心 > 30px 时对陀螺施加 Y 轴上移偏移（`dist * 0.15 * perspectiveRatio`）和尺寸缩放（最小 0.85x），模拟碗壁坡度的深度感。
4. **非线性向心力**：修改 `physicsStep()` 向心力段，公式改为 `acc = (bowlForce * dist + bowlForceQuadratic * dist²) * forceMult`，低 RPM（< 40%）系数从 1.5 提升至 1.8，使陀螺在边缘受到更强的向中心拉力。

### 目的
让玩家从视觉上一眼看出竞技盘是朝中心倾斜的碗形，并确保物理上陀螺在无初速度时能明显向中心滑落，视觉与物理感知保持一致。

### 技术方案
- 视觉：利用 Canvas 2D `ctx.scale()` 变换实现椭圆绘制，避免引入 `ctx.ellipse()` 兼容性问题；所有椭圆绘制均在 `ctx.save()/restore()` 块内完成，不影响后续陀螺渲染坐标系。
- 物理：`ARENA.radius`（220px）仍作为物理碰撞边界，视觉椭圆 `radiusX = ARENA.radius` 保持一致，仅 Y 轴压缩为视觉效果，不影响碰撞判定。
- 透视映射仅影响渲染坐标，不修改 `top.x/top.y` 物理坐标，保证物理与视觉解耦。

---

## 战斗弧线增强 + 物理模拟完善 — 2026-03-01 12:30

### 修改文件
- `SideProject/spinning-top.html`

### 内容简述
实现「战斗弧线三件套」（方案D+B+A）及 Magnus 效应物理完善，共5项改动：

1. **方案D：对角出发入场** — 修改 `startBattleLoop()` 中陀螺初始位置，玩家陀螺从左下角（225°方向，距中心 `ARENA.radius * 0.75`）出发，敌人陀螺从右上角（45°方向）对角出发；初始速度方向仍基于 `launchAngle/launchPower` 计算，仅起始坐标改变。

2. **方案B：碰撞冷却帧** — 在陀螺状态对象新增 `_collisionCooldown` 字段；`physicsStep()` 每帧递减；`checkTopCollision()` 中冷却期内跳过 RPM 伤害计算，物理弹开正常执行；冷却时长 30 帧（≈0.5s）。

3. **方案A：濒死保护期** — 新增 `_dyingProtection`、`_dyingProtectionUsed`、`_dyingProtectionStartTime` 字段；`physicsStep()` 每帧检测 `rpm/maxRpm < 0.15`，首次触发时激活保护期；保护期内 RPM 自然衰减降至 30%，碰撞伤害降至 40%；4 秒后自动结束，每场每陀螺只触发一次。

4. **Magnus 效应完善（任务4）** — 在 `checkTopCollision()` 的切向冲量计算段补充同向自旋分支：同向自旋施加方向取反、幅度为 `magnusTangentRatio * 0.6` 的切向冲量；对向自旋保持原有全量逻辑；RPM < 20% 时的 30% 衰减系数对两种情况均生效。

5. **BattleLog 更新（任务5）** — 冷却跳过时输出 `[冷却中-跳过伤害]` 及剩余冷却帧；濒死保护激活/结束时输出陀螺身份、RPM 百分比；`endBattle` 和 `endBattleTimeout` 两处战斗结束摘要均补充：总碰撞次数（含冷却跳过）、首次伤害碰撞时间、濒死保护触发次数。

### 目的
解决战斗 1 秒内结束的体验问题，确保每场战斗有「开场 → 拉锯 → 收尾」三个可感知阶段；同时完善 Magnus 切向偏转物理，使同向/对向自旋产生明显不同的碰撞手感。

### 技术方案
- 冷却帧采用每陀螺独立计数器，冷却期内物理弹开仍执行（防穿模），仅跳过 RPM 伤害，符合真实碰撞后弹开间隔的物理直觉。
- 濒死保护通过 `_dyingProtectionUsed` 标记确保每场只触发一次，保护期结束后恢复正常衰减，不影响 Burst 结算逻辑。
- Magnus 同向自旋切向冲量方向取反（`-pTangentSign`），幅度 60%，与对向自旋形成差异化手感，符合真实陀螺同向摩擦/对向弹射的物理规律。

---

## Feature: 示巴星 13 个星图部件完整实现 — 2026-03-01 12:20

### 新建文件
- `Assets/Scripts/Combat/Projectile/HomingModifier.cs` — 制导棱镜 IProjectileModifier 实现
- `Assets/Scripts/Combat/Projectile/MinePlacerModifier.cs` — 布雷棱镜 IProjectileModifier 实现
- `Assets/Scripts/Combat/StarChart/Satellite/AutoTurretBehavior.cs` — 自动机炮伴星 SatelliteBehavior 实现
- `Assets/Scripts/Combat/Editor/ShebaAssetCreator.cs` — Editor 一键资产创建工具

### 修改文件
- `Assets/Scripts/Combat/Projectile/Projectile.cs` — 新增 `LifetimeRemaining` 可读写属性（供 MinePlacerModifier 延长生命周期）

### 内容简述

**HomingModifier**：继承 `IProjectileModifier`，在 `OnProjectileUpdate` 中每帧用 `Physics2D.OverlapCircleAll` 检测 45° 锥角内最近敌人，以 `_turnSpeed`（默认 180°/s）通过 `Vector2.MoveTowards` 平滑旋转子弹方向，同步更新 `Rigidbody2D.linearVelocity` 和 `transform.rotation`。

**MinePlacerModifier**：继承 `IProjectileModifier`，在 `OnProjectileSpawned` 时将子弹速度归零（`Speed = 0`，`rb.linearVelocity = Vector2.zero`），并将 `Projectile.LifetimeRemaining` 乘以 3 倍延长存活时间。`OnProjectileUpdate` 每帧防漂移保持静止。

**AutoTurretBehavior**：继承 `SatelliteBehavior`，`EvaluateTrigger` 检测 15 单位范围内是否有敌人，`Execute` 从飞船位置向最近敌人发射 Matter 家族低伤害子弹（通过 `PoolManager` 对象池）。`Initialize` 预热对象池，`Cleanup` 清空引用。

**ShebaAssetCreator**：Editor 工具，菜单 `ProjectArk > Create Sheba Star Chart Assets`，幂等创建：
- 3 个新 Modifier Prefab（Modifier_Homing、Modifier_MinePlacer、Sat_AutoTurret）
- 4 个星核 SO（ShebaCore_MachineGun/FocusLaser/Shotgun/PulseWave）
- 6 个棱镜 SO（ShebaP_TwinSplit/RapidFire/Bounce/Boomerang/Homing/MinePlacer）
- 2 个光帆 SO（ShebaSail_Standard/Scout）
- 1 个伴星 SO（ShebaSat_AutoTurret）
- 自动追加所有新 SO 到 PlayerInventory.asset，Console 输出「Created X / Skipped Y」摘要

### 目的
完整实现示巴星关卡的 13 个星图部件，覆盖 4 种核心攻击风格（连发/激光/散弹/波纹）和 6 种棱镜机制（双生/连射/反弹/回旋/制导/布雷），以及斥候帆和自动机炮伴星。

### 技术方案
- HomingModifier 使用 `Vector2.MoveTowards` 而非直接赋值，保证平滑转向不产生跳变；LayerMask 显式声明，禁 `~0`。
- MinePlacerModifier 通过新增的 `Projectile.LifetimeRemaining` 属性修改运行时副本，不触碰 SO 原始数据，遵循「运行时数据隔离」原则。
- AutoTurretBehavior 遵循 SatelliteBehavior IF-THEN 模式，`EvaluateTrigger` 负责条件判断，`Execute` 负责执行，冷却由 SatelliteRunner 统一管理（1.5s）。
- ShebaAssetCreator 复用 Batch5AssetCreator 的 `SetField` 模式（SerializedObject 反射写入私有字段），幂等检查用 `AssetDatabase.LoadAssetAtPath`，库存追加用 `FindAssets("t:X Sheba")` 过滤名称前缀。

---

## 轨道共振破坏系统（方案F） — 2026-03-01 13:40

### 修改文件
- `SideProject/spinning-top.html`

### 内容简述
在陀螺战斗游戏中实现「轨道共振破坏」机制：当双方陀螺连续 180 帧（约 3 秒）保持在竞技场半径 25%～65% 的稳定绕圈距离区间内时，系统自动对双方施加相向冲量（强度 0.22），强制打破稳定轨道，制造碰撞机会。

### 目的
解决战斗中双方陀螺长时间绕圈消耗、缺乏刺激碰撞的问题，形成「绕圈 → 系统扰动 → 相向冲撞 → 弹开 → 再次绕圈」的自然战斗节奏。

### 技术方案
1. **ARENA 常量扩展**：新增 5 个可调参数（`orbitDetectFrames`=180、`orbitDistanceMin`=radius×0.25、`orbitDistanceMax`=radius×0.65、`orbitBreakImpulse`=0.22、`orbitBreakCooldown`=300），其中距离参数使用 getter 动态读取 `this.radius`，支持运行时修改立即生效。
2. **G.battle 状态扩展**：新增 `_orbitStableFrames`（稳定帧计数器）、`_orbitBreakCooldown`（冷却帧倒计时）、`_orbitBreakCount`（触发次数统计），战斗初始化时自动重置为 0。
3. **checkOrbitResonanceBreak 函数**：独立函数，在 `battleLoop` 中 `checkTopCollision` 之后每帧调用。逻辑分层：① 冷却期直接 return；② 濒死保护期冻结计数器（不重置不累加）；③ 距离在区间内则累加稳定帧，否则重置；④ 达到阈值时施加相向冲量、限速（硬上限 9.0 px/frame）、重置计数器并进入冷却期。
4. **BattleLog 集成**：扰动触发时输出 `🌀 轨道扰动` 日志（含时间/距离/冲量/稳定帧数）；战斗结束汇总区块（正常结束和超时结束两处）均追加「轨道扰动触发次数」统计行。

---

## Bug Fix: InventoryView._itemPrefab 为 null 导致星图部件不显示 - 2026-03-01 13:49

### 修改文件
- `Assets/Scenes/SampleScene.unity`（修复 InventoryView 组件序列化字段）

### 内容简述
星图面板底部 Inventory 区域完全空白，无法显示任何部件卡片，尽管 `INVENTORY 21 ITEMS` 状态栏显示数据已正确加载。

### 根本原因
`InventoryView` 组件的 `_itemPrefab` 字段在场景中序列化为 `{fileID: 0}`（null）。`Refresh()` 方法有守卫逻辑：`if (_inventory == null || _itemPrefab == null || _contentParent == null) return;`，导致静默跳过所有渲染，不实例化任何 `InventoryItemView` 卡片。

### 技术方案
直接编辑 `SampleScene.unity` 场景文件，将 `InventoryView` 组件的 `_itemPrefab` 字段从 `{fileID: 0}` 修复为 `InventoryItemView.prefab` 的正确引用（guid: `1761a57fd23df4a5bbbff1b7c05cd6f3`）。

---

## 战斗碰撞修复 + 不规则弹射系统 — 2026-03-01 16:30

### 修改文件
- `SideProject/spinning-top.html`

### 内容简述
修复了导致整场战斗零碰撞的两个根本性 Bug，并新增不规则弹射（菱角效果）系统，使战斗轨迹更多变刺激。

### 根本原因
1. **碰撞检测距离错误**：`collisionDist = 28px`，但两个陀螺 `radius = 18px`，相切距离应为 36px。28 < 36 导致陀螺互相穿透，永远不触发碰撞。
2. **轨道扰动区间失效**：`orbitDistanceMin = 55px`（radius×0.25），但陀螺被向心力拉向中心后实际绕圈距离往往 < 55px，完全不在检测区间内，扰动从未触发。

### 技术方案

**修复1 — 碰撞检测距离**
- `ARENA.collisionDist` 从 `28` 修正为 `36`（两个 radius=18 的陀螺相切距离）
- `checkTopCollision()` 内部改为动态计算 `collisionDist = (p.radius || 18) + (e.radius || 18)`，支持不同半径陀螺
- overlap 分离修正同步使用动态 `collisionDist` 变量

**修复2 — 轨道扰动五项参数**
- `orbitDistanceMin`：`radius * 0.25`（55px）→ `radius * 0.05`（11px），覆盖近距离绕圈
- `orbitDistanceMax`：`radius * 0.65`（143px）→ `radius * 0.85`（187px），覆盖远距离绕圈
- `orbitBreakImpulse`：`0.22` → `0.45`，冲量翻倍确保有效推开
- `orbitDetectFrames`：`180`（3s）→ `120`（2s），更快响应僵局
- `orbitBreakCooldown`：`300`（5s）→ `180`（3s），允许更频繁扰动

**新增3 — 不规则弹射（菱角效果）**
- `ARENA` 常量新增 `irregularDeflectAngle: 20`（度），方便调参
- 在 `checkTopCollision()` 所有物理冲量计算完成后，对玩家和敌人各自独立生成 `±20°` 范围内的随机角度偏转
- 使用旋转矩阵 `(cos θ, -sin θ / sin θ, cos θ)` 应用到速度向量 `(vx, vy)` 上
- 不影响 RPM 伤害计算；`irregularDeflectAngle = 0` 时退化为原有理想弹性碰撞（向下兼容）
- 冷却跳过碰撞时同样应用随机偏转（物理弹射不受冷却影响）

---

## Fix: TypeColumn 提取为独立类 + UICanvasBuilder 自动 wire _itemPrefab — 2026-03-01 16:51

### 修改文件
- **新建** `Assets/Scripts/UI/TypeColumn.cs`
- **修改** `Assets/Scripts/UI/TrackView.cs`
- **修改** `Assets/Scripts/UI/Editor/UICanvasBuilder.cs`

### 问题描述
1. `SailColumn / PrismColumn / CoreColumn / SatColumn` 在 Inspector 中显示 "the associated script cannot be loaded"
2. `InventoryView._itemPrefab` 在 Build UI Canvas 后始终为 Missing

### 根本原因
1. `TypeColumn` 是 `TrackView` 的**内部类（nested class）**。Unity 对内部类 MonoBehaviour 的 GUID 序列化不稳定——当场景被重建时，fileID 哈希可能与已保存场景中的值不一致，导致脚本引用失效。
2. `Build UI Canvas` 工具只创建 InventoryView 节点，但不 wire `_itemPrefab`；该字段只在 `Create InventoryItemView Prefab` 时才被赋值，两步操作之间没有联动。

### 技术方案
1. **提取 TypeColumn 为独立顶级类**：新建 `TypeColumn.cs`，将完整实现从 `TrackView` 内部类移出，保持 `ProjectArk.UI` namespace 不变。`TrackView.cs` 中删除内部类定义，保留对 `TypeColumn` 的字段引用（同 namespace 无需 using）。移除 `TrackView.cs` 中不再需要的 `using PrimeTween`。
2. **UICanvasBuilder 改用独立 TypeColumn**：`BuildTypeColumn()` 方法签名和 `AddComponent<>` 从 `TrackView.TypeColumn` 改为 `TypeColumn`。
3. **Build UI Canvas 自动 wire _itemPrefab**：在 `BuildUICanvas()` 末尾新增 Step 7，检查 `Assets/_Prefabs/UI/InventoryItemView.prefab` 是否存在，若存在则自动 wire 到场景中的 `InventoryView._itemPrefab`。

### 操作指南
- 删除旧 Canvas → 执行 `ProjectArk → Build UI Canvas` → 所有 TypeColumn 脚本引用正确，_itemPrefab 自动 wire（如果 prefab 已存在）
- 若 prefab 不存在，先执行 `ProjectArk → Create InventoryItemView Prefab`，再执行 `Build UI Canvas`

---

## Bug Fix: ScrollArea Mask Image alpha=0 导致 Inventory 部件全部不可见 — 2026-03-01 18:51

### 修改文件
- `Assets/Scenes/SampleScene.unity`（修复 ScrollArea Image 组件 alpha 值）
- `Assets/Scripts/UI/Editor/UICanvasBuilder.cs`（修复代码层，防止重建时复现）

### 问题描述
星图面板底部 Inventory 区域完全空白，数据已正确加载（`INVENTORY 21 ITEMS`），`_itemPrefab` 也已正确赋值，但仍然看不到任何部件卡片。

### 根本原因
`ScrollArea` 节点上的 `Mask` 组件依赖同节点的 `Image` 组件作为 stencil 遮罩。`UICanvasBuilder` 中使用了 `scrollGo.AddComponent<Image>().color = Color.clear`（alpha=0），而 **uGUI Mask 使用 Image 的 alpha 通道作为 stencil buffer**——alpha=0 意味着遮罩完全透明，导致 Mask 裁剪掉所有子内容（ContentParent 下的所有 InventoryItemView 卡片全部被裁掉，不渲染）。

这是 CLAUDE.md 中记录的「常见陷阱第4项：uGUI Mask 裁剪失效（alpha=0）」。

### 技术方案
1. **场景文件直接修复**：将 `SampleScene.unity` 中 `ScrollArea` 的 `Image` 组件（fileID: 1947166889）的 `m_Color.a` 从 `0` 改为 `1`。`Mask.showMaskGraphic = false` 保持不变（视觉上不显示黑色背景，但 stencil 正常工作）。
2. **UICanvasBuilder 代码修复**：将 `scrollGo.AddComponent<Image>().color = Color.clear` 改为显式设置 `alpha=1`，并添加注释说明原因，防止未来重建 Canvas 时复现。

---

## StarChartPanel 初始化修复 — C键首次无效 Bug (2026-03-01 19:30)

### 修改文件
- `Assets/Scripts/UI/StarChartPanel.cs`
- `Assets/Scripts/UI/Editor/UICanvasBuilder.cs`

### 问题描述
每次删除场景 Canvas 并执行 `BuildUICanvas` 后，必须手动在 Hierarchy 中将 `StarChartPanel` 从 inactive 切换为 active，C 键才能正常打开星图面板。

### 根本原因
`UICanvasBuilder.BuildUICanvas()` Step 5 在 Editor 模式下调用 `starChartPanel.gameObject.SetActive(false)`，将 `inactive` 状态序列化进场景文件。进入 Play Mode 后 `Awake()` 不会被调用。当 C 键触发 `Open()` → `gameObject.SetActive(true)` 时，`Awake()` 才首次执行，而 `Awake()` 内部又调用了 `gameObject.SetActive(false)`，导致面板立刻被关闭，C 键永久失效。

### 技术方案
1. **`StarChartPanel.Awake()`**：移除 `gameObject.SetActive(false)`，改为仅初始化 CanvasGroup（alpha=0, interactable=false, blocksRaycasts=false）。新增 `private bool _isOpen = false` 字段追踪面板开关状态。
2. **`StarChartPanel.IsOpen`**：从 `gameObject.activeSelf` 改为返回 `_isOpen` 字段，确保状态判断不依赖 GameObject active 状态。
3. **`StarChartPanel.Open()`**：移除 `gameObject.SetActive(true)`，改为设置 `_isOpen = true`。
4. **`StarChartPanel.Close()`**：移除 `ChainCallback` 和 else 分支中的 `gameObject.SetActive(false)`，改为设置 `_isOpen = false`。
5. **`UICanvasBuilder` Step 5**：移除 `starChartPanel.gameObject.SetActive(false)`，改为获取 CanvasGroup 并设置 alpha=0, interactable=false, blocksRaycasts=false，确保 GameObject 始终保持 active。

### 目的
`StarChartPanel` 在场景中始终保持 active，`Awake()` 在场景加载时正常执行完毕，C 键无需任何手动操作即可立即使用。视觉隐藏完全依赖 CanvasGroup，不再依赖 `SetActive`。

---

## 陀螺碰撞弹开距离修复 2026-03-01 20:20

### 修改文件
- `SideProject/spinning-top.html`

### 问题描述
碰撞后陀螺持续粘连，3.5s 内发生 104 次碰撞（其中 82 次冷却跳过），战斗过快结束。

### 根本原因
1. **位置分离不足**：`overlap * 0.5` 分离后下一帧两陀螺仍重叠，立刻再次触发碰撞
2. **弹性系数偏低**：`restitution: 0.85` 弹开速度不足，加上 `linearDamping` 衰减，陀螺很快又靠近
3. **无最小分离速度保证**：冲量计算后未校验双方是否真正在分离

### 技术方案
1. **`restitution: 0.85 → 1.2`**：超弹性碰撞，确保弹开速度大于碰前接近速度
2. **位置分离量 `overlap * 0.5 → (overlap + 2px) * 0.5`**：额外 2px 间隙，防止浮点误差导致下帧仍重叠
3. **最小分离速度保证**：碰后检查双方沿法线方向的相对速度，若 `sepSpeed < 1.5`，则补充冲量确保最小分离速度

### 目的
确保每次碰撞后陀螺能真正弹开，减少粘连碰撞次数，使战斗时长恢复到合理范围（10s+）。

---

## StarChart Shape-Aware Drag Ghost & Tooltip System — 2026-03-01 20:35

### 新建文件
- `Assets/Scripts/UI/TooltipContentBuilder.cs` — 静态工具类，根据 StarChartItemSO 子类型生成属性行文本（Core/Prism/LightSail/Satellite + HeatCost）

### 修改文件
- `Assets/Scripts/UI/DragDrop/DragGhostView.cs` — 完全重写：新增 `SetShape(int slotSize)` 方法，动态调整 Ghost 高度并生成对应数量的半透明格子网格 Image；新增 `DropPreviewState` 枚举；`Show()` 自动调用 `SetShape()`；`SetDropState()` 更新边框颜色和替换提示
- `Assets/Scripts/UI/DragDrop/DragDropManager.cs` — `BeginDrag()` 调用 `SetShape()`；`HighlightMatchingColumns()` 改用 `SetDropCandidate()` 呼吸脉冲；新增 `PlaySnapInAnimation()` snap-in 弹入动画（1.18→0.96→1.0）；拖拽开始/结束时通知 `ItemTooltipView.SetDragSuppressed()`
- `Assets/Scripts/UI/TypeColumn.cs` — 新增 `SetDropPreview(DropPreviewState)` 列级预览高亮；新增 `SetDropCandidate(bool)` 呼吸脉冲候选高亮（PrimeTween Yoyo 循环）；`Initialize()` 注入 `OwnerColumn` 到每个 cell
- `Assets/Scripts/UI/SlotCellView.cs` — 新增 `OwnerColumn` 属性；`OnPointerEnter()` 同步调用 `OwnerColumn.SetDropPreview()`；`ClearDropTargetNextFrame()` 恢复列预览为 None
- `Assets/Scripts/UI/ItemTooltipView.cs` — 完全重写：UniTask 150ms 延迟显示；PrimeTween 淡入淡出；屏幕边界检测（超出右/下边界自动翻转）；拖拽期间屏蔽显示；`PopulateContent()` 填充图标/名称/类型/属性/描述/装备状态/操作提示
- `Assets/Scripts/UI/TrackView.cs` — 新增 `OnCellPointerEntered(StarChartItemSO, string)` 和 `OnCellPointerExited` 事件；`InitColumn()` 订阅 SlotCellView hover 事件并转发；`HandleCellPointerEnter()` 构建 "PRIMARY · CORE" 格式的位置字符串
- `Assets/Scripts/UI/StarChartPanel.cs` — 新增 `[SerializeField] ItemTooltipView _tooltipView`；新增 `ShowTooltip()`/`HideTooltip()`/`GetEquippedLocation()` 公共 API；`Bind()` 订阅 TrackView 和 InventoryView 的 hover 事件；`OnDestroy()` 取消订阅
- `Assets/Scripts/UI/InventoryView.cs` — 新增 `OnItemPointerEntered`/`OnItemPointerExited` 事件；`Refresh()` 订阅每个 InventoryItemView 的 hover 事件；`ClearViews()` 取消订阅
- `Assets/Scripts/UI/Editor/UICanvasBuilder.cs` — `BuildStarChartSection()` 新增 ItemTooltipView 完整 UI 层级构建（背景/边框/类型徽章/图标+名称行/属性文本/描述/装备状态/操作提示）；Wire `_tooltipView` 到 StarChartPanel

### 内容简述
实现了两大功能：
1. **Shape-Aware 拖拽幽灵**：Ghost 根据 SlotSize 动态调整高度，生成 N 个半透明格子网格；拖拽开始时匹配类型的 TypeColumn 显示呼吸脉冲候选高亮；悬停时 Ghost 边框和列边框同步显示 Valid/Replace/Invalid 三色预览；放置成功后播放 snap-in 弹入动画（scale 1.18→0.96→1.0）
2. **Tooltip 悬停详情卡**：150ms 延迟显示，PrimeTween 淡入淡出，屏幕边界自动翻转，拖拽期间屏蔽；属性内容按类型分别展示（Core 显示 DAMAGE/FIRE RATE/SPEED，Prism 显示 StatModifiers 列表，LightSail 显示条件+效果描述，Satellite 显示触发+动作+冷却）

### 目的
对齐 StarChartUIPrototype.html 原型的两大核心交互差距：异形部件拖拽系统和 Tooltip 悬停详情卡。

### 技术方案
- **DragGhostView.SetShape()**：动态销毁/重建 GhostCell_N Image 数组，每个 cell 使用 `anchorMin=(0,1), anchorMax=(1,1), pivot=(0.5,1)` 从顶部向下排列，高度 = N×cellHeight + (N-1)×cellGap
- **TypeColumn.SetDropCandidate()**：PrimeTween `Tween.Color` Yoyo 循环（cycles=-1），dim→typeColor(0.6α)→dim，duration=0.7s
- **ItemTooltipView**：`UniTask.Delay(150ms)` 替代 Coroutine；`CanvasGroup.alpha` 控制显隐（永不 SetActive(false)）；屏幕边界检测通过 `Screen.width/height` 与 tooltip 尺寸比较后翻转
- **TooltipContentBuilder**：C# pattern matching `switch(item)` 分派到各子类型的 BuildXxxStats 方法，StringBuilder 拼接多行属性文本

---

## Bug Fix: ItemTooltipView 使用旧 Input API 导致异常 — 2026-03-01 21:11

### 修改文件
- `Assets/Scripts/UI/ItemTooltipView.cs`

### 问题描述
`PositionNearMouse()` 方法中使用了 `UnityEngine.Input.mousePosition`，项目已切换至 New Input System，导致运行时抛出 `InvalidOperationException`。

### 修复方案
- 添加 `using UnityEngine.InputSystem;`
- 将 `Input.mousePosition` 替换为 `Mouse.current.position.ReadValue()`
- 添加 `Mouse.current == null` 守卫，防止无鼠标设备时崩溃

---

## Tooltip 定位修复（坐标系不匹配）— 2026-03-01 21:18

### 修改文件
- `Assets/Scripts/UI/ItemTooltipView.cs`

### 问题描述
`PositionNearMouse()` 经历了两次错误修复：
1. 第一次将 `rect.localPosition` 改为 `rect.anchoredPosition`，但 `ScreenPointToLocalPointInRectangle` 的参考矩形是根 Canvas，而 `anchoredPosition` 是相对于父节点的坐标，两者坐标系不一致导致位置偏移。
2. 第二次改为用父节点作为参考矩形，但 `anchoredPosition` 还受 anchor 设置影响，tooltip 完全不可见。

### 修复方案
改用 `ScreenPointToWorldPointInRectangle` + `rect.position`（世界坐标），完全绕开 `anchoredPosition` 和 anchor/pivot 偏移的影响。无论 tooltip 嵌套多深、anchor 如何设置，世界坐标都能正确对应鼠标位置。

---

## SpaceLife 过渡动画优化 — 2026-03-01 23:21

### 修改文件
- `Assets/Scripts/SpaceLife/TransitionUI.cs`
- `Assets/Scripts/SpaceLife/SpaceLifeManager.cs`

### 内容简述
将按 Tab 进入/退出 SpaceLife 的过渡动画从"打字机文字 + 长等待"重构为"快速淡黑→切换→淡出"三段式结构，总时长约 300ms，与按 C 打开星图的手感对齐。

### 目的
消除拖泥带水的打字机动画（原约 3-4 秒），提升操作响应感。

### 技术方案
**TransitionUI.cs**：
- 移除 `_centerText`、`_typewriterSpeed`、`_textDisplayDuration` 字段
- 移除 `TypeTextAsync`、`PlayEnterTransitionAsync`、`PlayExitTransitionAsync`、`PlayTransitionAsync` 方法
- `_fadeDuration` 默认值从 0.3f 改为 0.15f
- `FadeOutAsync`/`FadeInAsync` 签名简化为无 `duration` 参数，使用内部 `_fadeDuration`，`useUnscaledTime: true`

**SpaceLifeManager.cs**：
- 移除 `_enterText`/`_exitText` Inspector 字段
- `EnterSpaceLifeAsync`/`ExitSpaceLifeAsync` 改为三段式：`FadeOutAsync` → 场景切换（摄像机/根节点/InputHandler 切换）→ `FadeInAsync`
- 场景切换操作夹在两段动画之间，避免视觉撕裂
- 保留 `_isTransitioning` 保护逻辑防止重复触发

## StarChart Tooltip 视觉优化 — 2026-03-01 23:30

### 修改文件
- `Assets/Scripts/UI/Editor/UICanvasBuilder.cs`
- `Assets/Scripts/UI/ItemTooltipView.cs`

### 内容简述
放大 StarChart Tooltip 整体尺寸与所有文字字号，并将背景色从白色改为科技感深蓝色风格。

### 目的
提升星图 Tooltip 的可读性与视觉质感，使其与游戏整体科技感 UI 风格一致。

### 技术方案
**UICanvasBuilder.cs**：
- `tooltipRect.sizeDelta`：`(220, 180)` → `(280, 230)`
- `TooltipBackground` 背景色：`(0.05, 0.07, 0.11, 0.97)` → `(0.04, 0.07, 0.14, 0.97)`（更深的科技蓝）
- `TooltipBorder` 边框色：StarChartTheme.Border → `(0.2, 0.6, 1.0, 0.85)`（亮蓝色边框）
- `TypeBadgeBackground` 背景色：`(0, 0.85, 1, 0.12)` → `(0, 0.5, 1, 0.15)`
- `NameText` 字号：14 → 17
- `TypeText` 字号：10 → 12
- `StatsText` 字号：11 → 13
- `DescriptionText` 字号：10 → 12
- `EquippedStatusText` 字号：10 → 12
- `ActionHintText` 字号：9 → 11

**ItemTooltipView.cs**：
- `_tooltipWidth` 默认值：220f → 280f
- `_tooltipHeight` 默认值：180f → 230f（与新尺寸同步，确保边界检测正确）

---

## 战斗高光时刻（Bullet Time + 镜头聚焦）— 2026-03-01 23:37

### 修改文件
- `SideProject/spinning-top.html`

### 内容简述
在陀螺对战游戏中实现"高光时刻"效果：两陀螺即将碰撞前触发时间减速（Bullet Time）+ 镜头推近聚焦，增强战斗戏剧性。

### 目的
提升战斗画面表现力，在关键碰撞时刻给玩家强烈的视觉冲击感。

### 技术方案

**参数配置（ARENA 常量新增）**：
- `highlightCooldown: 4.0s`、`highlightSlowScale: 0.2`、`highlightSlowInDuration: 0.15s`
- `highlightSlowHoldDuration: 0.6s`、`highlightSlowOutDuration: 0.2s`
- `highlightTriggerDistance: 80px`、`highlightPendingWaitMax: 3.0s`
- `highlightDmgThreshold: 0.10`（伤害 ≥ 敌方当前 RPM × 10% 触发）
- `highlightZoomTargetRatio: 0.75`（两陀螺占屏幕宽度 75%）

**状态管理（highlightState 全局对象）**：
- 包含 `active`、`phase`（idle/slowIn/hold/slowOut）、`timeScale`、`cameraZoom`、`cameraOffsetX/Y`、`scheduledTimes[]`、`pendingIndex`、`cooldownTimer` 等字段
- `resetHighlightState()` 在每局战斗开始时重置全部状态

**保底触发（scheduleHighlightTimes）**：
- 战斗开始时随机生成 2～3 个触发时间点，分别落在前期（15%～33%）、中期（40%～60%）、后期（67%～85%）
- 每帧在 `updateHighlightMoment()` 中检测是否到达时间点，到达后检查距离 ≤ 80px 则触发，否则等待最多 3 秒后跳过

**伤害触发**：
- 在 `checkTopCollision()` 碰撞伤害计算后，判断 `eDmgDealt >= ePrevRpm * 0.10` 则调用 `triggerHighlight()`

**Bullet Time（updateHighlightMoment）**：
- slowIn 阶段（0.15s）：timeScale 从 1.0 线性插值到 0.2
- hold 阶段（0.6s）：保持 timeScale = 0.2，实时跟踪两陀螺中心点
- slowOut 阶段（0.2s）：timeScale 从 0.2 线性插值回 1.0，结束后进入 4s 冷却
- `battleLoop` 中 `dt = realDt * highlightState.timeScale`，战斗计时器使用 `realDt` 避免冻结

**镜头聚焦（renderArena）**：
- 高光时刻期间在 `renderArena()` 开头应用 `ctx.translate(camOX, camOY) + ctx.scale(camZoom, camZoom)`
- RPM bars（HUD）在 `ctx.restore()` 之后绘制，不受缩放影响
- 额外添加暗角（vignette）叠加层，强度随 timeScale 降低而增强

**边界保护**：
- `endBattle()` 开头强制重置所有高光状态（timeScale=1, zoom=1, offset=0）
- 高光 `active` 期间跳过轨道扰动逻辑（`if (!highlightState.active)` 守卫）
- 战斗结束日志新增"高光时刻触发次数"统计

---

## Slot 尺寸放大（Track + Inventory 统一放大至 80×80）— 2026-03-01 23:44

### 修改文件
- `Assets/Scripts/UI/Editor/UICanvasBuilder.cs`

### 内容简述
将星图面板中 Track 装备槽与背包 Inventory 槽的尺寸统一放大一倍，并保持两者一致。

### 目的
原有 slot 尺寸（40×40 / 44×44）偏小，图标内容难以辨认；放大后视觉更清晰，两区域尺寸统一，提升整体 UI 一致性。

### 技术方案
1. **Track Slot**：`TypeColumn` 内 `GridContainer` 的 `GridLayoutGroup.cellSize` 从 `(40, 40)` 改为 `(80, 80)`；`constraintCount = 2`（2×2 布局）保持不变；锚点比例代码未触碰。
2. **Inventory Slot**：`InventoryView` 的 `GridLayoutGroup.cellSize` 从 `(44, 44)` 改为 `(80, 80)`；`constraintCount` 从 `12` 改为 `8`（防止内容溢出，新宽度 (80+2)×8+12 = 668px）；`spacing (2, 2)` 与 `padding (6, 6, 6, 6)` 保持不变。
3. **InventoryItemView Prefab**：根节点 `sizeDelta` 从 `(44, 44)` 改为 `(80, 80)`，与 `GridLayoutGroup.cellSize` 保持同步；内部子元素（图标、名称标签、类型点）均使用相对锚点，无需额外调整。
















---

## Bug Fix: StarChart 拖拽第一次无效（DragGhostView Awake 自我关闭三连锁）— 2026-03-02 16:45

### 修改文件
- `Assets/Scripts/UI/DragDrop/DragGhostView.cs`
- `Assets/Scripts/UI/Editor/UICanvasBuilder.cs`

### 根本原因
`DragGhostView.Awake()` 末尾调用了 `gameObject.SetActive(false)`，而 `UICanvasBuilder` 在构建时也调用了 `ghostGo.SetActive(false)`。

这触发了 CLAUDE.md 常见陷阱第11条的经典三连锁：
1. `UICanvasBuilder` 调用 `ghostGo.SetActive(false)` → Ghost 处于 inactive，Awake 被推迟
2. 第一次拖拽时 `DragGhostView.Show()` 调用 `gameObject.SetActive(true)` → **此时 Awake 才第一次执行**
3. `Awake()` 末尾调用 `gameObject.SetActive(false)` → **Ghost 立刻被关掉**
4. 结果：第一次拖拽 Ghost 不可见，拖拽无响应；第二次拖拽 Awake 已执行完毕，正常工作

### 修复方案
- **`DragGhostView.cs`**：`Awake()` 末尾改为 `_canvasGroup.alpha = 0f` 隐藏 Ghost，不再调用 `SetActive(false)`；`Show()` 改为 `_canvasGroup.alpha = _ghostAlpha` 显示；`Hide()` 改为动画结束后 `_canvasGroup.alpha = 0f` + 重置 scale，不再调用 `SetActive(false)`；可见性检查从 `activeSelf` 改为 `_canvasGroup.alpha <= 0f`
- **`UICanvasBuilder.cs`**：移除 `ghostGo.SetActive(false)` 调用，Ghost GameObject 始终保持 active，由 CanvasGroup.alpha 控制显隐

### 验收标准
- 第一次拖拽库存部件时 Ghost 立即出现并跟随鼠标 ✓
- 拖拽结束后 Ghost 正常消失 ✓
- 多次拖拽行为一致 ✓

---

## Bug Fix: DragGhostView 第一次拖拽 Ghost 不显示（localScale=0 持久化）— 2026-03-02 17:25

### 修改文件
- `Assets/Scripts/UI/DragDrop/DragGhostView.cs`

### 根本原因（通过 Unity MCP 运行时诊断确认）
MCP 诊断发现 Ghost 在 Play Mode 启动后 `localScale=(0,0,0)`，这是 `Hide()` 动画的遗留状态：

1. `Bind()` 调用 `Hide()` 时，Ghost 是 active 的（Awake 刚执行完，SetActive(false) 还没来得及执行）
2. `Hide()` 启动缩小到 `Vector3.zero` 的 tween，`OnComplete` 调用 `SetActive(false)`
3. `localScale=(0,0,0)` 被持久化到 Ghost 上
4. 第一次 `Show()` 时：`_scaleTween.Stop()` 停止 tween → PrimeTween 的 Stop() 可能触发 OnComplete → `SetActive(false)` 再次执行 → Ghost 被关掉

### 修复方案
两处修改：
1. **`Show()` 里**：把 `_scaleTween.Stop()` 移到 `SetActive(true)` 之前，并在 `SetActive(true)` 之前先重置 `localScale = Vector3.one`，确保 Ghost 从干净状态开始
2. **`Hide()` 的 `OnComplete` 里**：在 `SetActive(false)` 后加 `localScale = Vector3.one` 重置，防止 scale=0 状态持久化到下次 Show()

### 验收标准（MCP 运行时验证）
- Show() 后：activeSelf=True, localScale=(0.8,0.8,0.8), alpha=0.70 ✅
- 第一次拖拽 Ghost 正常出现 ✅

---

## CLAUDE.md 更新：Unity MCP 工具使用指南 — 2026-03-02 17:22

### 修改文件
- `CLAUDE.md`

### 内容
在"Unity 编辑器操作边界"章节后新增独立章节"Unity MCP 工具使用指南"，包含：
1. **能力边界总览表**：覆盖全部 MCP 工具（Console / 场景 / GameObject / 资产 / 脚本 / 截图 / 编辑器状态 / 反射 / 包管理 / 测试）
2. **Project Ark 专项推荐场景 7 条**：Bug 排查、场景序列化验证、运行时数据污染检查、对象池回收验证、一次性批量检查（script-execute）、UI 视觉验证、单元测试
3. **使用原则 5 条**：Play Mode 前确认状态、script-execute 临时性、reflection 慎用等

### 目的
将 Unity MCP 的使用规范固化进项目开发规范，让 AI 在后续开发中能主动利用 MCP 闭环验证，减少"写代码 → 用户手动验证 → 截图反馈"的低效回路。

### 技术
无代码变更，纯文档更新。

---

## StarChart UI 完善 — 对齐 HTML 原型（需求1/2/3/4/7/8/9）— 2026-03-03 14:40

### 新建文件
- `Assets/Scripts/Combat/StarChart/ItemShapeHelper.cs` — 2D 形状坐标工具类

### 修改文件
- `Assets/Scripts/Combat/StarChart/StarChartEnums.cs` — 新增 `ItemShape` 枚举（5种形状）和 `InventoryFilter` 枚举
- `Assets/Scripts/Combat/StarChart/StarChartItemSO.cs` — 新增 `[SerializeField] ItemShape _shape` 字段及 `Shape` 属性
- `Assets/Scripts/Combat/StarChart/SlotLayer.cs` — 完全重构为 2D 矩阵网格，新增 `CanPlace`/`TryPlace`/`Remove`/`GetAt`/`GetAnchor` API
- `Assets/Scripts/UI/TrackView.cs` — `RefreshColumn` 改为 2D 矩阵遍历，新增 `SetShapeHighlight` 和 `FindFirstAnchor` 方法
- `Assets/Scripts/UI/DragDrop/DragGhostView.cs` — 新增 `SetShape(ItemShape)` 重载，`SetDropState` 新增 `evictCount` 参数，边框颜色改用 PrimeTween 过渡，替换提示文字改为 `↺ 替换 N 个部件`
- `Assets/Scripts/UI/DragDrop/DragDropManager.cs` — `UpdateGhostDropState` 新增 `evictCount` 参数；拖拽 Slot 时显示卸装提示
- `Assets/Scripts/UI/SlotCellView.cs` — `UpdateGhostDropState` 调用传入 `evictCount`
- `Assets/Scripts/UI/InventoryItemView.cs` — 新增 `BuildShapePreview(ItemShape)` 生成半透明格子预览
- `Assets/Scripts/UI/LoadoutSwitcher.cs` — 完全重写：Drum Counter 翻牌动画；PlaySlideAnimation 并行 Scale 纵深；RENAME/DELETE/SAVE CONFIG 三个管理按钮
- `Assets/Scripts/UI/StatusBarView.cs` — 完整提示文字；新增 `ShowPersistent` 和 `RestoreDefault` 方法

### 内容简述
对齐 StarChartUIPrototype.html 原型，完成 7 项差距修复：异形部件 2D Shape 系统、Loadout 管理 UI、Drum Counter 翻牌动画、Loadout Card Scale 纵深感、拖拽预览高亮三态、库存过滤器顺序规范化、状态栏文字补全。

### 目的
让 StarChart UI 在功能完整性和视觉表现上全面对齐 HTML 原型，提升策略深度与操作手感。

### 技术方案
- `ItemShapeHelper.GetCells(ItemShape)` 返回预分配的 `Vector2Int[]` 偏移数组，零 GC
- `SlotLayer<T>` 内部 `T[GRID_ROWS, GRID_COLS]` 二维数组 + `Dictionary<T, Vector2Int>` 锚点缓存
- `DragGhostView.RebuildShapeGrid` 按 bounding box 动态生成格子 GameObject
- `LoadoutSwitcher.PlayDrumFlip` 使用 `Sequence.Group` 并行驱动前后两层 `LocalEulerAngles`
- 所有补间使用 `useUnscaledTime: true`，兼容暂停状态
- 遵循 CLAUDE.md：CanvasGroup 控制显隐，禁止 `SetActive(false)`；PrimeTween 替代手写 Lerp

---

## StarChart UI 完善 — Code Review 修复 2026-03-03 15:10

### 修改文件
- `Assets/Scripts/UI/DragDrop/DragGhostView.cs`
- `Assets/Scripts/UI/LoadoutSwitcher.cs`
- `Assets/Scripts/UI/StatusBarView.cs`
- `Assets/Scripts/Combat/StarChart/ItemShapeHelper.cs`
- `Assets/Scripts/UI/TrackView.cs`

### 内容简述
对 StarChart UI 完善模块进行 Code Review 后，修复了 2 个 Major 问题、3 个 Minor 问题和 1 个 Info 问题：

**Major-1 修复（DragGhostView.cs）**：`_replaceHintLabel` 和 `_nameLabel` 原先使用 `gameObject.SetActive(false/true)` 控制显隐，违反 CLAUDE.md 第11条。改为在 `Awake` 中为两个 TMP_Text 各自获取/添加 `CanvasGroup`，通过 `alpha = 0/1` 控制可见性，`blocksRaycasts = false` 防止遮挡交互。

**Major-2 修复（LoadoutSwitcher.cs）**：`ConfirmRename` 同时订阅了 `onSubmit` 和 `onDeselect`，按下 Enter 时两者同时触发导致双调用（事件触发两次、SaveManager 写入两次）。添加 `_isConfirmingRename` bool 防重入 flag，第二次调用直接 return。

**Minor-5 修复（StatusBarView.cs）**：`ShowIdle()` 直接将 `_idleText` 赋给 `_label.text`，导致玩家看到 `"EQUIPPED {0}/10 · INVENTORY {1} ITEMS · ..."` 原始占位符字符串。添加 `_equippedCount`/`_inventoryCount` 字段和 `SetCounts(int, int)` 公共方法，`ShowIdle()` 改为 `string.Format(_idleText, _equippedCount, _inventoryCount)`。

**Minor-3 修复（ItemShapeHelper.cs）**：`GetAbsoluteCells` 每次调用都 `new List<Vector2Int>`，在拖拽 hover 热路径上产生 GC 压力。添加接受 `List<Vector2Int> result` 输出参数的重载，调用方可复用缓存列表，原有重载保留向后兼容。

**Minor-4 修复（TrackView.cs）**：`SetShapeHighlight` 中使用 `SlotLayer<StarChartItemSO>.GRID_COLS` 访问常量，泛型类型参数与常量无关，语义混淆。提取为 `const int gridCols = SlotLayer<StarChartItemSO>.GRID_COLS` 局部常量，并添加注释说明。

**Info-2 修复（LoadoutSwitcher.cs）**：`OnDestroy` 原先只停止 PrimeTween 序列，未取消 Button 的 `onClick` 和 InputField 的 `onSubmit`/`onDeselect` 监听器。补充 `RemoveAllListeners()` 调用，符合 CLAUDE.md 架构原则（事件卫生）。

### 目的
消除 Code Review 发现的规范违规和功能性 bug，提升代码健壮性和可维护性。

### 技术方案
- CanvasGroup 替代 SetActive：在 Awake 中 `GetComponent<CanvasGroup>() ?? AddComponent<CanvasGroup>()`，alpha 控制显隐
- 防重入 flag：`_isConfirmingRename` bool，进入时置 true，退出时置 false
- string.Format 填充占位符：`SetCounts` 更新计数，`ShowIdle` 格式化输出
- GC 友好重载：`GetAbsoluteCells(shape, col, row, List<Vector2Int> result)` 原地填充
- 局部常量消歧：`const int gridCols = SlotLayer<StarChartItemSO>.GRID_COLS`

---

## UICanvasBuilder 补全 LoadoutSwitcher 连线 — 2026-03-03 15:30

### 修改文件
- `Assets/Scripts/UI/Editor/UICanvasBuilder.cs`

### 内容简述
`BuildGatlingCol` 中的 DrumCounter 重构为双层翻牌结构，新增 RENAME/DELETE/SAVE 三个管理按钮、LoadoutNameLabel、RenameInputField，并补全所有新 SerializeField 的 WireField 连线。同时在 BuildStarChartSection 末尾补充 `LoadoutSwitcher._statusBar` 的跨引用连线。

### 目的
UICanvasBuilder 一键重建后，LoadoutSwitcher 的所有字段均自动连线，无需手动在 Inspector 中拖拽。

### 技术方案
- DrumCounter 拆分为 DrumContainer（RectTransform，用于 PrimeTween LocalEulerAngles 翻转）+ DrumFront + DrumBack 两个 TMP_Text 子层
- RenameInputField 初始 CanvasGroup.alpha=0，符合 CLAUDE.md 第11条（禁止 SetActive）
- DELETE 按钮背景色红色调，SAVE 按钮背景色绿色调，视觉区分危险/安全操作
- `_statusBar` 在 BuildStarChartSection 末尾统一连线，避免 BuildGatlingCol 内部依赖 statusBarView 变量作用域问题

---

## StarChart 部件异形格子配置 — 2026-03-03 15:35

### 修改文件
- `Assets/_Data/StarChart/Cores/ShebaCore_MachineGun.asset`
- `Assets/_Data/StarChart/Cores/ShebaCore_Shotgun.asset`
- `Assets/_Data/StarChart/Cores/EchoCore_BasicWave.asset`
- `Assets/_Data/StarChart/Cores/ShebaCore_PulseWave.asset`
- `Assets/_Data/StarChart/Prisms/FractalPrism_TwinSplit.asset`
- `Assets/_Data/StarChart/Prisms/ShebaP_Homing.asset`
- `Assets/_Data/StarChart/Prisms/ShebaP_MinePlacer.asset`

### 内容简述
将 7 个原本全为 Shape1x1 的部件改为异形，以便测试 SlotLayer 2D 网格放置逻辑。

### 目的
所有部件均为 1×1 时，SlotLayer 的多格占用、碰撞检测、L形/2×2 放置路径永远不会被触发，无法验收 StarChart UI 完善模块的核心功能。

### 技术方案
基于 DPS 数值演算（BaseDamage × FireRate）和语义合理性，按"格子越多=越强/越特殊"原则分配形状：

| 部件 | 形状 | 格数 | 理由 |
|------|------|------|------|
| ShebaCore_MachineGun | Shape1x2H | 2 | 高频连射(DPS=60)，枪管横向延伸 |
| ShebaCore_Shotgun | ShapeL | 3 | 散弹近战爆发(spread=30°)，结构复杂 |
| EchoCore_BasicWave | Shape2x1V | 2 | AOE穿墙波(DPS=30)，纵向扩散 |
| ShebaCore_PulseWave | Shape2x2 | 4 | 超强击退AOE(knockback=2.5)，最强控场 |
| FractalPrism_TwinSplit | Shape1x2H | 2 | +2弹数分裂，横向光路分叉 |
| ShebaP_Homing | Shape2x1V | 2 | 追踪导引需要额外传感器空间 |
| ShebaP_MinePlacer | ShapeL | 3 | 地雷部署机构，结构最复杂 |

`_shape` 字段直接写入 .asset 序列化文件（整数枚举值：Shape1x2H=1, Shape2x1V=2, ShapeL=3, Shape2x2=4），同步更新 `_slotSize` 保持 legacy 兼容。

---

## 移除 ItemTooltipView 中的诊断 Debug.Log — 2026-03-03 17:07

### 修改文件
- `Assets/Scripts/UI/ItemTooltipView.cs`

### 内容简述
删除了 `PositionNearMouse()` 方法中每帧通过 `Update()` 触发的 4 条诊断日志（`Debug.Log`/`Debug.LogWarning`），这些日志严重污染了 Console 并降低了编辑器性能。

### 目的
消除每帧日志刷屏问题，使 Console 恢复可用状态，并降低编辑器性能开销。

### 技术方案
直接删除 `PositionNearMouse()` 中的 4 条日志语句（2× `Debug.Log`、2× `Debug.LogWarning`），必要处替换为静默注释。不影响 tooltip 定位逻辑的任何行为。

---

## 修复 DragGhostView 中 ReplaceHint 的 MissingComponentException — 2026-03-03 17:12

### 修改文件
- `Assets/Scripts/UI/DragDrop/DragGhostView.cs`

### 场景变更
- `ReplaceHint` GameObject：添加 `CanvasGroup` 组件，将 `activeSelf` 设为 `true`（原为 false）

### 内容简述
修复了 `DragGhostView.Awake()` 第 91 行在尝试对 `ReplaceHint` 子 GameObject 设置 `CanvasGroup.alpha` 时抛出的 `MissingComponentException`。

### 目的
消除阻止 DragGhost 正常初始化的运行时异常。

### 技术方案
1. **根因**：`ReplaceHint` GameObject 没有 `CanvasGroup` 组件且处于 `SetActive(false)` 状态。代码中的 `AddComponent<CanvasGroup>()` 回退逻辑在非激活对象上执行失败，产生 `MissingComponentException`。
2. **场景修复**：直接在场景中为 `ReplaceHint` 添加 `CanvasGroup` 组件，并将 `activeSelf` 设为 `true`，`CanvasGroup.alpha = 0`（遵循 CLAUDE.md 第 11 条：禁止用 SetActive 控制 UI 显隐）。
3. **代码修复**：在 `Awake()` 中为 `_replaceHintCg` 的使用添加 null 守卫，并配合 `Debug.LogError` 回退，防止未来出现静默失败。

---

## 异形背包格子 — 背包按 HTML 原型显示部件形状 — 2026-03-03 17:16

### 修改文件
- `Assets/Scripts/UI/InventoryView.cs`
- `Assets/Scripts/UI/InventoryItemView.cs`

### 内容简述
背包现在以实际多格尺寸显示异形部件（1×2H、2×1V、L 形、2×2），与 HTML 原型的 CSS Grid `grid-column: span N / grid-row: span N` 行为一致。此前所有部件无论形状如何都显示为统一的 1×1 方块。

### 目的
用户反馈背包中所有部件都显示为 1×1，而拖拽 Ghost 已能正确显示形状。背包格子需要像 HTML 原型一样在视觉上区分异形部件。

### 技术方案

**InventoryView.cs — 自定义行优先排列布局：**
1. 用自定义排列算法替代 Unity 的 `GridLayoutGroup`（后者强制统一 cellSize）。
2. 新增序列化字段：`_gridColumns`（8）、`_cellSize`（80）、`_cellGap`（2）、`_gridPadding`（6）——与原有 GridLayoutGroup 设置保持一致。
3. 在 `Refresh()` 中：运行时禁用 `GridLayoutGroup` 和 `ContentSizeFitter`，然后用二维布尔占位网格手动定位每个部件。
4. `TryFindPosition()`：逐行逐列扫描，找到第一个能容纳部件 bounding box 的可用位置（行优先排列，与 CSS Grid auto-flow 一致）。
5. 每个 `InventoryItemView` 的 `RectTransform.sizeDelta` 设为 `(spanCols × cellSize + gaps, spanRows × cellSize + gaps)`。
6. 内容高度根据最高占用行计算，并应用到 content RectTransform，确保 ScrollRect 正常工作。

**InventoryItemView.cs — 增强形状预览：**
1. `BuildShapePreview()` 现在为多格形状渲染完整的 bounding box 网格（而非仅渲染 active cells）。
2. Active cells 使用类型颜色着色（通过 `StarChartTheme.GetTypeColor()`），透明度 25%——与 HTML 的 `.inv-shape-active-{type}` 类一致。
3. Bounding box 中的空格（如 L 形的右下角）为透明，清晰展示非矩形形状。
4. 1×1 部件完全跳过形状预览（与全格填充无视觉差异）。
5. 新增 `System.Collections.Generic` 导入，用于 active cell 快查的 `HashSet<Vector2Int>`。

---

## AI Skills 使用指南 — 添加到 CLAUDE.md (2026-03-03 22:25)

**修改文件：**
- `CLAUDE.md`

**内容：**
在 CLAUDE.md 的 "Unity MCP 工具使用指南" 和 "实用开发 Tips" 之间新增 "AI Skills 使用指南" 章节，包含：
- 可用 Skills 完整列表及各自的触发场景说明（15 个 Skill）
- 5 条使用原则
- Project Ark 常见工作流与 Skills 的映射关系（新功能开发/Bug修复/架构重构/新星图部件/性能优化）

**目的：**
将 AI Skills 的使用规范正式写入项目规范文档，确保 AI 在合适场景下积极主动调用 Skills 提升任务执行质量。

**技术：**
文档化标准工作流映射。

---

## Fix: Track 拖拽预览形状不一致 (2026-03-03 22:27)

**修改文件：**
- `Assets/Scripts/UI/SlotCellView.cs`

**根因：**
`SlotCellView.OnPointerEnter` 调用的是旧的 `SetMultiCellHighlight(CellIndex, SlotSize, ...)` 方法，该方法从 CellIndex 开始**线性连续**高亮 SlotSize 个格子，完全不考虑 2D 形状。例如 Shape1x2H（横向2格）会错误地高亮同一列的两行，而不是同一行的两列。

**修复：**
将 `SetMultiCellHighlight` 替换为已有的 `SetShapeHighlight`：
1. 将线性 `CellIndex` 转换为 2D 坐标：`anchorCol = CellIndex  0RID_COLS`，`anchorRow = CellIndex / GRID_COLS`
2. 调用 `OwnerTrack.SetShapeHighlight(anchorCol, anchorRow, payload.Item.Shape, previewState, isCoreLayer)` 按实际 2D 形状高亮对应格子

**技术：**
`SlotLayer.GRID_COLS=3`，TypeColumn cells 按 row-major 排列（cellIndex = row * 3 + col），与 RefreshColumn 的计算方式一致。

---

## Feature: StarChart Track 显示修复 — 完整形状覆盖层 + 智能锚点推算 (2026-03-04 16:39)

**新建文件：**
- `Assets/Scripts/UI/ItemOverlayView.cs`

**修改文件：**
- `Assets/Scripts/Combat/StarChart/StarChartEnums.cs`
- `Assets/Scripts/Combat/StarChart/ItemShapeHelper.cs`
- `Assets/Scripts/Combat/StarChart/WeaponTrack.cs`
- `Assets/Scripts/UI/TypeColumn.cs`
- `Assets/Scripts/UI/TrackView.cs`
- `Assets/Scripts/UI/SlotCellView.cs`
- `Assets/Scripts/UI/DragDrop/DragDropManager.cs`

**需求 1：Track 上已装备部件以完整形状覆盖层展示**

问题：已装备的异形部件在 Track 上每个格子独立着色，无法直观看出部件整体轮廓，且颜色与背包不一致。

方案：
1. 新建 `ItemOverlayView` 组件，由 `TrackView.RefreshColumn` 在每次刷新时动态创建，覆盖在 TypeColumn 的 GridContainer 上方。
2. `ItemOverlayView.Setup` 根据形状偏移列表动态计算 bounding box，在 bounding box 内为每个格子创建 Image 子对象：occupied 格子显示类型颜色，empty 格子透明。
3. 在锚点格居中显示部件图标（`item.Icon`）。
4. `TypeColumn` 新增 `GridContainer` 属性（返回 cells[0] 的父 RectTransform）。
5. `TrackView` 新增 `_activeOverlays` 列表，每次 `RefreshColumn` 时先销毁旧 overlays，再创建新的。
6. `TrackView.Awake` 中从第一个可用格子的 RectTransform 自动检测 `_cellSize` 和 `_cellGap`。
7. `ItemOverlayView` 实现完整事件接口，hover/click/drag 事件转发给底层 SlotCellView。

**需求 2：拖拽放置时自动推算合法锚点**

问题：系统以鼠标悬停格作为锚点，导致悬停在非左上角格时形状越界或位置错误。

方案：
1. `ItemShapeHelper.FindBestAnchor` — 反向枚举候选锚点算法：对形状每个 offset (dx,dy) 计算候选锚点 = (hoverCol-dx, hoverRow-dy)，验证边界，选 row 最小再 col 最小的（top-left 优先），候选集为空返回 false（Invalid）。
2. `SlotCellView.OnPointerEnter` 调用 `FindBestAnchor`，结果存入 `DragDropManager.DropTargetAnchorCol/Row`。
3. `DragDropManager.EquipToTrack` 改用 `DropTargetAnchorCol/Row` 调用带锚点的 `WeaponTrack.EquipCore/EquipPrism` 重载。
4. `WeaponTrack` 新增带锚点参数的 `EquipCore/EquipPrism` 重载，调用 `SlotLayer.TryPlace`。

**扩展：ShapeLMirror**
- `StarChartEnums.ItemShape` 新增 `ShapeLMirror`（左上+左下+右下，缺右上）。
- `ItemShapeHelper` 新增 `CellsLMirror = { (0,0), (0,1), (1,1) }`，`GetCells/GetBounds` switch 新增对应 case。

**技术要点：**
- 所有算法基于形状偏移列表动态计算，无硬编码。Grid 尺寸变化只需更新 `SlotLayer.GRID_COLS/GRID_ROWS` 常量。
- 新增形状只需在 `ItemShapeHelper.GetCells` 添加 case，锚点推算和覆盖层渲染无需修改。
- `ItemOverlayView` 通过代码创建（`new GameObject + AddComponent`），Setup 中动态创建 border/icon 子对象，无需 prefab。

---

## Universal Shape Contract（全量形状一致性）— 2026-02-23 (今日)

**新建文件：**
- `Assets/Scripts/Combat/Editor/ShapeContractValidator.cs` — C4 护栏：Editor 菜单工具，一键验证所有 ItemShape

**修改文件：**
- `Assets/Scripts/Combat/StarChart/ItemShapeHelper.cs`
- `Assets/Scripts/UI/InventoryView.cs`
- `Assets/Scripts/UI/InventoryItemView.cs`

**内容：**
建立并落地 Universal Shape Contract（C1–C4），解决背包/Track/Ghost 形状显示不一致问题（如 L 形部件在背包中显示为 2x2 方块）。

**C1 单一真源**
- `ItemShapeHelper.GetCells()` 是所有占位查询的唯一依据。
- `GetBounds()` 仅用于外框尺寸显示，不参与占位判定。
- `GetCells()` 的 `default` 分支改为调用 `FallbackCells()`，在运行时对未注册 shape 打印 `LogWarning`（C4 护栏前置）。

**C2 一致占位（背包 shape-cell 改造）**
- `InventoryView.TryFindPosition/CanPlace/MarkOccupied` 全部替换为 `TryFindAnchorForShape/CanPlaceShape/MarkOccupiedByShape`。
- 新方法均遍历 `ItemShapeHelper.GetCells()` 而非矩形边界框，L 形仅占 3 格，不再挤占第 4 个空格。
- 排序策略（行优先、左上优先）保持不变，避免背包卡片位置体验漂移。

**C3 视觉洞位可读（InventoryItemView 透明底）**
- 移除 `Setup()` 中 `bgImage.color = alpha 1` 的强制不透明赋值，改为 `Color.clear`。
- 所有视觉着色仅来自 `BuildShapePreview()` 动态生成的 shape-cell Images。
- L 形的空洞位置现在为真正透明，而不是被卡片背景色填满。
- `_equippedBorder`（stretch Image）改为保持 `Color.clear`，装备状态改由 `BuildShapePreview` 在 active cells 上混合 equippedGreen 颜色（`_isEquipped` 标志）。
- `BuildShapePreview` 现在对所有形状（包括 1×1）生成 shape-cell，active 0.35 opacity，empty Color.clear。
- 新增 `RefreshShapeColors()` 公开方法，支持运行时更新装备色而无需重建 cell 列表。

**C4 可扩展护栏**
- `ItemShapeHelper.ValidateAllShapes()` — 遍历所有 `ItemShape` 枚举值，验证 cells 非空、cells 在 bounds 范围内、bounds 不小于实际 max cell extents，返回文字报告。
- `ShapeContractValidator.cs`（Editor-only）— `ProjectArk > Validate Shape Contract` 菜单，调用 `ValidateAllShapes()`，结果以 `DisplayDialog + Debug.Log/LogError` 双路输出。
- 新增 shape 时：只需在 `GetCells()` + `GetBounds()` 各加一个 case，运行一次菜单验证，其余 UI/拖拽逻辑无需修改（C4）。

**验收标准回归：**
1. L 形部件在背包中占 3 格（不再挤占第 4 格），视觉上第 4 角透明。✓
2. 所有非矩形形状（ShapeL、ShapeLMirror）的 empty cells 视觉上可辨识为洞位。✓
3. Track overlay / Ghost 已通过 `ItemShapeHelper.GetCells` 正确渲染（现有逻辑，回归验证）。✓
4. `ProjectArk > Validate Shape Contract` 对所有当前 6 个形状报告 PASS。✓
5. 新增 ItemShape 枚举值后未更新 GetCells，运行时 console 出现 LogWarning（C4 前置护栏）。✓

**技术：**
- Shape-cell 背包占位（替代矩形 bounding-box 占位）
- C# HashSet<Vector2Int> active-cell 快查
- 透明卡片底 + 动态 Image 子节点着色（完全 shape-driven 视觉）
- Editor MenuItem + `System.Text.StringBuilder` 自检报告

---

## DragDrop 架构改进（广播注册 + 双重验证 + Tooltip PointerMove + Inventory 对象池）— 2026-03-04 23:45

**修改文件：**
- `Assets/Scripts/UI/DragDrop/DragDropManager.cs`
- `Assets/Scripts/UI/TypeColumn.cs`
- `Assets/Scripts/UI/SlotCellView.cs`
- `Assets/Scripts/UI/ItemTooltipView.cs`
- `Assets/Scripts/UI/InventoryItemView.cs`
- `Assets/Scripts/UI/InventoryView.cs`

**内容：**
按 `WkecWulin_DragDrop_Analysis` 的可借鉴设计，对 Project Ark 拖拽系统做了 4 项架构级修复与优化：

1. **BeginDrag 广播注册机制（去硬编码）**
   - `DragDropManager` 新增 `_registeredColumns` 与 `RegisterColumn/UnregisterColumn`。
   - `BeginDrag/CleanUp` 改为广播 `BroadcastDragBegin/BroadcastDragEnd`，由每个 `TypeColumn` 自行判断是否高亮。
   - `TypeColumn` 新增 `OnEnable/Start/OnDisable` 注册生命周期，以及 `OnDragBeginBroadcast/OnDragEndBroadcast`。
   - 删除 `DragDropManager.HighlightMatchingColumns()`，不再每次拖拽都 `GetComponentsInChildren` + switch 硬编码。

2. **OnDrop 双重验证（消除过期缓存）**
   - `SlotCellView` 提取 `ComputeDropValidity()`，统一封装类型匹配、空间检查、替换判定、形状锚点推算。
   - `OnDrop` 改为再次实时校验，而不是依赖 `OnPointerEnter` 阶段的缓存状态。
   - 移除 `_isHighlightedValid` 相关逻辑，降低拖拽过程中状态变化导致的误判风险。

3. **Tooltip 改为 PointerMove 驱动（去每帧 Update）**
   - `ItemTooltipView` 删除 `Update()`，新增 `UpdatePosition()`。
   - `InventoryItemView`、`SlotCellView` 实现 `IPointerMoveHandler`，仅在指针移动时刷新 tooltip 位置。
   - `DragDropManager.Bind` 缓存 `TooltipView`，统一提供给拖拽相关视图访问。

4. **InventoryView 对象池化（替代 DestroyImmediate 全量重建）**
   - 新增 `_pooledViews`，`Refresh` 改为 `GetOrCreateView()` 复用卡片。
   - `ClearViews` 改为解绑事件并回收到池中（`SetActive(false)`），不再 `DestroyImmediate` 全销毁。
   - `OnDestroy` 时统一销毁池内对象，减少频繁刷新导致的 GC 和 EventSystem 抖动。

**目的：**
- 提升拖拽系统的可扩展性（新列类型不再改 manager 核心逻辑）。
- 提升拖放判定稳定性（避免 hover 缓存过期造成的误触发）。
- 降低 UI 每帧开销与刷新抖动（tooltip 与 inventory 视图均改为事件驱动/对象复用）。

**技术：**
- 事件广播 + 自判断（注册表模式）。
- Drop 双重验证（Begin/Drop 两阶段一致性保障）。
- PointerMove 驱动 UI 更新（替代 Update 常驻轮询）。
- 轻量对象池复用（减少销毁/重建成本）。

---

## Bug Fix: DragGhostView 双重渲染修复（Bug 2）— 2026-03-05 12:31

**修改文件：**
- `Assets/Scripts/UI/DragDrop/DragGhostView.cs`

**内容简述：**
修复拖拽 Ghost 同时显示"正确 L 形 cell grid"和"矩形 icon"的双重渲染问题。

**根因：**
`Show()` 方法中 `_iconImage` 被赋予 sprite + alpha，该 Image 的 RectTransform 拉伸到整个 bounding box（L 形 = 2×2），与 `RebuildShapeGrid()` 创建的 cell grid 叠加，形成双重渲染。

**修复方案：**
在 `Show()` 中将 `_iconImage.sprite = null` 且 `_iconImage.color = Color.clear`，完全隐藏 icon image。Ghost 的视觉形状完全由 cell grid（`_cellImages`）表达，与背包中部件的显示形态保持一致。`_nameLabel` 已显示部件名称，无需 icon 重复表达。

**技术：**
- 最小化改动：仅修改 `Show()` 中 `_iconImage` 的赋值逻辑，不影响其他路径。
- 设计原则：拖拽时看到的形状 = 背包中看到的形状（cell grid 统一表达）。

---

## StarChart UI Bug 修复 + Track 动态容量系统 — 2026-03-05 12:52

**修改文件：**
- `Assets/Scripts/Combat/StarChart/SlotLayer.cs`（重构）
- `Assets/Scripts/Combat/StarChart/WeaponTrack.cs`（新增 `SetLayerCols`）
- `Assets/Scripts/Combat/StarChart/StarChartController.cs`（修改 `ExportTrack`/`ImportTrack`）
- `Assets/Scripts/Core/Save/SaveData.cs`（`TrackSaveData` 新增字段）
- `Assets/Scripts/UI/TrackView.cs`（常量引用替换）
- `Assets/Scripts/UI/SlotCellView.cs`（常量引用替换）
- `Assets/Scripts/Combat/Tests/SlotLayerTests.cs`（测试重写）

**内容简述：**
1. **SlotLayer 动态容量**：删除 `GRID_COLS/GRID_ROWS/GRID_SIZE` 硬编码常量，改为 `Rows`（固定2）、`Cols`（动态1-4）、`Capacity = Rows×Cols` 属性。新增 `TryUnlockColumn()` 方法（返回 bool，达到 MAX_COLS=4 时返回 false）。修复 `FreeSpace = Capacity - UsedSpace`（原来错误地用 `GRID_COLS - UsedSpace`）。构造函数接收 `initialCols` 参数，默认 1（初始 2 格）。
2. **存档持久化**：`TrackSaveData` 新增 `CoreLayerCols`/`PrismLayerCols` 字段（默认 1，旧存档 clamp 到 ≥1）。`ExportTrack` 写入列数，`ImportTrack` 读取并调用 `WeaponTrack.SetLayerCols()` 恢复。
3. **UI 层常量替换**：`TrackView.cs` 和 `SlotCellView.cs` 中所有 `SlotLayer<T>.GRID_COLS/GRID_ROWS` 静态引用替换为从 layer 实例动态读取。
4. **Bug 1/3 确认**：`InventoryItemView` 和 `ItemOverlayView` 已正确实现 empty cell `Color.clear + raycastTarget=false`，无需额外修改。
5. **单元测试**：重写 `SlotLayerTests.cs`，修复旧测试（初始容量 2 而非 6），新增容量解锁测试（初始容量、解锁后容量、MAX_COLS 边界、旧数据不受影响）。

**目的：** 修复 StarChart 星图 UI 的 4 个 Bug（L 形部件显示、Track Overlay 显示、Track 容量动态解锁、FreeSpace 计算错误）。

**技术方案：**
- `SlotLayer._grid` 预分配为 `[FIXED_ROWS, MAX_COLS]`，避免解锁时重新分配内存；`InBounds` 检查使用动态 `Cols` 限制访问范围。
- `WeaponTrack.SetLayerCols` 通过循环调用 `TryUnlockColumn` 恢复列数，不依赖 `UnityEngine.Mathf`（纯 C# 类）。
- 旧存档兼容：`JsonUtility` 反序列化缺失字段时默认为 0，`ImportTrack` 中 `Mathf.Max(1, data.CoreLayerCols)` 确保不低于 1。

---

## 未解锁格子完全隐藏 - 2026-03-05 14:22

**修改文件：**
- `Assets/Scripts/UI/TrackView.cs`（`RefreshColumn` 方法）

**内容简述：**
在 `RefreshColumn<T>` 方法中，根据 `layer.Rows × layer.Cols` 动态控制 cell 的 `SetActive` 状态：
- 索引 `< unlockedCount` 的 cell：`SetActive(true)`（显示）
- 索引 `>= unlockedCount` 的 cell：`SetActive(false)`（完全隐藏）
- `layer == null` 时 `unlockedCount = 0`，所有 cell 隐藏

**目的：** 未解锁的格子不再显示灰色 `+` 占位符，而是完全从 UI 中隐藏。

**技术方案：**
- 利用已有的 `layer.Rows`/`layer.Cols` 动态容量属性计算 `unlockedCount`
- 在重置 cell 状态之前先做 `SetActive` 控制，避免对隐藏 cell 调用 `SetEmpty()`
- SAIL/SAT 列不受影响（使用独立的 `RefreshSailColumn`/`RefreshSatColumn` 方法）

---

## Bug Fix: DragGhostView CanvasGroup 初始化失败 - 2026-03-05 14:30

**修改文件：**
- `Assets/Scripts/UI/DragDrop/DragGhostView.cs`（`Awake` 方法）

**错误信息：**
```
[DragGhostView] Failed to get/add CanvasGroup on ReplaceHint 'ReplaceHint'
```

**根因：**
C# 的 `??` 运算符绕过了 `UnityEngine.Object` 对 `==` 的重写，直接在 CLR 层面检查 null。
`GetComponent<CanvasGroup>()` 找不到组件时返回"假 null"（Unity 层面为 null，CLR 层面不为 null），
导致 `??` 认为左侧有值，不执行右侧的 `AddComponent`，`_replaceHintCg` 持有无效对象，
后续 `if (_replaceHintCg != null)` 用 Unity `==` 检查时判定为 null，触发 LogError。

**修复方案：**
将 `??` 运算符改为显式 `if` 判断：
```csharp
_replaceHintCg = _replaceHintLabel.GetComponent<CanvasGroup>();
if (_replaceHintCg == null)
    _replaceHintCg = _replaceHintLabel.gameObject.AddComponent<CanvasGroup>();
```
同样修复了 `_nameLabelCg` 的相同问题。

**教训：** Unity 项目中凡是涉及 `UnityEngine.Object` 的 null 检查，必须用显式 `== null`，禁止用 `??` 运算符。

---

## TrackView Debug 格子数参数暴露 — 2026-03-05 15:53

**修改文件：**
- `Assets/Scripts/UI/TrackView.cs`

**内容简述：**
在 `TrackView` 上新增 4 个 `[SerializeField]` 调试参数，允许在 Inspector 中直接覆盖各分类的格子数，无需修改存档或游戏进度：

| 参数 | 类型 | 说明 |
|------|------|------|
| `_debugCoreCols` | `int [0-4]` | Core 层列数（0 = 使用存档值，1-4 = 强制覆盖） |
| `_debugPrismCols` | `int [0-4]` | Prism 层列数（0 = 使用存档值，1-4 = 强制覆盖） |
| `_debugSailSlots` | `int [0-4]` | Sail 显示格子数（0 = 默认 1 格，1-4 = 覆盖） |
| `_debugSatSlots` | `int [0-4]` | Satellite 显示格子数（0 = 默认 4 格，1-4 = 覆盖） |

**行为规则：**
- Core/Prism：值 > 0 时调用 `_track.SetLayerCols()` 强制设置层列数，影响实际数据层（格子数 = cols × 2 行）
- Sail/Sat：值 > 0 时控制 `RefreshSailColumn`/`RefreshSatColumn` 中显示的格子数量（纯 UI 层，不影响数据）
- 所有参数默认为 0（不覆盖），不影响正式游戏流程
- 在 `Awake` 和 `Bind` 时均会应用，确保无论初始化顺序如何都生效

**目的：** 解决问题 B（Track 格子数不一致），提供快速调试手段，无需依赖存档数据即可验证各分类格子数的视觉表现。

**技术方案：**
- 新增私有方法 `ApplyDebugSlotCounts()`，在 `Awake` 末尾和 `Bind` 中（track 绑定后）各调用一次
- Sail/Sat 的 debug 参数不调用 `SetLayerCols`（Sail/Sat 无 SlotLayer），而是在 Refresh 方法内通过 `sailSlots`/`satSlots` 局部变量控制循环范围

---

## Icon/Shape 解耦渲染（方案B）+ Bug D/E 修复 — 2026-03-05 16:00

**新建文件：**
- `Assets/Scripts/UI/ItemIconRenderer.cs`

**修改文件：**
- `Assets/Scripts/UI/InventoryItemView.cs`
- `Assets/Scripts/UI/ItemOverlayView.cs`
- `Assets/Scripts/UI/DragDrop/DragGhostView.cs`
- `Assets/Scripts/UI/TrackView.cs`
- `Assets/Scripts/UI/DragDrop/DragDropManager.cs`

**内容简述：**

### 核心思路（Backpack Monsters 渲染哲学）

Icon 和 Shape 是两个完全独立的层：

| 层 | 职责 | 实现 |
|----|------|------|
| **Shape Layer** | 表达"占了哪些格子"（颜色、高亮、选中） | `ItemIconRenderer.BuildShapeCells()` — 每个 active cell 一个 Image，empty cell 透明 |
| **Icon Layer** | 表达"这是什么东西"（身份识别） | `ItemIconRenderer.BuildIconCentered()` — 固定大小 Image，锚定在 shape 视觉重心，不拉伸 |

### ItemIconRenderer.cs（新建）

静态工具类，提供：
- `BuildShapeCells(parent, shape, activeColor)` — anchor 比例定位，用于 InventoryItemView
- `BuildShapeCellsAbsolute(parent, shape, activeColor, cellSize, cellGap)` — 绝对像素定位，用于 ItemOverlayView / DragGhostView
- `RefreshShapeCellColors(cellImages, shape, activeColor)` — 重新着色（equip 状态变化时）
- `BuildIconCentered(parent, item, iconSizePx)` — 固定大小 Icon，锚定在 shape 重心（normalized 坐标）
- `BuildIconOnAnchorCell(parent, item, cellSize, iconSizePx)` — 固定大小 Icon，锚定在 anchor cell 中心（绝对坐标）
- `GetShapeCentroidNormalized(shape)` — 计算 active cells 的视觉重心（normalized [0,1] 空间）

### InventoryItemView.cs（改造）

- 移除 `[SerializeField] private Image _iconImage`（不再需要 Inspector 连线的 icon）
- 新增 `private Image _iconImageDynamic`（由 ItemIconRenderer 动态创建）
- `BuildShapePreview()` 改为调用 `ItemIconRenderer.BuildShapeCells()` + `BuildIconCentered()`
- `RefreshShapeColors()` 改为调用 `ItemIconRenderer.RefreshShapeCellColors()`
- `OnDestroy()` 同时清理 `_iconImageDynamic`

### ItemOverlayView.cs（改造）

- 移除手写的 cell 循环（`occupiedSet`、`bounds`、`cells` 局部变量）
- 移除手写的 `iconGo` 创建和 icon 定位代码
- 改为调用 `ItemIconRenderer.BuildShapeCellsAbsolute()` + `BuildIconOnAnchorCell()`
- 移除 `using System.Collections.Generic` 和 `using TMPro`

### DragGhostView.cs（改造）

- `RebuildShapeGrid()` 改为调用 `ItemIconRenderer.BuildShapeCellsAbsolute()`
- `Show()` 中新增 Icon Layer：调用 `ItemIconRenderer.BuildIconCentered()`，固定大小居中
- 新增 `private Image _iconImageDynamic` 字段，`OnDestroy()` 中清理

### TrackView.cs（问题E修复）

- `RefreshColumn()` 中将 `cells[i].gameObject.SetActive(i < unlockedCount)` 改为 CanvasGroup 控制：
  - `cg.alpha = unlocked ? 1f : 0f`
  - `cg.interactable = unlocked`
  - `cg.blocksRaycasts = unlocked`
- 遵守 CLAUDE.md 第11条：uGUI 面板禁止用 SetActive 控制显隐

### DragDropManager.cs（问题D修复）

- `BeginDrag()` 中，当 `payload.Source == DragSource.Slot` 时，立即调用 `_panel.RefreshAllViews()`
- 目的：移除被拖拽部件的 `ItemOverlayView`，避免 Ghost + overlay 同时可见（视觉重叠）

**目的：** 彻底解决 Icon 和 Shape 耦合导致的所有视觉 bug（非矩形 shape 的 Icon 溢出、Ghost 双重渲染、overlay icon 残留），同时修复 SetActive 违规和拖拽 overlay 残留问题。

**技术方案：**
- 单一职责：`ItemIconRenderer` 是唯一知道如何渲染 Icon+Shape 的地方，三个 View 都委托给它
- Icon 固定大小（32px inventory / 36px overlay / ghostSize×0.55 ghost），不随 bounding box 变化
- Shape centroid 计算：`(Σ(col+0.5)/boundsX) / cellCount`，y 轴翻转适配 Unity UI 坐标系



---

## GG 飞船手感复刻 — 移动模型完整重构 (2026-03-05 16:13)

### 背景

通过 Il2CppDumper 反编译 Galactic Glitch (IL2CPP) 的 global-metadata.dat，提取出 GGSteering 类的完整字段和方法签名，确认了 GG 飞船的物理模型。

**GG 移动模型**（角加速度前向推力）与 Project Ark 旧模型（Twin-Stick 速度映射）根本不同，本次做全量对齐重构。

---

### 新建/修改文件

#### Assets/Scripts/Ship/Data/ShipStatsSO.cs（完全重写）
- 删除全部旧字段（AccelerationCurve、Deceleration、SharpTurn、InitialBoost 等）
- 新增 GG 对应参数：
  - **Rotation**：_angularAcceleration（900 deg/s²）、_maxRotationSpeed（380 deg/s）、_angularDrag（8）
  - **ForwardThrust**：_forwardAcceleration（20 units/s²）、_maxSpeed（10 units/s）、_linearDrag（3）
  - **Boost**：_boostImpulse（18）、_boostDuration（0.25s）、_boostMaxSpeedMultiplier（2×）、_boostCooldown（1.2s）、_boostBufferWindow（0.15s）
  - **Dash**：_dashImpulse（22）、_dashIFrameDuration（0.12s）、_dashCooldown（0.4s）
- 保留向后兼容别名（MoveSpeed、DashSpeed、DashDuration 等）防止其他脚本编译报错

#### Assets/Scripts/Ship/Movement/ShipMotor.cs（完全重写）
- 旧模型：Twin-Stick（输入直接映射到世界方向速度）
- 新模型（GGSteering 对齐）：
  - 只检测前进输入（input.y > 0.1f），忽略横向
  - FixedUpdate 中 rb.AddForce(transform.up * forwardAcceleration, Force) 沿船头施力
  - 速度上限由代码 clamp（boost 时放宽为 maxSpeed × BoostSpeedMultiplier）
  - 无输入时 Rigidbody2D.linearDrag 自然衰减（无刹车代码）
- 新增 AddExternalImpulse() 公共接口供 Boost/Dash/击退使用
- 保留 ApplyImpulse()、SetVelocityOverride()、ClearVelocityOverride() 向后兼容

#### Assets/Scripts/Ship/Aiming/ShipAiming.cs（完全重写）
- 旧模型：Mathf.MoveTowardsAngle 固定角速度（无惯性）
- 新模型（GGSteering.RotateTowardsAimTarget 对齐）：
  - FixedUpdate 计算目标角度和角度差 Mathf.DeltaAngle
  - desiredAngularVelocity = clamp(angleDiff × (angularAcceleration / maxRotationSpeed))
  - 以 angularAcceleration × dt 步长 MoveTowards 到目标角速度（逐帧加速，不瞬间到位）
  - 写入 Rigidbody2D.angularVelocity，松手后由 angularDrag 自然衰减

#### Assets/Scripts/Ship/Movement/ShipDash.cs（重写）
- 旧模型：速度覆盖（强制维持固定速度到 duration 结束）
- 新模型（GG AddForce(Impulse) 对齐）：
  - 触发时 _motor.AddExternalImpulse(dashDir × dashImpulse) 一次性冲量
  - 无敌帧按 DashIFrameDuration 独立计时
  - 简化输入缓冲（单次布尔缓冲，替代旧 InputBuffer）

#### Assets/Scripts/Ship/Movement/ShipBoost.cs（新建）
- GG BoosterBurnoutPower 的完整复刻：
  - 方向 = 船头方向（ShipAiming.FacingDirection）
  - _motor.AddExternalImpulse(boostDir × boostImpulse) 一次性冲量
  - Boost 持续期间设置 Motor.IsBoosting = true 和 BoostSpeedMultiplier 放宽速度上限
  - Boost 结束后多余速度由 linearDrag 自然衰减（无强制减速代码）
  - CooldownProgress (0~1) 属性供 UI 冷却条使用
  - 事件：OnBoostStarted / OnBoostEnded / OnBoostReady 供 VFX 订阅

#### Assets/Scripts/Ship/Input/InputHandler.cs（增量修改）
- 新增 public event Action OnBoostPressed
- 新增 _boostAction 字段，映射 Ship ActionMap 的 "Boost" Action
- OnEnable/OnDisable 中注册/取消订阅

#### Assets/Scripts/Ship/Editor/ShipFeelAssetCreator.cs（更新）
- 删除旧字段赋值（_acceleration、_deceleration、_accelerationCurve 等）
- 新增 GG 参数默认值赋值
- 移除不再需要的 SetAnimationCurve 和 EnsureCurve 辅助方法

---

### 目的

一比一复刻 Galactic Glitch 飞船手感：
1. **旋转有重量感**：角加速度模型，快速但有惯性
2. **前向推力**：只能向船头方向加速，转向需要先旋转
3. **自然减速**：linearDrag 物理衰减，松开后滑行感
4. **Boost**：沿船头方向冲量 + 持续速度放宽，对应 GG 的 BoosterBurnoutPower

### 技术

- Rigidbody2D.angularVelocity（旋转惯性）
- Rigidbody2D.AddForce (Force 模式 + Impulse 模式)
- Rigidbody2D.linearDrag / angularDrag（自然衰减）
- UniTask.Delay（Boost/Dash 持续时间异步等待）

### 下一步

- 在 Input Action Asset 中添加 "Boost" Action（Space / SouthButton）
- ShipBoost 挂载到飞船 Prefab，引用 ShipStatsSO
- ShipEngineVFX 订阅 ShipBoost.OnBoostStarted 增强引擎粒子
- 运行游戏调整 ShipStatsSO 参数以匹配 GG 手感
---

## 拖拽放置高亮系统 (Drag Placement Highlight System)  2026-03-05 16:23

### 新建文件
- `Assets/Scripts/UI/DragHighlightLayer.cs`  独立高亮 tile 管理器（189 行）

### 修改文件
- `Assets/Scripts/UI/TrackView.cs`  集成 DragHighlightLayer，改造 SetShapeHighlight，新增 SetSingleHighlight / ClearShapeHighlight / ClearAllHighlightTiles
- `Assets/Scripts/UI/SlotCellView.cs`  OnPointerEnter 中 Core/Prism/SAIL/SAT 高亮全部改为调用 DragHighlightLayer，不再直接修改 _backgroundImage
- `Assets/Scripts/UI/DragDrop/DragDropManager.cs`  CleanUp 中新增遍历所有 TrackView 调用 ClearAllHighlightTiles()

### 内容简述
实现了独立的拖拽高亮 tile 层，将高亮渲染与格子背景色完全解耦。

### 目的
解决拖拽悬停高亮污染 SlotCellView._backgroundImage.color 的问题：高亮 tile 是独立的 GameObject 层，不影响格子原始状态，清除即隐藏，无残影。

### 技术方案
1. DragHighlightLayer：每个 TypeColumn 对应一个实例，在 TrackView.Awake 中动态创建。内部维护 tile 对象池，仅在 shape/anchor/state 变化时重建。
2. 坐标系与 ItemOverlayView 完全一致：anchorMin=anchorMax=(0,0)，pivot=(0,1)，anchoredPosition=(col*step, -row*step)。
3. raycastTarget=false：高亮 tile 不阻挡指针事件，底层格子仍可正常接收 OnPointerEnter/Exit。
4. SetAsLastSibling：每次 ShowHighlight 时将 tile 置于最顶层，渲染在所有 ItemOverlayView 之上。
5. Core/Prism invalid 分支：改为调用 SetShapeHighlight 传入 Shape1x1 + Invalid，走统一的 tile 层。
6. SAIL/SAT 支持：通过 SetSingleHighlight(SlotType, col=0, row=CellIndex, state) 实现单格高亮。
7. CleanUp 保障：DragDropManager.CleanUp 中遍历所有 TrackView 调用 ClearAllHighlightTiles()，确保拖拽结束后无残留高亮。

---

## GG 解包 — AssetRipper 提取真实数值 (2026-03-05 16:32)

### 方法论总结（4种方案实测结果）

| 方案 | 工具 | 结果 |
|------|------|------|
| UnityPy 直接读 | UnityPy 1.25.0 | ✗ IL2CPP MonoBehaviour 无 TypeTree，raw_data 为空 |
| UnityPy + DummyDll TypeTree | TypeTreeGeneratorAPI | ✗ 工具尚未成熟，依赖链太复杂 |
| 特征扫描 raw bytes | 自写 Python 模式匹配 | ✗ raw_data 为空，无法扫描 |
| AssetRipper GUI + REST API | AssetRipper 1.3.11 headless | ✓ 成功导出完整 YAML Prefab |

**最终有效方案**：AssetRipper headless 模式 → REST API 加载 GG 目录 → /Export/UnityProject 导出 → 在 C:\Temp\GG_Ripped\ExportedProject\Assets\GameObject\Player.prefab 中找到全部序列化数值。

### 关键发现：GG 真实架构

**GG 飞船不是静态参数，而是状态机驱动**：Player.prefab 里有一个状态机组件，根据当前动作状态（IsBlueState/IsBoostState/IsMainAttackState/IsDodgeState等）动态切换物理参数。

#### Rigidbody2D（真实值）
- m_Mass: 100
- m_LinearDrag: 2.5（基准值）
- m_AngularDrag: 0
- m_GravityScale: 0

#### 状态机参数对照表（从 Player.prefab 直接读取）

| 状态 | linearDrag | moveAccel | maxMoveSpeed | angularAccel | maxRotSpeed |
|------|-----------|-----------|--------------|-------------|------------|
| IsBlueState（0，正常） | 3 | 100 | 7.5 | 80 | 80 |
| IsBoostState（3） | 2.5 | 100 | **9** | 40 | 80 |
| IsRedState（1） | 3 | 0 | 7.5 | 180 | 360 |
| IsMainAttackState（6） | 3 | 100 | 7.5 | **720** | **720** |
| IsMainAttackFireState（10） | 3 | 100 | 5.25 | 720 | 720 |
| IsSecondaryAttackState（7） | 2.5 | 100 | 5.25 | 180 | 360 |
| IsDodgeState（2） | **1.7** | 12 | 4 | 20 | 50 |
| IsHealState（4） | 1.5 | 0 | 5 | 0 | 240 |
| IsShapeShiftState（5） | 3 | 100 | 6.5 | 120 | 360 |

#### 其他关键数值（Player MonoBehaviour）
- afterBoostDrag: 1.8（Boost 结束后的 drag）
- dodgeForce: 13（Dash 冲量）
- dodgeForceAfterDodge: 5（Dash 结束后残余力）
- dodgeInvulnerabilityTime: 0.15（Dash 无敌帧）
- dodgeCacheTime: 0.2（输入缓冲）
- dodgeRechargeTime: 0.5（Dash 冷却）
- speedModAfterDodge: 0.7（Dash 后速度系数）

#### BoosterBurnoutPower（并非移动加速，是战斗 Power）
- 这是一个战斗 Power（升级装备），触发时在飞船后方留下伤害轨迹
- **不是**控制飞船加速的 Boost 系统

### ShipStatsSO 更新的默认值（对齐 GG 真实值）

- _angularAcceleration: 900 → **80** (GG IsBlueState)
- _maxRotationSpeed: 380 → **80** (GG IsBlueState)
- _angularDrag: 8 → **0** (GG 无旋转阻尼)
- _forwardAcceleration: 20 → **100** (GG IsBlueState)
- _maxSpeed: 10 → **7.5** (GG IsBlueState)
- _linearDrag: 3 → **3** (✓ 已正确)
- _boostImpulse: 18 → **6** (GG IsBoostState vs IsBlueState 差值)
- _boostDuration: 0.25 → **0.2** (GG minTime=0.2s)
- _boostMaxSpeedMultiplier: 2.0 → **1.2** (9/7.5=1.2)
- _boostCooldown: 1.2 → **1.0**
- _dashImpulse: 22 → **13** (GG dodgeForce)
- _dashIFrameDuration: 0.12 → **0.15** (GG dodgeInvulnerabilityTime)
- _dashCooldown: 0.4 → **0.5** (GG dodgeRechargeTime)


---

## Bug Fix: DragGhostView 拖拽时双重 Icon 渲染  2026-03-05 16:47

### 修改文件
- `Assets/Scripts/UI/DragDrop/DragGhostView.cs`

### 内容简述
修复拖拽时同时出现两个视觉元素（一个 Icon + 一个 Shape）的 bug。

### 根因
DragGhostView 中存在两个 Icon 来源同时可见：
1. `[SerializeField] private Image _iconImage`  Prefab Inspector 连线的旧字段
2. `_iconImageDynamic`  Show() 中通过 ItemIconRenderer.BuildIconCentered() 动态创建的新 Icon

旧代码在 Show() 中用 `_iconImage.sprite = null; _iconImage.color = Color.clear;` 试图隐藏旧字段，但 `color = Color.clear` 不能保证 Image 完全不渲染（Prefab 默认颜色可能覆盖），且 GameObject 仍然 active 占据层级。

### 修复方案
在 Awake() 中立即为 `_iconImage` 添加 CanvasGroup 并设 alpha=0，彻底阻断其渲染，与 CLAUDE.md 第11条（禁止 SetActive）保持一致。Show() 中的旧清除代码替换为注释说明。

---

## Bug Fix: DragGhostView Icon 与 Shape 坐标系不匹配导致分离  2026-03-05 16:56

### 修改文件
- `Assets/Scripts/UI/ItemIconRenderer.cs`（新增 `BuildIconAbsolute` 方法）
- `Assets/Scripts/UI/DragDrop/DragGhostView.cs`（Show() 中改用 `BuildIconAbsolute`）

### 内容简述
修复拖拽 Ghost 中 Icon 和 Shape cells 出现在两个不同位置的 bug（截图中蓝色小方块 + 青色 L 形分离）。

### 根因
坐标系不匹配：
- `BuildShapeCellsAbsolute` 使用 `anchorMin=anchorMax=zero`（左下角锚点），从 `(0,0)` 原点向右向下排列 cells
- `BuildIconCentered` 使用归一化 centroid 作为 anchor（`anchorMin=anchorMax=centroid`，相对于父节点 sizeDelta 的百分比）

对于 L 形 Shape，`BuildIconCentered` 计算出的 centroid ≈ `(0.5, 0.5)`（bounding box 中心），而 Shape cells 实际分布在左下角，导致 Icon 跑到右上角空白区域，两者完全分离。

### 修复方案
新增 `ItemIconRenderer.BuildIconAbsolute()` 方法：
- 与 `BuildShapeCellsAbsolute` 使用完全相同的坐标系（`anchorMin=anchorMax=zero`）
- Icon centroid 用绝对像素计算：`cx = Σ(col*step + cellSize/2) / count`，`cy = -Σ(row*step + cellSize/2) / count`（负号因 UI y 轴向下）
- `DragGhostView.Show()` 改为调用 `BuildIconAbsolute`，传入 `cellSize=_ghostSize.x`、`cellGap=_cellGap`


---

## Bug Fix: DragGhostView 格子颜色与背包格子颜色不一致  2026-03-05 17:04

### 修改文件
- `Assets/Scripts/UI/DragDrop/DragGhostView.cs`（删除 CellActiveColor 硬编码，改为按 item.ItemType 动态取色）

### 内容简述
修复拖拽 Ghost 格子颜色与背包中格子颜色不一致的问题。

### 根因
- 背包格子颜色：`new Color(_typeColor.r, _typeColor.g, _typeColor.b, 0.22f)`（按类型变色：Core=蓝、Prism=紫、Sail=绿、Sat=黄）
- Ghost 格子颜色：`new Color(0f, 0.85f, 1f, 0.18f)`（硬编码固定青色）
两者颜色完全不同，导致 Ghost 看起来总是青色，而背包中是类型对应的颜色。

### 修复方案
1. 删除 `CellActiveColor` 静态常量
2. `SetShape(ItemShape, StarChartItemType)` 新增 itemType 参数
3. 用 `StarChartTheme.GetTypeColor(itemType)` + alpha=0.22 构造格子颜色（与 SlotCellView.SetItem() 完全一致）
4. `Show(item)` 调用 `SetShape(item.Shape, item.ItemType)` 传入类型
5. Ghost 整体透明度由 `CanvasGroup.alpha=0.7` 控制，格子颜色本身保持 alpha=0.22 不变


---

## Bug Fix: 拖拽 Ghost 鼠标锚点偏移  2026-03-05 17:12

### 修改文件
- `Assets/Scripts/UI/DragDrop/DragGhostView.cs`（新增 ComputeDragOffset，修改 FollowPointer 接受 dragOffset 参数）
- `Assets/Scripts/UI/DragDrop/DragDropManager.cs`（BeginDrag 新增 PointerEventData 参数，计算并存储 _dragOffset）
- `Assets/Scripts/UI/InventoryItemView.cs`（OnBeginDrag 传入 eventData）
- `Assets/Scripts/UI/ItemOverlayView.cs`（OnBeginDrag 传入 eventData）
- `Assets/Scripts/UI/SlotCellView.cs`（OnBeginDrag 传入 eventData）

### 内容简述
修复拖拽时 Ghost 不跟随鼠标点击位置的问题：鼠标点击部件的哪个位置，拖动时鼠标始终保持在该位置，不会偏移到 Ghost 中心。

### 根因
`FollowPointer` 直接将 Ghost 的 `localPosition` 设为鼠标的 canvas 本地坐标，而 Ghost 的 pivot=(0.5,0.5)，导致 Ghost 中心始终对齐鼠标，而非鼠标点击的原始位置。

### 技术方案
1. `DragGhostView.ComputeDragOffset(eventData)`：在 `BeginDrag` 时调用一次，计算 `pressPosition - currentPosition`（均转换到 canvas 本地坐标），得到鼠标点击点相对于当前鼠标位置的偏移量。
2. `DragDropManager._dragOffset`：存储该偏移，每帧传给 `FollowPointer`。
3. `FollowPointer(eventData, dragOffset)`：`localPosition = mouseCanvasLocal + dragOffset`，使 Ghost 的视觉锚点始终保持在用户最初点击的位置。
4. 三个拖拽发起方（InventoryItemView/ItemOverlayView/SlotCellView）均传入 `eventData`。

---

## Feature: GG Dash/Boost 架构重新对齐 - Boost 改为状态切换模型  2026-03-05 20:30

### 修改文件
- `Assets/Scripts/Ship/Movement/ShipBoost.cs`（完整重写：从冲量模型改为状态切换模型）
- `Assets/Scripts/Ship/Movement/ShipMotor.cs`（新增 EnterBoostState/ExitBoostState API）
- `Assets/Scripts/Ship/Aiming/ShipAiming.cs`（Boost 期间使用 BoostAngularAcceleration）
- `Assets/Scripts/Ship/Data/ShipStatsSO.cs`（移除 _boostImpulse/_boostMaxSpeedMultiplier，新增 _boostLinearDrag/_boostMaxSpeed/_boostAngularAcceleration）
- `Assets/Scripts/Ship/Editor/ShipFeelAssetCreator.cs`（同步更新默认值）
- `Assets/_Data/Ship/DefaultShipStats.asset`（同步序列化字段）

### 根因分析
对 GG 的 Boost 机制理解有误：之前将 Boost 实现为"沿船头冲量 + 放宽速度上限"。
经过仔细解读 Player.prefab 序列化数据发现：

**GG 真实的 Boost 是 StateData 状态切换，不是冲量：**
- 进入 IsBoostState → linearDrag 从 3 降到 2.5，maxMoveSpeed 从 7.5 升到 9，angularAcceleration 从 80 降到 40
- 持续 minTime=0.2s 后自动退回 IsBlueState（正常参数）
- 玩家需要在 Boost 期间持续按 WASD 加速才能突破正常速度上限
- Space 键在 GG 中只绑定 Dodge（dodgeForce=13 冲量 + IsDodgeState 0.225s），Boost 是独立解锁能力

**GG Dodge 真实参数（Player.prefab 确认）：**
- dodgeForce: 13（冲量）
- dodgeInvulnerabilityTime: 0.15s
- dodgeRechargeTime: 0.5s
- IsDodgeState: linearDrag=1.7, moveAccel=12, maxMoveSpeed=4, angularAccel=20, maxRotSpeed=50, 持续 0.225s

### 新架构
- ShipMotor.EnterBoostState(boostDrag, boostMaxSpeed)：临时替换 Rigidbody2D.linearDamping + 覆盖速度上限
- ShipMotor.ExitBoostState()：恢复 stats.LinearDrag + 清除速度覆盖
- ShipAiming：检测 motor.IsBoosting 时使用 BoostAngularAcceleration（降低旋转响应，给追击感）
- ShipBoost.ExecuteBoostAsync()：进入状态 → 等待 BoostDuration → 退出状态 → 冷却

### 数值对齐（mass=1 适配）
- boostLinearDrag: 2.5（★GG 原值）
- boostMaxSpeed: 9（★GG 原值）
- boostAngularAcceleration: 400（GG 原值 40×10，mass=1 对应）
- boostDuration: 0.2s（★GG minTime 原值）


## Bug Fix: 拖拽 Ghost 视觉中心与鼠标不对齐 — 2026-03-06 13:16

### 修改文件
- `Assets/Scripts/UI/DragDrop/DragGhostView.cs`

### 内容简述
修复拖拽 Ghost 出现在鼠标下方而非居中于鼠标的问题。

### 根因
Ghost RectTransform pivot = (0.5, 0.5)。`FollowPointer` 将 pivot 点（中心）放置在鼠标位置。然而 `BuildShapeCellsAbsolute` 创建的 shape cells 以 `anchorMin=zero`、`anchoredPosition` 从 `(0,0)` 开始——即 Ghost 的中心——向右向下展开。这导致整个视觉内容偏移到鼠标右下方。

### 修复方案
在 `FollowPointer` 中添加 `centeringOffset = (-size.x * 0.5f, size.y * 0.5f)`，将 Ghost 向左上方偏移半个尺寸，使 shape cell 网格的左上角对齐鼠标，视觉中心呈现在鼠标位置。

---

## Bug Fix: 拖拽 Ghost 位置偏移 — 2026-03-06 13:02

### 修改文件
- `Assets/Scripts/UI/DragDrop/DragGhostView.cs`
- `Assets/Scripts/UI/DragDrop/DragDropManager.cs`
- `Assets/Scripts/UI/InventoryItemView.cs`
- `Assets/Scripts/UI/SlotCellView.cs`
- `Assets/Scripts/UI/ItemOverlayView.cs`

### 内容简述
修复拖拽 Ghost 不跟随卡片上实际点击位置的问题。

### 根因
`ComputeDragOffset` 计算 `pressLocal - pointerLocal`，但由于 `BeginDrag` 是从 `OnBeginDrag` 调用的，此时 `eventData.position == eventData.pressPosition`，导致偏移量始终为 `Vector2.zero`。Ghost 中心始终吸附到鼠标位置，而非用户实际点击的位置。

### 修复方案
- `DragGhostView.ComputeDragOffset(eventData, sourceRect)`：新增 `sourceRect` 参数。现在通过 `GetWorldCorners` 计算点击位置相对于卡片左上角的偏移，再减去 Ghost 半尺寸，得到相对于 Ghost 中心的正确偏移量。
- `DragDropManager.BeginDrag(...)`：新增可选的 `sourceRect` 参数，透传给 `ComputeDragOffset`。
- `InventoryItemView`、`SlotCellView`、`ItemOverlayView`：在 `BeginDrag` 调用中传入 `GetComponent<RectTransform>()` 作为 `sourceRect`。

---

## Bug Fix: DragHighlightLayer 高亮 Tile 与实际格子严重偏移 — 根因修复 — 2026-03-06 13:45

### 修改文件
- `Assets/Scripts/UI/DragHighlightLayer.cs`
- `Assets/Scripts/UI/TrackView.cs`

### 内容简述
完全重写 `DragHighlightLayer`，改为直接读取每个 `SlotCellView` 的 `RectTransform` 来定位高亮 tile，而非用公式计算位置。

### 根因（原方案根本错误）
所有之前的尝试都试图通过 `anchoredPosition = offset + (col * step, -row * step)` 公式计算 tile 位置。这种方式必然失败，因为 `GridLayoutGroup` 可以使用任意 `childAlignment`（如 MiddleCenter）、padding 或 spacing，使格子偏离假设的原点。在不了解 `GridLayoutGroup` 全部内部参数的情况下，没有任何公式能可靠地还原精确的格子位置。

### 修复方案（直接读取格子坐标）
- `DragHighlightLayer.Initialize(container, cells[], gridCols)`：现在直接接收 `SlotCellView[]` 数组，无需 cellSize/cellGap/originOffset。
- `ShowHighlight()`：对每个形状格子偏移，计算 `cellIndex = row * gridCols + col`，然后直接从 `cells[cellIndex].GetComponent<RectTransform>()` 复制 `anchorMin/anchorMax/pivot/anchoredPosition/sizeDelta`。Tile 位置保证像素级精确。
- 移除所有基于公式的字段：`_cellSize`、`_cellGap`、`_originOffset`、`_cell0Rect`。
- `TrackView.CreateHighlightLayer()`：现在调用 `layer.Initialize(col.GridContainer, col.Cells, 2)`——无需 cell0Rect 和尺寸参数。

---

## Bug Fix: DragHighlightLayer 诊断日志清理 — 2026-03-06 13:51

### 修改文件
- `Assets/Scripts/UI/DragHighlightLayer.cs`

### 内容简述
在通过 InstanceID 日志确认 `tileParentID == cellParentID == containerID` 且 `tileWorld == cellWorld`（像素级精确匹配）后，移除了 `ShowHighlight()` 中的临时诊断 `Debug.Log`。保留了越界格子索引未命中时的 `Debug.LogWarning`。

---

## Bug Fix: 移除 Ghost 放置状态颜色高亮 — 2026-03-06 13:55

### 修改文件
- `Assets/Scripts/UI/DragDrop/DragGhostView.cs`

### 内容简述
移除了拖拽 Ghost 上的绿色/红色边框颜色反馈（`SetDropState`）。放置状态的视觉反馈现在**仅通过 `DragHighlightLayer` 显示在 Track 格子上**。Ghost 边框保持永久透明（`Color.clear`）。这简化了视觉语言：只有一个高亮位置（Track 格子），而非两个。

---

## Bug Fix: SAT/SAIL 高亮格子索引不匹配 — 2026-03-06 14:14

### 修改文件
- `Assets/Scripts/UI/DragHighlightLayer.cs`
- `Assets/Scripts/UI/TrackView.cs`
- `Assets/Scripts/UI/SlotCellView.cs`

### 根因
`SlotCellView` 对 SAIL/SAT 格子调用 `SetSingleHighlight(SlotType, col=0, row=CellIndex, state)`。在 `ShowHighlight` 内部，索引被重新计算为 `cellIndex = row * gridCols + col = CellIndex * 2 + 0`。对于 SAT（2×2 网格，gridCols=2），`CellIndex=1`（右上角）→ `cellIndex=2`（左下角），导致高亮出现在错误的格子上。

### 修复方案
- 新增 `DragHighlightLayer.ShowHighlightAtCellIndex(int cellIndex, DropPreviewState)`——直接读取 `cells[cellIndex].RectTransform`，无需行列转换。
- 新增 `TrackView.SetSingleHighlightAtIndex(SlotType, int cellIndex, DropPreviewState)`——路由到新方法。
- 将 `SlotCellView` 的 SAIL/SAT 分支改为调用 `SetSingleHighlightAtIndex(SlotType, CellIndex, state)`，而非 `SetSingleHighlight(SlotType, 0, CellIndex, state)`。
- 移除 `DragHighlightLayer.ShowHighlight()` 中残留的 `[HL]` 调试日志。

---

## Bug Fix: ItemOverlayView 幽灵/重复渲染（PRISM/CORE）— 2026-03-06 15:17

### 修改文件
- `Assets/Scripts/UI/TrackView.cs`
- `Assets/Scripts/UI/ItemIconRenderer.cs`

### 根因
`LayoutElement.ignoreLayout = true` 是在 `SetParent()` **之后**才添加到 overlay GameObject 上的。Unity 的 `GridLayoutGroup` 在 `SetParent()` 被调用时**立即**重新定位子节点，覆盖了 `ItemOverlayView.Setup()` 中设置的拉伸锚点（`anchorMin=0,0`、`anchorMax=1,1`）。等到 `ignoreLayout = true` 生效时，overlay 的锚点已被覆写为某个网格格子的位置，导致 overlay 渲染在错误位置（幽灵/重复渲染现象）。

`BuildShapeCellsFromCells` 和 `BuildIconFromCell` 中 overlay 子节点也存在相同的顺序 bug。

### 修复方案
- 在 `TrackView.RefreshColumn` 中：将 `overlayGo.AddComponent<LayoutElement>()` + `ignoreLayout = true` 移到 `SetParent(gridContainer, false)` **之前**。
- 在 `ItemIconRenderer.BuildShapeCellsFromCells` 中：将 `go.AddComponent<LayoutElement>()` + `ignoreLayout = true` 移到 `go.transform.SetParent(parent, false)` 之前。
- 在 `ItemIconRenderer.BuildIconFromCell` 中：同样修复——`LayoutElement` 在 `SetParent` 之前添加。

### 关键洞察
SAIL/SAT 从未出现此 bug，因为它们直接调用 `SlotCellView.SetItem()`——不创建 overlay GameObject，`GridLayoutGroup` 从不介入。这是指向 overlay 创建顺序为根因的诊断线索。

---

## Bug Fix: PRISM/CORE 部件无法拖回背包 — 2026-03-06 15:42

### 修改文件
- `Assets/Scripts/UI/SlotCellView.cs`
- `Assets/Scripts/UI/TrackView.cs`
- `Assets/Scripts/UI/ItemOverlayView.cs`

### 根因
`SetHiddenByOverlay()` 将 `DisplayedItem` 设为 `null`。`SlotCellView.OnBeginDrag` 检查 `if (DisplayedItem == null) return`，导致拖拽在开始前被静默中止。

SAIL/SAT 不受影响，因为它们调用 `SetItem(item)` 将 `DisplayedItem = item`，保持了拖拽路径完整。

### 修复方案
- `SlotCellView.SetHiddenByOverlay(StarChartItemSO item)`：修改签名以接收 `item` 参数，设置 `DisplayedItem = item`（保留拖拽来源），仅清除视觉（背景/图标/标签）。
- `TrackView.RefreshColumn`：更新调用为 `cells[cellIndex].SetHiddenByOverlay(item)`。
- `ItemOverlayView.Setup`：移除上一次失败修复尝试中错误添加的 `raycastBlocker` AddComponent。

### 关键洞察
修复方案直接镜像 SAIL/SAT 的行为：在格子上保留 `DisplayedItem`，使 `OnBeginDrag` 能够触发。Overlay 纯粹是视觉层——底层格子仍是功能性的拖拽来源。**Overlay = 纯视觉，Cell = 交互**。

---

## Bug Fix: SAIL 放置后默认跳到 Primary 轨道第一格 — 2026-03-06 15:51

### 修改文件
- `Assets/Scripts/UI/TrackView.cs`

### 根因
SAIL 和 SAT 是**全局槽位**——不属于某个特定轨道。`RefreshSailColumn()` 和 `RefreshSatColumn()` 只在 **Primary 轨道**上显示已装备的部件；Secondary 轨道显示空格。

然而 `HasSpaceForSail()` 和 `HasSpaceForSat()` 没有 `isPrimary` 守卫。当用户将 SAIL 部件拖到 **Secondary 轨道的 SAIL 格子**时，`ComputeDropValidity` 返回 `true`（有空间），`DropTargetTrack` 被设为 Secondary 轨道，`EquipLightSail()` 成功调用。

`RefreshAllViews()` 后，SAIL 出现在 **Primary 轨道的第一格**（因为 `RefreshSailColumn` 始终在那里渲染），看起来像是部件"跳"到了错误的位置。

### 修复方案
为两个委托添加 `isPrimary` 守卫：
- `HasSpaceForSail`：若 `_track.Id != Primary` 则立即返回 `false`
- `HasSpaceForSat`：若 `_track.Id != Primary` 则立即返回 `false`

Secondary 轨道的 SAIL/SAT 格子现在显示为无效放置目标（红色高亮），防止令人困惑的视觉跳转。

### 关键洞察
全局槽位（SAIL/SAT）只能在 Primary 轨道上接受放置。Secondary 轨道渲染空格纯粹是为了视觉对称——这些格子不应成为功能性的放置目标。

---

### ✨ Refactor: 伴星（Satellite）从 Shared 改为 Per-Track — 2026-03-06 22:30

**功能说明：** 将伴星数据从 `LoadoutSlot` 级别（Shared）迁移到 `WeaponTrack` 级别（Per-Track），使 Primary 和 Secondary 轨道可以独立配置不同的伴星组合。消除了 `isPrimary` 守卫逻辑，Secondary TrackView 不再显示无意义的空格占位。

**修改文件：**
- `Assets/Scripts/Combat/StarChart/WeaponTrack.cs`
  - 新增 `public readonly List<SatelliteSO> EquippedSatelliteSOs = new()` 字段
  - `ClearAll()` 中新增 `EquippedSatelliteSOs.Clear()`
- `Assets/Scripts/Combat/StarChart/LoadoutSlot.cs`
  - 移除 `EquippedSatelliteSOs` 字段（已迁移至 WeaponTrack）
  - `Clear()` 中移除 `EquippedSatelliteSOs.Clear()`（由 WeaponTrack.ClearAll() 负责）
- `Assets/Scripts/Core/Save/SaveData.cs`
  - `TrackSaveData` 新增 `List<string> SatelliteIDs` 字段（Per-Track 存档）
  - `LoadoutSlotSaveData.SatelliteIDs` 标记为 `[Obsolete]`，保留用于旧存档迁移
- `Assets/Scripts/Combat/StarChart/StarChartController.cs`
  - `_satelliteRunners` 拆分为 `_primarySatRunners` 和 `_secondarySatRunners` 两个并行数组
  - `EquipSatellite(SatelliteSO, WeaponTrack.TrackId)` 新增 TrackId 参数
  - `UnequipSatellite(SatelliteSO, WeaponTrack.TrackId)` 新增 TrackId 参数
  - `GetEquippedSatellites(WeaponTrack.TrackId)` 新增 TrackId 参数
  - `Update()` 分别 Tick Primary/Secondary 伴星 Runner 列表
  - `DisposeSlotRunners()` / `RebuildSlotRunners()` 按轨道分组处理
  - `ExportTrack()` 新增 SatelliteIDs 序列化
  - `ImportFromSaveData()` 新增旧格式迁移逻辑（旧 SatelliteIDs → PrimaryTrack）
  - 新增 `ImportTrackSatellites()` 辅助方法（含 ID 解析失败警告）
  - `InitializeSailAndSatellites()` debug 伴星改为写入 PrimaryTrack
- `Assets/Scripts/UI/TrackView.cs`
  - `RefreshSatColumn()` 直接读取 `_track.EquippedSatelliteSOs`，移除 `isPrimary` 守卫
  - `HasSpaceForSat()` 移除 Primary 限制，改为检查当前轨道伴星数量（< 4）
- `Assets/Scripts/UI/StarChartPanel.cs`
  - `GetEquippedLocation()` SAT 改为检查 Primary/Secondary 各自的 EquippedSatelliteSOs
  - `CountEquipped()` 改为累加两个轨道的伴星数量
  - `EquipItem()` SAT 改为 `EquipSatellite(sat, _selectedTrack.Id)`
  - `UnequipItem()` SAT 改为按归属轨道调用 `UnequipSatellite(sat, trackId)`
  - `IsItemEquipped()` SAT 改为检查两个轨道的 EquippedSatelliteSOs
- `Assets/Scripts/UI/DragDrop/DragDropManager.cs`
  - `EquipToTrack()` SAT 改为 `EquipSatellite(sat, track.Id)`，maxSats 改为 4（每轨道 2×2）
  - `UnequipFromTrack()` SAT 改为 `UnequipSatellite(sat, track.Id)`

**存档迁移规则：**
- 旧格式：`LoadoutSlotSaveData.SatelliteIDs` 非空 → 全部迁移到 `PrimaryTrack.SatelliteIDs`，Secondary 为空
- 新格式：`TrackSaveData.SatelliteIDs` 分别存储 Primary/Secondary 的伴星 ID
- 无法解析的 ID 跳过并输出 `Debug.LogWarning`，不抛出异常

**Per-Track Runner 扩展点：**
- `_primarySatRunners[slotIndex]` 和 `_secondarySatRunners[slotIndex]` 分别管理两个轨道的 Runner
- 未来如需 Per-Track Context（如 `WeaponTrack.ResetCooldown` 触发），只需向 `SatelliteRunner` 构造函数传入轨道引用，无需修改基类

---

## SAT 拖拽位置修复 — 完全参考 Prism/Core 实现 (2026-03-06 23:19)

### 修改文件
- `Assets/Scripts/Combat/StarChart/WeaponTrack.cs`
- `Assets/Scripts/Combat/StarChart/StarChartController.cs`
- `Assets/Scripts/UI/TrackView.cs`
- `Assets/Scripts/UI/SlotCellView.cs`
- `Assets/Scripts/UI/DragDrop/DragDropManager.cs`

### 问题描述
SAT 无论拖到哪个格子，都会自动吸附到左上角第一格。

### 根本原因
SAT 的数据层使用 `List<SatelliteSO>`（无位置信息），`RefreshSatColumn()` 总是从 `cells[0]` 开始按列表顺序填充，导致 SAT 始终显示在第一个空格子。同时 `ComputeDropValidity` 中 SAT 走 `HasSpaceForItem` delegate 分支，不进行 anchor 计算，`DropTargetAnchorCol/Row` 始终为 (0,0)。

### 技术方案
完全参考 Prism/Core 的 `SlotLayer<T>` 实现：

1. **WeaponTrack.cs**：添加 `SlotLayer<SatelliteSO> _satLayer`（固定 2 cols × 2 rows），添加 `EquipSatellite(sat, col, row)` / `EquipSatellite(sat)` / `UnequipSatellite(sat)` 方法，`EquippedSatelliteSOs` 改为 `IReadOnlyList<SatelliteSO>`（从 `_satLayer.Items` 派生）

2. **StarChartController.cs**：添加 `EquipSatellite(sat, trackId, anchorCol, anchorRow)` 重载，所有直接操作 `EquippedSatelliteSOs.Add/Remove` 的地方改为调用 `track.EquipSatellite/UnequipSatellite`

3. **TrackView.cs**：移除 `HasSpaceForSat` delegate 注入，`RefreshSatColumn()` 改为 `RefreshColumn(_satColumn, _track?.SatLayer, StarChartItemType.Satellite)`，删除废弃的 `RefreshSatColumn()` 方法和 `_debugSatSlots` 字段

4. **SlotCellView.cs**：`ComputeDropValidity` 中 SAT 走 `FindBestAnchor` 路径（和 Core/Prism 一样），使用 `SatLayer.Cols` 计算 `gridCols`；高亮逻辑中 SAT 走 `SetSingleHighlightAtIndex` 路径

5. **DragDropManager.cs**：`EquipToTrack` 中 SAT 分支使用方法开头已声明的 `anchorCol/anchorRow`，先检查目标格子是否有占用者并 evict，再调用 `EquipSatellite(sat, trackId, anchorCol, anchorRow)`

### 结果
SAT 现在完全参考 Prism/Core 的实现，支持按位置放置，拖到哪个格子就显示在哪个格子。编译零错误。

---

## SAIL 拖拽修复 — 无法放入/取下部件 (2026-03-06 23:47)

### 修改文件
- `Assets/Scripts/UI/StarChartPanel.cs`
- `Assets/Scripts/UI/DragDrop/DragDropManager.cs`

### 问题描述
1. SAIL 列无法放入任何部件（拖拽时格子不显示 Valid 高亮）
2. 已装备的 SAIL 部件无法取下（拖到 Inventory 无效）

### 根本原因

**问题 1（无法放入）**：
`_sharedSailColumn` 在 `UICanvasBuilder.BuildTypeColumn()` 中创建，但**没有调用 `TypeColumn.Initialize()`**。`Initialize()` 负责将 `SlotType.LightSail` 设置到每个格子的 `SlotCellView.SlotType` 字段。
- 格子的 `SlotType` 保持默认值（`SlotType.Core = 0`）
- `SlotCellView.IsTypeMatch()` 检查 `SlotType.LightSail`，但格子是 `Core`，返回 `false`
- `ComputeDropValidity` 返回 `false`，格子不接受任何拖拽

**问题 2（无法取下）**：
- SAIL 格子的 `OwnerTrack = null`（共享列，不属于任何 TrackView）
- `SlotCellView.OnBeginDrag` 创建 `DragPayload(item, DragSource.Slot, OwnerTrack?.Track)` → `SourceTrack = null`
- `ExecuteUnequipDrop()` 中 `if (sourceTrack != null)` 条件不满足，直接跳过，SAIL 不会被卸下
- 另外，`DropTargetIsSailColumn = (OwnerTrack == null && SlotType == LightSail)` 因为 `SlotType` 不对，始终为 `false`

### 技术方案

1. **StarChartPanel.cs**：在 `Bind()` 中注入 `HasSpaceForItem` delegate 之前，先调用 `_sharedSailColumn.Initialize(SlotType.LightSail, StarChartTheme.SailColor, null)`，正确设置每个格子的 `SlotType = LightSail` 和 `OwnerTrack = null`

2. **DragDropManager.cs**：在 `ExecuteUnequipDrop()` 中添加 `else if (item is LightSailSO)` 分支，当 `sourceTrack == null` 时直接调用 `_controller?.UnequipLightSail()`（SAIL 是共享列，不需要 track 引用）

### 结果
两个修改合计 2 处，编译零错误。SAIL 现在可以正常放入和取下部件。

---

## SAIL 吸附左上角 & 无高亮修复 (2026-03-06 23:38)

### 修改文件
- `Assets/Scripts/UI/StarChartPanel.cs`

### 问题描述
1. SAIL 无论拖到哪个格子，都会吸附到左上角（cells[0]）
2. SAIL 格子拖拽时没有红绿高亮反馈

### 根本原因

**问题 1（吸附左上角）**：
`RefreshSharedSailColumn()` 只刷新 `cells[0]`，但 SAIL 列有 4 个格子（cells[0..3]），其他 3 个格子没有被隐藏，用户可以拖到任意格子。由于 SAIL 只有 1 个槽位，`RefreshSharedSailColumn()` 永远只在 `cells[0]` 显示，所以无论拖到哪个格子，视觉上都"跳"到 cells[0]（左上角）。

**问题 2（无高亮）**：
`_sailHighlightLayer` 是 `[SerializeField]` 直接引用，但**从未调用 `Initialize()`**。`DragHighlightLayer.ShowHighlightAtCellIndex()` 第一行检查 `if (_container == null) return`，由于 `_container` 未被初始化（为 null），所有高亮调用都直接返回，不显示任何颜色。

### 技术方案（完全参考 Prism/Core 实现）

1. **修复高亮**：在 `Bind()` 中调用 `_sharedSailColumn.Initialize()` 之后，立即调用：
   ```csharp
   _sailHighlightLayer.Initialize(_sharedSailColumn.GridContainer, _sharedSailColumn.Cells, 2);
   ```
   这与 `TrackView.CreateHighlightLayer()` 的逻辑完全一致。

2. **修复吸附**：重构 `RefreshSharedSailColumn()` 完全参考 `TrackView.RefreshColumn()` 的实现：
   - 用 CanvasGroup 隐藏 cells[1..3]（`alpha=0, interactable=false, blocksRaycasts=false`）
   - 只保留 cells[0] 可见和可交互
   - 这样用户只能拖到 cells[0]，不会有"吸附"的感觉

### 结果
1 个文件修改，编译零错误。SAIL 现在只显示 1 个可见格子，高亮正常工作（绿色=可放入，红色=不可放入，橙色=替换）。
