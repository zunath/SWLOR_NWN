<#
.SYNOPSIS
    Keeps content in the branch's shape: content gets its services through its constructor instead of the static GameServices
    lookup (Readmes/MaintainabilityRefactorContentInjection.md, P5-13).

.DESCRIPTION
    Wraps tools/Refactor/SWLOR.ContentInjection (a C# program, because isolated agents cannot run powershell.exe). The data is
    tools/Refactor/ContentInjection.json (the converted content files and folders), SWLOR.Architecture.Tests/ConvertedServices.json
    (a service name to its interface) and SWLOR.Game.Server/Composition/ContentRegistrations.cs (the content singletons). A run:
      * converts each listed file: a static class that reaches a service becomes a content singleton (static comes off the class and
        off every member that reaches one, each service becomes a private get-only property named like the GameServices property,
        set by the constructor), any other class is made content that takes its services first in each constructor;
      * rewrites `new X(values)` of made content with services to `Content.Create<X, A...>(values)` in every project that references
        the server, and gives the creator Content (injected, the temporary GameServices.Content, or a fixture property in a unit test);
      * regenerates SWLOR.Game.Server/Composition/ContentRegistrations.cs (the singletons and the Content.Create signatures);
      * refuses, and lists, what needs a person: singletons that need each other, state, static members of made content that reach a
        service, more than 10 services, named arguments and initializers at creation sites, callers that cannot take a singleton.
        A file with a refusal is left as it was.
    While ContentInjection.json lists nothing, a run only keeps ContentRegistrations.cs. A tree without
    SWLOR.Game.Server/Core/IContentFactory.cs (master, the parity copy) is skipped entirely. The tool is idempotent: a second run
    changes nothing. The daily sync re-runs it after Apply-FacadeRemoval.ps1, so master's new content is converted at every sync.

    Runs on Windows PowerShell 5.1 and PowerShell 7.

.PARAMETER Check
    Write nothing. Exit 1 when a run would change something.

.PARAMETER ListOwnedFiles
    Write nothing and build nothing; print the files this script owns (ContentRegistrations.cs, which it writes whole), then exit 0
    (tools/Refactor/MechanicalScripts.txt contract).

.PARAMETER Reverse
    Turn Content.Create<X, A...>(args) back into new X(args) and remove the fixture properties the forward run adds, in the folder
    -Path names, so code written for the branch compiles against master. The parity copy (MasterCopy.ps1) runs it on its copy of the
    characterization tests, before Apply-FacadeRemoval.ps1 -Reverse. It needs nothing of the tree, so it runs on master's. Never
    run it on the branch.

.PARAMETER Path
    The folder -Reverse rewrites.

.PARAMETER RepositoryRoot
    Repository root. Defaults to two folders above this script. The data file is always this script's own.

.EXAMPLE
    pwsh -NoProfile -File tools/Refactor/Apply-ContentInjection.ps1
    pwsh -NoProfile -File tools/Refactor/Apply-ContentInjection.ps1 -Check
    pwsh -NoProfile -File tools/Refactor/Apply-ContentInjection.ps1 -Reverse -Path C:\Temp\parity\master\SWLOR.Game.Server.CharacterizationTests
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
    Write-Output 'SWLOR.Game.Server/Composition/ContentRegistrations.cs'
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

$project = Join-Path $PSScriptRoot 'SWLOR.ContentInjection'

& dotnet build $project '-p:RunPostBuildEvent=Never' -v q '-clp:ErrorsOnly'
if ($LASTEXITCODE -ne 0) {
    Write-Host 'The content injection tool did not build.'
    exit 2
}

$arguments = @('--data', (Join-Path $PSScriptRoot 'ContentInjection.json'))
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
