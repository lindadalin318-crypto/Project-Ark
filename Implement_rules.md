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
> - `Ship / VFX`：`Docs/2_TechnicalDesign/Ship/ShipVFX_CanonicalSpec.md` + `Docs/2_TechnicalDesign/Ship/ShipVFX_AssetRegistry.md`
> - `Level`：`Docs/2_TechnicalDesign/Level/Level_CanonicalSpec.md`
> - 其余暂无独立 `CanonicalSpec` 的治理章节：以本文档的约束为准；现役链路、对象映射和实现真相仍以对应代码、Prefab / Scene 与设计文档为准
>
> 当前已启用章节：
>
> - `Ship / VFX`
> - `Level`
> - `全局 Unity / Editor 治理`
> - `Core / Infrastructure`
> - `UI`
> - `Combat / Projectile`
> - `SpaceLife / Dialogue`
>
> 后续可以继续追加：
>
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

- 现役规范：`Docs/2_TechnicalDesign/Ship/ShipVFX_CanonicalSpec.md`
- 资产映射：`Docs/2_TechnicalDesign/Ship/ShipVFX_AssetRegistry.md`

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
  - `Docs/2_TechnicalDesign/Ship/ShipVFX_CanonicalSpec.md`
  - `Docs/2_TechnicalDesign/Ship/ShipVFX_AssetRegistry.md`
  - `Docs/0_Plan/ongoing/ShipVFX_MigrationPlan.md`
  - `Docs/7_Reference/GameAnalysis/ShipVFX_PlayerPerception_Reference.md`

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


| 范围            | 对象 / 引用类型                                                                     | 唯一权威入口                                                              | 明确禁止的并行写入者                                               | 备注                                                         |
| ------------- | ----------------------------------------------------------------------------- | ------------------------------------------------------------------- | -------------------------------------------------------- | ---------------------------------------------------------- |
| Prefab 结构     | `Ship.prefab` 的结构、根节点组件、关键节点、内部核心序列化引用                                        | `ShipPrefabRebuilder`                                               | Runtime fallback、Debug 工具、手工 scene 修补代替 prefab 修正        | 含物理组件、运行时脚本组件、ShipStatsSO/InputActions/DashAfterImage 接线   |
| Prefab 结构     | `BoostTrailRoot.prefab` 的结构、子节点、内部核心序列化引用                                     | `BoostTrailPrefabCreator`                                           | `ShipPrefabRebuilder` 直接改写内部结构、Runtime fallback、Debug 工具 | `ShipPrefabRebuilder` 只负责把 nested prefab 集成进 `Ship.prefab` |
| Scene-only 接线 | `BoostTrailBloomVolume` → `BoostTrailView._boostBloomVolume` 这类 scene-only 绑定 | `ShipBoostTrailSceneBinder`                                         | Runtime 自动补线、Debug 工具、Prefab builder 越权写 scene           | 这类引用允许存在于 scene，但必须有唯一绑定入口                                 |
| 现役材质贴图回填      | 现役 `BoostTrail` 材质与贴图映射                                                       | `MaterialTextureLinker`                                             | Prefab builder 顺手写材质、Runtime 模糊查找贴图、Debug 工具临时写回         | 仅允许维护现役材质映射，不做全项目兜底搜索                                      |
| Runtime 表现    | Boost 启停、持续层强度、burst 时序、sprite 切换                                             | `ShipView` / `BoostTrailView` / `ShipEngineVFX` / `ShipVisualJuice` | Editor 工具在 Play Mode 接管、Debug 持续 override、scene 数据修行为    | Runtime 只消费已准备好的引用，不负责修资产                                  |
| Debug 预览      | Play Mode 观察、Solo Layer、Force Sustain Preview                                 | `BoostTrailDebugManager`                                            | 作为正式链路 owner、默认常开、隐式写回 prefab / scene / runtime 默认值      | 只允许 preview，不得成为主驱动                                        |


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

> **当前定位**：Level 模块已进入现役 authoring / validation / scene integration 阶段。这里不再维护“重构计划”或“已删除旧工具清单”，只记录**当前还在代码里存在并且影响协作的规则**。
>
> **与 VFX 的区别**：VFX 的 Authority Matrix 主要用于收口多入口写同一链路；Level 模块更强调 Scene authoring、Door 连线、导入骨架与校验器之间的职责边界。
>
> **权威来源**：
>
> - 目标架构 / 数据结构 / 工具链边界：`Docs/2_TechnicalDesign/Level/Level_CanonicalSpec.md`
> - 实现约束 / 踩坑治理：本文档本节
> - 若两者冲突，以 `Level_CanonicalSpec.md` 为准

### 7.1 模块边界

本节适用范围以 `Level_CanonicalSpec.md` §2 为准：

- Runtime
  - `Assets/Scripts/Level/` 全部子目录（Room / Data / Checkpoint / Progression / WorldClock / DynamicWorld / Map / Camera / Narrative / GameFlow / Hazard / SaveBridge）
  - `Assets/Scripts/Core/LevelEvents.cs`
- Editor
  - `Assets/Scripts/Level/Editor/` 全部文件
- Data Assets
  - `Assets/_Data/Level/` — 所有 SO 资产（RoomSO、EncounterSO、WorldPhaseSO 等）
- Scene
  - `Assets/Scenes/SampleScene.unity`（示巴星切片）
- 相关文档
  - `Docs/2_TechnicalDesign/Level/Level_CanonicalSpec.md`
  - `Docs/3_WorkflowsAndRules/LevelArchitect/Level_WorkflowSpec.md`
  - `Docs/7_Reference/GameAnalysis/Level_Architecture_Synthesis_Minishoot_Silksong_TUNIC.md`（参考输入）
  - `Docs/8_Obsolete/Plan/LevelModule_Plan.md`（历史参考）

### 7.2 模块目标

Level 模块的治理目标：

- **加一个新房间时，只需要走现役工具链与场景布局，不需要考古执行顺序**
- **改一扇门的连接时，只需要改 Door 的 `_targetRoom` / `_targetSpawnPoint` 引用（或跑一次 `DoorWiringService`），不需要同时改 Runtime / Scene / 地图**
- **结构演进完成后，旧路径要及时删除，不保留长期双轨语义**

### 7.3 Authority 执行约束表

> 说明：本表只保留仓库中**当前存在**的现役工具。工具的完整职责定义以 `Level_CanonicalSpec.md` §9 为准，本表只约束执行边界。


| 对象类型              | 唯一写入者                                            | 禁止                                | 备注                                              |
| ----------------- | ------------------------------------------------ | --------------------------------- | ----------------------------------------------- |
| Editor 统一入口       | `LevelArchitectWindow`                           | 直接承担运行时 authority                 | 只编排 `Build / Quick Edit / Validate` 入口，不替代子工具权威 |
| 标准房间模板骨架          | `RoomFactory`                                    | 手动散建标准子节点、Runtime 补全              | 用于 Scene 内快速创建合规 `Room`                         |
| Door 双向连线         | `DoorWiringService`                              | Runtime 自动补线、手动在 Inspector 大量逐个接线 | `GateID` 仍需与设计意图保持一致                            |
| 全局校验与显式修复         | `LevelValidator`                                 | 任何工具隐式静默修复                        | 默认只报告；只有显式 `Auto-Fix` 才允许写回                     |
| Scene View 可视化    | `PacingOverlayRenderer` / `RoomBlockoutRenderer` | 写入场景数据                            | 只读显示                                            |
| Scene View 白盒交互   | `BlockoutModeHandler`                            | 绕过 `LevelArchitectWindow` 状态直接改结构 | 只处理编辑交互，不定义数据权威                                 |
| Runtime 房间加载 / 转场 | `RoomManager` / `DoorTransitionController`       | Editor 工具在 Play Mode 接管           | Runtime 只消费已完成 authoring 的 Door 引用数据            |


> **维护规则**：已删除、计划中、或一次性任务工具不在此处维护；历史沿革交给 `git log` 或对应设计文档，不再混入现役规则表。

### 7.4 实现规则

#### 7.4.1 演进纪律（最高优先级）

Level 的高风险不是“功能没写完”，而是 authoring 工具、场景数据和 runtime 消费链**长期双轨**。以下规则必须遵守：

- 每次结构性改动后，都必须明确记录：
  - 哪些旧入口 / 旧字段已被新路径替代
  - 哪些旧路径仍暂时保留（写明原因与退役条件）
  - 哪些旧路径已完成删除
