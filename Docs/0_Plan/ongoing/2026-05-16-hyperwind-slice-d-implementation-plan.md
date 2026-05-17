# HyperWind Slice D' Implementation Plan

> **For agentic workers:** This is the active task-by-task plan for `HyperWind_MechanicsBrief.md` §10. Track progress with checkbox (`- [ ]` / `- [x]`) items. Every file modification must be logged in `Docs/5_ImplementationLog/ImplementationLog_2026-05.md` before completion.

**Goal:** Build the first playable HyperWind MVP slice, **D' · Ground Cyclone Arena**, to validate both “wind feel” and “ground cyclone tactics” in a 5-10 minute combat room.

**Design Source:** `Docs/1_GameDesign/HyperWind_MechanicsBrief.md` §10.  
**Architecture Source:** `Docs/2_TechnicalDesign/HyperWind/HyperWind_ArchBrief.md`.  
**Workflow Source:** `Docs/3_WorkflowsAndRules/Project/ProceduralPresentation_WorkflowSpec.md`.

---

## Global constraints

- Implement against the latest D' spec in `HyperWind_MechanicsBrief.md` §10, not the older Slice D summary in §6.
- Default L8 release mode is **direction inheritance** from the player's last fire direction.
- L8 capacity is **15 projectiles**.
- L8 draw phase is **4 seconds**.
- L8 acceleration / damage multiplier cap is **×2.5**.
- MVP procedural visuals are allowed, but gameplay must not depend on procedural texture / sprite / mesh internals.
- Do not let procedural preview become the long-term owner unless explicitly promoted later.
- Do not include `LaserBeam` or `EchoWave` in cyclone capture for the first playable slice.
- Avoid runtime `FindObjectOfType` / `FindAnyObjectByType`; use `ServiceLocator` or explicit references.
- Do not modify `.meta` files manually.
- Do not run `git commit` unless the user explicitly asks.

---

## Current decisions

| Topic | Decision |
|------|----------|
| MVP slice | D' · Ground Cyclone Arena |
| Wind field first implementation | Rectangular regions, later replaceable by texture/vector field |
| Service location | `IWindFieldService` / `IWindPhaseService` via `ServiceLocator` |
| Script root | First shared contracts in `Assets/Scripts/Core/HyperWind/` |
| Visual strategy | Procedural preview first, replaceable `View` layer |
| First projectile capture scope | `Projectile` + `EnemyProjectile` only |
| First enemy | Reuse / extend Charge Rusher family as E1 Wind Rider |

---

## File structure map

### Current / planned docs

