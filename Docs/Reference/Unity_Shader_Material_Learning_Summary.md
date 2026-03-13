# Unity Shader / Material 学习总结（Project Ark）

> **用途**：沉淀本轮针对 Unity Shader / Material 的学习成果，形成后续可复用的学习地图、项目内技术路线和 AI + MCP 工作流。
>
> **日期**：2026-03-13
>
> **适用项目**：Project Ark（静默方舟）— Unity 6 + URP 2D

---

## 学习目标

本轮学习的目标不是“泛泛收集 Shader 资料”，而是建立一套**对 Project Ark 真正有用**的 Shader / Material 学习与生产框架：

- 找到 **高质量、值得长期参考** 的 Unity Shader 教程和示例项目
- 提炼“别人是怎么写 Shader 的”这一层面的**代码观察方法**
- 判断是否已经存在 **AI / MCP 辅助 Shader / Material 开发** 的可落地工作流
- 把这些资料映射回 Project Ark 当前阶段的真实需求，例如：
  - `BoostTrail`
  - `Heat Overload`
  - `Damage Flash`
  - `Shield / Energy`
  - UI 扫描线、噪声扰动、叠加发光等 2D URP 常见效果

---

## 一、这次学习得出的总判断

### 1. 关于学习资料

结论很明确：**Unity Shader 值得学的资料很多，但真正高价值的不是碎片化博客，而是“官方图形仓库 + 体系化教程 + 高星 sample repo”三类组合。**

最值得长期依赖的来源不是“搜到什么看什么”，而是：

- **Unity 官方 Graphics 仓库**：看标准 URP/HDRP 工程化组织
- **Catlike Coding**：看概念、数学和结构化讲解
- **高星 GitHub 示例项目**：看完整效果如何落地到 Shader、Material、Scene
- **Unity 官方 Shader Graph 文档**：看与当前编辑器工作流最贴近的标准用法

### 2. 关于 AI / MCP 辅助 Shader 开发

结论同样明确：**已经存在可行路线，而且不是概念验证，而是能工作、能迭代的流程。**

但必须区分两件事：

- **最成熟的方式**：AI 生成 HLSL / ShaderLab / 节点方案，MCP 在 Unity 里导入、挂载、截图、读 Console、继续迭代
- **还不够成熟的方式**：让 AI 直接稳定地产出复杂 `.shadergraph` 资产并一次成功

因此，当前最现实、最适合 Project Ark 的路线是：

```text
AI 负责：效果拆解、参数设计、HLSL / ShaderLab 编写、Shader Graph 节点方案
MCP 负责：导入 Unity、创建/修改 Material、挂到对象、截图验证、读取 Console、快速回路迭代
```

---

## 二、推荐学习资料（5 个重点资源）

以下 5 个资源是本轮筛选后，最值得重点投入时间的学习对象。

---

### 1. `Unity-Technologies/Graphics`

- **类型**：Unity 官方图形仓库
- **价值级别**：最高优先级的“标准答案库”
- **为什么重要**：
  - 这是 Unity 自己维护的图形栈源码
  - 能直接看到 `URP` / `HDRP` 的 Shader 组织方式
  - 能学习官方如何组织：
    - `ShaderLibrary`
    - `include` 文件
    - `Pass`
    - `Renderer Feature`
    - 材质属性和渲染管线的接线关系
- **适合学习的内容**：
  - URP Shader 的标准骨架
  - `Attributes` / `Varyings` 结构
  - 顶点到片元的数据传递方式
  - 官方命名、目录布局、通用函数组织
- **适合什么时候看**：
  - 当你已经会一点 Shader，想建立“标准 URP 工程写法”时
- **注意点**：
  - 这个仓库不算最友好入门材料，但它最接近“规范写法”

---

### 2. `ColinLeung-NiloCat/UnityURPUnlitScreenSpaceDecalShader`

- **类型**：URP 效果型 sample
- **社区热度**：约 `1.3k+ stars`
- **为什么重要**：
  - 非常适合学习一个实际效果是如何从 Shader 落到 Material 和场景使用的
  - 仓库体量比官方仓库小，更容易读完主干逻辑
