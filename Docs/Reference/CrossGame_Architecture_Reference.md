# Project Ark — 跨游戏借鉴技术方案参考手册

> **版本**：v2.0  
> **创建时间**：2026-03-08  
> **最后更新**：2026-03-08  
> **数据来源**：TUNIC / Hollow Knight Silksong / Minishoot' Adventures / Rain World / Galactic Glitch / BackpackBattles + BackpackMonsters / Magicraft 代码解构  
> **适用范围**：Progression 系统 · Exploration 系统 · 地图架构 · 关卡世界设计 · 战斗手感 · 武器弹幕 · 性能架构 · 存档设计 · 架构风险  
> **阅读约定**：每条方案均标注"问题 → 参考来源 → Ark 适配方案 → 工期"，直接对应 Ark 已有模块  
> **v2.0 新增**：Section 5（战斗手感）· Section 6（武器弹幕）· Section 7（性能循环）· Section 8（存档架构）· Section 9（架构风险红队）

---

## Section 0 — 文档说明

### 这份文档是什么

这是一份**可落地的设计决策手册**，不是游戏评测。每个方案的出发点都是 Project Ark 现阶段的已知缺口，并给出经过跨游戏验证的解决思路和接口草稿。

### 如何使用

- 开始一个新的 Feature 前，先查 Section 4 的优先级矩阵，确认是否已有参考方案
- 每个方案的伪代码是**设计规格**，不是最终代码，实现时按 Ark 代码规范调整
- 带 `[MVP]` 标注的方案可以在 1 天内做出最小可玩版本
- 带 `[FUTURE]` 标注的方案是中长期方向，现在不做但记在这里

### 当前阶段定位

Ark 已完成：战斗循环 + 星图 + 敌人 AI + 关卡模块（Phase 1-6）  
当前阶段：场景配置与验证 → **下一阶段重点：Progression 闭环 + Exploration 反馈**

### 版本变更记录

| 版本 | 日期 | 变更内容 |
|------|------|---------|
| v1.0 | 2026-03-08 | 初始版本。TUNIC / Silksong / Minishoot / RainWorld。Section 1-3 + 优先级矩阵 |
| v2.0 | 2026-03-08 | 新增 GalacticGlitch / BackpackBattles / Magicraft。新增 Section 5-9（战斗手感/武器弹幕/性能/存档/架构风险） |

---

## Section 1 — Progression 系统方案

### 1.1 进度标志位架构

**问题：** Ark 的 `WorldProgressManager` 存在，但"从进度标志改变到场景物件响应"的信号链完整性未经验证。玩家击败 Boss 后，世界应如何"记住"这件事并给出可见的反馈？

**参考来源：**
- Minishoot `WorldState` 静态类（字符串键 → 布尔值 + 事件广播）
- TUNIC `StateVariable : ScriptableObject`（SO 资产 + 订阅者列表）
- Silksong `PlayerData.GetBool(fieldName)` / `SetBool(fieldName, value)`（反射式存取）

**Ark 适配方案：**

推荐采用"**Minishoot 的轻量存储 + TUNIC 的 SO 驱动响应**"组合，分两层：

```
层 1 — 存储层（已有基础，强化接口）：
  SaveManager.SetFlag(string key, bool value)  // 写入并广播
  SaveManager.GetFlag(string key) : bool       // 读取

层 2 — 响应层（新增）：
  ProgressFlag : ScriptableObject              // 每个进度事件一个 SO 资产
  FlagActivatedResponder : MonoBehaviour        // 挂在场景物件上，零代码响应
```

`ProgressFlag` ScriptableObject 草案：

```csharp
// Assets/_Data/Level/Flags/boss_moss_mother_defeated.asset
[CreateAssetMenu(menuName = "ProjectArk/Level/ProgressFlag")]
public class ProgressFlag : ScriptableObject
{
    [field: SerializeField] public string Key { get; private set; }
    
    private readonly List<Action<bool>> _subscribers = new();
    
    public void Subscribe(Action<bool> callback) => _subscribers.Add(callback);
    public void Unsubscribe(Action<bool> callback) => _subscribers.Remove(callback);
    
    public void Raise(bool value)
    {
        foreach (var sub in _subscribers) sub.Invoke(value);
    }
}
```

`FlagActivatedResponder` MonoBehaviour 草案（零代码配置）：

```csharp
public class FlagActivatedResponder : MonoBehaviour
{
    [SerializeField] private ProgressFlag _flag;
    [SerializeField] private bool _activeWhenTrue = true;
    [SerializeField] private UnityEvent _onActivated;
    [SerializeField] private UnityEvent _onDeactivated;

    private void OnEnable()
    {
        _flag.Subscribe(OnFlagChanged);
        // 初始化时同步当前状态
        OnFlagChanged(ServiceLocator.Get<SaveManager>().GetFlag(_flag.Key));
    }

    private void OnDisable() => _flag.Unsubscribe(OnFlagChanged);

    private void OnFlagChanged(bool value)
    {
        if (value == _activeWhenTrue) _onActivated.Invoke();
        else _onDeactivated.Invoke();
    }
}
```

**存档键命名规范：**（字符串即文档，策划可读）

```
boss_{name}_encountered    // 遭遇过（见 1.3）
boss_{name}_defeated       // 剧情击败
boss_{name}_killed         // 血量归零
area_{zone}_unlocked       // 区域解锁
gate_{id}_opened           // 特定门已打开
item_{id}_collected        // 特定物品已收集
checkpoint_{id}_activated  // 存档点已激活
```

**工期估算：** `ProgressFlag` SO + `FlagActivatedResponder` 约 1 天；与现有 `SaveManager` 集成约半天。

---

### 1.2 技能解锁规范（Ability Registry）

**问题：** Ark 飞船解锁新能力（如双射、穿透、减速炸弹）目前是否有统一的解锁状态管理？还是分散在各系统自己的布尔字段里？

**参考来源：**
- Silksong `PlayerData` 的 `has*` 布尔字段体系（20+ 技能字段）
- Silksong `HasSeen*` 字段（首次教学提示控制）
- Minishoot `enum Skill` / `enum Modules` 二分法（永久解锁 vs 装备生效）

**Silksong 的完整模式（值得完全照搬）：**

```
hasDash            // 解锁了冲刺
HasSeenDash        // 教学提示已显示过（避免重复弹出）
```

每个新能力 = 两个字段，一个控制功能开关，一个控制新手引导。

**Ark 适配方案：**

将 Minishoot 的"Skill（永久解锁）vs Module（装备生效）"映射到 Ark 的体系：

| Minishoot 类型 | Ark 对应 | 说明 |
|---------------|---------|------|
| `enum Skill` 解锁型 | 飞船能力（冲刺、护盾、减速场） | 学会了就永久拥有，不占星图槽 |
| `enum Modules` 装备型 | 星图部件（Core/Prism/Sail/Satellite） | 装上才生效，已有完整系统 |

在 `PlayerSaveData` 中添加技能注册表字段：

```csharp
public class PlayerSaveData
{
    // ... 现有字段 ...
    
    // 飞船永久能力解锁（字符串键 → 布尔，易于扩展）
    public Dictionary<string, bool> UnlockedAbilities = new();
    public Dictionary<string, bool> AbilityTutorialShown = new();  // HasSeen*
    
    public bool HasAbility(string abilityKey) =>
        UnlockedAbilities.TryGetValue(abilityKey, out var v) && v;
    
    public bool HasSeenAbilityTutorial(string abilityKey) =>
        AbilityTutorialShown.TryGetValue(abilityKey, out var v) && v;
}
```

能力键常量集中管理：

```csharp
public static class AbilityKeys
{
    public const string Dash           = "ability_dash";
    public const string ShieldBubble   = "ability_shield_bubble";
    public const string SlowField     = "ability_slow_field";
    public const string DoubleShot    = "ability_double_shot";
    // ... 新增只需加一行 ...
}
```

**工期估算：** `PlayerSaveData` 扩展约半天；接入各系统的 `HasAbility()` 检查约 1 天。

---

### 1.3 Boss 三阶段状态追踪

**问题：** Ark 存档层对 Boss 的记录是否区分了"遭遇过"和"击败了"？这个区分影响：NPC 对话变化、日志条目解锁时机、成就触发点。

**参考来源：**
- Silksong `PlayerData` 的三段式字段命名模式：
  - `encounteredMossMother` → 玩家进入 Boss 房间触发
  - `defeatedMossMother` → Boss 剧情失败过场播完后触发
  - `defeatedMossMotherAfterRedMemory` → 特殊条件二次击败（说明同一 Boss 可以有多个"击败"时间点）

**为什么分三个（设计意图）：**

```
遭遇(encountered)：进 Boss 房间 → NPC 可以说"你去见过它了吗？"
击败(defeated)：剧情层面的失败 → 触发后续剧情、开门、NPC 变对话
死亡(killed)：血量归零 → 成就、日志词条、战斗统计
```

有时 `defeated` ≠ `killed`：比如 Boss 在 40% 血量时触发逃跑过场，`defeated=true` 但 `killed=false`（本次没打死）。这在叙事驱动的银河城中极常见。

**Ark 适配方案：**

在 `PlayerSaveData` 中添加 Boss 状态结构：

```csharp
[Serializable]
public struct BossRecord
{
    public bool Encountered;  // 进入 Boss 房间
    public bool Defeated;     // 剧情层面击败（过场完成）
    public bool Killed;       // 血量归零
    public int  KillCount;    // 击杀次数（挑战模式用）
}

public class PlayerSaveData
{
    // ...
    public Dictionary<string, BossRecord> BossRecords = new();
    
    public BossRecord GetBossRecord(string bossId) =>
        BossRecords.TryGetValue(bossId, out var r) ? r : default;
}
```

`EnemyEntity` 或 Boss 专用脚本在对应时机调用：

```csharp
// 玩家进入 Boss 房间时
SaveManager.SetBossEncountered("star_piercer");

// Boss 触发死亡过场（血量阈值或条件）时
SaveManager.SetBossDefeated("star_piercer");

// 血量真正归零时
SaveManager.SetBossKilled("star_piercer");
```

**工期估算：** 数据结构约半天；接入 Boss 触发点约 1 天。

---

### 1.4 StateVariable SO 驱动环境响应

**问题：** 当进度状态改变时（Boss 死亡、区域解锁、物品拾取），场景内的门/障碍/光源/NPC 应该自动响应，但目前这些响应是否需要手写 C# 订阅代码？

**参考来源：**
- TUNIC `StateVariable : ScriptableObject`（内置观察者模式）
- TUNIC `StatefulActive : MonoBehaviour`（Inspector 中指定 SO，零代码响应显隐）
- TUNIC `StatefulMaterialSwap`（同一 SO 驱动材质切换）
- TUNIC `StateVarTrigger`（玩家进入触发器时设置 StateVariable）

**TUNIC 的核心洞察：**

> 不是"Boss 死亡时手动调用 door.Open()"，而是"Boss 死亡时设置一个 SO 的值，所有订阅了这个 SO 的物件自动响应"。

这实现了进度系统和场景系统的**完全解耦**。

**Ark 适配方案（基于 1.1 的 ProgressFlag）：**

在 1.1 方案的基础上，可以为场景内物件添加更多即插即用组件：

```csharp
// 控制 GameObject 显示/隐藏（最常用）
// Inspector: _flag = boss_star_piercer_defeated, _activeWhenTrue = true
public class FlagActivatedResponder : MonoBehaviour { ... }  // 见 1.1

// 控制 Animator Parameter（开门动画）
public class FlagAnimatorTrigger : MonoBehaviour
{
    [SerializeField] private ProgressFlag _flag;
    [SerializeField] private string _triggerName;
    [SerializeField] private Animator _animator;
    
    private void OnEnable() { _flag.Subscribe(OnFlag); }
    private void OnDisable() { _flag.Unsubscribe(OnFlag); }
    private void OnFlag(bool v) { if (v) _animator.SetTrigger(_triggerName); }
}

// 控制 TilemapVariantSwitcher（切换地形变体）
public class FlagTilemapVariant : MonoBehaviour
{
    [SerializeField] private ProgressFlag _flag;
    [SerializeField] private TilemapVariantSwitcher _switcher;
    [SerializeField] private int _variantIndexWhenTrue;
    
    private void OnEnable() { _flag.Subscribe(v => _switcher.SwitchTo(v ? _variantIndexWhenTrue : 0)); }
    private void OnDisable() { _flag.Unsubscribe(...); }
}
```

这些组件通过 `[SerializeField]` 暴露给 Inspector，策划不需要写代码就可以配置复杂的进度响应链。

