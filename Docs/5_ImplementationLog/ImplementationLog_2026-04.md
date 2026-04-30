# Implementation Log — 2026-04

---

## StarChart 特效层 P0 治理（Projectile Warning + Trail 参数化 + StarCoreVFXAuditor） — 2026-04-26 11:55

- **修改文件**
  - `Assets/Scripts/Combat/Projectile/Projectile.cs`（`Awake` 的 silent fallback 加 Warning；新增 `ApplyTrailOverrides`；`ConfigureTrail` 改为接收参数）
  - `Assets/Scripts/Combat/StarChart/ProjectileParams.cs`（新增 `TrailTime` / `TrailWidth` / `TrailColor` 三个 readonly 字段 + 兼容的构造器默认参数）
  - `Assets/Scripts/Combat/StarChart/FiringSnapshot.cs`（`CoreSnapshot` 新增同三字段 + `ToProjectileParams()` 注入）
  - `Assets/Scripts/Combat/StarChart/SnapshotBuilder.cs`（从 `StarCoreSO` 把三字段 pass-through 到 `CoreSnapshot`）
  - `Assets/Scripts/Combat/StarChart/StarCoreSO.cs`（新增 `_trailTime / _trailWidth / _trailColor` 三字段 + 三属性；独立 `[Header("VFX — Projectile Trail (Matter / Anomaly only)")]` 区块）
  - `Assets/Scripts/Combat/StarChart/ProjectileSpawner.cs`（顺手补 `using ProjectArk.Core;` 消掉两条 pre-existing `PoolReference` 编译错误）
- **新建文件**
  - `Assets/Scripts/Combat/Editor/StarCoreVFXAuditor.cs`（Editor 菜单 `ProjectArk/Audit StarCore VFX`；按 family 分组列出每个 Core 的 `MuzzleFlash` / `ImpactVFX` / `FireSound` / `Trail` 缺失状态；根据严重程度分三级：ProjectilePrefab 缺失=ERROR、VFX/Audio 缺失=WARNING、Trail 未设=INFO）
- **内容**
  - **Warning 化 silent fallback**：`Projectile.Awake` 在缺 sprite 时生成 8px 程序化占位后，现在会发一条 `Debug.LogWarning` 标明资产名；缺 `TrailRenderer` 时运行时 `AddComponent` 也会发 Warning 标明默认参数值。符合 `Implement_rules.md` §3.5 和 `ProceduralPresentation_WorkflowSpec.md` §3.5 / §7-3 要求的"宁可响亮失败，也不要静默失效"。
  - **Trail 参数契约化**：`StarCoreSO` 现在可以为 Matter / Anomaly 家族的子弹配置 `TrailTime` / `TrailWidth` / `TrailColor`；数据走 `StarCoreSO → SnapshotBuilder → CoreSnapshot → ProjectileParams → Projectile.ApplyTrailOverrides`，保持 Light / Echo 家族不受影响（它们走各自的 prefab）。负值/零 alpha 被解释为"未配置"，保持现有 prefab 或程序化 fallback 不变，**完全向后兼容**。此前 `Projectile.ConfigureTrail` 的硬编码 `0.15f / 0.085f / Color.white` 现在降级为默认 fallback 常量（`DEFAULT_TRAIL_TIME` / `DEFAULT_TRAIL_WIDTH` / `DEFAULT_TRAIL_COLOR`），不再是"gameplay 脚本里的视觉魔法数字"。
  - **StarCoreVFXAuditor 审计工具**：一键遍历项目中所有 `StarCoreSO`，按 `CoreFamily` 分组输出报告。报告中每行一个 Core，flag 用图标标明：`✓`（全齐）、`⬜`（warning 级缺失）、`ⓘ`（info 级 Trail 未设）、`✗`（error 级 Prefab 缺失）。报告末尾有 summary 计数表。UI 根据最严重级别显示 ERRORS / WARNINGS / PASS 三种对话框。这把"8 个 Core 的 VFX 空没空"从"Inspector 点 8 次"变成"菜单点 1 次"。
  - **顺带修复 2 个 pre-existing 编译错误**：`ProjectileSpawner.cs` 用了 `PoolReference` 但没 `using ProjectArk.Core;`，导致 CS0246 × 2。本次顺手补上。剩余 1 个 pre-existing 错误（`StarChartSaveSerializer.cs` 的 `??` + `StarChartContext` 类型推断）留给下一轮，不在本次 P0 范围。
- **目的**
  - 把上一轮 §9 审查中指出的 P0 级债务落地为代码：让"8 个 Core 的 VFX 字段全空"这个盲区变成"菜单一键可见"；让 `Projectile.Awake` 的未文档化 silent fallback 变成"响亮的 Warning + 白纸黑字的默认常量"；让 Trail 参数从"写死在 gameplay 脚本里"变成"数据驱动、可被 Core SO 驱动、对已有 Prefab 零影响"。
  - 本次**不**包含的 P0：4 family 的占位 MuzzleFlash + ImpactVFX prefab 制作，需要先决定占位风格（ParticleSystem / Sprite flipbook / LineRenderer 爆闪），独立 Batch。
- **技术**
  - 向后兼容策略：`ProjectileParams` 的三个 trail 字段全部走 **optional 参数 + sentinel 值**（`trailTime = -1f`、`trailWidth = -1f`、`trailColor = default`）。`Projectile.ApplyTrailOverrides` 三个字段各自独立判断 `>0f` / `alpha > 0f`，任一为"未设"时保留 Projectile 的 fallback 默认值。Editor 中 `StarCoreSO` 的 trail 字段默认初始化为 `-1f / -1f / new Color(0,0,0,0)`，表示"未配置"。所以已有 8 个 Core 的 SO 资产**不需要任何字段更新**就能继续工作，视觉表现完全一致。
  - Trail re-configure 时机：`ApplyTrailOverrides` 在 `Initialize` 里调用，`_trail.Clear()` 清掉旧 trail buffer 避免池复用时残留。只在至少有一个字段被设置时才走完整 `ConfigureTrail` 路径，否则直接 return，避免对已按 prefab 配置好的 trail 做无谓覆盖。
  - Auditor 数据源：`AssetDatabase.FindAssets($"t:{nameof(StarCoreSO)}")`，与现有 `StarChartInventoryValidator` 一致；按 `CoreFamily` 枚举值排序输出让 family 分组可读。
  - 验证：本次 6 个文件改动通过 `dotnet build ProjectArk.Combat.csproj` 编译验证，新增 0 个 error、0 个 warning（`PoolManager.cs` 的 `GetInstanceID` 和 `FindObjectsByType` 的 deprecation warning 为 pre-existing）。stash/pop 确认了剩余 1 个 pre-existing `StarChartSaveSerializer.cs` 错误在本次改动前已存在。

---

## StarChart 特效层审查（基于 ProceduralPresentation Workflow Spec） — 2026-04-26 11:20

- **修改文件**
  - `Docs/6_Diagnostics/StarChart_Components_Inventory.md`（新增 §9 特效审查章节；原 §9 参考锚点改为 §10 并追加 VFX 代码锚点）
