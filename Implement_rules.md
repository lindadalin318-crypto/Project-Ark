# Project Ark — Implement Rules

> 本文档是 **模块级实现规则 / 协作约束 / 踩坑治理** 的统一入口。
>
> 它的职责是：
>
> - 按模块沉淀长期有效的实现规则与协作约束
> - 记录那些“不会马上写进代码，但必须在实现时遵守”的治理原则
> - 把高频踩坑收敛成可执行的 checklist / tips / guardrails
>
> 它**不是**现役链路、资产映射、Prefab/Scene owner 的真相源。各模块的权威来源：
>
> - `Ship / VFX`：`Docs/Reference/ShipVFX_CanonicalSpec.md` + `ShipVFX_AssetRegistry.md`
> - `Level`：`Docs/Reference/Level_CanonicalSpec.md`
>
> 当前已启用模块：
>
> - `Ship / VFX`
> - `Level`
>
> 后续可以继续追加：
>
> - `Combat`
> - `UI`
> - `Enemy`
> - `Save`
>
> 约束：
>
> - 本文档负责“怎么做更不容易烂掉”
> - `CanonicalSpec` 负责“现在什么是现役真相”
> - `AssetRegistry` 负责“对象映射、owner、状态与路径”
>
> 若三者发生冲突：**现役链路 / owner / 路径 / 状态判断，以 `CanonicalSpec` 和 `AssetRegistry` 为准；本文档负责约束实现行为。**

---

## 1. 文档使用规则

### 1.1 写入时机

当某个模块出现以下任一信号时，应考虑把经验沉淀到本文件对应模块：

- 同类 bug 连续出现两次以上
- 排查时间明显长于实现时间
- 存在多个入口同时改同一条链路
- 开始出现 fallback、override、hardcode、silent no-op 叠加
- 新功能开发时需要先“考古”才能动手

### 1.2 写法约束

每个模块规则都应尽量包含以下 4 类内容：

1. **模块边界**：本规则管哪些目录 / prefab / scene / doc
2. **实现规则**：之后写代码时必须遵守什么
3. **踩坑总结**：已经踩过什么坑，根因是什么
4. **验收清单**：改完后至少要验证什么

### 1.3 禁止事项

- 禁止把本文件写成第二份 `CanonicalSpec`
- 禁止在本文件中复制大段资产映射表，避免重复维护
- 禁止把一次性调试过程原样贴进来；这里只保留能复用的规则
- 禁止用模糊表述代替约束；能落成 checklist 的尽量落成 checklist

---

## 2. Ship / VFX 模块规则

### 2.1 模块边界

本节适用范围以以下文档为准：

- 现役规范：`Docs/Reference/ShipVFX_CanonicalSpec.md`
- 资产映射：`Docs/Reference/ShipVFX_AssetRegistry.md`

当前主要覆盖：

- Runtime
  - `Assets/Scripts/Ship/VFX/`
  - `Assets/Scripts/Ship/Movement/ShipBoost.cs`
  - `Assets/Scripts/Ship/ShipStateController.cs`
- Editor
  - `Assets/Scripts/Ship/Editor/`
- Prefab / Scene
  - `Assets/_Prefabs/Ship/Ship.prefab`
  - `Assets/_Prefabs/VFX/BoostTrailRoot.prefab`
  - `Assets/Scenes/SampleScene.unity`
- 相关文档
  - `Docs/Reference/ShipVFX_CanonicalSpec.md`
  - `Docs/Reference/ShipVFX_AssetRegistry.md`
  - `Docs/Reference/ShipVFX_MigrationPlan.md`
  - `Docs/Reference/Ship_VFX_Player_Perception_Reference.md`

### 2.2 模块目标

`Ship/VFX` 模块的首要目标不是“局部看起来能跑”，而是：

- **新增一个 VFX 层时，不需要重新考古整条工具链**
- **修一个 bug 时，不需要同时怀疑 Runtime / Prefab / Scene / Debug 四层都在偷偷改状态**
- **场景接线、Prefab 结构、运行时行为三者的 owner 清晰且可验证**

换句话说，这个模块的长期目标是：

> **让下一次迭代更快，而不只是让这一次 bug 暂时消失。**

### 2.3 Phase A 治理完成标准

当 `Ship/VFX` 完成这一轮 authority 收口治理后，至少要满足以下 5 条：

