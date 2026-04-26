# StarChart 模块架构审计

> **目的**：从架构完善度、扩展性、冗余度、精简度四个维度，对 `Assets/Scripts/Combat/StarChart/` 做一次事实性审计。
>
> **姊妹文档**：
> - 数据健康度 → `StarChart_Components_Inventory.md`（部件级 21 个 SO 的完成度）
> - 现役主链真相 → `Docs/2_TechnicalDesign/Combat/StarChart_CanonicalSpec.md`
>
> **审计时间**：2026-04-25
>
> **审计范围**：`Assets/Scripts/Combat/StarChart/*.cs`（含 `LightSail/`、`Satellite/` 子目录），共 22 个源文件。不含 `Projectile/`、`Editor/`、UI 侧。
>
> **事实锚定原则**：每个判断必须有代码锚点（`文件名:行号`），推断必须标记为"推断"或"建议"。

---

## 0. TL;DR — 一页纸结论

| 维度 | 评级 | 核心判断 |
|---|---|---|
| 整体架构 | 🟢 良好 | 分层清晰，SO/Track/Runner/Controller 职责分明，四家族 switch 可预测，无反向依赖 |
| 扩展性 | 🟢 良好 | 新 Core/Prism/Sail/Sat 基本不用改核心逻辑；仅"新 CoreFamily"需要同时改 switch + 池化策略 |
| 冗余度 | 🟡 中等 | 4 处可量化冗余：死 static event、SlotSize 双数据源、Spawn 四分支样板、Equip 重载分裂 |
| 精简度 | 🟡 中等 | `StarChartController` 1016 行偏肥，承担 UI API / 发射 / 存档 / 音效四重职责 |
| 架构风险 | 🟢 低 | 无循环依赖、无反向 using、无 `FindObjectOfType`、ServiceLocator 注册正确、Runtime 不写 SO |

**一句话总评**：架构骨架是健康可扩展的，问题集中在"小冗余的代价随 Core 家族增长线性累加"和"Controller 作为大厨房承担太多职责"。目前规模尚未压垮可读性，但已经出现可收口的扩张信号。

---

## 1. 审计方法与评判口径

### 1.1 四个维度的判定标准

| 维度 | 量化指标 | 评级阈值 |
|---|---|---|
| **架构完善度** | 分层是否清晰 / 职责是否单一 / 是否存在跨层反向依赖 | 🟢 无反向依赖且职责清晰；🟡 存在少量越界；🔴 模块边界破损 |
| **扩展性** | 新增一个 Core/Prism/Sail/Sat 要改几个文件 | 🟢 ≤2 文件；🟡 3-5 文件；🔴 ≥6 文件 |
| **冗余度** | 重复代码 / 死代码 / 双通道 / legacy 字段数量 | 🟢 ≤2 处；🟡 3-6 处；🔴 ≥7 处 |
| **精简度** | 单文件行数 + 圈复杂度高点 | 🟢 无文件 >500 行；🟡 1 个文件 500-1000 行；🔴 多文件 >1000 行 |

### 1.2 审计产物

- §2 模块全景
- §3 架构完善度评估（分层、不变量、瑕疵）
- §4 扩展性评估（三类新增场景的成本）
- §5 冗余与精简度评估（具体冗余清单）
- §6 未来扩展风险清单
- §7 改造优先级排序

---

## 2. 模块全景（现役事实）

### 2.1 源文件清单与规模

主目录 16 个 + `LightSail/` 3 个 + `Satellite/` 3 个 = **22 个 `.cs`**。

