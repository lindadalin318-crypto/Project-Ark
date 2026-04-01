# TUNIC — 完整项目结构分析

> **分析日期**：2026-03-08  
> **Unity 版本**：2020.3.17（IL2CPP 构建）  
> **构建类型**：**IL2CPP**（`GameAssembly.dll` 21MB，`global-metadata.dat` 4.8MB）  
> **音频系统**：**FMOD Studio**（全部 .bank 文件，无 Unity AudioClip）  
> **输入系统**：**InControl**（第三方跨平台输入库）  
> **关卡建模**：**ProBuilder**（运行时关卡几何体）  
> **后处理**：**AmplifyColor**（色彩分级）+ Unity Standard Image Effects  
> **数据序列化**：**Newtonsoft.Json**  
> **AI 寻路**：**NavMesh Components**  
> **源文件路径**：`F:\SteamLibrary\steamapps\common\TUNIC\`  
> **Il2CppDumper 输出**：`D:\Tools\TUNIC_dump\`

---

## 一、项目概览

TUNIC 是一款**等距视角动作冒险**游戏，玩家扮演一只小狐狸，在一个充满谜题和秘密的世界中探索。游戏最核心的设计哲学是：**游戏手册本身就是谜题**——玩家通过收集散落在世界各处的手册页面来理解游戏规则，而手册使用一种虚构的文字系统（Glyph）书写。

### 核心数字

| 类别 | 数量 |
|------|------|
| C# 类型（Assembly-CSharp.dll） | **754 个**（660 个游戏代码类） |
| 场景数量（GI 光照贴图目录） | **72 个**场景（level5 ~ level84） |
| FMOD 音频 Bank 文件 | **137 个**（含每个敌人独立 bank） |
| 主音乐 Bank | `music_main.bank`（361 MB！） |
| 资产包 | `data.unity3d`（882 MB） |
| 成就数量 | **37 个**（AchievementID 枚举） |
| 升级类型 | **6 种**（UpgradeType 枚举） |
| 饰品效果 | **16 种**（TrinketEffect 枚举） |
| 伤害类型 | **13 种**（HitType 枚举） |
| 物品类型 | **8 种**（Item.ItemType 枚举） |

### 第三方库清单

| 库 | 用途 |
|----|------|
| **InControl** | 跨平台输入（手柄/键鼠统一抽象） |
| **FMOD Studio** | 音频引擎（全部音效/音乐） |
| **ProBuilder** | 关卡几何体建模（运行时 Mesh） |
| **AmplifyColor** | 色彩分级后处理 |
| **Newtonsoft.Json** | JSON 序列化 |
| **NavMesh Components** | AI 寻路 |
| **Unity.Timeline** | 过场动画 |
| **TextMeshPro** | 文字渲染 |
| **Steamworks.NET** | Steam 成就/存档 |
| **XGamingRuntime** | Xbox 平台支持 |

---

## 二、代码架构总览

### 2.1 类型分布

```
Assembly-CSharp.dll（754 个类型）
├── 根命名空间（游戏代码）：660 个
├── UnityStandardAssets.ImageEffects：40 个（后处理效果）
├── AmplifyColor：11 个（色彩分级）
├── GameHelp：11 个（游戏帮助系统）
├── PigeonCoopToolkit.Effects.Trails：8 个（拖尾效果）
├── Lib.Build：6 个（构建工具）
└── 其他：18 个
```

### 2.2 核心实体层次

```
MonoBehaviour
├── PlayerCharacter          ← 玩家（小狐狸）
│   ├── PlayerInput          ← 输入处理（InControl）
│   ├── PlayerCharacterActionSet  ← 动作集合
│   └── PlayerStaminaHUD     ← 耐力 HUD
├── Creature                 ← 所有生物基类
│   ├── Monster              ← 普通敌人基类
│   │   ├── NavigatingMonster ← 有寻路能力的敌人
│   │   └── [各种具体敌人]
│   └── [Boss 类]
│       ├── CryptBoss        ← 地下室 Boss
│       ├── ScavengerBoss    ← 拾荒者 Boss
│       └── Foxgod           ← 最终 Boss（狐狸神）
└── Item                     ← 物品基类
    └── ItemBehaviour        ← 物品行为基类
        ├── SwordItemBehaviour
        ├── LanternItemBehaviour
        ├── CrossbowItemBehaviour
        └── [其他武器/道具]
```

### 2.3 关键管理器

```
MonsterManager          ← 怪物管理（生成/追踪/清除）
ShopManager             ← 商店管理
BloodstainManager       ← 血迹（死亡记录）管理
PermanentStateByPositionManager  ← 位置状态持久化
AnnotationManager       ← 注释/标记管理
PlatformSaveManager     ← 跨平台存档管理
```

---

## 三、Progression System（进度系统）— 重点分析

### 3.1 StateVariable 系统（核心架构）

TUNIC 的进度系统完全基于 **`StateVariable`**（ScriptableObject）：

```csharp
// StateVariable : ScriptableObject
public class StateVariable : ScriptableObject {
    [SerializeField] protected Achievements.AchievementID unlockAchievement; // 解锁成就
    protected string _cachedName;
    protected List<Action> subscribers;           // 订阅者列表（观察者模式）
    protected static List<StateVariable> stateVariableList;
    protected static readonly string kStatevarPath; // 存档路径键

