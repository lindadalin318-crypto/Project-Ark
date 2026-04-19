# 程序化表现工作流规范 (Procedural Presentation Workflow Spec)

> **文档目的**：为《静默方舟》中“程序化美术 / 程序化特效 / 临时占位可视化”提供统一工作法，确保它们既能快速验证手感，又能在后续被正式美术**低痛替换**。
>
> **文档定位**：这是一份 **workflow / rules** 文档，回答“什么时候可以先用程序化表现、怎么保证可替换、开发时必须回答哪些问题、验收时怎么证明替换缝真的存在”。
>
> **一句话原则**：**先用程序化表现把体验做出来，但从第一天起就把“以后怎么换掉它”设计清楚。**

---

## 1. 文档定位与 authority

### 1.1 这份文档负责什么

这份文档只负责 5 件事：

1. 定义什么场景下应优先使用程序化表现，而不是先等正式美术
2. 定义“程序化表现可替换”到底是什么意思，而不是停留在口头上
3. 规定新功能在采用程序化表现时必须回答的检查问题
4. 提供最小实现模板：哪些层该解耦，替换缝应该放在哪
5. 给出验收清单，确保 preview 不会反向绑死 gameplay

### 1.2 这份文档不负责什么

- 单个系统的现役 runtime 真相源
- 某个具体 VFX、Shader、Prefab 的最终 owner 定义
- 正式美术资产映射表
- 一次性调试历史或实现日志

### 1.3 Authority 表

| 主题 | 真相源 | 说明 |
|------|--------|------|
| 功能目标 / 玩家体验 | 对应 Feature Spec / 需求说明 | 先定义玩家应该看到和感受到什么 |
| gameplay 输入参数 | 该功能自己的 `SO` / runtime data contract | 例如半径、持续时间、厚度、颜色主题、淡出曲线 |
| 程序化 preview 的工作法 | 本文档 | 规定什么时候可用、怎么做得可替换 |
| 模块级硬规则 | `Implement_rules.md` | 收口长期 guardrails 与踩坑治理 |
| 最终采用哪种视觉实现 | feature owner + 现役模块文档 | 可能是 procedural preview，也可能换成正式资产 |

### 1.4 一句话边界

**本文档负责“怎么先做程序化表现且不把后路焊死”，模块 `CanonicalSpec` 负责“当前现役链路谁说了算”。**

---

## 2. 适用场景

以下场景应优先参考本规范：

- 用程序生成的 `Texture2D` / `Sprite` / `Mesh` / `Trail` / `LineRenderer` 做临时可视化
- 需要先快速验证范围感、节奏感、可读性、命中反馈，再决定正式美术形态
- 某个新功能已经有 gameplay 逻辑，但正式美术未到位，需要可玩的 placeholder
- 需要用独立 sample、preview rig、调试场景快速验证表现输入

以下场景**不应**直接套本规范当万能兜底：

- 已有稳定正式资产与明确 owner，只是局部调参数
- 需要长期 shipping 的复杂 VFX 管线，但还没有定义输入契约
- 把“程序化先上”当作不做模块边界设计的借口

---

## 3. 核心原则

### 3.1 先验证体验，再追求精致资产

- 程序化表现的第一职责是：**让功能先可见、可玩、可调**。
- 它不是正式美术的劣化版，而是验证玩法读感和节奏的快速工具。

### 3.2 gameplay 依赖“意图参数”，不依赖“表现实现”

- gameplay 层应依赖的是：半径、持续时间、颜色语义、节奏曲线、层级语义。
- gameplay 层**不应**直接依赖某张程序化纹理、某个 child 名、某个 shader property 名或某段 procedural builder 内部步骤。

### 3.3 preview 和 final art 应共享同一输入契约

- 若 preview 吃的是一套参数，final art 就应尽可能也吃同一套参数。
- 正确替换应是“换 renderer / view provider”，而不是“重写调用方”。

### 3.4 替换缝必须显式存在

- 每个程序化表现都必须回答：**以后正式美术接手时，究竟替换哪一层？**
- 推荐替换点：
  - `View` 组件实现
  - visual provider / renderer 实现
  - prefab 引用
  - material / profile 绑定
- 不推荐替换点：
  - gameplay 主逻辑
  - 伤害 / 触发 / 冷却 / 目标选择逻辑

### 3.5 fallback 策略必须白纸黑字写清

- 开发阶段允许 procedural preview 作为 fallback，保证“先能看见、先能玩”。
- 但必须明确：正式链路里它是长期允许、阶段性允许，还是最终必须移除。
- 禁止出现“正式资产没接上，于是系统悄悄退回 preview，但没人知道”的 silent fallback。

### 3.6 程序化表现必须可诊断，而不是“看不见就靠猜”

- 动态生成的 `Texture2D` / `Sprite` / `Mesh` 需要最小诊断手段。
- 至少要能回答：对象在不在、renderer 在不在、材质在不在、生成结果是否真的有可见像素 / 顶点 / 长度。

---

## 4. 新功能采用程序化表现时，必须先回答的 6 个问题

每次新功能准备使用程序化表现前，必须先回答：

