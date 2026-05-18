# Ship Art / VFX Workflow

> **文档定位**：本文档描述 Project Ark 如何从 0 到 1 生产、接入、验证飞船美术与特效工作流。  
> **适用对象**：金丝雀号主体美术、飞船状态 Sprite、Boost / Hit / Fire / Weaving / Overheat VFX、材质、Shader、后处理、验证场景。  
> **不替代**：本文档不替代现役主链规范与资产注册表。现役链路、owner、路径、状态判断仍以 `Docs/2_TechnicalDesign/Ship/ShipVFX_CanonicalSpec.md` 与 `Docs/2_TechnicalDesign/Ship/ShipVFX_AssetRegistry.md` 为准。  
> **参考输入**：`Docs/7_Reference/GameAnalysis/Minishoot_vs_GalacticGlitch_ArtModule_Comparison.md`、`Docs/7_Reference/GameAnalysis/GalacticGlitch_Structure_Analysis.md`、`Docs/7_Reference/GameAnalysis/ShipVFX_PlayerPerception_Reference.md`。

---

## 1. 总目标

Project Ark 的飞船美术模块不是“一张 Sprite 替换工程”，而是一套围绕玩家状态变化服务的视觉反馈系统。

金丝雀号最终应由以下模块共同组成：

```text
飞船主体 Sprite 分层
+ 状态 Sprite / 贴图变体
+ Material 参数变体
+ Shader / Shader Graph 表现算法
+ ParticleSystem / TrailRenderer / Sprite VFX
+ PrimeTween / Animator 状态过渡
+ Bloom / PostProcess / Camera Juice
+ Ship/VFX Runtime 主链
+ Debug / Validator / Test Scene 验证闭环
```

### 1.1 核心原则

- **手感优先**：飞船美术必须服务 Boost、射击、受击、编织、过热等玩家可感知状态。
- **可读性优先**：缩小到实机尺寸后，朝向、状态、危险信号必须清楚。
- **垂直切片优先**：先跑通 Normal → Boost → Hit → Fire → Weaving 的完整闭环，再扩展 LowHealth / Death / Respawn。
- **少量 Shader，中量 Material，多层 Sprite，大量参数驱动**：避免为每个组合状态画整船或写独立 Shader。
- **不新增第二真相源**：接入现有 `ShipView` / Worker / `BoostTrailView` / Prefab Builder / Scene Binder 主链。
- **Debug 不接管正式链**：调试工具只观察与预览，不成为 Runtime owner。

---

## 2. 最终工作流总览

```text
Step 0  定义视觉目标与玩家感受
Step 1  建立飞船状态表
Step 2  设计飞船视觉分层
Step 3  生产概念图与参考板
Step 4  生产 Normal 分层 Sprite
Step 5  生产状态 Sprite / Overlay / Mask
Step 6  Unity 导入与 Sprite 规范化
Step 7  接入 Ship.prefab 多层视觉骨架
Step 8  建立 Material / Shader 基础库
Step 9  制作核心 VFX Prefab
Step 10 接入 Runtime 状态驱动
Step 11 加入 Animator / PrimeTween 过渡
Step 12 加入 Bloom / PostProcess / Camera Juice
Step 13 建立测试场景与调试面板
Step 14 资产注册、审计、文档固化
Step 15 迭代扩展新状态 / 新皮肤 / 新飞船
```

每一步都必须回答三件事：

1. **这一步产出什么？**
2. **推荐用什么方式生产？**
3. **怎么确认它可以进入下一步？**

---

## 3. Step 0：定义视觉目标与玩家感受

### 3.1 目的

在生成图片、写 Shader、做 Prefab 之前，先确认金丝雀号在玩家心中的定位。

推荐方向：

```text
破旧工业探测船 + 神秘星图核心
```

也就是：

- 船体本身像求生工具，有磨损、金属、暴露结构。
- 能量核心来自星图遗物，有紫蓝色、符号、脉冲、编织感。
- Boost / Weaving / Overheat 不是单纯变亮，而是船体状态发生变化。

### 3.2 需要产出

- `ShipVisualDirection` 文本说明。
- 3-5 张飞船主体参考。
- 3-5 张能量 / Boost / Aura / Overheat 参考。
- 一组颜色关键词：主体色、能量色、危险色、受击色。
- 一组禁止方向。

### 3.3 推荐生产方式

- 从 `Galactic Glitch` 提取飞船状态参考：多层、能量、高光、状态切换。
- 从 `Minishoot` 提取可读性参考：轮廓清楚、弹幕环境中不糊。
- 使用 AI 生图工具生成 mood board，但不要直接接受第一张图为最终资产。
- 使用固定 prompt 模板约束视角、透明背景、轮廓、风格。

### 3.4 推荐 Prompt 方向

```text
Top-down 2D spaceship, small fragile industrial scout ship, worn metal hull, mysterious star-map energy core, readable silhouette, transparent background, game sprite, orthographic top-down view, centered composition, clear nose direction, blue purple energy accents, no cockpit close-up, no perspective distortion
```

### 3.5 验收标准

- 能用一句话说明金丝雀号的视觉气质。
- 飞船主色、能量色、危险色已确定。
- 视觉方向能服务 `Normal / Boost / Fire / Hit / Weaving / Overheat` 六类状态。
- 缩小到实机显示尺寸后仍能看出朝向。

