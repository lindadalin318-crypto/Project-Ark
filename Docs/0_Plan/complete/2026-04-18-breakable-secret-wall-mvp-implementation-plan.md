# Breakable Secret Wall MVP Implementation Plan

> **Status:** 已完成（功能闭环）— 2026-04-18 16:17
>
> **For historical traceability:** 这份 implementation plan 的功能范围已落地；运行时主链、`BreakableWall` 语义壳、`Level Architect` starter、`LevelValidator` 护栏，以及后续补上的 `Wall` layer authoring 缺口都已收口。原计划中的“轻微信号”现收口为后续可选美术 / 可读性增强，不影响本次完成判定。

**Goal:** 构建一个“秘密裂墙”MVP，让玩家用现有常规攻击打碎伪装墙，打开隐藏奖励通路，并在离房/读档后保持破坏状态。

**Architecture:** 复用现有 `DestroyableObject -> RoomFlagRegistry -> SaveBridge` 作为唯一的受击、房间持久化与存档主链，不新造第二套伤害或状态系统。新增 `BreakableWall` 作为语义外壳，只负责“秘密裂墙”的 authoring 合约、可疑信号和破坏前后表现切换；`Level Architect` 与 `LevelValidator` 只补 starter 和 guardrail，不接管 runtime authority。

**Tech Stack:** Unity 6000.3.7f1、C#、`ProjectArk.Level`、`ServiceLocator`、`LevelEvents`、Editor NUnit tests、`SampleScene` 手工 authoring

> **Completion Note — 2026-04-18 16:17**
> - `DestroyableObject` 的只读销毁状态与 `OnDestroyed` 事件出口已经落地。
> - `BreakableWall` 语义壳、`LevelRuntimeAssistFactory` starter、`LevelValidator` 护栏与相关 EditMode tests 已全部实现。
> - `BreakableWall` starter 现已默认写入 `Wall` layer，修复了此前“环境受伤逻辑正确，但部分武器看不到墙”的 authoring 漏点。
> - 场景内已完成功能验证：墙可被打碎；轻微信号与额外美术提示不纳入本次归档完成条件。

---

## 1. MVP 定义与完成标准

### 1.1 体验目标

这一版不是做“主路能力门”，而是做**探索回报型秘密裂墙**：

- 玩家从轻微信号中怀疑一面墙不对劲
- 玩家用现有武器试探并打碎它
- 墙后出现隐藏奖励或捷径
- 这个发现会永久成立，而不是离房后重置

### 1.2 完成标准（满足以下 5 条视为完成）

- [ ] 一个样板房中存在一面非主线强制的秘密裂墙
- [ ] 玩家用当前标准武器可以稳定打碎这面墙
- [ ] 墙碎后阻挡消失，露出隐藏通路或小偏室
- [ ] 离房再回来后，墙保持已破坏状态
- [ ] 读档后，墙仍保持已破坏状态，且静态几何 authoring 主链不被污染

### 1.3 明确不做

本轮 **不要** 引入以下内容：

- 特定武器类型才能破坏
- 主线强制封路
- 多段耐久 / 元素克制 / 爆炸判定
- 复杂碎块物理与大量一次性 VFX 池化资产
- 地图联动、剧情联动、Phase / Stage 联动

---

## 2. 文件落点与职责

### 2.1 计划新增文件

- `Assets/Scripts/Level/Room/BreakableWall.cs`
  - `DestroyableObject` 的语义外壳
  - 持有“可疑信号 / intact-only 对象 / destroyed-only 对象”三类 authoring 引用
  - 订阅 destroy 事件并切换前后表现
  - 不直接写存档，不自建 HP，不重复实现受击逻辑

- `Docs/0_Plan/complete/2026-04-18-breakable-secret-wall-mvp-implementation-plan.md`
  - 当前 implementation record（已归档）

### 2.2 计划修改文件

- `Assets/Scripts/Level/Room/DestroyableObject.cs`
  - 保持其“泛化可破坏物”定位
  - 仅补最小公开出口（销毁事件 / 只读状态）供 `BreakableWall` 复用
  - 不塞入墙专属语义

