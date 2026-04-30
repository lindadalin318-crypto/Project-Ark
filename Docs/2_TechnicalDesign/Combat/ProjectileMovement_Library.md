# ProjectileMovement 组件库设计

> **⚠️ 修订历史**
>
> | 日期 | 版本 | 变更 |
> |---|---|---|
> | 2026-04-27 14:58 | v1.0 | 初版（H3 方案：组件库 + Prefab 装配） |
> | 2026-04-27 15:10 | v1.1 | **红队审查后保留**：H3 方案经审查仍然成立，内容无实质修改；仅同步配套文档版本号（主文档 + Plan 文档从 v1.0 修正为 v1.1） |
> | 2026-04-27 16:10 | **v1.2（当前）** | **代码对齐修正**：<br>① §2.1 命名空间由 `ProjectArk.Combat.Projectile` 改为 `ProjectArk.Combat`（与现有 `Projectile.cs` 对齐）<br>② §2.2 `Projectile.Initialize` 签名对齐实际代码 `(direction, parms, modifiers)`，并明确 Movement 接入为"可选第四参数"<br>③ §2.2 新增规范："运动学默认用 `Rigidbody2D.velocity` 放 `FixedUpdate` 流，改 transform 的特殊行为写 Update" |

> **文档定位**
> 本文档回答一个问题：**"同一 CoreFamily 内的不同行为（直线/追踪/蛇形/回旋…）怎么实现？"**
>
> - 只定义**组件接口与实现规范**；CSV 层面的 `Archetype` 列与 `MovementParams` 协议见 `StarChart_DataPipeline.md` §9
> - 只定义**库结构与落地清单**；具体每个 Magicraft 法术的复刻清单见后续 Batch 文档
>
> **关联文档**
> - 数据管线 → `StarChart_DataPipeline.md`
> - 具体实施 → `StarChart_DataPipeline_Plan.md`
> - 运行时现役链路 → `StarChart_CanonicalSpec.md`
>
> **维护原则**
> 新增一个 Movement 实现时，必须同步更新本文档的 §3 实现清单与 §5 参数协议表。

---

## 1. 背景与设计决策

### 1.1 问题

Magicraft 的 32 个 Missile 法术（1001-1031），**同为"飞弹"家族却有完全不同的飞行行为**：

- `Bullet` 直线匀速
- `Rollball` 在地面滚动，消除敌方弹幕
- `Butterfly` 追踪最近敌人
- `SnakeWalk` 正弦波蜿蜒
- `Meteor` 从天而降（带重力）
- `Boomerang` 飞出去再回来
- `JudgementBlade` 绕施法者转 + 自动攻击

Magicraft 用**每行为一个 C# 类 + 一个 Prefab** 实现（`Spell1001Bullet : SpellBase` / `Spell1022Boomerang : SpellBase` 等）。

### 1.2 我们的方案：H3（Movement 组件库 + Prefab 装配）

> 决策于 2026-04-27 对话，排除 H1 纯 switch 分派 与 H2 纯 Prefab 多份的原因：
> - H1 扩展时改核心代码（违反 OCP），运行时 AddComponent 违反对象池清单
> - H2 Prefab 爆炸（10 个变体 = 10 份 Prefab），美术维护负担高
> - H3 组件独立成类（OCP），参数可 CSV 覆盖（同一 Prefab 出多个部件变体），天然对象池友好

**核心思路**：

```
┌────────────────────────────────────────────┐
│  IProjectileMovement（接口）                │
│  ├─ void OnSpawn(ProjectileContext ctx)    │
│  ├─ void OnUpdate(float dt)                │
│  └─ void OnReturnToPool()                  │
└────────────────────────────────────────────┘
                 ↑ implement
    ┌────────────┼────────────┐────────────┐
    │            │            │            │
Straight   Tracking   Serpentine  Boomerang  ...
Movement    Movement   Movement    Movement
  (空操作)   (追踪)    (蛇形)      (回旋)

每个 Movement 类：
  • 独立 C# 文件
  • MonoBehaviour（可挂 Prefab）
  • 公开自身参数（Amplitude / Frequency / TurnRate / ...）
  • 在 OnUpdate 中修改 Rigidbody2D.velocity 或 transform
```