---

## 4. Step 1：建立飞船状态表

### 4.1 目的

先定义“玩家会经历哪些视觉状态”，再决定哪些状态需要 Sprite、哪些需要 Material、哪些需要 VFX。

### 4.2 推荐状态分类

#### Base State

Base State 是飞船主体或主要视觉层会变化的状态。

| 状态 | 玩家感受 | MVP | 推荐表现 |
| --- | --- | --- | --- |
| `Normal` | 默认探索 / 战斗 | 是 | Body + Energy + Highlight 基础层 |
| `Boost` | 瞬间推进、速度增加 | 是 | Energy 变亮、Engine/Trail 增强、Bloom pulse |
| `Fire` | 武器发射反馈 | 是 | Muzzle flash、Energy pulse、轻微 recoil juice |
| `Hit` | 受击、危险、短促反馈 | 是 | 白闪、火花、短震、i-frame flicker |
| `Weaving` | 星图编织、能量展开 | 是 | Aura、紫蓝能量脉冲、后处理轻微变化 |
| `Overheat` | 热量危险、系统过载 | 第二批 | 橙红警告、抖动、火花、vignette |
| `LowHealth` | 受损、勉强运行 | 第二批 | Damage overlay、漏电、核心警告 |
| `Death` | 爆炸、失控、终止 | 第二批 | 分层爆炸、碎片、屏幕冲击 |

#### Overlay State

Overlay State 不应重画整船，而是叠加在 Base State 上。

| 状态 | 推荐实现 |
| --- | --- |
| `HitFlash` | `ShipHitVisuals` 同步驱动 5 层白闪 |
| `Invulnerable` | Solid / HL / Core 闪烁 |
| `HeatRising` | Energy / Core 颜色偏橙、抖动增强 |
| `WeaponCharged` | Core / Muzzle socket 脉冲 |
| `DashGhost` | `Dodge_Sprite` + `DashAfterImageSpawner` |

#### Moment VFX

Moment VFX 是一次性事件，不应变成常驻状态。

| 事件 | 推荐实现 |
| --- | --- |
| `BoostStart` | FlameCore burst + Bloom burst + Trail ramp |
| `BoostEnd` | Ember decay + Trail fade |
| `WeaponFired` | MuzzleFlash VFX + Energy pulse |
| `DamageTaken` | HitSpark + HitFlash + Camera impulse |
| `WeavingEnter` | Aura 展开 + 后处理 pulse |
| `OverheatStart` | 火花 + 红橙 pulse + 轻微 chromatic aberration |

### 4.3 需要产出

- `ShipStateVisualMatrix`：状态 → Sprite / Material / VFX / PostProcess 对照表。
- `MVP State List`：第一批只做 `Normal / Boost / Fire / Hit / Weaving`。
- `Future State List`：第二批做 `Overheat / LowHealth / Death / Respawn`。

### 4.4 推荐生产方式

- 先用 Markdown 表格定义状态，不急着创建 SO。
- 每个状态写清：玩家感受、视觉表现、实现层级、验收标准。
- 若状态组合超过两层，优先拆成 Base + Overlay + Moment，而不是新增完整状态图。

### 4.5 验收标准

- 任一状态都能说清楚“玩家此刻应该感受到什么”。
- 不存在 `Boost+Hit+Weaving+Overheat` 这类整船组合图需求。
- 每个状态都能归入 Sprite / Material / VFX / PostProcess / Camera 的某一层。

---

## 5. Step 2：设计飞船视觉分层

### 5.1 目的

把飞船从“一张图”升级成可组合的多层视觉结构。

当前现役链路已存在：

```text
ShipVisual
├── Ship_Sprite_Back
├── Ship_Sprite_Liquid
├── Ship_Sprite_HL
├── Ship_Sprite_Solid
├── Ship_Sprite_Core
├── Dodge_Sprite
└── BoostTrailRoot
```

本文档后续生产都应围绕这条主链扩展，不另起第二套飞船视觉根节点。

### 5.2 推荐层级职责

| Canonical 语义 | 当前 Physical Name | 职责 |
| --- | --- | --- |
| `ShipBackSprite` | `Ship_Sprite_Back` | 船尾、推进器基底、后层装饰 |
| `ShipLiquidSprite` | `Ship_Sprite_Liquid` | 能量、液态、状态色变化 |
| `ShipHighlightSprite` | `Ship_Sprite_HL` | 高光、闪白、边缘强调 |
| `ShipSolidSprite` | `Ship_Sprite_Solid` | 主体轮廓、实体结构 |
| `ShipCoreSprite` | `Ship_Sprite_Core` | 核心、眼睛、反应堆、状态脉冲 |
| `ShipDashGhostSprite` | `Dodge_Sprite` | Dash 静态残影 |
| `BoostTrailRoot` | `BoostTrailRoot` | Boost 尾迹、火焰、余烬、能量层 |

### 5.3 需要产出

- 分层设计图或表格。
- 每层的图像职责。
- 每层是否需要独立 Sprite。
- 每层是否需要独立 Material。
- 每层是否会被 Runtime 动态改参数。

### 5.4 推荐生产方式

