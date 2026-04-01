# Hollow Knight Silksong — 完整解构报告

> 生成时间：2026-03-08  
> 工具：Mono.Cecil（代码分析）+ AssetRipper（项目导出）+ UnityPy（美术导出）  
> 用途：架构参考，仅限个人研究，禁止用于商业项目资产复用

---

## 1. 技术栈总览

| 项目 | 值 |
|------|-----|
| **Unity 版本** | 6000.0.50f1（Unity 6 LTS） |
| **脚本后端** | **Mono**（非 IL2CPP）→ Assembly-CSharp.dll 可直接反编译 |
| **资产系统** | Unity **Addressables** (`StreamingAssets/aa/`) |
| **输入系统** | InControl（第三方，非 New Input System） |
| **FSM 框架** | **PlayMaker**（HutongGames，2,240 个 Action 类） |
| **序列化** | Newtonsoft.Json（`Newtonsoft.Json.dll` 675KB） |
| **UI 框架** | Unity UGUI + TextMeshPro |
| **物理** | Unity Physics2D |
| **平台** | Steamworks.NET + Xbox GDK（XGamingRuntime） |
| **构建平台** | Windows Standalone x64 |

### 重大架构发现

1. **Mono 后端** = 代码完全可读（dnSpy/ILSpy 直接反编译）
2. **PlayMaker 是游戏逻辑主干** = 2,240 个 Action 类，几乎所有 NPC 对话、事件触发、Boss 行为都用 PlayMaker FSM 实现
3. **Addressables 资产系统** = 所有场景/资产通过 Addressables 异步加载，~478 个资产包
4. **Assembly-CSharp.dll 6.8MB** = 5,079 个类型，其中无命名空间核心类 2,015 个

---

## 2. 程序集与命名空间架构

```
Assembly-CSharp (6.8 MB, 5,079 类型)
├── HutongGames.PlayMaker.Actions    — 2,240 个 Action（游戏逻辑主体）
├── (无命名空间)                      — 2,015 个（核心游戏类）
├── InControl.NativeDeviceProfiles   — 224 个（手柄映射）
├── InControl.UnityDeviceProfiles    — 178 个（手柄配置）
├── InControl                        — 115 个（输入框架）
├── TMProOld                         — 85 个（旧版 TMP）
├── GlobalEnums                      — 51 个（全局枚举）
├── TeamCherry.PS5                   — 13 个（PS5 平台代码）
├── TeamCherry.GameCore              — 6 个（核心工具）
└── ...
```

---

## 3. 核心系统地图

### 3.1 数据单点存储：PlayerData（1,359 字段）

PlayerData 是整个游戏状态的**单一来源（Single Source of Truth）**，以单例模式运行，所有进度、探索、技能、道具状态均扁平化存储在此。

**字段类型分布：**
- Boolean: 1,163 个（占 85%）— 主要用于状态标记
- Int32: 91 个 — 货币/计数器
- String: 27 个 — 场景名/存档标记
- Single: 11 个 — 浮点数值（游戏时间等）
- 复杂类型: ~67 个（集合类、枚举、结构体）

**PlayerData 关键方法：**
```csharp
// 通用 getter/setter（反射式存取）
GetBool(string fieldName), SetBool(string fieldName, bool value)
GetInt(string fieldName),  SetInt(string fieldName, int value)
GetFloat / SetFloat / GetString / SetString / GetVector3 / SetVector3

// 游戏逻辑
SetBenchRespawn(string sceneName, string markerName, int respawnType)
SetHazardRespawn(Vector3 pos, FacingDirection facing)
SetupNewPlayerData()     // 新存档初始化
SetupExistingPlayerData() // 读档后初始化
OnBeforeSave()           // 存档前处理
CountGameCompletion()    // 计算完成度百分比
```

**PlayMaker 与 PlayerData 的桥接（24 个专用 Action）：**
```
GetPlayerDataBool / SetPlayerDataBool / PlayerDataBoolTest
GetPlayerDataInt  / SetPlayerDataInt  / IncrementPlayerDataInt / DecrementPlayerDataInt / PlayerDataIntAdd
GetPlayerDataFloat / SetPlayerDataFloat
GetPlayerDataString / SetPlayerDataString
PlayerDataBoolMultiTest / PlayerDataBoolAllTrue / PlayerDataBoolTrueAndFalse
SetPlayerDataVariable / GetPlayerDataVariable / PlayerDataVariableTest
GetPlayerDataVector3 / SetPlayerDataVector3
SetCollectablePickupPlayerDataBool / CheckPlayerDataTimeLimit / SetPlayerDataTimeLimit
```

