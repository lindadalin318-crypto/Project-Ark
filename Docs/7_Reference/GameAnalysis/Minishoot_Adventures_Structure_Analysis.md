# Minishoot' Adventures — 完整项目结构分析

> **分析日期**：2026-03-08  
> **Unity 版本**：2021.3.14f1  
> **构建类型**：Mono（非 IL2CPP）  
> **渲染管线**：URP 2D  
> **第三方库**：DOTween、Sirenix Odin Inspector、I2 Localization、Easy Save 3 (ES3)、Steamworks.NET  
> **源文件路径**：`D:\Tools\Minishoot_Ripped\ExportedProject\`（AssetRipper 导出）

---

## 一、项目概览

Minishoot' Adventures 是一款 **Top-Down 2D 弹幕射击 × 银河恶魔城** 游戏。玩家驾驶小飞船在开放世界中探索，击败敌人获取 XP 升级属性，收集 Modules/Skills 解锁新能力，挑战 Boss 推进剧情。

### 核心数字

| 类别 | 数量 |
|------|------|
| C# 脚本 | 337 个 |
| Prefab | ~150 个 |
| Texture2D | 2,724 个 |
| Sprite | 4,424 个 |
| AudioClip | 850 个 |
| AnimatorController | 13 个 |
| AnimationClip | 42 个 |
| 场景 | 16 个 |
| ScriptableObject 资产 | ~200 个 |

---

## 二、场景结构

```
Assets/- Scenes/
├── -Main.unity          # 启动场景，包含 GameManager
├── -GlobalObjects.unity # 全局单例对象（Player、UIManager、AudioManager 等）
├── Cave.unity           # 初始地牢（教程区）
├── CaveArena.unity      # 竞技场
├── CaveExtra.unity      # 洞穴扩展区
├── Overworld.unity      # 主世界地图
├── Snow.unity           # 雪地区域
├── Dungeon1~5.unity     # 5 个主要地牢
├── Temple1~3.unity      # 3 个神殿
└── Tower.unity          # 最终塔楼
```

**加载策略**：单场景 + Additive 加载。`GameManager` 在启动时预加载 Cave + Overworld，后续通过 `LoadLocation()` 动态加载/激活其他场景。

---

## 三、代码架构

### 3.1 继承层次（核心实体）

```
MonoBehaviour
└── MiniBehaviour          # 基类，封装 MiniUpdate（受 GameManager.InGame 控制）
    └── ShipBehaviour      # 所有飞船实体基类（持有 Movable/Rotable/Destroyable 等引用）
        ├── Player         # 玩家（单例 Player.Instance）
        └── Enemy          # 敌人基类
            └── Boss       # Boss 特化
