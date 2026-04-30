# StarChart 数据管线实施计划（Data Pipeline Plan）

> **⚠️ 修订历史**
>
> | 日期 | 版本 | 变更 |
> |---|---|---|
> | 2026-04-27 14:58 | v1.0 | 初版（7 个 Phase，套餐 C，16-22h） |
> | 2026-04-27 15:10 | v1.1 | **红队审查修正为 B+ 方案**：<br>① 删除原 Phase 6（反向导出 Exporter）<br>② 改写原 Phase 3（Registry）：用 `AssetDatabase + StarChartManifest.asset` 替代 `Resources.LoadAll`，无需 SO 搬家<br>③ 总工时：原 16-22h → **11-14h** |
> | 2026-04-27 16:10 | **v1.2（当前）** | **代码对齐修正**：<br>① Phase 1 新增 "Phase 0 预置：老 SO 重命名"（~10min，为 `{InternalName}.asset` 命名统一铺路）<br>② Phase 1 任务清单明确 Archetype 可用集（仅 `Straight`，Tracking 延至 Phase 2）<br>③ Phase 2 新增"修改 `Projectile.Initialize` 签名"任务（与实际代码对齐，影响 3 处调用点）<br>④ 清理残留 v1.0 术语 |
>
> 方案修正根因详见主文档 `StarChart_DataPipeline.md` 的修订历史。

> **文档定位**
> 本文档回答一个问题：**"B+ 方案的 4 项能力，按什么顺序、在哪天做完？"**
>
> - 只写**执行步骤与验收节点**；架构设计归 `StarChart_DataPipeline.md`
> - 只写**分阶段可交付的增量**；具体类/方法/字段设计归主文档
>
> **维护原则**
> 每完成一个 Phase：
> 1. 在本文档勾掉该 Phase
> 2. 追加 `Docs/5_ImplementationLog/ImplementationLog_YYYY-MM.md` 日志
> 3. 若实施中发现设计问题，**先改 `StarChart_DataPipeline.md`，再改代码**

---

## 0. 执行原则

### 0.1 MVP 先行

第一阶段（Phase 1）必须能**端到端跑通**：改 CSV → Import → Play 看到变化。这一阶段交付，后续阶段才有安全感。

### 0.2 先加不删

每个 Phase **只引入新代码/新文件**；现有 SO / Resolver / ServiceLocator 调用**全部保留**。`StarChartRegistry` 与 `IStarChartItemResolver` 并行运行。等 Phase 7 清理期才做替换。

### 0.3 单 Phase 不超过 2 天工作量

若某阶段预估超过 2 天，**先拆**。这是 Project Ark 的迭代速度底线（Implement_rules.md 4）。

### 0.4 Done 的严格定义

每个 Phase 末尾都有一组 **"可演示"** 的验收项。所有项目必须能在 Unity Play Mode 中演示通过，否则不算完成。

---

## 0.5 Phase 0 — 预置（老 SO 重命名 + CSV ID 前缀化）

> **v1.2 新增**：v1.0/v1.1 未考虑"现有 SO 命名与 CSV InternalName 不一致"的问题。用户决策 P1（SO 文件名 = `{InternalName}.asset`）要求在 Phase 1 前做一次性对齐。

**目标**：把现有 8 Core + 8 Prism + ... SO 重命名为 CSV InternalName 对应格式，避免 Phase 1 导入时产生"新老两套命名并存"。

**工时估算**：0.5-1h（主要是手工重命名 + 验证引用不断）

### 0.5.1 任务清单

**Step 1：SO 文件重命名**

对每个 SO 用 `Assets > Rename`（保留 GUID 不断引用）：

