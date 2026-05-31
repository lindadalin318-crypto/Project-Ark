---

## Ship/VFX ShipVfxValidator Authority Audit 聚合 — 2026-05-31 22:28

- **新建/修改文件**
  - `Assets/Scripts/Ship/Editor/ShipVfxValidator.cs`
  - `Assets/Scripts/Ship/Editor/ShipVfxValidatorTests.cs`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容**：为 `ShipVfxValidator.RunAudit(showDialog, logToConsole)` 增加只读 authority audit 聚合步骤，统一调用 `ShipPrefabRebuilder.RunAudit(false)`、`BoostTrailPrefabCreator.RunAudit(false)`、`ShipBoostTrailSceneBinder.RunAudit(false)` 与 `MaterialTextureLinker.RunAudit(false)`，并将下游 `Info` / `Warning` / `Error` 映射为 `ShipVfxValidator.ValidationResult`。新增回归测试覆盖“破坏 `mat_flame_trail._BaseMap` 后，总 Validator 能以 `Authority Audit/MaterialTextureLinker` scope 报告错误，同时不修复材质贴图”的只读聚合契约。

- **目的**：继续 Ship/VFX Phase A authority cleanup，把前面 A2-A5 分散的单点 Audit 收口到一个总控入口，确保 prefab、scene-only Bloom、材质贴图与 Ship prefab 结构漂移都能通过 `ShipVfxValidator` 一次性显式暴露，而不是要求开发者记住多个菜单顺序。

- **技术**：按 TDD 思路先新增 `RunAudit_IncludesMaterialTextureLinkerAuthorityErrorsWithoutRepairingIt` 失败测试，再实现最小泛型 `AppendAuthorityAuditResults` 和四个 `ConvertSeverity` 映射方法。聚合步骤只调用下游 `RunAudit(logToConsole: false)`，不调用任何 `CreateOrRebuild`、`ForceRebuild`、`Setup`、`Link`、`SetDirty` 或 `SaveAssets` 写入入口。验证：`dotnet build Project-Ark.slnx` 通过（仅保留第三方示例脚本既有 2 个 warning）。本轮 Unity MCP 的 `run_tests`、`execute_code` 与 `read_console` 多次出现会话未就绪或无明细返回，因此未声称 Unity EditMode 测试已完成。

---

## Ship/VFX ShipPrefabRebuilder Audit-only 模式 — 2026-05-31 22:12

- **新建/修改文件**
  - `Assets/Scripts/Ship/Editor/ShipPrefabRebuilder.cs`
  - `Assets/Scripts/Ship/Editor/ShipVfxValidatorTests.cs`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容**：为 `ShipPrefabRebuilder` 增加只读 `RunAudit(logToConsole)` 入口和菜单项 `ProjectArk/Ship/Authority/Audit Ship Prefab`。Audit 会检查 `Ship.prefab` 根组件、`ShipVisual` 现役子节点、旧 `Ship_Sprite_Liquid` / `Ship_Sprite_HL` / `Ship_Sprite_Solid` 回流、关键默认显隐状态，以及 `ShipView`、`ShipBoostVisuals`、`ShipHitVisuals`、`ShipDashVisuals`、`ShipFireVisuals`、`ShipVisualJuice`、`DashAfterImageSpawner` 的核心序列化引用。新增 EditMode 回归测试覆盖“删除 `Ship_Sprite_Body` 后 Audit 报错但不自动重建 prefab 子节点”的只读行为。

- **目的**：继续 Ship/VFX Phase A authority cleanup，把 `Ship.prefab` 唯一权威工具从单一 Apply/Rebuild 菜单推进到 Audit / Apply 分离，确保正式飞船 prefab 的结构漂移、legacy 节点回流或核心 worker 引用缺失时能被显式发现，而不是依赖重建菜单或运行时 fallback 暗中修复。

- **技术**：按 TDD 执行，先新增失败测试并在 Unity Editor 内确认 RED 条件为缺少 `ShipPrefabRebuilder.RunAudit(bool)`；再实现最小 `Severity`、`AuditResult`、`RunAudit` 与 prefab contents 只读检查 helper。`RunAudit` 只使用 `AssetDatabase.LoadAssetAtPath`、`PrefabUtility.LoadPrefabContents`、`SerializedObject` 读取状态，不调用 `SaveAsPrefabAsset`、`SetDirty`、`SaveAssets` 或 `Refresh`。验证：`dotnet build Project-Ark.slnx --no-restore` 通过（仅保留第三方示例脚本既有 2 个 warning）；Unity Editor 直接执行目标测试方法 `ShipPrefabRebuilderRunAudit_ReportsMissingShipBodyWithoutRebuildingIt` passed；直接执行当前权威状态 `ShipPrefabRebuilder.RunAudit(false)` 返回 0 error；Unity Console error 复查未发现项目脚本错误。Unity MCP Test Runner job 本轮两次初始化超时，未进入具体测试用例，因此未把该 job 作为业务测试失败处理。

---

## Ship/VFX BoostTrailPrefabCreator Audit-only 模式 — 2026-05-31 20:59

- **新建/修改文件**
  - `Assets/Scripts/Ship/Editor/BoostTrailPrefabCreator.cs`
  - `Assets/Scripts/Ship/Editor/ShipVfxValidatorTests.cs`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容**：为 `BoostTrailPrefabCreator` 增加只读 `RunAudit(logToConsole)` 入口和菜单项 `ProjectArk/Ship/VFX/Authority/Audit BoostTrailRoot Prefab`。Audit 会检查 `BoostTrailRoot.prefab` 是否存在、根节点命名、`BoostTrailView` / `BoostTrailDebugManager` 组件、`BoostTrailView._juiceSettings`、prefab 内 `_boostBloomVolume` 必须保持空引用、`_aresSustainParticles` 数组、`AresBoostTrail` 粒子层，以及旧 `MainTrail` / `FlameTrail_*` / `BoostEnergyLayer*` / `BoostActivationHalo` 等 legacy 节点不得回流。新增 EditMode 回归测试覆盖“删除 `AresBoostTrail` 后 Audit 报错但不自动重建 prefab 子节点”的只读行为。

- **目的**：继续 Ship/VFX Phase A authority cleanup，把 `BoostTrailRoot.prefab` 结构权威工具从单一 Apply/Rebuild 菜单推进到 Audit / Apply 分离，确保 prefab 结构漂移、legacy 节点回流或核心序列化引用缺失时能被显式审计，而不是依赖重建菜单或运行时 fallback 暗中修复。

- **技术**：按 TDD 执行，先新增失败测试并用 `dotnet build Project-Ark.slnx` 确认 RED 失败原因为缺少 `BoostTrailPrefabCreator.RunAudit` / `Severity`，再实现最小 `Severity`、`AuditResult`、`RunAudit` 和 prefab contents 只读检查 helper。`RunAudit` 只使用 `AssetDatabase.LoadAssetAtPath` 与 `PrefabUtility.LoadPrefabContents` 读取状态，不调用 `SaveAsPrefabAsset`、`SetDirty`、`SaveAssets` 或 `Refresh`。验证：`dotnet build Project-Ark.slnx` 通过（仅保留第三方示例脚本既有 2 个 warning）；Unity EditMode 目标测试 `BoostTrailPrefabCreatorRunAudit_ReportsMissingAresTrailWithoutRebuildingIt` 1/1 passed；整类 `ShipVfxValidatorTests` 5/5 passed；Unity Console error 复查未发现项目脚本错误。

---

## Ship/VFX MaterialTextureLinker Audit-only 模式 — 2026-05-31 19:29

- **新建/修改文件**
  - `Assets/Scripts/Ship/Editor/MaterialTextureLinker.cs`
  - `Assets/Scripts/Ship/Editor/ShipVfxValidatorTests.cs`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容**：为 `MaterialTextureLinker` 增加只读 `RunAudit(logToConsole)` 入口和菜单项 `ProjectArk/Ship/VFX/Authority/Audit Active BoostTrail Material Textures`。Audit 会检查当前工具声明的 BoostTrail 材质是否存在、指定 shader 是否匹配、各 texture slot 是否指向 `Assets/_Art/VFX/BoostTrail/Textures` 下的精确路径资源，并额外检查 `mat_trail_main._UseLegacySlots` 是否保持 0。新增 EditMode 回归测试覆盖“破坏 `mat_flame_trail._BaseMap` 后 Audit 报错但不自动修复”的只读行为。

- **目的**：继续 Ship/VFX Phase A authority cleanup，把现役材质贴图回填工具从单一 Apply 菜单推进到 Audit / Apply 分离，避免材质或贴图漂移只能通过写入型菜单发现，同时满足关键依赖缺失必须可审计、不可 silent no-op 的模块规则。

- **技术**：按 TDD 执行，先新增失败测试并用 `dotnet build Project-Ark.slnx` 确认 RED 失败原因为缺少 `MaterialTextureLinker.RunAudit` / `Severity`，再实现最小 `Severity`、`AuditResult`、`RunAudit` 和只读检查 helper。`RunAudit` 不调用 `SetDirty`、`SaveAssets` 或 `Refresh`，只读取 `Material`、`Shader`、`Texture2D` 与 shader property 状态并输出结构化结果。验证：`dotnet build Project-Ark.slnx` 通过（仅保留第三方示例脚本既有 2 个 warning）；Unity `TestResults.xml` 确认 `MaterialTextureLinkerRunAudit_ReportsBrokenTextureBindingWithoutRepairingIt` passed；Unity Console error 复查为 0。Unity MCP test job 状态未及时释放，整类测试未重复启动。

---

## Ship/VFX SceneBinder Audit-only 模式 — 2026-05-31 18:04

- **新建/修改文件**
  - `Assets/Scripts/Ship/Editor/ShipBoostTrailSceneBinder.cs`
  - `Assets/Scripts/Ship/Editor/ShipVfxValidatorTests.cs`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容**：为 `ShipBoostTrailSceneBinder` 增加只读 `RunAudit(logToConsole)` 入口和菜单项 `ProjectArk/Ship/VFX/Authority/Audit BoostTrail Scene Bloom References`。Audit 会检查 `BoostTrailBloomVolume` 场景对象、`UnityEngine.Rendering.Volume` 配置、`BoostBloomVolumeProfile.asset` 绑定，以及场景中 `BoostTrailView._boostBloomVolume` 是否指向 scene-only Bloom authority。新增 EditMode 回归测试覆盖“删除 `BoostTrailBloomVolume` 后 Audit 报错但不创建对象”的只读行为。

- **目的**：继续 Ship/VFX Phase A authority cleanup，把 scene-only Bloom 绑定从单一 Apply 菜单推进到 Audit / Apply 分离，避免未来排查时必须先写场景才能发现漂移，同时防止关键引用缺失退化为 silent no-op。

- **技术**：按 TDD 执行，先新增失败测试并确认 Unity Console 编译错误指向缺少 `ShipBoostTrailSceneBinder.RunAudit` / `Severity`，再实现最小 `Severity`、`AuditResult`、`RunAudit` 与只读序列化检查逻辑。`RunAudit` 不调用 `Undo`、`SetDirty`、`MarkSceneDirty` 或 `SaveScene`，只读取 scene/component/serialized properties 并输出结果。验证：`validate_script` 对 `ShipBoostTrailSceneBinder.cs` 返回 0 errors / 0 warnings；`dotnet build Project-Ark.slnx` 通过（仅保留第三方示例脚本既有 2 个 warning）；Unity `TestResults.xml` 确认 `SceneBinderRunAudit_ReportsMissingBloomVolumeWithoutCreatingIt` 1/1 passed。Unity MCP job 状态汇报曾出现初始化超时，但结果文件和 Console 输出确认测试实际执行并通过。

---

## Canary Ship Batch 11 Pool/Reset 验证 — 2026-05-31 17:00

- **新建/修改文件**
  - `Assets/Scripts/Ship/VFX/ShipVisualValidationView.cs`
  - `Docs/0_Plan/ongoing/2026-05-25-canary-ship-complete-art-vfx-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容**：完成 Batch 11 / Task 29 的 pool/reset 验证，并修复验证视图暴露出的两个防御性复位问题。Dash / Fire / Hit / Weaving / Overheat 各连续触发 10 次后均能回到 clean Normal；Play Mode 中对验证实例执行 SetActive false/true 后也能回到 clean Normal。计划中已勾选 `Pool/reset checks pass`，并在 Console 中按 `Ship` 过滤确认没有 Ship visual missing reference 相关 error/warning，Batch 11 Completion Gate 已全部完成。

- **目的**：关闭 Canary ship complete gameplay visual asset 的最终复位门槛，确保高风险视觉状态不会在重复触发、对象禁用重启或 Play Mode 退出后留下 sprite、颜色、alpha、scale、图层显隐残留。

- **技术**：按系统化调试先复现失败，再定位到 `Overheat -> Normal` 后 `Core` sprite 仍停留在 `spr_ship_canary_core_overheat_emission`。在 `ShipVisualValidationView` 中缓存默认 Core sprite，并在 `ResetVisuals()` 恢复；随后发现禁用/重启不会重新执行 `Awake()`，新增 `OnEnable()` 调用初始状态恢复，同时让默认 sprite 缓存只初始化一次，避免把脏 sprite 缓存成默认值。验证使用 Unity 临时代码比对 SpriteRenderer sprite/enabled/color/transform 签名，并检查该验证控制器没有 Material / ScriptableObject / AssetDatabase 写入路径。

## 场景 Warning 清理：TMP CardIcon 缺字降级 — 2026-05-31 10:15

- **新建/修改文件**
  - `Assets/Scenes/SampleScene.unity`
  - `Assets/Scripts/UI/ItemDetailView.cs`
  - `Assets/Scripts/UI/Editor/UICanvasBuilder.cs`
  - `Docs/0_Plan/ongoing/2026-05-25-canary-ship-complete-art-vfx-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容**：修复 Play Mode 中 TextMeshPro 对 `CardIcon` 的 fallback glyph warning。根因是 `CardIcon` 和 `ItemDetailView/TypeLabel` 使用 `U+25C8`（`◈`），默认 `LiberationSans SDF` 不包含该字形，运行时被替换为方框。将当前场景序列化文本、`ItemDetailView` 运行时类型标签、`UICanvasBuilder` 未来重建 UI 的默认文本统一改为 ASCII `>` / `>  CORE` 形式，避免依赖新增 TMP 字体资产。

- **目的**：用最小风险方式清理 UI Console 噪音，不引入新字体资源、不扩大 TMP fallback 链，也不改变星图装备逻辑。该修复只调整装饰性文本标记，保留类型颜色和布局结构。

- **技术**：先读取 Unity Console 确认 warning 来源为 `CardIcon`，再通过运行时代码采样定位对象路径与实际文本。修改源码和 `SampleScene.unity` 后执行 `dotnet build Project-Ark.slnx`，构建成功。随后同步 Unity Editor 当前打开场景内存状态并保存，清空 Console，强制 `TMP_Text.ForceMeshUpdate()` 与 `Canvas.ForceUpdateCanvases()`；复查 warning 列表为 0，确认 `U+25C8` / `CardIcon` fallback warning 不再出现。

---

## 场景 Warning 清理：Projectile_Matter TrailRenderer 固化 — 2026-05-31 10:07

- **新建/修改文件**
  - `Assets/_Data/StarChart/Prefabs/Projectile_Matter.prefab`
  - `Assets/Scripts/Combat/Editor/Batch5AssetCreator.cs`
  - `Docs/0_Plan/ongoing/2026-05-25-canary-ship-complete-art-vfx-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容**：修复 Play Mode 中 `[Projectile] 'Projectile_Matter(Clone)' has no TrailRenderer` 的运行时 fallback warning。将 `Projectile_Matter.prefab` 补齐预配置 `TrailRenderer`，参数与原 `Projectile.Awake()` fallback 保持一致：`time = 0.15`、头部宽度 `0.085`、白色渐隐、`minVertexDistance = 0.1`、`LineAlignment.TransformZ`，并复用 `SpriteRenderer.sharedMaterial`。同步更新 legacy `Batch5AssetCreator.CreateProjectilePrefab()`，使未来重新生成 `Projectile_Matter` 时不会再次缺少 `TrailRenderer`。

- **目的**：把 projectile trail 从运行时自动补组件转回 authored prefab 状态，减少 Console 噪音并避免对象池预热阶段动态添加组件。这个修复不改变射击数值、不改 `Projectile` runtime 发射逻辑，也不引入新的 fallback path。

- **技术**：先用 Unity 代码验证红灯状态：`Projectile_Matter.prefab` 缺 `TrailRenderer`；再通过 `PrefabUtility.LoadPrefabContents()` / `SaveAsPrefabAsset()` 对 prefab 做安全写入。修复后执行静态检查确认 prefab 上 `TrailRenderer` 存在且参数正确；`dotnet build Project-Ark.slnx` 编译通过。清空 Console 后进入 Play Mode，`StarChartController` 初始化对象池时不再出现 `Projectile_Matter(Clone)` / `TrailRenderer` fallback warning；运行时采样确认 20 个 `Projectile_Matter` 池化实例全部带 `TrailRenderer`。

---

## Canary Ship/VFX Plan 状态收口 — 2026-05-31 10:00

- **新建/修改文件**
  - `Docs/0_Plan/ongoing/2026-05-25-canary-ship-complete-art-vfx-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容**：同步 Canary Ship/VFX ongoing plan 中已过期的状态项：将 Batch 4 的 `Canary_Dash.anim` 从 deferred 改为已完成，并记录 0.25 秒 Dash burst 回到 Idle / Normal body 的验证结论；将 Batch 5 的 Boost-vs-Dash gate 改为已完成，明确 Dash 是短压缩 / smear burst，Boost 是 `BoostTrailRoot/AresBoostTrail` 的持续推进读感；将末尾 Unity 在线 Play Mode / Console 复查从“待完成”更新为已完成，记录三类目标 warning 已消失。

