# StarChart AssetRegistry — 资产 Owner 映射表

> **文档定位**：本文件是 StarChart 模块**当前现役资产**的 **owner 映射真相源**。回答"哪个文件存在、由谁生成、引用了谁、归谁维护"。
>
> **冲突裁决优先级**（与 `StarChart_CanonicalSpec.md` 保持一致）：
>
> ```
> 代码 / .asset / .prefab 实际内容  >  本 Registry  >  StarChart_CanonicalSpec.md  >  Implement_rules.md
> ```
>
> 若本文档与磁盘上的 `.asset` / `.prefab` / `.meta` 不一致，**以磁盘为准**，并立即修正本文档。
>
> **扫码时间**：2026-04-24（初版）/ 2026-04-24 复审修正  
> **扫码方式**：GUID 交叉验证（`.asset` YAML 的 `guid:` 字段对照 `.meta` 文件的 `guid` 字段）+ Editor 工具代码 grep + SO 字段磁盘值抽取。所有结论均可从磁盘复现，禁止推测。
>
> **字段命名口径**（与 `StarCoreSO.cs` / `PrismSO.cs` 实际序列化字段一致）：
>
> - Core 家族字段：`_family`（类型 `CoreFamily`），**不是** `_coreFamily`
> - Prism 家族字段：`_family`（类型 `PrismFamily`），**不是** `_prismFamily`
> - 形状字段：`_shape`（类型 `ItemShape`，int 枚举）
> - Legacy 1D 尺寸：`_slotSize`（仅保留字段但不再驱动任何逻辑，见 §3.2）
>
> 审计脚本以及任何 grep 命令必须使用以上真实字段名，否则会全空返回。
>
> **Owner 工具说明**：
>
> - **`Batch5AssetCreator`**：`Assets/Scripts/Combat/Editor/Batch5AssetCreator.cs`，Legacy、Hidden from menu。负责 Batch 5 时期的初版 Prefab 与 SO 资产。
> - **`ShebaAssetCreator`**：`Assets/Scripts/Combat/Editor/ShebaAssetCreator.cs`，Legacy、Hidden from menu。负责示巴星关卡阶段追加的 Sheba 系列资产。
> - **`Unknown-Historical`**：不在任何 Editor 工具代码中出现的资产。推定为手工创建或已删除的历史工具产物。**新增星图部件禁止走这条路径**。

---

## 1. 资产目录结构

```
Assets/_Data/StarChart/
├── Cores/                    # StarCoreSO 资产（8 个）
├── Prisms/                   # PrismSO 资产（9 个）
├── Sails/                    # LightSailSO 资产（3 个）
├── Satellites/               # SatelliteSO 资产（1 个）
├── Prefabs/                  # Projectile / LaserBeam / EchoWave / Modifier prefab（8 个）
└── PlayerInventory.asset     # StarChartInventorySO，聚合全部 21 个 SO 的 GUID 引用

Assets/_Prefabs/StarChart/
├── Sat_AutoTurret.prefab               # Satellite behavior prefab
└── SpeedDamageSailBehavior.prefab      # LightSail behavior prefab
```

**资产总数现状（Phase A 基线）**：

| 类别 | 数量 | 目录 |
|------|------|------|
| StarCoreSO | 8 | `Assets/_Data/StarChart/Cores/` |
| PrismSO | 9 | `Assets/_Data/StarChart/Prisms/` |
| LightSailSO | 3 | `Assets/_Data/StarChart/Sails/` |
| SatelliteSO | 1 | `Assets/_Data/StarChart/Satellites/` |
| Projectile 根 Prefab（Matter / LaserBeam / EchoWave） | 3 | `Assets/_Data/StarChart/Prefabs/` |
| Modifier Prefab | 5 | `Assets/_Data/StarChart/Prefabs/` |
| Sail/Satellite Behavior Prefab | 2 | `Assets/_Prefabs/StarChart/` |
| Inventory 聚合 | 1 | `Assets/_Data/StarChart/PlayerInventory.asset` |

**PlayerInventory.asset 交叉验证结论**：21 个 SO 的 GUID 全部出现在 `_ownedItems` 数组中（8 Cores + 9 Prisms + 3 Sails + 1 Satellite = 21），数量吻合。

