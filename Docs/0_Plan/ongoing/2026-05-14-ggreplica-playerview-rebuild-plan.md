# GGReplica PlayerView Rebuild Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rebuild the isolated `Ship_GGReplica.prefab` visual lane from a simplified five-state sprite switcher into a GG-like `PlayerView + PlayerSkin + ChangeViewState(int)` MVP.

**Architecture:** Keep the replica lane isolated: new GG-like data/model code lives under `Assets/Scripts/Ship/GGReplica/`, editor builders live under `Assets/Scripts/Ship/Editor/GGReplica/`, assets stay under `Assets/_Art/Ship/GGReplica/`, and only `Ship_GGReplica.prefab` consumes them. The current `GGReplicaShipViewAdapter` remains as legacy prototype code during migration, but the rebuilt prefab must use `GGReplicaPlayerViewAdapter` and module components instead.

**Tech Stack:** Unity 6, URP 2D, C#, ScriptableObject authored data, Unity Editor automation, Unity Test Framework, Project Ark Ship/VFX authority rules.

---

## Global constraints

- Do not modify `Assets/_Prefabs/Ship/Ship.prefab`.
- Do not modify `Assets/Scenes/SampleScene.unity`.
- Do not copy external `.meta` files.
- Do not attach original GG scripts/prefabs (`Player.cs`, `PlayerView.cs`, `Player.prefab`) directly.
- Do not use DevX shader trial files as runtime shaders; they remain L0/reference-only.
- Do not change live Ship/VFX authority tools except read-only inspection.
- Do not run `git commit` unless the user explicitly asks.
- Every file modification must be logged in `Docs/5_ImplementationLog/ImplementationLog_2026-05.md` before completion.

## Source roots

```text
GG_ROOT=/Users/dada/Documents/Quark/ReferenceAssets/Galactic Glitch
DEVX=$GG_ROOT/DevXUnity
DEVX_EXPORTED=$GG_ROOT/DevXUnity_exported
PROJECT=/Users/dada/Documents/GitHub/Project-Ark
```

## Key references

- `Docs/6_Diagnostics/GGReplica_PlayerView_DeepAudit.md`
- `Docs/0_Plan/specs/2026-05-14-ggreplica-playerview-rebuild-design.md`
- `Docs/7_Reference/GameAnalysis/GalacticGlitch_Structure_Analysis.md`
- `Docs/2_TechnicalDesign/Ship/ShipVFX_AssetRegistry.md`
- Legacy prototype plan: `Docs/0_Plan/ongoing/2026-05-13-ggreplica-ship-migration-plan.md`

## File structure map

### New runtime files

- Create `Assets/Scripts/Ship/GGReplica/GGReplicaPlayerSkinSO.cs`
  - Defines `GGReplicaViewState`, `GGReplicaViewSpritePack`, and authored fixed skin fields.
- Create `Assets/Scripts/Ship/GGReplica/GGReplicaPlayerViewAdapter.cs`
  - New visual coordinator with GG-like `ChangeViewState(int)` API.
- Create `Assets/Scripts/Ship/GGReplica/GGReplicaCoreVisualModule.cs`
  - MVP dodge ghost / half silhouette / core alpha-scale response.
- Create `Assets/Scripts/Ship/GGReplica/GGReplicaBoostVisualModule.cs`
  - MVP boost trail/particle visibility bridge.
- Create `Assets/Scripts/Ship/GGReplica/GGReplicaShapeVisualModule.cs`
  - MVP grab hand visibility and shape layer behavior.

### Existing runtime files to modify

- Modify `Assets/Scripts/Ship/GGReplica/GGReplicaTestSwitcher.cs`
  - Replace five-state controls with original ViewState int controls.
- Keep `Assets/Scripts/Ship/GGReplica/GGReplicaShipViewAdapter.cs`
  - Legacy prototype only; do not wire it in rebuilt prefab.
- Keep `Assets/Scripts/Ship/GGReplica/GGReplicaShipVisualProfileSO.cs`
  - Legacy/audio bridge for this phase; do not churn existing audio adapters unless necessary.

### New/modified editor files

- Modify `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaAssetImporter.cs`
  - Add missing Secondary / Grab / Heal / View / Dodge-half sprite entries.
- Create `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaPlayerSkinAssetBuilder.cs`
  - Builds `Assets/_Data/Ship/GGReplicaPlayerSkin.asset` from imported sprites.
- Modify `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaPrefabBuilder.cs`
  - Build `GGPlayerViewRoot` hierarchy and wire new adapter/modules.
- Modify `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaTestSceneBuilder.cs`
  - Wire switcher to `GGReplicaPlayerViewAdapter`.
- Modify `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaAuditor.cs`
  - Validate full state table, fixed skin fields, required nodes/modules, and isolation.

### Tests

- Create `Assets/Scripts/Ship/Tests/GGReplicaPlayerSkinTests.cs`
- Create `Assets/Scripts/Ship/Tests/GGReplicaPlayerViewAdapterTests.cs`
- Create `Assets/Scripts/Ship/Tests/GGReplicaVisualModuleTests.cs`
- Create `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaPlayerSkinAssetBuilderTests.cs`
- Modify existing builder/switcher/auditor tests.

### Assets/docs generated or updated by tasks

