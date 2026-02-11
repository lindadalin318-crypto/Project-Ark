# 需求文档：Enemy_Bestiary.CSV 配置表重设计

## 引言

当前 `Enemy_Bestiary.csv` 仅有 15 个字段，远不能覆盖 `EnemyStatsSO.cs` 代码中的 30+ 运行时参数。这导致策划填表后仍需程序手动在 Unity Inspector 中补填大量数据，违背了项目"**数据驱动、禁止 hardcode**"的核心架构原则。

本次重设计的目标是：将 `Enemy_Bestiary.csv` 扩展为**唯一权威数据源（Single Source of Truth）**，使其能够：
1. **1:1 映射** 到 `EnemyStatsSO` 的所有 `public` 字段
2. **覆盖未来可预见的扩展维度**（抗性、掉落、特殊行为标签等）
3. **支持自动化管线**：CSV → 编辑器脚本 → 批量生成/更新 SO 资产

本文档同时参考了 `Enemy_List.csv`（80+ 怪物设计文案）和 `EnemyPlanning.csv`（AI 原型库）中的设计意图，确保配置表能承载这些设计信息。

---

## 需求

### 需求 1：配置表字段必须完整覆盖 EnemyStatsSO 的所有运行时参数

**用户故事：** 作为一名策划，我希望在 CSV 表格中填入一行数据后，程序能自动生成完整可用的 EnemyStatsSO 资产，以便我无需打开 Unity Inspector 手动补填任何数值。

#### 验收标准

1. WHEN 配置表定义了一行敌人数据 THEN 系统 SHALL 能从该行数据生成一个字段完整的 `EnemyStatsSO` 实例，所有 `public` 字段均有对应列。
2. IF `EnemyStatsSO.cs` 中存在字段 X THEN `Enemy_Bestiary.csv` SHALL 存在对应的列名，且列名与字段名保持一致或有明确的映射关系。
3. WHEN 表格中某个可选字段为空 THEN 系统 SHALL 使用 `EnemyStatsSO` 中定义的默认值（如 `[Min]`、初始赋值等）。

**当前缺失字段清单（与 EnemyStatsSO 的 Gap 分析）：**

| 分组 | EnemyStatsSO 字段 | 当前 Bestiary 是否有 | 状态 |
|------|-------------------|---------------------|------|
| Identity | EnemyName | ✅ DisplayName | OK |
| Identity | EnemyID | ✅ InternalName | OK |
| Health | MaxHP | ✅ MaxHealth | OK（列名不一致） |
| Health | MaxPoise | ✅ Poise | OK（列名不一致） |
| Movement | MoveSpeed | ✅ | OK |
| Movement | RotationSpeed | ❌ | **缺失** |
| Attack | AttackDamage | ❌ | **缺失** |
| Attack | AttackRange | ✅ | OK |
| Attack | AttackCooldown | ✅ | OK |
| Attack | AttackKnockback | ❌ | **缺失** |
| Attack Phases | TelegraphDuration | ❌ | **缺失** |
| Attack Phases | AttackActiveDuration | ❌ | **缺失** |
| Attack Phases | RecoveryDuration | ❌ | **缺失** |
| Ranged | ProjectilePrefab | ❌ | **缺失** |
| Ranged | ProjectileSpeed | ❌ | **缺失** |
| Ranged | ProjectileDamage | ❌ | **缺失** |
| Ranged | ProjectileKnockback | ❌ | **缺失** |
| Ranged | ProjectileLifetime | ❌ | **缺失** |
| Ranged | ShotsPerBurst | ❌ | **缺失** |
| Ranged | BurstInterval | ❌ | **缺失** |
| Ranged | PreferredRange | ❌ | **缺失** |
| Ranged | RetreatRange | ❌ | **缺失** |
| Perception | SightRange | ❌（仅有 AggroRange） | **缺失** |
| Perception | SightAngle | ❌ | **缺失** |
| Perception | HearingRange | ❌ | **缺失** |
| Leash | LeashRange | ❌ | **缺失** |
| Leash | MemoryDuration | ❌ | **缺失** |
| Visuals | HitFlashDuration | ❌ | **缺失** |
| Visuals | BaseColor | ❌ | **缺失** |

**总计：19 个字段缺失，需要新增。**

---

### 需求 2：配置表必须包含策划侧的设计元数据列

**用户故事：** 作为一名策划，我希望在同一张表中记录敌人的设计意图、生态位、出场星球等信息，以便查阅表格时无需在多个文档间来回切换。

#### 验收标准

