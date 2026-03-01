# 需求文档：StarChart Tooltip 视觉优化

## 引言

当前 StarChart 的 ItemTooltipView 尺寸偏小（220×180px）、字体偏小（9–14pt），整体视觉密度过高、不易阅读。背景虽已是深色（`#0D1219`），但缺乏科技感。本次优化目标：放大 tooltip 整体尺寸与字体，并将视觉风格调整为更具科技感的深蓝色调（深色背景 + 蓝色边框光晕）。

---

## 需求

### 需求 1：放大 Tooltip 整体尺寸

**用户故事：** 作为玩家，我希望 tooltip 卡片更大，以便在不靠近屏幕的情况下也能轻松阅读部件信息。

#### 验收标准

1. WHEN UICanvasBuilder 构建 tooltip 时 THEN 系统 SHALL 将 `sizeDelta` 从 `(220, 180)` 改为 `(280, 230)`。
2. WHEN ItemTooltipView 运行时定位时 THEN 系统 SHALL 将 `_tooltipWidth` 默认值从 `220f` 改为 `280f`，`_tooltipHeight` 从 `180f` 改为 `230f`，确保边界检测与实际尺寸一致。

---

### 需求 2：放大所有文字字号

**用户故事：** 作为玩家，我希望 tooltip 内的文字更大，以便快速扫读部件属性。

#### 验收标准

1. WHEN UICanvasBuilder 构建 NameText 时 THEN 系统 SHALL 将字号从 `14` 改为 `17`。
2. WHEN UICanvasBuilder 构建 TypeText 时 THEN 系统 SHALL 将字号从 `10` 改为 `12`。
3. WHEN UICanvasBuilder 构建 StatsText 时 THEN 系统 SHALL 将字号从 `11` 改为 `13`。
4. WHEN UICanvasBuilder 构建 DescriptionText 时 THEN 系统 SHALL 将字号从 `10` 改为 `12`。
5. WHEN UICanvasBuilder 构建 EquippedStatusText 时 THEN 系统 SHALL 将字号从 `10` 改为 `12`。
6. WHEN UICanvasBuilder 构建 ActionHintText 时 THEN 系统 SHALL 将字号从 `9` 改为 `11`。

---

### 需求 3：科技感深蓝色视觉风格

**用户故事：** 作为玩家，我希望 tooltip 呈现科技感的深蓝色外观，以便与游戏的星图 UI 风格保持一致。

#### 验收标准

1. WHEN UICanvasBuilder 构建 TooltipBackground 时 THEN 系统 SHALL 将背景色改为深蓝色 `(0.04f, 0.07f, 0.14f, 0.97f)`（接近 `#0A1224`）。
2. WHEN UICanvasBuilder 构建 TooltipBorder 时 THEN 系统 SHALL 将边框色改为亮蓝色 `(0.2f, 0.6f, 1f, 0.85f)`，使边框更醒目。
3. WHEN UICanvasBuilder 构建 TypeBadge 背景时 THEN 系统 SHALL 将类型徽章底色改为 `(0f, 0.5f, 1f, 0.15f)`，与深蓝背景协调。
4. IF 背景色已是深色 THEN 系统 SHALL 保持所有文字颜色不变（白色名称、彩色类型标签、青色属性文字），确保对比度足够。