- 以 `Galactic Glitch` 的 `solid / liquid / highlight` 三层思路作为基础。
- 但采用 Project Ark 当前现役节点命名，不照搬参考项目节点名。
- 优先把状态变化放在 `Liquid / HL / Core / BoostTrailRoot`，不要频繁替换 `Solid` 主轮廓。
- `Solid` 应保持最高可读性，确保 Bloom / Shader 失效时飞船仍能识别。

### 5.5 验收标准

- 关闭 `Liquid / HL / Core / BoostTrailRoot` 后，`Solid` 仍可读。
- 打开全部层后，飞船不糊、不遮挡朝向。
- 每层职责单一，不出现同一视觉效果由多个层重复承担。
- 不新增与 `ShipVisual` 平行的第二视觉主链。

---

## 6. Step 3：生产概念图与参考板

### 6.1 目的

用 AI 工具或手绘快速探索飞船轮廓，但不直接进入 Unity。

### 6.2 需要产出

- 8-20 张飞船概念缩略图。
- 3 张候选 Top-down 正交图。
- 1 张最终选定轮廓图。
- 1 份弃用原因记录：为什么没有选择其他方向。

### 6.3 推荐生产方式

#### AI 生图流程

```text
1. 先生成多张 silhouette / concept
2. 选 1-3 个轮廓方向
3. 针对选中轮廓做 top-down orthographic 重绘
4. 锁定一张作为 source of truth
5. 再基于这张图拆层，而不是每层重新随机生成
```

#### 图像要求

| 项目 | 要求 |
| --- | --- |
| 视角 | Top-down / orthographic |
| 朝向 | 全项目统一，推荐默认朝右或朝上，由项目当前控制逻辑决定 |
| 画布 | 512×512 或 1024×1024 source |
| 背景 | 透明或纯色方便抠图 |
| 主体 | 居中、留出尾焰空间 |
| 光源 | 固定方向，不随图变化 |
| 轮廓 | 128px 预览下仍能看清 |

### 6.4 推荐工具

- AI 生图：用于概念探索、轮廓、能量纹理方向。
- Photoshop / Krita / Procreate：用于抠图、清理边缘、拆层。
- Aseprite：如果转向低分辨率 Sprite 或做 frame animation。
- Python / ImageMagick：用于批量裁切、透明边检查、尺寸统一。

### 6.5 验收标准

- 选定轮廓适合 Top-down 玩法。
- 飞船朝向一眼可辨。
- AI 生成细节不会干扰实机缩放后的识别。
- 有明确 source image，后续拆层都基于它。

---

## 7. Step 4：生产 Normal 分层 Sprite

### 7.1 目的

先做最基础、最稳定的默认飞船表现。Normal 是后续所有状态的基线。

### 7.2 需要产出

建议第一批至少产出：

```text
spr_ship_canary_solid_normal.png
spr_ship_canary_liquid_normal.png
spr_ship_canary_highlight_normal.png
spr_ship_canary_core_normal.png
spr_ship_canary_back_normal.png
```

如果暂时不做全部层，最低可接受：

```text
spr_ship_canary_solid_normal.png
spr_ship_canary_liquid_normal.png
spr_ship_canary_highlight_normal.png
```

### 7.3 推荐生产方式

#### 手工 / AI 混合拆层

```text
1. 以最终概念图为基底
2. 手工清理主体轮廓，得到 Solid
3. 从主体中提取能量纹路，得到 Liquid
4. 提取高光边缘或重绘，得到 Highlight
5. 提取核心发光区域，得到 Core
6. 提取尾部/推进器基底，得到 Back
```

#### 每层图像规则

| 层 | 图像规则 |
| --- | --- |
| `Solid` | 不依赖发光也能识别飞船；alpha 干净；避免过多细碎纹理 |
| `Liquid` | 主要承载能量纹路和状态颜色；适合 Material 改色 |
| `Highlight` | 高光 / 边缘 / 受击闪白；适合 additive 或 alpha tween |
| `Core` | 小范围强状态信号；适合低血量 / overheat / weaving pulse |
| `Back` | 尾部喷口和船后结构；为 Boost 提供视觉锚点 |

### 7.4 验收标准

- 所有层画布尺寸一致。
- 所有层 pivot 一致。
- 所有层叠合后与概念图一致。
- 单独看 `Solid`，飞船仍可读。
- 单独看 `Liquid / Highlight / Core`，知道它们不是主体，而是叠加层。

---

## 8. Step 5：生产状态 Sprite / Overlay / Mask

### 8.1 目的

为 `Boost / Fire / Hit / Weaving / Overheat` 生产必要的状态图，但避免组合爆炸。

### 8.2 需要产出

#### Boost

```text
spr_ship_canary_liquid_boost.png
spr_ship_canary_core_boost.png
spr_ship_canary_back_boost.png
tex_boost_trail_main.png
tex_boost_noise_main.png
```

#### Fire

```text
spr_ship_canary_core_firepulse.png
spr_vfx_muzzle_flash_canary.png
tex_vfx_muzzle_flash_mask.png
```

#### Hit

```text
spr_ship_canary_highlight_hitmask.png
spr_vfx_hit_spark_01.png
spr_vfx_hit_spark_02.png
```

#### Weaving

```text
spr_ship_canary_liquid_weaving.png
spr_ship_canary_core_weaving.png
spr_ship_canary_aura_weaving.png
tex_weaving_noise.png
tex_weaving_ring_mask.png
```

#### Overheat

