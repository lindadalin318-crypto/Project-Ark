# 需求文档：门元素位置 → 玩家出生点（SpawnPoint）管线打通

## 引言

当前 LevelDesigner（HTML工具）中的门元素（🚪）可以在房间内自由拖放定位，其位置会导出到 JSON 的 `elements[].position` 字段中。在 Unity 侧，`HtmlScaffoldImporter` 已能将该位置转为 `ScaffoldElement.LocalPosition`，`ScaffoldToSceneGenerator` 也能生成 Door 和 SpawnPoint。

**然而**，当前 `ScaffoldToSceneGenerator` Phase 4 创建 SpawnPoint 时，使用的是 `connection.DoorPosition`（房间边缘中点）或 `FindReverseDoorPosition`（反向连接的边缘中点），并不使用门元素的实际位置。这意味着：
- 在 LevelDesigner 中精心摆放的门元素位置，**不会**影响玩家穿过门后的出生点
- SpawnPoint 始终生成在房间边缘中点位置

**目标**：将 HTML LevelDesigner 中门元素的位置，作为"玩家从对面穿过该门后的出生位置"导出，并在 Unity 侧 `ScaffoldToSceneGenerator` 一键生成时自动应用到 SpawnPoint，实现**设计即所得**。

**工作流范围**（仅修改以下3个环节）：
1. `Tools/LevelDesigner.html` — 导出 JSON 的 `doorLinks[]` 中新增 `spawnOffset`
2. `Assets/Scripts/Level/Editor/HtmlScaffoldImporter.cs` — 解析 `spawnOffset` 并存入 `ScaffoldDoorConnection` 新字段
3. `Assets/Scripts/Level/Editor/ScaffoldToSceneGenerator.cs` — 使用新字段生成 SpawnPoint 位置

**不涉及**：`LevelGenerator.cs`、`LevelDesignerWindow.cs`、`LevelScaffoldData.cs` 中新增字段等。

## 当前数据流（现状分析）

```
LevelDesigner(HTML)                           Unity
─────────────────                             ─────
doorLink {roomId, entryDir, doorIndex}  ─→  JSON doorLinks[]
door element position                   ─→  JSON elements[].position
                                              ↓
                                        HtmlScaffoldImporter
                                              ↓
                                        ScaffoldElement.LocalPosition (门元素位置 ✅)
                                        ScaffoldDoorConnection.DoorPosition (边缘中点)
                                              ↓
                                        ScaffoldToSceneGenerator Phase 4
                                              ↓
                                        Door GO localPosition = conn.DoorPosition ✅
                                        SpawnPoint localPosition = reverseConn.DoorPosition ❌ (边缘中点，非门元素位置)
```

## 目标数据流

```
LevelDesigner(HTML)                           Unity
─────────────────                             ─────
doorLink {roomId, entryDir, doorIndex,  ─→  JSON doorLinks[]（含 spawnOffset）
          spawnOffset: [x, y]}
                                              ↓
                                        HtmlScaffoldImporter
                                              ↓
                                        ScaffoldDoorConnection.SpawnOffset (新字段, 来自门元素位置)
                                              ↓
                                        ScaffoldToSceneGenerator Phase 4
                                              ↓
                                        SpawnPoint localPosition = conn.SpawnOffset ✅ (门元素位置)
```

## 需求

### 需求 1：LevelDesigner 导出 doorLink 的 spawnOffset 数据

**用户故事：** 作为一名关卡设计师，我希望导出 JSON 时，每个 doorLink 自动包含对应门元素的位置偏移量，以便 Unity 侧能获取到门元素的精确位置用于生成出生点。

#### 验收标准

1. WHEN LevelDesigner 导出 JSON THEN 每个 doorLink 条目 SHALL 包含 `spawnOffset` 字段，值为该 doorLink 对应的门元素在房间内的 `[x, y]` 位置（格子单位，相对于房间左上角，与 elements[].position 一致）。
2. IF doorLink 的 `doorIndex` 指向的门元素不存在 THEN 该 doorLink SHALL 不包含 `spawnOffset` 字段。
3. WHEN 设计师在 LevelDesigner 中拖动已绑定 doorLink 的门元素 THEN 下次导出时 `spawnOffset` 值 SHALL 反映最新的门元素位置。