- Create `Assets/_Data/Ship/GGReplicaPlayerSkin.asset`
- Modify `Assets/_Prefabs/Ship/Ship_GGReplica.prefab`
- Modify `Assets/Scenes/GGReplicaShipTest.unity`
- Modify `Docs/6_Diagnostics/GGReplica_ShipAsset_Audit.md`
- Modify `Docs/2_TechnicalDesign/Ship/ShipVFX_AssetRegistry.md`
- Modify `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

---

## Task 0: Baseline verification and asset source expansion

**Files:**
- Modify: `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaAssetImporter.cs`
- Modify: `Docs/6_Diagnostics/GGReplica_ShipAsset_Audit.md`

- [ ] **Step 0.1: Verify current project compiles before changes**

Run:

```bash
cd /Users/dada/Documents/GitHub/Project-Ark && dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly
```

Expected: build succeeds with `0 Error(s)`.

- [ ] **Step 0.2: Verify missing GG source sprites exist**

Run:

```bash
cd /Users/dada/Documents/GitHub/Project-Ark && python3 - << 'PY'
from pathlib import Path
root = Path('/Users/dada/Documents/Quark/ReferenceAssets/Galactic Glitch')
files = [
 'DevXUnity/Sprite/Secondary_d8.png',
 'DevXUnity/Sprite/Secondary.png',
 'DevXUnity/Sprite/Secondary_d17.png',
 'DevXUnity/Sprite/Healing_d1.png',
 'DevXUnity/Sprite/Healing.png',
 'DevXUnity/Sprite/vfx_dot_001.png',
 'DevXUnity/Sprite/GrabGun_Base_d9.png',
 'DevXUnity/Sprite/GrabGun_Base_d8.png',
 'DevXUnity/Sprite/GrabGun_Hand_d7.png',
 'DevXUnity/Sprite/scheme3_tp.png',
 'DevXUnity/Sprite/SHIP_PLAYER_DODGE_HALF.png',
]
missing = [f for f in files if not (root / f).exists()]
print('MISSING:', missing)
raise SystemExit(1 if missing else 0)
PY
```

Expected: `MISSING: []`. If one file is missing, stop and update the audit doc with the exact missing path; do not fuzzy-search a substitute silently.

- [ ] **Step 0.3: Extend curated sprite entries**

In `GGReplicaAssetImporter.cs`, append these entries to `SpriteEntries`:

```csharp
new CopyEntry("DevXUnity/Sprite/Secondary_d8.png", "Sprites/Secondary_8.png", DefaultShipPPU, new Vector2(0.5f, 0.5f)),
new CopyEntry("DevXUnity/Sprite/Secondary.png", "Sprites/Secondary_0.png", DefaultShipPPU, new Vector2(0.5f, 0.5f)),
new CopyEntry("DevXUnity/Sprite/Secondary_d17.png", "Sprites/Secondary_17.png", DefaultShipPPU, new Vector2(0.5f, 0.5f)),
new CopyEntry("DevXUnity/Sprite/Healing_d1.png", "Sprites/Healing_0.png", DefaultShipPPU, new Vector2(0.5f, 0.5f)),
new CopyEntry("DevXUnity/Sprite/Healing.png", "Sprites/Healing.png", DefaultShipPPU, new Vector2(0.5f, 0.5f)),
new CopyEntry("DevXUnity/Sprite/vfx_dot_001.png", "Sprites/vfx_dot_001.png", DefaultShipPPU, new Vector2(0.5f, 0.5f)),
new CopyEntry("DevXUnity/Sprite/GrabGun_Base_d9.png", "Sprites/GrabGun_Base_9.png", DefaultShipPPU, new Vector2(0.5f, 0.5f)),
new CopyEntry("DevXUnity/Sprite/GrabGun_Base_d8.png", "Sprites/GrabGun_Base_8.png", DefaultShipPPU, new Vector2(0.5f, 0.5f)),
new CopyEntry("DevXUnity/Sprite/GrabGun_Hand_d7.png", "Sprites/GrabGun_Hand_7.png", DefaultShipPPU, new Vector2(0.5f, 0.5f)),
new CopyEntry("DevXUnity/Sprite/scheme3_tp.png", "Sprites/scheme3_tp.png", DefaultShipPPU, new Vector2(0.5f, 0.5f)),
new CopyEntry("DevXUnity/Sprite/SHIP_PLAYER_DODGE_HALF.png", "Sprites/SHIP_PLAYER_DODGE_HALF.png", DefaultShipPPU, new Vector2(0.5f, 0.5f))
```

- [ ] **Step 0.4: Run importer**

Use Unity menu or MCP:

```text
ProjectArk > Ship > GG Replica > Import Curated Assets
```

Expected Unity Console line:

```text
[GGReplicaAssetImporter] Curated GGReplica assets imported successfully.
```

- [ ] **Step 0.5: Update asset audit document**

In `Docs/6_Diagnostics/GGReplica_ShipAsset_Audit.md`, add rows for the 11 new sprite mappings and mark them `IMPORTED` after Step 0.4 succeeds.

---

## Task 1: GG-aligned skin data model

**Files:**
- Create: `Assets/Scripts/Ship/Tests/GGReplicaPlayerSkinTests.cs`
- Create: `Assets/Scripts/Ship/GGReplica/GGReplicaPlayerSkinSO.cs`

- [ ] **Step 1.1: Write failing tests for ViewState values and Dodge null-pack rule**

Create `GGReplicaPlayerSkinTests.cs` with tests named:

```csharp
[Test]
public void ViewState_IntValues_MatchOriginalGG()
{
    Assert.That((int)GGReplicaViewState.Idle, Is.EqualTo(0));
    Assert.That((int)GGReplicaViewState.Boost, Is.EqualTo(1));
    Assert.That((int)GGReplicaViewState.Dodge, Is.EqualTo(2));
    Assert.That((int)GGReplicaViewState.Aim, Is.EqualTo(3));
    Assert.That((int)GGReplicaViewState.Fire, Is.EqualTo(4));
    Assert.That((int)GGReplicaViewState.HeavyFire, Is.EqualTo(5));
    Assert.That((int)GGReplicaViewState.HeavyAim, Is.EqualTo(6));
    Assert.That((int)GGReplicaViewState.Grab, Is.EqualTo(7));
    Assert.That((int)GGReplicaViewState.WeaponUseMoment, Is.EqualTo(8));
    Assert.That((int)GGReplicaViewState.Heal, Is.EqualTo(9));
    Assert.That((int)GGReplicaViewState.Undefined, Is.EqualTo(15));
}