- **禁止新增永久兼容分支**。若必须保留旧代码路径，必须在代码或文档中写清退役条件，不允许“先留着以后再说”。
- 新 authoring 入口一旦成为现役，就应尽快把旧入口降级为历史参考或删除，避免 scene、tool、runtime 三层同时维护两套语义。

#### 7.4.2 Runtime 不回写设计时数据

- `RoomSO` 等 ScriptableObject 是设计时离线数据（CanonicalSpec §7），Runtime 严禁修改
- Runtime 需要的可变状态（房间解锁、门状态）走 `RoomFlagRegistry` 等运行时状态容器，不走 SO

#### 7.4.3 工具执行模式

与 VFX 一致的原则（CanonicalSpec §9.3）：

- 工具默认为 **Audit / Preview** 模式，显式操作才 **Apply**
- 所有工具执行后必须输出"改了什么 / 没改什么 / 缺了什么"
- 禁止工具在 `OnValidate`、`Awake`、`OnEnable`、Play Mode 启动流程中隐式写回资产或 scene

#### 7.4.4 禁止 Silent No-Op

Level 关键链路缺引用时，禁止静默 return：

- `RoomManager` 找不到注册的 Room → `Debug.LogError`
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

- Room 的 RoomNodeType（由 RoomSO 驱动，不在场景实例上手动改）
- Door 的 GateID / ConnectionType（由 DoorWiringService 或手动分配，场景中的 Door 组件必须与设计意图一致）

#### 7.4.6 房间元素分类口径必须统一

`Level` 房间元素的顶层 authoring 分类，统一使用 **六大玩法家族 + 一类基础设施件**：


| 中文分类      | 英文标签             | 判断问题                       | 当前代表                                                                                                       |
| --------- | ---------------- | -------------------------- | ---------------------------------------------------------------------------------------------------------- |
| **通路件**   | `Path`           | 它是不是在控制玩家怎么去别的地方？          | `Door`                                                                                                     |
| **交互件**   | `Interact`       | 玩家是不是要主动碰它、按它、拿它？          | `Checkpoint`、`Lock`、`PickupBase`                                                                           |
| **状态件**   | `Stateful`       | 房间是不是需要记住它已经变了？            | `DestroyableObject`                                                                                        |
| **战斗件**   | `Combat`         | 它是不是在决定什么时候开打、怎么打、什么时候结束？  | `OpenEncounterTrigger`、`ArenaController`、`EnemySpawner`                                                    |
| **环境机关件** | `Environment`    | 它是不是在持续改变玩家的通行、生存或移动条件？    | `EnvironmentHazard` 及其子类                                                                                   |
| **导演件**   | `Directing`      | 它是不是在控制镜头、氛围、相位或演出状态？      | `BiomeTrigger`、`HiddenAreaMask`、`ScheduledBehaviour`、`ActivationGroup`、`WorldEventTrigger`、`CameraTrigger` |
| **基础设施件** | `Infrastructure` | 它是不是在支撑房间系统运转，而不是直接提供玩法对象？ | `SpawnPoint`、`CameraConfiner`                                                                              |


补充约束：

- 这套分类用于 **文档、Inspector 分组、Validator 输出、authoring 沟通**。
- **禁止**把分类标签当成具体组件类名的替代；代码类名仍保持精确语义，例如 `OpenEncounterTrigger`、`WorldEventTrigger`、`RoomCameraConfiner`。
- 若后续 `CanonicalSpec`、`WorkflowSpec`、`LevelValidator`、诊断文档使用分类标签，应统一沿用本表，不再同时维护第二套近义词命名。

#### 7.4.7 新房间元素必须先落类，再落实现

新增 `Level` 房间元素前，必须先回答：

1. 它属于哪一个顶层玩法家族，或是否应归入 `Infrastructure`？
2. 它的默认挂点应该在 `Navigation / Elements / Encounters / Hazards / Triggers / CameraConfiner` 的哪一层？
3. 它是 `Room` 主链成员，还是组件自治元素？
4. 它是否需要进入状态通道 / Save？

执行规则：

- **优先新增现有家族下的子类型**，不要轻易新增第七个玩法大类。
- 只有当一个新元素**长期重复出现**，且同时满足以下大部分条件时，才考虑新增顶层分类：
  - 默认挂点与现有家族都不一致
  - 运行时 owner 明显独立于现有家族
  - 状态 / Save 规则是一套新语义
  - 未来会形成一批重复元素，而不是一次性特例
- 若只是表现不同、交互不同、子系统不同，但顶层意图仍清楚落在现有家族，应继续归入原家族，不得为了“命名顺手”新开大类。

#### 7.4.8 分类不是运行时 owner，也不是 `Room` 主链声明

- 顶层分类解决的是 **authoring 语义**，不是 Runtime owner 判定。
- **同一分类下的元素，可以分属不同消费层级**：
  - 有些对象由 `Room` 主链主动收集
  - 有些对象属于组件自治，只挂在场景中运行
  - 有些对象同时接入状态通道 / Save
- `Infrastructure` 也不应与玩法六类混用；它服务于房间运行支撑，而不是直接提供玩家交互目标。
- 因此，后续讨论新元素时，必须分别说明：
  - **它属于哪个分类**
  - **它由谁消费**
  - **它挂在哪个根节点**
  - **它是否持久化**

#### 7.4.9 `CameraConfiner` 绝不能参与玩家物理阻挡

- `CameraConfiner` 的职责是提供 **Cinemachine 边界形状**，不是房间实体墙。
- 任何通过 `RoomFactory`、`LevelValidator` 生成或修复出来的 `CameraConfiner`，都必须满足：
  - GameObject 在 `Ignore Raycast` 层
  - `PolygonCollider2D.isTrigger = true`
  - 只服务于相机约束，不承担玩家阻挡职责
- 排查“飞船被 room 边缘堵住，但代码里只看到 room trigger 没问题”时，**优先检查 `CameraConfiner` 是否被保存成 `RoomBounds + isTrigger=false`**。
- 若 `CameraConfiner` 状态不对，禁止只在 Runtime 临时忽略碰撞；必须回到 Editor 生成链或 Validator 修正源头。

### 7.5 踩坑总结

> 本节记录已发生且可复用的真实踩坑；后续新增案例继续增量追加。

#### 7.5.1 （预防性）双轨语义并存

- **风险**：新旧 authoring 入口、字段语义或导入链长期并存，部分代码走旧路径、部分走新路径，导致行为不一致
- **防御**：结构迁移必须尽快收口到单一真相源；若暂时并存，必须明确 owner、退役条件，以及 `LevelValidator` 或文档层的兜底说明

#### 7.5.2 （预防性）Door 连接引用不完整

- **风险**：Door 的 `_targetRoom` 或 `_targetSpawnPoint` 为 null，导致转场静默失败或 MinimapManager 拓扑缺失
- **防御**：`LevelValidator` 校验 Door 引用完整性。每次改完连接关系后必须跑一次 Validator

#### 7.5.3 `CameraConfiner` 被误当成房间实体边界

- **现象**：飞船看起来像被 room 边缘堵住，`Room` 根节点的 `BoxCollider2D` 明明已经是 trigger，但玩家仍然无法顺利进入房间；同时相机问题容易被误诊为 Door / RoomManager / Confiner 切换失败。
- **根因**：房间子节点 `CameraConfiner` 上的 `PolygonCollider2D` 被错误保存成 `RoomBounds` 层且 `isTrigger=false`，结果相机边界形状实际参与了玩家物理碰撞，变成一圈隐形实体墙。
- **防御**：把 `CameraConfiner` 视为“相机专用形状”而不是“房间墙体”——统一要求 `Ignore Raycast + isTrigger=true`；导入后若再出现边缘阻挡，先枚举房间下全部 `Collider2D`，优先找剩余的非 trigger collider，而不是先怀疑移动代码或相机跟随逻辑。

### 7.6 验收清单

#### 每次结构性改动后

1. `LevelValidator` 是否通过（0 error）？
2. 新增或变更的工具是否写入了 Authority 执行约束表？
3. 是否明确标注了旧路径处置（保留 / 已删 / 退役条件）？
4. Runtime 是否有回写 SO 的行为？（禁止）
5. 新增的 Editor 工具是否遵循“执行后输出报告”原则？

#### 常规改动验收