---

## 4. Progression 系统

### 4.1 技能/能力解锁（`has*` 布尔字段）

所有移动/战斗技能通过 PlayerData 的 `has*` 布尔字段控制：

| 字段 | 能力 |
|------|------|
| `hasDash` | 冲刺（Harpoon Dash 升级前基础冲刺） |
| `hasHarpoonDash` | 鱼叉冲刺（升级形态） |
| `hasWalljump` | 攀墙跳 |
| `hasDoubleJump` | 二段跳 |
| `hasSuperJump` | 超级跳 |
| `hasBrolly` | 雨伞（滑翔/弹射） |
| `hasNeedleThrow` | 针矢投掷 |
| `hasNeedolin` | 内针（近战升级） |
| `hasNeedolinMemoryPowerup` | 记忆强化 |
| `hasChargeSlash` | 蓄力斩 |
| `hasThreadSphere` | 丝线球 |
| `hasParry` | 格挡 |
| `hasSilkSpecial` | 丝绸特技（`silkSpecialLevel` 等级） |
| `hasSilkCharge` | 丝绸冲能 |
| `hasSilkBomb` | 丝绸炸弹 |
| `hasSilkBossNeedle` | Boss 级针矢 |
| `hasQuill` | 羽毛笔（地图相关？） |
| `hasGodfinder` | 神明探测 |
| `hasMarker` / `hasMarker_a/b/c/d/e` | 传送标记 |
| `hasJournal` | 猎手日志 |

**"首次见到"提示（HasSeen* 字段）：**
每个新技能还有对应的 `HasSeen*` 字段用于控制教学提示是否已经显示。

### 4.2 地图解锁系统（28 个区域）

**完整地图区域（`MapZone` 枚举，42 个值）：**
```
NONE, TEST_AREA,
PATH_OF_BONE, GREYMOOR, SHELLWOOD_THICKET, RED_CORAL_GORGE,
CITY_OF_SONG, THE_SLAB, GLOOM, DUSTPENS, BELLTOWN,
HUNTERS_NEST, BONETOWN, MOSS_CAVE, PHARLOOM_BAY, DOCKS,
WILDS, WEAVER_SHRINE, BONECHURCH, MOSSTOWN, LIBRARY,
CLOVER, UNDERSTORE, COG_CORE, PEAK, DUST_MAZE,
WARD, HANG, ARBORIUM, CRADLE, PILGRIMS_REST,
HALFWAY_HOUSE, JUDGE_STEPS, MEMORY, CRAWLSPACE,
WISP, SWAMP, ABYSS, AQUEDUCT, SURFACE, FRONT_GATE, CORAL_CAVERNS
```

**地图碎片解锁字段（`Has*Map` 布尔，28 个）：**
```
HasMossGrottoMap, HasWildsMap, HasBoneforestMap, HasDocksMap,
HasGreymoorMap, HasBellhartMap, HasShellwoodMap, HasCrawlMap,
HasHuntersNestMap, HasJudgeStepsMap, HasDustpensMap, HasSlabMap,
HasPeakMap, HasCitadelUnderstoreMap, HasCoralMap, HasSwampMap,
HasCloverMap, HasLibraryMap, HasAbyssMap, HasAqueductMap,
HasArboriumMap, HasWardMap, HasHangMap, HasCradleMap,
HasSongGateMap, HasWeavehomeMap, HasHallsMap, HasCogMap
```

地图碎片通过 NPC "制图师(Mapper)"购买，Mapper 在各区域出现，PlayerData 记录 Mapper 的对话状态（`mapperRosaryConvo`, `mapperMetInAnt04` 等 20+ 字段）。

### 4.3 快速旅行（Tube 系统，13 个节点）

```csharp
enum FastTravelLocations {
    None, Bonetown, Docks, BoneforestEast, Greymoor, Belltown,
    CoralTower, City, Peak, Shellwood, Bone, Shadow, Aqueduct
}
```

找到对应的 `hasPinStag`, `hasPinTube` 等 Pin 字段，以及 `TubeTravelLocations` 枚举。

