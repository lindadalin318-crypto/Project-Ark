# 需求文档：Boost Trail VFX 全复刻

## 引言

本功能目标是在 Project Ark 中**完整复刻** GalacticGlitch（GG）飞船在 Boost 状态下的全套视觉特效。

研究基础：已完成对 GG 的 1.rdc / 3.rdc / 4.rdc / 5.rdc 四个 RenderDoc 帧捕获的完整逆向分析，所有关键纹理已提取，Shader 逻辑已反汇编，参数已记录于 `Docs/Reference/GalacticGlitch_BoostTrail_VFX_Plan.md`。

**GG Boost Trail 完整架构（7层）**：
1. 背景视差层（6层叠加，与本功能无关，跳过）
2. 飞船本体（Solid + Liquid + Highlight 三层贴图，Liquid 层有液体流动动画）
3. Boost 能量层 1（程序化 Perlin 噪声，4张纹理，3个 CBuffer）
4. Boost 能量层 2（双重噪声叠加，5张纹理，顶点颜色驱动）
5. 全局 Boost 能量场（世界空间，4层梯度噪声，4×4矩阵变换）
6. Trail 粒子系统（Trail 粒子段 + 颜色混合 + Trail 主特效三个子层）
7. 后处理（全屏 Bloom 爆发 + 全屏闪光）

**实现策略**：
- Layer 2（Boost 噪声层 1）：通过 **Shader Graph** 复刻（Perlin 噪声 UV 扰动 + 4层纹理叠加 + smoothstep alpha），SPIR-V 反汇编已提供完整逻辑，按图索骥连线即可
- Layer 3（Boost 噪声层 2）：通过 **Shader Graph** 复刻（顶点颜色驱动 + UV×2 平铺 + 二值化 alpha Step 节点）
- Layer 4（全局能量场）：**本期纳入，用 Shader Graph 实现**（World Position Node + 4层梯度噪声 + 4×4矩阵变换 + 5.1MB LUT 纹理），复杂度最高，SPIR-V bound=1971，预计工作量 2-3 天，作为最后实现
- Layer 6（Trail 粒子）：直接使用 GG 提取的纹理（`vfx_boost_techno_flame.png` / `vfx_ember_trail.png` / `vfx_ember_sparks.png`），配合 URP Particles/Unlit Additive 材质
- Layer 7（后处理）：URP Volume + CanvasGroup，纯代码实现
- 飞船本体 Liquid 动画：通过 **切换 Sprite 贴图**（`Movement_3` → `Boost_16`）实现，复用现有 `ShipView` 架构，无需 Shader Graph

---

## 需求

### 需求 1：TrailRenderer 主拖尾

**用户故事：** 作为玩家，我希望飞船在 Boost 状态下身后有一条发光的火焰拖尾，以便在视觉上感受到高速移动的爽快感。

#### 验收标准

1. WHEN 飞船进入 Boost 状态 THEN 系统 SHALL 立即激活 TrailRenderer 开始发射拖尾
2. WHEN 飞船退出 Boost 状态 THEN 系统 SHALL 停止发射拖尾，已有拖尾在 3.5 秒内自然消散
3. WHEN TrailRenderer 激活时 THEN 系统 SHALL 使用 URP Particles/Unlit Additive 材质，BaseMap 为 `vfx_boost_techno_flame.png`，颜色为 HDR `(2.0, 1.1, 0.24)`（橙黄）
4. WHEN 渲染拖尾时 THEN 系统 SHALL 使用宽度曲线（头部 0.3 → 40% 处 1.0），widthMultiplier = 3.0
5. WHEN 飞船对象被回收到对象池 THEN 系统 SHALL 调用 `TrailRenderer.Clear()` 并停止发射，不留残影

---

### 需求 2：火焰粒子系统（FlameTrail）

**用户故事：** 作为玩家，我希望飞船 Boost 时两侧有连续的火焰粒子拖尾，以便增强速度感和方向感。

#### 验收标准

