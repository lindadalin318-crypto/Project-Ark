# 需求文档：星图编织态交互体验 (Star Chart Weaving State IxD)

## 引言

GDD 中"交互规范：星图界面的微观聚焦"描述了星图编织态的**完成形态**，包括镜头运动、后处理效果、飞船形态变换、音频氛围等高度打磨的交互细节。

本文档基于**当前项目实际进度**，将 GDD 交互规范逐条拆解为 **🟢 现阶段实现** 与 **🔴 未来再做** 两部分，确保用最小投入获得可交互的编织态原型。

---

## 当前项目基础设施盘点

| 依赖 | 状态 | 说明 |
|------|------|------|
| URP 2D Renderer | ✅ 已有 | URP 管线已配置 |
| URP Volume (Post-Processing) | ✅ 已有 | DefaultVolumeProfile.asset 含 DepthOfField + Vignette |
| Shader Graph 包 | ✅ 已有 | com.unity.shadergraph 17.3.0 |
| New Input System | ✅ 已有 | Pause action 已绑定 TAB/ESC |
| UIManager 面板切换 | ✅ 已有 | TimeScale=0 暂停 + Fire 输入禁用 |
| StarChartPanel + 子视图 | ✅ 代码已完成 | 未在编辑器搭建 |
| DOTween / PrimeTween | ❌ 未安装 | 无 Tween 库 |
| Cinemachine | ❌ 未使用 | 当前摄像机为原生 Camera |
| AudioMixer | ❌ 未创建 | 无低通滤波能力 |
| 自定义飞船 Shader | ❌ 未创建 | 无全息线框材质 |
| 飞船模型/装甲动画 | ❌ 不存在 | 2D Sprite，无装甲展开概念 |

---

## GDD 交互规范逐条对照 & 阶段划分

### 📌 GDD 第 1 节：进入流程 (The Entry Sequence)

| # | GDD 原文功能 | 可行性分析 | 阶段划分 |
|---|-------------|-----------|---------|
| 1.1a | 镜头推拉 (Dolly In) — Camera Height 15→4 | 2D 正交相机可通过 `orthographicSize` 模拟，协程 + AnimationCurve 插值 | 🟢 现阶段（简化版） |
| 1.1b | FOV 缩窄 60°→40° | 正交相机无 FOV，与 orthographicSize 合并处理 | 🟢 同上合并 |
| 1.1c | 目标点锁定飞船中心 | 简单相机位置跟随 | 🟢 现阶段 |
| 1.2a | 景深 (DoF) 模糊背景 | URP Volume 已含 DoF Override，代码切换权重即可 | 🟢 现阶段 |
| 1.2b | 暗角 (Vignette) 0.1→0.5 | URP Volume 已含 Vignette Override，代码切换即可 | 🟢 现阶段 |
| 1.2c | 色差脉冲 (Chromatic Aberration) 0→1→0 | 锦上添花效果，优先级低 | 🔴 未来 |
| 1.3a | 飞船半透明线框材质 (Holographic Wireframe) | 需 Shader Graph 全新制作 + 线框美术资源 | 🔴 未来 |
| 1.3b | 装甲展开动画（甲壳虫张翅膀） | 2D Sprite 无装甲板，需美术重制/分层 | 🔴 未来 |
| 1.4a | UI 以飞船为中心生长 (Grow) | 需 Tween 库 + UI 锚点/世界空间 Canvas 改造 | 🔴 未来 |
| 1.4b | 三阶段弹射动画（Core→Prism→外环） | 需精细 Tween 序列编排 | 🔴 未来 |
| — | EaseOutCubic 0.35s 过渡曲线 | 协程 + AnimationCurve 可实现 | 🟢 现阶段 |

### 📌 GDD 第 2 节：编织态体验 (The Weaving State)