1. **唯一权威**：每类引用只有一个权威来源。
   - 例如：prefab 结构只能由一个工具生成，scene-only 接线只能由一个入口负责。
2. **无双轨主链**：不再保留不必要的 runtime fallback。
3. **debug 不接管主链**：debug 工具默认只观察，不得持续覆盖正式运行态。
4. **override 白名单化**：明确哪些 scene override 是允许的，哪些必须清零。
5. **无静默失败**：关键依赖缺失时，必须报错或被 validator / audit 抓到。

### 2.4 Authority Matrix 的文档定位

- `Implement_rules.md` **可以直接承载 authority matrix / 权限表**，因为它属于“如何治理实现边界”的规则层内容。
- 但它只定义：
  - 哪类对象应由谁写
  - 哪类工具应被降权
  - 哪些 override 合法
  - 哪些 builder 只能 audit / preview，不能隐式 apply
- 它**不替代**以下文档：
  - `ShipVFX_CanonicalSpec.md`：现役主链与职责口径
  - `ShipVFX_AssetRegistry.md`：对象路径、owner、状态映射
- 若 authority matrix 中的工具名、对象名与这两份文档冲突，应先同步 `CanonicalSpec` / `AssetRegistry`，再更新这里的执行约束。

---

## 3. Ship / VFX 实现规则

### 3.1 单一真相源 (Single Source of Truth)

- 每个 VFX 子系统必须明确 **唯一权威来源**：
  - **运行时行为**：由运行时代码负责
  - **Prefab 结构**：由唯一一个 Editor 构建器负责
  - **Scene-only 引用**：由唯一一个 Scene Binder 或明确手动流程负责
  - **Debug 预览**：只允许观察 / 预览，不得成为正式行为来源
- 任何时候都要避免同一条链同时被 `Runtime + PrefabCreator + Rebuilder + SceneBinder + DebugManager` 共同改写。
- 遇到“代码改了但表现没变”时，优先排查是否存在多个入口同时写同一条链，而不是先加更多保护逻辑。

### 3.2 改动前必须先回答 5 个问题

对 `Ship/VFX` 做任何非微小改动前，必须先回答：

1. 这个功能 / bug 的**唯一运行时入口**是谁？
2. 相关 **Prefab 结构** 由谁生成？
3. 相关 **Scene-only 引用** 由谁负责？
4. **Debug 工具** 能不能影响正式运行链？
5. 当前是否还有 **legacy / fallback** 在参与行为？

若答不清这 5 个问题，说明还没有摸清模块边界，不应直接编码。

### 3.3 禁止默认新增 Fallback / Legacy Path

- **除非明确批准，否则不允许新增 fallback、legacy path、兼容分支、双轨接线。**
- 若必须保留旧路径，必须同时写清：
  - 为什么现在还不能删
  - 哪些场景 / 工具仍依赖它
  - 计划何时删
- 原则：**先删旧路径，再加新逻辑**；不要把“以后再清理”当作默认收尾方式。

### 3.4 Debug 工具不得接管正式运行链

- Debug 工具默认只能做：
  - 状态观察
  - 图层遮罩
  - 显式手动触发的测试入口
  - Play Mode 预览
- **禁止** Debug 工具在 `Update` / `LateUpdate` 中持续覆盖正式运行态，除非该工具被明确标记为“调试接管模式”。
- **禁止** Debug 工具在 `OnDisable` / `OnEnable` 中隐式重置正式状态，除非文档明确说明其副作用。
- 若 Debug 模式会影响正式逻辑，必须：
  - 有显式总开关
  - Inspector 中有醒目提示
  - 可一键恢复默认状态

### 3.5 禁止 Silent No-Op

- `Ship/VFX` 关键链路缺引用、缺材质、缺 scene object、缺 profile 时，**禁止静默 return**。
- 至少要满足以下之一：
  - `Debug.LogError(...)`
  - Inspector 红字提示
  - Validator / Audit Tool 报告
  - Editor 菜单执行后输出缺失清单
- 原则：**宁可响亮失败，也不要静默失效。**

### 3.6 Runtime / Editor / Scene 三层职责隔离

- **Runtime 层**：只负责表现逻辑，不做资产修复
- **Editor 层**：只负责生成、修复、审计，不在 Play Mode 中偷偷接管主链
- **Scene 层**：只保留少量、明确允许的 scene-only 引用
- 不允许为了“先跑起来”就在三层同时叠逻辑。三层同时加补丁通常不是稳定，而是在制造新复杂度。

