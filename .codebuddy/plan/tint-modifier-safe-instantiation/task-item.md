# 实施计划：Tint 棱镜 Modifier 安全实例化

---

- [ ] 1. 变更 `CoreSnapshot` 数据结构，将 Tint modifier 从组件实例改为 prefab 引用
   - 将 `CoreSnapshot.Modifiers`（`List<IProjectileModifier>`）重命名为 `TintModifierPrefabs`，类型改为 `List<GameObject>`
   - 保留 `AnomalyModifierPrefab`（`GameObject`）字段不变
   - 文件：`FiringSnapshot.cs`
   - _需求：2.2_

- [ ] 2. 更新 `SnapshotBuilder.CollectTintModifiers()` 返回 prefab 引用列表
   - 将方法返回类型从 `List<IProjectileModifier>` 改为 `List<GameObject>`
   - 改为收集 `PrismSO.ProjectileModifierPrefab`（`GameObject`），而非 `GetComponent<IProjectileModifier>()`
   - 更新 `BuildCoreSnapshot()` 中赋值字段为 `TintModifierPrefabs`
   - 文件：`SnapshotBuilder.cs`
   - _需求：2.1_

- [ ] 3. 提取共享的 modifier 运行时实例化工具方法
   - 在 `StarChartController` 中创建 `private static List<IProjectileModifier> InstantiateModifiers(GameObject targetObj, List<GameObject> prefabs)` 方法
   - 逻辑：遍历 prefab 列表，对每个 prefab 上的 `IProjectileModifier` 组件执行 `AddComponent` + `JsonUtility.FromJsonOverwrite`，返回独立实例列表
   - 包含防御性检查：跳过 null prefab 和无 `IProjectileModifier` 组件的 prefab
   - 文件：`StarChartController.cs`
   - _需求：3.1, 3.3_

- [ ] 4. 重构 `SpawnAnomalyEntity()` 使用共享工具方法
   - 将现有 Anomaly modifier 实例化代码替换为调用 `InstantiateModifiers()`
   - 将 `AnomalyModifierPrefab` 包装为单元素列表传入共享方法（或单独处理后合并）
   - 同时对 Tint prefab 列表也调用 `InstantiateModifiers()`，合并两者结果
   - 文件：`StarChartController.cs`
   - _需求：1.4, 3.2_

- [ ] 5. 更新 `SpawnMatterProjectile()` 使用运行时实例化
   - 调用 `InstantiateModifiers(bulletObj, coreSnap.TintModifierPrefabs)` 获取独立实例列表
   - 将实例列表传入 `projectile.Initialize(direction, parms, modifiers)`
   - 文件：`StarChartController.cs`
   - _需求：1.1_

- [ ] 6. 更新 `SpawnLightBeam()` 使用运行时实例化
   - 调用 `InstantiateModifiers(beamObj, coreSnap.TintModifierPrefabs)` 获取独立实例列表
   - 将 `LaserBeam.Fire()` 签名保持不变（仍接收 `List<IProjectileModifier>`），传入运行时实例
   - 文件：`StarChartController.cs`
   - _需求：1.1, 2.3_

- [ ] 7. 更新 `SpawnEchoWave()` 使用运行时实例化
   - 调用 `InstantiateModifiers(waveObj, coreSnap.TintModifierPrefabs)` 获取独立实例列表
   - 将 `EchoWave.Fire()` 签名保持不变（仍接收 `List<IProjectileModifier>`），传入运行时实例
   - 文件：`StarChartController.cs`
   - _需求：1.1, 2.3_

- [ ] 8. 更新 `LaserBeam.OnReturnToPool()` 添加动态 modifier 组件清理
   - 在现有 `_modifiers.Clear()` 之前，遍历 `_modifiers` 列表，对每个 `MonoBehaviour` 类型的 modifier 调用 `Destroy(mb)`
   - 逻辑与 `Projectile.OnReturnToPool()` 中已有的清理模式一致
   - 文件：`LaserBeam.cs`
   - _需求：4.2_

- [ ] 9. 更新 `EchoWave.OnReturnToPool()` 添加动态 modifier 组件清理
   - 在现有 `_modifiers.Clear()` 之前，遍历 `_modifiers` 列表，对每个 `MonoBehaviour` 类型的 modifier 调用 `Destroy(mb)`
   - 逻辑与 `Projectile.OnReturnToPool()` 中已有的清理模式一致
   - 文件：`EchoWave.cs`
   - _需求：4.2_

- [ ] 10. 更新 `ImplementationLog.md` 记录本次修改
   - 记录所有变更文件、修改原因、技术方案
   - 文件：`Docs/ImplementationLog/ImplementationLog.md`
