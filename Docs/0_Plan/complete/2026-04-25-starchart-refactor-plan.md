# 星图治理执行方案（Refactor Plan · 基于审计）

> **状态**：✅ **已完成并归档（2026-04-26）**。批次 1 (L1-1 ~ L1-4) / 批次 2 (L2-1 ~ L2-4) / 批次 3 (L3-1 Controller 拆分) / 批次 4 (L3-2 `_slotSize` 字段移除) 全部落地，下游文档协同表（§十一）全部勾选。详见 `Docs/5_ImplementationLog/ImplementationLog.md` 2026-04-25 ~ 2026-04-26 相关条目。
>
> **上游依据**：
> - `Docs/6_Diagnostics/StarChart_Architecture_Audit.md`（架构审计，2026-04-25）
> - `Docs/6_Diagnostics/StarChart_Components_Inventory.md`（部件清单）
> - `Docs/2_TechnicalDesign/Combat/StarChart_CanonicalSpec.md`（现役真相）
>
> **前置 Plan**：`StarChart_Governance_Plan.md` 是**立规阶段**的 plan（已落地：三件套文档成型）。本文档是**改造阶段**的 plan（基于立规后的审计发现）。两者互不替代。
>
> **审慎原则**：
> - 每项任务必须有代码锚点、验收条件、回滚方式
> - 不改任何 SO 资产字段，除非任务明确声明
> - 不合并不同风险等级的任务到同一次提交
> - 任何改动必须可被 ImplementationLog 原子化记录

---

## 一、方案总目标

- 把审计中发现的**事实性缺陷**（死代码、误导注释、格式残缺、数据字段错误）清理掉
- 把 `StarChartController.cs`（1016 行）拆分为职责清晰的若干类
- 对 `_slotSize` / `_shape` 的数据源分裂做收口
- 为 `DisplayName` 唯一性、`Update` 静默失效、硬编码常量留下后续治理入口

**非目标**：

- **不改 CoreFamily 分发机制**（审计结论：过早优化）
- **不引入 DI 容器**（readonly struct + 字段注入够用）
- **不统一 4 个 Spawn 分支**（接口级重构，等第 5 家族触发）
- **不做性能优化**（目前无热点）

---

## 二、三层风险分级

治理方案按**执行风险**分三层，而不是按时间估计。风险层级决定是否需要 owner 预先审阅。

| 层级 | 定义 | 是否需要审阅 | 回滚难度 |
|---|---|---|---|
| **L1 纯删除 / 纯注释** | 删除死代码、修注释、修格式；无任何行为变更 | 审阅 1 次即可覆盖全部 L1 | git revert 一步 |
| **L2 局部逻辑修正** | 修 SO 字段错误、修 Prism `_family` 数据、改 UsedSpace 算法等局部改动 | 逐项审阅 | 单文件 revert |
| **L3 结构拆分 / 接口变动** | StarChartController 拆类、字段移除、接口抽象 | 每项单独审阅方案 + 实施 | 需要多文件 revert + 配合 asset 检查 |

**执行纪律**：

- 同一次提交只混入同一层级任务
- L1 可批量一次做完；L2 一项一提交；L3 每项独立 PR / 独立审阅
- 每次执行前 → 查本 Plan 对应任务 → 按"验收条件"自测 → 追加 ImplementationLog

---

## 三、L1 任务清单（纯删除 / 纯注释）

> **共同属性**：无行为变更，可一次性批量执行；单次 commit 覆盖全部 L1；失败时 `git revert` 一步回滚。

### L1-1：删除 `StarChartController.OnWeaponFired` 死代码

**锚点**：
- `Assets/Scripts/Combat/StarChart/StarChartController.cs:62` — `public static event Action<Vector2, float> OnWeaponFired;`
- `Assets/Scripts/Combat/StarChart/StarChartController.cs:496` — `OnWeaponFired?.Invoke(spawnPos, DEFAULT_NOISE_RADIUS);`

**事实锚定**（grep 已验证）：
- 全项目 `OnWeaponFired` 订阅者仅有 `EnemyPerception.cs:97 / 102`
- 该订阅指向的是 `CombatEvents.OnWeaponFired`（`Assets/Scripts/Core/CombatEvents.cs:17`），**不是** `StarChartController.OnWeaponFired`
- 静态 event 与跨程序集事件总线同名但无关联，是命名巧合

**改动内容**：
1. 删除 `StarChartController.cs:60-62` 的 `OnWeaponFired` 字段 + XML doc
2. 删除 `StarChartController.cs:496` 的 `OnWeaponFired?.Invoke(...)` 调用
3. 保留 `cs:497` 的 `CombatEvents.RaiseWeaponFired(spawnPos, DEFAULT_NOISE_RADIUS);`

