[CmdletBinding()]
param([Parameter(Mandatory)][string]$Archive, [Parameter(Mandatory)][string]$ToolsetPath, [switch]$Apply)
$ErrorActionPreference = 'Stop'
$arguments = @((Join-Path $PSScriptRoot 'ToolsetDistribution.py'), 'import-contribution', '--archive', $Archive, '--toolset', $ToolsetPath)
if ($Apply) { $arguments += '--apply' }
& python @arguments
exit $LASTEXITCODE