| 文件 | 行数 | 角色 | 备注 |
|---|---|---|---|
| `StarChartController.cs` | **1016** | 顶层编排 | 单体最大文件，承担 UI API / 发射 / 存档 / 音效所有职责 |
| `WeaponTrack.cs` | 313 | 轨道状态机 | Pure C#，非 MonoBehaviour |
| `SlotLayer.cs` | 294 | 2D 网格容器 | 泛型，给 Core/Prism/Sail/Sat 四层共用 |
| `SnapshotBuilder.cs` | 252 | 数值聚合 | 静态类，零分配字典复用，单一权威 |
| `ItemShapeHelper.cs` | 228 | 形状几何 | C1 单一权威，带 self-check |
| `AutoTurretBehavior.cs` | 127 | Satellite 示例实现 | |
| `LightSailRunner.cs` | 108 | Sail 生命周期 | 与 SatelliteRunner 对称 |
| `StarChartEnums.cs` | 90 | 枚举集中 | 5 组枚举 |
| `SatelliteRunner.cs` | 83 | Sat 生命周期 | 与 LightSailRunner 对称 |
| `StarCoreSO.cs` | 82 | Core SO | |
| `FiringSnapshot.cs` | 75 | 快照数据类 | CoreSnapshot + TrackFiringSnapshot |
| `LoadoutSlot.cs` | 59 | 3 槽容器 | Pure C# |
| `LightSailBehavior.cs` | 50 | MB 基类 | 抽象 |
| `StarChartItemSO.cs` | 48 | SO 基类 | DisplayName / Shape / HeatCost |
| `SpeedDamageSail.cs` | 41 | Sail 示例实现 | |
| `ProjectileParams.cs` | 43 | readonly struct | Spawn 层传参 |
| `SatelliteBehavior.cs` | 40 | MB 基类 | 抽象 |
| `LightSailSO.cs` | 38 | Sail SO | |
| `StarChartContext.cs` | 34 | readonly struct | 依赖注入包 |
| `PrismSO.cs` | 32 | Prism SO | |
| `SatelliteSO.cs` | 42 | Satellite SO | |
| `StatModifier.cs` | 21 | 结构体 | Stat + Operation + Value |
| `FirePoint.cs` | 18 | MB 标记 | muzzle 位置 |
| `IStarChartItemResolver.cs` | 15 | 接口 | 依赖反转 |

**合计 ~3200 行**（主目录 + 两个子目录）。不包含 Projectile 与 UI 侧。

### 2.2 核心依赖拓扑

```
                   ┌─────────────────────────────┐
                   │  StarChartController (MB)    │  ← UI 入口 + 发射编排 + 存档
                   └──────────────┬───────────────┘
             ┌───────────────┬────┴────┬───────────────┐
             ▼               ▼         ▼               ▼
        LoadoutSlot[3]   FirePoint  AudioSource   _context (readonly struct)
             │                                         │
             │  owns 3x                                │  passed to
             ▼                                         ▼
       WeaponTrack Primary + Secondary       LightSailRunner + SatelliteRunner[]
             │                                         │
             │  owns 3x SlotLayer<T>                   │  instantiates
             ▼                                         ▼
       Core/Prism/Sat                          LightSailBehavior / SatelliteBehavior
                                                    (MonoBehaviour)
             │
             │  snapshot build
             ▼
        SnapshotBuilder (static)
             │
             │  returns
             ▼
        TrackFiringSnapshot → List<CoreSnapshot>
             │
             │  dispatch (switch CoreFamily)
             ▼
        SpawnMatter / SpawnLight / SpawnEcho / SpawnAnomaly
             │                │             │             │
             └──> Projectile  LaserBeam     EchoWave      Projectile + AnomalyMod
                         │         │            │             │
                         └─────────┴────────────┴─────────────┘
                              共同注入 Tint Modifiers (JsonUtility 深拷贝)
```

**没有**反向依赖：

- `ProjectArk.Combat` 不引用 `ProjectArk.UI`（通过 `IStarChartItemResolver` 反转）
- `ProjectArk.Core` 不引用 `ProjectArk.Combat`（通过 `CombatEvents` 静态事件总线）
- ServiceLocator 注册正确：`StarChartController.cs:329 / 404`（Awake 注册 / OnDestroy 注销）

---

## 3. 架构完善度评估（🟢）

### 3.1 分层结构

| 层 | 文件 | 职责 | 状态 |
|---|---|---|---|
| **数据层** | `*SO.cs` | authored 配置，运行时只读 | ✅ 严守 SO 只读原则 |
| **容器层** | `SlotLayer<T>` / `LoadoutSlot` / `WeaponTrack` | 装备槽 + 轨道状态 | ✅ 全部 Pure C#，无 MB 污染 |
| **聚合层** | `SnapshotBuilder` | Prism 数值聚合 → `CoreSnapshot` | ✅ 静态单点、零分配字典 |
| **发射层** | `StarChartController` | Spawn 分发 + VFX + 后坐力 + 音效 | 🟡 承担职责偏多（见 §5） |
| **运行时层** | `LightSailRunner` / `SatelliteRunner` | Behavior 生命周期 | ✅ 对称设计 |
| **行为层** | `LightSailBehavior` / `SatelliteBehavior` | 具体 MB 实现 | ✅ 抽象基类清晰 |
| **跨层抽象** | `StarChartContext` / `IStarChartItemResolver` | 依赖注入 + 反转 | ✅ readonly struct + 接口 |

### 3.2 关键不变量检查

