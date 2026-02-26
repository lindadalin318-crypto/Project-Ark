# 需求文档：示巴星 ACT1+ACT2 关卡布局重构

## 引言

当前的 `Sheba_ACT1_ACT2.json` 关卡布局是一条完全线性的"一本道"结构——所有房间从左到右一字排开，连接全部是 east→west 单一方向。这与银河恶魔城游戏的核心体验背道而驰。

银河恶魔城的地图精髓在于：**网状互连、垂直层叠、密钥循环（Key-Lock Loop）、捷径回路（Shortcut Loop）、视觉诱惑与延迟回报**。玩家应该在探索中不断发现新区域、解锁捷径回老区域、从上层跌落到下层、从底层爬升到顶层，体验立体的空间感和探索奖励感。

本次重构将：
1. 基于 `关卡心流与节奏控制-2.csv` 中的 10 个时间段节点（ACT1 Z1a~Z1f + ACT2 Z2a~Z2d）重新设计房间布局
2. 从一本道改为网状拓扑，包含分支路径、垂直层级、捷径回路、锁钥机制
3. JSON 格式完全对齐 LevelDesigner.html 的最新功能（connections 单/双向、doorLinks 门关联）
4. 保留 CSV 中的心流节奏（张力曲线 4→2→7→4→3→6→5→3→5→9）

---

## 需求

### 需求 1：网状拓扑结构 — 多路径探索

**用户故事：** 作为一名玩家，我希望关卡有多条可探索路径和分支，以便我有银河恶魔城式的"地图在手，世界任闯"的探索快感

#### 验收标准

1. WHEN 玩家完成教学区(Z1a)后 THEN 地图 SHALL 提供至少2条不同的前进路径（如：东向主路 / 南向地下通道）
2. WHEN 玩家到达 ACT1 中段(Z1c~Z1d) THEN 地图 SHALL 至少有一个分支汇聚点（两条路径汇合到同一区域）
3. WHEN 整体布局完成 THEN ACT1+ACT2的房间连接图 SHALL 存在至少3个环路（循环路径），而非纯树状结构
4. WHEN 房间摆放时 THEN position 坐标 SHALL 利用上下左右四个方向铺展，而非仅从左到右
5. IF 某段是纯线性走廊（如Boss前的紧迫感走廊） THEN 该段 SHALL 不超过连续3个房间

### 需求 2：垂直层级与 floor 系统

**用户故事：** 作为一名玩家，我希望关卡有上下层的空间感，以便体验"跌落到深渊"或"攀爬到高处"的垂直探索乐趣

#### 验收标准

1. WHEN 整体布局完成 THEN ACT1+ACT2 SHALL 使用至少3个不同的 floor 值（如 -1, 0, 1）
2. WHEN ACT1 音叉林(Z1d)区域 THEN 该区域 SHALL 包含 floor=0 和 floor=1 的房间（音叉林有重力减弱/高跳设定，适合垂直探索）
3. WHEN ACT2 叹息峡谷(Z2a) THEN 该区域 SHALL 包含 floor=0 和 floor=-1 的房间（峡谷降临段暗示垂直下降）
4. WHEN 不同 floor 的房间连接时 THEN connections 中 SHALL 使用 north/south 方向表示上下层过渡
5. IF 存在多层区域 THEN 每层 SHALL 至少有2个房间（避免孤立的单房间层）

### 需求 3：捷径回路与锁钥循环

**用户故事：** 作为一名玩家，我希望探索后能发现连回安全区的捷径，以便获得"哇，这里连通了"的惊喜感和便利

#### 验收标准

1. WHEN 玩家首次穿越 Z1b→Z1c→Z1d 后 THEN 地图 SHALL 提供至少一条从 Z1d 区域通回 Z1a/Z1b 安全区的捷径（单向门或可解锁通道）
2. WHEN 玩家获得涟漪能力(Z2b) 后 THEN 地图 SHALL 暗示有新的可探索路径（需要涟漪才能进入的区域）
3. WHEN 整个ACT1+ACT2完成 THEN 地图 SHALL 包含至少2条单向门连接（用于捷径/跌落，JSON中表现为只有一条 connection A→B 而无 B→A）
4. WHEN 整个ACT1+ACT2完成 THEN 地图 SHALL 包含至少2个可选的支线区域（非主线必经，但有额外奖励）

### 需求 4：房间类型与元素多样性

**用户故事：** 作为一名关卡设计师，我希望每个房间的功能类型和内部元素清晰明确，以便在 LevelDesigner.html 中直观地理解关卡节奏

#### 验收标准