```text
spr_ship_canary_liquid_overheat.png
spr_ship_canary_core_overheat.png
tex_overheat_noise.png
spr_vfx_overheat_spark.png
```

### 8.3 推荐生产方式

| 状态 | 推荐生产方式 |
| --- | --- |
| `Boost` | 基于 Normal 的 `Liquid / Back / Core` 改造，不重画整船；尾焰用独立纹理 + Trail / Particle |
| `Fire` | 不换整船；用 muzzle flash、core pulse、短促 bloom 表达 |
| `Hit` | 不重画整船；用 `Highlight` 白闪 + HitSpark 粒子 |
| `Weaving` | 重点做 Aura / Liquid / Core；体现星图符号与紫蓝能量 |
| `Overheat` | 重点做 Liquid/Core 颜色偏移、火花、热扰动；不让它像受击闪白 |

### 8.4 关键限制

禁止生产这类组合图：

```text
canary_boost_hit_weaving_overheat.png
canary_fire_boost_hit.png
canary_weaving_lowhealth_fire.png
```

遇到组合状态，使用：

```text
Base Sprite
+ Overlay Material
+ Moment VFX
+ PostProcess Pulse
```

### 8.5 验收标准

- 每个状态至少有一个“玩家一眼可见”的视觉信号。
- 状态之间颜色与形状不混淆。
- `Hit` 与 `Overheat` 不应都只是红/白闪。
- `Weaving` 必须和普通 Boost 区分。
- 状态图不制造组合爆炸。

---

## 9. Step 6：Unity 导入与 Sprite 规范化

### 9.1 目的

把图像资产稳定导入 Unity，避免尺寸、pivot、alpha、压缩设置不一致。

### 9.2 推荐目录

新生产的金丝雀号正式资产建议放在：

```text
Assets/_Art/Ship/Canary/
├── Source/
│   ├── Concepts/
│   ├── Layered/
│   └── Exports/
├── Sprites/
│   ├── Solid/
│   ├── Liquid/
│   ├── Highlight/
│   ├── Core/
│   ├── Back/
│   └── Aura/
├── Materials/
├── Shaders/
└── Textures/
```

Boost / 通用 VFX 继续遵守现役路径：

```text
Assets/_Art/VFX/BoostTrail/
├── Materials/
├── Shaders/
└── Textures/
```

### 9.3 Import Settings 建议

| 设置 | 推荐值 |
| --- | --- |
| Texture Type | `Sprite (2D and UI)` |
| Sprite Mode | `Single`，除非是 spritesheet |
| Pixels Per Unit | 与当前飞船主链一致，不独立发明 |
| Mesh Type | `Full Rect` 优先，若需要贴合轮廓再考虑 `Tight` |
| Filter Mode | 非像素风使用 `Bilinear` |
| Compression | 初期 `None` 或高质量；后期统一优化 |
| Generate Mip Maps | 2D Sprite 通常关闭 |
| Alpha Is Transparency | 开启 |
| Pivot | 所有层一致 |

### 9.4 推荐生产方式

- 用 Unity Editor 批量检查导入设置。
- 若资产数量增加，写 Editor 工具统一设置，不手工逐个点。
- 不手写 `.meta`，让 Unity 自动生成 GUID。
- 若需要替换现役 Sprite，必须先确认 `AssetRegistry` 中 owner 与状态。

### 9.5 验收标准

- 所有 Sprite 在场景中叠合无偏移。
- 不出现透明边脏色或黑边。
- Pivot 统一。
- Sorting 后叠层顺序正确。
- 关闭材质 / Shader 特效后，基础 Sprite 仍可读。

---

## 10. Step 7：接入 `Ship.prefab` 多层视觉骨架

### 10.1 目的

把生产好的 Sprite 接入现役 `Ship.prefab` 主链，而不是手工在场景里临时拼一个新飞船。

### 10.2 现役权威

| 对象 | 权威入口 |
| --- | --- |
| `Ship.prefab` 结构 | `ShipPrefabRebuilder` |
| `BoostTrailRoot.prefab` 结构 | `BoostTrailPrefabCreator` |
| scene-only Bloom 绑定 | `ShipBoostTrailSceneBinder` |
| BoostTrail 材质贴图回填 | `MaterialTextureLinker` |
| 只读审计 | `ShipVfxValidator` |

### 10.3 需要产出

- 新 Sprite 已分配到对应 `SpriteRenderer` 或 `ShipJuiceSettingsSO` 字段。
- `Ship.prefab` 仍保持现役 `ShipVisual` 结构。
- 若新增层，必须先决定它属于现役哪个节点，还是需要正式扩展 prefab 结构。
- 若扩展结构，必须同步更新 `CanonicalSpec` / `AssetRegistry`。

### 10.4 推荐生产方式

- 不在 Scene 实例上长期覆盖 `ShipVisual` 子节点。
- 不让 Runtime fallback 自动找 Sprite 或自动修 Prefab。
- 若需要批量接入 Sprite，优先扩展 Editor 工具，且工具必须是显式 Apply。
- 接入后运行 `ShipVfxValidator`。

### 10.5 验收标准

