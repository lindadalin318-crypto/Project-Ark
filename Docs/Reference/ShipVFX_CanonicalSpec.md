# Ship VFX Canonical Spec

## 1. 文档目的

这份文档是当前 `Ship` / `BoostTrail` / `VFX` 规范化工作的**唯一现役规范入口**。

它负责定义：

- 当前有效的命名口径
- 目录归属与路径表达方式
- Runtime / Prefab / Editor / Scene 的职责边界
- `Live / Dormant / Reference / Legacy` 四类状态的判定标准
- MVP 阶段哪些名称允许只做 **canonical alias**，哪些可以物理改动

> 本文档**不负责**记录玩家主观体验，也**不负责**记录 Shader 实验过程。对应内容分别放在：
>
> - `Ship_VFX_Player_Perception_Reference.md`
> - `BoostTrail_Shader_Implementation_Status.md`
>
> 本文档优先级高于历史计划、逆向分析和导入脚本注释。

## 2. 适用范围

当前纳入规范化治理的范围：

- Runtime
  - `Assets/Scripts/Ship/VFX/`
  - `Assets/Scripts/Ship/Movement/ShipBoost.cs`
  - `Assets/Scripts/Ship/ShipStateController.cs`
  - `Assets/Scripts/Ship/Data/`
- Editor 工具
  - `Assets/Scripts/Ship/Editor/`
- Prefab / Scene
  - `Assets/_Prefabs/Ship/Ship.prefab`
  - `Assets/_Prefabs/VFX/BoostTrailRoot.prefab`
  - `Assets/Scenes/SampleScene.unity`
- Art / Shader / Material / Texture
  - `Assets/_Art/Ship/Glitch/`
  - `Assets/_Art/VFX/BoostTrail/`
- Docs
  - `Docs/Reference/`
  - `Docs/ImplementationLog/`
- Reference Import
  - `Tools/CopyGGTextures.ps1`

## 3. 当前现役链路

当前应视为**现役主链**的结构如下：

```text
Ship.prefab
└── ShipVisual
    ├── Ship_Sprite_Back
    ├── Ship_Sprite_Liquid
    ├── Ship_Sprite_HL
    ├── Ship_Sprite_Solid
    ├── Ship_Sprite_Core
    ├── Dodge_Sprite
    └── BoostTrailRoot (nested prefab)
        ├── MainTrail
        ├── FlameTrail_R
        ├── FlameTrail_B
        ├── FlameCore
        ├── EmberTrail
        ├── EmberSparks
        ├── BoostEnergyLayer2
        └── BoostEnergyLayer3
```

> **注意**：`BoostActivationHalo` 已于 2026-03-22 取消（合并入 Bloom Burst + FlameCore Burst），不再是现役节点。

Runtime 主驱动关系：

- `ShipView` (Coordinator)：轻量协调器，持有 5 层 sprite 引用（Back/Liquid/HL/Solid/Core），捕获基线颜色，订阅 `ShipStateController.OnStateChanged` + `ShipHealth.OnDamageTaken` + `ShipMotor.OnSpeedChanged` 事件，路由到对应 Worker。自身不包含任何 VFX 实现逻辑。
  - `ShipBoostVisuals` (Worker)：Boost 状态视觉——Liquid sprite swap（Normal↔Boost 贴图）+ sortOrder 提升（Solid 下方↔上方）+ HDR 颜色 tween、HL/Core alpha ramp、推进器脉冲、`BoostTrailView` 启停委托
  - `ShipHitVisuals` (Worker)：受击反馈——5 层同步白闪、受击后 i-frame 闪烁、Core 低血量警告脉冲
  - `ShipDashVisuals` (Worker)：冲刺反馈——Solid/HL/Core i-frame 闪烁、Dodge_Sprite 静态残影（分离→淡出→回挂）、`DashAfterImageSpawner` 启停委托（二级 Worker）
  - `ShipVisualJuice` (Worker)：船体 juice——移动倾斜（tilt）、急加速/减速形变（squash/stretch）；由 ShipView 注入组件引用，通过 `OnSpeedChanged()` / `OnDashStarted()` 被动接收信号
  - `DashAfterImageSpawner` (二级 Worker)：Dash 连续残影（对象池化）；由 `ShipDashVisuals` 通过 `TriggerSpawn()` 驱动，不自行订阅事件
