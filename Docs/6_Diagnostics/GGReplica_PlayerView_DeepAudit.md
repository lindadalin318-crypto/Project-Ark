# GGReplica PlayerView Deep Audit

> Date: 2026-05-14 01:29  
> Scope: Galactic Glitch original player ship visual state chain and why the current GGReplica five-state sprite switcher is not enough.  
> Source root: `/Users/dada/Documents/Quark/ReferenceAssets/Galactic Glitch`  
> Project target: `Ship_GGReplica.prefab` remains an isolated experiment, not live `Ship.prefab`.

---

## 1. Conclusion

The original Galactic Glitch ship visual system is not a direct hotkey-to-sprite-pack switcher.

The real chain is:

```text
PlayerShipState
→ Player.controller Animator state
→ AnimationClip time=0 event: ChangeViewState(int)
→ Player.ChangeViewState(PlayerView.ViewState)
→ PlayerView.ChangeState(...)
→ PlayerView.ChangeViewSprite(...)
→ PlayerSkinDefault.stateToSpritesTable
→ shipSolidSR / shipLiquidSR / shipHLSR
→ PlayerView modules add Boost / Dodge / core / grab / trail / shape-change effects
```

Current `GGReplicaShipViewAdapter` only does the middle part: it swaps Solid/Liquid/Highlight sprites. It misses the original system's state indirection, fixed layers, ViewState table, module side effects, and several visual states.

---

## 2. Primary source files

| Purpose | Source path | Notes |
|---|---|---|
| Player logic bridge | `Scripts_dnSpyEx/GeneralAssembly_direct/GeneralAssembly/Player.cs` | Contains `ChangeViewState(PlayerView.ViewState)` method signature. |
| Visual coordinator | `Scripts_dnSpyEx/GeneralAssembly_direct/GeneralAssembly/PlayerView.cs` | Holds modules, renderers, `ViewState`, `ShipSpritePack`, `StateToAnimator`. |
| Skin data type | `Scripts_dnSpyEx/GeneralAssembly_direct/GeneralAssembly/PlayerSkin.cs` | Holds `stateToSpritesTable`, fixed sprites, colors. |
| Core visual module | `Scripts_dnSpyEx/GeneralAssembly_direct/GeneralAssembly/PlayerViewCoreModule.cs` | Handles core/dodge/weapon/pupil/shell fields. |
| Boost visual module | `Scripts_dnSpyEx/GeneralAssembly_direct/GeneralAssembly/PlayerViewBoostModule.cs` | Handles boost particle arrays and boost events. |
| Shape-change module | `Scripts_dnSpyEx/GeneralAssembly_direct/GeneralAssembly/PlayerViewModules/PlayerViewShapeChangeModule.cs` | Handles state-dependent force fields, orbit, shape particles, grav gun. |
| Shape-change config | `Scripts_dnSpyEx/GeneralAssembly_direct/GeneralAssembly/PlayerViewModules/PlayerViewShapeChangeSettings.cs` | transition / gravity / emission parameters. |
| Player prefab | `DevXUnity_exported/Assets/Prefab/Player.prefab` | Renderer hierarchy and serialized references. |
| Animator controller | `DevXUnity_exported/Assets/AnimatorController/Player.controller` | Player state machine. |
| Animation clips | `DevXUnity_exported/Assets/AnimationClip/*.anim` | Contain `ChangeViewState(int)` events. |
| Skin asset | `DevXUnity_exported/Assets/MonoBehaviour/PlayerSkinDefault.asset` | Serialized `PlayerSkinDefault`. |
| Existing analysis | `Docs/7_Reference/GameAnalysis/GalacticGlitch_Structure_Analysis.md` | Already contains resolved sprite mapping table. |

---

## 3. Evidence: Animator events drive view state

The exported animation clips contain time-zero events calling `ChangeViewState` with an integer view-state parameter:

| AnimationClip | Event | ViewState int | Meaning |
|---|---|---:|---|
| `IdleBlue.anim` | `ChangeViewState` | 0 | Idle / Normal |
| `IdleRed.anim` | `ChangeViewState` | 0 | Idle / Normal variant uses same view state |
| `BoostState.anim` | `ChangeViewState` | 1 | Boost |
| `DodgeBlue.anim` | `ChangeViewState` | 2 | Dodge |
| `MainAttackState.anim` | `ChangeViewState` | 3 | Aim / Primary aim |
| `MainAttackFireState.anim` | `ChangeViewState` | 4 | Fire / Primary fire |
| `SecondaryAttackFireState.anim` | `ChangeViewState` | 5 | HeavyFire |
| `SecondaryAttackState.anim` | `ChangeViewState` | 6 | HeavyAim |
| `SurroundingGravGunState.anim` | `ChangeViewState` | 7 | Grab / GravGun |

Representative YAML evidence:

```yaml
# DevXUnity_exported/Assets/AnimationClip/BoostState.anim
m_Events:
- time: 0
  functionName: ChangeViewState
  intParameter: 1
```

```yaml
# DevXUnity_exported/Assets/AnimationClip/MainAttackFireState.anim
m_Events:
- time: 0
  functionName: ChangeViewState
  intParameter: 4
```

Implication: the test UI should not be the canonical model. It should call a GG-like `ChangeViewState(int)` API that mirrors Animator events.

---

## 4. Evidence: PlayerView state model

`PlayerView.cs` defines the real visual state enum:

```csharp
public enum ViewState
{
    Idle,
    Boost,
    Dodge,
    Aim,
    Fire,
    HeavyFire,
    HeavyAim,
    Grab,
    WeaponUseMoment,
    Heal,
    Undefined = 15
}
```

Important fields in `PlayerView.cs`:

```csharp
private PlayerViewCoreModule coreModule;
private PlayerViewEnergyModule energyModule;
private PlayerViewShapeTrailModule shapeTrailModule;
private PlayerViewFluxyTrailModule fluxyTrailModule;
private PlayerViewFluxyGrabModule fluxyGrabModule;
private PlayerViewLQTrailModule particleTrailModule;
private PlayerViewTeleportModule teleportModule;
private PlayerViewBoostModule boostModule;
private Transform sprites;
private SpriteRenderer mapSR;
private SpriteRenderer shipSolidSR;
private SpriteRenderer shipLiquidSR;
private SpriteRenderer shipHLSR;
private SpriteRenderer shipSpriteSolidGrab_R;
private SpriteRenderer shipSpriteSolidGrab_L;
private SpriteRenderer shipSpriteBack;
private List<PlayerView.StateToAnimator> stateAnimators;
private Dictionary<PlayerView.ViewState, PlayerView.ShipSpritePack> stateToSpritePackDictionary;
private PlayerView.ViewState currentState;
private Sequence stateFadeSequence;
```

`PlayerView.ShipSpritePack` contains:

```csharp
public PlayerView.ViewState state;
public float fadeDuration;
public Sprite solidSprite;
public Sprite liquidSprite;
public Sprite highlightSprite;
public Vector3 spritesOffset;
```

Implication: current `GGReplicaVisualState` is too small and should be replaced or wrapped by a GG-aligned `GGReplicaViewState` preserving original int values.

---

## 5. Evidence: PlayerSkinDefault sprite table

Resolved from existing structure analysis (`Docs/7_Reference/GameAnalysis/GalacticGlitch_Structure_Analysis.md`):

| ViewState int | Meaning | fadeDuration | solidSprite | liquidSprite | highlightSprite | spritesOffset |
|---:|---|---:|---|---|---|---|
| 0 | Idle / Normal | 0.2 | `Movement_10` | `Movement_3` | `Movement_21` | `(0,0,0)` |
| 15 | Undefined / Normal fallback | 0.2 | `Movement_10` | `Movement_3` | `Movement_21` | `(0,0,0)` |
| 1 | Boost | 0.2 | `Boost_2` | `Boost_16` | `Boost_8` | `(0,0,0)` |
| 2 | Dodge | 0.2 | `null` | `null` | `null` | `(0,0,0)` |
| 3 | Aim / Primary | 0.2 | `Primary_4` | `Primary` | `Primary_6` | `(0,0,0)` |
| 4 | Fire / Primary fire | 0 | `Primary_4` | `Primary` | `Primary_6` | `(0,0,0)` |
| 8 | WeaponUseMoment / Primary+X | 0 | `Primary_4` | `Primary` | `Primary_6` | `(0,0,0)` |
| 5 | HeavyFire / Secondary fire | 0.2 | `Secondary_8` | `Secondary_0` | `Secondary_17` | `(0,0,0)` |
| 6 | HeavyAim / Secondary aim | 0.2 | `Secondary_8` | `Secondary_0` | `Secondary_17` | `(0,0,0)` |
| 7 | Grab / GravGun | 0 | `GrabGun_Base_9` | `GrabGun_Base_9` | `GrabGun_Base_8` | `(0,-0.1,0)` |
| 9 | Heal | 0.2 | `Healing_0` | `Healing` | `vfx_dot_001` | `(0,0,0)` |