```

### 3.2 Player 系统

`Player` 是一个**组合式单例**，通过 `GetComponent` 在 Awake 中收集所有子系统：

```csharp
// Player.cs 核心组件
Player
├── PlayerView          // 视觉表现（Wobble、Die、Restore 动画）
├── PlayerEmote         // 表情系统
├── PlayerEnergy        // 能量条（Supershot/Boost/Dash 消耗）
├── PlayerPowers        // 三种能力：Bomb / Slow / Ally
├── PlayerWeapon        // 武器系统（主炮 + Supershot + Primordial + Burst + Rage）
├── PlayerControl       // 输入处理 → 移动/瞄准
├── PlayerLevelUpView   // 升级 UI
├── PlayerEnergyUpView  // 能量升级 UI
└── PlayerUpgradeView   // 属性升级 UI
```

**静态访问模式**：`Player.Weapon`、`Player.Energy`、`Player.Position` 等均为静态属性，通过 `Instance` 转发。

### 3.3 移动系统（Movable）

`Movable` 实现 `IMovable` 接口，使用 **Rigidbody2D + Lerp 速度插值**：

```csharp
// FixedUpdate 核心逻辑
Vector2 targetVelocity = Direction * moveData.speedMax * SpeedCoef * enemySpeedMod * slowMod;
Rigid.velocity = Vector2.Lerp(Rigid.velocity, targetVelocity, moveData.acceleration * Time.fixedDeltaTime);
```

三套移动参数（`MoveDataStru`）：
- `BaseMove`：地面正常移动
- `BoostMove`：加速冲刺
- `WaterMove`：水面移动（降速）

**Dash 实现**：`AddForce(Impulse)` + DOTween 延迟重置 `Dashing = false`。

### 3.4 武器系统（PlayerWeapon）

5 种子弹模式，通过 `BulletEmitter` 发射：

| 模式 | 触发条件 | 特点 |
|------|----------|------|
| Normal | 默认 | 支持 Homing（自动瞄准） |
| Supershot | 按住 Supershot 键 + 能量足够 | 多管齐发，射程加成 |
| Primordial | PrimordialMode 激活 | 高伤害，低射速 |
| Burst | 特殊技能 | 360° 爆发，高 DPS |
| Rage | RageMode 激活 | 强制 Homing，伤害加成 |

**Homing 逻辑**：`Physics2D.OverlapCircleAll` 在射程+半径内找最近且在瞄准角度内的敌人。

### 3.5 子弹系统（Bullet）

`Bullet` 实现 `IPoolable`，使用**手动 CircleCast 碰撞检测**（非 Unity 物理触发器）：

```csharp
// Update 中每帧 CircleCast
Physics2D.CircleCastNonAlloc(transform.position, collisionRadius, moveDir, castResults, moveDir.magnitude, collisionLayerMask)
```

子弹特性：
- `BulletData`：速度/射程/扩散/正弦波动/角速度/缩放曲线
- `Homing`：子组件，负责追踪目标
- `SpeedCurve`：速度随时间变化曲线
- `SinWave`：正弦波动轨迹（`UseSine` + `Freq` + `Magn`）

### 3.6 敌人 AI 系统

#### 架构：Pattern-Action 驱动

```
Enemy (ShipBehaviour)
├── AIPatternManager    # 时间轴调度器，按 PatternStep 序列执行 Actions
├── AIWeapon            # 武器行为（实现 IPatternable）
├── AIFollow            # 追踪行为（实现 IPatternable）
├── AICharge            # 冲锋行为
├── AIBusher            # 伏击行为
├── AISneak             # 隐身行为
├── AIGrid              # 网格移动行为
├── AIScriptable        # 脚本化自定义行为
├── AIRotable           # 旋转/瞄准
├── AINavigation        # 寻路（Dijkstra 图）
└── AIRestorable        # 复活/重置
```

#### PatternStep 执行流程

```
AIPatternManager.StartPattern()
  → 执行 initialStep.Actions（立即）
  → 等待 offset + initialStep.StepDuration
  → 循环执行 pattern[stepIndex].Actions
      → AIPatternAction.ExecuteAction(properStartTime)
          → 反射设置 component.Data = xxxData
          → 反射调用 component.StartAction(properStartTime)
