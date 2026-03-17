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
| `ShipViewController` | `ShipView` | Runtime Script | Live | `Assets/Scripts/Ship/VFX/ShipView.cs` | Runtime | 飞船 sprite / ghost / boost 视觉总控 |
| `BoostTrailController` | `BoostTrailView` | Runtime Script | Live | `Assets/Scripts/Ship/VFX/BoostTrailView.cs` | Runtime | Boost 尾迹主控 |
| `ShipEngineVFXController` | `ShipEngineVFX` | Runtime Script | Live | `Assets/Scripts/Ship/VFX/ShipEngineVFX.cs` | Runtime | 常驻引擎尾焰 |
| `ShipVisualJuiceController` | `ShipVisualJuice` | Runtime Script | Live | `Assets/Scripts/Ship/VFX/ShipVisualJuice.cs` | Runtime | tilt / squash / stretch |
| `DashAfterImageSpawnerController` | `DashAfterImageSpawner` | Runtime Script | Live | `Assets/Scripts/Ship/VFX/DashAfterImageSpawner.cs` | Runtime | Dash 连续残影 |
| `DashAfterImageRuntime` | `DashAfterImage` | Runtime Script | Live | `Assets/Scripts/Ship/VFX/DashAfterImage.cs` | Runtime | 残影实例组件 |
| `ShipBoostStateController` | `ShipBoost` | Runtime Script | Live | `Assets/Scripts/Ship/Movement/ShipBoost.cs` | Runtime | 提供 Boost 事件，不直接渲染 |
| `ShipStateMachineController` | `ShipStateController` | Runtime Script | Live | `Assets/Scripts/Ship/ShipStateController.cs` | Runtime | 统一状态事件入口 |
| `ShipStateEnum` | `ShipShipState` | Data Type | Live | `Assets/Scripts/Ship/Data/ShipShipState.cs` | Data | 物理名冻结，语义名统一为 Ship State Enum |
| `ShipJuiceSettings` | `ShipJuiceSettingsSO` | ScriptableObject Type | Live | `Assets/Scripts/Ship/Data/ShipJuiceSettingsSO.cs` | Data | 视觉参数数据源 |

## 3. Editor Tools

| Canonical Name | Physical Name | Type | Status | Current Path | Owner | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| `ShipPrefabAuthority` | `ShipPrefabRebuilder` | Editor Tool | Live | `Assets/Scripts/Ship/Editor/ShipPrefabRebuilder.cs` | Editor | `Ship.prefab` 结构与关键引用权威 |
| `BoostTrailPrefabAuthority` | `BoostTrailPrefabCreator` | Editor Tool | Live | `Assets/Scripts/Ship/Editor/BoostTrailPrefabCreator.cs` | Editor | 仅负责 `BoostTrailRoot.prefab` |
| `BoostTrailMaterialLinker` | `MaterialTextureLinker` | Editor Tool | Live | `Assets/Scripts/Ship/Editor/MaterialTextureLinker.cs` | Editor | 只维护现役材质映射 |
| `BoostTrailSceneBinder` | `ShipBoostTrailSceneBinder` | Editor Tool | Live | `Assets/Scripts/Ship/Editor/ShipBoostTrailSceneBinder.cs` | Editor | scene-only 引用绑定 |
| `ShipSceneBootstrapTool` | `ShipBuilder` | Editor Tool | Live | `Assets/Scripts/Ship/Editor/ShipBuilder.cs` | Editor | 场景 bootstrap，非 prefab 权威 |
| `ShipGlowMaterialCreator` | `ShipGlowMaterialCreator` | Editor Tool | Live | `Assets/Scripts/Ship/Editor/ShipGlowMaterialCreator.cs` | Editor | 共享 glow 材质辅助生成 |
| `ShipReferenceTextureImporter` | `CopyGGTextures.ps1` | Import Tool | Reference | `Tools/CopyGGTextures.ps1` | Reference Import | 参考资源导入；仅复制当前保留的 GG 纹理，不定义现役规范 |

