# 实施计划：伴星（Satellite）从 Shared 改为 Per-Track

- [ ] 1. 重构数据层 — `WeaponTrack` 持有伴星列表，`LoadoutSlot` 移除该字段
   - 在 `WeaponTrack` 中新增 `List<SatelliteSO> EquippedSatelliteSOs`，初始化为空列表
   - 在 `WeaponTrack.ClearAll()` 中清空 `EquippedSatelliteSOs`
   - 在 `WeaponTrack` 伴星列表变化时触发 `OnLoadoutChanged` 事件
   - 从 `LoadoutSlot` 中移除 `EquippedSatelliteSOs` 字段，修复所有编译引用
   - _需求：1.1、1.2、1.3、1.4_

- [ ] 2. 重构存档层 — `TrackSaveData` 新增 `SatelliteIDs`，`LoadoutSlotSaveData` 标记废弃
   - 在 `TrackSaveData` 中新增 `List<string> SatelliteIDs` 字段
   - 将 `LoadoutSlotSaveData.SatelliteIDs` 标记为 `[Obsolete]`，保留字段用于迁移读取
   - _需求：2.1、2.2_

- [ ] 3. 实现存档迁移逻辑
   - 在存档加载入口处检测旧格式（`LoadoutSlotSaveData.SatelliteIDs` 非空）
   - 将旧 `SatelliteIDs` 自动迁移到 `PrimaryTrack.SatelliteIDs`，Secondary 保持空列表
   - 新格式加载时分别将 Primary/Secondary 的 `SatelliteIDs` 还原到对应 `WeaponTrack`
   - 无法解析的 ID 跳过并输出 `Debug.LogWarning`，不抛出异常
   - _需求：2.3、2.4、2.5_

- [ ] 4. 重构控制器层 — `StarChartController.EquipSatellite / UnequipSatellite` 新增 `TrackId` 参数
   - 修改 `EquipSatellite(SatelliteSO, TrackId)` 签名，将伴星写入指定轨道并创建 `SatelliteRunner`
   - 修改 `UnequipSatellite(SatelliteSO, TrackId)` 签名，从指定轨道移除并 Dispose Runner
   - 修改 `GetEquippedSatellites(TrackId)` 签名，返回指定轨道的伴星列表
   - 修复所有调用方（StarChartPanel、DragDropManager 等）传入正确的 `TrackId`
   - _需求：3.1、3.2、3.5_

- [ ] 5. 重构控制器层 — Runner Tick 和配装槽切换按轨道分组
   - 将 `_satelliteRunners` 拆分为 `_primaryRunners` 和 `_secondaryRunners` 两个列表
   - `Update()` 中分别 Tick 两个列表（当前共用同一 `StarChartContext`）
   - 切换配装槽时分别 Dispose 旧槽两个轨道的所有 Runner，并为新槽重建
   - _需求：3.3、3.4、3.6_

- [ ] 6. 重构 UI 层 — `TrackView` SAT 列直接读取所属轨道数据，移除 `isPrimary` 守卫
   - `TrackView.RefreshSatellites()` 改为直接读取 `_track.EquippedSatelliteSOs`
   - 移除所有 `isPrimary` 守卫判断，Secondary `TrackView` 不再显示空格占位
   - SAT 格子的 Drop 事件调用 `EquipSatellite(sat, _track.TrackId)`
   - SAT 格子的 Unequip 事件调用 `UnequipSatellite(sat, _track.TrackId)`
   - `_satColumn` 为 null 时跳过刷新并输出 `Debug.LogWarning`
   - _需求：4.1、4.2、4.3、4.4、4.5、4.6_

- [ ] 7. 全量编译验证与空引用修复
   - 确保移除 `LoadoutSlot.EquippedSatelliteSOs` 后所有引用均已更新，无编译错误
   - 检查 `StarChartPanel`、`DragDropManager`、`TrackView` 中所有伴星相关调用均传入正确 `TrackId`
   - Play Mode 验证：装备/卸载伴星到 Primary 和 Secondary 各自独立，互不影响
   - _需求：1.2、3.1、3.2、4.3、4.4_

- [ ] 8. 追加实现日志到 `ImplementationLog.md`
   - 记录本次重构涉及的新建/修改文件路径列表
   - 记录存档迁移规则和新旧格式对比
   - 记录 Per-Track Runner 管理的扩展点说明
   - _需求：全部_
