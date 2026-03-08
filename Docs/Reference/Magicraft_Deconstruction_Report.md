# Magicraft 完整解构报告

> 解构日期：2026-03-08  
> 解构工具：Il2CppDumper v6.7.46 + UnityPy 1.25.0  
> 游戏引擎：Unity 6000.0.62f1（IL2CPP 后端）  
> 本文件仅供内部研究参考，不得用于商业侵权用途。

---

## 一、技术栈分析

| 维度 | 内容 |
|------|------|
| Unity 版本 | 6000.0.62f1（Unity 6） |
| 渲染管线 | URP（`Unity.RenderPipelines.Universal.Runtime.dll` 829KB） |
| 脚本后端 | IL2CPP（不可直接反编译逻辑，只能提取类型签名） |
| ECS/DOTS | 大量使用 `Unity.Entities.dll`（1395KB）、`Unity.Physics.dll`（733KB）、`Unity.Collections.dll`（560KB） |
| 数学库 | `Unity.Mathematics.dll`（1436KB） |
| 输入系统 | New Input System（`Unity.InputSystem.dll` 1200KB） |
| 物理动画 | `Rukhanka.Runtime.dll`（354KB）— 高性能骨骼动画 |
| 物理扩展 | `Unity.Physics.Stateful`（12个类）— DOTS 物理事件 |
| JSON | `Newtonsoft.Json.dll`（819KB） |
| 平台集成 | Steamworks.NET、B站直播互动（`BiliOneSDKMgr`、`BLiveMgr`） |
| 第三方VFX | EpicToonFX、SciFiArsenal、CartoonCoffee、ETFXPEL |
| 日志 | `PlayerLogger`（18个类）+ PlayerLogger.Events（13个类） |
| 混淆 | Beebyte.Obfuscator（8个类）— 已做代码混淆，逻辑难还原 |

**关键发现：大量使用 DOTS/ECS（962个 Authoring/System/Job 类）**，这是 Magicraft 高性能弹幕的基础，法术碰撞检测和移动系统均跑在 Jobs 线程上。

---

## 二、代码架构总览

### 总类型数量

- **Assembly-CSharp.dll**：3,055 个类型（含嵌套类）
- 核心游戏类（无命名空间）：约 2,925 个
- 第三方库命名空间：PlayerLogger(18)、EpicToonFX(13)、SciFiArsenal(10) 等

### 按系统分类

| 系统 | 类数量 | 描述 |
|------|--------|------|
| DOTS/ECS | ~962 | Authoring、System、Job、Baker |
| 战斗/法术 | ~953 | Spell、Boss攻击、投射物 |
| 敌人/AI | ~470 | Monster 1-112、Elite 1-17、Boss 1-13 |
| UI | ~273 | 战斗UI、背包UI、商店UI |
| Buff/状态 | ~235 | Curse、Buff、Effect |
| 关卡/场景 | ~181 | Room、Door、Level |
| 背包/物品 | ~108 | Item、Relic、Gear、Chest |
| 资源管理 | ~95 | Mgr(各类管理器) |
| 玩家 | ~33 | PlayerController、PlayerMgr |

---

## 三、核心系统深度分析

### 3.1 法术系统（Magicraft 核心）

#### SpellType 枚举
```
Missile = 0   // 投射物型法术（飞行子弹）
Summon  = 1   // 召唤型法术（生成实体）
Enhance = 2   // 强化型法术（修改其他法术）
Passive = 3   // 被动型法术（持续效果）
```

#### SpellColorType（元素属性）
```
Player  = 0   // 玩家系（中性）
Frozen  = 1   // 冰冻系
Monster = 2   // 怪物系
Mucus   = 3   // 黏液系（减速）
Venom   = 4   // 毒液系（持续伤害）
Fire    = 5   // 火焰系（灼烧）
Thunder = 6   // 雷电系（链式伤害）
Void    = 7   // 虚空系（穿透）
```

#### 法术编号规划
| 编号段 | 类型 | 数量 |
|--------|------|------|
| 1001-1031 | 基础法术（Missile类） | ~31种 |
| 1099 | 特殊基础法术 | 1种 |
| 2001-2007 | 召唤类法术（Summon） | 7种 |
| 3007-3129 | 强化类法术（Enhance，带颜色变体） | ~16种 |
| 4004-4027 | 被动类法术（Passive） | ~12种 |
| 9001-9045 | 特殊/Boss法术 | ~30种 |
| 10201 | 超大型特殊法术 | 1种 |
| 40121, 90012 | 极少数特殊法术 | 2种 |

