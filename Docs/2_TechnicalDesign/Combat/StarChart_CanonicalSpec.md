# StarChart 现役规范（Canonical Spec）

> **文档定位**
> 本文档回答一个问题：**"星图今天是怎么运作的，谁负责哪一段？"**
>
> - 只写**现役事实**，不写"应该怎么做"（施工规则归 `Implement_rules.md` 第 8 节）
> - 只写**权威口径**，不写"之前是怎么演化的"（演化史归 `ImplementationLog.md`）
> - 只写**代码里真正在跑的链路**，不抄 GDD 的设计意图（设计意图归 `Docs/1_GameDesign/`）
>
> **产物协作**
> - 资产路径 / owner 工具 / 状态映射 → `StarChart_AssetRegistry.md`
> - 施工规则 / 踩坑 / 验收清单 → `Implement_rules.md` 第 8 节
> - 三者如冲突：**代码 > 本 Spec > Registry > Rules**。代码是最终真相。
>
> **维护原则**
> 当星图的现役链路、owner 或职责边界**发生变化**时，本文档必须同步更新，不允许用"后续再对齐"之类 TODO 搁置。

---

## 1. 模块边界

### 1.1 Runtime 代码

- `Assets/Scripts/Combat/StarChart/`（主目录）
  - 控制器：`StarChartController.cs`（顶层编排 + UI / Save 对外 API；内部委托下列三个协作器）
  - 装备管理：`LoadoutManager.cs`（`Equip*` / `Unequip*` / `SwitchLoadout` / Slot 级别 Dispose / Rebuild，Pure C#，由 Controller 构造注入）
  - 发射分发：`ProjectileSpawner.cs`（`SpawnProjectile` + 四个家族分支 + `InstantiateModifiers`，Pure C#，由 Controller 构造注入）
  - 存档序列化：`StarChartSaveSerializer.cs`（`Export` / `Import` + 所有 `*Track*` 子例程，Pure C#，由 Controller 构造注入）
  - 轨道：`WeaponTrack.cs` / `LoadoutSlot.cs`
  - 快照与参数：`SnapshotBuilder.cs` / `FiringSnapshot.cs` / `ProjectileParams.cs`
  - SO 基类与子类：`StarChartItemSO.cs` / `StarCoreSO.cs` / `PrismSO.cs` / `LightSailSO.cs` / `SatelliteSO.cs`
  - 槽位与形状：`SlotLayer.cs` / `ItemShapeHelper.cs` / `StarChartEnums.cs`
  - 修正器与跨层上下文：`StatModifier.cs` / `StarChartContext.cs`
  - 解耦接口：`IStarChartItemResolver.cs`
  - 发射点：`FirePoint.cs`
  - LightSail 子目录：`LightSail/LightSailBehavior.cs` / `LightSailRunner.cs` / `SpeedDamageSail.cs`
  - Satellite 子目录：`Satellite/SatelliteBehavior.cs` / `SatelliteRunner.cs` / `AutoTurretBehavior.cs`
- `Assets/Scripts/Combat/Projectile/`
  - 投射物本体：`Projectile.cs` / `LaserBeam.cs` / `EchoWave.cs`
  - Modifier 接口与实现：`IProjectileModifier.cs` + `BoomerangModifier` / `BounceModifier` / `HomingModifier` / `MinePlacerModifier` / `SlowOnHitModifier`
- UI 层（消费方，不属于 Runtime 主链）：`Assets/Scripts/UI/StarChart/`

### 1.2 Editor 代码

`Assets/Scripts/Combat/Editor/`：

- `ShebaAssetCreator.cs` — 新批次 SO 的**唯一推荐 owner**
- `Batch5AssetCreator.cs` — Legacy（历史批次），只保留不新增
- `ShapeContractValidator.cs` — 形状静态校验
- `EchoWaveProceduralPreviewMenu.cs` — 见 `ProceduralPresentation_WorkflowSpec.md`，**只能 preview，不接管 Runtime**
- `BestiaryImporter.cs` / `EnemyAssetCreator.cs` — **与星图无关**，这里仅列出以划清边界

### 1.3 数据资产

- `Assets/_Data/StarChart/Cores|Prisms|Sails|Satellites` — SO 资产
- `Assets/_Data/StarChart/Prefabs` — Projectile / Modifier prefab
- `Assets/_Data/StarChart/PlayerInventory.asset` — 玩家持有清单（`StarChartInventorySO`）
- 所有资产的路径、owner、状态映射见 `StarChart_AssetRegistry.md`

