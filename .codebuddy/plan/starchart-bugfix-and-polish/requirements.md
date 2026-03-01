# 需求文档：StarChart UI 全面排查与修复

## 引言

本次排查目标是将 Unity 中的 StarChart UI 实现与 `StarChartUIPrototype.html` 原型对齐，同时修复两个已知运行时 Bug：
1. **Canvas 在 Play 开始时就可见**（应默认隐藏，按 Tab 才显示）
2. **面板无法点击**（按钮/拖拽不响应）

排查范围涵盖：初始化流程、布局结构、视觉风格、交互逻辑、拖拽系统、物品详情面板、背包过滤、状态栏。

---

## 需求

### 需求 1：修复 Canvas 初始化 Bug（Play 开始时面板不可见）

**用户故事：** 作为玩家，我希望进入 Play Mode 后 StarChart 面板默认隐藏，按 Tab 键才弹出，以便不干扰正常游戏流程。

#### 验收标准

1. WHEN Play Mode 启动 THEN StarChartPanel GameObject SHALL 处于 `SetActive(false)` 状态
2. WHEN UIManager.Start() 执行 THEN 系统 SHALL 在 `Bind()` 之后立即调用 `SetActive(false)`
3. WHEN UICanvasBuilder 构建 Canvas THEN StarChartPanel SHALL 被设置为 `SetActive(false)`，确保 Prefab/Scene 中默认不可见
4. IF StarChartPanel 的 CanvasGroup 存在 THEN 系统 SHALL 在 `Awake` 中将 `alpha=0`、`interactable=false`、`blocksRaycasts=false`，防止透明状态下仍拦截点击

---

### 需求 2：修复面板无法点击 Bug

**用户故事：** 作为玩家，我希望打开 StarChart 面板后所有按钮、拖拽、过滤器都能正常响应点击，以便正常使用装备系统。

#### 验收标准

1. WHEN 面板 Open() 动画完成 THEN CanvasGroup.interactable SHALL 被设置为 `true`，CanvasGroup.blocksRaycasts SHALL 为 `true`
2. WHEN 面板处于关闭状态 THEN CanvasGroup.blocksRaycasts SHALL 为 `false`，防止不可见面板拦截游戏输入
3. WHEN UICanvasBuilder 构建 StarChartPanel THEN 系统 SHALL 确保 CanvasGroup 组件存在并正确初始化
4. IF InputSystemUIInputModule 未正确连线 THEN UIManager SHALL 在 Start() 中重新执行 ConfigureUIInputModule()

---

### 需求 3：布局与原型对齐

**用户故事：** 作为开发者，我希望 Unity 中的 StarChart 布局与 HTML 原型完全一致，以便视觉验收通过。

#### 验收标准

1. WHEN 面板打开 THEN 布局 SHALL 为：顶部 Header（6%）+ 中部 LoadoutSection（55%，含 GatlingCol 左 6.5% + LoadoutCard 右 93.5%）+ 分隔线 + 底部 InventorySection（38%）+ 状态栏（4%）
2. WHEN LoadoutCard 渲染 THEN 其内部 SHALL 包含：CardHeader（◈ 图标 + 名称 + 副标题）+ TracksArea（PRIMARY 左半 + SECONDARY 右半，各含 4 列 TypeColumn）
3. WHEN TypeColumn 渲染 THEN 每列 SHALL 包含：列头（彩色圆点 + 类型标签）+ 2×2 GridLayout（4 个 SlotCellView）
4. WHEN 背包区域渲染 THEN 过滤按钮顺序 SHALL 为：ALL → SAILS → PRISMS → CORES → SATS（与原型一致）
5. WHEN 状态栏渲染 THEN 内容 SHALL 为：`EQUIPPED X/10 · INVENTORY X ITEMS · HOVER TO INSPECT · DRAG ITEM TO SLOT`

---

### 需求 4：视觉风格对齐

**用户故事：** 作为开发者，我希望 Unity 中的颜色、字体、装饰元素与 HTML 原型一致，以便视觉验收通过。

#### 验收标准

