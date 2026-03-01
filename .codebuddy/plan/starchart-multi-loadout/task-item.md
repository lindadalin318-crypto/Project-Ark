# 实施计划：星图多 Loadout 系统

- [ ] 1. 新建 `LoadoutSlot` 数据类
   - 在 `Assets/Scripts/Combat/StarChart/` 下新建 `LoadoutSlot.cs`
   - 包含 `PrimaryTrack`（WeaponTrack）、`SecondaryTrack`（WeaponTrack）、`EquippedLightSailSO`、`EquippedSatelliteSOs` 字段
   - 构造函数中创建两条全新 WeaponTrack 实例（不共享）
   - 提供 `Clear()` 方法清空所有轨道数据和装备列表
   - _需求：1.1、1.2、1.3_

- [ ] 2. 重构 `StarChartController` 支持 3 个 Loadout 槽位
   - 将现有 `_primaryTrack` / `_secondaryTrack` / LightSail / Satellites 字段迁移到 `LoadoutSlot` 内
   - 添加 `LoadoutSlot[] _loadouts`（长度 3）和 `int _activeLoadoutIndex` 字段
   - 在 `Initialize()` 中创建 3 个 `LoadoutSlot` 实例，默认激活索引 0
   - 实现 `SwitchLoadout(int index)`：Dispose 旧槽位 Runner → 切换索引 → 重建新槽位 Runner → 调用 `InitializePools()`
   - 所有对 `_primaryTrack` / `_secondaryTrack` 的引用改为通过 `_loadouts[_activeLoadoutIndex]` 访问
   - Debug Loadout 数据只加载到槽位 0
   - _需求：2.1、2.2、2.3、2.4、2.5、2.6_

- [ ] 3. 扩展存档数据结构并实现迁移
   - 在 `SaveData.cs` 中新增 `LoadoutSlotSaveData`（含 PrimaryTrack/SecondaryTrack/LightSail/Satellites 序列化字段）
   - 修改 `StarChartSaveData`：添加 `List<LoadoutSlotSaveData> Loadouts` 字段，保留旧字段并标记 `[Obsolete]` 用于迁移
   - 在 `StarChartController.ExportSaveData()` 中序列化全部 3 个槽位
   - 在 `StarChartController.ImportSaveData()` 中：优先读取新 `Loadouts` 列表；若为旧格式则将旧字段数据迁移到槽位 0，槽位 1/2 保持空白；列表长度不足 3 时补空槽位
   - _需求：4.1、4.2、4.3、4.4_

- [ ] 4. 联动 `StarChartPanel` UI 与 `LoadoutSwitcher`
   - 在 `StarChartPanel.Bind()` 中将 `SetLoadoutCount(1)` 改为 `SetLoadoutCount(3)`
   - 在 `HandleLoadoutChanged(int index)` 中调用 `_controller.SwitchLoadout(index)`，然后用新槽位的 Track 重新调用 `TrackView.Bind()`
   - 切换完成后调用 `RefreshStatusBar()` 更新装备数量显示
   - _需求：3.1、3.2、3.3、3.4、3.5_

- [ ] 5. 修正 `IsItemEquipped` 只检查当前激活槽位，并在切换后刷新背包视图
   - 确认 `IsItemEquipped(item)` 的查询路径只走 `_loadouts[_activeLoadoutIndex]`，不遍历其他槽位
   - 在 `HandleLoadoutChanged` 完成后调用 `InventoryView.RefreshAll()` 刷新已装备标记
   - _需求：5.1、5.2_
