# GGReplica Ship Asset Audit

> Source: `/Users/dada/Documents/Quark/ReferenceAssets/Galactic Glitch`  
> Target: `Assets/_Art/Ship/GGReplica/`  
> Status: Phase 1 curated import complete — 2026-05-13 15:36

## 1. Rules

- All source/fallback paths in this document are relative to `/Users/dada/Documents/Quark/ReferenceAssets/Galactic Glitch`.
- All target paths are Unity AssetDatabase paths relative to `/Users/dada/Documents/GitHub/Project-Ark`.
- Do not copy external `.meta` files.
- Prefer `DevXUnity/Sprite/` for Sprite PNG sources.
- `DevXUnity_exported/Assets/Images/*.meta` is a reference source only; Task 1 importer uses the explicit PPU/Pivot values in this audit table as the authority.
- `Exported fallback` PNG paths are backup copy sources if preferred PNG sources disappear; they are not importer-setting sources.
- Use `DevXUnity_exported/Assets/Sounds/` only as audio fallback.
- Copy only the three listed shader trial files into `Shaders/DevX_Trial/`; this is allowed for spike testing and does not mean the shaders are approved for runtime use.
- Do not copy whole shader directories.
- Do not modify live `Assets/_Art/Ship/Glitch/` assets during this replica experiment.
- Do not modify live `Assets/_Prefabs/Ship/Ship.prefab` or `Assets/Scenes/SampleScene.unity`.

## 2. Required sprite mapping

