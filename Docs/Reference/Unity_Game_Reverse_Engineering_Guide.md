# Unity 游戏解包逆向工程指南

> 本文基于对 **Galactic Glitch**（IL2CPP, Unity 2021）和 **Backpack Monsters**（Mono）的实战解包经验总结，涵盖工具选型、流程、踩坑记录、以及完整复刻一个游戏的全套方案。

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
```

| 特征 | Mono | IL2CPP |
|------|------|--------|
| 代码可读性 | dnSpy/ILSpy 直接反编译，可读性接近源码 | 需要 Il2CppDumper 提取签名，逻辑在 GameAssembly.dll 里（native code） |
| 资产提取 | UnityPy / AssetStudio 均可，TypeTree 完整 | UnityPy 读 MonoBehaviour 会失败；AssetRipper 最可靠 |
| 难度 | ★★☆☆☆ | ★★★★☆ |
| 代表游戏 | Backpack Monsters、Hollow Knight（旧版） | Galactic Glitch、大多数商业发行游戏 |

---

## 二、工具箱总览

### 代码层工具

| 工具 | 用途 | 支持 |
|------|------|------|
| **dnSpy** | Mono DLL 反编译，可读性极高，支持断点调试 | Mono |
| **ILSpy** | 轻量 Mono 反编译，支持搜索 | Mono |
| **Il2CppDumper** | 从 `global-metadata.dat` + `GameAssembly.dll` 提取所有类/字段/方法签名（`dump.cs` + `script.json`） | IL2CPP |
| **Ghidra / IDA Pro** | 逆向 GameAssembly.dll native 函数体（极难，通常不必要） | IL2CPP |

### 资产层工具

| 工具 | 用途 | 支持 | 推荐度 |
|------|------|------|--------|
| **AssetStudio** | GUI 资产浏览器，查看/导出贴图、音频、Mesh | Mono ✓ / IL2CPP ⚠️（MonoBehaviour 字段为空） | ★★★★☆ |
| **AssetRipper** | 完整导出为 Unity 项目（YAML Prefab + SO + 场景），IL2CPP 下最可靠 | Mono ✓ / IL2CPP ✓ | ★★★★★ |
| **UnityPy** | Python 脚本化资产读写，适合批量处理 | Mono ✓ / IL2CPP ✗（TypeTree 缺失） | ★★★☆☆ |
| **UABE (Unity Asset Bundle Extractor)** | 老工具，修改资产包用 | Mono ✓ | ★★☆☆☆ |

### 运行时工具

| 工具 | 用途 |
|------|------|
| **Cheat Engine** | 内存扫描，运行时抓取浮点数值（速度、伤害等） |
| **x64dbg / OllyDbg** | 断点调试 native 代码 |
| **dnSpy（Play Mode Attach）** | Mono 游戏运行时直接断点 |

---

## 三、Galactic Glitch 实战复盘（IL2CPP）

### 目标
提取飞船物理参数（linearDrag、moveAcceleration、maxMoveSpeed、angularAcceleration 等）用于 Project Ark 的手感复刻。

### 我尝试的 4 种方案

#### 方案 A：UnityPy 直接读取 ✗

```python
import unitypy
env = unitypy.load("GalacticGlitch_Data/data.unity3d")
for obj in env.objects:
    if obj.type.name == "MonoBehaviour":
        data = obj.read()
        print(data.to_dict())  # AttributeError: 'GameObject' has no attribute 'to_dict'
