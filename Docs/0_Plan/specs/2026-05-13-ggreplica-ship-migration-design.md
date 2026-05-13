# GGReplica 飞船迁移设计 Spec

> 日期：2026-05-13  
> 状态：Draft for user review  
> 范围：从 `/Users/dada/Documents/Quark/ReferenceAssets/Galactic Glitch` 选择性迁移玩家飞船外观、状态切换、Boost/Dodge 特效、音效与手感参数，构建隔离实验 Prefab。  
> 关键原则：**先验证、后合入；先隔离、后主链；不直接搬 `Player.prefab`。**

---

## 1. 目标

本次目标是建立一个可 Play Mode 验证的 **Galactic Glitch 飞船复刻实验体**：

```text
Assets/_Prefabs/Ship/Ship_GGReplica.prefab
```

第一版覆盖战斗五态：

1. `Normal`
2. `Boost`
3. `Dodge`
4. `Primary / Fire`
5. `Primary + Boost`

复刻目标包含：

- 飞船多层贴图结构；
- 状态驱动的 sprite pack 切换；
- Boost / Dodge 特效和音效；
- 尽量贴近 GG 的 dodge / boost 手感；
- 与现役 `Ship.prefab` 并行存在，可 A/B 对比；
- 不污染现役 `Ship/VFX` 主链。

---

## 2. 非目标

本次不做：

- 不直接迁移 GG 的 `Player.prefab` 全量结构；
- 不直接迁移 GG 的 `Player.cs` / `PlayerView.cs` 等 MonoBehaviour；
- 不依赖 GG 的 Fluxy 运行时；
- 不把 DevX 反编译 shader 直接设为现役主链；
- 不替换现有 `Assets/_Prefabs/Ship/Ship.prefab`；
- 不改 `SampleScene.unity` 的正式玩家入口；
- 不把参考资源状态从 `Reference` 提升为 `Live`，除非通过本 spec 的验收。

---

## 3. 参考来源

### 3.1 外部参考目录

```text
/Users/dada/Documents/Quark/ReferenceAssets/Galactic Glitch/
├── DevXUnity/
│   ├── Sprite/
│   ├── Texture2D/
│   ├── Material/
│   ├── AudioClip/
│   ├── CLG/
│   └── Shader Graphs/
├── DevXUnity_exported/
│   ├── Assets/Prefab/Player.prefab
│   ├── Assets/Images/
│   ├── Assets/Materials/
│   ├── Assets/AnimatorController/Player.controller
│   ├── Assets/AnimationClip/
│   └── Assets/MonoBehaviour/
└── Scripts_dnSpyEx/
    └── GeneralAssembly_direct/GeneralAssembly/
```

### 3.2 项目内既有参考文档

```text
Docs/7_Reference/GameAnalysis/GalacticGlitch_Structure_Analysis.md
Docs/7_Reference/GameAnalysis/GalacticGlitch_BoostTrail_VFX_Plan.md
Docs/7_Reference/GameAnalysis/GalacticGlitch_Primary_VFX_Plan.md
Docs/2_TechnicalDesign/Ship/ShipVFX_CanonicalSpec.md
Docs/2_TechnicalDesign/Ship/ShipVFX_AssetRegistry.md
Implement_rules.md
```

---

## 4. 总体架构

采用 **Replica 隔离层**。

```text
Assets/_Prefabs/Ship/
├── Ship.prefab              # 现役主链，不动
└── Ship_GGReplica.prefab    # 新实验 Prefab

Assets/_Data/Ship/
├── GGReplicaShipVisualProfile.asset
└── GGReplicaShipFeelProfile.asset

Assets/_Art/Ship/GGReplica/
├── Sprites/
├── Materials/
├── Shaders/
│   ├── DevX_Trial/
│   └── ProjectArk_Rebuilt/
└── Audio/
```

运行时结构：

```text
Ship_GGReplica.prefab
├── 复用现有 ShipMotor / ShipAiming / InputHandler / ShipHealth
├── 复用现有 ShipView 主链思想
├── 新增 GGReplicaShipViewAdapter
├── 新增 GGReplicaShipFeelAdapter
├── 引用 GGReplicaShipVisualProfileSO
└── 引用 GGReplicaShipFeelProfileSO
```

原则：

- `Ship.prefab` 不动；
- `ShipStatsSO` 不动；
- `ShipPrefabRebuilder` 的现役主链不被 GGReplica 隐式覆盖；
- GGReplica 使用独立 builder / audit / test scene；
- 验证成功后再决定是否合入现役主链。

---

## 5. 状态映射设计

### 5.1 GG 五态

