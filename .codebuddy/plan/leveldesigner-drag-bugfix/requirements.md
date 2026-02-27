# 需求文档：LevelDesigner.html 拖拽功能 Bug 修复

## 引言

`Tools/LevelDesigner.html` 是 Project Ark 的关卡设计工具，支持将左侧面板的房间类型和房间元素拖拽到画布上进行关卡布局。当前版本存在多个 Bug，导致**所有拖拽操作完全失效**——无论是从左侧面板拖拽房间到画布，还是拖拽元素到房间内，均无法触发 drop 事件。

经过代码排查，共发现以下 5 个 Bug，需要全部修复。

---

## 需求

### 需求 1：修复 `.room` 元素上缺少 `dragover` 阻止默认行为

**用户故事：** 作为关卡设计师，我希望将左侧面板的元素拖拽到画布上的房间内，以便快速布置房间内容。

#### 验收标准

1. WHEN 用户拖拽元素经过 `.room` 元素上方 THEN 系统 SHALL 调用 `e.preventDefault()` 以允许 drop 事件触发。
2. WHEN 用户拖拽房间类型经过 `.room` 元素上方 THEN 系统 SHALL 不阻止 drop 事件冒泡到 `canvasArea`。
3. IF `.room` 元素没有 `dragover` 监听器 THEN 浏览器 SHALL 使用默认行为（禁止 drop），导致 `canvasArea` 的 `drop` 事件不触发。

---

### 需求 2：修复 `setupRoomEvents` 中 `mousedown` 的 `preventDefault` 干扰 HTML5 拖拽

**用户故事：** 作为关卡设计师，我希望点击房间可以选中并拖动房间，同时不影响从左侧面板拖拽元素到房间的操作。

#### 验收标准

1. WHEN 用户在 `.room` 上按下鼠标开始拖动房间 THEN 系统 SHALL 正确进入房间拖动模式。
2. WHEN 用户从左侧面板拖拽 `preset-item` 到画布 THEN 系统 SHALL 不因 `room` 的 `mousedown` 事件干扰 HTML5 drag & drop 流程。
3. WHEN `dragover` 事件在 `.room` 上触发 THEN 系统 SHALL 调用 `e.preventDefault()` 并阻止事件继续传播到浏览器默认处理。

---

### 需求 3：修复 `world` 元素缺少 `drop` 事件监听

**用户故事：** 作为关卡设计师，我希望拖拽操作在画布的任意位置都能正常触发，包括有房间覆盖的区域。

#### 验收标准

1. WHEN 用户将房间类型拖拽到 `world` 内的空白区域 THEN 系统 SHALL 正确创建新房间。
2. WHEN 用户将元素拖拽到 `world` 内的房间上 THEN 系统 SHALL 正确将元素添加到目标房间。
3. IF `world` 元素没有 `drop` 监听器 THEN 系统 SHALL 依赖 `canvasArea` 的 `drop` 事件冒泡处理（当前已有，但需确保冒泡路径畅通）。

---

### 需求 4：修复 `drop` 事件中 `elementType` 落点检测逻辑

**用户故事：** 作为关卡设计师，我希望将元素拖拽到房间内时，元素能准确放置在鼠标释放的位置，而不是偏移到错误位置。

#### 验收标准

1. WHEN 用户将元素拖拽到房间内并释放 THEN 系统 SHALL 使用 `document.elementFromPoint(e.clientX, e.clientY)` 精确检测目标房间，而不仅依赖坐标范围计算。
2. WHEN 目标房间检测失败（坐标不在任何房间内）THEN 系统 SHALL 回退到 `selectedRoom`（当前选中的房间）。
3. WHEN 元素被放置到房间内 THEN 元素的相对坐标 SHALL 基于鼠标在房间内的实际位置计算，确保视觉位置与数据一致。

---

### 需求 5：确保所有拖拽路径上的 `dragover` 均调用 `preventDefault`

**用户故事：** 作为关卡设计师，我希望拖拽操作在整个画布区域（包括 `canvasArea`、`world`、`.room` 元素）都能正常工作。

#### 验收标准

1. WHEN 用户拖拽任意 `preset-item` 经过 `canvasArea` THEN 系统 SHALL 调用 `e.preventDefault()`（已有，保留）。
2. WHEN 用户拖拽任意 `preset-item` 经过 `world` THEN 系统 SHALL 调用 `e.preventDefault()`（已有，保留）。
3. WHEN 用户拖拽任意 `preset-item` 经过 `.room` 元素 THEN 系统 SHALL 调用 `e.preventDefault()`（**缺失，需新增**）。
4. WHEN 用户拖拽任意 `preset-item` 经过 `.room` 内的子元素（如 `.connection-point`、`.resize-handle`、`.room-element`）THEN 系统 SHALL 通过事件冒泡确保 `dragover` 的 `preventDefault` 被调用。
5. AFTER 所有修复完成 THEN 用户 SHALL 能够成功将房间类型从左侧面板拖拽到画布并创建房间。
6. AFTER 所有修复完成 THEN 用户 SHALL 能够成功将房间元素从左侧面板拖拽到已有房间内并添加元素。