1. 改动是否只涉及 Authority 表中允许的写入者？
2. 是否新增了 fallback / 兼容分支？若是，是否标注了退役计划？
3. 若新增 / 调整了房间元素，是否已明确它的分类、默认挂点、消费 owner 与持久化语义？
4. Door 连接引用是否完整？（跑 Validator）
5. `CameraConfiner` 是否仍满足 `Ignore Raycast + isTrigger=true`？
6. 是否有 silent no-op？（关键引用缺失必须报错）

### 7.7 推荐工作流

#### 新房间 / 新连接

1. 用 `LevelArchitectWindow` 作为统一入口，通过 `Build` / `Quick Edit` 主链创建和精修标准房间骨架
2. 确认 `Room / RoomSO / Door` 的基础结构已经由现役工具链生成，而不是手工补出第二套骨架
3. 手动或用 `DoorWiringService` 配置 Door 的 `_targetRoom` / `_targetSpawnPoint` 引用
4. 跑 `LevelValidator` 确认 Door 连接完整性与关键结构无漂移
5. 补 `ImplementationLog`

#### 新房间元素

1. 先确定它属于 `Path / Interact / Stateful / Combat / Environment / Directing / Infrastructure` 的哪一类
2. 再确定它的默认挂点、Runtime owner、是否进入 `Room` 主链、是否接入 Save
3. 若它只是现有家族的新子类型，沿用原分类；若想新增顶层分类，先补书面理由，再改文档 / 工具 / validator 口径
4. 把 authoring 语义、挂点和校验需求一起落到 `CanonicalSpec` / `WorkflowSpec` / `LevelValidator` 的相应位置
5. 补 `ImplementationLog`

#### 难定位 bug

1. 先跑 `LevelValidator` — 大部分问题是 Door 引用缺失、GateID 重复，或 `CameraConfiner` 配置漂移
2. 若表现为“房间边缘堵船 / 进房异常”，先枚举该 Room 下全部 `Collider2D`，优先找剩余的非 trigger collider
3. 确认 Runtime 有没有回写 SO 数据（运行时数据隔离原则）
4. 确认是否仍有旧 authoring 入口、legacy alias 或历史切片对象在参与行为
5. 若问题根因是工具职责不清，优先补 Authority 约束，而非叠保护逻辑

---

## 8. 全局 Unity / Editor 治理

> 本节处理跨模块共享的 Unity / 序列化 / Editor 操作 guardrails。它不替代各模块规则，而是收口那些**几乎任何模块都可能踩**的底层坑。

### 8.1 适用范围

- `Assets/**/*.unity`、`Assets/**/*.prefab`、`Assets/**/*.asset`、`Assets/**/*.meta`
- Unity Editor 中的 Layer / Tag / Physics2D 配置
- 需要直接编辑 Unity 序列化文件或批量修场景 / prefab 的任务

### 8.2 治理目标

- **避免因为错误编辑 Unity 序列化文件而制造隐性损坏**
- **避免把应由 Editor GUI 或自动化工具处理的配置，降级成高风险手改 YAML**
- **让“能不能直接改 `.unity` / `.prefab` / `.meta`”这类决策有稳定边界**

### 8.3 实现规则

#### 8.3.1 严禁凭空创建 `.meta` 文件

- `.meta` 的 GUID 必须由 Unity 自动生成，禁止手编 GUID。
- 若需要新建 `.cs` / 资源文件，只创建资源本体；`.meta` 由 Unity 生成。

#### 8.3.2 直接编辑 Unity YAML 只能在定位明确时进行

- 允许直接改已存在的 `.unity` / `.prefab` / `.asset` / `.meta`，前提是**知道要改哪个对象、哪个字段、为什么改**。
- 若需要在数千行 YAML 中盲搜 `GUID` / `fileID` / 组件块，优先让用户在 Unity Editor 中提供定位信息，或改用 Editor 自动化工具。
- 原则：**定位成本高时，不要靠纯文本搜索硬撑**。

#### 8.3.3 `fileID` 必须从 Unity 生成结果复制，禁止手写

- 任何 scene / prefab 序列化中的 `fileID`，都必须从 Unity 已生成的真实文件中复制。
- 关键引用若为 null，守卫代码不能静默 return，必须配合 `Debug.LogError` 或 validator 暴露出来。

#### 8.3.4 Physics2D 碰撞矩阵统一走 Editor GUI

- Layer Collision Matrix 一律在 `Project Settings > Physics 2D` 中配置。
- 禁止通过手算位掩码直接改项目配置 YAML。
- 新增 Layer 后，应立即检查目标层与玩家、敌人、投射物、相机辅助层的碰撞关系。

### 8.4 踩坑总结

#### 8.4.1 Unity 内部类 `MonoBehaviour` GUID / fileID 不稳定

- **现象**：Inspector 显示 `script cannot be loaded`，场景或 prefab 上的脚本引用突然失效。
- **根因**：把 `MonoBehaviour` 写成内部类后，Unity 序列化依赖的类型标识会因为类名 / 文件变化而失配。
- **防御**：`MonoBehaviour` 必须是顶级类；遵守“一文件一类”，不要把 `MonoBehaviour` 写成其他类的内部类。

#### 8.4.2 场景序列化 `fileID` 错误导致字段 Missing

- **现象**：代码看起来引用齐全，但运行时字段反序列化成 null，系统走到守卫分支后静默失效。
- **根因**：手写或错误复制了 `fileID`，导致 Unity 无法还原真实引用。
- **防御**：`fileID` 只从 Unity 已生成文件中复制；关键 null 守卫必须配日志或 validator。

#### 8.4.3 Physics2D 碰撞矩阵遗漏

- **现象**：新增 Layer 后，玩家、子弹或触发器出现莫名碰撞 / 穿透 / 自碰撞。
- **根因**：项目设置中的碰撞矩阵没有同步更新。
- **防御**：新增 Layer 后立即做一次碰撞矩阵检查，不把这一步留给“以后再说”。

### 8.5 验收清单

1. 是否避免了手造 `.meta`？
2. 若直接编辑了 Unity YAML，是否有明确定位依据，而不是大范围盲改？
3. 新增或调整引用时，`fileID` 是否来自 Unity 真实生成结果？
4. 新增 Layer 后，是否检查了 Physics2D 碰撞矩阵？
5. 关键引用缺失时，是否会被日志或 validator 抓到，而不是 silent no-op？

---

## 9. Core / Infrastructure 模块规则

> 本节沉淀那些跨模块复用、但又不适合继续散落在 `CLAUDE.md` 的运行时防御性规则。

### 9.1 模块边界

- `Assets/Scripts/Core/` 及其子目录
- 所有实现 `IPoolable` 的运行时对象
- 运行时会从 authored prefab / SO 派生可变实例的系统
- 关键视觉组件缺失时需要 fallback 或响亮失败的表现链路

### 9.2 模块目标

- **对象池对象回收后绝不带脏状态**
- **运行时可变行为绝不共享 authored prefab 上的同一实例**
- **关键视觉依赖缺失时，优先响亮失败或提供明确 fallback**

### 9.3 实现规则

#### 9.3.1 `OnReturnToPool()` 必须完整复位

- 所有运行时字段、事件、动态组件、Transform、视觉状态，都必须在回池时清干净。
- 不允许只重置“当前 bug 涉及的那一项”；池对象要按全量复位思维写。

#### 9.3.2 authored prefab 上的可变行为不能被多实体共享

- 若某个组件或 modifier 会在运行时持有状态，禁止直接从 prefab 上 `GetComponent()` 后让多个实体复用。
- 这类对象必须为每个运行时实例创建独立副本（例如 `AddComponent` + 深拷贝 / 单独初始化）。

#### 9.3.3 关键视觉引用缺失时要么 fallback，要么响亮失败

- 像 `SpriteRenderer.sprite` 这类会导致“物体存在但完全不可见”的关键依赖，不能静默缺失。
- 至少应满足其一：运行时 fallback、显式报错、或 validator / audit 可抓到。

### 9.4 踩坑总结

#### 9.4.1 对象池状态泄漏

- **现象**：颜色、缩放、事件订阅、动态 modifier 等旧状态残留到下一次复用。
- **根因**：`OnReturnToPool()` 只重置了一部分字段。
- **防御**：把对象池回收视为“完整重建初始状态”，而不是“修当前问题”。

#### 9.4.2 SO / Prefab 字段返回共享实例

