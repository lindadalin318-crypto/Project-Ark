## BoostTrail Shader 实现进度梳理

### 当前结论

`BoostTrail` 这条 Shader 线已经从“调研/学习阶段”进入“项目内可落地 MVP 阶段”。
目前不是从 0 开始做 Shader，而是在把第一套可复用的 `BoostTrail` VFX Shader 模板做成真正可持续迭代的标准件。

一句话概括当前状态：

- **结构已经有了**
- **MVP 已经跑起来了**
- **运行时驱动做了一半**
- **风格化收口还没完成**

---

### 一、已完成：基础路线与首个落地点

- **Shader 学习路线已沉淀**
  - 已新增 `Unity Shader / Material` 学习总结文档。
  - 已明确资料来源、推荐学习顺序、阅读 Shader 代码的方法，以及面向 Project Ark 的落地方向。

- **首个验证目标已聚焦到 `BoostTrail`**
  - 当前没有同时分散推进多个效果，而是优先拿推进尾迹做垂直切片。
  - 这是符合项目当前节奏的正确策略：先做出一个可见、可调、可复用的样板。

- **`TrailMainEffect.shader` 已完成双模式改造**
  - 新增了基于程序噪声的扰动路径。
  - 同时保留原有 `slot` 贴图驱动的 legacy 路径。
  - 这意味着当前方案是演进式改造，而不是推翻旧系统重做。

- **新扰动 Shader 主体逻辑已存在且可运行**
  - 已包含 `ValueNoise + FBM` 噪声扰动。
  - 已包含 UV 流动、边缘 glow、加色输出等核心逻辑。
  - 已具备 `BoostTrail` 主拖尾的第一版程序化效果基础。

- **主材质与兼容材质分流已完成**
  - `mat_trail_main` 走新的扰动路径。
  - `mat_trail_main_effect` 保留 legacy 路径。
  - 说明实验流与生产流已经被清晰分开。

---

### 二、已完成：工程化保护

- **`MaterialTextureLinker` 已补齐流程保护**
  - 运行编辑器工具时，会同步确保材质使用正确 Shader。
  - 会同步写入 `_UseLegacySlots` 模式位。
  - 会回填 `TrailMainEffect` 相关贴图，避免后续工具把这次接线冲掉。

- **材质接线已经工具化**
  - `mat_trail_main_effect` 会被自动设置为 legacy 模式。
  - `mat_trail_main` 会被自动设置为 disturbance 模式，并绑定 `_BaseMap`。
  - 这一步已经把“人肉记忆配置”变成了可重复执行的编辑器流程。

---

### 三、已完成但需要修正认知：`_BoostIntensity` 并非完全未接

根据当前代码，`BoostTrailView` 中的 `_BoostIntensity` 运行时驱动其实已经存在：

- 已有 `_intensityRampUpDuration`
- 已有 `_intensityRampDownDuration`
- 已有 PrimeTween 驱动的 `0 -> 1 -> 0` 插值
- 已通过 `MaterialPropertyBlock` 写入：
  - `_energyLayer2`
  - `_energyLayer3`
  - `_energyField`

这说明：

- **`_BoostIntensity` 不是完全没接**
- **它已经接到了“能量层 Shader”上**
- **但还没有接到 `TrailMainEffect.shader` 这条主拖尾 Shader 上**

这是当前实现里最关键的“半完成状态”。

---

### 四、当前真正做了一半的点

#### 1. `BoostTrailView` 的运行时驱动已经有了

**已完成部分：**
- Boost 开始/结束的强度插值
- Halo burst
- Bloom burst
- 粒子与拖尾启停控制

**未完成部分：**
- `MainTrail` 对应的 `TrailMainEffect.shader` 还没有吃到这套强度驱动

#### 2. `TrailMainEffect.shader` 目前仍偏静态参数驱动

当前 Shader 已有参数：

