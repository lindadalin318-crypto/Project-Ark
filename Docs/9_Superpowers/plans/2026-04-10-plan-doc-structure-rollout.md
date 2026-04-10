# Plan Documentation Structure Rollout Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在不重排整个 `Docs` 的前提下，落地 `Docs/0_Plan/` 体系，建立 `ProjectPlan.md` 总入口，并迁入第一份现役专项计划 `ShipVFX-PhaseA`。

**Architecture:** 采用“先入口、后迁移”的增量落地方式：先建立 `Docs/0_Plan/` 目录与 `ProjectPlan.md` 驾驶舱，再把当前最典型的活跃计划从 `Docs/2_Design/Ship/ShipVFX_PhaseA_AuthorityPlan.md` 迁入 `Docs/0_Plan/ongoing/ShipVFX-PhaseA.md`，最后补 `ImplementationLog` 并验证没有断链。整个 rollout 不触碰 `CanonicalSpec`、`WorkflowSpec`、`ImplementationLog` 的职责边界，也不处理 `Docs` 顶层编号/非编号目录并存问题。

**Tech Stack:** Markdown 文档、PowerShell、Git、ripgrep（内容检索）

---

## File Structure Map

### Create

- `Docs/0_Plan/ProjectPlan.md` — 项目总入口，维护当前阶段、模块状态、活跃专项、候选专项、风险与导航
- `Docs/0_Plan/complete/README.md` — 说明 `complete/` 的语义，并保证空目录可被 Git 跟踪
- `Docs/0_Plan/ongoing/ShipVFX-PhaseA.md` — 第一份迁入新体系的活跃专项计划

### Delete

- `Docs/2_Design/Ship/ShipVFX_PhaseA_AuthorityPlan.md` — 旧位置的执行计划文档；迁入新路径并完成结构改造后删除

### Modify

- `Docs/5_ImplementationLog/ImplementationLog.md` — 记录本轮 `Plan/` 体系首轮落地的文档变更

### Keep As-Is

- `Docs/2_Design/Ship/ShipVFX_CanonicalSpec.md`
- `Docs/2_Design/Ship/ShipVFX_MigrationPlan.md`
- `Docs/2_Design/Level/Level_CanonicalSpec.md`
- `Docs/2_Design/Level/Level_WorkflowSpec.md`
- `Docs/8_Obsolete/LevelModulePlan.md`

这些文件继续保留在当前目录，不参与本轮物理迁移。

---

### Task 1: 建立 `Docs/0_Plan/` 入口与 `ProjectPlan.md`

**Files:**

- Create: `Docs/0_Plan/ProjectPlan.md`
- Create: `Docs/0_Plan/complete/README.md`

- [ ] **Step 1: 确认当前不存在 `Docs/0_Plan/` 并创建目录**

Run:

```powershell
Test-Path "f:\UnityProjects\Project-Ark\Docs\0_Plan"
New-Item -ItemType Directory -Force -Path "f:\UnityProjects\Project-Ark\Docs\0_Plan\ongoing" | Out-Null
New-Item -ItemType Directory -Force -Path "f:\UnityProjects\Project-Ark\Docs\0_Plan\complete" | Out-Null
```

Expected:

- 第一条命令返回 `False`
- 两个目录创建成功，无报错

- [ ] **Step 2: 创建 `Docs/0_Plan/complete/README.md`，固定 `complete/` 的语义**

Write this exact content to `Docs/0_Plan/complete/README.md`:

```markdown
# Completed Plans

本目录用于归档**已完成且仍有效**的专项计划。

- `complete/` 表示该专项的主目标已完成，结论仍可参考
- `Obsolete/` 表示文档口径已失效，不应继续作为现役依据
- 专项从 `ongoing/` 移入本目录前，应先同步更新 `ProjectPlan.md`
```

- [ ] **Step 3: 创建 `Docs/0_Plan/ProjectPlan.md` 初版，直接写入首轮可用内容**

Write this exact content to `Docs/0_Plan/ProjectPlan.md`:

