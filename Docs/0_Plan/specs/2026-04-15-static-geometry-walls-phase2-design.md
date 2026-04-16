# 静态几何墙 Phase 2 设计稿

**日期：** 2026-04-15  
**适用对象：** `Level` 模块、`Level Architect`、静态几何墙 authoring、`Door` 协作规则、future transfer trigger 边界  
**设计阶段：** Phase 2（Workbench Polish / Boundary Hardening）

---

## 1. 背景

`静态几何墙 MVP` 已经完成第一版收口：

- 新房间具备 `Navigation/Geometry/OuterWalls/InnerWalls` 骨架
- `RoomGeometryRoot` 已作为 geometry authoring 锚点接入
- `LevelValidator` 已能检查最关键的 geometry 结构与碰撞链
- workflow 已明确：静态几何墙是 **scene-backed geometry**，不是运行时墙系统

但当前仍存在两个明显缺口：

1. **authoring 仍偏手工**  
   目前作者知道应该在 `Navigation/Geometry` 下画墙，但还缺少一套更顺手的标准起手式（空 Tilemap 画布、标准碰撞链、常见创建路径）。

2. **跨房间入口语义边界仍不够清楚**  
   讨论门洞协作时，很容易把“所有跨房间转移都应当要求墙体开口”混为一谈。事实上，未来可能存在：
   - 普通 `Door`
   - 层间门 / 阶段门
   - 掉落陷阱导致的跨房间转移
   - 叙事坠落 / 特殊传送

这些对象并不都属于“穿墙通路”。如果 Phase 2 不提前钉死边界，未来 validator 和 authoring 规则很容易误把 trap / fall / teleport 强行纳入门洞协作模型。

---

## 2. Phase 2 的核心定位

> **Phase 2 的目标不是自动生成墙形，而是把静态墙 authoring 变顺手，并把 `Door` 与非门型跨房间转移的边界正式写清。**

这意味着本阶段服务两个结果：

- **Workbench Polish**：让 `Level Architect` 能快速创建可画墙、可校验的标准 geometry 画布
- **Boundary Hardening**：让团队明确知道什么需要门洞协作，什么不需要

一句话总结：

> **Phase 1 搭对骨架；Phase 2 让作者更快起步，并防止 future transfer 语义污染静态墙主链。**

---

## 3. Goal / Scope / Architecture

### 3.1 Goal

建立静态几何墙的第二阶段 authoring 规范，使作者可以通过 `Level Architect` 快速创建标准墙画布，并让 `Door` 与 future trigger 的跨房间转移边界在 workflow、validator、工具入口三层保持一致。

### 3.2 Scope

本阶段只覆盖：

- `Level Architect` 的静态墙 **starter-first** 起手能力
- `OuterWalls / InnerWalls` 画布创建与标准组件链准备
- `Door` 与 geometry 开口的协作规则
- future transfer trigger 的边界说明与 validator 排除误报规则
- 代表房间 authoring 验收流程

### 3.3 Architecture

本阶段仍坚持以下架构约束：

- 静态几何墙继续是 **scene-backed Tilemap geometry**
- `Level Architect` 只负责 **starter / validator / 语义护栏**，不负责生成最终墙形
- `Door` 仍是 `Navigation` 下的 **Path authority**
- 掉落陷阱 / 叙事坠落 / 特殊传送不被强行收编为 `Door` 变体
- 不引入统一的 `RoomTransfer` 运行时抽象层

---

## 4. Phase 2 解决的问题

### 4.1 问题一：墙画布起手仍不顺手

当前作者需要自己：

- 进入正确根节点
- 新建 Tilemap 宿主
- 手动挂 `TilemapRenderer`
- 手动补 `TilemapCollider2D`
- 手动补 `Rigidbody2D(Static)` 与 `CompositeCollider2D`

这条链虽然不复杂，但频繁重复，会让“静态墙应走标准主链”的规范落地成本偏高。

### 4.2 问题二：门洞协作规则容易被过度泛化

当团队说“门洞要和墙体对齐”时，这句话只对 **Door 型通路**成立。

但以下情况并不等价：

- 玩家踩到塌陷地板，掉入下层房间
- 被环境陷阱吞没后转移到另一房间
- 叙事触发导致角色坠入新的 room
- 特殊传送点把玩家送到另一个空间

这些都可能跨房间，但它们的语义不是“穿过一扇门”。如果规则写得不精确，就会产生两类错误：

- authoring 上误以为所有跨房间转移都要开门洞
- validator 上误把 future trigger 判成“缺少墙体开口”

---

## 5. 设计原则

### 5.1 starter-first，而不是 auto-layout first

Phase 2 优先做：

- 标准创建入口
- 标准 hierarchy
- 标准 Tilemap 宿主
- 标准组件链
- 标准 validator 提示

而不是：

- 自动刷完整外轮廓
- 自动推断墙厚
- 自动推断门洞尺寸
- 自动创建复杂 traversal 结构

### 5.2 先区分“空间事实”与“转移动作”