| Category | Source preferred | Exported fallback | Target | PPU | Pivot | Status | Notes |
|---|---|---|---|---:|---|---|---|
| Normal Solid | `DevXUnity/Sprite/Movement_d10.png` | `DevXUnity_exported/Assets/Images/Movement_d10.png` | `Assets/_Art/Ship/GGReplica/Sprites/Movement_10.png` | 320 | center | IMPORTED | Normal solid |
| Normal Liquid | `DevXUnity/Sprite/Movement_d3.png` | `DevXUnity_exported/Assets/Images/Movement_d3.png` | `Assets/_Art/Ship/GGReplica/Sprites/Movement_3.png` | 320 | center | IMPORTED | Normal liquid |
| Normal Highlight | `DevXUnity/Sprite/Movement_d21.png` | `DevXUnity_exported/Assets/Images/Movement_d21.png` | `Assets/_Art/Ship/GGReplica/Sprites/Movement_21.png` | 320 | center | IMPORTED | Normal highlight |
| Boost Solid | `DevXUnity/Sprite/Boost_d2.png` | `DevXUnity_exported/Assets/Images/Boost_d2.png` | `Assets/_Art/Ship/GGReplica/Sprites/Boost_2.png` | 320 | center | IMPORTED | Boost solid |
| Boost Liquid | `DevXUnity/Sprite/Boost_d16.png` | `DevXUnity_exported/Assets/Images/Boost_d16.png` | `Assets/_Art/Ship/GGReplica/Sprites/Boost_16.png` | 320 | center | IMPORTED | Boost liquid |
| Boost Highlight | `DevXUnity/Sprite/Boost_d8.png` | `DevXUnity_exported/Assets/Images/Boost_d8.png` | `Assets/_Art/Ship/GGReplica/Sprites/Boost_8.png` | 320 | center | IMPORTED | Boost highlight |
| Fire Solid | `DevXUnity/Sprite/Primary_d4.png` | `DevXUnity_exported/Assets/Images/Primary_d4.png` | `Assets/_Art/Ship/GGReplica/Sprites/Primary_4.png` | 320 | center | IMPORTED | Fire solid |
| Fire Liquid | `DevXUnity/Sprite/Primary.png` | `DevXUnity_exported/Assets/Images/Primary.png` | `Assets/_Art/Ship/GGReplica/Sprites/Primary.png` | 320 | center | IMPORTED | Fire liquid |
| Fire Highlight | `DevXUnity/Sprite/Primary_d6.png` | `DevXUnity_exported/Assets/Images/Primary_d6.png` | `Assets/_Art/Ship/GGReplica/Sprites/Primary_6.png` | 320 | center | IMPORTED | Fire highlight |
| Dodge Ghost | `DevXUnity/Sprite/player_test_fire.png` | `DevXUnity_exported/Assets/Images/player_test_fire.png` | `Assets/_Art/Ship/GGReplica/Sprites/player_test_fire.png` | 707 | `(0.5, 0.3282)` | IMPORTED | Dodge ghost |
| Back Thruster | `DevXUnity/Sprite/GrabGun_back_d3.png` | `DevXUnity_exported/Assets/Images/GrabGun_back_d3.png` | `Assets/_Art/Ship/GGReplica/Sprites/GrabGun_Back_3.png` | 320 | center | IMPORTED | Rear layer |
| Reactor | `DevXUnity/Sprite/reactor.png` | `DevXUnity_exported/Assets/Images/reactor.png` | `Assets/_Art/Ship/GGReplica/Sprites/reactor.png` | 320 | center | IMPORTED | Core sprite |
| Secondary Solid | `DevXUnity/Sprite/Secondary_d8.png` | `DevXUnity_exported/Assets/Images/Secondary_d8.png` | `Assets/_Art/Ship/GGReplica/Sprites/Secondary_8.png` | 320 | center | IMPORTED | HeavyFire / HeavyAim solid |
| Secondary Liquid | `DevXUnity/Sprite/Secondary.png` | `DevXUnity_exported/Assets/Images/Secondary.png` | `Assets/_Art/Ship/GGReplica/Sprites/Secondary_0.png` | 320 | center | IMPORTED | HeavyFire / HeavyAim liquid |
| Secondary Highlight | `DevXUnity/Sprite/Secondary_d17.png` | `DevXUnity_exported/Assets/Images/Secondary_d17.png` | `Assets/_Art/Ship/GGReplica/Sprites/Secondary_17.png` | 320 | center | IMPORTED | HeavyFire / HeavyAim highlight |
| Healing Solid | `DevXUnity/Sprite/Healing_d1.png` | `DevXUnity_exported/Assets/Images/Healing_d1.png` | `Assets/_Art/Ship/GGReplica/Sprites/Healing_0.png` | 320 | center | IMPORTED | Heal solid |
| Healing Liquid | `DevXUnity/Sprite/Healing.png` | `DevXUnity_exported/Assets/Images/Healing.png` | `Assets/_Art/Ship/GGReplica/Sprites/Healing.png` | 320 | center | IMPORTED | Heal liquid |
| Healing Highlight | `DevXUnity/Sprite/vfx_dot_001.png` | `DevXUnity_exported/Assets/Images/vfx_dot_001.png` | `Assets/_Art/Ship/GGReplica/Sprites/vfx_dot_001.png` | 320 | center | IMPORTED | Heal highlight |
| Grab Body | `DevXUnity/Sprite/GrabGun_Base_d9.png` | `DevXUnity_exported/Assets/Images/GrabGun_Base_d9.png` | `Assets/_Art/Ship/GGReplica/Sprites/GrabGun_Base_9.png` | 320 | center | IMPORTED | Grab solid/liquid |
| Grab Highlight | `DevXUnity/Sprite/GrabGun_Base_d8.png` | `DevXUnity_exported/Assets/Images/GrabGun_Base_d8.png` | `Assets/_Art/Ship/GGReplica/Sprites/GrabGun_Base_8.png` | 320 | center | IMPORTED | Grab highlight |
| Grab Hand | `DevXUnity/Sprite/GrabGun_Hand_d7.png` | `DevXUnity_exported/Assets/Images/GrabGun_Hand_d7.png` | `Assets/_Art/Ship/GGReplica/Sprites/GrabGun_Hand_7.png` | 320 | center | IMPORTED | Fixed grab hand layer |
| View Silhouette | `DevXUnity/Sprite/scheme3_tp.png` | `DevXUnity_exported/Assets/Images/scheme3_tp.png` | `Assets/_Art/Ship/GGReplica/Sprites/scheme3_tp.png` | 320 | center | IMPORTED | Top view/scheme silhouette |
| Dodge Half Silhouette | `DevXUnity/Sprite/SHIP_PLAYER_DODGE_HALF.png` | `DevXUnity_exported/Assets/Images/SHIP_PLAYER_DODGE_HALF.png` | `Assets/_Art/Ship/GGReplica/Sprites/SHIP_PLAYER_DODGE_HALF.png` | 320 | center | IMPORTED | Dodge half silhouette module layer |

## 3. Required audio mapping

