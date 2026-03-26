# Ship VFX Migration Plan

## 1. 目标

本计划用于指导 `Ship` / `BoostTrail` / `VFX` 的**渐进式规范化与体验重构**。

当前它分成两个阶段：

- **阶段 A：规范化治理** —— 统一命名口径、结构职责、文档角色、资产路径表达方式，以及 Editor 工具的真相源归属
- **阶段 B：体验重构** —— 以 `MainTrail` 当前满意读感为参考，但不冻结其本体；所有现役 `Ship VFX`（包含 `MainTrail`）都可在保持同类效果的前提下逐项重构、逐项验收

## 2. 本轮策略

采用 **方案 A：先规范化，再按特效单元逐项重构**。

MVP 原则：

1. **先立规范**：先产出 canonical spec、asset registry、migration plan
2. **先统一 owner**：先让工具职责边界清楚，再谈 rename / move
3. **先 alias，后 rename**：先统一语义名，不急着改物理名
4. **优先确定性路径**：减少 `AssetDatabase.FindAssets` 这类模糊搜索导致的误绑
5. **先锁读感，不锁实现**：`MainTrail` 当前满意的是**主观效果**，不是具体实现；若其结构、流程或技术栈不够干净，应直接改到更符合规范，只需保持同类读感
6. **优先复用成熟原则，不盲从现实现状**：后续 `Ship VFX` 重构优先参考 `MainTrail` 当前想保留的读感目标与 clean 原则，而不是把它此刻的节点形态、脚本结构或技术选型当成不可质疑的模板
7. **单特效推进**：体验重构阶段一次只讨论一个特效或一个紧耦合效果簇，避免多层一起动导致判断失真
8. **玩家读感优先**：每次讨论都以"玩家此刻应该感受到什么"为准，而不是先看工程实现是否精巧

## 3. 批次拆分

## Batch 1 — 规范与映射（当前批）

目标：

- 建立 `ShipVFX_CanonicalSpec.md`
- 建立 `ShipVFX_AssetRegistry.md`
- 建立 `ShipVFX_MigrationPlan.md`
- 收口现役与参考、dormant、legacy 的边界

完成标准：

- 文档中能找到当前现役链路、owner、冻结项和 canonical alias
- `Ship_VFX_Player_Perception_Reference.md` 不再承担规范权威职责
- `BoostTrail_Shader_Implementation_Status.md` 不再被误读成总规范文档

## Batch 2 — 工具职责收口

目标：

- `ShipPrefabRebuilder` 正式接管 `Ship.prefab` 全部结构、组件、接线（含原 `ShipBuilder` 职责）
- `BoostTrailPrefabCreator` 明确只负责 `BoostTrailRoot.prefab`
- `MaterialTextureLinker` 只维护现役材质并优先精确路径回填
- `ShipBoostTrailSceneBinder` 明确只处理 scene-only 引用
- `ShipBuilder` 已删除（2026-03-21），职责已合并到 `ShipPrefabRebuilder`

完成标准：

- 不再出现多个工具并行定义同一结构的情况
- `BoostTrailRoot` 的 prefab 生成、ship 集成、scene 绑定三者边界清楚
- 菜单路径和注释口径与职责一致

## Batch 3 — 高收益命名收口

目标：

- 修正文档、注释、菜单、内部常量中的高收益歧义命名
- 统一 `ShipVisual` / `VisualChild` 口径
- 统一 `FlameTrail_B = LeftFlameTrail` 的语义表达
- 为 `ShipShipState`、`Ship_Sprite_HL` 等冻结物理名补充规范 alias

完成标准：

- 新文档和新代码中不再把历史物理名直接当语义名使用
- 讨论和变更记录统一使用 canonical 术语

## Batch 4 — 低风险 dormant 清理与物理迁移评估

目标：

- 审计 dormant `.png` / `.mat` / `.shader` 的真实引用
- 清理已确认不再被 `scene / prefab / runtime / live material chain` 使用的低风险资源
- 更新 `ShipVFX_AssetRegistry.md` 与 `CopyGGTextures.ps1`，避免已清退资源被重新导入
- 继续评估是否需要重命名 active `.png` / `.mat` / `.shader` / prefab 节点
- 继续评估是否需要把目录收束到更强的 feature-owned 结构

前提：

- 资产注册表稳定
- 工具职责已收口
- Play Mode、Prefab、Scene 验证闭环稳定

完成标准：

- 首批 dormant 资源完成 GUID + 文本双重引用审计
- 已删除资源不会被 `CopyGGTextures.ps1` 再次复制回仓库
- Unity 刷新后无新的 missing asset / missing shader 错误
- 文档只保留当前仍存在的资源与路径

## Batch 5 — 冻结高歧义物理名改名前审计

目标：

- 明确 `FlameTrail_B`、`Ship_Sprite_HL`、`ShipShipState` 的当前风险等级
- 把"可改 / 不可改 / 需 Editor 配合"结论写回规范层，避免后续重复审计
- 把 prefab 节点改名与纹理 / 材质迁移拆开，避免一次性联动过大
- 为下一批真正的物理 rename 准备最小安全动作清单

完成标准：