```markdown
# Project Plan — Project Ark

## 文档定位

本文件是 `Project Ark` 当前的**项目状态入口**。

它负责维护：

- 当前项目阶段
- 各模块的实现状态
- 当前活跃专项
- 下一批候选专项
- 风险 / 阻塞项
- 文档导航

它**不**负责：

- 替代 `GDD`
- 替代 `CanonicalSpec` / `WorkflowSpec`
- 替代 `ImplementationLog`
- 承担专项级详细执行细节

一句话原则：

> `ProjectPlan.md` 回答“项目现在做到哪、当前在做什么、接下来做什么”。

---

## 当前项目阶段

- **阶段**：场景配置与验证阶段
- **主目标**：把已完成模块真正挂进场景并跑通可玩闭环
- **当前重点**：关卡 authoring 护栏、场景接线验证、Ship / VFX 治理收口、镜头体验方向收敛
- **当前非重点**：大规模新系统扩张、顶层目录历史债全面清理

---

## 模块状态总表

| 模块 | 状态 | 已完成摘要 | 当前重点 | 下一步 | 关联专项 |
| --- | --- | --- | --- | --- | --- |
| 3C | 已完成 | `ShipMotor`、`ShipAiming`、`InputHandler` 主链完成 | 与场景验证联调 | 按试玩反馈做小修 | — |
| Combat / StarChart | 已完成 | 战斗循环、星图编织、热量与四家族投射物已完成 | 场景接入与资产验证 | 按验证结果做定向修整 | — |
| Enemy AI | 已完成 | HFSM、4 原型、Phase 3 扩展已完成 | 遭遇配置与可读性联调 | 跟关卡试玩闭环验证 | — |
| UI | 已完成 | 星图面板、HUD、拖拽装备、编织态过渡已完成 | 场景内联调与细节修整 | 依据试玩反馈微调 | — |
| Infrastructure | 已完成 | `UniTask`、`PrimeTween`、`ServiceLocator`、`SaveManager`、`AudioManager`、`CombatEvents` 已完成 | 作为验证阶段底座 | 仅修复阻塞性问题 | — |
| Level | 验证中 | Phase 1-6 已全部完成 | 场景配置、authoring 护栏、可玩闭环验证 | 完成当前场景接线与 Play Mode 验收 | — |
| Ship / VFX 治理 | 进行中 | Phase A 的 A0-A1 已完成，边界冻结与工具审计已落地 | 收口 authority、validator 与 debug-only 边界 | 推进 `ShipVFX-PhaseA` | `ongoing/ShipVFX-PhaseA.md` |

---

## 当前活跃专项

### `ShipVFX-PhaseA`

- **所属模块**：`Ship / VFX`
- **目标**：把 `Ship / VFX` 从“多入口可写、靠经验排查”的状态，收口到“权威清晰、工具分层、能自动抓错”的状态
- **当前状态**：进行中（已完成 A0-A1，下一步推进 A2）
- **对应文档**：[`ongoing/ShipVFX-PhaseA.md`](./ongoing/ShipVFX-PhaseA.md)

---

## 下一批候选专项

- **`Camera-MVP`**：在现有镜头底座上补前向 lead、基础兴趣点与更明确的 framing 规则
- **`Level-Validation-Hardening`**：继续补齐 authoring 护栏与验证规则，让场景配置问题更早暴露
- **`Plan-Rollout-Phase2`**：当首轮入口跑顺后，再评估是否迁入第二批现役专项或清理顶层目录历史债

---

## 风险 / 阻塞项

- `Docs` 顶层仍存在编号目录与非编号目录并存问题，本轮只建立新入口，不主动解决该历史债
- 当前缺少自动化的 Markdown 链接校验，首轮 rollout 仍需人工 review
- 长期 `MigrationPlan` 与当前执行计划若不继续拆分，后续仍可能发生“路线图”和“执行计划”混用

---

## 文档导航

- 设计稿：[`../specs/2026-04-10-plan-doc-structure-design.md`](../specs/2026-04-10-plan-doc-structure-design.md)
- 关卡规范：[`../2_Design/Level/Level_CanonicalSpec.md`](../2_Design/Level/Level_CanonicalSpec.md)
- 关卡工作流：[`../2_Design/Level/Level_WorkflowSpec.md`](../2_Design/Level/Level_WorkflowSpec.md)
- Ship / VFX 规范：[`../2_Design/Ship/ShipVFX_CanonicalSpec.md`](../2_Design/Ship/ShipVFX_CanonicalSpec.md)
- Ship / VFX 迁移路线图：[`../2_Design/Ship/ShipVFX_MigrationPlan.md`](../2_Design/Ship/ShipVFX_MigrationPlan.md)
- 活跃专项：[`./ongoing/ShipVFX-PhaseA.md`](./ongoing/ShipVFX-PhaseA.md)
- 实现日志：[`../5_ImplementationLog/ImplementationLog.md`](../5_ImplementationLog/ImplementationLog.md)
- 历史关卡方案：[`../8_Obsolete/LevelModulePlan.md`](../8_Obsolete/LevelModulePlan.md)
```

