<#
.SYNOPSIS
    Keeps the callers of removed facades in the branch's shape: static code reaches a removed facade's service through
    SWLOR.Game.Server.Composition.GameServices (Readmes/MaintainabilityRefactorFacadeRemoval.md, P2-Z).

.DESCRIPTION
    Wraps tools/Refactor/SWLOR.FacadeRemoval (a C# program, because isolated agents cannot run powershell.exe). The data
    is tools/Refactor/RemovedFacades.json (the facades the branch deleted and the owners of their constants) and
    tools/Refactor/HandlerRenames.txt. A run:
      * adds `using static SWLOR.Game.Server.Composition.GameServices;` to each file that calls a removed facade (or the
        global form to a project whose GlobalUsings.cs imports SWLOR.Game.Server.Service globally), drops the qualifier
        of a qualified call, writes an explicit name as GameServices.<Name>, and reads a moved constant from its owner;
      * renames, in place, each line of SWLOR.Game.Server/Core/ScriptHandlerOrder.txt that names a removed facade's
        handler, to the branch name HandlerRenames.txt gives it (a line with no rename stops the run).
    A tree without SWLOR.Game.Server/Composition/GameServices.cs (master, the parity copy) is skipped entirely, and so
    is a facade whose source file still exists, so a run there changes nothing. The tool is idempotent: a second run
    changes nothing.

    The daily sync re-runs this script after merging master, so new master code that calls a removed facade compiles.
    The script owns no whole file: a conflict on a using line is settled by hand and the script runs again.

    Runs on Windows PowerShell 5.1 and PowerShell 7.

.PARAMETER Check
    Write nothing. Exit 1 when a run would change something.

.PARAMETER ListOwnedFiles
    Write nothing and build nothing; print the files this script owns (none: it only adds lines to files that people
    edit), then exit 0 (tools/Refactor/MechanicalScripts.txt contract).

.PARAMETER Reverse
    Undo the caller rewrites in the folder -Path names, so code written for the branch compiles against master. The
    parity copy (MasterCopy.ps1) runs it on its copy of the characterization tests. Never run it on the branch.

.PARAMETER Path
    The folder -Reverse rewrites.

.PARAMETER RepositoryRoot
    Repository root. Defaults to two folders above this script. The data files are always this script's own.

.EXAMPLE
    pwsh -NoProfile -File tools/Refactor/Apply-FacadeRemoval.ps1
    pwsh -NoProfile -File tools/Refactor/Apply-FacadeRemoval.ps1 -Check
    pwsh -NoProfile -File tools/Refactor/Apply-FacadeRemoval.ps1 -Reverse -Path C:\Temp\parity\master\SWLOR.Game.Server.CharacterizationTests
#>
[CmdletBinding()]
param(
    [switch]$Check,
    [switch]$ListOwnedFiles,
    [switch]$Reverse,
    [string]$Path,
    [string]$RepositoryRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ($ListOwnedFiles) {
    exit 0
}

if (-not $RepositoryRoot) {
    $RepositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
}
$RepositoryRoot = [System.IO.Path]::GetFullPath($RepositoryRoot)

if ($Reverse -and -not $Path) {
    Write-Host '-Reverse needs -Path <folder>.'
    exit 2
}

$project = Join-Path $PSScriptRoot 'SWLOR.FacadeRemoval'

& dotnet build $project '-p:RunPostBuildEvent=Never' -v q '-clp:ErrorsOnly'
if ($LASTEXITCODE -ne 0) {
    Write-Host 'The facade removal tool did not build.'
    exit 2
}

$arguments = @('--data', (Join-Path $PSScriptRoot 'RemovedFacades.json'))
if ($Reverse) {
    $arguments += @('--reverse', '--path', [System.IO.Path]::GetFullPath($Path))
}
else {
    $arguments += @('--repo', $RepositoryRoot)
    if ($Check) {
        $arguments += '--check'
    }
}

& dotnet run --project $project --no-build -- @arguments
exit $LASTEXITCODE