**与现有 CombatEvents 总线的关系：**

```
CombatEvents.OnBossDefeated 事件
    → BossEntity 广播 OnBossDefeated(bossId)
    → WorldProgressManager.HandleBossDefeated(bossId)
        → SaveManager.SetFlag($"boss_{bossId}_defeated", true)
            → ProgressFlag.Raise(true)                    ← 新增
                → FlagActivatedResponder（场景内所有订阅者）  ← 新增
```

**工期估算：** 基于 1.1 的 ProgressFlag 已有基础，再加 3 种 Responder 组件约 1 天。

---

## Section 2 — Exploration 系统方案

### 2.1 区域地图碎片 + ZoneId 架构

**问题：** Ark 的 `MinimapManager` 有房间发现机制，但"区域级别"的地图解锁概念（玩家需要获取某个区域的地图才能在全图上看到它）是否存在？这个机制本身就是一个探索驱动力。

**参考来源：**
- Silksong `MapZone` 枚举（42 个值）+ `Has*Map` 布尔字段（28 个区域地图碎片）
- Silksong 制图师 NPC 在各区域出售地图片段（`mapperSellingTubePins` 等字段）
- Minishoot `BiomeManager` 区域主题系统

**Silksong 的设计逻辑：**

```
地图系统三层：
1. MapZone 枚举 = 区域 ID（所有区域在代码中有确定的标识）
2. Has*Map 布尔 = 地图碎片（玩家是否持有这个区域的地图）
3. 房间发现 = 在已有地图碎片的基础上，玩家走过的房间才会显示
```

地图碎片由 NPC 出售，这直接将探索、货币系统和 NPC 互动三者关联起来。

**Ark 适配方案：**

定义 `StarZone` 枚举（对标 Silksong `MapZone`）：

```csharp
// GlobalEnums.cs 或单独文件
public enum StarZone
{
    None = 0,
    Sheba,          // 示巴星（第一关）
    CrystalNebula,  // 水晶星云
    BoneGraveyard,  // 骨墓场
    SilkVoid,       // 丝绸虚空
    // ... 随关卡设计扩展
}
```

在 `PlayerSaveData` 中添加区域地图状态：

```csharp
public class PlayerSaveData
{
    // 区域地图碎片（Has*Map 等价）
    public HashSet<StarZone> UnlockedZoneMaps = new();
    
    // 房间发现记录（已有类似机制，可扩展）
    public HashSet<string> DiscoveredRooms = new();
    
    public bool HasMapForZone(StarZone zone) => UnlockedZoneMaps.Contains(zone);
}
```

在 `MinimapManager` 中新增区域级显示逻辑：

```csharp
// 小地图只显示：
// 1. 玩家当前所在区域（HasMapForZone == true）的已发现房间
// 2. 或者：玩家可见范围内的房间轮廓（无地图时降级显示）
```

**工期估算：** 枚举定义 + `PlayerSaveData` 扩展约半天；`MinimapManager` 接入约 1 天。

---

### 2.2 PersistentBoolItem — 场景物件状态持久化

**问题：** 场景内的个别物件状态（已炸开的墙壁、已拾取的星图碎片、已激活的能量节点）在玩家离开场景后是否能持久保存？目前 Ark 的 Level 模块有哪些机制处理这个问题？

**参考来源：**
- Silksong `PersistentBoolItem : MonoBehaviour`（唯一 ID + 场景名 + 布尔值）
- Silksong `SceneData`（每个场景的 `PersistentBoolCollection`）
- Silksong `SceneData.SaveMyState()` → `GameManager.SaveLevelState()`

**Silksong 的方案精髓：**

```csharp
class PersistentBoolItem : MonoBehaviour {
    PersistentBoolData itemData;
    // itemData = { id: "sheba_01_crystal_left", sceneName: "Sheba_01", value: bool }
    // id 在整个项目中唯一
    
    bool disableIfActivated;  // 激活后自动禁用自身（用于收集物）
}
```

每个可被"永久改变"的场景物件：挂上组件 + 填写唯一 ID，其他都是自动的。

**Ark 适配方案：**

```csharp
[Serializable]
public struct PersistentBoolData
{
    public string Id;         // 唯一标识，格式: "{scene}_{type}_{desc}"
    public string SceneName;  // 所在场景名
    public bool   Value;      // 当前布尔值
}

public class PersistentBoolItem : MonoBehaviour
{
    [SerializeField] private PersistentBoolData _data;
    [SerializeField] private bool _disableWhenActivated = false;

    private void Start()
    {
        // 读取持久化状态
        _data.Value = ServiceLocator.Get<SaveManager>()
            .GetPersistentBool(_data.SceneName, _data.Id);
        
        if (_data.Value && _disableWhenActivated)
            gameObject.SetActive(false);
    }

    public void SetActivated(bool value)
    {
        _data.Value = value;
        ServiceLocator.Get<SaveManager>()
            .SetPersistentBool(_data.SceneName, _data.Id, value);
        
        if (value && _disableWhenActivated)
            gameObject.SetActive(false);
    }
}
```

`SaveManager` 需要新增 Persistent Bool 的存储结构：

```csharp
// PlayerSaveData 中
public Dictionary<string, Dictionary<string, bool>> ScenePersistentBools = new();
// key1 = sceneName, key2 = itemId, value = bool
```

**唯一 ID 命名规范：**

```
{sceneName}_{type}_{description}
sheba_01_wall_left_upper        // 可破坏墙
sheba_01_crystal_node_center    // 能量节点
sheba_01_starchart_shard_a      // 星图碎片
```

**工期估算：** `PersistentBoolItem` 组件 + `SaveManager` 扩展约 1.5 天。

---

### 2.3 TransitionPoint 过渡点人体工程学细节

**问题：** Ark 的 `Door` 系统是否有足够的细节参数控制玩家进入新房间时的体验？银河城的门不只是触发场景加载，还要控制玩家的进入方向、动画延迟等。

**参考来源：**
- Silksong `TransitionPoint`（40 个字段）的部分关键参数：
  ```csharp
  bool isInactive          // 门是否激活（条件性入口）
  bool isADoor             // 是否播放开门动画
  bool dontWalkOutOfDoor   // 进入后不播放走出动画
  bool alwaysEnterRight    // 总是从右侧进入（用于特定叙事时刻）
  bool alwaysEnterLeft     // 总是从左侧进入
  float entryDelay         // 进入延迟（过场前摇）
  ```

**Ark 适配方案：**

在 Ark 的 `Door` ScriptableObject 或 MonoBehaviour 中，建议补充以下参数：

```csharp
public class Door : MonoBehaviour
{
    // ... 现有字段 ...
    
    [Header("Entry Behavior")]
    [SerializeField] private float _entryDelay = 0f;       // 进入前等待时间（用于过场）
    [SerializeField] private bool _lockEntryDirection = false;
    [SerializeField] private Vector2 _forcedEntryDirection = Vector2.right;
    
    [Header("Condition")]
    [SerializeField] private ProgressFlag _requiredFlag;   // 需要此标志才能通过
    [SerializeField] private bool _isHidden = false;       // 是否隐形（秘密通道）
}
```

`entryDelay` 的典型应用：玩家到达门前 → 等待 0.5s → 镜头开始推进 → 场景加载。这个半秒延迟给了视觉反馈时间，让过渡感觉不突兀。

**工期估算：** 对现有 `Door` 类的字段补充约半天。

---

### 2.4 地图 Pin 类型系统

**问题：** Ark 的 `MinimapManager` 是否支持在地图上显示不同类型的兴趣点图标（POI）？比如：已发现的存档点、商人位置、未解锁的区域等。

**参考来源：**
- Silksong 的 `hasPinXxx` 字段体系（8 种 Pin 类型）：
  ```
  hasPinBench    — 存档点（Bell Bench）
  hasPinCocoon   — 复活茧（检查点）
  hasPinShop     — 商店
  hasPinSpa      — 温泉（补血点）
  hasPinStag     — 驿马站（快速旅行前置）
  hasPinTube     — 管道（快速旅行）
  hasPinFlea*    — 跳蚤（各区域特殊交通）
  hasMarker_a/b/c/d/e — 玩家自定义标记（5色）
  ```
- Silksong `MapPin : MonoBehaviour`（`SAVE_KEY` 字段控制是否显示）

**Ark 适配方案：**

定义 `MapPinType` 枚举并在 `MinimapManager` 中支持：

```csharp
public enum MapPinType
{
    Checkpoint,      // 存档/复活点（已激活才显示）
    Shop,            // 商人
    FastTravel,      // 快速旅行节点（解锁后才显示）
    BossRoom,        // Boss 房间（遭遇过才显示）
    SecretCache,     // 隐藏缓存（发现后显示）
    PlayerMarker,    // 玩家自定义标记
}

// 每个 Pin 的显示条件
public class MapPinData
{
    public MapPinType Type;
    public Vector2 WorldPosition;
    public string SaveKey;          // PlayerSaveData 中的标志键
    public Sprite Icon;
}
```

玩家自定义标记（`hasMarker_a/b/c/d/e` 的等价）：支持玩家在地图上放置 5 个颜色标记，存入 `PlayerSaveData.PlayerMarkers`。这是探索游戏中极受欢迎的 QoL 功能。

**工期估算：** Pin 枚举 + `MinimapManager` 扩展约 1.5 天；玩家自定义标记约 1 天（可选）。

---

### 2.5 AreaLabel — 区域进入提示

**问题：** 玩家进入新的星域区域时，屏幕上是否有"进入 XXX 星域"的文字提示？这是银河城/类魂游戏传递位置感知的标准手段，实现极简但沉浸感提升显著。

**参考来源：**
- TUNIC `AreaData : ScriptableObject`（`topLine` + `bottomLine` 多语言）
- TUNIC `AreaLabel : MonoBehaviour`（静态方法 `ShowLabel(area)` / `CancelShowLabel()`）
- TUNIC `AreaLabelOnLoad`（场景加载时自动触发）
- TUNIC `AreaLabelZone`（进入特定触发器时触发）
- FMOD 事件 `event:/main/ui/gameplay/area_title`（区域标题显示音效）

**[MVP] Ark 最简实现（1天内可完成）：**

```csharp
// Assets/_Data/Level/AreaData/sheba_star.asset
[CreateAssetMenu(menuName = "ProjectArk/Level/AreaData")]
public class AreaData : ScriptableObject
{
    [SerializeField] private string _primaryName;    // "示巴星"
    [SerializeField] private string _subTitle;       // "THE SHEBA SYSTEM"
}
```

```csharp
// UI 层，挂在常驻 Canvas 上
public class AreaLabelHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text _primaryText;
    [SerializeField] private TMP_Text _subTitleText;
    [SerializeField] private CanvasGroup _canvasGroup;
    
    public static AreaLabelHUD Instance { get; private set; }
    
    public void ShowLabel(AreaData area)
    {
        _primaryText.text = area.PrimaryName;
        _subTitleText.text = area.SubTitle;
        // PrimeTween 淡入淡出
        Tween.Alpha(_canvasGroup, 1f, 0.5f)
            .OnComplete(() => Tween.Delay(2f)
                .OnComplete(() => Tween.Alpha(_canvasGroup, 0f, 0.8f)));
    }
}
```

```csharp
// 放在每个 Room 或 Zone 边界触发器上
public class AreaLabelTrigger : MonoBehaviour
{
    [SerializeField] private AreaData _areaData;
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            AreaLabelHUD.Instance.ShowLabel(_areaData);
    }
}
```

**工期估算：** `AreaData` SO + `AreaLabelHUD` + `AreaLabelTrigger` 约 1 天（含 PrimeTween 动画）。

---

## Section 3 — 关卡/世界架构方案

### 3.1 BiomeZone 空间视觉切换

**问题：** Ark 已有 `WorldPhase`（时间维度：黎明/日落/深夜）控制全局视觉主题。但同一时间段内，不同星域区域应该有不同的视觉风格——这是空间维度的切换，两者可以并存并叠加。

**参考来源：**
- Minishoot `BiomeManager` + `BiomeTrigger`（进入触发器→调用 `SetBiome(BiomeType)`）
- Minishoot `BiomeType`：Cave / Forest / Desert / Snow / Dungeon
- TUNIC `AmplifyColorVolume2D`（基于区域的色彩 LUT 切换）
- TUNIC `StatefulCameraZone`（状态驱动的摄像机区域）

**Ark 适配方案：**

定义 `StarZoneBiome` ScriptableObject（空间视觉配置）：