**CSV 驱动流程**：

```
CSV Row:  Archetype=Serpentine, MovementParams=Amplitude:5;Frequency:3
              ↓
StarCoreSO: _archetype=Serpentine, _movementOverrides="Amplitude:5;Frequency:3"
              ↓
发射时 ProjectileSpawner:
  1. 从对象池取出 Projectile_Serpentine Prefab（已挂 SerpentineMovement）
  2. 读取 coreSnap.MovementOverrides
  3. MovementParamApplier.Apply(movement, overrides) → 反射写字段
  4. movement.OnSpawn(ctx)
```

---

## 2. 核心契约

### 2.1 接口定义

> **v1.2 修正**：命名空间从 `ProjectArk.Combat.Projectile`（v1.0 错写）改为 `ProjectArk.Combat`，与现有 `Projectile.cs` / `IProjectileModifier.cs` / `BoomerangModifier.cs` 保持一致。类文件物理位置在 `Assets/Scripts/Combat/Projectile/`，但命名空间不跟目录层级。

```csharp
namespace ProjectArk.Combat
{
    /// <summary>
    /// 投射物运动行为接口。每个 Movement 实现一种飞行模式。
    /// 与 IProjectileModifier（修饰/加成）职责正交：
    ///   - Modifier：命中时的额外效果（穿透、冰冻、追加伤害...）
    ///   - Movement：飞行过程中的位置更新（直线、追踪、蛇形...）
    /// </summary>
    public interface IProjectileMovement
    {
        /// <summary>
        /// 投射物从对象池取出后被调用一次。
        /// </summary>
        /// <param name="ctx">发射上下文（方向、速度、初始位置、施法者）</param>
        void OnSpawn(ProjectileContext ctx);

        /// <summary>
        /// 每物理帧调用（由 Projectile.FixedUpdate 转发）。
        /// 改 Rigidbody2D.velocity 的 Movement 必须在此实现。
        /// </summary>
        void OnFixedUpdate(float fixedDeltaTime);

        /// <summary>
        /// 每渲染帧调用（由 Projectile.Update 转发）。
        /// 改 transform.position/rotation 的非物理 Movement（如 Serpentine 叠加偏移）可在此实现。
        /// 默认空实现允许，不需要可 no-op。
        /// </summary>
        void OnUpdate(float deltaTime);

        /// <summary>
        /// 对象池回收时调用，重置内部状态。
        /// 遵循对象池回收清单（Implement_rules.md）。
        /// </summary>
        void OnReturnToPool();
    }

    /// <summary>发射时传递给 Movement 的上下文。</summary>
    public readonly struct ProjectileContext
    {
        public readonly Vector2 InitialDirection;
        public readonly float BaseSpeed;
        public readonly Vector3 OriginPosition;
        public readonly Transform ShooterTransform;  // 可能 null

        public ProjectileContext(Vector2 dir, float speed, Vector3 origin, Transform shooter)
        {
            InitialDirection = dir;
            BaseSpeed = speed;
            OriginPosition = origin;
            ShooterTransform = shooter;
        }
    }
}
```

**Update vs FixedUpdate 规范（v1.2 新增）**：

| 规则 | 选 `OnFixedUpdate` | 选 `OnUpdate` |
|---|---|---|
| 修改 `Rigidbody2D.velocity` | ✅ 必须 | ❌ 会被物理引擎覆盖 |
| 修改 `transform.position` 直接做位置叠加 | ⭕ 可以但注意与物理冲突 | ✅ 推荐 |
| 旋转 `transform.rotation` 视觉（不影响物理） | ⭕ 可以 | ✅ 推荐 |
| 查询附近敌人（感知） | ✅ 推荐（频率稳定） | ⭕ 每帧查询浪费 |

