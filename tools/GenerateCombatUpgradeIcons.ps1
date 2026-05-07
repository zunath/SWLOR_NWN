param(
    [string]$Feat2daPath = "SWLOR_Haks\swlor2_2da\feat.2da",
    [string]$IconOutputPath = "SWLOR_Haks\swlor2_tga",
    [int]$IconSize = 64
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Drawing

function Get-IconKind([string]$label) {
    if ($label -match "Stance$") { return "stance" }
    if ($label -match "Shield|Guard|Bastion|Rampart|Defense|Defensive|Wall|Fortress|Sentinel|Invincible|Impenetrable|Resolve|Shelter|Unmoving|Adamantine") { return "defense" }
    if ($label -match "Shot|Round|Volley|Fire|Hammer|Gun|Sniper|Deadeye|Overwatch|KillZone|Barrage|Burst|LowShot|OneShot|Headshot|Suppressive|Pacification") { return "ranged" }
    if ($label -match "Toss|Throw|Ricochet|Cluster|Bombardier|Fireburst|Concussive|Smoke|Tranq|Saturation|Pinning") { return "throwing" }
    if ($label -match "Force|Soul|Conduit|Lens|Gyre|Capacitor|Nullification|Suppression|Denial|Purify|Stasis|Current|Static|Storm") { return "energy" }
    if ($label -match "Toxic|Venom|Poison|Nerve|Neural|LifeSiphon|SapVitality|Bloodlust|Disease") { return "toxin" }
    if ($label -match "Palm|Elbows|Rib|Skull|Fang|Claws|Bonecrusher|Worldbreaker|GroundQuake|LineBreaker|Breaker|Counter") { return "martial" }
    if ($label -match "Tactical|Evasive|Roll|Skirmisher|Flanking|Flank|Side|Decoy|Feinting|Duelist|Perceptive|Attentiveness") { return "mobility" }
    if ($label -match "Blade|Saber|Strike|Slash|Cut|Cleave|Carve|Vortex|Cyclone|Tempest|Whirl|Rending|Piercing|Breach|Crushing|Fracture|Hamstring|Leg") { return "melee" }
    if ($label -match "SecondWind|Centering|Calming|Focused|Guardian|Mark|Expose|Precision|Vital|Essence|Hunter") { return "support" }
    return "melee"
}

function Get-KindPalette([string]$kind, [int]$hash) {
    $palettes = @{
        melee    = @(@(42, 18, 24), @(150, 36, 34), @(245, 177, 84))
        ranged   = @(@(14, 28, 44), @(45, 112, 167), @(190, 236, 255))
        throwing = @(@(36, 25, 13), @(204, 101, 34), @(255, 218, 98))
        defense  = @(@(20, 31, 38), @(74, 116, 129), @(217, 237, 220))
        stance   = @(@(24, 24, 36), @(109, 90, 165), @(239, 222, 255))
        energy   = @(@(18, 18, 42), @(76, 73, 202), @(151, 244, 255))
        toxin    = @(@(18, 35, 22), @(67, 142, 64), @(196, 255, 103))
        martial  = @(@(34, 23, 19), @(163, 72, 47), @(255, 214, 150))
        mobility = @(@(15, 32, 36), @(30, 143, 151), @(185, 255, 232))
        support  = @(@(29, 27, 17), @(174, 140, 44), @(255, 246, 173))
    }

    $palette = $palettes[$kind]
    $shift = ($hash % 18) - 9
    return @(
        [System.Drawing.Color]::FromArgb(255, [Math]::Max(0, [Math]::Min(255, $palette[0][0] + $shift)), [Math]::Max(0, [Math]::Min(255, $palette[0][1] + $shift)), [Math]::Max(0, [Math]::Min(255, $palette[0][2] + $shift))),
        [System.Drawing.Color]::FromArgb(255, [Math]::Max(0, [Math]::Min(255, $palette[1][0] + $shift)), [Math]::Max(0, [Math]::Min(255, $palette[1][1] + $shift)), [Math]::Max(0, [Math]::Min(255, $palette[1][2] + $shift))),
        [System.Drawing.Color]::FromArgb(255, [Math]::Max(0, [Math]::Min(255, $palette[2][0] + $shift)), [Math]::Max(0, [Math]::Min(255, $palette[2][1] + $shift)), [Math]::Max(0, [Math]::Min(255, $palette[2][2] + $shift)))
    )
}

function Draw-Starfield($g, [int]$hash, [System.Drawing.Pen]$pen) {
    for ($i = 0; $i -lt 10; $i++) {
        $x = 14 + (($hash + $i * 37) % 100)
        $y = 14 + (([Math]::Floor($hash / 3) + $i * 29) % 100)
        $g.DrawLine($pen, $x, $y, $x + 1, $y + 1)
    }
}

function Draw-Sword($g, [System.Drawing.Color]$accent, [System.Drawing.Color]$hot) {
    $blade = New-Object System.Drawing.Pen($hot, 10)
    $edge = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(220, 255, 255, 255), 3)
    $hilt = New-Object System.Drawing.Pen($accent, 9)
    $g.DrawLine($blade, 82, 26, 45, 94)
    $g.DrawLine($edge, 82, 26, 45, 94)
    $g.DrawLine($hilt, 38, 80, 59, 91)
}

