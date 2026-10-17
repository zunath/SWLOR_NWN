# Creates agent discovery links to the one canonical skills/ directory.
# Run after cloning or moving a checkout. No files are copied.
# Windows uses junctions if symbolic-link creation needs unavailable privileges.
# Usage: powershell -NoProfile -ExecutionPolicy Bypass -File tools/SyncAgentSkills.ps1 [-CheckOnly]
[CmdletBinding()]
param([switch]$CheckOnly)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$sharedRoot = Join-Path $repoRoot 'skills'
$isWindowsHost = [Environment]::OSVersion.Platform -eq [PlatformID]::Win32NT
$pathComparison = [StringComparison]::Ordinal
if ($isWindowsHost) { $pathComparison = [StringComparison]::OrdinalIgnoreCase }

function Normalize-Path([string]$path) {
    [IO.Path]::GetFullPath($path).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
}

function Get-Entry([string]$path) {
    Get-Item -LiteralPath $path -Force -ErrorAction SilentlyContinue
}

function Test-DirectoryLink($entry) {
    $entry -and ($entry.Attributes -band [IO.FileAttributes]::ReparsePoint) -and
        $entry.LinkType -in @('SymbolicLink', 'Junction') -and $entry.PSIsContainer
}

function Test-LinkTarget($entry) {
    if (-not (Test-DirectoryLink $entry)) { return $false }
    $targets = @($entry.Target)
    if ($targets.Count -ne 1 -or [string]::IsNullOrWhiteSpace($targets[0])) { return $false }
    $target = [string]$targets[0]
    if (-not [IO.Path]::IsPathRooted($target)) {
        $target = Join-Path (Split-Path -Parent $entry.FullName) $target
    }
    [string]::Equals((Normalize-Path $target), (Normalize-Path $sharedRoot), $pathComparison)
}

$sharedEntry = Get-Entry $sharedRoot
if (-not $sharedEntry -or -not $sharedEntry.PSIsContainer -or
    ($sharedEntry.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
    throw "Expected a real canonical skills directory at $sharedRoot."
}
$skillFolders = @(Get-ChildItem -LiteralPath $sharedRoot -Directory)
if ($skillFolders.Count -eq 0) { throw "No skills found in $sharedRoot." }
foreach ($folder in $skillFolders) {
    if (-not (Test-Path -LiteralPath (Join-Path $folder.FullName 'SKILL.md') -PathType Leaf)) {
        throw "Missing SKILL.md in $($folder.FullName)."
    }
}

$agentFolders = @('.agents', '.claude')
$issues = @()
# Preflight every path before making changes. Never replace a real directory or file.
foreach ($agent in $agentFolders) {
    $parent = Join-Path $repoRoot $agent
    $parentEntry = Get-Entry $parent
    if ($parentEntry -and (-not $parentEntry.PSIsContainer -or
        ($parentEntry.Attributes -band [IO.FileAttributes]::ReparsePoint))) {
        throw "Refusing to write through an unexpected agent directory: $parent"
    }
    $path = Join-Path $parent 'skills'
    $entry = Get-Entry $path
    if ($entry -and -not (Test-DirectoryLink $entry)) {
        throw "Refusing to replace existing data at $path. Move that directory or file aside, then rerun."
    }
    if (-not (Test-LinkTarget $entry)) { $issues += $path }
}

if ($CheckOnly) {
    if ($issues.Count -gt 0) {
        $issues | ForEach-Object { Write-Host "Missing or incorrect agent skill link: $_" }
        Write-Host 'Run tools/SyncAgentSkills.ps1 to create or repair the local links.'
        exit 1
    }
    Write-Host "Both agent skill links resolve to skills/ ($($skillFolders.Count) skills)."
    exit 0
}

foreach ($path in $issues) {
    $entry = Get-Entry $path
    if ($entry) {
        # Non-recursive deletion of the verified link itself; its target is untouched.
        [IO.Directory]::Delete($path)
    }
    $parent = Split-Path -Parent $path
    if (-not (Test-Path -LiteralPath $parent)) {
        New-Item -ItemType Directory -Path $parent | Out-Null
    }
    $target = '../skills'
    # Windows PowerShell 5.1 requires an absolute target for directory symlinks.
    if ($isWindowsHost) { $target = $sharedRoot }
    try {
        New-Item -ItemType SymbolicLink -Path $path -Target $target -ErrorAction Stop | Out-Null
    }
    catch {
        if (-not $isWindowsHost -or (Get-Entry $path)) { throw }
        New-Item -ItemType Junction -Path $path -Target $sharedRoot -ErrorAction Stop | Out-Null
        Write-Host "Symbolic-link privileges unavailable; created Windows junction: $path"
    }
    if (-not (Test-LinkTarget (Get-Entry $path))) {
        throw "Agent skill link did not resolve to the shared directory: $path"
    }
}
Write-Host "Both agents use the shared skills/ directory ($($skillFolders.Count) skills)."