[Test]
public void TryGetPack_DodgePack_AllowsNullBodySprites()
{
    var skin = ScriptableObject.CreateInstance<GGReplicaPlayerSkinSO>();
    SetPrivateField(skin, "_stateToSpritesTable", new[]
    {
        new GGReplicaViewSpritePack { State = GGReplicaViewState.Dodge, FadeDuration = 0.2f }
    });

    Assert.That(skin.TryGetPack(GGReplicaViewState.Dodge, out var pack), Is.True);
    Assert.That(pack.SolidSprite, Is.Null);
    Assert.That(pack.LiquidSprite, Is.Null);
    Assert.That(pack.HighlightSprite, Is.Null);

    Object.DestroyImmediate(skin);
}
```

Include a local `SetPrivateField(object target, string name, object value)` helper using reflection, matching the style in existing GGReplica tests.

- [ ] **Step 1.2: Run tests and verify RED**

Run Unity PlayMode test filter:

```text
ProjectArk.Ship.Tests.GGReplicaPlayerSkinTests
```

Expected: compile/test failure because `GGReplicaViewState`, `GGReplicaViewSpritePack`, and `GGReplicaPlayerSkinSO` do not exist.

- [ ] **Step 1.3: Implement minimal data model**

Create `GGReplicaPlayerSkinSO.cs`:

```csharp
using System;
using UnityEngine;

namespace ProjectArk.Ship
{
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

    [Serializable]
    public sealed class GGReplicaViewSpritePack
    {
        public GGReplicaViewState State;
        [Min(0f)] public float FadeDuration = 0.2f;
        public Sprite SolidSprite;
        public Sprite LiquidSprite;
        public Sprite HighlightSprite;
        public Vector3 SpritesOffset;
    }

    [CreateAssetMenu(fileName = "GGReplicaPlayerSkin", menuName = "ProjectArk/Ship/GG Replica/Player Skin")]
    public sealed class GGReplicaPlayerSkinSO : ScriptableObject
    {
        [SerializeField] private GGReplicaViewSpritePack[] _stateToSpritesTable = Array.Empty<GGReplicaViewSpritePack>();
        [SerializeField] private Sprite _shipSpriteSolidGrabR;
        [SerializeField] private Sprite _shipSpriteSolidGrabL;
        [SerializeField] private Sprite _shipSpriteBack;
        [SerializeField] private Sprite _reactorSprite;
        [SerializeField] private Sprite _eyeSprite;
        [SerializeField] private Sprite _viewSilhouetteSprite;
        [SerializeField] private Sprite _dodgeSprite;
        [SerializeField] private Sprite _dodgeHalfSprite;
        [SerializeField] private Color _shipHighlightColor = new Color(0.545f, 0.09f, 1f, 1f);
        [SerializeField] private Color _transitionColor = new Color(0.671f, 0f, 1f, 1f);

        public Sprite ShipSpriteSolidGrabR => _shipSpriteSolidGrabR;
        public Sprite ShipSpriteSolidGrabL => _shipSpriteSolidGrabL;
        public Sprite ShipSpriteBack => _shipSpriteBack;
        public Sprite ReactorSprite => _reactorSprite;
        public Sprite EyeSprite => _eyeSprite;
        public Sprite ViewSilhouetteSprite => _viewSilhouetteSprite;
        public Sprite DodgeSprite => _dodgeSprite;
        public Sprite DodgeHalfSprite => _dodgeHalfSprite;
        public Color ShipHighlightColor => _shipHighlightColor;
        public Color TransitionColor => _transitionColor;
        public IReadOnlyList<GGReplicaViewSpritePack> StateToSpritesTable => _stateToSpritesTable;

