$baseUrl = "http://localhost:8080"

# Check OpenAPI spec for Export/UnityProject parameters
$api = Invoke-WebRequest -Uri "$baseUrl/openapi.json" -UseBasicParsing
$apiJson = $api.Content | ConvertFrom-Json

# Show Export/UnityProject endpoint details
$exportPath = $apiJson.paths."/Export/UnityProject"
Write-Host "=== Export/UnityProject endpoint ==="
$exportPath | ConvertTo-Json -Depth 10

Write-Host ""
Write-Host "=== LoadFolder endpoint ==="
$loadPath = $apiJson.paths."/LoadFolder"
$loadPath | ConvertTo-Json -Depth 10