function Draw-Blaster($g, [System.Drawing.Color]$accent, [System.Drawing.Color]$hot) {
    $body = New-Object System.Drawing.SolidBrush($accent)
    $glow = New-Object System.Drawing.Pen($hot, 6)
    $g.FillRectangle($body, 30, 54, 62, 14)
    $g.FillRectangle($body, 62, 66, 13, 23)
    $g.DrawLine($glow, 90, 60, 111, 50)
}

function Draw-Shield($g, [System.Drawing.Color]$accent, [System.Drawing.Color]$hot) {
    $brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(220, $accent))
    $pen = New-Object System.Drawing.Pen($hot, 5)
    $points = @(
        [System.Drawing.Point]::new(64, 24), [System.Drawing.Point]::new(95, 38),
        [System.Drawing.Point]::new(88, 84), [System.Drawing.Point]::new(64, 104),
        [System.Drawing.Point]::new(40, 84), [System.Drawing.Point]::new(33, 38)
    )
    $g.FillPolygon($brush, $points)
    $g.DrawPolygon($pen, $points)
    $g.DrawLine((New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(170,255,255,255), 3)), 64, 33, 64, 92)
}

function Draw-Orb($g, [System.Drawing.Color]$accent, [System.Drawing.Color]$hot) {
    $brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(190, $accent))
    $pen = New-Object System.Drawing.Pen($hot, 6)
    $g.FillEllipse($brush, 41, 41, 46, 46)
    $g.DrawEllipse($pen, 34, 34, 60, 60)
    $g.DrawArc($pen, 25, 49, 78, 28, 190, 250)
}

function Draw-Toxin($g, [System.Drawing.Color]$accent, [System.Drawing.Color]$hot) {
    $brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(210, $accent))
    $pen = New-Object System.Drawing.Pen($hot, 5)
    $g.FillEllipse($brush, 42, 30, 44, 58)
    $g.FillEllipse((New-Object System.Drawing.SolidBrush($hot)), 50, 45, 10, 10)
    $g.FillEllipse((New-Object System.Drawing.SolidBrush($hot)), 68, 45, 10, 10)
    $g.DrawArc($pen, 49, 58, 30, 18, 20, 140)
}

function Draw-Fist($g, [System.Drawing.Color]$accent, [System.Drawing.Color]$hot) {
    $brush = New-Object System.Drawing.SolidBrush($accent)
    $pen = New-Object System.Drawing.Pen($hot, 4)
    foreach ($x in @(37, 50, 63, 76)) { $g.FillEllipse($brush, $x, 32, 16, 27) }
    $g.FillRectangle($brush, 39, 50, 51, 34)
    $g.DrawRectangle($pen, 39, 50, 51, 34)
}

function Draw-Boomerang($g, [System.Drawing.Color]$accent, [System.Drawing.Color]$hot) {
    $pen = New-Object System.Drawing.Pen($accent, 11)
    $tip = New-Object System.Drawing.Pen($hot, 4)
    $g.DrawArc($pen, 27, 33, 76, 52, 205, 210)
    $g.DrawLine($tip, 80, 36, 102, 27)
    $g.DrawLine($tip, 87, 78, 108, 90)
}

function Draw-Stance($g, [System.Drawing.Color]$accent, [System.Drawing.Color]$hot) {
    $pen = New-Object System.Drawing.Pen($hot, 5)
    $brush = New-Object System.Drawing.SolidBrush($accent)
    $g.FillEllipse($brush, 54, 26, 20, 20)
    $g.DrawLine($pen, 64, 48, 64, 83)
    $g.DrawLine($pen, 38, 62, 90, 62)
    $g.DrawLine($pen, 64, 83, 42, 101)
    $g.DrawLine($pen, 64, 83, 86, 101)
}

function Draw-Mobility($g, [System.Drawing.Color]$accent, [System.Drawing.Color]$hot) {
    $pen = New-Object System.Drawing.Pen($hot, 7)
    $g.DrawArc($pen, 31, 34, 65, 56, 35, 250)
    $points = @([System.Drawing.Point]::new(89, 35), [System.Drawing.Point]::new(111, 35), [System.Drawing.Point]::new(97, 55))
    $g.FillPolygon((New-Object System.Drawing.SolidBrush($accent)), $points)
}

function Draw-LevelPips($g, [int]$level, [System.Drawing.Color]$hot) {
    if ($level -lt 1) { return }
    $brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(230, $hot))
    for ($i = 0; $i -lt [Math]::Min($level, 5); $i++) {
        $g.FillEllipse($brush, 16 + ($i * 11), 108, 7, 7)
    }
}

