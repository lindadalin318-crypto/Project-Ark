# 实施计划：SpaceLife 过渡动画优化

- [ ] 1. 重构 `TransitionUI.cs`，移除打字机逻辑，改为 PrimeTween 淡入淡出
   - 删除 `TypeTextAsync`、`_centerText`、`_typewriterSpeed`、`_textDisplayDuration` 字段和方法
   - 将 `FadeOutAsync`（淡黑，150ms）和 `FadeInAsync`（淡出，150ms）改为使用 `Tween.Alpha` + `useUnscaledTime: true`
   - `_fadeOverlay` 未连线时直接 return，不报错
   - _需求：1.3、1.4、2.1、3.1_

- [ ] 2. 重构 `SpaceLifeManager.cs`，将过渡调用改为三段式结构
   - 进入/退出 SpaceLife 时统一调用：`await FadeOutAsync()` → 执行场景切换（摄像机切换、根节点激活/停用）→ `await FadeInAsync()`
   - 确保场景切换操作夹在两段动画之间，避免视觉撕裂
   - 保留已有的 `_isTransitioning` 保护逻辑，防止重复触发
   - _需求：1.1、1.2、2.2、2.3、3.2_

- [ ] 3. 验证并更新 `TransitionUI` 在场景中的连线状态
   - 确认 `_fadeOverlay`（全屏黑色 Image）已正确连线，初始 alpha = 0
   - 若场景中存在已废弃的打字机相关 UI 元素（`_centerText`），将其从 Hierarchy 中移除或隐藏
   - _需求：1.4、3.3_
