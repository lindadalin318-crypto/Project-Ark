# Camera_MVP

## 文档定位

本文件是 `Project Ark` 关于 **探索态镜头改造（Camera MVP）** 的正式专项计划。

它负责维护：

- 本轮镜头体验改造的目标体验
- 范围边界与架构约束
- 完成标准
- MVP 与未来增强的拆分
- 分步执行顺序与验证方式
- 与 `Level` authoring、房间边界、局部导演触发的关系

它**不**替代以下真相源：

- `Docs/2_TechnicalDesign/Level/Level_CanonicalSpec.md`
- `Docs/3_WorkflowsAndRules/LevelArchitect/Level_WorkflowSpec.md`
- `Docs/0_Plan/Project_Plan.md`
- `Docs/5_ImplementationLog/README.md`

一句话原则：

> **探索态镜头首先服务飞船手感，而不是服务房间边界。**

---

## Goal（目标体验）

> **把探索态 camera 从“房间边界优先”收口到“飞船始终尽量保持屏幕中心、局部导演通过 soft bias / zoom 完成”的状态。**

这轮专项要解决的核心体验问题不是“镜头有没有被限制”，而是：

- 玩家在普通探索房靠近任意边缘时，镜头仍应尽量跟住飞船，而不是先停在房间边界
- 玩家不应感受到“画幅卡边”带来的手感发粘、构图漂移和操作重心偏离
- 房间感、战斗感和叙事感应通过 **bias、zoom、触发器和世界空间视觉缓冲** 来建立，而不是默认靠 `CinemachineConfiner2D` 把相机夹在房间里

本轮方向来自两个已经明确的判断：

1. **当前目标体验已冻结**：普通探索房的 camera 要“永远尽量以飞船为中心”
2. **当前问题根因已定位**：镜头在房间边缘停住的主因不是 follow 配置，而是 `RoomCameraConfiner -> CinemachineConfiner2D` 这条“room bounds 优先”主链本身

---

## Scope（范围）

### In Scope

- `Assets/Scripts/Level/Camera/CameraDirector.cs`
- `Assets/Scripts/Level/Camera/CameraTrigger.cs`
- `Assets/Scripts/Level/Camera/RoomCameraConfiner.cs`
- `Assets/Scripts/Level/Room/Room.cs`（仅在需要 room-level camera policy 时补最小 authoring 字段）
- `Assets/Scripts/Level/Room/RoomManager.cs`（只检查与换房、复位、trigger 清理的协作边界）
- `Assets/Scripts/Level/Room/DoorTransitionController.cs`
- `Assets/Scripts/Level/GameFlow/GameFlowManager.cs`
- `Assets/Scripts/Level/Editor/LevelArchitect/RoomFactory.cs`
- `Assets/Scripts/Level/Editor/LevelArchitect/LevelSliceBuilder.cs`
- `Assets/Scenes/SampleScene.unity` 中代表性房间的镜头验证
- `Docs/2_TechnicalDesign/Level/Level_CanonicalSpec.md`
- `Docs/3_WorkflowsAndRules/LevelArchitect/Level_WorkflowSpec.md`

### Out of Scope

- 重写整套相机系统或替换 `CinemachineCamera`
- 一次性做完 Boss 全量动态 framing 系统
- 一次性清理所有历史房间边界资产
- 通过相机系统兜底解决所有关卡边缘露馅问题
- 为了“通用优雅”而先设计过重的镜头框架中间层

---

## Architecture（架构约束）

### 1. 探索态镜头的第一真相源是飞船，而不是房间边界

- 普通探索房中，camera 默认跟随飞船
- “飞船尽量居中”优先级高于“画幅完全不越出房间边界”
- 若两者冲突，默认优先保飞船跟随手感

### 2. `CameraConfiner` 从默认基础设施降级为特例工具

- `RoomCameraConfiner` 不再默认接管探索态镜头
- hard confine 只保留给少数**确有必要**的场景：
  - 纯演出镜头
  - 极小房间且边缘露馅风险不可接受
  - 临时过渡阶段尚未补齐视觉缓冲的特殊房间
- confiner 不再作为“所有房间的标准镜头行为”写进主链心智

### 3. 局部导演优先使用 soft bias，而不是 hard clamp

- Arena / Boss / 特殊景别应优先通过以下手段构图：
  - `zoom`
  - `bias target`
  - `bias weight`
  - `look-ahead` / 前向 lead
- 只有当上述手段仍不足以满足画面约束时，才考虑 hard confine

### 4. 房间边缘可见性应由关卡 authoring 承担，而不是全部甩给 runtime 相机

- 镜头越界后可能看到 room 外区域，这是新的 authoring 约束，而不是新的 runtime bug
- 优先解法：
  - 视觉缓冲带
  - 邻房本就可见的世界空间布局
  - 雾幕 / 遮挡 / 深空背景
  - 极少数特殊房间局部启用 confine

