# 需求文档：Tint 棱镜 Modifier 安全实例化

## 引言

### 背景

在当前的星图（The Loom）发射管线中，**Anomaly 家族**的 `IProjectileModifier`（如 `BoomerangModifier`）在 `StarChartController.SpawnAnomalyEntity()` 中采用了**运行时实例化**策略——每颗子弹通过 `AddComponent` + `JsonUtility.FromJsonOverwrite` 获得独立的 modifier 实例，因此每颗子弹拥有独立状态（如去/回程 HashSet）。

然而，**Tint 家族**棱镜的 `IProjectileModifier`（如 `SlowOnHitModifier`、`BounceModifier`）在 `SnapshotBuilder.CollectTintModifiers()` 中是从 `PrismSO.ProjectileModifierPrefab` 上直接 `GetComponent<IProjectileModifier>()` 获取 prefab 引用，**所有核心和所有子弹共享同一个 prefab 组件实例**。

### 问题

- **有状态的 Tint modifier 会出现竞态/污染**：`BounceModifier` 持有 `_remainingBounces` 状态，共享引用意味着所有子弹的反弹计数会互相干扰。
- **当前未出现明显 Bug** 仅因为 `SlowOnHitModifier` 是无状态的（占位实现），而 `BounceModifier` 虽有状态但未被玩家实际装备测试。
- **这是一个定时炸弹**：一旦 Tint 棱镜真正上线（如接入敌人 debuff 系统），任何有状态的 modifier 都会表现异常。

### 修复策略

将 Tint modifier 的注入方式统一为与 Anomaly 相同的**运行时实例化策略**：在 `StarChartController` 的每个 spawn 方法中，对 Tint modifier 也执行 `AddComponent` + `JsonUtility.FromJsonOverwrite` 运行时复制，确保每颗子弹拥有独立的 modifier 实例。

---

## 需求

### 需求 1：Tint Modifier 运行时实例化

**用户故事：** 作为一名装备了 Tint 棱镜的玩家，我希望每颗子弹都拥有独立的行为修改器实例，以便有状态的 modifier（如反弹计数、减速 debuff 追踪）不会在多颗子弹之间互相污染。

#### 验收标准

1. WHEN 任意家族（Matter/Light/Echo/Anomaly）的子弹被发射 AND 装备了 Tint 棱镜 THEN 系统 SHALL 为每颗子弹创建独立的 `IProjectileModifier` 运行时实例（通过 `AddComponent` + `JsonUtility.FromJsonOverwrite`），而非共享 prefab 引用。
2. WHEN `BounceModifier` 被注入到两颗不同的子弹 THEN 每颗子弹的 `_remainingBounces` 状态 SHALL 独立计数，互不影响。
3. WHEN `SlowOnHitModifier` 被注入到子弹 THEN 其行为 SHALL 与当前占位实现保持一致（无回归）。
4. WHEN Anomaly 家族子弹被发射 AND 同时装备了 Tint 棱镜 THEN Anomaly 自身的 modifier（如 `BoomerangModifier`）AND Tint modifier SHALL 都以独立实例方式注入，不会冲突。

### 需求 2：SnapshotBuilder 传递 Prefab 引用而非组件实例

**用户故事：** 作为系统开发者，我希望 `SnapshotBuilder` 传递的是 modifier prefab 引用列表（`List<GameObject>`），而非组件实例列表（`List<IProjectileModifier>`），以便 `StarChartController` 负责运行时实例化。

#### 验收标准

1. WHEN `SnapshotBuilder.Build()` 收集 Tint 棱镜 modifier THEN 系统 SHALL 将 `PrismSO.ProjectileModifierPrefab`（`GameObject` 引用）存入 `CoreSnapshot`，而非 `GetComponent<IProjectileModifier>()` 的结果。
2. WHEN `CoreSnapshot` 被构建完成 THEN `CoreSnapshot.Modifiers` 字段类型 SHALL 变更为 `List<GameObject>`（Tint modifier prefab 列表），或新增一个 `TintModifierPrefabs` 字段与现有 `AnomalyModifierPrefab` 并列。
3. IF `CoreSnapshot` 结构变更 THEN `LaserBeam.Fire()` 和 `EchoWave.Fire()` 的 modifier 参数签名 SHALL 同步更新。

### 需求 3：Modifier 实例化提取为共享工具方法

**用户故事：** 作为系统开发者，我希望 modifier 运行时实例化逻辑被提取为一个可复用的工具方法，以便 Matter/Light/Echo/Anomaly 四个 spawn 方法都使用同一套实例化逻辑，避免代码重复。

#### 验收标准

1. WHEN 项目中存在 modifier 实例化代码 THEN 系统 SHALL 提供一个共享方法（如 `InstantiateModifiers(GameObject targetObj, List<GameObject> prefabs)` → `List<IProjectileModifier>`），供所有 spawn 方法调用。
2. WHEN `SpawnAnomalyEntity()` 注入 Anomaly modifier THEN 系统 SHALL 同样通过该共享方法实例化，消除重复的 `AddComponent` + `JsonUtility.FromJsonOverwrite` 代码。
3. IF modifier prefab 为 null 或 prefab 上不含 `IProjectileModifier` 组件 THEN 系统 SHALL 跳过该 prefab 且不抛异常。

### 需求 4：对象池回收时清理动态 Modifier 组件

**用户故事：** 作为系统开发者，我希望子弹回池时自动清理动态添加的 modifier 组件，以便对象池复用不会累积残留组件。

#### 验收标准

1. WHEN 子弹（Projectile）被回池（`OnReturnToPool`） THEN 系统 SHALL 销毁所有在本次生命周期内通过 `AddComponent` 动态添加的 `IProjectileModifier` 组件。
2. WHEN LaserBeam 或 EchoWave 被回池 THEN 系统 SHALL 同样清理动态添加的 modifier 组件。
3. IF 子弹上原本就有的 prefab 组件（非动态添加） THEN 系统 SHALL NOT 销毁这些组件。

---

## 技术约束

- **零 GC 不可能**：`AddComponent` 和 `Destroy` 本身会产生少量 GC，但这与 Anomaly 家族一致，在当前规模下可接受。
- **性能边界**：单次齐射最多 20 颗子弹（硬上限），每颗 1-2 个 modifier，总共最多 40 次 `AddComponent`，性能影响可忽略。
- **向后兼容**：需确保没有外部代码直接依赖 `CoreSnapshot.Modifiers` 的 `List<IProjectileModifier>` 类型。

## 风险

| 风险 | 影响 | 缓解措施 |
|------|------|---------|
| `LaserBeam` 和 `EchoWave` 的 modifier 参数签名需要同步修改 | 中等 | 统一改为接收 prefab 列表，在内部做运行时实例化 |
| 对象池回收时遗漏清理导致组件泄漏 | 高 | 在 `OnReturnToPool` 中用标记列表追踪动态组件并逐一销毁 |
| `JsonUtility.FromJsonOverwrite` 对某些字段（如 `LayerMask`）序列化行为不一致 | 低 | `BounceModifier._wallLayer` 是 `LayerMask`，`JsonUtility` 支持其序列化 |
