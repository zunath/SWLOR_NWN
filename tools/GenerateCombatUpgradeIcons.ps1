param(
    [string]$Feat2daPath = "SWLOR_Haks\swlor2_2da\feat.2da",
    [string]$IconOutputPath = "SWLOR_Haks\swlor2_tga",
    [string]$IconManifestPath = "SWLOR.Game.Server\Readmes\GameplayIconManifest.csv",
    [int]$IconSize = 32,
    [int]$GeneratedFeatStart = 2000,
    [int]$GeneratedFeatEnd = 2578,
    [string]$SampleOutputPath = "",
    [string[]]$SampleIconResRefs = @(),
    [switch]$AllowPlaceholderArtwork
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (!$AllowPlaceholderArtwork) {
    throw "GenerateCombatUpgradeIcons.ps1 creates placeholder-quality ability artwork. Use tools\ImportCodexIconContactSheet.ps1 for new source art or tools\RestoreAbilityIconArtwork.ps1 to restore cached polished artwork. Pass -AllowPlaceholderArtwork only for local experiments."
}

Add-Type -AssemblyName System.Drawing

function Get-StableHash([string]$value) {
    $hash = [int64]17
    foreach ($ch in $value.ToCharArray()) {
        $hash = (($hash * 31) + [int][char]$ch) % 2147483647
    }

    return [int]$hash
}

function Get-ManifestKey([string]$type, [string]$key) {
    return "$($type.ToLowerInvariant())|$($key.ToLowerInvariant())"
}

function Import-IconManifest([string]$path) {
    $result = @{}
    if (!(Test-Path -LiteralPath $path)) {
        return $result
    }

    foreach ($row in Import-Csv -Path $path) {
        if ([string]::IsNullOrWhiteSpace($row.Type) -or [string]::IsNullOrWhiteSpace($row.Key)) {
            continue
        }

        $result[(Get-ManifestKey $row.Type $row.Key)] = $row
    }

    return $result
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
    $badgeMap = @{}
    foreach ($row in $rows) {
        $resref = (Get-OptionalProperty $row "IconResRef").Trim().ToLowerInvariant()
        if ([string]::IsNullOrWhiteSpace($resref)) {
            continue
        }

        $badgeMap[$resref] = ""
    }

    return $badgeMap
}

function Get-SemanticCategory([string]$label) {
    $key = Get-ManifestKey "Ability" $label
    if ($script:IconManifest.ContainsKey($key)) {
        $category = $script:IconManifest[$key].SemanticCategory
        if ([string]::IsNullOrWhiteSpace($category)) {
            throw "Ability icon '$label' is missing a SemanticCategory in $IconManifestPath."
        }

        return $category
    }

    throw "Ability icon '$label' is missing from $IconManifestPath. Run tools\UpdateGameplayIconStandards.ps1 -RefreshManifest first."
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

    throw "Unknown icon semantic category '$category'."
}

function Get-IconKind([string]$label) {
    if ($label -match "(Stance|Form)\d*$|BlazingSpikes") { return "stance" }
    if ($label -match "ForceSpark|ForceLightning|ForceDrain|ForceChoke|Nightmare|Collapse|Eclipse|Creeping|WeakenResolve|FuryStance|DevouringStrike|HungerOfTheDark") { return "darkforce" }
    if ($label -match "Benevolence|Renewal|MindTrick|ForcePush|ForceLeap|ForceTouch|ForceMend|ForceSanctuary|GuardianWard|LastStandOfTheLight|PurifyingWave|Innervate|Infusion|ForceJudgment|RadiantLance|SereneFocus|HarmonicRestoration") { return "lightforce" }
    if ($label -match "MedKit|TreatmentKit|Kolto|Resuscitation|EmergencyTriage|EmergencySealant|Coagulant|Antitoxin|PainSuppressant|AdrenalStim|FocusStim|PulseRelay|EmergencyCocktail") { return "medical" }
    if ($label -match "Grenade|Beacon|RemoteCharge|Flamethrower|WristRocket|RailDart|CryoSprayer|SonicBurst|PowerCell|Shielding|Deflector|Rayshield|Dampening|OverloadBarrage|WeaponJam") { return "tech" }
    if ($label -match "Bite|Claw|Pounce|Howl|Roar|Hide|Beast|Prey|Predator|Apex|Pack|Rampage|Primal") { return "beast" }
    if ($label -match "Order|Command|Standard|Formation|Rally|WatchfulPresence|Coordinated|ChargeOrder|PressTheAttack|HoldTheLine|MarkTarget|BreakMorale|CleanseOrder|CourageousResolve|Bolster") { return "command" }
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
        stance   = @(@(14, 28, 42), @(54, 156, 214), @(192, 242, 255))
        energy   = @(@(18, 18, 42), @(76, 73, 202), @(151, 244, 255))
        darkforce = @(@(34, 10, 32), @(136, 40, 130), @(255, 96, 196))
        lightforce = @(@(20, 30, 38), @(66, 139, 170), @(218, 255, 245))
        medical  = @(@(18, 31, 26), @(52, 148, 112), @(188, 255, 191))
        tech     = @(@(27, 27, 27), @(126, 125, 103), @(255, 214, 91))
        beast    = @(@(35, 19, 16), @(144, 65, 43), @(255, 179, 109))
        command  = @(@(25, 24, 36), @(95, 104, 173), @(255, 235, 156))
        toxin    = @(@(18, 35, 22), @(67, 142, 64), @(196, 255, 103))
        martial  = @(@(34, 23, 19), @(163, 72, 47), @(255, 214, 150))
        mobility = @(@(15, 32, 36), @(30, 143, 151), @(185, 255, 232))
        support  = @(@(29, 27, 17), @(174, 140, 44), @(255, 246, 173))
    }

    if (-not $palettes.ContainsKey($kind)) {
        $kind = "melee"
    }

    $palette = $palettes[$kind]
    $shift = ($hash % 18) - 9
    return @(
        [System.Drawing.Color]::FromArgb(255, [Math]::Max(0, [Math]::Min(255, $palette[0][0] + $shift)), [Math]::Max(0, [Math]::Min(255, $palette[0][1] + $shift)), [Math]::Max(0, [Math]::Min(255, $palette[0][2] + $shift))),
        [System.Drawing.Color]::FromArgb(255, [Math]::Max(0, [Math]::Min(255, $palette[1][0] + $shift)), [Math]::Max(0, [Math]::Min(255, $palette[1][1] + $shift)), [Math]::Max(0, [Math]::Min(255, $palette[1][2] + $shift))),
        [System.Drawing.Color]::FromArgb(255, [Math]::Max(0, [Math]::Min(255, $palette[2][0] + $shift)), [Math]::Max(0, [Math]::Min(255, $palette[2][1] + $shift)), [Math]::Max(0, [Math]::Min(255, $palette[2][2] + $shift)))
    )
}

