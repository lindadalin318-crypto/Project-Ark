# 实施计划：LevelDesigner 自定义元素

- [ ] 1. 新增自定义元素数据结构与 localStorage 持久化
   - 在脚本初始化区域声明 `customElementPresets` 数组（每项含 `label`、`color` 字段）
   - 实现 `saveCustomPresets()` 将数组序列化写入 `localStorage`（key: `leveldesigner_custom_elements`）
   - 实现 `loadCustomPresets()` 在页面加载时读取并恢复，无数据时初始化为空数组
   - _需求：4.1、4.2、4.3_

- [ ] 2. 在左侧面板添加"自定义元素"区块 HTML 与 CSS
   - 在左侧面板元素区域底部插入区块：标题"自定义元素"、名称输入框、颜色选择器（`<input type="color">`）、"添加"按钮
   - 添加自定义预设列表容器 `#custom-element-list` 及空状态提示文字
   - 编写对应 CSS：区块样式、预设项样式（左侧色条 + 深色半透明背景）、悬停时显示"×"删除按钮
   - _需求：1.1、1.3、3.1、3.3_

- [ ] 3. 实现自定义元素预设的创建与删除逻辑
   - 实现 `addCustomPreset()` 函数：校验名称非空（空时输入框边框变红）、创建预设对象、追加到 `customElementPresets`、调用 `renderCustomPresets()` 和 `saveCustomPresets()`、清空名称输入框
   - 实现 `renderCustomPresets()` 函数：遍历数组渲染预设项 DOM，每项绑定 dragstart 事件和删除按钮点击事件
   - 实现 `deleteCustomPreset(index)` 函数：从数组中移除对应项、重新渲染、持久化
   - _需求：1.2、1.3、1.4、3.1、3.2、3.3_

- [ ] 4. 实现自定义元素拖拽入画布与渲染
   - 在 `dragstart` 事件中携带 `customLabel` 和 `customColor` 数据（通过 `dataTransfer` 传递）
   - 修改 `drop` / `addElement` 逻辑：当 `type === "custom"` 时，从 dataTransfer 读取 `customLabel` 和 `customColor` 并存入元素对象
   - 修改 `renderElementIcon()` / 元素渲染函数：`type === "custom"` 时使用 `customColor` 作为背景色，显示截断后的 `customLabel`（超6字符加省略号）
   - _需求：2.1、2.2、2.3_

- [ ] 5. 实现自定义元素的属性面板编辑
   - 在元素属性面板中，当选中元素 `type === "custom"` 时，额外显示 `customLabel` 文本输入框和 `customColor` 颜色选择器
   - 绑定 `input` 事件，修改后实时更新元素数据并重新渲染房间内的元素图标
   - _需求：2.4_

- [ ] 6. 确保导出/导入 JSON 正确处理自定义元素字段
   - 检查 `getExportData()` 中元素序列化逻辑，确保 `type === "custom"` 时输出 `customLabel` 和 `customColor` 字段
   - 检查 `importJsonData()` 中元素反序列化逻辑，确保向后兼容（旧数据无 custom 字段时不报错），并在导入后正确渲染自定义元素颜色
   - _需求：5.1、5.2_
