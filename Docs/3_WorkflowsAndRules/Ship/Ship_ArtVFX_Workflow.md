# Ship Art / VFX Workflow

> **文档定位**：本文档是 Project Ark 金丝雀号飞船美术模块的细粒度生产计划。它假设执行者没有专业美术经验，因此每一步都写清楚要做什么、输出什么文件、文件规格是什么、怎么判断能不能进入下一步。  
> **适用对象**：主飞船 Sprite、Dodge/Boost/Fire/Hit/Weaving/Overheat 状态图、Albedo/Emission/Mask/Normal 等贴图、材质、Shader、VFX、Unity 导入、Prefab 接入与验证。  
> **不替代**：现役主链、owner、路径、状态判断仍以 `Docs/2_TechnicalDesign/Ship/ShipVFX_CanonicalSpec.md` 与 `Docs/2_TechnicalDesign/Ship/ShipVFX_AssetRegistry.md` 为准。本文档只告诉我们“怎么一步步生产资产”。

---

## 0. 先读：这份计划的使用方式

### 0.1 本文档解决什么问题

之前的计划更像路线图：知道要做 Sprite、Shader、VFX、Bloom，但对新手来说仍然会卡在“第一张图到底该画什么”“Dodge state 到底要几张图”“Normal 和 Albedo 是不是一回事”。

本版改成资产生产清单：

```text
先定义每张图的统一规格
再生产主飞船基础图
再逐个状态生产变体图
再生产材质需要的辅助贴图
最后才接入 Unity / Prefab / Runtime
```

### 0.2 工作原则

- **一张图只解决一个问题**：主体归主体，发光归发光，遮罩归遮罩，不要把所有效果画死在一张图里。
- **先做静态图，再做动态效果**：先确认静态 Sprite 可读，再上 Shader、VFX、Bloom。
- **先 Normal，再状态**：所有状态图都从 Normal 图复制修改，不重新随机生成。
- **先 Albedo，再 Emission/Mask/Normal**：先确定颜色与轮廓，再做发光和辅助贴图。
- **每一步都能被验收**：如果某一步不能明确判断好坏，就说明规格还不够细。

### 0.3 名词翻译

| 名词 | 新手解释 | 在本项目中的用途 |
| --- | --- | --- |
| `Sprite` | Unity 里显示的 2D 图片 | 飞船主体、残影、光环、火花 |
| `Albedo` | 不带发光的基础颜色图，也可以理解为“本体颜色” | 船体、金属、普通能量纹路 |
| `Emission` | 发光图，黑色不亮，彩色区域发光 | 核心、能量线、尾焰、光环 |
| `Mask` | 黑白控制图，白色代表要被效果影响，黑色代表不影响 | 控制闪白、溶解、过热、能量流动 |
| `Normal Map` | 伪造凹凸光照的蓝紫色贴图 | 可选；让船体有金属凹凸感 |
| `State` | 飞船当前视觉状态 | Normal、Dodge、Boost、Fire、Hit、Weaving、Overheat |
| `Layer` | 飞船被拆成的视觉层 | Solid、Liquid、Highlight、Core、Back、Aura、Outline |
| `Outline` | 轮廓/描边层，不等于 Highlight | 保证深色背景和高 Bloom 场景下仍能读清船体边界 |
| `Prefab` | Unity 中可复用的对象模板 | `Ship.prefab`、`BoostTrailRoot.prefab` |

### 0.4 参考项目优先级：Minishoot 为主轴，Galactic Glitch 为附录

本计划最后一版的参考优先级如下：

```text
主轴参考：Minishoot
附录参考：Galactic Glitch
```

原因：本轮目标不是复刻一整套复杂状态 Sprite 表，而是先把金丝雀号做成一个**可读、可动、可验证、能快速迭代**的 2D top-down 飞船。Minishoot 的玩家飞船实现更适合作为第一轮资产生产主轴：它用很少的核心 Sprite，加上 `Outline`、`Shape`、Lean/Dash 动画帧、TrailRenderer、ParticleSystem、Tween、音效和后处理，做出了清晰的移动手感。

已核对的 Minishoot 关键证据：

| 类别 | Minishoot 资产 / 实现 | 对本计划的约束 |
| --- | --- | --- |
| 主体 Sprite | `Player.png`、`__PlayerFull.png` | Normal 第一批应先做一张完整可读的主体图，而不是一开始拆成大量状态贴图 |
| 轮廓 / 形状材质 | `ShipPlayer.mat`、`ShipPlayerShape.mat`、`PlayerOutline.mat`、`SupershotPlayerOutline.png` | `Outline` / `Shape` 是主轴资产，不是可有可无的附属层；可读性优先于细节层数 |
| Dash 帧序列 | `PlayerDash1.png` - `PlayerDash5.png`、`PlayerDash.anim`、`PlayerDashHalf.anim` | Dodge/Dash 应优先准备短帧序列，而不是只做一张透明残影 |
| Lean 帧序列 | `PlayerLeanLeft1-3.png`、`PlayerLeanRight1-3.png`、`PlayerLean*.anim` | 移动 polish 应有左右倾斜状态；飞船转向反馈可以靠短帧和动画解决 |
| 附属视觉节点 | `EnergyBars`、`Weapons`、`SpiritDashTrail`、`SpiritDashParticles` | 能量条、武器点、尾迹、粒子应作为独立节点或 VFX，不要全部画死进主体图 |
| 运行时表现 | `PlayerView.Dash()`、`PlayerView.MovePolish()` | 手感来自 Sprite 帧 + Transform/Tween + Trail/Particle + SFX 的组合，不来自堆更多静态状态图 |

`Galactic Glitch` 仍有参考价值，但降级为 appendix / optional reference：

| 参考项目 | 现在的定位 | 可以参考什么 | 不再作为主轴的原因 |
| --- | --- | --- | --- |
| `Minishoot` | **主轴** | 主体轮廓、Outline/Shape、Lean、Dash、Trail、粒子、少量帧动画 | 更符合本轮 MVP：少资产、高可读、快验证 |
| `Galactic Glitch` | 附录 / 可选 | 多状态分层、PlayerSkin 状态映射、复杂 shader / material 参数 | 第一轮若照 GG 做，会过早进入多状态贴图表和复杂材质矩阵，拖慢可玩闭环 |

由此补充 7 条硬约束：

