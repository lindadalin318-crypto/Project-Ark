# StarChart 数据管线设计（Data Pipeline Spec）

> **⚠️ 修订历史**
>
> | 日期 | 版本 | 变更 |
> |---|---|---|
> | 2026-04-27 14:58 | v1.0 | 初版（套餐 C 激进方案：CSV + Registry + Archetype + 多语言 + 双向同步） |
> | 2026-04-27 15:10 | v1.1 | **红队审查修正为 B+ 方案**：<br>① 删除"双向同步 Exporter"（预支复杂度，违背垂直切片原则）<br>② Registry 从 `Resources.LoadAll` 改为 `AssetDatabase + Manifest`（避免 SO 搬家 + 更 Addressables 友好）<br>③ 不引入 `SlotData` 槽位元数据（YAGNI，GDD 暂无需求） |
> | 2026-04-27 16:10 | **v1.2（当前）** | **文档对齐实际代码修正**：<br>① ItemShape 枚举名对齐实际代码（`Shape1x2H` / `Shape2x1V` / `ShapeL` / `ShapeLMirror`）<br>② ID 格式确认为 `C001 / P001 / S001 / T001` 字母前缀（用户决策 O2）<br>③ SO 命名规则改为 `{InternalName}.asset` 无前缀（用户决策 P1，老 SO 在 Phase 1 前重命名）<br>④ 清理残留 v1.0 术语（"套餐 C"、"Resources/StarChart/"、Phase 编号错位） |
>
> **修正根因**：v1.1 通过架构审查后，对文档与现有代码的一致性做第二轮审查，发现 11 个实际偏差（3 个阻碍实施 + 3 个需澄清 + 4 个文档笔误）。v1.2 是"代码真相对齐"修正，不改变架构。详见 `ImplementationLog_2026-04.md` 2026-04-27 16:10 条目。

> **文档定位**
> 本文档回答一个问题：**"星图部件数据怎么从 CSV 流到运行时？"**
>
> - 只写**现役规范**与**目标架构**；演化与迭代步骤归 `StarChart_DataPipeline_Plan.md`
> - 只写**数据流与结构契约**；Movement 组件库内部归 `ProjectileMovement_Library.md`
> - 只写**数据层面**；UI 编辑器、策划使用说明归 `Docs/3_WorkflowsAndRules/`
>
> **产物协作**
> - Movement 组件库 → `ProjectileMovement_Library.md`
> - 分阶段实施计划 → `StarChart_DataPipeline_Plan.md`
> - 现役链路 owner → `StarChart_CanonicalSpec.md`
> - 部件资产路径 → `StarChart_AssetRegistry.md`
>
> 三者如冲突：**代码 > CanonicalSpec > 本文档 > Plan**。本文档是方案，代码是真相。
>
> **维护原则**
> 当 CSV 列结构、Importer 行为、Registry API、分号串协议发生变更时，本文档必须同步更新。

---

## 1. 背景与目标

### 1.1 现状盘点（2026-04-27）

| 层 | 现状 |
|---|---|
| 部件规划 | `Docs/4_GameData/StarChart/StarChart_Planning.csv` 规划 **90 个部件**（Core 24 / Prism 40 / Sail 12 / Sat 14） |
| CSV 数据 | `Docs/4_GameData/StarChart/` 已有 4 张表（StarCores / Prisms / LightSails / Satellites），共 13 Core + 18 Prism + N Sail / Sat |
| SO 资产 | `Assets/_Data/StarChart/` 已有 8 Core + 8 Prism + 3 Sail + 1 Sat |
| 管线状态 | **CSV 与 SO 未对接**——CSV 是策划规划，SO 是代码实际读取的数据源 |
| 查询方式 | 靠 `ServiceLocator.Get<IStarChartItemResolver>()` 逐 id 查询 |
| 多语言 | 未支持，`DisplayName` 仅中文 |

### 1.2 本文档目标（B+ 方案）

借鉴 Magicraft 的数据驱动架构，**精简后**交付 4 项能力：

1. **CSV 单向导入（CSV → SO）**：4 张表对应四种部件，Importer 全自动写入 SO
2. **StarChartRegistry（静态查询）**：O(1) 通过 string ID 查 SO，基于 **`AssetDatabase` 扫描 + `StarChartManifest` 序列化**（不使用 Resources，不迁移 SO 目录）
3. **CoreArchetype 枚举**（H3 Movement 组件库方案）：在 CoreFamily 之下提供行为细分
4. **多语言字段占位**：CSV 增加 `DisplayName_zh / _en` 双列；Importer 按当前语言写入 SO

