# 实施计划：Glitch 飞船特效 Bug 修复

- [ ] 1. 修复 ShipGlowMaterial Additive 混合（B2）
   - 重写 `ShipGlowMaterialCreator.CreateOrGet()`：当材质已存在时先删除再重建（强制刷新）
   - 改用双 shader 策略：优先尝试 URP 2D `Sprite-Lit-Default` + 关键字 `_SURFACE_TYPE_TRANSPARENT` + `_SrcBlend=1/_DstBlend=1`；若属性不存在则 fallback 到 `Sprites/Default` + `_SrcBlend=1/_DstBlend=1`
   - 设置 `renderQueue = 3000`，`SetOverrideTag("RenderType","Transparent")`
   - _需求：2.1、2.2、2.3、2.4_

- [ ] 2. 复制 player_test_fire.png 并配置导入设置（B1）
   - 在 `ShipPrefabRebuilder` 中新增 `EnsureDodgeSpriteTexture()` 方法：从 `D:\ReferenceAssets\GalacticGlitch\GG_Ripped\ExportedProject\Assets\Texture2D\player_test_fire.png` 复制到 `Assets/_Art/Ship/Glitch/Reference/player_test_fire.png`
   - 通过 `TextureImporter` 设置导入参数：`TextureType = Sprite`，`PPU = 707`，`FilterMode = Bilinear`，`Compression = None`
   - 将复制后的 Sprite 赋值给 `Dodge_Sprite` 的 `SpriteRenderer.sprite`；若源文件不存在则写入 `todo` 并 fallback 使用 `Ship_Sprite_Solid` 的 sprite
   - _需求：1.1、1.2、1.3、1.4_

- [ ] 3. 修复 Boost 粒子方向（B4）
   - 修改 `ShipBoostTrailVFX.ApplyParticleSettings()`：将 `simulationSpace` 从 `World` 改为 `Local`
   - 通过 `velocityOverLifetime` 或 `startSpeed` + `shape` 模块将初始速度方向设为局部 `-Y`（尾部喷出），速度值读取 `_juiceSettings.BoostTrailStartSpeed`
   - 在 `ShipPrefabRebuilder` 中确保 `BoostTrailParticles` GO 的 `localRotation = Quaternion.identity`
   - _需求：4.1、4.2、4.3、4.4_

- [ ] 4. 为 Boost 粒子和 TrailRenderer 赋予 Additive 材质（B3）
   - 修改 `ShipPrefabRebuilder`：在创建/更新 `BoostTrailParticles` 时，调用 `ShipGlowMaterialCreator.CreateOrGet()` 获取材质，赋值给 `ParticleSystemRenderer.sharedMaterial`
   - 同样为 `BoostTrail` 的 `TrailRenderer.sharedMaterial` 赋值 `ShipGlowMaterial`
   - IF `ShipGlowMaterial` 不存在则自动先创建再赋值
   - _需求：3.1、3.2、3.3_

- [ ] 5. 为引擎粒子添加青绿色颜色渐变（B5）
   - 在 `ShipJuiceSettingsSO` 中新增 `[SerializeField] private Color _engineParticleColor` 字段（默认 `rgba(0.28, 0.43, 0.43, 1.0)`）及 Public Getter `EngineParticleColor`
   - 修改 `ShipEngineVFX.ApplyPreciseBaseSettings()`：启用 `colorOverLifetime` 模块，设置从 `_juiceSettings.EngineParticleColor`（alpha=1）渐变至同色（alpha=0）的 Gradient
   - 确保颜色渐变在 Dash/Boost 状态切换时不被重置
   - _需求：5.1、5.2、5.3_

- [ ] 6. 清理死代码和冗余字段（B6、B7）
   - 从 `ShipView.cs` 中移除 `_solidTween` 字段声明及 `_solidTween.Stop()` 调用
   - 从 `ShipJuiceSettingsSO.cs` 中移除旧版 Engine 参数组：`_engineParticleMinSpeed`、`_engineBaseEmissionRate`、`_engineDashEmissionMultiplier` 及其对应 Public Getters
   - 检查并更新 `ShipEngineVFX.cs` 中所有对已删除旧字段的引用，改为使用新版精确参数字段
   - 确认编译零错误
   - _需求：6.1、6.2、6.3、6.4_

- [ ] 7. 更新 ImplementationLog
   - 在 `Docs/ImplementationLog/ImplementationLog.md` 末尾追加本次修复的日志条目
   - 记录：修改文件列表、每个 Bug 的根本原因和修复方案、技术方案要点
   - _需求：非功能性需求（零破坏性、编译零错误）_