- **静态墙** 负责“这里有没有实体边界 / 开口”
- **Door** 负责“从这里通向哪里”
- **Trap / Fall / Teleport** 负责“因事件或特殊机制被转移到哪里”

不要把空间边界 authoring 与跨房间转移动作绑成一个抽象对象。

### 5.3 validator 只检查当前 authority 应负责的东西

- `Door` 需要检查几何协作时，就检查开口对齐
- future trigger 没有门洞语义时，就不该被要求“必须有墙体开口”

validator 的价值是**减少误配**，不是把不同语义硬压成统一形态。

---

## 6. 正式分类：Door 型通路 vs 非门型跨房间转移

### 6.1 Door 型通路

适用对象：

- 普通 `Door`
- 层间门
- 阶段门
- 其他仍以 `Door` 组件表达的导航入口

语义特征：

- 属于 **Path**
- 默认挂在 `Navigation/Doors`
- 表达“玩家从这里穿过去”
- 需要明确 `_targetRoom` / `_targetSpawnPoint`
- 需要与空间边界形成可读的穿越关系

**结论：Door 型通路需要门洞协作规则。**

### 6.2 非门型跨房间转移

适用对象：

- 掉落陷阱导致的 room 转移
- 叙事坠落
- 特殊传送触发器
- 未来不以门洞表达的跨房间事件转移

语义特征：

- 本质是 **事件 / 机关 / 叙事 / hazard / directing** 触发
- 不一定位于 `Navigation/Doors`
- 不一定表达“穿过墙体”
- 可能根本没有“开口”这一视觉语义

**结论：非门型跨房间转移不强制要求墙体开口。**

### 6.3 当前策略

本阶段不去统一这两类对象的 runtime 抽象，只做：

- authoring 语义区分
- workflow 规则区分
- validator 检查边界区分
- future 扩展留口

---

## 7. Door 型通路的门洞协作规则

Phase 2 将把以下规则写成正式口径：

1. **几何开口由 `Geometry` author**  
   墙体哪里留空，首先是静态几何 authoring 的结果。

2. **连接语义由 `Door` author**  
   `Door` 决定目标房间、出生点、连接语义、阶段/进度门控。

3. **`Door` 不负责运行时挖墙**  
   不允许把 `Door` 设计成“进入时自动把 Tilemap 打洞”的 authority。

4. **`Door` 默认挂在 `Navigation/Doors`**  
   不将 `Door` 直接塞进 `OuterWalls` / `InnerWalls`。

5. **完成态必须满足“洞 + 门 + 连线”一致**  
   也就是：
   - 墙体有合理开口
   - `Door` 位置与开口大体对齐
   - `_targetRoom` / `_targetSpawnPoint` 完整

6. **validator 只做合理性检查，不做强自动改写**  
   提示门明显穿墙、明显偏离开口、缺引用，但不在 Phase 2 自动改墙形。

---

## 8. 非门型跨房间转移的边界规则

Phase 2 将明确写入以下规则：

1. **future trap / fall / teleport 可跨 room，但不要求门洞**
2. **它们不自动归类为 `Door`**
3. **它们的 validator 不应复用“门洞缺失”这类错误口径**
4. **后续若正式接入 `Level Architect`，应按其真实语义分类**
   - 若主要是环境危险 → 归 `Hazards`
   - 若主要是叙事或阶段触发 → 归 `Triggers`
   - 若未来确实形成稳定家族，再单独立项其 starter 与 validator 规范

### 8.1 对 `NarrativeFallTrigger` 的当前态度

当前项目里的 `NarrativeFallTrigger` 仍是 placeholder，不应在 Phase 2 被当成现役完整 authoring 目标。

因此本阶段只做两件事：

- 在文档里承认这类 future transfer 的合法性
- 在规则里防止它被误套 `Door` / 门洞语义

**明确不做：**

- 不将 `NarrativeFallTrigger` 正式纳入现役工具按钮
- 不为其编写完整 runtime / ceremony 方案
- 不把 Phase 2 扩成统一跨房间转移系统

---

## 9. Level Architect 的推荐增强形态

### 9.1 推荐新增：静态墙画布 starter

推荐在 `Level Architect` 内提供一类轻量入口，用于：

- 在 `OuterWalls` 或 `InnerWalls` 下创建标准 Tilemap 宿主
- 自动挂：
  - `Tilemap`
  - `TilemapRenderer`
  - `TilemapCollider2D`
  - `Rigidbody2D`（`Static`）
  - `CompositeCollider2D`
- 自动设置最小可用默认值
- 创建后聚焦该对象，便于作者立即开始画墙

### 9.2 推荐入口形态

坚持 **starter-first**，不做重 UI。

推荐形态可以是：

- `Create Outer Wall Canvas`
- `Create Inner Wall Canvas`
- 或统一的 `Create Geometry Tilemap Canvas`，再让用户选择挂点

关键不是按钮数量，而是：

- 创建后一定在正确根下
- 创建后默认结构符合 validator 预期
- 创建后可以直接开始 Tilemap authoring

### 9.3 Phase 2 不做的工具能力

