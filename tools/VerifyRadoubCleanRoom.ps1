# SPDX-License-Identifier: MIT

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("Formats", "Render", "Integration")]
    [string]$Role
)

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$failures = [System.Collections.Generic.List[string]]::new()
$checks = 1

if (Test-Path -LiteralPath (Join-Path $repositoryRoot "External\Radoub"))
{
    $failures.Add("The retired external source directory is present.")
}

if ($Role -eq "Render")
{
    $deniedRenderFiles = @(
        "SWLOR.Toolset.Domain\Render\MdlPartComposer.cs",
        "SWLOR.Toolset.Domain\Render\MdlPartBoneMap.cs",
        "SWLOR.Toolset.Domain\Render\TextureLoader.cs",
        "SWLOR.Toolset.Domain\Render\MdlMeshBuilder.cs",
        "SWLOR.Toolset.Domain\Render\MdlGeometryFlattener.cs"
    )
    $checks += $deniedRenderFiles.Count

    $presentCount = 0
    foreach ($relativePath in $deniedRenderFiles)
    {
        if (Test-Path -LiteralPath (Join-Path $repositoryRoot $relativePath))
        {
            $presentCount++
        }
    }

    if ($presentCount -ne 0)
    {
        $failures.Add(
            "$presentCount denied historical render file(s) are present; use a sanitized clean-author worktree.")
    }
}

$status = if ($failures.Count -eq 0) { "passed" } else { "failed" }
Write-Output "clean-room role=$Role checks=$checks status=$status failures=$($failures.Count)"
foreach ($failure in $failures)
{
    Write-Error $failure
}

if ($failures.Count -ne 0)
{
    exit 1
}
