# 需求文档：Boost Trail VFX Phase 2 — 缺失功能补全

## 引言

本文档是 **Boost Trail VFX 全复刻第二阶段**的需求规格，专门针对 Phase 1 实现后仍缺失的功能。

### 背景：Phase 1 已完成的内容

Phase 1 已实现：
- ✅ TrailRenderer 主拖尾（参数配置、材质、控制脚本）
- ✅ FlameTrail_R/B + FlameCore 粒子系统（参数配置）
- ✅ EmberTrail + EmberSparks 粒子系统（参数配置）
- ✅ 全屏闪光 + URP Volume Bloom 爆发（BoostTrailView.cs）
- ✅ Liquid 贴图切换（ShipView.cs 集成）
- ✅ BoostEnergyLayer2/3/Field HLSL Shader（临时方案）
- ✅ 材质文件创建（颜色参数正确，纹理槽位为空）
- ✅ Editor 一键创建 Prefab 脚本（BoostTrailPrefabCreator.cs）

### Phase 2 需要补全的缺失功能

经过对照 `GalacticGlitch_BoostTrail_VFX_Plan.md`（四 RDC 最终分析）与现有实现的完整检查，以下功能尚未实现：

1. **GGrenderdoc 纹理未导入**：Trail 主特效（eid_1598）、Boost 噪声（eid_1076）、双重能量层（eid_1126）、全局能量场（eid_1964）、飞船本体（eid_877）的纹理均未复制到项目
2. **Trail 主特效 Shader 缺失**：uniforms141 对应的 Shader（双层 Sprite Sheet + 边缘光晕 + ×8 亮度增强）完全未实现
3. **EmberGlow 粒子系统缺失**：ps_ember_glow（Burst 模式，余烬光晕，StartLifetime=0.12s）未在 BoostTrailView 中实现
4. **材质纹理未连线**：所有材质的 `_BaseMap` 槽位为空，需要在 Unity Editor 中连线
5. **Prefab 未实际创建**：BoostTrailPrefabCreator.cs 存在但未执行，BoostTrailRoot.prefab 不存在
6. **飞船本体贴图未导入**：eid_877 的 Solid（662KB）、Liquid（91KB）、Highlight（662KB）贴图未导入项目

---

## 需求

### 需求 1：GGrenderdoc 纹理资产导入

**用户故事：** 作为开发者，我希望将 GGrenderdoc 分析提取的所有关键纹理导入到 Project Ark 项目中，以便 Shader 和材质能够正确引用这些纹理资产。

#### 验收标准

1. WHEN 导入 Trail 主特效纹理时 THEN 系统 SHALL 将以下文件从 `GGrenderdoc/output/targeted_v7/eid_1598/` 复制到 `Assets/_Art/VFX/BoostTrail/Textures/`：
   - `tex_slot0.png`（4.5MB）→ 重命名为 `trail_main_spritesheet.png`（Trail 核心 Sprite Sheet）
   - `tex_slot1.png`（408KB）→ 重命名为 `trail_second_spritesheet.png`（Trail 第二层 Sprite Sheet）
   - `tex_slot2.png`（1.5KB）→ 重命名为 `trail_edge_glow.png`（Trail 边缘光晕纹理）
   - `tex_slot3.png`（90B）→ 重命名为 `trail_color_lut.png`（Trail 辅助颜色 LUT）

2. WHEN 导入 Boost 噪声特效纹理时 THEN 系统 SHALL 将以下文件从 `GGrenderdoc/output/targeted_v7/eid_1076/` 复制到 `Assets/_Art/VFX/BoostTrail/Textures/`：
   - `tex_slot0.png`（1.67MB）→ 重命名为 `boost_noise_main.png`（Boost 噪声主纹理）
   - `tex_slot1.png`（165KB）→ 重命名为 `boost_noise_distort.png`（噪声扰动纹理）
   - `tex_slot2.png`（215KB）→ 重命名为 `boost_noise_layer3.png`（第三层纹理）
   - `tex_slot3.png`（405KB）→ 重命名为 `boost_noise_layer4.png`（第四层纹理）

3. WHEN 导入双重 Boost 能量层纹理时 THEN 系统 SHALL 将以下文件从 `GGrenderdoc/output/targeted_v7/eid_1126/` 复制到 `Assets/_Art/VFX/BoostTrail/Textures/`：
   - `tex_slot0.png`（17KB）→ 重命名为 `boost_energy_noise_a.png`（能量层噪声纹理 A）
   - `tex_slot2.png`（136KB）→ 重命名为 `boost_energy_main.png`（能量层主纹理，两次采样）

