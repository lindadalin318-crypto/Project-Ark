# 实施计划：Boost Trail VFX Phase 2 — 缺失功能补全

## 执行顺序说明

按依赖关系从底层到上层：纹理资产导入 → Shader 实现 → 粒子系统补全 → 控制脚本更新 → Editor 脚本更新 → 材质连线 → 飞船贴图集成

---

- [ ] 1. 编写纹理批量导入脚本（PowerShell）
   - 编写 `Tools/CopyGGTextures.ps1`，将 GGrenderdoc 中所有目标纹理批量复制并重命名到项目目录
   - eid_1598 → `Assets/_Art/VFX/BoostTrail/Textures/`（trail_main_spritesheet / trail_second_spritesheet / trail_edge_glow / trail_color_lut）
   - eid_1076 → `Assets/_Art/VFX/BoostTrail/Textures/`（boost_noise_main / boost_noise_distort / boost_noise_layer3 / boost_noise_layer4）
   - eid_1126 → `Assets/_Art/VFX/BoostTrail/Textures/`（boost_energy_noise_a / boost_energy_main）
   - eid_1964 → `Assets/_Art/VFX/BoostTrail/Textures/`（boost_field_main / boost_field_aux）
   - eid_877 → `Assets/_Art/Ship/Glitch/`（ship_solid_gg / ship_liquid_boost / ship_highlight_gg）
   - _需求：1.1、1.2、1.3、1.4、1.5_

- [ ] 2. 实现 Trail 主特效 Shader（uniforms141 精确复刻）
   - 创建 `Assets/_Art/VFX/BoostTrail/Shaders/TrailMainEffect.shader`（URP HLSL）
   - 实现双层 Sprite Sheet 采样：`_Child0`（帧数 XY）控制 slot0，`_Child1`（帧数 XY）控制 slot1
   - 实现边缘光晕：基于 `_Child5.xy` 偏移 + `_Child5.zw` 缩放 + `_Child4.w` 椭圆参数的径向渐变叠加
   - 实现亮度增强：`_Child3 > 0` 时颜色 ×8（HDR 超亮发光），颜色混合权重由 `_Child2.xyz` 控制
   - 最终输出 `output.w = 1.0`（完全不透明，与 GG SPIR-V 一致）
   - 创建对应材质 `mat_trail_main_effect.mat`，引用新 Shader，4 张纹理槽位留空（等待步骤 6 连线）
   - _需求：2.1、2.2、2.3、2.4_

- [ ] 3. 验证并修正 BoostEnergyLayer2 Shader（Perlin 噪声常数校验）
   - 读取 `BoostEnergyLayer2.shader`，确认 Perlin 噪声实现使用 289.0 常数（与 GG SPIR-V 一致）
   - 如不一致则修正 `mod289` 函数和 `permute` 函数中的常数
   - 确认 4 层纹理叠加逻辑和 smoothstep alpha 裁剪与 SPIR-V 反汇编一致
   - _需求：3.4_

- [ ] 4. 补全 EmberGlow 粒子系统（BoostTrailView.cs 更新）
   - 在 `BoostTrailView.cs` 中添加 `[SerializeField] private ParticleSystem _emberGlow` 字段
   - 在 `OnBoostStart()` 中添加 `_emberGlow.Play()`（与 EmberTrail 同步）
   - 在 `OnBoostEnd()` 中添加 `_emberGlow.Stop(false, ParticleSystemStopBehavior.StopEmitting)`
   - 在 `ResetState()` 中添加 `_emberGlow.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear)`
   - _需求：4.2、4.3、4.4、4.5_

- [ ] 5. 更新 BoostTrailPrefabCreator.cs（添加 EmberGlow 节点 + 纹理连线指南）
   - 在 Prefab 层级创建逻辑中添加 `EmberGlow` 子节点（ParticleSystem）
   - 配置 EmberGlow 参数：StartLifetime=0.12s，StartSize=1.0，StartSpeed=0，Burst 模式，材质=`mat_ember_trail`，颜色 HDR `(2.0, 1.29, 0)`
   - 通过 `SerializedObject` 将 `_emberGlow` 字段连线到新创建的 EmberGlow 粒子系统
   - 更新完成对话框文本，添加纹理连线指南（列出所有材质的 _BaseMap / _Tex0~3 / _LUTTex 连线步骤）
   - _需求：5.1、5.2、6.1、6.2、6.3_

- [ ] 6. 编写材质纹理批量连线 Editor 脚本（MaterialTextureLinker.cs）
   - 创建 `Assets/Editor/VFX/MaterialTextureLinker.cs`，菜单项 `ProjectArk > VFX > Link Material Textures`
   - 自动将 `boost_noise_main/distort/layer3/layer4` 连线到 `mat_boost_energy_layer2` 的 `_Tex0~3`
   - 自动将 `boost_energy_noise_a / boost_energy_main` 连线到 `mat_boost_energy_layer3` 的 `_Tex0~1`
   - 自动将 `boost_field_main` 连线到 `mat_boost_energy_field` 的 `_LUTTex`，并设置 `_UseLUT=1`
   - 自动将 `trail_main_spritesheet / trail_second_spritesheet / trail_edge_glow / trail_color_lut` 连线到 `mat_trail_main_effect` 的 4 个纹理槽位
   - 自动将 `vfx_boost_techno_flame / vfx_ember_trail / vfx_ember_sparks` 连线到对应粒子材质的 `_BaseMap`
   - _需求：3.1、3.2、3.3、5.3_

- [ ] 7. 飞船本体贴图集成（ShipView Inspector 连线指南）
   - 确认 `ship_solid_gg.png`、`ship_liquid_boost.png`、`ship_highlight_gg.png` 已正确导入为 Sprite（Sprite Mode: Single，PPU: 100）
   - 检查 `ship_liquid_boost.png` 尺寸是否与现有 `Boost_16.png` 一致，记录差异
   - 在 `ShipView.cs` 的 Inspector 连线指南中补充：将 `ship_liquid_boost.png` 作为 `_boostLiquidSprite` 的备选说明
   - _需求：7.1、7.2、7.3_

- [ ] 8. 集成验证：Play Mode 全流程测试清单
   - 编写 `Docs/ImplementationLog/BoostTrailPhase2_TestChecklist.md`，列出 8 条验收标准的逐项验证步骤
   - 验证项包括：所有粒子系统同步启停、EmberGlow Burst 光晕可见、Trail 主特效 Shader 双层 Sprite Sheet 动画、Bloom 爆发效果、对象池回收后无残留粒子
   - 追加 `Docs/ImplementationLog/ImplementationLog.md` 记录 Phase 2 实现日志
   - _需求：8.1、8.2、8.3、8.4_
