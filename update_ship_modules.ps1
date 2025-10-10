# PowerShell script to add using directive to ShipModuleDefinition files
$files = Get-ChildItem -Path 'SWLOR.Component.Space\Definitions\ShipModuleDefinition\*.cs' -Recurse

foreach ($file in $files) {
    Write-Host "Processing $($file.Name)"

    # Read file content
    $content = Get-Content $file.FullName -Raw

    # Add Component.Combat.Contracts if not present
    if ($content -notmatch 'using SWLOR\.Component\.Combat\.Contracts;') {
        $content = $content -replace '(using SWLOR\.Shared\.Domain\.Combat\.Contracts;)', '$1
using SWLOR.Component.Combat.Contracts;'
        Write-Host "Added Component.Combat.Contracts to $($file.Name)"
    }

    # Add Combat.Enums if not present
    if ($content -notmatch 'using SWLOR\.Shared\.Domain\.Combat\.Enums;') {
        $content = $content -replace '(using SWLOR\.Shared\.Domain\.Combat\.Contracts;)', '$1
using SWLOR.Shared.Domain.Combat.Enums;'
        Write-Host "Added Combat.Enums to $($file.Name)"
    }

    Set-Content $file.FullName $content
}
