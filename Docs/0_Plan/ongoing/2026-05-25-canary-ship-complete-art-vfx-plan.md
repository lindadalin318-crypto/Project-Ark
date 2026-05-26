# Canary Ship Complete Art/VFX Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 完成 Project Ark 主飞船“金丝雀号”的完整美术与 VFX 制作闭环，从 Minishoot 主轴的 Normal/Lean/Dash 可玩切片开始，最终覆盖 Boost、Fire、Hit、Weaving、Overheat、Unity 接入与验收。

**Architecture:** 本计划以 `Docs/3_WorkflowsAndRules/Ship/Ship_ArtVFX_Workflow.md` 为唯一制作流程来源，以 Minishoot 的飞船实现为主轴：`Body + Shape + Outline + Lean + Dash + Trail/Particle`。`Galactic Glitch` 只作为 appendix / optional reference，用于复杂状态图、材质参数和禁误用校验，不决定本轮主生产清单。正式接入仍遵守 `Docs/2_TechnicalDesign/Ship/ShipVFX_CanonicalSpec.md` 与 `Docs/2_TechnicalDesign/Ship/ShipVFX_AssetRegistry.md`。

**Tech Stack:** Unity 6000.3.7f1, URP 2D, SpriteRenderer, Animator / Sprite swap, TrailRenderer, ParticleSystem, PrimeTween, MaterialPropertyBlock, existing Ship/VFX runtime chain.

---

## 0. Source Documents And Non-Negotiable Rules

**Primary source:**

- `Docs/3_WorkflowsAndRules/Ship/Ship_ArtVFX_Workflow.md`

**Runtime / authority references:**

- `Docs/2_TechnicalDesign/Ship/ShipVFX_CanonicalSpec.md`
- `Docs/2_TechnicalDesign/Ship/ShipVFX_AssetRegistry.md`
- `Docs/3_WorkflowsAndRules/Implement_rules.md`

**Reference projects:**

- Main axis: `Minishoot`
- Optional appendix: `Galactic Glitch`

**Hard rules:**

- [ ] Do not start from GG-style `Solid / Liquid / Highlight / Core / Back` multi-state sprite sheets.
- [ ] Start from Minishoot-style `Body / Shape / Outline / Lean / Dash / Trail / Particle` playable set.
- [ ] Do not permanently patch scene instances to make the ship “look right”.
- [ ] Do not let debug tools become the formal runtime owner.
- [ ] Do not write runtime changes back into shared Materials or ScriptableObjects.
- [ ] Do not create or hand-edit `.meta` files.
- [ ] After every file creation/modification, append an entry to `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`.

---

## 1. Final Completion Definition

The whole ship is considered complete only when all conditions below are true.

### 1.1 Asset Completion

- [ ] `Normal` ship body is readable at `128 × 128`.
- [ ] `Body + Shape + Outline + Core` align perfectly with no pixel drift.
- [ ] Left and right Lean have at least 2 levels each; 3 levels each are preferred.
- [ ] Dash has at least 3 frames; 5 frames are preferred.
- [ ] Boost has distinct sustained propulsion visuals.
- [ ] Fire has short, readable muzzle/core feedback.
- [ ] Hit has short, readable damage feedback and pooled sparks.
- [ ] Weaving has a star-chart connection / ritual aura that is visually distinct from Boost.
- [ ] Overheat communicates danger without relying only on UI.
- [ ] All official PNGs have transparent background and clean alpha edges.
- [ ] All same-family ship sprites share size, PPU, pivot, and import settings.

### 1.2 Runtime Completion

- [ ] Unity can switch or blend `Idle / Lean / Dash / Boost / Fire / Hit / Weaving / Overheat` without residual color, alpha, scale, trail, or particle state.
- [ ] Debug tools can preview but do not drive the formal runtime chain.
- [ ] Pool-returned VFX reset transform, alpha, color, material parameters, trail state, particle emission, and runtime fields.
- [ ] Runtime uses Material instances or `MaterialPropertyBlock`; authored Materials and SO assets are not mutated.
- [ ] Missing key references produce visible error/audit output, not silent no-op.

### 1.3 Documentation Completion

- [ ] `ShipVFX_AssetRegistry.md` is updated if any new formal node, material, prefab, sprite path, or owner enters the live chain.
- [ ] `ShipVFX_CanonicalSpec.md` is updated if the live authority chain changes.
- [ ] `ImplementationLog_2026-05.md` records every file change.
- [ ] This ongoing plan is updated as batches are completed or descoped.

---

## 2. Target Folder Layout

### Task 1: Prepare Asset Folders

**Files / folders:**

- Create folder: `Assets/_Art/Ship/Canary/Source/Concepts/`
- Create folder: `Assets/_Art/Ship/Canary/Source/Layered/`
- Create folder: `Assets/_Art/Ship/Canary/Source/AI_Raw/`
- Create folder: `Assets/_Art/Ship/Canary/Sprites/Body/`
- Create folder: `Assets/_Art/Ship/Canary/Sprites/Shape/`
- Create folder: `Assets/_Art/Ship/Canary/Sprites/Outline/`
- Create folder: `Assets/_Art/Ship/Canary/Sprites/Core/`
- Create folder: `Assets/_Art/Ship/Canary/Sprites/EnergyBars/`
- Create folder: `Assets/_Art/Ship/Canary/Sprites/WeaponMount/`
- Create folder: `Assets/_Art/Ship/Canary/Sprites/Lean/`
- Create folder: `Assets/_Art/Ship/Canary/Sprites/Dash/`
- Create folder: `Assets/_Art/Ship/Canary/Textures/Masks/`
- Create folder: `Assets/_Art/Ship/Canary/Textures/Emission/`
- Create folder: `Assets/_Art/Ship/Canary/Textures/Noise/`
- Create folder: `Assets/_Art/Ship/Canary/Materials/`
- Create folder: `Assets/_Art/Ship/Canary/Shaders/`

