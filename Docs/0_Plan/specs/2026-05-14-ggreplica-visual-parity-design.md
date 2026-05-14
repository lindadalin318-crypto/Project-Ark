# GGReplica Visual Parity Design

> Date: 2026-05-14  
> Status: Approved direction, implementation-ready  
> Goal: make `Ship_GGReplica.prefab` visually follow Galactic Glitch's original `Player.prefab` shader/material/VFX chain, not a sprite-only approximation.

---

## 1. Source of Truth

For GGReplica work, the external reference repository is mandatory:

```text
/Users/dada/Documents/Quark/ReferenceAssets/Galactic Glitch
├── DevXUnity/
└── DevXUnity_exported/
```

Do not infer GG behavior only from `Project-Ark`. The two DevXUnity folders contain the original extracted shader, material, prefab, sprite, and animation evidence.

---

## 2. Problem Statement

The current `GGReplicaPlayerViewAdapter` rebuild has the correct structural idea:

```text
ChangeViewState(int)
→ GGReplicaPlayerSkinSO state table
→ GGPlayerViewRoot renderer layers
→ Core / Boost / Shape modules
```

But visually it is still too close to plain sprite swapping. The original GG player visual chain relies on shader/material layers and module side effects:

- `Ship_Sprite_HL` is not a tinted sprite; it uses `PlayerShipHL` / `CLG/PlayerShipHighlight`.
- `View` is not just a black image; it uses `TeleportScheme`.
- Boost includes trail/energy visuals, including `PlayerLQTrail` material behavior.
- Dodge and Grab are module-driven transient visual states, not only sprite pack changes.

This pass fixes that mismatch.

---

## 3. Reference Evidence

### 3.1 PlayerShipHL

Shader evidence:

```text
/Users/dada/Documents/Quark/ReferenceAssets/Galactic Glitch/DevXUnity/CLG/CLG_PlayerShipHighlight.shader
```

Material evidence:

```text
/Users/dada/Documents/Quark/ReferenceAssets/Galactic Glitch/DevXUnity/Material/PlayerShipHL.mat
/Users/dada/Documents/Quark/ReferenceAssets/Galactic Glitch/DevXUnity_exported/Assets/Materials/PlayerShipHL.mat
```

Original material values from exported YAML:

```text
_Intensity = 8
_Smooth = 0.01
_Tint = RGBA(0.545098, 0.090196, 1, 1)
```

Original `Player.prefab` renderer evidence:

```text
Ship_Sprite_HL
material = PlayerShipHL.mat
sortingLayer = 5
sortingOrder = -1
```

### 3.2 TeleportScheme

Shader evidence:

```text
/Users/dada/Documents/Quark/ReferenceAssets/Galactic Glitch/DevXUnity/Shader Graphs/Shader Graphs_TeleportScheme.shader
```

Material evidence:

```text
/Users/dada/Documents/Quark/ReferenceAssets/Galactic Glitch/DevXUnity/Material/TeleportScheme.mat
/Users/dada/Documents/Quark/ReferenceAssets/Galactic Glitch/DevXUnity_exported/Assets/Materials/TeleportScheme.mat
```

Original material values from exported YAML:

```text
_Intensity = 1
_State = 0
Vector1_7d30456b30f54c21bbfffbf5604b1137 = 8
Vector1_e21f8c7fe8b74e4a998bb337674e196e = 0.3
Vector1_1caa320ead174d95a8275fd8551cfe6e = 0.002
```

Original `Player.prefab` renderer evidence:

```text
View
material = TeleportScheme.mat
sprite = scheme3_tp
color = black
sortingLayer = 2
sortingOrder = 5
```

### 3.3 PlayerLQTrail

Shader evidence:

```text
/Users/dada/Documents/Quark/ReferenceAssets/Galactic Glitch/DevXUnity/Shader Graphs/Shader Graphs_PlayerLQTrail.shader
```

