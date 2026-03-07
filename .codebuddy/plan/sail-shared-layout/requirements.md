# 需求文档：SAIL 共享列布局重构

## 引言

当前 Star Chart UI 中，SAIL（光帆）列被错误地放置在 `TrackView` 内部，导致：
- **Primary TrackView** 显示 SAIL 格子（有内容）
- **Secondary TrackView** 也显示 SAIL 格子（永远为空，浪费空间）
- `RefreshSailColumn()` 中存在 `isPrimary` 守卫判断，逻辑冗余
- `HasSpaceForSail()` 中存在 `isPrimary` 守卫，Secondary 格子拒绝接受拖拽

### HTML 原型参考布局（`StarChartUIPrototype.html`）

HTML 原型中 `lc-body` 的精确层级结构如下：

```
lc-body  (flex-direction: row)
├── sail-col-shared  (flex-shrink: 0，固定宽度，最左侧)
│   ├── track-col-header  → "SAIL" 标题
│   └── track-slots-grid  → SAIL 格子
└── tracks-area  (flex: 1，占剩余宽度)
    ├── track-block#primary  (data-track="primary")
    │   ├── track-block-label  → "PRIMARY" + "[LMB]"
    │   └── track-rows  (flex-direction: row)
    │       ├── track-col.trl-prism  → PRISM 列
    │       ├── track-col.trl-core   → CORE 列
    │       └── track-col.trl-sat    → SAT 列
    └── track-block#secondary  (data-track="secondary")
        ├── track-block-label  → "SECONDARY" + "[RMB]"
        └── track-rows  (flex-direction: row)
            ├── track-col.trl-prism  → PRISM 列
            ├── track-col.trl-core   → CORE 列
            └── track-col.trl-sat    → SAT 列
```

**关键设计要点（来自 HTML 原型注释）：**
- `SAIL is global (not primary/secondary specific) — stored at lo.sail`
- `sail-col-shared` 使用 `flex-shrink: 0`，不参与弹性伸缩，固定在最左侧
- `tracks-area` 使用 `flex: 1`，占据 `lc-body` 的剩余宽度
- `track-block-label`（PRIMARY/SECONDARY 标识）位于各自 `track-block` 内部，在 `tracks-area` 内，不在 `sail-col-shared` 旁边
- 每个 `track-block` 内部：标识在上，`track-rows`（3列）在下

本次重构目标：将 Unity 的 Star Chart UI 布局与 HTML 原型对齐，消除架构不一致。

---

## 需求

### 需求 1：SAIL 列从 TrackView 中独立出来

**用户故事：** 作为玩家，我希望 SAIL（光帆）在星图界面中只显示一次，清晰地表达它是飞船级共享部件，而不是某个轨道的专属配件。

#### 验收标准

1. WHEN 星图面板打开 THEN 系统 SHALL 在 LoadoutCard 的 `lc-body` 最左侧显示一个独立的 SAIL 列（对应 `sail-col-shared`），不属于 Primary 或 Secondary TrackView
2. WHEN 星图面板打开 THEN 系统 SHALL 确保 Secondary TrackView 中不再存在空的 SAIL 格子
3. IF SAIL 已装备 THEN 系统 SHALL 在共享 SAIL 列中显示该光帆图标
4. IF SAIL 未装备 THEN 系统 SHALL 在共享 SAIL 列中显示空格子（可接受拖拽）
5. WHEN 玩家将光帆拖入共享 SAIL 列 THEN 系统 SHALL 正确装备该光帆，无需 `isPrimary` 守卫判断

---

### 需求 2：PRIMARY / SECONDARY 标识位于 tracks-area 内部

**用户故事：** 作为玩家，我希望 PRIMARY [LMB] 和 SECONDARY [RMB] 标识清晰地标注在各自轨道的左侧，与 HTML 原型布局一致。

#### 验收标准

1. WHEN 星图面板打开 THEN 系统 SHALL 在 Primary `track-block` 内部顶部显示 "PRIMARY [LMB]" 标签（`track-block-label`）
2. WHEN 星图面板打开 THEN 系统 SHALL 在 Secondary `track-block` 内部顶部显示 "SECONDARY [RMB]" 标签（`track-block-label`）
3. THEN 系统 SHALL 确保 PRIMARY/SECONDARY 标识位于 `tracks-area` 内部，而非 `sail-col-shared` 旁边（即标识在 SAIL 列右侧）
4. WHEN UICanvasBuilder 执行 THEN 系统 SHALL 按照 HTML 原型的 flex 布局比例正确排列：`sail-col-shared`（固定宽）+ `tracks-area`（flex:1）

---

### 需求 3：TrackView 简化为 3 列（PRISM | CORE | SAT）

**用户故事：** 作为开发者，我希望 TrackView 只管理属于自己轨道的列（PRISM、CORE、SAT），不再持有 SAIL 列的引用，以消除 `isPrimary` 守卫和冗余逻辑。

