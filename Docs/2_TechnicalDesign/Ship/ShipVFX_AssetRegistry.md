# Ship VFX Asset Registry

## 1. 说明

这份注册表记录当前 `Ship` / `BoostTrail` / `VFX` 规范化范围内的关键对象映射。

记录原则：

- `Canonical Name`：规范语义名
- `Physical Name`：当前仓库真实文件名 / 节点名 / 类型名
- `Status`：`Live / Dormant / Reference / Legacy`
- `Owner`：当前权威维护入口
- `Notes`：补充别名、冻结策略、场景绑定要求等

> 这份表在 MVP 阶段优先解决“统一认知”，不是一次性要求所有对象物理改名。

## 2. Runtime Scripts

| Canonical Name | Physical Name | Type | Status | Current Path | Owner | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| `ShipViewController` | `ShipView` | Runtime Script | Live | `Assets/Scripts/Ship/VFX/ShipView.cs` | Runtime | Coordinator：5 层 sprite 基线颜色、事件路由到 Workers（订阅 OnStateChanged + OnDamageTaken + OnSpeedChanged） |
| `ShipBoostVisualsWorker` | `ShipBoostVisuals` | Runtime Script | Live | `Assets/Scripts/Ship/VFX/ShipBoostVisuals.cs` | Runtime | Worker：Canary Shape/Outline/Core alpha/color 支持 + 推进器脉冲 / TrailView 启停；当前正式配置关闭旧 Boost sprite swap |
| `ShipHitVisualsWorker` | `ShipHitVisuals` | Runtime Script | Live | `Assets/Scripts/Ship/VFX/ShipHitVisuals.cs` | Runtime | Worker：受击白闪 + i-frame 闪烁 + 低血量脉冲 |
| `ShipDashVisualsWorker` | `ShipDashVisuals` | Runtime Script | Live | `Assets/Scripts/Ship/VFX/ShipDashVisuals.cs` | Runtime | Worker：冲刺 i-frame 闪烁 + Dodge_Sprite ghost + DashAfterImageSpawner 委托 |
| `ShipFireVisualsWorker` | `ShipFireVisuals` | Runtime Script | Live | `Assets/Scripts/Ship/VFX/ShipFireVisuals.cs` | Runtime | Worker：Fire MVP；由 `ShipView` 路由 `CombatEvents.OnPlayerProjectileFired`，短促点亮 WeaponMount/Core，不自行订阅事件 |
| `BoostTrailController` | `BoostTrailView` | Runtime Script | Live | `Assets/Scripts/Ship/VFX/BoostTrailView.cs` | Runtime | Boost 尾迹主控；正式 Boost sustained read 由 nested `AresBoostTrail` 粒子承担 |
| `ShipVisualJuiceWorker` | `ShipVisualJuice` | Runtime Script | Live | `Assets/Scripts/Ship/VFX/ShipVisualJuice.cs` | Runtime | Worker：tilt / squash / stretch；由 ShipView 注入引用 + 路由 OnSpeedChanged / OnDashStarted |
| `DashAfterImageSpawnerWorker` | `DashAfterImageSpawner` | Runtime Script | Live | `Assets/Scripts/Ship/VFX/DashAfterImageSpawner.cs` | Runtime | 二级 Worker：Dash 连续残影；由 ShipDashVisuals.TriggerSpawn() 驱动，不自行订阅事件 |
| `DashAfterImageRuntime` | `DashAfterImage` | Runtime Script | Live | `Assets/Scripts/Ship/VFX/DashAfterImage.cs` | Runtime | 残影实例组件 |
| `ShipBoostStateController` | `ShipBoost` | Runtime Script | Live | `Assets/Scripts/Ship/Movement/ShipBoost.cs` | Runtime | 提供 Boost 事件，不直接渲染 |
| `ShipStateMachineController` | `ShipStateController` | Runtime Script | Live | `Assets/Scripts/Ship/ShipStateController.cs` | Runtime | 统一状态事件入口 |
| `ShipStateEnum` | `ShipShipState` | Data Type | Live | `Assets/Scripts/Ship/Data/ShipShipState.cs` | Data | 物理名冻结，语义名统一为 Ship State Enum |
| `ShipJuiceSettings` | `ShipJuiceSettingsSO` | ScriptableObject Type | Live | `Assets/Scripts/Ship/Data/ShipJuiceSettingsSO.cs` | Data | 视觉参数数据源；当前正式 Canary 配置中 `_boostLiquidSprite` 为空、`_boostLiquidSortOverride=false`，避免 Boost 状态切回旧 GG sprite |