| # | GDD 原文功能 | 可行性分析 | 阶段划分 |
|---|-------------|-----------|---------|
| 2.1a | 背景冻结 — TimeScale=0 | UIManager 已实现 | ✅ 已完成 |
| 2.1b | 子弹停在半空的视觉压迫感 | TimeScale=0 自然实现 | ✅ 已完成 |
| 2.1c | UI 视差微动 — 跟随鼠标偏移 | 一个简单脚本，技术可行 | 🟢 现阶段 |
| 2.1d | 连接线光路流动光效 | 需 UI Shader 或粒子系统 | 🔴 未来 |
| 2.2a | 低通滤波 (Low-pass Filter) 800Hz | 需 AudioMixer + LowpassFilter 组件 | 🔴 未来 |
| 2.2b | 环境音"晶体嗡嗡声" | 需音频素材 + AudioMixer Group | 🔴 未来 |
| 2.2c | 操作音清脆前景（不经过滤波器） | 需 AudioMixer Group 分离 | 🔴 未来 |

### 📌 GDD 第 3 节：退出流程 (The Exit Sequence)

| # | GDD 原文功能 | 可行性分析 | 阶段划分 |
|---|-------------|-----------|---------|
| 3.1 | 镜头弹回 (0.25s) | 进入流程的逆向，协程同样适用 | 🟢 现阶段 |
| 3.2 | 材质还原 + 装甲闭合 + Clank 声 | 依赖飞船 Shader | 🔴 未来 |
| 3.3 | UI 坍缩消失 | 依赖 Tween 动画 | 🔴 未来 |
| 3.4 | 低通滤波器移除 + BGM 回归 | 依赖 AudioMixer | 🔴 未来 |
| 3.5 | 气阀释放声 / 充能完毕声 | 需音效素材 | 🔴 未来 |

### 📌 GDD 第 4 节：技术清单

| # | 技术项 | 可行性 | 阶段划分 |
|---|--------|--------|---------|
| 4.1 | DOTween / PrimeTween 插值曲线 | 未安装，协程 + AnimationCurve 暂替 | 🟢 协程替代 |
| 4.2 | Shader Graph 全息 Shader | 需全新创建 | 🔴 未来 |
| 4.3 | Audio Mixer (LP Filter + Group) | 需创建资产和 Group 架构 | 🔴 未来 |
| 4.4 | Time.timeScale = 0 | UIManager 已实现 | ✅ 已完成 |
| 4.5 | UnscaledTime 动画 | HeatBarHUD 中已有使用经验 | ✅ 已有基础 |

---

## 需求列表

### 需求 1：镜头过渡效果（简化版）

**用户故事：** 作为玩家，我希望打开星图面板时摄像机有明显的聚焦缩放变化，以便感受到从战斗态切换到编织态的视觉反馈。

#### 验收标准

1. WHEN 玩家按下 TAB/ESC 打开星图面板 THEN 系统 SHALL 在 0.35 秒内将摄像机 `orthographicSize` 从当前战斗值平滑过渡至编织值（更小的 size = 更近的微距视角）
2. WHEN 镜头过渡进行中 THEN 系统 SHALL 使用 EaseOutCubic 插值曲线（通过 AnimationCurve 配置），确保快速启动、平滑停止
3. WHEN 玩家关闭星图面板 THEN 系统 SHALL 在 0.25 秒内将摄像机 `orthographicSize` 恢复至战斗值
4. IF Time.timeScale == 0 THEN 系统 SHALL 使用 `Time.unscaledDeltaTime` 驱动过渡动画
5. WHEN 过渡期间 THEN 系统 SHALL 将镜头目标锁定在飞船中心位置
6. WHEN 编织值和战斗值 THEN 系统 SHALL 通过 `[SerializeField]` 暴露这两个数值，允许设计师在 Inspector 中调整

### 需求 2：后处理氛围切换（简化版）

**用户故事：** 作为玩家，我希望进入编织态时背景变得模糊、画面边缘变暗，以便集中注意力到飞船和星图 UI 上。

#### 验收标准