1. **这个程序化表现是临时占位，还是长期可 shipping 路径？**
2. **gameplay 层依赖的核心输入参数是什么？**
3. **以后正式美术替换时，替换缝放在哪一层？**
4. **当前程序化实现是否只是 `View` / renderer，而不是反向绑死 gameplay？**
5. **缺正式资产时，系统允许 fallback，还是必须显式报错？**
6. **验收时如何证明“可替换”是真的，而不是口头上的？**

若这 6 个问题答不清，不应直接开做程序化表现。

---

## 5. 推荐最小结构（MVP 模板）

推荐优先采用下面这类结构：

```text
Feature Logic
    ↓
Visual Intent / Runtime Params / SO
    ↓
View Contract (接口 / 数据契约 / renderer input)
    ├─ Procedural Preview Renderer
    └─ Final Art Renderer
```

### 5.1 各层职责

- **Feature Logic**
  - 负责时机、伤害、碰撞、状态、触发
  - 不负责程序化绘制细节

- **Visual Intent / Runtime Params / SO**
  - 负责承载表现输入
  - 例如：半径、厚度、扩散速度、颜色、曲线、层级语义

- **View Contract**
  - 负责定义 preview 和 final art 都要遵守的输入接口
  - 是替换缝的核心

- **Procedural Preview Renderer**
  - 负责快速生成可见表现，服务玩法验证
  - 可以丑，但必须稳定、可读、可诊断

- **Final Art Renderer**
  - 负责正式表现
  - 理想状态下只替换实现，不改调用方

### 5.2 最小实现约束

- 不要让 gameplay 代码直接 `new Texture2D()`、直接操作 child renderer、直接拼 shader 参数
- 不要把 preview sample 的节点名、材质名、纹理名当成 gameplay 依赖
- 若必须有 sample rig，sample 应明确标注为 preview / debug-only，不让它悄悄接管正式链路

---

## 6. 什么时候适合先上程序化表现

### 6.1 适合

- 当前核心风险是“玩家看不看得懂 / 打起来顺不顺”，不是“美术是否最终定稿”
- 正式美术还未到位，但玩法必须先验证
- 需要快速试多个半径、厚度、节奏、颜色方案
- 需要一个独立 sample / preview rig 做开发期校准

### 6.2 不适合

- 需求还没定义输入契约，就急着写 procedural 实现
- 已经明确是正式 shipping 路径，但仍想靠 preview 结构硬扛到底
- 程序化实现必须侵入 gameplay 才能跑起来
- 团队无法接受后续替换时额外再做一次收口

---

## 7. 验收标准：怎样才算“可替换”

每个采用程序化表现的新功能，至少满足以下条件才算通过：

1. **替换正式表现时，不需要修改 gameplay 主逻辑**
2. **preview 与 final art 共享同一组核心输入参数**
3. **程序化表现缺失时，系统行为是明确的：fallback 或报错，不允许 silent no-op**
4. **程序化表现的可见性可验证**，不会出现“对象存在但贴图全透明 / mesh 空 / trail 长度为 0”还没人知道
5. **关键依赖不靠脆弱硬编码驱动 gameplay**（节点名、材质名、shader 属性名、sample object 名）
6. **preview rig / sample 的职责清楚**：它是验证工具，不是默认长期 owner

---

## 8. 开发期注入模板（可直接复制到 feature kickoff）

### 8.1 标准短模板（推荐默认使用）

当一个新功能准备采用程序化表现时，优先直接粘贴第 `4.1` 节的 **“程序化表现立项检查卡”**。

### 8.2 详细模板（需要补实现语义时使用）

当需求已经进入实现设计阶段，可在 feature kickoff 中追加下面这段：

```markdown
### 程序化表现检查
- 这次程序化表现是：临时占位 / 长期 shipping 路径
- gameplay 依赖的输入参数：
- 未来正式美术替换缝：
- 当前 procedural 实现所在层：
- 缺正式资产时的策略：fallback / 报错 / 阶段性允许
- 验收时如何证明它可替换：
```

要求：

- 这段检查不能留空
- 若答案是“先随便做，后面再说”，视为尚未完成设计
- 若短模板已经足以收口，不强制重复填写详细模板

---

## 9. 当前落地建议（Draft）


本规范当前先作为 **project-level draft** 使用：

- 第一阶段：作为新功能讨论与 feature kickoff 的必读补充
- 第二阶段：把核心条目收口进 `Implement_rules.md`
- 第三阶段：视实际使用频率，决定是否增加 validator / audit / code template

**当前推荐策略**：

- 先用这份文档统一提问方式和验收方式
- 不急着一上来就做大而全框架
- 但任何新程序化表现都必须先把替换缝和 fallback 策略说清楚

---

## 10. 与其他文档的关系

| 文档 | 负责内容 |
|------|----------|
| `ProceduralPresentation_WorkflowSpec.md` | 什么时候用程序化表现、怎么做得可替换 |
| `Implement_rules.md` | 长期硬规则、踩坑治理、guardrails |
| 各模块 `CanonicalSpec` | 当前现役运行时链路、模块 authority |
| `ImplementationLog.md` | 每次具体改了什么 |

**关系原则**：

- 本文档负责“怎么做这类事”
- `Implement_rules.md` 负责“哪些坑以后不能再踩”
- 模块 `CanonicalSpec` 负责“当前系统真相源是什么”