| 当前文件名 | 重命名为（= CSV.InternalName） |
|---|---|
| `MatterCore_StandardBullet.asset` | `Core_Matter_MachineGun.asset`（或按 CSV 实际值） |
| `LightCore_BasicLaser.asset` | `Core_Light_Laser.asset` |
| `EchoCore_BasicWave.asset` | `Core_Echo_SonicWave.asset` |
| `AnomalyCore_Boomerang.asset` | `Core_Anomaly_Boomerang.asset` |
| `ShebaCore_MachineGun.asset` | （根据 CSV 决定：合并去重 or 改名共存） |
| `ShebaCore_FocusLaser.asset` | 同上 |
| `ShebaCore_PulseWave.asset` | 同上 |
| `ShebaCore_Shotgun.asset` | 同上 |
| Prism 8 个、Sail 3 个、Sat 1 个 | 同规则处理 |

**重命名策略**：
- 在 Unity Editor 中 **右键 Rename**（不要在 Finder 手动改文件名，会丢 GUID）
- 对每个改名：打开引用它的其他资产（如 Prefab），确认引用未断
- 冲突处理：若 CSV InternalName 与多个现有 SO 都对应（如 ShebaCore vs MatterCore），**合并为一个**，其他作废

**Step 2：CSV ID 前缀化**

把 `StarChart_StarCores.csv` / `StarChart_Prisms.csv` / `StarChart_LightSails.csv` / `StarChart_Satellites.csv` 的 `ID` 列：
- Core：`1001` → `C001`，`1002` → `C002`，...
- Prism：`2001` → `P001`，...
- Sail：待补
- Sat：待补

**Step 3：验证**

- [ ] 打开现有 StarChart 相关 Prefab / Scene，Missing 引用计数 == 0
- [ ] Play Mode 启动正常（现有装备的 SO 仍可显示 + 发射）

### 0.5.2 验收

- [ ] 所有现有 SO 已改名为 `{InternalName}.asset` 格式
- [ ] 4 张 CSV 的 ID 列全部字母前缀化
- [ ] 现有 Unity 场景/Prefab 无 Missing 引用
- [ ] `git status` 显示大量 `.meta` 变化（GUID 保留，仅名字变）——**不应出现删除+新增的 meta 对**

---

## 1. Phase 1 — Importer 基础设施（MVP）

**目标**：跑通 StarCore 一张表的 CSV → SO 单向导入。

**工时估算**：3-4h（不含 Phase 0 的重命名时间）

### 1.1 任务清单

- [ ] 新建 `Assets/Scripts/Combat/Editor/DataPipeline/` 目录
- [ ] 实现 `StarChartImporterBase.cs`（抽象基类）：
  - [ ] `ParseCsvLine` / `BuildRowDict`（照抄 BestiaryImporter）
  - [ ] `TrySetFloat / TrySetInt / TrySetString / TrySetEnum` 工具方法
  - [ ] `TrySetPrefab(row, col, searchDirs[])` 多候选路径搜索
  - [ ] `TrySetSprite(row, col, searchDirs[])`
  - [ ] `ParseSemicolonDict(raw)` 分号串协议解析
  - [ ] `EnsureFolder` 辅助
- [ ] 实现 `StarCoresImporter.cs`：
  - [ ] `[MenuItem("ProjectArk/StarChart/Import Star Cores")]`
  - [ ] `ImportOne(row, rowNum)` 映射 Core 所有字段
  - [ ] **SO 路径：`Assets/_Data/StarChart/Cores/{InternalName}.asset`**（无前缀，v1.2）
  - [ ] 写入 `so._itemId = row["ID"]`
- [ ] 为 `StarCoreSO` 增加字段：
  - [ ] `[SerializeField] private string _itemId`
  - [ ] `[SerializeField] private CoreArchetype _archetype = CoreArchetype.Straight`
  - [ ] `[SerializeField] private string _movementOverrides`
  - [ ] 公开 getter：`ItemId / Archetype / MovementOverrides`
- [ ] 定义 `CoreArchetype` 枚举：
  - [ ] Phase 1 **只含 `Straight` 一个值**
  - [ ] Phase 2 再增 `Tracking`