| GGReplica State | 触发条件 | Solid | Liquid | Highlight | Fade |
|---|---|---|---|---|---:|
| `Normal` | 默认移动 | `Movement_10` | `Movement_3` | `Movement_21` | 0.2 |
| `Boost` | Boost 按下/持续 | `Boost_2` | `Boost_16` | `Boost_8` | 0.2 |
| `Dodge` | Dodge 触发期间 | 复用当前 solid 或 Dodge ghost | `player_test_fire` 可作 ghost | 可选 | 瞬时 |
| `Fire` | 正在主武器射击 | `Primary_4` | `Primary` | `Primary_6` | 0.2 |
| `FireBoost` | 射击 + Boost 同时满足 | `Primary_4` | `Primary` | `Primary_6` | 0.0 |

### 5.2 优先级规则

同一帧多个状态同时满足时，按以下优先级选择视觉状态：

```text
Dodge > FireBoost > Boost > Fire > Normal
```

理由：

- Dodge 是强反馈动作，必须压过所有视觉状态；
- FireBoost 是组合态，必须压过单独 Boost / Fire；
- Boost 比 Fire 更影响机体轮廓；
- Fire 不应打断 Dodge。

---

## 6. 资产迁移设计

### 6.1 第一批 PNG

目标目录：

```text
Assets/_Art/Ship/GGReplica/Sprites/
```

必迁：

```text
Movement_10.png
Movement_3.png
Movement_21.png
Boost_2.png
Boost_16.png
Boost_8.png
Primary_4.png
Primary.png
Primary_6.png
player_test_fire.png
GrabGun_Back_3.png
reactor.png
```

注意文件名映射：

| DevXUnity_exported | 项目内规范名 |
|---|---|
| `Movement_d10.png` | `Movement_10.png` |
| `Movement_d3.png` | `Movement_3.png` |
| `Movement_d21.png` | `Movement_21.png` |
| `Boost_d2.png` | `Boost_2.png` |
| `Boost_d16.png` | `Boost_16.png` |
| `Boost_d8.png` | `Boost_8.png` |
| `Primary_d4.png` | `Primary_4.png` |
| `Primary.png` | `Primary.png` |
| `Primary_d6.png` | `Primary_6.png` |
| `GrabGun_back_d3.png` | `GrabGun_Back_3.png` |

如果 `DevXUnity/Sprite/` 和 `DevXUnity_exported/Assets/Images/` 都存在同名资源，优先使用：

1. `DevXUnity/Sprite/`：用于 Sprite 源图；
2. `DevXUnity_exported/Assets/Images/*.meta`：用于读取 PPU / Pivot / importer 参数参考。

### 6.2 第一批 WAV

目标目录：

```text
Assets/_Art/Ship/GGReplica/Audio/
```

必迁：

```text
SND_PLAYER_BOOST.wav
SND_PLAYER_BOOST_IGNITE.wav
PLAYER_DODGE.wav
PLAYER_NORMAL_SHOT.wav
PLAYER_FIRST_SHOT.wav
PLAYER_LAST_SHOT.wav
PLAYER_DEATH.wav
```

### 6.3 Importer 设置

Importer 不直接复制 `.meta`，但应按 GG `.meta` 复刻关键参数：

| 参数 | 目标 |
|---|---|
| Texture Type | Sprite |
| Sprite Mode | Single |
| Pivot | 参考 GG `.meta` |
| PPU | 参考 GG `.meta`；飞船主图优先 320 |
| Mipmap | Off |
| Alpha Is Transparency | On |
| Compression | None 或 High Quality，第一版以视觉正确优先 |

---

## 7. DevX Shader Compatibility Spike

### 7.1 目标

在构建 `Ship_GGReplica.prefab` 前，先验证 DevX 提供的 shader 是否能直接或部分采用。

候选来源：

```text
DevXUnity/CLG/CLG_PlayerShipHighlight.shader
DevXUnity/Sprite Shaders URP/Lit Sprite/Color/*PlayerLightSource*.shader
DevXUnity/Shader Graphs/*PlayerLQTrail*.shader
DevXUnity/Material/PlayerShipHL.mat
DevXUnity_exported/Assets/Materials/PlayerShipHL.mat
```

### 7.2 隔离目录

```text
Assets/_Art/Ship/GGReplica/Shaders/DevX_Trial/
Assets/_Art/Ship/GGReplica/Materials/DevX_Trial/
```

### 7.3 成功等级

| 等级 | 含义 | 后续处理 |
|---|---|---|
| L0 | shader 能导入但不能编译 | 只做参考，不采用 |
| L1 | 能编译但视觉明显错误 | 抄属性 / Blend 模式，重写 |
| L2 | 能正确渲染主效果，但少量参数不生效 | 局部采用，封装成 Project Ark 材质 |
| L3 | 几乎可直接使用 | 迁入正式 `GGReplica/Shaders`，但改名、登记 owner |

### 7.4 验证对象

