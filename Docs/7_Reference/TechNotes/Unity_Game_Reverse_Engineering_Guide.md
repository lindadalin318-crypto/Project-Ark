# Unity 游戏解包逆向工程指南

> 本文基于对以下游戏的实战解包经验总结：
> - **Galactic Glitch**（IL2CPP, Unity 2021）
> - **Backpack Monsters**（Mono, Unity 2022）
> - **Magicraft**（IL2CPP, Unity 6000.0.62f1）
> - **Minishoot' Adventures**（Mono, Unity 2021.3.14f1）
> - **Rain World**（Mono, Unity 2021.x）
> - **TUNIC**（IL2CPP, Unity 2020.3.17）
>
> 涵盖工具选型、完整流程、自动化方案、配置数据读取、踩坑记录，以及对 Project Ark 的架构参考价值。

---

## 一、首先要判断：Mono 还是 IL2CPP？

这是一切的起点。两种后端决定了你用哪套工具链。

```
游戏目录/
├── GameName_Data/
│   ├── Managed/          ← 有这个文件夹 = Mono（直接可反编译）
│   │   └── Assembly-CSharp.dll
│   └── il2cpp_data/      ← 有这个文件夹 = IL2CPP（需要专门工具）
│       └── Metadata/
│           └── global-metadata.dat
└── GameAssembly.dll      ← IL2CPP 专有，native code 本体
```

| 特征 | Mono | IL2CPP |
|------|------|--------|
| 代码可读性 | dnSpy/ILSpy 直接反编译，可读性接近源码 | Il2CppDumper 提取签名（类/字段/方法名），但**无逻辑体** |
| 资产提取（内置类型） | UnityPy / AssetStudio 均可 | UnityPy 可读 Texture2D/AudioClip 等内置类型 |
| 资产提取（MonoBehaviour） | 完整字段值 | TypeTree 缺失，需 AssetRipper 重建 |
| 完整项目还原 | AssetRipper ✓ | AssetRipper ✓（Unity 6 也支持） |
| 难度 | ★★☆☆☆ | ★★★★☆ |
| 代表游戏 | Backpack Monsters、Hollow Knight（旧版） | Galactic Glitch、Magicraft、大多数商业发行游戏 |

### 如何判断 Unity 版本？

从 `globalgamemanagers` 文件头读取（前 200 字节含 ASCII 版本字符串）：

```powershell
$bytes = [System.IO.File]::ReadAllBytes("GameName_Data\globalgamemanagers")
$text = [System.Text.Encoding]::ASCII.GetString($bytes[0..200])
# 输出类似：...6000.0.62f1...
```

也可从 `UnityPlayer.dll` 的文件版本信息读取：
```powershell
$vi = [System.Diagnostics.FileVersionInfo]::GetVersionInfo("UnityPlayer.dll")
Write-Host $vi.FileVersion   # 如：6000.0.62.16359173
```

---

## 二、工具箱总览

### 代码层工具

| 工具 | 用途 | 支持 | 下载 |
|------|------|------|------|
| **dnSpy** | Mono DLL 反编译，可读性极高，支持断点调试 | Mono | GitHub: dnSpy/dnSpy |
| **ILSpy** | 轻量 Mono 反编译，支持搜索 | Mono | GitHub: icsharpcode/ILSpy |
| **Mono.Cecil** | .NET 库，可在 PowerShell 中程序化读取 DLL 类型（含 DummyDll） | Mono / DummyDll | NuGet |
| **Il2CppDumper** | 从 `global-metadata.dat` + `GameAssembly.dll` 提取所有类/字段/方法签名 | IL2CPP | GitHub: Perfare/Il2CppDumper |
| **Ghidra / IDA Pro** | 逆向 GameAssembly.dll native 函数体（极难，通常不必要） | IL2CPP | — |

**Il2CppDumper 输出文件说明**：

| 输出文件 | 大小参考 | 用途 |
|----------|---------|------|
| `dump.cs` | 60-100 MB | 所有类/字段/方法的 C# 签名，可 `grep` 搜索 |
| `script.json` | 200-300 MB | 方法内存地址映射，供 Ghidra/Cheat Engine 使用 |
| `il2cpp.h` | 90-100 MB | C 结构体头文件，供 Ghidra 插件使用 |
| `DummyDll/` | 100-200 MB | 113+ 个 DLL，可用 dnSpy/ILSpy/Mono.Cecil 查看类型树 |
| `stringliteral.json` | 2-3 MB | 字符串字面量，可查找游戏内文本/资源路径 |

### 资产层工具

| 工具 | 用途 | 支持 | 推荐度 |
|------|------|------|--------|
| **AssetRipper** | 完整导出为 Unity 项目（YAML Prefab + SO + 场景），IL2CPP 下最可靠，支持 Web API 自动化 | Mono ✓ / IL2CPP ✓ / Unity 6 ✓ | ★★★★★ |
| **AssetStudio** | GUI 资产浏览器，查看/导出贴图、音频、Mesh | Mono ✓ / IL2CPP ⚠️（MonoBehaviour 字段为空） | ★★★★☆ |
| **UnityPy** | Python 脚本化批量资产导出，适合贴图/音频等内置类型 | Mono ✓ / IL2CPP ⚠️（仅内置类型） | ★★★★☆ |
| **UABE** | 修改资产包 | Mono ✓ | ★★☆☆☆ |

### 运行时工具

| 工具 | 用途 |
|------|------|
| **Cheat Engine** | 内存扫描，运行时抓取浮点数值（速度、伤害等），验证配置值是否与运行值一致 |
| **x64dbg** | 断点调试 native 代码 |
| **dnSpy（Play Mode Attach）** | Mono 游戏运行时直接断点 |

---

## 三、AssetRipper Web API 自动化（重点）

AssetRipper 1.3.x 提供 Web GUI 模式，可通过 HTTP API 完全自动化控制——**无需手动操作浏览器**。

### 启动方式

```powershell
# headless 模式：不弹出浏览器，指定端口
Start-Process -FilePath "AssetRipper.GUI.Free.exe" `
    -ArgumentList "--headless", "--port", "9856", "--log", "--log-path", "output.log" `
    -WindowStyle Hidden

# 等待启动
Start-Sleep -Seconds 5
Test-NetConnection -ComputerName "localhost" -Port 9856
```

### 完整 API 端点列表（通过 `/openapi.json` 获取）

| 端点 | 方法 | 参数 | 用途 |
|------|------|------|------|
| `/LoadFile` | POST | `path=<文件路径>` | 加载单个资产文件 |
| `/LoadFolder` | POST | `path=<目录路径>` | 加载整个游戏数据目录 ✅ |
| `/Export/UnityProject` | POST | `path=<输出目录>` | 导出完整 Unity 项目 ✅ |
| `/Export/PrimaryContent` | POST | `path=<输出目录>` | 仅导出主要内容 |
| `/Assets/Image` | GET | — | 浏览图片资产 |
| `/Assets/Audio` | GET | — | 浏览音频资产 |
| `/Reset` | POST | — | 重置已加载数据 |

**⚠️ 关键注意**：
- 路径格式：`application/x-www-form-urlencoded`（不是 JSON）
- 需要 `--data-urlencode` 或 `curl.exe` 来正确处理反斜杠路径
- 导出操作耗时长（1-15 分钟），HTTP 请求会超时，这是正常现象——导出在后台继续运行，监控日志文件查看进度

### PowerShell 完整自动化脚本

```powershell
$GAME_DATA = "F:\SteamLibrary\steamapps\common\MyGame\MyGame_Data"
$OUTPUT_DIR = "D:\ReferenceAssets\MyGame_Ripped"
$LOG_FILE   = "D:\ReferenceAssets\assetripper.log"
$AR_EXE     = "D:\Tools\AssetRipper\AssetRipper.GUI.Free.exe"
$PORT       = 9856

# 1. 启动 AssetRipper headless
Start-Process -FilePath $AR_EXE `
    -ArgumentList "--headless", "--port", $PORT, "--log", "--log-path", $LOG_FILE `
    -WindowStyle Hidden
Start-Sleep -Seconds 5

# 2. 加载游戏目录
curl.exe -s -X POST "http://localhost:$PORT/LoadFolder" `
    -H "Content-Type: application/x-www-form-urlencoded" `
    --data-urlencode "path=$GAME_DATA" --max-time 30

# 3. 等待加载完成（检查首页按钮文字变化）
$timeout = 300
$elapsed = 0
while ($elapsed -lt $timeout) {
    $status = curl.exe -s "http://localhost:$PORT/" --max-time 5
    if ($status -match "Export") { Write-Host "加载完成"; break }
    Start-Sleep -Seconds 10; $elapsed += 10
}

# 4. 触发导出（会超时，属正常）
curl.exe -s -X POST "http://localhost:$PORT/Export/UnityProject" `
    -H "Content-Type: application/x-www-form-urlencoded" `
    --data-urlencode "path=$OUTPUT_DIR" --max-time 30

# 5. 监控日志进度
while ($true) {
    $last = Get-Content $LOG_FILE | Select-Object -Last 3
    $last | ForEach-Object { Write-Host $_ }
    if ($last -match "Finished post-export") { Write-Host "导出完成！"; break }
    Start-Sleep -Seconds 15
}
```