---

## 2. Prefab Registry（`Assets/_Data/StarChart/Prefabs/`）

| Prefab | GUID | Owner 工具 | 家族归属 | 用途 |
|--------|------|-----------|---------|------|
| `Projectile_Matter.prefab` | `178be1a279c3747f2b519ac5a7db13cc` | `Batch5AssetCreator` | Matter（兼 Anomaly 复用） | Matter 家族投射物根 Prefab；Anomaly 家族**当前复用此 Prefab** |
| `LaserBeam_Light.prefab` | `2016ff32cc94f433183601f0d25cabaf` | `Batch5AssetCreator` | Light | Light 家族激光投射物根 Prefab |
| `EchoWave_Echo.prefab` | `0e28c641f718246c99fb0dc2ee8079c7` | `Batch5AssetCreator` | Echo | Echo 家族脉冲波投射物根 Prefab |
| `Modifier_Boomerang.prefab` | `af692c9f108eb45f4ad105251164c12c` | `Batch5AssetCreator` | Anomaly / Matter（跨家族可用） | 回旋行为 Modifier（Anomaly Core + Sheba Prism 共用） |
| `Modifier_SlowOnHit.prefab` | `aece6f6e3911d4b65b22c2f9f4cc9806` | `Batch5AssetCreator` | Tint 家族 Prism | 命中减速 Modifier（`TintPrism_FrostSlow` 用） |
| `Modifier_Bounce.prefab` | `eb4f07e4275b54f1b8d239f37f7efb9f` | `Batch5AssetCreator` | Rheology / Sheba 跨用 | 弹射 Modifier（`RheologyPrism_Accelerate` + `ShebaP_Bounce` 共用） |
| `Modifier_Homing.prefab` | `4d498a9842fe0cc4699dc2df81aa82b6` | `ShebaAssetCreator` | Sheba | 追踪 Modifier（`ShebaP_Homing` 用） |
| `Modifier_MinePlacer.prefab` | `64f0f85b1799e6b418e4cb1aff6a915b` | `ShebaAssetCreator` | Sheba | 布雷 Modifier（`ShebaP_MinePlacer` 用） |

### 2.1 Behavior Prefab（`Assets/_Prefabs/StarChart/`）

| Prefab | GUID | Owner 工具 | 用途 |
|--------|------|-----------|------|
| `Sat_AutoTurret.prefab` | `eeb3eb2051630f34f806d14179451010` | `ShebaAssetCreator` | Satellite 自动炮台行为 Prefab（`ShebaSat_AutoTurret` 用） |
| `SpeedDamageSailBehavior.prefab` | `de0a1ce38e8edf848b028f7f76ed6e70` | `Unknown-Historical` | LightSail 加速伤害行为 Prefab（`ShebaSail_Scout` + `TestSpeedSail` 共用） |

**验证说明**：`SpeedDamageSailBehavior.prefab` 的创建时间戳为 `Feb 21 11:53`（早于 `ShebaAssetCreator` 的多数其他产物 `Mar 2 14:59`）。`ShebaAssetCreator` 代码仅 `LoadAssetAtPath<GameObject>(SpeedSailPrefabPath)` 读取此 prefab，不创建它——代码层面找不到生成者，归入 `Unknown-Historical`。

---

## 3. StarCoreSO Registry（`Assets/_Data/StarChart/Cores/`）

