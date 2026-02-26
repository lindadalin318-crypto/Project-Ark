# 需求文档：房间元素删除功能

## 引言

LevelDesigner 工具当前支持通过拖拽将元素（spawn、door、enemy、chest、checkpoint、npc）添加到房间中，也支持拖拽移动元素位置，但**不支持删除元素**。本功能将为房间内的元素添加选中和删除能力，使设计师能够移除不需要的元素。

### 现状分析
- 元素目前可拖拽到房间中创建、可拖拽移动位置
- 没有元素选中状态（无 `selectedElement` 概念）
- 没有元素删除功能（无 `deleteElement` 相关函数）
- 门（door）元素被 `doorLinks` 数组通过 `doorIndex` 引用，删除门元素会导致索引偏移问题
- 已有三种选中状态互斥机制：`selectedRoom`、`selectedConnectionIndex`、`selectedDoorLinkIndex`

---

## 需求

### 需求 1：元素选中

**用户故事：** 作为一名关卡设计师，我希望能点击房间内的元素将其选中，以便查看其属性并进行后续操作（如删除）。

#### 验收标准

1. WHEN 用户点击房间内的某个元素 THEN 系统 SHALL 将该元素标记为选中状态，并以视觉高亮（如蓝色边框/发光）区分于未选中元素。
2. WHEN 用户选中某个元素 THEN 系统 SHALL 取消其他所有选中状态（selectedRoom、selectedConnectionIndex、selectedDoorLinkIndex），保持四种选中状态互斥。
3. WHEN 用户选中某个元素 THEN 属性面板 SHALL 显示该元素的信息（类型、所属房间、在房间内的坐标位置），并提供删除按钮。
4. WHEN 用户点击画布空白区域 THEN 系统 SHALL 取消元素的选中状态。
5. WHEN 用户选中房间、连接线或 doorLink THEN 系统 SHALL 取消元素的选中状态。

---

### 需求 2：元素删除 — 交互方式

**用户故事：** 作为一名关卡设计师，我希望能通过多种方式删除已选中的房间元素，以便快速清理不需要的元素。

#### 验收标准

1. WHEN 用户选中一个元素并点击属性面板中的"删除"按钮 THEN 系统 SHALL 从该房间的 `elements` 数组中移除该元素。
2. WHEN 用户选中一个元素并按下 Delete 或 Backspace 键 THEN 系统 SHALL 从该房间的 `elements` 数组中移除该元素。
3. WHEN 元素被删除后 THEN 系统 SHALL 取消选中状态、重新渲染房间、更新属性面板、更新 ASCII 预览。

---

### 需求 3：删除门元素时的 doorLink 联动清理

**用户故事：** 作为一名关卡设计师，我希望删除门元素时系统自动清理关联的 doorLink 虚线，以避免出现悬空引用。

#### 验收标准

1. WHEN 用户删除一个 door 类型的元素 THEN 系统 SHALL 删除所有引用该门元素的 doorLinks（匹配 `roomId` 和 `doorIndex`）。
2. WHEN 用户删除一个元素导致同房间后续元素的索引发生偏移 THEN 系统 SHALL 更新所有受影响的 doorLinks 中的 `doorIndex`（索引大于被删除元素索引的减 1）。
3. WHEN doorLinks 被清理或更新后 THEN 系统 SHALL 重新渲染所有 doorLink 虚线。

---

### 需求 4：选中状态持久性与边界情况

**用户故事：** 作为一名关卡设计师，我希望元素的选中和删除操作不会引发异常，在各种边界情况下都能正常工作。

#### 验收标准

1. IF 元素正在被拖拽移动 THEN 系统 SHALL 不触发选中行为（拖拽优先于点击选中）。
2. IF 被选中的元素所在的房间被删除 THEN 系统 SHALL 自动取消元素选中状态。
3. WHEN 画布被清空（Clear Canvas）THEN 系统 SHALL 重置元素选中状态。
4. WHEN 加载本地数据或导入 JSON THEN 系统 SHALL 重置元素选中状态。