### 3.7 Scene Override 白名单化

- `Ship/VFX` 相关 prefab 必须明确：
  - **哪些字段允许 scene override**
  - **哪些字段必须始终跟随 prefab**
- 若一个 bug 根因是 scene override 漂移，修复时不能只修当前实例；必须补上：
  - 该字段是否允许 override
  - 如何检测再次漂移
  - 用什么工具或流程清回去

### 3.8 硬编码治理

- `Ship/VFX` 禁止继续增加以下高风险硬编码：
  - `GameObject.Find("固定名字")`
  - 依赖节点名字的隐式查找
  - 依赖资源文件名的全项目模糊搜索 fallback
  - 写死路径驱动正式运行逻辑
- 允许的例外只应存在于：
  - Editor 迁移脚本
  - 审计 / 清理工具
- 且必须写明：用途、退役条件、影响范围。

### 3.9 统一官方工具链

- 同一类任务只能保留一个“官方入口”：
  - 构建 `Ship.prefab`
  - 构建 `BoostTrailRoot.prefab`
  - 绑定 scene-only 引用
  - 回填材质贴图
  - 审计 / 校验当前状态
- 其余旧工具若继续保留，必须明确标注：
  - `Legacy`
  - `Debug Only`
  - `Migration Only`
- 菜单越多不代表越强；**职责重叠的菜单项是维护成本，不是能力。**

### 3.10 先加 Validator，再加更多 Debug Toggle

当模块已经进入“越来越难改、定位越来越慢”的状态时，优先新增的是：

- `ShipVfxValidator`
- `BoostTrailAudit`
- Override / missing reference / legacy residue 检查

而不是继续新增更多：

- debug toggle
- 临时 bypass
- 场景补丁按钮
- 运行时强制 override 开关

### 3.11 迁移纪律 (Migration Discipline)

- 迁移中的模块必须明确标注：
  - 新路径是否已成为主路径
  - 旧路径是否只是兼容
  - 计划何时删旧路径
- 迁移完成后，必须有一次“删旧路径”收尾。
- **半迁移状态是复杂度指数增长的高危区。**

### 3.12 变更说明必须包含 Override / Tooling 风险

以后凡是改 `Ship/VFX`：

- 必须额外说明这次改动是否会：
  - 产生 scene override
  - 被 prefab 重建器改回去
  - 需要额外跑菜单项
  - 影响 debug 预览链
- 如果这些风险不写明，后续排查会再次退化成“猜谁在改”。

### 3.13 当前 Phase A Authority Matrix（执行约束表）

> 说明：本表是 **治理执行约束**，用于收口“谁可以写”。对象路径、live 状态、owner 名称定义仍以 `ShipVFX_CanonicalSpec.md` 与 `ShipVFX_AssetRegistry.md` 为准。

| 范围 | 对象 / 引用类型 | 唯一权威入口 | 明确禁止的并行写入者 | 备注 |
| --- | --- | --- | --- | --- |
| Prefab 结构 | `Ship.prefab` 的结构、根节点组件、关键节点、内部核心序列化引用 | `ShipPrefabRebuilder` | Runtime fallback、Debug 工具、手工 scene 修补代替 prefab 修正 | 含物理组件、运行时脚本组件、ShipStatsSO/InputActions/DashAfterImage 接线 |
| Prefab 结构 | `BoostTrailRoot.prefab` 的结构、子节点、内部核心序列化引用 | `BoostTrailPrefabCreator` | `ShipPrefabRebuilder` 直接改写内部结构、Runtime fallback、Debug 工具 | `ShipPrefabRebuilder` 只负责把 nested prefab 集成进 `Ship.prefab` |
| Scene-only 接线 | `BoostTrailBloomVolume` → `BoostTrailView._boostBloomVolume` 这类 scene-only 绑定 | `ShipBoostTrailSceneBinder` | Runtime 自动补线、Debug 工具、Prefab builder 越权写 scene | 这类引用允许存在于 scene，但必须有唯一绑定入口 |
| 现役材质贴图回填 | 现役 `BoostTrail` 材质与贴图映射 | `MaterialTextureLinker` | Prefab builder 顺手写材质、Runtime 模糊查找贴图、Debug 工具临时写回 | 仅允许维护现役材质映射，不做全项目兜底搜索 |
| Runtime 表现 | Boost 启停、持续层强度、burst 时序、sprite 切换 | `ShipView` / `BoostTrailView` / `ShipEngineVFX` / `ShipVisualJuice` | Editor 工具在 Play Mode 接管、Debug 持续 override、scene 数据修行为 | Runtime 只消费已准备好的引用，不负责修资产 |
| Debug 预览 | Play Mode 观察、Solo Layer、Force Sustain Preview | `BoostTrailDebugManager` | 作为正式链路 owner、默认常开、隐式写回 prefab / scene / runtime 默认值 | 只允许 preview，不得成为主驱动 |