**总法术数量：约 90-100 种**

#### SpellBase 基类（所有法术的父类）
关键属性（从 IL2CPP 签名提取）：
- `damageRatio` / `finalDamageRatio`：伤害倍率
- `speedRatio` / `finalSpeedRatio`：速度倍率
- `radiusRatio` / `finalRadiusRatio`：范围倍率
- `knockbackRatio`：击退倍率
- `bonusDuration` / `finalDurationRatio`：持续时间
- `overalCriticalChance`：暴击率
- `spellFrozenTime` / `spellVenomTime` / `spellMucusTime`：元素状态持续时间
- `spellBurnTime` / `burnHpRatioPerSeconds`：燃烧伤害
- `isThroughWall`：穿墙
- `enableAroundPlayer`：绕玩家旋转
- `criticalDragDamagePercent` + `criticalDragPullForce`：暴击吸引效果
- `FromEcho`：是否来自回响效果
- `createReboundEffect`：创建弹跳特效

### 3.2 魔杖系统（Wand）

#### WandSlotType
```
Normal = 0  // 普通槽（主要法术槽）
Post   = 1  // 后置槽（触发条件释放的法术槽）
```

#### WandConfig 核心数据结构
```
id, icon, iconH             // 标识与图标
shootInterval               // 射击间隔（法术释放频率）
coolDown                    // 冷却时间（弹夹耗尽后）
angle                       // 散射角
maxMP, mpRecovery           // 最大MP和回复速度
shootCount                  // 一次射击的法术数量
damageCorrection            // 全局伤害修正
criticalChance              // 基础暴击率
normalSlotIsLock[]          // 哪些普通槽是锁定的
postSlotIsLock[]            // 哪些后置槽是锁定的
PostslotMoveChargeRatio     // 后置槽：移动时充能比率
PostslotKillEnemyChargeRatio// 后置槽：击杀充能
PostslotSpellHitChargeRatio // 后置槽：命中充能
PostslotCriticalHitChargeRatio // 后置槽：暴击充能
PostslotTakeDamageChargeRatio  // 后置槽：受伤充能
```

#### Wand 实例关键方法
- `TryShoot()`：尝试射击（检查MP和间隔）
- `ShootSpellGroup()`：执行法术组发射
- `ShootNormalSlotsSpellGroup()`：普通槽法术发射
- `ShootPostSlotsSpell()`：后置槽法术发射（触发条件满足时）
- `StartCharge()` / `ReleaseCharge()` / `CancelCharge()`：蓄力系统
- `RefreshAutoSpell()` / `RefreshHammer()` / `RefreshBiAnBlades()`：被动自动法术刷新
- `SpawnLaserCrystal()` / `SpawnBiAnBlades()` / `SpawnUmbrella()`：特殊被动法术生成
- `CalcLightningChainFinalStats()`：雷电链伤害计算
- `EnterNextGroup()`：法术组切换（循环遍历法术组）

### 3.3 玩家控制系统

#### PlayerController 关键功能
- 双摇杆控制（`inputLeftStick` + `inputRightStick`）
- `Shoot()` + `ShootSlowdownHandle()`：射击时速度降低
- `Move()` + `Rotate()` + `Knockback()`：基础移动
- `DrinkPotion()`：喝药水
- `DashOverHeatRegister()`：冲刺过热机制
- `Alpha1-7Performed()`：快捷键切换魔杖（7把魔杖）
- `WandUpPerformed()` / `WandDownPerformed()`：魔杖切换
- `PotionUpPerformed()` / `PotionDownPerformed()`：药水切换
- `TakeDamageAmaze()`：受伤眩晕
- `SetFrozen()` / `SetUnfrozen()`：冻结状态
- `Theme6Reposition()`：第6主题专用重定位（特殊关卡机制）

### 3.4 物品/背包系统

#### ItemType 枚举
```
Wand    = 0   // 魔杖
Spell   = 1   // 法术
Relic   = 2   // 遗物
Potion  = 3   // 药水
Resource = 4  // 资源（金币等）
Curse   = 5   // 诅咒
```