- **适合学习的内容**：
  - `URP Unlit Shader` 的实际写法
  - 屏幕空间坐标的使用方式
  - 透明混合与边缘过渡
  - 参数如何暴露给 Material，供美术调参
- **对 Project Ark 的直接价值**：
  - 地面警示圈
  - 命中覆盖层
  - 范围提示
  - Boost 尾迹叠加投影类效果

---

### 3. `keijiro/NoiseShader`

- **类型**：噪声 Shader 示例仓库
- **社区热度**：约 `1.3k+ stars`
- **为什么重要**：
  - 仓库小而精，适合“读代码学写法”
  - 噪声是 2D/3D 特效里极高频的基础能力
- **适合学习的内容**：
  - Value Noise / Simplex / FBM 等噪声函数组织方式
  - 如何把数学函数封装为可复用 Shader 片段
  - 如何把“函数”变成“视觉效果”
- **对 Project Ark 的直接价值**：
  - 推进尾焰扰动
  - 热量过载波纹
  - 护盾起伏
  - UI 扫描线 / dissolve / alpha 抖动

---

### 4. `hecomi/UnityFurURP`

- **类型**：URP 中高复杂度效果 sample
- **社区热度**：约 `1.3k+ stars`
- **为什么重要**：
  - 虽然 Fur 不是 Project Ark 当前重点，但这个仓库很适合学习中高复杂度 Shader 工程如何组织
- **适合学习的内容**：
  - 多层视觉效果的组织方式
  - 多 Pass 的用途与管理
  - 材质参数如何分组
  - 复杂效果的工程化结构
- **对 Project Ark 的启发**：
  - 学的不是“毛发”，而是**复杂视觉效果的组织法**

---

### 5. `Gaolingx/GenshinCelShaderURP`

- **类型**：URP 风格化 / Cel Shader 示例
- **社区热度**：约 `700+ stars`
- **为什么重要**：
  - 很适合观察风格化 Shader 如何组织参数、分层和美术调参接口
- **适合学习的内容**：
  - Ramp / Toon / 边缘光等风格化技巧
  - 风格化效果的参数暴露方式
  - 项目化 Shader 如何兼顾可调和可维护
- **对 Project Ark 的启发**：
  - 如果后续希望统一整体风格、强化主视觉表达，这类项目是很好的参考对象

---

## 三、两个必须补的基础教程来源

上面 5 个更偏“看代码、看 sample”。除此之外，真正要打基础，还必须补两个教程源。

### 1. `Catlike Coding`

- **特点**：体系化、讲原理、循序渐进
- **核心价值**：
  - 帮你理解 Shader 为什么这样写，而不只是照抄
  - 擅长把复杂概念拆成可理解的小步骤
- **建议优先吸收的主题**：
  - Vertex / Fragment 基础
  - UV / Normal / Screen Space
  - Blend / ZWrite / Cull / Depth
  - Noise / Interpolation / Mask / Gradient
- **最重要的收获**：
  - 学会把一个效果拆成：

```text
输入 → 坐标/空间变换 → mask → 动态层 → 颜色层 → 输出
```

### 2. Unity 官方 `Shader Graph` 文档 / Learn 教程

- **特点**：官方、贴近实际工作流、与当前 URP 使用习惯一致
- **核心价值**：
  - 建立 Shader Graph 与 Material 的完整链路认知
  - 适合快速做原型，尤其适合需要频繁调参的视觉效果
- **建议用途**：
  - 原型验证
  - 给自己或策划/美术提供快速调参入口
  - 与 `Custom Function` 形成混合工作流

---

## 四、正确的学习顺序（非常重要）

按 Star 数排序并不是最有效的学习方式。真正高效的顺序应该是下面这样：

### 第 1 步：先学 `Catlike Coding`

目标：建立“我知道自己在看什么”的能力。

先掌握：

- Shader 的基本结构
- 顶点/片元职责分工
- 坐标空间
- UV 操作
- Blend / 深度 / 剔除
- 噪声、插值、渐变、遮罩