1. WHEN 导出 JSON 时 THEN 每个房间 SHALL 包含与 CSV 对应的 type（safe/normal/arena/boss）
2. WHEN 房间是安全区(safe) THEN 该房间 SHALL 至少包含一个 checkpoint 元素
3. WHEN 房间是竞技场(arena/boss) THEN 该房间 SHALL 至少包含一个 enemy 元素
4. WHEN 房间包含门通往其他区域 THEN 该房间 SHALL 包含对应的 door 元素
5. WHEN 整个布局完成 THEN 元素类型 SHALL 包括 spawn、checkpoint、enemy、chest、npc、door 六种
6. WHEN Boss房间(Z2d Boss竞技场) THEN 该房间 SHALL 有明显更大的 size（至少 18x12）以适配Boss战

### 需求 5：JSON 格式对齐 LevelDesigner.html

**用户故事：** 作为一名开发者，我希望 JSON 能被 LevelDesigner.html 完整导入并正确渲染，以便使用可视化工具编辑和调整

#### 验收标准

1. WHEN JSON 导入到 LevelDesigner THEN 所有房间 SHALL 正确显示位置、大小、类型、层级
2. WHEN JSON 导入到 LevelDesigner THEN 所有 connections SHALL 正确渲染连接线
3. WHEN JSON 导入到 LevelDesigner THEN 所有 doorLinks SHALL 正确关联门元素
4. WHEN 导出的 JSON 数据结构 THEN 每个 room SHALL 包含字段：id, name, type, floor, position[x,y], size[w,h], elements[]
5. WHEN 导出的 JSON 数据结构 THEN 每条 connection SHALL 包含字段：from, to, fromDir, toDir
6. WHEN 导出的 JSON 数据结构 THEN 每条 doorLink SHALL 包含字段：roomId, entryDir, doorIndex
7. WHEN 双向通道(大部分房间连接) THEN JSON 中 SHALL 包含两条 connection（A→B 和 B→A）
8. WHEN 单向通道(捷径/跌落) THEN JSON 中 SHALL 只包含一条 connection（仅 A→B）
9. IF 房间有 comment 字段(用于人类阅读) THEN 该字段 SHALL 被保留但不影响导入导出

### 需求 6：心流节奏保持

**用户故事：** 作为一名关卡设计师，我希望重构后的布局仍然严格遵循 CSV 的张力曲线，以便保证游戏体验的情绪起伏不变

#### 验收标准

1. WHEN ACT1 THEN 张力曲线 SHALL 保持 4→2→7→4→3→6 的起伏节奏
2. WHEN ACT2 THEN 张力曲线 SHALL 保持 5→3→5→9 的递增节奏
3. WHEN 高张力节点(Z1c=7, Z2d=9) THEN 对应的房间群 SHALL 在空间上形成相对封闭/紧凑的竞技场区域
4. WHEN 低张力节点(Z1b=2, Z1e=3) THEN 对应的房间群 SHALL 在空间上更开阔，且周围有可选探索分支
5. WHEN 房间总数 THEN ACT1+ACT2 SHALL 保持在 30~50 个房间之间（当前28个，网状化后可适当增加中转/隐藏房间）

---

## 拓扑设计概念

为了实现上述需求，建议的空间拓扑概念如下（非强制，供任务规划参考）：

### ACT1（Z1 坠机冰原）拓扑概念

```
                    [Z1c上层·观景台] (floor=1)
                         |
[Z1a坠机点]——[Z1b冰雕广场]——[Z1c觉醒战场]——[Z1d音叉林上层] (floor=1)
     |              |              |              |
     |         [Z1b隐藏地穴]  [Z1c侧路]    [Z1d音叉林中层] (floor=0)
     |          (floor=-1)        |              |
     |              |         [捷径回Z1b]    [Z1d花园·安全区]
     |              └────────────┘              |
     └────[Z1e战后余波]————[Z1f下行通道·棱镜战]
                |                    |
           [Z1e道德房间]        [Z1f缕拉目击点]——[ACT1出口]
```

### ACT2（Z2 叹息峡谷）拓扑概念

```
[Z2a管风琴走廊]——[Z2a上层平台] (floor=1)
     |                 |
[Z2a缕拉援助]    [Z2a上层秘密]
     |
[Z2b声之走廊]——[Z2b隐藏房间·缕拉面对面]
     |                 |
[Z2c叹息深谷]    [Z2b涟漪奖励]
     |         ↗(单向跌落回Z2c)
[Z2c战斗验证]——[Z2c回音路]
     |              |
[Z2c世界时钟预兆]  [Z2c频率标记]
     |              |
     └——[Z2d前室]——┘
            |
     [Z2d Boss竞技场]
            |
     [Z2d Boss后奖励]——[ACT2出口]
```

这些概念展示了网状、分支、垂直层叠和捷径回路的设计意图。
