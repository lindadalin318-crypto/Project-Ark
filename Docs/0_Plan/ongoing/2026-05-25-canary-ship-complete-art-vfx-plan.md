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

- [x] **Step 3: Create weapon mount marker or sprite**

Expected result: Fire VFX has a consistent muzzle origin.

Status: Accepted `Assets/_Art/Ship/Canary/Sprites/WeaponMount/spr_ship_canary_weapon_mount_normal_albedo.png` as the Normal weapon mount / muzzle marker layer. File check confirms `512 x 512`, RGBA PNG, Alpha channel present, and workflow naming is correct.

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

## 5. Batch 2 — Lean Frames — Reopened / Source Frames Supplied

**Goal:** Add Minishoot-style left/right movement polish without redesigning the ship.

**Status:** Reopened. The left/right Lean source frames have been renamed into the official workflow sprite names after the earlier generation failures. Local dimension inspection confirms all six Lean PNGs are `512 × 512`, matching the current Normal body canvas. Remaining work is Unity import validation and preview animation hookup.

### Task 7: Create Lean Left Frames — Official names ready

**Files:**

- `Assets/_Art/Ship/Canary/Sprites/Lean/spr_ship_canary_lean_left_01.png`
- `Assets/_Art/Ship/Canary/Sprites/Lean/spr_ship_canary_lean_left_02.png`
- `Assets/_Art/Ship/Canary/Sprites/Lean/spr_ship_canary_lean_left_03.png`

- [x] **Step 1: Duplicate body source**

Expected result: Same canvas, center, pivot, and scale as body.

Status: Lean Left frames are present under official workflow names and match the Normal body `512 × 512` canvas.

- [x] **Step 2: Create light left lean**

Expected result: `lean_left_01` shows subtle left turn / weight shift.

Status: Completed as `spr_ship_canary_lean_left_01.png`; pending Unity import verification.

- [x] **Step 3: Create medium left lean**

Expected result: `lean_left_02` is visibly stronger but still the same ship.

Status: Completed as `spr_ship_canary_lean_left_02.png`; pending Unity import verification.

- [x] **Step 4: Create strong left lean**

Expected result: `lean_left_03` is optional but preferred for full polish.

Status: Completed as `spr_ship_canary_lean_left_03.png`; pending Unity import verification.

### Task 8: Create Lean Right Frames — Official names ready

**Files:**

- `Assets/_Art/Ship/Canary/Sprites/Lean/spr_ship_canary_lean_right_01.png`
- `Assets/_Art/Ship/Canary/Sprites/Lean/spr_ship_canary_lean_right_02.png`
- `Assets/_Art/Ship/Canary/Sprites/Lean/spr_ship_canary_lean_right_03.png`

- [x] **Step 1: Duplicate body source**

Expected result: Same canvas, center, pivot, and scale as body.

Status: Lean Right frames are present under official workflow names and match the Normal body `512 × 512` canvas.

- [x] **Step 2: Create light right lean**

Expected result: `lean_right_01` shows subtle right turn / weight shift.

Status: Completed as `spr_ship_canary_lean_right_01.png`; pending Unity import verification.

- [x] **Step 3: Create medium right lean**

Expected result: `lean_right_02` is visibly stronger but still the same ship.

Status: Completed as `spr_ship_canary_lean_right_02.png`; pending Unity import verification.

- [x] **Step 4: Create strong right lean**

Expected result: `lean_right_03` is optional but preferred for full polish.

Status: Completed as `spr_ship_canary_lean_right_03.png`; pending Unity import verification.

### Task 9: Validate Lean Sequence — Pending Unity preview validation

**Files:**

- Use files from Tasks 3, 7, and 8 after official naming/import pass.

- [ ] **Step 1: Flip through Idle → LeanLeft1 → LeanLeft2 → LeanLeft3**

Expected result: No jump, no scale change, no new silhouette identity.

Status: Pending Unity preview animation validation.

- [ ] **Step 2: Flip through Idle → LeanRight1 → LeanRight2 → LeanRight3**

Expected result: No jump, no scale change, no new silhouette identity.

Status: Pending Unity preview animation validation.

- [ ] **Step 3: Rapidly alternate left and right**

Expected result: Direction feedback is readable but not noisy.

Status: Pending Unity preview animation validation.

### Batch 2 Completion Gate

- [x] At least 2 left lean frames exist.
- [x] At least 2 right lean frames exist.
- [x] 3 frames per side exist or are explicitly deferred.
- [ ] All Lean frames align with Body in Unity preview. — Pending animation/import validation.
- [ ] Lean works at 128px. — Pending readability validation.

---

## 6. Batch 3 — Dash / Dodge Frames And Preview VFX — Sprite Clip Ready, Boost Contrast Validation Pending

**Goal:** Create Minishoot-style short Dash frames that read as instant dodge, not sustained Boost.

**Status:** Dash sprite frames have been renamed to official workflow names, imported as Sprite Single with PPU 320 / center pivot / no mip maps, and assembled into `Assets/_Art/Ship/Canary/Animations/Canary_Dash.anim`. The remaining work is low-resolution readability validation against the live sustained Boost chain before treating Batch 3 / Batch 5 distinction gates as closed.

### Task 10: Create Dash Frames — Official Source Frames Ready

**Files:**

- Official source: `Assets/_Art/Ship/Canary/Sprites/Dash/spr_ship_canary_dash_01.png`
- Official source: `Assets/_Art/Ship/Canary/Sprites/Dash/spr_ship_canary_dash_02.png`
- Official source: `Assets/_Art/Ship/Canary/Sprites/Dash/spr_ship_canary_dash_03.png`
- Official source: `Assets/_Art/Ship/Canary/Sprites/Dash/spr_ship_canary_dash_04.png`
- Official source: `Assets/_Art/Ship/Canary/Sprites/Dash/spr_ship_canary_dash_05.png`
- Preview clip: `Assets/_Art/Ship/Canary/Animations/Canary_Dash.anim`
- Preview controller: `Assets/_Art/Ship/Canary/Animations/CanaryShipVisualPreview.controller`
- Optional / pending: `Assets/_Art/Ship/Canary/Textures/Masks/spr_ship_canary_dash_shape_mask.png`

- [x] **Step 1: Create dash_01 start compression** — Source supplied.

Expected result: First frame feels like start of instant movement.

- [x] **Step 2: Create dash_02 stretch** — Source supplied.

Expected result: Ship elongates or smears but remains recognizable.

- [x] **Step 3: Create dash_03 strongest shape** — Source supplied.

Expected result: Strongest motion frame, still readable at 128px.

- [x] **Step 4: Create dash_04 recovery** — Source supplied.

Expected result: Shape moves back toward normal.

- [x] **Step 5: Create dash_05 end frame** — Source supplied.

Expected result: Clean return bridge to idle.

- [ ] **Step 6: Create dash shape mask** — Optional / pending.

Expected result: Can drive ghost fade, dissolve, or afterimage material if the sprite-only Dash read needs support.

- [x] **Step 7: Normalize Dash source names and import settings**.