- `_NoiseScale`
- `_DistortStrength`
- `_FlowSpeed`
- `_FlickerStrength`
- `_EdgePower`
- `_Brightness`
- `_Alpha`

但当前并没有真正使用 `_BoostIntensity` 参与主拖尾扰动逻辑，因此：

- 它已经是**可调的 Shader**
- 但还不是**随 Boost 状态呼吸变化的 Shader**

#### 3. 风格方向已经明确，但还没有正式收口

当前目标风格已经很清楚：

- 更像 GG 的读感
- 更细长
- 更收束
- 更像脉冲式推进尾迹

但目前还没有完成一轮专门针对这个目标的参数收口与最终定版。

---

### 五、当前实现清单

### A. 已完成清单

- **学习资料与技术路线整理完成**
- **`BoostTrail` 已被选为首个 Shader 样板**
- **`TrailMainEffect.shader` 双模式改造完成**
- **程序噪声扰动路径已实现**
- **`mat_trail_main` / `mat_trail_main_effect` 分流完成**
- **`MaterialTextureLinker` 工程化保护完成**
- **`BoostTrailView` 中 `_BoostIntensity` 的基础运行时动画完成**
- **Halo / Bloom / 粒子 / Trail 的整体 VFX 主控框架完成**

### B. 部分完成清单

- **`_BoostIntensity` 运行时驱动**：已接到能量层，**未接到主拖尾 Shader**
- **主拖尾 Shader**：已能出效果，**但仍偏静态材质**
- **Boost 视觉读感**：已有 MVP，**但还没完成风格定型**
- **项目内可复用模板**：结构已成，**但还需要完整验证与收口**

### C. 未完成清单

- **给 `TrailMainEffect.shader` 增加 `_BoostIntensity` 参与逻辑**
- **让 `_BoostIntensity` 真正驱动主拖尾的扰动强度 / 亮度 / 边缘发光 / 流速中的至少一部分**
- **确认 `MainTrail` 的运行时参数写入方式**
  - 例如材质实例、`MaterialPropertyBlock`、或其他更适合 `TrailRenderer` 的写法
- **做一轮 GG 风格化参数收口**
- **完成一次 Unity Editor 内真实观感验证，并固定默认值**
- **将这套方法复制到 `HeatPulse / Heat Overload / DamageFlash`**

---

### 六、建议的下一步优先顺序

#### 第一步：补完主拖尾动态驱动

目标：让 `TrailMainEffect` 不再只是静态材质，而是会随 Boost 状态动态“活起来”。

建议改动点：
- `TrailMainEffect.shader`
- `BoostTrailView.cs`

完成标准：
- Boost 开始时主拖尾会明显进入激活态
- Boost 结束时主拖尾会顺着强度自然熄火，而不是硬切

#### 第二步：做一轮 GG 风格调参

目标：从“可用”推进到“像项目自己的效果”。

优先关注参数：
- `_DistortStrength`
- `_FlowSpeed`
- `_FlickerStrength`
- `_EdgePower`
- `_Brightness`
- `_Alpha`

完成标准：
- 更细长
- 更集中在推进轴线上
- 更像脉冲喷焰，而不是整片随机抖动

#### 第三步：固定为标准模板

目标：确认 `BoostTrail` 这套方案可以作为后续效果模板复用。

需要确认：
- 编辑器工具不会冲掉当前设置
- prefab 重建后效果一致
- 场景实例与 prefab 的读感一致

#### 第四步：复制到其他效果

在 `BoostTrail` 这条线收口后，再将方法迁移到：

- `HeatPulse / Heat Overload`
- `DamageFlash`

---

### 七、阶段判断

当前阶段不是“还在学 Shader”，而是：

**第一套项目内自研 Shader 样板已经做出来，但还没有完成动态联动与风格收口。**

如果后续要继续推进，最有价值的不是再开新的 Shader 分支，而是先把 `BoostTrail` 这条线收成一个真正可复用、可验证、可维护的标准件。

---

### 八、版本 B：落地任务单（字段 / 方法 / Shader 参数）