1. WHEN 飞船进入 Boost 状态 THEN 系统 SHALL 激活左右两个 FlameTrail 粒子系统（FlameTrail_R / FlameTrail_B）
2. WHEN FlameTrail 粒子系统运行时 THEN 系统 SHALL 使用 `rateOverDistance = 15`（每移动 1 米发射 15 个粒子），形成连续拖尾
3. WHEN 发射火焰粒子时 THEN 系统 SHALL 使用参数：StartLifetime=0.4s，StartSize=0.4，StartSpeed=10，材质为 URP Particles/Unlit Additive + `vfx_boost_techno_flame.png`，颜色 HDR `(5.44, 0.42, 6.06)`（紫色）
4. WHEN 飞船进入 Boost 状态 THEN 系统 SHALL 同时激活 FlameCore 粒子系统（Burst 模式，StartLifetime=0.07~0.08s，极短生命）
5. WHEN 飞船退出 Boost 状态 THEN 系统 SHALL 停止所有 FlameTrail 和 FlameCore 粒子系统

---

### 需求 3：余烬粒子系统（Ember）

**用户故事：** 作为玩家，我希望飞船 Boost 时有余烬和火花粒子，以便增加特效的层次感和细节。

#### 验收标准

1. WHEN 飞船进入 Boost 状态 THEN 系统 SHALL 激活 EmberTrail 粒子系统（`rateOverDistance=2`，StartLifetime=0.35s，StartSize=0.7，StartSpeed=0）
2. WHEN EmberTrail 运行时 THEN 系统 SHALL 使用材质 URP Particles/Unlit Additive + `vfx_ember_trail.png`，颜色 HDR `(2.0, 0, 1.08)`（品红）
3. WHEN 飞船**首次**进入 Boost 状态 THEN 系统 SHALL 触发一次性 EmberSparks 爆发（StartSpeed=50，StartSize=0.3，StartLifetime=0.2s，Loop=false）
4. WHEN EmberSparks 播放时 THEN 系统 SHALL 使用材质 URP Particles/Unlit Additive + `vfx_ember_sparks.png`，颜色 HDR `(3.73, 3.73, 3.73)`（白色超亮）
5. WHEN 飞船退出 Boost 状态 THEN 系统 SHALL 停止 EmberTrail，EmberSparks 自然播放完毕不强制停止

---

### 需求 4：Boost 激活瞬间特效（全屏闪光 + Bloom 爆发）

**用户故事：** 作为玩家，我希望按下 Boost 的瞬间有强烈的视觉冲击感，以便感受到引擎点火的爆发力。

#### 验收标准

1. WHEN 飞船进入 Boost 状态 THEN 系统 SHALL 触发全屏闪光：Canvas Overlay 上的白色半透明 Image，alpha 从 0 → 0.7 → 0，总时长 0.3s，使用 PrimeTween EaseOutQuad
2. WHEN 飞船进入 Boost 状态 THEN 系统 SHALL 同时触发 URP Volume Bloom 爆发：Bloom Intensity 从当前值 → 3.0 → 恢复，总时长 0.4s，使用 PrimeTween EaseOutQuad
3. WHEN 全屏闪光播放时 THEN 系统 SHALL 使用 Additive 混合模式，不遮挡游戏画面，玩家仍能看到场景
4. WHEN 飞船对象被回收到对象池 THEN 系统 SHALL 立即将全屏闪光 alpha 重置为 0，Volume weight 重置为 0
5. IF 飞船在全屏闪光播放期间再次触发 Boost THEN 系统 SHALL 重新开始闪光动画（不叠加）

---

### 需求 5：飞船本体 Liquid 贴图切换

**用户故事：** 作为玩家，我希望飞船本体在 Boost 状态下有不同的液体层视觉，以便从飞船外观上感受到 Boost 状态的变化。

> **实现说明**：GG 逆向分析确认，Liquid 动画通过**切换 Sprite 贴图**实现（`Movement_3` → `Boost_16`），而非 Shader Graph。`ShipView.cs` 已有完整的多层 SpriteRenderer 架构和 Boost 联动，本需求在此基础上扩展。

#### 验收标准

1. WHEN 飞船进入 Boost 状态 THEN 系统 SHALL 将 `_liquidRenderer.sprite` 切换为 `Boost_16` 对应的 Sprite（来自 GG 逆向提取的贴图）
2. WHEN 飞船退出 Boost 状态 THEN 系统 SHALL 将 `_liquidRenderer.sprite` 切换回 Normal 状态的 `Movement_3` 对应 Sprite
3. WHEN 切换贴图时 THEN 系统 SHALL 同时用 PrimeTween 将 `_liquidRenderer` 亮度提升至 1.5 倍（Boost 进入时），退出时 0.3s 内恢复（复用现有 ShipView 逻辑）
4. WHEN 飞船对象被回收到对象池 THEN 系统 SHALL 将 `_liquidRenderer.sprite` 重置为 Normal 状态贴图
5. IF `Boost_16` 贴图尚未导入项目 THEN 系统 SHALL 退化为仅保留亮度变化效果，不阻塞其他需求

