# GGReplica Ship Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build an isolated `Ship_GGReplica.prefab` that selectively migrates Galactic Glitch ship visuals, shader candidates, Boost/Dodge VFX, audio, and feel tuning for A/B testing without modifying the live `Ship.prefab`.

**Architecture:** Use a parallel replica lane: copied GG assets live under `Assets/_Art/Ship/GGReplica/`, runtime adapters live under `Assets/Scripts/Ship/GGReplica/`, and only `Ship_GGReplica.prefab` consumes them. Existing `Ship.prefab`, `SampleScene.unity`, live Ship/VFX materials, and live authority tools remain untouched.

**Tech Stack:** Unity 6, URP 2D, C#, ScriptableObject profiles, Editor automation, PrimeTween/UniTask where already used, project `Ship/VFX` authority rules.

---

## Global constraints

- Do not modify `Assets/_Prefabs/Ship/Ship.prefab`.
- Do not modify `Assets/Scenes/SampleScene.unity`.
- Do not copy external `.meta` files from Galactic Glitch.
- Do not import whole DevX shader directories.
- Do not make DevX shader files part of the live Ship/VFX chain until Shader Spike grades them L2 or L3.
- Do not run `git commit` unless the user explicitly asks. Commit steps below are written as checkpoint suggestions only.
- Every file modification must be logged in `Docs/5_ImplementationLog/ImplementationLog_2026-05.md` before completion.

## Source roots

```text
GG_ROOT=/Users/dada/Documents/Quark/ReferenceAssets/Galactic Glitch
DEVX=$GG_ROOT/DevXUnity
DEVX_EXPORTED=$GG_ROOT/DevXUnity_exported
PROJECT=/Users/dada/Documents/GitHub/Project-Ark
```

## Target roots

```text
Assets/_Art/Ship/GGReplica/
Assets/_Prefabs/Ship/Ship_GGReplica.prefab
Assets/_Data/Ship/GGReplicaShipVisualProfile.asset
Assets/_Data/Ship/GGReplicaShipFeelProfile.asset
Assets/Scripts/Ship/GGReplica/
Assets/Scenes/GGReplicaShipTest.unity
Docs/6_Diagnostics/GGReplica_ShipAsset_Audit.md
Docs/6_Diagnostics/GGReplica_ShaderSpike_Report.md
```

---

## File structure map

### New diagnostics docs

- Create `Docs/6_Diagnostics/GGReplica_ShipAsset_Audit.md`
  - Records exact source file, target file, asset category, migration status, importer settings, and notes.
- Create `Docs/6_Diagnostics/GGReplica_ShaderSpike_Report.md`
  - Records shader candidates, compile status, visual result, L0-L3 grade, and final decision.

### New runtime scripts

- Create `Assets/Scripts/Ship/GGReplica/GGReplicaShipVisualProfileSO.cs`
  - ScriptableObject storing five visual state sprite packs and audio references.
- Create `Assets/Scripts/Ship/GGReplica/GGReplicaShipFeelProfileSO.cs`
  - ScriptableObject storing GG-inspired Boost/Dodge feel parameters.
- Create `Assets/Scripts/Ship/GGReplica/GGReplicaShipViewAdapter.cs`
  - Runtime state resolver and sprite-pack switcher for the replica prefab only.
- Create `Assets/Scripts/Ship/GGReplica/GGReplicaShipFeelAdapter.cs`
  - Runtime adapter applying feel profile values to replica-only components.
- Create `Assets/Scripts/Ship/GGReplica/GGReplicaBoostVfxAdapter.cs`
  - Replica-only Boost audio/VFX bridge.
- Create `Assets/Scripts/Ship/GGReplica/GGReplicaDashVfxAdapter.cs`
  - Replica-only Dodge audio/VFX bridge.
- Create `Assets/Scripts/Ship/GGReplica/GGReplicaTestSwitcher.cs`
  - Test scene helper for A/B switching and forced visual states.

### New editor scripts

- Create `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaAssetImporter.cs`
  - Copies approved PNG/WAV/shader trial files and applies Unity importer settings.
- Create `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaPrefabBuilder.cs`
  - Builds only `Ship_GGReplica.prefab` from the existing live ship prefab plus replica visual profile.
- Create `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaTestSceneBuilder.cs`
  - Builds `Assets/Scenes/GGReplicaShipTest.unity`.
- Create `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaAuditor.cs`
  - Read-only audit for missing assets, prefab pollution, and live-chain violations.

### Existing files to inspect or modify

- Inspect only unless a task explicitly says otherwise:
  - `Assets/Scripts/Ship/VFX/ShipView.cs`
  - `Assets/Scripts/Ship/VFX/ShipBoostVisuals.cs`
  - `Assets/Scripts/Ship/VFX/ShipDashVisuals.cs`
  - `Assets/Scripts/Ship/Movement/ShipBoost.cs`
  - `Assets/Scripts/Ship/Movement/ShipDash.cs`
  - `Assets/Scripts/Ship/Data/ShipStatsSO.cs`
  - `Assets/Scripts/Ship/Editor/ShipPrefabRebuilder.cs`
  - `Assets/Scripts/Ship/Editor/ShipVfxValidator.cs`
- Modify only if Phase 4 proves an adapter cannot reach required data safely:
  - `Assets/Scripts/Ship/Movement/ShipMotor.cs`
  - `Assets/Scripts/Ship/Movement/ShipBoost.cs`
  - `Assets/Scripts/Ship/Movement/ShipDash.cs`

---

## Task 0: Create asset audit document

**Files:**
- Create: `Docs/6_Diagnostics/GGReplica_ShipAsset_Audit.md`

- [ ] **Step 0.1: Write audit skeleton**

Create the file with this exact structure:

