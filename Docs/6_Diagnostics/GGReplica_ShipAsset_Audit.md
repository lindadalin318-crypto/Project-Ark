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

## 6. Audit results

- Missing required source files: none.
- All required sprite, audio, and shader trial sources exist under `DevXUnity/`.
- Exported fallbacks exist for sprites under `DevXUnity_exported/Assets/Images/`.
- Exported fallbacks exist for audio under `DevXUnity_exported/Assets/Sounds/`.
- Direct migration allowed: PNG, WAV, and the three limited shader trial files only.
- Shader trial files are for Phase 1.5 evaluation only and are not runtime-approved.
- Reference-only files are listed in section 5.
- Task 1 import completed through `ProjectArk > Ship > GG Replica > Import Curated Assets`.
- Import result: 12 PNG sprites, 7 WAV clips, and 3 shader trial files imported under `Assets/_Art/Ship/GGReplica/`.
- Missing source files during import: none.
- Task 2 shader spike completed in `Docs/6_Diagnostics/GGReplica_ShaderSpike_Report.md`.
- Shader spike result: all three DevX shader trial files are L0 because DevX restore markers cause parse errors; use them as reference only and rebuild Project Ark-owned replacements if needed.
- Next step: add GGReplica visual and feel profile scripts/assets.
