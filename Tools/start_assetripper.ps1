Start-Process "D:\Tools\AssetRipper\AssetRipper.GUI.Free.exe" -ArgumentList "--headless", "--port", "8080" -WindowStyle Hidden
Start-Sleep -Seconds 10

try {
    $r = Invoke-WebRequest -Uri "http://localhost:8080" -UseBasicParsing -TimeoutSec 5
    Write-Host "Server running: $($r.StatusCode)"
} catch {
    Write-Host "Status: $($_.Exception.Message)"
}
