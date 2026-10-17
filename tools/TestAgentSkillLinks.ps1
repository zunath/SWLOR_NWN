# Exercises directory-link setup in an isolated fixture, including Windows PowerShell 5.1.
[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$isWindowsHost = [Environment]::OSVersion.Platform -eq [PlatformID]::Win32NT
$shellPath = (Get-Process -Id $PID).Path
$sourceScript = Join-Path $PSScriptRoot 'SyncAgentSkills.ps1'
$tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$testRoot = Join-Path $tempRoot ('swlor-skill-links-' + [guid]::NewGuid())
$fixture = Join-Path $testRoot 'checkout with spaces'
$moved = Join-Path $testRoot 'moved checkout'
$agentNames = @('.agents', '.claude')

function Assert([bool]$condition, [string]$message) {
    if (-not $condition) { throw $message }
}
function Invoke-Setup([string]$root, [bool]$check, [bool]$expectSuccess) {
    $arguments = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', (Join-Path $root 'tools/SyncAgentSkills.ps1'))
    if ($check) { $arguments += '-CheckOnly' }
    $savedPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $output = & $shellPath @arguments 2>&1
        $code = $LASTEXITCODE
    }
    finally { $ErrorActionPreference = $savedPreference }
    if (($code -eq 0) -ne $expectSuccess) {
        throw "Unexpected setup exit code ${code}: $($output -join [Environment]::NewLine)"
    }
}
function Remove-Link([string]$path) {
    $entry = Get-Item -LiteralPath $path -Force -ErrorAction SilentlyContinue
    if ($entry) {
        Assert ([bool]($entry.Attributes -band [IO.FileAttributes]::ReparsePoint)) "Expected a link: $path"
        [IO.Directory]::Delete($path)
    }
}

try {
    New-Item -ItemType Directory -Path (Join-Path $fixture 'tools') -Force | Out-Null
    $sample = Join-Path $fixture 'skills/example'
    New-Item -ItemType Directory -Path (Join-Path $sample 'references') -Force | Out-Null
    [IO.File]::WriteAllText((Join-Path $sample 'SKILL.md'), "---`nname: example`ndescription: Test skill.`n---`nExample")
    [IO.File]::WriteAllText((Join-Path $sample 'references/shared.txt'), 'initial')
    Copy-Item -LiteralPath $sourceScript -Destination (Join-Path $fixture 'tools/SyncAgentSkills.ps1')

    Invoke-Setup $fixture $true $false
    Assert (-not (Test-Path -LiteralPath (Join-Path $fixture '.agents'))) 'CheckOnly created a directory.'
    Invoke-Setup $fixture $false $true
    Invoke-Setup $fixture $true $true
    Invoke-Setup $fixture $false $true
    foreach ($agent in $agentNames) {
        $alias = Join-Path $fixture "$agent/skills/example"
        Assert ((Get-Content -LiteralPath (Join-Path $alias 'SKILL.md') -Raw) -eq
            (Get-Content -LiteralPath (Join-Path $sample 'SKILL.md') -Raw)) "Cannot read the skill through $agent."
        $entry = Get-Item -LiteralPath (Join-Path $fixture "$agent/skills") -Force
        Assert ($entry.LinkType -in @('SymbolicLink', 'Junction')) "Expected a directory link for $agent."
    }
    [IO.File]::WriteAllText((Join-Path $fixture '.claude/skills/example/references/shared.txt'), 'edited via Claude')
    Assert ((Get-Content -LiteralPath (Join-Path $sample 'references/shared.txt') -Raw) -eq 'edited via Claude') 'Claude edited a copy.'
    Assert ((Get-Content -LiteralPath (Join-Path $fixture '.agents/skills/example/references/shared.txt') -Raw) -eq 'edited via Claude') 'Codex did not see the shared edit.'

    $claudeLink = Join-Path $fixture '.claude/skills'
    Remove-Link $claudeLink
    New-Item -ItemType Directory -Path $claudeLink | Out-Null
    [IO.File]::WriteAllText((Join-Path $claudeLink 'keep.txt'), 'must survive')
    Invoke-Setup $fixture $false $false
    Assert ((Get-Content -LiteralPath (Join-Path $claudeLink 'keep.txt') -Raw) -eq 'must survive') 'Existing data was overwritten.'
    Remove-Item -LiteralPath (Join-Path $claudeLink 'keep.txt')
    [IO.Directory]::Delete($claudeLink)

    $otherTarget = Join-Path $testRoot 'unrelated directory'
    New-Item -ItemType Directory -Path $otherTarget | Out-Null
    [IO.File]::WriteAllText((Join-Path $otherTarget 'keep.txt'), 'unrelated data')
    $linkType = 'SymbolicLink'
    if ($isWindowsHost) { $linkType = 'Junction' }
    New-Item -ItemType $linkType -Path $claudeLink -Target $otherTarget | Out-Null
    Invoke-Setup $fixture $true $false
    Invoke-Setup $fixture $false $true
    Assert ((Get-Content -LiteralPath (Join-Path $otherTarget 'keep.txt') -Raw) -eq 'unrelated data') 'Repair deleted link-target data.'

    Move-Item -LiteralPath $fixture -Destination $moved
    if ($isWindowsHost) { Invoke-Setup $moved $true $false }
    Invoke-Setup $moved $false $true
    Invoke-Setup $moved $true $true
    Assert ((Get-Content -LiteralPath (Join-Path $moved '.agents/skills/example/references/shared.txt') -Raw) -eq 'edited via Claude') 'Moved checkout lost shared data.'
    Write-Host 'Passed: read-only checks, creation, repeat runs, shared reads/edits, data preservation, wrong-target repair, and checkout relocation.'
}
finally {
    # Remove known junctions without recursion before deleting the isolated fixture.
    foreach ($root in @($fixture, $moved)) {
        foreach ($agent in $agentNames) {
            $path = Join-Path $root "$agent/skills"
            $entry = Get-Item -LiteralPath $path -Force -ErrorAction SilentlyContinue
            if ($entry -and ($entry.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
                [IO.Directory]::Delete($path)
            }
        }
    }
    $resolvedRoot = [IO.Path]::GetFullPath($testRoot)
    Assert ($resolvedRoot.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase) -and
        ([IO.Path]::GetFileName($resolvedRoot) -like 'swlor-skill-links-*')) 'Unsafe fixture cleanup path.'
    if (Test-Path -LiteralPath $resolvedRoot) { Remove-Item -LiteralPath $resolvedRoot -Recurse -Force }
}
