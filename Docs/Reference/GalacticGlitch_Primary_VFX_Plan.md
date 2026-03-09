
# GalacticGlitch Primary 状态美术表现 — 完整研究与实现方案

> **研究对象**：GalacticGlitch 参考游戏中，飞船处于 Primary 武器状态（State 3/4/8）时的全套美术表现，以 `Primary_4.png` 贴图为核心视觉锚点。
> **目标**：为 Project Ark 的 Primary 武器系统提供可直接落地的美术实现方案。

---

## 一、研究结论速览

| 维度 | 原版实现 | 核心特征 |
|------|---------|---------|
| 飞船贴图 | 3层 SpriteRenderer（Solid/Liquid/Highlight） | 紫色主题，PPU=320 |
| 飞船颜色 | Highlight: `#8B17FF`，Transition: `#AB00FF` | 高饱和紫色 |
| 子弹本体 | 2层 SpriteRenderer（GUN_PRO_T1 + Circle） | 橙色发光 |
| 子弹拖尾 | 4个 ParticleSystem（Trail + trail_1/2/3） | 多层叠加，渐细 |
| 命中爆炸 | 14个 ParticleSystem（flash/glow/sparks/smoke等） | 橙色爆炸 |
| 开枪闪光 | Light2D 动画（intensity 0.7→0，10帧） | 绿色光晕 |
| 子弹状态机 | Startup1→Startup2→FlyLoop / AnyState→Explode | 4状态 |

---

## 二、飞船视觉层（Ship Visual Layers）

### 2.1 三层 SpriteRenderer 架构

飞船由三个叠加的 SpriteRenderer 组成，State 3/4/8（Primary 状态）使用相同贴图组：

| 层级 | 字段名 | 贴图 | 作用 |
|------|--------|------|------|
| 底层 | `shipSolidSR` | `Primary_4.png` | 飞船主体轮廓（不透明） |
| 中层 | `shipLiquidSR` | `Primary.png` | 液态流动效果（半透明动画） |
| 顶层 | `shipHLSR` | `Primary_6.png` | 高光/发光层（Additive混合） |

**贴图规格**：
- 尺寸：430×430 px，RGBA 格式
- PPU（Pixels Per Unit）：**320**
- 世界尺寸：约 1.34×1.34 Unity units
- 主色调：紫色系（RGB 128,0,160 / 192,0,224 为主）

### 2.2 颜色主题（来自 PlayerSkinDefault.asset）

```
shipHLSR Tint Color:    #8B17FF  (r:0.545, g:0.090, b:1.000)
transitionColor:        #AB00FF  (r:0.671, g:0.000, b:1.000)
energyWave ColorMin:    #EE33FF  (r:0.933, g:0.200, b:1.000, a:0.8)
energyWave ColorMax:    #8632FC  (r:0.525, g:0.196, b:0.988, a:0.8)
energyGlow ColorMin:    #F580FF  (r:0.961, g:0.502, b:1.000, a:0.8)
energyGlow ColorMax:    #B37EFC  (r:0.702, g:0.494, b:0.988, a:0.8)
```

### 2.3 状态切换动画

飞船 Animator Controller（`Player.controller`）中，Primary 相关状态：

```
MainAttackState      → ChangeViewState(3)  [fadeDuration: 0.2s]
MainAttackFireState  → ChangeViewState(4)  [fadeDuration: 0s，即时切换]
```

- **State 3**（蓄力/瞄准）：0.2s 淡入过渡到 Primary 贴图组
- **State 4**（开火中）：即时切换（无过渡），保持相同贴图
- **State 8**（Boost+Primary）：同 State 3/4，相同贴图组

---

## 三、开枪闪光（Muzzle Flash / Light Burst）

### 3.1 Light2D 动画（FirstShootStartup.anim）

每次开枪时触发一个 **10帧（0.167s）** 的光源闪光动画：

```yaml
组件: Light2D (URP 2D)
颜色: RGB(0, 1, 0.06) → 绿色光晕（与紫色飞船形成互补对比）
强度曲线: 0.7 → 0  (线性衰减，10帧)
持续时间: 0.167s
```

> **设计意图**：绿色光晕与紫色飞船形成视觉对比，强调"能量释放"感。

