param(
    [string]$ManifestPath = "SWLOR.Game.Server\Readmes\GameplayIconManifest.csv",
    [string]$Feat2daPath = "SWLOR_Haks\swlor2_2da\feat.2da",
    [string]$Spells2daPath = "SWLOR_Haks\swlor2_2da\spells.2da",
    [string]$OutputPath = "output\imagegen\gpt2_icon_production",
    [int]$BatchSize = 10,
    [int]$Columns = 5,
    [int]$Rows = 2,
    [switch]$IncludeAll2daCustomIcons
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ($Columns -lt 1 -or $Rows -lt 1) {
    throw "Columns and Rows must be 1 or greater."
}

if ($BatchSize -lt 1) {
    throw "BatchSize must be 1 or greater."
}

if ($BatchSize -gt ($Columns * $Rows)) {
    throw "BatchSize $BatchSize exceeds the $Columns by $Rows grid capacity."
}

function ConvertTo-DisplayName([string]$value) {
    if ([string]::IsNullOrWhiteSpace($value)) {
        return ""
    }

    $name = $value
    $name = $name -replace "^ife_", ""
    $name = $name -replace "^ief_", ""
    $name = $name -replace "([a-z])([A-Z])", '$1 $2'
    $name = $name -replace "_", " "
    $name = $name -replace "\s+", " "
    return (Get-Culture).TextInfo.ToTitleCase($name.Trim().ToLowerInvariant())
}

function Get-Rank([string]$displayName, [string]$resref) {
    if ($displayName -match "\b([1-9][0-9]*)$") {
        return $Matches[1]
    }

    if ($displayName -match "\b(I|II|III|IV|V|VI|VII|VIII|IX|X)$") {
        switch ($Matches[1]) {
            "I" { return "1" }
            "II" { return "2" }
            "III" { return "3" }
            "IV" { return "4" }
            "V" { return "5" }
            "VI" { return "6" }
            "VII" { return "7" }
            "VIII" { return "8" }
            "IX" { return "9" }
            "X" { return "10" }
        }
    }

    return ""
}

function Get-SemanticCategory([string]$displayName, [string]$resref) {
    $value = "$displayName $resref".ToLowerInvariant()

    if ($value -match "food|rest|language|speech|detect|identify|travel|knowledge|craft|recipe|map|utility") {
        return "Utility"
    }

    if ($value -match "beacon|field|trap|charge|standard|bunker|mine|turret|deploy|placed|zone") {
        return "Deployable"
    }

    if ($value -match "stance|maneuver|roll|self|focus|centering|calming|body|form|mode|hide|evasive|skirmisher|sniper|deadeye|duelist|bastion") {
        return "Self"
    }

    if ($value -match "flash|sonic|stun|daze|fear|terror|pacify|choke|grip|hamstring|tranquil|sleep|jam|blind|silence|immobil|slow|bind|confus|mind|dominate|concussion|disorient") {
        return "Control"
    }

    if ($value -match "heal|benevolence|clarity|ward|guard|shield|armor|defense|defence|protection|resolve|antitoxin|cleanse|renew|mend|kolto|stim|coagulant|rally|bolster|restoration|sanctuary|bastion of light|light") {
        return "Beneficial"
    }

    return "Harmful"
}

function Get-SemanticColorName([string]$category) {
    switch ($category) {
        "Beneficial" { return "Beneficial green #54F67A" }
        "Harmful" { return "Harmful red #F05454" }
        "Self" { return "Self cyan-blue #4FC3FF" }
        "Control" { return "Control violet #B56CFF" }
        "Deployable" { return "Deployable amber #FFB84D" }
        "Utility" { return "Utility white-steel #DDE6F0" }
    }

    throw "Unknown semantic category '$category'."
}

function Get-SubjectDirection([string]$displayName, [string]$category, [string]$resref) {
    $value = "$displayName $resref".ToLowerInvariant()

    if ($value -match "food") { return "a clearly recognizable roasted drumstick or ration food item with polished highlights" }
    if ($value -match "rest") { return "a folded bedroll with a calm crescent glow and soft sleep motes, no letter text" }
    if ($value -match "flash") { return "a brilliant flash-blindness burst with prism shards and curved light streaks" }
    if ($value -match "sonic") { return "a compact sonic emitter core with layered contained shockwave rings" }
    if ($value -match "burn|fire|flame|incendiary|broil") { return "layered flames around a glowing ember shard with contained sparks" }
    if ($value -match "poison|toxic|venom|acid|ailment|disease|antitoxin") { return "a vial, toxin droplet, or antidote shield motif with clear liquid highlights" }
    if ($value -match "heal|mend|kolto|benevolence|renew|restoration") { return "a radiant heart-crystal, kolto vial, or restorative medical glow with gentle motes; no visible hands or fingers" }
    if ($value -match "guard|shield|ward|armor|defense|protection|bastion|sentinel|rampart|fortress") { return "a faceted shield, closed armored gauntlet plate with no visible fingers, or defensive energy barrier with polished metal facets" }
    if ($value -match "blaster|shot|round|sniper|deadeye|gun|volley|aim") { return "a detailed sci-fi blaster weapon, muzzle glint, or target reticle fragments" }
    if ($value -match "grenade|bomb|charge|rocket|dart|mine|trap") { return "a detailed sci-fi explosive, grenade, rocket, or deployable charge with contained energy accents" }
    if ($value -match "beacon|standard|field|zone|bunker|deploy") { return "a compact deployable device, banner, beacon, or field projector with contained signal light" }
    if ($value -match "force|mind|soul|spirit|terror|nightmare|choke|grip|dominate|pacify") { return "a glowing Force or mind-affecting sigil with faceted energy, wisps, and contained arcs" }
    if ($value -match "saber|blade|slash|strike|cleave|carve|vortex|cyclone|tempest|whirl") { return "a dynamic energy blade or melee weapon slash with layered sparks and contained motion arcs" }
    if ($value -match "bite|claw|beast|roar|howl|pounce|fang|predator|prey") { return "a beast fang, claw, roar mark, or animalistic strike rendered with sharp illustrated detail" }
    if ($value -match "order|command|rally|formation|morale|standard|bolster") { return "a command emblem, rallying signal, banner plate, or tactical mark with polished highlights" }
    if ($value -match "roll|evasive|dodge|step|leap|jump|maneuver|skirmish|flank") { return "a movement arc, boot, cloak, or evasive silhouette with crisp contained motion lines" }
    if ($value -match "language|speech|clarity|knowledge|detect|identify") { return "a clear utility symbol such as a glowing eye, scroll, speech rune, or crystal lens" }

    switch ($category) {
        "Beneficial" { return "a polished beneficial sigil with protective facets, warm glow, and clear restorative details" }
        "Control" { return "a polished control-effect sigil with restraint, disruption, or disorienting energy cues" }
        "Self" { return "a polished self-stance emblem with a closed armored stance plate, helmet mark, stance ring, or personal aura; avoid exposed fingers" }
        "Deployable" { return "a polished deployable device or field projector with contained signal accents" }
        "Utility" { return "a polished utility object or neutral support sigil with clean readable shape" }
        default { return "a polished harmful combat sigil, weapon strike, energy shard, or impact burst" }
    }
}

function Import-2daRows([string]$path) {
    $header = $null
    foreach ($line in Get-Content -LiteralPath $path) {
        $trim = $line.Trim()
        if ($trim.Length -eq 0 -or $trim.StartsWith("2DA")) {
            continue
        }

        if ($null -eq $header) {
            $header = $trim -split "\s+"
            continue
        }

        $parts = $trim -split "\s+"
        if ($parts.Count -lt 2) {
            continue
        }

        [pscustomobject]@{
            Header = $header
            Parts = $parts
        }
    }
}

function Get-2daValue([pscustomobject]$row, [string]$headerName) {
    $index = [array]::IndexOf($row.Header, $headerName)
    if ($index -lt 0) {
        return ""
    }

    $partIndex = $index + 1
    if ($row.Parts.Count -le $partIndex) {
        return ""
    }

    return $row.Parts[$partIndex]
}

function Get-RankFamilyKey([object]$row) {
    $key = [string]$row.Key
    if ([string]::IsNullOrWhiteSpace($key)) {
        $key = [string]$row.DisplayName
    }

    if ([string]::IsNullOrWhiteSpace($key)) {
        $key = [string]$row.IconResRef
    }

    $key = $key -replace "StatusEffect$", ""
    $rank = ([string]$row.Rank).Trim()

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

function Add-RankBadgeMetadata([object[]]$rows) {
    $rankValuesByFamily = @{}
    foreach ($row in $rows) {
        if ([string]$row.Type -eq "Ability") {
            continue
        }

        $rank = ([string]$row.Rank).Trim()
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

    foreach ($row in $rows) {
        $rank = ([string]$row.Rank).Trim()
        $badgeRank = ""
        if ([string]$row.Type -ne "Ability" -and ![string]::IsNullOrWhiteSpace($rank)) {
            $family = Get-RankFamilyKey $row
            if ($rankValuesByFamily.ContainsKey($family) -and $rankValuesByFamily[$family].Count -gt 1) {
                $badgeRank = $rank
            }
        }

        $row | Add-Member -NotePropertyName RankBadge -NotePropertyValue $badgeRank -Force
    }
}

function Add-Target([System.Collections.Generic.List[object]]$targets, [hashtable]$seen, [string]$type, [string]$key, [string]$displayName, [string]$category, [string]$rank, [string]$resref, [string]$sourcePath) {
    if ([string]::IsNullOrWhiteSpace($resref) -or $resref -eq "****" -or $resref -eq "*****") {
        return
    }

    if ($seen.ContainsKey($resref)) {
        return
    }

    if ($resref.Length -gt 16) {
        throw "Icon resref '$resref' exceeds NWN's 16 character limit."
    }

    if ([string]::IsNullOrWhiteSpace($displayName) -or $displayName -eq "****") {
        $displayName = ConvertTo-DisplayName $key
    }

    if ([string]::IsNullOrWhiteSpace($displayName)) {
        $displayName = ConvertTo-DisplayName $resref
    }

    if ([string]::IsNullOrWhiteSpace($category)) {
        $category = Get-SemanticCategory $displayName $resref
    }

    if ([string]::IsNullOrWhiteSpace($rank)) {
        $rank = Get-Rank $displayName $resref
    }

    $seen[$resref] = $true
    $targets.Add([pscustomobject]@{
        Type = $type
        Key = $key
        DisplayName = $displayName
        SemanticCategory = $category
        SemanticColor = Get-SemanticColorName $category
        Rank = $rank
        IconResRef = $resref
        SubjectDirection = Get-SubjectDirection $displayName $category $resref
        SourcePath = $sourcePath
    }) | Out-Null
}

$resolvedOutput = if ([System.IO.Path]::IsPathRooted($OutputPath)) { $OutputPath } else { Join-Path (Get-Location).Path $OutputPath }
$batchDirectory = Join-Path $resolvedOutput "prompts"
New-Item -ItemType Directory -Path $batchDirectory -Force | Out-Null

$targets = [System.Collections.Generic.List[object]]::new()
$seen = @{}

foreach ($row in Import-Csv -Path $ManifestPath) {
    Add-Target $targets $seen $row.Type $row.Key $row.DisplayName $row.SemanticCategory $row.Rank $row.IconResRef $row.SourcePath
}

if ($IncludeAll2daCustomIcons) {
    foreach ($spec in @(
        @{ Type = "Feat"; Path = $Feat2daPath; IconHeader = "ICON"; LabelHeader = "LABEL" },
        @{ Type = "Spell"; Path = $Spells2daPath; IconHeader = "IconResRef"; LabelHeader = "Label" }
    )) {
        foreach ($row in Import-2daRows $spec.Path) {
            $resref = Get-2daValue $row $spec.IconHeader
            if ($resref -notlike "ife_*") {
                continue
            }

            $label = Get-2daValue $row $spec.LabelHeader
            Add-Target $targets $seen $spec.Type $label (ConvertTo-DisplayName $label) "" "" $resref $spec.Path
        }
    }
}

$targetPath = Join-Path $resolvedOutput "icon_targets.csv"
$sortedTargets = @($targets | Sort-Object IconResRef)
Add-RankBadgeMetadata $sortedTargets
$sortedTargets | Export-Csv -Path $targetPath -NoTypeInformation

$promptHeader = @"
Create a single $Columns-by-$Rows sprite sheet containing exactly $BatchSize separate square SWLOR gameplay icons.

Use GPT Image 2 generation style: polished high-end fantasy/RPG ability icons, painterly highlights, shaded edges, faceted glows, layered detail, clear silhouettes, readable when reduced to 32x32.

Strict sheet rules:
- The output is a $Columns columns by $Rows rows sprite sheet.
- Each tile is one complete rounded-square game icon.
- Tiles must be equal size, edge-to-edge or with perfectly even minimal gutters, with no labels outside the icons.
- Each icon has a dark solid-to-subtle-radial-gradient background, not horizontal gray block bands.
- Keep all central art, glows, particles, waves, projectiles, and highlights inside the icon frame/border.
- Use a consistent frame, border thickness, and background treatment across all $BatchSize icons.
- Exact semantic frame colors are stamped later by the SWLOR import tool at final 32x32 size. The generated source art may include a matching frame, but the final category color must come from the import tool.
- Do not draw rank badges or numbers. Ability icons do not use rank badges; the SWLOR import tool adds any remaining non-ability badges later at final 32x32 size for readability.
- Leave the bottom-right corner visually calm and uncluttered on every icon.
- No text labels, filenames, watermark, letters, words, or numbers.
- Simplify small details for 32x32 readability: use bold silhouettes, strong contrast, and fewer tiny particles.
- Anatomy validation: if any human, humanoid, beast, or creature appendage is visible, it must be anatomically coherent. No extra fingers, missing fingers, fused fingers, malformed hands, broken claws, impossible wings, or incoherent tails. Prefer closed armored gauntlets, paws, claws, silhouettes, or symbolic emblems when exposed fingers are not essential.
- Avoid primitive placeholder geometry, flat pictograms, generic blobs, random corner pixels, noisy artifacts, cropped symbols, generated numbers, generated text, malformed appendages, and artwork extending outside the border.

Icons in reading order:
"@

$batchCount = [Math]::Ceiling($sortedTargets.Count / [double]$BatchSize)
for ($batchIndex = 0; $batchIndex -lt $batchCount; $batchIndex++) {
    $start = $batchIndex * $BatchSize
    $batchTargets = @($sortedTargets | Select-Object -Skip $start -First $BatchSize)
    $lines = [System.Collections.Generic.List[string]]::new()
    $lines.Add($promptHeader)

    for ($i = 0; $i -lt $batchTargets.Count; $i++) {
        $target = $batchTargets[$i]
        if ([string]::IsNullOrWhiteSpace($target.Rank)) {
            $rankText = "unranked"
        }
        elseif ($target.Type -eq "Ability") {
            $rankText = "ability rank $($target.Rank); do not draw a rank number or badge"
        }
        elseif ([string]::IsNullOrWhiteSpace($target.RankBadge)) {
            $rankText = "single-level rank $($target.Rank); do not draw a rank number or badge"
        }
        else {
            $rankText = "rank $($target.Rank) in a multi-rank family, but do not draw the rank number or badge; leave the bottom-right corner clear for a later tool-stamped badge"
        }

        $lines.Add(("{0}. {1}, {2}, {3}: {4}. Make it visually unique from every other icon." -f ($i + 1), $target.DisplayName, $target.SemanticColor, $rankText, $target.SubjectDirection))
    }

    if ($batchTargets.Count -lt $BatchSize) {
        for ($i = $batchTargets.Count; $i -lt $BatchSize; $i++) {
            $lines.Add(("{0}. Empty filler tile: render a neutral dark rounded square placeholder with no symbol and no badge." -f ($i + 1)))
        }
    }

    $promptPath = Join-Path $batchDirectory ("batch_{0:D4}.txt" -f ($batchIndex + 1))
    [System.IO.File]::WriteAllText($promptPath, ($lines -join [Environment]::NewLine), [System.Text.Encoding]::UTF8)
}

$summary = [pscustomobject]@{
    TargetCount = $targets.Count
        BatchSize = $BatchSize
        Columns = $Columns
        Rows = $Rows
    BatchCount = $batchCount
    TargetPath = $targetPath
    PromptDirectory = $batchDirectory
}

$summary | ConvertTo-Json