- `FlameTrail_B` 明确标记为"需 Editor 配合"，并写清 `BoostTrailPrefabCreator` + `_flameTrailB` 依赖
- `Ship_Sprite_HL` 明确标记为"需 Editor 配合"，并写清 `ShipPrefabRebuilder` + `_hlRenderer` 依赖
- `ShipShipState` 明确标记为"不可改（当前批）"
- 文档中明确 `Ship_Sprite_HL` 当前 live prefab 使用共享默认材质链，不与 `ShipGlowMaterial.mat` 绑定迁移

## Batch 6 — Ship VFX 体验重构 Backlog（当前执行批）

目标：

- 把 `MainTrail` 定位为**当前满意读感参考**，而不是不可碰的冻结基线；若本体存在结构冗余、流程不规范或实现过重，允许直接精简、重构或替换
- 把全部现役 `Ship VFX`（包含 `MainTrail`）整理成可逐项讨论、逐项决策、逐项验收的 backlog
- 让 `Ship_VFX_Player_Perception_Reference.md` 继续负责"玩家看到了什么"，而本计划负责"我们接下来按什么顺序改"
- 为后续逐项重构提供统一决策位：**保留 / 重做 / 合并 / 删除 / 延后**

完成标准：

- `MainTrail` 被明确标记为"满意读感参考 / 可重构"
- 全部现役 `Ship VFX` 均进入 backlog 清单，`MainTrail` 不再被排除在外
- backlog 以玩家体验阶段分组，并附带建议优先级
- 后续每次讨论都能直接从本节选一项推进，而不需要重新盘点现役效果

### 6.1 读感参考基线

| 条目 | 当前结论 | 原则 |
| --- | --- | --- |
| `MainTrail` | **满意读感 / 可重构** | 保留其当前 Boost 持续主读感与能量语系，但实现、结构、工具链若不够干净，可直接重做；目标是**效果类似而不是实现照抄** |

#### 6.1.1 `MainTrail` 读感与 clean 结构参考原则

后续文档里所有"**沿用 `MainTrail` 方案**"的表述，都应理解为：**优先参考它想保留的主观读感与 clean 结构原则，而不是把当前实现细节视为固定模板**。

默认优先参考的是以下原则：

1. **清晰的单一主载体**：先确定一个真正承担主读感的载体（例如 `TrailRenderer`、`ParticleSystem`、`SpriteRenderer`），避免多个层同时争主角。
2. **材质 / Shader 驱动优先**：优先把风格塑形放进材质和 Shader，而不是靠堆很多节点或很多一次性小特效去拼。
3. **少量关键运行时参数驱动**：参考 `MainTrail` 当前读感里最有效的关键参数驱动方式，尽量避免同一效果拆成很多互相独立的开关。
4. **单一现役主链**：优先保持一个明确的 live 材质 / 贴图 / prefab 链路，避免重新引入"现役链 + 兼容链 + fallback 链"并存的结构复杂度。
5. **Editor 装配与 Runtime 播放分离**：Prefab 结构和材质接线归 Editor 工具，运行时脚本只负责播放、过渡和参数驱动。
6. **先做可调 MVP，再加细节层**：先做一条能在 Play Mode 里稳定成立的主效果，再决定是否需要辅层，而不是一开始就把层数铺满。
7. **效果相似优先于实现相似**：只要玩家读到的持续推进感、受控高能感、主尾迹权重仍成立，就允许替换当前节点结构、材质写法、脚本组织甚至主载体技术。

如果某个条目最终确认**不适合**参考 `MainTrail` 当前读感或 clean 原则，需要在对应重构卡片里明确写出原因，而不是默认照抄，也不是默认另起炉灶。

### 6.2 逐项评审规则

后续我们一项一项过时，统一回答 6 个问题：

1. **目标**：玩家此刻应该感受到什么？
2. **问题**：当前版本最不对劲的地方是什么？
3. **决策**：保留 / 重做 / 合并 / 删除 / 延后？
4. **技术参考**：这一项是否应参考 `MainTrail` 当前想保留的读感与 clean 原则？如果 `MainTrail` 本体自己也更适合被重构，那就直接改它，而不是被它反向绑住。
5. **范围**：只改运行时参数、Prefab 结构、材质 / Shader，还是需要连同脚本触发一起改？
6. **验收**：进入 Play Mode 后，出现什么主观读感才算通过？

建议推进顺序：

1. **MainTrail + Boost 持续层**（按耦合簇推进；若规范化或简化收益明显，优先直接改 `MainTrail`）
2. **Boost 起手层**
3. **Boost 收尾层**
4. **Dash 爆发层**
5. **常驻基底层**

### 6.3 当前 backlog 总表