| 不变量 | 是否遵守 | 锚点 |
|---|---|---|
| Snapshot 单一构建路径 | ✅ | 任何 `CoreSnapshot` 只能来自 `SnapshotBuilder.BuildCoreSnapshot`；`WeaponTrack.TryFire` 是唯一调用方 |
| SO 运行时不可变 | ✅ | SnapshotBuilder 读 `core.BaseDamage` 写入 `snapshot.Damage`，原 SO 不变 |
| Modifier 深拷贝 | ✅ | `ProjectileSpawner.InstantiateModifiers`（L3-1 Phase B 后由 Controller 迁入 Spawner） 用 JsonUtility 序列化拷贝 |
| 事件卫生 | ✅ | `StarChartController.OnDestroy` 清所有事件 + `LoadoutManager.Dispose` 遍历 3 个 slot 的所有 runner；Runner.Dispose 取消 Heat 订阅（`LightSailRunner.cs:88-93`）。L3-1 拆分后该职责仍由 Controller / Manager 共同持有 |
| LayerMask 显式声明 | ✅ | `AutoTurretBehavior._enemyLayer` 要求 Inspector 显式设置，无 `~0` 默认 |
| ServiceLocator 生命周期 | ✅ | `StarChartController.Awake` / `OnDestroy` 注册 / 注销（行号随 L3-1 Phase C 拆分后漂移，不再固定） |
| 禁止 FindObjectOfType | ✅ | 全模块 0 处使用 |
| Pool 入口收敛 | ✅ | 所有 Pool 获取走 `WeaponTrack.GetProjectilePool` / `GetMuzzleFlashPool`，无散落调用 |
| 依赖方向 | ✅ | UI → Combat → Core，无反向 |

### 3.3 结构性瑕疵（轻微）

#### 瑕疵 1：`LoadoutSlot.cs:43` 格式残缺

```
SailLayer      = new SlotLayer<LightSailSO>(initialCols: 2, initialRows: 1);
```

这一行没有前导缩进（应为 12 空格）。不影响功能，但是 "code review 没做干净" 的视觉残留。**修复成本：5 秒**。

#### 瑕疵 2：`WeaponTrack.cs` 的 Equip 重载分裂

```csharp
public bool EquipSatellite(SatelliteSO sat)                          // cs:160-165（legacy）
public bool EquipSatellite(SatelliteSO sat, int anchorCol, int anchorRow)  // cs:150-155（current）
```

Core / Prism 有 `EquipCore(core)` + `EquipCore(core, col, row)` 两个重载（`WeaponTrack.cs:93-109`），Sat 也复制了这个双 API。legacy 版 `EquipSatellite(sat)` 在以下 4 处被调用：

- `StarChartController.cs:202`（runtime equip API，auto-fit 路径）
- `StarChartController.cs:746`（Debug 初始化）
- `StarChartController.cs:895`（legacy 存档迁移）
- `StarChartController.cs:998`（新版存档恢复）

全部是"不关心具体位置"的场景。这个重载**并非真正的 legacy**，只是命名误导——它提供的是"自动寻位"语义，与 "指定锚点" 语义**互补**，两者都应保留。建议：注释从 `/// <summary> Legacy: ... </summary>` 改为 `/// <summary> Auto-fit: equip at first available slot. </summary>`。

---

## 4. 扩展性评估（🟢）

### 4.1 三类典型新增场景的成本

#### 场景 A：新增一个 Matter 家族的 Core（例如 `Sheba_Railgun`）

| 文件 | 是否需要修改 | 变更内容 |
|---|---|---|
| `StarCoreSO.cs` | ❌ | 不改 |
| `StarChartEnums.cs` | ❌ | 不改（家族已存在） |
| `StarChartController.cs` | ❌ | 不改（switch 已覆盖 Matter） |
| `WeaponTrack.cs` | ❌ | 不改（Pool 预热已覆盖 Matter） |
| `SnapshotBuilder.cs` | ❌ | 不改 |
| Editor | `ShebaAssetCreator.cs` + 对应 .asset | 创建 SO |

**文件修改数：0（代码层）+ 资产创建。🟢 优秀。**

#### 场景 B：新增一个 Prism（例如 `Sheba_Charge`，纯数值 Rheology）

同场景 A，**0 代码改动**。纯数据驱动。

#### 场景 C：新增一个 Tint Prism 附带行为注入（例如 `Sheba_Poison`）

| 文件 | 是否需要修改 | 变更内容 |
|---|---|---|
| 新建 `PoisonOnHitModifier.cs` | ✅ | 实现 `IProjectileModifier` |
| 新建 `Modifier_Poison.prefab` | ✅ | 挂 PoisonOnHitModifier |
| 新建 `TintPrism_Poison.asset` | ✅ | 引用 prefab，_family = Tint |
| 其他代码 | ❌ | 不改 |

