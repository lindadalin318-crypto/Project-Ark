# Slay the Spire 2 解包完整报告

> **解包日期**：2026-03-16  
> **工具链**：GDRE Tools v2.4.0 + ILSpy (ilspycmd) v9.1.0  
> **游戏版本**：Early Access（截至 2026-03-11 更新）  
> **输出路径**：`C:\Temp\StS2_Ripped`（Godot 项目）+ `C:\Temp\StS2_CSharp`（C# 源码）

---

## 一、技术栈识别

| 项目 | 值 |
|------|-----|
| 游戏引擎 | **Godot 4.5.1 (Mono)** |
| 脚本语言 | **C# (.NET 9)** + 少量 GDScript |
| 渲染器 | Vulkan / D3D12 / OpenGL3（可选） |
| 资源格式 | `.pck`（Godot PCK v2） |
| 游戏逻辑 DLL | `sts2.dll`（8.8 MB，.NET 9 Native AOT + IL 混合） |
| 开发商命名空间 | `MegaCrit.Sts2.*` |
| Mod 支持 | 内置 `0Harmony.dll`（HarmonyX），官方 Mod API |
| 第三方 | FMOD（音频）、Spine（骨骼动画）、Steamworks.NET |

与 Godot 3/GDScript-only 项目不同，StS2 的所有游戏逻辑都在 C# DLL 中，GDScript 仅用于少量 Godot 专属的编辑器/工具脚本（48 个 .gd 文件 vs 3245 个 .cs 脚本）。

---

## 二、解包方法与工具

### 2.1 资源解包：GDRE Tools v2.4.0

**GDRE Tools** 是 Godot 专属逆向工具，支持 Godot 3/4，能做到：
- 从 `.pck` 提取所有资源（纹理、场景、音频、字体）
- 将二进制 `.scn`/`.res` 转换回文本 `.tscn`/`.tres`
- 反编译 GDScript `.gdc` 字节码回 `.gd` 源码
- 通过 `--csharp-assembly` 参数支持 C# 项目

**执行命令**：
```batch
C:\Temp\gdre_tools\gdre_tools.exe --headless ^
  "--recover=E:\SteamLibrary\steamapps\common\Slay the Spire 2\SlayTheSpire2.pck" ^
  "--output=C:\Temp\StS2_Ripped" ^
  "--csharp-assembly=E:\SteamLibrary\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\sts2.dll"
```

> **注意**：必须用 `.bat` 脚本或 `cmd /c` 执行，不能通过 PowerShell 管道（`| Select-Object`）——管道会截断进程导致提前退出。

**耗时**：约 1 分 51 秒

### 2.2 C# 代码反编译：ILSpy (ilspycmd)

StS2 使用 C# .NET，直接用 `ilspycmd` 反编译为完整 C# 项目（含 namespace 目录结构）：

```bash
ilspycmd "E:\SteamLibrary\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\sts2.dll" -p -o "C:\Temp\StS2_CSharp"
```

**耗时**：约 25 秒  
**输出**：3299 个 `.cs` 文件，生成完整 `sts2.csproj`

> **与 Unity IL2CPP 的对比**：Unity IL2CPP 游戏（如 GG）的代码被 AOT 编译到 native DLL，只能用 Il2CppDumper 提取签名，无法还原方法体。StS2 的 sts2.dll 是托管的 IL 代码，ILSpy 可以直接还原完整方法逻辑，质量几乎等同于原始源码。

---

## 三、解包统计

| 类别 | 数量 |
|------|------|
| 反编译 C# 脚本 | **3,245** 个 |
| 失败脚本 | 15 个（< 0.5%） |
| 场景 (.tscn) | **907** 个 |
| 资源 (.tres) | **493** 个 |
| PNG 纹理 | **3,365** 个 |
| GDScript (.gd) | 48 个 |
| 总文件数 | **24,139** 个 |
| 已导入资源 | 3,821 个（转换率 100%） |
| Lossy 转换 | 804 个（压缩纹理还原为 PNG） |
| 解包大小 | 约 2,415 MB |
| ILSpy 输出（C#） | 3,299 个 .cs 文件 |

