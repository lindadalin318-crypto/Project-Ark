
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