**文件修改数：0（已有代码）+ 3 个新增。🟢 优秀。**

#### 场景 D：新增一个 CoreFamily（例如 `Gravity` 引力家族）

| 文件 | 是否需要修改 | 变更内容 |
|---|---|---|
| `StarChartEnums.cs` | ✅ | 添加 `Gravity` 枚举值 |
| `StarChartController.cs` | ✅ | 添加 `case CoreFamily.Gravity: SpawnGravityEntity(...)`；**新增一个 Spawn*方法 70-100 行** |
| `WeaponTrack.cs` | ✅ | `InitializePools` switch 添加 Gravity 分支 |
| `FiringSnapshot.cs` | 🟡 | 若 Gravity 需要额外字段（如 `GravityStrength`），CoreSnapshot 要扩字段 |
| 新建 `GravityProjectile.cs` + prefab | ✅ | 具体实现 |

**文件修改数：3-4 个代码文件 + 资产。🟡 中等（符合预期）。**

→ 这是架构的"热点"：`CoreFamily` 增加时，`switch` 分发和池化预热是**硬耦合**，无法做到 0 改动。是否需要治理见 §5.3。

#### 场景 E：新增一个 LightSail 效果（例如 `ChargeShotSail`）

| 文件 | 是否需要修改 | 变更内容 |
|---|---|---|
| 新建 `ChargeShotSail.cs` | ✅ | 继承 `LightSailBehavior` |
| 新建 `ChargeShotSail.prefab` | ✅ | 挂 ChargeShotSail |
| 新建 `ShebaSail_Charge.asset` | ✅ | 引用 prefab |

**🟢 优秀**。策略模式在 Sail/Sat 两条线都落地得很干净。

### 4.2 扩展性总评

| 扩展场景 | 改动文件数 | 评级 |
|---|---|---|
| 新 Core / Prism（已有家族） | 0 | 🟢 |
| 新 Tint Prism + 行为注入 | 0 + 3 新建 | 🟢 |
| 新 Sail / Satellite | 0 + 2-3 新建 | 🟢 |
| 新 CoreFamily | 3-4 | 🟡 |
| 新 ItemShape | 1（`ItemShapeHelper.cs`） | 🟢（已通过 C4 guard 收口） |
| 新 WeaponStat | 1（`StarChartEnums.cs`） + SnapshotBuilder 自动拾取 | 🟢 |

**结论**：架构在"横向扩展"（新部件）方向设计得很好，在"纵向扩展"（新家族维度）仍有硬耦合代价。这是**合理的工程取舍**——家族数量变化远低于部件数量。

---

## 5. 冗余与精简度评估

### 5.1 冗余清单（4 处）

#### 🔴 冗余 1：`StarChartController.OnWeaponFired` 是真正的死代码

```csharp
// StarChartController.cs:62
public static event Action<Vector2, float> OnWeaponFired;
...
// StarChartController.cs:496
OnWeaponFired?.Invoke(spawnPos, DEFAULT_NOISE_RADIUS);
```

**grep 验证**：全项目无订阅者。`EnemyPerception.cs:97 / 102` 订阅的是 `CombatEvents.OnWeaponFired`，不是这个 local static event。

**后果**：每次开火多一次 null-check + invoke，CPU 代价小，但认知代价不小——新人看到会以为有两条独立的事件通道。

**治理建议**：直接删除 `StarChartController.OnWeaponFired` 的**字段声明和 invoke**（cs:62 和 cs:496）。注释顺便更新为"订阅走 `CombatEvents.OnWeaponFired`"。

**成本**：2 行删除 + 1 行注释。**P1 优先级**。

#### 🟡 冗余 2：`_slotSize` 与 `_shape` 的数据源分裂

```csharp
// StarChartItemSO.cs:20-25
[SerializeField] private int _slotSize = 1;   // Range(1,3)，UI 读
[SerializeField] private ItemShape _shape;    // SlotLayer 读
```

两个字段**描述的是同一件事**（"这个 item 多大"），但消费者不同：

| 消费者 | 读哪个字段 | 用途 |
|---|---|---|
| `SlotLayer.TryPlace` → `ItemShapeHelper.GetCells` | `_shape` | 占用哪些格子 |
| `SlotLayer.UsedSpace` / `FreeSpace`（`SlotLayer.cs:72-85`） | `_slotSize` | 总占用格数 |
| `StarChartController.cs:152` 用 `SailLayer.FreeSpace <= 0` | → `_slotSize` | Sail 驱逐判断 |
| `UI/ItemDetailView.cs:134` 显示 `SIZE N` | `_slotSize` | UI 文字 |
| `InventoryItemView.cs` | `_slotSize` | UI 角标 |