## 3. Editor Tools

| Canonical Name | Physical Name | Type | Status | Current Path | Owner | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| `ShipPrefabAuthority` | `ShipPrefabRebuilder` | Editor Tool | Live | `Assets/Scripts/Ship/Editor/ShipPrefabRebuilder.cs` | Editor | `Ship.prefab` 唯一权威：根节点物理/脚本组件 ensure + ShipStatsSO/InputActions/DashAfterImage prefab 接线 + Coordinator/Worker/二级Worker 接线 + 多层 sprite + BoostTrailRoot 集成 |
| `BoostTrailPrefabAuthority` | `BoostTrailPrefabCreator` | Editor Tool | Live | `Assets/Scripts/Ship/Editor/BoostTrailPrefabCreator.cs` | Editor | 仅负责 `BoostTrailRoot.prefab` |
| `BoostTrailMaterialLinker` | `MaterialTextureLinker` | Editor Tool | Live | `Assets/Scripts/Ship/Editor/MaterialTextureLinker.cs` | Editor | 只维护现役材质映射 |
| `BoostTrailSceneBinder` | `ShipBoostTrailSceneBinder` | Editor Tool | Live | `Assets/Scripts/Ship/Editor/ShipBoostTrailSceneBinder.cs` | Editor | scene-only 引用绑定 |
| `ShipGlowMaterialCreator` | `ShipGlowMaterialCreator` | Editor Tool | Live | `Assets/Scripts/Ship/Editor/ShipGlowMaterialCreator.cs` | Editor | 共享 glow 材质辅助生成 |
| `ShipVfxAuditTool` | `ShipVfxValidator` | Editor Tool | Live | `Assets/Scripts/Ship/Editor/ShipVfxValidator.cs` | Editor | 只读审计：Prefab 接线 + Scene Bloom + Override 白名单 + 代码残留防回退检测 |
| `BoostTrailDebugInspector` | `BoostTrailDebugManagerEditor` | Editor Tool | Live | `Assets/Scripts/Ship/Editor/BoostTrailDebugManagerEditor.cs` | Editor | Play Mode Inspector 预览面板，不代理到 runtime |
| `ShipReferenceTextureImporter` | `CopyGGTextures.ps1` | Import Tool | Reference | `Tools/CopyGGTextures.ps1` | Reference Import | 参考资源导入；仅复制当前保留的 GG 纹理，不定义现役规范 |

## 4. Prefabs and Scene Bindings