    public int IntValue { get; set; }   // 整数值（用于计数/枚举状态）
    public bool BoolValue { get; set; } // 布尔值（用于开关/解锁）

    // 静态方法
    public static void ResetAllStateVariables();
    public static void NotifyAllSubscribers();
    public static StateVariable GetStateVariableByName(string name);

    // 订阅/取消订阅
    public void SubscribeToChanges(Action callback);
    public void UnSubscribeToChanges(Action callback);
}
```

**设计要点**：
- `StateVariable` 是 **ScriptableObject**，在 Unity Editor 中创建，每个游戏状态对应一个 SO 资产
- 支持 `IntValue`（多状态）和 `BoolValue`（开关）两种模式
- 内置**观察者模式**（`subscribers` 列表），状态变化时自动通知所有监听者
- 每个 `StateVariable` 可以绑定一个 `AchievementID`，状态变化时自动触发成就
- `kStatevarPath` 是存档键，通过 `PCSaveSystem` 持久化到磁盘

### 3.2 StateVariable 的使用模式

```
// 场景中的对象通过 StateVariable 控制显隐
StatefulActive : MonoBehaviour {
    [SerializeField] StateVariable enablingStateVariable;
    [SerializeField] bool activeWhenFalse;
    // Awake 时订阅，OnDestroy 时取消订阅
}

// 材质切换
StatefulMaterialSwap : MonoBehaviour {
    [SerializeField] StateVariable stateVariable;
    // 根据 StateVariable 值切换材质
}

// 摄像机区域
StatefulCameraZone : MonoBehaviour {
    [SerializeField] StateVariable stateVariable;
}

// 触发器
StateVarTrigger : MonoBehaviour {
    // 玩家进入时设置 StateVariable
}

// 条件节点
ActiveByConduitNode : MonoBehaviour {
    // 根据 ConduitNode 的通电状态激活
}
```

### 3.3 ConduitNode 系统（谜题/电路系统）

TUNIC 有一套**电路/导管系统**，用于控制谜题机关：

```csharp
// ConduitNode_SO : ScriptableObject
public class ConduitNode_SO : ScriptableObject {
    [SerializeField] StateVariable isPowerSourceStateVar; // 是否为电源
    [SerializeField] List<ConduitNode_SO> connectedNodes; // 连接的节点

    public bool Closed { get; }         // 是否关闭（断路）
    public bool IsPoweredSource { get; } // 是否为电源节点
    public bool Powered { get; }         // 是否通电（递归检查连接）

    // 递归检查是否连接到电源
    protected static bool checkConnectedToPower(ConduitNode_SO cn);
}

// ConduitPath : MonoBehaviour（可视化导线）
// ConduitPlatform : MonoBehaviour（通电后激活的平台）
// ConduitPowerSource : MonoBehaviour（电源）
// ConduitTeleporter : MonoBehaviour（通电后激活的传送点）
// ConduitData / ConduitDataEntry（导管数据）
// IntersceneConduitID（跨场景导管 ID）
```

**设计意义**：ConduitNode 系统是 TUNIC 谜题的核心——玩家需要找到并激活电源节点，通过导线传导能量，解锁机关、平台、传送点。这是一个**图论问题**（连通性检查）。

### 3.4 升级系统（UpgradeAltar）

```csharp
// UpgradeType 枚举（6 种升级）
enum UpgradeType {
    HP_MAX = 0,        // 最大生命值
    SP_MAX = 1,        // 最大耐力值
    MP_MAX = 2,        // 最大魔力值
    ATTACK = 3,        // 攻击力
    DAMAGE_REDUCE = 4, // 减伤
    POTION_RECOVER = 5 // 药水回复量
}