- [x] **Step 1: Create folders in Unity or Finder**

Expected result: Unity generates `.meta` files automatically.

Status: Created on disk under `Assets/_Art/Ship/Canary/`. No `.meta` files were manually authored.

- [ ] **Step 2: Verify folder import in Unity**

Expected result: No manually authored `.meta` files; no missing folder warnings.

Status: Pending Unity Editor / AssetDatabase refresh verification.

- [x] **Step 3: Add ImplementationLog entry**

Record the folder creation and purpose.

---

## 3. Batch 0 — Minishoot Reference Lock

**Goal:** Before producing any final art, lock exactly what is being copied from Minishoot and what is not.

**Reference paths to inspect:**

- `Minishoot/DevXUnity/Sprite/`
- `Minishoot/DevXUnity/Texture2D/`
- `Minishoot/DevXUnity/Material/`
- `Minishoot/ExportedProject/Assets/AnimationClip/`
- `Minishoot/ExportedProject/Assets/Scripts/Assembly-CSharp/PlayerView.cs`

### Task 2: Build Reference Board

**Files:**

- Create: `Assets/_Art/Ship/Canary/Source/Concepts/canary_minishoot_reference_board.png`

- [x] **Step 1: Collect Minishoot references**

Include these visual categories:

```text
Player / __PlayerFull
PlayerOutline / SupershotPlayerOutline
PlayerLeanLeft1-3
PlayerLeanRight1-3
PlayerDash1-5
EnergyBars
Weapons
SpiritDashTrail
SpiritDashParticles
```

- [x] **Step 2: Annotate each reference category**

Each category must answer:

```text
What visual job does it perform?
What Project Ark asset will replace it?
Is it required for Batch 1, Batch 2, or later?
```

- [x] **Step 3: Reject non-goals**

Add a visible “Not in Batch 1” note for:

```text
GG-style full state sheet
Death / wreck ship
Complex shader parity
Final polish Bloom tuning
```

- [x] **Step 4: Verify reference board**

Expected result: A non-artist can look at the board and understand why `Body / Shape / Outline / Lean / Dash` come first.

Status: Created and visually checked `Assets/_Art/Ship/Canary/Source/Concepts/canary_minishoot_reference_board.png`. The board records each Minishoot reference category, its source file names, its visual job, its Project Ark replacement target, and its batch priority.

**Locked Minishoot reference mapping:**

| Minishoot category | Confirmed source references | Visual job | Project Ark replacement | Priority |
| --- | --- | --- | --- | --- |
| `Player / __PlayerFull` | `Minishoot/DevXUnity/Sprite/__PlayerFull.png` | Primary readable ship body; top-down silhouette, nose direction, playable footprint. | `Sprites/Body/spr_ship_canary_body_normal_albedo.png` + `Sprites/Shape/spr_ship_canary_shape_normal_mask.png` | Batch 1 |
| `PlayerOutline / SupershotPlayerOutline` | `Minishoot/DevXUnity/Sprite/SupershotPlayerOutline.png` | Readability halo / outline against mixed backgrounds. | `Sprites/Outline/spr_ship_canary_outline_normal_outline.png` | Batch 1 |
| `PlayerLeanLeft1-3` | `PlayerLeanLeft1.png`, `PlayerLeanLeft2.png`, `PlayerLeanLeft3.png` | Left movement banking and lateral thrust read. | `Sprites/Lean/spr_ship_canary_lean_left_01-03.png` | Batch 1 |
| `PlayerLeanRight1-3` | `PlayerLeanRight1.png`, `PlayerLeanRight2.png`, `PlayerLeanRight3.png` | Right movement banking and symmetric lateral thrust read. | `Sprites/Lean/spr_ship_canary_lean_right_01-03.png` | Batch 1 |
| `PlayerDash1-5` | `PlayerDash1.png` through `PlayerDash5.png`; animation refs include `PlayerDash.anim`, `PlayerDashHalf.anim`, `PlayerDashMenu.anim` | Dash pose / smear language before runtime trail polish. | `Sprites/Dash/spr_ship_canary_dash_01-05.png` | Batch 1/2 |
| `EnergyBars` | `EnergyBar.png`, `EnergyBarFill.png`, `EnergyEmpty.png`; runtime ref: `PlayerEnergyView.energyBars` | Small readable energy frame/fill/empty meter structure. | `Sprites/EnergyBars/spr_ship_canary_energy_bar_frame/fill/empty.png` | Batch 2 |
| `Weapons` | `Weapon0.png` through `Weapon4.png` | Weapon attachment silhouette and scale reference only; not identity copy. | `Sprites/WeaponMount/spr_ship_canary_weapon_mount_*.png` | Batch 2/later |
| `SpiritDashTrail / SpiritDashParticles` | `SpiritDash.png`; runtime refs in `PlayerView.cs`: `SpiritDashParticles`, `SpiritDashTrail`; materials include `SpiritDashShadow.mat`, `PlayerTrail.mat`, `PlayerTrail1.mat`, `PlayerTrailBoostSide.mat` | Runtime dash trail / particles reference, not a Batch 1 body requirement. | Later Ship/VFX TrailRenderer / ParticleSystem profile under existing authority rules. | Later |

**Not in Batch 1:** GG-style full state sheet, death / wreck ship, complex shader parity, final polish Bloom tuning.

---

## 4. Batch 1 — Normal Playable Set

**Goal:** Produce the minimum playable ship asset set that can stand in Unity and support Idle, Lean, and Dash preparation.

