[CmdletBinding()]
param([Parameter(Mandatory)][string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $OutputDirectory) { throw 'Choose a new output directory; existing packs are never replaced.' }
$revision = (& git -C $repoRoot ls-tree HEAD SWLOR_Haks).Split()[2]
$actual = & git -C (Join-Path $repoRoot 'SWLOR_Haks') rev-parse HEAD
if ($LASTEXITCODE -ne 0 -or $actual -ne $revision) { throw 'Initialize SWLOR_Haks at the parent repository pinned revision first.' }
& git -C (Join-Path $repoRoot 'SWLOR_Haks') diff --quiet
if ($LASTEXITCODE -ne 0) { throw 'Commit or discard local asset edits before publishing an immutable asset pack.' }
New-Item -ItemType Directory -Path $OutputDirectory | Out-Null
$cli = Join-Path $OutputDirectory 'cli'
$packed = Join-Path $OutputDirectory 'packed'
$release = Join-Path $OutputDirectory 'release'
New-Item -ItemType Directory -Path $release | Out-Null
Push-Location $repoRoot
try {
    & dotnet publish SWLOR.CLI/SWLOR.CLI.csproj -c Release -r win-x64 --self-contained true -o $cli -p:RunPostBuildEvent=Never
    if ($LASTEXITCODE -ne 0) { throw 'Building the HAK packer failed.' }
    Get-ChildItem -LiteralPath (Join-Path $repoRoot 'tools/SWLOR.CLI') -File |
        Where-Object Extension -In @('.exe', '.dll') | Copy-Item -Destination $cli
    $config = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'Build/hakbuilder.json') | ConvertFrom-Json
    foreach ($hak in $config.HakList) { $hak.Path = [IO.Path]::GetFullPath((Join-Path (Join-Path $repoRoot 'Build') $hak.Path)) }
    $config.TlkPath = [IO.Path]::GetFullPath((Join-Path (Join-Path $repoRoot 'Build') $config.TlkPath))
    $config.OutputPath = $packed.TrimEnd('\', '/') + '/'
    $config.EnableChecksumChecking = $false
    $config | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'hakbuilder.json') -Encoding UTF8
    Push-Location $OutputDirectory
    try { & (Join-Path $cli 'SWLOR.CLI.exe') -k; if ($LASTEXITCODE -ne 0) { throw 'HAK packing failed.' } }
    finally { Pop-Location }
    & python (Join-Path $PSScriptRoot 'BuildToolsetContent.py') assets --root $repoRoot --output $release --packed $packed --release-url "https://github.com/zunath/SWLOR_NWN/releases/download/toolset-assets-$revision"
    if ($LASTEXITCODE -ne 0) { throw 'Asset download packaging failed.' }
    Write-Host "Asset pack prepared at $release. Upload it once to release toolset-assets-$revision with make-latest=false."
} finally { Pop-Location }