1. WHEN 策划编辑 Bestiary 表 THEN 系统 SHALL 提供以下元数据列供策划填写：`Rank`（Minion/Elite/Boss）、`AI_Archetype`（行为原型引用）、`FactionID`（阵营）、`PlanetID`（首次出场星球）、`Description`（设计意图简述）、`SpawnWeight`（生成权重）。
2. IF 某列属于"纯策划备注"且不映射到代码 THEN 该列 SHALL 以 `_Note` 后缀命名，导入脚本会自动跳过这些列。
3. WHEN 策划需要记录某敌人的克制关系和玩家应对策略 THEN 系统 SHALL 提供 `DesignIntent` 和 `PlayerCounter` 列。

---

### 需求 3：配置表需支持战斗设计的额外维度扩展

**用户故事：** 作为一名战斗设计师，我希望能在配置表中定义敌人的抗性、弱点、掉落表、击杀奖励和特殊行为标签，以便实现更丰富的战斗交互而不需要修改代码。

#### 验收标准

1. WHEN 配置表定义了敌人的元素抗性 THEN 系统 SHALL 提供以下列：`Resist_Physical`、`Resist_Fire`、`Resist_Ice`、`Resist_Lightning`、`Resist_Void`，取值范围 0.0（无抗性）到 1.0（完全免疫），默认 0。
2. WHEN 配置表定义了击杀奖励 THEN 系统 SHALL 提供 `ExpReward`（经验值）和 `DropTableID`（掉落表引用 ID）列。
3. IF 敌人具有特殊行为标签（如 `SuperArmor`、`SelfDestruct`、`Invisible`、`Reflective`） THEN 系统 SHALL 提供 `BehaviorTags` 列，以英文分号 `;` 分隔的标签字符串形式存储。
4. WHEN 配置表中存在 `BehaviorTags` 列 THEN 导入脚本 SHALL 将其解析为 `List<string>` 或 `HashSet<string>`，供状态机在运行时查询。

---

### 需求 4：配置表的列名规范和分组约定

**用户故事：** 作为一名程序员，我希望 CSV 列名有清晰的命名规范和逻辑分组，以便编写导入脚本时能快速定位字段，且未来新增字段时位置不会混乱。

#### 验收标准

1. WHEN 配置表被创建 THEN 所有列 SHALL 按以下分组顺序排列：
   - **A. 身份与元数据**（ID, InternalName, DisplayName, Rank, AI_Archetype, FactionID, PlanetID）
   - **B. 生命与韧性**（MaxHP, MaxPoise）
   - **C. 移动**（MoveSpeed, RotationSpeed）
   - **D. 近战攻击**（AttackDamage, AttackRange, AttackCooldown, AttackKnockback）
   - **E. 攻击阶段**（TelegraphDuration, AttackActiveDuration, RecoveryDuration）
   - **F. 远程攻击**（ProjectilePrefab, ProjectileSpeed, ProjectileDamage, ProjectileKnockback, ProjectileLifetime, ShotsPerBurst, BurstInterval, PreferredRange, RetreatRange）
   - **G. 感知**（SightRange, SightAngle, HearingRange）
   - **H. 栓绳与记忆**（LeashRange, MemoryDuration）
   - **I. 抗性**（Resist_Physical, Resist_Fire, Resist_Ice, Resist_Lightning, Resist_Void）
   - **J. 奖励与掉落**（ExpReward, DropTableID）
   - **K. 视觉反馈**（HitFlashDuration, BaseColor_R, BaseColor_G, BaseColor_B, BaseColor_A, PrefabPath）
   - **L. 行为标签与设计备注**（BehaviorTags, SpawnWeight, DesignIntent, PlayerCounter, Description_Note）
2. WHEN 列名对应 `EnemyStatsSO` 字段 THEN 列名 SHALL 与字段名完全一致（PascalCase），以支持反射式自动映射。
3. IF 列名不对应代码字段（纯策划用） THEN 列名 SHALL 使用 `_Note` 后缀或归入 L 组末尾。

---

### 需求 5：EnemyStatsSO 需同步扩展以承接新字段

**用户故事：** 作为一名程序员，我希望 `EnemyStatsSO.cs` 同步新增抗性、掉落表引用、行为标签等字段，以便运行时代码能直接从 SO 读取这些数据，无需额外查表。

#### 验收标准

1. WHEN 配置表新增了抗性列 THEN `EnemyStatsSO` SHALL 新增对应的 `public float Resist_Physical = 0f` 等字段，并标注 `[Range(0f, 1f)]`。
2. WHEN 配置表新增了 `DropTableID` 列 THEN `EnemyStatsSO` SHALL 新增 `public string DropTableID = ""` 字段。
3. WHEN 配置表新增了 `BehaviorTags` 列 THEN `EnemyStatsSO` SHALL 新增 `public List<string> BehaviorTags = new List<string>()` 字段。
4. WHEN 配置表新增了 `PlanetID` 列 THEN `EnemyStatsSO` SHALL 新增 `public string PlanetID = ""` 字段（用于筛选/调试，不影响运行时行为）。
5. WHEN 配置表新增了 `SpawnWeight` 列 THEN `EnemyStatsSO` SHALL 新增 `public float SpawnWeight = 1f` 字段。

