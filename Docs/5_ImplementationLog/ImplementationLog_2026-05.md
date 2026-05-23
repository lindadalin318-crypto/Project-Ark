---

## Ship Art / VFX Minishoot 参考素材库整理 — 2026-05-18 22:10

- **新建文件 / 文件夹**
  - `Docs/7_Reference/ShipArtVFX_MinishootReference/`
  - `Docs/7_Reference/ShipArtVFX_MinishootReference/Batch_1_Normal/`
  - `Docs/7_Reference/ShipArtVFX_MinishootReference/Batch_2_Dodge/`
  - `Docs/7_Reference/ShipArtVFX_MinishootReference/Batch_3_Boost/`
  - `Docs/7_Reference/ShipArtVFX_MinishootReference/Batch_4_Fire_Hit/`
  - `Docs/7_Reference/ShipArtVFX_MinishootReference/Batch_5_Weaving/`
  - `Docs/7_Reference/ShipArtVFX_MinishootReference/Batch_6_Overheat/`
  - `Docs/7_Reference/ShipArtVFX_MinishootReference/Batch_7_Unity_Material_Shader/`
- **修改文件**
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：从 `Minishoot/DevXUnity` 解包素材中提取与飞船美术管线相关的参考素材，并按 `Ship_ArtVFX_Workflow.md` 的批次结构整理为 `Batch 1-7`。素材命名统一映射到计划中的 `spr_ship_canary_*`、`tex_ship_canary_*`、`mat_ship_canary_*`、`shader_ship_*` 口径，同时保留 `Source/source_*` 与 `source_map_batch7.csv` 方便追溯原始 Minishoot 文件。
- **目的**：让后续制作金丝雀号主船体、Dodge、Boost、Fire/Hit、Weaving、Overheat、Material/Shader 时，可以直接打开对应批次看到参考资产类型和命名目标，降低“我到底该产出哪张图”的理解成本。
- **技术**：文件级参考素材整理，不导入 Unity 运行链路，不创建 `.meta` 文件，不修改现役 `Ship.prefab` / `BoostTrailRoot.prefab` / `AssetRegistry`。分类遵循当前美术工作流的 `Batch 1 Normal → Batch 7 Unity/Material/Shader` 顺序，并继续保持参考素材与正式 Project Ark 资产隔离。

---

## Ship Art / VFX 工作流细化为新手资产生产计划 — 2026-05-18 21:53

- **修改文件**
  - `Docs/3_WorkflowsAndRules/Ship/Ship_ArtVFX_Workflow.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：将飞船美术工作流从阶段型路线图重构为更细粒度的资产生产清单。新增统一图片规格、像素尺寸、DPI 说明、透明背景、边距、命名规则、Unity Import Settings；并把主飞船 Normal、Dodge、Boost、Fire、Hit、Weaving、Overheat 拆到单张资产级别，明确每张 `Albedo`、`Emission`、`Mask`、可选 `Normal Map` 的用途、制作方式与验收标准。
- **目的**：让没有美术经验的人也能按步骤生产金丝雀号飞船资产，避免停留在“做 Sprite / 做 Shader / 做 VFX”的粗粒度描述；同时把 Dodge state 等具体状态提前拆清楚，形成可逐批执行的美术管线。
- **技术**：文档结构从 `Step 0-15` 改为 `统一规格 → 主飞船 Sprite → Dodge → Boost → Fire → Hit → Weaving → Overheat → Unity 接入 → Material/Shader → VFX Prefab → 测试验收`。继续遵守 `Ship/VFX` 现役主链、Prefab authority、Debug 不接管正式链、对象池复位与 AssetRegistry 同步约束。

---

## Ship Art / VFX 工作流文档创建 — 2026-05-18 17:01

- **新建文件**
  - `Docs/3_WorkflowsAndRules/Ship/Ship_ArtVFX_Workflow.md`
- **修改文件**
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：新增飞船美术与特效工作流文档，系统拆解金丝雀号从视觉目标、状态表、分层 Sprite、AI 生图、Unity 导入、Prefab 接入、Material / Shader、VFX Prefab、Runtime 状态驱动、Tween / Animator、Bloom / PostProcess、测试场景到资产注册固化的完整步骤。每个步骤均明确需要产出、推荐生产方式、验收标准和常见风险。
- **目的**：为后续逐步跑通飞船美术模块提供统一生产路线，避免只依赖临时讨论或单次 AI 输出；同时确保新工作流对齐现役 `Ship/VFX` 主链，不替代 `ShipVFX_CanonicalSpec` 与 `ShipVFX_AssetRegistry`。
- **技术**：文档工作流沉淀；结合 `Minishoot` 的清晰量产管线与 `Galactic Glitch` 的多层飞船 / Shader / VFX 状态表现路线，并遵守 Project Ark 的 `Ship/VFX` authority、对象池复位、Debug 不接管正式链、AssetRegistry 同步规则。

---

## HyperWind 切片 D' Plan Closeout — 2026-05-17 23:20

- **修改文件**
  - `Docs/2_TechnicalDesign/HyperWind/HyperWind_ArchBrief.md`
  - `Docs/0_Plan/ongoing/2026-05-16-hyperwind-slice-d-implementation-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：完成 HyperWind 切片 D' ongoing plan 收尾。将 `HyperWind_ArchBrief.md` 从 Batch 0 开工速写更新为 MVP closeout 架构事实，补充 `HyperWindArenaTestDirector`、`HyperWindArenaSceneConfigurator`、`GroundCyclone` 捕获/释放统计事件、运行时 capture layer 初始化、测试工具 owner 边界、程序化视觉替换缝与 closeout 约束。关闭 ongoing plan 中剩余的 `Step 0.3`、`Step 8.1`、`Step 8.3`，并把 Known risks 改写为 closeout 后的 follow-ups。
- **目的**：将已通过人工验证的切片 D' 从“进行中实现计划”转为“已完成 MVP 记录”，方便后续进入下一批扩展或正式关卡集成时快速理解现役事实与遗留风险。
- **技术**：文档 closeout，不新增运行时代码。`Step 8.1` 按 HyperWind-specific validation 口径关闭：HyperWind 编译与阻塞错误为 0；BoostTrail / SRP Batcher / AudioListener 属于非 HyperWind 既有 cleanup 项，单独留作 follow-up。

---

## HyperWind 切片 D' 首轮人工验证通过 — 2026-05-17 23:17


- **修改文件**
  - `Docs/0_Plan/ongoing/2026-05-16-hyperwind-slice-d-implementation-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：记录 HyperWind 切片 D' · 气旋竞技场增强版首轮人工验证结果。用户完成本地 Play Mode 验证并确认当前风感、气旋视觉/捕获释放、延迟齐射、敌弹反噬与 E1 风骑兵 MVP 均可接受，`Task 7.4` 与 `Task 8.2` 标记为完成。
- **目的**：把“可玩性验证通过”从聊天上下文沉淀到 ongoing plan，作为后续继续迭代或进入下一批扩展的决策依据。
- **技术**：不新增运行时代码；仅更新计划与日志状态。保留 `Task 8.1` 未完全关闭，因为 Console 仍有既有 BoostTrail / SRP Batcher / AudioListener 等非 HyperWind 阻塞项。

---

## HyperWind 切片 D' Log 复查与气旋 Layer 修复 — 2026-05-17 23:04


- **修改文件**
  - `Assets/Scripts/Combat/HyperWind/GroundCyclone.cs`
  - `Docs/0_Plan/ongoing/2026-05-16-hyperwind-slice-d-implementation-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：复查 Unity Console 后确认自动烟测链路已经跑通一次：`HyperWindArenaTestDirector` 输出 `playerFired=66, enemyFired=7, cycloneCaptured=3, cycloneReleased=3`，说明玩家弹/敌弹生成、气旋捕获与释放事件链路成立。随后修复 `GroundCyclone` 多次提示 `Capture layer mask is empty` 的问题：运行时由 `GroundCycloneSpawner` + `AddComponent<GroundCyclone>()` 创建的气旋不会可靠执行 `Reset()`，导致 `_captureLayers` 为 0；现在 `Awake()` 会调用 `EnsureCaptureLayersConfigured()`，在运行时显式配置 `PlayerProjectile + Default`，`Reset()` 也复用同一配置方法。
- **目的**：消除运行时气旋捕获层为空导致的重复 warning，避免后续误判 L8 玩法不稳定；同时保留当前项目没有 `EnemyProjectile` layer 的现实约束，继续用 `ICycloneCaptureTarget` 做二次过滤。
- **技术**：没有使用 `~0` 全层 mask；默认 mask 仍是显式 `LayerMask.GetMask("PlayerProjectile", "Default")`。新增 `ConfigureDefaultCaptureLayers()` 作为唯一默认层配置入口，`Awake()` 覆盖 runtime AddComponent 路径，`Reset()` 覆盖 Editor/Prefab 创建路径。验证：`read_lints` 为 0，Unity `refresh_unity` 后 C# 编译错误为 0。

---

## HyperWind 切片 D' Task 8 推进：自动烟测仪表化 — 2026-05-17 22:06


- **修改文件**
  - `Assets/Scripts/Combat/Enemy/HyperWindArenaTestDirector.cs`
  - `Assets/Scripts/Combat/HyperWind/GroundCyclone.cs`
  - `Docs/0_Plan/ongoing/2026-05-16-hyperwind-slice-d-implementation-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：为 HyperWind 竞技场补充自动烟测仪表化。`HyperWindArenaTestDirector` 新增自动烟测发射配置：Play Mode 后会从多条纵向 lane 自动向中区发射玩家弹，减少验证气旋吸弹时对人工输入的依赖；同时统计 `PlayerProjectilesFired`、`EnemyProjectilesFired`、`CycloneCapturedProjectiles`、`CycloneReleasedProjectiles`、`AutoSmokeElapsed` 等公开读数，并在烟测结束时输出摘要。`GroundCyclone` 新增 `TotalCapturedCount` / `TotalReleasedCount` 以及 `OnAnyProjectileCaptured` / `OnAnyProjectileReleased` 事件，供测试导演记录捕获与释放结果。
- **目的**：把 Task 8 的 Play Mode smoke test 从“看起来有对象生成”推进到“能客观回答气旋是否真的捕获/释放 projectile”，为之后手感调参与人工验收提供可复现诊断信号。
- **技术**：使用事件订阅而不是测试导演主动查找气旋；`OnEnable` / `OnDisable` 成对订阅/取消订阅，避免静态事件僵尸引用。新增公开只读属性供 Unity MCP 查询运行时读数。验证：新增脚本修改 `read_lints` 为 0，Unity `refresh_unity` 后 C# 编译错误为 0。当前 MCP Play Mode 可进入但一直报告 `is_changing=true` / `playmode_transition`，`AutoSmokeElapsed` 停在约 `0.02s`，因此自动烟测摘要未能通过 MCP 闭环；需在 Unity 本地 Game View 中再做人工运行验证。

---

## HyperWind 切片 D' Task 7 推进：竞技场试玩桥接 — 2026-05-17 21:34


- **新建文件**
  - `Assets/Scripts/Combat/Enemy/HyperWindArenaTestDirector.cs`
  - `Assets/Scripts/Combat/Editor/HyperWindArenaSceneConfigurator.cs`
  - `Assets/Scripts/Combat/Enemy/HyperWindArenaTestDirector.cs.meta`（Unity 自动生成）
  - `Assets/Scripts/Combat/Editor/HyperWindArenaSceneConfigurator.cs.meta`（Unity 自动生成）
- **修改文件**
  - `Assets/Scenes/HyperWind/HyperWind_SliceD_Test.unity`
  - `Assets/Scripts/Combat/Projectile/Projectile.cs`
  - `Docs/0_Plan/ongoing/2026-05-16-hyperwind-slice-d-implementation-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容**：为切片 D' 竞技场补齐可试玩桥接。新增 `HyperWindArenaTestDirector`，在测试场景中用池化玩家弹和敌弹制造持续弹幕流：玩家按住开火时从 `InputHandler` 读取瞄准方向发射 Matter projectile；敌方发射点按固定节奏轮流朝玩家发射 `EnemyProjectile`，用于验证气旋吸弹、方向继承齐射、敌弹反噬张力。新增 `HyperWindArenaSceneConfigurator` 显式 Editor 菜单 `ProjectArk > HyperWind > Configure Slice D Test Arena`，用于一键给 `HyperWind_SliceD_Test.unity` 补 `PoolManager`、竞技场导演、projectile prefab 引用、ship 引用，并重申 cyclone lane 每 10s 生成 1-2 个气旋。
- **目的**：把前面已经完成的 G1/M1/S1/L8/E1 从单点烟测推进到“一个房间里同时发生”的可试玩验证形态，让后续 Play Mode 直接回答风感、节律、气旋视觉、延迟齐射、敌弹反噬 5 个问题。
- **技术**：试玩桥接仍遵守池化原则，通过 `PoolManager.GetPool()` 预热并发射 projectile，不在战斗循环中直接 `Instantiate` 子弹；运行时不使用 `FindObjectOfType` / `FindAnyObjectByType`，玩家输入通过 `ServiceLocator.TryGet<InputHandler>()`，场景接线通过显式 Editor 菜单完成。Unity MCP 恢复后已执行配置菜单并保存 `HyperWind_SliceD_Test.unity`：`HyperWindRuntimeRoot` 挂有 `WindPhaseController` / `WindFieldManager` / `GroundCycloneSpawner` / `HyperWindArenaTestDirector`，场景中存在显式 `HyperWindPoolManager`。Play Mode 烟测生成 2 个 `GroundCyclone_Runtime`；新增代码编译错误为 0。针对对象池预热导致的 `Projectile_Matter(Clone)` 缺 TrailRenderer warning 刷屏，`Projectile` fallback 诊断改为每个 projectile prefab key 只报一次，保留诊断但不污染测试 Console。当前仍有既有 BoostTrail / SRP Batcher / AudioListener warning-error 项，未在本任务处理。


---

## HyperWind 切片 D' Task 6 完成：E1 风骑兵敌人 — 2026-05-17 12:22


- **新建文件**
  - `Assets/Scripts/Combat/Enemy/WindRiderWindAssist.cs`
  - `Assets/_Prefabs/Enemies/Enemy_WindRider.prefab`
  - `Assets/_Prefabs/Enemies/Enemy_WindRider.prefab.meta`（Unity 自动生成）
- **修改文件**
  - `Assets/Scenes/HyperWind/HyperWind_SliceD_Test.unity`
  - `Docs/0_Plan/ongoing/2026-05-16-hyperwind-slice-d-implementation-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：完成 E1 风骑兵 MVP。新增 `WindRiderWindAssist`，作为 `ChargeRusher` 的风场辅助层：保留现有 `ChargeRusherBrain` / `ChargeState` 的 Telegraph → Dash → Recovery 可读攻击模型，在敌人移动方向与当前风向对齐时叠加一层临时风助速度，并将 sprite tint 向青色偏移作为顺风借力提示。基于 `Enemy_ChargeRusher.prefab` 创建 `Enemy_WindRider.prefab` 并添加该 assist 组件；在 `HyperWind_SliceD_Test.unity` 中放置 4 个 `WindRider_Test_*` 敌人作为后续竞技场验证对象。
- **目的**：用最小成本完成“怪物-奇景融合”的第一版验证：风骑兵不是重写 AI，而是在现有可读冲锋敌人上叠加“顺风更危险”的生态关系。
- **技术**：`WindRiderWindAssist` 在 `LateUpdate()` 中作为 transient velocity layer 工作：EnemyBrain / EnemyEntity 在 `Update()` 写入基础移动速度后，assist 读取 `IWindFieldService` 并只在速度方向与风向超过阈值时追加风助。修复了一次速度层时序问题：不在 `LateUpdate()` 开头扣除上一帧 assist，避免 `EnemyEntity.MoveAtSpeed()` 已重写基础速度后被错误过度扣速。Play Mode 烟测：右侧风 `(3, 0)` 中，顺风速度 `(5, 0)` → `(8.75, 0)`，assist `(3.75, 0)`；逆风速度 `(-5, 0)` 保持 `(-5, 0)`，assist `(0, 0)`。Unity Console error 为 0。

---

## HyperWind 切片 D' Task 5 完成：L8 程序化气旋视觉层 — 2026-05-17 12:08


- **新建文件**
  - `Assets/Scripts/Combat/HyperWind/GroundCyclonePhase.cs`
  - `Assets/Scripts/Combat/HyperWind/GroundCycloneView.cs`
- **修改文件**
  - `Assets/Scripts/Combat/HyperWind/GroundCyclone.cs`
  - `Assets/Scripts/Combat/HyperWind/GroundCycloneSpawner.cs`
  - `Assets/Scenes/HyperWind/HyperWind_SliceD_Test.unity`
  - `Docs/0_Plan/ongoing/2026-05-16-hyperwind-slice-d-implementation-plan.md`
  - `Docs/2_TechnicalDesign/HyperWind/HyperWind_ArchBrief.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：完成 L8 的程序化视觉层。新增公开 `GroundCyclonePhase`，让视觉层读取气旋阶段；`GroundCyclone` 暴露 `CurrentPhase`、`PhaseProgress01`、`InfluenceRadius`、`OrbitRadius` 等视觉意图。新增 `GroundCycloneView`，使用 3 个 `LineRenderer` 绘制 Spawn 预警环/螺旋、Draw 期影响环/轨道环/稳定涡旋、Burst 期扩张爆发环。`GroundCycloneSpawner` 在无 prefab 的运行时气旋上自动添加 `GroundCycloneView`，测试场景 `HyperWindRuntimeRoot` 也已接入 spawner。
- **目的**：让已经跑通的 L8 gameplay core 变成玩家可读的可视装置，先用程序化表现验证“气旋在场、正在吸、即将爆”的信息层次，同时保持后续可替换为正式美术。
- **技术**：`GroundCycloneView` 只消费 `GroundCyclone` 的只读视觉意图，不参与捕获/释放/伤害逻辑；使用 `LineRenderer` 而非生成纹理，避免再次出现程序化贴图全透明问题，并加入材质/点数可见性诊断。Play Mode 烟测：测试场景中 spawner 生成 2 个 `GroundCyclone_Runtime`，均 `view=True`、`lineCount=3`、`visibleLineCount=3`，Console error 为 0。

---

## HyperWind 切片 D' Task 4 完成：L8 地面气旋 Gameplay Core — 2026-05-17 11:55


- **新建文件**
  - `Assets/Scripts/Combat/HyperWind/ICycloneCaptureTarget.cs`
  - `Assets/Scripts/Combat/HyperWind/CapturedProjectileState.cs`
  - `Assets/Scripts/Combat/HyperWind/GroundCyclone.cs`
  - `Assets/Scripts/Combat/HyperWind/GroundCycloneSpawner.cs`
- **修改文件**
  - `Assets/Scripts/Combat/Projectile/Projectile.cs`
  - `Assets/Scripts/Combat/Enemy/EnemyProjectile.cs`
  - `Docs/0_Plan/ongoing/2026-05-16-hyperwind-slice-d-implementation-plan.md`
  - `Docs/2_TechnicalDesign/HyperWind/HyperWind_ArchBrief.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：完成 L8 地面气旋 gameplay core。新增 `ICycloneCaptureTarget` 与 `CapturedProjectileState`，让玩家 `Projectile` 与敌方 `EnemyProjectile` 可被气旋捕获、暂停生命周期与碰撞、绕轨公转、按玩家最后射击方向释放，并按公转圈数获得速度/伤害倍率。新增 `GroundCyclone` 三阶段生命周期（Spawn / Draw / Burst）、容量上限 15、溢出丢弃、方向继承释放与倍率上限 ×2.5；新增 `GroundCycloneSpawner` 支持每批随机生成 1-2 个运行时气旋。
- **目的**：把切片 D' 的核心装置“吸子弹→绕飞→强化→方向继承齐射”从设计案落成可运行 gameplay，为下一步 Task 5 程序化视觉层提供稳定逻辑底座。
- **技术**：`GroundCyclone` 只依赖 `ICycloneCaptureTarget`，不依赖具体 projectile 实现或视觉表现；`Projectile` / `EnemyProjectile` 在回池和取出时都会重置气旋状态与 collider，避免对象池泄漏。Play Mode 烟测：玩家 Matter projectile 被捕获后释放，速度约 `(0, 11.08)`、伤害 `11.08`；敌方 projectile 被捕获后释放，速度约 `(11.08, 0)`、伤害 `11.08`。注意：项目当前没有 `EnemyProjectile` layer，`EnemyProjectile.prefab` 在 `Default` 层，因此 `GroundCyclone.Reset()` 暂时显式包含 `PlayerProjectile + Default`，并继续通过 `ICycloneCaptureTarget` 做二次过滤；后续若新增 `EnemyProjectile` layer，需同步检查 Physics2D 碰撞矩阵。

---

## HyperWind 切片 D' Task 3 完成：S1 子弹风偏与最后射击方向事件 — 2026-05-17 11:43


- **修改文件**
  - `Assets/Scripts/Combat/Projectile/Projectile.cs`
  - `Assets/Scripts/Combat/Enemy/EnemyProjectile.cs`
  - `Assets/Scripts/Core/CombatEvents.cs`
  - `Docs/0_Plan/ongoing/2026-05-16-hyperwind-slice-d-implementation-plan.md`
  - `Docs/2_TechnicalDesign/HyperWind/HyperWind_ArchBrief.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：完成 S1 子弹风偏基础接入。玩家 `Projectile` 与敌方 `EnemyProjectile` 均新增 HyperWind drift layer：每帧先移除上一帧 wind drift，再执行原有 projectile / modifier 更新，最后按 `IWindFieldService.Sample()` 累积一个有上限的 drift velocity，使弹道逐步弯曲而不是瞬移。新增 `CombatEvents.OnPlayerProjectileFired` / `RaisePlayerProjectileFired(position, direction)`，由 `Projectile.Initialize(...)` 在玩家物理子弹生成时广播，用于后续 L8 方向继承。
- **目的**：让切片 D' 的 S1 “风偏弹道”进入可验证状态，并为 L8 地面气旋的“玩家最后一次射击方向”建立稳定事件源。
- **技术**：玩家弹和敌弹都通过 `ServiceLocator.TryGet<IWindFieldService>()` 缓存风场服务；风偏状态在 `ReturnToPool()` / `OnReturnToPool()` 中重置，防止对象池状态泄漏。Play Mode 烟测结果：玩家子弹位于左区风 `(-3, 0)` 时速度从 `(0, 10)` 变为 `(-8, 10)`，敌方子弹位于右区风 `(3, 0)` 时速度从 `(0, 10)` 变为 `(8, 10)`；玩家子弹发射事件成功广播方向 `(0, 1)`。Unity Console error 为 0；测试场景仍有既有 `PoolManager` 缺失导致武器禁用提示，后续若要用正式武器射击验证，需要补场景 PoolManager 或轻量发射器。

---

## HyperWind 测试场景风区修正：从风墙改为可进入的外推风 — 2026-05-17 11:37


- **修改文件**
  - `Assets/Scenes/HyperWind/HyperWind_SliceD_Test.unity`
  - `Docs/0_Plan/ongoing/2026-05-16-hyperwind-slice-d-implementation-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：针对手动验证反馈“飞船根本进不去左右空间”，确认场景里没有额外 `Collider2D` 或 3D Collider 阻挡；问题来自上一轮为了肉眼可见而把左右风区调成了 `±9` 的强风，且左右风向都会在边界处把船推回中区，等效成“风墙”。将测试场景改为 traversal-lab 参数：中心入口区无风，左区向左外推、右区向右外推，左右风速约 `3`，`HyperWind_TestShip` 风响应倍率回到 `1.0`。
- **目的**：先确保玩家可以自由进入左右测试空间，再验证风感；避免测试场景本身把“风的存在”误做成不可穿越墙体。
- **技术**：通过 Unity Editor 执行代码修改并保存 `WindFieldManager` 的三个矩形风区与场景说明文本。当前采样结果：左区 `(-3.00, 0.00)`，中区 `(0.00, 0.00)`，右区 `(3.00, 0.00)`。本轮还确认场景内仅有 `HyperWind_TestShip` 的 `CircleCollider2D`，没有边界 marker 碰撞体；Console 中出现的 `Physics2D.Simulate` 警告来自临时诊断代码，不影响场景数据。

---

## HyperWind 测试场景风感强化：把 M1 调到肉眼可见 — 2026-05-17 11:31


- **修改文件**
  - `Assets/Scenes/HyperWind/HyperWind_SliceD_Test.unity`
  - `Docs/0_Plan/ongoing/2026-05-16-hyperwind-slice-d-implementation-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：针对手动场景验证反馈“根本没看到任何差别”，检查运行时确认风场服务和 `ShipMotor` 风速层都正常工作，但原始场景参数过弱：左/右侧风速约 `±1.63`，中区约 `0.49`，在正常飞船移动手感下不够明显。将 `HyperWind_SliceD_Test.unity` 调整为 lab validation 明显参数：左/右风区 base speed = `6`，中区 base speed = `1.5`，`HyperWind_TestShip` 的 `_windVelocityMultiplier = 1.5`。
- **目的**：让 M1 在专用测试场景中先达到“肉眼一眼能看出风在推船”的验证强度，优先确认机制感知成立，再回头做正式竞技场的舒适调参。
- **技术**：通过 Unity Editor 执行代码直接修改并保存测试场景中的 `WindFieldManager`、`WindPhaseController` 和 `ShipMotor` 序列化字段。Play Mode 烟测结果：左顺风 applied wind ≈ `(9.00, 0.00)`，中区 ≈ `(2.25, 0.00)`，右逆风 ≈ `(-9.00, 0.00)`，`PlayerControlledVelocity` 保持 `(0.00, 0.00)`，说明环境风仍是独立速度层。Console 无新增 HyperWind error；仅有既有 GGReplica 材质 SRP Batcher warning、`BoostTrailView` scene-only Bloom 引用警告、以及当前测试场景缺 `PoolManager` 导致武器禁用的提示（S1 前需要补测试场景 PoolManager）。

---

## HyperWind 切片 D' 测试场景创建：独立 M1/S1/L8 实验入口 — 2026-05-16 15:43


- **新建文件**
  - `Assets/Scenes/HyperWind/HyperWind_SliceD_Test.unity`
  - `Assets/Scenes/HyperWind/HyperWind_SliceD_Test.unity.meta`（Unity 自动生成）