        public bool TryGetPack(GGReplicaViewState state, out GGReplicaViewSpritePack pack)
        {
            foreach (var candidate in _stateToSpritesTable)
            {
                if (candidate != null && candidate.State == state)
                {
                    pack = candidate;
                    return true;
                }
            }

            pack = null;
            return false;
        }
    }
}
```

Add `using System.Collections.Generic;` if needed for `IReadOnlyList`.

- [ ] **Step 1.4: Run tests and verify GREEN**

Run:

```text
ProjectArk.Ship.Tests.GGReplicaPlayerSkinTests
```

Expected: tests pass.

---

## Task 2: PlayerSkin asset builder

**Files:**
- Create: `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaPlayerSkinAssetBuilder.cs`
- Create: `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaPlayerSkinAssetBuilderTests.cs`
- Create/update asset: `Assets/_Data/Ship/GGReplicaPlayerSkin.asset`

- [ ] **Step 2.1: Write failing builder test**

Create test `BuildPlayerSkinAsset_CreatesFullStateTableAndFixedFields`:

```csharp
[Test]
public void BuildPlayerSkinAsset_CreatesFullStateTableAndFixedFields()
{
    GGReplicaPlayerSkinAssetBuilder.BuildPlayerSkinAsset();

    var skin = AssetDatabase.LoadAssetAtPath<GGReplicaPlayerSkinSO>("Assets/_Data/Ship/GGReplicaPlayerSkin.asset");
    Assert.That(skin, Is.Not.Null);

    var expected = new[]
    {
        GGReplicaViewState.Idle, GGReplicaViewState.Boost, GGReplicaViewState.Dodge,
        GGReplicaViewState.Aim, GGReplicaViewState.Fire, GGReplicaViewState.HeavyFire,
        GGReplicaViewState.HeavyAim, GGReplicaViewState.Grab, GGReplicaViewState.WeaponUseMoment,
        GGReplicaViewState.Heal, GGReplicaViewState.Undefined
    };

    foreach (var state in expected)
    {
        Assert.That(skin.TryGetPack(state, out _), Is.True, $"Missing pack for {state}.");
    }

    Assert.That(skin.TryGetPack(GGReplicaViewState.Dodge, out var dodge), Is.True);
    Assert.That(dodge.SolidSprite, Is.Null);
    Assert.That(dodge.LiquidSprite, Is.Null);
    Assert.That(dodge.HighlightSprite, Is.Null);

    Assert.That(skin.ShipSpriteSolidGrabR, Is.Not.Null);
    Assert.That(skin.ShipSpriteSolidGrabL, Is.Not.Null);
    Assert.That(skin.ShipSpriteBack, Is.Not.Null);
    Assert.That(skin.ReactorSprite, Is.Not.Null);
    Assert.That(skin.ViewSilhouetteSprite, Is.Not.Null);
    Assert.That(skin.DodgeSprite, Is.Not.Null);
    Assert.That(skin.DodgeHalfSprite, Is.Not.Null);
}
```

- [ ] **Step 2.2: Verify RED**

Run EditMode test filter:

```text
ProjectArk.Ship.Editor.GGReplicaPlayerSkinAssetBuilderTests
```

Expected: compile/test failure because builder does not exist.

- [ ] **Step 2.3: Implement builder**

Create menu `ProjectArk/Ship/GG Replica/Build Player Skin Asset`. Builder must:

- Load/create `Assets/_Data/Ship/GGReplicaPlayerSkin.asset`.
- Use exact sprite asset paths under `Assets/_Art/Ship/GGReplica/Sprites/`.
- Write private serialized fields via `SerializedObject`.
- Log `Debug.LogError` for missing required non-Dodge sprites.
- Keep Dodge body sprites null.

Core mapping:

```csharp
private static GGReplicaViewSpritePack[] BuildPacks()
{
    return new[]
    {
        Pack(GGReplicaViewState.Idle, 0.2f, "Movement_10.png", "Movement_3.png", "Movement_21.png", Vector3.zero),
        Pack(GGReplicaViewState.Undefined, 0.2f, "Movement_10.png", "Movement_3.png", "Movement_21.png", Vector3.zero),
        Pack(GGReplicaViewState.Boost, 0.2f, "Boost_2.png", "Boost_16.png", "Boost_8.png", Vector3.zero),
        new GGReplicaViewSpritePack { State = GGReplicaViewState.Dodge, FadeDuration = 0.2f, SpritesOffset = Vector3.zero },
        Pack(GGReplicaViewState.Aim, 0.2f, "Primary_4.png", "Primary.png", "Primary_6.png", Vector3.zero),
        Pack(GGReplicaViewState.Fire, 0f, "Primary_4.png", "Primary.png", "Primary_6.png", Vector3.zero),
        Pack(GGReplicaViewState.WeaponUseMoment, 0f, "Primary_4.png", "Primary.png", "Primary_6.png", Vector3.zero),
        Pack(GGReplicaViewState.HeavyFire, 0.2f, "Secondary_8.png", "Secondary_0.png", "Secondary_17.png", Vector3.zero),
        Pack(GGReplicaViewState.HeavyAim, 0.2f, "Secondary_8.png", "Secondary_0.png", "Secondary_17.png", Vector3.zero),
        Pack(GGReplicaViewState.Grab, 0f, "GrabGun_Base_9.png", "GrabGun_Base_9.png", "GrabGun_Base_8.png", new Vector3(0f, -0.1f, 0f)),
        Pack(GGReplicaViewState.Heal, 0.2f, "Healing_0.png", "Healing.png", "vfx_dot_001.png", Vector3.zero)
    };
}
```

Fixed fields:

```text
_shipSpriteSolidGrabR = GrabGun_Hand_7.png
_shipSpriteSolidGrabL = GrabGun_Hand_7.png
_shipSpriteBack = GrabGun_Back_3.png
_reactorSprite = reactor.png
_eyeSprite = reactor.png       # temporary MVP until a better eye source is confirmed
_viewSilhouetteSprite = scheme3_tp.png
_dodgeSprite = player_test_fire.png
_dodgeHalfSprite = SHIP_PLAYER_DODGE_HALF.png
_shipHighlightColor = #8B17FF
_transitionColor = #AB00FF
```

- [ ] **Step 2.4: Run importer then builder test**

Run menus:

```text
ProjectArk > Ship > GG Replica > Import Curated Assets
ProjectArk > Ship > GG Replica > Build Player Skin Asset
```

Then run test:

```text
ProjectArk.Ship.Editor.GGReplicaPlayerSkinAssetBuilderTests
```

Expected: test passes and `Assets/_Data/Ship/GGReplicaPlayerSkin.asset` exists.

---

## Task 3: GGReplicaPlayerViewAdapter

**Files:**
- Create: `Assets/Scripts/Ship/Tests/GGReplicaPlayerViewAdapterTests.cs`
- Create: `Assets/Scripts/Ship/GGReplica/GGReplicaPlayerViewAdapter.cs`

- [ ] **Step 3.1: Write failing adapter tests**

Create tests for these behaviors:

```csharp
[Test]
public void ChangeViewState_IntAppliesBoostSprites()
```

- Build a GameObject with `SpriteRenderer` children for `Solid`, `Liquid`, `Highlight`, `Back`, `GrabR`, `GrabL`, `Core`, `Eye`, `View`, `Dodge`, `DodgeHalf`.
- Inject a `GGReplicaPlayerSkinSO` containing Idle and Boost packs.
- Call `adapter.ChangeViewState(1)`.
- Assert solid/liquid/highlight sprites equal Boost pack sprites.

```csharp
[Test]
public void ChangeViewState_DodgeDoesNotClearExistingBodySprites()
```

- Apply Idle first.
- Apply `ChangeViewState(2)`.
- Assert body renderers still hold Idle sprites.
- Assert dodge renderers/modules become visible.

```csharp
[Test]
public void ChangeViewState_GrabAppliesGrabOffsetAndHands()
```

- Apply `ChangeViewState(7)`.
- Assert body transform/root offset is `(0,-0.1,0)`.
- Assert grab hand renderers are enabled.

- [ ] **Step 3.2: Verify RED**

Run:

```text
ProjectArk.Ship.Tests.GGReplicaPlayerViewAdapterTests
```

Expected: compile/test failure because `GGReplicaPlayerViewAdapter` does not exist.

- [ ] **Step 3.3: Implement new adapter**

Create `GGReplicaPlayerViewAdapter` with serialized fields:

```csharp
[SerializeField] private GGReplicaPlayerSkinSO _skin;
[SerializeField] private Transform _spritesRoot;
[SerializeField] private SpriteRenderer _shipLiquidRenderer;
[SerializeField] private SpriteRenderer _shipHighlightRenderer;
[SerializeField] private SpriteRenderer _dodgeRenderer;
[SerializeField] private SpriteRenderer _shipSolidRenderer;
[SerializeField] private SpriteRenderer _shipBackRenderer;
[SerializeField] private SpriteRenderer _shipGrabRightRenderer;
[SerializeField] private SpriteRenderer _shipGrabLeftRenderer;
[SerializeField] private SpriteRenderer _coreRenderer;
[SerializeField] private SpriteRenderer _eyeRenderer;
[SerializeField] private SpriteRenderer _viewSilhouetteRenderer;
[SerializeField] private SpriteRenderer _dodgeHalfRenderer;
[SerializeField] private GGReplicaCoreVisualModule _coreModule;
[SerializeField] private GGReplicaBoostVisualModule _boostModule;
[SerializeField] private GGReplicaShapeVisualModule _shapeModule;
```

Public API:

```csharp
public GGReplicaViewState CurrentState { get; private set; } = GGReplicaViewState.Undefined;
public GGReplicaViewSpritePack CurrentSpritePack { get; private set; }