### Task 3: Create Main Body Sprite

**Files:**

- Create: `Assets/_Art/Ship/Canary/Sprites/Body/spr_ship_canary_body_normal_albedo.png`
- Optional source: `Assets/_Art/Ship/Canary/Source/Layered/ship_canary_body_normal_source.psd`

- [x] **Step 1: Produce source art at 1024 × 1024**

Requirements:

```text
Transparent background
Ship nose points up
Ship centered on canvas
Body occupies roughly 55%-70% canvas height
At least 10% top margin
At least 20% bottom margin
No baked background
No baked long tail flame
No baked Dash smear
```

Status: Accepted for current production pass based on the existing `spr_ship_canary_body_normal_albedo.png`.

- [x] **Step 2: Export official PNG at 512 × 512**

Expected file name:

```text
spr_ship_canary_body_normal_albedo.png
```

Status: Accepted as the official Batch 1 body sprite for now.

- [x] **Step 3: Run readability check**

Check on black, white, and deep-blue backgrounds.

Expected result:

```text
At 128 × 128, direction is readable.
At 512 × 512, silhouette is clean.
No dirty alpha edge.
```

Status: Passed by decision. Earlier background/layout concerns are explicitly deferred so production can continue from this body.

- [x] **Step 4: Decide whether this becomes the source for all derivative frames**

Expected result: This body sprite is stable enough to copy into Lean and Dash frames.

Status: Approved as the current source for Shape, Outline, Lean, and Dash derivative work.

### Task 4: Create Shape Mask

**Files:**

- Create: `Assets/_Art/Ship/Canary/Sprites/Shape/spr_ship_canary_shape_normal_mask.png`

- [x] **Step 1: Derive mask from body silhouette**

Requirements:

```text
Same canvas size as body
Same pivot assumption as body
White/gray ship body area
Black/transparent non-ship area
Less detail than body
```

Status: Completed with `spr_ship_canary_shape_normal_mask.png` moved into the official `Sprites/Shape/` target folder.

- [x] **Step 2: Verify overlay alignment**

Expected result: `Body + Shape` overlap with no offset.

Status: Accepted for this production pass. Canvas size is 512 × 512 with alpha channel, and the mask follows the approved body silhouette.

- [x] **Step 3: Verify mask usefulness**

Expected result: Mask can support hit flash, dissolve, tinting, or overheat later.

Status: Passed. The mask is a clean white ship silhouette on transparent background and can be used by later shader/VFX effects.

### Task 5: Create Outline Sprite

**Files:**

- Create: `Assets/_Art/Ship/Canary/Sprites/Outline/spr_ship_canary_outline_normal_outline.png`

Status: Completed with `spr_ship_canary_outline_normal_outline.png` accepted in the official `Sprites/Outline/` target folder.

- [x] **Step 1: Generate outline from final body silhouette**

Requirements:

```text
Same canvas size as body
Same pivot assumption as body
Clear outer contour
No thick UI-like border
Readable on dark and bright backgrounds
```

Status: Passed. The exported PNG is 512 × 512 RGBA with alpha channel and uses a clean cyan outer contour based on the Canary ship silhouette.

- [x] **Step 2: Test outline alone**

Expected result: A player can still infer ship boundary and facing direction.

Status: Passed. The outline alone clearly communicates the ship boundary and forward direction without text, watermark, background, or stray line artifacts.

- [x] **Step 3: Test `Body + Shape + Outline` stack**

Expected result: No offset, no jitter, no mismatched edge pixels.

Status: Accepted for this production pass. The outline uses the same 512 × 512 canvas and aligned silhouette assumption as the approved Body and Shape assets.

### Task 6: Create Core / EnergyBars / WeaponMount

**Files:**

- Create: `Assets/_Art/Ship/Canary/Sprites/Core/spr_ship_canary_core_normal_albedo.png`
- Create: `Assets/_Art/Ship/Canary/Sprites/EnergyBars/spr_ship_canary_energybar_left_normal_albedo.png`
- Create: `Assets/_Art/Ship/Canary/Sprites/EnergyBars/spr_ship_canary_energybar_right_normal_albedo.png`
- Create: `Assets/_Art/Ship/Canary/Sprites/WeaponMount/spr_ship_canary_weapon_mount_normal_albedo.png`

- [x] **Step 1: Create core asset**

Expected result: Small but clear energy focus, not too bright in Normal.

- [ ] **Step 2: Create left/right energy bars** — Skipped for now; revisit after the WeaponMount pass.

Expected result: Both can be independently animated, rotated, or tinted.

- [ ] **Step 3: Create weapon mount marker or sprite** — Current next step.

Expected result: Fire VFX has a consistent muzzle origin.

- [ ] **Step 4: Verify stack**

Expected result:

```text
Body + Shape + Outline + Core + EnergyBars + WeaponMount
all align without visual drift.
```

### Batch 1 Completion Gate

- [ ] Body readable at 128px.
- [ ] Shape and Outline align with Body.
- [ ] Core and EnergyBars do not overpower the body.
- [ ] WeaponMount gives a clear muzzle reference.
- [ ] All official PNGs use transparent background.
- [ ] All files follow workflow naming exactly.

---

## 5. Batch 2 — Lean Frames

**Goal:** Add Minishoot-style left/right movement polish without redesigning the ship.

### Task 7: Create Lean Left Frames

**Files:**

- Create: `Assets/_Art/Ship/Canary/Sprites/Lean/spr_ship_canary_lean_left_01.png`
- Create: `Assets/_Art/Ship/Canary/Sprites/Lean/spr_ship_canary_lean_left_02.png`
- Create: `Assets/_Art/Ship/Canary/Sprites/Lean/spr_ship_canary_lean_left_03.png`