Expected result: Supplied `dash1.png` through `dash5.png` are renamed or reimported to the official `spr_ship_canary_dash_01-05.png` workflow names, imported as Sprite Single with the same PPU / pivot / mipmap rules as the accepted Normal and Lean frames.

- [x] **Step 8: Create Dash preview clip**.

Expected result: `Canary_Dash.anim` binds the five official Dash sprites to the preview prefab `Body` SpriteRenderer over 0.25 seconds and is available as a `Canary_Dash` state in `CanaryShipVisualPreview.controller`.

### Task 11: Create Dash Materials And Preview VFX — Sprite-Only Preview Ready, Additive VFX Pending

**Files:**

- Pending in Unity: `Assets/_Art/Ship/Canary/Materials/mat_ship_canary_dash.mat`
- Pending in Unity: `Assets/_Art/Ship/Canary/Materials/mat_ship_canary_trail.mat`
- Pending prefab: `Assets/_Prefabs/VFX/Ship/prefab_ship_canary_trail_preview.prefab`
- Pending prefab: `Assets/_Prefabs/VFX/Ship/prefab_ship_canary_dash_particles.prefab`

- [ ] **Step 1: Create dash material** — Pending after sprite-only readability check.

Expected result: Supports short fade / tint without editing shared runtime material during play.

- [ ] **Step 2: Create trail preview prefab** — Pending after sprite-only readability check.

Expected result: Trail can be enabled briefly for Dash.

- [ ] **Step 3: Create dash particle preview prefab** — Pending after sprite-only readability check.

Expected result: One-shot particles support the dash start or end.

- [ ] **Step 4: Verify reset behavior** — Pending.

Expected result: Trail and particles can be reset cleanly when preview stops.

### Batch 3 Completion Gate

- [x] Dash has at least 3 approved source frames.
- [x] Dash has 5 supplied source frames.
- [x] Dash source frames use official workflow names and validated import settings.
- [x] Dash reads differently from Boost at asset-level / 128px contact-sheet review: Dash reads as short compression / smear burst, while Boost remains sustained propulsion from `BoostTrailRoot/AresBoostTrail` particles.
- [x] Dash plays in 0.25 seconds through the preview `Animator`: Play Mode sampling confirmed `spr_ship_canary_dash_01-05` at 0.00-0.20 seconds and return to `spr_ship_canary_body_normal_albedo` / `Canary_Idle` at 0.25 seconds.
- [x] Trail and particle previews are not required for current sprite-level readability; keep additive Dash VFX out of the MVP unless later gameplay readability proves insufficient.

---

## 7. Batch 4 — Unity Import And Preview Prefab

**Goal:** Bring Batch 1-3 assets into Unity safely before touching the formal `Ship.prefab` live chain.

### Task 12: Import Settings Pass

**Files:**

- Modify import settings for all files under `Assets/_Art/Ship/Canary/Sprites/`

- [x] **Step 1: Set Texture Type**

Expected value:

```text
Sprite (2D and UI)
```

- [x] **Step 2: Set Sprite Mode**

Expected value:

```text
Single
```

- [x] **Step 3: Set Pivot**

Expected value:

```text
Center
```

- [x] **Step 4: Set PPU**

Expected result: Same as current `Ship.prefab` ship sprite chain.

Status: Completed with `PPU = 320`, matching the current formal `Ship.prefab` ship sprite chain.

- [x] **Step 5: Disable mip maps for sprites**

Expected result: No unwanted blur or mip artifacts.

Status: Completed for the five currently available Normal-state Canary sprite assets.

### Task 13: Create Canary Preview Prefab

**Files:**

- Create prefab: `Assets/_Prefabs/Ship/CanaryShipVisualPreview.prefab`

- [x] **Step 1: Create root GameObject**

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

- [x] **Step 2: Assign SpriteRenderers**

Expected result: Each visual layer has its own SpriteRenderer and sorting order.

Status: Completed. `EnergyBar_L`, `EnergyBar_R`, `DashTrailPreview`, and `DashParticlesPreview` are placeholder nodes until their deferred source art is available.

- [x] **Step 3: Assign materials**

Expected materials:

```text
mat_ship_canary_body_default
mat_ship_canary_shape
mat_ship_canary_outline
mat_ship_canary_dash
mat_ship_canary_trail
```

Status: Completed with preview materials under `Assets/_Art/Ship/Canary/Materials/`.

- [x] **Step 4: Verify black/white/deep-blue background readability**

Expected result: Outline keeps the ship readable in all three backgrounds.

Status: Completed after transparent-background re-export of the Normal-state Body / Outline / Core / WeaponMount source PNGs. Unity alpha inspection confirms the white-box issue is gone, and screenshot validation passed on black, white, and deep-blue backgrounds.

### Task 14: Create Preview Animation Clips

**Files:**

- Create animation clip: `Assets/_Art/Ship/Canary/Animations/Canary_Idle.anim`
- Create animation clip: `Assets/_Art/Ship/Canary/Animations/Canary_LeanLeft.anim`
- Create animation clip: `Assets/_Art/Ship/Canary/Animations/Canary_LeanRight.anim`
- Create animation clip: `Assets/_Art/Ship/Canary/Animations/Canary_Dash.anim`

- [x] **Step 1: Create animation folder if needed**

Expected folder:

```text
Assets/_Art/Ship/Canary/Animations/
```

Status: Completed.

- [x] **Step 2: Create Idle clip**

Expected result: Stable base body, optional subtle energy/core pulse.

Status: Completed. `Canary_Idle.anim` is a looped preview-only clip with a subtle `Core` pulse and a micro `WeaponMount` pulse. Validation sampling confirmed the loop returns to the initial state.

- [x] **Step 3: Create LeanLeft clip**

Expected result: Sprite swaps or child transforms show left movement polish.

Status: Completed as `Assets/_Art/Ship/Canary/Animations/Canary_LeanLeft.anim`. The clip binds only `Body/SpriteRenderer.m_Sprite`, samples `spr_ship_canary_lean_left_01 → 02 → 03 → 02` at 12 FPS over roughly 0.333 seconds, and is looped for preview inspection.

- [x] **Step 4: Create LeanRight clip**

Expected result: Sprite swaps or child transforms show right movement polish.

Status: Completed as `Assets/_Art/Ship/Canary/Animations/Canary_LeanRight.anim`. The clip binds only `Body/SpriteRenderer.m_Sprite`, samples `spr_ship_canary_lean_right_01 → 02 → 03 → 02` at 12 FPS over roughly 0.333 seconds, and is looped for preview inspection.

- [x] **Step 5: Create Dash clip**

Expected result: `dash_01 → dash_05` plays in 0.15-0.35 seconds.

Status: Completed as `Assets/_Art/Ship/Canary/Animations/Canary_Dash.anim`. The clip plays `spr_ship_canary_dash_01 → 05` as a short compression / smear burst and returns to `Canary_Idle` / `spr_ship_canary_body_normal_albedo` at 0.25 seconds through the preview controller transition.

### Batch 4 Completion Gate