```

**失败原因**：IL2CPP 编译时会剥离 TypeTree（运行时类型元数据），`raw_data` 为空 bytes，UnityPy 无法反序列化字段。这不是 UnityPy 的 bug，是 IL2CPP 的设计——它把类型信息编译进 native 代码而非资产文件。

**教训**：IL2CPP 下 UnityPy 只能读 Texture2D、AudioClip 等内置类型，**对 MonoBehaviour 字段完全无效**。

---

#### 方案 B：UnityPy + DummyDll TypeTree 注入 ✗

原理：Il2CppDumper 生成 DummyDll（含字段布局），用 TypeTreeGeneratorAPI 把它转成 UnityPy 可用的 TypeTree，再注入到 UnityPy 解析流程。

**失败原因**：TypeTreeGeneratorAPI 依赖链复杂（需要特定版本的 Il2CppDumper 输出格式），工具本身尚不成熟，字段偏移量对不上导致解析全部乱码。

**判断**：这个路子理论可行，但工程量远超直接用 AssetRipper，性价比极低。

---

#### 方案 C：特征扫描 Raw Bytes ✗

```python
# 尝试在 raw_data 里找 float 值（如 7.5 → IEEE 754 = 0x40F00000）
import struct
target = struct.pack('<f', 7.5)
# raw_data 为空，无法扫描
```

**失败原因**：同方案 A，IL2CPP 下 raw_data 就是空的，根本没有字节可以扫描。

---

#### 方案 D：AssetRipper headless + REST API ✓

```
1. 下载 AssetRipper CLI 版本（带 --launch-web 参数）
2. 启动：AssetRipper.exe --launch-web
3. POST http://localhost:8080/LoadFolder  body: path=E:\SteamLibrary\...\GalacticGlitch_Data
4. POST http://localhost:8080/Export/UnityProject  body: outputPath=C:\Temp\GG_Ripped
5. 等待导出完成（~2-5分钟，613MB 的包）
6. 打开 C:\Temp\GG_Ripped\ExportedProject\Assets\GameObject\Player.prefab
```

**成功原因**：AssetRipper 实现了自己的 IL2CPP TypeTree 重建逻辑（基于 Unity 版本 + 类型推断），能够正确反序列化 MonoBehaviour 字段为 YAML。

**关键收获**：`Player.prefab` 里有完整的状态机组件，直接读到了所有状态下的物理参数。

---

### 最大的意外发现

解包之前，我以为 GG 的物理参数是"一套静态数值"（类似 ShipStatsSO 里的一组字段）。

**实际上是状态机驱动的动态切换**：

```
正常飞行(IsBlueState)  →  linearDrag=3,  maxSpeed=7.5, angularAccel=80
Boost状态(IsBoostState) →  linearDrag=2.5, maxSpeed=9,   angularAccel=40
Dodge状态(IsDodgeState) →  linearDrag=1.7, maxSpeed=4,   angularAccel=20
主攻击(IsMainAttackState) → linearDrag=3,  maxSpeed=7.5, angularAccel=720 ← 瞄准锁定感来自这里！
```

这解释了为什么 GG 射击时转向极其灵敏（720 deg/s²）但平时转向有明显惯性感（80 deg/s²）——**不是同一套参数**，是状态机在实时切换。

如果只看静态参数的话，永远不会明白这个手感是怎么来的。

---

### 踩坑清单

| 坑 | 根因 | 正确做法 |
|----|------|----------|
| UnityPy 读 IL2CPP MonoBehaviour 报错 | TypeTree 缺失 | 换 AssetRipper |
| AssetRipper API `/api/LoadFolder` 404 | 路径错误 | 正确路径是 `/LoadFolder`（无 `/api/` 前缀） |
| AssetRipper POST 返回 415 | Content-Type 错误 | 改为 `application/x-www-form-urlencoded` |
| 导出超时 | 613MB 包，30s 超时太短 | 后台异步等待 5 分钟，轮询 `/ExportStatus` |
| `BoosterBurnoutPower` 误判为移动 Boost | 名字误导 | 看实际字段：它是战斗 Power，有 `damagePerSecond`，不是推力 |
| 以为参数是静态的 | 没有先 dump.cs 理解架构 | 先 Il2CppDumper 看类结构，再 AssetRipper 看实际值 |

---

## 四、Mono 游戏解包（Backpack Monsters 复盘）

Mono 游戏流程简单得多：

```
1. 用 dnSpy 打开 GameName_Data/Managed/Assembly-CSharp.dll
2. 搜索关键类名（InventoryController、GridItem 等）
3. 直接看 C# 源码（反编译质量接近原始代码）
4. 用 AssetStudio 查看资产，或直接搜 .asset/.prefab 文件
```

**Backpack Monsters 的实现要点（已分析）**：
- 背包格子：`InventoryGrid` 维护二维 bool 数组，每个格子存 `itemId`
- 物品形状：`ItemShapeSO` 存多边形偏移列表，支持旋转
- 拖拽逻辑：`OnPointerDrag` 实时计算锚点格子 → `CanPlace()` 碰撞检测 → `CommitPlace()`
- 与 Project Ark 最相关的开源实现：`DavidSouzaLD/InventoryTetris-Unity`

---

## 五、想了解一个游戏的所有系统——完整解包流程

### Phase 1：情报侦察（30分钟）

```
目标：弄清楚这个游戏用了什么，有哪些系统
```

1. **判断 Mono / IL2CPP**（见第一节）
2. **Il2CppDumper**（IL2CPP）或 **dnSpy**（Mono）提取类列表
3. 搜索关键词，快速建立系统地图：

```
搜索词示例：
  Player, Character, Ship          → 核心实体
  Inventory, Item, Bag             → 背包系统
  Combat, Damage, Health, Hit      → 战斗系统
  State, Brain, FSM, Behavior      → AI系统
  Manager, Controller, System      → 管理器架构
  ScriptableObject, Data, Config   → 数据层
  Pool, Spawn, Factory             → 对象池
  Save, Checkpoint, Progress       → 存档系统
  UI, HUD, Panel, Screen           → UI架构