Key correction: Dodge is a `ViewState`, but its sprite pack is intentionally empty. Dodge visuals come from modules, not a normal body sprite pack.

---

## 6. Evidence: fixed skin fields and colors

`PlayerSkin.cs` contains:

```csharp
public List<PlayerView.ShipSpritePack> stateToSpritesTable;
public Sprite shipSpriteSolidGrab_R;
public Sprite shipSpriteSolidGrab_L;
public Sprite shipSpriteBack;
public Color shipHLSR;
public Color transitionColor;
public Color energyModuleReadyIdleWaveColorMin;
public Color energyModuleReadyIdleWaveColorMax;
public Color energyModuleReadyIdleGlowColorMin;
public Color energyModuleReadyIdleGlowColorMax;
```

Existing resolved values:

| Field | Value | Purpose |
|---|---|---|
| `shipSpriteSolidGrab_R` | `GrabGun_Hand_7` | right grab-hand attachment |
| `shipSpriteSolidGrab_L` | `GrabGun_Hand_7` | left grab-hand attachment |
| `shipSpriteBack` | `GrabGun_Back_3` | shared rear/back thruster layer |
| `shipHLSR` | `#8B17FF` | highlight renderer tint |
| `transitionColor` | `#AB00FF` | transition color |

Implication: the current profile must be expanded beyond per-state body sprites. It needs fixed skin fields and colors.

---

## 7. Evidence: Player prefab visual hierarchy

Existing analysis resolves the important renderers:

| Render order | GO name | Purpose |
|---|---|---|
| bottom | `Ship_Sprite_Liquid` | liquid/energy/glow body layer |
| above | `Ship_Sprite_HL` | highlight/edge light |
| above | `Dodge_Sprite` | dodge outline / ghost |
| above | `Ship_Sprite_Solid` | opaque body |
| above | `Ship_Sprite_Back` | shared rear/thruster layer |
| above | `Ship_Sprite_Solid_Grab_R` | right grab-hand attachment |
| above | `Core_Sprite (Reactor)` | reactor/core icon |
| above | `Core_Sprite (Living Eye)` | eye/lens |
| top/reference | `View` | black overhead silhouette / scheme layer |

Current `Ship_GGReplica.prefab` has only part of this model and lacks the GG-specific grab hand, living eye, and top view/scheme layer.

---

## 8. Evidence: modules add non-sprite-pack visual behavior

`PlayerView.cs` references multiple modules, not only renderers:

| Module | Evidence field | Likely role for replica |
|---|---|---|
| `PlayerViewCoreModule` | `coreModule` | core shell glow, dodge, weapon startup, pupil/eye motion |
| `PlayerViewBoostModule` | `boostModule` | boost particles activated on boost events |
| `PlayerViewShapeChangeModule` | `shapeTrailModule` / module file | shape-change force fields / orbit / shape particles |
| `PlayerViewFluxyTrailModule` | `fluxyTrailModule` | high-end boost trail / fluid trail |
| `PlayerViewFluxyGrabModule` | `fluxyGrabModule` | grav gun visuals |
| `PlayerViewLQTrailModule` | `particleTrailModule` | low quality trail fallback |

`PlayerViewCoreModule.cs` fields include:

```csharp
private SpriteRenderer coreSR;
private float shellGlow;
private float shellFade;
private float dodgeFadeInTime;
private float dodgeFadeOutTime;
private float dodgeScale;
private float dodgeScaleInDuration;
private float dodgeScaleOutDuration;
private ParticleSystem dodgeReplenishEffect;
private ParticleSystem cloakEffect;
private bool pupilApplyAimAssist;
private float pupilDamp;
private float pupilScanDamp;
private float squeezeDamp;
private Transform customTarget;
```

