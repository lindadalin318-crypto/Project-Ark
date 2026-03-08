$assetsPath = "D:\Tools\Minishoot_Ripped\ExportedProject\Assets"

Write-Host "=== Asset Directory Structure ==="
$dirs = Get-ChildItem $assetsPath -Recurse -Directory | Select-Object FullName
$dirs | ForEach-Object { $_.FullName.Replace($assetsPath, "") } | Select-Object -First 50

Write-Host ""
Write-Host "=== File counts by extension ==="
Get-ChildItem $assetsPath -Recurse -File | 
    Group-Object Extension | 
    Sort-Object Count -Descending | 
    Select-Object Name, Count | 
    Select-Object -First 20

Write-Host ""
Write-Host "=== Top-level folders with file counts ==="
Get-ChildItem $assetsPath -Directory | ForEach-Object {
    $count = (Get-ChildItem $_.FullName -Recurse -File).Count
    [PSCustomObject]@{ Folder = $_.Name; Files = $count }
} | Sort-Object Files -Descending