| 阶段 | 条目 | 当前 owner / 驱动 | 当前状态 | 建议处理 | 优先级 |
| --- | --- | --- | --- | --- | --- |
| 基线 | `MainTrail` | `BoostTrailView` | **满意读感，可重构** | 保留主读感；若规范化、简化流程或减冗余收益明显，允许直接精简 / 重做 | `P1（按需）` |
| 常驻 | `EngineParticles` | `ShipEngineVFX` | 待重构 | 重做 | `P2` |
| 常驻 | `Tilt` | `ShipVisualJuice` | 待重构 | 重做 | `P3` |
| 常驻 | `Squash / Stretch` | `ShipVisualJuice` | 待重构 | 重做 | `P3` |
| Dash | `IFrame Flicker` | `ShipView` | 待重构 | 重做 | `P2` |
| Dash | `Static Ghost` | `ShipView` | 待重构 | 重做 | `P2` |
| Dash | `After-Images` | `DashAfterImageSpawner` | 待重构 | 重做 | `P2` |
| Dash | `Dash Engine Burst` | `ShipEngineVFX` | 待重构 | 重做 | `P2` |
| Boost 起手 | `Activation Halo` | ~~`BoostTrailView`~~ | **已取消** ❌ | 取消（合并入 Bloom/FlameCore 强化） | — |
| Boost 起手 | `Bloom Burst` | `BoostTrailView` + Scene Volume | **已验收** ✅ | 重做 | `P1` |
| Boost 起手 | `FlameCore Burst` | `BoostTrailView` | **已验收** ✅ | 重做 | `P1` |
| Boost 起手 | `EmberSparks Burst` | `BoostTrailView` | **已验收** ✅ | 重做 | `P1` |
| Boost 起手 | `Liquid Boost State`（亮度 + Sprite 切换 + SortOrder） | `ShipBoostVisuals` | **已验收** ✅ | 重做 | `P1` |
| Boost 起手 | `Thruster Entry Pulse` | `ShipBoostVisuals` | 待重构 | 重做 | `P2` |
| Boost 持续 | `FlameTrail_R / FlameTrail_B` | `BoostTrailView` | **已验收** ✅ | 重做 | `P1` |
| Boost 持续 | `EmberTrail` | `BoostTrailView` | **已验收** ✅ | 重做 | `P1` |
| Boost 持续 | `BoostEnergyLayer2 / Layer3` | `BoostTrailView` | 实现中 | 重做 | `P1` |
| Boost 持续 | `Boost Engine Hold / Loop Pulse` | `ShipEngineVFX` + `ShipView` | 待重构 | 重做 | `P2` |
| Boost 收尾 | `Boost Outro Restore` | `BoostTrailView` + `ShipView` | 待重构 | 重做 | `P2` |

### 6.4 当前 list 的使用方式

- 每次只从表里挑 **一个条目** 或 **一个强耦合小簇** 来讨论
- 若某条目最终不值得保留，就把"建议处理"从 `重做` 改成 `删除` 或 `合并`
- 若某条目需要依附别的层存在，就在讨论时把它降级为辅层，而不要抢主读感
- 每完成一项，就在本表里把状态更新成：`已定方向` / `实现中` / `已验收`

### 6.5 已取消推进项：`Activation Halo` ❌

#### 最终结论

- **状态**：`已取消`（2026-03-22）
- **决策**：`取消（合并入 Bloom Burst + FlameCore Burst 强化）`
- **原始定位**：Boost 起手的局部主确认层

#### 取消原因

实机测试后发现 `Activation Halo` 不适合作为独立层保留：

1. **肉眼几乎不可见**：duration 仅 0.12s 的 ring sprite scale + alpha 动画，在实际 Boost 起手时完全被 `Bloom Burst` 和 `FlameCore Burst` 盖住
2. **维护成本过高**：实现跨 7 个文件（`BoostTrailView` / `BoostTrailPrefabCreator` / `ShipJuiceSettingsSO` / `BoostTrailDebugManager` / `BoostTrailDebugManagerEditor` / `ShipVfxValidator` / `MaterialTextureLinker`），投入产出比极低
3. **职责已被现有层覆盖**：强化后的 `Bloom Burst`（全局能量放大）+ `FlameCore Burst`（喷口根部点火核心）已经充分提供了 Boost 起手确认读感，不需要额外的 ring sprite 层

#### 替代方案

Boost 起手确认职责由以下已验收层共同承担：

- `Bloom Burst` ✅：全局能量抬升，短促放大器
- `FlameCore Burst` ✅：喷口根部点火核心，近身实体感
- `EmberSparks Burst` ✅：边缘火星强调
- `Liquid Boost State` ✅：船体高能态切换

#### 清理范围

以下代码和配置已在取消时一并删除：

- `BoostTrailView`：移除 `_activationHalo` 字段、`TriggerActivationHalo()` / `ApplyActivationHalo()` 方法
- `BoostTrailPrefabCreator`：移除 `ConfigureActivationHalo()` 相关生成逻辑
- `ShipJuiceSettingsSO`：移除 Halo 相关配置参数
- `BoostTrailDebugManager` / `BoostTrailDebugManagerEditor`：移除 Halo 预览入口
- `BoostTrailRoot.prefab`：移除 `BoostActivationHalo` 子节点

> **注意**：Halo 相关的纹理资产（`vfx_ring_glow_uneven.png` / `vfx_magnetic_rings.png`）暂保留在仓库中，状态从 `Live` 降级为 `Dormant`，待后续 Batch 4 dormant 清理时统一处理。

### 6.6 当前推进项：`Bloom Burst`

#### 当前结论

- **状态**：`已验收` ✅
- **决策**：`重做（降级为辅层）`
- **定位**：Boost 起手的**全局辅层放大器**
- **关系**：它只能放大 `FlameCore Burst` 已经建立的点火读感，不能单独承担"Boost 成功确认"

#### 目标（玩家此刻应该感受到什么）

当玩家按下 Boost 成功进入高能态时，`Bloom Burst` 应该提供的是一记**整体能量被瞬间顶起来**的放大感，而不是单独成为主角：

- **画面整体有一次短促抬升**，让人感觉能量瞬间冲上来
- **玩家第一眼仍然先读到飞船本体和推进器被点燃**，而不是只感觉屏幕发白
- **它和 `MainTrail` 属于同一高能科技语系**，但职责是"放大器"，不是"主读感载体"
- **它必须给本地层让位**：船体局部读感优先，后处理只负责托举

