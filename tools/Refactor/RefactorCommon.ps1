<#
.SYNOPSIS
    Helpers shared by Sync-MaintainabilityBranch.ps1 and Test-MaintainabilityParity.ps1. Not run on its own.

.DESCRIPTION
    Dot-source it: `. (Join-Path $PSScriptRoot 'RefactorCommon.ps1')`. It runs on Windows PowerShell 5.1 and on
    PowerShell 7 (Windows and Linux). It holds the process and git wrappers, the readers for the manifests in
    tools/Refactor/, and the build and test runners that read the TRX result files.

    Failures are exceptions made by New-Failure; each carries the exit code the calling script exits with.
#>

Set-StrictMode -Version Latest

$script:OnWindows = ([System.Environment]::OSVersion.Platform -eq [System.PlatformID]::Win32NT)
$script:Utf8 = New-Object System.Text.UTF8Encoding($false)
$script:GitPath = $null

function New-Failure {
    param([string]$Message, [int]$ExitCode = 1)
    $exception = New-Object System.Exception($Message)
    $exception.Data['ExitCode'] = $ExitCode
    return $exception
}

function Write-Section {
    param([string]$Title)
    Write-Host ''
    Write-Host "== $Title =="
}

function ConvertTo-ArgumentString {
    param([string[]]$Arguments)
    $parts = New-Object 'System.Collections.Generic.List[string]'
    foreach ($argument in $Arguments) {
        if ($argument.Length -gt 0 -and $argument -notmatch '[\s"]') {
            $parts.Add($argument)
            continue
        }
        # Backslashes before a quote are doubled and the quote escaped; trailing backslashes are doubled so they do
        # not escape the closing quote.
        $escaped = [regex]::Replace($argument, '(\\*)"', '$1$1\"')
        $escaped = [regex]::Replace($escaped, '(\\+)$', '$1$1')
        $parts.Add('"' + $escaped + '"')
    }
    return ($parts -join ' ')
}

function Resolve-Executable {
    param([string]$Name)
    $command = Get-Command $Name -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $command) { throw (New-Failure "'$Name' was not found on PATH." 4) }
    return $command.Source
}

function Get-HostExecutable {
    # The PowerShell that is running this script, so child scripts run under the same version.
    return (Get-Process -Id $PID).Path
}

# Starts a process. Without -Stream stdout and stderr are captured; with it the child writes to this console.
# Environment values of $null remove the variable from the child's environment.
function Invoke-Process {
    param(
        [Parameter(Mandatory)][string]$FilePath,
        [string[]]$Arguments = @(),
        [string]$WorkingDirectory,
        [hashtable]$Environment,
        [switch]$Stream
    )

    $info = New-Object System.Diagnostics.ProcessStartInfo
    $info.FileName = $FilePath
    $info.Arguments = ConvertTo-ArgumentString $Arguments
    $info.UseShellExecute = $false
    if ($WorkingDirectory) { $info.WorkingDirectory = $WorkingDirectory }
    if ($Environment) {
        foreach ($key in $Environment.Keys) {
            if ($null -eq $Environment[$key]) {
                [void]$info.EnvironmentVariables.Remove($key)
            }
            else {
                $info.EnvironmentVariables[$key] = [string]$Environment[$key]
            }
        }
    }
    if (-not $Stream) {
        $info.RedirectStandardOutput = $true
        $info.RedirectStandardError = $true
        $info.StandardOutputEncoding = $script:Utf8
        $info.StandardErrorEncoding = $script:Utf8
    }

    $process = [System.Diagnostics.Process]::Start($info)
    $outputTask = $null
    $errorTask = $null
    if (-not $Stream) {
        $outputTask = $process.StandardOutput.ReadToEndAsync()
        $errorTask = $process.StandardError.ReadToEndAsync()
    }
    $process.WaitForExit()

    $output = ''
    $errorText = ''
    if (-not $Stream) {
        $output = $outputTask.Result
        $errorText = $errorTask.Result
    }
    $exitCode = $process.ExitCode
    $process.Dispose()

    $lines = @()
    if ($output.Length -gt 0) {
        $lines = @($output -split "`r?`n" | Where-Object { $_ -ne '' })
    }
    return [pscustomobject]@{ ExitCode = $exitCode; Output = $output; Error = $errorText; Lines = $lines }
}