// UpgradeAltar : MonoBehaviour（升级祭坛）
// UpgradeStatue : MonoBehaviour（升级雕像）
// UpgradeMenu : MonoBehaviour（升级菜单）
// UpgradePresentation : MonoBehaviour（升级展示）
```

**升级货币**：游戏使用**金币（Coin）** + **宝石（Gem）** 双货币系统：
- `CoinSpawner`：金币生成器
- `SmallMoneyItem`、`SupplyCoinItem`：不同面值金币
- `PiggybankItemBehaviour`：存钱罐（特殊物品）

### 3.5 物品系统（Item）

```csharp
// Item.ItemType 枚举（8 种物品类型）
enum Item.ItemType {
    EQUIPMENT = 0,    // 装备（武器/防具）
    SUPPLIES = 1,     // 消耗品
    WELL_SUPPLIES = 2, // 井水补给
    FLASK = 3,        // 药瓶
    MONEY = 4,        // 货币
    GEAR = 5,         // 齿轮（升级材料）
    OFFERINGS = 6,    // 祭品
    TRINKETS = 7      // 饰品
}
```

**武器/装备列表**（从 ItemBehaviour 子类推断）：
| 武器 | 类 |
|------|-----|
| 剑 | `SwordItemBehaviour` |
| 十字架 | `CrossItemBehaviour` |
| 弩 | `CrossbowItemBehaviour` |
| 长矛 | `SpearItemBehaviour` |
| 科技弓 | `TechbowItemBehaviour` |
| 力量魔杖 | `ForcewandItemBehaviour` |
| 霰弹枪 | `ShotgunItemBehaviour` |
| 炸弹瓶 | `BombFlask` |
| 火把 | `TorchItemBehaviour` |
| 灯笼 | `LanternItemBehaviour` |
| 常春藤 | `IvyItemBehaviour` |
| 胡椒 | `PepperItemBehaviour` |
| 解毒剂 | `AntidoteItemBehaviour` |
| 慢动作 | `SlowmoItemBehaviour` |

### 3.6 饰品系统（Trinket）

```csharp
// TrinketEffect 枚举（16 种饰品效果）
enum TrinketEffect {
    RTSR = 0,                  // 低血量攻击提升（红色戒指）
    BTSR = 1,                  // 低血量防御提升（蓝色戒指）
    BLOCK_PLUS = 2,            // 格挡增强
    WALK_SPEED_PLUS = 3,       // 移速提升
    SNEAKY = 4,                // 潜行（减少敌人感知）
    STAMINA_RECHARGE_PLUS = 5, // 耐力恢复加速
    FAST_ICEDAGGER = 6,        // 冰匕首加速
    HEARTDROPS = 7,            // 击杀掉落心脏
    BLOODSTAIN_PLUS = 8,       // 血迹奖励增加
    ATTACK_UP_DEFENSE_DOWN = 9, // 攻击↑防御↓
    BLOODSTAIN_MP = 10,        // 血迹回复魔力
    MP_FLASKS = 11,            // 药瓶回复魔力
    MASK = 12,                 // 面具（隐身）
    GLASS_CANNON = 13,         // 玻璃炮（高攻低防）
    PARRY_WINDOW = 14,         // 格挡窗口扩大
    IFRAMES_UP = 15            // 无敌帧增加
}

// TrinketSlot : MonoBehaviour（饰品槽位）
// TrinketItem : MonoBehaviour（饰品物品）
// TrinketCoinItemBehaviour（饰品货币）
```

### 3.7 手册页面系统（Page System）

TUNIC 最独特的 Progression 设计——**游戏手册页面**：

```csharp
// PageData : MonoBehaviour（页面数据）
public class PageData : MonoBehaviour {
    public Texture2D grungifyTexture; // 做旧纹理
}

// PageDisplay（页面显示，有 6 个 PageSide 状态）
enum PageDisplay.PageSide {
    PREV_LEFT = 0,   // 上一页左面
    PREV_RIGHT = 1,  // 上一页右面
    FACING = 2,      // 面对页
    CURRENT = 3,     // 当前页
    NEXT_LEFT = 4,   // 下一页左面
    NEXT_RIGHT = 5   // 下一页右面
}

// PagePickup : MonoBehaviour（页面拾取）
// FinalPagePickup : MonoBehaviour（最终页面拾取）
// FALetterPickup : MonoBehaviour（字母拾取）
```

**设计意义**：手册页面是 TUNIC 的核心 Progression 机制——每张页面揭示游戏规则的一部分（用虚构文字书写），玩家通过收集页面逐渐理解游戏。`YouAreHereMapper` 和 `YouAreHereMarker` 负责在手册地图页上显示玩家当前位置。

### 3.8 血迹系统（Bloodstain）

类魂风格的死亡记录系统：

```csharp
// BloodstainChest : MonoBehaviour（血迹宝箱，死亡时掉落）
// BloodstainManager : MonoBehaviour（血迹管理器）
// BloodstainRecoveryNotification : MonoBehaviour（血迹回收通知）
```

### 3.9 存档系统（Save System）

```csharp
// PCSaveSystem : PlatformSaveInterface（PC 存档实现）
public class PCSaveSystem : PlatformSaveInterface {
    public string PersistentDataPath { get; }
    public void CreateFile(string filename);
    public void DeleteFile(string filename);
    public bool FileExists(string filename);
    public string[] ReadAllLines(string filename);
    public void WriteAllText(string filename, string contents);
    // PlayerPrefs 风格的 Key-Value 存储
    public void SetInt(string key, int value);
    public void SetFloat(string key, float value);
    public void SetString(string key, string value);
    public int GetInt(string key, int defaultValue);
    public float GetFloat(string key, float defaultValue);
    public string GetString(string key, string defaultValue);
}

