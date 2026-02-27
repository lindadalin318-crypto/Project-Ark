# 实施计划：LevelDesigner.html 拖拽功能 Bug 修复

- [ ] 1. 在 `setupRoomEvents` 中为 `.room` 元素添加 `dragover` 监听器
   - 在创建 `div` 后添加 `div.addEventListener('dragover', (e) => { e.preventDefault(); })` 
   - 确保 `.room` 内的子元素（`.connection-point`、`.resize-handle`、`.room-element`）的 `dragover` 事件能冒泡到 `.room` 并触发 `preventDefault`
   - _需求：1.1、1.2、5.3、5.4_

- [ ] 2. 修复 `setupRoomEvents` 中 `mousedown` 的 `e.preventDefault()` 干扰问题
   - 检查 `mousedown` 中调用 `e.preventDefault()` 的位置，仅在需要阻止浏览器默认行为（如文本选中）时保留，不影响 HTML5 drag & drop 的 `dragstart` 事件
   - 确认房间拖动（自定义 mousemove 逻辑）与 HTML5 drag & drop（preset-item 拖入）两套流程互不干扰
   - _需求：2.1、2.2、2.3_

- [ ] 3. 确保 `canvasArea` 和 `world` 的 `dragover` 监听器完整保留
   - 验证 `canvasArea.addEventListener('dragover', ...)` 和 `world.addEventListener('dragover', ...)` 均已调用 `e.preventDefault()`
   - 如缺失则补充，确保整条冒泡链路（`.room` → `world` → `canvasArea`）全部允许 drop
   - _需求：5.1、5.2、3.3_

- [ ] 4. 修复 `drop` 事件中 `elementType` 的目标房间检测逻辑
   - 将坐标范围计算替换为 `document.elementFromPoint(e.clientX, e.clientY)` 精确命中检测
   - 向上遍历 DOM 找到最近的 `.room` 父元素作为目标房间
   - 当 `elementFromPoint` 未命中任何房间时，回退到 `selectedRoom`
   - _需求：4.1、4.2、4.3_

- [ ] 5. 端到端验证所有拖拽场景
   - 在浏览器中打开 `Tools/LevelDesigner.html`，验证从左侧面板拖拽房间类型到画布空白区域能成功创建房间
   - 验证从左侧面板拖拽房间元素到已有房间内能成功添加元素
   - 验证房间的自定义拖动（mousedown + mousemove）仍然正常工作，未被影响
   - _需求：5.5、5.6、2.1、3.1、3.2_
