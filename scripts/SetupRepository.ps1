[CmdletBinding()]
param(
    [ValidateSet('Check', 'Setup', 'Toolset', 'Server')][string]$Mode = 'Check',
    [ValidateSet('Toolset', 'Server')][string]$Profile = 'Toolset'
)
$ErrorActionPreference = 'Stop'
if (!(Get-Command python -ErrorAction SilentlyContinue)) {
    Write-Host 'Install Python 3.11 or newer and add it to PATH, then reopen this window.'
    exit 1
}
& python (Join-Path $PSScriptRoot 'SetupRepository.py') --mode $Mode --profile $Profile
exit $LASTEXITCODE