// PlatformSaveManager（跨平台存档管理器）
// PlatformSaveInterface（存档接口，支持 PC/Xbox/PS4/Switch）
// SaveFile（存档文件数据）
// SaveDataDirNames（存档目录名）
// SpeedrunData（速通数据）
// OnSaveSceneAttribute（场景存档标记）
```

**存档策略**：
- `StateVariable` 通过 `kStatevarPath` 键存储到 `PCSaveSystem`
- 支持多存档槽（`savefiles_select_file` FMOD 事件）
- 存档点是**篝火/营地**（`ReturnToCampfireTrigger`）
- 速通模式有独立的 `SpeedrunData` 记录

### 3.10 成就系统

```csharp
// Achievements.AchievementID 枚举（37 个成就）
enum AchievementID {
    NONE = 0,
    STICK = 1,          // 获得棍子
    SWORD = 2,          // 获得剑
    RESURRECTION = 3,   // 复活
    EASTBELL = 4,       // 东方钟
    WESTBELL = 5,       // 西方钟
    FUSE = 6,           // 保险丝
    OFFERING = 7,       // 祭品
    HEX1-3 = 8-10,      // 六边形谜题 1-3
    GYRO = 11,          // 陀螺仪
    DASH = 12,          // 冲刺
    RELICS = 13,        // 遗物
    BADEND = 14,        // 坏结局
    MANUAL = 15,        // 手册完成
    FAIRY = 16,         // 仙女
    GT1-GT12 = 17-28,   // 黄金宝藏 1-12
    GRASS = 29,         // 割草
    PIGGYBANKS = 30,    // 存钱罐
    COIN1/COIN15 = 31-32, // 金币
    ROLLSTAB = 33,      // 翻滚刺击
    WRONGFIGHT = 34,    // 错误战斗
    FREEZESELF = 35,    // 冻结自己
    BONUSBOMB = 36      // 奖励炸弹
}
```

---

## 四、Exploration System（探索系统）— 重点分析

### 4.1 场景结构（72 个场景）

TUNIC 使用**独立场景加载**（非 Additive），每个区域是一个独立场景：

```
场景列表（从 FMOD bank 文件名 + stringliteral 推断）：
├── Overworld Redux          ← 主世界（核心枢纽）
├── Overworld Cave           ← 主世界洞穴
├── Overworld Interiors      ← 主世界内部
├── Playable Intro           ← 可玩序章
├── East Forest Redux        ← 东方森林
├── East Forest Redux Interior ← 东方森林内部
├── East Forest Redux Laddercave ← 东方森林梯子洞穴
├── Forest Belltower         ← 森林钟楼
├── Forest Boss Room         ← 森林 Boss 房间
├── Fortress Main            ← 要塞主体
├── Fortress East            ← 要塞东部
├── Fortress Courtyard       ← 要塞庭院
├── Fortress Basement        ← 要塞地下室
├── Fortress Reliquary       ← 要塞圣物室
├── Fortress Arena           ← 要塞竞技场
├── Library Hall             ← 图书馆大厅
├── Library Lab              ← 图书馆实验室
├── Library Rotunda          ← 图书馆圆形大厅
├── Library Exterior         ← 图书馆外部
├── Cathedral Redux          ← 大教堂
├── Cathedral Arena          ← 大教堂竞技场
├── Crypt Redux              ← 地下室
├── Swamp Redux              ← 沼泽
├── Quarry Redux             ← 采石场
├── Atoll Redux              ← 环礁
├── Archipelagos Redux       ← 群岛
├── Mountain                 ← 山脉
├── Mountaintop              ← 山顶
├── Monastery                ← 修道院
├── Ziggurat 0-3             ← 金字塔（4 层）
├── Ziggurat FTRoom          ← 金字塔特殊房间
├── Temple                   ← 神殿
├── Sewer                    ← 下水道
├── Sewer Boss               ← 下水道 Boss
├── Shop                     ← 商店
├── ShopSpecial              ← 特殊商店
├── Town Basement            ← 城镇地下室
├── Town FiligreeRoom        ← 城镇花丝房间
├── Ruined Shop              ← 废墟商店
├── Ruins Passage            ← 废墟通道
├── Waterfall                ← 瀑布
├── Windmill                 ← 风车
├── Maze Room                ← 迷宫房间
├── Dusty                    ← 尘土区域
├── Frog Stairs              ← 青蛙楼梯
├── Frog Cave Main           ← 青蛙洞穴
├── Sword Cave               ← 剑洞穴
├── Sword Access             ← 剑入口
├── Transit                  ← 传送区域
├── Resurrection             ← 复活区域
├── Spirit Arena             ← 灵魂竞技场
├── RelicVoid                ← 遗物虚空
├── CubeRoom                 ← 立方体房间
├── ChangingRoom             ← 变换房间
├── EastFiligreeCache        ← 东方花丝缓存
├── PatrolCave               ← 巡逻洞穴
├── Darkwoods Tunnel         ← 黑暗森林隧道
├── Purgatory                ← 炼狱
└── [其他 ~20 个场景]
```

### 4.2 场景切换系统

```csharp
// ScenePortal : MonoBehaviour（场景传送门）
// GoToScene : MonoBehaviour（切换到指定场景）
// GoToSceneOnEnable : MonoBehaviour（启用时切换场景）
// PortalToSceneAfterDelay : MonoBehaviour（延迟后切换场景）
// SceneLoader : MonoBehaviour（场景加载器）
// ShopScenePortal : MonoBehaviour（商店场景传送门）
// BeachTeleporter : MonoBehaviour（海滩传送器）
// TeleportStone : MonoBehaviour（传送石）
// TeleportSpell : MonoBehaviour（传送法术）
// ConduitTeleporter : MonoBehaviour（导管传送器，通电后激活）
// ReadSceneNames : MonoBehaviour（读取场景名列表）
```

### 4.3 区域标签系统（Area Label）

```csharp
// AreaData : ScriptableObject（区域数据）
public class AreaData : ScriptableObject {
    public LanguageLine topLine;    // 区域名称上行（支持多语言）
    public LanguageLine bottomLine; // 区域名称下行
}