| Source | Exported fallback | Target | Status | Notes |
|---|---|---|---|---|
| `DevXUnity/AudioClip/SND_PLAYER_BOOST.wav` | `DevXUnity_exported/Assets/Sounds/SND_PLAYER_BOOST.wav` | `Assets/_Art/Ship/GGReplica/Audio/SND_PLAYER_BOOST.wav` | IMPORTED | Boost loop |
| `DevXUnity/AudioClip/SND_PLAYER_BOOST_IGNITE.wav` | `DevXUnity_exported/Assets/Sounds/SND_PLAYER_BOOST_IGNITE.wav` | `Assets/_Art/Ship/GGReplica/Audio/SND_PLAYER_BOOST_IGNITE.wav` | IMPORTED | Boost start |
| `DevXUnity/AudioClip/PLAYER_DODGE.wav` | `DevXUnity_exported/Assets/Sounds/PLAYER_DODGE.wav` | `Assets/_Art/Ship/GGReplica/Audio/PLAYER_DODGE.wav` | IMPORTED | Dodge |
| `DevXUnity/AudioClip/PLAYER_NORMAL_SHOT.wav` | `DevXUnity_exported/Assets/Sounds/PLAYER_NORMAL_SHOT.wav` | `Assets/_Art/Ship/GGReplica/Audio/PLAYER_NORMAL_SHOT.wav` | IMPORTED | Fire loop/shot |
| `DevXUnity/AudioClip/PLAYER_FIRST_SHOT.wav` | `DevXUnity_exported/Assets/Sounds/PLAYER_FIRST_SHOT.wav` | `Assets/_Art/Ship/GGReplica/Audio/PLAYER_FIRST_SHOT.wav` | IMPORTED | Fire start |
| `DevXUnity/AudioClip/PLAYER_LAST_SHOT.wav` | `DevXUnity_exported/Assets/Sounds/PLAYER_LAST_SHOT.wav` | `Assets/_Art/Ship/GGReplica/Audio/PLAYER_LAST_SHOT.wav` | IMPORTED | Fire end |
| `DevXUnity/AudioClip/PLAYER_DEATH.wav` | `DevXUnity_exported/Assets/Sounds/PLAYER_DEATH.wav` | `Assets/_Art/Ship/GGReplica/Audio/PLAYER_DEATH.wav` | IMPORTED | Death reference |

## 4. Shader candidates

| Candidate | Source | Trial target | Source status | Grade | Decision | Notes |
|---|---|---|---|---|---|---|
| PlayerShipHighlight | `DevXUnity/CLG/CLG_PlayerShipHighlight.shader` | `Assets/_Art/Ship/GGReplica/Shaders/DevX_Trial/CLG_PlayerShipHighlight.shader` | IMPORTED | L0 | Reference only | Parse error at line 36 from DevX restore marker; rebuild additive highlight shader |
| PlayerLightSourceColored | `DevXUnity/Sprite Shaders URP/Lit Sprite/Color/Sprite Shaders URP_Lit Sprite_Color_Strong Tint (Override) Lit PlayerLightSourceColored.shader` | `Assets/_Art/Ship/GGReplica/Shaders/DevX_Trial/Lit_PlayerLightSourceColored.shader` | IMPORTED | L0 | Reference only | Parse error at line 53 from DevX restore marker; rebuild URP 2D tint/liquid shader |
| PlayerLQTrail | `DevXUnity/Shader Graphs/Shader Graphs_PlayerLQTrail.shader` | `Assets/_Art/Ship/GGReplica/Shaders/DevX_Trial/PlayerLQTrail.shader` | IMPORTED | L0 | Reference only | Parse error at line 35 from DevX restore marker; rebuild Project Ark boost trail shader |

Additional shader variants available for later spike if needed:

- `DevXUnity/Sprite Shaders URP/Lit Sprite/Color/Sprite Shaders URP_Lit Sprite_Color_Strong Tint (Override) Lit PlayerLightSourceColored (EO).shader`
- `DevXUnity/Sprite Shaders URP/Lit Sprite/Color/Sprite Shaders URP_Lit Sprite_Color_Strong Tint (Override) Glitch_PlayerLightSource_Colored.shader`

## 5. Reference-only paths

These files are allowed for inspection but must not be copied into the Project Ark runtime chain:

| Purpose | Preferred path | Notes |
|---|---|---|
| Player prefab hierarchy reference | `DevXUnity_exported/Assets/Prefab/Player.prefab` | Read-only; do not migrate as prefab |
| Animator parameter/state reference | `DevXUnity_exported/Assets/AnimatorController/Player.controller` | Read-only; do not attach directly |
| Player logic field reference | `Scripts_dnSpyEx/GeneralAssembly_direct/GeneralAssembly/Player.cs` | Preferred over `Scripts_ilspycmd` for this audit because prior Ship/VFX research used dnSpyEx paths |
| PlayerView visual field reference | `Scripts_dnSpyEx/GeneralAssembly_direct/GeneralAssembly/PlayerView.cs` | Preferred source for sprite-layer and ViewState field names |
| Boost view module reference | `Scripts_dnSpyEx/GeneralAssembly_direct/GeneralAssembly/PlayerViewBoostModule.cs` | Read-only; only module shape/fields are useful |

## 6. Visual parity material evidence

