# 星象仪 (Astrolabe) — STS2 智能决策顾问 Mod

## 核心功能

- **多方案并行**：为当前 Run 同时追踪 1-3 套强力构筑路线，随玩家选择动态收束
- **选牌建议**：每张候选牌在各方案下的推荐评级 + 理由
- **地图规划**：各方案对应的推荐路线高亮
- **篝火决策**：休息 vs 升级的智能判断，并指出推荐升级哪张牌
- **商店建议**（Phase 2）：购买优先级 + 金币分配建议
- **战斗走牌建议**（Phase 2）：每回合最优出牌顺序

## 开发环境要求

| 工具 | 版本 |
|------|------|
| .NET SDK | 9.0 |
| Godot Mono | 4.5.1（生成 .pck 用）|
| STS2 | Steam，任意最新版 |
| IDE | Rider 或 Visual Studio 2022+ |

## 快速开始

### 1. 配置本地路径

```bash
cd src/Astrolabe
cp local.props.example local.props
# 编辑 local.props，填入你的 STS2 安装路径和 Godot 路径
```

### 2. 构建

```bash
cd src/Astrolabe
dotnet build
```

构建成功后，`Astrolabe.dll` 会自动复制到 `STS2 安装目录/mods/Astrolabe/`。

### 3. 生成 .pck（首次或资产变更后）

```bash
cd src/Astrolabe/pack
godot --headless --export-pack "Windows Desktop" Astrolabe.pck
# 将生成的 Astrolabe.pck 复制到 mods/Astrolabe/ 目录
```

### 4. 启动游戏验证

查看日志：`%AppData%\Roaming\SlayTheSpire2\Player.log`

成功加载时应看到类似日志：
```text
=== Astrolabe v0.1.0 initializing ===
[DataLoader] Loaded 87 entries from cards.core.json
[DataLoader] Loaded 87 entries from cards.advisor.json
[DataLoader] Merged 87 card records from cards.core.json + cards.advisor.json
[DataLoader] Loaded 12 entries from buildpaths.json
=== Astrolabe initialized successfully ===
```


## 项目结构

```
StS2mod/
├── Docs/
│   ├── STS2_Asset_ID_System.md  # 解包资产与运行时 ID 规范
│   └── STS2_Unpack_Report.md    # StS2 解包方法与资产总览
├── src/
│   └── Astrolabe/
│       ├── Astrolabe.csproj     # 项目文件
│       ├── ModEntry.cs          # [ModInitializer] 入口
│       ├── Core/                # 状态快照（RunSnapshot / CombatSnapshot）
│       ├── Engine/              # 顾问引擎（BuildPathManager / CardAdvisor）
│       ├── Hooks/               # Harmony Hook（各界面）
│       ├── UI/                  # Godot HUD 面板
│       ├── Data/                # JSON 数据模型 + 加载器
│       └── pack/                # Godot 资产打包配置
├── data/                        # 游戏数据库 JSON
│   ├── cards.core.json          # 卡牌事实层（费用/类型/稀有度/结构化效果槽位）
│   ├── cards.advisor.json       # 卡牌顾问层（评分/流派权重/协同标签/建议先验）
│   ├── relics.json              # 遗物评分
│   ├── buildpaths.json          # 构筑方案定义（4职业各3套）
│   ├── bosses.json              # Boss 机制 + 针对建议
│   └── events.json              # 事件选项期望值

└── GDD.md                       # 策划案（完整设计文档）
```

## ⚠️ 技术验证待办

以下内容需要**反编译 `sts2.dll`**（使用 ILSpy）后确认：

| 文件 | 需要确认的内容 |
|------|--------------|
| `Core/RunStateReader.cs` | Player HP/Gold/Floor/Deck 的实际字段名 |
| `Core/CombatStateReader.cs` | Energy/Hand/Enemy Intent 的实际字段名 |
| `Hooks/CardRewardHook.cs` | 卡牌奖励界面的实际类名和方法名 |
| `Hooks/MapScreenHook.cs` | 地图界面的实际类名和方法名 |
| `Hooks/CampfireHook.cs` | 篝火界面的实际类名和方法名 |
| `UI/OverlayHUD.cs` | 游戏根节点的实际类名（用于注入 CanvasLayer）|

所有 TODO 标注的位置都有完整的伪代码框架，反编译确认后直接替换即可。

### 反编译步骤

1. 下载 [ILSpy](https://github.com/icsharpcode/ILSpy/releases)
2. 打开 `F:\SteamLibrary\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\sts2.dll`
3. 搜索关键字：`CardReward`、`MapScreen`、`Campfire`、`RunManager`、`Player`
4. 记录实际的类名和字段名，替换各文件中的 TODO 注释

## 数据库维护

`data/cards.advisor.json` 中的评分与建议先验基于社区 Tier List 和高端玩家攻略；`data/cards.core.json` 负责客观卡牌事实与结构化字段槽位。如需更新：

- 参考 Reddit r/slaythespire、Steam 讨论区的 Tier List 帖子
- 在 `cards.advisor.json` 中修改对应卡牌的 `base_score`、`path_scores`、`tier`、`synergy_tags`
- 在 `cards.core.json` 中补充 `target`、`effects`、`flags` 等结构化事实字段
- `path_scores` 中的 key 需与 `buildpaths.json` 中的 `path_id` 对应


## 参考资料

- [BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2) — 官方 Mod 框架
- [ModTemplate-StS2](https://github.com/Alchyr/ModTemplate-StS2) — Mod 项目模板
- [Harmony 2 文档](https://harmony.pardeike.net/) — 运行时补丁框架
- 游戏路径：`F:\SteamLibrary\steamapps\common\Slay the Spire 2`
- 游戏版本：`v0.98.3`，运行时 `.NET 9.0`，引擎 `Godot 4.5.1`
