# 星图治理立规计划（剪裁版 · 两份半）

> **状态**：Plan 草稿，等待 owner 审阅后进入执行。
>
> **目标**：在批量生产新武器之前，把星图现役链路、资产映射、施工规则三件事固化到文档，让下一轮批量生产按规施工，而不是重新考古。
>
> **原则**：先立规、再生产。立规阶段不改代码、不动资产（除非是只读校验），避免"立规动作"和"清理动作"混在一起。

---

## 一、立规总目标

- 未来每做一个新 Core / Prism / Sail / Satellite，能照规施工，不再考古
- 批量生产中发现的问题能反向沉淀到规则，而不是散落在 `ImplementationLog.md` 里
- Dangling reference、DisplayName 冲突、Modifier 注入路径混淆这类高频坑，被 Registry 和 Rules 联合兜住

**非目标**：

- 不是给星图来一次大重构
- 不是要把所有现有 SO 资产字段都文档化（那是 GDD 的事）
- 不是要学 `Ship/VFX` 搞 authority matrix 重表（星图目前乱度不到那个程度）

---

## 二、三份产出物的职责边界

| 产物 | 路径 | 职责 | 篇幅预估 |
|---|---|---|---|
| **CanonicalSpec** | `Docs/2_TechnicalDesign/Combat/StarChart_CanonicalSpec.md` | 现役主链 / owner / 数据流 / 依赖反转边界 | ~300 行 |
| **AssetRegistry** | `Docs/2_TechnicalDesign/Combat/StarChart_AssetRegistry.md` | 所有 SO / Prefab / Inventory 的路径与 owner 映射表 | ~200 行 |
| **Implement_rules 新增章节** | `Implement_rules.md` 追加 `## 8. StarChart 模块规则` | 施工规则 / 踩坑总结 / 验收清单 | ~200 行（新增） |

> 目录 `Docs/2_TechnicalDesign/Combat/` 当前不存在，执行 Step 1 时一并创建。

---

## 三、CanonicalSpec 大纲

> 目的：回答"星图今天是怎么运作的，谁负责哪一段？"

