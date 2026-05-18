# Minishoot vs Galactic Glitch — 美术模块实现差异对比

> **分析日期**：2026-05-18  
> **分析对象**：`Minishoot`、`Galactic Glitch` 两个 Unity 参考项目  
> **目的**：比较两者在 2D Top-Down 飞船游戏中的美术模块实现方式，为 Project Ark 的 `Ship / VFX / Environment / UI / PostProcess` 后续制作提供参考。  
> **结论先行**：`Minishoot` 更像“Sprite + Tilemap + Animator + 粒子 + 后处理”的清晰量产管线；`Galactic Glitch` 更像“多层 Sprite 状态机 + Shader/Material 驱动 + 高级 Trail/Fluxy + 屏幕特效”的高表现力管线。

---

## 1. 总体结论

### 1.1 两者美术实现的核心差异

| 维度 | Minishoot | Galactic Glitch |
| --- | --- | --- |
| 美术风格 | 像素风、清晰、明亮、可读性优先 | 高饱和科幻、霓虹、故障感、Shader 表现优先 |
| 主体实现方式 | Sprite/Animator 为主，结构较直接 | 多层 SpriteRenderer + PlayerSkin 状态表 + Shader/Material |
| 场景实现方式 | Tilemap / RuleTile / Biome / Additive Scene | 房间边界、Debris、背景 Shader、Glitch/Vignette 氛围 |
| 特效实现方式 | Cartoon FX、粒子池、闪白、倒影、阴影、Bloom | 大量自定义 Shader、Trail、Fluxy、Damage Overlay、Distortion |
| 动画重点 | Animator 状态机 + DOTween 短反馈 | 状态 SpritePack 切换 + Tween + 模块化视觉子系统 |
| 渲染复杂度 | 中等，适合独立游戏量产 | 高，偏重材质与 Shader 调参 |
| 数据驱动方式 | SO + Resources + Prefab + Pattern 数据 | PlayerSkin / ShipSpritePack / 多模块引用配置 |
| 项目可借鉴方向 | 地图、Tilemap、可读性、量产内容 | 飞船多层视觉、Boost/VFX、Shader 状态表现 |

### 1.2 最重要的判断

如果 Project Ark 现在处于“场景配置与验证阶段”，两个项目的借鉴优先级应当不同：

- **场景、地图、开放世界探索、Biome 切换**：优先参考 `Minishoot`。
- **飞船状态、Boost、武器开火、命中反馈、后处理冲击感**：优先参考 `Galactic Glitch`。
- **项目整体生产节奏**：先用 `Minishoot` 的清晰量产管线跑通垂直切片，再逐步引入 `Galactic Glitch` 的高表现力 Shader/VFX 层。

---

## 2. 美术资源规模与管线定位

### 2.1 Minishoot 的资源规模

既有分析中记录：

| 类别 | 数量 |
| --- | ---: |
| Texture2D | 2,724 |
| Sprite | 4,424 |
| AnimatorController | 13 |
| AnimationClip | 42 |
| AudioClip | 850 |
| 场景 | 16 |
| Prefab | 约 150 |

这说明 `Minishoot` 的美术生产重心在：

```text
大量 Sprite / Tile / Prefab
+ 少量清晰 AnimatorController
+ 统一 Tilemap / Biome 场景组织
+ 粒子与后处理补足手感
```

它更偏“可量产内容”的独立游戏管线。

### 2.2 Galactic Glitch 的资源规模

调研中确认 `Galactic Glitch` 资源规模更偏 Shader/VFX 密集：

| 类别 | 数量级 |
| --- | ---: |
| PNG | 约 6,111 |
| Material | 约 1,742 |
| Shader | 约 425 |
| AudioClip | 约 405 |

这说明它的美术生产重心不只是画图，而是：

```text
Sprite / Texture
+ Material 参数
+ Shader Graph / 自定义 Shader
+ Trail / Fluxy / Particle
+ 状态化视觉模块
```

它更偏“高表现力视觉系统”的管线。

---

## 3. 飞船主体表现差异

## 3.1 Minishoot：飞船是 Sprite + Animator + 简洁反馈

`Minishoot` 的玩家飞船系统更偏传统 2D 组合式：

```text
Player
├── PlayerView
├── PlayerEmote
├── PlayerEnergy
├── PlayerPowers
├── PlayerWeapon
├── PlayerControl
└── PlayerUpgradeView
```

其中 `PlayerView` 主要承担：