### 3.14 Builder 执行模式规则（Audit / Preview / Apply）

- 关键 builder / binder / linker 应逐步收口为三种明确模式：
  - **Audit**：只检查，不写入
  - **Preview / Dry Run**：列出将改哪些对象、字段、引用
  - **Apply**：显式执行写入
- 在三种模式未全部具备前，至少必须满足：
  - 菜单或 Inspector 入口是**显式触发**的
  - 执行后会输出“改了什么 / 没改什么 / 缺了什么”
  - 不允许在 `OnValidate`、`Awake`、`OnEnable`、Play Mode 启动流程中隐式写回资产或 scene
- 现阶段收口要求：
  - `ShipPrefabRebuilder`：允许 `Apply`，后续补 `Audit`
  - `BoostTrailPrefabCreator`：允许 `Apply`，后续补 `Audit`
  - `ShipBoostTrailSceneBinder`：允许 `Apply`，后续补 `Audit`
  - `MaterialTextureLinker`：允许 `Apply`，后续补 `Audit`
  - `BoostTrailDebugManager`：只允许 `Preview`，不允许 `Apply`

### 3.15 Scene Override 白名单（当前批）

#### 允许的 Scene Override / Scene-only 数据

- `SampleScene.unity` 中的 `BoostTrailBloomVolume` 场景对象
- `BoostTrailView._boostBloomVolume` 指向场景 volume 的引用
- 与该 scene volume 配套的场景级 profile / settings 使用关系

#### 必须跟随 Prefab / Builder，不允许长期漂移的内容

- `Ship.prefab` 内 `ShipVisual` 及其核心子节点层级
- `Ship.prefab` 内 `_boostTrailView`、`_hlRenderer` 等核心序列化引用
- `BoostTrailRoot.prefab` 内 `MainTrail`、`FlameTrail_R`、`FlameTrail_B`、`FlameCore`、`EmberTrail`、`EmberSparks`、`BoostEnergyLayer2`、`BoostEnergyLayer3` 的结构与核心引用
- `BoostTrailView._flameTrailB` 等依赖 prefab 内节点的核心序列化引用
- 现役材质与贴图映射

#### 处理规则

- 若发现“不允许 override”的字段在 scene 中发生漂移，不应靠 runtime fallback 补救；应：
  1. 记录是哪个 builder / prefab owner 应负责
  2. 清理错误 override
  3. 重新执行对应 authority 工具
  4. 补 validator / audit 检查，防止再次出现

### 3.16 Phase A 治理验收补充

本轮若要宣称 `Ship/VFX` authority 收口已完成，必须能明确回答：

1. `Ship.prefab` 结构今天究竟谁说了算？
2. `BoostTrailRoot.prefab` 结构今天究竟谁说了算？
3. scene-only Bloom 接线今天究竟谁说了算？
4. debug 工具关闭后，正式链路是否仍独立成立？
5. 若关键引用缺失，现在是会报错 / 被 audit 抓到，而不是 silent no-op 吗？

---

## 4. Ship / VFX 踩坑总结

### 4.1 多入口改同一链路 = 定位时间倍增

- 当 `Runtime + PrefabCreator + Rebuilder + SceneBinder + DebugManager` 同时能影响一条 VFX 链时，排查对象就不再是一个类，而是一组互相影响的工具链。
- 现象通常是：
  - 代码改了但表现没变
  - prefab 看起来对，但场景里不对
  - 调试时正常，关掉 debug 又不正常

### 4.2 旧路径不删，复杂度不会自己消失

- `legacy fallback` 在迁移早期有价值，但迁移结束后如果不删，就会持续污染认知模型。
- 每保留一条 fallback，未来排查就多一条岔路。

### 4.3 Debug 接管是最危险的隐性变量