- **现象**：多个实体看似各自独立，实际共享同一运行时组件状态，导致一个实例的修改污染全部实例。
- **根因**：直接从 authored prefab 上取组件引用，误把 authored data 当成 runtime instance 用。
- **防御**：所有运行时可变对象按实例创建，不共享 prefab 上的同一组件引用。

#### 9.4.3 `SpriteRenderer` 不可见

- **现象**：Prefab 已经生成，逻辑也在跑，但视觉上完全看不到对象。
- **根因**：关键 sprite 未分配，且没有 fallback 或告警。
- **防御**：在 `Awake` / 初始化阶段做显式检查，缺失时 fallback 或报错。

### 9.5 验收清单

1. 所有池对象的 `OnReturnToPool()` 是否重置了字段、事件、Transform、视觉状态？
2. 是否存在从 prefab 上直接取可变组件并在多个实体之间共享的路径？
3. 关键视觉依赖缺失时，是否至少能 fallback、报错或被 validator 抓到？
4. 新增运行时状态后，是否同步补了回池复位逻辑？

---

## 10. UI 模块规则

> 本节收口 UI / DragDrop / Mask / EventSystem 一类高频隐性坑，让“看起来什么都对但就是不响应”的问题有固定排查入口。

### 10.1 模块边界

- `Assets/Scripts/UI/` 及其子目录
- StarChart / 背包 / 拖拽 / Overlay / 高亮相关 UI
- `InputSystemUIInputModule`、uGUI Mask、CanvasGroup、Drag Ghost 等基础设施

### 10.2 模块目标

- **UI 显隐不依赖容易序列化漂移的 `SetActive` 语义**
- **拖拽 / 高亮 / Raycast 行为保持稳定可读**
- **Mask 与 InputModule 的底层前提被显式守护，而不是靠偶然正确**

### 10.3 实现规则

#### 10.3.1 uGUI 面板统一用 `CanvasGroup` 控制显隐

- GameObject 默认保持 active。
- 用 `CanvasGroup.alpha / interactable / blocksRaycasts` 控制开关。
- `Awake()` 中只做初始化，禁止顺手 `SetActive(false)`。

#### 10.3.2 Mask 相关 Image 不能把 alpha 做到 0

- Mask 所依赖的 `Image` 必须保持 alpha ≥ 1/255。
- 若只是想隐藏视觉，用 `showMaskGraphic=false`，不要用 `Color.clear` 充当“不可见但可工作”的 mask。

#### 10.3.3 `InputSystemUIInputModule` 关键 Action 必须显式连线

- 不依赖“Inspector 里大概接过了”。
- 应在初始化或验证阶段显式检查 UI Action 是否就绪，必要时由代码自动补线。

#### 10.3.4 拖拽坐标系和网格索引语义必须单一

- 线性 `cellIndex` 与二维 `(col, row)` 不能混用后二次转换。
- 动态构建子节点时，子节点 anchor 坐标系必须与父节点 pivot 语义一致；若不一致，必须在父节点定位时显式补偿。

#### 10.3.5 透明拦截层必须保留 Raycast 与交互数据

- 想做“透明但可点击 / 可拖拽”的 Image，alpha 不能为 0。
- Overlay 只负责视觉替换，不能把底层 Cell 的交互数据一起清空。

### 10.4 踩坑总结

#### 10.4.1 `InputSystemUIInputModule` 失效

- **现象**：UI 看得到，但点击、拖拽、导航都不响应。
- **根因**：Action 字段未连线或初始化时机错误。
- **防御**：在 `UIManager.Awake` 或等效入口中显式配置 / 验证 UI Actions。

#### 10.4.2 uGUI Mask 裁剪异常

- **现象**：子节点全部被裁掉，或 mask 看起来不生效。
- **根因**：Mask Image alpha 被设成 0，或错误使用 `Color.clear`。
- **防御**：Mask Image alpha 至少保留 1/255；隐藏视觉走 `showMaskGraphic=false`。

#### 10.4.3 面板用 `SetActive` 控制显隐导致 `Awake` 推迟

- **现象**：代码逻辑看起来正确，但首次打开面板会瞬间关闭，或某些初始化永远不执行。
- **根因**：inactive 状态被序列化进场景，`Awake` 被推迟到首次激活时才执行。
- **防御**：GameObject 常驻 active，显隐统一交给 `CanvasGroup`；修复后别忘了在 Editor 中把历史遗留的 inactive 状态重新勾回并保存场景。

#### 10.4.4 动态构建子节点导致拖拽 Ghost 偏移

- **现象**：鼠标位置是对的，但 Ghost 视觉内容整体偏到鼠标下方或侧边。
- **根因**：父节点 pivot 和子节点 anchor 的坐标系不一致。
- **防御**：统一坐标语义；必要时在 `FollowPointer` 层加显式 `centeringOffset`。

#### 10.4.5 网格高亮线性索引被二次转换

- **现象**：高亮落到错误格子，或越界。
- **根因**：已是线性 `cellIndex` 的值，又被当成 `row` 重新做了一次 `row * gridCols + col`。
- **防御**：API 参数语义保持单一；传线性索引就直接按线性索引定位。

#### 10.4.6 `Color.clear` Image 不接收 uGUI Raycast

- **现象**：透明拦截层存在，但事件完全穿透。
- **根因**：alpha 为 0 的像素被 uGUI 视为不可射线命中。
- **防御**：使用极小但非零的 alpha，或不要依赖透明 Image 充当事件层。

#### 10.4.7 Overlay 覆盖 Cell 时丢失交互数据

- **现象**：视觉被 Overlay 正常替换，但拖拽源 / 点击源彻底失效。
- **根因**：Overlay 方案顺手把 `DisplayedItem` 等交互数据也清空了。
- **防御**：坚持“Overlay = 纯视觉，Cell = 交互”，隐藏视觉不等于抹掉数据。

### 10.5 验收清单

1. 面板 GameObject 是否保持 active，显隐是否统一走 `CanvasGroup`？
2. Mask Image alpha 是否 ≥ 1/255，且隐藏视觉是否走 `showMaskGraphic=false`？
3. `InputSystemUIInputModule` 的关键 Action 是否已连线并可验证？
4. 拖拽 Ghost 的 anchor / pivot / offset 语义是否一致？
5. 网格高亮 API 是否明确区分线性索引与二维坐标？
6. 透明拦截层是否仍可接收 Raycast？
7. Overlay 方案是否保留了底层交互数据？

### 10.6 推荐工作流

#### 难定位 UI bug

1. 先确认目标面板 GameObject 是否仍然 active，显隐是否由 `CanvasGroup` 控制
2. 检查 `InputSystemUIInputModule` 的 Action 连线是否完整
3. 检查 Mask / RaycastBlocker 的 Image alpha 是否错误地设成了 0
4. 若是拖拽 / 高亮问题，明确当前 API 传的是 `cellIndex` 还是 `(col, row)`
5. 若是 Overlay 方案，核对视觉层是否误清空了底层交互数据

---

## 11. Combat / Projectile 模块规则

> 本节先收口当前最明确的一类 Combat 高频坑：投射物碰撞配置。后续若 Combat 子系统继续增长，再扩成完整模块规则。

### 11.1 模块边界

- `Assets/Scripts/Combat/Projectile/` 及相关 modifier / collision 处理
- 玩家投射物、敌人投射物及其 Layer / LayerMask 配置
- 依赖 Physics2D 触发与碰撞的战斗表现链路

### 11.2 模块目标

- **投射物永远不会因为层配置疏漏而互相误撞**
- **碰撞规则既体现在项目配置，也体现在代码过滤，不把安全性压在单点上**

### 11.3 实现规则

#### 11.3.1 Physics2D 查询的 `LayerMask` 必须显式声明

- 禁止默认 `~0`。
- 代码层和 prefab / scene 配置层都要明确写出目标层。

#### 11.3.2 投射物自碰撞必须做“双保险”

- 第一层：在 Physics2D 碰撞矩阵中关闭不该互撞的层对。
- 第二层：在代码中继续做 Layer 过滤，避免配置漂移时直接炸成运行时 bug。

### 11.4 踩坑总结

#### 11.4.1 子弹自碰撞

- **现象**：同阵营投射物互相触发 `OnTriggerEnter2D`，出现异常销毁、伤害丢失或表现错乱。
- **根因**：同 Layer 投射物默认仍在互相检测，项目设置和代码过滤至少有一层缺失。
- **防御**：碰撞矩阵关闭 `PlayerProjectile` 自碰撞；代码层继续做 Layer 过滤，不把安全性只押在配置表上。

