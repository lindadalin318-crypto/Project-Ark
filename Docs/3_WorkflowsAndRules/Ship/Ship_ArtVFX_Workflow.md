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
| `Layer` | 飞船被拆成的视觉层 | Solid、Liquid、Highlight、Core、Back、Aura |
| `Prefab` | Unity 中可复用的对象模板 | `Ship.prefab`、`BoostTrailRoot.prefab` |

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
| `map` | `albedo` / `emission` / `mask` / `normal` | 贴图用途 |

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

---

## 2. 第一大阶段：生产主飞船 Sprite

### 2.0 本阶段目标

本阶段只解决一个问题：

```text
做出金丝雀号在 Normal 状态下的主飞船视觉，并拆成 Unity 可以组合的层。
```

不要在本阶段做 Boost、Hit、Weaving、Overheat。那些是后续状态变体。

### 2.1 主飞船 Normal 的玩家感受

玩家看到 Normal 状态时应该感到：

```text
这是一艘脆弱但可靠的异星探测船，主体是破旧工业金属，内部有神秘星图能量核心。
```

关键词：

- 小型。
- 旧金属。
- 明确鼻尖朝向。
- 中央有能量核心。
- 不像战斗机那么军用，也不像魔法飞盘。

### 2.2 主飞船 Normal 必须产出的图

第一批必须产出 5 张：

| 编号 | 文件名 | 用途 | 必须做吗 |
| --- | --- | --- | --- |
| `1-A` | `spr_ship_canary_solid_normal_albedo.png` | 主体轮廓与金属船壳 | 必须 |
| `1-B` | `spr_ship_canary_liquid_normal_albedo.png` | 蓝紫能量纹路，不强发光 | 必须 |
| `1-C` | `spr_ship_canary_highlight_normal_albedo.png` | 边缘高光、金属亮面 | 必须 |
| `1-D` | `spr_ship_canary_core_normal_albedo.png` | 中央核心底色 | 必须 |
| `1-E` | `spr_ship_canary_back_normal_albedo.png` | 尾部喷口、后层结构 | 建议 |

第二批可选产出：

| 编号 | 文件名 | 用途 |
| --- | --- | --- |
| `1-F` | `tex_ship_canary_solid_normal_normal.png` | 船体凹凸感，可选 |
| `1-G` | `spr_ship_canary_liquid_normal_emission.png` | Normal 状态能量发光 |
| `1-H` | `spr_ship_canary_core_normal_emission.png` | 核心弱发光 |
| `1-I` | `spr_ship_canary_highlight_normal_mask.png` | 后续闪白遮罩 |

### 2.3 1-A：`solid_normal_albedo` 需求

这张图是飞船最重要的一张图。

#### 它要表现什么

- 飞船完整轮廓。
- 金属船壳。
- 鼻尖方向。
- 左右结构。
- 主要阴影。

#### 它不要表现什么

- 不要画强发光。
- 不要画尾焰。
- 不要画受击闪白。
- 不要把 Bloom 效果画死。
- 不要把背景星空画进去。

#### 制作方式

1. 用 AI 或手绘得到一张完整飞船概念图。
2. 把能量线、发光、光环临时关掉或擦掉。
3. 保留金属主体、轮廓、结构块。
4. 清理透明边缘。
5. 导出为 `spr_ship_canary_solid_normal_albedo.png`。

#### 验收标准

- 缩小到 `128 × 128` 仍能看出飞船朝上。
- 单独显示这张图时，飞船仍然成立。
- 没有背景像素。
- 没有强发光画死在主体上。

### 2.4 1-B：`liquid_normal_albedo` 需求

`Liquid` 不是水，它代表“可被状态改变的能量纹路层”。

#### 它要表现什么

- 船体内部或表面的星图能量纹路。
- 默认状态的低亮度蓝紫色。
- 可被 Boost / Weaving / Overheat 改色的区域。

#### 它不要表现什么

- 不要覆盖整艘船。
- 不要遮住 `Solid` 的轮廓。
- 不要画太亮，Normal 状态应该克制。

#### 制作方式

1. 复制完整飞船概念图。
2. 只保留能量纹路和液态/星图线条。
3. 删除金属船体主体。
4. 把能量纹路调成低亮蓝紫。
5. 导出透明 PNG。

#### 验收标准

- 单独看这张图时，只能看到能量纹路，不应该是一艘完整飞船。
- 叠在 `Solid` 上时，能量纹路不遮挡飞船朝向。
- 后续改成 Boost/Weaving/Overheat 颜色时有足够空间。

### 2.5 1-C：`highlight_normal_albedo` 需求

`Highlight` 用来增强金属边缘、受击闪白、视觉脉冲。

#### 它要表现什么

- 船体边缘高光。
- 鼻尖、翼尖、金属凸起处的亮线。
- 可以被 HitFlash 临时增强的区域。