// AreaLabel : MonoBehaviour（区域标签显示）
public class AreaLabel : MonoBehaviour {
    protected MixedTextBuilder topLine;
    protected MixedTextBuilder bottomLine;
    protected static AreaLabel instance;
    protected Animator cachedAnimator;

    public static void ShowLabel(AreaData area);  // 显示区域名称
    public static void CancelShowLabel();          // 取消显示
}

// AreaLabelOnLoad : MonoBehaviour（场景加载时显示区域标签）
// AreaLabelZone : MonoBehaviour（进入区域时触发标签）
```

**设计意义**：进入新区域时，屏幕上会显示区域名称（类似黑魂的区域提示）。`AreaData` 是 ScriptableObject，支持多语言（`LanguageLine`）。

### 4.4 地图系统（Manual Map）

TUNIC 的地图**就是手册的一页**，而不是独立的 UI 系统：

```csharp
// YouAreHereMapper : MonoBehaviour（地图上的"你在这里"标记）
public class YouAreHereMapper : MonoBehaviour {
    public int leafNumber;    // 手册页码
    public bool front;        // 正面/背面
    public Transform guideQuad; // 引导四边形
}

// YouAreHereMarker : MonoBehaviour（标记显示）
public class YouAreHereMarker : MonoBehaviour {
    public void Setup(int page, bool front);
    protected void setPosition(YouAreHereMapper yahm);
}
```

**设计意义**：玩家在手册中翻到地图页时，会看到一个小标记显示当前位置。这个标记通过 `YouAreHereMapper` 组件（放置在场景中）和 `YouAreHereMarker`（放置在手册 UI 上）配合实现。

### 4.5 门/障碍系统

```csharp
// Door : MonoBehaviour（普通门）
// ProximityDoor : MonoBehaviour（接近触发门）
// HolySealDoor : MonoBehaviour（圣印门，需要圣印物品）
// TempleDoor : MonoBehaviour（神殿门）
// ObsidianDoorway : MonoBehaviour（黑曜石门道）
// SecretPassagePanel : MonoBehaviour（秘密通道面板）
// TropicalSecret : MonoBehaviour（热带秘密）
// Ladder : MonoBehaviour（梯子）
// LadderEnd : MonoBehaviour（梯子末端）
// DungeonRoom : MonoBehaviour（地牢房间）
// EndRoom : MonoBehaviour（终点房间）
```

### 4.6 谜题/机关系统

```csharp
// SwitchType 枚举（4 种开关类型）
enum SwitchType {
    STONE = 0,           // 石头开关
    STONE_BIG = 1,       // 大石头开关
    ROPE = 2,            // 绳子开关
    EXTENDING_BRIDGE = 3 // 延伸桥
}

