param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

throw "Do not patch rank-badge areas directly on production ability TGAs. Restore or import source artwork with tools\RestoreAbilityIconArtwork.ps1 or tools\ImportCodexIconContactSheet.ps1, then run tools\GenerateCooldownIcons.ps1 -Force."