### 导出结果目录结构

```
ExportedProject/
├── Assets/
│   ├── AnimationClip/       ← 2,000-3,000 个 .anim 文件
│   ├── AnimatorController/  ← 500-700 个状态机，含 Transition 条件
│   ├── AudioClip/           ← 音频（wav/ogg）
│   ├── GameObject/          ← 独立 Prefab（Player.prefab 等）
│   ├── Material/            ← 材质（IL2CPP 下 Shader 逻辑丢失，但属性值保留）
│   ├── MonoBehaviour/       ← ScriptableObject 序列化值（.asset）
│   ├── Resources/
│   │   ├── configs/         ← JSON 游戏配置（如果游戏用 JSON 存数据）
│   │   ├── prefabs/         ← 按类型分类的 Prefab（spell/units/ui/ef 等）
│   │   └── sounds/          ← 音频资源
│   ├── Scripts/             ← IL2CPP 下是空壳脚本（无逻辑）
│   ├── Sprite/              ← 精灵图
│   ├── Texture2D/           ← 贴图（png/tga）
│   └── _Scenes/             ← 场景文件（.unity）
├── Packages/
└── ProjectSettings/
    └── Physics2D.asset      ← 物理设置（重力/碰撞矩阵）
```

---

## 四、Il2CppDumper 使用指南

### 下载与运行

```powershell
# 获取最新版本 URL
$release = Invoke-RestMethod "https://api.github.com/repos/Perfare/Il2CppDumper/releases/latest"
$url = ($release.assets | Where-Object { $_.name -like "*net6*" })[0].browser_download_url

# 下载并解压
curl.exe -L -o Il2CppDumper.zip $url
Expand-Archive Il2CppDumper.zip -DestinationPath D:\Tools\Il2CppDumper

# 运行（非交互模式，输出到指定目录）
# 注意：结束时会提示 "Press any key"，无交互控制台时报错但不影响输出
& "D:\Tools\Il2CppDumper\Il2CppDumper.exe" `
    "F:\SteamLibrary\steamapps\common\MyGame\GameAssembly.dll" `
    "F:\SteamLibrary\steamapps\common\MyGame\MyGame_Data\il2cpp_data\Metadata\global-metadata.dat" `
    "D:\ReferenceAssets\MyGame_dump"
```

运行成功的输出：
```
Initializing metadata...
Metadata Version: 31
Initializing il2cpp file...
Il2Cpp Version: 31
Searching...
CodeRegistration : 1855e0720
MetadataRegistration : 1867e8e40
Dumping... Done!
Generate struct... Done!
Generate dummy dll... Done!
```

### 用 Mono.Cecil 程序化分析 DummyDll

```powershell
# Mono.Cecil.dll 在 Il2CppDumper 目录里就有
Add-Type -Path "D:\Tools\Il2CppDumper\Mono.Cecil.dll"
$module = [Mono.Cecil.ModuleDefinition]::ReadModule("D:\MyGame_dump\DummyDll\Assembly-CSharp.dll")

# 统计命名空间
$module.Types | Group-Object { $_.Namespace } | Sort-Object Count -Descending | Select-Object -First 20

# 按关键词搜索类
$module.Types | Where-Object { $_.Name -match "Spell|Wand|Combat" } | Select-Object Name

# 查看某个类的字段
$wand = $module.Types | Where-Object { $_.Name -eq "Wand" }
$wand.Fields | Format-Table Name, @{N='Type';E={$_.FieldType.Name}} -AutoSize

# 查看某个类的方法
$wand.Methods | Where-Object { -not $_.Name.StartsWith("<") -and -not $_.Name.StartsWith("get_") } |
    Select-Object Name | Format-Table -AutoSize
```

### dump.cs 高效检索

```powershell
# 搜索关键枚举
Select-String -Path "dump.cs" -Pattern "enum SpellType|enum ItemType|enum WeaponType"

# 提取某个枚举的全部值（PowerShell）
$content = Get-Content "dump.cs" -Raw
$match = [regex]::Match($content, 'public enum SpellType[^{]*\{([^}]+)\}')
$match.Groups[1].Value -split "`n" | Where-Object { $_ -match "const" } | ForEach-Object { $_.Trim() }

# 搜索某个类的字段（grep 方式）
Select-String -Path "dump.cs" -Pattern "class WandConfig" -A 80 | Select-Object -First 80
```

---

## 五、UnityPy 批量美术导出

UnityPy 对 IL2CPP 游戏的内置资产类型（Texture2D、Sprite、AudioClip）**完全有效**，只有 MonoBehaviour 字段读不到。

### 安装

```powershell
pip install UnityPy
# 如果 import 找不到，需要显式添加路径
$site = python -c "import site; print(site.getsitepackages()[0])"
```

### 完整批量导出脚本（IL2CPP 兼容）

```python
import sys, os
# 如有路径问题，显式添加 site-packages
sys.path.insert(0, r"C:\Users\YourName\AppData\Local\Programs\Python\Python313\Lib\site-packages")

import UnityPy
from pathlib import Path

GAME_DATA_DIR = r"F:\SteamLibrary\steamapps\common\MyGame\MyGame_Data"
OUTPUT_DIR    = r"D:\ReferenceAssets\MyGame_Art"

out_tex    = Path(OUTPUT_DIR) / "Texture2D"
out_sprite = Path(OUTPUT_DIR) / "Sprites"
out_audio  = Path(OUTPUT_DIR) / "Audio"
for d in [out_tex, out_sprite, out_audio]:
    d.mkdir(parents=True, exist_ok=True)

print(f"加载 {GAME_DATA_DIR}（大型游戏可能需要 2-3 分钟）...")
env = UnityPy.load(GAME_DATA_DIR)  # 加载整个数据目录，自动处理 .resS

objs = list(env.objects)
print(f"共 {len(objs)} 个对象，开始导出...")

tex_count = sprite_count = audio_count = error_count = 0

for i, obj in enumerate(objs):
    if i % 5000 == 0:
        print(f"  进度: {i}/{len(objs)} ({i*100//len(objs)}%) "
              f"贴图:{tex_count} Sprite:{sprite_count} 音频:{audio_count}")

    try:
        if obj.type.name == "Texture2D":
            data = obj.read()
            name = data.m_Name or f"tex_{obj.path_id}"
            safe = "".join(c for c in name if c not in r'\/:*?"<>|')
            out = out_tex / f"{safe}.png"
            if not out.exists():
                img = data.image
                if img: img.save(str(out))
            tex_count += 1

        elif obj.type.name == "Sprite":
            data = obj.read()
            name = data.m_Name or f"sprite_{obj.path_id}"
            safe = "".join(c for c in name if c not in r'\/:*?"<>|')
            out = out_sprite / f"{safe}.png"
            if not out.exists():
                img = data.image
                if img: img.save(str(out))
            sprite_count += 1

        elif obj.type.name == "AudioClip":
            data = obj.read()
            name = data.m_Name or f"audio_{obj.path_id}"
            safe = "".join(c for c in name if c not in r'\/:*?"<>|')
            if hasattr(data, 'samples') and data.samples:
                for fname, sample_data in data.samples.items():
                    ext = Path(fname).suffix or ".wav"
                    out = out_audio / f"{safe}{ext}"
                    if not out.exists():
                        out.write_bytes(sample_data)
                audio_count += 1
            elif hasattr(data, 'm_AudioData') and data.m_AudioData:
                out = out_audio / f"{safe}.wav"
                if not out.exists():
                    out.write_bytes(bytes(data.m_AudioData))
                audio_count += 1

    except Exception:
        error_count += 1

