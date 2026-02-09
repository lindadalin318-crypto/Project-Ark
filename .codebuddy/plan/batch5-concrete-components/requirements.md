# Batch 5 需求文档：具体部件实现（各家族 Core / Prism）

## 引言

Batch 5 是星图编织系统的最后一个实现批次。前 4 批已完成：热量系统、槽位架构、发射管线、光帆/伴星框架、星图 UI。

当前状态：`StarChartController.SpawnProjectile()` 对所有 `CoreFamily` 统一使用 `Projectile`（Rigidbody2D 直线飞行）。棱镜的 `StatModifier`（数值修正）已在 `SnapshotBuilder` 中生效，但 **Fractal 家族的生成规则修改**和 **Tint 家族的行为注入**尚无具体实现。

本批次目标：
1. **星核家族差异化**：让 4 种 CoreFamily（Matter / Light / Echo / Anomaly）拥有各自独特的发射行为，而不是统一使用同一个 `Projectile` 脚本。
2. **棱镜具体实现**：为 3 种 PrismFamily（Fractal / Rheology / Tint）各创建至少 1 个可玩的具体部件 SO 资产，并实现对应的运行时逻辑。
3. **数据资产创建**：为每个家族创建至少 1 个可在 Unity 编辑器中配置和测试的 SO 实例。

### 现有架构约束

- 所有运行时生成对象**必须**通过 `PoolManager` 对象池管理
- `Projectile.Initialize()` 接收 `ProjectileParams` + `List<IProjectileModifier>`
- `CoreSnapshot.Family` 字段已存在，可在 `SpawnProjectile()` 中根据家族分支
- `PrismSO.ProjectileModifierPrefab` 字段已存在，供 Tint 家族注入 `IProjectileModifier`
- `SnapshotBuilder` 已处理 `StatModifier` 的聚合和分配，无需修改
- 所有脚本位于 `ProjectArk.Combat` 程序集

---

## 需求

### 需求 1：Matter 家族星核 — 物理子弹

**用户故事：** 作为一名玩家，我希望装备 Matter 系星核时发射实体物理子弹，以便获得有撞击感和后坐力反馈的射击体验。

#### 验收标准

1. WHEN 玩家装备 Matter 系星核并按下开火键 THEN 系统 SHALL 从炮口生成 Rigidbody2D 投射物，沿船头朝向以 `ProjectileSpeed` 匀速飞行。
2. WHEN 投射物碰撞环境或敌人 THEN 投射物 SHALL 立即回收至对象池，并在撞击点生成命中特效（如 `ImpactVFXPrefab` 配置了的话）。
3. WHEN 投射物存活时间超过 `Lifetime` THEN 投射物 SHALL 自动回收至对象池。
4. IF 已有 `Projectile.cs` 实现满足上述行为 THEN 系统 SHALL 直接复用现有 `Projectile` 类，无需创建新脚本。

> **注：** Matter 系即当前 `Projectile.cs` 的默认行为，已完成。本需求仅确认其作为 Matter 家族的正式归属，并创建一个明确命名的 SO 资产（如 `MatterCore_StandardBullet.asset`）。

---

### 需求 2：Light 家族星核 — 激光 / 即时命中

**用户故事：** 作为一名玩家，我希望装备 Light 系星核时发射瞬间命中的激光束，以便获得"快速精准"的射击风格。

#### 验收标准

1. WHEN 玩家装备 Light 系星核并按下开火键 THEN 系统 SHALL 执行一次 Raycast2D 从炮口沿船头方向检测命中，最大射程 = `ProjectileSpeed * Lifetime`。
2. WHEN Raycast 命中目标 THEN 系统 SHALL 对目标施加 `Damage` 伤害和 `Knockback` 击退。
3. WHEN 开火发生 THEN 系统 SHALL 使用 LineRenderer 或类似方式渲染一条从炮口到命中点（或最大射程点）的光束，持续约 0.1 秒后淡出。
4. IF 光束命中环境障碍物 THEN 光束 SHALL 在障碍物表面终止（不穿透墙壁）。
5. WHEN 光束渲染完毕 THEN 光束对象 SHALL 通过对象池回收，避免每帧 Instantiate/Destroy。
6. WHEN Light 系星核作为 `CoreSnapshot` 传入 `SpawnProjectile()` THEN 系统 SHALL 根据 `CoreSnapshot.Family == CoreFamily.Light` 进入激光分支，不创建 Rigidbody 投射物。

---

### 需求 3：Echo 家族星核 — 震荡波 / AOE

**用户故事：** 作为一名玩家，我希望装备 Echo 系星核时发射一个持续膨胀的震荡环，以便对周围大范围敌人造成多段伤害。

#### 验收标准