- 待机悬浮
- 转向倾斜
- Dash 动画
- 死亡/恢复动画
- Wobble 视觉反馈

玩家动画状态包括：

```text
Idle
Dash
DashHalf
LeanLeft1 / LeanLeft2 / LeanLeft3
LeanRight1 / LeanRight2 / LeanRight3
Menu
```

它的特点是：

- 飞船结构相对轻量。
- 主要依赖 Sprite 和 Animator 状态。
- 倾斜、冲刺、菜单状态都通过清晰的动画状态表达。
- 不强调每个状态都有独立的多层材质/Shader 组合。

### 3.2 Galactic Glitch：飞船是多层状态化视觉系统

`Galactic Glitch` 的飞船不是一张 Sprite，而是 `PlayerSkin` 数据驱动的多层 Sprite 状态机。

核心结构：

```text
PlayerSkin
└── stateToSpritesTable
    ├── ViewState
    ├── fadeDuration
    ├── solidSprite
    ├── liquidSprite
    ├── highlightSprite
    └── spritesOffset
```

典型状态映射：

| State | 含义 | solid | liquid | highlight |
| --- | --- | --- | --- | --- |
| 0 / 15 | Normal | Movement_10 | Movement_3 | Movement_21 |
| 1 | Boost | Boost_2 | Boost_16 | Boost_8 |
| 3 / 4 / 8 | Primary | Primary_4 | Primary | Primary_6 |
| 5 / 6 | Secondary | Secondary_8 | Secondary_0 | Secondary_17 |
| 7 | GrabGun | GrabGun_Base_9 | GrabGun_Base_9 | GrabGun_Base_8 |
| 9 | Healing | Healing_0 | Healing | vfx_dot_001 |

飞船渲染层级包含：

```text
Ship_Sprite_Liquid      能量/液体层
Ship_Sprite_HL          高光层
Dodge_Sprite            闪避残影层
Ship_Sprite_Solid       主体实体层
Ship_Sprite_Back        尾部推进器/后层
Ship_Sprite_Solid_Grab  抓取枪手形附件
Core_Sprite             核心/眼睛/反应堆
View                    黑色轮廓剪影
```

它的特点是：

- 每个视觉状态至少由 `solid / liquid / highlight` 三张 Sprite 组成。
- 同一张飞船在 Boost、Primary、Secondary、Grab、Healing 等状态下会整体换视觉包。
- 状态切换还有 `fadeDuration` 和 `spritesOffset`。
- 飞船外观强依赖 Material 和 Shader。

### 3.3 对 Project Ark 的启示

Project Ark 的金丝雀号不应停留在：

```text
Ship = 1 张 Sprite
```

更合适的 MVP 是：

```text
CanaryShipSkinSO
├── Normal
│   ├── Body
│   ├── Energy
│   └── Highlight
├── Boost
│   ├── Body
│   ├── Energy
│   └── Highlight
├── Fire
│   ├── Body
│   ├── Energy
│   └── Highlight
├── Hit
├── Overheat
└── Weaving
```

也就是说，飞船主体表现应学习 `Galactic Glitch`；但动画状态数量和内容生产节奏应学习 `Minishoot`，避免一开始就做过重的 Shader 系统。

---

## 4. 场景与环境美术差异

## 4.1 Minishoot：Tilemap / Biome / Additive Scene 是主干

`Minishoot` 的环境美术是非常典型的 2D 银河城量产管线：

```text
Tilemap
+ RuleTile 变体
+ BiomeTrigger
+ CameraTrigger
+ Additive Scene
+ Encounter / Door / Transition
```

Tile 类型覆盖：

```text
Ground / Dungeon Ground / Rock Ground
Wall / WallForest / WallDungeon / WallRed
WallColumn1~6
Water / WaterDeep / WaterDungeon / WaterGold
Hole / Grass / Snow / Shadow
```

它的环境表达方法是：

```text
一个区域主题 = 一组 Tile + 一组墙体 + 一组水/洞/草/雪 + 一组光照/后处理/音乐
```

这非常适合 Project Ark 当前阶段，因为我们已经完成关卡系统，正在做场景配置与验证。

### 4.2 Galactic Glitch：房间氛围更依赖 Shader 与屏幕效果

`Galactic Glitch` 的环境资源中可以看到：

```text
CLG_Room_Debris Tinted Room Bounded
Room Border Bubble
BlackBubbleBackground
CyberBackdrop
GlitchVignette
TextureVignette
CyberSpaceCombine
Corruption
Hologram Fill
```

