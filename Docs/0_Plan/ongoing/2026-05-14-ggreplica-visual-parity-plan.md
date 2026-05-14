# GGReplica Visual Parity Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Upgrade `Ship_GGReplica.prefab` from sprite-only PlayerView MVP to a GG-original-inspired shader/material/VFX replica using evidence from the external Galactic Glitch dump.

**Architecture:** Keep `GGReplicaPlayerViewAdapter` as the `ChangeViewState(int)` coordinator. Add clean Project Ark GGReplica shader/material assets, an editor material builder, prefab wiring, runtime `MaterialPropertyBlock` state modulation, and auditor checks. External GG dump files are evidence, not direct runtime authority.

**Tech Stack:** Unity 6000, URP 2D, C# Editor tooling, SpriteRenderer, TrailRenderer, MaterialPropertyBlock, NUnit EditMode/PlayMode tests.

---

## Reference Paths

Always inspect these before changing GGReplica visuals:

```text
/Users/dada/Documents/Quark/ReferenceAssets/Galactic Glitch/DevXUnity/
/Users/dada/Documents/Quark/ReferenceAssets/Galactic Glitch/DevXUnity_exported/
```

Important evidence:

```text
DevXUnity/CLG/CLG_PlayerShipHighlight.shader
DevXUnity/Material/PlayerShipHL.mat
DevXUnity_exported/Assets/Materials/PlayerShipHL.mat

DevXUnity/Shader Graphs/Shader Graphs_TeleportScheme.shader
DevXUnity/Material/TeleportScheme.mat
DevXUnity_exported/Assets/Materials/TeleportScheme.mat

DevXUnity/Shader Graphs/Shader Graphs_PlayerLQTrail.shader
DevXUnity/Material/PlayerLQTrail.mat
DevXUnity_exported/Assets/Materials/PlayerLQTrail.mat

DevXUnity_exported/Assets/Prefab/Player.prefab
```

---

## Files to Create or Modify

### Create

- `Assets/_Art/Ship/GGReplica/Shaders/GGReplicaPlayerShipHighlight.shader`
- `Assets/_Art/Ship/GGReplica/Shaders/GGReplicaTeleportScheme.shader`
- `Assets/_Art/Ship/GGReplica/Shaders/GGReplicaPlayerLQTrail.shader`
- `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaMaterialAssetBuilder.cs`
- `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaMaterialAssetBuilderTests.cs`
- `Assets/Scripts/Ship/GGReplica/GGReplicaMaterialVisualModule.cs`

Material assets will be created by the editor builder, not hand-written:

- `Assets/_Art/Ship/GGReplica/Materials/GGReplicaPlayerShipHL.mat`
- `Assets/_Art/Ship/GGReplica/Materials/GGReplicaTeleportScheme.mat`
- `Assets/_Art/Ship/GGReplica/Materials/GGReplicaPlayerLQTrail.mat`

### Modify