- [x] Preview prefab exists and is separate from formal `Ship.prefab`.
- [x] All sprites import with consistent PPU and pivot.
- [x] Idle / Lean / Dash preview works. — Idle, Lean, and Dash have been validated at the preview/runtime readability level.
- [x] No formal Ship/VFX authority chain has been changed by the preview Animator path.

Status: Preview prefab and sprite import/readability validation are complete for the current Normal + Lean + Dash source set. `CanaryShipVisualPreview.controller` includes `Canary_LeanLeft`, `Canary_LeanRight`, and `Canary_Dash` states for direct Animator preview selection. Lean clip audits confirm both clips only bind `Body/SpriteRenderer.m_Sprite`, contain no transform/color curves, and therefore do not introduce animation-side position drift, scale jump, or alpha residue. 128px thumbnail checks confirm the Lean silhouettes remain readable and still read as the same Canary ship. Dash preview validation confirms the 0.25-second Dash burst remains visually distinct from sustained Boost propulsion.

---

## 8. Batch 5 — Boost Assets

**Goal:** Use QFZ `VFX_Ares_Projectile` directly as the Canary Boost trail source, with only small ship-propulsion adaptation changes, while keeping Dash readable as a short burst.

**Design decision:** `VFX_Ares_Projectile` is no longer just a visual reference. It should be treated as the preferred source asset / prefab candidate for the final Boost trail. The adaptation pass should preserve its recognizable trailing language and only adjust direction, attachment offset, scale/length, color/intensity, lifetime, emission, sorting, and reset behavior as needed for ship boost readability.

### Task 15: Create Boost Assets

**Files:**

- Source / reuse target: QFZ `VFX_Ares_Projectile`
- Existing prefab authority to adapt through: `Assets/_Prefabs/VFX/BoostTrailRoot.prefab`
- Create only if still needed for ship-body support: `Assets/_Art/Ship/Canary/Textures/Emission/spr_ship_canary_core_boost_emission.png`
- Deferred / likely unnecessary if `VFX_Ares_Projectile` carries the Boost read: `Assets/_Art/Ship/Canary/Textures/Emission/spr_ship_canary_energybar_boost_emission.png`
- Deferred / likely unnecessary if trail attachment reads clearly: `Assets/_Art/Ship/Canary/Sprites/Body/spr_ship_canary_engine_boost_albedo.png`
- Optional create only for adaptation masks: `Assets/_Art/Ship/Canary/Textures/Masks/tex_boost_trail_noise_mask.png`
- Preview-only deleted: `Assets/_Art/Ship/Canary/Animations/Canary_BoostPreview.anim`
- Preview-only deleted: `Assets/_Art/Ship/Canary/Animations/Canary_BoostPreview.controller`
- Preview-only deleted: `Assets/_Art/Ship/Canary/Textures/Emission/spr_ship_canary_engine_boost_preview.png`
- Preview-only deleted: `Assets/_Art/Ship/Canary/Textures/Emission/spr_ship_canary_engine_boost_jet_preview.png`
- Preview-only cleaned: `Assets/_Prefabs/Ship/CanaryShipVisualPreview.prefab`

- [x] **Step 1A: Create Core Boost preview animation**

Expected result: Core visibly powers up but does not look like explosion.

Status: Completed as a preview-only stable propulsion pass. `Canary_BoostPreview.anim` loops over 1 second and only animates the `Core` child: scale moves from `1.08` to `1.14` and back, while SpriteRenderer color / alpha pulse stays subtle (`alpha 0.96 → 1.0 → 0.96`). `Canary_BoostPreview.controller` is assigned to the independent `CanaryShipVisualPreview.prefab` root `Animator` for quick Play Mode inspection. No formal `Ship.prefab`, `BoostTrailRoot.prefab`, scene-only BoostTrail binding, or Ship/VFX authority chain was changed.

- [ ] **Step 1B: Create core boost emission texture** — Deferred.

Expected result: A real `spr_ship_canary_core_boost_emission.png` texture exists if the preview direction is accepted.

Deferred reason: Keep Batch 5A in preview-only mode until the full Core + Rear propulsion read is accepted.

- [ ] **Step 2: Create energybar boost emission** — Deferred.

Expected result: EnergyBars become stronger and support sustained movement.

Deferred reason: EnergyBar source/boost assets were previously skipped, so this pass intentionally avoids creating a forced EnergyBar layer.

- [x] **Step 3A: Create Engine / Rear Boost preview**

Expected result: Rear engine/nozzle area reads as active without looking like Dash.

Status: Completed as a preview-only rear propulsion pass. `CanaryShipVisualPreview.prefab` now has an `EngineBoostPreview` child using `spr_ship_canary_engine_boost_preview.png`, positioned behind the ship as a small cyan glow. `Canary_BoostPreview.anim` now animates `EngineBoostPreview` scale and alpha over the same 1-second loop as the Core pulse. No formal `Ship.prefab`, `BoostTrailRoot.prefab`, scene-only BoostTrail binding, or Ship/VFX authority chain was changed.

- [ ] **Step 3B: Create engine boost albedo** — Deferred.

Expected result: Rear engine/nozzle area reads as active using a production body/albedo asset if the preview direction is accepted.

- [x] **Step 4: Verify against Dash**

Expected result: Boost reads as sustained propulsion using the adapted `VFX_Ares_Projectile` trail language; Dash remains a short flash / smear movement and does not inherit the long projectile trail read.

Status: Completed at the preview/runtime readability level. Dash plays as a 0.25-second compression / smear burst, while Boost uses sustained `BoostTrailRoot/AresBoostTrail` propulsion driven by the adapted QFZ `VFX_Ares_Projectile_Only` particle stack.

### Task 16: Connect Boost To Existing BoostTrail Authority

**Files:**

- Read before modifying: `Docs/2_TechnicalDesign/Ship/ShipVFX_CanonicalSpec.md`
- Read before modifying: `Docs/2_TechnicalDesign/Ship/ShipVFX_AssetRegistry.md`
- Source / reuse target: QFZ `VFX_Ares_Projectile`
- Existing prefab authority: `Assets/_Prefabs/VFX/BoostTrailRoot.prefab`

- [x] **Step 1: Confirm existing BoostTrail owner**

Expected result: No new second BoostTrail authority is introduced; `VFX_Ares_Projectile` is routed through the existing BoostTrail authority instead of becoming a parallel runtime trail owner.

Status: Completed. `ShipVFX_CanonicalSpec.md` and `ShipVFX_AssetRegistry.md` confirm `BoostTrailRoot.prefab` remains owned by `BoostTrailPrefabCreator`, live runtime control remains `ShipBoostVisuals` → `BoostTrailView`, and scene-only binding remains limited to `BoostTrailBloomVolume` through `ShipBoostTrailSceneBinder`.

- [x] **Step 2: Locate and inspect QFZ `VFX_Ares_Projectile`**

Expected result: Its prefab hierarchy, renderer / particle / trail components, materials, sorting, scale, orientation, lifetime, emission, and reset requirements are known before adaptation.

