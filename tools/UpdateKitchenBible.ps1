[CmdletBinding()]
param(
    [string]$WorkbookPath = "design\bible\SWLOR Design Bible.xlsx",
    [switch]$Check
)

# Writes the Kitchen minigame columns (dish, flavor target, radii, Freshness) onto the Agriculture rows of the
# Design Bible "Cooking Recipes" tab. The values are read from the compiled recipe definitions, so rerun
# `dotnet run --project tools/SWLOR.RecipeParameters -p:RunPostBuildEvent=Never -- write Kitchen` first when the profile changed.
# A second run changes nothing. After a workbook merge conflict, take the base workbook and run this again, then run
# tools/UpdateCombatUpgradeAudit.ps1 -RefreshLocalBible.

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$workbookFullPath = if ([IO.Path]::IsPathRooted($WorkbookPath)) { $WorkbookPath } else { Join-Path $repoRoot $WorkbookPath }
$dataPath = Join-Path ([IO.Path]::GetTempPath()) ("swlor-kitchen-parameters-{0}.json" -f [guid]::NewGuid().ToString("N"))

try {
    & dotnet run --project (Join-Path $repoRoot "tools\SWLOR.RecipeParameters") -p:RunPostBuildEvent=Never -- export $dataPath Kitchen
    if ($LASTEXITCODE -ne 0) { throw "Exporting the Kitchen parameters failed." }

    $arguments = @((Join-Path $repoRoot "tools\UpdateRecipeParameterBible.py"), "--data", $dataPath, "--workbook", $workbookFullPath)
    if ($Check) { $arguments += "--check" }
    & python -I @arguments
    if ($LASTEXITCODE -ne 0) { throw "Updating the Cooking Recipes tab failed." }
}
finally {
    if (Test-Path $dataPath) { Remove-Item $dataPath -Force }
}
