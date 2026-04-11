# Level-Architect-Workbench

## 文档定位

本文件是 `Level` 模块关于 **`Level Architect` 作者体验重构** 的正式专项计划。

它负责维护：

- 为什么现在值得做这轮 `Level Architect` 重构
- 本轮真正要优化的作者体验与北极星目标
- 范围边界与架构约束
- MVP 与后续增强的拆分
- 分阶段执行顺序、完成标准与风险提醒
- 与 `HTML` 设计器、`WorkflowSpec`、运行时 authority 的关系

它**不**替代以下真相源：

- `Docs/2_Design/Level/Level_CanonicalSpec.md`
- `Docs/2_Design/Level/Level_WorkflowSpec.md`
- `Docs/0_Plan/ongoing/Level-RoomRuntimeChain-Hardening.md`
- `Docs/5_ImplementationLog/ImplementationLog.md`

一句话原则：

> **把 `HTML` 的交互心智搬进 Unity，把 `Scene / Room / Door / RoomSO` 继续保留为 authority。**

---

## 当前目标

> **把 `Level Architect` 从“能搭，但体验跳的工具集合”，收口成一个 `Scene-backed authoring workbench`：`Palette + Canvas + Inspector + Preview + Validate`。**

当前最值得解决的，不是“功能缺失”，而是 **authoring loop 不连续**：

- `Build` Tab 更像工具入口集合，而不是连续搭建工作台
- 选中房间后，Unity 端主要看到的是只读摘要，不是可直接修改的 `Inspector`
- `Connect` 解决了“连上”，但还没有把连接语义做成一等公民
- `Validate` 现在更像结果列表，还没升级成 authoring coach
- 新房默认命名仍偏时间戳型，不利于切片作者按语义思考结构
- `WorkflowSpec` 里的 `HTML` / `Build` 心智仍有分叉，导致团队默认入口不够稳定

这轮专项的目标不是再造一个浏览器工具，而是让 `Level Architect` 在 Unity 内拥有接近 `HTML` 的工作流顺滑度。

---

## 当前状态与启动时机

- **状态**：待启动（提案已立项，尚未进入实施）
- **优先级判断**：高价值，但优先级暂低于 `Level-RoomRuntimeChain-Hardening` 的代表房间验证与 `Gate L` 收尾
- **建议启动时机**：当 `Room` 运行时消费链完成最终验证，且下一阶段重点转向“场景配置与验证效率”时
- **为什么现在先写 plan**：当前项目已经进入 `Level` 场景 authoring / 配置密集期，若没有统一北极星，后续容易继续在 `Build`、`HTML`、`Validate`、手工 Scene 操作之间分裂演化

---

## 目标体验（North Star）

### 1. Palette：明确起手点

作者在窗口中能快速选择并放置：

- `Safe / Transit / Combat / Arena / Reward / Boss`
- `Corridor` / 常用切片模板
- 后续高频 runtime 元素入口（如 `Checkpoint`、`Lock`、`OpenEncounterTrigger`、`BiomeTrigger`）

目标不是“塞进所有东西”，而是把**最常见的第一步动作**收口成清晰入口。

### 2. Canvas：继续以 SceneView 为主画布

`SceneView` 继续是空间布局与白盒搭建的主画布。

保留并强化已有能力：

- `Blockout`
- `Connect`
- 相邻自动连线
- Overlay / Floor 过滤
- 快速聚焦、选择与批量操作

本轮不推翻现有 `SceneView` authoring，而是补齐它缺少的配套层。

### 3. Inspector：选中即改，而不是选中只看

当作者选中单个房间或一条连接时，右侧面板应立即进入**可编辑 authoring 态**。

房间 `Inspector` 至少应覆盖：

- `RoomID`
- `DisplayName`
- `RoomNodeType`
- `FloorLevel`
- 房间尺寸 / 常用结构摘要
- `EncounterSO`
- Connected Rooms 摘要
- 常用快捷动作（`Focus`、`Duplicate`、`Stable Rename`、`Mark as Entry` 等）

连接 `Inspector` 至少应覆盖：

- `From Room` / `To Room`
- `ConnectionType`
- 当前 Door / Spawn 关系摘要
- 连接方向与回路语义
- 常用快捷动作（删除、翻转、重算落点、转回路连接等）

### 4. Preview：结构反馈始终可见

作者在搭建过程中，应能随时看到：