### 5. 不新增第二套镜头 authority

- 继续以 `CameraDirector` 作为镜头总控入口
- `CameraTrigger` 负责局部导演请求
- `RoomManager` 仍只负责换房 authority，不直接管局部 framing
- 不引入另一个“CameraManager 2.0”或额外全局 runtime singleton

### 6. 换房 / 复活 / 叙事转场仍要保持镜头复位稳定

- `DoorTransitionController`
- `GameFlowManager`
- 未来可能继续扩展的 `NarrativeFallTrigger`

这些链路在清 trigger、恢复 follow、切换 room 时必须继续稳定成立；本轮不能为了取消 confiner 而把转场稳定性打坏。

---

## 完成标准（Done Checklist）

1. **探索态居中成立**：普通探索房中，飞船靠近任意边缘时，镜头仍尽量保持飞船居中，不再出现明显“画幅卡边停住”
2. **手感问题消失**：探索态不再因为房间边界导致镜头发粘、拖滞或视觉重心偏离
3. **导演能力保留**：Arena / Boss / 叙事镜头仍能通过 zoom、bias 或锁点触发完成局部构图
4. **转场稳定**：过门、复活、坠落演出后，相机能稳定恢复到正确的 follow / trigger 状态
5. **authoring 口径同步**：`Level_CanonicalSpec`、`Level_WorkflowSpec` 与工具默认行为不再继续把 `CameraConfiner` 当探索态默认方案
6. **边缘可视可接受**：代表性房间试玩中，不会因为取消默认 confiner 而出现不可接受的露馅或阅读障碍

---

## 当前状态

- **状态**：待启动（方案已冻结，待进入 MVP 实施）
- **设计结论已明确**：普通探索房的 camera 目标已经定为“永远尽量以飞船为中心”
- **现状问题已明确**：当前 `RoomCameraConfiner` 会在 `RoomManager.OnCurrentRoomChanged` 后把 `Room.ConfinerBounds` 挂到 `CinemachineConfiner2D`，这条链直接导致房间边缘镜头停住
- **authoring 冲突已明确**：`RoomFactory` / `LevelSliceBuilder` 目前仍把 `CameraConfiner` 当标准房间基础设施自动生成；`Level_CanonicalSpec` / `Level_WorkflowSpec` 仍沿用旧口径
- **有利条件**：当前项目仍处于场景配置与验证阶段，尚未进入无法调整镜头主链的大规模内容冻结期

---

## MVP 与未来增强

### MVP（本轮必须完成）

- 探索态从默认 room confine 中解耦
- 验证“飞船始终尽量居中”是否显著改善探索手感
- 保住现有 `CameraDirector` 的 mode / zoom / trigger / transition 主链
- 给 Arena / Boss 留下 soft bias 的接入口，但不一开始就做过重系统
- 把文档与 authoring 默认行为同步到新的镜头哲学

### 未来增强（不并入本轮）

- 完整的 `bias target + weight + damping` 体系
- 动态 look-ahead / aim lead / velocity lead
- Boss 双主体 framing
- 更细粒度的 room-level camera policy authoring 面板
- 专门的 camera validation / visual buffer authoring checklist

---

## 工作拆分总览

| 步骤 | 名称 | 目标 | 产出 | 通过标准 |
| --- | --- | --- | --- | --- |
| C0 | 冻结体验与 authority | 先把“探索态居中优先”与 confiner 降级原则写死 | 体验 / authority 口径 | 团队能明确回答“探索态第一真相源是谁” |
| C1 | 解除探索态默认 confine | 让普通探索房不再被 room bounds 默认硬夹住 | 运行时默认行为改造 | 探索房贴边时镜头仍尽量跟飞船 |
| C2 | 保留局部导演能力 | 确保 Arena / Boss / 演出不因取消 confiner 而失去构图能力 | trigger / framing 过渡方案 | 特殊镜头仍能达成想要的景别 |
| C3 | 同步 authoring 默认行为 | 让房间创建工具与规范文档不再持续产出旧哲学 | 工具默认值与规范更新 | 新建房间不会默认强化旧 confiner 心智 |
| C4 | 代表房间试玩验收 | 在真实房间中验证手感、边缘可视与转场稳定性 | 验证记录 | Done checklist 通过 |

---

## 分步执行细则

## Step C0 — 冻结体验与 authority

### 目标

先明确本轮不是“调一个更舒服的 confiner 参数”，而是**切换镜头的第一约束**：

- 探索态：飞船优先
- 特殊导演：bias / zoom / lock 优先
- hard confine：降级到特例工具

### 要做什么