4. WHEN 导入全局能量场纹理时 THEN 系统 SHALL 将以下文件从 `GGrenderdoc/output/targeted_v7/eid_1964/` 复制到 `Assets/_Art/VFX/BoostTrail/Textures/`：
   - `tex_slot0.png`（4.78MB）→ 重命名为 `boost_field_main.png`（全局能量场主纹理）
   - `tex_slot1.png`（939KB）→ 重命名为 `boost_field_aux.png`（全局能量场辅助纹理，高质量版）

5. WHEN 导入飞船本体贴图时 THEN 系统 SHALL 将以下文件从 `GGrenderdoc/output/targeted_v7/eid_877/` 复制到 `Assets/_Art/Ship/Glitch/`：
   - `tex_slot0.png`（662KB）→ 重命名为 `ship_solid_gg.png`（飞船 Solid 层贴图）
   - `tex_slot1.png`（91KB）→ 重命名为 `ship_liquid_boost.png`（飞船 Liquid 层 Boost 帧）
   - `tex_slot2.png`（662KB）→ 重命名为 `ship_highlight_gg.png`（飞船 Highlight 层贴图）

---

### 需求 2：Trail 主特效 Shader 实现（uniforms141）

**用户故事：** 作为玩家，我希望飞船 Boost 拖尾有双层 Sprite Sheet 动画、边缘光晕和超亮发光效果，以便拖尾视觉效果与 GalacticGlitch 原版完全一致。

> **技术背景**：基于四 RDC 完全一致的 SPIR-V 反汇编（uniforms141，bound=710），Trail 主特效 Shader 包含：双层 Sprite Sheet 动画（`_child0`/`_child1` 控制）、边缘光晕（径向渐变，`_child4`/`_child5` 控制）、亮度增强 ×8（`_child3 > 0` 时启用）、颜色混合（`_child2.xyz` 权重）、最终 `output.w = 1.0`（完全不透明）。

#### 验收标准

1. WHEN 创建 Trail 主特效 Shader 时 THEN 系统 SHALL 实现以下核心逻辑：
   - 双层 Sprite Sheet 采样：`_child0.xy` 控制第一层帧数，`_child1.xy` 控制第二层帧数
   - 边缘光晕：基于 `_child5.xy` 偏移 + `_child5.zw` 缩放 + `_child4.w` 椭圆参数的径向渐变
   - 亮度增强：`_child3 > 0` 时颜色 ×8（HDR 超亮发光效果）
   - 颜色混合：`_child2.xyz` 权重混合主色和辅色
   - 最终输出：`output.w = 1.0`（完全不透明，与 GG 一致）

2. WHEN Trail 主特效 Shader 运行时 THEN 系统 SHALL 使用 4 张纹理槽位：
   - slot0（bind=1）：Trail 核心 Sprite Sheet（`trail_main_spritesheet.png`，4.5MB）
   - slot1（bind=2）：Trail 第二层 Sprite Sheet（`trail_second_spritesheet.png`，408KB）
   - slot2（bind=3）：Trail 边缘光晕纹理（`trail_edge_glow.png`，1.5KB）
   - slot3（bind=4）：Trail 辅助颜色 LUT（`trail_color_lut.png`，90B）

3. WHEN 创建 Trail 主特效材质时 THEN 系统 SHALL 创建 `mat_trail_main_effect.mat`，引用 Trail 主特效 Shader，并将 4 张纹理连线到对应槽位

4. WHEN Trail 主特效 Shader 编译失败时 THEN 系统 SHALL 退化为使用现有 `mat_trail_main.mat`（URP Particles/Unlit Additive），不阻塞其他功能

---

### 需求 3：Boost 噪声特效 Shader 更新（Layer 2 精确复刻）

**用户故事：** 作为玩家，我希望飞船 Boost 状态下本体表面的能量噪声特效与 GG 原版完全一致，以便获得相同的视觉质感。

> **技术背景**：Phase 1 的 `BoostEnergyLayer2.shader` 是基于 SPIR-V 分析的近似实现，但纹理槽位为空（`_Tex0~3` 均为空）。Phase 2 需要：①将正确的纹理连线到材质；②验证 Shader 逻辑与 SPIR-V 反汇编一致（Perlin 噪声 289.0 常数 + 4层纹理叠加 + smoothstep alpha）。

#### 验收标准

1. WHEN 更新 `mat_boost_energy_layer2.mat` 时 THEN 系统 SHALL 将以下纹理连线到对应槽位：
   - `_Tex0`：`boost_noise_main.png`（1.67MB 主纹理）
   - `_Tex1`：`boost_noise_distort.png`（165KB 扰动纹理）
   - `_Tex2`：`boost_noise_layer3.png`（215KB 第三层）
   - `_Tex3`：`boost_noise_layer4.png`（405KB 第四层）

