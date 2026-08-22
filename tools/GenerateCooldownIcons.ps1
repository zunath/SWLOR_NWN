param(
    [string]$Feat2daPath = "SWLOR_Haks\sw_2da\feat.2da",
    [string]$IconPath = "SWLOR_Haks\sw_ability",
    [string]$MagickPath = "magick",
    # Start at 1992 so the Lightsaber Force Link actives (feat rows 1992-1994), which sit just below the
    # main generated block, are covered by a plain -Force run. Rows 1982-1991 have no ife_ feats, so this
    # adds only Force Link; the placeholder-icon abilities below 1992 stay excluded.
    [int]$GeneratedFeatStart = 1992,
    [int]$GeneratedFeatEnd = 2899,
    [int]$IconSize = 32,
    [string[]]$IconResRefs = @(),
    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$MaximumStage = 5
$MaxResourceNameLength = 16

function Resolve-MagickPath {
    param([string]$RequestedPath)

    if (Test-Path -LiteralPath $RequestedPath) {
        return (Resolve-Path -LiteralPath $RequestedPath).Path
    }

    $command = Get-Command $RequestedPath -ErrorAction SilentlyContinue
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

function Invoke-Magick {
    param([string[]]$Arguments)

    & $script:MagickExecutable @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "ImageMagick failed with exit code $LASTEXITCODE. Arguments: $($Arguments -join ' ')"
    }
}

function Get-FeatIcons {
    param(
        [string]$Path,
        [int]$StartRow,
        [int]$EndRow
    )

    $icons = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)

    foreach ($line in Get-Content -Path $Path) {
        if ([string]::IsNullOrWhiteSpace($line)) {
            continue
        }

        $parts = $line -split "\s+"
        if ($parts.Length -lt 5) {
            continue
        }

        $row = 0
        if (![int]::TryParse($parts[0], [ref]$row) -or
            $row -lt $StartRow -or
            $row -gt $EndRow) {
            continue
        }

        $icon = $parts[4]
        if (![string]::IsNullOrWhiteSpace($icon) -and
            $icon -ne "****" -and
            $icon.StartsWith("ife_", [StringComparison]::OrdinalIgnoreCase)) {
            [void]$icons.Add($icon)
        }
    }

    return $icons | Sort-Object
}

function Get-CooldownIconName {
    param(
        [string]$SourceIcon,
        [int]$Stage
    )

    if ($Stage -lt 0 -or $Stage -gt $MaximumStage) {
        throw "Cooldown stage must be between 0 and $MaximumStage."
    }

    if ([string]::IsNullOrWhiteSpace($SourceIcon) -or
        !$SourceIcon.StartsWith("ife_", [StringComparison]::OrdinalIgnoreCase)) {
        throw "Cooldown icon generation expects feat icons to start with 'ife_'. Source icon '$SourceIcon' does not match."
    }

    $name = "pr$($Stage)_$($SourceIcon.Substring(4))"
    if ($name.Length -gt $MaxResourceNameLength) {
        throw "Generated cooldown icon '$name' is longer than NWN's 16-character resource limit."
    }

    return $name
}

function New-CooldownOverlay {
    param(
        [int]$Stage,
        [string]$OutputPath
    )

    $boundsOffset = [Math]::Max(4, [int]($IconSize * 0.09))
    $boundsEnd = $IconSize - $boundsOffset - 1
    $segmentDegrees = 360.0 / $MaximumStage
    $gapDegrees = 7.0
    $shadowWidth = [Math]::Max(6, [int]($IconSize / 8))
    $progressWidth = [Math]::Max(3, [int]($IconSize / 16))
    $drawCommands = @()

    for ($segment = 0; $segment -lt $Stage; $segment++) {
        $startAngle = -90.0 + ($segment * $segmentDegrees) + $gapDegrees
        $endAngle = $startAngle + $segmentDegrees - ($gapDegrees * 2.0)
        $drawCommands += "arc $boundsOffset,$boundsOffset $boundsEnd,$boundsEnd $startAngle,$endAngle"
    }

    $arguments = @(
        "-size", "$($IconSize)x$($IconSize)",
        "xc:none",
        "-alpha", "on",
        "-fill", "none",
        "-stroke", "rgba(5,5,5,0.67)",
        "-strokewidth", "$shadowWidth"
    )

    foreach ($drawCommand in $drawCommands) {
        $arguments += @("-draw", $drawCommand)
    }

    $arguments += @(
        "-stroke", "rgba(84,246,122,1.0)",
        "-strokewidth", "$progressWidth"
    )

    foreach ($drawCommand in $drawCommands) {
        $arguments += @("-draw", $drawCommand)
    }

    $arguments += @(
        "-depth", "8",
        "-compress", "None",
        $OutputPath)
    Invoke-Magick $arguments
}