| Canonical Name | Physical Name | Type | Status | Current Path | Owner | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| `ShipPrefab` | `Ship.prefab` | Prefab | Live | `Assets/_Prefabs/Ship/Ship.prefab` | `ShipPrefabRebuilder` | 飞船主体 prefab |
| `BoostTrailRootPrefab` | `BoostTrailRoot.prefab` | Prefab | Live | `Assets/_Prefabs/VFX/BoostTrailRoot.prefab` | `BoostTrailPrefabCreator` | 独立生成后由 `ShipPrefabRebuilder` 集成 |
| `QFZAresProjectileOnlySource` | `VFX_Ares_Projectile_Only.prefab` | Prefab Source | Reference Source | `Assets/QFX/ProjectilesFX/VFX_Prefabs/Projectiles/VFX_Ares_Projectile_Only.prefab` | QFX Source / `BoostTrailPrefabCreator` consumer | Canary Boost sustained trail source; consumed by `BoostTrailPrefabCreator`, not a parallel runtime owner |
| `BoostTrailAresSustainNode` | `AresBoostTrail` | Prefab Node | Live | `Assets/_Prefabs/VFX/BoostTrailRoot.prefab` | `BoostTrailPrefabCreator` / `BoostTrailView` | Adapted QFZ Ares sustained particles; serialized into `BoostTrailView._aresSustainParticles` for Boost start/end/reset |
| `DashAfterImagePrefab` | `DashAfterImage.prefab` | Prefab | Live | `Assets/_Prefabs/Ship/DashAfterImage.prefab` | Runtime / Prefab | Dash 残影 prefab |
| `ShipVisualRoot` | `ShipVisual` | Prefab Node | Live | `Assets/_Prefabs/Ship/Ship.prefab` | `ShipPrefabRebuilder` | `VisualChild` 是 legacy alias |
| `ShipVisualRootLegacyAlias` | `VisualChild` | Prefab Node Alias | Legacy | `Assets/_Prefabs/Ship/Ship.prefab` | Legacy | 仅作兼容查找，不再作为规范名 |
| `ShipCanaryBodyNode` | `Ship_Sprite_Body` | Prefab Node | Live | `Assets/_Prefabs/Ship/Ship.prefab` | `ShipPrefabRebuilder` | 正式主船体层；由 `ShipView._solidRenderer` 消费 |
| `ShipCanaryShapeNode` | `Ship_Sprite_Shape` | Prefab Node | Live | `Assets/_Prefabs/Ship/Ship.prefab` | `ShipPrefabRebuilder` | Canary Shape/mask 预留层；由 `ShipView._liquidRenderer` 消费，当前 renderer disabled 以避免未完成 mask 参与画面 |
| `ShipCanaryOutlineNode` | `Ship_Sprite_Outline` | Prefab Node | Live | `Assets/_Prefabs/Ship/Ship.prefab` | `ShipPrefabRebuilder` | 正式轮廓/readability 层；由 `ShipView._hlRenderer` 消费 |
| `ShipCanaryCoreNode` | `Ship_Sprite_Core` | Prefab Node | Live | `Assets/_Prefabs/Ship/Ship.prefab` | `ShipPrefabRebuilder` | 正式 core / energy focus 层；由 `ShipView._coreRenderer` 消费 |
| `ShipCanaryWeaponMountNode` | `Ship_Sprite_WeaponMount` | Prefab Node | Live | `Assets/_Prefabs/Ship/Ship.prefab` | `ShipPrefabRebuilder` | 正式 weapon mount / muzzle marker 层；当前为静态视觉层 |
| `ShipHitMaskFlashNode` | `Ship_HitMaskFlash` | Prefab Node | Live | `Assets/_Prefabs/Ship/Ship.prefab` | `ShipPrefabRebuilder` | 正式受击 overlay mask；默认 disabled/alpha 0，由 `ShipHitVisuals._hitMaskRenderer` 在正伤害时短促显示 |
| `ShipDashGhostNode` | `Dodge_Sprite` | Prefab Node | Live | `Assets/_Prefabs/Ship/Ship.prefab` | `ShipPrefabRebuilder` | Dash 静态 ghost；暂用 Canary body silhouette，直到 Batch 3 Dash frames 解除 deferred |
| `ShipLegacyLiquidNode` | `Ship_Sprite_Liquid` | Prefab Node | Legacy Removed | `Assets/_Prefabs/Ship/Ship.prefab` | Legacy | 已从正式 prefab force rebuild 清理；禁止重新作为 live 节点引入 |
| `ShipLegacyHighlightNode` | `Ship_Sprite_HL` | Prefab Node | Legacy Removed | `Assets/_Prefabs/Ship/Ship.prefab` | Legacy | 已从正式 prefab force rebuild 清理；由 `Ship_Sprite_Outline` 取代 |
| `ShipLegacySolidNode` | `Ship_Sprite_Solid` | Prefab Node | Legacy Removed | `Assets/_Prefabs/Ship/Ship.prefab` | Legacy | 已从正式 prefab force rebuild 清理；由 `Ship_Sprite_Body` 取代 |
| `LeftFlameTrailNode` | `FlameTrail_B` | Prefab Node | Legacy Removed | `Assets/_Prefabs/VFX/BoostTrailRoot.prefab` | Legacy | 旧 Boost 粒子层；已由 `AresBoostTrail` 取代，禁止重新作为 live 节点引入 |
| `BoostTrailNestedBinding` | `_boostTrailView` | Serialized Reference | Live | `Assets/_Prefabs/Ship/Ship.prefab` | `ShipPrefabRebuilder` | 应指向 nested `BoostTrailRoot` |
| `BoostTrailBloomSceneObject` | `BoostTrailBloomVolume` | Scene Object | Live | `Assets/Scenes/SampleScene.unity` | `ShipBoostTrailSceneBinder` | 仅场景级存在 |
| `BoostTrailBloomProfile` | `BoostBloomVolumeProfile.asset` | Asset | Live | `Assets/Settings/BoostBloomVolumeProfile.asset` | Scene / Settings | 给 scene volume 使用 |

## 5. Materials and Shaders

