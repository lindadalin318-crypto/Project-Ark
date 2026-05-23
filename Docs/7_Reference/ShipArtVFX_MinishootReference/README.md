# Ship Art/VFX Reference Folder — Minishoot + Galactic Glitch

> 更新时间：2026-05-18 23:18  
> 用途：同步 `Ship_ArtVFX_Workflow.md` 最新计划中的参考资源要求。  
> 注意：本目录是参考资料包，不是 Unity 正式导入目录；不要从这里直接建立运行时引用。

## 1. 本次同步原因

`Docs/3_WorkflowsAndRules/Ship/Ship_ArtVFX_Workflow.md` 已加入参考项目反查结论：

- `Outline` 必须独立于 `Highlight`。
- `Dodge` 需要 `dash_01-05` 或 lean left/right 方向帧意识。
- `Boost / Fire / Hit / Weaving / Overheat` 需要材质参数矩阵。
- `VFX` 可由小纹理 + Shader / Particle 参数组合生成。
- `Galactic Glitch` 的状态图必须保留原状态语境，不能凭文件名误用。

因此本目录从原本偏 `Minishoot` 的批次资源包，扩展为：

```text
Minishoot = 可读轮廓、Dash/Lean、程序化 glow/ring/spark 材质参考
Galactic Glitch = 多层飞船状态映射、状态误用警告、SpritePack 参考
```

## 2. 新增 / 更新内容

### Batch 0：参考映射

- `Batch_0_Reference_Mapping/GalacticGlitch_PlayerSkinDefault/`
  - 放入 `Galactic Glitch` 的 `PlayerSkinDefault` 核心状态参考图。
- `Batch_0_Reference_Mapping/galactic_glitch_playerskin_state_map.csv`
  - 记录 `State -> layer -> source sprite -> copied file` 的映射。
- `source_map_plan_sync_2026-05-18.csv`
  - 记录本次同步过来的所有 Minishoot / Galactic Glitch 资源。

### Batch 1：Normal 基础玩家船

- `Batch_1_Normal/Solid/spr_ship_canary_solid_normal_albedo.png`
- `Batch_1_Normal/Liquid/spr_ship_canary_liquid_normal_albedo.png`
- `Batch_1_Normal/Core/spr_ship_canary_core_normal_albedo.png`
- `Batch_1_Normal/Highlight/spr_ship_canary_highlight_normal_mask.png`
- `Batch_1_Normal/Source/source_minishoot_player.png`
- `Batch_1_Normal/Source/source_minishoot_player_liquid.png`
- `Batch_1_Normal/Source/source_minishoot_player_full.png`
- `Batch_1_Normal/Source/source_minishoot_player_crystal.png`
- `Batch_1_Normal/Source/source_minishoot_ship_white_mask.png`

来源：`Minishoot` 的 `Player.png`、`_Player.png`、`__PlayerFull.png`、`PlayerCrystal.png`、`ShipWhite.png`。

用途：保留基础玩家船同源组作为 `Canary` Normal 状态分层参考。已删除此前误混入的 `Weapon0.png`、`SupershotPlayer1.png`、`SupershotPlayerOutline.png`、`WreckShipWaterMask.png`、`LightPlayerWall.png` 等非基础玩家船资源。

### Batch 2：Dodge / Lean

- `Batch_2_Dodge/Lean/spr_ship_canary_lean_left_dodge_albedo.png`
- `Batch_2_Dodge/Lean/spr_ship_canary_lean_right_dodge_albedo.png`
- `Batch_2_Dodge/Source/source_player_dash_03.png`
- `Batch_2_Dodge/Source/source_player_dash_05.png`
- `Batch_2_Dodge/Source/source_player_lean_left_01.png`
- `Batch_2_Dodge/Source/source_player_lean_left_02.png`
- `Batch_2_Dodge/Source/source_player_lean_left_03.png`
- `Batch_2_Dodge/Source/source_player_lean_right_01.png`
- `Batch_2_Dodge/Source/source_player_lean_right_02.png`
- `Batch_2_Dodge/Source/source_player_lean_right_03.png`

来源：`Minishoot` 的 `PlayerDash1-5` 与 `PlayerLeanLeft/Right1-3`。

用途：避免 Dodge 只靠一张 Ghost 图；后续需要方向感时优先补短帧序列。

### Batch 7：材质 / Shader

新增 `Minishoot` 材质参考：

- `mat_minishoot_player_outline_reference.mat`
- `mat_minishoot_ship_player_reference.mat`
- `mat_minishoot_ship_player_shape_reference.mat`
- `mat_minishoot_player_trail_reference.mat`
- `mat_minishoot_player_trail_boost_side_reference.mat`
- `mat_minishoot_ring_additive_reference.mat`

用途：为 `Outline`、ship body、trail、ring/additive VFX 的材质参数矩阵提供参考。

## 3. Galactic Glitch 状态误用警告

以下资源必须按状态语境使用：

| State | 含义 | Solid | Liquid | Highlight | 注意 |
| --- | --- | --- | --- | --- | --- |
| `0 / 15` | Normal | `Movement_10` | `Movement_3` | `Movement_21` | 可参考 Normal 三层结构 |
| `1` | Boost | `Boost_2` | `Boost_16` | `Boost_8` | 可参考 Boost 三层状态包 |
| `3 / 4 / 8` | Primary | `Primary_4` | `Primary` | `Primary_6` | Primary 武器状态，不是 Normal |
| `5 / 6` | Secondary | `Secondary_8` | `Secondary_0` | `Secondary_17` | Secondary 武器状态 |
| `7` | GrabGun | `GrabGun_Base_9` | `GrabGun_Base_9` | `GrabGun_Base_8` | **禁止当作 Normal / Boost / Primary 使用** |
| `9` | Healing | `Healing_0` | `Healing` | `vfx_dot_001` | Healing 状态 |

特别注意：

- `GrabGun_Base_9 / GrabGun_Base_8` 只属于 `State 7`。
- `Primary_4` 属于 `State 3 / 4 / 8`。
- 状态不明的图只能进入 `Reference`，不能进入正式生产清单。

## 4. 使用规则

- 本目录只作为视觉参考和源素材对照，不作为运行时权威。
- 正式路径、owner、状态判断仍以：
  - `Docs/Reference/ShipVFX_CanonicalSpec.md`
  - `Docs/Reference/ShipVFX_AssetRegistry.md`
  - `Docs/3_WorkflowsAndRules/Ship/Ship_ArtVFX_Workflow.md`
  为准。
- 若要把这里的参考资源推进 Unity 正式目录，必须先补：
  - Import Settings / PPU / Pivot / Atlas Tag
  - Material 参数矩阵
  - AssetRegistry 映射
  - ImplementationLog
