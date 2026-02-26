# 需求文档：LevelDesigner.html Bug 修复与功能完整性审查

## 引言

LevelDesigner.html 是一个浏览器端关卡可视化编辑工具（2370 行），用于设计房间布局、连接关系和房间内元素配置。经过完整代码审查，发现以下 Bug 和功能完整性问题需要修复。本文档聚焦于**工具自身的 Bug 修复和功能完善**，不涉及与 Unity 端数据结构的对齐工作。

---

## 需求

### 需求 1：修复 importJsonData 未重置元素选中状态

**用户故事：** 作为一名关卡设计师，我希望导入 JSON 后所有选中状态被正确清除，以便不会出现幽灵选中引用导致后续操作异常。

#### 验收标准

1. WHEN 用户执行 JSON 导入 THEN `importJsonData` 函数 SHALL 重置 `selectedElementRoomId = null` 和 `selectedElementIndex = -1`
2. WHEN JSON 导入完成 THEN 属性面板 SHALL 显示默认提示文本而非残留的元素属性

#### 当前问题分析

`importJsonData` 函数（第 2051 行）中重置了 `selectedRoom`、`selectedConnectionIndex`、`selectedDoorLinkIndex`，但遗漏了 `selectedElementRoomId` 和 `selectedElementIndex`。对比 `loadFromLocal`（第 2165 行）和 `clearAll`（第 2190 行）均已正确重置这两个变量。

---

### 需求 2：修复 JSON 导出中 connections 和 doorLinks 直接引用内存对象

**用户故事：** 作为一名关卡设计师，我希望导出的 JSON 数据是干净的值拷贝，以便导出后的数据不会因为后续编辑操作而被意外修改，且导入时能正确还原。

#### 验收标准

1. WHEN `getExportData()` 导出 connections THEN 系统 SHALL 创建 connections 数组的值拷贝（包含 `from`、`to`、`fromDir`、`toDir` 字段），而非直接引用内存中的 connections 对象
2. WHEN `getExportData()` 导出 doorLinks THEN 系统 SHALL 创建 doorLinks 数组的值拷贝（包含 `roomId`、`entryDir`、`doorIndex` 字段），而非直接引用内存中的 doorLinks 对象
3. WHEN 导出完成后用户继续编辑画布 THEN 之前导出的 JSON 字符串 SHALL 不受影响

#### 当前问题分析

`getExportData()`（第 1972 行）中：
- `connections: connections` — 直接引用全局数组，不是值拷贝
- `doorLinks: doorLinks` — 直接引用全局数组，不是值拷贝

虽然在 `showExportModal` 中通过 `JSON.stringify` 序列化后即变成字符串不会被影响，但作为 `getExportData` 的返回值本身，如果其他地方调用此函数并持有引用，会导致数据污染。实时 JSON 预览 (`updateJsonLivePreview`) 也调用此函数，但仅用于 stringify 所以目前无副作用。为稳健性应修复。

---

### 需求 3：修复元素位置显示使用像素坐标而非网格单位

**用户故事：** 作为一名关卡设计师，我希望元素属性面板中的位置信息以网格单位显示（与房间属性面板的宽高保持一致），以便我能直观理解元素在房间中的相对位置。

#### 验收标准

1. WHEN 用户选中一个元素 THEN 属性面板中的位置 SHALL 显示为网格单位 `(x/GRID_SIZE, y/GRID_SIZE)` 或百分比，而非原始像素值
2. WHEN 元素位置在属性面板中显示 THEN 数值 SHALL 保留一位小数以提高可读性

#### 当前问题分析

`showElementProperties`（第 1735 行）显示 `Math.round(el.x)` 和 `Math.round(el.y)`，这是房间内的像素偏移值。对于用户来说，像素值没有意义，应该转换为网格单位（除以 GRID_SIZE=40）以与导出 JSON 中的 `position` 格式保持一致。

---

### 需求 4：修复连接属性面板中缺少房间名称显示

**用户故事：** 作为一名关卡设计师，我希望选中连接线后在属性面板中能看到起始和目标房间的名称（而非仅 ID），以便快速识别连接关系。

#### 验收标准

1. WHEN 用户选中一条连接线 THEN 属性面板 SHALL 显示起始房间和目标房间的名称（及 ID）
2. IF 房间名称不存在 THEN 系统 SHALL fallback 显示房间 ID

#### 当前问题分析

需要检查 `selectConnection` 中的属性面板渲染是否包含了房间名称。当前实现可能只显示了 roomId。

---

### 需求 5：确保所有可选中对象的选中状态互斥

**用户故事：** 作为一名关卡设计师，我希望同一时刻只能有一个对象（房间/连接/doorLink/元素）处于选中状态，以便不会出现多个属性面板冲突或操作混乱。

#### 验收标准

1. WHEN 用户点击画布空白区域 THEN 所有选中状态（房间、连接、doorLink、元素）SHALL 被清除，属性面板回到默认状态
2. WHEN 用户选中元素后再选中连接线 THEN 元素的选中高亮 SHALL 被清除
3. WHEN 用户选中连接线后再选中房间 THEN 连接线的选中高亮 SHALL 被清除
4. WHEN 用户选中 doorLink 后再选中元素 THEN doorLink 的选中高亮 SHALL 被清除

#### 当前问题分析

各 `selectXxx` 函数中已有互斥清除逻辑，但需要验证**点击画布空白区域**时是否正确取消所有选中状态。当前代码中 canvas 区域的 mousedown 事件开始拖拽/平移逻辑，click 事件在世界区域 `world` 上可能未处理空白区域点击的取消选中。

---

### 需求 6：清理冗余代码与样式

**用户故事：** 作为一名开发者，我希望代码中没有未使用的函数、变量或 CSS 样式，以便代码保持精简可维护。

#### 验收标准

1. WHEN 代码审查完成 THEN 所有未被调用的函数 SHALL 被移除
2. WHEN 代码审查完成 THEN 所有未被引用的 CSS 类 SHALL 被移除
3. WHEN 代码审查完成 THEN 所有注释掉的旧代码块 SHALL 被移除

#### 当前问题分析

需要进行全面扫描，检查是否存在：
- 定义但从未调用的函数
- 定义但从未使用的 CSS 类
- 注释掉的旧代码残留

---

## 边界情况

- **导入向后兼容**：导入旧格式 JSON（缺少某些字段如 `floor`）时应使用合理默认值，不报错
- **空画布操作**：在没有任何房间时执行导出、保存、清空等操作应安全处理
- **极端数据**：房间元素数量为 0 时的属性面板显示、doorLinks 引用已删除的门元素时的容错
- **选中状态持久性**：在拖拽移动元素/房间后，选中状态应保持不丢失
