# 实施计划：示巴星 13 个星图部件

- [ ] 1. 探查现有代码结构，确认接口与基类
   - 读取 `IProjectileModifier`、`SatelliteBehavior`、`LightSailBehavior` 基类定义
   - 读取已有 `BounceModifier`、`BoomerangModifier`、`SpeedDamageSail` 实现作为参考
   - 确认 `StarCoreSO`、`PrismSO`、`LightSailSO`、`SatelliteSO` 的序列化字段
   - _需求：1.4、2.1、3.1、4.1、5.3、6.3_

- [ ] 2. 实现 `HomingModifier.cs`（制导棱镜逻辑）
   - 继承 `IProjectileModifier`，在 `OnProjectileSpawned` 中启动每帧追踪协程/UniTask
   - 检测 45° 锥角内最近敌人，以 `TurnSpeed`（默认 180°/s）平滑旋转子弹速度方向
   - `OnReturnToPool` 时取消追踪任务，清空状态，防止内存泄漏
   - _需求：3.1、3.2、3.3、3.4_

- [ ] 3. 实现 `MinePlacerModifier.cs`（布雷棱镜逻辑）
   - 继承 `IProjectileModifier`，在 `OnProjectileSpawned` 时将子弹 Rigidbody2D 速度归零
   - 将子弹 `Lifetime` 延长为原始值的 3 倍（运行时副本，不修改 SO）
   - `OnReturnToPool` 时重置所有运行时字段
   - _需求：4.1、4.2、4.3_

- [ ] 4. 实现 `AutoTurretBehavior.cs`（自动机炮伴星逻辑）
   - 继承 `SatelliteBehavior`，使用 `UniTask` 循环每 `InternalCooldown`（1.5s）触发一次
   - 触发时用 `Physics2D.OverlapCircle` 检测 `DetectionRange`（15 单位）内最近敌人
   - 有目标时通过对象池从飞船位置发射 Matter 家族低伤害子弹；无目标时跳过
   - `OnDisable`/`OnReturnToPool` 时取消 UniTask，清空状态
   - _需求：6.1、6.2、6.3、6.4_

- [ ] 5. 编写 `ShebaAssetCreator.cs`（Editor 一键创建工具）
   - 在 `Assets/Editor/` 下新建，添加菜单项 `ProjectArk > Create Sheba Star Chart Assets`
   - 幂等创建逻辑：先 `AssetDatabase.LoadAssetAtPath` 检查，已存在则跳过
   - 创建 4 个星核 SO（1001/1002/1016/1018），配置对应 Prefab 引用、HeatCost、FireRate、Spread 等参数
   - 创建 6 个棱镜 SO（2001/2006/2013/2021/2024/2067），关联对应 Modifier Prefab 及参数
   - 创建 2 个光帆 SO（3005 BehaviorPrefab=null / 3006 关联 SpeedDamageSail）
   - 创建 1 个伴星 SO（4005 关联 AutoTurretBehavior Prefab）
   - 将所有新建 SO 追加到 `PlayerInventory.asset` 的 `_ownedItems` 列表
   - 完成后 Console 输出「新建 X / 跳过 Y」摘要
   - _需求：1.1、2.1、3.5、4.4、5.1、6.5、7.1、7.2、7.3_