- [ ] **Step 1: Duplicate body source**

Expected result: Same canvas, center, pivot, and scale as body.

- [ ] **Step 2: Create light left lean**

Expected result: `lean_left_01` shows subtle left turn / weight shift.

- [ ] **Step 3: Create medium left lean**

Expected result: `lean_left_02` is visibly stronger but still the same ship.

- [ ] **Step 4: Create strong left lean**

Expected result: `lean_left_03` is optional but preferred for full polish.

### Task 8: Create Lean Right Frames

**Files:**

- Create: `Assets/_Art/Ship/Canary/Sprites/Lean/spr_ship_canary_lean_right_01.png`
- Create: `Assets/_Art/Ship/Canary/Sprites/Lean/spr_ship_canary_lean_right_02.png`
- Create: `Assets/_Art/Ship/Canary/Sprites/Lean/spr_ship_canary_lean_right_03.png`

- [ ] **Step 1: Duplicate body source**

Expected result: Same canvas, center, pivot, and scale as body.

- [ ] **Step 2: Create light right lean**

Expected result: `lean_right_01` shows subtle right turn / weight shift.

- [ ] **Step 3: Create medium right lean**

Expected result: `lean_right_02` is visibly stronger but still the same ship.

- [ ] **Step 4: Create strong right lean**

Expected result: `lean_right_03` is optional but preferred for full polish.

### Task 9: Validate Lean Sequence

**Files:**

- Use files from Tasks 3, 7, and 8.

- [ ] **Step 1: Flip through Idle → LeanLeft1 → LeanLeft2 → LeanLeft3**

Expected result: No jump, no scale change, no new silhouette identity.

- [ ] **Step 2: Flip through Idle → LeanRight1 → LeanRight2 → LeanRight3**

Expected result: No jump, no scale change, no new silhouette identity.

- [ ] **Step 3: Rapidly alternate left and right**

Expected result: Direction feedback is readable but not noisy.

### Batch 2 Completion Gate

- [ ] At least 2 left lean frames exist.
- [ ] At least 2 right lean frames exist.
- [ ] 3 frames per side exist or are explicitly deferred.
- [ ] All Lean frames align with Body.
- [ ] Lean works at 128px.

---

## 6. Batch 3 — Dash / Dodge Frames And Preview VFX

**Goal:** Create Minishoot-style short Dash frames that read as instant dodge, not sustained Boost.

### Task 10: Create Dash Frames

**Files:**

- Create: `Assets/_Art/Ship/Canary/Sprites/Dash/spr_ship_canary_dash_01.png`
- Create: `Assets/_Art/Ship/Canary/Sprites/Dash/spr_ship_canary_dash_02.png`
- Create: `Assets/_Art/Ship/Canary/Sprites/Dash/spr_ship_canary_dash_03.png`
- Create: `Assets/_Art/Ship/Canary/Sprites/Dash/spr_ship_canary_dash_04.png`
- Create: `Assets/_Art/Ship/Canary/Sprites/Dash/spr_ship_canary_dash_05.png`
- Create: `Assets/_Art/Ship/Canary/Textures/Masks/spr_ship_canary_dash_shape_mask.png`

- [ ] **Step 1: Create dash_01 start compression**

Expected result: First frame feels like start of instant movement.

- [ ] **Step 2: Create dash_02 stretch**

Expected result: Ship elongates or smears but remains recognizable.

- [ ] **Step 3: Create dash_03 strongest shape**

Expected result: Strongest motion frame, still readable at 128px.

- [ ] **Step 4: Create dash_04 recovery**

Expected result: Shape moves back toward normal.

- [ ] **Step 5: Create dash_05 end frame**

Expected result: Clean return bridge to idle.

- [ ] **Step 6: Create dash shape mask**

Expected result: Can drive ghost fade, dissolve, or afterimage material.

### Task 11: Create Dash Materials And Preview VFX

**Files:**

- Create in Unity: `Assets/_Art/Ship/Canary/Materials/mat_ship_canary_dash.mat`
- Create in Unity: `Assets/_Art/Ship/Canary/Materials/mat_ship_canary_trail.mat`
- Create prefab: `Assets/_Prefabs/VFX/Ship/prefab_ship_canary_trail_preview.prefab`
- Create prefab: `Assets/_Prefabs/VFX/Ship/prefab_ship_canary_dash_particles.prefab`

- [ ] **Step 1: Create dash material**

Expected result: Supports short fade / tint without editing shared runtime material during play.

- [ ] **Step 2: Create trail preview prefab**

Expected result: Trail can be enabled briefly for Dash.

- [ ] **Step 3: Create dash particle preview prefab**

Expected result: One-shot particles support the dash start or end.

- [ ] **Step 4: Verify reset behavior**

Expected result: Trail and particles can be reset cleanly when preview stops.

### Batch 3 Completion Gate

- [ ] Dash has at least 3 approved frames.
- [ ] Dash has 5 frames or a written deferral note.
- [ ] Dash reads differently from Boost.
- [ ] Dash can play in 0.15-0.35 seconds without jump.
- [ ] Trail and particle previews do not replace sprite readability.

---

## 7. Batch 4 — Unity Import And Preview Prefab

**Goal:** Bring Batch 1-3 assets into Unity safely before touching the formal `Ship.prefab` live chain.

### Task 12: Import Settings Pass

**Files:**

- Modify import settings for all files under `Assets/_Art/Ship/Canary/Sprites/`

- [ ] **Step 1: Set Texture Type**

Expected value:

```text
Sprite (2D and UI)
```

- [ ] **Step 2: Set Sprite Mode**

Expected value:

```text
Single
```

- [ ] **Step 3: Set Pivot**