| 资产 | GUID | Owner 工具 | 家族 | DisplayName | Shape | 引用 Projectile Prefab | 引用 Anomaly Modifier |
|------|------|-----------|------|-------------|-------|------------------------|----------------------|
| `MatterCore_StandardBullet` | `8c64af7a7f4914bbc88726cf304ca7ef` | `Batch5AssetCreator` | Matter | "Standard Bullet" | 1x1 | `Projectile_Matter` | — |
| `LightCore_BasicLaser` | `f324bfb5bc456477bb700e503994f3ec` | `Batch5AssetCreator` | Light | "Basic Laser" | 1x1 | `LaserBeam_Light` | — |
| `EchoCore_BasicWave` | `e6b6d0f949472459e98ecccbcc0ea53a` | `Batch5AssetCreator` | Echo | "Basic Shockwave" | 2x1V | `EchoWave_Echo` | — |
| `AnomalyCore_Boomerang` | `c7c61dacd3bfb45b6ae14b6152ee3a8b` | `Batch5AssetCreator` | Anomaly | "Boomerang" | 1x1 | `Projectile_Matter` ⚠️ | `Modifier_Boomerang` |
| `ShebaCore_MachineGun` | `8d6ac5b36bcb6524ab64ff1529777c84` | `ShebaAssetCreator` | Matter | "Machine Gun" | 1x2H | `Projectile_Matter` | — |
| `ShebaCore_FocusLaser` | `e5cbb8575de192940935dc8ac81ee99a` | `ShebaAssetCreator` | Light | "Focus Laser" | 1x1 | `LaserBeam_Light` | — |
| `ShebaCore_Shotgun` | `8e120c424fc413a4db57127ce827d404` | `ShebaAssetCreator` | Matter | "Storm Scatter" | L | `Projectile_Matter` | — |
| `ShebaCore_PulseWave` | `4722638f16b3a7d4892ef19b5665ca73` | `ShebaAssetCreator` | Echo | "Pulse Wave" | 2x2 | `EchoWave_Echo` | — |

### 3.1 关键备注

- **⚠️ `AnomalyCore_Boomerang` 的 `_projectilePrefab` 复用 `Projectile_Matter.prefab`**（GUID `178be1a279c3747f2b519ac5a7db13cc` 交叉验证通过）。这是 Phase A 的既定事实，不是 bug。`StarChartController.SpawnAnomaly` 从 Matter 对象池获取 Projectile 实例，再 `InstantiateModifiers` 注入 `AnomalyCore.AnomalyModifierPrefab`（`Modifier_Boomerang.prefab`）构成完整行为。详见 `StarChart_CanonicalSpec.md` 第 3 节主链。
- **家族扩展关系**：`ShebaCore_MachineGun` / `ShebaCore_Shotgun` 虽然 DisplayName 不含 "Matter" 字样，但 `_family` 字段磁盘值 = `0` = `CoreFamily.Matter`（通过 `_projectilePrefab` 指向 `Projectile_Matter` 间接确认）。同理 `ShebaCore_PulseWave` 磁盘 `_family = 2 = CoreFamily.Echo`。
- **DisplayName 作为跨存档 ID**：8 个 Core 的 `_displayName` **在 Core 类型范围内互不重复**，满足 `StarChart_CanonicalSpec.md` 第 8 节的"同类型内全局唯一"约束。跨类型（Core vs Prism）存在重名（见 §4.1 `Boomerang`），当前存档方案允许，但未来若启用跨类型全局唯一强校验需同步修复。

### 3.2 `_shape` 枚举映射表

磁盘上 `_shape: N` 为 int，与 `ItemShape` 枚举对应如下：

| `_shape: N` | `ItemShape` 枚举名 | 文字记法 | 占格数 | 当前使用者 |
|---|---|---|---|---|
| 0 | `Shape1x1` | 1x1 | 1 | 多数 Core / Prism |
| 1 | `Shape1x2H` | 1x2H | 2 | `ShebaCore_MachineGun` |
| 2 | `Shape2x1V` | 2x1V | 2 | `EchoCore_BasicWave` / `ShebaP_Homing` |
| 3 | `ShapeL` | L | 3 | `ShebaCore_Shotgun` / `ShebaP_MinePlacer` |
| 4 | `ShapeLMirror` | L-Mirror | 3 | **当前无资产使用** |
| 5 | `Shape2x2` | 2x2 | 4 | `ShebaCore_PulseWave` |

**Legacy `_slotSize` 字段**：所有 SO 仍保留 `_slotSize` int 字段（值 1-4），但已**不参与任何运行时逻辑**，仅作为历史兼容字段存在（从 `_shape` 派生）。审计脚本与新代码**必须以 `_shape` 为准**，忽略 `_slotSize`。

**⚠️ 早期资产可能缺失 `_shape` 字段**：`RheologyPrism_Accelerate` / `TintPrism_FrostSlow` 等 Batch5 时期 asset YAML 中不写 `_shape:`（走默认值 `0 = Shape1x1`，与其 `_slotSize = 1` 一致），属于已知历史痕迹，不影响行为。新增资产必须显式写入 `_shape`。

