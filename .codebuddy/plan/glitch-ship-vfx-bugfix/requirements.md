# 需求文档：Glitch 飞船特效 Bug 修复

## 引言

本文档描述对上一轮 `glitch-ship-vfx-complete` 实现中发现的 **4 个严重差距** 和 **3 个重要差距** 的修复需求。这些问题导致当前运行时效果与 GG 原版 Glitch 飞船存在明显视觉差异。

### 差距来源

通过代码审查发现以下问题：

| 编号 | 差距 | 严重程度 | 根本原因 |
|------|------|---------|---------|
| B1 | `player_test_fire.png` 未复制到项目 | 🔴 严重 | Phase 1 遗漏，Dodge 残影贴图为 null |
| B2 | `ShipGlowMaterial` Additive 混合未生效 | 🔴 严重 | URP 2D shader 不支持 `_SrcBlend/_DstBlend` 属性 |
| B3 | Boost 粒子/Trail 无 Additive 材质 | 🔴 严重 | `ShipPrefabRebuilder` 创建 GO 时未赋材质 |
| B4 | Boost 粒子方向错误（World 空间无方向设置） | 🔴 严重 | `simulationSpace = World` 但未设置初始速度方向 |
| B5 | 引擎粒子颜色未设置（白色而非青绿色） | 🟡 重要 | `ShipEngineVFX` 未配置粒子颜色渐变 |
| B6 | `_solidTween` 死代码（声明但从未赋值） | 🟡 重要 | 重构遗留，造成代码混淆 |
| B7 | 旧版 Engine 参数冗余（两套重复字段） | 🟡 重要 | 未清理旧字段，Inspector 混淆 |

---

## 需求

### 需求 1：修复 Dodge_Sprite 残影贴图缺失（B1）

**用户故事：** 作为玩家，我希望 Dash 时看到使用 `player_test_fire.png` 的青绿色轮廓残影，以便与 GG 原版视觉效果一致。

#### 验收标准

1. WHEN 运行 `ProjectArk > Ship > Rebuild Ship Prefab Sprite Layers` THEN 系统 SHALL 在 `D:\ReferenceAssets\GalacticGlitch\GG_Ripped\ExportedProject\Assets\Texture2D\` 中查找 `player_test_fire.png` 并将其复制到 `Assets/_Art/Ship/Glitch/Reference/player_test_fire.png`
2. WHEN `player_test_fire.png` 已存在于项目中 THEN `ShipPrefabRebuilder` SHALL 将其赋值给 `Dodge_Sprite` 的 `SpriteRenderer.sprite` 字段
3. IF `player_test_fire.png` 在参考目录中不存在 THEN 系统 SHALL 在 `todo` 列表中输出明确的手动操作提示，并 fallback 使用 `Ship_Sprite_Solid` 的 sprite
4. WHEN `player_test_fire.png` 被复制 THEN 其导入设置 SHALL 为：`TextureType = Sprite`，`PPU = 707`，`FilterMode = Bilinear`，`Compression = None`

---

### 需求 2：修复 ShipGlowMaterial Additive 混合（B2）

**用户故事：** 作为玩家，我希望 `Ship_Sprite_Liquid` 层以 Additive 叠加方式渲染，产生发光效果，以便与 GG 原版的能量层视觉一致。

#### 验收标准

1. WHEN 运行 `ProjectArk > Ship > Create Ship Glow Material` THEN 系统 SHALL 使用 `Sprites/Default` shader（Built-in 兼容）并通过 `EnableKeyword("ETC1_EXTERNAL_ALPHA")` 或直接设置 `renderQueue` 的方式实现 Additive 混合
2. `ShipGlowMaterial` SHALL 使用以下正确的 URP 2D Additive 配置：shader 为 `Universal Render Pipeline/2D/Sprite-Lit-Default`，通过 `material.SetOverrideTag("RenderType", "Transparent")` + `material.renderQueue = 3000` + `material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT")` + `material.SetFloat("_BlendOp", 0)` + `material.SetFloat("_SrcBlend", 1)` + `material.SetFloat("_DstBlend", 1)` 实现 Additive
3. IF URP 2D shader 不支持上述属性 THEN 系统 SHALL fallback 使用 `Sprites/Default` shader 并设置 `_SrcBlend=1, _DstBlend=1`（Built-in Additive），并在 Console 输出警告提示手动验证
4. WHEN 材质已存在时重新运行工具 THEN 系统 SHALL 删除旧材质并重新创建（强制刷新），而不是直接返回旧材质
5. WHEN 材质创建完成 THEN Inspector 中 `Ship_Sprite_Liquid` 的 SpriteRenderer 应可见叠加发光效果（在深色背景下 Liquid 层颜色叠加到 Solid 层上方）

---

### 需求 3：为 Boost 粒子和 TrailRenderer 赋予 Additive 材质（B3）

**用户故事：** 作为玩家，我希望 Boost 时的粒子尾迹和 TrailRenderer 以 Additive 叠加方式渲染，产生发光流体感，以便与 GG 原版 `AdditiveTrail.mat` 效果一致。

