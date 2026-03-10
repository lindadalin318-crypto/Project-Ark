# CopyGGTextures.ps1
# Copies and renames GGrenderdoc extracted textures into Project Ark asset directories.
# Usage: .\Tools\CopyGGTextures.ps1

$GGRoot   = "F:\UnityProjects\Project-Ark\GGrenderdoc\output\targeted_v7"
$VFXDest  = "F:\UnityProjects\Project-Ark\Assets\_Art\VFX\BoostTrail\Textures"
$ShipDest = "F:\UnityProjects\Project-Ark\Assets\_Art\Ship\Glitch"

# Ensure destination directories exist
New-Item -ItemType Directory -Force -Path $VFXDest  | Out-Null
New-Item -ItemType Directory -Force -Path $ShipDest | Out-Null

# ── Helper ──────────────────────────────────────────────────────────────────
function Copy-Tex {
    param([string]$Src, [string]$Dst)
    if (Test-Path $Src) {
        Copy-Item $Src -Destination $Dst -Force
        Write-Host "  [OK] $(Split-Path $Dst -Leaf)"
    } else {
        Write-Warning "  [MISSING] $Src"
    }
}

# ── eid_1598 : Trail Main Effect ─────────────────────────────────────────────
Write-Host "`n[eid_1598] Trail Main Effect textures"
$src1598 = "$GGRoot\eid_1598"
Copy-Tex "$src1598\tex_slot0.png" "$VFXDest\trail_main_spritesheet.png"
Copy-Tex "$src1598\tex_slot1.png" "$VFXDest\trail_second_spritesheet.png"
Copy-Tex "$src1598\tex_slot2.png" "$VFXDest\trail_edge_glow.png"
Copy-Tex "$src1598\tex_slot3.png" "$VFXDest\trail_color_lut.png"

# ── eid_1076 : Boost Noise ───────────────────────────────────────────────────
Write-Host "`n[eid_1076] Boost Noise textures"
$src1076 = "$GGRoot\eid_1076"
Copy-Tex "$src1076\tex_slot0.png" "$VFXDest\boost_noise_main.png"
Copy-Tex "$src1076\tex_slot1.png" "$VFXDest\boost_noise_distort.png"
Copy-Tex "$src1076\tex_slot2.png" "$VFXDest\boost_noise_layer3.png"
Copy-Tex "$src1076\tex_slot3.png" "$VFXDest\boost_noise_layer4.png"

# ── eid_1126 : Boost Energy Layers ──────────────────────────────────────────
Write-Host "`n[eid_1126] Boost Energy Layer textures"
$src1126 = "$GGRoot\eid_1126"
Copy-Tex "$src1126\tex_slot0.png" "$VFXDest\boost_energy_noise_a.png"
Copy-Tex "$src1126\tex_slot2.png" "$VFXDest\boost_energy_main.png"

# ── eid_1964 : Boost Energy Field ───────────────────────────────────────────
Write-Host "`n[eid_1964] Boost Energy Field textures"
$src1964 = "$GGRoot\eid_1964"
Copy-Tex "$src1964\tex_slot0.png" "$VFXDest\boost_field_main.png"
Copy-Tex "$src1964\tex_slot1.png" "$VFXDest\boost_field_aux.png"

# ── eid_877 : Ship Body ──────────────────────────────────────────────────────
Write-Host "`n[eid_877] Ship Body textures"
$src877 = "$GGRoot\eid_877"
Copy-Tex "$src877\tex_slot0.png" "$ShipDest\ship_solid_gg.png"
Copy-Tex "$src877\tex_slot1.png" "$ShipDest\ship_liquid_boost.png"
Copy-Tex "$src877\tex_slot2.png" "$ShipDest\ship_highlight_gg.png"

# ── Summary ──────────────────────────────────────────────────────────────────
Write-Host "`n[Done] All textures copied."
Write-Host "VFX Textures -> $VFXDest"
Write-Host "Ship Textures -> $ShipDest"
Write-Host "`nNext step: Open Unity Editor to let it import the new .png files,"
Write-Host "then run 'ProjectArk > VFX > Link Material Textures' to auto-assign textures to materials."