- Debug 系统如果能在运行时强制显隐、重置状态、覆盖参数，它就已经不是“观察工具”，而是“行为参与者”。
- 这类工具非常容易让定位时间失真。

### 4.4 Scene Override 漂移是 Unity 项目的高频暗伤

- Prefab 正确，不代表 Scene 实例正确。
- 看到“只有这个场景坏、重建 prefab 也没完全修好”的情况时，优先怀疑 override 漂移，而不是继续加更多运行时保护代码。

### 4.5 Silent No-Op 会把 5 分钟问题拖成 2 小时

- 对 VFX 而言，“看起来什么都没发生”不代表没执行，可能只是关键引用没接上。
- 所以关键依赖必须能报错、能审计、能在 Inspector 中看出来。

### 4.6 工具越多，不代表迭代越快

- 菜单项过多、职责重叠、命名相似，会把“修功能”变成“记忆工具使用顺序”。
- 工具链要少而清晰，而不是多而模糊。

### 4.7 真正可扩展的标准：下次不用再考古

- 如果一个新特效要改 4 个脚本、跑 3 个菜单、补 2 个 scene 引用、再担心 debug 覆盖，那就不是可扩展架构。
- 可扩展的标准不是“这次终于跑起来了”，而是“下次同类需求还能快、还能稳”。

---

## 5. Ship / VFX 验收清单

### 5.1 Phase A 治理验收

对 `Ship/VFX` 完成这一轮 authority 收口治理后，至少检查：

1. 是否做到**唯一权威**：每类引用都只有一个权威来源？
2. 是否做到**无双轨主链**：不必要的 runtime fallback 已被删除或明确降级？
3. 是否做到**debug 不接管主链**：debug 工具默认只观察，不持续覆盖正式运行态？
4. 是否做到 **override 白名单化**：允许与不允许的 scene override 已写清，且处理方式明确？
5. 是否做到**无静默失败**：关键依赖缺失时，会报错或被 validator / audit 抓到？

### 5.2 常规改动验收

对 `Ship/VFX` 做完任意实现改动后，至少检查：

1. 是否新增了第二真相源？
2. 是否新增了 fallback / override / hardcode？
3. Debug 工具是否污染正式链路？
4. Scene / Prefab / Runtime 的职责边界是否更清晰？
5. 是否需要额外跑某个菜单项？若需要，是否写进变更说明？
6. 关键 scene-only 引用缺失时，是否仍会 silent no-op？
7. 这次改动之后，下次加同类 VFX 时，入口是否更容易找？

---

## 6. Ship / VFX 推荐工作流

### 6.1 新需求 / 新特效

1. 先看 `ShipVFX_CanonicalSpec.md` 确认现役主链与 owner
2. 再看 `ShipVFX_AssetRegistry.md` 确认对象映射、路径和当前 owner
3. 若属于迁移中的对象，再看 `ShipVFX_MigrationPlan.md`
4. 明确本次只改 Runtime、Editor、Scene 哪一层为主
5. 写 3-5 条验收标准
6. 编码
7. 做 scene / prefab / runtime 闭环验证
8. 补 `ImplementationLog`

### 6.2 难定位 bug

1. 先确认当前走的是哪条现役链
2. 再确认有没有 debug 接管
3. 再确认 prefab / scene override 是否漂移
4. 最后才考虑临时 trace / debug 开关
5. 若问题根因是模块边界模糊，优先补规则 / validator，而不是继续叠保护逻辑

---

## 7. Level 模块规则

> **启用时机**：Level 模块进入 CanonicalSpec 驱动的重构阶段（Batch 1 起），工具链开始增长，需要事前设置 authority 防止重蹈 VFX 的治理困境。
>
> **与 VFX 的区别**：VFX 的 Authority Matrix 是"事后救火"——5+ 个 Editor 工具同时写同一个 Prefab 导致定位地狱。Level 模块工具职责天然分离（每个工具改不同对象），因此采用**轻量预防式** authority，重点是迁移纪律和 Scene 接线治理。
>
> **权威来源**：
> - 目标架构 / 数据结构 / 迁移策略：`Docs/Reference/Level_CanonicalSpec.md`（权威）
> - 实现约束 / 踩坑治理：本文档本节（执行层）
> - 若两者冲突，以 `Level_CanonicalSpec.md` 为准

### 7.1 模块边界

本节适用范围以 `Level_CanonicalSpec.md` §2 为准：