#### 重要物品类
- `Item`、`ItemAuthoring`：物品基础组件
- `GearSystem`、`GearAuthoring`：装备系统（DOTS）
- `GearPickUpBuffer`：拾取缓冲区

### 3.5 诅咒系统（Curse）

发现的诅咒类型：
- `Curse_CantShootEnterRoom`：进房间后一段时间不能射击
- `Curse_DarkView`：视野变暗
- `Curse_InjuredCantShoot`：受伤后不能射击
- `Curse_RandomBomb`：随机炸弹（DOTS实现）

诅咒命令：
- `AddCursePaymentCommand`：添加诅咒并扣代价
- `AddOrRemoveCurseFreeCommand`：免代价增减诅咒

### 3.6 敌人系统

#### 规模
- **普通怪物**：Monster 1-56（无 46），Monster 101-112，Monster 301-306，Monster 301-306，Monster 995-999（特殊/剧情怪）
- **精英怪**：Elite 1-17（共 17 种精英敌人）
- **Boss**：Boss 1-13，Boss 99（特殊交互 Boss，疑似早期解锁功能）

#### Boss 分析（按复杂度）
- **Boss 1-4**：简单Boss，少量子组件
- **Boss 5**：复杂（手/触手/气泡/地面/毛发等多部件）
- **Boss 6**：极复杂（炮/激光/陨石/地震/子实体等大量机制）
- **Boss 13**：最复杂（潜艇/鱼雷/导弹/重力炸弹/镰刀等丰富机制）
- **Boss 99**：特殊（有 `BuyGame` 交互，可能是早期Demo的"购买游戏"Boss）

### 3.7 队友系统（Teammate）

- **Teammate 1-5**：5个可同行的队友
- 每个队友都有：基础类、融合控制器（FuseController）、融合形态部件
- 融合系统：`TeammateFuseSystem`、`TeammateFuseRequestBuffer`、`TeammateFusePairBuffer`

### 3.8 管理器架构（Mgr 系列）

| 管理器 | 职责 |
|--------|------|
| `GameMgr` | 游戏总控制器 |
| `DataMgr` | 数据加载/配置管理 |
| `LevelMgr` | 关卡管理 |
| `BattleMgr` | 战斗管理 |
| `CampMgr` | 营地（大厅）管理 |
| `ObjPoolMgr` | 对象池 |
| `UIBattleMgr` / `UICampMgr` / `UIMgr` | UI 管理 |
| `AudioMgr（MusicMgr/SEMgr）` | 音乐/音效管理 |
| `PlayerMgr` | 玩家数据 |
| `TagMgr` | 标签管理 |
| `LayerMgr` | 层级管理 |
| `TextMgr` | 文本/本地化 |
| `EventMgr` | 事件总线 |
| `StateVariableMgr` | 状态变量（进度/解锁） |
| `ModMgr` | Mod 支持！ |
| `ScriptableObjMgr` | SO 资源管理 |
| `DotweenMgr` | DOTween 管理 |
| `TimeScaleMgr` | 时间缩放（暂停/慢动作） |
| `SteamAchievementMgr` | Steam 成就 |
| `BiliOneSDKMgr` / `BLiveMgr` | B站直播互动 |

---

## 四、美术资产清单

### 4.1 导出结果