function New-RoundedRectanglePath([float]$x, [float]$y, [float]$width, [float]$height, [float]$radius) {
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $diameter = $radius * 2
    $path.AddArc($x, $y, $diameter, $diameter, 180, 90)
    $path.AddArc($x + $width - $diameter, $y, $diameter, $diameter, 270, 90)
    $path.AddArc($x + $width - $diameter, $y + $height - $diameter, $diameter, $diameter, 0, 90)
    $path.AddArc($x, $y + $height - $diameter, $diameter, $diameter, 90, 90)
    $path.CloseFigure()
    return $path
}

function Get-DarkIconColor([System.Drawing.Color]$color, [double]$scale, [int]$floorBlue = 8) {
    return [System.Drawing.Color]::FromArgb(
        255,
        [int][Math]::Max(3, [Math]::Min(90, $color.R * $scale)),
        [int][Math]::Max(3, [Math]::Min(90, $color.G * $scale)),
        [int][Math]::Max($floorBlue, [Math]::Min(100, $color.B * $scale))
    )
}

function Draw-IconBackdrop($g, [System.Drawing.Color]$semantic, [System.Drawing.Color]$accent, [System.Drawing.Color]$hot, [int]$hash) {
    $outer = New-RoundedRectanglePath 6 6 116 116 17
    $inner = New-RoundedRectanglePath 13 13 102 102 13
    $gradientAngle = 55 + (($hash % 25) - 12)
    $centerX = 64 + (($hash % 11) - 5)
    $centerY = 64 + (([Math]::Floor($hash / 11) % 11) - 5)

    $shadow = New-RoundedRectanglePath 8 10 112 112 16
    $g.FillPath((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(205, 0, 0, 0))), $shadow)
    $shadow.Dispose()

    $bgBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        [System.Drawing.Rectangle]::new(0, 0, 128, 128),
        (Get-DarkIconColor $semantic 0.38),
        (Get-DarkIconColor $semantic 0.08),
        $gradientAngle
    )
    $g.FillPath($bgBrush, $outer)
    $bgBrush.Dispose()

    $glow = New-Object System.Drawing.Drawing2D.PathGradientBrush($inner)
    $glow.CenterColor = [System.Drawing.Color]::FromArgb(120, $semantic)
    $glow.CenterPoint = [System.Drawing.PointF]::new($centerX, $centerY)
    $glow.SurroundColors = @([System.Drawing.Color]::FromArgb(0, $semantic))
    $g.FillPath($glow, $inner)
    $glow.Dispose()

    $g.DrawPath((New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(245, $semantic), 5)), $outer)
    $g.DrawPath((New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(110, $semantic), 2)), $inner)
    $g.DrawPath((New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(170, 0, 0, 0), 3)), (New-RoundedRectanglePath 10 10 108 108 15))
    $outer.Dispose()
    $inner.Dispose()
}

function Invoke-InContentBounds($g, [scriptblock]$drawAction) {
    $state = $g.Save()
    $clip = New-RoundedRectanglePath 18 18 92 92 10
    $g.SetClip($clip)
    & $drawAction
    $g.Restore($state)
    $clip.Dispose()
}

function Draw-IllustrativeAccents($g, [System.Drawing.Color]$accent, [System.Drawing.Color]$hot, [int]$hash) {
    $shadow = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(80, 0, 0, 0), 5)
    $ringHot = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(150, $hot), 3)
    $ringAccent = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(95, $accent), 2)
    $sparkPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(170, 255, 255, 255), 1)
    $sparkHot = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(185, $hot))
    $sparkAccent = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(155, $accent))

    $start = $hash % 360
    $g.DrawArc($shadow, 25, 26, 78, 72, $start + 8, 128)
    $g.DrawArc($ringHot, 24, 24, 80, 74, $start, 132)
    $g.DrawArc($ringAccent, 27, 29, 74, 64, ($start + 184) % 360, 72)

    for ($i = 0; $i -lt 5; $i++) {
        $angle = (($hash + ($i * 73)) % 360) * [Math]::PI / 180
        $radius = 34 + (($hash -shr ($i * 2)) -band 7)
        $cx = 64 + [Math]::Cos($angle) * $radius
        $cy = 64 + [Math]::Sin($angle) * ($radius * 0.78)
        $length = 7 + (($hash -shr ($i + 3)) -band 5)
        $width = 3 + ($i % 2)
        $dx = [Math]::Cos($angle)
        $dy = [Math]::Sin($angle)
        $px = -$dy
        $py = $dx
        $points = @(
            [System.Drawing.Point]::new([int]($cx + $dx * $length), [int]($cy + $dy * $length)),
            [System.Drawing.Point]::new([int]($cx + $px * $width), [int]($cy + $py * $width)),
            [System.Drawing.Point]::new([int]($cx - $dx * ($length * 0.6)), [int]($cy - $dy * ($length * 0.6))),
            [System.Drawing.Point]::new([int]($cx - $px * $width), [int]($cy - $py * $width))
        )
        $sparkBrush = if ($i % 2 -eq 0) { $sparkHot } else { $sparkAccent }
        $g.FillPolygon($sparkBrush, $points)
        $g.DrawPolygon($sparkPen, $points)
    }

    foreach ($dot in @(0, 1, 2)) {
        $angle = (($hash + 41 + ($dot * 97)) % 360) * [Math]::PI / 180
        $x = [int](64 + [Math]::Cos($angle) * (28 + ($dot * 7)))
        $y = [int](64 + [Math]::Sin($angle) * (23 + ($dot * 5)))
        $g.FillEllipse($sparkHot, $x, $y, 3 + ($dot % 2), 3 + ($dot % 2))
    }

    $shadow.Dispose()
    $ringHot.Dispose()
    $ringAccent.Dispose()
    $sparkPen.Dispose()
    $sparkHot.Dispose()
    $sparkAccent.Dispose()
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
    $body = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(235, $accent))
    $dark = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(235, 8, 14, 20))
    $shadow = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(145, 0, 0, 0))
    $outline = New-Object System.Drawing.Pen($hot, 4)
    $highlight = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(220, 235, 252, 255), 3)

    $grip = @(
        [System.Drawing.Point]::new(57, 65), [System.Drawing.Point]::new(76, 65),
        [System.Drawing.Point]::new(68, 93), [System.Drawing.Point]::new(53, 93)
    )
    $bodyShape = @(
        [System.Drawing.Point]::new(24, 52), [System.Drawing.Point]::new(73, 52),
        [System.Drawing.Point]::new(82, 45), [System.Drawing.Point]::new(100, 45),
        [System.Drawing.Point]::new(105, 52), [System.Drawing.Point]::new(97, 62),
        [System.Drawing.Point]::new(76, 66), [System.Drawing.Point]::new(24, 66)
    )
    $shadowBody = @($bodyShape | ForEach-Object { [System.Drawing.Point]::new($_.X + 3, $_.Y + 4) })
    $shadowGrip = @($grip | ForEach-Object { [System.Drawing.Point]::new($_.X + 3, $_.Y + 4) })

    $g.FillPolygon($shadow, $shadowBody)
    $g.FillPolygon($shadow, $shadowGrip)
    $g.FillPolygon($body, $bodyShape)
    $g.DrawPolygon($outline, $bodyShape)
    $g.FillPolygon($body, $grip)
    $g.DrawPolygon($outline, $grip)

    $g.FillRectangle($dark, 34, 56, 21, 5)
    $g.DrawLine($highlight, 31, 55, 73, 55)
    $g.DrawLine($highlight, 84, 49, 99, 47)
    $g.DrawArc((New-Object System.Drawing.Pen($hot, 3)), 45, 60, 24, 22, 195, 145)
    $g.FillEllipse((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(240, 255, 255, 255))), 101, 49, 7, 5)
    $g.DrawLine((New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(190, $hot), 3)), 104, 50, 111, 46)

    $body.Dispose()
    $dark.Dispose()
    $shadow.Dispose()
    $outline.Dispose()
    $highlight.Dispose()
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