### 4.4 Boss 进度追踪

Boss 状态字段模式：`encountered{BossName}` + `defeated{BossName}` + `killed{BossName}`
- 三阶段区分：遭遇 → 击败（剧情过场）→ 死亡（血量归零）
- **BossSequence 系统**（挑战回廊）：`BossSequenceController`、`BossDoorCompletionStates`、`BossDoorTargetLock`

部分已确认的 Boss：
- dicePilgrim（骰子朝圣者）
- garmond（Garmond，多阶段：Library/BlackThread）
- mossMother / mossEvolver（青苔之母/进化体）
- bonetownBoss（骨镇 Boss）
- skullKing（骷髅王，含 BlackThread 变体击败记录）
- bellBeast（铃铛兽）
- antQueen（蚁后，含记忆后再击败字段）
- vampireGnat Boss（吸血蚊 Boss）
- lace1（蕾丝1，暗示多次战斗）
- songGolem（音之傀儡）
- crowCourt（乌鸦庭院）
- pilby（Pilby）

### 4.5 收集物系统

**Collectable 类体系：**
```
CollectableItem (基类)
├── CollectableItemBasic
├── CollectableItemMemento (纪念物)
├── CollectableRelic (遗物)
└── CollectableItemStack

CollectableItemManager    — 运行时管理器（masterList）
CollectableRelicManager   — 遗物专用管理器
EnemyJournalManager       — 猎手日志管理器
```

**EnemyJournalManager 关键方法：**
```csharp
RecordKill(EnemyJournalKillData data)
GetCompletedEnemiesCount()  // 已完成条目数
GetAllEnemies()             // 全部敌人列表
IsAllRequiredComplete()     // 是否全完成
CheckJournalAchievements()  // 成就检查
```

**货币体系：**
- `geo` (Int32) — 普通货币（Geo，与 HK1 相同）
- `silk` (Int32) + `silkMax` + `silkParts` — 丝绸（技能释放资源）
- `silkSpoolParts` / `silkRegenMax` — 丝轴（丝绸再生上限道具）
- `ShellShards` (Int32) — 贝壳碎片（HP 强化材料）
- Relics (`CollectableRelicsData`) — 遗物收集

### 4.6 成就系统

```csharp
class AchievementHandler    // 成就管理器
class AchievementsList      // 成就列表（ScriptableObject）
class AchievementRecord     // 单条成就记录
class AchievementRecordFloat / AchievementRecordInt  // 进度类成就
```

GameManager 中的成就检查方法：
- `CheckAllAchievements()` 
- `CheckCompletionAchievements()`
- `CheckBellwayAchievements()`
- `CheckHeartAchievements()`
- `CheckSilkSpoolAchievements()`
- `CheckSubQuestAchievements()`
- `CheckTubeAchievements()`
- `CheckMapAchievements()`

成就分类（`AchievementType` 枚举）+ 进度类型（`AchievementValueType`）

### 4.7 游戏完成度计算

```csharp
// GameManager.CountGameCompletion()
// PlayerData.CountGameCompletion()
```
包括：地图探索率、Boss 击败、收集物、日志完成等多维度加权。

---

## 5. Exploration 系统

### 5.1 场景过渡机制（银河城核心）

**TransitionPoint（过渡点，银河城门）：**
```csharp
class TransitionPoint : InteractableBase {
    BeforeTransitionEvent OnBeforeTransition;
    GameManager gm;
    bool isInactive, isADoor, dontWalkOutOfDoor;
    bool alwaysEnterRight, alwaysEnterLeft;
    float entryDelay;
    // ... 40 个字段总计
    
    void Init();
    void Awake();
    // 通过 GameManager.ChangeToScene() 触发加载
}

class SceneTransitionZone : SceneTransitionZoneBase {
    string targetScene;   // 目标场景名
    string targetGate;    // 目标入口名
}
```

**加载流程：**
```
SceneTransitionZone 触发
→ GameManager.BeginSceneTransition()
→ GameManager.ChangeToScene(sceneName, gateName)
→ SceneLoad (Addressables AsyncOperationHandle)
→ GameManager.FindTransitionPoint() → FindEntryPoint()
→ GameManager.PositionHeroAtSceneEntrance()
→ GameManager.FinishedEnteringScene()
```

### 5.2 场景持久化（`PersistentBool` 系统）