---

### 需求 6：提供 CSV → SO 的自动化导入工具

**用户故事：** 作为一名程序员，我希望有一个 Editor 菜单项能一键从 `Enemy_Bestiary.csv` 批量生成/更新所有 `EnemyStatsSO` 资产，以便策划修改 CSV 后程序只需点一下就能同步到 Unity。

#### 验收标准

1. WHEN 用户点击 `ProjectArk > Import Enemy Bestiary` 菜单 THEN 系统 SHALL 读取 `Enemy_Bestiary.csv`，为每一行数据创建或更新对应的 `EnemyStatsSO` 资产。
2. IF 某个 `EnemyID` 的 SO 资产已存在 THEN 系统 SHALL 更新其字段值而非重复创建。
3. IF 某个 `EnemyID` 的 SO 资产不存在 THEN 系统 SHALL 在 `Assets/_Data/Enemies/` 目录下新建 `EnemyStats_{EnemyID}.asset`。
4. WHEN CSV 中某个可选字段为空 THEN 系统 SHALL 保留该字段的默认值不做修改。
5. WHEN 导入完成 THEN 系统 SHALL 弹出对话框报告：成功导入/更新了多少个 SO，跳过了多少行（无效数据），耗时多少。
6. WHEN CSV 中存在 `BaseColor_R/G/B/A` 四列 THEN 导入脚本 SHALL 将其合并为 `Color(R, G, B, A)` 赋值给 `EnemyStatsSO.BaseColor`。
7. WHEN CSV 中存在 `BehaviorTags` 列（分号分隔字符串） THEN 导入脚本 SHALL 将其拆分为 `List<string>` 赋值给 `EnemyStatsSO.BehaviorTags`。

---

### 需求 7：配置表需兼容已有数据并保持向后兼容

**用户故事：** 作为一名策划，我希望现有 `Enemy_Bestiary.csv` 中已填写的 6 行敌人数据能无缝迁移到新表格格式中，以便不丢失已有工作。

#### 验收标准

1. WHEN 新表格创建完成 THEN 系统 SHALL 保留已有 6 行数据（ID 5001–5006），将旧字段值迁移到对应的新列中。
2. IF 旧列名与新列名不同（如 `MaxHealth` → `MaxHP`，`AggroRange` → `SightRange`） THEN 迁移脚本或手动调整 SHALL 保证值正确映射。
3. WHEN 已有数据行的新增列（如 TelegraphDuration）为空 THEN 这些字段 SHALL 使用 EnemyStatsSO 的默认值。

---

## 最终配置表结构（完整列清单）

以下为重设计后的 `Enemy_Bestiary.csv` 完整列定义（共 48 列）：

