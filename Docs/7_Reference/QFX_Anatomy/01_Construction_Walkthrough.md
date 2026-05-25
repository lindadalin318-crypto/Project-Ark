# QFX 特效构造拆解 — 从 `VFX_Cyber_Projectile.prefab` 走通五层链路

> **样本**：`Assets/QFX/ProjectilesFX/VFX_Prefabs/Projectiles_Full/VFX_Cyber_Projectile.prefab`
> **目的**：让任何接手 Project Ark VFX 的人，在 30 分钟内理解 QFX 是如何构造一颗"赛博紫弹"的，并能照此复刻新主题。
> **方法**：直接读 prefab YAML / material YAML / shader 源码反推组装流程。
> **整理日期**：2026-05-23

---

## 〇、TL;DR — 一颗 QFX 子弹的真身

```
VFX_Cyber_Projectile.prefab
  └── 21 个 ParticleSystem 子物体（每个只做一件事）
        └── 引用 Material（13 张，全部在 Materials/Cyber/）
              └── 引用 Shader（4 选 1：Particles / Particles_Cutout / Aura / Trail）
                    + 引用贴图（来自 Textures/Glow|Trail|Flare|Smoke|Mask|Noise|Other）
                    + 一组 HDR 颜色参数（_TintColor / _EmissionColor / _Color）
```

**核心思想**：QFX 不是用一个粒子系统画出"科技弹"，而是用 **21 个非常简单、各只做一件事**
的粒子层叠加出来。加在一起肉眼看到的就是华丽的"赛博能量弹"。这是工业级 VFX 的通用做法。

---

## 一、整体构成范式（五层洋葱）

任意一个 QFX prefab 都符合这个分层结构：

```
┌─────────────────────────────────────────────────────────┐
│ Prefab（你拖进场景的对象）                              │
│  ├── N 个 ParticleSystem 子物体（每个是一"层"）          │  ← Hierarchy
│  │     └── Renderer 槽位引用 Material                   │  ← 渲染器
│  │           └── 引用 Shader + 贴图 + 参数              │  ← 渲染逻辑
│  │                 └── Shader 内部对贴图做 UV 动画/混合 │  ← 数学计算
│  └── 父物体上的 Transform / 生命周期管理                │
└─────────────────────────────────────────────────────────┘
```

下面逐层展开。

---

## 二、第一层：Prefab 的 21 个粒子子层

通过抓取 prefab YAML 中所有 `m_Name` 字段，得到 `VFX_Cyber_Projectile.prefab` 的子物体列表
（按视觉职责归类）：

| 类别 | 子层名 | 视觉职责 |
|---|---|---|
| **核心弹体** | `VFX_Cyber_Projectile`（根） | 容器 |
|              | `Glow` | 中心大光晕 |
|              | `Star` | 中心星形高光 |
|              | `Circle` | 弹体外圈光环 |
| **轨道装饰** | `Rectangles_Along` ×4 | 沿前进方向飞行的方块（赛博风） |
|              | `Circles_Along` | 沿前进方向的小圆圈 |
|              | `Glow_Along` | 沿前进方向的拉丝光 |
| **火花层**   | `Sparks_Along` | 沿弹道方向的火花 |
|              | `Sparks_Stretched_Along` | 拉伸火花（速度感） |
|              | `Sparks_Stretched` | 短促拉伸火花 |
|              | `Sparks` | 散射小火花 |
| **拖尾层**   | `Trail` | 长拖尾 |
| **闪光段（Flash）** | `Flare_Grid` / `Flare_Square` / `Flare` | 发射瞬间的格栅 / 方块 / 十字星光 |
| **命中段（Impact）** | `Hit` / `Impact` | 命中爆炸（嵌入在 _Full 里） |

> **关键观察**：`_Full` prefab = `Flash + Projectile + Impact` 三段一次性打包。
> `_Only` 子集 prefab 只有中间的 `Projectile` 段。
> 这就是为什么仓库里同时存在 `Projectiles/` 和 `Projectiles_Full/` 两个文件夹。

---

## 三、第二层：每个粒子层的 ParticleSystem 模块组合

每个子层都是一个 `ParticleSystem`。通过对 prefab YAML 中模块出现次数的统计，
**21 个 PS 全部启用了下列 6 个模块**：

