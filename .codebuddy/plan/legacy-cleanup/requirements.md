# 需求文档：Legacy 代码与资产清理

## 引言

Project Ark 经过 Batch 1~7 的迭代开发后，代码库中积累了一些被标记为 `[Obsolete]` 的 legacy 代码文件、过时的 SO 资产以及空目录。这些残留物增加了维护成本和认知负担。本次清理的目标是：

1. 删除已被 StarChart 系统完全取代的 legacy 代码和资产
2. 整理资产目录结构，将放错位置的资产迁移到正确路径
3. 清除代码中残留的 legacy 兼容桥和 `[Obsolete]` 方法
4. 将仍在使用但放在旧目录的代码文件迁移到正确目录

---

## 排查结果汇总

### 🔴 可以安全删除的文件

| 文件 | 类型 | 理由 |
|---|---|---|
| `Scripts/Combat/Weapon/WeaponSystem.cs` (+.meta) | 代码 | 标记 `[Obsolete]`，已被 `StarChartController` 完全替代，场景中无引用 |
| `Scripts/Combat/Data/WeaponStatsSO.cs` (+.meta) | 代码 | 标记 `[Obsolete]`，已被 `StarCoreSO` 替代 |
| `_Data/Weapons/DefaultWeaponStats.asset` (+.meta) | 资产 | 类型为旧 `WeaponStatsSO`，全项目无任何引用 |
| `_Prefabs/Projectiles/BasicBullet.prefab` (+.meta) | 资产 | 旧子弹 Prefab，仅被 `_Data/Weapons/` 下即将删除/迁移的旧 SO 引用；正式管线使用 `Projectile_Matter.prefab` |
| `_Prefabs/Effects/` 目录 (+.meta) | 空目录 | 无任何内容 |
| `_Data/Enemies/` 目录 (+.meta) | 空目录 | 无任何内容 |

### 🟡 需要迁移（不是删除）的资产

以下文件类型正确（`StarCoreSO` / `LightSailSO`），但路径在旧的 `_Data/Weapons/` 下，应迁移到 `_Data/StarChart/` 下对应子目录：

| 文件 | 实际类型 | 目标路径 | 备注 |
|---|---|---|---|
| `_Data/Weapons/StarCore.asset` | `StarCoreSO` (displayName: "Basic Bullet") | `_Data/StarChart/Cores/` | 被 PlayerInventory 引用，同时需要将其 _projectilePrefab 从 BasicBullet 改指向 Projectile_Matter |
| `_Data/Weapons/TestCore_FastBullet.asset` | `StarCoreSO` (displayName: "Basic Bullet2") | `_Data/StarChart/Cores/` | 被 PlayerInventory 引用，同理需改 _projectilePrefab |
| `_Data/Weapons/TestSpeedSail.asset` | `LightSailSO` (displayName: "Speed Damage Sail") | `_Data/StarChart/Sails/` (需新建) 或放入 `_Data/StarChart/` | 被 PlayerInventory 引用 |

### � 需要迁移的代码文件

以下代码文件仍被广泛使用，但放在旧 `Weapon/` 目录下，应迁移到 `StarChart/` 目录：

| 文件 | 引用方 | 目标路径 |
|---|---|---|
| `Scripts/Combat/Weapon/WeaponTrack.cs` | StarChartController、TrackView、DragDropManager 等 | `Scripts/Combat/StarChart/` |
| `Scripts/Combat/Weapon/FirePoint.cs` | StarChartController | `Scripts/Combat/StarChart/` |

### �🟢 需要从代码中清除的 legacy 兼容桥

| 文件 | 位置 | 内容 |
|---|---|---|
| `Scripts/Combat/Projectile/Projectile.cs` | 第88-95行 | `[Obsolete]` 的 `Initialize(Vector2, WeaponStatsSO, ...)` 重载 + 注释中对 `WeaponStatsSO` 的引用 |
| `Scripts/Combat/StarChart/ProjectileParams.cs` | 第39-55行 | `FromWeaponStats(WeaponStatsSO)` 静态方法及 `#pragma warning disable CS0618` |

### ⚪ 可选清理项

| 文件 | 类型 | 说明 |
|---|---|---|
| `Scripts/Combat/Editor/Batch5AssetCreator.cs` | 编辑器工具 | 一次性批量创建资产的工具，资产已创建完毕，保留无害但也无用 |
| `_Prefabs/Projectiles/` 目录 | 目录 | 删除 BasicBullet 后将变为空目录 |

---

## 需求

### 需求 1：删除 Legacy 代码文件

**用户故事：** 作为一名开发者，我希望移除所有标记为 `[Obsolete]` 且不再被引用的代码文件，以便减少代码库的认知负担和潜在的误用风险。

#### 验收标准