- **内容**
  - 以 `Docs/3_WorkflowsAndRules/Project/ProceduralPresentation_WorkflowSpec.md` 的 6 项立项检查 + 6 项验收标准为审查框架，遍历 8 个 Cores 的 `_muzzleFlashPrefab` / `_impactVFXPrefab` / `_fireSound` 字段，确认**全部 8 个 Core 三项 VFX/Audio 字段均为空**。
  - 审查三个投射物 prefab 的视觉现状：`Projectile_Matter` 只有 SpriteRenderer + `Projectile.Awake` 里程序化补的 TrailRenderer；`LaserBeam_Light` 只有内建色/宽曲线的 LineRenderer；`EchoWave_Echo` 只有一个空 Sprite 的单色方块（甚至没 sprite 资源）。
  - 发现 `Projectile.cs` Awake 中存在未被文档化的**静默程序化 fallback**：SpriteRenderer 缺 sprite 时 `CreateFallbackSprite(8)` 程序化造 8px 占位；TrailRenderer 缺失时 `AddComponent<TrailRenderer>()` + `ConfigureTrail` 硬编码参数。按 Workflow Spec §3.5 / §5.2，这属于"silent fallback"+"gameplay 脚本直接操作 renderer"两条硬违反。
  - 顺带梳理 Prism / Sail / Satellite 的特效生态位：Tint Prism 激活后玩家**无任何屏幕内视觉反馈**（Frost Slow 没有冷色 tint、Homing 靠弹道轨迹自然可读），是当前最大的可读性债。
  - 输出 7 条按 ROI 排序的特效债务清单（P0 × 2 / P1 × 2 / P2 × 2 / P3 × 1），并给出 5 步最小可玩增量路径：给 4 family 各配占位 MuzzleFlash + ImpactVFX、填 4 基线 Core 的 SO 字段、给 Projectile fallback 加 warning、给 StarChartAuditor 加"Core has no MuzzleFlashPrefab"规则。
  - §10 参考锚点追加 4 条新锚点：`StarCoreSO.cs:52-60`（字段定义）、`ProjectileSpawner.cs:213-232`（muzzle + fire sound 消费）、`Projectile.cs:227-233`（impact 消费）、`Projectile.cs:67-85`（程序化 fallback 位置）。
- **目的**
  - 按 Workflow Spec 的 authority（"程序化表现必须可诊断，替换缝必须显式存在，fallback 必须白纸黑字"）对 Cores 家族做一次特效层纵切，确认替换缝架构合规但执行为零，把"下一步该做什么特效工作"从模糊印象固化为带优先级和验收标准的清单。
  - 把 `Projectile.Awake` 里未文档化的程序化 fallback 正式暴露为待治理项——此前它一直以"静默兜底"形式运行，审查中首次被归类为 "silent no-op 违反"。
- **技术**
  - 审查手段：`.asset` YAML 字段直读（16 处 `_muzzleFlashPrefab` / `_impactVFXPrefab` / `_fireSound` 全为 `{fileID: 0}`）+ 代码交叉验证（`StarCoreSO.cs` 字段声明、`SnapshotBuilder.cs` 消费点、`ProjectileSpawner.cs` 消费点、`Projectile.cs` fallback 实现）+ prefab 组件结构审查（`Projectile_Matter` / `LaserBeam_Light` / `EchoWave_Echo`）。
  - 审查框架：Workflow Spec 的 §4 六问立项检查 + §7 六条验收标准，逐条对 Cores 打分得到"契约合规 60% / 执行合规 0%"的结论。
  - 诊断输出格式沿用 §0.0 总览表的 emoji 等级制（🟢/🟡/🔴/⚪），新增⬜️ 表示"字段为空"。

---

## CoreLoop.md v1.0 定稿锁定 — 2026-04-23 17:11

- **修改文件**
  - `Docs/1_GameDesign/CoreLoop.md`
- **内容**
  - 将 `CoreLoop.md` 从 v0.1 Hybrid Draft 升级为 v1.0 锁定版：
    - 头部版本标识改为 `v1.0（已锁定）` + 2026-04-23 定稿日期，并写入一句话共识："在大静默的星球上考古，用敌人的思维方式开火。"
    - §1.1 主轴锁定为**候选 B — 探索主轴"听见，再进入"**，补写三条硬理由（World Bible 基调一致性 / 支柱 I 的节奏需求 / 禁忌清单的暗示）和必须承认的三项长期代价。
    - §1.2 节拍图从"推荐版"转为"已锁定（基于探索主轴）"。
    - §1.3 锁定官方一句话 Loop 为 21 字版本，附 5 关键词决策矩阵（大静默 / 星球 / 考古 / 敌人的思维方式 / 开火）与反向用法约束。
    - §5 把"下一步"从待定改为三项可执行清单：立即生效的判断基准、下一份推荐起草的 `Moment_to_Moment_Combat.md`、三项需要补齐的模块短板（声学线索 / 掉落系统 / 星图局部小界面）。
    - 新增附录 C，归档被 v1.0 否决的两个主轴候选（A 战斗主轴、C 构筑主轴）、两条被否决的一句话候选及其否决理由、v1.0 决策时间线（2026-04-22 起草 → 2026-04-23 锁定）。
    - 全文清除 ❓ 待定标识，替换为 ✅ 已锁定 / 🔵 推荐版 二分标注。
- **目的**
  - 让"Project Ark 是什么游戏"在团队内部有一个可被引用的唯一答案，后续功能排期、UX 决策、美术方向都有统一尺子。
  - 把"战斗模块已完成但探索模块欠债"的认知矛盾显式化——通过锁定探索主轴，把后续开发重心指向声学线索、环境叙事、掉落系统等当前短板。
  - 为下一份待起草的 `Moment_to_Moment_Combat.md` 提供明确的约束上下文：战斗是脉冲而非主体，详细参数设计必须服务 §1.2 节拍图中 20-40s 这段。
- **技术**
  - 文档定稿工作流：候选并列 → 制作人裁决 → 锁定主干 + 历史存档双轨写入。
  - 决策留痕：未被选中的候选保留完整论证和否决理由，为未来可能的 v2.0 重审提供上下文，避免"当初为什么不选 X"这类问题需要重新讨论。

---

## Docs migration third-pass active-doc alignment — 2026-04-14 18:28

- **修改文件**
  - `Docs/0_Plan/ongoing/LevelArchitect_Workbench.md`
  - `Docs/0_Plan/ongoing/LevelRoomRuntimeChain_Hardening.md`
  - `Docs/0_Plan/ongoing/ShipVFX_MigrationPlan.md`
  - `Docs/2_TechnicalDesign/Level/Level_CanonicalSpec.md`
  - `Docs/3_WorkflowsAndRules/LevelArchitect/Level_WorkflowSpec.md`
  - `Docs/6_Diagnostics/Verification_Checklist.md`
  - `Docs/6_Diagnostics/LevelArchitect_SupportedElements_Matrix.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-04.md`