**问题**：
- 如果某个 SO 的 `_slotSize=2` 但 `_shape=Shape1x1`（或反过来），两者**不一致**。现役数据是否一致？未验证，但依赖人工填对字段是脆弱的。
- `_shape` 才是单一权威（见 `ItemShapeHelper.cs:8-19` 的 C1 契约注释：`No module may infer occupancy from slotSize or area.`），但 `_slotSize` 还被 UI 和 FreeSpace 读。
- `ItemShapeHelper.GetBounds(shape)` 已经能给出 bounding box，UI 完全可以从这里计算尺寸显示。

**治理建议**：
1. **P2**：把 `SlotLayer.UsedSpace` 改成 `item.Shape` 的 cell count 之和（用 `ItemShapeHelper.GetCells(item.Shape).Count`），让 UsedSpace 也走 _shape 单一权威。
2. **P2**：UI `ItemDetailView` / `InventoryItemView` 改读 `ItemShapeHelper.GetBounds(item.Shape)` 或 `GetCells(...).Count`。
3. **P3**：移除 `_slotSize` 字段本身（但这一步要改资产文件，收尾成本略高）。

**成本**：P2 改动约 10 行代码。P3 改动约 15 个 .asset 文件 + 清除字段。

#### 🟡 冗余 3：四个 Spawn 分支的样板重复 — 已迁出 Controller（L3-1 Phase B，2026-04-25）

> **Owner 更新**：以下四个 Spawn 分支已从 `StarChartController` 迁移到 `ProjectileSpawner`（L3-1 Phase B），锚点改为 `Assets/Scripts/Combat/StarChart/ProjectileSpawner.cs`。冗余现状不变，治理建议与原文一致。

`ProjectileSpawner.SpawnMatterProjectile` / `SpawnLightBeam` / `SpawnEchoWave` / `SpawnAnomalyEntity` 共 ~118 行，重复逻辑：

| 步骤 | 四分支是否相同 |
|---|---|
| 计算 angle | ✅ 完全相同 |
| `track.GetProjectilePool(coreSnap.ProjectilePrefab)` | ✅ 完全相同 |
| `pool.Get(spawnPos, Quaternion.Euler(...))` | ✅ 完全相同 |
| `GetComponent<XxxBehavior>()` 拿组件 | 不同（Projectile/LaserBeam/EchoWave） |
| `ProjectileSize` 缩放 | Matter/Echo/Anomaly 相同，Light 缺省 |
| `ToProjectileParams()` + Sail `ModifyProjectileParams` | ✅ 完全相同 |
| `InstantiateModifiers(targetObj, TintModifierPrefabs)` | ✅ 完全相同 |
| 具体 Init 调用 | 不同（`Initialize / Fire / Fire`） |
| Fallback 行为 | Light/Echo 有 fallback 到 Matter，Matter/Anomaly 直接 return |

**冗余部分**：约 70% 逻辑重复，30% 分支特化。

**为什么没有被抽象掉**：因为 `Projectile.Initialize(direction, parms, modifiers)` 和 `LaserBeam.Fire(spawnPos, direction, parms, modifiers)` 的参数签名不同；EchoWave 还多一个 `spread` 参数。**这是接口级别的不统一**，抽象代价不小。

**治理建议**：
- **P3**（低优）：定义一个 `ISpawnable` 接口，要求 `Projectile/LaserBeam/EchoWave` 都实现 `InitializeFromSnapshot(spawnPos, direction, parms, modifiers, coreSnap)`，Spawn 分支就能合并成一个方法。代价是要改 3 个 Projectile 类的 API。
- **实际建议**：**不急着治理**。当前 4 分支代码清晰可读，每个分支 25-35 行。只有在加入第 5 家族时才会感到痛，届时再抽象也不晚。记录此债务即可。

#### 🟢 冗余 4：`EquipSatellite` 的 legacy 注释误导

如 §3.3 瑕疵 2 所述，`WeaponTrack.EquipSatellite(sat)` 的 `/// Legacy` 注释是误导。

**治理建议**：改注释为"Auto-fit equip"。**成本：1 分钟。P1。**

### 5.2 精简度评估

#### 单文件行数分布

```
1016 行 │ StarChartController.cs  ←── 🟡 单体过大
 313 行 │ WeaponTrack.cs
 294 行 │ SlotLayer.cs
 252 行 │ SnapshotBuilder.cs
 228 行 │ ItemShapeHelper.cs
 127 行 │ AutoTurretBehavior.cs
 108 行 │ LightSailRunner.cs
 ...（其余均 <100 行）
```

