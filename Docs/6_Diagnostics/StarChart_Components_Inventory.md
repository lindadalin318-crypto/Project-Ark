# StarChart Components Inventory — 部件清单与完成度诊断

> **诊断时间**：2026-04-25
> **治理更新**：2026-04-26（批次 1 L1-4 落地后，按 `ShebaAssetCreator.cs` 代码权威源重新定性 Prism `_family` 真相）
> **诊断方式**：磁盘 `.asset` YAML 字段直读 + GUID 交叉验证 + `SnapshotBuilder` / `PrismSO` / `StarChartEnums` 代码核对
> **冲突裁决**：代码 / `ShebaAssetCreator` > 磁盘 `.asset` > 本文档 > `StarChart_AssetRegistry.md` > `StarChart_CanonicalSpec.md`
> **范围**：仅记录现役 SO（`Assets/_Data/StarChart/` 下 21 个 `.asset`）+ 行为 prefab 的运行可达性
> **不在范围**：未实装的设计草案、`Batch5AssetCreator` / `ShebaAssetCreator` 工具内部细节（见 `StarChart_AssetRegistry.md`）

---

## 0. 诊断摘要（TL;DR）

| 指标 | 数值 | 备注 |
|---|---|---|
| 总 SO 数 | **21** | 8 Cores + 9 Prisms + 3 Sails + 1 Satellite |
| `PlayerInventory._ownedItems` 引用数 | 21 | 全部已登记，无悬挂引用 |
| **Fully Functional**（数据/行为完整、能在 Play 中按设计运行） | **13** | 见 §6 红绿灯表 |
| **Partially Functional**（仅部分通道生效，存在设计层取舍） | **6** | Sheba Prism 大部分属于"数值家族 + 挂了 Modifier prefab 但被设计丢弃"的状态，参见 §3 的 owner 决策 |
| **Behavior-Broken**（设计意图依赖 Modifier 注入，但当前 100% 失效） | **0** | ✅ L1-4 已修复（`ShebaP_Homing._family=2` 已补齐） |
| **Stub / Test**（占位、测试用） | **1** | `ShebaSail_Standard`（明确 baseline，无副作用） |

**原始治理重心（已落地）**：批次 1（L1-4，2026-04-25）补齐了 `ShebaP_Homing._family=2` Tint 字段，这是本文档最初诊断的 🔴 Broken 项中唯一有明确行为缺陷的案例。

**剩余设计层取舍（非 bug）**：`ShebaP_Boomerang` / `ShebaP_Bounce` / `ShebaP_MinePlacer` 的 `_family` 在 `ShebaAssetCreator.cs` 中被显式设为 Rheology / Rheology / Fractal。这意味着它们的 `_projectileModifierPrefab` 字段**按设计就不会被 `SnapshotBuilder.CollectTintModifierPrefabs` 收集**。本文档 2026-04-25 版本基于"命名暗示"推测它们应当是 Tint，但治理复盘中确认：**`ShebaAssetCreator.cs` 才是这些 Prism 的 owner 权威**，不把它们当作 bug，而是标为 🟡 Partial（数值家族生效，Modifier 按设计舍弃）。若未来需要让它们真正注入 Modifier，正确的路径是改 `ShebaAssetCreator.cs` + 重新生成资产，而不是手改 asset。

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

- Core 行为分支：`ProjectileSpawner.SpawnProjectile` 按 `CoreFamily` switch 走四条管线（Matter / Light / Echo / Anomaly）。由 `StarChartController.ExecuteFire` 委托调用（L3-1 Phase B 拆分，2026-04-25）。
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

## 3. PrismSO（9 个 / Prisms/）

> 关键判定规则：**只有 `_family == 2 (Tint)` 的 Prism，其 `_projectileModifierPrefab` 才会被运行时收集并注入 projectile**。其余家族的 Prism 即使挂了 modifier prefab，也只能通过 `_statModifiers` 起作用。
>
> **Owner 真相源**：Sheba 系列 Prism 由 `Assets/Scripts/Combat/Editor/ShebaAssetCreator.cs` 生成；`_family` 字段以该脚本为权威。手改 asset YAML 属于低层修复手段，不改变 owner。