```

**关键设计**：`AIPatternAction` 通过**反射**动态分发 Data 和调用 `StartAction`，实现了无需 switch-case 的多态行为调度。

#### 敌人分级

- **Size**（1-3）：小/中/大，影响 HP、质量、掉落
- **Tier**（1-3）：普通/精英/暗黑，影响子弹类型和颜色
- **Level** = `3 * (Tier-1) + Size`（1-9 级）
- **HP 公式**：`(hpBase * HpBaseFactor + hpBase * (level-1) * HpFactor * pow(level, level * HpPowFactor)) * sizeFactor`

### 3.7 数据层（ScriptableObject）

#### PlayerData（核心数值 SO）

通过 `Resources.Load<PlayerData>("PlayerData")` 单例加载，包含：
- 所有属性的 `PlayerStatsDataStru`（含 Min/Max/LevelMax 和 AnimationCurve）
- 能量系统参数（消耗/恢复速率）
- XP 增益曲线
- 子弹/炸弹 Prefab 引用
- 各种倍率修正（Supershot/Primordial/Burst/Rage）

#### PlayerStats（静态计算层）

```csharp
// 属性计算：level/levelMax 映射到 AnimationCurve
public static float Compute(Stats id, int level = -1) {
    PlayerStatsDataStru data = PlayerData.GetStatsData(id);
    return data.Stats.GetValueByRatio((float)level / (float)data.LevelMax);
}
```

#### EnemyData（敌人数值 SO）

- 按 Size 分类的子弹 Prefab 字典
- HP 计算公式参数
- 移动速度曲线（`FollowSpeedByDistance`）
- 旋转速度曲线（`RotateAroundSpeedByDistance`）

### 3.8 存档系统（SaveManager + ES3）

使用 **Easy Save 3** 文件缓存系统：

```
存档键（SaveManager.Id）：
- CurrLocation / CurrCheckpoint  # 当前位置
- StatsLevel / StatsPoints       # 属性等级和点数
- Skills / Modules               # 技能和模块解锁状态
- WorldState                     # 世界状态标志位
- GameStats                      # 游戏统计（死亡次数/游玩时间等）
- Map / CollectableFound         # 地图探索和收集品
- DungeonKeys / DungeonBossKeys  # 钥匙
- XpTotal                        # 总 XP
```

**自动存档**：`GameManager.LateUpdate` 中调用 `SaveManager.BackupRegularly()` 和 `StoreSaveFile(force: false)`。

### 3.9 游戏状态管理

```csharp
public enum GameState { Loading, Intro, Game }
```

**事件总线**（GameManager 静态事件）：
- `GlobalObjectLoaded`：全局对象加载完成
- `GameLocationLoaded`：场景激活完成
- `GamePostLoaded`：场景后处理完成
- `GameStateLoaded`：游戏状态就绪（玩家可操控）
- `GameReset`：返回主菜单

### 3.10 枚举定义（游戏设计核心）

```csharp
// 玩家属性
enum Stats { PowerAllyLevel, BoostSpeed, BulletNumber, BulletSpeed, 
             PowerBombLevel, CriticChance, Energy, FireRange, FireRate, 
             Hp, MoveSpeed, Supershot, BulletDamage, PowerSlowLevel }

// 技能（解锁型）
enum Skill { Supershot, Dash, Hover, Boost }

// 模块（装备型）
enum Modules { IdolBomb, IdolSlow, IdolAlly, BoostCost, XpGain, HpDrop,
               PrimordialCrystal, HearthCrystal, SpiritDash, BlueBullet,
               Overcharge, CollectableScan, Rage, Retaliation, FreePower,
               Compass, Teleport }

// 能力
enum Power { Bomb, Slow, Ally }