**除 `StarChartController` 外，所有文件 ≤313 行，符合单文件 500 行的健康线。**

#### `StarChartController` 的职责画像

把 1016 行按职责切片：

| 职责块 | 行数 | 合理性 |
|---|---|---|
| UI 装备 API（EquipLightSail / EquipSatellite / UnequipXxx / SwitchLoadout） | ~210 行（cs:105-325） | 🟡 可拆出 `LoadoutManager` 独立类 |
| Awake/Start/Update/OnDestroy 生命周期 | ~110 行（cs:327-432） | ✅ 合理 |
| ExecuteFire + SpawnXxx 四分支 | ~220 行（cs:439-645） | 🟡 70% 重复见 §5.1.3 |
| VFX / Audio（MuzzleFlash / PlayFireSound） | ~22 行（cs:647-668） | ✅ 合理 |
| Debug loadout 初始化 | ~50 行（cs:670-710 + 725-750） | 🟢 可以保留（[Header Debug]） |
| Pool 初始化 | ~12 行（cs:712-723） | ✅ 合理 |
| InstantiateModifiers 静态方法 | ~28 行（cs:760-787） | 🟡 可拆到独立静态类 |
| Save/Load 序列化（Export/Import/ImportTrack/Track Satellites） | ~220 行（cs:797-1006） | 🟡 可拆出 `StarChartSaveSerializer` 独立类 |
| RotateVector2 数学工具 | ~7 行（cs:1008-1014） | 🟢 小函数，保留 |

**合理的切分**（按依赖方向和职责粒度）：

```
StarChartController.cs  →  保留：Awake/Start/Update/OnDestroy + ExecuteFire 编排 + 音效 + Debug loadout
  ↓ 拆出新文件
  · LoadoutManager.cs           ← Equip/Unequip/SwitchLoadout/DisposeSlot/RebuildSlot（210 行）
  · StarChartSaveSerializer.cs  ← Export/Import/ExportTrack/ImportTrack/ImportTrackSatellites（220 行）
  · ProjectileSpawner.cs        ← Spawn 4 分支 + InstantiateModifiers（260 行）
```

拆分后预估：

- `StarChartController.cs` → ~280 行（剩 Awake/Update/ExecuteFire 编排 + 音效）
- `LoadoutManager.cs` → ~210 行
- `StarChartSaveSerializer.cs` → ~220 行
- `ProjectileSpawner.cs` → ~260 行

**治理建议**：**P2 优先级**。不急着做，但下次大改时可以顺手切。注意：拆分是为了可读性，不是为了性能；拆出的类都是 `internal` pure C#，不增加 GameObject 成本。

### 5.3 `CoreFamily` 的治理评估

§4.1 场景 D 指出，加新家族要改 3-4 处。是否值得现在治理？

**不治理的理由**：
- 家族是**有限的设计概念**（Matter/Light/Echo/Anomaly 4 个已覆盖射击游戏的主要形态）
- 未来新增 Gravity/Plasma 等家族的频率预计 < 1 次/年
- 治理方案（如每个家族一个 Spawner 类 + 注册表）的复杂度超过当前 switch

**治理的理由**：
- 现役代码里，Spawn 分支的重复率就在 70%（§5.1.3）
- `WeaponTrack.InitializePools` 的 switch 重复分支（`WeaponTrack.cs:242-272`）

**建议**：**保持现状**。等到第 5 家族出现时，同时做 §5.2 的 `ProjectileSpawner` 拆分 + §5.1.3 的 `ISpawnable` 接口抽象。**现在治理属于过早优化**。

---

## 6. 未来扩展风险清单

### 6.1 `DisplayName` 作为跨存档 ID 的重名风险

- `ExportTrack` 写 `cores[i].DisplayName`（`cs:925`）
- `ImportTrack` 读 `resolver.FindCore(displayName)`（`cs:966`）
- 若两个 SO 的 `DisplayName` 重复，resolver 会**静默取错**（具体由 UI 侧 resolver 实现决定）

**现状**：21 个 SO 的 DisplayName 是否全部唯一？未经程序性验证。CanonicalSpec §8.2 已把"DisplayName 全局唯一"列为约束，但没有运行时检查。

**建议**：**P2**。在 `ShebaAssetCreator` 或 Editor 自测工具里加 validator：遍历 PlayerInventory 的 21 个 item，检测重名。

### 6.2 `MAX_PROJECTILES_PER_FIRE = 20` 硬上限

