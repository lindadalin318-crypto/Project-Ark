# Project Ark × Minishoot 墙元素可做项总表

> **文档目的**：把 `Minishoot` 中与“墙”相关、且当前 `Project Ark` **可以做 / 值得做 / 适合如何落地** 的要素统一收口成一份参考总表，避免后续再次分散考古。  
> **文档定位**：这是 **Reference**，不是现役规范；`Level` 模块现役 authority 仍以 `Docs/2_TechnicalDesign/Level/Level_CanonicalSpec.md`、运行时代码与实际 authoring 规则为准。  
> **来源依据**：`Minishoot` 解包项目中的 `Location`、`WallDestroyable`、`BulletBlocker`、`HiddenArea`、`OneWay` 等脚本与场景结构；以及 Ark 当前 `Level` Canonical Spec、`DestroyableObject`、`HiddenAreaMask`、`BiomeTrigger`、`WorldEventTrigger`、`SaveBridge` 等现役能力。  
> **创建时间**：2026-04-15

---

## 1. 一句话结论

`Minishoot` 真正值得 Ark 借的，不是一个统一的 `Wall.cs`，而是一套**按玩法语义拆开的墙家族**。

对 `Project Ark` 来说，当前最合理的方向是：

- 先把**静态几何墙**继续留在 `Tilemap + Collider` 的场景事实层。
- 再把会变化、会导演、会给探索回报的“墙”收口为一组标准件。
- 不把所有“看起来像墙”的东西都塞进同一个脚本或同一条 authoring 链。

---

## 2. 先给总表：目前所有值得考虑的墙家族

| 优先级 | 墙家族 | 建议 Ark 标准件名 | 当前是否有底座 | 主要价值 | 推荐状态 |
| --- | --- | --- | --- | --- | --- |
| **P0** | 静态几何墙 | `StructuralWall`（概念，不一定单独做脚本） | **有** | 定义不可通行空间边界 | **立即可用** |
| **P1** | 可破坏墙 | `BreakableWall` | **有一半**：`DestroyableObject` + `RoomFlagRegistry` + `SaveBridge` | 探索回报、永久开路、空间记忆 | **优先做** |
| **P1** | 隐藏遮挡墙 | `HiddenAreaMask` / `OccluderRevealVolume` | **有** | 秘密发现感、视觉 reveal、低成本高收益 | **优先做** |
| **P1** | 进度封堵墙 | `ProgressBarrier` / `CollapseBarrier` | **有一半**：`WorldEventTrigger` + `ActivationGroup` + `TilemapVariantSwitcher` | 剧情推进、区域回路、世界变化证据 | **优先做** |
| **P1** | 阶段相位墙 | `PhaseBarrier` / `TemporalWall` | **有一半**：`ScheduledBehaviour` + `WorldPhaseManager` | 世界时钟存在感、阶段性路线变化 | **优先做** |
| **P2** | 投射物屏障墙 | `ProjectileBarrier` | **基础有，标准件未收口** | 战斗语法、弹道空间规则、局部战术 | **第二批** |
| **P2** | 单向通行墙 | `OneWayPassage` / `DirectionalGate` | **需要新增标准件** | 回返路线、下落井、方向性规则 | **第二批** |
| **P3** | 仪式封印墙 / Boss 墙 | `CeremonySeal` / `BossBarrier` | **可借 `Door` 语义** | 类魂式仪式感、章节收束 | **按内容需要做** |

---

## 3. 选择原则

本总表采用以下筛选逻辑：

1. **优先借“墙语法”，不是借某个具体脚本名字**。
2. **先区分空间事实与玩法事实**：哪里不能过，是场景事实；什么时候能开、能碎、能显、能穿，是玩法事实。
3. **优先接 Ark 现有 authority**，不另起第二套关卡 manager。
4. **优先服务当前 `Level` 五层模型**，尤其是：
   - Layer 2：`DestroyableObject`、`RoomFlagRegistry`
   - Layer 1：`HiddenAreaMask`、`WorldEventTrigger`、`ScheduledBehaviour`
5. **先做能在单房间里验证手感的标准件**，再考虑全区域泛化。

---

## 4. 详细拆分