- **目的**：让 plan 当前状态与已经完成的 Unity / Play Mode 验证证据一致，避免后续继续围绕已关闭的 Dash、Boost-vs-Dash、场景三类 warning 进行重复工作，并把下一步收束到真正剩余的非阻断 warning cleanup。

- **技术**：只修改计划文档，不改 runtime、prefab、scene 或资产主链。基于既有实现日志与 Unity 在线复查记录做文档状态收口，保留 Ship/VFX authority 约束：不把 preview Animator 升级为正式 runtime owner，不引入 Dash additive trail / particle 作为当前 MVP blocker。

---

## 场景 Warning 配置清理：Ambience / Room 起始配置 — 2026-05-31 09:22

- **新建/修改文件**
  - `Assets/Scenes/SampleScene.unity`
  - `Docs/0_Plan/ongoing/2026-05-25-canary-ship-complete-art-vfx-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容**：完成一轮非阻断场景 warning cleanup。将 `AmbienceController._postProcessVolume` 绑定到场景现有 `Global Volume` 组件；将 `RoomManager._startingRoom` 绑定到 `DebugRoom` 的 `Room` 组件；将 `DebugRoom` 与 `TargetRoom` 上会被 `Room.Awake()` 读取并自动修复的第二个 `BoxCollider2D` 序列化为 trigger，避免每次进入 Play Mode 都输出 room trigger auto-fix warning。额外核对剩余 `m_IsTrigger: 0` collider，确认它们属于 Tilemap / Composite 物理碰撞链，不属于 `Room` 触发器。

- **目的**：把 Play Mode 中已确认的场景配置 warning 从运行时自动修复转为 authored scene 状态，减少 Console 噪音，让后续可玩性验证更聚焦在真正的 gameplay / VFX 问题上。

- **技术**：先根据 `AmbienceController`、`Room`、`RoomManager` 的生命周期和 warning 文案追踪根因，再在 `SampleScene.unity` 中做精确 YAML 修改：`RoomManager._startingRoom -> 413451006`，`AmbienceController._postProcessVolume -> 300000003`，`DebugRoom` collider `413451007` 与 `TargetRoom` collider `1950440151` 的 `m_IsTrigger` 改为 `1`。执行 `dotnet build Project-Ark.slnx` 通过。Unity MCP 会话在最终在线复查前终止，因此仍需在 Unity Editor 恢复后刷新场景并进 Play Mode 复查 Console。

---

## Canary Ship Play Mode 手感验证 — 2026-05-31 00:59

- **新建/修改文件**
  - `Docs/0_Plan/ongoing/2026-05-25-canary-ship-complete-art-vfx-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容**：完成一轮正式 Play Mode feel validation。通过正式 runtime 入口验证 `Normal / Lean / Dash / Boost / Hit` 链路：`Normal` baseline 清洁；`Boost` 通过 `ShipStateController.ToStateForce(Boost)` 启动 7 个 `AresBoostTrail` 粒子并在回到 `Normal` 后归零；`Dash` 切换到 `spr_ship_canary_dash_01`、隐藏 outline、启用 `Dodge_Sprite`，回到 `Normal` 后恢复 `spr_ship_canary_body_normal_albedo`；`Lean` 横向采样确认右/左分别使用 `spr_ship_canary_lean_right_03` / `spr_ship_canary_lean_left_03` 并隐藏 outline；`Hit` 通过 `ShipHealth.TakeDamage()` 启用 `Ship_HitMaskFlash`，`ShipView.ResetVFX()` 后恢复 disabled / alpha 0。

- **目的**：关闭当前 Ship/VFX MVP 的现场手感验证门槛，确认正式 `Ship.prefab` runtime worker 主链已经能表达 Normal、Lean、Dash、Boost、Hit 的核心可读性，不需要用 Dash additive trail / particle polish 阻塞当前批次。

- **技术**：验证只通过现役 runtime owner 执行，不修改代码或 prefab：状态切换走 `ShipStateController`，受击走 `ShipHealth`，视觉恢复走 `ShipView.ResetVFX()`。执行 `ProjectArk/Ship/VFX/Audit/Run Ship VFX Audit` 后，`ShipVfxValidator` 返回 `0 errors, 0 warnings, 3 info`。验证中一次 MCP 临时脚本调用 `Physics2D.Simulate` 触发非项目代码错误，已清空 Console 并重跑 audit，最终 Console 仅保留 validator info。

---

## Canary Lean / Dash 正式运行时 Sprite 动画接入 — 2026-05-30 14:13

- **新建/修改文件**
  - `Assets/Scripts/Ship/Data/ShipJuiceSettingsSO.cs`
  - `Assets/Scripts/Ship/VFX/ShipVisualJuice.cs`
  - `Assets/Scripts/Ship/VFX/ShipDashVisuals.cs`
  - `Assets/Scripts/Ship/VFX/ShipView.cs`
  - `Assets/Scripts/Ship/Tests/ShipVisualJuiceTests.cs`
  - `Assets/_Data/Ship/DefaultShipJuiceSettings.asset`
  - `Assets/_Prefabs/Ship/Ship.prefab`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容**：修复正式飞船行驶中看不到 Canary `Lean` / `Dash` 逐帧动画的问题。根因是 `Canary_LeanLeft.anim`、`Canary_LeanRight.anim`、`Canary_Dash.anim` 只接入了 `CanaryShipVisualPreview.prefab` 的预览 Animator，正式 `Ship.prefab` 运行链只执行程序化 tilt、i-frame flicker、Dodge ghost 和 after-image，没有任何 runtime 字段引用 Canary Lean/Dash sprite 帧。本次将 `Normal / Lean Left / Lean Right / Dash` body sprite 帧作为 `ShipJuiceSettingsSO` 数据配置接入，`ShipVisualJuice` 根据横向移动分量切换 Lean sprite，`ShipDashVisuals` 在进入 Dash 时播放 5 帧 one-shot Dash sprite burst，Dash 结束时恢复 normal 并由 `ShipView` 刷新当前 Lean/Normal 姿态。

- **目的**：让玩家在正式飞船驾驶中实际看到 Lean 和 Dash 的 sprite 姿态变化，而不是只能在预览 prefab / Animator 中验证。实现保持现役 `Ship/VFX` worker 主链，不把预览 Animator 升级为正式 runtime owner，避免新增双轨主链或 debug 接管正式链。

- **技术**：沿用 `ShipView` → worker 的事件路由：`ShipView.InitializeWorkers()` 将正式 `_solidRenderer` 注入 `ShipVisualJuice`；Dash 仍由 `ShipStateController.OnStateChanged(Dash)` 路由到 `ShipDashVisuals.OnDashStarted()`。Dash sprite 播放使用 `UniTask.Delay` + `CancellationTokenSource`，对象池 / VFX 复位路径中会取消并恢复 normal sprite。`DefaultShipJuiceSettings.asset` 直接引用已存在的 Canary sprite GUID，未手写新 `.meta`。验证：`dotnet build Project-Ark.slnx` 成功，Unity Console 未出现本轮新增 error；当前仍有既有弃用 / 未使用 warning。

---

## Canary Dash Play Mode 预览播放验证 — 2026-05-30 13:30

- **新建/修改文件**
  - `Assets/_Prefabs/Ship/CanaryShipVisualPreview.prefab`
  - `Assets/_Art/Ship/Canary/Animations/CanaryShipVisualPreview.controller`
  - `Assets/Screenshots/canary_dash_runtime_128_contact_sheet.png`
  - `Assets/Screenshots/canary_dash_runtime_128_contact_sheet.png.meta`
  - `Assets/Screenshots/canary_dash_animator_runtime_128_contact_sheet.png`
  - `Assets/Screenshots/canary_dash_animator_runtime_128_contact_sheet.png.meta`
  - `Docs/0_Plan/ongoing/2026-05-25-canary-ship-complete-art-vfx-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容**：修复 `CanaryShipVisualPreview.prefab` 的播放入口：给 prefab 根节点添加 `Animator`，绑定 `CanaryShipVisualPreview.controller`，关闭 root motion，并设置 Always Animate。为 `Canary_Dash` 状态补充 exit-time transition，0.25 秒播放完后自动回到 `Canary_Idle`。随后进入 Play Mode，用运行时 `Animator.Play("Canary_Dash")` + `Animator.Update()` 采样验证：0.00 / 0.05 / 0.10 / 0.15 / 0.20 秒依次为 `spr_ship_canary_dash_01` 至 `spr_ship_canary_dash_05`，0.25 秒回到 `spr_ship_canary_body_normal_albedo` / `Canary_Idle`。

- **目的**：关闭 `Task 15B` 的运行时预览验证门槛，确认真实 Dash preview playback 不会变成长 Boost 读感，也没有因为缺 Animator 导致 clip 只停留在静态资产层。当前结论是 Dash 读作 0.25 秒短压缩 / smear burst；Boost 仍读作 `BoostTrailRoot/AresBoostTrail` 的持续推进，二者在预览层已区分。

- **技术**：使用 `PrefabUtility.LoadPrefabContents` 安全修改 prefab 资产，用 `AnimatorController` API 写入 Dash→Idle transition。Play Mode 验证不修改正式 `Ship.prefab` 或 `BoostTrailRoot.prefab`，只实例化 `CanaryShipVisualPreview.prefab` 临时对象并销毁。Console 复查未发现本轮新增错误；现有 warning 仍为既有 Missing Script、`BoostTrailView` scene-only Bloom 引用、Room/ServiceLocator/Minimap/Ambience 等配置类 warning。

---

## Canary Boost / Dash 资产级低分辨率对照 — 2026-05-30 13:13

- **新建/修改文件**
  - `Assets/Screenshots/canary_dash_128_contact_sheet.png`
  - `Assets/Screenshots/canary_dash_128_contact_sheet.png.meta`
  - `Docs/0_Plan/ongoing/2026-05-25-canary-ship-complete-art-vfx-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容**：生成 Dash 5 帧 128px contact sheet，并与现有 `Assets/Screenshots/boost-trail-playmode-check-final.png` 以及 `Assets/_Prefabs/VFX/BoostTrailRoot.prefab` 的 `BoostTrailRoot/AresBoostTrail` 结构做资产级对照。结论：Dash 静态帧在 128px 下读作短压缩 / smear burst；Boost 仍由 `BoostTrailView` 驱动的 7 组 sustain particles / AresBoostTrail 形成持续推进语言，二者在资产级低分辨率读感上不重叠。

- **目的**：关闭 `Task 15B` 的静态资产级 Boost / Dash 区分风险，避免 Dash 误继承 Boost 的长拖尾语言。下一步只剩运行时播放确认：检查 `Canary_Dash.anim` 的 0.25 秒播放是否有跳帧、残留 alpha / scale、或意外长 trail 读感。

- **技术**：先尝试用本地 Python/PIL 生成接触表，但环境缺少 PIL；随后改用 Unity Editor `Texture2D` 直接读取原始 PNG 字节生成 contact sheet，避免 TextureImporter 缓存或裁剪数据干扰。MCP 读取 `BoostTrailRoot.prefab` 层级曾超时，改为直接读取 prefab YAML 验证 `BoostTrailView`、`AresBoostTrail` nested prefab 与 sustain particle 引用。未修改正式 `Ship.prefab`、`BoostTrailRoot.prefab`、Runtime 脚本或 Ship/VFX authority 主链。

---

## Canary Dash 官方命名与 Preview Clip 接入 — 2026-05-30 13:13

- **新建/修改文件**
  - `Assets/_Art/Ship/Canary/Sprites/Dash/spr_ship_canary_dash_01.png`
  - `Assets/_Art/Ship/Canary/Sprites/Dash/spr_ship_canary_dash_01.png.meta`
  - `Assets/_Art/Ship/Canary/Sprites/Dash/spr_ship_canary_dash_02.png`
  - `Assets/_Art/Ship/Canary/Sprites/Dash/spr_ship_canary_dash_02.png.meta`
  - `Assets/_Art/Ship/Canary/Sprites/Dash/spr_ship_canary_dash_03.png`
  - `Assets/_Art/Ship/Canary/Sprites/Dash/spr_ship_canary_dash_03.png.meta`
  - `Assets/_Art/Ship/Canary/Sprites/Dash/spr_ship_canary_dash_04.png`
  - `Assets/_Art/Ship/Canary/Sprites/Dash/spr_ship_canary_dash_04.png.meta`
  - `Assets/_Art/Ship/Canary/Sprites/Dash/spr_ship_canary_dash_05.png`
  - `Assets/_Art/Ship/Canary/Sprites/Dash/spr_ship_canary_dash_05.png.meta`
  - `Assets/_Art/Ship/Canary/Animations/Canary_Dash.anim`
  - `Assets/_Art/Ship/Canary/Animations/Canary_Dash.anim.meta`
  - `Assets/_Art/Ship/Canary/Animations/CanaryShipVisualPreview.controller`
  - `Docs/0_Plan/ongoing/2026-05-25-canary-ship-complete-art-vfx-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容**：将 Dash 源帧从临时 `dash1.png` 至 `dash5.png` 连同各自 `.meta` 重命名为官方 `spr_ship_canary_dash_01.png` 至 `spr_ship_canary_dash_05.png`，保留原 GUID。通过 Unity TextureImporter 将 5 张 Dash sprite 统一为 Sprite Single、PPU 320、center pivot、no mip maps、Point filter、Default platform uncompressed。新增 `Canary_Dash.anim`，用 5 张官方 Dash sprite 绑定预览 prefab 的 `Body` SpriteRenderer，采样率 20，时长 0.25 秒，非循环；并在 `CanaryShipVisualPreview.controller` 中接入 `Canary_Dash` 状态。

- **目的**：完成 `Task 15B` 前置的真实 Dash sprite 接入，使 Boost / Dash 读感对照不再停留在合同级假设。Dash 现在可以以 0.25 秒短爆发 clip 参与后续低分辨率可读性验证。

- **技术**：使用文件系统移动同时迁移 `.meta`，避免 Unity 重新生成 GUID；随后通过 Unity MCP 刷新 AssetDatabase 并用 Editor Importer API 批量设置 TextureImporter。用 `AnimationUtility.SetObjectReferenceCurve` 创建 SpriteRenderer `m_Sprite` 曲线，并通过 AnimatorController API 新增 / 更新 `Canary_Dash` 状态。Console 复查未发现由本次 Dash 资源和动画新增引入的新错误；当前 Console 中仍存在若干既有 warning（Missing script、BoostTrailView scene-only Bloom 引用、Room/ServiceLocator 等）。

---

## Canary Dash Sprite 源帧同步 — 2026-05-30 12:57

- **新建/修改文件**
  - `Docs/0_Plan/ongoing/2026-05-25-canary-ship-complete-art-vfx-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容**：同步 Dash sprite 已补齐的信息。计划中的 Batch 3 从整体 `Deferred` 改为 `Asset Frames Ready, Unity Validation Pending`，记录当前源帧已存在于 `Assets/_Art/Ship/Canary/Sprites/Dash/dash1.png` 至 `dash5.png`，同时保留官方工作流目标名 `spr_ship_canary_dash_01-05.png`。`Current Next Action` 中的 `Task 15B` 改为使用真实 Dash 源帧与现役 `BoostTrailRoot/AresBoostTrail` 做 Boost / Dash 读感对照。

- **目的**：避免继续把 Dash 当作纯合同级 deferred 项处理。下一步应先做 Dash 命名 / import settings / 短 clip 验证，再判断是否需要额外 Dash trail 或 particle 支撑；Boost 必须保持持续推进读感，Dash 必须保持短爆发 / smear 读感。

- **技术**：仅同步计划与日志，不修改 Unity 资产、Prefab、Scene、Runtime 脚本或 Ship/VFX authority 主链。通过文件系统确认 `Dash/` 目录下已有 5 张源图；当前不直接重命名或改 `.meta`，避免在未确认用户期望命名迁移方式前破坏 Unity 资产 GUID / 引用。

---

## Canary Boost 下一步指针推进 — 2026-05-30 12:54

