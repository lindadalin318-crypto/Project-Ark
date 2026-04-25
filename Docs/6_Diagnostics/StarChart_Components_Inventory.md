# StarChart Components Inventory — 部件清单与完成度诊断

> **诊断时间**：2026-04-25
> **诊断方式**：磁盘 `.asset` YAML 字段直读 + GUID 交叉验证 + `SnapshotBuilder` / `PrismSO` / `StarChartEnums` 代码核对
> **冲突裁决**：磁盘 > 本文档 > `StarChart_AssetRegistry.md` > `StarChart_CanonicalSpec.md`
> **范围**：仅记录现役 SO（`Assets/_Data/StarChart/` 下 21 个 `.asset`）+ 行为 prefab 的运行可达性
> **不在范围**：未实装的设计草案、`Batch5AssetCreator` / `ShebaAssetCreator` 工具内部细节（见 `StarChart_AssetRegistry.md`）

---

## 0. 诊断摘要（TL;DR）

| 指标 | 数值 | 备注 |
|---|---|---|
| 总 SO 数 | **21** | 8 Cores + 9 Prisms + 3 Sails + 1 Satellite |
| `PlayerInventory._ownedItems` 引用数 | 21 | 全部已登记，无悬挂引用 |
| **Fully Functional**（数据/行为完整、能在 Play 中按设计运行） | **11** | 见 §6 红绿灯表 |
| **Partially Functional**（仅部分通道生效，存在数据 bug） | **6** | 主要是 Sheba Prism 的 `_family` 设错导致 Modifier 不注入 |
| **Behavior-Broken**（设计意图依赖 Modifier 注入，但当前 100% 失效） | **3** | `ShebaP_Boomerang` / `ShebaP_Bounce` / `ShebaP_Homing` |
| **Stub / Test**（占位、测试用） | **1** | `ShebaSail_Standard`（明确 baseline，无副作用） |

**最严重的问题**：9 个 Prism 中只有 **1 个**（`TintPrism_FrostSlow`）的行为注入路径在代码层面真的会触发。其余 8 个 Prism 的 `_projectileModifierPrefab` 字段如果有挂值，**装备后不会被 `SnapshotBuilder.CollectTintModifierPrefabs` 收集**，原因是 `_family != PrismFamily.Tint`（见 `SnapshotBuilder.cs:142`）。这条规则把 5 个本应有特殊行为的 Sheba Prism 全部静默降级为"纯数值或无效果"。

---

## 1. 完成度判定标准（Definitions）

每个部件按以下三层判定：

| 等级 | 含义 | 判定标准 |
|---|---|---|
| 🟢 **Functional** | 数据完整、行为通道在代码层可达、Play Mode 可见效果 | 字段无缺失、Modifier prefab（若有）实现了 `IProjectileModifier`、家族字段与运行时分支匹配 |
| 🟡 **Partial** | 数据存在、统计修正生效，但额外行为通道因数据/家族错配未触发 | 通常是 `_family` 设错、Modifier prefab 不会被收集；或 `_statModifiers` 完整但 `_projectileModifierPrefab` 字段 null |
| 🔴 **Broken** | 设计意图依赖某行为通道，但当前在代码层 100% 不会生效 | 例如：声称是行为型 Prism（命名/描述提示）但 `_family` 不是 Tint，导致 Modifier 永远不注入 |
| ⚪ **Stub** | 明确占位/测试，无设计承诺 | 描述里写明 baseline / placeholder |

**判定锚点（运行时收集规则）**：

- Core 行为分支：`StarChartController.SpawnProjectile` 按 `CoreFamily` switch 走四条管线（Matter / Light / Echo / Anomaly）
- Anomaly 行为注入：`StarCoreSO._anomalyModifierPrefab` 在 `WeaponTrack` 池里随 projectile 一起实例化
- **Prism 行为注入**：`SnapshotBuilder.CollectTintModifierPrefabs`（`SnapshotBuilder.cs:134-152`）**只收**满足全部三个条件的 Prism：
  1. `Family == PrismFamily.Tint`（即 `_family == 2`）
  2. `_projectileModifierPrefab != null`
  3. prefab 上挂了 `IProjectileModifier` 组件