| 类型 | 数量 | 存储路径 |
|------|------|----------|
| Texture2D | 9,189 张（427 MB） | `D:\ReferenceAssets\Magicraft_Art\Texture2D\` |
| Sprite | 5,379 张 | `D:\ReferenceAssets\Magicraft_Art\Sprites\` |
| AudioClip | 833 个 | `D:\ReferenceAssets\Magicraft_Art\Audio\` |

### 4.2 资产分类

#### 高价值参考资产：法术特效序列帧
- 文件模式：`10011_Hit_Fire_*.png`、`10011_Hit_Frozen_*.png`、`10011_Hit_Monster_*.png`、`10011_Hit_Mucus_*.png`
- 这是按元素分类的命中特效帧序列
- 对 Project Ark 的"命中特效 + 元素属性"系统有直接参考价值

#### 高价值参考资产：法术图标
- `1002_Spell.png`、`1002_SpellVoid.png`（有元素变体版本）
- `1003_Spell 1.png`、`1008_Spell*.png`、`9012_Spell*.png`
- `9012_Spell_monster.png` vs `9012_Spell_player.png`（分别是怪物版和玩家版，有高分辨率 H 版）

#### 高价值参考资产：UI 元素
- `BloodUI.png`、`CoinUI.png`、`CoreUI.png`、`CrystalUI.png`：血量/货币/核心/水晶 HUD 图标
- `CastSpellCharge.png`：法术蓄力 UI
- `EF_ItemCurse.png` / `EF_ItemCurseH.png`：诅咒物品特效
- `BLiveGift_Relic.png`：B站礼物遗物图标
- `DruidRingInner.png` / `DruidRingOuter.png`：德鲁伊戒指内外层（旋转动画分层）

#### 参考资产：操作引导图
- `GuideImage1-4_Gamepad/Keyboard.png`：4张操作引导图（手柄/键盘两套）
- `GuideImage_Battle_*.png`：战斗引导图（含PS手柄、Switch Deck、移动端版本）

### 4.3 对 Project Ark 的参考价值

| 资产类型 | Project Ark 应用场景 | 价值评级 |
|----------|----------------------|----------|
| 法术命中特效序列帧 | 星图投射物命中反馈（元素效果视觉） | ★★★★★ |
| 法术图标（SpellColorType分层） | 星图部件的颜色属性视觉语言参考 | ★★★★☆ |
| UI 框架图（HUD 图标） | 热量条/星图 UI 的视觉风格参考 | ★★★☆☆ |
| 旋转分层特效（DruidRing等） | 伴星轨道、光帆效果的分层动画参考 | ★★★★☆ |
| 音效库（833个） | 战斗音效参考 | ★★★☆☆ |

---

## 五、项目架构总结（对 Project Ark 的架构启示）

### 5.1 Magicraft 的核心创新

1. **DOTS 驱动的弹幕系统**  
   法术飞行、碰撞检测全部跑在 Unity Jobs 上，实现了屏幕内数千法术同时飞行。Project Ark 如果扩展弹幕密度需考虑类似方案。

2. **后置槽（Post Slot）+ 多种充能触发条件**  
   不只是"放法术"，还有一套"触发器"机制：移动/击杀/命中/暴击/受伤/时间 均可为后置槽充能。这与 Project Ark 的"星图编排"有相似设计目标，但触发器层更丰富。

3. **法术颜色系统（SpellColorType）**  
   8 种元素属性对应 8 种视觉色彩，同一法术有不同颜色变体（如 `9012_Spell_player.png` vs `9012_Spell_monster.png`）。Project Ark 可考虑为星图部件增加"属性色"视觉标识。

4. **Wand 的 MP 系统**  
   Wand 有 `maxMP`、`mpRecovery`、`shootInterval`、`coolDown` 四个时间/资源参数。射击消耗MP，MP耗尽进入冷却，比简单的"CD"更有深度。可类比 Project Ark 的热量系统。

5. **队友融合（Fuse）系统**  
   5个队友各有融合形态，融合通过 DOTS 异步处理。这是一个复杂的多人协作视觉效果系统。

### 5.2 可直接复用的设计参考

| Magicraft 设计 | Project Ark 对应位置 | 具体建议 |
|----------------|---------------------|----------|
| 后置槽充能触发条件 | `WeaponTrack` 的激活逻辑 | 为星图轨道增加"触发条件"：暴击/命中/受伤/热量满 |
| 法术颜色属性（8种） | `StarChartItemSO` | 增加 `ElementType` 属性，对应视觉配色 |
| 诅咒系统（6种诅咒） | 关卡风险/奖励机制 | 可作为 Roguelike 路径选择的"负面 buff" |
| Rune 系统（红/绿/蓝） | 被动法术槽 | 三色符文提供不同被动加成 |
| 7把魔杖快捷键切换 | 星图多轨道 | 快速切换激活轨道的快捷键方案 |
| `SpellBase.FromEcho` | `EchoWave` 投射物 | 回响类投射物可继承原始投射物的属性 |

---

## 六、AssetRipper 完整解包结果

### 6.1 导出统计（`D:\ReferenceAssets\Magicraft_Ripped\ExportedProject\`）

| 资产类型 | 数量 | 说明 |
|----------|------|------|
| AnimationClip | 2,772 | 动画片段（角色+特效） |
| AnimatorController | 644 | 动画状态机 |
| Sprite | 10,122 | 精灵图（UI+角色+特效） |
| Texture2D | 22,786 | 贴图（含各种图集） |
| Material | 11,460 | 材质（含 GPU Instancing 变体） |
| Shader | 862 | 着色器 |
| GameObject | 1,066 | 预制体（独立 GameObject） |
| MonoBehaviour | 502 | ScriptableObject 等数据资产 |
| AudioClip | 192 | 音频片段 |
| ComputeShader | 70 | 计算着色器（DOTS 物理？） |
| VisualEffectAsset | 22 | VFX Graph 特效 |
| 场景 | 9 | `.unity` 场景文件 |

**Resources/prefabs：8,222 个 Prefab**，分以下类别：

| 目录 | 数量 | 内容 |
|------|------|------|
| spell/ | 4,350 | 法术 Prefab（最多！） |
| units/ | 996 | 怪物/Boss/玩家单位 |
| ef/ | 1,078 | 特效 Prefab |
| scene/ | 668 | 场景道具 |
| ui/ | 452 | UI 组件 |
| specialobjs/ | 214 | 特殊互动对象 |
| mixed/ | 156 | 混合对象 |
| item/ | 134 | 物品道具 |
| storys/ | 104 | 剧情演出 |
| npcs/ | 42 | NPC |
| resource/ | 26 | 资源拾取物 |

### 6.2 场景列表（完整）

| 场景名 | 用途 |
|--------|------|
| `Battle.unity` | 核心战斗场景（主要游戏场景） |
| `Camp.unity` | 营地大厅（装备/商店/NPC） |
| `Guide.unity` | 新手引导 |
| `Guide2.unity` | 引导（第二部分） |
| `MainMenu.unity` | 主菜单 |
| `Init.unity` | 初始化加载场景 |
| `Entry.unity` | 入口场景 |
| `EasyFinishBackHome.unity` | 快速返回大厅 |
| `NPC7Appearance.unity` | NPC7 外观选择 |

### 6.3 完整 JSON 游戏配置数据

| 配置文件 | 大小 | 内容 |
|----------|------|------|
| `SpellConfig.json` | 397KB | **329 条**法术配置 |
| `WandConfig.json` | 123KB | **135 把**魔杖配置 |
| `RelicConfig.json` | 90KB | **96 件**遗物配置 |
| `UnitConfig.json` | 404KB | **462 个**单位配置 |
| `CurseConfig.json` | 18KB | **61 条**诅咒配置 |
| `PotionConfig.json` | 5.4KB | 药水配置 |
| `TextConfig_Spell.json` | 774KB | 法术文本（多语言） |
| `TextConfig_Relic.json` | 232KB | 遗物文本 |
| `TextConfig_Wand.json` | 113KB | 魔杖文本 |
| `RoomConfig-0/1/2/3.json` | 各12MB | **关卡房间配置**（4个章节，极大！） |
| `TextConfig_Story.json` | 486KB | 剧情文本 |
| `HandbookConfig.json` | 2.7KB | 图鉴配置 |
| `MultiDifficultyConfig.json` | 15KB | 多难度配置 |
| `ResearchConfig.json` | 4.8KB | 研究系统 |

### 6.4 法术系统数值规律（从 SpellConfig 提取）

法术按 `abilityType` 分段，每段代表一种法术行为，多个等级的法术共用同一 `abilityType`：

| abilityType 段 | 法术类型 | 配置数量 |
|----------------|----------|---------|
| 1000-1999 | 投射物（Missile） | 92 条 |
| 2000-2999 | 召唤（Summon） | 19 条 |
| 3000-3999 | 强化（Enhance，无 prefab，纯数值） | 129 条 |
| 4000-4999 | 被动（Passive，无 prefab） | 56 条 |
| 9000-9999 | 特殊/Boss专用法术 | 30 条 |

法术有**1/2/3级**升级系统（`level` 字段），同一 `abilityType` 有多级版本（`id` 末位为级别）。

**Enhance 型法术数据结构特点**：
- `prefab = ""`（无飞行实体，是修改器）
- `mpCost = 0`（不消耗MP）
- `haveEffecforMissileSpell/SummonSpell`：标记适用哪类法术
- 通过 `float1/2/3 + int1/2/3` 传递具体修改参数

### 6.5 魔杖系统数值规律（从 WandConfig 提取）

135 把魔杖，关键数值范围：

| 参数 | 含义 | 典型值 |
|------|------|--------|
| `shootInterval` | 发射间隔(秒) | 0.1-0.5s |
| `coolDown` | 弹夹耗尽冷却 | 0.5-2.0s |
| `maxMP` | 最大MP | 50-200 |
| `mpRecovery` | 每秒MP回复 | 10-50 |
| `shootCount` | 单次发射法术数 | 1-5 |
| `damageCorrection` | 全局伤害修正% | 80-130 |
| `normalSlots` | 普通槽法术预设 | 2-8个槽 |

魔杖有**后置槽触发类型**（`postSlotTriggerType`）和充能速率（`PostSlotTriggerChargeRatio`），是核心差异化机制。

### 6.6 遗物系统数值规律（从 RelicConfig 提取）

96 件遗物，关键特点：
- 有**皮肤系统**（`skinNameDave`、`skinNameHalloween` 等——不同角色用不同皮肤！）
- 有**符文积分**（`RedRunePoint/GreenRunePoint/BlueRunePoint`）
- 有**升级机制**（`int1.value` vs `int1.valueUpgrade`）
- `maxCount`：可叠加上限（多件同一遗物）
- `InEndlessMode`：是否出现在无尽模式

---

## 七、项目全局文件索引（完整）

以上分析基于 Il2CppDumper（代码签名）和 UnityPy（贴图/音频）。  
若需要完整的 Prefab + Scene + 场景层次结构，需用 **AssetRipper GUI**（已安装在 `D:\Tools\AssetRipper`）。

### 操作步骤
1. 运行 `D:\Tools\AssetRipper\AssetRipper.GUI.Free.exe`
2. 浏览器会自动打开（或手动访问提示的端口）
3. 点击 **Import File/Folder** → 选择 `F:\SteamLibrary\steamapps\common\Magicraft\Magicraft_Data`
4. 等待解析完成（预计 5-10 分钟，资产 1.4GB）
5. 点击 **Export All** → 选择输出目录 `D:\ReferenceAssets\Magicraft_Ripped`

### 注意事项
- IL2CPP 游戏的 MonoBehaviour 字段值无法还原（代码逻辑不可见）
- Prefab 的组件引用会有 Missing Script（因为 DummyDll 只有签名）
- 场景层次结构、Animator Controller、Tilemap 数据等仍然可以完整提取

---

## 八、文件索引

| 文件/目录 | 内容 |
|-----------|------|
| `D:\ReferenceAssets\Magicraft_dump\dump.cs` | 所有类的 IL2CPP 签名（62MB，含字段/方法/枚举） |
| `D:\ReferenceAssets\Magicraft_dump\il2cpp.h` | C 结构体头文件（97MB，供 Ghidra/IDA 使用） |
| `D:\ReferenceAssets\Magicraft_dump\script.json` | 内存地址映射（249MB，供调试器使用） |
| `D:\ReferenceAssets\Magicraft_dump\DummyDll\` | 113 个 DLL（可用 dnSpy/ILSpy 查看类型树） |
| `D:\ReferenceAssets\Magicraft_Art\Texture2D\` | 9,189 张贴图（427 MB，UnityPy 直接导出） |
| `D:\ReferenceAssets\Magicraft_Art\Sprites\` | 5,379 张图集切片（UnityPy 直接导出） |
| `D:\ReferenceAssets\Magicraft_Art\Audio\` | 833 个音效/音乐文件（UnityPy 直接导出） |
| `D:\ReferenceAssets\Magicraft_Ripped\ExportedProject\` | **AssetRipper 完整导出**（72,705 个文件） |
| `D:\ReferenceAssets\Magicraft_Ripped\ExportedProject\Assets\Resources\configs\` | 所有 JSON 游戏配置数据（法术/魔杖/遗物/单位/诅咒/房间） |
| `D:\ReferenceAssets\Magicraft_Ripped\ExportedProject\Assets\Resources\prefabs\` | 8,222 个游戏 Prefab |
| `D:\ReferenceAssets\Magicraft_Ripped\ExportedProject\Assets\_Scenes\` | 9 个场景文件（.unity） |
| `D:\ReferenceAssets\Magicraft_Ripped\ExportedProject\Assets\Scripts\Assembly-CSharp\` | 还原的脚本目录（IL2CPP 无逻辑，仅结构） |

---

*报告生成于 2026-03-08*
