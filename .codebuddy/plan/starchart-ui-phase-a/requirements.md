# 需求文档：StarChart UI Phase A — 视觉样式与布局对齐

## 引言

当前 Unity 项目中的 StarChart 面板（`StarChartPanel`）已具备基础功能（双轨道 + 背包 + 详情面板 + 拖拽装备），但其布局和视觉样式与 `StarChartUIPrototype.html` 原型存在显著差距。

Phase A 的目标是**在不改变现有数据模型和交互逻辑的前提下**，通过重构 `UICanvasBuilder`（Editor 工具）和相关 View 脚本，使 Unity 中的 StarChart 面板在布局结构和视觉风格上与原型对齐。

**不在 Phase A 范围内**：
- 数据模型变更（WeaponTrack 结构、SlotLayer 格数）
- 拖拽强制替换逻辑
- 飞回背包动画
- 加特林切换动画

---

## 需求

### 需求 1：整体面板布局重构

**用户故事：** 作为玩家，我希望打开星图面板时看到与原型一致的整体布局，以便获得清晰的视觉层次感。

#### 验收标准

1. WHEN 星图面板打开 THEN 系统 SHALL 显示深色半透明背景（`#0d1117`，alpha 0.95），覆盖全屏
2. WHEN 星图面板打开 THEN 系统 SHALL 在面板四角显示 L 形科技感装饰括号（使用 `Image` 组件，白色低透明度）
3. WHEN 星图面板打开 THEN 系统 SHALL 在顶部显示 Header 栏，包含：左侧标题文字 `STAR CHART`、中央状态指示点（绿色脉冲圆点）、右侧关闭按钮 `[×]`
4. WHEN 星图面板打开 THEN 系统 SHALL 在底部显示 Status Bar，高度约 22px，显示当前通知文字（如 `EQUIPPED: xxx`）
5. WHEN 星图面板打开 THEN 系统 SHALL 将上半区（双轨道）和下半区（背包）以水平分割线分隔，上半区占约 55%，下半区占约 40%

---

### 需求 2：Track 区域视觉重构

**用户故事：** 作为玩家，我希望 PRIMARY 和 SECONDARY 两个轨道区域有清晰的视觉分区和类型标签，以便快速识别各槽位类型。

#### 验收标准

1. WHEN 轨道区域渲染 THEN 系统 SHALL 将 PRIMARY 和 SECONDARY 两个 TrackView 并排显示，各占上半区宽度的约 50%，中间有分隔线
2. WHEN 轨道区域渲染 THEN 系统 SHALL 在每个 TrackView 内，将现有的 PrismRow 和 CoreRow 改为带类型标签的列式布局：每列顶部显示类型名称（`PRISM` / `CORE`），下方为槽位格子
3. WHEN 轨道区域渲染 THEN 系统 SHALL 为每种类型使用对应主题色：PRISM 紫色（`#a78bfa`）、CORE 蓝色（`#60a5fa`）
4. WHEN 轨道区域渲染 THEN 系统 SHALL 为每个 TrackView 显示轨道标题（`PRIMARY` / `SECONDARY`），字体加粗，颜色为青色（`#00e5ff`）
5. WHEN 槽位为空 THEN 系统 SHALL 在空槽位中显示 `+` 占位符文字，颜色为低透明度白色
6. WHEN 槽位已装备部件 THEN 系统 SHALL 显示部件图标和短名称，背景使用对应类型主题色（低透明度）
7. WHEN 鼠标悬停在槽位上 THEN 系统 SHALL 显示高亮边框（对应类型主题色，发光效果）

---

### 需求 3：背包区域视觉重构

**用户故事：** 作为玩家，我希望背包区域有清晰的网格布局和类型过滤功能，以便快速找到想要的部件。

#### 验收标准

1. WHEN 背包区域渲染 THEN 系统 SHALL 将背包格子以 `GridLayoutGroup` 排列，每格大小约 64×64px，间距 6px
2. WHEN 背包格子渲染 THEN 系统 SHALL 在每个格子左上角显示类型色点（CORE 蓝、PRISM 紫、SAIL 绿 `#34d399`、SAT 黄 `#fbbf24`）
3. WHEN 部件已装备 THEN 系统 SHALL 在格子右下角显示绿色 `✓` 装备标记，格子边框变为绿色低透明度
4. WHEN 背包区域渲染 THEN 系统 SHALL 在背包上方显示类型过滤栏（ALL / CORES / PRISMS / SAILS / SATS），当前激活的过滤按钮高亮显示
5. WHEN 鼠标悬停在背包格子上 THEN 系统 SHALL 格子轻微放大（scale 1.06）并显示青色边框高亮
6. WHEN 背包格子被选中 THEN 系统 SHALL 格子显示青色发光边框（`#00e5ff`）

---

### 需求 4：颜色主题系统

**用户故事：** 作为开发者，我希望所有颜色通过统一的主题配置管理，以便后续调整和扩展。

#### 验收标准

1. WHEN 系统初始化 THEN 系统 SHALL 通过 `StarChartTheme`（静态类或 ScriptableObject）提供所有主题色常量，包括：背景色、边框色、四种类型主题色（CORE/PRISM/SAIL/SAT）及其 dim/glow 变体
2. WHEN 任何 View 需要类型主题色 THEN 系统 SHALL 通过 `StarChartTheme.GetTypeColor(itemType)` 获取，不得 hardcode 颜色值
3. WHEN `UICanvasBuilder` 构建 UI THEN 系统 SHALL 使用 `StarChartTheme` 中的颜色常量，不得在 Builder 中 hardcode 颜色

---

### 需求 5：UICanvasBuilder 重构

**用户故事：** 作为开发者，我希望运行 `ProjectArk > Build UI Canvas` 后能一键生成与原型对齐的完整 UI 层级，以便快速迭代布局。

#### 验收标准

1. WHEN 运行 `Build UI Canvas` THEN 系统 SHALL 生成与需求 1-3 描述一致的完整 UI 层级（幂等操作，重复运行不创建重复节点）
2. WHEN 运行 `Build UI Canvas` THEN 系统 SHALL 自动连线所有 `[SerializeField]` 引用（TrackView、InventoryView、ItemDetailView、DragDropManager 等）
3. WHEN 运行 `Build UI Canvas` THEN 系统 SHALL 在 Console 输出完整的层级结构日志，列出所有创建/跳过的节点
4. IF 场景中已存在 StarChartPanel THEN 系统 SHALL 跳过重建，仅更新缺失的子节点

---

### 需求 6：Status Bar 通知系统

**用户故事：** 作为玩家，我希望装备/卸载部件时底部状态栏有文字反馈，以便确认操作结果。

#### 验收标准

1. WHEN 部件装备成功 THEN 系统 SHALL 在 Status Bar 显示 `EQUIPPED: [部件名]`，持续约 3 秒后淡出
2. WHEN 部件卸载成功 THEN 系统 SHALL 在 Status Bar 显示 `UNEQUIPPED: [部件名]`，持续约 3 秒后淡出
3. WHEN 装备失败（空间不足） THEN 系统 SHALL 在 Status Bar 显示 `NO SPACE: [部件名]`，文字颜色为红色
4. WHEN 新通知到来时旧通知仍在显示 THEN 系统 SHALL 立即替换旧通知文字，重置淡出计时器
5. WHEN Status Bar 无通知 THEN 系统 SHALL 显示默认提示文字（如 `DRAG TO EQUIP · CLICK TO INSPECT`），颜色为低透明度