```
1. 模块边界
   - Runtime 代码范围（Assets/Scripts/Combat/StarChart/ + Projectile/ + UI/StarChart/）
   - Editor 工具范围（Assets/Scripts/Combat/Editor/）
   - 数据资产范围（Assets/_Data/StarChart/）
   - 关联 GDD 文档（Docs/1_GameDesign/ 对应章节，如存在）
   - 与 ProceduralPresentation_WorkflowSpec.md 的关系（preview 工具边界）

2. 核心概念与语义
   - 四家族 × 四层装备矩阵（Matter / Light / Echo / Anomaly × Core / Prism / Sail / Satellite）
   - 槽位与轨道的对应关系（LoadoutSlot 层 → WeaponTrack，3 个独立 Loadout Slot）
   - SlotLayer<T> 形状系统（网格占位 + 装备校验 + 列解锁）
   - Multi-Loadout：3 个独立槽位，每个槽位独立的 Primary/Secondary Track + Sail + Satellites

3. 现役发射主链（Owner 标注）
   - Input 触发 → StarChartController.Update() 检查 IsFireHeld / IsSecondaryFireHeld
   - 冷却检查 → StarChartController.CanFireTrack() + HeatSystem.CanFire()
   - Loadout 读取 → WeaponTrack.TryFire() → 内部调用 SnapshotBuilder.Build()
   - Snapshot 构建 → SnapshotBuilder.Build() ⚠️ 这是唯一权威路径，禁止绕过
   - 投射物分发 → StarChartController.SpawnProjectile() switch by CoreFamily
     * Matter  → SpawnMatterProjectile()
     * Light   → SpawnLightBeam()
     * Echo    → SpawnEchoWave()
     * Anomaly → SpawnAnomalyEntity()（特殊：额外处理 AnomalyModifierPrefab）
   - Modifier 注入（两条路径必须明确）：
     * Tint Prism → CoreSnapshot.TintModifierPrefabs（所有家族统一处理）
     * Anomaly Core → CoreSnapshot.AnomalyModifierPrefab（仅 SpawnAnomalyEntity 分支处理）
   - 反馈收尾 → Recoil / Heat / MuzzleFlash / FireSound / OnTrackFired / OnWeaponFired
   - 全局事件 → CombatEvents.RaiseWeaponFired（敌人听觉感知订阅）

4. 依赖反转与跨程序集边界
   - IStarChartItemResolver（Combat 层接口，UI/Save 层实现）
   - StarChartContext（发射时的上下文载体，Runtime-only）
   - 禁止事项：低层程序集（Core）反向 using 高层（Combat/UI）

5. 运行时数据隔离
   - StarChartItemSO 是 authored data，禁止运行时写
   - CoreSnapshot 是 SnapshotBuilder 产生的 runtime 只读快照
   - StatModifier 聚合：Additive/Multiplicative 链在 SnapshotBuilder 中构建
   - Modifier 深拷贝：InstantiateModifiers() 用 AddComponent + JsonUtility.FromJsonOverwrite

6. 对象池配额（从 Projectile_Matter.prefab 等定义读取）
   - 四家族 Projectile Prefab 列表（见 AssetRegistry）
   - Muzzle Flash / VFX 池
   - WeaponTrack.InitializePools() 与 SwitchLoadout 的预热策略
   - 防御性复位清单引用 → Implement_rules 对应章节

7. UI 与战斗层的通信
   - DragDrop → StarChartController 的 EquipXxx / UnequipXxx API
   - CombatEvents.OnWeaponFired 的订阅者清单
   - 编织态切换（WeavingStateTransition）与发射链的隔离
   - StarChartInventorySO（PlayerInventory.asset）的 _ownedItems 列表作用

8. Save / Load 序列化
   - ExportToSaveData / ImportFromSaveData 路径
   - DisplayName 作为跨存档 ID 的约定（→ 命名唯一性硬约束）
   - Legacy 迁移：单 Loadout → 多 Loadout / 槽级 Satellites → 轨级 Satellites

9. Editor 工具链与 Runtime 的职责边界
   - ShebaAssetCreator：SO 批量创建 + Inventory 追加（唯一推荐 owner）
   - Batch5AssetCreator：历史批次工具（Legacy，不新增用法）
   - ShapeContractValidator：Shape 静态校验
   - EchoWaveProceduralPreviewMenu：preview 工具（属于 ProceduralPresentation 体系，禁止接管 Runtime）
   - BestiaryImporter / EnemyAssetCreator 与星图无关（这里只是说明边界）

10. 非现役 / 已废弃链路（Known Legacy）
    - 初步结论：当前无已废弃主链
    - 存在的"兼容分支"：StarChartSaveData 中的 PrimaryTrack/SecondaryTrack/LightSailID/SatelliteIDs
      旧字段（已用 #pragma warning disable CS0618 标记 Obsolete），仅用于老存档 migration
```

---

## 四、AssetRegistry 大纲

> 目的：回答"星图有哪些资产，它们在哪，谁负责创建？"

### 资产现状（本次调查已确认）

- **Cores**：8 个 SO
  - 旧版/基础：`MatterCore_StandardBullet` / `LightCore_BasicLaser` / `EchoCore_BasicWave` / `AnomalyCore_Boomerang`
  - Sheba 批次：`ShebaCore_MachineGun` / `ShebaCore_Shotgun` / `ShebaCore_FocusLaser` / `ShebaCore_PulseWave`
- **Prisms**：9 个 SO
  - 旧版：`FractalPrism_TwinSplit` / `RheologyPrism_Accelerate` / `TintPrism_FrostSlow`
  - Sheba 批次：`ShebaP_TwinSplit` / `ShebaP_RapidFire` / `ShebaP_Homing` / `ShebaP_Bounce` / `ShebaP_Boomerang` / `ShebaP_MinePlacer`
- **Sails**：3 个 SO（**实装**，不是空）
  - `ShebaSail_Standard` / `ShebaSail_Scout` / `TestSpeedSail`
- **Satellites**：1 个 SO（**实装**，不是空）
  - `ShebaSat_AutoTurret`
- **Projectile / Modifier Prefabs**：8 个
  - 基础投射物：`Projectile_Matter` / `LaserBeam_Light` / `EchoWave_Echo`（3 个）
  - Modifier：`Modifier_Boomerang` / `Modifier_Bounce` / `Modifier_Homing` / `Modifier_MinePlacer` / `Modifier_SlowOnHit`（5 个）
  - **Anomaly 家族当前版本复用 `Projectile_Matter` 当载体**（已通过 GUID 确认：`AnomalyCore_Boomerang._projectilePrefab` → `178be1a279c3747f2b519ac5a7db13cc` = `Projectile_Matter.prefab`），行为变化由 `_anomalyModifierPrefab` → `Modifier_Boomerang.prefab`（GUID `af692c9f108eb45f4ad105251164c12c`）在运行时动态挂载实现。这是**事实**，不是 TODO。