**为什么这一步必须最先做**：

因为如果没有原理基础，后面去看高星 repo，只会看到一堆宏、数学函数和 include，无法真正吸收。

---

### 第 2 步：再看 `Unity-Technologies/Graphics`

目标：建立“标准 URP 工程骨架”的感觉。

重点学习：

- 官方如何组织 Shader
- 官方如何拆 `include`
- Pass 与渲染流程的关系
- Attributes / Varyings 结构
- 材质属性的命名与组织

**你最终要带走的成果**：

- 一份自己的 `URP Unlit` 标准骨架认知
- 一份知道该如何扩展的模板思路

---

### 第 3 步：看 `UnityURPUnlitScreenSpaceDecalShader`

目标：从“标准骨架”进入“完整效果落地”。

重点学习：

- 一个完整效果项目的目录结构
- 屏幕空间相关坐标如何用
- 参数如何服务 Material 调参
- 边缘过渡和混合模式如何写

---

### 第 4 步：看 `NoiseShader`

目标：把数学函数转化为可复用的 VFX 资产。

重点学习：

- 噪声函数封装方式
- 最小成本做出“有生命感”的动态效果
- UV 偏移、扰动、alpha 噪声、dissolve 这些常见模式

---

### 第 5 步：最后看 `GenshinCelShaderURP` 或 `UnityFurURP`

目标：学习复杂项目的组织方式，而不是只看效果表面。

如果你更想优先学风格化：

- 先看 `GenshinCelShaderURP`

如果你更想优先学复杂视觉工程组织：

- 先看 `UnityFurURP`

**这一阶段重点不是复刻效果本身，而是学习：**

- 参数分组
- Keyword / Variant 管理
- 多层视觉如何解耦
- Shader 与 Material 的职责边界

---

## 五、读别人 Shader 代码时，应该怎么看

这一部分是本轮学习最关键的成果之一。

以后读 Shader，不要只看“效果酷不酷”，而要按下面这套观察框架看。

### 1. 先看结构

重点观察：

- `Properties`
- `SubShader`
- `Pass`
- `Tags`
- `Blend`
- `ZWrite`
- `Cull`
- `ZTest`

你需要先回答：

- 这是 `Lit` 还是 `Unlit`
- 这是不透明还是透明
- 是否需要深度
- 是否需要多 Pass

---

### 2. 再看参数设计

重点观察：

- 哪些参数暴露给 `Material`
- 哪些参数写死在 Shader 中
- 参数名是否清晰表达美术含义
- 参数是否最小化、是否易调

你要学的是：

- 怎样设计一套让自己和美术都不痛苦的参数系统

---

### 3. 看复用方式

重点观察：

- 是否有公共函数
- 是否有 `include` / `ShaderLibrary`
- 是否用了 Keyword / Variant
- 多个效果之间是否共享数学函数或输入逻辑

你要学的是：

- 别人如何避免“一份 Shader 写成一坨不可维护的特例代码”

---

### 4. 看项目落地方式

重点观察：

- Shader 如何连接到 Material
- Material 如何暴露给场景对象
- 最终效果依赖哪些场景设置、贴图、Renderer 配置

你要学的是：

- “一个视觉效果”不是只有一段 Shader，而是一整条资产链路

---

### 5. 看 URP 限制与约束

重点观察：

- 这是不是 URP 推荐做法
- 哪些只是 demo 技巧
- 哪些地方可能和 2D Renderer / Sprite 渲染路径有差异

你要学的是：

- 不要把某个 demo 的技巧不加判断地搬进项目

---

## 六、为 Project Ark 提炼出的 5 个实战抓手

以后在 Project Ark 中落地 Shader / Material，我会优先按下面 5 个抓手组织，而不是凭感觉硬写。

### 1. 骨架优先

任何效果开始之前，先定 4 件事：

- `Unlit` 还是 `Lit`
- 是否透明
- 是否需要深度
- 是否需要多 Pass

如果这一步不先定，后面几乎一定会返工。

---

### 2. 参数最小化

一开始只暴露真正影响手感和视觉辨识度的参数，例如：