## 4.1 静态几何墙：`StructuralWall`

### 定义

这是最基础的一类墙：

- 挡玩家
- 挡实体移动
- 定义空间边界
- 本身不承担复杂状态逻辑

### Minishoot 给 Ark 的启发

`Minishoot` 在 `Location` 里把 `Wall / Fence / Hole / Water` 分成不同几何层，这说明它首先把“墙”当作**场景几何语法**，而不是互动件。

### 对 Ark 的建议

Ark 也应保持这个原则：

- **普通舱壁、岩壁、残骸边界**：继续作为 `Tilemap + Collider` 的空间事实
- 不要把所有普通墙都转成 MonoBehaviour
- 不要为纯静态墙额外发明“墙状态系统”

### 当前落地方式

- 继续走场景 authoring
- 由 Tilemap / Collider / 房间边界承担空间真相
- 必要时仅补 validator / root 约束，不补重逻辑

### 建议状态

**立即可用。** 这不是一个必须新建脚本的 feature，更像一条 authoring 纪律。

---

## 4.2 可破坏墙：`BreakableWall`

### 定义

玩家通过攻击、解谜或特定能力，破坏一段封堵墙体，让路线永久改变。

### 为什么值得优先做

这是最符合银河城体验的一类墙：

- 打开捷径
- 揭示秘密通道
- 形成“之前过不去，现在能过”的空间记忆
- 给战斗 / 探索一个清晰回报

### 当前 Ark 底座

Ark 已有：

- `DestroyableObject`
- `RoomFlagRegistry`
- `SaveBridge`

这意味着你们已经具备：

- 破坏行为
- 房间级持久化
- 存档恢复链

真正缺的是**把它收口成“墙语义预设”**，而不是继续把每堵墙当泛化 destroyable。

### 建议翻译形态

- 晶体裂墙
- 坍塌 debris 墙
- 共鸣脆壁
- 腐化外壳封堵

### 建议挂点

- `Elements/`
- 归类为 `Stateful` 元素
- 状态统一进 `RoomFlagRegistry`

### MVP

先只做：

- 可受伤
- 破坏后关闭碰撞 / 显示碎裂结果
- 写入房间持久状态
- 重新进房或读档后维持已破坏状态

### Phase 2

- 仅特定武器 / 星图配置可破坏
- 破坏前后小地图变化
- 镜头 / 音效 / VFX 仪式强化
- 与隐藏区、奖励链联动

---

## 4.3 隐藏遮挡墙：`HiddenAreaMask` / `OccluderRevealVolume`

### 定义

这类墙不是实体阻挡，而是**视觉遮挡层**：

- 先把秘密区域遮住
- 玩家靠近或进入后逐步 reveal
- 离开时恢复或保持揭示

### 为什么值得优先做

这类墙的性价比很高：

- 不一定要改导航
- 但能显著增强发现感
- 非常贴 `Project Ark` 的异星探索氛围

### 当前 Ark 底座

Ark 已有：

- `HiddenAreaMask`
- `LevelValidator` 已覆盖其 trigger / preferred-root 校验

说明这一类并不是从零开始，而是已经进入现役 authoring 语法。

### 建议翻译形态

- 孢子幕墙
- 晶簇遮蔽层
- 机械残骸遮挡
- 岩壁假墙
- 黑暗遮罩后的隐秘舱室

### 建议挂点

- `Triggers/`
- 归类为 `Directing`
- 不承担奖励逻辑本身，只负责 reveal

### MVP

先只做：

- 遮挡层显隐
- 进入淡出 / 离开恢复
- 支持纯视觉 reveal

### Phase 2

- 永久揭示
- 与 minimap / 收藏 / Lore 点联动
- 与局部 ambience / camera 叠层

---

## 4.4 进度封堵墙：`ProgressBarrier` / `CollapseBarrier`

### 定义

这类墙会在**世界进度推进后永久移除或变形**。

它不是“房间内临时机关”，而是：

- 章节推进证据
- 世界状态变化证据
- 回路开启的物理化表现

### 为什么值得优先做

它能把“进度解锁”从 UI / 文案层，变成玩家在空间里真正看到的结果。

