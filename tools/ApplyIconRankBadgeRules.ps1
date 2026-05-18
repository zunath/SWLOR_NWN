param(
    [string]$ManifestPath = "SWLOR.Game.Server\Readmes\GameplayIconManifest.csv",
    [string]$IconPath = "SWLOR_Haks\swlor2_tga",
    [string]$MagickPath = "magick",
    [string]$WorkPath = "SWLOR_Haks\output\icon_badge_rules",
    [int]$IconSize = 32
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Drawing

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

function Get-OptionalProperty([object]$row, [string]$name) {
    $property = $row.PSObject.Properties[$name]
    if ($property) {
        return [string]$property.Value
    }

    return ""
}

function Get-RankFamilyKey([object]$row) {
    $key = Get-OptionalProperty $row "Key"
    if ([string]::IsNullOrWhiteSpace($key)) {
        $key = Get-OptionalProperty $row "DisplayName"
    }

    if ([string]::IsNullOrWhiteSpace($key)) {
        $key = Get-OptionalProperty $row "IconResRef"
    }

    $key = $key -replace "StatusEffect$", ""
    $rank = (Get-OptionalProperty $row "Rank").Trim()

    if (![string]::IsNullOrWhiteSpace($rank)) {
        $escapedRank = [regex]::Escape($rank)
        if ($key -match "^(.*)$escapedRank$") {
            $key = $Matches[1]
        }
        elseif ($key -match "^(.*?)$escapedRank([A-Z][A-Za-z]*)$") {
            $key = "$($Matches[1])$($Matches[2])"
        }
    }

    return $key.ToLowerInvariant()
}

function Get-RankBadgeMap([object[]]$rows) {
    $rankValuesByFamily = @{}
    foreach ($row in $rows) {
        $rank = (Get-OptionalProperty $row "Rank").Trim()
        if ([string]::IsNullOrWhiteSpace($rank)) {
            continue
        }

        $rankValue = 0
        if (![int]::TryParse($rank, [ref]$rankValue) -or $rankValue -lt 1) {
            continue
        }

        $family = Get-RankFamilyKey $row
        if (!$rankValuesByFamily.ContainsKey($family)) {
            $rankValuesByFamily[$family] = @{}
        }

        $rankValuesByFamily[$family][$rankValue] = $true
    }

    $badgeMap = @{}
    foreach ($row in $rows) {
        $resref = (Get-OptionalProperty $row "IconResRef").Trim().ToLowerInvariant()
        if ([string]::IsNullOrWhiteSpace($resref)) {
            continue
        }

        $rank = (Get-OptionalProperty $row "Rank").Trim()
        $badgeMap[$resref] = ""
        if ([string]::IsNullOrWhiteSpace($rank)) {
            continue
        }

        $family = Get-RankFamilyKey $row
        if ($rankValuesByFamily.ContainsKey($family) -and $rankValuesByFamily[$family].Count -gt 1) {
            $badgeMap[$resref] = $rank
        }
    }

    return $badgeMap
}

function Get-SemanticColor([string]$category) {
    switch ($category) {
        "Beneficial" { return [System.Drawing.Color]::FromArgb(255, 84, 246, 122) }
        "Harmful" { return [System.Drawing.Color]::FromArgb(255, 240, 84, 84) }
        "Self" { return [System.Drawing.Color]::FromArgb(255, 79, 195, 255) }
        "Control" { return [System.Drawing.Color]::FromArgb(255, 181, 108, 255) }
        "Deployable" { return [System.Drawing.Color]::FromArgb(255, 255, 184, 77) }
        "Utility" { return [System.Drawing.Color]::FromArgb(255, 221, 230, 240) }
    }

    throw "Unknown semantic category '$category'."
}

function Get-LightenedColor([System.Drawing.Color]$color, [int]$amount) {
    return [System.Drawing.Color]::FromArgb(
        255,
        [Math]::Min(255, $color.R + $amount),
        [Math]::Min(255, $color.G + $amount),
        [Math]::Min(255, $color.B + $amount))
}

function Blend-Color([System.Drawing.Color]$a, [System.Drawing.Color]$b, [double]$amount) {
    return [System.Drawing.Color]::FromArgb(
        255,
        [int]($a.R + (($b.R - $a.R) * $amount)),
        [int]($a.G + (($b.G - $a.G) * $amount)),
        [int]($a.B + (($b.B - $a.B) * $amount)))
}

function Scale-Color([System.Drawing.Color]$color, [double]$scale) {
    return [System.Drawing.Color]::FromArgb(
        255,
        [int][Math]::Max(0, [Math]::Min(255, $color.R * $scale)),
        [int][Math]::Max(0, [Math]::Min(255, $color.G * $scale)),
        [int][Math]::Max(0, [Math]::Min(255, $color.B * $scale)))
}

function New-RoundedRectanglePath([single]$x, [single]$y, [single]$width, [single]$height, [single]$radius) {
    $path = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $diameter = $radius * 2
    $right = $x + $width
    $bottom = $y + $height

    $path.AddArc($x, $y, $diameter, $diameter, 180, 90)
    $path.AddArc($right - $diameter, $y, $diameter, $diameter, 270, 90)
    $path.AddArc($right - $diameter, $bottom - $diameter, $diameter, $diameter, 0, 90)
    $path.AddArc($x, $bottom - $diameter, $diameter, $diameter, 90, 90)
    $path.CloseFigure()

    return $path
}

function Draw-SemanticFrame([System.Drawing.Graphics]$graphics, [string]$category) {
    $semantic = Get-SemanticColor $category
    $highlight = Get-LightenedColor $semantic 45
    $shadow = [System.Drawing.Color]::FromArgb(220, 0, 0, 0)

    $outerPath = New-RoundedRectanglePath 1.0 1.0 ($IconSize - 2.0) ($IconSize - 2.0) 4.5
    $innerPath = New-RoundedRectanglePath 3.0 3.0 ($IconSize - 6.0) ($IconSize - 6.0) 3.0
    $shadowPen = [System.Drawing.Pen]::new($shadow, 3.0)
    $outerPen = [System.Drawing.Pen]::new($semantic, 2.0)
    $innerPen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(215, $highlight), 1.0)

    try {
        $graphics.DrawPath($shadowPen, $outerPath)
        $graphics.DrawPath($outerPen, $outerPath)
        $graphics.DrawPath($innerPen, $innerPath)
    }
    finally {
        $shadowPen.Dispose()
        $outerPen.Dispose()
        $innerPen.Dispose()
        $outerPath.Dispose()
        $innerPath.Dispose()
    }
}