2. WHEN 更新 `mat_boost_energy_layer3.mat` 时 THEN 系统 SHALL 将以下纹理连线到对应槽位：
   - `_Tex0`：`boost_energy_noise_a.png`（17KB 噪声纹理 A）
   - `_Tex1`：`boost_energy_main.png`（136KB 主纹理，UV 缩放 ×2 偏移 -0.5）

3. WHEN 更新 `mat_boost_energy_field.mat` 时 THEN 系统 SHALL 将以下纹理连线到对应槽位：
   - `_LUTTex`：`boost_field_main.png`（4.78MB 主纹理）
   - 将 `_UseLUT` 参数设为 1（启用 LUT 采样模式）

4. WHEN 所有材质纹理连线完成后 THEN 系统 SHALL 验证 `BoostEnergyLayer2.shader` 中的 Perlin 噪声实现使用 289.0 常数（与 GG SPIR-V 一致），如不一致则修正

---

### 需求 4：EmberGlow 粒子系统

**用户故事：** 作为玩家，我希望飞船 Boost 时余烬粒子有光晕效果，以便增加特效的层次感和发光质感。

> **技术背景**：GG 的 `ps_ember_glow` 是独立的粒子系统（材质 `ember_trail_add`，Additive 混合），与 `ps_ember_trail` 并列运行，提供余烬的光晕叠加效果。Phase 1 遗漏了这个粒子系统。

#### 验收标准

1. WHEN 创建 EmberGlow 粒子系统时 THEN 系统 SHALL 使用以下参数：
   - StartLifetime = 0.12s（极短，快速消散）
   - StartSize = 1.0（比 EmberTrail 大，产生光晕扩散感）
   - StartSpeed = 0（不自主移动，跟随飞船）
   - EmissionMode = Burst（每次 Boost 激活时爆发）
   - 材质：URP Particles/Unlit Additive + `vfx_ember_trail.png`，颜色 HDR `(2.0, 1.29, 0)`（橙黄，对应 `mat_boost_ember_trail_add` 的 ColorA）

2. WHEN 飞船进入 Boost 状态 THEN 系统 SHALL 同时激活 EmberGlow 粒子系统（与 EmberTrail 同步启动）

3. WHEN 飞船退出 Boost 状态 THEN 系统 SHALL 停止 EmberGlow 粒子系统（与 EmberTrail 同步停止）

4. WHEN 飞船对象被回收到对象池 THEN 系统 SHALL 调用 `EmberGlow.Stop(true, StopEmittingAndClear)` 完整重置

5. WHEN `BoostTrailView.cs` 更新时 THEN 系统 SHALL 添加 `[SerializeField] private ParticleSystem _emberGlow` 字段，并在 `OnBoostStart/End/ResetState` 中正确处理

---

### 需求 5：BoostTrailRoot Prefab 实际创建与材质连线

**用户故事：** 作为开发者，我希望 BoostTrailRoot Prefab 在 Unity Editor 中实际存在并正确配置，以便可以直接拖入场景使用。

> **技术背景**：Phase 1 创建了 `BoostTrailPrefabCreator.cs` Editor 脚本，但该脚本尚未在 Unity Editor 中执行，因此 `Assets/_Prefabs/VFX/BoostTrailRoot.prefab` 不存在。同时，所有材质的纹理槽位为空，需要在 Unity Editor 中连线。

#### 验收标准

1. WHEN 执行 `ProjectArk > VFX > Create BoostTrailRoot Prefab` 菜单时 THEN 系统 SHALL 在 `Assets/_Prefabs/VFX/BoostTrailRoot.prefab` 创建完整的 Prefab，包含以下层级：
   ```
   BoostTrailRoot [BoostTrailView]
   ├── MainTrail [TrailRenderer]
   ├── FlameTrail_R [ParticleSystem]
   ├── FlameTrail_B [ParticleSystem]
   ├── FlameCore [ParticleSystem]
   ├── EmberTrail [ParticleSystem]
   ├── EmberSparks [ParticleSystem]
   ├── EmberGlow [ParticleSystem]        ← Phase 2 新增
   ├── BoostEnergyLayer2 [SpriteRenderer]
   ├── BoostEnergyLayer3 [SpriteRenderer]
   └── BoostEnergyField [MeshRenderer, disabled]
   ```