function Draw-LightningBolt($g, [System.Drawing.Color]$accent, [System.Drawing.Color]$hot) {
    $points = @(
        [System.Drawing.Point]::new(75, 18), [System.Drawing.Point]::new(39, 67),
        [System.Drawing.Point]::new(59, 66), [System.Drawing.Point]::new(48, 108),
        [System.Drawing.Point]::new(91, 54), [System.Drawing.Point]::new(69, 56)
    )
    $g.FillPolygon((New-Object System.Drawing.SolidBrush($hot)), $points)
    $g.DrawPolygon((New-Object System.Drawing.Pen($accent, 5)), $points)
}

function Draw-SparkBurst($g, [System.Drawing.Color]$accent, [System.Drawing.Color]$hot) {
    $outer = New-Object System.Drawing.Pen($hot, 5)
    $inner = New-Object System.Drawing.Pen($accent, 3)
    for ($i = 0; $i -lt 12; $i++) {
        $angle = (($i * 30) - 90) * [Math]::PI / 180
        $innerRadius = 16 + (($i % 2) * 5)
        $outerRadius = 40 - (($i % 3) * 4)
        $x1 = [int](64 + [Math]::Cos($angle) * $innerRadius)
        $y1 = [int](64 + [Math]::Sin($angle) * $innerRadius)
        $x2 = [int](64 + [Math]::Cos($angle) * $outerRadius)
        $y2 = [int](64 + [Math]::Sin($angle) * $outerRadius)
        $g.DrawLine($outer, $x1, $y1, $x2, $y2)
        $g.DrawLine($inner, $x1, $y1, $x2, $y2)
    }

    $diamond = @(
        [System.Drawing.Point]::new(64, 37), [System.Drawing.Point]::new(82, 64),
        [System.Drawing.Point]::new(64, 91), [System.Drawing.Point]::new(46, 64)
    )
    $g.FillPolygon((New-Object System.Drawing.SolidBrush($accent)), $diamond)
    $g.DrawPolygon((New-Object System.Drawing.Pen($hot, 4)), $diamond)
}

function Draw-BodyAura($g, [System.Drawing.Color]$accent, [System.Drawing.Color]$hot) {
    $aura = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(210, $hot), 5)
    $body = New-Object System.Drawing.Pen($accent, 8)
    $g.DrawEllipse($aura, 30, 19, 68, 90)
    $g.FillEllipse((New-Object System.Drawing.SolidBrush($hot)), 53, 29, 22, 22)
    $g.DrawLine($body, 64, 52, 64, 87)
    $g.DrawLine($body, 43, 67, 85, 67)
    $g.DrawLine($body, 64, 86, 46, 102)
    $g.DrawLine($body, 64, 86, 82, 102)
}

function Draw-DrainGlyph($g, [System.Drawing.Color]$accent, [System.Drawing.Color]$hot) {
    $pen = New-Object System.Drawing.Pen($hot, 6)
    $g.DrawArc($pen, 30, 27, 68, 60, 210, 260)
    $g.DrawArc((New-Object System.Drawing.Pen($accent, 5)), 40, 38, 48, 43, 210, 250)
    $drop = @(
        [System.Drawing.Point]::new(64, 53), [System.Drawing.Point]::new(78, 81),
        [System.Drawing.Point]::new(64, 102), [System.Drawing.Point]::new(50, 81)
    )
    $g.FillPolygon((New-Object System.Drawing.SolidBrush($accent)), $drop)
    $g.DrawPolygon($pen, $drop)
}

function Draw-VortexGlyph($g, [System.Drawing.Color]$accent, [System.Drawing.Color]$hot) {
    $penHot = New-Object System.Drawing.Pen($hot, 6)
    $penAccent = New-Object System.Drawing.Pen($accent, 4)
    $g.DrawArc($penHot, 27, 28, 74, 74, 30, 285)
    $g.DrawArc($penAccent, 39, 39, 50, 50, 210, 285)
    $g.DrawArc($penHot, 49, 49, 30, 30, 30, 255)
    $tip = @([System.Drawing.Point]::new(93, 38), [System.Drawing.Point]::new(111, 39), [System.Drawing.Point]::new(99, 55))
    $g.FillPolygon((New-Object System.Drawing.SolidBrush($hot)), $tip)
}

function Draw-RageGlyph($g, [System.Drawing.Color]$accent, [System.Drawing.Color]$hot) {
    $flame = @(
        [System.Drawing.Point]::new(64, 18), [System.Drawing.Point]::new(82, 51),
        [System.Drawing.Point]::new(73, 49), [System.Drawing.Point]::new(93, 103),
        [System.Drawing.Point]::new(62, 90), [System.Drawing.Point]::new(43, 107),
        [System.Drawing.Point]::new(51, 64), [System.Drawing.Point]::new(38, 70)
    )
    $g.FillPolygon((New-Object System.Drawing.SolidBrush($accent)), $flame)
    $g.DrawPolygon((New-Object System.Drawing.Pen($hot, 4)), $flame)
    $g.DrawLine((New-Object System.Drawing.Pen($hot, 5)), 48, 81, 82, 48)
}

function Draw-MawGlyph($g, [System.Drawing.Color]$accent, [System.Drawing.Color]$hot) {
    $g.FillEllipse((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(220, 5, 5, 8))), 30, 38, 68, 53)
    $g.DrawEllipse((New-Object System.Drawing.Pen($hot, 5)), 30, 38, 68, 53)
    foreach ($x in @(42, 56, 70, 84)) {
        $tooth = @(
            [System.Drawing.Point]::new($x, 42), [System.Drawing.Point]::new($x + 8, 42),
            [System.Drawing.Point]::new($x + 4, 60)
        )
        $g.FillPolygon((New-Object System.Drawing.SolidBrush($accent)), $tooth)
    }
    $g.DrawLine((New-Object System.Drawing.Pen($accent, 5)), 41, 83, 87, 83)
}