---

## 四、项目目录结构

```
C:\Temp\StS2_Ripped\
├── scenes/                     # 907 个 .tscn 场景
│   ├── game.tscn               # 游戏主场景
│   ├── run.tscn                # 一局 Run 场景
│   ├── one_time_initialization.tscn
│   ├── asset_loader.tscn
│   ├── cards/                  # 卡牌相关场景
│   ├── combat/                 # 战斗场景
│   ├── encounters/             # 遭遇战
│   ├── events/                 # 随机事件
│   ├── screens/                # 所有 UI 界面
│   │   ├── MainMenu
│   │   ├── CharacterSelect
│   │   ├── Map
│   │   ├── CardSelection
│   │   ├── Shops
│   │   └── ... (17+ 子目录)
│   ├── relics/                 # 遗物场景
│   ├── orbs/                   # 球场景
│   ├── potions/                # 药水
│   ├── vfx/                    # 特效场景
│   └── ...
├── images/                     # 3,365 张 PNG 纹理
├── animations/                 # Spine + Godot 动画
├── fonts/                      # 多语言字体
├── shaders/                    # GLSL 着色器
├── themes/                     # Godot UI 主题
├── materials/                  # 材质资源
├── localization/               # 多语言文本
├── models/                     # 3D 模型（?)
├── banks/                      # FMOD 音频 Banks
├── addons/                     # Godot 插件
└── src/                        # C# 源码（从 DLL 还原，镜像 ilspycmd 输出）
    ├── Core/                   # 核心游戏逻辑
    └── GameInfo/               # 游戏数据定义
```

---

## 五、C# 代码模块架构（MegaCrit.Sts2.*）