- **修改文件**
  - `Docs/0_Plan/ongoing/2026-05-16-hyperwind-slice-d-implementation-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：创建独立 HyperWind 测试场景 `HyperWind_SliceD_Test.unity`，作为切片 D' 的 lab scene。场景中包含 `HyperWindRuntimeRoot`（挂载 `WindPhaseController` + `WindFieldManager`）、`HyperWind_TestShip`（实例化现役 `Ship.prefab`）、`HyperWind_TestCamera`、左顺风/中区弱风/右逆风三段可视标记、边界 marker 与说明文本。
- **目的**：将 M1/S1/L8 的验证入口从当前工作场景和正式关卡中隔离出来，避免污染 `SampleScene.unity` 或 `GGReplicaShipTest.unity`；为后续子弹风偏、单气旋实验、敌弹反噬和小竞技场提供稳定测试容器。
- **技术**：通过 Unity Editor 执行代码创建并保存 `.unity` 场景，`.meta` 由 Unity 自动生成，未手写 meta。风场区域沿用 `WindFieldManager.Reset()` 的默认左顺风/中区弱风/右逆风布局。验证：AssetDatabase refresh 完成，场景文件存在；Unity Console error 为 0，仅有既有 GGReplica 材质 SRP Batcher warning。

---

## HyperWind 切片 D' Task 2 验证完成：M1 Play Mode 烟测通过 — 2026-05-16 14:29


- **修改文件**
  - `Docs/0_Plan/ongoing/2026-05-16-hyperwind-slice-d-implementation-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：使用 Unity MCP 进入 Play Mode，在运行时临时创建 `WindPhaseController` / `WindFieldManager` 验证入口，并手动对现有 `ShipMotor` 执行左顺风、中区弱风、右逆风三点采样与 `FixedUpdate()` 烟测。验证结果：左区 sample / applied wind velocity ≈ `(1.63, 0.00)`，中区 ≈ `(0.49, 0.00)`，右区 ≈ `(-1.63, 0.00)`；`PlayerControlledVelocity` 保持 `(0.00, 0.00)`，说明环境风速层确实在玩家速度 clamp 之后独立叠加。
- **目的**：完成 ongoing plan 中 Task 2.4 的运行时验证，确认 M1 飞船风矢叠加的技术结构成立，再进入 S1 子弹风偏接入。
- **技术**：验证对象全部为 Play Mode 临时 GameObject，未保存场景；通过 `ShipMotor.SetVelocity(Vector2.zero)` 清理旧风速层后再采样，避免测试代码直接写 `Rigidbody2D.linearVelocity` 造成误判。退出 Play Mode 后 Console error 为 0；仅出现既有 GGReplica 材质 SRP Batcher warning，与本次 HyperWind 改动无关。

---

## HyperWind 切片 D' Task 2 部分完成：M1 飞船风矢叠加接入 — 2026-05-16 14:12


- **修改文件**
  - `Assets/Scripts/Ship/Movement/ShipMotor.cs`
  - `Assets/Scripts/Core/HyperWind/WindFieldManager.cs`
  - `Docs/0_Plan/ongoing/2026-05-16-hyperwind-slice-d-implementation-plan.md`
  - `Docs/2_TechnicalDesign/HyperWind/HyperWind_ArchBrief.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容**：在 `ShipMotor` 中接入 M1 风矢叠加的最小运行时实现。新增 HyperWind 开关 `_enableWindFieldInfluence`、风速倍率 `_windVelocityMultiplier`、调试属性 `CurrentWindVelocity` / `PlayerControlledVelocity`，并在 `FixedUpdate()` 中采用“移除旧风速 → 玩家推进 → `ClampSpeed()` → 叠加新风速”的顺序，使环境风速不会被玩家速度上限吃掉。直接设速 `SetVelocity()` 前也会清理已叠加风速，避免旧风速残留影响剧情/测试设速。
- **目的**：完成切片 D' 的 M1 飞船风感接入地基，让飞船在风场中开始拥有顺风/逆风位移差异，同时保持现有玩家推进、Dash/Boost 冲量和速度上限逻辑的基本边界。
- **技术**：通过 `ServiceLocator.TryGet<IWindFieldService>()` 缓存风场服务，避免每帧使用 `ServiceLocator.Get<T>()` 产生日志；环境风作为单独 velocity layer 每帧重算，不写入 `ShipStatsSO`，不新增 SO，不修改 prefab。为通过 Unity 编译，同步修复 `WindFieldManager` 中 target-typed `new(...) * float` 的 C# 语法兼容问题，改为显式 `new Vector3(...)`。`ongoing plan` 中 Task 2.1-2.3 已标记完成，Task 2.4 Play Mode 手感验证仍保留未完成。验证：`read_lints` 相关文件 0 diagnostics；Unity MCP 脚本编译后 Console error 为 0。


---

## HyperWind 切片 D' Ongoing Plan 补建 — 2026-05-16 14:09


- **新建文件**
  - `Docs/0_Plan/ongoing/2026-05-16-hyperwind-slice-d-implementation-plan.md`
- **修改文件**
  - `Docs/0_Plan/ongoing/README.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：补建 HyperWind 切片 D' · 气旋竞技场增强版的 ongoing implementation plan，用 checkbox task 形式串起已完成的 Batch 0-1、后续 M1/S1 风接入、L8 地面气旋、程序化视图、E1 风骑兵、竞技场装配与最终验证。同步更新 `Docs/0_Plan/ongoing/README.md`，把该计划列为当前正在推进的专项。
- **目的**：修复启动 HyperWind 实现时只建了架构速写和代码地基、但没有建立 ongoing plan 的流程遗漏；让后续多 Batch 实现有可追踪任务源，避免仅靠聊天上下文推进。
- **技术**：采用与现有 ongoing plan 一致的 checklist 结构，明确 source docs、global constraints、文件结构、Task 0-8、风险清单和日志要求；已完成项标记为 `[x]`，后续批次保留为 `[ ]`。

---

## HyperWind 切片 D' Batch 0-1：架构速写与风场地基服务 — 2026-05-16 14:04


- **新建文件**
  - `Docs/2_TechnicalDesign/HyperWind/HyperWind_ArchBrief.md`
  - `Assets/Scripts/Core/HyperWind/WindSample.cs`
  - `Assets/Scripts/Core/HyperWind/IWindFieldService.cs`
  - `Assets/Scripts/Core/HyperWind/IWindPhaseService.cs`
  - `Assets/Scripts/Core/HyperWind/WindPhaseController.cs`
  - `Assets/Scripts/Core/HyperWind/WindFieldManager.cs`
- **修改文件**
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：开始实现 `HyperWind_MechanicsBrief.md` 的切片 D' · 气旋竞技场增强版。先补 `HyperWind_ArchBrief.md`，明确 G1/G2/G3/M1/S1/L8/E1 的跨模块边界、程序化表现替换缝、首轮暂不做项与 Batch 1 验收标准。随后在 `Core/HyperWind` 下新增最小共享风场地基：`WindSample`、`IWindFieldService`、`IWindPhaseService`、`WindPhaseController`、`WindFieldManager`。`WindPhaseController` 提供弱相位、沙浪预警、声音预警、强相位与风速倍率；`WindFieldManager` 提供矩形风区采样、默认左顺风/中区弱风/右逆风布局，以及 SceneView gizmo 调试箭头。
- **目的**：先让 HyperWind 的“风”成为可采样、可调试、可被 Ship/Combat/Level/Enemy 共同消费的基础服务，再进入 M1/S1 和 L8 气旋玩法，避免直接在 `ShipMotor` 或 `Projectile` 中硬写风逻辑。
- **技术**：采用 `ServiceLocator.Register<IWindPhaseService>()` / `Register<IWindFieldService>()` 注册服务；首轮把接口和最小实现放入 `ProjectArk.Core` assembly 下的 `ProjectArk.HyperWind` 命名空间，避免 Batch 1 同时修改多份 asmdef。风场实现为 MVP 矩形区域采样，后续可替换为贴图/矢量场采样而不改变消费者接口。验证：`read_lints` 针对 `Assets/Scripts/Core/HyperWind` 与 `HyperWind_ArchBrief.md` 返回 0 diagnostics；Unity MCP `refresh_unity` 请求脚本编译后 editor 回到 ready，Console error 为 0；尝试执行 `dotnet build Project-Ark.slnx` 时因当前工作区缺少 Unity 生成的多个 `.csproj` 文件而失败，属于项目文件生成状态问题，不是本次新增代码的 C# 语法诊断。


---

## GGReplica 飞船迁移设计 Spec — 2026-05-13 15:15


- **新建文件**
  - `Docs/0_Plan/specs/2026-05-13-ggreplica-ship-migration-design.md`
- **内容**：完成 GGReplica 飞船迁移设计 spec，目标是在不污染现役 `Ship.prefab` 的前提下，创建并行实验 Prefab `Ship_GGReplica.prefab`，分阶段选择性迁移 Galactic Glitch 的飞船贴图、Boost/Dodge 特效、音效、状态切换与手感参数。设计覆盖战斗五态：Normal / Boost / Dodge / Primary-Fire / Primary+Boost。
- **目的**：验证能否从 `/Users/dada/Documents/Quark/ReferenceAssets/Galactic Glitch` 的 `DevXUnity` / `DevXUnity_exported` 中选择性迁移飞船资产，并尽量复刻 GG 飞船的模型、贴图、特效和 dodge/boost 手感，同时遵守 Project Ark 的 Ship/VFX authority 治理，不直接搬运外部 `Player.prefab` 或污染现役主链。
- **技术**：采用 Replica 隔离层：`Ship.prefab` 保持现役，新增 `Ship_GGReplica.prefab`、`GGReplicaShipVisualProfileSO`、`GGReplicaShipFeelProfileSO`、`GGReplicaShipViewAdapter`、`GGReplicaShipFeelAdapter`；新增 Phase 1.5 DevX Shader Compatibility Spike，先验证 DevX shader 是否可编译/可局部采用，再决定材质结构。分阶段为 Phase 0 资产审计、Phase 1 复制 PNG/WAV、Phase 1.5 Shader Spike、Phase 2 Prefab 构建、Phase 3 Boost/Dodge VFX+音效、Phase 4 GG 手感配置实验、Phase 5 测试场景与 A/B 对比。
- **自审**：已扫描 spec 中 `TBD/TODO/待定/??` 等占位，无遗留占位；确认非目标明确写出不直接迁 GG `Player.prefab`、不覆盖现役 shader、不修改 `SampleScene.unity` 正式入口；确认 shader spike 前置，不会在不知道 shader 可用性的情况下构建 prefab 材质链。

---

## GGReplica 飞船迁移实施计划 — 2026-05-13 15:20

- **新建文件**
  - `Docs/0_Plan/ongoing/2026-05-13-ggreplica-ship-migration-plan.md`
- **内容**：基于已审批的 `2026-05-13-ggreplica-ship-migration-design.md`，编写完整实施计划。计划拆成 10 个任务：Task 0 资产审计、Task 1 资产导入器、Task 2 DevX Shader Spike、Task 3 Visual/Feel Profiles、Task 4 视觉 Adapter、Task 5 实验 Prefab Builder、Task 6 Boost/Dash VFX 音效 Adapter、Task 7 Feel Adapter、Task 8 A/B 测试场景、Task 9 Auditor + 文档登记。
- **目的**：在不污染现役 `Ship.prefab` / `SampleScene.unity` / Ship/VFX 主链的前提下，建立可验证的 `Ship_GGReplica.prefab` 实验通道，先隔离迁移 Galactic Glitch 飞船资产与 Shader 候选，再逐步验证外观、五态切换、Boost/Dodge 特效、音效和手感。
- **技术**：计划使用 Editor automation 进行 curated asset import、Prefab 构建和测试场景生成；运行时新增 `GGReplicaShipVisualProfileSO`、`GGReplicaShipFeelProfileSO`、`GGReplicaShipViewAdapter`、`GGReplicaBoostVfxAdapter`、`GGReplicaDashVfxAdapter`、`GGReplicaShipFeelAdapter`。Shader 采用 Spike 分级 L0-L3，DevX shader 只进 `DevX_Trial` 隔离目录，不能直接成为现役 shader。
- **自审**：已扫描计划中的 `TBD/TODO/待定/.../appropriate/similar to/implement later/fill in`，无遗留占位。计划中的 git commit 命令全部注释，遵守“未经用户明确要求不提交”的安全规则。

---

## GGReplica curated 资产导入工具 — 2026-05-13 15:36

- **新建文件**
  - `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaAssetImporter.cs`
  - `Assets/_Art/Ship/GGReplica/Sprites/`（12 个 PNG，由 Unity 生成对应 `.meta`）
  - `Assets/_Art/Ship/GGReplica/Audio/`（7 个 WAV，由 Unity 生成对应 `.meta`）
  - `Assets/_Art/Ship/GGReplica/Shaders/DevX_Trial/`（3 个 shader trial，由 Unity 生成对应 `.meta`）
  - `Assets/_Art/Ship/GGReplica/Materials/DevX_Trial/`（为后续 shader spike 预留）
- **修改文件**
  - `Docs/6_Diagnostics/GGReplica_ShipAsset_Audit.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：新增 `ProjectArk > Ship > GG Replica > Import Curated Assets` Editor 菜单，按审计表复制 curated Galactic Glitch PNG/WAV/限定 shader trial 文件到隔离目录 `Assets/_Art/Ship/GGReplica/`，并为 Sprite 设置 `TextureImporterType.Sprite`、Single、PPU/Pivot、关闭 mipmap、开启 alpha transparency、无压缩，为音频设置 `DecompressOnLoad` + PCM。执行导入后更新审计表状态为 `IMPORTED`。
- **目的**：完成 GGReplica Phase 1 资产迁移入口，让后续 `Ship_GGReplica.prefab`、Visual Profile 和 Shader Spike 只消费隔离资产，不污染现役 `Ship.prefab`、`SampleScene.unity` 或 `Assets/_Art/Ship/Glitch/` 主链。
- **技术**：Unity Editor automation + `AssetDatabase` + `TextureImporter` + `AudioImporter`；不复制外部 `.meta`，所有 `.meta` 由 Unity 自动生成。导入菜单不弹出模态对话框，便于 MCP/CI 自动执行。验证结果：`dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 通过（0 错误）；Unity Console 出现导入成功日志，无 GGReplica missing-source 错误。导入 shader trial 后的 shader parse errors 记录为 Task 2 Shader Spike 的评级输入，不视为 Task 1 缺源失败。

---

## GGReplica DevX Shader Spike — 2026-05-13 15:36

- **新建文件**
  - `Docs/6_Diagnostics/GGReplica_ShaderSpike_Report.md`
- **修改文件**
  - `Docs/6_Diagnostics/GGReplica_ShipAsset_Audit.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：完成三个 DevX shader trial 文件的兼容性 spike：`CLG_PlayerShipHighlight.shader`、`Lit_PlayerLightSourceColored.shader`、`PlayerLQTrail.shader`。Unity Console 报告三条 shader parse error；源码检查确认对应行都是 DevX `!!! Allow restore shader as UnityLab format` 还原标记与反编译 `Program` 块。三者均评级为 L0，不能直接或局部采用为运行时 shader。
- **目的**：落实用户要求“先尝试 DevXUnity 提供的 shader 是否能直接采用”，在构建 `Ship_GGReplica.prefab` 前明确 DevX shader 只能作为视觉/属性参考，避免把不可编译 shader 接入实验 Prefab 或现役 Ship/VFX 主链。
- **技术**：Unity MCP 读取 Console + 源码行号审计 + L0-L3 分级决策。结论：保留 `Assets/_Art/Ship/GGReplica/Shaders/DevX_Trial/` 文件作为 reference-only；后续若需要高光、液体 tint、Boost trail，应在 `Assets/_Art/Ship/GGReplica/Shaders/ProjectArk_Rebuilt/` 下重建 Project Ark-owned shader，参考 `_Tint` / `_Intensity` / `_Smooth` / noise/color 参数，不直接赋给材质。

---

## GGReplica Visual/Feel Profiles — 2026-05-13 15:45

- **新建文件**
  - `Assets/Scripts/Ship/GGReplica/GGReplicaShipVisualProfileSO.cs`
  - `Assets/Scripts/Ship/GGReplica/GGReplicaShipFeelProfileSO.cs`
  - `Assets/Scripts/Ship/Tests/ProjectArk.Ship.Tests.asmdef`
  - `Assets/Scripts/Ship/Tests/GGReplicaShipProfileTests.cs`
  - `Assets/_Data/Ship/GGReplicaShipVisualProfile.asset`
  - `Assets/_Data/Ship/GGReplicaShipFeelProfile.asset`
- **修改文件**
  - `Project-Ark.slnx`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：新增 GGReplica 视觉与手感 Profile。`GGReplicaShipVisualProfileSO` 存储 Normal / Boost / Dodge / Fire / FireBoost 五态 `GGReplicaSpritePack`、背部/核心/闪避 ghost sprite 与 Boost/Dodge/Fire 音频引用；`GGReplicaShipFeelProfileSO` 存储 GG 风格 Boost/Dodge 初始调参。通过 Unity MCP 创建并填充两个 SO 资产，引用已导入的 `Assets/_Art/Ship/GGReplica/` sprites/audio。
- **目的**：为后续 `Ship_GGReplica.prefab`、视觉 Adapter、音效 Adapter 和手感 Adapter 提供隔离数据源，避免修改现役 `ShipStatsSO`、`Ship.prefab` 或 `SampleScene.unity`。
- **技术**：TDD：先新增 `GGReplicaShipProfileTests`，确认 RED 为缺失 `GGReplicaShipVisualProfileSO` / `GGReplicaSpritePack` / `GGReplicaVisualState` / `GGReplicaShipFeelProfileSO` 类型；随后实现最小 SO 代码并运行 PlayMode 指定测试，3/3 通过。资产创建使用 Unity Editor 内存执行代码 + `SerializedObject` 写入私有序列化字段，未手写 `.asset` 或 `.meta`。验证结果：`dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 通过（0 错误）；新增测试 `ProjectArk.Ship.Tests.GGReplicaShipProfileTests` 3/3 通过。

---

## GGReplica Ship View Adapter — 2026-05-13 15:55

- **新建文件**
  - `Assets/Scripts/Ship/GGReplica/GGReplicaShipViewAdapter.cs`
  - `Assets/Scripts/Ship/Tests/GGReplicaShipViewAdapterTests.cs`
- **修改文件**
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：新增 `GGReplicaShipViewAdapter`，作为 `Ship_GGReplica.prefab` 专用的五态 sprite pack 切换器。支持 `SetFiring(bool)` 与 `ForceVisualState(GGReplicaVisualState)`；按 `Dodge > FireBoost > Boost > Fire > Normal` 优先级从 `ShipDash`、`ShipBoost`、`ShipStateController` 与外部 firing flag 解析视觉状态；应用 Solid / Liquid / Highlight 三层 sprite、offset，并控制 Dodge ghost 显隐；OnEnable/OnDisable 中订阅并清理 `ShipStateController`、`ShipDash`、`ShipBoost` 事件。
- **目的**：让后续实验 Prefab 能消费 `GGReplicaShipVisualProfileSO`，在不修改现役 `ShipView`、`Ship.prefab` 或 `SampleScene.unity` 的前提下验证 Galactic Glitch 五态飞船外观。
- **技术**：TDD：先新增 `GGReplicaShipViewAdapterTests`，确认 RED 为缺失 `GGReplicaShipViewAdapter` 类型；实现 Adapter 后运行 PlayMode 指定测试，`GGReplicaShipProfileTests` + `GGReplicaShipViewAdapterTests` 共 5/5 通过。测试覆盖强制 Dodge/Normal 状态切换、Dodge ghost 显隐、sprite/offset 应用，以及无 Boost/Dash 时 `SetFiring(true)` 切换 Fire pack。验证结果：`dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 通过（0 错误）。

---

## GGReplica Prefab Builder — 2026-05-13 16:05

- **新建文件**
  - `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaPrefabBuilder.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaPrefabBuilderTests.cs`
  - `Assets/_Prefabs/Ship/Ship_GGReplica.prefab`
- **修改文件**
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：新增 `ProjectArk > Ship > GG Replica > Build Experimental Prefab` Editor 菜单。Builder 从现役 `Assets/_Prefabs/Ship/Ship.prefab` 读取 prefab contents，生成隔离目标 `Assets/_Prefabs/Ship/Ship_GGReplica.prefab`，在副本根节点添加并接线 `GGReplicaShipViewAdapter`，包括 Visual Profile、Back/Liquid/Highlight/Solid/Core/DodgeGhost renderer、`ShipStateController`、`ShipBoost`、`ShipDash` 引用。
- **目的**：建立可继续挂载 VFX/audio/feel adapter 的 GGReplica 实验 Prefab，同时保持现役 `Ship.prefab` 不被修改，符合 Replica lane 隔离策略。
- **技术**：TDD：先新增 `GGReplicaPrefabBuilderTests`，确认 RED 为缺失 `GGReplicaPrefabBuilder`；实现后运行 EditMode 指定测试 `BuildExperimentalPrefab_CreatesReplicaWithAdapterWired`，1/1 通过。测试断言 `Ship_GGReplica.prefab` 存在、根名正确、Adapter 存在且 Profile/Renderer 引用已接线，并确认 live `Ship.prefab` 不包含 replica-only Adapter。回归运行 GGReplica runtime tests 5/5 通过；`dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 通过（0 错误）。

---

## GGReplica Boost/Dash VFX Audio Adapters — 2026-05-13 16:13

- **新建文件**
  - `Assets/Scripts/Ship/GGReplica/GGReplicaBoostVfxAdapter.cs`
  - `Assets/Scripts/Ship/GGReplica/GGReplicaDashVfxAdapter.cs`
  - `Assets/Scripts/Ship/Tests/GGReplicaVfxAdapterTests.cs`
- **修改文件**
  - `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaPrefabBuilder.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaPrefabBuilderTests.cs`
  - `Assets/_Prefabs/Ship/Ship_GGReplica.prefab`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：新增 GGReplica 专用 Boost/Dash 音效 Adapter。`GGReplicaBoostVfxAdapter` 订阅 `ShipBoost.OnBoostStarted/OnBoostEnded`，播放 Boost ignite one-shot、设置 Boost loop clip 并在结束时停止/清空 loop；`GGReplicaDashVfxAdapter` 订阅 `ShipDash.OnDashStarted` 播放 Dodge 音效。`GGReplicaPrefabBuilder` 现在会在 `Ship_GGReplica.prefab` 根节点确保 `AudioSource`、`GGReplicaBoostVfxAdapter`、`GGReplicaDashVfxAdapter` 存在并完成 Profile/AudioSource/事件源接线。
- **目的**：让 `Ship_GGReplica.prefab` 具备第一版 Boost/Dodge 音频反馈，同时保持现役 `Ship.prefab`、`ShipView`、`AudioManager` 与 `SampleScene.unity` 不受影响。
- **技术**：TDD：先新增 `GGReplicaVfxAdapterTests`，确认 RED 为缺失 `GGReplicaBoostVfxAdapter` / `GGReplicaDashVfxAdapter`；实现后运行 PlayMode Adapter 测试 2/2 通过。随后扩展 `GGReplicaPrefabBuilderTests`，确认 RED 为 prefab 未接入音效 Adapter；更新 Builder 后 EditMode builder 测试 1/1 通过。回归运行 GGReplica runtime tests 7/7 通过；`dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 通过（0 错误）；Unity Console 无 GGReplica error。

---

## GGReplica Ship Feel Adapter — 2026-05-13 16:24

- **新建文件**
  - `Assets/Scripts/Ship/GGReplica/GGReplicaShipFeelAdapter.cs`
  - `Assets/Scripts/Ship/Tests/GGReplicaFeelAdapterTests.cs`
- **修改文件**
  - `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaPrefabBuilder.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaPrefabBuilderTests.cs`
  - `Assets/_Prefabs/Ship/Ship_GGReplica.prefab`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：新增 GGReplica 专用手感 Adapter。`GGReplicaShipFeelAdapter` 读取 `GGReplicaShipFeelProfileSO`，订阅 `ShipDash.OnDashStarted` 与 `ShipBoost.OnBoostStarted/OnBoostEnded`，对实验 Prefab 的 `Rigidbody2D` 施加 Dash after-dodge impulse、Boost start impulse，并在 Boost/Dash 阶段调整 `linearDamping`；禁用/销毁时取消 UniTask drag tween 并恢复进入时的 base drag。`GGReplicaPrefabBuilder` 现在会给 `Ship_GGReplica.prefab` 接入 `GGReplicaShipFeelAdapter`，并接线 Feel Profile、Rigidbody2D、ShipDash、ShipBoost。
- **目的**：为 `Ship_GGReplica.prefab` 提供第一版 GG 风格 Dodge/Boost 手感实验，不修改现役 `ShipStatsSO`、`ShipMotor` 默认值、`Ship.prefab` 或 `SampleScene.unity`。
- **技术**：TDD：先新增 `GGReplicaFeelAdapterTests` 与 Builder 断言，确认 RED 为缺失 `GGReplicaShipFeelAdapter`；实现 Adapter 与 Builder 接线后，PlayMode Feel Adapter 测试 3/3 通过，EditMode Builder 测试 1/1 通过。回归运行 GGReplica runtime tests 10/10 通过；`dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 通过（0 错误）；Unity Console 无 GGReplica error。异步 drag 使用 UniTask + linked `CancellationTokenSource`，生命周期取消不抛未处理异常。

---

## GGReplica A/B Test Scene — 2026-05-13 16:32

- **新建文件**
  - `Assets/Scripts/Ship/GGReplica/GGReplicaTestSwitcher.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaTestSceneBuilder.cs`
  - `Assets/Scripts/Ship/Tests/GGReplicaTestSwitcherTests.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaTestSceneBuilderTests.cs`
  - `Assets/Scenes/GGReplicaShipTest.unity`
- **修改文件**
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：新增 GGReplica A/B 测试场景工具。`GGReplicaTestSwitcher` 支持 `SetReplicaActive(bool)`、F1 切换 live/replica，以及 F2-F6 强制 Normal/Boost/Dodge/Fire/FireBoost 视觉状态。`GGReplicaTestSceneBuilder` 新增菜单 `ProjectArk > Ship > GG Replica > Build Test Scene`，生成 `Assets/Scenes/GGReplicaShipTest.unity`，包含 `LiveShip_A`、`GGReplicaShip_B`、`GGReplicaTestSwitcher` 与正交 `Main Camera`。
- **目的**：给用户提供可直接 Play Mode 打开的隔离 A/B 验证场景，在不修改 `Assets/Scenes/SampleScene.unity` 的前提下对比现役 `Ship.prefab` 与实验 `Ship_GGReplica.prefab` 的五态视觉、音效和手感。
- **技术**：TDD：先新增 `GGReplicaTestSwitcherTests` 和 `GGReplicaTestSceneBuilderTests`，确认 RED 为缺失 `GGReplicaTestSwitcher` / `GGReplicaTestSceneBuilder`；实现后 PlayMode switcher 测试 1/1 通过，EditMode scene builder 测试 1/1 通过。回归运行 GGReplica runtime tests 11/11 通过；`dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 通过（0 错误）；Unity Console 无 GGReplica error。场景由 Unity 生成 `.unity` / `.meta`，未手写 scene GUID 或 meta。

---

## GGReplica Isolation Auditor 与资产登记 — 2026-05-13 22:42

- **新建文件**
  - `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaAuditor.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaAuditorTests.cs`
- **修改文件**
  - `Docs/2_TechnicalDesign/Ship/ShipVFX_AssetRegistry.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：新增只读 `GGReplicaAuditor`，提供菜单 `ProjectArk > Ship > GG Replica > Audit Replica Isolation`。Auditor 检查 GGReplica 必需资产存在、`Ship_GGReplica.prefab` 的 View/VFX/Feel Adapter 与 Profile/Renderer/AudioSource/Rigidbody/事件源引用已接线，并检查 live `Ship.prefab` 与 `SampleScene.unity` 没有 GGReplica 组件或引用污染。`ShipVFX_AssetRegistry.md` 新增 `GGReplica Experimental Assets` 表，登记 `Ship_GGReplica.prefab`、Visual/Feel Profile、Art Root、A/B Test Scene 与 Isolation Auditor，全部标记为 `Reference` 而非 `Live`。