- **新建/修改文件**
  - `Docs/0_Plan/ongoing/2026-05-25-canary-ship-complete-art-vfx-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容**：修正 Canary Ship 完整 Art/VFX plan 的 `Current Next Action`。将过期的 `Task 15A: Boost source audit + adaptation target confirmation` 推进为 `Task 15B: Boost vs Dash distinction validation`，并在上下文中明确 `Task 15A / Task 16` 已完成：QFZ `VFX_Ares_Projectile` 已审计、`VFX_Ares_Projectile_Only.prefab` 已通过 `BoostTrailPrefabCreator` 接入 `BoostTrailRoot.prefab`，剩余 Batch 5 gate 是 Boost 与未来 Dash 读感区分。

- **目的**：避免后续执行继续卡在已完成的 Boost source audit 上，把计划焦点收口到玩家可读性风险：Boost 必须保持持续推进读感，Dash 仍应是短爆发 / smear，不应继承长拖尾语言。

- **技术**：仅做文档计划推进，不修改 Unity 资产、Prefab、Scene、Runtime 脚本或 Ship/VFX authority 主链。下一步验证应优先使用现役 `BoostTrailRoot/AresBoostTrail` 链路作为 Boost 样本，并以 Dash 合同级视觉约束完成低分辨率可读性对比。

---

## Canary 受击 Hit Mask Overlay 接入 — 2026-05-29 15:54

- **新建/修改文件**
  - `Assets/_Art/Ship/Canary/Textures/Masks/spr_ship_canary_shape_hit_mask.png`
  - `Assets/_Art/Ship/Canary/Textures/Masks/spr_ship_canary_shape_hit_mask.png.meta`
  - `Assets/Scripts/Ship/VFX/ShipHitVisuals.cs`
  - `Assets/Scripts/Ship/Editor/ShipPrefabRebuilder.cs`
  - `Assets/Scripts/Ship/Tests/ShipHitVisualsTests.cs`
  - `Docs/0_Plan/ongoing/2026-05-25-canary-ship-complete-art-vfx-plan.md`
  - `Docs/2_TechnicalDesign/Ship/ShipVFX_CanonicalSpec.md`
  - `Docs/2_TechnicalDesign/Ship/ShipVFX_AssetRegistry.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容**：完成 Task 18 Hit Mask MVP。新增 `spr_ship_canary_shape_hit_mask.png` 作为透明白色 overlay mask，视觉范围聚焦船体外轮廓与中轴/core 区域，避免整船白色矩形闪烁。`ShipHitVisuals` 新增 `_hitMaskRenderer` 与 `_enableHitMask`，正伤害时短促显示并淡出 mask，`ResetState()` 会停止补间并隐藏 overlay。`ShipPrefabRebuilder` 作为 `Ship.prefab` 唯一权威，新增 `Ship_HitMaskFlash` 节点、导入 PNG 为 Sprite、默认禁用/alpha 0，并将其接线到 `ShipHitVisuals._hitMaskRenderer`。测试文件补充正伤害显示 Hit Mask overlay 的用例。

- **目的**：把受击反馈从“整船白闪 + Hit Spark”推进到更可读的外轮廓/core 混合遮罩，强化被击中的瞬时读感，同时保持 Canary 船体身份识别度，不新增 shader、不新增 runtime fallback、不让 debug 或 scene override 成为第二真相源。

- **技术**：沿用 Ship/VFX Worker 模式与 `ShipPrefabRebuilder` authority builder。Mask 资产为程序化生成的 512×512 RGBA PNG，Unity `.meta` 由 Editor/AssetDatabase 自动生成；运行时用 PrimeTween 对 `SpriteRenderer` alpha 做短促淡出，并在防御性复位中清理 tween/alpha/enabled 状态。验证：`dotnet build Project-Ark.slnx` 成功，`ProjectArk.Ship`、`ProjectArk.Ship.Editor`、`ProjectArk.Ship.Tests` 均编译通过；输出仅包含项目既有 warning。`dotnet test Project-Ark.slnx --filter FullyQualifiedName~ShipHitVisualsTests` 仅触发 Unity 测试程序集构建，未作为 Unity Test Runner 通过证据。当前 shell 环境未找到 Unity 可执行文件，无法自动执行 `ShipPrefabRebuilder.RebuildSpriteLayersSilently()`；`Ship.prefab` 尚未写入 `Ship_HitMaskFlash`，需要在 Unity Editor 中执行 `ProjectArk/Ship/Authority/Rebuild Ship Prefab` 后再做 Play Mode 受击视觉确认。

---

## 清理 Ship 场景旧 Glitch Sprite 覆盖层 — 2026-05-29 00:54

- **新建/修改/删除文件**
  - `Assets/Scripts/Ship/Editor/ShipPrefabRebuilder.cs`
  - `Assets/_Prefabs/Ship/Ship.prefab`
  - `Assets/Scenes/SampleScene.unity`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容**：排查用户截图中新版 Canary 船体上方仍有紫色旧 sprite 覆盖的问题。根因不是 `player_test_fire`，而是 `Ship.prefab` 与当前 `SampleScene` 实例中仍残留旧 Glitch 三层：`Ship_Sprite_Liquid -> Movement_3.png`、`Ship_Sprite_HL -> Movement_21.png`、`Ship_Sprite_Solid -> Movement_10.png`。已让 `ShipPrefabRebuilder` 在普通 rebuild 时也显式删除这三个 legacy visual nodes，并执行 authority rebuild 清理 `Ship.prefab`；随后清理/刷新当前场景实例中的对应 scene override。

- **目的**：彻底移除旧紫色 GG/Glitch 船体覆盖层，保证正式 Ship/VFX 主链只显示 Canary/Ares 现役资产，不再依赖旧 sprite，也不通过 runtime hide/fallback 掩盖问题。

- **技术**：使用 Unity probe 先列出 `Ship.prefab` 与当前场景 `SpriteRenderer` 的 sprite path / sorting order / active 状态，建立 RED：`Movement_3/10/21` 仍参与渲染。修复后运行 `ShipPrefabRebuilder.RebuildSpriteLayersSilently()`，并验证：`Ship.prefab` 与场景实例均无 `Movement_3/10/21` renderer；当前场景中没有任何 `SpriteRenderer` 使用 `Assets/_Art/Ship/Glitch/` 或 `Assets/_Art/Ship/GGReplica/` 飞船旧资源；`dotnet build Project-Ark.slnx --no-restore` 成功（仅既有 warning）；Scene View 截图确认主船体不再有紫色旧层覆盖。Console 仍有既有 SampleScene UI Missing Script 错误，与本次 Ship/VFX 修复无关。

---

## 删除旧紫色 Fire Test Sprite 残留 — 2026-05-29 00:21

- **新建/修改/删除文件**
  - `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaAssetImporter.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaPlayerSkinAssetBuilder.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaPlayerSkinAssetBuilderTests.cs`
  - `Assets/Scripts/Ship/Editor/GGReplica/V2/GGReplicaGlitchV2PrefabBuilder.cs`
  - `Assets/Scripts/Ship/Editor/ShipPrefabRebuilder.cs`
  - `Assets/_Data/Ship/GGReplicaPlayerSkin.asset`
  - `Assets/_Data/Ship/GGReplicaShipVisualProfile.asset`
  - `Assets/_Prefabs/Ship/Ship_GGReplica.prefab`
  - `Assets/_Prefabs/Ship/Ship_GGReplicaV2.prefab`
  - `Assets/_Art/Ship/Glitch/Reference/player_test_fire.png`（删除）
  - `Assets/_Art/Ship/GGReplica/Sprites/player_test_fire.png`（删除）
  - `Docs/2_TechnicalDesign/Ship/ShipVFX_CanonicalSpec.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容**：删除旧紫色 `player_test_fire` sprite 的两份项目内副本，并清掉它在 GGReplica importer、PlayerSkin builder、V2 prefab builder、测试、数据资产与 legacy prefab 中的所有导入/构建/序列化残留。`ShipPrefabRebuilder` 中旧的 `import player_test_fire.png` 提示已改为当前 Canary Body silhouette 路径提示。`ShipVFX_CanonicalSpec` 不再把 `Reference/player_test_fire.png` 记录为 Dash ghost 来源。

- **目的**：修正 Fire MVP 后旧紫色 sprite 仍可被工具或序列化残留带回正式/调试链路的问题。该资产属于旧参考资产，不应作为 Canary Fire、WeaponMount 或 Dash 的现役来源；本次处理选择直接删除和清空引用，而不是替换成另一条兼容 fallback。

- **技术**：按 root-cause 清理而非视觉补丁处理：先用 grep 验证 `player_test_fire` / `229513161421663746` / 两个旧 GUID 的 RED 状态，再移除 importer entry、builder hardcode、serialized references 与文档口径。验证：`Assets` 下已无旧 sprite 名称、fileID 或 GUID 残留；`dotnet build Project-Ark.slnx --no-restore` 成功（仅既有 warning）；Unity probe 确认 `Ship.prefab` 中 `Ship_Sprite_WeaponMount` 指向 Canary WeaponMount，`Dodge_Sprite` 指向 Canary Body silhouette，`Ship_Sprite_Core` 指向 Canary Core；Unity Console 当前无 error。

---

## Ship/VFX 文档同步与 Fire MVP 接入 — 2026-05-28 23:47

- **新建/修改文件**
  - `Assets/Scripts/Ship/VFX/ShipFireVisuals.cs`
  - `Assets/Scripts/Ship/VFX/ShipView.cs`
  - `Assets/Scripts/Ship/Data/ShipJuiceSettingsSO.cs`
  - `Assets/Scripts/Ship/Editor/ShipPrefabRebuilder.cs`
  - `Assets/Scripts/Ship/Tests/ShipFireVisualsTests.cs`
  - `Assets/_Prefabs/Ship/Ship.prefab`
  - `Docs/0_Plan/ongoing/2026-05-25-canary-ship-complete-art-vfx-plan.md`
  - `Docs/2_TechnicalDesign/Ship/ShipVFX_CanonicalSpec.md`
  - `Docs/2_TechnicalDesign/Ship/ShipVFX_AssetRegistry.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容**：先将 Ship/VFX 规范与注册表同步为当前真实 Ares-only Boost 链路，移除旧 `MainTrail` / `FlameTrail_*` / `FlameCore` / `Ember*` / `BoostEnergyLayer*` 的 live 口径，并把旧 Boost 材质、shader、纹理降级为 `Legacy Reference`。随后接入 Fire MVP：新增 `ShipFireVisuals` Worker，由 `ShipView` 订阅 `CombatEvents.OnPlayerProjectileFired` 后统一路由，短促点亮 `Ship_Sprite_WeaponMount` 与 `Ship_Sprite_Core`，并在 `ResetState()` 中恢复颜色和 scale。`ShipJuiceSettingsSO` 新增 Fire 颜色、scale、attack/release 参数；`ShipPrefabRebuilder` 负责 ensure/wire `ShipFireVisuals`、`ShipView._fireVisuals` 与 `ShipView._weaponMountRenderer`。

- **目的**：在进入 Batch 6 Fire/Hit 前先清除 Boost 文档认知污染，并交付一个最小可玩的 Fire 反馈切片：开火时短、亮、清楚，不抢 Boost 视觉语言，不改变船体 Body 身份，且仍保持 `ShipView` 作为唯一 runtime 路由入口。

- **技术**：沿用 Ship/VFX Worker 模式与 authority builder 接线；Fire 参数保持数据驱动，避免把手感数值硬编码在 prefab 或 scene override 中。验证：`dotnet build Project-Ark.slnx --no-restore` 成功（仅既有 warning）；执行 `ShipPrefabRebuilder.RebuildSpriteLayersSilently()` 后，Unity probe 确认 `Ship.prefab` 上 `ShipFireVisuals` 存在，`ShipFireVisuals._weaponMountRenderer` / `_coreRenderer` / `_juiceSettings` 与 `ShipView._fireVisuals` / `_weaponMountRenderer` 均已接线；运行时 probe 返回 `PASS ShipFireVisuals OnWeaponFired/ResetState runtime check`。Unity Test Runner 当前对 `ProjectArk.Ship.Tests` 返回 `total=0`，未将其作为有效通过证据。Console 中仍有 3 条已定位的无关 Missing Script：`SampleScene.unity` 的 `SpaceLifeCanvas/DialogueUI`、`GiftUI`、`NPCInteractionUI`。

---

## Boost Trail 旧特效完全删除 — 2026-05-28 23:28

- **新建/修改文件**
  - `Assets/Scripts/Ship/Editor/BoostTrailPrefabCreator.cs`
  - `Assets/Scripts/Ship/VFX/BoostTrailView.cs`
  - `Assets/Scripts/Ship/VFX/BoostTrailDebugManager.cs`
  - `Assets/Scripts/Ship/Editor/BoostTrailDebugManagerEditor.cs`
  - `Assets/Scripts/Ship/Data/ShipJuiceSettingsSO.cs`
  - `Assets/Scripts/Ship/Editor/ShipVfxValidator.cs`
  - `Assets/_Prefabs/VFX/BoostTrailRoot.prefab`
  - `Assets/_Prefabs/Ship/Ship.prefab`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容**：彻底移除正式 Boost 链路里的旧版可见特效层：`MainTrail`、`FlameTrail_R`、`FlameTrail_B`、`FlameCore`、`EmberTrail`、`EmberSparks`、`BoostEnergyLayer2`、`BoostEnergyLayer3`。`BoostTrailPrefabCreator` 现在只生成 `AresBoostTrail`；`BoostTrailView` 只负责 Ares sustained particles 与 Bloom burst；`BoostTrailDebugManager` / 自定义 Inspector 只保留 Ares 与 Bloom 的预览开关；`ShipJuiceSettingsSO` 删除旧 flame/ember/energy/trail 参数；`ShipVfxValidator` 改为禁止旧 Boost 子节点并要求 `AresBoostTrail` + `_aresSustainParticles` 有效，同时同步 Canary 船体节点命名。

- **目的**：解决玩家仍能看到旧 Boost trail / flame / ember / energy layer 残留的问题，让正式玩家船 Boost 表现完全走 QFZ Ares 改造链路，避免旧视觉层继续参与运行时表现或被 builder 重建回来。

- **技术**：按 Ship/VFX authority 规则从唯一 prefab builder、runtime controller、debug preview、validator 四层同时清理旧链路；重新执行 `BoostTrailPrefabCreator.CreateOrRebuildBoostTrailRootPrefab(false)` 与 `ShipPrefabRebuilder.ForceRebuildSpriteLayersSilently()`。验证：`dotnet build Project-Ark.slnx` 成功（仅既有 warning）；Unity 结构探针显示 `BoostTrailRoot.prefab` 与 `Ship.prefab` 内嵌 `BoostTrailRoot` 均为 `PASS childCount=1 aresParticles=7`；`Assets/_Prefabs` grep 不再包含旧 Boost 节点/字段名；Console 清空后无 error/warning。

---

## Ares Boost Trail 持续发射修复 — 2026-05-28 23:17

- **新建/修改文件**
  - `Assets/Scripts/Ship/Editor/BoostTrailPrefabCreator.cs`
  - `Assets/_Prefabs/VFX/BoostTrailRoot.prefab`
  - `Assets/_Prefabs/Ship/Ship.prefab`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容**：修复 `AresBoostTrail` 已被嵌入但 Boost 时几乎不可见的问题。根因是 QFZ `VFX_Ares_Projectile_Only.prefab` 的 sustained layers 原本依赖 `rateOverDistance`，上次适配为 local Boost trail 时把 `rateOverDistance` 清零，却没有补 `rateOverTime`，导致正式 `Ship.prefab` 中 7 个 Ares 粒子层发射率全为 0，仅剩极弱的一次性 burst。现在 `BoostTrailPrefabCreator` 会按粒子层名写入非零 `rateOverTime`，放大 `AresBoostTrail` 到 0.68，并提高 sorting order，让 Ares 粒子成为 Boost sustained read 的可见层。

- **目的**：让正式玩家船在 Boost 时真正看到 `VFX_Ares_Projectile` 改造后的持续拖尾，而不是继续主要显示旧版 Boost trail / flame 效果。

- **技术**：通过 Unity 诊断正式 `Ship.prefab` 中 `BoostTrailView._aresSustainParticles`，确认数组有 7 个引用但 `rateTime=0.00`；修复 builder 后重新执行 `BoostTrailPrefabCreator.CreateOrRebuildBoostTrailRootPrefab(false)` 与 `ShipPrefabRebuilder.ForceRebuildSpriteLayersSilently()`。验证结果：正式 `Ship.prefab` 中 Ares 粒子层 `rateTime` 分别为 8/14/26/16/28/10/10，`OnBoostStart()` runtime probe 显示 7 个粒子层均 `isPlaying=True` 且 `isEmitting=True`；`dotnet build Project-Ark.slnx` 成功（仅既有 warning）；Unity Console 清空后无 error/warning。

---

## Canary 正式 Ship.prefab 迁移 — 2026-05-28 22:52

