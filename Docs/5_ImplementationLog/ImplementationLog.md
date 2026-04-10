 # Implementation Log — Project Ark

---

## Level 房间元素分类规范同步到设计文档与诊断表 — 2026-04-10 15:10

### 修改文件
- `Docs/2_Design/Level/Level_CanonicalSpec.md`
- `Docs/2_Design/Level/Level_WorkflowSpec.md`
- `Docs/6_Diagnostics/Level_RoomElements_Findings_2026-04-10.md`
- `Docs/6_Diagnostics/Level_RoomElements_Findings_2026-04-10.csv`

### 内容
- 将 `Level_CanonicalSpec.md` 中的现役房间元素矩阵正式改为新分类规范：`通路件 / 交互件 / 状态件 / 战斗件 / 环境机关件 / 导演件 / 基础设施件`，并加入对应英文标签 `Path / Interact / Stateful / Combat / Environment / Directing / Infrastructure`。
- 将 `Level_WorkflowSpec.md` 的“新增房间元素接入 SOP”和“分类落位速查”统一切换到新分类规范，确保 authoring 流程、默认挂点判断和新增元素决策树与模块规则一致。
- 将 `Level_RoomElements_Findings_2026-04-10.md` 从“推荐命名”口径升级为“现役分类主表”，保留旧术语仅作为兼容说明，不再把旧家族名当作当前规范。
- 重写 `Level_RoomElements_Findings_2026-04-10.csv` 的分类字段，改为 `category_cn + category_en` 双列，并将逐元素记录统一映射到新分类标签；同时把 `Room infrastructure`、`Interact Anchor`、`Encounter Element` 等旧值全部切换到新规范。
- 额外统一了 `CanonicalSpec` / `WorkflowSpec` 中零散的“互动件”表述，全部收口为“交互件”，避免同一分类出现多个中文叫法。

### 目的
- 让 `Level` 的模块规则、设计规范、搭建手册、诊断表四处使用同一套房间元素分类口径，消除“规则是新名、设计文档还是旧名”的双轨状态。
- 为后续继续扩 `LevelValidator`、新增房间元素和做关卡 authoring 评审提供统一、可筛选、可复用的分类语言。

### 技术
- 文档规范同步：以 `Implement_rules.md` 中已确立的分类规则为基线，回写到 `CanonicalSpec`、`WorkflowSpec` 和诊断产物。
- 结构化数据升级：对 CSV 做字段级 schema 调整，用 `category_cn` / `category_en` 取代旧的 `canonical_family` 口径。

## Level 房间元素分类规范写入模块规则 — 2026-04-10 15:00

### 修改文件
- `Implement_rules.md`

### 内容
- 在 `Implement_rules.md` 的 `Level` 模块规则中新增房间元素分类总则，正式把 `通路件 / 交互件 / 状态件 / 战斗件 / 环境机关件 / 导演件 / 基础设施件` 这套口径写入治理文档。
- 同步加入简洁英文标签约束：`Path`、`Interact`、`Stateful`、`Combat`、`Environment`、`Directing`、`Infrastructure`，并明确这些短名只用于文档、Inspector 分组、Validator 输出和 authoring 沟通，不替代具体组件类名。
- 新增“新房间元素必须先落类，再落实现”规则，要求后续任何 `Level` 新元素在实现前先明确所属分类、默认挂点、Runtime owner、是否进入 `Room` 主链、是否接入 Save。
- 新增“分类不是运行时 owner，也不是 `Room` 主链声明”的边界说明，并把这套规则接入 `Level` 模块的常规验收清单与推荐工作流，避免后续再次混淆 authoring 分类、运行时消费层级和基础设施职责。

### 目的
- 将前面在诊断文档里提炼出的房间元素命名与分类规范，升级为模块级长期规则，而不是停留在一次性分析结论。
- 为后续新增 `Level` 元素、扩展 `LevelValidator`、补齐 `CanonicalSpec` / `WorkflowSpec` 提供统一、简洁、可执行的口径。

### 技术
- 规则沉淀：以 `Implement_rules.md` 为治理入口，把人话版分类、英文标签、落类决策和边界说明写成可执行条款。
- 流程收口：同时更新验收清单与推荐工作流，确保这套分类规则在后续实现流程中真正被使用。

## Level 房间元素分类命名收口（人话版 + 简洁英文）— 2026-04-10 14:54

### 修改文件
- `Docs/6_Diagnostics/Level_RoomElements_Findings_2026-04-10.md`

### 内容
- 将 `Level_RoomElements_Findings_2026-04-10.md` 中“房间元素家族”章节改写为更贴近 authoring 和玩家感知的版本，把原有技术口径收口为：`通路件 / 交互件 / 状态件 / 战斗件 / 环境机关件 / 导演件`，并将 `SpawnPoint`、`CameraConfiner` 单列为 `基础设施件`。
- 在文档中新增“推荐命名（中文 / 英文）”对照表，明确当前分类层可使用更简洁的英文标签：`Path`、`Interact`、`Stateful`、`Combat`、`Environment`、`Directing`，同时强调这些短名用于文档、Inspector 分组、Validator 输出与沟通，不替代具体组件类名。
- 同步把逐元素结论矩阵改写为新的清晰分类和英文标签，避免正文使用新口径、表格仍保留旧术语，造成文档内部语义割裂。
- 更新最终判断表述，将总结中的“六大家族”收口为“六大玩法家族 + 一类基础设施件”，让后续团队讨论时更容易统一认知边界。

### 目的
- 降低 `Level` 房间元素分类的理解门槛，让分类更适合用于关卡 authoring、设计评审和验证讨论。
- 为后续若要把这套分类继续写入 `CanonicalSpec` / `WorkflowSpec` 或 `LevelValidator` 输出文案，先建立一套更短、更稳的人类可读命名口径。

### 技术
- 文档结构重写：在不改变本轮验证事实的前提下，重组分类章节、命名映射表和逐元素矩阵的表头与家族字段。
- 命名分层：区分“分类标签”与“具体组件类名”，避免把简洁英文分类误用为代码级命名替代。

## Level 房间元素验证结论 MD 沉淀 — 2026-04-10 14:37

### 新建文件
- `Docs/6_Diagnostics/Level_RoomElements_Findings_2026-04-10.md`

### 内容
- 新增 `Level_RoomElements_Findings_2026-04-10.md`，将本轮 `Level` 模块房间元素验证结果从对话结论和 CSV 矩阵进一步沉淀为完整 Markdown 报告。
- 报告按“文档目的 → 结论先说 → 验证口径 → 元素家族 → 运行时消费分层 → `SampleScene` 实际挂载 → 标准根节点覆盖 → 逐元素结论矩阵 → 编辑期 schema 边界 → 当前配置问题 → 最终判断 → 后续优先级”组织，便于后续评审和复盘。
- 明确保留本轮最重要的结构性判断：`SampleScene` 当前有 17 个 `Room`，但只有 2/17 完整具备标准根节点；当前场景现役成熟元素集中在 `Door`、`Checkpoint`、`Lock`、`PickupBase`、`EnemySpawner`、`ArenaController`、`EnvironmentHazard`；`rooms[].elements[]` 与 `ScaffoldElementType` 不是运行时 authority。
- 在 Markdown 中额外补充了人类可读的分层说明和逐元素表格，降低后续只看 CSV 时的信息压缩感。

### 目的
- 为后续 `Level` authoring 收口、场景补铺和验证复盘提供一份适合直接阅读、讨论和引用的结论文档。
- 与 CSV 形成互补：CSV 用于过滤和筛选，Markdown 用于表达完整判断逻辑与证据链。

### 技术
- 文档沉淀：基于已生成的 `Level_RoomElements_Findings_2026-04-10.csv` 与本轮场景验证结论，重组为面向阅读的 Markdown 报告。
- 结论重组策略：保留 CSV 的矩阵信息，同时补充运行时分层、场景状态解释与验证口径，避免后续再次从零拼接推理链。

## Level 房间元素验证结论 CSV 沉淀 — 2026-04-10 14:32

### 新建文件
- `Docs/6_Diagnostics/Level_RoomElements_Findings_2026-04-10.csv`

### 内容
- 新增 `Level_RoomElements_Findings_2026-04-10.csv`，把本轮 `Level` 模块房间元素验证的发现、证据和结论收口为单文件 CSV。
- CSV 同时覆盖 3 层信息：规范 / 架构层结论、逐元素矩阵、编辑期 schema 非运行时 authority 结论，避免后续再次把“代码存在”“场景已挂载”“运行时主链已消费”混为一谈。
- 逐元素行包含 `canonical family`、默认挂点、运行时 owner / 入口、是否进入 `Room` 主链、是否组件自治、状态通道 / Save、是否纳入 `LevelValidator`、`SampleScene` 实例数、现役成熟度、最终结论等字段。
- 明确固化本轮几个关键判断：`SampleScene` 当前有 17 个 `Room`，但只有 2/17 完整具备标准根节点；当前场景主流已实装元素集中在 `Door`、`Checkpoint`、`Lock`、`PickupBase`、`EnemySpawner`、`ArenaController`、`EnvironmentHazard`；`rooms[].elements[]` 与 `ScaffoldElementType` 不是 Unity 运行时实装 authority。

### 目的
- 将本轮关卡验证结果从对话结论沉淀为结构化数据资产，方便后续筛选“现役可实装”“代码支持但未铺开”“编辑期模型”三类元素。
- 为下一步继续做 `Level` authoring 收口、场景补铺和 Validator 扩护栏提供统一的核对表。

### 技术
- 结构化文档沉淀：使用 CSV 而非 Markdown，便于按列过滤运行时主链、场景实例、Validator 覆盖和成熟度。
- 结论组织方式：将整体诊断结论与逐元素事实矩阵放在同一文件，通过 `section` 字段区分 `summary`、`element` 与 `editor_schema`。

## Camera 对比分析文档沉淀（Project Ark × Minishoot）— 2026-04-10 13:18

### 新建文件
- `Docs/7_Reference/GameAnalysis/ProjectArk_Minishoot_Camera_Analysis.md`

### 内容
- 新增 `ProjectArk_Minishoot_Camera_Analysis.md`，把本轮对 `Project Ark` 与 `Minishoot` 的镜头调研结果沉淀为可复用参考文档。
- 文档按“Ark 当前镜头链路 → Minishoot 镜头链路 → 核心差异 → 可借鉴点 → 阶段性判断 → 后续迭代方向”组织，避免后续继续讨论 camera 时重复考古脚本与场景配置。
- 明确记录 Ark 当前 `SampleScene` 的关键状态：已接入 `CinemachineCamera + PositionComposer + Confiner2D + Impulse`，但 `Lookahead` 关闭、`TargetOffset = 0`、`CameraTrigger` 尚未在场景实际 authoring 中使用。
- 明确记录本轮最核心结论：Ark 不缺 camera 底座，当前主要缺 `Minishoot` 式的前向 lead、速度驱动 zoom / follow、StickyPoint 兴趣点机制、独立 Camera FX / Juice 层。

### 目的
- 将本次 camera 对比结论固化为后续镜头改造的基线文档，降低反复读取脚本与场景配置的沟通成本。
- 为下一步讨论 `Project Ark` 的镜头 MVP 改造方案提供统一参考口径。

### 技术
- 文档沉淀：基于已完成的代码链路比对、场景配置核对与参考工程脚本调研，输出结构化分析文档。
- 对比维度聚焦：围绕房间边界、持续跟随、前向引导、兴趣点机制、Boss framing 与 Camera FX 分层做横向归纳。

---

## LevelValidator 第二批层级与 ActivationGroup 校验扩展 — 2026-04-01 23:06

### 修改文件
- `Assets/Scripts/Level/Editor/LevelArchitect/LevelValidator.cs`
- `Assets/Scripts/Level/Editor/LevelArchitect/LevelValidatorTests.cs`

### 内容
- 在 `LevelValidator.cs` 中新增 `ValidateActivationGroups()`：补上 `ActivationGroup` 不在 `Room` 下、没有任何成员/子节点、`_members` 中有空引用、引用了其他房间成员 等高价值警告。
- 在 `ValidateAll()` 主链接入第二批规则，并新增 `ValidatePreferredAuthoringRoots()`，把房间元素的官方挂点契约显式化：`Lock` / `Checkpoint` / `DestroyableObject` → `Elements`，`OpenEncounterTrigger` → `Encounters`，`BiomeTrigger` / `HiddenAreaMask` / `ScheduledBehaviour` / `WorldEventTrigger` → `Triggers`，`EnvironmentHazard` → `Hazards`，`ActivationGroup` → `Triggers` 或 `ActivationGroups`。
- 扩展 `LevelValidatorTests.cs`，新增第二批测试覆盖：`ActivationGroup` 无 `Room` 父级、空成员、挂错根节点，以及 `Lock` / `BiomeTrigger` 挂错官方根节点的失败场景。
- 通过 `dotnet build ProjectArk.Level.Editor.csproj -nologo -clp:ErrorsOnly` 验证当前改动编译通过（39 warnings / 0 errors）；再次探测 `dotnet test` 后，命令行仍未稳定返回 NUnit 用例执行结果，因此当前只确认到“测试文件已编译并纳入程序集”，未把 CLI 测试执行结果作为可信验收来源。

### 目的
- 把 `Level_WorkflowSpec.md` 中“新元素必须同步补固定挂点校验”的规则真正落到编辑器护栏上，避免组件虽然存在但因挂错根节点而悄悄偏离 `Room` 的运行时主链。
- 补足 `ActivationGroup` 这类 Trigger / Director 元素的最低可工作性检查，让“场景里摆了组但永远不响应房间切换”这种静默错误在编辑期就暴露出来。

### 技术
- 语义校验扩展：通过 `TryGetImmediateRoomRoot()` 提取组件位于 `Room` 下的一级根节点名称，将运行时的 preferred-root 假设升级为统一编辑期警告。
- Trigger / Director 护栏：用 `SerializedObject` 读取 `ActivationGroup._members`，在不猜测策划语义的前提下，只校验会导致行为失效或明显越界的数据问题。
- 测试先行：先把第二批错误场景补入 `LevelValidatorTests.cs`，再落生产代码；在当前 CLI 测试链不稳定的约束下，以“测试用例已落地 + 编辑器程序集编译通过”作为本轮可验证边界。

---

## LevelValidator 首批房间元素校验扩展 — 2026-04-01 22:57


### 修改文件
- `Assets/Scripts/Level/Editor/LevelArchitect/LevelValidator.cs`
- `Assets/Scripts/Level/Editor/LevelArchitect/LevelValidatorTests.cs`
- `ProjectArk.Level.Editor.csproj`

### 内容
- 在 `LevelValidator.cs` 中把首批新增元素规则接入 `ValidateAll()` 主链，新增对 `Lock`、`Checkpoint`、`HiddenAreaMask`、`BiomeTrigger`、`ScheduledBehaviour` 的统一扫描与校验。
- 为 `Lock`、`Checkpoint`、`BiomeTrigger` 补入关键引用缺失错误：`_requiredKey`、`_targetDoor`、`_data`、`_ambiencePreset` 缺失时直接报错，避免场景能摆但交互静默失效。
- 为上述交互/触发类对象补入通用 Trigger / LayerMask 校验，并允许对缺失 Trigger 与空 `_playerLayer` 做有限 Auto-Fix；同时为 `HiddenAreaMask` 增加“无 SpriteRenderer”警告，为 `ScheduledBehaviour` 增加“空相位数组 / 无目标也无子物体”警告。
- 新建 `LevelValidatorTests.cs`，先用测试固定 4 个高优先级失效模式：`Lock` 缺 key、`Checkpoint` 缺 `CheckpointSO`、`BiomeTrigger` 缺 `RoomAmbienceSO`、`ScheduledBehaviour` 空相位。
- 将 `LevelValidatorTests.cs` 纳入 `ProjectArk.Level.Editor.csproj`，并通过 `dotnet build ProjectArk.Level.Editor.csproj -nologo -clp:ErrorsOnly` 验证当前改动编译通过（35 warnings / 0 errors）。

### 目的
- 把上一轮文档里定义的“新增元素必须进入 Validator 护栏”真正落到代码上，先覆盖最容易导致主链交互失效的房间元素家族。
- 让 `Level` 模块新增互动件和触发器时，不再只有运行时自检日志，而是能在编辑期集中暴露缺引用、空 Layer、错误 Trigger 这类高频配置问题。

### 技术
- Editor 校验扩展：在 `LevelValidator` 中新增场景级 `FindObjectsByType(..., FindObjectsInactive.Include, ...)` 扫描，把非 `Room` 主链对象纳入统一校验入口。
- 通用规则抽取：复用 `ValidateTriggerCollider()` 与 `ValidateLayerMask()` 处理交互件共享的 Trigger / `_playerLayer` 约束，保持错误消息和 Auto-Fix 边界一致。
- 测试先行：先写 `LevelValidatorTests.cs` 固定目标行为，再补生产代码；当前命令行 `dotnet test` 输出链不稳定，但新增测试文件已随程序集成功编译。

---

## Level 新增房间元素接入协议收口 — 2026-04-01 22:51


### 修改文件
- `Docs/2_Design/Level/Level_CanonicalSpec.md`
- `Docs/2_Design/Level/Level_WorkflowSpec.md`

### 内容
- 在 `Level_CanonicalSpec.md` 中新增“新增房间元素接入协议（严格版）”，把新元素接入前必须回答的 6 个维度正式写成现役规范：模板家族、场景挂点、运行时 owner、状态通道、触发来源、Validator 契约。
- 在 `Level_CanonicalSpec.md` 中补入 6 类现役模板家族矩阵，明确 `Door / Gate`、`Interact Anchor`、`Persistent Room Element`、`Encounter Element`、`Hazard Element`、`Trigger / Director` 的默认挂点、authority、状态通道和 Validator 关注点。
- 在 `Level_CanonicalSpec.md` 中补入“新增元素的 LevelValidator 接入规则”，明确何时必须扩校验、严重度如何划分，以及 Auto-Fix 只能修结构默认值、不能猜语义字段。
- 在 `Level_WorkflowSpec.md` 中新增“新增元素的 Validator 接入规则”和“新增房间元素接入 SOP（严格版）”，把日常 authoring 的执行顺序收口为：选模板 → 定挂点 → 定 owner → 定状态通道 → 判断是否接 `Room` 主链 → 判断是否扩编辑期模型 → 补校验与验收。
- 在 `Level_WorkflowSpec.md` 中补入六类模板落位速查表，方便后续每次增加新互动件、新机关或新触发器时直接按模板接入，而不是重新考古整个 Level 模块。

### 目的
- 把“新增房间元素如何并入现有流程”从口头讨论收口为现役规范，降低后续增加互动件、机关、触发器时的接入分歧。
- 明确新元素接入时的 authority、持久化、Room 集成和 Validator 责任边界，避免再出现新元素能跑但没有统一接入路径、没有校验护栏的问题。

### 技术
- 文档架构设计：将协议层写入 `CanonicalSpec`，将执行 SOP 与校验入口写入 `WorkflowSpec`，保持“谁说了算”与“怎么落地”分层。
- 代码事实对齐：以 `Room.CollectSceneReferences()`、`RoomFlagRegistry`、`SaveBridge`、`Lock`、`Checkpoint`、`OpenEncounterTrigger`、`BiomeTrigger`、`ScheduledBehaviour`、`ActivationGroup` 的现役行为为依据组织模板。
- Validator 治理：将严重度、Auto-Fix 边界与“何时必须扩 `LevelValidator`”规则显式化，避免后续新增模板进入标准流程时缺少校验约束。

---

## Level 文档收口为现役规范版 — 2026-04-01 22:22


### 修改文件
- `Docs/2_Design/Level/Level_CanonicalSpec.md`
- `Docs/2_Design/Level/Level_WorkflowSpec.md`

### 内容
- 将 `Level_CanonicalSpec.md` 重写为现役规范文档，只保留架构总览、核心声明、房间语法、运行时 authority、模块边界、Editor 工具链 authority 与校验基线。
- 删除正文中的迁移策略、Batch 计划、版本记录、历史 Appendix，以及“已移除 / 已完成 / 待证实”这类阶段性描述，避免文档继续混合现役规则与历史过程。
- 将 `Level_WorkflowSpec.md` 收口为现役操作手册，只保留搭建入口、统一工作流、验证边界、标准 authoring 结构、`LevelScaffoldData` 定位与 `LevelSliceBuilder` JSON Schema。
- 删除 Workflow 中的历史兼容说明、过程性优化展望和版本降级语气，让文档直接回答“现在怎么搭、哪里是 authority、参数长什么样”。

### 目的
- 让 `Level` 文档体系只承载现役真相，降低阅读时的历史噪声和认知分叉。
- 把迁移、阶段、版本等过程信息统一收口到 `ImplementationLog`，让规范文档回到“声明当前规则”的职责。

### 技术
- 文档治理：按“架构 / 参数 / 声明 / authority”四类信息重组 `CanonicalSpec` 与 `WorkflowSpec`。
- 内容裁剪：去掉历史迁移、Batch 状态、版本追溯、兼容别名与未来优化段落，只保留现役 authoring 和 runtime 口径。
- 细节修正：同步修正文档中残留的历史措辞与 Markdown 反引号排版问题。

---

## 关卡 Batch 状态复核 + Level 工程文件收尾 — 2026-04-01 22:08

### 修改文件
- `Docs/2_Design/Level/Level_CanonicalSpec.md`
- `ProjectArk.Level.csproj`
- `ProjectArk.Level.Editor.csproj`

### 内容
- 复核 `Level_CanonicalSpec.md` 的 Batch 2 / 3 / 4 状态，按当前代码事实重写批次状态：`Batch 2`、`Batch 3` 收口为已完成，`Batch 4` 改为“工具侧已完成，场景验证待证实”。
- 在复核过程中实际运行 `dotnet build Project-Ark.slnx`，发现解决方案并未通过：`ProjectArk.Level.csproj` 与 `ProjectArk.Level.Editor.csproj` 仍残留对已删除 `WorldGraphSO` / `RoomNodeData` / `ConnectionEdge` / `RoomType` 以及旧 `WorldGraph` / `Scaffold` 编辑器脚本的源码引用。
- 清理 `ProjectArk.Level.csproj` 中 4 个失效源码条目，清理 `ProjectArk.Level.Editor.csproj` 中 9 个退役编辑器脚本条目，并补上现役 `LevelSliceBuilder.cs`。
- 再次执行 `dotnet build Project-Ark.slnx`，结果恢复为 **25 warnings / 0 errors**，确认当前文档里“编译通过”类验收项重新具备事实支撑。

### 目的
- 让 `Level_CanonicalSpec.md` 的批次状态只陈述“能被代码和构建结果直接证明”的事实，避免把工具完成误写成场景落地完成。
- 补完 WorldGraph 迁移后的工程文件收尾，消除“文档已经删干净，但解决方案还在引用旧文件”的隐性断链。

### 技术
- 代码锚点复核：对照 `RoomFactory`、`Room.cs`、`LevelValidator`、`PacingOverlayRenderer`、`RoomFlagRegistry`、`DestroyableObject`、`DoorTransitionController`、`SaveBridge`、`LevelEvents` 判定 Batch 状态。
- 构建验证：两轮 `dotnet build Project-Ark.slnx` 识别并回归验证失效引用问题。
- 文本编辑策略：同一文件改用串行替换，避免并行替换覆盖导致 `csproj` 和文档条目回滚。

---

## Implement_rules.md 同步更新（移除 WorldGraphSO 引用） — 2026-04-01 21:36

### 修改文件
- `Implement_rules.md`

### 内容
- 全面清理 Level 模块规则（§7）中的 11 处 WorldGraphSO 引用，与 2026-04-01 15:30 的代码删除和 Level_CanonicalSpec.md v1.1 更新保持一致。
- §7.1 模块边界：Data Assets 列表去掉 WorldGraphSO。
- §7.2 模块目标：改门连接的工作流改为直接改 Door 引用。
- §7.3 Authority 执行约束表：删除 WorldGraphSO 创建/编辑两行；SO 批量创建去掉 WorldGraphSO；Runtime 消费改为 Door 引用数据。
- §7.4.2：设计时离线数据从 WorldGraphSO 改为 RoomSO 等通用 SO。
- §7.4.4：RoomManager 错误条件从"找不到 WorldGraphSO"改为"找不到注册的 Room"。
- §7.4.5 Scene 接线白名单：去掉 WorldGraphSO 作为权威数据源的条目，改为 RoomSO + Door-based。
- §7.5.2 踩坑预防：从"WorldGraphSO ↔ Scene 不一致"改为"Door 连接引用不完整"。
- §7.6 验收清单：一致性校验改为 Door 连接完整性检查。
- §7.7 推荐工作流：新房间/新连接和难定位 bug 的流程全部改为 Door-based。
- 最终验证：文件内 `WorldGraphSO` / `WorldGraph` 搜索结果为 0，彻底清理完成。

### 目的
- 消除实现规则文档与代码实际状态之间的不一致，确保 `Implement_rules.md` 中的 Level 模块规则反映当前 Door-based 连接拓扑方案。
- 与 `Level_CanonicalSpec.md` v1.1 保持同步。

### 技术
- Markdown 文档逐处精确替换，共修改 11 处
- 修改后全文搜索验证零残留

---

## Level_CanonicalSpec.md 同步更新（移除 WorldGraphSO 引用） — 2026-04-01 21:28

### 修改文件
- `Docs/2_Design/Level/Level_CanonicalSpec.md`

### 内容
- 将文档从 v1.0 更新到 v1.1，全面移除已于 2026-04-01 15:30 删除的 WorldGraphSO、RoomNodeData、ConnectionEdge 相关内容。
- §1 文档目的：核心数据结构列表改为 GateID / ConnectionType / TransitionCeremony / RoomNodeType / RoomFlagRegistry。
- §4 五层模型：Layer 5 从"WorldGraphSO 显式房间网络"改为"Door-based 连接拓扑（Door 是连接关系的 Single Source of Truth）"。
- §5 数据结构定义：WorldGraphSO（§5.1）、RoomNodeData（§5.2）、ConnectionEdge（§5.4）的代码定义替换为"已移除"说明，保留移除原因和保留内容的记录。
- §5.6 GateID 命名规范：去掉 WorldGraphSO 端点标识引用，改为工具链连线和 Pacing 可视化用途。
- §5.7 Door 升级字段：Header 从 "World Graph Integration" 改为 "Gate & Connection"，与当前代码一致。
- §7 运行时数据流：数据源从 WorldGraphSO 改为 Door._targetRoom 直接引用；MinimapManager 路径改为固定走 GatherConnectionsFromDoors。
- §9 Editor 工具链：WorldGraphEditor 标记为已移除；LevelValidator 描述改为 Door 连接完整性校验。
- §10 迁移策略：6 项任务全部标注完成/移除状态；MinimapManager 迁移标记为已完成。
- §11 Batch 1：标记为已完成，WorldGraphSO 相关任务标记为已移除，验收标准更新为实际通过项。
- §12 校验规则：去掉 WorldGraphSO 一致性校验条目，改为 Door-based 检查（GateID 重复、引用非空）。

### 目的
- 消除设计文档与代码实际状态之间的不一致，避免后续开发者按照已删除的 WorldGraphSO 架构搭建关卡。
- 让 Level_CanonicalSpec.md 重新成为可信赖的"唯一目标架构规范"。

### 技术
- Markdown 文档增量更新，逐章节精确替换
- 基于 ImplementationLog 中 "移除 WorldGraphSO — 2026-04-01 15:30" 记录的事实进行同步

---

## 关卡工作流权威文档 — 2026-04-01 16:10

### 新建文件
- `Docs/2_Design/Level/Level_WorkflowSpec.md` — 关卡工作流权威规范文档

### 内容
全面扫描关卡模块所有文件（脚本 72 个、Editor 工具 11 个、SO 资产 97 个、文档 14 个），构建了完整的权威工作流文档。

文档涵盖：
- **两条搭建路径**的完整步骤（HTML 设计器导入 + SceneView 手绘）
- **6 大核心枚举**的完整定义和语义说明（RoomNodeType 10 种 / ConnectionType 6 种 / TransitionCeremony 5 种 / DoorState 5 种 / EncounterMode 2 种）
- **10 种 ScriptableObject 数据类型**的字段表（RoomSO / EncounterSO / CheckpointSO / KeyItemSO / WorldPhaseSO / WorldProgressStageSO / RoomVariantSO / RoomAmbienceSO / RoomPresetSO / LevelScaffoldData）
- **Room 标准层级结构**（Tilemaps / Elements / Encounters / Decoration / Triggers / ActivationGroups）
- **Editor 工具链 + Runtime 脚本**完整清单（61 个运行时 + 11 个编辑器）
- **已有数据资产目录**（12 个早期 Sheba 房间 + 38 个 JSON 导入房间 + 17 个 Encounter + 19 个 Checkpoint + 2 个 Key + 4 个 WorldPhase + 2 个 WorldStage）
- **JSON 导入格式规范**（type/connectionType 字段映射表）
- **已知限制与待办**清单

### 目的
建立关卡模块的单一真相源文档，让任何参与者都能快速理解完整工作流和已有资产，减少考古成本。

### 技术
文档编写，基于代码扫描和文件结构分析，无代码变更。

---

## LevelDesigner.html 对齐 Unity 关卡模块 — 2026-04-01 15:45

### 修改文件
- `Assets/Scripts/Level/Editor/LevelArchitect/LevelSliceBuilder.cs` — 补全 `ParseNodeType()` 和 `ParseConnectionType()` 的映射；更新 `RoomJson`/`ConnectionJson` 注释
- `Tools/LevelDesigner.html` — 房间类型、连接类型、CSS 样式、颜色配置、标签函数全面对齐 Unity

### 内容

**原则确立：Unity 关卡模块代码是唯一权威来源，HTML 工具是消费者。**

**LevelSliceBuilder.cs — ParseNodeType() 补全：**
- `"arena"` → `Resolution`（最关键缺口：修复后导入 arena 房间时会自动创建 ArenaController + EncounterSO）
- `"normal"` → `Pressure`
- `"narrative"` → `Anchor`
- `"puzzle"` → `Reward`
- `"corridor"` → `Transit`（显式声明，原为默认兜底）
- 新增全部 10 种 RoomNodeType 的规范字符串映射（transit/pressure/resolution/reward/anchor/loop/hub/threshold/safe/boss）

**LevelSliceBuilder.cs — ParseConnectionType() 补全：**
- `"tidal"` → `Scheduled`（潮汐门 = 时间控制门）
- `"locked"` → `Ability`（钥匙锁门 = 银河城能力门）
- `"one_way"` → `Progression`（单向通道 = 推进连接）
- 保留 `"secret"` → `Ability` 兼容映射
- 新增全部 6 种 ConnectionType 的规范字符串映射

**LevelDesigner.html 同步更新：**
- 左侧拖拽预设面板：7 种旧类型 → 10 种 RoomNodeType 正规类型
- 属性面板类型下拉框：同步替换
- 连接类型下拉框：4 种旧类型 → 6 种 ConnectionType 正规类型（value 值用规范字符串）
- CONN_TYPE_CONFIG 颜色配置：对齐新连接类型
- SVG arrow markers：新增 gray/purple/green 三色
- getTypeLabel()：对齐新房间类型标签
- CSS 房间颜色类：新增 10 种规范类型，旧 5 种类型保留为向后兼容 alias

### 目的
消除 HTML 设计器导入工作流中的类型映射缺口，确保从 HTML 设计 → JSON 导出 → Unity 场景生成的完整流程中，类型语义不丢失。

### 技术
`ParseNodeType`/`ParseConnectionType` 使用 C# switch expression 扩展映射表；HTML 旧类型 CSS 保留为 legacy alias，旧 JSON 文件向后兼容。编译零 error。

---

## 移除 WorldGraphSO — 2026-04-01 15:30

### 删除文件
- `Assets/Scripts/Level/Data/WorldGraphSO.cs` — 世界图谱 SO 核心类
- `Assets/Scripts/Level/Data/RoomNodeData.cs` — 只服务于 WorldGraphSO 的房间节点数据结构
- `Assets/Scripts/Level/Data/ConnectionEdge.cs` — 只服务于 WorldGraphSO 的连接边数据结构

### 修改文件
- `Assets/Scripts/Level/Room/DoorTransitionController.cs` — 删除 `_worldGraph` 字段、`ResolveTarget()` 方法、`FindSpawnPointForGate()` 方法；目标解析改为直接读取 `Door._targetRoom` / `Door._targetSpawnPoint`
- `Assets/Scripts/Level/Map/MinimapManager.cs` — 删除 `_worldGraph` 字段、`GatherConnectionsFromWorldGraph()` 方法；连接关系构建固定走 `GatherConnectionsFromDoors()`
- `Assets/Scripts/Level/Editor/LevelArchitect/LevelValidator.cs` — 删除规则 9-11（WorldGraph 一致性校验：RoomID 存在性、GateID 匹配、孤立节点）及所有相关方法
- `Assets/Scripts/Level/Room/Door.cs` — Header 从 "World Graph Integration" 改为 "Gate & Connection"，属性注释去掉 WorldGraphSO 引用
- `Assets/Scripts/Level/Room/RoomManager.cs` — `FindRoomByID()` 注释去掉 WorldGraphSO 引用
- `ProjectArk.Level.csproj` — 删除三个已删除文件的 `<Compile>` 条目

### 内容
决策背景：WorldGraphSO 是一个离线拓扑快照，运行时 DoorTransitionController 和 MinimapManager 本来就在走 fallback 路径（直接读 Door 引用），WorldGraphSO 仅被 LevelValidator 的规则 9-11 消费。现阶段关卡搭建不需要手动维护此资产，删除可减少认知负担并简化工作流。

保留内容：`GateID`（Door 字段，工具链写入）、`ConnectionType`（PacingOverlay 可视化）、`TransitionCeremony`（DoorTransitionController 分级演出）——这三者均独立于 WorldGraphSO。

### 目的
简化关卡工作流，消除"规划了但未落地"的半成品接口，让搭建场景时无需考虑 WorldGraphSO 资产维护。

### 技术
纯删除，零架构变更；DoorTransitionController/MinimapManager 运行时行为完全不变（删的是从未生效的 WorldGraph 路径）。编译零 error。

---

## 关卡模块 Batch 4：节奏验证工具 + 示巴星首切片落地（验证层） — 2026-03-24 18:30

### 新建文件
- `Assets/Scripts/Level/Editor/LevelArchitect/ShebaSliceBuilder.cs` — 一键构建示巴星 7 房间首切片的 Editor 自动化脚本

### 修改文件
- `Assets/Scripts/Level/Editor/LevelArchitect/PacingOverlayRenderer.cs` — 新增 `DrawConnectionTypeOverlay()` 方法，渲染 ConnectionType 语义色连接线 + 类型标签 + 右上角颜色图例
- `Assets/Scripts/Level/Editor/LevelArchitect/LevelArchitectWindow.cs` — 新增 `ShowConnectionTypes` toggle 开关
- `Assets/Scripts/Level/Editor/LevelArchitect/RoomBlockoutRenderer.cs` — ConnectionType overlay 激活时跳过默认灰色连接线，避免视觉冲突

### 内容

**B4.1 PacingOverlayRenderer ConnectionType 可视化**
- 直接从场景 Door MonoBehaviour 读取 `_connectionType` 字段（而非查询 WorldGraphSO），Door 作为连接类型的 single source of truth
- 颜色映射复用 `ConnectionGraphEdge.GetConnectionTypeColor()`：Progression=浅灰, Return=紫色, Ability=青色, Challenge=红色, Identity=金色, Scheduled=绿色
- 普通连接画虚线，层间过渡画粗实线
- 连接中点显示简称标签（PROG/RET/ABL/CHAL/IDENT/SCHED）
- SceneView 右上角渲染颜色图例面板
- `RoomBlockoutRenderer.DrawDoorConnections()` 在 ConnectionType overlay 激活时 early return，避免灰色线与语义色线叠加

**B4.2 ShebaSliceBuilder 一键构建脚本**
- 菜单入口：`ProjectArk > Level > Build Sheba 7-Room Slice`
- 自动创建 7 个 RoomSO 资产（`Assets/_Data/Level/Rooms/Sheba/SH-R{01-07}_Data.asset`），全部使用 `_useLegacyTypeMapping = false` 显式指定 NodeType
- 自动创建 1 个 EncounterSO 占位资产（`SH-R04_Encounter`，Closed 模式，空 waves）
- 在场景中创建 7 个 Room GameObject，遵循 RoomFactory 标准层级（Navigation/Elements/Encounters/Hazards/Decoration/Triggers + CameraConfiner）
- 自动创建双向 Door 对 + SpawnPoint，共 7 条连接（14 扇门）：
  - SH-R01→R02 (Progression), R02→R03 (Progression), R03→R04 (Challenge)
  - R04→R05 (Progression), R05→R06 (Progression), R06→R01 (Return/loop)
  - R06→R07 (Progression)
- 支持 Undo 一键撤销，已有同名资产自动覆盖
- Resolution 房间（SH-R04）自带 ArenaController + 4 个 SpawnPoints

### 目的
- B4.1：为 Level Architect 工具增加 ConnectionType 语义色可视化，让关卡设计师在 SceneView 中一眼看到连接关系的节奏含义
- B4.2：用自动化脚本一键搭建 Sheba 首切片的全部关卡基础设施，为 Play Mode 闭环验证做准备

### 技术
- SerializedObject API 设置所有 MonoBehaviour 和 SO 字段（与 RoomFactory 模式一致）
- 两遍式 Door 接线：第一遍创建所有 Door + SpawnPoint，第二遍交叉引用 `_targetRoom` 和 `_targetSpawnPoint`
- Undo 分组 + try/catch 保护，失败时自动回滚

---

## 关卡模块 Batch 3：房间级持久状态 + 互动件标准化（状态层） — 2026-03-24 15:00

### 新建文件
- `Assets/Scripts/Level/Room/RoomFlagRegistry.cs` — 房间级持久 Flag 系统（ServiceLocator 注册 + SaveBridge 集成）
- `Assets/Scripts/Level/Room/DestroyableObject.cs` — 可破坏环境物件基类（IDamageable + RoomFlagRegistry 死亡联动）

### 修改文件
- `Assets/Scripts/Core/LevelEvents.cs` — 新增 `OnRoomFlagChanged(string roomID, string flagKey, bool newValue)` 静态事件
- `Assets/Scripts/Level/SaveBridge.cs` — Collect/Distribute 流程中接入 RoomFlagRegistry 的 WriteToSaveData / ReadFromSaveData
- `Assets/Scripts/Level/Room/Room.cs` — Awake 自动收集 DestroyableObject（从 Elements/ 优先根），暴露 Destroyables 属性
- `Assets/Scripts/Level/Room/DoorTransitionController.cs` — Boss/Heavy 门独立演出分支（拆分自共用的 PlayLayerTransitionEffects）

### 内容

**B3.1 RoomFlagRegistry**
- 嵌套字典 `roomID → { flagKey → bool }` 内存结构
- `SetFlag()` 自动去重 + 广播 `LevelEvents.OnRoomFlagChanged`
- `WriteToSaveData()` / `ReadFromSaveData()` 复用 `ProgressSaveData.Flags`（`List<SaveFlag>`），key 格式 `room_{roomID}_{flagKey}`
- 接口约定完全对齐 `KeyInventory` 的 WriteToSaveData / ReadFromSaveData 模式

**B3.2 DestroyableObject**
- 实现 `IDamageable`，支持 `DamagePayload` 伤害管线
- 被打碎后通过 `RoomFlagRegistry.SetFlag()` 持久化
- `Start()` 中从 Registry 检查是否已被破坏，自动恢复已破坏视觉状态
- 支持自定义破坏 Sprite、VFX、SFX；无 Sprite 时 fallback 为关闭 Collider + 隐藏 SpriteRenderer
- Room.Awake 从 `Elements/` 子节点自动收集 DestroyableObject 数组

**B3.3 Boss/Heavy 门独立演出**
- `PlayBossTransitionEffects()`：Boss 专属 SFX（fallback 到 Layer SFX）、更大缩放（3 单位 vs Layer 的 2）、PrimeTween 震屏
- `PlayHeavyTransitionEffects()`：两阶段演出（Phase 1 轻震 + 半缩放 → Phase 2 强震 + 全缩放）、专属 SFX（三级 fallback 链）
- `ApplyCameraShake()` 使用 PrimeTween `ShakeLocalPosition` 实现
- `ExecuteTransition()` 内的 ceremony 分支从旧的 `isLayerish` 布尔判断改为 `switch(ceremony)` 精确路由

**B3.4 LevelEvents.OnRoomFlagChanged**
- 签名：`Action<string, string, bool>`（roomID, flagKey, newValue）
- 对应 Raise 方法：`RaiseRoomFlagChanged()`
- XML doc 标注 Publisher（RoomFlagRegistry）和 Consumer（DestroyableObject / Door / UI / Save）

### 目的
- 实现 Level_CanonicalSpec.md Batch 3 的全部 4 项任务（B3.1 - B3.4）
- 让房间内的互动件能写入持久状态，形成"空间被改写"的体验
- Boss/Heavy 门有了区别于普通门的独立视觉演出，强化仪式感分级
- 为 Batch 4 的示巴星切片落地准备了"打碎后不复原"的持久化基础

### 技术
- ServiceLocator 注册/注销模式（RoomFlagRegistry）
- SaveBridge Collect/Distribute 模式（复用 ProgressSaveData.Flags + SaveFlag）
- IDamageable 接口实现 + DamagePayload 伤害管线
- LevelEvents 静态事件总线模式
- PrimeTween ShakeLocalPosition 震屏
- Room.CollectComponentsFromPreferredRoot 子节点优先收集模式
- 编译验证：`dotnet build Project-Ark.slnx`，0 error / 0 新增 warning

---

## 关卡模块 Batch 2：Level Architect 迁移入口修补 — 2026-03-23 17:18

### 修改文件
- `Assets/Scripts/Level/Editor/LevelArchitect/LevelArchitectWindow.cs` — 将 `Migrate Scene NodeTypes` 入口真正挂回 `Level Architect` 主窗口 `Actions` 区与侧栏 `Validate` 区

### 内容
- 修复了“整场景 NodeType 迁移方法已存在，但主窗口按钮缺失”的 UI 暴露问题。
- 现在在 `Level Architect` 顶部 `Actions` 区可以直接点击 `Migrate Scene NodeTypes`，执行整场景 `legacy → explicit` 迁移。
- SceneView 侧栏底部的验证区也同步增加了同名按钮，避免入口只藏在多选房间的批量编辑面板里。
- 迁移执行后会立即跑一次 `Validate All` 并刷新窗口，方便当场确认还有哪些房间残留 legacy 映射告警。
- `dotnet build Project-Ark.slnx` 编译通过，0 warning / 0 error。

### 目的
- 把已经实现的整场景迁移能力真正变成可见、可点、可验证的编辑器工作流，而不是只停留在代码方法和隐蔽入口里。
- 降低旧房间清理的使用门槛，避免使用者误以为迁移按钮不存在或功能未完成。

### 技术
- EditorWindow UI 补口：在 `LevelArchitectWindow` 两个验证入口区挂载同一个整场景迁移调用。
- 迁移后即时校验：按钮点击后串联 `BatchEditPanel.MigrateSceneRoomsToExplicitNodeType()` 与 `LevelValidator.ValidateAll()`，形成更短的验证回路。
- 编译验证：`dotnet build Project-Ark.slnx`。

## 关卡模块 Batch 2：RoomNodeType 迁移工具化 — 2026-03-23 13:27

### 修改文件
- `Assets/Scripts/Level/Data/RoomSO.cs` — 暴露 `ExplicitNodeType` / `LegacyMappedNodeType` 只读属性，便于编辑器区分 authored 值与 legacy 映射值
- `Assets/Scripts/Level/Editor/LevelArchitect/BatchEditPanel.cs` — 新增批量 `Apply Node`、`Migrate Legacy→Explicit`、单房间/整场景显式化迁移入口，并让 `Apply Type` 同步写入显式 `NodeType`
- `Assets/Scripts/Level/Editor/LevelArchitect/LevelArchitectWindow.cs` — 主窗口新增 `Migrate Scene NodeTypes` 快捷入口，单房间面板显示 `NodeType` 来源并支持一键显式化
- `Assets/Scripts/Level/Editor/LevelArchitect/LevelValidator.cs` — 新增“房间仍在使用 legacy RoomType→NodeType 映射”告警与 AutoFix

### 内容
- 把 `RoomNodeType` 的迁移从“代码层兜底”推进到“编辑器可操作”：现在可以对所选房间、单个房间、甚至整场景房间执行 `legacy → explicit` 显式化迁移。
- `BatchEditPanel` 现在同时支持批量设置 `RoomType` 和 `NodeType`，并在应用 `RoomType` 时同步写入显式 `NodeType`，避免工具继续制造半迁移数据。
- `LevelArchitectWindow` 的单房间视图会直接显示 `Node Source / Explicit Node / Mapped Node`，让迁移状态不再是隐藏信息。
- `LevelValidator` 会把仍依赖 legacy 映射的房间列为 Warning，并提供一键修复，方便集中清理旧房间资产。
- `dotnet build Project-Ark.slnx` 编译通过，0 error（保留项目既有 warning）。

### 目的
- 让 Batch 2 不只是“新房间语法已标准化”，还具备把旧房间逐步迁移到显式 `RoomNodeType` 的低成本路径。
- 减少后续做节奏语义微调时对 legacy `RoomType` 隐式映射的依赖，避免可视化、校验、authoring 三层出现语义漂移。
- 为后续 Batch 3/4 的房间状态与节奏验证工作先清掉一层数据债。

### 技术
- 渐进式数据迁移：保留 legacy 运行时类型，同时提供显式 `NodeType` 批量落盘入口。
- 编辑器闭环：`BatchEditPanel` / `LevelArchitectWindow` / `LevelValidator` 三处同时提供迁移与校验入口。
- 数据可观测性：将 `NodeType` 的来源状态显式暴露在编辑器 UI 中，降低“看起来一样、其实来源不同”的排查成本。
- 编译验证：`dotnet build Project-Ark.slnx`。

## 关卡模块 Batch 2：房间语法标准化（骨架接入） — 2026-03-23 12:15

### 修改文件
- `Assets/Scripts/Level/Data/RoomSO.cs` — 新增 `RoomNodeType`、legacy→node 映射兜底与迁移字段
- `Assets/Scripts/Level/Room/Room.cs` — `Awake()` 优先按标准层级收集 Door / Encounter / SpawnPoint，并保留旧结构 fallback
- `Assets/Scripts/Level/Editor/LevelArchitect/RoomFactory.cs` — 新建房间时自动生成 `Navigation/Elements/Encounters/Hazards/Decoration/Triggers` 六层骨架
- `Assets/Scripts/Level/Editor/LevelArchitect/DoorWiringService.cs` — 自动创建的 Door / SpawnPoint 优先挂到 `Navigation` 标准层级
- `Assets/Scripts/Level/Editor/LevelArchitect/LevelValidator.cs` — 新增标准房间层级校验与一键补全
- `Assets/Scripts/Level/Editor/LevelArchitect/PacingOverlayRenderer.cs` — 用 `RoomNodeType` 颜色显示房间节奏语义，并保留战斗强度标签
- `Assets/Scripts/Level/Editor/LevelArchitect/RoomBlockoutRenderer.cs` — SceneView 房间块、标签、Tooltip 切到 `RoomNodeType` 语义展示
- `Assets/Scripts/Level/Editor/LevelArchitect/LevelArchitectWindow.cs` — 房间列表和单房间面板接入 `RoomNodeType` 可视化信息

### 内容
- 为 `RoomSO` 增加新的 `RoomNodeType` authored 数据入口，同时保留旧 `RoomType` 到新语义的映射兜底，避免现有房间资产在迁移期被静默改写。
- 将 `Room.Awake()` 升级为“优先走标准子节点路径，找不到再回退旧结构”的双路径收集逻辑，兼容当前场景与后续规范化房间。
- 让 `RoomFactory`、`DoorWiringService`、`LevelValidator` 三条编辑器链路统一对齐到标准层级，保证“新建 / 自动连门 / 自动修复”不会把数据重新写回旧结构。
- 将 SceneView 与 Level Architect 面板中的主要节奏可视化统一到 `RoomNodeType`，让 `WorldGraph` 语义真正进入日常关卡编辑视图。
- `dotnet build Project-Ark.slnx` 编译通过，0 error。

### 目的
- 正式落地 `Level_CanonicalSpec.md` Batch 2 的骨架层目标：让房间具备固定语法、工具链围绕同一套层级说话。
- 在不破坏现有 Arena/Boss/Safe 运行时逻辑的前提下，把 authoring 语义从旧 `RoomType` 平滑迁移到新 `RoomNodeType`。
- 缩短后续 Batch 2/3 的改动路径，避免新工具继续往房间根节点随意落对象，导致层级再次发散。

### 技术
- 渐进式迁移：`RoomSO` 同时保留 legacy `RoomType` 与新 `RoomNodeType`，并提供映射兜底。
- 标准层级治理：`Navigation/Elements/Encounters/Hazards/Decoration/Triggers` 六层骨架 + 编辑器 AutoFix。
- 编辑器一致性收口：`RoomFactory` / `DoorWiringService` / `LevelValidator` / `PacingOverlayRenderer` / `RoomBlockoutRenderer` / `LevelArchitectWindow` 全链路统一语义。
- 编译验证：`dotnet build Project-Ark.slnx`。

---

## WorldGraphEditor 画布平移操纵器 (SpacePanManipulator) — 2026-03-23 23:30

### 新建文件
- `Assets/Scripts/Level/Editor/LevelArchitect/SpacePanManipulator.cs`

### 修改文件
- `Assets/Scripts/Level/Editor/LevelArchitect/WorldGraphView.cs` — 注册 SpacePanManipulator

### 内容
为 WorldGraphEditor 添加 Figma 风格的画布平移操作：按住空格键 + 鼠标左键拖拽可自由移动画布。同时支持鼠标中键拖拽平移（无需按空格）。

### 目的
原始 GraphView 的 `ContentDragger` 仅支持 Alt+LMB 或 MMB 拖拽平移，不符合 Figma/Photoshop 用户习惯。添加 Space+LMB 的交互模式提升编辑体验。

### 技术
- 继承 `Manipulator`，使用 `TrickleDown` 模式注册键盘和鼠标事件确保优先级
- 通过 `GraphView.viewTransform.position` 增量修改画布位置（GraphView 标准 API）
- 用 `CaptureMouse()` / `ReleaseMouse()` 管理鼠标捕获状态
- `#pragma warning disable CS0618` 消除 Unity 尚未迁移的 `ITransform.position` 过时警告

---

## WorldGraphEditor 节点图编辑器 + ScaffoldToWorldGraphBuilder — 2026-03-23 21:30

**新建文件：**
- `Assets/Scripts/Level/Editor/LevelArchitect/WorldGraphEditorWindow.cs` — 完整版 GraphView 节点图编辑器宿主窗口
- `Assets/Scripts/Level/Editor/LevelArchitect/WorldGraphView.cs` — GraphView 子类，渲染和编辑 WorldGraphSO
- `Assets/Scripts/Level/Editor/LevelArchitect/RoomGraphNode.cs` — GraphView 节点元素（房间节点，按 RoomNodeType 着色）
- `Assets/Scripts/Level/Editor/LevelArchitect/ConnectionGraphEdge.cs` — GraphView 连线元素（按 ConnectionType 着色）
- `Assets/Scripts/Level/Editor/LevelArchitect/WorldGraphSOEditor.cs` — WorldGraphSO 的 Custom Inspector（含 "Open in Graph Editor" 按钮）
- `Assets/Scripts/Level/Editor/LevelArchitect/ScaffoldToWorldGraphBuilder.cs` — Authority Builder，从 LevelScaffoldData 自动生成 WorldGraphSO

**修改文件：**
- `Assets/Scripts/Level/Data/RoomNodeData.cs` — 添加 `EditorPosition (Vector2)` 字段供 GraphView 存储节点布局坐标

**内容：**
实现了 Batch 1 的两个核心工具：
1. **WorldGraphEditor**：基于 Unity `GraphView` API 的完整节点图编辑器。支持：
   - 可拖拽房间节点 + 可视化连线，类似 Shader Graph 交互体验
   - 按 RoomNodeType 着色节点（10 种颜色映射），按 ConnectionType 着色连线（6 种）
   - 右键创建新节点（支持全部 10 种 RoomNodeType）
   - 从端口拖拽创建新连接
   - 删除节点/连接
   - Inspector 侧面板编辑选中节点的详细属性（NodeType、DesignerNote、连接列表）
   - 力导向自动布局算法（50 次迭代，含斥力/引力/阻尼）
   - 底部图例条显示颜色编码含义
   - 自动保存（关闭窗口时持久化回 WorldGraphSO）
   - 菜单路径：`ProjectArk/Level/Authority/World Graph Editor`

2. **ScaffoldToWorldGraphBuilder**：Authority Builder，从 LevelScaffoldData 一键生成 WorldGraphSO：
   - RoomType → RoomNodeType 映射表（优先使用 DisplayName 关键字精确匹配，fallback 到枚举映射）
   - ScaffoldDoorConnection → 双向 ConnectionEdge 自动生成
   - GateID 从门方向 + 目标房间自动推导
   - ConnectionType 从 DoorState / 目标房间类型自动推断
   - EditorPosition 从 ScaffoldRoom.Position 缩放推导
   - 菜单路径：`ProjectArk/Level/Authority/Build WorldGraph from Scaffold`

**目的：** 闭环 Batch 1 验收标准中的 B1.6（WorldGraphEditor）和 B1.7（示巴星 WorldGraphSO 资产生成）。

**技术：** Unity GraphView API (`UnityEditor.Experimental.GraphView`)、SerializedObject API、力导向布局算法、策略模式映射表。

---

## 删除一次性 LevelAssetCreator — 2026-03-23

**删除文件：**
- `Assets/Scripts/Level/Editor/LevelAssetCreator.cs` + `.meta`

**内容：**
- 该文件包含 4 个硬编码的 Phase 2 SO 资产创建菜单（Create Phase 2 Assets / Checkpoint / Key / WorldStage），资产早已创建完毕
- 一次性脚本不应留在 Authority 菜单中，清理后 Authority 菜单只保留真正的长期工具

**目的：**
- 确保 `ProjectArk/Level/Authority/` 菜单下只有 CanonicalSpec §9.1 定义的正式 Authority Builder，不含过期的一次性脚本

**技术：**
- 删除 `.cs` + `.meta`，清理 `.csproj` 缓存引用，编译验证通过（0 error）

---

## Level Editor 菜单路径统一到 Authority 前缀 — 2026-03-23

**修改文件：**
- `Assets/Scripts/Level/Editor/LevelArchitect/LevelArchitectWindow.cs` — 菜单从 `Window/ProjectArk/Level Architect` → `ProjectArk/Level/Authority/Level Architect`
- `Assets/Scripts/Level/Editor/ScaffoldToSceneGenerator.cs` — 菜单从 `Window/ProjectArk/Generate Level From Scaffold` → `ProjectArk/Level/Authority/Generate Level From Scaffold`
- `Assets/Scripts/Level/Editor/LevelAssetCreator.cs` — 4 个菜单从 `ProjectArk/Level/...` → `ProjectArk/Level/Authority/...`

**目的：**
- 与 Ship/VFX 模块统一菜单命名约定（`ProjectArk/{Module}/Authority/...`）
- 所有 CanonicalSpec §9.1 定义的官方 builder 在 Unity 菜单中集中到同一个 `Authority` 子菜单下，一目了然

**技术：**
- 只修改 `[MenuItem]` 字符串和 `MENU_PATH` 常量，无逻辑变更
- 编译验证通过（0 error）

---

## Level Editor 工具链 Authority 标记 + 旧工具清理 — 2026-03-22

**修改文件：**
- `Assets/Scripts/Level/Editor/LevelArchitect/LevelArchitectWindow.cs` — 加 `[Authority]` 标记 + 删除 Legacy Tool Detection 代码段
- `Assets/Scripts/Level/Editor/LevelArchitect/RoomFactory.cs` — 加 `[Authority]` 标记
- `Assets/Scripts/Level/Editor/LevelArchitect/DoorWiringService.cs` — 加 `[Authority]` 标记
- `Assets/Scripts/Level/Editor/LevelArchitect/LevelValidator.cs` — 加 `[Authority]` 标记
- `Assets/Scripts/Level/Editor/LevelArchitect/PacingOverlayRenderer.cs` — 加 `[Authority]` 标记
- `Assets/Scripts/Level/Editor/LevelArchitect/RoomBlockoutRenderer.cs` — 加 `[Authority]` 标记
- `Assets/Scripts/Level/Editor/LevelArchitect/BatchEditPanel.cs` — 加 `[Authority]` 标记
- `Assets/Scripts/Level/Editor/LevelArchitect/BlockoutModeHandler.cs` — 加 `[Authority]` 标记
- `Assets/Scripts/Level/Editor/LevelArchitect/ScaffoldSceneBinder.cs` — 加 `[Authority]` 标记
- `Assets/Scripts/Level/Editor/LevelArchitect/SceneScanner.cs` — 加 `[Authority]` 标记
- `Assets/Scripts/Level/Editor/ScaffoldToSceneGenerator.cs` — 加 `[Authority]` 标记（one-shot tool）
- `Assets/Scripts/Level/Editor/LevelAssetCreator.cs` — 加 `[Authority]` 标记
- `Implement_rules.md` — §7.3 追加已删除工具清单 + 代码层标记说明

**删除文件：**
- `Assets/Scripts/Level/Editor/LevelDesignerWindow.cs`（`[Obsolete]`，被 LevelArchitectWindow 替代）
- `Assets/Scripts/Level/Editor/RoomBatchEditor.cs`（`[Obsolete]`，被 LevelArchitectWindow 替代）
- `Assets/Scripts/Level/Editor/ShebaLevelScaffolder.cs`（`[Obsolete]`，被 LevelArchitectWindow 替代）
- `Assets/Scripts/Level/Editor/LevelGenerator.cs`（旧 scaffold 生成器，被 ScaffoldToSceneGenerator 替代）
- `Assets/Scripts/Level/Editor/HtmlScaffoldImporter.cs`（一次性 HTML 导入工具，已完成使命）
- `Assets/Scripts/Level/Editor/Phase6AssetCreator.cs`（Phase 6 一次性资产生成，已完成使命）
- `Assets/Scripts/Level/Editor/MapUIBuilder.cs`（Map UI 一次性构建工具，已完成使命）
- `Assets/Scripts/Level/Data/LevelElementLibrary.cs`（数据类，仅被已删除的工具引用）

**目的：**
- 给 CanonicalSpec §9.1 定义的所有官方 builder 在代码层加 `[Authority: Level CanonicalSpec §9.1]` 标记，使 authority 可 grep 追溯
- 删除所有已废弃（`[Obsolete]`）和已完成使命的一次性工具，减少菜单项混乱和维护负担
- 清理 LevelArchitectWindow 中检测已删除旧工具的 Legacy Tool Detection 代码段

**技术：**
- `[Authority]` 标记写入 XML doc `<summary>` 中，不影响编译，可通过 `grep -r "Authority.*CanonicalSpec"` 检索
- 编译验证通过（0 error）

---

## Level 模块 Authority 执行约束表 — 2026-03-22

**修改文件：**
- `Implement_rules.md` — 新增 Section 7 "Level 模块规则"（含 Authority 执行约束表），更新文档头部已启用模块列表，原 Section 8 通用模板调整为 Section 9

**内容：**
- 在 Implement_rules.md 中追加 Level 模块的轻量预防式 Authority 骨架
- 包含：模块边界（§7.1）、模块目标（§7.2）、Authority 执行约束表（§7.3，10 行工具→对象映射）、实现规则（§7.4，含迁移纪律/Runtime 数据隔离/工具执行模式/禁止 Silent No-Op/Scene 接线白名单）、预防性踩坑总结（§7.5）、验收清单（§7.6）、推荐工作流（§7.7）

**目的：**
- 在 Level 重构阶段开始前事前设置 authority，防止重蹈 VFX 模块"事后救火"的治理困境
- 与 VFX 的重度 Authority Matrix 不同，Level 采用轻量形式——工具职责天然分离，重点约束迁移纪律和 Scene 接线治理

**技术：**
- 遵循 Implement_rules.md §8（现 §9）的通用模块规则模板结构
- Authority 执行约束表严格对齐 Level_CanonicalSpec.md §9 工具矩阵，只做执行约束层补充
- 预留增量追加机制：后续 Batch 遇到实际问题时按 §7.5 追加踩坑总结

---

## 关卡模块 Batch 1：世界图谱 + 门语义升级（骨架层） — 2026-03-22

**新建文件：**
- `Assets/Scripts/Level/Data/RoomNodeType.cs` — 10 种节奏节点类型枚举
- `Assets/Scripts/Level/Data/ConnectionType.cs` — 6 种连接语义枚举
- `Assets/Scripts/Level/Data/TransitionCeremony.cs` — 5 级过渡仪式感枚举
- `Assets/Scripts/Level/Data/RoomNodeData.cs` — 世界图谱中的房间节点数据结构
- `Assets/Scripts/Level/Data/ConnectionEdge.cs` — 世界图谱中的连接边数据结构
- `Assets/Scripts/Level/Data/WorldGraphSO.cs` — 显式世界图谱 ScriptableObject（关卡拓扑单一真相源）

**修改文件：**
- `Assets/Scripts/Level/Room/Door.cs` — 新增 `_gateID`、`_connectionType`、`_ceremony` 字段和 `EffectiveCeremony` 属性（兼容旧 `_isLayerTransition`）
- `Assets/Scripts/Level/Room/DoorTransitionController.cs` — 新增 `_worldGraph` 引用、Ceremony-based timing、`ResolveTarget` 方法（优先 WorldGraphSO + GateID 定位，fallback 到 `_targetRoom`）
- `Assets/Scripts/Level/Room/RoomManager.cs` — 新增 `_roomLookup` 字典和 `FindRoomByID()` 公开方法
- `Assets/Scripts/Level/Map/MinimapManager.cs` — 新增 `_worldGraph` 引用，`GatherSceneData` 拆为 `GatherConnectionsFromWorldGraph`（新路径）和 `GatherConnectionsFromDoors`（旧 fallback）
- `Assets/Scripts/Level/Editor/LevelArchitect/LevelValidator.cs` — 新增 3 条 WorldGraphSO 校验规则（Rule 9: RoomID 场景缺失、Rule 10: GateID 不匹配、Rule 11: 孤立房间）

**内容：**
- 实现 Level_CanonicalSpec.md 中 Batch 1 的全部 7 项任务（B1.1 - B1.7，B1.7 的资产创建留待 Unity Editor 内操作）
- WorldGraphSO 包含运行时字典缓存（O(1) 查找）、GateID 连接查找、邻接查询和校验辅助方法
- DoorTransitionController 的目标解析支持双路径：WorldGraphSO + GateID（新） → Door._targetRoom（旧 fallback）
- MinimapManager 邻接关系读取支持双路径：WorldGraphSO（新） → Door 运行时推导（旧 fallback）
- 所有新增枚举/数据结构遵循 Spec 中的精确定义

**目的：**
- 让关卡连接关系从"运行时推导"变成"显式数据 + 运行时校验"
- 为后续 Batch 2（房间语法标准化）和 Batch 3（房间级持久状态）提供骨架

**技术：**
- 增量升级策略：所有改动保留旧路径 fallback，零破坏性
- WorldGraphSO 使用 OnEnable/OnValidate 清缓存 + 惰性重建模式
- Door.EffectiveCeremony 兼容旧 _isLayerTransition（Ceremony=Standard + _isLayerTransition=true → Layer）
- DoorTransitionController.ResolveTarget 使用 SpawnPoint_{gateID} 命名约定查找生成点
- LevelValidator 新增的 3 条规则会自动扫描项目中所有 WorldGraphSO 资产
- `dotnet build Project-Ark.slnx` 编译通过，0 错误

---

## 关卡模块重构规范文档：Level_CanonicalSpec.md v1.0 — 2026-03-22

**新建文件：**
- `Docs/Reference/Level_CanonicalSpec.md`

**内容：**
- 建立关卡模块重构的唯一目标架构规范文档
- 定义五层目标架构模型（世界图谱层 → 房间容器层 → 连接缝合层 → 节奏内容层 → 导演引导层）
- 设计核心数据结构：WorldGraphSO、RoomNodeData、ConnectionEdge、RoomNodeType（10 种节点类型）、ConnectionType（6 种连接语义）、TransitionCeremony（5 级仪式感）、GateID 命名规范、RoomFlagRegistry
- 定义房间标准化子节点语法（Navigation/Elements/Encounters/Hazards/Decoration/Triggers）
- 绘制运行时权威数据流向图
- 明确模块边界与跨模块依赖矩阵
- 划分 Editor 工具链职责（轻量版 Authority Matrix）
- 制定增量迁移策略（RoomType → RoomNodeType 映射表、Door 字段迁移、MinimapManager 兼容策略）
- 拆分 4 个 Batch 执行计划，每个 Batch 含任务清单和验收标准
- 附录：示巴星切片 7 房间参考表

**目的：**
- 为关卡模块大重构提供"事前定义"式的目标架构文档，避免 VFX 模块"先乱了再收口"的教训
- 以 Minishoot 工程化关卡思维为基底，保留 Ark 差异化优势（世界时钟、动态阶段、热量战斗核心）
- 作为 Batch 1-4 全部实现工作的执行契约和北极星

**技术：**
- 参考 ShipVFX_CanonicalSpec.md 的文档体系，但侧重"目标架构定义"而非"权限收口治理"
- 基于 Level_Architecture_Synthesis_Minishoot_Silksong_TUNIC.md 的分析结论
- 兼容当前 ~6,900 行运行时代码 + 19 个 Editor 工具脚本的增量升级路径

---

## 文档更新：Activation Halo 标记为已取消 — 2026-03-23 00:10

**修改文件：**
- `Docs/Reference/ShipVFX_MigrationPlan.md`
- `Docs/Reference/ShipVFX_CanonicalSpec.md`
- `Docs/Reference/ShipVFX_AssetRegistry.md`

**内容：**
- 将 `Activation Halo` 从 backlog 总表状态更新为 `已取消 ❌`
- 将 6.5 节整体重写为取消说明，记录取消原因（肉眼不可见、维护成本过高、职责已被覆盖）、替代方案和清理范围
- 从 CanonicalSpec 现役结构树中移除 `BoostActivationHalo` 节点，添加取消注释
- BoostTrailView 层数描述从 `7+1` 更新为 `7`
- AssetRegistry 中 Halo 纹理（`vfx_ring_glow_uneven.png` / `vfx_magnetic_rings.png`）状态从 `Live` 降级为 `Dormant`
- CanonicalSpec 冻结项列表中移除已不再需要冻结的 Halo 纹理
- 清理 MigrationPlan 中其他推进项（Bloom Burst / FlameCore Burst / EmberSparks / Liquid Boost State / FlameTrail）对已取消 Halo 的过时引用，统一改为引用 `FlameCore Burst` 作为起手主确认层

**目的：** 同步文档与代码现实——Activation Halo 代码已被删除（替代方案为强化 Bloom Burst + FlameCore Burst），但三份规范文档仍停留在"实现中"状态，需要对齐。

**技术：** 纯文档维护，无代码变更。

---

## 文档更正：FlameTrail / EmberTrail 描述与验收状态 — 2026-03-22 23:45

**修改文件：**
- `Docs/Reference/ShipVFX_CanonicalSpec.md`
- `Docs/Reference/ShipVFX_MigrationPlan.md`
- `Docs/Reference/ShipVFX_Player_Perception_Reference.md`

**内容：**
- **CanonicalSpec**：FlameTrail_R / FlameTrail_B 的命名表说明从"火焰拖尾"更正为"喷口持续火焰（Local-space，rateOverTime）"
- **MigrationPlan backlog**：FlameTrail_R / FlameTrail_B 和 EmberTrail 状态从"实现中"更新为"已验收 ✅"
- **MigrationPlan 6.10 节**：状态从"实现中，待实机验收"改为"已验收 ✅"；定位描述补充技术细节（Local-space / World-space rateOverDistance）；"当前问题"段标注为"已解决"并逐项说明修复方式
- **Perception Reference**：FlameTrail 从"两侧火焰稳定输出"更正为"喷口根部左右两侧持续喷出贴身火焰，跟随船体移动"；EmberTrail 从"更散更轻的余烬轨迹"更正为"飞船移动时路径上散落的微小余烬碎屑，静止则无粒子"

**目的：** 使文档准确反映当前实现：FlameTrail 已从世界空间拖尾粒子改为本地空间喷口持续火焰，EmberTrail 已从大散团状改为 rateOverDistance 微小碎屑，两者均已完成验收

**技术：** 纯文档更正，无代码变更

---

## 修复 EmberTrail 静止预览无粒子输出 — 2026-03-22 23:15

**修改文件：**
- `Assets/Scripts/Ship/VFX/BoostTrailView.cs`

**内容：**
- 修复 `DebugPreviewEmberTrailSustain()` 点击后完全看不到任何粒子的 bug

**根因：**
- EmberTrail 配置为 `rateOverDistance=2.2`、`rateOverTime=0`（World Space 模拟）
- 粒子只在飞船移动时沿距离发射，静止时一个粒子都不会出现
- Inspector 预览按钮是在飞船静止状态下使用的，所以永远看不到效果
- 同时 `SetBoostIntensity()` 在内部调用 `ApplyParticleSustainState()` 时对 EmberTrail 传入 `useRateOverTime: false`，会把 `rateOverTimeMultiplier` 强制设为 0，即使手动启动粒子系统也不会发射

**修复：**
- `DebugPreviewEmberTrailSustain()` 现在临时将 EmberTrail 从 `rateOverDistance` 切换到 `rateOverTime` 模式
- 新增 `SetBoostIntensityShaderOnly()` 方法：只驱动 MainTrail + EnergyLayer 的 shader `_BoostIntensity`，不修改粒子发射参数，避免覆盖预览时的临时 rateOverTime 设置
- 直接设置 `_currentIntensity = 1f` 而非调用 `SetBoostIntensity(1f)`，防止 sustain 逻辑覆盖预览配置

**技术：** ParticleSystem.emission rateOverTime/rateOverDistance 模式切换 + 预览/运行时职责隔离

---

## FlameTrail Preview 拆分为 R / B / Both — 2026-03-22 22:45

**修改文件：**
- `Assets/Scripts/Ship/VFX/BoostTrailView.cs`
- `Assets/Scripts/Ship/VFX/BoostTrailDebugManager.cs`
- `Assets/Scripts/Ship/Editor/BoostTrailDebugManagerEditor.cs`

**内容：**
- 将原来的 `DebugPreviewFlameTrailSustain()` 拆分为三个方法：`DebugPreviewFlameTrailR()` / `DebugPreviewFlameTrailB()` / `DebugPreviewFlameTrailBoth()`
- 共享 isolation 逻辑提取到 `IsolateForFlameSustainPreview()` 私有方法
- DebugManager 对应拆为 `PreviewFlameTrailR()` / `PreviewFlameTrailB()` / `PreviewFlameTrailBoth()`
- Editor Inspector 的 Sustain Layer Preview 区域改为水平三按钮布局：`FlameTrail_R` | `FlameTrail_B` | `Both`

**目的：**
- 用户反馈 preview 时无法区分 FlameTrail_R（红/橙焰尾）和 FlameTrail_B（紫色电光）的各自表现，需要独立隔离查看

**技术：**
- 提取共享 isolation 逻辑为 private 方法，减少重复代码
- 三个按钮用 `EditorGUILayout.BeginHorizontal()` 水平排列，节省 Inspector 垂直空间

---

## FlameTrail / EmberTrail Quick Preview 按钮 — 2026-03-22 22:30

**修改文件：**
- `Assets/Scripts/Ship/VFX/BoostTrailView.cs`
- `Assets/Scripts/Ship/VFX/BoostTrailDebugManager.cs`
- `Assets/Scripts/Ship/Editor/BoostTrailDebugManagerEditor.cs`

**内容：**
- 在 `BoostTrailView` 新增 `DebugPreviewFlameTrailSustain()` 和 `DebugPreviewEmberTrailSustain()` 两个独立 preview 方法
- 每个方法的逻辑：Reset 所有其他层 → 将 master intensity 设为 1.0 → 只播放目标 sustain 粒子系统
- 在 `BoostTrailDebugManager` 新增 `PreviewFlameTrailSustain()` 和 `PreviewEmberTrailSustain()` 入口方法
- 在 `BoostTrailDebugManagerEditor` 的 Quick Preview 区域新增 "Sustain Layer Preview" 子标签，下挂两个按钮

**目的：**
- 此前 FlameTrail_R/B 和 EmberTrail 是持续层（sustain particles），只能通过 ForceSustainPreview + Solo Layer 组合操作才能单独预览，步骤繁琐
- 新增独立 Quick Preview 按钮后，一键即可隔离查看单层 sustain 效果

**技术：**
- 复用已有的 `ResetState()` + `SetBoostIntensity()` + `PlayParticle()` 内部方法
- sustain preview 按钮放在 burst 按钮的 `DisabledScope` 之外，因为它们不依赖 ForceSustainPreview 模式
- 遵循 debug 工具纪律：只做显式预览，不接管正式运行链

---

## BoostTrailView 参数数据驱动迁移 + 衰减尾声自动停止 — 2026-03-22 17:45

**修改文件：**
- `Assets/Scripts/Ship/Data/ShipJuiceSettingsSO.cs`
- `Assets/Scripts/Ship/VFX/BoostTrailView.cs`
- `Assets/Scripts/Ship/Editor/BoostTrailPrefabCreator.cs`
- `Assets/Scripts/Ship/Editor/ShipVfxValidator.cs`

**内容：**
1. **参数数据驱动迁移**：将 `BoostTrailView` 中 25+ 个硬编码 `[SerializeField]` 参数全部迁移到 `ShipJuiceSettingsSO`。包括：
   - Startup Sequencing（3 个 delay 参数）
   - FlameTrail/EmberTrail/EnergyLayer2/EnergyLayer3 Sustain（各 2 个：blendInThreshold + maxIntensity，共 8 个）
   - Particle Sustain Curve（minSize + minSpeed + stopThreshold，3 个新参数）
   - Intensity Animation（rampUp + rampDown，2 个）
   - TrailRenderer（trailTime + widthMultiplier，2 个）
   - Bloom Burst（5 个参数）
2. **衰减尾声自动停止**：新增 `_sustainParticleStopThreshold`（默认 0.005），`ApplyParticleSustainState` 在 intensity 低于此阈值时主动调用 `Stop()`，消除 "playing but emitting nothing" 的空闲 profiler 开销。
3. **BoostTrailPrefabCreator 更新**：删除旧的 17 行 `FindProperty("_xxx").floatValue = ...` 硬编码参数设置，替换为 `_juiceSettings` SO 引用接线。
4. **ShipVfxValidator 更新**：
   - `AllowedBoostTrailViewSceneOverrides` 白名单增加 `_juiceSettings`
   - 新增 `_juiceSettings` 为空的 prefab 级验证检查

**目的：** 遵循架构原则"数据驱动"——所有游戏数值必须放在 ScriptableObject 中，策划可以不碰代码就调参。同时消除衰减尾声的微量 profiler 开销。

**技术：** SO getter + 内联 fallback 默认值（防止 SO 未接线时运行时崩溃）。`ApplyParticleSustainState` 新增 early-return Stop 逻辑。

---

## Liquid Boost State 验收 + MigrationPlan/CanonicalSpec/AssetRegistry 文档更新 — 2026-03-22 01:30

### 修改文件
- `Docs/Reference/ShipVFX_MigrationPlan.md`
- `Docs/Reference/ShipVFX_CanonicalSpec.md`
- `Docs/Reference/ShipVFX_AssetRegistry.md`

### 内容
`Liquid Boost State` 已通过实机验收，更新三份规范文档：
1. **MigrationPlan**：backlog 总表中该条目状态从"实现中"更新为"已验收 ✅"；owner 从 `ShipView` 更正为 `ShipBoostVisuals`；推进项 §6.9 的详细卡片替换为实现总结与验收结果；同步更正 `Thruster Entry Pulse` 的 owner
2. **CanonicalSpec**：`ShipBoostVisuals` Worker 职责描述更新，反映 Liquid sprite swap + sortOrder + HDR 颜色 tween
3. **AssetRegistry**：`ShipBoostVisuals` / `ShipJuiceSettingsSO` / `Boost_16.png` 的 Notes 和 Owner 更新

### 目的
保持三份规范文档与实际代码、验收状态一致，避免认知漂移。

### 技术
纯文档更新，无代码变更。

---

## Liquid 层 Boost 可见性增强（Sprite Swap + SortOrder + HDR 颜色）— 2026-03-22 01:15

### 修改文件
- `Assets/Scripts/Ship/Data/ShipJuiceSettingsSO.cs`
- `Assets/Scripts/Ship/VFX/ShipBoostVisuals.cs`
- `Assets/_Data/Ship/DefaultShipJuiceSettings.asset`

### 内容
Liquid 层在 Boost 时完全不可见的问题修复：
1. **ShipJuiceSettingsSO** 新增三个数据驱动字段：
   - `_boostLiquidSprite`（Sprite）：Boost 时替换的 Sprite（设为 Boost_16.png）
   - `_boostLiquidSortOverride`（bool）+ `_boostLiquidSortOrder`（int）：Boost 时将 Liquid 的 sortOrder 从 -2 提升到 1（在 Solid 之上）
   - `_boostLiquidColor` 默认值从 `(0.6, 0.85, 1.0)` 改为 HDR `(3, 4, 5, 1)`
2. **ShipBoostVisuals** 在 `OnBoostStarted()` / `OnBoostEnded()` / `ResetState()` 中实现：
   - Sprite swap（Boost 时换成 Boost_16.png，结束时恢复 Movement_3.png）
   - SortOrder 提升（Boost 时 sortOrder=1 在 Solid 之上，结束时恢复 -2）
   - 基线 sprite 和 sortOrder 在 `Initialize()` 中捕获

### 目的
Liquid 层使用 Additive 材质 + sortOrder -2（在 Solid 之下），且基线颜色极暗 `(0.15, 0.25, 0.3)`，即使 tween 到 `(1, 0, 0.8)` 也因被 Solid 层完全遮盖而不可见。通过三管齐下（更亮的 sprite、提升到 Solid 之上、HDR 高强度颜色）确保 Boost 时 Liquid 层可见。

### 技术
- 数据驱动（所有参数在 SO 中配置），运行时不 hardcode
- PrimeTween `Tween.Color` 做 HDR 颜色过渡
- 基线状态在 `Initialize()` 捕获，`ResetState()` 完整恢复（防对象池状态泄漏）

---

## 清理重复 ShipStateController + Rebuilder 防重复加固 — 2026-03-22 09:00

**修改文件：**
- `Assets/Scripts/Ship/Editor/ShipPrefabRebuilder.cs`
- `Assets/Scenes/SampleScene.unity`（通过 MCP 保存）

**内容：**
- 通过 MCP `manage_components(remove)` 删除了 Ship GameObject 上多余的第二个 `ShipStateController` 组件（scene override 残留，instanceID: 63378），保存场景
- 加固 `ShipPrefabRebuilder.EnsureComponent<T>()` 方法：使用 `GetComponents<T>()` (复数) 检测同类型重复组件，如果发现多个则保留第一个、`DestroyImmediate` 其余重复项，并在日志中输出 `⚠ removed N duplicate(s)` 警告

**目的：**
- 修复历史遗留的重复组件问题（`GetComponent` 只返回第一个，无法感知第二个的存在）
- 防止未来任何原因（手动添加、scene override 漂移）再次出现同类型组件重复

**技术：**
- `GetComponents<T>()` vs `GetComponent<T>()` 的区别：前者返回数组包含所有同类型组件，后者只返回第一个

---

## Liquid Boost State 重构：删除 Sprite Swap，改用颜色 Tween — 2026-03-22 08:30

**修改文件：**
- `Assets/_Art/Ship/Glitch/Boost_16.png.meta` — 修复 PPU 从 100 → 320（与其他 sprite 一致）
- `Assets/Scripts/Ship/VFX/ShipBoostVisuals.cs` — 完全重写
- `Assets/Scripts/Ship/Data/ShipJuiceSettingsSO.cs` — 替换参数
- `Assets/Scripts/Ship/Editor/ShipPrefabRebuilder.cs` — 移除 Boost sprite 接线
- `Assets/Scripts/Ship/Editor/ShipVfxValidator.cs` — 移除已删字段的验证

**问题根因：** `Boost_16.png` 的 Pixels Per Unit = 100，而 `Movement_3.png` = 320。Boost 时切换 sprite 导致 Liquid 层瞬间变大 3.2 倍，加上 Additive 材质黑底，表现为"比飞船大一倍的黑色 sprite"。

**删除的逻辑：**
- Liquid Sprite Swap（`_boostLiquidSprite` / `_normalLiquidSprite` / `_enableLiquidSwap`）
- 三段式 Glow 亮度包络（Attack-Settle-Sustain，含 `BoostGlowBrightnessMultiplier` / `BoostGlowEntryBrightnessMultiplier` / `BoostGlowRampUpDuration` / `BoostGlowSettleDuration` / `BoostGlowRampDownDuration` 共 5 个参数）
- 手写 easing 辅助函数（`EaseOutQuad` / `EaseOutCubic`）

**新方案：**
- 用 `Tween.Color()` 直接从基线色 tween 到可配置的 `BoostLiquidColor`（默认浅蓝 `0.6, 0.85, 1.0`）
- 参数简化为 3 个：`BoostLiquidColor` / `BoostLiquidRampUpDuration` / `BoostLiquidRampDownDuration`
- 使用 PrimeTween 内置缓动（`OutCubic` 上升 / `InQuad` 下降），删除手写 easing
- Boost_16.png 保留但修复 PPU，未来可用于其他用途

**技术：** PrimeTween `Tween.Color()`，数据驱动 `ShipJuiceSettingsSO`

---

## 全局 DepthOfField 导致画面模糊 — 关闭默认 DoF — 2026-03-22 07:15

### 修改文件
- `Assets/Settings/GlobalVolumeProfile.asset` — 将 DepthOfField 组件的 `active` 从 `1` 改为 `0`

### 内容
- **根因**：`GlobalVolumeProfile.asset` 中启用了 DepthOfField（Bokeh 模式，focusDistance=5，focalLength=50）。在相机开启 Post Processing 之前此效果不生效，开启后 DoF 对所有画面产生景深模糊
- **症状**：开启 Post Processing 修复 Bloom Burst 后，整个画面变得模糊
- **分析**：对 2D 俯视角游戏来说，全局 DoF 几乎不应常开——所有 Sprite 在同一 Z 平面，DoF 只会产生不可控模糊。编织态（Weaving）过渡需要的 DoF 由 `WeavingStateTransition.cs` 动态控制，不依赖全局默认值
- **修复**：将 GlobalVolumeProfile 中 DepthOfField 的 `active` 设为 `0`（默认关闭），保留 Bloom 和 Vignette

### 目的
消除 2D 游戏中不必要的全局景深模糊，保持画面清晰锐利，特效后处理（Bloom/Vignette）继续正常工作

### 技术
- URP Volume Profile 后处理组件 `active` 开关控制
- DoF 仅在编织态等特定时刻由代码动态启用

---

## Bloom Burst 不可见 — Camera Post Processing 未开启 — 2026-03-22 06:50

### 修改文件
- `Assets/Scenes/SampleScene.unity` — Main Camera 的 `UniversalAdditionalCameraData.renderPostProcessing` 从 `false` 改为 `true`
- `Assets/Scripts/Ship/VFX/BoostTrailView.cs` — 新增 `ValidateCameraPostProcessing()` 防御性检查方法

### 内容
- **根因**：Main Camera 上的 URP Additional Camera Data 组件的 `renderPostProcessing` 为 `false`，导致 URP 渲染管线完全跳过后处理（包括 Bloom），Bloom Burst 的 Volume weight 和 intensity 被正确动画化但视觉上完全不可见
- **症状**：Preview Bloom Burst 按钮点击后无任何视觉反应，但 FlameCore Burst 和 EmberSparks Burst 正常工作（它们是粒子系统，不依赖后处理）
- **修复**：通过 MCP 将 `m_RenderPostProcessing` 设为 `true` 并保存场景
- **防御**：在 `BoostTrailView.Awake()` 中新增 `ValidateCameraPostProcessing()` 方法（`Conditional("UNITY_EDITOR")`），启动时主动检查主相机是否开启后处理，缺失时输出 `LogError`

### 目的
使 Bloom burst 后处理效果在运行时可见，并防止未来因相机配置遗漏导致同类 silent failure

### 技术
- URP `UniversalAdditionalCameraData.renderPostProcessing` 配置
- `[System.Diagnostics.Conditional("UNITY_EDITOR")]` 防御性校验（仅 Editor 生效，不影响 Release build 性能）

---

## ShipBuilder 删除 + ShipPrefabRebuilder 升级为全权威 — 2026-03-22 03:15

### 修改文件

- `Assets/Scripts/Ship/Editor/ShipPrefabRebuilder.cs` — 扩展为 Ship.prefab 唯一全权威
- `Assets/Scripts/Ship/Editor/ShipVfxValidator.cs` — 移除对已删除 ShipBuilder 的审计检查
- `Docs/Reference/ShipVFX_CanonicalSpec.md` — 更新工具职责表
- `Docs/Reference/ShipVFX_AssetRegistry.md` — 删除 ShipBuilder 条目、更新引用
- `Docs/Reference/ShipVFX_MigrationPlan.md` — 记录 ShipBuilder 已合并删除
- `Implement_rules.md` — Authority Matrix 更新

### 删除文件

- `Assets/Scripts/Ship/Editor/ShipBuilder.cs` — 已被 ShipPrefabRebuilder 完全取代

### 内容

Phase A 治理后续收口：消除 ShipBuilder（Bootstrap 工具）与 ShipPrefabRebuilder（Authority 工具）的职责重叠。

**ShipPrefabRebuilder 新增能力**（从 ShipBuilder 吸收）：
1. 根节点物理组件 ensure（Rigidbody2D + CircleCollider2D）
2. 根节点全部运行时脚本组件 ensure（InputHandler → ShipMotor → ShipAiming → ShipStateController → ShipHealth → ShipDash → ShipBoost），按 RequireComponent 依赖顺序
3. ShipStatsSO 接线到所有 6 个消费组件
4. InputActionAsset 接线到 InputHandler
5. DashAfterImage prefab + ShipStatsSO 接线到 DashAfterImageSpawner

**未转移的能力**（因属于场景级而非 prefab 级）：
- WeavingStateTransition 跨程序集场景接线 — 需手动或后续创建 ShipSceneBinder 处理

### 目的

实现 Phase A "只跑 Authority 菜单" 的工作流承诺：`ProjectArk > Ship > Authority > Rebuild Ship Prefab` 一个入口即可覆盖 Ship.prefab 的所有组件和接线，不再需要额外跑 Bootstrap 工具。

### 技术

- 使用 `PrefabUtility.EditPrefabContentsScope` 在 prefab 编辑上下文中操作
- 通过 `SerializedObject` + `FindProperty` 实现跨组件序列化字段接线
- 新增 `WireStatsToComponent<T>` 泛型辅助方法避免重复代码

---

## Halo 彻底删除 + Bloom/FlameCore 增强替代 — 2026-03-22 01:30

**修改文件：**
- `Assets/Scripts/Ship/VFX/BoostTrailView.cs`
- `Assets/Scripts/Ship/Editor/BoostTrailPrefabCreator.cs`
- `Assets/Scripts/Ship/Data/ShipJuiceSettingsSO.cs`
- `Assets/Scripts/Ship/VFX/ShipBoostVisuals.cs`
- `Implement_rules.md`
- `Docs/Reference/ShipVFX_CanonicalSpec.md`

**内容：**
1. **彻底删除 BoostActivationHalo**（之前 00:25 的删除未被 commit，代码回退后又复活）：
   - `BoostTrailView`：移除 `_activationHalo` / `_juiceSettings` 字段、`_haloSequence` 状态、Awake/OnDestroy/OnBoostStart/ResetState 中的 Halo 逻辑、整个 `TriggerActivationHalo()` 方法
   - `BoostTrailPrefabCreator`：移除 `BoostActivationHalo` 子节点创建、`_activationHalo` + `_juiceSettings` 接线
   - `ShipJuiceSettingsSO`：移除 `_haloPulseDuration` / `_haloPeakAlpha` / `_haloColor` 3 个字段和 3 个 Getter
   - `ShipBoostVisuals`：更新 Tooltip 移除 "halo" 字样
2. **替代方案——强化现有 Bloom burst + FlameCore burst**（不新增任何节点/组件/字段）：
   - Bloom burst：`_bloomBurstIntensity` 2.15→3.2、`_bloomPeakWeight` 0.72→0.88、`_bloomAttackDuration` 0.05→0.035（更快 attack = 更 punch）、`_bloomReleaseDuration` 0.16→0.22（更自然衰减）
   - FlameCore：`maxParticles` 48→64、`startSize` 0.28-0.52→0.32-0.62、burst count 16→22
   - 同步更新 `BoostTrailPrefabCreator` 中的对应默认值
3. **文档更新**：`Implement_rules.md` 和 `ShipVFX_CanonicalSpec.md` 移除所有 Halo 引用

**目的：** Halo 层 duration=0.25s 且与 Bloom burst 职能完全重叠，维护成本远高于视觉收益。通过强化已有的 Bloom 和 FlameCore，用零新增代码实现同等甚至更好的"起手确认感"。

**技术：** 纯参数调优，无新增类/字段/节点。BoostTrailRoot 从 8 层降到 7 层。

---

## Boost Activation Halo 简化重构（选项 B 后半部分） — 2026-03-21 23:55

**修改文件：**
- `Assets/Scripts/Ship/Data/ShipJuiceSettingsSO.cs`
- `Assets/Scripts/Ship/VFX/BoostTrailView.cs`
- `Assets/Scripts/Ship/Editor/BoostTrailPrefabCreator.cs`

**内容：**
- 在 `ShipJuiceSettingsSO` 中新增 3 个 Halo 参数：`_haloPulseDuration`（0.25s）、`_haloPeakAlpha`（0.65）、`_haloColor`（HDR 暖橙）
- 在 `BoostTrailView` 中新增 `_activationHalo`（SpriteRenderer）和 `_juiceSettings`（ShipJuiceSettingsSO）引用
- 新增 `TriggerActivationHalo()` 方法：用 PrimeTween Sequence 实现 alpha: 0→peak→0 + scale: 0.3→1.2→0.85 的脉冲动画
- 在 `OnBoostStart()` 中调用 `TriggerActivationHalo()`，紧跟 FlameCore burst 和 Bloom burst 之后
- `ResetState()` 和 `OnDestroy()` 中正确停止 `_haloSequence` 并重置 alpha/scale
- `Awake()` 中初始化 halo 为透明+零尺寸
- 在 `BoostTrailPrefabCreator` 中新增 `BoostActivationHalo` 子节点创建逻辑（SpriteRenderer, sortingOrder=4, 初始透明）
- Prefab 构建时自动接线 `_activationHalo` 和 `_juiceSettings`（从 `Assets/_Data/Ship/DefaultShipJuiceSettings.asset` 加载）

**目的：**
- 实现选项 B 方案的后半部分——用数据驱动的简化方式替代之前被删除的复杂 Halo 系统
- 3 个 SO 参数替代原来的 6 个字段 + 3 个方法，大幅简化维护成本

**技术：**
- PrimeTween Sequence（`Sequence.Create().Group().Chain().Group()`）实现并行 alpha + scale 动画
- ShipJuiceSettingsSO 数据驱动，策划可直接调参不碰代码
- 遵循 Ship/VFX Authority Matrix：Prefab 结构由 BoostTrailPrefabCreator 唯一负责

---

## 删除 BoostActivationHalo（选项 B 精简重构） — 2026-03-22 00:25

**修改文件：**
- `Assets/Scripts/Ship/VFX/BoostTrailView.cs`
- `Assets/Scripts/Ship/VFX/BoostTrailDebugManager.cs`
- `Assets/Scripts/Ship/Editor/BoostTrailDebugManagerEditor.cs`
- `Assets/Scripts/Ship/Editor/BoostTrailPrefabCreator.cs`
- `Assets/Scripts/Ship/Editor/ShipVfxValidator.cs`

**内容：**
完全移除 `BoostActivationHalo`（Layer 7）的全部代码。一个"boost 启动时闪 0.12 秒橙光环"的效果原本涉及 7 个文件（PrefabCreator / Rebuilder / MaterialCreator / BoostTrailView / DebugManager / DebugEditor / Validator）协作，属于典型过度工程。

**具体删除项：**
- `BoostTrailView`：移除 `_activationHalo` 引用字段、5 个调参字段（peakAlpha / duration / startScale / peakScale / endScale）、`_activationHaloTween` / `_activationHaloBaseScale` / `_activationHaloBaseColor` 私有状态、`TriggerActivationHalo()` / `ApplyActivationHalo()` / `ResetActivationHalo()` / `DebugPreviewActivationHalo()` 方法、仅 halo 使用的 `EaseOutQuart()` / `EaseInCubic()` ease 函数、`DebugApplyVisibilityMask` 签名中的 `showActivationHalo` 参数
- `BoostTrailDebugManager`：移除 `SoloLayer.ActivationHalo` 枚举值、`_showActivationHalo` 字段、`PreviewActivationHaloBurst()` 方法、`LayerVisibilityState` 中的 `ShowActivationHalo` 成员
- `BoostTrailDebugManagerEditor`：移除 halo 属性绘制和 "Preview Activation Halo" 按钮
- `BoostTrailPrefabCreator`：移除 halo sprite 路径常量（4 个）、`BoostActivationHalo` GO 创建代码、halo 接线和调参 SerializedProperty 设置、`FindActivationHaloSprite()` 方法、`ShipGlowMaterialPath` 常量（仅 halo 使用）
- `ShipVfxValidator`：从 `BoostTrailRequiredChildren` 移除 `"BoostActivationHalo"`、移除 halo SpriteRenderer 查找和验证行

**目的：**
消除过度工程化的 VFX 层。该效果 duration=0.12 秒、peakPoint=0.09 秒，实际肉眼几乎不可见，却需要跨 7 个文件维护。删除后 BoostTrail 从 8 层降为 7 层，代码减少约 120 行，工具链更简洁。

**技术：**
纯删除重构，无新增代码。编译 0 错误通过。后续需在 Unity Editor 中重建 BoostTrailRoot prefab（运行 `ProjectArk > Ship > VFX > Authority > Rebuild BoostTrailRoot Prefab`）以移除场景中残留的 BoostActivationHalo 子节点。

---

## 清理 EngineParticles / ShipEngineVFX 文档与代码残留 — 2026-03-21 23:05

**修改文件：**
- `Assets/Scripts/Ship/Editor/ShipPrefabRebuilder.cs` — 移除注释中对已删除 `ShipEngineVFX` 的具体引用（两处），改为通用描述
- `Assets/Scripts/Ship/Editor/ShipFeelAssetCreator.cs` — 对话框文字移除 `Engine VFX` 和 `ShipEngineVFX` 引用
- `Docs/VerificationChecklist.md` — 移除 F3（ShipEngineVFX 配置）和 F12（引擎粒子随速度变化），删除组件③ ShipEngineVFX 配置段落，重新编号

**确认结果：**
- `ShipPrefabRebuilder.cs` 功能性代码已干净，不会生成 `EngineParticles` 节点
- `Ship.prefab` 内无 `EngineParticles` 残留
- `SampleScene.unity` 中仍残留一个场景级 `EngineParticles` GameObject（fileID: 520920091）和一条 `_engineParticles` scene override（指向 missing script 字段），需用户在 Unity Editor 中手动删除

**目的：** 彻底清除已删除 `ShipEngineVFX` 系统在代码注释、Editor 工具对话框和验证文档中的残留引用，避免后续维护者产生困惑。

**技术：** 文本替换 + 文档同步。

---

## 收编体制外孤岛：ShipVisualJuice + DashAfterImageSpawner 纳入 Worker 体系 — 2026-03-21 21:30

**修改文件：**
- `Assets/Scripts/Ship/VFX/ShipVisualJuice.cs` — 移除 `[RequireComponent]`、`Awake` 中的 `GetComponent` 自获取、`OnEnable/OnDisable` 事件订阅（`ShipMotor.OnSpeedChanged` + `ShipDash.OnDashStarted`）。改为被动 Worker 模式：新增 `Initialize(motor, dash, boost, aiming)` 由 ShipView 注入引用，`OnSpeedChanged(float)` / `OnDashStarted()` 改为公有方法由 ShipView 路由调用，新增 `ResetState()` 供池回收，新增 `_enableAll` / `_enableMoveTilt` / `_enableSquashStretch` Inspector toggles
- `Assets/Scripts/Ship/VFX/DashAfterImageSpawner.cs` — 移除 `[RequireComponent]`、`Awake` 中的 `GetComponent` 自获取、`OnEnable/OnDisable` 事件订阅（`ShipDash.OnDashStarted`）。改为被动调用模式：新增 `Initialize(dash)` 注入引用，`TriggerSpawn()` 公有方法替代原内部 `OnDashStarted`，新增 `CancelSpawning()` 供重置，spawn 序列使用独立 `CancellationTokenSource` 管理
- `Assets/Scripts/Ship/VFX/ShipDashVisuals.cs` — 新增 `[SerializeField] DashAfterImageSpawner _afterImageSpawner` 引用 + `_enableAfterImages` toggle。`OnDashStarted()` 中新增 `_afterImageSpawner.TriggerSpawn()` 调用，`ResetState()` 中新增 `_afterImageSpawner.CancelSpawning()` 调用
- `Assets/Scripts/Ship/VFX/ShipView.cs` — 新增 `ShipVisualJuice _juiceVisuals` + `DashAfterImageSpawner _afterImageSpawner` 序列化引用 + `_enableJuiceVFX` toggle。新增 `ShipMotor`/`ShipBoost`/`ShipAiming` 缓存。`OnEnable` 新增 `ShipMotor.OnSpeedChanged` 订阅（`OnDisable` 对应取消）。`InitializeWorkers()` 新增两者的初始化调用。`HandleStateChanged()` 中 Dash 分支新增 `_juiceVisuals.OnDashStarted()` 路由。新增 `HandleSpeedChanged()` 方法路由到 `_juiceVisuals.OnSpeedChanged()`。`ResetVFX()` 新增 `_juiceVisuals.ResetState()` 调用
- `Assets/Scripts/Ship/Editor/ShipPrefabRebuilder.cs` — 新增 `ShipVisualJuice` 组件 `EnsureComponent` + `_visualChild` 接线 + `_juiceSettings` 接线。`DashAfterImageSpawner` 由原"被动检查"改为主动 `EnsureComponent` + 完整接线（`_shipSpriteRenderer` + `_juiceSettings` + wired into `ShipDashVisuals._afterImageSpawner`）。ShipView 新增 `_juiceVisuals` + `_afterImageSpawner` 两个字段的接线
- `Docs/Reference/ShipVFX_CanonicalSpec.md` — Runtime 主驱动关系更新：ShipVisualJuice 和 DashAfterImageSpawner 标注为 Worker / 二级 Worker；ShipView 订阅事件列表新增 `ShipMotor.OnSpeedChanged`；工具职责表更新
- `Docs/Reference/ShipVFX_AssetRegistry.md` — Runtime Scripts 表中 ShipVisualJuice / DashAfterImageSpawner 的 Canonical Name 从 Controller 改为 Worker，Notes 更新为 Worker 身份描述；ShipView Notes 更新；ShipPrefabRebuilder Notes 更新

**目的：** 消除 Ship/VFX 模块中最后两个"体制外孤岛"。改造前：`ShipVisualJuice` 直接订阅 `ShipMotor.OnSpeedChanged` + `ShipDash.OnDashStarted`，`DashAfterImageSpawner` 直接订阅 `ShipDash.OnDashStarted`——两者均绕过 ShipView 的统一事件路由，不受 `_enableXxxVFX` 主开关控制，不在 `ResetVFX()` 清理范围内。改造后：所有 VFX 组件统一通过 `ShipView → Worker` 体系管理，零体制外孤岛。

**技术：** Worker 被动调用模式（Initialize 注入引用 + 公有方法接收信号，不自行订阅事件）。DashAfterImageSpawner 采用"二级 Worker"模式（由 ShipDashVisuals 委托驱动，与 BoostTrailView 被 ShipBoostVisuals 委托的模式一致）。ShipView 新增 `ShipMotor.OnSpeedChanged` 订阅作为速度信号路由源。

---

## 清除体制外遗留 ShipEngineVFX + EngineParticles — 2026-03-21 20:15

**删除文件：**
- `Assets/Scripts/Ship/VFX/ShipEngineVFX.cs`（+ `.meta`）

**修改文件：**
- `Assets/Scripts/Ship/Editor/ShipPrefabRebuilder.cs` — 移除 EngineParticles 节点创建、ShipEngineVFX 组件创建/接线、MANAGED_BACK_CHILDREN 数组、EnsureParticleSystemChild helper、ForceDelete 相关逻辑
- `Assets/Scripts/Ship/Editor/ShipBuilder.cs` — 移除 ShipEngineVFX 组件添加、EngineParticles 子节点创建、ConfigureEngineParticles 方法、WireReferences 中 engineVFX 接线
- `Assets/Scripts/Ship/Editor/ShipVfxValidator.cs` — 移除 ValidateShipEngineVfxWiring 方法及其调用
- `Assets/Scripts/Ship/Data/ShipJuiceSettingsSO.cs` — 移除 Engine Particles 全部 8 个序列化字段和对应 public getters
- `Docs/Reference/ShipVFX_CanonicalSpec.md` — 从现役链路树、Runtime 驱动列表、工具职责表、节点命名表中移除 ShipEngineVFX / EngineParticles
- `Docs/Reference/ShipVFX_AssetRegistry.md` — 从 Runtime Scripts 表和 Prefabs 表中移除相关条目

**目的：** `ShipEngineVFX` 是 Worker 架构建立前的遗留组件，从未纳入 `ShipView → Worker` 统一管理体系。它绕过 ShipView 的事件路由，直接订阅 ShipMotor/ShipDash/ShipBoost 事件，没有 toggle 控制，且管理的 EngineParticles 节点本身没有 Sprite（实际视觉贡献为零）。清除后：权威入口更干净、toggle 体系完整、Prefab 结构更简洁。

**技术：** 直接删除脚本 + 从 3 个 Editor 工具中移除所有引用 + 从 SO 数据层中移除参数字段 + 同步 2 份规范文档。

**后续用户操作：**
- 在 Unity Editor 中运行 `ProjectArk > Ship > Authority > FORCE Rebuild Ship Prefab` 清除 Prefab 中残留的 EngineParticles 节点和 ShipEngineVFX 组件

---

## VFX Worker Enable Toggle — Inspector 细粒度开关统一 — 2026-03-21 19:00

**修改文件：**
- `Assets/Scripts/Ship/VFX/ShipBoostVisuals.cs`
- `Assets/Scripts/Ship/VFX/ShipHitVisuals.cs`
- `Assets/Scripts/Ship/VFX/ShipDashVisuals.cs`
- `Assets/Scripts/Ship/VFX/ShipView.cs`

**内容：**
为三个 VFX Worker 统一添加了 Inspector 可见的 Enable Toggle 开关体系，风格与 BoostTrailView 的 DebugApplyVisibilityMask 保持一致：

1. **ShipBoostVisuals** — 新增 7 个 toggle：
   - `_enableAll`（总开关）
   - `_enableLiquidSwap`（Liquid sprite Normal↔Boost 切换）
   - `_enableLiquidGlow`（Liquid glow 亮度渐变）
   - `_enableHLRamp`（HL 层 alpha 渐变）
   - `_enableCoreRamp`（Core 层 alpha 渐变）
   - `_enableThrusterPulse`（推进器脉冲动画）
   - `_enableBoostTrail`（BoostTrailView 启停代理）

2. **ShipHitVisuals** — 新增 4 个 toggle：
   - `_enableAll`（总开关）
   - `_enableHitFlash`（多层白闪）
   - `_enableIFrameBlink`（受击无敌闪烁）
   - `_enableLowHPPulse`（Core 层低血量红色脉冲）

3. **ShipDashVisuals** — 新增 3 个 toggle：
   - `_enableAll`（总开关）
   - `_enableIFrameFlicker`（冲刺无敌闪烁）
   - `_enableDodgeGhost`（冲刺残影幽灵）

4. **ShipView** — 新增 3 个 Worker 级总开关：
   - `_enableBoostVFX`（路由层控制是否向 BoostVisuals 派发事件）
   - `_enableHitVFX`（路由层控制是否向 HitVisuals 派发事件）
   - `_enableDashVFX`（路由层控制是否向 DashVisuals 派发事件）

**目的：** 统一 VFX 子系统的开关控制模式，策划可在 Inspector 中精细地开关任何一个子特效，无需改代码或断开引用。

**技术：** SerializeField bool toggle + 守卫检查模式，所有 toggle 默认 true（不影响已有行为），两层控制——ShipView 级总开关 + Worker 级细粒度开关。

---

## Ship VFX Phase A 治理收尾 — Prefab 验证与文档同步 — 2026-03-21 15:00

### 修改文件

- `Docs/Reference/ShipVFX_CanonicalSpec.md` — §3 Runtime 主驱动关系更新为 Coordinator+Worker 架构描述；§7 工具职责边界表更新 `ShipPrefabRebuilder` 包含 Worker 创建与接线
- `Docs/Reference/ShipVFX_AssetRegistry.md` — §2 新增 3 个 Worker 条目（ShipBoostVisuals/ShipHitVisuals/ShipDashVisuals）；§3 新增 ShipVfxAuditTool + BoostTrailDebugInspector 条目；§8 新增 Phase A 治理状态（5 条全部通过）+ 7 项持续关注项

### 内容

**Phase A 治理验收——五条标准全部通过：**

1. ✅ 唯一权威：`ShipPrefabRebuilder` / `BoostTrailPrefabCreator` / `ShipBoostTrailSceneBinder` / `MaterialTextureLinker` 各司其职，无交叉写入
2. ✅ 无双轨主链：16 项 legacy 代码模式（FindAssets/FindSpriteExactOrByName/VisualChild/DODGE_SPRITE_SRC_PATH 等）全部确认已清理
3. ✅ Debug 不接管主链：`BoostTrailDebugManager` 无 Awake/OnValidate/Reset/LateUpdate/AutoAssignReferences，纯预览组件
4. ✅ Override 白名单化：`ShipVfxValidator` 白名单仅允许 `_boostBloomVolume`
5. ✅ 无静默失败：关键引用缺失时有 Debug.LogError/LogWarning，ShipVfxValidator 1088 行审计全覆盖

**Prefab 验证：**

- 首次运行 ShipVfxValidator 发现 6 个 Error（Ship.prefab 缺少 ShipBoostVisuals/ShipHitVisuals/ShipDashVisuals 组件及接线）
- 运行 `ShipPrefabRebuilder`（菜单 ProjectArk > Ship > Authority > Rebuild Ship Prefab）成功修复
- 通过 MCP `manage_prefabs` 直接验证：Ship.prefab 根节点包含 19 个组件（含 ShipView + 3 Workers），ShipVisual 下 7 个子节点，BoostTrailRoot 下 9 个子节点——全部正确

### 目的

完成 Ship/VFX 模块 Phase A authority 收口治理的最终验证与文档同步，使该模块的权威来源、工具边界、debug 约束全部有据可查。

### 技术

MCP `manage_prefabs` (get_info + get_hierarchy) 替代 Console 读取做 Prefab 结构验证；静态代码审计覆盖 16 项反回归检查模式

### 备注

MCP `execute_menu_item` + `read_console` 存在时序问题：执行菜单后 Console 始终返回 0 条，多次重试均失败。根因可能是 Unity domain reload 后 Console 清空，或 MCP 读取时序与 Unity 日志写入不同步。已通过 `manage_prefabs` 直接检查 Prefab 绕过此问题。

---

## ShipView Coordinator + Worker 拆分重构 — 2026-03-20 23:30

### 新建文件

- `Assets/Scripts/Ship/VFX/ShipBoostVisuals.cs` — Boost 态全部视觉反馈 Worker（液态精灵切换、辉光亮度包络、HL/Core alpha ramp、推进器脉冲、BoostTrailView 委托）
- `Assets/Scripts/Ship/VFX/ShipHitVisuals.cs` — 受击视觉反馈 Worker（白闪、i-frame 闪烁、Core 低血量警告脉冲）
- `Assets/Scripts/Ship/VFX/ShipDashVisuals.cs` — Dash 态视觉反馈 Worker（i-frame 闪烁、Dodge_Sprite 鬼影）

### 修改文件

- `Assets/Scripts/Ship/VFX/ShipView.cs` — 从 898 行单体重写为 ~170 行轻量级 Coordinator（持有 5 层 sprite 引用 + 3 个 worker 引用；捕获 baseline colors 并分发给 worker；订阅 ShipStateController.OnStateChanged + ShipHealth.OnDamageTaken 路由到 worker）
- `Assets/Scripts/Ship/Editor/ShipPrefabRebuilder.cs` — 新增 ShipBoostVisuals / ShipHitVisuals / ShipDashVisuals 的 EnsureComponent + 字段接线；ShipView 接线精简为 Coordinator 模式（5 层 renderer + 3 worker + juiceSettings）
- `Assets/Scripts/Ship/Editor/ShipBuilder.cs` — AddScriptComponents 新增 3 个 worker 组件；WireReferences 新增 3 个 worker 字段接线 + ShipView worker 引用接线
- `Assets/Scripts/Ship/Editor/ShipVfxValidator.cs` — ValidateShipViewWiring 重写为验证 Coordinator + Worker 架构（验证 3 个 worker 组件存在性 + ShipView worker 引用 + 各 worker 内部 renderer 接线）；移除旧的 ShipView fallback/legacy 代码审计检查

### 架构决策

- **Presentation Layer vs Feel Layer 分离**：VFX（视觉反馈）由 ShipView Coordinator + Worker 管理；Feel（非视觉反馈如顿帧、屏幕震动、倾斜变形）由 ShipVisualJuice 管理
- **Coordinator + Worker 模式**：ShipView 只做事件路由和 baseline color 分发，不包含任何 VFX 实现逻辑；Worker 不直接订阅事件，由 Coordinator 路由调用
- **baseline color 同步**：多个 worker 共享相同 SpriteRenderer 引用，通过 Initialize() 接收 baseline colors，确保各 worker 的颜色恢复互不冲突
- **无 fallback / legacy path**：所有旧 ShipView 内联逻辑已完全删除，不保留兼容路径

### 目的

- 单一职责：每个 Worker 只负责一种状态的视觉反馈
- 可维护性：新增 VFX 只需创建新 Worker + 在 Coordinator 中添加路由
- Editor 工具链统一更新：ShipPrefabRebuilder / ShipBuilder / ShipVfxValidator 全部同步支持新架构

---

## 模块归属决策规则 — 2026-03-20 22:45

### 修改文件

- `CLAUDE.md`
- `.trae/rules/claude.md`

### 内容

在「模块架构速写」章节的「触发条件」之前，新增「模块归属判断（先于触发条件）」小节。定义 4 步决策流程：检查核心模块表 → 检查命名空间归属 → 独立性测试 → 问用户。核心倾向为"宁可归入已有模块"。

### 目的

解决"收到新功能需求时，AI 无法自主判断该归入已有模块还是起新模块"的问题。之前规则体系定义了已有模块边界，但缺乏跨模块的归属决策标准。

### 技术

4 步瀑布式判断流程，偏向收敛（减少不必要的新模块 / 新文档），只有通过独立性测试才起新模块。

---

## 模块架构速写 (Module Architecture Brief) 规则植入 — 2026-03-20 22:30

### 修改文件

- `CLAUDE.md`
- `.trae/rules/claude.md`
- `Implement_rules.md`

### 内容

在 Feature 开发工作流中植入「模块架构速写」作为强制产出物，采用渐进式设计（Lv.1 → Lv.2 → 完整 Spec）。

**CLAUDE.md / .trae/rules/claude.md 改动：**
- 工作流从 8 步扩展为 9 步：第 4 步从「架构设计」改为「架构速写」（强制产出），新增第 8 步「更新架构文档」
- 追加「模块架构速写」完整章节：触发条件、Lv.1 最小版本模板、Lv.2 完整版本模板、产出与演进规则

**Implement_rules.md 改动：**
- Section 7 占位微调（说明不要求一开始补齐全部章节）
- 新增 Section 8「通用模块规则模板」：统一结构（6 节）、质量标准、与架构速写的联动矩阵

### 目的

解决"行为约束有但产出约束缺失"的问题——之前规则体系告诉 AI 怎么想、怎么做，但没定义必须产出什么文档。新规则确保每个新模块开发前都有最低限度的架构记录，避免 Ship/VFX 式的事后补文档。

### 技术

渐进式文档演进（Brief → Spec → CanonicalSpec + AssetRegistry），双文档互补（架构速写回答"是什么"，实现规则回答"怎么改"）。

---

## 开发哲学规则更新（Architecture-First 哲学） — 2026-03-20 18:00

**修改文件：**
- `CLAUDE.md`
- `.trae/rules/claude.md`

**内容：** 对项目规则文档做了 6 处同步修改，将开发哲学从"先 work 再 right 再 fast"转向"第一次就做对"：

1. **行为准则**：从"先让它 work"改为"以长期可维护的架构为基础，第一次就把架构做对"
2. **垂直切片（第3条）**：新增"切片内部必须架构正确——范围可以小，但职责划分不能妥协"
3. **迭代速度（第4条）**：数据驱动既为调参效率也为职责清晰，两者同等重要
4. **新增第7条"第一次就做对"**：架构决策编码前完成、一脚本一职责、一SO一关注点
5. **MVP 拆分**：MVP = 功能范围小但架构完整的版本，不是临时方案
6. **Feature 工作流**：从 7 步扩展为 8 步，新增"架构设计"步骤（第4步），验收标准须含架构质量项

**目的：** 项目已进入规模化阶段，返工成本远高于前期设计成本。确保所有 AI agent 在首次实现时就遵循正确的架构约束，而非事后补救。

**技术：** 文档规范更新，两份规则文件（CodeBuddy 用 `CLAUDE.md` + Trae 用 `.trae/rules/claude.md`）同步修改保持一致。

---

## Ship VFX 全层脚本覆盖 — 2026-03-20 12:00

### 修改文件

- `Assets/Scripts/Ship/VFX/ShipView.cs` — 大幅扩展
- `Assets/Scripts/Ship/Combat/ShipHealth.cs` — 重构
- `Assets/Scripts/Ship/Data/ShipJuiceSettingsSO.cs` — 扩展
- `Docs/Reference/ShipVFX_CanonicalSpec.md` — 更新

### 内容

#### P0: ShipHealth 受击 VFX 链路修复
- **问题**：`ShipHealth.Awake()` 用 `GetComponent<SpriteRenderer>()` 从 Ship 根节点获取 renderer，但所有 SpriteRenderer 都在子节点上（Back/Liquid/HL/Solid/Core），根节点本身没有 SpriteRenderer → 受击白闪和受击 i-frame 闪烁**完全静默失效**
- **方案**：将所有视觉反馈从 `ShipHealth` 迁移到 `ShipView`。`ShipHealth` 现在是纯游戏逻辑（HP 管理 + 无敌帧计时 + 击退），通过 `OnDamageTaken` 事件驱动 `ShipView` 执行多层联动白闪 + i-frame 闪烁
- **结果**：受击白闪覆盖全部 5 层（Back/Liquid/HL/Solid/Core），受击 i-frame 闪烁覆盖全部 5 层

#### P1: Ship_Sprite_HL 高亮层动态化
- **问题**：HL 层仅在 Awake 时设置 alpha=0.5，之后完全静态
- **方案**：HL 层响应 Boost（alpha 从 0.5 ramp 到 0.85）、Dash（参与 i-frame 闪烁）、受击（参与白闪 + i-frame blink）
- 参数通过 `ShipJuiceSettingsSO` 数据驱动

#### P2: Ship_Sprite_Core 核心层行为分配
- **问题**：Core 层有序列化引用但 546 行代码中从未使用
- **方案**：
  - **Boost 能量响应**：alpha 从 0.3 ramp 到 0.9（座舱能量涌动）
  - **低血量警告脉冲**：HP ≤ 30% 时 Core 层切换为红色脉冲（0.3~0.9 alpha 交替），给玩家持续的危险感
  - **Dash**：参与 i-frame 闪烁
  - **受击**：参与白闪 + i-frame blink
- 所有阈值和颜色通过 `ShipJuiceSettingsSO` 数据驱动

### 目的
- 实现 Ship VFX 的 100% 脚本覆盖率（从 90.5% → 100%）
- 修复一个自立项以来一直静默失效的 P0 bug（受击视觉反馈）
- 遵循"手感优先于功能"原则，让受击反馈从"无感知"变为"全船联动白闪"

### 技术
- 事件驱动解耦：`ShipHealth.OnDamageTaken` → `ShipView.HandleDamageTaken()`
- PrimeTween：HL/Core alpha ramp、Core 低血量脉冲
- UniTask：受击白闪 + i-frame 闪烁的异步循环
- 数据驱动：所有新参数在 `ShipJuiceSettingsSO` 中暴露

## StarChart UI Review — Bug修复 2026-02-28 22:30

### 新建/修改文件
- `Assets/Scripts/UI/UICanvasBuilder.cs` — 修复 `BuildTrackView()` 中 `labelTmp` 未定义的编译错误，替换为正确的局部变量名
- `Assets/Scripts/UI/DragDrop/FlyBackAnimator.cs` — 修复 `SkipAll()` 在迭代集合时调用 `Complete()` 触发 `OnComplete` 回调导致集合被修改的崩溃；改为先拷贝到临时列表再迭代
- `Assets/Scripts/UI/InventoryItemView.cs` — 为 `OnPointerEnter`、`OnPointerExit`、`OnBeginDrag`、`OnEndDrag` 中所有 `Tween.*` 调用补充 `useUnscaledTime: true`
- `Assets/Scripts/UI/DragDrop/DragDropManager.cs` — 修复 `CleanUp()` 直接赋值 `alpha = 1f` 覆盖 PrimeTween 动画的问题，改为仅当 source 不是 `InventoryItemView` 时才直接赋值；修复 Slot→空白区域拖拽静默失败，补充 StatusBar 提示；删除冗余字段 `_ghost`，统一使用 `_ghostView`
- `Assets/Scripts/UI/SlotCellView.cs` — 修复 `OnPointerExit()` 立即清除 `DropTargetTrack` 导致同轨道内移动时 Ghost 边框闪烁；改为按 TrackView 粒度管理，同一 TrackView 内移动不触发清除
- `Assets/Scripts/UI/TrackView.cs` — 在 `RefreshSailCell()` 和 `RefreshSatCells()` 开头添加 `_controller == null` 时的 `Debug.LogWarning` 警告

### 目的
修复 StarChart UI Phase A/B/C 全面 Review 中发现的 P0/P1/P2 级别 Bug 和代码质量问题，确保功能完整性、运行时稳定性和代码可维护性。

### 技术方案
- **FlyBackAnimator 崩溃**：`SkipAll()` 改为 `var copy = new List<Sequence>(_activeAnimations); _activeAnimations.Clear();` 后对 copy 逐一调用 `Complete()`，避免回调中修改正在迭代的集合
- **useUnscaledTime**：星图面板在 `Time.timeScale = 0` 时打开，所有 PrimeTween 动画必须加 `useUnscaledTime: true`，否则暂停状态下动画完全失效
- **CleanUp alpha 冲突**：通过 `source is InventoryItemView` 类型判断区分两种 source，`InventoryItemView` 的 alpha 恢复由其自身 `OnEndDrag` 的 PrimeTween 动画负责，`CleanUp()` 不干预
- **Slot→空白区域**：在 `EndDrag(true)` 且 `DropTargetValid == false` 时调用 `_statusBar.ShowMessage("No valid drop target")`，并触发 Ghost 飞回原位
- **Ghost 闪烁**：`OnPointerExit` 改为检查 `OwnerTrack` 是否与当前 `DropTargetTrack` 所属 TrackView 相同，仅在离开整个 TrackView 时才清除，消除同轨道内移动的闪烁帧
- **_ghost 冗余**：删除 `_ghost` 私有字段及 `Bind()` 中的赋值，全部替换为 `_ghostView`，减少维护歧义

---

## LevelDesigner Door 配置自动化

### 77. LevelScaffoldData Door 配置扩展 — 2026-02-16 18:00

**修改文件：**
- `Assets/Scripts/Level/Data/LevelScaffoldData.cs`

**内容：**
1. **新增 `ScaffoldDoorElementConfig` 类**：专门存储 Door 元素的配置
   - `InitialState`：门的初始状态（Open / Locked_Combat / Locked_Key 等）
   - `RequiredKeyID`：需要的钥匙 ID
   - `OpenDuringPhases`：开门阶段（Locked_Schedule 用）
2. **扩展 `ScaffoldElement` 类**：
   - 新增 `DoorConfig` 字段，仅在 Door 类型时使用
   - 新增 `BoundConnectionID` 字段，绑定到 ScaffoldDoorConnection
   - 新增 `EnsureDoorConfigExists()` 方法，确保配置对象存在
3. **扩展 `ScaffoldDoorConnection` 类**：
   - 新增 `ConnectionID` 字段（GUID），用于 Door 元素绑定
4. **所有新增字段都有 `[Tooltip]` 和 `[SerializeField]`，符合 C# 编码规范**

**目的：** 实现 Door 元素与 Room Connection 的数据绑定，为后续一键放置 Door 功能做准备。

---

### 78. LevelDesignerWindow 一键放置 Door — 2026-02-16 18:05

**修改文件：**
- `Assets/Scripts/Level/Editor/LevelDesignerWindow.cs`

**内容：**
1. **Connection 编辑器新增 Place Door 按钮**：
   - 当 Connection 已配置 TargetRoom 时，按钮可用
   - 点击后自动创建 Door 元素，绑定到该 Connection
   - Door 的位置自动设为 Connection 的 DoorPosition
   - Door 的旋转自动从 Connection 的 DoorDirection 计算
   - 自动初始化 DoorConfig
2. **Connection 编辑器新增 Remove Door 按钮**：
   - 当 Door 已放置时显示 ✓ 提示
   - 点击可以移除绑定的 Door 元素
3. **Door Position 同步功能**：
   - 当修改 Connection 的 DoorPosition 时，自动同步到绑定的 Door 元素
4. **Room Elements 模式 Door 配置面板**：
   - 选中 Door 元素后，显示 Door Configuration 区域
   - 可编辑 Initial State、Required Key ID、Open During Phases
5. **新增辅助方法**：
   - `FindBoundDoor()`：查找绑定到 Connection 的 Door 元素
   - `PlaceDoorForConnection()`：一键放置 Door
   - `SyncDoorPosition()`：同步 Door 位置

**目的：** 让 Door 配置变得超级简单，只需在 Connection 面板点一下按钮，所有 Door 配置（包括 TargetRoom、SpawnPoint、InitialState 等）都会自动生成。

**技术：** Unity IMGUI、Undo 系统、Scaffold 数据绑定、自动旋转计算（Vector2.SignedAngle）

---

### 79. LevelGenerator Door 自动配置 — 2026-02-16 18:10

**修改文件：**
- `Assets/Scripts/Level/Editor/LevelGenerator.cs`

**内容：**
1. **移除旧的 SetupDoorConnections 流程**：不再单独生成 Door，改为由元素系统统一处理
2. **更新 GenerateSingleElement 方法**：
   - 新增 `scaffold` 和 `roomMap` 参数
   - 当元素是 Door 且绑定了 Connection 时，自动调用 SetupDoorFromBinding
3. **新增 SetupDoorFromBinding 方法**：
   - 查找绑定的 ScaffoldDoorConnection
   - 自动配置 Door 组件的所有字段：
     - `_targetRoom`：从 Connection 的 TargetRoomID 查找
     - `_targetSpawnPoint`：调用 CreateSpawnPoint 自动生成
     - `_isLayerTransition`：继承自 Connection
     - `_initialState`：从 DoorConfig 读取
     - `_requiredKeyID`：从 DoorConfig 读取
     - `_openDuringPhases`：从 DoorConfig 读取
4. **新增 GetRoomID 辅助方法**：根据 Room 组件反向查找 RoomID
5. **更新 GenerateRoomElements 调用**：传入正确的参数

**目的：** 实现 Door 生成时的全自动配置，不需要任何手动操作，所有属性从 Scaffold 数据自动读取并赋值。

**技术：** Unity SerializedObject 访问私有字段、Undo 系统、Room 组件与 Scaffold 数据映射

---

### 80. RoomSO 下拉栏选择 — 2026-02-16 18:15

**修改文件：**
- `Assets/Scripts/Level/Editor/LevelDesignerWindow.cs`

**内容：**
1. **新增 `DrawRoomSOPopup` 方法**：
   - 使用 `AssetDatabase.FindAssets("t:RoomSO")` 自动检索所有 RoomSO 资源
   - 加载所有找到的 RoomSO 并收集它们的名字
   - 生成下拉选项列表，第一个选项是 `(None)`
2. **替换 `ObjectField` 为 `EditorGUILayout.Popup`**：
   - 从原来的拖拽赋值改为点击下拉选择
   - 选择后自动更新 ScaffoldRoom.RoomSO
   - 支持 Undo/Redo
3. **保持原有功能**：`(None)` 选项对应 null 值，与旧行为一致

**目的：** 让 RoomSO 选择更直观、更快捷，不需要再去 Project 窗口找文件拖过来。

**技术：** `AssetDatabase.FindAssets`、`AssetDatabase.LoadAssetAtPath`、`EditorGUILayout.Popup`、Undo 系统

---

### 81. 房间自动吸附对齐功能 — 2026-02-16 18:20

**修改文件：**
- `Assets/Scripts/Level/Editor/LevelDesignerWindow.cs`

**内容：**
1. **新增 `SnapToOtherRooms` 方法**：
   - `snapThreshold = 1f`：吸附阈值 1 个单位
   - 计算每个房间的四个边缘（左、右、上、下）
   - 当拖拽房间的边缘靠近其他房间的边缘时（小于阈值），自动吸附
   - 支持四个方向的吸附：右对左、左对右、下对上、上对下
2. **修改拖拽逻辑**：
   - 在 `MouseDrag` 事件中，先计算原始拖拽位置
   - 调用 `SnapToOtherRooms` 获得吸附后的位置
   - 将房间位置设为吸附后的位置
3. **保持流畅体验**：
   - 只在鼠标拖拽时应用吸附
   - 吸附后立即生效，不需要松开鼠标
   - Undo/Redo 正常工作

**目的：** 让房间对齐变得超级简单，不需要手动微调位置，靠近边缘会自动吸过去，方便建造房间网。

**技术：** 边缘碰撞检测、距离阈值判断、坐标自动对齐

---

### 82. Element 形状配置与 Q/W/E/R 变换工具 — 2026-02-16 18:30

**修改文件：**
- `Assets/Scripts/Level/Data/LevelScaffoldData.cs`
- `Assets/Scripts/Level/Editor/LevelDesignerWindow.cs`

**内容：**

#### 一、LevelScaffoldData.cs 修改
1. **新增 `ElementGizmoShape` 枚举**：
   - `Square`：正方形
   - `Circle`：圆形
   - `Diamond`：菱形
2. **扩展 `ScaffoldElement` 类**：
   - 新增 `_gizmoShape` 字段，默认值 `Square`
   - 新增 `GizmoShape` 公共属性
   - 新增 `SetDefaultGizmoShapeForType()` 方法，根据元素类型设置默认形状
     - Wall/WallCorner/CrateWooden/CrateMetal/Door → Square
     - Checkpoint/PlayerSpawn/EnemySpawn/Hazard → Circle
3. **修改 `ElementType` setter**：设置类型时自动调用 `SetDefaultGizmoShapeForType()`

#### 二、LevelDesignerWindow.cs 修改
1. **新增 `ElementTransformTool` 枚举**：
   - `Select`：选择模式
   - `Move`：移动模式
   - `Rotate`：旋转模式
   - `Scale`：缩放模式
2. **新增工具条 UI**：
   - 在 Room Elements 面板顶部添加 Q/W/E/R 四个按钮的 Toolbar
3. **新增快捷键支持**：
   - `Q` → Select
   - `W` → Move
   - `E` → Rotate
   - `R` → Scale
4. **修改 `DrawRoomCanvasElements` 方法**：
   - 根据 `GizmoShape` 绘制不同形状
   - Square：`Handles.DrawSolidRectangleWithOutline`
   - Circle：`Handles.DrawSolidDisc`
   - Diamond：`Handles.DrawSolidPolygon` 绘制四边形
5. **新增 `DrawElementTransformGizmo` 方法**：
   - Move 模式：绘制红/绿方向箭头
   - Rotate 模式：绘制圆环
   - Scale 模式：绘制红/绿方向手柄
6. **修改 `HandleRoomCanvasInput` 方法**：
   - 支持快捷键切换工具
   - 拖拽时根据当前工具执行不同操作
   - Move：直接修改 LocalPosition
   - Rotate：通过鼠标与元素中心的夹角计算旋转
   - Scale：通过鼠标位移计算缩放，最小 0.1 防止负数
7. **Element Properties 面板新增 Gizmo Shape 下拉栏**：
   - 可以随时手动切换元素的显示形状
8. **修改 `AddElementToRoom` 方法**：
   - 新增元素时调用 `SetDefaultGizmoShapeForType()`

**目的：**
- 让不同类型的元素默认用合适的形状显示（Wall 是方的，Spawn 是圆的）
- 提供和 Unity Editor 一样的变换工具体验（Q/W/E/R 快捷键）
- 可以直观地拖拽移动、旋转、缩放元素

**技术：** Unity IMGUI、Handles 绘制、Keyboard Input、Undo 系统、向量夹角计算（Vector2.SignedAngle）

---

### 83. 房间吸附功能优化 — 2026-02-16 18:45

**修改文件：**
- `Assets/Scripts/Level/Editor/LevelDesignerWindow.cs`

**内容：**
1. **增大默认吸附阈值**：从 1 个单位 → 3 个单位
2. **添加可配置参数**：新增 `_roomSnapThreshold` 字段，默认值 3f
3. **UI 面板新增设置**：在 Settings 面板添加 "Room Snap Settings" 部分
   - 滑块：可在 0.5f 到 10f 之间调整阈值
   - HelpBox：说明 "Higher = easier to snap"
4. **修改吸附逻辑**：`SnapToOtherRooms` 方法使用 `_roomSnapThreshold` 替代硬编码值

**目的：**
- 原来 1 个单位的阈值太小，在 Topology View 中只有 10 像素，很难感受到
- 现在默认 3 个单位（30 像素），吸附更明显
- 用户可以根据自己的喜好调整阈值

**技术：** Unity IMGUI、EditorGUILayout.Slider、Mathf.Abs 距离检测

---

### 73. Level Architect Tool — 完整实现（Task 1-10）— 2026-02-16 10:56

**新建文件：**

| 文件路径 | 操作 | 简述 |
|---|---|---|
| `Assets/Scripts/Level/Editor/LevelArchitect/LevelArchitectWindow.cs` | 新建 | 主入口EditorWindow，管理工具模式(Select/Blockout/Connect)、SceneView工具栏、侧边面板、各子系统集成 |
| `Assets/Scripts/Level/Editor/LevelArchitect/RoomBlockoutRenderer.cs` | 新建 | 房间白膜渲染（颜色编码矩形）、鼠标交互（选择/拖拽/吸附/框选）、门图标和连接线、悬停信息浮窗 |
| `Assets/Scripts/Level/Data/RoomPresetSO.cs` | 新建 | ScriptableObject房间预设模板（名称/尺寸/房间类型/SpawnPoint数量/ArenaController配置） |
| `Assets/Scripts/Level/Editor/LevelArchitect/RoomFactory.cs` | 新建 | 从预设创建完整Room GameObject、自动创建子对象（Confiner/SpawnPoints/Spawner）、自动生成RoomSO资产 |
| `Assets/Scripts/Level/Editor/LevelArchitect/DoorWiringService.cs` | 新建 | 自动检测房间共享边缘、创建双向门连接对、SceneView连接模式交互、门位置更新 |
| `Assets/Scripts/Level/Editor/LevelArchitect/ScaffoldSceneBinder.cs` | 新建 | 维护ScaffoldRoom到场景Room的映射、双向同步Scene↔Scaffold变化 |
| `Assets/Scripts/Level/Editor/LevelArchitect/BlockoutModeHandler.cs` | 新建 | 矩形和走廊笔刷工具、拖拽绘制预览、链式绘制(Shift)、Quick Play、内置预设创建 |
| `Assets/Scripts/Level/Editor/LevelArchitect/LevelValidator.cs` | 新建 | 8项验证规则、自动修复、轻量级SceneView叠加检查、Error/Warning/Info分级 |
| `Assets/Scripts/Level/Editor/LevelArchitect/BatchEditPanel.cs` | 新建 | 多选房间批量属性编辑、右键上下文菜单、Copy/Paste配置、批量RoomType/FloorLevel/Size调整 |
| `Assets/Scripts/Level/Editor/LevelArchitect/PacingOverlayRenderer.cs` | 新建 | Pacing Overlay（战斗强度色阶）、Critical Path（BFS最短路径）、Lock-Key Graph（锁钥依赖） |
| `Assets/Scripts/Level/Editor/LevelArchitect/SceneScanner.cs` | 新建 | 反向扫描场景Room/Door构建LevelScaffoldData、自动创建缺失RoomSO |

**修改文件：**

| 文件路径 | 操作 | 简述 |
|---|---|---|
| `Assets/Scripts/Level/Editor/RoomBatchEditor.cs` | 修改 | 添加[Obsolete]标记 |
| `Assets/Scripts/Level/Editor/LevelDesignerWindow.cs` | 修改 | 添加[Obsolete]标记 |
| `Assets/Scripts/Level/Editor/ShebaLevelScaffolder.cs` | 修改 | 添加[Obsolete]标记 |

**内容简述：**
全新的统一关卡编辑工具，替代原有3个分散工具（RoomBatchEditor/LevelDesignerWindow/ShebaLevelScaffolder）。10个模块：
1. **基础框架** — EditorWindow + SceneView.duringSceneGui集成，模式切换工具栏
2. **白膜渲染** — 颜色编码房间矩形、选择/拖拽/吸附/框选交互
3. **预设系统** — RoomPresetSO + RoomFactory一键创建标准化房间
4. **智能门连接** — 共享边检测、双向Door自动创建、SceneView拖线连接
5. **双向同步** — ScaffoldData ↔ Scene Room实时同步
6. **白膜搭建** — 矩形/走廊笔刷、链式绘制、Quick Play
7. **验证修复** — 8项规则检查、Auto-Fix、SceneView红色警告叠加
8. **批量编辑** — 多选属性修改、右键菜单、Copy/Paste配置
9. **节奏可视化** — 战斗强度色阶、BFS关键路径、锁钥依赖图
10. **场景扫描** — 反向导入已有场景、旧工具[Obsolete]标记

**目的：** 提供Scene-View-First的关卡设计体验，让策划能在几分钟内完成关卡白膜搭建、自动配置门连接、一键验证修复错误、直观查看关卡节奏，大幅提升关卡制作效率。

**技术：** EditorWindow/SceneView集成、Handles绘制系统、SerializedObject批量编辑、BFS图搜索、ScriptableObject数据驱动、Undo撤销支持、AssetDatabase资产管理。

---

---

## 飞船操作手感优化

### 76. 飞船操作手感全面优化 — 2026-02-16 17:45

**修改文件：**
- `Assets/Scripts/Ship/Data/ShipStatsSO.cs`
- `Assets/Scripts/Ship/Movement/ShipMotor.cs`
- `Assets/_Data/Ship/DefaultShipStats.asset`

**内容：**

### 一、参数调整（立即生效）

| 参数 | 原值 | 新值 | 目的 |
|------|------|------|------|
| `MoveSpeed` | 12 | 14 | 整体速度更快，更有爽快感 |
| `Acceleration` | 45 | 70 | 加速更快，响应更直接 |
| `Deceleration` | 25 | 55 | 减速更快，停止更干脆 |
| `SharpTurnAngleThreshold` | 90° | 120° | 放宽转向惩罚触发条件 |
| `SharpTurnSpeedPenalty` | 0.7 | 0.9 | 减少转向速度惩罚 |
| `InitialBoostMultiplier` | 1.5 | 2.2 | 初始爆发更强 |
| `InitialBoostDuration` | 0.05s | 0.12s | 初始爆发持续更长 |

### 二、新增参数（更精细控制）

1. **`DirectionChangeSnappiness` (0.65)**
   - 范围：0.0 ~ 1.0
   - 0 = 非常滑（保持原方向）
   - 1 = 立即转向（无惯性）
   - 0.65 = 平衡值，有惯性但可控

2. **`MinSpeedForDirectionChange` (0.5)**
   - 最低速度阈值，低于此速度时不应用方向混合
   - 防止低速时抖动

### 三、曲线优化

**加速度曲线**：
- 起始点 (0, 1.0) = 初始响应快
- 中点 (0.3, 1.2) = 中期加速快
- 终点 (1.0, 0.6) = 接近最高速时平缓

**减速曲线**：
- 起始点 (0, 1.3) = 松开摇杆立即快速减速
- 中点 (0.4, 1.0) = 中期减速平稳
- 终点 (1.0, 0.7) = 低速时自然停止

### 四、代码改进

在 `ShipMotor.HandleMovement()` 中新增：
- 方向变化平滑混合（Lerp）
- 基于 `DirectionChangeSnappiness` 的可控转向

---

---

## Boost Trail VFX 移除全屏白色 Flash Canvas  2026-03-11 15:32

### 修改文件

- `Assets/Scripts/Ship/VFX/BoostTrailView.cs`  删除全屏 `Image` 引用、flash tween 与 `TriggerFlash()` 逻辑，仅保留局部 halo 与 bloom burst
- `Assets/Scripts/Ship/Editor/ShipBoostTrailSceneBinder.cs`  停止创建/绑定 `BoostTrailFlashOverlay`，并在重跑工具时自动删除场景中的旧 overlay
- `Assets/Scripts/Ship/Editor/ShipBoostDebugMenu.cs`  删除 `Show Flash Overlay` / `Hide Flash Overlay` 调试菜单
- `Assets/Scripts/Ship/Editor/BoostTrailPrefabCreator.cs`  更新说明文字，移除对 `_flashImage` 的场景接线提示
- `Assets/Scenes/SampleScene.unity`  重新执行 scene binder 后保存，清除旧的 `BoostTrailFlashOverlay`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容

结合参考文档和前几轮实测可以确认：虽然文档里曾保留一个 `Screen Flash` 层作为补充，但在当前项目实现中，这个全屏白色 Canvas 的主观存在感过强，已经明显偏离“GG 式局部小圆能量爆发”的目标。为避免它继续污染判断，这一轮将全屏 flash 从运行时主链路中彻底移除，不再只是把 alpha 压低，而是直接删掉 `BoostTrailView` 中的 flash 触发逻辑，并让 `ShipBoostTrailSceneBinder` 在后续任何一次执行时都主动清理旧的 `BoostTrailFlashOverlay` 场景对象。实际验证结果是：场景中已经查询不到 `BoostTrailFlashOverlay`，`BoostTrailView` 组件上也不再存在 `_flashImage` 相关序列化字段，Boost 激活视觉正式收敛为“局部 halo + bloom”两层。

### 目的

把 Boost 激活的视觉重心彻底锁回飞船附近，避免再出现“明明主要在看局部能量爆发，却总被一层全屏白闪打断”的违和感。这样后续继续对齐 GG 时，关注点就能集中在 halo 资源、shader 表现和局部 bloom 上，而不是不断被一个方向已经判定错误的全屏层干扰。

### 技术

1. 在 `BoostTrailView` 中删除 `_flashImage`、`_flashTween` 与 `TriggerFlash()`，从代码层彻底移除全屏 flash 运行时逻辑
2. 在 `ShipBoostTrailSceneBinder` 中把“创建 overlay Canvas/Image”改为“发现旧 overlay 就删除”，避免一键工具再次把白色全屏层加回来
3. 删除 `ShipBoostDebugMenu` 中的 flash 调试入口，避免误导后续验证流程
4. 用 Unity MCP 查询确认：
   - `BoostTrailFlashOverlay` 当前场景中 `totalCount = 0`
   - `BoostTrailView` 当前仅保留 `_boostBloomVolume`，不再暴露 `_flashImage`

---

---

## Bug Fix: InventoryView._itemPrefab 为 null 导致星图部件不显示 - 2026-03-01 13:49

### 修改文件
- `Assets/Scenes/SampleScene.unity`（修复 InventoryView 组件序列化字段）

### 内容简述
星图面板底部 Inventory 区域完全空白，无法显示任何部件卡片，尽管 `INVENTORY 21 ITEMS` 状态栏显示数据已正确加载。

### 根本原因
`InventoryView` 组件的 `_itemPrefab` 字段在场景中序列化为 `{fileID: 0}`（null）。`Refresh()` 方法有守卫逻辑：`if (_inventory == null || _itemPrefab == null || _contentParent == null) return;`，导致静默跳过所有渲染，不实例化任何 `InventoryItemView` 卡片。

### 技术方案
直接编辑 `SampleScene.unity` 场景文件，将 `InventoryView` 组件的 `_itemPrefab` 字段从 `{fileID: 0}` 修复为 `InventoryItemView.prefab` 的正确引用（guid: `1761a57fd23df4a5bbbff1b7c05cd6f3`）。

---

---

## Level Phase 4-5: Map / Save / Multi-Floor / Enhanced Transitions

**实施时间：** 当前会话
**阶段：** Level Phase 4 (Map & Exploration) + Phase 5 (Multi-Floor Structure)
**前置依赖：** Phase 1-3 (L1-L12) 全部完成，架构基建大修完成

---

### L13A: MinimapManager + MapRoomData + LevelEvents Extensions

**文件：**

| 路径 | 操作 | 用途 |
|------|------|------|
| `Assets/Scripts/Core/LevelEvents.cs` | 修改 | 新增 `OnRoomFirstVisit(string)` 和 `OnFloorChanged(int)` 事件 |
| `Assets/Scripts/Level/Map/MapRoomData.cs` | 新建 | 轻量房间地图数据结构体（RoomID、世界坐标、楼层、类型、访问状态） |
| `Assets/Scripts/Level/Map/MapConnection.cs` | 新建 | 房间连接结构体（FromRoomID、ToRoomID、中点、是否层间） |
| `Assets/Scripts/Level/Map/MinimapManager.cs` | 新建 | ServiceLocator 注册的地图数据管理器。场景初始化时收集所有 Room，通过 Door 引用构建邻接图，跟踪已访问房间集合。提供 API：GetRoomNodes/GetConnections/IsVisited/MarkVisited/CurrentFloor/GetFloorsDiscovered |
| `Assets/Scripts/Level/ProjectArk.Level.asmdef` | 修改 | 添加 `Unity.TextMeshPro` 和 `Unity.InputSystem` 引用 |

**技术要点：**
- MinimapManager 在 `Start()` 中通过 `FindObjectsByType<Room>` 收集场景数据
- 邻接图通过 Door.TargetRoom 引用构建，使用 HashSet 去重双向连接
- 楼层变化检测由 MinimapManager.HandleRoomChanged 驱动，触发 LevelEvents.OnFloorChanged
- 已访问房间从 SaveManager 加载/保存

---

### L13B: MapPanel + MapRoomWidget + MapConnectionLine + Input Action

**文件：**

| 路径 | 操作 | 用途 |
|------|------|------|
| `Assets/Scripts/Level/Map/MapRoomWidget.cs` | 新建 | 单个房间节点 UI 组件。根据 RoomType 着色（Normal/Arena/Boss/Safe），显示图标覆盖、当前高亮环、未访问迷雾 |
| `Assets/Scripts/Level/Map/MapConnectionLine.cs` | 新建 | 房间连接线 UI。使用拉伸 Image 绘制线段，支持层间连接差异化样式 |
| `Assets/Scripts/Level/Map/MapPanel.cs` | 新建 | 全屏地图面板。M 键（ToggleMap action）切换，ScrollRect 支持平移，滚轮缩放，楼层 Tab 切换，玩家图标定位 |
| `Assets/Input/ShipActions.inputactions` | 修改 | Ship map 新增 ToggleMap Button action，绑定 Keyboard/M 和 Gamepad/select |

**技术要点：**
- MapPanel 独立处理输入（不通过 UIManager），因为地图不需要暂停游戏
- 世界坐标到地图坐标使用可配置 _worldToMapScale 缩放因子
- 连接线通过 `transform.SetAsFirstSibling()` 渲染在房间节点下方
- 楼层 Tab 使用闭包捕获 floor index 绑定 onClick

---

### L13C: MinimapHUD Corner Overlay

**文件：**

| 路径 | 操作 | 用途 |
|------|------|------|
| `Assets/Scripts/Level/Map/MinimapHUD.cs` | 新建 | 屏幕角落小地图叠加层。以当前房间为中心显示可见半径内的房间，自动跟随房间切换刷新，显示楼层标签 |

**技术要点：**
- 使用可配置 `_visibleRadius`（世界单位）过滤远距离房间
- 每次进入新房间时完全重建（Rebuild），因为小地图元素数量少，性能开销可接受
- 订阅 `LevelEvents.OnRoomEntered` 和 `LevelEvents.OnFloorChanged` 触发刷新

---

### L14: SaveBridge + Save System Integration

**文件：**

| 路径 | 操作 | 用途 |
|------|------|------|
| `Assets/Scripts/Level/SaveBridge.cs` | 新建 | 集中式存档收集器。从 MinimapManager/KeyInventory/WorldProgressManager/ShipHealth/CheckpointManager 收集数据调用 SaveManager.Save；加载时反向分发到各子系统 |
| `Assets/Scripts/Level/Checkpoint/CheckpointManager.cs` | 修改 | `SaveProgress` 方法委托给 SaveBridge.SaveAll()，保留 fallback 直接存档 |
| `Assets/Scripts/Level/GameFlow/GameFlowManager.cs` | 修改 | `Start()` 中调用 SaveBridge.LoadAll() 加载存档；死亡重生存档委托给 SaveBridge |
| `Assets/Scripts/Level/Progression/WorldProgressManager.cs` | 修改 | `SaveProgress` 方法委托给 SaveBridge.SaveAll()，保留 fallback |
| `Assets/Scripts/Level/Room/RoomManager.cs` | 修改 | `EnterRoom()` 中首次访问时调用 MinimapManager.MarkVisited()；新增 `CurrentFloor` 便利属性 |

**技术要点：**
- SaveBridge 注册到 ServiceLocator，各子系统通过 `ServiceLocator.Get<SaveBridge>()` 调用
- 所有修改保留 fallback 逻辑（SaveBridge 不存在时回退到直接存档），确保向后兼容
- SaveBridge.LoadAll() 在 GameFlowManager.Start() 中最先执行，确保子系统在激活前获得存档数据

---

### L15: ShebaLevelScaffolder Editor Tool

**文件：**

| 路径 | 操作 | 用途 |
|------|------|------|
| `Assets/Scripts/Level/Editor/ProjectArk.Level.Editor.asmdef` | 新建 | Editor-only 程序集定义，includePlatforms = ["Editor"] |
| `Assets/Scripts/Level/Editor/ShebaLevelScaffolder.cs` | 新建 | 菜单 `ProjectArk > Scaffold Sheba Level`。创建 12 个 Room GameObject（含 BoxCollider2D、PolygonCollider2D confiner、Room 组件、SpawnPoints），匹配的 RoomSO 和 EncounterSO 资产，双向 Door 连接 |

**技术要点：**
- 12 间房间布局：Entrance → Hub → 双分支走廊 → Arena×2 → Key Chamber → Rest Station → Boss Antechamber → Boss + Underground 层
- 使用 SerializedObject 设置私有字段（遵循不直接修改 YAML 的原则）
- EncounterSO 自动分配不同的波次配置（Patrol_Light/Mixed_Medium/Arena_Heavy/Boss）
- 双向门连接：每个 DoorDef 同时在源和目标房间创建对应的 Door + SpawnPoint
- 执行完毕后在 Console 输出 10 步验收清单

---

### L16+L18: Floor Awareness + Minimap Floor Switching

**文件：** 已在 L13A-C 中前置实现

- `MinimapManager.CurrentFloor` / `GetFloorsDiscovered()` / `GetRoomNodes(int floor)` — L13A
- `RoomManager.CurrentFloor` 便利属性 — L14
- `LevelEvents.OnFloorChanged(int)` — L13A（由 MinimapManager 触发）
- `MapPanel` 楼层 Tab 栏 — L13B
- `MinimapHUD` 楼层标签 + 自动切换 — L13C

---

### L17: Enhanced Layer Transition

**文件：**

| 路径 | 操作 | 用途 |
|------|------|------|
| `Assets/Scripts/Level/Data/RoomSO.cs` | 修改 | 新增 `_ambientMusic` AudioClip 字段（per-room BGM 覆盖） |
| `Assets/Scripts/Level/Room/DoorTransitionController.cs` | 修改 | 新增层间转场增强效果：粒子特效、SFX、BGM crossfade、相机 zoom-out/snap-back |

**技术要点：**
- 粒子方向基于 currentFloor vs targetFloor 判断上升/下降
- 相机 zoom 使用 PrimeTween.Custom 动画 orthographicSize
- BGM crossfade 通过 AudioManager.PlayMusic(clip, fadeDuration) 实现
- 所有新增字段标记为 Optional（[SerializeField] 可空），不影响现有非层间转场

---

### L19: NarrativeFallTrigger Placeholder

**文件：**

| 路径 | 操作 | 用途 |
|------|------|------|
| `Assets/Scripts/Level/Narrative/NarrativeFallTrigger.cs` | 新建 | 叙事坠落触发器占位符。含 SerializeField（TargetRoom、LandingPoint、ParticlePrefab、Timeline、SFX、相机参数），TriggerFall() 方法含 9 步 TODO 注释描述完整实现。当前仅执行最小功能：传送 + 换房间 |

**技术要点：**
- 严格遵循计划：仅占位，不实现完整功能
- TriggerFall() 的 TODO 注释列出完整 9 步实现方案（供未来叙事过场开发参考）
- 支持 PlayableAsset（Timeline）引用，为未来 Timeline 集成预留接口
- ResetTrigger() 方法用于死亡/重生后重置触发器状态

---

---

## 关卡设计：示巴星 ACT1+ACT2 银河恶魔城式关卡重构

### 示巴星 ACT1+ACT2 关卡布局全面重构 — 2026-02-26 13:46

**修改文件：**
- `Docs/LevelDesigns/Sheba_ACT1_ACT2.json` — 完全重写

**内容简述：**
将原线性"一本道"关卡（28个房间，全east→west连接）重构为银河恶魔城式网状拓扑布局（38个房间）。覆盖ACT1 Z1a~Z1f + ACT2 Z2a~Z2d共10个心流节点，严格对齐CSV张力曲线4→2→7→4→3→6→5→3→5→9。

**关键设计特性：**
1. **网状拓扑**：多路径分支（Z1a起点提供东向+南向两条路径）、分支汇聚点（Z1e+Z1f双路汇入）
2. **垂直层级**：3个floor层（-1地下/0地面/1上层），Z1b隐藏地穴(floor=-1)、Z1c上层观景台(floor=1)、Z1d音叉林上层(floor=1)、Z2a上层平台(floor=1)
3. **3个环路**：Z1c侧路↔Z1b地穴、Z1d花园↔Z1f下行通道、Z2c回音路↔世界时钟预兆
4. **2条单向捷径**：Z1d悬崖→Z1b可选角落（跌落）、Z2b隐藏跌落→Z2c深谷入口（跌落）
5. **4种房间类型**：safe(16)、normal(18)、arena(3)、boss(1)
6. **6种元素类型**：spawn(1)、checkpoint(13)、enemy(39)、chest(23)、npc(13)、door(4)
7. **84条连接**（大部分双向A↔B）+ **5个doorLinks**
8. **Boss竞技场**：20×14（满足≥18×12要求）

**拓扑概念：**
```
ACT1:
Z1a(坠机点)→东→Z1b(冰雕广场)→东→Z1c(觉醒战场)→东→Z1d(音叉林)→花园
     ↓南                  ↕地穴    ↕侧路↔地穴(环1)     ↕上层  ↓悬崖→Z1b(捷径1)
Z1e(战后余波)→东→Z1f(下行通道)→首棱镜→缕拉目击→ACT1出口
                  ↑ Z1d花园直连(环2)

ACT2:
Z2a(管风琴走廊)→Z2a缕拉援助→↓南→Z2b(声之走廊)→涟漪奖励→缕拉面对面
     ↕上层平台+秘密                   ↓南               ↓隐藏跌落→Z2c(捷径2)
                        Z2c(深谷入口)→↓南→Z2c战斗→Z2c回音路
                             ↕世界时钟←→↕回音路(环3)→频率标记→Z2d前室→Boss→奖励→出口
```

**目的：**
从线性走廊改为银河恶魔城式网状探索，让玩家体验分支选择、垂直层叠、捷径回路和延迟回报。保持CSV心流节奏不变。

**技术方案：**
- JSON格式完全对齐LevelDesigner.html的getExportData()输出：rooms(id/name/type/floor/position/size/elements) + connections(from/to/fromDir/toDir) + doorLinks(roomId/entryDir/doorIndex)
- 双向连接：每条通道2条connection（A→B + B→A）
- 单向连接：捷径/跌落仅1条connection（A→B）
- comment字段用于人类可读的区域分隔标注（导入时被忽略但保留在JSON中）
- 位置坐标使用上下左右四方向展开，非线性排列

---

---

## Bug 修复：PrimeTween 包解析 + CS0246 + CS0117 + CS4014 + CanvasGroup — 2025-06-XX

### 修改文件

1. `Packages/manifest.json`
   - PrimeTween 从失效的 git URL (`nicktmv/PrimeTween.git`) 改为 NPM scoped registry (`registry.npmjs.org`, 版本 `1.3.0`)
   - 新增 `scopedRegistries` 配置块

2. `Assets/Scripts/Combat/StarChart/IStarChartItemResolver.cs` *(新建)*
   - 接口定义 `FindCore`, `FindPrism`, `FindLightSail`, `FindSatellite`
   - 解决 Combat 程序集无法引用 UI 程序集中 `StarChartInventorySO` 的循环依赖

3. `Assets/Scripts/Combat/StarChart/StarChartController.cs`
   - `ImportFromSaveData` 和 `ImportTrack` 参数从 `StarChartInventorySO` 改为 `IStarChartItemResolver`

4. `Assets/Scripts/UI/StarChartInventorySO.cs`
   - 实现 `IStarChartItemResolver` 接口

5. `Assets/Scripts/Combat/Tests/SnapshotBuilderTests.cs`
   - `PrismFamily.Stat`（不存在）→ `PrismFamily.Rheology`（正确枚举值）

6. `Assets/Scripts/UI/WeavingStateTransition.cs`
   - PrimeTween `Tween.Custom` 返回值添加 `_ =` 显式丢弃，消除 CS4014 警告

7. `Assets/Scripts/UI/Editor/UICanvasBuilder.cs`
   - `CreateInventoryItemViewPrefab()` 在 `AddComponent<InventoryItemView>()` 前显式添加 `CanvasGroup`

### 目的
修复架构大修后的编译错误和运行时警告：包解析失败、跨程序集循环依赖、枚举值不存在、异步返回值未处理、Prefab 缺少必需组件。

**技术**：NPM scoped registry、依赖反转原则 (DIP)、显式 discard (`_ =`)、`[RequireComponent]` 预防。

---

---

## 修复与优化

### 72. RoomBatchEditor 过时API修复 — 2026-02-16 17:30

**修改文件：**
- `Assets/Scripts/Level/Editor/RoomBatchEditor.cs`

**内容：** 修复过时API警告 CS0618：
- 将 `FindObjectsOfType<Room>()` 替换为 `FindObjectsByType<Room>(FindObjectsInactive.Include, FindObjectsSortMode.None)`
- Unity 6.0+ 推荐使用新的 `FindObjectsByType` API 替代已弃用的 `FindObjectsOfType`

**目的：** 消除编译警告，使用Unity推荐的最新API，确保代码的长期可维护性。

**技术：** `Object.FindObjectsByType`, `FindObjectsInactive`, `FindObjectsSortMode`

---

### 73. LevelDesignerWindow GUI布局错误修复 — 2026-02-16 17:32

**修改文件：**
- `Assets/Scripts/Level/Editor/LevelDesignerWindow.cs`

**内容：**
1. **修复CS1061错误**：`GUI.BeginScrollView()` 直接返回 Rect，不需要访问 `.position` 属性
2. **修复CS0117错误**：RoomType 枚举值不匹配，移除了不存在的 `Treasure`, `Rest`, `Entrance`, `Exit`，保留 `Normal`, `Arena`, `Boss`, `Safe`
3. **修复CS0649警告**：`_topologyScrollPosition` 正确赋值

**目的：** 解决编译错误，使LevelDesignerWindow能够正常编译和运行。

---

### 74. LevelDesignerWindow GUI布局优化 — 2026-02-16 17:35

**修改文件：**
- `Assets/Scripts/Level/Editor/LevelDesignerWindow.cs`

**内容：**
1. **修复GetLastRect错误**：移除了在布局组刚开始时调用 `GUILayoutUtility.GetLastRect()` 的代码，改用 `GUIStyle` 的 `normal.background` 属性设置背景颜色
2. **新增MakeTex辅助方法**：创建纯色纹理用于GUI背景样式
3. **房间初始位置优化**：第一个房间从 (0, 0, 0) 开始，后续房间按 3×3 网格自动排列
4. **新增Focus All Rooms按钮**：一键聚焦到所有房间
5. **空房间提示**：当没有房间时显示提示文字
6. **Reset View修复**：正确重置滚动位置

**目的：** 修复Unity GUI布局错误 "You cannot call GetLast immediately after beginning a group"，并优化用户体验，让房间更容易被看到和操作。

**技术：** `GUIStyle`, `Texture2D`, `EditorGUILayout.VerticalScope`, Unity IMGUI布局系统

---

### 75. LevelDesignerWindow 拓扑视图缩放修复 — 2026-02-16 17:40

**修改文件：**
- `Assets/Scripts/Level/Editor/LevelDesignerWindow.cs`

**内容：**
1. **问题分析**：房间尺寸太小（20×15 像素），在拓扑视图中几乎看不见
2. **添加拓扑缩放系数**：引入 `topologyScale = 10f`，将世界坐标单位放大 10 倍以像素显示
3. **更新绘制逻辑**：`roomWidth = Size.x * topologyScale * zoomLevel`
4. **更新拖拽逻辑**：鼠标位置判断和拖拽 delta 计算都使用相同的 `topologyScale`

**目的：** 让房间在拓扑视图中可见且易于操作，修复"按下 Add Room 后看不到房间"的问题。

**技术：** 坐标缩放转换、Unity Handles 绘制、鼠标事件处理

---

---

## 可视化关卡编辑器（Visual Level Designer）

### 68. LevelElementLibrary — 关卡元素库 — 2026-02-15

**新建文件：**
- `Assets/Scripts/Level/Data/LevelElementLibrary.cs`

**内容：** ScriptableObject 数据容器，定义关卡设计时可使用的所有元素Prefab：
- Room Elements: `_roomTemplate`（房间模板Prefab）
- Wall Elements: `_wallBasic`（基础墙）、`_wallCorner`（墙角）
- Prop Elements: `_crateWooden`（木箱）、`_crateMetal`（金属箱）
- Door Elements: `_doorBasic`（基础门）
- Checkpoint Elements: `_checkpoint`（存档点）
- Spawn Points: `_playerSpawnPoint`（玩家出生点）、`_enemySpawnPoint`（敌人出生点）

**目的：** 统一管理所有可放置的关卡元素Prefab，让LevelDesignerWindow可以动态获取元素库。

**技术：** ScriptableObject, `[CreateAssetMenu]`, 数据驱动模式。

---

### 69. LevelScaffoldData — 关卡脚手架数据 — 2026-02-15

**新建文件：**
- `Assets/Scripts/Level/Data/LevelScaffoldData.cs`

**内容：**
- **LevelScaffoldData**：ScriptableObject，包含：
  - `_levelName`：关卡名称
  - `_floorLevel`：楼层级别
  - `_rooms`：List<ScaffoldRoom> 房间列表
  
- **ScaffoldRoom**：Serializable类，单个房间数据：
  - `_roomID`：唯一ID（Guid生成）
  - `_displayName`：显示名称
  - `_roomType`：房间类型（RoomType枚举）
  - `_position`：世界坐标位置
  - `_size`：房间尺寸（宽×高）
  - `_connections`：List<ScaffoldDoorConnection> 门连接列表
  - `_roomSO`：关联的RoomSO（可选，自动生成）
  
- **ScaffoldDoorConnection**：Serializable类，门连接数据：
  - `_targetRoomID`：目标房间ID
  - `_doorPosition`：门在房间内的位置
  - `_doorDirection`：门朝向（指向目标房间）
  - `_isLayerTransition`：是否为层过渡（更长的淡入淡出）

**目的：** 将关卡设计数据与Unity场景分离，支持保存/加载，为可视化编辑器提供数据结构。

**技术：** ScriptableObject, `[Serializable]` 类, Guid唯一ID生成, 数据驱动设计。

---

### 70. LevelGenerator — 关卡生成器 — 2026-02-15

**新建文件：**
- `Assets/Scripts/Level/Editor/LevelGenerator.cs`

**内容：** 静态工具类，从LevelScaffoldData生成Unity场景对象：
1. **GenerateLevel()**：主入口，接收scaffold、elementLibrary、可选parentTransform
2. **CreateLevelRoot()**：创建关卡根GameObject（`--- {LevelName} ---`格式）
3. **GenerateRooms()**：遍历scaffold.Rooms生成所有Room对象
4. **GenerateSingleRoom()**：生成单个Room对象，设置位置、大小、Collider
5. **SetupRoomCollider()**：配置BoxCollider2D为Trigger，设置size和offset
6. **SetupRoomData()**：配置Room组件的_data引用，自动创建RoomSO（如果为空）
7. **CreateRoomSO()**：创建新的RoomSO资产并保存到Assets/_Data/Level/Rooms/
8. **SetupDoorConnections()**：设置所有房间之间的门连接
9. **CreateDoor()**：创建Door对象，配置目标房间和生成点
10. **CreateSpawnPoint()**：在目标房间创建玩家生成点

**技术要点：**
- 完整的Undo/Redo支持（Undo.SetCurrentGroupName, Undo.RegisterCreatedObjectUndo）
- 使用PrefabUtility.InstantiatePrefab实例化Prefab
- 使用SerializedObject和SerializedProperty安全修改私有字段
- 异常处理 + 自动回滚（Undo.RevertAllDownToGroup）
- 自动创建目录和保存AssetDatabase
- Editor-only代码，放在Editor文件夹

**目的：** 一键将脚手架数据转换为完整的Unity场景，包括Room、Door、Collider、RoomSO等所有必要组件，无需手动配置。

---

### 71. LevelDesignerWindow — 可视化关卡设计器 — 2026-02-15

**新建文件：**
- `Assets/Scripts/Level/Editor/LevelDesignerWindow.cs`

**内容：** EditorWindow，完整的可视化关卡编辑器，包含以下功能模块：

1. **设置面板**
   - Level Scaffold：选择/创建关卡脚手架数据
   - Element Library：选择关卡元素库
   - New Scaffold：一键创建新的LevelScaffoldData资产

2. **房间列表**
   - Add Room：添加新房间
   - 房间展开/折叠：显示/隐藏房间详情
   - 房间编辑：修改名称、类型、位置、大小、RoomSO
   - 连接管理：添加/编辑/删除门连接
   - 删除房间：带确认对话框

3. **拓扑视图（核心功能）**
   - 网格背景：50px网格线
   - 房间节点：彩色矩形表示房间，按RoomType着色
   - 连接线：蓝色线条表示门连接
   - 拖拽移动：鼠标左键拖拽房间移动位置
   - 缩放控制：Zoom滑块（0.25x - 2x）
   - 视图重置：Reset View按钮
   - 标签显示：房间名称标签

4. **Scene视图集成**
   - 房间Gizmo：在Scene视图中绘制房间WireCube
   - 选中高亮：选中的房间显示白色边框和标签
   - 位置同步：拓扑视图和Scene视图位置实时同步

5. **一键生成**
   - Generate to Scene：确认对话框后调用LevelGenerator
   - 完整Undo：所有生成操作可撤销

**技术要点：**
- EditorWindow生命周期（OnEnable/OnDisable）
- SceneView.duringSceneGui事件订阅
- Handles绘制API（DrawSolidRectangleWithOutline, DrawLine, Label）
- GUI事件处理（MouseDown, MouseDrag, MouseUp）
- 世界坐标 ↔ 拓扑坐标转换（WorldToTopology/TopologyToWorld）
- Undo系统集成（Undo.RecordObject）
- EditorUtility.DisplayDialog确认对话框
- EditorUtility.SaveFilePanelInProject文件保存面板

**工作流：**
1. 创建LevelElementLibrary并配置所有元素Prefab
2. 创建LevelScaffoldData
3. 在LevelDesignerWindow中选择Scaffold和Library
4. 点击"Add Room"添加房间，或在拓扑视图中拖拽
5. 配置房间属性（名称、类型、位置、大小）
6. 添加门连接，配置目标房间和位置
7. 点击"Generate to Scene"一键生成到Unity场景
8. 生成的场景包含完整的Room、Door、Collider、RoomSO

**目的：** 提供"搭积木"式的关卡编辑体验，让策划/设计师可以直观地布置房间、连接门，然后一键生成完整的Unity场景，所有组件自动配置完成。

---

---

## 杂项

### CreateAssetMenu menuName 统一 — 2026-02-13 15:40

**修改文件：**
- `Assets/Scripts/Level/Data/CheckpointSO.cs`
- `Assets/Scripts/Level/Data/EncounterSO.cs`
- `Assets/Scripts/Level/Data/KeyItemSO.cs`
- `Assets/Scripts/Level/Data/RoomSO.cs`
- `Assets/Scripts/Level/Data/WorldProgressStageSO.cs`

**内容：** 将 `[CreateAssetMenu]` 的 `menuName` 参数从 `"Project Ark/Level/..."` (带空格) 统一为 `"ProjectArk/Level/..."` (无空格)。

**目的：** 消除 Unity Editor Project 右键 Create 菜单中出现两个分组（"ProjectArk" 和 "Project Ark"）的问题，统一到 `ProjectArk` 分组下。

**技术：** `[CreateAssetMenu]` attribute menuName 字符串修改。

---

### 飞船手感 SO 资产一键导入脚本 — 2026-02-13 15:42

**新建文件：**
- `Assets/Scripts/Ship/Editor/ProjectArk.Ship.Editor.asmdef` — Ship Editor 程序集定义（Editor-only，引用 ProjectArk.Ship）
- `Assets/Scripts/Ship/Editor/ShipFeelAssetCreator.cs` — 一键创建/更新 ShipStatsSO 和 ShipJuiceSettingsSO 资产

**内容：**
- `ShipFeelAssetCreator` 提供 3 个菜单入口：
  - `ProjectArk > Ship > Create Ship Feel Assets (All)` — 一键创建所有资产
  - `ProjectArk > Ship > Create Ship Stats Asset` — 仅创建/更新 ShipStatsSO
  - `ProjectArk > Ship > Create Ship Juice Settings Asset` — 仅创建 ShipJuiceSettingsSO
- 幂等设计：已存在的资产不会重复创建
- 对已存在的 `DefaultShipStats.asset`，会智能填充新增字段（Dash/HitFeedback/Curves）的默认值，同时保留用户已手动调整的旧字段值
- 所有参数通过 `SerializedProperty` 精确设置，与 SO 中的 `[SerializeField]` 字段一一对应

**目的：** 让用户无需手动在 Inspector 中逐个配置新增的 20+ 个参数，一键即可生成完整配置的 SO 资产。

**技术：** `UnityEditor.MenuItem` + `SerializedObject` / `SerializedProperty` API + `AssetDatabase.CreateAsset` + 幂等检查。

---

### DashAfterImage PrimeTween API 修复 — 2026-02-13 16:00

**修改文件：**
- `Assets/Scripts/Ship/VFX/DashAfterImage.cs`

**内容：** 将 `Tween.Alpha(_spriteRenderer, ...)` 替换为 `Tween.Custom(this, startAlpha, 0f, ...)` + `.OnComplete(this, target => target.ReturnToPool())`。

**目的：** 修复编译错误 CS1739。PrimeTween 的 `Tween.Alpha()` 仅支持 UI Graphic 组件，不支持 SpriteRenderer，且无 `onComplete` 命名参数。

**技术：** 使用 `Tween.Custom<T>` 零 GC 重载手动插值 SpriteRenderer.color.a，配合零 GC `OnComplete(target, callback)` 链式调用触发回池。

---

### 批量修复 CS0618 + CS4014 编译器警告 — 2026-02-13 16:15

**修改文件（CS0618 — Physics2D NonAlloc 弃用，共 11 处）：**
- `Assets/Scripts/Combat/Enemy/HitboxResolver.cs` — 3 处（Circle、Box、Cone）
- `Assets/Scripts/Combat/Enemy/EnemyPerception.cs` — 1 处（FactionScan）
- `Assets/Scripts/Combat/Enemy/States/AttackSubState.cs` — 1 处（Legacy fallback）
- `Assets/Scripts/Combat/Enemy/EnemyAffixController.cs` — 2 处（Explosive、Reflect）
- `Assets/Scripts/Combat/Enemy/ThreatSensor.cs` — 1 处（ProjectileScan）
- `Assets/Scripts/Combat/Enemy/States/StalkerStrikeState.cs` — 1 处（Legacy fallback）
- `Assets/Scripts/Combat/Enemy/EnemyEntity.cs` — 2 处（ResolveOverlap、GetSeparationForce）

**内容：** 将所有 `Physics2D.OverlapCircleNonAlloc(pos, radius, buffer, layerMask)` 替换为 `Physics2D.OverlapCircle(pos, radius, contactFilter, buffer)`，`OverlapBoxNonAlloc` 同理。每个类新增一个 `static ContactFilter2D` 字段，调用前通过 `SetLayerMask()` 配置。

**目的：** Unity 6 将 `NonAlloc` 后缀 API 标记为 `[Obsolete]`，新版 `OverlapCircle`/`OverlapBox` 的 `ContactFilter2D + Collider2D[]` 重载是官方替代，功能等价且同样零 GC。

**技术：** `ContactFilter2D.SetLayerMask(int)` + `Physics2D.OverlapCircle(Vector2, float, ContactFilter2D, Collider2D[])` / `Physics2D.OverlapBox(...)` 缓冲区重载。

---

**修改文件（CS4014 — 未 await 的 PrimeTween 调用，共 3 处）：**
- `Assets/Scripts/Level/Pickup/PickupBase.cs` — `Tween.Scale` fire-and-forget
- `Assets/Scripts/Level/GameFlow/GameFlowManager.cs` — 2 处 `Tween.Custom` fade 补间

**内容：** 在返回值前加 `_ =` 显式丢弃，告知编译器这是有意为之的 fire-and-forget 调用。

**目的：** 消除 CS4014 警告。PrimeTween 补间动画不需要 await（由引擎 Update 驱动），但 C# 编译器会对未 await 的异步返回值发出警告。

**技术：** C# discard `_ =` 模式。

---

### 验收清单文档可读性优化 — 2026-02-13 16:30

**修改文件：**
- `Docs/VerificationChecklist_Phase3-5_ShipFeel.md`

**内容：**
- 第三步（飞船 Prefab 配置）：新增完整 Hierarchy 树形图，说明 4 个新组件为何必须放在根 GO 上（RequireComponent 依赖），将原始密集表格拆分为 5 个子步骤（3-1 到 3-5），每个组件独立表格逐字段说明赋值来源
- 第四步 4B（Arena 配置）：拆分为 RoomSO / Room GO / ArenaController 三层，表格化字段配置
- 第四步 4C（Hazard 配置）：拆分 3 种 Hazard 各自独立步骤，每种含完整的创建+配置表格
- 第六步（Phase 4-5 UI）：每个子步骤增加概览说明（"这是什么"、"放在哪里"），Inspector 字段表格增加"拖入来源"列明确 Hierarchy vs Project 窗口，6F 管理器表格增加"挂在哪"和"是否新增"列
- 第八步（层间转场）：增加组件定位说明和 BGM 配合说明
- 第九步（NarrativeFallTrigger）：表格化 Inspector 配置

**目的：** 提升配置文档可读性，消除"组件该挂在哪个 GameObject 上"和"字段值从哪里拖入"的歧义。

**技术：** 纯文档重构，无代码变更。

---

### ImpulseSourceRegistrar 自动注册脚本 — 2026-02-13 16:40

**新建文件：**
- `Assets/Scripts/Ship/Combat/ImpulseSourceRegistrar.cs`

**内容：** MonoBehaviour，`[RequireComponent(typeof(CinemachineImpulseSource))]`，`Start()` 中自动调用 `HitFeedbackService.RegisterImpulseSource()`。

**目的：** 消除手动在初始化脚本中注册 ImpulseSource 的步骤，挂到 CinemachineCamera 上即可自动完成注册。

**技术：** `[RequireComponent]` + `GetComponent<CinemachineImpulseSource>()` + 静态服务注册。

---

### 验收清单文档二次优化（补全前置步骤） — 2026-02-13 17:00

**修改文件：**
- `Docs/VerificationChecklist_Phase3-5_ShipFeel.md`

**内容：**
- **第四步（Phase 3 关卡配置）**：全面重写，拆分为 4A~4E 五个子步骤
  - 4A（EncounterSO）：增加"EncounterSO 是什么"解释，详细说明 Waves 数组如何点 `+` 添加
  - 4B（RoomSO）：新增"RoomSO 是什么"解释，区分"从零创建"和"修改 Scaffolder 已创建的"两条路径，表格化全部字段
  - 4C（Room GameObject）：新增"Room GameObject 是什么"解释，从零创建的完整步骤（含刷怪点、EnemySpawner），已有 Room 的快捷路径
  - 4D（ArenaController）：独立为单独子步骤，解释"ArenaController 是什么"和 RequireComponent 依赖关系，附完成后的 Hierarchy 树形图
  - 4E（Hazard）：增加"Hazard 是什么"和 Layer 使用说明
- **第五步（Sheba Scaffolder）**：增加 Scaffolder 功能解释，后续手动配置拆分为 6 个编号子项，每项标注操作位置和具体字段

**目的：** 解决"Arena 是什么、从哪来、怎么配"等前置知识缺失问题，确保从零开始的用户也能按照文档独立完成配置。

**技术：** 纯文档重构，无代码变更。

---

### 修复 CameraConfiner PolygonCollider2D 阻挡飞船 — 2026-02-13 17:20

**修改文件：**
- `Assets/Scripts/Level/Editor/ShebaLevelScaffolder.cs`

**内容：**
- 修复 `CreateRoomGameObjects()` 中 CameraConfiner 子物体的 Layer：从 `Default`(0) 改为 `Ignore Raycast`(2)
- 新增 `[MenuItem("ProjectArk/Fix CameraConfiner Layers")]` 一键修复菜单，批量将场景中所有已存在的 CameraConfiner 子物体的 Layer 设为 `Ignore Raycast`

**目的：** CameraConfiner 的 PolygonCollider2D 设计上仅供 Cinemachine Confiner 读取边界，但因为 `isTrigger = false` 且 Layer = Default，Physics2D 将其视为实体碰撞墙壁，导致飞船被推到房间外无法进入。改为 `Ignore Raycast` 层后该碰撞体不再参与任何物理碰撞。

**技术：** `GameObject.layer = 2`（Ignore Raycast 内置层），Editor MenuItem + `Undo.RecordObject` 批量修复。

---

### MapUIBuilder — Map UI 一键生成工具

**文件：**

| 路径 | 操作 | 用途 |
|------|------|------|
| `Assets/Scripts/Level/Editor/MapUIBuilder.cs` | 新建 | 两个 MenuItem：`ProjectArk > Map > Build Map UI Prefabs`（创建 3 个 Prefab）和 `ProjectArk > Map > Build Map UI Scene`（创建 MapPanel + MinimapHUD 场景层级并连线所有引用） |
| `Assets/Scripts/Level/Editor/ProjectArk.Level.Editor.asmdef` | 修改 | 添加 `Unity.TextMeshPro` 和 `Unity.InputSystem` 引用 |

**技术要点：**
- 遵循 UICanvasBuilder 的模式：`WireField(Component, string, Object)` 绑定 SerializeField，`PrefabUtility.SaveAsPrefabAsset()` 保存 Prefab
- 创建 3 个 Prefab：MapRoomWidget（含 Background/IconOverlay/CurrentHighlight/FogOverlay/Label 5 个子物体）、MapConnectionLine（含 Image）、FloorTabButton（含 Button + Label）
- MapPanel 场景层级：CanvasGroup + ScrollRect（Viewport + MapContent）+ FloorTabBar（HorizontalLayoutGroup）+ PlayerIcon（菱形旋转 45°）
- MinimapHUD 场景层级：锚定右下角 200×200，含 Background + Border + 带 Mask 的 Content + FloorLabel
- 自动查找 ShipActions InputActionAsset 并赋值
- 幂等设计：已存在的 Prefab / 组件不会重复创建

---

### Level Phase 6: 世界时钟与动态关卡系统 (L20-L27) — 2026-02-15 14:30

**新建文件：**

| 路径 | 用途 |
|------|------|
| `Assets/Scripts/Level/WorldClock/WorldClock.cs` | L20: 游戏内时钟核心，ServiceLocator 注册，可配置周期长度(默认1200s)、时间倍速、暂停恢复。每帧广播 `LevelEvents.OnTimeChanged(float normalizedTime)`，周期完成时广播 `OnCycleCompleted(int cycleCount)` |
| `Assets/Scripts/Level/WorldClock/WorldPhaseManager.cs` | L21: 世界阶段管理器，ServiceLocator 注册，持有 `WorldPhaseSO[]` 阶段定义列表，监听 `OnTimeChanged` 判定当前阶段，阶段切换时广播 `LevelEvents.OnPhaseChanged(int, string)` |
| `Assets/Scripts/Level/Data/WorldPhaseSO.cs` | L21: 世界时间阶段 SO 定义，含 PhaseName、StartTime/EndTime(归一化0..1)、AmbientColor、PhaseBGM、低通滤波器开关、敌人伤害/生命倍率、隐藏通道可见性。支持跨午夜时间范围 |
| `Assets/Scripts/Level/DynamicWorld/ScheduledBehaviour.cs` | L23: 通用阶段驱动组件，配置 `_activePhaseIndices[]` 指定在哪些阶段启用目标 GameObject。支持反转逻辑。用于 NPC 交易窗口、定时门、隐藏通道等 |
| `Assets/Scripts/Level/DynamicWorld/WorldEventTrigger.cs` | L24: 进度事件驱动的永久世界变化组件。监听 `OnWorldStageChanged`，当达到 `_requiredWorldStage` 时一次性触发：启用/禁用指定 GameObject + UnityEvent 回调。通过 `ProgressSaveData.Flags` 持久化触发状态 |
| `Assets/Scripts/Level/DynamicWorld/TilemapVariantSwitcher.cs` | L26: Tilemap 变体切换器，管理多个子 Tilemap（如塌陷前/塌陷后），`SwitchToVariant(int)` 公共 API 供 UnityEvent 调用 |
| `Assets/Scripts/Level/Data/RoomVariantSO.cs` | L25: 房间变体 SO 定义，含 VariantName、`ActivePhaseIndices[]`（激活阶段）、`OverrideEncounter`（覆盖怪物配置）、`EnvironmentIndex`（环境子物体索引） |
| `Assets/Scripts/Level/DynamicWorld/AmbienceController.cs` | L27: 全局氛围控制器，ServiceLocator 注册。监听 `OnPhaseChanged`，驱动 URP Volume 后处理渐变（Vignette + ColorAdjustments via PrimeTween）、环境粒子启停、BGM crossfade（AudioManager）、低通滤波器开关 |

**修改文件：**

| 路径 | 变更 |
|------|------|
| `Assets/Scripts/Core/LevelEvents.cs` | 新增 `OnTimeChanged(float)`、`OnCycleCompleted(int)`、`OnPhaseChanged(int, string)` 三组事件 + Raise 方法 |
| `Assets/Scripts/Core/Save/SaveData.cs` | `ProgressSaveData` 新增 `WorldClockTime`(float)、`WorldClockCycle`(int)、`CurrentPhaseIndex`(int) 三个字段 |
| `Assets/Scripts/Level/SaveBridge.cs` | `CollectProgressData` 新增 WorldClock + WorldPhaseManager 数据收集；`DistributeProgressData` 新增 WorldClock 时间恢复 + WorldPhaseManager 阶段恢复 |
| `Assets/Scripts/Level/Room/Door.cs` | 新增 `_openDuringPhases[]` 配置字段；新增 `Start()`/`OnDestroy()` 订阅 `OnPhaseChanged`；新增 `EvaluateSchedule()` 在阶段切换时自动在 `Open`/`Locked_Schedule` 间切换 |
| `Assets/Scripts/Level/Room/Room.cs` | 新增 `_variants[]` (RoomVariantSO) + `_variantEnvironments[]` 配置字段；新增 `Start()`/`OnDestroy()` 订阅 `OnPhaseChanged`；新增 `ApplyVariantForPhase()` 按阶段切换遭遇配置和环境子物体；`ActivateEnemies()` 改用 `ActiveEncounter` 属性支持变体覆盖 |
| `Assets/Scripts/Level/ProjectArk.Level.asmdef` | 新增 `Unity.RenderPipelines.Core.Runtime`、`Unity.RenderPipelines.Universal.Runtime` 引用（AmbienceController 需要访问 Volume/Vignette/ColorAdjustments） |

**技术要点：**
- 所有管理器遵循 ServiceLocator 注册模式（Awake 注册、OnDestroy 注销）
- 事件卫生：所有 `OnPhaseChanged`/`OnWorldStageChanged` 订阅均在 `OnDestroy` 中取消
- 异步纪律：AmbienceController 使用 `async UniTaskVoid` + `CancellationTokenSource` + `destroyCancellationToken` 链接
- PrimeTween 模式：后处理渐变使用 `Tween.Custom(..., useUnscaledTime: true, ease: Ease.InOutSine)` 与 DoorTransitionController 一致
- 数据驱动：WorldPhaseSO 支持跨午夜时间范围（`ContainsTime` 方法处理 start > end 的环绕情况）
- 运行时数据隔离：WorldPhaseSO 仅被读取，运行时状态由 WorldPhaseManager 管理
- WorldEventTrigger 持久化：通过 `ProgressSaveData.Flags` 以 key-value 形式存储触发状态，确保保存/加载后不重复触发

**目的：** 完成关卡模块 Phase 6（L20-L27），实现混合时间模型——事件驱动大阶段 + 轻量循环小周期。为"活的世界"体验提供完整系统支撑：定时门/NPC/隐藏通道（ScheduledBehaviour）、Boss击杀→永久世界变化（WorldEventTrigger）、Tilemap结构改变（TilemapVariantSwitcher）、全局氛围渐变（AmbienceController）。

---

### Phase6AssetCreator 一键配置工具 + 验收清单 Phase 6 章节 — 2026-02-15 15:30

**新建文件：**

| 路径 | 用途 |
|------|------|
| `Assets/Scripts/Level/Editor/Phase6AssetCreator.cs` | 三个 MenuItem：`Phase 6: Create World Clock Assets`（创建 4 个 WorldPhaseSO）、`Phase 6: Build Scene Managers`（创建 WorldClock/WorldPhaseManager/AmbienceController 管理器并连线）、`Phase 6: Setup All`（一键执行以上两步） |

**修改文件：**

| 路径 | 变更 |
|------|------|
| `Docs/VerificationChecklist_Phase3-5_ShipFeel.md` | 版本升级到 v2.0。新增 G 分组（Phase 6 共 13 项检查）、第十步（Phase 6 详细配置，含 10A-10H 八个子步骤）、第十一步（Phase 6 Play Mode 验证，含 5 个子表）、常见问题排查追加 10 条 Phase 6 相关条目 |

**技术要点：**
- Phase6AssetCreator 遵循 LevelAssetCreator 的模式：`SerializedObject` + `FindProperty` 设置 SO 字段
- 幂等设计：已存在的资产和组件不会重复创建
- 4 个 WorldPhaseSO 的时间范围按 GDD 定义分配：辐射潮(0~0.208)、平静期(0.208~0.5)、风暴期(0.5~0.75)、寂静时(0.75~1.0)
- 场景管理器创建为 Managers 的子物体，WorldPhaseManager 自动连线 4 个 SO

**目的：** 提供一键配置工具降低 Phase 6 系统的配置门槛；在验收清单中提供完整的分步操作指南，覆盖所有 Phase 6 组件的配置和验证。

---

### RoomBatchEditor 批量房间编辑器 — 2026-02-15 22:57

**新建/修改文件：**

| 路径 | 操作 | 用途 |
|------|------|------|
| `Assets/Scripts/Level/Editor/RoomBatchEditor.cs` | 修改 | 批量房间编辑器工具，提供房间列表管理、批量属性编辑、拓扑可视化、验证检查、JSON导入/导出功能 |

**功能特性：**

1. **房间列表管理**
   - 自动发现场景中所有Room组件（包含inactive）
   - 支持多选房间进行批量操作
   - 提供搜索功能（按房间名称或RoomID过滤）
   - 显示房间总数和选中数量

2. **批量属性编辑**
   - 批量修改RoomSO引用（使用SerializedObject安全修改私有`_data`字段）
   - 批量修改房间类型（Normal/Arena/Boss/Safe）
   - 批量修改楼层级别（FloorLevel）
   - 支持Undo/Redo（使用Undo.RecordObjects）
   - 自动保存AssetDatabase

3. **拓扑可视化**
   - 网格布局显示所有房间节点
   - 按房间类型颜色区分（Normal=蓝，Arena=红，Boss=深红，Safe=绿）
   - 支持鼠标滚轮缩放和平移
   - 选中房间高亮显示（黄色边框）

4. **验证检查**
   - 检查缺失RoomSO引用（Error级别）
   - 检查BoxCollider2D是否为Trigger（Warning级别）
   - 检查CameraConfiner是否在Ignore Raycast层（Warning级别）
   - 支持定位到问题房间

5. **JSON导入/导出**
   - 导出房间数据为JSON（名称、RoomID、类型、楼层、位置、是否有遭遇）
   - 导入JSON数据（仅数据导入，不创建GameObjects）
   - 使用JsonUtility序列化

6. **快捷操作**
   - 在Scene视图中聚焦选中房间
   - 在Hierarchy中选中房间
   - 激活/禁用选中房间

**技术要点：**
- 命名规范：常量使用PascalCase（WindowTitle、MenuPath），私有字段使用_camelCase
- 使用SerializedObject和SerializedProperty安全修改私有[SerializeField]字段
- 正确使用GUI.matrix并通过try/finally恢复状态
- 遵循项目架构原则：数据驱动、事件卫生、运行时数据隔离
- Editor-only代码，使用#if UNITY_EDITOR条件编译
- 遵循项目代码规范：XML文档注释、正确的命名空间（ProjectArk.Level.Editor）

**目的：** 提升关卡编辑效率，允许策划/开发者一次性对多个房间进行批量操作，避免逐个手动修改的繁琐过程。同时提供验证功能帮助发现常见配置错误。

---

---

## Boost Trail VFX GG 环形 Halo 资源切换与尺寸收敛  2026-03-11 15:26

### 修改文件

- `Assets/Scripts/Ship/Editor/BoostTrailPrefabCreator.cs`  将 `BoostActivationHalo` 默认资源切换为 `vfx_ring_glow_uneven`，新增 `vfx_magnetic_rings` 兜底，并移除阻塞 Unity MCP 的弹窗；同时把 halo 默认基础缩放从 `1.4` 收到 `1.0`
- `Assets/Scripts/Ship/VFX/BoostTrailView.cs`  将局部 halo 的爆发参数收敛为更小、更像 GG 的局部小圆 burst（`0.72 -> 1.45`，时长 `0.24s`）
- `Assets/Scripts/Ship/Editor/ShipBoostDebugMenu.cs`  调整 `Show Activation Halo` 的强制显示尺寸，便于直接验证“当前基础尺寸是否过大”
- `Assets/Scripts/Ship/Editor/ShipBoostTrailPrefabReplacer.cs`  移除完成后弹窗，避免一键替换在自动化链路里阻塞
- `Assets/Scripts/Ship/Editor/ShipBoostTrailSceneBinder.cs`  移除完成后弹窗，避免 scene wiring 菜单阻塞 Unity MCP
- `Assets/_Art/VFX/BoostTrail/Textures/vfx_ring_glow_uneven.png`  从 GG 原始解包素材导入为新的局部 halo 主资源
- `Assets/_Art/VFX/BoostTrail/Textures/vfx_magnetic_rings.png`  从 GG 原始解包素材导入为备用环形资源
- `Assets/_Prefabs/VFX/BoostTrailRoot.prefab`  重新生成，使 `BoostActivationHalo` 吃到新的 GG 环形素材与基础缩放
- `Assets/_Prefabs/Ship/Ship.prefab`  重新替换 `BoostTrailRoot`
- `Assets/Scenes/SampleScene.unity`  重新执行 scene references 绑定并保存
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容

继续针对“Boost 不该是全屏，而应该是飞船附近一个小小的圆形能量爆发”的偏差做第三轮收口。先从 GG 原始解包素材中确认 `vfx_ring_glow_uneven.png` 更接近参考效果，再将当前 prefab 生成链、Ship 替换链、scene wiring 链串起来重新落地。随后使用 Unity MCP 在 Play Mode 下分别抓取“强制显示 Halo”和“真实 Trigger Boost”两组截图，验证新的局部环形 glow 已经取代此前的全屏主视觉。相较前一版程序化 halo，这一轮的结果已经明显更接近 GG：主观读感不再是整屏闪白，而是围绕飞船的局部环形 burst；同时通过缩小基础 scale 和收紧扩张范围，避免 halo 再次膨胀成过大的覆盖圈。

### 目的

把当前实现从“方向已经修正，但局部 halo 仍像临时占位资源”继续推进到“已经能读出 GG 式局部小圆 burst”的状态。这样后续若还要继续精修，就不再是先解决明显错误，而是围绕颜色、持续时间、扩张速度和 shader 细节做品相打磨。

### 技术

1. 直接引入 GG 原始 `vfx_ring_glow_uneven` 贴图，替代过弱的程序化 halo 资源
2. 在 `BoostTrailPrefabCreator` 中把 halo 资源选择改成“GG 主资源 + GG 备用资源 + 旧 overlay 最终兜底”的查找顺序
3. 去掉三个 Editor 一键工具的 `DisplayDialog` 模态框，修复 Unity MCP 自动化会被菜单成功后的弹窗卡死的问题
4. 通过重新生成 `BoostTrailRoot`、重新替换 `Ship.prefab`、重新绑定 `SampleScene` 三步，确保资源切换真正落到运行中的 Ship 上
5. 使用 Unity MCP Play Mode 截图对比验证：
   - 静态强制显示时，环形 glow 已稳定呈现为局部小圆
   - 真实 Boost 触发时，局部 halo 已能在船体周围读到，而不再是整屏 flash 抢主视觉

---

---

## 拖拽放置高亮系统 (Drag Placement Highlight System)  2026-03-05 16:23

### 新建文件
- `Assets/Scripts/UI/DragHighlightLayer.cs`  独立高亮 tile 管理器（189 行）

### 修改文件
- `Assets/Scripts/UI/TrackView.cs`  集成 DragHighlightLayer，改造 SetShapeHighlight，新增 SetSingleHighlight / ClearShapeHighlight / ClearAllHighlightTiles
- `Assets/Scripts/UI/SlotCellView.cs`  OnPointerEnter 中 Core/Prism/SAIL/SAT 高亮全部改为调用 DragHighlightLayer，不再直接修改 _backgroundImage
- `Assets/Scripts/UI/DragDrop/DragDropManager.cs`  CleanUp 中新增遍历所有 TrackView 调用 ClearAllHighlightTiles()

### 内容简述
实现了独立的拖拽高亮 tile 层，将高亮渲染与格子背景色完全解耦。

### 目的
解决拖拽悬停高亮污染 SlotCellView._backgroundImage.color 的问题：高亮 tile 是独立的 GameObject 层，不影响格子原始状态，清除即隐藏，无残影。

### 技术方案
1. DragHighlightLayer：每个 TypeColumn 对应一个实例，在 TrackView.Awake 中动态创建。内部维护 tile 对象池，仅在 shape/anchor/state 变化时重建。
2. 坐标系与 ItemOverlayView 完全一致：anchorMin=anchorMax=(0,0)，pivot=(0,1)，anchoredPosition=(col*step, -row*step)。
3. raycastTarget=false：高亮 tile 不阻挡指针事件，底层格子仍可正常接收 OnPointerEnter/Exit。
4. SetAsLastSibling：每次 ShowHighlight 时将 tile 置于最顶层，渲染在所有 ItemOverlayView 之上。
5. Core/Prism invalid 分支：改为调用 SetShapeHighlight 传入 Shape1x1 + Invalid，走统一的 tile 层。
6. SAIL/SAT 支持：通过 SetSingleHighlight(SlotType, col=0, row=CellIndex, state) 实现单格高亮。
7. CleanUp 保障：DragDropManager.CleanUp 中遍历所有 TrackView 调用 ClearAllHighlightTiles()，确保拖拽结束后无残留高亮。

---

---

## Bug Fix: DragGhostView 拖拽时双重 Icon 渲染  2026-03-05 16:47

### 修改文件
- `Assets/Scripts/UI/DragDrop/DragGhostView.cs`

### 内容简述
修复拖拽时同时出现两个视觉元素（一个 Icon + 一个 Shape）的 bug。

### 根因
DragGhostView 中存在两个 Icon 来源同时可见：
1. `[SerializeField] private Image _iconImage`  Prefab Inspector 连线的旧字段
2. `_iconImageDynamic`  Show() 中通过 ItemIconRenderer.BuildIconCentered() 动态创建的新 Icon

旧代码在 Show() 中用 `_iconImage.sprite = null; _iconImage.color = Color.clear;` 试图隐藏旧字段，但 `color = Color.clear` 不能保证 Image 完全不渲染（Prefab 默认颜色可能覆盖），且 GameObject 仍然 active 占据层级。

### 修复方案
在 Awake() 中立即为 `_iconImage` 添加 CanvasGroup 并设 alpha=0，彻底阻断其渲染，与 CLAUDE.md 第11条（禁止 SetActive）保持一致。Show() 中的旧清除代码替换为注释说明。

---

---

## Bug Fix: DragGhostView Icon 与 Shape 坐标系不匹配导致分离  2026-03-05 16:56

### 修改文件
- `Assets/Scripts/UI/ItemIconRenderer.cs`（新增 `BuildIconAbsolute` 方法）
- `Assets/Scripts/UI/DragDrop/DragGhostView.cs`（Show() 中改用 `BuildIconAbsolute`）

### 内容简述
修复拖拽 Ghost 中 Icon 和 Shape cells 出现在两个不同位置的 bug（截图中蓝色小方块 + 青色 L 形分离）。

### 根因
坐标系不匹配：
- `BuildShapeCellsAbsolute` 使用 `anchorMin=anchorMax=zero`（左下角锚点），从 `(0,0)` 原点向右向下排列 cells
- `BuildIconCentered` 使用归一化 centroid 作为 anchor（`anchorMin=anchorMax=centroid`，相对于父节点 sizeDelta 的百分比）

对于 L 形 Shape，`BuildIconCentered` 计算出的 centroid ≈ `(0.5, 0.5)`（bounding box 中心），而 Shape cells 实际分布在左下角，导致 Icon 跑到右上角空白区域，两者完全分离。

### 修复方案
新增 `ItemIconRenderer.BuildIconAbsolute()` 方法：
- 与 `BuildShapeCellsAbsolute` 使用完全相同的坐标系（`anchorMin=anchorMax=zero`）
- Icon centroid 用绝对像素计算：`cx = Σ(col*step + cellSize/2) / count`，`cy = -Σ(row*step + cellSize/2) / count`（负号因 UI y 轴向下）
- `DragGhostView.Show()` 改为调用 `BuildIconAbsolute`，传入 `cellSize=_ghostSize.x`、`cellGap=_cellGap`

---

---

## 未解锁格子完全隐藏 - 2026-03-05 14:22

**修改文件：**
- `Assets/Scripts/UI/TrackView.cs`（`RefreshColumn` 方法）

**内容简述：**
在 `RefreshColumn<T>` 方法中，根据 `layer.Rows × layer.Cols` 动态控制 cell 的 `SetActive` 状态：
- 索引 `< unlockedCount` 的 cell：`SetActive(true)`（显示）
- 索引 `>= unlockedCount` 的 cell：`SetActive(false)`（完全隐藏）
- `layer == null` 时 `unlockedCount = 0`，所有 cell 隐藏

**目的：** 未解锁的格子不再显示灰色 `+` 占位符，而是完全从 UI 中隐藏。

**技术方案：**
- 利用已有的 `layer.Rows`/`layer.Cols` 动态容量属性计算 `unlockedCount`
- 在重置 cell 状态之前先做 `SetActive` 控制，避免对隐藏 cell 调用 `SetEmpty()`
- SAIL/SAT 列不受影响（使用独立的 `RefreshSailColumn`/`RefreshSatColumn` 方法）

---

---

## Bug Fix: DragGhostView CanvasGroup 初始化失败 - 2026-03-05 14:30

**修改文件：**
- `Assets/Scripts/UI/DragDrop/DragGhostView.cs`（`Awake` 方法）

**错误信息：**
```
[DragGhostView] Failed to get/add CanvasGroup on ReplaceHint 'ReplaceHint'
```

**根因：**
C# 的 `??` 运算符绕过了 `UnityEngine.Object` 对 `==` 的重写，直接在 CLR 层面检查 null。
`GetComponent<CanvasGroup>()` 找不到组件时返回"假 null"（Unity 层面为 null，CLR 层面不为 null），
导致 `??` 认为左侧有值，不执行右侧的 `AddComponent`，`_replaceHintCg` 持有无效对象，
后续 `if (_replaceHintCg != null)` 用 Unity `==` 检查时判定为 null，触发 LogError。

**修复方案：**
将 `??` 运算符改为显式 `if` 判断：
```csharp
_replaceHintCg = _replaceHintLabel.GetComponent<CanvasGroup>();
if (_replaceHintCg == null)
    _replaceHintCg = _replaceHintLabel.gameObject.AddComponent<CanvasGroup>();
```
同样修复了 `_nameLabelCg` 的相同问题。

**教训：** Unity 项目中凡是涉及 `UnityEngine.Object` 的 null 检查，必须用显式 `== null`，禁止用 `??` 运算符。

---

---

## 示巴星 NPC 缕拉(Lyra)设计 + 关卡心流/机制全面优化 2026-02-21 10:45

### 新建文件：
- `Docs/GDD/示巴星 (鸣钟者)/NPC-鸣钟者角色设定.md` — 完整的NPC角色设定文档

### 修改文件：
- `Docs/GDD/示巴星 (鸣钟者)/关卡心流与节奏控制-2.csv` — 全面重建(171行)，整合所有NPC+机制改进
- `Docs/GDD/示巴星 (鸣钟者)/叙事与遭遇演出表.csv` — 增加NPC缕拉叙事触发点L-01到L-08，更新N-12

### 内容简述：

#### 1. NPC 缕拉(Lyra)角色设计【最高优先级】
- **角色**：最后的调音师——共鸣王朝第七声部（即兴段），拒绝大静默封印的自由歌者
- **视觉**：淡金色小型鸣钟者，琥珀色核心，左臂有大静默黑色裂纹（持续歌唱抵抗侵蚀）
- **核心关系**：索尔（Sol）——第一声部/基音/缕拉的爱人，选择沉默保护所有人
- **5幕故事弧**：
  - ACT1: 间接线索(温暖冰雕Z1b)+足迹(花路Z1d)+首次远距目击(Z1f)
  - ACT2: 远程援助(Z2a)+首次面对面(Z2b从恐惧到好奇)+频率标记buff(Z2c)
  - ACT3: 共情#1索尔雕像(Z3a)+安抚Boss辅助(Z3c)+飞翔快乐(Z3d-bis)+共情#2追捕记忆(Z3e)+合唱指挥(Z3g)
  - ACT4: 摇篮曲消逝预兆(Z4a)+共情#3索尔遗言→停止歌唱→半身黑化(Z4c)+最后请求(Z4d)
  - ACT5: 虚弱指挥+第七声部注入新歌+索尔苏醒+上船(Do-Sol约定)
- **3个共情触发点**：#1索尔雕像(95:00) #2追捕记忆(165:00) #3索尔遗言(235:00)
- **可选增益**：歌唱者跟随buff/安抚容错提高/合唱效率+50%/幼体稳定/高频脉冲/歌唱者增益x3

#### 2. 关卡心流CSV全面重建(171行)
- 修复编码问题（原文件混合编码无法解析→重建为UTF-8）
- **27个关卡节点**（含新增Z3d-bis天穹回廊）覆盖310分钟完整体验
- 心流曲线：ACT1(4→2→7→4→3→6) ACT2(5→3→5→9) ACT3(4→7→8→4→3→8→7→9) ACT4(6→8→3→7) ACT5(8→9→10)

#### 3. 七项机制改进
1. **涟漪=独立交互工具**：长按专用键发射，不占武器槽位，定位为探索工具非战斗武器
2. **世界时钟深度铺垫**：ACT2预兆(Z2c退潮现象+Z2d Boss战DPS窗口)→ACT3正式引入(3+强依赖场景)→ACT4崩坏(剥夺感)
3. **Z4a噪音累积槽**：替代硬潜行，渐进后果(30%骚动→60%局部发狂→100%全面爆发)，停火恢复，涟漪不加噪音
4. **Z4b完美闪避降热**：Dash穿过攻击=排热15-20%，蒸汽涟漪视觉反馈，与共鸣水晶形成主动vs被动互补
5. **Z3c Boss安抚简化**：4水晶边缘放置+8-10秒保持(非3秒)+走位陆续点亮+缕拉辅助延长到10秒
6. **Z5c视觉电报系统**：高音=蓝色上方/低音=红色地面/和弦=金色边框，视觉早于音频0.5-1秒，Signal-Window模式
7. **Z3d-bis减压阀**：天穹回廊12分钟，零战斗零新机制，张力3/10，纯跑酷+缕拉最快乐时刻

#### 4. 叙事触发点扩展
- 在叙事与遭遇演出表中增加L-01到L-08共8个NPC缕拉专属触发点
- 更新N-12终焉触发点以反映缕拉+索尔的结局演出

### 目的：
1. 通过NPC缕拉为示巴星注入情感核心，确保玩家在结束探索时有足够动力将她带上金丝雀号
2. 修复ACT3信息过载、世界时钟铺垫不足、硬潜行割裂等心流隐患
3. 优化涟漪槽位冲突、Boss安抚操作难度、热量系统死锁、最终Boss可读性等机制问题

### 技术方案：
纯设计文档工作，无代码变更。使用Python脚本分5批写入CSV（ACT1→ACT2→ACT3→ACT4→ACT5+总结），UTF-8编码。NPC角色设定独立为.md文档。叙事触发点通过replace_in_file直接编辑CSV。所有设计与EnemyPlanning.csv/StarChartPlanning.csv/地图逻辑与锁钥矩阵.csv保持一致。

---

---

## StarChart UI 完善 — Code Review 修复 2026-03-03 15:10

### 修改文件
- `Assets/Scripts/UI/DragDrop/DragGhostView.cs`
- `Assets/Scripts/UI/LoadoutSwitcher.cs`
- `Assets/Scripts/UI/StatusBarView.cs`
- `Assets/Scripts/Combat/StarChart/ItemShapeHelper.cs`
- `Assets/Scripts/UI/TrackView.cs`

### 内容简述
对 StarChart UI 完善模块进行 Code Review 后，修复了 2 个 Major 问题、3 个 Minor 问题和 1 个 Info 问题：

**Major-1 修复（DragGhostView.cs）**：`_replaceHintLabel` 和 `_nameLabel` 原先使用 `gameObject.SetActive(false/true)` 控制显隐，违反 CLAUDE.md 第11条。改为在 `Awake` 中为两个 TMP_Text 各自获取/添加 `CanvasGroup`，通过 `alpha = 0/1` 控制可见性，`blocksRaycasts = false` 防止遮挡交互。

**Major-2 修复（LoadoutSwitcher.cs）**：`ConfirmRename` 同时订阅了 `onSubmit` 和 `onDeselect`，按下 Enter 时两者同时触发导致双调用（事件触发两次、SaveManager 写入两次）。添加 `_isConfirmingRename` bool 防重入 flag，第二次调用直接 return。

**Minor-5 修复（StatusBarView.cs）**：`ShowIdle()` 直接将 `_idleText` 赋给 `_label.text`，导致玩家看到 `"EQUIPPED {0}/10 · INVENTORY {1} ITEMS · ..."` 原始占位符字符串。添加 `_equippedCount`/`_inventoryCount` 字段和 `SetCounts(int, int)` 公共方法，`ShowIdle()` 改为 `string.Format(_idleText, _equippedCount, _inventoryCount)`。

**Minor-3 修复（ItemShapeHelper.cs）**：`GetAbsoluteCells` 每次调用都 `new List<Vector2Int>`，在拖拽 hover 热路径上产生 GC 压力。添加接受 `List<Vector2Int> result` 输出参数的重载，调用方可复用缓存列表，原有重载保留向后兼容。

**Minor-4 修复（TrackView.cs）**：`SetShapeHighlight` 中使用 `SlotLayer<StarChartItemSO>.GRID_COLS` 访问常量，泛型类型参数与常量无关，语义混淆。提取为 `const int gridCols = SlotLayer<StarChartItemSO>.GRID_COLS` 局部常量，并添加注释说明。

**Info-2 修复（LoadoutSwitcher.cs）**：`OnDestroy` 原先只停止 PrimeTween 序列，未取消 Button 的 `onClick` 和 InputField 的 `onSubmit`/`onDeselect` 监听器。补充 `RemoveAllListeners()` 调用，符合 CLAUDE.md 架构原则（事件卫生）。

### 目的
消除 Code Review 发现的规范违规和功能性 bug，提升代码健壮性和可维护性。

### 技术方案
- CanvasGroup 替代 SetActive：在 Awake 中 `GetComponent<CanvasGroup>() ?? AddComponent<CanvasGroup>()`，alpha 控制显隐
- 防重入 flag：`_isConfirmingRename` bool，进入时置 true，退出时置 false
- string.Format 填充占位符：`SetCounts` 更新计数，`ShowIdle` 格式化输出
- GC 友好重载：`GetAbsoluteCells(shape, col, row, List<Vector2Int> result)` 原地填充
- 局部常量消歧：`const int gridCols = SlotLayer<StarChartItemSO>.GRID_COLS`

---

---

## Boost Trail VFX 局部激活光晕修正  2026-03-11 11:30

### 修改文件

- `Assets/Scripts/Ship/VFX/BoostTrailView.cs`  新增局部 `BoostActivationHalo` 爆发控制，并弱化全屏 flash 的峰值与时序
- `Assets/Scripts/Ship/Editor/BoostTrailPrefabCreator.cs`  为 `BoostTrailRoot` 自动创建并连线 `BoostActivationHalo` 节点

### 内容

针对 BoostTrail 当前“视觉主观感受偏差较大，Boost 激活看起来更像整屏发白，而不是飞船周围能量爆开”的问题，先按最小可玩修正方案收口。保留全屏 flash 作为补充层，但将其峰值 alpha 和持续时间明显压低；同时在 `BoostTrailRoot` 中新增局部 `BoostActivationHalo` 精灵层，复用 `ship_highlight_gg` 贴图与现有加色材质，在 Boost 启动瞬间做一次围绕飞船的短促圆形/光晕爆发，让视觉焦点重新回到船体附近，而不是屏幕本身。

### 目的

让当前实现重新靠近 `GalacticGlitch_BoostTrail_VFX_Plan.md` 后半段对 `eid_1631` 的定义：Boost 激活最关键的第一眼读感应该是“飞船周围的局部能量闪爆”，全屏 flash 只能作为轻量辅助，而不应成为主视觉。这样可以在不推翻现有 BoostTrail 框架的前提下，先把最违和的观感问题纠正过来。

### 技术

1. 在 `BoostTrailView` 中新增 `SpriteRenderer _activationHalo` 引用，作为局部激活光晕层
2. 用单个 `Tween.Custom` 驱动局部 halo 的 alpha + scale：小尺寸快速亮起，再向外扩张并衰减
3. 将全屏 flash 默认值从高峰值长时间可见，改为更短更轻的辅助冲击层
4. 在 `ResetState()` 中补充局部 halo 的完整复位，避免对象池或重复触发造成状态泄漏
5. 在 `BoostTrailPrefabCreator` 中自动创建 `BoostActivationHalo` 子节点，默认绑定 `ship_highlight_gg` 和 `ShipGlowMaterial`
6. 由于本轮修改后 Unity MCP 在域重载后未恢复 ready 状态，暂未完成 prefab 自动替换与 Play Mode 截图回归；代码层已通过 `ReadLints` 校验，本轮运行时验证待 MCP 恢复后补做

---

---

## Boost Trail VFX 局部 Halo 第二轮实测修正  2026-03-11 11:46

### 修改文件

- `Assets/Scripts/Ship/VFX/BoostTrailView.cs`  增加世界空间能量场临时关闭开关，并继续调高局部 halo 的缩放/峰值
- `Assets/Scripts/Ship/Editor/BoostTrailPrefabCreator.cs`  将局部 halo 资源从错误的 `ship_highlight_gg` 切换为程序化 halo 纹理，并重新调整默认缩放与颜色
- `Assets/Scripts/Ship/Editor/ShipBoostDebugMenu.cs`  新增直接显示/隐藏 `BoostActivationHalo` 的调试菜单，便于把“素材问题”和“触发时机问题”拆开验证
- `Assets/_Art/VFX/BoostTrail/Textures/boost_activation_halo.png`  新增程序化生成的局部 halo 纹理
- `Assets/Scenes/SampleScene.unity`  重新替换 BoostTrailRoot 后保存

### 内容

Unity MCP 恢复后，继续把前一轮“局部 halo 替代全屏主视觉”的方案落到运行时验证。首先通过重新替换 `BoostTrailRoot` 和 Play Mode 截图确认：`ship_highlight_gg` 并不是圆形局部光晕，而是一整块噪声高亮遮罩，导致画面里再次出现大面积斜方形覆盖。随后将世界空间 `BoostEnergyField` 临时降级为关闭状态，避免它继续抢主视觉，并改为使用程序化生成的 `boost_activation_halo.png` 作为局部 halo 资源。最终实测结果是：此前最离谱的“大面积斜块覆盖”问题已经被压掉，Boost 画面重新回到飞船中心附近；但新的局部 halo 仍偏弱，距离文档预期的“明显的小圆/局部能量爆发”还有差距，后续需要更合适的纹理或直接做径向 halo shader 才能真正对齐 GG 观感。

### 目的

先把当前实现从“明显错误的整块遮罩/大面积屏幕覆盖”修回到“至少不违和、且视觉重心在飞船附近”的状态，再基于 Play Mode 证据明确下一步不是继续乱调数值，而是应该升级局部 halo 的素材或渲染方式。

### 技术

1. 使用 Unity MCP 截图对比三轮结果，确认问题来源先后包括：全屏 flash 过强、错误 halo 资源导致大块遮罩、局部 halo 新资源过弱
2. 在 `BoostTrailView` 中新增 `_enableWorldEnergyField` 开关，默认关闭当前仍过于抢眼的世界空间能量场
3. 通过 `manage_texture` 生成 `boost_activation_halo.png`，替换掉不适合作为局部爆发的 `ship_highlight_gg`
4. 通过 `ShipBoostDebugMenu` 新增 `Show Activation Halo` / `Hide Activation Halo`，将局部 halo 独立从 Boost 时序中剥离出来单独验证
5. 用 `ProjectArk/Ship/Replace Ship Boost Trail (GG)` 重新替换 `Ship.prefab` 中的 `BoostTrailRoot`，再进入 Play Mode 触发 `Trigger Boost Once`
6. Play Mode 结果确认：
   - 大面积斜方形遮罩问题已消失
   - 全屏 flash 已不再主导画面
   - 当前局部 halo 仍然过弱，不足以承担文档中 `eid_1631` 的主视觉职责

---

---

## Bug Fix: DragGhostView 格子颜色与背包格子颜色不一致  2026-03-05 17:04

### 修改文件
- `Assets/Scripts/UI/DragDrop/DragGhostView.cs`（删除 CellActiveColor 硬编码，改为按 item.ItemType 动态取色）

### 内容简述
修复拖拽 Ghost 格子颜色与背包中格子颜色不一致的问题。

### 根因
- 背包格子颜色：`new Color(_typeColor.r, _typeColor.g, _typeColor.b, 0.22f)`（按类型变色：Core=蓝、Prism=紫、Sail=绿、Sat=黄）
- Ghost 格子颜色：`new Color(0f, 0.85f, 1f, 0.18f)`（硬编码固定青色）
两者颜色完全不同，导致 Ghost 看起来总是青色，而背包中是类型对应的颜色。

### 修复方案
1. 删除 `CellActiveColor` 静态常量
2. `SetShape(ItemShape, StarChartItemType)` 新增 itemType 参数
3. 用 `StarChartTheme.GetTypeColor(itemType)` + alpha=0.22 构造格子颜色（与 SlotCellView.SetItem() 完全一致）
4. `Show(item)` 调用 `SetShape(item.Shape, item.ItemType)` 传入类型
5. Ghost 整体透明度由 `CanvasGroup.alpha=0.7` 控制，格子颜色本身保持 alpha=0.22 不变

---

---

## Bug Fix: 拖拽 Ghost 鼠标锚点偏移  2026-03-05 17:12

### 修改文件
- `Assets/Scripts/UI/DragDrop/DragGhostView.cs`（新增 ComputeDragOffset，修改 FollowPointer 接受 dragOffset 参数）
- `Assets/Scripts/UI/DragDrop/DragDropManager.cs`（BeginDrag 新增 PointerEventData 参数，计算并存储 _dragOffset）
- `Assets/Scripts/UI/InventoryItemView.cs`（OnBeginDrag 传入 eventData）
- `Assets/Scripts/UI/ItemOverlayView.cs`（OnBeginDrag 传入 eventData）
- `Assets/Scripts/UI/SlotCellView.cs`（OnBeginDrag 传入 eventData）

### 内容简述
修复拖拽时 Ghost 不跟随鼠标点击位置的问题：鼠标点击部件的哪个位置，拖动时鼠标始终保持在该位置，不会偏移到 Ghost 中心。

### 根因
`FollowPointer` 直接将 Ghost 的 `localPosition` 设为鼠标的 canvas 本地坐标，而 Ghost 的 pivot=(0.5,0.5)，导致 Ghost 中心始终对齐鼠标，而非鼠标点击的原始位置。

### 技术方案
1. `DragGhostView.ComputeDragOffset(eventData)`：在 `BeginDrag` 时调用一次，计算 `pressPosition - currentPosition`（均转换到 canvas 本地坐标），得到鼠标点击点相对于当前鼠标位置的偏移量。
2. `DragDropManager._dragOffset`：存储该偏移，每帧传给 `FollowPointer`。
3. `FollowPointer(eventData, dragOffset)`：`localPosition = mouseCanvasLocal + dragOffset`，使 Ghost 的视觉锚点始终保持在用户最初点击的位置。
4. 三个拖拽发起方（InventoryItemView/ItemOverlayView/SlotCellView）均传入 `eventData`。

---

---

## Feature: GG Dash/Boost 架构重新对齐 - Boost 改为状态切换模型  2026-03-05 20:30

### 修改文件
- `Assets/Scripts/Ship/Movement/ShipBoost.cs`（完整重写：从冲量模型改为状态切换模型）
- `Assets/Scripts/Ship/Movement/ShipMotor.cs`（新增 EnterBoostState/ExitBoostState API）
- `Assets/Scripts/Ship/Aiming/ShipAiming.cs`（Boost 期间使用 BoostAngularAcceleration）
- `Assets/Scripts/Ship/Data/ShipStatsSO.cs`（移除 _boostImpulse/_boostMaxSpeedMultiplier，新增 _boostLinearDrag/_boostMaxSpeed/_boostAngularAcceleration）
- `Assets/Scripts/Ship/Editor/ShipFeelAssetCreator.cs`（同步更新默认值）
- `Assets/_Data/Ship/DefaultShipStats.asset`（同步序列化字段）

### 根因分析
对 GG 的 Boost 机制理解有误：之前将 Boost 实现为"沿船头冲量 + 放宽速度上限"。
经过仔细解读 Player.prefab 序列化数据发现：

**GG 真实的 Boost 是 StateData 状态切换，不是冲量：**
- 进入 IsBoostState → linearDrag 从 3 降到 2.5，maxMoveSpeed 从 7.5 升到 9，angularAcceleration 从 80 降到 40
- 持续 minTime=0.2s 后自动退回 IsBlueState（正常参数）
- 玩家需要在 Boost 期间持续按 WASD 加速才能突破正常速度上限
- Space 键在 GG 中只绑定 Dodge（dodgeForce=13 冲量 + IsDodgeState 0.225s），Boost 是独立解锁能力

**GG Dodge 真实参数（Player.prefab 确认）：**
- dodgeForce: 13（冲量）
- dodgeInvulnerabilityTime: 0.15s
- dodgeRechargeTime: 0.5s
- IsDodgeState: linearDrag=1.7, moveAccel=12, maxMoveSpeed=4, angularAccel=20, maxRotSpeed=50, 持续 0.225s

### 新架构
- ShipMotor.EnterBoostState(boostDrag, boostMaxSpeed)：临时替换 Rigidbody2D.linearDamping + 覆盖速度上限
- ShipMotor.ExitBoostState()：恢复 stats.LinearDrag + 清除速度覆盖
- ShipAiming：检测 motor.IsBoosting 时使用 BoostAngularAcceleration（降低旋转响应，给追击感）
- ShipBoost.ExecuteBoostAsync()：进入状态 → 等待 BoostDuration → 退出状态 → 冷却

### 数值对齐（mass=1 适配）
- boostLinearDrag: 2.5（★GG 原值）
- boostMaxSpeed: 9（★GG 原值）
- boostAngularAcceleration: 400（GG 原值 40×10，mass=1 对应）
- boostDuration: 0.2s（★GG minTime 原值）

---

## 架构基建大修 — Architecture Infrastructure Overhaul

> 以下 Phase 1–6C 为一次性架构改进，解决进入关卡构建阶段前识别出的 9 项架构短板。

---

### Phase 1: UniTask + PrimeTween 集成 — 2025-06-XX

#### 新建/修改文件

1. `Packages/manifest.json`
   - 新增 `com.cysharp.unitask` (GitHub git URL)
   - 新增 `com.kyrylokuzyk.primetween` (初始为 git URL，后改为 NPM scoped registry)

2. `Assets/Scripts/UI/ProjectArk.UI.asmdef` — 新增 `UniTask`、`PrimeTween.Runtime` 引用

3. `Assets/Scripts/Combat/ProjectArk.Combat.asmdef` — 新增 `UniTask` 引用

4. `Assets/Scripts/Ship/ProjectArk.Ship.asmdef` — 新增 `UniTask` 引用

5. `Assets/Scripts/UI/WeavingStateTransition.cs`
   - 将协程过渡重构为 `async UniTaskVoid` + `PrimeTween.Tween.Custom`
   - 新增 `CancellationTokenSource` 管理，`CancelTransition()` 方法

6. `Assets/Scripts/Combat/Enemy/EnemySpawner.cs`
   - `RespawnAfterDelay` 从协程迁移为 `async UniTaskVoid` + `UniTask.Delay`

7. `Assets/Scripts/Combat/Enemy/EnemyEntity.cs`
   - `HitFlashCoroutine` 迁移为 `HitFlashAsync`，使用 `UniTask.Delay` + `CancellationTokenSource`

8. `Assets/Scripts/Ship/Combat/ShipHealth.cs`
   - `HitFlashCoroutine` 迁移为 `HitFlashAsync`，同上

#### 目的
用 UniTask 替代 Coroutine 实现零 GC 异步编程；用 PrimeTween 替代手写 Lerp 实现高性能补间动画。

**技术**：UniTask (`async UniTaskVoid`, `UniTask.Delay`, `UniTask.Yield`)、PrimeTween (`Tween.Custom`)、`CancellationTokenSource` 生命周期管理。

---

### Phase 2: ServiceLocator 轻量依赖注入 — 2025-06-XX

#### 新建/修改文件

1. `Assets/Scripts/Core/ServiceLocator.cs` *(新建)*
   - 静态泛型 Service Locator：`Register<T>`, `Get<T>`, `Unregister<T>`, `Clear()`

2. `Assets/Scripts/Core/Pool/PoolManager.cs` — Awake 注册 / OnDestroy 注销 ServiceLocator

3. `Assets/Scripts/Combat/Enemy/EnemyDirector.cs` — 同上

4. `Assets/Scripts/Heat/HeatSystem.cs` — 同上

5. `Assets/Scripts/Combat/StarChart/StarChartController.cs` — 同上；`InitializeAllPools` 从 ServiceLocator 获取 PoolManager

6. `Assets/Scripts/UI/UIManager.cs` — 替换所有 `FindAnyObjectByType` 为 `ServiceLocator.Get<T>()`

#### 目的
消除 `FindAnyObjectByType` 的 O(n) 运行时查找和直接单例引用，建立统一的服务解析入口。

**技术**：静态泛型字典 (`Dictionary<Type, object>`)、Register/Unregister 生命周期与 MonoBehaviour.Awake/OnDestroy 绑定。

---

### Phase 3: 统一伤害管线 — 2025-06-XX

#### 新建/修改文件

1. `Assets/Scripts/Core/DamageType.cs` *(新建)* — `enum DamageType { Physical, Fire, Ice, Lightning, Void }`

2. `Assets/Scripts/Core/DamagePayload.cs` *(新建)* — 不可变结构体封装伤害事件全部数据

3. `Assets/Scripts/Core/IResistant.cs` *(新建)* — 元素抗性接口

4. `Assets/Scripts/Core/IBlockable.cs` *(新建)* — 格挡接口

5. `Assets/Scripts/Core/DamageCalculator.cs` *(新建)* — 集中式伤害计算：抗性 → 格挡 → 最终伤害

6. `Assets/Scripts/Core/IDamageable.cs` — 新增 `TakeDamage(DamagePayload)` 重载，旧签名标记 `[Obsolete]`

7. `Assets/Scripts/Combat/StarChart/StarCoreSO.cs` — 新增 `_damageType` 字段

8. `Assets/Scripts/Combat/StarChart/FiringSnapshot.cs` — `CoreSnapshot` 新增 `DamageType`

9. `Assets/Scripts/Combat/StarChart/ProjectileParams.cs` — 新增 `DamageType` 字段

10. `Assets/Scripts/Combat/StarChart/SnapshotBuilder.cs` — `BuildCoreSnapshot` 填充 `DamageType`

11. `Assets/Scripts/Combat/Projectile/Projectile.cs` — 存储 `DamageType`，碰撞时构造 `DamagePayload`

12. `Assets/Scripts/Combat/Projectile/LaserBeam.cs` — 同上

13. `Assets/Scripts/Combat/Projectile/EchoWave.cs` — 同上

14. `Assets/Scripts/Combat/Enemy/States/AttackSubState.cs` — 使用 `DamagePayload(DamageType.Physical)`

15. `Assets/Scripts/Combat/Enemy/States/StalkerStrikeState.cs` — 同上

16. `Assets/Scripts/Combat/Enemy/EnemyProjectile.cs` — 同上

17. `Assets/Scripts/Combat/Enemy/EnemyLaserBeam.cs` — 同上

18. `Assets/Scripts/Combat/Enemy/EnemyAffixController.cs` — 爆炸 / 反弹词缀使用 `DamagePayload`

19. `Assets/Scripts/Combat/Enemy/EnemyEntity.cs` — 实现 `IResistant` + `IBlockable`，`TakeDamage` 调用 `DamageCalculator.Calculate`

20. `Assets/Scripts/Ship/Combat/ShipHealth.cs` — 新增 `TakeDamage(DamagePayload)` 重载

#### 目的
建立从发射到落点的完整伤害管线，支持元素抗性、格挡减伤、伤害来源追踪。

**技术**：`readonly struct DamagePayload`、策略式接口 (`IResistant`, `IBlockable`)、集中计算器模式 (`DamageCalculator`)、渐进式迁移 (`[Obsolete]` 旧 API)。

---

### Phase 4A: 存档系统数据模型 — 2025-06-XX

#### 新建/修改文件

1. `Assets/Scripts/Core/Save/SaveData.cs` *(新建)*
   - `PlayerSaveData`, `StarChartSaveData`, `TrackSaveData`, `InventorySaveData`, `ProgressSaveData`, `SaveFlag`, `PlayerStateSaveData`

2. `Assets/Scripts/Core/Save/SaveManager.cs` *(新建)*
   - 静态工具类：`Save`, `Load`, `Delete`, `HasSave`，支持自动备份

3. `Assets/Scripts/Combat/StarChart/StarChartController.cs`
   - 新增 `ExportToSaveData()` / `ImportFromSaveData()` 方法

4. `Assets/Scripts/UI/StarChartInventorySO.cs`
   - 新增 `FindCore`, `FindPrism`, `FindLightSail`, `FindSatellite` 查找方法

#### 目的
建立序列化安全的存档数据模型和 I/O 管理器，为后续关卡进度存储奠定基础。

**技术**：`JsonUtility` 序列化、`Application.persistentDataPath`、自动 `.bak` 备份、按槽位存档。

---

### Phase 4B: 音频架构 — 2025-06-XX

#### 新建/修改文件

1. `Assets/Scripts/Core/Audio/` *(新建目录)*

2. `Assets/Scripts/Core/Audio/ProjectArk.Core.Audio.asmdef` *(新建)* — 音频程序集定义

3. `Assets/Scripts/Core/Audio/AudioManager.cs` *(新建)*
   - SFX 池化播放、音乐淡入淡出、Mixer 音量/低通滤波控制
   - ServiceLocator 注册/注销

4. `Assets/Scripts/UI/ProjectArk.UI.asmdef` — 新增 `ProjectArk.Core.Audio` 引用

5. `Assets/Scripts/Combat/ProjectArk.Combat.asmdef` — 新增 `ProjectArk.Core.Audio` 引用

#### 目的
建立集中式音频管理，支持 SFX 池化（避免运行时 AudioSource 创建/销毁）和 Mixer 控制。

**技术**：AudioSource 对象池、`AudioMixer.SetFloat` 对数音量映射、`Mathf.Log10` dB 转换。

---

### Phase 5: Combat 程序集拆分 + 事件总线 — 2025-06-XX

#### 新建/修改文件

1. `Assets/Scripts/Combat/Enemy/ProjectArk.Enemy.asmdef` *(新建)*
   - 将 Enemy 子目录独立为 `ProjectArk.Enemy` 程序集

2. `Assets/Scripts/Core/CombatEvents.cs` *(新建)*
   - 静态事件总线：`OnWeaponFired` 事件，解耦 `EnemyPerception` 与 `StarChartController`

3. `Assets/Scripts/Combat/Enemy/EnemyPerception.cs`
   - 订阅 `CombatEvents.OnWeaponFired` 替代 `StarChartController.OnWeaponFired`

4. `Assets/Scripts/Combat/StarChart/StarChartController.cs`
   - 使用 `CombatEvents.RaiseWeaponFired()` 替代直接事件

5. `Assets/Scripts/Combat/Editor/ProjectArk.Combat.Editor.asmdef`
   - 新增 `ProjectArk.Enemy` 引用

#### 目的
打破 Combat ↔ Enemy 循环依赖，通过 Core 层事件总线实现跨程序集通信。

**技术**：Assembly Definition 拆分、静态事件总线模式 (`CombatEvents`)。

---

### Phase 6A: 数据驱动状态转换 — 2025-06-XX

#### 新建/修改文件

1. `Assets/Scripts/Combat/StateTransitionRule.cs` *(新建)*
   - `TransitionCondition` 枚举、`EnemyStateType` 枚举、`StateTransitionRule` 可序列化类

2. `Assets/Scripts/Combat/Enemy/TransitionRuleEvaluator.cs` *(新建)*
   - 静态工具类：评估转换规则、将 `EnemyStateType` 解析为 `IState` 实例

3. `Assets/Scripts/Combat/Enemy/EnemyStatsSO.cs`
   - 新增 `_transitionOverrides` 数组

4. `Assets/Scripts/Combat/Enemy/States/ChaseState.cs`
   - 优先检查数据驱动规则，无匹配时 fallback 到硬编码逻辑

#### 目的
允许策划在 SO Inspector 中配置 AI 状态转换规则，无需修改代码即可调整行为。

**技术**：数据驱动规则评估、策略模式状态解析、`StateTransitionRule` SO 可序列化数组。

---

### Phase 6B: 单元测试基础设施 — 2025-06-XX

#### 新建/修改文件

1. `Assets/Scripts/Core/Tests/ProjectArk.Core.Tests.asmdef` *(新建)*

2. `Assets/Scripts/Combat/Tests/ProjectArk.Combat.Tests.asmdef` *(新建)*

3. `Assets/Scripts/Core/Tests/DamageCalculatorTests.cs` *(新建)*
   - 抗性、格挡、边界情况测试

4. `Assets/Scripts/Combat/Tests/StateMachineTests.cs` *(新建)*
   - OnEnter/OnExit 顺序、Tick、状态切换验证

5. `Assets/Scripts/Combat/Tests/SnapshotBuilderTests.cs` *(新建)*
   - 棱镜修正聚合 (Add/Multiply)、弹丸计数上限

6. `Assets/Scripts/Combat/Tests/SlotLayerTests.cs` *(新建)*
   - 装备/卸下逻辑、槽位尺寸、占用验证

7. `Assets/Scripts/Core/Tests/HeatSystemTests.cs` *(新建)*
   - ServiceLocator 功能测试

#### 目的
建立单元测试基础设施，为核心系统提供回归保护。

**技术**：NUnit、Unity Test Framework、运行时创建 ScriptableObject 实例、反射设置私有字段。

---

---

## 射击系统核心框架（基于 GDD.md）

### 8. Core 对象池系统 — 2026-02-07 18:00

**新建文件：**
- `Assets/Scripts/Core/Pool/IPoolable.cs` — 池化接口：`OnGetFromPool()`, `OnReturnToPool()`
- `Assets/Scripts/Core/Pool/GameObjectPool.cs` — 预制体池，包装 `UnityEngine.Pool.ObjectPool`
- `Assets/Scripts/Core/Pool/PoolManager.cs` — 单例池注册中心，按 prefab 管理所有池
- `Assets/Scripts/Core/Pool/PoolReference.cs` — 挂在池对象上，提供 `ReturnToPool()` 自回收

**内容：** 通用对象池基础设施。GameObjectPool 负责实例化/回收/预热，自动调用 IPoolable 回调。PoolManager 提供全局访问点，按 prefab InstanceID 索引。PoolReference 让池中对象能自行回池。

**目的：** 满足架构原则"运行时生成的对象必须使用对象池"，为子弹、特效等提供底层支持。

**技术：** `UnityEngine.Pool.ObjectPool<GameObject>`, Singleton 模式, `DontDestroyOnLoad`, `IPoolable` 接口回调。

---

### 9. InputHandler 扩展 Fire 输入 — 2026-02-07 18:00

**修改文件：**
- `Assets/Scripts/Ship/Input/InputHandler.cs`

**内容：** 添加 Fire action 读取：
- `_fireAction` 字段绑定 `ShipActions` 中的 Fire action
- `IsFireHeld` (bool) 属性 — 射击按钮是否按住
- `OnFirePressed` / `OnFireReleased` 事件
- `performed`/`canceled` 回调自动更新状态

**目的：** 保持 InputHandler 作为唯一输入适配器的职责，WeaponSystem 不直接绑定 Input Action。

**技术：** `InputAction.performed`/`canceled` 回调，事件驱动。

---

### 10. ShipMotor 扩展后坐力接口 — 2026-02-07 18:00

**修改文件：**
- `Assets/Scripts/Ship/Movement/ShipMotor.cs`

**内容：** 添加 `ApplyImpulse(Vector2 impulse)` 公共方法，直接修改 `linearVelocity`。

**目的：** 为武器后坐力提供接口。冲量会被现有减速逻辑自然消化。

**技术：** 直接 `linearVelocity +=`，利用已有减速模型衰减。

---

### 11. Combat 程序集 + WeaponStatsSO — 2026-02-07 18:00

**新建文件：**
- `Assets/Scripts/Combat/ProjectArk.Combat.asmdef` — 战斗程序集，引用 Core + Ship
- `Assets/Scripts/Combat/Data/WeaponStatsSO.cs`

**内容：** 武器数据 ScriptableObject，包含：
- 射击参数：FireRate(5/s), Spread(0°)
- 弹道参数：BaseDamage(10), ProjectileSpeed(20), Lifetime(2.0s), Knockback(1.0)
- 后坐力：RecoilForce(0.5)
- 预制体引用：ProjectilePrefab, MuzzleFlashPrefab, ImpactVFXPrefab
- 音频引用：FireSound, HitSound, FireSoundPitchVariance(0.1)

**目的：** 数据驱动武器配置，未来星图系统修正数值时只需包装此 SO。

**技术：** ScriptableObject, `[CreateAssetMenu]`, 单向程序集依赖 (Combat → Ship → Core)。

---

### 12. FirePoint + IProjectileModifier — 2026-02-07 18:00

**新建文件：**
- `Assets/Scripts/Combat/Weapon/FirePoint.cs` — 炮口位置标记组件
- `Assets/Scripts/Combat/Projectile/IProjectileModifier.cs` — 星图扩展接口

**内容：** FirePoint 是极简标记组件，暴露 `Position` 和 `Direction`。IProjectileModifier 定义三个钩子：`OnProjectileSpawned`/`OnProjectileUpdate`/`OnProjectileHit`。

**目的：** FirePoint 解耦炮口位置与飞船逻辑。IProjectileModifier 为未来星图系统（追踪导弹、回旋镖等）预留扩展点。

**技术：** 标记组件模式, 策略模式接口。

---

### 13. Projectile 子弹系统 — 2026-02-07 18:00

**新建文件：**
- `Assets/Scripts/Combat/Projectile/Projectile.cs`

**内容：** MonoBehaviour + IPoolable，负责：
- `Initialize()` 接收方向/速度/伤害等参数，设置 `linearVelocity`
- `Update()` 倒计时生命周期，调用 modifier 钩子
- `OnTriggerEnter2D` 处理碰撞（回池 + 生成命中特效）
- IPoolable 回调清理 TrailRenderer 和状态
- 暴露 `Direction`/`Speed`/`Damage`/`Knockback` 供 modifier 读写

**目的：** 实体投射物，直线飞行，碰撞即销毁（回池），支持星图行为修改。

**技术：** Kinematic Rigidbody2D + Trigger Collider, `linearVelocity`, IPoolable, PoolReference 自回收。

---

### 14. PooledVFX 池化特效 — 2026-02-07 18:00

**新建文件：**
- `Assets/Scripts/Combat/VFX/PooledVFX.cs`

**内容：** MonoBehaviour + IPoolable，激活时播放 ParticleSystem，播放完毕自动通过 PoolReference 回池。

**目的：** 炮口焰和命中特效的通用池化容器，避免 Instantiate/Destroy。

**技术：** ParticleSystem 生命周期检测, IPoolable + PoolReference 自动回收。

---

### 15. WeaponSystem 武器核心 — 2026-02-07 18:00

**新建文件：**
- `Assets/Scripts/Combat/Weapon/WeaponSystem.cs`

**内容：** 核心射击编排器，Update 中检查 `IsFireHeld + 冷却计时`：
- 从 ShipAiming 读取 `FacingDirection` 作为弹道方向
- 从 FirePoint 读取生成位置
- 应用 Spread 随机偏转
- 从 PoolManager 获取子弹实例并 `Initialize()`
- 生成炮口焰（池化）
- 调用 `ShipMotor.ApplyImpulse()` 施加后坐力
- `AudioSource.PlayOneShot()` + 音调随机化
- 触发 `OnWeaponFired` 事件

**目的：** GDD 核心循环的完整实现——按下即射、按住连发、后坐力、VFX、音效。

**技术：** 冷却计时器模式, 对象池, 事件驱动, `RequireComponent` 依赖声明。

---

---

## 杂项

### Art 文件夹结构 — 2026-02-07 17:30

**新建目录：**
- `Assets/Art/Sprites/{Ship, Enemies, Projectiles, Environment, Items, UI}/`
- `Assets/Art/Animations/{Ship, Enemies, Effects}/`
- `Assets/Art/Tiles/{Tilesets, Palettes}/`
- `Assets/Art/VFX/`
- `Assets/Art/Materials/`

**目的：** 为美术资源建立分类存放结构。

---

### Docs 文件夹 — 2026-02-07 17:35

**新建目录：**
- `Docs/Design/` — 系统设计文档
- `Docs/GDD/` — 游戏策划案
- `Docs/ImplementationLog/` — 实现日志（本文件）

**目的：** 项目文档集中管理，与 Assets 分离。

---

---

## 星图编织系统 Batch 1：热量系统 + 星图数据基础（基于 GDD.md 第二部分）

### 16. Heat 程序集 + HeatStatsSO — 2026-02-08 00:00

**新建文件：**
- `Assets/Scripts/Heat/ProjectArk.Heat.asmdef` — 热量程序集，引用 Core
- `Assets/Scripts/Heat/Data/HeatStatsSO.cs` — 热量配置 SO

**内容：** 新建独立热量模块。HeatStatsSO 包含：MaxHeat(100), CoolingRate(15/s), OverheatDuration(3.0s), OverheatThreshold(1.0)。派生属性 `OverheatHeatValue` = MaxHeat * Threshold。

**目的：** 数据驱动的热量配置，与飞船数据分离，独立程序集保持模块边界。

**技术：** Assembly Definition, ScriptableObject, `[CreateAssetMenu]`。

---

### 17. HeatSystem — 热量状态管理 — 2026-02-08 00:00

**新建文件：**
- `Assets/Scripts/Heat/HeatSystem.cs`

**内容：** MonoBehaviour，挂在 Ship 上，管理热量状态机（Normal ↔ Overheated）：
- `AddHeat(float)` — 增加热量，检查过热触发
- `ReduceHeat(float)` / `ResetHeat()` — 减热/清零（为未来星图部件预留）
- `CanFire()` — 快速检查是否可开火
- Normal 状态：被动散热 `coolingRate * dt`
- Overheated 状态：3 秒倒计时后清零热量恢复
- 事件：`OnHeatChanged(float normalized)`, `OnOverheated`, `OnCooldownComplete`

**目的：** GDD 第 4 节热量/过热机制的完整实现。限制无限射击，增加战术决策。

**技术：** 状态机模式, 事件驱动, `Mathf.Clamp`。

---

### 18. WeaponSystem 热量集成 — 2026-02-08 00:00

**修改文件：**
- `Assets/Scripts/Combat/Data/WeaponStatsSO.cs` — 添加 `_heatCostPerShot`(5f) 字段
- `Assets/Scripts/Combat/Weapon/WeaponSystem.cs` — 集成热量检查和消耗
- `Assets/Scripts/Combat/ProjectArk.Combat.asmdef` — 添加 `ProjectArk.Heat` 引用

**内容：**
- WeaponStatsSO 新增 Heat header 和 `HeatCostPerShot` 属性
- WeaponSystem 添加 `[SerializeField] HeatSystem _heatSystem`（可选依赖）
- Update 中增加 `_heatSystem == null || _heatSystem.CanFire()` 守卫
- Fire 末尾调用 `_heatSystem?.AddHeat(heatCostPerShot)`
- 同时清理了之前的调试诊断日志

**目的：** 射击消耗热量，过热时自动停火。HeatSystem 为可选依赖，不挂则无限射击（向后兼容）。

**技术：** 可选依赖模式（SerializeField + null 检查），null 条件运算符 `?.`。

---

### 19. 星图数据层 — 枚举与基类 — 2026-02-08 00:00

**新建文件：**
- `Assets/Scripts/Combat/StarChart/StarChartEnums.cs` — 所有星图相关枚举
- `Assets/Scripts/Combat/StarChart/StarChartItemSO.cs` — 星图部件抽象基类

**内容：**
- 枚举：`StarChartItemType`(Core/Prism/LightSail/Satellite), `CoreFamily`(Matter/Light/Echo/Anomaly), `PrismFamily`(Fractal/Rheology/Tint), `ModifierOperation`(Add/Multiply), `WeaponStatType`(10 种可修改属性)
- StarChartItemSO：抽象基类，包含 DisplayName, Description, Icon, SlotSize(1-3), HeatCost, 抽象属性 `ItemType`

**目的：** 建立星图系统的类型体系，为所有部件提供统一的数据基类。

**技术：** 枚举类型, 抽象类, ScriptableObject 继承。

---

### 20. StarCoreSO — 星核数据 — 2026-02-08 00:00

**新建文件：**
- `Assets/Scripts/Combat/StarChart/StarCoreSO.cs`

**内容：** 继承 StarChartItemSO，包含完整的发射器参数：Family, ProjectilePrefab, FireRate, BaseDamage, ProjectileSpeed, Lifetime, Spread, Knockback, RecoilForce, VFX/Audio 引用。与现有 WeaponStatsSO 字段对应。

**目的：** 星核是发射源原型，4 个家族（实相/光谱/波动/异象）决定子弹的根本行为方式。Batch 2 管线重构时将替代 WeaponStatsSO。

**技术：** ScriptableObject 继承, `[CreateAssetMenu]`。

---

### 21. StatModifier + PrismSO — 棱镜数据 — 2026-02-08 00:00

**新建文件：**
- `Assets/Scripts/Combat/StarChart/StatModifier.cs` — 数值修改结构体
- `Assets/Scripts/Combat/StarChart/PrismSO.cs` — 棱镜 SO

**内容：** StatModifier 是 `[Serializable]` 结构体，包含 Stat(枚举)、Operation(Add/Multiply)、Value。PrismSO 包含 Family(分形/流变/晕染)、StatModifier 数组、可选的行为注入预制体。

**目的：** 棱镜是"垂直注入"修正器，效果平均分配给同轨道下方的所有星核。运行时应用逻辑在 Batch 2 实现。

**技术：** `[Serializable]` struct, ScriptableObject 继承, 数据驱动修正器模式。

---

### 22. LightSailSO + SatelliteSO — 光帆和伴星数据骨架 — 2026-02-08 00:00

**新建文件：**
- `Assets/Scripts/Combat/StarChart/LightSailSO.cs` — 光帆 SO
- `Assets/Scripts/Combat/StarChart/SatelliteSO.cs` — 伴星 SO

**内容：**
- LightSailSO：ConditionDescription, EffectDescription（策划可读文本）。全机唯一引擎插槽。
- SatelliteSO：TriggerDescription, ActionDescription, InternalCooldown(0.5s)。IF-THEN 事件响应器。

**目的：** 定义 SO 骨架。光帆监听飞船状态提供增益（多普勒效应、擦弹引擎等），伴星是自动化模组（复仇之月、清道夫等）。运行时逻辑在 Batch 3 实现。

**技术：** ScriptableObject 继承, `[TextArea]` 描述字段。

---

### 23. 清理调试日志 — 2026-02-08 00:00

**修改文件：**
- `Assets/Scripts/Ship/Input/InputHandler.cs` — 移除 `Debug.Log("[InputHandler] Fire pressed")`
- `Assets/Scripts/Combat/Weapon/WeaponSystem.cs` — 移除启动诊断块和 Fire() 中的调试 warning

**目的：** 移除上次射击系统调试时临时添加的日志，保持代码整洁。

---

---

## Boost Trail VFX 持续拖尾分层重调（方案 A）  2026-03-11 15:47

### 修改文件

- `Assets/Scripts/Ship/VFX/BoostTrailView.cs`  收紧 Boost 持续阶段的主拖尾默认时长与宽度，避免整条尾巴过厚过长
- `Assets/Scripts/Ship/Editor/BoostTrailPrefabCreator.cs`  重新调校 `MainTrail / FlameTrail_R / FlameTrail_B / FlameCore / EmberTrail / EmberGlow / EmberSparks` 的默认参数，使各层职责更接近 GG
- `Assets/_Art/VFX/BoostTrail/Materials/mat_trail_main_effect.mat`  调整 `TrailMainEffect` 的亮度与边缘 glow 默认参数，提升主拖尾的内部发光和轮廓读感
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容

针对“Boost 启动瞬间已经比之前更对，但持续推进阶段的火焰拖尾依然和 GG 差很远”的问题，本轮按方案 A 收口：不新增新的系统层级，而是直接把现有层的职责重新分开。`MainTrail` 从先前偏粗、偏长、像普通发光尾巴的状态，收紧为更像 GG 的“头细、中段最饱满、尾部收尖”的主轮廓；`FlameTrail_R/B` 则从普通连续粒子改成更细、更快、更贴近船尾两侧的科技火焰丝带；`FlameCore` 强化为贴近船尾中心的亮核；`EmberTrail` 改为承担持续阶段的残留余烬，而 `EmberSparks` 明确只负责启动瞬间的白热飞溅，不再在持续阶段抢主视觉。同时顺手把 `TrailMainEffect` 材质从几乎全默认值拉回到“有边缘 glow、有更亮的内部发光”的状态，避免主拖尾继续像一条静态普通贴图。

### 目的

先把当前效果从“启动瞬间像一点 GG，但持续阶段完全不像”修到“至少能明显读出 GG 的主轮廓 + 两侧火焰丝带 + 中心亮核 + 余烬尾部”这一层。这样后续若还要继续精修，就可以围绕更细的 shader 细节和实际截图对比继续推进，而不是还停留在层级职责没有分开的阶段。

### 技术

1. 将 `BoostTrailView` 的默认 `trailTime / trailWidthMultiplier` 从偏夸张的持续宽尾，收敛为更适合 GG 读感的持续长度与厚度
2. 在 `BoostTrailPrefabCreator` 中为 `FlameTrail_R/B` 补上更明确的 Box 发射形状、颜色渐变、SizeOverLifetime、Noise 和 Stretched Billboard，使其更像两侧高速火焰丝带
3. 将 `FlameCore` 调整为 Local Simulation 的中心亮核，缩小发射半径并增强短命亮核的颜色变化
4. 将 `EmberTrail` 调整为更轻、更短命、偏残留感的余烬层，避免和主火焰混成一层
5. 将 `EmberSparks` 明确压回启动瞬间的爆发层，减少粒子数并强调短命高亮飞溅
6. 调整 `MainTrail` 的 width curve、alpha curve 与 `mat_trail_main_effect` 的 `_Child2/_Child3/_Child4/_Child5`，提高主拖尾的内部发光与边缘 glow 读感
7. 由于当前回合尚未通过 Unity Editor 实际重建 prefab / scene 并截图回归，本轮先完成代码与默认资产参数调整；运行时观感验证待下一步在 Unity 中执行 `BoostTrailRoot` 重建后补做

---

---

## Boost Trail 运行链路全量同步修复  2026-03-11 17:24

### 修改文件

- `Assets/_Prefabs/VFX/BoostTrailRoot.prefab`  重新生成并落盘最新 `BoostTrailView` 序列化参数，统一主拖尾材质与子层默认参数
- `Assets/_Prefabs/Ship/Ship.prefab`  重新替换 `ShipVisual/BoostTrailRoot` 嵌套实例并刷新 `ShipView._boostTrailView` 引用
- `Assets/Scenes/SampleScene.unity`  重新执行场景绑定并保存，使 `BoostTrailView._boostBloomVolume` 正式写回场景
- `Assets/Scripts/Ship/Editor/BoostTrailPrefabCreator.cs`
- `Assets/Scripts/Ship/Editor/MaterialTextureLinker.cs`
- `Assets/Scripts/Ship/VFX/BoostTrailView.cs`
- `Assets/_Art/VFX/BoostTrail/Materials/mat_trail_main.mat`
- `Assets/_Art/VFX/BoostTrail/Materials/mat_trail_main_effect.mat`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容

针对“文件已经改了，但真正运行时看到的 Ship 仍然停在旧版 BoostTrail 参数和旧主拖尾材质上”的问题，本轮不再只检查 YAML，而是把整条运行链路重新同步了一遍：先通过项目内置 Editor 菜单重新执行 `Link Material Textures`、`Create BoostTrailRoot Prefab`、`Replace Ship Boost Trail (GG)` 和 `Setup Boost Trail Scene References (GG)`，再强制确认 `BoostTrailRoot.prefab` 中 `BoostTrailView` 的 `_trailTime / _trailWidthMultiplier` 已经从旧的 `2.6 / 2.35` 切回最新 `2.2 / 2.75`，并确认当前场景内 `Ship/ShipVisual/BoostTrailRoot/MainTrail` 的实际材质已经变为 `mat_trail_main`，不再停留在旧的 `mat_trail_main_effect` 路线。同时将场景中的 `BoostTrailBloomVolume` 重新绑定并保存到 `SampleScene`，避免 Bloom 只在当前内存会话里有效、重开场景后丢失。

### 目的

把“代码默认值、prefab 资产、Ship 嵌套实例、当前场景运行对象、scene-only Bloom 引用”这五层统一到同一状态，消除“看起来资源都在，但实际一跑还是旧效果”的伪完成状态。这样你现在进入当前场景直接运行时，看到的就是最新一轮主拖尾、侧喷焰和 Bloom 绑定都已经生效的版本。

### 技术

1. 使用项目内置 Editor 自动化菜单重建 `BoostTrailRoot`，确保 `BoostTrailView` 新序列化默认值真正写回 prefab，而不只停留在脚本源码默认值中
2. 通过 `ShipBoostTrailPrefabReplacer` 重新实例化并替换 `Ship.prefab` 内的 `BoostTrailRoot`，消除旧 nested prefab 残留和失配的 fileID
3. 通过 `ShipBoostTrailSceneBinder` 重建并回填场景中的 `BoostTrailBloomVolume` 绑定，使 `_boostBloomVolume` 从“prefab 中必然为 null 的 scene object”变成“当前场景可运行且已保存的引用”
4. 用 Unity MCP 再次核对运行态对象，确认：
   - `BoostTrailView._trailTime = 2.2`
   - `BoostTrailView._trailWidthMultiplier = 2.75`
   - `BoostTrailView._boostBloomVolume -> BoostTrailBloomVolume`
   - `MainTrail.sharedMaterial = Assets/_Art/VFX/BoostTrail/Materials/mat_trail_main.mat`
5. 保存 `Assets/Scenes/SampleScene.unity`，把场景级修复从内存状态正式落盘

---

## [2026-03-16 12:30] Research: Slay the Spire 2 完整解包

### 新建文件
- `Docs/Reference/STS2_Unpack_Report.md`

### 内容
完整解包 Slay the Spire 2（Godot 4.5.1 + C# .NET 9）：
1. GDRE Tools v2.4.0 解包 SlayTheSpire2.pck（1.57 GB）到 C:\Temp\StS2_Ripped：3245个C#脚本还原、907个.tscn场景、3365张PNG纹理、24139个文件
2. ilspycmd 反编译 sts2.dll（8.8 MB）到 C:\Temp\StS2_CSharp：3299个.cs文件、完整MegaCrit.Sts2.*命名空间还原
3. 关键踩坑：PowerShell管道会截断GDRE进程，必须用cmd /c .bat执行；参数是--output=不是--output-dir=

### 目的
为 Project Ark 卡牌/战斗系统设计提供参考，可直接查阅 StS2 的命令-动作架构、卡牌系统、Hooks Mod API、多人游戏同步等源码

---

---

## MS1: 飞船基础移动 + Twin-Stick 瞄准

### 1. 项目结构搭建 — 2026-02-07 01:49

**新建文件：**
- `Assets/Scripts/Core/ProjectArk.Core.asmdef` — Core 程序集，根模块，无外部依赖
- `Assets/Scripts/Ship/ProjectArk.Ship.asmdef` — Ship 程序集，引用 Core + Unity.InputSystem
- `Assets/Scripts/UI/ProjectArk.UI.asmdef` — UI 程序集，引用 Core

**目的：** 用 Assembly Definition 划分模块边界，强制解耦，加速编译。

**技术：** Unity Assembly Definition，模块化架构。

---

### 2. ShipStatsSO — 飞船数据配置 — 2026-02-07 01:49

**新建文件：**
- `Assets/Scripts/Ship/Data/ShipStatsSO.cs`
- `Assets/_Data/Ship/DefaultShipStats.asset`（SO 实例）

**内容：** ScriptableObject 数据容器，暴露 4 个手感参数：
- `MoveSpeed` = 12（最大移动速度）
- `Acceleration` = 45（加速度）
- `Deceleration` = 25（减速度，低于加速度以产生惯性滑行）
- `RotationSpeed` = 720（转向速度 °/s）

**目的：** 数据驱动，所有数值集中配置，无需改代码即可调参。

**技术：** ScriptableObject, `[CreateAssetMenu]`, 数据驱动模式。

---

### 3. ShipActions.inputactions — 输入配置 — 2026-02-07 01:49

**新建文件：**
- `Assets/Input/ShipActions.inputactions`

**内容：** 两个 Action Map：
- **Ship Map：** Move（WASD/左摇杆）、AimPosition（鼠标位置）、AimStick（右摇杆）、Fire、Dash、Interact（Hold 0.3s）、Pause
- **UI Map：** Navigate、Submit、Cancel、Point、Click、ScrollWheel

**控制方案：** Keyboard&Mouse / Gamepad 双方案

**目的：** 统一管理所有输入绑定，支持键鼠和手柄。

**技术：** New Input System 1.18.0, 2DVector Composite (mode=2 DigitalNormalized 防止对角线超速), StickDeadzone Processor。

---

### 4. InputHandler — 输入适配器 — 2026-02-07 01:49

**新建文件：**
- `Assets/Scripts/Ship/Input/InputHandler.cs`

**内容：** MonoBehaviour，读取 New Input System 并暴露处理后的数据：
- `MoveInput` — 归一化移动向量
- `AimWorldPosition` — 世界空间瞄准位置
- `AimDirection` — 瞄准方向（归一化）
- `HasAimInput` — 是否有瞄准输入
- `OnDeviceSwitched` 事件 — 键鼠/手柄切换通知

**目的：** 隔离输入层，上层系统（ShipMotor、ShipAiming）不直接接触 Input System API。

**技术：** New Input System `InputActionAsset` 手动绑定, `Camera.ScreenToWorldPoint` 鼠标坐标转换, 设备热切换检测。

---

### 5. ShipMotor — 物理移动 — 2026-02-07 01:49

**新建文件：**
- `Assets/Scripts/Ship/Movement/ShipMotor.cs`

**内容：** MonoBehaviour，FixedUpdate 中控制 Rigidbody2D.linearVelocity：
- 有输入时：向目标速度加速（步长 = Acceleration * dt）
- 无输入时：保持方向匀减速（步长 = Deceleration * dt）
- 强制最大速度上限防止超速
- 暴露 `OnSpeedChanged(float normalized)` 事件供 VFX/音效订阅

**Rigidbody2D 配置要求：** Dynamic, Gravity=0, Damping=0, Interpolate, Freeze Rotation Z

**目的：** 实现 Minishoot 风格的不对称惯性移动（快启动、慢滑行）。

**技术：** Rigidbody2D `linearVelocity`（Unity 6 新 API）, `Vector2.MoveTowards`, 不对称加减速。

---

### 6. ShipAiming — 旋转瞄准 — 2026-02-07 01:49

**新建文件：**
- `Assets/Scripts/Ship/Aiming/ShipAiming.cs`

**内容：** MonoBehaviour，LateUpdate 中旋转飞船朝向瞄准方向：
- `Atan2` 计算目标角度，-90° 偏移对齐 Sprite 朝上（+Y = forward）
- `RotationSpeed > 0` 时用 `MoveTowardsAngle` 匀速旋转
- `RotationSpeed ≈ 0` 时瞬间转向
- 暴露 `OnAimAngleChanged(float degrees)` 事件

**目的：** Twin-Stick 瞄准，飞船独立于移动方向旋转朝向目标。

**技术：** `Mathf.MoveTowardsAngle`（匀速旋转，非 Slerp）, `Quaternion.Euler`, LateUpdate 保证在移动后执行。

---

### 7. Ship Prefab 组装 — 2026-02-07 17:03

**新建文件：**
- `Assets/_Prefabs/Ship/Ship.prefab`

**内容：** 飞船预制体，挂载组件：Rigidbody2D + InputHandler + ShipMotor + ShipAiming

**目的：** 场景中可直接实例化的完整飞船对象。

---

---

## Boost Trail VFX 主拖尾与两侧喷焰继续贴近 GG  2026-03-11 15:54

### 修改文件

- `Assets/Scripts/Ship/VFX/BoostTrailView.cs`  再次调整 Boost 持续阶段默认 `trailTime / trailWidthMultiplier`，让主拖尾更厚但更短，更接近 GG 的主体读感
- `Assets/Scripts/Ship/Editor/BoostTrailPrefabCreator.cs`  继续强化 `MainTrail` 与 `FlameTrail_R/B`：主拖尾切回更可控的火焰轮廓材质，两侧火焰改成更贴边、更定向的喷焰丝带
- `Assets/Scripts/Ship/Editor/MaterialTextureLinker.cs`  将 `mat_trail_main` 的默认连线从 `trail_main_spritesheet` 改为 `vfx_boost_techno_flame`
- `Assets/_Art/VFX/BoostTrail/Materials/mat_trail_main.mat`  将主拖尾基础纹理切换为 `vfx_boost_techno_flame`，并提高橙黄主色亮度
- `Assets/_Prefabs/VFX/BoostTrailRoot.prefab`  手动同步主拖尾与 `FlameTrail_R/B` 的关键参数，避免 Unity 菜单链路失联时 prefab 停在旧版本
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容

在上一轮“先把层级职责拆开”的基础上，这一轮继续同时推进你点名的两个问题：主拖尾本体和两侧火焰丝带。过程中发现 `trail_main_spritesheet / trail_second_spritesheet` 更像 RenderDoc 抓出的整屏画面片段，而不是适合作为拖尾轮廓直接重复采样的可控火焰贴图，因此本轮不再继续把主观读感押在 `TrailMainEffect` 上，而是把 `MainTrail` 的默认材质优先切回 `mat_trail_main + vfx_boost_techno_flame`。这样主拖尾就能回到“橙黄主体 + 深橙收尾 + 沿长度重复的火焰片段”这一更可控、也更接近 GG 第一眼印象的状态。与此同时，两侧 `FlameTrail_R/B` 进一步从“左右两团拉长粒子”改成“更靠近船尾两侧、带轻微外撇角度、长度更长、密度更像按距离喷出”的定向喷焰丝带。

### 目的

把当前 Boost 持续阶段从“结构上已经分层，但主轮廓和侧喷焰仍然不够像 GG”继续推进到“第一眼就能同时读到主拖尾主体和两侧喷焰分工”的层级。这样后续再做精修时，就可以围绕强弱、颜色和节奏微调，而不是还在纠结主拖尾到底该靠哪套纹理和材质来成立。

### 技术

1. 将 `MainTrail` 默认材质优先切回 `mat_trail_main`，并把其 `_BaseMap` 改为 `vfx_boost_techno_flame`，避免继续依赖更像整屏截图的 trail 大图纹理
2. 将 `MainTrail` 的时间、宽度、width curve、gradient、textureMode 和 textureScale 调整为更像 GG 的“主体更厚、尾部更快收尖、沿长度重复火焰形状”的轮廓
3. 将 `FlameTrail_R/B` 的发射位置外移并增加轻微外撇角度，使两侧火焰明显从船尾两边喷出，而不是堆在中心附近
4. 将 `FlameTrail_R/B` 的 `rateOverDistance` 收回到更接近 GG 的 `15`，并提高 `Stretch` 渲染的 `velocityScale / lengthScale`，强化喷焰丝带读感
5. 由于 Unity MCP 在本轮后段进入 `ping not answered` 状态，未能可靠完成菜单回包；为避免当前 prefab 停在旧轮次，直接对现有 `BoostTrailRoot.prefab` 做了关键参数手动同步

---

---

## 星图编织系统 Batch 2：槽位架构 + 发射管线重构

### 24. ProjectileParams + Projectile 重构 — 2026-02-08 14:00

**新建文件：**
- `Assets/Scripts/Combat/StarChart/ProjectileParams.cs` — 轻量 readonly struct

**修改文件：**
- `Assets/Scripts/Combat/Projectile/Projectile.cs` — 新 Initialize 重载

**内容：** ProjectileParams 是 readonly struct（Damage, Speed, Lifetime, Knockback, ImpactVFXPrefab），替代 WeaponStatsSO 作为 Projectile.Initialize 参数。Projectile 新增 `_impactVFXPrefab` 字段替代 `_weaponStats` 引用。旧 Initialize(WeaponStatsSO) 标 `[Obsolete]` 并内部转调新版。

**目的：** 解耦 Projectile 对 WeaponStatsSO 的依赖，使星图管线可以传递棱镜修正后的数值。

**技术：** readonly struct（零分配）, `[Obsolete]` 向后兼容过渡, FromWeaponStats 工厂方法。

---

### 25. SlotLayer 槽位管理 — 2026-02-08 14:00

**新建文件：**
- `Assets/Scripts/Combat/StarChart/SlotLayer.cs` — 泛型槽位层

**内容：** `SlotLayer<T> where T : StarChartItemSO`，3 格固定容量。TryEquip 查找连续空闲格子，Unequip 释放占用，支持 SlotSize 1-3 的部件。bool[] 跟踪占用状态。

**目的：** GDD 第 3 节"三明治结构"的数据层实现。防止大部件超出容量、处理 Size=2/3 的连续占位逻辑。

**技术：** 泛型约束, 连续空间查找算法, 纯 C# 类（非 MonoBehaviour）。

---

### 26. FiringSnapshot 快照数据 — 2026-02-08 14:00

**新建文件：**
- `Assets/Scripts/Combat/StarChart/FiringSnapshot.cs` — CoreSnapshot + TrackFiringSnapshot

**内容：**
- `CoreSnapshot`：单个核心修正后的完整参数（数值 + prefab + 音效 + IProjectileModifier 列表），提供 `ToProjectileParams()` 转换。
- `TrackFiringSnapshot`：一个轨道的齐射快照（CoreSnapshot 列表 + 总热量/后坐力/冷却/弹数）。

**目的：** 作为 SnapshotBuilder 的输出和 StarChartController 的输入，在快照构建和消费之间传递不可变数据。

**技术：** 值对象模式, 不可变快照。

---

### 27. SnapshotBuilder 快照构建器 — 2026-02-08 14:00

**新建文件：**
- `Assets/Scripts/Combat/StarChart/SnapshotBuilder.cs` — 静态工具类

**内容：** `Build(cores, prisms)` 静态方法实现 GDD 第 5 节发射管线的 Step 2~4：
- 聚合所有棱镜 StatModifier（Multiply 累乘，Add 累加）
- 对每个核心：`result = (base * totalMul) + (totalAdd / coreCount)`
- 收集 Tint 棱镜的 IProjectileModifier
- 计算总热量（核心修正值 + 棱镜自身 flat）、总后坐力、最慢冷却
- 弹幕硬上限：>20 颗截断，多余转伤害加成（5%/颗）

**目的：** 棱镜修正算法核心——Add 平均分配（GDD 要求 +10/2核心=各+5），Multiply 全额应用。

**技术：** 静态复用字典（避免 GC），比例裁剪算法，硬上限保护。

---

### 28. WeaponTrack 武器轨道 — 2026-02-08 14:00

**新建文件：**
- `Assets/Scripts/Combat/Weapon/WeaponTrack.cs` — 纯 C# 类

**内容：** 持有 `SlotLayer<StarCoreSO>` + `SlotLayer<PrismSO>` 双层。提供 EquipCore/EquipPrism/Unequip 方法（dirty 标记 → 下次 TryFire 时重建快照）。Tick(dt) 更新冷却计时。TryFire() 返回缓存的 TrackFiringSnapshot 并设置冷却。

**目的：** 每个轨道是独立的射击通道（主/副），拥有独立冷却和槽位配置。快照缓存避免每帧重建。

**技术：** 脏标记 + 缓存模式, 委托 PoolManager 管理池, 纯 C# 类。

---

### 29. InputHandler 副武器输入 — 2026-02-08 14:00

**修改文件：**
- `Assets/Input/ShipActions.inputactions` — 新增 `FireSecondary` action
- `Assets/Scripts/Ship/Input/InputHandler.cs` — 新增副武器输入

**内容：**
- ShipActions：新增 FireSecondary 按钮绑定（Mouse/rightButton + Gamepad/leftTrigger）
- InputHandler：新增 `IsSecondaryFireHeld` 属性, `OnSecondaryFirePressed`/`OnSecondaryFireReleased` 事件

**目的：** 支持双轨道（主/副武器）的独立输入。

**技术：** New Input System action 扩展。

---

### 30. StarChartController 星图控制器 — 2026-02-08 14:00

**新建文件：**
- `Assets/Scripts/Combat/StarChart/StarChartController.cs` — 顶层 MonoBehaviour

**修改文件：**
- `Assets/Scripts/Combat/Weapon/WeaponSystem.cs` — 类标 `[Obsolete]`
- `Assets/Scripts/Combat/Data/WeaponStatsSO.cs` — 类标 `[Obsolete]`

**内容：** 替代 WeaponSystem 的新编排器。管理两个 WeaponTrack（Primary/Secondary），Update 中检查输入 + 热量 → ExecuteFire 遍历 CoreSnapshot → 生成弹头/炮口焰/音效 → 后坐力 + 热量结算 → 事件通知。[SerializeField] 暴露 debug 装备数组用于 Inspector 测试。

**目的：** GDD 第 3.3 节"齐射"逻辑和第 5 节发射管线的完整实现。所有核心同帧开火，棱镜修正管线通过 SnapshotBuilder 完成。

**技术：** MonoBehaviour 编排器, 组合模式（WeaponTrack 作为子组件），事件驱动, 对象池, `[RequireComponent]`。

---

---

## Batch 5: 具体部件实现（各家族 Core / Prism）

### 47. StarChartController 发射管线重构 — 家族分发 + 均匀扇形散布 — 2026-02-09 00:00

**修改文件：**
- `Assets/Scripts/Combat/StarChart/StarChartController.cs`

**内容：**
- 修改 `ExecuteFire()`：当 `ProjectileCount > 1` 时，散布逻辑从 `Random.Range(-Spread, Spread)` 改为 `[-Spread, +Spread]` 均匀等分
- 重构 `SpawnProjectile()` 为 `switch(coreSnap.Family)` 家族分发模式，调用各家族私有方法：`SpawnMatterProjectile()` / `SpawnLightBeam()` / `SpawnEchoWave()` / `SpawnAnomalyEntity()`
- default 分支打印 `Debug.LogWarning` 并 fallback 到 Matter
- 投射物生成后应用 `ProjectileSize`：`bulletObj.transform.localScale = Vector3.one * coreSnap.ProjectileSize`

**目的：** 让 4 种 CoreFamily 拥有各自独立的发射分支，支持均匀扇形散布（Fractal 棱镜需要）。

**技术：** switch 分发模式，均匀角度等分算法，策略模式扩展点。

---

### 48. LaserBeam — Light 家族即时命中激光 — 2026-02-09 00:00

**新建文件：**
- `Assets/Scripts/Combat/Projectile/LaserBeam.cs`

**内容：** MonoBehaviour + IPoolable。`Fire()` 方法执行 `Physics2D.Raycast` 从炮口沿方向检测命中（最大射程 = Speed × Lifetime）。使用 `LineRenderer` 渲染光束，持续约 0.1 秒后淡出并通过 PoolReference 回池。命中时调用 `IProjectileModifier.OnProjectileHit`。

**目的：** Light 家族的瞬间命中射击风格，不创建 Rigidbody2D 物理投射物。

**技术：** Physics2D.Raycast, LineRenderer 渐隐动画, IPoolable 对象池回收。

---

### 49. EchoWave — Echo 家族震荡波 AOE — 2026-02-09 00:00

**新建文件：**
- `Assets/Scripts/Combat/Projectile/EchoWave.cs`

**内容：** MonoBehaviour + IPoolable。`Initialize()` 设置膨胀参数。Update 中 `CircleCollider2D.radius` 按 `ProjectileSpeed` 线性膨胀。OnTriggerEnter2D 使用 `HashSet<Collider2D>` 去重（同一波次每敌人仅命中一次）。Spread > 0 时缩减为扇形波（角度检测限制触发范围）。穿墙特性通过碰撞层设置实现。超过 Lifetime 自动回池。

**目的：** Echo 家族的 AOE 扩散攻击风格，穿透墙壁，适合近距离群体控制。

**技术：** CircleCollider2D 动态膨胀, HashSet 去重, 扇形角度检测, IPoolable。

---

### 50. BoomerangModifier — Anomaly 家族回旋镖行为 — 2026-02-09 00:00

**新建文件：**
- `Assets/Scripts/Combat/Projectile/BoomerangModifier.cs`

**内容：** MonoBehaviour + IProjectileModifier。`OnProjectileSpawned` 记录发射者位置和初始方向。`OnProjectileUpdate` 实现去程减速 → 反转 → 回程加速的运动曲线，覆盖 `Projectile.Direction` 和 `Rigidbody2D.linearVelocity`。`OnProjectileHit` 使用两个 HashSet 分别跟踪去程和回程命中，每程每敌人各允许命中一次。设置 `ShouldDestroyOnHit = false` 实现穿透。返回发射者附近（< 1 单位）或 Lifetime 耗尽时回池。

**目的：** Anomaly 家族的自定义运动轨迹，实现回旋镖战术。

**技术：** IProjectileModifier 钩子, 运动曲线覆盖, 双 HashSet 去重。

---

### 51. Projectile 扩展 — 穿透与缩放重置 — 2026-02-09 00:00

**修改文件：**
- `Assets/Scripts/Combat/Projectile/Projectile.cs`

**内容：**
- 添加 `ShouldDestroyOnHit` 公共属性（默认 true），供 modifier 覆盖
- 修改 `OnTriggerEnter2D`：当 `ShouldDestroyOnHit == false` 时只执行 modifier 回调 + VFX，不回池
- 添加 `ForceReturnToPool()` 公共方法，供 BoomerangModifier 主动回池
- 在 `OnReturnToPool()` 中添加 `transform.localScale = Vector3.one` 重置缩放
- 在 `OnReturnToPool()` 中重置 `ShouldDestroyOnHit = true`

**目的：** 支持 Anomaly 回旋镖（穿透不销毁）和 Rheology 棱镜（ProjectileSize 缩放后回池重置）。

**技术：** 属性驱动行为开关, 回池状态重置。

---

### 52. SlowOnHitModifier — Tint 棱镜减速效果占位 — 2026-02-09 00:00

**新建文件：**
- `Assets/Scripts/Combat/Projectile/SlowOnHitModifier.cs`

**内容：** MonoBehaviour + IProjectileModifier。`OnProjectileHit` 检测碰撞体，当前为占位实现（`Debug.Log` 标记命中和预期减速效果）。`[SerializeField]` 暴露 `SlowPercent`(30%) 和 `Duration`(2s) 参数。`OnProjectileSpawned` 和 `OnProjectileUpdate` 为空实现。

**目的：** Tint 家族状态注入框架，待敌人系统完成后替换为实际 debuff 逻辑。

**技术：** IProjectileModifier 接口, 占位实现模式。

---

### 53. BounceModifier — Rheology 棱镜反弹效果 — 2026-02-09 00:00

**新建文件：**
- `Assets/Scripts/Combat/Projectile/BounceModifier.cs`

**内容：** MonoBehaviour + IProjectileModifier。`OnProjectileHit` 检测碰撞是否为墙壁层，若是则使用 `Vector2.Reflect` 计算反射方向，更新 `Projectile.Direction` 和 `Rigidbody2D.linearVelocity`，递减反弹计数。`[SerializeField]` 暴露 `MaxBounces`(3) 参数。反弹次数用尽时允许正常销毁。

**目的：** Rheology 棱镜的弹性反弹效果，让子弹在墙壁间弹射。

**技术：** Vector2.Reflect 反射算法, IProjectileModifier 接口, 计数器限制。

---

### 54. StarCoreSO 扩展 — Anomaly 家族 Modifier Prefab 链接 — 2026-02-09 00:00

**修改文件：**
- `Assets/Scripts/Combat/StarChart/StarCoreSO.cs` — 添加 `_anomalyModifierPrefab` 字段和 `AnomalyModifierPrefab` 属性
- `Assets/Scripts/Combat/StarChart/SnapshotBuilder.cs` — 在 `BuildCoreSnapshot()` 中传递 `AnomalyModifierPrefab` 到 `CoreSnapshot`
- `Assets/Scripts/Combat/StarChart/FiringSnapshot.cs` — `CoreSnapshot` 已包含 `AnomalyModifierPrefab` 字段

**内容：** 在 StarCoreSO 中添加 Anomaly 家族专用的 Modifier Prefab 引用字段，通过 SnapshotBuilder 管线传递到 CoreSnapshot，最终在 `SpawnAnomalyEntity()` 中使用。

**目的：** 完成 Anomaly 家族从 SO 配置到运行时行为注入的完整数据链路。

**技术：** SerializeField + 管线传递, 家族专用可选字段。

---

### 55. WeaponTrack 多家族池预热 — 2026-02-09 00:10

**修改文件：**
- `Assets/Scripts/Combat/Weapon/WeaponTrack.cs`

**内容：** 重构 `InitializePools()` 方法，根据 `CoreFamily` 分支预热不同容量的对象池：
- Matter: 20/50（高频子弹）
- Light: 5/20（短命 LineRenderer）
- Echo: 5/15（少量并发震荡波）
- Anomaly: 10/30（投射物池 + AnomalyModifierPrefab 池）
- 炮口焰池统一 5/20

**目的：** 避免运行时首次生成时的卡顿，按家族特性合理分配池容量。

**技术：** switch(CoreFamily) 分支, PoolManager.GetPool 预热。

---

### 56. Batch5AssetCreator — Editor 一键资产创建工具 — 2026-02-09 00:15

**新建文件：**
- `Assets/Scripts/Combat/Editor/Batch5AssetCreator.cs` — Editor 工具脚本
- `Assets/Scripts/Combat/Editor/ProjectArk.Combat.Editor.asmdef` — Editor-only 程序集

**内容：** 通过 Unity 菜单 `ProjectArk > Create Batch 5 Test Assets` 一键自动创建：
- 6 个 Prefab（Projectile_Matter, LaserBeam_Light, EchoWave_Echo, Modifier_Boomerang, Modifier_SlowOnHit, Modifier_Bounce）
- 4 个 StarCoreSO 资产（MatterCore_StandardBullet, LightCore_BasicLaser, EchoCore_BasicWave, AnomalyCore_Boomerang）
- 3 个 PrismSO 资产（FractalPrism_TwinSplit, RheologyPrism_Accelerate, TintPrism_FrostSlow）
- 目录自动创建于 `Assets/_Data/StarChart/` 下

**目的：** 解决 Prefab 和 SO 资产无法通过代码文本创建的问题，提供自动化资产生成工具。

**技术：** SerializedObject/SerializedProperty 反射设置私有字段, PrefabUtility.SaveAsPrefabAsset, AssetDatabase, Editor-only 程序集。

---

---

## Batch 6: 星图编织态交互体验 (Star Chart Weaving State IxD)

### 57. WeavingTransitionSettingsSO — 过渡配置 SO — 2026-02-09 12:45

**新建文件：**
- `Assets/Scripts/UI/WeavingTransitionSettingsSO.cs`

**内容：** ScriptableObject 数据容器，集中管理编织态过渡的所有可配置参数：
- Timing：进入时长 (0.35s)、退出时长 (0.25s)、AnimationCurve
- Camera：战斗态/编织态正交尺寸 (5 / 3)
- Vignette：战斗态/编织态强度 (0.1 / 0.5)
- DoF：焦距、焦点距离
- Audio：进入/退出音效 AudioClip

**目的：** 数据驱动，设计师可在 Inspector 中独立调整过渡参数，支持多套预设切换。

**技术：** ScriptableObject, `[CreateAssetMenu]`, 数据驱动模式。

---

### 58. WeavingStateTransition — 编织态过渡控制器 — 2026-02-09 12:45

**新建文件：**
- `Assets/Scripts/UI/WeavingStateTransition.cs`

**内容：** 编织态过渡的唯一编排器 MonoBehaviour，负责：
- 镜头推拉：协程驱动 `Camera.orthographicSize` 在战斗值/编织值之间平滑插值
- 后处理氛围：URP Volume 的 DepthOfField (启用/禁用) 和 Vignette (intensity 渐变)
- 镜头锁定：过渡期间将相机位置锁定在飞船中心（保持 Z 偏移）
- 音效播放：进入/退出时 `PlayOneShot`，AudioSource 设 `ignoreListenerPause = true`
- 全部使用 `Time.unscaledDeltaTime` 驱动，timeScale=0 下正常运行
- 快速切换防冲突：新协程启动前先 `StopCoroutine` 取消进行中的过渡
- 双数据源：优先读取 `WeavingTransitionSettingsSO`，为 null 时使用内联默认值

**公共 API：**
- `EnterWeavingState()` — 战斗态 → 编织态过渡
- `ExitWeavingState()` — 编织态 → 战斗态过渡

**目的：** 一个脚本统一编排镜头、后处理、音效三条过渡线，避免分散管理。

**技术：** 协程 + AnimationCurve, URP Volume TryGet<T>, Time.unscaledDeltaTime, 可选 SO 依赖 (fallback 模式)。

---

### 59. UIParallaxEffect — UI 视差微动 — 2026-02-09 12:45

**新建文件：**
- `Assets/Scripts/UI/UIParallaxEffect.cs`

**内容：** 挂在星图面板根节点的 MonoBehaviour，实现：
- 鼠标位置相对屏幕中心归一化 (-1,1)，乘最大偏移 (±15px) 并取反（视差=反向位移）
- `Vector2.Lerp` 平滑跟随，使用 `Time.unscaledDeltaTime`
- 手柄支持：`Gamepad.current?.rightStick.ReadValue()` 优先使用
- `OnDisable` 时自动 `ResetOffset()` 清零偏移
- `OnEnable` 时快照当前 `anchoredPosition` 作为基准原点

**目的：** 增加编织态 UI 的 3D 悬浮感和精致度。

**技术：** RectTransform anchoredPosition 偏移, New Input System Gamepad API, unscaledDeltaTime。

---

### 60. UIManager 集成 WeavingStateTransition — 2026-02-09 12:45

**修改文件：**
- `Assets/Scripts/UI/UIManager.cs`

**内容：**
- 新增 `[SerializeField] private WeavingStateTransition _weavingTransition` 字段
- `OpenPanel()` 中在 `Time.timeScale = 0` 之后调用 `_weavingTransition?.EnterWeavingState()`
- `ClosePanel()` 中在 `Time.timeScale = 1` 之前调用 `_weavingTransition?.ExitWeavingState()`
- 使用 null-conditional 确保为可选依赖，未配置时静默跳过

**目的：** 将过渡效果接入现有面板开关流程，零侵入式集成。

**技术：** 可选依赖模式 (null-conditional `?.`), SerializeField 编辑器连线。

---

### 25. UICanvasBuilder 编辑器脚本 — UI 自动搭建工具 — 2026-02-09 12:52

**新建文件：**
- `Assets/Scripts/UI/Editor/UICanvasBuilder.cs` — 编辑器工具，自动搭建完整 UI Canvas 层级

**内容：**
- 菜单 `ProjectArk > Build UI Canvas`：一键创建完整 UI 层级结构
  - Canvas (Screen Space Overlay, 1920×1080 参考分辨率)
  - EventSystem (如场景中缺失则自动创建)
  - HeatBarHUD (FillImage + OverheatFlash + Label，自动连线)
  - StarChartPanel (含 UIParallaxEffect，自动连线)
    - PrimaryTrackView / SecondaryTrackView (各含 3 PrismCell + 3 CoreCell)
    - InventoryView (过滤按钮栏 + ScrollRect 网格布局)
    - ItemDetailView (Icon + Name + Description + Stats + ActionButton)
  - UIManager (含 WeavingStateTransition 组件，自动连线)
- 菜单 `ProjectArk > Create InventoryItemView Prefab`：创建库存卡片预制体
- 自动查找并关联 InputActionAsset、StarChartInventorySO
- 自动创建 PlayerInventory.asset 并填充已有的 StarCoreSO/PrismSO/LightSailSO/SatelliteSO
- 所有 SerializeField 引用通过 SerializedObject API 自动连线

**目的：** 消除最大的编辑器配置缺口——UI Canvas 层级手动搭建工作量（约 30-60 分钟），一键完成。

**技术：** Unity Editor MenuItem, SerializedObject 反射连线, PrefabUtility, Undo 支持, 自动资产发现。

---

### Batch 7: 编辑器/资产批量配置 — 2026-02-09 16:10

**修改的文件：**
- `Assets/_Data/StarChart/Prisms/RheologyPrism_Accelerate.asset` — 连线 `_projectileModifierPrefab` → Modifier_Bounce.prefab
- `Assets/_Data/StarChart/Cores/MatterCore_StandardBullet.asset` — `_heatCost: 0 → 5`
- `Assets/_Data/StarChart/Cores/LightCore_BasicLaser.asset` — `_heatCost: 0 → 4`
- `Assets/_Data/StarChart/Cores/EchoCore_BasicWave.asset` — `_heatCost: 0 → 12`
- `Assets/_Data/StarChart/Cores/AnomalyCore_Boomerang.asset` — `_heatCost: 0 → 8`
- `ProjectSettings/TagManager.asset` — 添加 Layer 10: EchoWave
- `ProjectSettings/Physics2DSettings.asset` — 更新碰撞矩阵: Player↔PlayerProjectile❌, Player↔EchoWave❌, EchoWave↔Wall❌, EchoWave↔EchoWave❌, EchoWave↔PlayerProjectile❌, PlayerProjectile↔PlayerProjectile❌

**新建的文件：**
- `Assets/_Data/UI/WeavingTransitionSettings.asset` — WeavingTransitionSettingsSO 数据资产 (默认参数)
- `Assets/Settings/GlobalVolumeProfile.asset` — URP Volume Profile (Vignette + DepthOfField Override)

**场景修改 (SampleScene.unity)：**
- 新增 Global Volume GameObject (id: 300000001)，挂载 Volume 组件，引用 GlobalVolumeProfile
- UIManager 对象添加 AudioSource 组件 (id: 300000004, PlayOnAwake=false)
- WeavingStateTransition 连线: `_postProcessVolume` → Global Volume, `_sfxSource` → AudioSource, `_settings` → WeavingTransitionSettings.asset
- EventSystem: StandaloneInputModule → InputSystemUIInputModule (New Input System)
- HeatBarHUD.FillImage: `m_RaycastTarget: 1 → 0`
- HeatBarHUD._heatGradient: 白→白 → 绿(#33CC4C)→黄(#FFD700)→红(#FF3333)

**目的：** 批量完成所有已实现功能的编辑器配置，使项目可以直接 Play 测试。

**技术：** 直接编辑 Unity YAML 序列化文件 (.asset/.unity)，通过查找 PackageCache 中的 .cs.meta 文件获取正确的 script guid。

---

### Input System 整理 — 删除冗余 Player Action Map (2026-02-09 16:28)

**删除的文件：**
- `Assets/InputSystem_Actions.inputactions` — Unity 自动生成的默认 Input Actions，含 "Player" + "UI" Action Map，无任何代码/场景/预制体引用
- `Assets/InputSystem_Actions.inputactions.meta`

**保留的文件：**
- `Assets/Input/ShipActions.inputactions` — 项目实际使用的 Input Actions，含 "Ship" + "UI" 两个 Action Map，被 SampleScene、Ship.prefab、UIManager 引用

**目的：** 采用方案 B（独立 Ship Action Map），每个游戏状态一个 Action Map（Ship=飞行战斗，UI=菜单/星图），遵循 Unity New Input System 最佳实践和项目模块化架构原则。删除无人引用的默认 Player Action Map 文件，消除歧义。

---

### 修复星图 UI 点击无响应 — InputSystemUIInputModule Action 连线 (2026-02-09 16:42)

**问题：** 打开星图面板后，所有 UI 按钮（轨道选择、库存物品、详情面板）点击无响应，hover 也无效。

**根因：** 场景中 EventSystem 的 `InputSystemUIInputModule` 组件虽然已存在且引用了 `ShipActions.inputactions`，但其所有 UI Action 字段（`m_PointAction`、`m_LeftClickAction`、`m_MoveAction` 等）均为 `{fileID: 0}`（空引用）。这导致模块不知道鼠标指针位置，无法执行 GraphicRaycaster 射线检测，Button.onClick 永远不会被触发。

**修改的文件：**
- `Assets/Scripts/UI/UIManager.cs` — 在 `Awake()` 中新增 `ConfigureUIInputModule()` 方法，自动查找场景中的 `InputSystemUIInputModule`，将其 `point`/`leftClick`/`scrollWheel`/`move`/`submit`/`cancel` 属性连接到 `ShipActions.inputactions` 的 "UI" Action Map 中对应的 Action，并确保 UI Map 始终启用

**技术：** 使用 `InputActionReference.Create()` 在运行时动态创建 Action 引用，避免依赖 Unity YAML 序列化中的哈希值配置。`uiMap.Enable()` 确保 UI Action Map 在游戏全程保持激活。

---

### 修复星图库存卡片不可见 — ScrollArea Mask Image Alpha (2026-02-09 23:24)

**问题：** 打开星图面板后，库存区域完全空白，看不到任何物品卡片。日志显示 10 个卡片已正确创建（`instantiated 10 items, contentParent.childCount=10`），Setup 也成功执行。

**根因：** ScrollArea 对象上的 `Mask` 组件依赖同 GameObject 上的 `Image` 组件来确定裁剪区域。该 Image 的颜色被设为 `{r:0, g:0, b:0, a:0}`（完全透明），同时 `CanvasRenderer.m_CullTransparentMesh = 1`。这导致 Image 被 CanvasRenderer 判定为完全透明而剔除渲染，Mask 因此没有可渲染像素作为裁剪区域，所有子物体（包括 10 个卡片）被完全裁掉。

**修改的文件：**
- `Assets/Scenes/SampleScene.unity` — ScrollArea (fileID: 1074280193) 的两个组件：
  - Image (fileID: 1074280196): `m_Color.a: 0 → 0.00392`（1/255，肉眼不可见但 Mask 有效）, `r/g/b: 0 → 1`（白色基底）
  - CanvasRenderer (fileID: 1074280197): `m_CullTransparentMesh: 1 → 0`（禁止剔除低透明度网格）

**目的：** 让 Mask 组件有可渲染的 Image 像素来定义裁剪区域，同时保持视觉上不可见（`m_ShowMaskGraphic: 0` + 极低 alpha）。

**技术：** Unity uGUI Mask 组件依赖 Graphic（通常是 Image）的渲染像素区域做模板裁剪。alpha=0 + CullTransparentMesh=1 会导致 Image 完全不参与渲染，Mask 裁剪区域退化为零。

---

### Fix: Dual-Core Projectile Self-Collision (2026-02-10 01:16)

**问题：** 同时装备两个 StarCore（如 BasicBullet + BasicBullet2）时，子弹射程极短，呈螺旋状停留在飞船周围。单独装备任一核心时子弹正常飞行。

**根因：** `OnTriggerEnter2D` 没有任何 Layer 过滤逻辑。两个核心在同一位置同时发射子弹，子弹均位于 PlayerProjectile (Layer 7)，而 Physics2D 碰撞矩阵中 Layer 7 与 Layer 7 之间碰撞为开启状态。两颗子弹的 Trigger Collider 互相重叠，立即触发 `OnTriggerEnter2D` → `ReturnToPool()`，导致子弹在出生后 1-2 帧内就被回收。

**修改的文件：**
- `ProjectSettings/Physics2DSettings.asset` — 碰撞矩阵修改：
  - Row 6 (Player): `fffffffb` → `ffffff7b` — 关闭 Player 与 PlayerProjectile 的碰撞
  - Row 7 (PlayerProjectile): `7ffffffb` → `7fffff3b` — 关闭 PlayerProjectile 自碰撞 + 与 Player 的碰撞
- `Assets/Scripts/Combat/Projectile/Projectile.cs` — OnTriggerEnter2D 添加 Layer 过滤：
  - 忽略同 Layer（其他子弹）和 Player Layer 的碰撞
  - 缓存 `LayerMask.NameToLayer("Player")` 到静态字段避免运行时字符串查找
  - 清理所有之前的调试日志代码（`_debugFrameCount`、`FixedUpdate` 诊断、`Init/Update/OnGetFromPool` 日志）

**目的：** 防止玩家子弹之间互相碰撞导致瞬间回收，同时防止子弹与玩家飞船碰撞。

**技术：** 双重保险策略——Physics2D 碰撞矩阵层面关闭不需要的 Layer 间碰撞（性能最优，物理引擎直接跳过检测），代码层面在 `OnTriggerEnter2D` 中添加防御性过滤（防止未来 Layer 配置意外变更时的兜底）。

---

### 🔧 Remap: 星图切换键 ESC → Tab — 2026-02-10 11:30

**变更说明：** 将星图面板的切换按键从 ESC 改为 Tab，ESC 键预留给未来的暂停菜单功能。同时将 Input Action 和代码中的命名从 `Pause` 重命名为 `ToggleStarChart`，语义更明确。

**修改的文件：**
- `Assets/Input/ShipActions.inputactions` —
  - Ship action map 中 `Pause` action 重命名为 `ToggleStarChart`
  - 键盘绑定路径从 `<Keyboard>/escape` 改为 `<Keyboard>/tab`
  - 手柄绑定 `<Gamepad>/start` 保持不变，action 引用同步更新
- `Assets/Scripts/UI/UIManager.cs` —
  - `_pauseAction` 字段重命名为 `_toggleStarChartAction`
  - `FindAction("Pause")` 改为 `FindAction("ToggleStarChart")`
  - 回调方法 `OnPausePerformed` 重命名为 `OnToggleStarChartPerformed`

---

### ✨ Feature: 星图拖拽装备 (Star Chart Drag & Drop) — 2026-02-10 12:10

**功能说明：** 为星图面板新增拖拽交互，玩家可以直接从库存区拖拽星核/棱镜到轨道槽位完成装备，也可以从槽位拖出到库存区卸载，或跨轨道移动。原有"点击→详情→EQUIP"流程保留，拖拽为补充交互方式。

**核心机制：**
- 基于 uGUI EventSystem 的 `IBeginDragHandler` / `IDragHandler` / `IEndDragHandler` / `IDropHandler` / `IPointerEnterHandler` / `IPointerExitHandler` 接口
- 单例 `DragDropManager` 管理全局拖拽状态、幽灵视图、装备/卸载执行
- 半透明 `DragGhostView` 跟随鼠标，`CanvasGroup.blocksRaycasts = false` 确保不遮挡 drop target
- 类型校验（Core→Core层, Prism→Prism层）+ 空间校验（SlotLayer.FreeSpace ≥ SlotSize）
- 悬停时绿色/红色高亮反馈，SlotSize>1 支持多格高亮预览
- 拖拽进入 TrackView 区域自动切换选中轨道
- 所有逻辑不依赖 Time.deltaTime，天然兼容 timeScale=0
- LightSail/Satellite 不参与拖拽（当前无对应拖放目标）
- 面板关闭时自动取消进行中的拖拽并清理幽灵

**新建文件：**
- `Assets/Scripts/UI/DragDrop/DragPayload.cs` — 拖拽数据载体（DragSource 枚举 + 轻量数据类）
- `Assets/Scripts/UI/DragDrop/DragDropManager.cs` — 全局拖拽管理器单例（状态管理、装备/卸载执行）
- `Assets/Scripts/UI/DragDrop/DragGhostView.cs` — 半透明拖拽幽灵视图

**修改文件：**
- `Assets/Scripts/UI/InventoryItemView.cs` — 实现 IBeginDragHandler/IDragHandler/IEndDragHandler，添加 CanvasGroup alpha 控制
- `Assets/Scripts/UI/SlotCellView.cs` — 实现 IDropHandler/IPointerEnterHandler/IPointerExitHandler（拖放目标）+ IBeginDragHandler/IDragHandler/IEndDragHandler（已装备物品拖出）；新增 IsCoreCell/CellIndex/OwnerTrack 属性
- `Assets/Scripts/UI/TrackView.cs` — 实现 IPointerEnterHandler 自动选中；Awake 中初始化 cell 属性；新增 HasSpaceForItem/SetMultiCellHighlight/ClearAllHighlights 方法
- `Assets/Scripts/UI/InventoryView.cs` — 实现 IDropHandler 接收从槽位拖出的物品完成卸载
- `Assets/Scripts/UI/StarChartPanel.cs` — 集成 DragDropManager（Bind/Close 清理）；新增 RefreshAllViews/SelectAndShowItem 公共方法

**技术：** uGUI EventSystem 拖拽接口 + 单例管理器模式 + 策略化校验（类型+空间）。

---

### 🔧 Refactor: DragDropManager 去除 Instantiate — 2026-02-10 12:42

**修改文件：**
- `Assets/Scripts/UI/DragDrop/DragDropManager.cs` — `_ghostPrefab` 字段重命名为 `_ghostView`（语义从"预制体"变为"场景实例直接引用"）；`Bind()` 中移除 `Instantiate()` 调用，改为直接赋值 `_ghost = _ghostView`

**目的：** DragGhost 作为 StarChartPanel 子对象全场唯一，无需在运行时 Instantiate 拷贝。简化代码，减少运行时开销。

**技术：** 场景内直接引用模式（零 Instantiate / 零 Prefab），SerializeField 绑定场景实例。

---

### 🐛 Bugfix: StarChartPanel._dragDropManager 空引用修复 — 2026-02-10 12:48

**修改文件：**
- `Assets/Scenes/SampleScene.unity` — StarChartPanel 组件的 `_dragDropManager` 字段从 `{fileID: 0}`（空引用）修复为 `{fileID: 1315625636}`（DragDropManager 组件实例）

**问题：** DragDropManager 组件已正确挂载在 StarChartPanel GameObject 上，但 StarChartPanel 的 `[SerializeField] _dragDropManager` 字段未在 Inspector 中拖入赋值，导致运行时为 null，`Bind()` 不会执行，拖拽功能整体失效。

**原因：** 在 Inspector 中添加 DragDropManager 组件后，忘记将其拖入 StarChartPanel 的 Drag Drop Manager 字段完成连线。

---

### HeatBarHUD 数值显示增强 — 2026-02-10 12:52

**修改文件：**
- `Assets/Scripts/Heat/HeatSystem.cs` — 新增 `MaxHeat` 公开属性，暴露最大热量值供 UI 读取
- `Assets/Scripts/UI/HeatBarHUD.cs` — Label 从静态文本改为动态数值显示

**内容：** Heat 条标签在正常状态下显示 `HEAT(当前值/最大值)` 格式（如 `HEAT(45/100)`），过热状态仍显示 `OVERHEATED`。热量值每次变化时实时更新标签文本。

**目的：** 让玩家在战斗中能直观看到精确的热量数值，而非仅依赖进度条估算。

**技术：** 事件驱动更新，`F0` 格式化取整显示，通过 HeatSystem.MaxHeat 属性访问 HeatStatsSO 配置值。

---

### 🐛 Bugfix: 四家族 Core 发射管线全面修复 — 2026-02-10 13:05

**修改文件：**
- `Assets/Scripts/Combat/StarChart/StarChartController.cs` — **SpawnAnomalyEntity()** 重写：将 Prefab 组件引用改为运行时 AddComponent + JsonUtility 拷贝，确保每个投射物拥有独立的 BoomerangModifier 实例；增加 Modifiers null 安全处理
- `Assets/Scripts/Combat/Projectile/Projectile.cs` — **OnReturnToPool()** 增加动态添加的 Modifier 组件清理（Destroy），防止组件在对象池复用时泄漏累积
- `Assets/Scripts/Combat/Projectile/EchoWave.cs` — **Awake()** 增加 LayerMask 零值 fallback（Enemy/Wall）；增加程序化圆形 Sprite 生成（64x64 白色填充圆），解决 SpriteRenderer 无 Sprite 时完全不可见的问题
- `Assets/Scripts/Combat/Projectile/LaserBeam.cs` — **Awake()** 增加 hitMask 零值 fallback（Enemy+Wall）；调整默认 beam 参数使光束更易观察（duration 0.1→0.15s, fade 0.05→0.1s, startWidth 0.15→0.2, endWidth 0.05→0.08）
- `Assets/Scripts/Combat/StarChart/SnapshotBuilder.cs` — **CollectTintModifiers()** 从可能返回 null 改为始终返回非空 List，消除下游 NullReferenceException 隐患
- `Assets/_Data/StarChart/Prefabs/EchoWave_Echo.prefab` — Layer 从 7(PlayerProjectile) 改为 10(EchoWave)；_enemyMask 从 ~0(所有层) 改为 256(Enemy)；_wallMask 从 0 改为 512(Wall)
- `Assets/_Data/StarChart/Prefabs/LaserBeam_Light.prefab` — Layer 从 0(Default) 改为 7(PlayerProjectile)；_hitMask 从 ~0(所有层) 改为 768(Enemy+Wall)

**问题摘要：**
1. **Anomaly/Boomerang (P0):** `SpawnAnomalyEntity` 从 Prefab 资产上 `GetComponent<IProjectileModifier>()` 获取引用，导致所有投射物共享同一个 Modifier 实例（状态互相覆盖）；且 `coreSnap.Modifiers` 可能为 null 导致 NRE
2. **Echo/Shockwave (P0):** EchoWave Prefab 的 `_enemyMask=~0` 命中所有层（含玩家）；SpriteRenderer 无 Sprite 导致扩展波完全不可见；Prefab Layer 错误
3. **Light/Laser (P1):** LaserBeam Prefab 的 `_hitMask=~0` 射线命中所有层；Prefab Layer=0(Default) 而非 PlayerProjectile；beam 持续时间过短难以观察
4. **SnapshotBuilder (P1):** `CollectTintModifiers()` 在无 Tint 棱镜时返回 null，被 Anomaly 分支直接调用 `.Contains()` 引发 NRE

**技术：** Anomaly modifier 使用 AddComponent + JsonUtility.FromJsonOverwrite 实现运行时组件拷贝（保持序列化字段值与 Prefab 一致）。EchoWave 使用 Texture2D 程序化生成 64x64 白色填充圆作为 SpriteRenderer fallback。所有 LayerMask 在代码和 Prefab 两层均做了正确初始化。

---

### 🐛 Bugfix: LaserBeam 对象池复用后累积透明度衰减 — 2026-02-10 13:12

**修改文件：**
- `Assets/Scripts/Combat/Projectile/LaserBeam.cs` — **OnReturnToPool()** 增加 LineRenderer startColor/endColor 重置为 Color.white

**问题根因：** `OnReturnToPool()` 只重置了 width 未重置 color。fade 阶段将 alpha 降至 ≈0 后，对象回池；下次 `Fire()` 调用时 `_initialStartColor` / `_initialEndColor` 从 LineRenderer 读取的是上次残留的 alpha≈0 值，导致每次发射 alpha 递减，3 次后完全不可见。

**技术：** 在 OnReturnToPool 中将 LineRenderer 颜色重置为 `Color.white`（完全不透明），确保下次 Fire() 读取到正确的初始 alpha=1。

---

### 🧹 Legacy Code & Asset Cleanup — 2026-02-10 13:59

**目的：** 移除被 StarChart 系统完全取代的 legacy 代码文件、过时 SO 资产和空目录，整理资产目录结构。

**删除的代码文件：**
- `Assets/Scripts/Combat/Weapon/WeaponSystem.cs` (+.meta) — 标记 `[Obsolete]`，已被 StarChartController 完全替代
- `Assets/Scripts/Combat/Data/WeaponStatsSO.cs` (+.meta) — 标记 `[Obsolete]`，已被 StarCoreSO 替代

**清除的 legacy 兼容桥：**
- `Assets/Scripts/Combat/Projectile/Projectile.cs` — 移除 `[Obsolete]` 的 `Initialize(Vector2, WeaponStatsSO, ...)` 重载方法及注释中的 WeaponStatsSO 引用
- `Assets/Scripts/Combat/StarChart/ProjectileParams.cs` — 移除 `FromWeaponStats(WeaponStatsSO)` 静态方法及 `#pragma warning disable/restore CS0618`

**删除的 legacy 资产：**
- `Assets/_Data/Weapons/DefaultWeaponStats.asset` (+.meta) — 旧 WeaponStatsSO 实例，无引用
- `Assets/_Prefabs/Projectiles/BasicBullet.prefab` (+.meta) — 旧子弹 Prefab，已被 Projectile_Matter.prefab 替代

**修正 Prefab 引用：**
- `Assets/_Data/StarChart/Cores/StarCore.asset` — `_projectilePrefab` 从 BasicBullet.prefab 改指向 Projectile_Matter.prefab
- `Assets/_Data/StarChart/Cores/TestCore_FastBullet.asset` — 同上

**迁移的 SO 资产（保留 GUID）：**
- `_Data/Weapons/StarCore.asset` → `_Data/StarChart/Cores/StarCore.asset`
- `_Data/Weapons/TestCore_FastBullet.asset` → `_Data/StarChart/Cores/TestCore_FastBullet.asset`
- `_Data/Weapons/TestSpeedSail.asset` → `_Data/StarChart/Sails/TestSpeedSail.asset`（新建 Sails/ 目录）

**迁移的代码文件（保留 GUID，命名空间不变）：**
- `Scripts/Combat/Weapon/WeaponTrack.cs` → `Scripts/Combat/StarChart/WeaponTrack.cs`
- `Scripts/Combat/Weapon/FirePoint.cs` → `Scripts/Combat/StarChart/FirePoint.cs`

**删除的空目录：**
- `Assets/Scripts/Combat/Weapon/` (+.meta)
- `Assets/Scripts/Combat/Data/` (+.meta)
- `Assets/_Data/Weapons/` (+.meta)
- `Assets/_Prefabs/Projectiles/` (+.meta)
- `Assets/_Prefabs/Effects/` (+.meta)
- `Assets/_Data/Enemies/` (+.meta)

**技术：** 文件迁移通过文件系统 Move 操作连同 .meta 文件一起移动，保留 Unity GUID 引用不断裂。StarCoreSO 的 _projectilePrefab 字段通过修改 .asset YAML 中的 fileID 和 guid 完成 Prefab 引用切换。

---

### 🧹 删除早期 Legacy StarCoreSO 资产 — 2026-02-10 14:12

**问题：** 库存中显示 "Basic Bullet" (StarCore.asset) 和 "Basic Bullet2" (TestCore_FastBullet.asset) 两个早期手动创建的 StarCoreSO 资产。这些资产缺少 Batch 5 新增的 `_anomalyModifierPrefab` 序列化字段，且功能已被 Batch 5 自动创建的 `MatterCore_StandardBullet.asset` 完全替代。装备这些旧核心会导致射击功能异常。

**删除的资产文件：**
- `Assets/_Data/StarChart/Cores/StarCore.asset` (+.meta) — "Basic Bullet"，GUID: e804D3b5
- `Assets/_Data/StarChart/Cores/TestCore_FastBullet.asset` (+.meta) — "Basic Bullet2"，GUID: Ff2B3C4E

**修改的文件：**
- `Assets/_Data/StarChart/PlayerInventory.asset` — 从 `_ownedItems` 中移除上述两个旧 GUID 引用（10 项 → 8 项）
- `Assets/_Prefabs/Ship/Ship.prefab` — `_debugPrimaryCores` 从旧双核心改为单个 MatterCore_StandardBullet；`_debugSecondaryCores` 清空（原引用 TestCore_FastBullet）

**目的：** 消除库存中不可用的 legacy 核心，修复因旧 SO 资产字段不完整导致的射击失效问题。

**技术：** 直接编辑 Unity YAML 序列化文件中的 GUID 引用列表，删除前确认全项目无其他引用。

---

### 🐛 修复 Standard Bullet 不可见问题 — 2026-02-10 14:18

**问题：** Standard Bullet (MatterCore) 装备后射击时 heat 正常变化，但看不到子弹飞出。Laser 等其他家族正常。

**根因：** `Projectile_Matter.prefab` 在 Batch 5 由 `Batch5AssetCreator.cs` 自动创建时，`SpriteRenderer` 只设置了颜色 (1, 0.9, 0.3) 但**未分配 Sprite** (`m_Sprite: {fileID: 0}`, `m_WasSpriteAssigned: 0`)。没有 Sprite 的 SpriteRenderer 不会渲染任何像素，导致子弹虽然存在且在移动，但视觉上完全不可见。

**修改的文件：**
- `Assets/_Data/StarChart/Prefabs/Projectile_Matter.prefab` — SpriteRenderer 的 `m_Sprite` 引用改为 `bullet_0` sprite (来自 `Art/Sprites/Projectiles/bullet.png`)；`m_Size` 调整为 (0.08, 0.2)；`m_WasSpriteAssigned` 设为 1。
- `Assets/Scripts/Combat/Projectile/Projectile.cs` — `Awake()` 中新增 fallback 逻辑：若 SpriteRenderer 存在但 sprite 为 null，自动生成程序化圆形 sprite（与 EchoWave.cs 同款），防止未来新建 Matter prefab 时遗漏 sprite 分配。

**目的：** 让物理子弹在屏幕上可见，同时建立防御性 fallback 机制。

**技术：** 直接编辑 prefab YAML 中 SpriteRenderer 的 sprite 引用（fileID + GUID 指向 `bullet.png` 的 `bullet_0` sub-sprite）；Projectile.cs 使用静态缓存的程序化 Texture2D 生成 fallback sprite，零额外开销。

---

### 🔧 复刻旧 BasicBullet 视觉效果到 Standard Bullet — 2026-02-10 14:30

**问题：** 修复 sprite 不可见后，Standard Bullet 视觉效果仍"远不如"旧版 BasicBullet：无拖尾效果、颜色不对（黄色 vs 旧版白色）。

**根因分析（通过 Git 历史对比）：**
- 旧 `BasicBullet.prefab`（已删除）有 **TrailRenderer** 组件：`time=0.15s`，宽度从 `0.085` 锥形渐细到 `0`，颜色白色 `alpha=1 → alpha=0` 淡出
- 旧 SpriteRenderer 颜色为**白色** `(1, 1, 1, 1)`
- Batch 5 自动创建的 `Projectile_Matter.prefab` **完全缺失 TrailRenderer**，且 SpriteRenderer 颜色被设为黄色 `(1, 0.9, 0.3)`

**修改的文件：**
- `Assets/Scripts/Combat/Projectile/Projectile.cs` — `Awake()` 中新增 TrailRenderer 自动配置逻辑：若 prefab 没有 TrailRenderer 则动态添加并调用 `ConfigureTrail()` 方法。新增 `ConfigureTrail()` 静态方法，参数完全复刻旧 BasicBullet：`time=0.15`、宽度曲线 `0.085→0`、白色 alpha 淡出、使用 SpriteRenderer 相同材质（URP 兼容）。
- `Assets/_Data/StarChart/Prefabs/Projectile_Matter.prefab` — SpriteRenderer 颜色从黄色 `(1, 0.9, 0.3)` 恢复为白色 `(1, 1, 1, 1)`，与旧 BasicBullet 一致。

**目的：** 让 Standard Bullet 的视觉效果（拖尾、颜色）与旧版 BasicBullet 完全一致。

**技术：** TrailRenderer 在 `Awake()` 中动态创建配置而非手写 YAML，避免复杂的 prefab 序列化编辑且自带 fallback 保护。Trail 使用 SpriteRenderer 的 `sharedMaterial` 确保在 URP 2D 管线下正确渲染。

---

---

## MS2 Batch 3: 光帆框架 + 伴星运行时

### 31. StarChartContext 依赖上下文 — 2026-02-08 20:00

**新建文件：**
- `Assets/Scripts/Combat/StarChart/StarChartContext.cs` — readonly struct 依赖包

**内容：** 打包 ShipMotor/ShipAiming/InputHandler/HeatSystem/StarChartController/Transform 为单一注入点，供光帆和伴星 Behavior 访问所有 Ship 系统。

**目的：** 避免 Behavior 自行 GetComponent，统一依赖注入。

**技术：** readonly struct（零分配），依赖注入模式。

---

### 32. LightSailSO + SatelliteSO 运行时字段 — 2026-02-08 20:00

**修改文件：**
- `Assets/Scripts/Combat/StarChart/LightSailSO.cs` — 新增 `_behaviorPrefab` 字段
- `Assets/Scripts/Combat/StarChart/SatelliteSO.cs` — 新增 `_behaviorPrefab` 字段

**内容：** 添加 `[SerializeField] GameObject _behaviorPrefab` + 公开属性，用于关联运行时行为 Prefab。

**目的：** SO 数据层与运行时行为桥接。遵循 PrismSO 的 `ProjectileModifierPrefab` 先例。

**技术：** 策略模式（Prefab 持有具体 Behavior 子类）。

---

### 33. ProjectileParams + WeaponTrack 扩展 — 2026-02-08 20:00

**修改文件：**
- `Assets/Scripts/Combat/StarChart/ProjectileParams.cs` — 新增 `WithDamageMultiplied(float)` 方法
- `Assets/Scripts/Combat/Weapon/WeaponTrack.cs` — 新增 `ResetCooldown()` 方法

**内容：** ProjectileParams 新增零分配的 buff 应用便捷方法；WeaponTrack 暴露冷却重置 API 供光帆/伴星调用。

**目的：** 光帆 buff 通过 ref 传递 readonly struct 修改弹头参数；ResetCooldown 支持擦弹引擎等机制。

**技术：** readonly struct 值拷贝（零 GC），公开 API 扩展。

---

### 34. LightSailBehavior + LightSailRunner — 2026-02-08 20:10

**新建文件：**
- `Assets/Scripts/Combat/StarChart/LightSail/LightSailBehavior.cs` — 抽象 MonoBehaviour 基类
- `Assets/Scripts/Combat/StarChart/LightSail/LightSailRunner.cs` — 纯 C# 生命周期管理器

**内容：** LightSailBehavior 定义 Initialize/Tick/ModifyProjectileParams/OnDisabled/OnEnabled/Cleanup 生命周期。LightSailRunner 负责实例化 BehaviorPrefab 为 Ship 子物体、订阅 HeatSystem 过热事件（过热时禁用 buff）、Tick 驱动、Dispose 清理。

**目的：** 光帆运行时框架。抽象基类 + Runner 分离，遵循 WeaponTrack 纯 C# 先例。

**技术：** 抽象 MonoBehaviour，纯 C# Runner，事件订阅（OnOverheated/OnCooldownComplete），策略模式。

---

### 35. SatelliteBehavior + SatelliteRunner — 2026-02-08 20:10

**新建文件：**
- `Assets/Scripts/Combat/StarChart/Satellite/SatelliteBehavior.cs` — 抽象 MonoBehaviour 基类
- `Assets/Scripts/Combat/StarChart/Satellite/SatelliteRunner.cs` — 纯 C# 生命周期管理器

**内容：** SatelliteBehavior 实现 IF-THEN 模式：EvaluateTrigger(context)→bool + Execute(context)。SatelliteRunner 管理 Prefab 实例化、内部冷却计时（InternalCooldown）、触发评估-执行调度。

**目的：** 伴星运行时框架。支持轮询型（检查状态）和事件驱动型（设 flag → 下帧检查）两种触发模式。

**技术：** 模板方法模式（EvaluateTrigger/Execute），内部冷却防触发循环。

---

### 36. StarChartController Batch 3 集成 — 2026-02-08 20:20

**修改文件：**
- `Assets/Scripts/Combat/StarChart/StarChartController.cs` — 集成光帆/伴星 Runner

**内容：** 替换旧占位字段为 `_debugLightSail`/`_debugSatellites[]`。Awake 中创建 StarChartContext，Start 中初始化 Runner。Update 中 Tick 光帆/伴星。SpawnProjectile 中调用 `_lightSailRunner?.ModifyProjectileParams(ref parms)` 应用光帆 buff。OnDestroy 中 Dispose 所有 Runner。移除 Batch 2 诊断日志。

**目的：** 将光帆和伴星框架完整集成到星图控制器中。

**技术：** 纯 C# Runner 组合，ref 参数零分配 buff 传递，可空安全 (`?.`)。

---

### 37. SpeedDamageSail 测试实现 — 2026-02-08 20:20

**新建文件：**
- `Assets/Scripts/Combat/StarChart/LightSail/SpeedDamageSail.cs` — 简化版多普勒效应

**内容：** 继承 LightSailBehavior。Tick 中读取 NormalizedSpeed → 计算伤害乘数（0 速 = ×1.0，满速 = ×1.5）。ModifyProjectileParams 中通过 WithDamageMultiplied 应用。OnDisabled 时重置乘数。

**目的：** 验证光帆框架完整工作流（实例化 → Tick → Buff 应用 → 过热禁用/恢复）。

**技术：** 线性插值 buff，readonly struct 值拷贝。

---

---

## Batch 4: Star Chart UI + 热量 HUD

### 38. UI Assembly 配置 + StarChartInventorySO — 2026-02-08 21:00

**修改文件：**
- `Assets/Scripts/UI/ProjectArk.UI.asmdef` — 添加 Combat, Heat, InputSystem, TMP 引用

**新建文件：**
- `Assets/Scripts/UI/StarChartInventorySO.cs` — 原型库存 SO，持有玩家拥有的星图部件列表

**内容：** 扩展 UI 程序集依赖以访问战斗和热量系统。创建 StarChartInventorySO 提供按类型过滤的库存数据源。

**目的：** 为 UI 层建立编译基础和数据源。

**技术：** ScriptableObject 数据驱动，LINQ OfType 泛型过滤。

---

### 39. HeatBarHUD 常驻热量条 — 2026-02-08 21:05

**新建文件：**
- `Assets/Scripts/UI/HeatBarHUD.cs` — 常驻热量条 HUD

**内容：** 通过 Bind(HeatSystem) 注入引用，订阅 OnHeatChanged/OnOverheated/OnCooldownComplete 事件。填充量 + Gradient 渐变色 + 过热红色闪烁动画（sin 波 alpha）。OnDestroy 取消订阅。

**目的：** 战斗中始终可见的热量状态指示。

**技术：** 事件驱动 UI 更新，Time.unscaledDeltaTime 动画（兼容 timeScale=0）。

---

### 40. SlotCellView 格子视图 — 2026-02-08 21:10

**新建文件：**
- `Assets/Scripts/UI/SlotCellView.cs` — 单格子 UI 原子构件

**内容：** 支持四种状态：SetItem（显示图标）、SetEmpty（空格）、SetSpannedBy（被大型部件占用）、SetHighlight（绿/红高亮）。Button.onClick 委托 OnClicked 事件。

**目的：** 轨道视图和库存视图的通用格子构件。

**技术：** uGUI Image + Button，颜色状态机。

---

### 41. TrackView 轨道展示 — 2026-02-08 21:15

**新建文件：**
- `Assets/Scripts/UI/TrackView.cs` — 单武器轨道视图

**内容：** 三明治布局：3 棱镜格 + 3 星核格。Bind(WeaponTrack) 后订阅 OnLoadoutChanged，Refresh 根据 Items 和 SlotSize 映射到 SlotCellView。支持轨道选中高亮。

**目的：** 可视化武器轨道的当前装备配置。

**技术：** 事件驱动刷新，SlotSize 到格子索引的线性映射。

---

### 42. InventoryItemView + InventoryView — 2026-02-08 21:20

**新建文件：**
- `Assets/Scripts/UI/InventoryItemView.cs` — 单库存物品卡片
- `Assets/Scripts/UI/InventoryView.cs` — 可滚动库存格子 + 类型过滤

**内容：** InventoryItemView 显示图标/名称/装备标记。InventoryView 从 StarChartInventorySO 动态实例化卡片，支持 All/Cores/Prisms/Sails/Satellites 过滤标签。通过外部注入的 Func 检查装备状态。

**目的：** 让玩家浏览和选择可装备的星图部件。

**技术：** 动态 Instantiate UI 预制体，Func 委托解耦装备检查。

---

### 43. ItemDetailView 物品详情 — 2026-02-08 21:25

**新建文件：**
- `Assets/Scripts/UI/ItemDetailView.cs` — 物品详情面板

**内容：** ShowItem 显示名称/描述/属性/装备按钮。根据 SO 类型显示特有属性（Core: 伤害/射速, Prism: 修正列表, Sail: 效果描述, Satellite: 触发/动作描述）。按钮文字 EQUIP/UNEQUIP 自动切换。

**目的：** 装备操作的信息确认界面。

**技术：** switch 模式匹配，StringBuilder 属性构建。

---

### 44. StarChartPanel 面板编排器 — 2026-02-08 21:30

**新建文件：**
- `Assets/Scripts/UI/StarChartPanel.cs` — 星图面板根控制器

**内容：** 编排 TrackView×2 + InventoryView + ItemDetailView。Bind 注入 StarChartController 和 InventorySO。Open/Close 控制显示。选中轨道 → 选中物品 → 点击装备/卸载的完整交互流。支持所有四种部件类型的装备/卸载。

**目的：** Star Chart 面板的交互逻辑核心。

**技术：** 事件链编排，IsItemEquipped 遍历双轨道检查。

---

### 45. UIManager 面板开关 + 暂停 — 2026-02-08 21:35

**新建文件：**
- `Assets/Scripts/UI/UIManager.cs` — 顶层 UI 控制器

**内容：** 监听 Pause action（ESC/Start），Toggle 开关面板。打开时禁用 Fire/FireSecondary action + Time.timeScale=0。关闭时恢复。Start 中 FindAnyObjectByType 查找 StarChartController 和 HeatSystem 并绑定子视图。

**目的：** 游戏内菜单入口和暂停控制。

**技术：** InputAction 单独禁用/启用，Time.timeScale 暂停。

---

### 46. StarChartController 运行时装备 API — 2026-02-08 21:40

**修改文件：**
- `Assets/Scripts/Combat/StarChart/StarChartController.cs`

**内容：** 新增 6 个公开方法供 UI 调用：EquipLightSail, UnequipLightSail, EquipSatellite, UnequipSatellite, GetEquippedLightSail, GetEquippedSatellites。新增 OnLightSailChanged 和 OnSatellitesChanged 事件。内部新增 _equippedLightSailSO 和 _equippedSatelliteSOs 跟踪字段，debug 初始化时同步填充。

**目的：** 让 UI 能在运行时动态装备/卸载光帆和伴星。

**技术：** Runner Dispose + 重建模式，事件通知 UI 刷新。

---

---

## Tint Modifier 安全实例化 — 2026-02-10 15:20

**背景：** Tint 家族棱镜的 `IProjectileModifier`（如 `BounceModifier`、`SlowOnHitModifier`）在 `SnapshotBuilder` 中直接从 prefab 上 `GetComponent<IProjectileModifier>()` 获取组件实例，导致所有核心和所有子弹共享同一个 prefab 引用。有状态的 modifier（如 `BounceModifier._remainingBounces`）会在多颗子弹间产生竞态污染。修复方案将 Tint modifier 统一为与 Anomaly 家族相同的运行时实例化策略（`AddComponent` + `JsonUtility.FromJsonOverwrite`），确保每颗子弹拥有独立的 modifier 实例。

**修改的文件：**
- `Assets/Scripts/Combat/StarChart/FiringSnapshot.cs` — `CoreSnapshot.Modifiers`（`List<IProjectileModifier>`）重命名为 `TintModifierPrefabs`（`List<GameObject>`），存储 prefab 引用而非组件实例。
- `Assets/Scripts/Combat/StarChart/SnapshotBuilder.cs` — `CollectTintModifiers()` 重命名为 `CollectTintModifierPrefabs()`，返回类型从 `List<IProjectileModifier>` 改为 `List<GameObject>`，收集 `PrismSO.ProjectileModifierPrefab` 引用。`BuildCoreSnapshot()` 参数和赋值相应更新。
- `Assets/Scripts/Combat/StarChart/StarChartController.cs` — 新增 `InstantiateModifiers(GameObject targetObj, List<GameObject> prefabs)` 共享工具方法，遍历 prefab 列表对每个 `IProjectileModifier` 组件执行 `AddComponent` + `JsonUtility.FromJsonOverwrite` 创建独立实例。`SpawnMatterProjectile()`、`SpawnLightBeam()`、`SpawnEchoWave()` 改为调用 `InstantiateModifiers()` 获取独立 modifier 列表。`SpawnAnomalyEntity()` 重构为统一使用 `InstantiateModifiers()`，分别处理 Tint 和 Anomaly prefab 后合并结果。
- `Assets/Scripts/Combat/Projectile/LaserBeam.cs` — `OnReturnToPool()` 新增动态 modifier 组件清理逻辑（遍历 `_modifiers` 列表，对 `MonoBehaviour` 类型调用 `Destroy`），防止对象池复用时组件泄漏。
- `Assets/Scripts/Combat/Projectile/EchoWave.cs` — `OnReturnToPool()` 新增相同的动态 modifier 组件清理逻辑。

**目的：** 消除 Tint modifier 共享引用导致的有状态 modifier 竞态污染问题，统一四大家族（Matter/Light/Echo/Anomaly）的 modifier 注入策略为运行时实例化。

**技术：** `AddComponent` + `JsonUtility.FromJsonOverwrite` 运行时深拷贝模式（与 Anomaly 家族一致）。`OnReturnToPool()` 中通过 `is MonoBehaviour mb` 模式匹配识别动态组件并 `Destroy`，与 `Projectile.OnReturnToPool()` 已有的清理模式保持一致。

---

---

## 敌人 AI 基础框架 (Enemy AI Foundation) — 2026-02-10 16:08

### 概述

实现《静默方舟》Phase 1 敌人 AI 基础框架，使用分层状态机 (HFSM) 作为大脑层，配合 GDD 定义的三层架构（躯壳 Body / 大脑 Brain / 导演 Director）。本阶段完成躯壳层、大脑层（HFSM）、基础感知系统、数据驱动配置和第一个可玩原型：莽夫型 (The Rusher)。

### 新建文件

**核心接口与数据：**
- `Assets/Scripts/Combat/IDamageable.cs` — 通用伤害接口，定义 `TakeDamage(float, Vector2, float)` 和 `bool IsAlive`，供子弹/激光/震荡波等攻击源统一调用
- `Assets/Scripts/Combat/Enemy/EnemyStatsSO.cs` — 敌人数据配置 ScriptableObject，包含身份/生命/移动/攻击/攻击阶段/感知/脱战/表现等全部字段，使用 `[Header]` 分组和 `[Min]` 约束

**HFSM 状态机框架：**
- `Assets/Scripts/Combat/Enemy/FSM/IState.cs` — 状态接口，定义 `OnEnter`/`OnUpdate`/`OnExit` 三个生命周期方法
- `Assets/Scripts/Combat/Enemy/FSM/StateMachine.cs` — 纯 C# 状态机类，支持 `Initialize`/`Tick`/`TransitionTo`，转移顺序 OnExit→更新引用→OnEnter，支持嵌套（子状态机）

**躯壳层：**
- `Assets/Scripts/Combat/Enemy/EnemyEntity.cs` — MonoBehaviour，实现 `IDamageable` + `IPoolable`。处理 HP/受击反馈（闪白）/击退/死亡流程/移动执行/对象池生命周期

**感知系统：**
- `Assets/Scripts/Combat/Enemy/EnemyPerception.cs` — MonoBehaviour，实现视觉（扇形 + LoS Raycast，5Hz 定频）和听觉（订阅 `StarChartController.OnWeaponFired` 事件 + 距离校验）感知，支持记忆衰减

**大脑层：**
- `Assets/Scripts/Combat/Enemy/EnemyBrain.cs` — MonoBehaviour，持有外层 HFSM，在 Start 构建状态机，Update 中 Tick。暴露 Entity/Perception/Stats/SpawnPosition 供状态访问。Editor 模式下 OnGUI 显示当前状态名

**外层战术状态：**
- `Assets/Scripts/Combat/Enemy/States/IdleState.cs` — 待机状态，检测 HasTarget 转 Chase
- `Assets/Scripts/Combat/Enemy/States/ChaseState.cs` — 追击状态，直线追击目标，距离判断转 Engage/Return
- `Assets/Scripts/Combat/Enemy/States/EngageState.cs` — 交战状态，持有内层攻击子状态机
- `Assets/Scripts/Combat/Enemy/States/ReturnState.cs` — 回归状态，移动回出生点并恢复满血

**内层攻击子状态（信号-窗口模型）：**
- `Assets/Scripts/Combat/Enemy/States/TelegraphSubState.cs` — 前摇：停止移动，sprite 变红警告，计时后转 Attack
- `Assets/Scripts/Combat/Enemy/States/AttackSubState.cs` — 出招：OverlapCircle 生成伤害区域，不可转向（Commitment），通过 IDamageable 对玩家造成伤害
- `Assets/Scripts/Combat/Enemy/States/RecoverySubState.cs` — 恢复：硬直（玩家反击窗口），计时后标记攻击循环完成

**Editor 工具：**
- `Assets/Scripts/Combat/Editor/EnemyAssetCreator.cs` — 菜单 `ProjectArk > Create Rusher Enemy Assets`，一键创建 EnemyStats_Rusher.asset 并在 Console 输出 Prefab 组装指南

### 修改文件

**子弹系统 IDamageable 集成：**
- `Assets/Scripts/Combat/Projectile/Projectile.cs` — `OnTriggerEnter2D` 中的 TODO 注释替换为 `GetComponent<IDamageable>()?.TakeDamage()` 调用
- `Assets/Scripts/Combat/Projectile/LaserBeam.cs` — `Fire()` 中的 placeholder `Debug.Log` 替换为 `IDamageable.TakeDamage()` 调用
- `Assets/Scripts/Combat/Projectile/EchoWave.cs` — `OnTriggerEnter2D` 中的 placeholder `Debug.Log` 替换为 `IDamageable.TakeDamage()` 调用

**开火事件广播：**
- `Assets/Scripts/Combat/StarChart/StarChartController.cs` — 新增 `public static event Action<Vector2, float> OnWeaponFired`，在 SpawnMatterProjectile/SpawnLightBeam/SpawnEchoWave/SpawnAnomalyEntity 中广播（位置 + 15f 噪音半径），供敌人听觉感知订阅

### 架构说明

```
┌─────────────────────────────────────────────────┐
│              AI Brain (HFSM)                     │
│  ┌─────────────────────────────────────────┐    │
│  │  外层状态机 (战术层)                       │    │
│  │  Idle ──→ Chase ──→ Engage ──→ Return   │    │
│  └─────────────────┬───────────────────────┘    │
│                    │                             │
│  ┌─────────────────▼───────────────────────┐    │
│  │  内层状态机 (攻击层)                       │    │
│  │  Telegraph → Attack → Recovery          │    │
│  └─────────────────────────────────────────┘    │
└──────────────────────┬──────────────────────────┘
                       │ 指令
┌──────────────────────▼──────────────────────────┐
│           EnemyEntity (躯壳层)                    │
│  移动执行 · HP/韧性 · 受击反馈 · 对象池            │
└─────────────────────────────────────────────────┘
```

**设计原则：**
- 数据驱动：所有数值通过 EnemyStatsSO 配置，禁止 hardcode
- HFSM 纯 C# 类（非 MonoBehaviour），可单元测试，零 GC
- 信号-窗口攻击模型：Telegraph(读取信号) → Attack(承诺窗口) → Recovery(惩罚窗口)
- 子弹通过 IDamageable 接口统一造伤，解耦攻击源与目标类型
- 感知系统事件驱动（听觉）+ 定频轮询（视觉 5Hz），平衡性能与响应性

**技术：** HFSM 分层状态机, IDamageable 接口多态, Physics2D.OverlapCircle 扇形检测, C# event 事件通信, ScriptableObject 数据驱动, IPoolable 对象池集成。

### Prefab 组装指南（需手动在 Editor 中完成）

1. 创建空 GameObject `Enemy_Rusher`，设置 Layer 为 `Enemy`（需在 TagManager 中创建）
2. 添加组件：`EnemyEntity` + `EnemyBrain` + `EnemyPerception`
3. Rigidbody2D：Dynamic, Gravity Scale=0, Freeze Rotation Z, Interpolate
4. CircleCollider2D：Radius=0.4
5. SpriteRenderer：分配占位 sprite
6. 拖入 `EnemyStats_Rusher` SO 到 EnemyEntity 和 EnemyPerception 的 `_stats` 字段
7. EnemyPerception：Player Mask=Player 层, Obstacle Mask=Wall 层
8. Physics2D 碰撞矩阵：PlayerProjectile↔Enemy=ON, Player↔Enemy=ON
9. 保存为 Prefab 到 `Assets/_Prefabs/Enemies/Enemy_Rusher.prefab`

---

---

## Enemy AI 系统集成 — 玩家受伤 + 敌人生成闭环 (2026-02-10 23:30)

### 概要

将已实现的 Enemy AI 代码框架正式接入游戏循环，完成三个关键缺失环节：
1. 玩家飞船实现 `IDamageable` 接口，使敌人攻击能造成伤害
2. 创建 `EnemySpawner` 组件，通过对象池管理敌人运行时生成
3. 场景配置与完整战斗循环验证

### 新建文件

- `Assets/Scripts/Ship/Combat/ShipHealth.cs` — 玩家飞船生命值组件，实现 `IDamageable` 接口
  - 命名空间 `ProjectArk.Ship`，使用 `[RequireComponent(typeof(Rigidbody2D))]`
  - 从 `ShipStatsSO.MaxHP` 读取初始 HP（数据驱动，不硬编码）
  - `TakeDamage(float, Vector2, float)` 流程：死亡判断 → 扣减 HP → Rigidbody2D.AddForce(Impulse) 击退 → 协程闪白 → 触发 OnDamageTaken 事件 → HP ≤ 0 调用 Die()
  - `Die()` 流程：标记 `_isDead = true` → 禁用 InputHandler → 触发 OnDeath 事件（Game Over/重生逻辑暂未实现，预留事件订阅点）
  - 暴露 `OnDamageTaken(float damage, float currentHP)` 和 `OnDeath` 两个 Action 事件供 UI/其他系统订阅
  - 提供 `ResetHealth()` 公共方法用于未来重生/关卡重启

- `Assets/Scripts/Combat/Enemy/EnemySpawner.cs` — 敌人生成管理器
  - 命名空间 `ProjectArk.Combat.Enemy`
  - Inspector 暴露字段：`_enemyPrefab`(GameObject)、`_spawnPoints`(Transform[])、`_maxAlive`(int, 默认3)、`_spawnInterval`(float, 默认5s)、`_initialSpawnCount`(int, 默认1)、`_poolPrewarmCount`(int, 默认5)、`_poolMaxSize`(int, 默认10)
  - Start() 中创建 `GameObjectPool` 并执行初始生成（`_initialSpawnCount` 个，不超过 `_maxAlive`）
  - `SpawnEnemy()` 流程：检查存活上限 → Round-robin 选取 SpawnPoint → Pool.Get() → EnemyBrain.ResetBrain() 重置 AI → 订阅 EnemyEntity.OnDeath → 更新 _aliveCount
  - 敌人死亡回调：_aliveCount-- → 协程延迟 _spawnInterval 秒后补充生成
  - 注意：EnemyEntity.OnReturnToPool() 会清空事件订阅者，因此每次从池中取出时重新订阅 OnDeath
  - Editor Gizmos：选中时绘制绿色连线 + 圆圈标记刷怪点

### 修改文件

- `Assets/Scripts/Ship/Data/ShipStatsSO.cs` — 新增 `[Header("Survival")]` 区域
  - 添加 `_maxHP`(float, 默认100) 和 `_hitFlashDuration`(float, 默认0.1s) 字段
  - 添加对应公共属性 `MaxHP` 和 `HitFlashDuration`
  - 不影响现有 Movement/Aiming 参数，向后兼容 `DefaultShipStats.asset`

### 场景配置（手动在 Unity Editor 中完成）

- 飞船 GameObject 挂载 `ShipHealth` 组件，绑定 `DefaultShipStats` SO
- 创建 `EnemySpawner` GameObject 挂载 EnemySpawner 组件，含 `SpawnPoint_1`(-8,-5)、`SpawnPoint_2`(8,5) 两个子物体
- EnemySpawner 配置：enemyPrefab=Enemy_Rusher.prefab, maxAlive=3, spawnInterval=5s, initialSpawnCount=1, poolPrewarm=5, poolMax=10

### 问题修复

- 修复 `.meta` 文件 GUID 格式错误：`IDamageable.cs.meta`、`ShipHealth.cs.meta`、`EnemySpawner.cs.meta` 三个文件的 GUID 包含非法破折号（手动编造导致），删除后由 Unity 重新生成正确格式的 meta 文件

### Play Mode 验证结果（Editor Log 确认）

- ✅ 零编译错误（`error CS` 搜索无结果）
- ✅ 三个 meta GUID 问题已修复（`does not have a valid GUID` 搜索无结果）
- ✅ EnemySpawner 正常生成敌人：`[EnemySpawner] Spawned enemy at (8.00, 5.00, 0.00). Alive: 1/3`
- ✅ 完整伤害链路通畅：`Projectile.OnTriggerEnter2D` → `EnemyEntity.TakeDamage` → `EnemyEntity.Die` → `EnemySpawner.OnEnemyDied`
- ✅ 重生机制运作：死亡后延迟重生 `[EnemySpawner] Spawned enemy at (-8.00, -5.00, 0.00). Alive: 1/3`
- ✅ 无运行时 NullReferenceException
- ✅ Asset Pipeline 正常：scripts=1544, non-scripts=3163

### 架构说明

```
┌──────────────────────────────────────────────────────────┐
│                    EnemySpawner                           │
│  GameObjectPool → Get/Return → EnemyBrain.ResetBrain()   │
│  OnDeath 订阅 → _aliveCount 管理 → 延迟重生协程          │
└───────────────────────┬──────────────────────────────────┘
                        │ 生成
┌───────────────────────▼──────────────────────────────────┐
│               Enemy (Rusher)                              │
│  EnemyBrain(HFSM) → EngageState → AttackSubState         │
│                                      │                    │
│                          Physics2D.OverlapCircleAll       │
│                          LayerMask("Player")              │
│                                      │                    │
│                          GetComponent<IDamageable>()      │
│                                      │                    │
│                          TakeDamage(dmg, dir, force)      │
└──────────────────────────────────────┬───────────────────┘
                                       │ 攻击
┌──────────────────────────────────────▼───────────────────┐
│               Player Ship                                 │
│  ShipHealth : IDamageable                                 │
│  HP 扣减 → 击退(Impulse) → 闪白 → OnDamageTaken          │
│  HP ≤ 0 → Die() → 禁用 InputHandler → OnDeath            │
│                                                           │
│  ◄─── Projectile.OnTriggerEnter2D ───►                   │
│  子弹命中敌人 → EnemyEntity.TakeDamage → 敌人死亡 → 回池   │
└──────────────────────────────────────────────────────────┘
```

**双向伤害循环：** 敌人通过 `AttackSubState.TryHitPlayer()` + `Physics2D.OverlapCircleAll` 检测 Player Layer → `IDamageable.TakeDamage()` 伤害飞船；飞船通过 `Projectile.OnTriggerEnter2D` 检测 Enemy Layer → `IDamageable.TakeDamage()` 伤害敌人。两个方向统一使用 `IDamageable` 接口，完全解耦。

**技术：** IDamageable 接口多态, GameObjectPool 对象池, C# event 事件通信, Rigidbody2D.AddForce(Impulse) 击退, 协程闪白反馈, ScriptableObject 数据驱动, Round-robin 刷怪点选择。

---

---

## 飞船血条 HUD (Ship Health Bar UI) — 2026-02-11 13:30

### 新建文件

- `Assets/Scripts/UI/HealthBarHUD.cs` — 飞船血条 HUD 组件

**内容：** 遵循与 HeatBarHUD 相同的 `Bind()` 事件驱动模式。通过 `Bind(ShipHealth)` 注入引用，订阅 `OnDamageTaken` 和 `OnDeath` 事件。功能包括：
- 填充条（`Image.Filled`）+ `Gradient` 渐变色（绿→黄→红）
- 受击闪烁动画（红色闪光叠加层，淡出）
- 数值标签（`HP {current}/{max}`）
- 低血量脉冲警告（HP ≤ 30% 时填充条 alpha 脉冲）
- 死亡状态显示 `"DESTROYED"` 文字

### 修改文件

- `Assets/Scripts/UI/UIManager.cs` — 新增 `_healthBarHUD` 字段，`Start()` 中自动查找 `ShipHealth` 并调用 `_healthBarHUD.Bind(shipHealth)`
- `Assets/Scripts/UI/Editor/UICanvasBuilder.cs` — `BuildUICanvas()` 中新增 Step 2b，自动创建 HealthBarHUD 层级（Background + FillImage + DamageFlash + Label），自动连线所有 SerializeField 引用

### UI 布局

| 元素 | 锚点位置 | 说明 |
|------|---------|------|
| HealthBarHUD | 左上 (0.02, 0.92) ~ (0.28, 0.97) | 始终可见 |
| HeatBarHUD | 底部居中 (0.3, 0) ~ (0.7, 0.05) | 始终可见 |

**目的：** 战斗中显示飞船生命值状态，与热量条共同构成核心战斗 HUD。

**技术：** 事件驱动 UI 更新（C# event 订阅），Gradient 渐变色映射，`Time.unscaledDeltaTime` 动画（兼容 timeScale=0），Bind/Unbind 生命周期管理。

---

---

## HUD Gradient 修复 & UICanvasBuilder 完善 — 2026-02-11 14:09

### 修改文件

- `Assets/Scripts/UI/Editor/UICanvasBuilder.cs`

**问题：** HeatBarHUD 的 `_heatGradient` 和 HealthBarHUD 的 `_healthGradient` 字段类型为 `Gradient`（值类型），无法用 `WireField`（基于 `objectReferenceValue`）注入，导致 Builder 创建的 HUD 渐变效果缺失——填充条始终保持初始颜色不变。

**修复内容：**
1. 新增 `WireGradient()` helper 方法，使用 `SerializedProperty.gradientValue` 正确注入 `Gradient` 值
2. 新增 `CreateHeatGradient()`：绿(0%) → 黄(50%) → 红(100%)，用于热量条
3. 新增 `CreateHealthGradient()`：红(0%) → 黄(40%) → 绿(100%)，用于血条
4. 在 HeatBarHUD 和 HealthBarHUD 的 Wire 阶段分别调用注入

**补充说明：** StarChartPanel 在 Builder 的 Step 6 中被 `SetActive(false)` 是刻意设计，星图面板默认隐藏，由 `UIManager.Toggle()` 控制开关。

**技术：** `SerializedProperty.gradientValue` API（Unity Editor only），`GradientColorKey`/`GradientAlphaKey` 程序化创建渐变。

---

---

## 莽夫近战修复 + 射手型敌人实现 (2026-02-11 14:16)

### 问题修复：莽夫型敌人近战攻击不造成伤害

**根因分析：** Ship Prefab 根 GameObject 缺少 `Collider2D`，导致 `AttackSubState.TryHitPlayer()` 中的 `Physics2D.OverlapCircleAll` 无法检测到玩家。`ShipHealth` 虽已实现 `IDamageable`，但物理系统需要 Collider 才能被 OverlapCircle 捕获。

**修复文件：**
- `Assets/Scripts/Ship/Combat/ShipHealth.cs` — 添加 `[RequireComponent(typeof(Collider2D))]`，确保 Ship 必有碰撞体

### 新增：射手型敌人 (Shooter)

射手型是远程攻击型敌人，与莽夫型形成战术互补：莽夫冲锋近战施压，射手保持距离输出弹幕。

**行为状态图：**
```
Idle → [发现目标] → Chase → [进入 PreferredRange] → Shoot (Telegraph→Burst Fire→Recovery)
                                                        ↓ 玩家过近 (< RetreatRange)
                                                    Retreat (边退边保持朝向)
                                                        ↓ 恢复安全距离
                                                      Shoot (继续射击)
                                                        ↓ 超出 LeashRange / 丢失目标
                                                      Return → Idle
```

**新建文件：**
| 文件 | 用途 |
|------|------|
| `Assets/Scripts/Combat/Enemy/EnemyProjectile.cs` | 敌人子弹组件（检测 Player 层，忽略 Enemy 层，对象池回收） |
| `Assets/Scripts/Combat/Enemy/ShooterBrain.cs` | 射手型大脑层（继承 EnemyBrain，override BuildStateMachine 组装远程 HFSM） |
| `Assets/Scripts/Combat/Enemy/States/ShootState.cs` | 远程攻击状态（Telegraph 红闪→Burst 连射→Recovery 硬直，内嵌子状态机） |
| `Assets/Scripts/Combat/Enemy/States/RetreatState.cs` | 后撤状态（远离玩家至 PreferredRange，超出 LeashRange 转 Return） |

**修改文件：**
| 文件 | 变更 |
|------|------|
| `Assets/Scripts/Combat/Enemy/EnemyStatsSO.cs` | 新增 Ranged Attack 字段组：ProjectilePrefab, ProjectileSpeed, ProjectileDamage, ProjectileKnockback, ProjectileLifetime, ShotsPerBurst, BurstInterval, PreferredRange, RetreatRange |
| `Assets/Scripts/Combat/Enemy/EnemyBrain.cs` | 字段改 protected、方法改 virtual，支持 ShooterBrain 继承 |
| `Assets/Scripts/Combat/Enemy/States/ChaseState.cs` | 添加 ShooterBrain 多态分支：射手型进入 ShootState 而非 EngageState |
| `Assets/Scripts/Combat/Editor/EnemyAssetCreator.cs` | 新增 `Create Shooter Enemy Assets` 菜单项 + `CreateEnemyProjectilePrefab()` 辅助方法 |

**射手型 SO 预设数值（EnemyStats_Shooter）：**
| 分类 | 字段 | 值 | 设计意图 |
|------|------|----|----------|
| Health | MaxHP | 40 | 脆皮，鼓励玩家优先击杀 |
| Movement | MoveSpeed | 3.5 | 比莽夫慢，无法轻易逃脱 |
| Ranged | ProjectileSpeed | 10 | 中速弹，可闪避 |
| | ShotsPerBurst | 3 | 三连发，间隔 0.2s |
| | PreferredRange | 10 | 理想射击距离 |
| | RetreatRange | 5 | 玩家逼近此距离时后撤 |
| Perception | SightRange | 16 | 远视距补偿远程定位 |
| | SightAngle | 90° | 宽视锥，更容易发现玩家 |
| Visual | BaseColor | (0.2, 0.4, 0.9) | 冷蓝色，与莽夫红色区分 |

**架构决策：**
- `ShooterBrain` 继承 `EnemyBrain` 而非独立实现，复用 Idle/Chase/Return 共享状态
- `EnemyProjectile` 独立于玩家 `Projectile`，避免星图修改器系统的不必要耦合
- `ShootState` 内嵌 Telegraph→Burst→Recovery 子状态机，复用信号-窗口设计模式
- `ChaseState` 通过 `brain is ShooterBrain` 多态判断决定转入 Shoot 还是 Engage

**技术：** 继承 + 虚方法 override（EnemyBrain→ShooterBrain），PoolManager 对象池（EnemyProjectile），Physics2D.OverlapCircleAll + LayerMask（碰撞检测）。

---

### StarChart Component Data — 示巴星 & 机械坟场部件设计写入 (2026-02-11 15:19)

根据 `StarChartPlanning.csv` 对示巴星（新手期）和机械坟场（拓展期）的规划，完成了4个星图部件数据表的批量写入。

**修改文件：**
| 文件 | 变更 |
|------|------|
| `Docs/DataTables/StarChart_StarCores.csv` | 新增7个星核 (ID 1006–1012)：散弹、棱光射线、脉冲新星、转管炮、布雷器、裂变光束、余震 |
| `Docs/DataTables/StarChart_Prisms.csv` | 新增12个棱镜 (ID 2006–2017)：连射、重弹、远射、三连发、冲击、轻载、灼烧、反弹、齐射、减速弹、微缩、腐蚀 |
| `Docs/DataTables/StarChart_LightSails.csv` | 新增4个光帆 (ID 3005–3008)：标准航行帆、斥候帆、重装帆、脉冲帆 |
| `Docs/DataTables/StarChart_Satellites.csv` | 新增2个伴星 (ID 4005–4006)：自动机炮、拾取磁铁 |

**示巴星新增（10件）：**
- StarCores ×1：实相·风暴散射 (近距离扇形散射×5)
- Prisms ×6：连射/重弹/远射/三连发/冲击/轻载（基础数值类，教会玩家棱镜 trade-off）
- LightSails ×2：标准航行帆(无效果基准线) / 斥候帆(高速加伤)
- Satellites ×1：自动机炮(OnInterval 自动开火)

**机械坟场新增（15件）：**
- StarCores ×6：棱光射线/脉冲新星/转管炮(2格)/布雷器/裂变光束(2格三叉)/余震（元素类，流派分化）
- Prisms ×6：灼烧(DoT)/反弹(碰墙3次)/齐射(重叠双弹)/减速弹/微缩/腐蚀(降防)（手感类，空间控制维度）
- LightSails ×2：重装帆(站桩减伤,禁冲刺) / 脉冲帆(冲刺造伤)
- Satellites ×1：拾取磁铁(OnAlways 吸引掉落物)

**设计哲学：**
- 示巴星部件全部为简单数值修正，降低新手认知负荷
- 机械坟场引入攻防流派分化：重装帆(阵地战) vs 脉冲帆(游击战)
- 数值以撕裂者 DPS(120) 为基准锚定，2格武器提供约110%效率但占用更多槽位
- 棱镜 AddHeat 可为负值（轻载/微缩），作为低复杂度选项的奖励机制

**部件总数：** StarCores 12 / Prisms 17 / LightSails 8 / Satellites 6 = 共43件

---

---

## Enemy_Bestiary.CSV 配置表重设计 — 2026-02-11 16:55

将 `Enemy_Bestiary.csv` 从 15 列扩展为 **50 列**的完整配置表，使其成为敌人数据的唯一权威数据源 (Single Source of Truth)，支持 CSV → SO 自动化导入管线。

### 新建文件

| 文件 | 说明 |
|------|------|
| `Assets/Scripts/Combat/Editor/BestiaryImporter.cs` | Editor 工具脚本，提供 `ProjectArk > Import Enemy Bestiary` 菜单项，一键从 CSV 批量生成/更新 `EnemyStatsSO` 资产 |

### 修改文件

| 文件 | 变更 |
|------|------|
| `Assets/Scripts/Combat/Enemy/EnemyStatsSO.cs` | 新增 10 个字段：5 个抗性 (`Resist_Physical/Fire/Ice/Lightning/Void`, Range 0~1)、`DropTableID` (string)、`PlanetID` (string)、`SpawnWeight` (float)、`BehaviorTags` (List\<string\>)；新增 `using System.Collections.Generic` |
| `Docs/DataTables/Enemy_Bestiary.csv` | 从 15 列重建为 50 列（12 个分组 A~L），已有 6 行敌人数据完整迁移并补填缺失数值 |

### 配置表结构（50 列 × 12 分组）

- **A. 身份与元数据** (7列)：ID, InternalName, DisplayName, Rank, AI_Archetype, FactionID, PlanetID
- **B. 生命与韧性** (2列)：MaxHP, MaxPoise
- **C. 移动** (2列)：MoveSpeed, RotationSpeed
- **D. 近战攻击** (4列)：AttackDamage, AttackRange, AttackCooldown, AttackKnockback
- **E. 攻击阶段** (3列)：TelegraphDuration, AttackActiveDuration, RecoveryDuration
- **F. 远程攻击** (9列)：ProjectilePrefab ~ RetreatRange
- **G. 感知** (3列)：SightRange, SightAngle, HearingRange
- **H. 栓绳与记忆** (2列)：LeashRange, MemoryDuration
- **I. 抗性** (5列)：Resist_Physical/Fire/Ice/Lightning/Void
- **J. 奖励与掉落** (2列)：ExpReward, DropTableID
- **K. 视觉反馈** (5列)：HitFlashDuration, BaseColor_R/G/B/A, PrefabPath
- **L. 行为标签与设计备注** (5列)：BehaviorTags, SpawnWeight, DesignIntent, PlayerCounter, Description_Note

### BestiaryImporter 功能

- **菜单入口**：`ProjectArk > Import Enemy Bestiary`
- **CSV 解析**：支持逗号分隔、引号转义（双引号 `""` 语法）
- **字段映射**：显式映射 30+ 个 SO 字段，自动跳过纯策划列 (Rank, AI_Archetype, FactionID, ExpReward, DesignIntent, PlayerCounter, *_Note)
- **特殊处理**：BaseColor_RGBA 四列合并为 Color、ProjectilePrefab 路径转 GameObject 引用、BehaviorTags 分号分隔拆分为 List\<string\>
- **空字段策略**：CSV 为空时保留 SO 默认值不覆盖
- **报告**：导入完成后弹窗汇总 Created / Updated / Skipped 数量及耗时

### 数据迁移

- 6 行已有敌人 (ID 5001–5006) 完整迁移
- 旧列名映射：`MaxHealth` → `MaxHP`、`Poise` → `MaxPoise`、`AggroRange` → `SightRange`、`Description` → `Description_Note`
- 缺失数值参考 `EnemyAssetCreator.cs` 中 Rusher/Shooter 预设补填

---

---

## Enemy_Bestiary 怪物数据填充 — P1 示巴星 & P2 机械坟场 (2026-02-11 17:15)

### 概述

完成 `Enemy_Bestiary.csv` 的完整怪物数据填充，覆盖 P1 示巴星（废弃矿坑）和 P2 机械坟场（剧毒沼泽）两个星球的全部怪物。从 6 行原型数据扩展至 26 行完整配置。

### 修改文件

| 文件 | 变更内容 |
|------|----------|
| `Docs/DataTables/Enemy_Bestiary.csv` | 完善 5001–5006 共 6 行已有数据（更新 DisplayName、补齐 DesignIntent/PlayerCounter/Description_Note 等策划列）；新增 5007–5026 共 20 行怪物数据 |

### P1 示巴星怪物（ID 5001–5014，共 14 种）

**已有数据完善（5001–5006）：**
- 5001 深渊爬行者：确认为最基础 Minion，EXP=5，无抗性
- 5002 装甲爬行者：Elite 定位，物理抗性 0.3，EXP=20
- 5003 更名为"工蜂无人机"：匹配 EnemyPlanning 规划，Ranged_Kiter 远程基础单位
- 5004 更名为"天花板钻头"：Stationary_Turret 固定型，MoveSpeed=0，垂直攻击
- 5005 暗影潜伏者：Ambusher 刺客型，添加 Invisible 标签，虚空抗性 0.3
- 5006 更名为"锈蚀女王"：Boss 定位，多阶段 AI，SuperArmor 霸体

**新增怪物（5007–5014）：**
- 5007 酸蚀爬行者 (Minion)：死亡留酸液，战场分割者
- 5008 自爆蜱虫 (Minion)：HP=15 极脆 + Speed=7 极快 + 自爆伤害 30
- 5009 重型装载机 (Defense)：正面无敌 FrontShield，HP=100，绕背教学怪
- 5010 晶体甲虫 (Specialist)：反射激光 ReflectLaser，武器克制教学
- 5011 修理博比特 (Support)：零攻击纯治疗 Healer，击杀优先级教学
- 5012 暴走工头 (Elite)：HP=180 霸体冲撞 + 召唤爬行者，P1 综合考题
- 5013 盗矿地精 (Gimmick)：Speed=8 逃跑型，零攻击高 EXP=50，贪婪测试
- 5014 挖掘者 9000 (Mini-Boss)：HP=1500 游荡型，冲撞+落石+激光扫射

### P2 机械坟场怪物（ID 5015–5026，共 12 种）

**数值升级原则：** 同级别比 P1 提升 30%–50%

- 5015 机械猎犬 (Minion)：Melee_Flanker 侧翼包抄 + 死亡自爆
- 5016 狙击炮塔 (Ranged)：Stationary_Turret 不可移动，射程=20 极远，伤害=40
- 5017 迫击炮手 (Ranged)：Ranged_Lobber 抛物线榴弹 + 燃烧区域 AreaDenial
- 5018 方阵兵 (Defense)：Shield_Wall 结对激光墙 LaserWall;Paired
- 5019 磁力钩锁 (Specialist)：Ranged_Utility 强制拉近 ForcedPull + 晕眩 Stun
- 5020 干扰水晶 (Specialist)：Stationary_Aura 不可移动，封印技能 SkillJam
- 5021 隐形猎手 (Specialist)：Ambusher 光学迷彩 + 背刺暴击 CritStrike
- 5022 感应地雷 (Hazard)：Stationary_Trap HP=10 极脆，爆炸伤害=35，可引诱怪物踩踏
- 5023 反应装甲兵 (Specialist)：Counter_Attacker 受击反弹导弹 DamageReflect，射速惩罚
- 5024 电磁处刑者 (Elite)：Melee_Teleporter 瞬移连击 + 沉默 Silence，HP=150
- 5025 焚化炉 (Boss)：HP=3000 全屏高温 DOT + 召唤废料雨，DPS 检测
- 5026 游荡者 (Mini-Boss)：HP=1200 狙击风筝 + 逃跑回血 Regen，反向风筝体验

### 设计哲学

**P1 示巴星 — 教学生态：**
- 炮灰层（爬行者/蜱虫）→ 远程层（无人机/钻头）→ 防御层（装载机）→ 特化层（甲虫/博比特）→ 精英层（工头）→ Boss 层（锈蚀女王/挖掘者）
- 每种怪物承担一个明确的教学职责，循序渐进引导玩家掌握战斗语法

**P2 机械坟场 — 组合升级：**
- 引入"组合威胁"设计（磁力钩锁+自爆怪、方阵兵+迫击炮后排）
- 引入"流派检测"设计（反应装甲兵惩罚高射速、干扰水晶封印技能流）
- Boss 哲学差异化（焚化炉=DPS 检测 vs 游荡者=机动性检测）

### 数值规范遵循

- 信号-窗口模型：前摇 ≥ 判定窗口 ≥ 后摇
- 感知规则：HearingRange > SightRange，Boss SightAngle=360
- 抗性规则：Minion 无抗性、Elite 特色抗性、Boss 均匀中等抗性+主题弱点
- EXP 阶梯：Minion 5–15 / Elite 20–50 / Mini-Boss 100–200 / Boss 500+
- Boss/Mini-Boss：LeashRange=999（不脱战）、MemoryDuration=999（不遗忘）

---

---

## Phase 1 遗留项补完：韧性 (Poise) + 顿帧 (HitStop) + 群聚算法 (Boids) — 2026-02-11 19:30

### 概述

补完敌人 AI Phase 1 的三个遗留项，完善战斗手感与多敌人体验。

### 新建文件

- `Assets/Scripts/Core/HitStopEffect.cs` — 全局顿帧效果单例
  - 通过 `[RuntimeInitializeOnLoadMethod]` 自动创建持久 `[HitStop]` 对象
  - 静态 API `HitStopEffect.Trigger(float duration)` 供全局调用
  - 暂停 `Time.timeScale` 指定真实时间秒后恢复，使用 `Time.unscaledDeltaTime` 计时
  - 安全保护：星图面板已暂停时跳过、重叠调用取最长、销毁时恢复 timeScale

- `Assets/Scripts/Combat/Enemy/States/StaggerState.cs` — 韧性击破硬直状态
  - 敌人停止移动，sprite 变黄色（与前摇红色区分）
  - 水平震动视觉反馈（`Sin(Time * 40) * 0.06` 强度）
  - 持续 `EnemyStatsSO.StaggerDuration` 秒后重置韧性，转 Chase/Idle
  - 退出时恢复位置和颜色

### 修改文件

| 文件 | 变更 |
|------|------|
| `Assets/Scripts/Combat/Enemy/EnemyStatsSO.cs` | 新增 `[Header("Poise & Stagger")]` 区域，添加 `StaggerDuration`(float, 默认1.0s) 字段 |
| `Assets/Scripts/Combat/Enemy/EnemyEntity.cs` | **韧性系统**：`TakeDamage()` 中增加 poise 削减逻辑（伤害值=韧性伤害），poise ≤ 0 时标记 `IsStaggered` 并触发 `OnPoiseBroken` 事件 + 韧性击破时调用 `HitStopEffect.Trigger(0.08f)`；`Die()` 中调用 `HitStopEffect.Trigger(0.06f)`；新增 `ResetPoise()` 公开方法；新增 `OnPoiseBroken` 事件、`IsStaggered` 属性、`CurrentPoise` 属性；**群聚算法**：新增 `GetSeparationForce(float radius, float strength)` 方法，使用 `Physics2D.OverlapCircleAll` + Enemy Layer 检测邻近敌人，返回反距离加权推离向量；`OnReturnToPool()` 中新增 `OnPoiseBroken = null` 和 `IsStaggered = false` 清理 |
| `Assets/Scripts/Combat/Enemy/EnemyBrain.cs` | 新增 `StaggerState` 公开属性；`BuildStateMachine()` 中创建 `_staggerState` 实例并调用 `SubscribePoiseBroken()`；新增 `SubscribePoiseBroken()` 订阅 Entity 事件；新增 `ForceStagger()` 方法（检查 `SuperArmor` BehaviorTag 豁免）；新增 `OnDisable()` 取消订阅 |
| `Assets/Scripts/Combat/Enemy/States/ChaseState.cs` | 移动方向从纯追击改为追击方向 + `GetSeparationForce()` 混合（权重 0.3），防止多只近战敌人重叠堆积 |

### 系统设计

**韧性 (Poise) 系统：**
```
受击 → 伤害值同时削减 HP 和 Poise
  └→ Poise ≤ 0 → IsStaggered=true → OnPoiseBroken 事件
       └→ EnemyBrain.ForceStagger()
            ├→ 有 SuperArmor 标签 → 直接 ResetPoise()，不进入硬直
            └→ 无 SuperArmor → TransitionTo(StaggerState)
                 └→ StaggerDuration 秒后 → ResetPoise() → Chase/Idle
```

**顿帧 (HitStop) 时机与强度：**
| 触发事件 | 冻结时长 | 体感 |
|---------|---------|------|
| 韧性击破 | 0.08s | 明显顿挫，"打碎盔甲"的权重感 |
| 击杀 | 0.06s | 中等顿挫，"致命一击"的终结感 |

**群聚算法 (Boids Separation)：**
- 检测半径 1.5 单位内同 Layer 邻居
- 反距离加权：越近推力越大
- 混合权重 0.3：追击意图为主，分散为辅
- 结果：多只莽夫追击时呈扇形散开而非直线叠加

**技术：** `[RuntimeInitializeOnLoadMethod]` 自动单例创建, `Time.unscaledDeltaTime` 顿帧计时, C# event 事件驱动韧性击破, Physics2D.OverlapCircleAll Boids 邻居检测, BehaviorTags 字符串查询 SuperArmor 豁免。

---

---

## Bug Fix：敌人重叠堆叠修复 — 2026-02-11 20:15

### 问题
多只 Rusher 进入攻击状态（Engage/Telegraph）后停止移动，堆叠在同一坐标。原因：Boids 分离力仅在 `ChaseState` 中应用，一旦离开追击状态就完全失效；且距离 ≈ 0 时 `GetSeparationForce` 直接跳过（`if (dist < 0.001f) continue`），完美重叠时推力为零。

### 修改文件

| 文件路径 | 变更说明 |
|---------|---------|
| `Assets/Scripts/Combat/Enemy/EnemyEntity.cs` | 新增 `FixedUpdate()` + `ResolveOverlap()` 方法：使用 `Physics2D.OverlapCircleAll` 检测 `MIN_ENEMY_DISTANCE`(0.9) 范围内同 Layer 邻居，直接通过 `_rigidbody.position += push` 消解重叠（每人各承担一半重叠量）；完全重叠时生成随机方向避免死锁。修复 `GetSeparationForce` 中距离 ≈ 0 的处理：改为随机方向推开而非跳过。 |
| `Assets/Scripts/Combat/Enemy/States/ChaseState.cs` | `SEPARATION_WEIGHT` 从 0.3 提升至 0.6，在追击阶段就更积极地分散。 |

### 设计要点
- **FixedUpdate 位置消解** vs 力/速度方案：因为 `MoveTo()` 每帧直接设置 `linearVelocity`，AddForce 会被覆盖；直接修改 `_rigidbody.position` 是唯一在所有状态下可靠生效的方式。
- **双保险架构**：ChaseState 中的 Boids 方向混合（预防性扇形散开） + FixedUpdate 中的硬性重叠消解（兜底，任何状态生效）。

---

---

## Phase 2 完整实现：AttackDataSO + 导演系统 + 炮台原型 — 2026-02-11 21:00

### 概览

Phase 2 包含三大模块，按依赖顺序实现：
- **Phase 2A**：AttackDataSO 数据驱动攻击系统 + HitboxResolver 碰撞检测
- **Phase 2B**：EnemyDirector 导演系统（攻击令牌 + OrbitState 环绕等待）
- **Phase 2C**：Turret 炮台原型（激光 + 蓄力弹双变体）

### Phase 2A — AttackDataSO + Hitbox 系统

#### 新建文件

| 文件路径 | 说明 |
|---------|------|
| `Assets/Scripts/Combat/Enemy/AttackDataSO.cs` | ScriptableObject 定义单个攻击模式：`AttackType` 枚举 (Melee/Projectile/Laser) + `HitboxShape` 枚举 (Circle/Box/Cone) + 信号窗口三阶段时长 + 伤害/击退 + 碰撞形状参数 + 远程弹丸/激光参数 + 权重选择 + 视觉配置 |
| `Assets/Scripts/Combat/Enemy/HitboxResolver.cs` | 静态工具类，NonAlloc 共享缓冲区。`Resolve()` 根据 `HitboxShape` 执行不同 Physics2D 查询：Circle (`OverlapCircleNonAlloc`)、Box (`OverlapBoxNonAlloc`)、Cone (Circle + 角度过滤)。附带 Editor Gizmo 绘制 |

#### 修改文件

| 文件路径 | 变更说明 |
|---------|---------|
| `Assets/Scripts/Combat/Enemy/EnemyStatsSO.cs` | 新增 `AttackDataSO[] Attacks` 数组 + `HasAttackData` 属性 + `SelectRandomAttack()` 加权随机选择方法 |
| `Assets/Scripts/Combat/Enemy/States/EngageState.cs` | 新增 `SelectedAttack` 属性，`OnEnter()` 中执行 `SelectRandomAttack()`，子状态通过 `_engage.SelectedAttack` 读取；`OnExit()` 归还导演令牌 |
| `Assets/Scripts/Combat/Enemy/States/TelegraphSubState.cs` | 时长和颜色从 `SelectedAttack` 读取（null 时回退 legacy 字段） |
| `Assets/Scripts/Combat/Enemy/States/AttackSubState.cs` | 使用 `HitboxResolver.Resolve()` 替换原 `OverlapCircleAll`；伤害/击退从 `SelectedAttack` 读取；增加 legacy NonAlloc 缓冲区 |
| `Assets/Scripts/Combat/Enemy/States/RecoverySubState.cs` | 后摇时长从 `SelectedAttack` 读取 |
| `Assets/Scripts/Combat/Enemy/States/ShootState.cs` | 新增 `SelectProjectileAttack()` 筛选 Projectile 类型 AttackDataSO；所有阶段时长/弹丸参数优先从 AttackDataSO 读取；`OnExit()` 归还导演令牌 |

**设计要点**：全部修改保持 **100% 向后兼容** —— 当 `Attacks[]` 为空时，所有状态自动回退到 `EnemyStatsSO` 的遗留平面字段。

### Phase 2B — 导演系统 (EnemyDirector + OrbitState)

#### 新建文件

| 文件路径 | 说明 |
|---------|------|
| `Assets/Scripts/Combat/Enemy/EnemyDirector.cs` | 全局单例协调器。`_maxAttackTokens` (默认 2) 限制同时攻击敌人数；`HashSet<EnemyBrain>` 追踪令牌持有者；`RequestToken()` / `ReturnToken()` / `HasToken()` API；`LateUpdate()` 自动清理死亡/禁用的 Brain；无 Director 时全体自由攻击（向后兼容）；Editor 顶角显示 token 使用量 |
| `Assets/Scripts/Combat/Enemy/States/OrbitState.cs` | 等待令牌时的行为：以 `AttackRange * OrbitRadiusMultiplier` 为半径绕玩家环行；随机 CW/CCW 方向；混合 Boids 分离力（权重 0.5）；每 0.4s 重新请求令牌（非逐帧）；获得令牌后转入 Engage/Shoot |

#### 修改文件

| 文件路径 | 变更说明 |
|---------|---------|
| `Assets/Scripts/Combat/Enemy/EnemyBrain.cs` | 新增 `OrbitState` 属性；`BuildStateMachine()` 实例化 `_orbitState`；新增 `ReturnDirectorToken()` 公共方法；`ForceStagger()` 和 `OnDisable()` 中自动归还令牌；`ResetBrain()` 归还令牌；Debug GUI 显示 `[T]` 标记 |
| `Assets/Scripts/Combat/Enemy/ShooterBrain.cs` | Debug GUI 增加令牌状态显示 |
| `Assets/Scripts/Combat/Enemy/States/ChaseState.cs` | 进入攻击范围时先 `RequestToken()` —— 通过则转 Engage/Shoot，拒绝则转 OrbitState；无 Director 时直接攻击（兼容） |

**设计要点**：
- **电影感轮流单挑**：最多 2 敌人同时攻击，其余环绕助威
- **零分配**：`HashSet` O(1) 查找，`OrbitState` 令牌检查 0.4s 节流
- **健壮性**：死亡/禁用/硬直/池回收均自动归还令牌，`LateUpdate` 兜底清理

### Phase 2C — 炮台原型 (Turret)

#### 新建文件

| 文件路径 | 说明 |
|---------|------|
| `Assets/Scripts/Combat/Enemy/EnemyLaserBeam.cs` | 敌方激光束。`Fire()` 执行 Raycast + LineRenderer 渲染 + IDamageable 伤害；`ShowAimLine()` / `HideAimLine()` 显示瞄准线（Lock 阶段视觉预警）；支持持续光束模式（LaserDuration）；淡出效果；`IPoolable` 对象池支持 |
| `Assets/Scripts/Combat/Enemy/TurretBrain.cs` | 继承 EnemyBrain，**不调用** `base.BuildStateMachine()`（无 Chase/Engage/Return/Orbit）；4 个专用状态：Scan → Lock → Attack → Cooldown；`SelectAttackForCycle()` 从 AttackDataSO[] 选择攻击；支持韧性击破/硬直 |
| `Assets/Scripts/Combat/Enemy/States/TurretScanState.cs` | 扫描状态：缓慢旋转扫视（ScanRotationSpeed°/s），检测到目标 → Lock |
| `Assets/Scripts/Combat/Enemy/States/TurretLockState.cs` | 锁定状态：快速追踪目标，显示瞄准线（EnemyLaserBeam.ShowAimLine），LockOnDuration 后 → Attack；丢失目标 → Scan |
| `Assets/Scripts/Combat/Enemy/States/TurretAttackState.cs` | 攻击状态：根据 AttackDataSO.Type 分发——Laser 型调用 `EnemyLaserBeam.Fire()`，Projectile 型从对象池发射弹丸；完成 → Cooldown |
| `Assets/Scripts/Combat/Enemy/States/TurretCooldownState.cs` | 冷却状态：AttackCooldown 倒计时，完成后根据目标可见性 → Lock 或 Scan |

#### Editor 工具扩展

| 文件路径 | 变更说明 |
|---------|---------|
| `Assets/Scripts/Combat/Editor/EnemyAssetCreator.cs` | 新增 `CreateAttackDataAssets()` 菜单项（创建 RusherMelee + ShooterBurst AttackDataSO）；新增 `CreateTurretLaserAssets()` + `CreateTurretCannonAssets()` 菜单项（创建 EnemyStatsSO + AttackDataSO + 完整 Prefab，含子物体 LineRenderer + EnemyLaserBeam）；新增 `CreateTurretGameObject()` 共享 Prefab 构建方法 |

### 完整文件清单

**新建文件 (11)**:
1. `Assets/Scripts/Combat/Enemy/AttackDataSO.cs`
2. `Assets/Scripts/Combat/Enemy/HitboxResolver.cs`
3. `Assets/Scripts/Combat/Enemy/EnemyDirector.cs`
4. `Assets/Scripts/Combat/Enemy/EnemyLaserBeam.cs`
5. `Assets/Scripts/Combat/Enemy/TurretBrain.cs`
6. `Assets/Scripts/Combat/Enemy/States/OrbitState.cs`
7. `Assets/Scripts/Combat/Enemy/States/TurretScanState.cs`
8. `Assets/Scripts/Combat/Enemy/States/TurretLockState.cs`
9. `Assets/Scripts/Combat/Enemy/States/TurretAttackState.cs`
10. `Assets/Scripts/Combat/Enemy/States/TurretCooldownState.cs`

**修改文件 (9)**:
1. `Assets/Scripts/Combat/Enemy/EnemyStatsSO.cs`
2. `Assets/Scripts/Combat/Enemy/EnemyBrain.cs`
3. `Assets/Scripts/Combat/Enemy/ShooterBrain.cs`
4. `Assets/Scripts/Combat/Enemy/States/EngageState.cs`
5. `Assets/Scripts/Combat/Enemy/States/TelegraphSubState.cs`
6. `Assets/Scripts/Combat/Enemy/States/AttackSubState.cs`
7. `Assets/Scripts/Combat/Enemy/States/RecoverySubState.cs`
8. `Assets/Scripts/Combat/Enemy/States/ShootState.cs`
9. `Assets/Scripts/Combat/Enemy/States/ChaseState.cs`
10. `Assets/Scripts/Combat/Editor/EnemyAssetCreator.cs`

**技术**：数据驱动 (AttackDataSO)、策略模式 (AttackType 分发)、NonAlloc 物理查询、HashSet O(1) 令牌管理、环形运动 (OrbitState)、Raycast + LineRenderer 激光、IPoolable 对象池。

---

---

## Phase 3 完整实现：刺客 + 恐惧 + 阵营 + 闪避格挡 + 精英词缀 + Boss — 2026-02-11 23:00

### 概述
Phase 3 实现了 6 个子系统，构建了完整的高级敌人 AI 层：
- **3A** 刺客原型 (Stalker)
- **3B** 恐惧系统 (Fear)
- **3C** 阵营系统 (Faction)
- **3D** 闪避/格挡 AI (Dodge/Block)
- **3E** 精英词缀系统 (Elite Affix)
- **3F** 多阶段 Boss 控制器

### 新建文件

#### 3A: 刺客原型
1. `Assets/Scripts/Combat/Enemy/StalkerBrain.cs` — 刺客大脑，覆盖 BuildStateMachine()，4 个专属状态 + 透明度管理
2. `Assets/Scripts/Combat/Enemy/States/StealthState.cs` — 隐身状态：低 alpha，无目标时缓慢飘回出生点
3. `Assets/Scripts/Combat/Enemy/States/FlankState.cs` — 迂回状态：利用 Dot Product 判定玩家背后弧，移向背后位置
4. `Assets/Scripts/Combat/Enemy/States/StalkerStrikeState.cs` — 突袭状态：快速显形 → 单次近战 → 短暂僵直 → 脱离
5. `Assets/Scripts/Combat/Enemy/States/DisengageState.cs` — 脱离状态：加速远离 + 渐隐回隐身

#### 3B: 恐惧系统
6. `Assets/Scripts/Combat/Enemy/EnemyFear.cs` — 恐惧组件：累积恐惧值，订阅 OnAnyEnemyDeath，超阈值触发逃跑
7. `Assets/Scripts/Combat/Enemy/States/FleeState.cs` — 逃跑状态：远离玩家，加速移动，计时/距离退出

#### 3D: 闪避/格挡
8. `Assets/Scripts/Combat/Enemy/ThreatSensor.cs` — 威胁感知：5Hz NonAlloc 扫描来袭投射物，Dot Product 判断朝向
9. `Assets/Scripts/Combat/Enemy/States/DodgeState.cs` — 闪避状态：垂直于威胁方向侧闪
10. `Assets/Scripts/Combat/Enemy/States/BlockState.cs` — 格挡状态：面向威胁举盾，减伤

#### 3E: 精英词缀
11. `Assets/Scripts/Combat/Enemy/EnemyAffixSO.cs` — 词缀 SO：统计乘数 + 行为标签 + 特殊效果枚举
12. `Assets/Scripts/Combat/Enemy/EnemyAffixController.cs` — 词缀控制器：运行时应用词缀，处理特殊效果 (爆炸/反伤/狂暴等)

#### 3F: 多阶段 Boss
13. `Assets/Scripts/Combat/Enemy/BossPhaseDataSO.cs` — Boss 阶段 SO：HP 阈值、攻击模式、统计修正、过渡效果
14. `Assets/Scripts/Combat/Enemy/BossController.cs` — Boss 控制器：监听伤害事件，按 HP 阈值触发阶段转换
15. `Assets/Scripts/Combat/Enemy/States/BossTransitionState.cs` — Boss 过渡状态：无敌 + 视觉脉冲 + HitStop

### 修改文件

1. `Assets/Scripts/Combat/Enemy/EnemyEntity.cs`
   - 新增 `OnAnyEnemyDeath` 全局静态事件，Die() 中广播
   - 新增 `MoveAtSpeed(direction, speed)` 方法（变速移动）
   - 新增 `IsBlocking`、`BlockDamageReduction` 属性，TakeDamage 中应用格挡减伤
   - 新增 `IsInvulnerable` 属性，TakeDamage 中无敌检查
   - 新增 `_runtimeMaxHP`、`_runtimeDamageMultiplier`、`_runtimeSpeedMultiplier` 运行时统计
   - 新增 `ApplyAffixMultipliers()` 方法
   - `MoveTo()` 应用 `_runtimeSpeedMultiplier`
   - `OnReturnToPool()` 重置所有新状态

2. `Assets/Scripts/Combat/Enemy/EnemyStatsSO.cs`
   - 新增 `FactionID` 字段
   - 新增恐惧系统字段：`FearThreshold`、`FearFromAllyDeath`、`FearFromPoiseBroken`、`FearDecayRate`、`FleeDuration`
   - 新增闪避/格挡字段：`DodgeSpeed`、`DodgeDuration`、`BlockDamageReduction`、`BlockDuration`、`ThreatDetectionRadius`

3. `Assets/Scripts/Combat/Enemy/EnemyBrain.cs`
   - 新增 `FleeState`、`DodgeState`、`BlockState` 状态实例和属性
   - 新增 `ForceFleeCheck()` 方法（恐惧系统调用）
   - 新增 `ForceTransition(BossPhaseDataSO)` 方法（Boss 控制器调用）
   - 新增 `CheckThreatResponse()` 在 Update 中检查威胁感知并触发闪避/格挡

4. `Assets/Scripts/Combat/Enemy/EnemyPerception.cs`
   - 完全重写：新增 `TargetType` 枚举、`CurrentTargetType`、`CurrentTargetEntity` 属性
   - `LastKnownPlayerPosition` → `LastKnownTargetPosition`（旧名保留为别名）
   - 新增 `PerformFactionScan()` 方法：NonAlloc 扫描敌方阵营实体
   - 玩家始终优先（PerformVisionCheck > PerformFactionScan）
   - 记忆衰减更新：支持阵营目标存活刷新

5. `Assets/Scripts/Combat/Enemy/States/ChaseState.cs` — `LastKnownPlayerPosition` → `LastKnownTargetPosition`
6. `Assets/Scripts/Combat/Enemy/States/OrbitState.cs` — 同上
7. `Assets/Scripts/Combat/Enemy/States/ShootState.cs` — 同上
8. `Assets/Scripts/Combat/Enemy/States/TurretLockState.cs` — 同上
9. `Assets/Scripts/Combat/Enemy/States/RetreatState.cs` — 同上
10. `Assets/Scripts/Combat/Enemy/States/FlankState.cs` — 同上
11. `Assets/Scripts/Combat/Enemy/States/FleeState.cs` — 同上
12. `Assets/Scripts/Combat/Enemy/States/DisengageState.cs` — 同上

13. `Assets/Scripts/Combat/Enemy/EnemySpawner.cs`
    - 新增精英词缀生成：`_possibleAffixes`、`_eliteChance`、`_maxAffixCount` 字段
    - 新增 `TryApplyAffixes()` 方法

14. `Assets/Scripts/Combat/Editor/EnemyAssetCreator.cs`
    - 新增 `CreateStalkerAssets()` 菜单（SO + AttackData + Prefab）
    - 新增 `CreateAffixAssets()` 菜单（5 个精英词缀）

### 目的
完成 Phase 3 全部高级敌人 AI 系统，使战斗系统具备：
- 多样化敌人行为（隐身刺客、恐惧逃跑、阵营内斗）
- 动态战斗反应（闪避来袭投射物、举盾格挡）
- 可扩展的精英变体系统（词缀运行时堆叠）
- Boss 多阶段战斗机制（HP 阈值触发、攻击模式切换、无敌过渡）

**技术**：HFSM 子类化 (StalkerBrain)、事件驱动恐惧传播 (静态事件 + 距离判定)、NonAlloc 阵营/威胁扫描、运行时统计覆写 (AffixController)、Dot Product 几何判定 (后方弧/威胁朝向)、策略模式词缀效果、数据驱动 Boss 阶段 (BossPhaseDataSO)。

---

---

## CLAUDE.md 架构大修同步更新 — 2026-02-12 17:33

### 修改文件

1. `CLAUDE.md`

### 内容

将架构基建大修引入的所有新系统、新模式、新依赖同步更新到 CLAUDE.md，确保后续 AI 对话使用正确的技术栈和架构模式。同时强化 ImplementationLog 写入规则。

具体变更：
- **技术栈**：新增 UniTask、PrimeTween
- **核心模块**：新增基建 (Infrastructure) 条目（ServiceLocator、DamagePayload、SaveManager、AudioManager、CombatEvents、TransitionRuleEvaluator）
- **架构原则 §2 解耦与模块化**：新增跨程序集事件总线规则、依赖反转 (IStarChartItemResolver) 示例
- **架构原则 §7 服务定位**：新增 ServiceLocator 使用规范，禁止 FindAnyObjectByType
- **代码规范 > 异步纪律**：替换旧"协程纪律"为 UniTask/PrimeTween/CTS 规范
- **代码规范 > 命名**：新增 `ProjectArk.Core.Audio`、`ProjectArk.Core.Save`、`ProjectArk.Enemy` 命名空间
- **项目结构**：新增 `Core/Audio/`、`Core/Save/`、`Core/Tests/`、`Combat/Tests/` 目录
- **当前里程碑**：新增"架构基建大修"已完成行
- **实现日志**：升级为"严格执行"规则，新增执行时机说明（单阶段/多阶段/Bug 修复），在 Feature 开发工作流第 7 步添加强调标注

### 目的
CLAUDE.md 是 AI 对话的"ground truth"。架构大修后未同步更新会导致后续对话使用过时模式（如 Coroutine 而非 UniTask、FindAnyObjectByType 而非 ServiceLocator）。同时强化日志写入规则防止再次遗漏。

**技术**：文档维护。

---

---

## UICanvasBuilder 幂等重构 + DoorTransitionController 集成 — 2026-02-12 20:30

### 修改文件

| 文件 | 变更 | 目的 |
|------|------|------|
| `Assets/Scripts/UI/ProjectArk.UI.asmdef` | 添加 `ProjectArk.Level` 引用 | 使 UI Editor 脚本能引用 Level 模块类型 (DoorTransitionController) |
| `Assets/Scripts/UI/Editor/UICanvasBuilder.cs` | 重构为幂等 + 新增 DoorTransitionController 段 | 见下方详细说明 |

---

### UICanvasBuilder 改动详情

**1. 幂等创建（防重复）**
- `FindOrCreateCanvas()`: 先通过 UIManager 查找已有 Canvas，再通过 sortingOrder=10 匹配，均未找到才创建新 Canvas
- 每个 Section (HeatBarHUD / HealthBarHUD / StarChartPanel / UIManager / DoorTransitionController) 均先用 `GetComponentInChildren<T>(true)` 检查是否已存在
- 已存在的 Section 打印 skip 日志并跳过，不会重复创建

**2. 代码结构重构**
- 将每个 Section 的创建逻辑提取为独立私有方法（`BuildHeatBarSection` / `BuildHealthBarSection` / `BuildStarChartSection` / `BuildUIManagerSection` / `BuildDoorTransitionSection`）
- 主 `BuildUICanvas()` 方法简化为约 50 行的编排器，可读性大幅提升

**3. 新增 DoorTransitionController 段 (Step 6)**
- 创建 "FadeOverlay" 作为 Canvas 最后一个子物体（`SetAsLastSibling` 确保渲染在最顶层）
- 添加全屏 Image（黑色 alpha=0，默认不阻挡 raycast）
- 添加 DoorTransitionController 组件并自动连线 `_fadeImage`
- 用户不再需要手动创建 FadeOverlay 和 DoorTransitionController

### 技术

- 利用 `GetComponentInChildren<T>(true)` 的 includeInactive 参数确保即使 StarChartPanel 被隐藏也能被找到
- `FindObjectsByType<Canvas>(FindObjectsInactive.Include, ...)` 搜索包含未激活的 Canvas
- Section Builder 方法返回创建的组件引用，供后续 Section 连线使用（如 UIManager 需要 HeatBarHUD 等引用）

---

---

## Level Module Phase 1 — 基础房间系统 (L1-L5) — 2026-02-12 23:30

### 新建文件

| 文件 | 说明 |
|------|------|
| `Assets/Scripts/Core/LevelEvents.cs` | Core 层静态关卡事件总线（与 CombatEvents 平行）。事件：OnRoomEntered/OnRoomExited/OnRoomCleared/OnBossDefeated/OnCheckpointActivated/OnWorldStageChanged，每个事件配对 RaiseXxx() 方法 |
| `Assets/Scripts/Level/ProjectArk.Level.asmdef` | 新程序集定义。引用：Core, Combat, Ship, Enemy, Heat, Core.Audio, UniTask, PrimeTween, Unity.Cinemachine |
| `Assets/Scripts/Level/Data/RoomType.cs` | 房间类型枚举：Normal / Arena / Boss / Safe |
| `Assets/Scripts/Level/Data/RoomSO.cs` | 轻量房间元数据 SO（只存非空间信息：RoomID / DisplayName / FloorLevel / MapIcon / RoomType / EncounterSO 引用）。空间数据由场景 Room MonoBehaviour 管理 |
| `Assets/Scripts/Level/Data/EncounterSO.cs` | 战斗遭遇波次数据 SO：EnemyWave[]（每波含 EnemySpawnEntry[] + DelayBeforeWave），EnemySpawnEntry 含 EnemyPrefab + Count |
| `Assets/Scripts/Level/Room/RoomState.cs` | 房间运行时状态枚举：Undiscovered / Entered / Cleared / Locked |
| `Assets/Scripts/Level/Room/Room.cs` | 房间运行时 MonoBehaviour。引用 RoomSO 元数据；持有 BoxCollider2D Trigger（玩家检测）、Collider2D confinerBounds（摄像机约束）、Transform[] spawnPoints、Door[] doors。OnTriggerEnter2D/Exit2D 检测玩家进出并触发事件。提供 ActivateEnemies/DeactivateEnemies、LockAllDoors/UnlockCombatDoors 辅助方法 |
| `Assets/Scripts/Level/Room/DoorState.cs` | 门状态枚举：Open / Locked_Combat / Locked_Key / Locked_Ability / Locked_Schedule |
| `Assets/Scripts/Level/Room/Door.cs` | 门组件。持有 TargetRoom + TargetSpawnPoint 双向引用，DoorState 状态机。OnTriggerEnter2D 检测玩家并在 Open 状态下通过 DoorTransitionController 触发过渡 |
| `Assets/Scripts/Level/Room/DoorTransitionController.cs` | 门过渡控制器（ServiceLocator 注册）。使用 async UniTaskVoid + PrimeTween 实现淡黑→传送→淡入过渡。支持普通门（0.3s）和层间过渡（0.5s）两种时长。过渡期间禁用玩家输入，绑定 CancellationTokenSource + destroyCancellationToken |
| `Assets/Scripts/Level/Camera/RoomCameraConfiner.cs` | 摄像机房间约束桥接。订阅 RoomManager.OnCurrentRoomChanged，更新 CinemachineConfiner2D.BoundingShape2D 并调用 InvalidateBoundingShapeCache() |
| `Assets/_Data/Level/Rooms/` | RoomSO 资产存放目录（空） |
| `Assets/_Data/Level/Encounters/` | EncounterSO 资产存放目录（空） |

### 修改文件

| 文件 | 变更 |
|------|------|
| `Assets/Scripts/Combat/Enemy/EnemyDirector.cs` | 新增 `ReturnAllTokens()` 公共方法，清空所有攻击令牌。供 RoomManager 在房间切换时调用，防止跨房间令牌泄漏 |
| `Assets/Scripts/Ship/Input/InputHandler.cs` | 新增 `OnInteractPerformed` 事件 + `_interactAction` 字段。在 Awake 中查找 Ship/Interact action，在 OnEnable/OnDisable 中订阅/取消订阅 performed 回调。为门交互和未来检查点/NPC 交互提供输入支持 |

### 目的

搭建关卡模块 Phase 1 基础结构（L1-L5），从零建立房间系统骨架：
- **L1**：LevelEvents 事件总线 + ProjectArk.Level 程序集 + RoomSO/EncounterSO 数据层
- **L2**：Room 运行时组件（Trigger 检测 + 状态管理 + 敌人激活/休眠）
- **L3**：RoomManager 管理器（ServiceLocator 注册、房间追踪、事件广播、Director 令牌清理、Arena/Boss 自动锁门）
- **L4**：Door 组件 + DoorTransitionController（UniTask + PrimeTween 异步淡黑过渡）+ InputHandler Interact 暴露
- **L5**：RoomCameraConfiner（Cinemachine 3.x CinemachineConfiner2D 集成，房间切换时自动更新摄像机约束边界）

### 技术

- 命名空间：`ProjectArk.Level`（新程序集）、`ProjectArk.Core`（LevelEvents）
- 模式：静态事件总线（LevelEvents）、ServiceLocator 注册（RoomManager/DoorTransitionController）、UniTask + PrimeTween 异步过渡、CancellationTokenSource 生命周期管理
- 数据驱动：RoomSO（轻量元数据）+ EncounterSO（波次配置），场景即空间数据
- Cinemachine 3.x：CinemachineConfiner2D.BoundingShape2D + InvalidateBoundingShapeCache()

### 用户手动步骤

完成代码后需要在 Unity 编辑器中执行：
1. Physics2D 碰撞矩阵：为 Room Trigger 配置新 Layer（仅与 Player 碰撞）
2. 场景搭建：创建 CinemachineCamera + CinemachineConfiner2D、RoomManager GameObject、DoorTransitionController + Canvas/FadeImage、3 个测试 Room（Tilemap + BoxCollider2D + PolygonCollider2D + Door）
3. 创建测试 RoomSO 资产并拖入 Room 组件

---

---

## 关卡模块 Phase 2 — 进度系统全量实现 — 2026-02-13 09:04

### 概述

实现 Level Module Phase 2 的全部 5 个子系统：检查点 (L6)、物品拾取 (L8)、锁钥 (L7)、死亡重生 (L9)、世界进度 (L9.5)。同时修复 Phase 1 git 回退后遗失的 InputHandler ServiceLocator 注册，以及为 ShipHealth 添加 Heal() 方法。

### 前置修复

| 文件 | 变更 | 目的 |
|------|------|------|
| `Assets/Scripts/Ship/Input/InputHandler.cs` | 添加 `using ProjectArk.Core;`，在 `Awake()` 中 `ServiceLocator.Register(this)`，新增 `OnDestroy()` 取消注册 | Phase 1 验证时已添加但被 git reset 回退，DoorTransitionController 和所有 Phase 2 系统均依赖此注册 |
| `Assets/Scripts/Ship/Combat/ShipHealth.cs` | 新增 `Heal(float amount)` 公共方法 | HealthPickup (L8) 需要按固定量恢复 HP |

### L6: CheckpointSystem（检查点系统）

| 文件 | 类型 | 目的 |
|------|------|------|
| `Assets/Scripts/Level/Data/CheckpointSO.cs` | **新建** | 检查点配置 SO：CheckpointID、DisplayName、RestoreHP/RestoreHeat 开关、ActivationSFX |
| `Assets/Scripts/Level/Checkpoint/Checkpoint.cs` | **新建** | 场景检查点组件：Trigger 检测 + Interact 激活，恢复 HP/热量，通知 CheckpointManager，Sprite 颜色视觉反馈 |
| `Assets/Scripts/Level/Checkpoint/CheckpointManager.cs` | **新建** | 检查点管理器：ServiceLocator 注册，追踪活跃检查点/重生位置，广播 LevelEvents.RaiseCheckpointActivated，自动存档 |

**设计要点**：
- Checkpoint 在 OnEnable/OnDisable 订阅 InputHandler.OnInteractPerformed，玩家进入范围后按 Interact 激活
- CheckpointManager.GetCheckpointRoom() 通过 GetComponentInParent<Room>() 找到检查点所在房间，供 GameFlowManager 死亡重生使用
- 激活检查点时自动调用 SaveManager.Save() 持久化

### L8: ItemPickup（物品拾取系统）

| 文件 | 类型 | 目的 |
|------|------|------|
| `Assets/Scripts/Level/Pickup/PickupBase.cs` | **新建** | 抽象拾取基类：auto vs interact 拾取模式，PrimeTween bob 浮动动画，拾取后缩小消失动画，UniTask 异步 |
| `Assets/Scripts/Level/Pickup/KeyPickup.cs` | **新建** | 钥匙拾取：OnPickedUp → KeyInventory.AddKey() |
| `Assets/Scripts/Level/Pickup/HealthPickup.cs` | **新建** | 血量回复拾取：OnPickedUp → ShipHealth.Heal() |
| `Assets/Scripts/Level/Pickup/HeatPickup.cs` | **新建** | 热量清空拾取：OnPickedUp → HeatSystem.ResetHeat() |

**设计要点**：
- PickupBase 用 PrimeTween.LocalPositionY 做无限循环 bob 动画，拾取后 Tween.Scale 缩到 0 再 SetActive(false)
- _autoPickup=true 时碰到自动拾取，false 时需要外部调用 TryInteractPickup()
- 所有 Pickup 都兼容对象池（OnEnable 重置 consumed/scale/position）

### L7: LockKeySystem（锁钥系统）

| 文件 | 类型 | 目的 |
|------|------|------|
| `Assets/Scripts/Level/Data/KeyItemSO.cs` | **新建** | 钥匙 SO：KeyID、DisplayName、Icon、Description |
| `Assets/Scripts/Level/Progression/KeyInventory.cs` | **新建** | 玩家钥匙背包：HashSet<string> 存储，ServiceLocator 注册，序列化到 ProgressSaveData.Flags（key_ 前缀） |
| `Assets/Scripts/Level/Progression/Lock.cs` | **新建** | 锁组件：Trigger + Interact 检测，检查 KeyInventory.HasKey()，成功 → Door.SetState(Open) + 音效；失败 → 提示音 |
| `Assets/Scripts/Level/Room/Door.cs` | **修改** | 新增 `[SerializeField] string _requiredKeyID` 字段和 `RequiredKeyID` 属性，供 Lock/UI 查询 |

**设计要点**：
- KeyInventory 用 ProgressSaveData.Flags 持久化（key 格式: `key_{keyID}`），与通用 flag 系统共存
- Lock 组件可选消耗钥匙（_consumeKey），解锁后自动 disabled
- Door 新增 RequiredKeyID 为可选元数据，Lock 组件自身持有完整解锁逻辑

### L9: Death & Respawn（死亡与重生）

| 文件 | 类型 | 目的 |
|------|------|------|
| `Assets/Scripts/Level/GameFlow/GameFlowManager.cs` | **新建** | 游戏流程管理器：订阅 ShipHealth.OnDeath，异步编排死亡→黑屏→传送→重生序列 |
| `Assets/Scripts/Level/Room/Room.cs` | **修改** | 新增 `ResetEnemies()` 方法：关闭所有敌人 → 重置房间状态 → 解锁战斗门 → 重新激活 |

**死亡→重生序列（async UniTaskVoid）**：
1. 禁用输入
2. 死亡音效
3. PrimeTween 淡黑 (0.5s)
4. 黑屏停留 (1s)
5. 获取重生位置 (CheckpointManager.GetRespawnPosition)
6. 传送玩家 + 清零速度
7. 切换到检查点房间 (RoomManager.EnterRoom)
8. 重置 HP + 热量
9. 重置当前房间敌人 (Room.ResetEnemies)
10. 重生音效
11. 淡入 (0.5s)
12. 恢复输入
13. 存档

**设计要点**：
- 复用与 DoorTransitionController 相同的 fade Image（可共享或独立分配）
- CancellationTokenSource 绑定 destroyCancellationToken 防止对象销毁后继续执行
- Room.ResetEnemies() 会将 Cleared 状态回退到 Entered，允许重新触发战斗锁门

### L9.5: WorldProgressManager（世界进度管理器）

| 文件 | 类型 | 目的 |
|------|------|------|
| `Assets/Scripts/Level/Data/WorldProgressStageSO.cs` | **新建** | 世界进度阶段 SO：StageIndex、StageName、RequiredBossIDs、UnlockDoorIDs |
| `Assets/Scripts/Level/Progression/WorldProgressManager.cs` | **新建** | 世界进度管理器：订阅 LevelEvents.OnBossDefeated，数据驱动阶段推进，广播 OnWorldStageChanged |
| `Assets/Scripts/Core/Save/SaveData.cs` | **修改** | ProgressSaveData 新增 `int WorldStage` 字段 |

**设计要点**：
- WorldProgressStageSO 定义阶段推进条件（RequiredBossIDs 全部满足才升级）
- 阶段推进是单向的（irreversible），按顺序检查（遇到未满足条件即停止）
- UnlockDoorIDs 目前仅日志输出，完整实现需要 Door 注册表（留作后续扩展）

### 新增文件夹结构

```
Assets/Scripts/Level/
├── Checkpoint/
│   ├── Checkpoint.cs
│   └── CheckpointManager.cs
├── Pickup/
│   ├── PickupBase.cs
│   ├── KeyPickup.cs
│   ├── HealthPickup.cs
│   └── HeatPickup.cs
├── Progression/
│   ├── KeyInventory.cs
│   ├── Lock.cs
│   └── WorldProgressManager.cs
├── GameFlow/
│   └── GameFlowManager.cs
├── Data/
│   ├── CheckpointSO.cs    (new)
│   ├── KeyItemSO.cs        (new)
│   └── WorldProgressStageSO.cs (new)
└── ... (existing Phase 1 files)
```

### 技术

- **UniTask**：GameFlowManager 的死亡重生序列使用 async UniTaskVoid + CancellationTokenSource，零 GC
- **PrimeTween**：PickupBase bob 动画（无限循环 Yoyo），拾取消失动画（Scale InBack），GameFlowManager 淡入淡出
- **ServiceLocator**：CheckpointManager、KeyInventory、GameFlowManager、WorldProgressManager 全部注册，InputHandler 补注册
- **LevelEvents 静态事件总线**：OnCheckpointActivated（L6）、OnBossDefeated（L9.5 订阅）、OnWorldStageChanged（L9.5 广播）
- **SaveManager 集成**：检查点激活自动存档，世界进度通过 ProgressSaveData.WorldStage 和 DefeatedBossIDs 持久化，钥匙通过 Flags 持久化

### 编辑器配置清单（需用户在 Unity Editor 中完成）

1. **Physics2D 碰撞矩阵**：确保 Checkpoint/Pickup/Lock 的 Trigger 层与 Player 层碰撞
2. **场景 GameObject**：创建 CheckpointManager、KeyInventory、GameFlowManager、WorldProgressManager 空 GameObject 并挂载对应脚本
3. **GameFlowManager**：拖入 FadeImage 引用（可与 DoorTransitionController 共享）
4. **创建 SO 资产**：在 `Assets/_Data/Level/Checkpoints/` 和 `Assets/_Data/Level/Keys/` 下创建 CheckpointSO 和 KeyItemSO 资产
5. **Checkpoint Prefab**：Collider2D (Trigger) + SpriteRenderer + Checkpoint 组件，配置 PlayerLayer 和 CheckpointSO 引用
6. **Pickup Prefab**：Collider2D (Trigger) + SpriteRenderer + 对应 Pickup 子类组件
7. **Lock Prefab / 配置**：在需要锁定的门附近放置 Lock 组件，引用 KeyItemSO 和 Door

---

---

## LevelAssetCreator 一键创建 Phase 2 SO 资产 — 2026-02-13 09:22

### 新建文件

| 文件 | 目的 |
|------|------|
| `Assets/Scripts/Level/Editor/ProjectArk.Level.Editor.asmdef` | Level 模块 Editor 脚本程序集定义（仅 Editor 平台），引用 ProjectArk.Level + ProjectArk.Core |
| `Assets/Scripts/Level/Editor/LevelAssetCreator.cs` | 一键创建 Phase 2 全部 SO 资产的编辑器工具 |

### 菜单项

| 菜单路径 | 功能 |
|----------|------|
| `ProjectArk > Level > Create Phase 2 Assets (All)` | 一键创建所有 Checkpoint + Key + WorldStage 资产 |
| `ProjectArk > Level > Create Checkpoint Assets` | 仅创建 CheckpointSO 资产 |
| `ProjectArk > Level > Create Key Item Assets` | 仅创建 KeyItemSO 资产 |
| `ProjectArk > Level > Create World Progress Stage Assets` | 仅创建 WorldProgressStageSO 资产 |

### 创建的资产

| 资产路径 | 内容 |
|---------|------|
| `Assets/_Data/Level/Checkpoints/Checkpoint_Start.asset` | 起始锚点，恢复 HP+热量 |
| `Assets/_Data/Level/Checkpoints/Checkpoint_Corridor.asset` | 走廊锚点，恢复 HP+热量 |
| `Assets/_Data/Level/Checkpoints/Checkpoint_Combat.asset` | 战斗区锚点，仅恢复 HP（不恢复热量） |
| `Assets/_Data/Level/Keys/Key_AccessAlpha.asset` | Alpha 通行证（测试用钥匙） |
| `Assets/_Data/Level/Keys/Key_BossGate.asset` | 核心门钥（Boss 区域钥匙） |
| `Assets/_Data/Level/WorldStages/Stage_0_Initial.asset` | 初始阶段（无条件） |
| `Assets/_Data/Level/WorldStages/Stage_1_PostGuardian.asset` | Guardian Boss 击败后解锁 |

### 技术

- 遵循 EnemyAssetCreator 的幂等模式：已存在的资产跳过，不会重复创建
- 使用 SerializedObject + FindProperty 写入私有 `[SerializeField]` 字段
- 所有音效/图标引用留空，等美术资源就绪后手动分配

---

---

## Door 转场卡门修复 + OnTriggerStay2D — 2026-02-13 10:00

### 修改文件

| 文件 | 变更 | 目的 |
|------|------|------|
| `Assets/Scripts/Level/Room/Door.cs` | 新增 `OnTriggerStay2D` 和 `TryAutoTransition()` 方法 | 修复传送后"卡门"问题 |

### 问题

玩家从 Door_ToCoridor 传送到 Room_Corridor 后，SpawnPoint 恰好在 Door_ToStart 的 Trigger 范围内。`OnTriggerEnter2D` 在转场尚未结束时触发，被 `DoorTransitionController.IsTransitioning` 拒绝。转场结束后玩家一直在 Trigger 内，`OnTriggerEnter2D` 不会再次触发，导致门永远无法通过。

### 修复

- 新增 `OnTriggerStay2D`：每个 FixedUpdate 帧检测，如果玩家仍在范围内且转场已结束，重新尝试过渡
- 提取 `TryAutoTransition()` 统一入口：检查 Door 自身状态 + `DoorTransitionController.IsTransitioning` 全局锁
- 性能影响可忽略：仅在玩家处于 Door Trigger 内时触发，方法体全是 O(1) 检查

---

---

## Door 钥匙自动检查 — 2026-02-13 10:30

### 修改文件

| 文件 | 变更 | 目的 |
|------|------|------|
| `Assets/Scripts/Level/Room/Door.cs` | `TryAutoTransition()` 中集成 `Locked_Key` 自动钥匙验证逻辑 | 玩家碰到锁定门时自动检查背包，有钥匙即解锁并传送，无钥匙 Console 提示 |

### 行为变更

- **无钥匙碰门**：Console 输出 `需要钥匙 'xxx' 才能通过！`（单次提示，离开后重进才会再提示，避免 Stay 每帧刷屏）
- **有钥匙碰门**：Console 输出 `持有钥匙 'xxx'，门自动解锁！` → `SetState(Open)` → 立刻触发传送
- **Open 状态门**：碰到直接传送（不变）
- 新增 `_hasLoggedMissingKey` 标记：`OnTriggerExit2D` 时重置

### 设计决策

将钥匙验证逻辑从 Lock 组件迁入 Door 本身。原因：
1. 玩家期望碰到门自动处理，不需要额外按 Interact
2. Door 已持有 `_requiredKeyID` 字段，天然拥有所需信息
3. 减少配置复杂度（不需要额外挂 Lock 组件 + 配置 Collider）
4. Lock 组件仍保留用于更复杂的场景（如远离门的独立开关）

### 技术

- `ServiceLocator.Get<KeyInventory>()` 查询玩家钥匙背包
- `_hasLoggedMissingKey` 防止 `OnTriggerStay2D` 每帧输出日志

---

---

## Level Module Phase 3 — 战斗房间逻辑 — 2026-02-13 11:15

### L10: EncounterSystem（波次生成系统）

#### 新建文件

| 文件 | 内容 | 目的 |
|------|------|------|
| `Assets/Scripts/Combat/Enemy/ISpawnStrategy.cs` | 策略接口，定义 `Initialize/Start/OnEnemyDied/IsEncounterComplete/Reset` | 将生成行为抽象为可插拔策略，解耦 EnemySpawner 与具体逻辑 |
| `Assets/Scripts/Combat/Enemy/LoopSpawnStrategy.cs` | 循环刷怪策略（封装原有 EnemySpawner 逻辑） | 向后兼容：维持固定存活数，死亡后延迟重生，`IsEncounterComplete` 永远为 false |
| `Assets/Scripts/Level/Room/WaveSpawnStrategy.cs` | EncounterSO 驱动的波次生成策略 | 逐波生成敌人，当前波全灭后延迟 `DelayBeforeWave` 秒启动下一波，全部波次完成触发 `OnEncounterComplete` 事件 |

#### 修改文件

| 文件 | 变更 | 目的 |
|------|------|------|
| `Assets/Scripts/Combat/Enemy/EnemySpawner.cs` | 完全重构为策略模式上下文 | 保留通用基建（生成点轮询、精英词缀、对象池），新增 `SetStrategy()`/`StartStrategy()`/`SpawnFromPool(prefab)`/`ResetSpawner()` 公共 API，新增 `OnEncounterComplete` 事件 |
| `Assets/Scripts/Level/Room/Room.cs` | 重写 `ActivateEnemies()` 集成 WaveSpawnStrategy | 有 EncounterSO + EnemySpawner 且未清除 → 创建并启动 WaveSpawnStrategy；否则回退到激活预放置子对象。新增 `HandleEncounterComplete()` → 调用 `RoomManager.NotifyRoomCleared()`。`ResetEnemies()` 增加 spawner 和策略重置逻辑 |

#### 架构决策

- `WaveSpawnStrategy` 放在 `ProjectArk.Level` 程序集（依赖 `EncounterSO`），`ISpawnStrategy`/`LoopSpawnStrategy` 放在 `ProjectArk.Combat`（与 `EnemySpawner` 同级）。避免 Level→Combat 循环引用。
- `EnemySpawner` 只认识 `ISpawnStrategy` 接口，不依赖具体策略实现类。
- 多 Prefab 的池管理通过 `PoolManager.Instance.GetPool(prefab)` 按需创建，单 Prefab legacy 模式复用 `_legacyPool`。

#### 技术

- Strategy 模式（ISpawnStrategy → LoopSpawnStrategy / WaveSpawnStrategy）
- UniTask 异步波次间延迟 + CancellationTokenSource 生命周期管理
- PoolManager.GetPool() 按 Prefab InstanceID 索引，支持多种敌人 Prefab 共存

---

### L11: ArenaController（竞技场编排器）

#### 新建文件

| 文件 | 内容 | 目的 |
|------|------|------|
| `Assets/Scripts/Level/Room/ArenaController.cs` | Arena/Boss 房间战斗编排器 | 完整的遭遇序列：锁门→警报音效→预延迟→启动 WaveSpawnStrategy→全清→后延迟→解锁门→胜利音效→可选奖励掉落→标记 Cleared |

#### 修改文件

| 文件 | 变更 | 目的 |
|------|------|------|
| `Assets/Scripts/Level/Room/RoomManager.cs` | `EnterRoom()` 中 Arena/Boss 分支改为检测 ArenaController | 有 ArenaController 则委托其编排（包含锁门），无则回退到仅锁门（向后兼容） |

#### 设计

- `ArenaController` 挂在 Arena/Boss 类型的 Room 上，`[RequireComponent(typeof(Room))]`
- 全异步流程用 `async UniTaskVoid`，绑定 `destroyCancellationToken`
- Cleared 房间再次进入不重复触发（`BeginEncounter()` 检查 `RoomState.Cleared`）
- 音效通过 `ServiceLocator.Get<AudioManager>().PlaySFX2D()` 播放
- 奖励暂用 `Instantiate`（非战斗循环内，允许），后续可改对象池

#### 技术

- UniTask 异步遭遇序列
- 事件驱动：WaveSpawnStrategy.OnEncounterComplete → ArenaController.HandleWavesCleared
- ServiceLocator 获取 AudioManager、RoomManager

---

### L12: Hazard System（环境机关系统）

#### 新建文件

| 文件 | 内容 | 目的 |
|------|------|------|
| `Assets/Scripts/Level/Hazard/EnvironmentHazard.cs` | 抽象基类 | 统一伤害/击退/目标层配置，通过 `IDamageable.TakeDamage(DamagePayload)` 走统一伤害管线 |
| `Assets/Scripts/Level/Hazard/DamageZone.cs` | 持续伤害区域（酸液池/辐射区） | `OnTriggerStay2D` + `_tickInterval` 定时器，`Dictionary<int, float>` 追踪每目标下次伤害时间 |
| `Assets/Scripts/Level/Hazard/ContactHazard.cs` | 接触伤害（激光栅栏/电弧） | `OnTriggerEnter2D` 首次接触立即伤害 + `_hitCooldown` 冷却，`OnTriggerStay2D` 处理冷却期内进入的目标 |
| `Assets/Scripts/Level/Hazard/TimedHazard.cs` | 周期性开关机关（钻头陷阱/间歇激光） | `Update()` 中按 `_activeDuration`/`_inactiveDuration` 循环切换 Collider 启用状态，激活时按 ContactHazard 逻辑伤害，支持 `_startDelay` 和 `SpriteRenderer` alpha 视觉同步 |

#### 架构决策

- 继承层次：`EnvironmentHazard`（abstract）→ `DamageZone` / `ContactHazard` / `TimedHazard`
- 所有 Hazard 均在 `ProjectArk.Level` 程序集，引用 `ProjectArk.Core`（DamagePayload/IDamageable/DamageType）
- `EnvironmentHazard` 的 `Awake()` 自动修复非 trigger 的 Collider2D
- 每种 Hazard 的冷却/伤害追踪使用 `Dictionary<int, float>`（InstanceID → expiry time），`OnTriggerExit2D` / `OnDisable` 清理

#### 技术

- Template Method 模式（基类 `EnvironmentHazard` 提供 `ApplyDamage()`/`IsValidTarget()`，子类实现触发逻辑）
- `DamagePayload` 结构体走统一伤害管线
- `LayerMask _targetLayer` 显式声明目标层（遵循项目规范，禁止 `~0`）
- `TimedHazard` 的 `Update()` 使用模运算实现周期循环，避免额外计时器

---

---

## Ship Feel Enhancement — 飞船手感增强系统 — 2026-02-13 15:27

### 概述

全面升级飞船操控手感，涵盖曲线驱动移动、Dash/闪避系统、受击反馈（HitStop + 屏幕震动 + 无敌帧）、以及视觉 Juice（倾斜、Squash/Stretch、引擎粒子、Dash 残影）。所有参数通过 ScriptableObject 暴露，支持 Play Mode 热调参。

### 新建文件

| 路径 | 用途 |
|------|------|
| `Assets/Scripts/Ship/Data/ShipJuiceSettingsSO.cs` | 视觉 Juice 参数 SO（倾斜角度、Squash/Stretch 强度、残影数量、引擎粒子阈值） |
| `Assets/Scripts/Ship/Movement/ShipDash.cs` | Dash/闪避核心逻辑（async UniTaskVoid、输入缓冲、冷却、无敌帧、出口动量保留） |
| `Assets/Scripts/Ship/Input/InputBuffer.cs` | 通用输入缓冲工具类（Record/Consume/Peek/Clear） |
| `Assets/Scripts/Ship/Combat/HitFeedbackService.cs` | 静态服务：HitStop（Time.timeScale 冻结）+ ScreenShake（Cinemachine Impulse） |
| `Assets/Scripts/Ship/VFX/ShipVisualJuice.cs` | 移动倾斜 + Squash/Stretch（操控视觉子物体，PrimeTween 补间） |
| `Assets/Scripts/Ship/VFX/ShipEngineVFX.cs` | 引擎尾焰粒子控制（发射率/尺寸随速度缩放，Dash 爆发） |
| `Assets/Scripts/Ship/VFX/DashAfterImage.cs` | 残影单体组件（池化 Prefab，PrimeTween Alpha 淡出，IPoolable） |
| `Assets/Scripts/Ship/VFX/DashAfterImageSpawner.cs` | 残影生成器（监听 ShipDash 事件，等间距从对象池生成残影） |

### 修改文件

| 路径 | 变更内容 |
|------|----------|
| `Assets/Scripts/Ship/Data/ShipStatsSO.cs` | 新增 Movement Curves（AccelerationCurve、DecelerationCurve、SharpTurn、InitialBoost）、Dash 参数（Speed/Duration/Cooldown/Buffer/ExitRatio/IFrames）、HitFeedback 参数（HitStop/IFrame/ScreenShake） |
| `Assets/Scripts/Ship/Movement/ShipMotor.cs` | 重写为曲线驱动移动：AnimationCurve.Evaluate 加减速、急转弯惩罚、首帧 Boost、IsDashing 属性、SetVelocityOverride/ClearVelocityOverride API |
| `Assets/Scripts/Ship/Input/InputHandler.cs` | 新增 `_dashAction` + `OnDashPressed` 事件 + OnEnable/OnDisable 回调绑定 |
| `Assets/Scripts/Ship/Combat/ShipHealth.cs` | 新增 `IsInvulnerable` + `SetInvulnerable()` + `IFrameBlinkAsync()` 闪烁无敌帧 + TakeDamage 集成 HitStop/ScreenShake |
| `Assets/Scripts/Ship/ProjectArk.Ship.asmdef` | 新增 `Unity.Cinemachine` + `PrimeTween.Runtime` 程序集引用 |

### 技术要点

- **曲线驱动移动**：`AnimationCurve.Evaluate(progress)` 替代线性 MoveTowards，progress 基于当前速度/最大速度比值
- **急转弯惩罚**：`Vector2.Angle()` 检测输入方向 vs 当前速度方向，超过阈值（默认 90°）时施加速度乘数惩罚
- **Dash 异步流程**：`async UniTaskVoid` + `destroyCancellationToken` 生命周期管理，无 Coroutine
- **输入缓冲**：通用 `InputBuffer` 类，基于 `Time.unscaledTime` 时间戳 + 窗口消费机制
- **HitStop**：`Time.timeScale = 0` + `UniTask.Delay(ignoreTimeScale: true)` 实现帧冻结
- **ScreenShake**：`CinemachineImpulseSource.GenerateImpulse(intensity)` 需外部注册
- **无敌帧闪烁**：SpriteRenderer alpha 交替 1.0/0.3，CancellationTokenSource 管理可取消
- **残影系统**：PoolManager 对象池 + PrimeTween.Tween.Alpha 淡出 + IPoolable 回收清理
- **视觉子物体分离**：ShipVisualJuice 操控 child Transform，不干扰 physics/aiming
- **向后兼容**：保留 `ApplyImpulse()` 接口和 `OnSpeedChanged` 事件

### Play Mode 验证清单

- [ ] 加速曲线：起步有推力爆发感
- [ ] 减速曲线：松手后有滑行惯性
- [ ] 急转弯：90°+ 转向时明显减速
- [ ] Dash：按下后快速冲刺，冷却 0.3s 可再次使用
- [ ] Dash 无敌：冲刺期间不受伤
- [ ] Dash 动量保留：冲刺结束后有惯性延续
- [ ] Dash 输入缓冲：冷却结束前按 Dash 可自动执行
- [ ] HitStop：受伤瞬间有短暂顿帧
- [ ] 屏幕震动：受伤时摄像机抖动
- [ ] 无敌帧：受伤后 1s 内不再受伤，精灵闪烁
- [ ] 移动倾斜：横移时飞船视觉倾斜
- [ ] 引擎粒子：移动时有尾焰
- [ ] Dash 残影：冲刺时身后留下半透明残影
- [ ] 热调参：Play Mode 中修改 SO 参数即时生效
- [ ] `ApplyImpulse()` 仍正常工作

---

---

## Unity 废弃 API 更新：FindObjectOfType → FindFirstObjectByType — 2026-02-16

### 概述
将 Space Life 系统中所有废弃的 Unity API 调用更新到现代替代方案，消除编译警告并确保与 Unity 最新版本的兼容性。

### 修改文件

| 文件路径 | 变更说明 |
|---------|---------|
| `Assets/Scripts/SpaceLife/Room.cs` | `FindObjectOfType&lt;PlayerController2D&gt;()` → `FindFirstObjectByType&lt;PlayerController2D&gt;()` |
| `Assets/Scripts/SpaceLife/PlayerInteraction.cs` | `FindObjectsOfType&lt;Interactable&gt;()` → `FindObjectsByType&lt;Interactable&gt;(FindObjectsSortMode.None)` |
| `Assets/Scripts/SpaceLife/Door.cs` | `FindObjectOfType&lt;PlayerController2D&gt;()` → `FindFirstObjectByType&lt;PlayerController2D&gt;()` |
| `Assets/Scripts/SpaceLife/RoomManager.cs` | `FindObjectsOfType&lt;Room&gt;()` → `FindObjectsByType&lt;Room&gt;(FindObjectsSortMode.None)` |
| `Assets/Scripts/SpaceLife/Interactable.cs` | `FindObjectOfType&lt;PlayerController2D&gt;()` → `FindFirstObjectByType&lt;PlayerController2D&gt;()` |
| `Assets/Scripts/SpaceLife/Editor/SpaceLifeSetupWindow.cs` | 更新 14 处 `FindObjectOfType` 调用为 `Object.FindFirstObjectByType`；修复 `SetupPhase` 枚举访问修饰符 (internal → public)；修复隐式数组类型错误 (显式声明 `UnityEngine.MonoBehaviour[]`) |
| `Assets/Scripts/SpaceLife/Editor/SpaceLifeMenuItems.cs` | 更新 `FindObjectOfType` 调用为 `Object.FindFirstObjectByType`；添加缺失的 `using ProjectArk.SpaceLife.Data;` 命名空间引用 |

### 目的
消除所有 CS0618 废弃 API 警告，确保项目与 Unity 6 及未来版本的兼容性。同时修复了在更新过程中暴露的编译错误（命名空间缺失、访问修饰符不一致、隐式类型数组）。

### 技术
- 单对象查找：`Object.FindObjectOfType&lt;T&gt;()` → `Object.FindFirstObjectByType&lt;T&gt;()`
- 多对象查找（无序）：`Object.FindObjectsOfType&lt;T&gt;()` → `Object.FindObjectsByType&lt;T&gt;(FindObjectsSortMode.None)`
- 现代 Unity API 迁移，向后兼容保持行为一致性

---

---

## 太空生活系统完整实现 — 2026-02-16

**新建文件（23个）：**

**Runtime (16个)：**
1. `Assets/Scripts/SpaceLife/SpaceLifeManager.cs` — 单例管理器，处理模式切换、玩家生成、相机切换
2. `Assets/Scripts/SpaceLife/PlayerController2D.cs` — 2D角色移动控制器（Rigidbody2D，跳跃、地面检测、动画）
3. `Assets/Scripts/SpaceLife/PlayerInteraction.cs` — 玩家互动组件（查找最近可互动、E键互动）
4. `Assets/Scripts/SpaceLife/SpaceLifeInputHandler.cs` — 太空生活输入处理器
5. `Assets/Scripts/SpaceLife/Room.cs` — 房间组件（玩家检测、房间状态）
6. `Assets/Scripts/SpaceLife/RoomManager.cs` — 房间管理器（查找所有房间）
7. `Assets/Scripts/SpaceLife/Door.cs` — 门组件（传送玩家、钥匙验证）
8. `Assets/Scripts/SpaceLife/Interactable.cs` — 可互动对象组件（互动提示、范围检测）
9. `Assets/Scripts/SpaceLife/NPCController.cs` — NPC控制器
10. `Assets/Scripts/SpaceLife/RelationshipManager.cs` — 关系管理器（单例，关系值存储与事件）
11. `Assets/Scripts/SpaceLife/DialogueUI.cs` — 对话UI（打字机效果、选项选择）
12. `Assets/Scripts/SpaceLife/NPCInteractionUI.cs` — NPC综合互动UI
13. `Assets/Scripts/SpaceLife/GiftInventory.cs` — 礼物库存
14. `Assets/Scripts/SpaceLife/GiftUI.cs` — 送礼UI
15. `Assets/Scripts/SpaceLife/MinimapUI.cs` — 小地图UI
16. `Assets/Scripts/SpaceLife/SpaceLifeQuickSetup.cs` — 快速设置脚本

**Data (3个)：**
17. `Assets/Scripts/SpaceLife/Data/NPCDataSO.cs` — NPC数据ScriptableObject
18. `Assets/Scripts/SpaceLife/Data/ItemSO.cs` — 物品数据ScriptableObject
19. `Assets/Scripts/SpaceLife/Data/DialogueData.cs` — 对话数据结构

**Editor (4个)：**
20. `Assets/Scripts/SpaceLife/Editor/SpaceLifeSetupWindow.cs` — 设置向导窗口（分Phase 1-5，一键配置）
21. `Assets/Scripts/SpaceLife/Editor/SpaceLifeMenuItems.cs` — 快捷菜单工具
22. `Assets/Scripts/SpaceLife/Editor/ProjectArk.SpaceLife.Editor.asmdef` — Editor程序集定义
23. `Assets/Scripts/SpaceLife/ProjectArk.SpaceLife.asmdef` — Runtime程序集定义

**内容简述：**
完整的太空生活系统实现，包括：
1. **核心管理器** — SpaceLifeManager单例，处理模式切换、玩家生成、相机切换
2. **2D角色移动** — Rigidbody2D物理移动、跳跃、地面检测、动画控制
3. **房间系统** — Room/RoomManager/Door组件
4. **NPC系统** — NPCController/NPCDataSO/RelationshipManager
5. **对话系统** — DialogueUI带打字机效果
6. **送礼系统** — GiftInventory/GiftUI
7. **编辑器工具** — SpaceLifeSetupWindow分阶段向导、SpaceLifeMenuItems快捷菜单

**目的：** 完整实现太空生活系统，提供双视角切换、2D角色移动、NPC互动等核心功能。

**技术：** Unity MonoBehaviour、ScriptableObject、单例模式、事件驱动、Rigidbody2D物理、PrimeTween动画、UniTask异步。

---

---

## 星图系统设计文档 — 2026-02-16

**新建文件：**
- `Docs/StarChartDesign.md`

**内容：**
完整的星图系统设计文档，包含：
- 系统概述与核心概念（星核、棱镜、光帆、伴星、轨道）
- 轨道布局设计（16:9横版适配、主副轨道背包式布局）
- 拖拽装备系统
- 物品说明浮窗（鼠标hover显示，无卸载按钮）
- 主副轨道自动选中（鼠标靠近自动激活）
- 武器轨道组合切换（上下按钮，加特林旋转视觉效果）
- 未来扩展预留（格子数随游戏进度增加）

**目的：** 将星图系统的完整设计需求整理成可执行的文档，为后续实现提供明确的指导。

**技术：** Markdown文档、需求规格化、交互设计。

---

---

## 太空生活系统设计文档 — 2026-02-16

**新建文件：**
- `Docs/SpaceLifeSystemDesign.md`

**内容：**
完整的太空生活系统设计文档，包含：
- 系统概述（双视角切换：战斗视角 ↔ 飞船内视角）
- 视角切换机制（Tab键切换，C键打开星图）
- 飞船内空间布局（主空间网状连接，6个船员房间）
- 房间类型（驾驶室、星图室、船员休息室、医疗室、厨房、储物室）
- 2D横版角色移动（WASD移动，空格键跳跃）
- NPC互动系统（对话、送礼、关系值）
- 编辑器工具（一键配置向导）

**目的：** 将太空生活系统的完整设计需求整理成可执行的文档，为后续实现提供明确的指导。

**技术：** Markdown文档、需求规格化、游戏设计。

---

---

## 太空生活系统实现检查清单 — 2026-02-16

**新建文件：**
- `Docs/SpaceLifeImplementationChecklist.md`

**内容：**
完整的太空生活系统实现检查报告，包含：
- 整体进度概览（总体完成度约85%）
- 详细实现检查（核心管理器、2D角色移动、房间系统、NPC系统、对话系统、关系系统、送礼系统、双视角切换、输入处理、编辑器工具）
- Unity API更新状态
- 待完成功能清单
- 文件清单（23个已创建文件）
- 总结与下一步建议

**目的：**
系统地检查太空生活系统的实现状态，对比设计文档，列出所有已实现和缺失的功能，为后续开发提供清晰的指导。

**技术：**
Markdown文档、系统检查清单、状态报告。

---

---

## SpaceLife 模块 New Input System 统一迁移 — 2026-02-16

### 新建/修改文件：
| 文件路径 | 变更说明 |
|---------|---------|
| `Assets/Input/ShipActions.inputactions` | 新增 ToggleSpaceLife 和 SpaceLifeJump 两个 Action，添加相应键盘/手柄绑定 |
| `Assets/Scripts/SpaceLife/ProjectArk.SpaceLife.asmdef` | 添加 Unity.InputSystem 程序集引用，添加 rootNamespace |
| `Assets/Scripts/SpaceLife/SpaceLifeInputHandler.cs` | 重构为使用 New Input System，移除旧 Input 类 |
| `Assets/Scripts/SpaceLife/PlayerInteraction.cs` | 重构为使用 New Input System，移除旧 Input 类 |
| `Assets/Scripts/SpaceLife/PlayerController2D.cs` | 重构为使用 New Input System，移除旧 Input 类 |

### 内容：
将 SpaceLife 模块的所有输入处理统一迁移到 New Input System，与项目其他模块保持一致：
1. 在 ShipActions.inputactions 中新增 ToggleSpaceLife Action（绑定 Tab 键）
2. 在 ShipActions.inputactions 中新增 SpaceLifeJump Action（绑定 W/↑/Space/Gamepad 按钮）
3. 复用 Ship map 中已有的 Move Action 和 Interact Action
4. 重构 SpaceLifeInputHandler 使用 ToggleSpaceLife Action
5. 重构 PlayerInteraction 使用 Interact Action
6. 重构 PlayerController2D 使用 Move Action 和 SpaceLifeJump Action
7. 更新 ProjectArk.SpaceLife.asmdef 添加 Unity.InputSystem 引用

### 目的：
确保 SpaceLife 模块与项目整体架构一致，使用 CLAUDE.md 中明确要求的 New Input System 技术栈，避免新旧输入系统混用导致的潜在问题。

### 技术：
Unity New Input System、InputActionAsset、InputAction、事件驱动输入处理。

---

---

## SpaceLife 模块完整重构 — 2026-02-17 11:10

### 新建文件：
| 文件路径 | 变更说明 |
|---------|---------|
| `Assets/Scripts/SpaceLife/TransitionUI.cs` | 过渡动画系统（打字机效果 + 淡入淡出） |

### 修改文件：
| 文件路径 | 变更说明 |
|---------|---------|
| `Assets/Scripts/SpaceLife/SpaceLifeManager.cs` | ServiceLocator + PoolManager + AudioManager + TransitionUI + 异步过渡 |
| `Assets/Scripts/SpaceLife/PlayerController2D.cs` | Top-Down 移动 + 加速度/减速度曲线 |
| `Assets/Scripts/SpaceLife/DialogueUI.cs` | UniTask 打字机效果 + CancellationToken |
| `Assets/Scripts/SpaceLife/RoomManager.cs` | UniTask 相机移动 + CancellationToken |
| `Assets/Scripts/SpaceLife/MinimapUI.cs` | 房间导航按钮动态生成 |
| `Assets/Scripts/SpaceLife/Room.cs` | 新增 Doors 列表属性 |
| `Assets/Scripts/SpaceLife/Door.cs` | 新增 ConnectedRoom 属性 |
| `Assets/Scripts/SpaceLife/RelationshipManager.cs` | Singleton → ServiceLocator |
| `Assets/Scripts/SpaceLife/GiftInventory.cs` | Singleton → ServiceLocator |
| `Assets/Scripts/SpaceLife/GiftUI.cs` | Singleton → ServiceLocator |
| `Assets/Scripts/SpaceLife/NPCInteractionUI.cs` | Singleton → ServiceLocator |
| `Assets/Scripts/SpaceLife/NPCController.cs` | ServiceLocator 引用更新 |
| `Assets/Scripts/SpaceLife/Interactable.cs` | 缓存引用替代 Find |
| `Assets/Scripts/SpaceLife/SpaceLifeInputHandler.cs` | ServiceLocator |
| `Assets/Scripts/SpaceLife/ProjectArk.SpaceLife.asmdef` | 添加 UniTask + ProjectArk.Core.Audio 引用 |
| `Assets/Scripts/SpaceLife/Editor/SpaceLifeSetupWindow.cs` | 完全重写，自动创建 Prefab 和场景结构 |
| `ProjectArk.SpaceLife.csproj` | 添加 ProjectArk.Core.Audio 项目引用 |

### 内容：

#### Phase 1: 架构合规重构
- 迁移所有 Singleton 到 ServiceLocator 模式
- 移除所有 FindAnyObjectByType/FindFirstObjectByType 调用
- 使用缓存引用或 ServiceLocator.Get 替代

#### Phase 2: PlayerController2D 优化
- 改为 Top-Down 2D 移动（gravityScale = 0）
- 添加加速度/减速度曲线，提升手感
- 移除跳跃功能（根据用户反馈）

#### Phase 3: Coroutine → UniTask 迁移
- DialogueUI 打字机效果改用 UniTask
- RoomManager 相机移动改用 UniTask
- 添加 CancellationToken 支持取消操作

#### Phase 4: 集成核心系统
- PoolManager: 玩家 Spawn 使用对象池
- AudioManager: 添加进入/退出音效支持

#### Phase 5: 过渡动画和小地图
- TransitionUI: 打字机效果 + 淡入淡出
- MinimapUI: 动态生成房间导航按钮

#### Bug 修复
1. **看不到 PlayerCharacter**: 一键生成菜单现在自动创建 Player2D_Prefab 并赋值
2. **Tab 无法退出**: SpaceLifeInputHandler 独立于 _shipRoot，进入时启用、退出时禁用

### 目的：
1. 统一架构风格，与项目整体 ServiceLocator 模式保持一致
2. 提升性能，移除 O(n) 的 Find 调用
3. 现代化异步处理，使用 UniTask 替代 Coroutine
4. 集成项目核心系统（PoolManager、AudioManager）
5. 修复用户反馈的两个关键 Bug

### 技术：
- ServiceLocator 依赖注入模式
- UniTask 异步编程 + CancellationToken
- GameObjectPool 对象池
- Top-Down 2D 移动物理
- Unity Editor 脚本（PrefabUtility、SerializedObject）
- 程序集定义（asmdef）引用管理

---

---

## SpaceLife Setup 窗口增强 — 2026-02-17 11:23

### 修改文件：
| 文件路径 | 变更说明 |
|---------|---------|
| `Assets/Scripts/SpaceLife/Editor/SpaceLifeSetupWindow.cs` | 智能检测 + 补齐引用 + Player 可见性修复 |

### 内容：

#### 问题修复
1. **Player 不可见**：Player Prefab 添加 `SpriteRenderer` 组件（使用 Unity 内置 Knob sprite 作为占位符）
2. **重复创建检测**：已存在对象时不再跳过，而是检查并补齐缺失引用

#### 新增功能
- `EnsureManagerReferences()` 方法：统一检查并补齐 SpaceLifeManager 的所有引用
  - `_spaceLifePlayerPrefab`
  - `_spaceLifeSceneRoot`
  - `_spaceLifeSpawnPoint`
  - `_spaceLifeCamera`
  - `_spaceLifeInputHandler`

#### 行为改进
- 已存在 SpaceLifeManager → 检查引用并补齐
- 已存在 Player Prefab → 自动赋值给 Manager
- 已存在 SpaceLifeScene → 检查子对象并补齐缺失的 SpawnPoint/Camera
- 已存在 SpaceLifeInputHandler → 自动赋值给 Manager

### 目的：
1. 修复 Player 在 SpaceLife 模式下不可见的问题
2. 支持增量 Setup，避免重复创建对象
3. 自动补齐缺失的引用，减少手动配置

### 技术：
- SpriteRenderer 组件、Unity 内置资源
- SerializedObject 属性检查与赋值
- Editor 脚本增量检测逻辑

---

---

## SpaceLife 类重命名避免冲突 — 2026-02-17 11:35

### 新建文件：
| 文件路径 | 变更说明 |
|---------|---------|
| `Assets/Scripts/SpaceLife/SpaceLifeRoom.cs` | 原 Room.cs 重命名 |
| `Assets/Scripts/SpaceLife/SpaceLifeRoomManager.cs` | 原 RoomManager.cs 重命名 |
| `Assets/Scripts/SpaceLife/SpaceLifeDoor.cs` | 原 Door.cs 重命名 |

### 删除文件：
| 文件路径 | 变更说明 |
|---------|---------|
| `Assets/Scripts/SpaceLife/Room.cs` | 重命名为 SpaceLifeRoom.cs |
| `Assets/Scripts/SpaceLife/RoomManager.cs` | 重命名为 SpaceLifeRoomManager.cs |
| `Assets/Scripts/SpaceLife/Door.cs` | 重命名为 SpaceLifeDoor.cs |

### 修改文件：
| 文件路径 | 变更说明 |
|---------|---------|
| `Assets/Scripts/SpaceLife/MinimapUI.cs` | 更新引用 SpaceLifeRoom/SpaceLifeRoomManager |
| `Assets/Scripts/SpaceLife/SpaceLifeQuickSetup.cs` | 更新引用 SpaceLifeRoomManager |
| `Assets/Scripts/SpaceLife/Editor/SpaceLifeMenuItems.cs` | 更新菜单创建逻辑 |
| `Assets/Scripts/SpaceLife/Editor/SpaceLifeSetupWindow.cs` | 更新 Setup 窗口创建逻辑 |
| `ProjectArk.SpaceLife.csproj` | 更新文件引用 |

### 内容：
将 SpaceLife 模块中的房间相关类重命名，添加 `SpaceLife` 前缀，避免与 `Level/Room` 模块中的同名类冲突：

| 原类名 | 新类名 | 说明 |
|--------|--------|------|
| `Room` | `SpaceLifeRoom` | 太空生活房间 |
| `RoomManager` | `SpaceLifeRoomManager` | 太空生活房间管理器 |
| `RoomType` | `SpaceLifeRoomType` | 太空生活房间类型枚举 |
| `Door` | `SpaceLifeDoor` | 太空生活门 |

### 目的：
1. 解决 `Level/Room/RoomManager.cs` 与 `SpaceLife/RoomManager.cs` 的命名冲突
2. 保持两个模块独立，避免 ServiceLocator 注册冲突
3. 清晰区分不同模块的职责

### 技术：
- 类重命名、枚举重命名
- 程序集内引用更新
- Unity Editor 菜单更新

---

---

## 删除 One-Click Quick Setup 并合并功能 — 2026-02-17 11:40

### 删除功能：
- 移除 `ProjectArk/Space Life/🚀 One-Click Quick Setup (Visible!)` 菜单项
- 删除相关方法：`CreateSpaceLifeSystem`, `CreateManagers`, `CreatePlayerPrefab`, `CreateSceneContent`, `CreateNPC`, `CreateUI`, `ConnectEverything`

### 合并到 Setup Wizard：
| 功能 | 实现位置 |
|------|----------|
| 可见 Player (青色方块) | `CreatePlayerPrefab()` 使用 `CreateSquareSprite(Color.cyan)` |
| 可见 NPC (彩色方块) | `CreateDemoNPCs()` 为每个 NPC 添加 SpriteRenderer |
| 可见 Background | 新增 `CreateBackground()` 方法 |

### 修改文件：
| 文件路径 | 变更说明 |
|---------|---------|
| `Assets/Scripts/SpaceLife/Editor/SpaceLifeMenuItems.cs` | 删除 One-Click Setup，保留工具方法 |
| `Assets/Scripts/SpaceLife/Editor/SpaceLifeSetupWindow.cs` | 添加 Background 创建，更新 Player/NPC 可视化 |

### 目的：
1. 统一 Setup 入口，避免用户困惑
2. Setup Wizard 具备智能检测和补齐引用功能
3. 所有创建的对象都可见，便于调试

### 技术：
- `CreateSquareSprite(Color)` 生成纯色 Sprite
- SpriteRenderer.drawMode = Tiled 实现可平铺背景
- sortingOrder 控制渲染层级

---

---

## Setup Wizard 完整性修复 — 2026-02-17 11:45

### 问题修复：

| 问题 | 解决方案 |
|------|----------|
| SpaceLifeInputHandler 缺少 InputActionAsset | 新增 `EnsureInputHandlerReferences()` 自动查找并分配 |
| Player Prefab 缺少 InputActionAsset | 创建 Prefab 时自动分配给 PlayerController2D 和 PlayerInteraction |
| SpaceLifeManager 缺少 _shipRoot 引用 | `EnsureManagerReferences()` 自动查找 Ship.InputHandler 并赋值 |

### 修改文件：
| 文件路径 | 变更说明 |
|---------|---------|
| `Assets/Scripts/SpaceLife/Editor/SpaceLifeSetupWindow.cs` | 添加 InputActionAsset 自动分配逻辑 |

### 新增方法：
```csharp
private void EnsureInputHandlerReferences(SpaceLifeInputHandler handler)
{
    // 自动查找并分配 InputActionAsset
}
```

### 自动连接的引用：
| 组件 | 字段 | 来源 |
|------|------|------|
| SpaceLifeManager | _spaceLifePlayerPrefab | Assets/Scripts/SpaceLife/Prefabs/Player2D_Prefab.prefab |
| SpaceLifeManager | _spaceLifeSceneRoot | GameObject.Find("SpaceLifeScene") |
| SpaceLifeManager | _spaceLifeSpawnPoint | SpaceLifeScene/SpawnPoint |
| SpaceLifeManager | _spaceLifeCamera | SpaceLifeScene/SpaceLifeCamera |
| SpaceLifeManager | _spaceLifeInputHandler | FindFirstObjectByType<SpaceLifeInputHandler>() |
| SpaceLifeManager | _shipRoot | FindFirstObjectByType<Ship.InputHandler>().gameObject |
| SpaceLifeInputHandler | _inputActions | AssetDatabase.FindAssets("ShipActions") |
| PlayerController2D (Prefab) | _inputActions | 同上 |
| PlayerInteraction (Prefab) | _inputActions | 同上 |

### 技术：
- SerializedObject + SerializedProperty 运行时赋值
- AssetDatabase.FindAssets 查找资源
- Object.FindFirstObjectByType 查找场景对象

---

---

## 输入系统 ActionMap 冲突修复 — 2026-02-17 11:50

### 问题：
飞船和 SpaceLife 人物都无法控制

### 根本原因：
1. `Ship/Input/InputHandler` 在 `OnDisable` 时调用 `shipMap.Disable()`
2. `PlayerController2D` 和 `SpaceLifeInputHandler` 依赖同一个 `Ship` ActionMap
3. 当 `SpaceLifeManager` 禁用 `Ship/Input/InputHandler` 时，整个 `Ship` ActionMap 被禁用
4. 导致所有依赖该 ActionMap 的组件都无法接收输入

### 解决方案：
让需要输入的组件在 `OnEnable` 时主动启用 ActionMap

### 修改文件：
| 文件路径 | 变更说明 |
|---------|---------|
| `Assets/Scripts/SpaceLife/PlayerController2D.cs` | OnEnable 时检查并启用 Ship ActionMap |
| `Assets/Scripts/SpaceLife/SpaceLifeInputHandler.cs` | OnEnable 时检查并启用 Ship ActionMap |

### 修改代码：
```csharp
// PlayerController2D.OnEnable()
var shipMap = _inputActions.FindActionMap("Ship");
if (shipMap != null && !shipMap.enabled)
{
    shipMap.Enable();
}
```

### 技术：
- InputActionMap.enabled 检查状态
- 多组件共享 ActionMap 时的启用策略

---

---

## Player Prefab 更新机制 & 飞船输入修复 — 2026-02-17 11:55

### 问题：
1. Player2D Prefab 缺少 SpriteRenderer（已存在的 Prefab 不会更新）
2. 飞船 WASD 不可用

### 根本原因：
1. Setup Wizard 在 Prefab 已存在时直接返回，不检查/更新组件
2. `SpaceLifeInputHandler` 在游戏开始时处于启用状态，干扰了 `Ship/Input/InputHandler`

### 解决方案：

#### 1. 新增 `UpdatePlayerPrefabComponents()` 方法
检查并补齐已存在 Prefab 的缺失组件：
- SpriteRenderer + Sprite
- Rigidbody2D (gravityScale = 0)
- CapsuleCollider2D
- PlayerController2D + InputActionAsset
- PlayerInteraction + InputActionAsset

#### 2. SpaceLifeManager.Start() 禁用 SpaceLifeInputHandler
```csharp
if (_spaceLifeInputHandler != null)
    _spaceLifeInputHandler.enabled = false;
```

### 修改文件：
| 文件路径 | 变更说明 |
|---------|---------|
| `Assets/Scripts/SpaceLife/Editor/SpaceLifeSetupWindow.cs` | 新增 UpdatePlayerPrefabComponents 方法 |
| `Assets/Scripts/SpaceLife/SpaceLifeManager.cs` | Start 时禁用 SpaceLifeInputHandler |

### 输入系统状态流程：
```
游戏启动:
  - Ship/Input/InputHandler: enabled (ActionMap 启用)
  - SpaceLifeInputHandler: disabled
  - 飞船 WASD 可用 ✓

进入 SpaceLife:
  - Ship/Input/InputHandler: disabled (ActionMap 禁用)
  - SpaceLifeInputHandler: enabled (OnEnable 重新启用 ActionMap)
  - PlayerController2D: enabled (OnEnable 重新启用 ActionMap)
  - SpaceLife 人物 WASD 可用 ✓

退出 SpaceLife:
  - SpaceLifeInputHandler: disabled
  - PlayerController2D: disabled
  - Ship/Input/InputHandler: enabled (OnEnable 重新启用 ActionMap)
  - 飞船 WASD 可用 ✓
```

### 技术：
- EditorUtility.SetDirty + AssetDatabase.SaveAssets 保存 Prefab 更改
- 组件启用/禁用顺序控制输入系统状态

---

---

## Prefab 存放位置修正 — 2026-02-17 12:00

### 问题：
SpaceLife Prefab 存放在 `Assets/Scripts/SpaceLife/Prefabs/`，不符合项目规范

### 项目规范 (CLAUDE.md)：
```
Assets/
├── _Prefabs/                 # 游戏 Prefab
│   ├── Ship/
│   └── Enemies/
```

### 修改：
将 Prefab 路径从 `Assets/Scripts/SpaceLife/Prefabs/` 改为 `Assets/_Prefabs/SpaceLife/`

### 修改文件：
| 文件路径 | 变更说明 |
|---------|---------|
| `Assets/Scripts/SpaceLife/Editor/SpaceLifeSetupWindow.cs` | 更新 prefabPath 和 folderPath |

### 新路径：
```
Assets/_Prefabs/SpaceLife/Player2D_Prefab.prefab
```

### 技术：
- AssetDatabase.IsValidFolder 检查文件夹是否存在
- AssetDatabase.CreateFolder 创建嵌套文件夹

---

---

## Tab 无法进入 SpaceLife 修复 — 2026-02-17 12:05

### 问题：
按 Tab 无法进入 SpaceLife 模式

### 根本原因：
`SpaceLifeInputHandler` 在游戏开始时被禁用（为了不干扰飞船输入），所以无法接收 Tab 输入

### 解决方案：
让 `Ship/Input/InputHandler` 也监听 `ToggleSpaceLife` action，通过事件通知 `SpaceLifeManager`

### 修改文件：
| 文件路径 | 变更说明 |
|---------|---------|
| `Assets/Scripts/Ship/Input/InputHandler.cs` | 添加 OnToggleSpaceLifePerformed 事件 |
| `Assets/Scripts/SpaceLife/SpaceLifeManager.cs` | 订阅 Ship InputHandler 的事件 |

### 新增代码：
```csharp
// InputHandler.cs
public event Action OnToggleSpaceLifePerformed;
private InputAction _toggleSpaceLifeAction;

// OnEnable
if (_toggleSpaceLifeAction != null)
    _toggleSpaceLifeAction.performed += OnToggleSpaceLifeActionPerformed;

// SpaceLifeManager.cs Start()
_shipInputHandler.OnToggleSpaceLifePerformed += ToggleSpaceLife;
```

### 输入系统架构：
```
飞船模式:
  Ship/Input/InputHandler (enabled)
    └── 监听 ToggleSpaceLife → 触发 OnToggleSpaceLifePerformed 事件
    └── SpaceLifeManager 订阅事件 → 调用 ToggleSpaceLife()

SpaceLife 模式:
  SpaceLifeInputHandler (enabled)
    └── 监听 ToggleSpaceLife → 直接调用 SpaceLifeManager.ToggleSpaceLife()
```

### 技术：
- 事件订阅模式解耦输入处理
- 双入口确保两种模式都能切换

---

---

## 输入系统全面修复 — 2026-02-17 13:00

### 问题：
1. 按 Tab 无反应
2. 飞船只能旋转，WASD 移动无效
3. Console 报错 `Action map must be contained in state`
4. `[ServiceLocator] Get: InputHandler = NOT FOUND`

### 根本原因：
1. **场景中缺少 `Ship/Input/InputHandler` 组件** - 这是 Tab 切换和飞船移动的核心组件
2. **OnDisable 时禁用已无效的 Action** - 当 ActionMap 被禁用后再尝试禁用单个 Action 会报错

### 修复内容：

#### 1. 修复 OnDisable 安全检查
三个组件都需要检查 ActionMap 状态后再禁用 Action：

```csharp
// PlayerController2D.OnDisable()
if (_moveAction != null && _inputActions != null)
{
    var shipMap = _inputActions.FindActionMap("Ship");
    if (shipMap != null && shipMap.enabled)
    {
        _moveAction.Disable();
    }
}
```

同样修复应用于：
- `PlayerController2D.cs`
- `PlayerInteraction.cs`
- `SpaceLifeInputHandler.cs`

#### 2. Setup Wizard 状态检查增强
新增 `Ship/InputHandler` 状态检查：

```csharp
bool hasShipInputHandler = Object.FindFirstObjectByType<ProjectArk.Ship.InputHandler>() != null;
DrawStatusItem("Ship/InputHandler (CRITICAL)", hasShipInputHandler);

if (!hasShipInputHandler)
{
    EditorGUILayout.HelpBox("Ship/InputHandler is MISSING! This is required for Tab toggle to work.", MessageType.Error);
}
```

#### 3. 添加全面调试日志
- `InputHandler.Awake()` - 检查 _inputActions 是否为 null
- `InputHandler.OnEnable()` - 确认 ActionMap 已启用
- `SpaceLifeManager.Start()` - 确认订阅事件成功
- `ServiceLocator.Register/Get` - 追踪服务注册和获取

### 修改文件：
| 文件路径 | 变更说明 |
|---------|---------|
| `Assets/Scripts/SpaceLife/PlayerController2D.cs` | OnDisable 安全检查 |
| `Assets/Scripts/SpaceLife/PlayerInteraction.cs` | OnDisable 安全检查 |
| `Assets/Scripts/SpaceLife/SpaceLifeInputHandler.cs` | OnDisable 安全检查 |
| `Assets/Scripts/Ship/Input/InputHandler.cs` | 添加 null 检查和调试日志 |
| `Assets/Scripts/SpaceLife/SpaceLifeManager.cs` | 添加调试日志 |
| `Assets/Scripts/Core/ServiceLocator.cs` | 添加注册/获取日志 |
| `Assets/Scripts/SpaceLife/Editor/SpaceLifeSetupWindow.cs` | 检查 Ship/InputHandler 状态 |

### 用户需要确认：
**场景中必须有 `Ship/Input/InputHandler` 组件！**

这个组件通常在 Ship Prefab 上，由 `ShipMotor` 或 `ShipDash` 的 `RequireComponent` 自动添加。

### 技术：
- InputAction.Disable() 前检查 ActionMap.enabled
- ServiceLocator 调试日志追踪服务生命周期
- Editor 状态检查提示用户缺失的关键组件

---

---

## SpaceLife 模块 Bug 修复与架构清理 — 2026-02-17 21:08

### 概述
修复 SpaceLife 模块 3 大核心 Bug 并完成输入系统架构解耦，共涉及 8 个任务。

### 修改文件：
| 文件路径 | 变更说明 |
|---------|---------|
| `Assets/Input/ShipActions.inputactions` | 新增独立的 `SpaceLife` ActionMap（Move/Interact/ToggleSpaceLife）；移除多余的 `SpaceLifeJump` Action 及其 4 条绑定 |
| `Assets/Scripts/SpaceLife/SpaceLifeInputHandler.cs` | 从 Ship ActionMap 切换到 SpaceLife ActionMap；OnEnable/OnDisable 操作独立 Map 不再干扰 Ship 输入 |
| `Assets/Scripts/SpaceLife/PlayerController2D.cs` | 从 Ship ActionMap 切换到 SpaceLife ActionMap；Enable/Disable 独立不影响 Ship |
| `Assets/Scripts/SpaceLife/SpaceLifeManager.cs` | 增强序列化引用 fallback 自动获取逻辑（_spaceLifeInputHandler/FindFirstObjectByType、_mainCamera/Camera.main、_shipInputHandler/FindFirstObjectByType fallback）；EnterSpaceLife 和 ToggleSpaceLife 入口添加前置条件检查；所有错误日志增加具体组件名+修复建议 |
| `Assets/Scripts/SpaceLife/Editor/SpaceLifeSetupWindow.cs` | 新增 Scene Health Check 面板（检查所有关键组件 ✅/❌ 状态）；新增 "Add Ship to Scene" 按钮（从 Prefab 实例化）；新增 "Auto-Wire References" 按钮（自动填充 SpaceLifeManager 可推导的序列化引用） |

### 内容简述：
1. **输入系统解耦（B3/B6）**：在 ShipActions.inputactions 中新增独立的 `SpaceLife` ActionMap，包含 Move（WASD 4方向+方向键+Gamepad摇杆）、Interact（E键+Gamepad Y）、ToggleSpaceLife（Tab+Gamepad Back）。SpaceLifeInputHandler 和 PlayerController2D 完全切换到 SpaceLife Map，与 Ship Map 零耦合。
2. **移除 SpaceLifeJump（B5）**：从 Ship ActionMap 删除冗余的 SpaceLifeJump Action 和 4 条绑定（W/↑/Space/Gamepad South）。SpaceLife 确认使用 4 方向移动不需要跳跃。
3. **序列化引用修复（B4）**：SpaceLifeManager.Start() 中为 _spaceLifeInputHandler、_mainCamera、_shipInputHandler 增加运行时 fallback 自动获取+Warning 日志。
4. **防御性增强（B1/B2）**：ToggleSpaceLife 和 EnterSpaceLife 入口增加 _spaceLifePlayerPrefab/_spaceLifeCamera/_spaceLifeSceneRoot 的 null 前置条件检查，失败时打印具体原因。
5. **Editor 工具增强**：SpaceLifeSetupWindow 新增 Scene Health Check 面板，含一键添加 Ship 到场景和 Auto-Wire References 功能。

### 目的：
修复 SpaceLife 模块无法通过 Tab 进入、输入系统 Ship/SpaceLife 互相干扰、序列化引用缺失导致静默失败等问题，建立清晰的输入架构边界。

### 技术方案：
- 方案 A（独立 ActionMap）：在同一 InputActionAsset 中新增 SpaceLife ActionMap，避免共享 Ship Map 的 Enable/Disable 互相干扰
- SpaceLifeManager 使用 ServiceLocator + FindFirstObjectByType 双重 fallback 策略
- Editor 工具使用 SerializedObject API 检查和自动填充序列化引用
- PrefabUtility.InstantiatePrefab 用于一键添加 Ship 到场景

---

---

## SpaceLife Player2D 可见性修复 + 胶囊 Sprite — 2026-02-18 01:05

### 修改文件：
- `Assets/Scripts/SpaceLife/Editor/SpaceLifeMenuItems.cs` — 新增 `CreateCapsuleSprite` 方法；`CreatePlayerController` 菜单项添加 SpriteRenderer + 白色胶囊 sprite
- `Assets/Scripts/SpaceLife/Editor/SpaceLifeSetupWindow.cs` — `CreatePlayerPrefab` 和 `UpdatePlayerPrefabComponents` 改用白色胶囊 sprite（替换原来的 cyan 方形）
- `Assets/Scripts/SpaceLife/PlayerController2D.cs` — 新增 `Start()` 方法，防御性隐藏：非 SpaceLife 模式时自动 `SetActive(false)`

### 内容简述：
1. **CreateCapsuleSprite**：程序化生成 32×64 像素白色胶囊形状 Sprite（半圆顶+矩形身体+半圆底），用于 Player2D 的默认视觉
2. **Setup Wizard / Menu Items**：所有创建 Player2D 的入口（Wizard Phase 1、Create Player Controller 菜单、Update 检查）统一使用白色胶囊 sprite 替代原来的 cyan 方形
3. **Player2D 可见性**：在 `PlayerController2D.Start()` 中通过 `ServiceLocator.Get<SpaceLifeManager>()` 检查当前模式，如果不在 SpaceLife 模式则自动隐藏 gameObject，防止 Player2D 在飞船操作期间意外显示

### 目的：
- 修复 Setup Wizard 创建的 Player2D 缺少 SpriteRenderer / 使用错误形状的问题
- 修复 Player2D 在飞船操作期间仍然可见的 bug

### 技术方案：
- 程序化纹理生成（Texture2D + Sprite.Create）绘制胶囊形状，利用圆心距离判断像素是否在胶囊边界内
- 防御性 Start() 检查利用 ServiceLocator 查询 SpaceLifeManager 状态，确保 Player2D 只在 SpaceLife 模式激活时可见

---

---

## SpaceLife + StarChart 模块 CLAUDE.md 代码规范合规修复 — 2026-02-18 10:15

### 修改文件：
- `Assets/Scripts/SpaceLife/PlayerInteraction.cs` — 替换 FindObjectsByType 为 Trigger 检测模式（OnTriggerEnter2D/Exit2D + \_nearbyInteractables 列表）
- `Assets/Scripts/SpaceLife/SpaceLifeRoomManager.cs` — 移除 FindAllRooms()/FindObjectsByType，改为 RegisterRoom/UnregisterRoom 自注册模式；手写 Lerp 相机平移替换为 PrimeTween.Position；添加 PrimeTween 依赖
- `Assets/Scripts/SpaceLife/SpaceLifeRoom.cs` — 添加 OnEnable/OnDisable 自注册到 RoomManager
- `Assets/Scripts/SpaceLife/SpaceLifeManager.cs` — 移除 FindFirstObjectByType fallback，改为 ServiceLocator-only；OnDestroy 添加 OnEnterSpaceLife/OnExitSpaceLife = null 事件清理
- `Assets/Scripts/SpaceLife/Data/NPCDataSO.cs` — public 字段改为 [SerializeField] private + PascalCase 只读属性
- `Assets/Scripts/SpaceLife/Data/ItemSO.cs` — public 字段改为 [SerializeField] private + PascalCase 只读属性
- `Assets/Scripts/SpaceLife/Data/DialogueData.cs` — DialogueLine/DialogueOption public 字段改为 [SerializeField] private + PascalCase 只读属性
- `Assets/Scripts/SpaceLife/NPCController.cs` — 更新所有 NPCDataSO/DialogueLine 字段引用为新属性名
- `Assets/Scripts/SpaceLife/RelationshipManager.cs` — 更新 NPCDataSO 引用；OnDestroy 添加 OnRelationshipChanged = null；移除 RelationshipLevel 枚举
- `Assets/Scripts/SpaceLife/DialogueUI.cs` — 更新 DialogueLine/DialogueOption 引用；OnDestroy 添加 OnDialogueEnd = null
- `Assets/Scripts/SpaceLife/GiftUI.cs` — 更新 ItemSO 引用；OnDestroy 添加 OnGiftGiven = null
- `Assets/Scripts/SpaceLife/GiftInventory.cs` — 更新 ItemSO 引用；OnDestroy 添加 OnInventoryChanged = null
- `Assets/Scripts/SpaceLife/TransitionUI.cs` — FadeInAsync/FadeOutAsync 手写 Lerp 替换为 PrimeTween.Custom + ToUniTask
- `Assets/Scripts/SpaceLife/Interactable.cs` — 运行时 Instantiate/Destroy 指示器改为 Awake 预创建 + SetActive 切换
- `Assets/Scripts/Combat/StarChart/StarChartController.cs` — 动态 AddComponent<AudioSource> 改为 [RequireComponent] + GetComponent；OnDestroy 添加 OnTrackFired/OnLightSailChanged/OnSatellitesChanged = null

### 新建文件：
- `Assets/Scripts/SpaceLife/SpaceLifeRoomType.cs` — 从 SpaceLifeRoom.cs 提取的枚举
- `Assets/Scripts/SpaceLife/RelationshipLevel.cs` — 从 RelationshipManager.cs 提取的枚举
- `Assets/Scripts/SpaceLife/Data/NPCRole.cs` — 从 NPCDataSO.cs 提取的枚举
- `Assets/Scripts/SpaceLife/ProjectArk.SpaceLife.asmdef` — 新增 PrimeTween.Runtime 引用
- `Assets/Scripts/SpaceLife/Editor/ProjectArk.SpaceLife.Editor.asmdef` — 新增 rootNamespace 字段

### 内容简述：
全面修复 SpaceLife 模块（20个文件）和 StarChart 模块的 CLAUDE.md 代码规范违规项，共覆盖 9 个需求类别。

### 目的：
确保 SpaceLife 和 StarChart 模块完全符合 CLAUDE.md 架构原则和代码规范，消除所有运行时 Find* 调用、public SO 字段、缺失的事件清理、手写 Lerp 补间、动态 AddComponent、枚举与类同文件等技术债务。

### 技术方案：
1. **禁止运行时 Find***：PlayerInteraction 改为 Trigger 碰撞检测 + 列表维护；RoomManager 改为自注册模式；SpaceLifeManager 移除 fallback
2. **SO 数据封装**：NPCDataSO/ItemSO/DialogueData 所有 public 字段改为 `[SerializeField] private` + PascalCase 只读属性（IReadOnlyList 暴露集合）
3. **一文件一类**：SpaceLifeRoomType、RelationshipLevel、NPCRole 枚举各自独立文件
4. **事件卫生**：所有 event/Action 在 OnDestroy 中 `= null` 清空，防止内存泄漏
5. **PrimeTween 替代手写 Lerp**：TransitionUI 的 fade 用 Tween.Custom + useUnscaledTime:true；RoomManager 相机用 Tween.Position
6. **[RequireComponent] 替代动态 AddComponent**：StarChartController 的 AudioSource
7. **Interactable 指示器预创建**：Awake 中创建或引用，SetActive 切换，避免运行时 Instantiate/Destroy

---

---

## 示巴星前15分钟关卡设计三方案 — 2026-02-18 21:07

### 修改文件：
- `Docs/GDD/示巴星 (鸣钟者)/关卡心流与节奏控制.csv`（追加内容）

### 内容简述：
在关卡心流与节奏控制文档末尾追加了示巴星前15分钟的三个详细关卡设计方案及对比总结：
1. **方案一：紧张生存型 (Countdown Escape)** — 高压开场，倒计时逃跑+追击战+压力破墙，心流曲线 8→7→6→2（倒U型）
2. **方案二：探索发现型 (Quiet Discovery)** — 低张力开场，自由探索+听觉解谜+NPC道德选择，心流曲线 2→4→5→6（上升型）
3. **方案三：叙事沉浸型 (Echoes of Silence)** — 叙事驱动，纯氛围醒来+声音=色彩世界观+壁画渐进战斗，心流曲线 1→3→4→5（缓升型）
4. **三方案对比矩阵** — 从开场张力、武器获取方式、教学风格等9个维度横向对比
5. **推荐混合方案思路** — 方案三开场 + 方案一压力 + 方案二选择

### 目的：
为示巴星的前15分钟体验提供多样化的设计选项，支持团队讨论和决策。每个方案针对不同玩家类型（硬核/探索/叙事），并附带心流曲线可视化和风险评估。

### 技术方案：
纯设计文档工作，无代码变更。方案设计基于已有的怪物与机关表、叙事演出表、地图锁钥矩阵等文档数据，确保所有引用的敌人、机关、星图部件均与现有设计一致。

---

---

## 示巴星前一小时关卡心流五方案（5-10分钟颗粒度）— 2026-02-19 01:40

### 修改文件：
- `Docs/GDD/示巴星 (鸣钟者)/关卡心流与节奏控制3.csv`（重建并写入完整内容）

### 内容简述：
1. 依据示巴星现有文档（锁钥矩阵、怪物机关、叙事演出、资产清单）重建 `关卡心流与节奏控制3.csv` 为可直接评审的统一心流表。
2. 在同一表结构下输出 5 套“前1小时”方案（方案A-E），每套拆分为 8 个时段，单段时长控制在 5-10 分钟。
3. 每个时段完整填写 9 列信息：时段、区域、乐章、张力、目标、障碍、关键获取、能力验证、POIs，确保与现有 Z1-Z4 与 L-02/L-03/L-04/L-07 锁钥语义一致。
4. 五套方案分别覆盖不同体验目标：高压生存、探索发现、叙事沉浸、速攻推进、新手友好，便于制作阶段A/B测试与团队评审选型。

### 目的：
为示巴星首小时提供可直接落地的节奏设计备选集，在不改动核心世界观与系统约束的前提下，支持快速比较不同玩家体验路径和难度曲线。

### 技术方案：
- 文档驱动策划：沿用既有 CSV 表头与字段定义，不新增字段，保证与现有流程兼容。
- 锁钥一致性校验：方案中的获取与验证严格映射已有机制（涟漪、相位、穿透、捷径电梯）。
- 节奏分层设计：通过张力曲线与"高压-释放"段落配比，形成可执行的首小时心流模板。

---

---

## 示巴星前一小时关卡设计五方案（全新版） — 2026-02-19 10:32

### 修改文件：
- `Docs/GDD/示巴星 (鸣钟者)/关卡心流与节奏控制.csv`（替换旧的前15分钟方案，追加全新的前一小时5套方案）

### 内容简述：
在关卡心流与节奏控制文档中，将旧的前15分钟3方案区域替换为全新的前一小时（60min）5套详细关卡设计方案及对比总结：

1. **方案一：钟摆型 (The Pendulum)** — 张力在高低(8↔3)之间规律摆动，心流曲线：8-3-8-3-8-3-8
2. **方案二：洋葱层型 (The Onion)** — 复杂度单调递增(移动→战斗→叙事→综合→情感→道德)，心流曲线：1-4-5-7-6-9
3. **方案三：双螺旋型 (Double Helix)** — 战斗线(红)和叙事线(蓝)交替主导融合为合奏，心流曲线：4-2-7-3-8-4-7
4. **方案四：潮汐型 (The Tide)** — 波浪一波比一波高最终海啸，心流曲线：3-2-5-4-7-5-9
5. **方案五：拼图型 (The Jigsaw)** — 独立拼图碎片最后全景拼合，心流曲线：4-6-3-7-4-9
6. **五方案12维对比矩阵 + 推荐混合方案**

### 目的：
为示巴星的前一小时体验提供5套多样化的设计选项，支持团队讨论和决策。

### 技术方案：
纯设计文档工作，无代码变更。方案基于已有怪物与机关表、叙事演出表、地图锁钥矩阵设计。

---

---

## 示巴星前一小时关卡心流五方案（第三批）— 2026-02-19 11:06

### 新建文件：
- `Docs/GDD/示巴星 (鸣钟者)/关卡心流与节奏控制6.csv`（新建）

### 内容简述：
创建第三批示巴星前一小时关卡设计方案，与前两批（共10个方案）形成差异化互补。本次新增5套全新方案：

1. **方案一：螺旋上升型 (The Ascent)** — 每次战斗后回落休息，但每次休息起点都比上次更高，心流曲线：3→6→4→7→5→8→9
2. **方案二：峰谷对称型 (The Mirror)** — 前30分钟上升，30分钟顶峰(9/10)，后30分钟镜像下降，心流曲线：2→4→6→9→6→4→2
3. **方案三：心跳型 (The Heartbeat)** — 模拟心跳节律，收缩期(高压)/舒张期(低压)交替，中间有独特的"暂停"(1/10)，心流曲线：7→3→8→1→9→5→10
4. **方案四：阶梯型 (The Staircase)** — 稳定平台期(3-5)与剧烈跳跃期(8-9)交替，心流曲线：3→8→4→8→5→9
5. **方案五：暗夜行者型 (The Dawn)** — 从黑暗走向光明，开局最高张力(9/10)持续下降到结尾(2/10)，心流曲线：9→7→5→4→3→2

每套方案均包含：完整的时段划分（5-10分钟颗粒度）、区域设置、张力值、核心目标、障碍、获取物品、能力验证、POIs等详细字段。同时提供了三批方案总览（共15个方案）和推荐混合方案思路。

### 目的：
为示巴星前一小时提供更多样化的心流设计选项，覆盖螺旋成长、对称释放、心跳节奏、稳定递进、情感逆转等不同体验目标，支持团队评审选型或A/B测试。

### 技术方案：
纯设计文档工作，无代码变更。方案设计基于已有的怪物与机关表、叙事演出表、地图锁钥矩阵等文档数据，确保所有引用的敌人、机关、星图部件均与现有设计一致。

---

---

## 示巴星完整5小时关卡体验设计 — 2026-02-20 11:10

### 修改文件：
- `Docs/GDD/示巴星 (鸣钟者)/关卡心流与节奏控制-2.csv`（全面重写，63行→205行）

### 内容简述：
将原有的60分钟双螺旋型方案扩展为完整的5小时（300分钟）三幕五章关卡体验设计。文件包含：

**结构框架**：整体架构概览、星图部件获取规划（13件）、敌人图鉴（13种示巴星特化敌人）

**五章关卡流程**：
- **ACT 1 坠落与觉醒 (0:00-50:00)**：6个节点，双武器教学+基础战斗+认知转折(敌人=走调碎片)+首棱镜
- **ACT 2 叹息的峡谷 (50:00-90:00)**：4个节点，涟漪获取+四重用途+坚盾歌者+叹息墙Boss
- **ACT 3 回音枢纽 (90:00-190:00)**：7个节点，渐强渐弱系统+破碎守夜人(可安抚Boss)+光帆净化+指挥家登场+中期合唱考验
- **ACT 4 孵化深渊 (190:00-250:00)**：5个节点，孵化洞穴道德重压+静音走廊极限生存+叙事核弹(大静默真相:听觉猎手)+调律帆
- **ACT 5 哭泣钟楼 (250:00-300:00)**：3个节点，垂直攀登+永恒终止式+最终Boss:星际即兴曲(音乐会式Boss战+双结局)

**总结**：全程心流曲线(27个节点)、设计哲学6条、6星球体系定位矩阵(12维度)

### 目的：
完成示巴星作为第一个星球的完整4-6小时关卡体验设计，确保故事引人入胜、生态惊奇、敌人有趣、能力循序渐进、道德系统逐步深化，同时为后续5个星球预留成长空间。

### 技术方案：
纯设计文档工作，无代码变更。使用Python脚本分4批写入CSV（框架+ACT1→ACT2→ACT3→ACT4+ACT5+总结），确保UTF-8编码正确。所有敌人/星图部件引用与EnemyPlanning.csv和StarChartPlanning.csv保持一致。

---

---

## Universal Shape Contract（全量形状一致性）— 2026-02-23 (今日)

**新建文件：**
- `Assets/Scripts/Combat/Editor/ShapeContractValidator.cs` — C4 护栏：Editor 菜单工具，一键验证所有 ItemShape

**修改文件：**
- `Assets/Scripts/Combat/StarChart/ItemShapeHelper.cs`
- `Assets/Scripts/UI/InventoryView.cs`
- `Assets/Scripts/UI/InventoryItemView.cs`

**内容：**
建立并落地 Universal Shape Contract（C1–C4），解决背包/Track/Ghost 形状显示不一致问题（如 L 形部件在背包中显示为 2x2 方块）。

**C1 单一真源**
- `ItemShapeHelper.GetCells()` 是所有占位查询的唯一依据。
- `GetBounds()` 仅用于外框尺寸显示，不参与占位判定。
- `GetCells()` 的 `default` 分支改为调用 `FallbackCells()`，在运行时对未注册 shape 打印 `LogWarning`（C4 护栏前置）。

**C2 一致占位（背包 shape-cell 改造）**
- `InventoryView.TryFindPosition/CanPlace/MarkOccupied` 全部替换为 `TryFindAnchorForShape/CanPlaceShape/MarkOccupiedByShape`。
- 新方法均遍历 `ItemShapeHelper.GetCells()` 而非矩形边界框，L 形仅占 3 格，不再挤占第 4 个空格。
- 排序策略（行优先、左上优先）保持不变，避免背包卡片位置体验漂移。

**C3 视觉洞位可读（InventoryItemView 透明底）**
- 移除 `Setup()` 中 `bgImage.color = alpha 1` 的强制不透明赋值，改为 `Color.clear`。
- 所有视觉着色仅来自 `BuildShapePreview()` 动态生成的 shape-cell Images。
- L 形的空洞位置现在为真正透明，而不是被卡片背景色填满。
- `_equippedBorder`（stretch Image）改为保持 `Color.clear`，装备状态改由 `BuildShapePreview` 在 active cells 上混合 equippedGreen 颜色（`_isEquipped` 标志）。
- `BuildShapePreview` 现在对所有形状（包括 1×1）生成 shape-cell，active 0.35 opacity，empty Color.clear。
- 新增 `RefreshShapeColors()` 公开方法，支持运行时更新装备色而无需重建 cell 列表。

**C4 可扩展护栏**
- `ItemShapeHelper.ValidateAllShapes()` — 遍历所有 `ItemShape` 枚举值，验证 cells 非空、cells 在 bounds 范围内、bounds 不小于实际 max cell extents，返回文字报告。
- `ShapeContractValidator.cs`（Editor-only）— `ProjectArk > Validate Shape Contract` 菜单，调用 `ValidateAllShapes()`，结果以 `DisplayDialog + Debug.Log/LogError` 双路输出。
- 新增 shape 时：只需在 `GetCells()` + `GetBounds()` 各加一个 case，运行一次菜单验证，其余 UI/拖拽逻辑无需修改（C4）。

**验收标准回归：**
1. L 形部件在背包中占 3 格（不再挤占第 4 格），视觉上第 4 角透明。✓
2. 所有非矩形形状（ShapeL、ShapeLMirror）的 empty cells 视觉上可辨识为洞位。✓
3. Track overlay / Ghost 已通过 `ItemShapeHelper.GetCells` 正确渲染（现有逻辑，回归验证）。✓
4. `ProjectArk > Validate Shape Contract` 对所有当前 6 个形状报告 PASS。✓
5. 新增 ItemShape 枚举值后未更新 GetCells，运行时 console 出现 LogWarning（C4 前置护栏）。✓

**技术：**
- Shape-cell 背包占位（替代矩形 bounding-box 占位）
- C# HashSet<Vector2Int> active-cell 快查
- 透明卡片底 + 动态 Image 子节点着色（完全 shape-driven 视觉）
- Editor MenuItem + `System.Text.StringBuilder` 自检报告

---

---

## 世界观圣经框架 (World Bible Framework) — 2026-02-23 11:22

### 新建文件：
- `Docs/GDD/WorldBible.md`

### 内容简述：
创建《静默方舟》完整世界观设定框架文档（World Bible v1.0），共九篇章：
1. **宇宙本体论**：大静默机制（熵的强制归零）、侵蚀三阶段、宇宙听觉猎手
2. **文明图谱**：人类文明（巴别塔事件）+ 六大异类种族完整设定（十七年蝉/鸣钟者/因果倒置者/活体图书馆/宿主乘客/数学家）+ 种族关系矩阵
3. **星球生态**：三大星球（克洛诺斯/示巴/欧几里得）的环境、种族配对、核心冲突与玩家抉择
4. **核心角色**：金丝雀号三人组（舰长/伊恩/守夜人）+ 示巴星NPC缕拉完整故事弧 + 大指挥家
5. **核心主题与哲学**：三组核心矛盾、清算电影基调、理解度系统、因果透支机制
6. **视听美学宪章**：视觉/听觉/UI美学总纲与禁忌
7. **结局哲学**：飞升概念、六族逻辑大合奏、示巴星结局分支
8. **术语表**：18个核心术语定义
9. **参考圣经**：叙事/角色/美学/游戏设计四类参考作品

### 目的：
将散落在GDD.md、静默方舟.md、示巴星V2.md、NPC角色设定.md、关卡心流CSV等多个文档中的世界观信息整合为单一权威参考源，建立"宪法级"框架，确保所有子文档与其保持一致。

### 技术方案：
纯设计文档工作，无代码变更。综合分析所有现有设计文档后提炼核心世界观要素，按逻辑层次组织为九篇章结构。文档作为所有后续世界观相关设计的终极参考。

---

---

## 转换星球构建框架文档 — 2026-02-23 11:35

### 新建文件：
- `Docs/GDD/星球构建框架.md`

### 内容简述：
将 `Docs/GDD/星球构建框架.docx` 文件提取并转换为 Markdown 格式的文档 `Docs/GDD/星球构建框架.md`。该文档深度构建了游戏世界观的六个层次：
1. 第一层：物理与环境层（核心公理、空间拓扑、大气氛围等）
2. 第二层：生物与生态层（生理基质、感官逻辑、生态位等）
3. 第三层：文明与历史层（社会逻辑、建筑风格、历史断层等）

---

---

## 示巴星 (Sheba) 完整设定圣经文档 — 2026-02-23 18:30

### 新建文件：
- `Docs/GDD/示巴星 (鸣钟者)/ShebaPlanetBible.md`

### 内容简述：
创建示巴星完整设定圣经文档（约3000+行），作为示巴星一切设计的"宪法级"终极参考源。整合并扩展 WorldBible.md、示巴星V2.md、NPC-鸣钟者角色设定.md 等已有设定，与关卡心流CSV和地图锁钥矩阵保持一致。文档包含七篇：

1. **第一篇：星球物理与天文** — 声学显形定律公式化描述、热力学反转机制、天文参数、气象系统（声波风暴/静默潮/晶化雾）、世界时钟规则
2. **第二篇：鸣钟者文明** — 生理学（压电晶体外壳/声学凝胶内质）、七声部社会结构、日常生活细节、经济技术体系、七章史完整历史
3. **第三篇：区域地理与生态** — Z1-Z6六大区域详述、声学食物链、18种物种图谱（含8种非敌对生物）、6种植被/菌类、大静默生态破坏路径
4. **第四篇：缕拉完整人物设定** — 基础档案、3200年个人史、多维性格画像、5种标志性旋律库、关系网（6组NPC关系）、5幕故事弧、3个共情触发点、buff辅助表
5. **第五篇：NPC图谱** — 索尔(Sol)、大指挥家(Grand Conductor)、破碎守夜人(Broken Watchman)、痛苦长老(Elder of Sorrow)、巨型休眠鸣钟者(Sleeping Giant)、歌唱爬行者(Singing Crawlers)、黑水之母(Mother of Black Water)、活体图书馆观察者(Living Archive Observer)、失调工匠(Detuned Artisan)、回声体(Echo Remnants) — 共10个NPC完整设定页
6. **第六篇：文明遗迹与考古学** — Z1-Z6遗迹目录(21处)、7条克拉德尼文铭文(含原文与翻译)、4段黑匣子录音、七章史交叉引用
7. **第七篇：感官与美学规范** — 色彩字典(含Hex值)、6种核心材质库、Z1-Z6环境音效三层结构、6种光源类型与区域光照氛围

附带完整术语表（40+条目）。

### 目的：
为示巴星关卡提供完整的世界观、NPC、生态、美学参考，确保后续所有子文档（关卡设计表、美术参考板、音效设计稿、对话脚本）的一致性。

### 技术方案：
Markdown设定文档，整合已有设定+大幅扩展。参考源：WorldBible.md、示巴星V2.md、NPC-鸣钟者角色设定.md、关卡心流与节奏控制-2.csv、怪物与机关.csv、叙事与遭遇演出表.csv、地图逻辑与锁钥矩阵.csv。
4. 第四层：冲突与矛盾层（敌意逻辑、玩家定位、反派具象化等）
5. 第五层：干涉与救赎层（干涉机制、世界演变、认知重构等）
6. 第六层：交互与元系统层（界面叙事、元游戏成长、感官反馈等）
以及增补层：时间动态层（宏观周期、生态调度）和听觉架构层（声学物理、功能性音频）。

### 目的：
为星球构建提供一套规范化的深度架构指南，帮助设计在物理、生物、文明、冲突等多维度保持统一性和深度，并便于在项目中直接查阅和版本控制。

### 技术方案：
通过执行已有的 Python 脚本 `convert_docx.py`，解析 `.docx` 文件的 `word/document.xml`，提取文本内容与加粗样式等格式，最终将其转换为标准的 Markdown 文件输出。

---

---

## GDD 文档：世界构成框架 — 2026-02-23 23:34

### 新建文件：
- `Docs/GDD/世界构成框架.md`

### 内容简述：
创建世界构成框架元方法论文档，汇集构建 WorldBible（宇宙级）和 ShebaPlanetBible（星球级）过程中使用的所有维度、要素和思考工具。文档包含：

- **第零层：核心支柱与基调** — 高概念、设计支柱、情感基调声明、禁忌清单
- **第一层：物理与宇宙** — 核心公理、物理法则、天文参数、气象系统、科技边界、世界时钟、公理→机制映射
- **第二层：生态与生物** — 物种图谱、食物链/能量循环、植被/菌类/矿物、变异/感染视觉规范
- **第三层：文明与历史** — 种族百科、派系关系矩阵、经济系统、编年史
- **第四层：地理与空间** — 宏观地图、区域详述模板、建筑学/聚落逻辑、关键地标POI
- **第五层：角色** — 主角设定、核心NPC完整设定页模板、反派设计原则、补充NPC、跨星球NPC设计原则
- **第六层：遗迹与考古** — 遗迹目录模板、铭文/收集物、Lore五层传递方法论、音频日志
- **第七层：感官与美学** — 色彩字典、材质库、光照指南、音效设计指南、音乐风格指南、视觉禁忌清单
- **第八层：主题与哲学** — 核心矛盾、叙事基调、道德灰度框架、结局哲学
- **第九层：术语与命名** — 术语表规范、命名法则（按种族/层级）
- **第十层：一致性与交叉引用** — 一致性检查清单、子文档索引、参考圣经

附录包含：宇宙级vs星球级文档分工表、文档编写顺序建议、12条常见陷阱避坑指南、7条质量自检问题。

所有要素均以问题驱动形式呈现，附带资深世界架构师的心得和指导意见。

### 目的：
作为元方法论文档，为未来任何星球/世界的设定工作提供可复用的通用框架。确保后续克洛诺斯星、欧几里得星等PlanetBible的编写遵循一致的维度和质量标准。

### 技术方案：
Markdown方法论文档，综合WorldBible.md（2145行）和ShebaPlanetBible.md（2060行）的实际构建经验，提炼为通用框架。

---

---

## 删除"宇宙听觉猎手"概念 — 2026-02-24 00:49

### 修改文件：
- `Docs/GDD/WorldBible.md` — 删除1.3章节（宇宙听觉猎手定义）、修改过热机制描述、删除术语表条目
- `Docs/GDD/示巴星 (鸣钟者)/ShebaPlanetBible.md` — 替换13处"听觉猎手"引用为"大静默"相关表述、删除术语表条目
- `Docs/GDD/示巴星 (鸣钟者)/NPC-鸣钟者角色设定.md` — 替换4处引用（章节标题、指挥家态度、索尔遗言录音）
- `Docs/GDD/示巴星 (鸣钟者)/示巴星 V1.md` — 替换1处引用（真相揭示段落）
- `Docs/GDD/示巴星 (鸣钟者)/示巴星 V2.md` — 替换1处引用（结局真相描述）
- `Docs/GDD/示巴星 (鸣钟者)/关卡心流与节奏控制-2.csv` — 替换3处引用（叙事核弹描述、索尔遗言、验证列）

### 内容简述：
彻底删除"宇宙听觉猎手 (Auditory Predators / Cosmic Listener)"这一概念。该概念与"大静默"功能完全重叠——两者都是"追踪信息噪音源并将其归零"的宇宙级威胁。删除后，所有叙事中原由"听觉猎手"承担的威胁角色统一由"大静默"本身直接承担。

### 目的：
消除概念冗余。大静默作为宇宙熵增现象本身就具备"侵蚀信息噪音源"的特性，无需额外引入一个"执行者"实体。简化世界观层级，让威胁模型更清晰：大静默 = 唯一的宇宙级外部威胁。

### 技术方案：
全文搜索替换，逐处根据上下文调整措辞确保叙事自洽。关键改写包括：
- "被听觉猎手追踪" → "被大静默感知/侵蚀"
- "听觉猎手已接近" → "大静默已逼近"
- "引来听觉猎手" → "加速大静默的侵蚀"
- "听觉猎手找不到你" → "大静默触及不到你"
涉及6个文件共约22处修改，已通过全目录grep验证无残留。

---

---

## 新增终焉者种族概念储备 — 2026-02-25 15:30

### 修改文件：
- `Docs/GDD/WorldBible.md` — 在第二篇文明图谱新增 VII. 终焉者条目；在 1.4.2 种族能力边界表新增终焉者一行

### 内容简述：
新增第七个种族"终焉者 (The Concluders)"作为概念储备条目（标注 ⚠️ 待深化）。核心概念：以死亡为圆满而非终止的文明——每个个体携带"存在方程"，生命是逐步求解的过程，方程解出即完成，强制杀死等于污染而非解答。包含总览表、生存逻辑、战斗机制（完成催化/未竟枷锁/共情协议终极形态）、创伤细节。

### 目的：
填补现有六族对"死亡假设"这一人类认知底层盲区的覆盖空白。终焉者是唯一不恐惧大静默的文明，在叙事上能产生独特张力；其 Boss 战机制（推断并帮助完成存在方程）是共情协议系统最深层表达，与支柱 I"理解即武器"直接对位。

### 技术方案：
在数学家条目之后、2.3 种族关系矩阵之前插入完整条目。1.4.2 能力边界表同步补录，保持文档内部一致性。

---

---

## 示巴星 ACT1+ACT2 关卡 JSON 生成 + LevelDesigner 空格平移功能 — 2026-02-25 20:00

### 新建文件：
- `Docs/LevelDesigns/Sheba_ACT1_ACT2.json` — 示巴星 ACT1+ACT2 完整关卡布局

### 修改文件：
- `Tools/LevelDesigner.html` — 空格键平移画布（Figma 风格）+ 多项 Bug 修复

### 内容简述：

#### 1. 示巴星 ACT1+ACT2 关卡 JSON
根据 `关卡心流与节奏控制-2.csv` 节点 Z1a～Z2d（0:00～90:00，10个节点）生成可直接导入 LevelDesigner.html 的布局文件：
- **29 个房间**：safe / normal / arena / boss 四种类型，含 Z1d 隐藏墓穴（floor -1）、Z2a 上层平台（floor 1）、Z2d Boss 竞技场（22×13 最大尺寸）
- **28 条连接**：主线路径 + 侧路分岔，完整覆盖 ACT1 六节点与 ACT2 四节点
- **元素配置**：生成点、检查点、NPC（缕拉5幕弧触发点）、宝箱、敌人、门，与叙事与遭遇演出表、锁钥矩阵保持一致

#### 2. 空格键平移画布（Figma 风格）
- 按住 `Space` 光标变 grab，拖拽时变 grabbing，松开恢复正常
- 引入 `#world` 容器包裹所有房间 div，`transform: translate(panX, panY)` 整体平移
- SVG 连线层与 `#world` 同步 `transform`，坐标系始终对齐
- 网格背景（canvas）用 `panX/Y % GRID_SIZE` 取模计算偏移，实现无限滚动网格视觉
- 输入框聚焦时空格不触发平移

#### 3. Bug 修复（共 6 项）
1. **连线起点偏移**：`startConnection` 初始化 `mouseX/Y` 时漏减 `panX/Y`
2. **元素 drop 坐标重复偏移**：`addElementToRoom` 传参时 `x + panX` 重复计算，改为直接传 world 坐标
3. **pan 模式下误触发房间拖拽**：`setupRoomEvents mousedown` 加 `if (isPanMode) return` 拦截
4. **pan mousedown 事件时序**：改为 capture 阶段注册（`addEventListener(..., true)`）
5. **连线消失**：SVG 误放入 `#world`（宽高 0 容器）→ 移回 `canvas-area` 直接子级，`applyPan` 同步 SVG transform
6. **拖拽元素到画布无反应**：给 `#world` 加 `dragover` 监听；改为坐标 hit-test 自动检测落点所在房间

### 目的：
提升 LevelDesigner 工具可用性，支持大型关卡（29 个房间）的平移浏览；交付示巴星 ACT1+ACT2 完整关卡布局供策划评审。

### 技术方案：
- Pan 系统：`#world` + SVG 双层同步 translate，canvas 网格 modulo 偏移
- 坐标系统一为 world 空间：`worldX = screenX - canvasRect.left - panX`
- 元素 drop：`world.addEventListener('dragover')` + 坐标 hit-test fallback

---

---

## LevelDesigner 连接线方向与选中功能 — 2026-02-26 00:50

### 修改文件：
- `Tools/LevelDesigner.html`

### 内容简述：
重构 LevelDesigner HTML 工具中的连接线系统，支持单向/双向门配置与连接线交互操作：

1. **单向/双向连接逻辑**：从房间A拉到房间B创建一条A→B单向连接；再从B拉回A创建B→A单向连接，两条记录共存即表示双向互通。同方向不允许重复创建。
2. **箭头方向渲染**：每条连接线末端带SVG箭头（`<marker>`），指向目标房间。双向连接时两条线各自带箭头，并施加垂直偏移避免重叠。
3. **连接线可选中**：SVG连接线添加透明宽14px的hitarea层接收鼠标事件，点击选中后高亮为蓝色，属性面板显示连接详情（类型、来源、目标、方向）。
4. **连接线可删除**：选中连接线后可通过属性面板按钮或Delete/Backspace键删除。双向连接可选择只删单向或删除整对。
5. **属性面板联动**：选中房间时显示连接列表（标注单向/双向）；选中连接时显示连接属性面板；点击空白取消所有选中。

### 目的：
让关卡设计师能直观配置门的通行方向（单向门/双向门），为后续对接项目 Door 系统做准备。

### 技术方案：
- 连接数据结构保持 `{from, to, fromDir, toDir}` 不变，通过是否存在反向记录判断双向
- SVG 使用 `<marker>` 定义箭头，`<line>` 绘制可见线段与隐形点击区域
- 双向连接的两条线施加±4px垂直偏移，避免视觉重叠
- `selectedConnectionIndex` 全局状态管理选中的连接线索引
- 选中房间与选中连接互斥，切换时自动取消对方的选中状态

---

---

## LevelDesigner 严重 Bug 修复：mousemove 代码块合并错误 — 2026-02-26 01:03

### 修改文件：
- `Tools/LevelDesigner.html`

### 内容简述：
修复 mousemove 事件处理器中 `isDraggingRoom` 与 `isResizing` 两个代码块被错误合并到同一 if 块内的致命 bug。

### 目的：
修复页面完全无法交互的致命错误（拖拽、选择、连接等所有功能均失效）。

### 技术方案：
- **根因**：之前的编辑不慎将 `isResizing` 的整段逻辑错误地拼接到了 `isDraggingRoom` 块内部，导致 `const dx` / `const dy` / `const div` 在同一块作用域中被重复声明，触发 JavaScript `SyntaxError`，整个 `<script>` 标签无法执行。
- **修复**：将 `isDraggingRoom` 块正确关闭（`}`），并将 resize 代码恢复到独立的 `if (isResizing && selectedRoom) { ... }` 块中。

---

---

## LevelDesigner 自动生成门元素 + doorLink 功能 — 2026-02-26 01:15

### 修改文件：
- `Tools/LevelDesigner.html`

### 内容简述：
当从房间A拉连接线到房间B创建连接时，自动在目标房间B中生成一个"🚪门"元素并自动创建 doorLink 关联：

1. **自动生成门元素**：创建房间连接时，自动在目标房间内靠近入口方向的边缘处放置门元素（12% 内缩偏移），无需手动放置。
2. **自动创建 doorLink**：门元素生成后，自动创建一条从源连接点到目标门元素的 doorLink 虚线。
3. **位置推算**：`calculateDoorPosition(room, entryDir)` 根据入口方向（west/east/north/south）计算门在目标房间内的合理位置（靠近入口边缘的中点，12% inset）。
4. **删除联动**：删除连接时自动清理关联的 doorLinks（`cleanupDoorLinksForConnection`）。删除连接对（双向）时清理双向 doorLinks。

### 目的：
减少手动配置工作量——创建房间连接后门和 spawn point 自动就位，设计师只需微调位置即可。

### 技术方案：
- 新增 `calculateDoorPosition(room, entryDir)` 函数，基于 entryDir 返回 `{x, y}` 相对于房间的本地坐标
- 在 mouseup 的正常连接创建分支中，`connections.push` 后立即调用 `targetRoom.elements.push({ type: 'door', ... })` 和 `doorLinks.push({...})`
- `deleteSelectedConnection` 和 `deleteConnectionPair` 中新增 `cleanupDoorLinksForConnection(conn)` 调用

---

---

## LevelDesigner 连接点→门元素关联（doorLink）功能 — 2026-02-26 01:15

### 修改文件：
- `Tools/LevelDesigner.html`

### 内容简述：
新增"门关联"（doorLink）功能，允许从房间边缘的连接点拖拽到另一个房间内的"门"元素上，建立关联关系：

1. **交互方式**：从房间连接点（红点）拖拽，松开在目标房间的"🚪门"元素上即可创建 doorLink。拖拽过程中门元素会高亮发光提示可连接。仅门元素可作为目标，其他房间元素不响应。
2. **虚线可视化**：doorLink 渲染为橙色虚线（`stroke-dasharray: 6,4`），从连接点到门元素位置，末端有小圆点标记门的位置。
3. **选中与删除**：doorLink 可点击选中（蓝色高亮），属性面板显示连接点来源和目标门信息，支持删除按钮和 Delete/Backspace 键删除。
4. **位置联动**：拖拽房间移动、调整大小、拖拽门元素位置时，虚线实时更新。删除房间时自动清理相关 doorLinks。
5. **数据持久化**：doorLinks 数组纳入 JSON 导出/导入、本地保存/加载、画布清空。

### 目的：
让关卡设计师能直观指定"从某个连接进入后，玩家在哪个门元素位置生成"，为项目中 Door spawn point 系统的配置做准备。

### 技术方案：
- 新增 `doorLinks` 全局数组，每条记录 `{fromRoomId, fromDir, targetRoomId, targetDoorIndex}`
- 在 mouseup 的 isConnecting 分支中，优先检测 `document.elementFromPoint` 是否命中 `element-door` 类型的房间元素，命中则创建 doorLink 而非普通连接
- SVG 虚线渲染通过 `renderDoorLinks()` 函数独立管理，与 `renderConnections()` 分离
- 门元素拖拽期间高亮提示通过 CSS 类 `door-link-highlight` 实现（`box-shadow + scale`）
- `selectedDoorLinkIndex` 管理选中状态，与 `selectedConnectionIndex` 和 `selectedRoom` 三者互斥

---

---

## LevelDesigner 连接线箭头精准接触 + 选中/删除功能恢复 — 2026-02-26 01:23

### 修改文件：
- `Tools/LevelDesigner.html`

### 内容简述：
重新实现连接线的 SVG 箭头渲染（箭头尖端精确接触房间边缘），并恢复之前丢失的连接线选中、删除、属性面板等功能。

### 目的：
1. 修复箭头不接触房间边缘的视觉问题（用户反馈"看着难受"）
2. 恢复在之前编辑过程中意外丢失的连接线交互功能

### 技术方案：
- **SVG marker 箭头**：定义 `<marker>` 元素（红色/蓝色），关键参数 `refX=markerWidth(10)`，使箭头尖端精确定位在 line 终点
- **line 终点缩短**：将 line 终点从房间边缘往回缩 `arrowLen(10px)`，这样 marker 的箭头体自然填充这段距离，尖端正好接触边缘
- **双向连接偏移**：双向连接的两条线做 ±4px 垂直偏移避免重叠
- **hitarea 选中层**：每条连接线额外绘制透明 14px 宽的 hitarea `<line>` 接收鼠标点击
- **连接属性面板**：选中连接后右侧面板显示类型/来源/目标/方向，提供删除按钮
- **Delete/Backspace 快捷键**：支持键盘删除选中的连接线或房间
- **同方向去重**：连接创建时检查是否已存在 A→B 的同方向连接
- **连接列表增强**：选中房间时连接列表标注 (单向)/(双向) 类型，使用 →/←/⟷ 符号

---

---

## LevelDesigner Bug修复：点击空白处未取消房间选中状态 — 2026-02-26 11:28

### 修改文件：
- `Tools/LevelDesigner.html`

### 内容简述：
修复了点击画布空白区域后，已选中房间的连接点（connection points）仍然显示的问题。

### 目的：
连接点应仅在房间被选中（selected）或鼠标悬浮（hover）时显示，点击空白处取消选中后连接点应隐藏。

### 技术方案：
- **根因**：`canvasArea` 的 click 事件中，判断"点击空白处"的条件仅检查了 `e.target === canvasArea || e.target === gridCanvas`，遗漏了 SVG 层（`connections-svg`）和世界容器（`world`）。由于 DOM 层叠结构为 `canvas-area > grid-canvas + connections-svg + world`，点击空白处时 `e.target` 可能命中 SVG 或 world 元素，导致取消选中逻辑未执行。
- **修复**：将判断条件扩展为 `e.target === canvasArea || e.target === gridCanvas || e.target === svg || e.target === world`，覆盖所有非房间的空白区域元素。

---

---

## LevelDesigner Bug修复：连接线箭头未接触房间边缘 — 2026-02-26 11:30

### 修改文件：
- `Tools/LevelDesigner.html`

### 内容简述：
修复了连接线箭头尖端与房间视觉边缘之间存在明显间距、未精确接触的问题。

### 目的：
箭头尖端应精确接触房间的视觉边缘（border 外缘），而非停留在 content 边缘内侧。

### 技术方案：
两处根因，两处修复：

1. **连接点坐标未计算 border 偏移**：
   - `getConnectionPointPosition` 返回的坐标基于 `room.x/y/width/height`（content 区域边缘），但房间 DOM 使用 `content-box` 模型 + `border: 2px`，视觉边缘比 content 边缘外扩 2px。
   - **修复**：为四个方向的连接点坐标各加上 `border = 2` 的偏移量（north: `y - 2`, south: `y + height + 2`, east: `x + width + 2`, west: `x - 2`）。

2. **线段被错误缩短导致箭头远离终点**：
   - 原代码将可见线段终点缩短了 `arrowLen = 10px`，而 SVG marker 的 `refX=10` 让箭头尖端锚定在缩短后的线段终点，导致箭头尖端实际位于 `toPos - 10px` 处。
   - **修复**：移除线段缩短逻辑，直接以 `toPos`（房间边缘）为线段终点。marker `refX=markerWidth` 已保证箭头尖端对齐线段终点，箭头体自然向后延伸，无需手动缩短。

---

### LevelDesigner 自动生成门元素 + doorLink 功能实现 — 2026-02-26 12:11

**修改文件：**
- `Tools/LevelDesigner.html`

**内容简述：**
实现了连接线创建时自动在目标房间生成门元素（door）并建立 doorLink 关联的完整功能。

**目的：**
当两个房间通过连接点建立连接时，自动在目标房间的入口方向附近生成一个门元素，并通过橙色虚线（doorLink）将源房间连接点与目标房间门元素关联起来，实现连接→门的自动化工作流。

**技术方案：**

1. **数据结构**：
   - 全局 `doorLinks` 数组，每个元素包含 `{ fromRoomId, fromDir, targetRoomId, targetDoorIndex }`
   - 全局 `selectedDoorLinkIndex` 用于选中交互

2. **CSS 样式**：
   - `.door-link-line`：橙色虚线（stroke-dasharray: 6,4），选中时变蓝变粗
   - `.door-link-hitarea`：14px 宽透明点击区域
   - `.door-link-dot`：橙色小圆点标记门端位置
   - `.room-element.door-link-highlight`：门元素高亮效果（橙色发光+缩放）

3. **核心函数**：
   - `calculateDoorPosition(room, entryDir)`：根据入口方向计算门元素在房间内的位置（12%内缩）
   - `renderDoorLinks()`：SVG 渲染所有 doorLink 虚线、点击区域、端点圆点
   - `selectDoorLink(idx)` / `deselectDoorLink()`：选中/取消选中交互，互斥房间/连接选中
   - `showDoorLinkProperties(idx)`：在属性面板显示 doorLink 详情
   - `deleteSelectedDoorLink()`：删除选中的 doorLink
   - `cleanupDoorLinksForConnection(conn)`：删除连接时联动清理对应的 doorLinks

4. **自动生成逻辑**：
   - 在 mouseup isConnecting 分支中，创建连接后自动调用 `calculateDoorPosition` 在目标房间生成门元素
   - 同时创建 doorLink 将源连接点与新门元素关联

5. **交互支持**：
   - 点击虚线选中 doorLink，属性面板显示详情和删除按钮
   - Delete/Backspace 键删除选中的 doorLink
   - 选中 doorLink/连接/房间三者互斥
   - 删除连接或删除房间时联动清理相关 doorLinks

6. **数据持久化**：
   - `getExportData()` 导出 doorLinks
   - `importJsonData()` 导入 doorLinks 并渲染
   - `saveToLocal()` / `loadFromLocal()` 保存/加载 doorLinks
   - `clearAll()` 清空 doorLinks

7. **实时更新**：
   - 房间拖拽、缩放时同步更新 doorLinks 渲染
   - 画布点击空白处取消 doorLink 选中

---

---

## LevelDesigner 补全 doorLink 手动关联交互 — 2026-02-26 12:20

### 修改文件：
- `Tools/LevelDesigner.html`

### 内容简述：
补全 doorLink 功能中缺失的两项手动关联交互：

1. **拖拽过程门元素高亮提示**：在 `mousemove` 的 `isConnecting` 分支中，通过 `document.elementFromPoint` 实时检测鼠标下方是否为门元素（`element-door`），命中时添加 `door-link-highlight` CSS class（橙色发光 + scale 1.2），离开时移除。仅对非源房间的门元素响应。
2. **拖拽到已有门元素创建 doorLink**：在 `mouseup` 的 `isConnecting` 分支中，优先检测 `elementFromPoint` 是否命中 `element-door` 类型的房间元素。命中时仅创建 doorLink（不创建普通连接线），含重复检测防止同一源→目标门的重复关联。未命中门元素时走原有逻辑（创建连接线 + 自动生成门 + 自动 doorLink）。

### 目的：
补全 doorLink 功能的手动关联模式——允许用户从连接点拖拽到已有的门元素上建立关联关系，与自动关联模式互补，提供更灵活的关卡设计工作流。

### 技术方案：
- **mousemove 高亮**：每帧 `elementFromPoint` 检测 → 移除旧 `.door-link-highlight` → 命中门元素且非源房间则添加 class
- **mouseup 优先级分支**：Priority 1 检测门元素 → 创建 doorLink only；Priority 2 走原有 `closest('.room')` → 创建连接 + 自动门 + 自动 doorLink
- **防重复**：`doorLinks.some()` 检查 fromRoomId + fromDir + targetRoomId + targetDoorIndex 四元组唯一性
- **清理**：mouseup 开头统一移除残留的 `door-link-highlight` class

---

---

## LevelDesigner doorLink 语义修正（虚线起点改为目标房间入口连接点） — 2026-02-26 12:27

### 修改文件：
- `Tools/LevelDesigner.html`

### 内容简述：
修正 doorLink 的数据结构与渲染语义，使虚线从目标房间的入口连接点画到房间内的门元素，而非从源房间的连接点画过去。

1. **数据结构重构**：`doorLink` 从 `{fromRoomId, fromDir, targetRoomId, targetDoorIndex}` 改为 `{roomId, entryDir, doorIndex}`，语义变为"从 entryDir 方向进入 roomId 房间 → 出生在门 #doorIndex 位置"。
2. **渲染修正**：`renderDoorLinks()` 起点从 `getConnectionPointPosition(fromRoom, fromDir)` 改为 `getConnectionPointPosition(room, entryDir)`，虚线在同一个房间内部从入口边缘连到门元素。
3. **属性面板更新**：显示"所在房间"、"入口方向"、"门元素序号"三个字段，并新增含义说明文案。
4. **全链路字段名同步**：更新了 doorLinks.push（两处）、renderDoorLinks、showDoorLinkProperties、cleanupDoorLinksForConnection、deleteRoom、importJsonData 共 7 处引用。

### 目的：
doorLink 的正确语义是"从某方向进入此房间后的出生点位置"，虚线应在目标房间内部从入口连接点连到门元素，而不是跨房间从源房间连过来。

### 技术方案：
- 新字段 `roomId` = 门所在房间（即连接的目标房间 B）
- 新字段 `entryDir` = 入口方向（即 `toDir`，`getOppositeDirection(connectFromDir)`）
- 新字段 `doorIndex` = 房间 elements 数组中的门元素索引
- `renderDoorLinks` 中 `fromPos = getConnectionPointPosition(room, dl.entryDir)`，`toPos = room.x + doorEl.x, room.y + doorEl.y`
- `cleanupDoorLinksForConnection` 匹配条件改为 `dl.roomId === conn.to && dl.entryDir === conn.toDir`
- `deleteRoom` 清理条件简化为 `dl.roomId !== room.id`

---

---

## LevelDesigner 修复 doorLink 手动关联无法命中门元素 — 2026-02-26 12:38

### 修改文件：
- `Tools/LevelDesigner.html`

### 内容简述：
修复从连接点拖拽到另一个房间的门元素上无法建立 doorLink 的问题。

1. **根因**：`document.elementFromPoint` 只返回最顶层元素，SVG 层（`z-index: 5`）和临时连接线覆盖在门元素上方，导致鼠标释放时无法命中 18x18px 的门元素 div。
2. **修复方案**：将 `document.elementFromPoint`（单数）改为 `document.elementsFromPoint`（复数），穿透所有层级查找门元素。
3. **临时连接线屏蔽**：为 `.temp-connection-line` 添加 `pointer-events: none`，确保拖拽线不截获鼠标事件。
4. **高亮检测同步修复**：mousemove 中的门元素高亮检测也从 `elementFromPoint` 改为 `elementsFromPoint`，确保拖拽过程中门元素能正确高亮发光提示。

### 目的：
确保手动关联模式（从连接点拖拽到已有门元素上建立 doorLink）能正常工作。

### 技术方案：
- mouseup 中：`const allTargets = document.elementsFromPoint(e.clientX, e.clientY)`，用 `allTargets.find()` 分别查找 `doorTarget`（门元素优先）和 `target`（房间元素回退）
- mousemove 中：同样使用 `elementsFromPoint` 穿透查找门元素进行高亮
- CSS `.temp-connection-line` 新增 `pointer-events: none`

---

---

## LevelDesigner 全面代码审查与Bug修复 — 2026-02-26 12:46

### 修改文件：
- `Tools/LevelDesigner.html`

### 内容简述：
对 LevelDesigner.html 进行全面代码审查，修复4个Bug并清理2处冗余代码：

1. **Bug修复 — updateRoomProperty ID同步**：修改房间ID时未同步更新 `doorLinks` 数组中的 `roomId` 引用，导致重命名后 doorLink 虚线断裂。新增 `doorLinks.forEach(dl => { if (dl.roomId === oldId) dl.roomId = value; })` 同步逻辑。
2. **Bug修复 — updateRoomSize 缺少 renderDoorLinks**：通过属性面板手动调整房间尺寸后，连接线会更新但 doorLink 虚线不跟随更新。在 `renderConnections()` 后补充 `renderDoorLinks()` 调用。
3. **Bug修复 — deleteRoom doorLinks 清理不完整**：删除房间时直接过滤 connections，但未先调用 `cleanupDoorLinksForConnection` 清理关联的 doorLinks（那些因连接自动创建在目标房间中的 doorLinks）。改为先遍历相关 connections 调用 cleanup，再 filter 删除。
4. **Bug修复 — loadFromLocal 状态重置缺失**：从本地加载数据后未重置 `selectedRoom`、`selectedConnectionIndex`、`selectedDoorLinkIndex` 和属性面板显示状态，导致加载后可能出现残留选中状态。补充完整的状态重置逻辑。
5. **冗余清理 — getOppositeDirection**：该函数定义后未在任何地方被调用，直接删除。
6. **冗余清理 — Canvas 网格系统**：`<canvas id="grid-canvas">` 及关联的 `gridCanvas`、`ctx`、`drawGrid()`、`resizeCanvas()` 完全冗余——canvas 设置为 `display:none`，网格效果已由 CSS `background-image` 实现。删除全部 canvas 相关代码（HTML元素、CSS规则、JS变量和函数），`applyPan()` 中的 `drawGrid()` 调用替换为 `canvasArea.style.backgroundPosition` 更新以实现网格跟随平移。同步清理 click 事件中对已删除 `gridCanvas` 变量的引用。

### 目的：
确保 LevelDesigner 工具代码逻辑正确、无隐藏Bug、无冗余代码，提升可维护性。

### 技术方案：
- `updateRoomProperty` 的 `id` 分支新增 doorLinks roomId 同步
- `updateRoomSize` 末尾追加 `renderDoorLinks()` 调用
- `deleteRoom` 开头先收集 relatedConns 并逐个 `cleanupDoorLinksForConnection`，再 filter 删除 connections
- `loadFromLocal` 在解析数据后、渲染前重置三个选中状态变量和属性面板 display
- 删除 `getOppositeDirection` 函数定义（4行）
- 删除 canvas 相关代码约40行（`gridCanvas`/`ctx` 变量、`resizeCanvas`/`drawGrid` 函数、`window.resize` 监听、HTML `<canvas>` 元素、CSS `#grid-canvas` 规则）
- `applyPan` 中 `drawGrid()` 替换为 `canvasArea.style.backgroundPosition = \`${panX}px ${panY}px\``

---

---

## LevelDesigner 房间元素选中与删除功能 — 2026-02-26 13:04

### 修改文件：
- `Tools/LevelDesigner.html`

### 内容简述：
为 LevelDesigner 工具中的房间元素（spawn、door、enemy、chest、checkpoint、npc）添加选中和删除功能：

1. **元素选中状态管理**：新增 `selectedElementRoomId` 和 `selectedElementIndex` 全局变量，`clearElementSelection()` 清除函数。与 `selectedRoom`、`selectedConnectionIndex`、`selectedDoorLinkIndex` 三者互斥——选中任一类型自动取消其他类型的选中状态。
2. **点击选中交互**：在 mouseup 事件中区分元素拖拽与点击（位移 < 5px 判定为点击），点击时调用 `selectElement(roomId, index)` 选中元素。选中元素添加 `.element-selected` CSS 类呈现蓝色边框+发光高亮。
3. **属性面板联动**：`showElementProperties(roomId, elementIndex)` 在属性面板中显示元素类型（带 emoji）、所属房间、坐标位置、序号，并提供"🗑️ 删除元素"按钮。点击画布空白区域时清除选中并恢复默认面板。
4. **元素删除逻辑**：`deleteSelectedElement()` 从房间 `elements` 数组中 splice 移除元素。删除 door 类型元素时自动清理所有匹配的 doorLinks，并修正同房间内后续 doorLinks 的 `doorIndex` 索引偏移。
5. **多触发方式**：支持属性面板删除按钮和 Delete/Backspace 快捷键。键盘删除优先级：元素 → 连接线 → doorLink → 房间。
6. **边界情况**：deleteRoom 时若选中元素属于该房间则自动清除选中；clearAll 和 loadFromLocal 重置元素选中状态变量。

### 目的：
让关卡设计师能够选中查看房间内元素的属性，并通过多种方式删除不需要的元素，包括正确处理门元素删除时的 doorLink 联动清理。

### 技术方案：
- `selectedElementRoomId`（string|null）+ `selectedElementIndex`（int，-1 表示无选中）管理选中状态
- `.element-selected` CSS：`outline: 2px solid #2196F3; box-shadow: 0 0 8px rgba(33,150,243,0.6); transform: scale(1.15)`
- mouseup 中 `Math.hypot(dx, dy) < 5` 区分拖拽与点击
- `selectElement()` 内部调用所有其他 deselect 逻辑确保互斥
- `deleteSelectedElement()` 对 door 类型执行 `doorLinks.filter()` 清除 + `doorLinks.forEach()` 索引修正
- `selectRoom()`、`selectConnection()`、`selectDoorLink()` 入口处调用 `clearElementSelection()`
- keydown 处理中 `selectedElementIndex >= 0` 优先级最高

---

---

## LevelDesigner.html Bug 修复与功能完善 — 2026-02-26 13:32

### 修改文件：
- `Tools/LevelDesigner.html`

### 内容简述：
对 LevelDesigner 关卡编辑工具进行全面代码审查后，修复了 6 类 Bug 并清理了冗余代码：

1. **importJsonData 元素选中状态重置遗漏**：JSON 导入函数中未重置 `selectedElementRoomId` 和 `selectedElementIndex`，导致导入后可能出现幽灵选中引用。追加了这两个变量的重置以及 `hideConnectionProperties()` 调用。
2. **getExportData() 值拷贝问题**：`connections` 和 `doorLinks` 使用直接引用导出而非值拷贝。改为 `.map()` 创建独立的浅拷贝对象，确保导出数据不受后续编辑污染。
3. **元素位置显示像素 → 网格单位**：`showElementProperties` 中的坐标从 `Math.round(el.x)` 改为 `(el.x / GRID_SIZE).toFixed(1)`，标签更新为"位置(网格)"，与导出 JSON 中的单位一致。
4. **连接属性面板房间名称补全**：`showConnectionProperties` 中"从/到"字段从仅显示房间名改为 `名称 (ID)` 格式，方便快速识别连接关系。
5. **选中状态互斥 — 空白点击修复**：重构画布空白区域 click 事件处理，确保点击空白时：直接重置所有选中状态变量（room/connection/doorLink/element），最后调用 `hideConnectionProperties()` 统一恢复属性面板为默认提示文本。
6. **冗余代码清理**：移除未使用的 `.canvas-area.space-ready` CSS 类；清理 `renderConnections` 中无效的 `.connection-arrow` selector 引用。

### 目的：
消除全面审查中发现的 Bug，确保工具的数据导入导出健壮性、UI 信息一致性、选中状态互斥正确性，以及代码精简可维护。

### 技术方案：
- `importJsonData` 状态重置区域追加 `selectedElementRoomId = null; selectedElementIndex = -1; hideConnectionProperties();`
- `getExportData()` 中 `connections: connections.map(c => ({from, to, fromDir, toDir}))` 和 `doorLinks: doorLinks.map(d => ({roomId, entryDir, doorIndex}))`
- `showElementProperties` 坐标转换 `el.x / GRID_SIZE` 保留一位小数
- `showConnectionProperties` 中 `fromName = fromRoom ? \`${fromRoom.name} (${conn.from})\` : conn.from`
- 画布空白点击：避免调用 `deselectConnection/deselectDoorLink` 中间函数（它们内部会独立调用 hideConnectionProperties），改为直接变量赋值 + 最后统一调用 `hideConnectionProperties()`
- 移除 CSS `.canvas-area.space-ready { cursor: grab; }` 和 JS 中 `.connection-arrow` 无效引用

---

---

## HTML Scaffold JSON → LevelScaffoldData 导入工具 — 2026-02-26 14:12

**新建文件：**
- `Assets/Scripts/Level/Editor/HtmlScaffoldImporter.cs` — EditorWindow + 导入管线 + 嵌入式MiniJSON解析器

**内容简述：**
实现了完整的 HTML LevelDesigner JSON → Unity `LevelScaffoldData` ScriptableObject 一键导入脚本。

功能包括：
1. **EditorWindow UI**：菜单入口 `Window > ProjectArk > Import HTML Scaffold JSON`，文件选择（JSON输入 + .asset输出）、网格缩放因子
2. **JSON 解析**：内嵌轻量级递归下降 MiniJSON 解析器（因项目用 .NET Framework 配置级别，System.Text.Json 不可用），自动过滤 comment 对象
3. **房间映射**：HTML `id/name/type/floor/position/size` → `ScaffoldRoom`（RoomType枚举映射 + y轴翻转 + 缩放）
4. **元素映射**：`spawn→PlayerSpawn, enemy→EnemySpawn, checkpoint→Checkpoint, door→Door, chest→CrateWooden(占位), npc→Checkpoint(占位)`，坐标从房间左上角偏移转换为房间中心偏移
5. **连接转换**：全局 `connections[]` → 每个房间内嵌 `ScaffoldDoorConnection`，方向映射 + 房间边缘中点定位 + 跨楼层标记 `IsLayerTransition`
6. **doorLinks 绑定**：Door 元素的 `BoundConnectionID` 绑定到匹配方向的连接
7. **Floor 层级**：多楼层检测、主楼层自动选择、非主楼层房间名称追加 `[F=n]` 标记
8. **Undo 支持**：整个导入操作可 Ctrl+Z 撤销
9. **详尽日志**：Console 摘要（房间/连接/元素/跳过/占位映射/楼层分布）+ 成功对话框 + 自动选中创建的 asset

**目的：**
让策划可以从浏览器端 LevelDesigner.html 的可视化设计无缝导入 Unity，生成可直接用于 LevelArchitectWindow 和 LevelGenerator 的 ScaffoldData asset。

**技术方案：**
- 嵌入式 MiniJSON 递归下降解析器（Dictionary/List模式），兼容 Unity .NET Framework 配置
- SerializedObject 操作 private 字段（`_levelName`, `_floorLevel`）
- 所有公共 API 字段通过 public setter 设置
- 无任何第三方依赖，完全自包含

---

---

## HtmlScaffoldImporter Bug Fix: SerializedObject 覆盖 _rooms — 2026-02-26 14:57

**修改文件：**
- `Assets/Scripts/Level/Editor/HtmlScaffoldImporter.cs`

**内容简述：**
修复了导入 HTML Scaffold JSON 后生成的 .asset 文件中 `_rooms: []` 为空的关键 Bug。

**原因：**
`ExecuteImport()` 在 Step 2 创建了 `SerializedObject(scaffoldData)` 并设置 `_levelName`，此时 `_rooms` 为空。Step 3-5 通过 C# API（`AddRoom`/`AddConnection`）直接向底层对象添加了所有房间和连接。但 Step 6 调用 `so.ApplyModifiedPropertiesWithoutUndo()` 时，`SerializedObject` 将其初始快照（`_rooms = []`）写回对象，覆盖了所有已添加的房间数据。

**修复方案：**
在 `ApplyModifiedPropertiesWithoutUndo()` 之前插入 `so.Update()` 刷新 `SerializedObject` 的快照，使其同步底层对象的最新数据（包含所有房间）。然后重新设置 `_levelName` 和 `_floorLevel` 两个 SerializedProperty 字段，最后 Apply。

**目的：**
确保导入后的 .asset 文件包含完整的房间、连接和元素数据，可被 Level Architect 和 LevelGenerator 正常使用。

**技术方案：**
- `SerializedObject.Update()` → 重设 SerializedProperty → `ApplyModifiedPropertiesWithoutUndo()` 的正确三步序列化流程

---

---

## ScaffoldToSceneGenerator — 一键 Scaffold → 可玩关卡生成器 — 2026-02-26 15:30

### 新建文件

| 文件 | 目的 |
|------|------|
| `Assets/Scripts/Level/Editor/ScaffoldToSceneGenerator.cs` | 从 LevelScaffoldData 一键生成完整可玩关卡的 EditorWindow 工具 |

### 内容简述

实现了一个完整的 EditorWindow 工具 `ScaffoldToSceneGenerator`，通过菜单 `Window > ProjectArk > Generate Level From Scaffold` 打开。用户拖入 `LevelScaffoldData` 资产后点击 Generate 按钮，即可一键生成完整的场景关卡。

### 7 阶段生成管线

| 阶段 | 功能 | 关键实现 |
|------|------|----------|
| Phase 1 | Room GameObject 生成 | 遍历 scaffold.Rooms，每房间创建 GO + `Room` + `BoxCollider2D`(trigger) + `CameraConfiner` 子物体（Layer=IgnoreRaycast, `PolygonCollider2D` 匹配房间边界）。通过 `SerializedObject` 设置 `_confinerBounds` 和 `_playerLayer`。 |
| Phase 2 | RoomSO 资产创建与关联 | 为每个房间创建/更新 RoomSO（`Assets/_Data/Level/Rooms/{name}_Data.asset`），设置 `_roomID`、`_displayName`、`_floorLevel`、`_type`，赋值给 Room._data。 |
| Phase 3 | Element 实体化 | PlayerSpawn → 空 GO；EnemySpawn → 空 GO + 收集到列表；Checkpoint → GO + `Checkpoint` 组件 + `BoxCollider2D`(2×2) + `CheckpointSO` 资产；Door → 跳过（Phase 4）；其他 → 占位 GO。最后绑定 EnemySpawn 列表到 `Room._spawnPoints`。 |
| Phase 4 | Door 双向连接 | 用 `HashSet<string>` 去重连接对。每条 connection 创建正向门 + 反向门（各含 `Door` + `BoxCollider2D`(3×3) + SpawnPoint）。通过 `SerializedObject` 设置 `_targetRoom`、`_targetSpawnPoint`、`_isLayerTransition`、`_initialState`、`_playerLayer`。Door 元素的 `_boundConnectionID` 额外叠加 `DoorConfig`（`_initialState`、`_requiredKeyID`、`_openDuringPhases`）。 |
| Phase 5 | Arena/Boss 战斗配置 | Arena/Boss 房间添加 `ArenaController` + `EnemySpawner` 子物体。创建 `EncounterSO`（Arena: 1波×3敌人；Boss: 2波 2+3敌人 delay=1.5s），赋值给 `RoomSO._encounter`。 |
| Phase 6 | Normal 房间战斗 | Normal 房间如有 EnemySpawn → 创建 `EnemySpawner`（不加 ArenaController） + EncounterSO（1波×2敌人），赋值给 RoomSO。无 EnemySpawn 则跳过。 |
| Phase 7 | 验证报告 | Console 输出统计（房间/门/RoomSO/EncounterSO/CheckpointSO 数量）、Arena/Boss 缺 EnemySpawn 警告、6项 TODO checklist。 |

### 技术方案

- **Code-First 模式**：不依赖任何外部 Prefab 或 LevelElementLibrary，所有 Component 通过 `AddComponent<T>()` + `SerializedObject` 配置私有字段
- **Undo 全量支持**：`Undo.SetCurrentGroupName` + try/catch/finally 包裹，异常时 `RevertAllDownToGroup`，完成后 `CollapseUndoOperations`，Ctrl+Z 一键撤销全部
- **双向门去重**：canonical pair key（`min(A,B)|max(A,B)`）+ HashSet 避免重复创建门
- **DoorConfig 叠加**：Phase 4 先基于 connections 生成所有门（默认 Open），再在 `ApplyDoorConfigFromElements` 中根据 element._boundConnectionID 查找对应 Door 覆盖 DoorConfig 字段
- **默认敌人 Prefab**：从 `Assets/_Prefabs/Enemies/Enemy_Rusher.prefab` 加载，不存在则 Warning + null
- **Player Layer 安全**：`LayerMask.GetMask("Player")` 若返回 0 则 Warning 但不中断

### 目的

让关卡设计师从 HTML Scaffold JSON → LevelScaffoldData → 完整可玩关卡，实现全自动化管线。生成的关卡包含完整的房间结构、双向门连接、ArenaController 战斗编排、EnemySpawner 刷怪配置、CheckpointSO 存档点，只需后续补充 Tilemap 绘制和敌人 Prefab 替换即可进入 Play Mode。

---

---

## ScaffoldToSceneGenerator — Room Layer 修复 — 2026-02-26 17:01

### 修改文件

| 文件 | 修改内容 |
|------|----------|
| `Assets/Scripts/Level/Editor/ScaffoldToSceneGenerator.cs` | `CreateRoomGameObject` 中为 Room GO 设置 Layer = `RoomBounds` |

### 问题

生成的 Room GameObject 使用默认 Layer（Default），导致飞船被卡在房间外无法进入。

### 修复

在 `CreateRoomGameObject` 中，创建 `roomGO` 后立即设置 Layer：

```csharp
int roomBoundsLayer = LayerMask.NameToLayer("RoomBounds");
if (roomBoundsLayer >= 0)
    roomGO.layer = roomBoundsLayer;
else
    Debug.LogWarning(...); // 提示用户在 Project Settings 中添加 RoomBounds Layer
```

### 前提

项目 Tags and Layers 中必须存在名为 **`RoomBounds`** 的 Layer，否则生成时会输出 Warning 并回退到 Default Layer。

---

---

## BugFix: ScaffoldToSceneGenerator CameraConfiner 实体碰撞阻挡飞船 — 2026-02-26 22:42

### 修改文件

- `Assets/Scripts/Level/Editor/ScaffoldToSceneGenerator.cs`

### 问题描述

通过 `ScaffoldToSceneGenerator` 生成的关卡（示巴星 ACT1+ACT2）中，飞船进入任意 Room 时被卡在房间边缘，无法正常进入。旧版 `ShebaLevelScaffolder` 生成的 Sheba Level 正常。

### 根因

`CreateRoomGameObject` 方法中，`CameraConfiner` 子对象的 `PolygonCollider2D` 被设置为 `isTrigger = false`（实体碰撞体）。`Ignore Raycast`（Layer 2）只忽略射线检测，不忽略 Physics2D 碰撞，导致该实体 PolygonCollider2D 像一堵墙一样包围整个房间，阻止飞船进入。

### 修复

将 `CreateRoomGameObject` 中 `PolygonCollider2D` 的 `isTrigger` 从 `false` 改为 `true`：

```csharp
// Before
polyCol.isTrigger = false;

// After
polyCol.isTrigger = true;
```

### 影响范围

仅影响 `ScaffoldToSceneGenerator` 生成的新关卡。已生成的场景需手动将各 Room 下 `CameraConfiner/Po
lygonCollider2D` 的 Is Trigger 勾选为 true，或重新生成关卡。

---

---

## Feature: ScaffoldToSceneGenerator 元素可视化 Gizmo — 2026-02-26 23:04

### 修改文件

- `Assets/Scripts/Level/Editor/ScaffoldToSceneGenerator.cs`

### 内容简述

为 `ScaffoldToSceneGenerator` 生成的每个房间元素 GO 添加编辑期可视化：彩色 `SpriteRenderer` 色块 + `TextMesh` 文字标签，方便关卡设计师在 Scene 视图中直观识别各元素类型和位置。

### 目的

当前生成的元素 GO（`PlayerSpawn`、`EnemySpawn`、`Wall` 等）在 Scene 视图中是空 GameObject，无任何视觉表示，开发者无法直观区分。

### 技术方案

1. **新增 `GetElementGizmoColor(ScaffoldElementType)`**：静态辅助方法，按元素类型返回对应 `Color`（黄/红/绿/灰/深灰/橙/蓝灰/紫/青，其余白色）。

2. **新增 `AddGizmoVisuals(GameObject, string, Color)`**：通用辅助方法，在目标 GO 上：
   - 添加 `SpriteRenderer`，使用 `Resources.GetBuiltinResource<Sprite>("Sprites/Default")` 内置白色方块 Sprite，设置颜色与 `sortingOrder = 1`
   - 创建子 GO `"Label"`，添加 `TextMesh`，设置文字内容、`fontSize=12`、`anchor=MiddleCenter`、`alignment=Center`、白色字体、`localPosition=(0,0.6,0)`、`localScale=(0.1,0.1,0.1)`，`MeshRenderer.sortingOrder = 2`

3. **修改 `CreateElementGO()`**：增加可选参数 `ScaffoldElementType type` 和 `string labelOverride`，方法末尾调用 `AddGizmoVisuals()`；更新 Phase 3 中所有调用处传入对应 `elem.ElementType`。

4. **Phase 4 Door/SpawnPoint 可视化**：在 `GenerateDoors()` 中，对 `Door_to_XXX` GO 添加青色可视化，对 `SpawnPoint_from_XXX` GO 添加浅蓝色可视化。

5. **TODO Checklist 提示**：在 `Generate()` 末尾的 Console 输出中追加提示，提醒开发者发布前隐藏/删除 Gizmo 可视化组件。

---

---

## ScaffoldToSceneGenerator — Door/SpawnPoint Gizmo 尺寸匹配碰撞体 — 2026-02-26 23:18

**修改文件：**
- `Assets/Scripts/Level/Editor/ScaffoldToSceneGenerator.cs`

**内容简述：**
为 `AddGizmoVisuals()` 增加可选 `Vector2 size` 参数，使 SpriteRenderer 色块的世界空间大小可配置，并在 Door 和 SpawnPoint 的调用处传入对应尺寸。

**目的：**
让开发者在 Scene 视图中能直观看到 Door 触发区域的实际范围（3×3 单位），知道飞船何时进入了传送检测区域。

**技术方案：**
1. `AddGizmoVisuals(go, label, color, size)` 新增 `size` 参数（默认 `Vector2.one`），通过设置 `go.transform.localScale = (size.x, size.y, 1)` 让 SpriteRenderer 色块与碰撞体等大。
2. Label 子 GO 的 `localPosition.y` 和 `localScale` 做反向补偿（除以 parent scale），保证文字大小不随色块缩放而变形。
3. Door GO 调用时传入 `new Vector2(3f, 3f)`，与 `BoxCollider2D.size = (3,3)` 完全对齐。
4. SpawnPoint GO 调用时传入 `new Vector2(1.5f, 1.5f)`，提供清晰的落点可视范围。

---

---

## ScaffoldToSceneGenerator — 所有元素 Gizmo Sprite 匹配碰撞体大小 — 2026-02-26 23:24

**修改文件：**
- `Assets/Scripts/Level/Editor/ScaffoldToSceneGenerator.cs`

**内容简述：**
新增 `GetElementGizmoSize()` 辅助方法，统一管理各元素类型的 Gizmo sprite 尺寸；修复 `CreateElementGO` 和 `CreateCheckpointElement` 未传 size 导致 sprite 始终为 1×1 的问题。

**目的：**
让一键生成的所有元素（Door、Checkpoint、PlayerSpawn、EnemySpawn 等）的 SpriteRenderer 色块在 Scene 视图中与其 BoxCollider2D 触发区域完全重叠，方便开发者直观判断检测范围。

**技术方案：**
新增 `GetElementGizmoSize(ScaffoldElementType)` 方法，返回各类型对应的 `Vector2` 尺寸：
- `Door` → `(3, 3)`，匹配 `BoxCollider2D.size = (3,3)`
- `Checkpoint` → `(2, 2)`，匹配 `BoxCollider2D.size = (2,2)`
- `PlayerSpawn` / `EnemySpawn` → `(1.5, 1.5)`，无碰撞体但提供清晰落点范围
- 其余 → `(1, 1)` 默认

`CreateElementGO` 调用 `AddGizmoVisuals` 时自动传入 `GetElementGizmoSize(type)`；`CreateCheckpointElement` 也在添加 BoxCollider2D 后立即调用 `AddGizmoVisuals` 并传入 `(2,2)`。

---

---

## LevelDesigner.html 效率升级（7大功能） — 2026-02-27 00:31

**修改文件：**
- `Tools/LevelDesigner.html`

**内容简述：**
对浏览器端关卡可视化编辑工具进行全面效率升级，新增7项功能，覆盖数据扩展、类型扩展、快捷键、可视化辅助和向后兼容修复。

**目的：**
核心诉求：方便、快捷、尽量一步到位，减少整个制作关卡流程中琐碎的步骤。让设计师在 LevelDesigner.html 中完成更多配置，减少在 Unity 中手动补充的工作量。

**技术方案：**

1. **房间属性扩展（任务1）**：`createRoom()` 新增 `zoneId`、`act`、`tension`、`beatName`、`timeRange` 字段；属性面板新增对应输入控件（Zone ID 文本框、ACT 下拉 ACT1-ACT5、张力滑块 1-10、叙事乐章文本框、时长估算文本框）；`getExportData()` 仅在字段非空时输出对应 key，保持 JSON 简洁。

2. **类型扩展（任务2）**：新增房间类型 `narrative`（蓝紫色）、`puzzle`（青色）、`corridor`（灰色）；新增元素类型 `tidal_door`（潮汐门）、`resonance_crystal`（共鸣水晶）、`lore_audio`（Lore 音频日志）、`black_water`（黑水区域）；更新 `getTypeLabel()` 和 `getElementIcon()` 函数及对应 CSS 样式。

3. **快捷键支持（任务3）**：实现 `undoStack`（最大50条）+ `pushUndoState()` 快照机制，在所有状态变更前保存快照；`undo()` 恢复快照并重新渲染；`duplicateRoom()` 深拷贝选中房间（含所有属性和元素，不含连接关系），ID 自动添加 `_copy` 后缀；`keydown` 事件支持 `Ctrl+Z`/`Ctrl+D`/`Ctrl+S`/`Escape`，焦点在输入框时不触发。

4. **ACT 分组框（任务4）**：`ACT_COLORS` 常量定义5个 ACT 的半透明颜色；`renderActGroups()` 按 ACT 分组计算包围盒（+20px padding），动态创建 `.act-group-box` div（z-index 低于房间），左上角显示 ACT 标签；房间数 >50 时节流（16ms）；在拖拽移动、属性修改、导入、撤销、创建/删除房间后均调用。

5. **心流张力折线图（任务5）**：右侧面板新增 `<canvas>` 元素（高度80px）；`renderTensionChart()` 收集有 `tension > 0` 的房间，按 Zone ID 字母数字排序（Z1a < Z1b < Z2a），绘制折线图；节点显示 Zone ID 标签和张力数值；相邻节点张力差 >4 时连线高亮为橙色；每次 `updateAsciiPreview()` 末尾自动调用。

6. **连接线语义类型（任务6）**：`connections` 对象新增 `connectionType` 字段（默认 `normal`）；选中连接线时属性面板显示类型下拉（`normal`/`tidal`/`locked`/`one_way`）；`renderConnections()` 根据类型选择对应 SVG stroke 颜色（tidal=蓝绿、locked=黄色、one_way=橙色）；`getExportData()` 输出 `connectionType` 字段。

7. **向后兼容修复（任务7）**：`importJsonData()` 补充重置 `selectedElementRoomId`/`selectedElementIndex`；导入时为旧格式 JSON 的 room 补全新字段默认值（`zoneId: ''`、`act: ''`、`tension: 0` 等）；connections 导入时补全 `connectionType: 'normal'`；`getExportData()` 中 connections/doorLinks 改为值拷贝（`.map()` 展开字段）。

---

---

## ScaffoldToSceneGenerator — Asset 按 LevelName 分文件夹存放 — 2026-02-27 00:47

**修改文件：**
- `Assets/Scripts/Level/Editor/ScaffoldToSceneGenerator.cs`

**内容简述：**
移除三个固定常量目录（`ROOM_DATA_DIR`/`ENCOUNTER_DIR`/`CHECKPOINT_DIR`），改为在 `Generate()` 开始时根据 `_scaffold.LevelName` 动态计算每次生成的专属子文件夹路径，并存入实例字段供各阶段方法使用。

**目的：**
每次生成不同关卡时，所有产出的 RoomSO、EncounterSO、CheckpointSO 资产都自动归入 `Assets/_Data/Level/{LevelName}/` 下的对应子目录，避免多关卡资产混放在同一扁平目录中造成混乱，方便管理和版本控制。

**技术方案：**
- 删除 `ROOM_DATA_DIR`、`ENCOUNTER_DIR`、`CHECKPOINT_DIR` 三个 `const string` 常量
- 新增 `LEVEL_DATA_ROOT = "Assets/_Data/Level"` 根目录常量
- 新增三个实例字段 `_roomDataDir`、`_encounterDir`、`_checkpointDir`
- 在 `Generate()` try 块最开始，用 `SanitizeName(_scaffold.LevelName)` 计算安全名称，拼出三条路径并调用 `EnsureDirectory()` 确保目录存在
- `CreateOrUpdateRoomSO()`、`CreateCheckpointElement()`、`SetupArenaBossCombat()`、`SetupNormalRoomCombat()` 中的路径字符串均改为引用对应实例字段

生成结果示例（LevelName = "示巴星_Z1"）：
```
Assets/_Data/Level/示巴星_Z1/
  Rooms/       ← RoomSO assets
  Encounters/  ← EncounterSO assets
  Checkpoints/ ← CheckpointSO assets
```

---

---

## ScaffoldToSceneGenerator — 效率升级（需求2-8）— 2026-02-27 01:15

**修改文件：**
- `Assets/Scripts/Level/Editor/ScaffoldToSceneGenerator.cs`

**内容简述：**
对 ScaffoldToSceneGenerator 进行 6 项效率升级，消灭生成后仍需手动完成的琐碎步骤。

**目的：**
让关卡设计师在一键生成后能直接进入绘制/调试阶段，减少重复手动操作。

**技术方案：**

1. **需求8 — SanitizeName 空格处理 + 路径预览**
   - `SanitizeName()` 追加 `.Replace(" ", "_")` 处理空格字符
   - `OnGUI()` 在 Scaffold Data 字段下方用 `EditorGUILayout.HelpBox` 显示 `Output: Assets/_Data/Level/{SanitizedName}/`，`_scaffold` 为 null 时不显示

2. **需求2 — 房间尺寸 Fallback 与异常警告**
   - 新增 `GetFallbackSize(RoomType)` 方法，按类型返回默认尺寸（Normal=20×15，Arena=30×20，Boss=40×30，Corridor=20×8，Shop=15×12）
   - `Generate()` 遍历房间时检测 `Size.x <= 0 || Size.y <= 0`，自动 fallback 并输出 `Debug.LogWarning`
   - 生成报告 TODO Checklist 中列出所有使用了 fallback 的房间名

3. **需求3 — Door 位置自动推算（边缘吸附）**
   - 新增 `ResolveDoorPosition(ScaffoldDoorConnection, Vector2)` 方法，根据 `DoorDirection` 返回房间边缘局部坐标
   - `DoorPosition == Vector3.zero` 时自动调用；已有非零值则直接使用不覆盖
   - `DoorDirection` 为 None 时保留原 fallback 并输出 Warning

4. **需求4 — 自动创建标准 Tilemap 层级**
   - 新增 `CreateTilemapHierarchy(GameObject)` 方法，在房间 GO 下创建 `Tilemaps` 子对象
   - 自动创建三层：`Tilemap_Ground`（sortingOrder=0）、`Tilemap_Wall`（sortingOrder=1）、`Tilemap_Decoration`（sortingOrder=2），每层附加 `Tilemap` + `TilemapRenderer` 组件
   - `CreateRoomGO()` 末尾自动调用；生成报告提示"直接选中对应层开始绘制"

5. **需求5 — EncounterSO 按 EnemyTypeID 自动填充敌人 Prefab**
   - `ScaffoldElement` 新增 `EnemyTypeID`（string）字段
   - `CreateEncounterSO()` 收集房间内所有 EnemySpawn 的 `EnemyTypeID`，从 `Assets/_Prefabs/Enemies/{EnemyTypeID}.prefab` 加载 Prefab，找不到则 fallback 到 `Enemy_Rusher.prefab` 并输出 Warning
   - 同一 Wave 中为每种类型创建独立 Entry；生成报告列出每个房间的敌人类型汇总

6. **需求6 — 增量更新模式（Update Existing 复选框）**
   - EditorWindow 新增 `_updateExisting` bool 字段，Generate 按钮旁渲染 Toggle
   - `Update Existing = true` 时：按 DisplayName 查找已存在的同名 GO，存在则跳过重建，仅更新 RoomSO 引用、BoxCollider2D.size、Door 组件属性，保留 Tilemap 子层级
   - 增量更新完成后 Console 输出被保留的房间名称列表

7. **需求7 — Gizmo 统一开关（Toggle Gizmos 按钮）**
   - EditorWindow 新增 `_gizmosVisible` bool 字段（默认 true），Generate 按钮下方渲染 Toggle Gizmos 按钮
   - 新增 `ToggleGizmos()` 方法：遍历场景中所有 `SpriteRenderer`（sortingOrder==1）和名为 `Label` 的 `MeshRenderer`，统一设置 `enabled`
   - 按钮文字根据状态动态显示 `Hide Gizmos` / `Show Gizmos`；场景中无 Gizmo 对象时输出提示

---

---

## BugFix: DialogueLine 序列化深度超限修复 — 2026-02-27 09:07

**修改文件：**
- `Assets/Scripts/SpaceLife/Data/DialogueData.cs`
- `Assets/Scripts/SpaceLife/Data/NPCDataSO.cs`
- `Assets/Scripts/SpaceLife/NPCController.cs`
- `Assets/Scripts/SpaceLife/DialogueUI.cs`

**问题描述：**
Unity 报错 `Serialization depth limit 10 exceeded at DialogueLine._speakerAvatar`，原因是 `DialogueLine` 内联包含 `List<DialogueOption>`，`DialogueOption` 又内联包含 `DialogueLine`，形成无限递归序列化循环。

**技术方案：**
1. **DialogueData.cs**：`DialogueOption` 移除 `DialogueLine _nextLine` 字段，改为 `int _nextLineIndex`（-1 表示结束对话），彻底打破序列化循环
2. **NPCDataSO.cs**：将原三个独立 `List<DialogueLine>`（default/friendly/bestFriend）改为统一的 `List<DialogueLine> _dialogueNodes` 节点池，加入三个入口 index 字段（`_defaultEntryIndex`/`_friendlyEntryIndex`/`_bestFriendEntryIndex`），提供 `GetEntryLine(int relationship)` 和 `GetNodeAt(int index)` 辅助方法
4. **NPCController.cs**：`GetAppropriateDialogue` 改为调用 `_npcData.GetEntryLine(CurrentRelationship)`，移除冗余的 `using System.Linq`
4. **DialogueUI.cs**：`ShowDialogue` 缓存 `_currentNPCData`，`OnOptionSelected` 通过 `_currentNPCData.GetNodeAt(option.NextLineIndex)` 查找下一行，`CloseDialogue` 清空 `_currentNPCData`

---

---

## BugFix: Door 碰撞体过大 + 反复横跳修复 — 2026-02-27 09:19

**修改文件：**
- `Assets/Scripts/Level/Editor/ScaffoldToSceneGenerator.cs`
- `Assets/Scripts/Level/Room/Door.cs`

**问题描述：**
ScaffoldToSceneGenerator 生成的 Door BoxCollider2D 尺寸为 3×3，对于走廊等小房间几乎占满整个通道，导致玩家进入房间后立刻触发传送。同时，传送落点若恰好在目标房间反向门的碰撞体内，`OnTriggerStay2D` 会立刻再次触发反向传送，造成反复横跳。

**技术方案：**
1. **ScaffoldToSceneGenerator.cs**：将所有 Door 的 `BoxCollider2D.size` 从 `(3f, 3f)` 缩小至 `(1.5f, 1.5f)`，同步更新 Gizmo 可视化尺寸和 `GetDefaultGizmoSize` 中的 Door 默认值（共 5 处）
2. **Door.cs**：新增 `static float _globalTransitionCooldownUntil` 静态冷却时间戳，所有 Door 实例共享；`TryTransition` 的回调中设置 `_globalTransitionCooldownUntil = Time.unscaledTime + 1f`；`OnTriggerStay2D` 中加入冷却检查，冷却期间跳过 Stay 触发，防止目标房间反向门立刻反向传送玩家

---

---

## LevelDesigner — 自定义元素功能 — 2026-02-27 09:30

**修改文件：**
- `Tools/LevelDesigner.html`

**内容简述：**
在左侧面板新增「自定义元素」区块，允许设计师自由定义名称和颜色，创建自定义元素预设并拖入画布使用，支持 localStorage 持久化、属性面板编辑、JSON 导出/导入向后兼容。

**目的：**
满足设计师标注项目特有自定义标记点（特殊触发器、剧情锚点、测试标记等）的需求，无需修改代码即可扩展元素类型。

**技术方案：**
1. **数据结构**：新增 `customElementPresets` 数组（每项含 `label`、`color`），通过 `saveCustomPresets()` / `loadCustomPresets()` 与 `localStorage` 同步（key: `leveldesigner_custom_elements`）
2. **HTML/CSS**：左侧面板底部新增区块，含名称输入框、颜色选择器（`<input type="color">`）、添加按钮；自定义预设项样式使用动态颜色（左侧色条 + 半透明背景），悬停显示 × 删除按钮
3. **拖拽**：自定义预设项通过 `dataTransfer` 携带 `elementType=custom`、`customLabel`、`customColor`；`addElementToRoom` 扩展参数接收并存入元素对象
4. **渲染**：`renderRoom` 中 `type === 'custom'` 时使用 `customColor` 作为背景色，显示截断后的 `customLabel`（超3字符加省略号），CSS 类 `.element-custom` 使用圆角矩形区别于内置圆形图标
5. **属性面板**：`showElementProperties` 对 custom 类型额外渲染名称输入框和颜色选择器，`updateCustomElementField()` 实时更新元素数据并刷新画布图标
6. **JSON 兼容**：`getExportData` 对 custom 元素输出 `customLabel`/`customColor` 字段；`importJsonData` 反序列化时向后兼容（旧数据无 custom 字段时不报错）
7. **辅助函数**：新增 `hexToRgba(hex, alpha)` 将十六进制颜色转 rgba，`escapeHtml(str)` 防 XSS

---

---

## Bug Fix: EnemySpawner 一键生成后不产出敌人 — 2026-02-27 10:30

**修改文件：**
- `Assets/Scripts/Level/Editor/ScaffoldToSceneGenerator.cs`
- `Assets/Scripts/Level/Room/RoomManager.cs`

**问题描述：**
一键生成关卡后进入 Play Mode，所有房间（Arena/Boss/Normal）的 EnemySpawner 均不产出敌人。

**根本原因（双重 Bug）：**

**Bug 1（主因）：EncounterSO `_waves` 数组为空**
`CreateEncounterSO` 和 `CreateEncounterSO_Normal` 方法在 `AssetDatabase.CreateAsset()` 创建新资产后，通过 `SerializedObject` 写入 `_waves` 数据，但只调用了 `ApplyModifiedPropertiesWithoutUndo()` 而**没有调用 `EditorUtility.SetDirty(so)`**。导致修改只存在于内存中，`AssetDatabase.SaveAssets()` 时无法识别该资产为脏，数据不写入磁盘，最终 `.asset` 文件中 `_waves: []`。
- `HasEncounter` 返回 `false`（`WaveCount == 0`），`ArenaController.BeginEncounter()` 直接 return
- `Room.ActivateEnemies()` 虽然调用了 `StartWaveEncounter()`，但 `WaveSpawnStrategy` 因 `WaveCount == 0` 立刻触发 `OnEncounterComplete`，房间被标记为 Cleared

**Bug 2（次因）：Arena/Boss 房间双重启动策略冲突**
`RoomManager.EnterRoom()` 对 Arena/Boss 房间同时调用了 `_currentRoom.ActivateEnemies()`（创建策略 A 并立即 StartStrategy）和 `arena.BeginEncounter()`（调用 `SetStrategy(B)`，内部先 `Reset()` 策略 A，CancellationToken 被取消）。策略 A 的异步任务被取消，敌人消失。

**技术方案：**
1. **ScaffoldToSceneGenerator.cs**：在 `CreateEncounterSO` 和 `CreateEncounterSO_Normal` 的 `ApplyModifiedPropertiesWithoutUndo()` 后添加 `EditorUtility.SetDirty(so)`，确保 `_waves` 数据在 `SaveAssets()` 时正确持久化
2. **RoomManager.cs**：将 `ActivateEnemies()` 移入 `else` 分支，Arena/Boss 房间完全交由 `ArenaController.BeginEncounter()` 管理，不再提前启动策略

**操作说明：**
修复后需重新一键生成关卡（旧的 `_waves: []` 资产需重新生成才能获得正确数据）。

---

---

## Bug Fix: LevelDesigner.html 拖拽功能全面修复 — 2026-02-27 11:20

**修改文件：**
- `Tools/LevelDesigner.html`

**问题描述：**
LevelDesigner.html 中所有拖拽操作完全失效——无论是从左侧面板拖拽房间类型到画布，还是拖拽元素到房间内，均无法触发 drop 事件。

**根本原因（3 个 Bug）：**

**Bug 1（主因）：`.room` 元素缺少 `dragover` 的 `preventDefault()`**
HTML5 Drag & Drop API 要求 drop 目标的所有祖先元素在 `dragover` 事件中调用 `e.preventDefault()`，否则浏览器认为该区域不接受 drop。`canvasArea` 和 `world` 已有 `dragover` 监听，但 `.room` 元素（`world` 的子元素）没有。当鼠标拖拽到 `.room` 上方时，`.room` 成为最顶层命中元素，其默认行为阻止了 drop，导致 `canvasArea` 的 `drop` 事件不触发。

**Bug 2（次因）：`mousedown` 注释不清晰，`e.preventDefault()` 位置需说明**
`setupRoomEvents` 中 `mousedown` 末尾的 `e.preventDefault()` 仅在房间拖动路径执行（其他分支均提前 return），不影响 HTML5 drag & drop 流程，但缺少注释说明，容易误判为干扰源。

**Bug 3（精度问题）：`drop` 事件中目标房间检测依赖坐标范围计算**
当拖拽图标有偏移时，world-space 坐标可能偏移，导致 `rooms.find()` 找不到正确的目标房间。

**技术方案：**
1. **`setupRoomEvents` 添加 `dragover` 监听器**：在函数开头为 `div` 添加 `div.addEventListener('dragover', (e) => { e.preventDefault(); })`，确保整条冒泡链路（`.room` → `world` → `canvasArea`）全部允许 drop
2. **`mousedown` 添加注释**：明确说明 `e.preventDefault()` 仅用于防止文本选中，不调用 `stopPropagation`，不干扰 HTML5 drag 事件冒泡
3. **`drop` 事件精确命中检测**：将目标房间检测替换为 `document.elementFromPoint(e.clientX, e.clientY)` + `closest('.room')` 精确命中，坐标范围计算作为回退方案

---

---

## Bug Fix: LevelDesigner.html 拖拽功能修复 v3（#connections-svg 拦截 Drag 事件）— 2026-02-27 11:42

**修改文件：**
- `Tools/LevelDesigner.html`

**问题描述：**
上轮修复后拖拽仍然失效。从左侧面板拖拽房间类型/元素到画布，drop 事件依然不触发。

**根本原因：**
`#connections-svg`（`position: absolute; width: 100%; height: 100%; z-index: 5`）覆盖了整个画布区域，成为浏览器 drag 命中测试的最顶层元素。

关键误解：CSS `pointer-events: none` **只对鼠标事件（click/mousemove 等）有效，对 HTML5 Drag & Drop 事件（dragover/drop）完全无效**。

因此拖拽链路断裂：
1. 浏览器 drag 命中 → `#connections-svg`（z-index:5）
2. `#connections-svg` 没有 `dragover` 的 `e.preventDefault()`
3. 浏览器认为该区域不接受 drop → `canvasArea` 的 `drop` 事件永远不触发

历史上 `gridCanvas` 存在时，`gridCanvas` 是顶层元素，它通过 `canvasArea` 的冒泡获得了 `preventDefault()`，所以拖拽正常。删除 `gridCanvas` 后，SVG 成为顶层，问题暴露。

**技术方案：**
在 `canvasArea` 和 `world` 的 `dragover` 监听器旁边，添加一行：
```js
svg.addEventListener('dragover', (e) => { e.preventDefault(); });
```

**完整 dragover 链路（修复后）：**
`preset-item` → `#connections-svg`（新增）→ `#world` → `.room` → `canvasArea`

**验收：**
- `canvasArea`（第 1371 行）✅
- `world`（第 1372 行）✅
- `svg`（第 1373 行）✅ 新增
- `.room` in `setupRoomEvents`（第 1502 行）✅

---

---

## LevelDesigner Bug修复：点击空白处未取消房间选中状态（回归修复） — 2026-02-27 12:49

### 修改文件：
- `Tools/LevelDesigner.html`

### 内容简述：
修复了点击画布空白区域后，已选中房间的连接点（connection points）仍然显示的问题（回归 bug）。

### 目的：
连接点应仅在房间被选中（selected）或鼠标悬浮（hover）时显示，点击空白处取消选中后连接点应隐藏。

### 技术方案：
- **根因**：`canvasArea` 的 click handler 使用**白名单模式**检查 `e.target`（仅匹配 `canvasArea`、`svg`、`world` 三个元素）。但 `#world` 的 CSS 为 `width: 0; height: 0`（作为 transform 定位容器），导致其永远不会成为 `e.target`。此外，SVG 内部子元素（如 hitarea 线条附近区域、marker polygon 边缘）也可能成为 `e.target`，无法被白名单覆盖。
- **修复**：改用**黑名单模式**——使用 `e.target.closest('.room')` 检查点击是否在房间内部。只要点击目标不在 `.room`、`.toolbar`、`.instructions` 内，就执行取消选中逻辑。这种方式更健壮，不受 DOM 结构变化影响。

---

---

## LevelDesigner 优化：DoorLink虚线指向门元素icon正中心 — 2026-02-27 13:30

### 修改文件：
- `Tools/LevelDesigner.html`

### 内容简述：
修复了连接点（connection point）到门元素（door element）之间的虚线（doorLink dashed line）终点未指向门元素icon正中心的问题。

### 目的：
提升视觉美观度，让虚线精确地指向门元素icon的中心而非其左上角。

### 技术方案：
- **根因**：`renderDoorLinks` 函数中，虚线终点坐标 `(toX, toY)` 使用 `room.x + doorEl.x` / `room.y + doorEl.y`，这对应门元素 div 的**左上角**而非视觉中心。
- **修复**：加上元素尺寸的中心偏移。`.room-element` CSS 为 `width: 18px; height: 18px; border: 2px`，总显示尺寸 22×22px，中心偏移为 11px。在 `toX` 和 `toY` 上各加 `ELEMENT_CENTER_OFFSET = 11`。

---

---

## 门元素位置 → SpawnPoint 管线打通 — 2026-02-27 15:14

### 新建文件：
- 无

### 修改文件：
- `Assets/Scripts/Level/Data/LevelScaffoldData.cs`
- `Tools/LevelDesigner.html`
- `Assets/Scripts/Level/Editor/HtmlScaffoldImporter.cs`
- `Assets/Scripts/Level/Editor/ScaffoldToSceneGenerator.cs`

### 内容简述：
实现了门元素位置自动导出为 SpawnOffset，并在一键生成场景时将其应用为玩家出生点（SpawnPoint）位置的完整管线。

### 目的：
此前 SpawnPoint 始终生成在房间边缘中点（`DoorPosition`），不受 LevelDesigner 中门元素实际位置影响。本次修改使 LevelDesigner 中精心摆放的门元素位置能直接决定穿过该门后的出生位置，实现"设计即所得"。

### 技术方案：
1. **`LevelScaffoldData.cs` — `ScaffoldDoorConnection` 新增字段**：添加 `[SerializeField] private Vector3 _spawnOffset` 和公开属性 `SpawnOffset`（get/set）。默认值 `Vector3.zero`，确保旧 .asset 文件向后兼容。
2. **`LevelDesigner.html` — 导出 doorLink 附带 spawnOffset**：修改 `getExportData()` 中 `doorLinks.map()` 逻辑，通过 `doorLink.doorIndex` 在对应房间的 `elements[]` 中查找门元素，将其 `[x / GRID_SIZE, y / GRID_SIZE]` 写入 `spawnOffset` 字段。若门元素不存在或 doorIndex 无效则不包含该字段。
3. **`HtmlScaffoldImporter.cs` — 解析 spawnOffset 并坐标转换**：
   - `HtmlDoorLink` 类新增 `float[] SpawnOffset` 字段。
   - JSON 解析时检查 `spawnOffset` 数组并转为 `float[]`。
   - Step 5 doorLinks 处理循环中，当 `SpawnOffset` 非空时执行 HTML→Unity 坐标转换（左上角原点→房间中心原点、y 轴翻转、乘以 gridScale），赋值给 `matchingConn.SpawnOffset`。
   - 新增 `spawnOffsetApplied` 计数器，导入日志中报告应用数量。
4. **`ScaffoldToSceneGenerator.cs` — Phase 4 使用 SpawnOffset 生成 SpawnPoint**：
   - 提前查找 `reverseConn`（原在 WireDoor 前才查找），复用于 SpawnOffset 检查和 IsLayerTransition。
   - **正向 SpawnPoint（fwdSpawn，target 房间内）**：优先使用 `reverseConn.SpawnOffset`（若非零），否则回退 `FindReverseDoorPosition` 边缘中点逻辑。
   - **反向 SpawnPoint（revSpawn，source 房间内）**：优先使用 `conn.SpawnOffset`（若非零），否则回退 `conn.DoorPosition` 边缘中点逻辑。
   - 使用自定义 SpawnOffset 时输出日志。
5. **向后兼容**：旧 JSON（无 spawnOffset 字段）正常导入，SpawnOffset 默认为零向量，回退到旧的边缘中点逻辑。旧 .asset 文件中新字段自动为零向量。

---

---

## Bug 修复：GameObjectPool 已销毁池化实例引发 MissingReferenceException — 2026-02-27 16:15

### 修改文件：
- `Assets/Scripts/Core/Pool/GameObjectPool.cs`
- `Assets/Scripts/SpaceLife/SpaceLifeManager.cs`

### 内容简述：
修复了 `GameObjectPool.Get()` 中当池化实例被外部销毁时（如场景卸载而 PoolManager 因 DontDestroyOnLoad 仍存活）抛出 `MissingReferenceException` 的问题。同时在 `SpaceLifeManager.ReturnPlayerToPool()` 中添加了防御性空检查，防止将已销毁对象归还对象池。

### 目的：
防止对象池实例被外部销毁后进入 SpaceLife 模式时崩溃。

### 技术方案：
1. **`GameObjectPool.Get()`**：从内部 `ObjectPool<GameObject>` 取出实例后，添加 Unity 空检查（`instance == null`）。若底层 GameObject 已被销毁，则通过 `CreateInstance()` 重新创建新实例后再定位并激活。
2. **`SpaceLifeManager.ReturnPlayerToPool()`**：在调用 `_playerPool.Return()` 或 `Destroy()` 前添加二次 Unity 空检查，防范 `_currentPlayer` 在 C# 层非空但 Unity 对象已被销毁的情况（原有的 early return 仅检查 C# null，不检测 Unity 销毁状态）。

---

---

## UI 原型：星图 UI v2 — 2026-02-27 16:43

### 修改文件：
- `Docs/StarChartUIPrototype.html`

### 内容简述：
重写星图 UI HTML 原型，使其与 `StarChartUIDesign.md` 对齐。应用了大量结构与交互变更。

### 目的：
提供一个精确的、可交互的浏览器原型，用于 UI 讨论和迭代，之后再在 Unity `UICanvasBuilder.cs` 中实现。

### 技术方案：
1. **布局重构**：左列 = 加特林切换器（▲/▼ + 配装名 + 计数器）。右侧 = 单个配装卡片，包含 PRIMARY 和 SECONDARY 两条轨道纵向堆叠（匹配设计文档 §一）。
2. **加特林旋转动画**：CSS `perspective rotateX` 关键帧 — `rotate-out-up/down`（280ms）+ `rotate-in-down/up`（320ms）模拟转管手感。切换后 `loadout-section` 附带机械抖动。
3. **轨道槽位结构**：每条轨道为单行平铺混合 CORE+PRISM 槽位（空槽显示类型标签）。装备时强制匹配槽位类型，不匹配则抖动反馈。
4. **背包**：56×56px 槽位，2px 间距，flex-wrap — 匹配设计文档紧凑背包布局规格。980px 面板宽度下可容纳 10–12 列。
5. **悬停高亮**：轨道块 `mouseenter` 时以 `inset 2px 0 0 cyan` 边框 + 背景色调高亮。
6. **扫描线 + 扫描光束**：静态扫描线叠加层 + 动画移动光束（`scanBeam` 关键帧，4秒循环）。
7. **已装备槽位呼吸光效**：已装备槽位的 `::before` 伪元素上使用 `slotBreath` 关键帧动画。
8. **键盘快捷键**：Q/E + PageUp/PageDown 切换配装；Esc 清除选中。
9. **操作按钮**：保存配置 / 重命名（prompt）/ 删除（最后一个配装有保护机制）。
10. **工具提示**：150ms 延迟，跟随鼠标，智能边缘避让，显示已装备位置标签。

---

---

## UI 原型：星图 UI v2.1 — 轨道多行布局 & 类型配色 — 2026-02-27 16:53

### 修改文件：
- `Docs/StarChartUIPrototype.html`

### 内容简述：
更新 HTML 原型：PRIMARY 轨道改为双行布局；所有槽位类型拥有独立配色标识；背包筛选按钮顺序与轨道从左到右的类型顺序一致（SAIL→PRISM→CORE→SAT）。

### 变更细节：
1. **PRIMARY 轨道 → 双行**：第 0 行 = SAIL 槽位 + SAT 槽位（从左到右）。第 1 行 = PRISM 槽位 + CORE 槽位（从左到右）。SECONDARY 轨道保持单行（SAIL→PRISM→CORE→SAT）。
2. **类型配色系统**：新增 CSS 变量 `--col-sail`（#34d399 绿）、`--col-prism`（#a78bfa 紫）、`--col-core`（#60a5fa 蓝）、`--col-sat`（#fbbf24 黄），含暗色/发光变体。每种槽位类型拥有独立的背景、边框和悬停发光效果。
3. **行类别标签**：每组槽位带有彩色圆点 + 类型名称标签（`trl-sail/prism/core/sat` 类），便于用户即时识别所属类型。
4. **槽位类型标签颜色**：`.slot-type-label` 现继承对应类型的强调色，不再使用统一的暗白色。
5. **筛选按钮颜色**：每个筛选按钮（SAILS/PRISMS/CORES/SATS）使用对应类型强调色；激活态使用彩色背景 + 边框。
6. **筛选顺序**：从 ALL/CORES/PRISMS/SAILS/SATS → ALL/SAILS/PRISMS/CORES/SATS（从左到右与轨道布局一致）。
7. **数据结构**：装配槽位从扁平数组重构为 `{ sail:[], sat:[], prism:[], core:[] }` 每轨道。`renderTrack` 替换为 `renderSlotRow(trackName, rowType, slots)`。`allSlots()` 辅助函数用于展平装备/卸装逻辑。

---

---

## 星图 UI — Secondary Track 与 Primary 对齐 (2026-02-28 09:33)

### 修改文件：
- `Tools/StarChartUIPrototype.html`

### 内容简述：
使 SECONDARY 轨道的槽位数量和布局与 PRIMARY 轨道在所有 3 套配置中完全一致。

### 目的：
此前 SECONDARY 槽位较少（sail×1, prism×2, core×1, sat×1），而 PRIMARY 为（sail×2, sat×1, prism×3, core×1）。这种不一致使 SECONDARY 显得较弱且视觉不平衡。现在两条轨道共享相同的槽位结构。

### 技术方案：
1. **数据**：更新所有 3 套配置的 `secondary` 槽位数组，使其与 `primary` 数量一致：`sail×2, sat×1, prism×3, core×1`。已装备的物品在适用处予以保留。
2. **渲染顺序**：将 `renderAllTracks()` 中 secondary 的调用顺序从 `sail→prism→core→sat` 改为 `sail→sat→prism→core`，与 primary 的渲染顺序一致。
3. **数据键顺序**：Secondary 数据对象现与 primary 使用相同键顺序（`sail→sat→prism→core`），保持一致性。

---

---

## 星图 UI — 配装栏/背包布局再平衡 (2026-02-28 09:36)

### 修改文件：
- `Tools/StarChartUIPrototype.html`

### 内容简述：
缩小了配装栏区域，放大了背包面板。统一了配装栏与背包的槽位尺寸，使已装备部件与背包部件视觉大小一致。

### 目的：
此前配装栏槽位（64px）比背包槽位（56px）更大，造成视觉不一致。配装栏区域占用过多垂直空间，导致背包网格空间不足。用户要求缩小配装栏、放大背包，并统一部件尺寸。

### 技术方案：
1. **统一槽位尺寸**：`--slot-size` 和 `--inv-slot-size` 均设为 **52px**（分别从 64px 和 56px 调整）。
2. **面板高度**：从 620px 增加到 **700px**，为背包提供更多垂直空间。
3. **配装栏压缩**：缩小 loadout-section 内边距（8px→4px）、loadout-card 内边距（8px→4px）、间距（6px→3px）、track-block 间距（8px→6px，内边距 4px→2px）、track-block-label 宽度（80px→64px）、track-col 内边距（4px→2px，间距 3px→2px）、gatling-col 宽度（64px→56px）。
4. **字体缩放**：轨道标签 `.tb-name` 10px→9px、`.tb-bind` 9px→8px、`.slot-icon` 24px→20px、空槽 '+' 20px→16px。
5. **背包增强**：增大 `.inventory-area` 内边距（上 6px→8px，下 8px→10px），提供更多呼吸空间。

---

---

## 星图 UI — 配装栏/背包 50-50 布局 + 槽位缩小 (2026-02-28 09:52)

### 修改文件：
- `Tools/StarChartUIPrototype.html`

### 内容简述：
将配装栏与背包面板改为 50/50 等分布局，缩小配装栏槽位尺寸。轨道列保持原有单行 4 列排列（SAIL → PRISM → CORE → SAT）。

### 目的：
此前背包面板过大、配装栏与背包比例失调。用户要求两个区域 55 开（各占一半）。

### 技术方案：
1. **50/50 等分布局**：`.loadout-section` 从 `flex-shrink:0`（自然高度）改为 `flex:1`，与 `.inventory-section`（同为 `flex:1`）实现等分。两者均添加 `min-height:0` 和 `overflow:hidden` 防止内容溢出。
2. **轨道列保持单行 4 列**：`.track-rows` 维持 `flex-direction:row`，`.track-col` 维持 `flex:1`，列顺序 SAIL → PRISM → CORE → SAT 不变。
3. **槽位尺寸缩小**：`--slot-size` 从 52px 缩小到 **40px**，`--inv-slot-size` 从 52px 缩小到 **44px**。
4. **track-block 弹性填充**：添加 `flex:1` + `min-height:0` + `align-items:stretch`，使 PRIMARY 和 SECONDARY 在 loadout-card 内均匀分配高度。

---

---

## 星图 UI — Loadout 切换动画改为贴纸滑动 (2026-02-28 09:57)

### 修改文件：
- `Tools/StarChartUIPrototype.html`

### 内容简述：
将 Loadout 切换动画从 3D rotateX 滚筒旋转效果改为 2D translateY + opacity 贴纸滑动效果。切换到下一个 loadout 时，当前卡片向上滑出并渐隐，新卡片从下方滑入并渐显；切换到上一个 loadout 时方向相反。

### 目的：
用户描述 Loadout 切换效果应仿佛从正上方俯视擀面杖上的贴纸滚动——贴纸向上/下位移并渐隐，另一张从对向出现。原 3D rotateX 效果偏"翻转"而非"贴纸滑动"，需替换为纯 2D 位移+透明度动画。

### 技术方案：
1. **移除旧 3D 动画**：删除 `cylinderRollOutForward/InForward/OutBackward/InBackward` 四个 `@keyframes`（基于 `rotateX`），以及 `perspective`、`transform-style:preserve-3d`、`backface-visibility:hidden` 等 3D 属性。同时移除更早期的 `rotateOutUp/InDown/OutDown/InUp` 四个旧动画。
2. **新增贴纸滑动动画**：四个新 `@keyframes`：
   - `slideOutUp`：`translateY(0) → translateY(-40%)` + `opacity 1→0`（next 退出）
   - `slideInFromBelow`：`translateY(40%) → translateY(0)` + `opacity 0→1`（next 进入）
   - `slideOutDown`：`translateY(0) → translateY(40%)` + `opacity 1→0`（prev 退出）
   - `slideInFromAbove`：`translateY(-40%) → translateY(0)` + `opacity 0→1`（prev 进入）
3. **CSS 类映射不变**：`.roll-out-forward` / `.roll-in-forward` / `.roll-out-backward` / `.roll-in-backward` 类名保留，JS 切换逻辑零改动。
4. **时长微调**：动画时长从 0.28s 缩短至 0.26s，JS `DURATION` 常量同步调整为 260ms，使滑动更干脆。

---

---

## 星图 UI — Loadout 贴纸滑动动画增加深度缩放 (2026-02-28 10:02)

### 修改文件：
- `Tools/StarChartUIPrototype.html`

### 内容简述：
在 Loadout 切换的贴纸滑动动画中加入 `scale` 变换，退出时从 `scale(1)` 缩小到 `scale(0.88)`，进入时从 `scale(0.88)` 放大到 `scale(1)`，模拟贴纸沿擀面杖弧面滑动时向画面深处隐去/从深处浮现的透视感。位移幅度从 40% 微调至 35%，配合缩放达到更自然的视觉比例。

### 目的：
用户反馈纯位移+渐隐缺少"向画面深处隐去"的感觉，需要加入微缩放来模拟擀面杖弧面的透视纵深。

### 技术方案：
1. 四个 `@keyframes` 的 `transform` 属性从单纯 `translateY` 改为 `translateY + scale` 复合变换。
2. 缩放比例 `0.88`：足够感知到纵深，又不至于过度夸张。
3. 位移幅度从 `40%` 调整为 `35%`，因为缩放本身已提供额外的视觉退场效果，不需要过大位移。

---

---

## 星图 UI — 拖拽装备系统 (2026-02-28 13:22)

### 修改文件：
- `Tools/StarChartUIPrototype.html`

### 内容简述：
将星图 UI 原型的物品交互方式从点选（click-to-select + click-to-equip）完全替换为拖拽（drag & drop）系统。

### 目的：
实现更直觉化的背包式交互体验，用户可直接拖拽物品到轨道槽位装备，或从槽位拖回库存区域卸载。

### 技术方案：

**1. 移除旧点选逻辑：**
- 删除 `state.selectedInvItem` 状态字段
- 移除 `onInvClick()`、`onSlotClick()` 函数
- 移除库存物品和槽位的 `click` 事件监听

**2. 拖拽状态管理器（`dragState` 对象）：**
- 字段：`isDragging`、`sourceType`（`inventory`/`slot`）、`item`、`sourceSlotId`、`sourceLoadout`、`startX/Y`、`pendingEl/Item/Source`（预阈值状态）
- `startDrag()`：检查 `isAnimating` 保护，设置状态，显示 Ghost
- `endDrag()`：清理所有状态、高亮、Ghost、cursor
- `cancelDrag()`：回滚槽位预清空，调用 `endDrag()`

**3. Ghost 拖拽元素：**
- `#drag-ghost` 固定定位，`pointer-events:none`，`z-index:9999`
- 跟随鼠标（偏移 +14px），显示物品图标和短名称
- 颜色状态：默认青色边框 → 有效目标绿色（`.ghost-valid`）→ 无效目标红色（`.ghost-invalid`）

**4. 拖拽发起（4px 阈值）：**
- 库存物品：`mousedown` 记录起始坐标，`mousemove` 超过 4px 后触发 `startDrag()`，原物品设为半透明占位（`.dragging-source`）
- 已装备槽位：同上，`startDrag()` 时立即预清空槽位数据并重新渲染，记录原始数据用于取消回滚

**5. 槽位高亮反馈（`mousemove` 中实时计算）：**
- 类型匹配 → `.drop-valid`（绿色边框发光 + scale 1.08）
- 类型不匹配 → `.drop-invalid`（红色边框）
- 库存区域（从槽位拖出时）→ `.drop-target-inv`（绿色内阴影）

**6. mouseup 放置逻辑（四种情形）：**
- **情形 A（库存→空槽）**：装备物品，播放 `snap-in` 弹入动画（scale 1.18→0.96→1.0）
- **情形 B（库存→占用槽 / 槽→槽交换）**：原槽物品自动退回库存，新物品装入
- **情形 C（槽→库存区域）**：槽位已预清空，物品回到库存（重新渲染即可）
- **情形 D（无效区域/类型不匹配）**：调用 `cancelDrag()` 回滚，播放 shake 动画

**7. Edge Cases 防护：**
- `Escape` 键触发 `cancelDrag()`
- `startDrag()` 入口检查 `isAnimating`，动画中禁止拖拽
- `window.blur` 事件触发 `cancelDrag()`，防止鼠标移出窗口丢失 mouseup
- Loadout 切换时调用 `endDrag()` 清理进行中的拖拽

**8. 库存渲染逻辑更新：**
- `renderInventory()` 过滤掉当前 Loadout 所有槽位中已装备的物品
- 库存为空时显示 `"ALL ITEMS EQUIPPED"` 占位文字
- 过滤器（SAIL/PRISM/CORE/SAT）与新逻辑兼容（先按类型过滤，再过滤已装备）
- 移除库存物品的 `is-equipped` 标记（已装备物品不再显示在库存中）

---

---

## 星图 UI — 拖拽候选槽高亮 (2026-02-28 13:28)

### 修改文件：
- `Tools/StarChartUIPrototype.html`

### 内容简述：
从库存拖拽物品时，立即高亮所有类型匹配的槽位（绿色 pulse 动画），让用户一眼看到哪里可以放置。

### 技术方案：
- 新增 `.slot.drop-candidate` CSS 类：绿色边框 + `candidatePulse` 呼吸动画（1.1s 循环）
- `startDrag()` 中调用 `highlightCandidateSlots(item.type)`，遍历所有 `.slot` 元素，对 `data-row === itemType` 的槽位添加 `.drop-candidate`
- `endDrag()` 中调用 `clearCandidateSlots()` 清除所有候选高亮
- `mousemove` 中悬停到匹配槽时叠加 `.drop-valid`（更强的高亮 + scale 1.1 + 无 pulse），离开后 `.drop-valid` 被清除，候选槽恢复 pulse 状态
- 类型不匹配槽悬停时仍显示 `.drop-invalid`（红色）

---

---

## 星图 UI — 网格背包 & 异形部件系统 (2026-02-28 13:53)

### 修改文件：
- `Tools/StarChartUIPrototype.html`

### 内容简述：
将星图 UI 原型从单格槽位系统全面重构为 Backpack Battles 风格的网格背包系统，支持异形部件（1×1、1×2、2×1、L形、2×2）在 2×2 Track 网格中的拖拽装备与卸载。

### 目的：
实现计划文档 `.codebuddy/plan/grid-inventory/` 中定义的全部 9 项任务，为后续 Unity 实现提供可交互的 UI 原型参考。

### 技术方案：

**任务1 — 数据模型重构：**
- 为每个 `ITEM` 添加 `shape: [[col,row],...]` 坐标数组，支持 5 种形状
- Track 槽位从 `[{type, itemId}]` 数组改为 `{ grid: [[null,null],[null,null]] }` 2×2 矩阵
- 新增辅助函数：`getShapeBounds(shape)`、`shapeExceedsGrid()`、`canPlace()`、`clearItemFromGrid()`、`writeItemToGrid()`
- 初始 Loadout 数据用 `placeItem()` 辅助函数预填充

**任务2 — Track 2×2 网格渲染：**
- `renderTrackTypeGrid()` 将每个类型列渲染为 `display:grid; grid-template-columns: repeat(2, var(--slot-size))` 的 4 格网格
- 每个 `.track-cell` 携带 `data-track`、`data-type-key`、`data-col`、`data-row` 属性
- 空格子显示淡色 `+` 占位符，被占用格子透明度为 0（被覆盖层遮盖）

**任务3 — 部件覆盖层渲染：**
- `renderItemOverlay()` 在网格容器上用绝对定位叠加覆盖层
- 覆盖层尺寸 = `bounds.cols × CELL_SIZE + (cols-1) × CELL_GAP`，精确覆盖 shape bounding box
- 覆盖层内部渲染 shape 形状格子（非矩形填充），显示部件图标（居中）、名称（底部）
- 类型颜色边框 + 呼吸光晕动画，hover 时 scale(1.04)
- `mousedown` 发起卸载拖拽（Task 8）

**任务4 — 16列库存网格：**
- `renderInventory()` 使用 `display:grid; grid-template-columns: repeat(16, var(--inv-slot-size))`
- 每个部件用 `grid-column: span {cols}; grid-row: span {rows}` 按 shape bounding box 渲染
- 部件内部显示 shape 形状预览（半透明色块）+ 图标 + 名称

**任务5 — Shape-aware Ghost：**
- `showGhost()` 根据 `getShapeBounds(item.shape)` 动态计算 Ghost 宽高
- Ghost 内部渲染 shape 格子网格（active 格子显示青色半透明，empty 格子透明）
- Ghost 根据预览状态切换 `.ghost-valid`（绿色）/ `.ghost-invalid`（红色）边框

**任务6 — 碰撞检测与预览高亮：**
- `mousemove` 检测鼠标下方的 `.track-cell`，以其 `data-col/row` 为锚点
- `applyPreview()` 遍历 `item.shape` 所有偏移格子，调用 `canPlace()` 检测越界和占用
- 全部通过 → 预览格子加 `.preview-valid`（绿色）；任意失败 → `.preview-invalid`（红色）
- 拖拽开始时对匹配类型的整列 `.track-col` 添加 `.drop-candidate-col` 呼吸高亮

**任务7 — mouseup 放置逻辑：**
- 仅在 `dragState.previewValid === true` 时执行放置
- 检测被顶出的部件（displaced items），先清空其格子，再写入新部件
- 支持交换场景：被顶出部件自动回到库存
- 放置成功后对覆盖层播放 `snapIn` 动画（scale 1.18 → 0.96 → 1.0，150ms）

**任务8 — 卸载拖拽：**
- 覆盖层 `mousedown` 时：`sourceType = 'overlay'`，记录 `{ track, typeKey, anchorCol, anchorRow }`
- 拖拽阈值（4px）超过后：`clearItemFromGrid()` 清空格子，`renderAllTracks()` 刷新显示
- 拖拽结束未放置到有效位置 → `cancelDrag()` 恢复原位（`writeItemToGrid()` 重写格子）

**任务9 — Edge Cases 防护：**
- `Escape` 键：调用 `cancelDrag()`，overlay 来源时恢复原位
- `window.blur`：同 cancelDrag
- `isAnimating` 锁：Loadout 切换动画期间 `startDrag()` 直接返回
- `shapeExceedsGrid()` 越界检测集成在 `canPlace()` 中，`applyPreview()` 自动拒绝越界放置
- 类型不匹配时 `applyPreview()` 不调用（直接显示 ghost-invalid），不会写入错误数据

---

---

## Track 网格强制替换 + 飞回动画 — 2026-02-28 14:47

### 修改文件
- `Tools/StarChartUIPrototype.html`

### 内容简述
在星图 UI 原型中实现 Track 网格强制替换功能及被顶出部件飞回库存动画。

### 目的
玩家可直接将新部件拖入已有部件的格子，系统自动顶出旧部件送回库存，无需手动卸载。

### 技术方案

**任务1 — 放置逻辑重构（移除占用即拒绝）：**
- `mouseup` 放置分支：移除原 `canPlace()` 检测，改为遍历 `item.shape` 收集 `evictedIds`（Set）
- 对每个被顶出的部件调用 `clearItemFromGrid()` 清除其在 grid 中的**所有**格子（含未被新部件覆盖的格子）
- 唯一拒绝条件：`shapeExceedsGrid()` 越界 或 类型不匹配

**任务2 — 库存回收：**
- 顶出部件的 grid 数据清除后，`renderInventory()` 自动将其重新渲染到库存末尾（因为 `isEquippedInCurrentLoadout()` 返回 false）
- 覆盖层 DOM 由 `renderAllTracks()` 重新渲染时自动移除旧 overlay、恢复空格 `+` 占位符

**任务3 — 三色预览高亮系统：**
- 新增 `.track-cell.preview-replace`（橙色）CSS 样式
- 新增 `#drag-ghost.ghost-replace`（橙色边框）CSS 样式
- 新增 `.ghost-replace-hint` 文字样式（橙色，绝对定位在 ghost 顶部）
- `applyPreview()` 重构：越界 → 红色；有占用不越界 → 橙色；全空 → 绿色
- `updateGhostHint(count, isInvalid)` 函数：替换时在 Ghost 上显示 `↺ 替换 N 个部件`

**任务4 — 自身冲突处理：**
- 同 Track 内移动：`mousemove` 阶段已 `clearItemFromGrid()` 预清除，`mouseup` 时 grid 中不含自身 id，`evictedIds` 不会包含自身
- 跨 Track 移动：`mouseup` 中检测 `src.track !== track || src.typeKey !== typeKey` 时额外清除来源 grid

**任务5 — 飞回库存动画：**
- `activeFlightAnimations[]` 全局列表管理所有进行中的飞行动画
- `skipAllFlightAnimations()` 在 `startDrag()` 时调用，立即移除所有克隆体并恢复库存可见性
- `flyItemToInventory(itemId, sourceRect)` 函数：
  - 用 `getBoundingClientRect()` 获取来源 overlay 坐标（在 `renderAllTracks()` 前捕获）
  - 创建 `fixed` 定位的 `.fly-clone` DOM，初始位置与 overlay 完全重合
  - 计算 `dx/dy/scale` 使克隆体飞向库存目标格子
  - 通过 CSS `transition: transform 350ms ease-in, opacity 350ms ease-in` 执行飞行
  - 多部件顶出时每个克隆体错开 50ms stagger
  - 飞行期间库存真实 DOM `opacity: 0`，动画结束后恢复并播放 `landBounce`（scale 1.2→1.0，150ms）

**任务6 — Edge Cases 防护：**
- `assertGridDomSync()` 函数：遍历所有 track/typeKey，对比 grid 数据与 DOM overlay，发现不一致时 `console.warn`
- 强制替换成功后和 `cancelDrag()` 后均调用 `assertGridDomSync()`
- `Escape` 键 → `cancelDrag()` → 数据回滚 + 断言
- `window.blur` → `cancelDrag()` → 数据回滚
- 通知栏：替换成功显示 `REPLACED: [新部件名] ↔ [被替换部件名, ...]`

---

---

## Bug Fix: 强制替换锚点计算错误 + grabOffset偏移 — 2026-02-28 15:14

### 修改文件
- `Tools/StarChartUIPrototype.html`

### Bug 1 — 多格部件无法放置（锚点计算错误）
**根因**：`mousemove` 中调用 `applyPreview(trackName, typeKey, col, row)` 时传入的是**鼠标悬停的格子坐标**（如 `[1,1]`），而非部件锚点。对于 2×2 部件，`shapeExceedsGrid([[0,0],[1,0],[0,1],[1,1]], 1, 1)` 会返回 `true`（`1+1=2 > 1`），导致被误判为越界，`previewTarget` 始终为 null，mouseup 时无法放置。

**修复**：将 `applyPreview(trackName, typeKey, col, row)` 改为 `applyPreview(trackName, typeKey, 0, 0)`。由于所有形状都从 `[0,0]` 开始，锚点固定为左上角是正确的。

### Bug 2 — Ghost 拖拽时位置偏移
**根因**：`grabOffsetX/Y` 已经正确记录了鼠标在元素内的相对位置，确认两处（overlay mousedown 和 bindInvItemDrag）都使用 `e.clientX - rect.left` / `e.clientY - rect.top`，保持鼠标抓住哪个位置 ghost 就跟随那个位置，无偏移。

---

---

## Bug Fix: Overlay 遮挡 Cell 导致拖拽无法替换已有部件 — 2026-02-28 15:23

### 修改文件
- `Tools/StarChartUIPrototype.html`

### 根本原因
Track 上已有部件时，`.item-overlay` 元素覆盖在 `.track-cell` 上方。`mousemove` 和 `mouseup` 中用 `elementFromPoint()` 获取鼠标下方元素时，返回的是 overlay 而非 cell，导致 `cellEl` 为 null，预览不触发，`previewTarget` 始终为 null，mouseup 时无法放置。

**结果**：从背包拖部件到已有部件的 Track 上，永远无法替换——这正是用户反馈"不能用"的根本原因。

### 修复方案

**mousemove**：不再只检查 `cellEl`，同时检查 `overlayEl = target?.closest('.item-overlay')`。从 cellEl 或 overlayEl 中取 `trackName`/`typeKey`，只要任一有效就触发 `applyPreview`。

**mouseup**：判断条件从 `if (cellEl && dragState.previewTarget)` 改为 `if ((cellEl || overlayDropEl) && dragState.previewTarget)`，确保鼠标松开在 overlay 上时也能正确放置。

### 覆盖场景
- 1格部件拖到有1格部件的Track → 替换 ✓
- 4格部件拖到有2格部件的Track → 顶掉2格部件 ✓
- 2格部件拖到有1格部件的Track → 顶掉1格部件 ✓
- 任意部件拖到空Track → 正常放置 ✓

---

---

## StarChart UI Phase A — 视觉样式与布局对齐 — 2026-02-28 16:06

### 新建文件
- `Assets/Scripts/UI/StarChartTheme.cs` — 颜色主题静态类，提供所有主题色常量和 `GetTypeColor()` API
- `Assets/Scripts/UI/StatusBarView.cs` — 底部通知栏组件，PrimeTween Sequence 实现延迟淡出

### 修改文件
- `Assets/Scripts/UI/SlotCellView.cs` — 移除 hardcode 颜色字段，改用 StarChartTheme；新增 `SetThemeColor()`、`_placeholderLabel`（`+` 占位符）、`SetReplaceHighlight()`
- `Assets/Scripts/UI/TrackView.cs` — 新增 `_prismLabel`/`_coreLabel` 类型标签字段；`Bind()` 中设置青色/紫色/蓝色；`RefreshLayer()` 增加 `layerType` 参数并调用 `SetThemeColor()`
- `Assets/Scripts/UI/InventoryItemView.cs` — 新增 `_typeDot`/`_equippedBorder` 字段；`Setup()` 设置类型色点和装备边框；`SetSelected()` 改用 `StarChartTheme.SelectedCyan`；悬停时 PrimeTween scale 1.06 动画
- `Assets/Scripts/UI/InventoryView.cs` — `SetFilter()` 调用 `UpdateFilterButtonStyles()`；新增过滤按钮激活/非激活态颜色切换
- `Assets/Scripts/UI/StarChartPanel.cs` — 新增 `_statusBar` 字段；装备/卸载成功失败时调用 `ShowMessage()`
- `Assets/Scripts/UI/Editor/UICanvasBuilder.cs` — 全面重构 `BuildStarChartSection()`：深色背景、四角括号、Header 栏、上55%/下40%/底部4% 布局、StatusBar；重构 `BuildTrackView()`：类型标签、StarChartTheme 颜色；重构 `BuildSlotCell()`：占位符标签；更新 `CreateInventoryItemViewPrefab()`：新增 typeDot/equippedBorder

### 目的
将 Unity StarChart 面板的视觉样式与 `StarChartUIPrototype.html` 原型对齐，实现深色科技感配色、四色类型主题、Header/StatusBar 通知系统，不改变现有数据模型和交互逻辑。

### 技术方案
- `StarChartTheme` 静态类集中管理所有颜色常量，所有 View 通过 `GetTypeColor(itemType)` 获取类型色，禁止 hardcode
- `StatusBarView` 使用 PrimeTween `Sequence.ChainDelay + Chain(Tween.Alpha)` 实现延迟淡出，`_fadeTween.Stop()` 打断旧动画
- `UICanvasBuilder` 新增 `BuildCornerBrackets()` 和 `BuildHeader()` 辅助方法，`BuildStarChartSection()` 完全重写为原型对齐的层级结构
- 运行 `ProjectArk > Build UI Canvas` 后一键生成完整层级，幂等操作

---

# StarChart UI Phase B — 数据模型视图层重构 (2026-02-28 22:00)

### 新建文件
- 无

### 修改文件
- `Assets/Scripts/UI/SlotCellView.cs` — 新增 `SlotType` 枚举（Core/Prism/LightSail/Satellite）替换 `IsCoreCell` bool；新增 `IsOccupied` 属性；新增 `HasSpaceForItem` 委托（SAIL/SAT 空间检测）；`OnPointerEnter` 扩展为三色高亮逻辑（绿/橙/红）；调用 `mgr.UpdateGhostDropState()` 同步 Ghost 边框颜色；`OnBeginDrag` 移除类型限制（支持所有类型拖拽）
- `Assets/Scripts/UI/TrackView.cs` — 新增 `_sailCell`/`_satCells`/`_sailLabel`/`_satLabel` 字段；`Bind()` 新增 `StarChartController` 参数；`Refresh()` 新增 `RefreshSailCell()`/`RefreshSatCells()` 刷新 SAIL/SAT 槽位；`HasSpaceForItem()` 扩展支持 LightSail/Satellite；`SetMultiCellHighlight()` 新增 `isReplace` 参数；`ClearAllHighlights()` 覆盖 SAIL/SAT 槽位
- `Assets/Scripts/UI/DragDrop/DragDropManager.cs` — 新增 `DropTargetSlotType`/`DropTargetIsReplace` 属性；新增 `EvictedItems` 列表；新增 `EvictBlockingItems()` 方法（强制替换时卸载阻碍部件）；`EquipToTrack()` 扩展支持 LightSail/Satellite；新增 `UpdateGhostDropState()` 方法；`BeginDrag()` 调用 `FlyBackAnimator.SkipAll()`；新增 `ShowReplaceMessage()` 显示橙色替换通知
- `Assets/Scripts/UI/StarChartPanel.cs` — 新增 `StatusBar` 属性（暴露给 DragDropManager）；新增 `Controller` 属性；`Bind()` 中将 controller 传给 TrackView

### 目的
重构视图层支持 4 列类型布局（SAIL/PRISM/CORE/SAT），实现强制替换交互逻辑（拖拽时顶掉已有部件），完善三色拖拽预览系统。

### 技术方案
- `SlotType` 枚举替代 `IsCoreCell` bool，统一 4 种槽位类型的类型匹配逻辑
- SAIL/SAT 槽位通过 `HasSpaceForItem` 委托注入空间检测，不修改 `SlotLayer<T>` 底层结构
- 强制替换在 `DragDropManager.EvictBlockingItems()` 层实现，逐个卸载阻碍部件直到空间足够
- Ghost 边框颜色通过 `UpdateGhostDropState(DropPreviewState)` 实时同步

---

# StarChart UI Phase C — 交互动画与体验打磨 (2026-02-28 22:00)

### 新建文件
- `Assets/Scripts/UI/FlyBackAnimator.cs` — 静态动画系统，`FlyTo()` 创建飞行克隆体从槽位飞向背包（350ms InQuad，位移+缩小+淡出），落地弹跳（scale 1.0→1.12→1.0，100ms），`SkipAll()` 立即跳过所有进行中动画

### 修改文件
- `Assets/Scripts/UI/StatusBarView.cs` — `ShowMessage()` 改为先 alpha 0 淡入（150ms OutQuad），hold duration 后淡出（500ms InQuad）；新消息打断旧动画从 alpha 0 重新淡入；使用 `Sequence` 替代 `Tween`
- `Assets/Scripts/UI/DragDrop/DragGhostView.cs` — 新增 `DropPreviewState` 枚举；新增 `_borderImage`/`_replaceHintLabel` 字段；新增 `SetDropState()` 方法（绿/橙/红边框 + `↺ REPLACE` 提示）；`Show()` 播放 scale 0.8→1.0 弹出动画（80ms OutBack）；`Hide()` 播放 scale 1.0→0 收缩动画（80ms InQuad）后 `SetActive(false)`
- `Assets/Scripts/UI/SlotCellView.cs` — `SetItem()` 播放背景闪烁（白色→类型色，150ms OutQuad）；`SetEmpty()` 播放背景淡出（→SlotEmpty，100ms OutQuad）；所有动画使用 `useUnscaledTime: true`
- `Assets/Scripts/UI/InventoryItemView.cs` — `SetSelected()` 改为 PrimeTween 边框淡入淡出（100ms OutQuad）；`OnBeginDrag()` 移除类型限制（支持所有类型）；拖拽开始/结束时 alpha 动画（0.4/1.0，80ms）
- `Assets/Scripts/UI/DragDrop/DragDropManager.cs` — 新增 `_rootCanvas` 字段；`Bind()` 缓存 `_inventoryRect`；`ExecuteDrop()` 后调用 `TriggerFlyBackAnimations()`；新增 `TriggerFlyBackAnimations()` 方法
- `Assets/Scripts/UI/StarChartPanel.cs` — 新增 `_panelCanvasGroup` 字段；`Open()` 播放 scale 0.95→1.0 + alpha 0→1（200ms OutQuad），动画期间 `interactable=false`；`Close()` 播放 scale 1.0→0.95 + alpha 1→0（150ms InQuad），动画结束后 `SetActive(false)`

### 目的
为关键交互添加动画反馈，提升手感和视觉体验，与原型的动态效果对齐。

### 技术方案
- 所有动画使用 PrimeTween，`useUnscaledTime: true`（星图面板在暂停状态下打开）
- `FlyBackAnimator` 为静态类，维护 `_activeAnimations` 列表，`SkipAll()` 调用 `Sequence.Complete()` 立即跳过
- Ghost 动画通过 `_scaleTween` 句柄管理，`Show()`/`Hide()` 调用前先 `Stop()` 防止冲突
- 面板开关动画通过 `CanvasGroup.interactable` 防止动画期间误操作

---

---

## StarChart UI 冗余代码清理 — 2026-02-28 22:21

### 修改文件
- `Assets/Scripts/UI/StarChartPanel.cs` — 删除注释掉的 Tooltip 代码块（`_itemTooltipView` 字段、`HandleCellPointerEntered/Exited`、`HandleInventoryItemPointerEntered/Exited` 方法）；删除 `Bind()` 中注释掉的 `OnCellPointerEntered/Exited` 订阅行；删除 `// TODO: 光帆/伴星专用槽位` 注释；删除 `RefreshAll()`、`HandleInventoryItemSelected()`、`HandleCellClicked()` 中的 `Debug.Log` 调用
- `Assets/Scripts/UI/TrackView.cs` — 删除 `OnCellPointerEntered` 和 `OnCellPointerExited` 事件声明；删除 `Awake()` 中所有 Cell 的 `OnPointerEntered`/`OnPointerExited` 订阅转发行（Core/Prism/SAIL/SAT 共 5 处）
- `Assets/Scripts/UI/SlotCellView.cs` — 删除标注为 "Legacy compatibility" 的 `IsCoreCell` 属性（getter/setter 共 4 行）
- `Assets/Scripts/UI/InventoryView.cs` — 删除 `OnItemPointerEntered` 和 `OnItemPointerExited` 事件声明；删除 `Refresh()` 中的 `view.OnPointerEntered`/`view.OnPointerExited` 订阅转发行；删除 `Bind()` 中的 `Debug.Log`、`Refresh()` 中的 `Debug.LogWarning`（空检查）和末尾的 `Debug.Log`（统计）
- `Assets/Scripts/UI/DragDrop/DragDropManager.cs` — 将 `ShowReplaceMessage()` 中的 `System.Linq.Enumerable.Select` 替换为手写 `StringBuilder` 循环，消除对 `System.Linq` 的依赖

### 删除文件
- `Assets/Scripts/UI/Editor/StarChart169Builder.cs` — 废弃的旧版 Builder 存根（仅打印"请用新工具"日志），连同 `.meta` 文件一并删除

### 目的
在不改变任何运行时行为的前提下，清除 StarChart UI Phase A/B/C 三轮迭代后积累的所有冗余代码，使代码库清晰、精简、整洁。

### 技术方案
- 所有删除均经过 grep 全局验证，确认无其他文件引用被删除的符号（`OnCellPointerEntered/Exited`、`IsCoreCell`、`OnItemPointerEntered/Exited`）
- `System.Linq` 替换为 `System.Text.StringBuilder` 手写循环，符合项目"无不必要依赖"原则
- `StarChart169Builder.cs` 的 `.meta` 文件已同步删除，避免 Unity 产生孤立 GUID 警告

---

---

## Fix: PrimeTween Sequence useUnscaledTime Mismatch — 2026-03-01 00:19

### 修改文件
- `Assets/Scripts/UI/StarChartPanel.cs`
- `Assets/Scripts/UI/SlotCellView.cs`
- `Assets/Scripts/UI/FlyBackAnimator.cs`
- `Assets/Scripts/UI/StatusBarView.cs`

### 内容简述
修复 PrimeTween `Sequence.Create()` 与子 Tween `useUnscaledTime` 不一致导致的运行时 LogError。

### 目的
消除打开/关闭星图面板、装备物品、飞回动画、状态栏通知时控制台输出的 `useUnscaledTime was ignored` 错误。

### 技术方案
- PrimeTween 的 Sequence 会强制将自身的 `useUnscaledTime` 覆盖所有子 Tween，父子不一致时抛出错误
- 所有 4 个文件中的 `Sequence.Create()` 均缺少 `useUnscaledTime: true` 参数，而子 Tween 都传了 `useUnscaledTime: true`
- 修复方式：统一为 `Sequence.Create(useUnscaledTime: true)`，使父 Sequence 与所有子动画保持一致
- UI 动画使用 unscaled time 是正确行为——星图面板打开时游戏处于暂停状态（`Time.timeScale = 0`）

---

---

## StarChart UI 布局重构 — 完整实现（Phase 2）— 2026-03-01 00:27

### 新建/修改文件
- `Assets/Scripts/UI/TrackView.cs` ← **完全重写**
- `Assets/Scripts/UI/SlotCellView.cs` ← **修改**（新增 SetOverlay + 拖拽脉冲动画）
- `Assets/Scripts/UI/LoadoutSwitcher.cs` ← **新建**
- `Assets/Scripts/UI/StarChartPanel.cs` ← **完全重写**
- `Assets/Scripts/UI/DragDrop/DragDropManager.cs` ← **修改**（新增 HighlightMatchingColumns）

### 内容简述

#### 1. TrackView.cs — 完全重写
- 引入 `TypeColumn` 内部类（`MonoBehaviour`），持有 `_columnLabel`、`_columnDot`、`_columnBorder`、`_cells[4]`
- `TypeColumn.Initialize(slotType, typeColor, ownerTrack)` 统一初始化列标识和颜色
- `TypeColumn` 实现 `IPointerEnterHandler/IPointerExitHandler`，hover 时边框从 dim 色渐变到亮色（PrimeTween，`useUnscaledTime: true`）
- `TypeColumn.SetDropHighlight(bool)` / `ClearDropHighlight()` 供拖拽系统调用
- `TrackView` 改为持有 `_sailColumn/_prismColumn/_coreColumn/_satColumn` 四个 `TypeColumn` 引用
- `Refresh()` 按列分组刷新：`RefreshColumn<T>` 处理 Core/Prism，`RefreshSailColumn`/`RefreshSatColumn` 处理 SAIL/SAT
- 新增 `GetColumn(SlotType)` / `GetAllCells()` / `SetColumnDropHighlight(SlotType, bool)` 公共 API
- SAT 列容量从 2 扩展为 4（2×2 网格）

#### 2. SlotCellView.cs — 新增功能
- 新增 `SetOverlay(StarChartItemSO item, bool isPrimary)` 方法：主 cell 显示图标，次 cell 仅显示着色背景
- 新增 `_pulseTween` 字段：拖拽悬停时播放 scale 1.0→1.05 脉冲动画（`useUnscaledTime: true`）
- `OnPointerEnter` 在拖拽状态下触发脉冲；`OnPointerExit` 恢复 scale
- `OnDestroy` 中正确 Stop `_pulseTween`

#### 3. LoadoutSwitcher.cs — 新建
- `[SerializeField]` 字段：`_prevButton`、`_nextButton`、`_drumCounterLabel`、`_loadoutCard`
- `SetLoadoutCount(int)` 设置 Loadout 总数，MVP 单 Loadout 时禁用按钮
- `SwitchTo(int index, bool slideUp)` 切换 Loadout，播放 LoadoutCard 滑动动画
- `event Action<int> OnLoadoutChanged` 供 StarChartPanel 订阅
- DrumCounter 格式：`"LOADOUT #1  ·  1/1"`

#### 4. StarChartPanel.cs — 完全重写
- 新增 `[SerializeField] LoadoutSwitcher _loadoutSwitcher` 和 `[SerializeField] RectTransform _loadoutCard`
- `Open()` 动画序列：面板 scale+alpha + LoadoutCard 从下方滑入（`UIAnchoredPositionY`，OutCubic，0.25s）
- `Close()` 动画序列：面板 scale+alpha + LoadoutCard 向下滑出（InQuad，0.15s）
- `RefreshAll()` 新增 `UpdateStatusBar()` 调用，格式：`"EQUIPPED X/10 · INVENTORY X ITEMS · DRAG TO EQUIP · CLICK TO INSPECT"`
- `Bind()` 中初始化 LoadoutSwitcher（`SetLoadoutCount(1)`）并订阅 `OnLoadoutChanged`
- `OnDestroy()` 中取消订阅 `OnLoadoutChanged`

#### 5. DragDropManager.cs — 新增 TypeColumn 高亮
- `BeginDrag()` 末尾调用 `HighlightMatchingColumns(payload.Item, true)`
- `CleanUp()` 末尾调用 `HighlightMatchingColumns(CurrentPayload.Item, false)`
- 新增 `HighlightMatchingColumns(StarChartItemSO item, bool highlight)` 方法：
  - 通过 `_panel.GetComponentsInChildren<TrackView>()` 找到所有轨道视图
  - 对每个 TrackView 调用 `GetColumn(matchType).SetDropHighlight(true/false)`

### 目的
完整实现 StarChart UI 布局重构计划（task-item.md）的全部 9 个任务，使 Unity 实现对齐 `Tools/StarChartUIPrototype.html` 原型的布局架构和交互逻辑。

### 技术方案
- `TypeColumn` 作为 `MonoBehaviour` 挂载在独立 GameObject 上，通过 `UICanvasBuilder.WireField` 自动连线
- 所有 PrimeTween 动画统一使用 `useUnscaledTime: true`，兼容游戏暂停状态（`Time.timeScale = 0`）
- `LoadoutSwitcher` 通过 `event Action<int> OnLoadoutChanged` 与 `StarChartPanel` 解耦通信
- 拖拽高亮通过 `DragDropManager → TrackView.GetColumn → TypeColumn.SetDropHighlight` 链路实现
- 验收步骤：Unity Editor 执行 `ProjectArk > Build UI Canvas` → Play Mode 逐条验证

---

---

## StarChart UI 布局重构（对齐 HTML 原型）— 2026-03-01 00:28

### 新建文件
- `Assets/Scripts/UI/LoadoutSwitcher.cs`

### 修改文件
- `Assets/Scripts/UI/StarChartPanel.cs`
- `Assets/Scripts/UI/StarChartTheme.cs`
- `Assets/Scripts/UI/Editor/UICanvasBuilder.cs`

### 内容简述
将 StarChart UI 布局从旧的线性槽位结构重构为对齐 HTML 原型的新布局，主要变更：

1. **`LoadoutSwitcher.cs`（新建）**：Gatling 列组件，包含 ▲/▼ 按钮、DrumCounter（`TMP_Text`，显示 "LOADOUT #1 · 1/1"）、LOADOUT 标签。MVP 单 Loadout 模式下禁用按钮；预留 `SwitchTo(int index)` 接口供后续多 Loadout 扩展。Loadout 切换时对 LoadoutCard 播放滑动动画（PrimeTween，`useUnscaledTime: true`）。

2. **`StarChartPanel.cs`（修改）**：新增 `[SerializeField] LoadoutSwitcher _loadoutSwitcher` 和 `[SerializeField] RectTransform _loadoutCard` 引用；`Open()` 动画序列加入 LoadoutCard 从下方滑入动画（OutBack，0.25s）；`Bind()` 中初始化 LoadoutSwitcher；`OnDestroy()` 中取消订阅 `OnLoadoutChanged`。

3. **`StarChartTheme.cs`（修改）**：新增 `BgPanel`（`#0b1120`）和 `BgTrack`（`#0f1828`）颜色；更新 `CornerBracket` 为青色 `#22d3ee`（原为白色低透明度）。

4. **`UICanvasBuilder.cs`（重构）**：
   - `BuildStarChartSection`：布局改为 Header / LoadoutSection(GatlingCol + LoadoutCard) / SectionDivider / InventorySection / StatusBar；面板背景改用 `BgPanel`；自动连线 `_loadoutSwitcher` 和 `_loadoutCard`。
   - `BuildTrackView`：完全重写，改为生成 4 个 `TypeColumn` 组件（每列含列头 + 2×2 GridContainer），连线到 `_sailColumn/_prismColumn/_coreColumn/_satColumn`；移除旧的 `_sailCell/_prismCells/_coreCells/_satCells` 字段引用。
   - 新增 `BuildTypeColumn`：生成单个 TypeColumn（列头 dot+label + 2×2 GridLayout + 4 个 SlotCellView），连线 `_columnLabel/_columnDot/_columnBorder/_cells`。
   - 新增 `BuildGatlingCol`：生成 ▲ Button、DrumCounter TMP_Text、▼ Button、LOADOUT 标签，挂载并连线 `LoadoutSwitcher`。
   - 新增 `BuildLoadoutCardHeader`：生成 ◈ 图标 + Loadout 名称 + 副标题。
   - `BuildHeader`：更新标题为 "CANARY — STAR CHART CALIBRATION SYSTEM"，新增 "SYSTEM ONLINE" 徽章（绿色）。
   - 状态栏文字更新为 "EQUIPPED 0/10 · INVENTORY 0 ITEMS · DRAG TO EQUIP · CLICK TO INSPECT"。

### 目的
对齐 `Tools/StarChartUIPrototype.html` 原型的布局架构，实现 4 列 TypeColumn 2×2 网格、Loadout 切换器（Gatling 列）、深色科幻视觉风格。

### 技术方案
- `TypeColumn` 作为 `MonoBehaviour` 挂载在独立 GameObject 上，通过 `UICanvasBuilder` 的 `WireField` 自动连线到 `TrackView` 的 `_sailColumn/_prismColumn/_coreColumn/_satColumn` 字段
- `LoadoutSwitcher` 通过 `event Action<int> OnLoadoutChanged` 与 `StarChartPanel` 解耦通信
- 所有动画使用 PrimeTween + `useUnscaledTime: true`，兼容游戏暂停状态
- `UICanvasBuilder` 保持幂等性（`GetOrCreate` 模式），重复执行不创建重复对象
- `TrackView.TypeColumn` 已实现 `IPointerEnterHandler/IPointerExitHandler`，hover 时边框从 dim 色渐变到亮色（Task 8 已完成）
- `DragDropManager` 已支持 TypeColumn 目标检测（Task 7 已完成）

### 验收状态
- ✅ Task 1：TrackView 已有完整 TypeColumn 4列结构（代码已存在，无需修改）
- ✅ Task 2：SlotCellView 已有完整 overlay 渲染逻辑（代码已存在，无需修改）
- ✅ Task 3：LoadoutSwitcher.cs 新建完成
- ✅ Task 4：StarChartPanel.cs 添加 LoadoutSwitcher 引用和 LoadoutCard 动画
- ✅ Task 5：UICanvasBuilder.cs 重构完成，生成新布局层级
- ✅ Task 6：视觉风格对齐（BgPanel/BgTrack/CornerBracket/Header/StatusBar）
- ✅ Task 7：DragDropManager 已支持 TypeColumn（代码已存在，无需修改）
- ✅ Task 8：TypeColumn hover 动画已实现（代码已存在，无需修改）
- ⚠️ Task 9：需在 Unity Editor 中执行 `ProjectArk > Build UI Canvas` 进行 Play Mode 验收

---

---

## StarChart UI 布局重构 — 字段名修复 — 2026-03-01 00:39

### 修改文件
- `Assets/Scripts/UI/Editor/UICanvasBuilder.cs`

### 内容简述
修复 `BuildGatlingCol` 方法中 `WireField` 调用的字段名，对齐 `LoadoutSwitcher.cs` 的实际 `SerializeField` 字段名：

| 旧字段名（错误） | 新字段名（正确） |
|---|---|
| `_btnUp` | `_prevButton` |
| `_btnDown` | `_nextButton` |
| `_drumCounter` | `_drumCounterLabel` |

### 目的
`LoadoutSwitcher.cs` 已有完整实现（含 `SetLoadoutCount`、`SwitchTo`、`PlaySlideAnimation`），字段名与上一轮 `BuildGatlingCol` 中的 `WireField` 调用不一致，会导致运行时连线失败（字段保持 null）。

### 技术方案
`WireField` 通过反射按字段名查找 `SerializeField`，字段名必须与 C# 源码中的私有字段名完全一致。修复后三个字段均可正确连线。

### 验收状态
- ✅ `_prevButton` / `_nextButton` / `_drumCounterLabel` 字段名与 `LoadoutSwitcher.cs` 完全匹配
- ✅ `TrackView._sailColumn/_prismColumn/_coreColumn/_satColumn` 字段名验证通过
- ✅ `StarChartPanel._loadoutSwitcher/_loadoutCard/_panelCanvasGroup` 字段名验证通过
- ✅ `TypeColumn._columnLabel/_columnDot/_columnBorder/_cells` 字段名验证通过
- ✅ 无旧字段残留（`_sailLabel/_prismLabel/_coreCells/_satCells` 等已完全移除）
- ⚠️ 仍需在 Unity Editor 中执行 `ProjectArk > Build UI Canvas` 进行最终 Play Mode 验收

---

---

## StarChart UI 布局重构 — UICanvasBuilder 幂等性与 CanvasGroup 初始化 — 2026-03-01 01:01

### 修改文件
- `Assets/Scripts/UI/Editor/UICanvasBuilder.cs`

### 内容简述
完成任务8：修复 `BuildStarChartSection` 中 `panelCG`（CanvasGroup）添加后缺少初始化的问题。

**修复前**：
```csharp
var panelCG = panelGo.AddComponent<CanvasGroup>();
// 无初始化 → alpha=1, interactable=true, blocksRaycasts=true（Unity 默认值）
```

**修复后**：
```csharp
var panelCG = panelGo.AddComponent<CanvasGroup>();
panelCG.alpha = 0f;
panelCG.interactable = false;
panelCG.blocksRaycasts = false;
```

同时确认幂等性逻辑完整：
- `BuildUICanvas()` 主方法对每个 Section 均先 `GetComponentInChildren<T>()` 检查，已存在则跳过构建并打印 `skipping` 日志
- `FindOrCreateCanvas()` 三段式查找策略（UIManager → sortingOrder=10 → 新建）确保不重复创建 Canvas
- Step 5 `starChartPanel.gameObject.SetActive(false)` 确保构建完成后面板默认隐藏

### 目的
确保通过 `ProjectArk > Build UI Canvas` 构建的 StarChartPanel，在 Play Mode 启动时 CanvasGroup 处于完全隐藏且不拦截输入的初始状态，与 `StarChartPanel.Awake()` 中的初始化逻辑保持一致。

### 技术方案
- CanvasGroup 三属性同步初始化：`alpha=0`（不可见）、`interactable=false`（不响应输入）、`blocksRaycasts=false`（不拦截射线）
- `Open()` 动画完成回调中恢复三属性；`Close()` 动画完成回调中重置三属性
- 幂等性通过 `GetComponentInChildren<T>(includeInactive: true)` 实现，重复执行 Build 不会产生重复节点

### 验收状态
- ✅ CanvasGroup 初始化三属性均已设置
- ✅ 幂等性逻辑完整（每个 Section 均有 skip 保护）
- ✅ `SetActive(false)` 在 Build 完成后执行
- ✅ 与 `StarChartPanel.Awake()` 初始化逻辑一致

---

---

## Bug Fix: C 键无法打开 StarChart — 2026-03-01 01:10

### 修改文件
- `Assets/Scripts/UI/UIManager.cs`

### 根本原因
`UIManager.Awake()` 通过 `FindActionMap("Ship")` + `FindAction("ToggleStarChart")` 拿到了 action 引用，但**从未调用 `shipMap.Enable()`**。`ConfigureUIInputModule()` 只 Enable 了 `UI` map，`Ship` map 始终处于 disabled 状态，导致 C 键的 `performed` 事件永远不触发。

### 修复内容
1. 在 `Awake()` 中 `FindActionMap("Ship")` 之后立即调用 `shipMap.Enable()`
2. 新增 `_inputActions == null` 和 `shipMap == null` 的 null 保护，避免 NullReferenceException 导致整个初始化链断掉

### 验收
- Play Mode 按 C 键 → StarChart 面板正常打开/关闭
- Console 无 NullReferenceException

---

---

## Bug Fix: StarChartController & EnemyBrain NullReferenceException — 2026-03-01 01:20

### 修改文件
- `Assets/Scripts/Combat/StarChart/StarChartController.cs`
- `Assets/Scripts/Combat/Enemy/EnemyBrain.cs`

### 根本原因

**Bug 1 — StarChartController.cs:196**
`Update()` 中直接访问 `_primaryTrack`、`_secondaryTrack`、`_inputHandler`，但这些字段在 `Awake()` 中初始化。若 `Awake()` 因依赖组件缺失（如 `ShipMotor`/`ShipAiming` 未挂载）中途抛出异常，`_primaryTrack`/`_secondaryTrack` 可能未完成赋值。同时 `_satelliteRunners[i].Tick(dt)` 未做 null 元素保护。

**Bug 2 — EnemyBrain.cs:90**
`Update()` 中直接调用 `_stateMachine.Tick(Time.deltaTime)`，但 `_stateMachine` 在 `Start()` 的 `BuildStateMachine()` 中赋值。若 `_entity.Stats`（Inspector 未赋值的 SO）为 null，`Awake()` 中 `_stats = _entity.Stats` 返回 null，后续 State 构造函数访问 `_stats` 时抛出异常，导致 `_stateMachine` 永远未被赋值，`Update()` 第90行就 NullReferenceException。

### 修复内容

**StarChartController.Update()**
1. 方法开头加 `if (_primaryTrack == null || _secondaryTrack == null) return;` 保护
2. satellite 循环改为 `_satelliteRunners[i]?.Tick(dt)` 防止 null 元素
3. 开火逻辑前加 `if (_inputHandler == null) return;` 保护

**EnemyBrain.Update()**
1. `CheckThreatResponse()` 调用前加 `if (_stateMachine == null) return;` 保护，防止 `BuildStateMachine()` 未完成时 Tick 崩溃

### 验收
- Play Mode 进入场景，Console 无 NullReferenceException
- 武器正常开火，敌人 AI 正常运行

---

---

## Bug Fix: 按 C 键无法打开星图面板 — 2026-03-01 01:25

### 修改文件
- `Assets/Scripts/Ship/Input/InputHandler.cs`

### 根本原因

`InputHandler.OnDisable()` 中调用了 `shipMap.Disable()`，将整个 Ship ActionMap 禁用。由于 `UIManager` 的 `_toggleStarChartAction` 属于同一个 Ship ActionMap，ActionMap 被禁用后 `ToggleStarChart` Action 也随之失效，导致按 C 键完全没有响应。

触发时机：任何导致 `InputHandler` 组件 `OnDisable()` 被调用的操作（如 GameObject 被 `SetActive(false)`、场景切换等）都会把整个 Ship map 关掉。

### 修复内容

**InputHandler.OnDisable()**
- 移除 `shipMap.Disable()` 调用
- 保留所有事件取消订阅逻辑（`IsFireHeld`/`IsSecondaryFireHeld` 重置保留）
- 添加 `if (_inputActions == null) return;` 保护
- 所有 Action 取消订阅改为 null 条件调用（`?.`）防止 Awake 未完成时 OnDisable 崩溃
- 添加注释说明 Ship ActionMap 生命周期由 UIManager 统一管理

### 架构说明

Ship ActionMap 的 Enable/Disable 生命周期应由 `UIManager.Awake()` 统一管理（已调用 `shipMap.Enable()`）。`InputHandler` 只负责订阅/取消订阅自己关心的 Action 事件，不应干预 ActionMap 的启用状态。

### 验收
- Play Mode 按 C 键正常打开/关闭星图面板
- 飞船移动/射击输入正常
- Console 无报错

---

---

## Bug Fix: C 键无法打开星图面板（根因修复）— 2026-03-01 01:31

### 修改文件
- `Assets/Scripts/UI/UIManager.cs`

### 根本原因

`UIManager.Start()` 中在调用 `_starChartPanel.Bind(...)` 之后，紧接着调用了 `_starChartPanel.gameObject.SetActive(false)`。

这行代码是**多余且有害的**：
1. `StarChartPanel.Awake()` 已经自行调用 `gameObject.SetActive(false)` 完成初始隐藏，无需 UIManager 重复操作
2. 若 `StarChartPanel` 是 UIManager 所在 Canvas 的子节点，`SetActive(false)` 会触发 Canvas 层级上的 `OnDisable()` 回调，导致 `UIManager.OnDisable()` 被意外执行，从而取消订阅 `_toggleStarChartAction.performed`，之后 C 键永远无响应

### 修复内容

移除 `UIManager.Start()` 中的 `_starChartPanel.gameObject.SetActive(false)` 调用，并添加注释说明原因，防止未来被误加回来。

### 验收
- Play Mode 按 C 键正常打开/关闭星图面板
- Console 无 NullReferenceException
- 飞船移动/射击输入正常

---

---

## Fix UICanvasBuilder BuildGatlingCol NullReferenceException — 2026-03-01 10:42

### Modified Files
- `Assets/Scripts/UI/Editor/UICanvasBuilder.cs`

### Summary
Fixed NullReferenceException at line 1013 in `BuildGatlingCol` method caused by adding both `Image` and `TextMeshProUGUI` components to the same `DrumCounter` GameObject. Both inherit from `UnityEngine.UI.Graphic`, so Unity silently returns null on the second `AddComponent` call. Moved the TMP text to a child `Label` object under `DrumCounter`.

### Purpose
Unblock the `ProjectArk → Build UI Canvas` editor menu command so that UIManager and full StarChart UI can be properly built into the scene.

### Technical Approach
- Root cause: `Image` and `TextMeshProUGUI` both extend `Graphic`; Unity forbids two `Graphic` on the same GameObject.
- Fix: Created a child `Label` GameObject under `DrumCounter` to host the `TextMeshProUGUI`, keeping the `Image` background on the parent.

---

---

## StarChart Debug Code Rollback — 2026-03-01 10:50

### Modified Files
- `Assets/Scripts/UI/UIManager.cs`
- `Assets/Scripts/Ship/Input/InputHandler.cs`

### Summary
Rolled back diagnostic/debug code that was added during the multi-round StarChart "C key not working" investigation. The root cause turned out to be a missing UIManager component in the scene (resolved by rebuilding via `Build UI Canvas`), so all diagnostic instrumentation is now obsolete.

### What Was Rolled Back
- **UIManager.cs**: Removed all `Debug.Log` statements with `[UIManager]` prefix (Awake, OnEnable, Start, OnDisable, OnToggleStarChartPerformed). Removed the entire `Update()` method which contained frame-polling diagnostics and a raw-keyboard fallback.
- **InputHandler.cs**: Removed all `Debug.Log` statements with `[InputHandler]` prefix (Awake, OnEnable, ToggleSpaceLife). Restored `shipMap.Disable()` in `OnDisable()` — InputHandler should stop receiving input when disabled; UIManager re-enables the ActionMap via its own `shipMap.Enable()`.

### What Was Preserved (Intentionally Kept)
- **UIManager.cs**: `shipMap.Enable()` call in Awake (correct architectural responsibility), `ConfigureUIInputModule()` method (ensures InputSystemUIInputModule is wired up), `SetActive(false)` prevention comment in Start, `Debug.LogError`/`Debug.LogWarning` for real configuration errors.
- **InputHandler.cs**: `if (x != null)` null-check pattern replacing `?.performed` (fixes real CS0079 compile error).
- **StarChartController.cs**: Null protection guards in `Update()` (line 196) — defensive programming against missing dependencies.
- **EnemyBrain.cs**: `_stateMachine == null` guards — defensive programming against `BuildStateMachine` failure.
- **UICanvasBuilder.cs**: DrumCounter Image/TMP child object separation — fixes real bug where two `Graphic` components can't coexist on one GameObject.

### Purpose
Clean up Console noise and remove dead diagnostic code while preserving all meaningful bug fixes and architectural improvements made during the investigation.

### Technical Approach
- Selective rollback: evaluated each change individually against "was this diagnostic-only or a real fix?" criteria.
- Restored `shipMap.Disable()` with explanatory comment about UIManager's role in re-enabling the ActionMap.
- Kept `Debug.LogError`/`Debug.LogWarning` for genuine configuration problems (null `_inputActions`, missing ActionMaps).

---

---

## Bug Fix: Star Chart panel invisible on open (screen freeze) — 2026-03-01 10:54

### Modified Files
- `Assets/Scripts/UI/UIManager.cs`
- `Assets/Scripts/UI/StarChartPanel.cs`
- `Assets/Scripts/UI/StatusBarView.cs`

### Summary
Fixed the screen-freeze bug when pressing C to open the Star Chart panel. The game appeared to freeze because `Time.timeScale = 0` was set **before** `StarChartPanel.Open()`, causing PrimeTween to start animations on not-yet-active GameObjects in the hierarchy. The panel's alpha stayed at 0 (invisible) while the game was paused.

### Root Cause
1. **Timing order in `UIManager.OpenPanel()`**: `Time.timeScale = 0f` was called before `_starChartPanel?.Open()`. Although panel tweens used `useUnscaledTime: true`, the `SetActive(true)` call and subsequent `RefreshAll()` triggered child component initializations while the hierarchy was still settling, producing PrimeTween "Tween is started on GameObject that is not active in hierarchy" warnings for StarChartPanel, StatusLabel, and LoadoutCard.
2. **Zero-duration tween in `UpdateStatusBar()`**: Called `ShowMessage(..., duration: 0f)` which created a `ChainDelay(0)` triggering "Tween duration (0) <= 0" warning.

### Fix
- **`UIManager.OpenPanel()`**: Reordered to open the panel first, then set `Time.timeScale = 0f`, then trigger the weaving transition. This ensures all GameObjects are active in hierarchy when PrimeTween creates its animation sequences.
- **`StarChartPanel.UpdateStatusBar()`**: Replaced `ShowMessage(..., 0f)` with new `SetText()` method that sets text/color instantly without animation.
- **`StatusBarView.SetText()`**: New public method for instant text updates with no tween, avoiding the zero-duration warning.

---

---

## Bug Fix: PrimeTween "not active in hierarchy" warning on StarChartPanel.Open — 2026-03-01 11:00

### Modified Files
- `Assets/Scripts/UI/StarChartPanel.cs`

### Summary
Eliminated the PrimeTween "Tween is started on GameObject that is not active in hierarchy: StarChartPanel" warning that persisted even after the OpenPanel reorder fix. The warning was caused by PrimeTween checking `activeInHierarchy` in the same frame as `SetActive(true)`, before Unity had fully propagated the hierarchy activation.

### Root Cause
When `gameObject.SetActive(true)` is called and a Tween is created in the same synchronous call stack, PrimeTween's internal check sees the target as not yet fully active in the hierarchy. Unity's `activeInHierarchy` may not update until the end of the frame for freshly-activated objects that were previously `SetActive(false)` in `Awake()`.

### Fix
- Split `Open()` into synchronous setup (SetActive, RefreshAll, set alpha=0, fire OnOpened) and an async `PlayOpenAnimationAsync()` method.
- `PlayOpenAnimationAsync()` uses `await UniTask.Yield(PlayerLoopTiming.Update, destroyCancellationToken)` to defer Tween creation by one frame, ensuring the hierarchy is fully active when PrimeTween starts.
- Added `using Cysharp.Threading.Tasks` import.
- Fire-and-forget pattern via `.Forget()` per project async conventions.

---

---

## Bug Fix: PrimeTween "not active in hierarchy" — Round 2 (Canvas.ForceUpdateCanvases) — 2026-03-01 11:04

### Modified Files
- `Assets/Scripts/UI/StarChartPanel.cs`

### Summary
The previous fix (UniTask.Yield one frame) did not resolve the warning because `activeInHierarchy` was still `false` after the yield — indicating the panel was being deactivated between `Open()` and the next frame. Root cause: Unity defers Canvas hierarchy propagation; `activeInHierarchy` is not guaranteed to be `true` in the same frame as `SetActive(true)` when the object was previously inactive.

### Fix
- Reverted the async `PlayOpenAnimationAsync` approach.
- Restored synchronous Tween creation inside `Open()`.
- Added `Canvas.ForceUpdateCanvases()` immediately after `gameObject.SetActive(true)` and before any Tween creation. This forces Unity to immediately propagate the `SetActive(true)` through the Canvas hierarchy, ensuring `activeInHierarchy == true` when PrimeTween checks it.
- Removed the now-unused `using Cysharp.Threading.Tasks` import.
- Removed the dead `PlayOpenAnimationAsync()` method.

---

---

## Bug Fix: StarChart UI Layout — Primary/Secondary 上下堆叠 + 面板固定尺寸 — 2026-03-01 11:10

### Modified Files
- `Assets/Scripts/UI/Editor/UICanvasBuilder.cs`

### Summary
StarChart UI 布局与 HTML 原型不符，主要有两个问题：
1. `TracksArea` 内 `PrimaryTrackView` 和 `SecondaryTrackView` 是**左右分割**（各占50%宽度），但原型中应为**上下堆叠**（各占50%高度）。
2. 面板使用 `SetStretch` 铺满全屏，但原型是 980×700 固定尺寸居中显示。
3. `TrackLabel`（PRIMARY [LMB]）与 `ColumnHeader`（SAIL/PRISM/CORE/SAT）都在顶部，重叠显示。

### Fix
- `BuildStarChartSection`：将面板改为 980×700 固定尺寸，锚点居中 (0.5, 0.5)。
- `TracksArea`：Primary 改为上半 `(0,0.52)~(1,1)`，Secondary 改为下半 `(0,0)~(1,0.49)`，中间加水平分割线。
- `BuildTrackView`：将 `TrackLabel` 移到左侧固定列 `(0,0)~(0.08,1)`，4 列 SAIL/PRISM/CORE/SAT 从 x=0.09 开始，与原型的 `track-block-label + track-rows` 布局一致。

---

---

## Cleanup: 移除无效的 Canvas.ForceUpdateCanvases() 修复 — 2026-03-01 11:19

### Modified Files
- `Assets/Scripts/UI/StarChartPanel.cs`
- `Assets/Scripts/UI/UIManager.cs`

### Summary
发现 StarChartPanel 画面停滞的真正根因是 UICanvasBuilder 在 Build 后调用 `starChartPanel.gameObject.SetActive(false)`，导致面板在场景中默认 inactive。之前针对 PrimeTween "not active in hierarchy" 警告的多轮修复中，`Canvas.ForceUpdateCanvases()` 是无效的（它只刷新 Canvas 布局，无法解决 inactive 问题），属于冗余代码。

### 保留的必要修复
- `UIManager.OpenPanel()`：先调用 `Open()` 再设置 `Time.timeScale = 0f`（确保 Tween 在 timeScale=0 前创建）
- `StarChartPanel.Awake()`：`gameObject.SetActive(false)` 防御性保护
- `StatusBarView.SetText()`：避免 duration=0 触发 PrimeTween 警告的干净 API

---

---

## StarChart UI 布局修复 — 对齐 HTML 原型 (2026-03-01 11:32)

### 修改文件
- `Assets/Scripts/UI/Editor/UICanvasBuilder.cs`

### 内容简述
对 `BuildStarChartSection` 和 `BuildTrackView` 中的所有布局锚点参数进行系统性修正，使 Unity 中的 StarChartPanel 与 `Tools/StarChartUIPrototype.html` 完全一致。

### 目的
消除之前因布局参数不准确导致的：LoadoutSection 过高、InventorySection 被压缩、背包格子过大、TrackLabel 偏宽等视觉问题。

### 技术方案

**1. 整体高度比例（基于面板 700px）**
- Header: `y 0.940 → 1.0`（42px，6%）
- DividerH: `y 0.938 → 0.940`
- LoadoutSection: `y 0.487 → 0.938`（317.5px，45.4%，flex:1）
- SectionDivider: `y 0.485 → 0.487`（1px）
- InventorySection: `y 0.031 → 0.485`（317.5px，45.4%，flex:1）
- StatusBar: `y 0.0 → 0.031`（22px，3.1%）

**2. LoadoutCard 内部比例**
- CardHeader: `y 0.91 → 1.0`（约 28px，8.8%）
- TracksArea: `y 0.0 → 0.90`（剩余 90%）

**3. InventorySection 全宽布局**
- InventoryView 改为全宽：`x 0.01 → 0.99`（原为左 60%）
- ItemDetailView 设为 `SetActive(false)`（HTML 原型无右侧详情面板分割）

**4. 背包格子尺寸**
- `GridLayoutGroup.cellSize`: `(100, 120)` → `(44, 44)`（HTML: `--inv-slot-size: 44px`）
- `GridLayoutGroup.spacing`: `(6, 6)` → `(2, 2)`（HTML: `--slot-gap: 2px`）
- `GridLayoutGroup.constraintCount`: `4` → `12`（全宽约 960px，12列合适）
- `InventoryItemView` prefab `sizeDelta`: `(100, 120)` → `(44, 44)`

**5. TrackLabel 宽度与 TypeColumn 起始位置**
- TrackLabel: `x 0 → 0.08` → `x 0 → 0.07`（HTML: 64px fixed，64/911px ≈ 7%）
- DividerLabel: `x 0.079 → 0.081` → `x 0.069 → 0.071`
- TypeColumn 起始 x: `0.09` → `0.08`，列间距均匀分布（SAIL 0.08-0.29，PRISM 0.30-0.51，CORE 0.52-0.73，SAT 0.74-0.99）

---

---

## 星图多 Loadout 系统 — 2026-03-01 11:54

### 新建文件
- `Assets/Scripts/Combat/StarChart/LoadoutSlot.cs`

### 修改文件
- `Assets/Scripts/Combat/StarChart/StarChartController.cs`
- `Assets/Scripts/Core/Save/SaveData.cs`
- `Assets/Scripts/UI/StarChartPanel.cs`

### 内容简述
实现星图面板 3 套独立 Loadout 配置的完整切换系统。

### 目的
玩家可在星图面板通过 ▲/▼ 按钮在 3 套装备配置间自由切换，每套配置拥有完全独立的 Primary/Secondary Track、LightSail 和 Satellites 数据，切换时 UI 同步刷新，存档持久化全部 3 套配置。

### 技术方案
1. **LoadoutSlot.cs**：新建纯 C# 类，封装单套装备配置（PrimaryTrack、SecondaryTrack、EquippedLightSailSO、EquippedSatelliteSOs），提供 `Clear()` 方法。
2. **StarChartController 重构**：
   - 用 `LoadoutSlot[3]` + `LightSailRunner[3]` + `List<SatelliteRunner>[3]` 替换原有单套字段
   - `PrimaryTrack`/`SecondaryTrack`/`GetEquippedLightSail()`/`GetEquippedSatellites()` 均通过 `ActiveSlot` 属性代理到当前激活槽位
   - 新增 `SwitchLoadout(int index)`：Dispose 旧槽位 Runner → 切换索引 → RebuildSlotRunners → InitializeAllPools
   - Debug Loadout 数据只加载到槽位 0
   - `ExportToSaveData` 序列化全部 3 个槽位；`ImportFromSaveData` 支持新格式（Loadouts 列表）和旧格式（自动迁移到槽位 0）
3. **SaveData.cs 扩展**：新增 `LoadoutSlotSaveData` 类；`StarChartSaveData` 添加 `List<LoadoutSlotSaveData> Loadouts` 字段，旧字段标记 `[Obsolete]` 保留用于迁移。
4. **StarChartPanel 联动**：`SetLoadoutCount(1)` → `SetLoadoutCount(3)`；`HandleLoadoutChanged` 实现完整切换逻辑（SwitchLoadout → TrackView.Bind → UpdateTrackSelection → InventoryView.Refresh → UpdateStatusBar）；`IsItemEquipped` 注释更新为"只检查当前激活槽位"。

---

---

## Bug Fix: LoadoutSwitcher.Start() 覆盖多Loadout设置 — 2026-03-01 12:00

### 修改文件
- `Assets/Scripts/UI/LoadoutSwitcher.cs`

### 内容简述
移除 `LoadoutSwitcher.Start()` 中硬编码的 `SetLoadoutCount(1)` 调用。

### 目的
`Start()` 中的 `SetLoadoutCount(1)` 会在运行时覆盖 `StarChartPanel.Bind()` 中设置的 `SetLoadoutCount(3)`，导致 ▲/▼ 按钮被禁用，Loadout 切换功能失效。

### 技术方案
删除 `Start()` 方法体，改为注释说明 `SetLoadoutCount` 由外部 `StarChartPanel.Bind()` 调用。`UICanvasBuilder` 已正确 Wire 所有字段（`_loadoutSwitcher`、`_loadoutCard`、`_prevButton`、`_nextButton`、`_drumCounterLabel`），无需额外修改。

---

---

## Feature: 示巴星 13 个星图部件完整实现 — 2026-03-01 12:20

### 新建文件
- `Assets/Scripts/Combat/Projectile/HomingModifier.cs` — 制导棱镜 IProjectileModifier 实现
- `Assets/Scripts/Combat/Projectile/MinePlacerModifier.cs` — 布雷棱镜 IProjectileModifier 实现
- `Assets/Scripts/Combat/StarChart/Satellite/AutoTurretBehavior.cs` — 自动机炮伴星 SatelliteBehavior 实现
- `Assets/Scripts/Combat/Editor/ShebaAssetCreator.cs` — Editor 一键资产创建工具

### 修改文件
- `Assets/Scripts/Combat/Projectile/Projectile.cs` — 新增 `LifetimeRemaining` 可读写属性（供 MinePlacerModifier 延长生命周期）

### 内容简述

**HomingModifier**：继承 `IProjectileModifier`，在 `OnProjectileUpdate` 中每帧用 `Physics2D.OverlapCircleAll` 检测 45° 锥角内最近敌人，以 `_turnSpeed`（默认 180°/s）通过 `Vector2.MoveTowards` 平滑旋转子弹方向，同步更新 `Rigidbody2D.linearVelocity` 和 `transform.rotation`。

**MinePlacerModifier**：继承 `IProjectileModifier`，在 `OnProjectileSpawned` 时将子弹速度归零（`Speed = 0`，`rb.linearVelocity = Vector2.zero`），并将 `Projectile.LifetimeRemaining` 乘以 3 倍延长存活时间。`OnProjectileUpdate` 每帧防漂移保持静止。

**AutoTurretBehavior**：继承 `SatelliteBehavior`，`EvaluateTrigger` 检测 15 单位范围内是否有敌人，`Execute` 从飞船位置向最近敌人发射 Matter 家族低伤害子弹（通过 `PoolManager` 对象池）。`Initialize` 预热对象池，`Cleanup` 清空引用。

**ShebaAssetCreator**：Editor 工具，菜单 `ProjectArk > Create Sheba Star Chart Assets`，幂等创建：
- 3 个新 Modifier Prefab（Modifier_Homing、Modifier_MinePlacer、Sat_AutoTurret）
- 4 个星核 SO（ShebaCore_MachineGun/FocusLaser/Shotgun/PulseWave）
- 6 个棱镜 SO（ShebaP_TwinSplit/RapidFire/Bounce/Boomerang/Homing/MinePlacer）
- 2 个光帆 SO（ShebaSail_Standard/Scout）
- 1 个伴星 SO（ShebaSat_AutoTurret）
- 自动追加所有新 SO 到 PlayerInventory.asset，Console 输出「Created X / Skipped Y」摘要

### 目的
完整实现示巴星关卡的 13 个星图部件，覆盖 4 种核心攻击风格（连发/激光/散弹/波纹）和 6 种棱镜机制（双生/连射/反弹/回旋/制导/布雷），以及斥候帆和自动机炮伴星。

### 技术方案
- HomingModifier 使用 `Vector2.MoveTowards` 而非直接赋值，保证平滑转向不产生跳变；LayerMask 显式声明，禁 `~0`。
- MinePlacerModifier 通过新增的 `Projectile.LifetimeRemaining` 属性修改运行时副本，不触碰 SO 原始数据，遵循「运行时数据隔离」原则。
- AutoTurretBehavior 遵循 SatelliteBehavior IF-THEN 模式，`EvaluateTrigger` 负责条件判断，`Execute` 负责执行，冷却由 SatelliteRunner 统一管理（1.5s）。
- ShebaAssetCreator 复用 Batch5AssetCreator 的 `SetField` 模式（SerializedObject 反射写入私有字段），幂等检查用 `AssetDatabase.LoadAssetAtPath`，库存追加用 `FindAssets("t:X Sheba")` 过滤名称前缀。

---

---

## Fix: TypeColumn 提取为独立类 + UICanvasBuilder 自动 wire _itemPrefab — 2026-03-01 16:51

### 修改文件
- **新建** `Assets/Scripts/UI/TypeColumn.cs`
- **修改** `Assets/Scripts/UI/TrackView.cs`
- **修改** `Assets/Scripts/UI/Editor/UICanvasBuilder.cs`

### 问题描述
1. `SailColumn / PrismColumn / CoreColumn / SatColumn` 在 Inspector 中显示 "the associated script cannot be loaded"
2. `InventoryView._itemPrefab` 在 Build UI Canvas 后始终为 Missing

### 根本原因
1. `TypeColumn` 是 `TrackView` 的**内部类（nested class）**。Unity 对内部类 MonoBehaviour 的 GUID 序列化不稳定——当场景被重建时，fileID 哈希可能与已保存场景中的值不一致，导致脚本引用失效。
2. `Build UI Canvas` 工具只创建 InventoryView 节点，但不 wire `_itemPrefab`；该字段只在 `Create InventoryItemView Prefab` 时才被赋值，两步操作之间没有联动。

### 技术方案
1. **提取 TypeColumn 为独立顶级类**：新建 `TypeColumn.cs`，将完整实现从 `TrackView` 内部类移出，保持 `ProjectArk.UI` namespace 不变。`TrackView.cs` 中删除内部类定义，保留对 `TypeColumn` 的字段引用（同 namespace 无需 using）。移除 `TrackView.cs` 中不再需要的 `using PrimeTween`。
2. **UICanvasBuilder 改用独立 TypeColumn**：`BuildTypeColumn()` 方法签名和 `AddComponent<>` 从 `TrackView.TypeColumn` 改为 `TypeColumn`。
3. **Build UI Canvas 自动 wire _itemPrefab**：在 `BuildUICanvas()` 末尾新增 Step 7，检查 `Assets/_Prefabs/UI/InventoryItemView.prefab` 是否存在，若存在则自动 wire 到场景中的 `InventoryView._itemPrefab`。

### 操作指南
- 删除旧 Canvas → 执行 `ProjectArk → Build UI Canvas` → 所有 TypeColumn 脚本引用正确，_itemPrefab 自动 wire（如果 prefab 已存在）
- 若 prefab 不存在，先执行 `ProjectArk → Create InventoryItemView Prefab`，再执行 `Build UI Canvas`

---

---

## Bug Fix: ScrollArea Mask Image alpha=0 导致 Inventory 部件全部不可见 — 2026-03-01 18:51

### 修改文件
- `Assets/Scenes/SampleScene.unity`（修复 ScrollArea Image 组件 alpha 值）
- `Assets/Scripts/UI/Editor/UICanvasBuilder.cs`（修复代码层，防止重建时复现）

### 问题描述
星图面板底部 Inventory 区域完全空白，数据已正确加载（`INVENTORY 21 ITEMS`），`_itemPrefab` 也已正确赋值，但仍然看不到任何部件卡片。

### 根本原因
`ScrollArea` 节点上的 `Mask` 组件依赖同节点的 `Image` 组件作为 stencil 遮罩。`UICanvasBuilder` 中使用了 `scrollGo.AddComponent<Image>().color = Color.clear`（alpha=0），而 **uGUI Mask 使用 Image 的 alpha 通道作为 stencil buffer**——alpha=0 意味着遮罩完全透明，导致 Mask 裁剪掉所有子内容（ContentParent 下的所有 InventoryItemView 卡片全部被裁掉，不渲染）。

这是 CLAUDE.md 中记录的「常见陷阱第4项：uGUI Mask 裁剪失效（alpha=0）」。

### 技术方案
1. **场景文件直接修复**：将 `SampleScene.unity` 中 `ScrollArea` 的 `Image` 组件（fileID: 1947166889）的 `m_Color.a` 从 `0` 改为 `1`。`Mask.showMaskGraphic = false` 保持不变（视觉上不显示黑色背景，但 stencil 正常工作）。
2. **UICanvasBuilder 代码修复**：将 `scrollGo.AddComponent<Image>().color = Color.clear` 改为显式设置 `alpha=1`，并添加注释说明原因，防止未来重建 Canvas 时复现。

---

---

## StarChartPanel 初始化修复 — C键首次无效 Bug (2026-03-01 19:30)

### 修改文件
- `Assets/Scripts/UI/StarChartPanel.cs`
- `Assets/Scripts/UI/Editor/UICanvasBuilder.cs`

### 问题描述
每次删除场景 Canvas 并执行 `BuildUICanvas` 后，必须手动在 Hierarchy 中将 `StarChartPanel` 从 inactive 切换为 active，C 键才能正常打开星图面板。

### 根本原因
`UICanvasBuilder.BuildUICanvas()` Step 5 在 Editor 模式下调用 `starChartPanel.gameObject.SetActive(false)`，将 `inactive` 状态序列化进场景文件。进入 Play Mode 后 `Awake()` 不会被调用。当 C 键触发 `Open()` → `gameObject.SetActive(true)` 时，`Awake()` 才首次执行，而 `Awake()` 内部又调用了 `gameObject.SetActive(false)`，导致面板立刻被关闭，C 键永久失效。

### 技术方案
1. **`StarChartPanel.Awake()`**：移除 `gameObject.SetActive(false)`，改为仅初始化 CanvasGroup（alpha=0, interactable=false, blocksRaycasts=false）。新增 `private bool _isOpen = false` 字段追踪面板开关状态。
2. **`StarChartPanel.IsOpen`**：从 `gameObject.activeSelf` 改为返回 `_isOpen` 字段，确保状态判断不依赖 GameObject active 状态。
3. **`StarChartPanel.Open()`**：移除 `gameObject.SetActive(true)`，改为设置 `_isOpen = true`。
4. **`StarChartPanel.Close()`**：移除 `ChainCallback` 和 else 分支中的 `gameObject.SetActive(false)`，改为设置 `_isOpen = false`。
5. **`UICanvasBuilder` Step 5**：移除 `starChartPanel.gameObject.SetActive(false)`，改为获取 CanvasGroup 并设置 alpha=0, interactable=false, blocksRaycasts=false，确保 GameObject 始终保持 active。

### 目的
`StarChartPanel` 在场景中始终保持 active，`Awake()` 在场景加载时正常执行完毕，C 键无需任何手动操作即可立即使用。视觉隐藏完全依赖 CanvasGroup，不再依赖 `SetActive`。

---

---

## StarChart Shape-Aware Drag Ghost & Tooltip System — 2026-03-01 20:35

### 新建文件
- `Assets/Scripts/UI/TooltipContentBuilder.cs` — 静态工具类，根据 StarChartItemSO 子类型生成属性行文本（Core/Prism/LightSail/Satellite + HeatCost）

### 修改文件
- `Assets/Scripts/UI/DragDrop/DragGhostView.cs` — 完全重写：新增 `SetShape(int slotSize)` 方法，动态调整 Ghost 高度并生成对应数量的半透明格子网格 Image；新增 `DropPreviewState` 枚举；`Show()` 自动调用 `SetShape()`；`SetDropState()` 更新边框颜色和替换提示
- `Assets/Scripts/UI/DragDrop/DragDropManager.cs` — `BeginDrag()` 调用 `SetShape()`；`HighlightMatchingColumns()` 改用 `SetDropCandidate()` 呼吸脉冲；新增 `PlaySnapInAnimation()` snap-in 弹入动画（1.18→0.96→1.0）；拖拽开始/结束时通知 `ItemTooltipView.SetDragSuppressed()`
- `Assets/Scripts/UI/TypeColumn.cs` — 新增 `SetDropPreview(DropPreviewState)` 列级预览高亮；新增 `SetDropCandidate(bool)` 呼吸脉冲候选高亮（PrimeTween Yoyo 循环）；`Initialize()` 注入 `OwnerColumn` 到每个 cell
- `Assets/Scripts/UI/SlotCellView.cs` — 新增 `OwnerColumn` 属性；`OnPointerEnter()` 同步调用 `OwnerColumn.SetDropPreview()`；`ClearDropTargetNextFrame()` 恢复列预览为 None
- `Assets/Scripts/UI/ItemTooltipView.cs` — 完全重写：UniTask 150ms 延迟显示；PrimeTween 淡入淡出；屏幕边界检测（超出右/下边界自动翻转）；拖拽期间屏蔽显示；`PopulateContent()` 填充图标/名称/类型/属性/描述/装备状态/操作提示
- `Assets/Scripts/UI/TrackView.cs` — 新增 `OnCellPointerEntered(StarChartItemSO, string)` 和 `OnCellPointerExited` 事件；`InitColumn()` 订阅 SlotCellView hover 事件并转发；`HandleCellPointerEnter()` 构建 "PRIMARY · CORE" 格式的位置字符串
- `Assets/Scripts/UI/StarChartPanel.cs` — 新增 `[SerializeField] ItemTooltipView _tooltipView`；新增 `ShowTooltip()`/`HideTooltip()`/`GetEquippedLocation()` 公共 API；`Bind()` 订阅 TrackView 和 InventoryView 的 hover 事件；`OnDestroy()` 取消订阅
- `Assets/Scripts/UI/InventoryView.cs` — 新增 `OnItemPointerEntered`/`OnItemPointerExited` 事件；`Refresh()` 订阅每个 InventoryItemView 的 hover 事件；`ClearViews()` 取消订阅
- `Assets/Scripts/UI/Editor/UICanvasBuilder.cs` — `BuildStarChartSection()` 新增 ItemTooltipView 完整 UI 层级构建（背景/边框/类型徽章/图标+名称行/属性文本/描述/装备状态/操作提示）；Wire `_tooltipView` 到 StarChartPanel

### 内容简述
实现了两大功能：
1. **Shape-Aware 拖拽幽灵**：Ghost 根据 SlotSize 动态调整高度，生成 N 个半透明格子网格；拖拽开始时匹配类型的 TypeColumn 显示呼吸脉冲候选高亮；悬停时 Ghost 边框和列边框同步显示 Valid/Replace/Invalid 三色预览；放置成功后播放 snap-in 弹入动画（scale 1.18→0.96→1.0）
2. **Tooltip 悬停详情卡**：150ms 延迟显示，PrimeTween 淡入淡出，屏幕边界自动翻转，拖拽期间屏蔽；属性内容按类型分别展示（Core 显示 DAMAGE/FIRE RATE/SPEED，Prism 显示 StatModifiers 列表，LightSail 显示条件+效果描述，Satellite 显示触发+动作+冷却）

### 目的
对齐 StarChartUIPrototype.html 原型的两大核心交互差距：异形部件拖拽系统和 Tooltip 悬停详情卡。

### 技术方案
- **DragGhostView.SetShape()**：动态销毁/重建 GhostCell_N Image 数组，每个 cell 使用 `anchorMin=(0,1), anchorMax=(1,1), pivot=(0.5,1)` 从顶部向下排列，高度 = N×cellHeight + (N-1)×cellGap
- **TypeColumn.SetDropCandidate()**：PrimeTween `Tween.Color` Yoyo 循环（cycles=-1），dim→typeColor(0.6α)→dim，duration=0.7s
- **ItemTooltipView**：`UniTask.Delay(150ms)` 替代 Coroutine；`CanvasGroup.alpha` 控制显隐（永不 SetActive(false)）；屏幕边界检测通过 `Screen.width/height` 与 tooltip 尺寸比较后翻转
- **TooltipContentBuilder**：C# pattern matching `switch(item)` 分派到各子类型的 BuildXxxStats 方法，StringBuilder 拼接多行属性文本

---

---

## Bug Fix: ItemTooltipView 使用旧 Input API 导致异常 — 2026-03-01 21:11

### 修改文件
- `Assets/Scripts/UI/ItemTooltipView.cs`

### 问题描述
`PositionNearMouse()` 方法中使用了 `UnityEngine.Input.mousePosition`，项目已切换至 New Input System，导致运行时抛出 `InvalidOperationException`。

### 修复方案
- 添加 `using UnityEngine.InputSystem;`
- 将 `Input.mousePosition` 替换为 `Mouse.current.position.ReadValue()`
- 添加 `Mouse.current == null` 守卫，防止无鼠标设备时崩溃

---

---

## Tooltip 定位修复（坐标系不匹配）— 2026-03-01 21:18

### 修改文件
- `Assets/Scripts/UI/ItemTooltipView.cs`

### 问题描述
`PositionNearMouse()` 经历了两次错误修复：
1. 第一次将 `rect.localPosition` 改为 `rect.anchoredPosition`，但 `ScreenPointToLocalPointInRectangle` 的参考矩形是根 Canvas，而 `anchoredPosition` 是相对于父节点的坐标，两者坐标系不一致导致位置偏移。
2. 第二次改为用父节点作为参考矩形，但 `anchoredPosition` 还受 anchor 设置影响，tooltip 完全不可见。

### 修复方案
改用 `ScreenPointToWorldPointInRectangle` + `rect.position`（世界坐标），完全绕开 `anchoredPosition` 和 anchor/pivot 偏移的影响。无论 tooltip 嵌套多深、anchor 如何设置，世界坐标都能正确对应鼠标位置。

---

---

## SpaceLife 过渡动画优化 — 2026-03-01 23:21

### 修改文件
- `Assets/Scripts/SpaceLife/TransitionUI.cs`
- `Assets/Scripts/SpaceLife/SpaceLifeManager.cs`

### 内容简述
将按 Tab 进入/退出 SpaceLife 的过渡动画从"打字机文字 + 长等待"重构为"快速淡黑→切换→淡出"三段式结构，总时长约 300ms，与按 C 打开星图的手感对齐。

### 目的
消除拖泥带水的打字机动画（原约 3-4 秒），提升操作响应感。

### 技术方案
**TransitionUI.cs**：
- 移除 `_centerText`、`_typewriterSpeed`、`_textDisplayDuration` 字段
- 移除 `TypeTextAsync`、`PlayEnterTransitionAsync`、`PlayExitTransitionAsync`、`PlayTransitionAsync` 方法
- `_fadeDuration` 默认值从 0.3f 改为 0.15f
- `FadeOutAsync`/`FadeInAsync` 签名简化为无 `duration` 参数，使用内部 `_fadeDuration`，`useUnscaledTime: true`

**SpaceLifeManager.cs**：
- 移除 `_enterText`/`_exitText` Inspector 字段
- `EnterSpaceLifeAsync`/`ExitSpaceLifeAsync` 改为三段式：`FadeOutAsync` → 场景切换（摄像机/根节点/InputHandler 切换）→ `FadeInAsync`
- 场景切换操作夹在两段动画之间，避免视觉撕裂
- 保留 `_isTransitioning` 保护逻辑防止重复触发

---

## StarChart Tooltip 视觉优化 — 2026-03-01 23:30

### 修改文件
- `Assets/Scripts/UI/Editor/UICanvasBuilder.cs`
- `Assets/Scripts/UI/ItemTooltipView.cs`

### 内容简述
放大 StarChart Tooltip 整体尺寸与所有文字字号，并将背景色从白色改为科技感深蓝色风格。

### 目的
提升星图 Tooltip 的可读性与视觉质感，使其与游戏整体科技感 UI 风格一致。

### 技术方案
**UICanvasBuilder.cs**：
- `tooltipRect.sizeDelta`：`(220, 180)` → `(280, 230)`
- `TooltipBackground` 背景色：`(0.05, 0.07, 0.11, 0.97)` → `(0.04, 0.07, 0.14, 0.97)`（更深的科技蓝）
- `TooltipBorder` 边框色：StarChartTheme.Border → `(0.2, 0.6, 1.0, 0.85)`（亮蓝色边框）
- `TypeBadgeBackground` 背景色：`(0, 0.85, 1, 0.12)` → `(0, 0.5, 1, 0.15)`
- `NameText` 字号：14 → 17
- `TypeText` 字号：10 → 12
- `StatsText` 字号：11 → 13
- `DescriptionText` 字号：10 → 12
- `EquippedStatusText` 字号：10 → 12
- `ActionHintText` 字号：9 → 11

**ItemTooltipView.cs**：
- `_tooltipWidth` 默认值：220f → 280f
- `_tooltipHeight` 默认值：180f → 230f（与新尺寸同步，确保边界检测正确）

---

---

## Slot 尺寸放大（Track + Inventory 统一放大至 80×80）— 2026-03-01 23:44

### 修改文件
- `Assets/Scripts/UI/Editor/UICanvasBuilder.cs`

### 内容简述
将星图面板中 Track 装备槽与背包 Inventory 槽的尺寸统一放大一倍，并保持两者一致。

### 目的
原有 slot 尺寸（40×40 / 44×44）偏小，图标内容难以辨认；放大后视觉更清晰，两区域尺寸统一，提升整体 UI 一致性。

### 技术方案
1. **Track Slot**：`TypeColumn` 内 `GridContainer` 的 `GridLayoutGroup.cellSize` 从 `(40, 40)` 改为 `(80, 80)`；`constraintCount = 2`（2×2 布局）保持不变；锚点比例代码未触碰。
2. **Inventory Slot**：`InventoryView` 的 `GridLayoutGroup.cellSize` 从 `(44, 44)` 改为 `(80, 80)`；`constraintCount` 从 `12` 改为 `8`（防止内容溢出，新宽度 (80+2)×8+12 = 668px）；`spacing (2, 2)` 与 `padding (6, 6, 6, 6)` 保持不变。
3. **InventoryItemView Prefab**：根节点 `sizeDelta` 从 `(44, 44)` 改为 `(80, 80)`，与 `GridLayoutGroup.cellSize` 保持同步；内部子元素（图标、名称标签、类型点）均使用相对锚点，无需额外调整。

---

---

## Bug Fix: StarChart 拖拽第一次无效（DragGhostView Awake 自我关闭三连锁）— 2026-03-02 16:45

### 修改文件
- `Assets/Scripts/UI/DragDrop/DragGhostView.cs`
- `Assets/Scripts/UI/Editor/UICanvasBuilder.cs`

### 根本原因
`DragGhostView.Awake()` 末尾调用了 `gameObject.SetActive(false)`，而 `UICanvasBuilder` 在构建时也调用了 `ghostGo.SetActive(false)`。

这触发了 CLAUDE.md 常见陷阱第11条的经典三连锁：
1. `UICanvasBuilder` 调用 `ghostGo.SetActive(false)` → Ghost 处于 inactive，Awake 被推迟
2. 第一次拖拽时 `DragGhostView.Show()` 调用 `gameObject.SetActive(true)` → **此时 Awake 才第一次执行**
3. `Awake()` 末尾调用 `gameObject.SetActive(false)` → **Ghost 立刻被关掉**
4. 结果：第一次拖拽 Ghost 不可见，拖拽无响应；第二次拖拽 Awake 已执行完毕，正常工作

### 修复方案
- **`DragGhostView.cs`**：`Awake()` 末尾改为 `_canvasGroup.alpha = 0f` 隐藏 Ghost，不再调用 `SetActive(false)`；`Show()` 改为 `_canvasGroup.alpha = _ghostAlpha` 显示；`Hide()` 改为动画结束后 `_canvasGroup.alpha = 0f` + 重置 scale，不再调用 `SetActive(false)`；可见性检查从 `activeSelf` 改为 `_canvasGroup.alpha <= 0f`
- **`UICanvasBuilder.cs`**：移除 `ghostGo.SetActive(false)` 调用，Ghost GameObject 始终保持 active，由 CanvasGroup.alpha 控制显隐

### 验收标准
- 第一次拖拽库存部件时 Ghost 立即出现并跟随鼠标 ✓
- 拖拽结束后 Ghost 正常消失 ✓
- 多次拖拽行为一致 ✓

---

---

## CLAUDE.md 更新：Unity MCP 工具使用指南 — 2026-03-02 17:22

### 修改文件
- `CLAUDE.md`

### 内容
在"Unity 编辑器操作边界"章节后新增独立章节"Unity MCP 工具使用指南"，包含：
1. **能力边界总览表**：覆盖全部 MCP 工具（Console / 场景 / GameObject / 资产 / 脚本 / 截图 / 编辑器状态 / 反射 / 包管理 / 测试）
2. **Project Ark 专项推荐场景 7 条**：Bug 排查、场景序列化验证、运行时数据污染检查、对象池回收验证、一次性批量检查（script-execute）、UI 视觉验证、单元测试
3. **使用原则 5 条**：Play Mode 前确认状态、script-execute 临时性、reflection 慎用等

### 目的
将 Unity MCP 的使用规范固化进项目开发规范，让 AI 在后续开发中能主动利用 MCP 闭环验证，减少"写代码 → 用户手动验证 → 截图反馈"的低效回路。

### 技术
无代码变更，纯文档更新。

---

---

## Bug Fix: DragGhostView 第一次拖拽 Ghost 不显示（localScale=0 持久化）— 2026-03-02 17:25

### 修改文件
- `Assets/Scripts/UI/DragDrop/DragGhostView.cs`

### 根本原因（通过 Unity MCP 运行时诊断确认）
MCP 诊断发现 Ghost 在 Play Mode 启动后 `localScale=(0,0,0)`，这是 `Hide()` 动画的遗留状态：

1. `Bind()` 调用 `Hide()` 时，Ghost 是 active 的（Awake 刚执行完，SetActive(false) 还没来得及执行）
2. `Hide()` 启动缩小到 `Vector3.zero` 的 tween，`OnComplete` 调用 `SetActive(false)`
3. `localScale=(0,0,0)` 被持久化到 Ghost 上
4. 第一次 `Show()` 时：`_scaleTween.Stop()` 停止 tween → PrimeTween 的 Stop() 可能触发 OnComplete → `SetActive(false)` 再次执行 → Ghost 被关掉

### 修复方案
两处修改：
1. **`Show()` 里**：把 `_scaleTween.Stop()` 移到 `SetActive(true)` 之前，并在 `SetActive(true)` 之前先重置 `localScale = Vector3.one`，确保 Ghost 从干净状态开始
2. **`Hide()` 的 `OnComplete` 里**：在 `SetActive(false)` 后加 `localScale = Vector3.one` 重置，防止 scale=0 状态持久化到下次 Show()

### 验收标准（MCP 运行时验证）
- Show() 后：activeSelf=True, localScale=(0.8,0.8,0.8), alpha=0.70 ✅
- 第一次拖拽 Ghost 正常出现 ✅

---

---

## StarChart UI 完善 — 对齐 HTML 原型（需求1/2/3/4/7/8/9）— 2026-03-03 14:40

### 新建文件
- `Assets/Scripts/Combat/StarChart/ItemShapeHelper.cs` — 2D 形状坐标工具类

### 修改文件
- `Assets/Scripts/Combat/StarChart/StarChartEnums.cs` — 新增 `ItemShape` 枚举（5种形状）和 `InventoryFilter` 枚举
- `Assets/Scripts/Combat/StarChart/StarChartItemSO.cs` — 新增 `[SerializeField] ItemShape _shape` 字段及 `Shape` 属性
- `Assets/Scripts/Combat/StarChart/SlotLayer.cs` — 完全重构为 2D 矩阵网格，新增 `CanPlace`/`TryPlace`/`Remove`/`GetAt`/`GetAnchor` API
- `Assets/Scripts/UI/TrackView.cs` — `RefreshColumn` 改为 2D 矩阵遍历，新增 `SetShapeHighlight` 和 `FindFirstAnchor` 方法
- `Assets/Scripts/UI/DragDrop/DragGhostView.cs` — 新增 `SetShape(ItemShape)` 重载，`SetDropState` 新增 `evictCount` 参数，边框颜色改用 PrimeTween 过渡，替换提示文字改为 `↺ 替换 N 个部件`
- `Assets/Scripts/UI/DragDrop/DragDropManager.cs` — `UpdateGhostDropState` 新增 `evictCount` 参数；拖拽 Slot 时显示卸装提示
- `Assets/Scripts/UI/SlotCellView.cs` — `UpdateGhostDropState` 调用传入 `evictCount`
- `Assets/Scripts/UI/InventoryItemView.cs` — 新增 `BuildShapePreview(ItemShape)` 生成半透明格子预览
- `Assets/Scripts/UI/LoadoutSwitcher.cs` — 完全重写：Drum Counter 翻牌动画；PlaySlideAnimation 并行 Scale 纵深；RENAME/DELETE/SAVE CONFIG 三个管理按钮
- `Assets/Scripts/UI/StatusBarView.cs` — 完整提示文字；新增 `ShowPersistent` 和 `RestoreDefault` 方法

### 内容简述
对齐 StarChartUIPrototype.html 原型，完成 7 项差距修复：异形部件 2D Shape 系统、Loadout 管理 UI、Drum Counter 翻牌动画、Loadout Card Scale 纵深感、拖拽预览高亮三态、库存过滤器顺序规范化、状态栏文字补全。

### 目的
让 StarChart UI 在功能完整性和视觉表现上全面对齐 HTML 原型，提升策略深度与操作手感。

### 技术方案
- `ItemShapeHelper.GetCells(ItemShape)` 返回预分配的 `Vector2Int[]` 偏移数组，零 GC
- `SlotLayer<T>` 内部 `T[GRID_ROWS, GRID_COLS]` 二维数组 + `Dictionary<T, Vector2Int>` 锚点缓存
- `DragGhostView.RebuildShapeGrid` 按 bounding box 动态生成格子 GameObject
- `LoadoutSwitcher.PlayDrumFlip` 使用 `Sequence.Group` 并行驱动前后两层 `LocalEulerAngles`
- 所有补间使用 `useUnscaledTime: true`，兼容暂停状态
- 遵循 CLAUDE.md：CanvasGroup 控制显隐，禁止 `SetActive(false)`；PrimeTween 替代手写 Lerp

---

---

## UICanvasBuilder 补全 LoadoutSwitcher 连线 — 2026-03-03 15:30

### 修改文件
- `Assets/Scripts/UI/Editor/UICanvasBuilder.cs`

### 内容简述
`BuildGatlingCol` 中的 DrumCounter 重构为双层翻牌结构，新增 RENAME/DELETE/SAVE 三个管理按钮、LoadoutNameLabel、RenameInputField，并补全所有新 SerializeField 的 WireField 连线。同时在 BuildStarChartSection 末尾补充 `LoadoutSwitcher._statusBar` 的跨引用连线。

### 目的
UICanvasBuilder 一键重建后，LoadoutSwitcher 的所有字段均自动连线，无需手动在 Inspector 中拖拽。

### 技术方案
- DrumCounter 拆分为 DrumContainer（RectTransform，用于 PrimeTween LocalEulerAngles 翻转）+ DrumFront + DrumBack 两个 TMP_Text 子层
- RenameInputField 初始 CanvasGroup.alpha=0，符合 CLAUDE.md 第11条（禁止 SetActive）
- DELETE 按钮背景色红色调，SAVE 按钮背景色绿色调，视觉区分危险/安全操作
- `_statusBar` 在 BuildStarChartSection 末尾统一连线，避免 BuildGatlingCol 内部依赖 statusBarView 变量作用域问题

---

---

## StarChart 部件异形格子配置 — 2026-03-03 15:35

### 修改文件
- `Assets/_Data/StarChart/Cores/ShebaCore_MachineGun.asset`
- `Assets/_Data/StarChart/Cores/ShebaCore_Shotgun.asset`
- `Assets/_Data/StarChart/Cores/EchoCore_BasicWave.asset`
- `Assets/_Data/StarChart/Cores/ShebaCore_PulseWave.asset`
- `Assets/_Data/StarChart/Prisms/FractalPrism_TwinSplit.asset`
- `Assets/_Data/StarChart/Prisms/ShebaP_Homing.asset`
- `Assets/_Data/StarChart/Prisms/ShebaP_MinePlacer.asset`

### 内容简述
将 7 个原本全为 Shape1x1 的部件改为异形，以便测试 SlotLayer 2D 网格放置逻辑。

### 目的
所有部件均为 1×1 时，SlotLayer 的多格占用、碰撞检测、L形/2×2 放置路径永远不会被触发，无法验收 StarChart UI 完善模块的核心功能。

### 技术方案
基于 DPS 数值演算（BaseDamage × FireRate）和语义合理性，按"格子越多=越强/越特殊"原则分配形状：

| 部件 | 形状 | 格数 | 理由 |
|------|------|------|------|
| ShebaCore_MachineGun | Shape1x2H | 2 | 高频连射(DPS=60)，枪管横向延伸 |
| ShebaCore_Shotgun | ShapeL | 3 | 散弹近战爆发(spread=30°)，结构复杂 |
| EchoCore_BasicWave | Shape2x1V | 2 | AOE穿墙波(DPS=30)，纵向扩散 |
| ShebaCore_PulseWave | Shape2x2 | 4 | 超强击退AOE(knockback=2.5)，最强控场 |
| FractalPrism_TwinSplit | Shape1x2H | 2 | +2弹数分裂，横向光路分叉 |
| ShebaP_Homing | Shape2x1V | 2 | 追踪导引需要额外传感器空间 |
| ShebaP_MinePlacer | ShapeL | 3 | 地雷部署机构，结构最复杂 |

`_shape` 字段直接写入 .asset 序列化文件（整数枚举值：Shape1x2H=1, Shape2x1V=2, ShapeL=3, Shape2x2=4），同步更新 `_slotSize` 保持 legacy 兼容。

---

---

## 移除 ItemTooltipView 中的诊断 Debug.Log — 2026-03-03 17:07

### 修改文件
- `Assets/Scripts/UI/ItemTooltipView.cs`

### 内容简述
删除了 `PositionNearMouse()` 方法中每帧通过 `Update()` 触发的 4 条诊断日志（`Debug.Log`/`Debug.LogWarning`），这些日志严重污染了 Console 并降低了编辑器性能。

### 目的
消除每帧日志刷屏问题，使 Console 恢复可用状态，并降低编辑器性能开销。

### 技术方案
直接删除 `PositionNearMouse()` 中的 4 条日志语句（2× `Debug.Log`、2× `Debug.LogWarning`），必要处替换为静默注释。不影响 tooltip 定位逻辑的任何行为。

---

---

## 修复 DragGhostView 中 ReplaceHint 的 MissingComponentException — 2026-03-03 17:12

### 修改文件
- `Assets/Scripts/UI/DragDrop/DragGhostView.cs`

### 场景变更
- `ReplaceHint` GameObject：添加 `CanvasGroup` 组件，将 `activeSelf` 设为 `true`（原为 false）

### 内容简述
修复了 `DragGhostView.Awake()` 第 91 行在尝试对 `ReplaceHint` 子 GameObject 设置 `CanvasGroup.alpha` 时抛出的 `MissingComponentException`。

### 目的
消除阻止 DragGhost 正常初始化的运行时异常。

### 技术方案
1. **根因**：`ReplaceHint` GameObject 没有 `CanvasGroup` 组件且处于 `SetActive(false)` 状态。代码中的 `AddComponent<CanvasGroup>()` 回退逻辑在非激活对象上执行失败，产生 `MissingComponentException`。
2. **场景修复**：直接在场景中为 `ReplaceHint` 添加 `CanvasGroup` 组件，并将 `activeSelf` 设为 `true`，`CanvasGroup.alpha = 0`（遵循 CLAUDE.md 第 11 条：禁止用 SetActive 控制 UI 显隐）。
3. **代码修复**：在 `Awake()` 中为 `_replaceHintCg` 的使用添加 null 守卫，并配合 `Debug.LogError` 回退，防止未来出现静默失败。

---

---

## 异形背包格子 — 背包按 HTML 原型显示部件形状 — 2026-03-03 17:16

### 修改文件
- `Assets/Scripts/UI/InventoryView.cs`
- `Assets/Scripts/UI/InventoryItemView.cs`

### 内容简述
背包现在以实际多格尺寸显示异形部件（1×2H、2×1V、L 形、2×2），与 HTML 原型的 CSS Grid `grid-column: span N / grid-row: span N` 行为一致。此前所有部件无论形状如何都显示为统一的 1×1 方块。

### 目的
用户反馈背包中所有部件都显示为 1×1，而拖拽 Ghost 已能正确显示形状。背包格子需要像 HTML 原型一样在视觉上区分异形部件。

### 技术方案

**InventoryView.cs — 自定义行优先排列布局：**
1. 用自定义排列算法替代 Unity 的 `GridLayoutGroup`（后者强制统一 cellSize）。
2. 新增序列化字段：`_gridColumns`（8）、`_cellSize`（80）、`_cellGap`（2）、`_gridPadding`（6）——与原有 GridLayoutGroup 设置保持一致。
3. 在 `Refresh()` 中：运行时禁用 `GridLayoutGroup` 和 `ContentSizeFitter`，然后用二维布尔占位网格手动定位每个部件。
4. `TryFindPosition()`：逐行逐列扫描，找到第一个能容纳部件 bounding box 的可用位置（行优先排列，与 CSS Grid auto-flow 一致）。
5. 每个 `InventoryItemView` 的 `RectTransform.sizeDelta` 设为 `(spanCols × cellSize + gaps, spanRows × cellSize + gaps)`。
6. 内容高度根据最高占用行计算，并应用到 content RectTransform，确保 ScrollRect 正常工作。

**InventoryItemView.cs — 增强形状预览：**
1. `BuildShapePreview()` 现在为多格形状渲染完整的 bounding box 网格（而非仅渲染 active cells）。
2. Active cells 使用类型颜色着色（通过 `StarChartTheme.GetTypeColor()`），透明度 25%——与 HTML 的 `.inv-shape-active-{type}` 类一致。
3. Bounding box 中的空格（如 L 形的右下角）为透明，清晰展示非矩形形状。
4. 1×1 部件完全跳过形状预览（与全格填充无视觉差异）。
5. 新增 `System.Collections.Generic` 导入，用于 active cell 快查的 `HashSet<Vector2Int>`。

---

---

## AI Skills 使用指南 — 添加到 CLAUDE.md (2026-03-03 22:25)

**修改文件：**
- `CLAUDE.md`

**内容：**
在 CLAUDE.md 的 "Unity MCP 工具使用指南" 和 "实用开发 Tips" 之间新增 "AI Skills 使用指南" 章节，包含：
- 可用 Skills 完整列表及各自的触发场景说明（15 个 Skill）
- 5 条使用原则
- Project Ark 常见工作流与 Skills 的映射关系（新功能开发/Bug修复/架构重构/新星图部件/性能优化）

**目的：**
将 AI Skills 的使用规范正式写入项目规范文档，确保 AI 在合适场景下积极主动调用 Skills 提升任务执行质量。

**技术：**
文档化标准工作流映射。

---

---

## Fix: Track 拖拽预览形状不一致 (2026-03-03 22:27)

**修改文件：**
- `Assets/Scripts/UI/SlotCellView.cs`

**根因：**
`SlotCellView.OnPointerEnter` 调用的是旧的 `SetMultiCellHighlight(CellIndex, SlotSize, ...)` 方法，该方法从 CellIndex 开始**线性连续**高亮 SlotSize 个格子，完全不考虑 2D 形状。例如 Shape1x2H（横向2格）会错误地高亮同一列的两行，而不是同一行的两列。

**修复：**
将 `SetMultiCellHighlight` 替换为已有的 `SetShapeHighlight`：
1. 将线性 `CellIndex` 转换为 2D 坐标：`anchorCol = CellIndex  0RID_COLS`，`anchorRow = CellIndex / GRID_COLS`
2. 调用 `OwnerTrack.SetShapeHighlight(anchorCol, anchorRow, payload.Item.Shape, previewState, isCoreLayer)` 按实际 2D 形状高亮对应格子

**技术：**
`SlotLayer.GRID_COLS=3`，TypeColumn cells 按 row-major 排列（cellIndex = row * 3 + col），与 RefreshColumn 的计算方式一致。

---

---

## Feature: StarChart Track 显示修复 — 完整形状覆盖层 + 智能锚点推算 (2026-03-04 16:39)

**新建文件：**
- `Assets/Scripts/UI/ItemOverlayView.cs`

**修改文件：**
- `Assets/Scripts/Combat/StarChart/StarChartEnums.cs`
- `Assets/Scripts/Combat/StarChart/ItemShapeHelper.cs`
- `Assets/Scripts/Combat/StarChart/WeaponTrack.cs`
- `Assets/Scripts/UI/TypeColumn.cs`
- `Assets/Scripts/UI/TrackView.cs`
- `Assets/Scripts/UI/SlotCellView.cs`
- `Assets/Scripts/UI/DragDrop/DragDropManager.cs`

**需求 1：Track 上已装备部件以完整形状覆盖层展示**

问题：已装备的异形部件在 Track 上每个格子独立着色，无法直观看出部件整体轮廓，且颜色与背包不一致。

方案：
1. 新建 `ItemOverlayView` 组件，由 `TrackView.RefreshColumn` 在每次刷新时动态创建，覆盖在 TypeColumn 的 GridContainer 上方。
2. `ItemOverlayView.Setup` 根据形状偏移列表动态计算 bounding box，在 bounding box 内为每个格子创建 Image 子对象：occupied 格子显示类型颜色，empty 格子透明。
3. 在锚点格居中显示部件图标（`item.Icon`）。
4. `TypeColumn` 新增 `GridContainer` 属性（返回 cells[0] 的父 RectTransform）。
5. `TrackView` 新增 `_activeOverlays` 列表，每次 `RefreshColumn` 时先销毁旧 overlays，再创建新的。
6. `TrackView.Awake` 中从第一个可用格子的 RectTransform 自动检测 `_cellSize` 和 `_cellGap`。
7. `ItemOverlayView` 实现完整事件接口，hover/click/drag 事件转发给底层 SlotCellView。

**需求 2：拖拽放置时自动推算合法锚点**

问题：系统以鼠标悬停格作为锚点，导致悬停在非左上角格时形状越界或位置错误。

方案：
1. `ItemShapeHelper.FindBestAnchor` — 反向枚举候选锚点算法：对形状每个 offset (dx,dy) 计算候选锚点 = (hoverCol-dx, hoverRow-dy)，验证边界，选 row 最小再 col 最小的（top-left 优先），候选集为空返回 false（Invalid）。
2. `SlotCellView.OnPointerEnter` 调用 `FindBestAnchor`，结果存入 `DragDropManager.DropTargetAnchorCol/Row`。
3. `DragDropManager.EquipToTrack` 改用 `DropTargetAnchorCol/Row` 调用带锚点的 `WeaponTrack.EquipCore/EquipPrism` 重载。
4. `WeaponTrack` 新增带锚点参数的 `EquipCore/EquipPrism` 重载，调用 `SlotLayer.TryPlace`。

**扩展：ShapeLMirror**
- `StarChartEnums.ItemShape` 新增 `ShapeLMirror`（左上+左下+右下，缺右上）。
- `ItemShapeHelper` 新增 `CellsLMirror = { (0,0), (0,1), (1,1) }`，`GetCells/GetBounds` switch 新增对应 case。

**技术要点：**
- 所有算法基于形状偏移列表动态计算，无硬编码。Grid 尺寸变化只需更新 `SlotLayer.GRID_COLS/GRID_ROWS` 常量。
- 新增形状只需在 `ItemShapeHelper.GetCells` 添加 case，锚点推算和覆盖层渲染无需修改。
- `ItemOverlayView` 通过代码创建（`new GameObject + AddComponent`），Setup 中动态创建 border/icon 子对象，无需 prefab。

---

---

## DragDrop 架构改进（广播注册 + 双重验证 + Tooltip PointerMove + Inventory 对象池）— 2026-03-04 23:45

**修改文件：**
- `Assets/Scripts/UI/DragDrop/DragDropManager.cs`
- `Assets/Scripts/UI/TypeColumn.cs`
- `Assets/Scripts/UI/SlotCellView.cs`
- `Assets/Scripts/UI/ItemTooltipView.cs`
- `Assets/Scripts/UI/InventoryItemView.cs`
- `Assets/Scripts/UI/InventoryView.cs`

**内容：**
按 `WkecWulin_DragDrop_Analysis` 的可借鉴设计，对 Project Ark 拖拽系统做了 4 项架构级修复与优化：

1. **BeginDrag 广播注册机制（去硬编码）**
   - `DragDropManager` 新增 `_registeredColumns` 与 `RegisterColumn/UnregisterColumn`。
   - `BeginDrag/CleanUp` 改为广播 `BroadcastDragBegin/BroadcastDragEnd`，由每个 `TypeColumn` 自行判断是否高亮。
   - `TypeColumn` 新增 `OnEnable/Start/OnDisable` 注册生命周期，以及 `OnDragBeginBroadcast/OnDragEndBroadcast`。
   - 删除 `DragDropManager.HighlightMatchingColumns()`，不再每次拖拽都 `GetComponentsInChildren` + switch 硬编码。

2. **OnDrop 双重验证（消除过期缓存）**
   - `SlotCellView` 提取 `ComputeDropValidity()`，统一封装类型匹配、空间检查、替换判定、形状锚点推算。
   - `OnDrop` 改为再次实时校验，而不是依赖 `OnPointerEnter` 阶段的缓存状态。
   - 移除 `_isHighlightedValid` 相关逻辑，降低拖拽过程中状态变化导致的误判风险。

3. **Tooltip 改为 PointerMove 驱动（去每帧 Update）**
   - `ItemTooltipView` 删除 `Update()`，新增 `UpdatePosition()`。
   - `InventoryItemView`、`SlotCellView` 实现 `IPointerMoveHandler`，仅在指针移动时刷新 tooltip 位置。
   - `DragDropManager.Bind` 缓存 `TooltipView`，统一提供给拖拽相关视图访问。

4. **InventoryView 对象池化（替代 DestroyImmediate 全量重建）**
   - 新增 `_pooledViews`，`Refresh` 改为 `GetOrCreateView()` 复用卡片。
   - `ClearViews` 改为解绑事件并回收到池中（`SetActive(false)`），不再 `DestroyImmediate` 全销毁。
   - `OnDestroy` 时统一销毁池内对象，减少频繁刷新导致的 GC 和 EventSystem 抖动。

**目的：**
- 提升拖拽系统的可扩展性（新列类型不再改 manager 核心逻辑）。
- 提升拖放判定稳定性（避免 hover 缓存过期造成的误触发）。
- 降低 UI 每帧开销与刷新抖动（tooltip 与 inventory 视图均改为事件驱动/对象复用）。

**技术：**
- 事件广播 + 自判断（注册表模式）。
- Drop 双重验证（Begin/Drop 两阶段一致性保障）。
- PointerMove 驱动 UI 更新（替代 Update 常驻轮询）。
- 轻量对象池复用（减少销毁/重建成本）。

---

---

## Bug Fix: DragGhostView 双重渲染修复（Bug 2）— 2026-03-05 12:31

**修改文件：**
- `Assets/Scripts/UI/DragDrop/DragGhostView.cs`

**内容简述：**
修复拖拽 Ghost 同时显示"正确 L 形 cell grid"和"矩形 icon"的双重渲染问题。

**根因：**
`Show()` 方法中 `_iconImage` 被赋予 sprite + alpha，该 Image 的 RectTransform 拉伸到整个 bounding box（L 形 = 2×2），与 `RebuildShapeGrid()` 创建的 cell grid 叠加，形成双重渲染。

**修复方案：**
在 `Show()` 中将 `_iconImage.sprite = null` 且 `_iconImage.color = Color.clear`，完全隐藏 icon image。Ghost 的视觉形状完全由 cell grid（`_cellImages`）表达，与背包中部件的显示形态保持一致。`_nameLabel` 已显示部件名称，无需 icon 重复表达。

**技术：**
- 最小化改动：仅修改 `Show()` 中 `_iconImage` 的赋值逻辑，不影响其他路径。
- 设计原则：拖拽时看到的形状 = 背包中看到的形状（cell grid 统一表达）。

---

---

## StarChart UI Bug 修复 + Track 动态容量系统 — 2026-03-05 12:52

**修改文件：**
- `Assets/Scripts/Combat/StarChart/SlotLayer.cs`（重构）
- `Assets/Scripts/Combat/StarChart/WeaponTrack.cs`（新增 `SetLayerCols`）
- `Assets/Scripts/Combat/StarChart/StarChartController.cs`（修改 `ExportTrack`/`ImportTrack`）
- `Assets/Scripts/Core/Save/SaveData.cs`（`TrackSaveData` 新增字段）
- `Assets/Scripts/UI/TrackView.cs`（常量引用替换）
- `Assets/Scripts/UI/SlotCellView.cs`（常量引用替换）
- `Assets/Scripts/Combat/Tests/SlotLayerTests.cs`（测试重写）

**内容简述：**
1. **SlotLayer 动态容量**：删除 `GRID_COLS/GRID_ROWS/GRID_SIZE` 硬编码常量，改为 `Rows`（固定2）、`Cols`（动态1-4）、`Capacity = Rows×Cols` 属性。新增 `TryUnlockColumn()` 方法（返回 bool，达到 MAX_COLS=4 时返回 false）。修复 `FreeSpace = Capacity - UsedSpace`（原来错误地用 `GRID_COLS - UsedSpace`）。构造函数接收 `initialCols` 参数，默认 1（初始 2 格）。
2. **存档持久化**：`TrackSaveData` 新增 `CoreLayerCols`/`PrismLayerCols` 字段（默认 1，旧存档 clamp 到 ≥1）。`ExportTrack` 写入列数，`ImportTrack` 读取并调用 `WeaponTrack.SetLayerCols()` 恢复。
3. **UI 层常量替换**：`TrackView.cs` 和 `SlotCellView.cs` 中所有 `SlotLayer<T>.GRID_COLS/GRID_ROWS` 静态引用替换为从 layer 实例动态读取。
4. **Bug 1/3 确认**：`InventoryItemView` 和 `ItemOverlayView` 已正确实现 empty cell `Color.clear + raycastTarget=false`，无需额外修改。
5. **单元测试**：重写 `SlotLayerTests.cs`，修复旧测试（初始容量 2 而非 6），新增容量解锁测试（初始容量、解锁后容量、MAX_COLS 边界、旧数据不受影响）。

**目的：** 修复 StarChart 星图 UI 的 4 个 Bug（L 形部件显示、Track Overlay 显示、Track 容量动态解锁、FreeSpace 计算错误）。

**技术方案：**
- `SlotLayer._grid` 预分配为 `[FIXED_ROWS, MAX_COLS]`，避免解锁时重新分配内存；`InBounds` 检查使用动态 `Cols` 限制访问范围。
- `WeaponTrack.SetLayerCols` 通过循环调用 `TryUnlockColumn` 恢复列数，不依赖 `UnityEngine.Mathf`（纯 C# 类）。
- 旧存档兼容：`JsonUtility` 反序列化缺失字段时默认为 0，`ImportTrack` 中 `Mathf.Max(1, data.CoreLayerCols)` 确保不低于 1。

---

---

## TrackView Debug 格子数参数暴露 — 2026-03-05 15:53

**修改文件：**
- `Assets/Scripts/UI/TrackView.cs`

**内容简述：**
在 `TrackView` 上新增 4 个 `[SerializeField]` 调试参数，允许在 Inspector 中直接覆盖各分类的格子数，无需修改存档或游戏进度：

| 参数 | 类型 | 说明 |
|------|------|------|
| `_debugCoreCols` | `int [0-4]` | Core 层列数（0 = 使用存档值，1-4 = 强制覆盖） |
| `_debugPrismCols` | `int [0-4]` | Prism 层列数（0 = 使用存档值，1-4 = 强制覆盖） |
| `_debugSailSlots` | `int [0-4]` | Sail 显示格子数（0 = 默认 1 格，1-4 = 覆盖） |
| `_debugSatSlots` | `int [0-4]` | Satellite 显示格子数（0 = 默认 4 格，1-4 = 覆盖） |

**行为规则：**
- Core/Prism：值 > 0 时调用 `_track.SetLayerCols()` 强制设置层列数，影响实际数据层（格子数 = cols × 2 行）
- Sail/Sat：值 > 0 时控制 `RefreshSailColumn`/`RefreshSatColumn` 中显示的格子数量（纯 UI 层，不影响数据）
- 所有参数默认为 0（不覆盖），不影响正式游戏流程
- 在 `Awake` 和 `Bind` 时均会应用，确保无论初始化顺序如何都生效

**目的：** 解决问题 B（Track 格子数不一致），提供快速调试手段，无需依赖存档数据即可验证各分类格子数的视觉表现。

**技术方案：**
- 新增私有方法 `ApplyDebugSlotCounts()`，在 `Awake` 末尾和 `Bind` 中（track 绑定后）各调用一次
- Sail/Sat 的 debug 参数不调用 `SetLayerCols`（Sail/Sat 无 SlotLayer），而是在 Refresh 方法内通过 `sailSlots`/`satSlots` 局部变量控制循环范围

---

---

## Icon/Shape 解耦渲染（方案B）+ Bug D/E 修复 — 2026-03-05 16:00

**新建文件：**
- `Assets/Scripts/UI/ItemIconRenderer.cs`

**修改文件：**
- `Assets/Scripts/UI/InventoryItemView.cs`
- `Assets/Scripts/UI/ItemOverlayView.cs`
- `Assets/Scripts/UI/DragDrop/DragGhostView.cs`
- `Assets/Scripts/UI/TrackView.cs`
- `Assets/Scripts/UI/DragDrop/DragDropManager.cs`

**内容简述：**

### 核心思路（Backpack Monsters 渲染哲学）

Icon 和 Shape 是两个完全独立的层：

| 层 | 职责 | 实现 |
|----|------|------|
| **Shape Layer** | 表达"占了哪些格子"（颜色、高亮、选中） | `ItemIconRenderer.BuildShapeCells()` — 每个 active cell 一个 Image，empty cell 透明 |
| **Icon Layer** | 表达"这是什么东西"（身份识别） | `ItemIconRenderer.BuildIconCentered()` — 固定大小 Image，锚定在 shape 视觉重心，不拉伸 |

### ItemIconRenderer.cs（新建）

静态工具类，提供：
- `BuildShapeCells(parent, shape, activeColor)` — anchor 比例定位，用于 InventoryItemView
- `BuildShapeCellsAbsolute(parent, shape, activeColor, cellSize, cellGap)` — 绝对像素定位，用于 ItemOverlayView / DragGhostView
- `RefreshShapeCellColors(cellImages, shape, activeColor)` — 重新着色（equip 状态变化时）
- `BuildIconCentered(parent, item, iconSizePx)` — 固定大小 Icon，锚定在 shape 重心（normalized 坐标）
- `BuildIconOnAnchorCell(parent, item, cellSize, iconSizePx)` — 固定大小 Icon，锚定在 anchor cell 中心（绝对坐标）
- `GetShapeCentroidNormalized(shape)` — 计算 active cells 的视觉重心（normalized [0,1] 空间）

### InventoryItemView.cs（改造）

- 移除 `[SerializeField] private Image _iconImage`（不再需要 Inspector 连线的 icon）
- 新增 `private Image _iconImageDynamic`（由 ItemIconRenderer 动态创建）
- `BuildShapePreview()` 改为调用 `ItemIconRenderer.BuildShapeCells()` + `BuildIconCentered()`
- `RefreshShapeColors()` 改为调用 `ItemIconRenderer.RefreshShapeCellColors()`
- `OnDestroy()` 同时清理 `_iconImageDynamic`

### ItemOverlayView.cs（改造）

- 移除手写的 cell 循环（`occupiedSet`、`bounds`、`cells` 局部变量）
- 移除手写的 `iconGo` 创建和 icon 定位代码
- 改为调用 `ItemIconRenderer.BuildShapeCellsAbsolute()` + `BuildIconOnAnchorCell()`
- 移除 `using System.Collections.Generic` 和 `using TMPro`

### DragGhostView.cs（改造）

- `RebuildShapeGrid()` 改为调用 `ItemIconRenderer.BuildShapeCellsAbsolute()`
- `Show()` 中新增 Icon Layer：调用 `ItemIconRenderer.BuildIconCentered()`，固定大小居中
- 新增 `private Image _iconImageDynamic` 字段，`OnDestroy()` 中清理

### TrackView.cs（问题E修复）

- `RefreshColumn()` 中将 `cells[i].gameObject.SetActive(i < unlockedCount)` 改为 CanvasGroup 控制：
  - `cg.alpha = unlocked ? 1f : 0f`
  - `cg.interactable = unlocked`
  - `cg.blocksRaycasts = unlocked`
- 遵守 CLAUDE.md 第11条：uGUI 面板禁止用 SetActive 控制显隐

### DragDropManager.cs（问题D修复）

- `BeginDrag()` 中，当 `payload.Source == DragSource.Slot` 时，立即调用 `_panel.RefreshAllViews()`
- 目的：移除被拖拽部件的 `ItemOverlayView`，避免 Ghost + overlay 同时可见（视觉重叠）

**目的：** 彻底解决 Icon 和 Shape 耦合导致的所有视觉 bug（非矩形 shape 的 Icon 溢出、Ghost 双重渲染、overlay icon 残留），同时修复 SetActive 违规和拖拽 overlay 残留问题。

**技术方案：**
- 单一职责：`ItemIconRenderer` 是唯一知道如何渲染 Icon+Shape 的地方，三个 View 都委托给它
- Icon 固定大小（32px inventory / 36px overlay / ghostSize×0.55 ghost），不随 bounding box 变化
- Shape centroid 计算：`(Σ(col+0.5)/boundsX) / cellCount`，y 轴翻转适配 Unity UI 坐标系

---

---

## GG 飞船手感复刻 — 移动模型完整重构 (2026-03-05 16:13)

### 背景

通过 Il2CppDumper 反编译 Galactic Glitch (IL2CPP) 的 global-metadata.dat，提取出 GGSteering 类的完整字段和方法签名，确认了 GG 飞船的物理模型。

**GG 移动模型**（角加速度前向推力）与 Project Ark 旧模型（Twin-Stick 速度映射）根本不同，本次做全量对齐重构。

---

### 新建/修改文件

#### Assets/Scripts/Ship/Data/ShipStatsSO.cs（完全重写）
- 删除全部旧字段（AccelerationCurve、Deceleration、SharpTurn、InitialBoost 等）
- 新增 GG 对应参数：
  - **Rotation**：_angularAcceleration（900 deg/s²）、_maxRotationSpeed（380 deg/s）、_angularDrag（8）
  - **ForwardThrust**：_forwardAcceleration（20 units/s²）、_maxSpeed（10 units/s）、_linearDrag（3）
  - **Boost**：_boostImpulse（18）、_boostDuration（0.25s）、_boostMaxSpeedMultiplier（2×）、_boostCooldown（1.2s）、_boostBufferWindow（0.15s）
  - **Dash**：_dashImpulse（22）、_dashIFrameDuration（0.12s）、_dashCooldown（0.4s）
- 保留向后兼容别名（MoveSpeed、DashSpeed、DashDuration 等）防止其他脚本编译报错

#### Assets/Scripts/Ship/Movement/ShipMotor.cs（完全重写）
- 旧模型：Twin-Stick（输入直接映射到世界方向速度）
- 新模型（GGSteering 对齐）：
  - 只检测前进输入（input.y > 0.1f），忽略横向
  - FixedUpdate 中 rb.AddForce(transform.up * forwardAcceleration, Force) 沿船头施力
  - 速度上限由代码 clamp（boost 时放宽为 maxSpeed × BoostSpeedMultiplier）
  - 无输入时 Rigidbody2D.linearDrag 自然衰减（无刹车代码）
- 新增 AddExternalImpulse() 公共接口供 Boost/Dash/击退使用
- 保留 ApplyImpulse()、SetVelocityOverride()、ClearVelocityOverride() 向后兼容

#### Assets/Scripts/Ship/Aiming/ShipAiming.cs（完全重写）
- 旧模型：Mathf.MoveTowardsAngle 固定角速度（无惯性）
- 新模型（GGSteering.RotateTowardsAimTarget 对齐）：
  - FixedUpdate 计算目标角度和角度差 Mathf.DeltaAngle
  - desiredAngularVelocity = clamp(angleDiff × (angularAcceleration / maxRotationSpeed))
  - 以 angularAcceleration × dt 步长 MoveTowards 到目标角速度（逐帧加速，不瞬间到位）
  - 写入 Rigidbody2D.angularVelocity，松手后由 angularDrag 自然衰减

#### Assets/Scripts/Ship/Movement/ShipDash.cs（重写）
- 旧模型：速度覆盖（强制维持固定速度到 duration 结束）
- 新模型（GG AddForce(Impulse) 对齐）：
  - 触发时 _motor.AddExternalImpulse(dashDir × dashImpulse) 一次性冲量
  - 无敌帧按 DashIFrameDuration 独立计时
  - 简化输入缓冲（单次布尔缓冲，替代旧 InputBuffer）

#### Assets/Scripts/Ship/Movement/ShipBoost.cs（新建）
- GG BoosterBurnoutPower 的完整复刻：
  - 方向 = 船头方向（ShipAiming.FacingDirection）
  - _motor.AddExternalImpulse(boostDir × boostImpulse) 一次性冲量
  - Boost 持续期间设置 Motor.IsBoosting = true 和 BoostSpeedMultiplier 放宽速度上限
  - Boost 结束后多余速度由 linearDrag 自然衰减（无强制减速代码）
  - CooldownProgress (0~1) 属性供 UI 冷却条使用
  - 事件：OnBoostStarted / OnBoostEnded / OnBoostReady 供 VFX 订阅

#### Assets/Scripts/Ship/Input/InputHandler.cs（增量修改）
- 新增 public event Action OnBoostPressed
- 新增 _boostAction 字段，映射 Ship ActionMap 的 "Boost" Action
- OnEnable/OnDisable 中注册/取消订阅

#### Assets/Scripts/Ship/Editor/ShipFeelAssetCreator.cs（更新）
- 删除旧字段赋值（_acceleration、_deceleration、_accelerationCurve 等）
- 新增 GG 参数默认值赋值
- 移除不再需要的 SetAnimationCurve 和 EnsureCurve 辅助方法

---

### 目的

一比一复刻 Galactic Glitch 飞船手感：
1. **旋转有重量感**：角加速度模型，快速但有惯性
2. **前向推力**：只能向船头方向加速，转向需要先旋转
3. **自然减速**：linearDrag 物理衰减，松开后滑行感
4. **Boost**：沿船头方向冲量 + 持续速度放宽，对应 GG 的 BoosterBurnoutPower

### 技术

- Rigidbody2D.angularVelocity（旋转惯性）
- Rigidbody2D.AddForce (Force 模式 + Impulse 模式)
- Rigidbody2D.linearDrag / angularDrag（自然衰减）
- UniTask.Delay（Boost/Dash 持续时间异步等待）

### 下一步

- 在 Input Action Asset 中添加 "Boost" Action（Space / SouthButton）
- ShipBoost 挂载到飞船 Prefab，引用 ShipStatsSO
- ShipEngineVFX 订阅 ShipBoost.OnBoostStarted 增强引擎粒子
- 运行游戏调整 ShipStatsSO 参数以匹配 GG 手感
---

---

## GG 解包 — AssetRipper 提取真实数值 (2026-03-05 16:32)

### 方法论总结（4种方案实测结果）

| 方案 | 工具 | 结果 |
|------|------|------|
| UnityPy 直接读 | UnityPy 1.25.0 | ✗ IL2CPP MonoBehaviour 无 TypeTree，raw_data 为空 |
| UnityPy + DummyDll TypeTree | TypeTreeGeneratorAPI | ✗ 工具尚未成熟，依赖链太复杂 |
| 特征扫描 raw bytes | 自写 Python 模式匹配 | ✗ raw_data 为空，无法扫描 |
| AssetRipper GUI + REST API | AssetRipper 1.3.11 headless | ✓ 成功导出完整 YAML Prefab |

**最终有效方案**：AssetRipper headless 模式 → REST API 加载 GG 目录 → /Export/UnityProject 导出 → 在 C:\Temp\GG_Ripped\ExportedProject\Assets\GameObject\Player.prefab 中找到全部序列化数值。

### 关键发现：GG 真实架构

**GG 飞船不是静态参数，而是状态机驱动**：Player.prefab 里有一个状态机组件，根据当前动作状态（IsBlueState/IsBoostState/IsMainAttackState/IsDodgeState等）动态切换物理参数。

#### Rigidbody2D（真实值）
- m_Mass: 100
- m_LinearDrag: 2.5（基准值）
- m_AngularDrag: 0
- m_GravityScale: 0

#### 状态机参数对照表（从 Player.prefab 直接读取）

| 状态 | linearDrag | moveAccel | maxMoveSpeed | angularAccel | maxRotSpeed |
|------|-----------|-----------|--------------|-------------|------------|
| IsBlueState（0，正常） | 3 | 100 | 7.5 | 80 | 80 |
| IsBoostState（3） | 2.5 | 100 | **9** | 40 | 80 |
| IsRedState（1） | 3 | 0 | 7.5 | 180 | 360 |
| IsMainAttackState（6） | 3 | 100 | 7.5 | **720** | **720** |
| IsMainAttackFireState（10） | 3 | 100 | 5.25 | 720 | 720 |
| IsSecondaryAttackState（7） | 2.5 | 100 | 5.25 | 180 | 360 |
| IsDodgeState（2） | **1.7** | 12 | 4 | 20 | 50 |
| IsHealState（4） | 1.5 | 0 | 5 | 0 | 240 |
| IsShapeShiftState（5） | 3 | 100 | 6.5 | 120 | 360 |

#### 其他关键数值（Player MonoBehaviour）
- afterBoostDrag: 1.8（Boost 结束后的 drag）
- dodgeForce: 13（Dash 冲量）
- dodgeForceAfterDodge: 5（Dash 结束后残余力）
- dodgeInvulnerabilityTime: 0.15（Dash 无敌帧）
- dodgeCacheTime: 0.2（输入缓冲）
- dodgeRechargeTime: 0.5（Dash 冷却）
- speedModAfterDodge: 0.7（Dash 后速度系数）

#### BoosterBurnoutPower（并非移动加速，是战斗 Power）
- 这是一个战斗 Power（升级装备），触发时在飞船后方留下伤害轨迹
- **不是**控制飞船加速的 Boost 系统

### ShipStatsSO 更新的默认值（对齐 GG 真实值）

- _angularAcceleration: 900 → **80** (GG IsBlueState)
- _maxRotationSpeed: 380 → **80** (GG IsBlueState)
- _angularDrag: 8 → **0** (GG 无旋转阻尼)
- _forwardAcceleration: 20 → **100** (GG IsBlueState)
- _maxSpeed: 10 → **7.5** (GG IsBlueState)
- _linearDrag: 3 → **3** (✓ 已正确)
- _boostImpulse: 18 → **6** (GG IsBoostState vs IsBlueState 差值)
- _boostDuration: 0.25 → **0.2** (GG minTime=0.2s)
- _boostMaxSpeedMultiplier: 2.0 → **1.2** (9/7.5=1.2)
- _boostCooldown: 1.2 → **1.0**
- _dashImpulse: 22 → **13** (GG dodgeForce)
- _dashIFrameDuration: 0.12 → **0.15** (GG dodgeInvulnerabilityTime)
- _dashCooldown: 0.4 → **0.5** (GG dodgeRechargeTime)

---

---

## Bug Fix: 拖拽 Ghost 位置偏移 — 2026-03-06 13:02

### 修改文件
- `Assets/Scripts/UI/DragDrop/DragGhostView.cs`
- `Assets/Scripts/UI/DragDrop/DragDropManager.cs`
- `Assets/Scripts/UI/InventoryItemView.cs`
- `Assets/Scripts/UI/SlotCellView.cs`
- `Assets/Scripts/UI/ItemOverlayView.cs`

### 内容简述
修复拖拽 Ghost 不跟随卡片上实际点击位置的问题。

### 根因
`ComputeDragOffset` 计算 `pressLocal - pointerLocal`，但由于 `BeginDrag` 是从 `OnBeginDrag` 调用的，此时 `eventData.position == eventData.pressPosition`，导致偏移量始终为 `Vector2.zero`。Ghost 中心始终吸附到鼠标位置，而非用户实际点击的位置。

### 修复方案
- `DragGhostView.ComputeDragOffset(eventData, sourceRect)`：新增 `sourceRect` 参数。现在通过 `GetWorldCorners` 计算点击位置相对于卡片左上角的偏移，再减去 Ghost 半尺寸，得到相对于 Ghost 中心的正确偏移量。
- `DragDropManager.BeginDrag(...)`：新增可选的 `sourceRect` 参数，透传给 `ComputeDragOffset`。
- `InventoryItemView`、`SlotCellView`、`ItemOverlayView`：在 `BeginDrag` 调用中传入 `GetComponent<RectTransform>()` 作为 `sourceRect`。

---

---

## Bug Fix: 拖拽 Ghost 视觉中心与鼠标不对齐 — 2026-03-06 13:16

### 修改文件
- `Assets/Scripts/UI/DragDrop/DragGhostView.cs`

### 内容简述
修复拖拽 Ghost 出现在鼠标下方而非居中于鼠标的问题。

### 根因
Ghost RectTransform pivot = (0.5, 0.5)。`FollowPointer` 将 pivot 点（中心）放置在鼠标位置。然而 `BuildShapeCellsAbsolute` 创建的 shape cells 以 `anchorMin=zero`、`anchoredPosition` 从 `(0,0)` 开始——即 Ghost 的中心——向右向下展开。这导致整个视觉内容偏移到鼠标右下方。

### 修复方案
在 `FollowPointer` 中添加 `centeringOffset = (-size.x * 0.5f, size.y * 0.5f)`，将 Ghost 向左上方偏移半个尺寸，使 shape cell 网格的左上角对齐鼠标，视觉中心呈现在鼠标位置。

---

---

## Bug Fix: DragHighlightLayer 高亮 Tile 与实际格子严重偏移 — 根因修复 — 2026-03-06 13:45

### 修改文件
- `Assets/Scripts/UI/DragHighlightLayer.cs`
- `Assets/Scripts/UI/TrackView.cs`

### 内容简述
完全重写 `DragHighlightLayer`，改为直接读取每个 `SlotCellView` 的 `RectTransform` 来定位高亮 tile，而非用公式计算位置。

### 根因（原方案根本错误）
所有之前的尝试都试图通过 `anchoredPosition = offset + (col * step, -row * step)` 公式计算 tile 位置。这种方式必然失败，因为 `GridLayoutGroup` 可以使用任意 `childAlignment`（如 MiddleCenter）、padding 或 spacing，使格子偏离假设的原点。在不了解 `GridLayoutGroup` 全部内部参数的情况下，没有任何公式能可靠地还原精确的格子位置。

### 修复方案（直接读取格子坐标）
- `DragHighlightLayer.Initialize(container, cells[], gridCols)`：现在直接接收 `SlotCellView[]` 数组，无需 cellSize/cellGap/originOffset。
- `ShowHighlight()`：对每个形状格子偏移，计算 `cellIndex = row * gridCols + col`，然后直接从 `cells[cellIndex].GetComponent<RectTransform>()` 复制 `anchorMin/anchorMax/pivot/anchoredPosition/sizeDelta`。Tile 位置保证像素级精确。
- 移除所有基于公式的字段：`_cellSize`、`_cellGap`、`_originOffset`、`_cell0Rect`。
- `TrackView.CreateHighlightLayer()`：现在调用 `layer.Initialize(col.GridContainer, col.Cells, 2)`——无需 cell0Rect 和尺寸参数。

---

---

## Bug Fix: DragHighlightLayer 诊断日志清理 — 2026-03-06 13:51

### 修改文件
- `Assets/Scripts/UI/DragHighlightLayer.cs`

### 内容简述
在通过 InstanceID 日志确认 `tileParentID == cellParentID == containerID` 且 `tileWorld == cellWorld`（像素级精确匹配）后，移除了 `ShowHighlight()` 中的临时诊断 `Debug.Log`。保留了越界格子索引未命中时的 `Debug.LogWarning`。

---

---

## Bug Fix: 移除 Ghost 放置状态颜色高亮 — 2026-03-06 13:55

### 修改文件
- `Assets/Scripts/UI/DragDrop/DragGhostView.cs`

### 内容简述
移除了拖拽 Ghost 上的绿色/红色边框颜色反馈（`SetDropState`）。放置状态的视觉反馈现在**仅通过 `DragHighlightLayer` 显示在 Track 格子上**。Ghost 边框保持永久透明（`Color.clear`）。这简化了视觉语言：只有一个高亮位置（Track 格子），而非两个。

---

---

## Bug Fix: SAT/SAIL 高亮格子索引不匹配 — 2026-03-06 14:14

### 修改文件
- `Assets/Scripts/UI/DragHighlightLayer.cs`
- `Assets/Scripts/UI/TrackView.cs`
- `Assets/Scripts/UI/SlotCellView.cs`

### 根因
`SlotCellView` 对 SAIL/SAT 格子调用 `SetSingleHighlight(SlotType, col=0, row=CellIndex, state)`。在 `ShowHighlight` 内部，索引被重新计算为 `cellIndex = row * gridCols + col = CellIndex * 2 + 0`。对于 SAT（2×2 网格，gridCols=2），`CellIndex=1`（右上角）→ `cellIndex=2`（左下角），导致高亮出现在错误的格子上。

### 修复方案
- 新增 `DragHighlightLayer.ShowHighlightAtCellIndex(int cellIndex, DropPreviewState)`——直接读取 `cells[cellIndex].RectTransform`，无需行列转换。
- 新增 `TrackView.SetSingleHighlightAtIndex(SlotType, int cellIndex, DropPreviewState)`——路由到新方法。
- 将 `SlotCellView` 的 SAIL/SAT 分支改为调用 `SetSingleHighlightAtIndex(SlotType, CellIndex, state)`，而非 `SetSingleHighlight(SlotType, 0, CellIndex, state)`。
- 移除 `DragHighlightLayer.ShowHighlight()` 中残留的 `[HL]` 调试日志。

---

---

## Bug Fix: ItemOverlayView 幽灵/重复渲染（PRISM/CORE）— 2026-03-06 15:17

### 修改文件
- `Assets/Scripts/UI/TrackView.cs`
- `Assets/Scripts/UI/ItemIconRenderer.cs`

### 根因
`LayoutElement.ignoreLayout = true` 是在 `SetParent()` **之后**才添加到 overlay GameObject 上的。Unity 的 `GridLayoutGroup` 在 `SetParent()` 被调用时**立即**重新定位子节点，覆盖了 `ItemOverlayView.Setup()` 中设置的拉伸锚点（`anchorMin=0,0`、`anchorMax=1,1`）。等到 `ignoreLayout = true` 生效时，overlay 的锚点已被覆写为某个网格格子的位置，导致 overlay 渲染在错误位置（幽灵/重复渲染现象）。

`BuildShapeCellsFromCells` 和 `BuildIconFromCell` 中 overlay 子节点也存在相同的顺序 bug。

### 修复方案
- 在 `TrackView.RefreshColumn` 中：将 `overlayGo.AddComponent<LayoutElement>()` + `ignoreLayout = true` 移到 `SetParent(gridContainer, false)` **之前**。
- 在 `ItemIconRenderer.BuildShapeCellsFromCells` 中：将 `go.AddComponent<LayoutElement>()` + `ignoreLayout = true` 移到 `go.transform.SetParent(parent, false)` 之前。
- 在 `ItemIconRenderer.BuildIconFromCell` 中：同样修复——`LayoutElement` 在 `SetParent` 之前添加。

### 关键洞察
SAIL/SAT 从未出现此 bug，因为它们直接调用 `SlotCellView.SetItem()`——不创建 overlay GameObject，`GridLayoutGroup` 从不介入。这是指向 overlay 创建顺序为根因的诊断线索。

---

---

## Bug Fix: PRISM/CORE 部件无法拖回背包 — 2026-03-06 15:42

### 修改文件
- `Assets/Scripts/UI/SlotCellView.cs`
- `Assets/Scripts/UI/TrackView.cs`
- `Assets/Scripts/UI/ItemOverlayView.cs`

### 根因
`SetHiddenByOverlay()` 将 `DisplayedItem` 设为 `null`。`SlotCellView.OnBeginDrag` 检查 `if (DisplayedItem == null) return`，导致拖拽在开始前被静默中止。

SAIL/SAT 不受影响，因为它们调用 `SetItem(item)` 将 `DisplayedItem = item`，保持了拖拽路径完整。

### 修复方案
- `SlotCellView.SetHiddenByOverlay(StarChartItemSO item)`：修改签名以接收 `item` 参数，设置 `DisplayedItem = item`（保留拖拽来源），仅清除视觉（背景/图标/标签）。
- `TrackView.RefreshColumn`：更新调用为 `cells[cellIndex].SetHiddenByOverlay(item)`。
- `ItemOverlayView.Setup`：移除上一次失败修复尝试中错误添加的 `raycastBlocker` AddComponent。

### 关键洞察
修复方案直接镜像 SAIL/SAT 的行为：在格子上保留 `DisplayedItem`，使 `OnBeginDrag` 能够触发。Overlay 纯粹是视觉层——底层格子仍是功能性的拖拽来源。**Overlay = 纯视觉，Cell = 交互**。

---

---

## Bug Fix: SAIL 放置后默认跳到 Primary 轨道第一格 — 2026-03-06 15:51

### 修改文件
- `Assets/Scripts/UI/TrackView.cs`

### 根因
SAIL 和 SAT 是**全局槽位**——不属于某个特定轨道。`RefreshSailColumn()` 和 `RefreshSatColumn()` 只在 **Primary 轨道**上显示已装备的部件；Secondary 轨道显示空格。

然而 `HasSpaceForSail()` 和 `HasSpaceForSat()` 没有 `isPrimary` 守卫。当用户将 SAIL 部件拖到 **Secondary 轨道的 SAIL 格子**时，`ComputeDropValidity` 返回 `true`（有空间），`DropTargetTrack` 被设为 Secondary 轨道，`EquipLightSail()` 成功调用。

`RefreshAllViews()` 后，SAIL 出现在 **Primary 轨道的第一格**（因为 `RefreshSailColumn` 始终在那里渲染），看起来像是部件"跳"到了错误的位置。

### 修复方案
为两个委托添加 `isPrimary` 守卫：
- `HasSpaceForSail`：若 `_track.Id != Primary` 则立即返回 `false`
- `HasSpaceForSat`：若 `_track.Id != Primary` 则立即返回 `false`

Secondary 轨道的 SAIL/SAT 格子现在显示为无效放置目标（红色高亮），防止令人困惑的视觉跳转。

### 关键洞察
全局槽位（SAIL/SAT）只能在 Primary 轨道上接受放置。Secondary 轨道渲染空格纯粹是为了视觉对称——这些格子不应成为功能性的放置目标。

---

### ✨ Refactor: 伴星（Satellite）从 Shared 改为 Per-Track — 2026-03-06 22:30

**功能说明：** 将伴星数据从 `LoadoutSlot` 级别（Shared）迁移到 `WeaponTrack` 级别（Per-Track），使 Primary 和 Secondary 轨道可以独立配置不同的伴星组合。消除了 `isPrimary` 守卫逻辑，Secondary TrackView 不再显示无意义的空格占位。

**修改文件：**
- `Assets/Scripts/Combat/StarChart/WeaponTrack.cs`
  - 新增 `public readonly List<SatelliteSO> EquippedSatelliteSOs = new()` 字段
  - `ClearAll()` 中新增 `EquippedSatelliteSOs.Clear()`
- `Assets/Scripts/Combat/StarChart/LoadoutSlot.cs`
  - 移除 `EquippedSatelliteSOs` 字段（已迁移至 WeaponTrack）
  - `Clear()` 中移除 `EquippedSatelliteSOs.Clear()`（由 WeaponTrack.ClearAll() 负责）
- `Assets/Scripts/Core/Save/SaveData.cs`
  - `TrackSaveData` 新增 `List<string> SatelliteIDs` 字段（Per-Track 存档）
  - `LoadoutSlotSaveData.SatelliteIDs` 标记为 `[Obsolete]`，保留用于旧存档迁移
- `Assets/Scripts/Combat/StarChart/StarChartController.cs`
  - `_satelliteRunners` 拆分为 `_primarySatRunners` 和 `_secondarySatRunners` 两个并行数组
  - `EquipSatellite(SatelliteSO, WeaponTrack.TrackId)` 新增 TrackId 参数
  - `UnequipSatellite(SatelliteSO, WeaponTrack.TrackId)` 新增 TrackId 参数
  - `GetEquippedSatellites(WeaponTrack.TrackId)` 新增 TrackId 参数
  - `Update()` 分别 Tick Primary/Secondary 伴星 Runner 列表
  - `DisposeSlotRunners()` / `RebuildSlotRunners()` 按轨道分组处理
  - `ExportTrack()` 新增 SatelliteIDs 序列化
  - `ImportFromSaveData()` 新增旧格式迁移逻辑（旧 SatelliteIDs → PrimaryTrack）
  - 新增 `ImportTrackSatellites()` 辅助方法（含 ID 解析失败警告）
  - `InitializeSailAndSatellites()` debug 伴星改为写入 PrimaryTrack
- `Assets/Scripts/UI/TrackView.cs`
  - `RefreshSatColumn()` 直接读取 `_track.EquippedSatelliteSOs`，移除 `isPrimary` 守卫
  - `HasSpaceForSat()` 移除 Primary 限制，改为检查当前轨道伴星数量（< 4）
- `Assets/Scripts/UI/StarChartPanel.cs`
  - `GetEquippedLocation()` SAT 改为检查 Primary/Secondary 各自的 EquippedSatelliteSOs
  - `CountEquipped()` 改为累加两个轨道的伴星数量
  - `EquipItem()` SAT 改为 `EquipSatellite(sat, _selectedTrack.Id)`
  - `UnequipItem()` SAT 改为按归属轨道调用 `UnequipSatellite(sat, trackId)`
  - `IsItemEquipped()` SAT 改为检查两个轨道的 EquippedSatelliteSOs
- `Assets/Scripts/UI/DragDrop/DragDropManager.cs`
  - `EquipToTrack()` SAT 改为 `EquipSatellite(sat, track.Id)`，maxSats 改为 4（每轨道 2×2）
  - `UnequipFromTrack()` SAT 改为 `UnequipSatellite(sat, track.Id)`

**存档迁移规则：**
- 旧格式：`LoadoutSlotSaveData.SatelliteIDs` 非空 → 全部迁移到 `PrimaryTrack.SatelliteIDs`，Secondary 为空
- 新格式：`TrackSaveData.SatelliteIDs` 分别存储 Primary/Secondary 的伴星 ID
- 无法解析的 ID 跳过并输出 `Debug.LogWarning`，不抛出异常

**Per-Track Runner 扩展点：**
- `_primarySatRunners[slotIndex]` 和 `_secondarySatRunners[slotIndex]` 分别管理两个轨道的 Runner
- 未来如需 Per-Track Context（如 `WeaponTrack.ResetCooldown` 触发），只需向 `SatelliteRunner` 构造函数传入轨道引用，无需修改基类

---

## Docs: Minishoot 关卡架构综合分析启动 — 2026-03-10 23:00

### 修改文件
- `Docs/Reference/Level_Architecture_Synthesis_Minishoot_Silksong_TUNIC.md`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容简述
新建跨游戏关卡架构综合分析文档，并先完成 Minishoot 章节的第一轮深度分析；后续将继续补完 Silksong 与 TUNIC。当前已整理 Minishoot 的场景分层、Location/Transition/Encounter/Lock/Biome/Camera 架构、世界状态绑定、Overworld 导航平面与可复用标准件方法论。

### 目的
为 Project Ark 下一阶段的“可玩关卡切片”与长期关卡生产流程提供经过拆解验证的参考模型，重点提炼 Minishoot 在开放世界射击银河城中的空间组织、遭遇节奏、回路设计与轻量导演系统长处。

### 技术
- 基于解包 Unity 项目的脚本/场景结构逆向分析
- 以 `Location -> Transition -> Encounter/Lock -> Camera/Biome -> WorldState` 为框架提炼关卡语法
- 将参考结论翻译为可映射到 Project Ark 的标准件与生产方法论

---

## Docs: Minishoot 关卡架构综合分析文档 — 2026-03-10 23:00

### 新建/修改文件
- `Docs/Reference/Level_Architecture_Synthesis_Minishoot_Silksong_TUNIC.md`

### 内容
- 新建跨游戏关卡架构综合分析文档，先完成 `Minishoot' Adventures` 章节。
- 基于解包工程中的场景与关卡脚本，整理其 `Location -> Transition -> Encounter/Lock -> Biome/Camera -> Activation` 的分层结构。
- 提炼 Minishoot 在开放探索、封闭遭遇、世界状态门锁、轻量导演 Trigger、运行时激活策略等方面的长处，并映射到 `Project Ark` 的 `Room / Door / Encounter / Camera / Chunk Activation` 方向。
- 预留 `Silksong`、`TUNIC` 和跨游戏综合结论章节，供后续继续追加。

### 目的
- 为《静默方舟》接下来的可玩关卡切片提供更高质量的外部参照，不只分析系统结构，还聚焦“关卡如何被组织和生产出来”。
- 形成一个可持续扩充的参考文档，后续可直接汇总多款参考游戏的优点，反哺示巴星与后续星球的关卡架构设计。

### 技术
- 文档研究与架构归纳
- 基于 Unity 解包项目的场景/脚本逆向分析
- 以 `Project Ark` 当前房间系统和关卡构建阶段为目标的对照式提炼

---

## Docs: Minishoot 房间组织与节奏法则深化 — 2026-03-10 23:11

### 修改文件
- `Docs/Reference/Level_Architecture_Synthesis_Minishoot_Silksong_TUNIC.md`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容简述
继续深化 Minishoot 章节，将分析重点从“系统架构”推进到“房间组织与节奏法则”。新增了节点分级、开放遭遇与关闭遭遇的量级对比、房间连接语义、首段节拍模板，以及可直接映射到示巴星前 15 分钟切片的房间表。

### 目的
把 Minishoot 的参考价值从抽象拆解报告进一步转化为可直接指导 Project Ark 关卡搭建的设计规则，尤其服务于示巴星首个可玩切片的房间排布、节奏闭环与回路设计。

### 技术
- 基于 `Cave.unity` 与 `Overworld.unity` 中 `EncounterOpen / EncounterClose / CameraTrigger / Biome / Checkpoint` 的对象命名与数量分布进行结构推断
- 将关卡内容抽象为过路节点、压力节点、清算节点、回报节点、锚点节点、回路节点六类房间职责
- 将 Minishoot 的探索流、清算流、回报流、回返流整理为可直接照抄的切片节拍模板

---

## Docs: Minishoot 互动件与传送机制词典 — 2026-03-10 23:11

### 修改文件
- `Docs/Reference/Level_Architecture_Synthesis_Minishoot_Silksong_TUNIC.md`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容简述
继续扩展 Minishoot 章节，新增“机制词典”层分析。整理了门锁部件、钥匙/解锁器、火炬链、隐藏区、埋伏遭遇、可破坏环境、以及标准/特殊转送机制的职责、触发方式、持久化方式与可借鉴点，并提炼出“Minishoot 的互动件本质上是状态改写库”的总总结。

### 目的
把 Minishoot 的参考价值从房间组织与节奏法则继续推进到“可直接映射为关卡标准件”的粒度，为 Project Ark 后续定义示巴星的门、机关、隐藏区、可破坏环境和转场语法提供参照。

### 技术
- 基于 `Unlocker`、`UnlockerTorch`、`HiddenArea`、`EncounterHidden`、`Destroyable`、`StrongDoor`、`TeleportOutTrigger`、`KeyUnique/CrystalKey/BossKey` 等脚本进行部件级逆向分析
- 按“结构改写 / 可见性改写 / 节奏改写 / 持久性改写”四类状态变化重组互动机制
- 将参考机制转译为适合 Project Ark 的标准件设计问题与映射建议

---

## Docs: Minishoot 代表性房间样本拆解 — 2026-03-11 01:04

### 修改文件
- `Docs/Reference/Level_Architecture_Synthesis_Minishoot_Silksong_TUNIC.md`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容简述
继续扩展 Minishoot 章节，新增“代表性房间样本拆解”与“4 个可直接照抄的房间模板”。基于 `Cave.unity` / `Overworld.unity` 中 `CameraTriggerIntro`、`CameraTriggerMuseum`、`CameraTriggerPrimordial`、`CaveCheckpoint0`、`OverworldCheckpoint0`、`OverworldEncounterClose0`、`DoorBossDungeon` 等对象的命名与邻接关系，整理出引导房、结构锚点房、主题锚点房、开放压力房、清算房、导航枢纽房、章节门槛房、隐藏支房等样本，并将其压缩为可直接映射到示巴星切片的 4 类模板与推荐组合顺序。

### 目的
把前面对 Minishoot 的系统级与机制级拆解继续下沉到“实际搭房间时可直接套用”的粒度，让示巴星首个可玩切片在房间职责、组合顺序与节奏闭环上有明确参考骨架。

### 技术
- 基于 Unity 场景 YAML 中的对象命名、Checkpoint/Transition 邻接关系与 Encounter 类型分布做模式化重建
- 结合已完成的 Minishoot 机制词典、房间组织法则与节奏分析，提炼为房间原型样板
- 将样本结果转译成适合 `Project Ark` 的房间模板与切片级排布建议

---

## Docs: Minishoot 房间模板标准件化 — 2026-03-11 01:15

### 修改文件
- `Docs/Reference/Level_Architecture_Synthesis_Minishoot_Silksong_TUNIC.md`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容简述
继续深化 Minishoot 章节，将前面 8 类代表性房间样本进一步下沉为生产模板。为每类房间分别补充了“标准组件清单 + 进出规则 + 奖励规则”，并新增一张奖励适配对照表，明确哪些奖励适合放在开场引导房、结构锚点房、开放压力房、清算房、导航枢纽房、章节门槛房、隐藏支房中。

### 目的
把参考分析从“知道这种房间为何存在”推进到“搭这种房间时最低限度该放什么、什么时候该让玩家通过、通过后该给什么”，降低示巴星切片从分析文档到实际生产之间的转译成本。

### 技术
- 以房间职责为核心，将 Minishoot 的样本类型重写为关卡生产模板
- 按“组件 / 进入条件 / 退出条件 / 奖励类型”四个维度统一整理房间语法
- 将奖励分为信息奖励、结构奖励、推进奖励、资源奖励、长期承诺奖励，形成可直接映射到 `Project Ark` 的排布标准

---

## Docs: 示巴星房间生产检查表 — 2026-03-11 01:20

### 修改文件
- `Docs/Reference/Level_Architecture_Synthesis_Minishoot_Silksong_TUNIC.md`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容简述
继续在 Minishoot 章节末尾新增“示巴星房间生产检查表”，把前面 8 类房间样本压缩成一张可直接用于 `Project Ark` 制作现场的 checklist。新增了通用总检查表、8 类房间专项检查表、示巴星首切片专用简化版字段表，以及一个 `SH-SLICE-R04` 的填写范例，方便后续直接拿来定义每个房间的职责、组件、进出条件与奖励结算。

### 目的
把参考文档从“分析与归纳”继续推进到“可执行的生产模板”，让示巴星切片在实际排房间、写房间表、做关卡 review 时有统一的检查标准，减少房间职责发散和奖励失衡。

### 技术
- 将既有 8 类房间模型重组为 checklist 结构
- 按“通用检查 / 类型专项检查 / 切片字段模板 / 填写范例”四层组织生产规范
- 以单一主职责、明确通过条件、风险与奖励匹配为核心约束，形成适用于 `Project Ark` 的关卡生产纪律

---

---

## Docs: TUNIC 关卡架构拆解启动 — 2026-03-11 11:27

### 修改文件
- `Docs/Reference/Level_Architecture_Synthesis_Minishoot_Silksong_TUNIC.md`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容简述
开始补写 `TUNIC` 章节。基于 `il2cpp` 反编译产物中的 `SceneLoader`、`ScenePortal`、`PlayerCharacterSpawn`、`DungeonRoom`、`Campfire`、`Door`、`HolySealDoor`、`TempleDoor`、`ConduitNode`、`ConduitTeleporter`、`Fuse`、`PagePickup`、`PageDisplay`、`GameHelpManager`、`PageCondition` 与 `Profile` 等结构，整理出 TUNIC 的五层关卡架构：跨场景入口系统、场景内可见性切片、分层门槛系统、篝火回返锚点，以及“知识即通行证”的 manual/page 条件体系。同步补充了其对 `Project Ark` 的直接映射规则与可借鉴长处。

### 目的
把参考分析从 Minishoot 的“空间组织与可生产语法”推进到 TUNIC 的“视角遮蔽、知识门槛、认知重读”方向，为《静默方舟》后续落实“理解即武器”提供更贴近系统层的外部参照。

### 技术
- 基于 `il2cpp` 反编译代码与字符串表进行系统级逆向分析，而非依赖完整场景 YAML
- 按“跨场景跳转 / 场景内切片 / 门槛分层 / 回返锚点 / 知识条件”五层结构重组 TUNIC 的关卡逻辑
- 将 `TUNIC` 的知识门槛与世界状态系统翻译为适合 `Project Ark` 的生产规则与设计问题

---

---

## Docs: TUNIC 机制词典与房间模板细化 — 2026-03-11 11:34

### 修改文件
- `Docs/Reference/Level_Architecture_Synthesis_Minishoot_Silksong_TUNIC.md`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容简述
继续深化 `TUNIC` 章节，补写了接近 Minishoot 粒度的机制词典与房间模板分析。新增内容包括：`ScenePortal / ShopScenePortal / PlayerCharacterSpawn` 的跨场景入口件、`DungeonRoom / DungeonRoomRigidbodyTracker` 的场景内切片件、`Campfire / ReturnToCampfireTrigger / Profile` 的回返锚点件、`Door / ProximityDoor / HolySealDoor / TempleDoor / ObsidianDoorway` 的分层门槛件、`PrayerListenerBase / ConduitTeleporter / Fuse / TimeDevice` 的世界改写件、`SecretPassagePanel / RotatingCubeClue` 的隐藏与提示件，以及 `PagePickup / PageDisplay / GameHelpManager / PageCondition` 的知识门槛件。随后又将这些系统压缩成 TUNIC 的房间组织法则、8 类高可信房间模板，以及面向示巴星的 4 个“认知重读”生产问题。

### 目的
把 `TUNIC` 的参考价值从“高层理念”推进到“可拿来指导关卡生产”的颗粒度，使其在方法论上真正能与前面的 Minishoot 分析并列：前者提供空间与奖励语法，后者补充遮蔽、重读与知识门槛语法。

### 技术
- 基于 `il2cpp` 反编译结构持续做部件级归类，而非依赖场景坐标还原
- 按“跨场景入口 / 场景切片 / 回返锚点 / 门槛分层 / 世界改写 / 知识条件”重组 `TUNIC` 的关卡标准件
- 将系统结论进一步压缩为房间组织法则、房间模板和示巴星可执行的设计提问清单

---

---

## Docs: TUNIC 关卡系统扫尾补遗 — 2026-03-11 11:41

### 修改文件
- `Docs/Reference/Level_Architecture_Synthesis_Minishoot_Silksong_TUNIC.md`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容简述
继续对 `TUNIC` 做关卡系统扫尾遍历，并将补遗结果追加到参考文档。新增了对 `Chest`、`BloodstainChest`、`BloodstainManager`、`CameraPositionOverrideTrigger`、`PlayerCharacterSpiritSpawn`、`BedToggle`、`SleepPrompt`、`AreaData`、`AreaLabel`、`TropicalSecret` 等对象的分析，进一步说明 TUNIC 如何通过宝箱条件化、血痕回收、镜头重映射、灵体/现实出生点映射、昼夜睡眠节点、区域标题与少量区域特供秘密脚本来“缝合”整个世界。文档中同时新增了一个 6 层标准件层级总结，以及对后续 `TUNIC 代表性认知样本拆解` 的方法说明。

### 目的
把 `TUNIC` 的拆解从“主骨架已清楚”推进到“边缘但关键的缝合系统也已纳入版图”，提高覆盖度与可信度，避免后续把它误读成只有门、祈祷和手册三套系统。

### 技术
- 针对尚未覆盖的高权重边缘系统做定向反编译阅读
- 将补遗系统按“奖励节点 / 回返风险 / 镜头解释 / 双世界映射 / 时间相位 / 区域确认”重新归类
- 在文档中将主骨架与补遗部件统一压缩为一套 6 层关卡标准件层级

---

---

## Docs: TUNIC 代表性认知样本拆解 — 2026-03-11 11:43

### 修改文件
- `Docs/Reference/Level_Architecture_Synthesis_Minishoot_Silksong_TUNIC.md`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容简述
继续深化 `TUNIC` 章节，新增“代表性认知样本拆解”。基于前面已确认的入口系统、房间切片、世界改写、回返锚点与知识条件，整理出 7 类高可信认知样本：远景承诺->后续证明房、局部遮蔽->空间重读房、垂直差->前后景反转房、祈祷改写->世界重排房、篝火回返->风险重挂载房、页面提示->知识解锁房、双世界对应->旧区域翻面房。随后进一步压缩为 4 个适合 `Project Ark` 借用的模板，并明确指出示巴星首切片最值得优先借用的 3 类 TUNIC 样本。

### 目的
把 `TUNIC` 的拆解推进到真正能和 `Minishoot` 并列的“样本级”颗粒度，只是维度从空间职责转为认知阶段与重读结构，为后续将其方法论翻译到示巴星关卡提供更直接的中间层。

### 技术
- 以系统证据为基础做“认知样本”重建，而非假装拥有完整房间坐标数据
- 按“第一次阅读 / 误读点 / 第二次阅读 / 承诺对象 / 验证对象 / Ark 映射”六个维度统一拆解 TUNIC 样本
- 再将样本压缩为适合 `Project Ark` 的 4 类模板与 3 类优先借鉴对象

---

---

## Docs: Silksong 关卡架构拆解启动 — 2026-03-11 11:46

### 修改文件
- `Docs/Reference/Level_Architecture_Synthesis_Minishoot_Silksong_TUNIC.md`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容简述
开始补写 `Silksong` 章节第一轮总骨架。基于 `SceneTransitionZone`、`SceneTransitionZoneBase`、`WorldNavigation`、`CameraController`、`AreaTitleController`、`GameMap`、`InventoryWideMap`、`FastTravelMapButton`、`Trapdoor`、`TripWire`、`PositionActivator` 等脚本，先整理出其关卡架构的六个关键层面：显式场景图谱、目标场景+目标 Gate 的双字段转场语法、入口条件化的区域标题系统、条件化地图/宽图/快速旅行导航闭环、平台与轻机关标准件，以及镜头作为节奏控制器的角色。同步补充了其与 Minishoot / TUNIC 的差异定位，以及对 `Project Ark` 的第一批直接启示。

### 目的
在正式深入 `Silksong` 的机制词典与房间样本之前，先建立一个可信的总骨架，明确它最值得 Ark 借用的方向不是单个机关，而是“大规模房间网络的可导航生产方法”。

### 技术
- 基于 `Assembly-CSharp` 脚本做系统层逆向分析，而非依赖场景 YAML 逐房间复原
- 以“场景图谱 / 转场语法 / 导航闭环 / 平台机关 / 镜头控制”五个维度重组 Silksong 的关卡主骨架
- 将结论直接翻译为适合 `Project Ark` 的房间入口、地图、机关与镜头生产建议

---

## Docs: Silksong 机制词典补完（第一轮）— 2026-03-11 11:56

### 修改文件
- `Docs/Reference/Level_Architecture_Synthesis_Minishoot_Silksong_TUNIC.md`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容简述
继续深化 `Silksong` 章节，在原有总骨架基础上新增“机制词典”层分析。基于 `WorldNavigation`、`SceneTransitionZone`、`AreaTitleController`、`GameMapScene`、`FastTravelMapButtonBase`、`FastTravelMapPieceBase`、`CameraLockArea`、`PositionActivatorRange`、`SetPosConditional`、`GateSnap` 等脚本，把 `Silksong` 当前可确认的标准件压缩为世界图谱件、转场件、区域确认件、地图分层件、宽图导航件、快速旅行件、镜头锁区件、位置阈值件、轻机关件九类，并进一步归纳成 6 层标准件层级。最后明确指出它的核心价值更接近“导航生产工具箱”，重点不在单房间奇观，而在多房间、多入口、多层次世界中的可维护导航结构。

### 目的
把 `Silksong` 的拆解推进到与前面 `Minishoot` / `TUNIC` 相同的方法论层级，让后续继续下沉到房间组织、节奏法则与样本拆解时，有一套统一的标准件语言可依托。

### 技术
- 继续采用 `Assembly-CSharp` 逆向证据，优先抽取字段与职责稳定的关卡标准件
- 按“世界图谱 -> 转场 -> 区域确认 -> 导航可视化 -> 镜头边界 -> 路径机关”重组为生产层级
- 将每类标准件同步翻译为 `Project Ark` 可直接借用的架构建议，避免只停留在参考游戏描述

---

## Docs: Silksong 房间组织与节奏法则（第一轮）— 2026-03-11 12:07

### 修改文件
- `Docs/Reference/Level_Architecture_Synthesis_Minishoot_Silksong_TUNIC.md`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容简述
继续深化 `Silksong` 章节，在“机制词典”之下新增“房间组织法则 + 节奏法则 + 高可信模板”三层总结。基于 `WorldNavigation` 的显式 scene 图、`SceneTransitionZoneBase` 的 `targetScene + targetGate` 转场语法、`AreaTitleController` 的入口条件化区域播报、`CameraLockArea` 的镜头边界、`TripWire` / `Trapdoor` / `PositionActivatorRange` / `SetPosConditional` 的轻机关与位置阈值，归纳出 6 条房间组织法则：scene 作为正式节点、入口方向影响第一阅读、镜头边界属于房间本体、平台推进依赖轻机关切拍、导航锚点周期性减负、回返路径是房间图第二形态。随后再压缩出推进房、垂直筛选房、轻机关切拍房、区域确认房、结构压缩房 5 个高可信模板，并把结论翻译为示巴星首切片可直接借用的 4 个生产原则。

### 目的
把 `Silksong` 从“系统标准件拆解”推进到真正能指导搭房的层级，为后续继续做代表性房间样本拆解、奖励规则与跨游戏综合结论建立统一中间层。

### 技术
- 用“系统证据 -> 组织法则 -> 节奏法则 -> 房型模板”的方法，从脚本层直接重建关卡生产逻辑
- 重点关注入口、镜头、导航、捷径这些大型地图中最容易被忽略但最决定体验稳定性的结构件
- 将平台银河城经验翻译为适合 `Project Ark` 顶视角关卡的结构语言，而不是直接照搬横版动作形式

---

## Docs: Silksong 代表性房间样本拆解（高可信版）— 2026-03-11 12:33

### 修改文件
- `Docs/Reference/Level_Architecture_Synthesis_Minishoot_Silksong_TUNIC.md`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容简述
继续深化 `Silksong` 章节，在房间组织法则之后补入“代表性房间样本拆解（高可信版）”。由于当前证据主要来自 `WorldNavigation` 的场景命名/出口命名和若干关卡标准件脚本，因此不伪装成逐场景几何复原，而是以 scene 在世界图中的结构职责为核心，拆出 7 类高可信样本：`Tutorial_01` 教学推进房、`Town` 主枢纽导航房、`Room_Town_Stag_Station` 结构压缩房、`Room_mapper` 制图确认房、`Room_shop` / `Room_Mender_House` 生活侧室房、`Room_temple` 章节门槛房、`MazeMistZone` 惩罚式重定向房。随后把这 7 类再压缩为 4 个适合 `Project Ark` 借用的生产模板，并明确指出示巴星首切片最值得先借的 3 个样本：主枢纽导航房、结构压缩房、制图确认房。

### 目的
把 `Silksong` 的拆解从抽象法则推进到“样本级”颗粒度，使其能和前面的 `Minishoot` / `TUNIC` 一样，直接为示巴星首个可玩切片提供可用的房间参照物与搭建顺序依据。

### 技术
- 严格遵守证据边界：基于场景名、Gate 名、功能脚本职责重建“世界图中的房间角色”，而非假装拥有完整场景布局
- 按“已知证据 -> 高可信职责 -> 标准组件推断 -> 节奏作用 -> Ark 翻译”统一拆解每类样本
- 再将样本压缩为适合 `Project Ark` 的模板层与优先借鉴层，方便后续继续做规则表与跨游戏综合结论

---

## Docs: Silksong 房间生产模板继续下沉 — 2026-03-11 13:52

### 修改文件
- `Docs/Reference/Level_Architecture_Synthesis_Minishoot_Silksong_TUNIC.md`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容简述
继续深化 `Silksong` 章节，把前面 7 类代表性样本继续下沉到与 `Minishoot` 相同的“标准组件清单 + 进出规则 + 奖励规则”颗粒度。分别为教学推进房、主枢纽导航房、结构压缩房、制图确认房、生活侧室房、章节门槛房、惩罚式重定向房补充最低限度组件、进入/退出条件、以及最适合的奖励类型。额外新增一节专门总结 `Silksong` 奖励逻辑的核心并不偏向资源，而偏向导航奖励、压缩奖励、解释奖励、许可奖励，并进一步直接翻译成示巴星可使用的房间命名语言：初入废墟引导房、钟城主锚点房、共鸣中继站房、测绘确认房、低压功能房、大钟门槛房、失谐迷廊重置房。

### 目的
把 `Silksong` 从样本级拆解推进到真正可用于房间生产表和关卡清单的语言层，为后续整理 `Silksong` 版本的示巴星房间生产检查表，以及最终跨游戏综合结论打下基础。

### 技术
- 参照前面 `Minishoot` 的生产模板结构，统一补齐组件、进出条件、奖励规则三元组
- 强调 `Silksong` 奖励更偏结构性收益，而非掉落式收益，避免错误照搬
- 直接把参考游戏术语翻译为适合 `Project Ark` 世界观与示巴星语境的房型命名

---

## Docs: Silksong 版示巴星房间生产检查表 — 2026-03-11 14:08

### 修改文件
- `Docs/Reference/Level_Architecture_Synthesis_Minishoot_Silksong_TUNIC.md`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容简述
继续把 `Silksong` 章节从生产模板推进到真正可执行的检查表层级，新增“Silksong 版示巴星房间生产检查表”。结构上参考前面 `Minishoot` 的检查表格式，但将重点调整为 `Silksong` 特有的导航与结构问题：图节点身份、入口语义、导航减负职责、回返价值、记忆锚点、结构性奖励等。检查表包含 4 个部分：通用总检查表、7 类房间专项检查表、示巴星首切片专用简化版、以及一个 `SH-SLICE-R05` 的共鸣中继站房填写范例。最后补充了 `Silksong` 版执行纪律，强制每个房间回答“它是什么节点、回返是否变义、让玩家更懂了什么或更省了什么、奖励是否为结构性收益”这四个问题。

### 目的
让 `Silksong` 的参考价值不只停留在分析层，而是能像前面 `Minishoot` 一样，直接转化成示巴星房间排产和关卡制作前置检查的工作文档。

### 技术
- 沿用已有检查表的表格结构，保证不同参考游戏的生产语言可以横向对照
- 将 `Silksong` 的重点从“房间是否成立”转向“路网节点是否成立、回返是否成立、导航减负是否成立”
- 用示巴星命名语境和可直接复用的房型字段，缩短从参考分析到实际排房的转换距离

---

## Docs: 跨游戏综合结论与示巴星推荐骨架 — 2026-03-11 14:19

### 修改文件
- `Docs/Reference/Level_Architecture_Synthesis_Minishoot_Silksong_TUNIC.md`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容简述
完成 `## 4. 跨游戏综合结论`。将 `Minishoot`、`Silksong`、`TUNIC` 三部分分析压缩为面向 `Project Ark` 的最终执行结论：`Minishoot` 负责房间语法，`Silksong` 负责路网语法，`TUNIC` 负责理解语法。随后区分哪些长处适合 Ark 直接采纳，哪些只能借思想不借形式，并进一步把三者压成一套统一的生产语言：房间职责、图节点身份、首次/回返语义变化、理解增量、奖励类型。最后给出示巴星首个可玩切片的推荐架构模板与 8 房间顺序：初入废墟引导房 -> 开放压力房 -> 测绘确认房 -> 局部遮蔽/空间重读房 -> 节点清算房 -> 大钟门槛房 -> 共鸣中继站房 -> 钟城主锚点房。

### 目的
结束参考分析阶段，把三款参考游戏的价值真正收束为一个能指导示巴星首切片排房、定节奏、定奖励、定知识门槛的最终中间层。

### 技术
- 按“直接采纳 / 借思想不借形式 / 统一生产语言 / 推荐切片骨架”四层结构整合三款参考游戏
- 将抽象比较压回到示巴星可落地的房间顺序与执行原则，避免综合结论继续停留在概念层
- 保持对项目主轴“理解即武器”的对齐，使 `TUNIC` 的知识门槛方法与 `Minishoot` / `Silksong` 的结构纪律形成同一套工作文档

---

---

## Minishoot 标准件落地 Step 1 — 补齐关卡架构缺口 (2026-03-13)

### 目的
以 Minishoot 的关卡架构为底子，补齐 Ark 现有关卡系统缺失的标准件。这些组件构成了银河恶魔城关卡的"语法基础"，后续 Silksong/TUNIC 层的扩展都基于此底座。

### 新建文件

| 文件路径 | 说明 |
|---------|------|
| `Assets/Scripts/Level/Data/EncounterMode.cs` | EncounterMode 枚举（Closed 封门清算 / Open 开放骚扰），区分 Minishoot 两种遭遇范式 |
| `Assets/Scripts/Level/Data/RoomAmbienceSO.cs` | 房间级氛围预设 SO（颜色滤镜、暗角、BGM、低通滤波、粒子），BiomeTrigger 的数据层 |
| `Assets/Scripts/Level/Room/OpenEncounterTrigger.cs` | 开放遭遇触发器：玩家进入触发区激活敌群，不锁门，离开后有宽限期再消敌，支持重进重触发 |
| `Assets/Scripts/Level/DynamicWorld/BiomeTrigger.cs` | 房间级氛围切换触发器：进入时覆盖全局后处理/音频，退出时恢复，支持粒子实例化 |
| `Assets/Scripts/Level/Camera/CameraTrigger.cs` | 镜头导演触发器：进入时调整 Cinemachine ortho size / Follow 目标，退出时平滑恢复 |
| `Assets/Scripts/Level/Room/ActivationGroup.cs` | 通用可激活对象组：Room 进入/离开时批量启停装饰/灯光/互动件，性能优化 |
| `Assets/Scripts/Level/Room/HiddenAreaMask.cs` | 隐藏区域遮罩：玩家进入时淡出覆盖 Sprite，退出时淡回，支持永久揭示 |
| `Assets/Scripts/UI/AreaTitleDisplay.cs` | 区域名弹出 UI：首次进入房间时显示标题卡片（fade+slide 动画），监听 LevelEvents |

### 修改文件

| 文件路径 | 修改内容 |
|---------|---------|
| `Assets/Scripts/Level/Data/EncounterSO.cs` | 新增 `_mode` 字段和 `Mode` 属性（EncounterMode 枚举） |
| `Assets/Scripts/Level/Room/Room.cs` | 新增 `_openEncounters` 数组，Awake 中自动收集 OpenEncounterTrigger，ResetEnemies 中同时重置 |
| `Assets/Scripts/Level/Room/Door.cs` | 补完视觉反馈系统：新增 SpriteRenderer 引用 + 5 种状态颜色 + PrimeTween 颜色过渡动画 |

### 技术要点
- **数据驱动**：RoomAmbienceSO 作为独立 SO 资产，BiomeTrigger 只引用不 hardcode
- **UniTask + PrimeTween**：所有异步等待和补间动画遵循项目规范
- **事件解耦**：AreaTitleDisplay 通过 LevelEvents 事件总线监听，不直接引用 RoomManager
- **防御性重置**：OpenEncounterTrigger.ResetEncounter() 确保死亡重生时正确清理状态
- **CancellationToken 生命周期管理**：BiomeTrigger / OpenEncounterTrigger 都正确管理 CTS

### Minishoot 标准件对齐情况（更新后）

| Minishoot 标准件 | Ark 实现 | 状态 |
|---|---|---|
| `Location` | `Room` + `RoomSO` | ✅ |
| `Transition` | `Door` + `DoorTransitionController` | ✅ |
| `EncounterClose` | `ArenaController` + `WaveSpawnStrategy` | ✅ |
| `EncounterOpen` | `OpenEncounterTrigger` | ✅ 新增 |
| `WorldState` | `WorldProgressManager` + `SaveBridge` | ✅ |
| `BiomeTrigger` | `BiomeTrigger` + `RoomAmbienceSO` | ✅ 新增 |
| `CameraTrigger` | `CameraTrigger` | ✅ 新增 |
| `ActivationManager` | `ActivationGroup` | ✅ 新增 |
| `Checkpoint` | `CheckpointManager` + `Checkpoint` | ✅ |
| `DoorLocked` | `Door` 5 种 DoorState | ✅ |
| `HiddenArea` | `HiddenAreaMask` | ✅ 新增 |
| `AreaTitle` | `AreaTitleDisplay` | ✅ 新增 |
| `Door 视觉反馈` | `Door.ApplyVisualForState()` | ✅ 补完 |

---

## 示巴星房间语法规范 Step 2 — 生产语言文档化 + 首切片定义 (2026-03-13)

### 目的
把三款参考游戏（Minishoot / Silksong / TUNIC）综合分析的精华，压缩为 Ark 可直接执行的房间生产规范。为后续关卡搭建提供"先回答问题再动手"的纪律。

### 新建文件

| 文件路径 | 说明 |
|---------|------|
| `Docs/LevelDesigns/ShebaRoomGrammar.md` | 示巴星房间语法规范 v1.0 —— 含场景层级规范、8 类房间职责模板、5 个必答问题统一生产语言、通用检查表、8 类专项模板、首切片 R01-R07 完整定义与路网拓扑、节奏闭环验证 |
| `Docs/LevelDesigns/Sheba_FirstSlice.json` | 示巴星首切片结构化房间数据 —— R01-R07 的 JSON 定义，含位置、尺寸、elements、connections、验证清单，可直接用于关卡搭建参照 |

### 修改文件

| 文件路径 | 修改内容 |
|---------|---------|
| `Assets/Scripts/Level/Data/RoomType.cs` | 扩展 RoomType 枚举：新增 `Hub`（导航枢纽）和 `Gate`（章节门槛）两个类型，对齐 8 类职责模板 |

### 关键设计决策

1. **8 类房间职责不替代 RoomType 枚举**：RoomType 是技术分类（影响代码逻辑），8 类职责是设计标签（影响 authoring 内容选择）。两者各司其职
2. **场景层级规范固定 6 个子分组**：Tilemaps / Elements / Encounters / Decoration / Triggers / ActivationGroups，禁止散放
3. **统一生产语言 5 个必答问题**：综合三款参考游戏，任何一个答不清的房间不进入生产
4. **首切片 R01-R07 形成完整节奏闭环**：进入→施压→证明→回报→回路→收束 6 拍齐全
5. **路网有回路**：R06 捷径连回 R01/R02，R05 可回连 R02，不是纯线性

### 首切片房间表摘要

| Room | 类型 | 节奏拍 | 核心体验 |
|------|------|--------|----------|
| R01 | INTRO | 进入 | "我掉进了一个死寂但有秩序的世界" |
| R02 | PRESSURE | 施压 | "安静不是安全，噪音是代价" |
| R03 | MUSEUM | 锚点 | "这个世界有历史" |
| R04 | ARENA | 证明 | "主动制造噪音反而让我活下来" |
| R05 | ANCHOR | 回报 | "我学会了一件事" |
| R06 | GATE | 回路 | "我看到了远方，但现在还去不了" |
| R07 | ANCHOR/HUB | 收束 | "这是完整的一段旅程" |

---

---

## 相机系统增强 P0-P5 — 2026-03-13

### 目的
将 Ark 相机系统提升至 Minishoot/Silksong 水平，实现 6 项关键改进。

### 新建文件

| 文件路径 | 说明 |
|---------|------|
| `Assets/Scripts/Level/Camera/CameraDirector.cs` | 核心相机控制器，管理 6 种模式（FOLLOWING/LOCKED/PANNING/FROZEN/FADEOUT/FADEIN），支持触发器堆栈、Pan-to-Target、平滑过渡 |

### 修改文件

| 文件路径 | 修改内容 |
|---------|---------|
| `Assets/Scripts/Level/Camera/CameraTrigger.cs` | 重写使用 CameraDirector API，新增 priority、enterSFX、clearProjectilesOnEnter、positionLock 等字段 |
| `Assets/Scripts/Level/Room/DoorTransitionController.cs` | 添加 CameraDirector 引用，layer transition 时使用 CameraDirector 处理 camera zoom（保持 fallback 兼容） |

### 技术要点

- **模式系统**：CameraMode 枚举定义 6 种相机模式，SetMode() 管理状态切换
- **触发器堆栈**：CameraDirector.PushTrigger/PopTrigger 实现优先级仲裁，最高优先级触发器控制相机
- **Pan-to-Target**：PanToPosition/PanToTarget 方法支持镜头飞向目标后返回
- **Fade 服务**：CameraDirector 内置 FadeInAsync/FadeOutAsync（使用 CanvasGroup）
- **ServiceLocator**：所有管理器通过 ServiceLocator 注册，禁止 FindAnyObjectByType
- **PrimeTween**：所有平滑过渡使用 Tween.Custom + useUnscaledTime

### Minishoot 标准件对齐情况

| Minishoot 标准件 | Ark 实现 | 状态 |
|-----------------|---------|------|
| CameraManager | CameraDirector | ✅ 新增 |
| CameraTrigger | CameraTrigger (重写) | ✅ 升级 |
| CameraMode (6种) | CameraMode 枚举 | ✅ 新增 |
| CameraTrigger.priority | CameraTrigger._priority | ✅ 新增 |
| CameraTrigger.enterSFX | CameraTrigger._enterSFX | ✅ 新增 |
| CameraTrigger.clearProjectiles | CameraTrigger._clearProjectilesOnEnter | ✅ 新增 |
| Pan-to-Target | CameraDirector.PanToTarget() | ✅ 新增 |
| Trigger stacking | CameraDirector._activeTriggerStack | ✅ 新增 |
| Fade overlay | CameraDirector.FadeInAsync/FadeOutAsync | ✅ 新增 |

---

---

## 相机系统体检与收口 — 2026-03-13 15:39

### 修改文件
- `Assets/Scripts/Level/Camera/CameraDirector.cs`
- `Assets/Scripts/Level/Camera/CameraTrigger.cs`
- `Assets/Scripts/Level/Room/DoorTransitionController.cs`
- `Assets/Scripts/Level/GameFlow/GameFlowManager.cs`
- `Assets/Scripts/UI/WeavingStateTransition.cs`
- `Assets/Scripts/UI/Editor/UICanvasBuilder.cs`
- `Assets/Scenes/SampleScene.unity`

### 内容
- 重构 `CameraDirector`，修复未完成迁移留下的编译/运行风险：去掉不存在的 `ServiceLocator.TryGet` 依赖、修正触发器仲裁为真实按 `priority` 选择、补全 `LookAt`/状态恢复，并增加 `ClearAllTriggers()` 防止跨房间残留镜头状态。
- 重构 `CameraTrigger`，把“锁镜头/聚焦/清弹”改成可与 `CinemachineCamera` 共存的写法，移除错误的 `IPoolable.ReturnToPool()` 调用，统一回收到对象池引用。
- 升级 `DoorTransitionController` 为关卡共享 fade 入口，新增对 `CinemachineCamera` 的 fallback 缩放控制，避免楼层过门时直接操作 `Main Camera` 导致与 `CinemachineBrain` 打架。
- 让 `GameFlowManager` 复用 `DoorTransitionController` 的同一张 `FadeOverlay`，并在重生后清空旧房间遗留的相机触发状态。
- 重构 `WeavingStateTransition`，编织态缩放优先驱动 `CinemachineCamera.Lens.OrthographicSize`，仅在没有虚拟相机时才退回到旧的 `Main Camera` 路径，从根上消除编织态与房间相机/过门缩放的冲突。
- 更新 `UICanvasBuilder` 自动布线逻辑：自动给 `WeavingStateTransition` 和 `DoorTransitionController` 绑定 `CinemachineCamera`，把 `GameFlowManager` 也接到同一张 `FadeOverlay`，并在检测到 `CinemachineCamera` 但没有 `CameraDirector` 时自动补挂 `CameraDirector`。
- 更新 `SampleScene.unity`：把 `WeavingStateTransition._gameplayVirtualCamera`、`DoorTransitionController._fallbackVirtualCamera`、`GameFlowManager._fadeImage` 直接接到当前场景现有资产，确保现成场景立即使用新链路。

### 目的
- 收口多次迭代后分叉的相机控制链路，解决 `WeavingStateTransition`、`DoorTransitionController`、`GameFlowManager` 各自直控相机/遮罩导致的冲突。
- 让当前唯一主场景 `SampleScene` 在不依赖手工重新搭线的前提下，直接切到新的相机/转场组织方式。
- 为后续真正启用 `CameraDirector + CameraTrigger` 做干净的基础，避免再带着旧逻辑继续叠功能。

### 技术
- `CinemachineCamera` Lens 驱动替代 `Main Camera` 直控
- `ServiceLocator` 统一解析 + 共享转场遮罩复用
- PrimeTween + UniTask 无暂停补间/等待
- 场景 YAML 定点修复现有引用，避免只改代码不改资产的“半迁移”状态

---

---

## Unity Shader / Material 学习总结文档 — 2026-03-13 16:15

### 新建文件
- `Docs/Reference/Unity_Shader_Material_Learning_Summary.md`

### 内容
- 新建 Unity Shader / Material 学习总结文档，完整整理本轮调研得到的高质量教程、示例仓库、推荐学习顺序、读 Shader 代码的方法，以及面向 Project Ark 的落地方向。
- 文档中系统总结了 5 个重点学习资源：`Unity-Technologies/Graphics`、`UnityURPUnlitScreenSpaceDecalShader`、`keijiro/NoiseShader`、`UnityFurURP`、`GenshinCelShaderURP`，并补充 `Catlike Coding` 与 Unity 官方 `Shader Graph` 文档作为基础教程来源。
- 增加 AI + MCP 辅助 Shader / Material 开发的推荐工作流，明确“AI 负责设计与编码、MCP 负责 Unity 落地与验证”的协作方式。
- 将学习成果映射到 Project Ark 当前最相关的 3 个效果方向：`BoostTrail`、`HeatPulse / Heat Overload`、`DamageFlash`，并提炼为后续可复用的效果设计模板。

### 目的
- 把零散的 Shader 学习资料沉淀为项目内部可长期复用的参考文档，避免后续重复搜索和重复判断。
- 为 Project Ark 后续的飞船 VFX、热量反馈、命中特效、UI 扫描线等视觉开发建立统一的认知框架和实现路线。
- 提前明确适合本项目的 Shader 技术路线与 AI 协作方式，提升后续迭代速度。

### 技术
- 外部资料调研结果结构化整理
- Unity URP Shader / Material 学习路径设计
- AI + MCP 协作工作流沉淀
- 面向 Project Ark 的效果模板化提炼

---

---

## UnityMCP 相机场景验证与挂线修复 — 2026-03-13 16:26

### 修改文件
- `Assets/Scripts/UI/ProjectArk.UI.asmdef`
- `Assets/Scenes/SampleScene.unity`

### 内容
- 使用 UnityMCP 对 `SampleScene` 做真实验证：先执行资源刷新和编译，读 `Console` 抓到两轮真实问题——第一轮是 `GameFlowManager.ResolveFadeImage` 缺失与 `CameraDirector` 的 PrimeTween `onComplete` 用法错误；第二轮是 `ProjectArk.UI` 程序集缺少 `Unity.Cinemachine` 引用，导致 `WeavingStateTransition` / `UICanvasBuilder` 编译失败。
- 修复 `ProjectArk.UI.asmdef`，补上 `Unity.Cinemachine` 引用后重新编译，Unity `Console` 清空，说明相机相关脚本已恢复可编译状态。
- 继续用 UnityMCP 读取场景运行前真实挂载，发现当前 `SampleScene` 仍处于“代码已升级、场景未迁移完”的半接线状态：`CinemachineCamera` 没有 `CameraDirector`，`FadeOverlay` 没有 `CanvasGroup`，`DoorTransitionController._fallbackVirtualCamera` 为空，`GameFlowManager._fadeImage` 为空。
- 直接通过 UnityMCP 给现有对象补挂并回填引用：在 `CinemachineCamera` 上添加 `CameraDirector`，在 `Canvas/FadeOverlay` 上添加 `CanvasGroup` 并初始化为 `alpha=0 / interactable=false / blocksRaycasts=false`，同时把 `DoorTransitionController._fallbackVirtualCamera` 指向 `CinemachineCamera`，把 `GameFlowManager._fadeImage` 指向 `FadeOverlay` Image，然后保存 `SampleScene`。
- 进入 Play Mode 再次验证：`Main Camera` 上的 `CinemachineBrain` 正常驱动 `CinemachineCamera`，`CameraDirector` 在运行时成功初始化（默认正交尺寸回填为 `2.73`，FollowTarget 为 `Ship`），`WeavingStateTransition` / `DoorTransitionController` / `GameFlowManager` 的关键引用都保持有效，Console 为 0 条输出。
- 验证中同时确认一个结构性事实：当前场景里仍然 **没有任何 `CameraTrigger` 实例**，说明新的触发器式房间镜头系统尚未真正铺到关卡资产，只是核心主控链路已经可用。

### 目的
- 用 Unity Editor 的真实状态而不是静态猜测，验证这轮相机重构是否真的跑通。
- 把 `SampleScene` 从“脚本已改但资产未重挂”的危险状态收口到可运行、可继续迭代的状态。
- 明确区分“已经修通的主链路”和“仍待后续场景制作补齐的 CameraTrigger 资产层工作”。

### 技术
- UnityMCP：`refresh_unity`、`read_console`、`find_gameobjects`、`manage_camera`、`manage_components`、`manage_scene`
- Assembly Definition 引用修复（`Unity.Cinemachine`）
- 场景内组件补挂 + 序列化引用回填
- Play Mode 运行态回读验证
如果你觉得当前效果不对，我建议优先调这 4 个值
想更“卷”一点：增大 Distort Strength

想更“燃烧/活”一点：增大 Flow Speed

想更“科技能量”一点：增大 Edge Color 和 Brightness

想更稳、更克制：降低 Flicker Strength

下一步最值得做的两件事
第一步：我继续帮你做一版 更偏 GG 风格的“细长、收束、脉冲式” BoostTrail 参数调校

第二步：把 BoostTrailView.cs 里的 _BoostIntensity 真正接进这个新 Shader，让扰动强度随 Boost 状态动态变化，而不是只靠材质静态参数

结果：BoostTrail 主拖尾现在已经有一个可直接使用的扰动材质 MVP。

兼容性：保留了旧的 slot 实验材质路径，没有把现有参考流程破坏掉。

后续建议：下一轮最有价值的是把 _BoostIntensity 接进 Shader 动态控制，这样它会从“有扰动”升级成“会呼吸、会随推力变化”的真正推进尾迹。

---

---

## BoostTrail 扰动材质 MVP — 2026-03-13 16:27

### 修改文件
- `Assets/_Art/VFX/BoostTrail/Shaders/TrailMainEffect.shader`
- `Assets/_Art/VFX/BoostTrail/Materials/mat_trail_main.mat`
- `Assets/_Art/VFX/BoostTrail/Materials/mat_trail_main_effect.mat`
- `Assets/Scripts/Ship/Editor/MaterialTextureLinker.cs`

### 内容
- 将 `TrailMainEffect` 重构为双模式 Shader：新增基于程序噪声的 `BoostTrail` 扰动路径，同时保留原有 `slot` 贴图驱动的 legacy 路径。
- 将 `mat_trail_main` 主材质切换到新的扰动模式，增加 `_NoiseScale`、`_DistortStrength`、`_FlowSpeed`、`_FlickerStrength`、`_EdgePower` 等参数，并配置为可直接用于 `MainTrail` 的默认值。
- 为 `mat_trail_main_effect` 增加 `_UseLegacySlots = 1`，确保实验/旧流程材质继续走原先的 `slot` 纹理采样逻辑。
- 更新 `MaterialTextureLinker`，使重新绑定材质纹理时也会同步设置 Shader 和模式位，避免后续运行编辑器工具时把这次接线冲掉。

### 目的
- 为 `BoostTrail` 提供一个立即可见、可调参、适合当前项目迭代节奏的扰动尾迹材质 MVP。
- 在不破坏现有实验材质与旧贴图流程的前提下，让 `MainTrail` 直接获得更强的热流、卷曲和尾焰抖动感。
- 为后续继续细化 `BoostTrail`、`HeatPulse`、受击闪烁等 2D VFX 提供可复用的 URP 扰动 Shader 基础。

### 技术
- URP 自定义 HLSL Shader（Additive Transparent）
- 程序噪声 `ValueNoise + FBM` 扰动
- `TrailRenderer` 兼容的 UV 流动与边缘发光
- 双模式 Shader 分流（扰动路径 + legacy slot 路径）
- 编辑器材质重绑流程保护

---

---

## OpenUPM 安装 UnityMCP 包 — 2026-03-14 10:09

### 修改文件
- `Packages/manifest.json`

### 内容
- 使用 `npx --yes openupm-cli add com.coplaydev.unity-mcp` 通过 OpenUPM 安装 `com.coplaydev.unity-mcp`，避免继续走 `Git URL` 拉取仓库。
- 在 `manifest.json` 中新增 OpenUPM scoped registry：`package.openupm.com`，并写入依赖 `com.coplaydev.unity-mcp: 9.5.3`。
- 保留项目现有的 `npmjs` scoped registry 配置，不影响 `PrimeTween` 的现有安装来源。
- 安装命令成功返回 `notice added com.coplaydev.unity-mcp@9.5.3`；`packages-lock.json` 的最终解析结果需在 Unity 重新打开项目后由 Package Manager 应用刷新。

### 目的
- 绕开本机 `Git` 访问 GitHub 时被失效本地代理拦截的问题，改用更稳定的 OpenUPM registry 安装路径。
- 让 `UnityMCP` 以标准 UPM registry 依赖形式进入项目，降低后续包管理维护成本。

### 技术
- OpenUPM CLI（`openupm-cli`）
- Unity Package Manager scoped registry
- Windows 命令行内联环境变量清理（避免旧代理污染本次安装）

---

---

## BoostTrail Shader 实现进度文档整理 — 2026-03-15 10:25

### 新建文件
- `Docs/Reference/BoostTrail_Shader_Implementation_Status.md`

### 内容
- 将当前 `BoostTrail` Shader 线的实现状态整理为独立 Markdown 文档，归纳为“已完成 / 部分完成 / 未完成 / 下一步优先顺序”四个层次。
- 明确当前不再处于 Shader 学习期，而是已进入项目内首个自研 Shader 样板的 MVP 收口阶段。
- 特别澄清了 `_BoostIntensity` 的真实接线状态：它已经驱动 `BoostTrailView` 中的能量层，但尚未进入 `TrailMainEffect.shader` 的主拖尾逻辑。
- 将下一步工作重点收束到主拖尾动态驱动、GG 风格化调参、模板固化与后续效果复制四个方向。

### 目的
- 把刚才的阶段判断从对话内容沉淀为项目内可复用文档，方便后续继续沿着 `BoostTrail` Shader 路线推进。
- 避免团队后续对“哪些已经做完、哪些还只做了一半”产生认知偏差。
- 为下一轮真正开始改 `TrailMainEffect.shader` 和 `BoostTrailView.cs` 提供统一的状态基线。

### 技术
- Markdown 结构化技术状态文档
- 基于 `ImplementationLog` + 当前代码实现的交叉梳理
- 面向 Project Ark 的 VFX/Shader 迭代清单整理

---

---

## BoostTrail Shader 文档补充版本 B 落地方案 — 2026-03-15 10:28

### 修改文件
- `Docs/Reference/BoostTrail_Shader_Implementation_Status.md`

### 内容
- 在 `BoostTrail_Shader_Implementation_Status.md` 中新增“版本 B：落地任务单（字段 / 方法 / Shader 参数）”章节，把下一步方案从方向性建议细化为文件级改动清单。
- 方案明确将版本 B 的范围收束为 `TrailMainEffect.shader` 与 `BoostTrailView.cs` 两处，目标是让 `MainTrail` 的自研 Shader 正式吃到运行时 `_BoostIntensity`。
- 文档中补充了建议新增的 Shader 属性、`CBUFFER` 字段、`ShadeDisturbance()` 的参数联动方式，以及 `BoostTrailView` 中 `MaterialPropertyBlock` 的扩展思路。
- 同时明确了本轮 MVP 的验收标准、风险点、保底方案和明确不做项，避免下一轮实现时范围失控。

### 目的
- 把下一步即将实施的方案直接沉淀到状态文档里，让文档不仅说明“当前做到哪里”，也能指导“下一步具体怎么做”。
- 为后续真正开始修改 `TrailMainEffect.shader` 和 `BoostTrailView.cs` 提供统一的任务拆解与边界定义。
- 降低团队在版本 B 开工前对实现路径和范围的分歧。

### 技术
- Markdown 文档增量规划
- 基于现有 `BoostTrailView` / `TrailMainEffect` 代码的文件级任务拆解
- Shader 参数联动与 Unity 运行时写入策略设计

---

---

## BoostTrail 主拖尾接入运行时 _BoostIntensity — 2026-03-15 10:42

### 修改文件
- `Assets/_Art/VFX/BoostTrail/Shaders/TrailMainEffect.shader`
- `Assets/Scripts/Ship/VFX/BoostTrailView.cs`

### 内容
- 为 `TrailMainEffect.shader` 新增 `_BoostIntensity` 属性与 `CBUFFER` 字段，并在 `ShadeDisturbance()` 中把它接入主拖尾的扰动强度、流动速度、亮度、Alpha，以及轻度的边缘发光权重。
- 保持 `ShadeLegacy()` 与 `_UseLegacySlots` 分支完全不动，确保旧的 slot 管线不受这轮动态驱动接线影响。
- 为 `BoostTrailView.cs` 新增 `_mpbMainTrail`，在 `Awake()` 中初始化，并在 `SetBoostIntensity()` 中把现有的 Boost 强度动画同步写入 `_mainTrail`。
- 完成验证闭环：`dotnet build Project-Ark.slnx` 结果为 0 error，Unity 刷新与 Console 检查未发现本轮新增错误。

### 目的
- 让 `MainTrail` 从“静态可用材质”升级为“随 Boost 状态呼吸变化的主拖尾 Shader”。
- 在不引入运行时材质实例、不破坏 legacy 路径的前提下，把主拖尾纳入现有 `_BoostIntensity` 运行时控制链。
- 为后续做 GG 风格化参数收口与 `Heat Overload` / `Damage Flash` 迁移提供首个完成动态联动的样板。

### 技术
- URP 自定义 HLSL Shader 参数联动
- `TrailRenderer` / `SpriteRenderer` / `MeshRenderer` 统一 `MaterialPropertyBlock` 驱动
- PrimeTween 运行时强度插值复用
- `dotnet build` + UnityMCP `refresh_unity` / `read_console` 验证闭环

---

---

## SampleScene 飞船 BoostTrail 挂载排障与场景实例修复 — 2026-03-15 11:09

### 修改文件
- `Assets/Scenes/SampleScene.unity`

### 内容
- 对 `SampleScene` 中的 `Ship` 实例做了挂载链排查，确认 `ShipView._boostTrailView` 已正确指向 `BoostTrailRoot`，`_boostTrail` 为空，运行时代码实际走的是新的 `BoostTrailView` 路径，而不是旧 `TrailRenderer` fallback。
- 发现当前场景实例存在历史残留：额外的 `ShipBoost` 组件、空的 `VisualChild` 节点，以及 `ShipVisualJuice` / `DashAfterImageSpawner` 指向旧节点的场景级覆盖。
- 清理了额外 `ShipBoost`，将 `ShipVisualJuice._visualChild` 改回 `ShipVisual`，将 `DashAfterImageSpawner._shipSpriteRenderer` 改回 `Ship_Sprite_Solid`，并删除了空的 `VisualChild` 节点，随后保存 `SampleScene`。
- 进入 Play Mode 后执行 `ProjectArk/Ship/Debug/Trigger Boost Once`，确认 `MainTrail` 的 `TrailRenderer.emitting` 变为 `true`，证明当前 `Ship -> ShipView -> BoostTrailView -> MainTrail` 触发链已打通。

### 目的
- 验证用户当前看到的那艘 `Ship` 是否仍在使用旧拖尾链路，并排除场景实例覆盖导致的错误判断。
- 将 `SampleScene` 中的飞船实例收敛回以最新 `Ship.prefab` 为主导的状态，避免旧 override 干扰 `BoostTrail` 观察与后续调参。
- 把问题从“怀疑 Shader 没生效”收束为“运行时链路已通，剩余问题更偏向移动条件或视觉读感”。

### 技术
- UnityMCP 场景层级 / GameObject 组件读取
- Unity 场景实例级引用修复与节点删除
- Play Mode + 编辑器菜单 `Trigger Boost Once` 运行时验证
- `TrailRenderer.emitting` 状态核验

---

---

## 空格长按 Dash 后持续 Boost，松手结束（MVP）— 2026-03-15 11:45

### 修改文件
- `Assets/Scripts/Ship/Input/InputHandler.cs`
- `Assets/Scripts/Ship/Movement/ShipDash.cs`
- `Assets/Scripts/Ship/Movement/ShipBoost.cs`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容
- 在 `InputHandler.cs` 中为现有 `Dash` action 增加了 held 语义：按下时设置 `IsDashHeld=true`，松开时恢复为 `false`，并在 `OnEnable/OnDisable` 中补齐 `performed/canceled` 的订阅与清理。
- 在 `ShipDash.cs` 中把 Dash 结束后的 `_boost?.ForceActivate()` 改为条件衔接：只有 Dash 结束时空格仍处于按住状态，才进入后续 Boost；若玩家在 Dash 期间提前松手，则不会进入持续 Boost。
- 在 `ShipBoost.cs` 中把 `ForceActivate()` 扩展为可选的 `sustainWhileDashHeld` 模式；Dash 长按链路下改为 `UniTask.WaitUntil(() => !_inputHandler.IsDashHeld)`，即持续 Boost 直到松手；非 Dash 触发源仍保留原有 `BoostDuration` 固定时长兜底。
- 完成验证闭环：`dotnet build Project-Ark.slnx` 结果为 0 error；UnityMCP 刷新编译、Console 读取与运行时组件读取均通过，确认 `Ship` 运行时实例可直接读到 `InputHandler.IsDashHeld` 与 `ShipBoost.IsBoosting` 状态。补充说明：本轮尝试了 macOS 系统级自动长按注入，但输入未稳定送达 Unity 焦点窗口，因此“真实按键长按”仍建议在 Editor 中手感复验一次。

### 目的
- 将空格从“一次 Dash + 固定时长 Boost”改成更贴近目标手感的连续动作：Dash 起手后，只要玩家继续按住，就保持推进；松手立即收尾。
- 用最小改动复用现有 `ShipStateController`、`OnBoostStarted` / `OnBoostEnded`、`BoostTrailView` 和引擎 VFX 链路，避免扩大改动面。
- 保留非 Dash 触发源的固定时长兼容路径，降低对现有调试入口和未来扩展点的破坏风险。

### 技术
- New Input System `performed/canceled` 长按语义复用
- `UniTask.WaitUntil` 驱动的按住期间状态维持
- `ShipDash` → `ShipBoost` 的条件 handoff
- `dotnet build` + UnityMCP `refresh_unity` / `read_console` / 运行时组件读取验证

---

---

## Boost 时飞船贴图变窄排查与修复（MVP）— 2026-03-15 11:58

### 修改文件
- `Assets/Scripts/Ship/VFX/ShipVisualJuice.cs`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容
- 排查发现，问题不在 `ShipBoost` 主逻辑，也不在 `BoostTrailView` / Shader，而是在 `ShipVisualJuice.cs`：该脚本会对 `_visualChild` 执行 `Tween.Scale(...)`，其中加速与 Dash 分支都会生成 `new Vector3(1f - intensity, 1f + intensity, 1f)`，也就是 **X 轴压窄、Y 轴拉长**。
- 通过 UnityMCP 读取运行时 `Ship` 组件，确认 `ShipVisualJuice._visualChild` 当前绑定的是 `Ship/ShipVisual`，而这个节点正是整船视觉根（`path = Ship/ShipVisual`，下面挂了 7 个子节点），因此该缩放会直接作用到整套飞船贴图，而不是只作用到某个局部特效层。
- 代码层最小修复放在 `ShipVisualJuice.cs`：新增 `ShipBoost` 缓存；在 `OnSpeedChanged()` 中若当前处于 `Dash` 或 `Boost`，则停止现有 squash/stretch tween 并强制恢复 `_visualChild.localScale = Vector3.one`；同时把 `OnDashStarted()` 从“执行强烈拉伸”改成“仅停止 tween 并恢复 1 倍缩放”。
- 验证结果：`ShipVisualJuice.cs` 的 lints 为 0，`dotnet build Project-Ark.slnx` 结果为 0 error；UnityMCP 刷新编译后，Console 未出现本轮新增报错。由于当前 macOS → Unity 的系统级按键注入仍不稳定，未做自动化“真实空格长按”视觉截图，但从代码路径与运行时绑定关系上，已切断 Dash→Boost 链路中压窄整船贴图的直接来源。

### 目的
- 修复玩家在进入 Boost 链路时看到的“整艘船忽然变窄”问题，避免视觉 juice 误伤主船体可读性。
- 保留普通移动时的 tilt / squash-stretch 反馈，不去扩大修改到 `ShipBoost`、`ShipDash`、VFX Shader 或场景层级。
- 用最小代码变更只屏蔽 Dash/Boost 期间的整船缩放，优先恢复可读性和手感一致性。

### 技术
- Unity 运行时组件读取（UnityMCP）
- PrimeTween `Tween.Scale` 链路排查
- Dash / Boost 状态门控下的视觉缩放保护
- `dotnet build` + UnityMCP Console 验证

---

---

## Ship 特效按玩家感知分组参考文档整理 — 2026-03-15 12:09

### 新建/修改文件
- `Docs/Reference/Ship_VFX_Player_Perception_Reference.md`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容
- 新增 `Ship_VFX_Player_Perception_Reference.md`，将当前 `SampleScene` 中 `Ship` 的现役视觉特效重新整理为“按玩家感知分组”的参考文档。
- 文档按玩家体验时序拆为：常驻读感、Dash 爆发、Boost 起手、Boost 持续、Boost 收尾，以及“代码支持但当前未启用”的兼容路径。
- 每个分组都统一列出玩家感知、特效名、当前实现、触发方式、玩家看到什么、当前状态，便于后续做特效精简、删改优先级判断与读感复盘。
- 文档同时补充了适用范围说明，明确该表基于当前场景挂载与现有代码触发链，而不是历史遗留脚本全集。

### 目的
- 将对话中的一次性分析沉淀为可长期引用的 `Reference` 文档，避免后续重复盘点 Ship 特效结构。
- 为接下来的特效删改、预算控制、重复层识别和体验调优提供统一基线。
- 让团队后续讨论 Ship 特效时，能优先从玩家感知层而不是脚本层对齐认知。

### 技术
- 代码触发链梳理（`ShipView` / `BoostTrailView` / `ShipEngineVFX` / `DashAfterImageSpawner` / `ShipVisualJuice`）
- 当前场景挂载与启用状态交叉核对
- Markdown 参考文档结构化归档

---

---

## Ship 旧特效链路第一批清理 — 2026-03-15 12:33

### 新建/修改文件
- `Assets/Scripts/Ship/VFX/ShipView.cs`
- `Assets/Scripts/Ship/Data/ShipJuiceSettingsSO.cs`
- `Assets/Scripts/Ship/Editor/ShipPrefabRebuilder.cs`
- `Assets/Scripts/Ship/Editor/ShipBoostTrailSceneBinder.cs`
- `Assets/_Data/Ship/DefaultShipJuiceSettings.asset`
- `Assets/_Prefabs/Ship/Ship.prefab`
- `Docs/Reference/Ship_VFX_Player_Perception_Reference.md`
- `ProjectArk.Ship.csproj`
- `ProjectArk.Ship.Editor.csproj`
- `Docs/ImplementationLog/ImplementationLog.md`

### 删除文件
- `Assets/Scripts/Ship/VFX/ShipBoostTrailVFX.cs`
- `Assets/Scripts/Ship/VFX/ShipBoostTrailVFX.cs.meta`
- `Assets/Scripts/Ship/Editor/ShipBoostTrailPrefabReplacer.cs`
- `Assets/Scripts/Ship/Editor/ShipBoostTrailPrefabReplacer.cs.meta`

### 内容
- 删除了已退役的旧 Boost 视觉链路：`ShipBoostTrailVFX` 运行时脚本，以及只服务旧迁移流程的 `ShipBoostTrailPrefabReplacer` Editor 工具。
- 从 `ShipView.cs` 中移除了旧 `_boostTrail` 字段、旧 TrailRenderer 初始化/重置残留，以及旧 `StartBoostTrail()` / `StopBoostTrail()` fallback，Boost 视觉入口现在只保留 `BoostTrailView` 主路径。
- 从 `ShipJuiceSettingsSO.cs` 和 `DefaultShipJuiceSettings.asset` 中移除了只服务旧粒子拖尾与旧 TrailRenderer 的参数；同时清掉 `Ship.prefab` 中 `_boostTrail` 的空序列化残留。
- 更新了 `ShipPrefabRebuilder.cs`，让其不再创建旧 BoostTrail 粒子 / TrailRenderer / `ShipBoostTrailVFX`，并修正 `ShipBoostTrailSceneBinder.cs` 中指向旧迁移工具的提示文案。
- 同步更新了 `Ship_VFX_Player_Perception_Reference.md`，删除已清理旧路径的描述；最后通过 `dotnet build Project-Ark.slnx` 验证，本轮结果为 **0 error**（仅剩项目内既有 warning）。

### 目的
- 先完成 Ship 特效系统的第一批“去历史包袱”清理，为后续做特效删改优先级、预算收缩和结构收口打基础。
- 确保 Ship 的 Boost 视觉只剩一条正式主链：`ShipView` → `BoostTrailView`，避免未来继续被旧 fallback 或旧迁移工具干扰。
- 把代码、资产序列化、参考文档和本地构建链路一起收干净，减少“代码删了但引用还活着”的隐性维护成本。

### 技术
- 旧运行时链路与 Editor 迁移工具成组删除
- ScriptableObject authored data 与 prefab 序列化字段同步清理
- 现役 BoostTrailView 主路径收口
- `dotnet build` 全项目编译验证

---

---

## Ship 旧特效链路第二批清理 — 2026-03-15 13:37

### 新建/修改文件
- `Assets/Scripts/Ship/VFX/BoostTrailView.cs`
- `Assets/Scripts/Ship/Editor/BoostTrailPrefabCreator.cs`
- `Assets/Scripts/Ship/Editor/MaterialTextureLinker.cs`
- `Assets/Scripts/Ship/Editor/ShipBoostTrailSceneBinder.cs`
- `Assets/_Prefabs/VFX/BoostTrailRoot.prefab`
- `Assets/Scenes/SampleScene.unity`
- `Docs/ImplementationLog/BoostTrailPhase2_TestChecklist.md`
- `Docs/Reference/Ship_VFX_Player_Perception_Reference.md`
- `Docs/Reference/BoostTrail_Shader_Implementation_Status.md`
- `ProjectArk.Ship.Editor.csproj`
- `Docs/ImplementationLog/ImplementationLog.md`

### 删除文件
- `Assets/Scripts/Ship/Editor/CreateBoostTrailMaterials.cs`
- `Assets/Scripts/Ship/Editor/CreateBoostTrailMaterials.cs.meta`
- `Assets/_Art/Ship/Glitch/mat_boost_trail_glow.mat`
- `Assets/_Art/Ship/Glitch/mat_boost_trail_glow.mat.meta`
- `Assets/_Art/Ship/Glitch/mat_boost_ember_trail.mat`
- `Assets/_Art/Ship/Glitch/mat_boost_ember_trail.mat.meta`
- `Assets/_Art/VFX/BoostTrail/Materials/mat_boost_energy_field.mat`
- `Assets/_Art/VFX/BoostTrail/Materials/mat_boost_energy_field.mat.meta`
- `Assets/_Art/VFX/BoostTrail/Shaders/BoostEnergyField.shader`
- `Assets/_Art/VFX/BoostTrail/Shaders/BoostEnergyField.shader.meta`

### 内容
- 从 `BoostTrailView.cs` 中删除了默认关闭的世界空间能量场路径，移除 `_energyField` / `_enableWorldEnergyField` 及其相关运行时写入、启停和复位逻辑，让 Boost 主控只保留当前正式使用的本地层。
- 更新 `BoostTrailPrefabCreator.cs`、`MaterialTextureLinker.cs` 与 `BoostTrailRoot.prefab`，不再创建、接线或序列化 `BoostEnergyField`；同时删除其对应 shader / material 资源文件，彻底收掉这条退役支线。
- 删除了已无现役用途的 `CreateBoostTrailMaterials` Editor 工具，以及两份只服务旧工具链的孤儿材质 `mat_boost_trail_glow` / `mat_boost_ember_trail`。
- 从 `SampleScene.unity` 中清除了残留的 `_flashImage` prefab override，并让 `ShipBoostTrailSceneBinder.cs` 与 `BoostTrailPhase2_TestChecklist.md` 不再保留旧全屏 flash 的接线和验收描述。
- 同步更新 `Ship_VFX_Player_Perception_Reference.md` 与 `BoostTrail_Shader_Implementation_Status.md`，使当前参考文档与实际现役特效结构一致；最终通过全局搜索确认剩余旧词条仅存在于历史日志或 GG 研究资料中，并通过 `dotnet build Project-Ark.slnx` 验证结果为 **0 warning / 0 error**。

### 目的
- 完成 Ship 特效系统第二批“安全清理”，优先移除默认关闭、已退役、或仅剩历史包袱价值的特效层与工具链。
- 进一步降低当前 Boost 视觉结构的认知噪声，让团队后续讨论删改优先级时面对的是更干净的现役主链，而不是夹杂关闭分支和孤儿资源的半历史状态。
- 避免场景 override、prefab 序列化、编辑器工具、材质资源之间继续出现“代码已删但资产残留”的维护陷阱。

### 技术
- 运行时主控 / Editor 工具 / prefab / scene YAML 同步收口
- 退役 shader / material / 工具脚本与 meta 成组删除
- 全局文本残留搜索 + `dotnet build` 构建验证

---

---

## Ship 旧特效链路第三批清理 — 2026-03-15 14:13

### 新建/修改文件
- `Assets/Scripts/Ship/VFX/BoostTrailView.cs`
- `Assets/Scripts/Ship/Editor/BoostTrailPrefabCreator.cs`
- `Assets/Scripts/Ship/Editor/MaterialTextureLinker.cs`
- `Assets/_Prefabs/VFX/BoostTrailRoot.prefab`
- `Docs/ImplementationLog/BoostTrailPhase2_TestChecklist.md`
- `Docs/Reference/Ship_VFX_Player_Perception_Reference.md`
- `Docs/Reference/BoostTrail_Shader_Implementation_Status.md`
- `Tools/CopyGGTextures.ps1`
- `Docs/ImplementationLog/ImplementationLog.md`

### 删除文件
- `Assets/_Art/VFX/BoostTrail/Materials/mat_trail_main_effect.mat`
- `Assets/_Art/VFX/BoostTrail/Materials/mat_trail_main_effect.mat.meta`
- `Assets/_Art/VFX/BoostTrail/Textures/trail_main_spritesheet.png`
- `Assets/_Art/VFX/BoostTrail/Textures/trail_main_spritesheet.png.meta`
- `Assets/_Art/VFX/BoostTrail/Textures/trail_second_spritesheet.png`
- `Assets/_Art/VFX/BoostTrail/Textures/trail_second_spritesheet.png.meta`
- `Assets/_Art/VFX/BoostTrail/Textures/trail_edge_glow.png`
- `Assets/_Art/VFX/BoostTrail/Textures/trail_edge_glow.png.meta`
- `Assets/_Art/VFX/BoostTrail/Textures/trail_color_lut.png`
- `Assets/_Art/VFX/BoostTrail/Textures/trail_color_lut.png.meta`

### 内容
- 从 `BoostTrailView.cs` 中删除了 `EmberGlow` 的序列化字段、Boost 启停逻辑和对象池复位逻辑，让现役 Boost 主控只保留真正承担读感的 `EmberTrail`、`EmberSparks`、Halo、Bloom 与主拖尾链。
- 更新 `BoostTrailPrefabCreator.cs`，不再创建 `EmberGlow` 节点，也不再把 `MainTrail` fallback 到 `mat_trail_main_effect`；随后通过 Unity Editor 菜单重建了 `BoostTrailRoot.prefab`，确认 prefab 结构里已经没有 `EmberGlow` / `_emberGlow` 残留。
- 重写 `MaterialTextureLinker.cs`，彻底删除 `mat_trail_main_effect` 的 legacy 贴图回填逻辑；同步删除 `mat_trail_main_effect` 及其四张 slot 贴图资源，让主拖尾材质入口固定为 `mat_trail_main`。
- 更新 `BoostTrailPhase2_TestChecklist.md`、`Ship_VFX_Player_Perception_Reference.md` 与 `BoostTrail_Shader_Implementation_Status.md`，把现役验证口径、玩家感知分层和 Shader 状态统一到第三批清理后的真实结构。
- 更新 `Tools/CopyGGTextures.ps1`，删除会重新导入已退役 trail slot 贴图与世界空间能量场贴图的旧区段，避免历史资源链被再次种回项目；最终通过全局搜索确认在代码 / prefab / 工具脚本 /材质 / shader / scene / csproj 中已无 `EmberGlow`、`_emberGlow`、`mat_trail_main_effect` 与其 slot 贴图活引用，并通过 `dotnet build Project-Ark.slnx` 验证结果为 **17 warnings / 0 errors**（warning 为项目既有项）。

### 目的
- 完成 Ship 特效系统第三批“收益低层级 + legacy 主拖尾材质链”清理，继续缩减当前 Boost 视觉结构中的冗余层与历史兼容包袱。
- 让 `MainTrail` 的材质入口和 prefab 生成入口都回到单一现役主链，避免未来再被旧 slot 贴图路线或 `EmberGlow` 这类边际收益低的层误导。
- 把代码、Prefab、资产文件、导入工具与文档同时收口，进一步降低“删了一半、旧链还能被工具重新种回”的维护风险。

### 技术
- 运行时主控 / prefab 生成器 / 材质接线工具同步收口
- 退役材质与 slot 贴图资源成组删除
- Unity Editor 菜单重建 prefab + 全局文本残留搜索 + `dotnet build` 构建验证

---

---

## Ship VFX 规范层与工具权威收口 — 2026-03-15 15:14

### 新建/修改文件
- `Docs/Reference/ShipVFX_CanonicalSpec.md`
- `Docs/Reference/ShipVFX_AssetRegistry.md`
- `Docs/Reference/ShipVFX_MigrationPlan.md`
- `Docs/Reference/Ship_VFX_Player_Perception_Reference.md`
- `Docs/Reference/BoostTrail_Shader_Implementation_Status.md`
- `Assets/Scripts/Ship/Editor/ShipPrefabRebuilder.cs`
- `Assets/Scripts/Ship/Editor/BoostTrailPrefabCreator.cs`
- `Assets/Scripts/Ship/Editor/MaterialTextureLinker.cs`
- `Assets/Scripts/Ship/Editor/ShipBoostTrailSceneBinder.cs`
- `Assets/Scripts/Ship/Editor/ShipBuilder.cs`
- `Assets/Scripts/Ship/VFX/ShipEngineVFX.cs`
- `Assets/Scripts/Ship/Data/ShipShipState.cs`
- `Tools/CopyGGTextures.ps1`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容
- 新建 `ShipVFX_CanonicalSpec.md`、`ShipVFX_AssetRegistry.md` 与 `ShipVFX_MigrationPlan.md` 三份规范层文档，首次把 `Ship` / `BoostTrail` / `VFX` 的现役链路、canonical alias、owner 边界、冻结项和分批迁移顺序收成正式真相源。
- 重写 `Ship_VFX_Player_Perception_Reference.md` 与 `BoostTrail_Shader_Implementation_Status.md`，让它们分别只承担“玩家感知视角”和“主拖尾实现状态”职责，不再兼任命名权威或资产总表。
- 更新 `ShipPrefabRebuilder.cs`，让它正式接管 `Ship.prefab` 内的 `BoostTrailRoot` 嵌套集成，并补齐 `ShipView._boostTrailView`、`_normalLiquidSprite`、`_boostLiquidSprite` 的接线；同时把关键 sprite 查找切到“精确路径优先 + 名称回退”。
- 更新 `BoostTrailPrefabCreator.cs`、`MaterialTextureLinker.cs`、`ShipBoostTrailSceneBinder.cs` 与 `ShipBuilder.cs`，统一 `ProjectArk/Ship/VFX/...` 菜单域、工具职责说明和 canonical / legacy alias 口径；其中 `MaterialTextureLinker.cs` 进一步收紧为只走 `Assets/_Art/VFX/BoostTrail/Textures/` 精确路径，避免全项目模糊搜索误绑。
- 更新 `ShipEngineVFX.cs` 与 `ShipShipState.cs` 的注释口径，去掉已退役的 `BoostTrailVFX` 旧称，并为 `ShipShipState` 补充“物理名冻结、规范名为 Ship State Enum”的说明；同时在 `CopyGGTextures.ps1` 顶部明确它只是参考资源导入脚本，不再承担现役规范职责。
- 通过 `dotnet build Project-Ark.slnx` 验证本轮结果为 **0 warning / 0 error**；同时用 UnityMCP 验证 `SampleScene` 中 `Ship` 与 `BoostTrailBloomVolume` 仍存在，且当前场景里的 `ShipView` 已正确持有 `_boostTrailView`、`_boostLiquidSprite` 与 `_normalLiquidSprite`。Unity Console 在清空后仅剩两条既有材质提示：`mat_boost_energy_layer2` / `mat_boost_energy_layer3` 的 2D SRP Batcher 不兼容信息，本轮未额外放大。

### 目的
- 把 Ship 特效系统从“代码、Prefab、文档、导入脚本各自维护一部分真相”收口到统一规范层，降低后续命名治理和路径迁移时的认知成本。
- 先解决 owner 和映射问题，再推进高风险 rename / move，确保当前 Dash、Boost、拖尾和 Bloom 的现役读感不被一次性资产治理破坏。
- 为后续继续清理 `FlameTrail_B`、`Ship_Sprite_HL`、`ShipShipState` 这类历史物理名，以及 dormant 资源清退，提供可追踪、可验证的迁移基础。

### 技术
- Canonical spec / asset registry / migration plan 三层文档建模
- Editor 工具职责收口与 deterministic asset lookup（精确路径优先）
- `dotnet build` 构建验证 + UnityMCP 场景对象 / Console 验证

---

---

## Ship VFX 首批 dormant 资源清理 — 2026-03-15 17:03

### 新建/修改文件
- `Docs/Reference/ShipVFX_AssetRegistry.md`
- `Docs/Reference/ShipVFX_CanonicalSpec.md`
- `Docs/Reference/ShipVFX_MigrationPlan.md`
- `Tools/CopyGGTextures.ps1`
- `Assets/_Art/VFX/BoostTrail/Materials/mat_ui_boost_flash.mat`
- `Assets/_Art/VFX/BoostTrail/Materials/mat_ui_boost_flash.mat.meta`
- `Assets/_Art/VFX/BoostTrail/Shaders/UIBoostFlash.shader`
- `Assets/_Art/VFX/BoostTrail/Shaders/UIBoostFlash.shader.meta`
- `Assets/_Art/VFX/BoostTrail/Textures/boost_activation_halo.png`
- `Assets/_Art/VFX/BoostTrail/Textures/boost_activation_halo.png.meta`
- `Assets/_Art/VFX/BoostTrail/Textures/boost_field_main.png`
- `Assets/_Art/VFX/BoostTrail/Textures/boost_field_main.png.meta`
- `Assets/_Art/VFX/BoostTrail/Textures/boost_field_aux.png`
- `Assets/_Art/VFX/BoostTrail/Textures/boost_field_aux.png.meta`
- `Assets/_Art/Ship/Glitch/ship_solid_gg.png`
- `Assets/_Art/Ship/Glitch/ship_solid_gg.png.meta`
- `Assets/_Art/Ship/Glitch/ship_liquid_boost.png`
- `Assets/_Art/Ship/Glitch/ship_liquid_boost.png.meta`
- `Assets/_Art/Ship/Glitch/ship_highlight_gg.png`
- `Assets/_Art/Ship/Glitch/ship_highlight_gg.png.meta`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容
- 基于 GUID + 文本双重引用审计结果，清退 8 个已确认无 `scene / prefab / runtime / live material chain` 真实引用的 dormant 资源：`mat_ui_boost_flash`、`UIBoostFlash`、`boost_activation_halo`、`boost_field_main`、`boost_field_aux`、`ship_solid_gg`、`ship_liquid_boost`、`ship_highlight_gg`，并同步删除对应 `.meta`。
- 更新 `ShipVFX_AssetRegistry.md`，移除上述已清退资源的当前路径记录，同时把治理重点改成“首批 low-risk dormant 资源已完成引用审计并进入清退”。
- 更新 `ShipVFX_MigrationPlan.md`，把 Batch 4 收口为“低风险 dormant 清理与物理迁移评估”，并把冻结约束改成“未完成引用审计前不得直接删除 dormant / legacy 文件”。
- 更新 `ShipVFX_CanonicalSpec.md` 与 `CopyGGTextures.ps1`，明确参考导入脚本只复制当前仍保留的 GG 纹理，不再把已清退的 dormant 贴图重新导回仓库；同时把脚本末尾的材质回填菜单提示统一到 `ProjectArk > Ship > VFX > Link Material Textures`。
- 使用 UnityMCP 在清空 Console 后执行 refresh，并再次进入 Play Mode 复验；结果未出现新的 missing asset / missing shader 报错，现役报文仍只包含既有的 `mat_boost_energy_layer2` / `mat_boost_energy_layer3` 2D SRP Batcher 提示，以及与当前场景配置相关的 `AmbienceController`、`EnemySpawner`、`ServiceLocator` 警告。

### 目的
- 先从仓库中移除已证明无人使用的低风险资源，减少后续命名治理和目录迁移时的噪音面。
- 避免 dormant 资源继续通过导入脚本回流，确保 registry、migration plan 和实际仓库内容一致。
- 在不触碰 `Ship.prefab`、`BoostTrailRoot.prefab`、`SampleScene` 与冻结物理名的前提下，为后续更高风险的物理 rename / move 建立可重复的清理范式。

### 技术
- GUID 引用审计 + 文本引用审计双重判定
- 资源文件与 `.meta` 成对清理
- UnityMCP refresh / Play Mode / Console 闭环验证

---

---

## Ship VFX 冻结高歧义物理名审计收口 — 2026-03-15 21:30

### 新建/修改文件
- `Docs/Reference/ShipVFX_AssetRegistry.md`
- `Docs/Reference/ShipVFX_CanonicalSpec.md`
- `Docs/Reference/ShipVFX_MigrationPlan.md`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容
- 对 `FlameTrail_B`、`Ship_Sprite_HL`、`ShipShipState` 三个冻结高歧义物理名完成引用链与改名前风险审计，并把结论正式写回规范层文档。
- 更新 `ShipVFX_AssetRegistry.md`，新增 `ShipHighlightSpriteNode` 与 `LeftFlameTrailNode` 两条 prefab 节点记录，补齐 owner、序列化依赖和“若要改名需同步哪些 Editor 工具”的说明。
- 更新 `ShipVFX_CanonicalSpec.md`，新增“已审冻结高歧义项”小节，把三者的当前结论明确为：`FlameTrail_B`、`Ship_Sprite_HL` 为“需 Editor 配合”，`ShipShipState.cs` 为“不可改（当前批）”；同时注明 `Ship_Sprite_HL` 当前 live prefab 使用共享默认材质链，不应和 `ShipGlowMaterial.mat` / `Movement_21.png` 迁移绑成同一批动作。
- 更新 `ShipVFX_MigrationPlan.md`，新增 Batch 5“冻结高歧义物理名改名前审计”，并补充 `Editor 重建回写`、`共享材质误判` 两类风险及应对策略，使后续物理 rename 具备明确顺序和检查项。

### 目的
- 把“冻结”从模糊约束收口成可执行的迁移边界，避免后续每次讨论 `FlameTrail_B`、`Ship_Sprite_HL` 都重复做引用考古。
- 在不直接触碰 prefab / scene 序列化对象的前提下，为下一批真正的节点 rename 准备最小安全动作清单和风险顺序。
- 降低把 `Ship_Sprite_HL` 误判为专属 glow 材质链的概率，避免一次性联动过多资产导致验证成本放大。

### 技术
- GUID / 文本 / prefab 节点名 / Editor 硬编码四层交叉审计
- Canonical spec + asset registry + migration plan 三层规范收口
- 风险分级（需 Editor 配合 / 不可改）与最小安全动作建模

---

---

## Ship VFX 玩家感知文档现役版收口 — 2026-03-15 21:47

### 新建/修改文件
- `Docs/Reference/Ship_VFX_Player_Perception_Reference.md`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容
- 依据当前现役实现重新整理 `Ship_VFX_Player_Perception_Reference.md`，重新核对 `ShipView`、`BoostTrailView`、`ShipEngineVFX`、`ShipVisualJuice`、`DashAfterImageSpawner` 的运行时触发链，只保留玩家在 `SampleScene` 中真实可见的 Ship 视觉效果。
- 对原文做必要删减：移除 dormant / legacy / 参考资源、物理名 / owner / 迁移批次、Editor 接线细节，以及“修复了不该出现的问题”这类偏工程说明的表述。
- 将表格收口为“玩家感知 / 现役效果 / 玩家实际看到”三列，并把 Boost 段落改成更贴近当前现役栈的体验分层：`Halo + Bloom`、`FlameCore + EmberSparks`、`MainTrail`、双侧 `FlameTrail`、`EmberTrail`、能量层维持、尾喷 pulse 与引擎强档。
- 同步更新结尾的一句话总结与文档关系说明，使这份文档明确退回到“玩家感知参考”职责，不再混入规范层或迁移层信息。

### 目的
- 让这份文档重新成为策划 / 设计 / 体验讨论可直接使用的现役读感底稿，而不是夹杂工程细节的半规范文档。
- 降低后续做特效预算、删改优先级、体验对照时的阅读负担，避免把实现细节误当成玩家读感本身。
- 保持 `CanonicalSpec` / `AssetRegistry` / `MigrationPlan` 负责规范层，这份文档只负责“玩家看到了什么”。

### 技术
- 现役 runtime 触发链交叉核对
- 感知文档降噪与职责收口
- 玩家视角表格重构

---

---

## Ship VFX 体验重构计划建表 — 2026-03-15 22:35

### 新建/修改文件
- `Docs/Reference/ShipVFX_MigrationPlan.md`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容
- 将 `ShipVFX_MigrationPlan.md` 从单纯的规范化迁移计划扩展为“规范化治理 + 体验重构”双阶段文档，明确当前已从前五个规范化批次进入逐项特效重构阶段。
- 在计划中新增 `Batch 6 — Ship VFX 体验重构 Backlog（当前执行批）`，把 `MainTrail` 明确标记为“满意 / 当前冻结”的基线，并将除 `MainTrail` 外的现役 `Ship VFX` 全部整理成 backlog。
- backlog 按玩家体验阶段拆分为：常驻、Dash、Boost 起手、Boost 持续、Boost 收尾，并为每个条目标注当前驱动 owner、初始状态、建议处理和优先级，方便后续逐项过稿与逐项实现。
- 同时补充“逐项评审规则”和“当前 list 的使用方式”，把后续讨论统一收口到 `目标 / 问题 / 决策 / 范围 / 验收` 五个问题上，避免每次重新盘点现役特效范围。

### 目的
- 把“除了 `MainTrail` 之外都要重构”从口头共识沉淀成一个可执行的 plan，并给后续逐项推进提供稳定入口。
- 保持 `Ship_VFX_Player_Perception_Reference.md` 继续只负责玩家感知基线，不把执行计划重新塞回参考文档里。
- 降低后续讨论成本，使我们每次都能直接从 backlog 选一个条目推进，而不是重复做范围确认。

### 技术
- 现役 VFX 栈清单化
- 体验阶段分组与优先级建模
- 计划文档职责扩展与执行模板收口

---

---

## Ship VFX 首项重构卡片：Activation Halo — 2026-03-15 22:39

### 新建/修改文件
- `Docs/Reference/ShipVFX_MigrationPlan.md`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容
- 在 `ShipVFX_MigrationPlan.md` 的 `Batch 6` 下新增第一张具体重构卡片，选择 `Activation Halo` 作为首个推进项，并将其状态从通用 backlog 提升为“已定方向，待实现”。
- 为 `Activation Halo` 补充完整的计划字段：当前结论、玩家目标读感、当前问题假设、改动范围、MVP 方向、验收标准和未来增强边界。
- 明确 `Activation Halo` 的职责是 **Boost 起手的局部主确认层**，并规定它必须先于 `Bloom Burst` 定调，避免后续把起手确认错误地依赖到屏幕整体亮度变化上。
- 将第一项的体验目标收口为“近身、短促、压缩感强的点火确认”，使后续实现讨论有统一语义基线，而不是继续停留在抽象的‘重做’层面。

### 目的
- 把 backlog 中的第一项从列表升级为可直接开工的重构卡片，减少后续反复补定义的成本。
- 先钉死 Boost 起手的主确认层职责，再去讨论 `Bloom Burst`、`FlameCore Burst` 等辅层，避免多层职责再次重叠。
- 为后续一项一项推进建立固定模板，后面其他特效可以按同样结构继续扩卡。

### 技术
- 单项重构卡片化
- 玩家读感目标前置
- MVP / 验收标准 / 未来增强三段式拆分

---

---

## Ship VFX 首项实现：Activation Halo MVP — 2026-03-15 22:51

### 新建/修改文件
- `Assets/Scripts/Ship/VFX/BoostTrailView.cs`
- `Assets/Scripts/Ship/Editor/BoostTrailPrefabCreator.cs`
- `Assets/Scripts/Ship/Editor/ShipBoostDebugMenu.cs`
- `Assets/_Prefabs/VFX/BoostTrailRoot.prefab`
- `Assets/_Prefabs/Ship/Ship.prefab`
- `Docs/Reference/ShipVFX_MigrationPlan.md`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容
- 为 `Activation Halo` 落地首轮 MVP：在 `BoostTrailView.cs` 中把 Halo 动画从单段 alpha/scale 扩张改为“快速爆亮 → 立刻回收”的两段式节奏，并新增 `_activationHaloPeakScale` 参数，收紧总时长、起始尺度和收尾尺度。
- 在 `BoostTrailPrefabCreator.cs` 中重设 `BoostActivationHalo` 的默认基础形态：将 local scale 收为更贴船体的椭圆比例，略微下移位置，并提升颜色到更偏热启动的亮度组合，弱化 UI 圆环感。
- 同步更新 `ShipBoostDebugMenu.cs` 的 `Show/Hide Activation Halo` 调试预览参数，确保调试菜单看到的 Halo 与现役 prefab 基础形态一致。
- 通过 Unity Editor 重建 `BoostTrailRoot.prefab` 与 `Ship.prefab`，确认新的 Halo 序列化参数已写入现役 prefab；并补充更新 `ShipVFX_MigrationPlan.md`，将 `Activation Halo` 的状态推进到“实现中（已完成首轮 MVP 落地）”。
- 进行了两层验证：`dotnet build Project-Ark.slnx` 编译通过；Unity Play Mode 中执行 `Show Activation Halo` 和一次 `Trigger Boost Once`，未出现本轮新增运行时错误。

### 目的
- 把 `Activation Halo` 从“计划中的首项重构”推进到真正可运行的首轮实现，建立 Boost 起手主确认层的第一版新基线。
- 用最小改动先验证“更贴身、更短促、更有点火确认感”的方向，而不是一次性联动修改 `Bloom Burst` 或其他 Boost 起手层。
- 为你接下来亲手进 Play Mode 感受读感差异提供一个明确的可试玩版本，再决定是否进入第二轮精修。

### 技术
- Halo 时序曲线重构
- Prefab 默认形态与颜色重设
- 编译检查 + Unity prefab 重建 + Play Mode 运行时验证

---

---

## Ship VFX 计划补充：后续重构默认参考 MainTrail 技术方案 — 2026-03-15 23:08

### 新建/修改文件
- `Docs/Reference/ShipVFX_MigrationPlan.md`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容
- 在 `ShipVFX_MigrationPlan.md` 的总原则中补充：`MainTrail` 不仅是当前满意的视觉冻结基线，同时也是后续 `Ship VFX` 重构的**默认技术参考基线**。
- 在 `Batch 6` 下新增 `6.1.1 MainTrail 技术参考原则`，明确后续特效重构应尽量参考 `MainTrail` 已验证的实现结构：单一主载体、材质 / Shader 驱动、少量关键运行时参数驱动、单一现役主链，以及 Editor 装配与 Runtime 播放分离。
- 将 `6.2 逐项评审规则` 从 5 个问题扩展为 6 个问题，新增“**技术参考**”检查项，要求后续每个条目都显式判断是否沿用 `MainTrail` 方案；若不沿用，必须写清原因。

### 目的
- 把“之后的重构尽量参考 `MainTrail` 技术实现方案”从口头偏好沉淀为正式计划约束，避免后续每一项又重新另起一套实现思路。
- 让 `Batch 6` 的逐项推进同时对齐**体验基线**和**技术基线**，提高风格一致性与工程维护一致性。
- 降低未来特效重构的分叉风险，优先复用已经在 Play Mode 中验证成立的成熟实现范式。

### 技术
- 技术基线前置
- MainTrail 实现范式抽象化
- 逐项评审模板增强

---

---

## Ship VFX 计划推进：补充 `Bloom Burst` 与 `FlameCore Burst` 重构卡 — 2026-03-15 23:26

### 新建/修改文件
- `Docs/Reference/ShipVFX_MigrationPlan.md`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容
- 在 `ShipVFX_MigrationPlan.md` 的 `Batch 6` backlog 总表中，将 `Bloom Burst` 与 `FlameCore Burst` 的状态从“待重构”更新为“已定方向”，使总表与逐项卡片状态一致。
- 新增 `6.6 当前推进项：Bloom Burst`，明确它在后续方案中的定位是 **Boost 起手的全局辅层放大器**，只能放大 `Activation Halo` / `FlameCore Burst` 的本地点火读感，不能单独承担主确认职责。
- 新增 `6.7 当前推进项：FlameCore Burst`，明确它应被重做为 **推进器根部的 start-only 点火核心层**，并与 `EmberSparks Burst`、`FlameTrail_R / FlameTrail_B` 做清晰职责拆分。
- 两张卡均补齐了目标、当前问题、是否沿用 `MainTrail` 技术方案、范围、MVP 方向、验收标准与未来增强，形成可继续实现的正式迁移条目。

### 目的
- 按 `方案 A` 继续推进 `Boost 起手` 簇的 migration plan，把核心条目从 backlog 名称升级为可执行的设计卡。
- 先把 `Halo`、`Bloom`、`FlameCore` 三层的主次关系钉死，避免后续实现阶段再次出现“谁负责主确认、谁只是放大器”的职责漂移。
- 让后续 `Ship VFX` 重构继续围绕 `MainTrail` 的成熟技术范式展开，同时明确哪些层只应部分借鉴、不能照搬。

### 技术
- 运行时触发链与 scene-only 后处理边界审计
- 粒子 burst 职责拆分与 start-only 语义收口
- `MainTrail` 技术基线下的重构卡扩展

---

---

## Ship VFX 实现：落地 `Activation Halo` / `Bloom Burst` / `FlameCore Burst` 起手重构 — 2026-03-15 23:53

### 新建/修改文件
- `Assets/Scripts/Ship/VFX/BoostTrailView.cs`
- `Assets/Scripts/Ship/Editor/BoostTrailPrefabCreator.cs`
- `Assets/Settings/BoostBloomVolumeProfile.asset`
- `Assets/_Prefabs/VFX/BoostTrailRoot.prefab`
- `Docs/Reference/ShipVFX_MigrationPlan.md`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容
- 重构 `BoostTrailView` 的 Boost 起手流程：改为先触发 `Activation Halo`、`FlameCore`、`EmberSparks` 等 startup 层，再以短延迟启动 `FlameTrail_R / FlameTrail_B` 与 `EmberTrail`，避免持续层抢走起手确认读感。
- 将 `Bloom Burst` 从对称亮度脉冲改为 **快攻慢收** 的本地后处理放大器：增加 peak weight / attack / release 参数，并同时驱动 `Volume.weight` 与 `Bloom.intensity`，使其明确退居辅层。
- 将 `FlameCore` 从“短寿命循环粒子”收口为 **start-only 点火层**：运行时改用一次性 burst 触发；默认 prefab / particle 参数改为非循环、短持续、热核起始颜色与更贴近喷口根部的表现。
- 收紧 `BoostActivationHalo` 的默认位置、尺寸、颜色和动画参数，使其更贴身、更短促、更像船体局部被点燃，而不是大范围 UI 波纹。
- 同步下调 `BoostBloomVolumeProfile.asset` 的 Bloom 基线强度，并把 `BoostTrailRoot.prefab` 的现役序列化参数直接对齐到新实现，避免 Unity MCP 菜单暂时失去响应时，现役链仍停留在旧值。
- 将 `ShipVFX_MigrationPlan.md` 中 `Activation Halo`、`Bloom Burst`、`FlameCore Burst` 的状态更新为 **实现中，待实机验收**，让计划状态与实际落地进度一致。

### 目的
- 正式启动 `migration plan` 的实现阶段，把 `Boost 起手` 小簇从“已定方向”推进到实际代码与 prefab 级落地。
- 确保起手三层的职责边界真正成立：`Halo` 负责局部主确认，`Bloom` 负责全局放大，`FlameCore` 负责喷口根部点火，持续层则稍后接管。
- 以 `MainTrail` 为技术基线，先交付一个**主次清晰、可继续迭代、能直接进入实机调感**的 MVP 版本。

### 技术
- UniTask 短延迟启动序列 + CancellationToken 生命周期管理
- `Volume.weight` / `Bloom.intensity` 协同驱动
- ParticleSystem burst 化改造与现役 prefab YAML 直接对齐
- `dotnet build` 编译闭环验证

---

---

## Ship VFX 实现：推进 `EmberSparks Burst` 与 `Liquid Boost State` 起手重构 — 2026-03-16 00:22

### 新建/修改文件
- `Assets/Scripts/Ship/VFX/BoostTrailView.cs`
- `Assets/Scripts/Ship/VFX/ShipView.cs`
- `Assets/Scripts/Ship/Data/ShipJuiceSettingsSO.cs`
- `Assets/Scripts/Ship/Editor/BoostTrailPrefabCreator.cs`
- `Assets/_Data/Ship/DefaultShipJuiceSettings.asset`
- `Assets/_Prefabs/VFX/BoostTrailRoot.prefab`
- `Docs/Reference/ShipVFX_MigrationPlan.md`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容
- 在 `BoostTrailView` 中为 `EmberSparks` 增加 `_emberSparksBurstDelay`，把它从与 `FlameCore` 同帧触发改为**略延后进入**，并把启动链改成统一的时间点序列：先 `FlameCore`，再 `EmberSparks`，随后才进入 `FlameTrail_R / FlameTrail_B` 与 `EmberTrail`。
- 在 `BoostTrailPrefabCreator.ConfigureEmberSparks()` 中把火星形态从**径向爆开**收口成**沿喷口方向甩出的短促边缘火星**：降低 burst 数量、缩短生命周期、改用 `Box` shape 与定向 `velocityOverLifetime`，让它明确退居陪衬层。
- 在 `ShipView` 中把液态层的 Boost 进入逻辑从单段提亮改成**两段式高能切换**：先冲到 ignition peak，再回落到 sustain brightness，同时保持 `Boost_16` 的 sprite 切换作为状态确认。
- 在 `ShipJuiceSettingsSO` 与 `DefaultShipJuiceSettings.asset` 中新增液态层的起手峰值与 settle 参数，并将默认值调整为更短促的起手冲高节奏。
- 更新 `ShipVFX_MigrationPlan.md`：将 `Bloom Burst` / `FlameCore Burst` / `EmberSparks Burst` / `Liquid Boost State` 在 backlog 中统一推进到“实现中”，并补齐 `6.8 EmberSparks Burst` 与 `6.9 Liquid Boost State` 两张正式重构卡。
- 通过 Unity 菜单重新执行 `Create BoostTrailRoot Prefab`，确认现役 `BoostTrailRoot.prefab` 已吃到新的 `EmberSparks` 序列化参数；随后使用 `dotnet build Project-Ark.slnx` 验证，本轮改动未引入新的编译错误。

### 目的
- 继续推进 `migration plan` 的 `Boost 起手` 小簇，让“核心点火 → 边缘火星 → 持续喷发”的主次节奏真正成立。
- 让船体 `Liquid Boost State` 不再只是硬切参数，而是能明确读成“飞船进入高能态”的本体状态变化。
- 在不回改 `MainTrail` 的前提下，把更多现役起手层推进到可实机调感的状态。

### 技术
- UniTask 分段时序编排
- PrimeTween 单 Tween 分段亮度曲线驱动
- ParticleSystem 方向性 burst 参数收口
- ScriptableObject 调参扩展 + 现役 prefab 重建验证

---

---

## Ship VFX 实现：修复 `Particle Velocity curves must all be in the same mode` — 2026-03-16 00:37

### 新建/修改文件
- `Assets/Scripts/Ship/Editor/BoostTrailPrefabCreator.cs`
- `Assets/_Prefabs/VFX/BoostTrailRoot.prefab`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容
- 在 `BoostTrailPrefabCreator` 中修正 `FlameCore` 与 `EmberSparks` 的 `velocityOverLifetime` 配置：不再只设置 `x/y` 两个分量，而是把 `x/y/z`、`orbitalX/Y/Z`、`radial`、`speedModifier` 全部统一写成 `TwoConstants` 模式，消除 Unity 对粒子速度曲线模式不一致的报错。
- 调整了纵向速度区间的写法顺序，使负向喷射范围保持一致且更易读。
- 重新执行 `ProjectArk/Ship/VFX/Create BoostTrailRoot Prefab`，让修复后的粒子速度模块写回现役 `BoostTrailRoot.prefab`。
- 清空并回查 `Unity Console`，确认 `Particle Velocity curves must all be in the same mode` 本轮已不再出现；同时用 `dotnet build Project-Ark.slnx` 验证本轮修复未引入新的编译错误。

### 目的
- 修复本轮 `BoostTrail` 粒子重建时阻断工作流的 Unity 编辑器报错。
- 保证 `EmberSparks` / `FlameCore` 的方向性速度配置既能表达设计意图，也符合 Unity 粒子模块的序列化约束。
- 让后续继续调 `Boost 起手` 视觉时，不再被同一条 prefab 构建错误反复打断。

### 技术
- Unity `ParticleSystem.VelocityOverLifetimeModule` 全通道同模式配置
- Unity 菜单重建 prefab + Console 清空后回归验证
- `dotnet build` 编译闭环验证

---

---

## Ship VFX 实现：推进 `Boost 持续 P1` 小簇（Flame / Ember / EnergyLayer）— 2026-03-16 00:57

### 新建/修改文件
- `Assets/Scripts/Ship/VFX/BoostTrailView.cs`
- `Assets/Scripts/Ship/Editor/BoostTrailPrefabCreator.cs`
- `Assets/_Prefabs/VFX/BoostTrailRoot.prefab`
- `Docs/Reference/ShipVFX_MigrationPlan.md`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容
- 在 `BoostTrailView` 中新增持续层分层接管参数，把 `FlameTrail`、`EmberTrail`、`BoostEnergyLayer2 / Layer3` 从原先更接近“同步开满”的状态，收口成由同一条 master intensity 驱动的**阈值进入 + 强度上限**链路。
- 调整 `OnBoostEnd()`：起手 burst 层立即停，持续层则顺着共享强度链自然退下，避免 `FlameTrail` / `EmberTrail` 在退出 Boost 时出现硬切断电感。
- 在 `BoostTrailPrefabCreator` 中重做 `FlameTrail_R / FlameTrail_B` 的默认形态：从更像世界空间尾迹的粒子层改为**贴喷口的本地持续火焰**；同时把 `EmberTrail` 收口成更轻、更热的 residual embers，减少烟团感。
- 更新 `ShipVFX_MigrationPlan.md`：将 `FlameTrail_R / FlameTrail_B`、`EmberTrail`、`BoostEnergyLayer2 / Layer3` 推进到“实现中”，并补齐 `6.10`、`6.11` 两张正式重构卡，明确目标、范围、MVP 与验收标准。
- 在 Unity 验证过程中发现 `BoostTrailView.ResetState()` 于 `OnDisable / domain reload` 路径下存在 `MaterialPropertyBlock` 读取异常；随后将 `_BoostIntensity` 更新改为**直接写入 PropertyBlock**，移除 `GetPropertyBlock()` 依赖，并通过再次域重载验证该异常已消失。
- 重新执行 `ProjectArk/Ship/VFX/Create BoostTrailRoot Prefab`，确认现役 `BoostTrailRoot.prefab` 已写入新的持续层阈值参数；同时使用 `dotnet build Project-Ark.slnx` 验证，本轮改动未引入新的编译错误。

### 目的
- 让 Boost 的持续阶段建立明确层次：`MainTrail` 继续做唯一主尾迹，`FlameTrail` 提供喷口实体读感，`EmberTrail` 只做轻量热余烬，`EnergyLayer` 负责维持机体高能态。
- 避免持续层与 `MainTrail` 抢同一类空间读感，修复“持续段东西很多，但主次不清”的问题。
- 把这一簇推进到可继续实机调感的状态，并顺手修掉影响 Unity 域重载稳定性的运行时异常。

### 技术
- `BoostTrailView` 单一 master intensity 分层映射
- ParticleSystem 本地火焰 / 世界残余双载体分工
- Unity `MaterialPropertyBlock` 直接写入，规避 domain reload 生命周期问题
- Unity 菜单重建 prefab + Console 回归验证 + `dotnet build` 编译闭环

---

---

## Ship VFX 实现：新增 `BoostTrailDebugManager` Inspector 分层调试入口 — 2026-03-16 11:59

### 新建/修改文件
- `Assets/Scripts/Ship/VFX/BoostTrailDebugManager.cs`
- `Assets/Scripts/Ship/Editor/BoostTrailDebugManagerEditor.cs`
- `Assets/Scripts/Ship/VFX/BoostTrailView.cs`
- `Assets/Scripts/Ship/Editor/BoostTrailPrefabCreator.cs`
- `Assets/_Prefabs/VFX/BoostTrailRoot.prefab`
- `Docs/Reference/ShipVFX_CanonicalSpec.md`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容
- 新增 `BoostTrailDebugManager`：作为 `BoostTrailRoot` 上的 Play Mode Inspector 调试入口，支持 `Solo Layer`、逐层显隐遮罩，以及 `ObserveRuntime / ForceSustainPreview` 两种调试模式，方便单独查看 `MainTrail`、`FlameTrail`、`FlameCore`、`EmberTrail`、`EmberSparks`、`EnergyLayer2 / Layer3`、`Activation Halo`、`Bloom`。
- 新增 `BoostTrailDebugManagerEditor` 自定义 Inspector，为上述调试入口补充快速按钮：`Apply Current Debug Mask`、`Reset Preview`、`Preview Boost Start/End`、以及四个 burst 单独预览按钮，避免再临时写一次性 debug 代码。
- 在 `BoostTrailView` 中补充公开调试 API，使调试 manager 可以在不改正式状态机职责的前提下，调用 sustain 预览、图层遮罩和 burst 触发；同时继续把 `_BoostIntensity` 的 `MaterialPropertyBlock` 更新收口成直接写入，避免旧块读取带来的生命周期时序风险。
- 更新 `BoostTrailPrefabCreator`，让 `BoostTrailRoot` 重建时自动挂载并接线 `BoostTrailDebugManager`；经 Unity 重建后，现役 `BoostTrailRoot.prefab` 已包含该组件且 `_boostTrailView` 引用已写入序列化数据。
- 更新 `ShipVFX_CanonicalSpec.md`：补充 `BoostTrailDebugManager` 的 runtime/tool owner 身份，并增加 Play Mode 调试使用说明，明确它只作为调试入口，默认必须保持 dormant。
- 用 `dotnet build Project-Ark.slnx` 验证本轮新增的 runtime/editor 脚本均可编译；再通过 Unity 刷新与 prefab 重建验证，确认新调试入口已进入现役资源链路。

### 目的
- 解决当前 Boost 各层特效混杂在一起、难以快速辨认“哪一层是本轮新做 / 当前在看哪一层”的调试痛点。
- 给后续 `FlameTrail`、`EmberTrail`、`BoostEnergyLayer2 / Layer3` 的实机调感提供稳定、低摩擦的单层观察入口。
- 把调试逻辑和正式表现逻辑解耦，避免为了临时验收不断污染 `BoostTrailView` 主链代码。

### 技术
- `BoostTrailView` 公开调试 API + `BoostTrailDebugManager` 分层遮罩
- Unity Custom Inspector 按钮式调试入口
- `BoostTrailPrefabCreator` 自动挂载/接线调试组件
- `dotnet build` 编译验证 + Unity prefab 序列化验证

---

---

## Ship VFX 修复：`Show Main Trail` 在 Play Mode 下看似失效的 Inspector 代理问题 — 2026-03-16 12:24

### 新建/修改文件
- `Assets/Scripts/Ship/Editor/BoostTrailDebugManagerEditor.cs`
- `Docs/Reference/ShipVFX_CanonicalSpec.md`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容
- 排查确认当前问题的根因不是 `BoostTrailView.DebugApplyVisibilityMask()` 没有关闭 `TrailRenderer`，而是 Play Mode 中 Inspector 当前选中的是 `Assets/_Prefabs/VFX/BoostTrailRoot.prefab` 资源（Unity 选择对象 instanceID `52444`），真正正在播放的现役对象则是场景里的 `Ship/ShipVisual/BoostTrailRoot`（instanceID `-47880`）。因此用户修改 `Show Main Trail` 时，之前实际上改到的是 prefab 资源，不是运行时实例，画面自然不会变化。
- 重构 `BoostTrailDebugManagerEditor`：当 Play Mode 下当前目标不是处于场景层级中的现役实例时，Inspector 会自动查找并代理到活跃的 `BoostTrailDebugManager` 场景实例，再把所有调试字段和预览按钮直接作用到那份实例上。
- 在自定义 Inspector 中补充清晰提示：若当前是 prefab 资源，会显示“正在代理到场景实例”的 Warning，并提供 `Select Live Scene Instance` 按钮；若 `Enable Inspector Debug` 没开，也会明确提示“只改 Show 勾选不会接管运行时链路”。
- 更新 `ShipVFX_CanonicalSpec.md` 的 Play Mode 调试入口说明，补充 prefab 资源与场景实例的区别，并记录新的 Inspector 自动代理行为，降低后续调试误判成本。
- 通过 `dotnet build Project-Ark.slnx` 验证 Editor 代码编译通过；通过 Unity MCP 复查选择状态与场景层级，确认问题现场与修复假设一致。Unity Console 中未发现本次改动引入的新编译错误；当前可见的 `InputHandler` / `SRP Batcher` 警告为既有问题，与本次修复无关。

### 目的
- 确保 `BoostTrailDebugManager` 的 Inspector 开关真正打到 Play Mode 里的现役 `BoostTrailRoot`，避免出现“勾选变了但画面不变”的假阴性。
- 把 Boost 特效调试体验做成更抗误用的链路，减少 prefab 资源与场景实例混淆导致的排查时间浪费。
- 让 `Show Main Trail`、`Solo Layer`、burst preview 等调试操作在用户常见的误选场景下仍能稳定工作。

### 技术
- Unity Custom Inspector 运行时实例代理
- `Object.FindObjectsByType` 场景实例解析
- Play Mode 选择态诊断 + 快速跳转按钮
- `dotnet build` 编译验证 + Unity MCP 运行时实例核对

---

---

## BoostTrail 全局时间回退 — 2026-03-16 14:23

### 修改文件
- `Assets/Scripts/Ship/VFX/BoostTrailView.cs`
- `Assets/Scripts/Ship/Editor/BoostTrailPrefabCreator.cs`
- `Assets/Scripts/Ship/Editor/ShipBoostDebugMenu.cs`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容
- 回退 `BoostTrailView.cs` 中为排查时间冻结问题而加入的全局时间改动：移除启动/结束/Bloom/Halo Tween 上的 `useUnscaledTime: true`，将 `WaitForStartupMomentAsync()` 恢复为默认 `Time.timeScale` 驱动的 `UniTask.Delay(...)`，并删除运行时强制把粒子系统切到 `main.useUnscaledTime = true` 的辅助方法与 `Awake()` 入口。
- 回退 `BoostTrailPrefabCreator.cs` 中 `FlameTrail`、`FlameCore`、`EmberTrail`、`EmberSparks` 的 `main.useUnscaledTime = true` 生成配置，避免后续重建 prefab 时继续写回这组全局时间实验设置。
- 回退 `ShipBoostDebugMenu.cs` 中 `Application.runInBackground = true`，恢复编辑器默认后台行为，不再通过调试菜单改写全局运行策略。
- 完成快速校验：目标三处已确认不存在 `useUnscaledTime` / `ignoreTimeScale` / `runInBackground` 残留；`read_lints` 结果为 0；`dotnet build Project-Ark.slnx` 结果为 0 error（仍有项目既有 warning，与本次回退无关）。

### 目的
- 将之前为了缩小排查范围而临时引入的“全局时间绕行”全部撤回，避免把调试阶段的实验性策略固化进正式运行链路。
- 保持 `BoostTrail` 后续问题继续收敛在真实启动路径与状态机层，而不是混入额外的时间系统分叉。
- 恢复项目当前默认的时间语义，降低未来排查时对 Play Mode、暂停链和 prefab 生成结果的认知噪声。

### 技术
- PrimeTween / UniTask 时间语义回退
- ParticleSystem authored config 回退
- Editor 调试入口副作用收口
- `read_lints` + `dotnet build` 快速验证

---

---

## Ship VFX 模块规则入口落地 — 2026-03-16 14:54

### 新建/修改文件
- `Implement_rules.md`
- `Docs/Reference/ShipVFX_CanonicalSpec.md`
- `Docs/Reference/ShipVFX_AssetRegistry.md`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容
- 在项目根目录新增 `Implement_rules.md`，将其定义为 **模块级实现规则 / 协作约束 / 踩坑治理** 的统一入口，并先为 `Ship / VFX` 模块落地第一版规则集。
- 在新文档中明确 `Implement_rules.md` 与 `ShipVFX_CanonicalSpec.md`、`ShipVFX_AssetRegistry.md` 的职责边界：前者负责“如何实现更不容易继续长债”，后两者继续作为现役链路、owner、路径与状态判断的权威真相源。
- 为 `Ship / VFX` 写入首批长期规则：单一真相源、改动前 5 问、禁止默认新增 fallback、Debug 工具不得接管正式链路、禁止 silent no-op、Runtime / Editor / Scene 三层职责隔离、Scene override 白名单化、硬编码治理、统一官方工具链、先加 validator 再加 debug toggle、迁移纪律、变更说明必须包含 override / tooling 风险。
- 同步更新 `ShipVFX_CanonicalSpec.md`，在 Docs 索引与“与其他文档的关系”中加入 `Implement_rules.md`；同步更新 `ShipVFX_AssetRegistry.md`，将 `Implement_rules.md` 登记为 live docs 体系的一部分，避免规则文档成为孤立信息源或第二真相源。

### 目的
- 把此前口头总结的 VFX 治理经验沉淀为可持续复用的项目规则，而不是每次遇到同类问题再临时解释一遍。
- 为后续继续扩展 `Level`、`Combat`、`UI` 等模块保留统一入口，逐步把项目经验从“对话记忆”转成“仓库内规则资产”。
- 通过与 `CanonicalSpec` / `AssetRegistry` 显式互链，降低文档漂移和多份规则彼此打架的风险。

### 技术
- 模块级规则文档建模
- VFX 协作约束与治理规则沉淀
- 规范文档 / 注册表双向回链收口

---

---

## Ship VFX Authority Matrix 与 Phase A 验收落地 — 2026-03-16 15:08

### 新建/修改文件
- `Implement_rules.md`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容
- 在 `Implement_rules.md` 的 `Ship / VFX` 模块下新增 **Phase A 治理完成标准**，把本轮治理的 5 条完成条件正式写成项目规则：唯一权威、无双轨主链、debug 不接管主链、override 白名单化、无静默失败。
- 为 `Ship / VFX` 补入 **Authority Matrix（执行约束表）**，明确当前批中 `ShipPrefabRebuilder`、`BoostTrailPrefabCreator`、`ShipBoostTrailSceneBinder`、`MaterialTextureLinker`、`BoostTrailDebugManager` 各自的唯一权威范围与禁止的并行写入者，专门用于防止“文件改了又被 builder 写回去”。
- 为关键工具链补入 **Builder 执行模式规则**，要求 future builder / binder / linker 朝 `Audit / Preview / Apply` 三段式收口，并明确当前 `BoostTrailDebugManager` 只允许 preview、不允许 apply。
- 为本轮治理补入 **Scene Override 白名单**，把允许的 scene-only Bloom 绑定与必须跟随 prefab / builder 的核心结构、核心序列化引用区分开，避免继续把 override 漂移和 runtime fallback 混在一起处理。
- 将 `Ship / VFX` 验收清单拆为 **Phase A 治理验收** 与 **常规改动验收** 两层，使后续讨论 authority 收口时有统一 done definition，而普通特效迭代仍保留原先的日常验收项。

### 目的
- 把此前仍停留在对话里的“权限表 / builder 执行边界 / override 白名单”正式沉淀进仓库规则，避免后续继续靠口头约定协作。
- 让 `Implement_rules.md` 从原则文档升级为可执行的治理文档，使“谁能写、谁不能写、哪里允许 override”都能在实现前先被明确。
- 为后续继续做 `ShipVfxValidator`、删旧路径、收菜单权限提供清晰的规则前置条件。

### 技术
- Authority Matrix 文档化
- Builder 生命周期 / 执行模式治理
- Scene Override 白名单规则建模
- Phase A 验收标准收口

---

---

## Ship VFX Phase A 执行计划文档落地 — 2026-03-16 15:26

### 新建/修改文件
- `Docs/Reference/ShipVFX_PhaseA_AuthorityPlan.md`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容
- 新建独立文档 `ShipVFX_PhaseA_AuthorityPlan.md`，将当前 `Ship/VFX` 的 authority 收口治理从“对话里的推进建议”收束为**可执行、可跟踪、可验收**的正式计划文件。
- 在新计划中明确该文档与 `Implement_rules.md`、`ShipVFX_CanonicalSpec.md`、`ShipVFX_AssetRegistry.md`、`ShipVFX_MigrationPlan.md` 的职责边界，避免新 plan 变成第二份总迁移文档或第二真相源。
- 将治理主线拆为 `A0-A5 + Gate G`：冻结治理边界、工具权限审计、菜单与职责收口、Validator/Audit MVP、删除双轨主链、Scene Override 白名单落地，并为每一步写明目标、产出与完成标准。
- 在计划文档中补入执行状态板，标记 `A0` 为文档层已完成、`A1` 为下一步执行项，便于后续按 step 逐个推进并更新状态。
- 明确通过 `Gate G` 之后才进入体验重构主线，确保后续先治理、后体验，而不是两条线继续混跑。

### 目的
- 为当前 `Ship/VFX` 治理工作提供单独的施工文档，让后续每轮推进都有统一入口，而不是继续散落在对话和旧迁移计划中。
- 把“方案 A”的推进顺序固定下来，降低范围扩散和临时改顺序导致的返工风险。
- 让后续执行 `A1` 审计、`A3` validator、`A4` 清理 fallback 时都有统一的 done definition 和阶段门槛。

### 技术
- 治理型执行计划文档建模
- Authority 收口阶段拆分
- Gate 驱动的阶段验收设计
- Ship/VFX 文档职责分层

---

---

## Ship VFX A1 工具权限审计完成 — 2026-03-16 15:36

### 新建/修改文件
- `Docs/Reference/ShipVFX_PhaseA_AuthorityPlan.md`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容
- 在 `ShipVFX_PhaseA_AuthorityPlan.md` 的 `Step A1` 下补入 **实际工具权限审计表**，逐项审计 `ShipPrefabRebuilder`、`BoostTrailPrefabCreator`、`ShipBoostTrailSceneBinder`、`MaterialTextureLinker`、`ShipBuilder`、`BoostTrailDebugManager` 当前分别写哪一层、是否越权、是否存在 fallback / 名字查找 / 隐式写回，并给出明确归类结论。
- 审计结论确认：`ShipPrefabRebuilder`、`BoostTrailPrefabCreator`、`ShipBoostTrailSceneBinder`、`MaterialTextureLinker` 当前可分别视为 `Ship.prefab`、`BoostTrailRoot.prefab`、scene-only Bloom 接线、现役材质链的 authority 入口；`ShipBuilder` 应明确降权为 `Bootstrap`；`BoostTrailDebugManager` 只能保留为 `Debug Only`，且当前实现能力过强，需要在后续步骤中继续降权。
- 将 `ShipBoostDebugMenu` 和 `BoostTrailDebugManagerEditor` 作为 **补充发现** 写入计划文档，明确它们也会触碰 live chain，后续 `A2` 菜单与职责收口时必须一并治理，避免只审主工具却漏掉旁路 debug 入口。
- 同步更新计划文档的推进状态：把 `A1` 标记为已完成，将“当前推荐下一步”切换为 `A2：菜单与职责收口`，使计划文件与实际推进进度一致。

### 目的
- 把 authority matrix 从规则层推进到代码现状层，先把“现在是谁在写、以后谁该写”说清楚，再进入后续的菜单收口、validator 和 fallback 清理。
- 为 `A2` 提供直接可执行的整改依据，避免下一步继续凭感觉调整工具菜单和注释。
- 让本轮治理不是停留在对话结论，而是沉淀为项目正式的计划和实现历史。

### 技术
- Ship/VFX 工具权限审计
- Authority / Bootstrap / Debug Only 分类
- Fallback / 名字查找 / 隐式写回风险识别
- 计划文档状态同步

---

---

## Ship VFX A2 菜单与职责收口完成 — 2026-03-16 15:49

### 新建/修改文件
- `Assets/Scripts/Ship/Editor/ShipBuilder.cs`
- `Assets/Scripts/Ship/Editor/ShipPrefabRebuilder.cs`
- `Assets/Scripts/Ship/Editor/BoostTrailPrefabCreator.cs`
- `Assets/Scripts/Ship/Editor/ShipBoostTrailSceneBinder.cs`
- `Assets/Scripts/Ship/Editor/MaterialTextureLinker.cs`
- `Assets/Scripts/Ship/Editor/ShipBoostDebugMenu.cs`
- `Assets/Scripts/Ship/Editor/BoostTrailDebugManagerEditor.cs`
- `Assets/Scripts/Ship/VFX/BoostTrailDebugManager.cs`
- `Docs/Reference/ShipVFX_PhaseA_AuthorityPlan.md`
- `Docs/ImplementationLog/BoostTrailPhase2_TestChecklist.md`
- `Tools/CopyGGTextures.ps1`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容
- 将 `ShipPrefabRebuilder`、`BoostTrailPrefabCreator`、`ShipBoostTrailSceneBinder`、`MaterialTextureLinker` 的入口统一收口到 `Authority` 菜单分组，明确它们分别是 `Ship.prefab`、`BoostTrailRoot.prefab`、scene-only Bloom 绑定、现役材质贴图链的正式入口；同时移除主要入口中的重复 alias 与 `(GG)` 遗留命名。
- 将 `ShipBuilder` 的菜单改为 `ProjectArk > Ship > Bootstrap > Build Ship Scene Setup`，并在类注释与相关提示文案中明确其是 **bootstrap 工具**，不是 prefab authority。
- 将 `ShipBoostDebugMenu` 收口到 `ProjectArk > Ship > Debug > Legacy > ...`；为 `BoostTrailDebugManager` 增加 preview-only 注释、`AddComponentMenu` 身份入口与 Inspector Tooltip；为 `BoostTrailDebugManagerEditor` 补充 preview-only 顶部提示与按钮文案，明确 debug 工具链不是正式 authority。
- 将 `ShipVFX_PhaseA_AuthorityPlan.md` 更新为 **A2 已完成 / A3 下一步**，并在 `Step A2` 下追加结论摘要；同时同步更新 `CopyGGTextures.ps1` 与 `BoostTrailPhase2_TestChecklist.md` 的菜单提示，避免外部脚本和测试清单继续指向旧路径。

### 目的
- 把 `A1` 中已经审出来的 authority / bootstrap / debug-only 边界，真正落到工具入口、类注释和外部脚本提示上，让团队成员不用靠口头记忆判断谁能写哪一层。
- 先完成“身份收口”，为下一步 `A3` 的 validator / audit 提供稳定的命名与职责基础。
- 降低新人或未来自己在 Editor 菜单中误用旧工具、旁路 debug 工具或 bootstrap 工具的概率。

### 技术
- Unity Editor 菜单分层收口
- 工具职责文案治理
- Preview-only / Legacy Debug 标识强化
- 执行计划与外部提示同步

---

---

## Ship VFX Authority Plan Clean Exit 约束补充 — 2026-03-16 16:02

### 新建/修改文件
- `Docs/Reference/ShipVFX_PhaseA_AuthorityPlan.md`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容
- 在 `ShipVFX_PhaseA_AuthorityPlan.md` 的总目标中新增“**最终完成态约束**”，明确 Phase A 的最终交付态不是长期保留 `legacy` 文件，而是让 `Ship/VFX` 进入**单轨、干净、低冗余**状态。
- 在治理基线中补充：`Legacy / Migration Only` 只允许作为过渡标识；当 authority 替代与 validator 抓手已建立后，应删除不再需要的 legacy 文件、旧入口和旁路脚本。
- 将 `A2` 结论改写为“legacy / preview-only 只是中间态隔离，不是最终保留策略”；并把 `A4` 升级为**删除双轨主链、隐式写回与冗余旧路径**，要求显式给出保留项与删除项原因说明。
- 在 `Gate G` 与页首完成标准中补入 **`Clean Exit`** 约束：即使五条标准表面通过，只要仍保留无职责、无 owner、无保留必要性的旧脚本、旧菜单或 debug 旁路，仍不得视为 Phase A 完成。

### 目的
- 把“VFX 模块最终应干净、无冗余、职责唯一，不为 legacy 长期留位”的方向，从口头偏好升级为计划级硬约束。
- 让后续 `A3/A4/A5` 的执行默认以**清理和删除无效残留**为目标，而不是只做标签化隔离。
- 避免 Phase A 在表面通过审计后，仓库里仍残留与 authority plan 相冲突的旧实现。

### 技术
- Authority Plan 目标约束升级
- Clean Exit 验收口径补充
- Legacy / Migration residue 删除策略前置
- Phase A 步骤与验收标准联动修订

---

---

## Ship VFX MainTrail 定位修正完成 — 2026-03-16 16:17

### 新建/修改文件
- `Docs/Reference/ShipVFX_PhaseA_AuthorityPlan.md`
- `Docs/Reference/ShipVFX_MigrationPlan.md`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容
- 将 `ShipVFX_PhaseA_AuthorityPlan.md` 中关于 `MainTrail` 的口径从“满意基线 / 不主动回改”改为“**当前满意读感参考 / 可直接重构**”，明确：若 `MainTrail` 的实现、结构或流程不符合规范，可以直接精简、重构或替换，只需保持主观效果同类。
- 将 `ShipVFX_MigrationPlan.md` 的阶段目标、MVP 原则、Batch 6 目标、`6.1` 基线定义、`6.2` 评审问题、backlog 表格统一改写为“**先锁读感，不锁实现**”；`MainTrail` 不再被视为 `LOCKED`，而是允许在规范化、减冗余、简化流程收益明确时直接修改。
- 在 `MigrationPlan` 中补入解释：文档里后续所有“沿用 `MainTrail` 方案”的表述，默认都指**参考其想保留的读感与 clean 原则**，而不是把当前实现细节当成固定模板。

### 目的
- 避免 `MainTrail` 因“当前看起来不错”而变成坏标杆或不可质疑模板，导致后续重构反而背着历史包袱前进。
- 把团队执行口径统一到“**效果类似即可，结构和实现应服务于规范与简化**”，让后续改 `MainTrail` 时不再与计划文档冲突。
- 为接下来的 `A3/A4` 和 Phase B 体验重构建立更健康的决策基础：优先保留玩家读感，而不是保留偶然长出来的技术形态。

### 技术
- MainTrail 计划定位重构
- 读感参考 vs 实现冻结 口径拆分
- Batch 6 backlog 与评审规则重写
- Authority Plan / Migration Plan 联动校正

---

---

## Docs: ImplementationLog 时间顺序整理 (2026-03-16 16:46)

**新建/修改文件：**

- `Docs/ImplementationLog/ImplementationLog.md`

**内容：**

1. 将误插在文档中段的 5 条 `2026-03-16` StS2 日志块从旧位置移除，并统一挪到文档末尾。
2. 按时间顺序将 `13:14`、`13:23`、`13:44`、`14:42`、`15:46` 五条记录插入到尾部 `12:30` 研究记录之后、`16:23` 记录之前。
3. 同步恢复 `### Docs 文件夹` 条目下被挤散的 `**目的：**` 文本，让早期主线日志结构回到原始位置。

**目的：** 保持 `ImplementationLog.md` 以时间线为主轴，避免 SideProject 日志插入到 2026-02 的主项目开发记录中，降低后续查阅和追加日志时的混乱。

**技术：** Markdown 文本块重排、按时间顺序归位、最小化差异修正。

---

---

## Ship VFX 实现：落地 `A3 Validator / Audit MVP`（`ShipVfxValidator`）— 2026-03-16 17:05

### 新建/修改文件
- `Assets/Scripts/Ship/Editor/ShipVfxValidator.cs`
- `Docs/Reference/ShipVFX_PhaseA_AuthorityPlan.md`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容
- 新增 `ShipVfxValidator`，并将菜单入口定为 `ProjectArk/Ship/VFX/Audit/Run Ship VFX Audit`；该工具保持 **只读审计**，不会自动修 prefab、scene 或材质资产。
- 第一版审计覆盖四类问题：`Ship.prefab` 核心节点与 `ShipView / ShipEngineVFX` 序列化引用、`BoostTrailRoot.prefab` 核心节点与 `BoostTrailView / BoostTrailDebugManager` 默认状态、scene-only `BoostTrailBloomVolume` / `_boostBloomVolume` 绑定与 override 白名单、以及 authority 违规痕迹（`FindAssets` / `GameObject.Find` / `Type.GetType` / debug 自动补线）静态扫描。
- 在实现过程中发现 `ProjectArk.Ship.Editor` 程序集不能直接引用 `Volume / VolumeProfile`；因此将 scene Bloom 审计改为与 `ShipBoostTrailSceneBinder` 一致的 **反射 + `SerializedObject`** 读取方式，避免为 A3 额外扩张程序集依赖。
- 通过 Unity MCP 首次执行新菜单时，确认菜单已开始运行，但被 `EditorUtility.DisplayDialog` 阻塞成超时；随后将菜单收口为 **无弹窗、纯 Console 输出**，确保后续自动化流程和 MCP 均不会再被 modal dialog 卡住。
- 更新 `ShipVFX_PhaseA_AuthorityPlan.md`：为 `Step A3` 补写结论摘要，标记 `A3` 已完成，并将推荐下一步切换到 `A4`。
- 通过 `dotnet build Project-Ark.slnx` 与 IDE lints 验证，本轮新增审计器与文档同步未引入新的编译或诊断错误。

### 目的
- 把 `AuthorityPlan` 中“无静默失败”的规则正式落成可执行工具，为后续 `A4` 删除双轨主链与旧路径提供可重复的证据链。
- 让 `Ship/VFX` 的 prefab / scene / debug / code residue 问题都能被统一入口快速暴露，而不是继续依赖人工记忆和逐文件猜测。
- 为 `A5` 的 scene override 白名单收口提前铺好基础，避免 override 判定继续漂浮在文档层。

### 技术
- Unity Editor `MenuItem` + `PrefabUtility.LoadPrefabContents` 只读审计
- `SerializedObject` 序列化字段校验与 `PrefabUtility.GetPropertyModifications` override 扫描
- 反射解析 `Volume` 组件类型，规避额外程序集引用
- `dotnet build` 编译闭环 + IDE lints 校验

---

---

## Ship VFX 实现：落地 `A3 Validator / Audit MVP`（`ShipVfxValidator`）— 2026-03-16 17:05

### 新建/修改文件
- `Assets/Scripts/Ship/Editor/ShipVfxValidator.cs`
- `Docs/Reference/ShipVFX_PhaseA_AuthorityPlan.md`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容
- 新增 `ShipVfxValidator`，并将菜单入口定为 `ProjectArk/Ship/VFX/Audit/Run Ship VFX Audit`；该工具保持 **只读审计**，不会自动修 prefab、scene 或材质资产。
- 第一版审计覆盖四类问题：`Ship.prefab` 核心节点与 `ShipView / ShipEngineVFX` 序列化引用、`BoostTrailRoot.prefab` 核心节点与 `BoostTrailView / BoostTrailDebugManager` 默认状态、scene-only `BoostTrailBloomVolume` / `_boostBloomVolume` 绑定与 override 白名单、以及 authority 违规痕迹（`FindAssets` / `GameObject.Find` / `Type.GetType` / debug 自动补线）静态扫描。
- 在实现过程中发现 `ProjectArk.Ship.Editor` 程序集不能直接引用 `Volume / VolumeProfile`；因此将 scene Bloom 审计改为与 `ShipBoostTrailSceneBinder` 一致的 **反射 + `SerializedObject`** 读取方式，避免为 A3 额外扩张程序集依赖。
- 通过 Unity MCP 首次执行新菜单时，确认菜单已开始运行，但被 `EditorUtility.DisplayDialog` 阻塞成超时；随后将菜单收口为 **无弹窗、纯 Console 输出**，确保后续自动化流程和 MCP 均不会再被 modal dialog 卡住。
- 更新 `ShipVFX_PhaseA_AuthorityPlan.md`：为 `Step A3` 补写结论摘要，标记 `A3` 已完成，并将推荐下一步切换到 `A4`。
- 通过 `dotnet build Project-Ark.slnx` 与 IDE lints 验证，本轮新增审计器与文档同步未引入新的编译或诊断错误。

### 目的
- 把 `AuthorityPlan` 中“无静默失败”的规则正式落成可执行工具，为后续 `A4` 删除双轨主链与旧路径提供可重复的证据链。
- 让 `Ship/VFX` 的 prefab / scene / debug / code residue 问题都能被统一入口快速暴露，而不是继续依赖人工记忆和逐文件猜测。
- 为 `A5` 的 scene override 白名单收口提前铺好基础，避免 override 判定继续漂浮在文档层。

### 技术
- Unity Editor `MenuItem` + `PrefabUtility.LoadPrefabContents` 只读审计
- `SerializedObject` 序列化字段校验与 `PrefabUtility.GetPropertyModifications` override 扫描
- 反射解析 `Volume` 组件类型，规避额外程序集引用
- `dotnet build` 编译闭环 + IDE lints 校验

---

---

## Ship VFX 实现：推进 `A4` 第一批清理（debug takeover / ShipView fallback / legacy debug menu）— 2026-03-16 17:33

### 新建/修改文件
- `Assets/Scripts/Ship/VFX/BoostTrailDebugManager.cs`
- `Assets/Scripts/Ship/Editor/BoostTrailDebugManagerEditor.cs`
- `Assets/Scripts/Ship/VFX/ShipView.cs`
- `Assets/Scripts/Ship/Editor/ShipVfxValidator.cs`
- `Assets/Scripts/Ship/Editor/ShipBoostDebugMenu.cs`（删除）
- `Assets/Scripts/Ship/Editor/ShipBoostDebugMenu.cs.meta`（删除）
- `ProjectArk.Ship.Editor.csproj`
- `Docs/Reference/ShipVFX_PhaseA_AuthorityPlan.md`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容
- 删除 `ShipBoostDebugMenu` 及其 `.meta`，并同步从 `ProjectArk.Ship.Editor.csproj` 中移除编译项，彻底收口这条直接改 `ShipBoost` / `BoostActivationHalo` 的 legacy debug 旁路。
- 重构 `BoostTrailDebugManager`：移除 `Reset`、`OnValidate`、`Awake`、`LateUpdate`、`OnDisable`、`AutoAssignReferences()`、`RestoreSafeDefaultsIfStaleSessionDetected()` 等自动补线 / 自动恢复 / 持续 takeover 逻辑，改为只有显式预览按钮才会驱动 live chain；默认 `DebugMode` 也收回到 `ObserveRuntime`。
- 重构 `BoostTrailDebugManagerEditor`：取消“prefab Inspector 自动代理到运行时实例”的旧行为，改为只允许当前选中的 live scene instance 执行预览按钮，避免 prefab 资源与 runtime instance 再次混成一条链。
- 清理 `ShipView` 的双轨与运行时兜底：移除对 `ShipBoost / ShipDash` 事件的 legacy fallback，只保留 `ShipStateController.OnStateChanged` 主链；同时删除 normal liquid sprite 与 dodge sprite 的 runtime fallback，缺失关键接线时直接 `Debug.LogError(...)` 暴露问题。
- 扩展 `ShipVfxValidator` 的静态检查范围，把 `BoostTrailDebugManager.LateUpdate()`、`BoostTrailDebugManagerEditor` runtime proxy、`ShipView` 的 legacy 事件订阅与 sprite fallback 全部纳入防回归审计。
- 通过 `dotnet build Project-Ark.slnx` 验证本轮改动为 **0 warning / 0 error**；通过 Unity MCP 刷新并重跑 `ProjectArk/Ship/VFX/Audit/Run Ship VFX Audit`，确认审计输出从 **16 条降到 11 条**，本轮清理命中的 warning 已实际消失。
- 同步更新 `ShipVFX_PhaseA_AuthorityPlan.md`：将 `A4` 状态从“待开始”推进为“进行中”，并记录第一批已完成的收口动作与下一步焦点。

### 目的
- 先砍掉最直接的 `debug takeover`、legacy debug 菜单和 `ShipView` 运行时兜底，让 `Ship/VFX` 主链开始真正依赖唯一 authority，而不是继续靠旁路补救。
- 把“选中 prefab 资源却以为自己在调 runtime”的高频认知陷阱从工具链里拿掉，降低后续 Play Mode 调试误判成本。
- 为接下来的 `A4` 后半段（authority 工具 residue）和 `A5`（scene override 白名单）创造更干净的基线。

### 技术
- Unity Editor 调试入口降权与菜单退役
- 运行时 fallback 删除 + 显式报错替代 silent repair
- `ShipVfxValidator` Code Audit 防回归扩展
- `dotnet build` + Unity MCP 审计闭环验证

---

---

## Ship VFX 实现：推进 `A4` 第一批清理（debug takeover / ShipView fallback / legacy debug menu）— 2026-03-16 17:33

### 新建/修改文件
- `Assets/Scripts/Ship/VFX/BoostTrailDebugManager.cs`
- `Assets/Scripts/Ship/Editor/BoostTrailDebugManagerEditor.cs`
- `Assets/Scripts/Ship/VFX/ShipView.cs`
- `Assets/Scripts/Ship/Editor/ShipVfxValidator.cs`
- `Assets/Scripts/Ship/Editor/ShipBoostDebugMenu.cs`（删除）
- `Assets/Scripts/Ship/Editor/ShipBoostDebugMenu.cs.meta`（删除）
- `ProjectArk.Ship.Editor.csproj`
- `Docs/Reference/ShipVFX_PhaseA_AuthorityPlan.md`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容
- 删除 `ShipBoostDebugMenu` 及其 `.meta`，并同步从 `ProjectArk.Ship.Editor.csproj` 中移除编译项，彻底收口这条直接改 `ShipBoost` / `BoostActivationHalo` 的 legacy debug 旁路。
- 重构 `BoostTrailDebugManager`：移除 `Reset`、`OnValidate`、`Awake`、`LateUpdate`、`OnDisable`、`AutoAssignReferences()`、`RestoreSafeDefaultsIfStaleSessionDetected()` 等自动补线 / 自动恢复 / 持续 takeover 逻辑，改为只有显式预览按钮才会驱动 live chain；默认 `DebugMode` 也收回到 `ObserveRuntime`。
- 重构 `BoostTrailDebugManagerEditor`：取消“prefab Inspector 自动代理到运行时实例”的旧行为，改为只允许当前选中的 live scene instance 执行预览按钮，避免 prefab 资源与 runtime instance 再次混成一条链。
- 清理 `ShipView` 的双轨与运行时兜底：移除对 `ShipBoost / ShipDash` 事件的 legacy fallback，只保留 `ShipStateController.OnStateChanged` 主链；同时删除 normal liquid sprite 与 dodge sprite 的 runtime fallback，缺失关键接线时直接 `Debug.LogError(...)` 暴露问题。
- 扩展 `ShipVfxValidator` 的静态检查范围，把 `BoostTrailDebugManager.LateUpdate()`、`BoostTrailDebugManagerEditor` runtime proxy、`ShipView` 的 legacy 事件订阅与 sprite fallback 全部纳入防回归审计。
- 通过 `dotnet build Project-Ark.slnx` 验证本轮改动为 **0 warning / 0 error**；通过 Unity MCP 刷新并重跑 `ProjectArk/Ship/VFX/Audit/Run Ship VFX Audit`，确认审计输出从 **16 条降到 11 条**，本轮清理命中的 warning 已实际消失。
- 同步更新 `ShipVFX_PhaseA_AuthorityPlan.md`：将 `A4` 状态从“待开始”推进为“进行中”，并记录第一批已完成的收口动作与下一步焦点。

### 目的
- 先砍掉最直接的 `debug takeover`、legacy debug 菜单和 `ShipView` 运行时兜底，让 `Ship/VFX` 主链开始真正依赖唯一 authority，而不是继续靠旁路补救。
- 把“选中 prefab 资源却以为自己在调 runtime”的高频认知陷阱从工具链里拿掉，降低后续 Play Mode 调试误判成本。
- 为接下来的 `A4` 后半段（authority 工具 residue）和 `A5`（scene override 白名单）创造更干净的基线。

### 技术
- Unity Editor 调试入口降权与菜单退役
- 运行时 fallback 删除 + 显式报错替代 silent repair
- `ShipVfxValidator` Code Audit 防回归扩展
- `dotnet build` + Unity MCP 审计闭环验证

---

---

## Ship VFX 实现：推进 `A4` 后半段 authority 工具残留收口 — 2026-03-17 11:50

### 新建/修改文件
- `Assets/Scripts/Ship/Editor/ShipPrefabRebuilder.cs`
- `Assets/Scripts/Ship/Editor/BoostTrailPrefabCreator.cs`
- `Assets/Scripts/Ship/Editor/MaterialTextureLinker.cs`
- `Assets/Scripts/Ship/Editor/ShipBoostTrailSceneBinder.cs`
- `Docs/Reference/ShipVFX_PhaseA_AuthorityPlan.md`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容
- 收口 `ShipPrefabRebuilder`：把 `ShipJuiceSettingsSO` 改为固定路径 `Assets/_Data/Ship/DefaultShipJuiceSettings.asset`，把 ship/liquid/highlight/dodge sprite 全部改为 authority 路径精确加载；删除 `FindSpriteExactOrByName()` 名字搜索、删除外部盘符 `DODGE_SPRITE_SRC_PATH` / 自动复制逻辑，只保留仓库内受管资源。
- 收口 `BoostTrailPrefabCreator`：删除 `FindSpriteExactOrByName()` 模糊兜底，改为精确路径加载 overlay / activation halo sprite；当资源缺失时显式 `Debug.LogWarning(...)`，不再静默退回到全项目搜索。
- 收口 `MaterialTextureLinker`：删除 `AssetDatabase.FindAssets` 全项目纹理兜底，只允许从 `Assets/_Art/VFX/BoostTrail/Textures/` 下按精确路径回填材质贴图，并把日志改为输出 authority 路径。
- 收口 `ShipBoostTrailSceneBinder`：删除 `GameObject.Find` 和 `Type.GetType`；改为通过 `TypeCache.GetTypesDerivedFrom<Component>()` 解析 `Volume` 组件类型，并扫描现有组件列表定位 `BoostTrailBloomVolume`，同时继续用 `SerializedObject` 回填 `sharedProfile`，避免引入新的程序集依赖。
- 通过代码搜索确认以下 residue 已从 authority 工具源码中消失：`AssetDatabase.FindAssets`、`FindSpriteExactOrByName`、`DODGE_SPRITE_SRC_PATH`、`GameObject.Find`、`Type.GetType`。
- 通过 `dotnet build Project-Ark.slnx` 验证本轮改动为 **0 warning / 0 error**。
- 通过 Unity MCP 完成刷新与菜单调用，但本轮 `ShipVfxValidator` 的 Console 输出在 MCP 回读中未返回，因此未重新拿到最新审计计数；已在 `ShipVFX_PhaseA_AuthorityPlan.md` 中明确记录这一点，留待下一轮补审计计数确认。
- 同步更新 `ShipVFX_PhaseA_AuthorityPlan.md`：将 `A4` 状态推进为“进行中（2026-03-17）”，并把剩余焦点明确收口到 `VisualChild` legacy alias 与 `ShipBuilder` bootstrap residue。

### 目的
- 把 authority 工具本身制造的模糊搜索和兜底路径先清干净，让之后的 warning 更接近真实 legacy / scene 漂移问题。
- 让 prefab / 材质 / scene binder 的资源来源和依赖边界都回到“精确路径 + 显式缺失”模式，减少继续扩散的双轨复杂度。
- 为下一轮处理 `VisualChild` alias 与 `A5` scene override 白名单提供更干净的基线。

### 技术
- Unity Editor authority 工具精确路径收口
- `TypeCache` 组件类型解析替代 `Type.GetType`
- `SerializedObject` 场景引用回填
- `dotnet build` 编译闭环 + 代码搜索残留校验

---

---

## Ship VFX 实现：启动 `A5` 首轮场景 override 初勘 — 2026-03-17 12:31

### 新建/修改文件
- `Docs/Reference/ShipVFX_PhaseA_AuthorityPlan.md`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容
- 基于 `git diff -- Assets/Scenes/SampleScene.unity` 对当前场景改动做首轮 A5 初勘，并把结论沉淀进 `ShipVFX_PhaseA_AuthorityPlan.md`。
- 当前 diff 显示 scene-local `VisualChild` GameObject 与旧 `SpriteRenderer` 已从场景实例中移除；`DashAfterImageSpawner._shipSpriteRenderer` 已改绑到 prefab 内 `Ship_Sprite_Solid` stripped renderer；`ShipVisualJuice._visualChild` 已改绑到 prefab 内 `ShipVisual` stripped transform。
- `_boostBloomVolume -> BoostTrailBloomVolume` 仍作为 scene-only 绑定保留，已在计划文档中标记为 A5 的合法 override 候选。
- 多处 `_flashImage: null` override 已从 diff 中被清走，说明场景中无 owner 的空引用覆盖正在被清理；已把这条口径写入 A5 观察结论，避免后续把这类脏 override 又引回来。
- 同步更新状态板：A4 标记为“代码侧已收口”，A5 标记为“已完成场景 diff 初勘”。

### 目的
- 把 A5 从“抽象要做 scene override 白名单”推进到“已有首批具体场景证据”，减少下一轮在大 YAML 文件里重新考古的成本。
- 先区分合法修正和脏覆盖，再进入 Unity Editor 做最终审计计数确认。

### 技术
- `git diff` 场景序列化差异分析
- Prefab stripped 引用与 scene-only 绑定归类
- A5 白名单候选沉淀

---

---

## Ship VFX 实现：推进 `A4` 收尾（`VisualChild` alias + `ShipBuilder` bootstrap residue）— 2026-03-17 12:31

### 新建/修改文件
- `Assets/Scripts/Ship/Editor/ShipBuilder.cs`
- `Assets/Scripts/Ship/Editor/ShipPrefabRebuilder.cs`
- `Docs/Reference/ShipVFX_PhaseA_AuthorityPlan.md`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容
- 收口 `ShipBuilder`：删除 `VisualChild` 兼容路径；将 `GrabGun_Base_8/9` 参考 sprite、`DefaultShipStats.asset`、`DefaultShipJuiceSettings.asset`、`ShipActions.inputactions`、`DashAfterImage.prefab` 全部改为固定 authority 路径加载，不再使用 `AssetDatabase.FindAssets` 搜索。
- 同步修正 `ShipBuilder` 的旧 bootstrap 结构：不再给 `ShipVisual` 根节点挂旧单层 `SpriteRenderer`；`EngineParticles` 改为围绕多层 sprite 结构创建；`DashAfterImageSpawner._shipSpriteRenderer` 改为接到 `Ship_Sprite_Solid`，与 `ShipPrefabRebuilder` 当前 authority 接线保持一致。
- 收口 `ShipPrefabRebuilder`：删除最后残留的 `VisualChild` fallback，`ShipVisual` 成为唯一视觉根节点入口。
- 通过代码搜索确认以下目标 residue 已在 5 个 editor authority 工具中清零：`VisualChild`、`LEGACY_VISUAL_CHILD_NAME`、`AssetDatabase.FindAssets`、`FindSpriteExactOrByName`、`GameObject.Find`、`Type.GetType`。
- 额外对 `BoostTrailDebugManager.cs`、`BoostTrailDebugManagerEditor.cs`、`ShipView.cs` 做定点搜索，旧 debug takeover / runtime fallback 目标模式当前均为 0 命中。
- 通过 `dotnet build Project-Ark.slnx` 验证本轮改动后解决方案恢复为 **0 warning / 0 error**。
- 更新 `ShipVFX_PhaseA_AuthorityPlan.md`：把 A4 状态推进为“代码侧已收口”，并把下一步调整为补跑 Unity 审计计数 + 进入 A5 scene override 白名单收口。

### 目的
- 彻底消除 A4 剩余的 `VisualChild` alias 与 `ShipBuilder` bootstrap 双轨噪声，让 Ship/VFX 代码面只保留 canonical 单轨入口。
- 让之后的 `ShipVfxValidator` warning 更聚焦于真实的 Unity 场景态问题，而不是 editor 工具自己制造的残留信号。
- 为 A5 的 scene override 白名单整理提供更稳定的基线。

### 技术
- Unity Editor bootstrap / authority 工具固定路径收口
- 多层 `ShipVisual` 结构对齐
- 静态代码 residue 搜索校验
- `dotnet build` 编译闭环验证

---

---

## Ship VFX 实现：启动 `A5` 首轮场景 override 初勘 — 2026-03-17 12:31

### 新建/修改文件
- `Docs/Reference/ShipVFX_PhaseA_AuthorityPlan.md`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容
- 基于 `git diff -- Assets/Scenes/SampleScene.unity` 对当前场景改动做首轮 A5 初勘，并把结论沉淀进 `ShipVFX_PhaseA_AuthorityPlan.md`。
- 当前 diff 显示 scene-local `VisualChild` GameObject 与旧 `SpriteRenderer` 已从场景实例中移除；`DashAfterImageSpawner._shipSpriteRenderer` 已改绑到 prefab 内 `Ship_Sprite_Solid` stripped renderer；`ShipVisualJuice._visualChild` 已改绑到 prefab 内 `ShipVisual` stripped transform。
- `_boostBloomVolume -> BoostTrailBloomVolume` 仍作为 scene-only 绑定保留，已在计划文档中标记为 A5 的合法 override 候选。
- 多处 `_flashImage: null` override 已从 diff 中被清走，说明场景中无 owner 的空引用覆盖正在被清理；已把这条口径写入 A5 观察结论，避免后续把这类脏 override 又引回来。
- 同步更新状态板：A4 标记为“代码侧已收口”，A5 标记为“已完成场景 diff 初勘”。

### 目的
- 把 A5 从“抽象要做 scene override 白名单”推进到“已有首批具体场景证据”，减少下一轮在大 YAML 文件里重新考古的成本。
- 先区分合法修正和脏覆盖，再进入 Unity Editor 做最终审计计数确认。

### 技术
- `git diff` 场景序列化差异分析
- Prefab stripped 引用与 scene-only 绑定归类
- A5 白名单候选沉淀

---

---

## Ship VFX 实现：推进 `A4` 收尾（`VisualChild` alias + `ShipBuilder` bootstrap residue）— 2026-03-17 12:31

### 新建/修改文件
- `Assets/Scripts/Ship/Editor/ShipBuilder.cs`
- `Assets/Scripts/Ship/Editor/ShipPrefabRebuilder.cs`
- `Docs/Reference/ShipVFX_PhaseA_AuthorityPlan.md`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容
- 收口 `ShipBuilder`：删除 `VisualChild` 兼容路径；将 `GrabGun_Base_8/9` 参考 sprite、`DefaultShipStats.asset`、`DefaultShipJuiceSettings.asset`、`ShipActions.inputactions`、`DashAfterImage.prefab` 全部改为固定 authority 路径加载，不再使用 `AssetDatabase.FindAssets` 搜索。
- 同步修正 `ShipBuilder` 的旧 bootstrap 结构：不再给 `ShipVisual` 根节点挂旧单层 `SpriteRenderer`；`EngineParticles` 改为围绕多层 sprite 结构创建；`DashAfterImageSpawner._shipSpriteRenderer` 改为接到 `Ship_Sprite_Solid`，与 `ShipPrefabRebuilder` 当前 authority 接线保持一致。
- 收口 `ShipPrefabRebuilder`：删除最后残留的 `VisualChild` fallback，`ShipVisual` 成为唯一视觉根节点入口。
- 通过代码搜索确认以下目标 residue 已在 5 个 editor authority 工具中清零：`VisualChild`、`LEGACY_VISUAL_CHILD_NAME`、`AssetDatabase.FindAssets`、`FindSpriteExactOrByName`、`GameObject.Find`、`Type.GetType`。
- 额外对 `BoostTrailDebugManager.cs`、`BoostTrailDebugManagerEditor.cs`、`ShipView.cs` 做定点搜索，旧 debug takeover / runtime fallback 目标模式当前均为 0 命中。
- 通过 `dotnet build Project-Ark.slnx` 验证本轮改动后解决方案恢复为 **0 warning / 0 error**。
- 更新 `ShipVFX_PhaseA_AuthorityPlan.md`：把 A4 状态推进为“代码侧已收口”，并把下一步调整为补跑 Unity 审计计数 + 进入 A5 scene override 白名单收口。

### 目的
- 彻底消除 A4 剩余的 `VisualChild` alias 与 `ShipBuilder` bootstrap 双轨噪声，让 Ship/VFX 代码面只保留 canonical 单轨入口。
- 让之后的 `ShipVfxValidator` warning 更聚焦于真实的 Unity 场景态问题，而不是 editor 工具自己制造的残留信号。
- 为 A5 的 scene override 白名单整理提供更稳定的基线。

### 技术
- Unity Editor bootstrap / authority 工具固定路径收口
- 多层 `ShipVisual` 结构对齐
- 静态代码 residue 搜索校验
- `dotnet build` 编译闭环验证

---

---

## Ship VFX 实现：修正 `ShipVfxValidator` 的 A5 scene override 误报 — 2026-03-17 13:03

### 新建/修改文件
- `Assets/Scripts/Ship/Editor/ShipVfxValidator.cs`
- `Docs/Reference/ShipVFX_PhaseA_AuthorityPlan.md`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容
- 使用 Unity MCP 直接核对 `SampleScene` 实态：确认 `ShipVisualJuice._visualChild -> ShipVisual`、`DashAfterImageSpawner._shipSpriteRenderer -> Ship_Sprite_Solid`、`BoostTrailView._boostBloomVolume -> BoostTrailBloomVolume` 已处于当前 canonical / scene-only 合法链路。
- 同时确认 `BoostTrailDebugManager` 当前不是独立 scene object，而是挂在 `BoostTrailRoot` 上的组件，且 `_enableInspectorDebug = false`；因此 A5 的主要问题不再是活跃 debug takeover，而是审计与治理口径是否准确。
- 结合 `SampleScene.unity` 的 prefab instance 修改块与 Unity 实态，定位到 `ShipVfxValidator` 的误报根因：旧版 `GetIllegalOverridePaths()` 直接使用 `PrefabUtility.GetPropertyModifications(target)` 结果，不区分 modification 实际对应的 source object，导致外层 `Ship` prefab instance 的 `_stats`、`_engineParticles`、`_shipSpriteRenderer`、`_visualChild`、Rigidbody 默认值、Transform 默认值等 override 被错误归因到 `BoostTrailView` / `BoostTrailDebugManager`。
- 修改 `ShipVfxValidator`：新增基于 `PrefabUtility.GetCorrespondingObjectFromSource(target)` + `AssetDatabase.TryGetGUIDAndLocalFileIdentifier(...)` 的过滤逻辑，只统计“当前组件自身对应 source object”的 override；`BoostTrailView` 继续只白名单 `_boostBloomVolume`。
- 通过 `dotnet build Project-Ark.slnx` 验证本轮修改后解决方案仍为 **0 warning / 0 error**；Unity 刷新并重新触发审计后，`read_console` 未再返回新的 `ShipVfxValidator` 错误/警告项。
- 同步更新 `ShipVFX_PhaseA_AuthorityPlan.md`：把 A5 的当前焦点从“怀疑场景有大量非法 override”改为“完成 validator 粒度修正后的收尾治理”，并更新执行状态板。

### 目的
- 消除 A5 的核心误导信号，避免把已经合法化的 canonical 接线和 scene-only 绑定误判成脏 override。
- 让后续 scene override 白名单治理基于真实场景问题推进，而不是被 validator 的错误归因带偏。

### 技术
- Unity MCP 场景对象/组件实态核查
- `PrefabUtility.GetPropertyModifications` 归因过滤
- `AssetDatabase.TryGetGUIDAndLocalFileIdentifier` source object 精确匹配
- `dotnet build` + Unity 刷新验证

---

---

## Ship VFX 实现：修正 `ShipVfxValidator` 的 A5 scene override 误报 — 2026-03-17 13:03

### 新建/修改文件
- `Assets/Scripts/Ship/Editor/ShipVfxValidator.cs`
- `Docs/Reference/ShipVFX_PhaseA_AuthorityPlan.md`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容
- 使用 Unity MCP 直接核对 `SampleScene` 实态：确认 `ShipVisualJuice._visualChild -> ShipVisual`、`DashAfterImageSpawner._shipSpriteRenderer -> Ship_Sprite_Solid`、`BoostTrailView._boostBloomVolume -> BoostTrailBloomVolume` 已处于当前 canonical / scene-only 合法链路。
- 同时确认 `BoostTrailDebugManager` 当前不是独立 scene object，而是挂在 `BoostTrailRoot` 上的组件，且 `_enableInspectorDebug = false`；因此 A5 的主要问题不再是活跃 debug takeover，而是审计与治理口径是否准确。
- 结合 `SampleScene.unity` 的 prefab instance 修改块与 Unity 实态，定位到 `ShipVfxValidator` 的误报根因：旧版 `GetIllegalOverridePaths()` 直接使用 `PrefabUtility.GetPropertyModifications(target)` 结果，不区分 modification 实际对应的 source object，导致外层 `Ship` prefab instance 的 `_stats`、`_engineParticles`、`_shipSpriteRenderer`、`_visualChild`、Rigidbody 默认值、Transform 默认值等 override 被错误归因到 `BoostTrailView` / `BoostTrailDebugManager`。
- 修改 `ShipVfxValidator`：新增基于 `PrefabUtility.GetCorrespondingObjectFromSource(target)` + `AssetDatabase.TryGetGUIDAndLocalFileIdentifier(...)` 的过滤逻辑，只统计“当前组件自身对应 source object”的 override；`BoostTrailView` 继续只白名单 `_boostBloomVolume`。
- 通过 `dotnet build Project-Ark.slnx` 验证本轮修改后解决方案仍为 **0 warning / 0 error**；Unity 刷新并重新触发审计后，`read_console` 未再返回新的 `ShipVfxValidator` 错误/警告项。
- 同步更新 `ShipVFX_PhaseA_AuthorityPlan.md`：把 A5 的当前焦点从“怀疑场景有大量非法 override”改为“完成 validator 粒度修正后的收尾治理”，并更新执行状态板。

### 目的
- 消除 A5 的核心误导信号，避免把已经合法化的 canonical 接线和 scene-only 绑定误判成脏 override。
- 让后续 scene override 白名单治理基于真实场景问题推进，而不是被 validator 的错误归因带偏。

### 技术
- Unity MCP 场景对象/组件实态核查
- `PrefabUtility.GetPropertyModifications` 归因过滤
- `AssetDatabase.TryGetGUIDAndLocalFileIdentifier` source object 精确匹配
- `dotnet build` + Unity 刷新验证

---

---

## Ship VFX 实现：确认 `BoostTrailDebugManager` 已回到 preview-only 边界 — 2026-03-17 13:31

### 新建/修改文件
- `Docs/Reference/ShipVFX_PhaseA_AuthorityPlan.md`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容
- 重新核对 `BoostTrailDebugManager.cs` 与 `BoostTrailDebugManagerEditor.cs` 当前实现，确认 runtime 改动只来自显式预览按钮：组件本体已无 `Reset / OnValidate / Awake / LateUpdate / OnDisable` 这类自动补线、自动恢复或持续接管 live chain 的生命周期入口。
- 同时确认自定义 Inspector 已禁止从 prefab Inspector 代理到场景现役实例；只有 Play Mode 下选中 live scene instance 时，`Apply Current Preview Mask / Preview Boost Start / Preview Bloom Burst` 等按钮才会真正驱动运行时画面。
- 结合 Unity MCP 再次重跑 `ShipVfxValidator` 与读取 Editor 状态，未再看到新的 `Scene Debug / Scene Override` 告警输出；因此 A5 的剩余问题不再是 debug takeover，而是后续 Phase B 是否继续保留该 debug-only 组件的产品决策。
- 同步回写 `ShipVFX_PhaseA_AuthorityPlan.md`：修正文档前半段对 `BoostTrailDebugManager` 的历史性旧判断，避免与当前代码实态冲突；并将 A5 状态从“进行中”推进为“已完成（场景边界收口）”，下一步切入 `Gate G`。

### 目的
- 让计划文档、代码实态、Unity 场景核查三者口径重新一致，避免后续继续把已收口的 preview-only 工具误认为 active takeover 风险。
- 为进入 `Gate G` 提供一个明确前提：A5 的阻塞项已从“debug 工具接管主链”降为“Phase B 是否保留 debug-only 组件”的非阻塞治理决策。

### 技术
- C# 代码审计（生命周期入口 / Inspector 按钮驱动链）
- Unity MCP 编辑器状态与审计复核
- Phase A 计划文档状态板收口

---

---

## Ship VFX 实现：确认 `BoostTrailDebugManager` 已回到 preview-only 边界 — 2026-03-17 13:31

### 新建/修改文件
- `Docs/Reference/ShipVFX_PhaseA_AuthorityPlan.md`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容
- 重新核对 `BoostTrailDebugManager.cs` 与 `BoostTrailDebugManagerEditor.cs` 当前实现，确认 runtime 改动只来自显式预览按钮：组件本体已无 `Reset / OnValidate / Awake / LateUpdate / OnDisable` 这类自动补线、自动恢复或持续接管 live chain 的生命周期入口。
- 同时确认自定义 Inspector 已禁止从 prefab Inspector 代理到场景现役实例；只有 Play Mode 下选中 live scene instance 时，`Apply Current Preview Mask / Preview Boost Start / Preview Bloom Burst` 等按钮才会真正驱动运行时画面。
- 结合 Unity MCP 再次重跑 `ShipVfxValidator` 与读取 Editor 状态，未再看到新的 `Scene Debug / Scene Override` 告警输出；因此 A5 的剩余问题不再是 debug takeover，而是后续 Phase B 是否继续保留该 debug-only 组件的产品决策。
- 同步回写 `ShipVFX_PhaseA_AuthorityPlan.md`：修正文档前半段对 `BoostTrailDebugManager` 的历史性旧判断，避免与当前代码实态冲突；并将 A5 状态从“进行中”推进为“已完成（场景边界收口）”，下一步切入 `Gate G`。

### 目的
- 让计划文档、代码实态、Unity 场景核查三者口径重新一致，避免后续继续把已收口的 preview-only 工具误认为 active takeover 风险。
- 为进入 `Gate G` 提供一个明确前提：A5 的阻塞项已从“debug 工具接管主链”降为“Phase B 是否保留 debug-only 组件”的非阻塞治理决策。

### 技术
- C# 代码审计（生命周期入口 / Inspector 按钮驱动链）
- Unity MCP 编辑器状态与审计复核
- Phase A 计划文档状态板收口

---

---

## Ship VFX 实现：完成 `Gate G` 复核并结束 Phase A 治理 — 2026-03-17 13:36

### 新建/修改文件
- `Docs/Reference/ShipVFX_PhaseA_AuthorityPlan.md`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容
- 基于本轮已完成的 A4/A5 证据链，对 `Gate G` 五条标准与 `Clean Exit` 做了显式复核：唯一权威、无双轨主链、debug 不接管主链、override 白名单化、无静默失败均已满足。
- 复核依据包括：authority 菜单入口已收口、`BoostTrailDebugManager` / `BoostTrailDebugManagerEditor` 已满足 preview-only 边界、`ShipVfxValidator` 已覆盖 prefab/scene/debug/静态代码痕迹四类审计、`SampleScene` 中的 `_visualChild / _shipSpriteRenderer / _boostBloomVolume` 已能判定为合法修正或合法 scene-only 绑定。
- 额外检查 `Ship/Editor` 当前暴露的菜单入口后，确认仓库中已不存在无 owner 的 legacy / debug-only VFX 菜单；保留的 `ShipBuilder` 与 `BoostTrailDebugManager` 仍有明确 owner、使用边界与后续 Phase B 决策位置，因此不构成 `Clean Exit` 阻塞项。
- 同步回写 `ShipVFX_PhaseA_AuthorityPlan.md`：新增 `Gate G 复核结果（2026-03-17）`，并把“当前推荐的下一步”从“进入 Gate G 验收”推进为“正式进入 Phase B 体验重构主线”；状态板中的 `Gate G` 也标记为已通过。

### 目的
- 为 `Ship / VFX` 治理阶段提供正式的收尾节点，避免 Phase A 明明已完成却长期停留在“待验收”状态。
- 让后续工作重心明确切回体验重构，而不是继续在已收口的问题上重复治理。

### 技术
- Gate checklist 逐条复核
- Unity 菜单入口盘点
- 文档状态板收口

---

---

## Ship VFX 实现：完成 `Gate G` 复核并结束 Phase A 治理 — 2026-03-17 13:36

### 新建/修改文件
- `Docs/Reference/ShipVFX_PhaseA_AuthorityPlan.md`
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容
- 基于本轮已完成的 A4/A5 证据链，对 `Gate G` 五条标准与 `Clean Exit` 做了显式复核：唯一权威、无双轨主链、debug 不接管主链、override 白名单化、无静默失败均已满足。
- 复核依据包括：authority 菜单入口已收口、`BoostTrailDebugManager` / `BoostTrailDebugManagerEditor` 已满足 preview-only 边界、`ShipVfxValidator` 已覆盖 prefab/scene/debug/静态代码痕迹四类审计、`SampleScene` 中的 `_visualChild / _shipSpriteRenderer / _boostBloomVolume` 已能判定为合法修正或合法 scene-only 绑定。
- 额外检查 `Ship/Editor` 当前暴露的菜单入口后，确认仓库中已不存在无 owner 的 legacy / debug-only VFX 菜单；保留的 `ShipBuilder` 与 `BoostTrailDebugManager` 仍有明确 owner、使用边界与后续 Phase B 决策位置，因此不构成 `Clean Exit` 阻塞项。
- 同步回写 `ShipVFX_PhaseA_AuthorityPlan.md`：新增 `Gate G 复核结果（2026-03-17）`，并把“当前推荐的下一步”从“进入 Gate G 验收”推进为“正式进入 Phase B 体验重构主线”；状态板中的 `Gate G` 也标记为已通过。

### 目的
- 为 `Ship / VFX` 治理阶段提供正式的收尾节点，避免 Phase A 明明已完成却长期停留在“待验收”状态。
- 让后续工作重心明确切回体验重构，而不是继续在已收口的问题上重复治理。

### 技术
- Gate checklist 逐条复核
- Unity 菜单入口盘点
- 文档状态板收口

---

---

## Docs: ImplementationLog 合并冲突按时间线收口 — 2026-03-17 15:25

### 修改文件
- `Docs/ImplementationLog/ImplementationLog.md`

### 内容
- 清理 `<<<<<<<`、`=======`、`>>>>>>>` 冲突标记，保留冲突两侧的全部日志内容。
- 将冲突块中的 `Ship VFX` 与 `StS2` 记录按实际时间重新并入统一时间线，避免 `2026-03-16` 与 `2026-03-17` 段落前后倒置。
- 同步检查尾部日志顺序，确保后续继续追加实现记录时不会再基于错位时间轴操作。

### 目的
- 恢复 `ImplementationLog.md` 作为单一时间序日志的可读性与可维护性。
- 避免后续查阅、追溯或继续追加日志时，因为 merge conflict 残留而误判开发顺序。

### 技术
- Markdown 冲突块手工合并
- 基于时间戳的日志重排
- 最小化文本结构修复

---

---

## Docs: ImplementationLog merge conflict 时间顺序收口 (2026-03-19 22:08)

**修改文件：**
- `Docs/ImplementationLog/ImplementationLog.md`

**内容：**
1. 清理本轮 `ImplementationLog.md` 中残留的两处 merge conflict，移除 `<<<<<<< / ======= / >>>>>>>` 标记。
2. 恢复被冲突挤散的日志头部与正文对应关系，补回 `Demon HR` 三回合 Demo 条目的标题与文件列表。
3. 将 `StS2 Mod`、`Handle With Panic`、`Demon HR` 三组记录按真实时间重新串回统一时间线，修正 `2026-03-18` 与 `2026-03-19` 段落交错覆盖的问题。
4. 额外通过 Git 历史反查补齐 `EventHook 并入统一协议 MVP (2026-03-18 13:10)` 的原始标题与时间，避免用错位标题覆盖 `StS2` 记录。

**目的：**
- 恢复 `ImplementationLog.md` 作为单一时间序真相源的可读性与可追溯性。
- 避免后续继续追加日志时基于错位段落操作，导致时间线进一步污染。

**技术：** Markdown 冲突块手工合并、基于时间戳的日志重排、Git 历史反查缺失标题、最小化结构修复。

---

## Level: 清理所有 legacy/obsolete 代码 — 2026-04-01 00:46

**修改文件：**
- `Assets/Scripts/Level/Data/RoomSO.cs`
- `Assets/Scripts/Level/Data/RoomType.cs`
- `Assets/Scripts/Level/Data/RoomPresetSO.cs`
- `Assets/Scripts/Level/Data/LevelScaffoldData.cs`
- `Assets/Scripts/Level/Room/Room.cs`
- `Assets/Scripts/Level/Room/Door.cs`
- `Assets/Scripts/Level/Room/DoorTransitionController.cs`
- `Assets/Scripts/Level/Room/RoomManager.cs`
- `Assets/Scripts/Level/Map/MapRoomData.cs`
- `Assets/Scripts/Level/Map/MapRoomWidget.cs`
- `Assets/Scripts/Level/Map/MinimapManager.cs`
- `Assets/Scripts/Level/Editor/LevelArchitect/BatchEditPanel.cs`
- `Assets/Scripts/Level/Editor/LevelArchitect/LevelArchitectWindow.cs`
- `Assets/Scripts/Level/Editor/LevelArchitect/LevelValidator.cs`
- `Assets/Scripts/Level/Editor/LevelArchitect/RoomFactory.cs`
- `Assets/Scripts/Level/Editor/LevelArchitect/PacingOverlayRenderer.cs`
- `Assets/Scripts/Level/Editor/LevelArchitect/BlockoutModeHandler.cs`
- `Assets/Scripts/Level/Editor/LevelArchitect/SceneScanner.cs`
- `Assets/Scripts/Level/Editor/LevelArchitect/ScaffoldSceneBinder.cs`
- `Assets/Scripts/Level/Editor/LevelArchitect/ScaffoldToWorldGraphBuilder.cs`
- `Assets/Scripts/Level/Editor/LevelArchitect/DoorWiringService.cs`
- `Assets/Scripts/Level/Editor/LevelArchitect/RoomBlockoutRenderer.cs`
- `Assets/Scripts/Level/Editor/LevelArchitect/ShebaSliceBuilder.cs`
- `Assets/Scripts/Level/Editor/ScaffoldToSceneGenerator.cs`
- `Assets/Scripts/UI/AreaTitleDisplay.cs`

**内容：**
1. **RoomSO.cs**：删除 `_useLegacyTypeMapping`、`UseLegacyTypeMapping`、`LegacyMappedNodeType`、`MapLegacyTypeToNodeType`、`_type`/`Type` 等所有 legacy 迁移字段和方法；`NodeType` 直接返回 `_nodeType`。
2. **RoomType.cs**：添加 `[System.Obsolete]` 标记，保留枚举供已有 SO 资产序列化兼容，但标记为废弃。
3. **Room.cs**：删除 `Type` 属性（返回 `RoomType`）、`LEGACY_SPAWN_POINTS_ROOT_NAME` 常量、legacy SpawnPoints 根节点查找分支。
4. **Door.cs**：删除 `_isLayerTransition` 字段、`IsLayerTransition` 属性、`EffectiveCeremony` 兼容属性；统一使用 `Ceremony`；更新 Tooltip 注释。
5. **MapRoomData.cs**：`Type` 字段（`RoomType`）改为 `NodeType`（`RoomNodeType`）。
6. **MapRoomWidget.cs**：颜色映射从 `RoomType` 改为 `RoomNodeType`。
7. **MinimapManager.cs**：`MapRoomData` 构建改用 `NodeType`；`GatherConnectionsFromDoors` 中 `IsLayerTransition` 改用 `door.Ceremony >= Layer`。
8. **RoomManager.cs**：`room.Type == RoomType.Arena/Boss` 改为 `room.NodeType == RoomNodeType.Resolution/Boss`；日志输出改用 `NodeType`。
9. **BatchEditPanel.cs**：删除所有 `RoomType` 相关字段、`ApplyBatchRoomType`、`MigrateRoomNodeTypesToExplicit` 等迁移方法；统一使用 `RoomNodeType`。
10. **LevelArchitectWindow.cs**：删除 Migrate 按钮、legacy 信息行；`GetRoomTypeColor/GetRoomTypeOutlineColor` 参数改为 `RoomNodeType`；`preset.RoomTypeValue` 改为 `preset.NodeTypeValue`。
11. **LevelValidator.cs**：删除 Rule 3.6 `ValidateRoomNodeTypeMigration` 方法及其调用；Rule 5 改用 `NodeType`；自动修复 RoomSO 时去掉 `_type`/`_useLegacyTypeMapping` 设置。
12. **RoomFactory.cs**：`GenerateRoomName` 改用 `NodeTypeValue`；`CreateRoomSOForPreset` 去掉 `_type`/`_useLegacyTypeMapping` 设置；`SaveRoomAsPreset` 改用 `room.NodeType`。
13. **RoomPresetSO.cs**：`_roomType`（`RoomType`）改为 `_nodeType`（`RoomNodeType`）；`RoomTypeValue` 改为 `NodeTypeValue`。
14. **LevelScaffoldData.cs**：`ScaffoldRoom._roomType` 改为 `_nodeType`，`RoomType` 改为 `NodeType`；`ScaffoldDoorConnection._isLayerTransition` 改为 `_ceremony`（`TransitionCeremony`），`IsLayerTransition` 改为 `Ceremony`。
15. **ScaffoldToSceneGenerator.cs**：`WireDoor` 参数从 `bool isLayerTransition` 改为 `TransitionCeremony ceremony`，设置 `_ceremony` 字段；`SetupArenaBossRoomCombat` 改用 `NodeType`；`SetupNormalRoomCombat` 改用 `NodeType.Transit`；`CreateEncounterSO` 参数改为 `RoomNodeType`；日志输出改用 `NodeType`。
16. **ScaffoldToWorldGraphBuilder.cs**：删除 `MapRoomTypeToNodeType` 方法；`BuildNodes` 直接用 `room.NodeType`；`BuildEdges` 中 `IsLayerTransition` 改用 `conn.Ceremony >= Layer`；`InferConnectionType` 中 `RoomType.Boss/Arena` 改为 `NodeType.Boss/Resolution`，`conn.IsLayerTransition` 改为 `conn.Ceremony >= Layer`。
17. **BlockoutModeHandler.cs**：`FindDefaultPreset` 改用 `NodeTypeValue == Transit`；`CreateBuiltInPresets` 改用 `RoomNodeType`；`CreatePresetIfMissing` 参数改为 `RoomNodeType`，设置 `_nodeType` 字段。
18. **ShebaSliceBuilder.cs**：删除 `RoomDef.LegacyType` 字段；`CreateRoomSO` 去掉 `_type`/`_useLegacyTypeMapping` 设置，直接用 `_nodeType`。
19. **DoorWiringService.cs**：`_isLayerTransition` 改为设置 `_ceremony`（层间过渡时设为 `Layer`，否则 `Standard`）。
20. **RoomBlockoutRenderer.cs**：`door.IsLayerTransition` 改为 `door.Ceremony >= Layer`；`room.Type` 改为 `room.NodeType`。
21. **SceneScanner.cs**：`room.Type` 改为 `room.NodeType`；`_type`/`_roomType` 改为 `_nodeType`；`ScaffoldDoorConnection.IsLayerTransition` 改为 `Ceremony`。
22. **ScaffoldSceneBinder.cs**：`room.Type` 改为 `room.NodeType`，`_roomType` 改为 `_nodeType`。
23. **PacingOverlayRenderer.cs**：`door.IsLayerTransition` 改为 `door.Ceremony >= Layer`；`room.Type` 改为 `room.NodeType`。
24. **DoorTransitionController.cs**：`door.EffectiveCeremony` 改为 `door.Ceremony`。
25. **AreaTitleDisplay.cs**：`room.Type == RoomType.Corridor` 改为 `room.NodeType == RoomNodeType.Transit`。

**目的：**
- 彻底清除 `RoomType` 枚举的运行时使用，统一使用 `RoomNodeType`（世界图谱语法节点类型）。
- 彻底清除 `Door._isLayerTransition` 布尔值，统一使用 `TransitionCeremony` 枚举。
- 消除所有 legacy 迁移路径（双轨映射、兼容属性、迁移方法），让代码路径单一清晰。
- 编译验证：0 个错误，62 个警告（全部为 Unity 6 API 过时警告，与本次改动无关）。

**技术：** 全局 grep 扫描 → 逐文件 multi_replace → dotnet build 编译验证闭环。


---

## 关卡模块代码审查修复（全部 10 项）— 2026-04-01 01:19

### 修改文件
- Assets/Scripts/Level/Room/RoomFlagRegistry.cs — C1：修复 flag key 解析歧义 Bug
- Assets/Scripts/Level/DynamicWorld/BiomeTrigger.cs — M1：移除 FindAnyObjectByType
- Assets/Scripts/Level/DynamicWorld/AmbienceController.cs — M1/m3：添加 PostProcessVolume 属性；移除字符串匹配，改为数据驱动
- Assets/Scripts/Level/Data/WorldPhaseSO.cs — m3：添加 VignetteIntensityOverride 字段
- Assets/Scripts/Level/Room/RoomManager.cs — M2：添加 FindDoorByGateID 方法
- Assets/Scripts/Level/Progression/WorldProgressManager.cs — M2：实现门解锁功能
- Assets/Scripts/Level/Room/ArenaController.cs — M3/m4：SpawnReward 改用 PoolManager；HandleWavesCleared 调用 Dispose
- Assets/Scripts/Level/GameFlow/GameFlowManager.cs — M4：移除双轨 Fade fallback，强制依赖 DoorTransitionController
- Assets/Scripts/Ship/Combat/ShipHealth.cs — M5：添加 SetHP 方法
- Assets/Scripts/Level/SaveBridge.cs — M5：DistributePlayerState 调用 SetHP 恢复存档 HP
- Assets/Scripts/Level/Room/Door.cs — m2：添加 ResetGlobalTransitionCooldown 静态方法
- Assets/Scripts/Level/Room/DoorTransitionController.cs — m2：OnDestroy 中重置静态冷却字段
- Assets/Scripts/Level/Room/WaveSpawnStrategy.cs — m4：实现 IDisposable，添加 Dispose 方法
- Assets/Scripts/Level/Pickup/PickupBase.cs — m6：PlayConsumeAnimation 改用 await Tween.ToUniTask

### 内容

修复关卡模块代码审查发现的全部 10 项问题（1 个 Critical、5 个 Major、4 个 Minor）。

**C1** RoomFlagRegistry flag key 分隔符从 _ 改为 ::，防止 roomID 含下划线时存档数据损坏。

**M1** BiomeTrigger 移除 FindAnyObjectByType，改为通过 ServiceLocator.Get<AmbienceController>() 获取 Volume；AmbienceController 添加 PostProcessVolume 属性。

**M2** RoomManager 添加 FindDoorByGateID 方法；WorldProgressManager.AdvanceToStage 实现真正的门解锁逻辑，UnlockDoorIDs 格式为 roomID::gateID。

**M3** ArenaController.SpawnReward 改用 poolManager.GetPool(prefab).Get(pos, rot)，PoolManager 不存在时 LogError。

**M4** GameFlowManager 移除 _fadeImage 字段、ResolveFadeImage 方法及所有 fallback Fade 路径，强制依赖 DoorTransitionController，缺失时 LogError。

**M5** ShipHealth 添加 SetHP(float hp) 方法；SaveBridge.DistributePlayerState 调用 SetHP 恢复存档 HP。

**m2** Door 添加 ResetGlobalTransitionCooldown 静态方法；DoorTransitionController.OnDestroy 调用以防跨场景污染。

**m3** WorldPhaseSO 添加 VignetteIntensityOverride 字段；AmbienceController 移除字符串匹配，改为读取 SO 字段，移除 _stormVignetteIntensity/_radiationVignetteIntensity。

**m4** WaveSpawnStrategy 实现 System.IDisposable；ArenaController.HandleWavesCleared 调用 Dispose。

**m6** PickupBase.PlayConsumeAnimation 改用 await Tween.ToUniTask，移除 fire-and-forget + 手动 Delay 双轨写法。

### 目的
消除存档数据损坏风险、功能缺失（门解锁/HP恢复）、架构违规（FindAnyObjectByType/Instantiate/双轨Fade）和潜在内存泄漏。

### 技术
代码审查 → 逐文件 multi_replace/replace_in_file → dotnet build 编译验证（0 个错误）。

---

## 关卡模块全面代码审计与清理 — 2026-04-01 09:44

### 修改文件
- `Assets/Scripts/Level/WorldClock/WorldClock.cs`
- `Assets/Scripts/Level/DynamicWorld/ScheduledBehaviour.cs`
- `Assets/Scripts/Level/DynamicWorld/WorldEventTrigger.cs`
- `Assets/Scripts/Level/Room/DoorTransitionController.cs`
- `Assets/Scripts/Level/Data/RoomNodeType.cs`
- `Assets/Scripts/Level/Data/RoomType.cs` ← **已删除**
- `Assets/Scripts/Level/Editor/LevelArchitect/ScaffoldToWorldGraphBuilder.cs`
- `Assets/Scripts/Level/Room/RoomManager.cs`
- `Assets/Scripts/Level/Map/MinimapManager.cs`
- `Assets/Scripts/Level/Camera/CameraDirector.cs`
- `Assets/Scripts/Level/Camera/CameraTrigger.cs`
- `Assets/Scripts/Level/Hazard/ContactHazard.cs`
- `Assets/Scripts/Level/Hazard/DamageZone.cs`
- `Assets/Scripts/Level/Hazard/TimedHazard.cs`
- `Assets/Scripts/Level/Editor/LevelArchitect/LevelValidator.cs`
- `Assets/Scripts/Level/Editor/LevelArchitect/RoomBlockoutRenderer.cs`
- `Assets/Scripts/Level/Editor/LevelArchitect/DoorWiringService.cs`
- `Assets/Scripts/Level/Editor/LevelArchitect/ScaffoldSceneBinder.cs`
- `Assets/Scripts/Level/Editor/LevelArchitect/SceneScanner.cs`
- `Assets/Scripts/Level/Editor/LevelArchitect/LevelArchitectWindow.cs`
- `Assets/Scripts/Level/Editor/LevelArchitect/BlockoutModeHandler.cs`
- `ProjectArk.Level.csproj`

### 内容
1. **删除 `RoomType.cs`**：已标记 `[System.Obsolete]` 的枚举，Level 模块内已无任何代码引用，彻底移除。同步更新 `RoomNodeType.cs` 注释和 `ScaffoldToWorldGraphBuilder.cs` 注释。
2. **删除死代码**：`DoorTransitionController.FindSpawnPointForGate()` 中一段只有注释没有实际逻辑的空 `foreach` 分支。
3. **删除未使用变量**：`WorldClock.Update()` 中声明但从未读取的 `previousNormalized` 局部变量。
4. **删除未使用字段**：`ScheduledBehaviour` 中声明但从未读取的 `_initialized` 字段。
5. **简化空方法**：`WorldEventTrigger.SaveTriggeredState()` 调用了一个完全空的 `SetFlagInSaveData()` 方法（注释说"will be picked up"但实际无任何操作），删除该空方法，简化调用链。
6. **修复过时 API（16 处）**：将 Level 模块所有 `FindObjectsByType<T>(FindObjectsSortMode.None)` 替换为 `FindObjectsByType<T>()`（Unity 6 新 API）。
7. **修复过时 API（1 处）**：`CameraDirector` 中 `FindFirstObjectByType` → `FindAnyObjectByType`。
8. **修复过时 API（6 处）**：`ContactHazard`、`DamageZone`、`TimedHazard` 中 `GetInstanceID()` → 改用 `Dictionary<GameObject, float>` 作为 key，完全避免 ID 问题。
9. **消除 CS4014 警告**：`CameraDirector` 中 `Tween.Custom` 返回值用 `_ =` 丢弃。

### 目的
精简逻辑、删除 obsolete 功能、消除过时 API 警告，提升代码可读性和可维护性。

### 技术
全面代码审计 → grep 搜索定位问题 → replace_in_file/multi_replace/terminal sed 批量修复 → dotnet build 验证（0 错误，警告从 41 降至 20）。

---

## 删除 WorldGraph Editor — 工具链精简 2026-04-01 12:57

### 修改文件

**删除文件：**
- `Assets/Scripts/Level/Editor/LevelArchitect/WorldGraphEditorWindow.cs`
- `Assets/Scripts/Level/Editor/LevelArchitect/WorldGraphView.cs`
- `Assets/Scripts/Level/Editor/LevelArchitect/WorldGraphSOEditor.cs`
- `Assets/Scripts/Level/Editor/LevelArchitect/ScaffoldToWorldGraphBuilder.cs`
- `Assets/Scripts/Level/Editor/LevelArchitect/RoomGraphNode.cs`
- `Assets/Scripts/Level/Editor/LevelArchitect/ConnectionGraphEdge.cs`
- `Assets/Scripts/Level/Editor/LevelArchitect/SpacePanManipulator.cs`

**修改文件：**
- `Assets/Scripts/Level/Editor/LevelArchitect/LevelArchitectWindow.cs`（新增 `GetConnectionTypeColor` 静态方法）
- `Assets/Scripts/Level/Editor/LevelArchitect/PacingOverlayRenderer.cs`（将 `ConnectionGraphEdge.GetConnectionTypeColor` 引用替换为 `LevelArchitectWindow.GetConnectionTypeColor`）

### 内容
删除 WorldGraph Editor（节点图可视化编辑器）的全部 Editor UI 代码，保留 `WorldGraphSO` 数据类及其在运行时的使用（`DoorTransitionController`、`MinimapManager`）。将 `ConnectionGraphEdge.GetConnectionTypeColor()` 静态方法迁移到 `LevelArchitectWindow`，确保 `PacingOverlayRenderer` 的连接类型颜色叠加层功能不受影响。

### 目的
精简关卡编辑工具链，消除 WorldGraph Editor 与 Level Architect 之间的职责重叠。决策依据：
1. `WorldGraphSO` 的节点图可视化功能已被 `LevelDesigner.html`（浏览器工具）覆盖，且后者功能更强（含张力曲线、Act 分组、叙事设计等）
2. Level Architect 直接操作场景 GameObject，是游戏的真相源；WorldGraph Editor 操作数据资产，需要额外同步步骤，是维护负担
3. 符合 Implement_rules.md 原则：同一类任务只保留一个官方入口

### 技术
grep 搜索确认依赖关系 → 迁移 `GetConnectionTypeColor` 静态方法 → 删除 7 个 Editor 文件 → dotnet build 验证（0 错误）。

---

## 关卡工作流重构 Phase 1：清理冗余工具链 — 2026-04-01 13:13

### 删除文件
- `Assets/Scripts/Level/Editor/ScaffoldToSceneGenerator.cs` — 1287 行怪物文件，与 ShebaSliceBuilder 职责完全重叠，无外部引用，直接删除
- `Assets/Scripts/Level/Editor/LevelArchitect/ShebaSliceBuilder.cs` — 硬编码示巴星 7 房间的一次性脚本，功能已被通用化的 LevelSliceBuilder 替代

### 新建文件
- `Assets/Scripts/Level/Editor/LevelArchitect/LevelSliceBuilder.cs` — 通用关卡切片构建器，直接读取 LevelDesigner.html 导出的 JSON，生成完整场景骨架

### 修改文件
- `Assets/Scripts/Level/Editor/LevelArchitect/BlockoutModeHandler.cs` — 删除 `CreateBuiltInPresets()` 等方法（迁移到 RoomFactory），只保留 SceneView 输入处理逻辑
- `Assets/Scripts/Level/Editor/LevelArchitect/RoomFactory.cs` — 新增 `CreateBuiltInPresets()` 和 `CreatePresetIfMissing()`，成为预设资产创建的唯一权威入口
- `Assets/Scripts/Level/Editor/LevelArchitect/ScaffoldSceneBinder.cs` — 去掉双向同步，改为单向 Scene→Scaffold。场景是真相源，ScaffoldData 是快照目标
- `Assets/Scripts/Level/Editor/LevelArchitect/LevelArchitectWindow.cs` — `BlockoutModeHandler.CreateBuiltInPresets()` 改为 `RoomFactory.CreateBuiltInPresets()`
- `ProjectArk.Level.Editor.csproj` — 删除已删除文件的 Compile 引用，添加 LevelSliceBuilder.cs 引用

### 内容

**Phase 1A：删除 ScaffoldToSceneGenerator.cs**
确认无任何外部引用后直接删除，该文件与 ShebaSliceBuilder 职责完全重叠。

**Phase 1B+2B 合并：LevelSliceBuilder（通用 JSON 导入器）**
- 菜单入口：`ProjectArk/Level/Import from LevelDesigner JSON...`
- 解析 LevelDesigner.html 导出 JSON，生成 RoomSO 资产 + Room GameObject + Door 连接
- HTML connectionType 映射到项目 ConnectionType 枚举；Y 轴翻转处理；完整 Undo 支持

**Phase 1C：BlockoutModeHandler 职责清理**
CreateBuiltInPresets 系列方法迁移到 RoomFactory，BlockoutModeHandler 只保留 SceneView 输入处理。

**Phase 1D：ScaffoldSceneBinder 单向化**
去掉 Scaffold→Scene 自动同步，保留 Scene→Scaffold 实时检测。

### 目的
方案 B 关卡工作流重构 Phase 1：清理工具链冗余和职责混乱，为 Phase 2（LevelArchitectWindow 三 Tab 重构）奠定基础。

### 技术
JsonUtility.FromJson 零依赖解析 → 复用 ShebaSliceBuilder 成熟的场景生成模式 → dotnet build 验证：0 错误，0 警告。

---

## 关卡工作流重构 Phase 2：打通设计层→实现层 — 2026-04-01 14:40

### 修改文件
- `Tools/LevelDesigner.html` — 新增关卡名称输入框 + 一键导出文件按钮
- `Assets/Scripts/Level/Editor/LevelArchitect/LevelArchitectWindow.cs` — 重构为三 Tab 布局（Design / Build / Validate）

### 内容

**Phase 2A：LevelDesigner.html 改进**
- 右侧面板顶部新增"关卡名称"输入框（`#level-name-input`），默认值 `NewLevel`
- 工具栏新增 **💾 导出文件** 按钮（绿色高亮），一键直接下载 JSON，无需弹出 Modal
- `getExportData()` 改为读取 `getLevelName()` 函数，不再硬编码 `'New Level'`
- `downloadJson()` 改为使用 `getLevelName()` 作为文件名（`{levelName}.json`）
- 新增 `saveJsonToFile()` 函数：直接生成 JSON 并触发浏览器下载，带 1.5s 视觉反馈

**Phase 2C：LevelArchitectWindow 三 Tab 重构**
- 新增 `Tab` 枚举（Design / Build / Validate）和 `_activeTab` 序列化字段
- `OnGUI()` 改为 Tab 路由：`DrawDesignTab()` / `DrawBuildTab()` / `DrawValidateTab()`
- **Design Tab**：🌐 Open LevelDesigner.html + 📂 Import JSON + 6 步工作流说明
- **Build Tab**：Mode Selector + Overlay Toggles + Floor Level + Scan/Presets/QuickPlay
- **Validate Tab**：完整验证结果列表 + Fix 按钮 + Auto-Fix All
- SceneView 侧边栏 Validate 部分改为简洁摘要（"see Validate tab"）

### 目的
方案 B 关卡工作流重构 Phase 2：打通 LevelDesigner.html（设计层）→ LevelSliceBuilder（导入层）→ LevelArchitectWindow（实现层）的完整链路。

### 技术
HTML 使用 `URL.createObjectURL` + `<a>` 触发浏览器下载；Unity 使用 `Application.OpenURL("file://...")` 打开本地 HTML；`GUILayout.Toolbar` 实现 Tab 切换；dotnet build 验证：0 错误，0 警告。

---

## 关卡工作流审计与 Bug 修复 — 2026-04-01 14:51

### 修改文件
- `Assets/Scripts/Level/Editor/LevelArchitect/ScaffoldSceneBinder.cs` — 修复 RegisterRoomToScaffold 的 tempSO 静默失败 bug
- `Assets/Scripts/Level/Editor/LevelArchitect/LevelArchitectWindow.cs` — 修复 DrawModeButton 无效 style 赋值、DrawDesignTab 未使用变量、缩进错误（×2）
- `Assets/Scripts/Level/Editor/LevelArchitect/ShebaSliceBuilder.cs` — 删除（仅含注释的空文件）

### 内容

对整套关卡工作流进行全面审计，检查代码精简度、数据字段对应关系、以及全流程可跑通性。

**数据层审计（全部通过）**：
- HTML `getExportData()` → `LevelJson` 字段映射完全正确
- `RoomJson.position/size` 格式（`[x/GRID, y/GRID]`）与 `JsonPosToWorld/JsonSizeToWorld` 完全匹配，Y 轴翻转逻辑正确
- `RoomSO` 字段名（`_roomID/_displayName/_nodeType/_floorLevel/_encounter`）全部正确
- `Door` 字段名（`_gateID/_connectionType/_initialState/_ceremony/_targetRoom/_targetSpawnPoint`）全部正确
- `EncounterSO._mode` 字段名正确
- `DoorTransitionController` 支持 `_targetRoom` 直接引用 fallback，与 LevelSliceBuilder 接线方式兼容

**修复的 Bug**：

1. **严重 — ScaffoldSceneBinder.RegisterRoomToScaffold tempSO 静默失败**：原实现创建临时 `LevelScaffoldData` tempSO，通过 `SerializedObject` 写字段后，将 `tempSO.Rooms[0]` 复制给真实 `_scaffoldData`。但 tempSO 从未被 `AssetDatabase.CreateAsset`，`SerializedObject` 操作不会持久化到 `ScaffoldRoom` 对象，导致所有字段写入静默失败（roomID/displayName/nodeType/position/size/roomSO 全部为默认值）。修复：直接在真实 `_scaffoldData` 上操作 `SerializedObject`，先 `AddRoom` 再通过 `serialized.FindProperty("_rooms").GetArrayElementAtIndex(lastIndex)` 写字段。

2. **中 — DrawModeButton style 变量无效赋值**：`var style = isActive ? "Button" : "Button"` 两边完全相同，style 从未被使用。删除无效变量。

3. **中 — DrawDesignTab 未使用变量**：`var style = new GUIStyle(EditorStyles.helpBox)` 声明后从未使用。删除。

4. **轻微 — RoomFactory.CreateBuiltInPresets() 缩进丢失（×2 处）**：`DrawBuildActions` 和 `DrawSidePanelContent` 中各有一处缩进丢失。补回正确缩进。

5. **轻微 — ShebaSliceBuilder.cs 空文件**：仅含 3 行注释，无实际代码。删除文件及 .meta。

### 目的
确保关卡工作流 HTML→JSON→Unity 导入→场景生成 全链路数据对得上、代码无隐患、可全流程跑通。

### 技术
dotnet build 验证：0 错误，0 警告。

---

## 关卡工作流文档职责重组 — 2026-04-01 21:52

### 修改文件
- `Docs/2_Design/Level/Level_WorkflowSpec.md` — 重构为操作手册，明确 authority 边界并修正过期内容

### 内容

将 `Level_WorkflowSpec.md` 从“混合型权威文档”重构为**关卡搭建操作手册**，不再在正文里承担运行时架构、脚本盘点、资产目录快照等高漂移职责，重点收束为“怎么搭、怎么验、谁说了算”。

**本次重组的关键调整：**
- 新增 `WorkflowSpec` 与 `Level_CanonicalSpec` 的职责分工，明确 Scene / Door / SO / JSON / `LevelScaffoldData` 的 authority 边界
- 将主流程重组为：**入口路径 A/B → 结构搭建 Pass → 语义标注 Pass → Overlay 检查 → Validate → Quick Play → 完整 Play Mode 验收**
- 修正 `LevelSliceBuilder` 的真实 JSON schema：改为顶层 `connections[]`、房间 `position/size` 数组、连接方向 `fromDir/toDir = east/west/north/south`
- 修正当前官方最小房间结构：以 `Navigation / Elements / Encounters / Hazards / Decoration / Triggers + CameraConfiner` 为准，不再把历史 `Tilemaps / ActivationGroups` 误写为当前必需根节点
- 明确 `Quick Play` 的定位是**结构 smoke test**，不再将其表述为完整切片验收
- 明确 `LevelScaffoldData` 当前是 **Scene → Scaffold** 单向同步的编辑期快照/导出层，不是运行时权威，也不是 JSON 导入必经层
- 精简正文中高频过期的信息，把文档重心收回到实际 authoring 流程与验证边界

### 目的
让 `Level_WorkflowSpec.md` 真正成为当前关卡生产流程的可执行手册，减少“工作流说明、架构规范、资产盘点”混写导致的认知噪音和文档漂移，缩短新切片从规划到落地的迭代回路。

### 技术
以现行工具链实现为锚点，对照 `LevelArchitectWindow`、`LevelSliceBuilder`、`LevelValidator`、`RoomFactory`、`ScaffoldSceneBinder` 与 `Level_CanonicalSpec.md` 进行文档职责收口与字段对齐。

---

## 关卡 CanonicalSpec 职责收口与现役口径同步 — 2026-04-01 22:00

### 修改文件
- `Docs/2_Design/Level/Level_CanonicalSpec.md` — 收口与 `Level_WorkflowSpec.md` 的职责边界，修正残留的旧迁移口径与文档路径

### 内容

继续对关卡文档体系做第二轮治理，重点不是重写 `CanonicalSpec`，而是把**会直接误导执行**的旧口径清掉，让它和新版 `WorkflowSpec` 形成“一个管架构、一个管落地”的稳定分工。

**本次同步的关键调整：**
- 修正 `CanonicalSpec` 中的文档路径与关系表，明确 `Level_WorkflowSpec.md` 是执行侧手册，冲突时以 `CanonicalSpec` 为准
- 修正 `GateID` 章节，把 `LevelValidator` 尚未实现的重复校验从“既成事实”改为“当前空缺 + 需要人工自查”
- 将 `Room` 标准层级改写为与 `RoomFactory`、`Room.cs`、`LevelValidator` 一致的现役口径：六个标准根节点 + `Navigation/Doors`、`Navigation/SpawnPoints`、`Encounters/SpawnPoints`、`CameraConfiner`
- 更新 Editor 工具矩阵，去掉已过期的 `ScaffoldToSceneGenerator` / `RoomNodeMigrator` / `WorldGraphEditor` 口径，改为 `LevelSliceBuilder`、`ScaffoldSceneBinder` 与审计类工具的现役/后续分工
- 删除 `WorldGraphSO fallback` 残留迁移说明，明确 Door 目标引用就是当前运行时传送链真相源，`MinimapManager` 固定从 Door 推导邻接关系

### 目的
让 `Level_CanonicalSpec.md` 不再同时混杂“已完成迁移”和“已废弃历史口径”，避免新同学或未来自己在看文档时误以为当前仍存在双轨拓扑、旧 `RoomType` 迁移链或已实现的 GateID 审计能力。

### 技术
以现行代码为锚点，对照 `RoomFactory`、`Room.cs`、`LevelValidator`、`LevelSliceBuilder`、`MinimapManager`、`RoomManager` 与 `WorldProgressManager` 做文档事实校准；编辑过程中识别并规避了同文件并行替换导致的覆盖风险，改为串行收口。

---

## Level 验收清单收口为现役口径 — 2026-04-09 10:19

### 修改文件
- `Docs/6_Diagnostics/VerificationChecklist.md` — 重写为与最新 `ImplementationLog` 对齐的现役 Level 验收清单

### 内容
- 将旧的“Phase 3-6 + Ship Feel”混合型验收文档重写为 **Level 模块现役验收清单**，不再沿用历史阶段工具存在性检查，而改为围绕当前工作流与实际结果进行验收。
- 明确当前统一流程为：`结构搭建 / 场景落地 → 语义标注 → Overlay 检查 → Validate → Quick Play → 完整 Play Mode 验收`。
- 从清单中移除并降级为“已退役口径”的旧项：`Scaffold Sheba Level`、`Phase 6: Create World Clock Assets`、`Phase 6: Build Scene Managers`、`Phase 6: Setup All`、`ShebaSliceBuilder`、`Phase6AssetCreator`。
- 新增现役验收维度：`Level Architect` 三 Tab、`LevelSliceBuilder` 导入路径、`LevelValidator` 新增的元素/挂点/ActivationGroup 护栏、`Quick Play` 的 smoke test 边界，以及完整 Play Mode 下的战斗、地图、存档、世界阶段与动态世界联动。
- 补入当前仍需人工自查的项目，特别是 `GateID` 重复、`RoomNodeType` / `ConnectionType` / `TransitionCeremony` 语义正确性，避免误以为 `LevelValidator` 已覆盖全部检查。

### 目的
- 让团队之后做 Level 验收时，依据的是**当前现役工具链和代码事实**，而不是早期一次性工具留下的过期清单。
- 把“工具存在性验收”改成“结构、护栏、运行时闭环与人工试玩”的结果导向验收，和 2026-04-01 收口后的 Level 工作流保持一致。

### 技术
- 以 `ImplementationLog` 中 2026-04-01 的 Level 收口记录为唯一时间线依据，逐条对照现役入口、退役工具和当前验证边界。
- 文档重写策略：保留验收文件路径不变，直接把旧 checklist 改写为现役版，避免后续继续维护两套口径。

---

## Unity `ProjectArk` 旧菜单清理 — 2026-04-09 10:35

### 修改文件
- `Assets/Scripts/Combat/Editor/Batch5AssetCreator.cs` — 移除 `ProjectArk/Create Batch 5 Test Assets` 菜单暴露，并将文件头注释改为 legacy 隐藏工具口径
- `Assets/Scripts/Combat/Editor/ShebaAssetCreator.cs` — 移除 `ProjectArk/Create Sheba Star Chart Assets` 菜单暴露，并将文件头注释改为 legacy 隐藏工具口径
- `Assets/Scripts/SpaceLife/Editor/SpaceLifeMenuItems.cs` — 删除 `Space Life/Setup/Phase 1-5` 与 `All Phases` 的分阶段 Setup 菜单方法，保留 `Setup Wizard` 作为统一设置入口，以及 `Create/Data` 快捷工具

### 内容
- 清理掉两类已经不该继续占用 `ProjectArk` 主菜单的入口：
  - 明确属于旧批次/一次性内容生成的 `Batch5AssetCreator`、`ShebaAssetCreator`
  - 与 `SpaceLifeSetupWindow` 的统一 Setup 入口重复的 `Space Life` 分阶段 Setup 菜单
- 保留仍有现役职责的菜单：`Level Architect` / `LevelSliceBuilder`、`Ship/VFX Authority`、`EnemyAssetCreator`、`BestiaryImporter`、`ShapeContractValidator`、`UICanvasBuilder`、`Space Life/Setup Wizard`
- 额外同步修正文档注释，避免代码文件头继续声称旧菜单仍然存在

### 目的
- 收窄 Unity 顶层 `ProjectArk` 菜单的噪音，把当前团队真正还在使用的 authority / importer / validator / wizard 留在台前
- 避免成员继续误触旧批次测试工具或被重复的 `Space Life Setup` 入口干扰

### 技术
- 采用“**退菜单，不强删实现**”策略：优先移除 `MenuItem` 暴露和重复菜单方法，保留旧实现类作为隐藏辅助代码，降低误删风险
- 依据 `ImplementationLog` 里对 `Level` 工具链收口、`SpaceLife` 统一 Setup 入口和旧批次工具历史定位来做边界判断，而不是只按文件名猜测
- 变更后执行 `dotnet build Project-Ark.slnx` 验证，结果为 **0 error**（存在既有 warning，但本次未引入新编译错误）

---

## Unity `ProjectArk` 菜单第二轮瘦身 — 2026-04-09 11:25

### 修改文件
- `Assets/Scripts/Ship/Editor/ShipPhysicsDebugWindow.cs` — 移除 `ProjectArk/Ship/Physics Debug` 菜单暴露，并把文件头注释改为 legacy 隐藏调试窗口口径
- `Assets/Scripts/Ship/Editor/ShipGlowMaterialCreator.cs` — 移除 `ProjectArk/Ship/Create Ship Glow Material` 菜单暴露，并把文件头注释改为隐藏辅助工具口径
- `Assets/Scripts/Ship/Editor/ShipPrefabRebuilder.cs` — 更新缺失 `ShipGlowMaterial` 时的提示文案，避免继续引用已删除的旧菜单名
- `Assets/Scripts/Level/Editor/LevelArchitect/LevelSliceBuilder.cs` — 移除 `ProjectArk/Level/Import from LevelDesigner JSON...` 顶层重复菜单，并把文件头工作流说明改为通过 `LevelArchitectWindow` 进入

### 内容
- 第二轮继续收窄 `ProjectArk` 主菜单，退掉 3 个已经不适合常驻的入口：
  - `Ship/Physics Debug`：纯 Play Mode 调试窗口
  - `Ship/Create Ship Glow Material`：已降级为 `ShipPrefabRebuilder` 内部可调用的辅助创建器
  - `Level/Import from LevelDesigner JSON...`：功能仍保留，但统一收口到 `Level Architect` 的 Design 页按钮进入
- 同步修正 `ShipPrefabRebuilder`、`ShipPhysicsDebugWindow`、`ShipGlowMaterialCreator`、`LevelSliceBuilder` 的注释与提示文案，避免代码层继续传播旧菜单路径
- 复查脚本定义后，以上 3 个旧菜单路径在 `Assets/Scripts` 中已 **0 命中**

### 目的
- 继续把 Unity 顶层 `ProjectArk` 菜单收敛到 authority / validator / importer / wizard 这类现役入口
- 减少调试型、重复型、内部 helper 型菜单对日常工作流的干扰，降低误触成本

### 技术
- 延续第一轮的“**退菜单，不强删实现**”策略：移除 `MenuItem` 暴露，但保留 `ShipPhysicsDebugWindow.Open()`、`ShipGlowMaterialCreator.CreateOrGet()`、`LevelSliceBuilder.ImportFromJson()` 等实现供内部调用
- `LevelSliceBuilder` 的 JSON 导入能力未删除，只是撤掉与 `Level Architect` 重复的顶层快捷入口，保持现役 `Level` 工作流单入口化
- 变更后执行 `dotnet build Project-Ark.slnx`，结果为 **0 error / 43 warnings**；同时对本轮改动文件执行 lints 检查，未引入新问题

---

## Unity `ProjectArk` 菜单第三轮瘦身（Space Life Create 收口）— 2026-04-09 14:58

### 修改文件
- `Assets/Scripts/SpaceLife/Editor/SpaceLifeMenuItems.cs` — 移除剩余 `ProjectArk/Space Life/Create/*` 菜单暴露，并补回该类的 legacy/helper 职责说明
- `Assets/Scripts/SpaceLife/Editor/SpaceLifeSetupWindow.cs` — 保持 `ProjectArk/Space Life/Setup Wizard` 作为 Space Life 场景搭建主入口的说明口径

### 内容
- 清理 `SpaceLifeMenuItems.cs` 中剩余的场景 bootstrap / one-off 创建菜单暴露，包括 `SpaceLifeManager`、`SpaceLifeInputHandler`、`Player Controller`、`RelationshipManager`、`GiftInventory`、`Create NPC`、`Create Room`、`Create Door`、`Create Interactable Object`、`Create Dialogue UI`、`Create Gift UI`、`Create Minimap UI` 等 `Create/*` 入口。
- 保留这些静态方法本身，继续作为 `SpaceLifeSetupWindow` 等正式入口内部可调用的 helper；不把一次性创建能力硬删出代码，降低误删风险。
- 复查 `Assets/Scripts/SpaceLife` 后，`ProjectArk/Space Life/Create/*` 搜索结果已为 **0**；当前只保留 `ProjectArk/Space Life/Setup Wizard` 与 `ProjectArk/Space Life/Data/*` 这两类现役入口。
- 对 `SpaceLifeMenuItems.cs` 与 `SpaceLifeSetupWindow.cs` 执行 lints 检查，结果均为 **0**；再执行 `dotnet build Project-Ark.slnx`，结果为 **0 error / 33 warnings**。

### 目的
- 继续把 Unity 顶层 `ProjectArk` 菜单收口为 authority / wizard / data 这类现役入口，避免 `Space Life` 的场景搭建再次分裂回散装 `Create/*` 工作流。
- 明确 `SpaceLifeSetupWindow` 才是 Space Life 场景搭建与修复的单一主入口，减少新成员误触一次性 bootstrap 菜单的概率。

### 技术
- 延续“**退菜单，不强删实现**”策略：仅移除 `MenuItem` attribute，保留 `SpaceLifeMenuItems` 中的静态 helper 供 `SpaceLifeSetupWindow` 和未来内部工具复用。
- 通过全文搜索验证菜单路径零残留，再用 lints + `dotnet build Project-Ark.slnx` 做代码层闭环校验，确保这轮收口只影响菜单暴露面，不影响已有实现链路。

---

## LevelDesigner.html 全面审查 + 拖拽修复 + 数据对齐收口 — 2026-04-09 15:32

### 修改文件
- `Tools/LevelDesigner.html` — 审查并修复 HTML5 拖拽主链、统一连接类型 canonical 值、收口连线渲染与导入/导出口径

### 内容
- 全面审查 `LevelDesigner.html` 与 Unity 侧 `LevelSliceBuilder.cs` / `Level_WorkflowSpec.md` 的流程契约，确认当前 Unity 导入主链消费的是 `levelName`、`rooms[]`、`connections[]`，并继续把 `Door` 拓扑作为场景 authority。
- 修复房间/元素从左侧预设拖到画布的主链：新增统一的 `setDragPayload()` / `getDragPayload()`，把拖拽数据同时写入 `text/plain` 与 `application/x-projectark-leveldesigner`，并保留原有 `roomType` / `elementType` / `customLabel` / `customColor` 字段，降低 Safari / WebKit 对自定义 MIME 类型的兼容风险。
- 收口 `dragover` 处理为 `allowCanvasDrop()`，显式设置 `dropEffect = 'copy'`；`drop` 事件改为统一解析标准 payload，而不是散落读取多个 `dataTransfer.getData(...)` 字段。
- 审查后确认页面内部 `createRoom()` 链路本身一直可用；问题集中在 HTML5 DnD 数据交接层，而不是房间创建或渲染链路。
- 把连接类型内部状态与导出 JSON 统一收口到 Unity canonical 值：新增 `normalizeConnectionType()`，将 `normal / one_way → progression`，`tidal → scheduled`，`locked / secret → ability`；新建连接默认值改为 `progression`。
- 连接属性面板切回 canonical 下拉值（`progression / return / ability / challenge / identity / scheduled`），导入旧 JSON 时也会自动做 alias 归一化；连线颜色改为直接按 `CONN_TYPE_CONFIG` 设置，不再依赖旧 alias CSS class。
- 回归验证：lints 为 **0**；浏览器中脚本 helper 已实际生效；用同一个 `DataTransfer` 跑完整 `dragstart → drop` 可成功创建房间，并且导出 JSON 中新增 `rooms[]` 条目，`normalizeConnectionType('normal') / ('tidal')` 分别返回 `progression / scheduled`。

### 目的
- 修复 `LevelDesigner.html` 当前最影响使用的房间拖拽问题，让“左侧预设 → 画布放置”的最小主链重新稳定可用。
- 把 HTML 工具的连接类型状态、导出数据和 Unity 导入契约重新收口到同一套 canonical 口径，减少“能导入但不是规范值”的长期漂移。
- 明确本轮审查结论：`elements` / `doorLinks` 仍主要用于 HTML 规划与归档，Unity 当前现役导入主链仍以 `rooms[] + connections[]` 为核心消费数据。

### 技术
- 前端拖拽兼容治理：保留原有自定义字段的同时，额外写入标准 `text/plain` payload，避免把拖拽成败绑定在浏览器是否接受自定义 drag data type 上。
- 数据正规化：引入 `normalizeConnectionType()` 把 legacy alias 在编辑、导入、导出三个方向统一归一，保证新数据天然落成 canonical 值，同时保留旧 JSON 的兼容性。
- 验证策略：发现 `agent-browser drag` 不会可靠触发源元素 `dragstart`，因此改用同一 `DataTransfer` 手工串起 `dragstart → drop` 做页面主链回归，避免把自动化工具局限误判成页面本身仍然损坏。

## LevelDesigner 历史版本副本导出 - 2026-04-09 16:16
- 新建/修改文件：`Tools/LevelDesigner.2026-02-27_17-25-04.html`
- 内容：从 Git 提交 `48b0ec05b927d6120f9e5cf88bdc057260703d03`（时间 `2026-02-27 17:25:04 +0800`）导出 `LevelDesigner.html` 副本，保留当前工作区文件不变。
- 目的：为 room 拖拽失效问题提供历史版本对照，便于与当前版本做逐步 diff 和行为回归。
- 技术：使用 `git show <commit>:path` 提取历史文件快照并落地为独立 HTML 副本。

## LevelDesigner 0227基线恢复升级 - 2026-04-09 22:09
- 新建/修改文件：`Tools/LevelDesigner.html`、`Tools/LevelDesigner.pre-2026-04-09-2159.html`、`Tools/_tmp_upgrade_leveldesigner_from_0227.py`（已删除）
- 内容：以 `2026-02-27 17:25:04 +0800` 的稳定版 `LevelDesigner.html` 为基线重建主文件，恢复旧版拖拽/投放主链；同时前移关卡名称导出、一键导出文件、canonical 房间类型、canonical 连接类型、房间/连接 legacy→canonical 归一、导入导出与本地存档归一化。
- 目的：回到已验证可用的交互基线，避免继续在坏掉的新版本主链上修补；在不牺牲可用性的前提下保留 Unity 当前需要的 JSON 契约与类型升级。
- 技术：采用历史快照回退 + 选择性前移升级点的策略；保留旧版 HTML5 DnD 数据写法，使用字符串补丁重建 canonical room/connection 映射，并用 Node `vm.Script` 做静态语法校验。

## LevelDesigner 房间类型面板样式统一 - 2026-04-09 23:02
- 新建/修改文件：`Tools/LevelDesigner.html`
- 内容：修正左侧房间类型 preset 面板的 CSS 映射，为当前 canonical 房间类型 `transit / pressure / resolution / reward / anchor / loop / hub / threshold / safe / boss` 补齐背景色与左侧色条，同时保留旧版 alias 样式兼容。
- 目的：解决只有 `safe` 和 `boss` 显示背景、其余房型缺少视觉填充的问题，统一房型面板的可读性与视觉反馈。
- 技术：更新 HTML 内联 CSS 的类型类名映射，使 preset 面板样式与当前 RoomNodeType 命名保持一致。

## LevelDesigner 左侧栏滚动体验修正 - 2026-04-09 23:05
- 新建/修改文件：`Tools/LevelDesigner.html`
- 内容：确认 `黑水区域` 后仍有 `自定义元素` 区块和 `清空画布` 按钮后，将左侧栏改为整栏纵向滚动，并移除 `custom-element-section` 的内层独立滚动，改为统一由外层 sidebar 承担滚动。
- 目的：避免左侧面板在黑水区域处看似结束但实际后面仍有内容，提升侧栏内容可发现性与浏览一致性。
- 技术：调整 `.left-panel` 的 `overflow-y` / `min-height`，并简化自定义元素区块的布局滚动职责。

## LevelDesigner 左侧栏滚动修正补丁 - 2026-04-09 23:09
- 新建/修改文件：`Tools/LevelDesigner.html`
- 内容：修正左侧栏未实际生效的滚动样式，为 `.left-panel` 显式补上 `height: 100vh`、`overflow-y: auto`、`overflow-x: hidden`、`flex-shrink: 0` 与 `overscroll-behavior: contain`。
- 目的：解决左侧栏内容超出后仍无法滚动的问题，确保 `房间类型 / 房间元素 / 自定义元素 / 清空画布` 可以完整浏览。
- 技术：强化 sidebar 布局约束，避免仅依赖 flex 拉伸导致滚动容器未被正确建立。

## LevelDesigner 右侧栏滚动支持 - 2026-04-09 23:16
- 新建/修改文件：`Tools/LevelDesigner.html`
- 内容：为右侧属性栏 `.right-panel` 补充显式 sidebar 滚动约束，新增 `height: 100vh`、`overflow-y: auto`、`overflow-x: hidden`、`flex-shrink: 0`、`min-height: 0` 与 `overscroll-behavior: contain`。
- 目的：确保属性编辑、连接列表、元素列表和 ASCII 预览内容超出视口时仍可完整浏览，不会被侧栏高度截断。
- 技术：统一左右侧栏的布局策略，使用显式高度 + 纵向滚动容器的方式建立稳定的 sidebar 滚动行为。

## LevelDesigner JSON 字段对照 CSV - 2026-04-09 23:38
- 新建/修改文件：`Docs/Reference/LevelDesigner_JSON_Field_Matrix.csv`
- 内容：整理 `LevelDesigner.html` 当前导出 JSON 字段与 Unity `LevelSliceBuilder` 当前消费情况的对照表，覆盖顶层字段、`rooms[]`、`connections[]`、`rooms[].elements[]` 与 `doorLinks[]`。
- 目的：把口头说明沉淀为结构化表格，便于后续工具链迭代、字段补接与多人协作核对。
- 技术：基于现有 HTML 导出逻辑和 Unity 导入器字段模型，生成 CSV 形式的字段消费矩阵。

## LevelDesigner JSON 字段对照 CSV 归档位置调整 - 2026-04-09 23:57
- 新建/修改文件：`Docs/7_Reference/LevelDesigner_JSON_Field_Matrix.csv`、`Docs/5_ImplementationLog/ImplementationLog.md`
- 内容：将 `LevelDesigner_JSON_Field_Matrix.csv` 从未编号目录 `Docs/Reference/` 移动到已有编号目录 `Docs/7_Reference/`，保持文档目录结构一致。
- 目的：遵循项目文档按编号目录归档的习惯，避免继续向非编号目录落文件。
- 技术：使用工作区内文件移动方式调整文档归档位置，并追加实现日志记录此次归位操作。

## LevelDesigner JSON 字段对照 CSV 再归档到诊断目录 - 2026-04-10 00:13
- 新建/修改文件：`Docs/6_Diagnostics/LevelDesigner_JSON_Field_Matrix.csv`、`Docs/5_ImplementationLog/ImplementationLog.md`
- 内容：根据最新归档要求，将 `LevelDesigner_JSON_Field_Matrix.csv` 从 `Docs/7_Reference/` 移动到已有编号目录 `Docs/6_Diagnostics/`。
- 目的：统一放入当前更符合使用语境的诊断类文档目录，避免参考资料与诊断对照表分散。
- 技术：使用工作区内文件移动方式调整文档归档位置，并追加实现日志记录此次目录变更。

## LevelSliceBuilder 消费 doorLinks.spawnOffset - 2026-04-10 00:39
- 新建/修改文件：`Assets/Scripts/Level/Editor/LevelArchitect/LevelSliceBuilder.cs`、`Docs/6_Diagnostics/LevelDesigner_JSON_Field_Matrix.csv`、`Docs/5_ImplementationLog/ImplementationLog.md`
- 内容：为 `LevelSliceBuilder` 增加 `doorLinks` / `spawnOffset` 解析与消费逻辑；导入时按 `roomId + entryDir` 建立查找表，创建每个门的本地 `SpawnPoint` 时优先使用 JSON 中的自定义落点，缺失时继续回退到原有 inward offset 规则；同时更新字段矩阵，把 `doorLinks`、`roomId`、`entryDir`、`spawnOffset` 标记为 Unity 当前已消费。
- 目的：让 LevelDesigner 中“从哪个方向进入这个房间，就应该落在哪个门元素附近”的设计意图真正进入 `LevelSliceBuilder` 主导入链，实现门过渡出生点的自由定义而不破坏旧 JSON 兼容性。
- 技术：扩展 `LevelSliceBuilder` 的 `JsonUtility` 数据模型，新增 `DoorLinkJson` 和 `roomId+entryDir` 查找辅助函数；将 HTML 左上角房间坐标系下的 `spawnOffset` 转换为 Unity 房间中心局部坐标；保留旧 `GetSpawnOffset()` 作为 fallback，并用 `dotnet build Project-Ark.slnx` 完成编译验证。

## LevelDesigner spawnOffset 语义收口 - 2026-04-10 09:57
- 新建/修改文件：`Tools/LevelDesigner.html`、`Assets/Scripts/Level/Editor/LevelArchitect/LevelSliceBuilder.cs`、`Docs/6_Diagnostics/LevelDesigner_JSON_Field_Matrix.csv`、`Docs/5_ImplementationLog/ImplementationLog.md`
- 内容：将 `doorLinks[].spawnOffset` 在 HTML 侧正式收口为“进入目标房间后的玩家落点”；导出时优先保留显式 `spawnOffset`，缺失时按“门视觉中心 + inward”默认规则推导；导入 JSON 与本地恢复时保留该字段；门关联属性面板补充落点值与来源说明；Unity 侧补充注释，明确导入时只做 HTML 左上角局部坐标到房间中心局部坐标的边界转换；同步更新字段矩阵说明。
- 目的：让 HTML、JSON 契约与 Unity 导入对 `spawnOffset` 的业务语义保持一致，避免再把“门元素坐标”和“玩家落点”混为一谈，同时保留方案 1 的坐标转换边界。
- 技术：前端新增 `spawnOffset` 归一化与默认推导 helper；默认落点采用 `door icon visual center + inward 2.5 格`；JSON round-trip 保留显式 authored 值；Unity 维持现有 `roomId + entryDir` 查找与单次坐标系转换。

## LevelSliceBuilder 修复重复 Room Collider 导致 spawnOffset 偏移 - 2026-04-10 11:09
- 新建/修改文件：`Assets/Scripts/Level/Editor/LevelArchitect/LevelSliceBuilder.cs`、`Docs/5_ImplementationLog/ImplementationLog.md`
- 内容：在 `CreateRoomGameObject()` 中停止重复 `AddComponent<BoxCollider2D>()`，改为复用 `Room` 因 `[RequireComponent(typeof(BoxCollider2D))]` 自动注入的碰撞体；修复后用 Unity MCP 在临时空场景里复跑两组最小导入样例（默认 `spawnOffset` 与显式 `spawnOffset`），确认房间只保留一个尺寸正确的 `BoxCollider2D`，且 `Door.TargetSpawnPoint` 最终局部坐标与预期完全一致。
- 目的：修复真实导入链中因读取到默认 `1x1` BoxCollider2D 而导致的 `spawnOffset` 换算偏移，让 HTML → JSON → Unity 的门过渡落点在实际场景对象上真正对齐。
- 技术：利用 `RequireComponent` 行为分析定位重复组件根因；改为 `GetComponent<BoxCollider2D>() ?? AddComponent<BoxCollider2D>()` 的单实例策略；使用 Unity MCP `execute_code` 在临时 additive 场景中通过反射调用 `LevelSliceBuilder.BuildFromJson()` 做闭环验证，并在验证后自动清理临时资产。

## 导入房间阻挡与相机丢失问题排查收口 - 2026-04-10 11:23
- 新建/修改文件：`Assets/Scripts/Level/Editor/LevelArchitect/LevelSliceBuilder.cs`、`Assets/Scenes/SampleScene.unity`、`Docs/5_ImplementationLog/ImplementationLog.md`
- 内容：排查发现当前 `SampleScene` 中活跃导入切片 `── NewLevel ──` 的房间残留了旧版本导入生成的额外 `1x1` 非 trigger `BoxCollider2D`，同时 `RoomManager._startingRoom` 与 `CinemachineConfiner2D.BoundingShape2D` 仍指向 inactive 的旧 `Room_Start`；已清理 `NewLevel` 房间上的错误碰撞盒，仅保留正确的房间 trigger，并把起始房间与相机 confiner 绑定切到活跃导入房间 `room_reward_1`。另外为 `LevelSliceBuilder` 增加“同名切片覆盖式导入”行为，重复导入相同 `levelName` 时会先替换旧场景根节点，防止历史脏房间对象继续残留。
- 目的：修复导入房间测试时飞船被错误实体碰撞盒干扰、以及 camera 仍锁在旧房间导致看不到飞船的问题，同时让后续重复导入不会继续叠加旧坏数据。
- 技术：使用 `Resources.FindObjectsOfTypeAll<Transform>()` + `Undo.DestroyObjectImmediate()` 查找并替换已有切片根节点；通过 Unity MCP `execute_code` 清理当前场景的多余 `BoxCollider2D`、改写 `RoomManager` 起始房间和 `CinemachineConfiner2D` 绑定；在 Play Mode 下验证 `RoomManager` 初始进入 `room_reward_1`，并确认切到 `room_pressure_2` 时 `RoomCameraConfiner` 会同步更新边界。

## 房间边缘实体阻挡根因修复（CameraConfiner） - 2026-04-10 11:59
- 新建/修改文件：`Assets/Scripts/Level/Editor/LevelArchitect/LevelSliceBuilder.cs`、`Assets/Scripts/Level/Editor/LevelArchitect/RoomFactory.cs`、`Assets/Scripts/Level/Editor/LevelArchitect/LevelValidator.cs`、`Assets/Scenes/SampleScene.unity`、`Docs/5_ImplementationLog/ImplementationLog.md`
- 内容：继续排查后确认真正阻挡飞船的不是 `Room` 根节点 trigger，而是每个导入房间子节点 `CameraConfiner` 上的 `PolygonCollider2D`：当前场景里它被错误保存在 `RoomBounds` 层且 `isTrigger=false`，实际成了实体边界墙。已在 `SampleScene` 中把 `NewLevel` 下两个房间的 `CameraConfiner` 修正为 `Ignore Raycast` 且 `isTrigger=true`，同时清理了重复门对象；并在 `LevelSliceBuilder`、`RoomFactory`、`LevelValidator` 三个入口统一补上 `CameraConfiner` 必须为 trigger 的约束与自动修复逻辑。
- 目的：彻底消除导入房间边缘把飞船顶住的问题，避免 camera confiner 参与玩家物理碰撞；同时让后续所有建房入口都不会再次生成同类实体挡板。
- 技术：使用 Unity MCP `execute_code` 枚举 `NewLevel` 下全部 `Collider2D`，定位到 `CameraConfiner` 的 `PolygonCollider2D` 是唯一剩余非 trigger 边界；通过编辑器脚本把 confiner 统一改为 `Ignore Raycast + isTrigger=true`；在 Play Mode 下再次验证 `NewLevel` 内 `nonTriggerCount=0`，并用从房间外向内的 `Physics2D.RaycastAll` 确认只命中房间 trigger、不再命中实体边界。

## Level 模块补充 CameraConfiner 阻挡排查 tips - 2026-04-10 12:05
- 新建/修改文件：`Implement_rules.md`、`Docs/5_ImplementationLog/ImplementationLog.md`
- 内容：在 `Implement_rules.md` 的 Level 模块中追加了 `CameraConfiner` 非阻挡规则、真实踩坑总结、常规验收项和难定位 bug 的排查顺序，明确 `CameraConfiner` 必须保持 `Ignore Raycast + isTrigger=true`，并把“房间边缘堵船时先枚举 Room 下全部 Collider2D”沉淀为标准检查动作。
- 目的：把这次成功修补转化成长期可复用的 tips，避免未来再次把 `CameraConfiner` 误当成 Door、RoomManager 或移动系统问题。
- 技术：按 `Implement_rules.md` 的 Level 模块结构，将经验分别写入“实现规则 / 踩坑总结 / 验收清单 / 推荐工作流”四个层次，让后续排查能从规则、现象、检查清单和步骤四个维度直接复用。

## CLAUDE 与 Implement_rules 过时内容清理 - 2026-04-10 12:25
- 新建/修改文件：`CLAUDE.md`、`Implement_rules.md`、`Docs/5_ImplementationLog/ImplementationLog.md`
- 内容：对 `CLAUDE.md` 和 `Implement_rules.md` 做了一轮去过时化治理，清理了已失效的文档路径、旧的 `ImplementationLog` 路径、过期的 Skills 名称与调用方式说明，以及 `Level` 模块中已不存在的工具名、Batch 迁移叙述和历史清单；同时把 `Level` 章节改写为当前现役的 authoring / validation 工具链视角。
- 目的：让项目协作文档重新对齐当前仓库结构与真实工具链，避免后续开发、排查和多会话协作继续被旧路径、旧流程和不存在的工具误导。
- 技术：通过对照当前工作区真实文件路径、现役设计文档位置和实际存在的 Editor 工具类，逐段替换文档中的失配引用与阶段性叙述，并把规则表达从一次性迁移语境收口为长期有效的治理约束。

## CLAUDE 陷阱库迁移到 Implement_rules - 2026-04-10 13:08
- 新建/修改文件：`CLAUDE.md`、`Implement_rules.md`、`Docs/5_ImplementationLog/ImplementationLog.md`
- 内容：将原本堆在 `CLAUDE.md` 中的 `常见陷阱` 与 `Unity 编辑器操作边界` 从项目总章程里迁出，改为一个轻量的“模块陷阱与治理入口”索引；同时在 `Implement_rules.md` 中新增 `全局 Unity / Editor 治理`、`Core / Infrastructure`、`UI`、`Combat / Projectile` 四个正式章节，把对象池状态泄漏、共享运行时实例、uGUI Mask / CanvasGroup / DragDrop / Raycast 隐性坑，以及投射物自碰撞等经验按模块归档。
- 目的：把 `CLAUDE.md` 收口为全局协作规则与 agent 使用约束，把复用型踩坑经验统一沉淀到 `Implement_rules.md`，降低文档职责重叠和后续协作时的检索成本。
- 技术：先按“全局治理 / Core / UI / Combat”四类重组原有陷阱条目，再调整 `Implement_rules.md` 的顶部导航与模板编号，使新增章节与现有 `Ship / VFX`、`Level` 章节并列，同时保留 `CLAUDE.md` 中仅属于 agent / 工具层的实用提示。

