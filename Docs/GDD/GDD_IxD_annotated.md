# 🎨 交互规范：星图界面的"微观聚焦" (The Microscopic Focus IxD)
文档版本：v1.0 核心体验：潜入深海般的专注感。 状态定义：
- Combat State (战斗态)：时间流速 1.0，镜头远，UI 为 HUD 模式。
- Weaving State (编织态)：时间流速 0.0，镜头微距，UI 为 空间投影模式。

### 实现阶段总览

> 本规范描述的是游戏**完成形态**。基于当前项目进度（无 Tween 库、无 AudioMixer、无飞船自定义 Shader），功能已划分为两个阶段：
> - 🟢 **现阶段**：可用现有基础设施实现（协程 + AnimationCurve + URP Volume）
> - 🔴 **未来**：需要新增基础设施（DOTween、AudioMixer、Shader Graph 自定义材质、美术资源）
> - ✅ **已完成**：已在之前的 Batch 中实现

---
## 1. 进入流程 (The Entry Sequence)
当玩家按下 TAB 键时，执行以下 0.35秒 的过渡序列。所有效果需使用 EaseOutCubic 曲线，确保"快速启动，平滑刹车"。

### 1.1 镜头运动 (Camera Work) — 🟢 现阶段（简化版）
> **现阶段实现方式**：2D 正交相机通过协程插值 `orthographicSize`（大值=战斗远景，小值=编织微距），配合 `AnimationCurve` 的 EaseOutCubic 曲线。使用 `Time.unscaledDeltaTime` 驱动。

- 推拉 (Dolly In)：摄像机从当前的战斗高度（假设 Height=15）迅速下降至微距高度（Height=4）。
- 视场角 (FOV)：同时将 FOV 从 60° 缩窄至 40°，产生一种"聚精会神"的视觉压缩感。
  - *注：2D 正交相机无 FOV 概念，与 orthographicSize 合并处理。*
- 目标点 (Target)：镜头中心死死锁定飞船的物理中心。

### 1.2 画面后处理 (Post-Processing)
这是营造"微观感"的关键。
- 景深 (Depth of Field)： — 🟢 现阶段
  > **现阶段实现方式**：代码获取 URP Volume 中的 DoF Override，切换 active + 设置参数。
  - FocusDistance：锁定在飞船模型上。
  - FocalLength：瞬间拉大。
  - 效果：背景中的敌人、子弹、地形瞬间变得极度模糊（Bokeh 效果），只剩下飞船清晰可见。
- 暗角 (Vignette)：强度从 0.1 增加到 0.5，压暗屏幕四角，引导视线聚焦中心。 — 🟢 现阶段
  > **现阶段实现方式**：代码控制 URP Vignette Override 的 intensity 参数平滑过渡。
- 色差 (Chromatic Aberration)：在切换瞬间给一个短促的脉冲（0 -> 1.0 -> 0），模拟镜头极速变焦产生的物理瑕疵。 — 🔴 未来
  > **推迟原因**：锦上添花效果，优先级低。需确认当前 Volume Profile 是否含此 Override。

### 1.3 飞船形态转化 (Ship Metamorphosis) — 🔴 未来（整体推迟）
> **推迟原因**：当前飞船为 2D Sprite，不存在"装甲板"概念。需要 Shader Graph 全新制作全息线框材质 + 飞船分层美术资源。
> **前置依赖**：Shader Graph 全息材质制作 + 飞船美术重制/分层

飞船不能是个死物，它必须"打开"以接受改装。
- 材质切换：飞船模型从"PBR 实体材质"渐变为 "半透明晶体线框 (Holographic Wireframe)"。
  - 目的：让玩家能看清内部结构，且不遮挡底层的 UI 槽位。
- 动画：飞船装甲板微微展开（像甲壳虫张开翅膀），露出内部发光的星核槽位。

### 1.4 UI 展开 (UI Unfolding) — 🔴 未来（整体推迟）
> **推迟原因**：需要 Tween 库（DOTween/PrimeTween）+ UI 架构改造（世界空间 Canvas 或锚点动画系统）。
> **前置依赖**：DOTween/PrimeTween 安装
> **现阶段替代**：面板直接 SetActive(true) 显示，无动画。

