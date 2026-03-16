# StS2 资产汇总与 ID 体系规范

> 更新时间：2026-03-16  
> 适用范围：`ReferenceAssets/StS2` 解包资产 + `SideProject/StS2mod`（Astrolabe）数据/代码  
> 目标：统一 Slay the Spire 2 参考资产、运行时 `Id.Entry`、Astrolabe JSON 数据、Advisor 运行时快照的命名体系

---

## 1. 结论先行

本次核对后，**StS2 的 canonical 运行时 ID 规则已经可以明确写死**：

- **官方模型 ID（Card / Relic / Character / Boss 等）以 `ModelId.Entry` 为准**
- **`ModelId.Entry` 的生成规则是 `StringHelper.Slugify(type.Name)`**
- **最终格式是 `UPPER_SNAKE_CASE`**
- 示例：
  - `BodySlam` → `BODY_SLAM`
  - `PaperPhrog` → `PAPER_PHROG`
  - `StrikeIronclad` → `STRIKE_IRONCLAD`
  - `Ironclad` → `IRONCLAD`
  - `TheGuardian` / `the_guardian` 语义目标统一写成 `THE_GUARDIAN`

**因此，Astrolabe 内部凡是要和游戏运行时 `Id.Entry` 对接的字段，都必须使用 `UPPER_SNAKE_CASE`。**

唯一例外是：

- **`path_id` 属于 Astrolabe 自定义构筑方案命名空间，不属于官方 `ModelId.Entry`**
- 所以 `path_id` 应继续使用 **`lower_snake_case`**，例如：
  - `ironclad_strength`
  - `silent_poison`
  - `watcher_divinity`

---

## 2. 本次核对的证据来源

### 2.1 解包资产根目录

本次规范以 `F:\UnityProjects\ReferenceAssets\StS2` 为参考源，核心目录如下：

- `StS2_CSharp/`
  - ILSpy 反编译得到的 C# 源码
  - 用于确认 `ModelId`、`ModelDb`、`StringHelper`、Card/Relic/Character 类名
- `StS2_Ripped/`
  - GDRE 恢复出的 Godot 项目
  - 用于确认本地化 key、图片命名、资源路径、场景/图集命名

### 2.2 本次直接用于确认 ID 规则的关键文件

#### 源码侧

- `StS2_CSharp/MegaCrit.Sts2.Core.Models/ModelDb.cs`
  - `GetEntry(Type type) => StringHelper.Slugify(type.Name)`
- `StS2_CSharp/MegaCrit.Sts2.Core.Helpers/StringHelper.cs`
  - `Slugify()` 将类名转换为 `UPPER_SNAKE_CASE`
- `StS2_CSharp/MegaCrit.Sts2.Core.Models/CardModel.cs`
  - 卡牌标题/描述本地化 key 使用 `base.Id.Entry + ".title" / ".description"`
- `StS2_CSharp/MegaCrit.Sts2.Core.Models/RelicModel.cs`
  - 遗物标题/描述同样使用 `base.Id.Entry`
- 代表性模型文件：
  - `.../Cards/BodySlam.cs`
  - `.../Cards/Whirlwind.cs`
  - `.../Relics/PaperPhrog.cs`
  - `.../Relics/RedSkull.cs`
  - `.../Cards/StrikeIronclad.cs`
  - `.../Cards/DefendIronclad.cs`

#### 资产/本地化侧

- `StS2_Ripped/localization/eng/cards.json`
  - 例：`"BODY_SLAM.title"`
- `StS2_Ripped/localization/eng/relics.json`
  - 例：`"PAPER_PHROG.title"`
- `StS2_Ripped/images/` 与 `CardModel/RelicModel` 路径规则
  - 图片资源使用 `base.Id.Entry.ToLowerInvariant()` 拼接路径
  - 即：运行时 canonical ID 是大写蛇形，资源路径层再转小写

---

## 3. 官方 ID 体系的真实结构

## 3.1 ModelId 结构

官方 `ModelId` 由两部分组成：

- **Category**：模型大类，例如 Card / Relic / Character / Act 等
- **Entry**：具体条目 ID

Astrolabe 真正需要对齐的，是 **`Entry`**。

### 3.2 Entry 生成规则

规则可概括为：

1. 从 C# 类型名出发
2. 在驼峰边界插入 `_`
3. 转成全大写
4. 去掉非法字符

### 3.3 示例映射

| 源 | 旧写法 | canonical 写法 |
|---|---|---|
| 卡牌类名 | `BodySlam` | `BODY_SLAM` |
| 卡牌类名 | `StrikeIronclad` | `STRIKE_IRONCLAD` |
| 遗物类名 | `PaperPhrog` | `PAPER_PHROG` |
| 遗物类名 | `RedSkull` | `RED_SKULL` |
| 角色类名 | `Ironclad` | `IRONCLAD` |
| Boss/遭遇习惯写法 | `the_guardian` | `THE_GUARDIAN` |
| 旧 PascalCase 数据写法 | `LimitBreak` | `LIMIT_BREAK` |
| 旧 lower_snake 数据写法 | `limit_break` | `LIMIT_BREAK` |