1. WHEN 玩家装备 Echo 系星核并按下开火键 THEN 系统 SHALL 在飞船位置（或炮口）生成一个震荡波实体。
2. WHEN 震荡波生成后 THEN 其碰撞体积 SHALL 从初始半径随时间线性或曲线膨胀，膨胀速度由 `ProjectileSpeed` 决定。
3. WHEN 震荡波碰撞体接触到敌人 THEN 系统 SHALL 对该敌人施加一次 `Damage` 伤害，且同一敌人在同一波次中最多受到一次伤害（去重）。
4. WHEN 震荡波碰撞体接触到墙壁 THEN 震荡波 SHALL **不被阻挡**（穿墙特性）。
5. WHEN 震荡波存活时间超过 `Lifetime` THEN 震荡波 SHALL 回收至对象池。
6. IF `Spread` > 0 THEN 震荡环 SHALL 变为扇形波（角度限制 = Spread 值），而非完整圆环。

---

### 需求 4：Anomaly 家族星核 — 自定义行为实体

**用户故事：** 作为一名玩家，我希望装备 Anomaly 系星核时发射具有独特运动轨迹的实体（如回旋镖、浮游雷），以便获得"布置陷阱/机制操作"的战术体验。

#### 验收标准

1. WHEN 玩家装备 Anomaly 系星核并按下开火键 THEN 系统 SHALL 生成一个 Anomaly 投射物实体。
2. WHEN Anomaly 投射物处于飞行中 THEN 投射物 SHALL 根据其预配置的行为曲线运动（本批次实现一种：**回旋镖** — 飞出后减速、反向、返回发射者位置）。
3. WHEN 回旋镖投射物飞行过程中碰撞敌人 THEN 系统 SHALL 造成 `Damage` 伤害，且投射物**不销毁**（可穿透，去程和回程各命中一次同一敌人）。
4. WHEN 回旋镖投射物返回至飞船附近（距离 < 1 单位）或超过 `Lifetime` THEN 投射物 SHALL 回收至对象池。
5. WHEN Anomaly 投射物使用 `IProjectileModifier` 钩子实现自定义运动 THEN `OnProjectileUpdate` SHALL 覆盖默认直线运动逻辑。

---

### 需求 5：Fractal 棱镜 — 分裂 / 多重 / 连发

**用户故事：** 作为一名玩家，我希望装备 Fractal 系棱镜时能让子弹数量增加或产生扇形散射，以便获得"弹幕式"的压制火力。

#### 验收标准

1. WHEN 玩家装备一个 Fractal 棱镜（如"双子分形"）THEN 系统 SHALL 通过 `StatModifier` 修改 `ProjectileCount`（例如 Add +2）和 `Spread`（例如 Add +15°）。
2. WHEN `ProjectileCount > 1` 且 `Spread > 0` THEN `SpawnProjectile()` SHALL 将多颗子弹在扇形角度范围内均匀分布（而非随机偏移）。
3. WHEN 多颗子弹扇形分布生成 THEN 散布角度计算 SHALL 为 `[-Spread, +Spread]` 范围内等分 `ProjectileCount` 个方向。
4. IF 当前 `SpawnProjectile()` 的散布逻辑是随机偏移 THEN 系统 SHALL 在 `ProjectileCount > 1` 时切换为均匀扇形分布模式。
5. WHEN 棱镜增加了 ProjectileCount THEN 系统 SHALL 保持 `SnapshotBuilder` 的弹幕硬上限（20 颗）生效。

> **注：** Fractal 家族的核心效果通过已有的 `StatModifier` 机制即可实现（修改 ProjectileCount / Spread / FireRate），无需新的 `IProjectileModifier`。但散布的**均匀分布**逻辑需要在 `StarChartController.SpawnProjectile()` 或 `ExecuteFire()` 中调整。

---

### 需求 6：Rheology 棱镜 — 数值与物理修正

**用户故事：** 作为一名玩家，我希望装备 Rheology 系棱镜时能显著改变子弹的物理属性（速度、大小、弹性），以便创造独特的弹道效果。

#### 验收标准

1. WHEN 玩家装备一个 Rheology 棱镜（如"加速流变"）THEN 系统 SHALL 通过 `StatModifier` 修改对应数值（例如 `ProjectileSpeed` Multiply 1.5）。
2. WHEN `ProjectileSize != 1.0` THEN `SpawnProjectile()` SHALL 对生成的投射物应用 `Transform.localScale *= ProjectileSize`，使其视觉和碰撞体积同步缩放。
3. WHEN 投射物被缩放 THEN 其碰撞判定区域 SHALL 与视觉大小一致（无需额外修改 Collider，依赖 Transform.localScale 自动缩放）。
4. IF 一个 Rheology 棱镜的效果是"反弹"（Bounce）THEN 系统 SHALL 提供一个 `IProjectileModifier` 实现（`BounceModifier`），在 `OnProjectileHit` 中检测碰撞法线并反射方向，而非销毁投射物。
5. WHEN 反弹投射物碰撞墙壁 THEN 投射物 SHALL 改变方向继续飞行，最多反弹 N 次（由 SO 配置），超过次数后正常销毁。