**非目标**（这些不在本文档范围）：
- Movement 组件库的具体类设计 → 见 `ProjectileMovement_Library.md`
- Magicraft 法术复刻清单 → 后续 Batch 工作
- Editor UI 编辑器 → 未纳入范围，未来再议
- **双向同步（SO → CSV 反向导出）** → v1.0 曾包含；v1.1 基于 YAGNI 原则移除，真有需求时再做，预计 2-3h
- **SlotData 槽位元数据** → v1.0 曾讨论引入；v1.1 确认 GDD 现阶段不需要拟态/封印/槽位等级，不引入

---

## 2. 架构概览

### 2.1 数据流（单向：CSV → SO → Registry → 运行时）

```
                [策划在 Excel / Google Sheets 编辑]
                          ↓ 导出 UTF-8 CSV
                ┌──────────────────────────┐
                │  Docs/4_GameData/        │
                │  StarChart/*.csv         │
                │  （权威数值来源）         │
                └──────────────────────────┘
                          ↓ ProjectArk > StarChart > Import All
                ┌──────────────────────────┐
                │  Editor: StarChartImporter│
                │  • 解析 CSV               │
                │  • 分号串协议展开         │
                │  • Prefab 路径解析        │
                │  • 多语言字段选择         │
                │  • Create / Update SO     │
                │  • 重建 Manifest          │  ← v1.1 新增
                └──────────────────────────┘
                          ↓
                ┌──────────────────────────┐
                │  Assets/_Data/StarChart/  │
                │  {Cores|Prisms|...}/      │
                │  *.asset 文件             │
                │  （位置不变，不搬家）     │
                └──────────────────────────┘
                          ↓ Importer 收集 + 写入
                ┌──────────────────────────┐
                │  StarChartManifest.asset  │
                │  SerializedField[]        │
                │  （v1.1 新增：运行时入口） │
                └──────────────────────────┘
                          ↓ Boot 时加载
                ┌──────────────────────────┐
                │  StarChartRegistry        │
                │  Dictionary<string, SO>   │
                │  （O(1) 查询缓存）        │
                └──────────────────────────┘
                          ↓ Get<T>(id)
                [运行时消费：StarChartController / SnapshotBuilder / SaveSystem]
```

> **v1.1 修正说明**：v1.0 使用 `Resources.LoadAll<StarChartItemSO>("StarChart")`，要求把所有 SO 搬到 `Resources/StarChart/` 目录。v1.1 改为 Importer 在每次导入后刷新一个 `StarChartManifest.asset`（`[SerializeField] StarChartItemSO[] _allItems`），Registry 在运行时读该 manifest。优点：<br>• SO 位置不变，现有引用不破<br>• 不强制全量打包（未列入 manifest 的 SO 不会进 build）<br>• 对未来 Addressables 迁移更友好（只需把 manifest 字段改为 AssetReference 数组）

---

## 3. CSV 格式规范

### 3.1 文件布局（方案 B：四张独立表）

```
Docs/4_GameData/StarChart/
├── StarChart_Planning.csv         # 仅规划/元信息（不参与导入）
├── StarChart_StarCores.csv        # Core：约 28 列
├── StarChart_Prisms.csv           # Prism：约 13 列
├── StarChart_LightSails.csv       # Sail：约 12 列
└── StarChart_Satellites.csv       # Sat：约 13 列
```

**决策理由**（见 2026-04-27 对话）：四张独立表 > 一张大表。各子类字段差异大（Core 22 字段 vs Prism 4 字段），合并会产生 70+ 列大部分为空。

### 3.2 通用列（四张表都有）

| 列名 | 类型 | 必填 | 说明 |
|---|---|:---:|---|
| `ID` | string | ✅ | 全局唯一 ID，规则：`C001` / `P001` / `S001` / `T001`（C=Core / P=Prism / S=Sail / T=saTellite）<br>v1.2 决策：现有 CSV 数字 ID（1001/2001...）在 Phase 1 **一次性改为字母前缀格式**，Importer 以字母前缀判断类型族 |
| `InternalName` | string | ✅ | **SO 资产完整文件名（不含扩展名）**，如 `Core_Matter_MachineGun` → `Core_Matter_MachineGun.asset`。v1.2：Importer **不加额外前缀**，策划在 CSV 里写什么就生成什么文件名 |
| `DisplayName_zh` | string | ✅ | 中文显示名 |
| `DisplayName_en` | string | ⭕ | 英文显示名（多语言占位，未填则 fallback 到 zh） |
| `Description_zh` | string | ⭕ | 中文描述（长文本） |
| `Description_en` | string | ⭕ | 英文描述 |
| `IconPath` | string | ⭕ | 相对 `Assets/Art/UI/Icons/` 的路径（不含扩展名） |
| `Shape` | enum | ✅ | `Shape1x1` / `Shape1x2H` / `Shape2x1V` / `ShapeL` / `ShapeLMirror` / `Shape2x2`（**必须与 `StarChartEnums.ItemShape` 完全一致**） |
| `HeatCost` | float | ✅ | 热量消耗 |
| `DesignIntent_Note` | string | ⭕ | **给策划看的设计意图**，不映射到 SO |

