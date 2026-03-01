# 实施计划：StarChartPanel 初始化修复

- [ ] 1. 修改 `StarChartPanel.Awake()` — 移除 `gameObject.SetActive(false)`，改用 `_isOpen` 字段追踪状态
   - 删除 `Awake()` 末尾的 `gameObject.SetActive(false)` 调用
   - 新增私有字段 `private bool _isOpen = false;` 追踪面板开关状态
   - 保留 CanvasGroup 初始化逻辑（alpha=0, interactable=false, blocksRaycasts=false）
   - _需求：1.1、1.2、1.3_

- [ ] 2. 修改 `StarChartPanel.IsOpen` 属性 — 改为返回 `_isOpen` 字段而非 `gameObject.activeSelf`
   - 将 `public bool IsOpen => gameObject.activeSelf;` 改为 `public bool IsOpen => _isOpen;`
   - _需求：3.1、3.2_

- [ ] 3. 修改 `StarChartPanel.Open()` 和 `Close()` — 同步更新 `_isOpen` 字段，移除 `SetActive` 调用
   - `Open()` 开头设置 `_isOpen = true;`，移除 `gameObject.SetActive(true);`
   - `Close()` 的 `ChainCallback` 中设置 `_isOpen = false;`，移除 `gameObject.SetActive(false);`
   - `Close()` 的 else 分支（无 CanvasGroup）中同样移除 `gameObject.SetActive(false);`，改为直接设置 `_isOpen = false;`
   - _需求：3.3_

- [ ] 4. 修改 `UICanvasBuilder.BuildUICanvas()` Step 5 — 移除 `SetActive(false)`，改为初始化 CanvasGroup
   - 删除 Step 5 的 `starChartPanel.gameObject.SetActive(false);`
   - 改为获取 `CanvasGroup` 并设置 alpha=0, interactable=false, blocksRaycasts=false
   - _需求：2.1、2.2、2.3_