- `ShipHealth`：纯游戏逻辑（HP/无敌帧计时/击退），不直接操作任何 SpriteRenderer。通过 `OnDamageTaken` 事件驱动 `ShipView` 执行受击视觉反馈。
- `BoostTrailView`：负责 Boost 尾迹表现栈（7 层）与 Bloom burst
- `ShipBoost` + `ShipStateController`：提供状态事件，不直接承担渲染职责

Scene-only 绑定：

- `SampleScene.unity` 中的 `BoostTrailBloomVolume`
- `BoostTrailView._boostBloomVolume`
- `Assets/Settings/BoostBloomVolumeProfile.asset`

### 3.1 Play Mode 调试入口

当前 `Boost` 特效分层调试统一走 `BoostTrailDebugManager`：

1. 进入 `Play Mode`
2. 优先选中场景里的现役实例 `Ship/ShipVisual/BoostTrailRoot`
   - 若当前选中的是 `Assets/_Prefabs/VFX/BoostTrailRoot.prefab` 资源也没关系：`BoostTrailDebugManagerEditor` 会在 Play Mode 下自动把 Inspector 代理到现役场景实例，并给出提示与 `Select Live Scene Instance` 快捷按钮
3. 在 Inspector 中打开 `Enable Inspector Debug`
4. 按需求选择：
   - `ObserveRuntime`：保留真实起手 / 持续 / 退场时序，只做图层遮罩
   - `ForceSustainPreview`：把持续层固定到指定 `Preview Intensity`，适合单独看 `FlameTrail` / `EmberTrail` / `EnergyLayer2 / Layer3`
5. 若只想看某一层，用 `Solo Layer`
6. 若想逐个触发 burst，用 Inspector 按钮预览 `FlameCore` / `EmberSparks` / `Bloom`

约束：

- 该组件**只用于 Play Mode 调试**，默认必须保持关闭
- 它可以遮罩或固定预览现役 `BoostTrailView` 链路，但**不替代正式状态驱动**
- 调试结束后应 `Reset Preview`，避免误判当前场景中的残留视觉状态

## 4. 状态分类

### 4.1 `Live`

满足以下任一条件即判定为 `Live`：

- 当前被 `Ship.prefab` / `BoostTrailRoot.prefab` / `SampleScene.unity` 直接引用
- 当前被现役 runtime 脚本直接驱动
- 当前被现役 Editor 工具作为输出目标维护

### 4.2 `Dormant`

满足以下条件即判定为 `Dormant`：

- 文件仍存在于仓库中
- 当前不在现役 runtime / prefab / scene 链路中
- 仍可能被历史文档、参考脚本或迁移说明提及

### 4.3 `Reference`

满足以下条件即判定为 `Reference`：

- 用于逆向分析、对照研究、上游资产映射
- 不应被当作当前命名权威或实现规范

### 4.4 `Legacy`

满足以下条件即判定为 `Legacy`：

- 已被新的主链取代
- 继续保留只为兼容说明、迁移过渡或历史查证

## 5. 命名规则

## 5.1 代码与类型

- 类名：`PascalCase`
- 私有字段：`_camelCase`
- Editor 工具后缀：`Builder` / `Rebuilder` / `Creator` / `Linker` / `Binder`
- Runtime 类型名优先表达**职责**，不重复堆叠域前缀
- 现阶段保留物理名 `ShipShipState`，但在规范语义上统一称为 **Ship State Enum**

## 5.2 Canonical Name 与 Physical Name

MVP 阶段统一采用**双层命名**：

- `Canonical Name`：规范语义名，用于文档、注册表、职责讨论和未来迁移
- `Physical Name`：当前仓库里真实存在的文件名 / 节点名 / 资源名

原则：

- 文档与注册表优先写 `Canonical Name`
- 必须同时保留 `Physical Name`，避免 Unity 序列化和名字搜索断链
- 若物理名存在历史缩写或编号，MVP 阶段先做 alias，不直接 rename

## 5.3 Prefab 节点命名口径

当前现役节点的 canonical 口径如下：