- **新建/修改文件**
  - `Assets/Scripts/Ship/Editor/ShipPrefabRebuilder.cs`
  - `Assets/Scripts/Ship/Editor/BoostTrailPrefabCreator.cs`
  - `Assets/_Prefabs/Ship/Ship.prefab`
  - `Assets/_Prefabs/VFX/BoostTrailRoot.prefab`
  - `Assets/_Data/Ship/DefaultShipJuiceSettings.asset`
  - `Docs/0_Plan/ongoing/2026-05-25-canary-ship-complete-art-vfx-plan.md`
  - `Docs/2_TechnicalDesign/Ship/ShipVFX_AssetRegistry.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容**：将正式 `Assets/_Prefabs/Ship/Ship.prefab` 从旧 GG `Movement_*` 三层视觉迁移到 Canary Normal playable set。`ShipPrefabRebuilder` 改为通过唯一 prefab authority 生成 `Ship_Sprite_Body`、`Ship_Sprite_Shape`、`Ship_Sprite_Outline`、`Ship_Sprite_Core`、`Ship_Sprite_WeaponMount`、`Dodge_Sprite` 与 nested `BoostTrailRoot`；新增无弹窗 rebuild / force rebuild 入口，方便自动化清理旧 managed 节点。`DefaultShipJuiceSettings` 清空旧 `_boostLiquidSprite`，关闭 Boost sprite swap，并把 outline/core 基础 alpha 调整为正式 Canary 可读状态。`BoostTrailPrefabCreator` 的 Boost energy overlay 改用 Canary body/outline sprite，避免继续引用旧 `Boost_16` / `Movement_3`。

- **目的**：让玩家继续操控原正式 `Ship.prefab` 路径，但看到的是 Canary 船体与 QFZ Ares Boost 拖尾，而不是 preview-only ship 或旧 GG 船体。保持 `Ship.prefab` 由 `ShipPrefabRebuilder` 管理、`BoostTrailRoot.prefab` 由 `BoostTrailPrefabCreator` 管理，不引入 scene patch、第二可操控 prefab 或 Debug 主链。

- **技术**：通过 Unity Editor 内执行 `BoostTrailPrefabCreator.CreateOrRebuildBoostTrailRootPrefab(false)` 与 `ShipPrefabRebuilder.ForceRebuildSpriteLayersSilently()` 落地 prefab，避免手写 Unity YAML / fileID。验证包括：`dotnet build Project-Ark.slnx` 成功（仅既有 obsolete / 第三方 warning）；Unity Console 清空后无 error/warning；`manage_prefabs get_hierarchy` 确认正式 `Ship.prefab` 只剩 Canary sprite 层 + nested `BoostTrailRoot/AresBoostTrail`；文本 grep 确认正式 prefab 不再包含 `Ship_Sprite_Liquid`、`Ship_Sprite_HL`、`Ship_Sprite_Solid`，`DefaultShipJuiceSettings.asset` 不再引用旧 `Boost_16` GUID。

---

## Canary Boost 接入 QFZ Ares 拖尾 — 2026-05-28 22:14

- **新建/修改文件**
  - `Assets/Scripts/Ship/VFX/BoostTrailView.cs`
  - `Assets/Scripts/Ship/Editor/BoostTrailPrefabCreator.cs`
  - `Assets/Scripts/Ship/Tests/BoostTrailViewTests.cs`
  - `Assets/_Prefabs/VFX/BoostTrailRoot.prefab`
  - `Assets/_Prefabs/Ship/CanaryShipVisualPreview.prefab`
  - `Docs/0_Plan/ongoing/2026-05-25-canary-ship-complete-art-vfx-plan.md`
  - `Docs/2_TechnicalDesign/Ship/ShipVFX_AssetRegistry.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **删除文件**
  - `Assets/_Art/Ship/Canary/Animations/Canary_BoostPreview.anim`
  - `Assets/_Art/Ship/Canary/Animations/Canary_BoostPreview.controller`
  - `Assets/_Art/Ship/Canary/Textures/Emission/spr_ship_canary_engine_boost_preview.png`
  - `Assets/_Art/Ship/Canary/Textures/Emission/spr_ship_canary_engine_boost_jet_preview.png`
  - Canary Boost preview 相关截图资产

- **内容**：删除 Canary 临时 Boost preview 资产并清理 `CanaryShipVisualPreview.prefab` 中的 preview 节点 / Animator；将 QFZ `VFX_Ares_Projectile_Only.prefab` 通过唯一 prefab authority `BoostTrailPrefabCreator` 接入 `BoostTrailRoot.prefab`，生成 `AresBoostTrail` 子树并序列化到 `BoostTrailView._aresSustainParticles`。`BoostTrailView` 增加 Ares sustained 粒子数组入口，在 Boost start / end / reset 中统一启停与清理，避免对象池回收后粒子残留。

- **目的**：把 Boost 最终拖尾从临时 preview 方案收口到正式 Ship/VFX 链路：直接复用成熟的 QFZ Ares 拖尾语言，同时不新增第二条 BoostTrail authority、不做 scene-only patch、不让 Debug 工具接管正式运行链。

- **技术**：采用 TDD 最小增量，新增 `BoostTrailViewTests.ResetState_StopsAndClearsAresSustainParticles` 锁定防御性复位；`BoostTrailPrefabCreator` 在重建 prefab 时加载 `Assets/QFX/ProjectilesFX/VFX_Prefabs/Projectiles/VFX_Ares_Projectile_Only.prefab`，实例化为 `AresBoostTrail` 并仅调整 offset、rotation、scale、lifetime、speed、size、emission 与 sorting。已通过 `validate_script`、`dotnet build Project-Ark.slnx`、Editor 直接断言 `ResetState()` 后 Ares 粒子 `isPlaying=False, particleCount=0`，并确认 `BoostTrailRoot.prefab` 中 `_aresSustainParticles` 数组包含 7 个粒子系统引用。

---

## Canary Boost 复用 VFX_Ares_Projectile 计划修订 — 2026-05-28 22:08

- **新建/修改文件**
  - `Docs/0_Plan/ongoing/2026-05-25-canary-ship-complete-art-vfx-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容**：修订 Canary Ship 完整 Art/VFX plan 的 Batch 5 Boost 目标：将 `VFX_Ares_Projectile` 从“可能的视觉参考”明确升级为 Boost 最终拖尾的直接复用来源。Task 15 文件清单改为优先定位 / 复用 QFZ `VFX_Ares_Projectile`，仅在需要补充船体读感时才创建额外 Core / EnergyBar / Engine 资产；Task 16 改为先确认现有 `BoostTrailRoot` authority，再检查并小改适配 `VFX_Ares_Projectile`。Completion Gate 与 Risk Register 同步补充“不得引入第二条 BoostTrail authority”和“避免 projectile 被误读为武器发射”的约束。

- **目的**：让后续 Boost 方向与真实目标一致：直接拿成熟的 QFZ projectile 拖尾作为基础，减少重复造轮子，同时把它收口到现有 Ship/VFX authority 链内，避免产生新的并行 VFX 主链或 scene-only 临时方案。

- **技术**：文档层面更新 plan 约束，明确源资产、适配范围、registry 更新要求和风险治理项；不修改 Unity 资产、Prefab、Scene、Runtime 脚本或正式 `BoostTrailRoot.prefab`。

---

## Canary Engine / Rear Boost Preview 竖向喷流调优 — 2026-05-28 21:46

- **新建/修改文件**
  - `Assets/_Art/Ship/Canary/Textures/Emission/spr_ship_canary_engine_boost_jet_preview.png`
  - `Assets/_Art/Ship/Canary/Textures/Emission/spr_ship_canary_engine_boost_jet_preview.png.meta`
  - `Assets/_Art/Ship/Canary/Animations/Canary_BoostPreview.anim`
  - `Assets/_Prefabs/Ship/CanaryShipVisualPreview.prefab`
  - `Assets/Screenshots/canary_engine_boost_preview_v2_t050_rendered.png`
  - `Assets/Screenshots/canary_engine_boost_preview_v2_t050_rendered.png.meta`
  - `Assets/Screenshots/canary_engine_boost_preview_v3_t050_rendered.png`
  - `Assets/Screenshots/canary_engine_boost_preview_v3_t050_rendered.png.meta`
  - `Assets/Screenshots/canary_engine_boost_preview_v3_isolated_t050_rendered.png`
  - `Assets/Screenshots/canary_engine_boost_preview_v3_isolated_t050_rendered.png.meta`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：根据验收反馈将 Engine / Rear Boost preview 的主读感从横向 glow 调整为沿船体纵轴向后的竖向推进。新增 `spr_ship_canary_engine_boost_jet_preview.png` 竖向青色喷流 Sprite；在 `CanaryShipVisualPreview.prefab` 中保留原 `EngineBoostPreview` 作为低透明度横向底光，同时新增中央弱尾流 `EngineBoostJetPreview` 与左右双引擎主喷流 `EngineBoostJet_L` / `EngineBoostJet_R`。`Canary_BoostPreview.anim` 现在驱动左右双喷流进行 1 秒循环的 scale / alpha 呼吸，横向底光仅做辅助氛围。
- **目的**：让玩家从俯视角更直观地读到“船尾引擎正在持续向后输出推进力”，避免上一版横向 glow 被误读为地面照明或装饰光带，同时继续保持与 Dash 爆发、残影、长拖尾的语义区分。
- **技术**：使用 Unity Editor `Texture2D` 程序化生成透明 PNG 并导入为 Sprite；通过 `PrefabUtility.LoadPrefabContents` 修改独立 preview prefab；通过 `AnimationUtility.SetEditorCurve` 更新 `EngineBoostPreview`、`EngineBoostJetPreview`、`EngineBoostJet_L`、`EngineBoostJet_R` 的 Transform scale 与 SpriteRenderer color / alpha 曲线。验证阶段使用 `AnimationClip.SampleAnimation` 采样 0 / 0.5 / 1 秒，并用隔离坐标相机 `Camera.Render` 输出无场景干扰截图 `canary_engine_boost_preview_v3_isolated_t050_rendered.png`。未修改正式 `Ship.prefab`、`BoostTrailRoot.prefab`、scene-only BoostTrail 绑定或 Ship/VFX authority 主链。

---

## Canary Engine / Rear Boost Preview MCP 验收 — 2026-05-28 21:37

- **新建/修改文件**
  - `Assets/Screenshots/canary_engine_boost_preview_t075.png`
  - `Assets/Screenshots/canary_engine_boost_preview_t075.png.meta`
  - `Assets/Screenshots/canary_engine_boost_preview_t075_rendered.png`
  - `Assets/Screenshots/canary_engine_boost_preview_t075_rendered.png.meta`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：在 Unity MCP 恢复后，对 `Canary_BoostPreview.anim` 做 Editor 侧采样与截图验收。采样确认 clip 长度为 1 秒且 `loopTime = true`；`EngineBoostPreview` 使用 `spr_ship_canary_engine_boost_preview`，`sortingOrder = -1`；关键帧变化为 scale `0.82/0.38 → 0.98/0.43 → 0.82/0.38`，alpha `0.48 → 0.66 → 0.48`，Core 同步保持 `1.08 → 1.14 → 1.08` 的呼吸。生成 Scene View 截图后发现选择 / Gizmo 会造成白灰视觉干扰，因此额外生成无 Gizmo 的相机渲染截图 `canary_engine_boost_preview_t075_rendered.png` 作为真实视觉参考。
- **目的**：用 MCP 闭环验证最新 Engine / Rear Boost preview 的实际表现变化，确认船尾青色 glow 是持续推进读感而非 Dash 爆发，并区分 Scene View 工具可视化干扰与真实游戏渲染。
- **技术**：使用 Unity Editor `AnimationClip.SampleAnimation` 临时实例化 `CanaryShipVisualPreview.prefab` 采样 0 / 0.25 / 0.5 / 0.75 / 1 秒；使用 `Camera.Render` + `RenderTexture` 输出无 Gizmo PNG；验证后清理临时 GameObject 与 Camera。未修改正式 `Ship.prefab`、`BoostTrailRoot.prefab`、scene-only BoostTrail 绑定或 Ship/VFX authority 主链。

---

## Canary Engine / Rear Boost Preview 创建 — 2026-05-28 14:54

- **新建/修改文件**
  - `Assets/_Art/Ship/Canary/Textures/Emission/spr_ship_canary_engine_boost_preview.png`
  - `Assets/_Art/Ship/Canary/Textures/Emission/spr_ship_canary_engine_boost_preview.png.meta`
  - `Assets/_Art/Ship/Canary/Animations/Canary_BoostPreview.anim`
  - `Assets/_Prefabs/Ship/CanaryShipVisualPreview.prefab`
  - `Docs/0_Plan/ongoing/2026-05-25-canary-ship-complete-art-vfx-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：跳过本轮 EnergyBar Boost，改为完成 preview-only 的 Engine / Rear Boost pass。新增 `spr_ship_canary_engine_boost_preview.png` 小型青色后方推进 glow，并在 `CanaryShipVisualPreview.prefab` 下新增 `EngineBoostPreview` 子节点；扩展 `Canary_BoostPreview.anim`，让 `EngineBoostPreview` 与 Core 同步进行 1 秒循环的轻量 scale / alpha 呼吸。计划文档同步将 EnergyBar boost emission 与 production engine albedo 标记为 Deferred，并记录本次 preview-only 完成状态。
- **目的**：在 EnergyBar 相关源资产先前已跳过的前提下，继续验证 Boost 的“持续推进”读感：Core 表达能量源启动，Rear glow 表达推进输出，同时避免误用 DashTrail / DashParticles 造成 Dash 语义混淆。
- **技术**：使用 Unity Editor `Texture2D` 生成透明 PNG 并通过 `TextureImporter` 导入为 Sprite；使用 `PrefabUtility.LoadPrefabContents` 安全写入独立 `CanaryShipVisualPreview.prefab`；使用 `AnimationUtility.SetEditorCurve` 添加 `EngineBoostPreview` 的 Transform scale 与 SpriteRenderer color / alpha 曲线。验证阶段确认 prefab 层级新增节点、动画 YAML 包含 `EngineBoostPreview` 曲线，Unity Console 无本次编译错误；正式 `Ship.prefab`、`BoostTrailRoot.prefab` 与 scene-only BoostTrail 绑定未修改。

---

## Canary Core Boost Preview 创建 — 2026-05-28 12:52

- **新建/修改文件**
  - `Assets/_Art/Ship/Canary/Animations/Canary_BoostPreview.anim`
  - `Assets/_Art/Ship/Canary/Animations/Canary_BoostPreview.controller`
  - `Assets/_Prefabs/Ship/CanaryShipVisualPreview.prefab`
  - `Docs/0_Plan/ongoing/2026-05-25-canary-ship-complete-art-vfx-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：按 Batch 5A 的“稳定推进型”方向创建 preview-only Core Boost 动画与独立 Animator Controller，并将 `CanaryShipVisualPreview.prefab` 根节点挂载 `Animator` 指向 `Canary_BoostPreview.controller`。动画为 1 秒循环，仅驱动 `Core` 子节点：scale 在 `1.08 → 1.14 → 1.08` 之间稳定呼吸，SpriteRenderer color / alpha 做轻量供能脉冲，避免闪白、爆炸感或 Dash 式瞬间残影。更新计划文档，将 Task 15 拆分为已完成的 preview 动画 Step 1A 与后续真实 emission texture Step 1B。
- **目的**：在不触碰正式 `Ship.prefab`、`BoostTrailRoot.prefab`、scene-only BoostTrail 绑定或 Ship/VFX authority 主链的前提下，先验证 Canary Boost 的核心视觉方向是否成立，为后续 EnergyBar / Engine Boost / 正式链路决策提供最小可验收切片。
- **技术**：使用 Unity Editor `AssetDatabase` / `AnimationUtility.SetEditorCurve` 创建 AnimationClip 曲线，使用 `AnimatorController.CreateAnimatorControllerAtPath` 创建 preview controller，并通过 `PrefabUtility.LoadPrefabContents` 安全写入独立 preview prefab。验证阶段采样 0 秒、0.5 秒、1 秒曲线闭环，并确认正式 `Ship.prefab` 与 `BoostTrailRoot.prefab` 未变更。

---

## Canary Idle Preview Clip 创建 — 2026-05-28 11:22

- **新建/修改文件**
  - `Assets/_Art/Ship/Canary/Animations/Canary_Idle.anim`
  - `Docs/0_Plan/ongoing/2026-05-25-canary-ship-complete-art-vfx-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：创建 `Assets/_Art/Ship/Canary/Animations/` 目录和 `Canary_Idle.anim` 预览动画；Idle clip 设置为循环动画，仅对 `Core` 做轻微缩放 / alpha 呼吸，对 `WeaponMount` 做极弱缩放脉冲，保持 Body / Shape / Outline 稳定。通过临时 `CanaryIdleClipValidation` 实例采样验证 0 秒、0.5 秒、1 秒曲线闭环，并截图确认预览显示正常。将 Task 14 Step 1 / Step 2 标记为完成，LeanLeft / LeanRight / Dash clip 继续因源帧延后保持 deferred。
- **目的**：在 Lean / Dash 资产暂缓的情况下，先让 Canary Normal-state 视觉栈拥有一个可验证的 Idle 预览动画，继续推进 Unity 侧预览管线而不阻塞在生成失败的动作帧上。
- **技术**：使用 Unity Editor `AssetDatabase` 创建 AnimationClip 资产，通过 `AnimationUtility.SetEditorCurve` 写入 Transform scale 与 SpriteRenderer alpha 曲线；验证阶段仅实例化独立 `CanaryShipVisualPreview.prefab` 临时对象，未修改正式 `Ship.prefab` 或 Ship/VFX authority 主链。