```csharp
[CreateAssetMenu(menuName = "ProjectArk/Level/StarZoneBiome")]
public class StarZoneBiome : ScriptableObject
{
    [Header("Tilemap")]
    [SerializeField] private TileBase[] _wallTiles;
    [SerializeField] private TileBase[] _groundTiles;
    [SerializeField] private Color _ambientColor;
    
    [Header("Post Processing")]
    [SerializeField] private VolumeProfile _urpVolumeProfile;
    [SerializeField] private float _blendDuration = 0.5f;
    
    [Header("Ambience")]
    [SerializeField] private AudioClip _ambienceLoop;
    [SerializeField] private Color _particleColorTint;
}
```

与现有 `AmbienceController` 和 `WorldPhaseSO` 的关系：

```
WorldPhaseSO（时间维度）           StarZoneBiome（空间维度）
      ↓                                   ↓
  全局色调/光照                      区域特定贴图/后处理
  黎明=暖色, 深夜=冷蓝            示巴星=有机质感, 骨墓=枯骨灰色
      ↓                                   ↓
      └──────── 叠加计算 ──────────────────┘
                     ↓
              最终视觉效果
```

`BiomeTrigger` 组件（放在区域边界）：

```csharp
public class BiomeTrigger : MonoBehaviour
{
    [SerializeField] private StarZoneBiome _biome;
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            BiomeManager.Instance.TransitionTo(_biome);
    }
}
```

**工期估算：** `StarZoneBiome` SO + `BiomeManager` + `BiomeTrigger` 约 2 天；与现有 `AmbienceController` 集成约 1 天。

---

### 3.2 ConduitNode — 关卡谜题信号传导系统

**[FUTURE]**

**问题：** 银河城的深度来自于"解锁新区域的条件不只是找到开关"——玩家需要理解世界的结构。TUNIC 用"电路连通性"来实现这种深度。

**参考来源：**
- TUNIC `ConduitNode_SO : ScriptableObject`（图节点）：
  ```csharp
  List<ConduitNode_SO> connectedNodes;    // 相邻节点
  bool IsPoweredSource;                  // 是否为电源
  bool Powered;                          // 是否通电（递归检查）
  
  static bool checkConnectedToPower(ConduitNode_SO cn); // BFS/DFS
  ```
- TUNIC `ConduitPlatform`（通电后激活的平台）
- TUNIC `ConduitTeleporter`（通电后激活的传送点）

**对 Ark 的设计翻译：**

不用"电路"，而是"异星能量网络"——破坏某个能量中继站后，与它连通的防御屏障全部失效：

```
能量中继A ── 连接 ── 能量中继B ── 连接 ── 防御屏障
   ↑
玩家摧毁这个
   ↓
A从网络中断开 → B失去电源连接 → 防御屏障断电消失
```

核心代码（基于 SO 的图论）：

```csharp
[CreateAssetMenu(menuName = "ProjectArk/Level/EnergyNode")]
public class EnergyNodeSO : ScriptableObject
{
    [SerializeField] private ProgressFlag _isDestroyedFlag;
    [SerializeField] private List<EnergyNodeSO> _connectedNodes;

    public bool IsPowered =>
        !IsDestroyed && HasConnectionToPowerSource(this, new HashSet<EnergyNodeSO>());
    
    private bool IsDestroyed =>
        ServiceLocator.Get<SaveManager>().GetFlag(_isDestroyedFlag.Key);

    private static bool HasConnectionToPowerSource(EnergyNodeSO node, HashSet<EnergyNodeSO> visited)
    {
        if (!visited.Add(node)) return false;
        if (node.IsDestroyed) return false;
        if (node.IsPowerSource) return true;
        return node._connectedNodes.Any(n => HasConnectionToPowerSource(n, visited));
    }
}
```

`EnergyBarrier` 组件订阅相关节点的 `ProgressFlag`，当任意关联节点被摧毁时自动检查连通性。

**工期估算（完整实现）：** 约 5 天（含编辑器工具和可视化）。**建议作为中期目标，在第一个完整区域完成后再做。**

---

### 3.3 Quest 叙事发现分离（QuestRumour）

**[FUTURE]**

**问题：** 当未来 Ark 有 NPC 对话系统时，玩家"如何发现某个任务"会影响对话内容。如果玩家是亲身遭遇后才被 NPC 提醒，NPC 应该说不同的话。

**参考来源：**
- Silksong `QuestCompletionData`（任务完成状态）
- Silksong `QuestRumourData`（任务**发现方式**：从 NPC 口中听说、还是亲身遭遇）

**Ark 的设计意图：**

任务发现方式影响两件事：
1. NPC 对话变化（"你已经知道了？"vs"你去见过它了吗？"）
2. 日志条目的描述视角（亲历者视角 vs 传闻视角）

在 `PlayerSaveData` 中预留位置：

```csharp
public enum QuestDiscoverySource
{
    Unknown,
    DirectEncounter,    // 亲身遭遇（先遭遇后被告知）
    NPCRumour,          // 从 NPC 口中听说（先告知后遭遇）
    EnvironmentClue,    // 从环境线索发现
}

public class PlayerSaveData
{
    // 任务发现记录（当前可以是空 Dictionary，NPC 系统完成后再填入）
    public Dictionary<string, QuestDiscoverySource> QuestDiscoveries = new();
}
```

**工期估算：** 数据结构约半天（现在不做，NPC 系统时一起实现）。

---

### 3.4 世界层 AI — EnemyDirector 升级方向

**[FUTURE]**

**问题：** 银河城/开放世界中，敌人"应该记住自己死了"。玩家清扫了一个区域后，再次经过时不应该立刻刷新。Ark 的 `EnemyDirector` 已经做了场景级别的管理，但是否有跨场景的持久化概念？

**参考来源：**
- Rain World 的双层 AI 架构：
  ```
  AbstractCreatureAI    ← 生物不在当前场景时的轻量抽象（仅记录位置/状态）
  [Creature]AI          ← 生物在当前场景时的完整 AI（重型，按需加载）
  ```
- Rain World `FliesWorldAI` / `ScavengersWorldAI`（全局跨场景行为调度）

**Ark 的升级路径：**

`EnemyDirector` 当前职责：场景内的敌人生成/追踪/波次。  
建议增加"世界层记录"能力：

```csharp
public class EnemyDirector : MonoBehaviour
{
    // 现有功能...
    
    // 新增：跨场景的敌人死亡记录
    // 格式: {sceneId}_{enemySpawnerId} → bool (is permanently killed)
    private Dictionary<string, bool> _permanentKills;
    
    // 精英/命名敌人死亡后永久记录
    public void RecordPermanentKill(string sceneId, string spawnerId)
    {
        _permanentKills[$"{sceneId}_{spawnerId}"] = true;
        ServiceLocator.Get<SaveManager>().SetFlag($"enemy_dead_{sceneId}_{spawnerId}", true);
    }
    
    // 场景加载时查询，决定是否生成某个敌人
    public bool IsPermanentlyKilled(string sceneId, string spawnerId) =>
        _permanentKills.TryGetValue($"{sceneId}_{spawnerId}", out var v) && v;
}
```

这个设计使得：命名精英敌人、守门 Boss 小兵永久消失；普通敌人仍可刷新（通过 `EnemySpawner` 控制）。

**工期估算（完整实现）：** 约 3 天（依赖 `PersistentBoolItem` 基础设施完成后做）。

---

## Section 4 — 优先级矩阵

> 紧迫性基于"与当前开发阶段（场景配置验证 + Progression 闭环）的关联度"
> v2.0 新增：战斗手感 / 武器弹幕 / 性能 / 存档 / 风险修复条目

### 4.1 立即处理（1-3天，当前阶段阻塞项）

| 优先级 | 方案 | 参考来源 | 工期 | 与现有系统 | 备注 |
|--------|------|---------|------|-----------|------|
| 🔴 立即 | **[风险修复] UniTask CancellationToken 泄漏检查** (9.3) | — | 1-2h 检查 + 1d 修复 | 所有 async 代码 | 防止对象池回收后任务继续执行 |
| 🔴 立即 | **[风险修复] ServiceLocator Awake 依赖检查** (9.4) | Minishoot | 30min 检查 + 2-4h 修复 | 所有 Manager | 运行时 NullRef 的常见来源 |
| 🔴 立即 | **[风险修复] WeavingState UI inactive 状态检查** (9.6) | — | 30min | 所有 UI Panel | 当前场景配置阶段极易踩坑 |
| 🔴 立即 | **AreaLabel 区域进入提示** (2.5) | TUNIC | 1 天 | 新增，接现有 Room | 零风险，沉浸感提升显著 |
| 🔴 立即 | **has* 技能字段规范** (1.2) | Silksong | 半天 | 扩展 PlayerSaveData | 补全能力系统数据基础 |
| 🔴 立即 | **WorldState 标志位架构** (1.1) | Minishoot + TUNIC | 1.5 天 | 强化 SaveManager | Progression 闭环的核心 |
| 🔴 立即 | **CameraShakeManager** (5.4) | GalacticGlitch | 半天 | 新增，接战斗事件 | 手感基础设施，接入成本极低 |
| 🔴 立即 | **HitFlashEffect 独立组件** (5.2) | Minishoot | 半天 | 新增，挂在可受击对象 | 独立组件，零耦合 |

### 4.2 短期（3-7天）

| 优先级 | 方案 | 参考来源 | 工期 | 与现有系统 | 备注 |
|--------|------|---------|------|-----------|------|
| 🟡 短期 | **[风险修复] SO 运行时污染检查** (9.1) | — | 1-2h 检查 | StarChart SO 系统 | Play Mode 前后 SO 字段对比 |
| 🟡 短期 | **[风险修复] CombatEvents 僵尸订阅检查** (9.2) | GG（对比） | 1d 检查+修复 | CombatEvents + 对象池 | 检查所有可被回收的订阅者 |
| 🟡 短期 | **TimeScaleManager HitStop 统一** (5.1) | Magicraft | 1 天 | 包装 HitStopEffect | 防止多系统 timeScale 冲突 |
| 🟡 短期 | **Dash 惯性衰减** (5.3) | GalacticGlitch | 半天 | ShipMotor 扩展 | 手感核心，参数可快速调 |
| 🟡 短期 | **SaveManager 自动增量存档** (8.1) | Minishoot | 半天 | SaveManager 扩展 | 防崩溃丢进度兜底 |
| 🟡 短期 | **PersistentBoolItem** (2.2) | Silksong | 1.5 天 | 扩展 SaveManager + Level | 银河城基础设施必须项 |
| 🟡 短期 | **Boss 三阶段状态** (1.3) | Silksong | 1 天 | 扩展 PlayerSaveData | 叙事反馈基础 |
| 🟡 短期 | **MapPin 类型系统** (2.4) | Silksong | 1.5 天 | 扩展 MinimapManager | 探索反馈基础 |
| 🟡 短期 | **StateVariable SO 响应组件** (1.4) | TUNIC | 1 天 | 基于 1.1 的 ProgressFlag | 环境响应解耦 |
| 🟡 短期 | **IPausable 接口 + GamePauseManager** (7.1) | Minishoot | 1 天 | 接入 EnemyBrain + Projectile | 编织态暂停保证 |
| 🟡 短期 | **存档数据分层（RunData vs PlayerSaveData）** (8.2) | BackpackBattles | 半天 | SaveManager 架构决策 | 尽早明确分层，越晚越贵 |

### 4.3 中期（1-2周）

| 优先级 | 方案 | 参考来源 | 工期 | 与现有系统 | 备注 |
|--------|------|---------|------|-----------|------|
| 🟠 中期 | **BulletBehaviourData 弹道数据化** (6.1) | Minishoot + GG | 2 天 | 扩展 Projectile + SO | 使弹道行为可配置，减少子类 |
| 🟠 中期 | **武器攻击时序字段** (6.2) | GalacticGlitch | 1.5 天 | StarCoreSO + WeaponTrack | 玩家武器 Signal-Window 规范 |
| 🟠 中期 | **CircleCast 高速子弹检测** (7.3) | Minishoot | 半天 | Projectile 扩展 | 解决高速子弹穿墙问题 |
| 🟠 中期 | **TransitionPoint 细节字段** (2.3) | Silksong | 半天 | 扩展 Door 系统 | 过渡体验打磨 |
| 🟠 中期 | **ZoneId + 地图碎片系统** (2.1) | Silksong | 1.5 天 | 扩展 MinimapManager | 探索驱动力设计 |
| 🟠 中期 | **BiomeZone 空间视觉切换** (3.1) | Minishoot | 3 天 | 搭配现有 WorldPhase | 视觉分层体验 |
| 🟠 中期 | **[风险修复] Assembly 依赖方向检查** (9.5) | — | 10min 检查 + 1-3d 修复 | 所有 .asmdef | `dotnet build` 即可验证 |

