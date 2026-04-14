# Level Architect 语义化 authoring 边界设计

**日期：** 2026-04-14  
**状态：** 已达成设计共识，待实现规划  
**适用对象：** `Level Architect` 后续 room element 迭代、编辑器 authoring 入口设计、validator 扩展

---

## 1. 背景

当前 `Level Architect` 已经进入可用的房间/连接 authoring 阶段，并且已经具备一批 starter objects：

- `Checkpoint`
- `OpenEncounterTrigger`
- `BiomeTrigger`
- `ScheduledBehaviour`
- `WorldEventTrigger`
- `Lock starter`（连接级）

接下来会自然出现新的 room elements 需求，例如：

- 陷阱
- 可互动植物
- 可破坏或可切换的路径阻挡物
- 其他与探索、战斗、世界阶段有关的对象
- 大量纯 decoration 单品

这里最大的风险不是“少一个按钮”，而是 **`Level Architect` 失去边界**：

- 如果所有对象都收进 `Level Architect`，它会膨胀成第二个 `Hierarchy + Project`，逐渐变成臃肿的摆景器。
- 如果所有对象都继续直接从 `Project` 面板手工拖，又会失去语义入口、validator 约束和统一作者体验。

因此需要先定义清楚：**未来什么应该进入 `Level Architect`，什么不应该。**

---

## 2. 最终设计结论

### 2.1 定位

`Level Architect` 的长期定位应当是：

> **关卡语义编辑器（semantic authoring tool），而不是万能摆件编辑器。**

它负责的是：

- 影响玩家通行 / 战斗 / 探索判断的对象
- 需要和 `Room`、`Door`、`Phase`、`Lock`、`Save` 等系统建立语义关系的对象
- 容易因为层级、引用、碰撞体、root、layer、业务配置错误而出问题的对象
- 值得建立 starter + validator 约束的高复用对象

它**不负责**：

- 纯 decoration 单品
- 纯视觉 dressing
- 没有玩法语义、没有状态、没有系统绑定的摆景资产

### 2.2 总原则

- **`Level Architect`**：负责**语义元素**
- **`Project` 面板**：负责**纯装饰资产**

这条边界必须长期保持稳定，否则工具会很快失去聚焦。

---

## 3. 目标与非目标

## 3.1 目标

这份设计希望达到以下结果：

1. 让团队在新增 room element 时，能快速判断它是否应该进入 `Level Architect`
2. 让 `Level Architect` 继续保持锋利，而不是变成全量摆景系统
3. 让真正高风险、高复用的语义元素逐步进入 starter + validator 闭环
4. 让纯美术 dressing 继续保留快速、自由、低成本的直接摆放流程

## 3.2 非目标

以下内容不在本轮设计目标内：

- 不把所有 `Level` 运行时组件都接进 `Level Architect`
- 不做 decoration 单品浏览器
- 不做美术摆景工作流重构
- 不要求每个语义元素一开始就具备完整 Inspector authoring
- 不尝试用 `Level Architect` 取代 Unity 原生 `Hierarchy / Inspector / Project` 工作流

---

## 4. 准入模型：什么元素能进 Level Architect

本项目选择的判断方法是：

> **分级候选制 + 保守收口。**

即：

- 元素只要满足任一语义条件，就可以进入“候选池”
- 但正式进入 `Level Architect` 前，要采用**保守收口**策略，只收最硬核、最值得建立 authority 的对象

## 4.1 候选判定条件

一个 room element 若满足以下任一项，可进入候选池：

1. **会影响玩家通行 / 战斗 / 探索**
2. **需要 validator 兜底**（root、layer、collider、引用等容易配错）
3. **需要与 `Room` / `Door` / `Phase` / `Lock` / `Save` 绑定**
4. **高复用**，会被频繁重复 author
5. **需要 starter/scaffold** 才能保证一致性与搭建速度

## 4.2 正式准入规则（保守版）

为了避免 `Level Architect` 继续变胖，本项目后续采用如下准入规则：

### Rule A — 必须有明确玩法语义

元素必须**明显影响**以下至少一类体验：

- 通行
- 战斗
- 探索

如果一个对象删除后，主要损失只是“画面层次感”而不是“玩家行为判断”，那么它默认**不进入** `Level Architect`。

### Rule B — 再满足以下任一附加条件

在满足 Rule A 的前提下，还应至少满足以下一条：

