# HyperWind — 架构速写

> **范围**：`Docs/1_GameDesign/HyperWind_MechanicsBrief.md` 的首个 MVP 切片 D' · 气旋竞技场增强版。  
> **状态**：切片 D' MVP 已完成首轮实现与人工验证；本文同步记录最终 MVP 架构事实与后续替换缝。  

> **原则**：先把 5-10 分钟可玩切片跑起来；程序化表现只做 `View` / preview，不反向绑死 gameplay。

---

## 1. 切片目标

切片 D' 要同时验证两件事：

1. **风感成立**：飞船移动与子弹弹道都被风向场调制，但不产生失控挫败。
2. **地面气旋成立**：子弹被吸入、绕飞、强化、方向继承释放，形成“延迟齐射”的正向战术与“敌弹反噬”的负向博弈。

本轮以 `HyperWind_MechanicsBrief.md` §10 为准：

- G1 风向场
- G2 风相位脉动
- G3 三阶段预警
- M1 风矢叠加
- S1 风偏弹道
- L8 地面气旋（a/b/c/d + 方向继承 + ×2.5 上限）
- E1 风骑兵

---

## 2. 模块边界

### 2.1 新增目录

| 路径 | 用途 |
|------|------|
| `Assets/Scripts/Core/HyperWind/` | 首批共享风场接口、风相位和最小风场服务。放在 `Core` assembly，保证 `Ship` / `Combat` / `Level` 都可引用。 |
| `Docs/2_TechnicalDesign/HyperWind/` | HyperWind 技术速写与后续 Spec。 |

### 2.2 为什么先放 `Core/HyperWind`

`WindField` 是跨模块基础设施：

- `Ship` 需要采样它做 M1
- `Combat` 需要采样它做 S1 / L8 释放后二段偏折
- `Level` 需要布置风区和气旋生成器
- `Enemy` 需要读取风向做 E1 顺风冲撞

若首轮单独建 `ProjectArk.HyperWind` assembly，则需要修改多份 asmdef 引用；为降低 Batch 1 风险，先把接口和最小服务放进 `ProjectArk.Core` assembly。后续若 HyperWind 独立成长，再迁出为专用 assembly。

---

## 3. 脚本职责表（MVP 已落地）

| 脚本 | 唯一职责 | 类型 |
|------|----------|------|
| `WindSample` | 表示一次风场采样结果：方向、基础速度、相位倍率、最终风速 | 纯 C# struct |
| `IWindFieldService` | 给 Ship / Combat / Level / Enemy 提供统一采样接口 | interface |
| `IWindPhaseService` | 提供当前风相位、相位进度和风速倍率 | interface |
| `WindPhaseController` | 管理 G2/G3 的弱相位、预警、强相位节律 | `MonoBehaviour` service |
| `WindFieldManager` | 管理 G1 的最小矩形风区采样，并在 SceneView 画调试箭头 | `MonoBehaviour` service |
| `ShipMotor` 内部风层 | 接入 M1：每个物理帧先移除旧风速，玩家速度 `ClampSpeed()` 后再叠加环境风速，避免风速被上限吃掉 | existing runtime component |
| `Projectile` / `EnemyProjectile` 内部风偏层 | 接入 S1：每帧先移除旧风偏，原有 projectile / modifier 更新后再叠加 capped drift velocity；Laser / EchoWave MVP 不接入 | existing runtime component |
| `GroundCyclone` | L8 生命周期、捕获、绕飞、容量、方向继承释放、速度/伤害强化；暴露捕获/释放统计事件给测试工具 | `MonoBehaviour` gameplay core |
| `GroundCycloneSpawner` | 在竞技场中心 lane 每 10s 生成 1-2 个运行时气旋；无 prefab 时自动补 `GroundCycloneView` | `MonoBehaviour` arena tool |
| `GroundCycloneView` | L8 程序化视觉：按 `GroundCyclone` 的 phase / progress / radii 渲染 warning ring、vortex、orbit ring、burst ring，可替换正式美术 | `MonoBehaviour` view |
| `WindRiderWindAssist` | E1 风骑兵顺风辅助层；复用 `ChargeRusherBrain` / `ChargeState` 的可读冲锋骨架，只额外叠加顺风速度与 tint | `MonoBehaviour` enemy modifier |
| `HyperWindArenaTestDirector` | 测试场景专用导演：池化玩家弹/敌弹、自动烟测、捕获/释放计数；不进入正式 gameplay 主链 | `MonoBehaviour` test-only director |
| `HyperWindArenaSceneConfigurator` | 显式 Editor 菜单配置测试场景引用、PoolManager、director、projectile prefab；不是运行时依赖 | Editor tool |


---

## 4. 驱动关系

