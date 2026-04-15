# 静态几何墙设计稿

**日期：** 2026-04-15  
**状态：** 已达成设计共识，待实现规划  
**适用对象：** `Level` 模块、`Level Architect`、房间几何 authoring、后续墙家族扩展

---

## 1. 背景

当前 `Project Ark` 已经完成 `Level` 模块主体建设，并进入**场景配置与验证阶段**。

在这一阶段，项目已经具备：

- `Room` / `Door` / `RoomManager` 的基础房间运行链
- `DestroyableObject`、`HiddenAreaMask`、`BiomeTrigger`、`WorldEventTrigger` 等带语义的房间元素
- `LevelValidator` 与 `Level Architect` 的基础护栏与 starter 工具链

但仍缺少一条清晰、统一、可校验的**静态几何墙 authoring 规范**。

当前问题不在于“能不能摆墙”，而在于：

- 房间外轮廓与内部隔断缺少统一挂点
- 静态几何墙还没有被明确纳入 `Level` 的 authoring 语言
- validator 缺少稳定锚点去识别墙几何主链
- 后续若加入 `BreakableWall`、`PhaseBarrier`、`ProjectileBarrier`，容易把“静态空间事实”和“变化玩法墙”混在一起

因此，本设计的目标不是发明一个新的 runtime 墙系统，而是：

> **先把静态几何墙收口为一条稳定的场景几何主链。**

---

## 2. 目标与非目标

## 2.1 目标

本设计希望达成以下结果：

1. 让静态几何墙同时覆盖：
   - 房间外轮廓 / 主舱壁
   - 房内局部隔断 / 柱体 / 挡路块
2. 但第一版重点放在：
   - **先把外轮廓墙 authoring 收稳**
3. 给静态几何墙建立统一的：
   - 场景挂点
   - 层级结构
   - Tilemap / Collider 组合建议
   - validator 契约
4. 让 `Level Architect / RoomFactory` 至少能创建墙几何骨架，而不是继续完全靠作者手工搭根节点
5. 为后续墙家族扩展提供清晰分层基础：
   - `BreakableWall`
   - `PhaseBarrier`
   - `ProjectileBarrier`
   - `OneWayPassage`

## 2.2 非目标

以下内容不在本轮设计范围内：

- 不创建统一的 `Wall.cs` 运行时系统
- 不实现可破坏墙、隐藏墙、阶段墙、投射物墙的具体逻辑
- 不引入新的墙状态 manager
- 不自动生成具体墙形、门洞、墙厚、闭环轮廓
- 不扩展复杂 JSON schema 或把墙形数据移出 Scene
- 不替代 Unity 原生的 Tilemap authoring 工作流

---

## 3. 方案比较与结论

本轮讨论过三种可选方向：

## 3.1 方案 A — 纯场景几何

### 形态

- 只使用 `Tilemap + TilemapCollider2D + CompositeCollider2D`
- 不引入任何新的墙相关组件

### 优点

- 最轻
- 最符合“空间事实留在 Scene”原则
- 几乎没有新增维护成本

### 缺点

- 缺少 validator / tooling 锚点
- 根节点结构容易漂移
- 后续接入 `Level Architect` 时没有稳定目标

## 3.2 方案 B — 轻量根组件 + 纯几何本体

### 形态

- 墙本体继续是 `Tilemap + Collider`
- 在 `Room` 结构内增加统一的 `Geometry` 根
- 在 `Geometry` 上挂一个**超轻的结构标记组件**，仅作为 validator / tooling 入口

### 优点

- 仍然遵守 Scene 作为空间 authority 的原则
- 给 authoring、validator、工具链提供统一锚点
- 对房间外轮廓优先的目标最友好
- 最容易平滑升级到 `BreakableWall`、`PhaseBarrier` 等变化墙语义

### 缺点

- 会新增一个轻量组件与一条层级规范
- 需要补充 validator 与文档口径

## 3.3 方案 C — 轻量根组件 + 直接深接 `Level Architect`

### 形态

- 包含方案 B
- 再让 `Level Architect / RoomFactory` 更深入参与墙几何模板与自动结构生成

### 优点

