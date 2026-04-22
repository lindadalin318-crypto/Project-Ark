# SpaceLife Hub 对话系统技术设计

**日期：** 2026-04-20  
**状态：** 已达成设计共识，待进入实现  
**适用对象：** `SpaceLife`、Hub 船内对话、船员 NPC、终端 / AI、关系成长、服务入口、未来 `CSV -> SO` 铺量

---

## 1. 背景

当前项目里已经存在一套 `SpaceLife` 对话原型：`DialogueUI`、`NPCController`、`NPCInteractionUI`、`DialogueLine`、`RelationshipManager`、`GiftInventory` 等基础件已经能支撑简单的 NPC 互动。

但这套原型仍然是 **demo 级耦合实现**，主要问题包括：

- `DialogueData` 仍用 **数组 index** 串联节点，节点顺序变化会直接破坏链路
- `DialogueUI` 直接在选项点击里修改关系值，并直接按 index 取下一句，**UI 与业务耦合**
- `NPCController` 同时负责交互入口、起始台词选择、关系初始化、礼物偏好读取，**职责过重**
- 旧入口主要按 **关系值** 选择开场，不读取 **世界进度 / 一次性旗标**
- 旧 UI 依赖 `SetActive(true/false)` 控显隐，不符合项目当前对 uGUI 面板的治理方向

因此，本轮设计的真正目标不是“再做一个聊天框”，而是：

> **把飞船内部 Hub 对话收口为一套独立的内容状态机域：既能承载氛围叙事，也能作为功能入口，并支持关系成长与后续内容铺量。**

这套设计基于以下已确认方向：

- **场景**：`SpaceLife` 飞船内部 Hub，对话发生时飞船处于停靠 / 安全态
- **交互形态**：先进入对话，再从对话分支进入服务
- **体验目标**：`氛围叙事 + 功能交互 + 关系成长`
- **刷新逻辑**：`世界进度 + 关系值` 双轴驱动
- **authoring 路线**：先做 `SO MVP`，但从第一天保证未来可平滑升级到 `CSV -> SO`

---

## 2. 目标与非目标

## 2.1 目标

本轮设计希望达成以下结果：

1. 建立一套 **Hub-only** 的正式对话系统基线：
   - 面向 `SpaceLife` 飞船内部
   - 适用于船员 NPC 与终端 / AI 两类交互对象
2. 明确 runtime authority 与模块边界：
   - `DialogueDomain` 负责内容流与规则
   - `SpaceLife` 负责发起交互与承载服务出口
   - UI 只负责显示，不负责业务判断
3. 支持 MVP 所需的核心能力：
   - 中等分支
   - 世界进度 / 关系值 / 一次性旗标条件
   - 对话中进入 `Upgrade / Intel / Gift / RelationshipEvent`
4. 明确一套未来可铺量的数据模型：
   - runtime 只认稳定 ID 与标准数据结构
   - 不依赖数组 index 或场景硬引用
   - 为 `CSV -> SO` 导入保留无痛升级缝
5. 为后续实现提供清晰 MVP 切片与完成标准

## 2.2 非目标

以下内容 **不在本轮设计范围内**：

- 不先做通用节点编辑器
- 不先做 `CSV` 导入器
- 不先覆盖关卡内驻留 NPC / 远程通讯对话
- 不做复杂表达式语言或通用叙事 DSL
- 不让对话节点直接操纵大量世界物件或任意系统逻辑
- 不把 `DialogueUI` / `NPCController` 原型直接扩写成长期真相源
- 不在本轮就做大规模 NPC 铺量

---

## 3. 玩家体验目标与完成标准

## 3.1 体验目标

这版系统要服务的，不是高速战斗中的即时播报，而是：

> **玩家每次回到飞船内部后，与船员和终端进行一次“有情绪、有信息、有功能回报”的停靠交流。**

玩家应该感受到的是：

- 船内对话是一次**完整停顿**，不是飞行中的半悬浮提示
- 船员会随着相处与主线推进，逐渐说出不同的话、开放不同的服务
- 终端 / AI 也使用同一套底层逻辑，但在表现上更偏“系统接口”而非“人际交流” 
- 对话不是售货机菜单；玩家先进入交流，再自然分支到服务

一句话：

> **先有“回船见人”的关系感，再有“顺便做事”的功能流。**

## 3.2 完成标准（满足以下 5 条视为 MVP 设计命中）

- [ ] 玩家能在 `SpaceLife` 中与 **1 个船员 NPC** 和 **1 个终端 / AI** 发起 Hub 对话
- [ ] 对话支持“先聊 → 追问 → 决定是否进入服务”的 **中等分支**
- [ ] 节点与选项可根据 **世界进度 / 关系值 / 一次性 flag** 变化
- [ ] 服务入口通过统一 `ServiceExit` 跳出，而不是硬写在具体 NPC 或 UI 中
- [ ] 内容虽然先由 `SO` authoring，但 runtime 不依赖 index 跳转，可平滑升级到 `CSV -> SO`