function Draw-TendrilsGlyph($g, [System.Drawing.Color]$accent, [System.Drawing.Color]$hot) {
    $pen = New-Object System.Drawing.Pen($hot, 6)
    foreach ($offset in @(-28, -14, 0, 14, 28)) {
        $x = 64 + $offset
        $g.DrawBezier($pen, $x, 101, $x - 20, 73, $x + 20, 58, $x, 27)
    }
    $g.FillEllipse((New-Object System.Drawing.SolidBrush($accent)), 49, 49, 30, 30)
}

function Draw-GripGlyph($g, [System.Drawing.Color]$accent, [System.Drawing.Color]$hot) {
    $brush = New-Object System.Drawing.SolidBrush($accent)
    $pen = New-Object System.Drawing.Pen($hot, 5)
    foreach ($x in @(42, 54, 66, 78)) {
        $g.FillEllipse($brush, $x, 29, 14, 35)
    }
    $g.FillRectangle($brush, 42, 55, 50, 33)
    $g.DrawArc($pen, 27, 29, 76, 67, 210, 120)
    $g.DrawLine($pen, 39, 88, 28, 102)
    $g.DrawLine($pen, 89, 88, 101, 101)
}

function Draw-ChokeGlyph($g, [System.Drawing.Color]$accent, [System.Drawing.Color]$hot) {
    $g.DrawEllipse((New-Object System.Drawing.Pen($accent, 6)), 47, 27, 34, 38)
    $g.DrawLine((New-Object System.Drawing.Pen($accent, 7)), 55, 65, 47, 98)
    $g.DrawLine((New-Object System.Drawing.Pen($accent, 7)), 73, 65, 81, 98)
    $g.DrawArc((New-Object System.Drawing.Pen($hot, 7)), 31, 51, 66, 31, 0, 180)
    $g.DrawLine((New-Object System.Drawing.Pen($hot, 5)), 34, 63, 51, 58)
    $g.DrawLine((New-Object System.Drawing.Pen($hot, 5)), 94, 63, 77, 58)
}

function Draw-ShroudedHeadGlyph($g, [System.Drawing.Color]$accent, [System.Drawing.Color]$hot) {
    $hood = @(
        [System.Drawing.Point]::new(64, 19), [System.Drawing.Point]::new(94, 49),
        [System.Drawing.Point]::new(88, 105), [System.Drawing.Point]::new(40, 105),
        [System.Drawing.Point]::new(34, 49)
    )
    $g.FillPolygon((New-Object System.Drawing.SolidBrush($accent)), $hood)
    $g.FillEllipse((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(230, 8, 8, 12))), 45, 42, 38, 47)
    $g.DrawPolygon((New-Object System.Drawing.Pen($hot, 4)), $hood)
    $g.FillEllipse((New-Object System.Drawing.SolidBrush($hot)), 53, 60, 7, 5)
    $g.FillEllipse((New-Object System.Drawing.SolidBrush($hot)), 68, 60, 7, 5)
}

function Draw-NightmareGlyph($g, [System.Drawing.Color]$accent, [System.Drawing.Color]$hot) {
    $eye = @(
        [System.Drawing.Point]::new(27, 64), [System.Drawing.Point]::new(48, 43),
        [System.Drawing.Point]::new(80, 43), [System.Drawing.Point]::new(101, 64),
        [System.Drawing.Point]::new(80, 85), [System.Drawing.Point]::new(48, 85)
    )
    $g.FillPolygon((New-Object System.Drawing.SolidBrush($accent)), $eye)
    $g.DrawPolygon((New-Object System.Drawing.Pen($hot, 5)), $eye)
    $g.FillEllipse((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(230, 6, 6, 10))), 53, 53, 22, 22)
    $g.FillEllipse((New-Object System.Drawing.SolidBrush($hot)), 59, 59, 10, 10)
    $g.DrawLine((New-Object System.Drawing.Pen($hot, 4)), 36, 98, 92, 98)
}

function Draw-DominateMindGlyph($g, [System.Drawing.Color]$accent, [System.Drawing.Color]$hot) {
    $g.DrawEllipse((New-Object System.Drawing.Pen($accent, 7)), 42, 32, 44, 55)
    $g.DrawArc((New-Object System.Drawing.Pen($hot, 6)), 27, 24, 74, 74, 205, 130)
    $g.DrawLine((New-Object System.Drawing.Pen($hot, 4)), 64, 23, 64, 53)
    $g.DrawLine((New-Object System.Drawing.Pen($hot, 4)), 31, 62, 52, 62)
    $g.DrawLine((New-Object System.Drawing.Pen($hot, 4)), 97, 62, 76, 62)
    $g.FillEllipse((New-Object System.Drawing.SolidBrush($hot)), 56, 54, 16, 16)
}

function Draw-CollapseWillGlyph($g, [System.Drawing.Color]$accent, [System.Drawing.Color]$hot) {
    $g.DrawEllipse((New-Object System.Drawing.Pen($accent, 7)), 39, 30, 50, 62)
    $g.DrawLine((New-Object System.Drawing.Pen($hot, 5)), 62, 29, 55, 51)
    $g.DrawLine((New-Object System.Drawing.Pen($hot, 5)), 55, 51, 69, 62)
    $g.DrawLine((New-Object System.Drawing.Pen($hot, 5)), 69, 62, 58, 91)
    $g.FillRectangle((New-Object System.Drawing.SolidBrush($hot)), 86, 85, 9, 9)
    $g.FillRectangle((New-Object System.Drawing.SolidBrush($hot)), 31, 91, 7, 7)
}

function Draw-EclipseGlyph($g, [System.Drawing.Color]$accent, [System.Drawing.Color]$hot) {
    Draw-Shield $g $accent $hot
    $g.FillEllipse((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(230, 5, 5, 8))), 42, 39, 44, 44)
    $g.DrawArc((New-Object System.Drawing.Pen($hot, 5)), 37, 34, 54, 54, 290, 220)
}

function Draw-CrackedShieldGlyph($g, [System.Drawing.Color]$accent, [System.Drawing.Color]$hot) {
    Draw-Shield $g $accent $hot
    $g.DrawLine((New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(240, 8, 8, 12), 6)), 63, 31, 54, 58)
    $g.DrawLine((New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(240, 8, 8, 12), 6)), 54, 58, 69, 73)
    $g.DrawLine((New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(240, 8, 8, 12), 6)), 69, 73, 57, 101)
}

function Draw-HealingGlyph($g, [System.Drawing.Color]$accent, [System.Drawing.Color]$hot) {
    $brush = New-Object System.Drawing.SolidBrush($accent)
    $g.FillEllipse($brush, 34, 50, 27, 27)
    $g.FillEllipse($brush, 67, 50, 27, 27)
    $g.FillRectangle($brush, 51, 34, 26, 60)
    $g.FillRectangle($brush, 34, 51, 60, 26)
    $g.DrawEllipse((New-Object System.Drawing.Pen($hot, 5)), 26, 26, 76, 76)
}