// 子弹尺寸
enum Size { Small, Medium, Large }
```

---

## 四、美术资产结构

### 4.1 Texture & Sprite

```
Assets/Texture2D/    # 2,724 张原始贴图
Assets/Sprite/       # 4,424 个 Sprite（切片后）
```

**风格**：像素艺术（Pixel Art），Top-Down 视角，使用 URP 2D Renderer + 自定义 Shader。

### 4.2 Tilemap 系统

Tilemap 使用自定义 `RuleTile` 变体（`TileFilter`），通过 `TilemapParser` 解析。

**Tile 类型**（来自 MonoBehaviour 资产）：
| 类别 | 资产名 |
|------|--------|
| 地面 | Ground, GroundDungeon, GroundRock, GroundRockFull, GroundNoRandom |
| 墙壁 | Wall, WallDungeon, WallForest, WallGreen, WallRed, WallDungeonPurple |
| 墙柱 | WallColumn1~6, WallColumnDungeon1~6, WallColumnRed1~6 |
| 水面 | Water, WaterBlue, WaterDeep, WaterDungeon, WaterGold, WaterSoiled |
| 洞穴 | Hole, HoleDungeon |
| 草地 | Grass, Grass2, GrassBlue, GrassDark, GrassDirt, GrassDirtRed |
| 雪地 | Snow |
| 阴影 | WallShadow, WallForestShadow, WallColumnShadow1~6 |

**Biome 系统**：`BiomeManager` + `BiomeTrigger` 控制区域主题切换（Cave/Forest/Desert/Snow/Dungeon）。

### 4.3 动画系统

**AnimatorController（13 个）**：

| 控制器 | 用途 |
|--------|------|
| PlayerAnimator | 玩家飞船（Idle/Dash/DashHalf/LeanLeft1-3/LeanRight1-3/Menu） |
| Blober | 水母型敌人（Idle/Move/Fire/Shake） |
| Miniboss Blober | 水母 Miniboss |
| Frogger / FroggerBig | 青蛙型敌人（Idle/Walk/Jump） |
| JunkerS2 / JunkerS3 | 废铁型敌人（Hidden/Appear） |
| Scara | 圣甲虫（Fly） |
| NpcHouse | NPC 房屋（Sad/Happy/GetFree） |
| NpcTiny | 小 NPC（Fly） |
| NpcTree | 树 NPC（Sleep/WakeUp/Awake） |
| Turtle | 海龟（Hidden/Appear/SwimForward） |
| SkillCube | 技能方块 |

**玩家动画状态**：
- `Idle`：悬停待机
- `Dash` / `DashHalf`：冲刺
- `LeanLeft1/2/3` / `LeanRight1/2/3`：转向倾斜（3 级强度）
- `Menu` / `Menu1`：菜单状态

### 4.4 音频系统

```
Assets/AudioClip/    # 850 个音频文件
```

**音频管理**：
- `Sounds`：静态工具类，`Play(sfxId)` / `Play3D(sfxId, transform)` / `PlayLoop(sfxId)`
- `Music`：背景音乐管理
- `Sfx`：音效 ID 枚举（`SRResources` 自动生成）
- `Clips`：音乐 ID 枚举
- `SoundLoopPlayer`：循环音效组件（挂在 Player/Enemy 上）
- `AmbientSfx` / `AmbientTrigger`：环境音触发

### 4.5 视觉特效

```
Assets/Scripts/Assembly-CSharp/
├── Fx.cs              # 特效工厂（AddShipFx/BulletDestroyed/ExplosionStain 等）
├── FxData.cs          # 特效数据（颜色/曲线）
├── EmissionPools.cs   # 子弹/粒子对象池管理
├── ParticleMaster.cs  # 粒子系统主控
├── SpriteReflection.cs # 水面倒影
├── SpriteShadow.cs    # 精灵阴影
├── ShineEffect.cs     # 闪光效果
└── IsFlashingEffect.cs # 受击闪烁
```

**后处理**（URP Volume）：
- Bloom、ChromaticAberration、LensDistortion
- ColorAdjustments、ChannelMixer
- ShadowsMidtonesHighlights、Vignette、WhiteBalance

---

## 五、关卡与世界设计

### 5.1 场景层级

```
-Main (GameManager, UIManager, AudioManager)
  ↓ Additive Load
-GlobalObjects (Player, CameraManager, PostProcessManager)
  ↓ Additive Load  