---

## Canary Normal Sprite 透明通道复验通过 — 2026-05-27 16:08

- **修改文件**
  - `Assets/_Art/Ship/Canary/Sprites/Body/spr_ship_canary_body_normal_albedo.png`
  - `Assets/_Art/Ship/Canary/Sprites/Outline/spr_ship_canary_outline_normal_outline.png`
  - `Assets/_Art/Ship/Canary/Sprites/Core/spr_ship_canary_core_normal_albedo.png`
  - `Assets/_Art/Ship/Canary/Sprites/WeaponMount/spr_ship_canary_weapon_mount_normal_albedo.png`
  - `Docs/0_Plan/ongoing/2026-05-25-canary-ship-complete-art-vfx-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：刷新 Unity 资源并重新检查 5 张 Canary Normal-state Sprite 的导入与 alpha 分布，确认 Body / Outline / Core / WeaponMount 的白底全不透明问题已修复；重新生成临时三背景可读性检查对象并截图验证，黑、白、深蓝背景下均不再出现白色方块，船体和轮廓可读。将 Task 13 Step 4 从 blocked 更新为完成，并更新 Batch 4 completion gate 中已完成的导入、Prefab 与未触碰正式链路状态。
- **目的**：关闭 `CanaryShipVisualPreview.prefab` 的可读性阻塞，让当前 Normal-state 飞船视觉栈可以继续进入后续动画 / Boost / Fire 等预览任务，同时保持正式 `Ship.prefab` 主链不受影响。
- **技术**：通过 Unity MCP 执行 AssetDatabase 强制刷新、TextureImporter 设置复核、Texture2D alpha 像素统计，以及临时 `CanaryPreviewReadabilityCheck` + `CanaryPreviewReadabilityCamera` 三背景截图验证；验证完成后清理临时场景对象。

---

## Canary Import Settings 与 Preview Prefab 创建 — 2026-05-27 15:45

- **新建/修改文件**
  - `Assets/_Prefabs/Ship/CanaryShipVisualPreview.prefab`
  - `Assets/_Art/Ship/Canary/Materials/mat_ship_canary_body_default.mat`
  - `Assets/_Art/Ship/Canary/Materials/mat_ship_canary_shape.mat`
  - `Assets/_Art/Ship/Canary/Materials/mat_ship_canary_outline.mat`
  - `Assets/_Art/Ship/Canary/Materials/mat_ship_canary_dash.mat`
  - `Assets/_Art/Ship/Canary/Materials/mat_ship_canary_trail.mat`
  - `Assets/_Art/Ship/Canary/Sprites/Body/spr_ship_canary_body_normal_albedo.png`
  - `Assets/_Art/Ship/Canary/Sprites/Shape/spr_ship_canary_shape_normal_mask.png`
  - `Assets/_Art/Ship/Canary/Sprites/Outline/spr_ship_canary_outline_normal_outline.png`
  - `Assets/_Art/Ship/Canary/Sprites/Core/spr_ship_canary_core_normal_albedo.png`
  - `Assets/_Art/Ship/Canary/Sprites/WeaponMount/spr_ship_canary_weapon_mount_normal_albedo.png`
  - `Docs/0_Plan/ongoing/2026-05-25-canary-ship-complete-art-vfx-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：通过 Unity AssetDatabase 将当前 5 张 Normal-state Canary Sprite 统一为 `Sprite (2D and UI)`、`Single`、中心 pivot、`PPU = 320`、关闭 mipmap；创建 `CanaryShipVisualPreview.prefab`，包含 Body / Shape / Outline / Core / EnergyBar_L / EnergyBar_R / WeaponMount / DashTrailPreview / DashParticlesPreview 层级和独立 SpriteRenderer 排序；创建计划要求的 5 个 Canary preview material。可读性验证阶段发现 Body / Outline / Core / WeaponMount 源 PNG 目前为整张全不透明，导致背景被白块覆盖，因此将 Task 13 Step 4 标记为 blocked。
- **目的**：把已完成的 Normal-state 美术层安全接入 Unity 预览链，先在独立 Preview Prefab 中验证导入参数、层级、材质和排序，不触碰正式 `Ship.prefab` / ShipVFX authority 主链。
- **技术**：使用 Unity MCP 执行 Editor 侧 AssetDatabase / TextureImporter / PrefabUtility 操作；用正式 `Ship.prefab` 现役 SpriteRenderer 读取 `PPU = 320` 作为导入基准；通过临时 `CanaryPreviewReadabilityCheck` 场景对象和截图进行三背景可读性检查，并用像素 alpha 统计定位透明通道阻塞原因。

---

## Canary Dash 延后与 Unity Import 推进 — 2026-05-27 15:36

- **修改文件**
  - `Docs/0_Plan/ongoing/2026-05-25-canary-ship-complete-art-vfx-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：将 Batch 3 `Dash / Dodge Frames And Preview VFX`、Task 10 `Dash frames`、Task 11 `Dash materials and preview VFX` 标记为 deferred；在执行顺序摘要中同步标注 Dash 相关任务延后；将 `Current Next Action` 从 Task 10 推进为 Task 12 `Import Settings Pass`，并将 Task 13 `Create Canary Preview Prefab` 标为后续动作。
- **目的**：避免 Dash 图生成继续阻塞当前 Normal-state Canary stack 的 Unity 导入与预览验证，先确认 Body / Shape / Outline / Core / WeaponMount 可以在 Unity 中正确显示与对齐。
- **技术**：仅更新计划文档状态与月度实现日志；未创建、移动或修改 PNG 资产，未修改 Unity 运行链路、Prefab、Scene、Material、Shader 或 `.meta` 文件。

---

## Canary Lean 延后与 Dash 下一步推进 — 2026-05-27 00:35

- **修改文件**
  - `Docs/0_Plan/ongoing/2026-05-25-canary-ship-complete-art-vfx-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：将 Batch 2 `Lean Frames`、Task 7 `Lean Left`、Task 8 `Lean Right`、Task 9 `Lean validation` 标记为 deferred；在执行顺序摘要中同步标注 Lean 相关任务延后；将 `Current Next Action` 从 Lean 路径推进为 Task 10 `Create Dash Frames`，并将 Task 11 `Create Dash Materials And Preview VFX` 标为后续动作。
- **目的**：避免 Lean 图生成失败继续阻塞可玩切片推进，先进入 Dash / Dodge 帧与 Unity preview 验证，保持美术管线可继续迭代。
- **技术**：仅更新计划文档状态与月度实现日志；未创建、移动或修改 PNG 资产，未修改 Unity 运行链路、Prefab、Scene、Material、Shader 或 `.meta` 文件。

---

## Canary WeaponMount 验收通过与计划修正 — 2026-05-26 23:59

- **修改文件**
  - `Docs/0_Plan/ongoing/2026-05-25-canary-ship-complete-art-vfx-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：复核 `Assets/_Art/Ship/Canary/Sprites/WeaponMount/spr_ship_canary_weapon_mount_normal_albedo.png`，确认当前文件名正确，格式为 `512 x 512` RGBA PNG 且包含 Alpha 通道；接受其作为 Normal weapon mount / muzzle marker 层，并将 Batch 1 / Task 6 Step 3 标记为完成。
- **目的**：修正上一轮基于透明像素预览造成的误判，让后续 Fire VFX 炮口对齐和 stack 验证可以基于已通过的 WeaponMount 层继续推进。
- **技术**：使用目录检查、系统 PNG 文件信息与 Alpha 支持检查完成复核；仅更新计划文档状态，未修改 Unity 运行链路、Prefab、Scene、Material、Shader 或 `.meta` 文件。

---

## Canary Core 完成与 EnergyBars 延后标记 — 2026-05-26 16:40

- **修改文件**
  - `Docs/0_Plan/ongoing/2026-05-25-canary-ship-complete-art-vfx-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：将 Batch 1 / Task 6 的 `Create core asset` 标记为完成；将 `Create left/right energy bars` 标记为本轮暂时跳过并延后到 WeaponMount 之后；同步把 `Create weapon mount marker or sprite` 标注为当前下一步。
- **目的**：接受当前 `spr_ship_canary_core_normal_albedo.png` 作为 Core 正常层成果，同时避免 EnergyBars 阻塞 Batch 1 的功能接口推进，先进入炮口 / 武器挂载点参考层制作。
- **技术**：仅更新计划文档状态与实现日志；未移动或创建 PNG 资产，未修改 Unity 运行链路、Prefab、Scene、Material、Shader 或 `.meta` 文件。

---

## Canary Outline Sprite 通过判定与 Task 5 关闭 — 2026-05-26 14:55

- **修改文件**
  - `Docs/0_Plan/ongoing/2026-05-25-canary-ship-complete-art-vfx-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：验收 `Assets/_Art/Ship/Canary/Sprites/Outline/spr_ship_canary_outline_normal_outline.png`，确认其为 512 × 512 RGBA PNG 且带 Alpha 通道，视觉内容为干净的青色飞船外轮廓，无上一版的文字、水印或杂线残留。将 Batch 1 / Task 5 `Create Outline Sprite` 的三个步骤标记为完成，并把 `Current Next Action` 推进为 Task 6 `Create Core / EnergyBars / WeaponMount`。
- **目的**：关闭 Normal playable set 的 Outline 资产生产任务，让后续 Core、EnergyBars、WeaponMount 与 Lean 派生资产可以基于稳定的 Body / Shape / Outline 三层继续制作。
- **技术**：使用图像预览与 `sips` / `file` 命令完成格式验证；仅更新文档状态，未修改 Unity 运行链路、Prefab、Scene、Material、Shader 或 `.meta` 文件。

---

## Canary Shape Mask 移动与 Task 4 关闭 — 2026-05-26 14:07

- **移动文件**
  - `Assets/_Art/Ship/Canary/Sprites/Body/spr_ship_canary_shape_normal_mask.png` → `Assets/_Art/Ship/Canary/Sprites/Shape/spr_ship_canary_shape_normal_mask.png`
- **修改文件**
  - `Docs/0_Plan/ongoing/2026-05-25-canary-ship-complete-art-vfx-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：将 `spr_ship_canary_shape_normal_mask.png` 从 `Sprites/Body/` 移动到计划指定的 `Sprites/Shape/` 目录，并将 Batch 1 / Task 4 `Create Shape Mask` 的三个步骤标记为完成。同步把 `Current Next Action` 推进为 Task 5 `Create Outline Sprite`，并记录 Task 6 为后续 Lean 左右帧制作。
- **目的**：关闭 Shape Mask 生产任务，让当前金丝雀号 body 与 mask 成为后续 Outline、Lean、Dash 派生资产的稳定输入。
- **技术**：文件路径整理与文档状态更新；未创建 `.meta` 文件，交由 Unity AssetDatabase 自动生成/维护；未修改 Unity 运行链路、Prefab、Scene、Material 或 Shader。

---

## Canary Ship Body Sprite 通过判定与计划推进 — 2026-05-26 11:46

- **修改文件**
  - `Docs/0_Plan/ongoing/2026-05-25-canary-ship-complete-art-vfx-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容**：将 Batch 1 / Task 3 `spr_ship_canary_body_normal_albedo.png` 的四个步骤标记为通过，并记录该 body sprite 作为当前 Shape、Outline、Lean、Dash 派生工作的来源。同步把 `Current Next Action` 从过期的 Task 1 / Task 2 推进为 Task 4 `Create Shape Mask`。
- **目的**：接受当前主身体 Sprite 作为本轮生产基准，避免继续卡在背景/布局返修上，让 Batch 1 能进入 Shape Mask 与 Outline 制作。
- **技术**：文档状态更新；未修改 Unity 运行链路、Prefab、Scene、Material 或 `.meta` 文件。

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

---

## FPSShooterTut 移除缺失 iTween 依赖 — 2026-05-23 23:47

- **修改文件**
  - 
  - 

- **内容**：移除第三方示例脚本  中唯一的  调用，保留投射物生成、刚体速度赋值与 muzzle 生成逻辑。

- **目的**：解决  编译错误；该脚本来自导入的特效包示例，项目内没有导入 iTween，且全项目仅此一处引用，因此不引入额外第三方依赖，采用最小删除依赖的方式恢复编译。

- **技术**：通过全项目搜索确认  仅在该文件出现；删除示例性 punch offset 调用，不改 Project Ark 正式战斗/Ship/VFX 主链。验证：  正在确定要还原的项目…
  所有项目均是最新的，无法还原。
  ProjectArk.Core -> /Users/dada/Documents/GitHub/Project-Ark/Temp/bin/Debug/ProjectArk.Core.dll
  ProjectArk.Core.Audio -> /Users/dada/Documents/GitHub/Project-Ark/Temp/bin/Debug/ProjectArk.Core.Audio.dll
  ProjectArk.Ship -> /Users/dada/Documents/GitHub/Project-Ark/Temp/bin/Debug/ProjectArk.Ship.dll
  ProjectArk.Core.Tests -> /Users/dada/Documents/GitHub/Project-Ark/Temp/bin/Debug/ProjectArk.Core.Tests.dll
  ProjectArk.Heat -> /Users/dada/Documents/GitHub/Project-Ark/Temp/bin/Debug/ProjectArk.Heat.dll
  ProjectArk.Ship.Tests -> /Users/dada/Documents/GitHub/Project-Ark/Temp/bin/Debug/ProjectArk.Ship.Tests.dll
  ProjectArk.Ship.Editor -> /Users/dada/Documents/GitHub/Project-Ark/Temp/bin/Debug/ProjectArk.Ship.Editor.dll
  AmplifyShaderEditor -> /Users/dada/Documents/GitHub/Project-Ark/Temp/bin/Debug/AmplifyShaderEditor.dll
  ProjectArk.Combat -> /Users/dada/Documents/GitHub/Project-Ark/Temp/bin/Debug/ProjectArk.Combat.dll
  ProjectArk.Enemy -> /Users/dada/Documents/GitHub/Project-Ark/Temp/bin/Debug/ProjectArk.Enemy.dll
  ProjectArk.Level -> /Users/dada/Documents/GitHub/Project-Ark/Temp/bin/Debug/ProjectArk.Level.dll
  ProjectArk.Combat.Tests -> /Users/dada/Documents/GitHub/Project-Ark/Temp/bin/Debug/ProjectArk.Combat.Tests.dll
  ProjectArk.UI -> /Users/dada/Documents/GitHub/Project-Ark/Temp/bin/Debug/ProjectArk.UI.dll
  ProjectArk.SpaceLife -> /Users/dada/Documents/GitHub/Project-Ark/Temp/bin/Debug/ProjectArk.SpaceLife.dll
  ProjectArk.Level.Editor -> /Users/dada/Documents/GitHub/Project-Ark/Temp/bin/Debug/ProjectArk.Level.Editor.dll
  ProjectArk.SpaceLife.Tests -> /Users/dada/Documents/GitHub/Project-Ark/Temp/bin/Debug/ProjectArk.SpaceLife.Tests.dll
  ProjectArk.SpaceLife.Editor -> /Users/dada/Documents/GitHub/Project-Ark/Temp/bin/Debug/ProjectArk.SpaceLife.Editor.dll
  ProjectArk.Combat.Editor -> /Users/dada/Documents/GitHub/Project-Ark/Temp/bin/Debug/ProjectArk.Combat.Editor.dll
  Assembly-CSharp -> /Users/dada/Documents/GitHub/Project-Ark/Temp/bin/Debug/Assembly-CSharp.dll

已成功生成。
    0 个警告
    0 个错误

已用时间 00:00:01.39 通过，所有项目生成成功。

---

## FPSShooterTut 移除缺失 iTween 依赖 — 2026-05-23 23:47

- **修改文件**
  - `Assets/GabrielAguiarProductions/Scripts/FPSShooterTut.cs`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容**：移除第三方示例脚本 `FPSShooterTut` 中唯一的 `iTween.PunchPosition(...)` 调用，保留投射物生成、刚体速度赋值与 muzzle 生成逻辑。

- **目的**：解决 `CS0103: The name iTween does not exist in the current context` 编译错误；该脚本来自导入的特效包示例，项目内没有导入 iTween，且全项目仅此一处引用，因此不引入额外第三方依赖，采用最小删除依赖的方式恢复编译。

- **技术**：通过全项目搜索确认 `iTween` 仅在该文件出现；删除示例性 punch offset 调用，不改 Project Ark 正式战斗/Ship/VFX 主链。验证：`bash -lc "dotnet build Project-Ark.slnx"` 通过，所有项目生成成功。

---

## QFX Anomaly Projectile Only 原型生成工具 — 2026-05-25 15:24

- **新建/修改文件**
  - `Assets/Scripts/Combat/Editor/QfxProjectilePrototypeCreator.cs`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容**：新增 `QfxProjectilePrototypeCreator` Editor 菜单工具，菜单路径为 `ProjectArk/Combat/VFX/QFX Prototype/Create Anomaly Projectile Only Prototype`。该工具会从 QFX 的 `VFX_Cyber_Projectile_Only.prefab` 和 `Cyber` 主题 13 张材质复制出隔离的 Anomaly projectile-only 原型资产，并将 prefab 内的 `ParticleSystemRenderer` 材质槽重绑定到复制后的 Anomaly 材质。

- **目的**：用最小侵入方式试验 QFX authored projectile VFX 复刻路线，先验证“复制主题材质 + 改色 + 重绑定 prefab”的工作流，不接入正式 StarCore、Projectile Pool 或 Ship/VFX 主链。

- **技术**：使用 `AssetDatabase.CopyAsset` 保留 Unity 自动 GUID/meta 生成流程；使用 `PrefabUtility.LoadPrefabContents` / `SaveAsPrefabAsset` 离线重写 prefab 内 Renderer 材质引用；通过 `_Color`、`_EmissionColor`、`_TintColor`、`_ColorAddSubDiff` 写入 Anomaly 紫红/绿异常色板。验证：`dotnet build Project-Ark.slnx` 通过，新增脚本无编译错误；项目仍有既有 warning。



---

## Ship Art/VFX Workflow Minishoot 主轴重写 — 2026-05-25 22:40

- **新建/修改文件**
  - `Docs/3_WorkflowsAndRules/Ship/Ship_ArtVFX_Workflow.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容**：将金丝雀号飞船美术 workflow 的参考优先级调整为 `Minishoot` 主轴、`Galactic Glitch` 附录。重写参考项目优先级、Normal 主体资产、Dodge/Dash、Unity 接入、Material/Shader、推荐 Batch 顺序与 GG appendix，明确第一轮需要准备 `Body / Shape / Outline / Core / EnergyBars / WeaponMount / Lean / Dash / Trail / Particle` 这套 Minishoot 风格 playable set，而不是 GG 式多状态分层表。