// HexagonItemRecepticle : MonoBehaviour（六边形物品插槽）
// SealCombine : MonoBehaviour（印章组合）
// PotionCombine : MonoBehaviour（药水组合）
// OfferingCollection : MonoBehaviour（祭品收集）
// OfferingItem : MonoBehaviour（祭品物品）
// IdolAltar : MonoBehaviour（偶像祭坛）
// AudioPuzzleAssistance : MonoBehaviour（音频谜题辅助）
// GoldenTrophyRoom : MonoBehaviour（黄金奖杯房间）
```

### 4.7 敌人生成系统

```csharp
// EnemySpawner : MonoBehaviour（敌人生成器）
// GauntletSpawner : MonoBehaviour（挑战关卡生成器）
// CathedralGauntletManager : MonoBehaviour（大教堂挑战管理器）
// CathedralGauntletSummoner : MonoBehaviour（大教堂挑战召唤器）
// MonsterAggroGate : MonoBehaviour（怪物仇恨门）
// MonsterEgg : MonoBehaviour（怪物蛋）
// DUMBSPAWN : MonoBehaviour（简单生成器）
// PlayerMonstersZone : MonoBehaviour（玩家怪物区域）
```

### 4.8 环境/氛围系统

```csharp
// DarkZone : MonoBehaviour（黑暗区域，需要灯笼）
// NeedLanternInteractionTrigger（需要灯笼的交互触发器）
// PoisonZone : MonoBehaviour（毒素区域）
// TrapTileZone : MonoBehaviour（陷阱地砖区域）
// HideGeometryZone : MonoBehaviour（隐藏几何体区域）
// NoSSAOZone : MonoBehaviour（无 SSAO 区域）
// PerspectiveCameraZone : MonoBehaviour（透视摄像机区域）
// StatefulCameraZone : MonoBehaviour（状态摄像机区域）
// CameraZoneByParameter : MonoBehaviour（参数摄像机区域）
// AmbientRadiationGradient : MonoBehaviour（环境辐射渐变）
// DayNightBridge.DayNight（昼夜系统）
// AtollMusicControl : MonoBehaviour（环礁音乐控制）
```

---

## 五、战斗系统

### 5.1 伤害类型

```csharp
// HitType 枚举（13 种伤害类型）
enum HitType {
    NORMAL = 0,    // 普通
    EXPLOSIVE = 1, // 爆炸
    FIRE = 2,      // 火焰
    STICK = 3,     // 棍击
    STAB = 4,      // 刺击
    POISON = 5,    // 毒素
    STUNNER = 6,   // 眩晕
    TECHBOW = 7,   // 科技弓
    WAND = 8,      // 魔杖
    CROSS = 9,     // 十字架
    LASER = 10,    // 激光
    VOID = 11,     // 虚空
    KICK = 12      // 踢击
}
```

### 5.2 战斗核心类

```csharp
// HitReceiver : MonoBehaviour（受击接收器）
// HitReceiverForTriggers : MonoBehaviour（触发器受击接收器）
// HitTrigger : MonoBehaviour（攻击触发器）
// IDamageable（可受伤接口）
// HitQuiver : MonoBehaviour（受击抖动效果）
// ProjectileShadow : MonoBehaviour（投射物阴影）
// RaycastProjectile : MonoBehaviour（射线投射物）
// RaycastProjectile_LibrarianOrb : MonoBehaviour（图书馆员球形投射物）
// LanternProjectile : MonoBehaviour（灯笼投射物）
// TankProjectile : MonoBehaviour（坦克投射物）
// TankController : MonoBehaviour（坦克控制器）
// TankWeaponController : MonoBehaviour（坦克武器控制器）
```

### 5.3 法术系统

```csharp
// MagicSpell : MonoBehaviour（魔法法术基类）
// HealSpell : MonoBehaviour（治疗法术）
// FairySpell : MonoBehaviour（仙女法术）
// TeleportSpell : MonoBehaviour（传送法术）
// DollSpell : MonoBehaviour（玩偶法术）
// BHMSpell : MonoBehaviour（BHM 法术）
// CheapIceboltSpell : MonoBehaviour（廉价冰箭法术）
// RealestSpell : MonoBehaviour（真实法术）
// TestingSpell : MonoBehaviour（测试法术）
// SpellListenerBasic : MonoBehaviour（法术监听器）
// ToggleObjectBySpell : MonoBehaviour（法术切换对象）
```

### 5.4 玩家状态

```csharp
// PlayerCharacter.StatState 枚举
enum StatState {
    NORMAL = 0,    // 正常状态
    SUPRESSED = 1, // 被压制（某些区域禁用能力）
    HERO = 2       // 英雄状态（特殊增强）
}

// StaminaCostType（耐力消耗类型）
// StaminaRechargeType（耐力恢复类型）
// StaminaSettings.StaminaRequirementMode（耐力需求模式）
// FacingLockType（朝向锁定类型）
// GroundingTechnique（接地技术）
// StatSuppressionField（属性压制场）
```

---

## 六、AI/敌人系统

### 6.1 敌人类型（从 FMOD bank 文件名推断）

| 敌人 | Bank 文件 | 类型 |
|------|-----------|------|
| Administrator | `en_administrator.bank` | 最终 Boss |
| Beefboy | `en_beefboy.bank` | 普通敌人 |
| Blob / BlobCorrupted | `en_blob.bank` | 史莱姆 |
| Bomezome 系列 | `en_bomezome*.bank` | 骷髅系列（4 种变体） |
| Fox God | `en_boss_foxgod.bank` | 最终 Boss |
| Librarian | `en_boss_librarian.bank` | 图书馆 Boss |
| Scavenger Boss | `en_boss_scavenger.bank` | 拾荒者 Boss |
| Spider Tank | `en_boss_spidertank.bank` | 蜘蛛坦克 Boss |
| Crabbit / Crabbo | `en_crabbit.bank` | 螃蟹系列 |
| Crocodoo / CrocodooVoid | `en_crocodoo*.bank` | 鳄鱼系列 |
| Crow / CrowVoid | `en_crow*.bank` | 乌鸦系列 |
| Fairy Probe | `en_fairyProbe*.bank` | 仙女探针 |
| Fox Enemy / FoxZombie | `en_foxEnemy*.bank` | 狐狸敌人 |
| Frog 系列 | `en_frog*.bank` | 青蛙系列（4 种） |
| Ghost Fox Monster | `en_ghostfoxMonster.bank` | 幽灵狐狸 |
| Gunslinger | `en_gunslinger.bank` | 枪手 |
| Hedgehog 系列 | `en_hedgehog*.bank` | 刺猬系列 |
| Honour Guard | `en_honourguard.bank` | 荣誉卫士 |
| Plover | `en_plover.bank` | 鸻鸟 |
| Scavenger 系列 | `en_scavenger*.bank` | 拾荒者系列（4 种） |
| Skuladot 系列 | `en_skuladot*.bank` | 骷髅点系列（5 种） |
| Slorm 系列 | `en_slorm*.bank` | 蠕虫系列（3 种） |
| Spider 系列 | `en_spider*.bank` | 蜘蛛系列（2 种） |
| Tentacle | `en_tentacle.bank` | 触手 |
| Tonguebat 系列 | `en_tonguebat*.bank` | 舌蝙蝠系列 |
| Tunic Knight 系列 | `en_tunic_knight*.bank` | 铠甲骑士系列 |
| Turret | `en_turret.bank` | 炮台 |
| Voidling / Voidtouched | `en_void*.bank` | 虚空系列 |
| Wizard 系列 | `en_wizard*.bank` | 巫师系列（4 种） |

### 6.2 AI 架构

```csharp
// Monster : MonoBehaviour（怪物基类）
// NavigatingMonster : MonoBehaviour（有寻路能力的怪物）
// MonsterManager : MonoBehaviour（怪物管理器）
// MonsterStateBehaviour（怪物状态行为，Animator StateMachineBehaviour）
// MonsterStateMonitor（怪物状态监控）
// IMonsterStateListener（怪物状态监听接口）
// AggroTrigger（仇恨触发器）
// AlertEffect（警觉效果）
// BossAnnouncer / BossAnnounceOnAggro（Boss 公告）
// EnemyAggroBGMControl（仇恨时 BGM 控制）
// EnemyHealthHUD（敌人血量 HUD）
// PatrolTransform（巡逻路径点）
// PatrollingShadowTrigger（巡逻阴影触发器）
// PrayerListenerAggroMonster（祈祷触发仇恨）
// Bait（诱饵）
// Axefriend（斧头朋友，特殊敌人）
```

---

## 七、美术资产结构

### 7.1 资产存储策略

```
TUNIC 的资产存储策略：
  data.unity3d（882 MB）← 所有 Unity 资产（贴图/Mesh/Prefab/动画/材质）
  StreamingAssets/     ← 仅 FMOD 音频 bank 文件（137 个）
  GI/                  ← 光照贴图（72 个场景，共 ~1500 个光照贴图文件）
  Resources/           ← 仅 unity default resources（4.7 MB）
