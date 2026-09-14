param(
    [string]$ManifestPath = "SWLOR.Game.Server\Readmes\GameplayIconManifest.csv",
    [string]$IconPath = "SWLOR_Haks\sw_ability",
    [string]$MagickPath = "magick",
    [string]$WorkPath = "SWLOR_Haks\output\tga_origin_normalized"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-MagickPath([string]$requestedPath) {
    if (Test-Path -LiteralPath $requestedPath) {
        return (Resolve-Path -LiteralPath $requestedPath).Path
    }

    $command = Get-Command $requestedPath -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    $candidateRoots = @(
        [Environment]::GetFolderPath("ProgramFiles"),
        [Environment]::GetEnvironmentVariable("ProgramFiles(x86)")
    ) | Where-Object { ![string]::IsNullOrWhiteSpace($_) }

    foreach ($root in $candidateRoots) {
        $candidate = Get-ChildItem -Path $root -Directory -Filter "ImageMagick*" -ErrorAction SilentlyContinue |
            ForEach-Object { Join-Path $_.FullName "magick.exe" } |
            Where-Object { Test-Path -LiteralPath $_ } |
            Select-Object -First 1

        if ($candidate) {
            return $candidate
        }
    }

    throw "ImageMagick 'magick' was not found. Install ImageMagick or pass -MagickPath."
}

function Invoke-Magick([string[]]$arguments) {
    & $script:MagickExecutable @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "ImageMagick failed with exit code $LASTEXITCODE. Arguments: $($arguments -join ' ')"
    }
}

function Test-TopLeftOrigin([string]$path) {
    $bytes = [System.IO.File]::ReadAllBytes($path)
    if ($bytes.Length -lt 18) {
        throw "TGA file '$path' is too small to contain a valid header."
    }

    return (($bytes[17] -band 32) -ne 0)
}

$manifestResolved = (Resolve-Path -LiteralPath $ManifestPath).Path
$iconDirectory = (Resolve-Path -LiteralPath $IconPath).Path
$workDirectory = if ([System.IO.Path]::IsPathRooted($WorkPath)) { $WorkPath } else { Join-Path (Get-Location).Path $WorkPath }
New-Item -ItemType Directory -Path $workDirectory -Force | Out-Null

$script:MagickExecutable = Resolve-MagickPath $MagickPath
$rows = @(Import-Csv -Path $manifestResolved)
$paths = [System.Collections.Generic.List[string]]::new()

foreach ($row in $rows) {
    $resref = [string]$row.IconResRef
    if ([string]::IsNullOrWhiteSpace($resref)) {
        continue
    }

    $paths.Add((Join-Path $iconDirectory "$resref.tga")) | Out-Null

    if ($row.Type -eq "Ability" -and $resref.StartsWith("ife_", [System.StringComparison]::OrdinalIgnoreCase)) {
        $suffix = $resref.Substring(4)
        foreach ($stage in 0..5) {
            $paths.Add((Join-Path $iconDirectory "pr$($stage)_$suffix.tga")) | Out-Null
        }
    }
}

foreach ($file in Get-ChildItem -LiteralPath $iconDirectory -Filter "*.tga") {
    if ($file.BaseName -match "^(ife_|ief_|pr[0-5]_)") {
        $paths.Add($file.FullName) | Out-Null
    }
}

$normalized = 0
foreach ($path in ($paths | Sort-Object -Unique)) {
    if (!(Test-Path -LiteralPath $path)) {
        throw "Missing TGA file '$path'."
    }

    if (!(Test-TopLeftOrigin $path)) {
        continue
    }

    $fileName = [System.IO.Path]::GetFileName($path)
    $tempPath = Join-Path $workDirectory $fileName
    Invoke-Magick @(
        $path,
        "-flip",
        "-orient", "BottomLeft",
        $tempPath
    )

    Move-Item -LiteralPath $tempPath -Destination $path -Force
    $normalized++
}

Write-Host "Normalized $normalized gameplay TGA files to bottom-left origin."
