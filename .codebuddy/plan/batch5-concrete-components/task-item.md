# Batch 5 实施计划：具体部件实现（各家族 Core / Prism）

## 现有代码清单（供任务参考）

| 文件 | 职责 |
|------|------|
| `StarChartController.cs` | 顶层编排器，`ExecuteFire()` + `SpawnProjectile()` |
| `Projectile.cs` | Matter 系物理投射物，Rigidbody2D 直线飞行 |
| `IProjectileModifier.cs` | 行为钩子接口（Spawned / Update / Hit） |
| `FiringSnapshot.cs` | `CoreSnapshot` + `TrackFiringSnapshot` |
| `SnapshotBuilder.cs` | 棱镜修正管线（StatModifier 聚合 + Tint 收集） |
| `ProjectileParams.cs` | 投射物初始化参数 readonly struct |
| `StarCoreSO.cs` / `PrismSO.cs` | SO 数据定义 |
| `WeaponTrack.cs` | 轨道管理、池初始化、快照缓存 |
| `PoolManager.cs` | 全局对象池注册表 |

---

## 任务清单

- [ ] 1. **重构 `StarChartController` 发射管线 — 家族分发 + 均匀扇形散布**
   - 在 `ExecuteFire()` 中将 `ProjectileCount > 1` 时的散布逻辑从 `Random.Range(-Spread, Spread)` 改为 `[-Spread, +Spread]` 均匀等分
   - 将 `SpawnProjectile()` 重构为 `switch(coreSnap.Family)` 分发，调用各家族私有方法：`SpawnMatterProjectile()` / `SpawnLightBeam()` / `SpawnEchoWave()` / `SpawnAnomalyEntity()`
   - default 分支打印 `Debug.LogWarning` 并 fallback 到 Matter
   - 在投射物生成后应用 `ProjectileSize`：`bulletObj.transform.localScale = Vector3.one * coreSnap.ProjectileSize`
   - _需求：5.2, 5.3, 5.4, 8.1, 8.2, 8.3, 10.1_

- [ ] 2. **实现 `LaserBeam` 脚本 — Light 家族即时命中**
   - 新建 `Assets/Scripts/Combat/Projectile/LaserBeam.cs`（MonoBehaviour + IPoolable）
   - 实现 `Fire(Vector2 origin, Vector2 direction, ProjectileParams parms, List<IProjectileModifier> modifiers)`：执行 `Physics2D.Raycast` 检测命中，最大射程 = `Speed * Lifetime`
   - 使用 `LineRenderer` 渲染从 origin 到命中点（或最大射程点）的光束，持续 ~0.1 秒后自动淡出并回池
   - 命中时调用 `IProjectileModifier.OnProjectileHit`（复用 Tint 系统），并打印命中 Debug.Log（占位伤害）
   - 在 `StarChartController.SpawnLightBeam()` 中调用，使用独立的 LineRenderer 对象池（不依赖 Projectile 池）
   - _需求：2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 8.4_

- [ ] 3. **实现 `EchoWave` 脚本 — Echo 家族震荡波 AOE**
   - 新建 `Assets/Scripts/Combat/Projectile/EchoWave.cs`（MonoBehaviour + IPoolable）
   - 实现膨胀碰撞体：初始半径小，随时间按 `ProjectileSpeed` 线性膨胀 `CircleCollider2D.radius`
   - 使用 `HashSet<Collider2D>` 进行同波次敌人去重（每敌人仅命中一次）
   - 碰撞层设置为 Trigger，忽略墙壁层（穿墙特性）
   - 当 `Spread > 0` 时缩减为扇形波（通过角度检测限制触发范围）
   - 超过 `Lifetime` 自动回池，回池时重置碰撞体半径和 HashSet
   - 在 `StarChartController.SpawnEchoWave()` 中调用
   - _需求：3.1, 3.2, 3.3, 3.4, 3.5, 3.6_

- [ ] 4. **实现 `BoomerangModifier` — Anomaly 家族回旋镖行为**
   - 新建 `Assets/Scripts/Combat/Projectile/BoomerangModifier.cs`（MonoBehaviour + IProjectileModifier）
   - `OnProjectileSpawned`：记录发射者位置和初始方向
   - `OnProjectileUpdate`：实现去程减速 → 反转 → 回程加速的运动曲线，覆盖 `Projectile.Direction` 和 `Rigidbody2D.linearVelocity`
   - `OnProjectileHit`：使用 `HashSet` 去重，去程和回程各允许命中同一敌人一次，命中后**不销毁**
   - 在 `Projectile.OnTriggerEnter2D` 中增加"是否可穿透"判断（当存在 BoomerangModifier 时跳过 `ReturnToPool`）
   - 回池条件：返回发射者附近（< 1 单位）或 Lifetime 耗尽
   - _需求：4.1, 4.2, 4.3, 4.4, 4.5_