Expected value:

```text
Center
```

- [ ] **Step 4: Set PPU**

Expected result: Same as current `Ship.prefab` ship sprite chain.

- [ ] **Step 5: Disable mip maps for sprites**

Expected result: No unwanted blur or mip artifacts.

### Task 13: Create Canary Preview Prefab

**Files:**

- Create prefab: `Assets/_Prefabs/Ship/CanaryShipVisualPreview.prefab`

- [ ] **Step 1: Create root GameObject**

Expected hierarchy:

```text
CanaryShipVisualPreview
├── Body
├── Shape
├── Outline
├── Core
├── EnergyBar_L
├── EnergyBar_R
├── WeaponMount
├── DashTrailPreview
└── DashParticlesPreview
```

- [ ] **Step 2: Assign SpriteRenderers**

Expected result: Each visual layer has its own SpriteRenderer and sorting order.

- [ ] **Step 3: Assign materials**

Expected materials:

```text
mat_ship_canary_body_default
mat_ship_canary_shape
mat_ship_canary_outline
mat_ship_canary_dash
mat_ship_canary_trail
```

- [ ] **Step 4: Verify black/white/deep-blue background readability**

Expected result: Outline keeps the ship readable in all three backgrounds.

### Task 14: Create Preview Animation Clips

**Files:**

- Create animation clip: `Assets/_Art/Ship/Canary/Animations/Canary_Idle.anim`
- Create animation clip: `Assets/_Art/Ship/Canary/Animations/Canary_LeanLeft.anim`
- Create animation clip: `Assets/_Art/Ship/Canary/Animations/Canary_LeanRight.anim`
- Create animation clip: `Assets/_Art/Ship/Canary/Animations/Canary_Dash.anim`

- [ ] **Step 1: Create animation folder if needed**

Expected folder:

```text
Assets/_Art/Ship/Canary/Animations/
```

- [ ] **Step 2: Create Idle clip**

Expected result: Stable base body, optional subtle energy/core pulse.

- [ ] **Step 3: Create LeanLeft clip**

Expected result: Sprite swaps or child transforms show left movement polish.

- [ ] **Step 4: Create LeanRight clip**

Expected result: Sprite swaps or child transforms show right movement polish.

- [ ] **Step 5: Create Dash clip**

Expected result: `dash_01 → dash_05` plays in 0.15-0.35 seconds.

### Batch 4 Completion Gate

- [ ] Preview prefab exists and is separate from formal `Ship.prefab`.
- [ ] All sprites import with consistent PPU and pivot.
- [ ] Idle / Lean / Dash preview works.
- [ ] No formal Ship/VFX authority chain has been changed yet.

---

## 8. Batch 5 — Boost Assets

**Goal:** Add sustained propulsion visuals without confusing them with Dash.

### Task 15: Create Boost Assets

**Files:**

- Create: `Assets/_Art/Ship/Canary/Textures/Emission/spr_ship_canary_core_boost_emission.png`
- Create: `Assets/_Art/Ship/Canary/Textures/Emission/spr_ship_canary_energybar_boost_emission.png`
- Create: `Assets/_Art/Ship/Canary/Sprites/Body/spr_ship_canary_engine_boost_albedo.png`
- Optional create: `Assets/_Art/Ship/Canary/Textures/Masks/tex_boost_trail_noise_mask.png`

- [ ] **Step 1: Create core boost emission**

Expected result: Core visibly powers up but does not look like explosion.

- [ ] **Step 2: Create energybar boost emission**

Expected result: EnergyBars become stronger and support sustained movement.

- [ ] **Step 3: Create engine boost albedo**

Expected result: Rear engine/nozzle area reads as active.

- [ ] **Step 4: Verify against Dash**

Expected result: Boost reads as sustained propulsion; Dash reads as short flash movement.

### Task 16: Connect Boost To Existing BoostTrail Authority

**Files:**

- Read before modifying: `Docs/2_TechnicalDesign/Ship/ShipVFX_CanonicalSpec.md`
- Read before modifying: `Docs/2_TechnicalDesign/Ship/ShipVFX_AssetRegistry.md`
- Existing prefab authority: `Assets/_Prefabs/VFX/BoostTrailRoot.prefab`

- [ ] **Step 1: Confirm existing BoostTrail owner**

Expected result: No new second BoostTrail authority is introduced.

- [ ] **Step 2: Decide whether Canary boost assets stay preview-only or enter live chain**

Expected result: Decision is recorded in this plan and in `ShipVFX_AssetRegistry.md` if live.

- [ ] **Step 3: If live, update registry before or with prefab integration**

Expected result: Asset owner, path, status, and runtime consumer are explicit.

### Batch 5 Completion Gate

- [ ] Boost assets exist.
- [ ] Boost is distinct from Dash.
- [ ] Existing `BoostTrailRoot` remains the formal trail authority unless registry says otherwise.
- [ ] No scene-only patch is used as permanent Boost solution.

---

## 9. Batch 6 — Fire And Hit Assets

**Goal:** Add short combat feedback without replacing the whole ship.

### Task 17: Create Fire Assets

**Files:**

- Create: `Assets/_Art/Ship/Canary/Textures/Emission/spr_ship_canary_core_fire_emission.png`
- Create: `Assets/_Art/VFX/Ship/spr_vfx_canary_muzzle_flash_01.png`
- Optional create: `Assets/_Art/VFX/Ship/spr_vfx_canary_muzzle_flash_mask.png`
- Optional create: `Assets/_Art/VFX/Ship/spr_vfx_canary_muzzle_spark_01.png`

- [ ] **Step 1: Create core fire emission**

Expected result: Short pulse energy, not sustained weaving aura.