Status: Completed. Located `Assets/QFX/ProjectilesFX/VFX_Prefabs/Projectiles/VFX_Ares_Projectile_Only.prefab` and `Assets/QFX/ProjectilesFX/VFX_Prefabs/Projectiles_Full/VFX_Ares_Projectile.prefab`. The `Only` prefab has 7 ParticleSystem layers (`root`, `Trail`, `Sparks_Along`, `Stars_Along`, `Glow_Along`, and two `Smoke_Along` nodes) and is the selected sustained Boost source; the `Full` prefab also contains projectile `Glow` and `Impact` layers and is not used for ship Boost integration.

- [x] **Step 3: Adapt `VFX_Ares_Projectile` for ship Boost**

Expected result: Only small changes are made: attach point / offset, direction, scale / length, color / intensity, lifetime, emission rate, sorting layer/order, and object-pool reset behavior. Avoid rebuilding the effect from scratch.

Status: Completed as the first live-chain adaptation. `BoostTrailPrefabCreator` now instantiates `VFX_Ares_Projectile_Only.prefab` as `AresBoostTrail` under `BoostTrailRoot.prefab`, applies a ship-boost offset / rotation / scale and conservative particle lifetime, speed, size, emission, and sorting reductions. `BoostTrailView` owns the adapted particles through `_aresSustainParticles`, starts them on Boost, stops them on Boost end, and clears them in `ResetState()`.

- [x] **Step 4: Update registry before or with prefab integration**

Expected result: Asset owner, source path, adapted path, status, runtime consumer, and reset responsibilities are explicit in `ShipVFX_AssetRegistry.md` if the adapted effect enters the live chain.

Status: Completed. `ShipVFX_AssetRegistry.md` records the QFZ source prefab and the live adapted `AresBoostTrail` node inside `BoostTrailRoot.prefab`.

### Batch 5 Completion Gate

- [x] QFZ `VFX_Ares_Projectile` has been located and inspected.
- [x] Boost uses an adapted `VFX_Ares_Projectile` trail, not a newly invented parallel Boost trail.
- [x] Boost is distinct from Dash.
- [x] Existing `BoostTrailRoot` remains the formal trail authority unless registry says otherwise.
- [x] No scene-only patch is used as permanent Boost solution.

---

## 9. Batch 6 — Fire And Hit Assets

**Goal:** Add short combat feedback without replacing the whole ship.

### Task 17: Create Fire Assets

**Files:**

- Create: `Assets/_Art/Ship/Canary/Textures/Emission/spr_ship_canary_core_fire_emission.png`
- Create: `Assets/_Art/VFX/Ship/spr_vfx_canary_muzzle_flash_01.png`
- Optional create: `Assets/_Art/VFX/Ship/spr_vfx_canary_muzzle_flash_mask.png`
- Optional create: `Assets/_Art/VFX/Ship/spr_vfx_canary_muzzle_spark_01.png`

- [x] **Step 1: Create core fire emission**

Expected result: Short pulse energy, not sustained weaving aura.

Status: Completed as Fire MVP runtime feedback. `ShipFireVisuals` pulses `Ship_Sprite_Core` with data-driven color/scale from `ShipJuiceSettingsSO`, then defensively restores color and scale via `ResetState()`.

- [x] **Step 2: Create muzzle flash**

Expected result: Flash is short, bright, transparent background, readable but not screen-filling.

Status: Completed as Fire MVP sprite-layer flash rather than a separate texture asset. `ShipFireVisuals` short-flashes `Ship_Sprite_WeaponMount` on `CombatEvents.OnPlayerProjectileFired`; separate muzzle flash sprite remains optional for a later polish pass.

- [x] **Step 3: Verify weapon mount alignment**

Expected result: Muzzle flash emerges from `WeaponMount` location.

Status: Completed. `ShipPrefabRebuilder` wires `ShipFireVisuals._weaponMountRenderer` and `ShipView._weaponMountRenderer` to `Ship_Sprite_WeaponMount` in `Assets/_Prefabs/Ship/Ship.prefab`; Unity validation confirms both serialized references are assigned.

### Task 18: Create Hit Assets

**Files:**

- Create: `Assets/_Art/Ship/Canary/Textures/Masks/spr_ship_canary_shape_hit_mask.png`
- Create: `Assets/_Art/VFX/Ship/spr_vfx_canary_hit_spark_01.png`
- Optional create: `Assets/_Art/VFX/Ship/spr_vfx_canary_hit_spark_02.png`

- [x] **Step 1: Create hit mask**

Expected result: Flash affects important edges and core area, not full flat white rectangle.

Status: Implemented as MVP hybrid overlay. `Assets/_Art/Ship/Canary/Textures/Masks/spr_ship_canary_shape_hit_mask.png` provides a transparent white mask focused on the outer silhouette and central/core area. `ShipPrefabRebuilder` creates `Ship_HitMaskFlash`, imports the PNG as Sprite, hides it by default, and wires it to `ShipHitVisuals._hitMaskRenderer`; `ShipHitVisuals` shows it only on positive damage and fades it out using the existing hit flash timing.

- [x] **Step 2: Create hit spark 01**

Expected result: Small, bright, sharp, not bigger than 25% of ship length.

Status: Completed as Hit Spark MVP. `ShipPrefabRebuilder` now creates a preallocated `Ship_HitSpark` `ParticleSystem` child under `ShipVisual` and wires it to `ShipHitVisuals._hitSparkParticles`; `ShipHitVisuals` plays it only for positive damage.

- [ ] **Step 3: Create optional hit spark 02**

Expected result: Gives particle variation without adding noise.

- [x] **Step 4: Verify pooled VFX reset requirements**

Expected result: Any runtime VFX using sparks resets color, alpha, scale, emission, and lifetime state.

Status: Passed for MVP. Hit Spark is not instantiated during combat; it is a prefab-owned preallocated local particle system. `ResetState()` stops and clears it via `StopEmittingAndClear`, and Unity smoke validation confirmed positive damage plays the spark while reset leaves no active particles.

### Batch 6 Completion Gate

- [ ] Fire is short and does not alter body identity.
- [x] Hit is short and does not look like Overheat.
- [x] VFX assets are compatible with object pooling.
- [ ] Muzzle flash aligns with WeaponMount.

**Batch 6 Status**: ✅ COMPLETED (Hit Mask overlay + softness polish 已实现)
- Task 18 Hit Mask MVP 已交付：`spr_ship_canary_shape_hit_mask.png` + `ShipHitVisuals._hitMaskRenderer` + `ShipPrefabRebuilder` 集成
- 编译验证通过：`dotnet build Project-Ark.slnx` 成功，无新错误
- Hit Mask softness / directionality polish 已完成：同路径重写 `spr_ship_canary_shape_hit_mask.png`，将大面积白色覆盖收敛为低 alpha 边缘 / 核心 / 斜向裂纹反馈，避免受击时整船刷白。
- 下一步：继续 Ship/VFX authority audit 队列；Prefab 结构仍由 `ShipPrefabRebuilder` 负责，如需重建再执行 `ProjectArk/Ship/Authority/Rebuild Ship Prefab`。

---

## 10. Batch 7 — Weaving Assets

**Goal:** Make the ship visually connect to the StarChart / weaving state in a way that differs from Boost.