| Canonical Name | Physical Name | Type | Status | Current Path | Owner | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| `ShipCanaryBodyMaterial` | `mat_ship_canary_body_default.mat` | Material | Live | `Assets/_Art/Ship/Canary/Materials/mat_ship_canary_body_default.mat` | `ShipPrefabRebuilder` | 正式 Canary body/core/weapon mount 基础材质 |
| `ShipCanaryShapeMaterial` | `mat_ship_canary_shape.mat` | Material | Live | `Assets/_Art/Ship/Canary/Materials/mat_ship_canary_shape.mat` | `ShipPrefabRebuilder` | 正式 Canary shape/mask 层材质；当前 mask renderer disabled |
| `ShipCanaryOutlineMaterial` | `mat_ship_canary_outline.mat` | Material | Live | `Assets/_Art/Ship/Canary/Materials/mat_ship_canary_outline.mat` | `ShipPrefabRebuilder` | 正式 Canary outline/readability 材质 |
| `ShipSharedGlowMaterial` | `ShipGlowMaterial.mat` | Material | Legacy Reference | `Assets/_Art/Ship/Glitch/ShipGlowMaterial.mat` | `ShipGlowMaterialCreator` | 旧 GG liquid/glow 辅助材质；不再由正式 `ShipPrefabRebuilder` 写入 Ship.prefab |
| `BoostTrailMainMaterial` | `mat_trail_main.mat` | Material | Legacy Reference | `Assets/_Art/VFX/BoostTrail/Materials/mat_trail_main.mat` | Reference | 旧 Boost 主拖尾材质；Ares-only 正式链路不再使用 |
| `BoostEnergyLayer2Material` | `mat_boost_energy_layer2.mat` | Material | Legacy Reference | `Assets/_Art/VFX/BoostTrail/Materials/mat_boost_energy_layer2.mat` | Reference | 旧 Boost Layer2 材质；Ares-only 正式链路不再使用 |
| `BoostEnergyLayer3Material` | `mat_boost_energy_layer3.mat` | Material | Legacy Reference | `Assets/_Art/VFX/BoostTrail/Materials/mat_boost_energy_layer3.mat` | Reference | 旧 Boost Layer3 材质；Ares-only 正式链路不再使用 |
| `BoostFlameTrailMaterial` | `mat_flame_trail.mat` | Material | Legacy Reference | `Assets/_Art/VFX/BoostTrail/Materials/mat_flame_trail.mat` | Reference | 旧火焰粒子材质；Ares-only 正式链路不再使用 |
| `BoostEmberTrailMaterial` | `mat_ember_trail.mat` | Material | Legacy Reference | `Assets/_Art/VFX/BoostTrail/Materials/mat_ember_trail.mat` | Reference | 旧余烬拖尾材质；Ares-only 正式链路不再使用 |
| `BoostEmberSparksMaterial` | `mat_ember_sparks.mat` | Material | Legacy Reference | `Assets/_Art/VFX/BoostTrail/Materials/mat_ember_sparks.mat` | Reference | 旧余烬火花材质；Ares-only 正式链路不再使用 |
| `BoostTrailMainShader` | `TrailMainEffect.shader` | Shader | Legacy Reference | `Assets/_Art/VFX/BoostTrail/Shaders/TrailMainEffect.shader` | Reference | 旧 Boost shader；保留仅供历史排查 |
| `BoostEnergyLayer2Shader` | `BoostEnergyLayer2.shader` | Shader | Legacy Reference | `Assets/_Art/VFX/BoostTrail/Shaders/BoostEnergyLayer2.shader` | Reference | 旧 Layer2 shader；Ares-only 正式链路不再使用 |
| `BoostEnergyLayer3Shader` | `BoostEnergyLayer3.shader` | Shader | Legacy Reference | `Assets/_Art/VFX/BoostTrail/Shaders/BoostEnergyLayer3.shader` | Reference | 旧 Layer3 shader；Ares-only 正式链路不再使用 |

## 6. Textures and Sprites