- [ ] **Step 2: Create muzzle flash**

Expected result: Flash is short, bright, transparent background, readable but not screen-filling.

- [ ] **Step 3: Verify weapon mount alignment**

Expected result: Muzzle flash emerges from `WeaponMount` location.

### Task 18: Create Hit Assets

**Files:**

- Create: `Assets/_Art/Ship/Canary/Textures/Masks/spr_ship_canary_shape_hit_mask.png`
- Create: `Assets/_Art/VFX/Ship/spr_vfx_canary_hit_spark_01.png`
- Optional create: `Assets/_Art/VFX/Ship/spr_vfx_canary_hit_spark_02.png`

- [ ] **Step 1: Create hit mask**

Expected result: Flash affects important edges and core area, not full flat white rectangle.

- [ ] **Step 2: Create hit spark 01**

Expected result: Small, bright, sharp, not bigger than 25% of ship length.

- [ ] **Step 3: Create optional hit spark 02**

Expected result: Gives particle variation without adding noise.

- [ ] **Step 4: Verify pooled VFX reset requirements**

Expected result: Any runtime VFX using sparks resets color, alpha, scale, emission, and lifetime state.

### Batch 6 Completion Gate

- [ ] Fire is short and does not alter body identity.
- [ ] Hit is short and does not look like Overheat.
- [ ] VFX assets are compatible with object pooling.
- [ ] Muzzle flash aligns with WeaponMount.

---

## 10. Batch 7 — Weaving Assets

**Goal:** Make the ship visually connect to the StarChart / weaving state in a way that differs from Boost.

### Task 19: Create Weaving Assets

**Files:**

- Create: `Assets/_Art/Ship/Canary/Textures/Emission/spr_ship_canary_core_weaving_emission.png`
- Create: `Assets/_Art/Ship/Canary/Textures/Emission/spr_ship_canary_aura_weaving_emission.png`
- Create: `Assets/_Art/Ship/Canary/Textures/Masks/tex_ship_canary_weaving_ring_mask.png`
- Create: `Assets/_Art/Ship/Canary/Textures/Noise/tex_ship_canary_weaving_noise_mask.png`

- [ ] **Step 1: Create core weaving emission**

Expected result: Core feels opened / connected, not merely brighter.

- [ ] **Step 2: Create aura weaving emission**

Expected result: Aura supports ritual/star-chart feeling without covering gameplay.

- [ ] **Step 3: Create weaving ring mask**

Expected result: Ring can scale/pulse around the ship.

- [ ] **Step 4: Create weaving noise mask**

Expected result: Noise can drive subtle energy movement.

### Task 20: Preview Weaving State

**Files:**

- Modify preview prefab: `Assets/_Prefabs/Ship/CanaryShipVisualPreview.prefab`
- Optional create prefab: `Assets/_Prefabs/VFX/Ship/prefab_ship_canary_weaving_aura_preview.prefab`

- [ ] **Step 1: Add weaving aura preview child**

Expected result: Aura can be enabled/disabled without replacing body.

- [ ] **Step 2: Test with Bloom off**

Expected result: State is still readable.

- [ ] **Step 3: Test with Bloom on**

Expected result: Aura does not swallow ship outline or enemy/bullet readability.

### Batch 7 Completion Gate

- [ ] Weaving is visually distinct from Boost.
- [ ] Weaving suggests StarChart connection.
- [ ] Aura does not block gameplay information.
- [ ] Exit preview leaves no postprocess/material residue.

---

## 11. Batch 8 — Overheat Assets

**Goal:** Communicate heat danger through ship visuals without relying only on HUD.

### Task 21: Create Overheat Assets

**Files:**

- Create: `Assets/_Art/Ship/Canary/Textures/Emission/spr_ship_canary_core_overheat_emission.png`
- Create: `Assets/_Art/Ship/Canary/Textures/Masks/spr_ship_canary_shape_overheat_mask.png`
- Create: `Assets/_Art/Ship/Canary/Textures/Noise/tex_ship_canary_overheat_noise_mask.png`
- Create: `Assets/_Art/VFX/Ship/spr_vfx_canary_overheat_spark_01.png`

- [ ] **Step 1: Create core overheat emission**

Expected result: Core turns dangerous orange/red, but not permanently pure red.

- [ ] **Step 2: Create shape overheat mask**

Expected result: Controls which parts of the ship show heat stress.

- [ ] **Step 3: Create overheat noise mask**

Expected result: Supports heat shimmer / unstable energy if shader uses it.

- [ ] **Step 4: Create overheat spark**

Expected result: Small warning sparks, visually distinct from Hit sparks.

### Batch 8 Completion Gate

- [ ] Overheat reads as danger/rising heat.
- [ ] Overheat is not confused with Hit.
- [ ] Recovery can return visuals to Normal.
- [ ] No authored material or SO is runtime-mutated.

---

## 12. Batch 9 — Material And Shader Pass

**Goal:** Create the first stable material set for the ship and VFX previews.

### Task 22: Create Required Materials

**Files:**

- Create in Unity: `Assets/_Art/Ship/Canary/Materials/mat_ship_canary_body_default.mat`
- Create in Unity: `Assets/_Art/Ship/Canary/Materials/mat_ship_canary_shape.mat`
- Create in Unity: `Assets/_Art/Ship/Canary/Materials/mat_ship_canary_outline.mat`
- Create in Unity: `Assets/_Art/Ship/Canary/Materials/mat_ship_canary_core_default.mat`
- Create in Unity: `Assets/_Art/Ship/Canary/Materials/mat_ship_canary_dash.mat`
- Create in Unity: `Assets/_Art/Ship/Canary/Materials/mat_ship_canary_trail.mat`
- Create in Unity: `Assets/_Art/Ship/Canary/Materials/mat_vfx_canary_dash_particles.mat`
- Create in Unity: `Assets/_Art/Ship/Canary/Materials/mat_vfx_canary_muzzle_flash.mat`

