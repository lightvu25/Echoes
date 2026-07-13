try {
    $asm = [Reflection.Assembly]::LoadFrom('d:\UnityProject\Echoes\Assets\Plugins\NuGet\McpPlugin.dll')
} catch [System.Reflection.ReflectionTypeLoadException] {
    $out = @()
    foreach ($t in $_.Exception.Types) {
        if ($t -ne $null -and $t.Name -like '*McpPluginBuilder*') {
            $out += "Type: $($t.Name)"
        }
    }
    $out | Out-File -FilePath 'd:\UnityProject\Echoes\types.txt'
}
