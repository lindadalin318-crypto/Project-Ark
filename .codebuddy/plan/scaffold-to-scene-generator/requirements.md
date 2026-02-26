# 需求文档：一键 Scaffold-to-Scene 关卡生成器

## 引言

本功能是一个 Unity Editor 脚本 `ScaffoldToSceneGenerator`，实现从 `LevelScaffoldData` 资产（如 `示巴星___ACT1+ACT2__Z1a→Z2d_.asset`）一键生成完整、可运行的关卡场景。

### 背景

当前项目已有两个关卡生成器：
- `ShebaLevelScaffolder`（已标记 `[Obsolete]`）：Code-First，功能完整（Room + RoomSO + CameraConfiner + Door双向 + SpawnPoints + Encounter），但硬编码12个房间
- `LevelGenerator`：数据驱动，读取 `LevelScaffoldData`，但依赖 `LevelElementLibrary` Prefab（未创建），不创建 CameraConfiner，不创建双向门

本脚本融合两者优势：**数据驱动 + Code-First**（不依赖任何 Prefab），从 scaffold 数据一键生成37个房间的完整可玩关卡。

### 技术约束

- 项目：Unity 6 + URP 2D + C#
- 脚本必须放在 `Assets/Scripts/Level/Editor/` 目录
- 使用 `#if UNITY_EDITOR` 守卫
- 不需要任何外部 Prefab 或 LevelElementLibrary
- Player Layer 名称固定为 `"Player"`
- 默认敌人 Prefab 路径：`Assets/_Prefabs/Enemies/Enemy_Rusher.prefab`
- 所有资产（RoomSO、EncounterSO、CheckpointSO）保存到 `Assets/_Data/Level/` 子目录

### 现有数据结构参考

**RoomType 枚举**：Normal(0), Arena(1), Boss(2), Safe(3)
> 注意：HTML 导入器将 "Exploration" 类型映射为 RoomType(3)，但实际枚举中(3)是 `Safe`。scaffold asset 中 roomType 值可能为 0/1/2/3。

**DoorState 枚举**：Open(0), Locked_Combat(1), Locked_Key(2), Locked_Ability(3), Locked_Schedule(4)

**ScaffoldElementType 枚举**：Wall(0), WallCorner(1), CrateWooden(2), CrateMetal(3), Door(4), Checkpoint(5), PlayerSpawn(6), EnemySpawn(7), Hazard(8)

---

## 需求

### 需求 1：EditorWindow 入口与一键操作

**用户故事：** 作为一名关卡设计师，我希望通过菜单打开一个编辑器窗口，拖入 LevelScaffoldData 后点一个按钮就能生成完整关卡，以便快速验证关卡设计。

#### 验收标准

1. WHEN 用户点击菜单 `Window > ProjectArk > Generate Level From Scaffold` THEN 系统 SHALL 打开一个 EditorWindow。
2. WHEN EditorWindow 打开 THEN 系统 SHALL 显示一个 `LevelScaffoldData` 对象槽位（ObjectField）。
3. WHEN 用户拖入有效的 `LevelScaffoldData` 资产并点击 "Generate" 按钮 THEN 系统 SHALL 在当前场景中生成所有关卡内容。
4. WHEN 点击 "Generate" THEN 系统 SHALL 显示确认对话框，提示将要创建的资产数量和覆盖风险。
5. WHEN 生成完成 THEN 系统 SHALL 在 Console 中输出完整的生成报告（房间数、门数、资产数）和后续 TODO checklist。
6. WHEN 生成过程中发生错误 THEN 系统 SHALL 通过 Undo 回滚所有场景修改。

---

### 需求 2：Room GameObject 生成

**用户故事：** 作为一名关卡设计师，我希望每个 scaffold 房间都自动生成为配置完整的 Room GameObject，以便我不需要手动添加和配置组件。

#### 验收标准

