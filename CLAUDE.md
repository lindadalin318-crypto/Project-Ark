# Project Ark — 静默方舟 (Project Sheba)

这是一个 **Top-Down 2D (俯视角) 动作冒险游戏**，融合了 **银河恶魔城 (Metroidvania)** 的探索结构与 **类魂 (Soulslike)** 的叙事氛围。  
核心体验是驾驶飞船“金丝雀号”在手工打造的异星关卡中探索，通过组合“星图”部件（类似 Roguelike 的随机池，但用于 RPG 式的永久构建）来定制武器。Unity 6 + URP 2D + New Input System。

## 沟通方式

- 讨论/沟通用中文
- 代码中：变量名、类名、方法名用英文
- 公共 API 用英文 XML doc，内部注释可用中文
- Commit message 用英文
- 当我的需求描述不够清晰时，请先确认以下三点再编写代码：
  1. **目标 (Goal)**：我们要实现什么游玩体验？（例如：这是一个让玩家感到“惯性滑行”的移动手感，还是一个“瞬间爆发”的射击逻辑？）
  2. **范围 (Scope)**：涉及哪些脚本或系统？是否需要创建新的 `ScriptableObject` 数据资产？
  3. **架构约束 (Architecture)**：是否需要解耦？是否涉及对象池？是否需要暴露给策划配置？
- 确认后再执行，不要猜测意图。

## 核心模块

- **3C (Character/Camera/Control)**: 飞船控制 (`ShipMotor`), 摄像机跟随 (`Cinemachine`), 交互系统.
- **战斗系统 (Combat)**: 星图编织系统 (`StarChart`), 武器发射 (`WeaponSystem`), 热量管理 (`HeatSystem`).
- **敌人 AI (Enemy)**: 听觉仇恨系统, 行为树/状态机 (`EnemyController`).
- **数据资产 (Data)**: 所有数值配置 (`Stats`).
- **关卡系统 (Level)**: 锚点存档机制 (`CheckpointSystem`), 地图锁钥逻辑 (`LockKeySystem`).

## 技术栈

- Unity 6000.3.7f1, URP 2D (com.unity.render-pipelines.universal 17.3.0)
- New Input System (com.unity.inputsystem 1.18.0)
- C# / .NET (Unity 默认)
- 2D Sprite / Animation / Tilemap 全套

## 架构原则 (必读)

### 1. 数据驱动

- 所有游戏数值必须放在 ScriptableObject 中，严禁 hardcode
- 配置 SO 存放于 Assets/_Data/ 对应子目录

### 2. 解耦与模块化

- 单一职责：每个 MonoBehaviour 只负责一件事
- 事件驱动：系统间通过 C# event / System.Action 通信，禁止跨系统直接引用
- Assembly Definition 划分模块边界

### 3. 可扩展性

- 星图系统 (The Loom) 的部件用策略模式/装饰器模式，支持热插拔
- 新增内容（武器、敌人、部件）只需添加新的 SO + 实现类，不修改核心逻辑

### 4. 性能

- 所有运行时生成的对象（子弹、特效、飘字）必须使用对象池
- 战斗中严禁 Instantiate / Destroy

## 代码规范

### 命名

- 根命名空间: `ProjectArk`
- 子命名空间按模块: `ProjectArk.Ship`, `ProjectArk.Heat`, `ProjectArk.Combat`, `ProjectArk.Core`
- 类名: PascalCase (ShipMotor, HeatSystem)
- 方法/属性: PascalCase
- 私有字段: _camelCase (带下划线前缀)
- 常量: UPPER_SNAKE_CASE
- 事件命名: On + 动词过去分词 (OnOverheated, OnDamageTaken)

### 文件组织

- 一个文件一个类（内部类/小型辅助类除外）
- 文件名与类名一致

### Unity特性

- 使用 `[SerializeField]` 暴露私有字段给编辑器，而不是 `public`。
- 使用 `[RequireComponent(typeof(...))]` 确保依赖存在。
- 使用 `Mathf.Approximately` 比较浮点数。

## 项目结构

```
Assets/
├── _Data/               # ScriptableObject 资产
│   ├── Ship/            # 飞船配置 SO
│   ├── Weapons/         # 武器配置 SO
│   └── Enemies/         # 敌人配置 SO
├── _Prefabs/            # 所有 Prefab
│   ├── Ship/
│   ├── Projectiles/
│   └── Effects/
├── Scripts/
│   ├── Core/            # 核心框架（事件总线、对象池、单例基类等）
│   ├── Ship/            # 飞船相关（ShipMotor, InputHandler 等）
│   ├── Heat/            # 热量系统
│   ├── Combat/          # 战斗/武器系统
│   └── UI/              # UI 相关
├── Art/                 # 美术资源（Sprites, Animations）
├── Audio/               # 音频
├── Scenes/
├── Settings/            # URP 等设置
└── Input/               # Input Action Assets
```

## 当前里程碑

### MS1: 飞船基础移动 + Twin-Stick 瞄准 [代码已完成，待 Unity 验证]

- [x] 项目结构搭建（文件夹、Assembly Definitions: Core/Ship/UI）
- [x] ShipStatsSO（ScriptableObject 飞船配置: 速度/加速/减速/旋转速）
- [x] ShipActions.inputactions（Move/AimPosition/AimStick + 预留 Fire/Dash/Interact/Pause）
- [x] InputHandler（读取 New Input System，鼠标/手柄热切换）
- [x] ShipMotor（Rigidbody2D 直接 velocity 控制，加速>减速不对称）
- [x] ShipAiming（MoveTowardsAngle 匀速旋转，LateUpdate）
- [ ] 在 Unity 编辑器中组装 Ship Prefab 并验证手感
- [ ] 创建 DefaultShipStats.asset（在 _Data/Ship/）

### MS2: 热量系统 + 战斗基础 [待定]

- HeatSystem（热量产生/消耗/过热）
- WeaponSystem（基础射击）
- 事件桥接（热量事件 → 武器/UI）
- 调试 UI（显示热量条）

## 常见任务

### A. 新增飞船/武器参数

1. 修改对应的 `*StatsSO.cs` 脚本。
2. 在 `ShipMotor` 或 `WeaponSystem` 中引用该数据。
3. **不要**直接修改 MonoBehaviour 中的变量默认值。

### B. 创建新的星图部件 (Star Chart Component)

1. 确定类型：`StarCore` (星核), `Prism` (棱镜), 或 `Sail` (光帆), `Satellite` (伴星)。
2. 新建类继承自基类 (e.g., `BaseStarCore`)。
3. 实现 `Execute()` 或 `Modify()` 接口。
4. 创建对应的 `ScriptableObject` 资产文件。

### C. 敌人 AI 逻辑

1. AI 逻辑应基于状态模式 (State Pattern)：`Idle`, `Patrol`, `Chase`, `Attack`。
2. 索敌逻辑应包含**听觉检测** (监听 `OnPlayerFire` 事件)。