print(f"\n=== 导出完成 ===")
print(f"  Texture2D: {tex_count}  Sprite: {sprite_count}  AudioClip: {audio_count}  错误: {error_count}")
```

**关键 API 变更（UnityPy 1.x）**：
- 属性名改为 `data.m_Name`（不是旧版的 `data.name`）
- 加载整个目录 `UnityPy.load(dir)` 会自动关联 `.resS` 外部资源文件
- 部分 Sprite 的 `data.image` 依赖 Texture2D，需要完整加载整个 env 才能正确切片

---

## 六、Galactic Glitch 实战复盘（IL2CPP，飞船参数提取）

### 目标

提取飞船物理参数（`linearDrag`、`moveAcceleration`、`maxMoveSpeed`、`angularAcceleration` 等）用于 Project Ark 手感复刻。

### 尝试过的方案

| 方案 | 结果 | 失败原因 |
|------|------|---------|
| UnityPy 读 MonoBehaviour | ✗ | IL2CPP 剥离了 TypeTree，`raw_data` 为空 bytes |
| UnityPy + DummyDll TypeTree 注入 | ✗ | 依赖链复杂，字段偏移量对不上 |
| 特征扫描 Raw Bytes（找 IEEE 754 浮点值） | ✗ | `raw_data` 为空，无字节可扫 |
| **AssetRipper headless + REST API** | **✓** | AssetRipper 自己实现了 IL2CPP TypeTree 重建逻辑 |

### 最重要发现：参数是状态机驱动的

解包前以为参数是"一套静态数值"，实际上是**状态机实时切换**：

```
正常飞行(IsBlueState)     → linearDrag=3,   maxSpeed=7.5, angularAccel=80
Boost状态(IsBoostState)   → linearDrag=2.5, maxSpeed=9,   angularAccel=40
Dodge状态(IsDodgeState)   → linearDrag=1.7, maxSpeed=4,   angularAccel=20
主攻击(IsMainAttackState) → linearDrag=3,   maxSpeed=7.5, angularAccel=720 ← 射击时极灵敏来自这里！
```

**教训**：先 Il2CppDumper 看类结构，理解架构后再 AssetRipper 看实际值。从来不要直接假设参数是静态的。

### 踩坑记录

| 坑 | 根因 | 正确做法 |
|----|------|----------|
| AssetRipper API `/api/LoadFolder` 404 | 路径多了 `/api/` 前缀 | 正确路径是 `/LoadFolder`（无前缀） |
| POST 返回 415 | Content-Type 错误 | 改为 `application/x-www-form-urlencoded` |
| 导出请求超时 | 大资产包，30s 不够 | 后台异步，监控日志轮询进度 |
| `BoosterBurnoutPower` 误判为推力参数 | 名字误导 | 看完整字段：它有 `damagePerSecond`，是战斗 Power |

---

## 七、Backpack Monsters 复盘（Mono，背包系统分析）

Mono 游戏流程简单得多。

### 代码分析

```
1. 用 dnSpy 打开 Assembly-CSharp.dll
2. Ctrl+F 搜索 GridManager / PlaceableController / Placeable
3. 直接看 C# 源码，质量接近原始代码
4. 重点读 OnPointerDrag / CanPlace / CommitPlace
```

### 实现要点（已完整分析）

| 系统 | 实现 | 关键类 |
|------|------|--------|
| 背包格子 | 二维 bool 数组，每格存 `itemId` | `GridManager` |
| 物品形状 | `ItemShapeSO` 存多边形偏移列表，支持旋转 | `PlaceableSO` |
| 拖拽逻辑 | `OnPointerDrag` → `CanPlace()` 碰撞检测 → `CommitPlace()` | `PlaceableController` |
| 物理弹出效果 | `Rigidbody2D` dynamic → kinematic 切换 | `Placeable` |
| 融合动画 | DOTween 序列（链式 Append） | `Placeable` |

### 美术导出

Mono 游戏用 UnityPy 导出（参见第五节脚本），效果最好。Backpack Monsters 导出结果：
- Texture2D：580 张
- Sprite：775 张
- AudioClip：271 个

---

## 八、Magicraft 完整解构复盘（IL2CPP，Unity 6）

Magicraft 是本文档涉及的最复杂案例：Unity 6、IL2CPP、DOTS/ECS 架构、含完整 JSON 配置数据。

### 基本信息

| 项目 | 内容 |
|------|------|
| Unity 版本 | 6000.0.62f1（Unity 6） |
| 后端 | IL2CPP |
| 架构特点 | **大量 DOTS/ECS**（962 个 Authoring/System/Job 类），法术弹幕全跑在 Jobs 线程 |
| 第三方库 | Rukhanka（高性能骨骼动画）、Steamworks.NET、Beebyte 代码混淆 |
| 资产规模 | resources.assets 约 1.4GB，Texture2D 约 10,000 张 |

### 解构流程与结果

**Step 1：Il2CppDumper（约 30 秒）**

```
输出：dump.cs 62MB + DummyDll 113个 + il2cpp.h 97MB + script.json 249MB
核心发现：3,055 个类，其中 962 个 DOTS System/Authoring
```

**Step 2：UnityPy 批量美术导出（约 30 分钟）**

```
输出：Texture2D 9,189 张（427 MB），Sprite 5,379 张，AudioClip 833 个
```

**Step 3：AssetRipper 完整项目导出（约 10 分钟）**

```
输出：72,705 个文件，含完整 JSON 游戏配置数据
最有价值：Assets/Resources/configs/ 目录（见下方）
```

### 最有价值的发现：完整 JSON 配置数据

Magicraft 把游戏数值存在 JSON 文件里（而非 ScriptableObject），AssetRipper 导出后可直接 `ConvertFrom-Json` 读取：

```powershell
$configDir = "D:\ReferenceAssets\Magicraft\Magicraft_Ripped\ExportedProject\Assets\Resources\configs"

# 读取法术配置
$spells = Get-Content "$configDir\SpellConfig.json" | ConvertFrom-Json
Write-Host "法术总数：$($spells.Count)"  # 329

# 查看某个法术
$spells | Where-Object { $_.id -eq 10011 } | ConvertTo-Json

# 读取魔杖配置
$wands = Get-Content "$configDir\WandConfig.json" | ConvertFrom-Json
Write-Host "魔杖总数：$($wands.Count)"  # 135

# 读取遗物配置
$relics = Get-Content "$configDir\RelicConfig.json" | ConvertFrom-Json
Write-Host "遗物总数：$($relics.Count)"  # 96
```

**配置文件清单**：

| 文件 | 条目数 | 内容 |
|------|--------|------|
| `SpellConfig.json` | 329 | 法术基础属性（伤害/速度/MP消耗/类型） |
| `WandConfig.json` | 135 | 魔杖属性（槽位/MP/冷却/后置槽触发类型） |
| `RelicConfig.json` | 96 | 遗物属性（含升级值、符文积分、皮肤名） |
| `UnitConfig.json` | 462 | 单位属性（HP/速度/元素免疫/死亡特效） |
| `CurseConfig.json` | 61 | 诅咒配置 |
| `RoomConfig-0/1/2/3.json` | 各约 12MB | 章节关卡房间配置（极大！） |
| `TextConfig_Spell.json` | — | 法术多语言文本（774KB） |
| `TextConfig_Wand.json` | — | 魔杖多语言文本（113KB） |

### Magicraft 核心设计对 Project Ark 的启示

**1. 后置槽（Post Slot）充能触发条件**（最值得借鉴）

Wand 的 `postSlotTriggerType` 支持 8 种触发条件：
```
移动充能    → PostslotMoveChargeRatio
击杀充能    → PostslotKillEnemyChargeRatio
命中充能    → PostslotSpellHitChargeRatio
暴击充能    → PostslotCriticalHitChargeRatio
受伤充能    → PostslotTakeDamageChargeRatio
时间充能    → PostslotTimeChargeRatio
高伤充能    → PostslotHighDamageChargeRatio
蓄力充能    → PostslotCastSpellChargeRatio
```

这与 Project Ark 的 WeaponTrack 激活条件完全对应，可作为扩展方向参考。

**2. 法术分层架构**

```
SpellType：
  Missile = 0   投射物型（有 prefab，有飞行实体）
  Summon  = 1   召唤型（生成持久实体）
  Enhance = 2   强化型（无 prefab，纯数值修改器！对应 Project Ark 的 Prism）
  Passive = 3   被动型（无 prefab，持续效果）
```

Enhance 型法术 `prefab = ""`，通过 `float1/2/3 + int1/2/3` 传参给相邻的投射物法术——和 Project Ark 棱镜（Prism）作为修改器的设计思路高度相似。

**3. 法术升级系统**

同一法术类型有 1/2/3 级，`id` 末位为级别，价格按 3 倍递增（6→18→54 金币）。

**4. SpellBase 的方法架构（302 个方法，128 个公共方法）**

关键设计模式：
- `OnHitUnit` / `OnHitWallAndSolidObj` / `OnHitSpell`：分类命中回调
- `TryRefract` / `TryRefractOrPenetrate` / `TrySplit`：法术行为链（折射/穿透/分裂）
- `ApplyElementEffect`：元素效果统一入口（8 种元素）
- `PoolRecycle`：对象池回收

---

## 九、完整解包流程（通用）

### Phase 1：情报侦察（30 分钟）

```
目标：弄清这个游戏用了什么，有哪些系统
```

1. **判断 Mono / IL2CPP**
2. **提取类列表**：Il2CppDumper（IL2CPP）或 dnSpy（Mono）
3. **关键词搜索系统地图**：

```
Player, Character, Ship          → 核心实体
Inventory, Item, Bag, Spell      → 背包/物品系统
Combat, Damage, Health, Hit      → 战斗系统
State, Brain, FSM, Behavior      → AI系统
Manager, Controller, System      → 管理器架构
Authoring, Job, ECS, Dots        → DOTS 架构（Unity 新项目）
ScriptableObject, Data, Config   → 数据层
Pool, Spawn, Factory             → 对象池
Save, Checkpoint, Progress       → 存档系统
UI, HUD, Panel, Screen           → UI架构
```

4. **用 Mono.Cecil 程序化统计**（比手动 grep 高效得多）：

```powershell
Add-Type -Path "Il2CppDumper\Mono.Cecil.dll"
$module = [Mono.Cecil.ModuleDefinition]::ReadModule("DummyDll\Assembly-CSharp.dll")