function Write-Tga([System.Drawing.Bitmap]$bitmap, [string]$path) {
    $width = $bitmap.Width
    $height = $bitmap.Height
    $bytes = New-Object byte[] (18 + $width * $height * 4)
    $bytes[2] = 2
    $bytes[12] = [byte]($width -band 0xFF)
    $bytes[13] = [byte](($width -shr 8) -band 0xFF)
    $bytes[14] = [byte]($height -band 0xFF)
    $bytes[15] = [byte](($height -shr 8) -band 0xFF)
    $bytes[16] = 32
    $bytes[17] = 8
    $offset = 18
    for ($y = $height - 1; $y -ge 0; $y--) {
        for ($x = 0; $x -lt $width; $x++) {
            $c = $bitmap.GetPixel($x, $y)
            $bytes[$offset++] = $c.B
            $bytes[$offset++] = $c.G
            $bytes[$offset++] = $c.R
            $bytes[$offset++] = $c.A
        }
    }
    [System.IO.File]::WriteAllBytes($path, $bytes)
}

function New-CombatIcon([string]$label, [int]$row, [string]$outPath, [int]$size = 64) {
    $hash = [Math]::Abs($label.GetHashCode())
    $kind = Get-IconKind $label
    $palette = Get-KindPalette $kind $hash
    $dark = $palette[0]
    $accent = $palette[1]
    $hot = $palette[2]
    $level = 0
    if ($label -match "(\d+)$") { $level = [int]$Matches[1] }

    $large = New-Object System.Drawing.Bitmap 128, 128
    $g = [System.Drawing.Graphics]::FromImage($large)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)

    $bgBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        [System.Drawing.Rectangle]::new(0, 0, 128, 128),
        [System.Drawing.Color]::FromArgb(255, [Math]::Min(255, $dark.R + 18), [Math]::Min(255, $dark.G + 18), [Math]::Min(255, $dark.B + 18)),
        [System.Drawing.Color]::FromArgb(255, 5, 7, 10),
        45
    )
    $g.FillRectangle($bgBrush, 0, 0, 128, 128)

    Draw-Starfield $g $hash (New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(80, $hot), 1))
    $g.FillEllipse((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(32, $hot))), 16, 16, 96, 96)
    $g.DrawEllipse((New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(210, $hot), 4)), 7, 7, 114, 114)
    $g.DrawEllipse((New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(120, 0, 0, 0), 5)), 12, 12, 104, 104)

    switch ($kind) {
        "defense"  { Draw-Shield $g $accent $hot }
        "ranged"   { Draw-Blaster $g $accent $hot }
        "throwing" { Draw-Boomerang $g $accent $hot }
        "energy"   { Draw-Orb $g $accent $hot }
        "toxin"    { Draw-Toxin $g $accent $hot }
        "martial"  { Draw-Fist $g $accent $hot }
        "mobility" { Draw-Mobility $g $accent $hot }
        "stance"   { Draw-Stance $g $accent $hot }
        "support"  { Draw-Orb $g $accent $hot }
        default    { Draw-Sword $g $accent $hot }
    }

    Draw-LevelPips $g $level $hot

    $small = New-Object System.Drawing.Bitmap $size, $size
    $sg = [System.Drawing.Graphics]::FromImage($small)
    $sg.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $sg.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $sg.DrawImage($large, 0, 0, $size, $size)
    Write-Tga $small $outPath

    $sg.Dispose()
    $small.Dispose()
    $g.Dispose()
    $large.Dispose()
}

$featPath = Resolve-Path $Feat2daPath
$iconPath = Resolve-Path $IconOutputPath
$lines = [System.Collections.Generic.List[string]]::new()
$lines.AddRange([System.IO.File]::ReadAllLines($featPath))
$generated = 0

for ($i = 0; $i -lt $lines.Count; $i++) {
    $line = $lines[$i]
    if ($line.Trim().Length -eq 0) { continue }
    $parts = $line -split "\s+"
    if ($parts.Count -lt 5) { continue }
    $row = 0
    if (-not [int]::TryParse($parts[0], [ref]$row)) { continue }
    if ($row -lt 2000 -or $row -gt 2240) { continue }
    if ($parts[1] -eq "****") { continue }

    $resref = "ife_cu$row"
    $parts[4] = $resref
    $lines[$i] = ($parts -join " ")

    $outFile = Join-Path $iconPath "$resref.tga"
    New-CombatIcon $parts[1] $row $outFile $IconSize
    $generated++
}

[System.IO.File]::WriteAllLines($featPath, $lines)
Write-Host "Generated $generated Combat Upgrade feat icons and updated $Feat2daPath."

$renameScript = Join-Path $PSScriptRoot "RenameCombatUpgradeIconResrefs.ps1"
if (Test-Path -LiteralPath $renameScript) {
    & $renameScript -Feat2daPath $Feat2daPath -IconPath $IconOutputPath
}

$formatScript = Join-Path $PSScriptRoot "FormatCombatUpgradeFeatRows.ps1"
if (Test-Path -LiteralPath $formatScript) {
    & $formatScript -Feat2daPath $Feat2daPath
}