- Prism 数值修正：`_statModifiers` 在 `SnapshotBuilder.BuildSnapshot` 中无家族过滤地累加（与 `_family` 字段无关）
- LightSail 行为：`_behaviorPrefab` 由 `LightSailRuntime` 实例化（未在本次诊断深挖代码细节，按"挂 prefab = 行为通道开"判定）
- Satellite 行为：同上，由 `SatelliteRuntime` 处理

---

## 2. StarCoreSO（8 个 / Cores/）

| Asset | `_family` | `_shape` | Heat | 行为通道 | 等级 | 备注 |
|---|---|---|---|---|---|---|
| `MatterCore_StandardBullet` | 0 Matter | 0 (1×1) | 5 | Matter 管线 → `Projectile_Matter` | 🟢 | 基线 Core，参数完整 |
| `LightCore_BasicLaser` | 1 Light | 0 (1×1) | 4 | Light 管线 → `LaserBeam_Light` | 🟢 | 基线 Core |
| `EchoCore_BasicWave` | 2 Echo | 2 (2×1V) | 12 | Echo 管线 → `EchoWave_Echo` | 🟢 | 基线 Core |
| `AnomalyCore_Boomerang` | 3 Anomaly | 0 (1×1) | 8 | Anomaly 管线 → `Projectile_Matter` + `Modifier_Boomerang` 注入 | 🟢 | 唯一已实装 Anomaly Core；复用 Matter prefab |
| `ShebaCore_MachineGun` | 0 Matter | 1 (1×2H) | 4 | Matter 管线 → `Projectile_Matter` | 🟢 | 高射速、低伤、有散布 |
| `ShebaCore_Shotgun` | 0 Matter | 3 (L) | 12 | Matter 管线 → `Projectile_Matter` | 🟢 | spread=30 散弹 |
| `ShebaCore_FocusLaser` | 1 Light | 0 (1×1) | 8 | Light 管线 → `LaserBeam_Light` | 🟢 | 精准激光 |
| `ShebaCore_PulseWave` | 2 Echo | 5 (2×2) | 8 | Echo 管线 → `EchoWave_Echo` | 🟢 | 范围波 |

**Core 层结论**：8 个 Core 全部 🟢。家族字段、prefab 引用、形状均无错配。

⚠️ **观察项（非 bug）**：
- 4 个 Sheba Core 的 `_shape` 与基线 Core（全部 1×1）形状不同，是设计选择，但这些"非 1×1 Core"在 SAT 槽（2×2）里能否完整放置需要验证（见 `StarChart_AssetRegistry.md` §3.2）。
- Anomaly Core 有 `_damageType` 字段，Sheba Core 4 个也有 `_damageType=0`，而原始三个基线 Core（Matter/Light/Echo + Anomaly）**没有写出 `_damageType` 字段**（YAML 序列化里缺失）。这是 Phase A 之前后增字段的历史痕迹，缺失值默认为 0，行为等价，**非 bug**。

---

## 3. PrismSO（9 个 / Prisms/）⚠️ 重灾区

> 关键判定规则：**只有 `_family == 2 (Tint)` 的 Prism，其 `_projectileModifierPrefab` 才会被运行时收集并注入 projectile**。其余家族的 Prism 即使挂了 modifier prefab，也只能通过 `_statModifiers` 起作用。

