# 实施计划：LevelDesigner.html 效率升级

- [ ] 1. 扩展房间数据模型与属性面板（新增字段）
   - 在 `createRoom()` 函数中为 room 对象新增字段：`zoneId`、`act`、`tension`、`beatName`、`timeRange`，默认值均为空/0
   - 在右侧属性面板 HTML 中新增对应输入控件：Zone ID 文本框、ACT 下拉（ACT1-ACT5）、张力滑块（1-10）、叙事乐章文本框、时长估算文本框
   - 在 `selectRoom()` 中将新字段值填入对应控件
   - 在 `updateRoomProperty()` 中处理新字段的更新逻辑
   - 修改 `getExportData()` 中 rooms 的序列化逻辑，仅在字段非空时输出对应 key
   - _需求：1.1、1.2、1.3、1.4_

- [ ] 2. 扩展房间类型与元素类型（新增 narrative / puzzle / corridor / tidal_door 等）
   - 在左侧面板 HTML 中新增 3 个房间类型预设项：`narrative`（蓝紫色）、`puzzle`（青色）、`corridor`（灰色）
   - 在 CSS 中为新房间类型添加对应的 `.room.narrative`、`.room.puzzle`、`.room.corridor` 样式
   - 在 `getTypeLabel()` 中添加新类型的中文标签
   - 在属性面板的类型下拉 `<select>` 中新增 3 个 `<option>`
   - 在左侧面板 HTML 中新增 4 个元素类型预设项：`tidal_door`（蓝绿色）、`resonance_crystal`（金色）、`lore_audio`（白色）、`black_water`（深黑色）
   - 在 CSS 中为新元素类型添加对应的 `.element-tidal_door` 等样式
   - 在 `getElementIcon()` 中为新元素类型添加对应图标
   - _需求：2.1、2.2、2.3、3.1、3.2_

- [ ] 3. 实现撤销（Ctrl+Z）与复制（Ctrl+D）快捷键
   - 新增 `undoStack` 数组（最大 50 条）和 `pushUndoState()` 函数，在每次状态变更前（添加/删除/移动房间、添加/删除连接、添加/删除元素）调用以保存快照
   - 新增 `undo()` 函数，从 `undoStack` 弹出最近快照并恢复 `rooms`、`connections`、`doorLinks` 状态，然后重新渲染
   - 新增 `duplicateRoom()` 函数，深拷贝选中房间的所有属性和元素，生成新 ID（`_copy` 后缀），偏移 40px 放置，不复制连接关系
   - 在 `keydown` 事件处理中新增：`Ctrl+Z` → `undo()`，`Ctrl+D` → `duplicateRoom()`，`Ctrl+S` → `saveToLocal()`，`Escape` → 取消连接操作
   - _需求：5.1、5.2、5.3、5.4、5.5_

- [ ] 4. 实现 ACT 分组框可视化
   - 新增 `renderActGroups()` 函数，遍历所有有 `act` 字段的房间，按 ACT 分组计算包围盒（min/max x/y + 20px padding）
   - 在 `#world` div 中动态创建/更新 `.act-group-box` div 元素（每个 ACT 一个），使用 `position: absolute` 和半透明背景色，z-index 低于房间
   - 在 CSS 中为 5 个 ACT 分组框定义不同的半透明颜色和边框样式，左上角显示 ACT 标签
   - 在所有会改变房间位置/ACT 属性的操作后调用 `renderActGroups()`（包括拖拽移动、属性修改、导入、撤销）
   - _需求：6.1、6.2、6.3、6.4、6.5_

- [ ] 5. 实现心流张力折线图
   - 在右侧面板 `section-ascii` 上方新增 `section-tension` 区域，包含一个 `<canvas>` 元素（高度约 80px）
   - 新增 `renderTensionChart()` 函数：收集所有有 `tension > 0` 的房间，按 Zone ID 字母数字排序（Z1a < Z1b < Z2a），在 canvas 上绘制折线图
   - 折线图节点显示 Zone ID 标签（旋转 45°）和张力数值，Y 轴范围 1-10
   - 当相邻节点张力差 > 4 时，该段连线使用高亮颜色（如橙色）
   - 在 `updateAsciiPreview()` 末尾调用 `renderTensionChart()`，确保每次数据变更后图表实时更新
   - _需求：4.1、4.2、4.3、4.4、4.5_

- [ ] 6. 实现连接线语义类型
   - 在 `connections` 数组的每个连接对象中新增 `connectionType` 字段（默认 `normal`）
   - 在 `showConnectionProperties()` 中新增连接类型下拉选择（`normal` / `tidal` / `locked` / `one_way`），并绑定 `onchange` 事件更新连接数据
   - 在 CSS 中为不同连接类型定义对应的 SVG stroke 颜色（`tidal`=蓝绿、`locked`=黄色、`one_way`=橙色）
   - 修改 `renderConnections()` 中的线条颜色逻辑，根据 `connectionType` 选择对应颜色和 SVG marker
   - 修改 `getExportData()` 中 connections 的序列化，输出 `connectionType` 字段
   - _需求：7.1、7.2、7.3_

- [ ] 7. 修复 importJsonData 遗漏重置元素选中状态（顺带修复已知 Bug）
   - 在 `importJsonData()` 函数中补充重置 `selectedElementRoomId = null` 和 `selectedElementIndex = -1`
   - 修改 `getExportData()` 中 `connections` 和 `doorLinks` 为值拷贝（`.map()` 展开字段）
   - 修改 `showElementProperties()` 中位置显示为网格单位（`el.x / GRID_SIZE`）而非像素值
   - 在 `importJsonData()` 中支持新增字段的导入（`zoneId`、`act`、`tension`、`beatName`、`timeRange`、`connectionType`），缺失时使用默认值
   - _需求：1.3（向后兼容）、Bug 修复_