- `Tint`
- `Intensity`
- `Speed`
- `NoiseScale`
- `EdgeWidth`
- `Opacity`

不要一开始就堆十几个参数，否则迭代速度会明显下降。

---

### 3. 效果拆层

把每个视觉效果强制拆成这四层：

- **形状层**：由贴图 Alpha、Mask、Distance、Sprite 轮廓等决定
- **动态层**：由 Noise、Pulse、Flow、UV Offset 等决定
- **颜色层**：由 Tint、Gradient、Emission、Edge Glow 等决定
- **输出层**：由 Blend、Alpha、Depth、排序层决定

这样可以显著提升可维护性和复用性。

---

### 4. Shader Graph 与 HLSL 混合

推荐策略：

- 简单效果：优先 `Shader Graph`
- 复杂逻辑：放 `Custom Function` 或直接用 HLSL / ShaderLab

这是最适合小团队和高迭代节奏的折中方案。

---

### 5. 为调参而写，不是为炫技而写

Project Ark 当前最重要的不是“最炫 Shader”，而是：

- 2 分钟内能进 Play Mode 看到差异
- Material 参数能快速试出手感
- 效果能服务战斗可读性和反馈，而不是抢戏

这与项目的核心开发哲学是完全一致的。

---

## 七、最值得优先补的 Shader 方向（针对 Project Ark）

结合项目风格、阶段和收益，最值得优先投入的是这 5 类，而不是广撒网。

### 1. `Sprite / VFX Unlit Shader`

用途：

- 飞船主体附加层
- 弹道尾迹
- 命中特效
- 叠加发光

这是 2D URP 项目里性价比最高的一类。

---

### 2. `Noise / Distortion Shader`

用途：

- Heat Overload 波纹
- 护盾扰动
- 能量场边缘
- 推进尾流扰动

这是动态感和“科技感”的核心来源之一。

---

### 3. `Screen-space / Overlay` 效果

用途：

- 范围提示
- 命中覆盖
- 冲刺残影叠加
- 屏幕提示层

这类效果的收益很高，而且很容易直接影响玩家对战斗状态的理解。

---

### 4. `Stylized Lighting / Cel` 风格化表达

用途：

- 统一视觉调性
- 强化世界观风格感
- 做特定强表现敌人或装置的辨识度

这不是最先做的，但值得作为中期储备方向。

---

### 5. `Custom Function + Shader Graph` 混合路线

用途：

- 既保留可视化编辑优势
- 又可以处理 Graph 难以表达的复杂逻辑

这是当前最适合 AI 参与协作的一条路线。

---

## 八、AI + MCP 辅助 Shader / Material 的推荐工作流

这是本轮学习中对后续实操最有价值的一部分。

### 总原则

**AI 负责设计和编码，MCP 负责落地和验证。**

这条分工是目前最靠谱的。

---

### 路线 A：Shader Graph 原型工作流

适用场景：

- 扫光
- 能量描边
- UI 扫描线
- 简单 UV 扰动
- 热量条特效
- 透明叠加发光

流程：

```text
目标效果描述
→ AI 输出节点方案
→ 在 Unity 中搭建 Shader Graph
→ 创建 Material
→ 挂到测试对象
→ 截图回看
→ 调整 Blackboard 参数
→ 继续迭代
```

AI 在这条路上最擅长：

- 把视觉目标翻译为节点步骤
- 设计 Blackboard 参数
- 规划哪些参数需要暴露给 Material
- 将复杂局部逻辑改写成 `Custom Function`

---

### 路线 B：HLSL / ShaderLab 生产工作流

适用场景：

- 更复杂的效果
- 性能要求更高的效果
- 需要稳定版本控制与 diff 的效果
- 需要复用和持续演化的项目级 Shader

流程：

```text
描述目标体验
→ AI 生成 URP Shader 骨架
→ AI 补顶点/片元逻辑、Blend、Pass、参数
→ 导入 Unity
→ 创建/修改 Material
→ 挂到测试对象
→ 读取 Console
→ 截图验证
→ 根据视觉结果继续调整
```