#### 当前问题（基于现实现链路）

当前实现已经明确是一条 **scene-only 后处理链**：`BoostTrailView.TriggerBloomBurst()` 在 Boost 起手时直接把 `_boostBloomVolume.weight` 拉到 `1`，并对 `Bloom.intensity` 做 `baseline → peak → baseline` 的对称 tween，再回落到 `weight = 0`。

基于这条现役链，当前主要风险是：

- **后处理感太强**：容易把读感做成"屏幕亮了一下"，而不是"飞船被点燃了"
- **职责容易篡位**：如果峰值过高或持续时间过长，`Bloom` 会盖过 `FlameCore Burst`
- **节奏偏平均**：当前对称 `up/down` 结构更像一个泛用亮度脉冲，未必足够像"点火瞬间先顶一下、立刻回让"
- **场景依赖明显**：这层依赖 `ShipBoostTrailSceneBinder` 绑定的 `BoostTrailBloomVolume` 与 `BoostBloomVolumeProfile.asset`，天然更适合做辅层，而不是主确认层

#### 技术参考（是否沿用 `MainTrail` 方案）

`Bloom Burst` **不适合完整沿用 `MainTrail` 方案作为主载体**，因为它本质上是 **scene-only 的后处理放大器**，不是 ship-local 的主读感层。

但它仍应尽量沿用 `MainTrail` 的治理思路：

1. **单一现役主链**：只保留 `BoostTrailView` → `_boostBloomVolume` → `BoostBloomVolumeProfile.asset` 这一条 live chain。
2. **少量关键参数驱动**：优先用少数几个参数控制峰值、attack、release，不把 Bloom 拆成多套并行补丁。
3. **Editor 装配与 Runtime 播放分离**：scene volume 继续由 `ShipBoostTrailSceneBinder` 负责接线，运行时只负责时序驱动。
4. **让位于本地主读感**：像 `MainTrail` 一样先明确"谁是主角"，Bloom 在这里不能争主载体位置。

#### 范围

本项默认允许改动：

- `BoostTrailView.TriggerBloomBurst()` 的时序、峰值、attack / release 曲线与 weight 使用方式
- `BoostBloomVolumeProfile.asset` 的 Bloom 默认参数
- `ShipBoostTrailSceneBinder` 对 scene-only Bloom Volume 的约束说明
- 必要时调整它与 `FlameCore Burst` 的相对先后关系

本项默认**不**改动：

- `MainTrail` 本体
- `FlameCore Burst` / `EmberSparks Burst` 的本体点火表现
- 持续层的 `FlameTrail_R / FlameTrail_B` 与 `EmberTrail`

#### MVP 方向

先做一个**前置、短促、迅速回让**的版本，不追求"存在感最大化"：

- `Bloom` 只承担 **全局能量放大**，不承担"Boost 是否成功"的第一确认职责
- attack 应明显快于 release，避免均速脉冲感
- 峰值以"托举本体点火"为目标，而不是追求强烈泛白
- 如果必须在冲击和可读性之间取舍，优先保住船体本地层的可读性
- 关闭 `Bloom` 后，`Halo` 与本地点火层必须仍然能独立成立

#### 验收标准

满足以下条件视为本项完成：

- 玩家在 `SampleScene` 中进入 Boost 时，会感觉到**整体能量被抬起来一下**，但第一眼仍然先读到船体和推进器被点燃
- `Bloom Burst` 不会把飞船轮廓、喷口根部和本地起手层洗白到看不清
- 关闭 `Bloom Burst` 后，玩家仍能明确判断"Boost 点火成功了"
- 它的主观读感是**短促放大器**，而不是第二个持续特效
- 它不会抢走 `FlameCore Burst` 作为主确认层的职责

#### 未来增强（先不做）

- 依据速度、热量或状态阶段做峰值自适应
- 让 Bloom 与镜头曝光 / 色偏做更细的高能协同
- 仅在本体局部层已经稳定后，再考虑更复杂的后处理编排

### 6.7 当前推进项：`FlameCore Burst`

#### 当前结论

- **状态**：`已验收` ✅
- **决策**：`重做`
- **定位**：Boost 起手的**推进器根部点火核心层**
- **关系**：`FlameCore Burst` 负责喷口根部"核心被点燃"的实体读感，是 Boost 起手的**主确认层**（原 `Activation Halo` 已取消并入此层）；`EmberSparks Burst` 只能做边缘强调，`FlameTrail_R / FlameTrail_B` 则负责后续持续输出

#### 目标（玩家此刻应该感受到什么）

当玩家进入 Boost 时，推进器根部应该先给出一记**密、热、短、压缩感强**的核心点火，而不是直接跳进持续喷火：

- **喷口根部先被点燃**，像推进系统瞬间压满、点着核心
- **它必须是近身的、实体的**，让玩家感觉能量从推进器里被压出来
- **节奏要短狠**：更像一次 ignition punch，而不是持续火焰的提前版
- **它和 `MainTrail` 属于同一受控高能语系**：集中、清晰、有方向性，不是松散烟花

#### 当前问题（基于现实现链路）

当前 `FlameCore` 虽然命名上叫 `Burst`，但现役实现更接近一条**短寿命循环粒子层**：