- Runtime
  - `Assets/Scripts/Level/` 全部子目录（Room / Data / Checkpoint / Progression / WorldClock / DynamicWorld / Map / Camera / Narrative / GameFlow / Hazard / SaveBridge）
  - `Assets/Scripts/Core/LevelEvents.cs`
- Editor
  - `Assets/Scripts/Level/Editor/` 全部文件
- Data Assets
  - `Assets/_Data/Level/` — 所有 SO 资产（WorldGraphSO、RoomSO、EncounterSO、WorldPhaseSO 等）
- Scene
  - `Assets/Scenes/SampleScene.unity`（示巴星切片）
- 相关文档
  - `Docs/Reference/Level_CanonicalSpec.md`
  - `Docs/Reference/Level_Architecture_Synthesis_Minishoot_Silksong_TUNIC.md`（参考输入）
  - `Docs/LevelModulePlan.md`（v3.0，降级为历史参考）

### 7.2 模块目标

Level 模块的治理目标：

- **加一个新房间时，只需要创建 SO + 场景布局，不需要考古工具链执行顺序**
- **改一扇门的连接时，只需要改 WorldGraphSO 和跑一次 DoorWiringService，不需要同时改 Runtime / Scene / 地图**
- **迁移结束后，旧路径（RoomType 等）干净删除，不留半迁移状态**

### 7.3 Authority 执行约束表

> 说明：本表是 Level 模块的**轻量版 authority**，用于收口"谁可以写什么"。工具的完整职责定义以 `Level_CanonicalSpec.md` §9 为准，本表只约束执行边界。

| 对象类型 | 唯一写入者 | 禁止 | 备注 |
|---------|-----------|------|------|
| WorldGraphSO 资产（创建） | `LevelAssetCreator` | 手动 Assets 右键创建、Runtime 回写 | 幂等：已存在则跳过 |
| WorldGraphSO 资产（编辑） | Inspector 手动编辑 / `WorldGraphEditor` | Runtime 修改、其他 Editor 工具越权写 | 创建后数据完全可调 |
| Room GameObject 结构 | `RoomFactory` | 手动创建子节点、Runtime 补全子节点 | 标准子节点语法由 CanonicalSpec §6 定义 |
| Door 双向连线 | `DoorWiringService` | Runtime 自动补线、手动在 Inspector 逐个接 | GateID 仍为手动分配 |
| SO 资产批量创建 | `LevelAssetCreator` | 手动逐个创建 | 含 RoomSO、WorldGraphSO、EncounterSO 等 |
| 全局校验 | `LevelValidator` | 任何工具隐式修复问题 | Validator 只报告，不写入 |
| Scene View 可视化 | `PacingOverlayRenderer` / `RoomBlockoutRenderer` | 写入场景数据 | 只读 |
| RoomType → RoomNodeType 迁移 | `RoomNodeMigrator`（Batch 2） | 手动逐个改、Runtime 兼容分支永久保留 | 迁移完成后必须删旧路径 |
| 房间子节点审计 | `RoomHierarchyAuditor`（Batch 2） | 隐式修复 | 只审计，不自动修正 |
| Runtime 房间加载 / 转场 | `RoomManager` / `DoorTransitionController` | Editor 工具在 Play Mode 接管 | Runtime 只消费 WorldGraphSO 数据 |

> **代码层标记**：上表中所有"唯一写入者"的类在 XML doc `<summary>` 中均已标注 `[Authority: Level CanonicalSpec §9.1]`，方便 grep 定位。
>
> **已删除的旧工具**（2026-03-22）：
> - `LevelDesignerWindow`（`[Obsolete]`，被 `LevelArchitectWindow` 替代）
> - `RoomBatchEditor`（`[Obsolete]`，被 `LevelArchitectWindow` 替代）
> - `ShebaLevelScaffolder`（`[Obsolete]`，被 `LevelArchitectWindow` 替代）
> - `LevelGenerator`（旧 scaffold 生成器，被 `ScaffoldToSceneGenerator` 替代）
> - `HtmlScaffoldImporter`（一次性 HTML 导入工具，已完成使命）
> - `Phase6AssetCreator`（Phase 6 一次性资产生成，已完成使命）
> - `MapUIBuilder`（Map UI 一次性构建工具，已完成使命）
> - `LevelElementLibrary`（数据类，仅被 `LevelDesignerWindow` + `LevelGenerator` 引用）
> - 同步清理了 `LevelArchitectWindow` 中的 Legacy Tool Detection 代码段

