$managedPath = "F:\SteamLibrary\steamapps\common\Minishoot' Adventures\Windows\Minishoot_Data\Managed"
$dllPath = "$managedPath\Assembly-CSharp.dll"
$outputPath = "D:\UnityProjects\Project-Ark\Tools\minishoot_types.txt"

# Load all Unity DLLs first to resolve dependencies
$unityDlls = @(
    "UnityEngine.CoreModule.dll",
    "UnityEngine.dll",
    "UnityEngine.Physics2DModule.dll",
    "UnityEngine.AnimationModule.dll",
    "UnityEngine.AudioModule.dll",
    "UnityEngine.UI.dll",
    "UnityEngine.TilemapModule.dll",
    "Unity.TextMeshPro.dll",
    "Unity.InputSystem.dll",
    "Unity.RenderPipelines.Universal.Runtime.dll",
    "Unity.RenderPipelines.Core.Runtime.dll"
)

foreach ($dll in $unityDlls) {
    $path = "$managedPath\$dll"
    if (Test-Path $path) {
        try {
            [System.Reflection.Assembly]::LoadFile($path) | Out-Null
        } catch { }
    }
}

try {
    $asm = [System.Reflection.Assembly]::LoadFile($dllPath)
    try {
        $types = $asm.GetTypes()
    } catch [System.Reflection.ReflectionTypeLoadException] {
        # Get partial types even with load errors
        $types = $_.Exception.Types | Where-Object { $_ -ne $null }
        Write-Host "Partial load: $($types.Count) types loaded (some failed)"
    }
    
    $typeNames = $types | ForEach-Object { $_.FullName } | Where-Object { $_ -ne $null } | Sort-Object
    Write-Host "Total types: $($typeNames.Count)"
    $typeNames | Out-File -FilePath $outputPath -Encoding UTF8
    Write-Host "Saved to: $outputPath"
    
    # Print first 100 for preview
    $typeNames | Select-Object -First 100
} catch {
    Write-Host "Fatal Error: $_"
}