| Canonical Name | Physical Name | Type | Status | Current Path | Owner | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| `ShipCanaryBodySprite` | `spr_ship_canary_body_normal_albedo.png` | Sprite | Live | `Assets/_Art/Ship/Canary/Sprites/Body/spr_ship_canary_body_normal_albedo.png` | `ShipPrefabRebuilder` | 正式主船体层；也暂作 Dash ghost silhouette 来源 |
| `ShipCanaryShapeSprite` | `spr_ship_canary_shape_normal_mask.png` | Sprite | Live | `Assets/_Art/Ship/Canary/Sprites/Shape/spr_ship_canary_shape_normal_mask.png` | `ShipPrefabRebuilder` | 正式 Shape/mask 预留层；当前 renderer disabled，后续供 hit/dissolve/overheat 等效果使用 |
| `ShipCanaryHitMaskSprite` | `spr_ship_canary_shape_hit_mask.png` | Sprite | Live | `Assets/_Art/Ship/Canary/Textures/Masks/spr_ship_canary_shape_hit_mask.png` | `ShipPrefabRebuilder` | 正式受击 overlay mask；外轮廓 + 中轴/core 高亮，透明背景，运行时由 `ShipHitVisuals` 短促淡出 |
| `ShipCanaryOutlineSprite` | `spr_ship_canary_outline_normal_outline.png` | Sprite | Live | `Assets/_Art/Ship/Canary/Sprites/Outline/spr_ship_canary_outline_normal_outline.png` | `ShipPrefabRebuilder` | 正式 outline/readability 层 |
| `ShipCanaryCoreSprite` | `spr_ship_canary_core_normal_albedo.png` | Sprite | Live | `Assets/_Art/Ship/Canary/Sprites/Core/spr_ship_canary_core_normal_albedo.png` | `ShipPrefabRebuilder` | 正式 core / energy focus 层 |
| `ShipCanaryWeaponMountSprite` | `spr_ship_canary_weapon_mount_normal_albedo.png` | Sprite | Live | `Assets/_Art/Ship/Canary/Sprites/WeaponMount/spr_ship_canary_weapon_mount_normal_albedo.png` | `ShipPrefabRebuilder` | 正式 weapon mount / muzzle marker 层 |
| `ShipLiquidBoostSprite` | `Boost_16.png` | Sprite | Legacy Reference | `Assets/_Art/Ship/Glitch/Boost_16.png` | Reference | 旧 GG Boost 液态层；`DefaultShipJuiceSettings._boostLiquidSprite` 已清空，正式链路不再使用 |
| `ShipLiquidNormalSprite` | `Movement_3.png` | Sprite | Legacy Reference | `Assets/_Art/Ship/Glitch/Movement_3.png` | Reference | 旧 GG Normal 液态层；不再由正式 `ShipPrefabRebuilder` 写入 Ship.prefab |
| `ShipSolidNormalSprite` | `Movement_10.png` | Sprite | Legacy Reference | `Assets/_Art/Ship/Glitch/Movement_10.png` | Reference | 旧 GG 实色层；不再由正式 `ShipPrefabRebuilder` 写入 Ship.prefab |
| `ShipHighlightNormalSprite` | `Movement_21.png` | Sprite | Legacy Reference | `Assets/_Art/Ship/Glitch/Movement_21.png` | Reference | 旧 GG 高亮层；不再由正式 `ShipPrefabRebuilder` 写入 Ship.prefab |
| `ShipPrimaryBaseSprite` | `Primary.png` | Sprite | Reference | `Assets/_Art/Ship/Glitch/Primary.png` | Reference | 上游参考，不是当前主链 |
| `ShipPrimaryVariant4Sprite` | `Primary_4.png` | Sprite | Reference | `Assets/_Art/Ship/Glitch/Primary_4.png` | Reference | 结构分析参考 |
| `ShipPrimaryVariant6Sprite` | `Primary_6.png` | Sprite | Reference | `Assets/_Art/Ship/Glitch/Primary_6.png` | Reference | 结构分析参考 |
| `ShipDashGhostSource` | `spr_ship_canary_body_normal_albedo.png` | Sprite | Live | `Assets/_Art/Ship/Canary/Sprites/Body/spr_ship_canary_body_normal_albedo.png` | `ShipPrefabRebuilder` | Dash ghost 临时来源；Batch 3 Dash frames 落地后应替换为正式 dash/smear sprite |
| `BoostTrailMainTexture` | `vfx_boost_techno_flame.png` | Texture | Legacy Reference | `Assets/_Art/VFX/BoostTrail/Textures/vfx_boost_techno_flame.png` | Reference | 旧 Boost 主拖尾 / 火焰粒子贴图；Ares-only 正式链路不再使用 |
| `BoostEmberTrailTexture` | `vfx_ember_trail.png` | Texture | Legacy Reference | `Assets/_Art/VFX/BoostTrail/Textures/vfx_ember_trail.png` | Reference | 旧余烬拖尾贴图；Ares-only 正式链路不再使用 |
| `BoostEmberSparksTexture` | `vfx_ember_sparks.png` | Texture | Legacy Reference | `Assets/_Art/VFX/BoostTrail/Textures/vfx_ember_sparks.png` | Reference | 旧余烬火花贴图；Ares-only 正式链路不再使用 |
| `BoostNoiseMainTexture` | `boost_noise_main.png` | Texture | Legacy Reference | `Assets/_Art/VFX/BoostTrail/Textures/boost_noise_main.png` | Reference | 旧 Layer2 Tex0；Ares-only 正式链路不再使用 |
| `BoostNoiseDistortTexture` | `boost_noise_distort.png` | Texture | Legacy Reference | `Assets/_Art/VFX/BoostTrail/Textures/boost_noise_distort.png` | Reference | 旧 Layer2 Tex1；Ares-only 正式链路不再使用 |
| `BoostNoiseLayer3Texture` | `boost_noise_layer3.png` | Texture | Legacy Reference | `Assets/_Art/VFX/BoostTrail/Textures/boost_noise_layer3.png` | Reference | 旧 Layer2 Tex2；Ares-only 正式链路不再使用 |
| `BoostNoiseLayer4Texture` | `boost_noise_layer4.png` | Texture | Legacy Reference | `Assets/_Art/VFX/BoostTrail/Textures/boost_noise_layer4.png` | Reference | 旧 Layer2 Tex3；Ares-only 正式链路不再使用 |
| `BoostEnergyNoiseTexture` | `boost_energy_noise_a.png` | Texture | Legacy Reference | `Assets/_Art/VFX/BoostTrail/Textures/boost_energy_noise_a.png` | Reference | 旧 Layer3 Tex0；Ares-only 正式链路不再使用 |
| `BoostEnergyMainTexture` | `boost_energy_main.png` | Texture | Legacy Reference | `Assets/_Art/VFX/BoostTrail/Textures/boost_energy_main.png` | Reference | 旧 Layer3 Tex1；Ares-only 正式链路不再使用 |
| `BoostActivationHaloPrimary` | `vfx_ring_glow_uneven.png` | Texture | Dormant | `Assets/_Art/VFX/BoostTrail/Textures/vfx_ring_glow_uneven.png` | — | Halo 已取消（2026-03-22），纹理降级为 Dormant，待 Batch 4 清理 |
| `BoostActivationHaloFallback` | `vfx_magnetic_rings.png` | Texture | Dormant | `Assets/_Art/VFX/BoostTrail/Textures/vfx_magnetic_rings.png` | — | Halo 已取消（2026-03-22），纹理降级为 Dormant，待 Batch 4 清理 |

