param(
    [Parameter(Mandatory = $true)]
    [string]$SheetPath,

    [Parameter(Mandatory = $true)]
    [string]$TargetsPath,

    [Parameter(Mandatory = $true)]
    [int]$BatchNumber,

    [string]$IconOutputPath = "SWLOR_Haks\swlor2_tga",
    [string]$WorkPath = "output\imagegen\gpt2_icon_production\cropped",
    [string]$MagickPath = "magick",
    [int]$BatchSize = 10,
    [int]$Columns = 5,
    [int]$Rows = 2,
    [int]$IconSize = 32,
    [switch]$AnatomyReviewed
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

        $explicitBadgeRank = (Get-OptionalProperty $row "RankBadge").Trim()
        if (![string]::IsNullOrWhiteSpace($explicitBadgeRank)) {
            $badgeMap[$resref] = $explicitBadgeRank
            continue
        }

        $rank = (Get-OptionalProperty $row "Rank").Trim()
        if ([string]::IsNullOrWhiteSpace($rank)) {
            $badgeMap[$resref] = ""
            continue
        }

        $family = Get-RankFamilyKey $row
        $rankCount = 0
        if ($rankValuesByFamily.ContainsKey($family)) {
            $rankCount = $rankValuesByFamily[$family].Count
        }

        $badgeMap[$resref] = if ($rankCount -gt 1) { $rank } else { "" }
    }

    return $badgeMap
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

function Add-SemanticFrame([string]$inputPath, [string]$category, [string]$outputPath) {
    $semantic = Get-SemanticColor $category
    $highlight = Get-LightenedColor $semantic 45
    $shadow = [System.Drawing.Color]::FromArgb(220, 0, 0, 0)

    $source = [System.Drawing.Bitmap]::FromFile($inputPath)
    $bitmap = [System.Drawing.Bitmap]::new($source.Width, $source.Height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)

    try {
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $graphics.DrawImageUnscaled($source, 0, 0)

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

        $bitmap.Save($outputPath, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $graphics.Dispose()
        $bitmap.Dispose()
        $source.Dispose()
    }
}

function Add-RankBadge([string]$inputPath, [string]$rank, [string]$outputPath) {
    if ([string]::IsNullOrWhiteSpace($rank)) {
        return
    }

    $text = $rank.Trim()
    $isTwoDigit = $text.Length -gt 1
    $badgeWidth = if ($isTwoDigit) { 17 } else { 14 }
    $badgeHeight = 14
    $badgeX = $IconSize - $badgeWidth - 1
    $badgeY = $IconSize - $badgeHeight - 1
    $innerPaddingX = if ($isTwoDigit) { 2.0 } else { 3.0 }
    $innerPaddingY = 1.5

    $source = [System.Drawing.Bitmap]::FromFile($inputPath)
    $bitmap = [System.Drawing.Bitmap]::new($source.Width, $source.Height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)

    try {
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::SingleBitPerPixelGridFit
        $graphics.DrawImageUnscaled($source, 0, 0)

        $badgePath = New-RoundedRectanglePath $badgeX $badgeY $badgeWidth $badgeHeight 3
        $badgeBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(245, 0, 0, 0))
        $badgeBorder = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(235, 255, 255, 255), 1.0)

        try {
            $graphics.FillPath($badgeBrush, $badgePath)
            $graphics.DrawPath($badgeBorder, $badgePath)
        }
        finally {
            $badgeBrush.Dispose()
            $badgeBorder.Dispose()
            $badgePath.Dispose()
        }

        $fontFamily = [System.Drawing.FontFamily]::new("Arial")
        $textPath = [System.Drawing.Drawing2D.GraphicsPath]::new()
        $textPath.AddString(
            $text,
            $fontFamily,
            [int][System.Drawing.FontStyle]::Bold,
            18.0,
            [System.Drawing.PointF]::new(0, 0),
            [System.Drawing.StringFormat]::GenericTypographic)

        try {
            $bounds = $textPath.GetBounds()
            $fitWidth = $badgeWidth - ($innerPaddingX * 2)
            $fitHeight = $badgeHeight - ($innerPaddingY * 2)
            $scale = [Math]::Min($fitWidth / $bounds.Width, $fitHeight / $bounds.Height)

            $matrix = [System.Drawing.Drawing2D.Matrix]::new()
            $matrix.Translate(-$bounds.X, -$bounds.Y)
            $matrix.Scale($scale, $scale, [System.Drawing.Drawing2D.MatrixOrder]::Append)
            $textPath.Transform($matrix)
            $matrix.Dispose()

            $bounds = $textPath.GetBounds()
            $dx = $badgeX + (($badgeWidth - $bounds.Width) / 2.0) - $bounds.X
            $dy = $badgeY + (($badgeHeight - $bounds.Height) / 2.0) - $bounds.Y - 0.35

            $matrix = [System.Drawing.Drawing2D.Matrix]::new()
            $matrix.Translate($dx, $dy)
            $textPath.Transform($matrix)
            $matrix.Dispose()

            $textBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 255, 232, 96))
            try {
                $graphics.FillPath($textBrush, $textPath)
            }
            finally {
                $textBrush.Dispose()
            }
        }
        finally {
            $textPath.Dispose()
            $fontFamily.Dispose()
        }

        $bitmap.Save($outputPath, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $graphics.Dispose()
        $bitmap.Dispose()
        $source.Dispose()
    }
}