---

## 4. PrismSO Registry（`Assets/_Data/StarChart/Prisms/`）

| 资产 | GUID | Owner 工具 | 家族（磁盘 `_family`） | DisplayName | Shape | 引用 Modifier Prefab |
|------|------|-----------|------------------------|-------------|-------|---------------------|
| `FractalPrism_TwinSplit` | `f814efbf637ba4c2785129f9e3e95da3` | `Batch5AssetCreator` | Fractal | "Twin Split" | 1x1 | — (数值型 Prism) |
| `RheologyPrism_Accelerate` | `69e931aea30dd4d56b0918745582df3a` | `Batch5AssetCreator` | Rheology | "Accelerate" | 1x1 | `Modifier_Bounce` |
| `TintPrism_FrostSlow` | `ac97e1b2088574045a4985b7acfac5bd` | `Batch5AssetCreator` | Tint | "Frost Slow" | 1x1 | `Modifier_SlowOnHit` |
| `ShebaP_TwinSplit` | `b53795b9860ae4f4992a1393f5e3068b` | `ShebaAssetCreator` | Fractal | "Twin Split" ⚠️ | 1x1 | — (数值型) |
| `ShebaP_RapidFire` | `6b6e85f4c761ac848bb3970f5680469a` | `ShebaAssetCreator` | Rheology | "Rapid Fire" | 1x1 | — (数值型) |
| `ShebaP_Bounce` | `a3bb00dd50f952546882190b62dbb342` | `ShebaAssetCreator` | Rheology | "Bounce" | 1x1 | `Modifier_Bounce` |
| `ShebaP_Homing` | `9cb868cc31216704db6f937621990354` | `ShebaAssetCreator` | **⚠️ 磁盘字段缺失 → 默认 `Fractal`；代码意图 `Tint`** | "Homing" | 2x1V | `Modifier_Homing` |
| `ShebaP_MinePlacer` | `79874b085ec4cbf4d8f4c9f94cbb8492` | `ShebaAssetCreator` | Fractal | "Mine Placer" | L | `Modifier_MinePlacer` |
| `ShebaP_Boomerang` | `c671e95a5d2602247bd20cacddde1759` | `ShebaAssetCreator` | Rheology | "Boomerang" ⚠️ | 1x1 | `Modifier_Boomerang` |

### 4.1 DisplayName 冲突风险

- **⚠️ `FractalPrism_TwinSplit` 与 `ShebaP_TwinSplit` 的 `_displayName` 均为 "Twin Split"**。
- **⚠️ `ShebaP_Boomerang` 的 `_displayName` 为 "Boomerang"，与 `AnomalyCore_Boomerang` 相同**——但 Core 和 Prism 是不同类型，在 `LoadoutSlotSaveData` 中分开存储（`PrimaryCoreName` vs `PrimaryPrismNames`），**当前存档方案下不会冲突**，但 UI 展示层应警惕重名体验。
- 若未来引入全局 DisplayName 唯一性强校验（见 `StarChart_CanonicalSpec.md` 第 8 节），**必须**先重命名上述冲突项。本文档记录此风险，不在本轮强制修正。

### 4.2 Modifier Prefab 引用关系

`SnapshotBuilder.CollectTintModifierPrefabs` 的双重过滤规则（`PrismFamily.Tint` + prefab 带 `IProjectileModifier` 组件）意味着：

- 只有 **Tint 家族且持有 Modifier prefab** 的 Prism 会在所有家族发射时生效。
- **按磁盘 `_family` 真值判定**的 Tint 通道生效清单（Phase A 实际状态）：

  | Prism | 磁盘 `_family` | 走 Tint 通道？ |
  |---|---|---|
  | `TintPrism_FrostSlow` | `3 = Tint` | ✅ 是 |
  | `ShebaP_Homing` | **字段缺失 → 默认 `0 = Fractal`** | ❌ **否**（因磁盘 bug，详见 §4.3） |
  | `ShebaP_MinePlacer` | `0 = Fractal` | ❌ 否 |
  | `ShebaP_Boomerang` | `1 = Rheology` | ❌ 否 |
  | `ShebaP_Bounce` | `1 = Rheology` | ❌ 否 |