### 4.4 长期 / 未来

| 优先级 | 方案 | 参考来源 | 工期 | 前置依赖 | 备注 |
|--------|------|---------|------|---------|------|
| 🔵 长期 | **后置触发槽 Conditional Trigger** (6.3) | Magicraft | 1.5 天 | WeaponTrack 稳定后 | 战术深度大幅提升 |
| 🔵 长期 | **轨道共鸣系统** (6.4) | BackpackMonsters | 2 天 | 星图系统稳定后 | 横向组合的奖励层 |
| 🔵 长期 | **ConduitNode 能量网络谜题** (3.2) | TUNIC | 5 天 | ProgressFlag | 探索深度的核心 |
| 🔵 长期 | **EnemyDirector 世界层** (3.4) | Rain World | 3 天 | PersistentBoolItem | 命名精英永久记录 |
| 🔵 长期 | **BatchUpdate 批量更新** (7.2) | GalacticGlitch | 3 天 | 性能实际出现问题时 | 超过 300 对象时引入 |
| 🔵 长期 | **QuestRumour 叙事分离** (3.3) | Silksong | 半天数据结构 | NPC 系统前置 | 现在只做数据结构占位 |

---

## Section 5 — 验证用例：Progression 闭环

> 这是一个端到端测试用例，用来验证"进度状态 → 环境响应 → 存档重读后保持"的完整链路是否正常工作。

### 用例：击败 Boss → 门永久打开

**前置条件：**
- 场景中有一扇 `Door`，门上挂了 `FlagActivatedResponder`，指定 Flag = `boss_star_piercer_defeated`
- `EnemyEntity`（Boss）在死亡时触发 `CombatEvents.OnBossDefeated`
- `WorldProgressManager` 订阅此事件并调用 `SaveManager.SetFlag("boss_star_piercer_defeated", true)`

**测试步骤：**

```
1. 启动游戏，确认门处于关闭状态（Flag = false）
2. 击败 Boss "星穿者"（Star Piercer）
3. 确认门的开门动画触发（FlagActivatedResponder 接收到事件）
4. 不存档，直接退出游戏
5. 重新启动游戏（从最近存档点读档）
   → 如果门仍然是打开状态：持久化正确 ✅
   → 如果门恢复关闭：SaveManager 未在击败时存档 ❌
6. 存档后退出游戏，再次重启
   → 确认门仍然打开 ✅
7. 重新进入包含该门的场景（从另一个场景切换回来）
   → 确认门仍然打开（SceneData 正确恢复）✅
```

**关键节点检查：**

| 检查点 | 期望行为 | 相关系统 |
|--------|---------|---------|
| Boss 死亡瞬间 | Flag 被设置为 true | `EnemyEntity` + `CombatEvents` |
| Flag 设置后 | 门立即开始开门动画 | `FlagActivatedResponder` |
| 自动存档触发 | Flag 值被写入存档文件 | `SaveManager` + `WorldProgressManager` |
| 读档后 | Flag 值从存档恢复为 true | `PlayerSaveData` 反序列化 |
| 场景重新加载 | 门初始状态为已打开 | `FlagActivatedResponder.OnEnable()` 同步状态 |

### 用例：收集星图碎片 → 不再出现

**测试步骤：**

```
1. 进入场景，确认某个 PersistentBoolItem（星图碎片）可见
2. 收集该碎片（触发 SetActivated(true)）
3. 确认碎片消失（_disableWhenActivated = true）
4. 切换到另一个场景再切回来
   → 确认碎片不再出现 ✅（SceneData 正确恢复）
5. 完整存读档循环
   → 确认碎片仍然不出现 ✅
```

---

## 附录 A — 与现有系统的映射关系

```
Ark 已有系统                     ← 本文档建议扩展
─────────────────────────────────────────────────────
WorldProgressManager             ← 1.1 ProgressFlag 广播 + 1.3 Boss 三阶段
SaveManager / PlayerSaveData     ← 1.2 AbilityRegistry + 2.1 ZoneMap + 2.2 PersistentBool
                                 ← 8.1 自动增量存档 + 8.2 局内/永久分层
MinimapManager                   ← 2.1 StarZone + 2.4 MapPin 类型
Door / RoomManager               ← 2.3 TransitionPoint 细节字段
CombatEvents 事件总线            ← 1.1 ProgressFlag 的触发起点（风险：9.2 僵尸订阅）
WorldPhase / AmbienceController  ← 3.1 BiomeZone（空间维度补充）
EnemyDirector                    ← 3.4 世界层 AI（长期）
HitStopEffect                    ← 5.1 TimeScaleManager 统一管理
ShipMotor                        ← 5.3 Dash 惯性衰减 + 残影
Projectile / LaserBeam 等        ← 6.1 BulletBehaviourData 数据化 + 7.3 CircleCast
StarCoreSO / WeaponTrack         ← 6.2 武器时序字段 + 6.3 后置触发槽（长期）
SnapshotBuilder                  ← 6.4 轨道共鸣检测（长期）
EnemyBrain / Projectile          ← 7.1 IPausable 接口
Camera / Cinemachine             ← 5.4 CameraShakeManager
StarChartItemSO 全系列           ← 9.1 SO 污染风险防守
```

## 附录 B — 参考游戏与文档索引

### 已有报告（Docs/Reference/）

| 游戏 | 后端 | 报告文件 | v2.0 关键参考 |
|------|------|---------|-------------|
| Hollow Knight Silksong | Mono (Unity 6) | `Silksong_Deconstruction_Report.md` | Sec.4 Progression / Sec.5 Exploration / PersistentBoolItem |
| TUNIC | IL2CPP (Unity 2020) | `TUNIC_Structure_Analysis.md` | StateVariable SO / ConduitNode 谜题 / AreaLabel |
| Minishoot' Adventures | Mono (Unity 2021) | `Minishoot_Adventures_Structure_Analysis.md` | BulletData / MiniBehaviour / ES3 自动存档 / BiomeManager |
| Rain World | Mono (Unity 2019) | `RainWorld_Structure_Analysis.md` | 双层 AI / 程序化音乐 / 调色板区域着色 |
| Galactic Glitch | IL2CPP (Unity 2021) | `GalacticGlitch_ArtAssets_Analysis.md` | 飞船物理 / Weapon 时序 / 弹幕超类 / BatchUpdate |
| Magicraft | IL2CPP (Unity 6) | `Magicraft_Deconstruction_Report.md` | TimeScaleMgr / 后置触发槽 / DOTS 弹幕 / JSON 法术配置 |

### 需要补充报告的游戏

| 游戏 | 当前状态 | 关键待提取技术 |
|------|---------|-------------|
| BackpackBattles | Godot 加密字节码（无法反编译）；BackpackMonsters C# 源码可读 | 物品融合系统 / FusionRecipe / GridManager / 战斗倒计时 Overtime |

### 核心技术对照表

| 技术方案 | Ark 现有 | 参考来源（最佳实践） | 缺口 |
|---------|---------|----------------|------|
| 时间缩放管理 | `HitStopEffect`（分散） | Magicraft `TimeScaleMgr`（统一栈） | 需要 `TimeScaleManager` |
| 受击闪烁 | 未确认 | Minishoot `IsFlashingEffect`（独立组件） | 需要 `HitFlashEffect` |
| Dash 惯性 | 未确认 | GalacticGlitch `afterBoostDrag` | 需要 ShipMotor 扩展 |
| Screen Shake | 未确认 | GalacticGlitch Cinemachine Impulse | 需要 `CameraShakeManager` |
| 弹道行为 | 四家族独立继承 | Minishoot `BulletData` struct | 需要 `BulletBehaviourData` |
| 武器前/后摇 | 敌人有，玩家未确认 | GalacticGlitch `StartupTime/EndTime` | 需要 StarCoreSO 扩展 |
| 全局暂停 | 未确认 | Minishoot `MiniBehaviour` | 需要 `IPausable` 接口 |
| 自动存档 | 未确认 | Minishoot ES3 `BackupRegularly()` | 需要 SaveManager 扩展 |
| SO 污染防护 | 原则 6（规范） | — | 需要运行时断言 |
| 事件订阅泄漏 | 原则 5（规范） | GalacticGlitch 实例事件（对比） | 需要订阅数监控 |

---

*文档维护原则：每次借鉴方案落地实现后，在对应章节标注"已实现 - {日期}"，避免重复设计。*

---

## Section 5 — 战斗手感反馈系统

> **开发哲学第一条**：Screen shake、HitStop、音效不是"锦上添花"，它们**是**核心体验。本 Section 专门整理"juice"层面的技术方案，以及 Ark 当前在此方向的已知缺口。

### 5.1 HitStop / 时间缩放统一管理

**问题：** 多个系统（Boss 受击、玩家死亡、暴击命中）都可能触发时间缩放，如果各自调用 `Time.timeScale = x` 且不互相协调，短帧内叠加请求会导致时间缩放混乱：A 刚把 timeScale 设为 0.1，B 立刻覆盖为 0.3，A 的恢复计时器按 0.3 计算导致过早恢复。

**参考来源：**
- Magicraft `TimeScaleMgr`（专用管理器，持有当前所有缩放请求的栈，统一解算最终值）
- GalacticGlitch `onActionAfterDodge`（Dodge 后动作窗口的时序控制，与 HitStop 解耦）
- Minishoot HitStop 推测集成在 `CameraManager` 中，与 Shake 同步触发

**Magicraft TimeScaleMgr 核心设计：**

```
请求入栈: TimeScaleMgr.Push(scale=0.1, duration=0.08s, priority=HIGH)
解算:     取当前栈中 priority 最高的请求 → 应用 Time.timeScale
弹栈:     duration 到期后自动弹出 → 重新解算
```

这样即使 5 个系统同时发请求，timeScale 也只取"最高优先级值"，不会相互干扰。

**Ark 当前状态与风险：**

Ark 有 `HitStopEffect` 组件。但需要检查：
- `HitStopEffect` 是否是单例/管理器，还是分散挂在每个 Entity 上？
- 多个敌人同帧死亡时，多个 `HitStopEffect` 同时调用 `Time.timeScale` 会发生什么？

**Ark 适配方案（`TimeScaleManager`）：**

```csharp
// 替代或包装现有 HitStopEffect 的逻辑
public class TimeScaleManager : MonoBehaviour
{
    private readonly SortedList<int, TimeScaleRequest> _requests = 
        new SortedList<int, TimeScaleRequest>(Comparer<int>.Create((a, b) => b.CompareTo(a)));
    
    [Serializable]
    private struct TimeScaleRequest
    {
        public float Scale;
        public float Duration;
        public int   Priority;
        public float ExpiresAt; // Time.unscaledTime + Duration
    }
    
    // 任意系统调用，替代直接写 Time.timeScale
    public void RequestScale(float scale, float duration, int priority = 0)
    {
        var req = new TimeScaleRequest
        {
            Scale     = scale,
            Duration  = duration,
            Priority  = priority,
            ExpiresAt = Time.unscaledTime + duration
        };
        _requests[priority] = req; // 同优先级覆盖（防止暴击连击叠加）
    }
    
    private void Update()
    {
        // 移除过期请求
        var expired = _requests.Where(kv => Time.unscaledTime >= kv.Value.ExpiresAt).ToList();
        foreach (var kv in expired) _requests.Remove(kv.Key);
        
        Time.timeScale = _requests.Count > 0 ? _requests.Values[0].Scale : 1f;
    }
}
```

使用时：
```csharp
// 暴击命中：0.1s HitStop，中优先级
ServiceLocator.Get<TimeScaleManager>().RequestScale(0.05f, 0.08f, priority: 10);

// Boss 死亡：0.5s 慢动作，高优先级（覆盖普通 HitStop）
ServiceLocator.Get<TimeScaleManager>().RequestScale(0.2f, 0.5f, priority: 20);
```

**注意：** PrimeTween 使用 `useUnscaledTime: true` 的 Tween 不受此影响；UI 动画应全部使用 Unscaled Time。

**工期估算：** 新建 `TimeScaleManager` + 接入 `HitStopEffect` 约 1 天。

---

### 5.2 受击视觉反馈链

**问题：** 受击反馈是由多个独立效果叠加构成的链式反应：闪烁 → VFX 粒子 → 飘字 → 音效。每个环节都容易分散在不同脚本中，导致：某个效果多订阅了事件、某个效果缺失也没报错（静默失效）。

