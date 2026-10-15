[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidatePattern('^\d+\.\d+\.\d+(-[A-Za-z0-9.-]+)?$')][string]$Version,
    [Parameter(Mandatory)][string]$AssetDescriptor,
    [Parameter(Mandatory)][string]$OutputDirectory,
    [string]$SignParameters
)
$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
$AssetDescriptor = [IO.Path]::GetFullPath($AssetDescriptor)
if (Test-Path -LiteralPath $OutputDirectory) { throw 'Choose a new output directory; existing releases are never replaced.' }
New-Item -ItemType Directory -Path $OutputDirectory | Out-Null
$publish = Join-Path $OutputDirectory 'application'
$cli = Join-Path $publish 'tools/SWLOR.CLI'
$release = Join-Path $OutputDirectory 'release'
New-Item -ItemType Directory -Path $release | Out-Null
Push-Location $repoRoot
try {
    $revision = & git rev-parse HEAD
    if ($LASTEXITCODE -ne 0) { throw 'Cannot resolve the release source revision.' }
    foreach ($project in @('SWLOR.Toolset/SWLOR.Toolset.csproj', 'SWLOR.CLI/SWLOR.CLI.csproj')) {
        $target = if ($project -like 'SWLOR.Toolset/*') { $publish } else { $cli }
        & dotnet publish $project -c Release -r win-x64 --self-contained true -o $target -p:RunPostBuildEvent=Never -p:PublishTrimmed=false -p:PublishSingleFile=false -p:RestoreLockedMode=false -p:NuGetLockFilePath=obj/packages.publish.lock.json -p:Version=$Version
        if ($LASTEXITCODE -ne 0) { throw "Publishing $project failed." }
    }
    Get-ChildItem -LiteralPath (Join-Path $repoRoot 'tools/SWLOR.CLI') -File |
        Where-Object Extension -In @('.exe', '.dll') | Copy-Item -Destination $cli
    foreach ($folder in @('SWLOR.NWN.API/NWN', 'SWLOR.NWN.API/NWScript')) {
        $target = Join-Path $publish $folder
        New-Item -ItemType Directory -Path $target -Force | Out-Null
        Get-ChildItem -LiteralPath (Join-Path $repoRoot $folder) -File | Copy-Item -Destination $target
    }
    Copy-Item -LiteralPath (Join-Path $repoRoot 'LICENSE.txt') -Destination $publish
    Copy-Item -LiteralPath (Join-Path $repoRoot 'SWLOR.Toolset/LICENSE-NOTICE.md') -Destination $publish

    # Preserve package-supplied licenses and provenance alongside the executable.
    $notices = Join-Path $publish 'ThirdPartyNotices'
    foreach ($projectName in @('SWLOR.Toolset', 'SWLOR.CLI')) {
        $assetsFile = Join-Path $repoRoot "$projectName/obj/project.assets.json"
        $assets = Get-Content -Raw -LiteralPath $assetsFile | ConvertFrom-Json
        foreach ($library in $assets.libraries.PSObject.Properties) {
            if ($library.Value.type -ne 'package') { continue }
            foreach ($packageFolder in $assets.packageFolders.PSObject.Properties.Name) {
                $package = Join-Path $packageFolder $library.Value.path
                if (!(Test-Path -LiteralPath $package)) { continue }
                $target = Join-Path $notices $library.Name
                New-Item -ItemType Directory -Path $target -Force | Out-Null
                Get-ChildItem -LiteralPath $package -File -Recurse |
                    Where-Object { $_.Name -match '^(LICENSE|NOTICE|COPYING|COPYRIGHT|THIRD.?PARTY)' -or $_.Extension -eq '.nuspec' } |
                    ForEach-Object {
                        $relative = $_.FullName.Substring($package.Length).TrimStart('\', '/')
                        $destination = Join-Path $target $relative
                        New-Item -ItemType Directory -Path (Split-Path $destination -Parent) -Force | Out-Null
                        Copy-Item -LiteralPath $_.FullName -Destination $destination -Force
                    }
            }
        }
    }
    Copy-Item -LiteralPath (Join-Path $repoRoot 'SWLOR.Toolset/LICENSE.GPL-3.0') -Destination $notices

    $catalog = Join-Path $OutputDirectory 'game-code.json'
    $process = Start-Process -FilePath (Join-Path $publish 'SWLOR.Toolset.exe') -ArgumentList @('--export-catalog', "`"$repoRoot`"", $revision, "`"$catalog`"") -PassThru -WindowStyle Hidden
    if (!$process.WaitForExit(120000)) { $process.Kill(); $process.WaitForExit(); throw 'Editor catalog generation timed out.' }
    if ($process.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $catalog)) { throw 'Editor catalog generation failed.' }
    & python (Join-Path $PSScriptRoot 'BuildToolsetContent.py') workspace --root $repoRoot --output $release --catalog $catalog --assets $AssetDescriptor --release-url "https://github.com/zunath/SWLOR_NWN/releases/download/toolset-v$Version"
    if ($LASTEXITCODE -ne 0) { throw 'Workspace packaging failed.' }
    # A local CLI tool installation pins packaging to the same version as the application library.
    $vpk = Join-Path $OutputDirectory 'packaging-tools'
    & dotnet tool install vpk --version 1.2.161 --tool-path $vpk
    if ($LASTEXITCODE -ne 0) { throw 'Installing the packaging tool failed.' }
    $arguments = @('pack', '--packId', 'SWLOR.Toolset', '--packVersion', $Version, '--packDir', $publish, '--mainExe', 'SWLOR.Toolset.exe', '--runtime', 'win-x64', '--packTitle', 'SWLOR Toolset', '--icon', (Join-Path $repoRoot 'SWLOR.Toolset/Assets/swlor.ico'), '--outputDir', $release)
    if ($SignParameters) { $arguments += @('--signParams', $SignParameters) }
    & (Join-Path $vpk 'vpk.exe') @arguments
    if ($LASTEXITCODE -ne 0) { throw 'Installer packaging failed.' }
    Write-Host "Toolset release prepared at $release"
} finally { Pop-Location }