| Canonical Name | Physical Name | 说明 |
| --- | --- | --- |
| `ShipVisual` | `ShipVisual` | 飞船视觉根节点，`VisualChild` 仅作 legacy alias |
| `ShipBackSprite` | `Ship_Sprite_Back` | 船尾 / 推进器基底层 |
| `ShipLiquidSprite` | `Ship_Sprite_Liquid` | 能量液态层 |
| `ShipHighlightSprite` | `Ship_Sprite_HL` | 高亮层，物理名缩写保留 |
| `ShipSolidSprite` | `Ship_Sprite_Solid` | 船体实色层 |
| `ShipCoreSprite` | `Ship_Sprite_Core` | 核心 / 预留层 |
| `ShipDashGhostSprite` | `Dodge_Sprite` | Dash 静态 ghost |
| `BoostTrailRoot` | `BoostTrailRoot` | Boost 尾迹 nested prefab 根节点 |
| `RightFlameTrail` | `FlameTrail_R` | 右侧喷口持续火焰（Local-space，rateOverTime） |
| `LeftFlameTrail` | `FlameTrail_B` | 左侧喷口持续火焰（Local-space，rateOverTime）；`B` 不再解释为语义名 |

## 5.4 资产命名口径

当前存在大量上游编号式物理名。MVP 阶段统一解释为下表中的 canonical alias：

| Canonical Name | Physical Name | 当前用途 |
| --- | --- | --- |
| `ShipLiquidBoostSprite` | `Boost_16.png` | Boost 状态液态层贴图 |
| `ShipLiquidNormalSprite` | `Movement_3.png` | Normal 状态液态层贴图 |
| `ShipSolidNormalSprite` | `Movement_10.png` | 船体实色层贴图 |
| `ShipHighlightNormalSprite` | `Movement_21.png` | 船体高亮层贴图 |
| `ShipDashGhostSource` | `Reference/player_test_fire.png` | Dash ghost 贴图来源 |

## 6. 路径规则

## 6.1 Runtime / Editor

- Runtime：`Assets/Scripts/Ship/VFX/`
- 数据：`Assets/Scripts/Ship/Data/`
- Scene / Prefab 编辑器工具：`Assets/Scripts/Ship/Editor/`

## 6.2 Prefab / Scene

- 飞船主体 prefab：`Assets/_Prefabs/Ship/Ship.prefab`
- Boost 尾迹 prefab：`Assets/_Prefabs/VFX/BoostTrailRoot.prefab`
- 场景级引用验证：`Assets/Scenes/SampleScene.unity`

## 6.3 Art / Shader / Material / Texture

- 飞船贴图与共享 glow 材质：`Assets/_Art/Ship/Glitch/`
- BoostTrail 材质：`Assets/_Art/VFX/BoostTrail/Materials/`
- BoostTrail Shader：`Assets/_Art/VFX/BoostTrail/Shaders/`
- BoostTrail 贴图：`Assets/_Art/VFX/BoostTrail/Textures/`
- 上游参考贴图：`Assets/_Art/Ship/Glitch/Reference/`

## 6.4 Docs

- 模块规则入口：`Implement_rules.md`
- 规范权威：`Docs/Reference/ShipVFX_CanonicalSpec.md`
- 资产映射：`Docs/Reference/ShipVFX_AssetRegistry.md`
- 分批迁移：`Docs/Reference/ShipVFX_MigrationPlan.md`
- 玩家感知参考：`Docs/Reference/Ship_VFX_Player_Perception_Reference.md`
- 实现状态：`Docs/Reference/BoostTrail_Shader_Implementation_Status.md`

## 7. 工具职责边界