public void ChangeViewState(int state) => ChangeViewState((GGReplicaViewState)state, strict: false);

public void ChangeViewState(GGReplicaViewState state, bool strict = false)
{
    if (_skin == null)
    {
        Debug.LogError("[GGReplicaPlayerViewAdapter] Missing player skin.", this);
        return;
    }

    if (!_skin.TryGetPack(state, out var pack))
    {
        string message = $"[GGReplicaPlayerViewAdapter] Missing sprite pack for {state} ({(int)state}).";
        if (strict) Debug.LogError(message, this);
        else Debug.LogWarning(message, this);
        return;
    }

    CurrentState = state;
    CurrentSpritePack = pack;
    ApplyFixedSkinFields();
    ApplySpritePack(pack);
    NotifyModules(state);
}

public GGReplicaViewSpritePack GetCurrentSpritePack() => CurrentSpritePack;
```

Sprite rules:

```csharp
private void ApplySpritePack(GGReplicaViewSpritePack pack)
{
    if (_shipSolidRenderer != null && pack.SolidSprite != null) _shipSolidRenderer.sprite = pack.SolidSprite;
    if (_shipLiquidRenderer != null && pack.LiquidSprite != null) _shipLiquidRenderer.sprite = pack.LiquidSprite;
    if (_shipHighlightRenderer != null && pack.HighlightSprite != null) _shipHighlightRenderer.sprite = pack.HighlightSprite;

    if (_spritesRoot != null) _spritesRoot.localPosition = pack.SpritesOffset;
}
```

Do not set a body renderer sprite to null when pack sprite is null.

- [ ] **Step 3.4: Run tests and verify GREEN**

Run:

```text
ProjectArk.Ship.Tests.GGReplicaPlayerViewAdapterTests
```

Expected: tests pass.

---

## Task 4: MVP visual modules

**Files:**
- Create: `Assets/Scripts/Ship/Tests/GGReplicaVisualModuleTests.cs`
- Create: `Assets/Scripts/Ship/GGReplica/GGReplicaCoreVisualModule.cs`
- Create: `Assets/Scripts/Ship/GGReplica/GGReplicaBoostVisualModule.cs`
- Create: `Assets/Scripts/Ship/GGReplica/GGReplicaShapeVisualModule.cs`

- [ ] **Step 4.1: Write failing module tests**

Add tests:

```csharp
[Test]
public void CoreModule_DodgeState_ShowsDodgeAndHalfSilhouettes()
```

Expected: `dodgeRenderer.enabled == true`, `dodgeHalfRenderer.enabled == true`, core alpha/scale visibly changes.

```csharp
[Test]
public void BoostModule_BoostState_ShowsBoostTrailRoot()
```

Expected: assigned boost visual root active on `Boost`, inactive on `Idle`.

```csharp
[Test]
public void ShapeModule_GrabState_ShowsGrabHandsOnlyForGrab()
```

Expected: right/left grab hand renderers enabled for `Grab`, disabled for `Idle`.

- [ ] **Step 4.2: Verify RED**

Run:

```text
ProjectArk.Ship.Tests.GGReplicaVisualModuleTests
```

Expected: compile/test failure because module classes do not exist.

- [ ] **Step 4.3: Implement minimal modules**

Core module API:

```csharp
public void ApplyState(GGReplicaViewState state)
{
    bool dodging = state == GGReplicaViewState.Dodge;
    SetEnabled(_dodgeRenderer, dodging);
    SetEnabled(_dodgeHalfRenderer, dodging);
    if (_coreTransform != null) _coreTransform.localScale = dodging ? Vector3.one * _dodgeCoreScale : Vector3.one;
    if (_coreRenderer != null) _coreRenderer.color = dodging ? _dodgeCoreColor : Color.white;
}
```

Boost module API:

```csharp
public void ApplyState(GGReplicaViewState state)
{
    bool boosting = state == GGReplicaViewState.Boost;
    if (_boostVisualRoot != null) _boostVisualRoot.SetActive(boosting);
    foreach (var particle in _particles)
    {
        if (particle == null) continue;
        if (boosting && !particle.isPlaying) particle.Play();
        if (!boosting && particle.isPlaying) particle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }
}
```

Shape module API:

```csharp
public void ApplyState(GGReplicaViewState state)
{
    bool grabbing = state == GGReplicaViewState.Grab;
    SetEnabled(_grabRightRenderer, grabbing);
    SetEnabled(_grabLeftRenderer, grabbing);
}
```

Keep modules deterministic and no coroutine/Update logic for MVP.

- [ ] **Step 4.4: Wire module calls from adapter and verify GREEN**

In `GGReplicaPlayerViewAdapter.NotifyModules(state)`, call:

```csharp
_coreModule?.ApplyState(state);
_boostModule?.ApplyState(state);
_shapeModule?.ApplyState(state);
```

Run:

```text
ProjectArk.Ship.Tests.GGReplicaPlayerViewAdapterTests
ProjectArk.Ship.Tests.GGReplicaVisualModuleTests
```

Expected: all pass.

---

## Task 5: Rebuild prefab hierarchy and wiring

**Files:**
- Modify: `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaPrefabBuilderTests.cs`
- Modify: `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaPrefabBuilder.cs`
- Modify/generated: `Assets/_Prefabs/Ship/Ship_GGReplica.prefab`

- [ ] **Step 5.1: Update prefab builder test first**

Modify `BuildExperimentalPrefab_CreatesReplicaWithAdapterWired` to assert:

```csharp
Assert.That(replica.GetComponent<GGReplicaShipViewAdapter>(), Is.Null, "Rebuilt prefab must not use legacy five-state adapter.");
Assert.That(replica.GetComponent<GGReplicaPlayerViewAdapter>(), Is.Not.Null);
Assert.That(replica.GetComponent<GGReplicaCoreVisualModule>(), Is.Not.Null);
Assert.That(replica.GetComponent<GGReplicaBoostVisualModule>(), Is.Not.Null);
Assert.That(replica.GetComponent<GGReplicaShapeVisualModule>(), Is.Not.Null);