它的环境表现更像：

```text
Room 基础结构
+ Debris 层
+ Room Border / Bubble
+ 背景 Shader
+ Glitch / Vignette
+ Corruption / Hologram / Distortion
```

这意味着它不是单纯靠 Tilemap 生产内容，而是大量使用 Shader 和材质去制造“空间异常感”。

### 4.3 对 Project Ark 的启示

Project Ark 的环境美术可以分两阶段：

#### MVP 阶段：学习 Minishoot

```text
RoomVariantSO
+ TilemapVariantSwitcher
+ AmbienceController
+ WorldPhaseSO
+ Camera Confiner
+ Minimap
```

目标是让场景能快速形成区域差异：

```text
地表 / 遗迹 / 腐化区 / 深层设施 / Boss 房
```

#### 强化阶段：学习 Galactic Glitch

```text
WorldPhase Vignette
+ Room Border Energy
+ Corruption Overlay
+ Glitch Distortion
+ Background Shader
+ Ambient Particle
```

目标是让世界阶段变化被玩家直观看到。

---

## 5. VFX 与战斗反馈差异

## 5.1 Minishoot：粒子池 + CartoonFX + 闪白/阴影/倒影

`Minishoot` 的 VFX 管线由这些组件组成：

```text
Fx.cs
FxData.cs
EmissionPools.cs
ParticleMaster.cs
SpriteReflection.cs
SpriteShadow.cs
ShineEffect.cs
IsFlashingEffect.cs
```

它的战斗反馈更接近：

```text
开火
→ 子弹 Sprite / BulletData
→ Bullet Trail / Particle
→ 命中 Fx
→ 爆炸 Stain / CartoonFX
→ 闪白 / 屏幕反馈
→ 音效
```

优点：

- 结构直接。
- 可读性强。
- 易量产大量敌人和弹幕。
- 适合弹幕射击和银河城探索。

不足：

- 单个特效的“高级质感”主要依赖已有素材和粒子。
- 如果不额外引入 Shader，能量/扭曲/流体感有限。

## 5.2 Galactic Glitch：Shader / Trail / Fluxy / Overlay 是核心

`Galactic Glitch` 的 VFX 资源和脚本显示它使用了更重的视觉系统：

```text
PlayerViewBoostModule
PlayerViewFluxyTrailModule
PlayerViewFluxyGrabModule
PlayerViewDamageOverlayModule
PlayerViewLQTrailModule
PlayerViewTeleportModule
PlayerViewSpawnModule
```

常见材质/Shader 方向：

```text
EngineTrail
SmokeTrail
ErosedTrail
LaserBeamWithSubbeams
RadialDistortion
DamageOverlayStylizedCircle
GrabOverlay
GrabRecolor
OrganicWeakSpot
PortalThroughWaves
vfx_simple_mask_dissolve_additive
CLG_DisplacementCloak
CLG_LightningLink
CLG_GlitchVignette
CLG_Corruption
```

它的战斗反馈更接近：

```text
状态变化
→ 多层 Sprite 切换
→ Shader 参数变化
→ Trail / Fluxy 拉出动态尾迹
→ Damage Overlay / Distortion
→ Camera Shake
→ 后处理脉冲
```

优点：

- 高表现力。
- 状态差异非常明显。
- 适合科幻、异常、能量、空间扭曲主题。

不足：

- 管线更重。
- 需要更强的 Shader/Material 维护能力。
- 若缺少规范，容易出现多个工具/Prefab/Runtime 同时改 VFX 链的复杂度问题。

### 5.3 对 Project Ark 的启示

Project Ark 的战斗 VFX 应采用混合路线：

```text
Minishoot 的量产粒子池
+ Galactic Glitch 的关键状态 Shader/Trail
```

建议优先级：

1. **基础射击/命中/爆炸**：学习 `Minishoot`，走对象池和简单 Particle。
2. **Boost / Overheat / Weaving / Boss 能量技**：学习 `Galactic Glitch`，使用 Shader、Trail、Overlay。
3. **所有运行时 VFX**：必须遵守 Project Ark 对象池防御性复位规则。

---

## 6. 后处理与相机反馈差异

## 6.1 Minishoot：后处理用于统一画面与区域氛围

`Minishoot` 使用 URP Volume 后处理：

```text
Bloom
ChromaticAberration
LensDistortion
ColorAdjustments
ChannelMixer
ShadowsMidtonesHighlights
Vignette
WhiteBalance
```