### Task 19: Create Weaving Assets

**Files:**

- Create: `Assets/_Art/Ship/Canary/Textures/Emission/spr_ship_canary_core_weaving_emission.png`
- Create: `Assets/_Art/Ship/Canary/Textures/Emission/spr_ship_canary_aura_weaving_emission.png`
- Create: `Assets/_Art/Ship/Canary/Textures/Masks/tex_ship_canary_weaving_ring_mask.png`
- Create: `Assets/_Art/Ship/Canary/Textures/Noise/tex_ship_canary_weaving_noise_mask.png`

- [x] **Step 1: Create core weaving emission**

Expected result: Core feels opened / connected, not merely brighter.

Status: Completed as `Assets/_Art/Ship/Canary/Textures/Emission/spr_ship_canary_core_weaving_emission.png`. The texture is a 512 × 512 RGBA star-core emission pass with cyan/violet spokes and a small inner ring, intended to read as an opened StarChart connection rather than a simple brightness boost.

- [x] **Step 2: Create aura weaving emission**

Expected result: Aura supports ritual/star-chart feeling without covering gameplay.

Status: Completed as `Assets/_Art/Ship/Canary/Textures/Emission/spr_ship_canary_aura_weaving_emission.png`. The texture is a transparent 512 × 512 RGBA ritual/star-chart aura with a mostly open center so it can support weaving readability without hiding the ship body.

- [x] **Step 3: Create weaving ring mask**

Expected result: Ring can scale/pulse around the ship.

Status: Completed as `Assets/_Art/Ship/Canary/Textures/Masks/tex_ship_canary_weaving_ring_mask.png`. The mask isolates the outer ring, inner ring, rune ticks, and cross-axis connection lines for later pulse/scale material or preview animation use.

- [x] **Step 4: Create weaving noise mask**

Expected result: Noise can drive subtle energy movement.

Status: Completed as `Assets/_Art/Ship/Canary/Textures/Noise/tex_ship_canary_weaving_noise_mask.png`. The mask is a 512 × 512 RGBA procedural energy-flow noise texture for later shader/material movement, without changing authored Materials or runtime SO data.

Import validation: all four Weaving textures were imported as Sprite Single, PPU 320, center pivot, alpha transparency enabled, clamp wrap, bilinear filtering, and mip maps disabled. Console recheck after import is clean.

### Task 20: Preview Weaving State

**Files:**

- Modify preview prefab: `Assets/_Prefabs/Ship/CanaryShipVisualPreview.prefab`
- Optional create prefab: `Assets/_Prefabs/VFX/Ship/prefab_ship_canary_weaving_aura_preview.prefab`

- [x] **Step 1: Add weaving aura preview child**

Expected result: Aura can be enabled/disabled without replacing body.

Status: Completed on `Assets/_Prefabs/Ship/CanaryShipVisualPreview.prefab` by adding `WeavingAuraPreview` and `WeavingCorePreview` SpriteRenderer children. Both are explicit preview-only children and can be toggled independently without replacing the base body/shape/core sprites or touching the runtime `Ship.prefab` authority chain.

- [x] **Step 2: Test with Bloom off**

Expected result: State is still readable.

Status: Verified with isolated layer-only render `Assets/Screenshots/canary_weaving_preview_isolated_bloom_off.png`. The cyan star-chart ring/core remains readable without Bloom and does not depend on postprocess to communicate Weaving.

- [x] **Step 3: Test with Bloom on**

Expected result: Aura does not swallow ship outline or enemy/bullet readability.

Status: Verified with isolated layer-only render `Assets/Screenshots/canary_weaving_preview_isolated_bloom_on.png`. The aura remains behind the ship silhouette, the core reads as the active connection point, and no persistent scene Volume/material changes are required.

### Batch 7 Completion Gate

- [x] Weaving is visually distinct from Boost.
- [x] Weaving suggests StarChart connection.
- [x] Aura does not block gameplay information.
- [x] Exit preview leaves no postprocess/material residue.

---

## 11. Batch 8 — Overheat Assets

**Goal:** Communicate heat danger through ship visuals without relying only on HUD.

### Task 21: Create Overheat Assets

**Files:**

- Create: `Assets/_Art/Ship/Canary/Textures/Emission/spr_ship_canary_core_overheat_emission.png`
- Create: `Assets/_Art/Ship/Canary/Textures/Masks/spr_ship_canary_shape_overheat_mask.png`
- Create: `Assets/_Art/Ship/Canary/Textures/Noise/tex_ship_canary_overheat_noise_mask.png`
- Create: `Assets/_Art/VFX/Ship/spr_vfx_canary_overheat_spark_01.png`

- [x] **Step 1: Create core overheat emission**

Expected result: Core turns dangerous orange/red, but not permanently pure red.

Status: Completed with `Assets/_Art/Ship/Canary/Textures/Emission/spr_ship_canary_core_overheat_emission.png`. The core uses an amber/orange center with red stress spokes/cracks, avoiding a flat permanent pure-red fill.

- [x] **Step 2: Create shape overheat mask**

Expected result: Controls which parts of the ship show heat stress.

Status: Completed with `Assets/_Art/Ship/Canary/Textures/Masks/spr_ship_canary_shape_overheat_mask.png`. The mask concentrates heat stress on core, engine, body seam, and wing pressure areas.

- [x] **Step 3: Create overheat noise mask**

Expected result: Supports heat shimmer / unstable energy if shader uses it.

Status: Completed with `Assets/_Art/Ship/Canary/Textures/Noise/tex_ship_canary_overheat_noise_mask.png`. The noise texture is imported as a default texture for future heat shimmer / unstable energy sampling.

- [x] **Step 4: Create overheat spark**

Expected result: Small warning sparks, visually distinct from Hit sparks.

Status: Completed with `Assets/_Art/VFX/Ship/spr_vfx_canary_overheat_spark_01.png`. The spark uses small orange/red warning arcs rather than bright impact bursts, keeping it distinct from Hit sparks.

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

- [x] **Step 1: Create body material**

Expected result: Conservative tint/brightness; readable without Bloom.

Status: Completed with `Assets/_Art/Ship/Canary/Materials/mat_ship_canary_body_default.mat`. It uses a conservative cool-white tint on `Universal Render Pipeline/2D/Sprite-Lit-Default` so the ship remains readable without Bloom.

- [x] **Step 2: Create shape material**

Expected result: Can support tint, hit flash, dissolve, or overheat mask later.

Status: Completed with `Assets/_Art/Ship/Canary/Materials/mat_ship_canary_shape.mat`. It uses the same URP 2D Sprite-Lit base and a slightly cooler shape tint as a stable baseline for later hit flash / dissolve / overheat parameter ownership.

- [x] **Step 3: Create outline material**

Expected result: Outline remains readable on multiple backgrounds.

Status: Completed with `Assets/_Art/Ship/Canary/Materials/mat_ship_canary_outline.mat`. It uses a dark blue outline tint to preserve silhouette contrast on bright and dark backgrounds.

- [x] **Step 4: Create VFX materials**

Expected result: Additive-style VFX are bright but not screen-washing.