- 容易因为 authoring 失误产生 bug
- 需要系统级绑定（门、阶段、锁、存档、房间状态）
- 有明显的批量复用价值
- 值得建立标准 starter 和 validator 规则

### Rule C — 纯 decoration 默认排除

以下对象默认不进入 `Level Architect`：

- 小型摆件
- 纯装饰植物
- 光源摆设
- 墙面杂物
- 只承担气氛功能的散件 prefab

除非未来出现新的成组 dressing 工具需求，否则继续直接从 `Project` 面板摆放。

---

## 5. 元素分层模型

为了让后续评审更快，room elements 应被拆成 3 层：

## 5.1 Core Semantic（进入 Level Architect）

这类对象满足硬语义条件，应进入 `Level Architect`。

特征：

- 强玩法影响
- 配置风险高
- 有统一 root / collider / 引用规范
- 值得扩展 validator

示例：

- `Trap`
- `BreakableTraversalBlocker`
- `HiddenAreaMask`
- `ActivationGroup`
- `WorldPhaseBoundObject`
- `Door/Progression` 强绑定对象

## 5.2 Soft Semantic（暂不直接进入主链）

这类对象带有一定交互或状态，但当前不一定值得立刻进入 `Level Architect` 主链。

特征：

- 有轻度玩法语义
- 但复用度、风险或统一性尚未证明
- 适合先通过 prefab 手工摆放观察真实使用频率

示例：

- 某些轻交互植物
- 轻度反馈对象
- 只在单一章节出现的小型机关

这类对象未来可以升级为 `Core Semantic`，但前提是它们的 authoring 痛点被反复验证。

## 5.3 Pure Dressing（明确留在 Project）

这类对象不进入 `Level Architect`。

示例：

- 石块、藤蔓、蘑菇、灯、招牌、碎片、墙饰
- 纯氛围植物
- 背景装饰件
- 不影响逻辑的视觉 prefab

---

## 6. 推荐的实现策略：starter-first

对于允许进入 `Level Architect` 的新语义元素，默认不直接做“完整 authoring 面板”，而是采用：

> **starter-first**

即：

- 先给一个标准创建入口
- 自动创建正确 root、默认子对象、基础 collider、最常见组件
- 自动挂最小必要的默认值
- 自动聚焦新对象
- 把业务配置留给作者手动补完

### 6.1 为什么用 starter-first

原因：

1. 当前项目更需要**搭建速度**，不是一次性做完整复杂 UI
2. 这和现有 `Checkpoint / OpenEncounterTrigger / BiomeTrigger / ScheduledBehaviour / WorldEventTrigger / Lock` 的模式一致
3. 它能先验证“这个元素到底值不值得纳入 LA”
4. 能避免在需求还不稳定时把 Inspector 设计做死

### 6.2 starter-first 的完成定义

一个新 starter 至少应做到：

- 创建到正确根节点（如 `Elements` / `Triggers` / `Hazards` 等）
- 创建后层级结构符合 validator 预期
- 创建后默认 collider / 组件结构可直接进入手工细调
- 明确日志提示“还差哪些业务配置”
- 创建后 Play Mode 不会因为空引用直接炸掉

---

## 7. 未来元素的推荐入口策略

## 7.1 应进入 Level Architect 的第一批候选

### 陷阱（高优先级）

应进入 `Level Architect`。

原因：

- 强烈影响通行 / 战斗 /探索
- 高复用
- 很容易出 collider、layer、root、damage、trigger 配置错误
- 非常适合建立 starter + validator 规范

建议形态：

- `Trap starter`
- 后续按家族细分：`SpikeTrap`、`LaserTrap`、`GasTrap`、`Crusher`

### 可互动植物（分裂处理）

不应整体作为一类收进 `Level Architect`，而应拆分：

#### 进入 LA 的情况

当植物具备以下性质时，应进入 `Level Architect`：

- 阻路 / 改变路线
- 可破坏后打开路径
- 触发毒雾 / 火焰 / 掉落 / 机关
- 随世界阶段或房间状态切换
- 属于探索或战斗决策的一部分

#### 不进入 LA 的情况

以下植物继续通过 `Project` 面板摆放：

- 纯装饰植物
- 只负责摇摆、发光、营造气氛的植物
- 不参与逻辑、不参与掉落、不参与探索判断的植物

