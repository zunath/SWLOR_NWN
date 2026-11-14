<#
.SYNOPSIS
    Moves the vocabulary and definition types of the domain order to the lowest domain that uses them and keeps every
    reference in that shape (maintainability refactor, items P5-1c and P5-1e).

.DESCRIPTION
    Wraps tools/Refactor/SWLOR.TypeMoves (a C# program: it needs the semantic model, and isolated agents cannot run
    powershell.exe), like Move-ServiceTypes.ps1, with the data file tools/Refactor/DomainOrderTypeMoves.json: each type with
    the namespace it leaves and the namespace it joins (Readmes/MaintainabilityRefactorDomainOrder.md, Appendix B). A run
    moves each listed file whose old path exists, rewrites qualified names (in code, nameof, cref, using static and alias
    directives), adds a using of the new namespace at its sorted place to each file that names a moved type by its simple
    name, keeps the file's using of the old namespace (the data asks for the smallest edit), proves every name binds as
    before and qualifies the ones that would not, and rewrites the names and paths in other text. A type that moved already
    gets a stand-in in its old namespace for the analysis, so master's code merged later is moved the same way. The tool is
    idempotent: a second run changes nothing.

    The daily sync re-runs this script after merging master, after Move-ServiceTypes.ps1: master names these types through
    Service.<Name>Service, which Move-SupportNamespaces.ps1 first turns into Domain.<Domain>. The parity check runs it on its
    copy of master. The script owns no whole file: a conflict on a using line is settled by taking master's block and running
    the script again.

    Runs on Windows PowerShell 5.1 and PowerShell 7.

.PARAMETER Check
    Write nothing. List the pending moves and edits and exit 1 when there is anything.

.PARAMETER ListOwnedFiles
    Write nothing and build nothing; print the files this script owns (none: it only edits lines of files that people
    edit), then exit 0 (tools/Refactor/MechanicalScripts.txt contract).

.PARAMETER RepositoryRoot
    Repository root. Defaults to two folders above this script. The data file is always this script's own.

.EXAMPLE
    pwsh -NoProfile -File tools/Refactor/Move-DomainOrderTypes.ps1
    pwsh -NoProfile -File tools/Refactor/Move-DomainOrderTypes.ps1 -Check
#>
[CmdletBinding()]
param(
    [switch]$Check,
    [switch]$ListOwnedFiles,
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

$project = Join-Path $PSScriptRoot 'SWLOR.TypeMoves'

& dotnet build $project '-p:RunPostBuildEvent=Never' -v q '-clp:ErrorsOnly'
if ($LASTEXITCODE -ne 0) {
    Write-Host 'The type move tool did not build.'
    exit 2
}

$arguments = @('--data', (Join-Path $PSScriptRoot 'DomainOrderTypeMoves.json'), '--repo', $RepositoryRoot)
if ($Check) {
    $arguments += '--check'
}

& dotnet run --project $project --no-build -- @arguments
exit $LASTEXITCODE