- **StraightMovement / TrackingMovement / GravityMovement / BoomerangMovement**：走 `OnFixedUpdate`（物理速度）
- **SerpentineMovement**（在直线物理速度上叠加正弦位置）：`OnFixedUpdate` 维持 velocity，`OnUpdate` 叠加 `transform.position += sin偏移`
- **空实现允许**：只用其中一个生命周期时另一个 `{ }` 即可。但**两个都空则违反 Movement 存在意义**——应删除该类

### 2.2 `Projectile.cs` 的集成（现有类修改）

> **v1.2 修正**：v1.1 的伪代码签名 `Initialize(ProjectileParams prms, Vector2 dir, Transform shooter)` 与现有代码不符。实际代码签名是 `Initialize(Vector2 direction, ProjectileParams parms, List<IProjectileModifier> modifiers = null)`。本次修正采用**最小破坏**策略——保持现有前三个参数不变，新增可选第四参数 `shooter`，Movement 通过 `GetComponent` 自动发现。

**实际当前签名**（`Assets/Scripts/Combat/Projectile/Projectile.cs:187`）：
```csharp
public void Initialize(Vector2 direction, ProjectileParams parms,
                       List<IProjectileModifier> modifiers = null)
```

**Phase 2 改造后签名**：
```csharp
public void Initialize(Vector2 direction, ProjectileParams parms,
                       List<IProjectileModifier> modifiers = null,
                       Transform shooter = null)
```

**为什么这样改**：
- 前三个参数**顺序和语义完全不变** → 现有 3 处调用点（`ProjectileSpawner.cs:111` 和 `:205`、`AutoTurretBehavior.cs:95`）**无需改动**即可编译通过（新参数默认 null）
- `shooter` 供 `TrackingMovement` 等需要施法者引用的 Movement 使用；传 null 时这些 Movement 自行降级（如回落到"搜索最近敌人"而非"以 shooter 为原点"）
- Phase 2 按需将 `ProjectileSpawner` 两处调用点**升级传入 `track.ShooterTransform`**（如果需要），其他调用点保持 null

**改造后的 `Projectile.cs`（示意）**：
```csharp
public class Projectile : MonoBehaviour, IPoolable
{
    // 既有字段省略
    private IProjectileMovement _movement;      // v1.2 新增
    private bool _hasMovement;                   // 热路径优化

    private void Awake()
    {
        // 既有 Awake 逻辑...
        _movement = GetComponent<IProjectileMovement>();
        _hasMovement = _movement != null;
    }

    public void Initialize(Vector2 direction, ProjectileParams parms,
                           List<IProjectileModifier> modifiers = null,
                           Transform shooter = null)
    {
        // 既有初始化代码（含 _rigidbody.velocity 设置）...

        // v1.2 新增：通知 Movement。必须在既有 velocity 设置之后调用，
        // 让 Movement 的 OnSpawn 能读到基础 velocity 再做修改。
        if (_hasMovement)
        {
            _movement.OnSpawn(new ProjectileContext(
                direction, parms.Speed, transform.position, shooter));
        }
    }

    private void FixedUpdate()
    {
        if (!_isAlive) return;
        if (_hasMovement) _movement.OnFixedUpdate(Time.fixedDeltaTime);
    }

    private void Update()
    {
        if (!_isAlive) return;
        _lifetimeTimer -= Time.deltaTime;
        // ...既有 Update 逻辑（生命周期计时、trail）...

        if (_hasMovement) _movement.OnUpdate(Time.deltaTime);
    }

    public void OnReturnToPool()
    {
        if (_hasMovement) _movement.OnReturnToPool();
        // ...既有重置代码...
    }
}
```