function Get-BackgroundSampleColor([System.Drawing.Bitmap]$bitmap) {
    $r = 0
    $g = 0
    $b = 0
    $count = 0

    $samplePoints = @()
    foreach ($y in 17..29) {
        foreach ($x in 11..15) {
            $samplePoints += ,@($x, $y)
        }
    }
    foreach ($y in 11..15) {
        foreach ($x in 17..29) {
            $samplePoints += ,@($x, $y)
        }
    }
    foreach ($y in 11..15) {
        foreach ($x in 11..15) {
            $samplePoints += ,@($x, $y)
        }
    }

    foreach ($point in $samplePoints) {
        $color = $bitmap.GetPixel($point[0], $point[1])
        if ($color.A -lt 180) {
            continue
        }

        $brightness = $color.R + $color.G + $color.B
        if ($brightness -gt 650) {
            continue
        }

        $r += $color.R
        $g += $color.G
        $b += $color.B
        $count++
    }

    if ($count -eq 0) {
        return [System.Drawing.Color]::FromArgb(255, 20, 22, 27)
    }

    return [System.Drawing.Color]::FromArgb(255, [int]($r / $count), [int]($g / $count), [int]($b / $count))
}

function Clear-RankBadge([string]$inputPng, [string]$category, [string]$outputPng) {
    $source = [System.Drawing.Bitmap]::FromFile($inputPng)
    $bitmap = [System.Drawing.Bitmap]::new($source.Width, $source.Height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)

    try {
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $graphics.DrawImageUnscaled($source, 0, 0)

        $semantic = Get-SemanticColor $category
        $sample = Get-BackgroundSampleColor $bitmap
        $start = Blend-Color $sample $semantic 0.10
        $end = Scale-Color $sample 0.62
        $clearRect = [System.Drawing.Rectangle]::new(15, 15, $IconSize - 15, $IconSize - 15)
        $brush = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
            $clearRect,
            $start,
            $end,
            [System.Drawing.Drawing2D.LinearGradientMode]::ForwardDiagonal)

        try {
            $graphics.FillRectangle($brush, $clearRect)
        }
        finally {
            $brush.Dispose()
        }

        Draw-SemanticFrame $graphics $category
        $bitmap.Save($outputPng, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $graphics.Dispose()
        $bitmap.Dispose()
        $source.Dispose()
    }
}

$manifestResolved = (Resolve-Path -LiteralPath $ManifestPath).Path
$iconDirectory = (Resolve-Path -LiteralPath $IconPath).Path
$workDirectory = if ([System.IO.Path]::IsPathRooted($WorkPath)) { $WorkPath } else { Join-Path (Get-Location).Path $WorkPath }
New-Item -ItemType Directory -Path $workDirectory -Force | Out-Null

$script:MagickExecutable = Resolve-MagickPath $MagickPath
$rows = @(Import-Csv -Path $manifestResolved)
$rankBadgeByResRef = Get-RankBadgeMap $rows

$updated = 0
foreach ($row in $rows) {
    $rank = (Get-OptionalProperty $row "Rank").Trim()
    if ([string]::IsNullOrWhiteSpace($rank)) {
        continue
    }

    $resref = (Get-OptionalProperty $row "IconResRef").Trim()
    $resrefKey = $resref.ToLowerInvariant()
    if ($rankBadgeByResRef.ContainsKey($resrefKey) -and ![string]::IsNullOrWhiteSpace($rankBadgeByResRef[$resrefKey])) {
        continue
    }

    $iconFile = Join-Path $iconDirectory "$resref.tga"
    if (!(Test-Path -LiteralPath $iconFile)) {
        throw "Missing icon file '$iconFile'."
    }

    $sourcePng = Join-Path $workDirectory "$resref.source.png"
    $patchedPng = Join-Path $workDirectory "$resref.nobadge.png"

    Invoke-Magick @(
        $iconFile,
        $sourcePng
    )

    Clear-RankBadge $sourcePng $row.SemanticCategory $patchedPng

    Invoke-Magick @(
        $patchedPng,
        "-alpha", "on",
        "-depth", "8",
        "-flip",
        "-orient", "BottomLeft",
        $iconFile
    )

    $updated++
}

Write-Host "Applied conditional rank badge rules to $updated single-rank icon textures."
