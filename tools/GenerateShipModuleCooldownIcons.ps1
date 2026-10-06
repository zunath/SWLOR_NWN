param([switch]$Force)
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
# Module feat anchors display inventory artwork dynamically. Use the regular cooldown generator for that artwork.
$definitions = Get-ChildItem -LiteralPath (Join-Path $repoRoot "SWLOR.Game.Server/Feature/ShipModuleDefinition") -Filter "*.cs"
$icons = foreach ($definition in $definitions) {
    foreach ($match in [regex]::Matches((Get-Content -LiteralPath $definition.FullName -Raw), 'Texture(?:\s*=\s*|\(")("?)(iit_[^"\s]+)')) {
        $match.Groups[2].Value
    }
}
$icons = @($icons | Sort-Object -Unique)
if ($icons.Count -eq 0) { throw "No ship module artwork found in module definitions." }
& (Join-Path $PSScriptRoot "GenerateCooldownIcons.ps1") -IconPath (Join-Path $repoRoot "SWLOR_Haks/sw_ability") -SourceIconPath (Join-Path $repoRoot "SWLOR_Haks/sw_item") -Feat2daPath (Join-Path $repoRoot "SWLOR_Haks/sw_2da/feat.2da") -IconResRefs $icons -Force:$Force
