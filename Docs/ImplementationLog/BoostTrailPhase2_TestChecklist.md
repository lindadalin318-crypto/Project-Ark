# Boost Trail VFX Phase 2 — Play Mode Test Checklist

**Date**: 2026-03-10  
**Phase**: Phase 2 — Missing Features Completion  
**Tester**: Developer

---

## Pre-Test Setup

Before running Play Mode tests, complete these setup steps:

1. **Import textures**: Open Unity Editor → wait for auto-import of all new `.png` files in `Assets/_Art/VFX/BoostTrail/Textures/` and `Assets/_Art/Ship/Glitch/`
2. **Link textures**: Run `ProjectArk > VFX > Link Material Textures` → verify "Success: 15 texture assignments"
3. **Create Prefab**: Run `ProjectArk > VFX > Create BoostTrailRoot Prefab` → verify `Assets/_Prefabs/VFX/BoostTrailRoot.prefab` created
4. **Wire ShipView**: In ShipView Inspector, wire `_boostTrailView`, `_boostLiquidSprite` (Boost_16), `_normalLiquidSprite` (Movement_3)
5. **Wire Flash**: Create Canvas (Overlay) + full-screen white Image (Additive blend) → wire to `BoostTrailView._flashImage`
6. **Wire Bloom**: Create Local Volume with `BoostBloomVolumeProfile` → wire to `BoostTrailView._boostBloomVolume`

---

## Acceptance Criteria Checklist

### AC-1: All Particle Systems Activate on Boost Start
- [ ] Enter Play Mode
- [ ] Trigger Boost state (press Boost input)
- [ ] **Verify**: TrailRenderer emitting = true, trail visible behind ship
- [ ] **Verify**: FlameTrail_R and FlameTrail_B emitting purple HDR particles
- [ ] **Verify**: FlameCore emitting short-lifetime purple particles
- [ ] **Verify**: EmberTrail emitting magenta HDR particles
- [ ] **Verify**: EmberSparks fires one-shot burst (20 white HDR particles)
- [ ] **Verify**: **EmberGlow fires burst (15 orange-yellow HDR particles, lifetime 0.12s)** ← Phase 2 new
- [ ] **Expected**: All 6 particle systems active simultaneously within 1 frame

### AC-2: All Particle Systems Stop on Boost End
- [ ] While in Boost state, release Boost input
- [ ] **Verify**: TrailRenderer emitting = false (trail fades over 3.5s)
- [ ] **Verify**: FlameTrail_R/B stop emitting (existing particles fade out)
- [ ] **Verify**: EmberTrail stops emitting
- [ ] **Verify**: **EmberGlow stops emitting** ← Phase 2 new
- [ ] **Verify**: EmberSparks plays out naturally (no forced stop)
- [ ] **Expected**: All particle systems stopped within 1 frame of Boost end

### AC-3: Trail Main Effect Shader (uniforms141)
- [ ] Open `mat_trail_main_effect` in Inspector
- [ ] **Verify**: `_Slot0` = `trail_main_spritesheet.png` (4.5MB)
- [ ] **Verify**: `_Slot1` = `trail_second_spritesheet.png` (408KB)
- [ ] **Verify**: `_Slot2` = `trail_edge_glow.png` (1.5KB)
- [ ] **Verify**: `_Slot3` = `trail_color_lut.png` (90B)
- [ ] In Play Mode with Boost active: **Verify** TrailRenderer shows animated sprite sheet effect
- [ ] **Verify**: Trail output alpha = 1.0 (fully opaque, no transparency artifacts)

### AC-4: Boost Noise Textures Linked (Layer 2/3/Field)
- [ ] Open `mat_boost_energy_layer2` in Inspector
- [ ] **Verify**: `_Tex0` = `boost_noise_main.png`, `_Tex1` = `boost_noise_distort.png`
- [ ] **Verify**: `_Tex2` = `boost_noise_layer3.png`, `_Tex3` = `boost_noise_layer4.png`
- [ ] Open `mat_boost_energy_layer3` in Inspector
- [ ] **Verify**: `_Tex0` = `boost_energy_noise_a.png`, `_Tex1` = `boost_energy_main.png`
- [ ] Open `mat_boost_energy_field` in Inspector
- [ ] **Verify**: `_LUTTex` = `boost_field_main.png`, `_UseLUT` = 1
- [ ] In Play Mode: **Verify** energy layers show noise texture patterns (not solid white)

### AC-5: Full-Screen Flash Effect
- [ ] Trigger Boost state
- [ ] **Verify**: Screen flashes white (alpha 0 → 0.7 → 0) over 0.3s
- [ ] **Verify**: Flash does not persist after animation completes
- [ ] Trigger Boost multiple times rapidly
- [ ] **Verify**: Flash restarts correctly each time (no stacking)

### AC-6: URP Volume Bloom Burst
- [ ] Trigger Boost state
- [ ] **Verify**: Bloom intensity spikes (visible glow on HDR particles)
- [ ] **Verify**: Bloom returns to baseline after 0.4s
- [ ] **Verify**: HDR colors (>1.0) on particles trigger visible bloom glow

### AC-7: Object Pool Reset (No State Leakage)
- [ ] Trigger Boost state (all VFX active)
- [ ] Call `BoostTrailView.ResetState()` (simulate object pool return)
- [ ] **Verify**: TrailRenderer cleared (no trail visible)
- [ ] **Verify**: All particle systems stopped and cleared (no residual particles)
- [ ] **Verify**: **EmberGlow cleared** ← Phase 2 new
- [ ] **Verify**: `_BoostIntensity` = 0 on all energy layer shaders
- [ ] **Verify**: Flash image alpha = 0
- [ ] **Verify**: Bloom volume weight = 0

### AC-8: Liquid Sprite Switch
- [ ] Enter Boost state
- [ ] **Verify**: Ship liquid layer switches from `Movement_3` to `Boost_16`
- [ ] Exit Boost state
- [ ] **Verify**: Ship liquid layer switches back to `Movement_3`

---

## Ship Texture Size Difference Notes

| Texture | Size | Status |
|---------|------|--------|
| `ship_solid_gg.png` | 1024×1024 | ⚠️ GG original (larger than project 430×430) — **do NOT replace existing solid** |
| `ship_liquid_boost.png` | 512×512 | ⚠️ GG original (larger than Boost_16 430×430) — available as alternative |
| `ship_highlight_gg.png` | 1024×1024 | ⚠️ GG original (larger than project 430×430) — **do NOT replace existing highlight** |
| `Boost_16.png` | 430×430 | ✅ Current project Boost liquid sprite (use this) |

**Recommendation**: Use `Boost_16.png` as `_boostLiquidSprite` (matches project scale). `ship_liquid_boost.png` is available as a higher-resolution alternative if the project scale is adjusted.

---

## Known Limitations

1. **TrailMainEffect.shader**: The `mat_trail_main_effect.mat` Shader GUID is `00000000...` (placeholder). After Unity imports `TrailMainEffect.shader`, manually reassign the shader in the material Inspector.
2. **EmberGlow loop=false**: EmberGlow is configured as a one-shot burst. If Boost is held continuously, EmberGlow only fires once per `OnBoostStart()` call (correct behavior matching GG).
3. **ship_solid_gg.png / ship_highlight_gg.png**: These 1024×1024 textures are available but not integrated into ShipView (size mismatch with project's 430×430 sprites). Future work: scale adjustment or separate high-res ship skin.