- `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaPrefabBuilder.cs`
- `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaPrefabBuilderTests.cs`
- `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaAuditor.cs`
- `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaAuditorTests.cs`
- `Assets/Scripts/Ship/GGReplica/GGReplicaPlayerViewAdapter.cs`
- `Assets/Scripts/Ship/Tests/GGReplicaVisualModuleTests.cs`
- `Assets/_Prefabs/Ship/Ship_GGReplica.prefab`
- `Assets/Scenes/GGReplicaShipTest.unity`
- `Docs/2_TechnicalDesign/Ship/ShipVFX_AssetRegistry.md`
- `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

---

## Task 1: Material Asset Builder RED

**Files:**
- Create: `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaMaterialAssetBuilderTests.cs`

- [ ] **Step 1: Write failing test for material creation**

Create `GGReplicaMaterialAssetBuilderTests.cs` with these assertions:

```csharp
#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ProjectArk.Ship.Editor
{
    [TestFixture]
    public class GGReplicaMaterialAssetBuilderTests
    {
        private const string PlayerShipHlMaterialPath = "Assets/_Art/Ship/GGReplica/Materials/GGReplicaPlayerShipHL.mat";
        private const string TeleportSchemeMaterialPath = "Assets/_Art/Ship/GGReplica/Materials/GGReplicaTeleportScheme.mat";
        private const string PlayerLqTrailMaterialPath = "Assets/_Art/Ship/GGReplica/Materials/GGReplicaPlayerLQTrail.mat";

        [Test]
        public void BuildVisualMaterials_CreatesMaterialsWithOriginalGGParameters()
        {
            GGReplicaMaterialAssetBuilder.BuildVisualMaterials();

            var playerShipHl = AssetDatabase.LoadAssetAtPath<Material>(PlayerShipHlMaterialPath);
            var teleportScheme = AssetDatabase.LoadAssetAtPath<Material>(TeleportSchemeMaterialPath);
            var playerLqTrail = AssetDatabase.LoadAssetAtPath<Material>(PlayerLqTrailMaterialPath);

            Assert.That(playerShipHl, Is.Not.Null);
            Assert.That(teleportScheme, Is.Not.Null);
            Assert.That(playerLqTrail, Is.Not.Null);

            Assert.That(playerShipHl.shader.name, Is.EqualTo("ProjectArk/GGReplica/PlayerShipHighlight"));
            Assert.That(playerShipHl.GetFloat("_Intensity"), Is.EqualTo(8f).Within(0.001f));
            Assert.That(playerShipHl.GetFloat("_Smooth"), Is.EqualTo(0.01f).Within(0.001f));
            AssertColor(playerShipHl.GetColor("_Tint"), new Color(0.54509807f, 0.09019608f, 1f, 1f));

            Assert.That(teleportScheme.shader.name, Is.EqualTo("ProjectArk/GGReplica/TeleportScheme"));
            Assert.That(teleportScheme.GetFloat("_Intensity"), Is.EqualTo(1f).Within(0.001f));
            Assert.That(teleportScheme.GetFloat("_State"), Is.EqualTo(0f).Within(0.001f));
            Assert.That(teleportScheme.GetFloat("_ScanScale"), Is.EqualTo(8f).Within(0.001f));
            Assert.That(teleportScheme.GetFloat("_GlitchStrength"), Is.EqualTo(0.3f).Within(0.001f));

            Assert.That(playerLqTrail.shader.name, Is.EqualTo("ProjectArk/GGReplica/PlayerLQTrail"));
            AssertColor(playerLqTrail.GetColor("_MainColor"), new Color(0.12054504f, 0f, 0.18867922f, 1f));
            AssertColor(playerLqTrail.GetColor("_EdgeColor"), new Color(0.61328405f, 0f, 0.80784315f, 0f));
            Assert.That(playerLqTrail.GetVector("_NoiseParams"), Is.EqualTo(new Vector4(1f, 1f, 0.2f, 0.7f)));
        }

        private static void AssertColor(Color actual, Color expected)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.001f));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.001f));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.001f));
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(0.001f));
        }
    }
}
#endif
```

- [ ] **Step 2: Run test to verify RED**

Run through Unity Test Runner or MCP:

```text
ProjectArk.Ship.Editor.GGReplicaMaterialAssetBuilderTests.BuildVisualMaterials_CreatesMaterialsWithOriginalGGParameters
```

Expected: fail because `GGReplicaMaterialAssetBuilder` and shaders do not exist.

---

## Task 2: Clean GGReplica Shaders + Material Builder GREEN

**Files:**
- Create: `Assets/_Art/Ship/GGReplica/Shaders/GGReplicaPlayerShipHighlight.shader`
- Create: `Assets/_Art/Ship/GGReplica/Shaders/GGReplicaTeleportScheme.shader`
- Create: `Assets/_Art/Ship/GGReplica/Shaders/GGReplicaPlayerLQTrail.shader`
- Create: `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaMaterialAssetBuilder.cs`

- [ ] **Step 1: Add clean shader assets**

Use simple URP-compatible transparent sprite shaders. Keep the property names expected by tests:

```text
ProjectArk/GGReplica/PlayerShipHighlight
Properties: _MainTex, _SDFLast, _SDFNew, _SDFLastMask, _SDFNewMask, _Smooth, _Intensity, _Tint
Blend: SrcAlpha One
```

```text
ProjectArk/GGReplica/TeleportScheme
Properties: _MainTex, _SDFTex, _NoiseTex, _Intensity, _State, _ScanScale, _GlitchStrength, _ScrollSpeed
Blend: SrcAlpha OneMinusSrcAlpha
```

```text
ProjectArk/GGReplica/PlayerLQTrail
Properties: _MainColor, _EdgeColor, _NoiseParams, _ScrollSpeed
Blend: SrcAlpha OneMinusSrcAlpha
```

Implementation note: start with stable unlit sprite sampling. Add procedural scan/noise after the materials and prefab wiring are test-green.

- [ ] **Step 2: Add `GGReplicaMaterialAssetBuilder`**

Create an editor tool with constants:

```csharp
private const string PlayerShipHlShaderPath = "Assets/_Art/Ship/GGReplica/Shaders/GGReplicaPlayerShipHighlight.shader";
private const string TeleportSchemeShaderPath = "Assets/_Art/Ship/GGReplica/Shaders/GGReplicaTeleportScheme.shader";
private const string PlayerLqTrailShaderPath = "Assets/_Art/Ship/GGReplica/Shaders/GGReplicaPlayerLQTrail.shader";