### 1.4 外部依赖

- `ProjectArk.Ship`：`ShipMotor` / `ShipAiming` / `InputHandler`
- `ProjectArk.Heat`：`HeatSystem`
- `ProjectArk.Core`：`PoolManager` / `PoolReference` / `IPoolable` / `IDamageable` / `DamagePayload` / `ServiceLocator` / `CombatEvents`
- `ProjectArk.Core.Save`：`StarChartSaveData` / `LoadoutSlotSaveData` / `TrackSaveData`

### 1.5 关联文档

- `Implement_rules.md` 第 8 节（施工规则）
- `StarChart_AssetRegistry.md`（资产 owner 映射）
- `ProceduralPresentation_WorkflowSpec.md`（preview 工具边界，与 `EchoWaveProceduralPreviewMenu` 相关）
- GDD（如存在）：`Docs/1_GameDesign/` 对应章节

---

## 2. 核心概念与语义

### 2.1 四家族 × 四层装备矩阵

- **家族（Family）**：`Matter` / `Light` / `Echo` / `Anomaly`
  - 定义在 `StarChartEnums.cs` 的 `CoreFamily` 枚举
  - 决定 `ProjectileSpawner.SpawnProjectile()` 走哪个分支（见 §3.5）
- **四层装备类型**：`Core` / `Prism` / `LightSail` / `Satellite`
  - 定义在 `StarChartItemType` 枚举
  - 每种类型继承自 `StarChartItemSO`，共享 `DisplayName` / `Shape` / `HeatCost` 等基础字段
- **Prism 家族（PrismFamily）**：`Fractal` / `Rheology` / `Tint`
  - 只有 `Tint` 走"组件注入"路径，其余两家走"数值聚合"路径（见 §3.6）

### 2.2 Loadout 与 Track 的层级结构

- 顶层：**3 个独立 Loadout Slot**（`LoadoutSlot[]`），索引 0/1/2，在 `StarChartController.Awake` 常量 `SlotCount = 3` 固定创建
- 每个 Loadout Slot 内部包含：
  - `PrimaryTrack`（`WeaponTrack`）
  - `SecondaryTrack`（`WeaponTrack`）
  - `SailLayer`（`SlotLayer<LightSailSO>`，单层共享给两条 Track）
- 每个 Track 内部包含：
  - `CoreLayer`（`SlotLayer<StarCoreSO>`）
  - `PrismLayer`（`SlotLayer<PrismSO>`）
  - `SatLayer`（`SlotLayer<SatelliteSO>`，**Satellite 是 Per-Track 的**）
- 只有**当前激活的 Slot** 会在 `Update` 中被 Tick / 响应输入（`_activeLoadoutIndex` 0-based，通过 `SwitchLoadout(int)` 切换）

### 2.3 SlotLayer 形状系统

- 所有 4 类 layer 都使用泛型 `SlotLayer<T>`（T: `StarChartItemSO` 子类）
- 网格初始容量：**2 列 × 1 行**（见 `WeaponTrack` 构造函数与 `LoadoutSlot` 构造函数）
- 通过 `TryUnlockColumn` / `TryUnlockRow` 扩展容量，**只扩展不收缩**（`SetSailLayerCols` / `SetLayerCols` 均体现）
- 每个 item 的形状来自 `StarChartItemSO.Shape`（枚举 `ItemShape`，支持 1×1/1×2H/2×1V/L/L-Mirror/2×2）
- `TryPlace(item, col, row)` 精确锚定位置；`TryEquip(item)` 自动找首个合法位置
- **硬上限**：`SlotLayer.MAX_COLS = MAX_ROWS = 4`（见 `SlotLayer.cs:22-25`），内部 `_grid[MAX_ROWS, MAX_COLS]` 按 4×4 预分配，运行时无重分配。`TryUnlockColumn` / `TryUnlockRow` 超出上限时返回 false。

### 2.4 Track 激活与缓存语义

- Track 内缓存 `TrackFiringSnapshot`（`_cachedSnapshot`），通过 `_snapshotDirty` 标记延迟重建
- 任何 `EquipCore` / `UnequipCore` / `EquipPrism` / `UnequipPrism` / `ClearAll` / `SetLayerCols` 都会触发 `MarkDirty()`
- `TryFire()` 在 dirty 时调用 `RebuildSnapshot()` = `SnapshotBuilder.Build(coreLayer.Items, prismLayer.Items)`
- **Satellite 的装卸不会弄脏 Snapshot**（`EquipSatellite` 只发 `OnLoadoutChanged`，不调 `MarkDirty`），因为 Satellite 不参与 projectile 数值计算