这条路线是当前最推荐的“生产线”。

---

### 为什么不建议一开始就依赖 AI 直接生成复杂 Shader Graph 资产

原因有三点：

1. `.shadergraph` 是复杂序列化资产，不如文本 Shader 稳定
2. 复杂 Graph 很难通过纯文本一次性安全生成
3. 对于版本对比、审查和小范围迭代，HLSL / ShaderLab 更清晰

所以最佳策略不是否定 Shader Graph，而是：

- **Graph 用于原型和高频调参**
- **HLSL / ShaderLab 用于稳定生产**

---

## 九、Project Ark 的推荐落地方向

基于当前项目阶段，最适合做首批模板化沉淀的是下面 3 类：

### 1. `BoostTrail`

建议优先学习来源：

- `NoiseShader`
- `URP Unlit` 透明叠加写法

重点吸收：

- UV 扰动
- 噪声驱动透明变化
- Additive / Alpha Blend 尾迹组织方式

---

### 2. `HeatPulse / Heat Overload`

建议优先学习来源：

- `NoiseShader`
- `Catlike Coding` 的 pulse / mask / gradient 思路

重点吸收：

- Pulse 感
- Dissolve / Edge Glow
- 参数如何暴露给 Material 快速调试

---

### 3. `DamageFlash`

建议优先学习来源：

- `URP Unlit` 基础骨架
- Overlay / 透明叠加的简单写法

重点吸收：

- 最小结构的 Unlit Overlay Shader
- 闪烁/受击高亮的输出组织方式
- 与 SpriteRenderer/Material 的实际协作链路

---

## 十、以后做任何 Shader / Material 时的输出模板

基于本轮学习，后续如果要为 Project Ark 设计新效果，推荐统一按下面模板描述和实施。

### 1. 目标体验

先回答：

- 玩家此刻应该看到什么
- 玩家此刻应该感觉到什么
- 这个效果服务的是手感、可读性，还是世界氛围

### 2. 推荐实现路线

在开始前就明确：

- 用 `Shader Graph`
- 还是用 `HLSL / ShaderLab`
- 还是混合路线

### 3. 核心层次拆分

固定按四层组织：

- Shape
- Motion
- Color
- Output

### 4. 材质参数表

列出真正需要暴露给 Material 的最小参数集。

### 5. 适配对象

说明这个效果最终挂在什么对象上：

- `SpriteRenderer`
- `ParticleSystem`
- `TrailRenderer`
- `LineRenderer`
- 屏幕覆盖层

### 6. MCP 验证方式

明确如何验证：

- 截图比对
- Console 检查
- 实机 Play Mode 看参数反馈
- 材质迭代调参

---

## 十一、最终结论

本轮学习的核心结论可以收束为 6 句话：

1. **真正值得学的 Unity Shader 资料，核心是官方 Graphics、Catlike Coding 和少数高星 Sample。**
2. **学习顺序应是：原理 → 官方骨架 → 完整效果 Sample → 噪声资产化 → 复杂工程组织。**
3. **读 Shader 代码时，要看结构、参数、复用、落地链路和 URP 约束，而不是只看视觉结果。**
4. **AI + MCP 辅助 Shader 开发已经可行，但最成熟的是“AI 生成文本方案，MCP 完成 Unity 落地和验证”。**
5. **对 Project Ark 而言，最先补的是 Unlit、Noise、Overlay、调参友好的 Material 工作流。**
6. **最重要的不是做出最复杂的 Shader，而是做出能快速迭代、能服务战斗体验和视觉可读性的 Shader。**

---

## 十二、后续建议

建议后续工作按这个顺序继续推进：

1. 为这 5 个资料补一份“具体该看哪些文件/目录关键词”的阅读清单
2. 基于 Project Ark 做 3 个首批模板：
   - `BoostTrail`
   - `HeatPulse`
   - `DamageFlash`
3. 建立项目内统一的 Shader / Material 参数命名规范
4. 逐步形成可复用的 `URP Unlit 2D VFX` 基础模板

这样后续每做一个新效果，就不是从零开始，而是在已有骨架上快速迭代。
