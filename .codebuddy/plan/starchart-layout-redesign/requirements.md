# 需求文档：StarChart UI 布局重构（对齐 HTML 原型）

## 引言

当前 Unity 实现的 StarChart UI 与 `Tools/StarChartUIPrototype.html` 原型在**布局架构**上存在根本性差异。本次重构目标是让 Unity 实现在视觉结构和交互逻辑上完全对齐原型，同时保留现有的 C# 数据层（`WeaponTrack`、`StarChartController`、`SlotLayer` 等）不变。

### 当前实现 vs 原型的核心差异

| 差异点 | 当前实现 | HTML 原型 |
|--------|----------|-----------|
| 轨道内部布局 | 线性槽位（SAIL×1 + PRISM×3 + CORE×3 + SAT×2 横排） | 4 列分组（SAIL列 \| PRISM列 \| CORE列 \| SAT列），每列有列头 + 2×2 格子网格 |
| Loadout 切换器 | 不存在 | 左侧 Gatling 列（▲ 鼓形计数器 ▼ + LOADOUT 标签） |
| 整体面板结构 | 上下两个 TrackView + 背包 | Loadout 卡片（含 PRIMARY + SECONDARY 两个 track-block）+ 背包 |
| 背包物品 | 固定大小格子 | 形状感知（物品按 shape 占多格：1×1/1×2/2×1/L形/2×2） |
| 视觉风格 | 默认 Unity 颜色 | 深色科幻风（深黑背景、青色调、类型色彩编码） |

---

## 需求

### 需求 1：TrackView 内部重构为 4 列 × 2×2 网格布局

**用户故事：** 作为玩家，我希望在 StarChart 面板中看到 SAIL / PRISM / CORE / SAT 四个独立的类型列，每列有彩色列头和 2×2 格子网格，以便直观地理解每种类型的装备容量和当前装备状态。

#### 验收标准

1. WHEN TrackView 渲染时 THEN 系统 SHALL 显示 4 个横向排列的类型列（SAIL / PRISM / CORE / SAT），列间有分隔线。
2. WHEN 每个类型列渲染时 THEN 系统 SHALL 在列顶部显示带彩色圆点的类型标签（SAIL=#34d399 / PRISM=#a78bfa / CORE=#60a5fa / SAT=#fbbf24）。
3. WHEN 每个类型列渲染时 THEN 系统 SHALL 在列头下方显示 2 行 × 2 列的格子网格（共 4 个 cell），每个 cell 尺寸为 40×40px，间距 2px。
4. WHEN 一个物品占据多个 cell 时 THEN 系统 SHALL 在对应 cell 上渲染 Item Overlay（绝对定位，覆盖所占 cell 区域），而非在每个 cell 单独显示图标。
5. WHEN 格子为空时 THEN 系统 SHALL 显示半透明的 "+" 占位符。
6. IF 物品的 `SlotSize` 为 1 THEN 系统 SHALL 将其渲染为 1×1 overlay（占 1 个 cell）。
7. IF 物品的 `SlotSize` 为 2 THEN 系统 SHALL 将其渲染为 1×2 或 2×1 overlay（占 2 个 cell，方向由数据决定）。
8. WHEN 类型列的边框颜色 THEN 系统 SHALL 使用对应类型的 dim 色（低透明度）作为默认边框，hover 时加亮。

---

### 需求 2：新增 Loadout 切换器（Gatling 列）

**用户故事：** 作为玩家，我希望在面板左侧看到一个 Loadout 切换器，包含上下切换按钮和鼓形计数器，以便快速在多个 Loadout 配置之间切换。

#### 验收标准

1. WHEN StarChartPanel 渲染时 THEN 系统 SHALL 在 Loadout 区域左侧显示一个宽 56px 的 Gatling 列，包含：▲ 按钮、鼓形计数器（显示当前 Loadout 名称和 "X/N" 编号）、▼ 按钮、"LOADOUT" 标签。
2. WHEN 玩家点击 ▲ 或 ▼ 按钮时 THEN 系统 SHALL 切换到上一个/下一个 Loadout，并触发 Loadout 卡片的滑动切换动画。
3. WHEN Loadout 切换时 THEN 系统 SHALL 更新鼓形计数器显示新的 Loadout 名称和编号，并播放翻转动画（rotateX）。
4. WHEN 只有 1 个 Loadout 时 THEN 系统 SHALL 禁用 ▲▼ 按钮（或循环切换）。
5. WHEN Loadout 切换时 THEN 系统 SHALL 刷新 Loadout 卡片内的 PRIMARY 和 SECONDARY 轨道数据。

> **MVP 范围说明**：Loadout 数据层（多套 Loadout 存储/切换）可在第一阶段简化为 UI 结构占位（仅显示当前单一 Loadout，切换按钮存在但暂不实现多套数据），后续迭代补充完整数据层。

---

### 需求 3：面板整体结构重构为 Loadout 卡片模式

**用户故事：** 作为玩家，我希望 StarChart 面板的上半部分是一张"Loadout 卡片"，卡片内包含 PRIMARY 和 SECONDARY 两条轨道，以便在同一视图中对比和管理两条轨道的配置。