- 换言之，**当前 Phase A 磁盘状态下，唯一通过 Tint 通道跨家族生效的 Prism 是 `TintPrism_FrostSlow`**。其余持有 Modifier Prefab 的 Sheba Prism 的行为注入路径为：
  - `ShebaP_Homing` / `ShebaP_MinePlacer`（`Family = Fractal`）：数值调制（`_statModifiers`）生效，但 Modifier Prefab **不会被 `CollectTintModifierPrefabs` 拾取**；当前仅在 `SnapshotBuilder.ApplyModifier` 的数值分支中起作用。
  - `ShebaP_Boomerang` / `ShebaP_Bounce`（`Family = Rheology`）：数值调制生效；`Modifier_Boomerang` / `Modifier_Bounce` 的行为注入依赖 Anomaly 分支的 `AnomalyModifierPrefab` 路径（`ShebaP_Boomerang` 未走该路径，因为 Boomerang 行为只有 `AnomalyCore_Boomerang` 作为 Core 时才被注入）。

### 4.3 ⚠️ `ShebaP_Homing` 磁盘 Family 字段缺失（已知资产异常，待单独修复）

**事实**：`Assets/_Data/StarChart/Prisms/ShebaP_Homing.asset` 的 YAML **不包含 `_family:` 字段**。Unity 反序列化时走 C# 枚举默认值 = `0` = `PrismFamily.Fractal`。

**代码意图**：`ShebaAssetCreator.cs:342` 在创建时写的是 `SetField(so, "_family", PrismFamily.Tint);`，推定早期版本该赋值不存在或未生效，且 `ShebaAssetCreator.CreateShebaP_Homing` 在检测到资产已存在时 skip 重写，使得磁盘值一直未被修正。

**后果**：
- `SnapshotBuilder.CollectTintModifierPrefabs` 不会拾取 `ShebaP_Homing._projectileModifierPrefab`。
- `ShebaP_Homing` 当前仅作为 `Fractal` 家族数值型 Prism 生效，其 `Modifier_Homing.prefab` 行为注入路径断链。
- UI 背包分类 / Family 过滤器若依赖 `_family` 判断，也会错误归类为 Fractal。

**处置方案（本轮不执行，待单独任务）**：
- **选项 A**（推荐）：在 asset YAML 中手动补 `_family: 3` 到 `_shape` 字段附近（Tint = 3）。
- **选项 B**：删除 `ShebaP_Homing.asset` 后重跑 `ShebaAssetCreator.CreateShebaP_Homing`，让代码重新落盘。
- **选项 C**：若决定 `ShebaP_Homing` 就应归类为 Fractal（`Modifier_Homing` 走别的注入路径），则更新代码意图而非磁盘值。

本文档记录此异常，Registry 侧**以磁盘真值为准**标注为 Fractal（见 §4 表格），修复后应同步更新 §4 / §4.2。

---

## 5. LightSailSO Registry（`Assets/_Data/StarChart/Sails/`）

| 资产 | GUID | Owner 工具 | DisplayName | 引用 Behavior Prefab |
|------|------|-----------|-------------|---------------------|
| `ShebaSail_Standard` | `ba2517607d269ec4094a0116f5cbf480` | `ShebaAssetCreator` | "Standard Sail" | — (纯 stats buff) |
| `ShebaSail_Scout` | `70056dfe798f2fd4b984424619825448` | `ShebaAssetCreator` | "Scout Sail" | `SpeedDamageSailBehavior.prefab` |
| `TestSpeedSail` | `c862ec4a6c35f4046bbe546b132ce3e3` | `Unknown-Historical` ⚠️ | "Speed Damage Sail" | `SpeedDamageSailBehavior.prefab` |

### 5.1 关键备注

- `ShebaSail_Standard._behaviorPrefab` 为 `{fileID: 0}`（null），表示**纯数值 buff 型 Sail**，无 behavior 组件参与 `ModifyProjectileParams`。
- ⚠️ **`TestSpeedSail` 不在任何 Editor 工具代码的生成逻辑中**。`ShebaAssetCreator` 仅通过 `LoadAssetAtPath` 读取其引用的 `SpeedDamageSailBehavior.prefab`，不创建 Sail 资产本身。`TestSpeedSail.asset.meta` 时间戳 `Feb 21 11:53`（最早）——推定为 Sail 系统早期手工创建的测试资产。
- ⚠️ **`TestSpeedSail` 与 `ShebaSail_Scout` 共用同一 `_behaviorPrefab`**（`SpeedDamageSailBehavior.prefab`）。两者数值参数可能不同（通过 SO 自身字段控制），但 behavior 实现类是同一份。
- **DisplayName 无冲突**：三者的 `_displayName` 互不重复。