1. WHEN 系统处理一个 `ScaffoldRoom` THEN 系统 SHALL 创建一个 GameObject，名称为房间的 `_displayName`。
2. WHEN 创建 Room GameObject THEN 系统 SHALL 添加 `Room` 组件 + `BoxCollider2D`（isTrigger=true，size 匹配 `_size`）。
3. WHEN 创建 Room GameObject THEN 系统 SHALL 将其位置设置为 `_position`。
4. WHEN 创建 Room GameObject THEN 系统 SHALL 在其下创建一个名为 `CameraConfiner` 的子物体，Layer 设为 `Ignore Raycast`（index=2），带 `PolygonCollider2D`（isTrigger=false），顶点匹配房间边界矩形。
5. WHEN 创建 Room GameObject THEN 系统 SHALL 通过 `SerializedObject` 将 `Room._confinerBounds` 指向 CameraConfiner 的 PolygonCollider2D。
6. WHEN 创建 Room GameObject THEN 系统 SHALL 通过 `SerializedObject` 将 `Room._playerLayer` 设为 `LayerMask.GetMask("Player")`。
7. IF "Player" Layer 不存在 THEN 系统 SHALL 输出 Warning 日志但不中断生成。
8. WHEN 所有房间创建完成 THEN 系统 SHALL 将它们全部放在一个名为 `--- {LevelName} ---` 的根节点下。

---

### 需求 3：RoomSO 资产自动创建

**用户故事：** 作为一名关卡设计师，我希望每个房间都自动生成对应的 RoomSO 资产并正确关联，以便房间的元数据（ID、名称、类型、楼层、Encounter）自动填充。

#### 验收标准

1. WHEN 系统为一个 ScaffoldRoom 创建 RoomSO THEN 系统 SHALL 创建 `RoomSO` ScriptableObject 并保存到 `Assets/_Data/Level/Rooms/{sanitizedDisplayName}_Data.asset`。
2. WHEN 创建 RoomSO THEN 系统 SHALL 通过 `SerializedObject` 设置 `_roomID`（使用 scaffold 的 `_roomID`）、`_displayName`、`_floorLevel`（使用 scaffold 的 `_floorLevel` 或 position.z）、`_type`（使用 scaffold 的 `_roomType`）。
3. WHEN 创建 RoomSO THEN 系统 SHALL 将其赋值给 Room 组件的 `_data` 字段。
4. IF 目标路径已存在同名 RoomSO THEN 系统 SHALL 加载已有资产并更新字段，而非重复创建。
5. WHEN 目录不存在 THEN 系统 SHALL 自动创建所需目录结构。

---

### 需求 4：Door 双向连接生成

**用户故事：** 作为一名关卡设计师，我希望每条 scaffold connection 都自动生成双向门（从A到B + 从B到A），并且门的目标房间、SpawnPoint、LayerTransition 都配置好，以便玩家可以在房间之间自由穿梭。

#### 验收标准

1. WHEN 系统处理一个 `ScaffoldDoorConnection` THEN 系统 SHALL 在源房间下创建一个 Door 子物体，名称格式为 `Door_to_{targetDisplayName}`。
2. WHEN 创建 Door THEN 系统 SHALL 添加 `Door` 组件 + `BoxCollider2D`（isTrigger=true，size=3×3）。
3. WHEN 创建 Door THEN 系统 SHALL 将 Door 的 localPosition 设为 connection 的 `_doorPosition`。
4. WHEN 创建 Door THEN 系统 SHALL 在目标房间下创建一个 SpawnPoint 子物体，位置为目标房间对应 connection 的 `_doorPosition`（如果能找到反向 connection）或基于 `_doorDirection` 反方向偏移计算。
5. WHEN 创建 Door THEN 系统 SHALL 通过 `SerializedObject` 设置 `_targetRoom`（指向目标房间的 Room 组件）、`_targetSpawnPoint`（指向目标房间中的 SpawnPoint Transform）、`_isLayerTransition`、`_initialState`（默认 Open）、`_playerLayer`。
6. WHEN 目标房间中已有反向 connection 指回源房间 THEN 系统 SHALL 复用反向 connection 生成的 SpawnPoint，避免重复创建。
7. IF connection 有 DoorConfig（通过 scaffold element 的 `_boundConnectionID` 绑定）THEN 系统 SHALL 应用 DoorConfig 的 `_initialState`、`_requiredKeyID`、`_openDuringPhases`。