```

4. 用 `dump.cs` 的类列表绘制一张**系统依赖图**（手绘或 Mermaid），标出：
   - 核心实体（Player/Enemy）
   - 数据层（ScriptableObject）
   - 系统层（各 Manager/Controller）
   - 事件总线（有没有 EventBus / MessageBroker 类）

### Phase 2：数据提取（1-2小时）

```
目标：把所有 ScriptableObject、Prefab 序列化值读出来
```

**推荐工具**：AssetRipper（全量导出为 Unity 项目）

```bash
# 全量导出
AssetRipper.exe --launch-web
# POST /LoadFolder  path=<游戏Data目录>
# POST /Export/UnityProject  outputPath=<输出目录>
```

**导出后重点看**：

```
ExportedProject/
├── Assets/
│   ├── GameObject/          ← 所有 Prefab（Player.prefab, Enemy*.prefab 等）
│   ├── MonoBehaviour/       ← 所有 ScriptableObject 的序列化值（.asset）
│   ├── AnimatorController/  ← 动画状态机（参数名、Transition 条件）
│   ├── AudioClip/           ← 音频文件
│   └── Texture2D/           ← 贴图
```

**高效检索技巧**：
```bash
# 找到所有包含速度参数的 MonoBehaviour
grep -r "speed" ExportedProject/Assets/MonoBehaviour/ --include="*.asset" -l

# 找到 Player Prefab 里的特定组件
grep -A 20 "GGSteering" ExportedProject/Assets/GameObject/Player.prefab
```

### Phase 3：代码逻辑理解（2-8小时，取决于目标深度）

**IL2CPP 路线**（只能读结构，不能读逻辑体）：
- `dump.cs` 看字段名 → 推断行为
- Animator 状态机 YAML → 理解状态转换条件
- Prefab YAML 里的 component 顺序和引用关系 → 理解组件架构

**Mono 路线**（可以完整读逻辑）：
- dnSpy 直接读方法体
- 重点读 `Update()`、`FixedUpdate()`、核心 `Execute()` / `Tick()` 方法
- 关注事件订阅（`AddListener`、`+=` 操作符附近）

### Phase 4：美术资产提取（如果需要复刻）

> ⚠️ 注意：提取并使用商业游戏资产用于自己的商业项目涉及版权问题。以下内容仅用于学习研究目的。

**贴图/精灵**：
```
AssetStudio → Filter by Type: Texture2D → Export Selected
```
导出格式：PNG。Sprite Atlas 需要额外拆分（AssetStudio 支持 Sprite 子资产导出）。

**音频**：
```
AssetStudio → Filter by Type: AudioClip → Export Selected
```
格式通常是 .wav 或 .ogg，可直接使用。

**动画**：
```
AssetRipper 导出的 AnimationClip (.anim) 可以直接放进 Unity 工程用
AnimatorController 也会被导出为可读的 YAML
```

**字体/着色器**：
- 着色器通常以编译后的 ShaderVariant 形式存在，**很难反向还原**为可编辑 .hlsl
- 字体 (TMP_FontAsset) 通常可以被 AssetRipper 完整导出

**Tilemap**：
```
AssetRipper 会导出 Tilemap 数据，包括 Tile 的世界坐标
配合导出的 Tile Sprite，可以在新项目里重建关卡布局
```

**Prefab 层级**（完整复刻用）：
```
AssetRipper 导出的 .prefab 是标准 Unity YAML
可以直接 Import 进你的 Unity 项目，只需要修复 Script 引用（fileID 会不匹配）
```

---

## 六、进阶技巧

### 技巧 1：用 Animator 反推游戏设计

AnimatorController YAML 里有：
- 所有状态名（直接对应游戏机制，比如 `Attack_Combo1`、`DodgeRoll`、`Stun`）
- Transition 条件参数名（比如 `isGrounded: bool`、`attackIndex: int`）
- Transition 的 `exitTime` 和 `duration`（攻击前摇/后摇时间）

这些信息价值**远超**看字段名，因为它直接告诉你：**设计师认为这个角色有哪些状态，状态之间怎么转换**。

### 技巧 2：用 AudioClip 名字推断游戏事件

音频文件的命名往往直接反映游戏事件：
```
sfx_player_dash_start.wav       → Dash 开始
sfx_player_dash_whoosh.wav      → Dash 飞行中
sfx_player_footstep_grass.wav   → 地面材质系统
sfx_enemy_alert.wav             → 敌人发现玩家
sfx_ui_button_hover.wav         → UI 悬浮音效
```

通过音频列表，可以在**不读任何代码的情况下**了解游戏有哪些系统、哪些交互节点。

### 技巧 3：用 ScriptableObject 数据逆向设计文档

`.asset` 文件里的序列化值本质上就是**策划配置表**。把所有同类 SO 的字段收集起来，就等于拿到了这个游戏的 Balance Sheet：

```bash
# 提取所有敌人的 HP 和攻击力
grep -A 5 "health\|damage\|speed" ExportedProject/Assets/MonoBehaviour/Enemy*.asset
```

### 技巧 4：对照 Physics2D 设置理解物理模型

从 `ProjectSettings/Physics2D.asset` 可以读到：
- `m_DefaultMaterial`（默认物理材质，摩擦力/弹力）
- `m_Gravity`（重力方向和大小）
- Layer Collision Matrix（碰撞矩阵，可以看出哪些 Layer 之间有交互）

### 技巧 5：Cheat Engine 验证提取值

当你从 Prefab 里读到 `maxSpeed = 7.5` 时，用 Cheat Engine 在运行中找到飞船速度的内存地址，验证实际运行值是否与配置值一致。

如果不一致，说明有**运行时修改**（比如状态机在切换参数，或者有 Modifier 系统在叠加百分比）。

---

## 七、如果我要完整复刻一个游戏——工作流

```
Week 1：情报阶段
├── Il2CppDumper / dnSpy → 类列表 → 系统地图
├── AssetRipper 全量导出
└── 分析核心循环：玩家 → 输入 → 物理 → 战斗 → 反馈