```csharp
// SnapshotBuilder.cs:14
public const int MAX_PROJECTILES_PER_FIRE = 20;
```

20 这个数字是性能拐点（对象池 + Physics2D 查询），但**硬编码常量**而非 SO 配置。如果未来要给"大招"功能临时提升上限，需要改源码。

**建议**：**P3**（低优）。可以改成从某个全局配置 SO 读取，但当前 20 发足够一切现役需求，过早提取没有收益。

### 6.3 `DEFAULT_NOISE_RADIUS = 15f` 硬编码

```csharp
// StarChartController.cs:65
private const float DEFAULT_NOISE_RADIUS = 15f;
```

每次开火向敌人感知广播的音量半径。

**问题**：
- 不同武器"声音"不一样（冲锋枪 vs 狙击），但现役用统一半径
- 未来若加"消音"、"声波炸弹"功能，会绕过此常量

**建议**：**P3**。迁入 `StarCoreSO` 字段（默认 15），需要静音武器时重写即可。目前不急。

### 6.4 `Update` 循环的空守卫重复

`StarChartController.Update`（cs:367-395）有两处守卫：
```csharp
if (_loadouts == null) return;    // cs:369
...
if (_inputHandler == null) return;  // cs:387
```

这两个字段都在 `Awake` 初始化，正常情况下运行时不可能为 null。当前守卫是为了"组件缺失时 Update 不崩"。这是防御编程，但**掩盖了真正的配置错误**（例如 FirePoint 未赋值）——应该让它响亮失败，而不是静默 return。

**建议**：**P2**。`Awake` 里如果任何必需组件缺失，直接 `Debug.LogError + enabled = false`（现在 `InitializeAllPools` 已经这么做）。Update 的守卫可以删除或改成 `Debug.LogError` 首次触发。

### 6.5 Satellite Behavior 的 Per-GameObject 成本

每装备一个 Satellite 会 `Instantiate` 一个子 GameObject（`SatelliteRunner.cs:67`）。3 个 Loadout × 2 个 Track × 2 个 Sat 上限 = 最多 12 个隐藏 GameObject 挂在 Ship 下。

**现状**：数量级合理，没有实际问题。

**潜在风险**：若未来 Sat 数量激增（如某个部件池让玩家能装 8 个 Sat），GameObject 数会线性增长。Runtime Behavior 设计本身没问题，但需要关注。

**建议**：不需要立即治理，但在 `SatelliteRunner.Dispose` 时确保 `Object.Destroy` 被调用（现役已正确实现，见 `SatelliteRunner.cs:58`）。

---

## 7. 改造优先级排序

### 7.1 P1（应尽快做，成本低） — ✅ 全部完成（2026-04-25）

| # | 任务 | 锚点 | 预计耗时 | 状态 |
|---|---|---|---|---|
| 1 | 删除 `StarChartController.OnWeaponFired` 死代码 | `cs:62 / 496` | 3 分钟 | ✅ 批次 1（L1-1） |
| 2 | 修正 `LoadoutSlot.cs:43` 缩进 | `LoadoutSlot.cs:43` | 1 分钟 | ✅ 批次 1（L1-2） |
| 3 | `WeaponTrack.EquipSatellite(sat)` 改注释为 "Auto-fit" | `WeaponTrack.cs:157-160` | 1 分钟 | ✅ 批次 1（L1-3） |
| 4 | 修复 `StarChart_Components_Inventory.md` 列出的 Prism `_family` 字段错误 | 见姊妹文档 §7 | 30-60 分钟 | ✅ 批次 1（L1-4，仅补 `ShebaP_Homing._family: 2`；Boomerang / Bounce / MinePlacer 按 `ShebaAssetCreator` 代码真相保持 Rheology/Rheology/Fractal 不动，不再视为"错误"） |

**合计 P1 投入：约 1 小时，显著降低认知噪音。** ✅ 已落地。

### 7.2 P2（中优，可排期） — ✅ 全部完成（2026-04-25）

