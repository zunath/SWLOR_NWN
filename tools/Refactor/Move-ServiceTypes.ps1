<#
.SYNOPSIS
    Moves the types left directly in the namespace SWLOR.Game.Server.Service into their domains and keeps every reference
    in that shape (maintainability refactor, item P2-Z3).

.DESCRIPTION
    Wraps tools/Refactor/SWLOR.TypeMoves (a C# program: it needs the semantic model, and isolated agents cannot run
    powershell.exe). The data is tools/Refactor/TypeMoves.json: each type, the domain namespace it joins, and the paths
    the text rewrite leaves alone. A run:
      * moves each listed file whose old path exists to the folder of its new namespace and changes its namespace line;
      * compiles the server and every project that references it in memory and, with the semantic model, gives a
        qualified name (Service.Log, SWLOR.Game.Server.Service.Log, using static, alias) the new namespace, adds a using
        of the new namespace to a file that names a moved type by its simple name, and drops a using of
        SWLOR.Game.Server.Service where nothing binds through it any more;
      * compiles the edited files again and proves every name binds to what it bound to before; a name that would bind
        to something else is written qualified (Random meaning System.Random in Domain.Foundation becomes System.Random);
      * rewrites the names and paths in other text (guides, scripts, path lists, C# comments and literals), renames the
        moved handlers in SWLOR.Game.Server/Core/ScriptHandlerOrder.txt in place and records each in
        tools/Refactor/HandlerRenames.txt.
    A type that moved already gets a stand-in in the old namespace for the analysis, so master's code merged later is moved
    the same way. The tool is idempotent: a second run changes nothing.

    The daily sync re-runs this script after merging master (after Move-SupportNamespaces.ps1, which hands it master's
    moved support files still reaching these types through Service); the parity check runs it on its copy of master. The
    script owns no whole file: a conflict on a using line is settled by hand and the script runs again.

    Runs on Windows PowerShell 5.1 and PowerShell 7.

.PARAMETER Check
    Write nothing. List what a run would change and exit 1 when that is anything.

.PARAMETER ListOwnedFiles
    Write nothing and build nothing; print the files this script owns (none: it only edits lines of files that people
    edit), then exit 0 (tools/Refactor/MechanicalScripts.txt contract).

.PARAMETER RepositoryRoot
    Repository root. Defaults to two folders above this script. The data file is always this script's own.

.EXAMPLE
    pwsh -NoProfile -File tools/Refactor/Move-ServiceTypes.ps1
    pwsh -NoProfile -File tools/Refactor/Move-ServiceTypes.ps1 -Check
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

$arguments = @('--data', (Join-Path $PSScriptRoot 'TypeMoves.json'), '--repo', $RepositoryRoot)
if ($Check) {
    $arguments += '--check'
}

& dotnet run --project $project --no-build -- @arguments
exit $LASTEXITCODE