**关键点**：
- `_hasMovement` bool 缓存避免每物理/渲染帧做 null 比较（projectile 是热路径）
- `Movement.OnSpawn` 在既有初始化**之后**调用，让 Movement 能基于已设置的 velocity 做修正
- Prefab 上没挂 `IProjectileMovement` 组件时（现有所有 Prefab），所有分支都 no-op，**对现有投射物完全向后兼容**
- Phase 2 不需要触碰 3 处 `Initialize` 调用点（参数可选，默认 null 即可工作）

---

## 3. 实现清单（MVP + 扩展）

### 3.1 Phase-M1（MVP，2 个 Movement）

必须先跑通的最小集合：

| 类名 | 行为 | 参数 | 复刻 Magicraft |
|---|---|---|---|
| `StraightMovement` | 直线匀速（等于"无 Movement"，仅占位） | — | Bullet (1001) |
| `TrackingMovement` | 每帧微调方向指向最近敌人 | `TurnRate: float` | Butterfly (1003) |

这两个足以验证整条管线（CSV → SO → Spawn → Movement 参数覆盖 → 行为差异）。

### 3.2 Phase-M2（扩展，再 3 个）

| 类名 | 行为 | 参数 | 复刻 Magicraft |
|---|---|---|---|
| `SerpentineMovement` | 正弦波蜿蜒，围绕初始方向做左右摆动 | `Amplitude: float`<br>`Frequency: float` | SnakeWalk (1010) |
| `BoomerangMovement` | 飞出到指定距离后掉头回来 | `MaxDistance: float`<br>`ReturnAcceleration: float` | Boomerang (1022) |
| `GravityMovement` | 带重力下落，向目标点投掷 | `Gravity: float`<br>`InitialUpSpeed: float` | Meteor (1013) |

### 3.3 Phase-M3（完备，再 5 个）

| 类名 | 行为 | 参数 |
|---|---|---|
| `OrbitalMovement` | 围绕施法者旋转 | `Radius: float`<br>`AngularSpeed: float` |
| `HoverMovement` | 悬停原地不动（适合 HoverTorch） | `HoverDuration: float` |
| `BlackHoleMovement` | 慢速移动 + 周围拉扯敌人 | `PullRadius: float`<br>`PullForce: float` |
| `PulseMovement` | 变速飞行（加速 / 减速阶段） | `AccelPhase: float`<br>`DecelStart: float` |
| `RollingMovement` | 滚动（地面贴合），穿透敌方子弹 | `RollFriction: float` |

### 3.4 总览

**B+ 方案文档范围**：只落定 3.1 Phase-M1 的 2 个类（跑通管线即可）；其他留给未来 Batch。

---

## 4. 参数覆盖机制（MovementParamApplier）

### 4.1 需求

CSV 的 `MovementParams` 列用分号串表达一组 `Field:Value`：
```
Amplitude:5;Frequency:3;TurnRate:2.5
```

运行时需要把这些值写入 Movement 组件对应字段。

### 4.2 实现方案