- `BoostTrailPrefabCreator.ConfigureFlameCore()` 中 `main.loop = true`，`emission.rateOverTime = 42`，这意味着它当前不是一次性 burst，而是持续喷发
- `BoostTrailView.OnBoostStart()` 与 `OnBoostEnd()` 直接对 `_flameCore` 执行 `PlayParticle()` / `StopParticle()`，生命周期几乎与持续层一起走
- 它当前和 `FlameTrail_R / FlameTrail_B` 一样都使用火焰粒子语汇，容易职责重叠，导致"点火核心层"和"持续喷火层"边界不清
- 由于它不是显式的 start-only 节奏层，玩家未必能明确读到"推进器核心先被点燃，再进入持续输出"的过程

#### 技术参考（是否沿用 `MainTrail` 方案）

`FlameCore Burst` **应尽量沿用 `MainTrail` 的实现思路，但主载体改为 `ParticleSystem`**，因为它需要的是喷口根部的局部体积感，而不是带状拖尾几何。

这里应重点复用 `MainTrail` 的 5 个原则：

1. **清晰的单一主载体**：`FlameCore Burst` 自己承担"核心点火"主读感，不再和持续火焰混成一层。
2. **单一现役主链**：优先保持一个明确的 `FlameCore` live material / prefab / runtime chain，避免为同一职责叠多套兼容实现。
3. **少量关键参数驱动**：把 burst 强度、持续时间、尺寸节奏收束到少数关键参数，而不是多层粒子同时乱堆。
4. **Editor 装配与 Runtime 播放分离**：默认形态与材质选择归 `BoostTrailPrefabCreator`，运行时只负责何时触发、何时结束。
5. **先有可读 MVP，再补辅层**：先让根部点火本身成立，再考虑是否需要由 `EmberSparks` 或别的细节层做陪衬。

#### 范围

本项默认允许改动：

- `BoostTrailRoot.prefab` 中 `FlameCore` 的表现方式
- `BoostTrailPrefabCreator.ConfigureFlameCore()` 的 `loop` / `emission` / `lifetime` / `shape` / `renderer` / 默认材质选择
- `BoostTrailView` 对 `_flameCore` 的触发方式与存在时长控制
- 必要时重排它与 `EmberSparks Burst`、`FlameTrail_R / FlameTrail_B` 的先后关系

本项默认**不**改动：

- `MainTrail` 本体
- `Bloom Burst` 的全局放大职责
- 持续层 `EmberTrail` 的空间余烬表现

#### MVP 方向

先做一个**真正 start-only、密度高、存在时间短**的版本：

- `FlameCore Burst` 只负责 **喷口根部点火那一下**，不继续占用整个 Boost 持续期
- 主观上更像**核心被压亮并喷出**，而不是普通持续火焰提前一帧开始
- 形态优先强调**近源、高亮、短促、有方向性**，不要做成四散烟火
- 若与 `EmberSparks` 同时存在，玩家应先读到核心火焰，再读到边缘火星
- 它结束后，应自然把视觉主导权交给 `FlameTrail_R / FlameTrail_B`

#### 验收标准

满足以下条件视为本项完成：

- 玩家在 `SampleScene` 中进入 Boost 时，能明确读到**喷口根部先点火**，而不是只有持续火焰直接出现
- `FlameCore Burst` 的主观持续时间足够短，不会和 `FlameTrail_R / FlameTrail_B` 混成同一条持续层
- 即使关闭 `Bloom Burst`，玩家仍能清楚感受到推进器核心被点亮的瞬间
- 它不会干扰 `MainTrail` 的持续主读感，也不会和 `EmberSparks Burst` 抢主次
- 它和 `EmberSparks Burst` 形成主次分明的关系：核心火焰是主体，火星只是陪衬

#### 未来增强（先不做）

- 依据飞船朝向或瞬时速度做更强的定向拉伸
- 为核心点火引入专属材质 / shader，而不是沿用持续火焰材质
- 与尾喷 Entry Pulse 做更细的节奏锁定

### 6.8 当前推进项：`EmberSparks Burst`

#### 当前结论

- **状态**：`已验收` ✅
- **决策**：`重做`
- **定位**：Boost 起手的**边缘火星强调层**
- **关系**：它只能跟在 `FlameCore Burst` 之后补一记边缘甩火，不能抢先成为第一点火读感，更不能和 `FlameTrail_R / FlameTrail_B` 混成持续喷发

#### 目标（玩家此刻应该感受到什么）

当玩家按下 Boost 成功点火时，`EmberSparks Burst` 应该给人的感觉是：

- **核心火焰已经点着后，边缘有一圈短促火星被甩出去**
- **它是尖、快、轻的陪衬层**，负责补充能量炸开的边缘细节，而不是主体
- **方向感应与推进器一致**，更像喷口边缘被带出的灼热碎屑，而不是四散烟花
- **它必须主动让位给 `FlameCore Burst`**：玩家先读到根部点火，再注意到火星飞散

#### 当前问题（基于现实现链路）

当前 `EmberSparks` 虽然已经是 one-shot burst，但现役默认形态仍更接近一记**径向散开的白色爆点**：

- `BoostTrailView.OnBoostStart()` 之前与 `FlameCore` 同帧触发，读感上容易和核心点火抢第一眼
- `BoostTrailPrefabCreator.ConfigureEmberSparks()` 现有 `Circle + radial velocity` 更像**全向炸开**，不够贴喷口、也不够像边缘甩火
- 起始速度和亮度偏高时，`EmberSparks` 会从"边缘强调"漂移成"另一个主 burst"
- 若它持续太长或太散，玩家会更像看到一次烟花，而不是受控推进系统点火