### 7.4 实现规则

#### 7.4.1 迁移纪律（最高优先级）

Level 重构是**增量升级**（CanonicalSpec §10），最大风险是半迁移状态。以下规则必须遵守：

- 每个 Batch 结束时，必须明确标注：
  - 哪些旧路径已被新路径替代
  - 哪些旧路径仍需保留（写明原因 + 计划删除的 Batch）
  - 哪些旧路径本 Batch 已删除
- **禁止新增永久兼容分支**。若必须保留旧代码路径，必须用 `[Migration: remove in Batch N]` 注释标注，且 N ≤ 当前 Batch + 2
- RoomType → RoomNodeType 迁移完成后（Batch 2），必须有一次收尾：删除旧 `RoomType` 枚举及所有引用

#### 7.4.2 Runtime 不回写设计时数据

- `WorldGraphSO` 是设计时离线数据（CanonicalSpec §7），Runtime 严禁修改
- Runtime 需要的可变状态（房间解锁、门状态）走 `RoomFlagRegistry`（Batch 3），不走 SO

#### 7.4.3 工具执行模式

与 VFX 一致的原则（CanonicalSpec §9.3）：

- 工具默认为 **Audit / Preview** 模式，显式操作才 **Apply**
- 所有工具执行后必须输出"改了什么 / 没改什么 / 缺了什么"
- 禁止工具在 `OnValidate`、`Awake`、`OnEnable`、Play Mode 启动流程中隐式写回资产或 scene

#### 7.4.4 禁止 Silent No-Op

Level 关键链路缺引用时，禁止静默 return：

- `RoomManager` 找不到 WorldGraphSO → `Debug.LogError`
- `Door` 的 `_targetRoom` 为 null → `Debug.LogError`（不静默跳过转场）
- `LevelValidator` 发现不一致 → 必须报告，不静默通过

#### 7.4.5 Scene 接线白名单

**允许的 Scene-only 数据**（必须在场景中手动或工具配置，不存在于 Prefab 中）：

- Door 的 `_targetRoom` / `_targetSpawnPoint` 引用（由 `DoorWiringService` 或手动配置）
- Room 的 Cinemachine Confiner bounds
- Room 的 Tilemap 内容
- Checkpoint 位置
- 环境装饰 / Hazard 布局

**必须跟随 SO / 工具生成，不允许场景漂移的数据**：

- WorldGraphSO 中的房间列表和连接关系（权威数据源）
- Room 的 RoomNodeType（由 SO 驱动，不在场景实例上手动改）
- Door 的 GateID / ConnectionType（由 WorldGraphSO 定义，场景中的 Door 组件必须与 SO 一致）

### 7.5 踩坑总结

> 本节当前为骨架，后续 Batch 遇到实际问题时增量追加。

#### 7.5.1 （预防性）半迁移状态

- **风险**：RoomType 和 RoomNodeType 并存期间，部分代码走旧枚举、部分走新枚举，导致行为不一致
- **防御**：Batch 2 的 `RoomNodeMigrator` 必须是原子操作——一次迁移所有房间，不允许"先迁移一半"

#### 7.5.2 （预防性）WorldGraphSO 与场景不一致

- **风险**：WorldGraphSO 描述了 7 个房间的连接关系，但场景中 Door 的实际接线不匹配
- **防御**：`LevelValidator` 的核心校验规则就是"WorldGraphSO ↔ Scene Door 一致性"。每次改完连接关系后必须跑一次 Validator

### 7.6 验收清单

#### 每个 Batch 结束时

1. `LevelValidator` 是否通过（0 error）？
2. 本 Batch 新增的工具是否写入了 Authority 执行约束表？
3. 本 Batch 的迁移是否标注了旧路径处置（保留/已删/计划删）？
4. Runtime 是否有回写 SO 的行为？（禁止）
5. 新增的 Editor 工具是否遵循"执行后输出报告"原则？

#### 常规改动验收

1. 改动是否只涉及 Authority 表中允许的写入者？
2. 是否新增了 fallback / 兼容分支？若是，是否标注了退役计划？
3. WorldGraphSO 与场景 Door 是否仍然一致？（跑 Validator）
4. 是否有 silent no-op？（关键引用缺失必须报错）

### 7.7 推荐工作流

#### 新房间 / 新连接