2. WHEN Prefab 创建完成后 THEN 系统 SHALL 确保 `BoostTrailView` 的所有 `[SerializeField]` 引用已正确连线（包括新增的 `_emberGlow` 字段）

3. WHEN 材质纹理连线完成后 THEN 系统 SHALL 确保以下材质的 `_BaseMap` 槽位已连线：
   - `mat_flame_trail.mat` → `vfx_boost_techno_flame.png`
   - `mat_ember_trail.mat` → `vfx_ember_trail.png`
   - `mat_ember_sparks.mat` → `vfx_ember_sparks.png`
   - `mat_trail_main.mat` → `vfx_boost_techno_flame.png`

4. WHEN Prefab 创建完成后 THEN 系统 SHALL 提供详细的手动操作步骤文档（Inspector 连线指南），包括：全屏闪光 Image 的创建和连线、Local Volume 的创建和连线、ShipView Inspector 中的字段连线

---

### 需求 6：BoostTrailPrefabCreator.cs 更新（支持 EmberGlow）

**用户故事：** 作为开发者，我希望 Editor 自动化脚本能够创建包含 EmberGlow 的完整 Prefab，以便一键生成正确的层级结构。

#### 验收标准

1. WHEN 更新 `BoostTrailPrefabCreator.cs` 时 THEN 系统 SHALL 在 Prefab 层级中添加 `EmberGlow` 粒子系统节点，并配置正确参数（StartLifetime=0.12s，StartSize=1.0，Burst 模式，材质=`mat_ember_trail`，颜色 HDR `(2.0, 1.29, 0)`）

2. WHEN 更新 `BoostTrailPrefabCreator.cs` 时 THEN 系统 SHALL 通过 `SerializedObject` 将 `_emberGlow` 字段连线到新创建的 EmberGlow 粒子系统

3. WHEN 更新 `BoostTrailPrefabCreator.cs` 时 THEN 系统 SHALL 在创建完成后弹出的对话框中更新手动操作步骤，包含纹理连线指南

---

### 需求 7：飞船本体贴图集成（ShipView 扩展）

**用户故事：** 作为玩家，我希望飞船在 Boost 状态下使用 GG 原版的 Solid 和 Highlight 贴图，以便飞船外观与 GG 原版完全一致。

> **技术背景**：GG 逆向分析（eid_877，四 RDC 完全一致）提取了飞船的 Solid（662KB）、Liquid（91KB）、Highlight（662KB）三张贴图。这些贴图可以直接用于 Project Ark 的 ShipView，替换或补充现有贴图。

#### 验收标准

1. WHEN 导入飞船本体贴图后 THEN 系统 SHALL 将 `ship_solid_gg.png`、`ship_liquid_boost.png`、`ship_highlight_gg.png` 正确导入为 Unity Sprite 资产（Sprite Mode: Single，Pixels Per Unit: 100）

2. WHEN `ShipView.cs` 集成飞船本体贴图时 THEN 系统 SHALL 在 Inspector 中将 `ship_liquid_boost.png` 作为 `_boostLiquidSprite` 的备选（如果 `Boost_16.png` 不可用）

3. IF `ship_solid_gg.png` 与现有 Solid 贴图尺寸不匹配 THEN 系统 SHALL 记录差异并提供调整建议，不强制替换现有贴图

---

### 需求 8：实现验证与集成测试

**用户故事：** 作为开发者，我希望能够在 Unity Play Mode 中验证所有 Boost Trail 特效正确运行，以便确认 Phase 2 实现完整。

#### 验收标准

1. WHEN 在 Play Mode 中触发 Boost 状态 THEN 系统 SHALL 同时显示以下效果：
   - TrailRenderer 主拖尾（橙黄 HDR，宽度曲线正确）
   - FlameTrail_R/B 粒子（紫色 HDR，按距离发射）
   - EmberTrail 粒子（品红 HDR，按距离发射）
   - EmberGlow 粒子（橙黄 HDR，Burst 光晕）
   - EmberSparks 一次性爆发（白色超亮 HDR）
   - 全屏闪光（0→0.7→0，0.3s）
   - Bloom 爆发（Intensity 0→3→恢复，0.4s）

2. WHEN 在 Play Mode 中退出 Boost 状态 THEN 系统 SHALL 正确停止所有粒子系统，TrailRenderer 在 3.5s 内自然消散

3. WHEN 飞船对象被回收到对象池后重新激活 THEN 系统 SHALL 确保没有残留的拖尾或粒子（ResetState 正确执行）

4. WHEN 所有特效运行时 THEN 系统 SHALL 确保 HDR 颜色值（>1.0）正确触发 Bloom 发光效果（需要 URP HDR + Bloom 配置正确）