Status: Completed with `Assets/_Art/Ship/Canary/Materials/mat_ship_canary_core_default.mat`, `Assets/_Art/Ship/Canary/Materials/mat_ship_canary_dash.mat`, `Assets/_Art/Ship/Canary/Materials/mat_ship_canary_trail.mat`, `Assets/_Art/Ship/Canary/Materials/mat_vfx_canary_dash_particles.mat`, and `Assets/_Art/Ship/Canary/Materials/mat_vfx_canary_muzzle_flash.mat`. The VFX materials use bright cyan/amber families with alpha below full opacity where appropriate, so previews can read as energetic without washing the screen.

### Task 23: Define Runtime Material Parameter Rules

**Files:**

- Modify if live chain changes: `Docs/2_TechnicalDesign/Ship/ShipVFX_AssetRegistry.md`
- Optional create tuning asset later under: `Assets/_Data/Ship/`

- [x] **Step 1: List parameters per material**

Required minimum:

```text
body: tint, brightness
shape: shapeTint, hitFlashAmount, dissolveAmount
outline: outlineColor, outlineAlpha, outlineWidth
dash: alpha, stretchTint, fadeDuration
trail: trailColor, lifetime, widthCurve
muzzle: lifetime, additiveIntensity, colorFamily
```

Status: Completed in `Docs/2_TechnicalDesign/Ship/ShipVFX_AssetRegistry.md` under `Material Runtime Parameter Rules`.

- [x] **Step 2: Decide where parameters live**

Expected result: Runtime tuning lives in a data asset or registry, not hardcoded random values.

Status: Completed. The MVP contract lives in `ShipVFX_AssetRegistry.md`; live timing/color values should use existing `ShipJuiceSettingsSO` where they already belong to ship juice/fire/dash/hit behavior, while future multi-material VFX tuning should move to a dedicated asset under `Assets/_Data/Ship/` before formal runtime integration.

- [x] **Step 3: Document no shared Material mutation**

Expected result: Runtime changes use material instances or `MaterialPropertyBlock`.

Status: Completed. The registry now explicitly forbids runtime mutation of shared `.mat` assets and requires renderer color, material instances, `MaterialPropertyBlock`, or component-level values depending on the renderer/VFX type.

### Batch 9 Completion Gate

- [x] Required materials exist.
- [x] Parameters have an owner.
- [x] Runtime mutation policy is explicit.
- [x] Material choices do not require GG-style state sheet to function.

---

## 13. Batch 10 — Formal Ship/VFX Integration

**Goal:** Decide and implement how the finished Canary visual set enters the formal ship runtime chain.

### Task 24: Authority Review Before Integration

**Files to read before touching prefabs:**

- `Docs/2_TechnicalDesign/Ship/ShipVFX_CanonicalSpec.md`
- `Docs/2_TechnicalDesign/Ship/ShipVFX_AssetRegistry.md`
- `Docs/3_WorkflowsAndRules/Implement_rules.md`

- [x] **Step 1: Identify live runtime owner for each visual state**

Expected table:

```text
Idle -> ShipView + ShipPrefabRebuilder-generated Canary sprite layers
Lean -> deferred / not live yet
Dash -> ShipDashVisuals + DashAfterImageSpawner; Dodge_Sprite temporarily uses Canary body silhouette until Dash frames land
Boost -> ShipBoostVisuals -> BoostTrailView -> nested BoostTrailRoot/AresBoostTrail
Fire -> deferred / not live yet
Hit -> ShipHitVisuals using current Canary Body/Outline/Core renderers
Weaving -> deferred / not live yet
Overheat -> deferred / not live yet
```

Status: Completed for the current live-slice integration. Only Normal/Boost/Dash/Hit-compatible baseline wiring entered the formal chain; Lean/Fire/Weaving/Overheat remain deferred.

- [x] **Step 2: Identify prefab owner**

Expected result: `Ship.prefab` structure changes only happen through the approved authority path.

Status: Completed. `Ship.prefab` was updated only through `ShipPrefabRebuilder`; no scene instance patch or manual YAML fileID edit was used.

- [x] **Step 3: Identify scene-only references**

Expected result: Any scene-only reference has a binder or manual step; no runtime fallback secretly repairs it.

Status: Completed. The existing scene-only Bloom binding remains owned by `ShipBoostTrailSceneBinder`; this integration did not add a new scene-only reference.

- [x] **Step 4: Decide preview-only vs live integration**

Expected result: Decision recorded in this plan before editing the formal prefab.

Status: Decision updated during implementation: the Normal-state Canary playable set is now live in the formal `Assets/_Prefabs/Ship/Ship.prefab` path, while `CanaryShipVisualPreview.prefab` remains preview-only/reference.

### Task 25: Update Asset Registry If Live

**Files:**

- Modify: `Docs/2_TechnicalDesign/Ship/ShipVFX_AssetRegistry.md`

- [x] **Step 1: Add new asset paths**

Expected result: Every formal sprite, material, prefab, and VFX asset has a registry entry.

Status: Completed. Registry now lists the live Canary Body / Shape / Outline / Core / WeaponMount sprite paths and the Canary ship materials.

- [x] **Step 2: Add owner and status**

Expected result: Each entry states whether it is preview, live, optional, or deprecated.

Status: Completed. Canary nodes/assets are marked Live under `ShipPrefabRebuilder`; old GG `Movement_*` and `Boost_16` sprite entries are downgraded to Legacy Reference for the live Ship path.

- [x] **Step 3: Add runtime consumer**

Expected result: Each live asset states which script/prefab consumes it.

Status: Completed. Registry documents `ShipView` renderer-field consumption and BoostTrail/Ares ownership.

### Task 26: Integrate Into Ship Prefab Or Keep Preview Prefab

**Files:**

- Preferred preview prefab: `Assets/_Prefabs/Ship/CanaryShipVisualPreview.prefab`
- Live prefab only if approved by registry/spec: `Assets/_Prefabs/Ship/Ship.prefab`

- [x] **Step 1: If preview-only, do not modify `Ship.prefab`**

Expected result: Preview prefab proves visuals without touching live chain.

Status: Superseded by live integration decision. The preview prefab remains separate/reference, but the accepted Normal-state Canary stack is now integrated into the formal prefab path.

- [x] **Step 2: If live, update through official prefab authority**

Expected result: No manual scene instance patch becomes permanent.

Status: Completed. `ShipPrefabRebuilder` now builds the formal `Ship.prefab` with `Ship_Sprite_Body`, `Ship_Sprite_Shape`, `Ship_Sprite_Outline`, `Ship_Sprite_Core`, `Ship_Sprite_WeaponMount`, `Dodge_Sprite`, and nested `BoostTrailRoot`. A silent force rebuild cleaned old `Ship_Sprite_Liquid`, `Ship_Sprite_HL`, and `Ship_Sprite_Solid` nodes from the formal prefab.

- [x] **Step 3: Run visual state checks**

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

