[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Archive,
    [Parameter(Mandatory)][string]$ToolsetPath,
    [switch]$Apply
)
$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$Archive = [IO.Path]::GetFullPath($Archive)
$ToolsetPath = [IO.Path]::GetFullPath($ToolsetPath)
if (!(Test-Path -LiteralPath $Archive) -or !(Test-Path -LiteralPath $ToolsetPath)) { throw 'The contribution or toolset executable was not found.' }
if ($Apply) {
    $branch = & git -C $repoRoot symbolic-ref --short HEAD
    if ($LASTEXITCODE -ne 0 -or $branch -in @('master', 'main')) { throw 'Apply contributions on a feature branch.' }
    $status = & git -C $repoRoot status --porcelain
    if ($status) { throw 'Commit or stash local edits before importing a contribution.' }
}
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [IO.Compression.ZipFile]::OpenRead($Archive)
try {
    $entry = $zip.GetEntry('contribution.json')
    if (!$entry -or $entry.Length -gt 16777216) { throw 'Invalid contribution manifest.' }
    $reader = New-Object IO.StreamReader($entry.Open())
    try { $manifest = $reader.ReadToEnd() | ConvertFrom-Json } finally { $reader.Dispose() }
    $revision = (& git -C $repoRoot ls-tree HEAD SWLOR_Haks).Split()[2]
    if ($manifest.AssetRevision -ne $revision) { throw "The contribution uses asset revision $($manifest.AssetRevision). Review it on a branch with those pinned assets before merging." }
} finally { $zip.Dispose() }
$mode = if ($Apply) { '--import-contribution' } else { '--check-contribution' }
$process = Start-Process -FilePath $ToolsetPath -ArgumentList @($mode, "`"$Archive`"", "`"$repoRoot`"") -PassThru -WindowStyle Hidden
if (!$process.WaitForExit(120000)) { $process.Kill(); $process.WaitForExit(); throw 'Import timed out. Review Git status before retrying.' }
if ($process.ExitCode -ne 0) { throw 'Contribution check failed. A file may have changed since the snapshot, or the archive is invalid. No conflicting files are overwritten.' }
Write-Host 'Contribution checked. Review git diff before committing; conversations require a matching server build.'