- **目的**：让飞船美术生产回到“少资产、高可读、快验证”的垂直切片路径，优先复刻 Minishoot 的简洁飞船实现方式：完整主体、独立 outline/shape、左右 Lean 帧、Dash 短帧、TrailRenderer/ParticleSystem/Tween 组合。GG 保留为后续复杂状态图、材质和禁误用参考，避免拖慢本轮 Normal/Dash 可玩闭环。

- **技术**：基于 `Minishoot/DevXUnity/Sprite`、`Minishoot/DevXUnity/Texture2D`、`Minishoot/ExportedProject/Assets/AnimationClip` 与 `PlayerView.cs` 的实证核对，按 Markdown 标题边界替换 workflow 章节；保留现役 `Ship/VFX` 接入约束，要求新增正式节点前同步 `CanonicalSpec` / `AssetRegistry`，并维持 Runtime 不写 shared Material、Debug 不接管正式链路的规则。


---

## Canary Ship Complete Art/VFX Ongoing Plan — 2026-05-25 22:51

- **新建/修改文件**
  - 
  - 

- **内容**：根据  新建金丝雀号完整飞船制作 ongoing plan。计划把 Minishoot 主轴 workflow 拆成 29 个可执行任务，覆盖资产文件夹准备、Minishoot reference board、Normal playable set、Lean、Dash、Unity 导入、Preview Prefab、Boost、Fire、Hit、Weaving、Overheat、Material/Shader、Ship/VFX 正式接入、最终验证与对象池复位检查。

- **目的**：将飞船美术 workflow 从“制作规则文档”落成“可以逐项推进的 ongoing 执行计划”，明确每一步要制作哪些素材、素材路径、验收标准、MVP 边界与最终完成条件，目标是最终完成整个金丝雀号飞船美术与 VFX 制作闭环。

- **技术**：采用 Minishoot 风格  playable set 作为主生产模型，将  保留为 appendix / optional reference；计划中显式约束正式接入必须遵守 、 与 ，避免 scene override、debug 接管、shared Material runtime mutation 和 GG 式多状态表过早膨胀。


---

## Canary Ship Asset Folder Preparation — 2026-05-25 23:09

- **新建/修改文件**
  - `Assets/_Art/Ship/Canary/Source/Concepts/`
  - `Assets/_Art/Ship/Canary/Source/Layered/`
  - `Assets/_Art/Ship/Canary/Source/AI_Raw/`
  - `Assets/_Art/Ship/Canary/Sprites/Body/`
  - `Assets/_Art/Ship/Canary/Sprites/Shape/`
  - `Assets/_Art/Ship/Canary/Sprites/Outline/`
  - `Assets/_Art/Ship/Canary/Sprites/Core/`
  - `Assets/_Art/Ship/Canary/Sprites/EnergyBars/`
  - `Assets/_Art/Ship/Canary/Sprites/WeaponMount/`
  - `Assets/_Art/Ship/Canary/Sprites/Lean/`
  - `Assets/_Art/Ship/Canary/Sprites/Dash/`
  - `Assets/_Art/Ship/Canary/Textures/Masks/`
  - `Assets/_Art/Ship/Canary/Textures/Emission/`
  - `Assets/_Art/Ship/Canary/Textures/Noise/`
  - `Assets/_Art/Ship/Canary/Materials/`
  - `Assets/_Art/Ship/Canary/Shaders/`
  - `Docs/0_Plan/ongoing/2026-05-25-canary-ship-complete-art-vfx-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容**：执行金丝雀号完整飞船制作计划的 Task 1，创建 `Assets/_Art/Ship/Canary/` 下的 Source、Sprites、Textures、Materials、Shaders 资产目录，并更新 ongoing plan 中 Task 1 的状态。目录创建已完成；Unity Editor / AssetDatabase 导入验证保留为待确认步骤。

- **目的**：为 Minishoot-style 飞船制作主轴建立稳定资产管线，后续可按 `Body / Shape / Outline / Core / EnergyBars / WeaponMount / Lean / Dash` 的顺序放入正式素材，避免素材散落或提前污染现役 `Ship/VFX` 主链。

- **技术**：使用文件系统创建目录，不手写 `.meta`，遵守 Unity GUID 由 Editor 自动生成的规则；通过 `find` 检查当前目录下未出现手写 `.meta` 文件；计划文档只标记已完成的磁盘目录创建，并明确 Unity import verification 仍需后续刷新验证。


---

## Canary Ship Minishoot Reference Board — 2026-05-25 23:19

- **新建/修改文件**
  - `Assets/_Art/Ship/Canary/Source/Concepts/canary_minishoot_reference_board.png`
  - `Docs/0_Plan/ongoing/2026-05-25-canary-ship-complete-art-vfx-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容**：执行 Batch 0 / Task 2，定位 Minishoot 飞船参考资产，生成金丝雀号参考板 PNG，并在 ongoing plan 中补充 `__PlayerFull`、`SupershotPlayerOutline`、Lean、Dash、EnergyBars、Weapons、SpiritDashTrail / SpiritDashParticles 到 Project Ark 目标素材的映射表。

- **目的**：在正式制作金丝雀号素材前锁定 Minishoot 主参考，明确 Batch 1 优先制作 `Body / Shape / Outline / Lean / Dash`，并把 GG-style full state sheet、死亡残骸、复杂 shader parity、最终 Bloom 调参排除出 Batch 1。

- **技术**：使用文件系统与脚本引用检索确认 `Sprite`、`Texture2D`、`Material`、`AnimationClip`、`PlayerView.cs` 中的参考来源；用轻量 PNG 生成脚本制作可读参考板；不创建或手写 `.meta`。


## Ship Hit MVP — 2026-05-29 01:37

- **新建/修改文件：**
  - `Assets/Scripts/Ship/VFX/ShipHitVisuals.cs`
  - `Assets/Scripts/Ship/Data/ShipJuiceSettingsSO.cs`
  - `Assets/Scripts/Ship/Tests/ShipHitVisualsTests.cs`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容：**
  - 为 `ShipHitVisuals` 增加正伤害专用短促 scale punch，使受击瞬间在 Canary `Body / Outline / Core` 层上更可读。
  - `damage <= 0` 的 `OnDamageTaken` 事件只刷新低血量脉冲判断，不再触发白闪或 i-frame 闪烁，避免治疗/读档 HP 刷新误表现为受击。
  - 在 `ShipJuiceSettingsSO` 中新增 `HitImpactScalePeak`、`HitImpactAttackDuration`、`HitImpactReleaseDuration` 数据驱动参数。
  - 新增 `ShipHitVisualsTests` 覆盖非正伤害事件不应改变渲染颜色的边界。
- **目的：**
  - 在 Fire MVP 验收后补齐基础战斗反馈链路中的 Hit MVP，提升受击手感与可读性，同时避免 UI/HUD 复用血量事件造成误闪。
- **技术：**
  - 继续保持 `ShipView -> ShipHitVisuals` 单一运行时入口，不新增 prefab 节点、旧 sprite 或 legacy fallback。
  - 使用 PrimeTween `Sequence` + `Tween.Scale` 做短促视觉 punch，并在 `ResetState()` 中防御性停止 tween、恢复 scale/color。
  - 通过 `dotnet build Project-Ark.slnx`、Unity `validate_script` 和一次性 Editor smoke test 验证编译与关键行为。


## Ship Hit Spark MVP — 2026-05-29 09:46

- **新建/修改文件：**
  - `Assets/Scripts/Ship/VFX/ShipHitVisuals.cs`
  - `Assets/Scripts/Ship/Editor/ShipPrefabRebuilder.cs`
  - `Assets/Scripts/Ship/Tests/ShipHitVisualsTests.cs`
  - `Assets/_Prefabs/Ship/Ship.prefab`
  - `Docs/0_Plan/ongoing/2026-05-25-canary-ship-complete-art-vfx-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容：**
  - 为 `ShipHitVisuals` 增加 prefab 内预分配的 `_hitSparkParticles` 引用，正伤害时播放短促局部 spark，`ResetState()` 停止并清空粒子。
  - `ShipPrefabRebuilder` 新增受控子节点 `Ship_HitSpark` 的创建、粒子参数配置和 `ShipHitVisuals._hitSparkParticles` 接线，避免运行时生成对象或手工 scene override。
  - 为 `ShipHitVisualsTests` 增加 Hit Spark 播放和复位行为测试。
  - 更新 Canary Ship Art/VFX 计划中 Task 18 的 hit spark 与 pooled reset 状态。
- **目的：**
  - 在已验证的 Fire / Hit scale punch 基础上补齐受击局部火花读感，使 Hit 更短促、可读且不混淆 Overheat。
- **技术：**
  - 继续保持 `ShipView -> ShipHitVisuals` 单一运行时入口，不引入旧 sprite、不新增 legacy fallback。
  - 使用 prefab-owned preallocated `ParticleSystem` 承担局部 spark，战斗中只 `Play/Stop/Clear`，符合防御性复位与无 Instantiate/Destroy 约束。
  - 通过 `dotnet build Project-Ark.slnx`、Unity `validate_script`、Editor smoke test、`Ship.prefab` 层级与序列化引用检查完成验证。


## Ship Hit Spark 紫色材质修复 — 2026-05-29 09:58

- **新建/修改文件：**
  - `Assets/Scripts/Ship/Editor/ShipPrefabRebuilder.cs`
  - `Assets/Scripts/Ship/Editor/ShipPrefabHitSparkTests.cs`
  - `Assets/_Prefabs/Ship/Ship.prefab`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容：**
  - 确认 `Ship_HitSpark` 的 `ParticleSystemRenderer.m_Materials` 原本为空引用，导致 Unity/URP 粒子渲染落入紫色错误/默认材质表现。
  - `ShipPrefabRebuilder` 现在会为 `Ship_HitSpark` 显式复用现有 `mat_ship_canary_trail.mat`，并在材质缺失或 Shader 不支持时输出错误，不再静默生成紫色粒子。
  - 直接修复 `Ship.prefab` 中 `Ship_HitSpark` 的 renderer 材质引用，从 `{fileID: 0}` 改为已有 Canary Trail 材质引用。
  - 新增 `ShipPrefabHitSparkTests`，回归检查 `Ship.prefab` 中 Hit Spark renderer 必须拥有显式、受支持、非错误 Shader 材质。
- **目的：**
  - 修复玩家受击 spark 已出现但呈紫色的问题，保证 Hit Spark MVP 的颜色读感为金黄/白亮火花，而不是 Unity magenta 错误材质。
- **技术：**
  - 保持 `ShipPrefabRebuilder` 作为 `Ship.prefab` 结构和核心引用唯一权威。
  - 复用已有 Ship/Canary 受控材质资产，避免新增半成品材质和手造 `.meta`。
  - 已通过 `dotnet build Project-Ark.slnx` 验证 C# 编译；已通过 prefab YAML 检查确认 Hit Spark renderer 材质引用不再为空。Unity MCP 当前持续超时，需在 Editor 恢复后运行新增 EditMode 测试或人工 Play Mode 视觉验收。


## Canary Ship Art/VFX 下一步指针修正 — 2026-05-29 16:16

- **新建/修改文件：**
  - `Docs/0_Plan/ongoing/2026-05-25-canary-ship-complete-art-vfx-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容：**
  - 将 Task 18 Hit Mask overlay 继续按 MVP 完成入账，同时记录当前白色受击 mask 视觉偏突兀，后续需要做更柔和、方向性更强的 Hit feedback 返工。
  - 确认计划正文中 `Task 12: Import Settings Pass` 与 `Task 13: Create Canary Preview Prefab` 已完成，修正末尾 `Current Next Action`，不再指向已完成的 Task 12。
  - 将下一步推进目标更新为 `Task 14: Create Preview Animation Clips — Lean/Dash return pass`。
- **目的：**
  - 避免后续迭代被过期的 Current Next Action 带回已完成任务，同时保留 Hit Mask 视觉返工风险，保证当前可玩闭环继续推进。
- **技术：**
  - 仅更新计划与日志文档，不改 Runtime / Prefab / Scene 链路。
  - 继续遵守 `Ship.prefab` authority：下一步 Lean/Dash preview 不应破坏正式 `Ship.prefab` 主链。


## Canary Lean 源帧补齐状态同步 — 2026-05-29 22:32

- **新建/修改文件：**
  - `Docs/0_Plan/ongoing/2026-05-25-canary-ship-complete-art-vfx-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容：**
  - 确认 `Assets/_Art/Ship/Canary/Sprites/Lean/` 下已存在 `PlayerLeanLeft1.png`、`PlayerLeanLeft2.png`、`PlayerLeanLeft3.png`、`PlayerLeanRight1.png`、`PlayerLeanRight2.png`、`PlayerLeanRight3.png`。
  - 使用本地尺寸检查确认六张 Lean PNG 均为 `512 × 512`，与当前 Normal body canvas 一致。
  - 将计划中的 Batch 2 从 Deferred 更新为 Reopened / Source Frames Supplied，并将 Task 7 / Task 8 的左右 Lean 源帧创建步骤标记为已补齐。
  - 将 Task 14 的 LeanLeft / LeanRight clip 状态改为 Ready for hookup；Dash 保持 Deferred / MVP contract needed。
  - 将 Current Next Action 更新为 `Task 14A: Lean import/name pass + Lean preview clips`。
- **目的：**
  - 让计划反映用户已补齐 Lean 资产的当前事实，避免后续继续把 Lean 当作缺失阻塞项。
  - 把剩余工作收敛到官方命名/导入设置/Unity 预览动画验证，而不是重新生产 Lean 图。
- **技术：**
  - 仅更新计划与日志文档，不改 Runtime / Prefab / Scene 链路。
  - 通过 `sips -g pixelWidth -g pixelHeight` 对比 Normal body 与 Lean 源帧尺寸，确认基础 canvas 对齐。
  - 保留 workflow 命名风险：当前 supplied source 仍是 `PlayerLeanLeft/Right*.png`，下一步需要转为 `spr_ship_canary_lean_left/right_01-03.png` 或在计划中明确采用该命名。


## Canary Lean 官方命名迁移 — 2026-05-29 22:38

- **新建/修改文件：**
  - `Assets/_Art/Ship/Canary/Sprites/Lean/spr_ship_canary_lean_left_01.png`
  - `Assets/_Art/Ship/Canary/Sprites/Lean/spr_ship_canary_lean_left_02.png`
  - `Assets/_Art/Ship/Canary/Sprites/Lean/spr_ship_canary_lean_left_03.png`
  - `Assets/_Art/Ship/Canary/Sprites/Lean/spr_ship_canary_lean_right_01.png`
  - `Assets/_Art/Ship/Canary/Sprites/Lean/spr_ship_canary_lean_right_02.png`
  - `Assets/_Art/Ship/Canary/Sprites/Lean/spr_ship_canary_lean_right_03.png`
  - `Docs/0_Plan/ongoing/2026-05-25-canary-ship-complete-art-vfx-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容：**
  - 将 Lean 左右各三张源帧从 `PlayerLeanLeft/Right*.png` 命名迁移为正式 workflow 命名 `spr_ship_canary_lean_left/right_01-03.png`。
  - 迁移后再次确认六张 Lean PNG 均存在且保持 `512 × 512`。
  - 更新计划中的 Batch 2、Task 14 和 Current Next Action：官方命名已完成，下一步改为 Unity import validation + Lean preview clips。
  - 保留参考映射表中的 `PlayerLeanLeft/Right1-3` 原参考名，仅作为 reference asset mapping，不再作为当前执行路径。