### 需求 2：HtmlScaffoldImporter 解析 spawnOffset 并存入 ScaffoldDoorConnection

**用户故事：** 作为一名开发者，我希望 HtmlScaffoldImporter 能自动解析 JSON doorLink 中的 `spawnOffset`，并转换为 Unity 坐标存入 `ScaffoldDoorConnection` 的新字段，以便后续生成器使用。

#### 验收标准

1. WHEN HtmlScaffoldImporter 解析含有 `spawnOffset` 字段的 doorLink 条目 THEN 系统 SHALL 将该偏移量从 HTML 坐标系（房间左上角原点）转换为 Unity 本地坐标系（房间中心原点、y轴翻转、乘以 gridScale），并存储到对应 `ScaffoldDoorConnection` 的新字段 `SpawnOffset`（Vector3）中。
2. IF doorLink 条目不包含 `spawnOffset` 字段 THEN `ScaffoldDoorConnection.SpawnOffset` SHALL 保持默认值 `Vector3.zero`，后续生成器回退到当前行为。
3. WHEN 导入完成后 THEN 系统 SHALL 在 Console 日志中报告有多少个 doorLink 成功应用了自定义 spawnOffset。
4. 为 `ScaffoldDoorConnection` 新增 `_spawnOffset` / `SpawnOffset` 字段（`Vector3`，序列化），保持向后兼容（旧 .asset 文件中该字段默认为零向量）。

### 需求 3：ScaffoldToSceneGenerator 使用 SpawnOffset 生成 SpawnPoint

**用户故事：** 作为一名关卡设计师，我希望一键生成关卡时，SpawnPoint 的位置自动基于门元素的位置生成，以便设计与运行时行为完全一致。

#### 验收标准

1. WHEN ScaffoldToSceneGenerator Phase 4 为正向门创建 SpawnPoint THEN 系统 SHALL 查找**目标房间**中反向 connection 的 `SpawnOffset`。IF 该 `SpawnOffset != Vector3.zero` THEN SpawnPoint 的 localPosition SHALL 使用该 `SpawnOffset` 值。
2. WHEN ScaffoldToSceneGenerator Phase 4 为反向门创建 SpawnPoint THEN 系统 SHALL 查找**源房间**中正向 connection 的 `SpawnOffset`。IF 该 `SpawnOffset != Vector3.zero` THEN SpawnPoint 的 localPosition SHALL 使用该 `SpawnOffset` 值。
3. IF `SpawnOffset == Vector3.zero`（无自定义偏移/旧数据）THEN 系统 SHALL 回退到当前行为（使用 `FindReverseDoorPosition` / `conn.DoorPosition` 边缘中点逻辑）。
4. WHEN 生成完成后 THEN Door 组件的 `_targetSpawnPoint` 字段 SHALL 正确引用新生成的 SpawnPoint Transform。

## 边界情况

1. **一个房间有多个门通向同一目标房间**：当前 `ScaffoldToSceneGenerator` 已用 `processedPairs` 去重连接对，此行为保持不变。每对连接使用各自的 SpawnOffset。
2. **单向连接（只有 A→B 没有 B→A connection）**：反向门的 SpawnPoint 无法从反向 connection 获取 SpawnOffset，回退到边缘中点逻辑。
3. **门元素被放在房间中心（远离边缘）**：SpawnPoint 仍使用门元素位置，设计师全权负责合理性。
4. **Grid Scale ≠ 1**：HtmlScaffoldImporter 坐标转换需正确乘以 `_gridScale`。
5. **旧版 JSON 无 spawnOffset 字段**：向后兼容，`SpawnOffset` 默认为 `Vector3.zero`，回退到旧逻辑。
6. **旧版 .asset 文件无 `_spawnOffset` 字段**：Unity 序列化自动处理，新字段默认为零向量。

## 技术约束

- `ScaffoldDoorConnection` 新增的 `_spawnOffset` 字段使用 `[SerializeField]`，不破坏已有 .asset 序列化兼容性
- `HtmlScaffoldImporter` 的变更保持向后兼容（旧 JSON 仍可正常导入）
- 不引入新的第三方依赖
- 遵循项目既有的数据驱动架构