| 模块 | 干什么 | 在 Cyber 弹里的常见用法 |
|---|---|---|
| `InitialModule` | 起始参数：寿命、初速度、初始大小、初始旋转 | "我活 0.5s，初始大小 0.3，速度 5" |
| `EmissionModule` | 发射节奏：rate / burst | "持续每秒喷 30 个" 或 "瞬间爆发 20 个" |
| `ShapeModule` | 发射形状：球 / 圆锥 / 盒 / 边 / Mesh | Cyber 弹常用 Cone（向前喷）和 Sphere（散射） |
| `NoiseModule` | 噪声扰动：让粒子走 S 形而不是直线 | 让火花有"乱飞"的感觉 |
| `TrailModule` | 给每个粒子额外画一条 Trail | 长拖尾子层就靠这个 |
| `SubModule` | 子发射器：粒子死时再生成新粒子 | "弹体每飞 0.1s 抛 1 个小火花"，连锁反应 |

**这 6 个模块的组合 = 视觉个性的 80%。** 例：

- 想做"星河拖尾"→ 关掉 Noise，加大 Trail，调慢速度
- 想做"暴怒火花"→ 加大 Noise.Strength，缩短 lifetime，提高 emission rate
- 想做"分裂弹"→ 用 SubModule 在 Death 时调用另一个 PS

---

## 四、第三层：Material — 把 Shader 和贴图绑在一起

样本：`Assets/QFX/ProjectilesFX/VFX_Resources/Materials/Cyber/MFX_Projectile_1.mat`

这一份材质的"灵魂参数"如下（直接读自 YAML）：

```yaml
m_Shader:        PFX_Particles.shader             # 用哪个 shader
_MainTex:        Textures/Other/FX_Projectile_3.png    # 弹体形状（一张白底剪影）
_EmissionMap:    Textures/Glow/FX_Glow_1.TGA           # 光晕通道（一张径向白色渐变）
_Color:          (5.99, 5.99, 5.99, 1)            # HDR 高亮白（>1 才能触发 Bloom）
_EmissionColor:  (14.18, 1.51, 23.97, 1)          # HDR 紫红 — 赛博紫的真身
_TintColor:      (1, 1, 1, 1)                     # 二次染色，运行时可改
_SrcBlend = 5 (SrcAlpha)                          # 混合方程左半
_DstBlend = 1 (One)                               # 混合方程右半 → SrcAlpha+One = 加法叠加
_ZWrite = 0                                       # 不写深度（粒子标配）
_SoftParticlesEnabled = 1                         # 与场景物体相交时柔化（Built-in）
```

### 这一份材质讲了什么故事？

1. **形状从哪来**：`_MainTex` = 一张灰度形状图（菱形、方块、星星 ...）。
   它只决定"这一团光长什么样"。
2. **亮度从哪来**：`_EmissionMap × _EmissionColor`（紫色 HDR）= 让中心特别亮的光晕。
3. **颜色从哪来**：`_TintColor × ParticleSystem 自身的 Color over Lifetime`。
   **Tint 是运行时调色入口** —— 写代码改这个就能换色，不需要改贴图。
4. **为什么会发光**：HDR 颜色（>1.0 的 r/g/b 数值）+ 加法混合 + 摄像机开 Bloom 后处理。
   **三个缺一不可**：任何一个关掉，"科技感发光"就消失。

> **README 强调"必须开 HDR 摄像机 + Bloom"** 的根因 ——
> 没有 Bloom，那个 23.97 的紫数值会被 clamp 到 1，就只是普通紫色矩形，不再是赛博能量。

### 主题包的"槽位映射"

`Materials/Cyber/` 下 13 张材质，与 prefab 引用的 13 个 GUID **1:1 对应**：

| GUID | 材质文件 |
|---|---|
| 464c29432b5e8d94d87353199c0f081e | `MFX_Flare.mat` |
| 13e94c3830b1d1048b9888a5dbb7fe5a | `MFX_Glow.mat` |
| 479cc8eb196636c4a8a4d1cd79ef8d32 | `MFX_Grid.mat` |
| cecbde5e62e164545bd52d5e366cd85b | `MFX_Hit.mat` |
| 233eadaa49263274b8f5924e6fe9be55 | `MFX_Projectile_1.mat` |
| 7e2e9dc8f24b5d94cb8a4d8e45f856f1 | `MFX_Projectile_2.mat` |
| 19f8ee714c20297499479f9860f0c715 | `MFX_Spark_1.mat` |
| dbbc800df8ab497449f4c7ab97985b41 | `MFX_Spark_2.mat` |
| a8e5c1700c6c67c4dbab59579d662873 | `MFX_Square_1.mat` |
| fc06f290fc5b99843bc5b9127a4fb3ac | `MFX_Square_2.mat` |
| d22b30cf37eb39d469c73b7a06a91ee5 | `MFX_Square_3.mat` |
| d9e00010d832603438492278fe512613 | `MFX_Star.mat` |
| 802e5d8547392564e99ef6433e26aa2d | `MFX_Trail.mat` |

→ **每个主题包都是这 13 个槽位的同名副本，命名严格统一**。
→ 复刻只需"复制目录、改色"。

---