1. **Minishoot 主轴先于 GG 分层**：第一轮资产准备以 `Player / Outline / Shape / Dash / Lean / Trail / Particle` 为核心，不再以 GG 的 `Solid / Liquid / Highlight` 多状态表作为主生产模型。
2. **完整主体先于细分层**：先做一张完整、缩小后仍清楚的 `ship_canary_body_normal`；只有当 Unity 接入需要时，才拆出附加层。
3. **Outline 是必需品**：`Outline` / `Shape` 负责 gameplay readability，优先级高于复杂高光、法线、Bloom。
4. **Dash 必须有短帧序列意识**：参考 `PlayerDash1-5`，Dodge 第一轮至少准备 3-5 张 dash silhouette / smear 帧，不能只靠一张 ghost 拉伸。
5. **Lean 是移动手感资产**：参考 `PlayerLeanLeft/Right1-3`，左右倾斜帧是飞船移动 polish 的核心资产，应进入 Batch 1/2，而不是后期可选项。
6. **VFX 优先程序化组合**：Trail、SpiritDash、Spark、Muzzle、Aura 优先用小图 + ParticleSystem / TrailRenderer / 材质参数 / Tween 组合；不要把效果全部画进主 Sprite。
7. **GG 只在 appendix 中提供警戒和增强项**：GG 的状态映射、复杂材质、GrabGun/Healing/Secondary 禁误用规则仍保留，但不决定本轮必须产出的资产。

---

## 1. 全部图片的统一规格

### 1.1 画布尺寸

第一轮建议统一使用：

```text
Source working size: 1024 × 1024 px
Unity export size:   512 × 512 px
Preview check size:  128 × 128 px
```

解释：

- `1024 × 1024`：用于 AI 生图、手工清理、拆层，细节空间足够。
- `512 × 512`：导入 Unity 的默认正式 Sprite 尺寸，够清晰，不太浪费显存。
- `128 × 128`：模拟游戏中缩小后的可读性。如果 128px 看不清朝向，说明图失败。

### 1.2 是否需要“十寸”

游戏 Sprite 不按“十寸”这种印刷尺寸生产，应该按像素生产。  
如果工具要求填写画布尺寸，可以用：

```text
1024 × 1024 px
72 DPI
Transparent background
Square canvas
```

DPI 对 Unity 基本没有意义，Unity 只关心像素、Pixels Per Unit、Pivot、Import Settings。

### 1.3 方向与中心点

第一批统一约定：

```text
飞船鼻尖朝上
飞船中心在画布中心
Pivot = Center
尾焰从画布下方向外延展
```

如果后续项目控制逻辑确认飞船默认朝右，可以整体旋转 90°，但本工作流先用“朝上”作为新手最容易理解的方向。

### 1.4 边距

| 区域 | 要求 |
| --- | --- |
| 船体主体 | 占画布高度约 55%-70% |
| 左右留白 | 每侧至少 15% |
| 上方留白 | 至少 10%，防止鼻尖裁切 |
| 下方留白 | 至少 20%，给尾焰和 Boost 预留空间 |

### 1.5 透明背景

所有正式 Sprite 必须是：

```text
PNG
Transparent background
Straight alpha preferred
No white/black background baked into image
```

验收方式：把图放在黑、白、深蓝三种背景上看，不应该出现脏边、白边、黑边。

### 1.6 命名规则

统一格式：

```text
[type]_[object]_[layer]_[state]_[map].[ext]
```

示例：

```text
spr_ship_canary_solid_normal_albedo.png
spr_ship_canary_liquid_boost_emission.png
spr_ship_canary_highlight_hit_mask.png
spr_ship_canary_dodgeghost_dodge_albedo.png
tex_ship_canary_solid_normal_normal.png
```

字段解释：

| 字段 | 示例 | 含义 |
| --- | --- | --- |
| `type` | `spr` / `tex` / `mat` | Sprite、Texture、Material |
| `object` | `ship_canary` | 金丝雀号 |
| `layer` | `solid` / `liquid` / `core` | 飞船视觉层 |
| `state` | `normal` / `dodge` / `boost` | 状态 |
| `map` | `albedo` / `emission` / `mask` / `normal` / `outline` | 贴图用途 |

### 1.7 文件格式

| 用途 | 格式 | 说明 |
| --- | --- | --- |
| 正式 Sprite | `.png` | 透明背景 |
| Source 分层文件 | `.psd` / `.kra` / `.clip` | Photoshop/Krita/Clip Studio 可编辑文件 |
| AI 原始图 | `.png` / `.webp` | 放 Source，不直接进正式目录 |
| Unity 材质 | `.mat` | Unity 内创建 |
| Shader Graph | `.shadergraph` | Unity 内创建 |

### 1.8 颜色规格

第一轮建议：

| 类型 | 推荐颜色 | 用途 |
| --- | --- | --- |
| 船体暗部 | 深灰蓝、旧金属灰 | `Solid` 主体 |
| 船体亮部 | 暖灰、低饱和白 | 边缘高光 |
| 主能量 | 蓝紫、青蓝 | `Liquid` / `Core` |
| 编织能量 | 紫蓝 + 少量星图金 | `Weaving` |
| 过热警告 | 橙红 | `Overheat` |
| 受击闪白 | 白色 / 淡青白 | `Hit` |

### 1.9 Unity 导入默认值

| Import Setting | 推荐值 |
| --- | --- |
| Texture Type | `Sprite (2D and UI)` |
| Sprite Mode | `Single` |
| Pixels Per Unit | 跟当前 `Ship.prefab` 一致，不单独发明 |
| Mesh Type | `Full Rect` 起步 |
| Filter Mode | `Bilinear` |
| Compression | MVP 阶段 `None` 或高质量 |
| Generate Mip Maps | 关闭 |
| Alpha Is Transparency | 开启 |
| Pivot | `Center` |

### 1.10 SpriteAtlas / 导入一致性

参考 `Minishoot` 的简洁 Sprite 管线和 `Galactic Glitch` 的多层状态图，本项目第一轮必须建立导入一致性检查。

| 项目 | 要求 |
| --- | --- |
| PPU | 同一套飞船层必须完全一致，跟当前 `Ship.prefab` 现役 PPU 对齐 |
| Pivot | 所有 `ship_canary` 层使用 `Center`，不得单张偏移修图 |
| SpriteAtlas | 正式 Sprite 进入 `CanaryShip` atlas；VFX 小图进入 `CanaryVFX` atlas |
| Packing | 同一状态多层不可被不同压缩策略处理，否则叠合会出现边缘差 |
| Import Preset | 后续应固化为 Unity Import Preset 或 Editor 校验，而不是手工记忆 |

验收方式：随机抽取任意 3 个状态层叠合，切换黑/白/深蓝背景，不能出现抖动、偏移、脏边或压缩色块。

---

## 2. 第一大阶段：生产 Minishoot 主轴的 Normal 飞船资产

### 2.0 本阶段目标

本阶段只解决一个问题：

```text
按 Minishoot 的飞船实现思路，做出金丝雀号在 Normal 状态下可读、可动、可接入 Unity 的核心资产组。
```

本阶段不追求 GG 式完整多状态分层，不做 Boost、Hit、Weaving、Overheat 的专用贴图。第一轮重点是：

```text
一张完整主体
+ 独立 Outline / Shape
+ 核心/能量小件
+ 左右 Lean 短帧
+ Dash 短帧的准备规范
```