- **内容**
  - 清理 `0_Plan`、`3_WorkflowsAndRules`、`6_Diagnostics` 与 `2_TechnicalDesign` 中仍会误导当前使用者的旧入口描述，把 `ImplementationLog.md` 导航切换为新的日志索引或当前月度日志，并统一 `Level Architect` 的现役工作面口径为 `Build / Quick Edit / Validate`。
  - 将 `LevelDesigner.html` 的现役操作入口统一收口到 `Build` 工作面的 `Optional Draft & Import` 区，避免继续沿用已经不存在的 `Design` Tab 指引。
  - 保留 `ImplementationLog.md` 作为历史总账，不重写旧记录正文，只修正现役文档中的错误导航与当下操作说明。
- **目的**
  - 让新 `Docs` 主树不仅目录结构完成迁移，现役计划、工作流、诊断和技术规范层也能给出一致的真实入口。
  - 避免团队成员按照旧 Tab 名称或旧日志文件名执行当前流程，减少文档层面的误导航成本。
- **技术**
  - 文档收口治理：以“现役说明修正、历史记录保留”为原则做第三轮最小改动对齐。
  - 导航一致性修复：统一 `Level` 相关文档中的工作面命名、日志入口与 `LevelDesigner` 导入入口表述。

## Docs migration second-pass cleanup — 2026-04-14 18:19

- **新建文件**
  - `Docs/7_Reference/TechNotes/UnityMCP_Troubleshooting.md`
- **删除文件**
  - `Docs/7_Reference/TechNotes/UnityMCP-Troubleshooting.md`
- **修改文件**
  - `Docs/9_Superpowers/specs/2026-04-10-plan-doc-structure-design.md`
  - `Docs/9_Superpowers/plans/2026-04-10-plan-doc-structure-rollout.md`
  - `Docs/9_Superpowers/plans/2026-04-14-level-architect-hazards-starter-first-implementation-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-04.md`
- **内容**
  - 清理 `9_Superpowers` 元文档中残留的旧 `Docs` 主树路径与失效文件名，让 AI 规格稿和执行计划稿在引用现役文档时统一指向新的职责目录。
  - 将 `UnityMCP-Troubleshooting.md` 统一重命名为 `UnityMCP_Troubleshooting.md`，使 `7_Reference/TechNotes` 下的英文文件名继续保持下划线分词风格。
  - 收掉元文档中的最后几处旧目录语义和无效校验命令，使第二轮迁移后的全局检索结果更干净。
- **目的**
  - 让新主树不仅物理迁移完成，而且在元文档层也不再继续传播旧路径和旧命名。
  - 把第二轮迁移收尾控制在小范围修正内，避免历史执行稿继续成为错误导航源。
- **技术**
  - 文档收尾治理：针对 `9_Superpowers` 做定向路径替换与历史语义去旧。
  - 命名统一：通过文件重命名补齐 `TechNotes` 下的英文下划线词法一致性。

## Docs architecture migration rollout — 2026-04-14 17:56

- **新建文件**
  - `Docs/README.md`
  - `Docs/0_Plan/README.md`
  - `Docs/1_GameDesign/README.md`
  - `Docs/2_TechnicalDesign/README.md`
  - `Docs/3_WorkflowsAndRules/README.md`
  - `Docs/4_GameData/README.md`
  - `Docs/5_ImplementationLog/README.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-04.md`
  - `Docs/6_Diagnostics/README.md`
  - `Docs/7_Reference/README.md`
  - `Docs/8_Obsolete/README.md`
  - `Docs/9_Superpowers/README.md`
- **修改文件**
  - `Docs/0_Plan/Project_Plan.md`
  - `Docs/0_Plan/ongoing/LevelRoomRuntimeChain_Hardening.md`
  - `Docs/0_Plan/ongoing/LevelArchitect_Workbench.md`
  - `Docs/0_Plan/ongoing/ShipVFX_MigrationPlan.md`
  - `Docs/0_Plan/complete/ShipVFX_PhaseA.md`
  - `Docs/0_Plan/ongoing/README.md`
  - `Docs/0_Plan/complete/README.md`
  - `Docs/1_GameDesign/Ark_MasterDesign.md`
  - `Docs/2_TechnicalDesign/Level/Level_CanonicalSpec.md`
  - `Docs/3_WorkflowsAndRules/LevelArchitect/Level_WorkflowSpec.md`
  - `Implement_rules.md`
  - `Docs/5_ImplementationLog/ImplementationLog.md`
- **内容**
  - 按新的职责主树创建 `Docs` 总索引与各一级目录 README，正式建立 `0_Plan / 1_GameDesign / 2_TechnicalDesign / 3_WorkflowsAndRules / 4_GameData / 5_ImplementationLog / 6_Diagnostics / 7_Reference / 8_Obsolete / 9_Superpowers` 的导航骨架。
  - 将现役设计正文、技术真相源、工作流规范、结构化数据和历史归档文档迁移到新目录，并统一为英文文件名、下划线分词风格。
  - 将 `1_GDD / 2_Design / 3_LevelDesigns / 4_DataTables` 的现役主内容重新落位，同时把旧版需求稿、旧 JSON、旧 checklist、旧状态快照和历史方案归入 `8_Obsolete` 对应职责分区。
  - 建立新的月度实现日志文件，并保留原有 `ImplementationLog.md` 作为历史总账。
- **目的**
  - 让每个目录只回答一种问题，降低“文档在哪、当前真相在哪、旧稿在哪”的查找成本。
  - 把策划正文、技术真相源、authoring 规则、游戏数据、实时诊断和历史归档彻底拆开，避免后续继续混放。
- **技术**
  - 文档架构重构：按职责分区、按主题归档、按稳定命名词法重建 `Docs` 主树。
  - 导航治理：使用总索引 + 一级目录 README + 相对路径链接，建立更稳定的索引体系。
---

## SpaceLifeDialogueCoordinator — WorldProgressManager 降级为可选依赖 — 2026-04-22 22:39

- **修改文件**
  - `Assets/Scripts/SpaceLife/Dialogue/SpaceLifeDialogueCoordinator.cs`
- **内容**
  - 把 `WorldProgressManager` 从 `AuditDependenciesOnStart()` 的硬依赖列表中移除，不再作为 Start 期审计的必检项。
  - 在 audit 函数中补注释说明该依赖是有意保持为可选（intentionally optional），并解释 fallback 链路：优先 `WorldProgressManager.CurrentWorldStage`，缺席时降级到 `PlayerSaveData.Progress.WorldStage`，无存档时回落到 0。
  - 同步为 `ResolveWorldStage()` 补完整 XML doc，点明"full-flow integration"与"standalone SpaceLife demo"两种运行语义。
- **目的**
  - 让 SpaceLife 作为独立 hub 切片能在没有 Level 模块运行时管理器的场景中直接跑通对话 demo，无需强制挂载 `WorldProgressManager`。
  - 消除 audit 的硬依赖判定与 `ResolveWorldStage()` 的软依赖处理之间的自相矛盾：之前 audit 会直接禁用组件，导致即便 fallback 代码已写好也走不到。
  - 遵循 Implement_rules.md §3.7 "Runtime/Editor/Scene 三层职责隔离"——SpaceLife 模块不应强制依赖 Level 模块的管理器存在；未来完整流程整合时挂上 `WorldProgressManager` 自动生效，零迁移成本。