1. WHEN 面板背景渲染 THEN 颜色 SHALL 为 `#0b1120`（BgPanel），轨道背景 SHALL 为 `#0f1828`（BgTrack）
2. WHEN 类型颜色渲染 THEN SAIL SHALL 为 `#34d399`，PRISM SHALL 为 `#a78bfa`，CORE SHALL 为 `#60a5fa`，SAT SHALL 为 `#fbbf24`
3. WHEN Header 渲染 THEN SHALL 包含：青色脉冲圆点 + 标题文字（CANARY — STAR CHART CALIBRATION SYSTEM）+ 右侧 SYSTEM ONLINE 徽章 + 关闭按钮（×，hover 变红）
4. WHEN 面板四角渲染 THEN SHALL 有 L 形青色角落装饰（Corner Brackets）
5. WHEN StarChartTheme.cs 中颜色常量 SHALL 与原型 CSS 变量完全对应（BgDeep/BgPanel/BgTrack/Cyan/SailColor/PrismColor/CoreColor/SatColor/Border/HighlightValid/HighlightInvalid/HighlightReplace）

---

### 需求 5：物品详情面板（Tooltip/Detail）对齐

**用户故事：** 作为玩家，我希望点击或悬停物品时能看到完整的物品信息（名称、类型、描述、属性、装备状态），以便做出装备决策。

#### 验收标准

1. WHEN 玩家点击背包中的物品 THEN ItemDetailView SHALL 显示：物品图标 + 名称 + 类型标签（带类型颜色）+ 分隔线 + 属性列表 + 描述文字 + 装备状态标签
2. WHEN 物品已装备 THEN 详情面板 SHALL 显示绿色 `EQUIPPED` 标签，操作按钮 SHALL 显示 `UNEQUIP`
3. WHEN 物品未装备 THEN 操作按钮 SHALL 显示 `EQUIP`，并显示目标轨道提示
4. WHEN 玩家悬停在已装备的 SlotCellView 上 THEN 系统 SHALL 触发 OnPointerEntered 事件，ItemDetailView 可选择性显示 tooltip

---

### 需求 6：拖拽系统完整性

**用户故事：** 作为玩家，我希望拖拽物品到槽位时有清晰的视觉反馈（Ghost、高亮、替换提示），以便准确装备物品。

#### 验收标准

1. WHEN 玩家开始拖拽 THEN DragGhostView SHALL 跟随鼠标，显示物品图标和名称
2. WHEN 拖拽物品悬停在匹配类型的 TypeColumn 上 THEN 该列边框 SHALL 高亮（绿色=有空间，橙色=替换，红色=无效）
3. WHEN 拖拽物品悬停在具体 SlotCellView 上 THEN 该 cell SHALL 显示高亮，Ghost 边框颜色 SHALL 对应 Valid/Replace/Invalid 状态
4. WHEN 拖拽结束且目标有效 THEN 系统 SHALL 执行装备操作并刷新所有视图
5. WHEN 拖拽取消（ESC 或松开在无效区域）THEN Ghost SHALL 消失，所有高亮 SHALL 清除，物品 SHALL 飞回背包

---

### 需求 7：背包过滤与滚动

**用户故事：** 作为玩家，我希望能按类型过滤背包物品，并通过滚动查看所有物品，以便快速找到需要的部件。

#### 验收标准

1. WHEN 玩家点击过滤按钮 THEN 背包 SHALL 只显示对应类型的物品，当前激活按钮 SHALL 高亮
2. WHEN 背包物品超出显示区域 THEN ScrollRect SHALL 支持垂直滚动
3. WHEN 物品已装备 THEN 背包中该物品 SHALL 显示绿色 `✓` 角标或边框，表示已装备状态
4. WHEN 面板刷新 THEN 背包 SHALL 保持当前过滤状态不重置

---

### 需求 8：UICanvasBuilder 幂等性与完整性

**用户故事：** 作为开发者，我希望执行 `ProjectArk > Build UI Canvas` 后能一键生成完整可用的 StarChart UI，无需手动配置，以便快速验收。

#### 验收标准

1. WHEN 执行 Build UI Canvas THEN 系统 SHALL 生成完整的层级结构，包含所有 SerializeField 引用自动连线
2. WHEN 重复执行 Build UI Canvas THEN 系统 SHALL 先销毁旧 Canvas 再重建，保证幂等性
3. WHEN 构建完成 THEN StarChartPanel.gameObject SHALL 被设置为 `SetActive(false)`
4. WHEN 构建完成 THEN CanvasGroup SHALL 被初始化为 `alpha=0, interactable=false, blocksRaycasts=false`
5. WHEN 构建完成 THEN Console SHALL 输出构建成功日志，列出所有已连线的组件