- [ ] **Step 1: Create body material**

Expected result: Conservative tint/brightness; readable without Bloom.

- [ ] **Step 2: Create shape material**

Expected result: Can support tint, hit flash, dissolve, or overheat mask later.

- [ ] **Step 3: Create outline material**

Expected result: Outline remains readable on multiple backgrounds.

- [ ] **Step 4: Create VFX materials**

Expected result: Additive-style VFX are bright but not screen-washing.

### Task 23: Define Runtime Material Parameter Rules

**Files:**

- Modify if live chain changes: `Docs/2_TechnicalDesign/Ship/ShipVFX_AssetRegistry.md`
- Optional create tuning asset later under: `Assets/_Data/Ship/`

- [ ] **Step 1: List parameters per material**

Required minimum:

```text
body: tint, brightness
shape: shapeTint, hitFlashAmount, dissolveAmount
outline: outlineColor, outlineAlpha, outlineWidth
dash: alpha, stretchTint, fadeDuration
trail: trailColor, lifetime, widthCurve
muzzle: lifetime, additiveIntensity, colorFamily
```

- [ ] **Step 2: Decide where parameters live**

Expected result: Runtime tuning lives in a data asset or registry, not hardcoded random values.

- [ ] **Step 3: Document no shared Material mutation**

Expected result: Runtime changes use material instances or `MaterialPropertyBlock`.

### Batch 9 Completion Gate

- [ ] Required materials exist.
- [ ] Parameters have an owner.
- [ ] Runtime mutation policy is explicit.
- [ ] Material choices do not require GG-style state sheet to function.

---

## 13. Batch 10 — Formal Ship/VFX Integration

**Goal:** Decide and implement how the finished Canary visual set enters the formal ship runtime chain.

### Task 24: Authority Review Before Integration

**Files to read before touching prefabs:**

- `Docs/2_TechnicalDesign/Ship/ShipVFX_CanonicalSpec.md`
- `Docs/2_TechnicalDesign/Ship/ShipVFX_AssetRegistry.md`
- `Docs/3_WorkflowsAndRules/Implement_rules.md`

- [ ] **Step 1: Identify live runtime owner for each visual state**

Expected table:

```text
Idle -> owner
Lean -> owner
Dash -> owner
Boost -> owner
Fire -> owner
Hit -> owner
Weaving -> owner
Overheat -> owner
```

- [ ] **Step 2: Identify prefab owner**

Expected result: `Ship.prefab` structure changes only happen through the approved authority path.

- [ ] **Step 3: Identify scene-only references**

Expected result: Any scene-only reference has a binder or manual step; no runtime fallback secretly repairs it.

- [ ] **Step 4: Decide preview-only vs live integration**

Expected result: Decision recorded in this plan before editing the formal prefab.

### Task 25: Update Asset Registry If Live

**Files:**

- Modify: `Docs/2_TechnicalDesign/Ship/ShipVFX_AssetRegistry.md`

- [ ] **Step 1: Add new asset paths**

Expected result: Every formal sprite, material, prefab, and VFX asset has a registry entry.

- [ ] **Step 2: Add owner and status**

Expected result: Each entry states whether it is preview, live, optional, or deprecated.

- [ ] **Step 3: Add runtime consumer**

Expected result: Each live asset states which script/prefab consumes it.

### Task 26: Integrate Into Ship Prefab Or Keep Preview Prefab

**Files:**

- Preferred preview prefab: `Assets/_Prefabs/Ship/CanaryShipVisualPreview.prefab`
- Live prefab only if approved by registry/spec: `Assets/_Prefabs/Ship/Ship.prefab`

- [ ] **Step 1: If preview-only, do not modify `Ship.prefab`**

Expected result: Preview prefab proves visuals without touching live chain.

- [ ] **Step 2: If live, update through official prefab authority**

Expected result: No manual scene instance patch becomes permanent.

- [ ] **Step 3: Run visual state checks**

Expected result:

```text
Idle works
Lean works
Dash works
Boost works
Fire works
Hit works
Weaving works
Overheat works
```

### Batch 10 Completion Gate

- [ ] Integration path is explicit.
- [ ] Registry/spec are synchronized if the live chain changed.
- [ ] Debug is not the formal owner.
- [ ] No new fallback/legacy path is introduced without written approval.

---

## 14. Batch 11 — Final Validation Scene / Checklist

**Goal:** Prove the entire ship works as a complete gameplay asset, not just as a folder of images.

### Task 27: Build Validation View

**Files:**

- Preferred: create or reuse a ship visual validation scene/prefab under project-approved test/preview location.
- Do not create new live scene dependencies without documenting them.

- [ ] **Step 1: Add quick state toggles**

Required toggles:

```text
Normal
Lean Left
Lean Right
Dash
Boost
Fire
Hit
Weaving
Overheat
```

- [ ] **Step 2: Add background toggles**

Required backgrounds:

```text
Black
White
Deep blue
```

- [ ] **Step 3: Add Bloom toggle if available**

Expected result: Visuals are readable with Bloom on and off.

### Task 28: Run Final State Matrix

**Files:**

- Use preview or live integrated ship.

- [ ] **Step 1: Normal check**

Expected result: Ship body and direction are clear.

- [ ] **Step 2: Lean check**

Expected result: Movement polish is readable and non-jittery.

- [ ] **Step 3: Dash check**

Expected result: Dash is short, snappy, and distinct from Boost.

- [ ] **Step 4: Boost check**