#### 技术参考（是否沿用 `MainTrail` 方案）

`EmberSparks Burst` **不应照搬 `MainTrail` 的主载体形式**，因为它不是持续拖尾，而是极短命的粒子陪衬层。

但它仍应沿用 `MainTrail` 的治理原则：

1. **主次先定**：明确主体是 `FlameCore Burst`，`EmberSparks` 只做陪衬。
2. **单一现役主链**：只维护一套 `BoostTrailView` → `_emberSparks` → `BoostTrailRoot.prefab` 的 live chain。
3. **少量关键参数**：把时序、burst 数量、定向速度收口到少数可调参数，而不是再叠第二套火星方案。
4. **Editor 装配 / Runtime 触发分离**：形态归 `BoostTrailPrefabCreator`，时序归 `BoostTrailView`。

#### 范围

本项默认允许改动：

- `BoostTrailView` 中 `EmberSparks` 相对 `FlameCore` 的触发时序
- `BoostTrailPrefabCreator.ConfigureEmberSparks()` 的 burst 数量、速度、shape、renderer 与默认位置
- `BoostTrailRoot.prefab` 中 `EmberSparks` 的现役序列化参数

本项默认**不**改动：

- `FlameCore Burst` 的喷口点火主确认职责
- `Bloom Burst` 的全局放大职责
- `FlameTrail_R / FlameTrail_B` 的持续层主输出
- `MainTrail` 本体

#### MVP 方向

先做一个**稍晚于 FlameCore、沿喷口方向甩出的短促火星版本**：

- `EmberSparks` 相对 `FlameCore` 略延后一拍，保证核心点火先被读到
- 形态从**径向炸开**收口成**定向甩火**，减少烟花感
- 粒子数量、持续时间和亮度都以"补边"为上限，不追求存在感最大化
- 它结束后应快速消失，把视觉主导权自然交给持续层

#### 验收标准

满足以下条件视为本项完成：

- 玩家在 `SampleScene` 中进入 Boost 时，会先读到 `FlameCore Burst`，然后才感知到 `EmberSparks` 的边缘强调
- `EmberSparks` 的主观读感是**短促、定向、边缘化**，而不是一次全向爆炸
- 即使临时关闭 `EmberSparks`，Boost 起手主确认仍成立；说明它确实是辅层而非主体
- 它不会把 `FlameCore` 或 `MainTrail` 的主读感洗掉

#### 未来增强（先不做）

- 依据船体朝向做更非对称的双侧甩火分布
- 与 `Thruster Entry Pulse` 做更强的节奏联动
- 为火星引入专属细线拉伸材质，而不是继续复用通用 spark 材质

### 6.9 当前推进项：`Liquid Boost State`（亮度 + Sprite 切换）

#### 当前结论

- **状态**：`已验收` ✅
- **决策**：`重做`
- **定位**：Boost 起手到持续阶段的**船体高能态切换层**
- **关系**：它负责让玩家感到"船体液态能量层进入高能态"，但不能替代 `FlameCore Burst` 的瞬时点火职责，也不能拖成另一个独立特效

#### 实现总结

由 `ShipBoostVisuals`（Worker）驱动，三管齐下解决了 Liquid 层在 Boost 时完全不可见的问题：

1. **Sprite Swap**：Boost 时从 `Movement_3.png` 切换到 `Boost_16.png`，退出时恢复
2. **SortOrder 提升**：Boost 时 `sortingOrder` 从 `-2` 提升到 `1`（Solid 层之上），退出时恢复
3. **HDR 高强度颜色 tween**：颜色从基线 `(0.15, 0.25, 0.3)` tween 到 `(3, 4, 5, 1)` HDR 值

所有参数数据驱动，配置在 `ShipJuiceSettingsSO`：
- `_boostLiquidSprite`：Boost 专用 Sprite
- `_boostLiquidSortOverride` / `_boostLiquidSortOrder`：SortOrder 覆盖开关与值
- `_boostLiquidColor`：HDR 目标颜色
- `_boostLiquidRampUpDuration` / `_boostLiquidRampDownDuration`：过渡时长

基线状态在 `Initialize()` 中捕获，`ResetState()` 完整恢复（防对象池状态泄漏）。

#### 验收结果

- ✅ 进入 Boost 时，Liquid 层从 Solid 下方提升到上方，以 HDR 高亮色覆盖船体，明确感知"高能态"
- ✅ 与 `FlameCore Burst` / `Bloom Burst` 叠加时层次分明，不互相打架
- ✅ 退出 Boost 后，sprite / sortOrder / 颜色完整恢复到基线状态，无残留泄漏
- ✅ 所有参数数据驱动，可在 SO 中无代码调参

#### 未来增强（先不做）

- 让液态层亮度与热量或速度挂钩，形成更连续的高能反馈
- 为 Boost 贴图切换增加更细的过渡噪声或 shader 扰动
- 把退场恢复也升级为明确的"收束"节奏，而不是简单回色
- 加入 ignition peak → sustain 的两段式节奏（当前单段 ramp 已足够成立）

### 6.10 当前推进项：`FlameTrail_R / FlameTrail_B` + `EmberTrail`

#### 当前结论