### 2.1 主飞船 Normal 的玩家感受

玩家看到 Normal 状态时应该感到：

```text
这是一艘脆弱但可靠的异星探测船，轮廓极清楚，运动时像 Minishoot 飞船一样轻、快、干净。
```

关键词：

- 小型。
- 清楚轮廓。
- 鼻尖方向明确。
- 可在 128px 下读出朝向。
- 主体细节克制，手感靠 lean / dash / trail / particle 增强。
- 不像战斗机那么军用，也不像魔法飞盘。

### 2.2 如果只参考 Minishoot，本轮必须准备什么资产

Minishoot 主轴下，第一轮必须准备的资产不是 GG 式 `Solid / Liquid / Highlight / Core / Back` 多状态分层表，而是下面这套更轻的 playable set：

| 编号 | 文件名 | 对应 Minishoot 参考 | 用途 | 必须做吗 |
| --- | --- | --- | --- | --- |
| `M-1` | `spr_ship_canary_body_normal_albedo.png` | `Player.png` / `__PlayerFull.png` | 完整主船体，Normal 状态默认 Sprite | 必须 |
| `M-2` | `spr_ship_canary_shape_normal_mask.png` | `ShipPlayerShape.mat` | 船体形状 / 填充遮罩，用于材质染色、受击、溶解或描边辅助 | 必须 |
| `M-3` | `spr_ship_canary_outline_normal_outline.png` | `PlayerOutline.mat` / `SupershotPlayerOutline.png` | 独立轮廓，保证深色背景、Bloom、弹幕中仍可读 | 必须 |
| `M-4` | `spr_ship_canary_core_normal_albedo.png` | `PlayerCrystal.png` / energy focus | 小型核心或能量焦点 | 建议 |
| `M-5` | `spr_ship_canary_energybar_left_normal_albedo.png` | `EnergyBars/EnergyBarLeft` | 左侧能量条 / 翼侧能量件 | 建议 |
| `M-6` | `spr_ship_canary_energybar_right_normal_albedo.png` | `EnergyBars/EnergyBarRight` | 右侧能量条 / 翼侧能量件 | 建议 |
| `M-7` | `spr_ship_canary_weapon_mount_normal_albedo.png` | `Weapons` | 武器挂点 / 枪口锚点，供 Fire VFX 对齐 | 建议 |
| `M-8` | `spr_ship_canary_shadow_normal_albedo.png` | `SpriteShadow` / `Shadows` | 软阴影或残影底座，可用程序化替代 | 可选 |

第一轮 Lean / Dash 准备：

| 编号 | 文件名 | 对应 Minishoot 参考 | 用途 | 必须做吗 |
| --- | --- | --- | --- | --- |
| `L-1` | `spr_ship_canary_lean_left_01.png` | `PlayerLeanLeft1.png` | 轻微左倾 | 必须 |
| `L-2` | `spr_ship_canary_lean_left_02.png` | `PlayerLeanLeft2.png` | 中度左倾 | 必须 |
| `L-3` | `spr_ship_canary_lean_left_03.png` | `PlayerLeanLeft3.png` | 强左倾 | 建议 |
| `L-4` | `spr_ship_canary_lean_right_01.png` | `PlayerLeanRight1.png` | 轻微右倾 | 必须 |
| `L-5` | `spr_ship_canary_lean_right_02.png` | `PlayerLeanRight2.png` | 中度右倾 | 必须 |
| `L-6` | `spr_ship_canary_lean_right_03.png` | `PlayerLeanRight3.png` | 强右倾 | 建议 |
| `D-1` | `spr_ship_canary_dash_01.png` | `PlayerDash1.png` | Dash 起手帧 / smear 初段 | 必须 |
| `D-2` | `spr_ship_canary_dash_02.png` | `PlayerDash2.png` | Dash 拉伸帧 | 必须 |
| `D-3` | `spr_ship_canary_dash_03.png` | `PlayerDash3.png` | Dash 最强形变 | 必须 |
| `D-4` | `spr_ship_canary_dash_04.png` | `PlayerDash4.png` | Dash 回收帧 | 建议 |
| `D-5` | `spr_ship_canary_dash_05.png` | `PlayerDash5.png` | Dash 结束帧 | 建议 |

第一轮程序化 VFX / 材质准备：

| 编号 | 资产 | 对应 Minishoot 参考 | 用途 | 必须做吗 |
| --- | --- | --- | --- | --- |
| `V-1` | `mat_ship_canary_body_default.mat` | `ShipPlayer.mat` | 主体材质 | 必须 |
| `V-2` | `mat_ship_canary_shape.mat` | `ShipPlayerShape.mat` | Shape / mask 材质 | 必须 |
| `V-3` | `mat_ship_canary_outline.mat` | `PlayerOutline.mat` | Outline 材质 | 必须 |
| `V-4` | `mat_ship_canary_dash.mat` | Dash sprite material | Dash 帧 / 残影材质 | 必须 |
| `V-5` | `prefab_ship_canary_trail_preview.prefab` | `SpiritDashTrail` | TrailRenderer 预览 prefab | 建议 |
| `V-6` | `prefab_ship_canary_dash_particles.prefab` | `SpiritDashParticles` | Dash 粒子预览 prefab | 建议 |

### 2.3 主体图：`body_normal_albedo` 需求

这张图是 Minishoot 主轴下最重要的一张图。

#### 它要表现什么

- 飞船完整轮廓。
- 鼻尖方向。
- 左右结构。
- 主体明暗。
- 可承载 `Outline`、`Shape`、Lean、Dash 的统一基础形。

#### 它不要表现什么

- 不要画强 Bloom。
- 不要画长尾焰。
- 不要把 Dash smear 画进 Normal。
- 不要把 Fire / Hit / Overheat 的状态效果画死。
- 不要把背景星空画进去。

#### 制作方式

1. 用 AI 或手绘得到一张完整飞船概念图。
2. 清理成简洁、清楚、低噪声的 top-down 主体。
3. 优先保留轮廓和朝向，减少细碎装甲纹理。
4. 确认缩小到 `128 × 128` 后仍能读清方向。
5. 导出为 `spr_ship_canary_body_normal_albedo.png`。

#### 验收标准

- 缩小到 `128 × 128` 仍能看出飞船朝向。
- 单独显示这张图时，飞船成立。
- 没有背景像素。
- 没有强发光画死在主体上。
- 后续复制修改成 Lean / Dash 帧时形体不崩。

### 2.4 Shape / Outline 需求

Minishoot 的关键不是“画很多层”，而是用 shape / outline 保证任何运动状态都能读清。

#### `shape_normal_mask` 要表现什么

- 船体整体填充形状。
- 可用于材质染色、受击闪白、溶解、低血量警告。
- 应比主体更干净，少细节。

#### `outline_normal_outline` 要表现什么