### 3.2 ShootStartup 动画

`ShootStartup.anim` 仅控制 `SurroundingGravGunState` 的激活状态（与 Primary 无关，用于 GravGun 状态隐藏）。

---

## 四、子弹 Prefab 完整结构（PlayerPrimaryBulletPea_lvl1）

### 4.1 GameObject 层级

```
PlayerPrimaryBulletPea_lvl1 (Root)
├── [SpriteRenderer] GUN_PRO_T1          ← 子弹主体 (85×110px, mat: PrimaryBulletStylisticPea_lvl1)
├── [SpriteRenderer] Bullet_Sprite       ← 子弹核心 (alpha动画控制)
├── [SpriteRenderer] GUN_PRO_T1_VARIATION ← 变体层 (alpha=0.157, 半透明)
├── [SpriteRenderer] ObjectBubble        ← 气泡光晕 (Circle 256×256px, alpha=0.102)
│
├── Trail/                               ← 拖尾容器
│   ├── [PS] Trail                       ← 主拖尾 (smooth_circle_additive)
│   ├── Trail_Thin/RotationPivot
│   │   ├── [PS] trail_1                 ← 细拖尾层1 (glow_circle)
│   │   ├── [PS] trail_2                 ← 细拖尾层2 (glow_circle)
│   │   └── [PS] trail_3                 ← 细拖尾层3 (glow_circle)
│
├── Muzzle/                              ← 炮口特效（发射时激活）
│   └── vfx_spider_enemy_muzzle
│
├── Hit/                                 ← 命中爆炸（命中时激活）
│   ├── [PS] Hit (source bullet)         ← 命中源粒子 (Sprite-Lit-Default)
│   ├── [PS] flash                       ← 闪光 (explosion_circle)
│   ├── [PS] flash_above                 ← 上层闪光 (explosion_circle)
│   ├── [PS] circle_core                 ← 核心圆圈 (explosion_circle_add)
│   ├── [PS] rays_core                   ← 射线 (spark_additive)
│   ├── [PS] sparks                      ← 火花 (spark_additive)
│   ├── [PS] glow                        ← 光晕 (hot_glow_additive)
│   ├── [PS] glow_above                  ← 上层光晕 (hot_glow_additive)
│   ├── [PS] smoke_direct                ← 直射烟雾 (Smoke_multiply)
│   ├── [PS] smoke_around                ← 环绕烟雾 (Smoke_multiply)
│   ├── [PS] bullet_explode_L            ← 左爆炸碎片 (smooth_circle_additive)
│   ├── [PS] bullet_explode_R            ← 右爆炸碎片 (smooth_circle_additive)
│   ├── [PS] ps_smoke_dust_big           ← 大烟尘 (mat_eo_fakelit_puffs)
│   └── [PS] ps_smoke_dust_big_1         ← 大烟尘2 (mat_eo_fakelit_puffs)
│
└── vfx_spider_enemy_startup             ← 启动特效（Startup阶段激活）
```

### 4.2 子弹状态机（PlayerBulletPrimary_Pea.controller）

```
[Entry] → Startup1 (0.4s) → Startup2 (0.4s) → FlyLoop (∞循环)
AnyState → Explode (3.65s) [条件: IsHit=true]
```

| 状态 | 动画 | 时长 | 激活内容 |
|------|------|------|---------|
| Startup1 | PlayerBulletStartup1.anim | 0.4s | vfx_spider_enemy_startup ON, Trail OFF, Hit OFF |
| Startup2 | PlayerBulletStartup2.anim | 0.4s | 过渡阶段 |
| FlyLoop | PlayerBulletFlyLoop.anim | ∞ | Trail ON, Muzzle ON, Hit OFF |
| Explode | PlayerBulletExplode.anim | 3.65s | Hit ON, Trail OFF, Bullet_Sprite OFF |

---

## 五、拖尾系统（Trail System）

### 5.1 四层拖尾参数

| 粒子系统 | 材质 | 粒子大小 | 生命周期 | 发射率 | 特点 |
|---------|------|---------|---------|--------|------|
| Trail | smooth_circle_additive | 0.02 | 0.25s | 0 (Burst) | 极细主轴线 |
| trail_1 | glow_circle | 0.2 | 0.25s | 0 (Burst) | 中等光晕层 |
| trail_2 | glow_circle | 0.2 | 0.15s | 0 (Burst) | 短生命光晕 |
| trail_3 | glow_circle | 0.2 | 0.08s | 0 (Burst) | 极短核心光 |