private const string PlayerShipHlMaterialPath = "Assets/_Art/Ship/GGReplica/Materials/GGReplicaPlayerShipHL.mat";
private const string TeleportSchemeMaterialPath = "Assets/_Art/Ship/GGReplica/Materials/GGReplicaTeleportScheme.mat";
private const string PlayerLqTrailMaterialPath = "Assets/_Art/Ship/GGReplica/Materials/GGReplicaPlayerLQTrail.mat";
```

Public menu:

```csharp
[MenuItem("ProjectArk/Ship/GG Replica/Build Visual Materials")]
public static void BuildVisualMaterials()
```

Behavior:

- `AssetDatabase.LoadAssetAtPath<Shader>()` for each shader.
- If missing, `Debug.LogError` and abort.
- Ensure `Assets/_Art/Ship/GGReplica/Materials` exists.
- Create or update each material.
- Apply values from the design document.
- `AssetDatabase.SaveAssets()` and `AssetDatabase.Refresh()`.

- [ ] **Step 3: Run material builder test**

Expected: pass.

---

## Task 3: Prefab Material Wiring RED/GREEN

**Files:**
- Modify: `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaPrefabBuilderTests.cs`
- Modify: `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaPrefabBuilder.cs`

- [ ] **Step 1: Extend prefab builder test**

Add assertions to `BuildExperimentalPrefab_CreatesReplicaWithPlayerViewHierarchyAndModulesWired`:

```csharp
AssertRendererMaterial(viewRoot, "Ship_Sprite_HL", "Assets/_Art/Ship/GGReplica/Materials/GGReplicaPlayerShipHL.mat");
AssertRendererMaterial(viewRoot, "View", "Assets/_Art/Ship/GGReplica/Materials/GGReplicaTeleportScheme.mat");
```

Add helper:

```csharp
private static void AssertRendererMaterial(Transform viewRoot, string childName, string materialPath)
{
    var renderer = viewRoot.Find(childName)?.GetComponent<SpriteRenderer>();
    Assert.That(renderer, Is.Not.Null, $"Missing renderer {childName}.");
    var expected = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
    Assert.That(expected, Is.Not.Null, $"Missing expected material {materialPath}.");
    Assert.That(renderer.sharedMaterial, Is.SameAs(expected), $"{childName} must use {materialPath}.");
}
```

Add TrailRenderer assertion after `GGBoostVisualRoot` exists:

```csharp
var trail = shipVisual.GetComponentInChildren<TrailRenderer>(true);
Assert.That(trail, Is.Not.Null, "Replica should include a GGReplica trail renderer.");
var expectedTrailMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Art/Ship/GGReplica/Materials/GGReplicaPlayerLQTrail.mat");
Assert.That(trail.sharedMaterial, Is.SameAs(expectedTrailMaterial));
Assert.That(trail.time, Is.EqualTo(0.4f).Within(0.001f));
Assert.That(trail.widthMultiplier, Is.EqualTo(4f).Within(0.001f));
```

- [ ] **Step 2: Run test to verify RED**

Expected: fail because prefab builder does not wire materials/trail yet.

- [ ] **Step 3: Update `GGReplicaPrefabBuilder`**

Before `EnsureGGPlayerViewRoot`, call:

```csharp
GGReplicaMaterialAssetBuilder.BuildVisualMaterials();
```

Add material constants and load helper:

```csharp
private const string PlayerShipHlMaterialPath = "Assets/_Art/Ship/GGReplica/Materials/GGReplicaPlayerShipHL.mat";
private const string TeleportSchemeMaterialPath = "Assets/_Art/Ship/GGReplica/Materials/GGReplicaTeleportScheme.mat";
private const string PlayerLqTrailMaterialPath = "Assets/_Art/Ship/GGReplica/Materials/GGReplicaPlayerLQTrail.mat";
```

In `EnsureGGPlayerViewRoot`:

```csharp
highlight.sharedMaterial = LoadMaterial(PlayerShipHlMaterialPath);
view.sharedMaterial = LoadMaterial(TeleportSchemeMaterialPath);
```

In `EnsureVisualModules`, create/find:

```text
ShipVisual/GGPlayerLQTrail
```

Add `TrailRenderer` with:

```text
time = 0.4
widthMultiplier = 4
sharedMaterial = GGReplicaPlayerLQTrail.mat
emitting = false by default
```

Wire it to `GGReplicaBoostVisualModule` or the new material visual module in later tasks.

- [ ] **Step 4: Run prefab builder test**

Expected: pass.

---

## Task 4: Runtime Material Visual Module RED/GREEN

**Files:**
- Create: `Assets/Scripts/Ship/GGReplica/GGReplicaMaterialVisualModule.cs`
- Modify: `Assets/Scripts/Ship/GGReplica/GGReplicaPlayerViewAdapter.cs`
- Modify: `Assets/Scripts/Ship/Tests/GGReplicaVisualModuleTests.cs`

- [ ] **Step 1: Add PlayMode/EditMode test for runtime property blocks**

Add a test that creates renderers, assigns a test material, calls `ApplyState`, and verifies material assets are not mutated.

Expected behaviors:

```text
Boost -> highlight intensity > idle intensity; trail emits true
Dodge -> dodge/core pulse values > idle
Grab -> grab emphasis > idle
Idle -> values reset
```

- [ ] **Step 2: Implement `GGReplicaMaterialVisualModule`**

Fields:

```csharp
[SerializeField] private SpriteRenderer _highlightRenderer;
[SerializeField] private SpriteRenderer _viewRenderer;
[SerializeField] private SpriteRenderer _coreRenderer;
[SerializeField] private SpriteRenderer _dodgeRenderer;
[SerializeField] private TrailRenderer _playerLqTrail;
```

Use private `MaterialPropertyBlock` instances. Do not modify `sharedMaterial` in Play Mode.

State defaults:

```text
Idle/Undefined: highlight intensity 8, trail emitting false
Boost: highlight intensity 12, trail emitting true
Dodge: highlight intensity 10, dodge alpha/pulse enabled
Grab: highlight intensity 10, hand emphasis handled by shape module
Heal: highlight tint leans green/cyan briefly or via stable material value
```

- [ ] **Step 3: Wire module into adapter**

Add field:

```csharp
[SerializeField] private GGReplicaMaterialVisualModule _materialModule;
```

Update `NotifyModules`:

```csharp
_materialModule?.ApplyState(state);
```

- [ ] **Step 4: Run visual module tests**

Expected: pass.

---

## Task 5: Auditor Material Enforcement

**Files:**
- Modify: `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaAuditor.cs`
- Modify: `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaAuditorTests.cs`

- [ ] **Step 1: Add failing auditor test**

Test must fail if:

```text
Ship_Sprite_HL.sharedMaterial != GGReplicaPlayerShipHL.mat
View.sharedMaterial != GGReplicaTeleportScheme.mat
TrailRenderer.sharedMaterial != GGReplicaPlayerLQTrail.mat
```

- [ ] **Step 2: Update auditor**

Add material validation helpers:

```csharp
ValidateRendererMaterial(viewRoot, "Ship_Sprite_HL", PlayerShipHlMaterialPath, replica);
ValidateRendererMaterial(viewRoot, "View", TeleportSchemeMaterialPath, replica);
ValidateTrailMaterial(replica, PlayerLqTrailMaterialPath);
```

If material is missing or default, `Severity.Error`.

- [ ] **Step 3: Run auditor test**

Expected: pass.

---

## Task 6: Registry and Evidence Docs

**Files:**
- Modify: `Docs/2_TechnicalDesign/Ship/ShipVFX_AssetRegistry.md`
- Create or Modify: `Docs/6_Diagnostics/GGReplica_ShipAsset_Audit.md`
- Modify: `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- [ ] **Step 1: Update AssetRegistry**