这一版不是继续讨论方向，而是把下一步真正要改的点拆到文件级别。
目标是：**让 `MainTrail` 的 `TrailMainEffect.shader` 真正吃到 Boost 运行时强度，而不是继续停留在静态材质参数阶段。**

### 1. 本轮 MVP 目标

- **Goal**：让主拖尾在 `OnBoostStart()` 时逐步进入激活态，在 `OnBoostEnd()` 时逐步熄火。
- **Scope**：仅修改 `TrailMainEffect.shader` 与 `BoostTrailView.cs`；本轮不新增 ScriptableObject，不改 legacy 路径，不扩散到 `HeatPulse` / `DamageFlash`。
- **Architecture**：优先沿用现有 `MaterialPropertyBlock` 思路，避免为 `TrailRenderer` 创建运行时材质实例。

### 2. 验收标准（满足以下条件视为完成）

- **运行时联动成立**：Boost 开始后，`MainTrail` 的扰动、亮度或边缘发光会随强度爬升而明显增强。
- **退出平滑**：Boost 结束后，主拖尾不会硬切，而是随着 `_BoostIntensity` 回落自然衰减。
- **兼容性保持**：`_UseLegacySlots > 0.5` 的路径完全不受本轮改动影响。
- **工程稳定**：不引入新的材质实例泄漏，不破坏当前 `MaterialTextureLinker` 的接线流程。
- **可调参**：策划/美术能在材质面板里继续通过原有核心参数调风格，而不是把所有表现写死在代码里。

### 3. 文件级改动方案

#### A. `TrailMainEffect.shader`

**目标：** 为 disturbance 路径加入 `_BoostIntensity` 输入，并把它接入核心表现参数。

**建议新增属性：**
- `_BoostIntensity ("Boost Intensity", Range(0, 1)) = 1`

之所以默认给 `1`，是为了在没有运行时驱动时依旧保持当前材质观感，不会让旧资产在 Inspector / 预览里突然显得“失效”。真正进入运行时后，再由 `BoostTrailView` 用 PropertyBlock 覆盖为 `0 -> 1 -> 0`。

**建议加入 `CBUFFER`：**
- `float _BoostIntensity;`

**建议改动 `ShadeDisturbance()`：**

先在函数开头建立局部强度：
- `float intensity = saturate(_BoostIntensity);`

然后不要直接全量替换所有参数，而是先做一轮 **最小可玩联动**，优先驱动这 4 项：

- **扰动强度**：`distortStrength = lerp(_DistortStrength * 0.35, _DistortStrength, intensity)`
- **流动速度**：`flowSpeed = lerp(_FlowSpeed * 0.45, _FlowSpeed, intensity)`
- **亮度**：`brightness = lerp(_Brightness * 0.55, _Brightness, intensity)`
- **Alpha**：`alphaScale = lerp(_Alpha * 0.25, _Alpha, intensity)`

可选第 5 项（如果第一轮联动还不够明显再加）：
- **边缘发光权重**：让 `edgeColor` 或 `edgeMask` 额外乘一个 `lerp(0.7, 1.0, intensity)` 或略更激进的系数

**建议保持不动的部分：**
- `ShadeLegacy()` 全部逻辑
- `_UseLegacySlots` 分支判断
- 现有噪声函数 `Hash21 / ValueNoise / FBM`

换句话说，这一轮不是改 Shader 风格底层逻辑，而是给现有 disturbance 路径增加一个运行时“总强度拨杆”。

#### B. `BoostTrailView.cs`

**目标：** 把已有的 `_BoostIntensity` 动画真正写进 `MainTrail` 对应的 Shader。

当前这个脚本里，`_BoostIntensity` 已经会写到：
- `_energyLayer2`
- `_energyLayer3`
- `_energyField`

这一轮只需要把同样的值继续写给 `_mainTrail`。

**建议新增私有字段：**
- `private MaterialPropertyBlock _mpbMainTrail;`