```csharp
namespace ProjectArk.Combat
{
    public static class MovementParamApplier
    {
        /// <summary>
        /// 把分号串参数写入 movement 组件的对应字段。
        /// 字段必须是 public 或带 [SerializeField]。
        /// </summary>
        public static void Apply(IProjectileMovement movement, string semicolonStr)
        {
            if (string.IsNullOrWhiteSpace(semicolonStr)) return;
            if (movement == null) return;

            var type = movement.GetType();
            foreach (var pair in semicolonStr.Split(';'))
            {
                if (string.IsNullOrWhiteSpace(pair)) continue;
                var parts = pair.Split(':');
                if (parts.Length != 2)
                {
                    Debug.LogError($"[MovementParamApplier] Invalid pair '{pair}' in '{semicolonStr}'");
                    continue;
                }

                string fieldName = parts[0].Trim();
                string rawValue = parts[1].Trim();

                // 反射查找字段（包括 private + [SerializeField]）
                var field = type.GetField(fieldName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field == null)
                {
                    Debug.LogError($"[MovementParamApplier] Field '{fieldName}' not found on {type.Name}");
                    continue;
                }

                // 尝试转换类型
                if (TryConvert(rawValue, field.FieldType, out var converted))
                {
                    field.SetValue(movement, converted);
                }
                else
                {
                    Debug.LogError($"[MovementParamApplier] Cannot convert '{rawValue}' to {field.FieldType.Name} for field '{fieldName}'");
                }
            }
        }

        private static bool TryConvert(string raw, Type targetType, out object value)
        {
            value = null;
            try
            {
                if (targetType == typeof(float))
                {
                    if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var f))
                    { value = f; return true; }
                }
                else if (targetType == typeof(int))
                {
                    if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
                    { value = i; return true; }
                }
                else if (targetType == typeof(bool))
                {
                    if (bool.TryParse(raw, out var b))
                    { value = b; return true; }
                }
                else if (targetType.IsEnum)
                {
                    value = Enum.Parse(targetType, raw, ignoreCase: true);
                    return value != null;
                }
            }
            catch { /* fall through */ }
            return false;
        }
    }
}
```

### 4.3 支持的字段类型

| 类型 | 示例 |
|---|---|
| `float` | `Amplitude:5` |
| `int` | `MaxBounces:3` |
| `bool` | `RequiresTarget:true` |
| `enum` | `TargetMode:ClosestEnemy` |

**不支持**：`Vector2/3` / `Color` / 引用类型。如需这些，考虑扩展成多个 float 列（如 `Direction_X:1;Direction_Y:0`）。

### 4.4 性能考量

- 反射调用在**每次 Spawn 时执行一次**，不在 Update 里
- Spawn 本身是低频事件（玩家射击频率 ~5Hz，同帧最多 5-10 次 spawn）
- 实测 float field SetValue 约 1-2μs，50 次 spawn = 0.1ms，可忽略

**若未来性能敏感**（大量粒子发射器），可升级为**表达式树编译后缓存**，把反射成本摊到首次使用。**不纳入 B+ 方案**。

---

## 5. 参数协议表（CSV ↔ Movement 字段映射）

本表是策划查询手册，也是 Importer 与 Exporter 的**数据契约**。

### 5.1 通用参数（所有 Movement 均可覆盖）

| CSV Field | Movement Field | 类型 | 默认值 | 说明 |
|---|---|---|---|---|
| `SpeedMultiplier` | `_speedMultiplier` | float | 1.0 | 覆盖 `ProjectileContext.BaseSpeed` |

### 5.2 `TrackingMovement` 参数

| CSV Field | 类型 | 默认值 | 说明 |
|---|---|---|---|
| `TurnRate` | float | 180 | 每秒最大转向角度（度） |
| `AcquisitionRange` | float | 20 | 感知最近敌人的半径 |
| `LoseTargetBehavior` | enum | `KeepDirection` | 目标丢失时：`KeepDirection` / `Straight` / `Stop` |

### 5.3 `SerpentineMovement` 参数

| CSV Field | 类型 | 默认值 | 说明 |
|---|---|---|---|
| `Amplitude` | float | 2 | 正弦波振幅（米） |
| `Frequency` | float | 3 | 每秒摆动周期数 |
| `PhaseOffset` | float | 0 | 初始相位偏移（弧度） |

### 5.4 `BoomerangMovement` 参数

| CSV Field | 类型 | 默认值 | 说明 |
|---|---|---|---|
| `MaxDistance` | float | 10 | 飞出的最大距离 |
| `ReturnAcceleration` | float | 15 | 返回阶段的加速度 |
| `ReturnHomingStrength` | float | 5 | 返回时追踪施法者的力度 |

### 5.5 `GravityMovement` 参数

