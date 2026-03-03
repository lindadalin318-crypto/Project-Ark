# 需求文档：StarChart UI 完善（对齐 HTML 原型）

## 引言

StarChart（星图）是 Project Ark 的核心装备定制界面。当前 Unity 实现与 `StarChartUIPrototype.html` 原型之间存在若干视觉与功能差距。本次需求覆盖差距项 **1、2、3、4、7、8、9**，目标是让 Unity 实现在功能完整性和视觉表现上全面对齐 HTML 原型，提升策略深度与操作手感。

---

## 需求

### 需求 1：异形部件 2D Shape 系统

**用户故事：** 作为玩家，我希望部件能以 1×1、1×2、2×1、L形、2×2 等多种形状占据星图轨道的二维格子，以便在装配时产生真实的空间策略博弈感（类 Backpack Battles 体验）。

#### 验收标准

1. WHEN 系统初始化 StarChart 数据模型 THEN 系统 SHALL 将每条轨道（Track）的槽位表示为 `grid[row][col]`（2行×N列）的二维矩阵，而非一维线性数组。
2. WHEN 定义部件 SO 时 THEN 系统 SHALL 支持 5 种形状枚举：`Shape1x1`、`Shape1x2H`（水平）、`Shape2x1V`（垂直）、`ShapeL`（L形）、`Shape2x2`，每种形状对应一组相对坐标偏移集合。
3. WHEN 玩家将部件拖拽到轨道格子上 THEN 系统 SHALL 根据部件形状的 bounding box 计算所有被覆盖的格子，并以覆盖层（Overlay）方式渲染部件图标，跨越多个格子。
4. WHEN 拖拽预览时目标位置有效 THEN 系统 SHALL 高亮所有被覆盖格子为绿色（valid）；IF 有旧部件被驱逐 THEN 系统 SHALL 高亮为橙色（replace）；IF 超出边界或形状不合法 THEN 系统 SHALL 高亮为红色（invalid）。
5. WHEN 玩家确认放置且目标格子已有旧部件 THEN 系统 SHALL 将被覆盖的旧部件驱逐回库存（EvictedItems），并在放置动画完成后更新库存列表。
6. WHEN 拖拽 Ghost 显示时 THEN 系统 SHALL 根据部件形状的 bounding box 动态调整 Ghost 的宽高，并在 Ghost 内部渲染形状格子示意图（半透明网格）。
7. WHEN 库存中显示部件缩略图 THEN 系统 SHALL 在缩略图内叠加该部件形状的半透明格子预览。

---

### 需求 2：Loadout 命名 / 删除 / 保存操作

**用户故事：** 作为玩家，我希望能对每个 Loadout 进行重命名、删除和手动保存，以便管理多套装配方案并清晰区分它们的用途。

#### 验收标准

1. WHEN StarChart 面板打开 THEN 系统 SHALL 在 Loadout 区域显示 **RENAME**、**DELETE**、**SAVE CONFIG** 三个操作按钮。
2. WHEN 玩家点击 RENAME THEN 系统 SHALL 弹出输入框（InputField 弹窗或内联编辑），允许玩家输入新名称，确认后更新 Loadout 标题显示。
3. WHEN 玩家点击 DELETE 且当前 Loadout 数量大于 1 THEN 系统 SHALL 删除当前 Loadout 并切换到相邻 Loadout。
4. IF 当前只剩 1 个 Loadout THEN 系统 SHALL 禁用 DELETE 按钮（灰色不可点击），并显示 tooltip 提示"至少保留一个配置"。
5. WHEN 玩家点击 SAVE CONFIG THEN 系统 SHALL 将当前 Loadout 数据持久化到 SaveManager，并短暂显示"SAVED"确认反馈（0.8 秒后消失）。

---

### 需求 3：Drum Counter 翻牌动画

**用户故事：** 作为玩家，我希望 Loadout 编号计数器在切换时呈现类似机械翻牌的 3D 旋转动画，以便增强操作的仪式感和反馈质感。

#### 验收标准

1. WHEN Loadout 编号发生变化 THEN 系统 SHALL 播放翻牌动画：旧数字以 `rotateX(-90deg)` 翻出，新数字以 `rotateX(0deg)` 翻入，总时长 ≤ 300ms。
2. WHEN 向上切换 Loadout（编号增大）THEN 系统 SHALL 播放"向上翻"动画（旧数字向上翻出，新数字从下方翻入）。
3. WHEN 向下切换 Loadout（编号减小）THEN 系统 SHALL 播放"向下翻"动画（旧数字向下翻出，新数字从上方翻入）。
4. WHEN 翻牌动画播放时 THEN 系统 SHALL 使用 PrimeTween 驱动 `RectTransform` 的 `localEulerAngles.x`，不使用手写 Lerp 或 Coroutine。
5. WHEN 翻牌动画播放时 THEN 系统 SHALL 在计数器容器上应用 `perspective` 效果（通过 Canvas 3D 旋转模拟），使翻牌具有透视纵深感。