**SKIP 列**（永不映射到 SO 字段，仅供策划/Diff 阅读）：
- 所有以 `_Note` 结尾的列
- `ID`（用于 Importer 匹配逻辑 + 写入 `StarChartItemSO._itemId`，非其他 SO 字段）
- `DesignIntent` / `PlayerCounter`（照抄 Bestiary 惯例）

### 3.3 StarCores 专属列

完整列表：

```
ID, InternalName, DisplayName_zh, DisplayName_en, Description_zh, Description_en,
IconPath, Shape, HeatCost,
Family, Archetype, DamageType,
FireRate, BaseDamage, ProjectileSpeed, Lifetime, Spread, Knockback, RecoilForce,
ProjectilePrefab, AnomalyModifierPrefab,
MuzzleFlashPrefab, ImpactVFXPrefab,
TrailTime, TrailWidth, TrailColor,
FireSound, FireSoundPitchVariance,
MovementParams,
DesignIntent_Note
```

**Core 特有字段说明**：

| 列名 | 类型 | 说明 |
|---|---|---|
| `Family` | enum | `Matter` / `Light` / `Echo` / `Anomaly`（现有 `CoreFamily`） |
| `Archetype` | enum | `Straight` / `Tracking` / `Serpentine` / `Boomerang` / `Meteor` / ...（见 ProjectileMovement_Library.md） |
| `DamageType` | enum | `Physical` / `Fire` / `Ice` / `Lightning` / `Void` |
| `TrailColor` | color | 格式 `#RRGGBBAA`（例 `#FF5500AA`）；空则表示"用 Prefab 默认" |
| `MovementParams` | 分号串 | 用于覆盖 Movement 组件字段；格式 `Amplitude:5;Frequency:3`。见 §4 |

### 3.4 Prisms 专属列

```
ID, InternalName, DisplayName_zh, DisplayName_en, Description_zh, Description_en,
IconPath, Shape, HeatCost,
Family, StatModifiers, ProjectileModifierPrefab,
DesignIntent_Note
```

**Prism 特有字段说明**：

| 列名 | 类型 | 说明 |
|---|---|---|
| `Family` | enum | `Rheology` / `Fractal` / `Tint`（现有 `PrismFamily`） |
| `StatModifiers` | 分号串 | 格式 `Damage:Multiply:1.25;Spread:Add:5.0`。见 §4 |
| `ProjectileModifierPrefab` | path | Tint 族专用；其他族留空 |

> **注**：当前 `StarChart_Prisms.csv` 有 `LogicType / TargetStat / Operation / Param_1 / Param_2` 五列，这是 Magicraft-style 的扁平结构。**升级后合并为 `StatModifiers` 分号串**，更灵活（支持多个 Modifier）。迁移由 Importer 双模式兼容（§7.2）。

### 3.5 LightSails 专属列

```
ID, InternalName, DisplayName_zh, DisplayName_en, Description_zh, Description_en,
IconPath, Shape, HeatCost,
ConditionDescription_zh, ConditionDescription_en,
EffectDescription_zh, EffectDescription_en,
BehaviorPrefab,
DesignIntent_Note
```

### 3.6 Satellites 专属列

```
ID, InternalName, DisplayName_zh, DisplayName_en, Description_zh, Description_en,
IconPath, Shape, HeatCost,
TriggerDescription_zh, TriggerDescription_en,
ActionDescription_zh, ActionDescription_en,
InternalCooldown, BehaviorPrefab,
DesignIntent_Note
```

---

## 4. 分号串协议（Semicolon-Separated Format）

### 4.1 设计目的

CSV 原生不支持数组/结构体字段。分号串把复合数据压平成单列字符串，兼顾紧凑与可读。

### 4.2 协议规则

**基本语法**：
```
item1;item2;item3
```