`PlayerViewBoostModule.cs` fields include:

```csharp
[SerializeField] private ParticleSystem[] particles;
private Player player;
private bool isActive;
```

`PlayerViewShapeChangeModule.cs` fields include:

```csharp
public PlayerViewShapeChangeSettings config;
public ParticleSystemForceField[] forceFieldsForStates;
public ParticleSystem orbit;
public ParticleSystem shape;
public ParticleSystemForceField gravityGunField;
public SurroundingGravGun gravGun;
```

Implication: a credible replica must implement at least an MVP subset of module behavior, especially core/dodge, boost particles/trail, and grab/shape layers. Pure sprite switching will never feel like GG.

---

## 9. Missing / currently incomplete asset coverage

Current curated import covered only Normal, Boost, Primary, back, reactor, dodge ghost, and a few shader trial files.

Additional source assets confirmed available under `DevXUnity/Sprite/`:

| Needed for | Source file |
|---|---|
| Secondary solid | `Secondary_d8.png` |
| Secondary liquid | likely `Secondary.png` or `Secondary_d0` equivalent; existing mapping says `Secondary_0` |
| Secondary highlight | `Secondary_d17.png` |
| Healing solid | `Healing_d1.png` likely maps to `Healing_0` depending AssetRipper naming |
| Healing liquid | `Healing.png` |
| Healing highlight | `vfx_dot_001.png` |
| Grab solid/liquid | `GrabGun_Base_d9.png` |
| Grab highlight | `GrabGun_Base_d8.png` |
| Grab hand | `GrabGun_Hand_d7.png` |
| Back | `GrabGun_back_d3.png` already imported as `GrabGun_Back_3.png` |
| View silhouette | `scheme3_tp.png` |
| Dodge half silhouette | `SHIP_PLAYER_DODGE_HALF.png` |
| Reactor | `reactor.png` already imported |

Naming note: `DevXUnity/Sprite` often uses `_dN` physical names while Project Ark imported normalized names like `Boost_2.png`. The next importer must explicitly map source physical names to Project Ark canonical names.

---

## 10. Current GGReplica gap list

| Area | Current state | Original GG expectation | Gap |
|---|---|---|---|
| State enum | `Normal/Boost/Dodge/Fire/FireBoost` | `Idle/Boost/Dodge/Aim/Fire/HeavyFire/HeavyAim/Grab/WeaponUseMoment/Heal/Undefined` | Missing states and original int values |
| Trigger model | test UI directly forces enum | Animator clip events call `ChangeViewState(int)` | Wrong public API shape |
| Dodge | Normal pack + ghost toggle | ViewState 2 sprite pack is null; core/dodge module handles visuals | Mis-modeled Dodge |
| Sprite table | 5 authored packs | full `PlayerSkinDefault.stateToSpritesTable` | Missing Secondary/Grab/Heal/Undefined |
| Fixed layers | Back/Core/Dodge only | back + grab hands + core + living eye + view silhouette | Missing layers |
| Colors | default renderer colors | `shipHLSR`, `transitionColor`, energy colors | Missing skin colors |
| Modules | simple adapters | core/boost/shape/fluxy/lq/teleport/hold modules | Missing module effects |
| Materials/shaders | mostly live Ark materials | PlayerShipHL / Sprite-Lit / TeleportScheme and shader behavior | Not replicated |

---

## 11. Recommended next action

Do not continue adding behavior to the current simplified `GGReplicaShipViewAdapter` as the final path.

Next should be a rebuild design and implementation plan that:

1. Keeps existing `Ship_GGReplica.prefab` as a disposable prototype/reference.
2. Introduces GG-aligned view-state data and `ChangeViewState(int)` API.
3. Imports missing `Secondary`, `Grab`, `Healing`, `GrabHand`, `scheme3_tp`, and dodge silhouette assets.
4. Rebuilds the prefab visual hierarchy closer to `PlayerView`.
5. Adds MVP versions of core/dodge/boost/shape modules rather than pretending all changes are sprite swaps.