- **技术**
  - 可选依赖模式：字段保留 + ServiceLocator 解析保留 + audit 移除 + 运行时 null-check fallback 已覆盖两种路径。
  - 保持 `ServiceLocator.Get<WorldProgressManager>()` 解析调用不变，让 Level 模块存在时仍然能接管。

---

## StarChart_Components_Inventory 顶部总览表 — 2026-04-26 10:33

- **修改文件**
  - `Docs/6_Diagnostics/StarChart_Components_Inventory.md`
- **内容**
  - 在元信息段之后、§0 TL;DR 之前，新增 §0.0 "全部件一览（21 项，一目了然）" 单张表格：按 Cores (8) → Prisms (9) → Sails (3) → Satellites (1) 顺序列出全部 21 个现役部件，列项包括序号 / 类型 / Asset 名 / DisplayName / 家族或类别 / Shape 或行为 prefab / 等级 / 行为通道（现役）。
  - 表头附等级图例（🟢 / 🟡 / 🔴 / ⚪）和一句话速览："🟢13 ／ 🟡6 ／ 🔴0 ／ ⚪1，Prism 行为通道真实激活数 = 2"。
- **目的**
  - 让读者打开文档第一屏即可扫完全部 21 个部件的类型、家族、现役运行路径和完成度，无需翻到 §2-§5 拼图。
  - 满足用户"最上方加一张一目了然的表格"的阅读入口诉求，同时保持 §2-§5 继续承担"字段证据 + Action Items + 治理决策"的深度职责，避免表格变成第二份真相源。
- **技术**
  - 仅文档调整，无代码/资产改动；不引入新 owner 或新真相源字段，所有数据与 §2-§5、§6 红绿灯总览保持一致。

---

## StarChart 家族级 VFX 占位 prefab + 4 基线 Core 字段填充 — 2026-04-26 13:40

- **新建文件**
  - `Assets/Scripts/Combat/Editor/CoreVFXPrefabCreator.cs`：一键 Editor 工具，菜单 `ProjectArk/Create Core VFX Placeholder Prefabs`，按 4 个 CoreFamily（Matter / Light / Echo / Anomaly）的视觉 DNA 程序化生成 8 个占位 VFX prefab。
  - `Assets/_Prefabs/VFX/Core/MuzzleFlash_Matter.prefab` + `ImpactVFX_Matter.prefab`
  - `Assets/_Prefabs/VFX/Core/MuzzleFlash_Light.prefab` + `ImpactVFX_Light.prefab`
  - `Assets/_Prefabs/VFX/Core/MuzzleFlash_Echo.prefab` + `ImpactVFX_Echo.prefab`
  - `Assets/_Prefabs/VFX/Core/MuzzleFlash_Anomaly.prefab` + `ImpactVFX_Anomaly.prefab`
- **修改文件**
  - `Assets/_Data/StarChart/Cores/MatterCore_StandardBullet.asset`：`_muzzleFlashPrefab` / `_impactVFXPrefab` 填入 Matter 家族 prefab。
  - `Assets/_Data/StarChart/Cores/LightCore_BasicLaser.asset`：填入 Light 家族 prefab。
  - `Assets/_Data/StarChart/Cores/EchoCore_BasicWave.asset`：填入 Echo 家族 prefab。
  - `Assets/_Data/StarChart/Cores/AnomalyCore_Boomerang.asset`：填入 Anomaly 家族 prefab。
- **内容**
  - `CoreVFXPrefabCreator`：内部以 `FamilyProfile` 封装每家族的 `PrimaryColor / SecondaryColor / Lifetime / Speed / Burst / Size / ShapeType / Radius`；为 Muzzle 与 Impact 分别配置 `main / emission / shape / colorOverLifetime / sizeOverLifetime / renderer` 6 个 ParticleSystem 模块；使用 Unity built-in `Default-Particle.mat` 做材质，无美术依赖；`PrefabUtility.SaveAsPrefabAsset` 落地到 `Assets/_Prefabs/VFX/Core/`。
  - 每个 prefab 挂 `ParticleSystem` + `PooledVFX`，与现有 `WeaponTrack.GetMuzzleFlashPool` / `ProjectileSpawner.SpawnMuzzleFlash` / `Projectile.SpawnImpactVFX` 的池化链路无缝对接（`PoolReference` 由 `PoolManager` 池化时自动 AddComponent）。
  - 4 基线 Core SO 通过 `SerializedObject.FindProperty("_muzzleFlashPrefab") / ("_impactVFXPrefab")` + `ApplyModifiedPropertiesWithoutUndo` + `EditorUtility.SetDirty` + `AssetDatabase.SaveAssets` 完成字段填充。
  - 审计结果：4/4 基线 Core 全部 `[OK]`（Muzzle + Impact 均就绪），WARN=0。
- **目的**
  - 完成 §9.6 最小可玩增量路径的最后两步（Step 4 + 5）：把"family × VFX prefab → Core SO → 发射管线"的替换缝跑通，让后续星图重构有一个可被替换/覆盖的占位基线，而不是一直靠 null 兜底。
  - 让 `StarCoreVFXAuditor` 对 4 基线 Core 不再 warn，聚焦剩余 4 个变体 Core 的 hole。
  - 遵循 Implement_rules.md §3.1 "单一真相源"：VFX 资产由唯一一个 Editor 工具（`CoreVFXPrefabCreator`）生成，Runtime / 对象池只消费，不补资产。
- **技术**
  - 程序化 ParticleSystem 配置：main/emission/shape/colorOverLifetime/sizeOverLifetime/renderer 6 模块；4 family 靠 `startColor` + `Gradient` + `ParticleSystemShapeType`（Cone / Circle / Sphere）+ `startSize` 做视觉区分。
  - SerializedObject 资产字段修改（保持 Unity 推荐的 Undo-safe 路径）；`ApplyModifiedPropertiesWithoutUndo` 因为这是批量 bootstrap、无需 Undo 堆栈。
  - 对象池接入：`PooledVFX.OnGetFromPool` 自动播放粒子、Duration 到期自动回收，无需新增池化代码。
  - Unity 6 domain reload 不保证陷阱：新 Editor 菜单在 script compilation 后未必立即注册到 AppDomain，故本轮通过 Unity MCP `execute_code` 内联执行生成逻辑完成 bootstrap；后续用户主动 focus Unity 触发 domain reload 后，`ProjectArk/Create Core VFX Placeholder Prefabs` 菜单即可正常使用（用于未来重建 / 调参）。

---

## 修 StarChartSaveSerializer struct null-check 编译错误 — 2026-04-26 14:56

- **修改文件**
  - `Assets/Scripts/Combat/StarChart/StarChartSaveSerializer.cs`（第 66 行附近）
- **内容**
  - 将 `_context = context ?? throw new ArgumentNullException(nameof(context));` 改为 `_context = context;`，并补一行说明注释：`StarChartContext is a readonly struct — cannot be null, so no null-check is applicable.`
- **目的**
  - 消除长期存在的编译错误 `CS0019: Operator '??' cannot be applied to operands of type 'StarChartContext' and '<throw expression>'`。
  - 该错误是在 L3-1 Phase A 把 `StarChartContext` 从 class 改为 `readonly struct` 后遗留的构造器 null-check 笔误——struct 不可能为 null，`??` 自然无意义。
  - 在上一轮（VFX 占位收尾）中被标记为 "pre-existing、与本轮无关、留给后续 StarChart 重构 Batch"；本轮用户主动要求先处理，顺手消除掉。