- **目的**：完成 GGReplica 实验链收尾治理，给后续继续 Play Mode 验证或合入主链前的隔离检查提供官方入口，同时保持现役 Ship/VFX authority 不被实验通道污染。
- **技术**：TDD：先新增 `GGReplicaAuditorTests`，要求 auditor 运行后无 Error 且不报告 live chain / `SampleScene` 污染；随后实现 `RunAudit(logToConsole)`、`LastResults` 与 `Severity` 结果模型。当前因 Unity MCP session 暂时不可用，未能在本回合重新运行 Unity Test Runner；已完成替代验证：`dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 通过（0 错误），文本审计确认 `Assets/_Prefabs/Ship/Ship.prefab` 中 0 个 GGReplica 组件引用、`Assets/Scenes/SampleScene.unity` 中 0 个 `Ship_GGReplica` / `GGReplicaTestSwitcher` 引用，且 `Ship_GGReplica.prefab` 中存在 View/Boost/Dash/Feel 四个 GGReplica Adapter。

---

## GGReplica 测试场景输入入口修复 — 2026-05-14 00:40

- **修改文件**
  - `Assets/Scripts/Ship/GGReplica/GGReplicaTestSwitcher.cs`
  - `Assets/Scripts/Ship/Tests/GGReplicaTestSwitcherTests.cs`
  - `Assets/Scenes/GGReplicaShipTest.unity`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：针对 Play Mode 中用户反馈“F2-F6 五态视觉没有变化”，先用 MCP 验证 `GGReplicaShipViewAdapter.ForceVisualState()` 在 Edit/Play Mode 都能正确把三层 sprite 从 Normal 切到 Boost/Fire，排除 Profile/Prefab 接线问题。根因定位为测试场景只提示/依赖 F1-F6，而 macOS/Unity GameView 焦点下 F 键容易被系统功能键或焦点吞掉，导致用户没有触发 `GGReplicaTestSwitcher.Update()`。修复为保留 F1-F6，同时新增 Tab 与数字键 1-5 / 小键盘 1-5，并增加左上角 IMGUI 控制面板，支持鼠标点击切换 live/replica 和五态视觉。
- **目的**：让 A/B 测试场景不再依赖易失效的 F 键输入，确保用户能稳定触发 Normal/Boost/Dodge/Fire/FireBoost 五态视觉验证。
- **技术**：Systematic debugging：先读取 Console、检查 active scene、用 MCP 在 Edit/Play Mode 直接调用 `ForceVisualState()` 证明 sprite 可变；再最小化修改输入层。新增 `ReplicaActive` 只读属性辅助测试；`GGReplicaTestSwitcherTests` 增补 active 状态断言。验证结果：PlayMode `GGReplicaTestSwitcherTests.SetReplicaActive_TogglesLiveAndReplicaShips` 1/1 通过；重新生成 `GGReplicaShipTest.unity`；Play Mode MCP 调用 `switcher.ForceReplicaState(Boost/FireBoost)` 显示 sprite 从 `Movement_10_0/Movement_3_0/Movement_21_0` 切换到 `Boost_2/Boost_16/Boost_8` 再切到 `Primary_4/Primary/Primary_6`；`dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 通过（0 错误）；Unity Console 无 GGReplica error。

---

## GGReplica PlayerView 深度审计与重建设计 — 2026-05-14 01:29

- **新建文件**
  - `Docs/6_Diagnostics/GGReplica_PlayerView_DeepAudit.md`
  - `Docs/0_Plan/specs/2026-05-14-ggreplica-playerview-rebuild-design.md`
- **修改文件**
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：响应用户指出当前 GGReplica 只是“换了不同贴图”而非原版默认飞机表现的问题，重新审计 Galactic Glitch 原始 `PlayerView` / `PlayerSkin` / Animator Event / `PlayerSkinDefault` 链路。审计文档记录原版 `PlayerShipState -> Animator Clip -> ChangeViewState(int) -> PlayerView.ViewState -> PlayerSkinDefault.stateToSpritesTable -> SpriteRenderer + PlayerView modules` 的链路，明确 `Dodge` 的 sprite pack 为 null，不应被当成普通机体贴图状态。设计文档提出下一阶段重建：引入 `GGReplicaViewState` 原始 int 值、`GGReplicaPlayerSkinSO`、完整 stateToSpritesTable、固定 back/grab/core/eye/view silhouette 层，以及 Core/Boost/Shape 模块 MVP。
- **目的**：停止在错误的“五态硬切贴图”模型上继续叠补丁，把后续 GGReplica 复刻转向原版 `PlayerView + PlayerSkin + Animator Event` 模型。
- **技术**：只读审计 + 设计，不改运行时代码。证据来源包括 `PlayerView.cs` 的 `ViewState` / renderer / module 字段，`PlayerSkin.cs` 的 `stateToSpritesTable` 与固定皮肤字段，`AnimationClip/*.anim` 的 `ChangeViewState(int)` 事件，以及既有 `GalacticGlitch_Structure_Analysis.md` 中 resolved `PlayerSkinDefault` 映射。确认额外待导入源包括 `Secondary_*`、`Healing_*`、`GrabGun_Base_d9/d8`、`GrabGun_Hand_d7`、`scheme3_tp`、`SHIP_PLAYER_DODGE_HALF` 等。该文档为后续 implementation plan 的输入。

---

## GGReplica PlayerView 重建实施计划 — 2026-05-14 11:07

- **新建文件**
  - `Docs/0_Plan/ongoing/2026-05-14-ggreplica-playerview-rebuild-plan.md`
- **修改文件**
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：在用户认可 `GGReplica PlayerView Rebuild Design` 后，编写下一阶段实施计划。计划将重建拆成 9 个任务：基线与资产源扩展、`GGReplicaPlayerSkinSO` 数据模型、PlayerSkin 资产构建器、`GGReplicaPlayerViewAdapter`、Core/Boost/Shape MVP 模块、Prefab Builder 重建、Test Switcher/ViewState int 场景、Auditor/Registry 升级、最终验证与实现日志。
- **目的**：把后续工作从错误的“五态硬切贴图”原型迁移到可执行、可测试、保持隔离的 `PlayerView + PlayerSkin + ChangeViewState(int)` 重建路线，避免继续盲目补丁式实现。
- **技术**：计划采用 TDD 红绿循环、Unity Editor automation、ScriptableObject authored data、Replica lane 隔离和 Ship/VFX authority 约束。计划明确新增/修改文件、测试命名、菜单执行顺序、Prefab 层级、Auditor 验收项和 Play Mode 视觉验收清单；自审扫描无 `TBD/TODO/待定/??/implement later/fill in/appropriate/similar to` 占位。

---

## GGReplica PlayerView 资产源扩展 — 2026-05-14 11:16

- **新建文件**
  - `Assets/_Art/Ship/GGReplica/Sprites/Secondary_8.png`（及 Unity 生成 `.meta`）
  - `Assets/_Art/Ship/GGReplica/Sprites/Secondary_0.png`（及 Unity 生成 `.meta`）
  - `Assets/_Art/Ship/GGReplica/Sprites/Secondary_17.png`（及 Unity 生成 `.meta`）
  - `Assets/_Art/Ship/GGReplica/Sprites/Healing_0.png`（及 Unity 生成 `.meta`）
  - `Assets/_Art/Ship/GGReplica/Sprites/Healing.png`（及 Unity 生成 `.meta`）
  - `Assets/_Art/Ship/GGReplica/Sprites/vfx_dot_001.png`（及 Unity 生成 `.meta`）
  - `Assets/_Art/Ship/GGReplica/Sprites/GrabGun_Base_9.png`（及 Unity 生成 `.meta`）
  - `Assets/_Art/Ship/GGReplica/Sprites/GrabGun_Base_8.png`（及 Unity 生成 `.meta`）
  - `Assets/_Art/Ship/GGReplica/Sprites/GrabGun_Hand_7.png`（及 Unity 生成 `.meta`）
  - `Assets/_Art/Ship/GGReplica/Sprites/scheme3_tp.png`（及 Unity 生成 `.meta`）
  - `Assets/_Art/Ship/GGReplica/Sprites/SHIP_PLAYER_DODGE_HALF.png`（及 Unity 生成 `.meta`）
- **修改文件**
  - `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaAssetImporter.cs`
  - `Docs/6_Diagnostics/GGReplica_ShipAsset_Audit.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：执行实施计划 Task 0，先验证新增 GG 源 sprite 均存在，再扩展 `GGReplicaAssetImporter.SpriteEntries`，将 Secondary、Healing、Grab body/highlight/hand、View silhouette、Dodge half silhouette 纳入 curated import。随后执行 `ProjectArk > Ship > GG Replica > Import Curated Assets`，把总 sprite 导入量从 12 个扩展到 23 个，并更新资产审计表与审计结果。
- **目的**：为后续 `GGReplicaPlayerSkinSO` 还原原版 `PlayerSkinDefault.stateToSpritesTable` 准备完整 sprite 输入，避免 PlayerView 重建继续缺 Secondary / Heal / Grab / View / Dodge-half 层。
- **技术**：Unity Editor automation + `AssetDatabase` importer 设置；只复制 PNG，不复制外部 `.meta`，`.meta` 由 Unity 自动生成。验证结果：基线 `dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 为 0 错误；源文件检查 `MISSING: []`；Unity Console 输出 `[GGReplicaAssetImporter] Curated GGReplica assets imported successfully...`；扩展后编译为 0 错误（保留项目现有 4 个警告）。

---

## GGReplica PlayerSkin 数据模型 — 2026-05-14 11:29

- **新建文件**
  - `Assets/Scripts/Ship/GGReplica/GGReplicaPlayerSkinSO.cs`（及 Unity 生成 `.meta`）
  - `Assets/Scripts/Ship/Tests/GGReplicaPlayerSkinTests.cs`（及 Unity 生成 `.meta`）
- **修改文件**
  - `ProjectArk.Ship.csproj`（Unity 自动刷新生成）
  - `ProjectArk.Ship.Tests.csproj`（Unity 自动刷新生成）
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：执行实施计划 Task 1，新增 GG-aligned `GGReplicaViewState`、`GGReplicaViewSpritePack` 与 `GGReplicaPlayerSkinSO`。`GGReplicaViewState` 保留原版 GG int 值 `0,1,2,3,4,5,6,7,8,9,15`；`GGReplicaPlayerSkinSO` 提供 `StateToSpritesTable`、固定 skin 字段、highlight/transition color 和 `TryGetPack` 查询 API。测试覆盖 ViewState int 映射以及 Dodge pack 允许三层 body sprite 为 null。
- **目的**：建立后续 `GGReplicaPlayerViewAdapter` 和 PlayerSkin 资产构建器的 authored data 基础，避免继续沿用错误的五态 `GGReplicaVisualState` 作为最终模型。
- **技术**：TDD：先写 `GGReplicaPlayerSkinTests`，强制 Unity 刷新后 `dotnet build` RED，失败原因为缺失 `GGReplicaViewState` / `GGReplicaPlayerSkinSO` / `GGReplicaViewSpritePack`；随后实现最小数据模型。验证结果：`ProjectArk.Ship.Tests.GGReplicaPlayerSkinTests` PlayMode 2/2 通过；`dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 0 错误（Unity 生成项目文件后存在 86 个既有/生成警告）。

---

## GGReplica PlayerSkin 资产构建器 — 2026-05-14 11:31

- **新建文件**
  - `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaPlayerSkinAssetBuilder.cs`（及 Unity 生成 `.meta`）
  - `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaPlayerSkinAssetBuilderTests.cs`（及 Unity 生成 `.meta`）
  - `Assets/_Data/Ship/GGReplicaPlayerSkin.asset`（及 Unity 生成 `.meta`）
- **修改文件**
  - `ProjectArk.Ship.Editor.csproj`（Unity 自动刷新生成）
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：执行实施计划 Task 2，新增 `ProjectArk > Ship > GG Replica > Build Player Skin Asset` 菜单与 Builder。Builder 从 `Assets/_Art/Ship/GGReplica/Sprites/` 的 curated sprites 构建 `GGReplicaPlayerSkin.asset`，写入完整 ViewState table：Idle、Boost、Dodge、Aim、Fire、HeavyFire、HeavyAim、Grab、WeaponUseMoment、Heal、Undefined；其中 Dodge 三层 body sprite 保持 null，Grab 使用 `(0,-0.1,0)` offset。固定 skin 字段写入 Grab hand、Back、Reactor、临时 Eye、View silhouette、Dodge ghost、Dodge half，并设置 highlight/transition colors。
- **目的**：把原版 `PlayerSkinDefault.stateToSpritesTable` 映射落成 Project Ark 可审计的 authored SO 资产，作为后续 `GGReplicaPlayerViewAdapter` 与 Prefab Builder 的单一数据源。
- **技术**：TDD：先新增 `GGReplicaPlayerSkinAssetBuilderTests`，强制 Unity 刷新后 `dotnet build` RED，失败原因为缺失 `GGReplicaPlayerSkinAssetBuilder`；随后实现 Builder。根据审查反馈补强：Builder 现在先做完整 sprite/property preflight，缺资源或缺序列化字段时中止保存；测试改用临时输出资产并在 TearDown 清理，逐状态断言 sprite/fade/offset、完整 fixed fields 与颜色。验证结果：`ProjectArk.Ship.Editor.GGReplicaPlayerSkinAssetBuilderTests.BuildPlayerSkinAsset_CreatesFullStateTableAndFixedFields` EditMode 1/1 通过；`dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 0 错误（86 个既有/生成警告）。

---

## GGReplica PlayerView Adapter — 2026-05-14 11:35

- **新建文件**
  - `Assets/Scripts/Ship/GGReplica/GGReplicaPlayerViewAdapter.cs`（及 Unity 生成 `.meta`）
  - `Assets/Scripts/Ship/Tests/GGReplicaPlayerViewAdapterTests.cs`（及 Unity 生成 `.meta`）
- **修改文件**
  - `ProjectArk.Ship.csproj`（Unity 自动刷新生成）
  - `ProjectArk.Ship.Tests.csproj`（Unity 自动刷新生成）
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：执行实施计划 Task 3，新增 GG-like `GGReplicaPlayerViewAdapter`。Adapter 暴露 `ChangeViewState(int)`、`ChangeViewState(GGReplicaViewState, bool)`、`GetCurrentSpritePack()`、`CurrentState` 与 `CurrentSpritePack`，从 `GGReplicaPlayerSkinSO` 查表应用三层 body sprite、固定 back/grab/core/eye/view/dodge 层、highlight color 与 `SpritesOffset`。实现 Dodge null pack 规则：pack 内 sprite 为 null 时不清空现有 body renderer；Grab state 会启用左右 grab hand 并应用 `(0,-0.1,0)` offset。
- **目的**：把测试场景与后续 Animator Event 模拟入口切换到原版 GG 风格的 `ChangeViewState(int)` 语义，不再以五态 `ForceVisualState(GGReplicaVisualState)` 作为最终视觉模型。
- **技术**：TDD：先新增 `GGReplicaPlayerViewAdapterTests`，强制 Unity 刷新后 `dotnet build` RED，失败原因为缺失 `GGReplicaPlayerViewAdapter`；随后实现 Adapter。首次 PlayMode 测试暴露 AddComponent 阶段过早报缺 Skin log，已移除 `Awake` 过早报错，保留 `ChangeViewState` 缺 Skin 显式错误与后续 Auditor 兜底。根据审查反馈补强：`ChangeViewState` 现在验证必需引用并报错，`CurrentSpritePack` / `GetCurrentSpritePack()` 返回 pack 副本避免外部污染 SO 内部数据；测试新增 strict 缺 pack warning/error、GetCurrentSpritePack copy、DodgeHalf、Grab body sprites、fixed fields，并用 try/finally 清理 GameObject/SO/Sprite。验证结果：`ProjectArk.Ship.Tests.GGReplicaPlayerViewAdapterTests` PlayMode 5/5 通过；`dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 0 错误（86 个既有/生成警告）。

---

## GGReplica Core/Boost/Shape 视觉模块 MVP — 2026-05-14 12:09

- **新建文件**
  - `Assets/Scripts/Ship/GGReplica/GGReplicaCoreVisualModule.cs`（及 Unity 生成 `.meta`）
  - `Assets/Scripts/Ship/GGReplica/GGReplicaBoostVisualModule.cs`（及 Unity 生成 `.meta`）
  - `Assets/Scripts/Ship/GGReplica/GGReplicaShapeVisualModule.cs`（及 Unity 生成 `.meta`）
  - `Assets/Scripts/Ship/Tests/GGReplicaVisualModuleTests.cs`（及 Unity 生成 `.meta`）
- **修改文件**
  - `Assets/Scripts/Ship/GGReplica/GGReplicaPlayerViewAdapter.cs`
  - `ProjectArk.Ship.csproj`（Unity 自动刷新生成）
  - `ProjectArk.Ship.Tests.csproj`（Unity 自动刷新生成）
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：执行实施计划 Task 4，新增 GGReplica 三个 MVP 视觉模块。`GGReplicaCoreVisualModule` 负责 Dodge 状态下启用 dodge / dodge-half silhouettes，并调整 core scale/color；`GGReplicaBoostVisualModule` 负责 Boost 状态下启用 boost visual root 并播放/停止粒子；`GGReplicaShapeVisualModule` 负责 Grab 状态下启用左右 grab hand renderer。`GGReplicaPlayerViewAdapter` 新增 `_coreModule` / `_boostModule` / `_shapeModule` 引用，并在 `ChangeViewState` 成功后调用 `NotifyModules(state)`。
- **目的**：把原版 GG `PlayerView` 中非纯贴图切换的 Core / Boost / Shape 行为拆成独立模块，停止把 Dodge、Boost、Grab 都压进 body sprite pack 切换逻辑。
- **技术**：TDD：先新增 `GGReplicaVisualModuleTests`，强制 Unity 刷新后 `dotnet build` RED，失败原因为缺失 `GGReplicaCoreVisualModule` / `GGReplicaBoostVisualModule` / `GGReplicaShapeVisualModule`；随后实现三个无 Coroutine / 无 Update 的确定性模块，并给 Adapter 接入模块通知。验证结果：`ProjectArk.Ship.Tests.GGReplicaVisualModuleTests` PlayMode 4/4 通过；回归 `ProjectArk.Ship.Tests.GGReplicaPlayerViewAdapterTests` PlayMode 5/5 通过；`dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 0 错误。

---

## GGReplica Prefab PlayerView 层级重建 — 2026-05-14 12:30

- **修改文件**
  - `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaPrefabBuilder.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaPrefabBuilderTests.cs`
  - `Assets/_Prefabs/Ship/Ship_GGReplica.prefab`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：执行实施计划 Task 5，重建 `Ship_GGReplica.prefab` 的 PlayerView visual lane。`GGReplicaPrefabBuilder` 现在加载 `GGReplicaPlayerSkin.asset`，创建 `ShipVisual/GGPlayerViewRoot`，生成 11 个原版 PlayerView 风格 render nodes 并设置 sortingOrder；接入 `GGReplicaPlayerViewAdapter`、`GGReplicaCoreVisualModule`、`GGReplicaBoostVisualModule`、`GGReplicaShapeVisualModule`；移除旧 `GGReplicaShipViewAdapter`。同时保留 Boost/Dash audio adapters 与 Feel adapter。
- **目的**：让 `Ship_GGReplica.prefab` 从旧五态 sprite switcher 迁移为新 `PlayerView + PlayerSkin + Modules` 视觉 owner，避免新旧视觉链路双轨驱动。
- **技术**：TDD：先更新 `GGReplicaPrefabBuilderTests`，验证 RED 为 prefab 仍含旧 `GGReplicaShipViewAdapter`；随后实现 Builder 重建逻辑。根据审查反馈补强：Builder 接线失败会在保存前中止，避免覆盖成坏 prefab；`ShipView` live VFX authority 在 replica prefab 中禁用，旧 `ShipVisual` 直属 renderers 禁用；`GGPlayerViewRoot` 直接子节点严格为 11 个 required render nodes，`GGBoostVisualRoot` 移到其外部；测试用对象身份 `Is.SameAs` 验证 Adapter 引用指向 `GGPlayerViewRoot` 下的新 renderer。验证结果：`ProjectArk.Ship.Editor.GGReplicaPrefabBuilderTests.BuildExperimentalPrefab_CreatesReplicaWithPlayerViewHierarchyAndModulesWired` EditMode 1/1 通过；`dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 0 错误；最终规格复审 PASS，质量复审 APPROVED/PASS，prefab YAML 静态检查 PASS。

---

## GGReplica A/B 场景 ViewState int 控制 — 2026-05-14 14:33

- **修改文件**
  - `Assets/Scripts/Ship/GGReplica/GGReplicaTestSwitcher.cs`
  - `Assets/Scripts/Ship/Tests/GGReplicaTestSwitcherTests.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaTestSceneBuilder.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaTestSceneBuilderTests.cs`
  - `Assets/Scenes/GGReplicaShipTest.unity`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：执行实施计划 Task 6，将 A/B 测试场景控制入口从旧五态 `GGReplicaVisualState` 改为原版 GG `ViewState` int。`GGReplicaTestSwitcher` 现在序列化 `GGReplicaPlayerViewAdapter`，提供 `ForceReplicaViewState(int)` 并转发到 `ChangeViewState(int)`；IMGUI 与键盘覆盖 `0-9` 和 `U=15`。`GGReplicaTestSceneBuilder` 改为把 `_replicaView` 接到 `GGReplicaPlayerViewAdapter`，并在 replica prefab 缺少该 Adapter 时中止构建。
- **目的**：让测试场景真实模拟原版 `AnimationClip -> ChangeViewState(int)` 入口，不再依赖错误的五态热键/贴图切换器。
- **技术**：TDD：先更新 `GGReplicaTestSwitcherTests` 与 `GGReplicaTestSceneBuilderTests`，验证 RED 为缺失 `ForceReplicaViewState(int)` / scene builder test API；随后修改 switcher 和 scene builder。根据审查反馈补强：scene builder 提供测试用临时 scene/prefab 路径，缺 Adapter 时返回 false 且不保存场景；测试使用 SetUp/TearDown 清理临时资产并恢复空场景；正式 `GGReplicaShipTest.unity` 重新生成后 `_replicaView` 指向 `GGReplicaPlayerViewAdapter`，无旧 `GGReplicaShipViewAdapter` stale reference。验证结果：`ProjectArk.Ship.Tests.GGReplicaTestSwitcherTests` PlayMode 2/2 通过；`ProjectArk.Ship.Editor.GGReplicaTestSceneBuilderTests` EditMode 2/2 通过；`dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 0 错误；最终复审 APPROVED/PASS。

---

## GGReplica Auditor 与 Registry PlayerView 升级 — 2026-05-14 14:48

- **修改文件**
  - `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaAuditor.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaAuditorTests.cs`
  - `Docs/2_TechnicalDesign/Ship/ShipVFX_AssetRegistry.md`
  - `Assets/Scenes/GGReplicaShipTest.unity`（测试/菜单重建）
  - `Assets/_Prefabs/Ship/Ship_GGReplica.prefab`（测试/菜单重建）
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：执行实施计划 Task 7，将 `GGReplicaAuditor` 从旧五态 `GGReplicaShipViewAdapter` 口径升级为 PlayerView 重建口径。Auditor 现在验证 `GGReplicaPlayerSkin.asset` 存在、required ViewState packs 完整、Dodge 三层 body sprite 为 null、fixed fields 已赋值、Eye 临时复用 Reactor 仅为 Warning；验证 `Ship_GGReplica.prefab` 包含 `GGReplicaPlayerViewAdapter`、Core/Boost/Shape modules、`GGPlayerViewRoot` 11 个 required children，并拒绝 legacy `GGReplicaShipViewAdapter`；live `Ship.prefab` 污染检测改为递归 `GetComponentsInChildren(includeInactive:true)`；`SampleScene` 隔离检测加入 `Ship_GGReplica.prefab` GUID 与全部 12 个 GGReplica 脚本 GUID。
- **目的**：让官方审计入口反映当前 `PlayerView + PlayerSkin + Modules` 实验链真实架构，避免旧五态 required path 继续误导验收，同时增强 live/scene 污染漏报防护。
- **技术**：TDD：先更新 `GGReplicaAuditorTests`，验证 RED 为旧 Auditor 仍要求 `GGReplicaShipViewAdapter`；随后升级 Auditor。根据审查反馈补强：AuditorTests 按计划恢复 `BuildPlayerSkinAsset -> BuildExperimentalPrefab -> BuildTestScene -> RunAudit` 集成顺序；`GgReplicaScriptPaths` 覆盖当前 `Assets/Scripts/Ship/GGReplica/` 下全部 12 个脚本；旧 `GGReplicaShipVisualProfile.asset` 在代码和 Registry 中明确降级为 legacy audio profile，不再被描述为 PlayerView visual authority。验证结果：`ProjectArk.Ship.Editor.GGReplicaAuditorTests` EditMode 1/1 通过；Auditor 菜单输出 `[GGReplicaAuditor] PASS: GGReplica is isolated from live Ship.prefab and SampleScene.` 且 `0 errors, 1 warnings`（EyeSprite 临时复用 Reactor）；`dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 0 错误；最终复审 APPROVED/PASS。

---

## GGReplica PlayerView 重建最终验证 — 2026-05-14 15:48

- **修改文件**
  - `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaTestSceneBuilder.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaTestSceneBuilderTests.cs`
  - `Assets/Scenes/GGReplicaShipTest.unity`（菜单重建）
  - `Assets/_Prefabs/Ship/Ship_GGReplica.prefab`（菜单重建）
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：执行实施计划 Task 8，完成 GGReplica PlayerView rebuild 最终验证。验证过程中发现 A/B 场景 Play Mode 会因从完整 `Ship.prefab` 复制来的 `StarChartController` / `BoostTrailView` 在无完整服务场景中报错；根因是测试场景只用于视觉 A/B，不应保留依赖 `PoolManager` / scene-only bloom 的非视觉验证组件。`GGReplicaTestSceneBuilder` 新增 `StripNonVisualValidationComponents()`，在实例化 live/replica 后移除 `StarChartController` 与 `BoostTrailView`；测试补充断言两艘测试船均不包含这些组件。
- **目的**：让 `GGReplicaShipTest.unity` 成为干净的视觉验证场景，避免无关战斗/BoostTrail scene-only 依赖污染最终 Play Mode 验收，同时收口整个 `PlayerView + PlayerSkin + Modules` 重建流程。
- **技术**：系统化调试 + TDD：先读取 Play Mode Console error，定位为测试场景残留非视觉组件，而非 PlayerView 重建链路本身；随后给 `GGReplicaTestSceneBuilderTests` 添加 RED 断言，再在 builder 中按类型名剥离 `StarChartController` / `BoostTrailView`。最终验证结果：PlayMode GGReplica runtime tests 13/13 通过；EditMode GGReplica editor tests 5/5 通过；`dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 0 错误 0 警告；菜单链路 `Import Curated Assets -> Build Player Skin Asset -> Build Experimental Prefab -> Build Test Scene -> Audit Replica Isolation` 全部执行，Console 输出 Auditor PASS 且 `0 errors, 1 warnings`（EyeSprite 临时复用 Reactor）；Play Mode checklist 通过，覆盖 A/B 切换、ViewState `0/1/2/5/6/7/9/15`，Dodge 不清 body 且 ghost/half/core 生效，Grab hands/offset 生效，Secondary/Heal/Undefined sprites 正确；Play Mode checklist 后 Console error 数为 0；`git diff --check` 通过；最终静态审查确认 Done definition 1-8 已满足。