function Draw-RenewalGlyph($g, [System.Drawing.Color]$accent, [System.Drawing.Color]$hot) {
    $g.DrawArc((New-Object System.Drawing.Pen($hot, 6)), 29, 35, 70, 60, 35, 270)
    $leaf = @(
        [System.Drawing.Point]::new(64, 49), [System.Drawing.Point]::new(95, 35),
        [System.Drawing.Point]::new(82, 70)
    )
    $g.FillPolygon((New-Object System.Drawing.SolidBrush($accent)), $leaf)
    $g.DrawLine((New-Object System.Drawing.Pen($hot, 5)), 64, 51, 64, 101)
}

function Draw-MindTrickGlyph($g, [System.Drawing.Color]$accent, [System.Drawing.Color]$hot) {
    $g.DrawArc((New-Object System.Drawing.Pen($hot, 7)), 28, 29, 72, 72, 20, 300)
    $g.DrawArc((New-Object System.Drawing.Pen($accent, 5)), 43, 43, 42, 42, 205, 300)
    $g.FillEllipse((New-Object System.Drawing.SolidBrush($hot)), 61, 61, 8, 8)
    $g.FillEllipse((New-Object System.Drawing.SolidBrush($accent)), 91, 28, 9, 9)
}

function Draw-SpeechGlyph($g, [System.Drawing.Color]$accent, [System.Drawing.Color]$hot) {
    $g.FillEllipse((New-Object System.Drawing.SolidBrush($accent)), 28, 34, 72, 49)
    $tail = @([System.Drawing.Point]::new(52, 78), [System.Drawing.Point]::new(40, 101), [System.Drawing.Point]::new(73, 80))
    $g.FillPolygon((New-Object System.Drawing.SolidBrush($accent)), $tail)
    $g.DrawEllipse((New-Object System.Drawing.Pen($hot, 5)), 28, 34, 72, 49)
    foreach ($x in @(47, 62, 77)) { $g.FillEllipse((New-Object System.Drawing.SolidBrush($hot)), $x, 55, 7, 7) }
}

function Draw-ForcePushGlyph($g, [System.Drawing.Color]$accent, [System.Drawing.Color]$hot) {
    Draw-GripGlyph $g $accent $hot
    foreach ($y in @(42, 64, 86)) {
        $g.DrawLine((New-Object System.Drawing.Pen($hot, 5)), 87, $y, 113, $y)
        $arrow = @([System.Drawing.Point]::new(112, $y), [System.Drawing.Point]::new(100, $y - 8), [System.Drawing.Point]::new(100, $y + 8))
        $g.FillPolygon((New-Object System.Drawing.SolidBrush($hot)), $arrow)
    }
}

function Draw-ForceLeapGlyph($g, [System.Drawing.Color]$accent, [System.Drawing.Color]$hot) {
    Draw-Stance $g $accent $hot
    $g.DrawArc((New-Object System.Drawing.Pen($hot, 5)), 25, 24, 78, 77, 200, 130)
    $arrow = @([System.Drawing.Point]::new(100, 45), [System.Drawing.Point]::new(84, 40), [System.Drawing.Point]::new(93, 58))
    $g.FillPolygon((New-Object System.Drawing.SolidBrush($hot)), $arrow)
}

function Draw-KitGlyph($g, [System.Drawing.Color]$accent, [System.Drawing.Color]$hot) {
    $brush = New-Object System.Drawing.SolidBrush($accent)
    $g.FillRectangle($brush, 33, 42, 62, 49)
    $g.DrawRectangle((New-Object System.Drawing.Pen($hot, 5)), 33, 42, 62, 49)
    $g.DrawLine((New-Object System.Drawing.Pen($hot, 6)), 64, 51, 64, 82)
    $g.DrawLine((New-Object System.Drawing.Pen($hot, 6)), 49, 66, 79, 66)
}

function Draw-GrenadeGlyph($g, [System.Drawing.Color]$accent, [System.Drawing.Color]$hot) {
    $g.FillEllipse((New-Object System.Drawing.SolidBrush($accent)), 41, 43, 46, 53)
    $g.DrawEllipse((New-Object System.Drawing.Pen($hot, 5)), 41, 43, 46, 53)
    $g.DrawRectangle((New-Object System.Drawing.Pen($hot, 5)), 55, 31, 23, 18)
    $g.DrawArc((New-Object System.Drawing.Pen($hot, 4)), 72, 22, 27, 27, 120, 170)
}

function Draw-BeaconGlyph($g, [System.Drawing.Color]$accent, [System.Drawing.Color]$hot) {
    $g.FillRectangle((New-Object System.Drawing.SolidBrush($accent)), 50, 59, 28, 42)
    $g.DrawRectangle((New-Object System.Drawing.Pen($hot, 5)), 50, 59, 28, 42)
    $g.DrawEllipse((New-Object System.Drawing.Pen($hot, 5)), 42, 25, 44, 44)
    $g.DrawArc((New-Object System.Drawing.Pen($hot, 3)), 31, 14, 66, 66, 200, 140)
}

function Draw-TechShieldGlyph($g, [System.Drawing.Color]$accent, [System.Drawing.Color]$hot) {
    $g.DrawRectangle((New-Object System.Drawing.Pen($accent, 6)), 35, 35, 58, 58)
    $g.DrawLine((New-Object System.Drawing.Pen($hot, 5)), 35, 64, 93, 64)
    $g.DrawLine((New-Object System.Drawing.Pen($hot, 5)), 64, 35, 64, 93)
    $g.FillEllipse((New-Object System.Drawing.SolidBrush($hot)), 56, 56, 16, 16)
}

function Draw-FlameGlyph($g, [System.Drawing.Color]$accent, [System.Drawing.Color]$hot) {
    Draw-RageGlyph $g $accent $hot
    $g.DrawLine((New-Object System.Drawing.Pen($hot, 8)), 21, 91, 107, 91)
}

function Draw-FrostGlyph($g, [System.Drawing.Color]$accent, [System.Drawing.Color]$hot) {
    for ($i = 0; $i -lt 6; $i++) {
        $angle = ($i * 60) * [Math]::PI / 180
        $x = [int](64 + [Math]::Cos($angle) * 38)
        $y = [int](64 + [Math]::Sin($angle) * 38)
        $g.DrawLine((New-Object System.Drawing.Pen($hot, 5)), 64, 64, $x, $y)
    }
    $g.FillEllipse((New-Object System.Drawing.SolidBrush($accent)), 53, 53, 22, 22)
}