| Asset | `_family` | 按 Creator 权威是否意图 | `_statModifiers` | `_projectileModifierPrefab` | 实际生效路径 | 等级 |
|---|---|---|---|---|---|---|
| `FractalPrism_TwinSplit` | 0 Fractal | ✅ | +2 ProjectileCount, +15 Spread | (none) | 数值：+2 弹道 +15° spread | 🟢 |
| `RheologyPrism_Accelerate` | 1 Rheology | ✅ | ProjectileSpeed ×1.5 | `Modifier_Bounce` ⚠️（设计不明，建议清字段） | 数值：×1.5 速度<br>**Modifier 按设计不注入**（家族 Rheology） | 🟡 |
| `TintPrism_FrostSlow` | 2 Tint | ✅ | (空) | `Modifier_SlowOnHit` | 行为：命中减速 | 🟢 |
| `ShebaP_TwinSplit`（`DisplayName="Sheba Twin Split"`，L2-3 去重） | 0 Fractal | ✅ | +2 ProjectileCount, +15 Spread | (none) | 数值：+2 弹道 +15° spread | 🟢 |
| `ShebaP_RapidFire` | 1 Rheology | ✅ | FireRate ×1.3 | (none) | 数值：×1.3 射速 | 🟢 |
| `ShebaP_Boomerang`（`DisplayName="Sheba Boomerang"`，L2-3 去重） | **1 Rheology** ✅ by Creator | Creator 设为 Rheology | (空) | `Modifier_Boomerang` | **数值家族生效，Modifier 按设计舍弃**（家族 Rheology，不走 Tint 注入通道） | 🟡 |
| `ShebaP_Bounce` | **1 Rheology** ✅ by Creator | Creator 设为 Rheology | (空) | `Modifier_Bounce` | **数值家族生效，Modifier 按设计舍弃** | 🟡 |
| `ShebaP_Homing` | **2 Tint** ✅ by L1-4 补字段 | Creator 设为 Tint | (空) | `Modifier_Homing` | 行为：Tint 注入管线生效 | 🟢 |
| `ShebaP_MinePlacer` | **0 Fractal** ✅ by Creator | Creator 设为 Fractal | (空) | `Modifier_MinePlacer` | **数值家族生效，Modifier 按设计舍弃** | 🟡 |

**特别说明**：
- `RheologyPrism_Accelerate` 和 `ShebaP_Bounce` 共用 `Modifier_Bounce.prefab`（GUID `eb4f07e4275b54f1b8d239f37f7efb9f`）。本次治理期未深入 prefab 内部组件诊断（建议在真正需要这条行为通道时补一次 prefab 审计）。
- `ShebaP_Homing._family` 是 L1-4（批次 1，2026-04-25）**新补写**的字段。历史资产 YAML 中曾不含此字段，反序列化默认为 0（Fractal），导致 Tint 注入通道永远跳过。现已显式写入 `_family: 2`，与其对应的 `ShebaAssetCreator.cs` 中 `Tint` 意图一致。

### 3.1 Action Items（治理后剩余事项）

| 优先级 | 文件 | 事项 | 状态 |
|---|---|---|---|
| ~~P0~~ | `ShebaP_Homing.asset` | 补 `_family: 2` 字段 | ✅ 已完成（L1-4） |
| ~~P0~~ | `ShebaP_Boomerang.asset` | 原诊断建议改 `_family: 1 → 2` | ❌ **撤销**。`ShebaAssetCreator.cs` 把它显式设为 Rheology，owner 真相是数值家族；原诊断基于"命名暗示"的推测不成立 |
| ~~P0~~ | `ShebaP_Bounce.asset` | 原诊断建议改 `_family: 1 → 2` | ❌ **撤销**。同上，Creator 真相 = Rheology |
| ~~P0~~ | `ShebaP_MinePlacer.asset` | 原诊断建议改 `_family: 0 → 2` | ❌ **撤销**。Creator 真相 = Fractal |
| **P1** | `RheologyPrism_Accelerate.asset` | `_projectileModifierPrefab` 字段设计意图不明（数值家族挂了 Modifier 永远不会生效）。决策：要么清字段（明确只是 stat Prism），要么接受它是装饰性字段 | ⏸ 待决策 |
| ~~P2~~ | 4 个 Modifier prefab | 抽样验证 `Modifier_Bounce` / `Modifier_Homing` / `Modifier_MinePlacer` / `Modifier_Boomerang` 真的挂了 `IProjectileModifier` 实现类 | 部分完成：`Modifier_Homing` 通过 L1-4 后已走通；其余三个按 Creator 设计不走 Tint 注入通道，prefab 内部组件即使不符合也不影响现役行为 |
| **P1** | `ShebaAssetCreator.cs` | 若未来决定让 Boomerang / Bounce / MinePlacer 真正注入 Modifier，应在 Creator 层改 `_family` 并重新生成，而不是手改 asset（owner 真相原则） | ⏸ 待需求触发 |
| **P2** | Editor validator（L2-3 已实现）| 新增 "`_projectileModifierPrefab != null && _family != Tint`" 警告规则 | ⏸ 可并入 `StarChartAuditor` |

