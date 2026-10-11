param(
    [Parameter(Mandatory = $true)][string]$ModuleSource,
    [Parameter(Mandatory = $true)][string]$AreaResRef,
    [Parameter(Mandatory = $true)][ValidateSet('small','large')][string]$Workload,
    [Parameter(Mandatory = $true)][string]$HaksRoot,
    [Parameter(Mandatory = $true)][string]$PackedHakRoot,
    [Parameter(Mandatory = $true)][string]$PackedTlkRoot,
    [Parameter(Mandatory = $true)][string]$NwnInstallRoot,
    [Parameter(Mandatory = $true)][string]$CliDll,
    [string]$PreviewRenderDll,
    [int]$ProcessDeadlineSeconds = 1200,
    [switch]$ExternalContention,
    [string]$ContentionNote = ''
)
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$toolsetSourceRoot = [Environment]::GetEnvironmentVariable('NwnToolsetSourceRoot')
if (-not [string]::IsNullOrWhiteSpace($toolsetSourceRoot)) { throw 'Performance acceptance requires the verified normal-package dependency graph, not a source override.' }
$artifactParent = [IO.Path]::GetFullPath((Join-Path $repo 'artifacts/qualification'))
$module = [IO.Path]::GetFullPath($ModuleSource)
$defaultPreview = Join-Path $repo 'tests/SWLOR.Toolset.PreviewRender/bin/Release/net10.0/SWLOR.Toolset.PreviewRender.dll'
$preview = if ([string]::IsNullOrWhiteSpace($PreviewRenderDll)) { [IO.Path]::GetFullPath($defaultPreview) } else { [IO.Path]::GetFullPath($PreviewRenderDll) }
$cli = [IO.Path]::GetFullPath($CliDll)
$previewPdb = [IO.Path]::ChangeExtension($preview, '.pdb')
$cliPdb = [IO.Path]::ChangeExtension($cli, '.pdb')
foreach ($required in @($module, $HaksRoot, $PackedHakRoot, $PackedTlkRoot, $NwnInstallRoot)) {
    if (-not (Test-Path -LiteralPath $required -PathType Container)) { throw "Required read-only input directory is missing: $required" }
}
foreach ($required in @($preview, $cli, $previewPdb, $cliPdb)) {
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) { throw "Required built executable is missing: $required" }
}
if (-not (Test-Path -LiteralPath (Join-Path $module "are/$AreaResRef.are.json") -PathType Leaf)) {
    throw "The selected module does not contain the exact area fixture '$AreaResRef'."
}
if (-not (Test-Path -LiteralPath (Join-Path $repo 'Build/hakbuilder.json') -PathType Leaf)) {
    throw 'The selected checkout does not contain Build/hakbuilder.json.'
}
$runId = [Guid]::NewGuid().ToString('N')
$artifactRoot = Join-Path $artifactParent "sw-desktop-performance-$runId"
$currentArtifact = Get-Item -LiteralPath $artifactParent
while ($null -ne $currentArtifact) {
    if (($currentArtifact.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "Refusing reparse-point artifact ancestry: $($currentArtifact.FullName)" }
    if ($currentArtifact.FullName -eq $repo) { break }
    $currentArtifact = $currentArtifact.Parent
}
if ($null -eq $currentArtifact) { throw 'Performance output path is outside the selected worktree artifacts folder.' }
New-Item -ItemType Directory -Path $artifactRoot | Out-Null
$inputs = [ordered]@{
    Schema = 'swlor.desktop-performance-inputs.v1'
    RunId = $runId
    SourceCommit = (git -C $repo rev-parse HEAD)
    AreaResRef = $AreaResRef
    Workload = $Workload
    ExternalContentionObserved = [bool]$ExternalContention
    ContentionNote = $ContentionNote
    ModuleSource = $module
    AreaAreSha256 = (Get-FileHash -LiteralPath (Join-Path $module "are/$AreaResRef.are.json") -Algorithm SHA256).Hash.ToLowerInvariant()
    AreaGitSha256 = if (Test-Path -LiteralPath (Join-Path $module "git/$AreaResRef.git.json") -PathType Leaf) { (Get-FileHash -LiteralPath (Join-Path $module "git/$AreaResRef.git.json") -Algorithm SHA256).Hash.ToLowerInvariant() } else { $null }
    AreaGicSha256 = if (Test-Path -LiteralPath (Join-Path $module "gic/$AreaResRef.gic.json") -PathType Leaf) { (Get-FileHash -LiteralPath (Join-Path $module "gic/$AreaResRef.gic.json") -Algorithm SHA256).Hash.ToLowerInvariant() } else { $null }
    PreviewRender = $preview
    PreviewRenderSha256 = (Get-FileHash -LiteralPath $preview -Algorithm SHA256).Hash.ToLowerInvariant()
    PreviewRenderPdbSha256 = (Get-FileHash -LiteralPath $previewPdb -Algorithm SHA256).Hash.ToLowerInvariant()
    Cli = $cli
    CliSha256 = (Get-FileHash -LiteralPath $cli -Algorithm SHA256).Hash.ToLowerInvariant()
    CliPdbSha256 = (Get-FileHash -LiteralPath $cliPdb -Algorithm SHA256).Hash.ToLowerInvariant()
    ToolsetDomainReleaseLockSha256 = (Get-FileHash -LiteralPath (Join-Path $repo 'SWLOR.Toolset.Domain/packages.Release.lock.json') -Algorithm SHA256).Hash.ToLowerInvariant()
    ToolsetReleaseLockSha256 = (Get-FileHash -LiteralPath (Join-Path $repo 'SWLOR.Toolset/packages.Release.lock.json') -Algorithm SHA256).Hash.ToLowerInvariant()
    PreviewRenderReleaseLockSha256 = (Get-FileHash -LiteralPath (Join-Path $repo 'tests/SWLOR.Toolset.PreviewRender/packages.Release.lock.json') -Algorithm SHA256).Hash.ToLowerInvariant()
    NwnToolsetSourceRoot = $toolsetSourceRoot
    PerformanceTypesSourceSha256 = [ordered]@{}
    FixtureScriptSha256 = (Get-FileHash -LiteralPath $PSCommandPath -Algorithm SHA256).Hash.ToLowerInvariant()
    PreviewRenderSourceSha256 = [ordered]@{
        FullShellAreaPaletteCapture = (Get-FileHash -LiteralPath (Join-Path $repo 'tests/SWLOR.Toolset.PreviewRender/Application/FullShellAreaPaletteCapture.cs') -Algorithm SHA256).Hash.ToLowerInvariant()
        AreaPerformanceAcceptance = (Get-FileHash -LiteralPath (Join-Path $repo 'tests/SWLOR.Toolset.PreviewRender/Application/AreaPerformanceAcceptance.cs') -Algorithm SHA256).Hash.ToLowerInvariant()
        ToolsetPerformanceObserver = (Get-FileHash -LiteralPath (Join-Path $repo 'tests/SWLOR.Toolset.PreviewRender/Performance/ToolsetPerformanceObserver.cs') -Algorithm SHA256).Hash.ToLowerInvariant()
    }
    HaksRoot = [IO.Path]::GetFullPath($HaksRoot)
    PackedHakRoot = [IO.Path]::GetFullPath($PackedHakRoot)
    PackedTlkRoot = [IO.Path]::GetFullPath($PackedTlkRoot)
    NwnInstallRoot = [IO.Path]::GetFullPath($NwnInstallRoot)
}
Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'Performance') -Filter '*.cs' -File | Sort-Object Name | ForEach-Object {
    $inputs.PerformanceTypesSourceSha256[$_.Name] = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
}
$inputs | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $artifactRoot 'inputs.json') -Encoding utf8
$stages = [System.Collections.Generic.List[object]]::new()
$environmentNames = @(
    'SWLOR_PREVIEW_RENDER_MODE','SWLOR_AREA_EDITOR_CAPTURE_STAGE','SWLOR_AREA_EDITOR_CAPTURE_ARTIFACT_ROOT',
    'SWLOR_AREA_EDITOR_CAPTURE_RUN_ROOT','SWLOR_AREA_EDITOR_CAPTURE_OUTPUT','SWLOR_AREA_EDITOR_CAPTURE_MODULE_SOURCE',
    'SWLOR_AREA_EDITOR_CAPTURE_AREA_RESREF','SWLOR_TEST_REPOSITORY_ROOT','SWLOR_HAKS_ROOT',
    'SWLOR_PACKED_HAK_ROOT','SWLOR_PACKED_TLK_ROOT','SWLOR_NWN_INSTALL_ROOT','SWLOR_CLI_DLL',
    'SWLOR_AREA_EDITOR_CAPTURE_PRIOR_EVIDENCE','SWLOR_PERFORMANCE_PROCESS_STARTED_UTC'
)
$previousEnvironment = @{}
foreach ($name in $environmentNames) { $previousEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, 'Process') }
function Invoke-OwnedStage([string]$Stage, [string]$RunRoot, [string]$StageModule, [string]$PriorEvidence, [string]$Label) {
    if (Test-Path -LiteralPath $RunRoot) { throw "Refusing to reuse a performance stage directory: $RunRoot" }
    $output = Join-Path $RunRoot 'window.png'
    $env:SWLOR_PREVIEW_RENDER_MODE = 'full-shell-area-palette'
    $env:SWLOR_AREA_EDITOR_CAPTURE_STAGE = $Stage
    $env:SWLOR_AREA_EDITOR_CAPTURE_ARTIFACT_ROOT = $artifactRoot
    $env:SWLOR_AREA_EDITOR_CAPTURE_RUN_ROOT = $RunRoot
    $env:SWLOR_AREA_EDITOR_CAPTURE_OUTPUT = $output
    $env:SWLOR_AREA_EDITOR_CAPTURE_MODULE_SOURCE = $StageModule
    $env:SWLOR_AREA_EDITOR_CAPTURE_AREA_RESREF = $AreaResRef
    $env:SWLOR_TEST_REPOSITORY_ROOT = $repo
    $env:SWLOR_HAKS_ROOT = [IO.Path]::GetFullPath($HaksRoot)
    $env:SWLOR_PACKED_HAK_ROOT = [IO.Path]::GetFullPath($PackedHakRoot)
    $env:SWLOR_PACKED_TLK_ROOT = [IO.Path]::GetFullPath($PackedTlkRoot)
    $env:SWLOR_NWN_INSTALL_ROOT = [IO.Path]::GetFullPath($NwnInstallRoot)
    $env:SWLOR_CLI_DLL = $cli
    if ([string]::IsNullOrWhiteSpace($PriorEvidence)) { Remove-Item Env:SWLOR_AREA_EDITOR_CAPTURE_PRIOR_EVIDENCE -ErrorAction SilentlyContinue }
    else { $env:SWLOR_AREA_EDITOR_CAPTURE_PRIOR_EVIDENCE = $PriorEvidence }
    $stdoutPath = Join-Path $artifactRoot "$Label.stdout.log"
    $stderrPath = Join-Path $artifactRoot "$Label.stderr.log"
    $startedUtc = [DateTimeOffset]::UtcNow
    $env:SWLOR_PERFORMANCE_PROCESS_STARTED_UTC = $startedUtc.ToString('O')
    $process = Start-Process -FilePath 'dotnet' -ArgumentList @($preview) -WorkingDirectory $repo -PassThru -WindowStyle Hidden -RedirectStandardOutput $stdoutPath -RedirectStandardError $stderrPath
    $readyPath = Join-Path $RunRoot 'performance-ready.json'
    $budgetSeconds = if ($Label -eq 'process-cold-edit') { if ($Workload -eq 'large') { 120 } else { 30 } } elseif ($Label.EndsWith('-reopen')) { if ($Workload -eq 'large') { 15 } else { 5 } } else { if ($Workload -eq 'large') { 45 } else { 15 } }
    $watch = [Diagnostics.Stopwatch]::StartNew()
    $ready = $false
    $readinessOverrun = $false
    while (-not $process.HasExited -and $watch.Elapsed.TotalSeconds -lt $ProcessDeadlineSeconds) {
        if (Test-Path -LiteralPath $readyPath -PathType Leaf) {
            $ready = $true
            $readinessOverrun = $watch.Elapsed.TotalSeconds -gt $budgetSeconds
            break
        }
        if ($watch.Elapsed.TotalSeconds -gt $budgetSeconds) { $readinessOverrun = $true }
        Start-Sleep -Milliseconds 50
        $process.Refresh()
    }
    if (-not $process.HasExited -and $watch.Elapsed.TotalSeconds -ge $ProcessDeadlineSeconds) {
        $process.Kill($true)
        $process.WaitForExit()
        $stages.Add([ordered]@{ Label=$Label; Stage=$Stage; StartedUtc=$startedUtc; Ready=$ready; ReadinessDeadlineExceeded=$readinessOverrun; ExitCode=$process.ExitCode; TimedOut=$true; Stdout=$stdoutPath; Stderr=$stderrPath })
        return $false
    }
    $process.WaitForExit()
    $readyRecord = $null
    if ($ready) { $readyRecord = Get-Content -LiteralPath $readyPath -Raw | ConvertFrom-Json }
    $stages.Add([ordered]@{
        Label=$Label; Stage=$Stage; StartedUtc=$startedUtc; Ready=$ready
        ReadinessDeadlineSeconds=$budgetSeconds; ReadinessDeadlineExceeded=$readinessOverrun
        ProcessStartToReadyMilliseconds=if ($readyRecord) { $readyRecord.ProcessStartToReadyMilliseconds } else { $null }
        ExitCode=$process.ExitCode; TimedOut=$false; Stdout=$stdoutPath; Stderr=$stderrPath
        RunRoot=$RunRoot; Observations=Join-Path $RunRoot 'performance-observations.json'
    })
    return $ready -and $process.ExitCode -eq 0 -and -not $readinessOverrun
}
function Add-NotRunRepeats([int]$StartIndex, [string]$Reason) {
    for ($index = $StartIndex; $index -lt 4; $index++) {
        $label = if ($index -eq 0) { 'process-cold' } else { "warm-$index" }
        $stages.Add([ordered]@{ Label="$label-edit"; Stage='performance-edit'; NotRun=$true; Blocked=$Reason })
        $stages.Add([ordered]@{ Label="$label-reopen"; Stage='performance-reopen'; NotRun=$true; Blocked=$Reason })
    }
}
$repeatLabels = @('process-cold','warm-1','warm-2','warm-3')
$stopReason = $null
try {
    for ($index=0; $index -lt 4; $index++) {
        $label = $repeatLabels[$index]
        $iteration = Join-Path $artifactRoot $label
        New-Item -ItemType Directory -Path $iteration | Out-Null
        $editRoot = Join-Path $iteration 'edit-app'
        $reopenRoot = Join-Path $iteration 'reopen-app'
        $editOk = Invoke-OwnedStage 'performance-edit' $editRoot $module '' "$label-edit"
        if (-not $editOk) {
            $stopReason = "Stopped after $label edit failed readiness or exited unsuccessfully; no later repetitions were started."
            $stages.Add([ordered]@{ Label="$label-reopen"; Stage='performance-reopen'; NotRun=$true; Blocked='Owned edit/save/pack stage failed; failure retained.' })
            Add-NotRunRepeats ($index + 1) $stopReason
            break
        }
        $prior = Join-Path $editRoot 'area-performance-edit.json'
        if (-not (Test-Path -LiteralPath $prior -PathType Leaf)) {
            $stopReason = "Stopped after $label edit produced no saved edit evidence; no later repetitions were started."
            $stages.Add([ordered]@{ Label="$label-reopen"; Stage='performance-reopen'; NotRun=$true; Blocked='Edit stage produced no saved edit evidence.' })
            Add-NotRunRepeats ($index + 1) $stopReason
            break
        }
        $reopenOk = Invoke-OwnedStage 'performance-reopen' $reopenRoot (Join-Path $editRoot 'Module') $prior "$label-reopen"
        if (-not $reopenOk) {
            $stopReason = "Stopped after $label reopen failed readiness or exited unsuccessfully; no later repetitions were started."
            Add-NotRunRepeats ($index + 1) $stopReason
            break
        }
    }
}
finally {
    foreach ($name in $environmentNames) {
        $value = $previousEnvironment[$name]
        [Environment]::SetEnvironmentVariable($name, $value, 'Process')
    }
}
$sourceAfter = [ordered]@{
    AreaAreSha256 = (Get-FileHash -LiteralPath (Join-Path $module "are/$AreaResRef.are.json") -Algorithm SHA256).Hash.ToLowerInvariant()
    AreaGitSha256 = if (Test-Path -LiteralPath (Join-Path $module "git/$AreaResRef.git.json") -PathType Leaf) { (Get-FileHash -LiteralPath (Join-Path $module "git/$AreaResRef.git.json") -Algorithm SHA256).Hash.ToLowerInvariant() } else { $null }
    AreaGicSha256 = if (Test-Path -LiteralPath (Join-Path $module "gic/$AreaResRef.gic.json") -PathType Leaf) { (Get-FileHash -LiteralPath (Join-Path $module "gic/$AreaResRef.gic.json") -Algorithm SHA256).Hash.ToLowerInvariant() } else { $null }
}
$sourceUnchanged = $sourceAfter.AreaAreSha256 -eq $inputs.AreaAreSha256 -and $sourceAfter.AreaGitSha256 -eq $inputs.AreaGitSha256 -and $sourceAfter.AreaGicSha256 -eq $inputs.AreaGicSha256
$summary = [ordered]@{
    Schema='swlor.desktop-performance-qualification-attempt.v1'
    RunRoot=$artifactRoot
    Inputs=$inputs
    SourceAfter=$sourceAfter
    SourceModuleUnchanged=$sourceUnchanged
    RepeatLabels=$repeatLabels
    StoppedEarly=($null -ne $stopReason)
    StopReason=$stopReason
    NotRunStages=@($stages | Where-Object { $_.NotRun } | ForEach-Object { $_.Label })
    ColdMeansFreshProcessOnly=$true
    OsFileCachesWereCleared=$false
    ExternalContentionObserved=[bool]$ExternalContention
    ContentionNote=$ContentionNote
    PerformanceBudgetQualified=$false
    Stages=$stages
}
$summary | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $artifactRoot 'performance-qualification-attempt.json') -Encoding utf8
Write-Output "Four-repeat SWLOR desktop performance attempt retained at $artifactRoot"
if (-not $sourceUnchanged -or ($stages | Where-Object { -not $_.Ready -or $_.ExitCode -ne 0 -or $_.ReadinessDeadlineExceeded -or $_.TimedOut -or $_.Blocked })) { exit 1 }