---

### 需求 6：BoostTrailView 控制脚本

**用户故事：** 作为开发者，我希望有一个统一的控制脚本管理所有 Boost Trail 特效的生命周期，以便与飞船状态机正确联动，并支持对象池复用。

#### 验收标准

1. WHEN `BoostTrailView.OnBoostStart()` 被调用 THEN 系统 SHALL 同时激活 TrailRenderer、FlameTrail_R/B、FlameCore、EmberTrail，并触发 EmberSparks 和全屏闪光
2. WHEN `BoostTrailView.OnBoostEnd()` 被调用 THEN 系统 SHALL 停止 TrailRenderer 发射、停止所有粒子系统（EmberSparks 除外）
3. WHEN `BoostTrailView.ResetState()` 被调用（对象池回收） THEN 系统 SHALL 完整重置所有状态：Clear TrailRenderer、Stop+Clear 所有粒子系统、重置全屏闪光 alpha=0、重置 Volume weight=0
4. WHEN `BoostTrailView` 初始化时 THEN 系统 SHALL 通过 `[SerializeField]` 引用所有子组件，不使用 `FindObjectOfType` 或 `GetComponentInChildren`（运行时查找）
5. WHEN 飞船状态机切换到 Boost 状态 THEN 系统 SHALL 通过 `ShipView` 或事件总线调用 `BoostTrailView`，不直接耦合状态机

---

### 需求 7：Prefab 结构与资产组织

**用户故事：** 作为开发者，我希望 Boost Trail 特效有清晰的 Prefab 层级结构和资产目录，以便后续维护和扩展颜色主题。

#### 验收标准

1. WHEN 创建 Boost Trail Prefab 时 THEN 系统 SHALL 使用以下层级结构：
   ```
   BoostTrailRoot
   ├── [TrailRenderer] MainTrail
   ├── [PS] FlameTrail_R
   ├── [PS] FlameTrail_B
   ├── [PS] FlameCore
   ├── [PS] EmberTrail
   ├── [PS] EmberSparks
   ├── [SpriteRenderer] BoostEnergyLayer2  ← Layer 2 Shader Graph
   ├── [SpriteRenderer] BoostEnergyLayer3  ← Layer 3 Shader Graph
   └── [MeshRenderer] BoostEnergyField     ← Layer 4 Shader Graph（全局能量场）
   ```
2. WHEN 组织资产时 THEN 系统 SHALL 将纹理放置于 `Assets/_Art/VFX/BoostTrail/Textures/`，材质放置于 `Assets/_Art/VFX/BoostTrail/Materials/`，Shader Graph 放置于 `Assets/_Art/VFX/BoostTrail/Shaders/`，Prefab 放置于 `Assets/_Prefabs/VFX/`
3. WHEN 导入 GG 提取的纹理时 THEN 系统 SHALL 将以下纹理复制到项目：`vfx_boost_techno_flame.png`、`vfx_ember_trail.png`、`vfx_ember_sparks.png`
4. WHEN 创建材质时 THEN 系统 SHALL 为每种粒子类型创建独立材质（mat_flame_trail、mat_ember_trail、mat_ember_sparks），使用 URP Particles/Unlit Additive Shader

---

### 需求 9：Boost 能量层 Shader Graph（Layer 2 + Layer 3）

**用户故事：** 作为玩家，我希望飞船在 Boost 状态下本体表面有能量流动的视觉效果，以便感受到飞船内部能量涌动的质感。

> **实现说明**：基于 RenderDoc SPIR-V 反汇编结果（`eid_1050/1181` 和 `eid_1252`），GG 的 Boost 能量层使用程序化噪声 Shader。我们已知完整逻辑，用 Shader Graph 按图索骥复刻。

#### 验收标准