- 飞船外轮廓和关键负形。
- 在深色背景、Bloom 强光、弹幕密集时仍能读清边界。
- 可以被材质染成暗边或淡色描边。

#### 它们不要表现什么

- 不要承担 Normal 的所有细节。
- 不要替代 Dash 帧。
- 不要画成厚重 UI 描边。

#### 验收标准

- 关闭主体材质调色后，`Outline` 仍能说明船体边界。
- 关闭 `Outline` 后，`Shape` 仍能作为干净 mask 使用。
- `Body + Shape + Outline` 三者叠合无偏移。
- Bloom 打开时，`Outline` 不被能量层完全吞掉。

### 2.5 Core / EnergyBars / Weapon Mount 需求

Minishoot 的 `PlayerDash.anim` 和 Lean 动画会操作 `EnergyBars`、`Weapons` 等附属节点。本项目不需要照抄节点名，但需要准备同类锚点。

#### Core

- 小而明确。
- 是 Boost / Fire / Weaving / Overheat 后续状态的焦点。
- Normal 状态亮度克制。

#### EnergyBars

- 左右对称或近似对称。
- 可在 Dash / Lean 中旋转、偏移、缩放。
- 不要比主船体更抢眼。

#### Weapon Mount

- 标记开火点或武器挂点。
- 用于对齐 muzzle flash。
- 可以是一张小图，也可以只是 prefab 中的空节点。

#### 验收标准

- `Core / EnergyBars / Weapon Mount` 不依赖完整 GG 状态图也能工作。
- Lean / Dash 时它们可以跟随或被独立 tween。
- Fire VFX 能从 weapon mount 自然出现。

### 2.6 Lean 帧需求

Lean 是 Minishoot 飞船移动手感的关键资产。`PlayerView.MovePolish()` 会根据转向/strafe 强度播放 `LeanRight1-3` 或 `LeanLeft1-3`。

#### 它要表现什么

- 左右转向时的倾斜、压缩或翼侧重心变化。
- 保持同一艘船，不重新设计。
- 从轻微到强烈至少 2 档，推荐 3 档。

#### 制作方式

1. 复制 `body_normal_albedo`。
2. 保持画布、Pivot、整体中心不变。
3. 对船体做轻微左右倾斜、压缩、翼侧偏移或高光变化。
4. 导出 `lean_left_01-03` / `lean_right_01-03`。

#### 验收标准

- 连续切换 Normal → Lean1 → Lean2 → Lean3 不跳位。
- 左右帧不是简单镜像也可以，但朝向必须一致。
- 快速输入左右时不会闪成不同飞船。
- 128px 下能感觉到转向，但不会遮挡 gameplay。

### 2.7 Dash 帧需求

Dash 应参考 `PlayerDash1-5`：短、快、形变明确，配合粒子/Trail，而不是只生成一张 ghost。

#### 它要表现什么

- Dash 起手压缩。
- 中段拉伸 / smear。
- 结束回收。
- 可以带一点淡色能量边，但不画持续 Boost 尾焰。

#### 制作方式

1. 复制 `body_normal_albedo`。
2. 制作 3-5 张短帧：起手、拉伸、最强形变、回收、结束。
3. 所有帧画布、Pivot、主体中心一致。
4. 可额外准备 `dash_shape_mask`，用于残影或 dissolve。

#### 验收标准

- 0.15-0.35 秒内播放完整 Dash 仍能读清方向。
- Dash 和 Boost 一眼不同：Dash 是短形变，Boost 是持续推进。
- 连续 Dash 不残留 alpha / scale / color。
- Dash 粒子与 Trail 是辅助，不是主体可读性的唯一来源。

### 2.8 本阶段完成标准

只有满足以下条件，才进入 Boost / Fire / Hit / Weaving / Overheat 状态生产：

- `Body` 单独可读。
- `Body + Shape + Outline + Core` 叠合无偏移。
- Lean 左右至少各 2 档可播放。
- Dash 至少 3 帧可播放。
- 所有图尺寸一致。
- 所有图 Pivot 一致。
- 所有图透明边干净。
- `128 × 128` 缩略预览能看出朝向。
- 在 Unity 中可以用 Animator / Sprite swap / tween 快速切 `Idle / Lean / Dash`。

---

## 3. 第二大阶段：生产 Dodge / Dash State

### 3.0 Dodge / Dash State 的定位

在 Minishoot 主轴下，Dodge 的核心不是一张透明 ghost，而是一套短促的 Dash 帧 + 运行时 squash / trail / particle / audio。

| 状态 | 玩家感受 | 视觉重点 |
| --- | --- | --- |
| `Dodge / Dash` | 瞬间闪开、短促、轻盈 | `dash_01-05` 帧序列、短残影、TrailRenderer、粒子 |
| `Boost` | 持续推进、速度增强 | 持续尾迹、核心/喷口持续增强 |

### 3.1 Dodge / Dash 必须产出的图

如果 Batch 1 已经完成 `D-1` 到 `D-5`，本阶段只需要补齐材质、mask 和运行时预览。

| 编号 | 文件名 | 用途 | 必须做吗 |
| --- | --- | --- | --- |
| `2-A` | `spr_ship_canary_dash_01.png` | Dash 起手帧 | 必须 |
| `2-B` | `spr_ship_canary_dash_02.png` | Dash 拉伸帧 | 必须 |
| `2-C` | `spr_ship_canary_dash_03.png` | Dash 最强形变帧 | 必须 |
| `2-D` | `spr_ship_canary_dash_04.png` | Dash 回收帧 | 建议 |
| `2-E` | `spr_ship_canary_dash_05.png` | Dash 结束帧 | 建议 |
| `2-F` | `spr_ship_canary_dash_shape_mask.png` | Dash 残影 / dissolve mask | 建议 |
| `2-G` | `spr_vfx_canary_dash_streak_01.png` | 小型速度线 | 可选 |

### 3.2 Dash 帧制作要求

#### 它要表现什么

- 起手压缩。
- 中段拉伸。
- 最高速 smear。
- 结束回收。
- 飞船仍然是同一艘金丝雀号。

#### 它不要表现什么

- 不要比主飞船更实。
- 不要做成长尾焰。
- 不要做成持续 Boost。
- 不要完全依赖半透明 ghost 表达闪避。

### 3.3 Dodge Runtime 表现需求

参考 Minishoot `PlayerView.Dash()`：

```text
Dodge start:
  播放 dash_01 → dash_03 短帧
  主船 / shadow 做 0.1-0.3s squash 或 scale pulse
  TrailRenderer 开启短时间 emitting
  Dash particles 播放一次
Dodge recover:
  播放 dash_04 → dash_05 或直接回 Idle
  Trail / particles 停止或自然淡出
  Transform / color / alpha 全部复位
```

### 3.4 Dodge 验收标准