- 房间数 / 连接数 / 当前楼层统计
- 孤岛、断链、单向连接、无 Door 房等结构问题
- `RoomNodeType` 分布
- 简版节奏摘要（如 `Safe → Combat → Reward`）
- “下一步建议”而不是只给错误清单

### 5. Validate：从结果列表升级为 authoring coach

`Validate` 仍是最终护栏，但窗口本身应提前暴露关键问题，让作者在切换到验证前就能看见：

- 什么是阻塞问题
- 什么是建议补齐
- 什么是当前切片还缺的最小闭环动作

### 6. Quick Play：维持 smoke test 定位

`Quick Play` 保持“结构冒烟验证”定位，不扩张成完整验收工具。

本轮重点是让作者更快走到“值得进 Play Mode 验”的状态，而不是让 `Quick Play` 承担更多运行时职责。

---

## 架构约束（必须遵守）

### 1. 不新增第二套 authoring authority

- 空间结构 authority 仍然是 Scene 中的 `Room` / `Door`
- 房间元数据 authority 仍然是 `RoomSO` / `EncounterSO` 等资产
- `Level Architect` 只增强编辑体验，不引入新的中间真相源

### 2. 像 `HTML` 的是交互模型，不是 JSON 中间层

- 不把 `Level Architect` 做成另一个 `HTML → JSON` 工具
- 不在 Unity 内再维护一份独立拓扑数据模型作为正式状态
- 若后续新增 `Topology View`，它也只能是 Scene 数据的**投影层**，不是 authority

### 3. Editor metadata 只能做辅助，不得接管 runtime

如果为了作者体验需要引入 editor-only metadata（如 `DisplayName`、`Act`、`Tension`、`BeatName` 等），必须满足：

- 语义清楚
- owner 清楚
- 不直接假装自己是 runtime authority
- 不自动生成一整套场景对象去“猜”设计意图

### 4. 运行时元素入口只做 assist，不做全自动猜测

可增加 `Checkpoint`、`Lock`、`OpenEncounterTrigger` 等高频入口，但本轮不追求“一键全自动补齐所有 runtime 元素”。

原则是：**帮助作者少点几步，不替作者做设计判断。**

### 5. 文档与工具口径必须同步

如果 `Build` 被正式提升为主 authoring 路径，后续必须同步：

- `Level_WorkflowSpec.md`
- `Level Architect` 窗口文案
- 模板与按钮文案
- 验证提示与团队默认口径

---

## 范围

### In Scope

- `Assets/Scripts/Level/Editor/LevelArchitect/LevelArchitectWindow.cs`
- `Assets/Scripts/Level/Editor/LevelArchitect/BlockoutModeHandler.cs`
- `Assets/Scripts/Level/Editor/LevelArchitect/DoorWiringService.cs`
- `Assets/Scripts/Level/Editor/LevelArchitect/BatchEditPanel.cs`
- `Assets/Scripts/Level/Editor/LevelArchitect/RoomFactory.cs`
- `Assets/Scripts/Level/Editor/LevelArchitect/LevelValidator.cs`
- `Docs/2_Design/Level/Level_WorkflowSpec.md`（实施后同步）
- 与 `Level Architect` 直接相关的编辑器辅助面板 / authoring metadata / 模板资产

### Out of Scope

- `Level` 全模块 runtime 架构重写
- 新建一套独立图编辑器取代 `SceneView`
- 再造 `HTML / JSON` 双轨主链
- 全自动生成全部 runtime 元素
- 顺手重写 `Room` / `Door` / `RoomSO` 的 authority 定义
- 把 `Quick Play` 扩张成完整 Playtest / QA 框架

---

## 完成标准

当本专项达到以下 6 条时，才算第一轮重构完成：

1. **单房作者体验成立**：选中单个 `Room` 后，可以在 `Level Architect` 中直接完成主要 authoring 字段编辑，而不是只读查看
2. **连接语义成为一等公民**：至少存在可用的连接 `Inspector`，能查看并修改 `ConnectionType` 与关键连接摘要
3. **单屏闭环成立**：作者在窗口中能完成“放置 / 连接 / 修改 / 看反馈 / 决定下一步”的连续循环
4. **预览与建议可见**：工具能主动暴露拓扑摘要、缺口与下一步建议，而不是只在 `Validate` 里事后报错
5. **authority 仍然清楚**：没有长出新的中间真相源；`Scene / Door / SO` 的 authority 不被稀释
6. **MVP 对实际切片有提速**：至少一个最小验证切片的搭建流程明显更顺，工具不再要求作者频繁在 `SceneView`、文档和外部草图之间来回跳

