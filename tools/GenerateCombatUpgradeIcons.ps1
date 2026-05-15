param(
    [string]$Feat2daPath = "SWLOR_Haks\swlor2_2da\feat.2da",
    [string]$IconOutputPath = "SWLOR_Haks\swlor2_tga",
    [int]$IconSize = 64,
    [int]$GeneratedFeatStart = 2000,
    [int]$GeneratedFeatEnd = 2558
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Drawing

function Get-StableHash([string]$value) {
    $hash = [int64]17
    foreach ($ch in $value.ToCharArray()) {
        $hash = (($hash * 31) + [int][char]$ch) % 2147483647
    }

    return [int]$hash
}

function Get-IconKind([string]$label) {
    if ($label -match "Stance$") { return "stance" }
    if ($label -match "ForceSpark|ForceBody|ForceLightning|ForceDrain|ForceGrip|ForceChoke|ForceMaelstrom|MindShroud|Nightmare|Dominate|Collapse|Eclipse|Creeping|WeakenResolve|FractureFocus|SaberRend|ForceRage|DevouringStrike|HungerOfTheDark") { return "darkforce" }
    if ($label -match "Benevolence|Renewal|Clarity|Pacify|MindTrick|ComprehendSpeech|ForcePush|ForceLeap|ForceTouch|ForceMend|ForceSanctuary|GuardianWard|BastionOfLight|LastStandOfTheLight|CircleOfHarmony|PurifyingWave|Innervate|Infusion") { return "lightforce" }
    if ($label -match "MedKit|TreatmentKit|Kolto|Resuscitation|EmergencyTriage|EmergencySealant|Coagulant|Antitoxin|PainSuppressant|AdrenalStim|FocusStim|MaintenancePulse|EmergencyCocktail") { return "medical" }
    if ($label -match "Grenade|Beacon|RemoteCharge|Flamethrower|WristRocket|RailDart|CryoSprayer|SonicBurst|PowerCell|Shielding|Deflector|Hardlight|Dampening|OverloadBarrage|WeaponJam") { return "tech" }
    if ($label -match "Bite|Claw|Pounce|Howl|Roar|Hide|Beast|Prey|Predator|Apex|Pack|Rampage|Primal") { return "beast" }
    if ($label -match "Order|Command|Standard|Formation|Rally|WatchfulPresence|Coordinated|ChargeOrder|PressTheAttack|HoldTheLine|MarkTarget|BreakMorale|CleanseOrder|AuraOfCourage|Bolster") { return "command" }
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

function Draw-SaberRendGlyph($g, [System.Drawing.Color]$accent, [System.Drawing.Color]$hot) {
    $g.DrawLine((New-Object System.Drawing.Pen($hot, 10)), 32, 94, 90, 28)
    $g.DrawLine((New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(230, 255, 255, 255), 3)), 32, 94, 90, 28)
    $tear = New-Object System.Drawing.Pen($accent, 6)
    $g.DrawLine($tear, 75, 38, 98, 61)
    $g.DrawLine($tear, 63, 55, 91, 84)
    $g.DrawLine($tear, 50, 72, 73, 99)
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

function Draw-FractureFocusGlyph($g, [System.Drawing.Color]$accent, [System.Drawing.Color]$hot) {
    $g.DrawEllipse((New-Object System.Drawing.Pen($accent, 5)), 34, 34, 60, 60)
    $g.DrawLine((New-Object System.Drawing.Pen($accent, 4)), 64, 25, 64, 103)
    $g.DrawLine((New-Object System.Drawing.Pen($accent, 4)), 25, 64, 103, 64)
    $g.DrawLine((New-Object System.Drawing.Pen($hot, 5)), 51, 37, 66, 56)
    $g.DrawLine((New-Object System.Drawing.Pen($hot, 5)), 66, 56, 56, 75)
    $g.DrawLine((New-Object System.Drawing.Pen($hot, 5)), 56, 75, 78, 94)
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

function Draw-ClarityGlyph($g, [System.Drawing.Color]$accent, [System.Drawing.Color]$hot) {
    $crystal = @(
        [System.Drawing.Point]::new(64, 18), [System.Drawing.Point]::new(90, 47),
        [System.Drawing.Point]::new(78, 103), [System.Drawing.Point]::new(50, 103),
        [System.Drawing.Point]::new(38, 47)
    )
    $g.FillPolygon((New-Object System.Drawing.SolidBrush($accent)), $crystal)
    $g.DrawPolygon((New-Object System.Drawing.Pen($hot, 5)), $crystal)
    $g.DrawLine((New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(180,255,255,255), 3)), 64, 23, 64, 99)
}

function Draw-PacifyGlyph($g, [System.Drawing.Color]$accent, [System.Drawing.Color]$hot) {
    Draw-GripGlyph $g $accent $hot
    $g.DrawArc((New-Object System.Drawing.Pen($hot, 4)), 25, 23, 78, 77, 210, 120)
    $g.DrawArc((New-Object System.Drawing.Pen($hot, 3)), 17, 14, 94, 94, 215, 110)
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
    $g.FillPolygon((New-Object System.Drawing.SolidBrush($accent)), $body)
    $g.DrawPolygon((New-Object System.Drawing.Pen($hot, 5)), $body)
    $g.DrawLine((New-Object System.Drawing.Pen($hot, 6)), 37, 91, 23, 106)
}

function Draw-DartGlyph($g, [System.Drawing.Color]$accent, [System.Drawing.Color]$hot) {
    $g.DrawLine((New-Object System.Drawing.Pen($hot, 8)), 25, 84, 101, 43)
    $head = @([System.Drawing.Point]::new(102, 43), [System.Drawing.Point]::new(82, 38), [System.Drawing.Point]::new(92, 59))
    $g.FillPolygon((New-Object System.Drawing.SolidBrush($accent)), $head)
    $g.DrawLine((New-Object System.Drawing.Pen($accent, 5)), 34, 78, 23, 61)
}

function Draw-SonicGlyph($g, [System.Drawing.Color]$accent, [System.Drawing.Color]$hot) {
    $g.FillPie((New-Object System.Drawing.SolidBrush($accent)), 33, 49, 41, 31, 90, 180)
    foreach ($size in @(34, 54, 74)) {
        $g.DrawArc((New-Object System.Drawing.Pen($hot, 4)), 48, 64 - ($size / 2), $size, $size, 300, 120)
    }
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
        "^ForceBody$" { Draw-BodyAura $g $accent $hot; return $true }
        "^ForceLightning$" { Draw-LightningBolt $g $accent $hot; return $true }
        "^ForceDrain$" { Draw-DrainGlyph $g $accent $hot; return $true }
        "^ForceMaelstrom$" { Draw-VortexGlyph $g $accent $hot; return $true }
        "^SaberRend$" { Draw-SaberRendGlyph $g $accent $hot; return $true }
        "^ForceRage$" { Draw-RageGlyph $g $accent $hot; return $true }
        "^DevouringStrike$|^HungerOfTheDark$" { Draw-MawGlyph $g $accent $hot; return $true }
        "^CreepingTerror$" { Draw-TendrilsGlyph $g $accent $hot; return $true }
        "^ForceGrip$" { Draw-GripGlyph $g $accent $hot; return $true }
        "^ForceChoke$" { Draw-ChokeGlyph $g $accent $hot; return $true }
        "^MindShroud$" { Draw-ShroudedHeadGlyph $g $accent $hot; return $true }
        "^NightmareField$" { Draw-NightmareGlyph $g $accent $hot; return $true }
        "^DominateWeakMind$" { Draw-DominateMindGlyph $g $accent $hot; return $true }
        "^CollapseWill$" { Draw-CollapseWillGlyph $g $accent $hot; return $true }
        "^EclipseOfResolve$" { Draw-EclipseGlyph $g $accent $hot; return $true }
        "^WeakenResolve$" { Draw-CrackedShieldGlyph $g $accent $hot; return $true }
        "^FractureFocus$" { Draw-FractureFocusGlyph $g $accent $hot; return $true }
        "^GuardianWard$|^Shielding$|^DeflectorShield$|^HardlightScreen$|^ReflectiveBarrier$|^DampeningField$" { Draw-TechShieldGlyph $g $accent $hot; return $true }
        "^Benevolence$|^ForceMend$|^CircleOfHarmony$|^PurifyingWave$|^Infusion$|^Innervate$" { Draw-HealingGlyph $g $accent $hot; return $true }
        "^Renewal$" { Draw-RenewalGlyph $g $accent $hot; return $true }
        "^Clarity$" { Draw-ClarityGlyph $g $accent $hot; return $true }
        "^Pacify$|^ForceTouch$" { Draw-PacifyGlyph $g $accent $hot; return $true }
        "^MindTrick$|^PsychicCry$" { Draw-MindTrickGlyph $g $accent $hot; return $true }
        "^ComprehendSpeech$" { Draw-SpeechGlyph $g $accent $hot; return $true }
        "^ForcePush$" { Draw-ForcePushGlyph $g $accent $hot; return $true }
        "^ForceLeap$|^ForceIntercept$" { Draw-ForceLeapGlyph $g $accent $hot; return $true }
        "MedKit|TreatmentKit|EmergencyTriage|EmergencyCocktail|Resuscitation|KoltoMist|Coagulant|Antitoxin|PainSuppressant|AdrenalStim|FocusStim|MaintenancePulse|EmergencySealant" { Draw-KitGlyph $g $accent $hot; return $true }
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
        "Standard|Rally|RousingShout|AuraOfCourage|BreakMorale|CleanseOrder|Bolster" { Draw-RallyGlyph $g $accent $hot; return $true }
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

function Draw-UniqueMark($g, [int]$row, [System.Drawing.Color]$hot) {
    $brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(210, $hot))
    for ($i = 0; $i -lt 12; $i++) {
        if (($row -band (1 -shl $i)) -eq 0) {
            continue
        }

        $x = 103 + (($i % 4) * 4)
        $y = 102 + ([Math]::Floor($i / 4) * 4)
        $g.FillRectangle($brush, $x, $y, 2, 2)
    }
}

function Draw-VariantSigil($g, [int]$hash, [int]$row, [System.Drawing.Color]$hot) {
    $pen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(150, $hot), 3)
    $brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(175, $hot))
    for ($i = 0; $i -lt 4; $i++) {
        $angle = (($hash + $row + ($i * 83)) % 360) * [Math]::PI / 180
        $radius = 43 + (($hash + ($i * 7)) % 8)
        $x = [int](64 + [Math]::Cos($angle) * $radius)
        $y = [int](64 + [Math]::Sin($angle) * $radius)
        $g.FillEllipse($brush, $x - 4, $y - 4, 8, 8)
        $g.DrawLine($pen, 64, 64, $x, $y)
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
    $hash = Get-StableHash "$label#$row"
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

    Draw-VariantSigil $g $hash $row $hot
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

    Draw-LevelPips $g $level $hot
    Draw-UniqueMark $g $row $hot

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