- 把“普通探索房永远尽量以飞船为中心”写成正式口径
- 明确 `RoomCameraConfiner` 不是探索态默认 authority
- 明确 `CameraDirector` 仍是总控，不另起炉灶
- 冻结 MVP 与未来增强边界，避免直接长成大系统重写

### 完成标准

- 团队内部不会再把“探索态房间边界优先”当作默认前提
- 后续每个代码改动都能对照这个 authority 口径判断是否偏航

---

## Step C1 — 解除探索态默认 confine

### 目标

先用最小改动验证最重要的问题：**只要不再默认硬夹镜头，探索手感是否立刻更对。**

### 要做什么

- 调整 `RoomCameraConfiner` 的消费策略，使其不再对普通探索房默认生效
- 如有需要，为 `Room` 补一个最小的 room-level camera policy 开关，但避免一开始就长成复杂 profile 体系
- 确认切房后镜头仍由 `CameraDirector` 正常恢复 follow，不因 confiner 缺位而失去跟随
- 在代表性普通房 / 过道 / 安全房里直接试玩验证

### 完成标准

- 飞船贴近房间边缘时，镜头不会先被 room polygon 卡住
- 玩家操作时明显感受到“船是中心，房间是背景”，而不是相反
- 取消默认 confine 后，过门与复活不会出现错误相机状态

---

## Step C2 — 保留局部导演能力

### 目标

取消探索态默认 confine 后，仍保住 `Arena`、Boss、叙事触发对镜头景别和关注点的控制力。

### 要做什么

- 评估当前 `CameraTrigger` 的三种模式：恢复 follow / 完全跟随 override / lock 点
- 为后续 soft bias 预留最小扩展位：
  - `BiasTarget`
  - `BiasWeight`
  - `BiasBlendDuration`
- MVP 阶段允许先不把整套 bias 做满，但必须确定触发器未来不再只剩“硬切 follow target”这一种表达能力
- 对 `Arena` / Boss / 演出镜头列出最小代表案例，确认取消默认 confine 后不会直接丢镜头语言

### 完成标准

- 项目不会因为取消默认 confine 而退化成“所有镜头都只会傻跟飞船”
- 局部导演能力有清晰延续路线，而不是重新依赖 hard confine 兜底

---

## Step C3 — 同步 authoring 默认行为

### 目标

防止运行时改完后，编辑器工具和规范文档还在持续生成旧镜头哲学。

### 要做什么

- 重新定义 `RoomFactory` / `LevelSliceBuilder` 对 `CameraConfiner` 的默认处理方式
- 明确 `CameraConfiner` 是“可选特例件”还是“保留节点但默认不接主链”
- 更新 `Level_CanonicalSpec` 中关于 room camera behavior 的现役口径
- 更新 `Level_WorkflowSpec` 中房间标准结构与检查项的表达，避免继续把 confiner 写成探索态默认需求

### 完成标准

- 新建房间不会再天然把“镜头必须被 room bounds 限制”灌进 authoring 心智
- 规范文档、工具默认行为与 runtime 主链三者一致

---

## Step C4 — 代表房间试玩验收

### 目标

用真实房间验证：这次不是纸面上更合理，而是真的更好玩。

### 最少覆盖案例

- 普通探索房：贴边移动、环绕观察、边缘战斗
- 安全房 / 小房间：确认去掉默认 confine 后不会立刻严重露馅
- 过门切换：确认进入新房后仍稳定回到 follow
- 复活：确认 `GameFlowManager` 复位后镜头状态正确
- 至少 1 个 Arena 或 Boss 构图案例：验证特殊镜头仍可控

### 完成标准

- 探索态居中手感通过主观试玩验收
- 没有新增明显的边缘视觉灾难或转场问题
- 对需要保留 hard confine 的极少数案例形成明确名单，而不是再次回到“全部房间默认 confine”

---

## 风险与注意事项

### 1. 取消默认 confine 会暴露关卡边缘 authoring 质量

这不是失败，而是原本被相机系统遮住的问题开始显形。

### 2. 不要把“看到房间外一点点内容”自动判定为 bug

如果视觉上可接受、阅读上无害，那更符合当前探索态目标。

### 3. 不要直接把 `CameraTrigger` 改成另一套过重框架

本轮先保住主手感，再逐步补 soft bias 能力；避免在体验问题尚未验证前先大重构。

### 4. 转场稳定性优先级很高

过门、复活、叙事触发若因本轮改动变脆，会直接伤害主流程体验；这类问题优先级高于额外镜头 polish。

---

## 关联文档

- `Docs/0_Plan/Project_Plan.md`
- `Docs/2_TechnicalDesign/Level/Level_CanonicalSpec.md`
- `Docs/3_WorkflowsAndRules/LevelArchitect/Level_WorkflowSpec.md`
- `Docs/8_Obsolete/Plan/LevelModule_Plan.md`（仅作历史口径对照，不作为现役真相源）