function Draw-RocketGlyph($g, [System.Drawing.Color]$accent, [System.Drawing.Color]$hot) {
    $body = @(
        [System.Drawing.Point]::new(83, 27), [System.Drawing.Point]::new(99, 43),
        [System.Drawing.Point]::new(58, 90), [System.Drawing.Point]::new(38, 94),
        [System.Drawing.Point]::new(42, 74)
    )
    $shadow = @($body | ForEach-Object { [System.Drawing.Point]::new($_.X + 3, $_.Y + 4) })
    $g.FillPolygon((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(140, 0, 0, 0))), $shadow)
    $g.FillPolygon((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(235, $accent))), $body)
    $g.DrawPolygon((New-Object System.Drawing.Pen($hot, 5)), $body)
    $g.DrawLine((New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(220, 255, 255, 255), 3)), 82, 35, 53, 78)
    $g.FillEllipse((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(225, 255, 240, 155))), 75, 42, 10, 10)
    $leftFin = @([System.Drawing.Point]::new(44, 76), [System.Drawing.Point]::new(26, 83), [System.Drawing.Point]::new(39, 91))
    $rightFin = @([System.Drawing.Point]::new(55, 89), [System.Drawing.Point]::new(48, 108), [System.Drawing.Point]::new(67, 94))
    $g.FillPolygon((New-Object System.Drawing.SolidBrush($hot)), $leftFin)
    $g.FillPolygon((New-Object System.Drawing.SolidBrush($hot)), $rightFin)
    $flame = @([System.Drawing.Point]::new(37, 94), [System.Drawing.Point]::new(18, 109), [System.Drawing.Point]::new(31, 88))
    $g.FillPolygon((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(235, 255, 197, 61))), $flame)
}

function Draw-DartGlyph($g, [System.Drawing.Color]$accent, [System.Drawing.Color]$hot) {
    $shaft = New-Object System.Drawing.Pen($hot, 7)
    $core = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(220, 255, 255, 255), 2)
    $g.DrawLine((New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(145, 0, 0, 0), 9)), 28, 88, 101, 44)
    $g.DrawLine($shaft, 27, 84, 98, 43)
    $g.DrawLine($core, 34, 80, 90, 47)
    $head = @([System.Drawing.Point]::new(104, 40), [System.Drawing.Point]::new(80, 38), [System.Drawing.Point]::new(92, 60))
    $g.FillPolygon((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(235, $accent))), $head)
    $g.DrawPolygon((New-Object System.Drawing.Pen($hot, 3)), $head)
    $rear = @([System.Drawing.Point]::new(34, 78), [System.Drawing.Point]::new(21, 58), [System.Drawing.Point]::new(45, 72))
    $g.FillPolygon((New-Object System.Drawing.SolidBrush($accent)), $rear)
    $g.DrawPolygon((New-Object System.Drawing.Pen($hot, 2)), $rear)
    $shaft.Dispose()
    $core.Dispose()
}

function Draw-SonicGlyph($g, [System.Drawing.Color]$accent, [System.Drawing.Color]$hot) {
    $g.FillPie((New-Object System.Drawing.SolidBrush($accent)), 33, 49, 41, 31, 90, 180)
    foreach ($size in @(30, 46, 62)) {
        $g.DrawArc((New-Object System.Drawing.Pen($hot, 4)), 48, 64 - ($size / 2), $size, $size, 300, 120)
    }
}

function Draw-FlashGlyph($g, [System.Drawing.Color]$accent, [System.Drawing.Color]$hot) {
    $eye = @(
        [System.Drawing.Point]::new(28, 64), [System.Drawing.Point]::new(48, 45),
        [System.Drawing.Point]::new(80, 45), [System.Drawing.Point]::new(100, 64),
        [System.Drawing.Point]::new(80, 83), [System.Drawing.Point]::new(48, 83)
    )
    $g.FillPolygon((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(190, $accent))), $eye)
    $g.DrawPolygon((New-Object System.Drawing.Pen($hot, 5)), $eye)
    $g.FillEllipse((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(235, 5, 7, 10))), 54, 54, 20, 20)

    $burst = @(
        [System.Drawing.Point]::new(64, 25), [System.Drawing.Point]::new(70, 50),
        [System.Drawing.Point]::new(94, 38), [System.Drawing.Point]::new(78, 61),
        [System.Drawing.Point]::new(102, 68), [System.Drawing.Point]::new(75, 72),
        [System.Drawing.Point]::new(84, 101), [System.Drawing.Point]::new(64, 78),
        [System.Drawing.Point]::new(44, 101), [System.Drawing.Point]::new(53, 72),
        [System.Drawing.Point]::new(26, 68), [System.Drawing.Point]::new(50, 61),
        [System.Drawing.Point]::new(34, 38), [System.Drawing.Point]::new(58, 50)
    )
    $g.FillPolygon((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(230, 255, 235, 98))), $burst)
    $g.DrawPolygon((New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(235, 255, 255, 255), 3)), $burst)
    $g.DrawLine((New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(225, 255, 255, 255), 4)), 35, 91, 93, 37)
}

function Draw-BeastClawGlyph($g, [System.Drawing.Color]$accent, [System.Drawing.Color]$hot) {
    foreach ($x in @(39, 56, 73)) {
        $claw = @(
            [System.Drawing.Point]::new($x, 28), [System.Drawing.Point]::new($x + 14, 31),
            [System.Drawing.Point]::new($x + 3, 100), [System.Drawing.Point]::new($x - 8, 96)
        )
        $g.FillPolygon((New-Object System.Drawing.SolidBrush($accent)), $claw)
        $g.DrawPolygon((New-Object System.Drawing.Pen($hot, 3)), $claw)
    }
}

function Draw-BeastBiteGlyph($g, [System.Drawing.Color]$accent, [System.Drawing.Color]$hot) {
    Draw-MawGlyph $g $accent $hot
    $g.DrawArc((New-Object System.Drawing.Pen($accent, 6)), 25, 23, 78, 78, 35, 110)
    $g.DrawArc((New-Object System.Drawing.Pen($accent, 6)), 25, 28, 78, 78, 215, 110)
}

function Draw-RoarGlyph($g, [System.Drawing.Color]$accent, [System.Drawing.Color]$hot) {
    Draw-MawGlyph $g $accent $hot
    $g.DrawArc((New-Object System.Drawing.Pen($hot, 4)), 21, 24, 86, 86, 310, 100)
    $g.DrawArc((New-Object System.Drawing.Pen($hot, 3)), 14, 17, 100, 100, 310, 100)
}

function Draw-HideGlyph($g, [System.Drawing.Color]$accent, [System.Drawing.Color]$hot) {
    Draw-Shield $g $accent $hot
    foreach ($x in @(48, 64, 80)) {
        $g.FillEllipse((New-Object System.Drawing.SolidBrush($hot)), $x, 55, 8, 8)
    }
}

function Draw-RallyGlyph($g, [System.Drawing.Color]$accent, [System.Drawing.Color]$hot) {
    $g.DrawLine((New-Object System.Drawing.Pen($hot, 6)), 45, 30, 45, 101)
    $flag = @(
        [System.Drawing.Point]::new(47, 30), [System.Drawing.Point]::new(92, 39),
        [System.Drawing.Point]::new(78, 61), [System.Drawing.Point]::new(47, 56)
    )
    $g.FillPolygon((New-Object System.Drawing.SolidBrush($accent)), $flag)
    $g.DrawPolygon((New-Object System.Drawing.Pen($hot, 4)), $flag)
    $g.DrawLine((New-Object System.Drawing.Pen($hot, 5)), 31, 101, 76, 101)
}