---

## GGReplica 测试场景 ViewState 可见性修复 — 2026-05-14 16:33

- **修改文件**
  - `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaPrefabBuilder.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaPrefabBuilderTests.cs`
  - `Assets/_Prefabs/Ship/Ship_GGReplica.prefab`（重建）
  - `Assets/Scenes/GGReplicaShipTest.unity`（重建）
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：响应测试场景中左上角 `1-9` ViewState 按钮点击后画面无明显变化的问题。排查确认 `GGReplicaTestSwitcher -> GGReplicaPlayerViewAdapter.ChangeViewState(int)` 链路有效，状态和 sprite 已切换；真正根因是 `GGPlayerViewRoot/View` 剪影以 `sortingOrder=8` 渲染在机体状态层之上，同时旧 live `ShipVisual/BoostTrailRoot` 子 SpriteRenderer 仍可见，遮挡了 Solid/Liquid/Highlight 的状态变化。修复后 `View` 剪影改为 `sortingOrder=-10`，旧 live 视觉分支在 replica prefab 中整体 inactive 并禁用其子 SpriteRenderer，`GGBoostVisualRoot` 继续作为 GGReplica Boost 视觉权威。
- **目的**：让 A/B 测试场景的 `0-9/U` 状态按钮能真实呈现 `PlayerView` state sprite 差异，避免被旧 live VFX 或顶层剪影误判为“按钮无效”。
- **技术**：系统化调试 + TDD。先通过 Unity MCP 读取运行时对象、直接调用 `ForceReplicaViewState(7)` 并检查 renderer sprite/visible/sortingOrder，定位为显示遮挡而非输入或 Adapter 断线；随后给 `GGReplicaPrefabBuilderTests` 添加 RED 断言，验证 `View` 必须在 body sprite 后方、旧 `BoostTrailRoot` 必须不可见。修复后重新构建 `GGReplicaPlayerSkin.asset`、`Ship_GGReplica.prefab` 与 `GGReplicaShipTest.unity`。验证结果：新增回归测试先失败于 `View sortingOrder=8`，修复后 `GGReplicaPrefabBuilderTests` + `GGReplicaTestSceneBuilderTests` 2/2 通过；场景实例检查为 `afterState7=Grab solid=GrabGun_Base_9`、`viewOrder=-10 solidOrder=3`、`legacyBoostActiveSelf=False`；`dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 0 错误（构建摘要仍有 4 个 warning）；相关目录 lints 0 条。

---

## GGReplica Visual Parity 设计与实施计划 — 2026-05-14 20:52

- **新建文件**
  - `Docs/0_Plan/specs/2026-05-14-ggreplica-visual-parity-design.md`
  - `Docs/0_Plan/ongoing/2026-05-14-ggreplica-visual-parity-plan.md`
- **修改文件**
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：响应 GGReplica 长期目标偏离的问题，将下一阶段目标明确为复刻 Galactic Glitch 原版 `Player.prefab` 的 shader/material/VFX 视觉链路，而不是继续纯贴图切换。设计文档固化外部参考真相源 `/Users/dada/Documents/Quark/ReferenceAssets/Galactic Glitch/DevXUnity` 与 `DevXUnity_exported`，记录 `PlayerShipHL`、`TeleportScheme`、`PlayerLQTrail` 的原始 shader/material/prefab 接线证据和参数。实施计划拆分为 material builder、clean URP shader、prefab wiring、runtime MaterialPropertyBlock module、Auditor enforcement、registry/docs、full verification 七个任务。
- **目的**：把 GGReplica 复刻重新对齐用户一直要求的“还原 GG 原版观感”目标，避免再次只完成 PlayerView 入口/贴图层 MVP 而缺失 shader 和美术特效。
- **技术**：基于解包资产证据驱动设计：`PlayerShipHL.mat` 参数 `_Intensity=8`、`_Smooth=0.01`、`_Tint=#8B17FF`；`TeleportScheme.mat` 参数 `_Intensity=1`、`_State=0`、scan/glitch 参数；`PlayerLQTrail.mat` 的主色、边缘色与噪声参数；`Player.prefab` 中 `Ship_Sprite_HL`、`View`、TrailRenderer 的 material/sorting/time/width 接线。计划要求新建 GGReplica clean shader/material，由 Editor builder 接线，运行时用 `MaterialPropertyBlock` 调参，Auditor 检查材质链，且禁止修改 live `Ship.prefab` / `SampleScene.unity`。

---

## GGReplica Visual Parity Pass 1 材质链接入 — 2026-05-14 20:58

- **新建文件**
  - `Assets/_Art/Ship/GGReplica/Shaders/GGReplicaPlayerShipHighlight.shader`
  - `Assets/_Art/Ship/GGReplica/Shaders/GGReplicaTeleportScheme.shader`
  - `Assets/_Art/Ship/GGReplica/Shaders/GGReplicaPlayerLQTrail.shader`
  - `Assets/_Art/Ship/GGReplica/Materials/GGReplicaPlayerShipHL.mat`（Unity 生成）
  - `Assets/_Art/Ship/GGReplica/Materials/GGReplicaTeleportScheme.mat`（Unity 生成）
  - `Assets/_Art/Ship/GGReplica/Materials/GGReplicaPlayerLQTrail.mat`（Unity 生成）
  - `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaMaterialAssetBuilder.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaMaterialAssetBuilderTests.cs`
  - `Assets/Scripts/Ship/GGReplica/GGReplicaMaterialVisualModule.cs`
- **修改文件**
  - `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaPrefabBuilder.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaPrefabBuilderTests.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaAuditor.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaAuditorTests.cs`
  - `Assets/Scripts/Ship/GGReplica/GGReplicaPlayerViewAdapter.cs`
  - `Assets/Scripts/Ship/Tests/GGReplicaVisualModuleTests.cs`
  - `Assets/_Prefabs/Ship/Ship_GGReplica.prefab`
  - `Assets/Scenes/GGReplicaShipTest.unity`
  - `Docs/2_TechnicalDesign/Ship/ShipVFX_AssetRegistry.md`
  - `Docs/6_Diagnostics/GGReplica_ShipAsset_Audit.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：执行 GGReplica Visual Parity Pass 1，将 `Ship_GGReplica.prefab` 从纯贴图切换推进到原版 GG 关键材质链。新增三个 Project Ark-owned clean shader：`GGReplicaPlayerShipHighlight`、`GGReplicaTeleportScheme`、`GGReplicaPlayerLQTrail`，并通过 `GGReplicaMaterialAssetBuilder` 生成对应材质资产，参数来自解包证据。`GGReplicaPrefabBuilder` 现在自动构建材质，给 `Ship_Sprite_HL` 接 `GGReplicaPlayerShipHL.mat`、给 `View` 接 `GGReplicaTeleportScheme.mat`，并在 `ShipVisual/GGPlayerLQTrail` 创建 `TrailRenderer`，使用 `GGReplicaPlayerLQTrail.mat`、`time=0.4`、`widthMultiplier=4`。新增 `GGReplicaMaterialVisualModule`，通过 `MaterialPropertyBlock` 在 `ChangeViewState` 时驱动高光强度、View state、Dodge pulse 与 Boost trail emitting，不修改共享材质资产。Auditor 增加材质链检查，HL/View/Trail 退回默认材质会报错。
- **目的**：纠正之前 GGReplica 只完成 PlayerView 数据/层级但缺失 shader/material/VFX 的问题，让 A/B 测试船开始接近 Galactic Glitch 原版 `Player.prefab` 视觉链路。
- **技术**：TDD + Unity Editor automation。RED 证据：`GGReplicaMaterialAssetBuilderTests` 先因缺少 builder 编译失败；`GGReplicaPrefabBuilderTests` 先失败于 `Ship_Sprite_HL` 仍为 `Sprite-Lit-Default`；`GGReplicaVisualModuleTests` 先因缺少 `GGReplicaMaterialVisualModule` 失败；`GGReplicaAuditorTests` 先确认破坏 HL 材质链时 Auditor 不报错。GREEN 实现后验证：Visual Parity EditMode 4/4 通过；材质模块 PlayMode 1/1 通过；`GGReplicaPlayerViewAdapterTests` PlayMode 5/5 通过；菜单链路等价调用 `BuildVisualMaterials -> BuildPlayerSkinAsset -> BuildExperimentalPrefab -> BuildTestScene -> RunAudit` 返回 `errors=0`；测试场景实例检查为 `hlMat=GGReplicaPlayerShipHL/ProjectArk/GGReplica/PlayerShipHighlight`、`viewMat=GGReplicaTeleportScheme/ProjectArk/GGReplica/TeleportScheme`、`trailMat=GGReplicaPlayerLQTrail/ProjectArk/GGReplica/PlayerLQTrail time=0.4 width=4`、`Boost trailEmitting=True`；`dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 0 错误；Unity Console 清理后重新运行 Auditor 无 error/warning；`git diff --check` 通过。

---

## GGReplica Visual Parity Pass 2 动态状态反馈 — 2026-05-14 21:25

- **修改文件**
  - `Assets/Scripts/Ship/GGReplica/GGReplicaMaterialVisualModule.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaPrefabBuilder.cs`
  - `Assets/Scripts/Ship/Tests/GGReplicaVisualModuleTests.cs`
  - `Assets/_Art/Ship/GGReplica/Shaders/GGReplicaPlayerShipHighlight.shader`
  - `Assets/_Art/Ship/GGReplica/Shaders/GGReplicaTeleportScheme.shader`
  - `Assets/_Art/Ship/GGReplica/Shaders/GGReplicaPlayerLQTrail.shader`
  - `Assets/_Prefabs/Ship/Ship_GGReplica.prefab`
  - `Assets/Scenes/GGReplicaShipTest.unity`
  - `Docs/6_Diagnostics/GGReplica_ShipAsset_Audit.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：继续 GGReplica Visual Parity，在 Pass 1 的材质链接基础上加入更明确的运行时状态反馈。`GGReplicaMaterialVisualModule` 现在对 Boost/Dodge/Grab/Heal 分别写入 `_BoostAmount`、`_HealAmount`、`_SchemeAlpha`、`_GlitchStrength`、`_TrailIntensity`、`_EdgeBoost`、`_GrabEmphasis`、`_Pulse` 等 `MaterialPropertyBlock` 参数；`GGReplicaPrefabBuilder` 将 grab hand renderer 接入材质模块。三个 clean shader 扩展对应属性：PlayerShipHighlight 根据 Boost/Heal/Pulse/Grab 参数增强亮度和颜色，TeleportScheme 根据 SchemeAlpha/Glitch/Pulse 改变剪影动态，PlayerLQTrail 根据 TrailIntensity/EdgeBoost 增强拖尾能量。
- **目的**：让 `0-9/U` ViewState 切换产生 shader/material/VFX 层面的可见差异，尤其是 Boost trail、Dodge pulse、Grab hand emphasis、Heal tint，继续靠近 Galactic Glitch 原版 PlayerView 的模块化视觉表现，而不是停留在贴图替换。
- **技术**：TDD + MaterialPropertyBlock。先扩展 `GGReplicaVisualModuleTests.MaterialModule_ApplyState_UsesPropertyBlocksWithoutMutatingSharedMaterials`，RED 失败于缺少 `_grabRightRenderer`；随后实现材质模块和 shader 参数。验证结果：扩展材质模块 PlayMode 测试 1/1 通过；Visual Parity EditMode 测试 4/4 通过；`GGReplicaVisualModuleTests` + `GGReplicaPlayerViewAdapterTests` PlayMode 10/10 通过；重建链路 `BuildVisualMaterials -> BuildPlayerSkinAsset -> BuildExperimentalPrefab -> BuildTestScene -> RunAudit` 返回 `errors=0`；场景实例参数检查：`Boost intensity=12 boost=1 trail=1 emitting=True`，`Dodge pulse=0.7 corePulse=1`，`Grab emphasis=1`，`Heal amount=1 tint=0.35,1,0.85,1`；`dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 0 错误（仍有既有 warning）；Unity Console 清理后重新运行 Auditor 为 0 error / 0 warning。

---

## GGReplica V2 推倒重来审计基线 — 2026-05-14 21:59

- **新建文件**
  - `Docs/6_Diagnostics/GGReplica_V2_OriginalPlayerView_Audit.md`
- **修改文件**
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：响应用户指出当前 GGReplica 仍然只是换贴图、没有复刻 GG 基础 Glitch 飞船的真实状态表现的问题，停止继续在当前 `ChangeViewState -> state sprite pack` 路线上叠补丁。新增 V2 审计基线，明确外部真相源为 `/Users/dada/Documents/Quark/ReferenceAssets/Galactic Glitch` 下的 `DevXUnity`、`DevXUnity_exported`、`Scripts_ilspycmd`、`Scripts_dnSpyEx`。文档记录原版 `PlayerView` 的模块字段和 `Player.prefab` 中真实存在的 `ShapeTrailModule`、`LQTrailModule`、`DarkTrailModule`、`FluxySolver`、`FluxyGrabModule`、`LQTrailsContainer`、`vfx_boost_trail_loop_enhanced`、`vfx_boost_trail_burst_enhanced`、`ps_techno_flame_trail_R`、`ps_techno_flame_trail_quick`、`ps_techno_flame_trail_start`、`ps_ember_trail`、`startrails`、`startrails_long`、`ShapeShiftStateHitbox` 等证据。
- **目的**：将 GGReplica 目标从“ViewState 贴图表 + 少量材质参数”修正为“复刻原版 GG Glitch 基础飞船的 PlayerView 模块/VFX/输入/手感/状态链路”。V2 后续应新建 `Ship_GGReplicaV2.prefab` 与 `GGReplicaGlitchV2Test.unity`，以 Idle/Move/Boost Hold/Dodge Burst/Grab Hold/Heal 为主验收路径，而不是继续依赖 `0-9` 按钮换贴图。
- **技术**：系统化调试 + 原版证据审计。使用 `DevXUnity_exported/Assets/Prefab/Player.prefab`、`Scripts_ilspycmd/GeneralAssembly/PlayerView.cs`、`PlayerViewBoostModule.cs`、`PlayerViewCoreModule.cs`、`PlayerViewLQTrailModule.cs`、`PlayerViewFluxyTrailModule.cs`、`PlayerViewFluxyGrabModule.cs`、`PlayerViewShapeTrailModule.cs`、`PlayerViewTeleportModule.cs` 等作为 V2 设计输入。当前轮不再修改运行时代码，先冻结错误路线并建立正确 V2 实施基线。

---

## GGReplica V2 首个可玩复刻切片 — 2026-05-14 22:04

- **新建文件**
  - `Assets/Scripts/Ship/GGReplica/V2/GGReplicaGlitchState.cs`
  - `Assets/Scripts/Ship/GGReplica/V2/GGReplicaGlitchInputDriver.cs`
  - `Assets/Scripts/Ship/GGReplica/V2/GGReplicaGlitchMotor.cs`
  - `Assets/Scripts/Ship/GGReplica/V2/GGReplicaGlitchView.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/V2/GGReplicaGlitchV2PrefabBuilder.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/V2/GGReplicaGlitchV2PrefabBuilderTests.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/V2/GGReplicaGlitchV2TestSceneBuilder.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/V2/GGReplicaGlitchV2TestSceneBuilderTests.cs`
  - `Assets/Scripts/Ship/Tests/GGReplicaGlitchV2RuntimeTests.cs`
  - `Assets/_Prefabs/Ship/Ship_GGReplicaV2.prefab`
  - `Assets/Scenes/GGReplicaGlitchV2Test.unity`
- **修改文件**
  - `Docs/2_TechnicalDesign/Ship/ShipVFX_AssetRegistry.md`
  - `Docs/6_Diagnostics/GGReplica_V2_OriginalPlayerView_Audit.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：开始执行 GGReplica V2 推倒重来路线，创建完全隔离的 `Ship_GGReplicaV2.prefab` 与 `GGReplicaGlitchV2Test.unity`，不再沿用旧 `GGReplicaPlayerViewAdapter` / `GGReplicaTestSwitcher` 的 0-9 换贴图验证方式。V2 prefab 从零构建，包含 `GGReplicaGlitchInputDriver`、`GGReplicaGlitchMotor`、`GGReplicaGlitchView`，以及 `GGGlitchVisualRoot` 下的 `BodyLayers`、`CoreModule`、`BoostModule`、`LQTrailModule`、`LQTrailsContainer`、`ShapeTrailModule`、`DarkTrailModule`、`FluxySolver`、`FluxyGrabModule`、`GrabModule`、`HealModule`、`DodgeModule`、`FireAimModule`、`ShapeShiftStateHitbox`、`vfx_boost_trail_loop_enhanced`、`vfx_boost_trail_burst_enhanced`、`ps_techno_flame_trail_R`、`ps_techno_flame_trail_quick`、`ps_techno_flame_trail_start`、`ps_ember_trail`、`startrails`、`startrails_long` 等原版 PlayerView 模块/VFX 对应节点。测试场景通过 WASD/方向键、Shift、Space、E、Q、鼠标左键驱动 Move/Boost/Dodge/Grab/Heal/FireAim，不再要求用户点击按钮。
- **目的**：停止在错误的“ViewState 贴图表 + 少量材质参数”路线叠补丁，建立一个真正以 GG 原版基础 Glitch 飞船手感、输入、状态和 PlayerView 模块栈为中心的复刻入口。
- **技术**：TDD + Editor automation。先新增 V2 PrefabBuilder/TestSceneBuilder/Runtime tests，RED 阶段确认缺少 V2 builder/runtime 类型；随后实现 runtime 输入帧、状态机、Rigidbody2D 运动/Boost/Dodge、模块化 View root toggle、trail/particle stack，以及两个 Editor builder。验证结果：V2 editor tests 2/2 通过；V2 runtime tests 2/2 通过；正式构建 `Ship_GGReplicaV2.prefab` 和 `GGReplicaGlitchV2Test.unity`，构建摘要 `trails=5`、`particles=8`；`dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 0 错误。当前仍是 V2 第一可玩切片，下一步需要继续按原版 `Player.prefab` 序列化参数细化粒子/trail 形态与时序。

---

## GGReplica V2 原版手感参数接入 — 2026-05-14 22:35

- **修改文件**
  - `Assets/Scripts/Ship/GGReplica/V2/GGReplicaGlitchMotor.cs`
  - `Assets/Scripts/Ship/GGReplica/GGReplicaShipFeelProfileSO.cs`
  - `Assets/_Data/Ship/GGReplicaShipFeelProfile.asset`
  - `Assets/Scripts/Ship/Editor/GGReplica/V2/GGReplicaGlitchV2PrefabBuilder.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/V2/GGReplicaGlitchV2PrefabBuilderTests.cs`
  - `Assets/Scripts/Ship/Tests/GGReplicaGlitchV2RuntimeTests.cs`
  - `Assets/Scripts/Ship/Tests/GGReplicaShipProfileTests.cs`
  - `Assets/_Prefabs/Ship/Ship_GGReplicaV2.prefab`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：将 GGReplica V2 的 Boost/Dodge 从占位参数切换为 `GGReplicaShipFeelProfileSO` 驱动。新增 `DodgeStateDuration=0.225` 与 `DodgeLinearDamping=1.7` 配置；V2 Motor 现在使用原版 Glitch 调研参数：Boost 持续倍率 `1.2`、启动冲量 `4`、Boost 阻尼 `2.5`、Dodge 冲量 `13`、Dodge 最小状态时长 `0.225`，并在 Boost/Dodge 退出时恢复基础阻尼。V2 Prefab Builder 显式加载并写入 `Assets/_Data/Ship/GGReplicaShipFeelProfile.asset`。
- **目的**：继续纠正 V2 第一切片中“可动但手感仍是占位”的问题，让 Shift/Space 的变速、冲量与状态停留更接近 Galactic Glitch 原版基础 Glitch 飞船。
- **技术**：TDD + Unity MCP。RED：新增 `GGReplicaGlitchV2RuntimeTests` 两个 PlayMode 测试，先失败于 `GGReplicaGlitchMotor` 缺少 `_feelProfile` 字段；随后实现 Profile 接入和参数读取。验证结果：`GGReplicaGlitchV2RuntimeTests` PlayMode 4/4 通过；`GGReplicaGlitchV2PrefabBuilderTests.BuildPrefab_CreatesOriginalPlayerViewModuleRigAndInputRuntime` EditMode 1/1 通过；`GGReplicaShipProfileTests.FeelProfile_DefaultValues_MatchInitialGGReplicaTuning` PlayMode 1/1 通过；`dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 0 错误；`git diff --check` 通过。

---

## GGReplica V2 Boost/Dodge 视觉时序 — 2026-05-14 23:02

- **修改文件**
  - `Assets/Scripts/Ship/GGReplica/V2/GGReplicaGlitchView.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/V2/GGReplicaGlitchV2PrefabBuilder.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/V2/GGReplicaGlitchV2PrefabBuilderTests.cs`
  - `Assets/Scripts/Ship/Tests/GGReplicaGlitchV2RuntimeTests.cs`
  - `Assets/_Prefabs/Ship/Ship_GGReplicaV2.prefab`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：继续推进 GGReplica V2 的原版 PlayerView 复刻，将 `GGReplicaGlitchView` 从“状态切换时立即开关粒子”推进到 Boost/Dodge 分阶段视觉时序。Boost 进入时现在播放一次点火 burst，并在 `GGReplicaShipFeelProfileSO.BoostIgniteDuration` 窗口结束后停止 burst，同时保留 sustain 粒子和 trail；Dodge 进入时播放 Dodge burst，并在 `DodgeStateDuration` 窗口内衰减 `Dodge_Sprite` ghost alpha 与 core scale，窗口结束后清空 burst。Builder 现在为 View 写入 Feel Profile，并区分 `_boostBurstParticles` 与 `_dodgeBurstParticles`，避免 Boost 和 Dodge 粒子串用。
- **目的**：回应“继续做 V2 的视觉时序”，让 Shift Boost 与 Space Dodge 不再只是静态根节点开关，而是更接近 GG 原版 `PlayerViewBoostModule.OnBoostStart` 与 `PlayerViewFluxyTrailModule.OnDodgeStart/End` 的事件式视觉节奏。
- **技术**：TDD + Unity 运行时计时。新增 PlayMode 规格测试 `View_BoostIgnition_PlaysBurstOnlyDuringIgniteWindowThenKeepsSustain` 与 `View_DodgeVisuals_FadeGhostAndStopBurstAfterDodgeWindow`，以反射调用 `TickVisuals(float)` 验证时序窗口。实现使用 `Update()` 推进轻量计时器，不新增 Coroutine；粒子停止使用 `StopEmittingAndClear` 防止 one-shot 残留。验证结果：`dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 0 错误；`git diff --check` 通过。Unity MCP 当前处于 disconnected/connecting，命令行 Unity 测试因已有 Editor 实例打开同一项目被拒绝，PlayMode 测试需在 Unity MCP 恢复后补跑。

---

## GGReplica V2 DevXUnity Fake Fluxy Trail — 2026-05-14 23:25

- **新建文件**
  - `Assets/_Art/Ship/GGReplica/Shaders/GGReplicaFakeFluxy.shader`
  - `Assets/_Art/Ship/GGReplica/Materials/GGReplicaFakeFluxy.mat`（Unity 自动生成 `.meta`）
- **修改文件**
  - `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaMaterialAssetBuilder.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaMaterialAssetBuilderTests.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/V2/GGReplicaGlitchV2PrefabBuilder.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/V2/GGReplicaGlitchV2PrefabBuilderTests.cs`
  - `Assets/_Prefabs/Ship/Ship_GGReplicaV2.prefab`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：按用户要求将本轮参考重点切回 `DevXUnity/`，从 `DevXUnity/Material/vfx_helping hand_fake_fluxy.mat.meta` 提取 fake Fluxy 材质参数，新增 Project Ark-owned `GGReplicaFakeFluxy.shader` 与 `GGReplicaFakeFluxy.mat`。材质参数接入 DevXUnity 证据：`_BaseColor=(0,0,0,0)`、`_GlowColor=(1.1607844,0,2.9960785,0)`、`_DistortionOffset=-1`、`_DepthOffset=-0.3`、`_NoiseScale=6`、`_RimWidth=0.07`、`_FlowPower=3.77`。V2 Prefab Builder 现在将 `LQTrailModule/fluxy_like_lq_trail` 从通用 `GGReplicaPlayerLQTrail` 切换为 `GGReplicaFakeFluxy`。
- **目的**：让 V2 的 Fluxy-like trail 不再只是普通 LQTrail 的重复线条，而是开始按 DevXUnity 中 `vfx_helping hand_fake_fluxy` 的材质证据还原 GG 原版 Glitch 飞船/抓取相关的液体拖尾观感。
- **技术**：TDD + Unity Editor automation。RED：`GGReplicaMaterialAssetBuilderTests.BuildVisualMaterials_CreatesMaterialsWithOriginalGGParameters` 先失败于 `GGReplicaFakeFluxy.mat` 缺失；随后新增 shader/material builder 逻辑并更新 V2 Prefab Builder。验证结果：材质 Builder EditMode 1/1 通过；V2 Prefab Builder EditMode 1/1 通过；V2 Runtime PlayMode 6/6 通过；`dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 0 错误；`git diff --check` 通过。

---

## GGReplica V2 DevXUnity EngineTrail Boost 材质 — 2026-05-14 23:45

- **新建文件**
  - `Assets/_Art/Ship/GGReplica/Shaders/GGReplicaEngineTrail.shader`
  - `Assets/_Art/Ship/GGReplica/Materials/GGReplicaEngineTrail.mat`（Unity 自动生成 `.meta`）
- **修改文件**
  - `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaMaterialAssetBuilder.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaMaterialAssetBuilderTests.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/V2/GGReplicaGlitchV2PrefabBuilder.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/V2/GGReplicaGlitchV2PrefabBuilderTests.cs`
  - `Assets/_Prefabs/Ship/Ship_GGReplicaV2.prefab`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：继续按 `DevXUnity/` 作为主要参考源，从 `DevXUnity/Material/EngineTrail1.mat.meta` 提取 Boost/engine trail 材质参数，新增 Project Ark-owned `GGReplicaEngineTrail.shader` 与 `GGReplicaEngineTrail.mat`。接入参数包括 `BottomColor=(1,0,0.91456747,1)`、`TopColor=(0.0990566,0.8460265,1,1)`、`GhostColor=(0.09211465,0.8490566,0.6468398,0.3882353)`、`MixEffect=1`、`NoiseScale=1.31`、`Spread=2`、`Power=7`、`WobbleSpeed=0.4`、三层速度向量 `(-2,0,1,1)` / `(-1,0,1,1)` / `(-1.61,0,1,0.51)`。V2 Prefab Builder 现在把 `vfx_boost_trail_loop_enhanced`、`vfx_boost_trail_burst_enhanced`、`ps_techno_flame_trail_R`、`ps_techno_flame_trail_quick`、`ps_techno_flame_trail_start` 的 `ParticleSystemRenderer.sharedMaterial` 指向 `GGReplicaEngineTrail`。
- **目的**：让 V2 Boost flame/trail 不再使用默认粒子材质或仅靠 `startColor`，而是开始按 DevXUnity 中 `EngineTrail1` 的 shader graph 参数还原 GG 原版 Boost 火焰拖尾的青紫渐变、ghost 混色与多层流动噪声。
- **技术**：TDD + Unity Editor automation。RED：材质 Builder 测试先失败于 `GGReplicaEngineTrail.mat` 缺失；随后新增 shader/material builder 逻辑与 V2 Prefab Builder 粒子材质接线。验证结果：材质 Builder EditMode 1/1 通过；V2 Prefab Builder EditMode 1/1 通过；V2 Runtime PlayMode 6/6 通过；`dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 0 错误；`git diff --check` 通过。