---

## 4. Astrolabe 应遵守的字段规范

## 4.1 必须使用官方 canonical ID 的字段

以下字段**直接和 StS2 运行时数据对接**，必须统一为 `UPPER_SNAKE_CASE`：

- `cards.json`
  - `card_id`
- `relics.json`
  - `relic_id`
  - 顶层 `*_starter`
  - 顶层 `*_exclusive`
- `buildpaths.json`
  - `character`
  - `core_cards`
  - `key_relics`
  - `campfire_upgrades`
  - `good_against_bosses`
  - `bad_against_bosses`
- `bosses.json`
  - `boss_id`
  - `counter_cards`
- 运行时快照 / Hook 输入
  - `RunSnapshot.CharacterId`
  - `RunSnapshot.DeckCardIds`
  - `RunSnapshot.RelicIds`
  - 商店/选牌 Hook 里从 `Id.Entry` 读取出来的所有模型 ID

## 4.2 必须使用 Astrolabe 自定义 lower_snake 的字段

以下字段**不是官方模型 ID**，而是 Astrolabe 自己定义的构筑方案空间，应保持 `lower_snake_case`：

- `buildpaths.json`
  - `path_id`
- `cards.json` / `relics.json`
  - `path_scores` 的 key
- `bosses.json`
  - `dangerous_to_paths`
- 任何 Advisor 内部“构筑路线”维度字段

### 正确示例

- `card_id = "BODY_SLAM"`
- `relic_id = "PAPER_PHROG"`
- `character = "IRONCLAD"`
- `path_id = "ironclad_defense"`
- `path_scores["ironclad_defense"] = 9`

### 错误示例

- `card_id = "BodySlam"` ❌
- `card_id = "body_slam"` ❌
- `character = "ironclad"` ❌
- `path_id = "IRONCLAD_DEFENSE"` ❌

---

## 5. 升级牌的特殊规则

运行时升级牌会以 `+` 后缀出现，例如：

- `BODY_SLAM+`
- `WHIRLWIND+`

规范要求：

- **运行时快照允许保留 `+` 后缀**
- **静态数据库中的基础条目不单独存升级版**
- 查库时必须：
  1. 先去掉 `+`
  2. 再做 canonical 归一化

也就是说：

- 运行时：`BODY_SLAM+`
- 数据库：`BODY_SLAM`
- Lookup：先 strip `+`，再 lookup

这也是本次修复里必须补上的一个实质性 bug：之前 `CardAdvisor` 和商店建议对升级牌会直接 miss。

---

## 6. 资源路径规则

虽然运行时 canonical ID 用大写蛇形，但资源路径并不直接用大写，而是：

- `base.Id.Entry.ToLowerInvariant()`

这意味着：

- 逻辑/数据层：`BODY_SLAM`
- 资源路径层：`body_slam`

**重要：不要把资源文件名规则误认为运行时模型 ID 规则。**

这是之前产生混淆的根因之一：

- 有人看到本地化/图片路径里是小写或文件名风格
- 又看到 C# 类型名是 PascalCase
- 最后把 JSON 写成了 PascalCase 或 lower_snake 混合体

正确答案是：

- **逻辑 ID = `UPPER_SNAKE_CASE`**
- **资源路径 = `lowercase(entry)`**
- **源类名 = PascalCase，仅作为生成 canonical ID 的来源**

---

## 7. 当前 Astrolabe 数据覆盖现状

根据本次归一化报告 `tools/normalize_sts2_ids.report.json`：

- **卡牌评分数据**：87 条
- **遗物评分数据**：138 条
- **构筑方案**：12 条
- **Boss 数据**：9 条
- **当前已评分角色覆盖**：仅 `IRONCLAD`
- **当前保留 `legacy_path_scores` 的条目数**：卡牌 87，遗物 138
- **当前已生效 path score key**：

  - `ironclad_strength`
  - `ironclad_infinite`
  - `ironclad_defense`

### 这意味着什么

1. **Astrolabe 的多角色 buildpath 已经建好了骨架**
   - `silent_*`
   - `defect_*`
   - `watcher_*`
2. **但评分数据目前实际上仍以 Ironclad 为主**
3. 其他角色路径目前更多是“路线定义已存在，细分卡牌/遗物评分尚未补齐”

所以这次的 ID 统一，**先解决的是“体系不一致导致无法正确命中”**，不是“全角色数据完整度”问题。

---

## 8. 旧数据与新规则的映射结论

## 8.1 已确认并已落地映射的旧 key

| 旧 key | 新 key |
|---|---|
| `strength_build` | `ironclad_strength` |
| `exhaust_build` | `ironclad_infinite` |
| `block_build` | `ironclad_defense` |

## 8.2 暂未激活的遗留轴

| 旧 key | 当前处理 |
|---|---|
| `burn_build` | 不再作为 active path score 使用；若需要保留语义，应放入 `legacy_path_scores` 或未来补 `ironclad_burn` 路线 |

这条很关键：