| 工具 | Canonical Role | 不负责的内容 |
| --- | --- | --- |
| `ShipPrefabRebuilder` | `Ship.prefab` 唯一权威；负责根节点物理组件（Rigidbody2D / CircleCollider2D）、全部运行时脚本组件（InputHandler / ShipMotor / ShipAiming / ShipStateController / ShipHealth / ShipDash / ShipBoost）、ShipStatsSO + InputActionAsset + DashAfterImage prefab 接线、多层 sprite 层级、`BoostTrailRoot` 嵌套集成、`ShipView` (Coordinator) + Workers + 二级 Worker 组件创建与序列化引用接线 | 不负责场景级 Bloom Volume；不负责 WeavingStateTransition 跨程序集场景接线 |
| `BoostTrailPrefabCreator` | `BoostTrailRoot.prefab` 结构权威；负责子节点、粒子、能量层与 `BoostTrailDebugManager` 的生成与接线 | 不负责 `Ship.prefab` 集成；不负责 scene-only 引用 |
| `BoostTrailDebugManager` | Play Mode 分层调试权威；负责 Boost stack 的 solo layer、遮罩隔离、sustain preview 与 burst preview | 不负责正式状态驱动；默认必须保持 dormant |
| `MaterialTextureLinker` | 现役材质与纹理精确路径回填；禁止 legacy 材质链回流 | 不负责 prefab / scene |
| `ShipBoostTrailSceneBinder` | Scene-only 绑定权威；负责 `BoostTrailBloomVolume` 与 `BoostTrailView._boostBloomVolume` | 不负责 prefab 结构 |
| `CopyGGTextures.ps1` | 参考资源导入工具；只复制当前仍保留的上游纹理，不再导入已清退的 dormant 资源 | 不定义现役命名；不定义主链资产状态 |

## 8. MVP 冻结项

以下对象在 MVP 第一批中**只允许做 canonical alias，不做物理 rename / move**：

- `Assets/_Prefabs/Ship/Ship.prefab`
- `Assets/_Prefabs/VFX/BoostTrailRoot.prefab`
- `Assets/Scenes/SampleScene.unity`
- `Boost_16.png`
- `Movement_3.png`
- `Movement_10.png`
- `Movement_21.png`
- `Reference/player_test_fire.png`
- `ShipShipState.cs`
- `FlameTrail_B`
- `Ship_Sprite_HL`

冻结原因：

- Unity 序列化引用已存在
- 部分 Editor 工具仍依赖物理名或确切路径
- 需要先完成注册表和职责收口，才能安全迁移

## 8.1 已审冻结高歧义项

| Physical Name | 当前结论 | 约束来源 | 最小安全动作 |
| --- | --- | --- | --- |
| `FlameTrail_B` | 需 Editor 配合 | `BoostTrailView._flameTrailB` 序列化引用 + `BoostTrailPrefabCreator` 节点名硬编码 | 先改 `BoostTrailPrefabCreator`，再在 Unity Editor 中改 `BoostTrailRoot.prefab` 节点名并复验 `Ship.prefab` / `SampleScene` |
| `Ship_Sprite_HL` | 需 Editor 配合 | `ShipView._hlRenderer` 序列化引用 + `ShipPrefabRebuilder` 节点名硬编码 | 先改 `ShipPrefabRebuilder`，再在 Unity Editor 中改 `Ship.prefab` 节点名并复验 |
| `ShipShipState.cs` | 不可改（当前批） | Runtime 数据类型已扩散到脚本 API 与状态流 | 继续只使用 canonical alias `ShipStateEnum`，暂不做物理 rename |

补充说明：

- `Ship_Sprite_HL` 当前 live prefab 使用的是共享默认材质链；节点改名不应和 `ShipGlowMaterial.mat` 或 `Movement_21.png` 的迁移绑成同一批动作。
- `FlameTrail_B` 与 `Ship_Sprite_HL` 当前都更适合继续用 canonical alias 对外沟通，而不是直接动 prefab 物理名。

## 9. 校验规则

每次改动后至少检查：

1. `Ship.prefab` 中 `ShipView._boostTrailView` 仍正确指向 nested `BoostTrailRoot`
2. `BoostTrailRoot.prefab` 中 `_boostBloomVolume` 仍保持 prefab 为空引用
3. `SampleScene.unity` 中 `BoostTrailBloomVolume` 仍能被 binder 正确接线
4. `MaterialTextureLinker` 仅从精确目录回填现役贴图
5. 现役文档与注册表口径一致，不再把 GG 参考文档当作当前规范

## 10. 与其他文档的关系

- 玩家主观体验、删改优先级：看 `Ship_VFX_Player_Perception_Reference.md`
- 主拖尾、材质、Shader 当前实现状态：看 `BoostTrail_Shader_Implementation_Status.md`
- 物理名、路径、状态、owner 总表：看 `ShipVFX_AssetRegistry.md`
- 分批做什么、暂缓什么：看 `ShipVFX_MigrationPlan.md`
