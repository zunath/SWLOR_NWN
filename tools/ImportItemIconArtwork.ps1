#Requires -PSEdition Desktop
<# .SYNOPSIS Imports a selected original item image using ImageMagick and shared semantic framing. #>
param(
    [Parameter(Mandatory)][string]$SourcePath,
    [Parameter(Mandatory)][string]$Key,
    [string]$MagickPath = 'magick'
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$specPath = Join-Path $repoRoot 'SWLOR_Haks/sw_item_source/general/item-artwork.json'
$asset = @((Get-Content -LiteralPath $specPath -Raw | ConvertFrom-Json).assets | Where-Object key -EQ $Key)
if ($asset.Count -ne 1) { throw "Unknown or ambiguous item artwork key: $Key" }
$asset = $asset[0]
$source = Join-Path $repoRoot "SWLOR_Haks/sw_item_source/general/$($asset.source)"
& $MagickPath $SourcePath -resize '256x256!' $source
if ($LASTEXITCODE -ne 0) { throw "Source image import failed: $Key" }
$bindings = @(Import-Csv (Join-Path $repoRoot 'SWLOR.Game.Server/Readmes/ItemIconBindings.csv') | Where-Object Role -EQ $Key)
$targets = @([pscustomobject]@{ Path = "SWLOR_Haks/sw_ability/$($asset.icon).tga"; Size = 32; Icon = $asset.icon })
$targets += @($bindings | Sort-Object InventoryIcon -Unique | ForEach-Object {
    [pscustomobject]@{ Path = "SWLOR_Haks/sw_item/$($_.InventoryIcon).tga"; Size = 64; Icon = $_.InventoryIcon }
})
foreach ($target in $targets) {
    $size = $target.Size
    $contentSize = [int]($size * 0.8125)
    & $MagickPath $source -resize "${contentSize}x${contentSize}!" -background '#080A0D' -gravity center -extent "${size}x${size}" -alpha on -channel A -evaluate set '100%' +channel -type TrueColorAlpha -compress None -define tga:bits-per-pixel=32 -flip -orient BottomLeft (Join-Path $repoRoot $target.Path)
    if ($LASTEXITCODE -ne 0) { throw "Icon render failed: $($target.Icon)" }
}
$temporary = [System.IO.Path]::GetTempFileName()
try {
    foreach ($size in @(32,64)) {
        $targets | Where-Object Size -EQ $size | ForEach-Object {
            [pscustomobject]@{Type='Item'; Key=$Key; IconResRef=$_.Icon; SemanticCategory=$asset.category; Alignment=''}
        } | Export-Csv -NoTypeInformation -LiteralPath $temporary
        $folder = if ($size -eq 32) { 'SWLOR_Haks/sw_ability' } else { 'SWLOR_Haks/sw_item' }
        & (Join-Path $PSScriptRoot 'UpdateFeatSpellIconBorders.ps1') -ManifestPath $temporary -IconPath (Join-Path $repoRoot $folder) -IconSize $size -Apply -Force | Out-Null
    }
} finally { Remove-Item -LiteralPath $temporary -Force }
Write-Output "Imported original artwork: $Key"
