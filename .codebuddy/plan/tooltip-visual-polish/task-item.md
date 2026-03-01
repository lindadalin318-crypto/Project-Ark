# 实施计划：StarChart Tooltip 视觉优化

- [ ] 1. 修改 UICanvasBuilder 中 Tooltip 的尺寸与字号
   - 将 tooltip 容器 `sizeDelta` 从 `(220, 180)` 改为 `(280, 230)`
   - 将 NameText 字号从 `14` 改为 `17`
   - 将 TypeText 字号从 `10` 改为 `12`
   - 将 StatsText 字号从 `11` 改为 `13`
   - 将 DescriptionText 字号从 `10` 改为 `12`
   - 将 EquippedStatusText 字号从 `10` 改为 `12`
   - 将 ActionHintText 字号从 `9` 改为 `11`
   - _需求：1.1、2.1、2.2、2.3、2.4、2.5、2.6_

- [ ] 2. 修改 UICanvasBuilder 中 Tooltip 的颜色风格
   - 将 TooltipBackground 背景色改为 `(0.04f, 0.07f, 0.14f, 0.97f)`
   - 将 TooltipBorder 边框色改为 `(0.2f, 0.6f, 1f, 0.85f)`
   - 将 TypeBadge 背景色改为 `(0f, 0.5f, 1f, 0.15f)`
   - _需求：3.1、3.2、3.3_

- [ ] 3. 同步更新 ItemTooltipView 中的边界检测尺寸常量
   - 将 `_tooltipWidth` 默认值从 `220f` 改为 `280f`
   - 将 `_tooltipHeight` 默认值从 `180f` 改为 `230f`
   - _需求：1.2_

- [ ] 4. 运行 UICanvasBuilder 并验证效果
   - 执行菜单 `ProjectArk > Build UI Canvas` 重建场景 UI
   - 进入 Play Mode，打开星图（C 键），hover 部件卡片确认 tooltip 尺寸、字号、颜色均符合预期
   - 确认 tooltip 边界检测正常（靠近屏幕边缘时不超出屏幕）
   - _需求：1.1、1.2、2.1–2.6、3.1–3.4_