- `Ship.prefab` 中 `ShipView._boostTrailView` 指向 nested `BoostTrailRoot`。
- `BoostTrailRoot.prefab` 内部结构未被 `ShipPrefabRebuilder` 越权改写。
- `BoostTrailView._boostBloomVolume` 在 prefab 中保持空引用，只由 scene binder 绑定。
- Debug 工具关闭时，正式 Runtime 链仍能工作。

---

## 11. Step 8：建立 Material / Shader 基础库

### 11.1 目的

把状态差异从“重画图片”转成“少量 Shader + 多个 Material 参数变体”。

### 11.2 推荐 Shader 分类

第一批只做少量通用 Shader：

| Shader | 用途 | 优先级 |
| --- | --- | --- |
| `ShipEnergyPulse` | Liquid / Core 能量脉冲 | 高 |
| `ShipHighlightFlash` | 受击闪白 / 高光闪烁 | 高 |
| `AdditiveGlow` | Aura / Muzzle / Engine glow | 高 |
| `BoostTrailMain` | 主 Boost Trail | 已有现役基础 |
| `BoostEnergyLayer` | Boost 能量噪声层 | 已有现役基础 |
| `DissolveOrOverheat` | 过热 / 破损 / 死亡预备 | 中 |
| `DistortionPulse` | Weaving / Overheat 空间扰动 | 中 |

### 11.3 推荐 Material 变体

```text
mat_ship_canary_solid_default
mat_ship_canary_liquid_default
mat_ship_canary_liquid_boost
mat_ship_canary_liquid_weaving
mat_ship_canary_liquid_overheat
mat_ship_canary_highlight_default
mat_ship_canary_highlight_hitflash
mat_ship_canary_core_default
mat_ship_canary_core_weaving
mat_ship_canary_core_overheat
mat_vfx_canary_muzzle_flash
mat_vfx_canary_weaving_aura
```

### 11.4 Shader / Material 分工

```text
Shader   = 渲染算法
Material = 参数资产
Runtime  = 状态驱动
```

#### 放进 Shader 的内容

- UV 流动。
- 噪声采样。
- Additive 混合。
- 溶解算法。
- 扭曲算法。
- 边缘发光计算。

#### 放进 Material 的内容

- 颜色。
- 亮度。
- 透明度。
- 主贴图。
- 噪声贴图。
- 流动速度。
- 发光强度。
- 扭曲强度。

### 11.5 推荐生产方式

- 初期优先 Shader Graph 或简单 HLSL，避免一次写复杂 Uber Shader。
- Shader 参数命名统一使用英文，例如 `_TintColor`、`_Intensity`、`_PulseSpeed`、`_DistortionStrength`。
- Runtime 单对象变化优先使用 Material 实例或 `MaterialPropertyBlock`，不要污染 shared material。
- `MaterialTextureLinker` 只维护现役 BoostTrail 材质贴图，不把它扩成全项目兜底工具。

### 11.6 验收标准

- 不为每个状态写独立 Shader。
- Material 变体能解释状态差异。
- Runtime 不修改 authored Material 资产。
- 关闭 Bloom 后，材质效果仍可读。
- Shader 出错时不会让飞船主体完全不可见。

---

## 12. Step 9：制作核心 VFX Prefab

### 12.1 目的

把高频战斗反馈做成可复用、可池化、可复位的 VFX Prefab。

### 12.2 第一批 VFX

| VFX | 目的 | 推荐实现 | MVP |
| --- | --- | --- | --- |
| `EngineIdleVFX` | 默认推进器微光 | Sprite / Particle | 是 |
| `BoostSustainVFX` | Boost 持续尾焰 | `BoostTrailRoot` 现役链 | 是 |
| `BoostBurstVFX` | Boost 起步爆发 | FlameCore + Bloom burst | 是 |
| `MuzzleFlashVFX` | 开火瞬间 | SpriteRenderer / ParticleSystem | 是 |
| `HitSparkVFX` | 受击火花 | Pooled ParticleSystem | 是 |
| `HitFlash` | 船体闪白 | `ShipHitVisuals` | 是 |
| `WeavingAuraVFX` | 编织态能量环 | Sprite + Additive Material + Tween | 是 |
| `OverheatWarningVFX` | 过热警告 | Spark + color pulse + vignette | 第二批 |
| `DeathExplosionVFX` | 死亡爆炸 | Pooled particles + fragments | 第二批 |

### 12.3 推荐生产方式

- 高频 VFX 必须对象池化。
- Sprite VFX 使用 spritesheet 或单张 additive sprite。
- 粒子 VFX 使用独立 Prefab，不在战斗中 `Instantiate / Destroy`。
- Trail VFX 回收时必须清空 Trail 状态。
- 每个 VFX Prefab 必须有清楚 owner：Runtime、Prefab Creator、Debug Preview 不能混在一起。

### 12.4 对象池复位清单

每个池化 VFX 回收时至少重置：

1. 运行时字段。
2. 事件订阅。
3. 动态组件。
4. Transform：position / rotation / scale。
5. 视觉状态：color / alpha / material parameters / trail / particle emission。

### 12.5 验收标准

- 重复播放 20 次没有颜色、alpha、scale、trail 残留。
- VFX 关闭后飞船主体仍然可读。
- 高频 VFX 不在战斗中 `Instantiate / Destroy`。
- 缺关键引用时有报错或 validator 抓到，不 silent no-op。

---

## 13. Step 10：接入 Runtime 状态驱动

### 13.1 目的