```text
GGShaderTrialRoot
├── Sprite_HL_Test
├── Sprite_Liquid_Test
└── BoostTrail_Test
```

测试贴图：

- `Primary_6`：高光层；
- `Primary`：液体/发光层；
- `vfx_boost_techno_flame`：Boost trail。

### 7.5 约束

- 不覆盖现役 shader；
- 不导入 DevX 全 shader 目录；
- 不让 DevX shader 成为现役主链；
- DevX 反编译 shader 若出现 `!!! Allow restore shader...` 或平台限定代码，默认只做参考；
- 必须有 fallback：使用 Project Ark 自己的 URP shader 复刻 `_Tint / _Intensity / _Smooth / Blend SrcAlpha One`。

---

## 8. Prefab 设计

目标：

```text
Assets/_Prefabs/Ship/Ship_GGReplica.prefab
```

结构：

```text
Ship_GGReplica
├── ShipVisual
│   ├── Ship_Sprite_Back
│   ├── Ship_Sprite_Liquid
│   ├── Ship_Sprite_HL
│   ├── Ship_Sprite_Solid
│   ├── Ship_Sprite_Core
│   ├── Dodge_Sprite
│   └── BoostTrailRoot_GGReplica
├── ShipMotor
├── ShipBoost
├── ShipAiming
├── ShipHealth
├── GGReplicaShipViewAdapter
└── GGReplicaShipFeelAdapter
```

### 8.1 Builder

新增 Editor 工具：

```text
ProjectArk > Ship > GG Replica > Build Experimental Prefab
```

只写：

```text
Assets/_Prefabs/Ship/Ship_GGReplica.prefab
```

禁止写：

```text
Assets/_Prefabs/Ship/Ship.prefab
Assets/Scenes/SampleScene.unity
```

### 8.2 视觉 Profile

新增：

```text
Assets/_Data/Ship/GGReplicaShipVisualProfile.asset
```

字段：

```text
NormalPack
BoostPack
DodgePack
FirePack
FireBoostPack
DodgeGhostSprite
BoostIgniteAudio
BoostLoopAudio
DodgeAudio
FireAudio
```

每个 `ShipSpritePack`：

```text
state
fadeDuration
solidSprite
liquidSprite
highlightSprite
spritesOffset
```

---

## 9. 手感复刻设计

### 9.1 数据来源

GG 的 `Player.cs` 可提供字段结构参考：

```text
afterBoostDrag
nearPickupDrag
dragChangeTime
dodgeForce
dodgeForceAfterDodge
dodgeInvulnerabilityTime
dodgeCacheTime
dodgeRechargeTime
speedModAfterDodge
speedModAfterDodgeTime
timeForActionAfterEndDodge
maxDodgeCharges
```

但 IL2CPP 导出无法可靠还原所有序列化值。因此：

- 字段结构参考 GG；
- 初始数值由 Project Ark Play Mode 调参得到；
- 参数放入独立 SO，避免污染现役手感。

### 9.2 Feel Profile

新增：

```text
Assets/_Data/Ship/GGReplicaShipFeelProfile.asset
```

字段：

```text
Dodge:
- dodgeForce
- dodgeForceAfterDodge
- dodgeInvulnerabilityTime
- dodgeCacheTime
- dodgeRechargeTime
- maxDodgeCharges
- speedModAfterDodge
- speedModAfterDodgeTime
- timeForActionAfterEndDodge

Boost:
- boostSpeedMultiplier
- afterBoostDrag
- dragChangeTime
- boostStartImpulse
- boostDecayDuration
- boostIgniteDuration
```

### 9.3 Runtime Adapter

新增：

```text
GGReplicaShipFeelAdapter.cs
```

职责：

- 仅挂在 `Ship_GGReplica.prefab`；
- 读取 `GGReplicaShipFeelProfile`；
- 运行时覆盖 `Rigidbody2D` / `ShipMotor` / `ShipBoost` 的相关参数；
- Play Mode 退出后不写回 SO；
- 不修改现役 `ShipMotor` 默认数值。

---

## 10. 测试场景

新增：

```text
Assets/Scenes/GGReplicaShipTest.unity
```

包含：

```text
GGReplicaTestRoot
├── Ship.prefab                # A/B 对照
├── Ship_GGReplica.prefab       # 实验体
├── DummyEnemy
├── SimpleObstacle
├── Boost/Dodge Distance Markers
└── TestHUD
```

快捷键：

| 输入 | 行为 |
|---|---|
| `F1` | 切换现役 Ship / GGReplica |
| `F2` | 强制 Normal |
| `F3` | 强制 Boost |
| `F4` | 强制 Dodge |
| `F5` | 强制 Fire |
| `F6` | 强制 FireBoost |

---

## 11. 分阶段实施

### Phase 0：资产审计

产物：