银河城中每个可破坏墙/门/收集物的状态通过 `SceneData` 持久化：

```csharp
class SceneData {
    PersistentBoolCollection persistentBools;  // 布尔开关（可破坏物/门/机关）
    PersistentIntCollection  persistentInts;   // 整数状态
    PersistentIntCollection  geoRocks;         // Geo 矿石（死亡/存档间恢复）
    
    void Reset();          // 场景重置（死亡恢复）
    void SaveMyState();    // 持久化到 PlayerData/SaveData
}

class PersistentBoolItem : MonoBehaviour {
    PersistentBoolData itemData;   // { id: string, sceneName: string, value: bool }
    bool disableIfActivated;
    GameObject disablePrefabIfActivated;
    
    void LookForMyFSM();    // 从关联的 PlayMaker FSM 读取状态
    void GetValueFromFSM(); void SetValueOnFSM();
}
```

**设计要点：** 每个场景物件持有一个 `PersistentBoolItem`，其 ID 唯一标识该物件。场景卸载时 `SaveMyState()` 将所有布尔状态写入 `SceneData`，再由 `GameManager.SaveLevelState()` 序列化到磁盘。

### 5.3 地图系统（GameMap）

```csharp
class GameMap : MonoBehaviour {
    GameManager gm;
    InputHandler inputHandler;
    GameObject compassIcon;
    MapZone currentSceneMapZone;
    MapZone currentRegionMapZone;
    string overriddenSceneName;
    MapZone overriddenSceneRegion;
    MapZone corpseSceneMapZone;        // 死亡地点 MapZone
    ShadeMarkerArrow shadeMarker;      // 遗体指示箭头
    bool displayingCompass;
    // ... 60 个字段
    
    event UpdateQuickMapDisplayEvent UpdateQuickMapDisplay;
    event ViewPosUpdatedEvent ViewPosUpdated;
}

class MapPin : MonoBehaviour {
    ActiveConditions activeCondition; // 显示条件
    MapPin hideIfOtherActive;
    string SAVE_KEY;                   // PlayerData 布尔键
    GameMapScene parentScene;
    List<MapPin> _activePins;
    
    void CheckDidActivate();  // 根据 PlayerData 决定是否显示
}
```

**快速地图系统：** Horizon Bonk 系的 `MapMarkerArrow`、`MapMarkerButton`、`MapMarkerMenu`、`MapNextAreaDisplay` 类处理小地图 HUD。

**地图 Pin 类型（`hasPinXxx` 字段）：**
- `hasPinBench` — 休息点（小提琴音符）
- `hasPinCocoon` — 茧（复活点）
- `hasPinShop` — 商店
- `hasPinSpa` — 温泉
- `hasPinStag` — 驿马站
- `hasPinTube` — 管道（快速旅行）
- `hasPinFleaBlastedlands / Citadel / Marrowlands / Midlands / Mucklands / Peaklands` — 跳蚤（特殊交通）
- `hasMarker / hasMarker_a/b/c/d/e` — 玩家自定义标记（5种颜色）

### 5.4 Quest 系统架构

```csharp
class BasicQuestBase : QuestGroupBase {
    LocalisedString displayName;
    LocalisedString location;
    bool init;
    
    string GetDescription();
    void DoInit();        // 从 PlayerData 初始化状态
    void OnSelected();    // 选中时触发
}

// 任务状态通过 PlayerData 的 Bool 字段追踪
// 任务完成数据：QuestCompletionData（扩展 SerializableNamedList）
// 任务传言数据：QuestRumourData（追踪任务发现来源）
```

**已识别的 NPC Quest（PlayerData 字段）：**
| NPC | Quest 字段 |
|-----|-----------|
| 制图师(Mapper) | `mapperRosaryConvo`, `mapperTubeConvo`, `mapperSellingTubePins` 等 |
| 蓝色科学家 | `BlueScientistQuestOffered/Quest2/Quest3` |
| 骨底商人 | `BonebottomQuestOffered` |
| 灰熊修理工 | `fixerQuestBoardConvo`, `fixerAcceptedQuestConvo` |
| Huntress | `HuntressQuestOffered`, `HuntressRuntQuestOffered` |
| 蘑菇收集 | `MushroomQuestFound1~7` |
| 跳蚤 | `FleaQuestOffered` |
| Pinsmith | `PinsmithQuestOffered` |
| 朝圣者 | `pilgrimQuestSpoolCollected` |
| 医生 | `BelltownDoctorQuestOffered` |
| 骑手团 | `CaretakerOfferedSnareQuest` |