- 工具链完整
- 后续搭建效率更高

### 缺点

- 在 authoring 规则尚未稳定前容易把不成熟规则硬编码进编辑器
- 会把“规范未验证完成”的问题提前转成“工具维护问题”

## 3.4 最终结论

本项目选择：

> **方案 B：轻量根组件 + 纯几何本体。**

并将方案 C 作为后续阶段增强，而不是第一版目标。

---

## 4. 最终设计结论

## 4.1 顶层定位

静态几何墙的本质不是一个新的玩法家族，而是：

- `Scene` 中的**空间事实**
- `Navigation` 侧的几何子层
- 非状态件
- 非交互件
- 非导演件

因此，它不应被错误地挂到：

- `Elements`
- `Triggers`
- `Hazards`

它应作为 `Navigation` 的一个明确几何分支存在。

## 4.2 玩家体验目标

这一版给玩家带来的收益不是“多了一个墙系统”，而是：

- 房间边界更可信
- 导航阻挡更稳定
- 视觉读图与碰撞结果更一致
- 后续变化墙可以建立在稳定空间骨架上，而不是不断修基础几何

一句话：

> **先把世界的实体感做稳。**

---

## 5. 推荐层级结构

每个 `Room` 下面新增并固定以下结构：

```text
Room_[ID]
├── Navigation
│   ├── Doors
│   ├── SpawnPoints
│   └── Geometry
│       ├── OuterWalls
│       └── InnerWalls
├── Elements
├── Encounters
├── Hazards
├── Triggers
└── CameraConfiner
```

## 5.1 挂点说明

- `Navigation/Geometry`
  - 静态几何墙统一根节点
  - 表达房间的空间阻挡事实
- `Navigation/Geometry/OuterWalls`
  - 房间外轮廓 / 主舱壁 / 主要边界封闭
- `Navigation/Geometry/InnerWalls`
  - 房内局部隔断 / 柱体 / 挡路块 / 纯静态掩体厚墙

## 5.2 为什么放在 `Navigation`

因为它服务的是：

- 空间边界
- 通行阻挡
- 几何事实

而不是：

- 互动逻辑
- 持久状态
- 导演逻辑

将其放在 `Navigation` 下，可以保持 `Level_CanonicalSpec` 中“场景空间事实留在 Scene”的原则不被破坏。

---

## 6. 命名规范

## 6.1 固定节点命名

- `Geometry`
- `OuterWalls`
- `InnerWalls`

## 6.2 Tilemap 节点命名建议

第一版优先采用**功能命名**，而不是美术主题命名：

- `OuterWalls_Main`
- `OuterWalls_Collision`
- `InnerWalls_Main`
- `InnerWalls_Blockers_01`

## 6.3 命名原则

- 优先让 validator 和工具链容易识别“谁是主几何链”
- 不要求第一版就完美覆盖所有美术分层习惯
- 如果后续确实需要细分视觉层与碰撞层，再在既有结构内扩展，而不是推翻根命名

---

## 7. 组件组合建议

## 7.1 核心原则

静态几何墙的**本体仍然是 Tilemap 几何**，不是运行时行为对象。

## 7.2 推荐最小组合

对承担主碰撞责任的墙 Tilemap，推荐组合为：

- `Tilemap`
- `TilemapRenderer`
- `TilemapCollider2D`
- `Rigidbody2D`（`Static`）
- `CompositeCollider2D`

## 7.3 外轮廓优先策略

第一版应优先保证：

- `OuterWalls` 中至少存在一条稳定的主碰撞链
- 其承担房间外轮廓与主舱壁的空间事实

## 7.4 内部分隔策略

`InnerWalls` 第一版允许：

- 使用单一 Tilemap
- 或拆成少量局部 Tilemap

但仍需保持：

- 纯静态几何语义
- 不承担可破坏、可 reveal、可阶段切换等变化玩法

## 7.5 当前不建议做的拆分

第一版不建议：

- 过早把视觉墙和碰撞墙拆成很多层
- 把隐藏墙、可破坏墙、相位墙直接混进 `InnerWalls` / `OuterWalls`
- 为静态墙引入脚本驱动的运行时 owner