Material evidence:

```text
/Users/dada/Documents/Quark/ReferenceAssets/Galactic Glitch/DevXUnity/Material/PlayerLQTrail.mat
/Users/dada/Documents/Quark/ReferenceAssets/Galactic Glitch/DevXUnity_exported/Assets/Materials/PlayerLQTrail.mat
```

Original material values from exported YAML:

```text
Main Color = RGBA(0.120545, 0, 0.188679, 1)
Edge Color = RGBA(0.613284, 0, 0.807843, 0)
_NoiseTilingMutationSmoothEdge = (1, 1, 0.2, 0.7)
```

Original `Player.prefab` TrailRenderer evidence:

```text
material = PlayerLQTrail.mat
time = 0.4
widthMultiplier = 4
```

### 3.4 PlayerLightSourceColored

Shader evidence:

```text
/Users/dada/Documents/Quark/ReferenceAssets/Galactic Glitch/DevXUnity/Sprite Shaders URP/Lit Sprite/Color/Sprite Shaders URP_Lit Sprite_Color_Strong Tint (Override) Lit PlayerLightSourceColored.shader
```

This is relevant to body lighting, but it is not the first pass blocker. It should be evaluated after `PlayerShipHL`, `TeleportScheme`, and `PlayerLQTrail` are integrated.

---

## 4. Design Decision

Use the extracted GG files as reference evidence, but do not blindly wire raw DevX shader dump files as production shader authority.

Reason:

- The shader files are extracted/dumped and contain generated/disassembled content.
- Some files may compile under the original Unity/URP version but fail or behave incorrectly in Unity 6000 + URP 2D.
- The exported `.mat` files preserve important parameter values, but their shader references are not directly portable in Project Ark.

Therefore the implementation rule is:

1. Try to preserve original shader names and parameter semantics.
2. Create Project Ark clean URP-compatible GGReplica shader assets.
3. Seed material parameters from original `.mat` YAML values.
4. Wire only the GGReplica prefab, never live `Ship.prefab`.
5. Let `GGReplicaAuditor` fail if the prefab falls back to plain/default sprite materials.

---

## 5. Target Architecture

### 5.1 New Art Assets

```text
Assets/_Art/Ship/GGReplica/Shaders/GGReplicaPlayerShipHighlight.shader
Assets/_Art/Ship/GGReplica/Shaders/GGReplicaTeleportScheme.shader
Assets/_Art/Ship/GGReplica/Shaders/GGReplicaPlayerLQTrail.shader

Assets/_Art/Ship/GGReplica/Materials/GGReplicaPlayerShipHL.mat
Assets/_Art/Ship/GGReplica/Materials/GGReplicaTeleportScheme.mat
Assets/_Art/Ship/GGReplica/Materials/GGReplicaPlayerLQTrail.mat
```

The material asset names are Project Ark names, but their parameters mirror GG source values.

### 5.2 Editor Authority

`GGReplicaMaterialAssetBuilder` becomes the material/shader setup authority for GGReplica:

```text
ProjectArk > Ship > GG Replica > Build Visual Materials
```

Responsibilities:

- Load clean GGReplica shaders by path.
- Create/update GGReplica material assets.
- Apply original GG parameter values.
- Report error if any shader cannot be found.
- Never touch live `Ship.prefab` or `SampleScene`.

`GGReplicaPrefabBuilder` consumes the material assets and wires them:

```text
Ship_Sprite_HL.material = GGReplicaPlayerShipHL.mat
View.material = GGReplicaTeleportScheme.mat
TrailRenderer.material = GGReplicaPlayerLQTrail.mat
```

### 5.3 Runtime Authority

`GGReplicaPlayerViewAdapter` remains the `ChangeViewState(int)` coordinator.

A new `GGReplicaMaterialVisualModule` owns runtime material parameter changes using `MaterialPropertyBlock`, not by mutating shared material assets.

