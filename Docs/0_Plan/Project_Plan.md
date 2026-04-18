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

- 替代 `Ark_MasterDesign.md`
- 替代 `CanonicalSpec` / `WorkflowSpec`
- 替代 `ImplementationLog`
- 承担专项级详细执行细节

一句话原则：

> `Project_Plan.md` 回答“项目现在做到哪、当前在做什么、接下来做什么”。

---

## 当前项目阶段

- **阶段**：场景配置与验证阶段
- **主目标**：把已完成模块真正挂进场景并跑通可玩闭环
- **当前重点**：`Room` 运行时消费链收口、关卡 authoring 护栏、场景接线验证
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
| Level | 验证中 | Phase 1-6 与 `Room` 运行时消费链收口已完成 | 场景配置、接线与试玩验证收尾 | 按真实试玩结果做定向修整 | `complete/LevelRoomRuntimeChain_Hardening.md` |
| Ship / VFX 治理 | 已完成 | `Phase A` 已完成 authority 收口，`Gate G` 已通过 | 保持治理成果稳定 | 如启动体验重构，单独立项 `ShipVFX-PhaseB` | `complete/ShipVFX_PhaseA.md` |

---

## 当前活跃专项

- 当前无仍在推进的独立专项；`LevelRoomRuntimeChain_Hardening` 已完成并归档到 [`./complete/LevelRoomRuntimeChain_Hardening.md`](./complete/LevelRoomRuntimeChain_Hardening.md)。

### 已完成专项提示

- `ShipVFX_PhaseA` 已归档到 `complete/`
- `Camera_MVP` 已验收完成并归档到 `complete/`
- `静态几何墙 MVP authoring` 已完成，并归档到 `complete/2026-04-15-static-geometry-walls-implementation-plan.md`
- `静态几何墙 Phase 2 authoring guards` 已完成，并归档到 `complete/2026-04-15-static-geometry-walls-phase2-implementation-plan.md`
- `LevelRoomRuntimeChain_Hardening` 已完成，并归档到 `complete/LevelRoomRuntimeChain_Hardening.md`
- `LevelArchitect_Workbench` 已完成，并归档到 `complete/LevelArchitect_Workbench.md`
- `ShipVFX-PhaseB`、`Camera-Phase2`、`Level-Authoring-Standardization`、`Plan-Rollout-Phase2` 已在当前阶段视为完成或已吸收，不再保留为待启动候选

---

## 下一批候选专项

- 当前这一批候选已全部完成或已吸收到现役主链，暂不保留待启动候选。

---

## 风险 / 阻塞项

- `Docs` 顶层目录已切换到新职责主树；当前风险转为防止旧路径文本与旧入口名再次回流
- 当前缺少自动化的 Markdown 链接校验，首轮 rollout 仍需人工 review
- 长期 `MigrationPlan` 与当前执行计划若不继续拆分，后续仍可能发生“路线图”和“执行计划”混用

---

## 文档导航

- 总设计入口：[`../1_GameDesign/Ark_MasterDesign.md`](../1_GameDesign/Ark_MasterDesign.md)
- 文档结构设计稿：[`./specs/2026-04-10-plan-doc-structure-design.md`](./specs/2026-04-10-plan-doc-structure-design.md)
- 文档结构实施计划：[`./ongoing/2026-04-10-plan-doc-structure-rollout.md`](./ongoing/2026-04-10-plan-doc-structure-rollout.md)
- 关卡规范：[`../2_TechnicalDesign/Level/Level_CanonicalSpec.md`](../2_TechnicalDesign/Level/Level_CanonicalSpec.md)
- 关卡工作流：[`../3_WorkflowsAndRules/LevelArchitect/Level_WorkflowSpec.md`](../3_WorkflowsAndRules/LevelArchitect/Level_WorkflowSpec.md)
- Ship / VFX 规范：[`../2_TechnicalDesign/Ship/ShipVFX_CanonicalSpec.md`](../2_TechnicalDesign/Ship/ShipVFX_CanonicalSpec.md)
- Ship / VFX 迁移路线图：[`./ongoing/ShipVFX_MigrationPlan.md`](./ongoing/ShipVFX_MigrationPlan.md)
- 游戏数据入口：[`../4_GameData/README.md`](../4_GameData/README.md)
- 活跃专项入口：[`./ongoing/README.md`](./ongoing/README.md)
- 已完成专项：[`./complete/Camera_MVP.md`](./complete/Camera_MVP.md)
- 已完成专项：[`./complete/2026-04-15-static-geometry-walls-implementation-plan.md`](./complete/2026-04-15-static-geometry-walls-implementation-plan.md)
- 已完成专项：[`./complete/2026-04-15-static-geometry-walls-phase2-implementation-plan.md`](./complete/2026-04-15-static-geometry-walls-phase2-implementation-plan.md)
- 已完成专项：[`./complete/LevelRoomRuntimeChain_Hardening.md`](./complete/LevelRoomRuntimeChain_Hardening.md)
- 已完成专项：[`./complete/LevelArchitect_Workbench.md`](./complete/LevelArchitect_Workbench.md)

- 已完成专项：[`./complete/ShipVFX_PhaseA.md`](./complete/ShipVFX_PhaseA.md)
- 实现日志：[`../5_ImplementationLog/README.md`](../5_ImplementationLog/README.md)
- 历史关卡方案：[`../8_Obsolete/Plan/LevelModule_Plan.md`](../8_Obsolete/Plan/LevelModule_Plan.md)
