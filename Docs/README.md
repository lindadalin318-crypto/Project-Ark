# Docs Index — Project Ark

## 文档入口

`Docs/` 是《Project Ark》的统一文档树入口。

默认阅读顺序：

1. [`0_Plan/Project_Plan.md`](./0_Plan/Project_Plan.md) — 项目现在做到哪、当前在做什么、接下来做什么
2. [`1_GameDesign/README.md`](./1_GameDesign/README.md) — 策划正文设计区
3. [`2_TechnicalDesign/README.md`](./2_TechnicalDesign/README.md) — 技术真相源
4. [`3_WorkflowsAndRules/README.md`](./3_WorkflowsAndRules/README.md) — 工作流、authority、validator、治理规则
5. [`4_GameData/README.md`](./4_GameData/README.md) — 结构化游戏数据
6. [`6_Diagnostics/README.md`](./6_Diagnostics/README.md) — 当前项目现状诊断

## 顶层目录职责

- [`0_Plan/`](./0_Plan/README.md): 项目驾驶舱、设计 spec、活跃专项、已完成专项
- [`1_GameDesign/`](./1_GameDesign/README.md): 全部策划可直接理解和维护的设计正文
- [`2_TechnicalDesign/`](./2_TechnicalDesign/README.md): 运行时架构、模块边界、技术真相源
- [`3_WorkflowsAndRules/`](./3_WorkflowsAndRules/README.md): authoring workflow、authority、validator、文档治理规则
- [`4_GameData/`](./4_GameData/README.md): `.csv`、`.json` 及其他结构化游戏数据
- [`5_ImplementationLog/`](./5_ImplementationLog/README.md): 实现日志历史总账与月度日志
- [`6_Diagnostics/`](./6_Diagnostics/README.md): 当前支持矩阵、状态盘点、现状报告
- [`7_Reference/`](./7_Reference/README.md): 外部参考、游戏拆解、技术笔记
- [`8_Obsolete/`](./8_Obsolete/README.md): 已失效或历史归档文档

## 例外入口

- [`../Implement_rules.md`](../Implement_rules.md): 项目级实现规则例外入口。文件保留在仓库根目录，但属于 `3_WorkflowsAndRules` 体系的一部分。

## 维护约束

- 顶层索引以本文件为准
- 一级目录必须维护自己的 `README.md`
- 现役文档默认使用英文文件名、下划线分词、相对路径链接
- 结构化数据不放进 `1_GameDesign/`
- 带日期的旧诊断快照应迁入 `8_Obsolete/Diagnostics/`