```text
Docs/6_Diagnostics/GGReplica_ShipAsset_Audit.md
```

内容：

- GG 原始飞船贴图全量清单；
- 项目已迁入贴图清单；
- 缺失项；
- DevX shader 候选清单；
- 音效清单；
- 可迁 / 参考 / 禁止迁 分类。

验收：

- 清楚知道第一批复制哪些文件；
- 清楚每个文件的来源目录；
- 清楚对应项目目标路径。

### Phase 1：复制 PNG / WAV 资产

产物：

```text
Assets/_Art/Ship/GGReplica/Sprites/*
Assets/_Art/Ship/GGReplica/Audio/*
```

验收：

- Unity 能导入全部 Sprite；
- PPU / Pivot 与审计表一致；
- 音效可播放；
- 现役 `Ship.prefab` 无变化。

### Phase 1.5：DevX Shader Spike

产物：

```text
Assets/_Art/Ship/GGReplica/Shaders/DevX_Trial/*
Assets/_Art/Ship/GGReplica/Materials/DevX_Trial/*
Docs/6_Diagnostics/GGReplica_ShaderSpike_Report.md
```

验收：

- 至少测试 3 个候选 shader；
- 每个候选标注 L0-L3；
- 给出最终策略：直接采用 / 局部采用 / 参考重写；
- 不污染现役 shader。

### Phase 2：构建 `Ship_GGReplica.prefab`

产物：

```text
Assets/_Prefabs/Ship/Ship_GGReplica.prefab
Assets/_Data/Ship/GGReplicaShipVisualProfile.asset
```

验收：

- 五态贴图切换正确；
- 三层 Sprite 可见；
- Dodge ghost 可显示；
- 无 Missing Script / Missing Sprite；
- 现役 `Ship.prefab` 无变化。

### Phase 3：Boost / Dodge VFX + 音效

产物：

```text
GGReplicaBoostVfxAdapter.cs
GGReplicaDashVfxAdapter.cs
```

验收：

- Boost ignite 音效同步；
- Boost trail 持续；
- Dodge 音效同步；
- Dodge 残影可见；
- 对象池回收无颜色/alpha/trail 残留。

### Phase 4：GG 手感配置实验

产物：

```text
Assets/_Data/Ship/GGReplicaShipFeelProfile.asset
GGReplicaShipFeelAdapter.cs
```

验收：

- Dodge 有明显冲量；
- Dodge 有无敌窗口；
- Dodge 后短时间速度/拖拽变化；
- Boost 起步/结束有拖拽变化；
- 可 A/B 比较现役手感与 GGReplica 手感。

### Phase 5：测试场景与 A/B 对比

产物：

```text
Assets/Scenes/GGReplicaShipTest.unity
```

验收：

- 10 秒内能切换两艘船；
- 五态都可触发；
- Console 无 error；
- 无 Missing 引用；
- 用户可直接 Play Mode 判断像不像 GG。

---

## 12. 风险与防御

| 风险 | 防御 |
|---|---|
| 原 GG Player.prefab 依赖大量 Missing Script | 不迁原 Prefab，只重建子集 |
| DevX shader 不能编译 | Phase 1.5 先做 spike；失败则参考重写 |
| 手感数值无法完整还原 | 使用字段结构 + Play Mode 调参 |
| 现役 Ship/VFX 主链被污染 | 所有写入限定 `Ship_GGReplica.prefab` 与 `GGReplica/` 目录 |
| 资产命名混乱 | Phase 0 建映射表，统一规范名 |
| 商业版权风险 | 标记为 internal prototype/reference，最终商业版需替换或确认授权 |

---

## 13. Done 定义

本 spec 全部完成时，应满足：

1. `Ship_GGReplica.prefab` 可独立进入 Play Mode；
2. Normal / Boost / Dodge / Fire / FireBoost 五态视觉切换可见；
3. Boost / Dodge 的视觉和音效完成第一版；
4. GGReplica 手感参数独立于现役 Ship；
5. 可在测试场景 A/B 对比；
6. 现役 `Ship.prefab`、`SampleScene.unity`、现役 `Ship/VFX` authority 主链无污染；
7. 所有新增资源、脚本、prefab 都登记到对应文档或诊断报告。

---

## 14. 用户审批点

本 spec 获批后，下一步不是直接写代码，而是创建详细 implementation plan，拆成具体文件级任务：

```text
Phase 0 plan → Phase 1 plan → Phase 1.5 shader spike plan → ...
```

实施前仍需遵守：

- 修改/创建文件后追加 ImplementationLog；
- Unity Prefab 结构由 Editor 工具生成，不手写 `.meta`；
- 需要大量 scene/prefab 写入时优先用 Unity MCP / Editor automation；
- 不直接迁移外部 `.meta`；
- 不直接覆盖现役主链。
