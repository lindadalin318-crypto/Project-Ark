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