让飞船视觉由正式游戏状态驱动，而不是手动切图或 Debug 面板接管。

### 13.2 当前 Runtime 主链

```text
ShipStateController / ShipBoost / ShipHealth / ShipMotor
→ ShipView
→ ShipBoostVisuals / ShipHitVisuals / ShipDashVisuals / ShipVisualJuice
→ BoostTrailView / DashAfterImageSpawner
→ SpriteRenderer / Material / Trail / Particle / Bloom
```

### 13.3 状态到表现的推荐映射

| 输入事件 / 状态 | Runtime owner | 视觉输出 |
| --- | --- | --- |
| `ShipStateController.OnStateChanged` | `ShipView` | 分发状态变化 |
| Boost start / sustain / end | `ShipBoostVisuals` + `BoostTrailView` | Liquid swap、HDR tween、trail ramp、Bloom burst |
| `ShipHealth.OnDamageTaken` | `ShipHitVisuals` | 白闪、i-frame flicker、低血量 core pulse |
| `ShipMotor.OnSpeedChanged` | `ShipVisualJuice` | tilt、squash、stretch |
| Dash start | `ShipDashVisuals` | ghost、afterimage、i-frame visual |
| Weapon fire | 待正式接入 `CombatEvents.OnWeaponFired` | muzzle flash、core pulse、短促 recoil |
| Weaving enter / exit | 待正式接入 Weaving 状态事件 | aura、energy pulse、postprocess pulse |
| Overheat | 待正式接入 `HeatSystem` 事件 | overheat material、spark、vignette |

### 13.4 需要产出

- 状态事件源列表。
- 每个状态的 Runtime owner。
- 每个 owner 只做一件事。
- 若新增 Worker，更新 `CanonicalSpec` 与 `AssetRegistry`。

### 13.5 推荐生产方式

- 新视觉状态优先扩展现有 Worker，不直接让业务系统改 SpriteRenderer。
- `ShipHealth`、`HeatSystem`、`StarChartController` 只发事件或暴露状态，不直接操作视觉。
- 视觉参数放入 SO 或 Material，不 hardcode 在 MonoBehaviour 中。
- 不使用 `FindObjectOfType` / `GameObject.Find` 运行时查找。

### 13.6 验收标准

- Debug 关闭后，Play Mode 中状态变化能自动驱动画面。
- 游戏逻辑系统不直接持有具体 VFX 子节点引用。
- 缺引用时响亮失败。
- Runtime 不修资产、不回写 SO、不污染 shared material。

---

## 14. Step 11：加入 Animator / PrimeTween 过渡

### 14.1 目的

让状态变化有节奏，而不是硬切。

### 14.2 分工原则

| 类型 | 推荐实现 |
| --- | --- |
| 循环 Sprite frame | Animator |
| 爆炸序列 | Animator / ParticleSystem |
| 短促闪白 | PrimeTween |
| alpha 淡入淡出 | PrimeTween |
| scale pulse | PrimeTween |
| material intensity | PrimeTween / MaterialPropertyBlock |
| Boost ramp | PrimeTween |
| Aura breathing | PrimeTween 或 Shader 时间参数 |

### 14.3 不推荐做法

不要把所有组合状态塞进 Animator Controller：

```text
Normal
Boost
BoostFire
BoostHit
BoostWeaving
BoostWeavingHit
OverheatBoostFire
...
```

这会导致状态机爆炸。

### 14.4 推荐生产方式

- Animator 只负责真正的帧动画。
- 状态进入 / 退出用 PrimeTween 做短过渡。
- 所有 Tween 必须可取消、可复位，避免状态切换后残留。
- 新代码优先 UniTask / PrimeTween，不新增 Coroutine。

### 14.5 验收标准

- Boost 进入 / 退出有 100-200ms 过渡。
- HitFlash 短促明确，不拖泥带水。
- Weaving Aura 展开与收束可感知。
- Overheat 升温有渐进提示。
- 快速连续切状态不会残留 alpha / scale / material 参数。

---

## 15. Step 12：加入 Bloom / PostProcess / Camera Juice

### 15.1 目的

让飞船 VFX 融入整体画面，但不依赖 Bloom 掩盖基础资产问题。

### 15.2 推荐顺序

```text
先验证 Sprite 可读
再验证 Material / Shader
再加 Bloom
最后加 Camera / PostProcess pulse
```

### 15.3 推荐表现层

| 状态 | Bloom | PostProcess | Camera |
| --- | --- | --- | --- |
| `Normal` | 低 | 默认 | 无 |
| `Boost` | 短 burst + sustain glow | 轻微色彩增强 | 轻微 impulse |
| `Fire` | muzzle flash 短亮 | 无或极轻 | 小 recoil shake |
| `Hit` | 白闪辅助 | 轻微 vignette / chromatic | 短 shake + hitstop |
| `Weaving` | 紫蓝 aura glow | 轻微 chromatic / color shift | 低频脉冲 |
| `Overheat` | 橙红 pulse | vignette / heat tint | 紧张抖动 |
| `Death` | 强 burst | 短冲击 | 明确 shake |

### 15.4 需要产出

- `BoostBloomVolumeProfile` 现役 profile 维护。
- 状态 → 后处理参数表。
- Camera impulse 强度表。
- Bloom 开关前后截图对比。

### 15.5 推荐生产方式