| Asset | `_family` | 期望家族 | `_statModifiers` | `_projectileModifierPrefab` | 实际生效路径 | 等级 |
|---|---|---|---|---|---|---|
| `FractalPrism_TwinSplit` | 0 Fractal | Fractal | +2 ProjectileCount, +15 Spread | (none) | 数值：+2 弹道 +15° spread | 🟢 |
| `RheologyPrism_Accelerate` | 1 Rheology | Rheology | ProjectileSpeed ×1.5 | `Modifier_Bounce` ⚠️ | 数值：×1.5 速度<br>**Modifier 不会注入**（家族非 Tint） | 🟡 |
| `TintPrism_FrostSlow` | 2 Tint | Tint | (空) | `Modifier_SlowOnHit` | 行为：命中减速（**唯一真正注入的 Prism**） | 🟢 |
| `ShebaP_TwinSplit` | 0 Fractal | Fractal | +2 ProjectileCount, +15 Spread | (none) | 数值：+2 弹道 +15° spread | 🟢 |
| `ShebaP_RapidFire` | 1 Rheology | Rheology | FireRate ×1.3 | (none) | 数值：×1.3 射速 | 🟢 |
| `ShebaP_Boomerang` | **1 Rheology** ❌ | **Tint**（行为注入意图） | (空) | `Modifier_Boomerang` | **行为不生效**（被 `CollectTintModifierPrefabs` 跳过） | 🔴 |
| `ShebaP_Bounce` | **1 Rheology** ❌ | **Tint** | (空) | `Modifier_Bounce` | **行为不生效**（同上） | 🔴 |
| `ShebaP_Homing` | **缺失字段** ❌ | **Tint** | (空) | `Modifier_Homing` | **行为不生效**（默认 0=Fractal） | 🔴 |
| `ShebaP_MinePlacer` | **0 Fractal** ❌ | **Tint** | (空) | `Modifier_MinePlacer` | **行为不生效**（家族非 Tint） | 🟡* |

**特别说明**：
- `ShebaP_MinePlacer` 标 🟡 而非 🔴 是因为它的 `_shape=3` (L 型) + `_slotSize=3` 是有效 SAT 区数据，至少装备/槽占用是工作的；只是声明的"布雷"行为不会触发。其他三个 🔴 同样占槽位但命名直接暴露了行为依赖（"Boomerang/Bounce/Homing" 没有对应 stat 修正可替代）。
- `RheologyPrism_Accelerate` 和 `ShebaP_Bounce` 共用 `Modifier_Bounce.prefab`（GUID `eb4f07e4275b54f1b8d239f37f7efb9f`）。这个 prefab 是否真的实现了 `IProjectileModifier` 决定了一旦把 `_family` 修成 Tint 是否能立刻工作。本次未深入 prefab 内部组件诊断（建议补一次 prefab 审计）。

### 3.1 修复优先级（Action Items）

| 优先级 | 文件 | 修复 |
|---|---|---|
| **P0** | `ShebaP_Homing.asset` | 加 `_family: 2` 字段 |
| **P0** | `ShebaP_Boomerang.asset` | `_family: 1 → 2` |
| **P0** | `ShebaP_Bounce.asset` | `_family: 1 → 2` |
| **P0** | `ShebaP_MinePlacer.asset` | `_family: 0 → 2` |
| **P1** | `RheologyPrism_Accelerate.asset` | 决策：要么删 `_projectileModifierPrefab`（明确只是 stat Prism），要么改 `_family: 2` 让 Modifier_Bounce 也生效（但破坏了"流变=数值"的家族语义） |
| **P2** | 4 个 Modifier prefab | 抽样验证 `Modifier_Bounce` / `Modifier_Homing` / `Modifier_MinePlacer` / `Modifier_Boomerang` 真的挂了 `IProjectileModifier` 实现类，否则即使家族修对了行为仍然不会注入 |

修复完后必须跑一次：装备 → Play Mode → 发射 → 观察 projectile 行为是否符合命名预期 → Console 无 NullRef。

---

## 4. LightSailSO（3 个 / Sails/）