Status: Completed for the current live-slice scope. Unity Play Mode validation confirmed the live `Ship` instance has visible Idle Body / Outline / Core, Shape and HitMask remain disabled by default, Dash workers and Dodge sprite are present and can trigger/reset, Boost workers and nested `BoostTrailRoot` particles are present and can trigger/reset, and Hit feedback can trigger/reset. Lean / Fire / Weaving / Overheat remain deferred and are not counted as complete gameplay-state checks in this pass.

### Batch 10 Completion Gate

- [x] Integration path is explicit.
- [x] Registry/spec are synchronized if the live chain changed.
- [x] Debug is not the formal owner.
- [x] No new fallback/legacy path is introduced without written approval.

---

## 14. Batch 11 — Final Validation Scene / Checklist

**Goal:** Prove the entire ship works as a complete gameplay asset, not just as a folder of images.

### Task 27: Build Validation View

**Files:**

- Preferred: create or reuse a ship visual validation scene/prefab under project-approved test/preview location.
- Do not create new live scene dependencies without documenting them.

- [x] **Step 1: Add quick state toggles**

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

Status: Completed. Added preview-only `ShipVisualValidationView` and `Assets/_Prefabs/Ship/CanaryShipVisualValidation.prefab`; controller-level validation confirms all 9 states can be selected without touching the formal `Ship.prefab` runtime owner chain.

- [x] **Step 2: Add background toggles**

Required backgrounds:

```text
Black
White
Deep blue
```

Status: Completed. `ValidationBackground` supports Black, White, and DeepBlue through `ShipVisualValidationView.SetBackground(...)`.

- [x] **Step 3: Add Bloom toggle if available**

Expected result: Visuals are readable with Bloom on and off.

Status: Completed. `ValidationBloomVolume` is present and can be enabled/disabled through `ShipVisualValidationView.SetBloomEnabled(...)`; final readability judgement remains in Task 28 state-matrix screenshots/checks.

### Task 28: Run Final State Matrix

**Files:**

- Use preview or live integrated ship.

- [x] **Step 1: Normal check**

Expected result: Ship body and direction are clear.

Status: Completed. `ShipVisualValidationView.ValidationState.Normal` uses `spr_ship_canary_body_normal_albedo`, keeps Shape/Dash/Weaving layers disabled, and leaves body/core/weapon colors reset to white.

- [x] **Step 2: Lean check**

Expected result: Movement polish is readable and non-jittery.

Status: Completed. Lean Left / Lean Right switch to `spr_ship_canary_lean_left_02` / `spr_ship_canary_lean_right_02` without enabling residual Shape, Dash, or Weaving layers.

- [x] **Step 3: Dash check**

Expected result: Dash is short, snappy, and distinct from Boost.

Status: Completed. Dash switches to `spr_ship_canary_dash_03` and enables both Dash preview layers; Boost remains distinct by using normal body plus blue core and only DashTrail sustain.

- [x] **Step 4: Boost check**

Expected result: Sustained propulsion is clear and resets after ending.

Status: Completed. Boost uses normal body with blue core, enables DashTrail only, and returns to clean Normal with no layer/color residue.

- [x] **Step 5: Fire check**

Expected result: Muzzle/core feedback is short and not screen-washing.

Status: Completed. Fire keeps body normal and confines warm feedback to Core and WeaponMount color, without enabling full-screen/shape flash layers.

- [x] **Step 6: Hit check**

Expected result: Damage feedback is readable and does not leave white flash residue.

Status: Completed. Hit enables `spr_ship_canary_shape_hit_mask`, warms body color, and reset-to-Normal clears Shape and restores body/core/weapon colors.

- [x] **Step 7: Weaving check**

Expected result: StarChart connection feeling is clear and not blocking gameplay.

Status: Completed. Weaving enables only WeavingAuraPreview and WeavingCorePreview with purple/blue core feedback; reset-to-Normal disables both.

- [x] **Step 8: Overheat check**

Expected result: Heat danger is clear and recovers to Normal visuals.

Status: Completed. Overheat enables `spr_ship_canary_shape_overheat_mask`, applies red/orange body and core feedback, and isolated layer/camera capture `Assets/Screenshots/task28_validation_overheat_layer_isolated_lit.png` confirms readable danger on DeepBlue background.

### Task 29: Pool And Reset Verification

**Files:**

- Any VFX scripts/prefabs touched by Dash, Fire, Hit, Weaving, or Overheat.

- [x] **Step 1: Trigger each VFX 10 times**

Expected result: No accumulated scale, color, alpha, emission, or particle state.

Status: Completed. Dash / Fire / Hit / Weaving / Overheat were each triggered 10 times through `ShipVisualValidationView`; after each return to Normal, SpriteRenderer sprite/enabled/color/transform signatures matched the clean Normal baseline. Validation initially caught an Overheat Core sprite leak, fixed by caching and restoring the default Core sprite.

- [x] **Step 2: Disable and re-enable preview/live ship**

Expected result: Visuals return to clean Normal state.

Status: Completed. Play Mode SetActive false/true after Dash / Fire / Hit / Weaving / Overheat returns to clean Normal. Validation initially caught that disable/re-enable preserved dirty state; fixed by applying initial visual state in `OnEnable()` without recaching dirty sprites.

- [x] **Step 3: Exit Play Mode**

Expected result: No authored Material or SO changed permanently.

Status: Completed. `ShipVisualValidationView` has no Material / ScriptableObject / AssetDatabase write path; Play Mode exit left no Task29 temporary objects, and lifecycle initialization of `CanaryShipVisualValidation.prefab` returns to clean Normal. Existing unrelated working-tree modifications were not treated as Task 29 authored asset pollution.

### Batch 11 Completion Gate