#### 验收标准

1. WHEN StarChartPanel 渲染时 THEN 系统 SHALL 将上半部分（约 50% 高度）组织为：Gatling 列（左）+ Loadout 卡片（右）。
2. WHEN Loadout 卡片渲染时 THEN 系统 SHALL 在卡片顶部显示卡片头（◈ 图标 + Loadout 名称 + "LOADOUT #N" 副标题）。
3. WHEN Loadout 卡片渲染时 THEN 系统 SHALL 在卡片头下方依次显示 PRIMARY track-block 和 SECONDARY track-block，每个 track-block 左侧有轨道标签（PRIMARY [LMB] / SECONDARY [RMB]）。
4. WHEN Loadout 切换动画触发时 THEN 系统 SHALL 对 Loadout 卡片播放滑动动画（向上滑出 + 从下滑入，或向下滑出 + 从上滑入），使用 PrimeTween 实现，`useUnscaledTime: true`。
5. WHEN 面板布局时 THEN 系统 SHALL 保持下半部分（约 50% 高度）为背包区域（InventoryView + 过滤栏）。

---

### 需求 4：视觉风格对齐原型（颜色、字体、装饰）

**用户故事：** 作为玩家，我希望 StarChart 面板呈现深色科幻风格，与 HTML 原型的视觉设计一致，以便获得沉浸式的游戏体验。

#### 验收标准

1. WHEN 面板背景渲染时 THEN 系统 SHALL 使用 `#0b1120`（BgPanel）作为面板背景色，`#0f1828`（BgTrack）作为轨道区域背景色。
2. WHEN 类型色彩渲染时 THEN 系统 SHALL 使用以下颜色：SAIL=#34d399 / PRISM=#a78bfa / CORE=#60a5fa / SAT=#fbbf24，对应 dim 色用于背景，亮色用于边框和标签。
3. WHEN Header 渲染时 THEN 系统 SHALL 显示：青色脉冲圆点 + "CANARY — STAR CHART CALIBRATION SYSTEM" 标题 + "SYSTEM ONLINE" 徽章 + 关闭按钮。
4. WHEN 面板四角渲染时 THEN 系统 SHALL 显示 L 形角落装饰（青色，2px 边框，20×20px）。
5. WHEN 状态栏渲染时 THEN 系统 SHALL 显示：EQUIPPED X/10 · INVENTORY X ITEMS · 操作提示文字。
6. WHEN UICanvasBuilder 执行时 THEN 系统 SHALL 自动应用上述所有颜色配置到对应 UI 组件。

---

### 需求 5：UICanvasBuilder 重构以生成新布局

**用户故事：** 作为开发者，我希望执行 `ProjectArk > Build UI Canvas` 后能自动生成符合新布局的完整 UI 层级，以便快速验收和迭代。

#### 验收标准

1. WHEN 执行 `ProjectArk > Build UI Canvas` 时 THEN 系统 SHALL 生成包含新布局的 StarChartPanel 层级：Header / LoadoutSection（GatlingCol + LoadoutCard）/ SectionDivider / InventorySection / StatusBar。
2. WHEN UICanvasBuilder 生成 TrackView 时 THEN 系统 SHALL 为每个 TrackView 生成 4 个类型列（TypeColumn），每列包含列头 + 2×2 GridContainer。
3. WHEN UICanvasBuilder 生成 GatlingCol 时 THEN 系统 SHALL 创建 ▲ 按钮、DrumCounter（TMP_Text）、▼ 按钮、LOADOUT 标签，并连线到 LoadoutSwitcher 组件。
4. WHEN UICanvasBuilder 重复执行时 THEN 系统 SHALL 保持幂等性（已存在的组件不重复创建）。
5. WHEN UICanvasBuilder 执行完成时 THEN 系统 SHALL 自动连线所有 SerializeField 引用（TrackView 的 4 个 TypeColumn、StarChartPanel 的 LoadoutSwitcher 等）。

---

### 需求 6：拖拽系统适配新的 2×2 网格布局

**用户故事：** 作为玩家，我希望能将背包中的物品拖拽到 2×2 网格的任意空位，系统自动检测形状是否适合，以便直观地装备物品。

#### 验收标准

1. WHEN 玩家从背包拖拽物品到类型列时 THEN 系统 SHALL 高亮所有匹配类型的列（候选列发光动画）。
2. WHEN 拖拽物品悬停在某个 cell 上时 THEN 系统 SHALL 预览物品形状占据的 cell（绿色=可放置，橙色=会替换，红色=越界）。
3. WHEN 玩家释放鼠标在有效位置时 THEN 系统 SHALL 将物品写入对应 cell，并播放 snap-in 动画。
4. WHEN 放置导致替换时 THEN 系统 SHALL 将被替换的物品飞回背包（fly-back 动画），并在状态栏显示 "REPLACED: X ↔ Y"。
5. WHEN 玩家从已装备的 overlay 拖拽到背包区域时 THEN 系统 SHALL 执行卸装操作，物品回到背包。
6. IF 物品类型与目标列类型不匹配 THEN 系统 SHALL 显示红色无效高亮，不允许放置。

