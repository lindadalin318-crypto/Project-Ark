# Galactic Glitch 美术资源解包分析报告

> **用途**：为 Project Ark 飞船美术制作提供参考规格。通过 AssetRipper + Il2CppDumper + GameAssembly.dll 二进制分析完成。
> **日期**：2026-03-06
> **分析对象**：Galactic Glitch（Steam）玩家飞船 "Glitch" 的所有美术资源、Prefab 层级、材质与渲染设置

---

## 一、重要澄清：命名陷阱

在 GG 的解包资产中，以下命名容易引起混淆：

| 文件名前缀 | 实际内容 | 与飞船的关系 |
|-----------|---------|------------|
| `13_Glitch_a.png` ～ `22_Glitch_l.png` | 游戏**标题 Logo** "Galactic Glitch" 的字体动画图层 | **无关**，是 UI Logo |
| `GrabGun_Base_*.png` | **玩家飞船主体**贴图 | ✅ 飞船本体 |
| `GrabGun_Back_*.png` | **飞船尾部推进器** | ✅ 飞船背面 |
| `GrabGun_Hand_*.png` | **重力枪手形**（Ability 视觉效果） | ✅ 飞船附件 |

GG 飞船本体美术采用 `GrabGun_` 前缀命名，是因为这套资源同时被重力枪（Gravity Gun）能力的特殊视觉所复用。飞船型号名 "Glitch" 没有直接体现在纹理文件名中。

---

## 二、飞船 SpriteRenderer 完整层级

> 以下数据来自 `Player.prefab` 的 YAML 序列化，通过 AssetRipper 提取。

飞船视觉由 `PlayerView` MonoBehaviour 统一管理，包含以下核心 SpriteRenderer：

