# Mirrors canonical agent skills from .codex/skills into .claude/skills so both
# Codex and Claude Code discover the same skills. Codex-only files under each
# skill's agents/ directory (openai.yaml) are excluded from the mirror.
#
# Usage:
#   powershell -ExecutionPolicy Bypass -File tools/SyncAgentSkills.ps1             # sync mirror
#   powershell -ExecutionPolicy Bypass -File tools/SyncAgentSkills.ps1 -CheckOnly  # report drift, exit 1 if out of sync
[CmdletBinding()]
param(
    [switch]$CheckOnly
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$source = Join-Path $repoRoot '.codex\skills'
$mirror = Join-Path $repoRoot '.claude\skills'

if (-not (Test-Path $source)) {
    throw "Canonical skills directory not found: $source"
}

function Get-SkillFiles([string]$root) {
    if (-not (Test-Path $root)) { return @() }
    Get-ChildItem $root -Recurse -File |
        ForEach-Object { $_.FullName.Substring($root.Length + 1) } |
        Where-Object { $_ -notmatch '^[^\\]+\\agents\\' }
}

$sourceFiles = @(Get-SkillFiles $source)
$mirrorFiles = @(Get-SkillFiles $mirror)

$missing = @()
$differs = @()
foreach ($rel in $sourceFiles) {
    $dst = Join-Path $mirror $rel
    if (-not (Test-Path $dst)) {
        $missing += $rel
    }
    elseif ((Get-FileHash (Join-Path $source $rel)).Hash -ne (Get-FileHash $dst).Hash) {
        $differs += $rel
    }
}
$orphaned = @($mirrorFiles | Where-Object { $sourceFiles -notcontains $_ })

if ($CheckOnly) {
    $missing | ForEach-Object { Write-Host "missing from mirror: .claude\skills\$_" }
    $differs | ForEach-Object { Write-Host "differs from canonical: .claude\skills\$_" }
    $orphaned | ForEach-Object { Write-Host "orphaned in mirror: .claude\skills\$_" }
    if ($missing.Count + $differs.Count + $orphaned.Count -gt 0) {
        Write-Host 'Skill mirror is out of sync. Run tools/SyncAgentSkills.ps1 to fix.'
        exit 1
    }
    Write-Host 'Skill mirror is in sync with .codex/skills.'
    exit 0
}

foreach ($rel in $missing + $differs) {
    $dst = Join-Path $mirror $rel
    $dstDir = Split-Path -Parent $dst
    if (-not (Test-Path $dstDir)) { New-Item -ItemType Directory -Force $dstDir | Out-Null }
    Copy-Item (Join-Path $source $rel) $dst -Force
    Write-Host "synced: .claude\skills\$rel"
}
foreach ($rel in $orphaned) {
    Remove-Item (Join-Path $mirror $rel) -Force -Confirm:$false
    Write-Host "removed orphan: .claude\skills\$rel"
}

# Drop skill directories that no longer exist in the canonical source, and any
# directories emptied by orphan removal.
if (Test-Path $mirror) {
    Get-ChildItem $mirror -Recurse -Directory |
        Sort-Object { $_.FullName.Length } -Descending |
        Where-Object { -not (Get-ChildItem $_.FullName -Recurse -File) } |
        ForEach-Object {
            Remove-Item $_.FullName -Recurse -Force -Confirm:$false
            Write-Host "removed empty directory: $($_.FullName.Substring($repoRoot.Length + 1))"
        }
}

Write-Host "Skill mirror refreshed ($($sourceFiles.Count) files)."
