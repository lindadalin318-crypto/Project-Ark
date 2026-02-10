# 实施计划：Legacy 代码与资产清理

> 基于 [requirements.md](./requirements.md)

---

- [ ] 1. 清除代码中的 legacy 兼容桥
   - 移除 `Projectile.cs` 中引用 `WeaponStatsSO` 的 `[Obsolete]` 重载方法 `Initialize(Vector2, WeaponStatsSO, ...)`
   - 移除 `ProjectileParams.cs` 中的 `FromWeaponStats(WeaponStatsSO)` 静态方法及 `#pragma warning disable/restore CS0618`
   - 确认移除后无其他代码引用这两个方法
   - _需求：1.3、1.4_

- [ ] 2. 删除 legacy 代码文件
   - 删除 `Assets/Scripts/Combat/Weapon/WeaponSystem.cs` 及其 `.meta` 文件
   - 删除 `Assets/Scripts/Combat/Data/WeaponStatsSO.cs` 及其 `.meta` 文件
   - 验证项目无编译错误（步骤 1 已移除所有对这两个类的引用）
   - _需求：1.1、1.2_

- [ ] 3. 修正迁移资产中的 Prefab 引用
   - 读取 `_Data/Weapons/StarCore.asset`，将 `_projectilePrefab` 字段的 GUID 从 `BasicBullet.prefab` 改为 `Projectile_Matter.prefab` 的 GUID
   - 读取 `_Data/Weapons/TestCore_FastBullet.asset`，同样修正 `_projectilePrefab` 字段
   - _需求：3.4_

- [ ] 4. 迁移 SO 资产到正确目录
   - 将 `Assets/_Data/Weapons/StarCore.asset` (+.meta) 移至 `Assets/_Data/StarChart/Cores/`
   - 将 `Assets/_Data/Weapons/TestCore_FastBullet.asset` (+.meta) 移至 `Assets/_Data/StarChart/Cores/`
   - 在 `Assets/_Data/StarChart/` 下新建 `Sails/` 子目录
   - 将 `Assets/_Data/Weapons/TestSpeedSail.asset` (+.meta) 移至 `Assets/_Data/StarChart/Sails/`
   - _需求：3.1、3.2、3.3_

- [ ] 5. 删除无引用的 legacy 资产
   - 删除 `Assets/_Data/Weapons/DefaultWeaponStats.asset` (+.meta)
   - 删除 `Assets/_Prefabs/Projectiles/BasicBullet.prefab` (+.meta)
   - _需求：2.1、2.2_

- [ ] 6. 迁移仍在使用的代码文件
   - 将 `Assets/Scripts/Combat/Weapon/WeaponTrack.cs` (+.meta) 移至 `Assets/Scripts/Combat/StarChart/`
   - 将 `Assets/Scripts/Combat/Weapon/FirePoint.cs` (+.meta) 移至 `Assets/Scripts/Combat/StarChart/`
   - 确认命名空间 `ProjectArk.Combat` 无需变更
   - _需求：4.1、4.2、4.3_

- [ ] 7. 清理空目录
   - 删除 `Assets/Scripts/Combat/Weapon/` 目录 (+.meta)（步骤 2、6 完成后应为空）
   - 删除 `Assets/Scripts/Combat/Data/` 目录 (+.meta)（步骤 2 完成后应为空，需先确认无其他文件）
   - 删除 `Assets/_Data/Weapons/` 目录 (+.meta)（步骤 4、5 完成后应为空）
   - 删除 `Assets/_Prefabs/Projectiles/` 目录 (+.meta)（步骤 5 完成后应为空）
   - 删除 `Assets/_Prefabs/Effects/` 目录 (+.meta)（已为空）
   - 删除 `Assets/_Data/Enemies/` 目录 (+.meta)（已为空）
   - _需求：2.3、3.5、4.4_

- [ ] 8. 更新实现日志
   - 在 `Docs/ImplementationLog/ImplementationLog.md` 追加本次清理记录
   - 记录所有删除、迁移的文件路径
   - _需求：全部_

---

## 执行顺序说明

```
步骤 1（清兼容桥）→ 步骤 2（删代码）→ 步骤 3（改 Prefab 引用）→ 步骤 4（迁移 SO）→ 步骤 5（删旧资产）→ 步骤 6（迁移代码）→ 步骤 7（清空目录）→ 步骤 8（记日志）
```

- **步骤 1 必须在步骤 2 之前**：先移除对 `WeaponStatsSO` 的代码引用，再删除该类文件，避免编译错误
- **步骤 3 必须在步骤 5 之前**：先修正 StarCoreSO 的 prefab 引用指向 `Projectile_Matter`，再删除旧的 `BasicBullet.prefab`，避免 missing reference
- **步骤 4 必须在步骤 7 之前**：先将资产移出 `_Data/Weapons/`，再删除该空目录
- **步骤 6 必须在步骤 7 之前**：先将代码文件移出 `Weapon/`，再删除该空目录

## 注意事项

- 所有文件移动操作需连同 `.meta` 文件一起移动，以保留 Unity GUID 引用
- 推荐在 Unity Editor 中使用 `AssetDatabase.MoveAsset()` 或直接在 Project 窗口拖动，而非在文件系统层面移动
- 步骤 3 修改 `.asset` 文件时需查找 `Projectile_Matter.prefab` 的 GUID 进行替换