| # | 任务 | 收益 | 预计耗时 | 状态 |
|---|---|---|---|---|
| 5 | `StarChartController` 拆分（`LoadoutManager / SaveSerializer / ProjectileSpawner`） | 单文件 1016 → 280 行，可读性大增 | 3-4 小时 | ✅ 批次 3（L3-1），拆出 `LoadoutManager.cs` / `ProjectileSpawner.cs` / `StarChartSaveSerializer.cs`，Controller 保留对外 API 作为 Facade |
| 6 | `SlotLayer.UsedSpace` 改为 `_shape` 单一权威 | 消除 `_slotSize` / `_shape` 双源风险 | 30 分钟 | ✅ 批次 2（L2-1） |
| 7 | UI 侧（`ItemDetailView` / `InventoryItemView`）改读 `ItemShapeHelper.GetBounds` | 同上 | 30 分钟 | ✅ 批次 2（L2-2） |
| 8 | Editor validator：`DisplayName` 全局唯一性检查 | 预防存档取错的静默 bug | 30 分钟 | ✅ 批次 2（L2-3），首次运行发现 `ShebaP_TwinSplit` / `ShebaP_Boomerang` DisplayName 重名，已改为 `"Sheba Twin Split"` / `"Sheba Boomerang"` |
| 9 | `Update` 空守卫收紧为"Awake 失败 → 响亮错误" | 消除静默失效 | 15 分钟 | ✅ 批次 2（L2-4） |

### 7.3 P3（低优，只在合适时机做）

| # | 任务 | 触发条件 | 状态 |
|---|---|---|---|
| 10 | `_slotSize` 字段移除 | 所有资产过一次 ShebaAssetCreator 重新生成后 | ✅ 批次 4（L3-2），已从 `StarChartItemSO.cs` 删除字段；磁盘 YAML 孤儿字段 Unity 下次保存时自动清除 |
| 11 | `MAX_PROJECTILES_PER_FIRE` → 全局配置 SO | 策划需要大招功能时 | ⏸ 未触发 |
| 12 | `DEFAULT_NOISE_RADIUS` → Core SO 字段 | 需要消音武器时 | ⏸ 未触发 |
| 13 | `ISpawnable` 接口 + `ProjectileSpawner` 统一 | 新增第 5 个 CoreFamily 时 | ⏸ 未触发（`ProjectileSpawner` 已拆出，接口化留给后续） |

### 7.4 **不建议现在做**

- 引入 DI 容器替代 `StarChartContext`：目前 readonly struct + 字段注入足够，引入 Zenject/VContainer 是过度工程
- 为新 CoreFamily 做注册表抽象：当前 switch 足够可读
- 把 `WeaponTrack` 改成接口：没有第二种实现需求

---

## 8. 与姊妹文档的交叉引用

| 话题 | 本文档 | 数据健康度（Inventory） | 现役真相（CanonicalSpec） |
|---|---|---|---|
| 单个 SO 的完成度 | ❌ | ✅ | ❌ |
| 代码架构评估 | ✅ | ❌ | ❌ |
| 现役发射链的事实描述 | §2.2 拓扑图 | ❌ | ✅ §3 |
| 冗余与死代码清单 | ✅ §5 | ❌ | ❌ |
| 扩展成本分析 | ✅ §4 | ❌ | ❌ |
| 部件数据 bug（如 `ShebaP_Homing` 缺 _family） | 引用 | ✅ | ❌ |

---

## 附录 A：审计所读文件清单

### Runtime 代码（22 个）

主目录：`StarChartController` `WeaponTrack` `SlotLayer` `SnapshotBuilder` `ItemShapeHelper` `StarChartEnums` `StarCoreSO` `PrismSO` `LightSailSO` `SatelliteSO` `StarChartItemSO` `LoadoutSlot` `FiringSnapshot` `ProjectileParams` `StarChartContext` `StatModifier` `FirePoint` `IStarChartItemResolver`

`LightSail/`：`LightSailBehavior` `LightSailRunner` `SpeedDamageSail`

`Satellite/`：`SatelliteBehavior` `SatelliteRunner` `AutoTurretBehavior`

### 对照文档

- `StarChart_CanonicalSpec.md`（全文）
- `StarChart_AssetRegistry.md`（头 80 行，字段口径）
- 姊妹诊断：`StarChart_Components_Inventory.md`

### grep 验证

- `OnWeaponFired` 订阅者 → 仅 `CombatEvents.OnWeaponFired` 被订阅，`StarChartController.OnWeaponFired` 0 订阅
- `_slotSize` 消费者 → UI 侧 `ItemDetailView.cs:134` + `InventoryItemView.cs:24` + `SlotLayer.UsedSpace`
- `EquipSatellite` / `UnequipSatellite` → 9 处调用，4 种语义

---

> **审计完成时间**：2026-04-25 14:41
>
> **落地结果（2026-04-26 更新）**：P1 / P2 全部完成，P3 第 10 项（`_slotSize` 字段移除）完成。剩余 P3（#11 / #12 / #13）均为"未触发"状态，留待后续场景实际需要时处理。
>
> **改造落地方案（已归档）**：`Docs/0_Plan/complete/2026-04-25-starchart-refactor-plan.md`
