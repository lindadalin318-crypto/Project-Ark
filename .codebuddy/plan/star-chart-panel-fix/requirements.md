# 需求文档：StarChartPanel 初始化修复

## 引言

**问题描述**：每次删除场景 Canvas 并重新执行 `BuildUICanvas` 后，必须手动在 Hierarchy 中将 `StarChartPanel` 从 inactive 切换为 active，C 键才能正常打开星图面板。

**根本原因**：`UICanvasBuilder.BuildUICanvas()` Step 5 在 **Editor 模式**下调用 `starChartPanel.gameObject.SetActive(false)`，该操作将 `inactive` 状态**序列化进场景文件**。进入 Play Mode 后，`StarChartPanel` 从场景加载时已是 `inactive`，`Awake()` 不会被调用。当 C 键触发 `Open()` → `gameObject.SetActive(true)` 时，`Awake()` 才首次执行，而 `Awake()` 内部又调用了 `gameObject.SetActive(false)`，导致面板立刻被关闭，C 键永久失效。

**修复策略**：移除所有通过 `SetActive(false)` 实现初始隐藏的逻辑，改为完全依赖 `CanvasGroup`（alpha=0, interactable=false, blocksRaycasts=false）来实现视觉隐藏。`StarChartPanel` 在场景中始终保持 **active**，`Awake()` 在场景加载时正常执行完毕，C 键可立即工作。

---

## 需求

### 需求 1：移除 StarChartPanel.Awake() 中的 SetActive(false)

**用户故事：** 作为开发者，我希望 `StarChartPanel.Awake()` 不再调用 `gameObject.SetActive(false)`，以便面板在 Play Mode 启动时能正确完成初始化，C 键可立即使用。

#### 验收标准

1. WHEN Play Mode 启动 THEN `StarChartPanel.Awake()` SHALL 执行完毕（CanvasGroup 初始化为 alpha=0, interactable=false, blocksRaycasts=false），且 GameObject 保持 active 状态
2. WHEN C 键第一次按下 THEN 星图面板 SHALL 正常打开（淡入动画播放，面板可交互）
3. IF `_panelCanvasGroup` 为 null THEN `Awake()` SHALL 不抛出异常，仅跳过 CanvasGroup 初始化

### 需求 2：移除 UICanvasBuilder Step 5 的 SetActive(false)

**用户故事：** 作为开发者，我希望 `UICanvasBuilder.BuildUICanvas()` 不再在 Editor 模式下将 `StarChartPanel` 设为 inactive，以便每次重建 Canvas 后无需手动操作即可直接进入 Play Mode 使用 C 键。

#### 验收标准

1. WHEN `BuildUICanvas` 执行完毕 THEN `StarChartPanel` GameObject SHALL 在 Hierarchy 中保持 active 状态
2. WHEN `BuildUICanvas` 执行完毕 THEN `StarChartPanel` 的 `CanvasGroup` SHALL 被初始化为 alpha=0, interactable=false, blocksRaycasts=false（视觉上不可见）
3. WHEN 进入 Play Mode（无需任何手动操作）THEN 按 C 键 SHALL 能正常打开星图面板

### 需求 3：IsOpen 属性与 CanvasGroup 状态保持一致

**用户故事：** 作为开发者，我希望 `StarChartPanel.IsOpen` 属性能准确反映面板的可见状态，以便 `UIManager` 的 toggle 逻辑不会因 `activeSelf` 始终为 true 而出现状态错误。

#### 验收标准

1. WHEN 面板处于关闭状态（CanvasGroup alpha=0）THEN `IsOpen` SHALL 返回 `false`
2. WHEN 面板处于打开状态（CanvasGroup alpha=1）THEN `IsOpen` SHALL 返回 `true`
3. WHEN `Close()` 动画完成后 THEN `IsOpen` SHALL 返回 `false`（不依赖 `gameObject.activeSelf`）
