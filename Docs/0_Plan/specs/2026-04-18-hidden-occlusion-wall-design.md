# 隐藏遮挡墙设计稿

**日期：** 2026-04-18  
**状态：** 已达成设计共识，待实现规划  
**适用对象：** `Level` 模块、`HiddenAreaMask`、秘密区域 authoring、后续 `Level Architect` starter-first 收口

---

## 1. 背景

当前 `Project Ark` 已经具备一条可用但尚未完全产品化的“隐藏区域 reveal”底座：

- 运行时已有 `HiddenAreaMask`
- `LevelValidator` 已覆盖其 trigger / layer mask / 基础可见体校验
- `Level_CanonicalSpec` 已明确它属于 `Directing`，推荐挂在 `Triggers`
- `Level Architect` 的长期边界设计，也已把 `HiddenAreaMask` 列为适合逐步纳入 starter-first 的语义元素

因此，这一轮真正的问题不是“能不能做 reveal”，而是：

- **隐藏遮挡墙到底算什么语义对象？**
- **它和静态几何墙 / 可破坏墙 / 未来投射物屏障的边界在哪里？**
- **后续要不要给它做 authoring 入口，以及入口应该落在哪一层？**

本设计的目标不是再发明一个新的 runtime 墙系统，而是：

> **把“隐藏遮挡墙”收口成一类稳定、可沟通、可扩展的秘密区域 authoring 语法。**

---

## 2. 目标与非目标

## 2.1 目标

本轮设计希望达成以下结果：

1. 明确隐藏遮挡墙的玩家体验目标：
   - 先遮住一个可选秘密区域
   - 玩家靠近后逐步 reveal
   - 让玩家获得“我发现了不对劲”的探索回报
2. 明确它在 `Level` 模块中的分类与挂点：
   - 它是 **`Directing` / `Triggers`** 语义，不是静态几何主链
3. 明确它与其他墙家族的边界：
   - 不与 `BreakableWall` 语义混合
   - 不接管 `Navigation/Geometry` 的碰撞真相
   - 不提前偷做未来 `ProjectileBarrier`
4. 给后续实现提供一个稳定的 MVP 范围：
   - 先做纯视觉 reveal
   - 必要时允许“会话内永久揭示”
   - 不把持久化与复杂联动一次性做满
5. 为后续 `Level Architect` 的 starter-first 接入提供清晰方向

## 2.2 非目标

以下内容 **不在本轮设计范围内**：

- 不新建统一 `Wall.cs`
- 不把隐藏遮挡墙做成新的实体墙 runtime owner
- 不让 `HiddenAreaMask` 负责真实碰撞阻挡
- 不引入跨房间 / 跨存档的 reveal 持久化主链
- 不在本轮就做 minimap、Lore、相机、音频、世界阶段的复杂联动
- 不把所有“看起来像假墙”的东西都收进 `Wall Authoring` 区块

---

## 3. 玩家体验目标与完成标准

## 3.1 体验目标

这版隐藏遮挡墙更接近：

> **像《Minishoot》里“靠近一片可疑墙面后，遮挡层退去，秘密空间突然变得可读”的那个时刻。**

玩家应该感受到的是：

- 这块区域一开始看起来像正常墙面 / 孢子幕 / 晶簇遮蔽层
- 但靠近时会发生一个明确、可读、带一点惊喜的 reveal
- reveal 后玩家意识到：这里不是主路径墙体，而是一个被视觉藏起来的可选空间

一句话：

> **先给“发现感”，不是先给“破坏感”或“通行规则感”。**

## 3.2 完成标准（满足以下 5 条视为完成）

- [ ] 一个代表性房间中存在一处被遮挡层盖住的秘密偏室、奖励角落或短捷径
- [ ] 玩家进入遮挡触发区后，遮挡层会稳定淡出，露出背后的真实内容
- [ ] 玩家离开后，遮挡层能按配置恢复，或保持会话内永久揭示
- [ ] reveal 行为不会破坏真实碰撞主链：需要挡路的几何仍由 `Navigation/Geometry` 或其他明确 owner 负责
- [ ] 作者在场景里能清楚区分“视觉遮挡层”和“真实内容 owner”，不会把奖励、碰撞、导演全塞进同一个对象

---

## 4. 方案比较与结论

本轮讨论三个方向：

## 4.1 方案 A — 继续把 `HiddenAreaMask` 当成自由摆放组件

### 形态

- 不新增任何语义说明
- 继续允许作者自由决定把它怎么摆、摆在哪、背后藏什么
- 只依赖现有运行时脚本与 validator

### 优点

- 成本最低
- 现有代码已可用
- 不需要额外工具化

### 缺点

- 容易把它误摆进 `Navigation/Geometry` 或墙主链附近
- 容易把“视觉遮挡层”和“真实碰撞 / 奖励 owner”混在一起
- 团队沟通时仍会把它模糊理解成“某种墙对象”，而不是导演件
- 后续接 `Level Architect` 时缺少稳定模板