1. 在 WorldGraphSO 中添加 RoomNode / ConnectionEdge
2. 用 `RoomFactory` 在场景中创建 Room GameObject
3. 用 `DoorWiringService` 自动连线
4. 跑 `LevelValidator` 确认一致性
5. 补 `ImplementationLog`

#### 难定位 bug

1. 先跑 `LevelValidator` — 大部分问题是 WorldGraphSO ↔ Scene 不一致
2. 确认 Runtime 有没有回写 SO 数据（运行时数据隔离原则）
3. 确认是否有旧路径（RoomType）仍在参与行为
4. 若问题根因是工具职责不清，优先补 Authority 约束，而非叠保护逻辑

---

## 8. 后续可追加模块（占位）

后续可按同样结构追加：

- `Combat`
- `UI`
- `Enemy`
- `Save`
- `Core / Infrastructure`

新模块首次追加时，**不要求一开始就补齐全部章节**。按需启用，逐步沉淀。

---

## 9. 通用模块规则模板

> 本节定义当新模块需要在 `Implement_rules.md` 中追加规则时，应遵循的统一结构。
>
> **触发条件**（满足任一即应追加）：
> - 同类 bug 在该模块连续出现两次以上
> - 多个入口（Runtime / Editor / Scene / Debug）同时改同一条链路
> - 排查时间明显长于实现时间
> - Editor 工具 ≥3 个且职责开始重叠
> - 新功能开发时需要先"考古"才能动手
>
> **与架构速写的关系**：
> - `Docs/Reference/{ModuleName}_ArchBrief.md`：描述模块**是什么**——结构、职责、驱动关系
> - `Implement_rules.md` 中的模块规则：描述模块**怎么改更不容易烂**——实现约束、踩坑防御、验收清单
> - 两者互补，不重复。架构速写回答"谁管什么"，实现规则回答"改的时候要注意什么"

### 9.1 模块规则统一结构

每个模块追加到 `Implement_rules.md` 时，应包含以下章节（按需启用，不必一次写全）：

```markdown
## N. {模块名} 模块规则

### N.1 模块边界
本规则适用范围：管哪些目录 / prefab / scene / doc
（应与架构速写的「模块边界」保持一致，此处引用而非复制）

### N.2 模块目标
这个模块的治理目标是什么（1-3 句话）
不是功能目标，而是"怎么让迭代更快、改动更稳"

### N.3 实现规则
按条目列出，每条规则都应包含：
- 规则内容（做什么 / 不做什么）
- 根因（为什么有这条规则）
- 适用范围（哪些场景触发）

### N.4 踩坑总结
已踩过的坑，简述：
- 现象
- 根因
- 防御措施

### N.5 验收清单
改完后至少检查什么（3-7 条 checklist）

### N.6 推荐工作流
- 新需求怎么做
- 难定位 bug 怎么查
```

### 9.2 规则质量标准

- **可执行**：每条规则都能转化为具体检查动作，禁止模糊表述（如"注意代码质量"）
- **有根因**：每条规则都要说明"为什么"，没有根因的规则不写
- **不重复**：与 `CLAUDE.md` 的架构原则 / 代码规范不重复；此处只写该模块特有的约束
- **不过时**：定期审查，已不再适用的规则标记 `[已废弃]` 并说明原因，不要静默删除

### 9.3 模块规则与架构速写的联动

| 时机 | 架构速写 (`_ArchBrief.md`) | 模块规则 (`Implement_rules.md`) |
|------|---------------------------|--------------------------------|
| 新模块开发前 | ✅ 必须产出（工作流第 4 步） | ❌ 不需要，还没踩坑 |
| 首次踩坑后 | 可能需要更新驱动关系 | ✅ 追加踩坑总结 + 对应防御规则 |
| 模块复杂度增长 | Lv.1 → Lv.2 → 完整 Spec | 按需追加实现规则、工具矩阵 |
| 多人 / 多 AI session 协作 | 确保结构描述准确 | ✅ 追加协作约束（如 authority matrix） |
| 架构重构后 | 重写或大幅更新 | 清理过时规则，补新规则 |

### 9.4 已有模块的规则参考

- **Ship / VFX**：见本文档 Section 2-6（当前最完整的模块规则范例）
- **Level**：见本文档 Section 7（轻量预防式 authority）
- **Combat / UI / Enemy / Save**：待追加（见 Section 8 占位）