- **Inventory**：`PlayerInventory.asset` 的 `_ownedItems` 共 **21 个 GUID**，恰好等于 `8+9+3+1=21`，**无 dangling reference**（之前疑云的 21 vs 17 结论修正）

### 文档结构

```
1. Inventory 清单
   - PlayerInventory.asset 路径与 StarChartInventorySO 类定义
   - _ownedItems 的 21 个 GUID → SO 名称映射表（从 .asset 反查每条）
   - Dangling 审计：本次立规阶段只记录结论（无 dangling），不做清理

2. Cores 资产表（8 行）
   | DisplayName | Family | SO 路径 | Projectile Prefab | Modifier 路径 | Owner 工具 | 状态 |
   | Matter Core Standard Bullet | Matter | _Data/StarChart/Cores/MatterCore_StandardBullet.asset | Projectile_Matter.prefab | （无） | ShebaAssetCreator? / 历史手工? | Live |
   | ...（其余 7 个）
   注：Owner 工具栏必须逐个从代码扫准（交叉 Batch5AssetCreator.cs / ShebaAssetCreator.cs / 其他 Editor 脚本）。
   真正扫不出来源的（工具已删 / 资产来自更早历史）才标 `Unknown-Historical`，并在本节末尾单列例外名单。

3. Prisms 资产表（9 行）
   | DisplayName | PrismType | SO 路径 | ModifierPrefab（如有）| Owner 工具 | 状态 |
   - Tint 类必须有 ModifierPrefab
   - Fractal / Rheology 等行为修饰类通常无 ModifierPrefab，走 SnapshotBuilder 的 Stat 聚合

4. Sails 资产表（3 行）
   | DisplayName | SO 路径 | LightSailBehavior 子类 | Owner 工具 | 状态 |
   - 确认每个 Sail 是数据驱动还是行为驱动

5. Satellites 资产表（1 行）
   | DisplayName | SO 路径 | SatelliteBehavior 子类 | Owner 工具 | 状态 |

6. Projectile Prefab 表（3 行）
   | Prefab 名 | 挂载组件 | 使用 Core 清单 | Pool 预热配额 |
   - Projectile_Matter → MatterCore_StandardBullet + ShebaCore_MachineGun + ShebaCore_Shotgun + AnomalyCore_Boomerang（共 4 个 Core 共享）
   - LaserBeam_Light → LightCore_BasicLaser + ShebaCore_FocusLaser
   - EchoWave_Echo → EchoCore_BasicWave + ShebaCore_PulseWave
   注：Anomaly 家族"共享 Matter prefab"是经 GUID 交叉验证的现役实现，不是待查项

7. Modifier Prefab 表（5 行）
   | Prefab 名 | IProjectileModifier 实现类 | 被谁引用（Core/Prism）| 注入路径 |
   - Modifier_Boomerang → BoomerangModifier（Anomaly Core 路径，不是 Tint 路径）
   - Modifier_SlowOnHit → （冻结 Tint Prism 用）
   - 其余 Bounce/Homing/MinePlacer → ShebaP_ 前缀的 Prism 用

8. Owner 工具映射
   - ShebaAssetCreator：负责 Sheba 批次 SO 创建 + Inventory 追加 + 默认字段模板
   - Batch5AssetCreator：Legacy，只保留不新增
   - 手工遗留：列出所有"非工具生成"的资产（如有）
   执行时必须扫 Editor 工具源码交叉确认每个 SO 的生成来源，扫不准的单独落进 `Unknown-Historical` 例外名单。

9. 命名唯一性约束（硬约束）
   - DisplayName 必须全局唯一（Save/Load 与 Inventory 都走 name lookup）
   - SO 文件名约定：PascalCase + 家族/类型前缀（如 ShebaCore_ / ShebaP_ / ShebaSail_ / ShebaSat_）
   - Inventory 内部 Dictionary 的 name → SO 映射

10. 新增资产的施工入口
    - 只允许通过 ShebaAssetCreator 菜单路径进入
    - 手工创建 .asset 文件 → 禁止（例外：立规之前历史遗留）
    - 例外名单维护在本章节末尾
```