---

## 6. SatelliteSO Registry（`Assets/_Data/StarChart/Satellites/`）

| 资产 | GUID | Owner 工具 | DisplayName | 引用 Behavior Prefab |
|------|------|-----------|-------------|---------------------|
| `ShebaSat_AutoTurret` | `786ffe36f065aec4ab61d77474b57b9a` | `ShebaAssetCreator` | "Auto Turret" | `Sat_AutoTurret.prefab` |

### 6.1 关键备注

- 仅 1 个 Satellite 资产，为 Phase A 基线最小集。
- Satellite 的 Per-Track 归属语义见 `StarChart_CanonicalSpec.md` 第 2 节。

---

## 7. Inventory 聚合资产

### 7.1 `PlayerInventory.asset`

- **路径**：`Assets/_Data/StarChart/PlayerInventory.asset`
- **GUID**：`f9cc10f93b27143acbf2dfd19f4c14e9`（asset 的 `m_Script` GUID 为 `02bdc0e80ad637a47a776bac64f88ad3`，指向 `StarChartInventorySO`）
- **Owner 工具**：`ShebaAssetCreator`（`CreateAllAssets` 末尾调用 `AppendToPlayerInventory` 聚合逻辑）
- **内容**：`_ownedItems` 数组聚合全部 21 个 SO 的 GUID 引用
- **用途**：玩家持有物品清单。`StarChartInventorySO.OwnedItems` 被 UI 背包面板和 Save/Load 系统读取（详见 `StarChart_CanonicalSpec.md` 第 4 节跨程序集边界）。
- **数组顺序无语义**：当前数组顺序是 `ShebaAssetCreator.AppendToPlayerInventory` 按 `FindAssets("t:StarCoreSO") → t:PrismSO → t:LightSailSO → t:SatelliteSO` 的类型搜索顺序 append 产生的历史痕迹，**没有任何排序语义**。UI 展示必须通过 `InventoryFilter` / DisplayName 自行排序，**禁止依赖 `_ownedItems[i]` 的数组索引**。
- **维护原则**：新增星图 SO 资产后，**必须手动（或通过 AssetCreator）追加到 `_ownedItems` 数组**，否则玩家存档无法识别该物品。

---

## 8. 资产新增流程（Canonical Path）

当需要新增一个 StarChart 部件时，**必须走唯一官方入口**：

### 8.1 新增 StarCore / Prism（当前最常见场景）

1. **扩展 `ShebaAssetCreator`** 或**创建新的 AssetCreator**（命名应反映所属阶段，如 `Phase7AssetCreator`），**不要修改 `Batch5AssetCreator`**（Legacy）。
2. 在新 Creator 中：
   - `ScriptableObject.CreateInstance<StarCoreSO>()` / `CreateInstance<PrismSO>()`
   - 设置 `_displayName`（**同类型全局唯一**，参考第 4.1 节冲突风险）、`_family`（`CoreFamily` / `PrismFamily` 枚举）、`_shape`、`_statModifiers`
   - 若为 Anomaly Core，设置 `_anomalyModifierPrefab`（指向 `Modifier_Xxx.prefab`）
   - 若为 Tint Prism 或需注入行为，设置 `_projectileModifierPrefab`
3. `AssetDatabase.CreateAsset(so, path)` 落盘到 `Assets/_Data/StarChart/Cores/` 或 `Prisms/`
4. **追加到 `PlayerInventory.asset._ownedItems`**（否则 Inventory UI / Save 无法识别）
5. 运行 `ProjectArk > Validate Shape Contract` 确认 `_shape` 字段合法

### 8.2 新增 LightSail / Satellite

