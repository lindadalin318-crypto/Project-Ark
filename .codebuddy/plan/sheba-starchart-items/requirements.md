# 需求文档：示巴星 13 个星图部件完整实现

## 引言

示巴星（Sheba）是游戏的新手期关卡，主题为「机动性抉择」。本次需要完整实现 13 个星图部件的**代码逻辑层**（IProjectileModifier / LightSailBehavior / SatelliteBehavior 子类）以及**Editor 自动化资产创建脚本**（参考 Batch5AssetCreator 模式），使玩家在示巴星中能够获取并使用这 13 个部件。

### 部件清单（最终确认版）

| 类型 | ID | 名称 | 实现状态 |
|------|----|------|---------|
| 星核 | 1001 | 实相·撕裂者（连发枪） | ✅ 已有 MatterCore_StandardBullet，需新建 Sheba 专属 SO |
| 星核 | 1016 | 光谱·聚焦激光 | ✅ 已有 LightCore_BasicLaser，需新建 Sheba 专属 SO |
| 星核 | 1002 | 实相·风暴散射（散弹枪） | ✅ 已有 Projectile_Matter Prefab，需新建 SO |
| 星核 | 1018 | 波动·脉冲波纹 | ✅ 已有 EchoWave_Echo Prefab，需新建 SO |
| 棱镜 | 2001 | 分形棱镜·双生 | ✅ 已有 FractalPrism_TwinSplit，需确认参数 |
| 棱镜 | 2006 | 流变棱镜·连射 | ✅ 已有 RheologyPrism_Accelerate，需确认参数 |
| 棱镜 | 2013 | 流变棱镜·反弹 | ✅ BounceModifier 已实现，需新建 SO |
| 棱镜 | 2021 | 流变棱镜·回旋 | ✅ BoomerangModifier 已实现，需新建 SO |
| 棱镜 | 2024 | 流变棱镜·制导 | ❌ 需新建 HomingModifier.cs + SO |
| 棱镜 | 2067 | 分形棱镜·布雷 | ❌ 需新建 MinePlacerModifier.cs + SO |
| 光帆 | 3005 | 标准航行帆 | ❌ 需新建 SO（无行为，BehaviorPrefab = null） |
| 光帆 | 3006 | 斥候帆 | ✅ SpeedDamageSail 已实现，需新建 SO |
| 伴星 | 4005 | 自动机炮 | ❌ 需新建 AutoTurretBehavior.cs + SO |

---

## 需求

### 需求 1：新建 4 个示巴星专属星核 SO 资产

**用户故事：** 作为玩家，我希望在示巴星中能够获得 4 种不同手感的星核，以便体验实弹、激光、散弹、波纹四种截然不同的攻击风格。

#### 验收标准

1. WHEN 运行 `ProjectArk > Create Sheba Star Chart Assets` THEN 系统 SHALL 在 `Assets/_Data/StarChart/Cores/` 下创建 4 个 StarCoreSO 资产：`ShebaCore_MachineGun`、`ShebaCore_FocusLaser`、`ShebaCore_Shotgun`、`ShebaCore_PulseWave`
2. IF 星核 ID 为 1002（散弹枪）THEN 系统 SHALL 配置 Spread=30°、子弹数量通过 SpawnModifier 实现（5 颗）、HeatCost=12
3. IF 星核 ID 为 1018（脉冲波纹）THEN 系统 SHALL 使用 EchoWave_Echo Prefab，配置 HeatCost=8、FireRate=0.5s
4. WHEN 星核被装备到轨道 THEN 系统 SHALL 能正常发射对应家族的投射物

### 需求 2：新建 4 个示巴星专属棱镜 SO 资产（已有代码逻辑）

**用户故事：** 作为玩家，我希望能装备双生、连射、反弹、回旋 4 个棱镜，以便体验基础的数值强化和弹道改变。

#### 验收标准

1. WHEN 运行资产创建工具 THEN 系统 SHALL 在 `Assets/_Data/StarChart/Prisms/` 下创建：`ShebaP_TwinSplit`、`ShebaP_RapidFire`、`ShebaP_Bounce`、`ShebaP_Boomerang`
2. IF 棱镜为 2013（反弹）THEN 系统 SHALL 关联 `Modifier_Bounce` Prefab，MaxBounces=3
3. IF 棱镜为 2021（回旋）THEN 系统 SHALL 关联 `Modifier_Boomerang` Prefab
4. IF 棱镜为 2001（双生）THEN 系统 SHALL 配置 SpawnModifier：额外子弹数=2，散射角=15°
5. IF 棱镜为 2006（连射）THEN 系统 SHALL 配置 StatModifier：FireRate × 1.3