## 4. Prefabs and Scene Bindings

| Canonical Name | Physical Name | Type | Status | Current Path | Owner | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| `ShipPrefab` | `Ship.prefab` | Prefab | Live | `Assets/_Prefabs/Ship/Ship.prefab` | `ShipPrefabRebuilder` | 飞船主体 prefab |
| `BoostTrailRootPrefab` | `BoostTrailRoot.prefab` | Prefab | Live | `Assets/_Prefabs/VFX/BoostTrailRoot.prefab` | `BoostTrailPrefabCreator` | 独立生成后由 `ShipPrefabRebuilder` 集成 |
| `DashAfterImagePrefab` | `DashAfterImage.prefab` | Prefab | Live | `Assets/_Prefabs/Ship/DashAfterImage.prefab` | Runtime / Prefab | Dash 残影 prefab |
| `ShipVisualRoot` | `ShipVisual` | Prefab Node | Live | `Assets/_Prefabs/Ship/Ship.prefab` | `ShipPrefabRebuilder` | `VisualChild` 是 legacy alias |
| `ShipVisualRootLegacyAlias` | `VisualChild` | Prefab Node Alias | Legacy | `Assets/_Prefabs/Ship/Ship.prefab` | Legacy | 仅作兼容查找，不再作为规范名 |
| `ShipDashGhostNode` | `Dodge_Sprite` | Prefab Node | Live | `Assets/_Prefabs/Ship/Ship.prefab` | `ShipPrefabRebuilder` | Dash 静态 ghost |
| `ShipEngineParticlesNode` | `EngineParticles` | Prefab Node | Live | `Assets/_Prefabs/Ship/Ship.prefab` | `ShipPrefabRebuilder` | 常驻引擎尾焰 |
| `ShipHighlightSpriteNode` | `Ship_Sprite_HL` | Prefab Node | Live | `Assets/_Prefabs/Ship/Ship.prefab` | `ShipPrefabRebuilder` | 物理名冻结；由 `ShipView._hlRenderer` 持有序列化引用；若要改名需同步 `ShipPrefabRebuilder` / `ShipBuilder` 并走 Editor 复验 |
| `LeftFlameTrailNode` | `FlameTrail_B` | Prefab Node | Live | `Assets/_Prefabs/VFX/BoostTrailRoot.prefab` | `BoostTrailPrefabCreator` | 物理名冻结；由 `BoostTrailView._flameTrailB` 持有序列化引用；若要改名需同步 `BoostTrailPrefabCreator` 并走 Editor 复验 |
| `BoostTrailNestedBinding` | `_boostTrailView` | Serialized Reference | Live | `Assets/_Prefabs/Ship/Ship.prefab` | `ShipPrefabRebuilder` | 应指向 nested `BoostTrailRoot` |
| `BoostTrailBloomSceneObject` | `BoostTrailBloomVolume` | Scene Object | Live | `Assets/Scenes/SampleScene.unity` | `ShipBoostTrailSceneBinder` | 仅场景级存在 |
| `BoostTrailBloomProfile` | `BoostBloomVolumeProfile.asset` | Asset | Live | `Assets/Settings/BoostBloomVolumeProfile.asset` | Scene / Settings | 给 scene volume 使用 |

## 5. Materials and Shaders