### 2.5 Satellite 独立运行时模型（不参与 Spawn 链）

- **Satellite 不走 `ExecuteFire` / `ProjectileSpawner.SpawnProjectile` 分发链**，也不参与 Snapshot 聚合
- 运行时循环：`StarChartController.Update` → `LoadoutManager.TickActive(dt)` → `SatelliteRunner.Tick(dt)` → 对每个已装 Satellite 执行
  - `EvaluateTrigger(ctx)`：按 SO 配置的触发条件（距离 / 计时 / 手动等）判断
  - 命中则 `Execute(ctx)`（具体行为由 `SatelliteBehavior` 子类实现，如 `AutoTurretBehavior`）
  - `InternalCooldown` 由 Runner 自身维护，与 Track 冷却**完全独立**
- 因此：装/卸 Satellite 不会 MarkDirty 也不会重建 Snapshot；它是挂在 Controller 上的**并行子系统**
- Sail 层仍走 `ModifyProjectileParams` 接入 Spawn 链（见 §3.5），这是 Sail 与 Satellite 的关键差异

---

## 3. 现役发射主链（owner 标注）

> 以下每一步都是**事实**，写法是"谁在什么时候调谁"。不是"应该怎么做"。

### 3.1 输入采集

- **Owner**：`InputHandler`（`ProjectArk.Ship`）
- `IsFireHeld` / `IsSecondaryFireHeld` 由 New Input System 的 `performed` / `canceled` 回调维护
- `StarChartController.Update` 每帧读这两个字段

### 3.2 冷却闸门

- **Owner**：`StarChartController.CanFireTrack(WeaponTrack track)`
- 条件：`track.CanFire && (_heatSystem == null || _heatSystem.CanFire())`
  - `track.CanFire` ≡ `!_coreLayer.IsEmpty && _fireCooldownTimer <= 0f`
  - `HeatSystem.CanFire()` 由 `ProjectArk.Heat` 模块定义，过热期间返回 false

### 3.3 Snapshot 构建（单一权威路径）

- **Owner**：`WeaponTrack.TryFire()` → `SnapshotBuilder.Build(cores, prisms)`
- **禁止绕过**：任何代码都不得直接构造 `CoreSnapshot` / `TrackFiringSnapshot`，也不得跳过 `SnapshotBuilder`
- 关键步骤（严格按此顺序）：
  1. `AggregatePrismModifiers(prisms)` 把所有 Prism 的 `StatModifier` 分别聚合进 `s_addAccumulator` / `s_mulAccumulator`（静态复用字典，零分配）
  2. `CollectTintModifierPrefabs(prisms)` 收集所有 `PrismFamily.Tint` 且 prefab 上确实有 `IProjectileModifier` 组件的 prefab 清单（**双重过滤**：family 匹配 + 组件存在）
  3. 对每个 Core 调用 `BuildCoreSnapshot(core, coreCount, tintModifierPrefabs)`：
     - Multiply 先，Add 后（`result *= mulValue; result += addValue / coreCount;`）
     - **Add 平均分配给每个核心**（除以 `coreCount`），Multiply 全额应用
     - Clamp 各数值到合法范围（`Damage ≥ 0`, `ProjectileSpeed ≥ 0.1` 等）
  4. 累加每个 Core 的 `HeatCost` / `RecoilForce` / `ProjectileCount`，取**最慢 FireInterval** 作为 Track 冷却
  5. 加上 Prism 自身的 `HeatCost`（flat，**不参与 Add/Multiply 聚合**，直接累加到 `totalHeat`）
  6. **弹幕硬上限 `MAX_PROJECTILES_PER_FIRE = 20`**：
     - 超过时按比例裁剪 `ProjectileCount`（`CapProjectileCount`），最后一个核心拿剩余配额
     - 超额部分按 5% / 发换算为 `excessDamageBonus`，均分到所有核心的 Damage（`snapshot.Damage *= (1f + bonusPerCore)`）
  7. 返回 `TrackFiringSnapshot`；Core 列表为空时返回 null

### 3.4 冷却复位

- Track 冷却：`_fireCooldownTimer = _cachedSnapshot.TrackFireInterval`（最慢核心的 1/FireRate）
- Track 提供 `ResetCooldown()` 立即清零，供 Sail / Satellite 能力调用（如 Graze Engine 风格）