| 渲染顺序 | Sort Layer | Sort Order | GO 名称 | 功能描述 |
|---------|-----------|-----------|---------|---------|
| 最底 | 5 | -2 | `Ship_Sprite_Liquid` | 主机体液体/能量**发光**层 |
| ↑ | 5 | -1 | `Ship_Sprite_HL` | **高光/边缘光**层 |
| ↑ | 5 | -1 | `Dodge_Sprite` | Dodge **残影轮廓**（青绿色 tint） |
| ↑ | 5 | 0 | `Ship_Sprite_Solid` | 主机体**实体**层 |
| ↑ | 5 | 0 | `Ship_Sprite_Back` | 尾部**推进器/后层** |
| ↑ | 5 | 0 | `Ship_Sprite_Solid_Grab_R` | 重力枪**手形**（右） |
| ↑ | 5 | 1 | `Core_Sprite (Reactor)` | **反应堆核心**小图标 |
| ↑ | 5 | 2 | `Core_Sprite (Living Eye)` | 船眼/**透镜** |
| 最顶 | 2 | 5 | `View` | 飞船整体**鸟瞰轮廓**（黑色剪影） |

> **注意**：`Solid` 和 `Liquid` 层使用**同一张贴图** `GrabGun_Base_9.png`，靠材质/着色器差异呈现不同视觉效果（液体发光 vs 实体）。

---

## 三、核心纹理文件规格

### 3.1 主体贴图

#### `GrabGun_Base_9.png` — 飞船主体（Solid + Liquid 双用）

| 属性 | 值 |
|------|-----|
| 分辨率 | **430 × 430 px** |
| 色彩模式 | RGBA |
| 文件大小 | 41.2 KB |
| Sprite PPU | **320** |
| 世界单位大小 | **1.34 × 1.34 Unity units** |
| Sprite Pivot | (0.5, 0.5) 中心对齐 |
| 用途 | `Ship_Sprite_Solid`（Order 0）+ `Ship_Sprite_Liquid`（Order -2）双层 |

#### `GrabGun_Base_8.png` — 飞船高光层

| 属性 | 值 |
|------|-----|
| 分辨率 | **430 × 430 px** |
| 色彩模式 | RGBA |
| 文件大小 | 8.4 KB（纯高光信息，几乎透明） |
| Sprite PPU | **320** |
| 世界单位大小 | **1.34 × 1.34 Unity units** |
| 用途 | `Ship_Sprite_HL`（高光/边缘光，Order -1） |

> 主机体系列（`GrabGun_Base_0` ～ `GrabGun_Base_20`）共 21 张变体，均为 430×430 或 700×700 px，对应游戏内不同解锁的飞船皮肤。

---

### 3.2 背面/推进器

#### `GrabGun_Back_3.png` — 飞船尾部

| 属性 | 值 |
|------|-----|
| 分辨率 | **186 × 96 px** |
| 色彩模式 | RGBA |
| 文件大小 | 17.4 KB |
| Sprite PPU | **320** |
| 世界单位大小 | **0.58 × 0.30 Unity units** |
| 用途 | `Ship_Sprite_Back`（后层推进器） |

> Back 系列共 9 张变体（`GrabGun_Back`、`Back_1`～`Back_7`，`GrabGun_back_0/2`），分辨率为 186×96 或 700×700。

---

### 3.3 重力枪手形附件

#### `GrabGun_Hand_7.png` — 重力枪手形

| 属性 | 值 |
|------|-----|
| 分辨率 | **130 × 164 px** |
| 色彩模式 | RGBA |
| 文件大小 | 18.4 KB |
| Sprite PPU | **320** |
| 世界单位大小 | **0.36 × 0.50 Unity units** |
| Sprite Pivot | **(0.5706, 0.4906)** — 非中心，偏移量用于对齐抓取动画 |
| 用途 | `Ship_Sprite_Solid_Grab_R`（重力枪右手，两个实例） |

> Hand 系列共 8 张变体，均为 130×164 px。

---

### 3.4 特殊效果精灵

#### `player_test_fire.png` — Dodge 残影轮廓

| 属性 | 值 |
|------|-----|
| 分辨率 | **751 × 722 px** |
| 色彩模式 | RGBA |
| 文件大小 | 155.3 KB |
| Sprite PPU | **707** |
| 世界单位大小 | **1.06 × 1.02 Unity units** |
| Sprite Pivot | (0.5, 0.3282) — 偏下 |
| 渲染颜色 | rgba(0.28, 0.43, 0.43, 1.0)（青绿色半透明） |
| 用途 | `Dodge_Sprite`（旧版轮廓残影，标注为 "used for old outline trail"） |

#### `SHIP_PLAYER_DODGE_HALF.png` — Dodge 半身剪影

| 属性 | 值 |
|------|-----|
| 分辨率 | **135 × 104 px** |
| 色彩模式 | RGBA |
| 文件大小 | 1.2 KB（极小，几乎是剪影/mask） |
| 用途 | Dodge 状态的半透明飞船剪影 |

#### `scheme3_tp.png` — 飞船全局鸟瞰图

| 属性 | 值 |
|------|-----|
| 分辨率 | **564 × 636 px** |
| 色彩模式 | RGBA |
| 文件大小 | 306.1 KB |
| Sprite PPU | **100** |
| 世界单位大小 | **5.64 × 6.36 Unity units** |
| 渲染颜色 | rgba(0, 0, 0, 1)（纯黑剪影） |
| Sort Layer | **2**（比飞船层 5 更底，渲染在最下面） |
| Sort Order | 5 |
| 用途 | `View` GO — 飞船整体黑色轮廓剪影，用于特殊视觉效果或选船界面 |

---

### 3.5 辅助图标

| 文件 | 分辨率 | 文件大小 | 用途 |
|------|--------|---------|------|
| `MARKER_PLAYER.png` | 131 × 147 px | 11.3 KB | 小地图玩家标记 |
| `reactor.png` | 64 × 64 px | — | 反应堆核心图标 |

---

## 四、材质与着色器

### 飞船专用材质

| 材质名 | 用于 | 着色器类型 |
|-------|------|---------|
| `PlayerShipHL` | 高光层（`Ship_Sprite_HL`） | 自定义 HL 着色器 |
| `Sprite-Lit-Default` | 主体实体层（`Ship_Sprite_Solid`） | Unity 内置 Sprite-Lit |
| `TeleportScheme` | 整体轮廓层（`View`） | 自定义 scheme 着色器 |

### Boost 特效材质

| 材质名 | 用途 |
|-------|------|
| `mat_boost_trail_glow` | Boost 尾迹辉光（Additive） |
| `mat_boost_ember_trail` | Boost 火星粒子（粒子系统） |
| `mat_boost_techno_flame` | Boost 机械火焰效果 |
| `mat_boost_trail_head` | Boost 尾迹头部高亮 |

---

## 五、飞船在 Unity 世界中的实际尺寸

基于 PPU=320 和贴图尺寸的计算：

```
飞船主体直径 = 430px ÷ 320 PPU = 1.34 Unity units ≈ 1.34 米（假设 1u = 1m）

飞船推进器宽 = 186px ÷ 320 PPU = 0.58 Unity units
飞船推进器高 = 96px ÷ 320 PPU = 0.30 Unity units

重力枪手形 = 0.36 × 0.50 Unity units
```

**结论**：GG 飞船视觉上很小——约 1.34×1.34 Unity units 的正方形边界框。这对应一个非常紧凑的 Top-Down 飞船，在屏幕上看起来约占屏幕宽度的 8-12%。

---

## 六、精灵变体体系（多皮肤架构）

GG 的飞船皮肤系统通过 `PlayerView.ShipSpritePack` 实现，每个 `ViewState` 对应一套三张精灵：

```csharp
// PlayerView.ShipSpritePack（来自 dump.cs）
{
    ViewState state;       // 状态标识
    float fadeDuration;    // 切换淡入淡出时长
    Sprite solidSprite;    // 主体贴图
    Sprite liquidSprite;   // 液体/发光贴图
    Sprite highlightSprite;// 高光贴图
    Vector3 spritesOffset; // 位置偏移
}
```

`GrabGun_Base` 系列的变体编号（_0 ～ _20）对应不同飞船皮肤，每套皮肤有独立的 Base（主体）+ Back（后层）+ Hand（手形）三件套。

---

## 七、PlayerView 组件架构

`PlayerView` MonoBehaviour 由以下模块组成：

| 模块 | 职责 |
|------|------|
| `PlayerViewCoreModule` | 核心状态管理（Blue/Red/Boost/Dodge 切换） |
| `PlayerViewEnergyModule` | 能量条视觉 |
| `PlayerViewBoostModule` | Boost 状态视觉特效 |
| `PlayerViewJumpModule` | 跳跃/层间传送演出 |
| `PlayerViewFluxyTrailModule` | 尾迹粒子（Boost 火焰流体） |
| `PlayerViewFluxyGrabModule` | 重力枪抓取视觉 |
| `PlayerViewDamageOverlayModule` | 受伤叠加效果 |
| `PlayerViewLQTrailModule` | 低质量粒子尾迹 |
| `PlayerViewTeleportModule` | 传送视觉 |
| `PlayerViewSpawnModule` | 出生/召回演出 |
| `PlayerViewHoldModule` | 长按进度圈 |

---

## 八、对 Project Ark 美术制作的参考建议

### 8.1 分辨率参考

| 部件 | 推荐分辨率 | GG 原值 | PPU 设置 |
|------|-----------|---------|---------|
| 飞船主体 | **256×256** 或 **512×512** | 430×430 | **320** |
| 高光层 | 同主体尺寸 | 430×430 | 320 |
| 尾推进器 | ~**128×64** | 186×96 | 320 |
| 武器附件/手形 | ~**64×128** | 130×164 | 320 |
| Dodge 残影 | ~**256×256** | 751×722 | 300-700 |
| 小地图标记 | **32×32** ～ **64×64** | 131×147 | — |

> **注**：Project Ark 当前使用 PPU=100，若要对齐 GG 的细节密度，建议飞船贴图 PPU 提升至 200-320。

### 8.2 层级组织建议

仿照 GG 的三层分离方案：

```
ShipRoot
├── Ship_Sprite_Back       (SortOrder -3, 尾推进器 / 翼)
├── Ship_Sprite_Liquid     (SortOrder -2, 发光/能量层)
├── Ship_Sprite_HL         (SortOrder -1, 高光层)
├── Ship_Sprite_Solid      (SortOrder  0, 主体实体层)  ← 主要可见部分
└── Ship_Sprite_Core       (SortOrder  1, 核心/驾驶舱)
```

### 8.3 Dodge 视觉

- GG 用一张 **完整飞船轮廓**（751×722，PPU=707）作为 Dodge 残影
- 渲染颜色设为 `rgba(0.28, 0.43, 0.43, 1.0)`（青绿色）实现残影色调
- 配合 `Alpha` 随时间衰减

### 8.4 Boost 材质

GG 的 Boost 特效用 4 个独立材质：`trail_glow`（Additive）、`ember_trail`、`techno_flame`、`trail_head`。Project Ark 至少需要：
1. 一个 **Additive Trail** 材质（引擎粒子流光）
2. 一个 **Glow 叠加** 材质（船体辉光增强）

---

## 九、附录：所有 GG 飞船相关纹理清单

### GrabGun_Base 系列（飞船主体）

| 文件名 | 分辨率 | 文件大小 |
|-------|--------|---------|
| `GrabGun_Base.png` | 430×430 | 43.9 KB |
| `GrabGun_Base_0.png` | 700×700 | 16.7 KB |
| `GrabGun_Base_1.png` | 430×430 | 47.6 KB |
| `GrabGun_Base_2.png` | 700×700 | 35.1 KB |
| `GrabGun_Base_3.png` | 700×700 | 26.5 KB |
| `GrabGun_Base_4.png` | 700×700 | 23.8 KB |
| `GrabGun_Base_5.png` | 430×430 | 35.2 KB |
| `GrabGun_Base_6.png` | 700×700 | 35.6 KB |
| `GrabGun_Base_7.png` | 700×700 | 16.0 KB |
| `GrabGun_Base_8.png` | 430×430 | **8.4 KB** (高光，信息稀疏) |
| `GrabGun_Base_9.png` | 430×430 | 41.2 KB ← **Glitch 飞船主体** |
| `GrabGun_Base_10.png` | 430×430 | 34.0 KB |
| `GrabGun_Base_11.png` | 430×430 | 48.9 KB |
| `GrabGun_Base_12.png` | 430×430 | 36.7 KB |
| `GrabGun_Base_13.png` | 430×430 | 10.3 KB |
| `GrabGun_Base_14.png` | 430×430 | 33.2 KB |
| `GrabGun_Base_15.png` | 700×700 | 17.3 KB |
| `GrabGun_Base_16.png` | 700×700 | 30.2 KB |
| `GrabGun_Base_17.png` | 700×700 | 17.2 KB |
| `GrabGun_Base_18.png` | 700×700 | 26.4 KB |
| `GrabGun_Base_19.png` | 700×700 | 26.5 KB |
| `GrabGun_Base_20.png` | 700×700 | 32.0 KB |

### GrabGun_Back 系列（飞船后层）

| 文件名 | 分辨率 | 文件大小 |
|-------|--------|---------|
| `GrabGun_Back.png` | 700×700 | 44.7 KB |
| `GrabGun_Back_1.png` | 186×96 | 21.7 KB |
| `GrabGun_Back_3.png` | 186×96 | **17.4 KB ← Glitch 后层** |
| `GrabGun_Back_4.png` | 700×700 | 31.3 KB |
| `GrabGun_Back_5.png` | 186×96 | 19.3 KB |
| `GrabGun_Back_6.png` | 186×96 | 14.0 KB |
| `GrabGun_Back_7.png` | 186×109 | 17.8 KB |
| `GrabGun_back_0.png` | 700×700 | 30.2 KB |
| `GrabGun_back_2.png` | 700×700 | 35.6 KB |

### GrabGun_Hand 系列（重力枪手形）

| 文件名 | 分辨率 | 文件大小 |
|-------|--------|---------|
| `GrabGun_Hand.png` | 700×700 | 29.5 KB |
| `GrabGun_Hand_0.png` | 130×164 | 19.7 KB |
| `GrabGun_Hand_1.png` | 700×700 | 40.0 KB |
| `GrabGun_Hand_2.png` | 110×184 | 14.8 KB |
| `GrabGun_Hand_3.png` | 130×164 | 14.5 KB |
| `GrabGun_Hand_4.png` | 700×700 | 27.2 KB |
| `GrabGun_Hand_5.png` | 130×164 | 17.2 KB |
| `GrabGun_Hand_6.png` | 700×700 | 36.2 KB |
| `GrabGun_Hand_7.png` | 130×164 | **18.4 KB ← Glitch 手形** |

---

## 十、提取方法说明

本文档数据通过以下方法获取：

1. **AssetRipper**（`C:\Temp\GG_Ripped`）：将 GG 的 Unity Asset Bundle 导出为完整 Unity 项目（YAML 格式的 `.prefab`、`.asset`、`.unity` 文件 + 原始 PNG 纹理）
2. **Il2CppDumper**（`C:\Temp\GalacticGlitch_dump\dump.cs`）：提取所有 C# 类、字段、方法签名（IL2CPP 后端，无方法体）
3. **Python 二进制分析**：直接读取 `GameAssembly.dll`，通过 x64 汇编 CALL 指令的 RVA 追踪函数调用链（确认 `Player.Update()` 在 `+0x46d` 处调用 `ToStateForce(IsBoostState=3)`）
4. **PIL (Pillow)**：批量读取所有 PNG 文件获取分辨率、色彩模式、文件大小

原始数据存放于 `C:\Temp\GG_Ripped\ExportedProject\Assets\Texture2D\`，本次分析共检索 **3226 张** PNG 纹理文件。
