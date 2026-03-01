# 需求文档：SpaceLife 过渡动画优化

## 引言

当前按 Tab 键进入/退出飞船生活（SpaceLife）模式时，过渡动画使用 `TransitionUI` 实现：逐字打字机显示文字 → 等待 1.5 秒 → 淡出 → 淡入，整个流程约 3-4 秒，体验拖泥带水、响应迟钝。

相比之下，按 C 键打开星图（StarChart）的动画仅需 200ms（scale 0.95→1.0 + alpha 0→1，PrimeTween OutQuad），手感干脆利落。

本需求旨在将 SpaceLife 的进入/退出过渡动画对齐 StarChart 的风格：**快速、干净、有手感**，去掉打字机文字和长时间等待，改为简短的淡入淡出切换。

---

## 需求

### 需求 1：快速淡入淡出替换打字机过渡

**用户故事：** 作为玩家，我希望按 Tab 进入/退出飞船生活模式时的过渡动画快速且干净，以便操作响应感与按 C 打开星图保持一致。

#### 验收标准

1. WHEN 玩家按 Tab 触发进入 SpaceLife THEN 系统 SHALL 播放一次快速淡黑（fade to black，约 150ms）后立即切换场景状态，再淡出（fade from black，约 150ms），总时长不超过 400ms。
2. WHEN 玩家按 Tab 触发退出 SpaceLife THEN 系统 SHALL 播放相同的快速淡黑→切换→淡出序列，总时长不超过 400ms。
3. WHEN 过渡动画播放时 THEN 系统 SHALL 不显示任何打字机文字或中心提示文字。
4. IF `TransitionUI` 组件不存在或 `_fadeOverlay` 未连线 THEN 系统 SHALL 跳过淡入淡出直接切换，不报错。

---

### 需求 2：动画使用 PrimeTween，与项目规范对齐

**用户故事：** 作为开发者，我希望 SpaceLife 过渡动画使用 PrimeTween 实现，以便与项目其他 UI 动画保持技术一致性。

#### 验收标准

1. WHEN 实现淡入淡出动画 THEN 系统 SHALL 使用 `Tween.Alpha` 或 `Tween.Custom` 配合 `useUnscaledTime: true`，不使用 Coroutine 或手写 Lerp。
2. WHEN 过渡动画正在播放时玩家再次按 Tab THEN 系统 SHALL 忽略输入（`_isTransitioning` 保护已存在，保持不变）。
3. WHEN 场景切换发生在淡黑完成后 THEN 系统 SHALL 确保摄像机切换、场景根节点激活/停用等操作在淡黑完成的回调中执行，避免视觉撕裂。

---

### 需求 3：保留 TransitionUI 组件但简化其职责

**用户故事：** 作为开发者，我希望 `TransitionUI` 组件保留但只负责淡入淡出，以便未来可以按需扩展（如添加音效、特效），而不影响当前的简洁体验。

#### 验收标准

1. WHEN 重构 `TransitionUI` THEN 系统 SHALL 保留 `FadeInAsync` 和 `FadeOutAsync` 公共方法，移除或禁用打字机相关逻辑（`TypeTextAsync`、`_centerText`、`_typewriterSpeed`、`_textDisplayDuration`）。
2. WHEN `SpaceLifeManager` 调用过渡 THEN 系统 SHALL 使用 `FadeOutAsync`（淡黑）→ 执行场景切换 → `FadeInAsync`（淡出）的三段式结构，场景切换夹在两段动画之间。
3. IF 未来需要重新加入文字提示 THEN 系统 SHALL 能通过在 `TransitionUI` 中重新启用相关字段实现，不需要修改 `SpaceLifeManager`。