**Belltown 特有子任务组：** `BelltownBagpipersOfferedQuest`, `BelltownCouriersGenericQuests`, `BelltownCouriersLastCompletedQuest`

### 5.5 探索复活/惩罚系统

**死亡流程（非永久死亡）：**
```
HeroController.死亡触发
→ GameManager.PlayerDead() 
→ 保存遗体位置 (HeroCorpseScene, HeroCorpseMarkerGuid, HeroDeathScenePos)
→ 扣除部分货币（存入 HeroCorpseMoneyPool）
→ 恢复到最近 BellBench 存档点 (respawnScene / respawnMarkerName)
→ 加载 respawnScene
```

**非致命重生系统：**
```csharp
// nonLethalRespawnScene/Marker/Type — 区域边界推回点
// hazardRespawnLocation/Facing — 环境危险重生点（不经过遗体流程）
```

**永久死亡模式：** `permadeathMode: PermadeathModes`（支持 Iron Soul 模式）

### 5.6 记忆/叙事关卡（Memory 场景）

特殊叙事场景类型，通过 `IsMemoryScene()` / `ForceCurrentSceneIsMemory()` 标记。

```csharp
// completedMemory_* 字段追踪各记忆关卡完成状态：
completedMemory_reaper, completedMemory_wanderer,
completedMemory_beast, completedMemory_witch,
completedMemory_toolmaster, completedMemory_shaman
```

---

## 6. 战斗系统

### 6.1 Hornet（Silksong 主角）核心数值

**HeroController 运动参数（全部 hardcode 在代码中）：**
```csharp
float RUN_SPEED, WALK_SPEED
float JUMP_SPEED, MIN_JUMP_SPEED
int JUMP_STEPS, JUMP_STEPS_MIN
float AIR_HANG_GRAVITY, AIR_HANG_ACCEL
float DOUBLE_JUMP_RISE_STEPS, DOUBLE_JUMP_FALL_STEPS
float DASH_SPEED, DASH_TIME, AIR_DASH_TIME, DOWN_DASH_TIME
float DASH_COOLDOWN
float WALLSLIDE_STICK_TIME, WALLSLIDE_ACCEL
float NAIL_CHARGE_TIME, NAIL_CHARGE_TIME_QUICK
float DEFAULT_GRAVITY, UNDERWATER_GRAVITY
float RECOIL_HOR_VELOCITY, RECOIL_HOR_VELOCITY_LONG
float BOUNCE_TIME, BOUNCE_VELOCITY, SHROOM_BOUNCE_VELOCITY
// ... 578 个字段总计
```

**特殊移动状态（`ActorStates` 枚举）：**
```
// IDLE, RUNNING, JUMPING, FALLING, WALL_SLIDING, DASHING,
// ATTACKING, STUNNED, DEAD, ...（完整值需 dnSpy 查看）
```

**锁定状态（`HeroLockStates`）：** 控制技能/移动的锁定状态标志位。

### 6.2 工具系统（Tool）

Silksong 特有的道具/工具系统（PlayMaker 中 `AttackToolBinding`）：

```csharp
class InventoryItemToolManager
class InventoryItemTool : InventoryItemToolBase
class InventoryToolCrest               // 徽章槽位
class InventoryToolCrestSlot
class InventoryFloatingToolSlots       // 浮动工具槽
class AttackToolBinding               // 工具与攻击的绑定

// Tool 类数据
class ToolItem                        // 工具 ScriptableObject（? 推测）
class ToolCrestsData                  // PlayerData 中存储的徽章数据
class ToolItemsData, ToolItemLiquidsData  // 工具状态
```

**Charm 系统（护符）：**
```csharp
class CharmItem           // 护符实体
class CharmDisplay        // UI 显示
class CharmIconList       // 图标列表
class BuildEquippedCharms // 装备护符时重建组合效果

// PlayerData 操作：
PlayerData.EquipCharm(int charmID)
PlayerData.UnequipCharm(int charmID)
PlayerData.CalculateNotchesUsed()
PlayerData.RefreshOvercharm()  // 超载护符
```

### 6.3 伤害系统