1. 如需 behavior prefab：在 `Assets/_Prefabs/StarChart/` 创建 prefab（挂载 `LightSailBehavior` 或 `SatelliteBehavior` 子类组件）
2. `ScriptableObject.CreateInstance<LightSailSO>()` / `CreateInstance<SatelliteSO>()`
3. 设置 `_displayName`（全局唯一）、`_behaviorPrefab`（可为 null 表示纯 stats buff）、其余 stats 字段
4. 落盘到 `Assets/_Data/StarChart/Sails/` 或 `Satellites/`
5. 追加到 `PlayerInventory.asset._ownedItems`

### 8.3 新增 Modifier Prefab（最罕见）

1. 实现 `IProjectileModifier` 接口的新组件类（放在 `Assets/Scripts/Combat/StarChart/Modifiers/` 或对应家族目录）
2. 在新 AssetCreator 中创建 prefab 并挂载该组件
3. 落盘到 `Assets/_Data/StarChart/Prefabs/`
4. 由引用它的 Core / Prism SO 通过 `_projectileModifierPrefab` 或 `_anomalyModifierPrefab` 字段绑定

### 8.4 禁止事项

- **禁止手工在 Project 窗口右键创建 SO 资产**（会绕过 `PlayerInventory.asset` 的登记，也无法复现）。
- **禁止修改 `Batch5AssetCreator` 或 `ShebaAssetCreator`**（两者已标注 Legacy/Hidden，应作为历史归档保留）。
- **禁止复用既有 Core 的 `_displayName`**（Save/Load ID 冲突，见 `StarChart_CanonicalSpec.md` 第 8 节）。
- **禁止在 `Assets/_Data/StarChart/` 层级之外创建星图 SO 资产**（如意外放到 `Assets/_Prefabs/` 或 `Assets/_Data/` 根目录）。

---

## 9. 已废弃 / Unknown-Historical 清单

| 资产 / 工具 | 状态 | 处置建议 |
|------------|------|---------|
| `Batch5AssetCreator` | Legacy, Hidden from menu | 保留为历史归档；新增 SO 严禁调用此工具 |
| `ShebaAssetCreator` | Legacy, Hidden from menu | 保留为历史归档；新增 Sheba 以外的 SO 严禁扩展此工具 |
| `TestSpeedSail.asset` | Unknown-Historical | 若后续确认废弃则从 `PlayerInventory.asset._ownedItems` 移除并删除资产；否则追加到新 AssetCreator 的白名单并补说明 |
| `SpeedDamageSailBehavior.prefab` | Unknown-Historical | 同上，此 prefab 被 `ShebaSail_Scout` 和 `TestSpeedSail` 共用，处置需同步考虑两个消费者 |

---

## 10. Scene / Prefab 现役硬约束

以下关系是运行时主链的隐式依赖，**修改时必须同步更新**：

### 10.1 投射物对象池预热依赖

- `WeaponTrack.InitializePools` 对每个 `CoreFamily` 预热固定数量的 Projectile Prefab 实例。具体配额（详见 `StarChart_CanonicalSpec.md` §6.1）：
  - **Matter**：`MatterCore.ProjectilePrefab`（`Projectile_Matter.prefab`）× 20/50（初始/最大）
  - **Light**：`LightCore.ProjectilePrefab`（`LaserBeam_Light.prefab`）× 5/20
  - **Echo**：`EchoCore.ProjectilePrefab`（`EchoWave_Echo.prefab`）× 5/15
  - **Anomaly**：`AnomalyCore.ProjectilePrefab`（**当前为 `Projectile_Matter.prefab`**）× 10/30 + 额外预热 `AnomalyModifierPrefab`（如 `Modifier_Boomerang.prefab`）× 10/30
- **硬约束**：若替换任何家族的 `ProjectilePrefab`，必须在该 Prefab 根节点挂载对应组件：
  - Matter / Anomaly → `Projectile`
  - Light → `LaserBeam`
  - Echo → `EchoWave`
- 违反此约束会触发 `StarChartController.SpawnXxx` 的 fallback 到 Matter 分支，**且 fallback 前必须先归还 pool**（详见 CanonicalSpec 第 3.4 节）。

### 10.2 Sheba / Sail / Satellite Behavior Prefab 依赖