## 4.2 方案 B — 保留 `HiddenAreaMask` 运行时 owner，但补标准 authoring 模板（推荐）

### 形态

- 运行时继续由 `HiddenAreaMask` 驱动
- 文档明确它属于 `Directing / Triggers`
- 规定标准层级、命名方式与职责拆分
- 后续若要做 starter，starter 只生成标准骨架，不创造第二套 runtime

### 优点

- 最大化复用现有代码与 validator 护栏
- 明确“视觉 reveal”与“几何阻挡 / 奖励内容”分层
- 最符合当前 `Level` Canonical Spec` 的现役口径
- 最容易平滑升级到 `Level Architect` starter-first

### 缺点

- 需要补一份正式设计口径
- 短期内作者仍需手工摆内容，不会立刻获得完整按钮化体验

## 4.3 方案 C — 新建 `OcclusionWall` / `OccluderRevealVolume` 运行时语义壳

### 形态

- 在 `HiddenAreaMask` 外再包一个新的“墙语义组件”
- 可能持有遮挡层、奖励、碰撞、revealed 状态等更多字段

### 优点

- 名字更贴近“墙”语义
- Inspector 可一次性暴露更多 authoring 项

### 缺点

- 高概率与现有 `HiddenAreaMask` 形成双轨 owner
- 很容易一步滑向“又造了一个半通用墙系统”
- 过早把还没稳定的规则硬编码进 runtime API

## 4.4 最终结论

本项目选择：

> **方案 B：保留 `HiddenAreaMask` 作为唯一运行时 owner，并用标准 authoring 模板把“隐藏遮挡墙”收口为秘密 reveal 语法。**

---

## 5. 最终设计结论

## 5.1 顶层定位

隐藏遮挡墙的本质不是实体墙，而是：

- 一个 **秘密 reveal 装置**
- 一个 **视觉遮挡层控制器**
- 一个 **玩家靠近即触发的导演件**

因此它属于：

- **分类：** `Directing`
- **推荐根：** `Triggers`
- **运行时 owner：** `HiddenAreaMask`

它不应被误归为：

- `Navigation/Geometry` 的主碰撞墙
- `Elements` 中的持久化状态件
- `BreakableWall` 一类可受击 / 可销毁的墙

## 5.2 运行时 authority

本轮继续坚持以下 authority：

- **Reveal 行为**：`HiddenAreaMask`
- **真实碰撞**：`Tilemap + Collider` 或其他已明确的空间 / 战斗 owner
- **奖励 / 交互内容**：各自原本所属的 `Elements` / `Decoration` / `Encounters` / `Hazards`

换句话说：

> **遮挡层负责“看不见”，不负责“过不去”或“能不能打”。**

## 5.3 推荐层级结构

建议每个房间里按以下方式组织：

```text
Room_[ID]
├── Navigation
│   └── Geometry
│       ├── OuterWalls
│       └── InnerWalls
├── Elements
│   └── SecretReward_*         (如奖励、可拾取、互动件)
├── Decoration
│   └── SecretDressing_*       (可选：纯视觉内景)
└── Triggers
    └── HiddenAreaMask_[Name]
        ├── Mask_Main
        ├── Mask_Detail
        └── Mask_ForegroundFX   (可选)