### 可破坏路径阻挡物（高优先级）

应进入 `Level Architect`。

原因：

- 明确影响探索 / 通路
- 很适合统一语义命名与 validator 检查
- 容易和 secret、reward、ability gate 关联

### 隐藏区与激活组（高优先级）

应逐步进入 `Level Architect`。

原因：

- 当前 validator 已经覆盖
- 说明这些对象已经具有足够明确的 authoring 规范
- 现在缺的是创建入口，而不是概念成熟度

## 7.2 不建议进入 Level Architect 的对象

以下对象默认继续走 `Project` 面板：

- decoration 单品
- 纯美术植物
- 纯氛围小机关（没有玩法影响）
- 小型散件 dressing
- 背景摆景件

如果未来真的需要提高这类对象的铺设效率，建议走：

- `Decor Cluster`
- `Biome Dressing Kit`
- 批量 stamp / brush

而不是把每个散件都做成 `Level Architect` 按钮。

---

## 8. 推荐的产品边界

以后团队内部提到 `Level Architect` 时，建议统一使用以下口径：

> **`Level Architect` 是关卡语义 authoring 工具，不是全量美术摆景工具。**

也就是说：

- **LA 负责**：玩法语义、starter、validator、系统绑定
- **Project 负责**：dressing、氛围、散件 prefab

这条口径的价值在于：

- 后续评审不会再反复争论“为什么这棵草没有按钮”
- 团队能更早识别出“真正值得工具化的对象”
- `Level Architect` 的 UI 不会被大量低价值入口淹没

---

## 9. 未来迭代顺序建议

推荐按以下顺序扩展：

1. **Hazards 家族**
   - `Trap starter`
   - `SpikeTrap`
   - `LaserTrap`
   - `GasHazard`
   - `Crusher`
2. **Traversal / Secret 家族**
   - `BreakableTraversalBlocker`
   - `HiddenAreaMask`
   - `OneWayDrop`（如未来需要）
3. **Interaction 家族**
   - `BreakablePlant`
   - `BurnableVine`
   - 其他真正会影响探索路径的互动植物
4. **Group / World 家族**
   - `ActivationGroup`
   - phase/world 绑定 set piece

这个顺序的原则是：

- 先做最强语义、最高风险、最高复用的元素
- 后做灰区元素
- 永远不把纯 decoration 加进主链

---

## 10. 验收标准

如果这条设计被执行，后续新增一个 room element 时，应满足以下验收标准：

1. 团队可以在评审时明确判断：它属于 `Level Architect` 还是 `Project`
2. 新增的 `Level Architect` 元素都能说明自己的语义价值，而不是“只是为了方便拖 prefab”
3. `Level Architect` 新增入口时，至少是 starter-first，而不是半成品复杂 Inspector
4. validator 会只覆盖真正值得建立规范的对象，而不是无限扩张
5. 纯 decoration 工作流保持轻量，不被工具化改造拖慢

---

## 11. 风险与防御

### 风险 1：灰区元素持续膨胀

例如“可互动植物”如果不拆分，很容易整个类别都被收进 LA。

**防御：** 永远以“是否影响通行 / 战斗 / 探索”为第一判断条件，而不是看它是不是“有点互动”。

### 风险 2：starter 越堆越多但没有 validator 跟上

如果只加按钮不补规则，后面会重新回到乱摆状态。

**防御：** 每新增一个 starter，都必须同时补：

- 根节点规则
- 组件最低结构
- 至少一条 validator 检查
- 一条“创建后还差什么”的日志提示

### 风险 3：纯美术对象借“方便”之名混入 LA

短期看方便，长期会把 LA 变成摆景杂物间。

**防御：** 纯 decoration 永远不纳入主链；若未来确实有铺设效率问题，单独设计 `Decor Cluster / Dressing Kit` 工具，不污染语义 authoring 主线。

---

## 12. 最终一句话结论

**未来 `Level Architect` 的正确扩展方向不是“把更多东西塞进去”，而是“只把真正需要语义 authority 的 room elements 收进去，并优先用 starter-first 方式落地”。**

这意味着：

- **陷阱** 应进入 `Level Architect`
- **有玩法语义的可互动植物** 应进入 `Level Architect`
- **纯 decoration 单品** 不应进入 `Level Architect`
- **`Level Architect` 要守住“硬语义 authoring 工具”的定位**
