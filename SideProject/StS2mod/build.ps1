# Astrolabe Mod 一键构建脚本
# 用法：在 PowerShell 中运行：.\build.ps1
#
# 功能：
#   1. dotnet build 编译 Astrolabe.dll
#   2. Godot headless 导出 Astrolabe.pck
#   3. 将 .dll + .pck 部署到游戏 mods 目录

# ── 配置 ──────────────────────────────────────────────────────
$GodotExe    = "D:\Program Files (x86)\Godot_v4.6.1-stable_win64.exe"
$ProjectDir  = "$PSScriptRoot\src\Astrolabe\pack"
$BuildDir    = "$PSScriptRoot\src\Astrolabe"
$OutputPck   = "$PSScriptRoot\src\Astrolabe\bin\Debug\net9.0\Astrolabe.pck"
$ModsTarget  = "F:\SteamLibrary\steamapps\common\Slay the Spire 2\mods\Astrolabe"
# ─────────────────────────────────────────────────────────────

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "=== Astrolabe Build Script ===" -ForegroundColor Cyan
Write-Host ""

# ── Step 1: dotnet build ──────────────────────────────────────
Write-Host "[1/3] Building C# project..." -ForegroundColor Yellow
Push-Location $BuildDir
try {
    dotnet build --no-restore
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed" }
    Write-Host "  [OK] Astrolabe.dll compiled" -ForegroundColor Green
} finally {
    Pop-Location
}

# ── Step 2: Godot headless export PCK ────────────────────────
Write-Host ""
Write-Host "[2/3] Exporting PCK with Godot headless..." -ForegroundColor Yellow

# Godot 首次运行项目时需要导入资源，用 --headless --editor --quit 预热
Write-Host "  Importing project assets (first run may take ~10s)..."
$importArgs = @(
    "--path", $ProjectDir,
    "--headless",
    "--editor",
    "--quit"
)
& $GodotExe @importArgs 2>&1 | Out-Null
Start-Sleep -Seconds 2

# 执行 PCK 导出
$pckArgs = @(
    "--path", $ProjectDir,
    "--headless",
    "--export-pack", "BasicExport",
    $OutputPck
)
Write-Host "  Running: godot --export-pack BasicExport -> $OutputPck"
& $GodotExe @pckArgs 2>&1
# Godot writes the PCK asynchronously; wait a moment before checking
Start-Sleep -Seconds 3

if (Test-Path $OutputPck) {
    $pckSize = (Get-Item $OutputPck).Length
    Write-Host "  [OK] Astrolabe.pck generated ($pckSize bytes)" -ForegroundColor Green
} else {
    Write-Host "  [ERROR] PCK file not found at: $OutputPck" -ForegroundColor Red
    Write-Host "  Check Godot version compatibility (game uses 4.5.1, you have 4.6.1)" -ForegroundColor Yellow
    exit 1
}

# ── Step 3: Deploy to game mods folder ───────────────────────
Write-Host ""
Write-Host "[3/3] Deploying to game mods folder..." -ForegroundColor Yellow

if (-not (Test-Path $ModsTarget)) {
    New-Item -ItemType Directory -Path $ModsTarget -Force | Out-Null
}

$dllSrc = "$BuildDir\bin\Debug\net9.0\Astrolabe.dll"
Copy-Item $dllSrc -Destination "$ModsTarget\Astrolabe.dll" -Force
Copy-Item $OutputPck -Destination "$ModsTarget\Astrolabe.pck" -Force

Write-Host "  [OK] Deployed to: $ModsTarget" -ForegroundColor Green

Write-Host ""
Write-Host "=== Build Complete ===" -ForegroundColor Cyan
Write-Host "  DLL: $ModsTarget\Astrolabe.dll"
Write-Host "  PCK: $ModsTarget\Astrolabe.pck"
Write-Host ""
Write-Host "Now launch Slay the Spire 2 and check:" -ForegroundColor White
Write-Host "  %AppData%\Roaming\SlayTheSpire2\Player.log"
Write-Host "  Look for: '=== Astrolabe v0.1.0 initializing ==='"
