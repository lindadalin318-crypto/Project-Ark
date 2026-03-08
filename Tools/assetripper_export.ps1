$gamePath = "F:\SteamLibrary\steamapps\common\Minishoot' Adventures\Windows\Minishoot_Data"
$outputPath = "D:\Tools\Minishoot_Ripped"
$baseUrl = "http://localhost:8080"

# Step 1: Reset first
Write-Host "Resetting AssetRipper..."
Invoke-WebRequest -Uri "$baseUrl/Reset" -Method POST -UseBasicParsing | Out-Null
Start-Sleep -Seconds 2

# Step 2: Load game folder with correct field name 'path'
Write-Host "Loading game folder: $gamePath"
$body = "path=" + [System.Uri]::EscapeDataString($gamePath)
$loadResp = Invoke-WebRequest -Uri "$baseUrl/LoadFolder" `
    -Method POST `
    -ContentType "application/x-www-form-urlencoded" `
    -Body $body `
    -UseBasicParsing -TimeoutSec 300
Write-Host "Load status: $($loadResp.StatusCode)"

# Wait for loading
Write-Host "Waiting for assets to load..."
Start-Sleep -Seconds 15

# Step 3: Export as Unity project with correct field name 'path'
Write-Host "Exporting to: $outputPath"
New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
$exportBody = "path=" + [System.Uri]::EscapeDataString($outputPath)
$exportResp = Invoke-WebRequest -Uri "$baseUrl/Export/UnityProject" `
    -Method POST `
    -ContentType "application/x-www-form-urlencoded" `
    -Body $exportBody `
    -UseBasicParsing -TimeoutSec 600
Write-Host "Export triggered: $($exportResp.StatusCode)"

# Wait and check
Write-Host "Waiting for export to complete (this may take several minutes)..."
$maxWait = 300  # 5 minutes
$elapsed = 0
while ($elapsed -lt $maxWait) {
    Start-Sleep -Seconds 10
    $elapsed += 10
    $count = (Get-ChildItem $outputPath -Recurse -ErrorAction SilentlyContinue).Count
    Write-Host "[$elapsed s] Files exported: $count"
    if ($count -gt 100) {
        Write-Host "Export seems complete!"
        break
    }
}

# Show results
Write-Host ""
Write-Host "=== Export Results ==="
Get-ChildItem $outputPath -Depth 2 | Select-Object FullName | Select-Object -First 50