- `LightSailRunner` 读取 `LightSailSO._behaviorPrefab`，`Instantiate` 后调用 `LightSailBehavior.ModifyProjectileParams`。
- `SatelliteRunner` 读取 `SatelliteSO._behaviorPrefab`，挂载到 Per-Track 位置执行 `SatelliteBehavior` 逻辑。
- Behavior Prefab **必须挂载对应子类**（`LightSailBehavior` 或 `SatelliteBehavior`）组件，否则 Runner 静默跳过。

### 10.3 Inventory 一致性约束

- `PlayerInventory.asset._ownedItems` 数组中的所有 GUID 必须能解析到有效的 `StarChartItemSO` 子类资产。
- 若 SO 资产被删除但 Inventory 未清理，`AssetDatabase.LoadAssetAtPath` 返回 null，UI 面板将显示空位或报错。
- **新增 / 删除 SO 资产时必须同步维护此数组**。

---

## 11. 扫码复现方法（审计用）

若需要从磁盘重新复现本 Registry 内容：

### 11.1 列出所有 Prefab GUID

```bash
for f in Assets/_Data/StarChart/Prefabs/*.prefab.meta; do
  name=$(basename "$f" .prefab.meta)
  guid=$(grep -E '^guid:' "$f" | awk '{print $2}')
  echo "$name  $guid"
done
```

### 11.2 列出所有 Core / Prism / Sail / Sat 资产的引用

```bash
for f in Assets/_Data/StarChart/Cores/*.asset Assets/_Data/StarChart/Prisms/*.asset; do
  name=$(basename "$f" .asset)
  echo "=== $name ==="
  grep -E '_family:|_shape:|_slotSize:|_displayName:|_projectilePrefab|_projectileModifierPrefab|_anomalyModifierPrefab' "$f"
done
```

注意：字段名是 `_family`（不是 `_coreFamily` / `_prismFamily`）。`_slotSize` 为 legacy 字段，仅作审计参考。

### 11.3 验证 PlayerInventory 的 21 个 GUID 覆盖

```bash
# 导出 Inventory 的 GUID 列表
grep -E 'guid:' Assets/_Data/StarChart/PlayerInventory.asset | awk '{print $3}' | tr -d ',' | sort -u > /tmp/inv_guids.txt

# 导出所有 SO 的 GUID 列表
for d in Cores Prisms Sails Satellites; do
  for f in Assets/_Data/StarChart/$d/*.asset.meta; do
    grep -E '^guid:' "$f" | awk '{print $2}'
  done
done | sort -u > /tmp/so_guids.txt

# 对比差集
diff /tmp/inv_guids.txt /tmp/so_guids.txt
```

### 11.4 审计 Family 字段一致性（检测类似 ShebaP_Homing 的字段缺失）

```bash
# 列出所有 Core / Prism 的磁盘 _family 值；输出 "file family=N" 或 "file family="（后者表示字段缺失）
for f in Assets/_Data/StarChart/Cores/*.asset Assets/_Data/StarChart/Prisms/*.asset; do
  name=$(basename "$f" .asset)
  family=$(grep -E '^\s*_family:' "$f" | awk '{print $2}')
  echo "$name  family=$family"
done
```

**审计规则**：
- 若输出 `family=`（空值），说明 `_family` 字段缺失，实际行为走枚举默认值 `0`（Core = `Matter`，Prism = `Fractal`）。
- 应与 `ShebaAssetCreator.cs` / `Batch5AssetCreator.cs` 内的代码意图对比，发现不一致立即在 §4.3 / §3.1 补记录或安排修复。

---

## 12. Phase A 治理基线：Registry 侧收口

本轮 Registry 建立即代表：

1. **Owner 清晰**：每个资产都标注了唯一生成工具（或 `Unknown-Historical` 明确暴露）
2. **引用关系可追溯**：所有 prefab 引用均通过 GUID 交叉验证，禁止推测
3. **新增流程唯一**：第 8 节定义了唯一官方路径，禁止绕过
4. **废弃资产可见**：第 9 节保留了历史归档的透明记录

**后续 Phase 扩展时**：新增资产必须**同步更新本 Registry**，并在 ImplementationLog 中记录变更。若未更新 Registry，视为违反 `Implement_rules.md` 第 1.1 节"写入时机"约束。
