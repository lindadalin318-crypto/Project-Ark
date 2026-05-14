# GGReplica V2 Original PlayerView Audit

> Date: 2026-05-14  
> Status: V2 reset baseline  
> Source of truth: `/Users/dada/Documents/Quark/ReferenceAssets/Galactic Glitch`

---

## 1. Hard reset decision

The previous GGReplica direction is not sufficient.

Previous implementation focused on:

```text
ChangeViewState(int)
→ state sprite pack
→ GGPlayerViewRoot sprite renderers
→ small material parameter changes
```

This still presents as different ship sprites being swapped. It does not reproduce the basic Galactic Glitch `Glitch` player ship experience.

V2 must stop treating `PlayerSkin.stateToSpritesTable` as the main visual authority. In original GG, sprite packs are only one layer in a larger `PlayerView` module stack.

---

## 2. Mandatory external reference paths

All GG replica work must inspect these first:

```text
/Users/dada/Documents/Quark/ReferenceAssets/Galactic Glitch/DevXUnity/
/Users/dada/Documents/Quark/ReferenceAssets/Galactic Glitch/DevXUnity_exported/
/Users/dada/Documents/Quark/ReferenceAssets/Galactic Glitch/Scripts_ilspycmd/
/Users/dada/Documents/Quark/ReferenceAssets/Galactic Glitch/Scripts_dnSpyEx/
```

Project-local assets under `Assets/_Art/Ship/GGReplica/` are derived copies, not the truth source.

---

## 3. Original `PlayerView` script evidence

Reference file:

```text
/Users/dada/Documents/Quark/ReferenceAssets/Galactic Glitch/Scripts_ilspycmd/GeneralAssembly/PlayerView.cs
```

Original `PlayerView.ViewState` values:

```text
Idle = 0
Boost = 1
Dodge = 2
Aim = 3
Fire = 4
HeavyFire = 5
HeavyAim = 6
Grab = 7
WeaponUseMoment = 8
Heal = 9
Undefined = 15
```

Important point: this enum is not the full player state machine. It is a view-facing state used by `PlayerView` alongside modules, animators, input, boost, dodge, hold, fluxy, trails, and particles.

`PlayerView` serialized module fields include:

```text
PlayerViewCoreModule coreModule
PlayerViewEnergyModule energyModule
PlayerViewShapeTrailModule shapeTrailModule
PlayerViewJumpModule jumpModule
PlayerViewFluxyTrailModule fluxyTrailModule
PlayerViewFluxyGrabModule fluxyGrabModule
PlayerViewHoldModule holdModule
PlayerViewDamageOverlayModule damageOverlayModule
PlayerViewSpawnModule spawnModule
PlayerViewLQTrailModule particleTrailModule
PlayerViewTeleportModule teleportModule
PlayerViewBoostModule boostModule
```

Current Project Ark GGReplica only approximates a small subset. V2 must rebuild around this module stack.

---

## 4. Original `Player.prefab` visual module evidence

Reference file:

```text
/Users/dada/Documents/Quark/ReferenceAssets/Galactic Glitch/DevXUnity_exported/Assets/Prefab/Player.prefab
```

Confirmed original module / VFX GameObjects:

```text
PlayerView
ShapeTrailModule
LQTrailModule
DarkTrailModule
FluxySolver
FluxyGrabModule
LQTrailsContainer
ShapeShiftStateHitbox
vfx_boost_trail_loop_enhanced
vfx_boost_trail_burst_enhanced
ps_techno_flame_trail_R
ps_techno_flame_trail_quick
ps_techno_flame_trail_start
ps_ember_trail
startrails
startrails_long
```

This proves original GG visual state is not just `SpriteRenderer.sprite = x`.

V2 must reproduce the module/VFX hierarchy first, then wire state changes.

---

## 5. Original component evidence

Reference scripts:

```text
Scripts_ilspycmd/GeneralAssembly/PlayerViewBoostModule.cs
Scripts_ilspycmd/GeneralAssembly/PlayerViewCoreModule.cs
Scripts_ilspycmd/GeneralAssembly/PlayerViewLQTrailModule.cs
Scripts_ilspycmd/GeneralAssembly/PlayerViewFluxyTrailModule.cs
Scripts_ilspycmd/GeneralAssembly/PlayerViewFluxyGrabModule.cs
Scripts_ilspycmd/GeneralAssembly/PlayerViewShapeTrailModule.cs
Scripts_ilspycmd/GeneralAssembly/PlayerViewTeleportModule.cs
Scripts_ilspycmd/GeneralAssembly/PlayerViewModules/PlayerViewEnergyModule.cs
Scripts_ilspycmd/GeneralAssembly/PlayerViewModules/PlayerViewShapeChangeModule.cs
```

Key observations:

- `PlayerViewBoostModule` owns an array of `ParticleSystem` objects and listens to boost start/end.
- `PlayerViewCoreModule` owns core sprite, dodge shell values, dodge fade/scale timings, cloak effect, player velocity data, and glitched charged IDs.
- `PlayerViewLQTrailModule`, `PlayerViewFluxyTrailModule`, and `PlayerViewShapeTrailModule` are distinct systems, not one trail renderer.
- `PlayerViewFluxyGrabModule` and `PlayerViewShapeChangeModule` imply grab/shape behavior needs its own module, not only hand sprite visibility.

