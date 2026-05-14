# GGReplica PlayerView Rebuild Design

> Date: 2026-05-14 01:29  
> Status: Draft for user review  
> Basis: `Docs/6_Diagnostics/GGReplica_PlayerView_DeepAudit.md`  
> Goal: replace the current simplified GGReplica sprite switcher with a closer replica of Galactic Glitch's `PlayerView + PlayerSkin + Animator Event` visual model.

---

## 1. Problem statement

The current `Ship_GGReplica.prefab` is useful as an asset import and wiring prototype, but it is not a faithful Galactic Glitch player ship replica.

Current behavior:

```text
Test UI / F key
→ GGReplicaVisualState
→ directly swaps Solid / Liquid / Highlight sprites
```

Original GG behavior:

```text
PlayerShipState
→ Player.controller Animator state
→ AnimationClip event ChangeViewState(int)
→ Player.ChangeViewState(PlayerView.ViewState)
→ PlayerView.ChangeState(...)
→ PlayerSkinDefault.stateToSpritesTable
→ sprite renderers + fixed skin fields + module side effects
```

The next phase must rebuild the visual model rather than continue patching the simplified five-state switcher.

---

## 2. Goals

1. Preserve isolation: do not modify `Assets/_Prefabs/Ship/Ship.prefab` or `Assets/Scenes/SampleScene.unity`.
2. Replace simplified five-state visual logic with GG-aligned `ViewState` semantics.
3. Represent `PlayerSkinDefault.stateToSpritesTable` as authored data in Project Ark.
4. Rebuild `Ship_GGReplica.prefab` visual hierarchy closer to original `PlayerView`.
5. Treat Dodge, Boost, Grab, and core/eye effects as module behavior, not only sprite packs.
6. Keep the result testable through `GGReplicaShipTest.unity` and `GGReplicaAuditor`.

---

## 3. Non-goals

- Do not import or attach original GG `Player.cs`, `PlayerView.cs`, or `Player.prefab` directly.
- Do not adopt DevX shader files that already graded L0.
- Do not attempt full Fluxy fluid simulation in this phase.
- Do not replace Project Ark's live ship or live Ship/VFX chain.
- Do not solve all weapon gameplay state integration yet; provide explicit test/API entry points first.

---

## 4. Recommended approach

### Approach A — patch current five-state adapter

Keep `GGReplicaVisualState` and add more sprites/layers.

- Pros: fastest.
- Cons: keeps the wrong state model; Dodge/Grab/Heal remain incorrectly represented.
- Decision: reject.

### Approach B — rebuild around GG `PlayerView` concepts

Introduce a GG-aligned view-state model, full skin table, fixed skin fields, and small module components.

- Pros: closest to original architecture while remaining isolated and manageable.
- Cons: requires migrating some current profile/adapter code.
- Decision: recommended.

### Approach C — directly recreate original prefab hierarchy from exported YAML

Attempt to procedurally rebuild the whole original `Player.prefab` structure.

- Pros: maximum visual fidelity if successful.
- Cons: high risk due to missing scripts/shaders/Fluxy dependencies; likely unstable.
- Decision: reject for now.

---

## 5. Target architecture

```text
Ship_GGReplica.prefab
├── existing Project Ark movement/input/health components
├── GGReplicaPlayerViewAdapter             # new coordinator, GG-like API
├── GGReplicaCoreVisualModule              # core/eye/dodge shell MVP
├── GGReplicaBoostVisualModule             # boost particles/trail MVP
├── GGReplicaShapeVisualModule             # grab/shape-change MVP
├── GGReplicaAudio/Feel adapters            # existing, kept if still useful
└── ShipVisual / GGPlayerViewRoot
    ├── Ship_Sprite_Liquid
    ├── Ship_Sprite_HL
    ├── Dodge_Sprite
    ├── Ship_Sprite_Solid
    ├── Ship_Sprite_Back
    ├── Ship_Sprite_Solid_Grab_R
    ├── Ship_Sprite_Solid_Grab_L
    ├── Core_Sprite_Reactor
    ├── Core_Sprite_Eye
    └── View
```

`GGReplicaPlayerViewAdapter` becomes the coordinator. The current `GGReplicaShipViewAdapter` should be deprecated or replaced by this coordinator.

---

## 6. Data model

### 6.1 `GGReplicaViewState`

Preserve original integer values:

```csharp
public enum GGReplicaViewState
{
    Idle = 0,
    Boost = 1,
    Dodge = 2,
    Aim = 3,
    Fire = 4,
    HeavyFire = 5,
    HeavyAim = 6,
    Grab = 7,
    WeaponUseMoment = 8,
    Heal = 9,
    Undefined = 15
}
```

### 6.2 `GGReplicaSpritePack`

Either migrate the existing class or create a new class:

```csharp
[Serializable]
public sealed class GGReplicaViewSpritePack
{
    public GGReplicaViewState State;
    public float FadeDuration;
    public Sprite SolidSprite;
    public Sprite LiquidSprite;
    public Sprite HighlightSprite;
    public Vector3 SpritesOffset;
}
```

Important rule: sprite fields may be null. `Dodge` intentionally has null body sprites and must not clear the existing body visuals unless the original behavior requires it.

### 6.3 `GGReplicaPlayerSkinSO`

New or migrated profile asset:

```text
GGReplicaPlayerSkinSO
├── List<GGReplicaViewSpritePack> StateToSpritesTable
├── Sprite ShipSpriteSolidGrabR
├── Sprite ShipSpriteSolidGrabL
├── Sprite ShipSpriteBack
├── Sprite ReactorSprite
├── Sprite EyeSprite
├── Sprite ViewSilhouetteSprite
├── Sprite DodgeSprite
├── Sprite DodgeHalfSprite
├── Color ShipHighlightColor        # #8B17FF
├── Color TransitionColor           # #AB00FF
├── Energy wave/glow color fields
└── Audio references currently in visual profile, if still needed
```

The current `GGReplicaShipVisualProfileSO` can be kept as a migration bridge, but the target name should reflect original GG semantics: `GGReplicaPlayerSkinSO`.

---

## 7. Asset import expansion

Add importer entries for missing sprites:

| Target concept | Preferred source | Target name |
|---|---|---|
| Secondary solid | `DevXUnity/Sprite/Secondary_d8.png` | `Secondary_8.png` |
| Secondary liquid | `DevXUnity/Sprite/Secondary.png` or resolved `Secondary_d0` equivalent | `Secondary_0.png` |
| Secondary highlight | `DevXUnity/Sprite/Secondary_d17.png` | `Secondary_17.png` |
| Healing solid | `DevXUnity/Sprite/Healing_d1.png` after confirming mapping | `Healing_0.png` |
| Healing liquid | `DevXUnity/Sprite/Healing.png` | `Healing.png` |
| Healing highlight | `DevXUnity/Sprite/vfx_dot_001.png` | `vfx_dot_001.png` |
| Grab solid/liquid | `DevXUnity/Sprite/GrabGun_Base_d9.png` | `GrabGun_Base_9.png` |
| Grab highlight | `DevXUnity/Sprite/GrabGun_Base_d8.png` | `GrabGun_Base_8.png` |
| Grab hand | `DevXUnity/Sprite/GrabGun_Hand_d7.png` | `GrabGun_Hand_7.png` |
| View silhouette | `DevXUnity/Sprite/scheme3_tp.png` | `scheme3_tp.png` |
| Dodge half silhouette | `DevXUnity/Sprite/SHIP_PLAYER_DODGE_HALF.png` | `SHIP_PLAYER_DODGE_HALF.png` |

Mapping notes:

- Keep explicit source → target mapping. Do not fuzzy-search by display name.
- Do not copy external `.meta` files.
- Use authored PPU/pivot values from audit where known; otherwise record unknowns in the new audit and default to conservative importer settings.

---

## 8. Runtime behavior

### 8.1 Public API

`GGReplicaPlayerViewAdapter` should expose:

```csharp
public void ChangeViewState(int state);
public void ChangeViewState(GGReplicaViewState state, bool strict = false);
public GGReplicaViewSpritePack GetCurrentSpritePack();
```

The test scene UI should call this API. This mirrors original `AnimationClip -> ChangeViewState(int)` events.

### 8.2 Sprite application rules

1. Look up `GGReplicaViewSpritePack` by state.
2. If pack is missing, warn loudly.
3. If a pack sprite field is null, do not blindly clear that renderer. This is required for Dodge.
4. Apply `SpritesOffset` to the sprite group root or the three body renderer transforms consistently.
5. Honor `FadeDuration`:
   - `0` = instant.
   - `>0` = fade/tint transition MVP.