Responsibilities:

- Boost: raise highlight/trail intensity.
- Dodge: pulse ghost/dodge/core layers.
- Grab: emphasize hand/highlight layers.
- Heal: visible healing color/intensity response.
- Idle/Undefined: restore stable default values.

---

## 6. State Visual Requirements

| ViewState | Required Visual Behavior |
| --- | --- |
| `Idle (0)` | Body sprites visible; HL material active but stable; View scheme layer present behind body. |
| `Boost (1)` | Boost body sprite + PlayerLQTrail visible; highlight intensity increases; boost module enables trail. |
| `Dodge (2)` | Body sprites are not cleared; dodge ghost and half silhouette visible; core pulse visible. |
| `Aim (3)` | Uses GG state sprite pack; HL material remains active. |
| `Fire (4)` | Uses GG state sprite pack; brief high-intensity highlight flash is allowed. |
| `HeavyFire (5)` | Uses GG state sprite pack; stronger highlight/energy feedback than Fire. |
| `HeavyAim (6)` | Uses GG state sprite pack; no fallback to default material. |
| `Grab (7)` | Grab hand renderers visible; offset preserved; hand/highlight feedback visible. |
| `WeaponUseMoment (8)` | Uses GG state sprite pack; transient material spike allowed. |
| `Heal (9)` | Healing sprite pack + visible color/material response. |
| `Undefined (15)` | Safe fallback to stable visible body + material defaults. |

---

## 7. Testing and Audit Requirements

### 7.1 Automated Tests

- `GGReplicaMaterialAssetBuilderTests`
  - materials are created at expected paths.
  - material shader names match GGReplica clean shader names.
  - material values match original GG evidence.

- `GGReplicaPrefabBuilderTests`
  - `Ship_Sprite_HL` uses `GGReplicaPlayerShipHL.mat`.
  - `View` uses `GGReplicaTeleportScheme.mat`.
  - GGReplica trail uses `GGReplicaPlayerLQTrail.mat`.

- `GGReplicaVisualModuleTests`
  - module uses `MaterialPropertyBlock` to vary state values.
  - shared material assets are not mutated at runtime.

- `GGReplicaAuditorTests`
  - audit fails if any required GGReplica material is missing.
  - audit fails if `Ship_Sprite_HL` or `View` uses default sprite material.

### 7.2 Manual Play Mode Checklist

Open:

```text
Assets/Scenes/GGReplicaShipTest.unity
```

In Play Mode:

- `7 Grab`: hands visible and visually emphasized.
- `2 Dodge`: ghost / half silhouette / core pulse visible.
- `1 Boost`: trail/energy visual visible, not only body sprite swap.
- `9 Heal`: healing state has visible material/color feedback.
- `0 Idle`: returns to stable non-flashing baseline.
- Console has no shader compile errors.

---

## 8. Non-Goals

- Do not modify live `Assets/_Prefabs/Ship/Ship.prefab`.
- Do not modify `Assets/Scenes/SampleScene.unity`.
- Do not integrate the full original GG `Player.cs` / `PlayerView.cs` runtime code.
- Do not block the pass on byte-perfect original shader behavior if Unity 6000 compatibility fails.
- Do not use raw DevX binary `.mat` as authoritative Project Ark assets; use their YAML values as parameter evidence.

---

## 9. Definition of Done

This pass is complete when:

1. `Ship_GGReplica.prefab` has GGReplica-specific `PlayerShipHL`, `TeleportScheme`, and `PlayerLQTrail` material equivalents wired.
2. `GGReplicaAuditor` catches missing/wrong/default materials.
3. Runtime state changes drive at least one material/VFX parameter through `MaterialPropertyBlock`.
4. `GGReplicaShipTest.unity` visibly differs from pure sprite swapping in Boost, Dodge, Grab, and Heal states.
5. Build and relevant EditMode/PlayMode tests pass.