### 3.5 投射物分发（按 CoreFamily switch）

**Owner**：`ProjectileSpawner.SpawnProjectile(track, coreSnap, direction, spawnPos)`（`internal` 方法，由 `StarChartController.ExecuteFire` 调用；Controller 只编排时序，分发职责在 Spawner）

```csharp
switch (coreSnap.Family) {
  case CoreFamily.Matter:  SpawnMatterProjectile(...);  break;
  case CoreFamily.Light:   SpawnLightBeam(...);         break;
  case CoreFamily.Echo:    SpawnEchoWave(...);          break;
  case CoreFamily.Anomaly: SpawnAnomalyEntity(...);     break;
  default: // Debug.LogWarning + fallback 到 Matter
}
```

每个分支的现役行为：

| 分支 | 池取出组件 | 失败降级 | ProjectileSize | Modifier 注入 |
|---|---|---|---|---|
| Matter | `Projectile` | 无组件则 return | localScale = size | `TintModifierPrefabs` |
| Light | `LaserBeam` | **先归还 pool 再 fallback 到 Matter** | 不处理 | `TintModifierPrefabs` |
| Echo | `EchoWave` | **先归还 pool 再 fallback 到 Matter** | localScale = size | `TintModifierPrefabs` |
| Anomaly | `Projectile` | 无组件则 return | localScale = size | `TintModifierPrefabs` **+** `AnomalyModifierPrefab`（独立实例化） |

共同逻辑：
- 方向角：`angle = Mathf.Atan2(direction.y, direction.x) * Rad2Deg - 90f`
- Pool：`track.GetProjectilePool(coreSnap.ProjectilePrefab)` 委托 `PoolManager`（初始预热策略在 `WeaponTrack.InitializePools` 按家族差异化：Matter 20/50, Light 5/20, Echo 5/15, Anomaly 10/30；Anomaly 额外预热 `AnomalyModifierPrefab` 10/30）
- Spread 分布：多发时在 `[-Spread, +Spread]` 区间均匀扇形分布；单发且 Spread > 0 时随机偏移；Spread ≈ 0 时沿 `direction`
- Modifier 注入：**先 `ToProjectileParams`，再由 Sail Runner `ModifyProjectileParams(ref parms)` 注入**（Sail 层不走 Snapshot）。**每个 Spawn 分支（Matter/Light/Echo/Anomaly）各自单独调用一次 `ModifyProjectileParams`**，不是全局一次——见 `ProjectileSpawner.cs` 内 `SpawnMatterProjectile / SpawnLightBeam / SpawnEchoWave / SpawnAnomalyEntity` 四处调用点
- **LaserBeam / EchoWave 的 fallback 路径必须先归还 pool** 再调 `SpawnMatterProjectile`（避免无组件对象泄漏池外）

### 3.6 Modifier 注入的两条独立路径

> 这是星图最容易混淆的地方，两条路径**不可互换**。

**路径 A：Tint Prism → 所有家族共同的路径**

- 数据流：`PrismSO.ProjectileModifierPrefab`（仅 `Family == Tint` 且有 `IProjectileModifier` 组件时收集）
- 聚合点：`SnapshotBuilder.CollectTintModifierPrefabs` → `CoreSnapshot.TintModifierPrefabs`
- 注入点：`ProjectileSpawner.InstantiateModifiers(targetObj, coreSnap.TintModifierPrefabs)`（`private static` 方法）
- 适用：所有四个 `SpawnXxx` 分支**都调这一次**

**路径 B：Anomaly Core → 仅 Anomaly 家族额外注入**

- 数据流：`StarCoreSO._anomalyModifierPrefab` → `CoreSnapshot.AnomalyModifierPrefab`（SnapshotBuilder 直通，不聚合、不过滤）
- 注入点：**仅 `SpawnAnomalyEntity` 内部**再额外调一次 `InstantiateModifiers`，结果 `AddRange` 到 `modifiers` 列表后才 `projectile.Initialize(...)`
- 适用：**仅 Anomaly 分支**

**InstantiateModifiers 的实现**（`ProjectileSpawner.InstantiateModifiers`，`private static` 方法）：
- 对 prefab 上的每个 `IProjectileModifier` 组件，在 `targetObj` 上 `AddComponent` 同类型
- 用 `JsonUtility.ToJson(srcComponent)` + `JsonUtility.FromJsonOverwrite(json, newComponent)` 做**序列化字段深拷贝**
- 避免多个 projectile 共享同一 modifier 实例