> **Registry 写作时的强约束**：
>
> 1. 所有"Owner 工具"栏必须从代码扫准后填写，禁止推测。扫不准就继续扫，不允许靠 `⚠️ TODO` 糊弄过去。真正扫不出来的（例如资产早于工具诞生、来源彻底丢失）才允许标 `Unknown-Historical` 并在末尾例外名单单列。
> 2. 所有"状态"栏必须是以下之一：`Live` / `Legacy` / `Deprecated`
> 3. 所有路径必须用相对 `Assets/` 的路径
> 4. 本次不清理 dangling（已确认无 dangling），也不改任何资产字段

---

## 五、Implement_rules.md 新增章节大纲

> 追加在第 7 节"后续可追加模块（占位）"之后，作为新的第 8 节。不包含 authority matrix 表（星图现阶段不需要）。

```
## 8. StarChart 模块规则

### 8.1 模块边界
- Runtime：Assets/Scripts/Combat/StarChart/ + Projectile/ + UI/StarChart/
- Editor：Assets/Scripts/Combat/Editor/
- Data：Assets/_Data/StarChart/
- 关联文档三件套：
  - GDD（Docs/1_GameDesign/ 对应章节）
  - StarChart_CanonicalSpec.md（现役链路真相）
  - StarChart_AssetRegistry.md（资产 owner 映射）
- 关联横向规范：ProceduralPresentation_WorkflowSpec.md（preview 工具边界）

### 8.2 模块目标
- 让批量生产新武器的施工成本可预测（查 Spec → 查 Registry → 按 Rules 施工）
- 让对象池防御性复位成为肌肉记忆而非考古结果
- 让 Modifier 注入路径（Tint vs Anomaly）在编码时一目了然

### 8.3 实现规则

#### 8.3.1 新武器施工八条清单（每次新 Core/Prism/Sail/Sat 必须全过）
1) SO 资产创建必须走 ShebaAssetCreator（或后续替代工具）
2) DisplayName 全局唯一（Save/Load + Inventory 双重约束）
3) Tint Prism 必须配套 Modifier Prefab（挂 IProjectileModifier 组件）
4) Anomaly Core 的 Modifier 走 AnomalyModifierPrefab 字段，
   不走 PrismSO.ProjectileModifierPrefab，也不和 Tint 路径混用
5) 新 Projectile Prefab 必须挂 IPoolable + 注册到 PoolManager 预热
6) OnReturnToPool 必须按"对象池回收清单"五项全检
   （运行时字段 / event / 动态 Component / Transform / 视觉状态）
7) 新 SO 必须追加到 PlayerInventory.asset（ShebaAssetCreator 自动完成）
8) 新 Shape 必须过 ShapeContractValidator（Editor 菜单触发）

#### 8.3.2 禁止事项
- 禁止在 Runtime 中修改 StarChartItemSO 字段（违反运行时数据隔离）
- 禁止在 SpawnProjectile 的 switch 之外的地方判断 CoreFamily
- 禁止绕过 SnapshotBuilder 直接构造 CoreSnapshot / FiringSnapshot
- 禁止手工创建 SO 资产（除非写进 AssetRegistry 的例外名单）
- 禁止在 Modifier Prefab 上挂多个 IProjectileModifier 却不评估相互影响

#### 8.3.3 Debug / Preview 工具约束
- EchoWaveProceduralPreviewMenu 属于 ProceduralPresentation 体系，
  只允许 preview，不得在 Runtime 接管 EchoWave 生成链
- 未来新增 preview 工具必须显式与正式链路隔离：
  - 只读 SO，不写回
  - 不在 Play Mode 下持续 override
  - 不替换正式 renderer
- 参考：ProceduralPresentation_WorkflowSpec.md 第 3 / 7 节

### 8.4 踩坑总结
1) 对象池状态泄漏三连坑：颜色（LaserBeam） / Modifier 组件累积 / Projectile 缩放残留
2) Tint vs Anomaly 的 Modifier 注入路径混淆
   - Tint → CoreSnapshot.TintModifierPrefabs（所有家族统一走 InstantiateModifiers）
   - Anomaly → CoreSnapshot.AnomalyModifierPrefab（仅 SpawnAnomalyEntity 额外注入）
3) StatModifier Additive/Multiplicative 聚合链的初始化边缘情况
4) DisplayName 冲突导致 Inventory / Save lookup 静默取错
5) PlayerInventory dangling reference（历史疑云；本次立规阶段已确认 21 全在库，记为常识）
6) 新 Core 忘记在 SpawnProjectile 的 switch 加分支 → 默认 fallback 到 Matter，静默错误
7) Anomaly 家族当前复用 Matter 的 Projectile prefab，新增 Anomaly 核必须确认 Projectile 复用是否仍合理，
   若需要专属 prefab 必须同步在 AssetRegistry Prefab 表更新引用清单

### 8.5 验收清单
新武器/新部件上线前必过 8 条（对应 8.3.1 八条施工清单）：
1) SO 创建走了 ShebaAssetCreator？
2) DisplayName 未冲突？
3) Shape 过了 ShapeContractValidator？
4) Projectile Prefab 有 IPoolable 且完整五项复位？
5) Modifier 注入路径正确（Tint → Snapshot.TintModifierPrefabs / Anomaly → Snapshot.AnomalyModifierPrefab）？
6) Inventory 已追加？
7) Play Mode 实测：装备、发射、回收、再装备 四步无残留？
8) 若是新 CoreFamily，SpawnProjectile 的 switch 已补分支（否则写一条 ImplementationLog）？

### 8.6 推荐工作流
- 新武器：查 Spec → 查 Registry → 按 Rules 施工 → 跑验收清单 → 更新 Registry
- 难定位 bug：先查 Registry 确认资产完整 → 再查 Spec 确认是否走对链路 → 最后才 trace Runtime
```