- [ ] Importer 对 `Archetype` 列的校验逻辑：
  - [ ] Phase 1：CSV 填写其他值时 `Debug.LogError`，降级为 Straight（不阻断导入）
- [ ] **更新现有 CSV**（`StarChart_StarCores.csv`）：
  - [ ] ID 列字母前缀化（见 Phase 0 Step 2，已完成）
  - [ ] 补齐新列：`DisplayName_zh / DisplayName_en / Description_zh / Description_en / Archetype / MovementParams / TrailTime / TrailWidth / TrailColor / FireSound / FireSoundPitchVariance / DesignIntent_Note`
  - [ ] 现有 13 行逐行填写（Archetype 全部留空或填 `Straight`）

### 1.2 验收（必须全部通过）

- [ ] 菜单 `ProjectArk > StarChart > Import Star Cores` 不报错
- [ ] 导入后，`_Data/StarChart/Cores/` 下 SO 数量 == CSV 行数（13+）
- [ ] Phase 0 重命名的老 SO 被 Update（CSV 数值覆盖）
- [ ] 改 CSV 某行的 `BaseDamage` → Import → Inspector 看到对应 SO 更新
- [ ] 未填写的列不覆盖 SO 原值（等价于 `TrySet*` 的 skip-empty 策略）
- [ ] 错误行不影响其他行（如 CSV 第 3 行 `Family=Mater` 打错，第 3 行 LogError 但第 4+ 行正常）
- [ ] `Shape` 列填 `Shape1x2H` / `ShapeL` / `ShapeLMirror` 等实际枚举值能正确解析
- [ ] Importer 生成的 SO `_itemId` 字段 == CSV 的 ID 列值（`C001` 等）

### 1.3 不在本阶段的内容

- ❌ Movement 组件库实现（延至 Phase 2）
- ❌ Registry（延至 Phase 3）
- ❌ Prism / Sail / Sat 的 Importer（延至 Phase 4）
- ❌ Archetype `Tracking` / `Serpentine` 等扩展值（延至 Phase 2+）
- ❌ Export 反向工具（v1.1 已移除，不再计划）

---

## 2. Phase 2 — Movement 组件 MVP

**目标**：用 H3 方案跑通"直线" + "追踪"两种 Archetype 的 Core。

**工时估算**：3-4h

### 2.1 任务清单

- [ ] 新建 `Assets/Scripts/Combat/Projectile/Movement/` 目录
- [ ] 实现 `IProjectileMovement.cs` 接口 + `ProjectileContext` struct
  - [ ] 命名空间 `ProjectArk.Combat`（与现有 `Projectile.cs` 一致，不是 `ProjectArk.Combat.Projectile`）
  - [ ] 接口含 `OnSpawn` / `OnFixedUpdate` / `OnUpdate` / `OnReturnToPool`（见 Library §2.1）
- [ ] 实现 `MovementParamApplier.cs`（反射 Apply + TryConvert）
- [ ] 实现 `StraightMovement.cs`（占位实现，`OnFixedUpdate` / `OnUpdate` 都空）
- [ ] 实现 `TrackingMovement.cs`：
  - [ ] 参数：`[SerializeField] float _turnRate = 180f`
  - [ ] 参数：`[SerializeField] float _acquisitionRange = 20f`
  - [ ] `OnFixedUpdate`：每物理帧查询 `EnemyDirector` 获取最近敌人，按 turnRate 修正 `Rigidbody2D.velocity`
  - [ ] Awake 缓存默认值（防多 Core 共享 Prefab 污染，见 Library §7.2）