var root = replica.transform.Find("ShipVisual/GGPlayerViewRoot");
Assert.That(root, Is.Not.Null);
foreach (var childName in new[]
{
    "Ship_Sprite_Liquid", "Ship_Sprite_HL", "Dodge_Sprite", "Ship_Sprite_Solid",
    "Ship_Sprite_Back", "Ship_Sprite_Solid_Grab_R", "Ship_Sprite_Solid_Grab_L",
    "Core_Sprite_Reactor", "Core_Sprite_Eye", "View", "Dodge_Half_Sprite"
})
{
    Assert.That(root.Find(childName), Is.Not.Null, $"Missing GGPlayerViewRoot/{childName}.");
}
```

Also assert `_skin` references `Assets/_Data/Ship/GGReplicaPlayerSkin.asset`.

- [ ] **Step 5.2: Verify RED**

Run:

```text
ProjectArk.Ship.Editor.GGReplicaPrefabBuilderTests
```

Expected: fails because prefab builder still wires legacy adapter and old hierarchy.

- [ ] **Step 5.3: Update builder constants and methods**

Add:

```csharp
private const string PlayerSkinPath = "Assets/_Data/Ship/GGReplicaPlayerSkin.asset";
private const string GGPlayerViewRootPath = "ShipVisual/GGPlayerViewRoot";
```

Builder flow:

```csharp
var playerSkin = AssetDatabase.LoadAssetAtPath<GGReplicaPlayerSkinSO>(PlayerSkinPath);
EnsureGGPlayerViewRoot(root, playerSkin);
EnsurePlayerViewAdapter(root, playerSkin);
EnsureVisualModules(root);
RemoveLegacyViewAdapter(root);
EnsureAudioAdapters(root, visualProfile);
EnsureFeelAdapter(root, feelProfile);
```

- [ ] **Step 5.4: Implement hierarchy builder**

`EnsureGGPlayerViewRoot` must create/find `ShipVisual/GGPlayerViewRoot` and required children. Each child needs `SpriteRenderer`. Use sorting orders that preserve original visual stack:

```text
Ship_Sprite_Liquid: 0
Ship_Sprite_HL: 1
Dodge_Sprite: 2
Ship_Sprite_Solid: 3
Ship_Sprite_Back: 4
Ship_Sprite_Solid_Grab_R: 5
Ship_Sprite_Solid_Grab_L: 5
Core_Sprite_Reactor: 6
Core_Sprite_Eye: 7
View: 8
Dodge_Half_Sprite: 9
```

Initialize renderer sprites from `GGReplicaPlayerSkinSO` fixed fields where applicable.

- [ ] **Step 5.5: Run builder and test**

Run menu:

```text
ProjectArk > Ship > GG Replica > Build Experimental Prefab
```

Run test:

```text
ProjectArk.Ship.Editor.GGReplicaPrefabBuilderTests
```

Expected: passes; `Ship_GGReplica.prefab` has new adapter/modules and no legacy `GGReplicaShipViewAdapter` component.

---

## Task 6: Test switcher and A/B scene use ViewState ints

**Files:**
- Modify: `Assets/Scripts/Ship/Tests/GGReplicaTestSwitcherTests.cs`
- Modify: `Assets/Scripts/Ship/GGReplica/GGReplicaTestSwitcher.cs`
- Modify: `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaTestSceneBuilderTests.cs`
- Modify: `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaTestSceneBuilder.cs`
- Modify/generated: `Assets/Scenes/GGReplicaShipTest.unity`

- [ ] **Step 6.1: Update tests first**

`GGReplicaTestSwitcherTests` should verify:

```csharp
[Test]
public void ForceReplicaViewState_ForwardsOriginalIntToPlayerViewAdapter()
```

Use a real `GGReplicaPlayerViewAdapter` with a test skin, call:

```csharp
switcher.ForceReplicaViewState(7);
Assert.That(adapter.CurrentState, Is.EqualTo(GGReplicaViewState.Grab));
```

`GGReplicaTestSceneBuilderTests` should assert:

```csharp
Assert.That(replica.GetComponent<GGReplicaPlayerViewAdapter>(), Is.Not.Null);
Assert.That(switcherGo.GetComponent<GGReplicaTestSwitcher>(), Is.Not.Null);
```

- [ ] **Step 6.2: Verify RED**

Run:

```text
ProjectArk.Ship.Tests.GGReplicaTestSwitcherTests
ProjectArk.Ship.Editor.GGReplicaTestSceneBuilderTests
```

Expected: fails because switcher still references `GGReplicaShipViewAdapter` and five-state enum.

- [ ] **Step 6.3: Modify switcher**

Replace serialized view field:

```csharp
[SerializeField] private GGReplicaPlayerViewAdapter _replicaView;
```

Replace force method:

```csharp
public void ForceReplicaViewState(int state)
{
    if (_replicaView != null)
    {
        _replicaView.ChangeViewState(state);
    }
}
```

Update IMGUI labels/buttons for original ints:

```text
0 Idle
1 Boost
2 Dodge
3 Aim
4 Fire
5 HeavyFire
6 HeavyAim
7 Grab
8 WeaponUseMoment
9 Heal
15 Undefined
```

Keyboard MVP:

```text
Tab/F1 toggle A/B
1-9 force states 1-9
0 force Idle
U force Undefined(15)
```

- [ ] **Step 6.4: Modify test scene builder**

Set `_replicaView` using:

```csharp
switcherSO.FindProperty("_replicaView").objectReferenceValue = replica.GetComponent<GGReplicaPlayerViewAdapter>();
```

- [ ] **Step 6.5: Run builder and tests**

Run:

```text
ProjectArk > Ship > GG Replica > Build Test Scene
```

Then:

```text
ProjectArk.Ship.Tests.GGReplicaTestSwitcherTests
ProjectArk.Ship.Editor.GGReplicaTestSceneBuilderTests
```

Expected: all pass.

---

## Task 7: Auditor upgrade and registry/docs

**Files:**
- Modify: `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaAuditorTests.cs`
- Modify: `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaAuditor.cs`
- Modify: `Docs/2_TechnicalDesign/Ship/ShipVFX_AssetRegistry.md`

- [ ] **Step 7.1: Update auditor tests first**

Extend `RunAudit_ReportsNoErrorsForIsolatedReplicaLane` to build/import everything in order:

```csharp
GGReplicaPlayerSkinAssetBuilder.BuildPlayerSkinAsset();
GGReplicaPrefabBuilder.BuildExperimentalPrefab();
GGReplicaTestSceneBuilder.BuildTestScene();
var results = GGReplicaAuditor.RunAudit(logToConsole: false);
Assert.That(results.Any(result => result.Severity == GGReplicaAuditor.Severity.Error), Is.False);
```

Add assertions no message contains:

```text
Missing GGReplicaPlayerSkin
Missing required GGPlayerViewRoot child
Live Ship.prefab has GGReplicaPlayerViewAdapter
SampleScene contains GGReplica
```

- [ ] **Step 7.2: Verify RED**

Run:

```text
ProjectArk.Ship.Editor.GGReplicaAuditorTests
```

Expected: fails because auditor does not validate new skin/hierarchy/components yet.

- [ ] **Step 7.3: Update auditor**

Add constants:

```csharp
private const string PlayerSkinPath = "Assets/_Data/Ship/GGReplicaPlayerSkin.asset";
private static readonly GGReplicaViewState[] RequiredStates =
{
    GGReplicaViewState.Idle, GGReplicaViewState.Boost, GGReplicaViewState.Dodge,
    GGReplicaViewState.Aim, GGReplicaViewState.Fire, GGReplicaViewState.HeavyFire,
    GGReplicaViewState.HeavyAim, GGReplicaViewState.Grab, GGReplicaViewState.WeaponUseMoment,
    GGReplicaViewState.Heal, GGReplicaViewState.Undefined
};
```

Validate:

- `GGReplicaPlayerSkinSO` exists.
- every required state has a pack.
- Dodge pack exists and all three body sprite fields are null.
- fixed skin fields are assigned except `_eyeSprite` may temporarily equal reactor sprite and must be Warning, not Error.
- `Ship_GGReplica.prefab` has `GGReplicaPlayerViewAdapter`, `GGReplicaCoreVisualModule`, `GGReplicaBoostVisualModule`, `GGReplicaShapeVisualModule`.
- `Ship_GGReplica.prefab` does not have `GGReplicaShipViewAdapter`.
- required `GGPlayerViewRoot` children exist.
- live `Ship.prefab` does not have any `GGReplica*` component.
- `SampleScene.unity` does not contain `Ship_GGReplica`, `GGReplicaTestSwitcher`, or `GGReplicaPlayerViewAdapter`.

- [ ] **Step 7.4: Update registry**

In `Docs/2_TechnicalDesign/Ship/ShipVFX_AssetRegistry.md`, mark:

```text
GGReplicaPlayerSkin.asset: Experimental / Reference
GGReplicaPlayerViewAdapter: Experimental runtime owner for Ship_GGReplica only
GGReplicaShipViewAdapter: Legacy Prototype, not wired by rebuilt prefab
GGPlayerViewRoot hierarchy: Experimental prefab-only
```

- [ ] **Step 7.5: Run auditor test and menu**

Run:

```text
ProjectArk.Ship.Editor.GGReplicaAuditorTests
```

Then menu:

```text
ProjectArk > Ship > GG Replica > Audit Replica Isolation
```

Expected Console:

```text
[GGReplicaAuditor] PASS: GGReplica is isolated from live Ship.prefab and SampleScene.
```

---

## Task 8: Full verification and implementation log

**Files:**
- Modify: `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- [ ] **Step 8.1: Run full GGReplica test subset**