- `Assets/Scripts/Level/Editor/LevelArchitect/LevelRuntimeAssistFactory.cs`
  - 新增 `BreakableWall` starter
  - 默认创建到 `Elements/`
  - 自动挂基础 `Collider2D + SpriteRenderer + DestroyableObject + BreakableWall`

- `Assets/Scripts/Level/Editor/LevelArchitect/LevelValidator.cs`
  - 新增 `BreakableWall` 校验规则
  - 确保 `BreakableWall` 挂在 `Elements`
  - 确保同物体上存在 `DestroyableObject`
  - 可选校验：至少存在一个 `Collider2D` 与一个可见体或显式表现引用

- `Assets/Scripts/Level/Editor/LevelArchitect/LevelRuntimeAssistFactoryTests.cs`
  - 覆盖 starter 创建结果

- `Assets/Scripts/Level/Editor/LevelArchitect/LevelValidatorTests.cs`
  - 覆盖 `BreakableWall` 推荐根与缺失依赖校验

- `Assets/Scenes/SampleScene.unity`
  - 放入 1 个秘密裂墙样板房切片
  - 墙体本体放在 `Elements/`
  - 静态墙仍留在 `Navigation/Geometry/OuterWalls` 或 `InnerWalls`

- `Docs/2_TechnicalDesign/Level/Level_CanonicalSpec.md`
  - 追加 `BreakableWall` 作为 `DestroyableObject` 语义化标准件的现役说明

### 2.3 预期不需要修改的文件

- `Assets/Scripts/Level/Room/Room.cs`
  - 当前已经主动收集 `DestroyableObject`
  - 这版不需要为 `BreakableWall` 额外扩收集链

- `Assets/Scripts/Level/Room/RoomFlagRegistry.cs`
  - 现有房间级 flag 持久化链已足够

- `Assets/Scripts/Level/SaveBridge.cs`
  - 现有 `RoomFlagRegistry` 读写桥已足够

---

## 3. 设计决策（先定死，避免开工时摇摆）

### 3.1 为什么不是直接把 `DestroyableObject` 改成“墙组件”

因为 `DestroyableObject` 当前已经是通用的 `Stateful` 元素底座：它可以表示晶体、残骸、可破坏封堵，不应该被秘密墙语义绑死。

本轮正确做法是：

- `DestroyableObject` 继续做泛化“可受击 + 持久化销毁”
- `BreakableWall` 单独表达“这是一面秘密裂墙”的 authoring 与表现契约

### 3.2 为什么不改 `Room`

`Room` 当前已经通过 `Elements` 主动收集 `DestroyableObject`，而 `Level_CanonicalSpec` 明确要求：**只有当新元素需要被 `Room` / `RoomManager` 查询或统一调度时，才扩收集链**。

本轮 `BreakableWall` 是附着在 `DestroyableObject` 上的语义层，不需要额外进入房间主链。

### 3.3 为什么不新增 `BreakableWallSO`

这版不需要数据驱动到 `SO` 级别。原因：

- 它是单房验证切片，不是大批量内容生产阶段
- 当前需要先验证语义与手感，不需要提前为几十种裂墙变体建资产系统
- 空间事实、摆位、遮挡关系本来就应该留在 Scene authoring

后续如果出现 3 种以上重复 authoring 模式，再考虑把视觉模板或条件模板抽到 `SO`

---

## 4. 分任务实施顺序

### Task 1: 给 `DestroyableObject` 补最小事件出口

**Files:**
- Modify: `Assets/Scripts/Level/Room/DestroyableObject.cs`

- [ ] 增加只读公开状态，例如 `IsDestroyed` 或等价只读属性，避免外层组件反射或重复判断
- [ ] 增加销毁完成事件，例如 `OnDestroyed`，在 `ApplyDestroyedState(playEffects: true/false)` 完成状态切换后统一抛出
- [ ] 保持 `RoomFlagRegistry` 写入逻辑不变，不把 `BreakableWall` 的语义塞回 `DestroyableObject`
- [ ] 保持“必须是 `Room` 子物体、必须走 `Elements/`”的现有注释口径
- [ ] 自查不会引入重复触发：已销毁状态再次进入 `Start()` 恢复时只能表现一致，不应广播多次错误语义

**完成标志：** `DestroyableObject` 成为可被外层语义件监听的稳定 runtime primitive。

### Task 2: 新建 `BreakableWall` 语义外壳