#### 11.4.2 程序化波纹对象存在，但 `Sprite` 实际全透明

- **现象**：`EchoWavePreview_`* 这类运行时对象已经生成，`SpriteRenderer` 也 enabled，甚至中心 marker 可见，但真正的波环完全看不到，容易被误诊为 sorting、材质或镜头问题。
- **根因**：程序化 `Texture2D` 的 alpha mask 算错，导致生成出来的 ring texture `alphaNZ=0`；本次真实案例中，是把 Unity 的 `Mathf.SmoothStep(from, to, t)` 误当成阈值版 `smoothstep(edge0, edge1, x)` 使用，最终整张波环贴图完全透明。
- **防御**：先做运行时纹理验证，再做美术调参。排查顺序固定为：
  1. 确认运行时对象和 `SpriteRenderer` 确实存在
  2. 直接读取 `sprite.texture` 的 alpha 分布（例如统计 `alphaNZ`）
  3. 只有在纹理本身有非零 alpha 后，才继续调 sorting、颜色、线宽、扩散速度
  4. 对程序化生成函数增加“全透明即报错”的守卫，避免 silent no-op

### 11.5 验收清单

1. 相关 Physics2D 查询是否用了显式 `LayerMask`？
2. `PlayerProjectile` / `EnemyProjectile` 等层对是否在碰撞矩阵中配置正确？
3. 代码层是否仍保留了必要的 Layer 过滤？
4. 新增投射物家族时，是否同步检查了层与碰撞语义？
5. 若新增了程序化 `Texture2D` / `Sprite` 预览物，是否验证过它不是“对象存在但整张贴图全透明”？

---

## 12. SpaceLife / Dialogue 模块规则

> 本章沉淀自 `2026-04-22-spacelife-hub-dialogue-master-plan.md` (v1.1) Phase 0。
> 首要目的：在 SpaceLife 模块落地 Hub + NPC 对话前，先把"谁是权威"钉死，避免 UI / Domain / Editor 三层再次出现多入口改同一条链的历史悲剧（见 §4 踩坑总结）。

### 12.1 模块边界

本节适用范围：

- Runtime
  - `Assets/Scripts/SpaceLife/` 所有非 Editor 子目录
  - `Assets/Scripts/SpaceLife/Dialogue/`
  - `Assets/Scripts/SpaceLife/Data/NPCDataSO.cs`（Legacy 字段处于降级中）
  - `Assets/Scripts/SpaceLife/PlayerController2D.cs`
  - `Assets/Scripts/SpaceLife/PlayerInteraction.cs`
  - `Assets/Scripts/SpaceLife/SpaceLifeInputHandler.cs`
  - `Assets/Scripts/SpaceLife/MinimapUI.cs`
- Editor
  - `Assets/Scripts/SpaceLife/Editor/`（含 `SpaceLifeSetupWindow` — 已冻结）
- Prefab / Scene
  - `Assets/_Prefabs/UI/DialogueUI.prefab`（Phase 2 重建）
  - `Assets/_Prefabs/UI/GiftUI.prefab`（Phase 2 新建）
  - `Assets/Scenes/SampleScene.unity`（Hub 场景，Coordinator 在此拖线）
- 数据资产
  - `Assets/_Data/SpaceLife/Dialogue/`（`DialogueDatabaseSO` / `DialogueGraphSO` authoring 目录）
  - `Assets/_Data/SpaceLife/NPCs/`（`NPCDataSO` 资产）
- Input
  - `Assets/Input/ShipActions.inputactions`（Hub 模式下使用 "SpaceLife" ActionMap）
- 相关文档
  - 设计稿：`Docs/2_TechnicalDesign/SpaceLife/SpaceLife_HubDialogue_SystemDesign.md`
  - Master Plan：`Docs/0_Plan/ongoing/2026-04-22-spacelife-hub-dialogue-master-plan.md`
  - 04-21 MVP Plan：`Docs/0_Plan/ongoing/2026-04-21-spacelife-hub-dialogue-mvp-implementation-plan.md`（被 Master Plan 统摄）

> **说明：** SpaceLife 目前没有独立的 `CanonicalSpec` / `AssetRegistry`。在 Master Plan 执行完成并归档前，本章节同时承载"治理约束"和"现役权威口径"。Phase 4 结束后，现役 owner / 路径 / 状态判断会迁移至 `SpaceLife_HubDialogue_SystemDesign.md`，本章节回归纯治理职责。

### 12.2 模块目标

SpaceLife / Dialogue 模块的治理目标：

- **Domain / UI / Editor 三层边界清晰**：Domain 只产出 ViewModel，UI 只消费，Editor 工具只 Audit / Preview，禁止三层交叉写入
- **避免 Setup Window 再次膨胀成"祖传工具"**：1,630 行 SetupWindow 是本项目最大的技术债之一，本轮明确冻结，后续重写走独立 Plan
- **让新增 NPC / 对话分支无需重读整套 Domain 代码**：authoring → validator → runner → presenter 是一条干净的单向链路
- **治理成熟度 15% → 60%+**：通过 Authority Matrix + 3 个必做 Validator + Scene Override 白名单 + 7 条设计决策落地

换句话说：

> **下一个 NPC 只改数据，不改架构。**

### 12.3 Authority Matrix（五层权威表）

> 本表与 Master Plan §4.1 的 Authority Matrix 同步。若后续 Master Plan 修正此表，以 Master Plan 为准并同步回来。

| 范围 | 对象 / 引用类型 | 唯一权威入口 | 明确禁止的并行写入者 | 备注 |
|---|---|---|---|---|
| **Domain 数据** | `DialogueGraphSO` / `DialogueDatabaseSO` | 手工 authoring in `Assets/_Data/SpaceLife/Dialogue/` | Runtime 修改 SO、Editor 工具自动生成 graph 内容 | 运行时污染禁止写回 authored asset（对齐 §9.3 Runtime 不修改 SO） |
| **Runtime 状态** | session state / context / flags / relationship | `DialogueRunner`（session） + `DialogueFlagStore`（flags） + `RelationshipManager`（relationship） | UI 直接修改业务状态、NPC 脚本自己按 index 选台词 | UI 只消费 ViewModel |
| **UI Prefab 结构** | `DialogueUI.prefab` / `GiftUI.prefab` | 唯一一个 Editor 构建器（Phase 2 创建，暂定 `SpaceLifeUIPrefabBuilder`）只做 Apply | `SpaceLifeSetupWindow`、Scene 手工拼装、Runtime fallback | 禁止 Setup Window 参与 UI Prefab 构建 |
| **Scene-only 接线** | `SpaceLifeDialogueCoordinator` 在 `SampleScene` 中的引用拖线（UI 实例、Router、Database） | 手工拖线（Phase 3），或唯一一个 Scene Binder（若 Phase 5 实现） | Runtime 自动 Find、SetupWindow 自动回填 | Scene-only 引用必须写进 Scene Override 白名单（见 §12.4） |
| **Debug / Setup 工具** | `SpaceLifeSetupWindow` 等 Editor 工具 | 只允许 Audit / Preview（Phase 4 限定） | 在 Play Mode / `OnValidate` / `Awake` 中隐式接管主链 | Phase 4 明确降权，Phase 5 可选重写 |

### 12.4 Scene Override 白名单（Phase 3 定稿版）

> 本小节自 Phase 0 初稿升级为 Phase 3 **定稿**（2026-04-22）。依据 Phase 3 §7.4 要求，补充 Coordinator 接线模式转为 ServiceLocator、UIRoot 父挂载、历史 SerializeField 引用退役三项细节。

#### 允许的 Scene Override / Scene-only 数据

1. **UI Root Prefab 实例化**：`SpaceLifeUIRoot.prefab` 在 `SampleScene` 中作为 `SpaceLifeDialogueSampleSlice` 的子节点实例化（Prefab instance，允许 parent + position 作为 scene-only，不算非法 override）
2. **Canvas 视觉层叠**：`SpaceLifeUIRoot` / `DialogueUI` / `GiftUI` 实例的 `Canvas.sortingOrder`（若场景需要特定层叠调整）
3. **Coordinator 场景级资产引用**：`SpaceLifeDialogueCoordinator` 对 `DialogueDatabaseSO` 资产和场景中 `DialogueServiceRouter` 的 `[SerializeField]` 拖线（这两项仍保留 SerializeField，属于明确允许的 scene-only 接线）

#### 必须跟随 Prefab / Builder，不允许漂移的内容