**参考来源：**
- Minishoot `IsFlashingEffect.cs`（受击时 SpriteRenderer 快速循环显隐，独立组件）
- Minishoot `ShineEffect.cs`（受击白化闪光，独立组件，与 Flashing 可叠加）
- Minishoot `Fx.cs` 工厂（统一管理 HitFX / 爆炸 / 污渍，`Fx.Play(fxId, position)` 单入口）
- Magicraft 按**元素类型**分组 VFX：Hit_Fire / Hit_Frozen / Hit_Venom / Hit_Thunder
- GalacticGlitch 受伤事件：`OnTakeCollisionDamage` / `OnTakeExplosiveDamage`（按伤害类型分事件）

**Minishoot 的独立组件设计（值得直接照搬）：**

```csharp
// 挂在任意可受击对象上，零耦合
public class HitFlashEffect : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _renderer;
    [SerializeField] private float _flashDuration = 0.08f;
    [SerializeField] private Color _flashColor = Color.white;
    
    private Color _originalColor;
    
    private void Awake() => _originalColor = _renderer.color;
    
    public void Flash()
    {
        // PrimeTween: 白化 → 恢复，不受 timeScale 影响（使用 unscaled）
        Tween.Color(_renderer, _flashColor, _flashDuration * 0.5f, useUnscaledTime: true)
             .OnComplete(() => Tween.Color(_renderer, _originalColor, _flashDuration * 0.5f, useUnscaledTime: true));
    }
}
```

**Ark 的元素类型 VFX 扩展点（参考 Magicraft）：**

```csharp
// DamagePayload 已有 DamageType，在 VFX 系统中对应不同特效
public enum DamageType { Physical, Fire, Ice, Electric, Void }

// VFX 工厂按类型分发（避免 if/switch 散落各处）
public class CombatVfxFactory : MonoBehaviour
{
    [SerializeField] private PooledVFX[] _hitVfxByType; // 按 DamageType 枚举索引
    
    public void PlayHitVfx(DamageType type, Vector3 position)
    {
        var vfx = _hitVfxByType[(int)type];
        // 从对象池取出，设置位置，播放
        var instance = ServiceLocator.Get<PoolManager>().Get(vfx.gameObject);
        instance.transform.position = position;
    }
}
```

**受击反馈完整链（Ark 建议标准化顺序）：**

```
DamagePayload 命中
    ↓ IDamageable.TakeDamage()
    ├── 1. TimeScaleManager.RequestScale()        → HitStop (5.1)
    ├── 2. HitFlashEffect.Flash()                 → 白化闪烁（本节）
    ├── 3. CombatVfxFactory.PlayHitVfx(type, pos) → 元素特效粒子（本节）
    ├── 4. DamageNumberPool.Show(damage, pos)     → 飘字
    ├── 5. CinemachineImpulse.GenerateImpulse()   → Screen Shake (5.4)
    └── 6. AudioManager.Play(sfxId)               → 受击音效
```

**Ark 风险：** 当前 `PooledVFX` 是否按伤害类型扩展了变体？`DamagePayload` 的 `DamageType` 字段是否已在 VFX 系统中被消费？

**工期估算：** `HitFlashEffect` 组件约半天；`CombatVfxFactory` 按类型分发约 1 天。

---

### 5.3 Boost/Dash 手感 — 惯性与残影

**问题：** 飞船 Dash/Boost 结束后"手感"的关键在于**不能瞬停**。瞬停让飞船像一个滑动块，而非一艘有质量的飞船。GalacticGlitch 的飞船被玩家普遍认为手感优秀，其核心正是 Boost 结束的惯性处理。

**参考来源：**
- GalacticGlitch `afterBoostDrag`（Boost 结束后应用更大的 Rigidbody drag，而非立刻停止）
- GalacticGlitch `dragChangeTime`（drag 不是瞬间切换，而是在 `dragChangeTime` 秒内平滑过渡）
- GalacticGlitch `speedModAfterDodge`（Dodge 后有短暂速度加成，鼓励 Dodge-Cancel 连招）
- GalacticGlitch `Dodge_Sprite`（独立 SpriteRenderer 节点，Dodge 时出现青绿色残影，alpha 随时间衰减）

**GalacticGlitch 的 Boost 惯性实现（伪代码还原）：**

```csharp
// ShipMotor（GG 推测逻辑，用于 Ark 参考）
private IEnumerator OnBoostEnd()
{
    // Boost 结束时不直接停止，而是切换到高阻力模式
    float elapsed = 0f;
    float normalDrag = _rb.drag;
    
    while (elapsed < dragChangeTime)
    {
        elapsed += Time.deltaTime;
        _rb.drag = Mathf.Lerp(normalDrag, afterBoostDrag, elapsed / dragChangeTime);
        yield return null;
    }
    
    // 高阻力自然减速后，恢复正常 drag
    yield return new WaitForSeconds(0.3f);
    _rb.drag = normalDrag;
}
```

**Ark 适配方案（在 `ShipMotor` 中扩展）：**

```csharp
public class ShipMotor : MonoBehaviour
{
    // ... 现有字段 ...
    
    [Header("Dash Feel")]
    [SerializeField] private float _dashAfterDrag = 8f;        // Dash 结束后的高阻力
    [SerializeField] private float _dragTransitionTime = 0.15f; // drag 平滑过渡时间
    [SerializeField] private float _dashSpeedBonus = 1.3f;      // Dash 后短暂速度加成倍率
    [SerializeField] private float _dashSpeedBonusDuration = 0.2f;
    
    // Dash 残影（可选）
    [SerializeField] private SpriteRenderer _dashAfterimage;
    [SerializeField] private float _afterimageAlpha = 0.6f;
    [SerializeField] private float _afterimageFadeTime = 0.3f;
    
    private async UniTaskVoid OnDashEnd()
    {
        // 短暂速度加成（鼓励 Dash 后立即射击）
        _speedMultiplier = _dashSpeedBonus;
        await UniTask.Delay(TimeSpan.FromSeconds(_dashSpeedBonusDuration), 
                            cancellationToken: destroyCancellationToken);
        _speedMultiplier = 1f;
        
        // 惯性衰减：平滑切换 drag
        float startDrag = _rb.drag;
        float elapsed = 0f;
        while (elapsed < _dragTransitionTime)
        {
            elapsed += Time.deltaTime;
            _rb.drag = Mathf.Lerp(startDrag, _dashAfterDrag, elapsed / _dragTransitionTime);
            await UniTask.Yield(cancellationToken: destroyCancellationToken);
        }
        
        // 恢复正常 drag
        _rb.drag = _normalDrag;
    }
}
```

**Dash 残影的最简实现（PrimeTween）：**

```csharp
private void ShowDashAfterimage(Vector3 position, Quaternion rotation)
{
    if (_dashAfterimage == null) return;
    
    var ghost = Instantiate(_dashAfterimage, position, rotation);
    ghost.color = new Color(0.2f, 1f, 0.8f, _afterimageAlpha); // 青绿色
    Tween.Alpha(ghost, 0f, _afterimageFadeTime)
         .OnComplete(() => Destroy(ghost.gameObject));
}
```

**工期估算：** `ShipMotor` 扩展惯性参数约半天；Dash 残影约半天。

---

### 5.4 Screen Shake 架构

**问题：** Screen Shake 实现看起来简单，但有两个陷阱：① 多个系统同时触发 Shake 叠加失控；② 代码层 Shake 和 Cinemachine 不兼容（Camera.main.transform 直接操作和 Cinemachine Virtual Camera 不能混用）。

**参考来源：**
- GalacticGlitch：使用 Cinemachine Impulse Source（`CinemachineImpulseSource.GenerateImpulse()`）
- Minishoot：`CameraManager` 集成 Shake 逻辑，统一入口
- TUNIC：`StatefulCameraZone`（状态驱动的摄像机区域，进入特定区域触发摄像机预设）

**Ark 的推荐方案（Cinemachine Impulse，与现有 Cinemachine Confiner 兼容）：**

Ark 已经使用 Cinemachine（Level 系统中用 Cinemachine Confiner）。直接扩展：

```csharp
// 挂在场景常驻 Camera GameObject 上
[RequireComponent(typeof(CinemachineImpulseSource))]
public class CameraShakeManager : MonoBehaviour
{
    public static CameraShakeManager Instance { get; private set; }
    
    private CinemachineImpulseSource _impulseSource;
    
    private void Awake()
    {
        Instance = this;
        _impulseSource = GetComponent<CinemachineImpulseSource>();
    }
    
    // 按强度触发（整合到 TimeScaleManager 附近，统一 juice 入口）
    public void Shake(float amplitude = 1f, float duration = 0.15f)
    {
        _impulseSource.m_ImpulseDefinition.m_AmplitudeGain = amplitude;
        _impulseSource.m_ImpulseDefinition.m_TimeEnvelope.m_SustainTime = duration;
        _impulseSource.GenerateImpulse();
    }
    
    // 预设强度封装（语义清晰，避免策划到处改魔法数字）
    public void ShakeLight()  => Shake(0.3f, 0.1f);  // 普通子弹命中
    public void ShakeMedium() => Shake(0.8f, 0.2f);  // 暴击/精英受击
    public void ShakeHeavy()  => Shake(2.0f, 0.4f);  // Boss 死亡/爆炸
}
```

调用示例（接入受击反馈链）：
```csharp
// 在 IDamageable.TakeDamage 中，根据伤害量触发不同等级的 Shake
if (damage.IsCritical)
    CameraShakeManager.Instance.ShakeMedium();
else
    CameraShakeManager.Instance.ShakeLight();
```

**工期估算：** `CameraShakeManager` 约半天；接入战斗事件约半天。

---

## Section 6 — 武器与弹幕系统

> 本 Section 专注"玩家怎么打出去的东西"——从弹幕数据设计到武器攻击时序，再到进阶的触发条件和组合进化系统。

### 6.1 弹幕数据单结构覆盖所有变体

**问题：** Ark 的四家族投射物（`Projectile` / `LaserBeam` / `EchoWave` / `BoomerangModifier`）是独立继承链。每次要给弹幕添加新属性（如正弦波运动、命中分裂），需要在每个类里分别添加逻辑，维护成本随投射物种类线性增长。

**参考来源：**
- Minishoot `BulletData`（单个 struct，包含：速度 / 射程 / 扩散角 / 正弦波参数 / 角速度 / 缩放曲线 / 穿透层数 / 追踪目标）
- GalacticGlitch `Bullet` 超类（追踪 / 回旋 / 弧线 / 分裂 / AOE触发 全部编码在一个类，通过布尔开关激活）
- Magicraft 329 条法术 JSON 配置（`float1/2/3 + int1/2/3` 通用参数槽，法术间共用同一数据格式）

**Minishoot BulletData 的核心设计：**

```csharp
// Minishoot 的 BulletData（字段名来自反编译，已还原注释）
public struct BulletData
{
    // 基础运动
    public float Speed;
    public float Range;           // 最大射程（超出则销毁）
    public float SpreadAngle;     // 散射角度（扩散射击）
    
    // 高级运动
    public float AngularVelocity; // 角速度（弧线弹）
    public float SineAmplitude;   // 正弦波振幅（蛇形弹）
    public float SineFrequency;   // 正弦波频率
    public bool  IsHoming;        // 是否追踪
    public float HomingTurnRate;  // 追踪转向速率
    
    // 视觉缩放
    public AnimationCurve ScaleCurve; // 弹幕生命周期内的缩放曲线
    
    // 穿透
    public int PiercingCount;     // 穿透次数（0 = 不穿透）
}
```

**Ark 适配方案（扩展现有 `ProjectileData` 或 `StarCoreSO`）：**

建议在 `StarCoreSO`（或专门的 `BulletBehaviourData` struct）中增加弹道行为参数，让策划在 SO Inspector 中直接配置弹道变体，而不需要修改代码：

```csharp
[Serializable]
public struct BulletBehaviourData
{
    [Header("Homing")]
    public bool IsHoming;
    [Range(0f, 720f)] public float HomingTurnRatePerSecond;
    
    [Header("Sine Wave")]
    public bool UseSineWave;
    public float SineAmplitude;
    public float SineFrequency;
    
    [Header("Angular")]
    public float AngularVelocity;   // 顺时针为正（弧线弹）
    
    [Header("Penetration")]
    public int PiercingCount;       // 0 = 不穿透
    
    [Header("On-Hit Spawn")]
    public bool SpawnChildOnHit;
    public GameObject ChildProjectilePrefab;
    public int ChildCount;
    public float ChildSpreadAngle;
    
    [Header("Scale Over Lifetime")]
    public bool UseScaleCurve;
    public AnimationCurve ScaleCurve;
}
```

