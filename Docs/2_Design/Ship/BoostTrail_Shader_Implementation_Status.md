## BoostTrail Shader 当前实现状态

### 文档角色

这份文档只负责记录 **主拖尾 / 材质 / Shader / 相关主链收口状态**。

它不是命名规范文档，也不是资产总表。

当前对应关系如下：

- **规范权威**：`Docs/Reference/ShipVFX_CanonicalSpec.md`
- **资产映射**：`Docs/Reference/ShipVFX_AssetRegistry.md`
- **迁移计划**：`Docs/Reference/ShipVFX_MigrationPlan.md`
- **玩家感知视角**：`Docs/Reference/Ship_VFX_Player_Perception_Reference.md`

---

### 当前结论

`BoostTrail` 这条主拖尾 Shader 线已经完成从“实验兼容双轨”向“现役主链单轨”收口的关键一步。

一句话概括当前状态：

- **现役主材质已固定**
- **运行时强度驱动已接通**
- **legacy 材质链已退役**
- **共享 Shader 内部的 legacy 分支代码仍可作为后续减枝目标**

同时，本轮规范化后，相关 owner 已经明确为：

- `BoostTrailPrefabCreator`：负责 `BoostTrailRoot.prefab` 的 standalone 结构
- `ShipPrefabRebuilder`：负责把 `BoostTrailRoot` 集成进 `Ship.prefab` 并接通 `ShipView._boostTrailView`
- `MaterialTextureLinker`：负责现役材质的精确路径回填
- `ShipBoostTrailSceneBinder`：负责 `_boostBloomVolume` 的 scene-only 绑定

---

### 一、当前现役主链

当前 `MainTrail` 的实际工作路径是：

- `BoostTrailView.cs`
  - 通过 `SetBoostIntensity(float value)` 把 `_BoostIntensity` 写进 `TrailRenderer`
- `mat_trail_main`
  - 作为 `MainTrail` 的现役唯一材质
- `TrailMainEffect.shader`
  - 以 disturbance 路径作为现役表现
  - `_UseLegacySlots = 0`
  - `_BaseMap = vfx_boost_techno_flame.png`

这意味着：

- 主拖尾不再依赖旧的 slot 贴图链
- `_BoostIntensity` 不只驱动机体能量层，也已经驱动主拖尾本体
- 当前 Boost 主读感的重点是：`MainTrail + FlameTrail_R/B + FlameCore + EmberTrail + EmberSparks + Halo + Bloom`

---

### 二、第三批已完成的收口

#### 1. 删除 `EmberGlow`

已从现役链中移除：

- `BoostTrailView.cs` 中的 `_emberGlow` 字段与启停 / Reset 逻辑
- `BoostTrailPrefabCreator.cs` 中的 `EmberGlow` 创建与接线
- `BoostTrailRoot.prefab` 中对应的子节点
- 现役参考文档与验证清单中的相关描述

设计意图：

- 去掉与 `Halo / Bloom / EmberSparks / FlameCore` 在起手读感上职责重叠的一层
- 保留 Boost 起手最关键的“点火成功”主读感，不让结构继续膨胀

#### 2. 退役 `mat_trail_main_effect` legacy 材质链

已移除：

- `mat_trail_main_effect.mat`
- `trail_main_spritesheet.png`
- `trail_second_spritesheet.png`
- `trail_edge_glow.png`
- `trail_color_lut.png`
- `MaterialTextureLinker.cs` 中对这套资源的自动接线逻辑
- `BoostTrailPrefabCreator.cs` 中对 `mat_trail_main_effect` 的 fallback

设计意图：

- 让 `MainTrail` 的材质入口只剩现役主材质 `mat_trail_main`
- 避免未来再被旧 slot 路径误导，或者在 prefab 重建时重新回退到历史材质链

---

### 三、当前工程化保护状态

`MaterialTextureLinker` 现在只维护现役资源：

- `mat_boost_energy_layer2`
- `mat_boost_energy_layer3`
- `mat_flame_trail`
- `mat_ember_trail`
- `mat_ember_sparks`
- `mat_trail_main`

并且当前规范化后的预期保护行为是：

- 优先从 `Assets/_Art/VFX/BoostTrail/Textures/` **精确路径**取图
- 不再通过全项目模糊搜索回填同名纹理
- `mat_trail_main` 被强制保持在 `_UseLegacySlots = 0`
- `mat_trail_main` 的 `_BaseMap` 固定为 `vfx_boost_techno_flame.png`

这意味着 prefab 重建与材质回填流程现在面对的是更单纯的现役资源集合，而不是“主链 + 兼容链”并存的双轨状态。

---

### 四、当前还保留的后续工作

当前**还没有删除**的是 `TrailMainEffect.shader` 内部那段 legacy 兼容代码本身，包括：

- `_UseLegacySlots`
- `_Slot0 ~ _Slot3`
- `ShadeLegacy()` 等旧分支逻辑

之所以本批先不动它，是因为：

- 这份 Shader 仍然是现役 `mat_trail_main` 使用的共享 Shader
- 即使删的是 dead branch，改坏 Shader 也会直接炸到当前主拖尾
- 相比之下，先删除 legacy 材质和输入资源，收益 / 风险比更高

因此下一批如果继续清理，建议目标应是：

- 在确认 Play Mode 观感与 prefab 重建完全稳定后
- 再对 `TrailMainEffect.shader` 的 legacy 分支做代码级减枝

---

### 五、当前版本判断

当前 `BoostTrail` 已经不再处于“要不要继续保留实验兼容材质”的阶段，而是进入：

**现役主链已明确，剩余工作主要是内部代码减枝与风格细收。**

从工程维护角度看，这一版最大的价值不是再加新层，而是把当前 Boost 主拖尾彻底稳定成：

- 结构清晰
- 工具不会误回填旧链
- prefab 重建结果可预期
- 文档与现状一致

---

### 六、与规范层的关系

如果后续要讨论：

- `FlameTrail_B` 为什么语义上表示左侧
- `Boost_16` / `Movement_3` 这种物理名应该如何解释
- `Ship.prefab` / `BoostTrailRoot.prefab` / `SampleScene` 的 owner 分工
- 哪些对象现在只能 alias、不能物理 rename

请不要在这份文档里找结论，统一回到：

- `ShipVFX_CanonicalSpec.md`
- `ShipVFX_AssetRegistry.md`
- `ShipVFX_MigrationPlan.md`