> **关键技术**：所有拖尾使用 **Burst 发射**（rate=0），由子弹移动触发粒子生成，形成连续拖尾效果。`Trail_Thin/RotationPivot` 子节点可旋转，产生螺旋拖尾变化。

### 5.2 材质说明

| 材质名 | Shader | 混合模式 | 颜色 |
|--------|--------|---------|------|
| smooth_circle_additive | URP Particles Unlit | Additive | 白色（由PS颜色控制） |
| glow_circle | URP Particles Unlit | Additive | 白色 |

---

## 六、命中爆炸系统（Hit Explosion System）

### 6.1 爆炸分层（按视觉层次）

**Layer 1 - 即时闪光（0~0.05s）**
- `flash` / `flash_above`：explosion_circle 材质，大小1.2/1.0，生命0.05s
- 效果：瞬间白色/橙色圆形闪光

**Layer 2 - 核心扩散（0~0.2s）**
- `circle_core`：explosion_circle_add，大小0.7，生命0.2s
- `rays_core`：spark_additive，大小0.9，速度0.1，生命0.2s
- 效果：橙色圆圈扩散 + 短射线

**Layer 3 - 火花飞溅（0~0.3s）**
- `sparks`：spark_additive，大小0.5，速度**20**，生命0.3s
- 效果：高速飞溅的橙色火花

**Layer 4 - 光晕残留（0~0.8s）**
- `glow` / `glow_above`：hot_glow_additive，大小2.0，生命0.6/0.8s
- 效果：橙色大光晕，缓慢消散

**Layer 5 - 烟雾（0~1s）**
- `smoke_direct`：Smoke_multiply，大小1.0，速度5，生命1s
- `smoke_around`：Smoke_multiply，大小1.0，速度0.5，生命1s
- 效果：定向烟雾 + 环绕烟雾

**Layer 6 - 碎片（0~0.12s）**
- `bullet_explode_L/R`：smooth_circle_additive，大小0.2，速度1，生命0.12s
- 效果：子弹碎裂的小圆点

**Layer 7 - 大烟尘（0~0.6s，持续6s动画）**
- `ps_smoke_dust_big/1`：mat_eo_fakelit_puffs，大小0.5/0.2，速度8/0.5
- 效果：假光照烟尘，增加体积感

### 6.2 爆炸颜色主题

原版爆炸颜色为**橙色**（来自 PrimaryBulletGlow 材质 Tint: `r:1, g:0.6, b:0`），与飞船紫色形成强烈对比，突出命中感。

---

## 七、材质系统总结

| 材质名 | Shader | 用途 | 关键参数 |
|--------|--------|------|---------|
| PrimaryBulletStylisticPea_lvl1 | 自定义 | 子弹主体 | 无贴图（程序化） |
| PrimaryBulletGlow | 自定义 | 子弹发光 | Tint: 橙色(1,0.6,0), ColPow:1.2 |
| SmoothCoreParticleAdditive | 自定义 | 核心粒子 | Tint: 白色, ColPow:1.2 |
| smooth_circle_additive | URP Particles | 拖尾/碎片 | Additive混合 |
| glow_circle | URP Particles | 拖尾光晕 | Additive混合 |
| explosion_circle | URP Particles | 爆炸圆圈 | 标准混合 |
| explosion_circle_add | URP Particles | 爆炸核心 | Additive混合 |
| spark_additive | URP Particles | 火花/射线 | Additive, BaseMap: spark贴图 |
| hot_glow_additive | URP Particles | 热光晕 | Additive混合 |
| Smoke_multiply | URP Particles | 烟雾 | Multiply混合 |
| mat_eo_fakelit_puffs | 自定义 | 假光照烟尘 | 体积感烟雾 |

---

## 八、Project Ark 实现方案

### 8.1 飞船视觉层实现

**已有基础**：Project Ark 已有 `ShipView` / `PlayerView` 架构，三层 SpriteRenderer 结构。