# 按命名空间统计（快速了解架构分层）
$module.Types | Group-Object { $_.Namespace } | Sort-Object Count -Descending

# 找所有 System 类（DOTS 架构）
$module.Types | Where-Object { $_.Name -match "System$" } | Measure-Object

# 找所有管理器
$module.Types | Where-Object { $_.Name -match "Mgr$|Manager$" } | Select-Object Name
```

### Phase 2：数据提取（1-2 小时）

```
目标：把所有 ScriptableObject / JSON 配置 / Prefab 序列化值读出来
```

**推荐工具**：AssetRipper（全量导出，支持 Unity 6）

```powershell
# 使用第三节的自动化脚本
# 导出后重点查看：
Get-ChildItem "ExportedProject\Assets\Resources\configs" -Filter "*.json"
# 如果有 JSON 配置，直接 ConvertFrom-Json 读取，是最高价值的内容
```

**导出后检索**：

```powershell
# 找到包含速度参数的配置
Get-ChildItem "Assets\MonoBehaviour" -Filter "*.asset" -Recurse |
    Select-String "speed" | Select-Object -First 20

# 找 Player Prefab 里的组件
Get-Content "Assets\GameObject\Player.prefab" | Select-String "GGSteering" -A 30

# 查看场景列表
Get-ChildItem "Assets\_Scenes" -Filter "*.unity"
```

### Phase 3：美术资产批量导出（30-60 分钟）

使用第五节的 UnityPy 脚本。

**预先探测资产数量**（避免等待时间失控）：

```python
import UnityPy
from collections import Counter
env = UnityPy.load(r"GameName_Data\resources.assets")  # 先探最大单文件
objs = list(env.objects)
types = Counter(o.type.name for o in objs)
for t, c in types.most_common(10): print(f"  {t}: {c}")
```

### Phase 4：代码逻辑理解

**IL2CPP 路线**（只能读结构，不能读逻辑体）：
- `dump.cs` 看字段名 → 推断行为
- DummyDll + Mono.Cecil → 程序化分析类型关系
- Animator 状态机 YAML → 理解状态转换条件和时间参数
- Prefab YAML 里的 component 列表 → 理解组件架构

**Mono 路线**（可以完整读逻辑）：
- dnSpy 直接读方法体
- 重点读 `Update()`、`FixedUpdate()`、`Execute()` / `Tick()`
- 关注事件订阅（`AddListener`、`+=` 附近代码）

---

## 十、进阶技巧

### 技巧 1：用 Animator 反推游戏设计

AnimatorController YAML 里有：
- 所有状态名（直接对应游戏机制：`Attack_Combo1`、`DodgeRoll`、`Stun`）
- Transition 条件参数（`isGrounded: bool`、`attackIndex: int`）
- Transition 的 `exitTime` 和 `duration`（精确的攻击前摇/后摇时间）

**这些信息价值远超看字段名**——设计师认为这个角色有哪些状态，状态间怎么转换，都写在这里。

### 技巧 2：用 AudioClip 名字推断系统边界

音频命名直接反映游戏事件节点：
```
SE_Player_Dash_Start      → Dash 开始（有音效触发点）
SE_Player_Footstep_Grass  → 地面材质系统存在
SE_Enemy_Alert            → 敌人有发现玩家状态
SE_UI_Button_Hover        → UI 层有 Hover 反馈
SE_Spell_Hit_Fire         → 命中有元素类型区分
```

通过音频列表，可以**不读任何代码**就了解游戏有哪些系统和交互节点。

### 技巧 3：用 JSON 配置逆向 Balance Sheet

如果游戏用 JSON 存数据（Magicraft 的做法），直接用 PowerShell 分析：

```powershell
$spells = Get-Content "SpellConfig.json" | ConvertFrom-Json

# 伤害分布
$spells | Where-Object { $_.damage -gt 0 } | 
    Group-Object level | ForEach-Object { 
        "$($_.Name)级 - 平均伤害: $([math]::Round(($_.Group | Measure-Object damage -Average).Average))" 
    }

# 速度分布
$spells | Measure-Object speed -Min -Max -Average

# MP 消耗分布
$spells | Where-Object { $_.mpCost -gt 0 } | 
    Sort-Object mpCost | Format-Table id, mpCost, damage, speed -AutoSize
```

### 技巧 4：Cheat Engine 验证配置值

从 Prefab 读到 `maxSpeed = 7.5` 时，用 Cheat Engine 在运行中验证实际内存值。

如果不一致，说明有**运行时修改**（状态机切换参数、Modifier 系统叠加百分比）。这是发现"隐藏手感机制"的最可靠方法。

### 技巧 5：对照 Physics2D.asset 理解物理模型

```powershell
Get-Content "ProjectSettings\Physics2D.asset" | Select-String "m_Gravity|m_DefaultMaterial|m_LayerCollision"
```

### 技巧 6：用 stringliteral.json 找资源路径

Il2CppDumper 输出的 `stringliteral.json` 包含游戏内所有字符串字面量，可以找到：
- 资源加载路径（`"prefabs/spell/10011"`）
- 配置文件路径（`"configs/SpellConfig"`）
- 事件名称（`"OnPlayerDeath"`）

```powershell
$strings = Get-Content "stringliteral.json" | ConvertFrom-Json
$strings | Where-Object { $_.value -match "^configs|^prefabs" } | Select-Object value | Select-Object -First 30
```

---

## 十一、本地工具路径（Project Ark 环境）

| 工具 | 路径 |
|------|------|
| AssetRipper | `D:\Tools\AssetRipper\AssetRipper.GUI.Free.exe` |
| Il2CppDumper | `D:\Tools\Il2CppDumper\Il2CppDumper.exe` |
| Mono.Cecil | `D:\Tools\Il2CppDumper\Mono.Cecil.dll` |
| Python | `C:\Users\Anyee\AppData\Local\Programs\Python\Python313\python.exe` |
| UnityPy site-packages | `C:\Users\Anyee\AppData\Local\Programs\Python\Python313\Lib\site-packages` |

### 已解构的参考游戏

| 游戏 | 后端 | Unity 版本 | 输出目录 | 主要收获 |
|------|------|-----------|---------|---------|
| Galactic Glitch | IL2CPP | 2021.x | `D:\ReferenceAssets\GalacticGlitch\` | 飞船物理参数（状态机驱动） |
| Backpack Monsters | Mono | 2022.x | `D:\ReferenceAssets\BackpackMonsters\` | 背包格子/拖拽/融合系统 |
| Magicraft | IL2CPP | 6000.0.62f1 | `D:\ReferenceAssets\Magicraft\` | 法术系统/魔杖槽位/DOTS弹幕架构 |
| Minishoot' Adventures | Mono | 2021.3.14f1 | `D:\ReferenceAssets\Minishoot\` | Pattern-Action AI / CircleCast 子弹 / AnimationCurve 属性系统 |
| Rain World | Mono | 2021.x | `D:\ReferenceAssets\RainWorld\` | BodyChunk 物理 / 程序化动画 / LINEAGE 生态进化 / 调色板驱动渲染 |

---

## 十二、Minishoot' Adventures 实战复盘（Mono，完整源码分析）

Minishoot' Adventures 是一款 **Top-Down 弹幕射击 × 银河恶魔城**游戏，与 Project Ark 品类高度重合，是最直接的参考对象。

### 基本信息

| 项目 | 内容 |
|------|------|
| Unity 版本 | 2021.3.14f1 |
| 后端 | **Mono**（完整 C# 源码可读） |
| 第三方库 | DOTween、Sirenix Odin Inspector、I2 Localization、Easy Save 3 (ES3)、Steamworks.NET |
| 资产规模 | 337 个 C# 脚本，2,724 张贴图，4,424 个 Sprite，850 个音频，16 个场景 |
| 输出目录 | `D:\ReferenceAssets\Minishoot\ExportedProject\` |

### 解构流程

```
1. 确认 MonoBleedingEdge 目录 → Mono 构建
2. AssetRipper headless 导出（端口 8080，参数 path 小写）→ 12,513 个文件
3. 获得 337 个完整 C# 源文件（可直接阅读逻辑）
```

**AssetRipper 踩坑**：Minishoot 使用端口 8080，参数字段名为小写 `path`（不是 `outputPath`）。

### 核心架构发现

#### 继承层次

```
MonoBehaviour
└── MiniBehaviour          # 基类，封装 MiniUpdate（受 GameManager.InGame 控制）
    └── ShipBehaviour      # 所有飞船实体基类
        ├── Player         # 玩家（单例 Player.Instance）
        └── Enemy          # 敌人基类
            └── Boss       # Boss 特化