**Files:**
- Create: `Assets/Scripts/Level/Room/BreakableWall.cs`
- Modify: `Assets/Scripts/Level/Room/DestroyableObject.cs`

- [ ] `BreakableWall` 使用 `[RequireComponent(typeof(DestroyableObject))]`
- [ ] 暴露 3 类最小 authoring 引用：
  - `suspiciousSignalRenderers`：裂纹、漏光、色差等可疑信号
  - `intactOnlyObjects`：仅在未破坏时存在的遮挡块、遮挡贴花、碰撞补件
  - `destroyedOnlyObjects`：破坏后开启的碎裂残骸、边缘 decal、奖励提示
- [ ] 在 `Awake()` 缓存 `DestroyableObject`
- [ ] 在 `OnEnable()` / `OnDisable()` 订阅与取消订阅 destroy 事件
- [ ] 在初始化时读取当前 destroy 状态：若已经被打碎（离房重进或读档后），直接应用 destroyed 表现，不等待再次受击
- [ ] `BreakableWall` 只负责视觉和 authoring 语义，不负责 HP、伤害计算、flag key 或 save 写入

**完成标志：** 关卡作者可以把 `BreakableWall + DestroyableObject` 当成一个标准房间元素使用，而不是继续手拼“泛化 destroyable + 自定义摆法”。

### Task 3: 给 `Level Architect` 补 starter

**Files:**
- Modify: `Assets/Scripts/Level/Editor/LevelArchitect/LevelRuntimeAssistFactory.cs`
- Modify: `Assets/Scripts/Level/Editor/LevelArchitect/LevelRuntimeAssistFactoryTests.cs`

- [ ] 在 `RoomAssistType` 中新增 `BreakableWall`
- [ ] 在 `GetDisplayName()` 中加入显示名
- [ ] 新增 `CreateBreakableWall(Room room)`
- [ ] starter 创建位置固定在 `Elements/`
- [ ] starter 默认包含：
  - 非 Trigger 的 `BoxCollider2D`
  - `SpriteRenderer`
  - `DestroyableObject`
  - `BreakableWall`
- [ ] starter 默认命名采用 `BreakableWall_{room.RoomID}` 或等价稳定命名，不用临时时间戳
- [ ] starter 的 inspector 初始值只给最小可运行默认值：1 HP、空的 destroyed-only 表现、作者可再手工补裂纹和奖励
- [ ] 为该 starter 增加 EditMode 测试：断言它创建在 `Elements`、含 `DestroyableObject`、含 `BreakableWall`、阻挡碰撞不是 Trigger

**完成标志：** 以后在标准房间里试装秘密裂墙，不需要手工从零拼组件。

### Task 4: 给 `LevelValidator` 补护栏

**Files:**
- Modify: `Assets/Scripts/Level/Editor/LevelArchitect/LevelValidator.cs`
- Modify: `Assets/Scripts/Level/Editor/LevelArchitect/LevelValidatorTests.cs`

- [ ] 新增 `ValidateBreakableWalls()`，检查场景中所有 `BreakableWall`
- [ ] 至少覆盖以下规则：
  - 必须挂在 `Room` 子树内
  - 推荐根必须是 `Elements`
  - 同物体必须存在 `DestroyableObject`
  - 必须存在至少一个 `Collider2D`
- [ ] 若 `BreakableWall` 缺少 `DestroyableObject`，报 `Error`
- [ ] 若 `BreakableWall` 放在 `Triggers` / `Navigation` / `Hazards`，报 `Warning`
- [ ] 在 `ValidatePreferredAuthoringRoots()` 中把 `BreakableWall` 纳入 `Elements` 规则
- [ ] 增加 EditMode 测试：
  - 放错根时报 `Warning`
  - 缺少 `DestroyableObject` 报 `Error`
  - 合法摆放时不误报

**完成标志：** `BreakableWall` 成为 Level 模块的官方标准件，而不是“代码里能跑但 authoring 没护栏”的灰色元素。

### Task 5: 在 `SampleScene` 做单房间验证切片

**Files:**
- Modify: `Assets/Scenes/SampleScene.unity`