UI 不是淡入 (Fade In)，而是生长 (Grow)。
- 原点：所有 UI 元素都以飞船为中心点。
- 动画：
  - Phase 1 (0.0s - 0.1s)：星核槽位（Core Slots）从飞船中心向外弹射。
  - Phase 2 (0.1s - 0.2s)：棱镜槽位（Prism Slots）跟随星核槽位浮现。
  - Phase 3 (0.2s - 0.3s)：外环的伴星轨道和背包列表旋入屏幕。

---
## 2. 编织态体验 (The Weaving State)
进入界面后，玩家处于"暂停"状态。

### 2.1 视觉氛围
- 背景冻结：战场（敌人/子弹）虽然模糊了，但依然停留在原地。这种"子弹停在半空"的压迫感能提醒玩家："你正在战场中心修飞船"。 — ✅ 已完成（TimeScale=0）
- 微动效 (Micro-movements)：
  - 星图 UI 会跟随鼠标位置有轻微的 视差 (Parallax) 移动，增加 3D 悬浮感。 — 🟢 现阶段
    > **现阶段实现方式**：简单脚本读取鼠标/右摇杆偏移，对面板根节点应用 ±15px 反向位移。
  - 连接线（光路）有持续流动的光效。 — 🔴 未来
    > **推迟原因**：需要 UI Shader 或粒子系统 + 美术设计。

### 2.2 听觉氛围 (Audioscape) — 🔴 未来（整体推迟，需 AudioMixer 基础建设）
> **推迟原因**：低通滤波、音频分组均需 AudioMixer 资产和 Group 架构。当前无 AudioMixer。
> **前置依赖**：AudioMixer 创建 + Master/UI Group 划分 + 音效素材采购
> **现阶段替代**：仅添加开/关面板的简单占位音效（需求 4）。

- 低通滤波 (Low-pass Filter)：
  - 进入瞬间，所有战斗音效（BGM、枪声）经过一个 800Hz 的低通滤波器。
  - 听感：像是一头扎进了水里，外界的声音变得闷闷的。
- 环境音 (Ambience)：
  - 播放一层安静的"精密仪器运转声"或"晶体嗡嗡声" (Crystal Hum)。
- 操作音：
  - 所有的点击、拖拽音效必须不经过滤波器，保持清脆、高亮，形成听觉上的"前景"与"背景"分离。

---
## 3. 退出流程 (The Exit Sequence)
当玩家按下 TAB 或 ESC 时，执行 0.25秒 的快速退出。
1. 镜头弹回：迅速拉回战斗高度。 — 🟢 现阶段（进入流程的逆向协程）
2. 材质还原：飞船线框瞬间实体化，装甲闭合（伴随 Clank 的闭合声）。 — 🔴 未来（依赖飞船 Shader）
3. UI 收缩：所有 UI 元素向中心坍缩并消失。 — 🔴 未来（依赖 Tween 库）
4. 音效释放：低通滤波器移除，战场的喧嚣声（BGM 高潮）瞬间回归。 — 🔴 未来（依赖 AudioMixer）
  - 伴随音效：气阀释放声 (Psssshh) 或 能量充能完毕声 (Vroooom)。 — 🔴 未来（需音效素材）

---
## 4. 给技术美术 (Tech Art) 的实现清单
请将此清单发给负责 Unity 效果实现的程序：
1. DoTween / PrimeTween：用于控制 Camera Size, PostProcessing Weight 的插值曲线。 — 🟢 现阶段用协程 + AnimationCurve 替代
2. Shader Graph：制作一个支持 Lerp(Solid, Hologram) 的飞船 Shader。 — 🔴 未来
  - 需要参数：HologramIntensity (控制透明度和网格亮度)。
  - 需要参数：Expansion (控制装甲板顶点的世界坐标偏移，做展开动画)。
3. Audio Mixer：设置两个 Group：Master 和 UI。 — 🔴 未来
  - Master 组挂载 Lowpass Filter。
  - 在进入星图时，代码控制 Cutoff Frequency 从 22000 降至 800。
4. Time Scale： — ✅ 已完成
  - 进入时：Time.timeScale = 0 (完全暂停)。
  - 注意：UI 动画和 Shader 动画必须使用 UnscaledTime，否则动画也会被暂停。