- 按下 Dodge 的瞬间，玩家能感觉“短促闪开”。
- Dodge 和 Boost 一眼不同。
- Dash 帧播放时不跳位。
- Dash Ghost / Trail 不遮挡子弹和敌人。
- 快速连续 Dodge 不残留 alpha / scale / color。
- Debug 关闭后，正式 Runtime 仍由 `ShipDashVisuals` / `DashAfterImageSpawner` 驱动。

---

## 4. 第三大阶段：生产 Boost State

### 4.0 Boost State 的定位

Boost 是持续推进，不是短闪。

玩家感受：

```text
飞船引擎真正启动，能量从核心流向尾部，速度持续上升。
```

### 4.1 Boost 必须产出的图

| 编号 | 文件名 | 用途 | 必须做吗 |
| --- | --- | --- | --- |
| `3-A` | `spr_ship_canary_liquid_boost_albedo.png` | Boost 能量纹路底色 | 必须 |
| `3-B` | `spr_ship_canary_liquid_boost_emission.png` | Boost 能量发光 | 必须 |
| `3-C` | `spr_ship_canary_core_boost_emission.png` | Boost 核心增强 | 必须 |
| `3-D` | `spr_ship_canary_back_boost_albedo.png` | Boost 喷口增强 | 建议 |
| `3-E` | `tex_boost_trail_main_albedo.png` | 主尾迹贴图 | 已有可调 |
| `3-F` | `tex_boost_trail_noise_mask.png` | 尾迹噪声/流动遮罩 | 建议 |

### 4.2 Boost 图像需求

#### `liquid_boost_albedo`

- 基于 `liquid_normal_albedo` 修改。
- 颜色更亮、更偏青蓝。
- 纹路可以略微变粗。
- 不要覆盖主体轮廓。

#### `liquid_boost_emission`

- 发光区域和 `liquid_boost_albedo` 对齐。
- 比 Normal emission 明显更亮。
- 保持能量从核心向尾部流动的方向感。

#### `core_boost_emission`

- 核心亮度明显增强。
- 可以增加内圈或脉冲环。
- 不要做成爆炸。

#### `back_boost_albedo`

- 喷口结构更亮或展开。
- 可以加入小型蓝白热区。
- 不画长尾焰，长尾焰交给 `BoostTrailRoot`。

### 4.3 Boost 运行表现需求

```text
Boost start:
  Core emission 0.12s 内冲高
  Liquid emission 增强
  BoostTrailRoot FlameCore burst
  Bloom 短促增强
Boost sustain:
  Trail 保持
  Liquid/Core 轻微 pulse
Boost end:
  Trail fade
  Core/Liquid 回到 Normal
```

### 4.4 Boost 验收标准

- Boost 和 Dodge 一眼不同：Boost 是持续推进，Dodge 是短残影。
- 不看 UI 也能感到速度提高。
- 关闭 Bloom 后仍能看出 Boost 状态。
- 结束 Boost 后所有强度回到 Normal。

---

## 5. 第四大阶段：生产 Fire State

### 5.0 Fire State 的定位

Fire 是开火瞬间反馈，不应该替换整艘飞船。

玩家感受：

```text
核心供能，武器吐出短促亮光，飞船有一点反冲感。
```

### 5.1 Fire 必须产出的图

| 编号 | 文件名 | 用途 | 必须做吗 |
| --- | --- | --- | --- |
| `4-A` | `spr_ship_canary_core_fire_emission.png` | 开火时核心脉冲 | 必须 |
| `4-B` | `spr_vfx_canary_muzzle_flash_01.png` | 枪口闪光 | 必须 |
| `4-C` | `spr_vfx_canary_muzzle_flash_mask.png` | 枪口闪光遮罩 | 建议 |
| `4-D` | `spr_vfx_canary_muzzle_spark_01.png` | 小火花 | 可选 |

### 5.2 Fire 图像需求

#### `core_fire_emission`

- 基于 `core_normal_albedo` 或 `core_normal_emission`。
- 亮度短促增强。
- 可以加入向武器方向的小能量线。
- 不要和 Weaving 的持续大光环混淆。

#### `muzzle_flash_01`

- 形状短、尖、亮。
- 颜色可用淡蓝白或武器家族色。
- 背景透明。
- 中心亮，边缘透明。

### 5.3 Fire 运行表现需求

```text
Weapon fired:
  MuzzleFlash 播放 0.05-0.10s
  Core emission pulse 0.08-0.15s
  飞船轻微 recoil / squash
  可选小火花
```

### 5.4 Fire 验收标准

- 每次开火都有短促反馈。
- 连射时不会把屏幕刷白。
- Fire 不改变飞船主体轮廓。
- Fire 和 Hit 不混淆。

---

## 6. 第五大阶段：生产 Hit State

### 6.0 Hit State 的定位

Hit 是受击反馈，要短、明确、危险。

玩家感受：

```text
我被打中了，飞船受到了冲击，但还没有进入死亡状态。
```

### 6.1 Hit 必须产出的图

| 编号 | 文件名 | 用途 | 必须做吗 |
| --- | --- | --- | --- |
| `5-A` | `spr_ship_canary_highlight_hit_mask.png` | 船体闪白遮罩 | 必须 |
| `5-B` | `spr_vfx_canary_hit_spark_01.png` | 受击火花 1 | 必须 |
| `5-C` | `spr_vfx_canary_hit_spark_02.png` | 受击火花 2 | 建议 |
| `5-D` | `spr_ship_canary_core_lowhealth_emission.png` | 低血量核心警告 | 第二批 |

### 6.2 Hit 图像需求

#### `highlight_hit_mask`

- 白色区域是受击闪白区域。
- 优先覆盖 `Solid` 外缘、翼尖、核心附近。
- 不要把整张图填满纯白。

#### `hit_spark_01 / 02`

- 小而亮。
- 方向可以有尖刺感。
- 颜色可用白、黄、淡蓝。
- 单个火花不超过飞船长度的 25%。

### 6.3 Hit 运行表现需求

```text
DamageTaken:
  ShipHitVisuals 触发 0.06-0.12s 白闪
  生成 3-8 个 HitSpark
  Camera impulse / HitStop
  i-frame 时可轻微闪烁
```

### 6.4 Hit 验收标准

- 受击瞬间足够明确。
- 不是持续红色警告，那属于 Overheat / LowHealth。
- 连续受击不会残留白闪。
- 火花使用对象池，回收时重置颜色、alpha、scale、particle emission。

---

## 7. 第六大阶段：生产 Weaving State

### 7.0 Weaving State 的定位

Weaving 是星图编织态，应该比 Boost 更神秘、更仪式化。

玩家感受：

```text
飞船正在和星图结构连接，能量从核心向外展开，世界进入短暂的编织状态。
```

### 7.1 Weaving 必须产出的图

