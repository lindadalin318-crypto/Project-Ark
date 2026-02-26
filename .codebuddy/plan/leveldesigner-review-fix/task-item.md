# 实施计划：LevelDesigner.html Bug 修复与功能完整性

- [ ] 1. 修复 `importJsonData` 中遗漏的元素选中状态重置
   - 在 `importJsonData` 函数的状态重置区域（`selectedRoom = null` 附近）追加 `selectedElementRoomId = null` 和 `selectedElementIndex = -1`
   - 确认 `clearElementSelection()` 或等效逻辑被调用，属性面板恢复默认提示
   - _需求：1.1、1.2_

- [ ] 2. 修复 `getExportData()` 中 connections 和 doorLinks 的值拷贝问题
   - 将 `connections: connections` 改为 `connections: connections.map(c => ({from: c.from, to: c.to, fromDir: c.fromDir, toDir: c.toDir}))`
   - 将 `doorLinks: doorLinks` 改为 `doorLinks: doorLinks.map(d => ({roomId: d.roomId, entryDir: d.entryDir, doorIndex: d.doorIndex}))`
   - 同时检查 rooms 数组中的 elements 是否也存在引用问题，如有则一并修复
   - _需求：2.1、2.2、2.3_

- [ ] 3. 修复元素属性面板中位置坐标的显示单位
   - 在 `showElementProperties` 函数中，将 `Math.round(el.x)` 和 `Math.round(el.y)` 改为 `(el.x / GRID_SIZE).toFixed(1)` 和 `(el.y / GRID_SIZE).toFixed(1)`
   - 更新标签文本从 "位置" 改为 "位置(网格)" 以明确单位
   - _需求：3.1、3.2_

- [ ] 4. 完善连接属性面板中的房间名称显示
   - 在 `selectConnection` 的属性面板渲染代码中，根据 `conn.from` 和 `conn.to` 查找对应房间对象
   - 显示格式改为 "房间名称 (ID)" 或在名称不存在时 fallback 到纯 ID
   - _需求：4.1、4.2_

- [ ] 5. 验证并修复选中状态互斥逻辑
   - 检查 `selectRoom`、`selectConnection`、`selectDoorLink`、元素选中函数中是否正确调用了其他三种选中状态的清除函数
   - 检查画布空白区域点击（`world` 的 click 事件 或 canvas 的 mouseup 事件）是否清除所有四种选中状态
   - 如有缺失则补充互斥清除调用和空白区域取消选中逻辑
   - _需求：5.1、5.2、5.3、5.4_

- [ ] 6. 扫描并清理冗余代码
   - 列出所有 `function xxx` 定义，用 grep 逐一检查是否被调用，移除未使用的函数
   - 列出所有 CSS 类定义（`.xxx {`），检查是否在 HTML 或 JS 中被引用，移除未使用的样式
   - 检查并移除注释掉的旧代码块（连续 `//` 注释超过 3 行的区域）
   - _需求：6.1、6.2、6.3_

- [ ] 7. 更新 ImplementationLog.md 记录本次修复
   - 追加本次 Bug 修复和功能完善的日志条目
   - 包含修改的文件路径、内容简述、目的和技术方案
   - _需求：全部_