$script:MagickExecutable = Resolve-MagickPath $MagickPath
$featPath = (Resolve-Path -Path $Feat2daPath).Path
$iconDirectory = (Resolve-Path -Path $IconPath).Path
$icons = if ($IconResRefs.Count -gt 0) {
    $set = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($iconValue in $IconResRefs) {
        foreach ($icon in ([string]$iconValue -split "[,;]")) {
            $trimmed = $icon.Trim()
            if ([string]::IsNullOrWhiteSpace($trimmed) -or
                !$trimmed.StartsWith("ife_", [StringComparison]::OrdinalIgnoreCase)) {
                continue
            }

            [void]$set.Add($trimmed)
        }
    }

    $set | Sort-Object
}
else {
    Get-FeatIcons -Path $featPath -StartRow $GeneratedFeatStart -EndRow $GeneratedFeatEnd
}
$overlayDirectory = Join-Path ([IO.Path]::GetTempPath()) "swlor-cooldown-overlays"
$generated = 0

New-Item -ItemType Directory -Path $overlayDirectory -Force | Out-Null

for ($stage = 1; $stage -le $MaximumStage; $stage++) {
    New-CooldownOverlay -Stage $stage -OutputPath (Join-Path $overlayDirectory "circle$stage.tga")
}

foreach ($icon in $icons) {
    for ($stage = 0; $stage -le $MaximumStage; $stage++) {
        [void](Get-CooldownIconName -SourceIcon $icon -Stage $stage)
    }

    $sourceFile = Join-Path $iconDirectory "$icon.tga"
    if (!(Test-Path -LiteralPath $sourceFile)) {
        throw "Source icon does not exist: $sourceFile"
    }

    $baseFile = Join-Path $iconDirectory "$((Get-CooldownIconName -SourceIcon $icon -Stage 0)).tga"
    if ($Force -or !(Test-Path -LiteralPath $baseFile)) {
        Invoke-Magick @(
            $sourceFile,
            "-resize", "$($IconSize)x$($IconSize)!",
            "-alpha", "on",
            "-grayscale", "Rec709Luma",
            "-fill", "black",
            "-colorize", "40%",
            "-alpha", "opaque",
            "-depth", "8",
            "-compress", "None",
            "-flip",
            "-orient", "BottomLeft",
            $baseFile
        )
        $generated++
    }

    for ($stage = 1; $stage -le $MaximumStage; $stage++) {
        $stageFile = Join-Path $iconDirectory "$((Get-CooldownIconName -SourceIcon $icon -Stage $stage)).tga"
        if (!$Force -and (Test-Path -LiteralPath $stageFile)) {
            continue
        }

        $overlayFile = Join-Path $overlayDirectory "circle$stage.tga"
        Invoke-Magick @(
            $baseFile,
            $overlayFile,
            "-alpha", "on",
            "-compose", "over",
            "-composite",
            "-alpha", "opaque",
            "-depth", "8",
            "-compress", "None",
            "-flip",
            "-orient", "BottomLeft",
            $stageFile
        )
        $generated++
    }
}

$iconCount = @($icons).Count
Write-Host "Generated $generated cooldown icon textures for $iconCount source feat icons with ImageMagick."