| CSV Field | 类型 | 默认值 | 说明 |
|---|---|---|---|
| `Gravity` | float | 9.8 | 重力加速度 |
| `InitialUpSpeed` | float | 5 | 初始向上速度 |

> 更多 Movement 的参数表将在实施时追加。**每次新增 Movement 实现，必须更新本表。**

---

## 6. Prefab 组织约定

### 6.1 模板 Prefab 规则

**每个 Movement 对应一个"模板 Prefab"**，挂好 Rigidbody2D + Collider2D + Projectile + 该 Movement 组件：

```
Assets/_Data/StarChart/Prefabs/Projectiles/
├── Projectile_Straight.prefab      ← 无 Movement 组件（或挂 StraightMovement 占位）
├── Projectile_Tracking.prefab      ← 挂 TrackingMovement
├── Projectile_Serpentine.prefab    ← 挂 SerpentineMovement
├── Projectile_Boomerang.prefab     ← 挂 BoomerangMovement
├── Projectile_Gravity.prefab       ← 挂 GravityMovement
└── ...
```

### 6.2 多部件共享 Prefab

**一个 Prefab 可以被多个 Core 共享**，通过 `MovementParams` 产生数值差异：

```csv
ID,    InternalName,      ProjectilePrefab,       MovementParams
C010,  Core_SnakeSmall,   Projectile_Serpentine,  Amplitude:2;Frequency:5
C011,  Core_SnakeBig,     Projectile_Serpentine,  Amplitude:6;Frequency:1
C012,  Core_SnakeFast,    Projectile_Serpentine,  Amplitude:3;Frequency:8
```

三个 Core 共享 `Projectile_Serpentine.prefab`，但视觉/行为差异很大。

### 6.3 VFX / 音效分离

模板 Prefab **不绑定特定 VFX 或音效**。MuzzleFlash / Impact VFX / FireSound 通过 `StarCoreSO` 字段注入，发射时覆盖。

---

## 7. 对象池交互

### 7.1 回收清单（强制）

每个 Movement 实现的 `OnReturnToPool()` 必须：

1. ✅ 重置累积的状态变量（相位、计时器、回旋阶段标志等）
2. ✅ 清空目标引用（追踪的敌人、锁链的拖拽对象）
3. ✅ 不销毁自身组件（Prefab 上的组件必须保留）
4. ✅ 不修改 `Rigidbody2D.velocity`（Projectile 会在下次 Spawn 重设）

**反例（禁止）**：
```csharp
public void OnReturnToPool()
{
    // ❌ 修改 transform（应由 Projectile 重设）
    transform.localScale = Vector3.one;

    // ❌ 销毁自身（Prefab 组件不能销毁）
    Destroy(this);

    // ❌ 遗漏状态（下次 Spawn 时带着旧相位）
    // _phase 未重置
}
```

### 7.2 Movement 参数覆盖的重置

`MovementParamApplier.Apply` 在每次 Spawn 都会重写字段——这本身就是"重置"。**不需要在 OnReturnToPool 中再额外清理**。

但注意：**同一 Movement 若被不同 Core 引用，且一个 Core 覆盖了字段、另一个没有**，后者会继承前者的值。

**防御**：要求 Movement 实现在 `OnSpawn()` 开头**先恢复 Prefab 默认值**（通过保存一份初始 snapshot）。

**推荐模式**：
```csharp
public class SerpentineMovement : MonoBehaviour, IProjectileMovement
{
    [SerializeField] private float _amplitude = 2f;
    [SerializeField] private float _frequency = 3f;

    private float _defaultAmplitude;
    private float _defaultFrequency;

    private void Awake()
    {
        _defaultAmplitude = _amplitude;
        _defaultFrequency = _frequency;
    }

    public void OnSpawn(ProjectileContext ctx)
    {
        // 先还原 Prefab 默认值，再被 MovementParamApplier 覆盖
        _amplitude = _defaultAmplitude;
        _frequency = _defaultFrequency;

        _phase = 0f;  // 重置运行时状态
    }

    // ...OnUpdate / OnReturnToPool
}
```