它的后处理更像“区域滤镜”和“战斗增强”：

```text
Biome 切换
→ 色调变化
→ Bloom / Vignette 调整
→ 区域氛围统一
```

### 6.2 Galactic Glitch：后处理/屏幕 Shader 是状态表现的一部分

`Galactic Glitch` 出现了：

```text
CLG_TextureVignette
CLG_GlitchVignette
RadialDistortionWithNoise
DamageOverlaySimple
DamageOverlayStylizedCircle
```

这类效果不是普通滤镜，而更像“玩法状态的视觉语言”：

```text
受击 → Damage Overlay
传送 → Radial Distortion
Glitch 状态 → Glitch Vignette
空间异常 → Corruption / Distortion
```

### 6.3 对 Project Ark 的启示

Project Ark 应该建立 `PostProcessController`：

```text
NormalLook
CombatLook
LowHealthLook
OverheatLook
WeavingLook
BossRoomLook
WorldPhaseLook
DeathLook
```

并且把后处理当作战斗反馈的一部分，而不是最后统一加的滤镜。

---

## 7. UI 美术差异

## 7.1 Minishoot：传统 HUD / Map / Menu / Upgrade UI

`Minishoot` 的 UI 模块围绕：

```text
HUD
Map
PauseMenu
Options
TitleMenu
PopUpScreen
TextMessage
CutsceneIntro
LevelUp / EnergyUp / UpgradeView
```

它的 UI 更偏功能清晰：

- 血量/能量/XP
- 地图
- 升级选项
- 技能/模块解锁
- 文本剧情

### 7.2 Galactic Glitch：UI 与故障科幻风格绑定更强

`Galactic Glitch` 的资源目录包含：

```text
GlitchUI
Custom/UI
GUI
map_boost_icon
Boss_healthbar_flash
weapon_holder_frame
powerTokenPurple
BACKGROUND_PRICE
map_bg_outline
```

它的 UI 更强调：

- 紫色/霓虹风格一致性
- 武器框/能力图标/地图框的高风格化
- Boss 血条闪白和战斗反馈
- 与飞船能量颜色体系统一

### 7.3 对 Project Ark 的启示

Project Ark 的 UI 可以采取：

```text
Minishoot 的信息架构
+ Galactic Glitch 的视觉风格一致性
```

尤其是星图 UI：

```text
Slot Cell
Item Icon
Drag Ghost
Valid / Invalid Highlight
Track Frame
Heat Bar
Weaving Transition
```

应该优先保证交互清晰，再强化星图/编织态的能量视觉。

---

## 8. 动画体系差异

## 8.1 Minishoot：AnimatorController 负责主要状态

`Minishoot` 的 AnimatorController 数量不多，但覆盖明确：

```text
PlayerAnimator
Blober
Frogger
Junker
Scara
NpcHouse
NpcTree
Turtle
SkillCube
```

它适合：

- 角色/敌人循环动画
- Dash/Lean 等状态动画
- NPC 状态
- SkillCube 等交互物表现

## 8.2 Galactic Glitch：Animator + Tween + SpritePack 状态切换并行

`Galactic Glitch` 不只是 Animator，而是多套系统叠加：

```text
ViewState
+ ShipSpritePack
+ fadeDuration
+ Animator
+ Tween Sequence
+ VFX Module
```

它适合：

- 状态切换强烈的飞船
- 开火/Boost/Grab/Heal 等高反馈动作
- 需要 Shader/VFX 参与的复杂状态

### 8.3 对 Project Ark 的启示

Project Ark 应该避免“所有动画都塞进 Animator”。更好的分工是：

| 类型 | 推荐实现 |
| --- | --- |
| 循环状态 | Animator |
| 短促反馈 | PrimeTween |
| 状态换皮 | `ShipSkinSO` / `SpritePack` |
| 粒子与 Trail | VFX View 脚本 |
| 后处理脉冲 | `PostProcessController` |
| 镜头震动 | `CameraJuice` |

---

## 9. 两套管线的风险差异

## 9.1 Minishoot 风险

| 风险 | 说明 | Project Ark 防御方式 |
| --- | --- | --- |
| 静态单例过多 | `Player.Instance`、`GameManager`、`Sounds` 等全局访问较多 | 使用 `ServiceLocator` 与事件总线控制边界 |
| Coroutine / DOTween 旧模式 | 对 Project Ark 的 UniTask / PrimeTween 不完全匹配 | 新代码继续使用 UniTask + PrimeTween |
| 美术表现上限依赖素材数量 | 如果没有大量 Tile/Sprite，场景容易空 | 先做垂直切片，不水平铺大量区域 |
| Shader 表现相对克制 | 高级能量/扭曲效果少 | 关键状态引入 GG 式 Shader/VFX |