- 不让每个 VFX 自己随便改全局 Volume。
- PostProcess 应有统一 owner 或明确场景级控制器。
- 先做状态视觉，再做全屏效果。
- Bloom 强度必须服务可读性，不让主体轮廓糊掉。

### 15.6 验收标准

- Bloom 开启后主体轮廓仍清楚。
- 不同状态的屏幕反馈层级不同。
- 反馈不遮挡敌人攻击和弹幕。
- PostProcess 不是永久污染，退出状态会恢复。

---

## 16. Step 13：建立测试场景与调试面板

### 16.1 目的

让每次新 Sprite / Material / VFX 都能在 2 分钟内进 Play Mode 验证。

### 16.2 推荐测试场景

```text
ShipVfxTestRoom
├── NeutralBackground
├── DarkBackground
├── BrightBackground
├── BulletReadabilityLayer
├── EnemyDummy
├── ProjectileDummy
├── Ship Instance
├── VFX Test Controls
└── PostProcess Toggle
```

### 16.3 推荐 Debug 能力

| 功能 | 说明 |
| --- | --- |
| `Preview Normal` | 切回默认视觉 |
| `Preview Boost` | 触发 Boost start / sustain / end |
| `Preview Fire` | 播放开火反馈 |
| `Preview Hit` | 播放受击反馈 |
| `Preview Weaving` | 进入 / 退出编织态 |
| `Preview Overheat` | 模拟升温与过热 |
| `Solo Layer` | 单独查看 Solid / Liquid / HL / Core / Back |
| `Toggle Bloom` | 开关 Bloom 对照 |
| `Switch Background` | 测试不同背景可读性 |

### 16.4 关键约束

- Debug 工具默认关闭。
- Debug 可以预览，但不得成为正式 Runtime owner。
- Debug 不在 `Update / LateUpdate` 中持续覆盖正式状态，除非明确进入“调试接管模式”。
- Debug 结束后必须能 reset preview。

### 16.5 验收标准

- 2 分钟内能验证一个新 Sprite / Material / VFX。
- 所有核心状态可一键预览。
- 可在暗 / 亮 / 中性背景下检查可读性。
- Debug 关闭后正式链路仍独立成立。

---

## 17. Step 14：资产注册、审计、文档固化

### 17.1 目的

防止 AI 生成资产、临时材质、实验 Shader 越积越乱。

### 17.2 需要维护的表

对于进入正式链路的资产，必须更新或补充：

- `ShipVFX_AssetRegistry.md`：现役资产、owner、路径、状态。
- `ShipVFX_CanonicalSpec.md`：如果新增节点、Worker、主链职责。
- `Implement_rules.md`：如果出现新的长期规则或踩坑。
- `ImplementationLog_YYYY-MM.md`：每次创建 / 修改 / 删除文件后记录。

### 17.3 状态分类

| 状态 | 含义 |
| --- | --- |
| `Live` | 现役 runtime / prefab / scene / tool 直接使用 |
| `Dormant` | 文件存在但不在现役链路，未来可能清理 |
| `Reference` | 参考 / 实验 / 上游逆向，不可当规范 |
| `Legacy` | 已被新主链替代，仅保留历史查证 |

### 17.4 推荐生产方式

- AI 生成的临时图先放 `Source / Concepts`，不要直接进入正式路径。
- 进入 `Sprites / Materials / Shaders` 的资产必须有用途和 owner。
- 删除 dormant 资产前做 GUID + 文本双重引用审计。
- 任何现役路径变更必须同步注册表。

### 17.5 验收标准

- 每个正式资产知道谁使用它。
- 每个状态知道对应哪些 Sprite / Material / VFX。
- 没有“这个材质到底谁在改”的模糊区域。
- 新增同类 VFX 时不需要重新考古。

---

## 18. Step 15：迭代扩展新状态 / 新皮肤 / 新飞船

### 18.1 目的

当第一轮 MVP 跑通后，扩展更多状态或新飞船时不要推翻主链。

### 18.2 新状态扩展流程

```text
1. 写玩家感受
2. 判断 Base / Overlay / Moment
3. 决定 Sprite / Material / VFX / PostProcess 实现层
4. 生产最少资产
5. 接入现役 Runtime owner
6. Play Mode 验证
7. 更新 AssetRegistry / ImplementationLog
```

### 18.3 新皮肤扩展流程

```text
1. 复用现有 ShipVisual 层级
2. 新增 SpriteSet / MaterialSet
3. 不新增第二套 Runtime 逻辑
4. 用数据选择皮肤，而不是复制 Ship.prefab 主链
5. 更新注册表
```

### 18.4 新飞船扩展流程

新飞船可以有不同资源，但应尽量复用：

- `ShipView` 协调模型。
- Worker 分工。
- BoostTrail VFX 栈。
- Material / Shader 基础库。
- TestRoom / Debug 预览能力。

只有当新飞船玩法状态确实不同，才新增 Worker 或 Runtime 入口。

### 18.5 验收标准

- 新状态不造成组合 Sprite 爆炸。
- 新皮肤不复制第二套主链。
- 新飞船复用已有调试与验证流程。
- 新资产有明确 Live / Reference / Dormant 状态。

---

## 19. 推荐 MVP 批次

### Batch 0：视觉目标与状态表