---

### 需求 5：Scaffold Element 实体化

**用户故事：** 作为一名关卡设计师，我希望 scaffold 中的各种元素（PlayerSpawn、EnemySpawn、Checkpoint、Door元素等）都自动生成为对应的 GameObject 并配置好组件，以便关卡功能完整。

#### 验收标准

1. WHEN 系统处理 `ScaffoldElementType.PlayerSpawn` THEN 系统 SHALL 在房间下创建一个名为 `PlayerSpawn` 的空 GameObject，位于 element 的 `_localPosition`。
2. WHEN 系统处理 `ScaffoldElementType.EnemySpawn` THEN 系统 SHALL 在房间下创建一个名为 `EnemySpawn_{index}` 的空 GameObject，位于 element 的 `_localPosition`，并将其加入 Room 的 `_spawnPoints` 数组。
3. WHEN 系统处理 `ScaffoldElementType.Checkpoint` THEN 系统 SHALL 在房间下创建一个 Checkpoint GameObject，添加 `Checkpoint` 组件 + `BoxCollider2D`（isTrigger=true，size=2×2），设置 `_playerLayer`，并创建对应的 `CheckpointSO` 资产（保存到 `Assets/_Data/Level/Checkpoints/`，配置 `_checkpointID`、`_displayName`、`_restoreHP=true`、`_restoreHeat=true`）。
4. WHEN 系统处理 `ScaffoldElementType.Door` 且有非空 `_boundConnectionID` THEN 系统 SHALL 按需求 4 的逻辑创建门，并应用元素的 `_doorConfig`。
5. WHEN 系统处理其他类型（Wall、WallCorner、CrateWooden、CrateMetal、Hazard）THEN 系统 SHALL 创建空占位 GameObject，名称标注类型名，位于 element 的 `_localPosition`。

---

### 需求 6：Arena/Boss 房间战斗配置

**用户故事：** 作为一名关卡设计师，我希望 Arena 和 Boss 类型的房间自动配置好战斗所需的组件和默认遭遇战数据，以便按下 Play 后就能看到锁门→刷怪→清完→开门的完整流程。

#### 验收标准

1. WHEN 系统处理 `RoomType.Arena` 或 `RoomType.Boss` 房间 THEN 系统 SHALL 在 Room GameObject 上添加 `ArenaController` 组件。
2. WHEN 系统处理 Arena/Boss 房间 THEN 系统 SHALL 在房间下创建一个 `EnemySpawner` 子物体，添加 `EnemySpawner` 组件，并将房间中所有 EnemySpawn 元素的 Transform 绑定到 `_spawnPoints` 数组。
3. WHEN 系统处理 Arena/Boss 房间 THEN 系统 SHALL 创建一个 `EncounterSO` 资产，保存到 `Assets/_Data/Level/Encounters/{sanitizedRoomName}_Encounter.asset`。
4. WHEN 创建 EncounterSO THEN 对于 Arena 房间，系统 SHALL 配置 1 波，3 个 `Enemy_Rusher`（从 `Assets/_Prefabs/Enemies/Enemy_Rusher.prefab` 加载）。
5. WHEN 创建 EncounterSO THEN 对于 Boss 房间，系统 SHALL 配置 2 波：第 1 波 2 个 Enemy_Rusher（delay=0s），第 2 波 3 个 Enemy_Rusher（delay=1.5s）。
6. WHEN 创建 EncounterSO THEN 系统 SHALL 将其赋值给 `RoomSO._encounter` 字段。
7. IF `Enemy_Rusher.prefab` 不存在 THEN 系统 SHALL 创建空壳 EncounterSO（波次存在但 EnemyPrefab=null），并输出 Warning。
8. WHEN 创建的 EncounterSO 名称 THEN 系统 SHALL 包含 `[DEFAULT]` 标记，便于后续批量识别和替换。

