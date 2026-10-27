<#
.SYNOPSIS
    Generates the engine seams, for NWScript and for each NWNX plugin: the interface (INWScript, IChatPlugin, ...), the
    pass-through implementation, the runtime, the recording fake, and the forwarding bodies of the static wrappers in
    SWLOR.NWN.API (NWScript, NWScriptCustom and NWNX/*Plugin.cs).

.DESCRIPTION
    Wraps tools/Refactor/SWLOR.EngineSeamGenerator. The generator is deterministic and re-runnable: a second run changes
    nothing. It refuses to write unless every moved body is textually identical to the original, every signature matches
    the wrapper's, and a second run is a fixed point.

    The daily sync re-runs this script after merging master. If master changed a file in SWLOR.NWN.API/NWScript,
    NWScriptCustom or NWNX/*Plugin.cs, take master's version of that file (it holds original bodies) and run this script;
    it moves the new bodies and regenerates the interfaces and the fakes of every seam.

    Runs on Windows PowerShell 5.1 and PowerShell 7.

.PARAMETER Check
    Write nothing. Exit 1 when the checked-in output differs from what the generator produces.

.PARAMETER VerifyOrigin
    Write nothing. Build the output from the wrapper files as committed at this git revision (the pristine hand-written
    code) and exit 1 when the working tree differs from it. Use a revision from before the first seam was merged, so every
    wrapper still holds its original body.

.PARAMETER ListOwnedFiles
    Write nothing and build nothing; print only the repository-relative folders (forward slashes, each ending in /**)
    whose files this script owns: the wrapper sources it rewrites and everything it generates. The daily sync uses it to
    decide which merge conflicts it settles by taking master's version and running this script again
    (tools/Refactor/MechanicalScripts.txt). Keep the list equal to SeamDefinition.NWScript in the generator.

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
    [switch]$ListOwnedFiles,
    [string]$VerifyOrigin,
    [string]$RepositoryRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not $RepositoryRoot) {
    $RepositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
}
$RepositoryRoot = [System.IO.Path]::GetFullPath($RepositoryRoot)

if ($ListOwnedFiles) {
    Write-Output 'SWLOR.NWN.API/NWScript/**'
    Write-Output 'SWLOR.NWN.API/NWScriptCustom/**'
    Write-Output 'SWLOR.NWN.API/Generated/**'
    Write-Output 'SWLOR.NWN.API.Testing/Generated/**'
    exit 0
}

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