**带参数语法**（冒号分隔 key:value）：
```
Damage:Multiply:1.25;Spread:Add:5.0
^^^^^^ ^^^^^^^^ ^^^^
Stat   Op       Value
```

**空白处理**：
- 分号两侧允许空格（`"a; b ;c"` = `["a", "b", "c"]`）
- 值前后空格自动 trim

### 4.3 使用场景与字段协议

| 字段 | 格式 | 示例 |
|---|---|---|
| `StatModifiers` | `Stat:Op:Value;...` | `Damage:Multiply:1.25;Spread:Add:5.0` |
| `MovementParams` | `Field:Value;...` | `Amplitude:5;Frequency:3;Direction:2` |
| `BehaviorTags` | `Tag;Tag;...` | `Armored;Flying;Elite` |

### 4.4 错误处理

- **未知枚举值**（如 `StatModifiers=Damge:Multiply:1.25`）→ `Debug.LogError` 中断该行导入，其他行继续
- **参数个数不符**（如 `StatModifiers=Damage:Multiply`，缺 Value）→ `Debug.LogError`，跳过该 Modifier，其他继续
- **数值解析失败** → `Debug.LogError`，该字段保留默认值

原则：**宁可响亮失败，也不要静默失效**（对应 Implement_rules.md 3.5）。

---

## 5. Importer 架构

### 5.1 类图

```
StarChartImporterBase                  （抽象基类）
  ├─ LoadCsv(path): Dictionary<string, Dictionary<string, string>>
  ├─ EnsureFolder(path)
  ├─ [Protected] TrySetFloat / TrySetInt / TrySetString / TrySetEnum
  ├─ [Protected] TrySetPrefab(row, col, so, fieldName, searchDirs[])
  ├─ [Protected] TrySetSprite(row, col, so, fieldName, searchDirs[])
  ├─ [Protected] TrySetMultiLangString(row, colZh, colEn, so, fieldName)
  ├─ [Protected] ParseSemicolonDict(raw): List<KeyValuePair<string, string[]>>
  └─ [Abstract] void ImportOne(Dict row, int rowNum)

StarCoresImporter : StarChartImporterBase
Prisms Importer  : StarChartImporterBase
LightSailsImporter : StarChartImporterBase
SatellitesImporter : StarChartImporterBase

StarChartImportMenu                    （入口）
  ├─ [MenuItem] Import All Star Chart      → 依次调用 4 个
  ├─ [MenuItem] Import Star Cores only
  ├─ [MenuItem] Import Prisms only
  ├─ [MenuItem] Import Light Sails only
  └─ [MenuItem] Import Satellites only
```

### 5.2 导入流程（通用）

对每张 CSV：

```
1. 定位 CSV 文件（若不存在：弹框报错，return）
2. 读取 UTF-8 文本，按行拆分
3. 解析 header 行，记录列名
4. 逐行解析：
   4.1 ParseCsvLine → values[]
   4.2 BuildRowDict(headers, values) → Dict<string, string>
   4.3 验证必填字段（ID / InternalName / DisplayName_zh）
   4.4 生成 SO 路径：{SO_DIR}/{InternalName}.asset
       - SO_DIR 按子类定：Cores/Prisms/Sails/Satellites
       - 不加前缀（v1.2 决策 P1：InternalName 即完整文件名）
   4.5 加载 / 创建 SO
   4.6 ImportOne(row, rowNum)  → 子类实现字段映射，含 _itemId = row[ID]
   4.7 保存 SO（Create / SetDirty）
5. AssetDatabase.SaveAssets + Refresh
6. 刷新 StarChartManifest.asset（见 §6.2）
7. 展示摘要弹框
```

### 5.3 Prefab / Sprite 路径解析策略

照抄 Bestiary 的多候选模式，但按字段类型约定搜索目录：

| 字段 | 搜索目录（按顺序） |
|---|---|
| `ProjectilePrefab` | `Assets/_Data/StarChart/Prefabs/Projectiles/`, `Assets/_Prefabs/Projectiles/` |
| `MuzzleFlashPrefab` | `Assets/_Data/StarChart/Prefabs/VFX/`, `Assets/_Prefabs/VFX/` |
| `ImpactVFXPrefab` | `Assets/_Data/StarChart/Prefabs/VFX/`, `Assets/_Prefabs/VFX/` |
| `AnomalyModifierPrefab` | `Assets/_Data/StarChart/Prefabs/Modifiers/` |
| `ProjectileModifierPrefab` | `Assets/_Data/StarChart/Prefabs/Modifiers/` |
| `BehaviorPrefab` (Sail/Sat) | `Assets/_Data/StarChart/Prefabs/Behaviors/` |
| `IconPath` (Sprite) | `Assets/Art/UI/Icons/` |
| `FireSound` (AudioClip) | `Assets/Audio/SFX/Weapons/` |