### 3.7 反馈与收尾

按顺序（全部在 `ExecuteFire` 内）：

1. **每个 Core 每发**：`ProjectileSpawner.SpawnProjectile`（循环）
2. **每个 Core 一次**：`SpawnMuzzleFlash(track, coreSnap, direction, spawnPos)`
3. **每个 Core 一次**：`PlayFireSound(coreSnap)`（随机 pitch 在 `±FireSoundPitchVariance` 范围内）
4. **Track 一次**：`_shipMotor.ApplyImpulse(-direction * snapshot.TotalRecoilForce)` 后坐力
5. **Track 一次**：`_heatSystem?.AddHeat(snapshot.TotalHeatCost)`
6. **事件**：`OnTrackFired?.Invoke(track.Id)`（Track 粒度）
7. **全局广播**：
   - `StarChartController.OnWeaponFired`（static event，`(Vector2 pos, float radius)`）
   - `CombatEvents.RaiseWeaponFired(spawnPos, DEFAULT_NOISE_RADIUS)`（跨程序集事件总线）
   - `DEFAULT_NOISE_RADIUS = 15f`，固定常量
   - 订阅者：敌人感知（`EnemyPerception`）等

---

## 4. 依赖反转与跨程序集边界

### 4.1 IStarChartItemResolver

- **定义**：`Assets/Scripts/Combat/StarChart/IStarChartItemResolver.cs`（Combat 层）
- **实现**：UI / Save 层（拥有 Inventory 资产的引用）
- **用途**：`StarChartController.ImportFromSaveData(data, resolver)` 作为对外 API 入口（保持向后兼容），内部委托 `StarChartSaveSerializer.Import(data, resolver)` 解析存档；二者语义等价，`DisplayName` 解析仍走本接口
- **方法**：`FindCore` / `FindPrism` / `FindLightSail` / `FindSatellite`
- **约束**：Combat 层**不引用** UI / Save 的具体 Inventory 实现，只依赖这个接口

### 4.2 StarChartContext（readonly struct）

- 运行时依赖包，`StarChartController.Awake` 构造一次，传给 `LightSailRunner` / `SatelliteRunner`
- 字段：`ShipMotor Motor` / `ShipAiming Aiming` / `InputHandler Input` / `HeatSystem Heat`（可为 null）/ `StarChartController Controller` / `Transform ShipTransform`
- Runner 用它访问 Ship 侧的状态，无需反向 using UI / Save 层

### 4.3 CombatEvents（跨程序集事件总线）

- 定义在 `ProjectArk.Core`（最低层）
- `CombatEvents.OnWeaponFired` — 上层 publish，任意层 subscribe
- 方向：**低层定义 + 高层使用**（Core → Combat/Enemy/UI），不允许反向

### 4.4 ServiceLocator 注册

- `StarChartController.Awake`：`ServiceLocator.Register<StarChartController>(this)`
- `StarChartController.OnDestroy`：`ServiceLocator.Unregister<StarChartController>(this)`
- 其他系统（例如 UI 面板）通过 `ServiceLocator.Get<StarChartController>()` 获取，不用 `FindObjectOfType`

### 4.5 禁止事项（现役不变量）

- `ProjectArk.Core` 不得反向 `using ProjectArk.Combat`
- `ProjectArk.Combat` 不得反向 `using ProjectArk.UI`
- 需要高层类型被低层使用时，在低层定义接口、高层实现（见 `IStarChartItemResolver`）

---

## 5. 运行时数据隔离

### 5.1 SO 是 authored data

- `StarChartItemSO` 及其子类字段**仅在 Editor 配置**，运行时**只读**
- 任何需要变化的量都在 `CoreSnapshot` 或 Runner 实例字段中
- SnapshotBuilder 读取 `core.BaseDamage` / `core.FireRate` / ... 写入 `snapshot.Damage` / `snapshot.FireRate`，原 SO 不变

### 5.2 CoreSnapshot / TrackFiringSnapshot

- 均为 `class`（非 readonly），但**在 StarChartController 链路内视为只读快照**
- 每次 `RebuildSnapshot` 替换整个对象，不就地修改
- `WithDamageMultiplied`（`ProjectileParams`）是 readonly struct 的零分配拷贝

### 5.3 StatModifier 聚合语义

