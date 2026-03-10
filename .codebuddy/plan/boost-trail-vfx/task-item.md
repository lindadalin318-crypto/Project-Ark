# 实施计划：Boost Trail VFX 全复刻

## 执行顺序说明

按依赖关系从底层到上层：资产 → URP 配置 → 粒子/Trail → Shader Graph → 控制脚本 → Prefab 组装 → ShipView 联动

---

- [ ] 1. 导入资产并建立目录结构
   - 在 `Assets/_Art/VFX/BoostTrail/` 下创建子目录：`Textures/`、`Materials/`、`Shaders/`
   - 将以下纹理从 GG 提取目录复制到 `Textures/`：`vfx_boost_techno_flame.png`、`vfx_ember_trail.png`、`vfx_ember_sparks.png`
   - 将 `Boost_16` 对应 Sprite 导入项目（用于 Liquid 贴图切换）
   - _需求：7.2、7.3_

- [ ] 2. 配置 URP 渲染管线（HDR + Bloom）
   - 确认 URP Asset 开启 HDR，Camera 开启 Allow HDR
   - 在场景中创建 Global Volume，添加 Bloom 组件（Threshold=0.8，Intensity=1.5，Scatter=0.7）
   - 创建独立 Local Volume（用于 Boost 激活时的 Bloom 爆发叠加，初始 weight=0）
   - _需求：8.1、8.2、8.3、8.4_

- [ ] 3. 创建粒子系统材质
   - 创建 `mat_flame_trail`：URP Particles/Unlit Additive，BaseMap=`vfx_boost_techno_flame.png`
   - 创建 `mat_ember_trail`：URP Particles/Unlit Additive，BaseMap=`vfx_ember_trail.png`
   - 创建 `mat_ember_sparks`：URP Particles/Unlit Additive，BaseMap=`vfx_ember_sparks.png`
   - 创建 `mat_trail_main`：URP Particles/Unlit Additive，BaseMap=`vfx_boost_techno_flame.png`，颜色 HDR `(2.0, 1.1, 0.24)`
   - _需求：7.4_

- [ ] 4. 配置 TrailRenderer 主拖尾
   - 在 Prefab 中添加 `MainTrail` 子节点，挂载 TrailRenderer 组件
   - 设置参数：Time=3.5s，材质=`mat_trail_main`，宽度曲线（头部 0.3 → 40% 处 1.0），widthMultiplier=3.0
   - 初始状态 emitting=false
   - _需求：1.1、1.2、1.3、1.4、1.5_

- [ ] 5. 配置火焰与余烬粒子系统
   - 5.1 配置 `FlameTrail_R` 和 `FlameTrail_B`：rateOverDistance=15，StartLifetime=0.4s，StartSize=0.4，StartSpeed=10，材质=`mat_flame_trail`，颜色 HDR `(5.44, 0.42, 6.06)`，初始停止
   - 5.2 配置 `FlameCore`：Burst 模式，StartLifetime=0.07~0.08s，材质=`mat_flame_trail`，初始停止
   - 5.3 配置 `EmberTrail`：rateOverDistance=2，StartLifetime=0.35s，StartSize=0.7，StartSpeed=0，材质=`mat_ember_trail`，颜色 HDR `(2.0, 0, 1.08)`，初始停止
   - 5.4 配置 `EmberSparks`：Loop=false，StartSpeed=50，StartSize=0.3，StartLifetime=0.2s，材质=`mat_ember_sparks`，颜色 HDR `(3.73, 3.73, 3.73)`，初始停止
   - _需求：2.1~2.5、3.1~3.5_

- [ ] 6. 创建 Boost 能量层 Shader Graph（Layer 2 + Layer 3）
   - 6.1 创建 `BoostEnergyLayer2.shadergraph`：节点链 `Gradient Noise → UV Distortion → Sample Texture 2D × 4 → Lerp × 3 → Smoothstep`，暴露 `_BoostIntensity`（Float，默认 0）和 4 张纹理属性
   - 6.2 创建 `BoostEnergyLayer3.shadergraph`：节点链 `Vertex Color(w) → Lerp → UV Scale(×2-0.5) → Sample Texture 2D × 2 → Step(0.01)`，暴露 `_BoostIntensity`（Float，默认 0）
   - 6.3 为 Layer 2/3 各创建对应材质（`mat_boost_energy_layer2`、`mat_boost_energy_layer3`），放入 `Materials/`
   - _需求：9.1、9.2、9.6_

- [ ] 7. 创建全局 Boost 能量场 Shader Graph（Layer 4）
   - 创建 `BoostEnergyField.shadergraph`：核心节点链 `World Position → Transform(4×4矩阵) → Gradient Noise × 4 → Sample Texture 2D(LUT) → Step(0.01)`，暴露 `_BoostIntensity`（Float，默认 0）和 LUT 纹理属性
   - 降级处理：LUT 纹理属性设为可选，未赋值时节点链退化为纯程序化 Gradient Noise
   - 创建对应材质 `mat_boost_energy_field`，放入 `Materials/`
   - _需求：10.1、10.2、10.6、10.7_

- [ ] 8. 编写 `BoostTrailView.cs` 控制脚本
   - 通过 `[SerializeField]` 引用所有子组件（TrailRenderer、5个 ParticleSystem、2个 SpriteRenderer、MeshRenderer、Local Volume）
   - 实现 `OnBoostStart()`：激活 Trail + 所有粒子，触发 EmberSparks，用 PrimeTween 驱动全屏闪光（alpha 0→0.7→0，0.3s）和 Local Volume Bloom（Intensity 0→3.0→恢复，0.4s），用 Material Property Block 驱动 Layer 2/3/4 的 `_BoostIntensity` 0→1（0.3s EaseInQuad）
   - 实现 `OnBoostEnd()`：停止 Trail 发射，停止粒子（EmberSparks 除外），驱动 `_BoostIntensity` 1→0（0.5s EaseOutQuad），动画结束后禁用 BoostEnergyField MeshRenderer
   - 实现 `ResetState()`：TrailRenderer.Clear()，Stop+Clear 所有粒子，全屏闪光 alpha=0，Volume weight=0，`_BoostIntensity`=0，禁用 MeshRenderer
   - _需求：6.1~6.5_

- [ ] 9. 组装 BoostTrailRoot Prefab 并配置全屏闪光 UI
   - 按需求 7.1 的层级结构组装 Prefab，挂载 `BoostTrailView.cs`，在 Inspector 中连线所有 `[SerializeField]` 引用
   - 在 Canvas（Overlay）中创建全屏白色 Image（`BoostFlashImage`），设置 Additive 混合，初始 alpha=0，将引用传入 `BoostTrailView`
   - 将 Prefab 保存至 `Assets/_Prefabs/VFX/BoostTrailRoot.prefab`
   - _需求：4.1~4.5、7.1、7.2_

- [ ] 10. 联动 ShipView：Liquid 贴图切换 + BoostTrailView 调用
   - 在 `ShipView.cs` 的 Boost 进入回调中：切换 `_liquidRenderer.sprite` 为 `Boost_16` Sprite，PrimeTween 提升亮度至 1.5 倍，调用 `BoostTrailView.OnBoostStart()`
   - 在 `ShipView.cs` 的 Boost 退出回调中：切换 `_liquidRenderer.sprite` 回 `Movement_3` Sprite，0.3s 内恢复亮度，调用 `BoostTrailView.OnBoostEnd()`
   - 在对象池回收逻辑中调用 `BoostTrailView.ResetState()` 并重置 `_liquidRenderer.sprite`
   - _需求：5.1~5.5、6.5_