```markdown
# GGReplica Ship Asset Audit

> Source: `/Users/dada/Documents/Quark/ReferenceAssets/Galactic Glitch`  
> Target: `Assets/_Art/Ship/GGReplica/`  
> Status: Phase 0 audit

## 1. Rules

- Do not copy external `.meta` files.
- Prefer `DevXUnity/Sprite/` for Sprite PNG sources.
- Use `DevXUnity_exported/Assets/Images/*.meta` as importer-setting reference.
- Do not copy whole shader directories.
- Do not modify live `Assets/_Art/Ship/Glitch/` assets during this replica experiment.

## 2. Required sprite mapping

| Category | Source preferred | Exported fallback | Target | PPU | Pivot | Status | Notes |
|---|---|---|---|---:|---|---|---|
| Normal Solid | `DevXUnity/Sprite/Movement_d10.png` | `DevXUnity_exported/Assets/Images/Movement_d10.png` | `Assets/_Art/Ship/GGReplica/Sprites/Movement_10.png` | 320 | center | Pending | Normal solid |
| Normal Liquid | `DevXUnity/Sprite/Movement_d3.png` | `DevXUnity_exported/Assets/Images/Movement_d3.png` | `Assets/_Art/Ship/GGReplica/Sprites/Movement_3.png` | 320 | center | Pending | Normal liquid |
| Normal Highlight | `DevXUnity/Sprite/Movement_d21.png` | `DevXUnity_exported/Assets/Images/Movement_d21.png` | `Assets/_Art/Ship/GGReplica/Sprites/Movement_21.png` | 320 | center | Pending | Normal highlight |
| Boost Solid | `DevXUnity/Sprite/Boost_d2.png` | `DevXUnity_exported/Assets/Images/Boost_d2.png` | `Assets/_Art/Ship/GGReplica/Sprites/Boost_2.png` | 320 | center | Pending | Boost solid |
| Boost Liquid | `DevXUnity/Sprite/Boost_d16.png` | `DevXUnity_exported/Assets/Images/Boost_d16.png` | `Assets/_Art/Ship/GGReplica/Sprites/Boost_16.png` | 320 | center | Pending | Boost liquid |
| Boost Highlight | `DevXUnity/Sprite/Boost_d8.png` | `DevXUnity_exported/Assets/Images/Boost_d8.png` | `Assets/_Art/Ship/GGReplica/Sprites/Boost_8.png` | 320 | center | Pending | Boost highlight |
| Fire Solid | `DevXUnity/Sprite/Primary_d4.png` | `DevXUnity_exported/Assets/Images/Primary_d4.png` | `Assets/_Art/Ship/GGReplica/Sprites/Primary_4.png` | 320 | center | Pending | Fire solid |
| Fire Liquid | `DevXUnity/Sprite/Primary.png` | `DevXUnity_exported/Assets/Images/Primary.png` | `Assets/_Art/Ship/GGReplica/Sprites/Primary.png` | 320 | center | Pending | Fire liquid |
| Fire Highlight | `DevXUnity/Sprite/Primary_d6.png` | `DevXUnity_exported/Assets/Images/Primary_d6.png` | `Assets/_Art/Ship/GGReplica/Sprites/Primary_6.png` | 320 | center | Pending | Fire highlight |
| Dodge Ghost | `DevXUnity/Sprite/player_test_fire.png` | `DevXUnity_exported/Assets/Images/player_test_fire.png` | `Assets/_Art/Ship/GGReplica/Sprites/player_test_fire.png` | 707 | `(0.5, 0.3282)` | Pending | Dodge ghost |
| Back Thruster | `DevXUnity/Sprite/GrabGun_back_d3.png` | `DevXUnity_exported/Assets/Images/GrabGun_back_d3.png` | `Assets/_Art/Ship/GGReplica/Sprites/GrabGun_Back_3.png` | 320 | center | Pending | Rear layer |
| Reactor | `DevXUnity/Sprite/reactor.png` | `DevXUnity_exported/Assets/Images/reactor.png` | `Assets/_Art/Ship/GGReplica/Sprites/reactor.png` | 320 | center | Pending | Core sprite |

## 3. Required audio mapping

| Source | Target | Status | Notes |
|---|---|---|---|
| `DevXUnity/AudioClip/SND_PLAYER_BOOST.wav` | `Assets/_Art/Ship/GGReplica/Audio/SND_PLAYER_BOOST.wav` | Pending | Boost loop |
| `DevXUnity/AudioClip/SND_PLAYER_BOOST_IGNITE.wav` | `Assets/_Art/Ship/GGReplica/Audio/SND_PLAYER_BOOST_IGNITE.wav` | Pending | Boost start |
| `DevXUnity/AudioClip/PLAYER_DODGE.wav` | `Assets/_Art/Ship/GGReplica/Audio/PLAYER_DODGE.wav` | Pending | Dodge |
| `DevXUnity/AudioClip/PLAYER_NORMAL_SHOT.wav` | `Assets/_Art/Ship/GGReplica/Audio/PLAYER_NORMAL_SHOT.wav` | Pending | Fire loop/shot |
| `DevXUnity/AudioClip/PLAYER_FIRST_SHOT.wav` | `Assets/_Art/Ship/GGReplica/Audio/PLAYER_FIRST_SHOT.wav` | Pending | Fire start |
| `DevXUnity/AudioClip/PLAYER_LAST_SHOT.wav` | `Assets/_Art/Ship/GGReplica/Audio/PLAYER_LAST_SHOT.wav` | Pending | Fire end |
| `DevXUnity/AudioClip/PLAYER_DEATH.wav` | `Assets/_Art/Ship/GGReplica/Audio/PLAYER_DEATH.wav` | Pending | Death reference |

## 4. Shader candidates

| Candidate | Source | Trial target | Grade | Decision | Notes |
|---|---|---|---|---|---|
| PlayerShipHighlight | `DevXUnity/CLG/CLG_PlayerShipHighlight.shader` | `Assets/_Art/Ship/GGReplica/Shaders/DevX_Trial/CLG_PlayerShipHighlight.shader` | Pending | Pending | Highlight shader |
| PlayerLightSourceColored | `DevXUnity/Sprite Shaders URP/Lit Sprite/Color/Sprite Shaders URP_Lit Sprite_Color_Strong Tint (Override) Lit PlayerLightSourceColored.shader` | `Assets/_Art/Ship/GGReplica/Shaders/DevX_Trial/Lit_PlayerLightSourceColored.shader` | Pending | Pending | URP sprite tint |
| PlayerLQTrail | `DevXUnity/Shader Graphs/Shader Graphs_PlayerLQTrail.shader` | `Assets/_Art/Ship/GGReplica/Shaders/DevX_Trial/PlayerLQTrail.shader` | Pending | Pending | Trail shader |

## 5. Audit results

- Missing source files: none checked yet.
- Already present in live `Assets/_Art/Ship/Glitch/`: not evaluated in this replica audit.
- Direct migration allowed: PNG, WAV, limited shader trial files.
- Reference only: `Player.prefab`, `Player.controller`, `Player.cs`, `PlayerView.cs`.
```

- [ ] **Step 0.2: Verify source files exist**

Run:

```bash
cd /Users/dada/Documents/GitHub/Project-Ark && python3 - << 'PY'
from pathlib import Path
root = Path('/Users/dada/Documents/Quark/ReferenceAssets/Galactic Glitch')
files = [
 'DevXUnity/Sprite/Movement_d10.png','DevXUnity/Sprite/Movement_d3.png','DevXUnity/Sprite/Movement_d21.png',
 'DevXUnity/Sprite/Boost_d2.png','DevXUnity/Sprite/Boost_d16.png','DevXUnity/Sprite/Boost_d8.png',
 'DevXUnity/Sprite/Primary_d4.png','DevXUnity/Sprite/Primary.png','DevXUnity/Sprite/Primary_d6.png',
 'DevXUnity/Sprite/player_test_fire.png','DevXUnity/Sprite/GrabGun_back_d3.png','DevXUnity/Sprite/reactor.png',
 'DevXUnity/AudioClip/SND_PLAYER_BOOST.wav','DevXUnity/AudioClip/SND_PLAYER_BOOST_IGNITE.wav','DevXUnity/AudioClip/PLAYER_DODGE.wav',
 'DevXUnity/AudioClip/PLAYER_NORMAL_SHOT.wav','DevXUnity/AudioClip/PLAYER_FIRST_SHOT.wav','DevXUnity/AudioClip/PLAYER_LAST_SHOT.wav','DevXUnity/AudioClip/PLAYER_DEATH.wav',
 'DevXUnity/CLG/CLG_PlayerShipHighlight.shader'
]
missing = [f for f in files if not (root / f).exists()]
print('MISSING:', missing)
raise SystemExit(1 if missing else 0)
PY
```

Expected: `MISSING: []`.

- [ ] **Step 0.3: Checkpoint**

Do not commit unless the user explicitly approves. Suggested commit message if approved later:

```bash
git add Docs/6_Diagnostics/GGReplica_ShipAsset_Audit.md
# git commit -m "docs: audit GGReplica ship source assets"
```

---

## Task 1: Add asset importer editor tool

**Files:**
- Create: `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaAssetImporter.cs`
- Modify after import: `Docs/6_Diagnostics/GGReplica_ShipAsset_Audit.md`

- [ ] **Step 1.1: Create importer directory**

Run:

```bash
mkdir -p /Users/dada/Documents/GitHub/Project-Ark/Assets/Scripts/Ship/Editor/GGReplica
```

Expected: directory exists.

- [ ] **Step 1.2: Create `GGReplicaAssetImporter.cs`**

Create file with this complete implementation:

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ProjectArk.Ship.Editor
{
    /// <summary>
    /// Copies a curated subset of Galactic Glitch reference assets into the isolated
    /// GGReplica folder and applies deterministic Unity importer settings.
    /// Never copies external .meta files.
    /// </summary>
    public static class GGReplicaAssetImporter
    {
        private const string SourceRoot = "/Users/dada/Documents/Quark/ReferenceAssets/Galactic Glitch";
        private const string TargetRoot = "Assets/_Art/Ship/GGReplica";
        private const int DefaultShipPPU = 320;

        private readonly struct CopyEntry
        {
            public readonly string SourceRelative;
            public readonly string TargetRelative;
            public readonly int PixelsPerUnit;
            public readonly Vector2 Pivot;

            public CopyEntry(string sourceRelative, string targetRelative, int pixelsPerUnit, Vector2 pivot)
            {
                SourceRelative = sourceRelative;
                TargetRelative = targetRelative;
                PixelsPerUnit = pixelsPerUnit;
                Pivot = pivot;
            }
        }

        private static readonly CopyEntry[] SpriteEntries =
        {
            new("DevXUnity/Sprite/Movement_d10.png", "Sprites/Movement_10.png", DefaultShipPPU, new Vector2(0.5f, 0.5f)),
            new("DevXUnity/Sprite/Movement_d3.png", "Sprites/Movement_3.png", DefaultShipPPU, new Vector2(0.5f, 0.5f)),
            new("DevXUnity/Sprite/Movement_d21.png", "Sprites/Movement_21.png", DefaultShipPPU, new Vector2(0.5f, 0.5f)),
            new("DevXUnity/Sprite/Boost_d2.png", "Sprites/Boost_2.png", DefaultShipPPU, new Vector2(0.5f, 0.5f)),
            new("DevXUnity/Sprite/Boost_d16.png", "Sprites/Boost_16.png", DefaultShipPPU, new Vector2(0.5f, 0.5f)),
            new("DevXUnity/Sprite/Boost_d8.png", "Sprites/Boost_8.png", DefaultShipPPU, new Vector2(0.5f, 0.5f)),
            new("DevXUnity/Sprite/Primary_d4.png", "Sprites/Primary_4.png", DefaultShipPPU, new Vector2(0.5f, 0.5f)),
            new("DevXUnity/Sprite/Primary.png", "Sprites/Primary.png", DefaultShipPPU, new Vector2(0.5f, 0.5f)),
            new("DevXUnity/Sprite/Primary_d6.png", "Sprites/Primary_6.png", DefaultShipPPU, new Vector2(0.5f, 0.5f)),
            new("DevXUnity/Sprite/player_test_fire.png", "Sprites/player_test_fire.png", 707, new Vector2(0.5f, 0.3282f)),
            new("DevXUnity/Sprite/GrabGun_back_d3.png", "Sprites/GrabGun_Back_3.png", DefaultShipPPU, new Vector2(0.5f, 0.5f)),
            new("DevXUnity/Sprite/reactor.png", "Sprites/reactor.png", DefaultShipPPU, new Vector2(0.5f, 0.5f)),
        };

        private static readonly CopyEntry[] AudioEntries =
        {
            new("DevXUnity/AudioClip/SND_PLAYER_BOOST.wav", "Audio/SND_PLAYER_BOOST.wav", 0, Vector2.zero),
            new("DevXUnity/AudioClip/SND_PLAYER_BOOST_IGNITE.wav", "Audio/SND_PLAYER_BOOST_IGNITE.wav", 0, Vector2.zero),
            new("DevXUnity/AudioClip/PLAYER_DODGE.wav", "Audio/PLAYER_DODGE.wav", 0, Vector2.zero),
            new("DevXUnity/AudioClip/PLAYER_NORMAL_SHOT.wav", "Audio/PLAYER_NORMAL_SHOT.wav", 0, Vector2.zero),
            new("DevXUnity/AudioClip/PLAYER_FIRST_SHOT.wav", "Audio/PLAYER_FIRST_SHOT.wav", 0, Vector2.zero),
            new("DevXUnity/AudioClip/PLAYER_LAST_SHOT.wav", "Audio/PLAYER_LAST_SHOT.wav", 0, Vector2.zero),
            new("DevXUnity/AudioClip/PLAYER_DEATH.wav", "Audio/PLAYER_DEATH.wav", 0, Vector2.zero),
        };

        private static readonly CopyEntry[] ShaderTrialEntries =
        {
            new("DevXUnity/CLG/CLG_PlayerShipHighlight.shader", "Shaders/DevX_Trial/CLG_PlayerShipHighlight.shader", 0, Vector2.zero),
            new("DevXUnity/Sprite Shaders URP/Lit Sprite/Color/Sprite Shaders URP_Lit Sprite_Color_Strong Tint (Override) Lit PlayerLightSourceColored.shader", "Shaders/DevX_Trial/Lit_PlayerLightSourceColored.shader", 0, Vector2.zero),
            new("DevXUnity/Shader Graphs/Shader Graphs_PlayerLQTrail.shader", "Shaders/DevX_Trial/PlayerLQTrail.shader", 0, Vector2.zero),
        };

        [MenuItem("ProjectArk/Ship/GG Replica/Import Curated Assets")]
        public static void ImportCuratedAssets()
        {
            var missing = new List<string>();
            EnsureFolder("Assets/_Art", "Ship");
            EnsureFolder("Assets/_Art/Ship", "GGReplica");
            EnsureFolder(TargetRoot, "Sprites");
            EnsureFolder(TargetRoot, "Audio");
            EnsureFolder(TargetRoot, "Shaders");
            EnsureFolder($"{TargetRoot}/Shaders", "DevX_Trial");
            EnsureFolder(TargetRoot, "Materials");
            EnsureFolder($"{TargetRoot}/Materials", "DevX_Trial");

            CopyEntries(SpriteEntries, missing);
            CopyEntries(AudioEntries, missing);
            CopyEntries(ShaderTrialEntries, missing);
            AssetDatabase.Refresh();

            ApplySpriteSettings(SpriteEntries);
            ApplyAudioSettings(AudioEntries);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (missing.Count > 0)
            {
                Debug.LogError("[GGReplicaAssetImporter] Missing source files:\n" + string.Join("\n", missing));
                EditorUtility.DisplayDialog("GGReplica Import Completed With Missing Files",
                    string.Join("\n", missing), "OK");
                return;
            }

            Debug.Log("[GGReplicaAssetImporter] Curated GGReplica assets imported successfully.");
            EditorUtility.DisplayDialog("GGReplica Import Complete",
                "Imported curated sprites, audio, and shader trial files into Assets/_Art/Ship/GGReplica.", "OK");
        }

        private static void CopyEntries(IEnumerable<CopyEntry> entries, List<string> missing)
        {
            foreach (var entry in entries)
            {
                string source = Path.Combine(SourceRoot, entry.SourceRelative);
                string targetAssetPath = $"{TargetRoot}/{entry.TargetRelative}";
                string targetAbsolutePath = Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, targetAssetPath);
                Directory.CreateDirectory(Path.GetDirectoryName(targetAbsolutePath)!);

                if (!File.Exists(source))
                {
                    missing.Add(source);
                    continue;
                }

                File.Copy(source, targetAbsolutePath, overwrite: true);
            }
        }

        private static void ApplySpriteSettings(IEnumerable<CopyEntry> entries)
        {
            foreach (var entry in entries)
            {
                string assetPath = $"{TargetRoot}/{entry.TargetRelative}";
                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer == null) continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = entry.PixelsPerUnit;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.spritePivot = entry.Pivot;
                importer.SaveAndReimport();
            }
        }

        private static void ApplyAudioSettings(IEnumerable<CopyEntry> entries)
        {
            foreach (var entry in entries)
            {
                string assetPath = $"{TargetRoot}/{entry.TargetRelative}";
                var importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
                if (importer == null) continue;

                var sampleSettings = importer.defaultSampleSettings;
                sampleSettings.loadType = AudioClipLoadType.DecompressOnLoad;
                sampleSettings.compressionFormat = AudioCompressionFormat.PCM;
                importer.defaultSampleSettings = sampleSettings;
                importer.SaveAndReimport();
            }
        }

        private static void EnsureFolder(string parent, string folder)
        {
            string path = $"{parent}/{folder}";
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
```

- [ ] **Step 1.3: Validate editor compile**

Run:

```bash
cd /Users/dada/Documents/GitHub/Project-Ark && dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly
```

Expected: `0 个错误`.

- [ ] **Step 1.4: Run importer in Unity**

In Unity Editor run:

```text
ProjectArk > Ship > GG Replica > Import Curated Assets
```

Expected:

- `Assets/_Art/Ship/GGReplica/Sprites/` contains 12 PNG assets.
- `Assets/_Art/Ship/GGReplica/Audio/` contains 7 WAV assets.
- `Assets/_Art/Ship/GGReplica/Shaders/DevX_Trial/` contains 3 shader trial files.
- Console has no missing source errors.

- [ ] **Step 1.5: Checkpoint**

Do not commit unless the user explicitly approves. Suggested checkpoint:

```bash
git add Assets/Scripts/Ship/Editor/GGReplica/GGReplicaAssetImporter.cs Assets/_Art/Ship/GGReplica Docs/6_Diagnostics/GGReplica_ShipAsset_Audit.md
# git commit -m "feat: import curated GGReplica ship assets"
```

---

## Task 2: Run DevX shader compatibility spike

**Files:**
- Create: `Docs/6_Diagnostics/GGReplica_ShaderSpike_Report.md`
- Create trial assets under: `Assets/_Art/Ship/GGReplica/Materials/DevX_Trial/`

- [ ] **Step 2.1: Create shader spike report**

Create `Docs/6_Diagnostics/GGReplica_ShaderSpike_Report.md` with:

```markdown
# GGReplica Shader Spike Report

> Goal: determine whether selected DevXUnity shaders can be directly or partially used by Project Ark GGReplica.

## 1. Grades

| Grade | Meaning | Decision |
|---|---|---|
| L0 | Imports but does not compile | Reference only |
| L1 | Compiles but visual output is clearly wrong | Rebuild in Project Ark shader |
| L2 | Main visual works, minor parameters broken | Partial adoption |
| L3 | Works almost directly | Adopt under Project Ark naming and ownership |

## 2. Candidates

| Shader | Path | Compile result | Visual result | Grade | Decision |
|---|---|---|---|---|---|
| PlayerShipHighlight | `Assets/_Art/Ship/GGReplica/Shaders/DevX_Trial/CLG_PlayerShipHighlight.shader` | Pending | Pending | Pending | Pending |
| Lit_PlayerLightSourceColored | `Assets/_Art/Ship/GGReplica/Shaders/DevX_Trial/Lit_PlayerLightSourceColored.shader` | Pending | Pending | Pending | Pending |
| PlayerLQTrail | `Assets/_Art/Ship/GGReplica/Shaders/DevX_Trial/PlayerLQTrail.shader` | Pending | Pending | Pending | Pending |

## 3. Final decision

- Highlight layer shader: Pending
- Liquid layer shader: Pending
- Boost trail shader: Pending
- Fallback implementation needed: Pending
```

- [ ] **Step 2.2: Create trial materials manually in Unity**

In Unity create these materials:

```text
Assets/_Art/Ship/GGReplica/Materials/DevX_Trial/mat_trial_player_ship_hl.mat
Assets/_Art/Ship/GGReplica/Materials/DevX_Trial/mat_trial_player_lit_colored.mat
Assets/_Art/Ship/GGReplica/Materials/DevX_Trial/mat_trial_player_lq_trail.mat
```

Assign shaders:

```text
mat_trial_player_ship_hl      -> CLG/PlayerShipHighlight if it compiles
mat_trial_player_lit_colored  -> imported Lit_PlayerLightSourceColored if it compiles
mat_trial_player_lq_trail     -> imported PlayerLQTrail if it compiles
```

Set material values if available:

```text
_Tint = #8B17FFFF
_Intensity = 8
_Smooth = 0.01
```

- [ ] **Step 2.3: Read Unity Console**

Use Unity Console or MCP `read_console` if available.

Expected possibilities:

- If shader compile errors exist: grade L0 for that shader.
- If no compile errors: create simple SpriteRenderer objects and inspect visual output.

- [ ] **Step 2.4: Update shader spike report**

Update each candidate row with one of L0/L1/L2/L3 and final decision. Use concrete text, for example:

```markdown
| PlayerShipHighlight | `Assets/_Art/Ship/GGReplica/Shaders/DevX_Trial/CLG_PlayerShipHighlight.shader` | FAIL: UnityLab restore marker / invalid program block | Not rendered | L0 | Reference only; rebuild additive highlight shader with `_Tint`, `_Intensity`, `_Smooth`, `Blend SrcAlpha One` |
```

- [ ] **Step 2.5: Checkpoint**

Do not commit unless the user explicitly approves. Suggested checkpoint:

```bash
git add Assets/_Art/Ship/GGReplica/Shaders/DevX_Trial Assets/_Art/Ship/GGReplica/Materials/DevX_Trial Docs/6_Diagnostics/GGReplica_ShaderSpike_Report.md
# git commit -m "test: evaluate DevX shaders for GGReplica"
```

---

## Task 3: Add visual and feel profiles

**Files:**
- Create: `Assets/Scripts/Ship/GGReplica/GGReplicaShipVisualProfileSO.cs`
- Create: `Assets/Scripts/Ship/GGReplica/GGReplicaShipFeelProfileSO.cs`
- Create assets manually or via editor: `Assets/_Data/Ship/GGReplicaShipVisualProfile.asset`, `Assets/_Data/Ship/GGReplicaShipFeelProfile.asset`

- [ ] **Step 3.1: Create runtime directory**

Run:

```bash
mkdir -p /Users/dada/Documents/GitHub/Project-Ark/Assets/Scripts/Ship/GGReplica
```

- [ ] **Step 3.2: Create `GGReplicaShipVisualProfileSO.cs`**

```csharp
using System;
using UnityEngine;

namespace ProjectArk.Ship
{
    public enum GGReplicaVisualState
    {
        Normal,
        Boost,
        Dodge,
        Fire,
        FireBoost
    }

    [Serializable]
    public class GGReplicaSpritePack
    {
        public GGReplicaVisualState State;
        [Min(0f)] public float FadeDuration = 0.2f;
        public Sprite SolidSprite;
        public Sprite LiquidSprite;
        public Sprite HighlightSprite;
        public Vector3 SpritesOffset;
    }

    [CreateAssetMenu(fileName = "GGReplicaShipVisualProfile", menuName = "ProjectArk/Ship/GG Replica/Visual Profile")]
    public class GGReplicaShipVisualProfileSO : ScriptableObject
    {
        [Header("Sprite Packs")]
        [SerializeField] private GGReplicaSpritePack[] _spritePacks = Array.Empty<GGReplicaSpritePack>();

        [Header("Persistent Layers")]
        [SerializeField] private Sprite _backSprite;
        [SerializeField] private Sprite _coreSprite;
        [SerializeField] private Sprite _dodgeGhostSprite;

        [Header("Audio")]
        [SerializeField] private AudioClip _boostIgniteClip;
        [SerializeField] private AudioClip _boostLoopClip;
        [SerializeField] private AudioClip _dodgeClip;
        [SerializeField] private AudioClip _fireClip;

        public Sprite BackSprite => _backSprite;
        public Sprite CoreSprite => _coreSprite;
        public Sprite DodgeGhostSprite => _dodgeGhostSprite;
        public AudioClip BoostIgniteClip => _boostIgniteClip;
        public AudioClip BoostLoopClip => _boostLoopClip;
        public AudioClip DodgeClip => _dodgeClip;
        public AudioClip FireClip => _fireClip;

        public bool TryGetPack(GGReplicaVisualState state, out GGReplicaSpritePack pack)
        {
            foreach (var candidate in _spritePacks)
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

- [ ] **Step 3.3: Create `GGReplicaShipFeelProfileSO.cs`**

```csharp
using UnityEngine;

namespace ProjectArk.Ship
{
    [CreateAssetMenu(fileName = "GGReplicaShipFeelProfile", menuName = "ProjectArk/Ship/GG Replica/Feel Profile")]
    public class GGReplicaShipFeelProfileSO : ScriptableObject
    {
        [Header("Dodge")]
        [Min(0f)] [SerializeField] private float _dodgeForce = 13f;
        [Min(0f)] [SerializeField] private float _dodgeForceAfterDodge = 6f;
        [Min(0f)] [SerializeField] private float _dodgeInvulnerabilityTime = 0.15f;
        [Min(0f)] [SerializeField] private float _dodgeCacheTime = 0.12f;
        [Min(0f)] [SerializeField] private float _dodgeRechargeTime = 0.5f;
        [Min(1)] [SerializeField] private int _maxDodgeCharges = 1;
        [SerializeField] private float _speedModAfterDodge = 1.15f;
        [Min(0f)] [SerializeField] private float _speedModAfterDodgeTime = 0.2f;
        [Min(0f)] [SerializeField] private float _timeForActionAfterEndDodge = 0.12f;

        [Header("Boost")]
        [Min(0f)] [SerializeField] private float _boostSpeedMultiplier = 1.2f;
        [Min(0f)] [SerializeField] private float _afterBoostDrag = 2.5f;
        [Min(0f)] [SerializeField] private float _dragChangeTime = 0.15f;
        [Min(0f)] [SerializeField] private float _boostStartImpulse = 4f;
        [Min(0f)] [SerializeField] private float _boostDecayDuration = 0.2f;
        [Min(0f)] [SerializeField] private float _boostIgniteDuration = 0.08f;

        public float DodgeForce => _dodgeForce;
        public float DodgeForceAfterDodge => _dodgeForceAfterDodge;
        public float DodgeInvulnerabilityTime => _dodgeInvulnerabilityTime;
        public float DodgeCacheTime => _dodgeCacheTime;
        public float DodgeRechargeTime => _dodgeRechargeTime;
        public int MaxDodgeCharges => _maxDodgeCharges;
        public float SpeedModAfterDodge => _speedModAfterDodge;
        public float SpeedModAfterDodgeTime => _speedModAfterDodgeTime;
        public float TimeForActionAfterEndDodge => _timeForActionAfterEndDodge;
        public float BoostSpeedMultiplier => _boostSpeedMultiplier;
        public float AfterBoostDrag => _afterBoostDrag;
        public float DragChangeTime => _dragChangeTime;
        public float BoostStartImpulse => _boostStartImpulse;
        public float BoostDecayDuration => _boostDecayDuration;
        public float BoostIgniteDuration => _boostIgniteDuration;
    }
}
```

- [ ] **Step 3.4: Build compile check**

Run:

```bash
cd /Users/dada/Documents/GitHub/Project-Ark && dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly
```

Expected: `0 个错误`.

- [ ] **Step 3.5: Create profile assets in Unity**

In Unity:

```text
Assets/_Data/Ship > Create > ProjectArk > Ship > GG Replica > Visual Profile
Assets/_Data/Ship > Create > ProjectArk > Ship > GG Replica > Feel Profile
```

Name them:

```text
GGReplicaShipVisualProfile.asset
GGReplicaShipFeelProfile.asset
```

Fill Visual Profile with:

```text
Normal: Movement_10 / Movement_3 / Movement_21 / fade 0.2
Boost: Boost_2 / Boost_16 / Boost_8 / fade 0.2
Dodge: Movement_10 / Movement_3 / Movement_21 / fade 0
Fire: Primary_4 / Primary / Primary_6 / fade 0.2
FireBoost: Primary_4 / Primary / Primary_6 / fade 0
BackSprite: GrabGun_Back_3
CoreSprite: reactor
DodgeGhostSprite: player_test_fire
BoostIgniteClip: SND_PLAYER_BOOST_IGNITE
BoostLoopClip: SND_PLAYER_BOOST
DodgeClip: PLAYER_DODGE
FireClip: PLAYER_NORMAL_SHOT
```

- [ ] **Step 3.6: Checkpoint**

Do not commit unless the user explicitly approves. Suggested checkpoint:

```bash
git add Assets/Scripts/Ship/GGReplica/GGReplicaShipVisualProfileSO.cs Assets/Scripts/Ship/GGReplica/GGReplicaShipFeelProfileSO.cs Assets/_Data/Ship/GGReplicaShipVisualProfile.asset Assets/_Data/Ship/GGReplicaShipFeelProfile.asset
# git commit -m "feat: add GGReplica ship profiles"
```

---

## Task 4: Add runtime visual adapter

**Files:**
- Create: `Assets/Scripts/Ship/GGReplica/GGReplicaShipViewAdapter.cs`

- [ ] **Step 4.1: Create adapter script**

```csharp
using UnityEngine;

namespace ProjectArk.Ship
{
    public class GGReplicaShipViewAdapter : MonoBehaviour
    {
        [Header("Profile")]
        [SerializeField] private GGReplicaShipVisualProfileSO _profile;

        [Header("Sprite Layers")]
        [SerializeField] private SpriteRenderer _backRenderer;
        [SerializeField] private SpriteRenderer _liquidRenderer;
        [SerializeField] private SpriteRenderer _highlightRenderer;
        [SerializeField] private SpriteRenderer _solidRenderer;
        [SerializeField] private SpriteRenderer _coreRenderer;
        [SerializeField] private SpriteRenderer _dodgeGhostRenderer;

        [Header("State Sources")]
        [SerializeField] private ShipStateController _stateController;
        [SerializeField] private ShipBoost _boost;
        [SerializeField] private ShipDash _dash;

        private GGReplicaVisualState _currentState = GGReplicaVisualState.Normal;
        private bool _isFiring;

        public void SetFiring(bool isFiring)
        {
            _isFiring = isFiring;
            RefreshState();
        }

        public void ForceVisualState(GGReplicaVisualState state)
        {
            ApplyPack(state, force: true);
        }

        private void Awake()
        {
            if (_stateController == null) _stateController = GetComponent<ShipStateController>();
            if (_boost == null) _boost = GetComponent<ShipBoost>();
            if (_dash == null) _dash = GetComponent<ShipDash>();

            if (_profile == null)
                Debug.LogError("[GGReplicaShipViewAdapter] Missing visual profile.", this);
        }

        private void OnEnable()
        {
            if (_stateController != null) _stateController.OnStateChanged += HandleStateChanged;
            if (_dash != null) _dash.OnDashStarted += HandleDashStarted;
            if (_dash != null) _dash.OnDashEnded += HandleDashEnded;
            if (_boost != null) _boost.OnBoostStarted += HandleBoostChanged;
            if (_boost != null) _boost.OnBoostEnded += HandleBoostChanged;
            ApplyPersistentSprites();
            RefreshState();
        }

        private void OnDisable()
        {
            if (_stateController != null) _stateController.OnStateChanged -= HandleStateChanged;
            if (_dash != null) _dash.OnDashStarted -= HandleDashStarted;
            if (_dash != null) _dash.OnDashEnded -= HandleDashEnded;
            if (_boost != null) _boost.OnBoostStarted -= HandleBoostChanged;
            if (_boost != null) _boost.OnBoostEnded -= HandleBoostChanged;
        }

        private void HandleStateChanged(ShipShipState previous, ShipShipState current) => RefreshState();
        private void HandleDashStarted(Vector2 direction) => RefreshState();
        private void HandleDashEnded() => RefreshState();
        private void HandleBoostChanged() => RefreshState();

        private void ApplyPersistentSprites()
        {
            if (_profile == null) return;
            if (_backRenderer != null) _backRenderer.sprite = _profile.BackSprite;
            if (_coreRenderer != null) _coreRenderer.sprite = _profile.CoreSprite;
            if (_dodgeGhostRenderer != null)
            {
                _dodgeGhostRenderer.sprite = _profile.DodgeGhostSprite;
                _dodgeGhostRenderer.enabled = false;
            }
        }

        private void RefreshState()
        {
            if (_profile == null) return;
            var next = ResolveState();
            ApplyPack(next, force: false);
        }

        private GGReplicaVisualState ResolveState()
        {
            bool dashing = _dash != null && _dash.IsDashing;
            bool boosting = _boost != null && _boost.IsBoosting;

            if (dashing) return GGReplicaVisualState.Dodge;
            if (_isFiring && boosting) return GGReplicaVisualState.FireBoost;
            if (boosting) return GGReplicaVisualState.Boost;
            if (_isFiring) return GGReplicaVisualState.Fire;
            return GGReplicaVisualState.Normal;
        }

        private void ApplyPack(GGReplicaVisualState state, bool force)
        {
            if (!force && _currentState == state) return;
            if (_profile == null) return;
            if (!_profile.TryGetPack(state, out var pack))
            {
                Debug.LogWarning($"[GGReplicaShipViewAdapter] Missing sprite pack for {state}.", this);
                return;
            }

            _currentState = state;
            if (_solidRenderer != null) _solidRenderer.sprite = pack.SolidSprite;
            if (_liquidRenderer != null) _liquidRenderer.sprite = pack.LiquidSprite;
            if (_highlightRenderer != null) _highlightRenderer.sprite = pack.HighlightSprite;

            if (_solidRenderer != null) _solidRenderer.transform.localPosition = pack.SpritesOffset;
            if (_liquidRenderer != null) _liquidRenderer.transform.localPosition = pack.SpritesOffset;
            if (_highlightRenderer != null) _highlightRenderer.transform.localPosition = pack.SpritesOffset;

            if (_dodgeGhostRenderer != null)
                _dodgeGhostRenderer.enabled = state == GGReplicaVisualState.Dodge;
        }
    }
}
```

- [ ] **Step 4.2: Compile**

Run:

```bash
cd /Users/dada/Documents/GitHub/Project-Ark && dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly
```

Expected: `0 个错误`.

- [ ] **Step 4.3: Checkpoint**

Do not commit unless explicitly approved. Suggested checkpoint:

```bash
git add Assets/Scripts/Ship/GGReplica/GGReplicaShipViewAdapter.cs
# git commit -m "feat: add GGReplica ship visual adapter"
```

---

## Task 5: Add prefab builder for isolated replica prefab

**Files:**
- Create: `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaPrefabBuilder.cs`
- Create through tool: `Assets/_Prefabs/Ship/Ship_GGReplica.prefab`

- [ ] **Step 5.1: Create builder script**

Create the script with this implementation:

```csharp
using UnityEditor;
using UnityEngine;

namespace ProjectArk.Ship.Editor
{
    public static class GGReplicaPrefabBuilder
    {
        private const string SourceShipPrefab = "Assets/_Prefabs/Ship/Ship.prefab";
        private const string TargetPrefab = "Assets/_Prefabs/Ship/Ship_GGReplica.prefab";
        private const string VisualProfilePath = "Assets/_Data/Ship/GGReplicaShipVisualProfile.asset";

        [MenuItem("ProjectArk/Ship/GG Replica/Build Experimental Prefab")]
        public static void BuildExperimentalPrefab()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(SourceShipPrefab);
            if (source == null)
            {
                Debug.LogError($"[GGReplicaPrefabBuilder] Missing source prefab: {SourceShipPrefab}");
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            instance.name = "Ship_GGReplica";

            var visualProfile = AssetDatabase.LoadAssetAtPath<GGReplicaShipVisualProfileSO>(VisualProfilePath);
            if (visualProfile == null)
                Debug.LogError($"[GGReplicaPrefabBuilder] Missing visual profile: {VisualProfilePath}");

            EnsureAdapter(instance, visualProfile);
            PrefabUtility.SaveAsPrefabAsset(instance, TargetPrefab);
            Object.DestroyImmediate(instance);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[GGReplicaPrefabBuilder] Built {TargetPrefab}");
        }

        private static void EnsureAdapter(GameObject root, GGReplicaShipVisualProfileSO visualProfile)
        {
            var adapter = root.GetComponent<GGReplicaShipViewAdapter>();
            if (adapter == null) adapter = root.AddComponent<GGReplicaShipViewAdapter>();

            var visual = root.transform.Find("ShipVisual");
            if (visual == null)
            {
                Debug.LogError("[GGReplicaPrefabBuilder] Missing ShipVisual child on source Ship prefab.");
                return;
            }

            SetSerialized(adapter, "_profile", visualProfile);
            SetSerialized(adapter, "_backRenderer", FindRenderer(visual, "Ship_Sprite_Back"));
            SetSerialized(adapter, "_liquidRenderer", FindRenderer(visual, "Ship_Sprite_Liquid"));
            SetSerialized(adapter, "_highlightRenderer", FindRenderer(visual, "Ship_Sprite_HL"));
            SetSerialized(adapter, "_solidRenderer", FindRenderer(visual, "Ship_Sprite_Solid"));
            SetSerialized(adapter, "_coreRenderer", FindRenderer(visual, "Ship_Sprite_Core"));
            SetSerialized(adapter, "_dodgeGhostRenderer", FindRenderer(visual, "Dodge_Sprite"));
            SetSerialized(adapter, "_stateController", root.GetComponent<ShipStateController>());
            SetSerialized(adapter, "_boost", root.GetComponent<ShipBoost>());
            SetSerialized(adapter, "_dash", root.GetComponent<ShipDash>());
        }

        private static SpriteRenderer FindRenderer(Transform root, string childName)
        {
            var child = root.Find(childName);
            if (child == null)
            {
                Debug.LogError($"[GGReplicaPrefabBuilder] Missing child '{childName}' under {root.name}.");
                return null;
            }
            return child.GetComponent<SpriteRenderer>();
        }

        private static void SetSerialized(Object target, string propertyName, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(propertyName);
            if (prop == null)
            {
                Debug.LogError($"[GGReplicaPrefabBuilder] Missing serialized property '{propertyName}' on {target.name}.");
                return;
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }
    }
}
```

- [ ] **Step 5.2: Compile**

Run:

```bash
cd /Users/dada/Documents/GitHub/Project-Ark && dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly
```

Expected: `0 个错误`.

- [ ] **Step 5.3: Run builder**

In Unity:

```text
ProjectArk > Ship > GG Replica > Build Experimental Prefab
```

Expected: `Assets/_Prefabs/Ship/Ship_GGReplica.prefab` exists.

- [ ] **Step 5.4: Inspect prefab**

Open prefab and verify:

```text
Root name: Ship_GGReplica
Component exists: GGReplicaShipViewAdapter
Adapter _profile assigned
Adapter renderers assigned:
- _backRenderer
- _liquidRenderer
- _highlightRenderer
- _solidRenderer
- _coreRenderer
- _dodgeGhostRenderer
```

- [ ] **Step 5.5: Checkpoint**

Do not commit unless approved. Suggested checkpoint:

```bash
git add Assets/Scripts/Ship/Editor/GGReplica/GGReplicaPrefabBuilder.cs Assets/_Prefabs/Ship/Ship_GGReplica.prefab
# git commit -m "feat: build isolated GGReplica ship prefab"
```

---

## Task 6: Add VFX/audio adapters

**Files:**
- Create: `Assets/Scripts/Ship/GGReplica/GGReplicaBoostVfxAdapter.cs`
- Create: `Assets/Scripts/Ship/GGReplica/GGReplicaDashVfxAdapter.cs`

- [ ] **Step 6.1: Create Boost VFX adapter**

```csharp
using UnityEngine;

namespace ProjectArk.Ship
{
    public class GGReplicaBoostVfxAdapter : MonoBehaviour
    {
        [SerializeField] private GGReplicaShipVisualProfileSO _profile;
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private ShipBoost _boost;

        private void Awake()
        {
            if (_boost == null) _boost = GetComponent<ShipBoost>();
            if (_audioSource == null) _audioSource = GetComponent<AudioSource>();
        }

        private void OnEnable()
        {
            if (_boost != null)
            {
                _boost.OnBoostStarted += HandleBoostStarted;
                _boost.OnBoostEnded += HandleBoostEnded;
            }
        }

        private void OnDisable()
        {
            if (_boost != null)
            {
                _boost.OnBoostStarted -= HandleBoostStarted;
                _boost.OnBoostEnded -= HandleBoostEnded;
            }
            StopLoop();
        }

        private void HandleBoostStarted()
        {
            if (_profile == null || _audioSource == null) return;
            if (_profile.BoostIgniteClip != null)
                _audioSource.PlayOneShot(_profile.BoostIgniteClip);
            if (_profile.BoostLoopClip != null)
            {
                _audioSource.clip = _profile.BoostLoopClip;
                _audioSource.loop = true;
                _audioSource.Play();
            }
        }

        private void HandleBoostEnded() => StopLoop();

        private void StopLoop()
        {
            if (_audioSource == null) return;
            if (_audioSource.loop)
            {
                _audioSource.Stop();
                _audioSource.loop = false;
                _audioSource.clip = null;
            }
        }
    }
}
```

- [ ] **Step 6.2: Create Dash VFX adapter**

```csharp
using UnityEngine;

namespace ProjectArk.Ship
{
    public class GGReplicaDashVfxAdapter : MonoBehaviour
    {
        [SerializeField] private GGReplicaShipVisualProfileSO _profile;
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private ShipDash _dash;

        private void Awake()
        {
            if (_dash == null) _dash = GetComponent<ShipDash>();
            if (_audioSource == null) _audioSource = GetComponent<AudioSource>();
        }

        private void OnEnable()
        {
            if (_dash != null)
                _dash.OnDashStarted += HandleDashStarted;
        }

        private void OnDisable()
        {
            if (_dash != null)
                _dash.OnDashStarted -= HandleDashStarted;
        }

        private void HandleDashStarted(Vector2 direction)
        {
            if (_profile == null || _audioSource == null) return;
            if (_profile.DodgeClip != null)
                _audioSource.PlayOneShot(_profile.DodgeClip);
        }
    }
}
```

- [ ] **Step 6.3: Compile**

Run:

```bash
cd /Users/dada/Documents/GitHub/Project-Ark && dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly
```

Expected: `0 个错误`.

- [ ] **Step 6.4: Attach adapters to `Ship_GGReplica.prefab`**

In Unity prefab mode:

- Add `AudioSource` to root if missing.
- Add `GGReplicaBoostVfxAdapter`.
- Add `GGReplicaDashVfxAdapter`.
- Assign `_profile = GGReplicaShipVisualProfile.asset` on both.
- Assign `_audioSource` root AudioSource on both.

Expected: Boost and Dash play audio in Play Mode.

- [ ] **Step 6.5: Checkpoint**

Do not commit unless approved. Suggested checkpoint:

```bash
git add Assets/Scripts/Ship/GGReplica/GGReplicaBoostVfxAdapter.cs Assets/Scripts/Ship/GGReplica/GGReplicaDashVfxAdapter.cs Assets/_Prefabs/Ship/Ship_GGReplica.prefab
# git commit -m "feat: add GGReplica boost and dash audio adapters"
```

---

## Task 7: Add feel adapter

**Files:**
- Create: `Assets/Scripts/Ship/GGReplica/GGReplicaShipFeelAdapter.cs`

- [ ] **Step 7.1: Create feel adapter**

```csharp
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ProjectArk.Ship
{
    public class GGReplicaShipFeelAdapter : MonoBehaviour
    {
        [SerializeField] private GGReplicaShipFeelProfileSO _profile;
        [SerializeField] private Rigidbody2D _rigidbody;
        [SerializeField] private ShipDash _dash;
        [SerializeField] private ShipBoost _boost;

        private float _baseDrag;
        private bool _initialized;

        private void Awake()
        {
            if (_rigidbody == null) _rigidbody = GetComponent<Rigidbody2D>();
            if (_dash == null) _dash = GetComponent<ShipDash>();
            if (_boost == null) _boost = GetComponent<ShipBoost>();

            if (_rigidbody != null)
            {
                _baseDrag = _rigidbody.linearDamping;
                _initialized = true;
            }
        }

        private void OnEnable()
        {
            if (_dash != null) _dash.OnDashStarted += HandleDashStarted;
            if (_boost != null) _boost.OnBoostStarted += HandleBoostStarted;
            if (_boost != null) _boost.OnBoostEnded += HandleBoostEnded;
        }

        private void OnDisable()
        {
            if (_dash != null) _dash.OnDashStarted -= HandleDashStarted;
            if (_boost != null) _boost.OnBoostStarted -= HandleBoostStarted;
            if (_boost != null) _boost.OnBoostEnded -= HandleBoostEnded;
            RestoreDrag();
        }

        private void HandleDashStarted(Vector2 direction)
        {
            if (_profile == null || _rigidbody == null) return;
            _rigidbody.AddForce(direction.normalized * _profile.DodgeForceAfterDodge, ForceMode2D.Impulse);
            ApplyTemporaryDrag(_profile.AfterBoostDrag, _profile.SpeedModAfterDodgeTime).Forget();
        }

        private void HandleBoostStarted()
        {
            if (_profile == null || _rigidbody == null) return;
            Vector2 dir = transform.up;
            _rigidbody.AddForce(dir * _profile.BoostStartImpulse, ForceMode2D.Impulse);
            _rigidbody.linearDamping = _profile.AfterBoostDrag;
        }

        private void HandleBoostEnded()
        {
            if (_profile == null) return;
            ApplyTemporaryDrag(_baseDrag, _profile.BoostDecayDuration).Forget();
        }

        private async UniTaskVoid ApplyTemporaryDrag(float targetDrag, float duration)
        {
            if (_rigidbody == null) return;
            if (duration <= 0f)
            {
                _rigidbody.linearDamping = targetDrag;
                return;
            }

            float start = _rigidbody.linearDamping;
            float t = 0f;
            while (t < duration && _rigidbody != null)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);
                _rigidbody.linearDamping = Mathf.Lerp(start, targetDrag, p);
                await UniTask.Yield(destroyCancellationToken);
            }
        }

        private void RestoreDrag()
        {
            if (_initialized && _rigidbody != null)
                _rigidbody.linearDamping = _baseDrag;
        }
    }
}
```

- [ ] **Step 7.2: Compile**

Run:

```bash
cd /Users/dada/Documents/GitHub/Project-Ark && dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly
```

Expected: `0 个错误`.

- [ ] **Step 7.3: Attach to prefab**

In `Ship_GGReplica.prefab`:

- Add `GGReplicaShipFeelAdapter` to root.
- Assign `_profile = GGReplicaShipFeelProfile.asset`.
- Assign Rigidbody2D if not auto-filled.

Expected: Dodge/Boost produce visibly different drag/impulse from live ship.

- [ ] **Step 7.4: Checkpoint**

Do not commit unless approved. Suggested checkpoint:

```bash
git add Assets/Scripts/Ship/GGReplica/GGReplicaShipFeelAdapter.cs Assets/_Prefabs/Ship/Ship_GGReplica.prefab
# git commit -m "feat: add GGReplica feel adapter"
```

---

## Task 8: Build A/B test scene

**Files:**
- Create: `Assets/Scripts/Ship/GGReplica/GGReplicaTestSwitcher.cs`
- Create: `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaTestSceneBuilder.cs`
- Create via Unity: `Assets/Scenes/GGReplicaShipTest.unity`

- [ ] **Step 8.1: Create `GGReplicaTestSwitcher.cs`**

```csharp
using UnityEngine;

namespace ProjectArk.Ship
{
    public class GGReplicaTestSwitcher : MonoBehaviour
    {
        [SerializeField] private GameObject _liveShip;
        [SerializeField] private GameObject _replicaShip;
        [SerializeField] private GGReplicaShipViewAdapter _replicaView;

        private bool _replicaActive = true;

        private void Start()
        {
            ApplyActiveState();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                _replicaActive = !_replicaActive;
                ApplyActiveState();
            }

            if (_replicaView == null) return;
            if (Input.GetKeyDown(KeyCode.F2)) _replicaView.ForceVisualState(GGReplicaVisualState.Normal);
            if (Input.GetKeyDown(KeyCode.F3)) _replicaView.ForceVisualState(GGReplicaVisualState.Boost);
            if (Input.GetKeyDown(KeyCode.F4)) _replicaView.ForceVisualState(GGReplicaVisualState.Dodge);
            if (Input.GetKeyDown(KeyCode.F5)) _replicaView.ForceVisualState(GGReplicaVisualState.Fire);
            if (Input.GetKeyDown(KeyCode.F6)) _replicaView.ForceVisualState(GGReplicaVisualState.FireBoost);
        }

        private void ApplyActiveState()
        {
            if (_liveShip != null) _liveShip.SetActive(!_replicaActive);
            if (_replicaShip != null) _replicaShip.SetActive(_replicaActive);
        }
    }
}
```

- [ ] **Step 8.2: Create test scene builder**

```csharp
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ProjectArk.Ship.Editor
{
    public static class GGReplicaTestSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/GGReplicaShipTest.unity";
        private const string LiveShipPrefab = "Assets/_Prefabs/Ship/Ship.prefab";
        private const string ReplicaShipPrefab = "Assets/_Prefabs/Ship/Ship_GGReplica.prefab";

        [MenuItem("ProjectArk/Ship/GG Replica/Build Test Scene")]
        public static void BuildTestScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var livePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LiveShipPrefab);
            var replicaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ReplicaShipPrefab);
            if (livePrefab == null || replicaPrefab == null)
            {
                Debug.LogError("[GGReplicaTestSceneBuilder] Missing live or replica prefab.");
                return;
            }

            var live = (GameObject)PrefabUtility.InstantiatePrefab(livePrefab);
            live.name = "LiveShip_A";
            live.transform.position = new Vector3(-3f, 0f, 0f);

            var replica = (GameObject)PrefabUtility.InstantiatePrefab(replicaPrefab);
            replica.name = "GGReplicaShip_B";
            replica.transform.position = new Vector3(3f, 0f, 0f);

            var switcher = new GameObject("GGReplicaTestSwitcher");
            var component = switcher.AddComponent<GGReplicaTestSwitcher>();
            var so = new SerializedObject(component);
            so.FindProperty("_liveShip").objectReferenceValue = live;
            so.FindProperty("_replicaShip").objectReferenceValue = replica;
            so.FindProperty("_replicaView").objectReferenceValue = replica.GetComponent<GGReplicaShipViewAdapter>();
            so.ApplyModifiedPropertiesWithoutUndo();

            var cameraGo = new GameObject("Main Camera");
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 8f;
            cameraGo.tag = "MainCamera";

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            Debug.Log($"[GGReplicaTestSceneBuilder] Built {ScenePath}");
        }
    }
}
```

- [ ] **Step 8.3: Compile**

Run:

```bash
cd /Users/dada/Documents/GitHub/Project-Ark && dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly
```

Expected: `0 个错误`.

- [ ] **Step 8.4: Build scene**

In Unity:

```text
ProjectArk > Ship > GG Replica > Build Test Scene
```

Expected: `Assets/Scenes/GGReplicaShipTest.unity` exists.

- [ ] **Step 8.5: Play test**

Open scene and enter Play Mode.

Expected:

- `F1` toggles live/replica ship.
- `F2-F6` force replica visual states.
- Console has no missing reference errors.

- [ ] **Step 8.6: Checkpoint**

Do not commit unless approved. Suggested checkpoint:

```bash
git add Assets/Scripts/Ship/GGReplica/GGReplicaTestSwitcher.cs Assets/Scripts/Ship/Editor/GGReplica/GGReplicaTestSceneBuilder.cs Assets/Scenes/GGReplicaShipTest.unity
# git commit -m "test: add GGReplica ship A/B scene"
```

---

## Task 9: Add read-only auditor and final docs update

**Files:**
- Create: `Assets/Scripts/Ship/Editor/GGReplica/GGReplicaAuditor.cs`
- Modify: `Docs/2_TechnicalDesign/Ship/ShipVFX_AssetRegistry.md`
- Modify: `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- [ ] **Step 9.1: Create auditor**

```csharp
using UnityEditor;
using UnityEngine;

namespace ProjectArk.Ship.Editor
{
    public static class GGReplicaAuditor
    {
        private const string LiveShip = "Assets/_Prefabs/Ship/Ship.prefab";
        private const string ReplicaShip = "Assets/_Prefabs/Ship/Ship_GGReplica.prefab";

        [MenuItem("ProjectArk/Ship/GG Replica/Audit Replica Isolation")]
        public static void AuditReplicaIsolation()
        {
            var live = AssetDatabase.LoadAssetAtPath<GameObject>(LiveShip);
            var replica = AssetDatabase.LoadAssetAtPath<GameObject>(ReplicaShip);

            int errors = 0;
            if (live == null)
            {
                Debug.LogError($"[GGReplicaAuditor] Missing live ship prefab: {LiveShip}");
                errors++;
            }
            if (replica == null)
            {
                Debug.LogError($"[GGReplicaAuditor] Missing replica ship prefab: {ReplicaShip}");
                errors++;
            }
            if (replica != null && replica.GetComponent<GGReplicaShipViewAdapter>() == null)
            {
                Debug.LogError("[GGReplicaAuditor] Ship_GGReplica missing GGReplicaShipViewAdapter.");
                errors++;
            }
            if (live != null && live.GetComponent<GGReplicaShipViewAdapter>() != null)
            {
                Debug.LogError("[GGReplicaAuditor] Live Ship.prefab has GGReplicaShipViewAdapter. This violates isolation.");
                errors++;
            }

            if (errors == 0)
                Debug.Log("[GGReplicaAuditor] PASS: GGReplica is isolated from live Ship.prefab.");
            else
                Debug.LogError($"[GGReplicaAuditor] FAIL: {errors} issue(s) found.");
        }
    }
}
```

- [ ] **Step 9.2: Run auditor**

In Unity:

```text
ProjectArk > Ship > GG Replica > Audit Replica Isolation
```

Expected: `PASS: GGReplica is isolated from live Ship.prefab.`

- [ ] **Step 9.3: Update Asset Registry**

Append a `GGReplica Experimental Assets` section to `Docs/2_TechnicalDesign/Ship/ShipVFX_AssetRegistry.md`:

```markdown
## GGReplica Experimental Assets

| Canonical Name | Physical Name | Type | Status | Current Path | Owner | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| `GGReplicaShipPrefab` | `Ship_GGReplica.prefab` | Prefab | Reference | `Assets/_Prefabs/Ship/Ship_GGReplica.prefab` | `GGReplicaPrefabBuilder` | Experimental A/B replica; not live chain |
| `GGReplicaVisualProfile` | `GGReplicaShipVisualProfile.asset` | ScriptableObject | Reference | `Assets/_Data/Ship/GGReplicaShipVisualProfile.asset` | GGReplica | Five-state sprite/audio mapping |
| `GGReplicaFeelProfile` | `GGReplicaShipFeelProfile.asset` | ScriptableObject | Reference | `Assets/_Data/Ship/GGReplicaShipFeelProfile.asset` | GGReplica | Boost/Dodge feel tuning |
| `GGReplicaArtRoot` | `GGReplica/` | Art Folder | Reference | `Assets/_Art/Ship/GGReplica/` | GGReplica | Curated GG reference assets |
| `GGReplicaTestScene` | `GGReplicaShipTest.unity` | Scene | Reference | `Assets/Scenes/GGReplicaShipTest.unity` | GGReplica | A/B validation scene |
```

- [ ] **Step 9.4: Append ImplementationLog**

Append entry to `Docs/5_ImplementationLog/ImplementationLog_2026-05.md` summarizing phases completed, files created, and test results.

- [ ] **Step 9.5: Final validation**

Run:

```bash
cd /Users/dada/Documents/GitHub/Project-Ark && dotnet build Project-Ark.slnx -p:GenerateFullPaths=true -nologo -clp:ErrorsOnly
```

Expected: `0 个错误`.

In Unity:

```text
ProjectArk > Ship > GG Replica > Audit Replica Isolation
```

Expected: PASS.

Open `Assets/Scenes/GGReplicaShipTest.unity`, enter Play Mode, test:

```text
F1 toggles live/replica
F2 Normal
F3 Boost
F4 Dodge
F5 Fire
F6 FireBoost
```

Expected: all states visibly switch; no Console errors.

- [ ] **Step 9.6: Checkpoint**

Do not commit unless approved. Suggested checkpoint:

```bash
git add Assets/Scripts/Ship/Editor/GGReplica/GGReplicaAuditor.cs Docs/2_TechnicalDesign/Ship/ShipVFX_AssetRegistry.md Docs/5_ImplementationLog/ImplementationLog_2026-05.md
# git commit -m "docs: register GGReplica ship experiment"
```

---

## Self-review checklist

- Spec coverage:
  - Asset audit: Task 0.
  - Asset migration: Task 1.
  - Shader spike: Task 2.
  - Visual/feel profiles: Task 3.
  - Replica prefab: Task 5.
  - VFX/audio: Task 6.
  - Feel adapter: Task 7.
  - A/B test scene: Task 8.
  - Registry/docs/log: Task 9.
- No external `.meta` files copied.
- No task modifies `Assets/_Prefabs/Ship/Ship.prefab`.
- No task modifies `Assets/Scenes/SampleScene.unity`.
- Plan uses concrete file paths and commands.
- Git commit commands are commented out and require explicit user approval.