- [ ] 选择一个已有 `Transit` 或 `Reward` 倾向的代表房间；若现有房间都不合适，则在 `SampleScene` 新做一个很小的偏室验证块
- [ ] 保持静态墙仍 author 在 `Navigation/Geometry/OuterWalls` 或 `InnerWalls`
- [ ] 在 `Elements/` 下放一面 `BreakableWall`
- [ ] 墙后放一个明确奖励，优先复用现成 `HealthPickup` 或 `HeatPickup`
- [ ] 奖励房尽量小而清晰，避免本轮同时验证战斗与复杂世界触发
- [ ] 墙体信号只做“可疑但不过度直白”：轻裂纹、轻色差、边缘不自然，而不是大型发光问号

**完成标志：** 玩家第一次看到这个切片时，会产生“这面墙好像能打”的怀疑，并在打碎后得到明确回报。

### Task 6: 文档与现役口径收口

**Files:**
- Modify: `Docs/2_TechnicalDesign/Level/Level_CanonicalSpec.md`
- Modify: `Docs/5_ImplementationLog/ImplementationLog.md`

- [ ] 在 `Level_CanonicalSpec` 的 `Stateful` 元素说明中补一句：`BreakableWall` 是基于 `DestroyableObject` 的语义化标准件，默认挂在 `Elements`
- [ ] 如 starter / validator 已实装，补到 editor 工具覆盖说明
- [ ] 当前轮所有实际文件改动完成后，追加 Implementation Log

**完成标志：** 后续再看 `Level` 文档时，不会误以为“秘密裂墙还是非官方特例”。

---

## 5. 手工验收清单（Play Mode）

### 5.1 核心闭环

- [ ] 初次进入房间时，裂墙看起来像静态墙的一部分，但有轻微信号
- [ ] 普通攻击命中后，墙会按 `DestroyableObject` 逻辑掉血并销毁
- [ ] 销毁后碰撞消失，玩家能进入隐藏空间
- [ ] 墙后奖励可以被正常拾取

### 5.2 持久化闭环

- [ ] 离开房间再回来，墙保持已破坏状态
- [ ] 保存游戏后重新加载，墙保持已破坏状态
- [ ] 已破坏状态不会污染其他房间的同类墙

### 5.3 Authoring 闭环

- [ ] `BreakableWall` 不会被摆进 `Navigation/Geometry`
- [ ] `LevelValidator` 能指出放错根或缺依赖的问题
- [ ] `LevelRuntimeAssistFactory` 可以一键起一个 starter，不需要手工拼 4 个组件

---

## 6. 失败边界与回退策略

### 6.1 如果 `DestroyableObject` 事件出口导致泛化类变脏

回退方案：

- 不在 `DestroyableObject` 中加复杂事件数据结构
- 只保留最简单的 `OnDestroyed` 通知
- 所有墙专属逻辑继续留在 `BreakableWall`

### 6.2 如果 starter 增加过快导致 `LevelRuntimeAssistFactory` 继续膨胀

回退方案：

- 这轮只补 `BreakableWall` 一个 starter
- 不顺手把 `HiddenAreaMask`、`ProjectileBarrier`、`OneWayPassage` 一起塞进去

### 6.3 如果场景里暂时找不到合适房间

回退方案：

- 允许在 `SampleScene` 做一个最小独立验证房块
- 先验证“秘密裂墙语法成立”，再考虑整进正式路网

---

## 7. 建议提交节奏

- [ ] Commit 1: `feat(level): expose destroyable state hooks for semantic wrappers`
- [ ] Commit 2: `feat(level): add breakable wall runtime component`
- [ ] Commit 3: `feat(level-editor): add breakable wall starter and validator`
- [ ] Commit 4: `feat(level): author secret breakable wall slice in sample scene`
- [ ] Commit 5: `docs(level): document breakable wall authoring contract`

---

## 8. 执行建议

推荐按下面顺序开工：

1. **先做 Task 1 + Task 2**，把 runtime 语义壳立住
2. **再做 Task 3 + Task 4**，补 editor starter 与 validator
3. **最后做 Task 5**，把 `SampleScene` 的单房切片搭出来
4. **收尾做 Task 6**，把文档和现役口径补齐

如果中途发现 `BreakableWall` 需要 `Room` 主链额外感知，先停下来复核 `Level_CanonicalSpec 6.4`，不要直接把它塞进 `Room.CollectSceneReferences()`。