在 `Projectile.cs` 中消费这个 struct（飞行时每帧应用 SineWave/Angular 修正），并在 `OnHit` 时检查 `SpawnChildOnHit`。

**这样只需修改 SO 数据，就能得到追踪弹、蛇形弹、分裂弹，无需新增子类。**

**Ark 风险：** 当前 `Projectile` 的运动逻辑是否完全在代码中硬编码？`EchoWave` 的扩散是否支持数据化配置？

**工期估算：** 新增 `BulletBehaviourData` struct + `Projectile` 消费逻辑约 2 天。

---

### 6.2 武器攻击时序规范（玩家端 Signal-Window）

**问题：** CLAUDE.md 开发哲学第 2 条明确"每个敌人攻击必须遵循 Signal-Window 模型"。但**玩家的射击行为**是否也有前摇/后摇的数据化参数？若没有，玩家在编织态切换武器时可能出现"无缝瞬发"，缺乏手感节奏。

**参考来源：**
- GalacticGlitch `Weapon` 基类（标准化时序字段，值得完整移植）：
  ```csharp
  float StartupTime;    // 前摇（按下到第一发子弹出来的延迟）
  float PerformingTime; // 执行（持续发射时间，用于连射）
  float EndTime;        // 后摇（最后一发后的锁定时间）
  float CooldownTime;   // 冷却（后摇结束到下次可用）
  float MinChargeTime;  // 蓄力下限（蓄力武器）
  float MaxChargeTime;  // 蓄力上限（蓄力武器）
  ```
- GalacticGlitch `attacksInPattern[]` + `patternResetTime`（连发/交替模式）
- GalacticGlitch `InterruptAttack(softInterrupt, interruptReloading, reason)`（中断攻击的语义）

**Ark 适配方案（`StarCoreSO` 扩展）：**

```csharp
// StarCoreSO 中新增武器时序字段（全部 SerializeField，策划可配置）
[Header("Attack Timing")]
[SerializeField] private float _startupTime = 0f;   // 前摇秒数（0 = 瞬发）
[SerializeField] private float _recoveryTime = 0.1f; // 后摇（每发后的硬直）
[SerializeField] private float _cooldownTime = 0.5f; // 完整冷却
[SerializeField] private bool _isChargeable = false; // 是否可蓄力
[SerializeField] private float _minChargeTime = 0.3f;
[SerializeField] private float _maxChargeTime = 1.5f;
[SerializeField] private float _chargeReleaseDamageMult = 2.0f; // 满蓄力伤害倍率

// 攻击模式（支持连发 pattern）
[SerializeField] private int _shotsPerBurst = 1;     // 单次触发的发射数
[SerializeField] private float _inBurstInterval = 0.05f; // 连发帧间隔
```

`WeaponTrack` 在调用 `FireWeapon()` 前，需要等待 `_startupTime` 延迟，发射完成后强制进入 `_recoveryTime` 锁定。

**设计意义：** 即使前摇只有 0.05 秒，也会让玩家感受到"我**触发了**一次射击"，而不是"射击在持续发生"。这是手感节奏的基础。

**工期估算：** `StarCoreSO` 字段扩展约半天；`WeaponTrack` 消费逻辑约 1 天。

---

### 6.3 后置触发槽（Conditional Trigger Slot）

**问题：** 星图当前的触发机制是"玩家按射击键 → 轨道发射"。这是主动触发。Magicraft 提供了一个更丰富的维度：**条件触发**——某些效果在满足特定游戏事件时自动激活，而不是手动触发。

**参考来源（Magicraft 后置法术槽）：**

```
主槽（主动触发）：玩家按键 → 发射主法术
后置槽（条件触发）：满足条件 → 自动发射后置法术

条件类型（Magicraft 实现的5种）：
1. 击杀触发（OnKill）       → 每击杀一个敌人，额外发射
2. 命中触发（OnHit）        → 每次子弹命中，额外发射
3. 暴击触发（OnCrit）       → 每次暴击，额外发射
4. 受伤触发（OnTakeDamage） → 每次受击，反射弹幕
5. 移动触发（OnMove）       → 移动时持续释放尾迹效果
```

**对 Ark 的设计翻译（星图 WeaponTrack 扩展）：**

```csharp
public enum TrackTriggerCondition
{
    Manual,            // 玩家主动触发（当前唯一模式）
    OnEnemyKill,       // 敌人死亡时
    OnBulletHit,       // 子弹命中目标时
    OnCriticalHit,     // 暴击命中时
    OnPlayerDamaged,   // 玩家受击时（反击型）
    OnOverheat,        // 过热时（热量系统！）
    OnCooldown,        // 热量归零时（冷却奖励）
    Periodic,          // 周期性自动触发（时间间隔）
}

// WeaponTrack 扩展字段
[SerializeField] private TrackTriggerCondition _triggerCondition = TrackTriggerCondition.Manual;
[SerializeField] private float _periodicInterval = 2f; // Periodic 模式用

// WeaponTrack 订阅对应事件
private void OnEnable()
{
    switch (_triggerCondition)
    {
        case TrackTriggerCondition.OnEnemyKill:
            CombatEvents.OnEnemyKilled += OnConditionMet;    break;
        case TrackTriggerCondition.OnCriticalHit:
            CombatEvents.OnCriticalHit += OnConditionMet;   break;
        case TrackTriggerCondition.OnPlayerDamaged:
            CombatEvents.OnPlayerDamaged += OnConditionMet; break;
        case TrackTriggerCondition.OnOverheat:
            CombatEvents.OnOverheated += OnConditionMet;    break;
    }
}
```

**与热量系统的联动（Ark 独有的设计空间）：**

`OnOverheat` 和 `OnCooldown` 是 Ark 特有的触发条件，在其他参考游戏中不存在。飞船过热时自动触发一个"过热爆发"轨道，把惩罚变成奖励——这是一个差异化的战术选择层。

**工期估算：** `TrackTriggerCondition` 枚举 + `WeaponTrack` 订阅逻辑约 1.5 天；每种触发条件的 CombatEvents 事件定义约半天。

---

### 6.4 武器组合进化系统（星图深度拓展）

**问题：** 星图当前是"横向组合"系统：不同 Core/Prism/Sail 组合产生不同效果。这很好，但是否也需要"纵向成长"？玩家长期使用同一套配置，能否有某种进化奖励？

**参考来源：**

| 游戏 | 系统 | 机制 |
|------|------|------|
| GalacticGlitch | `WeaponEvolution` | 每把武器有 `nextWeapon` 进化目标，收集足够 `UpgradeFragment` 触发进化 |
| BackpackMonsters | `FusionRecipes[]` | 多个物品相邻放置满足配方 → 自动融合为更强的新物品 |
| Magicraft | 遗物叠加 `maxCount` | 同一遗物可以叠加多次，每次叠加增强效果（有上限） |

**对 Ark 的设计选项分析：**

```
选项 A（GG 线性进化）：星核有 Tier 1 → Tier 2 → Tier 3 进化链
  + 明确的成长目标，驱动力强
  - 与银河城永久进度有矛盾（Roguelite 每局重置则等同每局都在起点）

选项 B（BackpackMonsters 邻格融合）：特定 Core+Prism 组合在同一轨道触发"共鸣效果"
  + 不影响各部件独立性，只增加组合奖励
  + 和现有星图架构完全兼容（在 SnapshotBuilder 中检测共鸣条件）
  - 需要大量策划工作设计有意义的共鸣配对

选项 C（Magicraft 叠加增强）：同一 Core 装备多次时叠加强化（星图允许重复部件）
  + 实现最简单（计数器 × 数值缩放）
  - 可能导致"单核叠满"的无聊策略
```

**推荐方案（选项 B — 轨道共鸣）：**

```csharp
// SnapshotBuilder 中，检测当前轨道配置是否触发共鸣
public struct ResonanceEffect
{
    public StarCoreSO RequiredCore;
    public PrismSO    RequiredPrism;  // 可为 null（纯 Core 自共鸣）
    public LightSailSO RequiredSail;  // 可为 null
    public float DamageMultiplier;
    public float FireRateMultiplier;
    public PooledVFX ResonanceVfx;    // 共鸣时的特殊视觉
}

// ResonanceSO 资产（策划配置所有共鸣配对）
[CreateAssetMenu(menuName = "ProjectArk/Combat/Resonance")]
public class ResonanceSO : ScriptableObject
{
    public ResonanceEffect[] Resonances;
}
```

`SnapshotBuilder.BuildSnapshot()` 在生成快照时检查轨道上的 Core+Prism+Sail 组合，如果匹配任意 `ResonanceSO` 条目，则在快照中应用对应的乘数。

**工期估算：** 轨道共鸣数据结构 + `SnapshotBuilder` 检测约 2 天；共鸣 VFX 约 1 天（可独立推迟）。

---

## Section 7 — 性能与循环架构

> 本 Section 处理"游戏跑起来不卡"的工程问题，以及"编织态/暂停时所有东西都停下来"的系统控制问题。Ark 当前规模下这不是紧迫问题，但架构应该在今天就为明天留好接口。

### 7.1 全局暂停控制（IPausable 接口）

**问题：** Ark 的编织态（WeavingState）需要"暂停战斗、保持 UI 动画"。在 Play Mode 下多次测试后，最容易出现的问题是：某个系统没有被暂停（子弹继续飞、敌人继续 AI 计算）或某个本不该暂停的系统被暂停了（UI 动画卡住）。

**参考来源（Minishoot `MiniBehaviour`）：**

```csharp
// Minishoot 的解法：所有游戏逻辑对象继承 MiniBehaviour
public abstract class MiniBehaviour : MonoBehaviour
{
    // 全局开关，由 GameManager 控制
    public static bool IsGameRunning { get; private set; }
    
    protected abstract void MiniUpdate();  // 子类实现游戏逻辑
    
    private void Update()
    {
        if (!IsGameRunning) return;  // 一行代码暂停所有游戏对象
        MiniUpdate();
    }
}

// 暂停时：GameManager.PauseAll() → MiniBehaviour.IsGameRunning = false
// 所有继承 MiniBehaviour 的对象（敌人AI/子弹/特效）统一停止
// UI 类不继承 MiniBehaviour，不受影响
```

**Ark 适配方案（轻量版，不需要所有类改继承）：**

引入 `IPausable` 接口，不强制改变继承链：

```csharp
public interface IPausable
{
    void OnPause();
    void OnResume();
}

// 游戏暂停管理器（注册到 ServiceLocator）
public class GamePauseManager : MonoBehaviour
{
    private readonly List<IPausable> _pausables = new();
    
    public void Register(IPausable p)   => _pausables.Add(p);
    public void Unregister(IPausable p) => _pausables.Remove(p);
    
    // 编织态进入/退出时调用
    public void PauseGameplay()  { foreach (var p in _pausables) p.OnPause(); }
    public void ResumeGameplay() { foreach (var p in _pausables) p.OnResume(); }
}
```

关键系统实现 `IPausable`：

```csharp
// EnemyBrain 实现
public class EnemyBrain : MonoBehaviour, IPausable
{
    private bool _isPaused;
    
    public void OnPause()  => _isPaused = true;
    public void OnResume() => _isPaused = false;
    
    private void Update()
    {
        if (_isPaused) return;
        // AI 逻辑...
    }
    
    private void OnEnable()  => ServiceLocator.Get<GamePauseManager>().Register(this);
    private void OnDisable() => ServiceLocator.Get<GamePauseManager>().Unregister(this);
}
```

**注意：** 子弹（对象池中）需要在 `OnReturnToPool()` 中 Unregister，在激活时 Register。

**工期估算：** `IPausable` 接口 + `GamePauseManager` 约 1 天；接入核心系统（EnemyBrain / Projectile / EchoWave）约 1 天。

---

### 7.2 批量 Update 优化（何时引入，如何引入）

**问题：** Unity 的每个 `MonoBehaviour.Update()` 都有独立的 C# → C++ 调用开销（约 20-50ns）。当场景中有 200 个活跃对象时，仅 Update 调用本身就会消耗约 4-10ms（超出 60fps 的预算）。

**参考来源：**
- GalacticGlitch `BatchUpdate`（自定义批量更新系统，替代 Unity 全对象 `Update()` 轮询）
- Rain World Futile（完全绕过 Unity MonoBehaviour 渲染，手动管理 Mesh 批次）

**何时需要引入 BatchUpdate（Ark 的判断标准）：**