#### 验收标准

1. WHEN TrackView 初始化 THEN 系统 SHALL 不再包含 `_sailColumn` 字段和 `_sailHighlightLayer`
2. WHEN TrackView 刷新 THEN 系统 SHALL 不再调用 `RefreshSailColumn()`
3. WHEN TrackView 处理拖拽 THEN 系统 SHALL 不再需要 `HasSpaceForSail()` 中的 `isPrimary` 守卫
4. THEN 系统 SHALL 确保 `GetColumn(SlotType.LightSail)` 返回 null 或从 TrackView 中移除该分支
5. THEN 系统 SHALL 确保 `ClearAllHighlightTiles()` 不再清理 `_sailHighlightLayer`

---

### 需求 4：StarChartPanel 接管共享 SAIL 列的引用与刷新

**用户故事：** 作为开发者，我希望共享 SAIL 列由 StarChartPanel 统一管理其显示、刷新和拖拽逻辑，职责清晰。

#### 验收标准

1. WHEN StarChartPanel 初始化 THEN 系统 SHALL 将共享 SAIL 列的 `TypeColumn` 引用绑定到 `StarChartPanel`（而非 TrackView）
2. WHEN StarChartPanel 调用 `RefreshAllViews` THEN 系统 SHALL 单独刷新共享 SAIL 列
3. WHEN 玩家拖拽光帆到共享 SAIL 列 THEN 系统 SHALL 通过 `StarChartController.EquipLightSail()` 正确装备/替换光帆
4. WHEN 玩家从共享 SAIL 列拖出光帆 THEN 系统 SHALL 正确触发卸载逻辑
5. WHEN DragDropManager 处理 SAIL 拖拽 THEN 系统 SHALL 不再依赖 `DropTargetTrack` 来判断 `isPrimary`

---

### 需求 5：UICanvasBuilder 一键重建支持新布局

**用户故事：** 作为开发者，我希望执行 `ProjectArk > Build UI Canvas` 后，自动生成符合 HTML 原型的 UI 层级结构。

#### 验收标准

1. WHEN UICanvasBuilder 执行 THEN 系统 SHALL 在 `lc-body` 中按顺序创建：`SharedSailColumn`（`flex-shrink:0`）→ `TracksArea`（`flex:1`）
2. WHEN UICanvasBuilder 执行 THEN 系统 SHALL `TracksArea` 内包含 `PrimaryTrackBlock` 和 `SecondaryTrackBlock`，各自内部有 `track-block-label` + `track-rows`
3. WHEN UICanvasBuilder 执行 THEN 系统 SHALL 每个 `track-rows` 只构建 3 列（PRISM | CORE | SAT），不再构建 SAIL 列
4. WHEN UICanvasBuilder 执行 THEN 系统 SHALL 将 `SharedSailColumn` 的 `TypeColumn` 引用 Wire 到 `StarChartPanel._sharedSailColumn`
5. WHEN UICanvasBuilder 执行后 THEN Hierarchy 层级 SHALL 符合以下结构：
   ```
   LoadoutCard
   └── lc-body  (HorizontalLayoutGroup)
       ├── SharedSailColumn  (TypeColumn, flex-shrink:0)
       └── TracksArea  (VerticalLayoutGroup, flex:1)
           ├── PrimaryTrackBlock
           │   ├── track-block-label ("PRIMARY [LMB]")
           │   └── track-rows (HorizontalLayoutGroup)
           │       ├── PrismColumn
           │       ├── CoreColumn
           │       └── SatColumn
           └── SecondaryTrackBlock
               ├── track-block-label ("SECONDARY [RMB]")
               └── track-rows (HorizontalLayoutGroup)
                   ├── PrismColumn
                   ├── CoreColumn
                   └── SatColumn
   ```

---

## 技术约束

- 遵循 CLAUDE.md：禁止 `SetActive` 控制显隐，统一用 `CanvasGroup`
- 遵循 CLAUDE.md：禁止创建 `.meta` 文件（GUID 由 Unity 自动生成）
- 数据层不变：`LoadoutSlot.GetEquippedLightSail()` 和 `StarChartController.EquipLightSail()` 保持不变
- 拖拽系统：`DragDropManager` 中 SAIL 的 `EquipToTrack` 逻辑需适配（不再依赖 `DropTargetTrack` 的 `isPrimary` 判断）
- 高亮系统：共享 SAIL 列需要独立的 `DragHighlightLayer`，由 `StarChartPanel` 管理
- HTML 原型中 `sail-col-shared` 使用 `flex-shrink: 0`（Unity 对应：`LayoutElement.flexibleWidth = 0`，固定 `preferredWidth`）
- HTML 原型中 `tracks-area` 使用 `flex: 1`（Unity 对应：`LayoutElement.flexibleWidth = 1`）