**失败处理**：找不到 Prefab **不清空 SO 旧引用**，而是 `Debug.LogError`。保留旧值让用户有机会修。

### 5.4 多语言字段映射

导入器依据 Unity 当前语言（可通过 `EditorPrefs.GetInt("StarChartLang", 0)` 选择）选择列：

```csharp
private static string CurrentLang => EditorPrefs.GetString("ProjectArk.Lang", "zh");

protected void TrySetMultiLangString(Dict row, string colZh, string colEn, SO so, string fieldName) {
    string col = CurrentLang == "en" ? colEn : colZh;
    // fallback: 如果目标列空，回落 zh
    string value = row.GetValueOrDefault(col, "");
    if (string.IsNullOrWhiteSpace(value)) value = row.GetValueOrDefault(colZh, "");
    SetField(so, fieldName, value);
}
```

**菜单项**：
```
ProjectArk > StarChart > Language > 中文 (default)
ProjectArk > StarChart > Language > English
```

**已知限制**：该方案将多语言结果**烤入 SO**，切换语言必须重新导入。后续可升级为运行时查表（引入 `StarChartLocalization` 运行时表），但**不在 B+ 方案范围内**（B+ 是"占位"，不是"完整 i18n"）。

---

## 6. StarChartRegistry（静态查询缓存）

> **v1.1 方案**：基于 `StarChartManifest.asset`（ScriptableObject 清单）+ `AssetDatabase` 扫描。<br>
> **废弃 v1.0 方案**（`Resources.LoadAll`）的原因：要求把所有 SO 搬到 Resources 目录、强制全量打包、对 Addressables 不友好。

### 6.1 StarChartManifest 设计

```csharp
namespace ProjectArk.Combat
{
    /// <summary>
    /// 星图部件清单：列出所有参与 Registry 的 SO。
    /// 由 Importer 每次导入后自动重建；运行时 Registry 读取此文件。
    /// </summary>
    [CreateAssetMenu(fileName = "StarChartManifest", menuName = "ProjectArk/StarChart/Manifest")]
    public class StarChartManifest : ScriptableObject
    {
        [SerializeField] private StarChartItemSO[] _items;
        public IReadOnlyList<StarChartItemSO> Items => _items;

#if UNITY_EDITOR
        /// <summary>Editor-only：由 Importer 调用刷新清单。</summary>
        public void SetItems(StarChartItemSO[] items)
        {
            _items = items;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
```

**位置**：`Assets/_Data/StarChart/StarChartManifest.asset`（唯一）。

### 6.2 Importer 刷新策略

```csharp
// 在 StarChartImporterBase.ImportAll() 末尾：
#if UNITY_EDITOR
var allSOs = AssetDatabase.FindAssets("t:StarChartItemSO")
    .Select(guid => AssetDatabase.LoadAssetAtPath<StarChartItemSO>(
        AssetDatabase.GUIDToAssetPath(guid)))
    .Where(so => so != null && !string.IsNullOrEmpty(so.ItemId))
    .ToArray();

var manifest = AssetDatabase.LoadAssetAtPath<StarChartManifest>(MANIFEST_PATH);
if (manifest == null) {
    manifest = ScriptableObject.CreateInstance<StarChartManifest>();
    AssetDatabase.CreateAsset(manifest, MANIFEST_PATH);
}
manifest.SetItems(allSOs);
AssetDatabase.SaveAssets();
#endif
```

关键点：
- 用 `AssetDatabase.FindAssets("t:StarChartItemSO")` 扫全项目（不限目录）
- 只收集有 `ItemId` 的 SO（Importer 生成的都有；手捏的未填会被跳过）
- Manifest 由 Importer 负责维护，**人工不手改**

### 6.3 Registry API

