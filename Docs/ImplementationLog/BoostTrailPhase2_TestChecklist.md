# Boost Trail VFX — 当前现役验证清单

**Date**: 2026-03-15  
**Scope**: BoostTrail 第三批清理后的现役链路验证  
**Tester**: Developer

---

## Pre-Test Setup

在进入 Play Mode 前，先完成以下步骤：

1. **Import textures**: 打开 Unity Editor，等待 `Assets/_Art/VFX/BoostTrail/Textures/` 与 `Assets/_Art/Ship/Glitch/` 下现役贴图完成导入。
2. **Link textures**: 运行 `ProjectArk > Ship > VFX > Authority > Link Active BoostTrail Material Textures`，确认弹窗或 Console 中的成功赋值数为 **10**。
3. **Create Prefab**: 运行 `ProjectArk > Ship > VFX > Authority > Rebuild BoostTrailRoot Prefab`，确认 `Assets/_Prefabs/VFX/BoostTrailRoot.prefab` 已按当前结构重建。
4. **Wire ShipView**: 在 `ShipView` Inspector 中确认 `_boostTrailView`、`_boostLiquidSprite`（`Boost_16`）、`_normalLiquidSprite`（`Movement_3`）已正确接线。
5. **Wire Bloom**: 确认场景里的本地 Volume 已绑定到 `BoostTrailView._boostBloomVolume`。

---

## Acceptance Criteria Checklist

### AC-1: Boost 起手时现役层全部响应
- [ ] 进入 Play Mode
- [ ] 触发 Boost
- [ ] **Verify**: `MainTrail` 开始发射，尾迹立刻可见
- [ ] **Verify**: `FlameTrail_R` 与 `FlameTrail_B` 开始持续喷射紫色 HDR 粒子
- [ ] **Verify**: `FlameCore` 在船尾根部持续输出短命核心火焰
- [ ] **Verify**: `EmberTrail` 开始输出更散的余烬轨迹
- [ ] **Verify**: `EmberSparks` 触发一次性的白热火星 burst
- [ ] **Expected**: 当前现役 Boost 主链在 1 帧内全部进入工作状态

### AC-2: Boost 结束时现役层平滑收尾
- [ ] 在 Boost 中松开输入
- [ ] **Verify**: `MainTrail.emitting = false`，但尾迹按 `trailTime` 自然衰减
- [ ] **Verify**: `FlameTrail_R/B` 停止发射，已有粒子自然淡出
- [ ] **Verify**: `FlameCore` 停止发射
- [ ] **Verify**: `EmberTrail` 停止发射
- [ ] **Verify**: `EmberSparks` 不会被额外重触发
- [ ] **Expected**: 不出现硬切断或残留脏状态

### AC-3: MainTrail 使用现役材质链
- [ ] 打开 `mat_trail_main` 的 Inspector
- [ ] **Verify**: Shader 为 `ProjectArk/VFX/TrailMainEffect`
- [ ] **Verify**: `_UseLegacySlots = 0`
- [ ] **Verify**: `_BaseMap = vfx_boost_techno_flame.png`
- [ ] 进入 Play Mode 并触发 Boost
- [ ] **Verify**: 主拖尾表现来自当前 disturbance 路径，而不是旧的 slot 贴图路线

### AC-4: Boost Noise Textures 已正确接线（Layer 2/3）
- [ ] 打开 `mat_boost_energy_layer2`
- [ ] **Verify**: `_Tex0 = boost_noise_main.png`
- [ ] **Verify**: `_Tex1 = boost_noise_distort.png`
- [ ] **Verify**: `_Tex2 = boost_noise_layer3.png`
- [ ] **Verify**: `_Tex3 = boost_noise_layer4.png`
- [ ] 打开 `mat_boost_energy_layer3`
- [ ] **Verify**: `_Tex0 = boost_energy_noise_a.png`
- [ ] **Verify**: `_Tex1 = boost_energy_main.png`
- [ ] **Verify**: Play Mode 中能量层不是纯白贴片，而是有噪声流动感

### AC-5: Halo 与 Bloom 承担 Boost 起手主读感
- [ ] 触发 Boost
- [ ] **Verify**: `BoostActivationHalo` 在船身周围产生短促爆闪
- [ ] **Verify**: Bloom 强度在起手瞬间被顶高，然后在 0.4s 内回到基线
- [ ] **Verify**: 即使删掉 `EmberGlow`，Boost 起手依旧能清晰读到“点火成功”

### AC-6: Object Pool Reset 无状态泄漏
- [ ] 触发一次完整 Boost
- [ ] 调用 `BoostTrailView.ResetState()`（或模拟对象池回收）
- [ ] **Verify**: `MainTrail` 被清空且不再发射
- [ ] **Verify**: `FlameTrail_R/B`、`FlameCore`、`EmberTrail`、`EmberSparks` 全部停止并清空
- [ ] **Verify**: `_BoostIntensity = 0`
- [ ] **Verify**: `BoostActivationHalo` 被隐藏并恢复初始缩放
- [ ] **Verify**: Bloom Volume `weight = 0`

### AC-7: ShipView 的 Boost 联动未退化
- [ ] 进入 Boost
- [ ] **Verify**: 液态层贴图从 `Movement_3` 切到 `Boost_16`
- [ ] **Verify**: 尾喷 entry pulse 与循环 pulse 仍然正常
- [ ] 退出 Boost
- [ ] **Verify**: 液态层与尾喷回到普通状态

---

## Notes

- 当前现役链路已经**不再包含** `EmberGlow`。
- 当前现役主拖尾已经**不再依赖** `mat_trail_main_effect` 以及 `trail_main_spritesheet / trail_second_spritesheet / trail_edge_glow / trail_color_lut` 这组 legacy 资源。
- 如果后续还要继续做第四批，优先考虑的是 `TrailMainEffect.shader` 内部 legacy 分支的代码减枝，而不是回滚已删除的材质链。