- **技术**
  - 纯局部修正，不改变任何运行时行为（struct 本身就永远不为 null）。
  - 其它 5 个 `?? throw new ArgumentNullException(...)` 校验（`_loadouts` / `_lightSailRunners` / `_primarySatRunners` / `_secondarySatRunners` / `_disposeSlotRunners` / `_initializeAllPools`）都是引用类型，保持不变。
  - `dotnet build ProjectArk.Combat.csproj` 验证：0 错误、3 个无关的 Unity 6 废弃 API 警告（`GetInstanceID` / `FindObjectsSortMode`）。


---

## StarChart Debug 槽位字段支持可扩可缩 + 最小值 1 — 2026-04-26 15:32

- **修改文件**
  - `Assets/Scripts/Combat/StarChart/SlotLayer.cs`（新增 `TryShrinkColumn` / `TryShrinkRow` / 私有 `EvictItemsOutOfBounds`）
  - `Assets/Scripts/Combat/StarChart/WeaponTrack.cs`（`SetLayerCols` / `SetLayerRows` 最小值 2→1，改为可扩可缩；新增内部 `ResizeCols` / `ResizeRows`）
  - `Assets/Scripts/Combat/StarChart/LoadoutManager.cs`（`SetSailLayerCols` 改为可扩可缩）
  - `Assets/Scripts/Combat/StarChart/StarChartController.cs`（`SetSailLayerCols` 文档同步）
  - `Assets/Scripts/UI/TrackView.cs`（Tooltip 文案更新；新增 `[ContextMenu] Apply Debug Slot Counts` / `Reset Debug Slot Counts`）
  - `Assets/Scripts/UI/StarChartPanel.cs`（Tooltip 文案更新；新增 `[ContextMenu] Apply Debug SAIL Cols` / `Reset Debug SAIL Cols`）

- **内容**
  - `SlotLayer<T>` 之前只有 `TryUnlockColumn` / `TryUnlockRow`（单调扩展），现在对称补上 `TryShrinkColumn` / `TryShrinkRow`。收缩时通过 `EvictItemsOutOfBounds(newCols, newRows)` 先把 **footprint 越出新边界** 的 item 全部 `RemoveItem` 驱逐，再调整 `Cols` / `Rows`，保证状态一致。
  - `WeaponTrack.SetLayerCols` / `SetLayerRows` 原逻辑是 `while (layer.Cols < target) TryUnlockColumn()`，只能扩不能缩。现在改为：
    - 先 `while (layer.Cols < target) TryUnlockColumn()` 扩展
    - 再 `while (layer.Cols > target) TryShrinkColumn()` 收缩
    - 任意 TryXxx 返回 false 即 break，防止死循环
  - 最小值从 2 统一降为 1（`SetLayerCols`），语义上 1 列×1 行也是合法状态，和 Inspector `[Range(0, 4)]` 的 1 完全对齐。
  - `LoadoutManager.SetSailLayerCols` 对称改造，调用新的 `TryShrinkColumn`。
  - `TrackView` / `StarChartPanel` 的 Tooltip 明确写清：**"Supports BOTH expand and shrink. Shrinking evicts items whose footprint no longer fits."**
  - 两边都新增 `[ContextMenu]` 方法：`Apply Debug ...` 在 Play Mode 中调完 Inspector 字段后手动触发重算并 `Refresh()`；`Reset Debug ...` 把字段清零并把 layer 缩回默认的 2x1。

- **目的**
  - 支持调试流程中随意扩缩 slot 数量，不再因为底层单调扩展导致"改 Inspector 没反应，必须清存档"。
  - 统一四类 layer（Core / Prism / SAT / SAIL）的最小值为 1，Inspector 显示范围和实际生效范围完全一致，消除先前审查中发现的"填 1 却被静默提升到 2"的语义断层。
  - 让 SAIL 和 Core/Prism/SAT 的可扩缩能力对称，`StarChartController.SetSailLayerCols` 的公共 API 语义也随之更新。

- **技术**
  - 收缩时的驱逐策略：用 `ItemShapeHelper.GetCells` 枚举每个 item 占据的 cell，判定是否全部落入 `[0, newCols) × [0, newRows)`，不满足者整件 `RemoveItem`。先收集到 `List<T>`，避免在 `_itemList` 迭代中 mutate。
  - 不破坏现有调用方：`SetLayerCols(2, 2, 2)` / `SetLayerRows(1, 1, 1)` 的默认值和语义未变，只是现在当 current > target 时会主动缩回去，而不是静默不动。
  - 存档路径（`ImportTrack` / `ExportTrack`）没有改动，仍然走相同的 `SetLayerCols`。如果存档值比当前小，现在会被 **缩回**，这是一致性修复；如果更大，行为和之前一样扩展。
  - 编译验证：`dotnet build Project-Ark.slnx` 全通过，0 错误；所有警告均为 Unity 6 既有的 `GetInstanceID / FindObjectsSortMode` 过时 API 提示，与本次改动无关。


---

## TrackView Debug Row Overrides（对称 Col 的 Row 调试字段） — 2026-04-26 16:12

- **修改文件**
  - `Assets/Scripts/UI/TrackView.cs`（新增 `_debugCoreRows` / `_debugPrismRows` / `_debugSatRows` 三个 `[Range(0,4)]` 字段；`ApplyDebugSlotCounts()` 追加 rows 分支，对称调用 `WeaponTrack.SetLayerRows`；`ResetDebugSlotCounts()` 清零新字段并调用 `SetLayerRows(1,1,1)` 复位）
- **内容**：TrackView 之前只暴露 `_debugCoreCols / _debugPrismCols / _debugSatCols`，而 `WeaponTrack.SetLayerRows` API 早已就绪但从未被 UI 调用过。对称补齐 rows 维度，右键 `Apply Debug Slot Counts` 会同步调 Cols 和 Rows。
- **目的**：用户反馈"Prism 改为 4 列后 Homing 依然放不进去"，根因是 Homing 形状是 `Shape2x1V`（2 行高），Prism 层始终只有 1 行。补 Row debug 后可把 Prism 设为 `2×2` / `2×1` 等多行布局以容纳垂直形状。
- **技术**：对称 pattern；`_track.CoreLayer.Rows` 作为 0 值的 fallback（跟 Cols 完全一致的语义）。
- **已知物理约束（未治理）**：`TypeColumn._cells` 是固定 4 格 Prefab（代码注释仍写 "2×2 grid"），视图层上限永远是 `Rows*Cols ≤ 4`。也就是说 `1×4` / `2×2` / `4×1` 可行，但数据层虽然支持的 `4×2=8`、`3×3=9` 等组合**视图无法表达**，第 5+ 格会 silent 丢失。对应"方案 B 的可配合形状集合"实际为所有 `Rows*Cols≤4` 的布局。若未来要突破 4 格上限，需把 `TypeColumn` 的 cells 改为动态生成 + GridLayoutGroup 自动排版。

---