- `Docs/1_GameDesign/HyperWind_MechanicsBrief.md`
- `Docs/2_TechnicalDesign/HyperWind/HyperWind_ArchBrief.md`
- `Docs/0_Plan/ongoing/2026-05-16-hyperwind-slice-d-implementation-plan.md`
- `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

### Current / planned runtime scripts

- `Assets/Scripts/Core/HyperWind/WindSample.cs`
- `Assets/Scripts/Core/HyperWind/IWindFieldService.cs`
- `Assets/Scripts/Core/HyperWind/IWindPhaseService.cs`
- `Assets/Scripts/Core/HyperWind/WindPhaseController.cs`
- `Assets/Scripts/Core/HyperWind/WindFieldManager.cs`
- `Assets/Scripts/Ship/...` — M1 ship wind integration, exact adapter vs direct edit to be decided during Task 2
- `Assets/Scripts/Combat/...` — S1 projectile wind integration and L8 projectile capture
- `Assets/Scripts/Combat/Enemy/...` — E1 wind rider behavior
- `Assets/Scripts/Level/...` — arena / cyclone spawning integration if needed

---

## Task 0: Architecture and plan setup

**Status:** In progress / mostly complete.

**Files:**
- Create: `Docs/2_TechnicalDesign/HyperWind/HyperWind_ArchBrief.md`
- Create: `Docs/0_Plan/ongoing/2026-05-16-hyperwind-slice-d-implementation-plan.md`
- Modify: `Docs/0_Plan/ongoing/README.md`
- Modify: `Docs/5_ImplementationLog/ImplementationLog_2026-05.md`

- [x] **Step 0.1: Create HyperWind architecture brief**
  - Define slice D' scope.
  - Define cross-module boundaries.
  - Define procedural visual replaceability seam.
  - Define Batch 1 acceptance criteria.

- [x] **Step 0.2: Create ongoing implementation plan**
  - Track completed and remaining tasks with checkboxes.
  - Link design source, architecture source, and workflow source.

- [x] **Step 0.3: Keep plan updated after each task**
  - Marked completed steps through Task 8 closeout.
  - Added discovered risks / scope changes, including runtime `GroundCyclone` layer initialization and non-HyperWind known Console issues.
  - Kept implementation log aligned.


---

## Task 1: G1/G2/G3 wind field foundation

**Goal:** Make HyperWind sampleable and visible in SceneView before touching ship/projectile feel.

**Files:**
- Create: `Assets/Scripts/Core/HyperWind/WindSample.cs`
- Create: `Assets/Scripts/Core/HyperWind/IWindFieldService.cs`
- Create: `Assets/Scripts/Core/HyperWind/IWindPhaseService.cs`
- Create: `Assets/Scripts/Core/HyperWind/WindPhaseController.cs`
- Create: `Assets/Scripts/Core/HyperWind/WindFieldManager.cs`

- [x] **Step 1.1: Add wind sample contract**
  - `WindSample` exposes direction, base speed, phase multiplier, final speed, velocity.

- [x] **Step 1.2: Add phase service**
  - `IWindPhaseService` exposes current state, cycle progress, multiplier.
  - `WindPhaseController` implements weak / sand warning / audio warning / strong phases.

- [x] **Step 1.3: Add wind field service**
  - `IWindFieldService.Sample(Vector2 worldPosition)` is the only consumer-facing API.
  - `WindFieldManager` implements MVP rectangular regions.

- [x] **Step 1.4: Add SceneView debug arrows**
  - Selected `WindFieldManager` draws region boxes and wind direction arrows.

- [x] **Step 1.5: Validate compile status**
  - `read_lints` returns 0 diagnostics for `Assets/Scripts/Core/HyperWind`.
  - Unity MCP compile refresh returns ready; Console errors = 0.

---

## Task 2: M1 ship wind integration

**Goal:** Make the ship feel wind without letting wind speed get eaten by `ShipMotor.ClampSpeed()`.

**Candidate files:**
- Inspect / modify: `Assets/Scripts/Ship/Movement/ShipMotor.cs`
- Possible new adapter: `Assets/Scripts/Ship/Movement/ShipWindMotorAdapter.cs`

- [x] **Step 2.1: Decide integration shape**
  - Chosen: direct `ShipMotor` wind layer.
  - Avoids new adapter ordering ambiguity for MVP.
  - Uses cached `IWindFieldService` via `ServiceLocator.TryGet`, not per-frame `Get`.

- [x] **Step 2.2: Preserve player thrust speed cap**
  - Player-controlled velocity remains clamped by `RuntimeMaxSpeed`.
  - Environmental wind velocity is removed at the start of each physics frame, then re-added after `ClampSpeed()`.

- [x] **Step 2.3: Add tunable strength**
  - Added `_enableWindFieldInfluence` and `_windVelocityMultiplier` to `ShipMotor`.
  - Exposed `CurrentWindVelocity` / `PlayerControlledVelocity` for debug and later validation.

- [x] **Step 2.4: Validate feel**

  - Runtime smoke validation in Play Mode passed with temporary `WindPhaseController` / `WindFieldManager` managers.
  - Left region pushed ship right: sampled / applied wind velocity ≈ `(1.63, 0.00)`.
  - Center region was calmer: sampled / applied wind velocity ≈ `(0.49, 0.00)`.
  - Right region pushed ship left: sampled / applied wind velocity ≈ `(-1.63, 0.00)`.
  - Human hand-feel tuning remains for the final arena pass, but the M1 velocity layering is technically valid.

- [x] **Step 2.5: Create dedicated HyperWind test scene**
  - Created `Assets/Scenes/HyperWind/HyperWind_SliceD_Test.unity`.
  - Scene contains `HyperWindRuntimeRoot` with `WindPhaseController` + `WindFieldManager`.
  - Scene includes `HyperWind_TestShip`, a test camera, three region tints, region labels, and boundary markers.
  - First obvious-lab tuning (`±9` side wind) proved visible but behaved like a wind wall, preventing comfortable entry into side spaces.
  - Current traversal-lab tuning: center lane is calm (`0`), left region pushes outward left at about `(-3, 0)`, right region pushes outward right at about `(3, 0)`, ship wind multiplier = `1.0`.
  - Purpose: make left/right spaces enterable first, then later reintroduce headwind/tailwind combat lanes after S1/L8 are online.

  - This scene is the dedicated lab for M1/S1/L8 validation and should not replace `SampleScene.unity`.


---

## Task 3: S1 projectile wind integration


**Goal:** Make regular physical projectiles visibly drift under wind.

**Candidate files:**
- Inspect / modify: `Assets/Scripts/Combat/Projectile/Projectile.cs`
- Inspect / modify: `Assets/Scripts/Combat/Enemy/EnemyProjectile.cs`
- Possible shared helper: `Assets/Scripts/Combat/Projectile/ProjectileWindUtility.cs`

- [x] **Step 3.1: Define windable projectile scope**
  - Included player `Projectile`.
  - Included `EnemyProjectile`.
  - Excluded `LaserBeam` and `EchoWave` for MVP.

- [x] **Step 3.2: Apply wind drift**
  - Wind bends trajectory over time by accumulating a capped drift velocity.
  - Drift strength is tunable through `_windDriftAcceleration` and `_maxWindDriftSpeed`.
  - Play Mode smoke result: player projectile in left wind changed from `(0, 10)` to `(-8, 10)`; enemy projectile in right wind changed from `(0, 10)` to `(8, 10)`.

- [x] **Step 3.3: Keep StarChart modifiers safe**
  - Wind drift is removed before each projectile update and re-applied after existing `IProjectileModifier` updates.
  - This preserves authored/projectile modifier velocity writes as the base layer before wind drift.
  - Full per-modifier feel checks remain for later combat tuning.

- [x] **Step 3.4: Track last player fire direction**
  - Added `CombatEvents.OnPlayerProjectileFired` and `RaisePlayerProjectileFired(position, direction)`.
  - `Projectile.Initialize(...)` raises the event for player-owned physical projectiles.
  - Play Mode smoke result: event raised with last direction `(0, 1)`.


---

## Task 4: L8 ground cyclone gameplay core

**Goal:** Implement the playable double-edged projectile amplifier.

**Planned files:**
- `Assets/Scripts/Combat/HyperWind/GroundCyclone.cs`
- `Assets/Scripts/Combat/HyperWind/GroundCycloneSpawner.cs`
- `Assets/Scripts/Combat/HyperWind/CapturedProjectileState.cs`
- `Assets/Scripts/Combat/HyperWind/ICycloneCaptureTarget.cs` or equivalent adapter layer

- [x] **Step 4.1: Define capture interface / adapter**
  - Added `ICycloneCaptureTarget` plus `CapturedProjectileState`.
  - Player `Projectile` and enemy `EnemyProjectile` implement capture / release / discard methods.
  - Capture disables projectile collision and pauses lifetime by setting `_isAlive=false`; release restores ownership behavior and collider state.

- [x] **Step 4.2: Implement cyclone lifecycle**
  - Added `GroundCyclone` with Spawn → Draw → Burst → Finished state flow.
  - Spawn: 0.5s default, no capture.
  - Draw: 4s default, capture and orbit.
  - Burst: 0.3s default, release all.

- [x] **Step 4.3: Implement capacity**
  - Max capacity defaults to 15.
  - Overflow calls `DiscardByCyclone()` when `_discardOverflow` is enabled.

- [x] **Step 4.4: Implement orbit accumulation**
  - Captured bullets orbit on `_orbitRadius` and track completed turns.
  - Visual countability is deferred to Task 5 view layer.

- [x] **Step 4.5: Implement direction inheritance**
  - Release direction reads the latest `CombatEvents.OnPlayerProjectileFired` direction.
  - Applies to both player and enemy projectiles.

- [x] **Step 4.6: Implement speed/damage amplification**
  - Speed and damage multiplier = `1 + completedTurns * _speedMultiplierPerTurn`, capped at ×2.5.
  - Player projectile smoke: captured/released with velocity about `(0, 11.08)` and damage `11.08`.
  - Enemy projectile smoke: captured/released with velocity about `(11.08, 0)` and damage `11.08`, preserving enemy projectile type.
  - Note: current project has no `EnemyProjectile` layer; `EnemyProjectile.prefab` is on `Default`, so `GroundCyclone.Reset()` explicitly includes `PlayerProjectile + Default` and still filters by `ICycloneCaptureTarget`.


---

## Task 5: L8 procedural view layer

**Goal:** Make cyclone behavior readable and impressive while remaining replaceable.

**Planned files:**
- `Assets/Scripts/Combat/HyperWind/GroundCycloneView.cs`
- Optional: `Assets/Scripts/Combat/HyperWind/ProceduralCycloneRingRenderer.cs`

- [x] **Step 5.1: Spawn warning visual**
  - Added procedural warning ring and spiral during `GroundCyclonePhase.Spawn`.

- [x] **Step 5.2: Draw phase visual**
  - Added stable vortex spiral, influence ring, and orbit ring during `GroundCyclonePhase.Draw`.
  - Captured projectile count remains gameplay-readable through orbiting bullets; extra count UI is deferred.

- [x] **Step 5.3: Burst visual**
  - Added expanding burst ring and fading spiral during `GroundCyclonePhase.Burst`.

- [x] **Step 5.4: Add visibility diagnostics**
  - `GroundCycloneView` validates material creation and LineRenderer point counts.
  - Play Mode smoke result: spawned runtime cyclones had `view=True`, `lineCount=3`, `visibleLineCount=3`.

- [x] **Step 5.5: Maintain replaceability**
  - `GroundCycloneView` only consumes `GroundCyclone` visual intent (`CurrentPhase`, progress, radii, capture count).
  - Runtime spawner auto-adds `GroundCycloneView` when no prefab view exists.
  - View can be replaced by prefab / shader / VFX without modifying `GroundCyclone` gameplay logic.


---

## Task 6: E1 wind rider enemy

**Goal:** Add a readable wind-themed pressure enemy without overtaking the cyclone mechanic.

**Candidate files:**
- Reuse / inspect: `Assets/Scripts/Combat/Enemy/ChargeRusherBrain.cs`
- Reuse / inspect: charge state files under `Assets/Scripts/Combat/Enemy/States/`
- Possible new files: `WindRiderBrain.cs`, `WindRiderWindAssist.cs`

- [x] **Step 6.1: Reuse Charge Rusher telegraph/attack/recovery**
  - Created `Enemy_WindRider.prefab` from `Enemy_ChargeRusher.prefab`.
  - It preserves `ChargeRusherBrain` / `ChargeState`, so Signal-Window readability stays intact.

- [x] **Step 6.2: Add wind-aligned charge behavior**
  - Added `WindRiderWindAssist` as a late-frame environmental velocity layer.
  - When moving aligned with sampled wind, it adds wind assist and tints the sprite toward cyan.
  - Smoke result in right wind `(3, 0)`: velocity `(5, 0)` became `(8.75, 0)` with assist `(3.75, 0)`; velocity `(-5, 0)` stayed `(-5, 0)` with assist `(0, 0)`.

- [x] **Step 6.3: Configure 4-6 enemies for arena**
  - Added 4 `WindRider_Test_*` instances to `HyperWind_SliceD_Test.unity`.
  - Full EncounterSO / EnemySpawner integration is deferred to Task 7 arena assembly.


---

## Task 7: Arena slice assembly

**Goal:** Build a 5-10 minute playable test room.

**Candidate systems:**
- `Room`
- `ArenaController`
- `EncounterSO`
- `EnemySpawner`
- `WaveSpawnStrategy`

- [x] **Step 7.1: Create or configure arena room**
  - Medium open room exists in `Assets/Scenes/HyperWind/HyperWind_SliceD_Test.unity`.
  - Current layout keeps left/right traversable outward wind bands and center cyclone lane for the lab pass.
  - Added `HyperWindArenaTestDirector` to define arena bounds / cyclone lane gizmos and drive test projectile traffic.

- [x] **Step 7.2: Add cyclone spawner**
  - `GroundCycloneSpawner` is configured for the center lane: every 10s spawn 1-2 cyclones at random valid positions.
  - Added `HyperWindArenaSceneConfigurator` menu entry to re-apply the scene setup explicitly when Unity is available.

- [x] **Step 7.3: Add enemies**
  - 4 `WindRider_Test_*` instances are present for the first playable pass.
  - Added pooled enemy projectile volley support through `HyperWindArenaTestDirector` so enemy bullet backlash can be tested without full EncounterSO wiring.

- [x] **Step 7.4: Playtest success questions**
  - User local validation passed for the first playable slice.
  - Wind feel, cyclone readability, delayed volley, enemy bullet backlash, and Wind Rider pressure are accepted for MVP continuation.


- [x] **Step 7.5: Apply scene configurator once Unity session is available**
  - Executed `ProjectArk > HyperWind > Configure Slice D Test Arena` through Unity MCP.
  - Attached the arena director, explicit `PoolManager`, projectile prefab references, and ship references.
  - Scene saved cleanly with active scene `Assets/Scenes/HyperWind/HyperWind_SliceD_Test.unity`.



---

## Task 8: Validation and documentation closeout

- [x] **Step 8.1: Unity compile and console validation**
  - `refresh_unity` wait for ready passed after final HyperWind closeout changes.
  - HyperWind-related C# compile errors = 0.
  - HyperWind blocking runtime errors = 0 after runtime `GroundCyclone` capture layer fix.
  - Full Console still has known non-HyperWind BoostTrail / SRP Batcher / AudioListener entries; these are deferred to separate cleanup and do not block Slice D' MVP closeout.


- [x] **Step 8.2: Play Mode smoke test**
  - Entered arena through Unity MCP and local Unity validation.
  - Runtime root has `WindPhaseController`, `WindFieldManager`, `GroundCycloneSpawner`, and `HyperWindArenaTestDirector`.
  - `HyperWindPoolManager` exists in edit scene; Play Mode generated `GroundCyclone_Runtime` objects in smoke passes.
  - Projectile fallback warning is now once-per-prefab-key instead of pool prewarm spam.
  - Added auto-smoke instrumentation: `HyperWindArenaTestDirector` auto-fires player projectiles through the cyclone lane and exposes fired / captured / released counters.
  - Local Play Mode log confirmed the chain once: `playerFired=66`, `enemyFired=7`, `cycloneCaptured=3`, `cycloneReleased=3`.
  - Fixed a follow-up warning: runtime-created `GroundCyclone` instances now configure explicit `PlayerProjectile + Default` capture layers in `Awake`, because `Reset()` is not guaranteed for runtime `AddComponent` creation.
  - User completed manual validation and confirmed the slice is acceptable for the next step.





- [x] **Step 8.3: Update architecture docs**
  - Updated `HyperWind_ArchBrief.md` from Batch 0 architecture sketch to MVP closeout architecture facts.
  - Documented `HyperWindArenaTestDirector`, `HyperWindArenaSceneConfigurator`, GroundCyclone capture/release instrumentation, runtime capture layer initialization, and test-only owner boundaries.


- [x] **Step 8.4: Update implementation log**
  - Logged every file changed through the current validation pass.


---

## Known risks / follow-ups

1. `ShipMotor.ClampSpeed()` wind-layer risk is currently mitigated by removing old wind velocity before player movement and re-adding after `ClampSpeed()`.
2. Projectile capture lifetime / collision / pooling conflicts are currently mitigated for `Projectile` and `EnemyProjectile`; Laser / EchoWave remain intentionally out of scope.
3. “Speed = damage” is implemented explicitly in L8 release multiplier, not as a universal damage rule.
4. Procedural visuals can become hidden gameplay dependencies unless `GroundCycloneView` stays strictly replaceable.
5. Existing `.slnx` build can fail when Unity-generated `.csproj` files are absent; Unity compile / Console validation is the more reliable short-term gate.
6. Current MVP still uses `Default` layer for `EnemyProjectile`; if a dedicated `EnemyProjectile` layer is added later, update `GroundCyclone` mask and Physics2D collision matrix.
7. BoostTrail / SRP Batcher / AudioListener Console entries are known non-HyperWind cleanup items before a polished demo capture.