## 五、第四层：Shader — 真正画像素

QFX 4 个 shader 中，`PFX_Particles` 最常用，干的事极简单：

```glsl
// 伪代码（剥离 Amplify 节点图后）
float4 frag(VertexOutput i) {
    float2 uv     = i.texcoord * _MainTex_ST;
    float4 tex    = tex2D(_MainTex, uv);          // 读形状图
    float4 color  = _TintColor * tex * i.color * unity_ColorSpaceDouble;
    //              ↑材质染色   ↑形状  ↑粒子颜色  ↑色彩空间补偿
    return color;
}
```

**就这么简单** —— 它做的全部事就是：
> "把 `_MainTex` 形状图，用 `_TintColor × 粒子颜色` 染色后输出。"

剩下三个 shader 是这个 base 的扩展：

| Shader | 多了什么 | 适用场景 |
|---|---|---|
| `PFX_Particles` | base：形状 × 颜色 | 火花、闪光、命中、弹体 |
| `PFX_Particles_Cutout` | + `_NoiseTexture` + `_Cutout`：用噪声扰动遮罩边缘做"溶解感" | 边缘溶解的烟雾、消散弹 |
| `PFX_Aura` | + `panner` UV 动画 + `_MainTexturePower`：让贴图动起来（光环旋转） | 充能光环、护盾、aura |
| `PFX_Trail` | 双层 panner（两条不同速度的滚动）+ 可选的遮罩 alpha | 拖尾抖动质感 |

> **4 个 shader 撑起 181 个材质** —— 它们提供了 4 种"动画/形变模式"，
> 剩下的差异化全交给贴图 + 颜色参数。

---

## 六、第五层：贴图（PNG / TGA）

QFX 把所有贴图按"职责"分目录：

| 目录 | 数量 | 用途 |
|---|---|---|
| `Textures/Trail/` | 18 | 拖尾形状（细长条、衰减条） |
| `Textures/Glow/` | 14 | 径向光晕（赋给 `_EmissionMap`） |
| `Textures/Smoke/` | 14 | 烟雾扰流 |
| `Textures/Flare/` | 50 | 闪光、星光、十字光（最多） |
| `Textures/Mask/` | 8 | 用于 cutout 的遮罩 |
| `Textures/Noise/` | 20 | 噪声/扰动图 |
| `Textures/Other/` | 余 | 弹体形状（如 `FX_Projectile_3.png`） |

**关键**：贴图是**纯黑白灰度** / 单色径向，不带预设颜色 —— 颜色全靠材质里的
`_TintColor / _EmissionColor` 后期染。

> 这是工业级做法：**1 张灰度贴图能服务 N 套主题**，不用每个主题重画一份。

---

## 七、把链路反过来走 —— "Cyber 紫色科技弹" 怎么诞生？

倒推制作流程（这就是"复刻"的步骤）：

### Step 1 · 设计语言（脑内）
- 主题：科技 / 赛博
- 主色：HDR 紫红
- 形状语言：方块 / 网格 / 直线火花（不是水滴 / 烟雾）
- 节奏：高速、密集、闪烁

### Step 2 · 准备素材（贴图层）
- 弹体形状：菱形 / 六边形 / 方块的灰度 PNG → `Textures/Other/`
- 光晕：径向白渐变 TGA → `Textures/Glow/`
- 拖尾条：水平细长渐变 → `Textures/Trail/`
- 火花：1×8 拉长星点 → `Textures/Flare/`

### Step 3 · 建材质（Material 层）
为每种视觉职责建一个 `.mat`，命名严格按主题槽位规范：

| 材质名 | shader | _MainTex | _Color/_EmissionColor |
|---|---|---|---|
| `MFX_Projectile_1` | `PFX_Particles` | 弹体形状 | HDR 紫 (14, 1.5, 24) |
| `MFX_Glow` | `PFX_Particles` | 径向光晕 | HDR 白 × 紫 |
| `MFX_Trail` | `PFX_Trail` | 长条拖尾 | 渐变紫 |
| `MFX_Hit` | `PFX_Particles` | 圆形 burst | 高亮紫 |
| `MFX_Flare / MFX_Star / MFX_Spark_*` ... | 同上 | 各自形状贴图 | 同主题色 |

→ 这就是 `Materials/Cyber/` 那 13 张材质的由来。

### Step 4 · 搭粒子层（ParticleSystem 层）
新建若干空 GameObject（参考 §二 的 21 层职责清单），每个挂一个 ParticleSystem：

- `Glow` / `Star` / `Circle` ← 中心三件套
- `Rectangles_Along` ×4 ← 不同距离 / 角度的方块流
- `Sparks*` ×4 ← 火花组
- `Trail` ← 拖尾
- `Flare_*` ×3 ← 闪光（Flash 段）
- `Hit / Impact` ← 命中段（Impact）