- `DialogueUI.prefab` / `GiftUI.prefab` / `OptionButton.prefab` 内部子节点结构
- `SpaceLifeUIRoot.prefab` 作为 nested prefab root 的结构
- `CanvasGroup` 初始值（alpha=0 / interactable=false / blocksRaycasts=false）
- RectTransform 布局（anchor / pivot / sizeDelta / anchoredPosition）
- Presenter 脚本（`DialogueUIPresenter` / `GiftUIPresenter`）内部的 SerializeField 引用
- 任何数值字段（字体大小、颜色、padding、间距、spacing 等）

#### Phase 3 落地后已退役的接线模式

- ❌ `SpaceLifeDialogueCoordinator._dialogueUI` 的 `[SerializeField]` 拖线 —— Phase 3 §7.1 已取消 SerializeField，字段改为纯私有并通过 `ServiceLocator.Get<IDialoguePresenter>()` 解析。场景中若残留该字段的 override 必须清除。
- ❌ Runtime 自动 `FindObjectsOfType<DialogueUI>()` fallback —— Phase 2 重构时已全部删除。
- ❌ `SpaceLifeSetupWindow` 对 `DialogueUI` / `GiftUI` 的回填能力 —— 由 §12.7 冻结声明统一禁止。

#### 处理规则（漂移发生时）

若发现"不允许 override"的字段在场景中漂移：

1. **不得**用 Runtime fallback 补救（对齐 §3.3 / §3.5）
2. 用 `ProjectArk > Space Life > Audit UI Prefab Overrides` 菜单定位漂移项
3. 确认该字段的权威 owner（Prefab Builder / Prefab 本身 / 设计决策）
4. 清理错误 override（Revert to Prefab Instance Overrides）
5. 若同类漂移重复出现，把该字段加入 `SpaceLifeUIOverrideAudit` 黑名单检查项

### 12.5 Phase 0 设计决策锁定（7 条）

> 本清单是 Master Plan v1.1 §4.2 的**规则层固化**。决策一经落地，后续 Phase 1-5 不得随意推翻，若需变更必须先更新 Master Plan 并同步本节。

| # | 决策 | 依据 | 违反此决策的表现 |
|---|---|---|---|
| D1 | 保留 `DialogueUI` 组件名，重构为 Presenter（不新建第二套面板） | 避免双轨 UI authority | 出现 `NewDialoguePresenter` / `DialogueUI_v2` 等并行类 |
| D2 | `RelationshipManager` 直接读写 `PlayerSaveData`，不改 `SaveBridge` | 避免 `Level → SpaceLife` 反向耦合 | `SaveBridge` 中出现 `RelationshipManager` 引用 |
| D3 | `Upgrade` / `Intel` / `RelationshipEvent` 统一走 `DialogueServiceRouter`，不在节点里 `OpenXXXUI()` | 对话只产出结果，不持有别系统逻辑 | `DialogueNode` 里出现 `UpgradeUI.Open()` 直调 |
| D4 | 新 graph 不复用 `NPCDataSO.DialogueNodes`，另建 `DialogueGraphSO` | 旧结构 index-based、NPC 专属 | Phase 2+ 的新对话仍从 `NPCDataSO.DialogueNodes` 取数 |
| D5 | MVP 隐藏不满足条件的选项，不做 disabled reason | 减少 Presenter 复杂度 | Presenter 出现 `ShowDisabledWithReason(reason)` 分支 |
| D6 | **UI Prefab 体系从 0 建，不复用 SetupWindow 现有产物** | 避免把旧 UI owner 双轨带入新系统 | Phase 2 的 `DialogueUI.prefab` 由 SetupWindow 回填 |
| D7 | **SetupWindow 在 Phase 1-3 期间冻结，禁止执行 Rebuild** | 用执行约束替代推翻重写 | Phase 1-3 期间有人跑 SetupWindow 的 Rebuild 操作 |

### 12.6 实现规则

#### R1. Domain 不持有 UI 接口，UI 接口由 Core 定义

- `IDialoguePresenter` / `IGiftPresenter` 定义在 `Assets/Scripts/SpaceLife/Dialogue/` 下（作为 Core 层接口）
- `DialogueRunner` / `DialogueCoordinator` 只通过 `ServiceLocator.Get<IDialoguePresenter>()` 获取，**不得**直接引用 `DialogueUI` 类型
- **根因：** Phase 5 可能需要替换 UI 实现（如原生 UI Toolkit），接口隔离为此留活口

#### R2. HubLock 采用引用计数而非 bool

- `SpaceLifeManager.AcquireHubLock(object owner)` / `ReleaseHubLock(object owner)`
- bool 版本（如曾存在）必须在 Phase 1 内废弃
- **根因：** 对话 + 礼物 + 升级三类 UI 可能同时触发 lock，bool 会导致"第一个释放者把所有锁都解了"

#### R3. `AssetDatabase.FindAssets` 类模糊查找必须收口到单一 Locator

- Editor-only 场景下定位 `ShipActions.inputactions` 等关键资产，只允许通过 `ShipInputActionLocator`（Phase 1 P4 抽取）
- Runtime 消费者调用 `#if UNITY_EDITOR` + `ShipInputActionLocator.FindShipActionsAsset()`，**禁止**自己写 `AssetDatabase.FindAssets(...)` 字符串查询
- **根因：** 同类违规在 `PlayerController2D` / `PlayerInteraction` / `SpaceLifeInputHandler` 三处重复 = 典型第二真相源（对齐 §3.8 硬编码治理）

#### R4. ActionMap 语义边界必须与模式边界一致

- Hub 模式下的交互键（`Interact`）必须定义在 "SpaceLife" ActionMap 中，**不得**跨 map 读 "Ship" map 的键
- 若某按键同时被 Ship 模式和 SpaceLife 模式使用，两个 map 中各自独立声明，不共享 action
- **根因：** ActionMap 切换的语义是"切换输入上下文"，跨 map 读键会导致 Rebind UI 与多键位冲突时定位困难

#### R5. 所有 SpaceLife 面板必须用 CanvasGroup 控显隐

- `DialogueUIPresenter` / `GiftUIPresenter` / `MinimapUI` 及未来所有 SpaceLife UI 面板
- GameObject 始终保持 `active=true`，`Awake()` 中只初始化 CanvasGroup 状态
- **禁止** 调用 `SetActive(false)` / `SetActive(true)` 控制面板显隐
- **根因：** 对齐 §10 UI 模块规则与 CLAUDE.md 已知陷阱（SetActive 会推迟 Awake，历史已踩坑）

#### R6. Legacy API 必须显式标注 `[System.Obsolete]`

- `NPCDataSO` 的 `_dialogueNodes` / `GetNodeAt()` / `GetEntryLine()` / `*EntryIndex` 是旧原型 API
- 在 Phase 2 删除 Legacy 走线前，这些 API 必须带 `[System.Obsolete("...", false)]` 标注
- 标注中必须写明"预计退役时间"与"替代方案"
- **根因：** 对齐 §3.11 迁移纪律——旧路径不删，复杂度不会自己消失；`[Obsolete]` 是最低成本的降级信号

#### R7. SetupWindow 冻结期禁令

- **SpaceLifeSetupWindow 自 Phase 4（2026-04-23）起进入长期冻结**，详细约束见 §12.11
- 冻结期间，任何人（含 AI Agent）**禁止**：
  - 在 `SpaceLifeSetupWindow` 中新增功能
  - 执行 Setup Window 的 Rebuild / Apply 类操作
  - 依赖 Setup Window 回填字段作为"正确初始状态"
- Setup Window 必须保持三处冻结声明：`Implement_rules.md §12.11` + 代码注释 + Editor 警告 HelpBox
- **根因：** 1,630 行的 Setup Window 是当前 SpaceLife 最大的 authority 污染源，冻结 = 用执行约束替代推翻重写；若 Phase 5 长期不启动，冻结状态即为合理稳态

#### R8. Silent No-Op 禁令（SpaceLife 版）

- Coordinator 缺 Presenter 引用、Runner 缺 Database、Router 缺 Service 时，**禁止**静默 `return`
- 至少满足以下之一（对齐 §3.5）：
  - `Debug.LogError(...)` 带明确上下文
  - Inspector 红字提示
  - `Coordinator Dependency Audit` 报告（Phase 3 必做 Validator 之一）
- **根因：** SpaceLife 历史上多次因为缺字段静默失败，浪费数小时排查