```csharp
namespace ProjectArk.Combat
{
    public static class StarChartRegistry
    {
        private static Dictionary<string, StarChartItemSO> _byId;
        private static bool _initialized;

        public static void Initialize(StarChartManifest manifest)
        {
            if (_initialized) return;
            if (manifest == null)
            {
                Debug.LogError("[StarChartRegistry] Manifest is null; Registry will be empty.");
                _byId = new Dictionary<string, StarChartItemSO>(StringComparer.Ordinal);
                _initialized = true;
                return;
            }

            _byId = new Dictionary<string, StarChartItemSO>(StringComparer.Ordinal);
            foreach (var so in manifest.Items)
            {
                if (so == null) continue;
                if (string.IsNullOrEmpty(so.ItemId))
                {
                    Debug.LogError($"[StarChartRegistry] SO '{so.name}' 缺 ItemId，跳过注册");
                    continue;
                }
                if (_byId.ContainsKey(so.ItemId))
                {
                    Debug.LogError($"[StarChartRegistry] ID 冲突：'{so.ItemId}' 同时出现在 '{_byId[so.ItemId].name}' 和 '{so.name}'");
                    continue;
                }
                _byId[so.ItemId] = so;
            }
            _initialized = true;
            Debug.Log($"[StarChartRegistry] 已注册 {_byId.Count} 个星图部件");
        }

        public static T Get<T>(string id) where T : StarChartItemSO
            => _byId != null && _byId.TryGetValue(id, out var so) ? so as T : null;

        public static bool TryGet<T>(string id, out T item) where T : StarChartItemSO
        {
            item = Get<T>(id);
            return item != null;
        }

        public static IEnumerable<T> GetAll<T>() where T : StarChartItemSO
            => _byId?.Values.OfType<T>() ?? Enumerable.Empty<T>();

        public static void Clear()
        {
            _byId?.Clear();
            _initialized = false;
        }
    }
}
```

**调用时机**：
- BootLoader / 第一个场景的 `Awake()` 中通过 `[SerializeField] StarChartManifest _manifest` 引用，然后 `StarChartRegistry.Initialize(_manifest)`
- 这个 manifest 引用通过 Inspector 一次性连线，之后由 Importer 自动维护其内容

### 6.4 ItemId 字段

`StarChartItemSO` 基类新增字段：

```csharp
[Header("Identity")]
[Tooltip("Registry 查询 ID，由 CSV 的 ID 列驱动，运行时只读")]
[SerializeField] private string _itemId;
public string ItemId => _itemId;
```

ID 由 CSV 的 `ID` 列驱动，Importer 写入。

### 6.5 与 ServiceLocator 的关系

- `StarChartRegistry` **替代** `IStarChartItemResolver` 的大部分用法
- `ServiceLocator` 仍保留，用于注册/查询**管理器级**组件（如 `HeatSystem`, `StarChartController`）
- `IStarChartItemResolver` 可降级为对 Registry 的薄封装，供现有代码平滑过渡

### 6.6 SaveSystem 受益

存档从 "Guid 引用" 升级为 "string ID"：

```csharp
// 旧
[SerializeField] private StarCoreSO _equippedCore;

// 新（存档时）
saveData.EquippedCoreId = loadout.Core.ItemId;

// 读档时
loadout.Core = StarChartRegistry.Get<StarCoreSO>(saveData.EquippedCoreId);
```

优势：
- 存档文件可读（JSON 里是 `"EquippedCoreId": "C001"` 而非 GUID hash）
- 部件改名/移动不破档
- 策划删除某 ID 时，读档可以 graceful fallback

### 6.7 与未来 Addressables 的兼容路径

若未来切 Addressables，只需把 `StarChartManifest._items` 字段类型从 `StarChartItemSO[]` 改为 `AssetReference[]`，Registry.Initialize 改为 `await Addressables.LoadAssetsAsync`。**Manifest 结构不变，Importer 逻辑不变**。这是 v1.1 方案相对 `Resources.LoadAll` 的关键优势。

---

## 7. CSV 现状迁移策略

### 7.1 数据冲突处理

**现状**：CSV 有 13 Core，SO 有 8 Core，两边的 InternalName 不一致。

| 情形 | 处理 |
|---|---|
| CSV 有，SO 无 | Importer 创建新 SO |
| CSV 无，SO 有 | **保留 SO，不删除**（避免误删，留给手动清理） |
| 两边都有，InternalName 相同 | Importer Update SO（CSV 胜） |
| 两边都有，InternalName 不同 | 两份 SO 并存（策划决定哪份作废） |

### 7.2 Prism 扁平结构 → 分号串的迁移

**现 `StarChart_Prisms.csv` 列**：`LogicType / TargetStat / Operation / Param_1 / Param_2`
**目标列**：`StatModifiers`（分号串）

**Importer 双模式兼容**：
```
if (row 有 "StatModifiers" 列) {
    解析分号串（新模式）
} else if (row 有 "TargetStat" + "Operation" + "Param_1" 列) {
    合成一条 StatModifier = TargetStat:Operation:Param_1（旧模式）
}
```