---

## 6. V2 target behavior

The main validation UI must no longer be `0-9` as the primary workflow.

Primary validation states should be:

```text
Idle
Move
Boost Hold
Dodge Burst
Grab Hold
Heal
Fire / Aim secondary validation
```

`0-9 ViewState` may remain as debug-only, but is not the acceptance path.

V2 acceptance requires:

1. The ship reads as the same basic GG `Glitch` ship across states.
2. Boost shows original-style velocity/trail/particle stack, not only a sprite pack.
3. Dodge shows shell/ghost/core scale/fade behavior.
4. Grab shows shape/hand/fluxy-like visual behavior.
5. Heal shows a distinct healing visual module.
6. State changes are driven by controls/input simulation, not by manual sprite-pick buttons.

---

## 7. Implementation reset plan

V2 should create a new isolated implementation path instead of layering more patches onto the current sprite-switch prototype.

Proposed new artifacts:

```text
Assets/_Prefabs/Ship/Ship_GGReplicaV2.prefab
Assets/Scenes/GGReplicaGlitchV2Test.unity
Assets/Scripts/Ship/GGReplica/V2/GGReplicaGlitchInputDriver.cs
Assets/Scripts/Ship/GGReplica/V2/GGReplicaGlitchMotor.cs
Assets/Scripts/Ship/GGReplica/V2/GGReplicaGlitchView.cs
Assets/Scripts/Ship/GGReplica/V2/GGReplicaGlitchBoostModule.cs
Assets/Scripts/Ship/GGReplica/V2/GGReplicaGlitchDodgeModule.cs
Assets/Scripts/Ship/GGReplica/V2/GGReplicaGlitchGrabModule.cs
Assets/Scripts/Ship/GGReplica/V2/GGReplicaGlitchHealModule.cs
Assets/Scripts/Ship/Editor/GGReplica/V2/GGReplicaGlitchV2PrefabBuilder.cs
Assets/Scripts/Ship/Editor/GGReplica/V2/GGReplicaGlitchV2TestSceneBuilder.cs
Assets/Scripts/Ship/Editor/GGReplica/V2/GGReplicaGlitchV2Auditor.cs
```

The existing `Ship_GGReplica.prefab` can remain as legacy/reference until V2 is proven.

---

## 8. Immediate next step

Build `GGReplicaGlitchV2PrefabBuilder` from original `Player.prefab` evidence:

- Create `Ship_GGReplicaV2.prefab` from scratch.
- Build a `GGGlitchVisualRoot` that mirrors original module categories.
- Import/copy exact required original particle/trail materials and sprites by explicit path.
- Create a test scene with keyboard controls and no `0-9` button dependency.

Only after V2 visual rig exists should we reintroduce PlayerSkin state packs as supporting data.

---

## 9. V2 first playable slice status — 2026-05-14

Created isolated V2 artifacts:

```text
Assets/_Prefabs/Ship/Ship_GGReplicaV2.prefab
Assets/Scenes/GGReplicaGlitchV2Test.unity
Assets/Scripts/Ship/GGReplica/V2/GGReplicaGlitchState.cs
Assets/Scripts/Ship/GGReplica/V2/GGReplicaGlitchInputDriver.cs
Assets/Scripts/Ship/GGReplica/V2/GGReplicaGlitchMotor.cs
Assets/Scripts/Ship/GGReplica/V2/GGReplicaGlitchView.cs
Assets/Scripts/Ship/Editor/GGReplica/V2/GGReplicaGlitchV2PrefabBuilder.cs
Assets/Scripts/Ship/Editor/GGReplica/V2/GGReplicaGlitchV2TestSceneBuilder.cs
```

This first slice intentionally avoids the old `GGReplicaPlayerViewAdapter` and `GGReplicaTestSwitcher` path.

Validation scene controls:

```text
WASD / Arrow keys = Move
Shift = Boost Hold
Space = Dodge Burst
E = Grab Hold
Q = Heal
Mouse Left = Fire/Aim validation
```

Generated V2 rig currently contains:

```text
GGGlitchVisualRoot
BodyLayers
CoreModule
BoostModule
LQTrailModule
LQTrailsContainer
ShapeTrailModule
DarkTrailModule
FluxySolver
FluxyGrabModule
GrabModule
HealModule
DodgeModule
FireAimModule
ShapeShiftStateHitbox
vfx_boost_trail_loop_enhanced
vfx_boost_trail_burst_enhanced
ps_techno_flame_trail_R
ps_techno_flame_trail_quick
ps_techno_flame_trail_start
ps_ember_trail
startrails
startrails_long
```

Machine validation:

- V2 editor tests: 2/2 passed.
- V2 runtime tests: 2/2 passed.
- Built V2 prefab/test scene summary: `trails=5`, `particles=8`.
- `dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly`: 0 errors.

Known limitation: this is still a first playable reconstruction slice, not byte-perfect GG. Next step is to refine particle/trail shapes and timings against original `Player.prefab` serialized values.