- **状态**：`已验收` ✅
- **决策**：`重做`
- **定位**：Boost 持续阶段的**推进器实体层**（`FlameTrail`，Local-space 喷口持续火焰）与**残余余烬层**（`EmberTrail`，World-space rateOverDistance 移动散落碎屑）
- **关系**：`MainTrail` 必须继续作为唯一主尾迹；`FlameTrail` 负责让玩家读到喷口持续全开，`EmberTrail` 只负责在主尾迹后面补一层更轻的热余烬

#### 目标（玩家此刻应该感受到什么）

当玩家已经从 Boost 起手进入稳定高速态后，推进系统的持续读感应该是：

- **后方主速度尾迹已经成立**，这件事继续由 `MainTrail` 负责
- **喷口本体持续在高功率工作**，玩家能从船尾附近读到贴喷口的持续火焰
- **主尾迹之外还有轻微余烬残留**，帮助拉出热量层次，但不形成第二条主尾迹
- **整条持续链是受控的**，像高能推进系统稳定输出，而不是一堆粒子一起乱喷

#### 当前问题（已解决）

以下问题在本轮重做中已全部修复：

- ~~`FlameTrail_R / FlameTrail_B` 之前更接近**世界空间拖尾粒子**，容易和 `MainTrail` 抢同一类"后方拉尾"读感~~ → 已改为 Local-space `rateOverTime` 喷口持续火焰，紧贴船体
- ~~`EmberTrail` 之前偏**大、散、团状**，更像脏烟或糊开的彩色气团~~ → 已改为 World-space `rateOverDistance` 微小余烬碎屑，稀疏、极短命、半透明
- ~~持续层在运行时几乎是**一开就满**，缺少"主尾迹先成立，其他层再接管"的读感秩序~~ → 已通过 blend-in threshold 分层接管（FlameTrail 0.18 → EmberTrail 0.42）
- ~~Boost 结束时如果直接停粒子，会让持续态从"稳定巡航"突然掉成"硬切断电"~~ → 已由 master intensity ramp-down 自然淡出

#### 技术参考（是否沿用 `MainTrail` 方案）

这一簇**应明确沿用 `MainTrail` 的治理原则**，但主载体拆成两个角色：

1. **主读感唯一**：`MainTrail` 继续承担唯一世界空间主尾迹。
2. **单一共享强度链**：`BoostTrailView` 用一条 master intensity 统一驱动持续层，不给每层单独立王国。
3. **载体职责分离**：`FlameTrail` 改成喷口本地持续火焰；`EmberTrail` 保持轻量 world residual。
4. **Editor 装配 / Runtime 驱动分离**：形态在 `BoostTrailPrefabCreator`，接管时序和淡出在 `BoostTrailView`。

#### 范围

本项默认允许改动：

- `BoostTrailView` 中持续层的统一强度链、接管阈值和退场逻辑
- `BoostTrailPrefabCreator` 对 `FlameTrail_R / FlameTrail_B` 与 `EmberTrail` 的默认粒子形态配置
- `BoostTrailRoot.prefab` 中对应现役序列化参数

本项默认**不**改动：

- `MainTrail` 本体
- 起手层 `Bloom Burst` / `FlameCore Burst` / `EmberSparks Burst`
- `ShipEngineVFX` 的引擎 Hold / Loop Pulse

#### MVP 方向

先做一个**主尾迹先立、喷口火焰贴身、余烬轻量让位**的版本：

- `FlameTrail_R / FlameTrail_B` 改成**本地空间喷口持续火焰**，不再自己生成第二条世界尾迹
- `EmberTrail` 收口成**更稀、更轻、更热**的残余层，避免烟团感
- 两者都通过 `BoostTrailView` 的**共享 master intensity** 接入，按阈值顺序接管持续态
- Boost 退出时由同一条强度链带下持续层，而不是立即 `Stop()` 硬切

#### 验收标准

满足以下条件视为本项完成：

- 玩家在 `SampleScene` 中进入稳定 Boost 后，第一持续读感仍然是 `MainTrail`，而不是被双侧粒子抢走
- `FlameTrail` 的读感明显贴近喷口本体，像推进器在持续高功率喷发，而不是第二条尾迹
- `EmberTrail` 只提供轻量热余烬层次，不会把后方空间刷成一团雾
- Boost 结束时，持续层会顺着强度链自然退下，而不是突然硬停

#### 未来增强（先不做）

- 让 `FlameTrail` 随船体朝向 / 加速度做更细的偏转
- 给 `EmberTrail` 增加更明确的热流方向场，而不是只依赖当前基础噪声
- 把 `Boost Engine Hold / Loop Pulse` 与这一簇做更紧的节奏绑定

### 6.11 当前推进项：`BoostEnergyLayer2 / Layer3`

#### 当前结论

- **状态**：`实现中，待实机验收`
- **决策**：`重做`
- **定位**：Boost 持续阶段的**机体充能维持层**
- **关系**：它们必须服务于"飞船仍处于高能巡航态"这个读感，但不能在持续段里抢走 `MainTrail` 或喷口火焰的主体位置

#### 目标（玩家此刻应该感受到什么）

当玩家进入稳定 Boost 后，船体本身应该保持一种**仍在被高能驱动**的状态：

- **机体是持续充能的**，不是起手闪一下就完全归零
- **船身亮度被维持在高能档位**，但不会洗白整艘船
- **层次上有前后关系**：更外层先成立，内层更晚、更弱、更克制
- **它们像状态维持层**，而不是持续爆闪特效