---

### 需求 7：Tint 棱镜 — 状态 / 元素效果注入

**用户故事：** 作为一名玩家，我希望装备 Tint 系棱镜时能让子弹附带元素效果（如减速、持续伤害），以便丰富战斗策略。

#### 验收标准

1. WHEN 玩家装备一个 Tint 棱镜（如"霜冻晕染"）THEN 系统 SHALL 通过 `PrismSO.ProjectileModifierPrefab` 注入一个 `IProjectileModifier` 实现。
2. WHEN 带有减速效果的投射物命中敌人 THEN `OnProjectileHit` SHALL 对目标施加一个临时减速 debuff（降低移动速度 X%，持续 Y 秒）。
3. IF 目标实体尚无 `IDamageable` / debuff 系统 THEN Tint 棱镜的 `OnProjectileHit` SHALL 仅打印 Debug.Log 标记命中和预期效果（占位实现），待敌人系统完成后接入。
4. WHEN `SnapshotBuilder.CollectTintModifiers()` 收集到 Tint 棱镜的 modifier THEN 所有核心的投射物 SHALL 共享同一组 modifier（当前逻辑已实现）。
5. WHEN Tint 棱镜的 `ProjectileModifierPrefab` 为 null THEN 系统 SHALL 安全跳过，不注入任何 modifier（当前容错已实现）。

---

### 需求 8：SpawnProjectile 家族分发重构

**用户故事：** 作为一名开发者，我希望 `StarChartController.SpawnProjectile()` 能根据 `CoreFamily` 自动分发到不同的生成逻辑，以便支持各家族独立扩展。

#### 验收标准

1. WHEN `StarChartController.SpawnProjectile()` 被调用 THEN 系统 SHALL 根据 `CoreSnapshot.Family` 值（Matter / Light / Echo / Anomaly）调用对应的私有方法（如 `SpawnMatterProjectile()`, `SpawnLightBeam()`, `SpawnEchoWave()`, `SpawnAnomalyEntity()`）。
2. IF 新的 CoreFamily 枚举值被添加 THEN 系统 SHALL 在 switch-default 中打印 Warning 并 fallback 到 Matter 行为。
3. WHEN 散布逻辑从"随机偏移"升级为"均匀扇形"（需求 5）THEN 该逻辑 SHALL 在 `ExecuteFire()` 层面处理（外层循环决定角度），不在 `SpawnProjectile()` 内部重复计算。
4. WHEN Light 家族不生成 Rigidbody 投射物 THEN 系统 SHALL 不调用 `PoolManager.GetPool()` 获取 Projectile 池，而是使用独立的 LineRenderer 池。

---

### 需求 9：ScriptableObject 数据资产创建

**用户故事：** 作为一名开发者，我希望有一套完整的测试用 SO 数据资产，以便在 Unity 编辑器中快速验证各家族的行为差异。

#### 验收标准

1. WHEN Batch 5 代码完成 THEN 项目 SHALL 包含以下最低限度的 SO 资产（在 `Assets/_Data/StarChart/` 目录下）：
   - Matter 系：1 个标准子弹核心（已有，需确认命名和参数合理性）
   - Light 系：1 个基础激光核心
   - Echo 系：1 个基础震荡波核心
   - Anomaly 系：1 个回旋镖核心
   - Fractal 棱镜：1 个"双子分形"（+2 ProjectileCount, +15° Spread）
   - Rheology 棱镜：1 个"加速流变"（ProjectileSpeed ×1.5）
   - Tint 棱镜：1 个"霜冻晕染"（附带减速 modifier prefab）
2. WHEN 开发者在 StarChartController 的 Debug Loadout 中引用这些 SO THEN 系统 SHALL 能正确运行而不报错。
3. IF 某些 SO 需要 Prefab 引用（如 ProjectilePrefab、ProjectileModifierPrefab）THEN 开发者 SHALL 同时创建对应的 Prefab（或使用临时占位 Prefab）。

---

### 需求 10：ProjectileSize 应用与视觉缩放

**用户故事：** 作为一名玩家，我希望 Rheology 棱镜的"巨大化"效果能在视觉上明显可见，以便直观理解部件组合的效果。

#### 验收标准

1. WHEN `CoreSnapshot.ProjectileSize != 1.0` THEN `SpawnProjectile()` SHALL 在投射物生成后立即设置 `bulletObj.transform.localScale = Vector3.one * ProjectileSize`。
2. WHEN 投射物被缩放 THEN 碰撞体 SHALL 随 Transform 缩放（Unity 默认行为，无需额外代码）。
3. WHEN 投射物回池时 THEN `OnReturnToPool` SHALL 将 `localScale` 重置为 `Vector3.one`，防止污染下次使用。
