# Project Ark 文档结构改造设计稿（Plan 体系）

## 1. 背景

当前 `Docs` 目录中已经同时承载了多种不同职责的文档：

- `GDD`：设计意图、玩法目标、模块蓝图
- `Design / Spec`：authoring 规则、authority 边界、工作流手册、规范真相源
- `ImplementationLog`：历史变更记录
- `Reference`：调研、外部参考、考据资料
- 各类 `*Plan`：阶段计划、迁移计划、治理计划、模块计划

随着项目进入“多模块已完成、部分模块进入验证与治理、少数专项持续推进”的阶段，现有文档结构出现了一个明显问题：

> **项目并不缺文档，而是缺一个能够快速回答“现在做到哪、当前在做什么、接下来做什么”的统一入口。**

当前 `*Plan` 文档散落在不同目录中，导致以下摩擦：

- 想知道项目总状态时，需要同时翻 `GDD`、`Design`、`ImplementationLog`
- 想知道某专项是否仍在推进时，没有稳定入口
- 已完成专项和失效文档、长期规范之间的边界不够清晰
- 计划文档容易与规范文档、历史记录互相混用

因此，本设计稿提出建立新的 `Docs/0_Plan/` 体系，作为 **项目状态入口 + 当前执行入口 + 专项归档入口**。

---

## 2. 设计目标

本次改造要解决的是“入口与职责”问题，而不是对整个 `Docs` 做一次全面重排。

### 2.1 核心目标

- 建立统一的项目状态入口
- 让团队可以快速看到当前活跃专项
- 把“计划”从“规范”和“日志”中拆出来
- 为专项从启动到完成提供稳定的落位方式

### 2.2 成功标准

若改造成功，应满足以下条件：

1. 打开一个文件即可理解项目当前阶段与模块状态
2. 当前活跃专项有唯一入口，不再散落在 `Design` / `Reference` / `Log`
3. 已完成专项能够归档，但不会继续污染主入口
4. `Plan`、`Spec`、`ImplementationLog` 的职责边界清晰
5. 本轮可以增量落地，不要求一次性重构整个 `Docs`

### 2.3 非目标

本轮不追求以下事项：

- 不对全部历史文档做统一命名清洗
- 不在本轮解决 `Docs` 顶层编号目录与非编号目录并存问题
- 不让 `Plan` 替代 `GDD`、`CanonicalSpec`、`WorkflowSpec`、`ImplementationLog`
- 不把所有带 `Plan` 字样的历史文档一次性迁入新体系

---

## 3. 核心设计结论

采用如下结构：

```text
Docs/
└── Plan/
    ├── ProjectPlan.md
    ├── ongoing/
    │   ├── ShipVFX-PhaseA.md
    │   ├── Camera-MVP.md
    │   └── Level-Validation-Hardening.md
    └── complete/
        ├── LevelModule.md
        └── StarChart-Batch6.md
```

### 3.1 结构职责

- `ProjectPlan.md`
  - 项目总入口
  - 负责维护当前项目阶段、模块状态、活跃专项、候选专项、风险与导航

- `ongoing/`
  - 只放当前正在推进、且值得单独跟踪的专项计划
  - 面向执行层，是当前开工入口的一部分

- `complete/`
  - 只放已经完成的专项计划
  - 面向归档与回看，不等于 `Obsolete`

### 3.2 关键定义

- `complete/`：专项已完成，结论仍有效
- `Obsolete/`：文档口径已过时，不应再作为现役参考

---

## 4. 职责边界

新的 `Plan/` 体系必须从一开始就和现有文档类型明确分工。

### 4.1 Plan

`Plan` 只回答：

- 现在做到哪
- 当前在做什么
- 接下来做什么

### 4.2 Design / Spec

`Design / Spec` 只回答：

- 系统应该怎么做
- authoring / authority / workflow 的规则是什么
- 当前现役真相源在哪里

### 4.3 ImplementationLog

`ImplementationLog` 只回答：

- 已经改了什么
- 为什么改
- 使用了什么技术

### 4.4 一句话原则

> **Plan 回答“现在做到哪”；Spec 回答“应该怎么做”；Log 回答“已经做过什么”。**

---

## 5. `ProjectPlan.md` 固定结构

`ProjectPlan.md` 应是项目驾驶舱，而不是第二份 `GDD`。推荐固定包含以下栏目：

### 5.1 文档定位

说明：

- 本文档负责什么
- 不负责什么
- `ongoing/`、`complete/`、`Design`、`ImplementationLog` 的分工

### 5.2 当前项目阶段

简要说明：

- 当前所处阶段
- 当前主目标
- 当前重点
- 当前阶段不做什么

### 5.3 模块状态总表

建议字段：

- 模块
- 当前状态
- 已完成摘要
- 当前重点
- 下一步
- 关联专项

建议统一使用以下状态词：

- 已完成
- 进行中
- 验证中
- 待启动
- 维护中

### 5.4 当前活跃专项

列出当前 `ongoing/` 中活跃的专项，并说明：

- 专项名
- 所属模块
- 目标
- 当前状态
- 对应文档

### 5.5 下一批候选专项

只保留少量高价值候选项，避免演化为大 backlog。

### 5.6 风险 / 阻塞项

只记录影响当前阶段推进的项目级风险。

### 5.7 文档导航

统一链接到：

- `GDD`
- `Design / Spec`
- `Plan/ongoing`
- `Plan/complete`
- `ImplementationLog`
- `Reference`

---

## 6. 单个专项 Plan 模板

### 6.1 适用原则

`ongoing/` 与 `complete/` 使用同一套主模板，不在专项完成时重写第二份文档。

### 6.2 推荐模板结构

每个专项文档固定包含：

