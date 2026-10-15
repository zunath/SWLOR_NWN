[CmdletBinding()]
param(
    [ValidateSet('Check', 'Setup', 'Toolset', 'Server')][string]$Mode = 'Check',
    [ValidateSet('Toolset', 'Server')][string]$Profile = 'Toolset'
)
$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Set-Location -LiteralPath $repoRoot
if ($Mode -eq 'Server') { $Profile = 'Server' }
$failures = New-Object 'System.Collections.Generic.List[string]'
function Check-Command([string]$Name, [string]$Help) {
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        $failures.Add($Help)
        return $false
    }
    Write-Host "OK: $Name"
    return $true
}
$hasGit = Check-Command 'git' 'Install Git from https://git-scm.com/downloads/win and reopen this window.'
$hasDotnet = Check-Command 'dotnet' 'Install the .NET 10 SDK from https://dotnet.microsoft.com/download/dotnet/10.0 and reopen this window.'
if ($hasDotnet) {
    $sdk = $null
    try { $sdk = & dotnet --version 2>&1 } catch { $sdk = $null }
    if (!$sdk -or $LASTEXITCODE -ne 0 -or "$sdk" -notmatch '^10\.\d+\.\d+$') {
        $failures.Add('Install a stable .NET 10 SDK. global.json selects the newest installed .NET 10 feature band; preview SDKs are not used.')
    } else { Write-Host "OK: .NET SDK $sdk" }
}
foreach ($tool in @('nwn_erf.exe', 'nwn_gff.exe', 'nwn_script_comp.exe', 'pcre64.dll', 'libnwnscriptcomp.dll')) {
    if (-not (Test-Path -LiteralPath (Join-Path $repoRoot "tools/SWLOR.CLI/$tool"))) {
        $failures.Add("Missing bundled tool $tool. Restore tools/SWLOR.CLI from Git.")
    }
}
if ($Profile -eq 'Server' -and (Check-Command 'docker' 'Install and start Docker Desktop with Linux containers: https://www.docker.com/products/docker-desktop/')) {
    try {
        $dockerOS = & docker info --format '{{.OSType}}' 2>$null
        if ($LASTEXITCODE -ne 0) { $failures.Add('Start Docker Desktop, wait until its engine is ready, and rerun setup.') }
        elseif ($dockerOS -ne 'linux') { $failures.Add('Switch Docker Desktop to Linux containers.') }
        & docker compose version 2>$null | Out-Null
        if ($LASTEXITCODE -ne 0) { $failures.Add('Update Docker Desktop to a version with Docker Compose.') }
    } catch { $failures.Add('Start Docker Desktop with Linux containers, wait until its engine is ready, and rerun setup.') }
}
if ($failures.Count -gt 0) {
    foreach ($failure in $failures) { Write-Host "NEEDS ATTENTION: $failure" -ForegroundColor Yellow }
    exit 1
}
if ($Mode -ne 'Check') {
    & git submodule update --init --recursive
    if ($LASTEXITCODE -ne 0) { throw 'Asset checkout failed. Check your network and Git credentials; existing work was kept.' }
}
if (-not (Test-Path -LiteralPath (Join-Path $repoRoot 'SWLOR_Haks/sw_tlk/sw_tlk.tlk.json'))) {
    Write-Host 'NEEDS ATTENTION: SWLOR assets are missing. Run Setup.cmd to initialize the pinned submodule.' -ForegroundColor Yellow
    exit 1
}
if ($Mode -eq 'Check') { Write-Host 'Setup checks passed. No files or containers were changed.'; exit 0 }
$project = if ($Profile -eq 'Server') { 'SWLOR.Runner/SWLOR.Runner.csproj' } else { 'SWLOR.Toolset/SWLOR.Toolset.csproj' }
& dotnet build $project -p:RunPostBuildEvent=Never
if ($LASTEXITCODE -ne 0) { throw 'Build failed. The first error above identifies the dependency or source file to fix.' }
if ($Mode -eq 'Setup') { Write-Host 'Setup complete. Use StartToolset.cmd or StartServer.cmd.'; exit 0 }
if ($Mode -eq 'Toolset') {
    & dotnet run --no-build --project $project
    exit $LASTEXITCODE
}
# The existing pack/deploy pipeline prepares only this checkout's debugserver directory.
& dotnet build 'SWLOR.CLI/SWLOR.CLI.csproj' -c Release -p:RunPostBuildEvent=Never
if ($LASTEXITCODE -ne 0) { throw 'Module packer build failed.' }
$cli = Join-Path $repoRoot 'SWLOR.CLI/bin/Release/net10.0/SWLOR.CLI.dll'
$previousPath = $env:PATH
try {
    $env:PATH = (Join-Path $repoRoot 'tools/SWLOR.CLI') + ';' + $previousPath
    Push-Location (Join-Path $repoRoot 'Module')
    try { & dotnet $cli -p './Star Wars LOR v2.mod' --no-prompt; if ($LASTEXITCODE -ne 0) { throw 'Module packing failed.' } }
    finally { Pop-Location }
    Push-Location (Join-Path $repoRoot 'Build')
    try {
        & dotnet $cli -o --server-output (Join-Path $repoRoot 'SWLOR.Game.Server/bin/Debug/net10.0')
        if ($LASTEXITCODE -ne 0) { throw 'Preparing the local server failed.' }
    } finally { Pop-Location }
} finally { $env:PATH = $previousPath }
Write-Host 'Starting the local server. Close this window or press Ctrl+C to stop it.'
& dotnet run --no-build --project $project
exit $LASTEXITCODE