```

**关键洞察**：TUNIC 把几乎所有资产打包进 `data.unity3d`，这是标准的 Unity 发布方式。音频完全使用 FMOD，不在 Unity 资产包中。

### 7.2 FMOD 音频系统

```
StreamingAssets/（137 个 .bank 文件）
├── Master Bank.bank（86 KB）         ← FMOD 主 Bank
├── Master Bank.strings.bank（72 KB） ← 字符串表
├── music_main.bank（361 MB！）        ← 所有音乐（极大）
├── sfx_player.bank（14.8 MB）         ← 玩家音效
├── sfx_global_*.bank（4 个）          ← 全局音效
├── sfx_scene_*.bank（~60 个）         ← 每个场景的环境音效
├── sfx_ui.bank（3.5 MB）              ← UI 音效
└── en_*.bank（~60 个）                ← 每个敌人的独立音效 bank
```

**FMOD 事件路径（从 stringliteral.json 提取）**：
```
event:/main/player/general/parry_impact     ← 格挡音效
event:/main/player/general/hyperdash        ← 超级冲刺
event:/main/player/general/spell_heal       ← 治疗法术
event:/main/player/general/weapon_deflect   ← 武器偏转
event:/main/player/item/weapon/stundagger_enemy_freeze ← 冰匕首冻结
event:/main/enemy/shared/death              ← 敌人死亡（共享）
event:/main/enemy/shared/hit               ← 敌人受击（共享）
event:/main/music/forest/miniboss          ← 森林小 Boss 音乐
event:/main/music/library/librarian        ← 图书馆员音乐
event:/main/ui/gameplay/area_title         ← 区域标题显示
event:/main/ui/gameplay/secret_found       ← 发现秘密
event:/main/ui/gameplay/savepoint_offering_result ← 存档点祭品结果
event:/main/ui/inventory/trinketmenu_open  ← 饰品菜单打开
```

### 7.3 光照系统

TUNIC 使用**烘焙光照**（Baked Lightmaps）：
- 72 个场景，每个场景有独立的光照贴图目录（`GI/level5/` ~ `GI/level84/`）
- 最大场景（level59/level60/level84）有 93-105 个光照贴图文件
- 使用 Unity 标准 GI 系统（非 HDRP/URP）

### 7.4 后处理系统

```
AmplifyColor（色彩分级）：
  AmplifyColorBase / AmplifyColorEffect
  AmplifyColorRenderMask / AmplifyColorRenderMaskBase
  AmplifyColorTriggerProxy / AmplifyColorTriggerProxy2D
  AmplifyColorVolume / AmplifyColorVolume2D
  → 通过 Volume 区域切换色彩 LUT，实现不同区域的视觉风格

Unity Standard Image Effects（后处理）：
  Bloom / BloomOptimized
  DepthOfField / DepthOfFieldDeprecated
  CameraMotionBlur
  ScreenSpaceAmbientOcclusion（SSAO）
  SunShafts（光束）
  TiltShift（移轴效果）
  Tonemapping（色调映射）
  VignetteAndChromaticAberration（暗角+色差）
  EdgeDetection（边缘检测）