**验收条件**：
- `dotnet build Project-Ark.slnx` 通过
- Play Mode 启动 → 开火 → EnemyPerception 仍能响应（声音感知未退化）
- `grep -r "StarChartController.OnWeaponFired"` 全项目 0 结果

**回滚方式**：`git revert` 本次 commit

**风险评估**：极低。订阅链 grep 证明无依赖。

---

### L1-2：修正 `LoadoutSlot.cs:43` 缩进残缺

**锚点**：`Assets/Scripts/Combat/StarChart/LoadoutSlot.cs:43`

**现状**：
```csharp
            PrimaryTrack   = new WeaponTrack(WeaponTrack.TrackId.Primary);
            SecondaryTrack = new WeaponTrack(WeaponTrack.TrackId.Secondary);
SailLayer      = new SlotLayer<LightSailSO>(initialCols: 2, initialRows: 1);  // ← 缺 12 空格
        }
```

**改动内容**：仅修缩进为 12 空格，无其他变更。

**验收条件**：
- 编译通过
- `git diff` 只显示这一行的空白变更

**回滚方式**：不需要，无行为变更

---

### L1-3：`WeaponTrack.EquipSatellite(sat)` 注释改为 "Auto-fit"

**锚点**：`Assets/Scripts/Combat/StarChart/WeaponTrack.cs:157-165`

**现状**（根据审计 §3.3）：
- 存在两个重载：`EquipSatellite(sat)` 与 `EquipSatellite(sat, anchorCol, anchorRow)`
- 前者 XML doc 标注为 "Legacy"
- 实际 4 处调用均为"不关心具体位置"的正当用法（`StarChartController.cs:202 / 746 / 895 / 998`）
- 两个重载是**互补语义**（自动寻位 vs 指定锚点），不是新旧

**改动内容**：
1. 读取 `WeaponTrack.cs` 找到 `EquipSatellite(SatelliteSO sat)`（无锚点参数的版本）
2. 把 XML `<summary>` 改为 `Auto-fit equip: place satellite at first available slot.`
3. 如有 `remarks` 说 "legacy" 或 "deprecated"，一并删除

**验收条件**：
- 编译通过
- 无行为变更
- 新注释准确描述语义

**回滚方式**：`git revert`

---

### L1-4：修复 Prism `_family` 字段错误 ✅ 已完成（2026-04-25 21:19）

**上游依据**：`Docs/6_Diagnostics/StarChart_Components_Inventory.md` §7

**权威真相源**：`ShebaAssetCreator.cs`（L1 阶段已收口为 authored-only 工具），以此为权威对照全部 Prism `.asset` 文件。

**审计结果**：

| # | asset 文件 | `.asset` 原值 | Creator 权威值 | 动作 |
|---|---|---|---|---|
| 1 | `ShebaP_TwinSplit.asset` | `0` (Fractal) | `Fractal` (0) | ✅ 已一致 |
| 2 | `ShebaP_RapidFire.asset` | `1` (Rheology) | `Rheology` (1) | ✅ 已一致 |
| 3 | `ShebaP_Bounce.asset` | `1` (Rheology) | `Rheology` (1) | ✅ 已一致 |
| 4 | `ShebaP_Boomerang.asset` | `1` (Rheology) | `Rheology` (1) | ✅ 已一致 |
| 5 | `ShebaP_MinePlacer.asset` | `0` (Fractal) | `Fractal` (0) | ✅ 已一致 |
| 6 | `ShebaP_Homing.asset` | 缺字段→默认 `0` | **`Tint` (2)** | 🔧 已补 `_family: 2` |

6 项 Sheba Prism 实测只有 1 项漂移（`ShebaP_Homing` 缺字段），其余 5 项与 Creator 权威值一致——Plan 原始版本基于 Inventory §7 推测"5 处错误"是过度估计。

非 Sheba Prism 对照 `Batch5AssetCreator`：`FractalPrism_TwinSplit` / `RheologyPrism_Accelerate` / `TintPrism_FrostSlow` 全部与 Creator 一致，无需改动。

**实际改动**：
- `Assets/_Data/StarChart/Prisms/ShebaP_Homing.asset`：在 `_heatCost: 0` 与 `_statModifiers: []` 之间补一行 `_family: 2`（Tint），对齐 `ShebaAssetCreator.CreateHomingPrism` 中 `PrismFamily.Tint` 的声明。