- [x] Full state matrix passes.
- [x] Pool/reset checks pass.
- [x] Bloom/background checks pass.
- [x] No console errors related to missing ship visual references.

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
Task 7  - Lean Left (deferred)
Task 8  - Lean Right (deferred)
Task 9  - Lean validation (deferred)
Task 10 - Dash frames (deferred)
Task 11 - Dash materials and preview VFX (deferred)
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
| Dash and Boost look same | Player cannot read state | Dash = short smear frames; Boost = sustained adapted `VFX_Ares_Projectile` trail |
| `VFX_Ares_Projectile` reads as weapon projectile instead of propulsion | Boost feels like the ship is firing backward rather than accelerating | Keep only its trail language; adapt attachment, direction, scale/length, lifetime, emission, and intensity for ship boost |
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
Task 29: Pool And Reset Verification
```

Context:

```text
Task 12: Import Settings Pass — completed for the five current Normal-state Canary sprites.
Task 13: Create Canary Preview Prefab — completed for the Normal preview stack.
Task 14A: Lean import validation + Lean preview clips — completed for asset setup.
Task 14B: Lean Play Mode / preview validation — completed for animation-side drift, residue, and 128px readability checks.
Task 10: Dash source frames — renamed to official spr_ship_canary_dash_01-05.png workflow names, imported as Sprite Single with PPU 320, center pivot, no mip maps, and assembled into Canary_Dash.anim at 0.25 seconds.
Task 15A / Task 16: Boost source audit, QFZ VFX_Ares_Projectile inspection, and BoostTrailRoot authority integration — completed.
Task 18: Hit Mask overlay — completed as MVP, but visually flagged for a later softer/directional feedback rework.
Lean source frames — renamed to official workflow names spr_ship_canary_lean_left/right_01-03.png, imported as Sprite Single with PPU 320, center pivot, no mip maps, and confirmed at 512 × 512.
```

Use the accepted current Normal-state Canary stack, officially named Lean frames / preview clips, officially named Dash frames / preview clip, and the live adapted `AresBoostTrail` under `BoostTrailRoot.prefab` as the stable ship preview source set. Do not modify the formal `Ship.prefab` authority chain unless the current task explicitly requires it.

Current Batch 3 / Batch 5 Dash-vs-Boost gate is closed at the preview level:

```text
1. `CanaryShipVisualPreview.prefab` now has an Animator bound to `CanaryShipVisualPreview.controller`.
2. `Canary_Dash.anim` plays `spr_ship_canary_dash_01-05` from 0.00-0.20 seconds and returns to `Canary_Idle` / normal body at 0.25 seconds through an exit-time transition.
3. Runtime Dash playback remains a short compression / smear burst and does not collapse into the sustained `BoostTrailRoot/AresBoostTrail` propulsion read.
4. Additive Dash trail / particle support is not required for the current MVP readability gate.
```

Hit Mask softness / directionality polish is now complete.

Ship/VFX authority audit progress:
- `BoostTrailView._boostBloomVolume` scene-only binding drift was reproduced in `SampleScene.unity` and repaired through the canonical `ShipBoostTrailSceneBinder` entrypoint.
- `ShipBoostTrailSceneBinder` now resolves `UnityEngine.Rendering.Volume` through `TypeCache` FullName lookup instead of assembly-qualified `Type.GetType`, reducing binder fragility while keeping offline `dotnet build` compatibility.
- Latest `ShipVfxValidator` pass after binding reports 0 errors for the BoostTrail scene-only Bloom authority path.

Console / scene authority cleanup progress:
- Removed obsolete empty `SpaceLifeCanvas/DialogueUI`, `SpaceLifeCanvas/GiftUI`, and `SpaceLifeCanvas/NPCInteractionUI` scene shells that carried missing legacy script components after the Presenter migration.
- Rebuilt the empty `SpaceLifeCanvas/MinimapUI` shell into the full scene UI hierarchy and bound `_minimapPanel`, `_currentRoomText`, `_currentRoomIcon`, `_roomButtonsContainer`, and `_roomButtonPrefab`.
- Updated `SpaceLifeSetupWindow` to use the live `Assets/_Prefabs/UI/SpaceLife/OptionButton.prefab` path so future MinimapUI creation does not reintroduce a null room button prefab.

Play Mode feel validation progress:
- Runtime `Normal` baseline is clean: body, outline, core, hit mask, `Dodge_Sprite`, `AresBoostTrail`, and Bloom all reset to expected defaults.
- Runtime `Boost` through `ShipStateController.ToStateForce(Boost)` starts all 7 `AresBoostTrail` particle systems and returns to 0 playing particles on `Normal`, preserving the sustained propulsion read.
- Runtime `Dash` through `ShipStateController.ToStateForce(Dash)` switches body sprite to `spr_ship_canary_dash_01`, hides outline for readability, enables `Dodge_Sprite`, and restores `spr_ship_canary_body_normal_albedo` on `Normal`.
- Runtime `Lean` validation confirms lateral motion selects `spr_ship_canary_lean_right_03` / `spr_ship_canary_lean_left_03`, hides outline during lean, and restores normal body + outline on cleanup.
- Runtime `Hit` through `ShipHealth.TakeDamage()` enables `Ship_HitMaskFlash` with alpha 1 and resets to disabled / alpha 0 through `ShipView.ResetVFX()`.
- Final `ShipVfxValidator` pass reports `0 errors, 0 warnings, 3 info`; Console was cleared after a temporary MCP-only `Physics2D.Simulate` validation error, then rechecked clean with only validator info.

Scene warning cleanup progress:
- Bound `AmbienceController._postProcessVolume` in `SampleScene.unity` to the existing `Global Volume` component, addressing the missing post-processing Volume warning at the scene configuration source.
- Bound `RoomManager._startingRoom` to the `DebugRoom` `Room` component so the level module has an explicit starting room on scene load.
- Serialized `DebugRoom` and `TargetRoom` secondary `BoxCollider2D` components as triggers, removing the need for `Room.Awake()` to auto-fix those room trigger colliders every Play Mode entry.
- Verified remaining `m_IsTrigger: 0` colliders belong to Tilemap / Composite physics collision, not `Room` trigger detection.
- `dotnet build Project-Ark.slnx` passes after the scene changes.
- Unity online Play Mode / Console recheck is complete: the three target warnings for missing `AmbienceController` Volume, missing `RoomManager` starting room, and `Room` trigger auto-fix no longer appear. Runtime sampling confirmed `AmbienceController._postProcessVolume -> Global Volume`, `RoomManager._startingRoom -> DebugRoom`, and the relevant room trigger `BoxCollider2D.isTrigger` values are true.
- `Projectile_Matter` runtime `TrailRenderer` fallback warning is fixed at the prefab source: `Projectile_Matter.prefab` now owns a preconfigured `TrailRenderer` matching the former fallback parameters (`time = 0.15`, `width = 0.085`), and `Batch5AssetCreator` now creates the same component when regenerating the legacy test prefab.
- Play Mode recheck after clearing Console confirms the `Projectile_Matter(Clone)` / `TrailRenderer` fallback warning no longer appears. Runtime sampling found 20 `Projectile_Matter` pooled instances and all 20 carried a `TrailRenderer`.
- TMP `CardIcon` fallback glyph warning is fixed by replacing the unsupported `U+25C8` diamond marker with the ASCII `>` marker in the live `SampleScene.unity`, `ItemDetailView`, and `UICanvasBuilder` UI generation path.
- Fresh verification after clearing Console and forcing TMP mesh updates confirms `CardIcon` now renders `>` with `LiberationSans SDF`, `ItemDetailView/TypeLabel` renders `>  CORE`, and no TMP fallback glyph warning is emitted.
- Optional `ServiceLocator` missing-registration noise is fixed at the API usage source: optional / lazy / fallback dependencies now use `ServiceLocator.TryGet<T>()` instead of warning-producing `ServiceLocator.Get<T>()`.
- Covered target services include `PlayerController2D`, `WorldProgressManager`, `SaveBridge`, `MinimapManager`, `ShipHealth`, `HeatSystem`, and `AudioManager`, while required dependencies retain explicit errors or warnings at their own call sites.
- Verification: `dotnet build Project-Ark.slnx` passes; targeted grep confirms no remaining `ServiceLocator.Get<T>()` calls for the batch target service set; Unity Console recheck shows no `ServiceLocator Get: ... NOT FOUND` entries, only unrelated pre-existing compile warnings and one MCP transport warning.

Next work should continue non-blocking scene warning cleanup only if new Console evidence appears. Do not block the current Ship/VFX MVP on Dash additive trail / particle polish.