### 当前 Ark 底座

Ark 已有：

- `WorldEventTrigger`
- `ActivationGroup`
- `TilemapVariantSwitcher`
- `SaveBridge`

这套底座已经足够支持：

- 某个世界阶段 / 剧情阶段达成后
- 永久关闭某段阻挡物
- 或切换到另一个场景变体

### 建议翻译形态

- 塌方 debris 清除
- 主控系统解锁后的隔离墙打开
- 古代封堵门永久回收
- 区域大门旁的旁路被打通

### 建议挂点

- 若是**不可逆世界变化**：`Triggers/` + `WorldEventTrigger`
- 若是**表现对象本体**：`Elements/` 或 Tilemap variant

### MVP

先只做：

- 达成某条件后移除阻挡物
- 写入永久状态
- 重进场景后保持结果

### Phase 2

- 远程镜头拉取
- 变体切换伴随 VFX / SFX / 小地图更新
- 与门语义、捷径语义联动

---

## 4.5 阶段相位墙：`PhaseBarrier` / `TemporalWall`

### 定义

这类墙由**世界时钟 / 世界阶段**决定存在与否：

- 某一阶段存在
- 另一阶段消失
- 或在阶段切换时改变碰撞 / 可视状态

### 为什么值得优先做

`Project Ark` 已经有 `WorldClock + WorldPhaseManager`，所以这类墙是最能体现你们项目差异化的空间语法之一。

### 当前 Ark 底座

Ark 已有：

- `WorldClock`
- `WorldPhaseManager`
- `ScheduledBehaviour`
- `TilemapVariantSwitcher`

所以缺的通常不是底层能力，而是：

- 明确把“阶段性墙”定义成一类标准 authoring 件
- 给内容设计一个统一叫法和使用边界

### 建议翻译形态

- 时相壁
- 星蚀期能量膜
- 仅在夜相出现的骨桥封层
- 阶段切换时退去的酸雾封堵

### 建议挂点

- `Triggers/` 或 `DynamicWorld/` authoring 链
- 归类为 `Directing` 或 world-driven barrier

### MVP

先只做：

- 跟随 `WorldPhase` 启用 / 禁用一组墙体对象
- 支持 collider 与 visual 同步开关

### Phase 2

- 切换动画
- 阶段进入前后的提前预告
- 与隐藏区、危险区、Boss 前厅联动

---

## 4.6 投射物屏障墙：`ProjectileBarrier`

### 定义

这类墙**不一定挡玩家**，但会挡某些投射物、光束或敌我攻击。

### 为什么值得做

它能把“墙”从纯导航障碍，升级成**战斗语法**：

- 改变弹道
- 重塑掩体关系
- 创造武器差异表达
- 给房间加局部战术层

### Minishoot 启发

`BulletBlocker` 说明“墙”可以只挡子弹，不挡角色本体；这对俯视角射击游戏非常值钱。

### 当前 Ark 状态

Ark 具备碰撞、伤害、层级与投射物体系，但**还没有一个正式收口的投射物墙标准件**。

也就是说：

- **技术上能做**
- **设计上值得做**
- **但当前还不是现役标准语法**

### 建议翻译形态

- 共鸣屏障
- 只挡弹的能量幕
- 只允许某类核心家族穿透的时相膜
- Boss 房弹道分隔墙

### 建议挂点

- 以 `Elements/` 为主
- 若只负责战斗局部规则，可归 `Environment` 或独立 barrier authoring 件

### MVP

先只做：

- 挡投射物，不挡玩家
- 可配置敌我过滤
- 可配置是否被摧毁 / 关闭

### Phase 2

- 按伤害类型 / CoreFamily 过滤
- 与 `BreakableWall` 组合
- 反射 / 折射 / 共鸣穿透特殊交互

---

## 4.7 单向通行墙：`OneWayPassage` / `DirectionalGate`

### 定义

这类墙允许玩家**从一个方向穿过，但不能反向通过**。

### 为什么值得做

它非常适合银河城：

- 做下落井
- 做单向裂隙
- 做回返捷径
- 强化“先承诺、后兑现”的路线组织

### 当前 Ark 状态