1. WHEN 创建 Layer 2 Shader Graph 时 THEN 系统 SHALL 包含以下节点链：`Gradient Noise → UV Distortion → Sample Texture 2D × 4 → Lerp × 3 → Smoothstep`，对应 SPIR-V 中的 Perlin 噪声（289.0 常数）+ 4层纹理叠加逻辑
2. WHEN 创建 Layer 3 Shader Graph 时 THEN 系统 SHALL 包含以下节点链：`Vertex Color（w分量）→ Lerp → UV Scale（×2 - 0.5）→ Sample Texture 2D × 2 → Step（0.01）`，对应 SPIR-V 中的顶点颜色驱动 + 二值化 alpha 逻辑
3. WHEN 飞船进入 Boost 状态 THEN 系统 SHALL 通过 Material Property Block 将 Layer 2 Shader 的 `_BoostIntensity` 参数从 0 → 1（0.3s PrimeTween EaseInQuad）
4. WHEN 飞船退出 Boost 状态 THEN 系统 SHALL 将 `_BoostIntensity` 从 1 → 0（0.5s PrimeTween EaseOutQuad）
5. WHEN 飞船对象被回收到对象池 THEN 系统 SHALL 立即将 `_BoostIntensity` 重置为 0
6. IF Shader Graph 编译失败或平台不支持 THEN 系统 SHALL 退化为仅显示基础飞船贴图，不影响其他需求

---

### 需求 10：全局 Boost 能量场 Shader Graph（Layer 4）

**用户故事：** 作为玩家，我希望飞船在 Boost 状态下周围有世界空间的能量场特效，以便感受到飞船在空间中爆发的宏观能量感。

> **实现说明**：基于 RenderDoc SPIR-V 反汇编结果（`eid_1785/1964`，bound=1971），这是所有层中最复杂的 Shader。使用世界空间坐标 + 4层梯度噪声 + 4×4矩阵变换 + 5.1MB 预计算 LUT 纹理。在 Project Ark 中用一个跟随飞船的 Quad Mesh + Shader Graph（World Position 节点）实现。

#### 验收标准

1. WHEN 创建 Layer 4 Shader Graph 时 THEN 系统 SHALL 包含以下核心节点：`World Position → Transform（4×4矩阵）→ Gradient Noise × 4（4层叠加）→ Sample Texture 2D（5.1MB LUT）→ Step（0.01，二值化 alpha）`
2. WHEN Layer 4 Shader Graph 运行时 THEN 系统 SHALL 使用世界空间坐标（World Position Node）而非 UV 坐标，使特效跟随世界位置而非飞船 UV
3. WHEN 飞船进入 Boost 状态 THEN 系统 SHALL 通过 Material Property Block 将 `_BoostIntensity` 从 0 → 1（0.3s PrimeTween EaseInQuad），同时激活 BoostEnergyField MeshRenderer
4. WHEN 飞船退出 Boost 状态 THEN 系统 SHALL 将 `_BoostIntensity` 从 1 → 0（0.5s PrimeTween EaseOutQuad），动画结束后禁用 MeshRenderer
5. WHEN 飞船对象被回收到对象池 THEN 系统 SHALL 立即将 `_BoostIntensity` 重置为 0 并禁用 MeshRenderer
6. IF 5.1MB LUT 纹理尚未导入项目 THEN 系统 SHALL 退化为使用程序化 Gradient Noise 替代，不阻塞其他需求
7. IF Shader Graph 编译失败 THEN 系统 SHALL 直接禁用 BoostEnergyField MeshRenderer，不影响其他层

---

### 需求 8：URP 渲染配置

**用户故事：** 作为开发者，我希望 URP 渲染管线正确配置 HDR 和 Bloom，以便 Boost Trail 的 HDR 颜色能正确产生发光效果。

#### 验收标准

1. WHEN 运行游戏时 THEN 系统 SHALL 确保 Camera 开启 Allow HDR
2. WHEN 运行游戏时 THEN 系统 SHALL 确保 URP Asset 开启 HDR
3. WHEN 运行游戏时 THEN 系统 SHALL 确保场景中有 Global Volume，包含 Bloom（Threshold=0.8，Intensity=1.5，Scatter=0.7）
4. WHEN Boost 激活时 THEN 系统 SHALL 通过独立的 Local Volume（weight 动画）叠加 Bloom 爆发，不影响全局 Bloom 基础值