if ($BatchNumber -lt 1) {
    throw "BatchNumber must be 1 or greater."
}

if ($Columns -lt 1 -or $Rows -lt 1) {
    throw "Columns and Rows must be 1 or greater."
}

$sheet = (Resolve-Path -LiteralPath $SheetPath).Path
$targets = @(Import-Csv -Path $TargetsPath)
$rankBadgeByResRef = Get-RankBadgeMap $targets
$start = ($BatchNumber - 1) * $BatchSize
$batchTargets = @($targets | Select-Object -Skip $start -First $BatchSize)
if ($batchTargets.Count -eq 0) {
    throw "No targets found for batch $BatchNumber."
}

if ($batchTargets.Count -gt ($Columns * $Rows)) {
    throw "Batch $BatchNumber has $($batchTargets.Count) targets but the sheet grid only has $($Columns * $Rows) cells."
}

$iconDirectory = if ([System.IO.Path]::IsPathRooted($IconOutputPath)) { $IconOutputPath } else { Join-Path (Get-Location).Path $IconOutputPath }
$workDirectory = if ([System.IO.Path]::IsPathRooted($WorkPath)) { $WorkPath } else { Join-Path (Get-Location).Path $WorkPath }
$tileDirectory = Join-Path $workDirectory ("batch_{0:D4}" -f $BatchNumber)
New-Item -ItemType Directory -Path $iconDirectory, $tileDirectory -Force | Out-Null

$productionIconDirectory = Join-Path (Get-Location).Path "SWLOR_Haks\swlor2_tga"
if ($iconDirectory.Equals($productionIconDirectory, [System.StringComparison]::OrdinalIgnoreCase) -and !$AnatomyReviewed) {
    throw "Production icon import requires -AnatomyReviewed. Review the source sheet and enlarged 32x32 preview for malformed fingers, claws, limbs, wings, tails, or other appendages before importing final icons."
}

$script:MagickExecutable = Resolve-MagickPath $MagickPath
$tilePattern = Join-Path $tileDirectory "tile_%02d.png"

Get-ChildItem -LiteralPath $tileDirectory -Filter "tile_*.png" -ErrorAction SilentlyContinue | Remove-Item -Force

Invoke-Magick @(
    $sheet,
    "-crop", "$($Columns)x$($Rows)@",
    "+repage",
    $tilePattern
)

$imported = 0
for ($i = 0; $i -lt $batchTargets.Count; $i++) {
    $target = $batchTargets[$i]
    $tile = Join-Path $tileDirectory ("tile_{0:D2}.png" -f $i)
    if (!(Test-Path -LiteralPath $tile)) {
        throw "Expected cropped tile was not produced: $tile"
    }

    $destination = Join-Path $iconDirectory "$($target.IconResRef).tga"
    $preparedPng = Join-Path $tileDirectory ("{0}_prepared.png" -f $target.IconResRef)
    $basePng = Join-Path $tileDirectory ("{0}_base32.png" -f $target.IconResRef)
    $framedPng = Join-Path $tileDirectory ("{0}_framed32.png" -f $target.IconResRef)
    $finalPng = Join-Path $tileDirectory ("{0}_final32.png" -f $target.IconResRef)

    Invoke-Magick @(
        $tile,
        "-fuzz", "2%",
        "-trim",
        "+repage",
        "-background", "black",
        "-gravity", "center",
        "-set", "option:icon_extent", "%[fx:max(w,h)]",
        "-extent", "%[icon_extent]x%[icon_extent]",
        $preparedPng
    )

    Invoke-Magick @(
        $preparedPng,
        "-filter", "LanczosSharp",
        "-resize", "$($IconSize)x$($IconSize)!",
        "-unsharp", "0x0.55+0.55+0.008",
        "-alpha", "on",
        "-depth", "8",
        $basePng
    )

    Add-SemanticFrame $basePng $target.SemanticCategory $framedPng
    $targetResRef = $target.IconResRef.ToLowerInvariant()
    $badgeRank = if ($rankBadgeByResRef.ContainsKey($targetResRef)) { $rankBadgeByResRef[$targetResRef] } else { "" }
    Add-RankBadge $framedPng $badgeRank $finalPng
    $sourceForTga = if ([string]::IsNullOrWhiteSpace($badgeRank)) { $framedPng } else { $finalPng }

    Invoke-Magick @(
        $sourceForTga,
        "-alpha", "on",
        "-depth", "8",
        "-flip",
        "-orient", "BottomLeft",
        $destination
    )

    $imported++
}

Write-Host "Imported $imported icons from batch $BatchNumber into $iconDirectory."