- **目的：**
  - 消除 Lean 资产命名与项目 workflow 不一致的问题，使后续 Unity 导入设置、AnimationClip 和预览 Prefab 可以引用稳定的正式路径。
  - 避免后续任务继续指向已不存在的 `PlayerLean*` 源文件名。
- **技术：**
  - 使用文件移动完成 PNG 重命名；未发现现有 `.png.meta`，因此没有手造 Unity `.meta` 或 GUID。
  - 使用 `find` 和 `sips -g pixelWidth -g pixelHeight` 验证目标文件存在与尺寸一致。
  - 仅更新计划/日志和 art asset 文件名，不改 Runtime / Prefab / Scene 主链。


## Canary Lean 导入验证与预览动画 — 2026-05-30 10:57

- **新建/修改文件：**
  - `Assets/_Art/Ship/Canary/Sprites/Lean/spr_ship_canary_lean_left_01.png.meta`
  - `Assets/_Art/Ship/Canary/Sprites/Lean/spr_ship_canary_lean_left_02.png.meta`
  - `Assets/_Art/Ship/Canary/Sprites/Lean/spr_ship_canary_lean_left_03.png.meta`
  - `Assets/_Art/Ship/Canary/Sprites/Lean/spr_ship_canary_lean_right_01.png.meta`
  - `Assets/_Art/Ship/Canary/Sprites/Lean/spr_ship_canary_lean_right_02.png.meta`
  - `Assets/_Art/Ship/Canary/Sprites/Lean/spr_ship_canary_lean_right_03.png.meta`
  - `Assets/_Art/Ship/Canary/Animations/Canary_LeanLeft.anim`
  - `Assets/_Art/Ship/Canary/Animations/Canary_LeanLeft.anim.meta`
  - `Assets/_Art/Ship/Canary/Animations/Canary_LeanRight.anim`
  - `Assets/_Art/Ship/Canary/Animations/Canary_LeanRight.anim.meta`
  - `Assets/_Art/Ship/Canary/Animations/CanaryShipVisualPreview.controller`
  - `Docs/0_Plan/ongoing/2026-05-25-canary-ship-complete-art-vfx-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容：**
  - 通过 Unity 资产导入器将六张 Lean PNG 对齐 Normal stack 的 Sprite 设置：`Sprite`、`Single`、`PPU 320`、`Center pivot`、`MipMap off`、`Point filter`、`Uncompressed`。
  - 新建 `Canary_LeanLeft.anim` 与 `Canary_LeanRight.anim`，只绑定 `Body/SpriteRenderer.m_Sprite`，分别播放 `01 → 02 → 03 → 02`，12 FPS，约 `0.333s`，loop 用于预览检查。
  - 将 `CanaryShipVisualPreview.controller` 接入 `Canary_LeanLeft` 和 `Canary_LeanRight` 状态，方便 Animator 中直接选择预览。
  - 更新计划：Task 14 LeanLeft / LeanRight clip 标记完成，Current Next Action 推进为 `Task 14B: Lean Play Mode visual validation`。
- **目的：**
  - 完成 Lean 资产从图片补齐到 Unity 可预览动画资产的闭环，让下一步可以专注于 Play Mode 视觉验收：无跳位、无缩放突变、无 alpha 残留、128px 可读。
  - 保持 Lean 预览链独立于正式 `Ship.prefab` / ShipVFX authority 主链，避免预览资产污染正式运行链。
- **技术：**
  - 使用 Unity Editor `TextureImporter` 设置导入参数，`.meta` 由 Unity 自动生成/维护，没有手造 GUID。
  - 使用 `AnimationUtility.SetObjectReferenceCurve` 创建 Sprite swap 曲线，并用 `AnimatorController` 状态承载预览入口。
  - 通过 Unity Editor 查询验证六张 Lean Sprite 的 importer 参数、Sprite rect、clip 绑定路径和 keyframe 内容；Console 仅显示既有项目 warning，未发现本次导入/动画创建引入的新错误。
  - 未修改正式 `Assets/_Prefabs/Ship/Ship.prefab`、`Assets/_Prefabs/VFX/BoostTrailRoot.prefab` 或 scene-only Boost/VFX 接线。


## Canary Lean 预览验收与下一步推进 — 2026-05-30 12:31

- **新建/修改文件：**
  - `Docs/0_Plan/ongoing/2026-05-25-canary-ship-complete-art-vfx-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容：**
  - 完成 `Task 14B` 的 Lean 预览验收记录：LeanLeft / LeanRight clip 审计确认只绑定 `Body/SpriteRenderer.m_Sprite`，没有 transform / color 曲线。
  - 记录自动采样结论：Lean 动画资产本身不会引入位置漂移、缩放跳变或 alpha 残留；`AnimationClip.SampleAnimation` 对 ObjectReference Sprite 曲线不作为视觉换帧证据，因此以 `AnimationUtility` 曲线审计作为 Sprite swap 的正式验证依据。
  - 使用 128px 缩略图对 Normal、LeanLeft 中段、LeanRight 中段进行读图检查，确认 Lean 轮廓、机头方向和左右翼关系保持可读，且仍读作同一艘 Canary。
  - 更新计划中的 Batch 4 Completion Gate：Idle + Lean 已完成当前验收，Dash 继续保持 deferred。
  - 将 Current Next Action 推进为 `Task 15A: Boost source audit + adaptation target confirmation`。
- **目的：**
  - 把 Lean 从“资产已接入”推进到“当前可验收”，避免继续阻塞 Batch 5 Boost 工作。
  - 保留 Dash 与 Hit Mask 的后续返工/定义空间，不让它们阻塞 Boost 源资产审计。
- **技术：**
  - 使用 Unity Editor `AnimationUtility.GetObjectReferenceCurveBindings` / `GetCurveBindings` 审计 AnimationClip 曲线，确认 Lean clip 只有 Sprite ObjectReference 曲线，无 transform / color 副作用曲线。
  - 使用临时 128px 缩略图进行视觉读图检查；验证产物不进入正式 `Assets/` 资产链。
  - 未修改 Runtime、Prefab、Scene 或 Ship/VFX authority 主链。

## 2026-05-30 14:52 — Canary Lean/Dash Highlight MVP 修复

- **修改文件：**
  - `Assets/Scripts/Ship/VFX/ShipView.cs`
  - `Assets/Scripts/Ship/VFX/ShipVisualJuice.cs`
  - `Assets/Scripts/Ship/VFX/ShipDashVisuals.cs`
- **内容：**
  - `ShipView` 将 `_hlRenderer` 注入 `ShipVisualJuice`，保持 `ShipView` 只做 Coordinator。
  - `ShipVisualJuice` 在 lean body sprite 非 normal 时将 normal highlight/outline alpha 压到 0，回到 normal body sprite 时恢复基线 alpha。
  - `ShipDashVisuals` 在 dash body sprite burst 与 i-frame flicker 期间隐藏 highlight/outline，Dash 结束或 Reset 时恢复。
- **目的：**
  - 修复 Canary 在 lean / 转弯 / dash 姿态下仍叠加 normal highlight，导致姿态错位和视觉脏乱的问题。
  - 先交付 MVP：隐藏错误 normal highlight，不新增 per-state highlight sprite 数据链。
- **技术：**
  - 沿用现役 `Ship/VFX` Worker owner：lean 归 `ShipVisualJuice`，dash 归 `ShipDashVisuals`，不改 Prefab 结构、不新增 fallback / legacy path。
  - 使用 alpha restore 方式保留现有 renderer、材质与排序，不引入第二套高光链路。
- **验证：**
  - `dotnet build Project-Ark.slnx` 通过；仅存在项目既有 warning。
  - `ShipView.cs` / `ShipDashVisuals.cs` Unity `validate_script` 无错误。
  - `ShipVisualJuice.cs` Unity `validate_script` 无错误，仅返回一个泛化 GC warning。
  - Unity Console error 过滤读取为 0 条。


## Canary Hit Mask Softness / Directionality Polish — 2026-05-30 15:10

- **新建/修改文件：**
  - `Assets/_Art/Ship/Canary/Textures/Masks/spr_ship_canary_shape_hit_mask.png`
  - `Docs/0_Plan/ongoing/2026-05-25-canary-ship-complete-art-vfx-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容：**
  - 同路径重写 `spr_ship_canary_shape_hit_mask.png`，保留原 GUID / import 链路，不新增 `.meta` 或第二套 Hit Mask 资产。
  - 将原先偏完整的白色受击覆盖改为更低 alpha 的边缘、核心与斜向裂纹反馈；Unity 生成统计为约 25.3% 非透明覆盖、最大 alpha 184、非零平均 alpha 约 50.4。
  - 更新 Canary Ship Art/VFX 计划：Batch 6 状态改为 Hit Mask overlay + softness polish 已完成，下一步移动到 Ship/VFX authority audit 队列。

- **目的：**
  - 修复当前受击 mask 视觉偏突兀、容易整船刷白的问题，让 Hit feedback 更短、更柔和、更有方向感，并避免破坏飞船身份。

- **技术：**
  - 使用现有 `spr_ship_canary_shape_normal_mask.png` 作为轮廓源，在 Unity Editor 内通过 `Texture2D` 生成低 alpha 边缘 / 核心 / 裂纹分布并重写 PNG。
  - 继续沿用现役 `ShipHitVisuals._hitMaskRenderer` runtime owner 与 `ShipPrefabRebuilder` prefab 接线，不新增 runtime fallback、scene override 或 debug 接管路径。


## Ship VFX BoostTrail Scene Binding Authority Audit — 2026-05-30 15:33

- **新建/修改文件：**
  - `Assets/Scenes/SampleScene.unity`
  - `Assets/Scripts/Ship/Editor/ShipBoostTrailSceneBinder.cs`
  - `Docs/0_Plan/ongoing/2026-05-25-canary-ship-complete-art-vfx-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容：**
  - 定位 `BoostTrailView._boostBloomVolume` 运行时报空的根因：当前 `SampleScene.unity` live 实例 `Ship/ShipVisual/BoostTrailRoot` 的 scene-only `_boostBloomVolume` 引用漂移为空。
  - 通过唯一权威入口 `ShipBoostTrailSceneBinder.SetupBoostTrailSceneReferences()` 重新绑定 `BoostTrailBloomVolume`，并保存 `SampleScene.unity`。
  - 将 `ShipBoostTrailSceneBinder` 的 `Volume` 类型解析从 assembly-qualified `Type.GetType` 收口为 `TypeCache` FullName lookup，清理 authority audit 中的脆弱字符串解析 warning，同时保持离线 `dotnet build` 兼容。
  - 更新 Canary Ship Art/VFX plan 末尾 Current Next Action，记录 BoostTrail scene-only Bloom authority path 已审计并修复。

- **目的：**
  - 让 Boost bloom burst 的 scene-only 引用回到明确的 authority 链路，避免运行时 Boost 时因 `_boostBloomVolume` 为空而报错或失去 bloom 反馈。
  - 不新增 runtime fallback，不让 debug 或 prefab builder 越权接管 scene-only 绑定。

- **技术：**
  - 使用 `ShipVfxValidator.RunAudit()` 复现并确认唯一 error 为 `Scene BoostTrailView: _boostBloomVolume 为空`。
  - 使用既有 `ShipBoostTrailSceneBinder` 作为 Apply 入口完成绑定；runtime `BoostTrailView` 继续只消费序列化引用并在缺失时显式报错。
  - 使用 `TypeCache.GetTypesDerivedFrom<Component>()` 按 `UnityEngine.Rendering.Volume` FullName 解析 Volume 类型，保持 Editor binder 的 scene-only 职责不变。


## Scene Console Authority Cleanup — 2026-05-30 16:27

- **新建/修改文件：**
  - `Assets/Scenes/SampleScene.unity`
  - `Assets/Scripts/SpaceLife/Editor/SpaceLifeSetupWindow.cs`
  - `Docs/0_Plan/ongoing/2026-05-25-canary-ship-complete-art-vfx-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容：**
  - 定位并清理 `SampleScene.unity` 中 3 个 Missing Script 残留：`SpaceLifeCanvas/DialogueUI`、`SpaceLifeCanvas/GiftUI`、`SpaceLifeCanvas/NPCInteractionUI`。这些对象是 Presenter 迁移后的空壳，无子节点且无入站引用。
  - 删除旧的空 `SpaceLifeCanvas/MinimapUI` shell，并重建完整 `MinimapUI` 场景层级：`MinimapPanel`、`CurrentRoomIcon`、`CurrentRoomText`、`RoomButtonsContainer`。
  - 补齐 `MinimapUI` 的 `_minimapPanel`、`_currentRoomText`、`_currentRoomIcon`、`_roomButtonsContainer`、`_roomButtonPrefab` 引用。
  - 将 `SpaceLifeSetupWindow` 的共享 OptionButton prefab 路径从旧 `Assets/_Prefabs/SpaceLife/UI/OptionButton_Prefab.prefab` 同步到现役 `Assets/_Prefabs/UI/SpaceLife/OptionButton.prefab`。
  - 更新 Canary Ship Art/VFX plan 末尾 Current Next Action，记录 Missing Script 与 MinimapUI assignment 已完成。

- **目的：**
  - 消除场景配置阶段的 Missing Script 噪声，避免 Console 中的旧 UI owner 残留掩盖真正的 Ship/VFX 问题。
  - 让 `MinimapUI` 不再因 `_minimapPanel` 或 `_roomButtonPrefab` 为空而 silent no-op。
  - 避免旧 setup wizard 后续再次生成空引用。

- **技术：**
  - 使用 Unity Editor live scene scan 验证 missing component、入站引用和序列化字段状态。
  - 通过 Unity Editor API 删除无引用旧空壳并保存场景，避免手写 `.unity` fileID。
  - 沿用 `CanvasGroup` 控制 uGUI 显隐的规则，保持 UI GameObject active，不使用 `SetActive(false)` 作为面板显隐方案。


## ServiceLocator Optional Missing 噪音治理 — 2026-05-31 10:24

- **新建/修改文件：**
  - `Assets/Scripts/SpaceLife/Dialogue/SpaceLifeDialogueCoordinator.cs`
  - `Assets/Scripts/SpaceLife/Interactable.cs`
  - `Assets/Scripts/SpaceLife/SpaceLifeRoom.cs`
  - `Assets/Scripts/SpaceLife/SpaceLifeDoor.cs`
  - `Assets/Scripts/SpaceLife/PlayerController2D.cs`
  - `Assets/Scripts/SpaceLife/SpaceLifeManager.cs`
  - `Assets/Scripts/SpaceLife/SpaceLifeInputHandler.cs`
  - `Assets/Scripts/SpaceLife/MinimapUI.cs`
  - `Assets/Scripts/UI/UIManager.cs`
  - `Assets/Scripts/Combat/StarChart/StarChartController.cs`
  - `Assets/Scripts/Level/Room/RoomManager.cs`
  - `Assets/Scripts/Level/Room/DestroyableObject.cs`
  - `Assets/Scripts/Level/Room/ArenaController.cs`
  - `Assets/Scripts/Level/Room/DoorTransitionController.cs`
  - `Assets/Scripts/Level/Checkpoint/CheckpointManager.cs`
  - `Assets/Scripts/Level/Checkpoint/Checkpoint.cs`
  - `Assets/Scripts/Level/GameFlow/GameFlowManager.cs`
  - `Assets/Scripts/Level/SaveBridge.cs`
  - `Assets/Scripts/Level/Map/MapPanel.cs`
  - `Assets/Scripts/Level/Map/MinimapHUD.cs`
  - `Assets/Scripts/Level/Progression/WorldProgressManager.cs`
  - `Assets/Scripts/Level/Progression/Lock.cs`
  - `Assets/Scripts/Level/Pickup/HealthPickup.cs`
  - `Assets/Scripts/Level/Pickup/HeatPickup.cs`
  - `Assets/Scripts/Level/DynamicWorld/WorldEventTrigger.cs`
  - `Assets/Scripts/Level/DynamicWorld/BiomeTrigger.cs`
  - `Assets/Scripts/Level/DynamicWorld/AmbienceController.cs`
  - `Docs/0_Plan/ongoing/2026-05-25-canary-ship-complete-art-vfx-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容：**
  - 将 optional / lazy / fallback 语义的服务解析从 `ServiceLocator.Get<T>()` 改为 `ServiceLocator.TryGet<T>()`，避免缺失可选服务时先产生统一 missing-registration warning。
  - 覆盖 `PlayerController2D`、`WorldProgressManager`、`SaveBridge`、`MinimapManager`、`ShipHealth`、`HeatSystem`、`AudioManager` 等本批次目标服务。
  - 保留真正关键依赖的显式 `Debug.LogError` / `Debug.LogWarning`，让缺配置问题继续由业务调用点清晰报出，而不是被 `ServiceLocator` 泛化 warning 淹没。
  - 更新 Canary Ship Art/VFX 计划尾部，记录 `scene-warning-batch-3` 的 ServiceLocator optional missing 噪音治理结果。