- [ ] **修改 `Projectile.cs`**：
  - [ ] Awake 时 `GetComponent<IProjectileMovement>()` + 缓存 `_hasMovement` bool（见 Library §2.2）
  - [ ] `Initialize` 增加可选第四参数 `Transform shooter = null`（**保持前三个参数不变，向后兼容**）
  - [ ] `Initialize` 既有代码末尾 `if (_hasMovement) _movement.OnSpawn(ctx)`
  - [ ] `FixedUpdate` 增加分支：`if (_hasMovement) _movement.OnFixedUpdate(dt)`（如果原来没有 FixedUpdate 方法则新建）
  - [ ] `Update` 末尾增加 `if (_hasMovement) _movement.OnUpdate(dt)`
  - [ ] `OnReturnToPool` 头部调用 `if (_hasMovement) _movement.OnReturnToPool()`
- [ ] **扩充 `CoreArchetype` 枚举**：新增 `Tracking`
- [ ] 创建两个模板 Prefab：
  - [ ] `Assets/_Data/StarChart/Prefabs/Projectiles/Projectile_Straight.prefab`（不挂 Movement 组件或挂 StraightMovement 占位均可）
  - [ ] `Assets/_Data/StarChart/Prefabs/Projectiles/Projectile_Tracking.prefab`（挂 TrackingMovement）
- [ ] 在 CSV 某行 Core 上设置 `Archetype=Tracking, ProjectilePrefab=Projectile_Tracking, MovementParams=TurnRate:90`
- [ ] 更新 `ProjectileSpawner.SpawnMatterProjectile`：
  - [ ] spawn 后调 `MovementParamApplier.Apply(movement, coreSnap.MovementOverrides)`
  - [ ] （可选）补传 `shooter = track.ShooterTransform` 到 `projectile.Initialize`

### 2.2 验收

- [ ] Play Mode 中发射 Archetype=Straight 的 Core → 子弹直线飞（StraightMovement no-op，等价现有行为）
- [ ] 发射 Archetype=Tracking 的 Core → 子弹追踪附近敌人
- [ ] CSV 把 TurnRate 改为 45 → Import → 子弹转向速度显著变慢
- [ ] 发射 100 次后 Memory Profiler 无泄漏
- [ ] Tracking + Penetrate Prism 同时装备 → 子弹追踪且穿透（Modifier 与 Movement 互相兼容）
- [ ] **现有 3 处 `Projectile.Initialize` 调用点（ProjectileSpawner 两处 + AutoTurretBehavior 一处）无需改动依旧编译通过**（第四参数 shooter 默认 null）

### 2.3 不在本阶段

- ❌ SerpentineMovement / BoomerangMovement / GravityMovement（延至未来 Batch-M2/M3）
- ❌ 现有 `BoomerangModifier` 迁移（延至 Phase 7 清理期）

---

## 3. Phase 3 — StarChartRegistry（基于 Manifest）

**目标**：O(1) 通过 string ID 查 SO，为 SaveSystem 升级铺路。

**工时估算**：2h

> **v1.1 修改**：原方案要求 `Resources.LoadAll` + SO 搬家到 Resources 目录（高风险）。新方案用 `StarChartManifest.asset` 承载清单，**SO 位置不变**。

### 3.1 任务清单

- [ ] 定义 `StarChartManifest.cs`（ScriptableObject，持有 `[SerializeField] StarChartItemSO[] _items`）
- [ ] 创建实例资产 `Assets/_Data/StarChart/StarChartManifest.asset`
- [ ] 在 `StarChartImporterBase.cs` 的 `ImportAll()` 末尾添加"刷新 Manifest"逻辑（`AssetDatabase.FindAssets("t:StarChartItemSO")`）
- [ ] 实现 `Assets/Scripts/Combat/StarChart/StarChartRegistry.cs`（API 见主文档 §6.3）
- [ ] 在 `StarChartItemSO` 基类已有的 `_itemId` 字段基础上公开 `ItemId`
- [ ] BootLoader / SceneStartup 添加 `[SerializeField] StarChartManifest _manifest`，Awake 时调 `StarChartRegistry.Initialize(_manifest)`
- [ ] Play Mode 退出时 `StarChartRegistry.Clear()`（若 Domain Reload disabled）
- [ ] 单元测试：
  - [ ] `Registry_InitializePopulatesDictFromManifest`
  - [ ] `Registry_ReturnsNullForUnknownId`
  - [ ] `Registry_DetectsDuplicateId`
  - [ ] `Manifest_RefreshedAfterImport`（跑 Import 后 Items 数量增加）