## 7. Documentation and References

| Canonical Name | Physical Name | Type | Status | Current Path | Owner | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| `ImplementRules` | `Implement_rules.md` | Doc | Live | `Implement_rules.md` | Docs | 模块级实现规则、协作约束与踩坑治理入口 |
| `ShipVFXCanonicalSpec` | `ShipVFX_CanonicalSpec.md` | Doc | Live | `Docs/Reference/ShipVFX_CanonicalSpec.md` | Docs | 规范权威入口 |
| `ShipVFXAssetRegistry` | `ShipVFX_AssetRegistry.md` | Doc | Live | `Docs/Reference/ShipVFX_AssetRegistry.md` | Docs | 资产映射总表 |
| `ShipVFXMigrationPlan` | `ShipVFX_MigrationPlan.md` | Doc | Live | `Docs/Reference/ShipVFX_MigrationPlan.md` | Docs | 分批迁移计划 |
| `ShipVFXPerceptionReference` | `Ship_VFX_Player_Perception_Reference.md` | Doc | Live | `Docs/Reference/Ship_VFX_Player_Perception_Reference.md` | Docs | 玩家感知视角参考 |
| `BoostTrailImplementationStatus` | `BoostTrail_Shader_Implementation_Status.md` | Doc | Live | `Docs/Reference/BoostTrail_Shader_Implementation_Status.md` | Docs | 主拖尾实现状态说明 |
| `GalacticGlitchBoostTrailResearch` | `GalacticGlitch_BoostTrail_VFX_Plan.md` | Doc | Reference | `Docs/Reference/GalacticGlitch_BoostTrail_VFX_Plan.md` | Reference | 上游逆向研究，不是现役规范 |

## 8. GGReplica Experimental Assets