## TrackView / StarChartPanel UI 网格形状对齐逻辑真相（constraintCount 跟随 Cols） — 2026-04-27 11:05

- **修改文件**
  - `Assets/Scripts/UI/TrackView.cs`（`RefreshColumn<T>()` 内新增 constraintCount 同步块）
  - `Assets/Scripts/UI/StarChartPanel.cs`（`RefreshSharedSailColumn()` 内对称新增同步块）
- **内容**：`GridLayoutGroup.constraintCount` 原本在 `UICanvasBuilder` 构建 UI 时硬编码写死为 2，导致 UI 视觉形状与逻辑层 `(Cols, Rows)` 真相脱节——例如 `_debugCoreCols=4` 且 `_debugCoreRows=1` 时，UI 会把 4 个线性格子折成视觉上的 2×2 假象，而 `ItemShapeHelper.FitsInGrid` 读的仍是 (4,1)，L 型（需 2 行）永远装不进去。本次改动让刷新时强制 `constraintCount = layer.Cols` 并立即 `LayoutRebuilder.ForceRebuildLayoutImmediate`，所见即所得。
- **目的**：修复用户报告的 "debug cols=4 时 UI 显示 2×2 但无法装 L 型" bug，消除"显示真相源 ≠ 逻辑真相源"的架构违规（对应 `Implement_rules.md` 3.1 条：单一真相源）。
- **技术**：每次 `RefreshColumn` / `RefreshSharedSailColumn` 进入时比较当前 `constraintCount` 与 `layer.Cols`，不等时写入 `FixedColumnCount` 约束并立即强制重建布局，保证后续 overlay 定位时 cell anchoredPosition 已是新值；仅在值变化时触发重建，避免每帧无谓开销。Core/Prism 的 `DragHighlightLayer` 已通过 `SetShapeHighlight` 里的 `SetGridCols(layer.Cols)` 自行同步，无需改动；SAIL 只用 `ShowHighlightAtCellIndex`（不依赖 gridCols），也无需改动。
- **现在的行为对照表**：
  - `Cols=4, Rows=1` → UI 显示 4×1 一字排开（真实逻辑）
  - `Cols=2, Rows=2` → UI 显示 2×2 方形（真实逻辑）
  - `Cols=4, Rows=2` → UI 显示 4×2（8 格，超过 `TypeColumn._cells` 长度 4 的部分会被 CanvasGroup 隐藏，这是另一条物理约束，已在 2026-04-26 条目中记录）
- **视觉副作用**：`Cols` 越大一行越宽，TypeColumn 容器视觉宽度由 anchor 占比决定（约 33%）；单格 80px 情况下 `Cols=4` 需要 ~326px，若面板总宽不足需要美术侧拉宽列容器或缩小单格尺寸——这是 UI 布局问题，不影响逻辑正确性。

---

## TypeColumn._cells 动态伸缩（突破 4 格上限） — 2026-04-27 11:34

- **修改文件**
  - `Assets/Scripts/UI/TypeColumn.cs`（新增 `EnsureCellCapacity(int, TrackView)` 方法）
  - `Assets/Scripts/UI/TrackView.cs`（`RefreshColumn<T>()` 在 constraintCount 同步前调用扩容；`InitColumn` 抽出 `WireColumnCells` 供扩容后补订阅使用）
  - `Assets/Scripts/UI/StarChartPanel.cs`（`Bind()` 中 SAIL 连线逻辑抽成 `WireSailCells()`；`RefreshSharedSailColumn` 在扩容后重新调用它）
  - `Assets/Scripts/UI/SlotCellView.cs`（新增 `IsTrackCellWired` 幂等标志位）
- **内容**：`TypeColumn._cells` 原本硬编码长度 4（`UICanvasBuilder.BuildTypeColumn` 构建期一次性创建 4 个 Cell_i 并 `WireArrayField`）。当逻辑层 `SlotLayer.Rows * Cols > 4` 时（如 4×2=8、3×3=9），第 5+ 格的 `SetItem` 调用会被 `cellIndex < cells.Length` 守卫静默跳过，表现为"拖进去就消失"。本次改造让 cells 数量在运行时跟随 `layer.Rows * layer.Cols` 伸缩：扩容时以 `cells[0]` 为 Unity `Instantiate` 模板克隆（自动 remap 内部子节点引用），缩容时销毁尾部多余 cell。
- **目的**：消除 UI 侧的容量真相源瓶颈，支持任意 `(Cols, Rows)` 组合，为后续 CORE 3×3、PRISM 4×2、SAT 2×3 等大容量需求打开空间。对应 `Implement_rules.md` 3.1 条（单一真相源）——cells 数量必须跟随逻辑层，不得保留"固定 4 格"的第二真相源。
- **技术**：
  1. **克隆而非独立构建**：`EnsureCellCapacity` 用 `Object.Instantiate(cells[0].gameObject, parent)` 克隆模板 cell，零维护成本——`BuildSlotCell` 将来怎么改（增加 SpriteMask、SpriteRenderer、额外子节点等），克隆出来的新 cell 会自动跟着变，不会出现"Editor 构建路径和运行时构建路径漂移"的双轨问题。
  2. **Unity Instantiate 的引用 remap 特性**：`SlotCellView._backgroundImage / _iconImage / _button / _placeholderLabel` 指向的是自身的子 GameObject，Instantiate 会把这些引用自动映射到新副本的对应子节点，而非共享原 cell 的子节点（Unity 编辑器 Prefab 实例化的标准行为）。因此每个克隆 cell 的视觉组件是独立的。
  3. **事件订阅幂等化**：扩容后的新 cell 没有继承 C# event 订阅（Unity Instantiate 只复制序列化字段，不复制 delegate 链），所以必须在扩容后重跑一次 wiring。为防止重复订阅，TrackView 引入 `SlotCellView.IsTrackCellWired` 标志位；SAIL 列的 `WireSailCells` 则先 `-=` 再 `+=` 实现幂等。
  4. **SAIL 列的特殊性**：SAIL cells 不走 `TrackView.InitColumn` 路径（`OwnerTrack=null`），需要在 `StarChartPanel.Bind` 中直接注入 `HasSpaceForItem` 委托、订阅 pointer 事件、并将 `cells` 数组缓存到 `_sailHighlightLayer`。这 3 类 wiring 抽成 `WireSailCells()`，扩容后重新调用一次（重新 Initialize 高亮层 + 重新订阅事件 + 为新 cell 注入委托）。
  5. **布局顺序**：`EnsureCellCapacity → WireColumnCells → 设置 constraintCount → LayoutRebuilder.ForceRebuildLayoutImmediate`，确保布局重建时已经有完整的 cell 集合。
- **验收**：
  - `_debugCoreCols=3, _debugCoreRows=3` → Core 列显示 9 格，可装下 T 型 / 十字 / L 型
  - `_debugCoreCols=4, _debugCoreRows=2` → 8 格一字两排全部可见（旧版本被 CanvasGroup 隐藏第 5-8 格）
  - 从大容量缩回 2×2 → 多余 cell 被销毁，无残留
  - 拖拽、hover 高亮、tooltip、overlay 定位在新扩出来的 cell 上正常工作