---

## GGReplica V2 DevXUnity DodgeParticles 材质 — 2026-05-14 23:55

- **新建文件**
  - `Assets/_Art/Ship/GGReplica/Shaders/GGReplicaDodgeParticles.shader`
  - `Assets/_Art/Ship/GGReplica/Materials/GGReplicaDodgeParticles.mat`（Unity 自动生成 `.meta`）
- **修改文件**
  - `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaMaterialAssetBuilder.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaMaterialAssetBuilderTests.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/V2/GGReplicaGlitchV2PrefabBuilder.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/V2/GGReplicaGlitchV2PrefabBuilderTests.cs`
  - `Assets/_Prefabs/Ship/Ship_GGReplicaV2.prefab`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：继续按 `DevXUnity/` 作为主要参考源，从 `DevXUnity/Material/DodgeParticlesNew.mat.meta` 提取 Dodge 粒子材质参数，新增 Project Ark-owned `GGReplicaDodgeParticles.shader` 与 `GGReplicaDodgeParticles.mat`。接入参数包括 `_Color=(1,0.78035855,0,1)`、`_TintColor=(1,1,1,1)`、`_EmissionColor=(0,0,0,1)`、`_InvFade=3`、`_SrcBlend=1`、`_DstBlend=0`、`_ZWrite=1`。V2 Prefab Builder 现在把 `DodgeModule/ps_dodge_shell` 的 `ParticleSystemRenderer.sharedMaterial` 指向 `GGReplicaDodgeParticles`。
- **目的**：让 V2 Dodge shell 不再只靠紫色 `startColor` 和默认粒子材质，而是按 DevXUnity 中 `DodgeParticlesNew` 的材质证据还原 GG 原版 Dodge 粒子的橙金色硬边 shell 质感。
- **技术**：TDD + Unity Editor automation。RED：材质 Builder 测试先失败于 `GGReplicaDodgeParticles.mat` 缺失；随后新增 shader/material builder 逻辑与 V2 Prefab Builder Dodge 粒子材质接线。验证结果：材质 Builder EditMode 1/1 通过；V2 Prefab Builder EditMode 1/1 通过；V2 Runtime PlayMode 6/6 通过；`dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 0 错误；`git diff --check` 通过。

---

## GGReplica V2 DevXUnity Boost/Dodge 音效时序 — 2026-05-15 00:17

- **新建文件**
  - `Assets/Scripts/Ship/GGReplica/V2/GGReplicaGlitchAudioFeedback.cs`（Unity 自动生成 `.meta`）
- **修改文件**
  - `Assets/Scripts/Ship/GGReplica/V2/GGReplicaGlitchMotor.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/V2/GGReplicaGlitchV2PrefabBuilder.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/V2/GGReplicaGlitchV2PrefabBuilderTests.cs`
  - `Assets/Scripts/Ship/Tests/GGReplicaGlitchV2RuntimeTests.cs`
  - `Assets/_Prefabs/Ship/Ship_GGReplicaV2.prefab`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：继续按 `DevXUnity/AudioClip` 作为主要参考源，将 V2 的 Boost/Dodge 原版音效接入输入驱动状态机。新增 `GGReplicaGlitchAudioFeedback`，从 `GGReplicaShipVisualProfileSO` 读取已导入的 `SND_PLAYER_BOOST_IGNITE.wav`、`SND_PLAYER_BOOST.wav`、`PLAYER_DODGE.wav`：进入 `BoostHold` 时播放 ignite one-shot 并启动 loop；退出 Boost 或进入 Dodge 时停止 loop；进入 `DodgeBurst` 时播放 Dodge one-shot。`GGReplicaGlitchMotor` 现在在状态切换时同步通知 `GGReplicaGlitchAudioFeedback`。V2 Prefab Builder 自动添加 `AudioSource` 与音频反馈组件，并接入 `GGReplicaShipVisualProfile.asset`。
- **目的**：让 V2 的操作反馈从“只有视觉变化”推进到 GG 原版输入节奏：Shift Boost 有点火声与持续推进声，Space Dodge 有独立 Dodge 声，进一步接近原版基础 Glitch 飞船手感。
- **技术**：TDD + Unity MCP。RED：先新增 `Motor_StateTransitions_NotifyV2AudioFeedback` 与 Builder 接线断言，编译失败于缺少 `GGReplicaGlitchAudioFeedback`；随后实现组件、Motor 通知与 Prefab 接线。验证结果：V2 Runtime PlayMode 7/7 通过；V2 Prefab Builder EditMode 1/1 通过；`dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 0 错误；Unity Console 无新增测试失败错误。

---

## GGReplica V2 DevXUnity FireAim 音效接入 — 2026-05-15 00:37

- **修改文件**
  - `Assets/Scripts/Ship/GGReplica/V2/GGReplicaGlitchAudioFeedback.cs`
  - `Assets/Scripts/Ship/Tests/GGReplicaGlitchV2RuntimeTests.cs`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：继续按 `DevXUnity/AudioClip` 作为主要参考源，将 V2 `FireAim` 状态接入原版射击音效。测试中锁定 `PLAYER_NORMAL_SHOT.wav` 作为 `GGReplicaShipVisualProfileSO.FireClip`；`GGReplicaGlitchAudioFeedback` 现在进入 `FireAim` 时会停止 Boost loop，并播放 `FireClip` one-shot，避免射击瞄准态继续残留 Boost 推进声。
- **目的**：补齐 V2 的基础攻击/瞄准状态听觉反馈，让鼠标左键 `FireAim` 不再只有视觉/速度变化，开始对齐 GG 原版基础 Glitch 飞船的射击反馈。
- **技术**：TDD。RED：`Motor_StateTransitions_NotifyV2AudioFeedback` 先失败于进入 `FireAim` 后 `_lastOneShotClip` 仍为 `SND_PLAYER_BOOST_IGNITE`，未播放 `PLAYER_NORMAL_SHOT`；GREEN：在 `GGReplicaGlitchAudioFeedback.ApplyState` 中新增 `FireAim` 分支，停止 loop 并播放 `FireClip`。验证结果：`dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 0 错误；Unity MCP 当前返回 `no_unity_session`，命令行 Unity PlayMode 测试未生成结果文件，因此 FireAim PlayMode 绿色验证需在 Unity 会话恢复后补跑。


## ChargeRusher 敌人行为复刻 - 2026-05-15 00:47

- **新建/修改文件：**
  - `Assets/Scripts/Combat/Enemy/AttackDataSO.cs`
  - `Assets/Scripts/Combat/Enemy/ChargeRusherBrain.cs`
  - `Assets/Scripts/Combat/Enemy/States/ChargeState.cs`
  - `Assets/Scripts/Combat/Enemy/States/ChaseState.cs`
  - `ProjectArk.Enemy.csproj`
- **内容：** 新增 `AttackType.Charge` 与 Charge 参数，新增 `ChargeRusherBrain` 和 `ChargeState`，将近身攻击入口从普通 `EngageState` 路由到专用冲刺状态，复刻 Minishoot 的暂停蓄力抖动、锁定方向、高速冲刺、撞墙/超时恢复流程。
- **目的：** 提供一只可读、可躲、可惩罚的 Signal-Window 型冲撞敌，作为 Minishoot 敌人迁移的第一个 Project Ark 原生原型。
- **技术：** 复用现有 HFSM、`EnemyEntity`、`EnemyPerception`、`EnemyDirector` 与 `DamagePayload` 统一伤害管线；数值放入 `AttackDataSO`；蓄力视觉退出时恢复 Sprite 原始颜色与本地坐标，避免状态泄漏。
- **验证：** `read_lints` 检查新增/修改脚本无错误；`dotnet build Project-Ark.slnx` 编译成功，剩余警告为项目既有警告，非本次改动引入。

---

## GGReplica V2 DevXUnity Grab hand 视觉接入 — 2026-05-15 01:01

- **修改文件**
  - `Assets/Scripts/Ship/GGReplica/V2/GGReplicaGlitchView.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/V2/GGReplicaGlitchV2PrefabBuilder.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/V2/GGReplicaGlitchV2PrefabBuilderTests.cs`
  - `Assets/Scripts/Ship/Tests/GGReplicaGlitchV2RuntimeTests.cs`
  - `Assets/_Prefabs/Ship/Ship_GGReplicaV2.prefab`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：继续按 `DevXUnity/` 作为主要参考源推进 V2 Grab 状态复刻。参考 `DevXUnity/Sprite/GrabGun_Hand_d7.png` 与 `DevXUnity/Material/vfx_helping hand_fake_fluxy.mat.meta`，将 V2 `GrabModule` 的左右 `Ship_Sprite_Solid_Grab_R/L` 改为使用 `GGReplicaFakeFluxy` 材质；`GGReplicaGlitchView` 新增 `_grabRenderers`，进入 `GrabHold` 时缓存闭合位置并将双手向外展开、轻微上移、放大到 `1.15x`，退出 Grab 时恢复原始位置和缩放。V2 Prefab Builder 自动写入 `_grabRenderers`，现有 `Ship_GGReplicaV2.prefab` 也同步了材质与序列化引用。
- **目的**：让 `E` Grab 状态不再只是显示两张手部贴图，而是开始有基于 DevXUnity fake Fluxy 材质的能量手视觉和明确的展开/收回状态反馈。
- **技术**：TDD。新增 PlayMode 规格测试 `View_GrabHold_ExtendsHandsWithFakeFluxyEmphasisAndRestoresOnExit`，要求 Grab hands 展开、放大并在退出时恢复；更新 V2 Prefab Builder 测试，要求 Grab hands 使用 `GGReplicaFakeFluxy` 且 `_grabRenderers` 数组接线为 2。验证结果：`dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 0 错误；`git diff --check` 通过。Unity MCP 当前仍返回 `no_unity_session`，Grab PlayMode/EditMode 测试需 Unity 会话恢复后补跑。

---

## ChargeRusher 资产自动化接入 - 2026-05-15 10:07

- **修改文件：**
  - `Assets/Scripts/Combat/Editor/EnemyAssetCreator.cs`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容：** 在 `EnemyAssetCreator` 中新增 `ProjectArk/Create ChargeRusher Enemy Assets` 菜单入口，可一键创建/复用 `ChargeRusherCharge.asset`、`EnemyStats_ChargeRusher.asset` 与 `Enemy_ChargeRusher.prefab`。生成的 Prefab 自动配置 `SpriteRenderer`、`Rigidbody2D`、`CircleCollider2D`、`EnemyEntity`、`EnemyPerception` 与 `ChargeRusherBrain`，并写入 Player/Wall 感知 LayerMask。
- **目的：** 将上一阶段完成的 `ChargeRusherBrain` / `ChargeState` 行为代码推进到 Unity 可玩闭环，降低后续敌人资产创建成本，让测试房间可以快速拖入 ChargeRusher 做 Play Mode 手感调参。
- **技术：** 复用现有 Editor 资产生成工具链与 `AttackDataSO` 数据驱动模式；Charge 数值全部写入 `AttackDataSO` / `EnemyStatsSO`，包括 `ChargeSpeed=12`、`ChargeMaxDuration=0.6`、`ChargeAnticipation=0.08`、`TelegraphDuration=0.45`、`RecoveryDuration=0.75`，避免运行时代码 hardcode。
- **验证：** `read_lints` 检查 `EnemyAssetCreator.cs` 无错误；`dotnet build ProjectArk.Combat.Editor.csproj /p:BuildProjectReferences=false` 成功，剩余 4 个警告来自既有 `EchoWaveProceduralPreviewMenu` 过时 API。全项目 `dotnet build Project-Ark.slnx` 当前被既有 `GGReplicaShipVisualProfileSO.HealClip` 缺失错误阻塞，非本次改动引入。

---

## GGReplica V2 DevXUnity Heal Hold 反馈接入 — 2026-05-15 10:14

- **修改文件**
  - `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaAssetImporter.cs`
  - `Assets/Scripts/Ship/GGReplica/GGReplicaShipVisualProfileSO.cs`
  - `Assets/Scripts/Ship/GGReplica/V2/GGReplicaGlitchAudioFeedback.cs`
  - `Assets/Scripts/Ship/GGReplica/V2/GGReplicaGlitchView.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/V2/GGReplicaGlitchV2PrefabBuilder.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/V2/GGReplicaGlitchV2PrefabBuilderTests.cs`
  - `Assets/Scripts/Ship/Tests/GGReplicaGlitchV2RuntimeTests.cs`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：继续按 `DevXUnity/` 作为主要参考源补齐 V2 `Heal` 状态反馈。`GGReplicaShipVisualProfileSO` 新增 `_healClip` / `HealClip`，`GGReplicaGlitchAudioFeedback` 在进入 `Heal` 时停止 Boost loop 并播放治疗音效；`GGReplicaAssetImporter` 将 `DevXUnity/AudioClip/PlayerHealingProgress.wav` 纳入 curated audio 列表。`GGReplicaGlitchView` 新增 `_healRenderers`、`_healParticles` 与 Heal pulse 计时，进入 Heal 时启用 `Healing_0` / `vfx_dot_001` 青绿色缩放脉冲并播放专用 Heal 粒子，退出时复位 sprite/particle，且不再串用 Boost ignition burst。V2 Prefab Builder 创建并接线 `HealModule/Healing_0`、`HealModule/vfx_dot_001` 与 `ps_glitch_heal`。
- **目的**：让 `Q` Heal Hold 不再只是打开一个空的 `HealModule` 根节点或染色机体，而是具备可听、可见、可复位的治疗状态反馈，继续靠近 GG 原版基础 Glitch 飞船的 Heal ViewState 感知。
- **技术**：TDD。RED：先扩展 `GGReplicaGlitchV2RuntimeTests`，通过 `dotnet build Project-Ark.slnx` 验证失败于 `GGReplicaShipVisualProfileSO` 缺少 `HealClip`。GREEN：补齐 Profile API、音频状态分支、Heal sprite/particle runtime 字段与 Prefab Builder 接线。验证结果：`dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 0 错误（86 个既有/生成 warning）；相关脚本 `read_lints` 0 条；`git diff --check` 通过。Unity MCP 本轮仍超时 / `no_unity_session`，命令行 Unity 因已有 Editor 实例打开同一项目被拒绝，因此 V2 PlayMode/EditMode focused tests、Importer 执行与 `Ship_GGReplicaV2.prefab` 实体重建需在 Unity 会话恢复后补跑。

---

## GGReplica V2 Heal Hold Unity 重建与验证 — 2026-05-15 11:14

- **新建文件**
  - `Assets/_Art/Ship/GGReplica/Audio/PlayerHealingProgress.wav`（及 Unity 生成 `.meta`）
- **修改文件**
  - `Assets/_Data/Ship/GGReplicaShipVisualProfile.asset`
  - `Assets/_Prefabs/Ship/Ship_GGReplicaV2.prefab`
  - `Assets/Scenes/GGReplicaGlitchV2Test.unity`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：Unity MCP 恢复后，执行 GGReplica curated importer，将 `PlayerHealingProgress.wav` 真正导入到 `Assets/_Art/Ship/GGReplica/Audio/`；通过 `SerializedObject` 将 `GGReplicaShipVisualProfile.asset._healClip` 指向该音频；重新运行 `BuildVisualMaterials`、`Build Glitch V2 Prefab` 与 `Build Glitch V2 Test Scene`，把 Heal sprite/particle/audio 接线写入实际 Prefab 和测试场景。
- **目的**：闭环上一条 Heal Hold 代码实现，确保 Unity 资产、SO、Prefab、测试场景都消费同一条 Heal 反馈链，而不是停留在代码可编译但 Prefab 未重建的半完成状态。
- **技术**：Unity MCP `execute_code` 执行 Editor 侧导入/回填/重建；随后运行 focused Unity Test Runner。验证结果：EditMode `GGReplicaGlitchV2PrefabBuilderTests`、`GGReplicaGlitchV2TestSceneBuilderTests`、`GGReplicaMaterialAssetBuilderTests` 共 3/3 通过；PlayMode `GGReplicaGlitchV2RuntimeTests` 9/9 通过；Console 清理后仅剩 TestRunner 普通日志，无 error/warning；`dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 0 错误 0 警告；`git diff --check` 通过。Unity 自动改动的 `ProjectSettings/EditorSettings.asset` 已还原，Prefab 中空 `m_Name` 尾随空格已清理。

---

## GGReplica V2 FireAim 主攻击视觉层接入 — 2026-05-15 11:30

- **修改文件**
  - `Assets/Scripts/Ship/GGReplica/V2/GGReplicaGlitchView.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/V2/GGReplicaGlitchV2PrefabBuilder.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/V2/GGReplicaGlitchV2PrefabBuilderTests.cs`
  - `Assets/Scripts/Ship/Tests/GGReplicaGlitchV2RuntimeTests.cs`
  - `Assets/_Prefabs/Ship/Ship_GGReplicaV2.prefab`
  - `Assets/Scenes/GGReplicaGlitchV2Test.unity`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：继续推进 GGReplica V2 原版 Glitch 飞船状态反馈，为鼠标左键 `FireAim` 接入可见主攻击视觉层。参考 `DevXUnity_exported/Assets/Prefab/Player.prefab` 中 `MainAttackState`、`MainAttackFireState`、`MainAttackStateHitbox` 与 `GlitchEnergyReadyParticles (weapon once)` 命名证据，`GGReplicaGlitchView` 新增 `_fireAimRenderers`、`_fireAimParticles` 与 FireAim pulse 计时；进入 `FireAim` 时启用 primary attack sprite 层、粉紫色缩放脉冲和专用 weapon once 粒子，退出时复位 renderer/particle。`ApplyBurstParticles` 不再让 FireAim 复用 Boost ignition burst。V2 Prefab Builder 创建并接线 FireAimModule 下的三个 MainAttack sprite 层与专用粒子。
- **目的**：让 `FireAim` 不再只是机体染粉和播放射击音效，而是拥有独立的主攻击视觉模块，继续把 V2 从“输入状态能切换”推进到“每个 GG 基础状态都有可读的模块反馈”。
- **技术**：TDD。RED：新增 `View_FireAim_ShowsPrimaryAttackLayersAndDedicatedShotParticles`，Unity PlayMode 先失败于 `GGReplicaGlitchView` 缺少 `_fireAimRenderers` 字段；Prefab Builder 测试先要求 `FireAimModule/MainAttackState`、`MainAttackFireState`、`MainAttackStateHitbox`、`GlitchEnergyReadyParticles (weapon once)` 和对应序列化数组。GREEN：实现 FireAim runtime 字段、脉冲逻辑和 Builder 接线，并重建 `Ship_GGReplicaV2.prefab` / `GGReplicaGlitchV2Test.unity`。验证结果：新增 FireAim PlayMode 单测 1/1 通过；V2 EditMode focused tests 3/3 通过；V2 Runtime PlayMode tests 10/10 通过；Console 无 error/warning；`dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 0 错误 0 警告；`git diff --check` 通过。Unity 自动改动的 `ProjectSettings/EditorSettings.asset` 已还原，Prefab 空 `m_Name` 尾随空格已清理。

---

## GGReplica V2 Aim Direction 朝向旋转修复 — 2026-05-15 11:49

- **修改文件**
  - `Assets/Scripts/Ship/GGReplica/V2/GGReplicaGlitchState.cs`
  - `Assets/Scripts/Ship/GGReplica/V2/GGReplicaGlitchInputDriver.cs`
  - `Assets/Scripts/Ship/GGReplica/V2/GGReplicaGlitchMotor.cs`
  - `Assets/Scripts/Ship/Tests/GGReplicaGlitchV2RuntimeTests.cs`
  - `Assets/_Prefabs/Ship/Ship_GGReplicaV2.prefab`
  - `Assets/Scenes/GGReplicaGlitchV2Test.unity`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：修复用户反馈“sprite 不会随着朝向转向”的根因。V2 原先的 `GGReplicaGlitchInputFrame` 只有 WASD 移动和状态按钮，没有瞄准方向；`GGReplicaGlitchMotor` 只写 `linearVelocity` 和状态，不写 `Rigidbody2D.rotation` / `angularVelocity`，因此整套 `GGGlitchVisualRoot` 虽然挂在飞船根节点下，但根节点本身永远不转。现在输入帧新增 `AimDirection`，`GGReplicaGlitchInputDriver` 从 `Mouse.current` + `Camera.main.ScreenToWorldPoint` 计算鼠标世界方向，缺相机时回退到移动方向；`GGReplicaGlitchMotor` 记录 `_lastAimDirection`，在 `FixedUpdate` 中用与现役 `ShipAiming` 对齐的 GGSteering 角速度模型转向，并显式更新 `Rigidbody2D` 旋转，让 PlayerView sprites 随朝向可见旋转。WASD 移动仍保持世界空间，不被瞄准方向污染。
- **目的**：把 V2 从“状态特效在静态机体上播放”修正为“基础 Glitch 飞船随鼠标/瞄准方向转向”的必要行为，靠近原版 GG 鼠标模式的 WASD 移动 + 鼠标朝向分离手感。
- **技术**：系统化调试 + TDD。先确认代码链路缺少 aim 数据流：InputDriver 不采集鼠标方向、InputFrame 无 aim 字段、Motor 无旋转逻辑。RED：新增 `Motor_AimDirection_RotatesShipTowardAimIndependentlyFromMove`，先通过 `dotnet build` 失败于 `GGReplicaGlitchInputFrame` 缺少 7 参数构造，补字段后 Unity PlayMode 再失败于 `Rigidbody2D.rotation` 仍为 0。GREEN：实现 aimDirection 输入、GGSteering 式角速度旋转和 `MoveRotation` / `rotation` 更新。验证结果：新增朝向 PlayMode 单测 1/1 通过；V2 Runtime PlayMode tests 11/11 通过；V2 Prefab/Scene EditMode tests 2/2 通过；Console 无 error/warning；`dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 0 错误（86 个既有/生成 warning）；`git diff --check` 通过。重建 `Ship_GGReplicaV2.prefab` / `GGReplicaGlitchV2Test.unity` 后已清理 Prefab 空 `m_Name` 尾随空格。


## Minishoot Charged Rusher ReferenceOnly 原型资产接入 — 2026-05-15 12:21

- **新建/修改文件**：
  - `Assets/_ReferenceOnly/Minishoot/ChargedRusher/Sprites/ChargerT1S1.png`（及 Unity 生成 `.meta`）
  - `Assets/_ReferenceOnly/Minishoot/ChargedRusher/Sprites/ChargerT1S2.png`（及 Unity 生成 `.meta`）
  - `Assets/_ReferenceOnly/Minishoot/ChargedRusher/Sprites/ChargerT1S3.png`（及 Unity 生成 `.meta`）
  - `Assets/_ReferenceOnly/Minishoot/ChargedRusher/Sprites/ChargerT2S1.png`（及 Unity 生成 `.meta`）
  - `Assets/_ReferenceOnly/Minishoot/ChargedRusher/Sprites/ChargerT2S2.png`（及 Unity 生成 `.meta`）
  - `Assets/_ReferenceOnly/Minishoot/ChargedRusher/Sprites/ChargerT2S3.png`（及 Unity 生成 `.meta`）
  - `Assets/_ReferenceOnly/Minishoot/ChargedRusher/Sprites/ChargerT3S1.png`（及 Unity 生成 `.meta`）
  - `Assets/_ReferenceOnly/Minishoot/ChargedRusher/Sprites/ChargerT3S2.png`（及 Unity 生成 `.meta`）
  - `Assets/_ReferenceOnly/Minishoot/ChargedRusher/Sprites/ChargerT3S3.png`（及 Unity 生成 `.meta`）
  - `Assets/_ReferenceOnly/Minishoot/ChargedRusher/Sprites/Overcharge.png`（及 Unity 生成 `.meta`）
  - `Assets/_ReferenceOnly/Minishoot/ChargedRusher/Audio/EnergyRecharged.ogg`（及 Unity 生成 `.meta`）
  - `Assets/_ReferenceOnly/Minishoot/ChargedRusher/Audio/EnergyRechargedB.ogg`（及 Unity 生成 `.meta`）
  - `Assets/_ReferenceOnly/Minishoot/ChargedRusher/Audio/EnergyRechargedPartially.ogg`（及 Unity 生成 `.meta`）
  - `Assets/_ReferenceOnly/Minishoot/ChargedRusher/Prefabs/Enemy_ChargeRusher_REF_Minshoot.prefab`（及 Unity 生成 `.meta`）
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：将 Minishoot Charged Rusher 最小 Sprite/Audio 资产集导入到 `Assets/_ReferenceOnly/Minishoot/ChargedRusher/` 隔离目录。未复制外部 `.meta`，由 Unity 在 Project Ark 内自动生成 GUID 和导入设置。基于现有 `Enemy_ChargeRusher.prefab` 创建 `Enemy_ChargeRusher_REF_Minshoot.prefab` ReferenceOnly 副本，保留现有 `EnemyEntity`、`EnemyPerception`、`ChargeRusherBrain` 与 `EnemyStats_ChargeRusher` 行为链，只替换根 `SpriteRenderer` 为 Minishoot Charger 贴图并添加临时 `AudioSource`。
- **目的**：在不污染正式美术与敌人主链的前提下，快速验证 Charged Rusher 的 Minishoot 风格视觉锚点，用于本地原型和内部 Demo。正式 `Assets/_Prefabs/Enemies/Enemy_ChargeRusher.prefab` 保持不变，后续可直接删除或替换 `_ReferenceOnly` 目录。
- **技术**：使用 Unity AssetDatabase / PrefabUtility 生成项目内 Prefab 引用，避免手写 `.meta` 或手写 Unity 序列化 fileID。采用 ReferenceOnly 隔离目录策略，将商业参考资产与正式 `Art/`、`_Art/`、`_Prefabs/` 主链分离。