```text
WindPhaseController
    └─ registers IWindPhaseService

WindFieldManager
    ├─ registers IWindFieldService
    ├─ reads IWindPhaseService multiplier
    └─ returns WindSample at world position

ShipMotor / Projectile / Enemy / GroundCyclone
    └─ sample IWindFieldService through ServiceLocator.TryGet

GroundCyclone
    ├─ captures projectile runtime bodies
    ├─ owns captured orbit state
    ├─ releases by last player fire direction
    ├─ exposes capture/release counters and events for test instrumentation
    ├─ configures explicit PlayerProjectile + Default capture layers on Awake for runtime AddComponent path
    └─ sends visual intent to GroundCycloneView

GroundCycloneView
    └─ procedural preview only; replaceable by prefab / shader / VFX later

HyperWindArenaTestDirector / HyperWindArenaSceneConfigurator
    ├─ test-scene only wiring and smoke validation
    └─ must not become shipping gameplay owner
```


---

## 5. 数据归属

### 5.1 Authoring 数据

首轮不新建 SO，先用场景中 `WindFieldManager` / `WindPhaseController` 的 serialized fields 调参：

- 风区矩形
- 风向
- 基础风速
- 相位周期
- 强相位时长
- 预警提前量
- 弱/强相位倍率

后续若切片成立，再抽成 `HyperWindFieldProfileSO` / `WindPhaseProfileSO`。

### 5.2 运行时状态

- 当前相位时间与状态归 `WindPhaseController`
- 风区采样归 `WindFieldManager`
- 被捕获子弹、公转角、圈数、强化倍率归 `GroundCyclone`
- `GroundCyclone.TotalCapturedCount` / `TotalReleasedCount` 与静态捕获/释放事件仅服务测试仪表化，不作为正式战斗平衡输入
- 程序化视觉生成缓存归 `GroundCycloneView`，不得被 gameplay 依赖
- `HyperWindArenaTestDirector` 的 fired / captured / released counters 是测试场景状态，不能被正式关卡逻辑依赖


---

## 6. 程序化表现立项检查卡

- **定位**：MVP 阶段临时占位，允许后续演化成 shipping 路径。
- **体验目标**：风场让移动/射击明显偏移；气旋让子弹绕飞、存弹、爆发一眼可读。
- **核心输入**：风向、风速、相位倍率、气旋半径、轨道半径、容量、吸吮时长、释放方向、强化倍率。
- **替换缝**：`WindField` / `GroundCyclone` 提供 gameplay 数据；程序化表现只在 `View` 层消费这些数据。
- **资产缺失策略**：MVP 允许程序化 fallback；关键可见性必须有诊断，禁止 silent no-op。
- **通过标准**：替换气旋视觉时不修改捕获、绕飞、释放、伤害强化逻辑。

---

## 7. 首轮验收结果

切片 D' MVP 已完成首轮实现与人工验证：

1. 场景中添加 `WindPhaseController` + `WindFieldManager` 后，可通过 `IWindFieldService.Sample()` 在不同位置得到不同风向/风速。
2. `WindPhaseController` 能循环进入弱相位、预警相位、强相位，并输出风速倍率。
3. `WindFieldManager` 能绘制 SceneView 调试箭头，策划不用看代码也能确认左右风带方向。
4. 没有新增运行时 `FindObjectOfType` / `FindAnyObjectByType` 依赖；服务通过 `ServiceLocator` 注册，测试场景引用通过显式 Editor 配置入口完成。
5. `ShipMotor` / `Projectile` / `EnemyProjectile` 已接入独立风速层，不让风速被原有速度上限或 modifier 更新吞掉。
6. `GroundCyclone` 完成 Spawn → Draw → Burst → Finished、容量 15、方向继承释放、速度/伤害 ×2.5 上限。
7. 自动烟测曾确认 `playerFired=66`、`enemyFired=7`、`cycloneCaptured=3`、`cycloneReleased=3`。
8. 用户本地 Play Mode 人工验证通过：风感、气旋可读性、延迟齐射、敌弹反噬、E1 风骑兵压力均可接受，可进入下一步迭代。


---

## 8. 明确暂不做

- 不实现 G6/G7/G8。
- 不实现风眼、风门、风帆机关。
- 不让 Laser / EchoWave 进入气旋捕获。
- 不做正式美术资产。
- 不把程序化气旋视觉做成 gameplay 依赖。
- 不在 Batch 1 修改 `Ship.prefab` 或 `SampleScene.unity`。

---

## 9. Closeout 约束

- `HyperWind_SliceD_Test.unity` 是实验/验证场景，不替代正式 `SampleScene.unity` 或关卡模块的 EncounterSO 管线。
- `HyperWindArenaTestDirector` 与 `HyperWindArenaSceneConfigurator` 是测试工具，不应成为 shipping 玩法 owner。
- `GroundCycloneView` 仍是程序化预览层；正式视觉可替换，但不能让视觉反向驱动捕获/释放/伤害逻辑。
- 当前仍没有 `EnemyProjectile` layer；气旋 MVP 继续显式使用 `PlayerProjectile + Default` mask，并通过 `ICycloneCaptureTarget` 做二次过滤。若后续新增 `EnemyProjectile` layer，必须同步检查 Physics2D 碰撞矩阵。
- Console 中 BoostTrail / SRP Batcher / AudioListener 等既有项不是 HyperWind 阻塞项，但在合入正式演示场景前应另开任务处理。