#### 当前问题（基于现实现链路）

当前 `BoostEnergyLayer2 / Layer3` 的问题主要在于：

- 它们之前和 `MainTrail` 共享同一个 1:1 `_BoostIntensity`，很容易一起**同步冲满**
- 若两层同时进入、同时满强度，就会把船体读感洗得过亮，削弱推进器与尾迹的层次关系
- 它们缺少"哪一层先建立、哪一层只做弱补充"的顺序治理，容易变成纯参数叠加

#### 技术参考（是否沿用 `MainTrail` 方案）

这一项**应沿用 `MainTrail` 的关键参数驱动原则**，但主载体是 `SpriteRenderer + Shader _BoostIntensity`：

1. **单一主链**：仍由 `BoostTrailView` 统一下发强度。
2. **少量参数控制层次**：用 blend-in threshold 和 max intensity 管理进入时序与上限。
3. **避免并列主角**：`Layer2` 可更早出现但必须受上限约束，`Layer3` 更晚、更弱。
4. **运行时只改参数，不堆新节点**：不额外增加新的 overlay 链。

#### 范围

本项默认允许改动：

- `BoostTrailView.SetBoostIntensity()` 对 `Layer2 / Layer3` 的运行时权重分配
- `BoostTrailRoot.prefab` 中 `BoostTrailView` 的持续层调参默认值
- 必要时微调现役 shader 强度分配的使用方式（保持 `_BoostIntensity` 为单一入口）

本项默认**不**改动：

- `ShipView` 的 `Liquid Boost State`
- `MainTrail` 本体
- 材质 / 贴图的大规模迁移

#### MVP 方向

先做一个**外层先立、内层后补、整体受限**的版本：

- `Layer2` 作为较早建立的外层充能感，但强度不追满
- `Layer3` 更晚、更弱，只做最轻的内层脉动补充
- 二者都从同一 master intensity 派生，不再与 `MainTrail` 1:1 直连
- 退场时跟随同一条强度链回落，保持整船高能态自然卸载

#### 验收标准

满足以下条件视为本项完成：

- 玩家在稳定 Boost 中仍能感到船体维持在高能档位，而不是只靠尾迹判断
- `BoostEnergyLayer2 / Layer3` 不会把机体洗成持续爆闪白板
- `Layer2` 与 `Layer3` 的层次关系清楚：前者更早、更明显；后者更晚、更弱
- 它们在视觉上是"维持高能态"，而不是"持续抢戏"

#### 未来增强（先不做）

- 给层间引入更细的相位差或低频脉动
- 把能量层权重与热量 / 速度进一步耦合
- 针对不同飞船皮肤做分层权重 profile

## 4. MVP 冻结清单

本轮不做：

- 全仓批量 rename
- 大规模移动 `Assets/_Art/...` 底层资源路径
- **未完成引用审计前**直接删除 dormant / legacy 文件
- 直接物理重命名 `ShipShipState.cs`
- 直接物理重命名 `FlameTrail_B` / `Ship_Sprite_HL` 等序列化节点

## 5. 风险与应对

| 风险 | 说明 | 当前策略 |
| --- | --- | --- |
| Prefab 断链 | `Ship.prefab` 与 nested `BoostTrailRoot` 之间的引用断裂 | 先由 `ShipPrefabRebuilder` 收口集成与接线 |
| Scene-only 引用丢失 | `_boostBloomVolume` prefab 不可序列化保存 | 由 `ShipBoostTrailSceneBinder` 单独管理 |
| 模糊搜索误绑 | `FindAssets` 可能命中错误同名资源 | 对现役材质与关键 sprite 优先精确路径 |
| 文档口径漂移 | 参考文档继续被当成现役规范 | 建立 canonical spec + registry 双文档 |
| 历史物理名扩散 | `Boost_16` / `Movement_3` 等继续被当语义名使用 | 先统一 canonical alias |
| Editor 重建回写 | 手工改 `FlameTrail_B` / `Ship_Sprite_HL` 后，又被 `BoostTrailPrefabCreator` / `ShipPrefabRebuilder` 用旧名重建覆盖 | 先改 Editor 工具常量，再在 Unity Editor 中执行节点改名与引用复验 |
| 共享材质误判 | 把 `Ship_Sprite_HL` 误认为专属 glow 材质链，导致一次性联动过多 | 节点改名与材质 / 贴图迁移拆开处理，先单独验证当前 shared default material 现状 |

## 6. 验证方式

每一批结束后至少做：

1. 脚本静态检查 / 编译检查
2. `Ship.prefab` 与 `BoostTrailRoot.prefab` 引用核对
3. `SampleScene.unity` 中 scene-only 绑定核对
4. 现役文档与注册表一致性复查
5. `Docs/ImplementationLog/ImplementationLog.md` 追加记录

## 7. 当前批完成后应达到的状态

- 项目里第一次出现正式的 **Ship VFX 规范层**
- 现役资产、dormant 资产、参考资料不再混为一谈
- 已验证 Play Mode 主链稳定，并完成首批 dormant 资源清退
- `FlameTrail_B` / `Ship_Sprite_HL` / `ShipShipState` 已完成改名前风险分类，可直接进入"最小安全动作"阶段
- 后续即使不马上 rename 资源，团队也能用统一语言讨论和维护这条链路