---

### 需求 4：Loadout Card 切换 Scale 纵深感

**用户故事：** 作为玩家，我希望 Loadout 卡片在切换时除了位移动画外还有缩放变化，以便营造卡片从远处飞来/飞走的纵深感。

#### 验收标准

1. WHEN Loadout 卡片退出（切换离开）THEN 系统 SHALL 在位移动画的同时将卡片 Scale 从 1.0 缩小到 0.88，模拟向远处退去。
2. WHEN Loadout 卡片进入（切换到来）THEN 系统 SHALL 将卡片 Scale 从 0.88 增大到 1.0，模拟从远处飞来。
3. WHEN Scale 动画播放 THEN 系统 SHALL 使用 PrimeTween `Tween.Scale` 与位移动画并行执行（同一时间轴），总时长与现有位移动画保持一致。
4. WHEN 动画完成 THEN 系统 SHALL 确保卡片 Scale 精确还原为 `Vector3.one`，不产生累积误差。

---

### 需求 7：拖拽预览高亮的三态区分

**用户故事：** 作为玩家，我希望在拖拽部件时能通过颜色直观区分"可放置"、"会替换旧部件"、"不可放置"三种状态，并在替换时看到具体的替换数量提示，以便做出更准确的装配决策。

#### 验收标准

1. WHEN 玩家拖拽部件悬停在有效目标格子上且无冲突 THEN 系统 SHALL 将目标格子高亮为**绿色**（`preview-valid`），Ghost 边框同步变为绿色。
2. WHEN 玩家拖拽部件悬停在目标格子上且会驱逐已装备部件 THEN 系统 SHALL 将目标格子高亮为**橙色**（`preview-replace`），Ghost 边框同步变为橙色，并在 Ghost 上显示替换提示文字 `↺ 替换 N 个部件`（N 为被驱逐部件数量）。
3. WHEN 玩家拖拽部件悬停在无效位置（超出边界、形状不合法、类型不匹配）THEN 系统 SHALL 将目标格子高亮为**红色**（`preview-invalid`），Ghost 边框同步变为红色。
4. WHEN 拖拽结束（放置成功、取消或离开有效区域）THEN 系统 SHALL 立即清除所有格子的高亮状态，Ghost 边框恢复默认色。
5. WHEN Ghost 边框颜色切换时 THEN 系统 SHALL 使用 PrimeTween 进行颜色插值过渡（时长 ≤ 80ms），而非瞬间跳变。

---

### 需求 8：库存过滤器按钮顺序对齐

**用户故事：** 作为玩家，我希望库存区域的过滤器按钮顺序与轨道列顺序一致（ALL → SAILS → PRISMS → CORES → SATS），以便在视觉上形成直觉映射，快速定位目标部件。

#### 验收标准

1. WHEN 库存面板渲染过滤器按钮 THEN 系统 SHALL 按照 `ALL → SAILS → PRISMS → CORES → SATS` 的顺序从左到右排列。
2. WHEN 过滤器按钮顺序与轨道列顺序一致 THEN 系统 SHALL 确保两者使用相同的枚举/常量定义作为单一数据源，避免硬编码导致的顺序不一致。

---

### 需求 9：状态栏提示文字补全

**用户故事：** 作为玩家，我希望状态栏能完整显示所有操作提示（包括"拖拽槽位到库存以卸装"），以便在不熟悉操作时能通过界面自学。

#### 验收标准

1. WHEN StarChart 面板处于默认待机状态 THEN 系统 SHALL 在状态栏显示完整提示：`EQUIPPED X/10 · INVENTORY X ITEMS · DRAG TO EQUIP · CLICK TO INSPECT · DRAG SLOT TO INVENTORY TO UNEQUIP`。
2. WHEN 玩家开始拖拽已装备槽位 THEN 系统 SHALL 将状态栏切换为高亮提示：`DRAG TO INVENTORY TO UNEQUIP`，引导玩家完成卸装操作。
3. WHEN 拖拽结束（无论成功或取消）THEN 系统 SHALL 将状态栏恢复为默认待机文字。
