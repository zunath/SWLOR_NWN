[CmdletBinding()]
param(
    [ValidateSet('Check', 'Setup', 'Toolset', 'Server')][string]$Mode = 'Check',
    [ValidateSet('Toolset', 'Server')][string]$Profile = 'Toolset'
)
$ErrorActionPreference = 'Stop'
if (!(Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host 'Install a stable .NET 10 SDK from https://dotnet.microsoft.com/download/dotnet/10.0, then reopen this window.'
    exit 1
}
Push-Location (Split-Path -Parent $PSScriptRoot)
try {
    try {
        $sdk = & dotnet --version 2>$null
        $sdkExitCode = $LASTEXITCODE
    } catch {
        $sdk = ''
        $sdkExitCode = 1
    }
    if ($sdkExitCode -ne 0 -or "$sdk" -notmatch '^10\.\d+\.\d+$') {
        Write-Host 'No stable .NET 10 SDK found. Install one (10.0.100 or newer; release candidates do not work) from https://dotnet.microsoft.com/download/dotnet/10.0, then reopen this window.'
        exit 1
    }
    & dotnet run --project (Join-Path $PSScriptRoot 'SWLOR.ContributorSetup/SWLOR.ContributorSetup.csproj') -p:RunPostBuildEvent=Never -- --mode $Mode --profile $Profile
    exit $LASTEXITCODE
} finally {
    Pop-Location
}