```csharp
class DamageHero          // 对 Hornet 造成伤害
class DamageEnemies       // 对敌人造成伤害
class DamageReference     // 伤害数据引用
class DamageStack         // 伤害堆叠（类魂踉跄系统？）
class DamageTag / DamageTagInfo  // 伤害类型标签

enum DamagePropertyFlags  // 伤害属性标志
enum DamageMode           // 伤害模式
enum AttackDirection      // 攻击方向
```

**特殊伤害：** `EnemyCoalBurn`（燃烧）、`BlackThreadAttack`（黑线攻击，精英变体）

---

## 7. 存档/读档系统

### 7.1 GameManager 存档相关方法

```csharp
// 存档触发点
SaveGame()                    // 手动存档
QueueSaveGame()               // 队列存档（安全异步）
QueueAutoSave(AutoSaveName)   // 自动存档（场景过渡时）
DoQueuedSaveGame()            // 执行队列中的存档

// 存档数据
CreateSaveGameData()          // 构建存档数据对象
GetBytesForSaveJson()         // JSON 序列化
PreparePlayerDataForSave()    // 存档前清理（OnBeforeSave）
SaveLevelState()              // 保存关卡持久化数据

// 读档
LoadGame(int profileID)
ContinueGame()
LoadGameFromUI()
SetLoadedGameData()

// 存档槽管理
HasSaveFile(int slot)
ClearSaveFile(int slot)
EnsureSaveSlotSpace()
GetSaveStatsForSlot()        // 读取存档摘要（无需完整读档）
```

### 7.2 自动存档触发点（AutoSaveName 枚举）

每次进入 Bench（休息点）或完成特定事件会触发自动存档，`AutoSaveName` 枚举标记存档来源。

### 7.3 存档安全机制

- `DesktopSaveRestoreHandler` — 桌面平台存档恢复
- `DoSaveRestorePoint()` / `CreateRestorePoint()` — 创建还原点（防止存档损坏）

---

## 8. PlayMaker 架构分析

### 8.1 PlayMaker 在 Silksong 中的角色

PlayMaker 是 **Hollow Knight 系列的核心技术选择**，所有：
- NPC 对话流程
- 环境触发器（开门/解锁/播放动画）
- Boss 行为相位
- 场景初始化逻辑
- UI 动画流

都通过 PlayMaker 的可视化状态机实现，而非手写 C#。

**C# 代码的作用**：为 PlayMaker 提供 Action 扩展。每个 `HutongGames.PlayMaker.Actions.XXXX` 类就是一个可在 Inspector 中使用的 PlayMaker Action。

### 8.2 PlayerData 专用 PlayMaker Actions（24 个）

这是 Silksong 进度系统的核心接口——PlayMaker FSM 通过这些 Action 读写玩家数据：

| Action 类 | 功能 |
|-----------|------|
| `GetPlayerDataBool` / `SetPlayerDataBool` | 读/写布尔标记 |
| `PlayerDataBoolTest` | 条件分支 |
| `PlayerDataBoolMultiTest` | 多条件 AND 测试 |
| `PlayerDataBoolAllTrue` | 检查多字段全为 true |
| `PlayerDataBoolTrueAndFalse` | 复合条件 |
| `GetPlayerDataInt` / `SetPlayerDataInt` | 读/写整数 |
| `IncrementPlayerDataInt` | 计数器递增 |
| `DecrementPlayerDataInt` | 计数器递减 |
| `PlayerDataIntAdd` | 整数加法 |
| `PlayerdataIntCompare` | 整数比较 |
| `GetPlayerDataFloat` / `SetPlayerDataFloat` | 读/写浮点 |
| `GetPlayerDataString` / `SetPlayerDataString` | 读/写字符串 |
| `GetPlayerDataVector3` / `SetPlayerDataVector3` | 读/写 Vector3 |
| `GetPlayerDataVariable` / `SetPlayerDataVariable` | 通用变量 |
| `PlayerDataVariableTest` | 变量测试 |
| `SetCollectablePickupPlayerDataBool` | 收集物拾取标记 |
| `CheckPlayerDataTimeLimit` | 时限检测 |
| `SetPlayerDataTimeLimit` | 设置时限 |

---

## 9. 美术资产目录

### 9.1 AssetRipper 导出结果（全项目导出）

