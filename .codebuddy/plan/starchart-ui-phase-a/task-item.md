# 实施计划：StarChart UI Phase A — 视觉样式与布局对齐

- [ ] 1. 新建 `StarChartTheme.cs` 颜色主题静态类
   - 定义所有主题色常量：背景色 `#0d1117`、边框色、青色 `#00e5ff`
   - 定义四种类型主题色及其 dim/glow 变体：CORE `#60a5fa`、PRISM `#a78bfa`、SAIL `#34d399`、SAT `#fbbf24`
   - 实现 `GetTypeColor(StarChartItemType type)` 静态方法
   - _需求：4.1、4.2、4.3_

- [ ] 2. 新建 `StatusBarView.cs` 通知组件
   - `[SerializeField] TMP_Text _label`，公开 `ShowMessage(string text, Color color, float duration)` 方法
   - 使用 PrimeTween 实现 3 秒后淡出，新消息到来时立即打断旧动画
   - 无消息时显示默认提示文字（低透明度）
   - _需求：6.1、6.2、6.3、6.4、6.5_

- [ ] 3. 重构 `SlotCellView.cs` 视觉样式
   - 新增 `SetThemeColor(Color typeColor)` 方法，装备状态下背景使用类型主题色（低透明度）
   - 新增 `SetEmpty()` 时显示 `+` 占位符文字（低透明度白色）
   - 悬停高亮改为使用传入的类型主题色边框（替换现有固定绿/红色）
   - _需求：2.5、2.6、2.7_

- [ ] 4. 重构 `TrackView.cs` 布局与样式
   - 在 Inspector 中新增 `[SerializeField] TMP_Text _trackLabel` 显示 `PRIMARY` / `SECONDARY`（青色 `#00e5ff`）
   - 为 PrismRow 和 CoreRow 各自添加类型标签文字（`PRISM` / `CORE`），颜色使用 `StarChartTheme.GetTypeColor`
   - `Refresh()` 中调用 `cell.SetThemeColor(StarChartTheme.GetTypeColor(layerType))` 传入对应类型色
   - 空槽位调用 `cell.SetEmpty()` 显示 `+` 占位符
   - _需求：2.1、2.2、2.3、2.4、2.5、2.6_

- [ ] 5. 重构 `InventoryItemView.cs` 视觉样式
   - 左上角添加类型色点 `Image`（`StarChartTheme.GetTypeColor`，圆形小点）
   - 右下角添加装备标记 `TMP_Text`（`✓`，绿色），已装备时显示，未装备时隐藏
   - 已装备时格子边框变为绿色低透明度
   - 悬停时 scale 1.06 放大（PrimeTween），离开时恢复
   - 选中时显示青色发光边框 `#00e5ff`
   - _需求：3.2、3.3、3.5、3.6_

- [ ] 6. 重构 `InventoryView.cs` 过滤栏样式
   - 过滤按钮激活态：使用对应类型主题色背景 + 边框高亮（通过 `StarChartTheme.GetTypeColor`）
   - 非激活态：统一暗色背景
   - `GridLayoutGroup` 格子大小确认为 64×64px，间距 6px
   - _需求：3.1、3.4_

- [ ] 7. 重构 `UICanvasBuilder.cs` 生成与原型对齐的完整层级
   - 7.1 面板根层级：深色背景 Image（`StarChartTheme.BgDeep`）+ 四角 L 形装饰括号（4 个 `Image`）
   - 7.2 Header 栏：左侧 `STAR CHART` 标题、中央绿色脉冲状态点 `Image`、右侧关闭按钮
   - 7.3 上半区（55%）：PRIMARY + SECONDARY 两个 `TrackView` 并排，中间竖向分隔线
   - 7.4 下半区（40%）：`InventoryView`（含过滤栏 + `GridLayoutGroup` 网格）
   - 7.5 底部 `StatusBarView`（22px 高）
   - 所有 `[SerializeField]` 引用自动连线，幂等操作（已存在节点跳过重建）
   - _需求：1.1、1.2、1.3、1.4、1.5、5.1、5.2、5.3、5.4_

- [ ] 8. 将 `StatusBarView` 接入 `StarChartPanel.cs`
   - `StarChartPanel` 新增 `[SerializeField] StatusBarView _statusBar`
   - 装备成功时调用 `_statusBar.ShowMessage("EQUIPPED: " + item.name, white, 3f)`
   - 卸载成功时调用 `_statusBar.ShowMessage("UNEQUIPPED: " + item.name, white, 3f)`
   - 装备失败时调用 `_statusBar.ShowMessage("NO SPACE: " + item.name, red, 3f)`
   - _需求：6.1、6.2、6.3、6.4_
