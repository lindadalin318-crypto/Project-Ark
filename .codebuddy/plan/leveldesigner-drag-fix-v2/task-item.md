# 实施计划：LevelDesigner.html 拖拽功能修复 v3

- [ ] 1. 给 `#connections-svg` 添加 `dragover` 的 `e.preventDefault()`
   - 在 JS 初始化代码中找到 `svg`（即 `document.getElementById('connections-svg')`）的引用
   - 在 `canvasArea` 和 `world` 的 `dragover` 监听器附近，添加一行：`svg.addEventListener('dragover', (e) => { e.preventDefault(); })`
   - 确保 `pointer-events: none` 的 CSS 保持不变（该属性仅影响鼠标事件，不影响此修复）
   - _需求：1.3、1.4、2.4_

- [ ] 2. 验证完整 `dragover` 链路并补全缺失项
   - 检查 `canvasArea.addEventListener('dragover', ...)` 是否存在且调用了 `e.preventDefault()`（已有，确认保留）
   - 检查 `world.addEventListener('dragover', ...)` 是否存在且调用了 `e.preventDefault()`（已有，确认保留）
   - 检查 `setupRoomEvents` 中 `.room` 元素的 `dragover` 监听器是否存在且调用了 `e.preventDefault()`（上轮已修复，确认保留）
   - _需求：2.1、2.2、2.3_

- [ ] 3. 端到端验证拖拽功能恢复正常
   - 在浏览器中打开 `Tools/LevelDesigner.html`，从左侧面板拖拽房间类型（safe/normal/arena/boss）到画布空白区域，确认能成功创建房间
   - 拖拽房间元素（spawn/enemy/checkpoint）到已有房间上，确认能成功添加元素
   - 点击 SVG 连接线，确认连接线的点击选中交互未受影响
   - _需求：1.1、1.2、1.5_