- 不自动刷整圈外轮廓
- 不自动根据 room bounds 生成墙体
- 不自动创建门洞
- 不自动推断转角、墙厚、封闭轮廓
- 不自动为 future trap / fall 建跨房间转移链

---

## 10. Validator / Audit 边界

### 10.1 Phase 2 应新增或强化的检查

对于静态墙 / Door 协作，建议 validator 增强以下方向：

- `Door` 是否在正确根下
- `Door` 是否缺 `_targetRoom` / `_targetSpawnPoint`
- `Door` 是否明显放在实体墙中心而没有对应开口
- `Door` 是否明显偏离其几何开口
- `OuterWalls_*` / `InnerWalls_*` Tilemap 是否挂在正确 geometry 根下
- 标准 Tilemap 碰撞链是否完整

### 10.2 Phase 2 明确不要误报的情况

以下情况不应被报成“缺少门洞”或“Door 几何协作错误”：

- 不在 `Navigation/Doors` 下的 future transfer trigger
- 叙事掉落触发器
- 陷阱导致的跨房间转移原型
- 特殊 teleport / event transfer 原型

### 10.3 validator 的态度

validator 在本阶段应该：

- **对 `Door` 更严格**
- **对 future transfer 更克制**
- **优先减少错误引导，而不是提前替未来系统定死形态**

---

## 11. 代表房间验收建议

Phase 2 不应只做工具按钮，还必须跑一轮代表房间验证。

建议至少覆盖：

### 11.1 普通门洞房

验证：

- `OuterWalls` 画墙流程是否顺手
- `Door` 与开口对齐是否易理解
- validator 能否抓到明显的穿墙 / 错根问题

### 11.2 复杂内外墙房

验证：

- `OuterWalls / InnerWalls` 的语义是否足够清楚
- 多个 Tilemap 宿主的命名和层级是否仍可管理
- starter 是否真的减少重复手工操作

### 11.3 future boundary case（只做边界验证）

至少构造一个不依赖门洞的 future case，例如：

- 一个“地面塌陷后掉落到另一房间”的占位案例

这里不要求完整实现玩法，只验证：

- 团队不会误认为必须开墙洞
- validator 不会把它误报成 Door 协作错误
- 文档分类足够清楚

---

## 12. 与其他专项的关系

### 12.1 与 `LevelArchitect_Workbench`

本设计可视为 `LevelArchitect_Workbench` 的一个具体切片：

- 它不是新增第二套 authority
- 而是把 `scene-backed authoring workbench` 的理念落到静态墙上

### 12.2 与 `LevelRoomRuntimeChain_Hardening`

本设计不修改 `Room` 运行时 owner 主链。

它只处理：

- authoring 起手效率
- validator 边界
- `Door` / future transfer 的规则清晰度

因此应避免把 Phase 2 扩写成运行时转移系统收口计划。

---

## 13. 非目标（明确不在本阶段）

以下内容明确不属于本阶段：

- `BreakableWall`
- `PhaseBarrier`
- `ProjectileBarrier`
- 完整 trap family starter
- 完整 `NarrativeFallTrigger` 正式化
- 统一 `RoomTransfer` 抽象层
- 自动生成外轮廓 / 门洞 / 墙厚
- 把所有跨房间转移统一压成一个 validator 模型

---

## 14. Done Checklist

当以下 5 条成立时，可认为 `静态几何墙 Phase 2` 设计目标达成：

1. `Level Architect` 能创建标准静态墙 Tilemap 画布，而不是只靠手工空建对象
2. `Door` 的门洞协作规则被正式写入 workflow / plan / 验收口径
3. future trap / fall / teleport 被明确排除出“必须有门洞”的规则
4. validator 对 `Door` 更清晰，但不会对 future transfer 产生误报
5. 至少一轮代表房间 authoring 验证覆盖了普通门洞房、复杂内外墙房与一个 future boundary case

---

## 15. 推荐的下一步

本设计确认后，下一步应进入 implementation planning，并把工作拆成三个批次：

1. **Starter Batch**
   - 静态墙 Tilemap 画布创建入口

2. **Validator Batch**
   - `Door` / geometry 协作检查与误报边界收口

3. **Validation Batch**
   - 代表房间 authoring 验收与 workflow 文档同步

这三个批次应继续遵循：

- 先让它 work
- 再让 authoring 更顺手
- 最后再考虑更强自动化

---

## 16. 最终结论

`静态几何墙 Phase 2` 不应被做成“自动建墙系统”，也不应被扩写成“统一跨房间传送系统”。

它更准确的职责是：

> **把静态墙的标准起手式做好，把 `Door` 的门洞协作钉死，同时给 future trap / fall / teleport 留出正确边界。**

这样后续无论扩展：

- `BreakableTraversalBlocker`
- `Trap starter`
- `OneWayDrop`
- `NarrativeFallTrigger`
- 其他特殊跨房间入口

都不会反过来污染静态墙主链，也不会把 validator 重新拖回“所有入口都长得像门”的混乱状态。