---

## 4. 方案结论

本轮对比过三个方向：

- **方案 A**：直接在旧 `DialogueUI` / `NPCController` 原型上扩写
- **方案 B**：新增独立 `DialogueDomain`，`SpaceLife` 只负责交互壳接入
- **方案 C**：一开始就做通用剧情图框架与重型工具链

最终选择：

> **方案 B：用独立 `DialogueDomain` 承担内容规则与节点跳转，`SpaceLife` 只承载 Hub 交互表现与服务跳出。**

### 选择 B 的原因

- 最符合 **先跑 `SO MVP`、未来无痛升级 `CSV`** 的分阶段路线
- 能同时覆盖 **船员 NPC** 与 **终端 / AI** 两种交互口径
- 能把 **对话内容** 与 **Hub 具体表现** 解耦
- 不会把旧原型里 `UI + NPC + 关系 + 选项逻辑` 的强耦合直接带进长期主链

---

## 5. 顶层边界与 authority

## 5.1 `DialogueDomain`：对话规则层

职责：

- 持有对话图数据
- 根据 `OwnerId + DialogueContext` 解析起始节点
- 评估节点 / 选项条件
- 执行 effect
- 处理跳转、结束、服务出口

不负责：

- UI 长什么样
- 是哪个场景对象发起的
- 输入、镜头、动画、面板显隐细节
- 具体服务菜单如何实现

## 5.2 `SpaceLife` 接入层：Hub 交互壳

职责：

- 在飞船内部发起与 NPC / 终端的交互
- 组装 `DialogueContext`
- 请求 `DialogueDomain.StartDialogue(...)`
- 接收对话输出并驱动 UI
- 对接服务出口，真正打开升级、情报、赠礼等界面

不负责：

- 判断当前应该说哪句台词
- 判断选项是否因关系值不足而锁住
- 自己决定服务是否已解锁

## 5.3 `DialogueUIPresenter`：表现层

职责：

- 显示发言者、文本、头像、选项
- 处理打字机、继续、关闭、样式切换
- 接收玩家选择并回传 `DialogueRunner`

不负责：

- 改关系值
- 查存档
- 决定节点跳转
- 直接打开服务系统

> **关键约束：** 新 UI 面板必须遵守项目现有 uGUI 规则，**用 `CanvasGroup` 控显隐，GameObject 始终保持 active**，禁止再用 `SetActive(false)` 作为正式显隐主链。

## 5.4 `ServiceExit`：服务出口层

服务不是对话系统内部实现，而是对话系统的**统一出口**。

对话只能决定：

- 是否允许进入某个服务
- 此刻应该跳出到哪一类服务
- 是否需要附带一个轻量 payload

真正的服务逻辑由上层系统负责，例如：

- `Upgrade`
- `Intel`
- `Gift`
- `RelationshipEvent`

## 5.5 外部状态真相

对话系统 **读取** 以下真相，不拥有它们：

- **世界进度**：现有 `WorldStage` / 进度系统 / 统一旗标
- **一次性状态**：`ProgressSaveData.Flags`
- **关系值**：独立关系系统
- **库存 / 礼物**：现有 `GiftInventory` 或后续统一物资系统

一句话总结：

> **对话系统负责“解释并消费状态”，不负责偷偷维护第二套世界真相。**

---

## 6. 核心数据模型

## 6.1 设计原则

这套数据模型必须同时满足两件事：

1. **MVP 阶段**：能在 `ScriptableObject` 中直接 authoring
2. **铺量阶段**：能被 `CSV` 稳定导入，不推翻 runtime

因此 runtime 真相必须建立在：

- **稳定字符串 ID**
- **描述式条件 / 效果**
- **与 Inspector 数组顺序解耦**

而不是建立在：

- 数组下标跳转
- 直接对象引用串图
- UI 特判逻辑

## 6.2 `DialogueGraphSO`

代表一份完整对话资源。

建议字段：

- `GraphId`
- `OwnerId`
- `OwnerType`
- `EntryRules[]`
- `Nodes[]`
- `PresentationProfileId`（可选）

语义：

- `GraphId` 是内容资产自身 ID
- `OwnerId` 代表这份图属于谁，例如某个船员或某个终端
- `EntryRules` 用于选择当前从哪个节点开场
- `Nodes` 是节点池，不依赖数组顺序做跳转

## 6.3 `DialogueNodeData`

每个节点是一条“当前说什么、接下来能去哪”的数据单元。

建议字段：

- `NodeId`
- `SpeakerId`
- `SpeakerDisplayName` 或 `SpeakerKey`
- `RawText` 或 `TextKey`
- `NodeType`
- `Choices[]`
- `Conditions[]`
- `OnEnterEffects[]`