```

#### Pattern-Action AI（最值得借鉴）

```
Enemy
├── AIPatternManager    # 时间轴调度器，按 PatternStep 序列执行 Actions
├── AIWeapon            # 武器行为（实现 IPatternable）
├── AIFollow            # 追踪行为（实现 IPatternable）
├── AICharge / AIBusher / AISneak / AIGrid / AIScriptable
└── AINavigation        # 寻路（Dijkstra 图）
```

**关键设计**：`AIPatternAction` 通过**反射**动态分发 Data 和调用 `StartAction`，实现无 switch-case 的多态行为调度。比 HFSM 更轻量，适合中小型敌人。

#### CircleCast 子弹碰撞

```csharp
// Bullet.Update() 中每帧手动 CircleCast（非 Physics Trigger）
Physics2D.CircleCastNonAlloc(transform.position, collisionRadius, moveDir,
    castResults, moveDir.magnitude, collisionLayerMask)
```

避免 Unity 物理触发器的帧延迟，精度更高。子弹支持 `SinWave`（正弦波动轨迹）、`SpeedCurve`（速度曲线）、`Homing`（追踪子组件）。

#### AnimationCurve 属性系统

```csharp
// PlayerStats 静态计算层
public static float Compute(Stats id, int level = -1) {
    PlayerStatsDataStru data = PlayerData.GetStatsData(id);
    return data.Stats.GetValueByRatio((float)level / (float)data.LevelMax);
}
// 属性值通过 AnimationCurve.Evaluate(level/levelMax) 计算，曲线可视化调参
```

#### 敌人分级公式

```
Level = 3 * (Tier-1) + Size   （1-9 级）
HP = (hpBase * HpBaseFactor + hpBase * (level-1) * HpFactor * pow(level, level * HpPowFactor)) * sizeFactor
```

### 关键枚举（游戏设计核心）

```csharp
enum Stats { PowerAllyLevel, BoostSpeed, BulletNumber, BulletSpeed,
             PowerBombLevel, CriticChance, Energy, FireRange, FireRate,
             Hp, MoveSpeed, Supershot, BulletDamage, PowerSlowLevel }

enum Skill { Supershot, Dash, Hover, Boost }  // 解锁型技能

enum Modules { IdolBomb, IdolSlow, IdolAlly, BoostCost, XpGain, HpDrop,
               PrimordialCrystal, HearthCrystal, SpiritDash, BlueBullet,
               Overcharge, CollectableScan, Rage, Retaliation, FreePower,
               Compass, Teleport }  // 装备型模块
```

### 对 Project Ark 的借鉴

| 借鉴点 | Minishoot 实现 | Project Ark 应用方向 |
|--------|--------------|--------------------|
| Pattern-Action 调度 | `AIPatternManager` + `IPatternable` 反射分发 | 中小型敌人的轻量行为调度 |
| BulletData 数据驱动 | 速度/射程/正弦/角速度/缩放曲线集中在一个 struct | 子弹参数 SO 设计 |
| CircleCast 碰撞 | 每帧手动检测，无帧延迟 | 高速子弹的精确碰撞 |
| AnimationCurve 属性 | `level/levelMax` 映射到曲线 | 飞船属性升级曲线 |
| Additive 场景加载 | GlobalObjects 常驻，其他场景按需激活 | 关卡系统参考 |

---

## 十三、Rain World 实战复盘（Mono，StreamingAssets 文本架构）

Rain World 是一款**生态模拟 × 平台动作**游戏，以程序化生物 AI 和独特的物理/动画系统著称。

### 基本信息

| 项目 | 内容 |
|------|------|
| Unity 版本 | 2021.x |
| 后端 | **Mono**（`Assembly-CSharp.dll` 可直接反编译） |
| 渲染框架 | **Futile**（自定义 2D 渲染器，非 Unity SpriteRenderer） |
| 物理系统 | **完全自定义**（BodyChunk 质点 + SharedPhysics Verlet 积分） |
| 动画系统 | **完全程序化**（IK + 弹簧阻尼 + Perlin Noise，无任何 AnimationClip） |
| 模组框架 | BepInEx（活跃社区） |
| 输出目录 | `D:\ReferenceAssets\RainWorld\`（目前仅文本提取，未做完整 AssetRipper 导出） |

### 解构方法

Rain World 的大部分内容**不需要 AssetRipper**，因为游戏数据直接存在 StreamingAssets 的纯文本文件中：

```
RainWorld_Data/StreamingAssets/
├── world/          ← 世界图（直接可读的 .txt）
├── shaders/        ← 313 个着色器源码（直接可读的 .shader，运行时编译！）
├── palettes/       ← 54 个调色板（直接可读的 .png）
├── decals/         ← 265 个贴花（直接可读的 .png）
├── illustrations/  ← 251 张插图（直接可读的 .png）
└── loadedsoundeffects/ ← 1,620 个音效（直接可读的 .wav）
```

代码类名通过 Python 解析 .NET PE 元数据的 `#Strings` 堆提取（11,729 个符号）。

### 核心架构：极度反 Unity 惯例

```
标准 Unity 游戏：
  GameObject/Component → SpriteRenderer → Physics2D → Animator → AssetBundle

Rain World：
  PhysicalObject（BodyChunk 数组）→ Futile FSprite → 手写物理 → 程序化动画 → StreamingAssets 文本
```

### 程序化世界系统（最核心技术）

#### 世界图格式（文本邻接表）

```
// world_su.txt
ROOMS
SU_A33 : SU_B13, SU_B04, SU_A20          ← 房间连接（邻接表）
SU_S04 : SU_A37 : SHELTER                 ← 庇护所标记
SU_A40 : SU_A17, SU_B07 : SWARMROOM      ← 蝙蝠群房间
SU_C02 : SU_A45, SU_A07 : SCAVOUTPOST    ← 拾荒者哨站
GATE_SU_DS : SU_B14, DISCONNECTED : GATE ← 区域门
END ROOMS

CREATURES
SU_B11 : 4-CicadaB, 5-CicadaA            ← 固定生物（槽位-物种）
LINEAGE : SU_A31 : 3 : NONE-0.1, Small Centipede-1.0, Centipede-{0.3}-1.0, ...
(White)SU_C02 : 5-Pink, 2-BigNeedleWorm  ← 特定角色专属生物
END CREATURES
```

#### LINEAGE 进化链（最独特设计）

```
LINEAGE : 房间ID : 生成槽位 : 物种-概率, 物种-{参数}-概率, ...
```

- 每个 `LINEAGE` 条目定义一个**生成槽的进化链**
- 每个 Rain Cycle（约 5-8 分钟）结束后，该槽位从概率列表**重新随机选择**生物
- 玩家无法预测下一周期会遇到什么，增加探索不确定性
- `{0.3}` 等参数是生物的体型/强度参数

#### Room_Attr 生物偏好系统

```
// properties.txt
Room_Attr: SU_B04: PinkLizard-Like, GreenLizard-Like, Vulture-Avoid, Centipede-Forbidden
// 每个房间对每种生物有 Like/Avoid/Forbidden/Stay 标记
// AI 寻路权重受这些标记影响
// 这是设计师控制生态分布的主要工具
```

### 双层 AI 架构

```
AbstractCreatureAI（世界层）← 生物在玩家视野外时的轻量模拟（跨房间迁徙/觅食）
└── [Creature]AI（房间层）  ← 生物进入视野时的完整 AI
    ├── Tracker（感知：视觉/听觉/嗅觉）
    ├── Behavior（当前行为状态）
    ├── Relationships（与其他生物的关系）
    └── PathFinder（基于 Tile 图的 A*）
```

**70+ 种独立 AI 类**：`LizardAI`, `VultureAI`, `ScavengerAI`, `CentipedeAI`, `OverseerAI`...

**世界层调度**：`FliesWorldAI`（蝙蝠群迁徙）、`ScavengersWorldAI`（部落领地）、`OverseersWorldAI`（监视者分配）

**设计意义**：生物不是在玩家进入房间时才"生成"，而是一直在世界中活动，只是在视野外用轻量级模拟代替——这让世界感觉**真实存在**。

### 调色板驱动渲染

```
工作原理：
1. 所有 Sprite 使用灰度纹理（不含颜色信息）
2. 着色器运行时从调色板 PNG 采样颜色
3. 修改调色板即可实现区域主题切换（白天/黑夜/不同区域）
4. 313 个 .shader 文件以源码形式存在 StreamingAssets，运行时动态编译
```

### 资产统计

| 类别 | 数量 |
|------|------|
| 着色器（.shader 源码） | **313 个** |
| 音效 | **1,620 个** .wav |
| 程序化音乐片段 | **269 个** .ogg |
| 完整音乐曲目 | **267 个** .mp3 |
| 调色板 | **54 个** PNG |
| 贴花 | **265 个** PNG |
| 插图 | **251 张** PNG |
| 世界区域 | **12 个**（SU/CC/DS/HI/GW/SI/SH/SL/LF/UW/SB/SS） |