- **遗留**：`UICanvasBuilder.BuildTypeColumn` 初始仍创建 4 个 cell，这是"默认容量"，运行时会被 `EnsureCellCapacity` 覆盖。若以后希望更彻底地消除初始硬编码，可以把初始数量也读 `SlotLayer<T>.DEFAULT_COLS × DEFAULT_ROWS`，但属于锦上添花，不是必要改动。

---

## StarChart 数据管线设计文档（套餐 C 三份文档） — 2026-04-27 14:58

- **新建文件**
  - `Docs/2_TechnicalDesign/Combat/StarChart_DataPipeline.md`（主文档，全套架构规范）
  - `Docs/2_TechnicalDesign/Combat/ProjectileMovement_Library.md`（H3 方案的 Movement 组件库设计）
  - `Docs/2_TechnicalDesign/Combat/StarChart_DataPipeline_Plan.md`（分 7 Phase 实施计划，16-22h 工作量）
- **内容**：选择性借鉴 Magicraft 数据驱动架构，交付套餐 C 激进方案的设计蓝图——包含 5 项能力：(1) CSV 单向导入（CSV → SO），(2) StarChartRegistry 静态 O(1) 查询，(3) CoreArchetype 枚举 + Movement 组件库（H3 方案：Movement 独立类 + Prefab 装配 + MovementParams 分号串覆盖参数），(4) 多语言字段占位（zh/en 双列），(5) 双向同步（SO → CSV 反向导出）。
- **目的**：当前 CSV（13 Core + 18 Prism）与 SO 未对接，策划在 CSV 写数据、代码读 SO，形成数据孤岛；部件规划总量 90 个（Planning.csv），SO 手动维护会崩。建立 CSV 为权威数据源、SO 为运行时缓存的分层架构，满足 30+ 部件规模下的可维护性。对应 Implement_rules.md 3.1（单一真相源）与 3.5（宁可响亮失败）。
- **技术**：
  1. **借鉴 Magicraft 3 招**：POCO + 静态字典（Registry）、Copy() 运行时可变副本（我们的 CoreSnapshot 已等价实现）、abilityType 枚举分派（变种为 CoreArchetype）。
  2. **排除 Magicraft 3 个坏味道**：`float1/float2/float3` 万能槽（换语义字段）、Enhance 的巨型 switch（我们的 IProjectileModifier 已更优）、字符串 Resources.Load（改 AssetDatabase 路径搜索 + Registry 缓存）。
  3. **H3 Movement 组件库方案**：接口 `IProjectileMovement { OnSpawn / OnUpdate / OnReturnToPool }`；每个 Movement 独立类（StraightMovement / TrackingMovement / SerpentineMovement / BoomerangMovement / GravityMovement / ...），公开 `[SerializeField]` 参数；CSV 的 `MovementParams=Amplitude:5;Frequency:3` 通过反射的 `MovementParamApplier` 在 Spawn 时覆盖字段；同一 Prefab 可产多部件变体；对象池友好（组件常驻 Prefab，不 Runtime AddComponent）。
  4. **分号串协议**：`Field:Value;Field:Value` 统一用于 StatModifiers / MovementParams / BehaviorTags；未知枚举/字段名 LogError 不静默跳过。
  5. **Registry 设计**：启动时 `Resources.LoadAll<StarChartItemSO>("StarChart")` 扫描，Dictionary<string, SO> 缓存；SaveSystem 从"GUID 引用"升级为"string ID"，存档人类可读且支持部件改名。
  6. **执行顺序（7 Phase）**：P1 Importer 基础 + StarCore 单表 → P2 Movement MVP（2 种 Archetype）→ P3 Registry + Resources 目录迁移 → P4 补齐另 3 子类 Importer → P5 多语言 + SaveSystem 升级 → P6 反向导出 → P7 清理期（删旧兼容）。每个 Phase 独立可交付 + 明确验收项。
- **关键决策**：(1) 四张独立 CSV > 一张大表（列数合理、并行编辑无冲突）；(2) StatModifier 用分号串 > 展开成固定几组列（更灵活、上限可变）；(3) H3 > H1（纯 switch 违反 OCP） / H2（Prefab 爆炸）；(4) 套餐 C > A（只 CSV） / B（CSV+Registry+Archetype）——用户选择一次到位推到多语言占位 + 双向同步。
- **非目标**：本次仅交付设计文档，不动代码；Magicraft 20 个具体法术复刻留待后续 Batch；Movement 组件库仅定义接口与 MVP 2 个实现（Straight / Tracking），其余 8 个 Movement 延至部件内容制作阶段。
- **风险提示**：Phase 3 的 SO 迁移到 Resources 目录是最大风险点（可能破坏现有引用，需要 MoveAsset 而非复制+删除）；Phase 5 的 SaveSystem 升级需要保留至少 1 个月的旧 GUID fallback 兼容窗口。

---

## StarChart 数据管线文档红队审查修正（套餐 C → B+） — 2026-04-27 15:10

- **修改文件**
  - `Docs/2_TechnicalDesign/Combat/StarChart_DataPipeline.md`（v1.0 → v1.1，加修订历史 + 重写 §6 Registry + 改写 §7.3 + 替换 §8 + 更新 §10 §12）
  - `Docs/2_TechnicalDesign/Combat/StarChart_DataPipeline_Plan.md`（v1.0 → v1.1，改写 Phase 3 + 废除 Phase 6 + 更新时间线 §8 §10 §11）
  - `Docs/2_TechnicalDesign/Combat/ProjectileMovement_Library.md`（加修订历史，内容未变）
- **内容**：对刚出炉 1 小时的"套餐 C 激进方案"做红队审查，发现三个盲点并修正——(a) 下意识照搬 Magicraft 的 `Resources.LoadAll` 模式，忽视 Unity Editor 下 AssetDatabase 是更好选择；(b) 把 "可能用到的双向同步 Exporter" 包装成激进亮点，违背"垂直切片优先"的项目哲学；(c) 错过 Magicraft 最精巧的设计 `SlotData` 槽位元数据（拟态/封印/槽位级等级），但诚实评估 GDD 现阶段不需要，按 YAGNI 不引入。
- **目的**：避免先做完再后悔——红队审查是 pre-mortem 工具，在代码投入前质疑方案。按"先 Why 后 What"原则（Implement_rules.md 开发哲学 6），宁可文档阶段多花 30 分钟审查，也不在实施 10 小时后发现架构选错。
- **技术**：
  1. **Registry 方案重写**：从 `Resources.LoadAll<StarChartItemSO>("StarChart")`（要求所有 SO 迁移到 Resources 目录）改为 `StarChartManifest.asset`（ScriptableObject 承载 `StarChartItemSO[]` 清单）+ `AssetDatabase.FindAssets("t:StarChartItemSO")` 扫描。优点：SO 位置不变（现有引用不破）、不强制全量打包、未来切 Addressables 只需把 `StarChartItemSO[]` 改为 `AssetReference[]`。Importer 每次导入结尾自动刷新 Manifest，人工无需手改。
  2. **Exporter 延期**：用真实使用频率表否定"SO → CSV 反向同步"是高频需求；定义未来重启条件（连续 3 次 Inspector 改不回 CSV / 批量改动 > 20 条 / 策划主动请求），不满足前延期。
  3. **SlotData 不引入**：审查 Magicraft 的 `SlotData` 支持 `mimicSpellID`（拟态）+ `sealSlotOwner`（封印）+ `slotSpellExtraLevel`（槽位级加成），但我们 GDD 暂无这些机制需求，按 YAGNI 推迟。若将来需要，加一层 SlotData 预计 2-3h。
  4. **保留不变的部分**：CSV 四张独立表、分号串协议、CoreArchetype + H3 Movement 库、IProjectileMovement 接口、MovementParamApplier 反射覆盖、双语占位字段、string ID（ItemId 字段）——这些经得起红队审查。
  5. **工时节省**：套餐 C 原 16-22h → B+ 修正后 11-14h，节省 5-8h（删 Exporter 2-3h + 删 SO 搬家步骤 1-2h + Registry 方案简化 0-1h）。