| 编号 | 文件名 | 用途 | 必须做吗 |
| --- | --- | --- | --- |
| `6-A` | `spr_ship_canary_liquid_weaving_albedo.png` | 编织态能量纹路 | 必须 |
| `6-B` | `spr_ship_canary_liquid_weaving_emission.png` | 编织态发光纹路 | 必须 |
| `6-C` | `spr_ship_canary_core_weaving_emission.png` | 核心展开光 | 必须 |
| `6-D` | `spr_ship_canary_aura_weaving_emission.png` | 外圈光环 | 必须 |
| `6-E` | `tex_ship_canary_weaving_ring_mask.png` | 光环遮罩 | 建议 |
| `6-F` | `tex_ship_canary_weaving_noise_mask.png` | 能量流动噪声 | 建议 |

### 7.2 Weaving 图像需求

#### `liquid_weaving_albedo / emission`

- 基于 Normal Liquid 修改。
- 颜色偏紫蓝，可以加入少量星图金。
- 纹路更像符号、线路、星图轨迹。
- 不要像 Boost 那样只强调尾部推进。

#### `core_weaving_emission`

- 核心像被打开。
- 可以有圆环、星点、符号感。
- 亮度高于 Normal，但不必像爆炸。

#### `aura_weaving_emission`

- 飞船外部的能量环。
- 面积可以比飞船大，但透明度要低。
- 不遮挡敌人和弹幕。

### 7.3 Weaving 运行表现需求

```text
Weaving enter:
  Aura 从 0 放大到目标大小
  Core emission pulse
  Liquid 切到 weaving 材质/贴图
  轻微 postprocess pulse
Weaving sustain:
  Aura 慢速呼吸
  Liquid UV / emission 轻微流动
Weaving exit:
  Aura 收束或淡出
  Liquid/Core 回 Normal
```

### 7.4 Weaving 验收标准

- Weaving 和 Boost 明显不同。
- Weaving 有“星图连接”的感觉。
- 光环不遮挡 gameplay。
- 退出后没有后处理残留。

---

## 8. 第七大阶段：生产 Overheat State

### 8.0 Overheat State 的定位

Overheat 是热量危险提示，不是普通受击。

玩家感受：

```text
飞船系统正在过载，如果继续输出会付出代价。
```

### 8.1 Overheat 必须产出的图

| 编号 | 文件名 | 用途 | 必须做吗 |
| --- | --- | --- | --- |
| `7-A` | `spr_ship_canary_liquid_overheat_albedo.png` | 过热能量纹路 | 必须 |
| `7-B` | `spr_ship_canary_liquid_overheat_emission.png` | 橙红过热发光 | 必须 |
| `7-C` | `spr_ship_canary_core_overheat_emission.png` | 核心过载 | 必须 |
| `7-D` | `tex_ship_canary_overheat_noise_mask.png` | 热扰动噪声 | 建议 |
| `7-E` | `spr_vfx_canary_overheat_spark_01.png` | 过热火花 | 建议 |

### 8.2 Overheat 图像需求

- 颜色从蓝紫转为橙红。
- 核心区域最危险。
- 纹路可以不稳定、断裂、抖动感。
- 不要做成 Hit 的白闪。
- 不要让整艘船常驻纯红，避免视觉疲劳。

### 8.3 Overheat 运行表现需求

```text
Heat rising:
  Liquid 逐渐偏橙
  Core pulse 频率提高
Overheat start:
  橙红 emission burst
  火花出现
  轻微 vignette / camera tension
Overheat recover:
  颜色从橙红回到蓝紫
  火花停止
```

### 8.4 Overheat 验收标准

- 不看 UI 也能感到危险正在升高。
- Overheat 和 Hit 不混淆。
- 恢复后颜色、发光、火花全部复位。
- 不污染 authored Material / ScriptableObject。

---

## 9. 第八大阶段：Unity 接入

### 9.1 目录放置

Minishoot 主轴下，主飞船资产建议：

```text
Assets/_Art/Ship/Canary/
├── Source/
│   ├── Concepts/
│   ├── Layered/
│   └── AI_Raw/
├── Sprites/
│   ├── Body/
│   ├── Shape/
│   ├── Outline/
│   ├── Core/
│   ├── EnergyBars/
│   ├── WeaponMount/
│   ├── Lean/
│   └── Dash/
├── Textures/
│   ├── Masks/
│   ├── Emission/
│   └── Noise/
├── Materials/
└── Shaders/
```

BoostTrail 仍遵守现役路径：

```text
Assets/_Art/VFX/BoostTrail/
├── Materials/
├── Shaders/
└── Textures/
```

### 9.2 导入检查清单

每张 Sprite 导入后检查：

- Texture Type 是 `Sprite (2D and UI)`。
- Sprite Mode 是 `Single`。
- Pivot 是 `Center`。
- Lean / Dash / Body 的尺寸、PPU、Pivot 完全一致。
- Alpha 边缘干净。
- `Body + Shape + Outline + Core` 叠合无偏移。
- Dash 帧 0.15-0.35 秒播放时不跳位。

### 9.3 接入现役节点

| 资产 | 接入节点 / 运行时用途 |
| --- | --- |
| `body_normal` | `Ship_Sprite_Solid` 或 Canary 预览 prefab 的主 SpriteRenderer |
| `shape_mask` | 作为材质 mask / 独立 Shape Renderer，先在预览 prefab 验证 |
| `outline` | 独立 Outline Renderer；若要进现役 `Ship.prefab`，先更新 `CanonicalSpec` / `AssetRegistry` |
| `core` | `Ship_Sprite_Core` 或独立核心节点 |
| `energybar_left/right` | 独立子节点，供 Lean / Dash 动画或 tween 操作 |
| `weapon_mount` | 空节点或小 Sprite，用于对齐 Fire / Muzzle VFX |
| `lean_left/right_*` | Animator Sprite swap 或 `ShipVisualJuice` 风格运行时切换 |
| `dash_01-05` | `ShipDashVisuals` / Dash 预览 Animator / afterimage source |

### 9.4 接入约束

- 不在 Scene 实例上长期修 `ShipVisual`。
- 不新增第二套正式飞船视觉根节点；若需要验证，先做 `Reference` / preview prefab。
- 不让 Runtime fallback 自动修资产。
- 若要新增 `Outline`、`Shape`、`EnergyBars`、`WeaponMount` 等节点进入正式主链，先更新 `CanonicalSpec` / `AssetRegistry`。
- Debug 工具只 preview，不接管正式链。

---

## 10. 第九大阶段：Material / Shader 生产

### 10.1 第一批 Material（Minishoot 主轴）

