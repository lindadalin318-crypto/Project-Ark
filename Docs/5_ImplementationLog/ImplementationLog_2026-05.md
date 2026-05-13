
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