---

### 需求 7：SpawnPoints 绑定与 PlayerLayer 统一配置

**用户故事：** 作为一名关卡设计师，我希望所有需要 PlayerLayer 的组件（Room、Door、Checkpoint）都自动配置好 Player 检测层，以便不需要手动逐个设置。

#### 验收标准

1. WHEN 系统创建 Room 组件 THEN 系统 SHALL 将 `_playerLayer` 设为 `LayerMask.GetMask("Player")`。
2. WHEN 系统创建 Door 组件 THEN 系统 SHALL 将 `_playerLayer` 设为 `LayerMask.GetMask("Player")`。
3. WHEN 系统创建 Checkpoint 组件 THEN 系统 SHALL 将 `_playerLayer` 设为 `LayerMask.GetMask("Player")`。
4. WHEN 系统创建 Room 组件 THEN 系统 SHALL 收集该房间下所有 EnemySpawn Transform，赋值给 `Room._spawnPoints` 数组。

---

### 需求 8：Undo 支持与验证报告

**用户故事：** 作为一名关卡设计师，我希望整个生成操作可以通过 Ctrl+Z 一键撤销，并且生成完成后有清晰的报告告诉我做了什么、还需要做什么。

#### 验收标准

1. WHEN 生成操作执行 THEN 系统 SHALL 将所有操作包装在一个 Undo Group 中。
2. WHEN 生成过程中发生异常 THEN 系统 SHALL 回滚整个 Undo Group。
3. WHEN 生成完成 THEN 系统 SHALL 输出如下报告到 Console：
   - 生成的房间总数
   - 创建的门总数（包含双向）
   - 创建的 RoomSO 资产数
   - 创建的 EncounterSO 资产数
   - 创建的 CheckpointSO 资产数
   - Arena/Boss 房间中缺少 EnemySpawn 元素的警告
4. WHEN 生成完成 THEN 系统 SHALL 输出后续 TODO checklist：
   - 为 Tilemap 绘制房间地图
   - 替换 `[DEFAULT]` EncounterSO 中的敌人 Prefab
   - 为 Boss 房间配置专属 Boss Prefab
   - 添加视觉装饰、光照、粒子特效
   - 在 Checkpoint 上添加 SpriteRenderer
   - 配置 Physics2D 碰撞矩阵

---

### 需求 9：Normal 类型房间的战斗处理

**用户故事：** 作为一名关卡设计师，我希望 Normal 类型的房间如果包含 EnemySpawn 元素也能自动配置好 EnemySpawner，以便巡逻怪可以正常生成。

#### 验收标准

1. WHEN 系统处理 `RoomType.Normal` 房间且该房间含有 `ScaffoldElementType.EnemySpawn` 元素 THEN 系统 SHALL 在房间下创建 `EnemySpawner` 子物体并绑定 SpawnPoints。
2. WHEN Normal 房间有 EnemySpawn 元素 THEN 系统 SHALL 创建一个 `EncounterSO`，配置 1 波、2 个 `Enemy_Rusher`（比 Arena 更少），并赋值给 RoomSO._encounter。
3. WHEN Normal 房间没有 EnemySpawn 元素 THEN 系统 SHALL 不创建 EnemySpawner 或 EncounterSO。
4. WHEN Normal 房间有战斗配置 THEN 系统 SHALL **不** 添加 ArenaController 组件（Normal 房间玩家可自由离开，门不锁）。