这条约定会写入实施时的代码模板。

---

## 8. 与现有架构的关系

### 8.1 `IProjectileModifier` vs `IProjectileMovement`

| | Modifier | Movement |
|---|---|---|
| 职责 | 命中时的**额外效果** | 飞行过程中的**位置变化** |
| 挂载 | PrismSO.ProjectileModifierPrefab（动态 Instantiate） | Projectile Prefab 上预挂（池内复用） |
| 数量 | 每次 Spawn 可挂 N 个（多棱镜叠加） | 每 Prefab 至多 1 个 |
| 例子 | 穿透 / 冰冻 / 追加伤害 / 雷电链 | 直线 / 追踪 / 蛇形 / 回旋 |

**它们互相正交，可以同时存在**：一颗"蛇形穿透冰冻弹"= SerpentineMovement + PenetrateModifier + FrostModifier。

### 8.2 `BoomerangModifier`（现有代码）要迁移吗？

现 `Assets/Scripts/Combat/Projectile/BoomerangModifier.cs` 实际上是"Movement"性质（控制飞行轨迹），但被归到 Modifier 体系。**这是历史包袱**。

**迁移方案**（B+ 方案后续 Batch）：
1. 创建 `BoomerangMovement : IProjectileMovement`
2. 把 `BoomerangModifier` 逻辑挪过去
3. 删除旧的 `BoomerangModifier`（遵循"先删旧路径，再加新逻辑"原则）
4. `AnomalyCore_Boomerang.asset` 的 `AnomalyModifierPrefab` 字段改为引用新 Prefab

**不纳入 B+ 方案本次范围**，但写入 `StarChart_DataPipeline_Plan.md` 的"Phase 7 清理期"阶段。

---

## 9. 验收（Phase-M1 MVP 完成标志）

| 验收项 | 方法 |
|---|---|
| `IProjectileMovement` 接口存在 | 新文件 `Assets/Scripts/Combat/Projectile/Movement/IProjectileMovement.cs` |
| `StraightMovement` 跑通 | `Projectile_Straight.prefab` 上挂，Play Mode 直线飞，无报错 |
| `TrackingMovement` 跑通 | 把 `ShebaCore_MachineGun.asset` 的 Archetype 改为 Tracking，Play Mode 子弹追踪敌人 |
| `MovementParamApplier` 可用 | CSV 的 `MovementParams=TurnRate:90` 生效，子弹转向速度减半 |
| `OnReturnToPool` 正确 | 重复发射 100 次，Memory Profiler 无泄漏、视觉无残留 |
| 和 Modifier 兼容 | 同时装 Tracking Core + Penetrate Prism，子弹追踪且穿透 |

---

## 10. 风险与防御

| 风险 | 防御 |
|---|---|
| 反射性能陷阱 | Spawn 频率低，一次性成本可接受。若未来发射密集型武器（机炮），可缓存 FieldInfo |
| 字段名打错 | Importer 解析 MovementParams 时检查字段是否存在；人肉 CI 跑 Import 看 Console |
| OnSpawn 前读 Overrides 顺序错乱 | 统一在 `Projectile.Initialize` 末尾调用 `Apply → movement.OnSpawn`（本文档 §2.2） |
| 多 Core 共享 Prefab 的字段污染 | Movement Awake 时缓存默认值，OnSpawn 开头恢复（§7.2） |
| 循环依赖 | `IProjectileMovement` 放在 `ProjectArk.Combat.Projectile` 命名空间，不依赖 StarChart；Core 通过 CSV 字符串引用，不编译期耦合 |

---

*本文档是 H3 方案的具体落地细节。实施步骤见 `StarChart_DataPipeline_Plan.md`。*