- [ ] **Step 4: 用检索命令确认 `ProjectPlan.md` 具备预期栏目**

Run:

```powershell
rg -n "^## 文档定位|^## 当前项目阶段|^## 模块状态总表|^## 当前活跃专项|^## 下一批候选专项|^## 风险 / 阻塞项|^## 文档导航" "f:\UnityProjects\Project-Ark\Docs\0_Plan\ProjectPlan.md"
```

Expected:

- 输出 7 条匹配结果
- 栏目顺序与计划一致

- [ ] **Step 5: 检查当前改动范围（不提交）**

Run:

```powershell
git -C "f:\UnityProjects\Project-Ark" --no-pager diff --stat -- "Docs/0_Plan"
```

Expected:

- 只看到 `Docs/0_Plan/ProjectPlan.md` 和 `Docs/0_Plan/complete/README.md` 的新增统计

---

### Task 2: 迁入第一份活跃专项 `ShipVFX-PhaseA`

**Files:**

- Create: `Docs/0_Plan/ongoing/ShipVFX-PhaseA.md`
- Delete: `Docs/2_Design/Ship/ShipVFX_PhaseA_AuthorityPlan.md`

- [ ] **Step 1: 先确认旧计划文档没有现役强引用，再执行迁移**

Run:

```powershell
rg -n "ShipVFX_PhaseA_AuthorityPlan\.md|ShipVFX-PhaseA" "f:\UnityProjects\Project-Ark"
```

Expected:

- 可以看到设计稿和后续新文件中的提及
- 不应出现依赖旧路径的现役强引用；若有，先把引用改到新路径再继续删除旧文件

- [ ] **Step 2: 将旧文件移动到新位置，作为迁移基础**

Run:

```powershell
Move-Item "f:\UnityProjects\Project-Ark\Docs\2_Design\Ship\ShipVFX_PhaseA_AuthorityPlan.md" "f:\UnityProjects\Project-Ark\Docs\0_Plan\ongoing\ShipVFX-PhaseA.md"
```

Expected:

- 新路径文件存在
- 旧路径文件消失

- [ ] **Step 3: 把迁移后的文档顶部结构改造成 `ongoing` 模板**

Replace the top section of `Docs/0_Plan/ongoing/ShipVFX-PhaseA.md` from the title down to the line just before the old `## 5. 执行顺序总览` with this exact content:

```markdown
# ShipVFX-PhaseA

## 文档定位

本文件是 `Ship / VFX` 当前活跃专项 `Phase A` 的执行计划。

它负责维护：

- 本轮治理目标
- 范围边界
- 完成标准
- 当前状态
- 工作拆分
- 风险与注意事项
- 关联文档

它不替代以下真相源：

- `Implement_rules.md`
- `ShipVFX_CanonicalSpec.md`
- `ShipVFX_AssetRegistry.md`
- `ShipVFX_MigrationPlan.md`

一句话原则：

> 本文件回答“`Ship / VFX` Phase A 现在做到哪、下一步做什么、做到什么算完成”。

---

## 当前目标

> **把 `Ship / VFX` 从“多入口可写、靠经验排查”的状态，收口到“权威清晰、工具分层、能自动抓错”的状态。**

本轮优先解决：

- prefab / scene / runtime / debug 多入口并行写入
- builder 越权写回
- runtime fallback 维持双轨主链
- debug 工具默认参与正式链路
- scene override 漂移与 silent no-op
- legacy / migration residue 长期滞留

---

## 范围

### In Scope

- `Assets/Scripts/Ship/Editor/`
- `Assets/Scripts/Ship/VFX/`
- `Assets/Scripts/Ship/Movement/ShipBoost.cs`
- `Assets/_Prefabs/Ship/Ship.prefab`
- `Assets/_Prefabs/VFX/BoostTrailRoot.prefab`
- `Assets/Scenes/SampleScene.unity` 中的 scene-only Bloom 链路
- `ShipVFX` 相关文档与工具职责口径

### Out of Scope

- 大规模视觉重做
- 大范围物理 rename / 资源迁移
- 脱离当前读感目标的整包重做
- 批量清理所有 dormant 资源
- 直接推进 backlog 中的视觉验收条目

---

## 完成标准

1. **唯一权威**：每类引用只有一个权威来源
2. **无双轨主链**：不再保留不必要的 runtime fallback
3. **debug 不接管主链**：debug 工具默认只观察，不持续覆盖正式运行态
4. **override 白名单化**：明确哪些 scene override 可保留，哪些必须清理
5. **无静默失败**：关键依赖缺失时会报错，或能被 validator / audit 抓到
6. **Clean Exit**：不以“legacy 继续挂着备用”的形态收尾

---

## 当前状态

- **状态**：进行中
- **已完成**：A0 冻结治理边界；A1 工具权限审计
- **当前重点**：A2 菜单与职责收口
- **下一步**：A3 Validator / Audit MVP → A4 删除双轨主链与冗余旧路径 → A5 Scene Override 收口
- **当前风险**：authority 工具内部仍夹带 fallback / migration residue，若不继续清理，会再次模糊边界

---

## 工作拆分总览
```