function Draw-TargetGlyph($g, [System.Drawing.Color]$accent, [System.Drawing.Color]$hot) {
    $g.DrawEllipse((New-Object System.Drawing.Pen($accent, 5)), 31, 31, 66, 66)
    $g.DrawEllipse((New-Object System.Drawing.Pen($hot, 4)), 47, 47, 34, 34)
    $g.DrawLine((New-Object System.Drawing.Pen($hot, 4)), 64, 23, 64, 105)
    $g.DrawLine((New-Object System.Drawing.Pen($hot, 4)), 23, 64, 105, 64)
}

function Draw-CommandArrowGlyph($g, [System.Drawing.Color]$accent, [System.Drawing.Color]$hot) {
    $g.DrawLine((New-Object System.Drawing.Pen($hot, 8)), 29, 83, 82, 38)
    $arrow = @(
        [System.Drawing.Point]::new(82, 38), [System.Drawing.Point]::new(78, 63),
        [System.Drawing.Point]::new(105, 45)
    )
    $g.FillPolygon((New-Object System.Drawing.SolidBrush($accent)), $arrow)
    $g.DrawLine((New-Object System.Drawing.Pen($accent, 5)), 35, 96, 94, 96)
}

function Draw-NamedMotif($g, [string]$label, [System.Drawing.Color]$accent, [System.Drawing.Color]$hot) {
    $base = $label -replace "\d+$", ""
    switch -Regex ($base) {
        "^ForceSpark$" { Draw-SparkBurst $g $accent $hot; return $true }
        "^ForceLightning$" { Draw-LightningBolt $g $accent $hot; return $true }
        "^ForceDrain$" { Draw-DrainGlyph $g $accent $hot; return $true }
        "^FuryStance$" { Draw-RageGlyph $g $accent $hot; return $true }
        "^DevouringStrike$|^HungerOfTheDark$" { Draw-MawGlyph $g $accent $hot; return $true }
        "^CreepingTerror$" { Draw-TendrilsGlyph $g $accent $hot; return $true }
        "^ForceChoke$" { Draw-ChokeGlyph $g $accent $hot; return $true }
        "^NightmareField$" { Draw-NightmareGlyph $g $accent $hot; return $true }
        "^CollapseWill$" { Draw-CollapseWillGlyph $g $accent $hot; return $true }
        "^EclipseOfResolve$" { Draw-EclipseGlyph $g $accent $hot; return $true }
        "^WeakenResolve$" { Draw-CrackedShieldGlyph $g $accent $hot; return $true }
        "^GuardianWard$|^Shielding$|^DeflectorShield$|^RayshieldScreen$|^ReflectiveBarrier$|^DampeningField$" { Draw-TechShieldGlyph $g $accent $hot; return $true }
        "^Benevolence$|^ForceMend$|^PurifyingWave$|^Infusion$|^Innervate$|^HarmonicRestoration$" { Draw-HealingGlyph $g $accent $hot; return $true }
        "^Renewal$" { Draw-RenewalGlyph $g $accent $hot; return $true }
        "^Flash$" { Draw-FlashGlyph $g $accent $hot; return $true }
        "^ForceTouch$" { Draw-GripGlyph $g $accent $hot; return $true }
        "^MindTrick$|^PsychicCry$" { Draw-MindTrickGlyph $g $accent $hot; return $true }
        "^ForcePush$" { Draw-ForcePushGlyph $g $accent $hot; return $true }
        "^ForceLeap$|^ForceIntercept$" { Draw-ForceLeapGlyph $g $accent $hot; return $true }
        "MedKit|TreatmentKit|EmergencyTriage|EmergencyCocktail|Resuscitation|KoltoMist|Coagulant|Antitoxin|PainSuppressant|AdrenalStim|FocusStim|PulseRelay|EmergencySealant" { Draw-KitGlyph $g $accent $hot; return $true }
        "Grenade|Bomb|Toss|Detonator|RemoteCharge" { Draw-GrenadeGlyph $g $accent $hot; return $true }
        "Beacon" { Draw-BeaconGlyph $g $accent $hot; return $true }
        "Flamethrower|Incendiary|Fireburst" { Draw-FlameGlyph $g $accent $hot; return $true }
        "CryoSprayer|IceBreath" { Draw-FrostGlyph $g $accent $hot; return $true }
        "WristRocket" { Draw-RocketGlyph $g $accent $hot; return $true }
        "RailDart" { Draw-DartGlyph $g $accent $hot; return $true }
        "SonicBurst" { Draw-SonicGlyph $g $accent $hot; return $true }
        "Bite|Pounce|ApexBite" { Draw-BeastBiteGlyph $g $accent $hot; return $true }
        "Claw|RendingClaw" { Draw-BeastClawGlyph $g $accent $hot; return $true }
        "Howl|Roar" { Draw-RoarGlyph $g $accent $hot; return $true }
        "Hide|IronHide|RampartHide" { Draw-HideGlyph $g $accent $hot; return $true }
        "Standard|Rally|RousingShout|CourageousResolve|BreakMorale|CleanseOrder|Bolster" { Draw-RallyGlyph $g $accent $hot; return $true }
        "MarkTarget|PredatorsMark|ExposePrey|ExecutePrey" { Draw-TargetGlyph $g $accent $hot; return $true }
        "Command|Order|Formation|Coordinated|PressTheAttack|HoldTheLine|WatchfulPresence|FieldRecovery|PackRecovery" { Draw-CommandArrowGlyph $g $accent $hot; return $true }
    }

    return $false
}

function Draw-LevelPips($g, [int]$level, [System.Drawing.Color]$hot) {
    if ($level -lt 1) { return }
    $brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(230, $hot))
    for ($i = 0; $i -lt [Math]::Min($level, 5); $i++) {
        $g.FillEllipse($brush, 16 + ($i * 11), 108, 7, 7)
    }
}

function Draw-RankBadge($g, [int]$level, [System.Drawing.Color]$semantic) {
    if ($level -lt 1) { return }

    $rankLabel = [string]$level

    $badgeRect = [System.Drawing.RectangleF]::new(86, 84, 29, 29)
    $badgePath = New-RoundedRectanglePath $badgeRect.X $badgeRect.Y $badgeRect.Width $badgeRect.Height 5
    $badgeBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(238, 4, 5, 8))
    $badgePen = New-Object System.Drawing.Pen($semantic, 3)
    $g.FillPath($badgeBrush, $badgePath)
    $g.DrawPath($badgePen, $badgePath)

    $fontSize = if ($rankLabel.Length -le 1) { 22 } elseif ($rankLabel.Length -le 2) { 19 } elseif ($rankLabel.Length -le 3) { 15 } else { 13 }
    $font = New-Object System.Drawing.Font("Arial", $fontSize, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
    $format = New-Object System.Drawing.StringFormat
    $format.Alignment = [System.Drawing.StringAlignment]::Center
    $format.LineAlignment = [System.Drawing.StringAlignment]::Center

    $shadowBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(245, 0, 0, 0))
    $textBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 255, 255, 245))
    $shadowRect = [System.Drawing.RectangleF]::new($badgeRect.X + 1, $badgeRect.Y + 2, $badgeRect.Width, $badgeRect.Height)
    $g.DrawString($rankLabel, $font, $shadowBrush, $shadowRect, $format)
    $g.DrawString($rankLabel, $font, $textBrush, $badgeRect, $format)

    $font.Dispose()
    $format.Dispose()
    $shadowBrush.Dispose()
    $textBrush.Dispose()
    $badgeBrush.Dispose()
    $badgePen.Dispose()
    $badgePath.Dispose()
}