| Canonical Name | Physical Name | Type | Status | Current Path | Owner | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| `GGReplicaShipPrefab` | `Ship_GGReplica.prefab` | Prefab | Reference | `Assets/_Prefabs/Ship/Ship_GGReplica.prefab` | `GGReplicaPrefabBuilder` | Experimental A/B replica; `GGReplicaPlayerViewAdapter` owns the prefab-only visual lane; not live chain |
| `GGReplicaPlayerSkin` | `GGReplicaPlayerSkin.asset` | ScriptableObject | Reference | `Assets/_Data/Ship/GGReplicaPlayerSkin.asset` | `GGReplicaPlayerSkinAssetBuilder` | Experimental PlayerSkin table for original GG ViewState ints `0,1,2,3,4,5,6,7,8,9,15`; Dodge body sprites intentionally null |
| `GGReplicaVisualProfile` | `GGReplicaShipVisualProfile.asset` | ScriptableObject | Reference | `Assets/_Data/Ship/GGReplicaShipVisualProfile.asset` | GGReplica | Legacy audio bridge / prototype five-state data; not the rebuilt PlayerView visual authority |
| `GGReplicaFeelProfile` | `GGReplicaShipFeelProfile.asset` | ScriptableObject | Reference | `Assets/_Data/Ship/GGReplicaShipFeelProfile.asset` | GGReplica | Boost/Dodge feel tuning |
| `GGReplicaPlayerViewRuntimeOwner` | `GGReplicaPlayerViewAdapter` | Runtime Script | Reference | `Assets/Scripts/Ship/GGReplica/GGReplicaPlayerViewAdapter.cs` | GGReplica Runtime | Experimental runtime owner for `Ship_GGReplica.prefab` only; consumes `GGReplicaPlayerSkin.asset` and notifies Core/Boost/Shape/Material modules |
| `GGReplicaMaterialRuntimeOwner` | `GGReplicaMaterialVisualModule` | Runtime Script | Reference | `Assets/Scripts/Ship/GGReplica/GGReplicaMaterialVisualModule.cs` | GGReplica Runtime | Runtime `MaterialPropertyBlock` owner for GGReplica shader/material state feedback; never mutates shared material assets |
| `GGReplicaLegacySpriteSwitcher` | `GGReplicaShipViewAdapter` | Runtime Script | Legacy | `Assets/Scripts/Ship/GGReplica/GGReplicaShipViewAdapter.cs` | Legacy Prototype | Old five-state sprite switcher; retained for reference/tests only, not wired by rebuilt `Ship_GGReplica.prefab` |
| `GGReplicaPlayerViewHierarchy` | `ShipVisual/GGPlayerViewRoot` | Prefab Node | Reference | `Assets/_Prefabs/Ship/Ship_GGReplica.prefab` | `GGReplicaPrefabBuilder` | Experimental prefab-only hierarchy with 11 required PlayerView render nodes; live `ShipView` lane is disabled on replica prefab |
| `GGReplicaArtRoot` | `GGReplica/` | Art Folder | Reference | `Assets/_Art/Ship/GGReplica/` | GGReplica | Curated GG reference assets |
| `GGReplicaPlayerShipHighlightShader` | `GGReplicaPlayerShipHighlight.shader` | Shader | Reference | `Assets/_Art/Ship/GGReplica/Shaders/GGReplicaPlayerShipHighlight.shader` | `GGReplicaMaterialAssetBuilder` | Clean Unity 6000-compatible equivalent of GG `CLG/PlayerShipHighlight`; source evidence: `DevXUnity/CLG/CLG_PlayerShipHighlight.shader` |
| `GGReplicaTeleportSchemeShader` | `GGReplicaTeleportScheme.shader` | Shader | Reference | `Assets/_Art/Ship/GGReplica/Shaders/GGReplicaTeleportScheme.shader` | `GGReplicaMaterialAssetBuilder` | Clean equivalent of GG `TeleportScheme`; source evidence: `DevXUnity/Shader Graphs/Shader Graphs_TeleportScheme.shader` |
| `GGReplicaPlayerLQTrailShader` | `GGReplicaPlayerLQTrail.shader` | Shader | Reference | `Assets/_Art/Ship/GGReplica/Shaders/GGReplicaPlayerLQTrail.shader` | `GGReplicaMaterialAssetBuilder` | Clean equivalent of GG `PlayerLQTrail`; source evidence: `DevXUnity/Shader Graphs/Shader Graphs_PlayerLQTrail.shader` |
| `GGReplicaPlayerShipHLMaterial` | `GGReplicaPlayerShipHL.mat` | Material | Reference | `Assets/_Art/Ship/GGReplica/Materials/GGReplicaPlayerShipHL.mat` | `GGReplicaMaterialAssetBuilder` | Mirrors original `PlayerShipHL.mat` values: `_Intensity=8`, `_Smooth=0.01`, `_Tint=#8B17FF` |
| `GGReplicaTeleportSchemeMaterial` | `GGReplicaTeleportScheme.mat` | Material | Reference | `Assets/_Art/Ship/GGReplica/Materials/GGReplicaTeleportScheme.mat` | `GGReplicaMaterialAssetBuilder` | Mirrors original `TeleportScheme.mat` parameter evidence and is wired to `GGPlayerViewRoot/View` |
| `GGReplicaPlayerLQTrailMaterial` | `GGReplicaPlayerLQTrail.mat` | Material | Reference | `Assets/_Art/Ship/GGReplica/Materials/GGReplicaPlayerLQTrail.mat` | `GGReplicaMaterialAssetBuilder` | Mirrors original `PlayerLQTrail.mat` color/noise values and is wired to `ShipVisual/GGPlayerLQTrail` |
| `GGReplicaTestScene` | `GGReplicaShipTest.unity` | Scene | Reference | `Assets/Scenes/GGReplicaShipTest.unity` | GGReplica | A/B validation scene using original GG ViewState int controls |
| `GGReplicaGlitchV2Prefab` | `Ship_GGReplicaV2.prefab` | Prefab | Reference | `Assets/_Prefabs/Ship/Ship_GGReplicaV2.prefab` | `GGReplicaGlitchV2PrefabBuilder` | V2 reset path: input-driven Glitch ship replica with module-style Boost/LQTrail/Fluxy/Grab/Heal roots; does not use old 0-9 sprite switcher |
| `GGReplicaGlitchV2TestScene` | `GGReplicaGlitchV2Test.unity` | Scene | Reference | `Assets/Scenes/GGReplicaGlitchV2Test.unity` | `GGReplicaGlitchV2TestSceneBuilder` | Input-driven validation scene: WASD/Shift/Space/E/Q/Mouse, no `GGReplicaTestSwitcher` button UI |
| `GGReplicaGlitchV2Runtime` | `GGReplicaGlitchInputDriver` / `GGReplicaGlitchMotor` / `GGReplicaGlitchView` | Runtime Script | Reference | `Assets/Scripts/Ship/GGReplica/V2/` | GGReplica Runtime | V2 playable slice for movement, boost velocity, dodge burst, grab hold, heal, and module VFX toggles |
| `GGReplicaIsolationAuditor` | `GGReplicaAuditor` | Editor Tool | Reference | `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaAuditor.cs` | GGReplica | Read-only isolation audit for PlayerView replica prefab, PlayerSkin data, and live-chain pollution |