- **目的：**
  - 清理 Unity Console 中可选服务缺失造成的噪音，避免这些非阻断 warning 遮蔽真正需要修复的场景配置问题。
  - 区分“必须存在的 manager 缺失”和“允许缺失的模块集成点”，让场景验证阶段的 Console 信号更可信。

- **技术：**
  - 使用 `ServiceLocator.TryGet<T>()` 承载 optional / lazy / fallback dependency 查询。
  - 保持 required dependency 的 loud failure 策略：业务层显式错误仍保留，未新增 runtime fallback 或 scene override。
  - 使用 grep 复查目标服务集合，确保不再存在 `ServiceLocator.Get<T>()` 残留。

- **验证：**
  - `dotnet build Project-Ark.slnx` 通过；仅存在项目既有 warning。
  - 目标服务集合 grep 复查：`ServiceLocator.Get<(SaveBridge|ShipHealth|AudioManager|MinimapManager|WorldProgressManager|PlayerController2D|HeatSystem)>` 无结果。
  - Unity Console 复查未出现 `ServiceLocator Get: ... NOT FOUND` 目标噪音；剩余为既有编译 warning 与一次 MCP transport warning。


## Project Code Warning Cleanup Final Pass — 2026-05-31 10:36

- **新建/修改文件：**
  - `Assets/Scripts/UI/Inventory/DragGhostView.cs`
  - `Assets/Scripts/UI/Inventory/ItemOverlayView.cs`
  - `Assets/Scripts/Combat/Editor/HyperWindArenaSceneConfigurator.cs`
  - `Assets/Scripts/Combat/Editor/EchoWaveProceduralPreviewMenu.cs`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容：**
  - 完成 UI tween 字段初始化/使用路径清理，消除 `DragGhostView` 与 `ItemOverlayView` 的未赋值字段 warning。
  - 完成 Combat Editor 工具的 Unity 6 object-search obsolete API 替换，消除项目内 Editor warning。
  - 复核 `GiftUIPresenter` 与 `SpaceLifeDoor` 所在 `ProjectArk.SpaceLife` 程序集当前无剩余项目内 warning。

- **验证：**
  - `dotnet build /Users/dada/Documents/GitHub/Project-Ark/Project-Ark.slnx` 成功。项目内程序集通过；剩余 2 条 warning 均来自第三方资源目录 `Assets/Hovl Studio` 与 `Assets/QFX`，本轮不修改。
  - Unity Console error/warning 复查为 0 条。

- **目的：**
  - 将本轮 scene warning cleanup 后续扩展到项目内编译 warning，确保 Console 与项目代码 warning 信号恢复干净，第三方资源 warning 与项目代码 warning 明确分离。


## Canary Ship Batch 7 Weaving 资产 MVP — 2026-05-31 11:01

- **新建/修改文件**
  - `Assets/_Art/Ship/Canary/Textures/Emission/spr_ship_canary_core_weaving_emission.png`
  - `Assets/_Art/Ship/Canary/Textures/Emission/spr_ship_canary_aura_weaving_emission.png`
  - `Assets/_Art/Ship/Canary/Textures/Masks/tex_ship_canary_weaving_ring_mask.png`
  - `Assets/_Art/Ship/Canary/Textures/Noise/tex_ship_canary_weaving_noise_mask.png`
  - `Docs/0_Plan/ongoing/2026-05-25-canary-ship-complete-art-vfx-plan.md`

- **内容**：完成 Batch 7 / Task 19 的四张 Weaving MVP 视觉资产：核心编织发光、星图仪式光环、环形连接 mask、能量流噪声 mask；并在计划文件中标记 Task 19 已完成，将下一步推进到 Task 20 Weaving preview。

- **目的**：为金丝雀号编织态提供与 Boost 明确区分的 StarChart connection / ritual aura 视觉素材，同时保持本轮为资产层增量，不改正式 `Ship.prefab` authority 链。

- **技术**：使用 Python 标准库生成 512 × 512 RGBA PNG，不手写 `.meta`；通过 Unity AssetDatabase 刷新后统一导入为 Sprite Single、PPU 320、Center pivot、alpha transparency、Clamp、Bilinear、无 mipmap；Console 清理后复查为 0 error / 0 warning。

## Canary Ship Batch 9 Required Materials — 2026-05-31 14:22

- **新建/修改文件**
  - `Assets/_Art/Ship/Canary/Materials/mat_ship_canary_body_default.mat`
  - `Assets/_Art/Ship/Canary/Materials/mat_ship_canary_shape.mat`
  - `Assets/_Art/Ship/Canary/Materials/mat_ship_canary_outline.mat`
  - `Assets/_Art/Ship/Canary/Materials/mat_ship_canary_core_default.mat`
  - `Assets/_Art/Ship/Canary/Materials/mat_ship_canary_dash.mat`
  - `Assets/_Art/Ship/Canary/Materials/mat_ship_canary_trail.mat`
  - `Assets/_Art/Ship/Canary/Materials/mat_vfx_canary_dash_particles.mat`
  - `Assets/_Art/Ship/Canary/Materials/mat_vfx_canary_muzzle_flash.mat`
  - `Docs/0_Plan/ongoing/2026-05-25-canary-ship-complete-art-vfx-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容**：完成 Batch 9 / Task 22 的首批金丝雀号材质集合。保留并配置已有的 body / shape / outline / dash / trail 材质，补齐缺失的 core default / dash particles / muzzle flash 材质，并在计划文件中标记 Task 22 已完成，将下一步推进到 Task 23 Runtime Material Parameter Rules。

- **目的**：为后续 ship/VFX preview 和正式集成提供稳定材质基线；body 保持保守亮度、outline 保持轮廓对比，dash / trail / particle / muzzle flash 维持高可读但不过曝的 cyan / amber 视觉家族。本轮只创建和配置资产，不把材质接入正式 `Ship.prefab` 运行链，也不定义运行时参数 owner。

- **技术**：通过 Unity Editor `AssetDatabase` 创建/配置 `.mat`，统一使用 `Universal Render Pipeline/2D/Sprite-Lit-Default`，缺失时可回退到 `Sprites/Default`；设置 `_Color` / `_RendererColor` 基础 tint、透明度和 renderQueue 3000。Unity 验证确认 8 个材质均存在且 shader/color/queue 正确，Console 最终复查为 0 error / 0 warning。

## Canary Ship Batch 9 Runtime Material Parameter Rules — 2026-05-31 15:08

- **新建/修改文件**
  - `Docs/2_TechnicalDesign/Ship/ShipVFX_AssetRegistry.md`
  - `Docs/0_Plan/ongoing/2026-05-25-canary-ship-complete-art-vfx-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容**：完成 Batch 9 / Task 23 的材质运行时参数规则。`ShipVFX_AssetRegistry.md` 现在补齐 Canary core / dash / trail / dash particles / muzzle flash 材质映射，并新增 `Material Runtime Parameter Rules`，列出 body / shape / outline / dash / trail / muzzle 的必需参数、参数 owner、runtime writer 与 mutation policy。计划文件中 Task 23 三步和 Batch 9 Completion Gate 已全部标记完成，并将下一步推进到 Task 24 Authority Review Before Integration。

- **目的**：在正式集成前先明确材质参数契约，避免后续 Worker 硬编码随机数、直接修改 shared `.mat`、或重新引入 GG-style state sheet 作为第二真相源。MVP 阶段由 registry 承载契约，已有 ship juice / fire / dash / hit 调参继续归 `ShipJuiceSettingsSO`，未来多材质 VFX 调参再迁移到 `Assets/_Data/Ship/` 下的专用 tuning asset。

- **技术**：文档层定义运行时数据隔离规则：Runtime 只能通过 renderer color、材质实例、`MaterialPropertyBlock` 或 component-level values 改变表现；`MaterialTextureLinker` 只维护材质/贴图链接，不成为 runtime 参数 owner；所有 Canary 材质保持 authored baseline，不在 Play Mode 写回资产。

## Canary Ship Batch 10 Live Slice Visual Checks — 2026-05-31 16:19

- **新建/修改文件**
  - `Docs/0_Plan/ongoing/2026-05-25-canary-ship-complete-art-vfx-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容**：完成 Batch 10 / Task 26 Step 3 的 live-slice 视觉状态检查，并在计划中标记 Task 26 Step 3 与 Batch 10 Completion Gate 完成。当前下一步推进到 Task 27 Build Validation View。

- **目的**：在进入最终验证视图前，确认正式 `Ship.prefab` 集成路径中的 Normal / Dash / Boost / Hit 可由 live `Ship` 实例触发和复位，同时保持 Lean / Fire / Weaving / Overheat 的 deferred 边界，不把尚未正式接入的状态误判为完成。

- **技术**：通过 Unity Play Mode 临时代码只读检查 `Ship` 场景实例：确认 Idle Body / Outline / Core 可见，Shape 与 HitMask 默认 disabled，Dash / Boost / Hit Worker 存在且公开入口可 trigger/reset，`BoostTrailRoot` 下存在 7 个 ParticleSystem；退出 Play Mode 后 Console 复查为 0 warning / 0 error。本轮未修改 prefab、scene、材质或运行时代码，也未新增 fallback / legacy path / debug owner。

## Canary Ship Batch 11 Validation View — 2026-05-31 16:25

- **新建/修改文件**
  - `Assets/Scripts/Ship/VFX/ShipVisualValidationView.cs`
  - `Assets/_Prefabs/Ship/CanaryShipVisualValidation.prefab`
  - `Docs/0_Plan/ongoing/2026-05-25-canary-ship-complete-art-vfx-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容**：完成 Batch 11 / Task 27 的验证视图建设。新增 preview-only `ShipVisualValidationView`，提供 Normal / Lean Left / Lean Right / Dash / Boost / Fire / Hit / Weaving / Overheat 九状态切换、Black / White / DeepBlue 背景切换，以及 Bloom Volume on/off 开关；基于 `CanaryShipVisualPreview.prefab` 创建 `CanaryShipVisualValidation.prefab`，并将当前下一步推进到 Task 28 Run Final State Matrix。

- **目的**：给最终状态矩阵提供一个稳定、可快速切换、可截图的 ship visual validation 入口，让 Batch 11 能验证“完整 gameplay asset”的可读性，而不是继续靠分散 prefab / 临时代码观察。

- **技术**：验证控制器只读/切换 preview prefab 内 SpriteRenderer、背景 SpriteRenderer 与本 prefab 内 `ValidationBloomVolume`，不写 `Ship.prefab`、不新增 live scene dependency、不接管 `ShipView` / `ShipPrefabRebuilder` / runtime Worker owner。Unity 侧验证已覆盖九状态调用、三背景颜色和 Bloom enable/disable；`dotnet build Project-Ark.slnx` 通过。

## Canary Ship Batch 11 Final State Matrix — 2026-05-31 16:38

- **新建/修改文件**
  - `Assets/Screenshots/task28_validation_overheat_camera_clean.png`
  - `Docs/0_Plan/ongoing/2026-05-25-canary-ship-complete-art-vfx-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容**：完成 Batch 11 / Task 28 的最终状态矩阵验证。Normal / Lean Left / Lean Right / Dash / Boost / Fire / Hit / Weaving / Overheat 均通过 `ShipVisualValidationView` 控制器级矩阵检查；Black / White / DeepBlue 背景与 Bloom on/off 组合通过；计划当前下一步推进到 Task 29 Pool And Reset Verification。

- **目的**：验证 Canary ship complete art/VFX 作为 gameplay asset 的核心可读性：Normal 方向清晰、Lean 不抖、Dash 与 Boost 区分、Fire 不洗屏、Hit 无白闪残留、Weaving 不挡 gameplay、Overheat 危险明确且可回 Normal。

- **技术**：使用 Unity 临时代码加载 `CanaryShipVisualValidation.prefab` 并逐状态检查 SpriteRenderer sprite、enabled、color 和 reset 后残留；发现 Scene View / 默认 Camera 截图混入 `CanaryShipVisualPreview`、`Enemy_ChargeRusher_REF_Minshoot` 与其他场景内容后，按系统化调试定位为场景中已有参考对象 / Camera 捕获路径干扰，而非 validation prefab 重复层；最终使用 layer 30 隔离的临时 Camera + 临时 Global Light 2D 捕获 `task28_validation_overheat_layer_isolated_lit.png` 作为 DeepBlue + Overheat 可视证据。临时场景对象已清理，正式 `Ship` 已恢复。


## Canary Ship Batch 7 Weaving Preview — 2026-05-31 11:35

- **新建/修改文件**
  - `Assets/_Prefabs/Ship/CanaryShipVisualPreview.prefab`
  - `Assets/Screenshots/canary_weaving_preview_isolated_bloom_off.png`
  - `Assets/Screenshots/canary_weaving_preview_isolated_bloom_on.png`
  - `Docs/0_Plan/ongoing/2026-05-25-canary-ship-complete-art-vfx-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容**：完成 Batch 7 / Task 20 的 Weaving preview。给 `CanaryShipVisualPreview.prefab` 增加 `WeavingAuraPreview` 与 `WeavingCorePreview` 两个可独立启停的 preview-only SpriteRenderer 子节点，并完成 Bloom off / Bloom on 隔离渲染验证。

- **目的**：验证 Weaving 视觉语言是否能和 Boost 区分，并确认 StarChart connection / ritual aura 感在无 Bloom 与有 Bloom 情况下都可读，同时不接管正式 `Ship.prefab` 运行链。

- **技术**：使用 prefab headless edit 写入 preview 子节点；Weaving aura 使用内置 `Sprites-Default` 材质并放在船体后方，core connection 放在船体上方；使用临时相机、临时 layer 与临时 VolumeProfile 渲染隔离截图，渲染后销毁临时对象，不留下 postprocess/material residue；Unity Console 最终复查为 0 error / 0 warning。


## Canary Ship Batch 8 Overheat Assets — 2026-05-31 12:04

- **新建/修改文件**
  - `Assets/_Art/Ship/Canary/Textures/Emission/spr_ship_canary_core_overheat_emission.png`
  - `Assets/_Art/Ship/Canary/Textures/Masks/spr_ship_canary_shape_overheat_mask.png`
  - `Assets/_Art/Ship/Canary/Textures/Noise/tex_ship_canary_overheat_noise_mask.png`
  - `Assets/_Art/VFX/Ship/spr_vfx_canary_overheat_spark_01.png`
  - `Docs/0_Plan/ongoing/2026-05-25-canary-ship-complete-art-vfx-plan.md`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- **内容**：完成 Batch 8 / Task 21 的四张 Overheat MVP 视觉资产：核心过热发光、船体热压 mask、热浪噪声 mask、过热警示 spark；并在计划文件中标记 Task 21 已完成，将下一步推进到 Task 22 Material and Shader pass。

- **目的**：让金丝雀号过热状态能通过船体视觉读出“危险 / 热压上升”，不只依赖 HUD；同时保持本轮为资产层增量，不运行时修改 authored material 或 ScriptableObject。

- **技术**：使用 Unity Editor 内部 `Texture2D` 程序化写入 512 × 512 RGBA PNG，不手写 `.meta`；核心与 spark 导入为 Sprite Single，PPU 320，Center pivot，alpha transparency，Clamp，Bilinear，无 mipmap；noise 导入为 Default Texture，用于未来 shader 热浪采样。Unity 导入验证确认四个资产均为 512 × 512，Console 最终复查为 0 error / 0 warning。


## Ship/VFX Authority Audit Phase A 增量 — 2026-05-31 17:32

- **新建/修改文件：**
  - `Assets/Scripts/Ship/Editor/ShipVfxValidatorTests.cs`
  - `Assets/Scripts/Ship/Editor/ShipVfxValidatorTests.cs.meta`
  - `Assets/Scripts/Ship/Editor/ShipVfxValidator.cs`
  - `Assets/Scenes/SampleScene.unity`
  - `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`
- **内容：** 为 `ShipVfxValidator` 新增 EditMode 回归测试，覆盖当前 authority chain 无错误，以及旧 `BoostActivationHalo` 节点回流时必须被审计为错误；扩展 validator 的 legacy BoostTrail forbidden child 列表；为 `RunAudit` 增加 `logToConsole` 可选参数；修正 `SampleScene.unity` 中 `BoostTrailBloomVolume.weight` 基线为 `0`。
- **目的：** 推进 Ship/VFX Phase A 的 Audit-first 收口，让 authority / legacy residue / scene-only baseline 可回归验证，避免旧 Boost Activation Halo 静默回流到正式 Ares-only Boost 链路。
- **技术：** Unity EditMode NUnit 测试；使用 `PrefabUtility.LoadPrefabContents` 注入临时 legacy 节点验证审计规则；测试 cleanup 通过 `BoostTrailPrefabCreator` + `ShipBoostTrailSceneBinder` 恢复权威状态；`ShipVfxValidator` 保持只读审计，新增参数只控制日志输出。
- **验证：** `dotnet build Project-Ark.slnx` 通过；Unity EditMode `ProjectArk.Ship.Editor.ShipVfxValidatorTests`：2/2 passed。