- `burn_build` 说明旧数据曾经存在“燃烧流”维度
- 但当前 `buildpaths.json` 没有 `ironclad_burn`
- 所以它**不能再继续留在 `path_scores` 主字典里**，否则会制造“有评分轴但没有对应路径”的新不一致

推荐策略就是本次采用的方式：

- 主评分字典只保留当前真实存在的 buildpath key
- 遗留轴单独保留，供未来扩线时复用

---

## 9. 本次已对 Astrolabe 落地的修改

## 9.1 数据文件

已对以下文件完成 canonical 化：

- `SideProject/StS2mod/data/cards.core.json`
- `SideProject/StS2mod/data/cards.advisor.json`
- `SideProject/StS2mod/data/relics.json`
- `SideProject/StS2mod/data/buildpaths.json`
- `SideProject/StS2mod/data/bosses.json`


### 已落地的数据改动

- `card_id` / `relic_id` 统一改为 `UPPER_SNAKE_CASE`
- `buildpaths` 中所有卡牌/遗物/Boss 引用统一改为 `UPPER_SNAKE_CASE`
- `character` 统一改为 `IRONCLAD` 这类 canonical 写法
- `path_scores` 主字典统一对齐当前真实存在的 `path_id`
- `bosses.json` 中 `boss_id` / `counter_cards` 统一 canonical 化
- 顶层 `note` 字段改写为新规则说明

## 9.2 代码层

已新增/修改：

- `src/Astrolabe/Data/IdNormalizer.cs`
  - 统一做：
    - 模型 ID 归一化
    - lookup 去 `+`
    - 旧 path key 映射
    - starter strike/defend 判断
- `src/Astrolabe/Data/DataLoader.cs`
  - 数据加载时统一做规范化
  - `GetCard/GetRelic/GetBoss/GetBuildPath/GetPathsForCharacter` 全部走归一化查询
- `src/Astrolabe/Engine/AdvisorEngine.cs`
  - 删除基础牌的逻辑改为 canonical 判断，不再依赖旧 `strike/defend` 小写写法
- `src/Astrolabe/Core/RunStateReader.cs`
  - 注释更新为真实 canonical 示例
- `src/Astrolabe/Core/Snapshots.cs`
  - 注释更新为真实 canonical 示例
- `src/Astrolabe/Engine/BuildPathManager.cs`
  - 注释中的示例 ID 已统一到新规范

## 9.3 工具脚本

新增：

- `tools/normalize_sts2_ids.py`
  - 负责分析并重写 `cards/relics/buildpaths/bosses`
- `tools/normalize_sts2_ids.report.json`
  - 当前覆盖范围报告
- `tools/restore_legacy_burn.py`
  - 一次性从 Git 原始数据回填 `burn_build` 遗留评分到 `legacy_path_scores`

---

## 10. 之后的执行规则

后续所有新增数据，必须遵守以下规则：

1. **凡是和 StS2 运行时 `Id.Entry` 对接的字段，一律写 `UPPER_SNAKE_CASE`**
2. **凡是 Astrolabe 自己定义的路线/策略 ID，一律写 `lower_snake_case`**
3. **运行时允许 `+` 后缀，静态数据库不存升级副本**
4. **不要再把 PascalCase 类名直接写进 JSON**
5. **不要再把资源文件名的小写写法当成逻辑 ID**
6. **若新增暂未启用的 archetype 轴，不要混进主 `path_scores`，而是明确建新 `path_id` 或单独存为 legacy 字段**

---

## 11. 当前仍需注意的限制

### 11.1 当前评分数据仍以 Ironclad 为主

虽然 `buildpaths.json` 里已经有 12 条路径，但当前 `cards/relics` 的有效 path score 只覆盖：

- `ironclad_strength`
- `ironclad_infinite`
- `ironclad_defense`

也就是说：

- **ID 体系现在已经统一了**
- **但 Silent / Defect / Watcher 的评分内容还没有补齐到同等完整度**

### 11.2 `ActBossId` 目前仍是 Act key 的粗略代理

`RunStateReader` 当前还没有从 `BossEncounters` 精确取到当前 Boss 模型 ID，仍是临时取 `runState.Act.Id.Entry` 做占位。

因此：

- `bosses.json` 的 canonical 化是对的
- 但地图/路线顾问若要真正做到 Boss 精准针对，还需要后续补一轮运行时读取链路

---

## 12. 建议的后续工作顺序

1. **先保持当前 canonical 规则不再漂移**
2. **补 Silent / Defect / Watcher 的 cards/relics 评分数据**
3. **若确实要恢复燃烧流，显式新增 `ironclad_burn` path_id，而不是偷放回主 `path_scores`**
4. **补 Boss 精确读取链路，把 `ActBossId` 从 Act key 升级为真实 Boss ID**

---

## 13. 一句话标准

以后在 Astrolabe 里判断一个字段该怎么写，只问一句：

- **这个字段是不是要和游戏运行时 `Id.Entry` 对上？**
  - 是 → `UPPER_SNAKE_CASE`
  - 否，属于 Astrolabe 自己的路线/策略空间 → `lower_snake_case`