#### 它不要表现什么

- 不要画成完整白色飞船。
- 不要铺满大面积白色。
- 不要包含能量核心的大块发光。

#### 制作方式

1. 在 `Solid` 上方新建图层。
2. 用浅灰/淡青白画出少量边缘高光。
3. 删除 `Solid` 本体，只保留高光线。
4. 导出透明 PNG。

#### 验收标准

- 单独看像“高光线稿”，不是完整飞船。
- 叠上去后飞船更清楚，但不刺眼。
- 后续 HitFlash 可以把这层临时变白。

### 2.6 1-D：`core_normal_albedo` 需求

`Core` 是玩家判断状态的主要焦点之一。

#### 它要表现什么

- 中央星图核心。
- 默认状态下的低亮能量点。
- 后续 Fire / Weaving / Overheat 的状态锚点。

#### 它不要表现什么

- 不要比整艘船还大。
- 不要做成 UI 图标。
- 不要强到盖过主体轮廓。

#### 制作方式

1. 在飞船中心选择一个明确区域。
2. 画核心外壳、内圈、能量点。
3. 默认状态亮度控制在中低。
4. 导出透明 PNG。

#### 验收标准

- 叠在飞船上后，玩家能知道“这里是核心”。
- 不打开 Emission 时也能看见。
- 不会抢走朝向信息。

### 2.7 1-E：`back_normal_albedo` 需求

`Back` 是 Boost 和尾焰的视觉锚点。

#### 它要表现什么

- 船尾结构。
- 喷口底座。
- 后层机械结构。

#### 它不要表现什么

- 不要画持续尾焰。
- 不要画大面积粒子。
- 不要和 `Solid` 重复太多。

#### 制作方式

1. 从完整飞船中提取尾部结构。
2. 如果 `Solid` 已经包含完整尾部，可以只保留喷口和后层装饰。
3. 导出透明 PNG。

#### 验收标准

- Boost Trail 能从这张图附近自然长出来。
- 不打开 Boost 时也不突兀。
- 不遮挡主体。

### 2.8 1-F：`solid_normal_normal` 可选需求

Normal Map 是可选项。第一轮如果没有把握，可以跳过。

#### 它要表现什么

- 船体金属凹凸。
- 装甲板边缘。
- 轻微体积感。

#### 推荐工具

- Photoshop Normal Map 插件。
- Materialize。
- Krita 法线贴图工具。
- Unity 中临时用普通 Sprite Lit 材质测试。

#### 验收标准

- 法线不应该让飞船看起来像 3D 塑料玩具。
- 光照方向变化时只产生轻微体积感。
- 如果效果不好，宁可不用。

### 2.9 1-G / 1-H：Normal Emission 需求

Emission 是发光贴图。

#### 它要表现什么

- `Liquid` 的低亮能量发光。
- `Core` 的低亮核心发光。

#### 黑白规则

```text
黑色 = 不发光
彩色 = 发光
越亮 = 越强
```

#### 验收标准

- Normal 状态不应该像 Boost。
- 关闭 Bloom 后，图像仍然可读。
- 开启 Bloom 后，核心和能量线有轻微呼吸感。

### 2.10 本阶段完成标准

只有满足以下条件，才进入 Dodge / Boost 状态生产：

- `Solid` 单独可读。
- `Solid + Liquid + Highlight + Core + Back` 叠合无偏移。
- 所有图尺寸一致。
- 所有图 Pivot 一致。
- 所有图透明边干净。
- `128 × 128` 缩略预览能看出朝向。

---

## 3. 第二大阶段：生产 Dodge State

### 3.0 Dodge State 的定位

Dodge 是闪避 / 冲刺瞬间的视觉反馈。它和 Boost 不一样：

| 状态 | 玩家感受 | 视觉重点 |
| --- | --- | --- |
| `Dodge` | 瞬间闪开、短促、轻盈 | 残影、透明、方向拖尾 |
| `Boost` | 持续推进、速度增强 | 尾焰、能量持续增强 |

Dodge 不应该是一套完整新飞船，而是：

```text
主飞船 Normal 图
+ Dodge Ghost 残影图
+ 短促高光/透明 Tween
+ 可选小型粒子
```

### 3.1 Dodge 必须产出的图

| 编号 | 文件名 | 用途 | 必须做吗 |
| --- | --- | --- | --- |
| `2-A` | `spr_ship_canary_dodgeghost_dodge_albedo.png` | Dodge 静态残影 | 必须 |
| `2-B` | `spr_ship_canary_dodgeghost_dodge_mask.png` | 控制残影透明/溶解 | 建议 |
| `2-C` | `spr_ship_canary_highlight_dodge_albedo.png` | Dodge 瞬间高光 | 建议 |
| `2-D` | `spr_vfx_canary_dodge_streak_01.png` | 小型速度线 | 可选 |