### 3.2 验收

- [ ] `StarChartManifest.asset` 存在且 Items 数组非空（至少含现有 8 Core + 8 Prism + ...）
- [ ] 运行 Import → Manifest 自动包含新 SO
- [ ] 运行时 `StarChartRegistry.Get<StarCoreSO>("C001")` 返回非 null
- [ ] 不存在 ID 返回 null 而非抛异常
- [ ] Console 启动日志显示 `[StarChartRegistry] 已注册 N 个星图部件`，N == Manifest.Items.Length
- [ ] 单元测试全绿
- [ ] **现有 SO 目录布局不变**，所有引用未断

### 3.3 不在本阶段

- ❌ SaveSystem 改为存 string ID（延至 Phase 5）
- ❌ 废弃 `IStarChartItemResolver`（延至 Phase 5）

---

## 4. Phase 4 — Prism / Sail / Sat Importer

**目标**：补齐其他三个子类的 CSV 导入。

**工时估算**：3h

### 4.1 任务清单

- [ ] 实现 `PrismsImporter.cs`：
  - [ ] 支持**双模式兼容**（新 `StatModifiers` 分号串 + 旧 `LogicType/TargetStat/Operation/Param_1/Param_2` 扁平）
  - [ ] 见主文档 §7.2
- [ ] 实现 `LightSailsImporter.cs`
- [ ] 实现 `SatellitesImporter.cs`
- [ ] 新增菜单项：
  - [ ] `ProjectArk/StarChart/Import Prisms`
  - [ ] `ProjectArk/StarChart/Import Light Sails`
  - [ ] `ProjectArk/StarChart/Import Satellites`
  - [ ] `ProjectArk/StarChart/Import All Star Chart`（一键全部）
- [ ] **更新现有三张 CSV**：
  - [ ] `StarChart_Prisms.csv` 加 `DisplayName_zh / _en / Description_zh / _en / DesignIntent_Note` 列（旧列保留）
  - [ ] `StarChart_LightSails.csv` / `StarChart_Satellites.csv` 同样补齐

### 4.2 验收

- [ ] 四张表全部可导入，菜单一键跑完
- [ ] 现有 18 行 Prism 数据成功生成 SO（双模式兼容）
- [ ] Sail / Sat 至少 1 条新增记录成功生成 SO
- [ ] 重复导入不产生重复 SO（InternalName 作为 key）

### 4.3 不在本阶段

- ❌ Prism 旧扁平列的 CSV 重写（让策划自然迁移；Phase 7 清理）

---

## 5. Phase 5 — 多语言字段 & SaveSystem 升级

**目标**：多语言字段通过 Importer 就位；SaveSystem 改为存 string ID。

**工时估算**：3-4h

### 5.1 任务清单

- [ ] 实现 `StarChartImporterBase.TrySetMultiLangString(row, colZh, colEn, so, fieldName)`
- [ ] 新增菜单：`ProjectArk/StarChart/Language/中文`、`English`（切换 EditorPref）
- [ ] 在 `Import All` 摘要弹框显示当前语言
- [ ] 重新 Import 所有子类，验证 `DisplayName / Description` 按语言正确写入
- [ ] 升级 `PlayerSaveData` / `StarChartSaveSerializer`：
  - [ ] 存：`string CoreId = loadout.Core?.ItemId`
  - [ ] 读：`loadout.Core = StarChartRegistry.Get<StarCoreSO>(saveData.CoreId)`
  - [ ] 兼容旧档：若存档是老格式（GUID 引用），fallback 走旧路径 + 写入 log 建议用户重存档