### 12.7 改动前必须回答的 5 个问题

对 SpaceLife / Dialogue 做任何非微小改动前：

1. 这次改动的**唯一运行时入口**是谁？（Runner / Coordinator / Presenter / Manager？）
2. 涉及的 **UI Prefab** 谁负责构建？（`SpaceLifeUIPrefabBuilder` 还是手工？）
3. 涉及的 **Scene-only 引用**是否在白名单中？（§12.4）
4. 是否会触发 **Setup Window 的冻结红线**？（R7）
5. 是否在违反 **D1-D7 的任一条决策**？（§12.5）

若任一问题答不清，不应直接编码——先补文档或对齐 Master Plan。

### 12.8 踩坑总结（持续追加）

#### 坑 1：`DialogueUI` 旧 Legacy 走线与新 Presenter 并存导致双轨

- **现象：** 新对话节点触发后，画面显示为旧 `DialogueUI.ShowDialogue(line)` 的硬编码 UI，新 Presenter 的 ViewModel 被静默吞掉
- **根因：** Legacy 方法未删除 + 没有 `[Obsolete]` 警告，新代码容易误调旧 API
- **防御：** Phase 1 给 `NPCDataSO` Legacy 字段打 `[Obsolete]`；Phase 2 删除 `DialogueUI.ShowDialogue(line)` 的 Legacy 走线

#### 坑 2：`SpaceLifeSetupWindow` 自动回填 Coordinator 引用

- **现象：** Coordinator 的 Database 引用被 Setup Window 在 `OnValidate` 阶段隐式写回，导致手工清空后下次保存又恢复
- **根因：** Editor 工具越权写 Scene-only 引用，违反 Authority Matrix 第 3 / 4 行
- **防御：** Setup Window 冻结声明 + Phase 3 必做 `Coordinator Dependency Audit` 确认引用来源

#### 坑 3：`PlayerInteraction` 从 "Ship" ActionMap 读 `Interact`

- **现象：** Hub 模式下按 E 能触发交互（表现正常），但按键解绑逻辑写在 "Ship" map 里，未来做 Rebind UI 时会出现"SpaceLife 模式下也能解绑 Ship 模式的键"的语义混乱
- **根因：** ActionMap 语义边界与模式边界不一致（R4）
- **防御：** Phase 1 P5 迁移 `Interact` 到 "SpaceLife" map

#### 坑 4：`AssetDatabase.FindAssets("ShipActions t:InputActionAsset")` 在 3 个文件重复

- **现象：** 三个独立 Runtime 类在 `#if UNITY_EDITOR` 块中各自写字符串查询，`ShipActions.inputactions` 一旦改名或移动，三处都要同步
- **根因：** 没有 Locator 抽象层（R3）
- **防御：** Phase 1 P4 抽取 `ShipInputActionLocator`

#### 坑 5：`SetActive(false)` 初始化顺序坑（UI Presenter 落地时踩到）

- **现象：** 新构建的 `DialogueUIPresenter` / `GiftUIPresenter` Prefab 放进场景后，第一次 `Open()` 调用不生效 —— 明明走到了 `CanvasGroup.alpha=1` 的赋值逻辑，画面仍然是黑的
- **根因：** 早期 Editor 工具曾在生成 Prefab 时顺手把 Panel GameObject 设成 `m_IsActive: 0`（被序列化进 Prefab）。`SetActive(false)` 会推迟 `Awake()` 执行，Presenter 的 `CanvasGroup` 缓存与 `ServiceLocator.Register` 都发生在 `Awake()`，于是首次 `Open()` 里访问到的是 **未初始化的引用**；即使后续把 GameObject SetActive(true)，Register 也已错过 `SpaceLifeDialogueCoordinator.Start()` 的解析时点，Coordinator 因为 Audit 失败把自己 `enabled = false`
- **防御：**
  1. `SpaceLifeUIPrefabBuilder` 在生成 Prefab 时必须确保所有 UI 根节点 `GameObject.activeSelf = true`
  2. Prefab 内所有 Panel 的初始显隐状态统一用 `CanvasGroup(alpha=0, interactable=false, blocksRaycasts=false)` 表达，**绝不调用 `SetActive`**（R5）
  3. 发生过该坑的 Prefab 被修复后，需在 Unity Editor 中手动确认根节点的 active 勾选框是开启状态并保存场景；历史遗留的 `m_IsActive: 0` 不会因代码修复而自动消失（对齐 CLAUDE.md 常见陷阱表"uGUI 面板禁止用 SetActive 控制显隐"）

#### 坑 6：UI owner 双轨（`DialogueUI` 与 `NPCInteractionUI` 并存）

- **现象：** Phase 2 之前，`NPCController` 同时持有对 `DialogueUI` 和 `NPCInteractionUI` 两套 UI 的引用；修改对话交互路径时，有些修改落到 `DialogueUI`、有些落到 `NPCInteractionUI`，出现"看起来改了但没生效"的高频 bug
- **根因：** UI authority 同时有两个合法 owner；修改任一方都只是局部正确
- **防御：**
  1. Phase 2 删除 `NPCInteractionUI`，NPC 的交互引导感（高亮 / 可交互提示）由场景实例的 Interactable 组件承担，对话面板唯一 owner 为 `DialogueUIPresenter`
  2. 新的 UI 面板进入 SpaceLife 前，必须先回答"它替代谁、与谁并存、是否双轨"（§12.7 第 1-2 问）
  3. 审计机制：`SpaceLifeUIOverrideAudit` 扫描场景中 `IDialoguePresenter` / `IGiftPresenter` 实例数，>1 时报错（防止同一接口被多个 Presenter 同时注册）

#### 坑 7：Silent No-Op 让"看起来什么都没发生"

- **现象：** Coordinator 的 Database 引用忘拖、或 Presenter 未挂场景时，按 E 触发 NPC 没有任何反应、Console 也无错 —— 排查路径会被误导到"按键绑定"或"碰撞体"
- **根因：** Coordinator 早期版本的 `StartDialogueFromNpc` 带 `if (_database == null) return;` silent return，关键依赖缺失不响亮失败
- **防御：**
  1. 所有关键依赖改由 `AuditDependenciesOnStart()` 汇总检查，缺任一项 `Debug.LogError` 包含完整缺失清单并 `enabled = false`
  2. 入口函数前置 `if (!_dependencyAuditPassed) return;` 闸门（配合 `Debug.LogError` 上下文提示 "Coordinator disabled due to failed audit"），而不是 silent
  3. 通用原则：SpaceLife 模块内**任何** `if (x == null) return;` 都要被视为可疑代码，除非有注释说明为什么是合法的静默
  4. Phase 5 若启动，`SilentFailScanner` 会把全模块的此类代码清单化

### 12.9 验收清单

每次对 SpaceLife / Dialogue 做完改动后，至少检查：

1. 是否新增了第二真相源？（Domain / UI / Editor 是否交叉写入）
2. 是否新增了 `AssetDatabase.FindAssets` / `GameObject.Find` / fallback 分支？
3. 是否触发了 SetupWindow 冻结红线？（R7 / §12.11）
4. 涉及的 Scene Override 是否都在白名单中？（§12.4）
5. 关键依赖缺失时，是否会 Silent No-Op？（R8）
6. 是否违反了 D1-D7 任一条设计决策？（§12.5）
7. 是否需要新增 / 更新 Validator？（`DialogueDatabaseValidator` / `UI Prefab Override Audit` / `Coordinator Dependency Audit`）

#### Phase 4 专项验收（2026-04-23 追加）

Master Plan v1.1 Phase 4 完成判据，后续若再启动 Plan 级工作时复用同一检查口径：

1. **设计稿已同步到现役真相源：** `Docs/2_TechnicalDesign/SpaceLife/SpaceLife_HubDialogue_SystemDesign.md` 至少追加 "实现落地状态" 章节，明确 runtime 文件清单 / 存档方案 / 接口路径 / UI Prefab 权威入口 / Audit 口径 / SetupWindow 冻结状态六项
2. **ImplementationLog 完整：** `Docs/5_ImplementationLog/ImplementationLog.md` 包含 Phase 0 / Phase 1 / Phase 2 / Phase 3 / Phase 4 各阶段的独立记录，按日期倒序编排
3. **旧 Plan 归档：** `2026-04-21-spacelife-hub-dialogue-mvp-implementation-plan.md` 与 `2026-04-22-spacelife-hub-dialogue-master-plan.md` 均已移入 `Docs/0_Plan/complete/`，`Docs/0_Plan/ongoing/README.md` 同步更新
4. **SetupWindow 冻结声明三处就位（详见 §12.11）：**
   - `Implement_rules.md §12.11` 冻结执行约束
   - `Assets/Scripts/SpaceLife/Editor/SpaceLifeSetupWindow.cs` 顶部 FROZEN 注释
   - `SpaceLifeSetupWindow.OnGUI()` 顶部红色 `EditorGUILayout.HelpBox`
