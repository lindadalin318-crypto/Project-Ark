# Implementation Log — 2026-04

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