- [ ] 降级 `IStarChartItemResolver` 为 Registry 的薄封装（保留接口以免破坏现有调用者）

### 5.2 验收

- [ ] 切换到 English → Import → SO 的 `_displayName` 变成英文
- [ ] Play → 装备若干部件 → 保存 → 重启 → 读档 → 部件正确恢复
- [ ] 存档 JSON 打开可读，包含形如 `"CoreId": "C001"` 的字段
- [ ] 删除某 SO 再读档 → Console 报"未知 ID 'C999'"，游戏不崩
- [ ] 现有所有 `IStarChartItemResolver` 调用点无需改动（接口降级透明）

### 5.3 不在本阶段

- ❌ 运行时切换语言（即 SO 里同时存 zh + en 字段）。B+ 方案是"编辑时语言烤入"占位方案

---

## 6. ~~Phase 6 — 反向导出（SO → CSV）~~（v1.1 已删除）

> **v1.1 决策**：红队审查认定此阶段为"预支复杂度"，违背"垂直切片优先"原则，延期至真有需求时再做。详见主文档 §8。
>
> **未来重启条件**：
> - 同一部件连续 3 次只在 Inspector 改不回 CSV
> - 批量 SO 改动超过 20 条
> - 策划主动请求
>
> 重启后预计工时 2-3h。

---

## 7. Phase 7 — 清理期（可选）

**目标**：清理临时兼容代码，收束权威路径。

**工时估算**：1-2h

### 7.1 任务清单（按需）

- [ ] 删除 `PrismsImporter` 的"旧扁平模式"兼容分支（若所有 CSV 已迁移到 `StatModifiers` 分号串）
- [ ] 迁移 `BoomerangModifier` 为 `BoomerangMovement`（见 Library §8.2）
- [ ] 若 SaveSystem 切稳，删除旧 GUID 存档兼容代码（发警告要求玩家重存档）
- [ ] 更新 `StarChart_CanonicalSpec.md` 以反映新架构
- [ ] 更新 `StarChart_AssetRegistry.md` 登记 Manifest 资产

### 7.2 验收

- [ ] `grep -r "LogicType\|TargetStat"` 在 Importer 代码中无结果（旧模式已删）
- [ ] CanonicalSpec 第 N 节明确引用 Registry API
- [ ] Log 打开项目，不再出现 "legacy save format" 警告

---

## 8. 时间线总览（v1.2 B+ 方案）

| Phase | 名称 | 工时 | 可交付物 |
|---|---|---|---|
| **0** | **预置：SO 重命名 + CSV ID 前缀化** | **0.5-1h** | **现有 SO 与 CSV 命名统一（v1.2 新增）** |
| 1 | Importer 基础设施 | 3-4h | StarCores CSV → SO 导入跑通（Archetype 仅 Straight） |
| 2 | Movement 组件 MVP | 3-4h | Straight + Tracking 两种 Archetype；`Projectile.Initialize` 增可选参数 |
| 3 | StarChartRegistry（Manifest 版） | 2h | O(1) ID 查询可用（SO 不搬家） |
| 4 | Prism / Sail / Sat Importer | 3h | 四张表全部导入跑通；Prism 双模式兼容 |
| 5 | 多语言 + SaveSystem 升级 | 3-4h | 存档用 string ID；中英双语切换 |
| ~~6~~ | ~~反向导出~~ | ~~2-3h~~ | **v1.1 已删除**（延期至真有需求） |
| 7 | 清理期（可选） | 1-2h | 删除旧兼容路径、迁移 BoomerangModifier → Movement |

**累计**：**11.5-15h**（原 v1.1 为 11-14h，v1.2 +0.5-1h 是 Phase 0）。建议分 2-4 个开发日完成，每个 Phase 结束立即追加 ImplementationLog 并提交。

---

## 9. 执行约束