| 编号 | Material 名 | 使用贴图 / Renderer | 对应 Minishoot 参考 |
| --- | --- | --- | --- |
| `M-1` | `mat_ship_canary_body_default` | `body_normal_albedo` | `ShipPlayer.mat` |
| `M-2` | `mat_ship_canary_shape` | `shape_normal_mask` | `ShipPlayerShape.mat` |
| `M-3` | `mat_ship_canary_outline` | `outline_normal_outline` | `PlayerOutline.mat` |
| `M-4` | `mat_ship_canary_core_default` | `core_normal_albedo` | `PlayerCrystal` / energy focus |
| `M-5` | `mat_ship_canary_dash` | `dash_01-05 + dash_shape_mask` | `PlayerDash` frames |
| `M-6` | `mat_ship_canary_trail` | TrailRenderer material | `SpiritDashTrail` |
| `M-7` | `mat_vfx_canary_dash_particles` | Dash particle sprite / small texture | `SpiritDashParticles` |
| `M-8` | `mat_vfx_canary_muzzle_flash` | `muzzle_flash_01` | Minishoot-style short feedback |

### 10.2 第一批 Shader

第一轮只需要少量通用 Shader：

| Shader | 用途 |
| --- | --- |
| `ShipBodyDefault` | 主体 Sprite，可先用 URP 2D Lit / Sprite Unlit 替代 |
| `ShipShapeMask` | Shape / mask 染色、受击、溶解预备 |
| `ShipOutline` | Outline 层，保证可读性 |
| `AdditiveGlow` | Dash particles / Muzzle / Spark / Aura |
| `DissolveFade` | Dash ghost / Teleport / Death 预备 |
| `BoostTrailMain` | 现役 BoostTrail |

### 10.3 材质与 Shader 分工

```text
Shader = 算法
Material = 参数
Sprite/Texture = 图像内容
Runtime = 什么时候切换、什么时候 tween
```

运行时不要修改 authored Material。单对象变化使用 Material 实例或 `MaterialPropertyBlock`。

### 10.4 材质参数矩阵

Minishoot 主轴下，第一轮参数矩阵应围绕可读性和运动反馈，而不是复杂状态贴图表：

| 状态 / 材质 | 必填参数 | 说明 |
| --- | --- | --- |
| `mat_ship_canary_body_default` | `tint`、`brightness` | Normal 主体克制、清楚 |
| `mat_ship_canary_shape` | `shapeTint`、`hitFlashAmount`、`dissolveAmount` | Shape 是运行时效果的主要 mask |
| `mat_ship_canary_outline` | `outlineColor`、`outlineAlpha`、`outlineWidth` | Outline 保证 gameplay readability |
| `mat_ship_canary_dash` | `alpha`、`stretchTint`、`fadeDuration` | Dash 必须短、轻、可复位 |
| `mat_ship_canary_trail` | `trailColor`、`lifetime`、`widthCurve` | Trail 是 Dash/Boost 辅助，不替代主体帧 |
| `mat_vfx_canary_muzzle_flash` | `lifetime`、`additiveIntensity`、`colorFamily` | 连射时不能刷白屏幕 |

这些参数后续应进入 `ShipVFX_AssetRegistry` 或专门的 VFX tuning 表。Runtime 只通过 `MaterialPropertyBlock` 或实例材质写运行时值，不写回 shared Material。

---

## 11. 第十大阶段：VFX Prefab 生产

### 11.1 第一批 VFX Prefab

| 编号 | Prefab | 需要的图 | 对象池 |
| --- | --- | --- | --- |
| `VFX-1` | `DodgeGhostVFX` | `dodgeghost_dodge_albedo + mask` | 是 |
| `VFX-2` | `MuzzleFlashVFX` | `muzzle_flash_01` | 是 |
| `VFX-3` | `HitSparkVFX` | `hit_spark_01/02` | 是 |
| `VFX-4` | `WeavingAuraVFX` | `aura_weaving_emission` | 可池化 |
| `VFX-5` | `OverheatSparkVFX` | `overheat_spark_01` | 是 |
| `VFX-6` | `BoostTrailRoot` | 现役 BoostTrail 贴图 | 已有现役链 |

### 11.2 程序化 VFX 资产补充

参考 `Galactic Glitch` 的 `vfx_noise_*`、`vfx_glow_ring`、`LMG_muzzle_flash_*`，以及 `Minishoot` 的 `CFX` glow/ring/spark 材质，本项目第一轮允许用程序化 VFX 降低手绘压力。

| 资产 | 推荐实现 | 需要记录的内容 |
| --- | --- | --- |
| Ring / Aura | 小型 ring 纹理 + Additive 材质 + scale/pulse tween | 半径、alpha、pulse 曲线、遮挡风险 |
| Spark | 1-2 张小火花 Sprite + ParticleSystem | 生命周期、速度、颜色、回池复位 |
| Muzzle Flash | 扇形/尖形 Sprite + Additive 材质 | lifetime、颜色族、连射亮度上限 |
| Trail Noise | 灰度噪声图 + UV scroll | scroll speed、dissolve、tiling |
| Outline | 独立 outline Sprite 或 outline 材质 | 厚度、颜色、是否受状态染色 |

### 11.3 对象池复位清单

每个 VFX 回池时必须重置：

1. 运行时字段。
2. 事件订阅。
3. 动态组件。
4. Transform：position / rotation / scale。
5. 视觉状态：color / alpha / material parameters / trail / particle emission。

---

## 12. 第十一大阶段：测试场景与验收

### 12.1 必须建立的检查方式

即使没有正式测试场景，也要能做到：

```text
一键显示 Normal
一键显示 Dodge
一键显示 Boost
一键显示 Fire
一键显示 Hit
一键显示 Weaving
一键显示 Overheat
一键切换黑/白/深蓝背景
一键关闭 Bloom
```

### 12.2 新手验收流程

每生产一张图，都按这个流程检查：

1. 放到黑色背景上看。
2. 放到白色背景上看。
3. 缩小到 `128 × 128` 看。
4. 和其他层叠起来看。
5. 在 Unity 中看。
6. 关闭 Bloom 看。
7. 打开 Bloom 看。
8. 快速切状态 10 次，看有没有残留。

### 12.3 状态最终验收表

| 状态 | 必须证明什么 |
| --- | --- |
| `Normal` | 飞船主体清楚，朝向清楚 |
| `Dodge` | 短促残影，不像 Boost |
| `Boost` | 持续推进，不像 Dodge |
| `Fire` | 开火短反馈，不改主体 |
| `Hit` | 受击明确，不像 Overheat |
| `Weaving` | 星图感、仪式感，和 Boost 不同 |
| `Overheat` | 危险升温，不只是红色 UI |

---

## 13. 推荐执行顺序

### Batch 0：Minishoot 飞船实现反查与资产映射

产出：

```text
Minishoot Player / __PlayerFull 主体参考核对
ShipPlayer / ShipPlayerShape / PlayerOutline 材质参考核对
PlayerDash1-5 / PlayerDash.anim 参考核对
PlayerLeanLeft/Right1-3 / Lean 动画参考核对
SpiritDashTrail / SpiritDashParticles / EnergyBars / Weapons 节点参考核对
Galactic Glitch 附录参考归档
```

完成标准：每个 Minishoot 飞船资产都知道它承担的是主体、outline、shape、lean、dash、trail、particle 还是附属节点；GG 资产只进入 appendix，不决定本轮主生产清单。