**L2-3（批次 2，2026-04-25）**：`DisplayName` 全局唯一性 validator 首次运行时发现 `ShebaP_TwinSplit` / `ShebaP_Boomerang` 与 `FractalPrism_TwinSplit` / `AnomalyCore_Boomerang` 重名，已改为 `"Sheba Twin Split"` / `"Sheba Boomerang"` 去重。

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

## 6. 完成度红绿灯总览（21 个部件一图全收，2026-04-26 治理后）

```
🟢 Functional (13)
├── Cores (8)：MatterCore / LightCore / EchoCore / AnomalyCore
│              ShebaCore_MachineGun / Shotgun / FocusLaser / PulseWave
└── Prisms (5)：FractalPrism_TwinSplit / TintPrism_FrostSlow
                ShebaP_TwinSplit / ShebaP_RapidFire / ShebaP_Homing ✅ L1-4

🟡 Partial (6)
├── RheologyPrism_Accelerate     — 数值生效，Modifier 字段意图不明（待决策）
├── ShebaP_Boomerang              — 数值生效，Modifier 按 Creator 设计舍弃
├── ShebaP_Bounce                 — 数值生效，Modifier 按 Creator 设计舍弃
├── ShebaP_MinePlacer             — 数值生效，Modifier 按 Creator 设计舍弃
├── ShebaSail_Scout               — 数据完整，behavior prefab 内部未验证
├── TestSpeedSail                 — 同上 + 与 Scout 重复
└── ShebaSat_AutoTurret           — 数据完整，behavior prefab 内部未验证

🔴 Broken (0)  ✅ 治理后清零
└── （原 ShebaP_Boomerang / Bounce / Homing 已按 owner 真相重新分类）

⚪ Stub (1)
└── ShebaSail_Standard            — 明确 baseline 无效果
```

> **治理复盘**：原 2026-04-25 初版本把 3 个 Prism 标为 🔴，背后假设是"命名和 Modifier prefab 暗示它们应当是 Tint"。但 `ShebaAssetCreator.cs` 作为 owner 权威源明确把 Boomerang / Bounce / MinePlacer 设为 Rheology / Rheology / Fractal，只有 `Homing` 因为历史缺字段被反序列化成 Fractal——L1-4 已修复。剩下三个不是 bug，而是"数值 Prism 挂了未被激活的 Modifier 字段"，等价于 `RheologyPrism_Accelerate` 的状态。

---

## 7. 系统性观察

### 7.1 Prism `_family` 字段的 owner 真相（治理复盘）

原诊断（2026-04-25 初版）把 "9 Prism 中 4 个 `_family` 设错"列为最大数据债。治理期复盘后结论更新为：

- **真正的 bug**：只有 `ShebaP_Homing` 一个——历史 YAML 缺 `_family` 字段，反序列化默认 0（Fractal），与 `ShebaAssetCreator.cs` 中 `Tint` 意图不一致。L1-4 已补字段，bug 已解。
- **不是 bug 的设计取舍**：`ShebaP_Boomerang` / `ShebaP_Bounce` / `ShebaP_MinePlacer` 的 `_family` 由 `ShebaAssetCreator.cs` 显式设为 Rheology / Rheology / Fractal。Creator 是这些资产的 owner 权威源，所以它们是"数值家族 Prism 挂了装饰性 Modifier prefab"的合法状态，不应标为 Broken。

**防御措施（L2-3 已落地）**：`DisplayName` 全局唯一性 validator 已实现；未来可并入 `StarChartAuditor`，追加"`_projectileModifierPrefab != null && _family != Tint`"警告规则，把这类"挂了但不会生效"的字段标出来，供设计同学决策是清字段还是改家族。

### 7.2 Modifier prefab 共用关系