| Target layer | Original shader evidence | Original material evidence | Project Ark target | Original parameters / prefab evidence |
|---|---|---|---|---|
| `Ship_Sprite_HL` | `DevXUnity/CLG/CLG_PlayerShipHighlight.shader` | `DevXUnity/Material/PlayerShipHL.mat`; `DevXUnity_exported/Assets/Materials/PlayerShipHL.mat` | `Assets/_Art/Ship/GGReplica/Materials/GGReplicaPlayerShipHL.mat` | `_Intensity=8`, `_Smooth=0.01`, `_Tint=#8B17FF`; `Player.prefab` wires this material to `Ship_Sprite_HL`, sorting layer 5, order -1 |
| `View` | `DevXUnity/Shader Graphs/Shader Graphs_TeleportScheme.shader` | `DevXUnity/Material/TeleportScheme.mat`; `DevXUnity_exported/Assets/Materials/TeleportScheme.mat` | `Assets/_Art/Ship/GGReplica/Materials/GGReplicaTeleportScheme.mat` | `_Intensity=1`, `_State=0`, scan scale 8, glitch strength 0.3; `Player.prefab` wires this material to `View`, sprite `scheme3_tp`, black color, sorting layer 2, order 5 |
| `GGPlayerLQTrail` | `DevXUnity/Shader Graphs/Shader Graphs_PlayerLQTrail.shader` | `DevXUnity/Material/PlayerLQTrail.mat`; `DevXUnity_exported/Assets/Materials/PlayerLQTrail.mat` | `Assets/_Art/Ship/GGReplica/Materials/GGReplicaPlayerLQTrail.mat` | Main color `(0.120545,0,0.188679,1)`, edge color `(0.613284,0,0.807843,0)`, noise `(1,1,0.2,0.7)`; `Player.prefab` TrailRenderer uses time `0.4`, width `4` |

These original shader files remain reference evidence. Project Ark owns clean Unity 6000-compatible shader replacements under `Assets/_Art/Ship/GGReplica/Shaders/`.

## 7. Runtime material-state feedback

`GGReplicaMaterialVisualModule` drives runtime shader parameters through `MaterialPropertyBlock`, so authored material assets remain unchanged during Play Mode.

| ViewState | Runtime parameters | Expected visible purpose |
|---|---|---|
| `Idle` / `Undefined` | `_Intensity=8`, `_BoostAmount=0`, `_HealAmount=0`, `_SchemeAlpha=0.45`, `_TrailIntensity=0` | Stable baseline matching GG default highlight/scheme layers |
| `Boost` | `_Intensity=12`, `_BoostAmount=1`, `_GlitchStrength=0.55`, `_TrailIntensity=1`, TrailRenderer `emitting=true` | Visible boost trail and stronger player highlight |
| `Dodge` | `_Intensity=10`, `_Pulse=1`, `_GlitchStrength=0.7`, trail off | Dodge ghost/core/scheme pulse instead of plain sprite swap |
| `Grab` | `_Intensity=10`, `_GrabEmphasis=1`, `_GlitchStrength=0.45` | Grab hand layers receive extra shader emphasis |
| `Heal` | `_Intensity=11`, `_HealAmount=1`, `_Tint=(0.35,1,0.85,1)`, `_GlitchStrength=0.25` | Healing state reads as a distinct material/color response |

## 8. Audit results

- Missing required source files: none.
- All required sprite, audio, and shader trial sources exist under `DevXUnity/`.
- Exported fallbacks exist for sprites under `DevXUnity_exported/Assets/Images/`.
- Exported fallbacks exist for audio under `DevXUnity_exported/Assets/Sounds/`.
- Direct migration allowed: PNG, WAV, and the three limited shader trial files only.
- Shader trial files are for Phase 1.5 evaluation only and are not runtime-approved.
- Reference-only files are listed in section 5.
- Task 1 import completed through `ProjectArk > Ship > GG Replica > Import Curated Assets`.
- Import result: 23 PNG sprites, 7 WAV clips, and 3 shader trial files imported under `Assets/_Art/Ship/GGReplica/`.
- PlayerView rebuild sprite expansion completed on 2026-05-14: Secondary, Healing, Grab body/highlight/hand, View silhouette, and Dodge half silhouette are now included in the curated importer.
- Missing source files during import: none.
- Task 2 shader spike completed in `Docs/6_Diagnostics/GGReplica_ShaderSpike_Report.md`.
- Shader spike result: all three DevX shader trial files are L0 because DevX restore markers cause parse errors; use them as reference only and rebuild Project Ark-owned replacements if needed.
- Next step: add `GGReplicaPlayerSkinSO` and build `GGReplicaPlayerSkin.asset` from the expanded sprite set.