Ark 目前没有看到现役收口的单向墙标准件，因此这类内容属于：

- **技术上可实现**
- **玩法上很有价值**
- **但建议排在第二批**

### 建议翻译形态

- 下降可过、上升不可返的裂井
- 顺流可过、逆流不可返的风道
- 从背面可穿、正面不可穿的相位膜

### 建议挂点

- `Elements/` 或 `Navigation` 扩展件
- 需要非常明确地定义方向语义与碰撞规则

### MVP

先只做：

- 单方向放行
- 基础碰撞切换
- 最少量的视觉提示

### Phase 2

- 与能力门组合
- 与 camera / one-way VFX 联动
- 支持不同实体类型采用不同规则

---

## 4.8 仪式封印墙 / Boss 墙：`CeremonySeal` / `BossBarrier`

### 定义

这类墙的重点不是“阻挡”，而是**章节感、门槛感和仪式感**。

### 为什么放在较后

它很重要，但通常依赖更明确的内容上下文：

- Boss 前厅
- 章节封印门
- 主线关键节点

如果没有对应内容场景，先做标准件意义不大。

### 当前 Ark 底座

可借：

- `Door`
- `ConnectionType`
- `WorldProgressManager`
- `WorldEventTrigger`

### 建议翻译形态

- 星图认证门
- 古代封印墙
- Boss 前厅收束墙
- 清场后升起 / 降下的仪式障壁

### 建议状态

**按内容需要做，不作为第一批基础墙件。**

---

## 5. 推荐落地顺序

如果按“最小可玩增量”排序，我建议这样推进：

1. **`BreakableWall`**
   - 最容易给玩家明确回报
   - 与现有持久化链最贴合
2. **`HiddenAreaMask`**
   - 最低成本补探索发现感
   - 几乎不需要改动全局架构
3. **`ProgressBarrier`**
   - 最适合把世界推进变成空间事实
4. **`PhaseBarrier`**
   - 最能体现 Ark 现有世界时钟差异化
5. **`ProjectileBarrier`**
   - 用来把房间战术层做厚
6. **`OneWayPassage`**
   - 用于更成熟的银河城路线设计
7. **`CeremonySeal`**
   - 在 Boss / 章节内容 ready 后加入

---

## 6. 对 authoring 的关键约束

## 6.1 不要做统一 `Wall.cs`

建议保持“墙家族”拆分，而不是做一个大而全的通用墙类。

## 6.2 视觉墙与碰撞墙解耦

- 看起来像墙，不代表它一定挡移动
- 会挡弹，不代表它一定挡玩家
- 会 reveal，不代表它应该进 `Elements`

## 6.3 会变化的墙必须进正确状态链

- **房间内永久变化** → `RoomFlagRegistry` + `SaveBridge`
- **世界进度不可逆变化** → `WorldEventTrigger` / 世界进度链
- **世界阶段切换变化** → `ScheduledBehaviour` / `WorldPhaseManager`

## 6.4 先做 authoring 标准件，再谈大范围泛化

先让一个代表性房间出现：

- 一堵可破坏墙
- 一处隐藏遮挡墙
- 一段进度封堵墙

只要这三件在同一切片里 work，墙家族就已经从“概念”进入“现役语法候选”。

---

## 7. 最终建议

### 现在就值得进入计划池的

- `BreakableWall`
- `HiddenAreaMask`（按墙语义重读）
- `ProgressBarrier`
- `PhaseBarrier`

### 技术上可做，但建议放第二批的

- `ProjectileBarrier`
- `OneWayPassage`

### 不建议现在单独起项的

- 纯静态普通墙（继续留在几何层即可）
- 纯仪式 Boss 墙（等具体内容房再做更值）

---

## 8. 对 Ark 的真正启发

`Minishoot` 给 Ark 的最大启发，不是“也做墙”，而是：

> **把墙从单一美术块，提升为一组能组织探索、战斗、导演与世界变化的标准件词汇。**

只要按这个方向推进，Ark 的墙就不会只是“挡路的砖”，而会变成：

- 探索回报
- 战斗规则
- 章节封锁
- 世界阶段证据
- 秘密发现装置

这才是它真正值得借的部分。