- Multiply 先，Add 后除以 `coreCount`（**现役语义，不可混淆**）
- 初值：`result = baseValue`；Multiply 累加器若无匹配，跳过不乘；Add 累加器若无匹配，跳过不加
- 多个 Multiply 修正**累乘**（`(current == 0f ? 1f : current) * mod.Value`，保证初次 == mod.Value 而非 0）
- 多个 Add 修正**累加**

### 5.4 Modifier 深拷贝

- `ProjectileSpawner.InstantiateModifiers` 使用 `AddComponent + JsonUtility.FromJsonOverwrite`
- **原因**：直接 `GetComponent` 从 prefab 取引用会导致多个 projectile 共享同一 modifier，运行时互相污染
- **已踩过的坑**：历史上出现过"Modifier 组件累积"（见 `Implement_rules.md` 第 8.4 节）

---

## 6. 对象池配额

### 6.1 预热策略（`WeaponTrack.InitializePools`）

| 家族 | ProjectilePrefab 预热 | 额外预热 |
|---|---|---|
| Matter | `GetPool(prefab, 20, 50)` | — |
| Light | `GetPool(prefab, 5, 20)` | — |
| Echo | `GetPool(prefab, 5, 15)` | — |
| Anomaly | `GetPool(prefab, 10, 30)` | `GetPool(AnomalyModifierPrefab, 10, 30)` |
| default/fallback | 按 Matter 处理 | — |

**MuzzleFlash**（所有家族共享）：`GetPool(muzzleFlashPrefab, 5, 20)`

**调用时机**：
- `Start()` → `InitializeAllPools()` → `ActiveSlot.PrimaryTrack.InitializePools()` + `SecondaryTrack.InitializePools()`
- `SwitchLoadout(index)` 后再次调用 `InitializeAllPools()` 预热新 Slot
- `ImportFromSaveData` 结尾再次调用

### 6.2 IPoolable 回收现役合约

所有投射物 `OnReturnToPool` 的最小必须动作（见 `Implement_rules.md` 的"对象池回收清单"）：

| 类型 | `_modifiers.Destroy + Clear` | `ShouldDestroyOnHit = true` | `transform.localScale = one` | 视觉清理 |
|---|---|---|---|---|
| `Projectile` | ✅ | ✅ | ✅ | `_trail.Clear()`（OnGetFromPool）|
| `LaserBeam` | ✅ | — | — | `_lineRenderer.enabled=false` + `positionCount=0` + 宽度重置 |
| `EchoWave` | ✅ | — | — | `_circleCollider.radius=initial` + `_hitEnemies.Clear()` |

**关键防御**：对所有动态 `AddComponent` 的 `IProjectileModifier` 实例，回收时必须 `Destroy(mb)`，防止池复用时组件累积。

### 6.3 Pool 获取的入口收敛

- `WeaponTrack.GetProjectilePool(prefab)` / `GetMuzzleFlashPool(prefab)` 是现役唯一入口
- 底层委托 `PoolManager.Instance.GetPool(prefab, initial, max)`
- 预热失败（`PoolManager` 未注册）时，`InitializeAllPools` 直接 `enabled = false` 并报错，**不静默失效**

---

## 7. UI / 战斗层通信

### 7.1 装备 API（UI 拖拽 → Controller）

以下 API 均在 `StarChartController` 暴露给 UI 层：

- `EquipLightSail(LightSailSO sail, int anchorCol = 0, int anchorRow = 0)` / `UnequipLightSail()` / `UnequipLightSail(LightSailSO)`
- `EquipSatellite(SatelliteSO sat, TrackId trackId)` / `EquipSatellite(SatelliteSO sat, TrackId trackId, int anchorCol, int anchorRow)` / `UnequipSatellite(SatelliteSO, TrackId)`
- `SwitchLoadout(int index)` / `SetSailLayerCols(int cols)`
- `GetEquippedLightSail()` / `GetEquippedSatellites(TrackId)`

Core / Prism 的装备由 UI 层直接操作 Track（`EquipCore` / `EquipPrism`），Controller 不再封一层——因为 Track 自己发 `OnLoadoutChanged` 足够驱动 UI 刷新。

### 7.2 事件订阅者

| 事件 | 发布者 | 消费者 |
|---|---|---|
| `WeaponTrack.OnLoadoutChanged` | Track 自身（装备变化） | UI StarChart 面板 |
| `StarChartController.OnTrackFired` | Controller | UI（HUD 反馈、热量条动画） |
| `StarChartController.OnLightSailChanged` | Controller | UI |
| `StarChartController.OnSatellitesChanged` | Controller | UI |
| `StarChartController.OnWeaponFired` (static) | Controller | 敌人感知 |
| `CombatEvents.OnWeaponFired` | Controller | 跨程序集订阅者 |