---

## 8. `RoomGeometryRoot` 设计

## 8.1 是否需要

需要。

但它必须是一个**超轻组件**。

## 8.2 角色定位

`RoomGeometryRoot` 的职责仅限于：

- 作为 validator 锚点
- 作为 editor / tooling 识别入口
- 作为未来 `Level Architect` 创建墙几何骨架时的稳定挂点

## 8.3 禁止事项

`RoomGeometryRoot` 不得：

- 管理墙启停
- 驱动运行时逻辑
- 缓存房间墙列表做主流程控制
- 订阅世界阶段 / 房间切换事件
- 尝试替代 Tilemap / Collider 作为空间真相源

## 8.4 设计原则

一句话：

> **它是结构标记，不是行为控制器。**

---

## 9. Validator 契约

第一版 `LevelValidator` 只增加**少量但高价值**检查，不做复杂几何分析。

## 9.1 结构存在性检查

针对每个 `Room`：

- `Navigation` 是否存在
- `Navigation/Geometry` 是否存在
- `Navigation/Geometry/OuterWalls` 是否存在
- `Navigation/Geometry/InnerWalls` 是否存在

### 建议严重级别

- `Geometry` 缺失：`Warning`
- `OuterWalls` 缺失：`Warning`
- `InnerWalls` 缺失：`Info` 或 `Warning`

### 设计理由

第一版仍处于规范推广阶段，旧房间允许渐进迁移，不宜一上来把历史内容全部打成 fatal。

## 9.2 根组件检查

对 `Navigation/Geometry`：

- 是否挂有 `RoomGeometryRoot`
- 是否只挂了一个 `RoomGeometryRoot`
- 是否出现明显不该存在的重 runtime 逻辑组件

### 建议严重级别

- 缺 `RoomGeometryRoot`：`Warning`
- 多个 `RoomGeometryRoot`：`Error`
- 出现不该存在的重组件：`Warning` 或 `Error`

## 9.3 Tilemap 碰撞链检查

对 `OuterWalls` 下的主碰撞链，至少检查：

- 存在 `Tilemap`
- 存在 `TilemapCollider2D`
- 若存在 `CompositeCollider2D`，则必须有 `Rigidbody2D`
- `Rigidbody2D` 必须为 `Static`
- TilemapCollider 的 composite 配置正确

### 建议严重级别

- 缺 `TilemapCollider2D`：`Error`
- `CompositeCollider2D` 存在但无 `Rigidbody2D`：`Error`
- `Rigidbody2D` 不是 `Static`：`Error`
- 空 Tilemap：`Info`

## 9.4 语义层级检查

- 主边界墙应位于 `OuterWalls`
- 局部静态隔断应位于 `InnerWalls`
- 不允许把主静态墙主链长期放在 `Elements` 或 `Triggers`

### 建议严重级别

- 在 `Elements` 下发现疑似主墙 Tilemap：`Warning`
- 在 `Triggers` 下发现带实体碰撞的墙 Tilemap：`Warning`

## 9.5 第一版不做的校验

暂不检查：

- 墙是否闭合成环
- 门洞尺寸是否合法
- 边界是否绝对无漏缝
- 墙几何与 minimap bounds 的自动比对
- 复杂几何自交或拓扑正确性

原因：第一版先抓**结构错误**，不抓**几何美术正确性**。

---

## 10. `Level Architect / RoomFactory` 接入边界

## 10.1 第一阶段允许接入的能力

当新建 `Room` 时，工具链应自动创建：

```text
Navigation
├── Doors
├── SpawnPoints
└── Geometry
    ├── OuterWalls
    └── InnerWalls
```

同时：

- 在 `Geometry` 上挂 `RoomGeometryRoot`
- 可选创建空的 `OuterWalls_Main`
- 可选创建空的 `InnerWalls_Main`

## 10.2 第一阶段明确不做的能力

不应自动：

- 生成整圈外轮廓
- 推断门洞
- 决定墙厚
- 生成内部隔断模板
- 生成复杂墙 schema

## 10.3 设计理由

当前最重要的是：