---

## MVP 与未来增强

### MVP（第一轮必须完成）

- 把单房面板从只读摘要升级为可编辑 `Inspector`
- 给连接补最小可用的语义 `Inspector`
- 引入稳定命名 / `DisplayName` / `Stable Rename` 的 authoring 支持
- 在窗口中补 `Preview / Summary / Next Step` 区块
- 增加一个 `5-Room Validation Slice` 或等价的最小切片模板入口
- 让 `Build` 路径的默认建议、窗口文案与计划文档保持一致

### 未来增强（不并入第一轮）

- `Topology View` / 简版关系图
- `Act` / `Tension` / `Beat` 等设计元数据的可视化
- Runtime 元素快捷创建向导
- 基于代表房间的 authoring coach 规则扩展
- 更系统的多房批量编辑与节奏检查

---

## 工作拆分总览

| 步骤 | 名称 | 目标 | 产出 | 通过标准 |
| --- | --- | --- | --- | --- |
| LA0 | 冻结北极星与 authority | 先统一“要像 `HTML` 的到底是什么” | 目标体验、边界与反目标清单 | 团队能用一句话说明本轮不是再造 JSON 工具 |
| LA1 | 升级房间 Inspector | 把单房选中后的体验从只读改为可编辑 | 房间 authoring 面板 MVP | 主要房间字段可在窗口中直接改 |
| LA2 | 补连接语义面板 | 让连接成为一等公民 | 连接 `Inspector` MVP | `ConnectionType` 与连接摘要可直接查看/修改 |
| LA3 | 补预览与下一步建议 | 让作者始终知道当前结构状态 | `Preview / Summary / Next Step` 面板 | 作者不打开 `Validate` 也能看见关键缺口 |
| LA4 | 补最小切片模板与稳定命名 | 让起手更快、命名更像语义 authoring | 切片模板入口、稳定命名策略 | 可更快起一个验证切片，不再依赖时间戳记忆 |
| LA5 | 补高频 runtime assist 入口 | 减少最常见场景补件摩擦 | 运行时高频元素快捷入口 MVP | 常见补件步骤减少，但不引入自动猜测 |
| G | Gate A 验收 | 判断第一轮 workbench 是否成立 | 代表切片 authoring 复盘 | 满足本文件的 6 条完成标准 |

---

## 分步执行细则

## Step LA0 — 冻结北极星与 authority

### 目标

先明确：

- 这轮要学 `HTML` 的是**交互闭环**，不是数据模型
- `Level Architect` 的目标是 `Scene-backed workbench`
- `Scene / Room / Door / SO` 的 authority 不动摇

### 要做什么

- 把目标体验统一表述为 `Palette + Canvas + Inspector + Preview + Validate`
- 明确 `HTML` 保留为可选外部规划入口，而不是本轮对标的 authority
- 把“不要做什么”写清：不新增 JSON 主链、不造第二套拓扑真相源、不把 `Quick Play` 职责做大

### 完成标准

- 团队内部不再出现“是不是要把 Unity 工具再做成一个浏览器设计器”的歧义
- 后续需求取舍可直接对照本计划判断是否在 scope 内

---

## Step LA1 — 升级房间 Inspector

### 目标

让单房 authoring 在一个窗口里完成主要动作，而不是“选中 → 看摘要 → 再去别处改”。

### 要做什么

- 把 `DrawSingleRoomInfo()` 升级为真正的单房 `Inspector`
- 支持 `RoomID`、`DisplayName`、`RoomNodeType`、`FloorLevel`、尺寸、`EncounterSO` 等核心字段编辑
- 补常用快捷动作：`Focus`、`Duplicate`、`Stable Rename`、`Select Connected Rooms`
- 保持多选时继续走 `BatchEditPanel`，不破坏已有批量编辑路径

### 完成标准

- 作者调整单房核心 authoring 字段时，不再需要主要依赖外部 Inspector
- 单房选中后的下一步动作对作者是显眼、低摩擦的

---

## Step LA2 — 补连接语义面板

### 目标

让 `Connect` 不只负责“连上”，还负责“连得有语义”。

### 要做什么