Week 2：框架搭建
├── 参考 Prefab 层级搭建 GameObject 结构
├── 参考 SO 数据创建等效 ScriptableObject
└── 实现核心移动（最先可感知的系统）

Week 3-4：系统复刻
├── 参考 Animator + dump.cs → 实现状态机
├── 参考 AudioClip 命名 → 实现音效触发点
├── 参考 Prefab 的 Component 列表 → 补充功能组件
└── 用 Cheat Engine 对比参数 → 调优

Week 5+：美术集成（如果用原始资产）
├── AssetStudio 批量导出贴图/音频
├── 重建 Sprite Atlas（Unity Sprite Packer）
├── 修复 Prefab 的 Script 引用（fileID 替换）
└── 逐场景验证视觉效果
```

---

## 八、法律与伦理边界

| 行为 | 合法性 |
|------|--------|
| 解包用于学习/研究 | 大多数国家允许，属于逆向工程合理使用 |
| 提取美术资产用于**自己的非商业项目** | 灰色地带，通常被容忍 |
| 提取美术资产用于**商业项目** | 侵权，严禁 |
| 复刻游戏机制（不用原始资产） | 合法（机制不受版权保护） |
| 直接二次分发原游戏资产 | 侵权 |

**Project Ark 的正确做法**：学习 GG 的机制和数值用于设计参考，所有最终呈现的美术/音频使用自制资产。

---

## 九、快速参考卡片

### IL2CPP 游戏完整解包命令序列

```bash
# Step 1: 提取类签名
Il2CppDumper.exe GameAssembly.dll global-metadata.dat output_dir/

# Step 2: 查看类结构
grep -A 30 "class GGSteering" output_dir/dump.cs

# Step 3: 启动 AssetRipper headless
AssetRipper.exe --launch-web

# Step 4: 加载游戏数据（PowerShell）
Invoke-WebRequest -Uri "http://localhost:8080/LoadFolder" `
  -Method POST `
  -ContentType "application/x-www-form-urlencoded" `
  -Body "path=E:\SteamLibrary\steamapps\common\GameName\GameName_Data"

# Step 5: 导出项目
Invoke-WebRequest -Uri "http://localhost:8080/Export/UnityProject" `
  -Method POST `
  -ContentType "application/x-www-form-urlencoded" `
  -Body "outputPath=C:\Temp\Ripped"

# Step 6: 等待并查找目标文件
Get-ChildItem "C:\Temp\Ripped\ExportedProject\Assets\GameObject\" -Filter "Player*"
```

### Mono 游戏快速反编译

```
1. 拖拽 Assembly-CSharp.dll 到 dnSpy
2. Ctrl+F 搜索类名
3. 重点看 Awake/Start/Update/FixedUpdate
4. 用 "Analyze" 功能追踪方法调用链
```

### UnityPy 适用场景（仅限 Mono 或内置类型）

```python
import unitypy

env = unitypy.load("data.unity3d")
for obj in env.objects:
    if obj.type.name == "Texture2D":
        tex = obj.read()
        tex.image.save(f"{tex.name}.png")
    elif obj.type.name == "AudioClip":
        clip = obj.read()
        # 导出音频
```

---

*文档创建时间：2026-03-05*
*基于 Galactic Glitch (IL2CPP) + Backpack Monsters (Mono) 实战经验*