- 固化 authoring 语言
- 固化层级结构
- 固化 validator 抓手

而不是提前把不成熟的墙规则写死进工具。

一句话：

> **第一阶段只建骨架，不替作者设计墙形。**

---

## 11. 数据流与 authority

## 11.1 运行时 authority

静态几何墙没有独立运行时 owner。

其 authority 为：

- `Scene` 中的 `Navigation/Geometry/OuterWalls`
- `Scene` 中的 `Navigation/Geometry/InnerWalls`
- Tilemap / Collider 的实际物理结果

## 11.2 `Room` 的关系

`Room` 仍是房间空间语境 owner，但不需要主动管理静态墙状态。

## 11.3 `RoomGeometryRoot` 的关系

`RoomGeometryRoot` 只是结构锚点，不参与运行时主链。

## 11.4 核心原则

> **静态几何墙 = 空间事实，不是玩法状态。**

---

## 12. 风险与防御措施

## 12.1 风险一：静态墙与玩法墙混用

### 风险表现

- 把 `BreakableWall` 直接画进 `OuterWalls`
- 把 `HiddenAreaMask` 混入主碰撞几何链
- 把 `PhaseBarrier` 当普通 Tilemap 做开关

### 防御措施

明确分层：

- 静态几何墙 → `Navigation/Geometry`
- 状态墙 / 交互墙 → `Elements`
- 导演 / 阶段 / 世界变化墙 → `Triggers` / `DynamicWorld`

## 12.2 风险二：过早自动化

### 风险表现

- 还没跑过真实 authoring，就让工具自动生成墙形
- 后续一旦规则变化，工具和内容会一起失真

### 防御措施

第一阶段只做：

- 骨架
- 校验
- 结构规范

不做几何形状自动化。

## 12.3 风险三：`RoomGeometryRoot` 长成 manager

### 风险表现

- 开始收墙列表
- 运行时自动修配置
- 参与房间切换或状态逻辑

### 防御措施

在设计和实现中明确写死边界：

- marker only
- validator anchor
- tooling anchor

## 12.4 风险四：旧房间一次性被新规范打爆

### 防御措施

- 旧房间先以 `Warning` 渐进迁移
- 新房间从创建骨架开始走新规范
- 等代表房验证完成后，再评估是否提高 validator 严格度

---

## 13. 升级路径

## 13.1 升级到 `BreakableWall`

未来应以“局部替换段”或独立语义件的方式存在，而不是直接污染 `OuterWalls` 主链。

## 13.2 升级到 `PhaseBarrier`

应走世界阶段 / 导演链，而不是让 phase 切换直接改主静态墙 authority。

## 13.3 升级到 `ProjectileBarrier`

应作为战斗规则件存在，而不是吞并静态墙概念。

## 13.4 升级到更强 `Level Architect`

推荐顺序：

1. 创建骨架
2. 创建空 Tilemap 画布
3. 增强 audit / validator
4. 最后才考虑模板化外轮廓与门洞协作

---

## 14. 完成定义（Done Checklist）

当以下 5 条满足时，可视为第一版静态几何墙设计目标达成：

1. `Room` 存在统一的 `Navigation/Geometry` 挂点
2. `OuterWalls` 与 `InnerWalls` 语义分层固定
3. 静态墙本体继续使用 `Tilemap + Collider`，不引入重 runtime 墙系统
4. `LevelValidator` 能抓到最关键的结构与碰撞链错误
5. `Level Architect / RoomFactory` 只接到骨架创建，不接具体墙形生成

---

## 15. 最终结论

本设计的本质，不是在做一个“墙系统”，而是在给 `Level` 模块补一条**不会烂掉的空间骨架**。

只要第一版先把静态几何墙收口为：

- `Navigation/Geometry`
- `OuterWalls`
- `InnerWalls`
- `RoomGeometryRoot`
- `Tilemap + Collider`
- 最小 validator + starter-first 工具边界

那么后续再做：

- `BreakableWall`
- `HiddenAreaMask`
- `ProgressBarrier`
- `PhaseBarrier`
- `ProjectileBarrier`

都会明显更稳、更快，也更不容易把不同语义的“墙”搅成一团。