### 3.2 2-A：`dodgeghost_dodge_albedo` 需求

这是 Dodge 最核心的图。

#### 它要表现什么

- 飞船轮廓的残影。
- 颜色偏淡青/蓝紫。
- 透明感。
- 比主飞船更虚、更轻。

#### 它不要表现什么

- 不要比主飞船更实。
- 不要包含复杂金属细节。
- 不要有尾焰持续效果。
- 不要有完整背景拖尾。

#### 制作方式

1. 复制 `spr_ship_canary_solid_normal_albedo.png`。
2. 降低细节：模糊或淡化金属纹理。
3. 统一染成淡青蓝或淡紫蓝。
4. Alpha 降低到约 35%-55%。
5. 保留清晰轮廓，删除过细小结构。
6. 导出为 `spr_ship_canary_dodgeghost_dodge_albedo.png`。

#### 验收标准

- 一眼能看出是飞船残影。
- 不会被误认为当前实体船体。
- 在深色背景上可见，在亮色背景上不刺眼。
- 连续生成 3 个残影时画面不糊。

### 3.3 2-B：`dodgeghost_dodge_mask` 需求

Mask 用来控制残影从前到后逐渐消失。

#### 它要表现什么

- 白色区域：残影保留更久。
- 黑色区域：残影更快消失。
- 推荐鼻尖偏白，尾部偏灰黑，让残影向后散掉。

#### 制作方式

1. 复制 Dodge Ghost 轮廓。
2. 转成灰度。
3. 鼻尖和核心区域保持亮。
4. 翼尖和尾部做灰黑渐变。
5. 导出为 `spr_ship_canary_dodgeghost_dodge_mask.png`。

#### 验收标准

- 单独看是黑白/灰度图。
- 没有彩色信息。
- 用它做透明渐隐时，残影消失方向自然。

### 3.4 2-C：`highlight_dodge_albedo` 需求

Dodge 高光用于闪避开始的一瞬间。

#### 它要表现什么

- 鼻尖和翼缘的短促亮线。
- 类似“瞬间折光”。
- 只出现 0.05-0.15 秒。

#### 制作方式

1. 复制 `highlight_normal_albedo`。
2. 提高亮度。
3. 删除不必要的内部细节，只保留外缘和方向感。
4. 颜色用淡青白。

#### 验收标准

- 单帧出现时玩家能感觉“闪了一下”。
- 不会被误认为受击白闪。
- 面积小于 HitFlash。

### 3.5 Dodge Runtime 表现需求

Dodge 不只是图，还需要播放方式。

建议表现：

```text
Dodge start:
  主船轻微透明 0.08s
  生成 2-3 个 Dodge Ghost
  Highlight 快速闪一下
Dodge sustain:
  Ghost 向相反方向淡出
Dodge end:
  主船恢复正常 alpha
```

### 3.6 Dodge 验收标准

- 按下 Dodge 的瞬间，玩家能感觉“短促闪开”。
- Dodge 和 Boost 一眼不同。
- Dodge Ghost 不遮挡子弹和敌人。
- 快速连续 Dodge 不残留残影 alpha / scale / color。
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

主飞船资产建议：