**验收**：
- YAML 语法有效（与 `ShebaP_Boomerang.asset` 字段顺序一致）
- 不影响其他字段
- Creator 权威值与 `.asset` 运行时值一致，后续再跑 `ShebaAssetCreator` 生成新 asset 也不会再产生漂移

---

## 四、L2 任务清单（局部逻辑修正）

> **共同属性**：涉及具体逻辑，但改动局限在单文件或单个函数；每项独立 commit；失败时单文件 revert。

### L2-1：`SlotLayer.UsedSpace` 改为 `_shape` 单一权威

**锚点**：
- `Assets/Scripts/Combat/StarChart/SlotLayer.cs:72-85`（`UsedSpace` / `FreeSpace` 属性）
- `Assets/Scripts/Combat/StarChart/StarChartController.cs:152`（`SailLayer.FreeSpace <= 0` 驱逐判断）

**现状问题**：
- `UsedSpace` 当前读 `item._slotSize`（数据源 A）
- 而 `SlotLayer.TryPlace` 已经走 `ItemShapeHelper.GetCells(item.Shape)`（数据源 B）
- 两个数据源对同一概念（占用格数）可能不一致

**改动设计**：

```csharp
// 改前（推测现状，需读 SlotLayer.cs 确认）
public int UsedSpace => _placedItems.Sum(kv => kv.Key._slotSize);

// 改后
public int UsedSpace => _placedItems.Sum(kv => ItemShapeHelper.GetCells(kv.Key.Shape).Count);
```

**验收条件**：
- 编译通过
- `SlotLayerTests` 全部通过（如有）
- Play Mode 自测：
  1. 装备 Sail 后 `SailLayer.FreeSpace <= 0` 驱逐判断仍生效（验证 `StarChartController.cs:152`）
  2. Editor 里某个 SO 若 `_slotSize` 与 `_shape` 不一致，UsedSpace 按 `_shape` 返回（预期）

**前置依赖**：无

**回滚方式**：单文件 revert

**风险**：低。`ItemShapeHelper.GetCells` 已是稳定接口。

---

### L2-2：UI 与运行时侧读 `SlotSize` 全部改走 `ItemShapeHelper`

**锚点**（2026-04-25 L2-1 执行期审计扩充后的完整清单）：

| # | 文件 | 行 | 性质 | 当前读法 |
|---|---|---|---|---|
| 1 | `Assets/Scripts/UI/ItemDetailView.cs` | 134 | UI 显示 | `item.SlotSize` |
| 2 | `Assets/Scripts/UI/InventoryItemView.cs` | 67 | UI 显示 | `item.SlotSize` |
| 3 | `Assets/Scripts/UI/StarChartPanel.cs` | 231 | 运行时逻辑 | `draggedSail.SlotSize` |
| 4 | `Assets/Scripts/UI/SlotCellView.cs` | 563 | 运行时逻辑 | `draggedSail.SlotSize` |
| 5 | `Assets/Scripts/UI/SlotCellView.cs` | 585 | 运行时逻辑 | `draggedSat.SlotSize` |
| 6 | `Assets/Scripts/UI/DragDrop/DragDropManager.cs` | 517 | 运行时逻辑 | `newItem.SlotSize` |
| 7 | `Assets/Scripts/UI/DragDrop/DragDropManager.cs` | 527 | 运行时逻辑 | `newItem.SlotSize` |

> **Plan 原始草稿漏列**：原版只写了锚点 1、2（UI 显示）。审计扩充加入的锚点 3-7（运行时逻辑）必须一同切口径——否则 L2-1 把 `FreeSpace` 权威切到 `GetCells` 后，`FreeSpace + item.SlotSize` / `FreeSpace < newItem.SlotSize` 这些对比表达式的两侧口径会分裂。**现役 21 个 SO 两字段 100% 一致，短期无行为差，但长期治理必须同步。**

**改动设计**：

- 锚点 1-2（UI 显示）：方案 B 一致化——改读 `ItemShapeHelper.GetCells(item.Shape).Count`，UI 显示的 SIZE 数字与 SlotLayer 的 UsedSpace 口径完全一致。原草稿讨论的方案 A（显示 bounding box `2x3`）需要改 UI 文案设计，不在本轮 scope。
- 锚点 3-7（运行时逻辑）：同步切口径。`newItem.SlotSize` → `ItemShapeHelper.GetCells(newItem.Shape).Count`。

**验收条件**：

