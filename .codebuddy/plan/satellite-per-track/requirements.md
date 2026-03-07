# 需求文档：伴星（Satellite）从 Shared 改为 Per-Track

## 引言

当前伴星（Satellite）在数据层属于 `LoadoutSlot` 级别（Shared），即一个配装槽的 Primary 和 Secondary 轨道共享同一组伴星。UI 层通过"只在 Primary TrackView 显示，Secondary 显示空格"的临时方案规避了重复渲染问题。

本次重构决定采用**方案 B：SAT 分属 Primary 和 Secondary**，理由如下：

1. **GDD 中存在明确的 Per-Track 触发语义**：`[双子座]` — IF 右键开火 THEN 左键自动向后开火；`[同步炮]` — IF Primary 开火 THEN 自动补射。这类伴星的触发条件本质上绑定到特定轨道，Shared 模型无法表达这种语义。
2. **策略深度更高**：玩家可以为 Primary 配置进攻型伴星、为 Secondary 配置防御型伴星，形成更丰富的构建策略。
3. **UI 架构更干净**：消除 `isPrimary` 守卫逻辑，Secondary TrackView 不再显示无意义的空格。
4. **与 HTML 原型一致**：原型中 PRIMARY 和 SECONDARY 各自拥有独立的 SAT 列。

### 架构变更范围

| 层级 | 变更内容 |
|------|---------|
| **数据层** | `LoadoutSlot` 移除 `EquippedSatelliteSOs`；`WeaponTrack` 新增 `EquippedSatelliteSOs` |
| **存档层** | `LoadoutSlotSaveData` 移除 `SatelliteIDs`；`TrackSaveData` 新增 `SatelliteIDs`（含存档迁移） |
| **控制器层** | `StarChartController` 的 Equip/Unequip/Runner 管理改为 Per-Track |
| **UI 层** | `TrackView` 的 SAT 列直接读取所属 `WeaponTrack` 的伴星数据，移除 `isPrimary` 守卫 |

---

## 需求

### 需求 1：数据层重构 — WeaponTrack 持有伴星列表

**用户故事：** 作为开发者，我希望伴星数据归属于 WeaponTrack，以便 Primary 和 Secondary 轨道可以独立配置不同的伴星组合。

#### 验收标准

1. WHEN 系统初始化 THEN `WeaponTrack` SHALL 持有 `List<SatelliteSO> EquippedSatelliteSOs`，初始为空列表。
2. WHEN 系统初始化 THEN `LoadoutSlot` SHALL 不再持有 `EquippedSatelliteSOs` 字段，该字段从 `LoadoutSlot` 中移除。
3. WHEN `WeaponTrack.ClearAll()` 被调用 THEN 系统 SHALL 同时清空 `EquippedSatelliteSOs` 列表。
4. IF `WeaponTrack` 的伴星列表发生变化 THEN `WeaponTrack.OnLoadoutChanged` 事件 SHALL 被触发，通知 UI 刷新。

---

### 需求 2：存档层重构 — TrackSaveData 持有伴星 ID 列表（含迁移）

**用户故事：** 作为开发者，我希望存档格式与新数据结构一致，并且旧存档能够自动迁移，以便玩家不会丢失已有的配装数据。

#### 验收标准

1. WHEN 系统序列化存档 THEN `TrackSaveData` SHALL 包含 `List<string> SatelliteIDs` 字段，分别记录 Primary 和 Secondary 轨道的伴星 ID。
2. WHEN 系统序列化存档 THEN `LoadoutSlotSaveData` SHALL 不再包含顶层 `SatelliteIDs` 字段（标记为 `[Obsolete]` 保留用于迁移）。
3. WHEN 系统读取旧格式存档（`LoadoutSlotSaveData.SatelliteIDs` 非空）THEN 系统 SHALL 自动将旧 `SatelliteIDs` 迁移到 `PrimaryTrack.SatelliteIDs`，Secondary 轨道伴星列表为空。
4. WHEN 系统读取新格式存档 THEN 系统 SHALL 分别将 `PrimaryTrack.SatelliteIDs` 和 `SecondaryTrack.SatelliteIDs` 还原到对应 `WeaponTrack` 的 `EquippedSatelliteSOs`。
5. IF 存档中的伴星 ID 无法被 `IStarChartItemResolver` 解析 THEN 系统 SHALL 跳过该条目并记录警告日志，不抛出异常。

---