| Modifier Prefab | 被引用的 SO | 实际注入状态（治理后） |
|---|---|---|
| `Modifier_Boomerang` | `AnomalyCore_Boomerang`（Anomaly 注入路径）+ `ShebaP_Boomerang`（装饰性字段） | Anomaly 路径 ✅；Prism 路径 ❌（家族 Rheology） |
| `Modifier_Bounce` | `RheologyPrism_Accelerate` + `ShebaP_Bounce` | 两者均为装饰性字段，家族均非 Tint |
| `Modifier_SlowOnHit` | 仅 `TintPrism_FrostSlow` | ✅ 唯一通过 Prism Tint 通道注入的 Modifier |
| `Modifier_Homing` | 仅 `ShebaP_Homing` | ✅ L1-4 后通道打通 |
| `Modifier_MinePlacer` | 仅 `ShebaP_MinePlacer` | 装饰性字段（家族 Fractal） |

**结论**：`Modifier_Boomerang` 因为被 Anomaly Core 以"随 projectile 入池"的路径使用，本身的 `IProjectileModifier` 实现类验证是通过了的。这是一个意外的好消息：若未来决定让 `ShebaP_Boomerang` 真正走 Prism 注入路径，只需改 Creator 的 `_family` 并重新生成资产，prefab 本身不需要改。

### 7.3 行为通道完成度断层（治理后）

| 通道 | 现役状态 |
|---|---|
| Core 四家族（Matter / Light / Echo / Anomaly）发射管线 | 🟢 全通 |
| Anomaly Core modifier 注入 | 🟢 通（Boomerang 已验证） |
| **Prism Tint modifier 注入** | 🟢 **2/9 Prism 激活**：`TintPrism_FrostSlow` + `ShebaP_Homing`（L1-4 后） |
| LightSail 行为 | ❓ 1/3 nontrivial（Scout + TestSpeedSail），未深入验证 |
| Satellite 行为 | ❓ 1/1，未深入验证 |

**结论**：射击核心 + Core 部件高度可用；Prism 通道"真实激活数"从 1/9 提升到 2/9；其余 Prism 按设计为"纯数值家族"，等待设计同学在需求触发时决策是否升格为 Tint；Sail/Sat 数量本身就少，需要后续扩充并验证 behavior prefab 内部代码。

---

## 8. 下一步建议（按 ROI 排序，治理后更新）

1. ~~**P0**：批量修复 4 个 Sheba Prism 的 `_family` 字段~~ → 已部分落地（仅 `ShebaP_Homing` 修复，其余 3 个按 owner 真相保持不变）
2. **P1（1 h）**：抽查 `SpeedDamageSailBehavior.prefab` 和 `Sat_AutoTurret.prefab` 的内部组件代码，把 🟡 转 🟢 或定位失效点。
3. **P1（30 min）**：决策 `RheologyPrism_Accelerate._projectileModifierPrefab` 字段是否清除——这是当前唯一语义不明的字段（基线 Prism，不是 Sheba 系列，Creator 权威不能用来解释）。
4. **P2（1 h）**：扩展 L2-3 已实现的 `DisplayName` validator，追加 "`_projectileModifierPrefab != null && _family != Tint`" 警告规则，把装饰性 Modifier 字段显式化。
5. **P2（半天）**：决策 `TestSpeedSail` 与 `ShebaSail_Scout` 的去留——保留谁、删谁、还是合并。
6. **P3（待需求触发）**：若设计同学决定让 `ShebaP_Boomerang` / `Bounce` / `MinePlacer` 真正注入 Modifier，改 `ShebaAssetCreator.cs` 中的 `_family` 再重新生成资产，不要手改 asset。

---

## 9. 参考文档与代码锚点

- 字段命名口径：`Docs/2_TechnicalDesign/Combat/StarChart_AssetRegistry.md` §0
- 资产 owner 真相源：同上 §3 / §4 / §5
- Prism 注入规则：`Assets/Scripts/Combat/StarChart/SnapshotBuilder.cs:134-152`
- 家族枚举定义：`Assets/Scripts/Combat/StarChart/StarChartEnums.cs:13-27`
- 形状枚举定义：同上 `StarChartEnums.cs:56-80`
- 现役 Inventory：`Assets/_Data/StarChart/PlayerInventory.asset`（21 GUID 已交叉验证）