function Draw-VariantSigil($g, [int]$hash, [int]$row, [System.Drawing.Color]$hot) {
    $pen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(80, $hot), 2)
    for ($i = 0; $i -lt 5; $i++) {
        $start = ($hash + $row + ($i * 67)) % 360
        $g.DrawArc($pen, 20 + ($i * 2), 20 + ($i * 2), 88 - ($i * 4), 88 - ($i * 4), $start, 22)
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
            $bytes[$offset++] = 255
        }
    }
    [System.IO.File]::WriteAllBytes($path, $bytes)
}

function New-CombatIcon([string]$label, [int]$row, [string]$outPath, [int]$size = 64) {
    $hash = Get-StableHash "$label#$row"
    $kind = Get-IconKind $label
    $semanticCategory = Get-SemanticCategory $label
    $semantic = Get-SemanticColor $semanticCategory
    $palette = Get-KindPalette $kind $hash
    $dark = $palette[0]
    $accent = $palette[1]
    $hot = $palette[2]
    $level = 0
    if ($label -match "(\d+)$") { $level = [int]$Matches[1] }

    $large = New-Object System.Drawing.Bitmap 256, 256
    $g = [System.Drawing.Graphics]::FromImage($large)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
    $g.ScaleTransform(2, 2)
    $g.Clear([System.Drawing.Color]::FromArgb(255, 0, 0, 0))

    Draw-IconBackdrop $g $semantic $accent $hot $hash

    Invoke-InContentBounds $g {
        Draw-IllustrativeAccents $g $accent $hot $hash
        $usedNamedMotif = Draw-NamedMotif $g $label $accent $hot
        if (-not $usedNamedMotif) {
            switch ($kind) {
                "defense"  { Draw-Shield $g $accent $hot }
                "ranged"   { Draw-Blaster $g $accent $hot }
                "throwing" { Draw-Boomerang $g $accent $hot }
                "energy"   { Draw-Orb $g $accent $hot }
                "darkforce" { Draw-Orb $g $accent $hot }
                "lightforce" { Draw-Orb $g $accent $hot }
                "medical"  { Draw-KitGlyph $g $accent $hot }
                "tech"     { Draw-TechShieldGlyph $g $accent $hot }
                "beast"    { Draw-BeastClawGlyph $g $accent $hot }
                "command"  { Draw-RallyGlyph $g $accent $hot }
                "toxin"    { Draw-Toxin $g $accent $hot }
                "martial"  { Draw-Fist $g $accent $hot }
                "mobility" { Draw-Mobility $g $accent $hot }
                "stance"   { Draw-Stance $g $accent $hot }
                "support"  { Draw-Orb $g $accent $hot }
                default    { Draw-Sword $g $accent $hot }
            }
        }
    }

    $badgeRank = ""
    $manifestKey = Get-ManifestKey "Ability" $label
    if ($script:IconManifest.ContainsKey($manifestKey)) {
        $iconResRef = $script:IconManifest[$manifestKey].IconResRef.ToLowerInvariant()
        if ($script:RankBadgeByResRef.ContainsKey($iconResRef)) {
            $badgeRank = $script:RankBadgeByResRef[$iconResRef]
        }
    }

    if (![string]::IsNullOrWhiteSpace($badgeRank)) {
        Draw-RankBadge $g ([int]$badgeRank) $semantic
    }

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
$manifestPath = Resolve-Path $IconManifestPath
$script:IconManifestRows = @(Import-Csv -Path $manifestPath)
$script:IconManifest = Import-IconManifest $manifestPath
$script:RankBadgeByResRef = Get-RankBadgeMap $script:IconManifestRows
$lines = [System.Collections.Generic.List[string]]::new()
$lines.AddRange([System.IO.File]::ReadAllLines($featPath))
$generated = 0

if (![string]::IsNullOrWhiteSpace($SampleOutputPath)) {
    $resolvedOutput = if ([System.IO.Path]::IsPathRooted($SampleOutputPath)) { $SampleOutputPath } else { Join-Path (Get-Location).Path $SampleOutputPath }
    if (!(Test-Path -LiteralPath $resolvedOutput)) {
        New-Item -ItemType Directory -Path $resolvedOutput -Force | Out-Null
    }

    $requested = @{}
    foreach ($resrefValue in $SampleIconResRefs) {
        foreach ($resref in ([string]$resrefValue -split "[,;]")) {
            $trimmed = $resref.Trim()
            if (![string]::IsNullOrWhiteSpace($trimmed)) {
                $requested[$trimmed.ToLowerInvariant()] = $true
            }
        }
    }

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        if ($line.Trim().Length -eq 0) { continue }
        $parts = $line -split "\s+"
        if ($parts.Count -lt 5) { continue }
        $row = 0
        if (-not [int]::TryParse($parts[0], [ref]$row)) { continue }
        if ($row -lt $GeneratedFeatStart -or $row -gt $GeneratedFeatEnd) { continue }
        if ($parts[1] -eq "****" -or $parts[4] -eq "****") { continue }
        if ($requested.Count -gt 0 -and !$requested.ContainsKey($parts[4].ToLowerInvariant())) { continue }

        New-CombatIcon $parts[1] $row (Join-Path $resolvedOutput "$($parts[4]).tga") $IconSize
        $generated++
    }

    Write-Host "Generated $generated Combat Upgrade feat icon samples in $resolvedOutput."
    return
}

for ($i = 0; $i -lt $lines.Count; $i++) {
    $line = $lines[$i]
    if ($line.Trim().Length -eq 0) { continue }
    $parts = $line -split "\s+"
    if ($parts.Count -lt 5) { continue }
    $row = 0
    if (-not [int]::TryParse($parts[0], [ref]$row)) { continue }
    if ($row -lt $GeneratedFeatStart -or $row -gt $GeneratedFeatEnd) { continue }
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
    & $renameScript -Feat2daPath $Feat2daPath -IconPath $IconOutputPath -GeneratedFeatStart $GeneratedFeatStart -GeneratedFeatEnd $GeneratedFeatEnd
}

$formatScript = Join-Path $PSScriptRoot "FormatCombatUpgradeFeatRows.ps1"
if (Test-Path -LiteralPath $formatScript) {
    & $formatScript -Feat2daPath $Feat2daPath
}