**产出**：视觉方向、状态矩阵、颜色规范、生产规则。  
**生产方式**：文档 + mood board + 参考分析。  
**完成标准**：知道第一批要画什么、不画什么、哪些靠 VFX。

### Batch 1：Normal 分层 Sprite

**产出**：Solid / Liquid / Highlight / Core / Back。  
**生产方式**：AI 概念图 + 手工拆层 + Unity 导入。  
**完成标准**：多层叠合可读，单独 Solid 可读。

### Batch 2：Prefab 接入

**产出**：Sprite 接入现役 `ShipVisual`。  
**生产方式**：通过现役 Prefab 权威工具或明确手动配置，不做 scene 长期 override。  
**完成标准**：`ShipVfxValidator` 通过，Debug 关闭后正常显示。

### Batch 3：Boost MVP

**产出**：Boost Liquid / Core / Back 变体，BoostTrail 材质贴图调优。  
**生产方式**：复用 `BoostTrailRoot`，调 Material / Shader / Tween。  
**完成标准**：按 Boost 时尾焰、能量、Bloom、速度感同步变化。

### Batch 4：Hit / Fire MVP

**产出**：HitFlash、HitSpark、MuzzleFlash、Core pulse。  
**生产方式**：Particle / Sprite VFX + `ShipHitVisuals` + Combat event 接入。  
**完成标准**：受击和开火反馈短促、清楚、不混淆。

### Batch 5：Weaving MVP

**产出**：Weaving Liquid / Core / Aura / Material / PostProcess pulse。  
**生产方式**：Sprite Aura + Additive Material + Tween / Shader pulse。  
**完成标准**：编织态与 Boost、Normal 明显不同。

### Batch 6：Overheat MVP

**产出**：Overheat 材质、火花、热量警告、vignette。  
**生产方式**：HeatSystem 事件 → 视觉 Worker / PostProcess owner。  
**完成标准**：不看 UI 也能感到危险升级。

### Batch 7：测试场景与固化

**产出**：ShipVfxTestRoom、Debug Preview、AssetRegistry 更新。  
**生产方式**：Editor / Play Mode 验证工具。  
**完成标准**：新资产 2 分钟内可验证，文档与注册表一致。

---

## 20. 每一步的“可继续下一步”检查表

| 步骤 | 可以继续的条件 |
| --- | --- |
| 视觉目标 | 玩家感受、色彩、禁止方向明确 |
| 状态表 | 每个状态都有实现层，不存在组合爆炸 |
| 分层结构 | 层职责清楚，符合现役 `ShipVisual` |
| 概念图 | 选定 source of truth，朝向明确 |
| Normal Sprite | 多层叠合无偏移，Solid 单独可读 |
| 状态 Sprite | 状态差异清楚，不重画组合图 |
| Unity 导入 | 尺寸 / pivot / alpha / import 设置统一 |
| Prefab 接入 | 现役主链无漂移，Validator 可通过 |
| Material / Shader | 参数变体清楚，不污染 shared asset |
| VFX Prefab | 池化复位完整，不残留状态 |
| Runtime 驱动 | 由事件驱动，不靠 Debug 接管 |
| Tween / Animator | 快速切状态无残留 |
| PostProcess | 增强表现但不破坏可读性 |
| TestRoom | 2 分钟内可验证新效果 |
| Registry | 资产 owner / path / status 明确 |

---

## 21. 常见错误与防御

### 21.1 错误：先写 Shader，再想状态

**问题**：Shader 很酷，但不知道服务哪个玩家状态。  
**防御**：先写状态矩阵，再决定 Shader 参数。

### 21.2 错误：每个状态画整船

**问题**：组合状态会爆炸。  
**防御**：Base + Overlay + Moment 分层。

### 21.3 错误：AI 每次生成不同角度

**问题**：状态图无法叠合。  
**防御**：先锁定 source of truth，再基于同一图拆层。

### 21.4 错误：在 Scene 实例上修到能看

**问题**：Prefab 正确但 Scene override 漂移，后续难排查。  
**防御**：回到 Prefab authority 和 Scene Binder。

### 21.5 错误：Debug 面板持续覆盖正式状态

**问题**：调试时正常，关闭 Debug 后坏。  
**防御**：Debug 默认关闭，只做 preview，不做 Runtime owner。

### 21.6 错误：运行时改 shared material

**问题**：污染所有实例或编辑器资产。  
**防御**：使用实例 Material 或 `MaterialPropertyBlock`。

### 21.7 错误：VFX 回池不重置 Trail / Color / Alpha

**问题**：下一次播放带脏状态。  
**防御**：严格执行对象池复位五项清单。

---

## 22. 最推荐的近期执行顺序

如果从现在开始推进，我建议按以下顺序：

```text
1. Batch 0：视觉目标与状态表
2. Batch 1：Normal 分层 Sprite
3. Batch 2：接入现役 ShipVisual
4. Batch 3：Boost MVP
5. Batch 4：Hit / Fire MVP
6. Batch 5：Weaving MVP
7. Batch 6：Overheat MVP
8. Batch 7：测试场景与资产注册固化
```

第一轮只追求一个目标：

```text
金丝雀号从“能移动的一张图”升级为“能根据玩家状态变化的多层视觉系统”。
```

不要一开始追求完整最终品质。先让它 work，再让它 right，最后让它 fast。
