[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Version,
    [string]$AssetDescriptor,
    [Parameter(Mandatory)][string]$OutputDirectory,
    [string]$SignParameters,
    [switch]$SkipWorkspace
)
$ErrorActionPreference = 'Stop'
$arguments = @((Join-Path $PSScriptRoot 'ToolsetDistribution.py'), 'publish', '--version', $Version, '--output', $OutputDirectory)
if ($AssetDescriptor) { $arguments += @('--asset-descriptor', $AssetDescriptor) }
if ($SignParameters) { $arguments += @('--sign-parameters', $SignParameters) }
if ($SkipWorkspace) { $arguments += '--skip-workspace' }
& python @arguments
exit $LASTEXITCODE
