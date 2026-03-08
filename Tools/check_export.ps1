Start-Sleep -Seconds 90

# Check if export is done
$outputPath = "D:\Tools\Minishoot_Ripped"
$items = Get-ChildItem $outputPath -Recurse -Depth 3 -ErrorAction SilentlyContinue
Write-Host "Files found: $($items.Count)"
$items | Select-Object FullName | Select-Object -First 50
