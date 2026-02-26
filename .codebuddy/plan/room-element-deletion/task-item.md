# 实施计划：房间元素删除功能

> 所有修改均在 `Tools/LevelDesigner.html` 单文件内完成。

- [ ] 1. 新增元素选中状态全局变量与清除函数
   - 添加 `selectedElementRoomId` 和 `selectedElementIndex` 两个全局变量（初始值 `null` / `-1`），用于标识当前选中的元素
   - 编写 `clearElementSelection()` 函数，重置上述两个变量并移除元素的视觉高亮 CSS 类
   - 在现有的 `selectRoom()`、`selectConnection()`、`selectDoorLink()` 函数入口处调用 `clearElementSelection()`，确保四种选中状态互斥
   - _需求：1.2、1.5_

- [ ] 2. 实现元素点击选中交互与视觉高亮
   - 在房间元素的 mousedown/click 事件处理中，添加选中逻辑：记录 `selectedElementRoomId` 和 `selectedElementIndex`，并为被选中的 DOM 元素添加高亮 CSS 类（如 `element-selected`，蓝色边框/发光）
   - 添加 `.element-selected` CSS 样式（`outline: 2px solid #2196F3; box-shadow: 0 0 8px rgba(33,150,243,0.6)`）
   - 区分拖拽与点击：仅当鼠标未发生明显位移时才触发选中（利用已有的拖拽判断逻辑）
   - 选中元素时调用已有的清除房间/连接线/doorLink 选中函数，保持互斥
   - _需求：1.1、1.2、4.1_

- [ ] 3. 元素选中时属性面板显示元素信息
   - 编写 `showElementProperties(roomId, elementIndex)` 函数，在右侧属性面板中渲染：元素类型（带 emoji）、所属房间 ID、房间内坐标位置（x, y）、以及"🗑️ 删除元素"按钮
   - 在元素选中时调用该函数更新属性面板
   - 点击画布空白区域时清除元素选中并恢复属性面板默认状态
   - _需求：1.3、1.4_

- [ ] 4. 实现元素删除核心逻辑（含 doorLink 联动）
   - 编写 `deleteSelectedElement()` 函数：
     - 从目标房间的 `elements` 数组中 `splice` 移除选中元素
     - 如果被删除的是 door 类型元素，删除所有匹配 `roomId` 和 `doorIndex` 的 doorLinks
     - 遍历剩余 doorLinks，将同房间内 `doorIndex` 大于被删除索引的记录减 1，修正索引偏移
     - 清除选中状态、重新渲染房间、渲染 doorLinks、更新属性面板、更新 ASCII 预览
   - _需求：2.1、2.3、3.1、3.2、3.3_

- [ ] 5. 绑定删除触发方式（按钮 + 键盘）
   - 属性面板中的"删除元素"按钮绑定 `deleteSelectedElement()`
   - 在已有的 keydown 事件处理中，增加对 `selectedElementIndex >= 0` 的判断分支，Delete/Backspace 键触发 `deleteSelectedElement()`
   - 确保与已有的连接线删除、doorLink 删除键盘处理互不冲突（按优先级：元素 → doorLink → 连接线 → 房间）
   - _需求：2.1、2.2_

- [ ] 6. 边界情况处理：房间删除、画布清空、数据加载
   - 在 `deleteRoom()` 函数中：若被删除房间包含当前选中元素，调用 `clearElementSelection()`
   - 在 `clearCanvas()` 函数中：调用 `clearElementSelection()`
   - 在 `loadFromLocal()` 和 `importJSON()` 函数中：调用 `clearElementSelection()`
   - _需求：4.2、4.3、4.4_

- [ ] 7. 追加实现日志到 ImplementationLog.md
   - 按照项目规范在 `Docs/ImplementationLog/ImplementationLog.md` 末尾追加本次功能的实现记录
   - 包含：标题、修改文件列表、内容简述、目的、技术方案
   - _需求：项目规范强制步骤_