function Invoke-Git {
    param(
        [string]$Repository,
        [Parameter(Mandatory)][string[]]$Arguments,
        [switch]$AllowFailure,
        [switch]$Network,
        [hashtable]$Environment
    )

    if (-not $script:GitPath) { $script:GitPath = Resolve-Executable 'git' }
    $all = New-Object 'System.Collections.Generic.List[string]'
    $all.AddRange([string[]]@('-c', 'core.quotepath=false', '-c', 'core.longpaths=true'))
    # Git over HTTPS on the maintainer's Windows machine needs the schannel backend (Readmes/AgentEnvironment.md).
    if ($Network -and $script:OnWindows) { $all.AddRange([string[]]@('-c', 'http.sslBackend=schannel')) }
    if ($Repository) { $all.AddRange([string[]]@('-C', $Repository)) }
    $all.AddRange($Arguments)

    $merged = @{ GIT_TERMINAL_PROMPT = '0' }
    if ($Environment) { foreach ($key in $Environment.Keys) { $merged[$key] = $Environment[$key] } }

    $result = Invoke-Process -FilePath $script:GitPath -Arguments $all.ToArray() -Environment $merged
    if ($result.ExitCode -ne 0 -and -not $AllowFailure) {
        $detail = ($result.Error.Trim() + ' ' + $result.Output.Trim()).Trim()
        throw (New-Failure ("git $($Arguments -join ' ') failed (exit $($result.ExitCode)): $detail") 4)
    }
    return $result
}

function Get-GitDirectory {
    param([string]$Repository)
    $path = (Invoke-Git -Repository $Repository -Arguments @('rev-parse', '--absolute-git-dir')).Lines[0]
    return $path
}

# Non-empty, non-comment lines of a manifest. A line may end in ` # comment`.
function Read-ManifestLines {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw (New-Failure "Manifest not found: $Path" 4) }
    $lines = New-Object 'System.Collections.Generic.List[string]'
    foreach ($raw in [System.IO.File]::ReadAllLines($Path)) {
        $line = $raw.Trim()
        if ($line.Length -eq 0 -or $line.StartsWith('#')) { continue }
        $comment = $line.IndexOf(' #')
        if ($comment -ge 0) { $line = $line.Substring(0, $comment).TrimEnd() }
        $lines.Add($line)
    }
    return $lines.ToArray()
}

function Get-RepositoryRootOfScript {
    # tools/Refactor/<script> -> repository root.
    return [System.IO.Path]::GetFullPath((Join-Path (Join-Path $PSScriptRoot '..') '..'))
}

# ---------------------------------------------------------------------------------------------------------------------
# Mechanical scripts (tools/Refactor/MechanicalScripts.txt)
# ---------------------------------------------------------------------------------------------------------------------

function Get-MechanicalScripts {
    param([Parameter(Mandatory)][string]$Root)
    $scripts = New-Object 'System.Collections.Generic.List[object]'
    foreach ($relative in (Read-ManifestLines (Join-Path $Root 'tools/Refactor/MechanicalScripts.txt'))) {
        $full = Join-Path $Root $relative
        if (-not (Test-Path -LiteralPath $full -PathType Leaf)) {
            throw (New-Failure "tools/Refactor/MechanicalScripts.txt lists $relative, which does not exist." 4)
        }
        $scripts.Add([pscustomobject]@{
                Name         = [System.IO.Path]::GetFileNameWithoutExtension($relative)
                RelativePath = $relative
                FullPath     = $full
            })
    }
    return $scripts.ToArray()
}

# Runs one mechanical script against $TargetRoot. Mode: Apply, Check or List.
function Invoke-MechanicalScript {
    param(
        [Parameter(Mandatory)]$Script,
        [Parameter(Mandatory)][string]$TargetRoot,
        [ValidateSet('Apply', 'Check', 'List')][string]$Mode = 'Apply'
    )
    $arguments = @('-NoProfile', '-NonInteractive', '-File', $Script.FullPath, '-RepositoryRoot', $TargetRoot)
    if ($Mode -eq 'Check') { $arguments += '-Check' }
    if ($Mode -eq 'List') { $arguments += '-ListOwnedFiles' }
    return Invoke-Process -FilePath (Get-HostExecutable) -Arguments $arguments
}