### Batch 1：只做 Minishoot 主轴 Normal + Lean + Dash 基础资产

产出：

```text
spr_ship_canary_body_normal_albedo.png
spr_ship_canary_shape_normal_mask.png
spr_ship_canary_outline_normal_outline.png
spr_ship_canary_core_normal_albedo.png
spr_ship_canary_energybar_left_normal_albedo.png
spr_ship_canary_energybar_right_normal_albedo.png
spr_ship_canary_lean_left_01.png
spr_ship_canary_lean_left_02.png
spr_ship_canary_lean_right_01.png
spr_ship_canary_lean_right_02.png
spr_ship_canary_dash_01.png
spr_ship_canary_dash_02.png
spr_ship_canary_dash_03.png
```

完成标准：主体单独可读；`Body + Shape + Outline + Core` 叠合无偏移；Lean 左右至少各 2 档；Dash 至少 3 帧；128px 能读清朝向。

### Batch 2：做 Dodge / Dash 可玩闭环

产出：

```text
spr_ship_canary_dash_04.png
spr_ship_canary_dash_05.png
spr_ship_canary_dash_shape_mask.png
mat_ship_canary_dash.mat
prefab_ship_canary_trail_preview.prefab
prefab_ship_canary_dash_particles.prefab
```

完成标准：能在 Play Mode 中看到短促 Dash 帧 + trail / particles；连续触发不残留 alpha、scale、color。

### Batch 3：做 Boost

产出：

```text
spr_ship_canary_core_boost_emission.png
spr_ship_canary_energybar_boost_emission.png
spr_ship_canary_engine_boost_albedo.png
```

完成标准：Boost 启停有持续推进感；仍以现役 `BoostTrailRoot` 为主要尾迹，不把长尾焰画死进主 Sprite。

### Batch 4：做 Fire / Hit

产出：

```text
spr_ship_canary_core_fire_emission.png
spr_vfx_canary_muzzle_flash_01.png
spr_ship_canary_shape_hit_mask.png
spr_vfx_canary_hit_spark_01.png
```

完成标准：开火和受击都短促、清楚、不混淆；Fire 从 `weapon_mount` 对齐生成。

### Batch 5：做 Weaving

产出：

```text
spr_ship_canary_core_weaving_emission.png
spr_ship_canary_aura_weaving_emission.png
tex_ship_canary_weaving_ring_mask.png
tex_ship_canary_weaving_noise_mask.png
```

完成标准：编织态有星图连接感；优先用 aura / ring / noise 程序化组合，不重画整艘船。

### Batch 6：做 Overheat

产出：

```text
spr_ship_canary_core_overheat_emission.png
spr_ship_canary_shape_overheat_mask.png
tex_ship_canary_overheat_noise_mask.png
spr_vfx_canary_overheat_spark_01.png
```

完成标准：不看 UI 也知道热量危险；恢复后颜色、发光、火花全部复位。

### Batch 7：固化 Unity 接入和注册表

产出：

```text
Unity Import Settings 统一
Canary preview prefab 或 Ship.prefab 接入方案
Animator / Sprite swap / tween 验证
VFX Prefab 接入
ShipVFX_AssetRegistry 更新
ImplementationLog 更新
Material 参数矩阵更新
SpriteAtlas / Import Preset 检查
```

完成标准：Debug 关闭后，正式 Runtime 链路仍能驱动 Idle / Lean / Dash / Boost / Fire / Hit / Weaving / Overheat；导入设置、材质参数、AssetRegistry 三者一致。

---

## 14. 不要做的事

- 不要一开始就追求最终品质。
- 不要每个状态都重新画整艘船。
- 不要让 AI 每次随机生成不同角度的飞船。
- 不要把发光、尾焰、背景全部画死在主 Sprite 里。
- 不要运行时修改 shared Material。
- 不要新增 `Boost+Hit+Weaving+Overheat` 这种组合状态图。
- 不要在 Scene 实例上长期修到“看起来能用”。
- 不要让 Debug 面板成为正式 Runtime owner。
- 不要把 `Galactic Glitch` 的某个状态图脱离原状态语境直接复用；GG 现在只属于 appendix / optional reference，尤其不要把 GrabGun / Healing / Secondary 当成 Normal。
- 不要把 `Minishoot` 的极简单层表现误解成“本项目只需要一张 Player 图”；它的主轴价值在 `Body + Shape + Outline + Lean + Dash + Trail/Particle` 的组合。
- 不要把 `WreckShip`、Debris、broken ship 参考提前混入本轮主飞船状态；死亡/毁坏是后续独立批次。

---

## 15. Appendix：Galactic Glitch 只作为可选增强参考

### 15.1 Appendix 定位

`Galactic Glitch` 不再作为本 workflow 的主生产模型。它只用于：

- 校验复杂状态图不要误用。
- 后续需要更复杂材质 / shader / 多状态换皮时做参考。
- 对照 PlayerSkin 状态映射，避免把某个状态专属图当成通用 Normal。

### 15.2 可选参考项

| GG 项 | 可借鉴内容 | 本轮处理方式 |
| --- | --- | --- |
| `Movement / Boost / Primary / Secondary / GrabGun / Healing` 状态表 | 多状态视觉切换思路 | 只作为后续扩展参考，不进入 Batch 1 必做清单 |
| `CLG_PlayerShipHighlight` / 高光材质 | 高光、脉冲、颜色参数 | 可借鉴参数，不照搬资产结构 |
| muzzle flash / ring / trail / noise | 程序化 VFX 纹理与材质组合 | 可用于 Fire / Weaving / Boost 的增强项 |
| PlayerSkinDefault 映射 | 防误用状态图 | 作为 Appendix 警戒表保留 |

### 15.3 禁误用规则

- GG 的 `GrabGun_Base_9/8` 只属于 GrabGun 状态，不得用于 Normal / Boost / Primary。
- GG 的 `Healing`、`Secondary`、`Primary` 贴图不得脱离原状态语境直接复用。
- 状态不明的 GG 图只能放 `Reference`，不能进入正式 Canary 生产目录。
- 如果某个 GG 参考会迫使我们提前建立完整多状态 Sprite 表，默认推迟到 Minishoot 主轴闭环之后。

---

## 16. 一句话总结

第一轮目标不是做出最终美术，而是按 Minishoot 主轴做出一套人人都能继续扩展的飞船美术资产结构：

```text
Minishoot 主轴 Body / Shape / Outline
+ Lean / Dash 短帧
+ Dodge 残影与 Trail/Particle
+ Boost 能量增强
+ Fire 短反馈
+ Hit 短反馈
+ Weaving 星图展开
+ Overheat 危险升温
+ Unity 可验证接入
```

只要这个闭环跑通，后续提升画质、加 Shader、调 Bloom、换皮肤，都会变得更快、更安全。