1. WHEN legacy 代码文件 `WeaponSystem.cs` 和 `WeaponStatsSO.cs` 被删除 THEN 项目 SHALL 编译无错误
2. WHEN 删除代码后 THEN 场景 SHALL 正常加载，无 missing script 警告
3. IF `Projectile.cs` 中存在引用 `WeaponStatsSO` 的 `[Obsolete]` 重载方法 THEN 系统 SHALL 同步移除该方法
4. IF `ProjectileParams.cs` 中存在 `FromWeaponStats()` legacy 兼容桥 THEN 系统 SHALL 同步移除该方法及相关 `#pragma warning`

### 需求 2：删除无引用的 Legacy 资产

**用户故事：** 作为一名开发者，我希望删除不再被任何场景/SO/代码引用的旧资产文件，以便保持项目资产的整洁。

#### 验收标准

1. WHEN `DefaultWeaponStats.asset` 被删除 THEN 项目 SHALL 无 missing reference 警告
2. WHEN `BasicBullet.prefab` 被删除 THEN 项目 SHALL 无 missing reference 警告（前提是先完成需求 3 的 prefab 引用修正）
3. WHEN 空目录 `_Prefabs/Effects/`、`_Data/Enemies/`、`_Prefabs/Projectiles/` 被删除 THEN Unity 项目结构 SHALL 正常

### 需求 3：迁移放错位置的资产

**用户故事：** 作为一名开发者，我希望将正确类型但放在旧目录的 SO 资产迁移到规范路径，以便资产目录结构与当前架构一致。

#### 验收标准

1. WHEN `StarCore.asset` 迁移到 `_Data/StarChart/Cores/` THEN PlayerInventory 中的引用 SHALL 自动更新（Unity 的 GUID 机制保证）
2. WHEN `TestCore_FastBullet.asset` 迁移到 `_Data/StarChart/Cores/` THEN PlayerInventory 中的引用 SHALL 自动更新
3. WHEN `TestSpeedSail.asset` 迁移到 `_Data/StarChart/` 合适位置 THEN PlayerInventory 中的引用 SHALL 自动更新
4. WHEN 上述迁移的 StarCoreSO 资产仍引用旧的 `BasicBullet.prefab` THEN 系统 SHALL 将其 `_projectilePrefab` 字段改为指向 `Projectile_Matter.prefab`
5. WHEN 迁移完成后 `_Data/Weapons/` 目录变为空 THEN 该空目录 SHALL 被删除

### 需求 4：迁移仍在使用的代码文件到正确目录

**用户故事：** 作为一名开发者，我希望将仍在使用但放在旧 `Weapon/` 目录下的代码文件迁移到 `StarChart/` 目录，以便目录结构反映当前的系统架构。

#### 验收标准

1. `WeaponTrack.cs` SHALL 从 `Scripts/Combat/Weapon/` 移至 `Scripts/Combat/StarChart/`
2. `FirePoint.cs` SHALL 从 `Scripts/Combat/Weapon/` 移至 `Scripts/Combat/StarChart/`
3. WHEN 文件迁移后 THEN 所有 `using` 语句和命名空间 SHALL 保持不变（因为都在 `ProjectArk.Combat` 命名空间下）
4. WHEN `Weapon/` 目录变为空 THEN 该目录及其 `.meta` 文件 SHALL 被删除

### 需求 5（可选）：删除一次性编辑器工具

**用户故事：** 作为一名开发者，我希望清理已完成使命的一次性编辑器脚本，以便编辑器菜单保持简洁。

#### 验收标准

1. IF 用户选择执行此项 THEN `Batch5AssetCreator.cs` (+.meta) SHALL 被删除
2. WHEN 删除后 THEN Unity 编辑器菜单 `ProjectArk/Create Batch 5 Test Assets` SHALL 不再显示

---

## 风险与注意事项

1. **迁移 vs 删除**：`_Data/Weapons/` 下的 `StarCore.asset`、`TestCore_FastBullet.asset`、`TestSpeedSail.asset` 虽在旧目录但被 `PlayerInventory` 活跃引用，**绝不能删除**，只能迁移
2. **WeaponTrack.cs / FirePoint.cs 不能删除**：它们被 `StarChartController`、`TrackView`、`DragDropManager` 等多处活跃引用，只做目录迁移
3. **GUID 稳定性**：在 Unity 内通过 AssetDatabase 移动文件会保留 GUID，外部移动则会丢失引用
4. **编译顺序**：删除代码时需确保先删除所有 consumer 中的引用，再删除被依赖的类
5. **代码文件迁移**：`WeaponTrack.cs` 和 `FirePoint.cs` 都在 `ProjectArk.Combat` 命名空间下，迁移目录不影响编译，但需连同 `.meta` 文件一起移动以保留 GUID
