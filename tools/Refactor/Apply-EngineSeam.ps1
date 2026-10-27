<#
.SYNOPSIS
    Generates the NWScript engine seam: INWScript, the pass-through implementation, the runtime, the recording fake, and
    the forwarding bodies of the static wrappers in SWLOR.NWN.API.

.DESCRIPTION
    Wraps tools/Refactor/SWLOR.EngineSeamGenerator. The generator is deterministic and re-runnable: a second run changes
    nothing. It refuses to write unless every moved body is textually identical to the original, every signature matches
    the wrapper's, and a second run is a fixed point.

    The daily sync re-runs this script after merging master. If master changed a file in SWLOR.NWN.API/NWScript or
    NWScriptCustom, take master's version of that file (it holds original bodies) and run this script; it moves the new
    bodies and regenerates the interface and the fake.

    Runs on Windows PowerShell 5.1 and PowerShell 7.

.PARAMETER Check
    Write nothing. Exit 1 when the checked-in output differs from what the generator produces.

.PARAMETER VerifyOrigin
    Write nothing. Build the output from the wrapper files as committed at this git revision (the pristine hand-written
    code) and exit 1 when the working tree differs from it.

.PARAMETER RepositoryRoot
    Repository root. Defaults to two folders above this script.

.EXAMPLE
    pwsh -NoProfile -File tools/Refactor/Apply-EngineSeam.ps1
    pwsh -NoProfile -File tools/Refactor/Apply-EngineSeam.ps1 -Check
    pwsh -NoProfile -File tools/Refactor/Apply-EngineSeam.ps1 -VerifyOrigin d156ec076
#>
[CmdletBinding()]
param(
    [switch]$Check,
    [string]$VerifyOrigin,
    [string]$RepositoryRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not $RepositoryRoot) {
    $RepositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
}
$RepositoryRoot = [System.IO.Path]::GetFullPath($RepositoryRoot)
$project = Join-Path $PSScriptRoot 'SWLOR.EngineSeamGenerator'

& dotnet build $project '-p:RunPostBuildEvent=Never' -v q '-clp:ErrorsOnly'
if ($LASTEXITCODE -ne 0) {
    Write-Host 'The generator did not build.'
    exit 2
}

$arguments = @('--repo', $RepositoryRoot)
if ($VerifyOrigin) {
    $arguments += @('--verify-origin', $VerifyOrigin)
}
elseif ($Check) {
    $arguments += '--check'
}

& dotnet run --project $project --no-build -- @arguments
exit $LASTEXITCODE