### `NodeType`（MVP）

第一版先支持：

- `Line`
- `ServiceGate`
- `RelationshipEvent`
- `End`

## 6.4 `DialogueChoiceData`

每个选项只描述三件事：

- 显示什么
- 是否可见 / 可选
- 选中后去哪里 / 做什么

建议字段：

- `ChoiceId`
- `RawText` 或 `TextKey`
- `Conditions[]`
- `NextNodeId`
- `Effects[]`
- `ExitType`
- `ExitPayload`

### 关键约束

- `NextNodeId` 使用稳定 ID，不使用数组 index
- 进入服务时优先通过 `ExitType` 表达，不在 choice 上直接写 UI 打开逻辑
- `Effects` 与 `ExitType` 可并存，例如：
  - 先加关系值
  - 再跳到 `Gift`

## 6.5 `DialogueConditionData`

MVP 不做表达式语言，只做有限、可审计的硬条件。

建议字段：

- `ConditionType`
- `TargetKey`
- `CompareOp`
- `Value`

MVP 先支持：

- `WorldStage`
- `RelationshipValue`
- `FlagPresent`
- `FlagAbsent`
- `ServiceUnlocked`
- `OwnerType`

## 6.6 `DialogueEffectData`

建议字段：

- `EffectType`
- `TargetKey`
- `IntValue`
- `StringValue`

MVP 先支持：

- `SetFlag`
- `ClearFlag`
- `AddRelationship`
- `EndDialogue`
- `EmitServiceExit`

## 6.7 `DialogueContext`

所有条件评估都只读取统一 `DialogueContext`，不要让节点和选项自行到处查管理器。

建议包含：

- `OwnerId`
- `OwnerType`
- `WorldStage`
- `Flags`
- `RelationshipValue`
- `AvailableServices`
- `InventorySnapshot`（可选）
- `InteractionChannel`（NPC / Terminal / AI）

---

## 7. 运行时主流程

## 7.1 发起对话

`SpaceLife` 交互层在玩家触发 NPC / 终端时：

1. 识别当前 `OwnerId`
2. 组装 `DialogueContext`
3. 请求 `DialogueDomain.StartDialogue(ownerId, context)`

## 7.2 解析入口

`DialogueRunner`：

1. 根据 `OwnerId` 找到对应 `DialogueGraphSO`
2. 遍历 `EntryRules`
3. 选出当前条件下最合适的入口节点
4. 生成可供 UI 消费的当前节点视图模型

这样可以自然支持：

- 主线推进后更换开场白
- 关系提高后出现新的私人话题
- 一次性事件看过后不再重复旧入口

## 7.3 展示当前节点

`DialogueUIPresenter` 消费 view model：

- 显示头像 / 名称 / 文本
- 生成选项列表
- 处理继续 / 关闭 / 样式切换
- 把玩家选择回传给 `DialogueRunner.Choose(choiceId)`

## 7.4 处理选择

`DialogueRunner` 在玩家点击选项后：

1. 再次确认 choice 条件仍成立
2. 执行 `Effects`
3. 进入以下三类结果之一：
   - 跳到下一个节点
   - 结束对话
   - 退出对话并返回 `ServiceExit`

## 7.5 跳出服务

对话系统返回统一出口结果，例如：

- `None`
- `EndDialogue`
- `OpenUpgrade`
- `OpenIntel`
- `OpenGift`
- `TriggerRelationshipEvent`

由 `SpaceLife` 上层协调器真正打开对应功能界面或事件流程。

> **关键原则：** 对话系统是“内容状态机”，不是“功能系统总控台”。

---

## 8. 存档、关系与状态写入

## 8.1 一次性状态

MVP 直接复用现有 `ProgressSaveData.Flags`。

适合写入 flags 的内容包括：

- 某段对话是否已看过
- 某个一次性关系事件是否已触发
- 某个教学 / 引导是否已展示
- 某个服务入口是否已永久解锁

推荐 key 风格：

- `dialogue.engineer.first_talk_seen`
- `dialogue.ship_ai.upgrade_intro_seen`
- `relationship.medic.gift_event_01_seen`

## 8.2 关系值

关系值保持为**独立数值真相**，不要把它拆成一堆 bool flag。

原因：

- 它天然是连续成长
- 以后要支持阈值解锁、赠礼增益、事件阶段时，数值比 bool 更稳
- 能更自然地兼容未来更丰富的角色成长设计

## 8.3 状态写入边界

MVP 里对话节点 / 选项允许写入的内容应保持克制：

- 设置 / 清除一个旗标
- 增减一个关系值
- 触发一个服务出口
- 结束对话

不要让对话直接：

- 打开 / 关闭大量世界对象
- 直接推动复杂关卡逻辑
- 直接接管别的系统状态机

