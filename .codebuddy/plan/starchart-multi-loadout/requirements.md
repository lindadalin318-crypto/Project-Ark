# 需求文档：星图多 Loadout 系统（方案 B 完整实现）

## 引言

当前星图系统（StarChartController）只支持单套装备配置（Primary Track + Secondary Track + LightSail + Satellites）。玩家无法在战斗前预设多套配置并快速切换。

本功能将实现 **3 个独立 Loadout 槽位**，每个槽位拥有完全独立的武器轨道数据（Primary/Secondary Track、LightSail、Satellites）。玩家可在星图面板中通过 ▲/▼ 按钮切换 Loadout，切换时 StarChartController 立即应用对应槽位的装备配置，UI 同步刷新。存档系统同步扩展以持久化全部 3 套配置。

**当前代码现状：**
- `LoadoutSwitcher`：UI 已就绪，但 `SetLoadoutCount(1)` 导致按钮被禁用
- `StarChartController`：只有单套 `_primaryTrack` / `_secondaryTrack`，无多 Loadout 数据结构
- `StarChartPanel.HandleLoadoutChanged`：只调用 `RefreshAll()`，不切换数据
- `SaveData.cs`：`StarChartSaveData` 只存单套配置

---

## 需求

### 需求 1：LoadoutSlot 数据结构

**用户故事：** 作为开发者，我希望有一个独立的 `LoadoutSlot` 数据类封装单套装备配置，以便 StarChartController 可以管理多个槽位而不重构现有 WeaponTrack 逻辑。

#### 验收标准

1. WHEN 系统初始化 THEN `LoadoutSlot` SHALL 包含独立的 `PrimaryTrack`（WeaponTrack）、`SecondaryTrack`（WeaponTrack）、`EquippedLightSailSO`（LightSailSO）、`EquippedSatelliteSOs`（List\<SatelliteSO\>）字段。
2. WHEN `LoadoutSlot` 被创建 THEN 其两条 WeaponTrack SHALL 为全新实例，互不共享。
3. IF `LoadoutSlot` 被清空 THEN 两条轨道的所有 Core/Prism SHALL 被移除，LightSail 和 Satellites 列表 SHALL 被清空。

---

### 需求 2：StarChartController 多 Loadout 管理

**用户故事：** 作为玩家，我希望 StarChartController 能管理 3 个独立的 Loadout 槽位，以便我的装备配置真正独立存储。

#### 验收标准

1. WHEN `StarChartController` 初始化 THEN SHALL 创建 3 个 `LoadoutSlot` 实例（索引 0/1/2），默认激活槽位 0。
2. WHEN 调用 `SwitchLoadout(int index)` THEN `PrimaryTrack`、`SecondaryTrack`、`GetEquippedLightSail()`、`GetEquippedSatellites()` SHALL 立即返回目标槽位的数据。
3. WHEN 切换 Loadout THEN 旧槽位的 LightSailRunner 和 SatelliteRunners SHALL 被 Dispose，新槽位的 Runner SHALL 被重新创建。
4. WHEN 切换 Loadout THEN 新槽位的 WeaponTrack SHALL 调用 `InitializePools()` 确保对象池就绪。
5. WHEN 在某槽位装备/卸载部件 THEN 只影响当前激活槽位，其他槽位数据 SHALL 不变。
6. WHEN 调试 Debug Loadout 时 THEN Debug 数据 SHALL 只加载到槽位 0，槽位 1/2 保持空白。

---

### 需求 3：StarChartPanel UI 联动

**用户故事：** 作为玩家，我希望星图面板的 ▲/▼ 按钮能真正切换 Loadout 并刷新 UI，以便我能直观地看到每套配置的内容。

#### 验收标准

1. WHEN `StarChartPanel.Bind()` 被调用 THEN `LoadoutSwitcher.SetLoadoutCount(3)` SHALL 被调用，▲/▼ 按钮 SHALL 变为可交互状态。
2. WHEN 玩家点击 ▲/▼ 按钮 THEN `StarChartController.SwitchLoadout(index)` SHALL 被调用。
3. WHEN `SwitchLoadout` 完成 THEN `TrackView` 的 `Bind()` SHALL 用新槽位的 Track 重新绑定，UI SHALL 完整刷新。
4. WHEN 切换 Loadout THEN `LoadoutSwitcher` 的卡片滑动动画 SHALL 正常播放（已有实现，保持不变）。
5. WHEN 切换 Loadout THEN `StatusBar` SHALL 更新为新槽位的装备数量。

---

### 需求 4：存档系统扩展

**用户故事：** 作为玩家，我希望 3 套 Loadout 配置都能被存档持久化，以便重启游戏后配置不丢失。

#### 验收标准

1. WHEN 导出存档 THEN `StarChartSaveData` SHALL 包含 3 个 `LoadoutSlotSaveData` 的列表（替代原有单套 PrimaryTrack/SecondaryTrack 字段）。
2. WHEN 导入存档 THEN `StarChartController` SHALL 将每个 `LoadoutSlotSaveData` 还原到对应槽位。
3. IF 存档中 Loadout 列表长度不足 3 THEN 系统 SHALL 用空槽位补全，不抛出异常（向前兼容）。
4. WHEN 存档版本为旧格式（只有单套 PrimaryTrack/SecondaryTrack）THEN 系统 SHALL 将其迁移为槽位 0 的数据，槽位 1/2 保持空白。

---

### 需求 5：IsItemEquipped 跨槽位检查

**用户故事：** 作为玩家，我希望背包中的物品"已装备"标记能正确反映当前激活 Loadout 的状态，以便我清楚地知道哪些部件正在使用中。

#### 验收标准

1. WHEN `IsItemEquipped(item)` 被调用 THEN SHALL 只检查当前激活槽位的装备状态，不跨槽位检查。
2. WHEN 切换 Loadout THEN 背包视图 SHALL 刷新，物品的"已装备"标记 SHALL 反映新槽位的状态。