- [ ] **Step 4: 调整迁移后的章节标题，使其符合新模板但保留原有详细内容**

Perform these exact title replacements inside `Docs/0_Plan/ongoing/ShipVFX-PhaseA.md`:

```text
把 `## 5. 执行顺序总览` 改为 `## 6. 工作拆分总览`
把 `## 6. 分步执行细则` 改为 `## 7. 分步执行细则`
在文件末尾追加 `## 8. 风险与注意事项` 和 `## 9. 关联文档`
```

Append this exact block to the end of `Docs/0_Plan/ongoing/ShipVFX-PhaseA.md`:

```markdown
---

## 8. 风险与注意事项

- 本专项是治理计划，不是体验重构计划；不要顺手把视觉风格重做混进来
- 若 authority 工具中的 fallback / migration residue 不被继续清理，文档迁移本身不会自动带来边界收口
- 迁入 `Plan/ongoing/` 后，规范真相源仍在 `Implement_rules.md`、`ShipVFX_CanonicalSpec.md` 与 `ShipVFX_AssetRegistry.md`

## 9. 关联文档

- `Implement_rules.md`
- `Docs/2_Design/Ship/ShipVFX_CanonicalSpec.md`
- `Docs/2_Design/Ship/ShipVFX_MigrationPlan.md`
- `Docs/0_Plan/ProjectPlan.md`
- `Docs/5_ImplementationLog/ImplementationLog.md`
```

- [ ] **Step 5: 验证迁移后文件的标题和旧路径状态**

Run:

```powershell
Test-Path "f:\UnityProjects\Project-Ark\Docs\2_Design\Ship\ShipVFX_PhaseA_AuthorityPlan.md"
Test-Path "f:\UnityProjects\Project-Ark\Docs\0_Plan\ongoing\ShipVFX-PhaseA.md"
rg -n "^## 文档定位|^## 当前目标|^## 范围|^## 完成标准|^## 当前状态|^## 6\. 工作拆分总览|^## 7\. 分步执行细则|^## 8\. 风险与注意事项|^## 9\. 关联文档" "f:\UnityProjects\Project-Ark\Docs\0_Plan\ongoing\ShipVFX-PhaseA.md"
```

Expected:

- 第一个 `Test-Path` 返回 `False`
- 第二个 `Test-Path` 返回 `True`
- `rg` 输出 9 条匹配，说明模板结构已成型

---

### Task 3: 记录本轮落地并做最终验证

**Files:**

- Modify: `Docs/5_ImplementationLog/ImplementationLog.md`
- Verify: `Docs/0_Plan/ProjectPlan.md`
- Verify: `Docs/0_Plan/ongoing/ShipVFX-PhaseA.md`
- Verify: `Docs/0_Plan/complete/README.md`

- [ ] **Step 1: 把本轮 `Plan/` 首轮落地记录进实现日志**

Insert this exact log entry at the top of `Docs/5_ImplementationLog/ImplementationLog.md`, immediately after the file title separator block:

```markdown
## Plan 体系首轮落地（ProjectPlan + ShipVFX ongoing）— 2026-04-10 HH:MM

### 新建文件

- `Docs/0_Plan/ProjectPlan.md`
- `Docs/0_Plan/complete/README.md`
- `Docs/0_Plan/ongoing/ShipVFX-PhaseA.md`

### 删除文件

- `Docs/2_Design/Ship/ShipVFX_PhaseA_AuthorityPlan.md`

### 修改文件

- `Docs/5_ImplementationLog/ImplementationLog.md`

### 内容