Run Unity tests:

```text
ProjectArk.Ship.Tests.GGReplicaPlayerSkinTests
ProjectArk.Ship.Tests.GGReplicaPlayerViewAdapterTests
ProjectArk.Ship.Tests.GGReplicaVisualModuleTests
ProjectArk.Ship.Tests.GGReplicaTestSwitcherTests
ProjectArk.Ship.Editor.GGReplicaPlayerSkinAssetBuilderTests
ProjectArk.Ship.Editor.GGReplicaPrefabBuilderTests
ProjectArk.Ship.Editor.GGReplicaTestSceneBuilderTests
ProjectArk.Ship.Editor.GGReplicaAuditorTests
```

Expected: all pass.

- [ ] **Step 8.2: Run project compile**

Run:

```bash
cd /Users/dada/Documents/GitHub/Project-Ark && dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly
```

Expected: build succeeds with `0 Error(s)`.

- [ ] **Step 8.3: Rebuild assets/prefab/scene in order**

Run menus:

```text
ProjectArk > Ship > GG Replica > Import Curated Assets
ProjectArk > Ship > GG Replica > Build Player Skin Asset
ProjectArk > Ship > GG Replica > Build Experimental Prefab
ProjectArk > Ship > GG Replica > Build Test Scene
ProjectArk > Ship > GG Replica > Audit Replica Isolation
```