- [ ] 5. **修改 `Projectile.cs` 支持穿透与缩放重置**
   - 添加 `public bool ShouldDestroyOnHit` 属性（默认 true），供 modifier 覆盖（Anomaly 回旋镖设为 false）
   - 在 `OnTriggerEnter2D` 中判断 `ShouldDestroyOnHit`，为 false 时只执行 modifier 回调 + VFX，不回池
   - 在 `OnReturnToPool()` 中添加 `transform.localScale = Vector3.one` 重置缩放
   - 在 `OnReturnToPool()` 中重置 `ShouldDestroyOnHit = true`
   - _需求：4.3, 10.2, 10.3_

- [ ] 6. **实现 `SlowOnHitModifier` — Tint 棱镜减速效果占位**
   - 新建 `Assets/Scripts/Combat/Projectile/SlowOnHitModifier.cs`（MonoBehaviour + IProjectileModifier）
   - `OnProjectileHit`：检测碰撞体是否实现 `IDamageable` 接口；若无则 `Debug.Log($"[Tint] Slow applied to {other.name}: -{SlowPercent}% speed for {Duration}s")`
   - 通过 `[SerializeField]` 暴露 `SlowPercent` 和 `Duration` 参数
   - `OnProjectileSpawned` 和 `OnProjectileUpdate` 为空实现
   - _需求：7.1, 7.2, 7.3_

- [ ] 7. **实现 `BounceModifier` — Rheology 棱镜反弹效果**
   - 新建 `Assets/Scripts/Combat/Projectile/BounceModifier.cs`（MonoBehaviour + IProjectileModifier）
   - `OnProjectileHit`：检测碰撞是否为墙壁层，若是则计算反射方向（`Vector2.Reflect`），更新 `Projectile.Direction` 和 `Rigidbody2D.linearVelocity`，递减反弹计数
   - 通过 `[SerializeField]` 暴露 `MaxBounces` 参数
   - 反弹次数用尽时允许正常销毁，未用尽时设 `ShouldDestroyOnHit = false`（仅对墙壁碰撞）
   - _需求：6.4, 6.5_

- [ ] 8. **创建 `StarCoreSO` 配置字段扩展 — Anomaly 家族 Prefab 链接**
   - 在 `StarCoreSO` 中添加 `[SerializeField] private GameObject _anomalyModifierPrefab`（仅 Anomaly 家族使用）
   - 添加 public 属性 `AnomalyModifierPrefab`
   - 在 `StarChartController.SpawnAnomalyEntity()` 中从该字段获取 `BoomerangModifier` 组件并注入 modifier 列表
   - _需求：4.5, 8.1_

- [ ] 9. **更新 `WeaponTrack.InitializePools()` 支持多家族池预热**
   - 对 Light 家族核心：预热 `LaserBeam` Prefab 池（LineRenderer 对象池）
   - 对 Echo 家族核心：预热 `EchoWave` Prefab 池
   - 对 Anomaly 家族核心：同时预热 `ProjectilePrefab` 池和 `AnomalyModifierPrefab` 池
   - 池预热逻辑根据 `StarCoreSO.Family` 分支处理
   - _需求：8.4, 9.2_

- [ ] 10. **创建全套测试用 SO 数据资产与 Prefab**
   - 创建 `Assets/_Data/StarChart/Cores/` 目录，包含 4 个 StarCoreSO 资产：
     - `MatterCore_StandardBullet`（Family=Matter，引用现有 Projectile Prefab）
     - `LightCore_BasicLaser`（Family=Light，引用新建 LaserBeam Prefab）
     - `EchoCore_BasicWave`（Family=Echo，引用新建 EchoWave Prefab）
     - `AnomalyCore_Boomerang`（Family=Anomaly，引用 Projectile Prefab + BoomerangModifier Prefab）
   - 创建 `Assets/_Data/StarChart/Prisms/` 目录，包含 3 个 PrismSO 资产：
     - `FractalPrism_TwinSplit`（Family=Fractal，StatModifiers: ProjectileCount Add +2, Spread Add +15）
     - `RheologyPrism_Accelerate`（Family=Rheology，StatModifiers: ProjectileSpeed Multiply 1.5）
     - `TintPrism_FrostSlow`（Family=Tint，ProjectileModifierPrefab 引用 SlowOnHitModifier Prefab）
   - 创建对应的 Prefab：LaserBeam Prefab、EchoWave Prefab、BoomerangModifier Prefab、SlowOnHitModifier Prefab、BounceModifier Prefab
   - _需求：9.1, 9.2, 9.3_
