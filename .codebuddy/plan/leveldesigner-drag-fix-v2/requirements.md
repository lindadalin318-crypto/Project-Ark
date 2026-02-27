# 需求文档：LevelDesigner.html 拖拽功能修复 v3（精准定位版）

## 引言

`Tools/LevelDesigner.html` 的拖拽功能（从左侧面板拖拽房间类型/元素到画布）在 **2026-02-26 12:46** 的"全面代码审查与Bug修复"后失效。

### 根因追溯

该次修改删除了冗余的 `<canvas id="grid-canvas">` 及其关联代码。删除后，`#connections-svg` 成为画布上唯一覆盖全区域的顶层元素（`position: absolute; width: 100%; height: 100%; z-index: 5`）。

**关键误解**：`#connections-svg` 的 CSS `pointer-events: none` 只对鼠标事件（click/mousemove/mouseenter 等）有效，**对 HTML5 Drag & Drop 事件（dragover/drop）完全无效**。

因此，当用户拖拽 `preset-item` 到画布时：
1. 浏览器 drag 命中测试找到最顶层元素 → `#connections-svg`（z-index:5）
2. `#connections-svg` 没有绑定 `dragover` 的 `e.preventDefault()`
3. 浏览器认为该区域不接受 drop → 整个拖拽链路断裂，`canvasArea` 的 `drop` 事件永远不触发

在 `gridCanvas` 存在时，`gridCanvas` 是顶层元素，它绑定了 `dragover` 的 `preventDefault()`（通过 `canvasArea` 的冒泡），所以拖拽正常工作。删除 `gridCanvas` 后，SVG 成为顶层，问题暴露。

### 修复方案

只需一行代码：给 `svg`（`#connections-svg`）添加 `dragover` 的 `e.preventDefault()`，让 drop 事件能穿透到 `canvasArea` 的 `drop` 处理器。

---

## 需求

### 需求 1：修复 `#connections-svg` 拦截 HTML5 Drag & Drop 事件

**用户故事：** 作为关卡设计师，我希望将左侧面板的房间类型和元素拖拽到画布上，以便快速创建和配置关卡布局。

#### 验收标准

1. WHEN 用户从左侧面板拖拽房间类型（safe/normal/arena/boss 等）到画布空白区域 THEN 系统 SHALL 在鼠标释放位置创建对应类型的新房间。
2. WHEN 用户从左侧面板拖拽房间元素（spawn/enemy/checkpoint 等）到已有房间上 THEN 系统 SHALL 将元素添加到目标房间内。
3. WHEN 用户拖拽 `preset-item` 经过 `#connections-svg` 上方 THEN `#connections-svg` SHALL 调用 `dragover` 事件的 `e.preventDefault()`，允许 drop 事件冒泡到 `canvasArea`。
4. IF `#connections-svg` 的 `pointer-events: none` CSS 属性存在 THEN 该属性 SHALL 继续保留（仅影响鼠标事件，不影响 drag 事件修复）。
5. AFTER 修复完成 THEN SVG 连接线的点击选中交互（`pointer-events: stroke`）SHALL 不受影响，仍然正常工作。

---

### 需求 2：确保 `canvasArea` 和 `world` 的 `dragover` 链路完整

**用户故事：** 作为关卡设计师，我希望拖拽操作在整个画布区域（包括有房间覆盖的区域和空白区域）都能正常响应。

#### 验收标准

1. WHEN 用户拖拽 `preset-item` 经过 `canvasArea` THEN `canvasArea` SHALL 调用 `dragover` 的 `e.preventDefault()`（已有，保留）。
2. WHEN 用户拖拽 `preset-item` 经过 `#world` THEN `#world` SHALL 调用 `dragover` 的 `e.preventDefault()`（已有，保留）。
3. WHEN 用户拖拽 `preset-item` 经过 `.room` 元素 THEN `.room` SHALL 调用 `dragover` 的 `e.preventDefault()`（已有，保留）。
4. AFTER 所有修复完成 THEN 完整的 dragover 链路 SHALL 为：`preset-item` → `#connections-svg`（新增）→ `#world` → `.room`（已有）→ `canvasArea`（已有）。