```text
Assets/_Art/Ship/Canary/
├── Source/
│   ├── Concepts/
│   ├── Layered/
│   └── AI_Raw/
├── Sprites/
│   ├── Solid/
│   ├── Liquid/
│   ├── Highlight/
│   ├── Core/
│   ├── Back/
│   ├── DodgeGhost/
│   └── Aura/
├── Textures/
│   ├── Masks/
│   ├── Emission/
│   └── Normal/
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
- 尺寸没有被 Unity 自动压缩糊掉。
- Alpha 边缘干净。
- 所有层叠合无偏移。

### 9.3 接入现役节点

| 资产 | 接入节点 |
| --- | --- |
| `solid_*` | `Ship_Sprite_Solid` |
| `liquid_*` | `Ship_Sprite_Liquid` |
| `highlight_*` | `Ship_Sprite_HL` |
| `core_*` | `Ship_Sprite_Core` |
| `back_*` | `Ship_Sprite_Back` |
| `dodgeghost_*` | `Dodge_Sprite` / `DashAfterImageSpawner` |
| `aura_weaving_*` | 新增前先确认是否作为 VFX Prefab，而不是改 `ShipVisual` 主层 |

### 9.4 接入约束

- 不在 Scene 实例上长期修 `ShipVisual`。
- 不新增第二套飞船视觉根节点。
- 不让 Runtime fallback 自动修资产。
- 若要新增节点，先更新 `CanonicalSpec` / `AssetRegistry`。
- Debug 工具只 preview，不接管正式链。

---

## 10. 第九大阶段：Material / Shader 生产

### 10.1 第一批 Material

| 编号 | Material 名 | 使用贴图 |
| --- | --- | --- |
| `M-1` | `mat_ship_canary_solid_default` | `solid_normal_albedo`，可选 normal |
| `M-2` | `mat_ship_canary_liquid_normal` | `liquid_normal_albedo + emission` |
| `M-3` | `mat_ship_canary_liquid_boost` | `liquid_boost_albedo + emission` |
| `M-4` | `mat_ship_canary_liquid_weaving` | `liquid_weaving_albedo + emission + noise` |
| `M-5` | `mat_ship_canary_liquid_overheat` | `liquid_overheat_albedo + emission + noise` |
| `M-6` | `mat_ship_canary_highlight_default` | `highlight_normal_albedo` |
| `M-7` | `mat_ship_canary_highlight_hitflash` | `highlight_hit_mask` |
| `M-8` | `mat_ship_canary_core_default` | `core_normal_albedo + emission` |
| `M-9` | `mat_ship_canary_dodgeghost` | `dodgeghost_dodge_albedo + mask` |
| `M-10` | `mat_ship_canary_weaving_aura` | `aura_weaving_emission + ring mask` |

### 10.2 第一批 Shader

第一轮只需要少量通用 Shader：

| Shader | 用途 |
| --- | --- |
| `ShipEnergyPulse` | Liquid/Core 发光脉冲 |
| `ShipHighlightFlash` | HitFlash / DodgeFlash |
| `AdditiveGlow` | Aura / Muzzle / Spark |
| `BoostTrailMain` | 现役 BoostTrail |
| `DissolveFade` | DodgeGhost / Death 预备 |

### 10.3 材质与 Shader 分工

```text
Shader = 算法
Material = 参数
Sprite/Texture = 图像内容
Runtime = 什么时候切换、什么时候 tween
```

运行时不要修改 authored Material。单对象变化使用 Material 实例或 `MaterialPropertyBlock`。

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

### 11.2 对象池复位清单

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

### Batch 1：只做主飞船 Normal

产出：

```text
spr_ship_canary_solid_normal_albedo.png
spr_ship_canary_liquid_normal_albedo.png
spr_ship_canary_highlight_normal_albedo.png
spr_ship_canary_core_normal_albedo.png
spr_ship_canary_back_normal_albedo.png
```

完成标准：五层叠合可读，`Solid` 单独可读。

### Batch 2：做 Dodge

产出：

```text
spr_ship_canary_dodgeghost_dodge_albedo.png
spr_ship_canary_dodgeghost_dodge_mask.png
spr_ship_canary_highlight_dodge_albedo.png
```

完成标准：能在 Play Mode 中看到短促残影。

### Batch 3：做 Boost

产出：

```text
spr_ship_canary_liquid_boost_albedo.png
spr_ship_canary_liquid_boost_emission.png
spr_ship_canary_core_boost_emission.png
spr_ship_canary_back_boost_albedo.png
```

完成标准：Boost 启停有持续推进感。

### Batch 4：做 Fire / Hit

产出：

```text
spr_ship_canary_core_fire_emission.png
spr_vfx_canary_muzzle_flash_01.png
spr_ship_canary_highlight_hit_mask.png
spr_vfx_canary_hit_spark_01.png
```

完成标准：开火和受击都短促、清楚、不混淆。

### Batch 5：做 Weaving

产出：

```text
spr_ship_canary_liquid_weaving_albedo.png
spr_ship_canary_liquid_weaving_emission.png
spr_ship_canary_core_weaving_emission.png
spr_ship_canary_aura_weaving_emission.png
```

完成标准：编织态有星图连接感。

### Batch 6：做 Overheat

产出：

```text
spr_ship_canary_liquid_overheat_albedo.png
spr_ship_canary_liquid_overheat_emission.png
spr_ship_canary_core_overheat_emission.png
spr_vfx_canary_overheat_spark_01.png
```

完成标准：不看 UI 也知道热量危险。

### Batch 7：固化 Unity 接入和注册表

产出：

```text
Unity Import Settings 统一
Ship.prefab 接入
VFX Prefab 接入
ShipVFX_AssetRegistry 更新
ImplementationLog 更新
```

完成标准：Debug 关闭后，正式 Runtime 链路仍能驱动所有状态。

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

---

## 15. 一句话总结

第一轮目标不是做出最终美术，而是做出一套人人都能继续扩展的飞船美术资产结构：

```text
Normal 主体分层
+ Dodge 残影
+ Boost 能量增强
+ Fire 短反馈
+ Hit 短反馈
+ Weaving 星图展开
+ Overheat 危险升温
+ Unity 可验证接入
```

只要这个闭环跑通，后续提升画质、加 Shader、调 Bloom、换皮肤，都会变得更快、更安全。