Add entries:

```text
GGReplicaPlayerShipHLMaterial -> Assets/_Art/Ship/GGReplica/Materials/GGReplicaPlayerShipHL.mat
GGReplicaTeleportSchemeMaterial -> Assets/_Art/Ship/GGReplica/Materials/GGReplicaTeleportScheme.mat
GGReplicaPlayerLQTrailMaterial -> Assets/_Art/Ship/GGReplica/Materials/GGReplicaPlayerLQTrail.mat
GGReplicaPlayerShipHighlightShader -> Assets/_Art/Ship/GGReplica/Shaders/GGReplicaPlayerShipHighlight.shader
GGReplicaTeleportSchemeShader -> Assets/_Art/Ship/GGReplica/Shaders/GGReplicaTeleportScheme.shader
GGReplicaPlayerLQTrailShader -> Assets/_Art/Ship/GGReplica/Shaders/GGReplicaPlayerLQTrail.shader
```

- [ ] **Step 2: Update diagnostics**

Record the external source evidence paths and exact GG parameter values from this plan.

- [ ] **Step 3: Append implementation log**

Use current timestamp. Include created/modified files, content, purpose, and technical approach.

---

## Task 7: Full Verification

**Commands / Tools:**

- [ ] **Step 1: Run focused EditMode tests**