**建议在 `Awake()` 中初始化：**
- `_mpbMainTrail = new MaterialPropertyBlock();`

**建议在 `SetBoostIntensity(float value)` 中追加：**
- 对 `_mainTrail` 调用 `GetPropertyBlock()`
- 写入 `BoostIntensityID`
- 再 `SetPropertyBlock()`

建议写法与当前 `SpriteRenderer` / `MeshRenderer` 的逻辑保持一致，避免新开一套材质控制路径。

**本轮不建议做的事：**
- 不要同时引入运行时材质实例 clone
- 不要同时拆新的曲线资产或 SO
- 不要把 `BoostTrailView` 再拆成多个控制脚本

先让现有主控脚本把主拖尾动态驱动跑通，再决定要不要进一步解耦。

### 4. 推荐的最小改动点清单

#### `TrailMainEffect.shader`
- **新增**：`_BoostIntensity` Property
- **新增**：`CBUFFER` 中的 `_BoostIntensity`
- **修改**：`ShadeDisturbance()` 内对 `_DistortStrength`、`_FlowSpeed`、`_Brightness`、`_Alpha` 的实际使用方式

#### `BoostTrailView.cs`
- **新增字段**：`_mpbMainTrail`
- **修改方法**：`Awake()`
- **修改方法**：`SetBoostIntensity(float value)`
- **复用现有逻辑**：`OnBoostStart()` / `OnBoostEnd()` / `ResetState()` 无需重构，只需继续走当前强度动画链路

### 5. 运行时写入策略建议

本轮首选策略：**`TrailRenderer` + `MaterialPropertyBlock`**

原因：
- 和当前能量层的实现方式一致
- 不会污染材质资产
- 不会为每艘船或每次运行创建新的材质实例
- 更符合项目当前“先 work，再 right”的节奏

**风险点：**
- 如果 Unity 6 下 `TrailRenderer.SetPropertyBlock()` 实测对当前 Shader 不生效，那么再退回到“运行时克隆材质实例”的保底方案。

**保底方案只在必要时启用：**
- `Awake()` 时复制 `sharedMaterial`
- 运行时只改实例材质上的 `_BoostIntensity`
- `OnDestroy()` 时清理引用，避免泄漏

但在没有证据表明 MPB 不可用前，不建议先走这条更重的路。

### 6. 本轮建议的参数联动优先级

为了避免一次改太多导致风格失控，建议按这个顺序接：

1. **`_BoostIntensity -> _DistortStrength`**
2. **`_BoostIntensity -> _Brightness`**
3. **`_BoostIntensity -> _Alpha`**
4. **`_BoostIntensity -> _FlowSpeed`**
5. **`_BoostIntensity -> edge glow`（可选增强项）**

这样做的好处是：
- 第一眼变化最直观
- 容易判断是不是“联动成立”
- 就算只做到前 3 项，也已经能形成明确的 Boost 激活 / 熄火读感

### 7. 本轮明确不做的事

为了保证版本 B 是一个小而稳的 MVP，这一轮暂时**不做**：

- 不新增 `Shader Graph` 版本
- 不重写噪声模型
- 不改 `legacy slot` 路径
- 不把 `HeatPulse / Heat Overload / DamageFlash` 一起拉进来
- 不先做“大而全”的风格总调参面板

本轮唯一目标就是：**让 `MainTrail` 的自研 Shader 正式进入运行时动态驱动阶段。**

### 8. 版本 B 完成后的自然下一步

如果这一轮顺利完成，下一步就会非常明确：

- 先在 Unity 里做一次真实 Play Mode 观感验证
- 固定一版 GG 风格参数
- 再决定要不要把 `edge glow`、色温偏移、脉冲节奏做成更强的二阶段增强

换句话说，版本 B 的意义不是“做完最终效果”，而是把 `BoostTrail` 从 **静态可用材质** 升级为 **运行时可呼吸的主拖尾 Shader**。