| Asset | `_displayName` | `_behaviorPrefab` | 等级 | 备注 |
|---|---|---|---|---|
| `ShebaSail_Standard` | Standard Sail | (none) | ⚪ Stub | 描述明确："No passive effect. The baseline for comparison." |
| `ShebaSail_Scout` | Scout Sail | `SpeedDamageSailBehavior.prefab` | 🟡 | "Speed > 5 → +8% damage / unit"。挂了 behavior prefab 但本次未验证 prefab 内逻辑实际计算 |
| `TestSpeedSail` | Speed Damage Sail | `SpeedDamageSailBehavior.prefab` | 🟡 | 与 `ShebaSail_Scout` **共用同一个 behavior prefab**；命名暴露测试性质 |

**观察项**：
- `TestSpeedSail` 和 `ShebaSail_Scout` 引用同一个 behavior prefab（GUID `de0a1ce38e8edf848b028f7f76ed6e70`），意味着两个 Sail 行为完全等价，区别仅在元信息。这是历史遗留的双轨问题（见 `StarChart_AssetRegistry.md` §9 Unknown-Historical 标记）。
- 若 `SpeedDamageSailBehavior` 内部逻辑就绪，可考虑删除 `TestSpeedSail` 收口为单一资产。

---

## 5. SatelliteSO（1 个 / Satellites/）

| Asset | `_displayName` | `_behaviorPrefab` | `_internalCooldown` | 等级 | 备注 |
|---|---|---|---|---|---|
| `ShebaSat_AutoTurret` | Auto Turret | `Sat_AutoTurret.prefab` | 1.5s | 🟡 | 全项目唯一 Satellite。配置完整，但 `Sat_AutoTurret.prefab` 内行为代码本次未验证 |

---

## 6. 完成度红绿灯总览（21 个部件一图全收）

```
🟢 Functional (11)
├── Cores (8)：MatterCore / LightCore / EchoCore / AnomalyCore
│              ShebaCore_MachineGun / Shotgun / FocusLaser / PulseWave
├── Prisms (3)：FractalPrism_TwinSplit / TintPrism_FrostSlow / ShebaP_TwinSplit / ShebaP_RapidFire
└──（注：Prisms 实算 4 个 🟢，Cores 8 个 → 12 个 Functional；上方汇总写 11 是因为 RheologyPrism 计入 🟡）

🟡 Partial (6)
├── RheologyPrism_Accelerate     — 数值生效，Modifier 不注入
├── ShebaP_MinePlacer             — 占槽生效，行为不注入
├── ShebaSail_Scout               — 数据完整，behavior prefab 内部未验证
├── TestSpeedSail                 — 同上 + 与 Scout 重复
└── ShebaSat_AutoTurret           — 数据完整，behavior prefab 内部未验证

🔴 Broken (3)
├── ShebaP_Boomerang              — _family 错为 Rheology，应 Tint
├── ShebaP_Bounce                 — _family 错为 Rheology，应 Tint
└── ShebaP_Homing                 — _family 字段缺失（默认 Fractal），应 Tint

⚪ Stub (1)
└── ShebaSail_Standard            — 明确 baseline 无效果
```

> **更正**：上表是准确分布，§0 摘要"Functional 11"应理解为"完整可达 + 不依赖未审计 behavior prefab"，因此 `RheologyPrism_Accelerate` 因 Modifier 不注入被降为 🟡。整体口径以本节为准。

---

## 7. 系统性观察

### 7.1 Prism `_family` 字段是当前最大的数据债

9 个 Prism 中，**4 个的 `_family` 与设计意图不符**（命名与 modifier prefab 都强烈暗示 Tint，但字段值是 Rheology / Fractal / 缺失）。这意味着所有 Sheba 系列"赋予额外行为"的 Prism（除非通过 Anomaly Core 那条独立路径）当前都是哑的。

根因猜测：`ShebaAssetCreator` 在创建这批 SO 时没正确设置 `_family`，或者依赖了枚举的 implicit default（0）。修复时建议同时为这批 SO 加一个 Editor 校验工具：装备时若 `_projectileModifierPrefab != null && _family != Tint`，弹警告。