```text
GGReplicaMaterialAssetBuilderTests
GGReplicaPrefabBuilderTests
GGReplicaAuditorTests
```

Expected: all pass.

- [ ] **Step 2: Run focused runtime tests**

```text
GGReplicaVisualModuleTests
GGReplicaPlayerViewAdapterTests
```

Expected: all pass.

- [ ] **Step 3: Run build**

```bash
cd /Users/dada/Documents/GitHub/Project-Ark
dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly
```

Expected: 0 errors.

- [ ] **Step 4: Run Unity menu chain**

```text
ProjectArk > Ship > GG Replica > Build Visual Materials
ProjectArk > Ship > GG Replica > Build Player Skin Asset
ProjectArk > Ship > GG Replica > Build Experimental Prefab
ProjectArk > Ship > GG Replica > Build Test Scene
ProjectArk > Ship > GG Replica > Audit Replica Isolation
```

Expected: Auditor PASS with no material errors.

- [ ] **Step 5: Play Mode visual checklist**

Open:

```text
Assets/Scenes/GGReplicaShipTest.unity
```

Check:

```text
1 Boost -> trail/material effect visible
2 Dodge -> ghost/half/core effect visible
7 Grab -> hands + emphasis visible
9 Heal -> non-sprite-only visual feedback visible
0 Idle -> stable reset
```

- [ ] **Step 6: Patch hygiene**

```bash
cd /Users/dada/Documents/GitHub/Project-Ark
git diff --check
```

Expected: no output.

---

## Execution Notes

- Do not edit `.meta` files manually.
- Do not write to `/Users/dada/Documents/Quark/ReferenceAssets/Galactic Glitch`; it is reference-only.
- Do not mutate shared material assets at runtime; use `MaterialPropertyBlock`.
- Do not modify live `Ship.prefab` or `SampleScene.unity`.
- Every file change must be recorded in `Docs/5_ImplementationLog/ImplementationLog_2026-05.md` before ending the implementation turn.
