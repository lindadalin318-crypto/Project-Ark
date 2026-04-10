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
- **当前重点**：关卡 authoring 护栏、场景接线验证、镜头体验方向收敛、梳理下一批专项入口
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
| Ship / VFX 治理 | 已完成 | `Phase A` 已完成 authority 收口，`Gate G` 已通过 | 保持治理成果稳定 | 如启动体验重构，单独立项 `ShipVFX-PhaseB` | `complete/ShipVFX-PhaseA.md` |

---

## 当前活跃专项

- **当前暂无已迁入 `ongoing/` 的专项**
- **说明**：首轮迁移核对后确认 `ShipVFX-PhaseA` 已达到完成态，因此已归档到 `complete/`
- **下一份建议立项**：`ShipVFX-PhaseB`、`Camera-MVP`、`Level-Validation-Hardening`

---

## 下一批候选专项

- **`ShipVFX-PhaseB`**：基于 `Phase A` 已完成的治理基线，进入第一批体验重构切片
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

- 设计稿：[`../superpowers/specs/2026-04-10-plan-doc-structure-design.md`](../superpowers/specs/2026-04-10-plan-doc-structure-design.md)
- 实施计划：[`../superpowers/plans/2026-04-10-plan-doc-structure-rollout.md`](../superpowers/plans/2026-04-10-plan-doc-structure-rollout.md)
- 关卡规范：[`../2_Design/Level/Level_CanonicalSpec.md`](../2_Design/Level/Level_CanonicalSpec.md)
- 关卡工作流：[`../2_Design/Level/Level_WorkflowSpec.md`](../2_Design/Level/Level_WorkflowSpec.md)
- Ship / VFX 规范：[`../2_Design/Ship/ShipVFX_CanonicalSpec.md`](../2_Design/Ship/ShipVFX_CanonicalSpec.md)
- Ship / VFX 迁移路线图：[`../2_Design/Ship/ShipVFX_MigrationPlan.md`](../2_Design/Ship/ShipVFX_MigrationPlan.md)
- 活跃专项入口：[`./ongoing/README.md`](./ongoing/README.md)
- 已完成专项：[`./complete/ShipVFX-PhaseA.md`](./complete/ShipVFX-PhaseA.md)
- 实现日志：[`../5_ImplementationLog/ImplementationLog.md`](../5_ImplementationLog/ImplementationLog.md)
- 历史关卡方案：[`../8_Obsolete/LevelModulePlan.md`](../8_Obsolete/LevelModulePlan.md)
