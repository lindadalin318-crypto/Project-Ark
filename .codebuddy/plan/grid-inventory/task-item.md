# 实施计划：网格背包 & 异形部件系统

- [ ] 1. 重构数据模型：为部件添加 shape 字段，将 Track 槽位改为 2×2 网格结构
   - 在 `state.items` 中为每个部件添加 `shape: [[col,row],...]` 坐标数组
   - 将 Track 的每个类型列（SAIL/PRISM/CORE/SAT）的数据结构从单槽改为 `grid: [[null,null],[null,null]]`（2×2，存 itemId）
   - 新增辅助函数：`getShapeBounds(shape)` 返回 `{cols, rows}`，`shapeExceedsGrid(shape, anchorCol, anchorRow)` 检测越界，`getOccupiedCells(trackId, itemId)` 返回部件占用的所有格子
   - _需求：2.1、2.3、2.4_

- [ ] 2. 重构 Track 网格渲染：将每个类型列渲染为 2×2 CSS Grid
   - 修改 `renderAllTracks()` 和 `renderTrack()`，将每个类型列输出为 `display:grid; grid-template-columns: repeat(2,1fr); grid-template-rows: repeat(2,1fr)` 的 4 格网格
   - 空格子显示淡色 `+` 占位符，每个格子携带 `data-track`、`data-type`、`data-col`、`data-row` 属性
   - 添加对应 CSS：`.track-grid`、`.track-cell`、`.track-cell.empty` 样式
   - _需求：1.1、1.3_

- [ ] 3. 实现多格部件的视觉合并覆盖层渲染
   - 新增 `renderItemOverlay(trackId, typeKey, itemId, anchorCol, anchorRow)` 函数：在网格容器上用绝对定位叠加覆盖层
   - 覆盖层尺寸根据 `shape` 的 bounding box × 格子尺寸 + gap 计算，显示部件图标（居中）、名称（底部）、类型颜色边框
   - 覆盖层绑定 `mousedown` 发起卸载拖拽，`mouseenter` 显示 Tooltip
   - _需求：5.1、5.2、5.4_

- [ ] 4. 重构库存为 16 列 CSS Grid，按 shape 渲染部件尺寸
   - 修改 `renderInventory()`，容器改为 `display:grid; grid-template-columns: repeat(16, 1fr)`
   - 每个库存部件元素用 `grid-column: span {cols}; grid-row: span {rows}` 按 shape bounding box 渲染
   - 库存为空时显示 "ALL ITEMS EQUIPPED" 占位文字
   - _需求：3.1、3.2、3.5_

- [ ] 5. 重构 Ghost 元素：按 shape bounding box 计算尺寸，实时显示可放/不可放颜色
   - 修改 `createGhost()` 和 `updateGhost()`，Ghost 宽高 = `cols × cellSize + (cols-1) × gap`，高度同理
   - Ghost 内部用小格子网格渲染 shape 形状（非 bounding box 矩形，而是实际格子轮廓）
   - Ghost 根据当前悬停状态切换绿色/红色边框（`.ghost-valid` / `.ghost-invalid`）
   - _需求：6.1、6.2、6.3、6.4_

- [ ] 6. 实现拖拽碰撞检测与悬停预览高亮
   - 修改 `mousemove` 处理器：检测鼠标下方的 `.track-cell`，以其 `data-col/row` 为锚点，遍历 `dragState.item.shape` 所有偏移格子
   - 对每个预览格子：检查是否在 2×2 范围内、是否已被占用（且不是当前拖拽部件自身）、类型是否匹配
   - 全部通过 → 预览格子加 `.preview-valid`（绿色）；任意失败 → 加 `.preview-invalid`（红色）
   - `mouseleave` 时清除所有预览高亮
   - _需求：4.1、4.2、4.3_

- [ ] 7. 实现 mouseup 放置逻辑：写入网格数据、渲染覆盖层、更新库存
   - 修改 `handleDrop()`：若当前预览状态为 valid，将 `shape` 所有格子写入 `grid[row][col] = itemId`，调用 `renderItemOverlay()`，从库存移除该部件
   - 若目标格子已有其他部件（交换场景），先将被顶出部件的所有格子清空、移除其覆盖层、追加回库存，再写入新部件
   - 若预览状态为 invalid 或无预览，取消拖拽，部件回到库存（若来自 Track 则重新渲染覆盖层）
   - 放置成功时对覆盖层播放 snap-in 动画（`scale 1.15 → 1.0`，duration 150ms）
   - _需求：4.4、4.5、7.5、7.6_

- [ ] 8. 实现卸载拖拽：从 Track 覆盖层拖回库存
   - 覆盖层 `mousedown` 时：清除该部件在 `grid` 中的所有格子（置 null）、移除覆盖层 DOM、将部件加入 `dragState` 并开始拖拽
   - 若拖拽结束时未放置到有效 Track 格子，部件追加回库存末尾
   - _需求：5.3、5.4、3.4_

- [ ] 9. 实现 Edge Cases 防护
   - Escape 键监听：立即取消拖拽，若部件来自 Track 则恢复原位（重新写回 grid 并渲染覆盖层）
   - `window` 级 `mouseup` 监听：处理鼠标移出窗口后释放的情况，逻辑同取消拖拽
   - `isAnimating` 锁：Loadout 切换动画期间忽略所有 `mousedown` 拖拽发起
   - 越界检测：`shapeExceedsGrid` 返回 true 时拒绝放置（已在需求 6 中集成）
   - _需求：7.1、7.2、7.3、7.4_
