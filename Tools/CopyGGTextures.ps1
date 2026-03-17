# CopyGGTextures.ps1
# Reference-only importer for currently retained GG RenderDoc textures.
# It only copies textures that are still kept by the active/reference pipeline.
# Retired dormant textures are intentionally excluded from this script.
# Canonical naming, active status, and ownership are defined by:
#   - Docs/Reference/ShipVFX_CanonicalSpec.md
#   - Docs/Reference/ShipVFX_AssetRegistry.md
# Usage: .\Tools\CopyGGTextures.ps1

$GGRoot  = "F:\UnityProjects\Project-Ark\GGrenderdoc\output\targeted_v7"
$VFXDest = "F:\UnityProjects\Project-Ark\Assets\_Art\VFX\BoostTrail\Textures"

# Ensure destination directory exists
New-Item -ItemType Directory -Force -Path $VFXDest | Out-Null

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

# ── Summary ──────────────────────────────────────────────────────────────────
Write-Host "`n[Done] Retained GG textures copied."
Write-Host "VFX Textures -> $VFXDest"
Write-Host "`nNext step: Open Unity Editor to let it import the new .png files,"
Write-Host "then run 'ProjectArk > Ship > VFX > Authority > Link Active BoostTrail Material Textures' to auto-assign textures to materials."