- 支持在窗口中选中一条连接或一对 Door link
- 暴露 `From Room`、`To Room`、`ConnectionType`、方向与目标落点摘要
- 补最小快捷动作：删除、翻转方向、转成 `return` / 回路连接、重算落点

### 完成标准

- `ConnectionType` 不再主要依赖外部工具或隐式默认值
- 连接在 authoring 体验里真正成为一等公民，而不是 Scene 里的一条“辅助线”

---

## Step LA3 — 补预览与下一步建议

### 目标

把“状态可见”补回来，让 `Level Architect` 像 `HTML` 一样始终给作者结构反馈。

### 要做什么

- 增加拓扑摘要：房间数、连接数、孤岛数、单向连接数、当前楼层统计
- 增加语义摘要：`RoomNodeType` 分布、简单节奏序列、关键房型缺口
- 增加“下一步建议”：例如缺 `Entry`、`Arena` 缺 `EncounterSO`、有房无 Door、结构能通但节奏不清

### 完成标准

- 作者看一眼窗口，就能知道“当前切片已经到哪一步”“最该补什么”
- `Validate` 不再是首次暴露结构问题的唯一时刻

---

## Step LA4 — 补最小切片模板与稳定命名

### 目标

降低起手成本，让作者更按语义而不是按时间戳记忆地图。

### 要做什么

- 增加 `5-Room Validation Slice` 或等价模板入口
- 为新房提供更稳定的命名 / 显示名 authoring 机制
- 把模板生成结果与 `Build` / `Validate` / `Quick Play` 的最小闭环配套起来

### 完成标准

- 新起一个验证切片的首轮搭建速度明显提升
- 新房命名更利于协作沟通与复盘，而不是只能靠位置记忆

---

## Step LA5 — 补高频 runtime assist 入口

### 目标

让最常见的 runtime 补件动作更顺手，但不跨越到“工具替作者猜设计”。

### 要做什么

- 提供高频入口：`Checkpoint`、`Lock`、`OpenEncounterTrigger`、`BiomeTrigger`、`ScheduledBehaviour`、`WorldEventTrigger`
- 与 `LevelValidator` 当前推荐挂点与根节点约定保持一致
- 入口只负责创建标准起点，不自动替作者填满全部业务细节

### 完成标准

- 高频补件步骤减少
- 新入口不会破坏现有场景层级规则，也不会制造新的 authority 混乱

---

## Gate A — 本专项验收门槛

在宣称第一轮 `Level Architect` workbench 成立前，至少要用一个代表性最小切片复盘以下问题：

1. 作者能否在一个窗口里完成“放房 → 连线 → 改语义 → 看摘要 → 决定下一步”？
2. 单房与连接是否都已经成为一等公民？
3. 作者是否明显减少了在 `SceneView`、外部文档和工具之间的来回跳转？
4. `HTML` 的优势是否被吸收到 Unity authoring loop，而不是继续依赖第二套工具做核心思考？
5. 新体验是否保持了 `Scene / Door / SO` authority 清晰，而不是又长出中间模型？
6. 本轮改动是否真的提升了最小验证切片的搭建效率，而不只是增加了更多按钮？

只有这些问题都能给出正向回答，本专项才应从 `ongoing` 进入后续完成或下一阶段拆分。

---

## 风险与注意事项

- **最大风险不是技术实现，而是 scope 膨胀**：很容易把“补 authoring loop”做成“重写整套关卡编辑器”
- **第二风险是 authority 漂移**：如果为追求像 `HTML` 而引入新的中间状态，后续维护成本会迅速上升
- **第三风险是过早自动化 runtime 元素**：一旦工具开始猜设计意图，错误成本会高于省下的点击数
- **第四风险是文档不同步**：如果工具已经改成 `Build` 主路，但 `WorkflowSpec` 和团队口径没同步，会继续制造协作歧义
- **第五风险是把 `Validate` 和 `Quick Play` 职责做大**：本轮目标是让作者更快到达验证点，而不是让这些工具承担更多系统职责

---

## 关联文档

- `Docs/2_Design/Level/Level_CanonicalSpec.md`
- `Docs/2_Design/Level/Level_WorkflowSpec.md`
- `Docs/0_Plan/ongoing/Level-RoomRuntimeChain-Hardening.md`
- `Tools/LevelDesigner.html`
- `Docs/5_ImplementationLog/ImplementationLog.md`
