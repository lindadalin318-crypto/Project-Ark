$baseUrl = "http://localhost:8080"

# Check available API endpoints
Write-Host "=== Checking OpenAPI spec ==="
try {
    $api = Invoke-WebRequest -Uri "$baseUrl/openapi.json" -UseBasicParsing -TimeoutSec 10
    $apiJson = $api.Content | ConvertFrom-Json
    Write-Host "API paths:"
    $apiJson.paths.PSObject.Properties.Name | Sort-Object
} catch {
    Write-Host "OpenAPI error: $($_.Exception.Message)"
}