每个 PS 配上对应材质（在 Renderer 模块的 Material 槽），调好 6 个核心模块。

### Step 5 · 父子打包成 Prefab
把这些 GO 全塞进根 GameObject `VFX_<Theme>_Projectile` 下，存成 `.prefab`。
完成。

---

## 八、🎯 复刻实战 — 把 Cyber 改成 Project Ark 的 "Anomaly 异常紫" 主题

这是**最低成本路径**，例化前面所有理论：

```bash
# 1. 复制主题包目录
cp -r Assets/QFX/ProjectilesFX/VFX_Resources/Materials/Cyber \
      Assets/QFX/ProjectilesFX/VFX_Resources/Materials/Anomaly

# 2. 在 Unity 里批量打开 Anomaly/*.mat，只改 4 个字段：
#    _Color / _EmissionColor / _TintColor / 必要时换 _MainTex
#    例：把 (14.18, 1.51, 23.97) 改成 (20.0, 2.0, 8.0) 得到"血红异常色"

# 3. 复制 prefab
cp Assets/QFX/ProjectilesFX/VFX_Prefabs/Projectiles_Full/VFX_Cyber_Projectile.prefab \
   Assets/QFX/ProjectilesFX/VFX_Prefabs/Projectiles_Full/VFX_Anomaly_Projectile.prefab

# 4. 在 Unity 里打开新 prefab，把 21 个 PS 的 Renderer Material 槽位
#    一个个换成 Anomaly/MFX_*.mat（按命名 1:1 对应，不会乱）
```

**全程不写一行代码、不动 shader、不画新贴图**。
这是 QFX 架构的扩展性红利。

---

## 九、为什么这套构造方式能"很容易扩展" —— 四个机制总结

| 机制 | 让什么变得简单 |
|---|---|
| **形状/颜色解耦**：贴图灰度 + 材质 HDR 颜色 | 1 张贴图 → N 个主题颜色，不用重画 |
| **shader 标准化**：4 shader 的属性命名跨主题完全一致 | 一个 ScriptableObject 就能驱动所有材质参数 |
| **粒子层职责单一**：21 层每层只做一件事 | 想加一层"碎片飞溅"只是再加一个 PS，不动其他 20 层 |
| **三段式打包**：Flash + Projectile + Impact 独立 prefab | 武器不同阶段用不同 prefab，组合自由 |

---

## 十、🚀 学习 / 上手梯度（推荐路径）

| 阶段 | 任务 | 验证 |
|---|---|---|
| **理解** | 在 Unity 里打开 `VFX_Cyber_Projectile.prefab`，逐个点 21 个子层，看 Inspector 里每个 PS 的模块设置 | 能说出每层的视觉职责 |
| **小改** | 复制 `MFX_Projectile_1.mat`，只把 `_EmissionColor` 改成绿色，看预览 | 知道颜色参数实时生效 |
| **复刻** | 按 §八 的步骤做一个 `Anomaly` 主题副本 | 4 元素之一就有了 |
| **创造** | 拿现有 6 类贴图 + 4 shader，**新建一个 QFX 没有的形态**（如"激光束" = `PFX_Trail.shader` + 拉伸 PS + 高 emission rate） | 真正进入"扩展"层 |
| **体系化** | 写一个 `VfxThemeSO`，把 4 套主题色板序列化进去，运行时换皮 | 进入"数据驱动 VFX"阶段 |

---

## 附 · 关键文件速查表

| 用途 | 路径 |
|---|---|
| 完整 Cyber prefab（Flash+Projectile+Impact） | `Assets/QFX/ProjectilesFX/VFX_Prefabs/Projectiles_Full/VFX_Cyber_Projectile.prefab` |
| 仅 Projectile 段子集 | `Assets/QFX/ProjectilesFX/VFX_Prefabs/Projectiles/VFX_Cyber_Projectile_Only.prefab` |
| Cyber Flash | `Assets/QFX/ProjectilesFX/VFX_Prefabs/Flashes/VFX_Cyber_Flash.prefab` |
| Cyber Impact | `Assets/QFX/ProjectilesFX/VFX_Prefabs/Impacts/VFX_Cyber_Impact.prefab` |
| 主题色板（Cyber 13 mat） | `Assets/QFX/ProjectilesFX/VFX_Resources/Materials/Cyber/MFX_*.mat` |
| 4 个 shader | `Assets/QFX/ProjectilesFX/VFX_Resources/Shaders/PFX_*.shader` |
| 6 类贴图 | `Assets/QFX/ProjectilesFX/VFX_Resources/Textures/{Trail,Glow,Smoke,Flare,Mask,Noise,Other}/` |