## 9.2 Galactic Glitch 风险

| 风险 | 说明 | Project Ark 防御方式 |
| --- | --- | --- |
| 材质/Shader 数量庞大 | 维护成本高，容易出现资源映射混乱 | 建立 `AssetRegistry` 和 Canonical Spec |
| 多入口同时影响 VFX | Runtime、Prefab、Debug、Scene 可能同时改视觉链 | 遵守 `Ship / VFX` authority matrix |
| 状态映射容易误用 | 如 `GrabGun_Base_9/8` 只属于 State 7 | 文档化状态映射，禁止凭文件名猜用途 |
| 表现系统过重 | 早期照搬会拖慢垂直切片 | 只抽取关键状态，不全量复刻 |

---

## 10. Project Ark 推荐采用的混合美术路线

### 10.1 第一阶段：Minishoot 式清晰可玩

目标：快速形成完整可玩的房间。

```text
Tilemap 房间
+ 明确区域色调
+ 基础粒子
+ 基础后处理
+ 清晰 HUD
+ 基础飞船状态动画
```

验收标准：

- 玩家能一眼分辨地形、门、敌人、子弹、奖励。
- 房间从探索切换到战斗时，音乐/相机/后处理/门状态有反馈。
- 世界阶段切换时，至少有色调或环境粒子变化。

### 10.2 第二阶段：Galactic Glitch 式飞船/VFX 强化

目标：让金丝雀号有“活着的飞船”的状态感。

```text
ShipSkinSO
+ Body / Energy / Highlight 三层
+ Boost Trail
+ Overheat Overlay
+ Weaving Aura
+ Hit Flash
+ Camera Shake
+ PostProcess Pulse
```

验收标准：

- Normal、Boost、Fire、Hit、Overheat、Weaving 六个状态肉眼可区分。
- Boost 不只是速度变快，而有 Trail、Bloom、音效、相机轻反馈。
- Overheat 有持续视觉压力。
- Weaving 态有明确的星图/能量视觉语言。

### 10.3 第三阶段：世界阶段视觉系统

目标：让 Project Ark 的关卡系统和美术模块结合。

```text
WorldPhaseSO
+ RoomVariantSO
+ TilemapVariantSwitcher
+ AmbienceController
+ PostProcessController
+ Ambient VFX
```

验收标准：

- 世界阶段变化不只影响数值，也影响场景视觉。
- 玩家能通过画面判断当前世界状态。
- Boss/异常房间拥有独立氛围层。

---

## 11. 最终建议

### 11.1 Project Ark 不应该二选一

`Minishoot` 和 `Galactic Glitch` 的价值不同：

```text
Minishoot = 如何把一个 2D Top-Down 银河城做成完整、清晰、可量产的游戏
Galactic Glitch = 如何把一个飞船/战斗/VFX 做出高状态感和高能量质感
```

Project Ark 应该采取：

```text
场景生产与地图结构：Minishoot
飞船状态与战斗 VFX：Galactic Glitch
UI 信息架构：Minishoot
UI 风格化与能量色彩：Galactic Glitch
后处理使用方式：两者结合
```

### 11.2 最适合 Project Ark 的近期落地顺序

1. **建立 `ShipSkinSO` 和三层 SpriteRenderer 结构**：参考 `Galactic Glitch`。
2. **建立 `PostProcessController` 状态表**：参考两者。
3. **用 `RoomVariantSO` + `AmbienceController` 做房间美术差异**：参考 `Minishoot`。
4. **将 Boost / Overheat / Weaving 做成三个视觉样板**：参考 `Galactic Glitch`。
5. **把基础 VFX 全部对象池化并做防御性复位**：遵守 Project Ark 现有架构规范。

### 11.3 一句话总结

`Minishoot` 给 Project Ark 的答案是：

```text
如何稳定量产一个清晰可玩的 2D 银河城世界。
```

`Galactic Glitch` 给 Project Ark 的答案是：

```text
如何把飞船、能量、Boost、受击、后处理做成一套有状态、有质感的视觉系统。
```

Project Ark 的正确路线是：

```text
先用 Minishoot 的管线确保可玩，
再用 Galactic Glitch 的视觉系统提升金丝雀号和战斗反馈的记忆点。
```