# The files a mechanical script owns, as repository-relative paths and folder prefixes (from a trailing /**).
function Get-OwnedFilePatterns {
    param([Parameter(Mandatory)]$Script, [Parameter(Mandatory)][string]$Root)
    $result = Invoke-MechanicalScript -Script $Script -TargetRoot $Root -Mode List
    if ($result.ExitCode -ne 0) {
        throw (New-Failure "$($Script.RelativePath) -ListOwnedFiles failed (exit $($result.ExitCode)): $($result.Error.Trim()) $($result.Output.Trim())" 4)
    }
    return @($result.Lines | ForEach-Object { $_.Trim().Replace('\', '/') } | Where-Object { $_.Length -gt 0 })
}

function Test-OwnedPath {
    param([string]$Path, [string[]]$Patterns)
    $normalized = $Path.Replace('\', '/')
    foreach ($pattern in $Patterns) {
        if ($pattern.EndsWith('/**')) {
            $prefix = $pattern.Substring(0, $pattern.Length - 2)
            if ($normalized.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) { return $true }
        }
        elseif ([string]::Equals($normalized, $pattern, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }
    return $false
}

# ---------------------------------------------------------------------------------------------------------------------
# Build and test
# ---------------------------------------------------------------------------------------------------------------------

function Get-DotnetEnvironment {
    # MSBuild's persistent server can get stuck on this machine (AgentEnvironment.md); node reuse and the shared compiler
    # server would leave processes running after the script ends.
    return @{
        MSBUILDUSESERVER                = '0'
        DOTNET_CLI_TELEMETRY_OPTOUT     = '1'
        DOTNET_NOLOGO                   = '1'
        SWLOR_UPDATE_SNAPSHOTS          = $null
        SWLOR_SNAPSHOT_DIRECTORY        = $null
    }
}

function Invoke-DotnetBuild {
    param(
        [Parameter(Mandatory)][string]$Project,
        [string]$Configuration = 'Debug'
    )
    $arguments = @('build', $Project, '-c', $Configuration, '-p:RunPostBuildEvent=Never', '-v', 'q', '-clp:ErrorsOnly',
        '-nodeReuse:false', '-p:UseSharedCompilation=false')
    $started = Get-Date
    $result = Invoke-Process -FilePath (Resolve-Executable 'dotnet') -Arguments $arguments -Environment (Get-DotnetEnvironment) -Stream
    $seconds = [int]((Get-Date) - $started).TotalSeconds
    if ($result.ExitCode -ne 0) {
        throw (New-Failure "dotnet build $Project failed (exit $($result.ExitCode)) after ${seconds}s; the errors are printed above." 1)
    }
    Write-Host ("Built {0} ({1}s)" -f [System.IO.Path]::GetFileNameWithoutExtension($Project), $seconds)
}

# Reads a TRX file into counts and the failed tests.
function Read-TrxSummary {
    param([string]$TrxPath)
    if (-not (Test-Path -LiteralPath $TrxPath -PathType Leaf)) { return $null }
    $xml = New-Object System.Xml.XmlDocument
    $xml.Load($TrxPath)
    $namespaces = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
    $namespaces.AddNamespace('t', 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010')

    $counters = $xml.SelectSingleNode('//t:ResultSummary/t:Counters', $namespaces)
    $failed = New-Object 'System.Collections.Generic.List[object]'
    foreach ($node in $xml.SelectNodes('//t:Results/t:UnitTestResult[@outcome="Failed"]', $namespaces)) {
        $message = ''
        $messageNode = $node.SelectSingleNode('t:Output/t:ErrorInfo/t:Message', $namespaces)
        if ($messageNode) { $message = ($messageNode.InnerText -split "`r?`n" | Select-Object -First 1) }
        $failed.Add([pscustomobject]@{ Name = $node.GetAttribute('testName'); Message = $message })
    }

    $total = 0; $passed = 0; $failedCount = 0; $notExecuted = 0
    if ($counters) {
        $total = [int]$counters.GetAttribute('total')
        $passed = [int]$counters.GetAttribute('passed')
        $failedCount = [int]$counters.GetAttribute('failed')
        $notExecuted = [int]$counters.GetAttribute('notExecuted')
    }
    return [pscustomobject]@{
        Total       = $total
        Passed      = $passed
        Failed      = $failedCount
        NotExecuted = $notExecuted
        FailedTests = $failed.ToArray()
    }
}

# Runs `dotnet test --no-build` for one project. Returns the project's name, the filter, counts and failed tests.
# A run that produced no result file, or exited non-zero with no failed test, counts as failed.
function Invoke-DotnetTest {
    param(
        [Parameter(Mandatory)][string]$Project,
        [Parameter(Mandatory)][string]$ResultsDirectory,
        [string]$Configuration = 'Debug',
        [string]$Filter,
        [hashtable]$Environment
    )
    $name = [System.IO.Path]::GetFileNameWithoutExtension($Project)
    New-Item -ItemType Directory -Force -Path $ResultsDirectory | Out-Null
    $trx = Join-Path $ResultsDirectory "$name.trx"
    if (Test-Path -LiteralPath $trx) { Remove-Item -LiteralPath $trx -Force }

    $arguments = @('test', $Project, '-c', $Configuration, '--no-build', '--logger', "trx;LogFileName=$name.trx",
        '--results-directory', $ResultsDirectory)
    if ($Filter) { $arguments += @('--filter', $Filter) }

    $merged = Get-DotnetEnvironment
    if ($Environment) { foreach ($key in $Environment.Keys) { $merged[$key] = $Environment[$key] } }

    $started = Get-Date
    $run = Invoke-Process -FilePath (Resolve-Executable 'dotnet') -Arguments $arguments -Environment $merged -Stream
    $seconds = [int]((Get-Date) - $started).TotalSeconds

    $summary = Read-TrxSummary $trx
    $ok = ($run.ExitCode -eq 0)
    if ($null -eq $summary) {
        $summary = [pscustomobject]@{ Total = 0; Passed = 0; Failed = 0; NotExecuted = 0; FailedTests = @() }
        $ok = $false
    }
    elseif ($summary.Failed -gt 0) {
        $ok = $false
    }
    return [pscustomobject]@{
        Project     = $name
        Filter      = $Filter
        ExitCode    = $run.ExitCode
        Ok          = $ok
        Seconds     = $seconds
        Total       = $summary.Total
        Passed      = $summary.Passed
        Failed      = $summary.Failed
        Skipped     = $summary.NotExecuted
        FailedTests = $summary.FailedTests
        HasResults  = (Test-Path -LiteralPath $trx)
    }
}

# tools/Refactor/SyncTests.txt: `project <name>` starts a block, `exclude <term>` lines follow.
function Read-SyncTests {
    param([Parameter(Mandatory)][string]$Root)
    $projects = New-Object 'System.Collections.Generic.List[object]'
    $current = $null
    foreach ($line in (Read-ManifestLines (Join-Path $Root 'tools/Refactor/SyncTests.txt'))) {
        $parts = $line -split '\s+', 2
        switch ($parts[0]) {
            'project' {
                $current = [pscustomobject]@{ Name = $parts[1].Trim(); Excludes = (New-Object 'System.Collections.Generic.List[string]') }
                $projects.Add($current)
            }
            'exclude' {
                if (-not $current) { throw (New-Failure 'tools/Refactor/SyncTests.txt: an exclude line comes before any project line.' 4) }
                $current.Excludes.Add($parts[1].Trim())
            }
            default { throw (New-Failure "tools/Refactor/SyncTests.txt: unknown line '$line'." 4) }
        }
    }
    return $projects.ToArray()
}

function ConvertTo-TestFilter {
    param([string[]]$Excludes)
    if (-not $Excludes -or $Excludes.Count -eq 0) { return $null }
    return (($Excludes | ForEach-Object { "FullyQualifiedName!~$_" }) -join '&')
}

# ---------------------------------------------------------------------------------------------------------------------
# SWLOR_Haks (sparse submodule worktree; Readmes/AgentEnvironment.md)
# ---------------------------------------------------------------------------------------------------------------------

$script:HaksFolders = @('sw_2da', 'sw_tlk', 'sw_ability')

function Test-HaksCheckout {
    param([string]$WorktreeRoot)
    return (Test-Path -LiteralPath (Join-Path (Join-Path $WorktreeRoot 'SWLOR_Haks') '.git'))
}

# Makes sure the haks commit exists in the repository that holds it, fetching it when it does not.
function Confirm-HaksCommit {
    param([string]$HaksRepository, [string]$Sha)
    $exists = Invoke-Git -Repository $HaksRepository -Arguments @('cat-file', '-e', "$Sha^{commit}") -AllowFailure
    if ($exists.ExitCode -eq 0) { return }
    Write-Host "Fetching SWLOR_Haks commit $Sha ..."
    Invoke-Git -Repository $HaksRepository -Arguments @('fetch', 'origin', $Sha) -Network | Out-Null
    Invoke-Git -Repository $HaksRepository -Arguments @('cat-file', '-e', "$Sha^{commit}") | Out-Null
}

# Moves the sparse SWLOR_Haks checkout of $WorktreeRoot to the commit its HEAD pins. Returns a note, or $null when it
# was already there or there is no checkout.
function Update-HaksPointer {
    param([string]$WorktreeRoot)
    if (-not (Test-HaksCheckout $WorktreeRoot)) { return 'SWLOR_Haks is not checked out here; hak-dependent tests will fail until it is (Readmes/AgentEnvironment.md).' }
    $haks = Join-Path $WorktreeRoot 'SWLOR_Haks'
    $expected = (Invoke-Git -Repository $WorktreeRoot -Arguments @('rev-parse', 'HEAD:SWLOR_Haks')).Lines[0]
    $actual = (Invoke-Git -Repository $haks -Arguments @('rev-parse', 'HEAD')).Lines[0]
    if ($expected -eq $actual) { return $null }
    Confirm-HaksCommit -HaksRepository $haks -Sha $expected
    Invoke-Git -Repository $haks -Arguments @('checkout', '--detach', $expected) | Out-Null
    return "SWLOR_Haks moved from $($actual.Substring(0, 10)) to $($expected.Substring(0, 10))."
}

# A sparse SWLOR_Haks worktree inside $WorktreeRoot (a worktree whose HEAD pins the commit), made through the
# repository $AnchorHaksRepository, exactly as AgentEnvironment.md describes.
function Install-SparseHaks {
    param([string]$WorktreeRoot, [string]$AnchorHaksRepository)
    $configured = Invoke-Git -Repository $AnchorHaksRepository -Arguments @('config', '--get', 'extensions.worktreeConfig') -AllowFailure
    if ($configured.ExitCode -ne 0 -or $configured.Lines.Count -eq 0 -or $configured.Lines[0] -ne 'true') {
        throw (New-Failure 'extensions.worktreeConfig is not enabled on the SWLOR_Haks repository. Do the one-time setup in Readmes/AgentEnvironment.md ("Worktrees and the haks submodule"), then run this again.' 4)
    }
    $sha = (Invoke-Git -Repository $WorktreeRoot -Arguments @('rev-parse', 'HEAD:SWLOR_Haks')).Lines[0]
    Confirm-HaksCommit -HaksRepository $AnchorHaksRepository -Sha $sha
    $target = Join-Path $WorktreeRoot 'SWLOR_Haks'
    Invoke-Git -Repository $AnchorHaksRepository -Arguments @('worktree', 'add', '--no-checkout', '--detach', $target, $sha) | Out-Null
    Invoke-Git -Repository $target -Arguments @('config', '--worktree', 'core.worktree', $target) | Out-Null
    $top = (Invoke-Git -Repository $target -Arguments @('rev-parse', '--show-toplevel')).Lines[0]
    if ([System.IO.Path]::GetFullPath($top).TrimEnd('\', '/') -ne [System.IO.Path]::GetFullPath($target).TrimEnd('\', '/')) {
        throw (New-Failure "SWLOR_Haks worktree resolves to $top, not $target (core.worktree override missing)." 4)
    }
    Invoke-Git -Repository $target -Arguments (@('sparse-checkout', 'set', '--cone') + $script:HaksFolders) | Out-Null
    Invoke-Git -Repository $target -Arguments @('checkout', '--detach', $sha) | Out-Null
}