Cave / Overworld / Dungeon1~5 / Temple1~3 / Snow / Tower
```

### 5.2 关卡组件

| 组件 | 职责 |
|------|------|
| `Location` | 场景标识，持有 Checkpoints 数组 |
| `LocationManager` | 管理所有已加载 Location，处理场景切换 |
| `Checkpoint` | 复活点，`Checkpoint.Current` 静态引用 |
| `ArenaManager` | 竞技场管理（波次/进度） |
| `EncounterWave` | 遭遇战波次定义 |
| `EncounterOpen/Close` | 遭遇战开始/结束触发器 |
| `Lock` / `Unlocker` | 门锁系统（击败敌人解锁） |
| `DoorLocked` / `BossDoor` | 门类型 |
| `Transition` | 场景过渡触发器 |
| `CameraTrigger` | 摄像机区域触发 |
| `BiomeTrigger` | 生物群系切换触发 |

### 5.3 世界状态系统

`WorldState` 静态类管理全局布尔标志：

```csharp
// 关键世界状态标志
"OverworldReached"      // 到达主世界
"Dungeon2Reached"       // 到达地牢2
"FinalBossBeaten"       // 击败最终 Boss
"CrystalBoss"           // 水晶 Boss 状态
"Introduced"            // 完成介绍
"FirstBulletShot"       // 第一次射击（成就触发）
```

---

## 六、UI 系统

### 6.1 UI 管理

`UIManager` 单例管理所有 UI 面板：
- `HUD`：血条/能量条/XP 条
- `Map`：世界地图
- `PauseMenu`：暂停菜单
- `Options`：设置
- `TitleMenu`：主菜单
- `PopUpScreen`：弹出对话框
- `TextMessage`：剧情文字
- `CutsceneIntro`：过场动画

### 6.2 输入系统

使用 **Unity New Input System**，Input Actions 资产：
- `Gameplay_Move` / `Gameplay_MoveSlow`
- `Gameplay_Shoot` / `Gameplay_ShootMouse`
- `Gameplay_Boost` / `Gameplay_Dash`
- `Gameplay_Supershot`
- `Gameplay_PowerBomb` / `Gameplay_PowerSlow` / `Gameplay_PowerAlly`
- `Gameplay_Interact` / `Gameplay_Inventory` / `Gameplay_Map`
- `Menu_Navigate` / `Menu_Submit` / `Menu_Cancel` 等

`PlayerInputs` 静态类封装输入读取，`DeviceManager` 处理手柄/键鼠切换。

---

## 七、对象池系统

`EmissionPools` 管理所有子弹和粒子的对象池：
- `RecycleAll()`：场景切换时回收所有活跃对象
- `ExploseAIBullet()`：清除所有敌人子弹（Boss 死亡时）

`Pool<T>` 泛型对象池，`IPoolable` 接口定义 `ResetItem()` 和 `Recycled` 事件。

---

## 八、关键设计模式总结

| 模式 | 应用场景 |
|------|----------|
| **Singleton** | Player、GameManager、UIManager、CameraManager、EnemyData、PlayerData |
| **Component Composition** | ShipBehaviour 聚合所有子系统组件 |
| **Strategy（Data-Driven）** | AIPatternAction 通过反射动态分发行为 |
| **Observer（C# Events）** | Player.Restored、Enemy.Destroyed、GameManager.GameStateLoaded 等 |
| **Object Pool** | EmissionPools 管理子弹/粒子 |
| **ScriptableObject Config** | PlayerData、EnemyData 集中管理数值 |
| **Static Utility** | PlayerStats、WorldState、SaveManager、Sounds 等无状态工具类 |
| **Additive Scene Loading** | 多场景叠加，GlobalObjects 常驻 |

---

## 九、与 Project Ark 的对比与借鉴点

### 可直接借鉴

1. **PatternStep 行为调度**：`AIPatternManager` 的时间轴 + `IPatternable` 接口模式，比 HFSM 更轻量，适合中小型敌人
2. **BulletData 数据驱动**：子弹所有参数（速度/射程/正弦/角速度/缩放曲线）集中在一个 struct，通过 `BulletEmitter.Fire()` 传入
3. **CircleCast 子弹碰撞**：避免 Unity 物理触发器的帧延迟，更精确
4. **Homing 子组件**：Homing 作为独立子组件挂在 Bullet 上，解耦清晰
5. **PlayerStats 静态计算层**：属性值通过 `AnimationCurve.Evaluate(level/levelMax)` 计算，曲线可视化调参

### 差异点（Project Ark 的优势）

1. Minishoot 使用 **DOTween**（旧），Project Ark 使用 **PrimeTween**（更现代）
2. Minishoot 使用 **Coroutine**，Project Ark 使用 **UniTask**
3. Minishoot 无 Assembly Definition 隔离，Project Ark 有模块化边界
4. Minishoot 的 `Player.Instance` 全局静态访问，Project Ark 使用 **ServiceLocator** 更解耦
5. Minishoot 的 AI 是 Pattern-Action 平铺式，Project Ark 的 HFSM 三层架构更适合复杂 Boss

---

## 十、文件路径索引

```
D:\Tools\Minishoot_Ripped\ExportedProject\
├── Assets/
│   ├── - Scenes/           # 16 个场景
│   ├── Scripts/Assembly-CSharp/  # 337 个 C# 源文件
│   ├── GameObject/         # ~150 个 Prefab
│   ├── MonoBehaviour/      # ScriptableObject 资产（Tile/Input/URP 配置等）
│   ├── Texture2D/          # 2,724 张贴图
│   ├── Sprite/             # 4,424 个 Sprite
│   ├── AnimatorController/ # 13 个动画控制器
│   ├── AnimationClip/      # 42 个动画片段
│   ├── AudioClip/          # 850 个音频
│   ├── Material/           # 材质
│   ├── Shader/             # 着色器
│   └── Resources/          # 运行时动态加载资源
└── ProjectSettings/        # Unity 项目设置
```