1. 文档定位
2. 当前目标
3. 范围
   - In Scope
   - Out of Scope
4. 完成标准
5. 当前状态
6. 工作拆分
7. 风险与注意事项
8. 关联文档

### 6.3 从 `ongoing` 到 `complete` 的迁移方式

专项完成时，只做以下动作：

- 更新当前状态为完成态
- 补充“完成结论”
- 补充“遗留事项 / 后续可选项”
- 将文件移动到 `complete/`

### 6.4 原则

> **专项 plan 是一份会成长的文档，而不是做完后另起一份总结。**

---

## 7. 创建与归档规则

### 7.1 何时只更新 `ProjectPlan.md`

满足以下条件时，只更新总 plan，不单独创建专项文档：

- 只是模块状态变化
- 没有独立阶段目标
- 不需要单独跟踪完成标准、风险与依赖

### 7.2 何时必须创建 `ongoing` 专项文档

满足以下任一条件时，应在 `ongoing/` 中单独建文件：

- 预计持续多个会话
- 跨多个脚本 / 文档 / 场景 / 资产
- 有明确范围与完成标准
- 需要独立追踪阶段、阻塞和收尾

### 7.3 何时移入 `complete/`

满足以下条件时，专项可从 `ongoing/` 移入 `complete/`：

- 主目标已完成
- 完成标准已满足
- `ProjectPlan.md` 已同步状态
- 剩余问题只是不影响完成态的小尾项

---

## 8. 迁移策略

本次改造应采用 **增量迁移**，而不是一次性大搬家。

### 8.1 Phase 1：建立新入口

先创建：

- `Docs/0_Plan/`
- `Docs/0_Plan/ProjectPlan.md`
- `Docs/0_Plan/ongoing/`
- `Docs/0_Plan/complete/`

第一阶段先让 `ProjectPlan.md` 成为新的默认阅读入口。

### 8.2 Phase 2：迁入少量活跃专项

第一批只迁 1-3 份真正活跃的专项文档。

推荐第一批候选：

- `ShipVFX_PhaseA_AuthorityPlan.md` → `Docs/0_Plan/ongoing/ShipVFX-PhaseA.md`
- 其余专项视当前是否正式立项决定，避免“凡是带 Plan 的都迁”

### 8.3 Phase 3：完成专项归档

当专项完成后：

- 将其从 `ongoing/` 移动到 `complete/`
- 同步更新 `ProjectPlan.md`
- 保留与 `Spec`、`ImplementationLog` 的链接

### 8.4 Phase 4：再处理目录历史债

以下问题不建议与本轮绑定：

- `1_GDD` / `GDD` 并存
- `5_ImplementationLog` / `ImplementationLog` 并存
- `7_Reference` / `Reference` 并存

这些属于第二阶段的顶层目录治理问题，应在 `Plan/` 工作流跑顺后再单独处理。

---

## 9. 文档迁移判定规则

### 9.1 应迁入 `Plan/` 的文档

- 当前仍在执行中的阶段计划
- 明确回答“先做哪一步、怎么推进、做到什么算完成”的专项计划

### 9.2 不应迁入 `Plan/` 的文档

- `CanonicalSpec`
- `WorkflowSpec`
- `ImplementationLog`
- 纯 `Reference`
- 已失效的 `Obsolete`

### 9.3 特殊情况：长期 Migration 文档

像 `ShipVFX_MigrationPlan.md` 这类“长期路线图 + backlog”的复合文档，不建议整份直接搬进新的 `Plan/ongoing/`。推荐处理方式：

- 把当前正在执行的部分抽成 `ongoing` 专项文件
- 把长期方向摘要纳入 `ProjectPlan.md`
- 原文暂时保留，后续再评估是继续留作参考、拆分，还是归档

---

## 10. 维护规则

### 10.1 必须更新 `ProjectPlan.md` 的时机

- 项目阶段切换
- 模块状态变化
- 新专项启动
- 专项完成归档

### 10.2 必须更新专项文档的时机

- 范围变化
- 当前状态变化
- 完成标准被调整
- 出现新的关键风险或 blocker

### 10.3 维护原则

`ProjectPlan.md` 必须保持“5 分钟读完全局、1 分钟找到当前重点”的可读性。

因此，不应把以下内容塞入总 plan：

- 大段背景叙述
- 详细规范正文
- 长篇历史记录
- 专项级超细 TODO
- 大型 backlog 清单

---

## 11. 推荐首轮落地方案

为了以最低风险启动这套体系，建议首轮只做以下事项：

1. 创建 `Docs/0_Plan/` 结构
2. 新建 `ProjectPlan.md` 最小可用版本
3. 先迁入一个最典型的活跃专项：`ShipVFX-PhaseA`
4. 暂不迁动 `CanonicalSpec`、`WorkflowSpec`、`ImplementationLog`
5. 暂不处理 `Docs` 顶层重复目录问题

这样可以先验证：

- 团队是否真的会把 `ProjectPlan.md` 当作开工入口
- `ongoing → complete` 的流程是否顺手
- 新体系是否能减少“去哪看”的沟通成本

---

## 12. 最终建议

本设计建议采用 **“总 plan + ongoing + complete”** 的混合模式，并以增量方式落地。

它的价值不在于“把文档排得更整齐”，而在于建立一个真正可用的项目驾驶舱：

- 看总貌，有 `ProjectPlan.md`
- 看当前执行，有 `ongoing/`
- 看已完成专项，有 `complete/`
- 看规则，去 `Design / Spec`
- 看历史，去 `ImplementationLog`

这能有效缓解当前文档体系中“计划散落、入口分裂、职责混用”的问题，并为项目后续阶段切换与专项推进提供更稳定的工作流基础。