5. **`dotnet build Project-Ark.slnx` 通过**，0 警告 0 错误

### 12.10 推荐工作流

#### 新需求（新 NPC / 新对话分支）

1. 确认属于 **Domain 数据层** 改动 → 直接 authoring `DialogueGraphSO` / `DialogueDatabaseSO`
2. 运行 `DialogueDatabaseValidator`（Phase 1 后可用）
3. Play Mode 验证
4. **不要**改 Runner / Presenter / Coordinator 代码

#### 难定位 bug

1. 先 `read_console` 看日志（对齐 MCP 使用原则）
2. 确认触发的是 **新 Presenter** 还是 **Legacy 走线**（坑 1）
3. 确认 Coordinator 的引用是否被 SetupWindow 隐式接管（坑 2）
4. 确认 Scene Override 是否漂移（§12.4）
5. 若以上都不是根因，补 Validator / Audit，而不是加 Debug.Log

#### 新功能（Phase 5 之后）

1. 先读 `SpaceLife_HubDialogue_SystemDesign.md` 确认现役 owner
2. 对照 Authority Matrix 确认新功能应落到哪一层
3. 若触发 D6 / D7 / R7 的红线，必须先开新 Plan 推翻对应决策

### 12.11 `SpaceLifeSetupWindow` 冻结声明（Phase 4 定稿）

> 本节是 Master Plan v1.1 Phase 4 §8.3 要求的**执行约束层声明**，与代码顶部 FROZEN 注释、Editor HelpBox 并列构成三处冻结证据。  
> 本声明效力 **长期有效**，直至且仅直至 Phase 5（推翻重写）启动并完成。若 Phase 5 长期不启动，本冻结状态即为 SpaceLife 模块的**合理稳态**，不是"待办"。

#### 冻结对象

- `Assets/Scripts/SpaceLife/Editor/SpaceLifeSetupWindow.cs`（1,630 行遗留 Editor 工具）

#### 冻结内容

以下操作在冻结期间 **一律禁止**，无论执行者是人还是 AI Agent：

1. 通过 `SpaceLifeSetupWindow` 的任何 Rebuild / Apply / Regenerate 按钮执行写入
2. 依赖 `SpaceLifeSetupWindow` 生成或回填 `DialogueUI.prefab` / `GiftUI.prefab` / `SpaceLifeUIRoot.prefab` / `OptionButton.prefab` 的结构或序列化字段
3. 依赖 `SpaceLifeSetupWindow` 自动回填 `SpaceLifeDialogueCoordinator` 在场景中的引用（Database / Router / Presenter）
4. 在 `SpaceLifeSetupWindow` 中新增任何功能（新增按钮 / 新增分区 / 新增菜单）
5. 把 `SpaceLifeSetupWindow` 设为"推荐入口"写进任何新文档或新 Plan

#### 冻结期间的合法操作

- 阅读代码（理解历史逻辑）
- 为 Phase 5 写重写 Plan 时作为需求溯源参考
- 作为反面教材在 Implement_rules 中被引用

#### 现役权威入口（冻结期间的替代方案）

| 任务 | 冻结前入口 | 现役唯一入口 |
|---|---|---|
| 生成 / 更新 SpaceLife UI Prefab | `SpaceLifeSetupWindow.Rebuild` | `ProjectArk > Space Life > Build UI Prefabs (Apply)` |
| 审计 Scene 中 UI Prefab override | （无） | `ProjectArk > Space Life > Audit UI Prefab Overrides` |
| 校验 `DialogueDatabase` 完整性 | （无） | `ProjectArk > Validate Dialogue Database` |
| Coordinator 依赖接线 | `SpaceLifeSetupWindow` 自动回填 | 手工拖线 `DialogueDatabase` + `DialogueServiceRouter`，Presenter 由 `ServiceLocator` 自动解析（详见 §12.4 / §14.3） |

#### 冻结解除条件

**唯一合法解除路径**：启动 Master Plan 所定义的 Phase 5 "SetupWindow 推翻重写"，完成以下全部工作后方可解除：

- 拆分为 `Audit` / `Preview` / `Apply` 三模式（对齐 §3.14 Builder 执行模式规则）
- 所有 silent no-op 分支移除
- 完整集成到 `Implement_rules.md §12` 规则体系
- 更新本节记录冻结解除日期与原因

在此之前，**任何绕过冻结的尝试（包括"只跑一次看看"）都视为违反治理规则**。

#### 违反冻结时的处理

若发现冻结被违反：

1. 立即回滚本次 Rebuild / Apply 产生的 Prefab / Scene 变更
2. 在 `ImplementationLog.md` 记录违规事件与回滚操作
3. 若 Scene 中出现未预期的 override，执行 `Audit UI Prefab Overrides` 菜单清理（§12.4 处理规则）

---

## 13. 后续可追加模块（占位）

后续可按同样结构继续追加：

- `Enemy`
- `Save`
- `Map / Minimap`
- `Audio`

新模块首次追加时，**不要求一开始就补齐全部章节**。按需启用，逐步沉淀。

---

## 14. 通用模块规则模板

> 本节定义当新模块需要在 `Implement_rules.md` 中追加规则时，应遵循的统一结构。
>
> **触发条件**（满足任一即应追加）：
>
> - 同类 bug 在该模块连续出现两次以上
> - 多个入口（Runtime / Editor / Scene / Debug）同时改同一条链路
> - 排查时间明显长于实现时间
> - Editor 工具 ≥3 个且职责开始重叠
> - 新功能开发时需要先"考古"才能动手
>
> **与架构速写的关系**：
>
> - `Docs/2_TechnicalDesign/{ModuleName}/{ModuleName}_ArchBrief.md`：描述模块**是什么**——结构、职责、驱动关系
> - `Implement_rules.md` 中的模块规则：描述模块**怎么改更不容易烂**——实现约束、踩坑防御、验收清单
> - 两者互补，不重复。架构速写回答"谁管什么"，实现规则回答"改的时候要注意什么"

### 14.1 模块规则统一结构

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

### 14.2 规则质量标准

- **可执行**：每条规则都能转化为具体检查动作，禁止模糊表述（如"注意代码质量"）
- **有根因**：每条规则都要说明"为什么"，没有根因的规则不写
- **不重复**：与 `CLAUDE.md` 的架构原则 / 代码规范不重复；此处只写该模块特有的约束
- **不过时**：定期审查，已不再适用的规则标记 `[已废弃]` 并说明原因，不要静默删除

### 14.3 模块规则与架构速写的联动


| 时机                   | 架构速写 (`_ArchBrief.md`) | 模块规则 (`Implement_rules.md`)  |
| -------------------- | ---------------------- | ---------------------------- |
| 新模块开发前               | ✅ 必须产出（工作流第 4 步）       | ❌ 不需要，还没踩坑                   |
| 首次踩坑后                | 可能需要更新驱动关系             | ✅ 追加踩坑总结 + 对应防御规则            |
| 模块复杂度增长              | Lv.1 → Lv.2 → 完整 Spec  | 按需追加实现规则、工具矩阵                |
| 多人 / 多 AI session 协作 | 确保结构描述准确               | ✅ 追加协作约束（如 authority matrix） |
| 架构重构后                | 重写或大幅更新                | 清理过时规则，补新规则                  |


### 14.4 已有模块的规则参考

- **Ship / VFX**：见本文档 Section 2-6（当前最完整的模块规则范例）
- **Level**：见本文档 Section 7（现役 authoring / validation 范例）
- **全局 Unity / Editor 治理**：见 Section 8（跨模块底层 guardrails）
- **Core / Infrastructure / UI / Combat / Projectile**：见 Section 9-11（由 `CLAUDE.md` 迁入的首批通用陷阱沉淀）
- **SpaceLife / Dialogue**：见 Section 12（2026-04-22 Master Plan v1.1 Phase 0 沉淀，含 Authority Matrix 五层 + Scene Override 白名单 + 7 条设计决策锁定）