- 新建 `Docs/0_Plan/` 体系首轮入口：`ProjectPlan.md`、`ongoing/`、`complete/`。
- 在 `ProjectPlan.md` 中落入当前项目阶段、模块状态总表、活跃专项、候选专项、风险与文档导航，使其成为新的项目状态入口。
- 将 `ShipVFX_PhaseA_AuthorityPlan.md` 迁入 `Docs/0_Plan/ongoing/ShipVFX-PhaseA.md`，并按新的 ongoing 模板重组顶部结构与关联文档。
- 明确 `complete/` 与 `Obsolete/` 的职责差异，避免后续把“已完成”与“已失效”再次混用。

### 目的

- 把 `Plan` 从 `Spec` 和 `ImplementationLog` 中拆出，建立稳定的项目驾驶舱与当前执行入口。
- 以最小迁移范围验证新工作流是否顺手，再决定是否推进第二批专项迁移。

### 技术

- 文档结构重组：采用“总入口 + ongoing + complete”的增量落地方式。
- 文档迁移：通过物理迁移一份现役专项计划，验证 `ongoing → complete` 的后续归档路径。
```

Replace `HH:MM` with the actual local time at the moment you write the log.

- [ ] **Step 2: 做一次 workspace 级断链和新入口验证**

Run:

```powershell
rg -n "Docs/0_Plan/|ProjectPlan\.md|ShipVFX-PhaseA" "f:\UnityProjects\Project-Ark\Docs"
git -C "f:\UnityProjects\Project-Ark" --no-pager diff --stat -- "Docs/0_Plan" "Docs/5_ImplementationLog/ImplementationLog.md" "Docs/2_Design/Ship/ShipVFX_PhaseA_AuthorityPlan.md"
```

Expected:

- `rg` 能看到 `ProjectPlan.md`、`ShipVFX-PhaseA.md`、设计稿和实现日志中的新路径
- `git diff --stat` 只显示本轮预期的新增 / 删除 / 修改文件

- [ ] **Step 3: 做最终人工 review，确认 rollout 没有越界**

Open and manually check these files:

- `Docs/0_Plan/ProjectPlan.md`
- `Docs/0_Plan/ongoing/ShipVFX-PhaseA.md`
- `Docs/0_Plan/complete/README.md`
- `Docs/5_ImplementationLog/ImplementationLog.md`

Review checklist:

- `ProjectPlan.md` 是否能在 5 分钟内读完整局
- `ShipVFX-PhaseA.md` 是否已经从“Spec 风格”转成“ongoing plan 风格”
- `complete/README.md` 是否明确区分了 `complete` 与 `Obsolete`
- 本轮是否没有误迁 `CanonicalSpec`、`WorkflowSpec`、`MigrationPlan`
- 本轮是否没有顺手开始处理 `Docs` 顶层历史目录债

- [ ] **Step 4: 停在未提交状态，等待用户决定下一步**

Run:

```powershell
git -C "f:\UnityProjects\Project-Ark" --no-pager status --short -- "Docs/0_Plan" "Docs/5_ImplementationLog/ImplementationLog.md" "Docs/2_Design/Ship/ShipVFX_PhaseA_AuthorityPlan.md"
```

Expected:

- 工作区保留本轮文档改动
- **不要执行 `git commit`，除非用户明确要求提交**

---

## Self-Review

### Spec coverage

本计划完整覆盖了设计稿中约定的首轮落地范围：

- 创建 `Docs/0_Plan/` 入口
- 创建 `ProjectPlan.md`
- 迁入第一份活跃专项 `ShipVFX-PhaseA`
- 不迁动 `CanonicalSpec` / `WorkflowSpec` / `ImplementationLog`
- 不处理 `Docs` 顶层重复目录历史债
- 补实现日志并做新入口验证

没有超出设计稿范围去处理第二批专项或顶层目录清理。

### Placeholder scan

已检查计划正文，不包含未落地的占位表达。唯一需要执行时替换的动态值是日志时间 `HH:MM`，该处已显式说明必须替换为实际本地时间。

### Type consistency

本计划中使用的关键文件路径、目录名、专项名保持一致：

- `Docs/0_Plan/ProjectPlan.md`
- `Docs/0_Plan/ongoing/ShipVFX-PhaseA.md`
- `Docs/0_Plan/complete/README.md`
- `Docs/2_Design/Ship/ShipVFX_PhaseA_AuthorityPlan.md`

不存在同一对象在不同任务中使用不同命名的问题。