### 需求 3：实现制导棱镜（HomingModifier）

**用户故事：** 作为玩家，我希望装备制导棱镜后子弹能在 45° 锥角内自动转向最近敌人，以便降低瞄准门槛并感受「智能子弹」的新鲜感。

#### 验收标准

1. WHEN 制导棱镜被装备 THEN 系统 SHALL 在子弹生命周期内每帧检测 45° 锥角内最近的敌人
2. WHEN 检测到目标 THEN 系统 SHALL 以可配置的转向速度（默认 180°/s）平滑旋转子弹方向
3. IF 锥角内无目标 THEN 系统 SHALL 子弹保持原方向直线飞行
4. WHEN 子弹命中或超时 THEN 系统 SHALL 正常回池，不产生内存泄漏
5. WHEN 运行资产创建工具 THEN 系统 SHALL 创建 `Modifier_Homing` Prefab 和 `ShebaP_Homing` SO

### 需求 4：实现布雷棱镜（MinePlacerModifier）

**用户故事：** 作为玩家，我希望装备布雷棱镜后子弹初速为 0 并在原地停留，以便将「射击」变成「布阵」，配合斥候帆的走位主题。

#### 验收标准

1. WHEN 布雷棱镜被装备 THEN 系统 SHALL 在 `OnProjectileSpawned` 时将子弹速度设为 0，使其停在发射位置
2. WHEN 子弹停留时 THEN 系统 SHALL 子弹存活时间延长为原始 Lifetime 的 3 倍
3. WHEN 敌人进入子弹碰撞体 THEN 系统 SHALL 正常触发伤害并回池
4. WHEN 运行资产创建工具 THEN 系统 SHALL 创建 `Modifier_MinePlacer` Prefab 和 `ShebaP_MinePlacer` SO

### 需求 5：新建 2 个光帆 SO 资产

**用户故事：** 作为玩家，我希望能在标准航行帆（无效果基准）和斥候帆（高速伤害加成）之间做出选择，以便理解光帆系统的「驾驶风格」概念。

#### 验收标准

1. WHEN 运行资产创建工具 THEN 系统 SHALL 在 `Assets/_Data/StarChart/Sails/` 下创建 `ShebaSail_Standard` 和 `ShebaSail_Scout`
2. IF 光帆为 3005（标准航行帆）THEN 系统 SHALL BehaviorPrefab 为 null，无任何被动效果
3. IF 光帆为 3006（斥候帆）THEN 系统 SHALL 关联 `SpeedDamageSail` Behavior Prefab，速度 > 5 时每单位速度 +8% 伤害

### 需求 6：实现自动机炮伴星（AutoTurretBehavior）

**用户故事：** 作为玩家，我希望装备自动机炮伴星后每 1.5 秒自动向最近敌人开火，以便获得稳定的辅助输出而无需额外操作。

#### 验收标准

1. WHEN 自动机炮伴星被装备 THEN 系统 SHALL 每 1.5 秒（InternalCooldown）触发一次
2. WHEN 触发时 THEN 系统 SHALL 检测一定范围（默认 15 单位）内最近的敌人
3. IF 范围内有敌人 THEN 系统 SHALL 从飞船位置向敌人方向发射一颗低伤害子弹（使用 Matter 家族 Prefab）
4. IF 范围内无敌人 THEN 系统 SHALL 跳过本次触发，不浪费冷却
5. WHEN 运行资产创建工具 THEN 系统 SHALL 创建 `Sat_AutoTurret` Behavior Prefab 和 `ShebaSat_AutoTurret` SO

### 需求 7：Editor 一键资产创建工具

**用户故事：** 作为开发者，我希望通过一个菜单命令就能创建所有 13 个示巴星部件的 SO 资产和 Prefab，以便快速验证和迭代。

#### 验收标准

1. WHEN 执行 `ProjectArk > Create Sheba Star Chart Assets` THEN 系统 SHALL 幂等地创建所有缺失的 Prefab 和 SO 资产（已存在的不重复创建）
2. WHEN 资产创建完成 THEN 系统 SHALL 自动将所有新资产添加到 `PlayerInventory.asset` 的 `_ownedItems` 列表中
3. WHEN 创建完成 THEN 系统 SHALL 在 Console 输出创建摘要（新建数量 / 跳过数量）