6. Apply fixed skin fields on initialization:
   - back sprite
   - grab hand sprites
   - reactor / eye / view silhouette
   - highlight color

### 8.3 Module behavior MVP

Do not try to recreate all GG modules at once. MVP module scope:

| Module | MVP behavior |
|---|---|
| `GGReplicaCoreVisualModule` | Dodge ghost/half silhouette, core scale/alpha, simple shell glow color response. |
| `GGReplicaBoostVisualModule` | Existing boost audio plus visible particle/trail toggle; do not rely on failed DevX shaders. |
| `GGReplicaShapeVisualModule` | Show/hide grab hands and apply Grab offset/state pack. |

Fluxy, teleport, hold, damage overlay, cloak, and full shape force fields remain future enhancements.

---

## 9. Editor tooling

### 9.1 Importer

Extend `GGReplicaAssetImporter` to import the missing skin sprites and update audit docs.

### 9.2 Profile builder

Add an editor tool or extend existing builder to create/fill `GGReplicaPlayerSkinSO` from imported assets.

### 9.3 Prefab builder

Update `GGReplicaPrefabBuilder` to:

- Build `GGPlayerViewRoot` hierarchy.
- Add fixed layer nodes.
- Wire `GGReplicaPlayerViewAdapter` and modules.
- Keep old simplified adapter off the final prefab or mark it legacy-only.

### 9.4 Auditor

Update `GGReplicaAuditor` to validate:

- full state table exists for states `0,1,2,3,4,5,6,7,8,9,15`.
- fixed skin fields are assigned.
- prefab contains required visual nodes.
- live `Ship.prefab` and `SampleScene.unity` remain unpolluted.

---

## 10. Testing strategy

Add tests before implementation:

1. `GGReplicaPlayerSkinTests`
   - table contains expected ViewState values.
   - Dodge pack permits null sprites.
   - fixed fields are assigned.
2. `GGReplicaPlayerViewAdapterTests`
   - `ChangeViewState(1)` applies Boost sprites.
   - `ChangeViewState(2)` does not clear body sprites.
   - `ChangeViewState(7)` applies Grab sprites and y offset.
   - `ChangeViewState(4)` uses instant transition path.
3. `GGReplicaPrefabBuilderTests`
   - prefab contains all required nodes and modules.
4. `GGReplicaAuditorTests`
   - audit passes after builder/import/profile setup.
5. Play Mode scene validation
   - UI buttons call `ChangeViewState(int)` values, not simplified enum labels.

---

## 11. Migration strategy

1. Keep current `Ship_GGReplica.prefab` as a prototype until the new builder is ready.
2. Introduce new data/classes alongside current ones.
3. Once tests pass, update builder to output the new hierarchy.
4. Remove or mark current `GGReplicaShipViewAdapter` as `Legacy Prototype` in registry/docs.
5. Rebuild `GGReplicaShipTest.unity` with integer ViewState controls.

---

## 12. Done definition

This rebuild phase is done when:

1. `GGReplica_PlayerView_DeepAudit.md` and this design are both present.
2. Missing skin sprites are imported and documented.
3. `GGReplicaPlayerSkinSO` represents original `PlayerSkinDefault` data, including null Dodge body sprites.
4. `Ship_GGReplica.prefab` has the full MVP visual hierarchy.
5. Test scene can trigger original ViewState ints: `0,1,2,3,4,5,6,7,8,9,15`.
6. Automated tests prove Dodge does not behave like a normal body sprite pack.
7. Auditor passes and confirms `Ship.prefab` / `SampleScene.unity` are not polluted.
8. Play Mode visual check shows more than body texture swapping: fixed layers, grab layer, dodge ghost/core effects, and state offsets are visible.

---

## 13. Open questions for implementation plan

1. Should the current simplified `GGReplicaShipViewAdapter` be kept as a legacy debug component or replaced outright?
2. Should `GGReplicaPlayerSkinSO` absorb audio references, or should audio stay in the existing visual profile during migration?
3. How far should MVP Dodge go: ghost/scale/alpha only, or also half silhouette and core shell glow?
4. Should `View` silhouette be visible by default or only in a debug/outline mode?

Recommended defaults for plan:

- Replace current simplified view adapter in the final prefab, but keep the file until migration is stable.
- Keep audio references where they are for this phase; do not churn unrelated adapters.
- MVP Dodge = ghost + half silhouette + scale/alpha only.
- Add `View` silhouette node but allow it to be toggled in the test UI.
