# 实施计划：SAIL 共享列布局重构

## 概览

将 SAIL 列从 TrackView 中独立出来，作为 `lc-body` 最左侧的共享列（`sail-col-shared`，固定宽），右侧 `TracksArea`（`flex:1`）包含 Primary 和 Secondary 两个 `track-block`，各自内部有 `track-block-label` + 3 列 `track-rows`（PRISM | CORE | SAT），与 HTML 原型布局完全对齐。

---

- [ ] 1. 清理 TrackView.cs：移除 SAIL 相关字段与逻辑
   - 删除 `_sailColumn`、`_sailHighlightLayer` 字段
   - 删除 `RefreshSailColumn()` 方法及其所有调用点
   - 删除 `HasSpaceForSail()` 中的 `isPrimary` 守卫（或整个方法）
   - 删除 `GetColumn(SlotType.LightSail)` 分支
   - 删除 `ClearAllHighlightTiles()` 中对 `_sailHighlightLayer` 的清理
   - _需求：3.1、3.2、3.3、3.4、3.5_

- [ ] 2. 修改 StarChartPanel.cs：接管共享 SAIL 列的引用与刷新
   - 新增 `[SerializeField] TypeColumn _sharedSailColumn` 字段
   - 新增 `[SerializeField] DragHighlightLayer _sailHighlightLayer` 字段（供拖拽高亮使用）
   - 在 `RefreshAllViews()` 中新增对 `RefreshSharedSailColumn()` 的调用
   - 实现 `RefreshSharedSailColumn()`：读取 `LoadoutSlot.GetEquippedLightSail()` 并更新共享列显示
   - _需求：4.1、4.2_

- [ ] 3. 修改 DragDropManager.cs：移除 SAIL 拖拽的 isPrimary 依赖
   - 找到 SAIL 拖拽处理逻辑中依赖 `DropTargetTrack.isPrimary` 的判断
   - 改为直接调用 `StarChartController.EquipLightSail()` / `UnequipLightSail()`，不再区分轨道
   - 拖入共享 SAIL 列时，高亮逻辑改用 `StarChartPanel._sailHighlightLayer`
   - _需求：4.3、4.4、4.5_

- [ ] 4. 修改 UICanvasBuilder.cs：重构 lc-body 布局生成逻辑
   - 在 `BuildLoadoutCard()` 中，按 HTML 原型顺序创建两个子节点：
     - `SharedSailColumn`：`TypeColumn` 组件 + `LayoutElement`（`flexibleWidth=0`，固定 `preferredWidth`）
     - `TracksArea`：`VerticalLayoutGroup` + `LayoutElement`（`flexibleWidth=1`）
   - `TracksArea` 内依次创建 `PrimaryTrackBlock` 和 `SecondaryTrackBlock`，每个 block 包含：
     - `track-block-label` TextMeshPro（Primary 显示 "PRIMARY [LMB]"，Secondary 显示 "SECONDARY [RMB]"）
     - `track-rows` HorizontalLayoutGroup，调用 `BuildTrackView()` 只构建 3 列（PRISM | CORE | SAT）
   - 移除原 `BuildTrackView()` 中 SAIL 列的构建代码
   - Wire 阶段：将 `SharedSailColumn` 的 `TypeColumn` 引用赋值给 `StarChartPanel._sharedSailColumn`
   - _需求：2.1、2.2、2.3、2.4、5.1、5.2、5.3、5.4、5.5_

- [ ] 5. 执行 Build UI Canvas 并全量验收
   - 执行菜单 `ProjectArk > Build UI Canvas`，确认 Hierarchy 层级符合预期：
     ```
     lc-body > SharedSailColumn（最左，固定宽）
     lc-body > TracksArea > PrimaryTrackBlock > track-block-label + track-rows（PRISM/CORE/SAT）
     lc-body > TracksArea > SecondaryTrackBlock > track-block-label + track-rows（PRISM/CORE/SAT）
     ```
   - 进入 Play Mode，验证 SAIL 拖拽装备/卸载功能正常（需求 1.3、1.4、1.5）
   - 验证 Secondary TrackView 中不再出现空 SAIL 格子（需求 1.2）
   - 验证 PRIMARY/SECONDARY 标识在 SAIL 列右侧（需求 2.3）
   - 追加实现日志到 `Docs/ImplementationLog/ImplementationLog.md`
   - _需求：1.1、1.2、1.3、1.4、1.5、2.3_