```

### 7.5 特殊渲染系统

```csharp
// PlayerPalette : MonoBehaviour（玩家调色板）
// PlayerPaletteTrigger : MonoBehaviour（调色板触发器）
// PlayerPaletteResetter : MonoBehaviour（调色板重置器）
// PlayerPalette.PaletteDial 枚举（调色板拨盘）
// CreatureMaterialManager（生物材质管理器）
// CreatureMaterialManager.IceState（冰冻状态材质）
// CreatureWaterFringeFX（生物水边缘效果）
// BlitRTexToDisplay（渲染纹理到显示器）
// CopyShadowMap（复制阴影贴图）
// SSAOFadeController（SSAO 淡入淡出控制器）
// RuntimeCameraQualityController（运行时摄像机质量控制）
// BoidFlock（鸟群模拟）
// Boid（单个鸟群个体）
```

---

## 八、Glyph 系统（虚构文字）

TUNIC 最独特的设计——游戏内使用虚构文字系统：

```csharp
// GlyphType 枚举（8 种字形类型）
enum GlyphType {
    VOWEL = 0,       // 元音
    CONSONANT = 1,   // 辅音
    PUNCTUATION = 2, // 标点
    NUMBER = 3,      // 数字
    ROMAN = 4,       // 罗马字母
    CARTOUCHE = 5,   // 象形文字框
    GLYPHSPLIT = 6,  // 字形分割
    UNKNOWN = 7      // 未知
}

// FontMapping / LocalizationFontSetMapping（字体映射）
// RTLTMPro（从右到左文字支持）
// MixedTextBuilder（混合文字构建器，支持虚构文字+真实文字混排）
// LanguageLine（多语言文本行）
// IntroText（序章文字）
// Annotation / AnnotationManager（注释/标记系统）
```

---

## 九、与 Project Ark 的对比与借鉴

### 可直接借鉴

| 借鉴点 | TUNIC 实现 | Project Ark 应用方向 |
|--------|-----------|---------------------|
| **StateVariable 系统** | ScriptableObject + 观察者模式，全局状态管理 | 关卡状态/门锁/事件触发的统一管理 |
| **ConduitNode 电路系统** | 图论连通性检查，ScriptableObject 节点 | 关卡谜题的能量/信号传导系统 |
| **AreaLabel 区域提示** | ScriptableObject 数据 + 多语言支持 | 进入新区域时的区域名称提示 |
| **FMOD 音频架构** | 每个敌人独立 bank，场景独立 bank | 音频系统的模块化组织方式 |
| **AmplifyColor 区域色彩** | Volume 触发器切换 LUT | 不同星域的视觉主题切换 |
| **BloodstainManager** | 死亡位置记录 + 回收奖励 | 类魂风格的死亡记录系统 |
| **TrinketEffect 饰品系统** | 16 种效果枚举，TrinketSlot 管理 | 星图部件的被动效果系统 |
| **PlatformSaveInterface** | 接口抽象，支持多平台存档 | 跨平台存档系统设计 |
| **YouAreHereMapper** | 场景中放置标记，手册 UI 读取位置 | 游戏内地图的"你在这里"功能 |
| **StatefulActive** | StateVariable 驱动 GameObject 显隐 | 关卡状态驱动的环境变化 |

### 差异点

| 方面 | TUNIC | Project Ark |
|------|-------|-------------|
| 渲染管线 | Unity 2020 Built-in | Unity 6 URP 2D |
| 输入系统 | InControl | New Input System |
| 音频 | FMOD Studio | FMOD Studio（相同！） |
| 存档 | 文件系统 + PlayerPrefs | SaveManager（自定义） |
| 场景管理 | 独立场景切换 | 单场景 + Additive 加载 |
| 视角 | 等距 3D | Top-Down 2D |
| 进度系统 | StateVariable SO | WorldProgressManager |

---

## 十、文件路径索引

```
F:\SteamLibrary\steamapps\common\TUNIC\
├── Tunic.exe
├── GameAssembly.dll（21 MB，IL2CPP native code）
├── UnityPlayer.dll（Unity 2020.3.17）
└── Tunic_Data\
    ├── data.unity3d（882 MB，所有 Unity 资产）
    ├── GI\（72 个场景的光照贴图）
    ├── il2cpp_data\
    │   └── Metadata\global-metadata.dat（4.8 MB）
    ├── StreamingAssets\（137 个 FMOD .bank 文件）
    │   ├── music_main.bank（361 MB）
    │   ├── sfx_*.bank（场景/全局音效）
    │   └── en_*.bank（每个敌人独立音效）
    └── ScriptingAssemblies.json（程序集列表）

D:\Tools\TUNIC_dump\（Il2CppDumper 输出）
├── dump.cs（类/字段/方法签名）
├── DummyDll\Assembly-CSharp.dll（可用 Mono.Cecil 分析）
├── stringliteral.json（9,829 个字符串字面量）
├── script.json（方法地址映射）
└── il2cpp.h（C 结构体头文件）
```

---

*文档创建时间：2026-03-08*  
*分析方法：Il2CppDumper（dump.cs + DummyDll）+ Mono.Cecil 程序化分析 + FMOD bank 文件名推断 + stringliteral.json 字符串提取*  
*注意：TUNIC 为 IL2CPP 构建，无法直接读取方法逻辑体，所有行为推断基于类名/字段名/枚举值*
