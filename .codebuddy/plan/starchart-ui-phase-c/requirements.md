# 需求文档：StarChart UI Phase C — 交互动画与体验打磨

## 引言

Phase B 完成了 4 列布局和强制替换逻辑。Phase C 的目标是**为关键交互添加动画反馈**，提升手感和视觉体验，使其与原型的动态效果对齐。

**Phase C 的核心约束**：
- 所有动画使用 PrimeTween，不手写 Lerp
- 动画不影响游戏逻辑（纯视觉层）
- 动画时长控制在 300ms 以内，不阻塞操作
- 使用 `useUnscaledTime: true`（星图面板在暂停状态下打开）

**不在 Phase C 范围内**：
- 加特林切换翻转动画（CSS perspective 3D 效果，Unity UI 无原生支持，优先级低）
- 扫描光束动画（需要全屏 Shader，属于视觉锦上添花）

---

## 需求

### 需求 1：部件飞回背包动画

**用户故事：** 作为玩家，我希望被顶出的部件有飞回背包的动画，以便直观感受替换操作的结果。

#### 验收标准

1. WHEN 强制替换发生，部件被顶出 THEN 系统 SHALL 创建一个飞行克隆体（`RectTransform`），从槽位位置飞向背包区域
2. WHEN 飞行动画播放 THEN 系统 SHALL 使用 PrimeTween 在 350ms 内完成位移 + 缩小 + 淡出（`Ease.InQuad`）
3. WHEN 飞行动画结束 THEN 系统 SHALL 销毁克隆体，背包格子显示落地弹跳效果（scale 1.0 → 1.12 → 1.0，100ms）
4. WHEN 多个部件同时被顶出 THEN 系统 SHALL 并行播放多个飞行动画（不串行等待）
5. WHEN 新的拖拽操作开始时飞行动画仍在播放 THEN 系统 SHALL 立即跳过所有进行中的飞行动画
6. WHEN 飞行动画播放期间 THEN 系统 SHALL 隐藏背包中对应格子的真实 UI（避免重叠），动画结束后恢复显示

---

### 需求 2：拖拽 Ghost 增强

**用户故事：** 作为玩家，我希望拖拽时的 Ghost 预览更清晰，能显示替换提示，以便做出正确决策。

#### 验收标准

1. WHEN 拖拽悬停在空槽位上 THEN 系统 SHALL Ghost 边框变为绿色（`StarChartTheme.HighlightValid`）
2. WHEN 拖拽悬停在有部件的槽位上 THEN 系统 SHALL Ghost 边框变为橙色（`StarChartTheme.HighlightReplace`），并在 Ghost 底部显示 `↺ REPLACE` 提示文字
3. WHEN 拖拽悬停在无效槽位上 THEN 系统 SHALL Ghost 边框变为红色（`StarChartTheme.HighlightInvalid`）
4. WHEN 拖拽开始 THEN 系统 SHALL Ghost 以 scale 0.8 → 1.0 的弹出动画出现（80ms，`Ease.OutBack`）
5. WHEN 拖拽取消或放置 THEN 系统 SHALL Ghost 以 scale 1.0 → 0.0 的收缩动画消失（80ms，`Ease.InQuad`）

---

### 需求 3：槽位装备/卸载动画

**用户故事：** 作为玩家，我希望装备和卸载部件时槽位有视觉反馈，以便确认操作成功。

#### 验收标准

1. WHEN 部件成功装备到槽位 THEN 系统 SHALL 槽位背景闪烁一次（当前颜色 → 白色 → 当前颜色，150ms）
2. WHEN 部件从槽位卸载 THEN 系统 SHALL 槽位背景淡出到空槽位颜色（100ms，`Ease.OutQuad`）
3. WHEN 拖拽放置成功 THEN 系统 SHALL 目标槽位播放装备动画（需求 3.1）

---

### 需求 4：背包格子交互动画

**用户故事：** 作为玩家，我希望背包格子的悬停和选中有流畅的动画反馈。

#### 验收标准

1. WHEN 鼠标悬停在背包格子上 THEN 系统 SHALL 格子以 scale 1.0 → 1.06 动画放大（120ms，`Ease.OutQuad`）
2. WHEN 鼠标离开背包格子 THEN 系统 SHALL 格子以 scale 1.06 → 1.0 动画缩小（120ms，`Ease.OutQuad`）
3. WHEN 背包格子被选中 THEN 系统 SHALL 格子边框以 PrimeTween 淡入青色发光（100ms）
4. WHEN 背包格子取消选中 THEN 系统 SHALL 格子边框以 PrimeTween 淡出（100ms）
5. WHEN 拖拽开始时 THEN 系统 SHALL 源格子（被拖拽的格子）alpha 降低到 0.4（80ms）
6. WHEN 拖拽结束时 THEN 系统 SHALL 源格子 alpha 恢复到 1.0（80ms）

---

### 需求 5：StatusBar 动画增强

**用户故事：** 作为玩家，我希望 StatusBar 的通知消息有淡入效果，以便注意到新消息。

#### 验收标准

1. WHEN 新消息显示 THEN 系统 SHALL 消息文字以 alpha 0 → 1 淡入（150ms，`Ease.OutQuad`）
2. WHEN 消息淡出 THEN 系统 SHALL 消息文字以 alpha 1 → 0 淡出（500ms，`Ease.InQuad`）
3. WHEN 新消息打断旧消息 THEN 系统 SHALL 立即停止旧动画，新消息从 alpha 0 开始淡入

---

### 需求 6：面板开关动画

**用户故事：** 作为玩家，我希望星图面板的打开和关闭有流畅的动画，以便获得沉浸感。

#### 验收标准

1. WHEN 星图面板打开 THEN 系统 SHALL 面板以 scale 0.95 → 1.0 + alpha 0 → 1 动画出现（200ms，`Ease.OutQuad`）
2. WHEN 星图面板关闭 THEN 系统 SHALL 面板以 scale 1.0 → 0.95 + alpha 1 → 0 动画消失（150ms，`Ease.InQuad`），动画结束后 `SetActive(false)`
3. WHEN 面板动画播放期间 THEN 系统 SHALL 禁用所有交互（`CanvasGroup.interactable = false`），动画结束后恢复