### 9.1 每个 Phase 开始前必须完成

1. 阅读主文档 `StarChart_DataPipeline.md` 对应章节
2. 在 Console 确认前一 Phase 的验收项无回归
3. 创建 feature branch：`feature/datapipeline-phase-N`

### 9.2 每个 Phase 完成后必须完成

1. 执行所有"验收"项并在 Play Mode 演示
2. 追加 `Docs/5_ImplementationLog/ImplementationLog_YYYY-MM.md`
3. 若改动了主文档的接口/协议，**同步更新 `StarChart_DataPipeline.md` 或 `ProjectileMovement_Library.md`**
4. 跑一次 `dotnet build Project-Ark.slnx` 确认 0 错误

### 9.3 卡壳时的决策树

```
遇到阻碍？
├─ 是否 Phase 设计问题 → 改 StarChart_DataPipeline.md，不擅自加 hack
├─ 是否 Movement 库问题 → 改 ProjectileMovement_Library.md
├─ 是否单纯实施问题 → 在本 Plan 追加 9.4 子任务
└─ 是否跨 Phase 耦合 → 停下，重新评估阶段边界
```

### 9.4 紧急 TODO（实施中追加）

空（实施时按需填写）。

---

## 10. 风险与回退策略

| 风险 | 发生概率 | 回退方案 |
|---|---|---|
| ~~Phase 3 的 SO 迁移到 Resources 后引用全断~~ | ~~低~~ | **v1.1 已消除该风险**（不再迁移 SO 目录） |
| Manifest 资产丢失 / 损坏 | 低 | 重新跑一次 Import 即可重建；Manifest 由 Importer 确定性生成 |
| Phase 5 的 SaveSystem 升级破坏老存档 | 中 | 保留旧 GUID fallback 路径至少 1 个月，给用户迁移窗口 |
| ~~Phase 6 的 Exporter 破坏 CSV（如格式错误）~~ | ~~低~~ | **v1.1 已消除该风险**（不做 Exporter） |
| 反射性能不满足需求 | 极低 | Movement 参数覆盖在低频 Spawn 路径，可接受 |
| 多语言只支持 zh/en 两种 | 已知 | 不是风险，是 scope 限制；后续可扩展 |

---

## 11. 交付物 checklist（B+ 方案全部完成时）

- [ ] **Phase 0 产物**：所有现有 SO 改名为 `{InternalName}.asset` 格式；4 张 CSV 的 ID 列字母前缀化
- [ ] 4 份新 Editor 脚本：StarCoresImporter / PrismsImporter / LightSailsImporter / SatellitesImporter
- [ ] 1 份 Importer 基类：StarChartImporterBase
- [ ] ~~4 份对应 Exporter~~ **v1.1 已删除**
- [ ] 1 份 Registry：StarChartRegistry（基于 Manifest）
- [ ] 1 份 Manifest 类：StarChartManifest（ScriptableObject）
- [ ] 1 份 Manifest 资产实例：`Assets/_Data/StarChart/StarChartManifest.asset`
- [ ] 2 份 Movement MVP 实现：StraightMovement / TrackingMovement + IProjectileMovement + MovementParamApplier
- [ ] **Projectile.cs 升级**：新增可选 `Transform shooter` 参数 + FixedUpdate 分派（向后兼容）
- [ ] 4 张 CSV 更新完毕（含多语言列、新协议列、字母前缀 ID）
- [ ] ~~所有 SO 迁移至 Resources 目录~~ **v1.1 已取消**（SO 保持现有目录）
- [ ] SaveSystem 支持 string ID
- [ ] 新增单元测试至少 6 项
- [ ] ImplementationLog 有 6-7 条对应日志
- [ ] CanonicalSpec / AssetRegistry / Implement_rules 同步更新

---

*本计划是 `StarChart_DataPipeline.md` + `ProjectileMovement_Library.md` 的落地执行手册。*