### 对 Project Ark 的借鉴

| 借鉴点 | Rain World 实现 | Project Ark 应用方向 |
|--------|----------------|--------------------|
| 双层 AI 架构 | AbstractAI（世界层）+ 实体 AI（房间层） | EnemyDirector 的世界层调度 |
| Room_Attr 偏好系统 | 文本文件控制每个房间的生态分布 | 关卡设计工具中的敌人分布控制 |
| LINEAGE 进化链 | 生成槽概率权重列表，每周期重新随机 | Rogue-like 关卡的生成槽变体 |
| 调色板驱动着色 | 灰度 Sprite + 运行时调色板采样 | 不同星域的视觉主题切换 |
| 程序化动画思路 | IK + 弹簧阻尼 + Perlin Noise | 飞船引擎尾焰/武器后坐力的自然感 |

---

## 十四、如果要完整复刻一个游戏——工作流

```
Week 1：情报阶段├── Il2CppDumper / dnSpy → 类列表 → 系统地图
├── AssetRipper 全量导出（优先读 JSON configs）
└── 分析核心循环：玩家 → 输入 → 物理 → 战斗 → 反馈

Week 2：框架搭建
├── 参考 Prefab 层级搭建 GameObject 结构
├── 参考 SO/JSON 数据创建等效 ScriptableObject
└── 实现核心移动（最先可感知的系统）

Week 3-4：系统复刻
├── 参考 Animator + dump.cs → 实现状态机
├── 参考 AudioClip 命名 → 实现音效触发点
├── 参考 Prefab 的 Component 列表 → 补充功能组件
└── Cheat Engine 对比运行时参数 → 发现隐藏的动态修改逻辑

Week 5+：美术集成
├── UnityPy 批量导出贴图/音频（仅用于参考）
├── 用音频文件名推断还原 SFX 触发点
└── 所有最终上线资产必须使用自制内容（版权合规）
```

---

## 十五、法律与伦理边界

| 行为 | 合法性 |
|------|--------|
| 解包用于学习/研究 | 大多数国家允许，属于逆向工程合理使用 |
| 提取美术资产用于**自己的非商业项目** | 灰色地带，通常被容忍 |
| 提取美术资产用于**商业项目** | **侵权，严禁** |
| 复刻游戏机制（不用原始资产） | 合法（机制不受版权保护） |
| 直接二次分发原游戏资产 | 侵权 |

**Project Ark 的正确做法**：学习 Magicraft 的法术系统机制和数值平衡用于设计参考，所有最终呈现的美术/音频使用自制资产。提取出来的资产可作为临时占位符或风格参考，上线前必须全部替换。

---

## 十六、快速参考卡片

### IL2CPP 游戏完整解包命令序列（PowerShell）

```powershell
$GAME  = "F:\SteamLibrary\steamapps\common\MyGame"
$DATA  = "$GAME\MyGame_Data"
$OUT   = "D:\ReferenceAssets\MyGame"
$DUMP  = "$OUT\dump"
$ART   = "$OUT\Art"
$RIPPED = "$OUT\Ripped"

# 1. 判断版本
$bytes = [System.IO.File]::ReadAllBytes("$DATA\globalgamemanagers")
[System.Text.Encoding]::ASCII.GetString($bytes[0..200]) | Select-String "20\d\d\.\d"

# 2. Il2CppDumper
New-Item -Force -ItemType Directory $DUMP
& "D:\Tools\Il2CppDumper\Il2CppDumper.exe" "$GAME\GameAssembly.dll" "$DATA\il2cpp_data\Metadata\global-metadata.dat" $DUMP

# 3. 用 Mono.Cecil 快速统计类
Add-Type -Path "D:\Tools\Il2CppDumper\Mono.Cecil.dll"
$m = [Mono.Cecil.ModuleDefinition]::ReadModule("$DUMP\DummyDll\Assembly-CSharp.dll")
$m.Types | Group-Object { $_.Namespace } | Sort-Object Count -Descending | Select-Object -First 15

# 4. AssetRipper 启动
Start-Process "D:\Tools\AssetRipper\AssetRipper.GUI.Free.exe" "--headless --port 9856 --log --log-path $OUT\ar.log"
Start-Sleep 5

# 5. 加载游戏数据
curl.exe -s -X POST "http://localhost:9856/LoadFolder" -H "Content-Type: application/x-www-form-urlencoded" --data-urlencode "path=$DATA" --max-time 30

# 6. 等待加载完成
while (-not (curl.exe -s "http://localhost:9856/" | Select-String "Export")) { Start-Sleep 15 }

# 7. 触发导出
New-Item -Force -ItemType Directory $RIPPED
curl.exe -s -X POST "http://localhost:9856/Export/UnityProject" -H "Content-Type: application/x-www-form-urlencoded" --data-urlencode "path=$RIPPED" --max-time 35

# 8. 监控进度
while (-not (Get-Content "$OUT\ar.log" | Select-String "Finished post-export")) { Start-Sleep 15; Get-Content "$OUT\ar.log" | Select-Object -Last 2 }

# 9. UnityPy 批量导出美术
python "export_art.py"  # 使用第五节脚本，修改路径后运行
```

### Mono 游戏快速反编译

```
1. 拖 Assembly-CSharp.dll 到 dnSpy
2. Ctrl+F 搜索类名
3. 重点看 Awake/Start/Update/FixedUpdate
4. 右键 "Analyze" 追踪方法调用链
5. 用 "Search Assemblies"（Ctrl+Shift+F）全局搜索字段名
```

---

## 十七、RenderDoc + Python API 帧分析工作流

> **适用场景**：当 AssetRipper 无法提取 Shader 逻辑（IL2CPP），但你需要了解游戏的**实际渲染管线**、**Shader 参数**和**运行时纹理**时，使用 RenderDoc 帧捕获 + Python API 进行精确分析。
>
> **实战来源**：对 Galactic Glitch 飞船 Boost Trail 特效进行的 4 个 RDC 帧捕获分析（1.rdc / 3.rdc / 4.rdc / 5.rdc），完整还原了 7 层渲染架构。

### 17.1 工具与环境

| 工具 | 用途 | 路径 |
|------|------|------|
| **RenderDoc** | 帧捕获 + Python Shell 宿主 | 官网下载，安装后自带 Python 环境 |
| **提取脚本** | 自动化 Draw Call 分析 + 纹理提取 | `F:\UnityProjects\Project-Ark\Tools\renderdoc_extract_targeted.py` |

**RenderDoc Python Shell 的特殊性**：
- RenderDoc 内置 Python 解释器，`import renderdoc as rd` 只在 RenderDoc 的 Python Shell 中有效
- **不能**在系统 Python 中运行，必须通过 RenderDoc 菜单 → **Window → Python Shell** 执行
- 执行方式：`exec(open(r"脚本路径", encoding="utf-8").read())`

### 17.2 完整工作流（两阶段）

```
Phase 1: LIST MODE（扫描阶段）
  目标：快速了解这个 RDC 有哪些 Draw Call，各自有多少纹理/CBuffer
  操作：设置 TARGET_EVENT_IDS = []，运行脚本
  输出：all_drawcalls.txt（每行一个 Draw Call 的摘要）

Phase 2: EXTRACT MODE（提取阶段）
  目标：对选定的目标 EID 提取纹理 + Shader 反汇编 + CBuffer 值
  操作：分析 all_drawcalls.txt，设置 TARGET_EVENT_IDS = [eid1, eid2, ...]，再次运行
  输出：每个 EID 一个子目录，含 report.txt + tex_slotN.png + ps_disasm.txt
```

### 17.3 完整提取脚本

脚本路径：`F:\UnityProjects\Project-Ark\Tools\renderdoc_extract_targeted.py`