```
当前 Ark 规模：单房间 5-20 个活跃 AI + 30-100 个活跃子弹
预期 Update 调用数：~120 个/帧
20ns × 120 = 2.4ms → 在 60fps 预算（16.6ms）中占比 14%
→ 当前阶段：不需要优化

需要引入 BatchUpdate 的触发条件：
- Unity Profiler 中 "Update" 占帧时间超过 3ms
- 场景活跃对象超过 300 个（包含粒子系统的 MonoBehaviour）
- 开始制作"密集弹幕"或"群体 AI"房间
```

**最小侵入式 BatchUpdate 实现（当需要时）：**

```csharp
// 注册到全局批量更新器，而非依赖 Unity Update
public interface IBatchUpdatable
{
    void BatchUpdate(float deltaTime);
}

public class BatchUpdateManager : MonoBehaviour
{
    private readonly List<IBatchUpdatable> _updatables = new(256);
    
    private void Update()
    {
        float dt = Time.deltaTime;
        // 单次循环替代 N 次独立的 MonoBehaviour.Update 回调
        for (int i = _updatables.Count - 1; i >= 0; i--)
            _updatables[i].BatchUpdate(dt);
    }
}
```

**工期估算：** 基础框架约 1 天；接入现有系统约 2-3 天（只在性能实际成为问题时做）。**当前阶段标记为 [FUTURE]**。

---

### 7.3 子弹检测：CircleCast vs OnTriggerEnter2D

**问题：** Ark 使用 `OnTriggerEnter2D` 检测子弹碰撞。这在大多数情况下工作正常，但有一个已知的物理学问题：**高速子弹可能穿过薄墙**（Tunneling）——如果子弹在单帧内移动距离超过了碰撞体的厚度，Unity 物理系统可能不触发碰撞回调。

**参考来源（Minishoot 的解法）：**

```csharp
// Minishoot Bullet.Update() 的运动检测（推测还原）
private void Update()
{
    Vector2 origin = transform.position;
    Vector2 direction = transform.up;
    float distance = _speed * Time.deltaTime;
    
    // CircleCast 沿运动路径扫描（而非点检测）
    var hit = Physics2D.CircleCast(origin, _radius, direction, distance, _hitLayers);
    
    if (hit.collider != null)
    {
        // 精确设置位置到命中点（而非子弹飞过去）
        transform.position = hit.point;
        OnHit(hit.collider, hit.normal);
    }
    else
    {
        transform.position += (Vector3)(direction * distance);
    }
}
```

**CircleCast 的优势：**
1. **防穿墙**：扫描整条移动路径，不会因为单帧移动过远而漏检
2. **精确命中点**：返回 `hit.point` 和 `hit.normal`，可以实现入射特效方向正确
3. **自碰撞可控**：LayerMask 直接过滤，不依赖 Unity 物理碰撞矩阵

**Ark 适配建议：**

不需要全面改造，只对**高速子弹**应用此方案（如穿透弹、激光追踪弹）：

```csharp
// Projectile.cs 新增可选的 ContinuousDetection 模式
[Header("Continuous Detection")]
[SerializeField] private bool _useContinuousDetection = false; // 高速子弹开启
[SerializeField] private float _detectionRadius = 0.1f;

private void Update()
{
    if (_useContinuousDetection)
        MoveContinuous();
    else
        MoveDiscrete(); // 原有逻辑，依赖物理引擎
}
```

**工期估算：** `Projectile.cs` 扩展约半天；测试场景约半天。

---

### 7.4 对象池规模判断与 DOTS 升级路径

**问题：** 何时 Ark 的 `PoolManager` 会成为瓶颈？现有方案的上限在哪里？什么时候需要考虑 Jobs/DOTS？

**参考来源（Magicraft 的极端案例）：**

Magicraft 支持屏幕内同时存在**数千个法术实体**，使用：
1. `ObjPoolMgr` 对象池（基础层）
2. **Unity ECS + Jobs**（高性能层）：碰撞检测、运动更新完全在 Jobs 中并行
3. `ComputeShader`（70个）：辅助物理计算

**Ark 的规模上限预估：**

| 场景 | 预估活跃子弹数 | PoolManager 是否足够 |
|------|-------------|---------------------|
| 普通战斗房间 | 20-80 | 足够 |
| 精英房间 | 50-150 | 足够 |
| Boss 战（弹幕密集） | 100-300 | 足够 |
| 假设：密集弹幕 Boss | 300-1000 | 开始有 GC 压力 |
| Magicraft 规模 | 1000+ | 需要 ECS |

**结论：** Ark 当前规模不需要 ECS。但如果未来 Boss 战设计为"密集弹幕回避"风格（如东方 Project 类型），则在 Boss 战专用场景中可以引入 Job System 仅处理子弹物理，其他系统保持 MonoBehaviour。

**当前阶段行动：** 确保 `PoolManager` 的 `Get()` / `Return()` 方法没有 `Linq` 或不必要的 `new`；在 Profiler 中验证 GC Alloc 为 0。

**工期估算：** [FUTURE] 当 Boss 战弹幕超过 300 个时评估；当前只做 Profiler 验证（约半天）。

---

## Section 8 — 存档与数据架构

> 存档系统的设计错误在游戏开发中期才会暴露，但修复代价极高。本 Section 从参考游戏中提取存档架构的最佳实践，尤其关注 Roguelite 和银河城的不同存档需求。

### 8.1 自动增量存档策略

**问题：** Ark 目前的存档触发时机是什么？玩家在存档点存档（主动），还是有自动存档兜底？如果游戏崩溃，玩家会丢失多少进度？

**参考来源：**
- Minishoot ES3 `BackupRegularly()`（在 `LateUpdate` 中按时间间隔自动保存，不需要手动调用）
- Magicraft `StateVariableMgr`（统一管理所有游戏状态变量，状态变更时自动标记 dirty，下一帧 LateUpdate 批量写入磁盘）
- Silksong `GameManager.SaveLevelState()`（场景卸载时自动触发，保证场景物件状态不丢失）

**Minishoot ES3 自动存档的设计精髓：**

```csharp
// 不需要手动调用 Save，在 LateUpdate 中自动检查
private float _lastSaveTime;
private const float AUTO_SAVE_INTERVAL = 30f; // 30 秒自动存一次

private void LateUpdate()
{
    if (Time.time - _lastSaveTime >= AUTO_SAVE_INTERVAL)
    {
        BackupRegularly(); // 异步写入，不阻塞主线程
        _lastSaveTime = Time.time;
    }
}
```

**Ark 适配方案：**

存档触发应该是三层防线：

```
层 1 — 主动存档（Checkpoint）：
  玩家激活存档点 → 完整存档（HP、位置、已激活的所有 Flag）

层 2 — 场景退出存档（自动）：
  玩家离开场景时 → 保存当前场景的 PersistentBool 状态
  → 这是防止"炸了墙但没存档就崩溃"问题的关键

层 3 — 增量自动存档（兜底）：
  每 60 秒自动写一次当前状态
  → 只写"会变化的状态"（已触发的 Flag、房间发现记录）
  → 不包含玩家位置（防止存到悬崖边上）
```

**`SaveManager` 扩展（UniTask 异步写入）：**

```csharp
public class SaveManager : MonoBehaviour
{
    // ... 现有代码 ...
    
    [Header("Auto Save")]
    [SerializeField] private float _autoSaveInterval = 60f;
    private float _lastAutoSave;
    
    private void LateUpdate()
    {
        if (Time.time - _lastAutoSave >= _autoSaveInterval)
        {
            _lastAutoSave = Time.time;
            AutoSaveAsync(destroyCancellationToken).Forget();
        }
    }
    
    private async UniTaskVoid AutoSaveAsync(CancellationToken token)
    {
        // 只保存增量状态（Flags + PersistentBools），不保存位置
        await UniTask.SwitchToThreadPool();    // 切到线程池，不阻塞主线程
        SaveFlagsAndPersistentBools();
        await UniTask.SwitchToMainThread(token); // 切回主线程
    }
}
```

**工期估算：** `SaveManager` 扩展自动存档约半天；场景退出自动存档约半天。

---

### 8.2 局内存档 vs 永久存档分离（RunData 模式）

**问题：** Ark 是否明确区分了"这一局的进度"和"跨局的永久解锁"？这两类数据的存档频率、重置逻辑、回档处理完全不同，混在一起会导致级联 bug。

**参考来源：**

| 游戏 | 局内数据 | 永久数据 |
|------|---------|---------|
| BackpackBattles | `RunData`（当前局背包、金币、局数） | `GameData`（总胜率、Elo、解锁角色） |
| Magicraft | `Battle.unity` 场景内所有状态 | `Camp.unity` 大厅的持久解锁数据 |
| Silksong | 无（纯银河城，无 Roguelite 局内概念） | `PlayerData`（全部永久） |

**Ark 的定位（银河城 + 星图 Roguelite 组合）：**

Ark 是一个**混合游戏**：区域探索进度是永久的（银河城维度），但星图配置可能是局内的（Roguelite 维度）。这个设计决策需要明确：

```
方案 A：纯银河城
  → 所有进度永久，死亡只扣 HP，存档点复活
  → 星图配置永久保存，玩家自由装备
  → 存档：一套 PlayerSaveData，无 RunData

方案 B：银河城框架 + 局内星图
  → 探索进度（区域解锁/Boss 击败）永久
  → 每局星图从零开始收集部件（Roguelite 随机池）
  → 存档：PlayerSaveData（永久）+ RunSaveData（局内）

方案 C：完整 Roguelite（GalacticGlitch 模式）
  → 每局从头开始，死亡全部重置
  → 只有元进度（解锁新武器类型）跨局保存
  → 存档：RunData（每局）+ MetaData（元进度）
```

**Ark 建议的数据分层（基于 CLAUDE.md 的银河城定位）：**

```csharp
// 永久层：银河城进度
[Serializable]
public class PlayerSaveData
{
    public Dictionary<string, BossRecord>  BossRecords;
    public Dictionary<string, bool>        ProgressFlags;
    public Dictionary<string, bool>        UnlockedAbilities;
    public HashSet<StarZone>              UnlockedZoneMaps;
    public Dictionary<string, Dictionary<string, bool>> ScenePersistentBools;
    public HashSet<string>                DiscoveredRooms;
    // 玩家最后所在位置（复活点，而非实时位置）
    public string LastCheckpointScene;
    public string LastCheckpointId;
}

// 局内层：当前星图配置（如果 Ark 引入 Roguelite 随机池）
[Serializable]
public class RunSaveData
{
    public List<string> EquippedCoreIds;     // 当前轨道上的 Core
    public List<string> EquippedPrismIds;    // 当前轨道上的 Prism
    public List<string> EquippedSailIds;     // 当前轨道上的 Sail
    public int          RunScore;
    public float        RunDuration;
    // 注意：RunSaveData 死亡时清空，不在 Checkpoint 存档中保留
}
```

**工期估算：** 数据结构定义约半天；存档层分离重构（如现有混在一起）约 1-2 天。

---

### 8.3 完整的存档数据分层图

基于以上各方案，Ark 的存档系统建议如下分层：

```
PlayerSaveData（永久层）
├── 进度标志       → ProgressFlags (Dictionary<string, bool>)
├── Boss 记录      → BossRecords (Dictionary<string, BossRecord>)
├── 技能解锁       → UnlockedAbilities
├── 地图数据       → UnlockedZoneMaps + DiscoveredRooms
├── 场景持久化     → ScenePersistentBools
└── 最后存档点     → LastCheckpointScene + LastCheckpointId

RunSaveData（局内层，可选）
├── 星图配置       → Equipped* Lists
└── 局内统计       → Score, Duration, KillCount

SystemSaveData（系统层，独立文件）
├── 音量设置       → MasterVolume, SfxVolume, BgmVolume
├── 分辨率/窗口    → Resolution, FullscreenMode
└── 无障碍设置     → ColorblindMode, TextSize

存档触发时机：
  PlayerSaveData → 存档点激活 + 每60s增量自动 + 场景卸载时
  RunSaveData    → 每个重要事件后 + 场景卸载时
  SystemSaveData → 设置改变时即时写入
```

**工期估算：** 数据分层重构（如现有 SaveData 混杂）约 1 天。

---

## Section 9 — 架构风险红队

> **本节是本文档最重要的新增内容。** 站在 Lead Architect 的角度，主动识别 Project Ark 当前架构中可能在中后期爆发的潜在问题。每条风险均给出检查方法和防御方案。

### 9.1 [HIGH] 星图 SO 运行时数据污染

**风险等级：HIGH**

**风险描述：**