### 7.3 编织态（Weaving）与发射链隔离

- 编织态过渡（`WeavingStateTransition`）属于 UI / 表现层
- 编织态开启期间 `StarChartController.Update` 仍在跑（没有 gate），但当前设计通过 `Time.timeScale` / `InputHandler` 自身的输入屏蔽控制开火
- **本 Spec 不记录 Weaving 的具体 gate 机制**，属于 UI 层职责；发射链不主动感知 Weaving

### 7.4 PlayerInventory 的语义

- `StarChartInventorySO._ownedItems` — 玩家持有的全部 SO（21 个 GUID，恰好 = 8 Core + 9 Prism + 3 Sail + 1 Sat）
- UI 过滤器枚举顺序固定：`All / LightSail / Prism / Core / Satellite`（定义在 `InventoryFilter`）
- `DisplayName` 是 Inventory 内部 Dictionary 的 lookup key（见 §8.2）

---

## 8. Save / Load 序列化

### 8.1 当前存档格式（新版，多 Loadout）

`StarChartSaveData`（`ProjectArk.Core.Save.SaveData`）：

```
Loadouts : List<LoadoutSlotSaveData>    // 新格式，3 个 slot
```

`LoadoutSlotSaveData`：

```
PrimaryTrack    : TrackSaveData
SecondaryTrack  : TrackSaveData
LightSailID     : string                 // DisplayName
SailLayerCols   : int                    // SAIL 层解锁列数（默认 2）
SailLayerRows   : int                    // SAIL 层解锁行数（默认 1）
```

`TrackSaveData`：

```
CoreIDs         : List<string>           // DisplayName 列表
PrismIDs        : List<string>
SatelliteIDs    : List<string>           // Per-Track（新版）
CoreLayerCols   : int                    // Core 层解锁列数
CoreLayerRows   : int                    // Core 层解锁行数
PrismLayerCols  : int
PrismLayerRows  : int
SatLayerCols    : int
SatLayerRows    : int
```

**注意**：每个 Layer 都是 **Cols + Rows 双维度**（与 §2.3 的 SlotLayer 4×4 上限对应）。早期存档只有 Cols 字段，Import 时 Rows 会默认为 1，由 `ImportFromSaveData` 通过 `SetLayerCols` / `SetLayerRows` 恢复。

### 8.2 ID 语义：DisplayName 作为跨存档 ID

- Save 写：`cores[i].DisplayName`（见 `ExportTrack`）
- Load 读：`resolver.FindCore(displayName)`（见 `ImportTrack`）
- **约束**：`DisplayName` 必须**全局唯一**。重名会导致 resolver 静默取错。
  → 收敛为 `Implement_rules.md` 第 8 节的硬约束。

### 8.3 Legacy 兼容分支（仅为 migration）

`StarChartSaveData` 上有 4 个字段标了 `[Obsolete]`：

- `PrimaryTrack` / `SecondaryTrack`（旧：单 Loadout）
- `LightSailID`（旧：单 Loadout）
- `SatelliteIDs`（旧：slot 级 Satellite）

`LoadoutSlotSaveData` 上还有一个：

- `SatelliteIDs`（旧：slot 级 Satellite，已改为 Per-Track）

**现役行为**：

- `Loadouts.Count > 0` 时走新格式
- 否则从旧字段迁移到 slot 0（`ImportFromSaveData` 内部用 `#pragma warning disable CS0618` 兜底）
- 旧 `SatelliteIDs`（slot 级）会 `Debug.LogWarning` 后迁移到 Primary Track
- Resolver 解不到的 ID 会 `Debug.LogWarning` 并跳过，**不抛异常**

### 8.4 ExportToSaveData / ImportFromSaveData

- Export：直接遍历 3 个 slot，每个调 `ExportTrack`；Satellite 走 Per-Track 的 `TrackSaveData.SatelliteIDs`
- Import 流程：
  1. Dispose 所有 slot 的 Runners
  2. Clear 所有 slot 数据（`LoadoutSlot.Clear`）
  3. 分新/旧格式取 `slotDataList`
  4. 对每个 slot：恢复列宽 → Equip Cores → Equip Prisms → 处理 Sail → 处理 Satellites（含 legacy 迁移）
  5. 最后 `InitializeAllPools()` 预热激活 slot