## Minishoot ChargeRusher ReferenceOnly 表现层 v2 — 2026-05-15 12:32

- **新建/修改文件**：
  - `Assets/Scripts/Combat/Enemy/ChargeRusherReferencePhaseResolver.cs`（及 Unity 生成 `.meta`）
  - `Assets/Scripts/Combat/Enemy/ChargeRusherReferenceVisual.cs`（及 Unity 生成 `.meta`）
  - `Assets/Scripts/Combat/Tests/ChargeRusherReferencePhaseResolverTests.cs`（及 Unity 生成 `.meta`）
  - `Assets/_ReferenceOnly/Minishoot/ChargedRusher/Prefabs/Enemy_ChargeRusher_REF_Minshoot.prefab`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：新增 ReferenceOnly 专用 `ChargeRusherReferenceVisual`，只读取现有 `EnemyBrain.StateMachine.CurrentState`，根据 `ChargeState` 内的累计时间切换 Telegraph / Dashing / Recovery 表现；新增纯 C# `ChargeRusherReferencePhaseResolver` 与 NUnit 测试覆盖阶段解析。ReferenceOnly Prefab 已挂接该组件，并配置 `ChargerT3S1`、`Overcharge`、`ChargerT3S3`、`ChargerT3S2` 以及临时蓄力/冲锋音效。
- **目的**：继续复刻 Minishoot Charged Rusher 的读招表现，让本地原型在不改正式敌人主链的前提下拥有更清晰的 Telegraph → Attack → Recovery 视觉节奏。
- **技术**：采用可删 ReferenceOnly 表现组件，运行时不驱动 AI、不修改 SO、不 Instantiate；Prefab 引用通过 Unity `PrefabUtility` / `SerializedObject` 写入，避免手写 `.meta` 或 fileID。验证结果：`dotnet build Project-Ark.slnx` 通过；Unity Console 清空后刷新检查无 error/warning。

---

## GGReplica V2 原版 PlayerSkin 状态贴图表接入 — 2026-05-15 12:40

- **修改文件**
  - `Assets/Scripts/Ship/GGReplica/V2/GGReplicaGlitchView.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/V2/GGReplicaGlitchV2PrefabBuilder.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/V2/GGReplicaGlitchV2PrefabBuilderTests.cs`
  - `Assets/Scripts/Ship/Tests/GGReplicaGlitchV2RuntimeTests.cs`
  - `Assets/_Prefabs/Ship/Ship_GGReplicaV2.prefab`
  - `Assets/Scenes/GGReplicaGlitchV2Test.unity`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：继续推进 GGReplica V2 的“一比一复刻”目标，将 V2 机体三层 body sprite 从固定 `Movement_10/3/21` 改为消费已有 `GGReplicaPlayerSkin.asset.stateToSpritesTable`。`GGReplicaGlitchView` 新增 `_playerSkin`、`_bodyLayersRoot`、`_solidRenderer`、`_liquidRenderer`、`_highlightRenderer` 引用，并将 V2 输入状态映射到原版 `GGReplicaViewState`：Idle/Move→Idle，BoostHold→Boost，DodgeBurst→Dodge（null body pack，保留上一帧 body sprite），GrabHold→Grab，Heal→Heal，FireAim→Fire。进入状态时按 pack 切换 Solid/Liquid/Highlight sprite 并应用 `SpritesOffset`。V2 Prefab Builder 加载 `GGReplicaPlayerSkin.asset`，写入 PlayerSkin 和三层 renderer 引用。
- **目的**：修复 V2 “状态特效在变，但机体 sprite 本体不变”的偏差，让 Boost / Fire / Heal / Grab 等状态开始使用原版 `PlayerSkinDefault` 的状态贴图表，而不是只靠染色和附加特效伪装状态变化。
- **技术**：TDD。RED：新增 `View_StateChanges_ApplyOriginalPlayerSkinSpritePacks`，Unity PlayMode 先失败于 `GGReplicaGlitchView` 缺少 `_playerSkin` 字段；Prefab Builder 测试要求 `_playerSkin`、`_bodyLayersRoot`、`_solidRenderer`、`_liquidRenderer`、`_highlightRenderer` 均完成接线。GREEN：实现状态映射和 sprite pack 应用，重建 `GGReplicaPlayerSkin.asset`、`Ship_GGReplicaV2.prefab` 与 `GGReplicaGlitchV2Test.unity`。验证结果：新增 PlayerSkin sprite pack PlayMode 单测 1/1 通过；V2 Runtime PlayMode tests 12/12 通过；V2 Prefab Builder EditMode 单测 1/1 通过；最终 Prefab 检查返回 `OK: V2 prefab has PlayerSkin and body renderer wiring`；Console 无 error/warning；`dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 0 错误 0 警告；`git diff --check` 通过。重建 Prefab 后已清理空 `m_Name` 尾随空格。

---

## GGReplica V2 Dodge CoreModule half/shell 细化 — 2026-05-15 13:18

- **修改文件**
  - `Assets/Scripts/Ship/GGReplica/V2/GGReplicaGlitchView.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/V2/GGReplicaGlitchV2PrefabBuilder.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/V2/GGReplicaGlitchV2PrefabBuilderTests.cs`
  - `Assets/Scripts/Ship/Tests/GGReplicaGlitchV2RuntimeTests.cs`
  - `Assets/_Prefabs/Ship/Ship_GGReplicaV2.prefab`
  - `Assets/Scenes/GGReplicaGlitchV2Test.unity`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：继续按原版 `PlayerViewCoreModule` 复刻 Dodge 表现。参考 `PlayerViewCoreModule.cs` 中 `coreSR`、`shellGlow`、`shellFade`、`dodgeFadeInTime`、`dodgeFadeOutTime`、`dodgeScale`、`dodgeScaleInDuration`、`dodgeScaleOutDuration` 字段，以及原版 `Player.prefab` 中 `AdditiveCore_Dodge`、`Dodge_Sprite (used for old outline trail)`、`SHIP_PLAYER_DODGE_HALF` 相关节点证据。`GGReplicaGlitchView` 新增 `_dodgeHalfRenderer` 与 `_dodgeAdditiveCoreRenderer`，Dodge 中启用半身 shell silhouette、橙金 additive core、旧 outline trail sprite、core scale/fade 和 dodge shell 粒子；Dodge 窗口结束后统一隐藏并复位 scale/color。V2 Prefab Builder 创建 `DodgeModule/DodgeHalf_Sprite`、`DodgeModule/AdditiveCore_Dodge`、`DodgeModule/Dodge_Sprite (used for old outline trail)` 并写入 View 引用。
- **目的**：让 `Space` Dodge 不再只是紫色 ghost + core 放大，而是开始接近原版 GG 的 CoreModule shell/half/additive-core 分层表现，强化“一比一复刻”的状态可读性。
- **技术**：TDD。RED：扩展 `View_DodgeVisuals_FadeGhostAndStopBurstAfterDodgeWindow`，Unity PlayMode 先失败于 `GGReplicaGlitchView` 缺少 `_dodgeHalfRenderer`；Prefab Builder 测试要求新增原版命名节点和 `_dodgeHalfRenderer` / `_dodgeAdditiveCoreRenderer` 引用。GREEN：实现 runtime 层启停/缩放/alpha 和 Builder 接线；重建 `Ship_GGReplicaV2.prefab` 与 `GGReplicaGlitchV2Test.unity`。验证结果：新增 Dodge half/additive core PlayMode 单测 1/1 通过；V2 Runtime PlayMode tests 12/12 通过；V2 Prefab Builder EditMode 单测 1/1 通过；Console 无 error/warning；`dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 0 错误 0 警告；`git diff --check` 通过。重建 Prefab 后已清理空 `m_Name` 尾随空格。


## Minishoot ChargeRusher ReferenceOnly 冲锋残影与 Burst — 2026-05-15 13:04

- **新建/修改文件**：
  - `Assets/Scripts/Combat/Enemy/ChargeRusherReferenceAfterimageSampler.cs`（及 Unity 自动生成 `.meta`）
  - `Assets/Scripts/Combat/Enemy/ChargeRusherReferenceDashJuice.cs`（及 Unity 自动生成 `.meta`）
  - `Assets/Scripts/Combat/Enemy/ChargeRusherReferencePhaseResolver.cs`
  - `Assets/Scripts/Combat/Tests/ChargeRusherReferencePhaseResolverTests.cs`
  - `Assets/Scripts/Combat/Tests/ChargeRusherReferenceAfterimageSamplerTests.cs`（临时测试文件已中和为说明注释，避免 Unity 生成工程显式 include 导致重复测试类）
  - `Assets/_ReferenceOnly/Minishoot/ChargedRusher/Prefabs/Enemy_ChargeRusher_REF_Minshoot.prefab`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：新增 ReferenceOnly 专用 `ChargeRusherReferenceDashJuice`，在 `Awake` 预创建 5 个残影 `SpriteRenderer` 子对象，冲锋阶段通过 `ChargeRusherReferenceAfterimageSampler` 按 0.045 秒节流复用残影，并在进入 `Dashing` 时触发 0.08 秒短 burst。扩展 `ChargeRusherReferencePhaseResolverTests.cs` 覆盖残影采样规则。
- **目的**：继续复刻 Minishoot Charged Rusher 的冲锋读招表现，让 Telegraph → Dash → Recovery 中的 Dash 阶段更有速度感和攻击确认，不改正式 `Enemy_ChargeRusher` 主链。
- **技术**：残影组件只读取 `EnemyBrain.StateMachine.CurrentState` 与 `SpriteRenderer`，不驱动 AI、不修改 SO、不在战斗中 Instantiate；Prefab 引用通过 Unity `PrefabUtility` / `SerializedObject` 写入。修复过程中移除了 `ChargeRusherReferencePhaseResolver.cs` 中误留的重复采样器定义，保持一文件一类。验证结果：`dotnet build /Users/dada/Documents/GitHub/Project-Ark/Project-Ark.slnx` 通过；Unity Console 清空后无脚本编译错误，仅剩 TestRunner/PerformanceTesting 的测试结果保存日志。


## Minishoot ChargeRusher ReferenceOnly 命中反馈 — 2026-05-15 13:30

- **新建/修改文件**：
  - `Assets/Scripts/Combat/Enemy/ChargeRusherReferenceImpactGate.cs`（及 Unity 自动生成 `.meta`）
  - `Assets/Scripts/Combat/Enemy/ChargeRusherReferenceImpactFeedback.cs`（及 Unity 自动生成 `.meta`）
  - `Assets/Scripts/Combat/Tests/ChargeRusherReferencePhaseResolverTests.cs`
  - `Assets/_ReferenceOnly/Minishoot/ChargedRusher/Prefabs/Enemy_ChargeRusher_REF_Minshoot.prefab`
  - `ProjectArk.Enemy.csproj`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：新增 ReferenceOnly 专用 `ChargeRusherReferenceImpactGate`，用测试覆盖“只在 Dashing 阶段触发、每次冲锋只触发一次、离开 Dashing 后复位”。新增 `ChargeRusherReferenceImpactFeedback`，监听 ReferenceOnly Prefab 的 2D trigger/collision，在冲锋接触时播放一次预创建 `SpriteRenderer` spark、短 hit flash 和可选音效。Prefab 已挂接该组件并配置 `_sparkLifetime=0.12`、`_flashDuration=0.06`、`_sparkSortingOffset=2`。
- **目的**：继续提升 Minishoot Charged Rusher 的可读攻击反馈，让玩家在 Dash 命中/擦身时获得明确 impact cue，同时保持 Telegraph → Dash → Recovery 的 Signal-Window 读招模型。
- **技术**：组件只观察 `EnemyBrain.StateMachine.CurrentState` 和碰撞事件，不调用 `IDamageable.TakeDamage`、不修改正式 `ChargeState` / `Enemy_ChargeRusher` 主链；spark 对象在 `Awake` 预创建，运行中仅复用显隐，避免战斗中 Instantiate。Unity `.meta` 通过 AssetDatabase 导入自动生成，Prefab 通过 `PrefabUtility` / `SerializedObject` 写入。验证结果：先通过 RED 测到 `ChargeRusherReferenceImpactGate` 缺失，再实现 GREEN；`dotnet build /Users/dada/Documents/GitHub/Project-Ark/Project-Ark.slnx` 通过（仅既有 warning）；Unity Console 当前 0 error / 0 warning。

---

## GGReplica V2 Dodge ShapeTrail outline/additive 粒子拖尾 — 2026-05-15 13:44

- **修改文件**
  - `Assets/Scripts/Ship/GGReplica/V2/GGReplicaGlitchView.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/V2/GGReplicaGlitchV2PrefabBuilder.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/V2/GGReplicaGlitchV2PrefabBuilderTests.cs`
  - `Assets/Scripts/Ship/Tests/GGReplicaGlitchV2RuntimeTests.cs`
  - `Assets/_Prefabs/Ship/Ship_GGReplicaV2.prefab`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：继续复刻 GG 原版 `PlayerViewShapeTrailModule.StartDodge/EndDodge` 路径。参考 `Player.prefab` 中 `ShapeTrailModule`、`ShapeTrail_Dodge (old outline trail)`、`AdditiveTrail_Dodge` 节点，以及 `PlayerViewShapeTrailModule.cs` 中 `dodgeTrail` / `coreTrail` 字段，`GGReplicaGlitchView` 新增 `_dodgeTrailParticles`；进入 `DodgeBurst` 时启动 outline/additive 两条 Dodge 粒子拖尾，退出状态或 Dodge 视觉窗口结束时停止并清空。V2 Prefab Builder 在 `ShapeTrailModule` 下创建原版命名节点，并写入 View 的序列化数组。
- **目的**：让 `Space` Dodge 不再只有 CoreModule half/shell 和通用 TrailRenderer，而是补上原版 ShapeTrail 的旧轮廓拖尾与 additive core 拖尾，继续靠近 GG 基础 Glitch 飞船的一比一 Dodge 读感。
- **技术**：TDD + Unity Editor automation。RED：新增 `View_DodgeShapeTrail_StartsOriginalOutlineAndAdditiveParticleTrails`，PlayMode 先失败于缺少 `_dodgeTrailParticles` 字段；Prefab Builder 规格扩展要求新增两个原版命名节点和 `_dodgeTrailParticles` 接线。GREEN：实现 runtime 粒子启停、Builder 节点创建与 prefab 重建。验证结果：`ProjectArk.Ship.Tests` PlayMode 37/37 通过；`ProjectArk.Ship.Editor` EditMode 9/9 通过；`dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 0 错误 0 警告；`git diff --check` 通过。Prefab 空 `m_Name` 尾随空格已清理；Unity Console 中剩余 error 为既有负向测试刻意输出。

---

## GGReplica V2 Fluxy Trail Dodge 强度转场 — 2026-05-15 14:37

- **修改文件**
  - `Assets/Scripts/Ship/GGReplica/V2/GGReplicaGlitchView.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/V2/GGReplicaGlitchV2PrefabBuilder.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/V2/GGReplicaGlitchV2PrefabBuilderTests.cs`
  - `Assets/Scripts/Ship/Tests/GGReplicaGlitchV2RuntimeTests.cs`
  - `Assets/_Prefabs/Ship/Ship_GGReplicaV2.prefab`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：继续复刻原版 `PlayerViewFluxyTrailModule` 的 Dodge 路径。参考原版脚本中的 `dodgeColor`、`dodgeSize`、`dodgeInDuration`、`dodgeOutDuration`、`dodgeTrailForce`、`transitionDuration` 字段，以及 V2 已接入的 `GGReplicaFakeFluxy.mat`。`GGReplicaGlitchView` 新增 `_fluxyTrailRenderer` 和默认值缓存；进入 `DodgeBurst` 时把 `fluxy_like_lq_trail` 放大为 Dodge fluid 读感：增加 `TrailRenderer.widthMultiplier/time/localScale`、切换紫金 start/end color，并通过 `MaterialPropertyBlock` 提升 `_Alpha`、`_FlowPower`、`_NoiseScale`；Dodge 视觉窗口结束或退出状态时恢复默认 time/width/scale/color 并将 per-renderer `_Alpha` 降为 0。Boost/Move 状态保留较低强度的 Fluxy Trail。
- **目的**：让 `fluxy_like_lq_trail` 不再只是挂着 FakeFluxy 材质的普通线条，而是在 Dodge 窗口内表现出更接近 GG 原版 Fluxy fluid target 被推开的强度、规模和流动速度，同时避免污染共享材质资产。
- **技术**：TDD + MaterialPropertyBlock。RED：新增 `View_DodgeFluxyTrail_UsesOriginalDodgeIntensityWithoutMutatingSharedMaterial`，PlayMode 先失败于缺少 `_fluxyTrailRenderer` 字段；Prefab Builder 测试要求 `_fluxyTrailRenderer` 指向 `LQTrailModule/fluxy_like_lq_trail`。GREEN：实现 runtime Fluxy Trail 状态机、默认值恢复和 Builder 接线。验证结果：新增 Fluxy Trail PlayMode 单测 1/1 通过；V2 Prefab Builder focused EditMode 1/1 通过；`ProjectArk.Ship.Tests` PlayMode 38/38 通过；`ProjectArk.Ship.Editor` EditMode 9/9 通过。Prefab 空 `m_Name` 尾随空格已清理。


## Minishoot ChargeRusher ReferenceOnly 命中顿帧 — 2026-05-15 14:41

- **新建/修改文件**：
  - `Assets/Scripts/Combat/Enemy/ChargeRusherReferenceImpactFeedback.cs`
  - `Assets/_ReferenceOnly/Minishoot/ChargedRusher/Prefabs/Enemy_ChargeRusher_REF_Minshoot.prefab`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：在 ReferenceOnly `ChargeRusherReferenceImpactFeedback` 中新增 `_hitStopDuration` 配置，并在 `ImpactGate` 成功通过后调用现有 `HitStopEffect.Trigger()`。ReferenceOnly Prefab 已写入 `_hitStopDuration=0.025`。
- **目的**：继续复刻 Minishoot Charged Rusher 的命中确认感，让 Dash 接触瞬间有短暂停顿重量，同时不扩大正式敌人伤害主链。
- **技术**：复用 `ProjectArk.Core.HitStopEffect`，仅在 `Dashing` 命中且每次冲锋一次的既有门控后触发；没有新增运行时 Instantiate/Destroy，没有接入 `IDamageable` / `DamagePayload`。验证结果：`dotnet build /Users/dada/Documents/GitHub/Project-Ark/Project-Ark.slnx` 通过；Play Mode 反射触发验证显示 `hitStopDuration=0.025` 且触发后 `Time.timeScale=0`；Unity Console 仅剩既有 `MinimapUI._minimapPanel` 场景配置错误，非本轮改动引入。

---

## GGReplica V2 FluxyGrab 液体抓取反馈 — 2026-05-15 14:48

- **修改文件**
  - `Assets/Scripts/Ship/GGReplica/V2/GGReplicaGlitchView.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/V2/GGReplicaGlitchV2PrefabBuilder.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/V2/GGReplicaGlitchV2PrefabBuilderTests.cs`
  - `Assets/Scripts/Ship/Tests/GGReplicaGlitchV2RuntimeTests.cs`
  - `Assets/_Prefabs/Ship/Ship_GGReplicaV2.prefab`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：继续复刻原版 `PlayerViewFluxyGrabModule`。参考原版脚本中的 `interactTarget`、`hologramPrefab`、`rippableOverlayPrefab`、`throwPointer`、`ViewData(texture/size/pivotOffset)` 字段，以及 `Player.prefab` 中 `FluxyGrabModule`、`Grab_Hands`、`Ship_Sprite_Solid_Grab_R/L` 节点证据。`GGReplicaGlitchView` 新增 `_fluxyGrabModuleRoot`、`_grabFluxyRenderers` 与 `_grabThrowPointer`；进入 `GrabHold` 时除了原有左右 Grab hand 外，现在启用 fake fluxy hologram 手部、紫青 throw pointer 线、per-renderer `_Alpha/_FlowPower/_NoiseScale`，退出时隐藏并复位 scale / alpha。V2 Prefab Builder 在 `FluxyGrabModule` 下创建 `Grab_Hands`、`FluxyGrabHolo_R`、`FluxyGrabHolo_L`、`GrabThrowPointer`，并写入 View 引用。
- **目的**：让 `E` Grab 不再只是两张手 sprite 外移，而是开始具备 GG 原版 FluxyGrab 的液体目标 / hologram / 牵引线读感，提高抓取状态的可读性和“一比一复刻”接近度。
- **技术**：TDD + MaterialPropertyBlock + LineRenderer。RED：新增 `View_GrabHold_ShowsFluxyGrabHoloAndThrowPointerWithoutMutatingSharedMaterial`，PlayMode 先失败于缺少 `_fluxyGrabModuleRoot` 字段；Prefab Builder 测试要求新增 FluxyGrab 原版命名节点和序列化接线。GREEN：实现 runtime FluxyGrab 视觉控制、Builder 节点创建与 prefab 重建。验证结果：新增 FluxyGrab PlayMode 单测 1/1 通过；V2 Prefab Builder focused EditMode 1/1 通过；`ProjectArk.Ship.Tests` PlayMode 39/39 通过；`ProjectArk.Ship.Editor` EditMode 9/9 通过；`dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 0 错误（90 个既有/生成 warning）。Prefab 空 `m_Name` 尾随空格已清理，Unity Console 当前 0 error。

---

## GGReplica V2 Grab 选中/锁定/释放三段反馈 — 2026-05-15 14:58

- **修改文件**
  - `Assets/Scripts/Ship/GGReplica/V2/GGReplicaGlitchView.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/V2/GGReplicaGlitchV2PrefabBuilder.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/V2/GGReplicaGlitchV2PrefabBuilderTests.cs`
  - `Assets/Scripts/Ship/Tests/GGReplicaGlitchV2RuntimeTests.cs`
  - `Assets/Scripts/Core/Tests/ProjectArk.Core.Tests.asmdef`
  - `Assets/_Prefabs/Ship/Ship_GGReplicaV2.prefab`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：继续复刻原版 `PlayerViewFluxyGrabModule.OnSelect / OnLock / OnRelease` 的节奏，在当前 V2 没有真实 `GravGunInteractable` 目标系统前，先做本地可见版本。`GGReplicaGlitchView` 新增 `_grabLockRenderer`、`_grabReleaseRenderer`、`_grabHoldTimer`、`_grabReleaseTimer`；进入 `GrabHold` 后先显示 select 强度的 holo / pointer，按住超过 `GrabLockDelay=0.18s` 后启用 cyan lock ring 并加粗 pointer，松开 `E` 退出 Grab 时保留 `FluxyGrabModule` 短时间并播放 release pulse，随后隐藏并复位。V2 Prefab Builder 增加 `GrabLockRing` 与 `GrabReleasePulse` 节点和 View 接线测试，并已通过 `ProjectArk.Ship.Editor` 重建 `Ship_GGReplicaV2.prefab`。补测过程中发现 `ProjectArk.Core.Tests.asmdef` 同时使用显式 `UnityEngine.TestRunner` / `UnityEditor.TestRunner` 引用和旧版 `optionalUnityReferences: TestAssemblies`，导致 Unity Test Runner 初始化时报 duplicate references；已移除旧版 optionalUnityReferences。