---

## 六、落地执行顺序

```
Step 1：创建 Combat 目录 + CanonicalSpec（~35 分钟）
    → mkdir Docs/2_TechnicalDesign/Combat/
    → 从 StarChartController.cs 提炼现役主链（本 plan 调查阶段已完成代码摸底）
    → 重点确认：SpawnProjectile 的 4 个 switch 分支、Modifier 注入两条路径、Save/Load 兼容分支
    → 产物：Docs/2_TechnicalDesign/Combat/StarChart_CanonicalSpec.md

Step 2：AssetRegistry（~30 分钟）
    → 逐个打开 Cores/Prisms/Sails/Satellites 的 .asset 文件，读 DisplayName 与 ModifierPrefab 字段
    → 交叉确认 Inventory 的 21 个 GUID 对应的 DisplayName
    → 交叉扫 ShebaAssetCreator / Batch5AssetCreator 源码，确认每个资产的 Owner 工具
    → 扫不出的归入 `Unknown-Historical` 例外名单，禁止用 `⚠️ TODO` 搁置
    → 产物：Docs/2_TechnicalDesign/Combat/StarChart_AssetRegistry.md

Step 3：Implement_rules 新增第 8 节（~20 分钟）
    → 正式化八条施工清单 + 踩坑总结 + 验收清单
    → 不加 authority matrix 表
    → 产物：Implement_rules.md 末尾追加

Step 4：更新 CLAUDE.md 常见任务 B（~5 分钟）
    → "创建新的星图部件"段落补充三份文档链接
    → 产物：CLAUDE.md 单段修改

Step 5：追加 ImplementationLog（~5 分钟）
    → 记录立规动作本身（未改资产、未改代码，仅新增文档 3 + 修改 2）
```

**总预估**：~95 分钟文档 + ~10 分钟联动更新。

---

## 七、立规期的"不做"清单

为避免立规和其他动作混在一起，执行 Step 1-5 期间**不做**以下事：

- **不改任何 SO 资产字段**（即使扫 Registry 时发现配置看着奇怪）
- **不改任何代码**（即使发现 switch 分支有小瑕疵）
- **不重命名资产**（即使命名风格不一致）
- **不清理 PlayerInventory**（已确认无 dangling，也不做排序/美化）
- **不执行任何 Editor 工具**（避免把"立规"和"资产生成"混成一次提交）
- **不写新武器**（立规通过后才开始批量生产）

发现的任何问题一律沉淀为：

- Registry 里的 `Unknown-Historical` 例外条目（仅限真正扫不出来的历史遗留）
- Rules 第 8.4 节的踩坑条目
- 或本 plan 底部的"立规结束后待办"

---

## 八、立规结束后的待办（占位）

执行完 Step 1-5 后，再开新 plan 处理：

1. 批量生产的 Sheba 部件后续批次（真正的"新武器生产"）
2. 如有发现，清理命名不一致的 SO
3. 若 Registry 扫出多个资产 Owner 未收口到 ShebaAssetCreator，评估要不要统一收口
4. 评估是否需要为 Anomaly 家族单独加一份 Projectile Prefab（现状：复用 Matter，是有意的设计选择还是遗留简化？由未来 Anomaly 新核的需求推动）
