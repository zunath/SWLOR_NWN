<#
.SYNOPSIS
Runs the live smoke scenario on this checkout: builds the Release server, boots it in a private
Docker stack, logs a client in, walks the core player loop and tears everything down.

.DESCRIPTION
One repeatable command for the end of each maintainability-refactor phase. It wraps
tools/SmokeTest.py, which drives the same Docker stack as tools/NwnLiveClient.py, so every
isolation rule of InEngineVisualVerification.md applies: a unique Compose project, no published
ports, a copied cdkey.ini that is deleted at teardown, nothing started outside the stack.

Results (results.json, results.md and the screenshots) go to a folder outside the repository,
by default under the temp folder's swlor-smoke. The exit code is 0 when no step failed, the
teardown was clean and, with -Repeat, every run agreed; 1 on a failure or a difference; 2 when
the run could not be prepared.

-ServerOnly boots and checks the server without a client. It reads no CD key, so the client
steps are recorded as blocked; use it to check a build when no key is available.

The public CD key (8 characters from a server log "Joined as Player" line) comes from -PublicCdKey
or the SWLOR_LIVE_PUBLIC_CDKEY environment variable. Never commit it.

.EXAMPLE
powershell -NoProfile -File tools/RunSmokeTest.ps1 -Repeat 2

.EXAMPLE
powershell -NoProfile -File tools/RunSmokeTest.ps1 -ServerOnly -SkipBuild
#>
[CmdletBinding()]
param(
    [string]$Name,
    [string]$ResultsRoot,
    # Run the scenario this many times in a row and compare the runs.
    [int]$Repeat = 1,
    [switch]$ServerOnly,
    # Use the existing Release build instead of building first.
    [switch]$SkipBuild,
    [string]$Character,
    [string]$PublicCdKey,
    [string]$CdKey,
    [string]$Module,
    [string]$HakDir,
    [string]$TlkDir,
    [string]$ClientInstall
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repository = Split-Path -Parent $PSScriptRoot

if (-not $SkipBuild) {
    # The newest SDK's persistent MSBuild server can get stuck; a one-off build does not need it.
    $previousServer = $env:MSBUILDUSESERVER
    $env:MSBUILDUSESERVER = "0"
    try {
        & dotnet build (Join-Path $repository "SWLOR.Game.Server\SWLOR.Game.Server.csproj") -c Release `
            -p:RunPostBuildEvent=Never -v q -clp:ErrorsOnly
        if ($LASTEXITCODE -ne 0) {
            Write-Error "The Release build failed (exit $LASTEXITCODE); the smoke run did not start."
            exit 2
        }
    }
    finally {
        $env:MSBUILDUSESERVER = $previousServer
    }
}

$arguments = @("-B", (Join-Path $PSScriptRoot "SmokeTest.py"), "run", "--repeat", $Repeat)
if ($ServerOnly) { $arguments += "--server-only" }
foreach ($pair in @(
        @("--name", $Name), @("--results-root", $ResultsRoot), @("--character", $Character),
        @("--cdkey", $CdKey), @("--module", $Module), @("--hak-dir", $HakDir),
        @("--tlk-dir", $TlkDir), @("--client-install", $ClientInstall))) {
    if ($pair[1]) { $arguments += $pair }
}

# The key reaches the child process through its environment, never its command line.
$previousKey = $env:SWLOR_LIVE_PUBLIC_CDKEY
if ($PublicCdKey) { $env:SWLOR_LIVE_PUBLIC_CDKEY = $PublicCdKey }
try {
    & python @arguments
    exit $LASTEXITCODE
}
finally {
    $env:SWLOR_LIVE_PUBLIC_CDKEY = $previousKey
}