若未来确实需要更大的联动，应走：

- `SetFlag`
- 或统一事件出口
- 再由其他系统监听处理

---

## 9. UI 与交互表现建议

## 9.1 模态级别

由于本系统定位于 `SpaceLife` 飞船内部 Hub，对话发生时飞船已经处于停靠 / 安全态，因此 MVP 默认采用：

> **完整模态对话。**

也就是：

- 锁住角色移动 / 主要交互输入
- 聚焦当前说话对象与对话面板
- 结束对话后再还原普通 Hub 控制

## 9.2 NPC 与终端的表现差异

底层逻辑共用同一 `DialogueDomain`，但表现层允许差异化：

- **船员 NPC**：强调头像、名字、关系感、礼物与私人分支
- **终端 / AI**：强调系统风格、简洁布局、功能入口与情报反馈

这类差异应通过 `PresentationProfileId` 或类似 UI profile 解决，**不要复制一套新的 runtime**。

## 9.3 旧 UI 的处置原则

旧 `DialogueUI`、`NPCInteractionUI` 可以在实现期作为：

- 临时参考
- 场景布线样例
- 现有资产容器

但不应继续作为：

- 新系统的业务核心
- 长期 authority
- 新数据结构的宿主

---

## 10. MVP 切片范围

## 10.1 推荐最小可玩切片

第一版严格只做：

- `SpaceLife` 飞船内部 Hub
- **1 个船员 NPC**
- **1 个终端 / AI**
- 中等分支
- 双轴条件：世界进度 + 关系值
- 一次性旗标
- 三类服务出口：
  - `Upgrade`
  - `Intel`
  - `Gift / RelationshipEvent`

## 10.2 推荐 smoke test

至少验证以下四条链：

### A. 船员对话链

- 初始关系时进入普通开场
- 关系提高后出现额外分支
- 某条一次性台词不会重复触发

### B. 终端 / AI 对话链

- 走同一 runner
- 能根据世界进度切换开场和选项
- UI 风格可与 NPC 不同

### C. 服务出口链

- 从对话中进入 `Upgrade`
- 从对话中进入 `Intel`
- 从对话中进入 `Gift / RelationshipEvent`
- 确认通过统一出口跳出，而非 NPC 特判

### D. 存档恢复链

- 已读 flag 正确保留
- 关系值跨读档保留
- 服务解锁状态恢复后仍能正确影响入口 / 选项

---

## 11. `SO -> CSV` 升级路线

## 11.1 Phase 1：SO MVP

- 直接在 `DialogueGraphSO` 中 authoring 内容
- 建立稳定 ID、条件描述、效果描述、服务出口协议
- 跑通 1 个船员 + 1 个终端的完整切片

## 11.2 Phase 2：CSV 铺量接入

在 runtime 不改主链的前提下新增：

- `CSV/Sheet -> DTO`
- `DTO -> DialogueGraphSO`
- 导入校验：
  - `GraphId` 唯一
  - `NodeId` 唯一
  - `NextNodeId` 可解析
  - 条件 / 效果枚举合法

## 11.3 Phase 3：更强内容生产工具

当内容量真正扩大后，再考虑：

- 更好的 Inspector authoring 工具
- 节点编辑辅助
- 本地化流程
- 批量校验与诊断报表

> **关键纪律：** 先让 runtime 结构正确，再让内容工具舒服；不要倒过来。

---

## 12. 实现守则

本设计进入实现时，应额外遵守以下约束：

1. **SO authored data 禁止运行时污染**
   - 运行时状态必须进入 context / session / save，而不是改写 `DialogueGraphSO`
2. **节点跳转必须使用稳定 ID**
   - 禁止重新回到 index-based 跳转
3. **UI 不得成为业务 owner**
   - UI 只消费结果，不计算结果
4. **关系值与 flags 分层**
   - 数值成长与一次性状态不得混用
5. **服务出口统一化**
   - 新增服务时扩展出口协议，不在具体 NPC 脚本里直接开 UI
6. **保持 Hub-first**
   - 在船内 Hub 切片未稳定前，不扩到关卡对话与远程通讯

---

## 13. 最终结论

本项目对 `SpaceLife` Hub 对话系统的正式选择是：

> **先把对话做成独立 `DialogueDomain`，由 `SpaceLife` 承载 Hub 内交互与服务跳出；MVP 先用 `SO` 跑通 1 个船员 NPC + 1 个终端 / AI 的双轴条件对话，并从第一天保证未来可无痛升级到 `CSV -> SO` 铺量。**

这份文档的核心价值不在于“先列很多类型”，而在于先把以下四件事钉死：

- **Hub-only，而不是全场景铺开**
- **先对话，再进服务**
- **世界进度 + 关系值双轴驱动**
- **SO MVP，但 runtime 结构天然兼容 CSV 铺量**