```

### 说明

- `HiddenAreaMask_[Name]`
  - 挂 `HiddenAreaMask`
  - 挂 `BoxCollider2D`（Trigger）
- `Mask_Main / Detail / ForegroundFX`
  - 挂 `SpriteRenderer`
  - 作为被 fade 的视觉遮挡层
- 被 reveal 的真实内容**不要**全部塞到 `HiddenAreaMask` 子树下；应继续放在各自语义根下

## 5.4 视觉层、碰撞层、内容层的职责拆分

### 视觉遮挡层

- 由 `HiddenAreaMask` 控制显隐
- 负责“表面上像墙 / 像幕 / 像遮蔽物”
- 不承担真实阻挡 authority

### 真实碰撞层

如果秘密区域前方真的有通行或投射物规则需求：

- **挡移动** → 继续放在 `Navigation/Geometry`
- **可被打碎后开路** → 用 `BreakableWall + DestroyableObject`
- **只挡投射物** → 留给未来 `ProjectileBarrier`

### 内容层

被 reveal 的对象应保留原有 owner：

- 奖励 / 拾取物 → `Elements`
- 纯内景 / 氛围 → `Decoration`
- 世界触发 / 导演件 → `Triggers`

这条规则非常重要，因为它能防止：

- `HiddenAreaMask` 既当遮挡层又当奖励容器
- reveal 触发器与真实内容强耦合
- 后续 validator 和工具链失去边界

## 5.5 参数建议（MVP）

第一版建议采用保守、可读的参数范围：

- `FadeDuration`：`0.25s ~ 0.45s`
- `HiddenAlpha`：`1.0`
- `RevealedAlpha`：`0.0 ~ 0.15`
- `PermanentReveal`：默认关闭；只在会话内秘密发现感明显更重要时开启

推荐默认体验：

- **快速但不突兀**
- **更像“揭开遮蔽”而不是“闪烁切换”**

---

## 6. MVP 与未来增强

## 6.1 MVP

MVP 只做以下内容：

1. 一块视觉遮挡层覆盖秘密区域入口或局部空间
2. 玩家进入 trigger 后淡出 reveal
3. 玩家离开后恢复，或按配置会话内永久 reveal
4. 不新增跨房 / 跨存档持久化
5. 不改变现有几何碰撞 owner

## 6.2 Future Phase

后续增强建议按这个顺序推进：

1. **Authoring Starter**
   - 在 `Level Architect` 中提供标准骨架创建入口
2. **Reveal Persist**
   - 若确实需要“发现一次后永久揭示”，再接 `RoomFlagRegistry + SaveBridge`
3. **地图 / 奖励联动**
   - reveal 后同步 minimap、Lore 点、可拾取提示
4. **氛围强化**
   - 加轻微 ambience、camera、后处理变化
5. **高级遮挡语法**
   - 多层遮挡、阶段 reveal、与 `BreakableWall` 串联

---

## 7. 与 `Level Architect` 的接入边界

## 7.1 不建议的接法

当前**不建议**把隐藏遮挡墙直接并入现有 `Wall Authoring` 区块。原因：

- `Wall Authoring` 当前强调的是 `Navigation/Geometry` 与 `BreakableWall` 这类“墙语义 / 路径语义”
- `HiddenAreaMask` 的本质仍是 `Triggers` 下的 reveal 导演件
- 如果硬塞进同一个入口，会重新模糊“视觉遮挡”和“真实墙 owner”的边界

## 7.2 推荐的接法

后续若要开放 starter，建议走：

> **`Starter Objects` / `Secret & Traversal` / `Triggers` 语义入口**

它应该创建的是：

- 一个挂在 `Triggers` 下的 `HiddenAreaMask` 骨架
- 一个默认 trigger collider
- 一个可继续补美术的遮挡层子节点容器

而不是：

- 自动创建真实墙碰撞
- 自动推断奖励 owner
- 自动生成复杂 secret room 套件

也就是说：

> **starter 只负责把作者带到正确轨道，不替作者做完整内容设计。**

---

## 8. 风险与防御措施

## 8.1 风险：把它混进几何主链

### 表现

- 把 `HiddenAreaMask` 直接放进 `Navigation/Geometry`
- 让遮挡层 sprite 和主碰撞 Tilemap 绑在一个对象上

### 防御

- 文档与 validator 继续坚持：`HiddenAreaMask` 推荐根是 `Triggers`
- 遮挡层与几何层分开 authoring

## 8.2 风险：让它偷接碰撞 / 伤害语义

### 表现

- 想让 reveal 墙顺便挡子弹
- 想让它也能被打碎

### 防御

- 需要挡路 → 交回 `Navigation/Geometry`
- 需要破坏 → 交给 `BreakableWall`
- 需要挡弹 → 留给未来 `ProjectileBarrier`

## 8.3 风险：把 reveal 做成过大范围的全房切换

### 表现

- 一个 `HiddenAreaMask` 覆盖整个房间大面积内容
- 玩家只是在房边擦到 trigger，就把整片空间全揭开

### 防御

- 第一版只覆盖“秘密偏室 / 奖励角 / 小捷径入口”级别
- trigger 体积优先贴近玩家真正进入秘密区域的时刻

## 8.4 风险：过早做持久化

### 表现

- 在还没验证 reveal 手感前，就把 `RoomFlagRegistry`、存档恢复、地图联动全部接上

### 防御

- 第一版先验证发现感与空间可读性
- 真需要永久 reveal 时，再单独起 `PersistentHiddenAreaReveal` 范围

---

## 9. 推荐的下一步实现范围

如果进入实现阶段，我建议按以下顺序推进：

1. **小幅 hardening `HiddenAreaMask` authoring 口径**
   - 补充命名与样板约束
   - 不重写 runtime 主链
2. **为 `Level Architect` 增加 `HiddenAreaMask` starter**
   - 入口落在 `Starter Objects` / `Triggers` / `Secret` 语义，而不是 `Wall Authoring`
3. **补充 EditMode tests 与文档同步**
   - `LevelRuntimeAssistFactoryTests`
   - `LevelValidatorTests`
   - `Level_CanonicalSpec` / `LevelArchitect_SupportedElements_Matrix`
4. **最后做一个代表房切片验证**
   - 只验证秘密 reveal 闭环，不混入复杂世界事件

---

## 10. 最终建议

隐藏遮挡墙的正确落点不是“再做一堵特殊墙”，而是：

> **把它当成一个负责秘密 reveal 的导演件，并明确规定它如何与真实墙、奖励和空间内容协作。**

只要这条边界守住，后续不论是：

- 给它加 starter
- 给它加永久 reveal
- 让它和 `BreakableWall` 组合
- 甚至让它和世界阶段联动

都还能沿着一条清晰主链继续长，而不会重新掉回“所有墙都像同一种东西”的混乱状态。