- **方法论**：本次红队审查由用户主动触发（"请再次审查一遍我们选择的方案"），采用 the-fool 风格的批判性审查——不护着自己之前的推荐，用真实使用频率、YAGNI 原则、垂直切片哲学逐条拷问每个设计点。结果是方案精简 30%、风险下降（取消高风险的 SO 搬家）、交付速度提升。应成为后续大规模设计决策的标准工序。
- **决策责任人**：用户拍板 K1+L1+M2（接受修正 B+、不引入 SlotData、三份文档全改加修订历史）。
- **下一步**：方案已稳定。等用户决定何时启动 Phase 1（Importer 基础设施 + StarCores，3-4h 可完成 MVP）。

---

## StarChart 数据管线文档 v1.1 审阅 + v1.2 代码对齐修正 — 2026-04-27 16:10

- **修改文件**
  - `Docs/2_TechnicalDesign/Combat/StarChart_DataPipeline.md`（v1.1 → v1.2）
  - `Docs/2_TechnicalDesign/Combat/StarChart_DataPipeline_Plan.md`（v1.1 → v1.2）
  - `Docs/2_TechnicalDesign/Combat/ProjectileMovement_Library.md`（v1.1 → v1.2）
- **背景**：用户要求"先审阅 v1.1 再动工"。执行认真的自审后发现 11 个实际偏差——3 个阻碍 Phase 实施（Projectile.Initialize 签名错、ItemShape 枚举名错、命名空间不一致），3 个需澄清（Phase 1 Archetype 可用集、SO 命名规则、ID 格式），4 个文档笔误（残留 v1.0 术语 "套餐 C" / "Resources/StarChart/" / Phase 编号错位等），1 个架构规范缺失（Update vs FixedUpdate 未明确）。用户选择 N1+O2+P1（全部修 + 字母前缀 ID + InternalName 即 SO 文件名）。
- **内容**：以"代码真相对齐"为目的修正三份文档的 11 个问题，不改变架构决策。关键变更：
  1. **Library §2.1 接口**：命名空间从 `ProjectArk.Combat.Projectile` 改为 `ProjectArk.Combat`（与现有 Projectile.cs 对齐）；接口新增 `OnFixedUpdate(fixedDt)` 与 `OnUpdate(dt)` 双生命周期（v1.1 只有 `OnUpdate` 笼统放一起，不利于物理与视觉分层），明确"默认走 FixedUpdate 改 velocity，Update 只做 transform 视觉叠加"。
  2. **Library §2.2 Projectile 集成**：Initialize 签名改为对齐实际代码 `(direction, parms, modifiers = null, shooter = null)`，第四参数 shooter 可选默认 null → **3 处现有调用点（ProjectileSpawner 两处 + AutoTurretBehavior 一处）Phase 2 实施时无需改动即可编译通过**。Projectile 新增 `_hasMovement` bool 缓存避免每帧 null 比较。
  3. **主文档 §3.2 通用列**：Shape 枚举值对齐实际代码（Shape1x2H / Shape2x1V / ShapeL / ShapeLMirror），ID 列规则明确为 C001/P001/S001/T001 字母前缀，InternalName 列语义改为"SO 完整文件名（不含扩展名）"。
  4. **主文档 §9.1 Archetype 可用集**：新增分 Phase 的可用值表——Phase 1 仅 Straight，Phase 2 新增 Tracking，Phase-M2/M3 后续扩展。策划现在清楚哪个 Archetype 值能用。
  5. **Plan 新增 Phase 0**：预置阶段（0.5-1h），SO 重命名为 `{InternalName}.asset` + CSV ID 字母前缀化，避免 Phase 1 导入时新老命名并存。强制要求 Unity Editor 内 Rename（保留 GUID 不断引用），手动 Finder 改名会丢引用。
  6. **Plan Phase 1/2 任务清单**：具体化 Phase 1 的 Archetype 仅 Straight、Phase 2 的 Projectile.Initialize 签名升级细节（前三参数不变 + 第四可选参数）。
  7. **清理残留术语**：v1.0 的"套餐 C"、"Resources/StarChart/"、"Phase 5 清理"等全部替换为"B+ 方案"、"StarChartManifest.asset"、"Phase 7 清理"。修订历史表中保留作为历史说明。
- **目的**：文档是施工蓝图。v1.1 文档与代码实际存在的偏差会在实施时制造编译错误和行为偏差，预期多花 1-2h 排查。v1.2 修正在动工前 25 分钟内完成，是典型的 "pre-mortem 10 倍 ROI" 投资。
- **技术**：
  1. **Initialize 向后兼容策略**：新参数 shooter 放末尾且默认 null。这样现有 3 处调用点代码完全不变即可编译（C# 默认参数规则），Phase 2 只需补传 shooter 给 TrackingMovement 需要的两处。**避免"破坏性变更 + 改动点散布"的反模式**。
  2. **双生命周期接口**：OnFixedUpdate + OnUpdate 分离物理层与视觉层。StraightMovement 用 FixedUpdate 维持 velocity；SerpentineMovement 用 FixedUpdate 维持 velocity + Update 叠加正弦 transform 偏移。避免 Magicraft 风格"什么都塞 OnUpdate 后来悔不当初"的 shi 山。
  3. **CoreArchetype Phase-gated 可用值**：CSV 填非当前 Phase 可用的 Archetype → Debug.LogError + 降级为 Straight（不阻断导入）。策划不会因为尝试 `Tracking` 前置值而整条 CSV 导入失败。
  4. **SO 命名标准化策略**：选 P1（`{InternalName}.asset` 无前缀）让 CSV 的 InternalName 列成为唯一 SO 命名真相源。老 SO 通过 Unity Rename 一次性对齐，GUID 保留，引用不断。
- **影响**：v1.2 是文档修正，**不涉及任何代码改动**。工作树本次变化仅三份文档 + 实现日志。Phase 0 新增总工时 +0.5-1h，Phase 1/2 实施风险下降（已排除签名不一致、枚举名错、命名空间错 3 个陷阱）。
- **方法论反思**：v1.1 审查聚焦"架构决策"，v1.2 审查聚焦"代码真相对齐"。**两层审查缺一不可**——架构好但与代码不对齐，实施时仍会摔跤。后续大型设计决策应默认做"架构审查（红队 pre-mortem）+ 实施审查（代码对齐）"两轮。