Expected: no errors, auditor PASS.

- [ ] **Step 8.4: Play Mode visual checklist**

Open `Assets/Scenes/GGReplicaShipTest.unity`, enter Play Mode, verify:

- Tab/F1 toggles Live vs GGReplica.
- `0/1/2/3/4/5/6/7/8/9/U` call original ViewState ints.
- Dodge `2` does not erase body sprites; dodge ghost/half/core effect appears.
- Grab `7` shows grab hands and downward offset.
- Secondary states `5/6` use Secondary sprites.
- Heal `9` uses Healing/vfx dot sprites.
- Undefined `15` falls back to normal Movement sprites.
- `Ship.prefab` and `SampleScene.unity` remain unmodified by replica tools.

- [ ] **Step 8.5: Append implementation log**

Append an entry to `Docs/5_ImplementationLog/ImplementationLog_2026-05.md` with:

```markdown
---

## GGReplica PlayerView 重建实现 — YYYY-MM-DD HH:MM

- **新建文件**
  - ...
- **修改文件**
  - ...
- **内容**：...
- **目的**：...
- **技术**：...
- **验证**：...
```

Use the actual timestamp and actual file list.

---

## Done definition

This implementation is complete when:

1. `GGReplicaPlayerSkinSO` represents original ViewState table values `0,1,2,3,4,5,6,7,8,9,15`.
2. Dodge body sprites are intentionally null in data and do not clear existing body renderer sprites at runtime.
3. `Ship_GGReplica.prefab` uses `GGReplicaPlayerViewAdapter`, `GGReplicaCoreVisualModule`, `GGReplicaBoostVisualModule`, and `GGReplicaShapeVisualModule`.
4. Rebuilt prefab has `ShipVisual/GGPlayerViewRoot` and all required render nodes.
5. Test scene controls call `ChangeViewState(int)`, not the old five-state enum.
6. Auditor passes and confirms live `Ship.prefab` / `SampleScene.unity` remain unpolluted.
7. Full GGReplica test subset and `dotnet build Project-Ark.slnx` pass.
8. Implementation log is updated.

## Execution handoff

Recommended execution mode: **Subagent-Driven**.

- Dispatch one fresh implementation subagent per task.
- After each task, review diff, run the task-specific tests, and update this plan checkboxes if desired.
- Do not start Task 5 prefab rewriting until Tasks 1-4 tests are green.
- Do not claim visual parity until Task 8 Play Mode checklist is manually/visually verified.