**需要实现**：
1. 在 `ShipStateSO` 或对应数据结构中，为 Primary 状态配置：
   - `solidSprite` = Primary_4 对应贴图
   - `liquidSprite` = Primary 对应贴图  
   - `highlightSprite` = Primary_6 对应贴图
   - `fadeDuration` = 0.2s（State 3），0s（State 4）

2. Highlight 层颜色：`#8B17FF`（紫色）

### 8.2 开枪闪光实现

```csharp
// 在 WeaponFireHandler 或 ShipWeaponView 中
[SerializeField] private Light2D muzzleLight;
[SerializeField] private float muzzleFlashIntensity = 0.7f;
[SerializeField] private float muzzleFlashDuration = 0.167f;

private void OnFire()
{
    // 使用 PrimeTween（项目规范）
    muzzleLight.intensity = muzzleFlashIntensity;
    Tween.Custom(muzzleLight, muzzleFlashIntensity, 0f, muzzleFlashDuration,
        (light, val) => light.intensity = val);
}
```

**Light2D 配置**：
- Color: `#00FF0F`（绿色，与紫色飞船互补）
- Intensity: 0（默认），开枪时动画到 0.7 再衰减
- Range: 约 2-3 Unity units

### 8.3 子弹 Prefab 实现方案

#### 8.3.1 子弹本体层

```
PrimaryBullet (Root)
├── SpriteRenderer [GUN_PRO_T1]          ← 主体，85×110px，PPU=100
│   └── Material: PrimaryBulletCore (Additive, 橙色Tint)
├── SpriteRenderer [BulletGlow]          ← 发光层，Circle 256×256px
│   └── Material: PrimaryBulletGlow (Additive, Tint: 1,0.6,0)
│   └── Color.a = 0.102（半透明）
```

#### 8.3.2 拖尾实现（4层 ParticleSystem）

```csharp
// Trail 配置（以 trail_1 为例）
ParticleSystem.MainModule main = trailPS.main;
main.duration = 5f;
main.loop = true;
main.startLifetime = 0.25f;
main.startSize = 0.2f;
main.startSpeed = 0f;  // 不自主移动，跟随子弹

ParticleSystem.EmissionModule emission = trailPS.emission;
emission.rateOverTime = 0f;
emission.rateOverDistance = 30f;  // 按距离发射，形成连续拖尾
```

**拖尾材质**：使用 URP Particles Unlit + Additive 混合，颜色由 `Color over Lifetime` 控制（白→橙→透明）。

#### 8.3.3 命中爆炸实现

推荐使用 **对象池** 管理爆炸 Prefab，命中时从池中取出，播放完毕后归还。

```csharp
// 爆炸 Prefab 结构（按优先级分组）
HitExplosion (Root)
├── [PS] Flash          ← 即时闪光，size=1.2, life=0.05s, Additive
├── [PS] CircleCore     ← 核心圆圈，size=0.7, life=0.2s, Additive  
├── [PS] Sparks         ← 火花，size=0.5, speed=20, life=0.3s, Additive
├── [PS] Glow           ← 光晕，size=2.0, life=0.6s, Additive
├── [PS] Smoke          ← 烟雾，size=1.0, speed=5, life=1s, Multiply
└── [PS] SmokeDust      ← 烟尘，size=0.5, speed=8, life=0.6s, FakeLit
```

**总持续时间**：约 3.65s（与原版 PlayerBulletExplode.anim 一致）

### 8.4 子弹状态机实现

```
Animator States:
├── Startup (0.4s)   → 显示启动特效，隐藏拖尾
├── FlyLoop (∞)      → 显示拖尾，隐藏命中特效
└── Explode (3.65s)  → 显示命中特效，隐藏本体和拖尾
    [Trigger: IsHit]
```

或者用代码直接控制（推荐，更灵活）：