`StarChartItemSO`（及其子类 `StarCoreSO` / `PrismSO` / `LightSailSO`）是 ScriptableObject 资产。CLAUDE.md 架构原则 6 明确"严禁在运行时修改 SO"，但这个规则**极难在代码审查中发现违反**——因为写 `coreSO.baseAttack = 10` 和写 `_runtimeAttack = 10` 在语法上完全相同，只是左值不同。

**最常见的违反模式：**

```csharp
// 危险：直接修改 SO
void ApplyPrismModifier(StarCoreSO coreSO, float mult)
{
    coreSO.BaseDamage *= mult;  // ← 这会污染 Editor 中的 SO 资产！
}

// 安全：修改运行时副本
void ApplyPrismModifier(ref RuntimeCoreData runtimeData, float mult)
{
    runtimeData.BaseDamage *= mult;  // ← 只影响当前局，退出 Play Mode 后消失
}
```

**检查方法：**

```
1. 在 Unity Editor 中，Play Mode 前记录某个 StarCoreSO 的 BaseDamage 值
2. 进入 Play Mode → 配置星图 → 开几轮战斗 → 退出 Play Mode
3. 检查同一 StarCoreSO 的 BaseDamage 值是否与步骤 1 相同
4. 若不同：已发生 SO 污染，需要在 WeaponTrack / SnapshotBuilder 中找到直接写入 SO 的代码
```

用 MCP 自动化检查：
```
FetchMcpResource: mcpforunity://editor_state → 确认不在 Play Mode
manage_asset: search StarCoreSO → 读取字段值 → 记录
进入 Play Mode，战斗若干次，退出
manage_asset: search StarCoreSO → 读取字段值 → 对比
```

**防御方案：**

在 `StarChartItemSO` 基类添加运行时防护：

```csharp
public abstract class StarChartItemSO : ScriptableObject
{
#if UNITY_EDITOR
    // Editor Only 守卫：任何运行时字段修改都会触发警告
    protected void OnValidate()
    {
        if (Application.isPlaying)
            Debug.LogError($"[SO POLLUTION] {name} 被在 Play Mode 中修改！调用栈：\n{System.Environment.StackTrace}");
    }
#endif
}
```

---

### 9.2 [HIGH] CombatEvents 静态总线僵尸订阅

**风险等级：HIGH**

**风险描述：**

`CombatEvents` 使用 C# 静态事件（`static event Action<...>`）。对象被对象池回收后，如果 `OnReturnToPool()` 没有取消订阅，该对象会继续接收事件——即使它已经被"回收"且逻辑上不再活跃。调用一个池中已回收对象的方法会导致：

1. **静默错误**：回收的子弹又触发了一次伤害计算
2. **空引用异常**：回收时某些引用被清空（如 `_target = null`），事件回调尝试访问时崩溃
3. **级联 bug**：回收的敌人接收到 OnEnemyKilled 事件，重新触发死亡逻辑

**参考对比（GalacticGlitch 的更安全设计）：**

GalacticGlitch 将事件集中在 `Player` **实例**上（非静态）：
```csharp
// GG：实例事件，随对象生命周期自动清理
player.onShootHitEvent += MyCallback;
// player 销毁时，事件自动无效（无人持有引用）
```

而 Ark 的静态总线：
```csharp
// Ark：静态事件，必须手动管理
CombatEvents.OnWeaponFired += MyCallback;
// 对象销毁/回收后，必须手动 -= ，否则 MyCallback 仍会被调用
```

**检查清单（需要审查的类）：**

以下类如果订阅了 `CombatEvents` 静态事件，且可能被对象池回收，需要检查 `OnReturnToPool()` 中是否有对应的取消订阅：

```
□ Projectile.cs         — 可能订阅 OnPlayerDamaged（反击弹幕）
□ LaserBeam.cs          — 可能订阅热量事件
□ EchoWave.cs           — 可能订阅命中事件
□ EnemyBrain 子类       — 可能订阅 OnAnyEnemyDeath（恐惧系统）
□ WeaponTrack.cs        — 可能订阅 OnCriticalHit（后置槽，如 6.3 实现后）
□ HeatSystem 相关组件   — 是否订阅了战斗事件？
```

**防御方案：**

在 `CombatEvents` 中加入订阅泄漏检测（Debug Only）：

```csharp
public static class CombatEvents
{
    private static event Action<DamagePayload> _onWeaponFired;
    
    public static event Action<DamagePayload> OnWeaponFired
    {
        add
        {
            _onWeaponFired += value;
#if UNITY_EDITOR
            if (_onWeaponFired?.GetInvocationList().Length > 20)
                Debug.LogWarning($"[CombatEvents] OnWeaponFired 有 {_onWeaponFired.GetInvocationList().Length} 个订阅者，可能有泄漏！");
#endif
        }
        remove => _onWeaponFired -= value;
    }
}
```

---

### 9.3 [MEDIUM] UniTask CancellationToken 生命周期泄漏

**风险等级：MEDIUM**

**风险描述：**

`async UniTaskVoid` 方法中如果使用了 `UniTask.Delay(duration)` 但没有传入 `CancellationToken`，那么即使该 MonoBehaviour 被 Destroy 或回收，延迟任务仍会继续执行。这在对象池中尤其危险：

```csharp
// 危险：没有 CancellationToken
private async UniTaskVoid DelayedEffect()
{
    await UniTask.Delay(1000); // 等 1 秒
    // 如果对象在这 1 秒内被池回收并重新激活，
    // 上一次调用的任务会在此处继续执行，
    // 对当前（可能是另一个实体）的对象执行操作
    _renderer.color = Color.red;  // ← 可能作用在错误的对象上
}

// 安全：绑定 destroyCancellationToken
private async UniTaskVoid DelayedEffect()
{
    await UniTask.Delay(1000, cancellationToken: destroyCancellationToken);
    _renderer.color = Color.red;
}

// 对象池需要用 CancellationTokenSource，因为 destroyCancellationToken 
// 只在 Destroy 时触发，而对象池是 SetActive(false) 而非 Destroy
private CancellationTokenSource _poolCts;

public void OnReturnToPool()
{
    _poolCts?.Cancel();
    _poolCts?.Dispose();
    _poolCts = null;
}

public void OnGetFromPool()
{
    _poolCts = new CancellationTokenSource();
}
```

**项目搜索检查（执行此命令找出潜在问题）：**

```bash
# 在项目 Scripts 目录中搜索：有 UniTask.Delay 但没有 cancellationToken 参数的方法
rg "UniTask\.Delay\([^)]*\)" --include="*.cs" -l
# 然后逐文件检查是否传入了 cancellationToken
```

**工期估算：** 逐文件检查约 1-2 小时；修复已知泄漏约 1 天。

---

### 9.4 [MEDIUM] ServiceLocator 注册顺序 Awake 依赖

**风险等级：MEDIUM**

**风险描述：**

Unity 的 `Awake()` 执行顺序在不同 GameObject 之间**不确定**（除非在 Project Settings > Script Execution Order 中显式设置）。如果系统 A 在 `Awake()` 中调用 `ServiceLocator.Get<B>()`，而 B 还没执行 `Awake()` 注册，会抛出"服务未注册"异常。

**典型危险场景：**

```csharp
// AudioManager.Awake() 注册
public class AudioManager : MonoBehaviour
{
    private void Awake() => ServiceLocator.Register<AudioManager>(this); // 可能在后面执行
}

// WeaponTrack.Awake() 依赖
public class WeaponTrack : MonoBehaviour
{
    private void Awake()
    {
        // 危险：AudioManager 可能还没注册
        var audio = ServiceLocator.Get<AudioManager>(); // 可能为 null 或抛出异常
    }
}
```

**参考对比（Minishoot 的解法）：**

Minishoot 使用传统单例模式：
```csharp
public class Player : MonoBehaviour
{
    public static Player Instance { get; private set; }
    private void Awake() => Instance = this;
}
// 任意代码在任意时机都可以访问 Player.Instance（即使是 null 也是静默的，不抛异常）
```

**Ark 的规则（应写入 CLAUDE.md）：**

```
ServiceLocator 使用规则（防止 Awake 顺序依赖）：
1. Awake() — 只做 Register（ServiceLocator.Register<T>(this)）
2. Start()  — 可以 Get（ServiceLocator.Get<T>()），此时所有 Awake 已执行完毕
3. 运行时   — 可以 Get，但建议缓存结果而非每次 Get
4. 禁止     — 在 Awake() 中 Get 另一个系统的服务
```

**检查方法：**

```bash
# 搜索在 Awake 中调用 ServiceLocator.Get 的代码
rg "void Awake" -A 10 --include="*.cs" | rg "ServiceLocator\.Get"
```

**工期估算：** 代码搜索约 30 分钟；修复违反规则的调用约 2-4 小时。

---

### 9.5 [MEDIUM] Assembly Definition 边界与依赖方向

**风险等级：MEDIUM**

**风险描述：**

Ark 有明确的程序集划分（`ProjectArk.Core` / `ProjectArk.Combat` / `ProjectArk.Level` / `ProjectArk.Enemy` 等）。程序集边界的目的是强制"低层不依赖高层"。但随着功能增加，会出现需要"高层和低层互相通信"的需求，这时有两种不规范的解决方式：

1. **循环依赖**：Level 引用 Combat，Combat 又引用 Level → 编译失败（可以检测）
2. **向上引用**：Core 程序集引用了 Combat 程序集 → 编译成功但违反分层（难以检测）

**CLAUDE.md 已有规定（但需要验证是否执行）：**

```
禁止低层程序集反向引用高层
正确的跨程序集通信：在 Core 层定义接口，高层实现（依赖反转）
```

**检查方法：**

```bash
# 在项目根目录执行（检查编译是否有循环依赖警告）
dotnet build Project-Ark.slnx 2>&1 | grep -i "circular\|cycle\|dependency"

# 手动检查 .asmdef 文件中的引用关系
```

检查关键边界：
```
ProjectArk.Core     → 不应引用任何其他 ProjectArk.* 程序集
ProjectArk.Combat   → 可以引用 Core，不可引用 Level / Enemy / Ship
ProjectArk.Level    → 可以引用 Core，不可引用 Combat（应通过 Core 的接口/事件总线通信）
ProjectArk.Enemy    → 可以引用 Core + Combat，不可引用 Level（应通过接口）
```

**最常见的违反场景：**

```csharp
// Level 程序集中的 EncounterSystem 需要知道"敌人死了"
// 错误做法：直接引用 EnemyEntity（会导致 Level 依赖 Enemy）
// 正确做法：通过 CombatEvents.OnAnyEnemyDeath 静态事件（在 Core 层定义）
```

**工期估算：** `dotnet build` 检查约 10 分钟；修复违反的引用约 1-3 天（取决于数量）。

---

### 9.6 [LOW] WeavingState 过渡中的 CanvasGroup vs SetActive 遗留状态

**风险等级：LOW（但已踩坑，需要主动防御）**

**风险描述：**

CLAUDE.md 的常见陷阱章节已记录"uGUI 面板禁止用 `SetActive` 控制显隐"。但这条规则在场景配置阶段容易被"间接违反"：

1. 开发者用代码的 `CanvasGroup` 方式实现了 UI 面板
2. 但在 Unity Editor 中手动点了某个 UI 面板的 `active` 勾选框（关掉了它）
3. 这个 inactive 状态被序列化进了 `.unity` 文件
4. Play Mode 启动时面板是 inactive 的，所有 `Awake()` 都被推迟执行

**当前进入"场景配置与验证阶段"，这是最容易踩这个坑的时期。**

**主动检查方法（MCP 自动化）：**

```
find_gameobjects: 按 tag="UIPanel" 或名称包含 "Panel/HUD" 搜索
→ FetchMcpResource: scene/gameobject/{id}/components
→ 检查 m_IsActive 字段（应为 1，即 true）
→ 若为 0：立即在 Editor 中手动激活并保存场景
```

**防御措施：**

在所有 UI 面板的初始化代码中加入运行时检查：

```csharp
private void Awake()
{
#if UNITY_EDITOR
    if (!gameObject.activeSelf)
        Debug.LogError($"[UI] {name} 在 Awake 时是 inactive！请在 Editor 中激活此 GameObject 并保存场景。");
#endif
    // 正确：用 CanvasGroup 控制可见性，而非 SetActive
    _canvasGroup.alpha = 0f;
    _canvasGroup.interactable = false;
    _canvasGroup.blocksRaycasts = false;
}
```

**工期估算：** 添加检查代码约 30 分钟；修复已有 inactive 面板约 1 小时。