- **目的**：让 Grab 的操作节奏从“按住显示、松开消失”推进到“选中 → 锁定 → 释放”的三段可读反馈，更接近原版 GG GravGun/FluxyGrab 的交互读感，同时不引入目标选择/物理抓取系统；并恢复 Unity Test Runner 的可用性，保证后续验证闭环稳定。
- **技术**：TDD + MaterialPropertyBlock + 计时状态机。RED：新增 `View_GrabHold_ProgressesSelectLockAndReleaseFeedback`，PlayMode 先失败于缺少 `_grabLockRenderer` 字段；Prefab Builder 测试要求新增 `GrabLockRing` / `GrabReleasePulse` 节点和序列化接线。GREEN：实现 hold/release 计时、lock ring、release pulse 和 Builder 生成逻辑。补测结果：`Ship_GGReplicaV2.prefab` 已包含 `GrabThrowPointer`、`FluxyGrabHolo_R/L`、`GrabLockRing`、`GrabReleasePulse` 以及 `_grabThrowPointer` / `_grabLockRenderer` / `_grabReleaseRenderer` 引用；`ProjectArk.Ship.Editor` EditMode 9/9 通过；`ProjectArk.Ship.Tests` PlayMode 40/40 通过；`dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 0 错误 0 警告；`git diff --check` 通过。Unity Console 当前 error 为既有负向测试刻意输出。

---

## Minishoot ChargeRusher ReferenceOnly 命中镜头震动 — 2026-05-15 14:52

- **新建/修改文件**：
  - `Assets/Scripts/Core/CameraShakeService.cs`（及 Unity 自动生成 `.meta`）
  - `Assets/Scripts/Core/Tests/CameraShakeServiceTests.cs`（及 Unity 自动生成 `.meta`）
  - `Assets/Scripts/Combat/Enemy/ChargeRusherReferenceImpactFeedback.cs`
  - `Assets/_ReferenceOnly/Minishoot/ChargedRusher/Prefabs/Enemy_ChargeRusher_REF_Minshoot.prefab`
  - `Assets/Scenes/SampleScene.unity`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：新增 Core 层 `CameraShakeService`，挂载在相机/相机 rig 上后会通过 `ServiceLocator` 注册，提供 `Shake(duration, amplitude, frequency)` 与确定性 `Step(unscaledDeltaTime)`；使用 unscaled time 推进，结束时恢复初始 localPosition。`ChargeRusherReferenceImpactFeedback` 新增 `_cameraShakeDuration/_cameraShakeAmplitude/_cameraShakeFrequency`，在既有 ImpactGate 通过后触发镜头震动；若服务缺失则只输出一次 warning。ReferenceOnly Prefab 写入默认 `0.08s / 0.08 amplitude / 26Hz`；`SampleScene` 的 `Main Camera` 已挂接 `CameraShakeService`。
- **目的**：继续复刻 Minishoot Charged Rusher 的命中确认感，在 hit flash、spark、hitstop 之外加入短促镜头冲击，同时保持正式敌人主链、伤害管线与 `Enemy_ChargeRusher` Prefab 不变。
- **技术**：轻量服务定位 + optional dependency。Enemy 层只依赖 Core 层服务，不直接查找或操作相机，避免运行时 `FindObjectOfType` 和相机实现耦合；shake 不产生运行时 Instantiate/Destroy。新增 EditMode 测试覆盖服务注册、参数夹取与持续时间结束复位。验证结果：`dotnet build Project-Ark.slnx --no-restore` 通过；`validate_script` 对 `CameraShakeService` 与 `ChargeRusherReferenceImpactFeedback` 均为 0 error；Unity Console 当前 0 error / 0 warning；`git diff --check` 通过。Unity MCP Test Runner 以程序集/测试名过滤运行时返回 `total: 0`，未发现用例，未作为失败处理。

---

## CameraShakeService 测试发现修复 — 2026-05-15 15:58

- **修改文件**
  - `Assets/Scripts/Core/Tests/ProjectArk.Core.Tests.asmdef`
  - `Assets/Scripts/Core/Tests/CameraShakeServiceTests.cs`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：修复 `ProjectArk.Core.Tests` 在 Unity Test Runner 中 EditMode 运行时返回 `total: 0` 的问题。将 Core 测试程序集限定为 `Editor` 平台，使 `CameraShakeServiceTests`、`DamageCalculatorTests`、`HeatSystemTests` 被归类到 EditMode 测试树；同时调整 `CameraShakeServiceTests` 的 EditMode 初始化方式，通过反射直接调用 `CameraShakeService.Awake()`，避免 `SendMessage("Awake")` 在 EditMode 中触发 Unity 内部 `ShouldRunBehaviour()` 断言。
- **目的**：让新增的镜头震动服务测试和既有 Core 单元测试能被 Unity EditMode Test Runner 稳定发现与执行，避免后续把 `total: 0` 误判为测试通过。
- **技术**：Unity asmdef 平台分类修正 + NUnit EditMode 测试适配 + 生命周期入口显式调用。验证结果：`dotnet build Project-Ark.slnx --no-restore` 通过；Unity EditMode `ProjectArk.Core.Tests` 发现并执行 17 个测试，17 passed / 0 failed；Unity Console 仅剩 Test Framework 自身运行提示，无项目编译错误。

---

## GGReplica V2 Grab Release burst/throw 收束反馈 — 2026-05-15 16:13

- **修改文件**
  - `Assets/Scripts/Ship/GGReplica/V2/GGReplicaGlitchView.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/V2/GGReplicaGlitchV2PrefabBuilder.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/V2/GGReplicaGlitchV2PrefabBuilderTests.cs`
  - `Assets/Scripts/Ship/Tests/GGReplicaGlitchV2RuntimeTests.cs`
  - `Assets/_Prefabs/Ship/Ship_GGReplicaV2.prefab`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：继续细化原版 `PlayerViewFluxyGrabModule.OnRelease` 的本地可见版本。`GGReplicaGlitchView` 新增 `_grabReleaseParticles` 与 `_grabReleaseThrowLine`；松开 `E` 退出 `GrabHold` 时，除了既有 `GrabReleasePulse`，现在会播放一次 `GrabReleaseBurst` 粒子，并启用三点 `GrabReleaseThrowLine`，用紫青渐变、宽度收束和中点上扬模拟液体甩出 / 指针收束。Release 窗口结束后停止粒子、隐藏 line，并关闭 `FluxyGrabModule`。V2 Prefab Builder 在 `FluxyGrabModule` 下创建 `GrabReleaseBurst` 和 `GrabReleaseThrowLine`，并写入 View 引用。
- **目的**：让 Grab 松开瞬间不再只是静态 pulse，而是具备更接近 GG 原版 `OnRelease` 的“液体甩出 / throw pointer fade”反馈，增强 `E` 释放时的操作确认感。
- **技术**：TDD + ParticleSystem + LineRenderer + MaterialPropertyBlock。RED：新增 `View_GrabRelease_PlaysBurstAndCollapsesThrowLine`，PlayMode 先失败于缺少 `_grabReleaseParticles` 字段；Prefab Builder 测试要求新增 `GrabReleaseBurst` / `GrabReleaseThrowLine` 节点和序列化接线。GREEN：实现 release burst 触发、throw line 三点曲线、窗口结束复位和 Builder 生成逻辑。验证结果：新增 Grab Release PlayMode 单测 1/1 通过；V2 Prefab Builder focused EditMode 1/1 通过；`ProjectArk.Ship.Tests` PlayMode 41/41 通过；`ProjectArk.Ship.Editor` EditMode 9/9 通过；`Ship_GGReplicaV2.prefab` 已包含 `GrabReleaseBurst`、`GrabReleaseThrowLine`、`_grabReleaseParticles`、`_grabReleaseThrowLine`；`dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 0 错误（86 个 warning）；`git diff --check` 通过。Unity Console 当前 error 为既有负向测试刻意输出。

---

## GGReplica V2 Grab 本地目标假影反馈 — 2026-05-16 10:41

- **修改文件**
  - `Assets/Scripts/Ship/GGReplica/V2/GGReplicaGlitchView.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/V2/GGReplicaGlitchV2PrefabBuilder.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/V2/GGReplicaGlitchV2PrefabBuilderTests.cs`
  - `Assets/Scripts/Ship/Tests/GGReplicaGlitchV2RuntimeTests.cs`
  - `Assets/_Prefabs/Ship/Ship_GGReplicaV2.prefab`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：继续复刻原版 `PlayerViewFluxyGrabModule` 的 `interactTarget` / `hologramPrefab` / `rippableOverlayPrefab` 语义，在尚未接入真实 `GravGunInteractable` 目标系统前，先加入本地 placeholder target。`GGReplicaGlitchView` 新增 `_grabTargetRenderer` 与 `_grabTargetOverlayRenderer`；`GrabHold` select 阶段显示 `GrabTargetHolo`，并把 `GrabThrowPointer` 指向目标假影；lock 阶段显示 `GrabRippableOverlay`，目标 scale/alpha/flow 强化；退出 Grab 后目标与 overlay 复位隐藏。V2 Prefab Builder 在 `FluxyGrabModule` 下创建 `GrabTargetHolo` 与 `GrabRippableOverlay`，并写入 View 引用。
- **目的**：让 `OnSelect / OnLock / OnRelease` 的视觉有明确落点，不再只围绕飞船自身播放，从而更接近 GG 原版 Grab 对“可交互目标”的读感，同时仍不引入真实物理抓取或目标选择系统。
- **技术**：TDD + 本地 placeholder target + MaterialPropertyBlock。RED：新增 `View_GrabHold_ShowsLocalInteractTargetAndLockOverlay`，PlayMode 先失败于缺少 `_grabTargetRenderer` 字段；Prefab Builder 测试要求新增 `GrabTargetHolo` / `GrabRippableOverlay` 节点和序列化接线。GREEN：实现目标假影位置/缩放/颜色/材质强度、pointer 指向目标、lock overlay 和退出复位。验证结果：新增 Grab target PlayMode 单测 1/1 通过；V2 Prefab Builder focused EditMode 1/1 通过；`ProjectArk.Ship.Tests` PlayMode 42/42 通过；`ProjectArk.Ship.Editor` EditMode 9/9 通过；`Ship_GGReplicaV2.prefab` 已包含 `GrabTargetHolo`、`GrabRippableOverlay`、`_grabTargetRenderer`、`_grabTargetOverlayRenderer`；`dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 0 错误 0 警告；`git diff --check` 通过。Unity Console 当前 error 为既有负向测试刻意输出。


---

## Minishoot 解包目录 Git Ignore — 2026-05-16 10:59

- **修改文件**
  - `.gitignore`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：在仓库根 `.gitignore` 末尾追加 `/Minishoot/`，并标注为本地解包参考资产目录。确认 `Minishoot` 目录已存在，包含 DevXUnity、ExportedProject、AuxiliaryFiles 等大量解包文件与程序集。
- **目的**：避免 Minishoot 解包参考文件、大型 DLL、导出工程与临时 `.DS_Store` 被 Git 跟踪，保留其作为本地 ReferenceOnly 资料使用。
- **技术**：Git ignore 根路径规则。使用 `/Minishoot/` 精确忽略仓库根目录下的解包文件夹，不影响其他可能同名子目录。

---

## GGReplica V2 Grab HoldModule 持续抓取场反馈 — 2026-05-16 10:52

- **修改文件**
  - `Assets/Scripts/Ship/GGReplica/V2/GGReplicaGlitchView.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/V2/GGReplicaGlitchV2PrefabBuilder.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/V2/GGReplicaGlitchV2PrefabBuilderTests.cs`
  - `Assets/Scripts/Ship/Tests/GGReplicaGlitchV2RuntimeTests.cs`
  - `Assets/_Prefabs/Ship/Ship_GGReplicaV2.prefab`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：继续复刻原版 `PlayerViewHoldModule` / `HoldModule` 的持续抓取场语义。参考原版 `PlayerViewHoldModule` 中的 `fadeTime`、`sr`、`progressTween`，以及 `Player.prefab` 中 `HoldModule`、`HoldParticles`、`HoldProgress` 节点。`GGReplicaGlitchView` 新增 `_holdModuleRoot`、`_holdParticles`、`_holdFieldRenderer`、`_holdProgressRenderer`、`_holdTetherLine`；`GrabHold` 期间启用 HoldModule、播放 HoldParticles，并按 `_grabHoldTimer / GrabHoldFieldChargeDuration` 推进持续场环、进度环与牵引线强度；退出 Grab 后停止粒子并隐藏复位。V2 Prefab Builder 在 `GGGlitchVisualRoot/HoldModule` 下创建 `HoldParticles`、`HoldFieldRing`、`HoldProgress`、`HoldTetherLine`，并写入 View 引用。
- **目的**：让 `E` 持续抓取不只是 select/lock/release 瞬时反馈，而是在按住期间持续显示一个稳定的 hold field，使 Grab 的维持状态更接近 GG 原版 PlayerView 的 HoldModule 读感。
- **技术**：TDD + ParticleSystem + LineRenderer + MaterialPropertyBlock。RED：新增 `View_GrabHold_ShowsHoldFieldProgressWhileMaintainingTarget`，PlayMode 先失败于缺少 `_holdModuleRoot` 字段；Prefab Builder 测试要求新增 `HoldModule`、`HoldParticles`、`HoldProgress`、`HoldFieldRing`、`HoldTetherLine` 节点和序列化接线。GREEN：实现 Hold field runtime 状态、充能进度、场环/进度环/牵引线与 Builder 生成逻辑。验证结果：新增 HoldModule PlayMode 单测 1/1 通过；V2 Prefab Builder focused EditMode 1/1 通过；`ProjectArk.Ship.Tests` PlayMode 43/43 通过；`ProjectArk.Ship.Editor` EditMode 9/9 通过；`Ship_GGReplicaV2.prefab` 已包含 `HoldModule`、`HoldParticles`、`HoldProgress`、`HoldFieldRing`、`HoldTetherLine`、`_holdModuleRoot`、`_holdParticles`、`_holdFieldRenderer`、`_holdProgressRenderer`、`_holdTetherLine`；`dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 0 错误（86 个 warning）；`git diff --check` 通过。Unity Console 当前 error 为既有负向测试刻意输出。

---

## GGReplica V2 Boost/Dodge/Grab 中断节奏 — 2026-05-16 11:22

- **修改文件**
  - `Assets/Scripts/Ship/GGReplica/V2/GGReplicaGlitchMotor.cs`
  - `Assets/Scripts/Ship/GGReplica/V2/GGReplicaGlitchView.cs`
  - `Assets/Scripts/Ship/Tests/GGReplicaGlitchV2RuntimeTests.cs`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：补充 Boost / Grab 快速切换的 PlayMode 规格；`GGReplicaGlitchMotor` 在 Boost 被 Grab / Dodge / Heal / Fire 等更高优先级状态打断时立即恢复基础阻尼，避免 AfterBoostDrag 泄漏到 Grab；`GGReplicaGlitchView` 新增 Boost cutoff afterimage 计时器，在 Boost 被 Dodge 或 Grab 打断时短暂保留 BoostModule 并复用 ignition burst 作为中断残影，同时立即停止 Boost sustain。
- **目的**：让 V2 不只是单状态像原版，而是在 Boost→Grab / Boost→Dodge 这类快速切换中保留原版“先断开推进、再叠加高优先反馈”的节奏，避免硬切和滑行手感泄漏。
- **技术**：TDD Red-Green；状态优先级仲裁仍集中在 `GGReplicaGlitchMotor.ApplyInput`，视觉中断反馈集中在 `GGReplicaGlitchView.ApplyState` / `TickVisuals`，未新增 Prefab 序列化字段，因此无需更新 Prefab Builder 接线。验证：新增 focused PlayMode 测试先 RED（2/2 失败，分别命中 Boost drag 泄漏与 BoostModule 硬切），实现后 `TestResults.xml` 显示新增 focused PlayMode 测试 2/2 通过；`dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 通过（0 错误）。Unity MCP 后续全量测试启动阶段有间歇性 timeout，未产生测试失败明细。


---

## Piercer ReferenceOnly MVP — 2026-05-16 11:18

- **新建/修改文件**
  - `Assets/Scripts/Combat/Enemy/PiercerReferencePhaseResolver.cs`
  - `Assets/Scripts/Combat/Enemy/PiercerReferenceVisual.cs`
  - `Assets/Scripts/Combat/Tests/PiercerReferencePhaseResolverTests.cs`
  - `Assets/_ReferenceOnly/Minishoot/Piercer/Sprites/PiercerT1S1.png`
  - `Assets/_ReferenceOnly/Minishoot/Piercer/Sprites/PiercerT1S2.png`
  - `Assets/_ReferenceOnly/Minishoot/Piercer/Sprites/PiercerT1S3.png`
  - `Assets/_ReferenceOnly/Minishoot/Piercer/Prefabs/Enemy_Piercer_REF_Minshoot.prefab`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容**：新增独立 `Piercer` ReferenceOnly MVP。`PiercerReferencePhaseResolver` 将现有 `ChargeState` elapsed time 映射为 Minishoot 原版 `AICharge` 风格的 `Pause -> Anticipation -> Dashing -> Recovery -> Idle` 表现阶段；`PiercerReferenceVisual` 只读取 `EnemyBrain.StateMachine.CurrentState` 并切换 `PiercerT1` 三帧 sprite、颜色、pause pulse 与 anticipation shake，不驱动移动、伤害或正式 AI 行为。从解包目录导入 `PiercerT1S1/S2/S3` 到独立 ReferenceOnly 目录，并创建 `Enemy_Piercer_REF_Minshoot.prefab`。

- **目的**：将 Minishoot 原版 `AICharge` 证据链从已有 `ChargerT*` 外观分支中拆出，建立更干净的 `Piercer` ReferenceOnly 复刻分支；保留现有 `ChargedRusher` ReferenceOnly 原型不动，避免继续混淆 `ChargerT*` 视觉资源与 `PiercerT*` 原版冲锋行为证据。

- **技术**：TDD 先添加 `PiercerReferencePhaseResolverTests`，并在 Unity Console 中确认 RED 阶段为缺少生产类型；用纯 C# resolver 承载可测试 Signal-Window 时序，MonoBehaviour 只负责表现读取和 sprite/颜色/scale/position juice；用 Unity Editor API 导入 Sprite、生成 Prefab 和校验序列化引用，避免手写 `.meta`。验证：`dotnet build Project-Ark.slnx` 全程序集成功；Unity 侧读取 prefab 确认 `ChargeRusherBrain`、`PiercerReferenceVisual`、`PiercerT1S1/S2/S3` 引用有效。Unity MCP 测试过滤本次返回 `total=0`，未作为通过依据。

---

## Piercer ReferenceOnly 独立 Brain 收口 — 2026-05-16 11:33

- **新建/修改文件**
  - `Assets/Scripts/Combat/Enemy/PiercerReferenceBrain.cs`
  - `Assets/Scripts/Combat/Enemy/PiercerReferenceChargeState.cs`
  - `Assets/Scripts/Combat/Enemy/PiercerReferencePhaseResolver.cs`
  - `Assets/Scripts/Combat/Tests/PiercerReferencePhaseResolverTests.cs`
  - `Assets/_ReferenceOnly/Minishoot/Piercer/Prefabs/Enemy_Piercer_REF_Minshoot.prefab`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容**：新增 `PiercerReferenceBrain` 与 `PiercerReferenceChargeState`，让 `Enemy_Piercer_REF_Minshoot.prefab` 不再借用 `ChargeRusherBrain` 作为 ReferenceOnly 行为壳；`PiercerReferenceVisual._brain` 现在指向独立 `PiercerReferenceBrain`。`PiercerReferencePhaseResolver` 增加对 `PiercerReferenceChargeState` 的支持，并补充对应测试用例。

- **目的**：将 Minishoot `Piercer` 的 ReferenceOnly 证据链从 `ChargeRusher` 命名与行为壳中进一步拆出，降低后续复刻 `AICharge` 时的认知混淆，同时继续保持不影响正式敌人 AI 主链。

- **技术**：`PiercerReferenceBrain` 继承 `EnemyBrain` 以满足现有组件约束，但只初始化一个纯标记状态 `PiercerReferenceChargeState`，不申请导演 token、不驱动正式攻击逻辑。使用 Unity Editor API 清理 prefab 中残留的 `ChargeRusherBrain` 并重接 `PiercerReferenceVisual._brain`。验证：`dotnet build Project-Ark.slnx` 成功；Unity 读取 prefab 确认 `hasPiercerBrain=True`、`hasChargeRusherBrain=False`、`visualBrainType=PiercerReferenceBrain`、resolver 对 `PiercerReferenceChargeState` 返回 `Pause`；清空 Console 后 error 数为 0。

---

## Piercer ReferenceOnly 循环预演 — 2026-05-16 11:44

- **新建/修改文件**
  - `Assets/Scripts/Combat/Enemy/PiercerReferencePhaseResolver.cs`
  - `Assets/Scripts/Combat/Enemy/PiercerReferenceVisual.cs`
  - `Assets/Scripts/Combat/Tests/PiercerReferencePhaseResolverTests.cs`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容**：为 `PiercerReferencePhaseResolver` 新增 `ResolveLooping`，在 `Pause -> Anticipation -> Dashing -> Recovery` 之后保留可配置 `Idle` gap，再 wrap 回下一轮 `Pause`；`PiercerReferenceVisual` 新增 `_loopPreview` 与 `_idleGapDuration`，默认启用循环预演，让 `Enemy_Piercer_REF_Minshoot` 在 ReferenceOnly 场景中可以持续观察完整 Signal-Window 节奏。

- **目的**：让 `Piercer` ReferenceOnly 原型不再只播放一次 `AICharge` 证据链，而是能持续预演 Telegraph/Attack/Recovery 闭环，方便后续手感调参和与原版 Minishoot 行为对照，同时不驱动正式移动、伤害或敌人 AI 主链。

- **技术**：TDD 先添加 `ResolveLooping_WrapsElapsedTimeBackToPauseWindow`，确认缺少 `ResolveLooping` 的 RED 编译失败；随后抽出 `IsChargeState` 与 `ResolveChargeWindow`，保持原 `Resolve` 非循环语义不变，并新增 idle gap 用例覆盖循环间隔。验证：`dotnet build Project-Ark.slnx` 成功（仅既有警告）；Unity 读取 prefab 确认 `loopPreview=True`、`idleGap=0.3`、`phaseAfterWrap=Pause`、`phaseDuringGap=Idle`；Unity Console error 数为 0。

---

## GGReplica V2 Dodge 打断续接 — 2026-05-16 13:46

- **修改文件**
  - `Assets/Scripts/Ship/GGReplica/V2/GGReplicaGlitchView.cs`
  - `Assets/Scripts/Ship/Tests/GGReplicaGlitchV2RuntimeTests.cs`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：补充 Dodge 打断窗口的 PlayMode 规格，覆盖 `GrabHold -> DodgeBurst -> held Grab`、`BoostHold -> DodgeBurst -> held Boost`、`GrabHold -> DodgeBurst` 视觉取消脉冲。`GGReplicaGlitchView` 新增 `_grabReleaseThrowActive`，将正常 Grab release 与 Dodge cancel 分开：正常 release 保留 throw line，Dodge cancel 使用更短的 `GrabCancelDuration`，只保留液体 pulse / burst，不画完整投掷线。同步更新模块切换测试，使 Boost 被 Grab 打断时的 cutoff afterimage 成为明确预期。
- **目的**：让 Dodge 作为最高优先级动作时，不再吞掉玩家持续按住的 Grab / Boost 输入，并让 Grab 被 Dodge 打断时读起来像“取消”而不是“投掷释放”，提升快速切换节奏感。
- **技术**：TDD Red-Green；输入续接沿用现有 `ApplyInput` 在 Dodge lockout 期间持续接收 held input 的行为，视觉层通过 release/cancel 状态位进行表现分支，没有新增 Prefab 序列化引用，因此不需要重建 `Ship_GGReplicaV2.prefab`。验证：新增 focused PlayMode 测试先 RED（Grab cancel throw line 仍显示），实现后 focused 3/3 通过；`ProjectArk.Ship.Tests` PlayMode 全量 48/48 通过；`dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 通过（0 错误）；`git diff --check` 通过。

---

## Piercer ReferenceOnly 阶段调试标签 — 2026-05-16 13:42

- **新建/修改文件**
  - `Assets/Scripts/Combat/Enemy/PiercerReferencePhaseResolver.cs`
  - `Assets/Scripts/Combat/Enemy/PiercerReferenceVisual.cs`
  - `Assets/Scripts/Combat/Tests/PiercerReferencePhaseResolverTests.cs`
  - `Assets/_ReferenceOnly/Minishoot/Piercer/Prefabs/Enemy_Piercer_REF_Minshoot.prefab`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容**：为 `PiercerReferencePhaseResolver` 新增 `FormatDebugLabel`，并在 `PiercerReferenceVisual` 中加入默认关闭的 `_showDebugPhaseLabel` 与 `_debugPhaseLabelOffset`。选中 ReferenceOnly Piercer 且开启开关后，会通过 Editor-only `OnDrawGizmosSelected` + `Handles.Label` 显示当前阶段、阶段计时与循环状态。同步将 prefab 上 `_loopPreview`、`_idleGapDuration`、`_showDebugPhaseLabel`、`_debugPhaseLabelOffset` 显式序列化，方便 Inspector 调参与审查。

- **目的**：提升 `Piercer` ReferenceOnly Signal-Window 调参效率，让 Telegraph / Attack / Recovery 当前窗口在 Scene 视图中可读，同时保持默认关闭，不生成运行时 UI 对象，不污染正式敌人 AI、伤害、移动或对象池链路。

- **技术**：TDD 先添加 `FormatDebugLabel_IncludesPhaseElapsedAndLoopState`，确认 RED 为缺少 `FormatDebugLabel`；随后实现纯 C# 格式化方法并在 Visual 层用 `#if UNITY_EDITOR` 包裹 Scene 标签绘制。验证：`dotnet build Project-Ark.slnx` 成功（仅既有 warning）；Unity SerializedObject 写入 prefab 默认值并保存成功；grep 确认 prefab YAML 存在 4 个新增/显式字段；Unity Console error 数为 0。

---

## GGReplica V2 Dodge 连按重触发 — 2026-05-16 13:54

- **修改文件**
  - `Assets/Scripts/Ship/GGReplica/V2/GGReplicaGlitchMotor.cs`
  - `Assets/Scripts/Ship/GGReplica/V2/GGReplicaGlitchView.cs`
  - `Assets/Scripts/Ship/Tests/GGReplicaGlitchV2RuntimeTests.cs`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：补充 `Motor_DodgePressedDuringDodge_RestartsVisualBurstWindow` PlayMode 规格，覆盖 Dodge 窗口内再次按 Dodge 时“物理刷新但视觉不重启”的节奏断层。`GGReplicaGlitchMotor.ApplyState` 新增 `forceReenter` 参数，DodgePressed 始终强制重派发 Dodge 状态；`GGReplicaGlitchView.ApplyState` 新增默认关闭的 `forceReenter` 参数，用于同状态 Dodge 重入时重置 dodge visual timer、重新点燃 ghost / burst / fluxy trail。
- **目的**：让连续 Dodge 或快速转向 Dodge 不再只有速度方向变化而缺少二次视觉/音频确认，保持原版 Glitch 的 burst 节奏可读性。
- **技术**：TDD Red-Green；默认 `ApplyState(state)` 行为保持兼容，仅 Motor 在 DodgePressed 时调用强制重入，避免其它状态每帧重复触发。验证：新增 focused PlayMode 测试先 RED（ghost alpha 保持 faded 0.1875），实现后 focused 1/1 通过；`ProjectArk.Ship.Tests` PlayMode 全量 49/49 通过；`dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 通过（0 错误）；`git diff --check` 通过。

---

## GGReplica V2 Dodge 粒子重启 — 2026-05-16 14:02

- **修改文件**
  - `Assets/Scripts/Ship/GGReplica/V2/GGReplicaGlitchView.cs`
  - `Assets/Scripts/Ship/Tests/GGReplicaGlitchV2RuntimeTests.cs`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：扩展 `Motor_DodgePressedDuringDodge_RestartsVisualBurstWindow` 规格，要求 Dodge 连按时 `ps_dodge_shell` 等 burst 粒子从头播放，而不是只重置 ghost alpha。`GGReplicaGlitchView` 新增 `RestartParticles` helper，并在进入/重入 `DodgeBurst` 时使用它重启 `DodgeBurstParticles`。
- **目的**：让连续 Dodge 的第二次输入有完整的粒子爆发反馈，避免视觉上只看到旧 emission 继续播放，提升快速 Dodge 转向的节奏确认。
- **技术**：TDD Red-Green；先通过粒子 `Simulate` 推进首段 burst 时间，确认 RED 为二次 Dodge 后 `burst.time` 仍停在旧 emission 的 `0.1`，再改为 Stop+Play 重启粒子。验证：focused PlayMode 1/1 通过；`ProjectArk.Ship.Tests` PlayMode 全量 49/49 通过；`dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 通过（0 错误）；`git diff --check` 通过。

---

## Piercer ReferenceOnly 阶段进度标签 — 2026-05-16 13:58

- **新建/修改文件**
  - `Assets/Scripts/Combat/Enemy/PiercerReferencePhaseResolver.cs`
  - `Assets/Scripts/Combat/Enemy/PiercerReferenceVisual.cs`
  - `Assets/Scripts/Combat/Tests/PiercerReferencePhaseResolverTests.cs`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容**：为 `PiercerReferencePhaseResolver` 新增 `ResolvePhaseProgress` 与 `FormatDetailedDebugLabel`，可计算当前 Signal-Window 阶段内归一化进度，并输出包含百分比的调试标签。`PiercerReferenceVisual` 的 Editor-only `OnDrawGizmosSelected` 改为显示 detailed label，例如 `Piercer REF | Anticipation | t=1.05s | p=25% | loop=on`。

- **目的**：提升 `Piercer` ReferenceOnly Telegraph / Attack / Recovery 调参可读性，让选中 prefab 时不仅能看到当前阶段，还能看到当前阶段走到多少百分比，便于快速判断窗口节奏是否符合 Minishoot `AICharge` 参考手感。

- **技术**：按 TDD 先添加 `ResolvePhaseProgress_ReturnsNormalizedProgressWithinCurrentWindow` 与 `FormatDetailedDebugLabel_IncludesPhaseProgressPercent`，确认 RED 为缺少 API；随后实现纯 C# resolver helper，并让 Visual 层只消费计算结果。验证：`dotnet build Project-Ark.slnx` 成功（仅既有 warning）；Unity `refresh_unity` 请求脚本编译后 Console error 数为 0；Unity Editor 内执行 resolver 调用得到 `progress=0.25` 与 detailed label 预期文本。

---

## Galactic Glitch 解包目录忽略 — 2026-05-16 14:06

