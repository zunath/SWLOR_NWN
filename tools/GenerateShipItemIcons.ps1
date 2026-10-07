<#
.SYNOPSIS
Renders original ship artwork as framed action and native inventory icons using the shared icon tools.
#>
param([string]$MagickPath = "magick")
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$manifest = Join-Path $repoRoot "SWLOR.Game.Server/Readmes/ShipItemIconManifest.csv"
$bindings = @(Import-Csv (Join-Path $repoRoot "SWLOR.Game.Server/Readmes/ShipItemIconBindings.csv"))
$artwork = @(Import-Csv $manifest)
$actionPath = Join-Path $repoRoot "SWLOR_Haks/sw_ability"
$inventoryPath = Join-Path $repoRoot "SWLOR_Haks/sw_item"

function Render-Icon([string]$Source, [string]$Target, [int]$Size) {
    $contentSize = [int]($Size * 0.8125)
    & $MagickPath $Source -resize "${contentSize}x${contentSize}!" -background "#080A0D" -gravity center -extent "${Size}x${Size}" -alpha on -channel A -evaluate set 100% +channel -type TrueColorAlpha -compress None -define tga:bits-per-pixel=32 -flip -orient BottomLeft $Target
    if ($LASTEXITCODE -ne 0) { throw "ImageMagick failed for $Source" }
}

foreach ($row in $artwork) {
    $source = Join-Path $repoRoot $row.SourcePath
    if (!(Test-Path -LiteralPath $source)) { throw "Missing original artwork: $source" }
    Render-Icon $source (Join-Path $actionPath "$($row.IconResRef).tga") 32
    foreach ($binding in @($bindings | Where-Object ActionIcon -EQ $row.IconResRef | Sort-Object InventoryIcon -Unique)) {
        Render-Icon $source (Join-Path $inventoryPath "$($binding.InventoryIcon).tga") 64
    }
}

# Both sizes use the established semantic stamper. Native aliases must retain the engine's inventory naming.
& (Join-Path $PSScriptRoot "UpdateFeatSpellIconBorders.ps1") -ManifestPath $manifest -IconPath $actionPath -Apply -Force
$temporaryManifest = [System.IO.Path]::GetTempFileName()
try {
    $bindings | Sort-Object InventoryIcon -Unique | ForEach-Object {
        [pscustomobject]@{ Type = "Item"; Key = $_.Role; IconResRef = $_.InventoryIcon; SemanticCategory = $_.SemanticCategory; Alignment = "" }
    } | Export-Csv -LiteralPath $temporaryManifest -NoTypeInformation
    & (Join-Path $PSScriptRoot "UpdateFeatSpellIconBorders.ps1") -ManifestPath $temporaryManifest -IconPath $inventoryPath -IconSize 64 -Apply -Force
}
finally { Remove-Item -LiteralPath $temporaryManifest }

$previousPath = $env:PATH
try {
    $env:PATH = "$(Split-Path -Parent (Get-Command $MagickPath).Source);$previousPath"
    & (Join-Path $PSScriptRoot "GenerateShipModuleCooldownIcons.ps1") -Force
}
finally { $env:PATH = $previousPath }