**迁移时间窗口**：
- Phase 4（Prism Importer 阶段）：Importer 同时支持两种模式
- Phase 7（清理期）：手动把旧列重写为 `StatModifiers` 分号串，删除旧列

### 7.3 SO 物理位置（v1.1：不迁移）

**保持现状**：`Assets/_Data/StarChart/{Cores|Prisms|Sails|Satellites}/*.asset`

v1.0 曾要求迁移到 `Resources/StarChart/` 子目录以支持 `Resources.LoadAll`。v1.1 改用 `StarChartManifest` + `AssetDatabase.FindAssets`，**不再需要任何目录迁移**，现有 SO 引用全部保留。

新增资产：
- `Assets/_Data/StarChart/StarChartManifest.asset`（一份，由 Importer 自动维护）

---

## 8. 反向导出：为什么不做（v1.1 决策）

v1.0 曾规划 "SO → CSV" 反向导出工具。v1.1 基于红队审查移除。

### 8.1 推进动机的重新审视

| 预设场景 | 实际频率 | 真实替代方案 |
|---|---|---|
| 程序在 Inspector 里调试数值 | 高 | Play Mode Hot Reload 足够，不需要永久化 |
| 程序想把调试结果永久留下 | 中 | 手动改 CSV 一行（< 30 秒），比一键 Export 更精确 |
| 批量 SO 改动（加列等） | 极低 | 一次性 Editor 脚本处理 |

### 8.2 延期理由

- **预支复杂度**：为尚未出现的需求预建架构，违背"垂直切片优先"原则（Implement_rules.md）
- **双向维护成本**：Importer 与 Exporter 必须配对维护，每次加字段两处改，错一处就漂移
- **无需求验证**：CSV 管线跑起来前，"谁需要反向"是伪命题——实际使用后再评估
- **工期节省**：2-3h 省出来做 Magicraft 法术复刻更有价值

### 8.3 未来重启条件

如果出现以下信号，再考虑实施 Exporter（预计 2-3h）：
- [ ] 同一部件 **连续 3 次** 只在 Inspector 改不回 CSV，导致策划 / 程序数据漂移
- [ ] 批量 SO 改动（如给所有 Core 加一个新字段）超过 20 条
- [ ] 策划主动请求"帮我导出最新数值"

**不符合上述条件前，手动改 CSV 是标准工作流。**

---

## 9. Archetype 枚举与 Movement 参数

> **本节只定义 Archetype 在 CSV 和 SO 中的位置**。具体行为、组件库、参数列表见 `ProjectileMovement_Library.md`。

### 9.1 CSV 列与可用集

`StarCores.csv` 新增 `Archetype` 列（enum，string）。

**可用值依 Phase 逐步扩充**（v1.2 新增明确说明）：

| Phase | 可用 Archetype 值 | Importer 行为 |
|---|---|---|
| Phase 1 | `Straight` | 仅接受 `Straight` 或留空（=Straight）；遇其他值 `Debug.LogError` 并置 Straight 降级 |
| Phase 2 | `Straight` / `Tracking` | 新增 Tracking；其他值仍报错 |
| Phase-M2（未来） | + `Serpentine` / `Boomerang` / `Gravity` | Library §3.2 列出的扩展集 |
| Phase-M3（未来） | + `Orbital` / `Hover` / `BlackHole` / `Pulse` / `Rolling` | Library §3.3 列出的完备集 |

**策划使用约束**：在 Phase 1 期间，CSV 的 `Archetype` 列只允许填 `Straight` 或空白。填写其他值虽不会破坏导入（会降级为 Straight），但会在 Console 产生 LogError，影响 CI 绿灯状态。

### 9.2 SO 字段

`StarCoreSO.cs` 新增：
```csharp
[Header("Behavior")]
[Tooltip("投射物行为类型。配合 MovementOverrides 使用。")]
[SerializeField] private CoreArchetype _archetype = CoreArchetype.Straight;

[Tooltip("覆盖 Movement 组件字段（分号串格式，如 'Amplitude:5;Frequency:3'）")]
[SerializeField] private string _movementOverrides;

public CoreArchetype Archetype => _archetype;
public string MovementOverrides => _movementOverrides;
```

### 9.3 Movement 参数覆盖协议

运行时 `ProjectileSpawner` spawn 投射物后：
```csharp
var movement = bullet.GetComponent<IProjectileMovement>();
if (movement != null && !string.IsNullOrEmpty(coreSnap.MovementOverrides)) {
    MovementParamApplier.Apply(movement, coreSnap.MovementOverrides);
}
```