1. WHEN 打开星图面板 THEN 系统 SHALL 启用 URP Volume Override 中的 DepthOfField，将焦点锁定在飞船所在距离
2. WHEN 打开星图面板 THEN 系统 SHALL 将 Vignette 强度从默认值（~0.1）平滑过渡至 0.5
3. WHEN 关闭星图面板 THEN 系统 SHALL 反向过渡所有后处理参数：关闭 DoF、恢复 Vignette 默认值
4. IF Time.timeScale == 0 THEN 后处理过渡 SHALL 使用 `Time.unscaledDeltaTime` 驱动
5. WHEN 后处理参数 THEN 系统 SHALL 通过 `[SerializeField]` 暴露 Vignette 目标强度和 DoF 参数，允许设计师调整

### 需求 3：UI 视差微动效

**用户故事：** 作为玩家，我希望编织态下星图 UI 跟随鼠标有轻微位移，以便增加界面的 3D 悬浮感和精致度。

#### 验收标准

1. WHEN 处于编织态且鼠标移动 THEN 星图面板根节点 SHALL 根据鼠标偏离屏幕中心的程度做轻微反向位移（视差效果）
2. WHEN 视差移动 THEN 最大位移量 SHALL 不超过 ±15 像素（可配置），避免影响操作精度
3. IF 使用手柄输入 THEN 视差效果 SHALL 基于右摇杆方向而非鼠标位置
4. WHEN 面板关闭 THEN 视差偏移 SHALL 立即清零恢复原位

### 需求 4：进入/退出音效反馈

**用户故事：** 作为玩家，我希望打开和关闭星图时有音效反馈，以便增强操作的确认感。

#### 验收标准

1. WHEN 打开星图面板 THEN 系统 SHALL 播放一个"打开"音效（AudioClip 通过 SerializeField 配置，可先用占位音效）
2. WHEN 关闭星图面板 THEN 系统 SHALL 播放一个"关闭"音效（同上）
3. IF Time.timeScale == 0 THEN 音效播放 SHALL 不受时间缩放影响（在设置 timeScale=0 之前播放，或使用不受暂停影响的 AudioSource 设置）
4. IF 音效 AudioClip 为 null THEN 系统 SHALL 静默跳过，不报错

---

## 🔴 标记为"未来再做"的功能清单

以下功能在 GDD 中有描述但**当前阶段不实现**，将在 GDD 中标注阶段：

| 功能 | 不做的原因 | 前置依赖 |
|------|-----------|----------|
| 飞船半透明线框材质 (Holographic Wireframe) | 需 Shader Graph 全新制作 + 线框美术资源 | Shader Graph 制作 + 美术 |
| 飞船装甲展开动画 | 2D Sprite 无装甲板概念，需飞船分层美术 | 飞船美术重制 |
| UI 从飞船中心弹射生长 + 三阶段动画 | 需 Tween 库 + UI 架构改造（世界空间 Canvas 或锚点动画） | DOTween/PrimeTween |
| 色差脉冲 (Chromatic Aberration) | 锦上添花效果，当前 Volume Profile 中可能未含此 Override | 确认 Volume Profile |
| 低通滤波音频 (800Hz LP) | 需 AudioMixer 资产和 Group 架构 | AudioMixer 基础建设 |
| 环境音 / 操作音分离 | 需 AudioMixer Group 分离 + 音频素材 | AudioMixer + 音效采购 |
| 连接线光路流动光效 | 需 UI Shader 或粒子系统 + 美术设计 | 美术资源 |
| UI 坍缩退出动画 | 需 Tween 库 | DOTween/PrimeTween |
| 退出气阀声 / 充能声 | 需音效素材 | 音效采购/制作 |

---

## 范围边界总结

### ✅ 本阶段做（4 个需求）：
1. 用**协程 + AnimationCurve** 实现简化版镜头推拉过渡
2. 用**代码控制 URP Volume Override** 实现 DoF + Vignette 切换
3. 用简单脚本实现 **UI 视差微动**
4. 在 UIManager 开关面板时添加**音效播放**占位

### 🚫 本阶段不做：
- 不安装 DOTween/PrimeTween（协程暂替，未来可无痛替换）
- 不创建 AudioMixer（音频架构属于独立模块）
- 不制作飞船 Shader / 线框材质（等待美术资源）
- 不制作 UI 弹射/坍缩动画（等待 Tween 库）