Expected result: Sustained propulsion is clear and resets after ending.

- [ ] **Step 5: Fire check**

Expected result: Muzzle/core feedback is short and not screen-washing.

- [ ] **Step 6: Hit check**

Expected result: Damage feedback is readable and does not leave white flash residue.

- [ ] **Step 7: Weaving check**

Expected result: StarChart connection feeling is clear and not blocking gameplay.

- [ ] **Step 8: Overheat check**

Expected result: Heat danger is clear and recovers to Normal visuals.

### Task 29: Pool And Reset Verification

**Files:**

- Any VFX scripts/prefabs touched by Dash, Fire, Hit, Weaving, or Overheat.

- [ ] **Step 1: Trigger each VFX 10 times**

Expected result: No accumulated scale, color, alpha, emission, or particle state.

- [ ] **Step 2: Disable and re-enable preview/live ship**

Expected result: Visuals return to clean Normal state.

- [ ] **Step 3: Exit Play Mode**

Expected result: No authored Material or SO changed permanently.

### Batch 11 Completion Gate

- [ ] Full state matrix passes.
- [ ] Pool/reset checks pass.
- [ ] Bloom/background checks pass.
- [ ] No console errors related to missing ship visual references.

---

## 15. Execution Order Summary

Execute in this exact order unless a blocker is found:

```text
Task 1  - Prepare folders
Task 2  - Build Minishoot reference board
Task 3  - Body
Task 4  - Shape
Task 5  - Outline
Task 6  - Core / EnergyBars / WeaponMount
Task 7  - Lean Left
Task 8  - Lean Right
Task 9  - Lean validation
Task 10 - Dash frames
Task 11 - Dash materials and preview VFX
Task 12 - Import settings
Task 13 - Preview prefab
Task 14 - Preview animations
Task 15 - Boost assets
Task 16 - BoostTrail authority decision
Task 17 - Fire assets
Task 18 - Hit assets
Task 19 - Weaving assets
Task 20 - Weaving preview
Task 21 - Overheat assets
Task 22 - Materials
Task 23 - Material parameter ownership
Task 24 - Authority review
Task 25 - Asset registry update if live
Task 26 - Preview/live integration
Task 27 - Validation view
Task 28 - Final state matrix
Task 29 - Pool/reset verification
```

---

## 16. MVP And Future Enhancement Split

### MVP Must Finish

- [ ] Batch 0 reference lock.
- [ ] Batch 1 Normal playable set.
- [ ] Batch 2 Lean frames.
- [ ] Batch 3 Dash frames and preview VFX.
- [ ] Batch 4 Unity import and preview prefab.

MVP success means:

```text
We can see the Canary ship in Unity, with readable Body/Shape/Outline, playable Lean, and snappy Dash preview.
```

### Full Completion Must Finish

- [ ] Batch 5 Boost.
- [ ] Batch 6 Fire / Hit.
- [ ] Batch 7 Weaving.
- [ ] Batch 8 Overheat.
- [ ] Batch 9 Material / Shader pass.
- [ ] Batch 10 Formal integration.
- [ ] Batch 11 Final validation.

Full success means:

```text
The Canary ship is a complete gameplay-ready visual asset across all planned ship states.
```

### Future Enhancements After Completion

Do not start these until the full completion gate passes:

- [ ] Death / wrecked ship version.
- [ ] Skin variants.
- [ ] GG-style complex multi-state layer parity.
- [ ] Advanced shader graph effects.
- [ ] Final Bloom/postprocess cinematic polish.
- [ ] SpriteAtlas automation and import preset validator.

---

## 17. Risk Register

| Risk | Symptom | Prevention |
| --- | --- | --- |
| Over-producing art before Unity validation | Many nice PNGs but no playable ship | MVP ends at preview prefab, not at folder completion |
| GG reference becomes main path again | Batch 1 turns into state sheet production | Keep GG only in appendix; use Minishoot checklist |
| Scene override drift | Prefab looks right but scene differs | Do not patch scene instance as permanent solution |
| Debug tool becomes owner | Works only with debug enabled | Validate with debug off |
| Dash and Boost look same | Player cannot read state | Dash = short smear frames; Boost = sustained propulsion/trail |
| Overheat and Hit look same | Damage/heat feedback confused | Hit = white/short; Overheat = orange-red/rising/recovering |
| Runtime material pollution | Play Mode changes persist | Use MaterialPropertyBlock or instances only |
| Pool state leakage | VFX gets brighter/larger over time | Reset all transform/visual/particle/trail fields on return |

---

## 18. Per-Batch Logging Rule

After each batch, append to:

```text
Docs/5_ImplementationLog/ImplementationLog_2026-05.md
```

Required log fields:

```text
## [Feature / Batch Name] — YYYY-MM-DD HH:MM

- **新建/修改文件**
  - `path`

- **内容**：What changed.

- **目的**：Why it changed.

- **技术**：How it was done.
```

Do not wait until the end of the whole ship project to write logs.

---

## 19. Current Next Action

The immediate next action is:

```text
Task 6: Create Core / EnergyBars / WeaponMount
```

Use the accepted current body, shape mask, and outline as sources:

```text
Assets/_Art/Ship/Canary/Sprites/Body/spr_ship_canary_body_normal_albedo.png
Assets/_Art/Ship/Canary/Sprites/Shape/spr_ship_canary_shape_normal_mask.png
Assets/_Art/Ship/Canary/Sprites/Outline/spr_ship_canary_outline_normal_outline.png
```

After Task 6 is complete, immediately do:

```text
Task 7: Create Lean Left Body Sprite
```

Do not produce Boost / Fire / Hit / Weaving / Overheat before Batch 1-4 prove the Minishoot-style playable ship slice in Unity.