```python
"""
RenderDoc Targeted Shader Extractor v4
- LIST MODE: TARGET_EVENT_IDS = [] → 扫描所有 Draw Call，输出 all_drawcalls.txt
- EXTRACT MODE: TARGET_EVENT_IDS = [eid1, eid2, ...] → 提取指定 EID 的纹理+Shader
"""
import renderdoc as rd
import os

# ===== 配置区域（每次分析新 RDC 时修改这里）=====
RDC_FILE   = r"F:\UnityProjects\ReferenceAssets\GGrenderdoc\1.rdc"
OUTPUT_DIR = r"F:\UnityProjects\ReferenceAssets\GGrenderdoc\output\targeted_v1"

# 留空 [] → LIST MODE（扫描所有 Draw Call）
# 填入 EID → EXTRACT MODE（提取指定 Draw Call）
TARGET_EVENT_IDS = [
    878,   # Ship body (3tex+2cb)
    1596,  # Trail main effect (4tex+1cb)
]
# ===== 配置区域结束 =====

os.makedirs(OUTPUT_DIR, exist_ok=True)

def flatten_actions(actions):
    result = []
    for a in actions:
        result.append(a)
        if hasattr(a, 'children') and a.children:
            result.extend(flatten_actions(a.children))
    return result

def save_texture(controller, res_id, slot_name, out_dir):
    try:
        texsave = rd.TextureSave()
        texsave.resourceId = res_id
        texsave.destType = rd.FileType.PNG
        texsave.mip = 0
        texsave.slice.sliceIndex = 0
        out_path = os.path.join(out_dir, f"{slot_name}.png")
        controller.SaveTexture(texsave, out_path)
        if os.path.exists(out_path):
            return f"OK ({os.path.getsize(out_path)} bytes)"
        return "(file not created)"
    except Exception as e:
        return f"(save failed: {e})"

def get_bound_textures(controller, state, lines):
    """获取当前 Draw Call 绑定的 PS 纹理列表。"""
    bound = []
    try:
        descriptors = state.GetReadOnlyResources(rd.ShaderStage.Pixel)
        for i, used in enumerate(descriptors):
            res_id = None
            if hasattr(used, 'descriptor'):
                d = used.descriptor
                if hasattr(d, 'resourceId'):   res_id = d.resourceId
                elif hasattr(d, 'resource'):   res_id = d.resource
            slot_idx = used.access.index if hasattr(used, 'access') and hasattr(used.access, 'index') else i
            if res_id and res_id != rd.ResourceId.Null():
                bound.append((f"tex_slot{slot_idx}", res_id))
        if bound:
            return bound
    except Exception as e:
        lines.append(f"  [GetReadOnlyResources] failed: {e}\n")
    return bound

def read_cbuffer_values(controller, state, lines):
    """读取 CBuffer 的运行时值。"""
    try:
        refl = state.GetShaderReflection(rd.ShaderStage.Pixel)
        if not refl or not refl.constantBlocks:
            lines.append("  (no constant blocks)\n")
            return
        pipe_obj  = state.GetGraphicsPipelineObject()
        shader_id = state.GetShader(rd.ShaderStage.Pixel)
        for cb_idx, cb in enumerate(refl.constantBlocks):
            lines.append(f"  CB[{cb_idx}] '{cb.name}' ({len(cb.variables)} vars):\n")
            try:
                cb_res = state.GetConstantBuffer(rd.ShaderStage.Pixel, cb_idx, 0)
                buf_id = cb_res.resourceId if hasattr(cb_res, 'resourceId') else rd.ResourceId.Null()
                vars_data = controller.GetCBufferVariableContents(
                    pipe_obj, shader_id, rd.ShaderStage.Pixel,
                    refl.entryPoint, cb_idx, buf_id, 0, 0)
                for v in vars_data[:20]:
                    lines.append(f"    {v.name} = {getattr(v, 'value', None)}\n")
            except Exception as e1:
                for v in cb.variables:
                    lines.append(f"    {v.name}: {v.type.name} @offset={v.byteOffset}\n")
    except Exception as e:
        lines.append(f"  [CBuffer] failed: {e}\n")

def extract_draw(controller, draw, out_dir):
    eid = draw.eventId
    draw_dir = os.path.join(out_dir, f"eid_{eid}")
    os.makedirs(draw_dir, exist_ok=True)
    lines = [f"=== EventId {eid} ===\n"]
    try:
        controller.SetFrameEvent(eid, True)
        state = controller.GetPipelineState()
        ps_id = state.GetShader(rd.ShaderStage.Pixel)
        if ps_id == rd.ResourceId.Null():
            lines.append("[PS] No pixel shader\n")
        else:
            refl = state.GetShaderReflection(rd.ShaderStage.Pixel)
            if refl:
                lines.append(f"[PS] Entry: {refl.entryPoint}\n")
                for cb in refl.constantBlocks:
                    lines.append(f"  CB '{cb.name}' bind={cb.fixedBindNumber} ({len(cb.variables)} vars)\n")
                for r in refl.readOnlyResources:
                    lines.append(f"  Tex '{r.name}' bind={r.fixedBindNumber}\n")
            try:
                disasm = controller.DisassembleShader(ps_id, refl, "")
                if disasm:
                    with open(os.path.join(draw_dir, "ps_disasm.txt"), "w", encoding="utf-8") as f:
                        f.write(disasm)
                    lines.append(f"[PS] Disasm saved ({len(disasm)} chars)\n")
            except Exception as e:
                lines.append(f"[PS] Disasm error: {e}\n")
        lines.append("\n[Textures]\n")
        for slot_name, res_id in get_bound_textures(controller, state, lines):
            result = save_texture(controller, res_id, slot_name, draw_dir)
            lines.append(f"  SAVED {slot_name}: {result}\n")
        lines.append("\n[CBuffer Values]\n")
        read_cbuffer_values(controller, state, lines)
    except Exception as e:
        import traceback
        lines.append(f"[FATAL] {e}\n{traceback.format_exc()}")
    with open(os.path.join(draw_dir, "report.txt"), "w", encoding="utf-8") as f:
        f.writelines(lines)
    return lines

def main():
    print(f"Opening: {RDC_FILE}")
    cap = rd.OpenCaptureFile()
    if cap.OpenFile(RDC_FILE, "", None) != rd.ResultCode.Succeeded:
        print("[ERROR] Open failed"); return

    result, controller = cap.OpenCapture(rd.ReplayOptions(), None)
    if result != rd.ResultCode.Succeeded:
        print("[ERROR] Replay failed"); cap.Shutdown(); return

    root = controller.GetRootActions() if hasattr(controller, 'GetRootActions') else controller.GetDrawcalls()
    all_actions = flatten_actions(root)
    eid_map = {a.eventId: a for a in all_actions}
    print(f"[INFO] Total actions: {len(all_actions)}")

    target_ids = list(TARGET_EVENT_IDS)

    # ---- LIST MODE ----
    if not target_ids:
        print("[LIST MODE] Scanning all draw calls with PS...")
        list_lines = []
        for a in all_actions:
            if not (a.flags & rd.ActionFlags.Drawcall):
                continue
            try:
                controller.SetFrameEvent(a.eventId, True)
                state = controller.GetPipelineState()
                if state.GetShader(rd.ShaderStage.Pixel) == rd.ResourceId.Null():
                    continue
                refl = state.GetShaderReflection(rd.ShaderStage.Pixel)
                tex_count = len(refl.readOnlyResources) if refl else 0
                cb_count  = len(refl.constantBlocks)    if refl else 0
                entry     = refl.entryPoint              if refl else "?"
                line = f"eid={a.eventId:5d}  textures={tex_count}  cbuffers={cb_count}  entry={entry}"
                print(line)
                list_lines.append(line + "\n")
            except Exception as e:
                list_lines.append(f"eid={a.eventId:5d}  ERROR: {e}\n")
        list_path = os.path.join(OUTPUT_DIR, "all_drawcalls.txt")
        os.makedirs(OUTPUT_DIR, exist_ok=True)
        with open(list_path, "w", encoding="utf-8") as f:
            f.writelines(list_lines)
        print(f"\n[DONE] List saved to: {list_path}")
        print("Set TARGET_EVENT_IDS to the EIDs you want to extract, then re-run.")
        controller.Shutdown(); cap.Shutdown(); return

    # ---- EXTRACT MODE ----
    for eid in target_ids:
        if eid in eid_map:
            print(f"  Extracting eid={eid} ...")
            lines = extract_draw(controller, eid_map[eid], OUTPUT_DIR)
            for l in lines[:8]: print("   ", l.rstrip())
        else:
            print(f"  [SKIP] eid={eid} not found")

    print(f"\n[DONE] Output: {OUTPUT_DIR}")
    controller.Shutdown()
    cap.Shutdown()

main()
```

### 17.4 操作步骤（完整流程）

#### Step 1：录制帧捕获

1. 打开 RenderDoc，点击 **Launch Application**
2. 填入游戏 `.exe` 路径，点击 **Launch**
3. 游戏运行后，在你想分析的**特效出现时**按 `F12`（或 Print Screen）截帧
4. 回到 RenderDoc，双击捕获的帧缩略图打开

#### Step 2：LIST MODE — 扫描所有 Draw Call

1. 修改脚本顶部配置：
   ```python
   RDC_FILE   = r"F:\...\your_capture.rdc"
   OUTPUT_DIR = r"F:\...\output\targeted_v1"
   TARGET_EVENT_IDS = []   # 空列表 = LIST MODE
   ```
2. 在 RenderDoc 菜单 → **Window → Python Shell**
3. 执行：
   ```python
   exec(open(r"F:\UnityProjects\Project-Ark\Tools\renderdoc_extract_targeted.py", encoding="utf-8").read())
   ```
4. 等待扫描完成，查看输出的 `all_drawcalls.txt`

#### Step 3：分析 all_drawcalls.txt，选择目标 EID