- **注意**：Import 只恢复**激活 slot** 的 Pool，非激活 slot 的 pool 在 `SwitchLoadout` 时才会预热

---

## 9. Editor 工具链与 Runtime 职责边界

### 9.1 ShebaAssetCreator（**唯一推荐 owner**）

- 职责：批量创建 Sheba 系列 SO（Core / Prism / Sail / Sat） + 追加到 `PlayerInventory.asset` + 填充默认字段
- 现役定位：**所有新 Sheba 批次资产的唯一入口**
- 与 Runtime 的交互：**0**（纯 Editor 工具，不在 Play Mode 被调用）

### 9.2 Batch5AssetCreator（Legacy）

- 历史批次工具，负责 Batch 5 的 Core / Prism 创建
- 现役定位：**Legacy，只保留不新增**
- 新资产必须走 ShebaAssetCreator（或其替代工具），不得回退到 Batch5AssetCreator

### 9.3 ShapeContractValidator

- 静态校验 `ItemShape` 约束（占位合法性、网格边界、anchor 对齐）
- 通过 Editor 菜单触发，不介入 Runtime
- 新增 Shape 时必须过此 validator（收敛为 Rules）

### 9.4 EchoWaveProceduralPreviewMenu（preview-only）

- 属于 `ProceduralPresentation` 体系（详见 `ProceduralPresentation_WorkflowSpec.md` 第 3 / 7 节）
- **只允许 preview**：
  - 不写回 SO
  - 不在 Play Mode 下持续 override Runtime 的 EchoWave
  - 不替换正式 renderer
- 未来新增 preview 工具必须遵循同样约束

### 9.5 与星图无关的 Editor 工具

`BestiaryImporter.cs` / `EnemyAssetCreator.cs` 属于敌人模块，这里仅列出以划清边界。

---

## 10. 非现役 / 已废弃链路

### 10.1 当前无已废弃主链

经本次调查确认：`StarChartController` + `LoadoutManager` / `ProjectileSpawner` / `StarChartSaveSerializer` 三个协作器组成的现役主链**无 dead code / fallback 旁路**（除 §3.5 的 Light/Echo 无组件 fallback 到 Matter，这是**防御性降级**，不是 legacy）。

### 10.2 兼容分支清单（非废弃，仍在用）

以下字段标了 `[Obsolete]` 但**不是 dead code**，仅用于老存档 migration：

- `StarChartSaveData.PrimaryTrack` / `SecondaryTrack` / `LightSailID` / `SatelliteIDs`
- `LoadoutSlotSaveData.SatelliteIDs`（slot 级，已改为 Per-Track）

以下字段**没有 `[Obsolete]` 标注**，属于"注释级 legacy"（与上面的 `[Obsolete]` 级 legacy 退役时机独立）：

- `StarChartItemSO._slotSize`（legacy 1D 尺寸，注释标 `Legacy 1D slot size. Derived from Shape for backward compatibility.`，现役新代码用 `_shape`）

上述 `[Obsolete]` 字段受 `#pragma warning disable CS0618` 保护读写，保留期限：**直到所有老存档都完成过一次 Import**。`_slotSize` 的退役时机独立于存档字段，由未来新 plan 决定，不在本 Spec 范围。

### 10.3 Anomaly 家族的 Projectile Prefab 共享

- 现役事实：`AnomalyCore_Boomerang` 复用 `Projectile_Matter.prefab` 作为载体
  （GUID `178be1a279c3747f2b519ac5a7db13cc`，已交叉验证）
- 行为差异由 `AnomalyModifierPrefab`（`Modifier_Boomerang.prefab`，GUID `af692c9f108eb45f4ad105251164c12c`）在运行时 `AddComponent` 注入
- **这不是遗留，是有意的现役设计**：Projectile 本体相同（Rigidbody2D + Collider2D + TrailRenderer），Anomaly 的独特行为由 Modifier 提供
- 未来若出现需要**结构级差异**的 Anomaly 核（例如无 Rigidbody 的浮游体），才需要拆出独立 prefab；此 Spec 记录当前事实，不预判未来决策

---

> **本 Spec 覆盖范围就此结束。**
>
> 若读者想问：
> - "怎么加一个新 Core？" → 请看 `Implement_rules.md` 第 8.3.1 节"新武器施工八条清单"
> - "资产在哪、谁创建的？" → 请看 `StarChart_AssetRegistry.md`
> - "为什么以前这里是别的写法？" → 请看 `ImplementationLog.md`
