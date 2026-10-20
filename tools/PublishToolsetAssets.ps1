[CmdletBinding()]
param([Parameter(Mandatory)][string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
& python (Join-Path $PSScriptRoot 'ToolsetDistribution.py') assets --output $OutputDirectory
exit $LASTEXITCODE