| Canonical Name | Physical Name | Type | Status | Current Path | Owner | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| `ShipSharedGlowMaterial` | `ShipGlowMaterial.mat` | Material | Live | `Assets/_Art/Ship/Glitch/ShipGlowMaterial.mat` | `ShipGlowMaterialCreator` | 飞船 glow / Halo 共享材质 |
| `BoostTrailMainMaterial` | `mat_trail_main.mat` | Material | Live | `Assets/_Art/VFX/BoostTrail/Materials/mat_trail_main.mat` | `MaterialTextureLinker` | 主拖尾材质，固定 `_UseLegacySlots=0` |
| `BoostEnergyLayer2Material` | `mat_boost_energy_layer2.mat` | Material | Live | `Assets/_Art/VFX/BoostTrail/Materials/mat_boost_energy_layer2.mat` | `MaterialTextureLinker` | 对应 Layer2 shader |
| `BoostEnergyLayer3Material` | `mat_boost_energy_layer3.mat` | Material | Live | `Assets/_Art/VFX/BoostTrail/Materials/mat_boost_energy_layer3.mat` | `MaterialTextureLinker` | 对应 Layer3 shader |
| `BoostFlameTrailMaterial` | `mat_flame_trail.mat` | Material | Live | `Assets/_Art/VFX/BoostTrail/Materials/mat_flame_trail.mat` | `MaterialTextureLinker` | 火焰粒子材质 |
| `BoostEmberTrailMaterial` | `mat_ember_trail.mat` | Material | Live | `Assets/_Art/VFX/BoostTrail/Materials/mat_ember_trail.mat` | `MaterialTextureLinker` | 余烬拖尾材质 |
| `BoostEmberSparksMaterial` | `mat_ember_sparks.mat` | Material | Live | `Assets/_Art/VFX/BoostTrail/Materials/mat_ember_sparks.mat` | `MaterialTextureLinker` | 余烬火花材质 |
| `BoostTrailMainShader` | `TrailMainEffect.shader` | Shader | Live | `Assets/_Art/VFX/BoostTrail/Shaders/TrailMainEffect.shader` | Shader | 仍保留 legacy branch 代码，但材质链已收口 |
| `BoostEnergyLayer2Shader` | `BoostEnergyLayer2.shader` | Shader | Live | `Assets/_Art/VFX/BoostTrail/Shaders/BoostEnergyLayer2.shader` | Shader | Layer2 能量层 |
| `BoostEnergyLayer3Shader` | `BoostEnergyLayer3.shader` | Shader | Live | `Assets/_Art/VFX/BoostTrail/Shaders/BoostEnergyLayer3.shader` | Shader | Layer3 能量层 |

## 6. Textures and Sprites