- **修改文件**
  - `.gitignore`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：在本地解包参考资产忽略段中新增 `/Galactic Glitch/`，与现有 `/Minishoot/` 一起作为工作区内 reference-only 解包目录处理。
- **目的**：避免将 Galactic Glitch 的 `DevXUnity`、`DevXUnity_exported`、`Il2CppDumper_Output`、反编译脚本等外部参考文件误加入版本库，同时保留本地搜索/对照能力。
- **技术**：Git ignore 根目录锚定规则；验证 `git status --short --ignored` 显示 `!! "Galactic Glitch/"`。

---

## Piercer ReferenceOnly 阶段剩余时间标签 — 2026-05-16 14:09

- **修改文件**
  - `Assets/Scripts/Combat/Enemy/PiercerReferencePhaseResolver.cs`
  - `Assets/Scripts/Combat/Enemy/PiercerReferenceVisual.cs`
  - `Assets/Scripts/Combat/Tests/PiercerReferencePhaseResolverTests.cs`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容**：为 `PiercerReferencePhaseResolver` 新增 `ResolvePhaseRemainingTime`，并扩展 `FormatDetailedDebugLabel`，使 Scene 选中标签在阶段进度百分比之外显示当前阶段剩余秒数，例如 `Piercer REF | Anticipation | t=1.05s | p=25% | left=0.15s | loop=on`。`PiercerReferenceVisual` 的 Editor-only `OnDrawGizmosSelected` 同步传入剩余时间。

- **目的**：提升 ReferenceOnly Piercer Telegraph / Attack / Recovery 调参效率，让开发时能直接看到当前 Signal-Window 还剩多久，便于快速对齐 Minishoot `AICharge` 的读招节奏。

- **技术**：TDD Red-Green；先添加 `ResolvePhaseRemainingTime_ReturnsSecondsLeftWithinCurrentWindow`、`ResolvePhaseRemainingTime_UsesWrappedElapsedTimeWhenLoopPreviewIsEnabled`、`ResolvePhaseRemainingTime_ReturnsZero_WhenCurrentStateIsNotChargeState`，并更新 detailed label 期望，确认 RED 为缺 API/参数；随后实现纯 C# window remaining helper，Visual 仅消费结果，不驱动 gameplay。验证：`dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 成功（0 错误，仅既有 warning）；Unity Console error 数为 0；Unity Editor 内执行 resolver 调用得到 `progress=0.25; remaining=0.15; label=Piercer REF | Anticipation | t=1.05s | p=25% | left=0.15s | loop=on`。

---

## Piercer ReferenceOnly 循环位置标签 — 2026-05-16 14:30

- **修改文件**
  - `Assets/Scripts/Combat/Enemy/PiercerReferencePhaseResolver.cs`
  - `Assets/Scripts/Combat/Enemy/PiercerReferenceVisual.cs`
  - `Assets/Scripts/Combat/Tests/PiercerReferencePhaseResolverTests.cs`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容**：为 `PiercerReferencePhaseResolver` 新增 `ResolveCycleElapsedTime` 与 `ResolveCycleDuration`，并扩展 `FormatDetailedDebugLabel` 输出完整循环位置，例如 `cycle=0.05/2.20s`。`PiercerReferenceVisual` 的 Editor-only `OnDrawGizmosSelected` 现在会同时显示当前阶段进度、阶段剩余时间、循环内时间和循环总时长。

- **目的**：继续提升 ReferenceOnly Piercer 的 Signal-Window 调参效率。阶段进度/剩余时间解决“当前小窗口走到哪里”，循环位置解决“整段 Pause→Anticipation→Dash→Recovery→IdleGap 预览走到哪里”，便于判断 loop preview wrap 与 idle gap 节奏。

- **技术**：TDD Red-Green；先添加 `ResolveCycleElapsedTime_UsesWrappedElapsedTimeWhenLoopPreviewIsEnabled`、`ResolveCycleElapsedTime_ReturnsSafeElapsedTimeWhenLoopPreviewIsDisabled`、`ResolveCycleElapsedTime_ReturnsZero_WhenCurrentStateIsNotChargeState`、`ResolveCycleDuration_ReturnsClampedChargeAndIdleGapDuration` 与 detailed label 期望，确认 RED 为缺少 API/签名；随后实现纯 C# cycle helper，并让 Visual 仅消费计算结果。验证：`dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 成功（0 错误，仅既有 warning）；本地搜索确认 `PiercerReferenceVisual` 已传入 `cycleElapsedTime` 和 `cycleDuration`。Unity MCP 在脚本刷新后连续超时，未能完成 Unity Console 与 Editor 内 resolver 调用复验，因此本条 Unity Editor 验证状态记录为受阻。

---

## GGReplica V2 Boost 尾焰 StopEmitting — 2026-05-16 14:45

- **修改文件**
  - `Assets/Scripts/Ship/GGReplica/V2/GGReplicaGlitchView.cs`
  - `Assets/Scripts/Ship/Tests/GGReplicaGlitchV2RuntimeTests.cs`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：基于 `Docs/6_Diagnostics/GGReplica_V2_OriginalPlayerView_Audit.md` 对 `PlayerViewBoostModule` 的记录，以及 `2026-05-14-ggreplica-playerview-rebuild-plan.md` 中 Boost module API 的 `StopEmitting` 语义，补充 `View_BoostExit_StopsEmittingWithoutClearingLiveParticles` 规格。`GGReplicaGlitchView` 新增 Boost 专用粒子启停路径：Boost 结束时对 sustain 粒子使用 `ParticleSystemStopBehavior.StopEmitting`，保留 live particles 自然消散；Boost 根节点在 live particles 清空后再关闭。
- **目的**：避免 Boost 结束时尾焰被 `StopEmittingAndClear` 硬清，进一步贴近原 GG 的 PlayerView module stack，而不是凭空硬切。
- **技术**：TDD Red-Green；先验证 RED（Boost 退出后 live particle count 被清为 0），再引入 `SetBoostParticles`、可配置 `StopParticles` overload 与 `HasLiveParticles`。验证：focused PlayMode 1/1 通过；`ProjectArk.Ship.Tests` PlayMode 全量 50/50 通过；`dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 通过（0 错误）；`git diff --check` 通过。

---

## Piercer ReferenceOnly 工作区卫生清理 — 2026-05-17 11:27

- **修改文件**
  - `Anticipation`（删除空文件）
  - `Dashing`（删除空文件）
  - `Idle`（删除空文件）
  - `Recovery`（删除空文件）
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容**：清理根目录下误生成的四个空临时文件 `Anticipation`、`Dashing`、`Idle`、`Recovery`。复查 `Assets/_ReferenceOnly/Minishoot/Piercer/` 后确认其中包含 Piercer ReferenceOnly prefab 与 sprite 资源，未清理该参考资产目录；同时未触碰当前工作区内既有的 `GGReplica` 修改。

- **目的**：收口 Piercer ReferenceOnly 垂直切片的工作区卫生，避免阶段名空文件混入版本库或干扰后续 review。

- **技术**：先用 `file Anticipation Dashing Idle Recovery` 确认四个条目均为空文件，再用 Python 按“存在且 size=0”条件删除，避免误删非空内容。验证：`git status --short` 不再显示四个根目录临时文件；`dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 成功（0 错误）。当前会话未连接 Unity Editor MCP，仅能完成 dotnet 编译复验，Unity Console / Editor 内复验需后续在 Editor 可用时执行。

---

## Piercer ReferenceOnly Debug Harness A 阶段 — 2026-05-17 11:56

- **修改文件**
  - `Assets/Scripts/Combat/Enemy/PiercerReferencePhaseResolver.cs`
  - `Assets/Scripts/Combat/Enemy/PiercerReferenceVisual.cs`
  - `Assets/Scripts/Combat/Enemy/PiercerReferenceDebugHarness.cs`
  - `Assets/Scripts/Combat/Tests/PiercerReferencePhaseResolverTests.cs`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容**：新增 `PiercerReferencePhaseSnapshot` 与 `ResolveSnapshot`，把 phase、phase progress、remaining time、cycle elapsed/duration 与 detailed label 收口成一次性查询结果。`PiercerReferenceVisual` 新增 `ConfigurePreviewTiming`、`ResetPreviewCycle` 与 `ResolveCurrentSnapshot`，并让 Scene label 复用 snapshot。新增 `PiercerReferenceDebugHarness`，用于 Play Mode 中以 ReferenceOnly 方式快速配置并重置 Piercer 的纯视觉 Signal-Window 预览。

- **目的**：交付 A 阶段纯视觉调参 Harness，让开发者能在 2 分钟内进入 Play Mode 观察 `Pause → Anticipation → Dashing → Recovery → IdleGap` 的读招节奏；本阶段不做位移、不接碰撞、不造成伤害、不申请 EnemyDirector token，为后续 B 阶段轻量 Dash Preview 留出清晰边界。

- **技术**：TDD Red-Green；先为 `ResolveSnapshot_ReturnsPhaseTimingAndDetailedLabelForHarness` 写 RED，确认 `PiercerReferencePhaseSnapshot` 与 `ResolveSnapshot` 缺失后失败，再实现只读 snapshot 聚合 API。Harness 通过显式 public 配置入口驱动 `PiercerReferenceVisual`，不使用反射、不新增 Coroutine、不运行时查找全局对象。验证：`dotnet build ProjectArk.Combat.Tests.csproj -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 成功；`dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 成功（0 错误，仅既有 warning）；Unity MCP `refresh_unity` 超时，但 `read_console` 读取 error 返回 0 条。

---

## GGReplica V2 尾迹三模块分层 — 2026-05-17 11:47

- **修改文件**
  - `Assets/Scripts/Ship/GGReplica/V2/GGReplicaGlitchView.cs`
  - `Assets/Scripts/Ship/Tests/GGReplicaGlitchV2RuntimeTests.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/V2/GGReplicaGlitchV2PrefabBuilder.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/V2/GGReplicaGlitchV2PrefabBuilderTests.cs`
  - `Assets/_Prefabs/Ship/Ship_GGReplicaV2.prefab`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：基于 `GGReplica_V2_OriginalPlayerView_Audit.md` 与解包 `PlayerViewLQTrailModule`、`PlayerViewFluxyTrailModule`、`PlayerViewShapeTrailModule` 的字段/职责证据，将 V2 的统一 `_trailRenderers` 拆为 `_lqTrailRenderers`、`_darkTrailRenderers`、`_shapeTrailRenderers` 与既有 `_fluxyTrailRenderer`。`Move/Boost` 只驱动 LQ lane；`Dodge` 驱动 ShapeTrail lane 与 FluxyTrail lane；DarkTrail 暂保持独立但默认不由基础 LQ 路径打开。同步更新 Prefab Builder 与现有 `Ship_GGReplicaV2.prefab` 的序列化接线，并补充 runtime / builder 测试规格。
- **目的**：落实简报中“`PlayerViewLQTrailModule`、`PlayerViewFluxyTrailModule`、`PlayerViewShapeTrailModule` 是独立系统，不是一个 TrailRenderer 数组”的约束，避免继续用统一开关驱动所有尾迹，提升 GG 飞船尾迹分层复刻的准确性。
- **技术**：TDD 规格先行；新增 `View_TrailModules_MoveAndDodgeUseSeparateOriginalModuleLanes`，并扩展 `GGReplicaGlitchV2PrefabBuilderTests` 验证三类 trail 字段接线。运行时通过 `SetTrailEmitting(TrailRenderer[] trails, bool emitting)` 与 `HasTrailPositions` 管理 LQ root 生命周期；Prefab YAML 仅替换已知旧 `_trailRenderers` fileID 为新字段，未手写 `.meta`。验证：Unity MCP 恢复后补跑 `View_TrailModules_MoveAndDodgeUseSeparateOriginalModuleLanes` PlayMode focused 1/1 通过；`GGReplicaGlitchV2PrefabBuilderTests` EditMode focused 1/1 通过；`ProjectArk.Ship.Tests` PlayMode 全量 51/51 通过；`ProjectArk.Ship.Editor` EditMode 全量 9/9 通过；`dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 通过（0 错误）；`git diff --check` 通过。

---

## Piercer ReferenceOnly Debug Harness B 阶段 — 2026-05-17 12:01

- **修改文件**
  - `Assets/Scripts/Combat/Enemy/PiercerReferenceDashPreviewSampler.cs`
  - `Assets/Scripts/Combat/Enemy/PiercerReferenceDebugDashPreview.cs`
  - `Assets/Scripts/Combat/Tests/PiercerReferencePhaseResolverTests.cs`
  - `Assets/_ReferenceOnly/Minishoot/Piercer/Prefabs/Enemy_Piercer_REF_Minshoot.prefab`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容**：新增 `PiercerReferenceDashPreviewSampler`，以 `PiercerReferencePhaseSnapshot` 为输入，只在 `Dashing` 阶段输出 eased dash offset，非 dash 阶段返回零。新增 `PiercerReferenceDebugDashPreview`，在 `LateUpdate` 中把采样 offset 应用到预览目标，并在禁用或关闭预览时复位。同步把 `PiercerReferenceDebugHarness` 与 `PiercerReferenceDebugDashPreview` 挂到 `Enemy_Piercer_REF_Minshoot.prefab` 根节点，确保 ReferenceOnly prefab 拖入场景后可以直接 Play Mode 预览。

- **目的**：交付 B 阶段轻量位移预览，让 Piercer 的 Signal-Window 不仅能看到颜色/缩放/读招阶段，还能在 `Dashing` 窗口感受到短距离突进方向与节奏；本阶段仍不移动 gameplay collider、不造成伤害、不接 EnemyDirector token。

- **技术**：TDD Red-Green；先为 `PiercerReferenceDashPreviewSampler` 写 RED，确认测试因缺少 sampler 类型失败，再实现纯 helper 并通过 Unity `manage_asset import` 让新脚本纳入 Unity 生成项目缓存。位移采用 `EaseOutCubic`，方向归一化，距离负值钳制为 0。验证：`dotnet build ProjectArk.Combat.Tests.csproj -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 成功；`dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 成功（0 错误，仅既有 warning）；Unity Console error 读取返回 0 条；`refresh_unity` wait 超时但 Console 未残留编译错误。

---

## Piercer ReferenceOnly Debug Harness C 阶段 — 2026-05-17 12:09

- **修改文件**
  - `Assets/Scripts/Combat/Enemy/PiercerReferenceDashPreviewSampler.cs`
  - `Assets/Scripts/Combat/Enemy/PiercerReferenceDebugDashPreview.cs`
  - `Assets/Scripts/Combat/Tests/PiercerReferencePhaseResolverTests.cs`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容**：为 `PiercerReferenceDashPreviewSampler` 新增 `ResolveDirection`，把 dash preview 的 local/world direction 语义收口到纯逻辑 helper 中。`PiercerReferenceDebugDashPreview` 改为复用 sampler 解析方向，并移除自身重复的 `ResolveDirection`。新增测试覆盖：启用 local direction 时方向会被 owner rotation 转换；关闭 local direction 时输入向量就是 world direction，不受 owner rotation 影响。

- **目的**：补齐 B 阶段 Play Mode 调参闭环中的方向语义，避免 Inspector 中 `_useLocalDirection=false` 时仍被 `TransformDirection` 旋转导致“世界方向”表现不符合字段含义。该阶段仍保持 ReferenceOnly 边界：不接碰撞、不造成伤害、不申请 EnemyDirector token。

- **技术**：TDD Red-Green；先追加 `ResolveDirection_ReturnsLocalDirectionTransformedByOwner_WhenUseLocalDirectionIsEnabled` 与 `ResolveDirection_ReturnsWorldDirectionUnchanged_WhenUseLocalDirectionIsDisabled`，确认 focused 编译因缺少 `ResolveDirection` 失败，再实现最小纯函数并让 MonoBehaviour 调用。验证：`dotnet build ProjectArk.Combat.Tests.csproj -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 成功；`dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 成功（0 错误，仅既有 warning）；Unity Console error 读取返回 0 条；`manage_prefabs get_info` 确认 `Enemy_Piercer_REF_Minshoot.prefab` 根节点含 `PiercerReferenceDebugHarness` 与 `PiercerReferenceDebugDashPreview`。

---

## Piercer ReferenceOnly Debug Harness D 阶段 — 2026-05-18 11:39

- **修改文件**
  - `Assets/Scripts/Combat/Enemy/PiercerReferenceDashPreviewSampler.cs`
  - `Assets/Scripts/Combat/Enemy/PiercerReferenceDebugDashPreview.cs`
  - `Assets/Scripts/Combat/Tests/PiercerReferencePhaseResolverTests.cs`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容**：为 `PiercerReferenceDashPreviewSampler` 新增 `FormatReadout`，把 ReferenceOnly dash preview 的 `Preview ON/OFF`、local/world 模式、当前 phase、phase progress 与 offset 格式化为稳定文本。`PiercerReferenceDebugDashPreview` 新增 Play Mode Inspector readout 字段：`_currentPreviewPhase`、`_currentPreviewOffset`、`_playModeReadout`，并在 `LateUpdate` 中随 snapshot/offset 实时刷新；关闭 preview 时会复位位置并显示零 offset。

- **目的**：交付 D 阶段最小 Play Mode Feel Pass 验收辅助，让 Piercer prefab 拖入场景后，开发者能同时用肉眼观察 Telegraph → Dash → Recovery 的颜色/缩放/位移，并在 Inspector 中确认当前相位、方向模式和实际 offset，缩短 2 分钟内进 Play Mode 调参的闭环。该阶段仍保持 ReferenceOnly 边界：不接碰撞、不造成伤害、不申请 EnemyDirector token。

- **技术**：TDD Red-Green；先追加 `FormatReadout_IncludesPhaseOffsetAndDirectionMode`，确认 focused 编译因缺少 `FormatReadout` 失败，再实现 `CultureInfo.InvariantCulture` 格式化，避免系统区域设置影响小数输出。验证：`dotnet build ProjectArk.Combat.Tests.csproj -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 成功（0 错误，仅既有 `TurretAttackState._hasFired` warning）。全方案 `dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 当前被既有 `Assets/Scripts/Ship/GGReplica/V2/GGReplicaGlitchView.cs` 中 `exitingDodge`、`_shapeTrailTimer`、`ApplyShapeTrailVisuals` 未定义错误阻断，错误不在本次 Piercer 修改范围内。

---

## Minishoot vs Galactic Glitch 美术模块对比文档 — 2026-05-18 12:36

- **新增文件**
  - `Docs/7_Reference/GameAnalysis/Minishoot_vs_GalacticGlitch_ArtModule_Comparison.md`
- **修改文件**
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：新增一份横向参考分析文档，对比 `Minishoot` 与 `Galactic Glitch` 在美术模块上的实现差异，覆盖资源规模、飞船主体表现、场景环境、VFX、后处理、UI、动画体系、风险差异与 Project Ark 的混合落地路线。文档明确指出 `Minishoot` 更适合作为 Tilemap/Biome/量产场景管线参考，`Galactic Glitch` 更适合作为多层飞船状态、Shader、Trail、Boost/VFX 与状态化屏幕反馈参考。
- **目的**：为 Project Ark 当前场景配置与验证阶段提供美术模块决策依据，避免把美术理解停留在单张 Sprite 层面，并帮助后续 `ShipSkinSO`、`PostProcessController`、`RoomVariantSO`、`AmbienceController`、Boost/Overheat/Weaving VFX 样板的优先级排序。
- **技术**：基于既有 `Minishoot_Adventures_Structure_Analysis.md` 与 `GalacticGlitch_Structure_Analysis.md`，结合本轮对两个参考项目资源结构、脚本命名、Shader/Material/VFX 模块的调研结论，按美术管线维度整理为 Markdown 参考文档。本次仅新增文档，不修改运行时代码或 Unity 资产。

---

## GGReplica V2 ShapeTrail 残影窗口 — 2026-05-18 15:03

- **修改文件**
  - `Assets/Scripts/Ship/GGReplica/V2/GGReplicaGlitchView.cs`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：基于 `GGReplica_V2_OriginalPlayerView_Audit.md` 与解包 `PlayerViewShapeTrailModule` 的 `StartDodge` / `EndDodge`、`dodgeTrail`、`coreTrail`、`dodgeTrailFadeTime` 证据，为 V2 ShapeTrail 增加独立 `_shapeTrailTimer`。`DodgeBurst` 进入/强制重入时重启 `ShapeTrail` 粒子与 renderer；退出 Dodge 时对 `_dodgeTrailParticles` 使用 `StopEmitting` 并保留短残影窗口，窗口结束后再关闭 ShapeTrail renderer 与 DodgeModule。
- **目的**：让 ShapeTrail 从“Dodge 状态硬开硬关”升级为更接近原 GG 的独立残影模块，避免 Dodge 退出瞬间清空 outline/additive trail，同时支持连续 Dodge 重触发时从头播放残影。
- **技术**：模块分层延续上一轮 LQ/Fluxy/ShapeTrail 拆分；使用计时窗口模拟原版 `EndDodge` fade，而不是引入运行时 DOTween 依赖。验证：`dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly` 通过（0 错误）；`git diff --check` 通过。Unity MCP 当前端口 `127.0.0.1:8080` 未监听，PlayMode focused / Ship 全量测试需 MCP 恢复后补跑。

---

## Ship Art/VFX Workflow 参考项目反查补充 — 2026-05-18 22:49

- **修改文件**
  - `Docs/3_WorkflowsAndRules/Ship/Ship_ArtVFX_Workflow.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容**：基于 `Galactic Glitch` 与 `Minishoot` 两个解包项目的实际资产结构，补充 Ship Art/VFX 生产计划中的遗漏项：新增 `0.4 参考项目反查结论`、`Outline` 层定义、SpriteAtlas / Import Preset / PPU / Pivot 一致性要求、Normal 阶段 `spr_ship_canary_outline_normal_outline.png`、Dodge 阶段 lean left/right 方向帧、材质参数矩阵、程序化 VFX 资产规则、`Batch 0` 参考项目反查批次，以及避免误用参考状态图的禁用项。

- **目的**：让金丝雀号美术生产计划不只停留在单张 Sprite 清单，而是吸收 `Galactic Glitch` 的多状态分层/Shader/VFX 管线经验与 `Minishoot` 的高可读轮廓、Dash/Lean 帧、Outline/材质参数经验，避免后续把参考资产脱离状态语境误用，并提前固化导入一致性和材质参数记录要求。

- **技术**：文档级 workflow 修订；通过文件名与资源目录扫描反查两个参考项目中的 `Movement / Boost / Primary / Secondary / GrabGun / Healing`、`PlayerDash1-5`、`PlayerLeanLeft/Right`、`PlayerOutline`、`ShipPlayer`、`glow`、`ring`、`muzzle_flash`、`trail`、`noise`、`mask` 等资产信号，再映射为 Project Ark 的资产生产约束。本次不修改运行时代码、Prefab 或 Unity 资产。


---

## ShipArtVFX 参考资源文件夹同步 Minishoot + Galactic Glitch — 2026-05-18 23:18

- **新建 / 修改文件**
  - `Docs/7_Reference/ShipArtVFX_MinishootReference/README.md`
  - `Docs/7_Reference/ShipArtVFX_MinishootReference/source_map_plan_sync_2026-05-18.csv`
  - `Docs/7_Reference/ShipArtVFX_MinishootReference/Batch_0_Reference_Mapping/galactic_glitch_playerskin_state_map.csv`
  - `Docs/7_Reference/ShipArtVFX_MinishootReference/Batch_0_Reference_Mapping/GalacticGlitch_PlayerSkinDefault/*.png`
  - `Docs/7_Reference/ShipArtVFX_MinishootReference/Batch_1_Normal/Outline/*.png`
  - `Docs/7_Reference/ShipArtVFX_MinishootReference/Batch_2_Dodge/Lean/*.png`
  - `Docs/7_Reference/ShipArtVFX_MinishootReference/Batch_2_Dodge/Source/*.png`
  - `Docs/7_Reference/ShipArtVFX_MinishootReference/Batch_7_Unity_Material_Shader/Materials/*.mat`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容**：根据 `Ship_ArtVFX_Workflow.md` 最新反查结论，同步更新 `ShipArtVFX_MinishootReference` 参考资料文件夹。补入 `Minishoot` 的 `SupershotPlayerOutline`、`PlayerDash3/5`、`PlayerLeanLeft/Right1-3`、`PlayerOutline.mat`、`ShipPlayer*.mat`、`PlayerTrail*.mat`、`ring (additive).mat` 等资源；新增 `Galactic Glitch` 的 `PlayerSkinDefault` 状态参考子目录与 CSV 映射表，覆盖 Normal、Boost、Primary、Secondary、GrabGun、Healing 的 solid/liquid/highlight 层参考。

- **目的**：让参考资料文件夹与最新 Ship Art/VFX plan 对齐：补齐 `Outline` 独立层、Dodge lean / dash 短帧序列、材质参数矩阵参考、程序化 ring/trail/glow 参考，并把 `Galactic Glitch` 状态图的语境和误用警告放到同一处，避免后续把 `GrabGun_Base_9/8` 或 `Primary_4` 脱离原状态误用。

- **技术**：使用脚本从两个解包项目复制参考资源到文档目录，不创建 Unity `.meta`，不修改运行时代码、Prefab、Scene 或正式 Unity 资产。新增 `README.md` 作为资料夹索引，新增 `source_map_plan_sync_2026-05-18.csv` 记录来源资产，新增 `galactic_glitch_playerskin_state_map.csv` 固化 `State -> layer -> source sprite -> copied file` 映射。

---

## Ship Art/VFX Reference Batch 1 Minishoot 资源校正 — 2026-05-19 00:35

- **修改文件**
  - `Docs/7_Reference/ShipArtVFX_MinishootReference/Batch_1_Normal/`
  - `Docs/7_Reference/ShipArtVFX_MinishootReference/README.md`
  - `Docs/7_Reference/ShipArtVFX_MinishootReference/source_map_plan_sync_2026-05-18.csv`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：根据 Batch 1 资源校对结果，删除误混入 Normal 批次的 `Weapon0.png`、`SupershotPlayer1.png`、`SupershotPlayerOutline.png`、`WreckShipWaterMask.png`、`LightPlayerWall.png` 等非基础玩家船资源；将 `Batch_1_Normal` 收口为 `Player.png`、`_Player.png`、`__PlayerFull.png`、`PlayerCrystal.png`、`ShipWhite.png` 对应的基础玩家船同源参考组，并同步更新 README 与 source map。
- **目的**：避免将武器、Supershot 状态、残骸遮罩或场景光照图误当作 `Canary` Normal 状态分层参考，保证 Reference 包与 Ship Art/VFX 工作流中的状态语境一致。
- **技术**：通过 SHA-256 内容哈希对照 `Minishoot/DevXUnity/Texture2D` 源文件确认来源；保留源图副本到 `Source/` 目录，生产命名图仅保留 Solid、Liquid、Core 与 Highlight mask 四类。
- **技术**：通过 SHA-256 内容哈希对照  源文件确认来源；保留源图副本到  目录，生产命名图仅保留 Solid、Liquid、Core 与 Highlight mask 四类。