## 9. 当前已知治理重点

### Phase A 治理状态：✅ 全部通过（2026-03-21 审计）

五条治理验收标准全部满足：

1. ✅ **唯一权威**：每类引用只有一个权威来源（`ShipPrefabRebuilder` / `BoostTrailPrefabCreator` / `ShipBoostTrailSceneBinder` / `MaterialTextureLinker` 各司其职，无交叉写入）
2. ✅ **无双轨主链**：所有 `FindAssets` / `FindSpriteExactOrByName` / `VisualChild` legacy / `DODGE_SPRITE_SRC_PATH` 已清理，全面改为精确路径
3. ✅ **Debug 不接管主链**：`BoostTrailDebugManager` 无 `Awake/OnValidate/Reset/LateUpdate/AutoAssignReferences`，纯预览组件；`ShipBoostDebugMenu.cs` 已删除
4. ✅ **Override 白名单化**：`ShipVfxValidator` 定义了 scene override 白名单，仅允许 `_boostBloomVolume`
5. ✅ **无静默失败**：关键引用缺失时有 `Debug.LogError`/`LogWarning`，`ShipVfxValidator` 审计全覆盖

### 持续关注项

1. `ShipShipState` 当前结论为 **不可改（当前批）**：它是 runtime 数据类型，改名收益低于 API / 序列化 / 编译风险。
2. `ShipView._hlRenderer` 当前指向 Canary `Ship_Sprite_Outline`，字段名仅为兼容性语义名；不得据此恢复旧 `Ship_Sprite_HL` 节点。
3. `FlameTrail_B` / `MainTrail` / `Ember*` / `BoostEnergyLayer*` 已从正式 Boost 链路清退；后续 validator 若发现这些节点重新出现在 `BoostTrailRoot.prefab`，应视为 legacy residue。
4. 首批 low-risk dormant 资源已完成引用审计并进入清退：`mat_ui_boost_flash`、`UIBoostFlash`、`boost_activation_halo`、`boost_field_*`、`ship_*_gg`。
5. 后续若继续清理 dormant / legacy reference 资源，必须继续保持 GUID + 文本双重引用审计。