| Canonical Name | Physical Name | Type | Status | Current Path | Owner | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| `ShipLiquidBoostSprite` | `Boost_16.png` | Sprite | Live | `Assets/_Art/Ship/Glitch/Boost_16.png` | `ShipView` / `ShipPrefabRebuilder` | Boost 液态层 |
| `ShipLiquidNormalSprite` | `Movement_3.png` | Sprite | Live | `Assets/_Art/Ship/Glitch/Movement_3.png` | `ShipView` / `ShipPrefabRebuilder` | Normal 液态层 |
| `ShipSolidNormalSprite` | `Movement_10.png` | Sprite | Live | `Assets/_Art/Ship/Glitch/Movement_10.png` | `ShipPrefabRebuilder` | 实色层 |
| `ShipHighlightNormalSprite` | `Movement_21.png` | Sprite | Live | `Assets/_Art/Ship/Glitch/Movement_21.png` | `ShipPrefabRebuilder` | 高亮层 |
| `ShipPrimaryBaseSprite` | `Primary.png` | Sprite | Reference | `Assets/_Art/Ship/Glitch/Primary.png` | Reference | 上游参考，不是当前主链 |
| `ShipPrimaryVariant4Sprite` | `Primary_4.png` | Sprite | Reference | `Assets/_Art/Ship/Glitch/Primary_4.png` | Reference | 结构分析参考 |
| `ShipPrimaryVariant6Sprite` | `Primary_6.png` | Sprite | Reference | `Assets/_Art/Ship/Glitch/Primary_6.png` | Reference | 结构分析参考 |
| `ShipDashGhostSource` | `Reference/player_test_fire.png` | Sprite | Live | `Assets/_Art/Ship/Glitch/Reference/player_test_fire.png` | `ShipPrefabRebuilder` | Dash ghost 来源 |
| `BoostTrailMainTexture` | `vfx_boost_techno_flame.png` | Texture | Live | `Assets/_Art/VFX/BoostTrail/Textures/vfx_boost_techno_flame.png` | `MaterialTextureLinker` | 主拖尾 / 火焰粒子 |
| `BoostEmberTrailTexture` | `vfx_ember_trail.png` | Texture | Live | `Assets/_Art/VFX/BoostTrail/Textures/vfx_ember_trail.png` | `MaterialTextureLinker` | 余烬拖尾 |
| `BoostEmberSparksTexture` | `vfx_ember_sparks.png` | Texture | Live | `Assets/_Art/VFX/BoostTrail/Textures/vfx_ember_sparks.png` | `MaterialTextureLinker` | 余烬火花 |
| `BoostNoiseMainTexture` | `boost_noise_main.png` | Texture | Live | `Assets/_Art/VFX/BoostTrail/Textures/boost_noise_main.png` | `MaterialTextureLinker` | Layer2 Tex0 |
| `BoostNoiseDistortTexture` | `boost_noise_distort.png` | Texture | Live | `Assets/_Art/VFX/BoostTrail/Textures/boost_noise_distort.png` | `MaterialTextureLinker` | Layer2 Tex1 |
| `BoostNoiseLayer3Texture` | `boost_noise_layer3.png` | Texture | Live | `Assets/_Art/VFX/BoostTrail/Textures/boost_noise_layer3.png` | `MaterialTextureLinker` | Layer2 Tex2 |
| `BoostNoiseLayer4Texture` | `boost_noise_layer4.png` | Texture | Live | `Assets/_Art/VFX/BoostTrail/Textures/boost_noise_layer4.png` | `MaterialTextureLinker` | Layer2 Tex3 |
| `BoostEnergyNoiseTexture` | `boost_energy_noise_a.png` | Texture | Live | `Assets/_Art/VFX/BoostTrail/Textures/boost_energy_noise_a.png` | `MaterialTextureLinker` | Layer3 Tex0 |
| `BoostEnergyMainTexture` | `boost_energy_main.png` | Texture | Live | `Assets/_Art/VFX/BoostTrail/Textures/boost_energy_main.png` | `MaterialTextureLinker` | Layer3 Tex1 |
| `BoostActivationHaloPrimary` | `vfx_ring_glow_uneven.png` | Texture | Live | `Assets/_Art/VFX/BoostTrail/Textures/vfx_ring_glow_uneven.png` | `BoostTrailPrefabCreator` | Halo 主纹理 |
| `BoostActivationHaloFallback` | `vfx_magnetic_rings.png` | Texture | Live | `Assets/_Art/VFX/BoostTrail/Textures/vfx_magnetic_rings.png` | `BoostTrailPrefabCreator` | Halo 回退纹理 |

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

## 8. 当前已知治理重点

1. `Ship.prefab` 内 `BoostTrailRoot` 的嵌套集成与 `_boostTrailView` 接线，需要由 `ShipPrefabRebuilder` 正式接管。
2. `MaterialTextureLinker` 应持续避免模糊全项目搜索，优先使用精确路径回填现役材质。
3. `FlameTrail_B` 当前结论为 **需 Editor 配合**：`BoostTrailView._flameTrailB` 通过序列化引用消费，且 `BoostTrailPrefabCreator` 仍硬编码该节点名。
4. `Ship_Sprite_HL` 当前结论为 **需 Editor 配合**：`ShipView._hlRenderer` 通过序列化引用消费，且 `ShipPrefabRebuilder` / `ShipBuilder` 仍硬编码该节点名。
5. `ShipShipState` 当前结论为 **不可改（当前批）**：它是 runtime 数据类型，改名收益低于 API / 序列化 / 编译风险。
6. `Ship_Sprite_HL` 当前 live prefab 使用的是共享默认材质链，不应误判为必须与 `ShipGlowMaterial.mat` 一起迁移。
7. 首批 low-risk dormant 资源已完成引用审计并进入清退：`mat_ui_boost_flash`、`UIBoostFlash`、`boost_activation_halo`、`boost_field_*`、`ship_*_gg`。
8. 后续若继续清理 dormant 资源，必须继续保持 GUID + 文本双重引用审计。