**`MovementParamApplier.Apply`** 使用反射按字段名写入。失败时 `Debug.LogError` 并跳过该字段。

> **设计权衡**：反射写字段是运行时成本，但 spawn 是低频事件（每次发射几次），可接受。若性能敏感可升级为"表达式树缓存"。详见 `ProjectileMovement_Library.md` §4。

---

## 10. 单元测试与验收

### 10.1 必要测试（Phase 1 MVP 完成时）

| 测试 | 验证点 |
|---|---|
| `CsvParser_HandlesQuotedCommas` | 描述字段含逗号不被误切 |
| `CsvParser_HandlesEscapedQuotes` | `""` 转义正确解析为 `"` |
| `SemicolonDict_MultipleKVPairs` | `"a:1;b:2"` → `[(a,[1]), (b,[2])]` |
| `SemicolonDict_TrimsWhitespace` | `"a : 1 ; b : 2"` 解析正确 |
| `Importer_CreatesNewSOForNewRow` | CSV 有新 ID → SO 生成 |
| `Importer_UpdatesExistingSOForSameInternalName` | CSV 改数值 → SO 数值更新 |
| `Importer_SkipsUnmappedColumns` | CSV 多一列不崩，打 log |
| `Importer_LogsErrorForInvalidEnum` | `Family=Matr` → 该行失败，其他行正常 |
| `Registry_InitializePopulatesDict` | Manifest 的 Items 数量 == Registry Count |
| `Registry_ReturnsNullForUnknownId` | `Get("nonexist")` 返回 null 不抛异常 |
| `Registry_DetectsDuplicateId` | 同 ID 两个 SO 触发 LogError |
| `Manifest_RefreshedAfterImport` | Import 完成后 Manifest 的 Items 包含新 SO |

### 10.2 人肉验收

在 Play Mode 中：

1. 改 CSV 某 Core 的 `BaseDamage` 列 → Import → Play → 该 Core 伤害变化
2. 新增一行 Core → Import → Inspector 看到新 SO；打开 `StarChartManifest.asset` 看到 Items 已含新 SO
3. 运行时打 `StarChartRegistry.Get<StarCoreSO>("C001")` → 返回非 null
4. 误删 SO → 重新 Import → Manifest 自动移除；读档日志报"未知 ID 'C999'"，游戏不崩

---

## 11. 与现有文档的关系

| 文档 | 关系 |
|---|---|
| `StarChart_CanonicalSpec.md` | 本文档定义**如何把数据灌进 SO**；Spec 定义**SO 数据如何在运行时流动**。灌进之后归 Spec 管 |
| `StarChart_AssetRegistry.md` | 本文档新增的 `StarChartManifest.asset` 资产需要登记；不再涉及 `Resources/StarChart/` 目录 |
| `StarChart_Governance_Plan.md` | B+ 方案实施后更新该文档的"工作流"章节 |
| `Implement_rules.md` | 本文档引用的"单一真相源"、"宁可响亮失败"、"对象池回收清单"均来自此 |
| `ProjectileMovement_Library.md` | 本文档的 §9 引用其具体组件设计 |
| `StarChart_DataPipeline_Plan.md` | 本文档的 §7 迁移策略、§10 测试清单被 Plan 拆成执行步骤 |

---

## 12. 已知风险与防御

| 风险 | 防御 |
|---|---|
| CSV UTF-8 编码 | Importer 强制 `Encoding.UTF8`，仓库强制 UTF-8 with BOM |
| 分号串打错字 | Importer 遇未知枚举 `Debug.LogError`，人肉 CI 跑 Import 后检查 Console |
| Prefab 改名 | Importer 找不到 Prefab 时**不清空 SO 旧引用**，log error 由人处理 |
| 新增字段遗漏 Importer 映射 | 在 `ImportOne()` 头部列一份字段 checklist 注释，每次加字段必改两处 |
| ID 冲突 | Registry Initialize 时强制唯一性检查，冲突则 LogError 跳过第二个 |
| Manifest 与 SO 实际状态不同步 | Importer 每次导入结尾自动刷新 Manifest；人工改 SO 后需再跑一次 Import |
| AssetDatabase.FindAssets 扫全项目慢 | 目前部件 < 100，一次扫 < 100ms；若未来 > 1000 改为按目录限定扫描 |
| 多语言切换需要重新 Import | B+ 方案明确为"占位"，完整 i18n 是后续工作 |

---

*本文档的具体实施路线见 `StarChart_DataPipeline_Plan.md`；Movement 组件设计见 `ProjectileMovement_Library.md`。*