`C:\Temp\StS2_CSharp\` 目录结构直接反映了游戏架构：

### 核心系统层 (`MegaCrit.Sts2.Core.*`)

| 模块 | 命名空间 | 职责 |
|------|---------|------|
| **战斗核心** | `Core.Combat` | 战斗循环、历史记录、回合管理 |
| **实体系统** | `Core.Entities.*` | Cards、Relics、Powers、Potions、Orbs、Creatures |
| **动作队列** | `Core.GameActions` | 所有游戏动作（含 Multiplayer） |
| **命令系统** | `Core.Commands` | 命令模式，含 Builder |
| **地图** | `Core.Map` | 地图生成与导航 |
| **存档** | `Core.Saves.*` | 存档/读档 + 多版本迁移 |
| **随机** | `Core.Random` | RNG 管理 |
| **概率** | `Core.Odds` | 掉落率计算 |
| **奖励** | `Core.Rewards` | 奖励生成 |
| **房间** | `Core.Rooms` | 各类房间逻辑 |
| **Run 管理** | `Core.Runs.*` | Run 状态、历史、指标 |
| **解锁** | `Core.Unlocks` | 解锁系统 |
| **时间线** | `Core.Timeline.*` | 故事线 + Epoch 系统 |
| **钩子系统** | `Core.Hooks` | Mod API 钩子点 |
| **Mod 支持** | `Core.Modding` | HarmonyX Mod 接口 |

### UI 节点层 (`MegaCrit.Sts2.Core.Nodes.*`)

| 模块 | 内容 |
|------|------|
| `Nodes.Cards` | 卡牌节点、持有者 |
| `Nodes.Combat` | 战斗 UI 节点 |
| `Nodes.Screens.*` | 所有界面（主菜单、地图、商店、遗物收藏等 17 个界面） |
| `Nodes.Vfx.*` | 视觉特效（背景、卡牌、事件） |
| `Nodes.Pooling` | 对象池 |

### 数据定义层 (`MegaCrit.Sts2.GameInfo.*`)

- `GameInfo.Objects` — 所有游戏内对象的静态数据定义

### AutoSlay（自动战斗 AI）

完整的 AutoSlay 模块（`Core.AutoSlay.*`），包含：
- `Handlers.Rooms` — 各房间自动处理
- `Handlers.Screens` — 各界面自动操作
- `Helpers` — AI 辅助工具

---

## 六、关键架构发现

### 6.1 命令-动作模式

StS2 的战斗系统用**命令模式** + **动作队列**：

```
Core.Commands / Core.Commands.Builders  →  命令构建
Core.GameActions / Core.GameActions.Multiplayer  →  动作执行
Core.Combat.History / Core.Combat.History.Entries  →  历史记录
```

与 StS1 继承自 GDX 的 Java 架构一脉相承，但迁移到了 C#/Godot。

### 6.2 Hooks 系统（Mod API）

`Core.Hooks` 命名空间提供了官方的 Mod 钩子点，配合 `0Harmony.dll`（HarmonyX），Mod 可以在不修改源码的情况下 patch 任意方法。

### 6.3 多人游戏

完整的 Multiplayer 子系统：

```
Core.Multiplayer/
├── Connection/     — 网络连接管理
├── Game/           — 游戏同步逻辑（含 Lobby）
├── Messages/       — 消息协议（Game/Lobby 两套）
│   └── Game/       — Checksums、Flavor、Sync
├── Quality/        — 网络质量监控
├── Replay/         — 录像回放
├── Serialization/  — 状态序列化
└── Transport/      — 传输层（ENet + Steam P2P）
```

### 6.4 存档迁移系统

`Core.Saves.Migrations.*` 有完整的版本迁移链：
- PrefsSaves / ProfileSaves / ProgressSaves
- RunHistories / SerializableRuns
- SettingsSaves / Shared

说明 StS2 从早期版本就设计了向前兼容存档格式。

### 6.5 Spine 骨骼动画

游戏角色/怪物使用 **Spine** 骨骼动画（`libspine_godot.dll`），在 Godot 中通过 `Core.Bindings.MegaSpine` 绑定层调用。

---

## 七、资源规格参考（部分）

### 7.1 卡牌场景

主卡牌场景：`scenes/cards/card.tscn`  
卡牌网格：`scenes/cards/card_grid.tscn`  
材质：`card_canvas_group_blur_material.tres`、`card_canvas_group_mask_material.tres`

### 7.2 音频

使用 **FMOD Studio**，音频 Banks 存于 `banks/` 目录（`.bank` 文件）。不使用 Godot 原生音频系统。

### 7.3 本地化

`localization/` 目录含多语言文本文件，支持 CJK 多种字体（FiraSans、NotoSansCJK、SourceHanSerif、GyeonggiCheonnyeon）。

---

## 八、解包方法论总结

### Godot 游戏解包路线图

```
游戏类型判断
├── GDScript (无 C#)
│   └── GDRE Tools --recover  →  直接得到 .gd 源码 + 场景资源
│
└── C# (Mono / .NET)
    ├── GDRE Tools --recover  →  场景/资源 + C# DLL 引用
    └── ilspycmd -p           →  完整 C# 源码（比 IL2CPP 质量高得多）
```

### 关键陷阱

| 陷阱 | 说明 |
|------|------|
| **PowerShell 管道截断进程** | `gdre_tools.exe ... \| Select-Object` 会导致进程提前退出；必须用 `.bat` 或 `cmd /c` 执行 |
| **--output-dir 不存在** | GDRE 的参数是 `--output=<DIR>`，不是 `--output-dir` |
| **--path 参数不支持** | GDRE 是 Godot export template 编译，不支持 `--path`；PCK 文件直接作为 `--recover=` 的值传入 |
| **进度条会把 Select-Object 的截断当成"完成"** | 要等 `Recovery finished` 字样出现才是真正完成 |

---

## 九、数据存放位置

| 内容 | 路径 |
|------|------|
| 完整 Godot 项目（场景/资源/纹理） | `C:\Temp\StS2_Ripped\` |
| C# 源码（ILSpy 反编译） | `C:\Temp\StS2_CSharp\` |
| 原始 PCK | `E:\SteamLibrary\steamapps\common\Slay the Spire 2\SlayTheSpire2.pck` |
| 原始 C# DLL | `...data_sts2_windows_x86_64\sts2.dll` |
| GDRE Tools | `C:\Temp\gdre_tools\gdre_tools.exe` |
