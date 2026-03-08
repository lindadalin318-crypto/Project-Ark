$gamePath = "F:\SteamLibrary\steamapps\common\Minishoot' Adventures\Windows\Minishoot_Data"
$outputPath = "D:\Tools\Minishoot_Ripped"
$baseUrl = "http://localhost:8080"

Write-Host "Step 1: Loading game folder..."
try {
    $loadResp = Invoke-WebRequest -Uri "$baseUrl/LoadFolder" `
        -Method POST `
        -ContentType "application/x-www-form-urlencoded" `
        -Body "path=$([System.Uri]::EscapeDataString($gamePath))" `
        -UseBasicParsing -TimeoutSec 60
    Write-Host "Load response: $($loadResp.StatusCode) - $($loadResp.Content)"
} catch {
    Write-Host "Load error: $($_.Exception.Message)"
    Write-Host "Response: $($_.Exception.Response)"
}