### 需求 3：控制器层重构 — StarChartController 按轨道管理伴星 Runner

**用户故事：** 作为开发者，我希望 StarChartController 的伴星装备/卸载/Tick 逻辑按轨道分组，以便 Per-Track 触发语义（如"IF Primary 开火"）能够在未来正确实现。

#### 验收标准

1. WHEN 玩家装备伴星 THEN `StarChartController.EquipSatellite(SatelliteSO, WeaponTrack.TrackId)` SHALL 接受轨道 ID 参数，将伴星添加到指定轨道的 `EquippedSatelliteSOs` 并创建对应 `SatelliteRunner`。
2. WHEN 玩家卸载伴星 THEN `StarChartController.UnequipSatellite(SatelliteSO, WeaponTrack.TrackId)` SHALL 接受轨道 ID 参数，从指定轨道移除伴星并 Dispose 对应 Runner。
3. WHEN `StarChartController` 每帧 Update THEN 系统 SHALL 分别 Tick Primary 和 Secondary 轨道的伴星 Runner 列表（当前均使用同一 `StarChartContext`，为未来 Per-Track Context 预留扩展点）。
4. WHEN `StarChartController` 切换配装槽 THEN 系统 SHALL 分别 Dispose 旧槽 Primary/Secondary 两个轨道的所有伴星 Runner，并为新槽重建。
5. WHEN `StarChartController.GetEquippedSatellites()` 被调用 THEN 系统 SHALL 接受 `WeaponTrack.TrackId` 参数，返回指定轨道的伴星列表（UI 层调用）。
6. IF `SatelliteRunner` 的 `StarChartContext` 未来需要区分轨道（如访问 `WeaponTrack.ResetCooldown`）THEN 系统 SHALL 能够通过向 `SatelliteRunner` 构造函数传入轨道引用来扩展，无需修改 `SatelliteRunner` 基类。

---

### 需求 4：UI 层重构 — TrackView SAT 列直接读取所属轨道数据

**用户故事：** 作为玩家，我希望 Primary 和 Secondary 轨道各自显示自己的伴星槽位，以便我能直观地为两条轨道分别配置伴星。

#### 验收标准

1. WHEN `TrackView` 刷新 SAT 列 THEN 系统 SHALL 直接读取 `_track.EquippedSatelliteSOs`（所属 `WeaponTrack` 的伴星列表），不再通过 `isPrimary` 守卫判断。
2. WHEN `TrackView` 刷新 SAT 列 THEN Primary 和 Secondary 的 SAT 列 SHALL 各自独立显示，Secondary 不再显示空格占位。
3. WHEN 玩家将伴星拖拽到 Primary TrackView 的 SAT 格子 THEN 系统 SHALL 调用 `EquipSatellite(sat, TrackId.Primary)`。
4. WHEN 玩家将伴星拖拽到 Secondary TrackView 的 SAT 格子 THEN 系统 SHALL 调用 `EquipSatellite(sat, TrackId.Secondary)`。
5. WHEN 玩家点击已装备伴星的格子 THEN 系统 SHALL 调用 `UnequipSatellite(sat, ownerTrackId)`，从正确的轨道卸载。
6. IF `TrackView` 的 `_satColumn` 为 null THEN 系统 SHALL 跳过 SAT 刷新并记录警告，不抛出 NullReferenceException。

---

## 附录：存档格式变更对比

### 旧格式（当前）
```json
{
  "Loadouts": [{
    "PrimaryTrack":   { "CoreIDs": [...], "PrismIDs": [...] },
    "SecondaryTrack": { "CoreIDs": [...], "PrismIDs": [...] },
    "LightSailID": "...",
    "SatelliteIDs": ["sat1", "sat2"]   ← Slot 级别
  }]
}
```

### 新格式（目标）
```json
{
  "Loadouts": [{
    "PrimaryTrack":   { "CoreIDs": [...], "PrismIDs": [...], "SatelliteIDs": ["sat1"] },
    "SecondaryTrack": { "CoreIDs": [...], "PrismIDs": [...], "SatelliteIDs": ["sat2"] },
    "LightSailID": "..."
    ← SatelliteIDs 从 Slot 级别移除
  }]
}
```

### 迁移规则
旧存档中 `LoadoutSlotSaveData.SatelliteIDs` 非空时，全部迁移到 `PrimaryTrack.SatelliteIDs`，Secondary 轨道伴星列表为空。
