param(
    [Parameter(Mandatory = $true)][string]$SeedModulePath,
    [Parameter(Mandatory = $true)][string]$RepositoryRoot,
    [Parameter(Mandatory = $true)][string]$HaksRoot,
    [Parameter(Mandatory = $true)][string]$PackedHakRoot,
    [Parameter(Mandatory = $true)][string]$PackedTlkRoot,
    [Parameter(Mandatory = $true)][string]$NwnInstallRoot
)
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$artifactParent = [IO.Path]::GetFullPath((Join-Path $repo 'artifacts/qualification'))
$artifactRoot = Join-Path $artifactParent ('sw-area-editor-native-' + [Guid]::NewGuid().ToString('N'))
$seed = [IO.Path]::GetFullPath($SeedModulePath)
$cli = [IO.Path]::GetFullPath((Join-Path $repo 'SWLOR.CLI/bin/Release/net10.0/SWLOR.CLI.dll'))
$preview = [IO.Path]::GetFullPath((Join-Path $repo 'tests/SWLOR.Toolset.PreviewRender/bin/Release/net10.0/SWLOR.Toolset.PreviewRender.dll'))
if (-not (Test-Path -LiteralPath $seed -PathType Leaf)) { throw "Seed module does not exist: $seed" }
if (-not (Test-Path -LiteralPath $cli -PathType Leaf)) { throw "Existing SWLOR.CLI DLL is missing: $cli" }
if (-not (Test-Path -LiteralPath $preview -PathType Leaf)) { throw "PreviewRender DLL is missing: $preview" }
$seedHash = (Get-FileHash -LiteralPath $seed -Algorithm SHA256).Hash.ToLowerInvariant()
if ($seedHash -ne 'fb77deed60dfd20819683d2ee4251c5b108fabe57b53366163771c16f2605f66') { throw "Unexpected seed MOD hash: $seedHash" }
if (-not (Test-Path -LiteralPath $artifactParent -PathType Container)) { New-Item -ItemType Directory -Path $artifactParent | Out-Null }
$current = Get-Item -LiteralPath $artifactParent
while ($null -ne $current) {
    if (($current.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "Refusing reparse-point artifact ancestry: $($current.FullName)" }
    if ($current.FullName -eq $repo) { break }
    $current = $current.Parent
}
if ($null -eq $current) { throw 'Artifact root is outside the feature checkout.' }
New-Item -ItemType Directory -Path $artifactRoot | Out-Null
$owner = [ordered]@{ Schema='swlor.area-editor-owned-run.v1'; RunId=(Split-Path $artifactRoot -Leaf); SeedSha256=$seedHash; SourceCommit=(git -C $repo rev-parse HEAD); CreatedUtc=[DateTime]::UtcNow.ToString('O') }
$owner | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $artifactRoot '.swlor-area-editor-owner.json') -Encoding utf8
$assetRootConfigSource = Join-Path $RepositoryRoot 'Build/hakbuilder.json'
$assetRootHaksSource = [IO.Path]::GetFullPath((Join-Path $RepositoryRoot 'SWLOR_Haks'))
if (-not (Test-Path -LiteralPath $assetRootConfigSource -PathType Leaf)) { throw "The selected read-only game-data root lacks its resource configuration: $assetRootConfigSource" }
if (-not (Test-Path -LiteralPath $assetRootHaksSource -PathType Container)) { throw "The selected read-only game-data root lacks its HAK corpus: $assetRootHaksSource" }
$assetRootBuild = Join-Path $artifactRoot 'Build'
$assetRootConfig = Join-Path $assetRootBuild 'hakbuilder.json'
$assetRootHaks = Join-Path $artifactRoot 'SWLOR_Haks'
New-Item -ItemType Directory -Path $assetRootBuild | Out-Null
Copy-Item -LiteralPath $assetRootConfigSource -Destination $assetRootConfig
New-Item -ItemType Junction -Path $assetRootHaks -Target $assetRootHaksSource | Out-Null
$link = Get-Item -LiteralPath $assetRootHaks
if (($link.Attributes -band [IO.FileAttributes]::ReparsePoint) -eq 0 -or $link.LinkType -ne 'Junction' -or [IO.Path]::GetFullPath([string]$link.Target) -ne $assetRootHaksSource) {
    throw 'The owned SWLOR_Haks junction does not resolve to the explicitly selected read-only corpus.'
}
$assetRootManifest = [ordered]@{
    Schema='swlor.area-editor-owned-game-data-root.v1'
    OwnedRepoRoot=$artifactRoot
    ConfigSource=$assetRootConfigSource
    ConfigSourceSha256=(Get-FileHash -LiteralPath $assetRootConfigSource -Algorithm SHA256).Hash.ToLowerInvariant()
    ConfigCopy=$assetRootConfig
    ConfigCopySha256=(Get-FileHash -LiteralPath $assetRootConfig -Algorithm SHA256).Hash.ToLowerInvariant()
    HaksJunction=$assetRootHaks
    HaksTarget=$assetRootHaksSource
    IsReadOnlyInput=$true
}
$assetRootManifest | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $artifactRoot 'owned-game-data-root.json') -Encoding utf8
$seedModuleRoot = Join-Path $artifactRoot 'seed/Module'
New-Item -ItemType Directory -Path $seedModuleRoot -Force | Out-Null
$seedCopy = Join-Path $seedModuleRoot 'seed.mod'
Copy-Item -LiteralPath $seed -Destination $seedCopy
$extract = Start-Process -FilePath 'dotnet' -ArgumentList @($cli, '--unpack', $seedCopy, '--no-prompt') -WorkingDirectory $seedModuleRoot -PassThru -WindowStyle Hidden -RedirectStandardOutput (Join-Path $artifactRoot 'unpack.stdout.log') -RedirectStandardError (Join-Path $artifactRoot 'unpack.stderr.log')
if (-not $extract.WaitForExit(600000)) { $extract.Kill($true); $extract.WaitForExit(); throw 'Owned CLI seed unpack exceeded 600 seconds; logs retained in the run folder.' }
if ($extract.ExitCode -ne 0) { throw "Owned CLI seed unpack failed with $($extract.ExitCode); logs retained in the run folder." }
$seedArea = 'xm_check_area'
$seedArePath = Join-Path $seedModuleRoot "are/$seedArea.are.json"
$seedGitPath = Join-Path $seedModuleRoot "git/$seedArea.git.json"
$seedGicPath = Join-Path $seedModuleRoot "gic/$seedArea.gic.json"
if (-not (Test-Path -LiteralPath $seedArePath -PathType Leaf) -or -not (Test-Path -LiteralPath $seedGitPath -PathType Leaf)) { throw 'The exact seed MOD did not unpack its expected original area resources.' }
$seedInputs = [ordered]@{ Schema='swlor.area-editor-seed-baseline.v1'; SeedMod=$seed; SeedModSha256=$seedHash; OriginalArea=$seedArea; AreJsonSha256=(Get-FileHash -LiteralPath $seedArePath -Algorithm SHA256).Hash.ToLowerInvariant(); GitJsonSha256=(Get-FileHash -LiteralPath $seedGitPath -Algorithm SHA256).Hash.ToLowerInvariant(); CreatureListField='Creature List'; CreatureListHashCapturedByApplication=$true; GicWasAbsent=(-not (Test-Path -LiteralPath $seedGicPath)); GicPath=$seedGicPath }
if (-not $seedInputs.GicWasAbsent) { throw 'The fixture assumption changed: the pinned seed now contains an original GIC resource.' }
$seedInputs | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $artifactRoot 'seed-baseline.json') -Encoding utf8
$baselineModule = Join-Path $seedModuleRoot 'seed-baseline.mod'
$baselinePack = Start-Process -FilePath 'dotnet' -ArgumentList @($cli, '--pack', $baselineModule, '--no-prompt') -WorkingDirectory $seedModuleRoot -PassThru -WindowStyle Hidden -RedirectStandardOutput (Join-Path $artifactRoot 'seed-pack.stdout.log') -RedirectStandardError (Join-Path $artifactRoot 'seed-pack.stderr.log')
if (-not $baselinePack.WaitForExit(600000)) { $baselinePack.Kill($true); $baselinePack.WaitForExit(); throw 'Owned CLI baseline pack exceeded 600 seconds; logs retained in the run folder.' }
if ($baselinePack.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $baselineModule -PathType Leaf)) { throw "Owned CLI baseline pack failed with $($baselinePack.ExitCode); logs retained in the run folder." }
$templateManifest = @()
foreach ($extension in @('are','git','gic')) {
    $source = Join-Path $RepositoryRoot "Module/$extension/area_template.$extension.json"
    $destinationDirectory = Join-Path $seedModuleRoot $extension
    $destination = Join-Path $destinationDirectory "area_template.$extension.json"
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { throw "The read-only primary module lacks its standard New Area template: $source" }
    if (Test-Path -LiteralPath $destination) { throw "The seed already contains a template destination: $destination" }
    Copy-Item -LiteralPath $source -Destination $destination
    $templateManifest += [ordered]@{ Extension=$extension; Source=$source; SourceSha256=(Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash.ToLowerInvariant(); OwnedDestination=$destination; DestinationSha256=(Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash.ToLowerInvariant() }
}
$templateManifest | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $artifactRoot 'new-area-template-augmentation.json') -Encoding utf8
$sourceModule = $seedModuleRoot
$editRoot = Join-Path $artifactRoot 'edit'
$reopenRoot = Join-Path $artifactRoot 'reopen'
function Invoke-PreviewStage([string]$Stage, [string]$RunRoot, [string]$ModuleSource, [string]$OutputPath, [string]$PriorEvidence) {
    $env:SWLOR_PREVIEW_RENDER_MODE = 'full-shell-area-palette'
    $env:SWLOR_AREA_EDITOR_CAPTURE_STAGE = $Stage
    $env:SWLOR_AREA_EDITOR_CAPTURE_ARTIFACT_ROOT = $artifactRoot
    $env:SWLOR_AREA_EDITOR_CAPTURE_RUN_ROOT = $RunRoot
    $env:SWLOR_AREA_EDITOR_CAPTURE_OUTPUT = $OutputPath
    $env:SWLOR_AREA_EDITOR_CAPTURE_MODULE_SOURCE = $ModuleSource
    $env:SWLOR_AREA_EDITOR_CAPTURE_AREA_RESREF = 'sweditarea01'
    $env:SWLOR_TEST_REPOSITORY_ROOT = $RepositoryRoot
    $env:SWLOR_HAKS_ROOT = $HaksRoot
    $env:SWLOR_PACKED_HAK_ROOT = $PackedHakRoot
    $env:SWLOR_PACKED_TLK_ROOT = $PackedTlkRoot
    $env:SWLOR_NWN_INSTALL_ROOT = $NwnInstallRoot
    $env:SWLOR_CLI_DLL = $cli
    $env:SWLOR_AREA_EDITOR_CAPTURE_SEED_MOD = $seed
    $env:SWLOR_AREA_EDITOR_CAPTURE_SEED_BASELINE_MOD = $baselineModule
    $env:SWLOR_AREA_EDITOR_CAPTURE_SEED_ARE_SHA256 = $seedInputs.AreJsonSha256
    $env:SWLOR_AREA_EDITOR_CAPTURE_SEED_GIT_SHA256 = $seedInputs.GitJsonSha256
    if ($PriorEvidence) { $env:SWLOR_AREA_EDITOR_CAPTURE_PRIOR_EVIDENCE = $PriorEvidence }
    elseif (Test-Path Env:SWLOR_AREA_EDITOR_CAPTURE_PRIOR_EVIDENCE) { Remove-Item Env:SWLOR_AREA_EDITOR_CAPTURE_PRIOR_EVIDENCE }
    $stdoutPath = Join-Path $artifactRoot ($Stage + '.stdout.log')
    $stderrPath = Join-Path $artifactRoot ($Stage + '.stderr.log')
    $process = Start-Process -FilePath 'dotnet' -ArgumentList @($preview) -WorkingDirectory $repo -PassThru -WindowStyle Hidden -RedirectStandardOutput $stdoutPath -RedirectStandardError $stderrPath
    if (-not $process.WaitForExit(300000)) { $process.Kill($true); $process.WaitForExit(); throw "$Stage PreviewRender exceeded 300 seconds; exact owned process stopped and logs retained." }
    if ($process.ExitCode -ne 0) { throw "$Stage PreviewRender failed with $($process.ExitCode); logs retained under $artifactRoot." }
}
Invoke-PreviewStage 'edit' $editRoot $sourceModule (Join-Path $editRoot 'editor.png') ''
$priorEvidence = Join-Path $editRoot 'editor-acceptance.json'
if (-not (Test-Path -LiteralPath $priorEvidence -PathType Leaf)) { throw 'Edit stage did not create its acceptance evidence.' }
Invoke-PreviewStage 'reopen' $reopenRoot (Join-Path $editRoot 'Module') (Join-Path $reopenRoot 'reopened.png') $priorEvidence
$editorEvidence = Get-Content -LiteralPath $priorEvidence -Raw | ConvertFrom-Json
$packedModulePath = [IO.Path]::GetFullPath([string]$editorEvidence.PackedModule)
if (-not (Test-Path -LiteralPath $packedModulePath -PathType Leaf)) { throw "Edit evidence points to a missing packed module: $packedModulePath" }
$manifest = [ordered]@{
    Schema='swlor.area-editor-run.v1'; RunRoot=$artifactRoot; SourceCommit=(git -C $repo rev-parse HEAD); SeedPath=$seed; SeedSha256=$seedHash
    RepositoryRoot=[IO.Path]::GetFullPath($RepositoryRoot); HaksRoot=[IO.Path]::GetFullPath($HaksRoot); PackedHakRoot=[IO.Path]::GetFullPath($PackedHakRoot); PackedTlkRoot=[IO.Path]::GetFullPath($PackedTlkRoot); NwnInstallRoot=[IO.Path]::GetFullPath($NwnInstallRoot)
    CliPath=$cli; CliSha256=(Get-FileHash -LiteralPath $cli -Algorithm SHA256).Hash.ToLowerInvariant(); PreviewRenderPath=$preview
    PreviewRenderSha256=(Get-FileHash -LiteralPath $preview -Algorithm SHA256).Hash.ToLowerInvariant()
    FixtureScriptSha256=(Get-FileHash -LiteralPath $PSCommandPath -Algorithm SHA256).Hash.ToLowerInvariant()
    PreviewRenderSourceSha256=[ordered]@{
        FullShellAreaPaletteCapture=(Get-FileHash -LiteralPath (Join-Path $repo 'tests/SWLOR.Toolset.PreviewRender/Application/FullShellAreaPaletteCapture.cs') -Algorithm SHA256).Hash.ToLowerInvariant()
        SharedAreaNativeAcceptance=(Get-FileHash -LiteralPath (Join-Path $repo 'tests/SWLOR.Toolset.PreviewRender/Application/SharedAreaNativeAcceptance.cs') -Algorithm SHA256).Hash.ToLowerInvariant()
    }
    OwnedGameDataRootPath=(Join-Path $artifactRoot 'owned-game-data-root.json'); OwnedGameDataRoot=$assetRootManifest
    SeedBaselinePath=(Join-Path $artifactRoot 'seed-baseline.json'); SeedBaselineModule=$baselineModule; SeedBaselineModuleSha256=(Get-FileHash -LiteralPath $baselineModule -Algorithm SHA256).Hash.ToLowerInvariant(); TemplateAugmentationPath=(Join-Path $artifactRoot 'new-area-template-augmentation.json')
    EditorEvidencePath=$priorEvidence; ReopenEvidencePath=(Join-Path $reopenRoot 'fresh-reopen.json'); PackedModulePath=$packedModulePath; PackedModuleSha256=(Get-FileHash -LiteralPath $packedModulePath -Algorithm SHA256).Hash.ToLowerInvariant()
}
$manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $artifactRoot 'run-manifest.json') -Encoding utf8
Write-Output "SWLOR shared area editor save/reopen/pack fixture passed its bounded stages: $artifactRoot"