| # | 列名 | 类型 | 分组 | 映射 EnemyStatsSO 字段 | 默认值 | 说明 |
|---|------|------|------|----------------------|--------|------|
| 1 | ID | int | A | — | — | 唯一标识，5xxx 格式 |
| 2 | InternalName | string | A | EnemyID | — | 代码内部名称 |
| 3 | DisplayName | string | A | EnemyName | — | 中文显示名 |
| 4 | Rank | enum | A | — | Minion | Minion / Elite / Boss |
| 5 | AI_Archetype | string | A | — | — | 行为原型 ID（引用 EnemyPlanning 表） |
| 6 | FactionID | string | A | — | — | 阵营标识 |
| 7 | PlanetID | string | A | PlanetID | — | 首次出场星球（P1/P2/P3…/Global） |
| 8 | MaxHP | float | B | MaxHP | 100 | 最大生命值 |
| 9 | MaxPoise | float | B | MaxPoise | 50 | 最大韧性值 |
| 10 | MoveSpeed | float | C | MoveSpeed | 3 | 移动速度（单位/秒） |
| 11 | RotationSpeed | float | C | RotationSpeed | 360 | 旋转速度（度/秒） |
| 12 | AttackDamage | float | D | AttackDamage | 10 | 近战攻击伤害 |
| 13 | AttackRange | float | D | AttackRange | 1.5 | 近战攻击距离 |
| 14 | AttackCooldown | float | D | AttackCooldown | 1 | 攻击冷却（秒） |
| 15 | AttackKnockback | float | D | AttackKnockback | 5 | 近战击退力 |
| 16 | TelegraphDuration | float | E | TelegraphDuration | 0.4 | 前摇时长（秒）——玩家读信号窗口 |
| 17 | AttackActiveDuration | float | E | AttackActiveDuration | 0.2 | 判定窗口时长（秒）——无法转向 |
| 18 | RecoveryDuration | float | E | RecoveryDuration | 0.6 | 后摇硬直（秒）——玩家惩罚窗口 |
| 19 | ProjectilePrefab | string | F | ProjectilePrefab* | — | 远程子弹 Prefab 路径（空=近战型） |
| 20 | ProjectileSpeed | float | F | ProjectileSpeed | 8 | 子弹飞行速度 |
| 21 | ProjectileDamage | float | F | ProjectileDamage | 8 | 子弹伤害 |
| 22 | ProjectileKnockback | float | F | ProjectileKnockback | 3 | 子弹击退力 |
| 23 | ProjectileLifetime | float | F | ProjectileLifetime | 4 | 子弹存活时间（秒） |
| 24 | ShotsPerBurst | int | F | ShotsPerBurst | 3 | 每轮连射发数 |
| 25 | BurstInterval | float | F | BurstInterval | 0.25 | 连射间隔（秒） |
| 26 | PreferredRange | float | F | PreferredRange | 10 | 射手理想射击距离 |
| 27 | RetreatRange | float | F | RetreatRange | 5 | 射手后撤触发距离 |
| 28 | SightRange | float | G | SightRange | 10 | 视觉检测距离 |
| 29 | SightAngle | float | G | SightAngle | 60 | 视锥半角（度） |
| 30 | HearingRange | float | G | HearingRange | 15 | 听觉检测距离 |
| 31 | LeashRange | float | H | LeashRange | 20 | 栓绳距离（超出则脱战） |
| 32 | MemoryDuration | float | H | MemoryDuration | 3 | 记忆衰退时间（秒） |
| 33 | Resist_Physical | float | I | Resist_Physical* | 0 | 物理抗性 (0~1) |
| 34 | Resist_Fire | float | I | Resist_Fire* | 0 | 火焰抗性 (0~1) |
| 35 | Resist_Ice | float | I | Resist_Ice* | 0 | 冰霜抗性 (0~1) |
| 36 | Resist_Lightning | float | I | Resist_Lightning* | 0 | 雷电抗性 (0~1) |
| 37 | Resist_Void | float | I | Resist_Void* | 0 | 虚空抗性 (0~1) |
| 38 | ExpReward | int | J | — | 0 | 击杀经验值 |
| 39 | DropTableID | string | J | DropTableID* | — | 掉落表引用 ID |
| 40 | HitFlashDuration | float | K | HitFlashDuration | 0.1 | 受击闪白时长（秒） |
| 41 | BaseColor_R | float | K | BaseColor.r | 1 | 基础颜色 R (0~1) |
| 42 | BaseColor_G | float | K | BaseColor.g | 1 | 基础颜色 G (0~1) |
| 43 | BaseColor_B | float | K | BaseColor.b | 1 | 基础颜色 B (0~1) |
| 44 | BaseColor_A | float | K | BaseColor.a | 1 | 基础颜色 A (0~1) |
| 45 | PrefabPath | string | K | — | — | Prefab 资源路径 |
| 46 | BehaviorTags | string | L | BehaviorTags* | — | 行为标签（分号分隔） |
| 47 | SpawnWeight | float | L | SpawnWeight* | 1 | 生成权重 |
| 48 | DesignIntent | string | L | — | — | 设计意图简述 |
| 49 | PlayerCounter | string | L | — | — | 玩家应对策略 |
| 50 | Description_Note | string | L | — | — | 策划备注（不导入代码） |

> 标注 `*` 的字段需要在 `EnemyStatsSO.cs` 中同步新增。

---

## 边界情况与注意事项

1. **BaseColor 拆分为 RGBA 四列**：CSV 不支持 `Color` 类型，故拆成 4 个 float 列。导入脚本需合并。
2. **ProjectilePrefab 是路径字符串**：CSV 中填写 Prefab 的相对路径（如 `Enemies/EnemyProjectile`），导入脚本通过 `AssetDatabase.LoadAssetAtPath` 转为 `GameObject` 引用。
3. **BehaviorTags 分号分隔**：如 `SuperArmor;SelfDestruct;Invisible`，导入脚本 `Split(';')` 后赋值给 `List<string>`。
4. **近战型敌人的远程列留空**：远程攻击分组 (F) 的所有列可为空，导入脚本使用默认值。`ProjectilePrefab` 为空表示该敌人是近战型。
5. **Boss 类敌人的特殊性**：Boss 通常有独立的多阶段脚本，配置表只记录基础数值和元数据，复杂阶段逻辑仍由专用 Brain 脚本实现。`AI_Archetype` 填 `Boss_Phase_*` 表示使用独立脚本。
6. **向后兼容**：旧表 `MaxHealth` → 新表 `MaxHP`，旧表 `Poise` → 新表 `MaxPoise`，旧表 `AggroRange` → 新表 `SightRange`。迁移时需手动映射。