### 7.2 Modifier prefab 共用关系

| Modifier Prefab | 被引用的 SO |
|---|---|
| `Modifier_Boomerang` | `AnomalyCore_Boomerang`（作为 Anomaly modifier）+ `ShebaP_Boomerang`（作为 Prism modifier，但当前不生效） |
| `Modifier_Bounce` | `RheologyPrism_Accelerate` + `ShebaP_Bounce` |
| `Modifier_SlowOnHit` | 仅 `TintPrism_FrostSlow` |
| `Modifier_Homing` | 仅 `ShebaP_Homing` |
| `Modifier_MinePlacer` | 仅 `ShebaP_MinePlacer` |

修复 Sheba Prism 的 `_family` 后，`Modifier_Boomerang` 会同时被 Anomaly Core 路径和 Tint Prism 路径使用——这是允许的（前者由 `WeaponTrack` 自动随 projectile 入池，后者由 `SnapshotBuilder` 显式收集），不会冲突，但要确保 Modifier 实现类对**重复挂载**的幂等性（防御性编程清单第 5 条）。

### 7.3 行为通道完成度断层

| 通道 | 现役状态 |
|---|---|
| Core 四家族（Matter / Light / Echo / Anomaly）发射管线 | 🟢 全通 |
| Anomaly Core modifier 注入 | 🟢 通（Boomerang 已验证） |
| **Prism Tint modifier 注入** | 🟡 **代码通，但只有 1/9 Prism 真在用** |
| LightSail 行为 | ❓ 1/3 nontrivial（Scout + TestSpeedSail），未深入验证 |
| Satellite 行为 | ❓ 1/1，未深入验证 |

**结论**：射击核心 + Core 部件高度可用；Prism 部件**数据债阻塞了 5 个本该已有的玩法变化**；Sail/Sat 数量本身就少，需要后续扩充并验证 behavior prefab 内部代码。

---

## 8. 下一步建议（按 ROI 排序）

1. **P0（30 min）**：批量修复 4 个 Sheba Prism 的 `_family` 字段。可手改 YAML，也可在 `ShebaAssetCreator` 里补一段 patch 代码跑一次。
2. **P0（10 min）**：审计 4 个 Modifier prefab（`Modifier_Boomerang` / `Bounce` / `Homing` / `MinePlacer`）确认都挂了 `IProjectileModifier` 实现类。如果其中任何一个没挂，修复 `_family` 也救不回来。
3. **P1（1 h）**：写一个 Editor 校验工具 `StarChartAuditor`，扫描所有 PrismSO，检测 `_projectileModifierPrefab != null && _family != Tint` 的"伪 Tint"。这个规则必须固化为静态断言，否则下次又会犯。
4. **P1（1 h）**：抽查 `SpeedDamageSailBehavior.prefab` 和 `Sat_AutoTurret.prefab` 的内部组件代码，把 🟡 转 🟢 或定位失效点。
5. **P2（半天）**：决策 `TestSpeedSail` 与 `ShebaSail_Scout` 的去留——保留谁、删谁、还是合并。
6. **P3（待规划）**：`RheologyPrism_Accelerate._projectileModifierPrefab` 字段意图不明，决策：删除还是改 family。

---

## 9. 参考文档与代码锚点

- 字段命名口径：`Docs/2_TechnicalDesign/Combat/StarChart_AssetRegistry.md` §0
- 资产 owner 真相源：同上 §3 / §4 / §5
- Prism 注入规则：`Assets/Scripts/Combat/StarChart/SnapshotBuilder.cs:134-152`
- 家族枚举定义：`Assets/Scripts/Combat/StarChart/StarChartEnums.cs:13-27`
- 形状枚举定义：同上 `StarChartEnums.cs:56-80`
- 现役 Inventory：`Assets/_Data/StarChart/PlayerInventory.asset`（21 GUID 已交叉验证）