位置：`D:\ReferenceAssets\Silksong\Ripped\ExportedProject\`

| 类型 | 数量 |
|------|------|
| Scenes | 4 个（仅主菜单场景，游戏场景通过 Addressables 加载） |
| Scripts | 9,099 个（完整反编译代码！） |
| Texture2D | 52 个（主要资产包中） |
| Sprite | 2 个（主资产包中，Addressables 中有大量 Sprite） |

> **注意**：绝大部分美术资产（角色、场景、UI）在 Addressables 的 `.bundle` 文件中，不在 `resources.assets`。需要 UnityPy 的 `load(GAME_DATA_DIR)` 模式全量扫描。

### 9.2 UnityPy 美术导出

位置：`D:\ReferenceAssets\Silksong\Art\`

（美术导出正在后台运行，包含 Texture2D/Sprites/Audio 分类）

### 9.3 主要视觉风格参考

基于已知的 Silksong 信息和已导出数据：

- **颜色主题**：橙/金/棕（Hornet 配色）+ 暗绿（草地/苔藓区域）+ 蓝紫（神秘区域）
- **粒子风格**：丝绸运动轨迹、针矢投影的残影
- **场景层次**：前景/中景/背景三层视差（继承自 HK1）
- **敌人设计**：节肢动物/昆虫类（蚂蚁、跳蚤、蚊子）+ 骨骸类（骨镇区域）

---

## 10. 地图/区域架构分析

### 10.1 已确认区域及特征

从 `MapZone` 枚举和 PlayerData 字段推断：

| 区域 | 特征线索 |
|------|---------|
| **Bonetown / Path of Bone** | 骨骸主题，骨镇 Boss，Rosary Pilgrim，骨底区域 |
| **Greymoor** | 灰沼，有地图出售，Vampire Gnat Boss，快速旅行节点 |
| **Belltown** | 钟镇，BellBench（存档点），NPC 密集（医生/信使/遗物商） |
| **Shellwood Thicket** | 贝壳树林，地图可购买 |
| **Docks** | 码头，快速旅行节点，Dock Foremen Boss |
| **Coral Gorge / Coral Caverns** | 珊瑚峡谷，快速旅行节点（CoralTower） |
| **Wilds** | 荒野，宽地图（IsWildsWideMapFull 属性） |
| **Dustpens** | 尘笔区 |
| **Library** | 图书馆，Garmond（黑线守卫）相关 |
| **Hunters Nest** | 猎人巢穴，Huntress Quest |
| **Peak** | 山峰，快速旅行节点 |
| **City of Song / Citadel** | 歌之城，内城区 + Citadel SPA |
| **Abyss / Aqueduct** | 深渊 / 水道，快速旅行节点 |
| **Memory** | 叙事记忆关卡（6个已知：Reaper/Wanderer/Beast/Witch/Toolmaster/Shaman） |

### 10.2 特殊系统

**CaravanTroupe（大篷车团）：** `CaravanLocationScenes` + `CaravanTroupeLocations` 枚举 — NPC 旅行团会在不同区域出现，类似 HK1 的流浪商人系统扩展。

**SethNpc 系统：** `SethNpcLocations` 枚举 — Seth 这个 NPC 可能也在多处出现（类似 Grand Nanny Mossbag？）。

**GreenPrince 系统：** `GreenPrinceLocations` 枚举 — 绿王子，可能是重要 NPC/敌人。

---

## 11. 对 Project Ark 的架构启示

### 11.1 Progression 系统借鉴

| Silksong 设计 | Project Ark 对应 | 启示 |
|--------------|-----------------|------|
| `PlayerData` 单体，1,359 字段全扁平化 | `PlayerSaveData` | Silksong 的极端扁平化牺牲了结构但换来 PlayMaker 可直接读写——我们用 ServiceLocator + SaveManager 分层反而更好 |
| `GetBool/SetBool(fieldName)` 反射式 API | `SaveManager.SetFlag(key)` | 类似！但 Silksong 直接反射 C# 字段，我们应该用 Dictionary 键值对更安全 |
| `has*` 布尔控制技能解锁 | `AbilityRegistry` | 可以完全照搬这个模式：新技能只是一个新的 `hasDash: bool` 字段 |
| MapZone 枚举 + `Has*Map` 布尔 | 房间发现系统 | 我们的 `WorldProgressManager` + `MinimapManager` 已经实现了类似机制 |
| `scenesEncounteredBench/Cocoon` | `CheckpointSystem` | 检查点发现记录，可以加入到我们的 PlayerSaveData |
| `HeroCorpseScene/Pos` 遗体系统 | 死亡惩罚 | 《方舟》目前没有遗体系统，可以作为未来特性参考 |

### 11.2 Exploration 系统借鉴

| Silksong 设计 | 借鉴价值 |
|--------------|---------|
| `PersistentBoolItem`（场景内物件状态持久化） | 我们的 `PersistentBoolItem` 概念已经在 Level 模块中存在，可以参考 Silksong 的 `PersistentBoolCollection` 存储结构 |
| `TransitionPoint.dontWalkOutOfDoor` 等细节字段 | 过渡点的人体工程学设计：进入动画方向、延迟进入、总是从右进入等参数 |
| `GameMap.corpseSceneMapZone` — 记录死亡地点区域 | 小地图显示死亡位置（遗体标记） |
| `hasPinXxx` 地图标记系统 | 我们的 `MinimapManager` 可以增加 Pin 类型扩展 |
| Quest + 任务传言（`QuestRumourData`）分离 | 任务发现方式（听闻/直接遭遇）可以影响日志显示 |
| `FastTravelLocations` 枚举 | 我们的 Level 模块可以加入类似的快速旅行节点系统 |

### 11.3 战斗手感

| Silksong 设计 | 借鉴价值 |
|--------------|---------|
| 移动参数全部 `const float` hardcode | 手感调参时直接改代码数值——但我们已经使用 SO，更好 |
| `FreezeMoment()` 顿帧（3 种重载） | 顿帧系统已在我们的 `HitStopEffect` 实现，方法签名设计可参考 |
| `DamageStack` 伤害堆叠 | 多段伤害/持续伤害的堆叠管理机制 |
| `silkSpecialLevel` — 技能等级 | 单个技能有多个等级的字段设计 |

### 11.4 技术决策对比

| 技术点 | Silksong 选择 | Project Ark 选择 | 评估 |
|--------|--------------|-----------------|------|
| 游戏逻辑实现 | PlayMaker FSM | C# HFSM（手写） | Ark 更可维护，但 Silksong 的 PlayMaker 让策划可以无代码修改逻辑 |
| 资产管理 | Addressables | 直接引用 | Addressables 使 Silksong 可以分包，但 Ark 体量更小暂不需要 |
| 输入系统 | InControl（第三方） | Unity New Input System | Ark 更现代 |
| 存档序列化 | Newtonsoft.Json | 自定义 SaveManager | 类似思路 |
| 调试工具 | `TeamCherry.DebugMenu`（专用调试菜单） | 无专用调试面板 | 值得为 Ark 添加简单的调试控制台 |

---

## 12. 资产规模

| 类型 | 数量/大小 |
|------|---------|
| Assembly-CSharp.dll | 6.8 MB |
| 总类型数 | 5,079 |
| PlayMaker Actions | 2,240 |
| 核心游戏类 | 2,015 |
| PlayerData 字段 | 1,359 |
| GameManager 方法 | 323 |
| HeroController 字段 | 578 |
| HeroController 方法 | 641 |
| MapZone 枚举 | 42 个区域 |
| FastTravel 节点 | 13 个 |
| 地图碎片 | 28 个 |
| Quest 类型 | 30+ NPC |
| Boss | 15+ 已识别 |
| AssetRipper 导出 Scripts | 9,099 个 .cs 文件 |
| Addressables bundles | ~478 个 |

---

## 13. 文件索引

| 文件 | 描述 |
|------|------|
| `D:\ReferenceAssets\Silksong\Ripped\` | AssetRipper 全项目导出（含反编译 Scripts） |
| `D:\ReferenceAssets\Silksong\Art\` | UnityPy 美术资产导出（Texture2D/Sprites/Audio） |
| `D:\ReferenceAssets\Silksong\export_art2.py` | 美术导出脚本 |
| `D:\ReferenceAssets\Silksong\probe.py` | 资产类型探测脚本 |
| `F:\SteamLibrary\steamapps\common\Hollow Knight Silksong\` | 原始游戏目录（Mono 后端） |

---

## 14. 法律声明

本报告仅供个人学习和架构研究使用。Hollow Knight Silksong 及其所有代码/美术资产的版权归 Team Cherry 所有。任何导出的资产**严禁**用于商业项目。