```
eid=   21  textures=6  cbuffers=1  entry=main   ← 6张纹理，值得关注
eid=  869  textures=3  cbuffers=2  entry=main   ← 3tex+2cb，飞船本体候选
eid= 1050  textures=4  cbuffers=3  entry=main   ← 4tex+3cb，最复杂！
eid= 1571  textures=4  cbuffers=1  entry=main   ← Trail 主特效候选
```

**选择策略**：
- `textures` 数量异常多（≥4）→ 高优先级
- `cbuffers` 数量多（≥2）→ 复杂 Shader，值得分析
- 连续出现相同特征的 EID（如 869/870/871...）→ 粒子系统批次
- 帧开头的 EID（如 21）→ 背景/环境层
- 帧末尾的 EID → 后处理/UI 层

#### Step 4：EXTRACT MODE — 提取目标 EID

1. 修改脚本：
   ```python
   TARGET_EVENT_IDS = [
       21,    # 6tex - background layers
       869,   # 3tex+2cb - ship body candidate
       1050,  # 4tex+3cb - complex shader
       1571,  # 4tex - Trail main effect candidate
   ]
   ```
2. 再次在 Python Shell 执行脚本
3. 查看输出目录，每个 EID 一个子目录：
   ```
   output/targeted_v1/
     eid_21/
       report.txt        ← Shader 信息 + 纹理大小
       tex_slot0.png     ← 第一张绑定纹理
       tex_slot1.png     ← 第二张绑定纹理
       ps_disasm.txt     ← Pixel Shader 反汇编（SPIR-V）
     eid_869/
       ...
   ```

#### Step 5：分析 report.txt

```
=== EventId 869 ===
[PS] Entry: main
  CB 'uniforms43' bind=2 (1 vars)
  CB 'uniforms56' bind=3 (18 vars)
  Tex 'res84' bind=5
  Tex 'res135' bind=3
  Tex 'res156' bind=4
[PS] Disasm saved (45230 chars)

[Textures]
  SAVED tex_slot5: OK (677888 bytes)   ← 662 KB
  SAVED tex_slot3: OK (93184 bytes)    ← 91 KB
  SAVED tex_slot4: OK (677888 bytes)   ← 662 KB

[CBuffer Values]
  CB[0] 'uniforms43' (1 vars):
    _child0 = [0.5, 0.5, 0.5, 1.0]
  CB[1] 'uniforms56' (18 vars):
    _child0 = [1.0, 0.0, 0.0, 0.0]
    ...
```

**关键信息解读**：
- `CB 'uniforms43' (1 vars)` → Shader 名 + 参数数量
- `tex_slot5: OK (677888 bytes)` → 纹理大小（662KB）
- 纹理大小相同 → 可能是同一张纹理的不同用途（Solid/Highlight）
- 纹理大小极小（77B）→ 1×1 颜色纹理（占位符）

### 17.5 跨 RDC 交叉验证方法

分析多个 RDC 时，通过以下特征进行跨帧验证：

| 验证维度 | 方法 | 意义 |
|---------|------|------|
| **Shader 名称** | 比较 `CB 'uniforms43'` 等名称 | 相同 Shader = 相同特效 |
| **纹理大小** | 比较 `tex_slotN` 的字节数 | 相同大小 = 相同纹理资产 |
| **CBuffer 参数数量** | 比较 `(N vars)` | 相同数量 = 相同 Shader 变体 |
| **Disasm 字符数** | 比较 `Disasm saved (N chars)` | 完全相同 = 完全相同的 Shader 代码 |
| **EID 位置** | 比较 EID 在帧中的相对位置 | 相同位置 = 相同渲染阶段 |

**实战案例**（GG 飞船 Boost Trail，4 个 RDC 交叉验证）：

| 特效 | 1.rdc | 3.rdc | 4.rdc | 5.rdc | 验证结论 |
|------|-------|-------|-------|-------|---------|
| 飞船本体 | eid_878 | eid_877 | eid_869 | eid_877 | ✅ Shader 名/纹理大小完全一致 |
| Trail 主特效 | eid_1596 | eid_1598 | eid_1571 | eid_1725 | ✅ 4.5MB 主纹理四 RDC 一致 |
| Boost 噪声 | — | eid_1076 | eid_1050 | eid_1181 | ✅ 3个CBuffer结构完全一致 |
| 背景视差层 | — | eid_21 | eid_21 | eid_21 | ✅ EID 完全相同！ |

### 17.6 ps_disasm.txt 分析技巧

SPIR-V 反汇编（`ps_disasm.txt`）可以揭示 Shader 的核心逻辑，即使没有源码：

```glsl
// 关键模式识别：

// 1. Perlin/Simplex 噪声（经典 289.0 常数）
OpFMul %float %uv %float_289   → 噪声生成

// 2. smoothstep 函数
// t*t*(-2*t+3) 的展开形式
OpFMul %t %t %neg2t_plus3      → smoothstep(0,1,t)

// 3. 多层纹理混合
OpImageSampleImplicitLod %tex0 %uv0
OpImageSampleImplicitLod %tex1 %uv1
OpFMix %result %tex0 %tex1 %alpha  → lerp(tex0, tex1, alpha)

// 4. 顶点颜色驱动
OpLoad %vertex_color %vs_COLOR     → 顶点颜色输入
OpCompositeExtract %w %vertex_color 3  → 取 w 分量

// 5. 世界空间坐标（说明特效跟随世界位置）
OpLoad %world_pos %vs_TEXCOORD0    → 世界空间坐标输入
OpMatrixTimesVector %transformed %matrix %world_pos  → 矩阵变换
```

**bound 值的意义**：
- `bound=100~300`：简单 Shader（颜色混合/UV 变换）
- `bound=300~700`：中等复杂度（多层纹理 + 噪声）
- `bound=700~2000`：复杂 Shader（程序化噪声 + 世界空间变换）

### 17.7 常见陷阱与解决方案

| 陷阱 | 原因 | 解决方案 |
|------|------|---------|
| `import renderdoc` 失败 | 在系统 Python 中运行 | 必须在 RenderDoc 的 Python Shell 中执行 |
| 纹理全部为 77B（极小） | RT 绑定 Pass，纹理未实际写入 | 跳过这个 EID，它是渲染目标绑定操作 |
| `GetReadOnlyResources` 返回空 | API 版本差异 | 检查 `descriptor.resourceId` vs `descriptor.resource` |
| 纹理保存失败 | 纹理是 Render Target（非普通纹理） | 正常现象，RT 无法直接保存为 PNG |
| 同一特效在不同 RDC 中纹理大小不同 | 动态纹理切换（Sprite Sheet 帧） | 这是正常的，说明该特效有动画帧 |
| `DisassembleShader` 返回空 | Shader 被混淆或格式不支持 | 跳过，只分析纹理和 CBuffer |
| 脚本运行很慢（LIST MODE） | 每个 Draw Call 都要 SetFrameEvent | 正常，等待即可（通常 1-3 分钟） |

### 17.8 输出目录结构

```
output/
└── targeted_v1/                    ← OUTPUT_DIR
    ├── all_drawcalls.txt           ← LIST MODE 输出（所有 Draw Call 摘要）
    ├── eid_21/                     ← 每个目标 EID 一个子目录
    │   ├── report.txt              ← Shader 信息 + 纹理大小 + CBuffer 值
    │   ├── tex_slot0.png           ← 第一张绑定纹理（按 binding 槽位命名）
    │   ├── tex_slot1.png
    │   └── ps_disasm.txt           ← Pixel Shader SPIR-V 反汇编
    ├── eid_869/
    │   ├── report.txt
    │   ├── tex_slot3.png           ← 注意：槽位编号来自 fixedBindNumber
    │   ├── tex_slot4.png
    │   ├── tex_slot5.png
    │   └── ps_disasm.txt
    └── eid_1571/
        └── ...
```

### 17.9 与其他工具的配合

| 场景 | 推荐工具组合 |
|------|------------|
| 了解游戏有哪些系统 | Il2CppDumper → dump.cs 搜索类名 |
| 提取 ScriptableObject 数值 | AssetRipper → MonoBehaviour YAML |
| 提取贴图/音频 | UnityPy 批量导出 |
| **分析运行时 Shader 和纹理** | **RenderDoc + Python API（本章）** |
| 验证配置值是否与运行时一致 | Cheat Engine 内存扫描 |

**典型组合流程**（IL2CPP 游戏完整分析）：
```
1. Il2CppDumper → 了解类结构和字段名
2. AssetRipper  → 提取 SO 数值和 Prefab 结构
3. UnityPy      → 批量导出贴图/音频
4. RenderDoc    → 分析运行时渲染管线（本章）
5. Cheat Engine → 验证运行时参数值
```

---

*文档最后更新：2026-03-09*
*基于实战经验：Galactic Glitch (IL2CPP/2021) + Backpack Monsters (Mono/2022) + Magicraft (IL2CPP/Unity 6) + Minishoot' Adventures (Mono/2021) + Rain World (Mono/2021)*
*第十七章新增：RenderDoc + Python API 帧分析工作流（基于 GG Boost Trail 4 RDC 实战）*