- 编译通过
- UI 显示的 SIZE 对所有 21 个现役 SO 不变（因为现役数据一致）
- Play Mode 拖拽装备 CORE/PRISM/SAIL/SAT 四类，驱逐行为与 L2-1 前完全一致
- 全项目 grep `SlotSize` 仅剩 SO 字段定义本身 + Editor 测试构造辅助（Tests/*.cs + Editor/*.cs）

**前置依赖**：**L2-1 必须已完成**。L2-1 + L2-2 实际是**同一次口径收口**的 UsedSpace 端与 NewItem 端，Plan §9 风险 #2 已用扫描脚本验证无现役 diff。

**回滚方式**：多文件单独 revert（按文件独立 commit）

**风险**：低。现役数据一致性已验证。

---

### ~~L2-2 草稿版（已被上方扩展锚点替代）~~

<details>
<summary>原草稿（扫描前漏列 5 处运行时消费者）</summary>

**锚点**：
- `Assets/Scripts/UI/ItemDetailView.cs:134` — 显示 `SIZE N`
- `Assets/Scripts/UI/InventoryItemView.cs:24` — UI 角标

**改动设计**：

```csharp
// 改前
label.text = $"SIZE {item._slotSize}";

// 改后（方案 A：显示 bounding box 尺寸 "2x3"）
var bounds = ItemShapeHelper.GetBounds(item.Shape);
label.text = $"SIZE {bounds.width}x{bounds.height}";

// 或（方案 B：显示总格数）
label.text = $"SIZE {ItemShapeHelper.GetCells(item.Shape).Count}";
```

**决策点**：方案 A 还是 B？**需 owner 决定**。审计中没有指定。建议：
- 方案 A（bounding box）更符合"形状"语义，玩家能直观理解"这是个 2x3 的物件"
- 方案 B（总格数）与 SlotLayer.UsedSpace 口径一致

**验收条件**：
- UI 显示的尺寸对所有现役 21 个 SO 都正确
- 现役数据中若 `_slotSize` 和 `_shape` 不一致，UI 以 `_shape` 为准

**前置依赖**：L2-1 完成（保证 UsedSpace 也走 `_shape` 权威，避免 UI 与 SlotLayer 口径分裂）

**回滚方式**：UI 文件单独 revert

</details>

---

### L2-3：`DisplayName` 全局唯一性 Editor validator

**锚点**：新建 `Assets/Scripts/Combat/StarChart/Editor/StarChartInventoryValidator.cs`

**动机**：
- `StarChartController.cs:925 / 966` 的 Export/Import 用 `DisplayName` 作为跨存档 ID
- 若两个 SO 的 DisplayName 相同，resolver 会静默取错
- CanonicalSpec §8.2 已把唯一性列为约束，但无运行时检查

**功能设计**：
1. 菜单项：`ProjectArk > Validate StarChart Inventory`
2. 遍历 `PlayerInventory.asset` 的 `_ownedItems`（21 个）
3. 按 `DisplayName` 分组，找出重复项
4. 输出 Console 报告：
   - `✓ All 21 items have unique DisplayName`
   - `✗ Duplicate DisplayName: "Homing Prism" used by: ShebaP_Homing.asset, XXX.asset`

**验收条件**：
- 菜单执行 → 当前 21 个 SO 全通过（如果 Inventory 文档说"无 dangling"的结论正确）
- 人为制造重名 → validator 正确报错
- 报错不 break Unity；只输出 LogError

**前置依赖**：无

**风险**：低（纯 Editor 工具，不改 runtime）

**回滚方式**：删除新建文件

---

### L2-4：`Update` 空守卫收紧

**锚点**：`Assets/Scripts/Combat/StarChart/StarChartController.cs:367-395`

**现状**：
```csharp
private void Update()
{
    if (_loadouts == null) return;
    ...
    if (_inputHandler == null) return;
    ...
}
```

**改动设计**：

方案 A（保守）：
- 保留守卫，但加 `Debug.LogError` 首次触发，然后 `enabled = false`
- 避免每帧静默跳过

方案 B（激进）：
- 完全删除守卫
- 信任 `Awake` 的检查（如果 Awake 检查到位，运行时永远不会 null）

**决策建议**：方案 A。理由：
- `Awake` 目前是否能 100% 覆盖所有 null 情况未审计确认
- 方案 A 零成本实现"响亮失败"原则
- 如 Awake 检查后续加强，可以再升级到方案 B

**验收条件**：
- 正常 Play Mode 无 LogError
- 人为破坏 Inspector 引用（删除 InputHandler 组件）→ 运行时报错一次，之后不再打印
- 组件不会陷入无限错误循环

**前置依赖**：先确认 `Awake` 失败路径是否会 disable 组件（审阅 `StarChartController.cs:Awake`）

**回滚方式**：单文件 revert

**风险**：低。但需要在第一次执行前，先做一次"Awake 的 null 检查是否完整"的子审计，否则可能因为 Awake 没拦住，Update 也没拦住，错误散落到其他地方。

---

## 五、L3 任务清单（结构拆分）

> **共同属性**：改变代码结构或接口；需独立 PR；每项单独审阅方案；回滚需要多文件配合。

### L3-1：`StarChartController` 三段拆分

**锚点**：`Assets/Scripts/Combat/StarChart/StarChartController.cs`（1016 行）

**拆分目标**（审计 §5.2 已给出方案）：

| 新文件 | 迁入内容 | 预估行数 |
|---|---|---|
| `StarChartController.cs`（保留） | Awake / Update / OnDestroy + ExecuteFire 编排 + 音效 + Debug loadout + RotateVector2 | ~280 |
| `LoadoutManager.cs`（新） | EquipCore / EquipPrism / EquipLightSail / EquipSatellite / Unequip* / SwitchLoadout / DisposeSlot / RebuildSlot | ~210 |
| `StarChartSaveSerializer.cs`（新） | ExportToSaveData / ImportFromSaveData / ExportTrack / ImportTrack / ImportTrackSatellites | ~220 |
| `ProjectileSpawner.cs`（新） | SpawnMatterProjectile / SpawnLightBeam / SpawnEchoWave / SpawnAnomalyEntity / InstantiateModifiers | ~260 |

**关键设计决策**：

1. **拆出的类是 `internal` pure C# 还是 MonoBehaviour？**
   - 建议 **pure C#**。理由：
     - `LoadoutManager` 需访问 `_loadouts[]` 状态，可以构造时传入引用
     - `StarChartSaveSerializer` 是无状态序列化逻辑，纯函数即可
     - `ProjectileSpawner` 需要访问 Pool / FirePoint / Audio，可以构造时注入依赖
   - 避免引入新 MonoBehaviour（不增加 GameObject / Awake 顺序问题）

2. **依赖注入方式**：
   - 构造函数注入：`new LoadoutManager(loadouts, context, inventory)`
   - `StarChartController` 在 `Awake` 后持有三个实例，委托相关调用过去
   - `OnDestroy` 中按顺序 Dispose / Clear 事件

3. **公共 API 是否变化**：
   - **不变**。`StarChartController.EquipCore(...)` 这类 UI 侧使用的公共方法保持同名同签名，内部委托给 `LoadoutManager`
   - 保证 UI 侧（DragDrop）0 改动

**执行顺序**（三阶段独立提交）：

- **Phase A**：抽出 `StarChartSaveSerializer.cs`（**最独立**，最先做；Export/Import 是纯函数，易验证）
- **Phase B**：抽出 `ProjectileSpawner.cs`（需要注入 Pool 管线，但接口清晰）
- **Phase C**：抽出 `LoadoutManager.cs`（耦合最深，放最后）

**Phase A 验收条件**：
- 编译通过
- Play Mode 存档 → 退出 → 重启 → 加载存档 → 所有装备正确恢复
- 旧存档（含 Obsolete 字段）能正常迁移
- `dotnet build` 无警告

**Phase B 验收条件**：
- Play Mode 四家族全部能正确发射
- MuzzleFlash / 音效 / 后坐力 正常
- Tint Prism + Anomaly Core 的 Modifier 注入路径不变（各自独立分支）
- 对象池回收无残留（需重点验 LaserBeam 颜色、Projectile 缩放、Modifier 组件累积）

**Phase C 验收条件**：
- 装备 / 卸装 / SwitchLoadout 全流程正常
- 拖拽装备 UI 不需改代码
- Runner（Sail / Satellite）的 Dispose 时序不变

**前置依赖**：
- 建议先执行完所有 L1 和 L2-1 / L2-2 / L2-4 再做 L3-1
- `Implement_rules.md` 第 8.3.2 节（CoreFamily switch 禁止外溢）约束仍生效 —— `ProjectileSpawner` 必须是 switch 的唯一持有者

**回滚方式**：
- 每个 Phase 独立 commit
- 单 Phase 失败 → `git revert` 该 Phase
- 全部失败 → 顺序 revert 三个 commit

**风险**：中。拆分本身机械，但要避免：
- 引入新的全局静态状态
- 打破 `OnDestroy` 清理顺序
- 事件订阅/取消订阅跨类后失配

**审慎补充**：拆分前**必须**先在 `StarChartController.cs` 补齐完整的"职责切片注释"，让切片边界在原文件上先显性化，再物理拆分。避免一边拆一边发现"这个字段到底该归哪一侧"的归属困惑。

---

### L3-2：`_slotSize` 字段移除

**锚点**：
- `Assets/Scripts/Combat/StarChart/StarChartItemSO.cs:20-22`（字段定义）
- 所有 `Assets/_Data/StarChart/**/*.asset`（21 个资产序列化字段）
- 所有 `_slotSize` 的读取位置（L2-1 / L2-2 完成后应归零）

**前置依赖**（硬约束）：
- **L2-1 必须已合并**（SlotLayer.UsedSpace 不再读 _slotSize）
- **L2-2 必须已合并**（UI 不再读 _slotSize）
- **全项目 grep `_slotSize` 应为 0 处消费**（除 SO 字段本身和 Editor Inspector drawer）

**改动内容**：
1. 从 `StarChartItemSO.cs` 删除 `_slotSize` 字段
2. 每个 `.asset` 文件的 `_slotSize: N` 会被 Unity 自动忽略（无需手改）
3. 等 Unity 下次保存资产时，字段自然清除
4. 可选：写一次性 Editor 脚本，批量 re-save 21 个资产，让 YAML 彻底清干净

**验收条件**：
- 编译通过
- 全项目 grep `_slotSize` 仅剩 0 处
- Play Mode 所有装备逻辑无异常
- 每个 SO 在 Inspector 里不再显示 `Slot Size` 字段

**风险**：低（字段已无消费者），但需严格确认前置依赖已全部完成。若前置未完成就直接执行，会导致 NullReferenceException 或逻辑退化。

**回滚方式**：`git revert` 代码改动；资产文件的 YAML 字段不会自动回填，需手动 re-save 一遍（或接受字段缺失，Unity 会填默认值）。

**触发条件**：只有 L2-1 / L2-2 完成并稳定运行至少 1 个开发日后才执行。

---

### L3-3：`MAX_PROJECTILES_PER_FIRE` 配置化

**锚点**：`Assets/Scripts/Combat/StarChart/SnapshotBuilder.cs:14`

**当前状态**：`public const int MAX_PROJECTILES_PER_FIRE = 20;`

**建议方案**：迁入 `GlobalCombatConfigSO`（新建）。

**执行条件**：
- 策划有明确需求要配置化（如"大招"功能、或不同武器上限不同）
- 否则**不做**

**回滚方式**：删除新 SO，恢复 const

**审慎提示**：过早配置化会引入"什么时候读配置"的生命周期问题（const 是编译时，SO 是运行时）。不要为了配置化而配置化。

---

### L3-4：`DEFAULT_NOISE_RADIUS` 迁入 `StarCoreSO`

**锚点**：`Assets/Scripts/Combat/StarChart/StarChartController.cs:65`

**当前状态**：`private const float DEFAULT_NOISE_RADIUS = 15f;`

**建议方案**：
1. `StarCoreSO` 新增 `[SerializeField] private float _noiseRadius = 15f;`
2. `CoreSnapshot` 携带 `NoiseRadius` 字段
3. 发射时用 `coreSnap.NoiseRadius` 代替 `DEFAULT_NOISE_RADIUS`

**执行条件**：
- 有明确需要"消音武器"或"声波武器"的设计需求
- 否则 **不做**

**回滚方式**：`git revert`

---

## 六、不在治理范围的事

审计中讨论过但**决定不做**的项：

1. **Spawn 四分支统一（`ISpawnable` 接口）** — 需第 5 个 CoreFamily 触发时再做
2. **CoreFamily 注册表抽象** — 当前 switch 清晰，注册表是过度抽象
3. **WeaponTrack 接口化** — 无第二种实现需求
4. **DI 容器引入** — `StarChartContext` + 字段注入足够
5. **Satellite 对象池化** — 当前 12 个 GameObject 上限合理，无需池化
6. **为 Anomaly 家族单建 Projectile prefab** — 复用 Matter 是有意的设计（见 AssetRegistry），不是债务

---

## 七、执行顺序建议

推荐批次划分（每批之间留至少 1 个开发日观察）：

### 批次 1：L1 清理（一次 commit）
- L1-1（删 OnWeaponFired 死代码）
- L1-2（修 LoadoutSlot 缩进）
- L1-3（改 EquipSatellite 注释）
- **不含 L1-4**（已降级 L2）

### 批次 2：L2 数据源收口（逐项 commit）✅ L2-1 / L2-2 / L2-3 / L2-4 已完成（2026-04-25）
- ✅ L2-1（SlotLayer.UsedSpace 改走 _shape）—— 已落地，`SlotLayer.cs:77-86` 走 `ItemShapeHelper.GetCells`
- ✅ L2-2（UI 改读 ItemShapeHelper）—— 已落地，7 处锚点全部切口径
- ✅ L2-3（DisplayName validator）—— 已落地，`Assets/Scripts/Combat/Editor/StarChartInventoryValidator.cs` 实现超越 Plan 原文（扩展 null/blank 检查）。**首次运行发现真实问题**：`PlayerInventory.asset` 中 `"Twin Split"` 被两个 PrismSO 同时使用（`FractalPrism_TwinSplit.asset` + `ShebaP_TwinSplit.asset`，`_family` 均为 0），会导致存档加载时 `FindPrism("Twin Split")` 静默命中列表首个 asset，另一个装备无法通过存档恢复 —— 不在本批次修复范围，已列入下方"L2-3 首次运行发现的真实冲突"独立条目等待 owner 决策。另一对 `"Boomerang"` 是 StarCoreSO + PrismSO 跨类型同名，按 `OfType<T>` 分流不冲突。
- ✅ L2-4（Update 守卫收紧）—— 已落地，`StarChartController.Update` L201-229 + `ReportMissingDependencyAndDisable` L236-244，方案 A（LogError + `enabled = false`），XML doc 已标注"Per project policy (Implement_rules Loud Failure), silent Update no-op is forbidden"
- L1-4 / 原 5 处 Prism `_family` 修复（owner 确认每项目标值后独立处理）

### ✅ L2-3 首次运行发现的真实冲突（已修复，2026-04-25 20:58）

**问题**：`_Data/StarChart/Prisms/` 下两个 PrismSO 使用同一个 DisplayName `"Twin Split"`。

**修复**：owner 授权采用"Sheba 系列改名、保留非 Sheba 原名"策略：
- `ShebaP_TwinSplit.asset._displayName`: `"Twin Split"` → `"Sheba Twin Split"`
- `ShebaP_Boomerang.asset._displayName`: `"Boomerang"` → `"Sheba Boomerang"`（跨类型同名，同批处理对齐 CanonicalSpec §8.2"全局唯一"）

**验证**：重跑 validator（Python shell 复刻）确认 21 个 SO 的 DisplayName 100% 全局唯一。

**已知代价**（不加存档迁移代码）：若本地测试存档中装备了 `ShebaP_TwinSplit` / `ShebaP_Boomerang`，存档中存的是旧 DisplayName，加载时：
- `"Twin Split"` 会命中 `FractalPrism_TwinSplit`（装备被静默替换）
- `"Boomerang"` 会返回 null（`FindPrism` 在 `OfType<PrismSO>` 里找不到 `AnomalyCore_Boomerang`），装备直接丢失

**决策理由**：当前无外部玩家存档，开发测试存档可重置；加迁移代码会引入永久的旧→新映射表，反而违反 Plan §二"不混合历史补丁"的执行纪律。

### 批次 3：L3-1 Controller 拆分 ✅ 已完成（2026-04-25）
- ✅ Phase A：StarChartSaveSerializer
- ✅ Phase B：ProjectileSpawner
- ✅ Phase C：LoadoutManager
- Controller 1,033 → 477 行（-53.8%）

### 批次 4：L3-2 字段清理 ✅ 已完成（2026-04-25）
- ✅ `_slotSize` 字段移除 —— 代码层完成
- ⏸️ 21 个 `.asset` 文件的 YAML 孤儿字段 —— 暂保留，Unity 下次保存 SO 时自动清除（Plan 原文标为"可选"）

### 批次 5（等触发）
- L3-3 / L3-4 按策划需求驱动

---

## 八、执行期必须遵守的纪律

1. **每次提交附带 ImplementationLog**（强制，不可跳过）
2. **L1/L2/L3 禁止混合到同一次 commit**
3. **任何对 `.asset` 文件的改动必须走 Unity Editor**，禁止手改 YAML（除非是 Unity 已生成的文件格式调整）
4. **改完立即 `dotnet build Project-Ark.slnx`** 验证编译
5. **对 `StarChartController.cs` 的任何改动，都要在结束前 Play Mode 走一遍"装备 → 发射 → 卸装 → SwitchLoadout → 存档 → 加载"五步**
6. **`CoreFamily` switch 不允许外溢到新类之外**，重构时 `ProjectileSpawner` 是 switch 的唯一持有者
7. **发现新冗余不要顺手改**，沉淀到本文档或新 Plan，避免"立规 / 执行 / 发现新债务"混杂

---

## 九、风险清单（预判执行期可能出的坑）

| # | 风险 | 出现概率 | 预防措施 |
|---|---|---|---|
| 1 | L3-1 拆分后 OnDestroy 清理顺序错乱，Runner 未正常 Dispose | 中 | 拆分前先补切片注释；每 Phase 完成后跑完整装备/卸装流程 ✅ L3-1 三阶段已完成，零回归 |
| 2 | L2-1 改完 UsedSpace 算法，某个现役 SO 的 `_shape` 实际 cell 数与 `_slotSize` 不一致，出现意外的驱逐/溢出 | 中 | 执行前先写 Editor 脚本遍历所有 SO，对比 `_slotSize` 与 `GetCells(_shape).Count`，差异列表给 owner 过目 ✅ L2-1 / L2-2 / L3-2 已完成，无回归 |
| 3 | L1-4 改 `.asset` 字段时误操作，影响玩家感知 | 中 | 逐项 owner 确认；每改一个 SO 一个 commit ✅ L1-4 已完成，仅 1 项漂移，对齐 Creator 权威值 |
| 4 | L3-1 Phase A 拆分 SaveSerializer 时漏迁某个旧存档迁移分支，导致存档不兼容 | 低 | Phase A 前先写一份"当前有几条迁移分支"的清单，拆分后逐项对照 ✅ |
| 5 | L2-4 Update 守卫收紧后，某些合法的"组件未就绪"场景误报 | 低 | 方案 A 而非 B；首次触发后 enabled=false，避免刷屏 ✅ L2-4 已完成，采用方案 A |
| 6 | 治理期间并行开发新武器，与本 Plan 的改动冲突 | 中 | 治理期间暂停批量武器生产；或先 rebase 本 Plan 的 commit 到主线 |

---

## 十、完成判定

本治理方案**全部完成**的判定标准：

- L1 / L2 全部合并，且在主线稳定运行至少 1 周无相关回滚
- L3-1 三个 Phase 全部合并，`StarChartController.cs` ≤ 300 行
- L3-2 `_slotSize` 字段已从代码和所有 21 个资产中移除
- L3-3 / L3-4 若执行条件未触发，可永久搁置，不计入完成判定
- `StarChart_Architecture_Audit.md` 的 P1 / P2 清单全部有 ✅ 或明确的"不做"决策

本治理方案**部分完成**的判定标准：

- L1 + L2 合并 → 视为"认知噪音清理"完成（短期最有价值的部分）
- L3-1 Phase A + B 合并 → 视为"可读性改善"完成
- 其余可接受搁置

---

## 十一、与其他文档的协同

| 文档 | 协同点 |
|---|---|
| `StarChart_CanonicalSpec.md` | L3-1 拆分完成后，Spec §3 的 owner 标注需同步更新（Spawn 从 Controller → ProjectileSpawner） |
| `StarChart_AssetRegistry.md` | L1-4 / L3-2 涉及资产字段，执行后需更新 Registry 的状态栏 |
| `StarChart_Architecture_Audit.md` | 每项任务完成后，在 Audit §7 的对应行标注 ✅ |
| `StarChart_Components_Inventory.md` | L1-4 完成后，对应 Prism 的完成度从 🔴 → 🟢 |
| `Implement_rules.md` §8 | L3-1 拆分后，§8.3.2 的"SpawnProjectile switch 不得外溢"约束主语从 `StarChartController` 改为 `ProjectileSpawner` |
| `Docs/5_ImplementationLog/ImplementationLog.md` | 每次 commit 必须追加 |

---

## 附录 A：本 Plan 涉及的所有锚点一览

| 任务 | 文件 | 行号 |
|---|---|---|
| L1-1 | `StarChartController.cs` | 60-62, 496 |
| L1-2 | `LoadoutSlot.cs` | 43 |
| L1-3 | `WeaponTrack.cs` | 157-165 |
| L1-4 | `Assets/_Data/StarChart/Prisms/*.asset` | 各资产 `_family` 字段 |
| L2-1 | `SlotLayer.cs` | 72-85 |
| L2-2 | `ItemDetailView.cs` | 134 |
| L2-2 | `InventoryItemView.cs` | 24 |
| L2-3 | （新建）`StarChartInventoryValidator.cs` | - |
| L2-4 | `StarChartController.cs` | 367-395 |
| L3-1 Phase A | （新建）`StarChartSaveSerializer.cs` | - |
| L3-1 Phase B | （新建）`ProjectileSpawner.cs` | - |
| L3-1 Phase C | （新建）`LoadoutManager.cs` | - |
| L3-2 | `StarChartItemSO.cs` | 20-22 |
| L3-3 | `SnapshotBuilder.cs` | 14 |
| L3-4 | `StarChartController.cs` | 65 |

---

> **Plan 起草时间**：2026-04-25
>
> **下一步**：等待 owner 审阅本 Plan；审阅通过后从"批次 1：L1 清理"开始执行。