```csharp
public class PrimaryBulletView : MonoBehaviour
{
    [SerializeField] private GameObject trailRoot;
    [SerializeField] private GameObject hitRoot;
    [SerializeField] private SpriteRenderer bulletSprite;
    
    public void OnSpawn()
    {
        trailRoot.SetActive(false);
        hitRoot.SetActive(false);
        bulletSprite.enabled = true;
        // 0.4s 后激活拖尾
        UniTask.Delay(400, cancellationToken: destroyCancellationToken)
            .ContinueWith(() => trailRoot.SetActive(true)).Forget();
    }
    
    public void OnHit(Vector2 hitPoint)
    {
        trailRoot.SetActive(false);
        bulletSprite.enabled = false;
        hitRoot.SetActive(true);
        // 播放所有命中粒子
        foreach (var ps in hitRoot.GetComponentsInChildren<ParticleSystem>())
            ps.Play();
        // 3.65s 后归还对象池
        UniTask.Delay(3650, cancellationToken: destroyCancellationToken)
            .ContinueWith(() => ReturnToPool()).Forget();
    }
}
```

---

## 九、颜色方案总结（Project Ark 可直接使用）

### Primary 武器颜色主题

```
飞船 Highlight:     #8B17FF  (紫色)
飞船 Transition:    #AB00FF  (亮紫色)
能量波动 Min:       #EE33FF  (品红紫)
能量波动 Max:       #8632FC  (深紫)
开枪闪光:           #00FF0F  (绿色，互补色)
子弹发光:           #FF9900  (橙色，对比色)
爆炸核心:           #FF9900  (橙色)
爆炸光晕:           #FF6600  (深橙)
```

### 视觉设计逻辑

```
飞船（紫色）→ 开枪（绿色闪光，互补）→ 子弹（橙色，对比）→ 爆炸（橙色，延续）
```

这种颜色设计创造了强烈的视觉节奏：**紫色蓄力 → 绿色释放 → 橙色冲击**。

---

## 十、实现优先级与 MVP 拆分

### Phase 1 - MVP（最小可玩版本）
- [ ] 飞船 Primary 状态贴图切换（3层 SpriteRenderer）
- [ ] 子弹本体 SpriteRenderer（GUN_PRO_T1 + 发光层）
- [ ] 基础拖尾（1个 ParticleSystem，glow_circle 材质）
- [ ] 命中时隐藏子弹本体

### Phase 2 - 完整拖尾
- [ ] 4层拖尾系统（Trail + trail_1/2/3）
- [ ] 拖尾颜色渐变（白→橙→透明）
- [ ] 开枪 Light2D 闪光

### Phase 3 - 命中爆炸
- [ ] 完整 7 层爆炸系统
- [ ] 爆炸 Prefab 对象池
- [ ] 爆炸动画时序控制

### Phase 4 - 细节打磨
- [ ] 子弹 Startup 动画（0.4s 出现过渡）
- [ ] 飞船状态切换淡入淡出（0.2s）
- [ ] 能量模块粒子颜色（紫色波动）

---

## 附录：关键文件路径（参考资产）

```
贴图：
  F:\UnityProjects\ReferenceAssets\GalacticGlitch\GG_Ripped\ExportedProject\Assets\Texture2D\
  - Primary_4.png (430×430, 主体)
  - Primary.png   (430×430, 液态层)
  - Primary_6.png (430×430, 高光层)
  - GUN_PRO_T1.png (85×110, 子弹主体)
  - Circle.png    (256×256, 子弹光晕)

材质：
  F:\UnityProjects\ReferenceAssets\GalacticGlitch\GG_Ripped\ExportedProject\Assets\Material\
  - PrimaryBulletGlow.mat (Tint: 橙色)
  - glow_circle.mat
  - smooth_circle_additive.mat
  - spark_additive.mat
  - explosion_circle.mat

Prefab：
  F:\UnityProjects\ReferenceAssets\GalacticGlitch\GG_Ripped\ExportedProject\Assets\GameObject\
  - PlayerPrimaryBulletPea_lvl1.prefab

动画：
  F:\UnityProjects\ReferenceAssets\GalacticGlitch\GG_Ripped\ExportedProject\Assets\AnimationClip\
  - PlayerBulletStartup1.anim (0.4s)
  - PlayerBulletFlyLoop.anim (∞)
  - PlayerBulletExplode.anim (3.65s)
  - FirstShootStartup.anim (0.167s, Light2D)
  - MainAttackState.anim
  - MainAttackFireState.anim

皮肤数据：
  F:\UnityProjects\ReferenceAssets\GalacticGlitch\GG_Ripped\ExportedProject\Assets\MonoBehaviour\
  - PlayerSkinDefault.asset
```
