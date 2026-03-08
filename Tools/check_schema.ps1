$baseUrl = "http://localhost:8080"

# Check PathFormData schema
$api = Invoke-WebRequest -Uri "$baseUrl/openapi.json" -UseBasicParsing
$apiJson = $api.Content | ConvertFrom-Json

Write-Host "=== PathFormData schema ==="
$apiJson.components.schemas.PathFormData | ConvertTo-Json -Depth 10

Write-Host ""
Write-Host "=== All schemas ==="
$apiJson.components.schemas.PSObject.Properties.Name