#### 验收标准

1. WHEN 运行 `ShipPrefabRebuilder` THEN 系统 SHALL 为 `BoostTrailParticles` 的 `ParticleSystemRenderer` 赋值 `ShipGlowMaterial`（Additive 材质）
2. WHEN 运行 `ShipPrefabRebuilder` THEN 系统 SHALL 为 `BoostTrail` 的 `TrailRenderer` 赋值 `ShipGlowMaterial`（Additive 材质）
3. IF `ShipGlowMaterial` 不存在 THEN `ShipPrefabRebuilder` SHALL 自动调用 `ShipGlowMaterialCreator.CreateOrGet()` 先创建材质，再赋值
4. WHEN 材质赋值完成 THEN Boost 激活时粒子和尾迹 SHALL 在深色背景下呈现叠加发光的青绿色效果，而非不透明白色

---

### 需求 4：修复 Boost 粒子方向（B4）

**用户故事：** 作为玩家，我希望 Boost 时粒子从飞船尾部向后喷出，方向始终跟随飞船朝向，以便与 GG 原版推进器尾迹方向一致。

#### 验收标准

1. WHEN `ShipBoostTrailVFX.ApplyParticleSettings()` 被调用 THEN 系统 SHALL 将 `simulationSpace` 改为 `ParticleSystemSimulationSpace.Local`（而非 World），使粒子方向跟随飞船旋转
2. WHEN Boost 激活 THEN 粒子 SHALL 沿飞船局部坐标的 `-Y` 方向（尾部方向）喷出，起始速度为 `_juiceSettings.BoostTrailStartSpeed`（默认 3.0）
3. WHEN 飞船旋转时 THEN 粒子发射方向 SHALL 实时跟随飞船朝向，不出现粒子向世界固定方向喷射的情况
4. WHEN `BoostTrailParticles` GO 的局部坐标 `-Y` 方向不对齐飞船尾部 THEN `ShipPrefabRebuilder` SHALL 将该 GO 的 `localRotation` 设为 `Quaternion.identity`（依赖父节点 `Ship_Sprite_Back` 的朝向）

---

### 需求 5：为引擎粒子设置青绿色颜色渐变（B5）

**用户故事：** 作为玩家，我希望引擎粒子颜色为青绿色渐变至透明，以便与 GG 原版 `rgba(0.28, 0.43, 0.43)` 的引擎颜色一致，而非默认白色。

#### 验收标准

1. WHEN `ShipEngineVFX.ApplyPreciseBaseSettings()` 被调用 THEN 系统 SHALL 通过 `colorOverLifetime` 模块设置粒子颜色：从 `rgba(0.28, 0.43, 0.43, 1.0)` 渐变至 `rgba(0.28, 0.43, 0.43, 0.0)`
2. 引擎粒子颜色 SHALL 通过 `ShipJuiceSettingsSO` 的新字段 `EngineParticleColor`（默认 `rgba(0.28, 0.43, 0.43, 1.0)`）配置，不得 hardcode
3. WHEN Dash 或 Boost 状态切换时 THEN 粒子颜色渐变 SHALL 保持不变（颜色不随状态切换而改变，只有大小和发射率变化）

---

### 需求 6：清理 ShipView 死代码和 ShipJuiceSettingsSO 冗余字段（B6、B7）

**用户故事：** 作为开发者，我希望代码库中没有死代码和冗余字段，以便保持代码可读性和 Inspector 整洁。

#### 验收标准

1. `ShipView.cs` 中的 `_solidTween` 字段 SHALL 被移除（该字段声明但从未被赋值，`_solidTween.Stop()` 调用无效）
2. `ShipJuiceSettingsSO.cs` 中的旧版 Engine 参数组 SHALL 被移除，包括：`_engineParticleMinSpeed`（旧）、`_engineBaseEmissionRate`（旧）、`_engineDashEmissionMultiplier`（旧），以及对应的 Public Getters `EngineParticleMinSpeed`、`EngineBaseEmissionRate`、`EngineDashEmissionMultiplier`
3. WHEN 旧版字段被移除 THEN 所有引用这些字段的代码（`ShipEngineVFX.cs` 中的旧版引用）SHALL 同步更新为使用新版精确参数字段
4. WHEN 清理完成 THEN 项目 SHALL 无编译错误

---

## 非功能性需求

- **零破坏性**：所有修复不得改变现有已正常工作的功能（Boost 发光层亮度、Dash 闪烁、推进器脉冲、残影生成逻辑）
- **幂等工具**：`ShipGlowMaterialCreator` 和 `ShipPrefabRebuilder` 修复后仍须保持幂等性
- **编译零错误**：所有修改完成后项目必须无编译错误、无编译警告（除 Unity 内部警告外）
- **参数可配置**：新增的 `EngineParticleColor` 字段须通过 `ShipJuiceSettingsSO` 暴露，不得 hardcode
