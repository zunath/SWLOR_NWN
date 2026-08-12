param(
    [string]$ManifestPath = "SWLOR.Game.Server\Readmes\GameplayIconManifest.csv",
    [string]$Feat2daPath = "SWLOR_Haks\sw_2da\feat.2da",
    [string]$Spells2daPath = "SWLOR_Haks\sw_2da\spells.2da",
    [string]$IconPath = "SWLOR_Haks\sw_ability",
    [string]$StatusIconSourcePath = "SWLOR_Haks\sw_ability_source",
    [string]$EffectIcons2daPath = "SWLOR_Haks\sw_2da\effecticons.2da",
    [string]$EffectIconTypePath = "SWLOR.NWN.API\NWScript\Enum\EffectIconType.cs",
    [string]$StatusEffectPath = "SWLOR.Game.Server\Feature\StatusEffectDefinition",
    [string]$TlkJsonPath = "SWLOR_Haks\sw_tlk\sw_tlk.tlk.json",
    [int]$GeneratedFeatStart = 2000,
    [int]$GeneratedFeatEnd = 2898,
    [int]$CustomFeatStart = 1116,
    [int]$CustomSpellStart = 1000,
    [int]$StatusEffectIconStart = 141,
    [int]$IconSize = 32,
    [switch]$RefreshManifest,
    [switch]$GenerateIcons,
    [switch]$UpdateStatusEffectCode,
    [switch]$AuditOnly,
    [string]$SampleOutputPath = "",
    [string[]]$SampleIconResRefs = @()
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Drawing

$ApprovedCategories = @("Beneficial", "Harmful", "Self", "Control", "Deployable", "Passive", "Utility")
$GeneratedEnumStartMarker = "        // Custom status effect icons"
$GeneratedEnumEndMarker = "        // End custom status effect icons"
$CustomTlkOffset = 16777216
$IconStopWords = @("a", "an", "and", "of", "the", "status", "effect")
$RomanNumerals = @{
    I = 1; II = 2; III = 3; IV = 4; V = 5
    VI = 6; VII = 7; VIII = 8; IX = 9; X = 10
}
$IconWordAliases = @{
    Absolute = "abs"; Adamantine = "adam"; Adhesive = "adh"; Ailment = "ail"; Resistance = "res"
    Adrenal = "adr"; Stim = "stim"; Alpha = "alp"; Rhythm = "rhy"; Beast = "bst"; Antitoxin = "antitox"
    Assault = "aslt"; Aura = "aura"; Courage = "crg"; Courageous = "crgs"; Resolve = "res"
    Bastion = "bast"; Light = "lght"; Stance = "stnc"; Berserker = "bers"; Blazing = "blaz"
    Spikes = "spk"; Bleed = "bleed"; Blind = "blind"; Blood = "blood"; Weapon = "wpn"; Bolster = "bolst"
    Attack = "atk"; Bombardier = "bomb"; Breach = "brch"; Brutal = "brut"; Burn = "burn"; Calming = "calm"
    Centering = "cent"; Charge = "chrg"; Order = "ord"; Circle = "circ"; Harmony = "harm"
    Cleanse = "clns"; Coagulant = "coag"; Cobra = "cobra"; Comprehend = "comp"; Speech = "spch"
    Conduit = "cond"; Coordinated = "coord"; Focus = "foc"; Covering = "cov"; Claws = "claw"; Strike = "strk"
    Creeping = "creep"; Terror = "terr"; Damage = "dmg"; Crippled = "crip"; Crippling = "crip"; Defense = "def"
    Defensive = "def"; Crusher = "crush"; Crushing = "crush"; Blow = "blow"; Cyclone = "cycl"; Dampening = "damp"
    Field = "fld"; Dazed = "dazed"; Deadeye = "eye"; Deadly = "dead"; Precision = "prec"; Debilitating = "debil"
    Decisive = "decis"; Command = "cmd"; Decoy = "decoy"; Deflecting = "defl"; Deflective = "defl"; Presence = "pres"
    Disease = "dis"; Vitality = "vit"; Penalty = "pen"; Disoriented = "disor"; Disruption = "disrp"
    Distracting = "distr"; Feint = "feint"; Dominate = "dom"; Weak = "weak"; Mind = "mind"; Duelists = "duel"
    Duelist = "duel"; Challenge = "chal"; Self = "self"; Eclipse = "ecl"; Emergency = "emrg"; Bunker = "bnkr"
    Sealant = "seal"; Essence = "ess"; Drain = "drn"; Evasive = "evas"; Combat = "cmbt"; Maneuver = "mnvr"
    Exhausted = "exh"; Exposed = "exp"; Expose = "xpose"; Point = "pt"; Ferocity = "feroc"; Final = "final"
    Form = "form"; Flanking = "flank"; Barrage = "barr"; Flash = "flash"; Grenade = "grnd"; Focused = "foc"
    Foggy = "fog"; Food = "food"; Force = "force"; Forcebane = "fbane"; Body = "body"; Bonded = "bond"
    Capacitor = "cap"; Choke = "choke"; Erosion = "eros"; Intercept = "intc"; Lens = "lens"; Rage = "rage"
    Sanctuary = "sanct"; Spark = "spark"; Suppression = "sup"; Warding = "ward"; Fortress = "fort"
    Fractured = "fract"; Fracture = "fract"; Freezing = "frz"; Might = "might"; Guarded = "grded"; Channel = "chan"
    Guardians = "guard"; Guardian = "guard"; Guarding = "grd"; Guard = "grd"; Wrath = "wrath"; Roar = "roar"
    Step = "step"; Gunfighter = "gunf"; Gunslinger = "guns"; Hamstring = "hamstr"; Hasten = "hast"
    Hemorrhage = "hem"; Hobble = "hob"; Hold = "hold"; Line = "line"; Hunger = "hunger"; Dark = "dark"
    Immobilized = "immob"; Impenetrable = "impen"; Improved = "imp"; Attentiveness = "attn"
    Incapacitate = "incap"; Infinite = "inf"; Invincible = "invin"; Iron = "iron"; Carapace = "carap"
    Hide = "hide"; Wall = "wall"; Kill = "kill"; Zone = "zone"; Knockdown = "knock"; Kolto = "kolto"
    Mist = "mist"; Healing = "heal"; Last = "last"; Stand = "stand"; Marked = "mark"; Marking = "mark"
    Mark = "mark"; Target = "tgt"; Predators = "pred"; Predator = "pred"; Shroud = "shrd"; Nightmare = "night"
    Pacification = "pac"; Pain = "pain"; Suppressant = "sup"; Perceptive = "perc"
    Pet = "pet"; Poison = "psn"; Power = "pwr"; Cell = "cell"; Rush = "rush"; Press = "press"; Primal = "prim"
    Overrun = "ovrn"; Psychic = "psy"; Cry = "cry"; Rallying = "rally"; Standard = "std"; Rampart = "ramp"
    Rayshield = "raysh"; Screen = "scr"; Reflective = "refl"; Barrier = "bar"; Regenerative = "regen"
    Rest = "rest"; Rousing = "rous"; Shout = "shout"; Sentinel = "sent"; Shadow = "shdw"; Shelter = "shel"
    Shielding = "shld"; Shield = "shld"; Shock = "shock"; Skirmisher = "skirm"; Slug = "slug"; Shake = "shake"
    Smoke = "smoke"; Bomb = "bomb"; Snap = "snap"; Roll = "roll"; Sniper = "snip"; Sonic = "sonic"; Burst = "brst"
    Soothing = "sooth"; Soul = "soul"; Ascension = "asc"; Devourer = "dev"; Sacrifice = "sac"; Storm = "storm"
    Split = "split"; Spotter = "spot"; Steady = "steady"; Formation = "form"; Stunned = "stun"; Subdual = "sub"
    Sunder = "sunder"; Sweeping = "sweep"; Taunting = "taunt"; Tempest = "temp"; Terrified = "fear"
    Toxic = "tox"; Toxin = "toxin"; Tranquilized = "tranq"; Treatment = "treat"; Kit = "kit"; Triage = "triage"
    Protocol = "prot"; Twin = "twin"; Unbreakable = "unbrk"; Unmoving = "unmove"; Untouchable = "untch"
    Instinct = "inst"; Vital = "vital"; Vulnerable = "vuln"; Watchful = "watch"; Weakened = "weak"
    Weaken = "weaken"; Whirling = "whirl"
    # Vowel-stripping would reduce these to unreadable fragments ("Overload" -> "vrld"), so they
    # carry explicit abbreviations that keep the leading sound.
    Overload = "ovrld"; Apex = "apex"; Collapse = "cllps"; Sustain = "sstn"; Warden = "wrdn"
    Mandate = "mndt"; Canister = "cnstr"; Sweep = "swp"; Tempo = "tempo"
    Butchers = "btchrs"; Stealth = "stlth"
}

function Resolve-RepoPath([string]$path) {
    return (Resolve-Path -Path $path).Path
}

function ConvertTo-Base36([int]$value) {
    $digits = "0123456789abcdefghijklmnopqrstuvwxyz"
    if ($value -eq 0) { return "0" }

    $remaining = [Math]::Abs($value)
    $result = ""
    while ($remaining -gt 0) {
        $index = [int]($remaining % 36)
        $result = $digits[$index] + $result
        $remaining = [int][Math]::Floor($remaining / 36)
    }

    return $result
}

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

function Import-ExistingManifest([string]$path) {
    $map = @{}
    if (!(Test-Path -LiteralPath $path)) {
        return $map
    }

    foreach ($row in Import-Csv -Path $path) {
        if ([string]::IsNullOrWhiteSpace($row.Type) -or [string]::IsNullOrWhiteSpace($row.Key)) {
            continue
        }

        $map[(Get-ManifestKey $row.Type $row.Key)] = $row
    }

    return $map
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
        if ((Get-OptionalProperty $row "Type") -ne "StatusEffect") {
            continue
        }

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
        if ((Get-OptionalProperty $row "Type") -ne "StatusEffect") {
            continue
        }

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

function Get-PreservedCategory([hashtable]$existing, [string]$type, [string]$key, [string]$fallback) {
    $manifestKey = Get-ManifestKey $type $key
    if ($existing.ContainsKey($manifestKey) -and ![string]::IsNullOrWhiteSpace($existing[$manifestKey].SemanticCategory)) {
        return $existing[$manifestKey].SemanticCategory
    }

    # A row's Type can move between refreshes (an ability whose feat row falls outside the generated
    # range is rediscovered as a custom Feat, and vice versa). The Type-qualified key misses in that
    # case, which would silently discard a hand-corrected category and replace it with the regex
    # guess. Fall back to the name alone so a deliberate category survives a Type change.
    $row = Get-ManifestRowByKey $existing $key
    if ($null -ne $row -and ![string]::IsNullOrWhiteSpace($row.SemanticCategory)) {
        return $row.SemanticCategory
    }

    return $fallback
}

# Looks up a manifest row by name across every Type. Returns $null when the name is absent or
# ambiguous (the same name under two Types), since a guess would be worse than the derived default.
function Get-ManifestRowByKey([hashtable]$existing, [string]$key) {
    $suffix = "|$($key.ToLowerInvariant())"
    $matched = @()
    foreach ($manifestKey in $existing.Keys) {
        if ($manifestKey.EndsWith($suffix)) {
            $matched += $existing[$manifestKey]
        }
    }

    if ($matched.Count -eq 1) {
        return $matched[0]
    }

    return $null
}

# The Force alignment gem is owned by UpdateFeatSpellIconBorders.ps1, which reads the manifest's
# Alignment column as its source of truth. This script never derives that value, so it must carry the
# existing one through on a refresh; otherwise Export-Csv drops the column and every gem assignment
# is silently lost.
function Get-PreservedAlignment([hashtable]$existing, [string]$type, [string]$key) {
    $manifestKey = Get-ManifestKey $type $key
    if ($existing.ContainsKey($manifestKey)) {
        return (Get-OptionalProperty $existing[$manifestKey] "Alignment")
    }

    $row = Get-ManifestRowByKey $existing $key
    if ($null -ne $row) {
        return (Get-OptionalProperty $row "Alignment")
    }

    return ""
}

function Get-RankFromText([string]$text) {
    if ([string]::IsNullOrWhiteSpace($text)) {
        return $null
    }

    if ($text -match "(\d+)$") {
        return [int]$Matches[1]
    }

    if ($text -match "(\d+)(?=[A-Z][A-Za-z]*$)") {
        return [int]$Matches[1]
    }

    if ($text -match "\b(I|II|III|IV|V|VI|VII|VIII|IX|X)$") {
        return $RomanNumerals[$Matches[1]]
    }

    return $null
}

function Test-CustomStrRef([string]$value) {
    $number = 0
    return [int]::TryParse($value, [ref]$number) -and $number -ge $CustomTlkOffset
}

function Import-2daRows([string]$path) {
    $lines = @(Get-Content -Path $path | Where-Object { ![string]::IsNullOrWhiteSpace($_) })
    if ($lines.Count -lt 2) {
        return @()
    }

    $headers = $lines[1].Trim() -split "\s+"
    $rows = @()
    for ($i = 2; $i -lt $lines.Count; $i++) {
        $parts = $lines[$i].Trim() -split "\s+"
        if ($parts.Count -lt ($headers.Count + 1)) {
            continue
        }

        $rowNumber = 0
        if (![int]::TryParse($parts[0], [ref]$rowNumber)) {
            continue
        }

        $row = [ordered]@{
            Row = $rowNumber
        }

        for ($column = 0; $column -lt $headers.Count; $column++) {
            $row[$headers[$column]] = $parts[$column + 1]
        }

        $rows += [pscustomobject]$row
    }

    return $rows
}

function Test-DynamicShipModulePlaceholder([string]$label, [string]$icon) {
    return $label -match "^ShipModule(?:[1-9]|[12][0-9]|30)$" -and
        $icon -match "^ife_sm(?:[1-9]|[12][0-9]|30)$"
}

function Test-OpaqueGameplayIconResRef([pscustomobject]$entry) {
    $resref = (Get-OptionalProperty $entry "IconResRef").Trim()
    if ([string]::IsNullOrWhiteSpace($resref)) {
        return $false
    }

    if (($entry.Type -eq "Ability" -or $entry.Type -eq "Feat" -or $entry.Type -eq "Spell") -and
        $resref.StartsWith("ife_", [System.StringComparison]::OrdinalIgnoreCase)) {
        $body = $resref.Substring(4)
        if ($entry.Type -eq "Ability" -and $body -match "\d[a-z0-9]{2,}$") {
            return $true
        }

        if ($entry.Type -eq "Feat" -and
            $entry.Key.EndsWith("Trait", [System.StringComparison]::Ordinal) -and
            $body -match "\d") {
            return $true
        }
    }

    return $false
}

function Get-FeatSpellSemanticCategory([string[]]$labels) {
    $label = ($labels | Where-Object { ![string]::IsNullOrWhiteSpace($_) }) -join " "

    if ($label -match "PropertyMenu|OpenRestMenu|Rest\b|Rename|ChatCommand|Tame|CallBeast|Sniff|Comprehend|Speech|Language|Travel|Dash|Reward|Treasure|Credit|PetFood|Food") {
        return "Utility"
    }

    if ($label -match "\b[A-Za-z0-9]+Trait\b") {
        return "Passive"
    }

    if ($label -match "Blueprint|Recipe|Harvest|Refin|Scaveng|Management|Module|Research|Training|Mastery|StimPacks|HardLook|Craft|Assembly|Networking|Projects|Upkeep|GuildRelations|CityManagement|Starships") {
        return "Passive"
    }

    return Get-AbilitySemanticCategory $label
}

function Get-CustomFeatSpellRows([object[]]$abilityRows, [hashtable]$existing) {
    $iconDirectory = Resolve-RepoPath $IconPath
    $coveredIconResRefs = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($ability in $abilityRows) {
        [void]$coveredIconResRefs.Add($ability.IconResRef)
    }

    $groupsByIcon = @{}

    foreach ($row in Import-2daRows (Resolve-RepoPath $Feat2daPath)) {
        $label = (Get-OptionalProperty $row "LABEL").Trim()
        $icon = (Get-OptionalProperty $row "ICON").Trim()
        if ([string]::IsNullOrWhiteSpace($label) -or
            $label -eq "****" -or
            $label -eq "DELETED" -or
            [string]::IsNullOrWhiteSpace($icon) -or
            $icon -eq "****" -or
            $coveredIconResRefs.Contains($icon)) {
            continue
        }

        if (Test-DynamicShipModulePlaceholder $label $icon) {
            continue
        }

        if ($row.Row -lt $CustomFeatStart -and
            !(Test-CustomStrRef (Get-OptionalProperty $row "FEAT")) -and
            !(Test-CustomStrRef (Get-OptionalProperty $row "DESCRIPTION"))) {
            continue
        }

        $isPassiveTrait = $label.EndsWith("Trait", [System.StringComparison]::Ordinal)
        $iconFile = Join-Path $iconDirectory "$icon.tga"
        if (!(Test-Path -LiteralPath $iconFile) -and !$isPassiveTrait) {
            continue
        }

        $key = $icon.ToLowerInvariant()
        if (!$groupsByIcon.ContainsKey($key)) {
            $groupsByIcon[$key] = [pscustomobject]@{
                Type = "Feat"
                HasActiveReference = $false
                IconResRef = $icon
                Labels = [System.Collections.Generic.List[string]]::new()
                SourcePath = $Feat2daPath
            }
        }

        if ((Get-OptionalProperty $row "SPELLID") -ne "****" -or (Get-OptionalProperty $row "TARGETSELF") -eq "1") {
            $groupsByIcon[$key].HasActiveReference = $true
        }

        $groupsByIcon[$key].Labels.Add($label) | Out-Null
    }

    foreach ($row in Import-2daRows (Resolve-RepoPath $Spells2daPath)) {
        $label = (Get-OptionalProperty $row "Label").Trim()
        $icon = (Get-OptionalProperty $row "IconResRef").Trim()
        if ([string]::IsNullOrWhiteSpace($label) -or
            $label -eq "****" -or
            $label -eq "DELETED" -or
            [string]::IsNullOrWhiteSpace($icon) -or
            $icon -eq "****" -or
            $coveredIconResRefs.Contains($icon)) {
            continue
        }

        if ($row.Row -lt $CustomSpellStart -and
            !(Test-CustomStrRef (Get-OptionalProperty $row "Name")) -and
            !(Test-CustomStrRef (Get-OptionalProperty $row "SpellDesc"))) {
            continue
        }

        $iconFile = Join-Path $iconDirectory "$icon.tga"
        if (!(Test-Path -LiteralPath $iconFile)) {
            continue
        }

        $key = $icon.ToLowerInvariant()
        if (!$groupsByIcon.ContainsKey($key)) {
            $groupsByIcon[$key] = [pscustomobject]@{
                Type = "Spell"
                HasActiveReference = $true
                IconResRef = $icon
                Labels = [System.Collections.Generic.List[string]]::new()
                SourcePath = $Spells2daPath
            }
        }
        elseif ($groupsByIcon[$key].SourcePath -notmatch [regex]::Escape($Spells2daPath)) {
            $groupsByIcon[$key].SourcePath = "$($groupsByIcon[$key].SourcePath);$Spells2daPath"
        }

        $groupsByIcon[$key].Labels.Add($label) | Out-Null
    }

    $rows = @()
    foreach ($group in $groupsByIcon.Values) {
        $labels = @($group.Labels | Select-Object -Unique)
        if ($labels.Count -eq 0) {
            continue
        }

        $key = $labels[0]
        $type = $group.Type
        $category = Get-PreservedCategory $existing $type $key (Get-FeatSpellSemanticCategory $labels)
        $rows += [pscustomobject]@{
            Type = $type
            Key = $key
            DisplayName = $key
            SemanticCategory = $category
            Rank = Get-RankFromText $key
            IconResRef = $group.IconResRef
            SourcePath = $group.SourcePath
            Alignment = Get-PreservedAlignment $existing $type $key
        }
    }

    return $rows | Sort-Object Type, Key
}

function Split-IconWords([string]$text) {
    $clean = $text -replace "'", "" -replace "[^A-Za-z0-9]+", " "
    $matches = [regex]::Matches($clean, "[A-Z]+(?=[A-Z][a-z]|\d|$)|[A-Z]?[a-z]+|\d+")
    return @(
        $matches |
            ForEach-Object { $_.Value } |
            Where-Object { $_ -and ($IconStopWords -notcontains $_.ToLowerInvariant()) }
    )
}

function ConvertTo-IconChunk([string]$word) {
    if ($IconWordAliases.ContainsKey($word)) {
        return $IconWordAliases[$word]
    }

    $lower = $word.ToLowerInvariant()
    $withoutVowels = $lower -replace "[aeiou]", ""
    if ($withoutVowels.Length -ge 3) {
        return $withoutVowels
    }

    return $lower.Substring(0, [Math]::Min(4, $lower.Length))
}

function Get-IconWordsWithoutRank([string]$text) {
    return @(
        Split-IconWords $text |
            Where-Object {
                $_ -notmatch "^\d+$" -and
                -not $RomanNumerals.ContainsKey($_.ToUpperInvariant())
            }
    )
}

function ConvertTo-PascalIconLabel([object[]]$words) {
    return (($words | ForEach-Object {
        $word = [string]$_
        if ([string]::IsNullOrWhiteSpace($word)) {
            return ""
        }

        if ($word -match "^\d+$") {
            return $word
        }

        $lower = $word.ToLowerInvariant()
        return $lower.Substring(0, 1).ToUpperInvariant() + $lower.Substring(1)
    }) -join "")
}

function Get-EffectIconLabel([pscustomobject]$entry) {
    $displayWords = @(Get-IconWordsWithoutRank $entry.DisplayName)
    $classWords = @(Get-IconWordsWithoutRank ($entry.Key -replace "StatusEffect$", ""))
    $words = @()
    if ($displayWords.Count -gt 0) {
        $words += $displayWords
    }
    else {
        $words += $classWords
    }

    foreach ($extra in @("Self", "Beast", "Slow", "Damage", "Penalty", "Healing")) {
        if (($classWords -contains $extra) -and ($words -notcontains $extra)) {
            $words += $extra
        }
    }

    if ($words.Count -eq 0) {
        $words = @(Get-IconWordsWithoutRank ($entry.Key -replace "StatusEffect$", ""))
    }

    $label = ConvertTo-PascalIconLabel $words
    if ([string]::IsNullOrWhiteSpace($label)) {
        return ($entry.Key -replace "StatusEffect$", "")
    }

    if ($entry.Rank) {
        $label = "$label$($entry.Rank)"
    }

    return $label
}

function Get-AbilitySemanticCategory([string]$label) {
    # Disruption Field is an instant area silence, not a placed object, so the "Field" in its
    # name must not pull it into Deployable. Its player-facing intent is control.
    if ($label -match "DisruptionField") {
        return "Control"
    }

    if ($label -match "Beacon|Field|Standard|Bunker|RemoteCharge|Killzone|KillZone|DampeningField|IncendiaryField|EmergencyBunker") {
        return "Deployable"
    }

    if ($label -match "(Stance|Form)\d*$|SoulDevourer|BlazingSpikes|ToxicRush|SnapRoll|EvasiveManeuver|ShadowStep|Dash") {
        return "Self"
    }

    if ($label -match "Stun|Daze|Dazed|Disorient|Immobil|Hobble|Hamstring|Knock|Slow|Mind|Nightmare|Choke|WeaponJam|Flash|Concussion|Sonic|Tranquil|Terror|Fear|Confuse|CollapseWill") {
        return "Control"
    }

    if ($label -match "MedKit|TreatmentKit|Kolto|Infusion|Mend|Benevolence|Renewal|Shielding|Deflector|Rayshield|Barrier|Ward|Sanctuary|Guard|Bastion|Resolve|Rally|Rousing|Bolster|Recovery|Cleanse|Antitoxin|Coagulant|PainSuppressant|Adrenal|FocusStim|PowerCell|Maintenance|Soothe|Revive|Reward|Hasten|IronHide|IronShell|Warding|Unbreakable|Untouchable|PackRecovery|FieldRecovery|SteadyFormation|HoldTheLine|WatchfulPresence|HarmonicRestoration|SereneFocus") {
        return "Beneficial"
    }

    if ($label -match "Tame|CallBeast") {
        return "Utility"
    }

    return "Harmful"
}

function Get-StatusSemanticCategory([string]$className, [string]$name, [string]$content) {
    # Stances take the Self frame (see IconStandards.md), and the class declares that it is one, so
    # read the declaration instead of guessing from the name. This has to precede the keyword checks
    # below: a stance whose name happens to contain a debuff word ("Sustain Burn") is otherwise
    # classified Harmful, and hand-correcting the manifest only survives until the next refresh that
    # has no prior row to preserve the value from.
    if ($content -match "StatusEffectSourceType\.Stance") {
        return "Self"
    }

    if ($content -match "StatusEffectCategory\.[A-Za-z0-9_ ]*(Debuff|Control|Bleeding)" -or
        $className -match "Burn|Poison|Toxin|Bleed|Sunder|Weaken|Exhaust|Dazed|Stun|Blind|Vulnerable|Exposed|Hemorrhage|Hobble|Immobil|Mark|Terrified|Tranquil|Disease|Penalty|Drain|Choke|Terror|Sonic|WeaponJam|Distracting|Flash|Erosion|Fracture|Disruption|Breach|Crippling|Incapacitate|SmokeBomb|Decoy|ChallengeStatusEffect|Vulnerability|Fatigue|Taunting|CoveringClaws") {
        return "Harmful"
    }

    if ($className -match "Food|Rest") {
        return "Utility"
    }

    return "Beneficial"
}

function Get-AbilityRows([string]$path, [int]$startRow, [int]$endRow) {
    $rows = @()
    foreach ($line in Get-Content -Path $path) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $parts = $line -split "\s+"
        if ($parts.Count -lt 5) { continue }

        $row = 0
        if (![int]::TryParse($parts[0], [ref]$row)) { continue }
        if ($row -lt $startRow -or $row -gt $endRow -or $parts[1] -eq "****") { continue }

        $rank = Get-RankFromText $parts[1]
        $rows += [pscustomobject]@{
            Type = "Ability"
            Key = $parts[1]
            DisplayName = $parts[1]
            Rank = $rank
            IconResRef = $parts[4]
            SourcePath = $Feat2daPath
        }
    }

    return $rows
}

function Get-StatusEffectClasses([string]$path) {
    $rows = @()
    foreach ($file in Get-ChildItem -Path $path -Filter "*StatusEffect.cs" | Sort-Object Name) {
        $content = Get-Content -Path $file.FullName -Raw
        if ($content -notmatch "public\s+(sealed\s+)?class\s+([A-Za-z0-9_]+)\s*:") {
            continue
        }

        $className = $Matches[2]

        # A [StatConfiguredIcon] effect owns no icon identity of its own: its icon arrives per
        # application from a StatType adjustment and belongs to the configuring perk, whose own
        # anchor class carries the enum member, manifest row, TLK entry, and artwork. It therefore
        # has no per-class row to audit or generate. See IconStandards.md, "Stat-Configured Icons".
        # Only a real attribute declaration on the class counts: the marker must start its own line
        # (never inside a comment or string) with nothing but other attributes between it and the
        # class declaration.
        if ($content -match '(?ms)^\s*\[StatConfiguredIcon\]\s*(?:^\s*\[[^\]\r\n]+\]\s*)*(?:public|internal)\s+(?:(?:sealed|abstract|partial)\s+)*class\s') {
            continue
        }

        # Every other status effect that can be applied to a creature must carry a gameplay icon so
        # the player can see it is active; an effect with nothing worth showing on the icon bar
        # should be modelled as a static stat contribution instead of a status effect. So no class
        # is otherwise exempt here: a definition left on EffectIconType.Invalid is picked up,
        # assigned a real icon by -UpdateStatusEffectCode, and required to carry an
        # effecticons.2da row and TLK entry.

        $name = $className -replace "StatusEffect$", ""
        if ($content -match 'public\s+override\s+string\s+Name\s*=>\s*"([^"]+)"') {
            $name = $Matches[1]
        }
        elseif ($content -match 'public\s+override\s+string\s+Name\s*=>\s*\$"([^"{]+)') {
            # Interpolated names (e.g. stack counters such as "Cruel Momentum ({Stacks})") use their
            # literal prefix as the static display name; the dynamic suffix is combat-log-only and the
            # TLK/effecticons.2da row carries the base name.
            $name = ($Matches[1] -replace '[\s(+\-]+$', '').Trim()
        }

        $rank = Get-RankFromText ($className -replace "StatusEffect$", "")
        if ($null -eq $rank) {
            $rank = Get-RankFromText $name
        }

        $rows += [pscustomobject]@{
            Type = "StatusEffect"
            Key = $className
            DisplayName = $name
            Rank = $rank
            SourcePath = $file.FullName
            Content = $content
        }
    }

    return $rows
}

function New-StatusIconResRef([pscustomobject]$entry, [hashtable]$seen) {
    $rank = if ($entry.Rank) { [string]$entry.Rank } else { "" }
    $displayWords = @(Get-IconWordsWithoutRank $entry.DisplayName)
    $classWords = @(Get-IconWordsWithoutRank ($entry.Key -replace "StatusEffect$", ""))
    $words = @()
    if ($displayWords.Count -gt 0) {
        $words += $displayWords
    }
    else {
        $words += $classWords
    }

    foreach ($extra in @("Self", "Beast", "Slow", "Damage", "Penalty", "Healing")) {
        if (($classWords -contains $extra) -and ($words -notcontains $extra)) {
            $words += $extra
        }
    }

    $body = (($words | ForEach-Object { ConvertTo-IconChunk $_ }) -join "")
    if ([string]::IsNullOrWhiteSpace($body)) {
        $body = "status"
    }

    $maxBody = 12 - $rank.Length
    if ($body.Length -gt $maxBody) {
        $body = $body.Substring(0, $maxBody)
    }

    $baseCandidate = "ief_$body$rank".ToLowerInvariant()
    $candidate = $baseCandidate
    $attempt = 2
    while ($seen.ContainsKey($candidate)) {
        $suffix = [string]$attempt
        $maxBody = 12 - $rank.Length - $suffix.Length
        if ($maxBody -lt 3) {
            throw "Cannot create a readable unique status icon resref for '$($entry.Key)' within 16 characters."
        }

        $candidate = "ief_$($body.Substring(0, [Math]::Min($body.Length, $maxBody)))$suffix$rank".ToLowerInvariant()
        $attempt++
    }

    if ($candidate.Length -gt 16) {
        throw "Generated status icon resref '$candidate' for '$($entry.Key)' exceeds NWN's 16 character limit."
    }

    $seen[$candidate] = $entry.Key
    return $candidate
}

function Get-PreservedStatusIconResRef(
    [hashtable]$existing,
    [pscustomobject]$entry,
    [hashtable]$seen) {
    $manifestKey = Get-ManifestKey $entry.Type $entry.Key
    $row = if ($existing.ContainsKey($manifestKey)) {
        $existing[$manifestKey]
    }
    else {
        Get-ManifestRowByKey $existing $entry.Key
    }

    if ($null -ne $row) {
        $preserved = (Get-OptionalProperty $row "IconResRef").Trim().ToLowerInvariant()
        if (![string]::IsNullOrWhiteSpace($preserved)) {
            if ($preserved.Length -gt 16 -or $preserved -notmatch "^[a-z0-9_]+$") {
                throw "Preserved status icon resref '$preserved' for '$($entry.Key)' is not a valid NWN resource name."
            }
            if ($seen.ContainsKey($preserved)) {
                throw "Preserved status icon resref '$preserved' is shared by '$($seen[$preserved])' and '$($entry.Key)'."
            }

            $seen[$preserved] = $entry.Key
            return $preserved
        }
    }

    return New-StatusIconResRef $entry $seen
}

function Get-SemanticColor([string]$category) {
    switch ($category) {
        "Beneficial" { return [System.Drawing.Color]::FromArgb(255, 84, 246, 122) }
        "Harmful" { return [System.Drawing.Color]::FromArgb(255, 240, 84, 84) }
        "Self" { return [System.Drawing.Color]::FromArgb(255, 79, 195, 255) }
        "Control" { return [System.Drawing.Color]::FromArgb(255, 181, 108, 255) }
        "Deployable" { return [System.Drawing.Color]::FromArgb(255, 255, 184, 77) }
        "Passive" { return [System.Drawing.Color]::FromArgb(255, 245, 215, 110) }
        "Utility" { return [System.Drawing.Color]::FromArgb(255, 221, 230, 240) }
    }

    throw "Unknown icon semantic category '$category'."
}

function Get-MotifColor([string]$className) {
    if ($className -match "Bleed|Blood|Hemorrhage") { return [System.Drawing.Color]::FromArgb(255, 234, 47, 61) }
    if ($className -match "Poison|Toxin|Venom") { return [System.Drawing.Color]::FromArgb(255, 150, 238, 76) }
    if ($className -match "Burn|Fire|Flame") { return [System.Drawing.Color]::FromArgb(255, 255, 151, 48) }
    if ($className -match "Shock|Lightning|Current|Static") { return [System.Drawing.Color]::FromArgb(255, 152, 242, 255) }
    if ($className -match "Ice|Frost|Freezing|Cryo") { return [System.Drawing.Color]::FromArgb(255, 188, 246, 255) }
    if ($className -match "Force|Mind|Terror|Fear|Shroud|Choke") { return [System.Drawing.Color]::FromArgb(255, 198, 147, 255) }
    if ($className -match "Shield|Guard|Ward|Barrier|Bastion|Defense|Resolve") { return [System.Drawing.Color]::FromArgb(255, 185, 236, 255) }
    if ($className -match "Heal|Mend|Kolto|Treatment|Triage|Recovery|Regenerative|Rejuvenation|Coagulant|Antitoxin") { return [System.Drawing.Color]::FromArgb(255, 203, 255, 213) }
    return [System.Drawing.Color]::FromArgb(255, 238, 238, 226)
}

function Get-ShiftedColor([System.Drawing.Color]$color, [int]$amount) {
    return [System.Drawing.Color]::FromArgb(
        $color.A,
        [Math]::Max(0, [Math]::Min(255, $color.R + $amount)),
        [Math]::Max(0, [Math]::Min(255, $color.G + $amount)),
        [Math]::Max(0, [Math]::Min(255, $color.B + $amount))
    )
}

function Get-DarkIconColor([System.Drawing.Color]$color, [double]$scale, [int]$floorBlue = 8) {
    return [System.Drawing.Color]::FromArgb(
        255,
        [int][Math]::Max(3, [Math]::Min(90, $color.R * $scale)),
        [int][Math]::Max(3, [Math]::Min(90, $color.G * $scale)),
        [int][Math]::Max($floorBlue, [Math]::Min(100, $color.B * $scale))
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

function Draw-IconBackdrop($g, [System.Drawing.Color]$semantic, [System.Drawing.Color]$motif, [int]$hash) {
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

function Draw-RankBadge($g, [int]$level, [System.Drawing.Color]$semantic) {
    if ($level -lt 1) { return }
    $rankLabel = [string]$level

    $rect = [System.Drawing.RectangleF]::new(86, 84, 29, 29)
    $path = New-RoundedRectanglePath $rect.X $rect.Y $rect.Width $rect.Height 5
    $g.FillPath((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(238, 4, 5, 8))), $path)
    $g.DrawPath((New-Object System.Drawing.Pen($semantic, 3)), $path)

    $fontSize = if ($rankLabel.Length -le 1) { 22 } elseif ($rankLabel.Length -le 2) { 19 } elseif ($rankLabel.Length -le 3) { 15 } else { 13 }
    $font = New-Object System.Drawing.Font("Arial", $fontSize, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
    $format = New-Object System.Drawing.StringFormat
    $format.Alignment = [System.Drawing.StringAlignment]::Center
    $format.LineAlignment = [System.Drawing.StringAlignment]::Center
    $g.DrawString($rankLabel, $font, (New-Object System.Drawing.SolidBrush([System.Drawing.Color]::Black)), [System.Drawing.RectangleF]::new($rect.X + 1, $rect.Y + 2, $rect.Width, $rect.Height), $format)
    $g.DrawString($rankLabel, $font, (New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)), $rect, $format)
    $font.Dispose()
    $format.Dispose()
    $path.Dispose()
}

function Draw-StatusMotif($g, [string]$className, [System.Drawing.Color]$motif, [System.Drawing.Color]$semantic) {
    $pen = New-Object System.Drawing.Pen($motif, 8)
    $thin = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(235, $semantic), 4)
    $hot = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(225, 255, 255, 255), 2)
    $brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(235, $motif))
    $shadowBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(155, 0, 0, 0))
    $shadowPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(165, 0, 0, 0), 9)

    if ($className -match "Ailment|Resistance") {
        $points = @(
            [System.Drawing.Point]::new(64, 25), [System.Drawing.Point]::new(96, 38),
            [System.Drawing.Point]::new(88, 86), [System.Drawing.Point]::new(64, 106),
            [System.Drawing.Point]::new(40, 86), [System.Drawing.Point]::new(32, 38)
        )
        $shadow = @($points | ForEach-Object { [System.Drawing.Point]::new($_.X + 3, $_.Y + 4) })
        $g.FillPolygon($shadowBrush, $shadow)
        $g.FillPolygon($brush, $points)
        $g.DrawPolygon($thin, $points)
        $g.FillRectangle((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(235, 4, 5, 8))), 56, 51, 16, 36)
        $g.FillRectangle((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(235, 4, 5, 8))), 46, 61, 36, 16)
    }
    elseif ($className -match "Canister|Injector") {
        # A combat stim is injected, not healing: it buffs attack and attack speed. Give it an
        # injector rather than the medical cross, which belongs to the restoration family above and
        # would otherwise read as a heal.
        $g.DrawLine($shadowPen, 47, 96, 88, 55)
        $barrel = @(
            [System.Drawing.Point]::new(58, 39), [System.Drawing.Point]::new(89, 70),
            [System.Drawing.Point]::new(76, 83), [System.Drawing.Point]::new(45, 52)
        )
        $g.FillPolygon($brush, $barrel)
        $g.DrawPolygon($thin, $barrel)
        # Plunger at the top, needle running down to the tip.
        $g.DrawLine($pen, 66, 31, 83, 48)
        $g.DrawLine($thin, 38, 90, 60, 68)
        $g.DrawLine($hot, 55, 52, 74, 71)
        $g.FillEllipse($brush, 33, 88, 10, 10)
    }
    elseif ($className -match "Heal|Mend|Kolto|Treatment|Triage|Recovery|Regenerative|Rejuvenation|Coagulant|Antitoxin|Soothe") {
        $g.FillEllipse($shadowBrush, 28, 28, 76, 76)
        $g.FillRectangle($brush, 54, 29, 20, 70)
        $g.FillRectangle($brush, 29, 54, 70, 20)
        $g.DrawEllipse($thin, 27, 27, 74, 74)
        $g.DrawLine($hot, 64, 35, 64, 90)
    }
    elseif ($className -match "WardenWallAura") {
        # The radiated, ally-facing counterpart to a defensive shield: the same crest ringed by
        # concentric arcs, so a party member cannot confuse it with the self-side buff it pairs with.
        $crest = @(
            [System.Drawing.Point]::new(64, 38), [System.Drawing.Point]::new(85, 47),
            [System.Drawing.Point]::new(80, 79), [System.Drawing.Point]::new(64, 92),
            [System.Drawing.Point]::new(48, 79), [System.Drawing.Point]::new(43, 47)
        )
        $shadow = @($crest | ForEach-Object { [System.Drawing.Point]::new($_.X + 3, $_.Y + 4) })
        $g.FillPolygon($shadowBrush, $shadow)
        $g.FillPolygon($brush, $crest)
        $g.DrawPolygon($thin, $crest)
        foreach ($ring in @(@(26, 26, 76), @(17, 17, 94))) {
            $g.DrawEllipse($thin, $ring[0], $ring[1], $ring[2], $ring[2])
        }
        $g.DrawLine($hot, 64, 45, 64, 84)
    }
    elseif ($className -match "WardenSweep") {
        # Retaliation rather than plain mitigation: a crest throwing damage back out, so it reads
        # differently from the damage-reduction shields it sits beside.
        $crest = @(
            [System.Drawing.Point]::new(64, 36), [System.Drawing.Point]::new(84, 46),
            [System.Drawing.Point]::new(79, 78), [System.Drawing.Point]::new(64, 91),
            [System.Drawing.Point]::new(49, 78), [System.Drawing.Point]::new(44, 46)
        )
        $shadow = @($crest | ForEach-Object { [System.Drawing.Point]::new($_.X + 3, $_.Y + 4) })
        $g.FillPolygon($shadowBrush, $shadow)
        $g.FillPolygon($brush, $crest)
        $g.DrawPolygon($thin, $crest)
        foreach ($chevron in @(
            @(64, 16, 50, 31, 78, 31), @(26, 64, 41, 50, 41, 78), @(102, 64, 87, 50, 87, 78)
        )) {
            $arrow = @(
                [System.Drawing.Point]::new($chevron[0], $chevron[1]),
                [System.Drawing.Point]::new($chevron[2], $chevron[3]),
                [System.Drawing.Point]::new($chevron[4], $chevron[5])
            )
            $g.FillPolygon($brush, $arrow)
            $g.DrawPolygon($thin, $arrow)
        }
    }
    elseif ($className -match "Shield|Guard|Ward|Barrier|Bastion|Defense|Resolve|Armor|Hide|Warding") {
        $points = @(
            [System.Drawing.Point]::new(64, 25), [System.Drawing.Point]::new(96, 38),
            [System.Drawing.Point]::new(88, 86), [System.Drawing.Point]::new(64, 106),
            [System.Drawing.Point]::new(40, 86), [System.Drawing.Point]::new(32, 38)
        )
        $shadow = @($points | ForEach-Object { [System.Drawing.Point]::new($_.X + 3, $_.Y + 4) })
        $g.FillPolygon($shadowBrush, $shadow)
        $g.FillPolygon($brush, $points)
        $g.DrawPolygon($thin, $points)
        $g.DrawLine($hot, 64, 35, 64, 91)
    }
    elseif ($className -match "Poison|Toxin|Venom|Disease") {
        $vial = @(
            [System.Drawing.Point]::new(54, 25), [System.Drawing.Point]::new(74, 25),
            [System.Drawing.Point]::new(74, 45), [System.Drawing.Point]::new(91, 89),
            [System.Drawing.Point]::new(79, 105), [System.Drawing.Point]::new(49, 105),
            [System.Drawing.Point]::new(37, 89), [System.Drawing.Point]::new(54, 45)
        )
        $shadow = @($vial | ForEach-Object { [System.Drawing.Point]::new($_.X + 3, $_.Y + 4) })
        $g.FillPolygon($shadowBrush, $shadow)
        $g.FillPolygon($brush, $vial)
        $g.DrawPolygon($thin, $vial)
        $g.DrawLine($hot, 49, 78, 78, 58)
        $g.FillEllipse((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(230, $semantic))), 51, 78, 8, 8)
        $g.FillEllipse((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(230, $semantic))), 69, 88, 6, 6)
    }
    elseif ($className -match "Bleed|Blood|Hemorrhage") {
        $drop = @(
            [System.Drawing.Point]::new(64, 24), [System.Drawing.Point]::new(84, 65),
            [System.Drawing.Point]::new(79, 91), [System.Drawing.Point]::new(64, 106),
            [System.Drawing.Point]::new(49, 91), [System.Drawing.Point]::new(44, 65)
        )
        $shadow = @($drop | ForEach-Object { [System.Drawing.Point]::new($_.X + 3, $_.Y + 4) })
        $g.FillPolygon($shadowBrush, $shadow)
        $g.FillPolygon($brush, $drop)
        $g.DrawPolygon($thin, $drop)
        $g.DrawArc($hot, 55, 58, 24, 26, 205, 115)
    }
    elseif ($className -match "Burn|Fire|Flame") {
        $flame = @(
            [System.Drawing.Point]::new(64, 20), [System.Drawing.Point]::new(84, 53),
            [System.Drawing.Point]::new(75, 52), [System.Drawing.Point]::new(94, 103),
            [System.Drawing.Point]::new(63, 90), [System.Drawing.Point]::new(43, 106),
            [System.Drawing.Point]::new(51, 65), [System.Drawing.Point]::new(38, 70)
        )
        $shadow = @($flame | ForEach-Object { [System.Drawing.Point]::new($_.X + 3, $_.Y + 4) })
        $g.FillPolygon($shadowBrush, $shadow)
        $g.FillPolygon($brush, $flame)
        $g.DrawPolygon($thin, $flame)
        $inner = @([System.Drawing.Point]::new(64, 46), [System.Drawing.Point]::new(75, 73), [System.Drawing.Point]::new(62, 89), [System.Drawing.Point]::new(54, 76))
        $g.FillPolygon((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(210, 255, 238, 128))), $inner)
    }
    elseif ($className -match "Shock|Lightning|Current|Static") {
        $bolt = @(
            [System.Drawing.Point]::new(75, 19), [System.Drawing.Point]::new(39, 67),
            [System.Drawing.Point]::new(59, 66), [System.Drawing.Point]::new(48, 108),
            [System.Drawing.Point]::new(91, 54), [System.Drawing.Point]::new(69, 56)
        )
        $shadow = @($bolt | ForEach-Object { [System.Drawing.Point]::new($_.X + 3, $_.Y + 4) })
        $g.FillPolygon($shadowBrush, $shadow)
        $g.FillPolygon($brush, $bolt)
        $g.DrawPolygon($thin, $bolt)
    }
    elseif ($className -match "Stun|Daze|Mind|Terror|Fear|Confuse|Choke|Disorient|Tranquil|Nightmare|Psychic") {
        $eye = @(
            [System.Drawing.Point]::new(27, 64), [System.Drawing.Point]::new(48, 43),
            [System.Drawing.Point]::new(80, 43), [System.Drawing.Point]::new(101, 64),
            [System.Drawing.Point]::new(80, 85), [System.Drawing.Point]::new(48, 85)
        )
        $shadow = @($eye | ForEach-Object { [System.Drawing.Point]::new($_.X + 3, $_.Y + 4) })
        $g.FillPolygon($shadowBrush, $shadow)
        $g.FillPolygon($brush, $eye)
        $g.DrawPolygon($thin, $eye)
        $g.FillEllipse((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(240, 5, 7, 10))), 54, 54, 21, 21)
        $g.FillEllipse((New-Object System.Drawing.SolidBrush($semantic)), 60, 60, 9, 9)
    }
    elseif ($className -match "Mark|Expose|Vulnerable|Weaken|Breach|Sunder|Fracture") {
        $g.DrawEllipse($shadowPen, 30, 30, 68, 68)
        $g.DrawEllipse($thin, 30, 30, 68, 68)
        $g.DrawEllipse((New-Object System.Drawing.Pen($motif, 5)), 45, 45, 38, 38)
        $g.DrawLine($pen, 64, 20, 64, 108)
        $g.DrawLine($pen, 20, 64, 108, 64)
        $g.DrawLine($hot, 64, 35, 64, 93)
    }
    elseif ($className -match "Order|Command|Rally|Standard|Formation|Presence|Shout|Mandate") {
        $g.DrawLine($shadowPen, 45, 31, 45, 104)
        $g.DrawLine($pen, 43, 28, 43, 103)
        $flag = @(
            [System.Drawing.Point]::new(47, 30), [System.Drawing.Point]::new(93, 39),
            [System.Drawing.Point]::new(79, 62), [System.Drawing.Point]::new(47, 56)
        )
        $g.FillPolygon($brush, $flag)
        $g.DrawPolygon($thin, $flag)
        $g.DrawLine($hot, 53, 38, 83, 43)
    }
    elseif ($className -match "Smoke|Decoy|Fog|Stealth|Conceal|Cloak") {
        foreach ($circle in @(
            @(32, 61, 33), @(50, 45, 42), @(74, 55, 36), @(43, 74, 45)
        )) {
            $g.FillEllipse($shadowBrush, $circle[0] + 3, $circle[1] + 4, $circle[2], $circle[2])
            $g.FillEllipse($brush, $circle[0], $circle[1], $circle[2], $circle[2])
        }
        $g.DrawArc($thin, 34, 46, 62, 50, 190, 190)
    }
    elseif ($className -match "WeaponJam|Disruption|Dampening|Suppression|PowerCell|Capacitor|Overload|Overcharge") {
        $g.FillEllipse($shadowBrush, 31, 31, 70, 70)
        $g.DrawEllipse($thin, 32, 32, 66, 66)
        for ($i = 0; $i -lt 8; $i++) {
            $angle = (($i * 45) - 90) * [Math]::PI / 180
            $x1 = [int](64 + [Math]::Cos($angle) * 21)
            $y1 = [int](64 + [Math]::Sin($angle) * 21)
            $x2 = [int](64 + [Math]::Cos($angle) * 44)
            $y2 = [int](64 + [Math]::Sin($angle) * 44)
            $g.DrawLine($pen, $x1, $y1, $x2, $y2)
        }
        $g.FillEllipse($brush, 52, 52, 24, 24)
        $g.DrawLine($hot, 44, 84, 84, 44)
    }
    elseif ($className -match "Haste|Speed|Movement|Hobble|Hamstring|Immobil|Slow|Dash|Tempo|FinishingDriveMomentum") {
        $g.DrawArc($shadowPen, 31, 39, 66, 52, 35, 250)
        $g.DrawArc($pen, 30, 37, 66, 52, 35, 250)
        $arrow = @([System.Drawing.Point]::new(91, 37), [System.Drawing.Point]::new(112, 38), [System.Drawing.Point]::new(98, 57))
        $g.FillPolygon($brush, $arrow)
        $g.DrawLine($thin, 35, 94, 77, 94)
    }
    elseif ($className -match "PetFood|Pet Food") {
        $g.FillEllipse($shadowBrush, 31, 58, 66, 38)
        $g.FillEllipse($brush, 31, 55, 66, 38)
        $g.DrawArc($thin, 35, 57, 58, 28, 0, 180)
        $g.DrawLine($thin, 38, 75, 90, 75)
        $g.FillEllipse((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(235, 5, 7, 10))), 52, 37, 11, 11)
        $g.FillEllipse((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(235, 5, 7, 10))), 67, 37, 11, 11)
        $g.FillEllipse((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(235, 5, 7, 10))), 58, 50, 18, 15)
        $g.DrawArc($hot, 45, 60, 38, 20, 8, 164)
    }
    elseif ($className -match "Food") {
        $meat = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(245, 205, 112, 42))
        $meatDark = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(235, 133, 61, 26))
        $bone = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(245, 247, 229, 190))
        $bonePen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(235, 247, 229, 190), 9)
        $boneEdge = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(230, 255, 247, 220), 4)
        $meatEdge = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(230, 255, 207, 100), 4)
        $seasoning = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(230, 236, 150, 72))
        $shine = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(145, 255, 225, 164))

        $g.DrawLine($bonePen, 47, 83, 28, 101)
        $g.DrawLine($boneEdge, 48, 83, 29, 101)
        $g.FillEllipse($bone, 19, 94, 18, 18)
        $g.FillEllipse($bone, 27, 101, 17, 17)

        $meatPath = New-Object System.Drawing.Drawing2D.GraphicsPath
        $meatPath.StartFigure()
        $meatPath.AddBezier(41, 83, 35, 67, 42, 43, 59, 30)
        $meatPath.AddBezier(58, 31, 70, 19, 92, 15, 103, 27)
        $meatPath.AddBezier(103, 27, 115, 40, 106, 66, 86, 76)
        $meatPath.AddBezier(86, 76, 69, 86, 50, 89, 41, 83)
        $meatPath.CloseFigure()
        $shadowPath = $meatPath.Clone()
        $matrix = New-Object System.Drawing.Drawing2D.Matrix
        $matrix.Translate(3, 4)
        $shadowPath.Transform($matrix)
        $g.FillPath($shadowBrush, $shadowPath)
        $g.FillPath($meat, $meatPath)
        $g.DrawPath($thin, $meatPath)

        $darkPath = New-Object System.Drawing.Drawing2D.GraphicsPath
        $darkPath.StartFigure()
        $darkPath.AddBezier(43, 78, 57, 86, 77, 80, 92, 67)
        $darkPath.AddBezier(82, 75, 63, 85, 43, 81, 43, 78)
        $darkPath.CloseFigure()
        $g.FillPath($meatDark, $darkPath)

        $g.FillEllipse($shine, 66, 35, 22, 9)
        foreach ($dot in @(
            @(58, 31, 4), @(92, 29, 4), @(52, 53, 4), @(70, 61, 4), @(89, 58, 3), @(61, 71, 3)
        )) {
            $g.FillEllipse($seasoning, $dot[0], $dot[1], $dot[2], $dot[2])
        }
        $g.DrawArc($meatEdge, 56, 25, 43, 32, 200, 120)

        $meat.Dispose()
        $meatDark.Dispose()
        $bone.Dispose()
        $bonePen.Dispose()
        $boneEdge.Dispose()
        $meatEdge.Dispose()
        $seasoning.Dispose()
        $shine.Dispose()
        $meatPath.Dispose()
        $shadowPath.Dispose()
        $matrix.Dispose()
        $darkPath.Dispose()
    }
    elseif ($className -match "Rest") {
        $g.FillRectangle($shadowBrush, 26, 60, 74, 32)
        $g.FillRectangle($brush, 25, 56, 76, 33)
        $g.FillRectangle((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(238, 5, 7, 10))), 33, 61, 21, 16)
        $g.DrawLine($thin, 25, 89, 101, 89)
        $g.DrawLine($thin, 28, 55, 28, 94)
        $g.DrawArc($hot, 52, 31, 35, 35, 85, 230)
        $g.FillEllipse((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(235, 5, 7, 10))), 64, 27, 34, 34)
    }
    elseif ($className -match "Stance|Focus|Centering|Attentiveness|Precision|Collapse|CruelMomentum") {
        $g.FillEllipse($shadowBrush, 53, 25, 24, 24)
        $g.FillEllipse($brush, 52, 24, 24, 24)
        $g.DrawLine($pen, 64, 49, 64, 86)
        $g.DrawLine($pen, 39, 63, 89, 63)
        $g.DrawLine($pen, 64, 86, 45, 105)
        $g.DrawLine($pen, 64, 86, 83, 105)
        $g.DrawEllipse($thin, 30, 25, 68, 88)
    }
    else {
        $diamond = @(
            [System.Drawing.Point]::new(64, 25), [System.Drawing.Point]::new(96, 64),
            [System.Drawing.Point]::new(64, 103), [System.Drawing.Point]::new(32, 64)
        )
        $shadow = @($diamond | ForEach-Object { [System.Drawing.Point]::new($_.X + 3, $_.Y + 4) })
        $g.FillPolygon($shadowBrush, $shadow)
        $g.FillPolygon($brush, $diamond)
        $g.DrawPolygon($thin, $diamond)
        $g.DrawLine($hot, 64, 37, 64, 91)
        $g.DrawLine($thin, 36, 79, 58, 92)
    }

    $pen.Dispose()
    $thin.Dispose()
    $hot.Dispose()
    $brush.Dispose()
    $shadowBrush.Dispose()
    $shadowPen.Dispose()
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

function New-StatusIcon([pscustomobject]$entry, [string]$outputPath) {
    $semantic = Get-SemanticColor $entry.SemanticCategory
    $motif = Get-MotifColor "$($entry.Key) $($entry.DisplayName)"
    $hash = Get-StableHash $entry.Key

    $large = New-Object System.Drawing.Bitmap 256, 256
    $g = [System.Drawing.Graphics]::FromImage($large)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
    $g.ScaleTransform(2, 2)
    $g.Clear([System.Drawing.Color]::FromArgb(255, 0, 0, 0))

    Draw-IconBackdrop $g $semantic $motif $hash

    $sourcePath = Join-Path (Resolve-RepoPath $StatusIconSourcePath) "$($entry.IconResRef).png"
    if (Test-Path -LiteralPath $sourcePath) {
        $source = [System.Drawing.Image]::FromFile($sourcePath)
        try {
            $cropSize = [Math]::Min($source.Width, $source.Height)
            $cropX = [int](($source.Width - $cropSize) / 2)
            $cropY = [int](($source.Height - $cropSize) / 2)
            $destination = [System.Drawing.RectangleF]::new(18, 18, 92, 92)
            $sourceRectangle = [System.Drawing.Rectangle]::new($cropX, $cropY, $cropSize, $cropSize)

            Invoke-InContentBounds $g {
                $g.DrawImage(
                    $source,
                    $destination,
                    $sourceRectangle,
                    [System.Drawing.GraphicsUnit]::Pixel)
            }
        }
        finally {
            $source.Dispose()
        }
    }
    else {
        Invoke-InContentBounds $g {
            Draw-IllustrativeAccents $g $motif $semantic $hash
            Draw-StatusMotif $g "$($entry.Key) $($entry.DisplayName)" $motif $semantic
        }
    }
    $badgeRank = ""
    $resrefKey = $entry.IconResRef.ToLowerInvariant()
    if ($script:RankBadgeByResRef -and $script:RankBadgeByResRef.ContainsKey($resrefKey)) {
        $badgeRank = $script:RankBadgeByResRef[$resrefKey]
    }

    if (![string]::IsNullOrWhiteSpace($badgeRank)) {
        Draw-RankBadge $g ([int]$badgeRank) $semantic
    }

    $small = New-Object System.Drawing.Bitmap $IconSize, $IconSize
    $sg = [System.Drawing.Graphics]::FromImage($small)
    $sg.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $sg.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $sg.DrawImage($large, 0, 0, $IconSize, $IconSize)
    Write-Tga $small $outputPath

    $sg.Dispose()
    $small.Dispose()
    $g.Dispose()
    $large.Dispose()
}

function Build-ManifestRows([hashtable]$existing) {
    $statusIconSeen = @{}
    $rows = @()
    $abilityRows = @(Get-AbilityRows (Resolve-RepoPath $Feat2daPath) $GeneratedFeatStart $GeneratedFeatEnd)

    foreach ($ability in $abilityRows) {
        $rows += [pscustomobject]@{
            Type = $ability.Type
            Key = $ability.Key
            DisplayName = $ability.DisplayName
            SemanticCategory = Get-PreservedCategory $existing $ability.Type $ability.Key (Get-AbilitySemanticCategory $ability.Key)
            Rank = $ability.Rank
            IconResRef = $ability.IconResRef
            SourcePath = $ability.SourcePath
            Alignment = Get-PreservedAlignment $existing $ability.Type $ability.Key
        }
    }

    $rows += @(Get-CustomFeatSpellRows $abilityRows $existing)

    foreach ($status in Get-StatusEffectClasses (Resolve-RepoPath $StatusEffectPath)) {
        $resref = Get-PreservedStatusIconResRef $existing $status $statusIconSeen
        $relativePath = $status.SourcePath.Substring((Get-Location).Path.Length + 1)
        $rows += [pscustomobject]@{
            Type = $status.Type
            Key = $status.Key
            DisplayName = $status.DisplayName
            SemanticCategory = Get-PreservedCategory $existing $status.Type $status.Key (Get-StatusSemanticCategory $status.Key $status.DisplayName $status.Content)
            Rank = $status.Rank
            IconResRef = $resref
            SourcePath = $relativePath
            Alignment = Get-PreservedAlignment $existing $status.Type $status.Key
        }
    }

    return $rows | Sort-Object Type, Key
}

function Write-Manifest([object[]]$rows, [string]$path) {
    $directory = Split-Path -Parent $path
    if (!(Test-Path -LiteralPath $directory)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }

    $rows | Export-Csv -Path $path -NoTypeInformation
}

function Get-CustomTlkTextToStrRef([string]$path) {
    if (!(Test-Path -LiteralPath $path)) {
        throw "Could not find TLK JSON '$path'."
    }

    $tlk = Get-Content -Path $path -Raw | ConvertFrom-Json
    $map = @{}
    foreach ($entry in $tlk.entries) {
        if ([string]::IsNullOrWhiteSpace($entry.text)) {
            continue
        }

        if (!$map.ContainsKey($entry.text)) {
            $map[$entry.text] = ($CustomTlkOffset + [int]$entry.id).ToString()
        }
    }

    return $map
}

function Get-StatusEffectStrRefsByKey([object[]]$statusRows, [hashtable]$tlkTextToStrRef) {
    $map = @{}
    $errors = [System.Collections.Generic.List[string]]::new()

    foreach ($entry in $statusRows) {
        if (!$tlkTextToStrRef.ContainsKey($entry.DisplayName)) {
            $errors.Add("StatusEffect '$($entry.Key)' display name '$($entry.DisplayName)' is missing from the custom TLK.") | Out-Null
            continue
        }

        $map[$entry.Key] = $tlkTextToStrRef[$entry.DisplayName]
    }

    if ($errors.Count -gt 0) {
        throw "Status effect TLK audit failed:`n$($errors -join "`n")"
    }

    return $map
}

function Update-EffectIconTypeEnum([object[]]$statusRows, [string]$path) {
    $text = Get-Content -Path $path -Raw
    $blockLines = [System.Collections.Generic.List[string]]::new()
    $blockLines.Add($GeneratedEnumStartMarker) | Out-Null
    $row = $StatusEffectIconStart
    foreach ($entry in $statusRows) {
        $blockLines.Add("        $($entry.Key) = $row,") | Out-Null
        $row++
    }
    $blockLines.Add($GeneratedEnumEndMarker) | Out-Null
    $block = ($blockLines -join [Environment]::NewLine)

    $pattern = [regex]::Escape($GeneratedEnumStartMarker) + ".*?" + [regex]::Escape($GeneratedEnumEndMarker)
    if ([regex]::IsMatch($text, $pattern, [System.Text.RegularExpressions.RegexOptions]::Singleline)) {
        $text = [regex]::Replace($text, $pattern, [System.Text.RegularExpressions.MatchEvaluator]{ param($m) $block }, [System.Text.RegularExpressions.RegexOptions]::Singleline)
    }
    else {
        $insert = "$block$([Environment]::NewLine)"
        $text = [regex]::Replace($text, "\s*    }\s*}\s*$", "$([Environment]::NewLine)$insert    }$([Environment]::NewLine)}$([Environment]::NewLine)")
    }

    Set-Content -Path $path -Value $text -NoNewline
}

function Update-EffectIcons2da([object[]]$statusRows, [string]$path, [hashtable]$statusEffectStrRefsByKey) {
    $baseLines = @()
    foreach ($line in Get-Content -Path $path) {
        $trimmed = $line.Trim()
        if ($trimmed -match "^(\d+)\s+") {
            if ([int]$Matches[1] -ge $StatusEffectIconStart) {
                continue
            }
        }

        $baseLines += $line
    }

    $row = $StatusEffectIconStart
    foreach ($entry in $statusRows) {
        $label = Get-EffectIconLabel $entry
        if (!$statusEffectStrRefsByKey.ContainsKey($entry.Key)) {
            throw "No TLK string ref found for status effect '$($entry.Key)'."
        }

        $baseLines += ("{0,-5} {1,-45} {2,-18} {3}" -f $row, $label, $entry.IconResRef, $statusEffectStrRefsByKey[$entry.Key])
        $row++
    }

    # Write as UTF-8 without a BOM. NWN's 2DA parser rejects any file that
    # begins with a byte-order mark ("Failed to demand <table>.2da"), which
    # takes the whole table offline and crashes clients that resolve its rows.
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllLines($path, [string[]]$baseLines, $utf8NoBom)
}

function Ensure-EffectIconUsing([string]$content) {
    if ($content -match "using SWLOR\.NWN\.API\.NWScript\.Enum;") {
        return $content
    }

    if ($content -match "^using ") {
        $regex = [regex]::new("(?s)(using .+?;\r?\n)(?!using )")
        return $regex.Replace($content, "`$1using SWLOR.NWN.API.NWScript.Enum;`r`n", 1)
    }

    return "using SWLOR.NWN.API.NWScript.Enum;`r`n`r`n$content"
}

function Update-StatusEffectCode([object[]]$statusRows) {
    foreach ($entry in $statusRows) {
        $path = Resolve-RepoPath $entry.SourcePath
        $content = Get-Content -Path $path -Raw
        $content = Ensure-EffectIconUsing $content
        $iconLine = "        public override EffectIconType Icon => EffectIconType.$($entry.Key);"

        $dynamicIconRegex = [regex]::new("(?ms)^\s*public\s+override\s+EffectIconType\s+Icon\s*=>\s*[^;\r\n]*switch\s*\{.*?^\s*};\r?\n?")
        $simpleIconRegex = [regex]::new("(?m)^\s*public\s+override\s+EffectIconType\s+Icon\s*=>\s*EffectIconType\.[A-Za-z0-9_]+\s*;\r?\n?")
        $content = $dynamicIconRegex.Replace($content, "")
        $content = $simpleIconRegex.Replace($content, "")

        if ($content -match 'public\s+override\s+string\s+Name\s*=>\s*[^;]+;') {
            $regex = [regex]::new('(public\s+override\s+string\s+Name\s*=>\s*[^;]+;)')
            $content = $regex.Replace($content, "`$1`r`n$iconLine", 1)
        }
        else {
            throw "Could not place icon property in $path."
        }

        Set-Content -Path $path -Value $content -NoNewline
    }
}

function Generate-StatusIcons([object[]]$statusRows, [string]$iconDirectory) {
    $expected = @{}
    foreach ($entry in $statusRows) {
        $expected[$entry.IconResRef.ToLowerInvariant()] = $true
    }

    foreach ($file in Get-ChildItem -Path $iconDirectory -Filter "ief_*.tga") {
        if (-not $expected.ContainsKey($file.BaseName.ToLowerInvariant())) {
            Remove-Item -LiteralPath $file.FullName
        }
    }

    foreach ($entry in $statusRows) {
        New-StatusIcon $entry (Join-Path $iconDirectory "$($entry.IconResRef).tga")
    }
}

function Export-StatusIconSamples([object[]]$statusRows, [string]$outputDirectory, [string[]]$iconResRefs) {
    if ([string]::IsNullOrWhiteSpace($outputDirectory)) {
        throw "SampleOutputPath is required for sample generation."
    }

    $resolvedOutput = if ([System.IO.Path]::IsPathRooted($outputDirectory)) { $outputDirectory } else { Join-Path (Get-Location).Path $outputDirectory }
    if (!(Test-Path -LiteralPath $resolvedOutput)) {
        New-Item -ItemType Directory -Path $resolvedOutput -Force | Out-Null
    }

    $requested = @{}
    foreach ($resrefValue in $iconResRefs) {
        foreach ($resref in ([string]$resrefValue -split "[,;]")) {
            $trimmed = $resref.Trim()
            if (![string]::IsNullOrWhiteSpace($trimmed)) {
                $requested[$trimmed.ToLowerInvariant()] = $true
            }
        }
    }

    $generated = 0
    foreach ($entry in $statusRows) {
        if ($requested.Count -gt 0 -and !$requested.ContainsKey($entry.IconResRef.ToLowerInvariant())) {
            continue
        }

        New-StatusIcon $entry (Join-Path $resolvedOutput "$($entry.IconResRef).tga")
        $generated++
    }

    Write-Host "Generated $generated status effect icon samples in $resolvedOutput."
}

function Add-TgaValidationErrors([System.Collections.Generic.List[string]]$errors, [string]$path, [string]$label) {
    $bytes = [System.IO.File]::ReadAllBytes($path)
    if ($bytes.Length -lt 18) {
        $errors.Add("$label TGA '$path' is too small to contain a valid header.") | Out-Null
        return
    }

    $width = $bytes[12] + ($bytes[13] -shl 8)
    $height = $bytes[14] + ($bytes[15] -shl 8)
    if ($width -ne $IconSize -or $height -ne $IconSize) {
        $errors.Add("$label TGA '$path' is $($width)x$height; expected $($IconSize)x$IconSize.") | Out-Null
    }

    if (($bytes[17] -band 32) -ne 0) {
        $errors.Add("$label TGA '$path' uses top-left origin; NWN gameplay icons must use bottom-left origin.") | Out-Null
    }

    if ($bytes[16] -eq 32) {
        $offset = 18
        for ($i = 0; $i -lt ($width * $height); $i++) {
            $alpha = $bytes[$offset + 3]
            if ($alpha -ne 255) {
                $errors.Add("$label TGA '$path' contains non-opaque alpha at pixel $i; gameplay icons must be fully opaque.") | Out-Null
                break
            }

            $offset += 4
        }
    }
}

function Add-SemanticFrameValidationErrors(
    [System.Collections.Generic.List[string]]$errors,
    [string]$path,
    [string]$label,
    [string]$category) {
    $bytes = [System.IO.File]::ReadAllBytes($path)
    if ($bytes.Length -lt 18) {
        return
    }

    $width = $bytes[12] + ($bytes[13] -shl 8)
    $height = $bytes[14] + ($bytes[15] -shl 8)
    if ($width -ne $IconSize -or $height -ne $IconSize) {
        return
    }

    if ($bytes[2] -ne 2 -or ($bytes[16] -ne 24 -and $bytes[16] -ne 32)) {
        $errors.Add("$label TGA '$path' must be an uncompressed 24-bit or 32-bit final gameplay icon to verify semantic frame color.") | Out-Null
        return
    }

    $expected = Get-SemanticColor $category
    $bytesPerPixel = [int]($bytes[16] / 8)
    $offset = 18 + $bytes[0]
    $matches = 0

    for ($y = 0; $y -lt $height; $y++) {
        for ($x = 0; $x -lt $width; $x++) {
            if ($offset + 2 -ge $bytes.Length) {
                break
            }

            $isFramePixel =
                ($x -ge 1 -and $x -le 30 -and ($y -eq 1 -or $y -eq 30)) -or
                ($y -ge 1 -and $y -le 30 -and ($x -eq 1 -or $x -eq 30)) -or
                ($x -ge 3 -and $x -le 28 -and ($y -eq 3 -or $y -eq 28)) -or
                ($y -ge 3 -and $y -le 28 -and ($x -eq 3 -or $x -eq 28))

            if ($isFramePixel) {
                $blue = $bytes[$offset]
                $green = $bytes[$offset + 1]
                $red = $bytes[$offset + 2]
                if ([Math]::Abs($red - $expected.R) -le 55 -and
                    [Math]::Abs($green - $expected.G) -le 55 -and
                    [Math]::Abs($blue - $expected.B) -le 55) {
                    $matches++
                }
            }

            $offset += $bytesPerPixel
        }
    }

    if ($matches -lt 16) {
        $errors.Add("$label TGA '$path' is missing the $category semantic frame color.") | Out-Null
    }
}

function Test-GameplayIconStandards([object[]]$rows, [hashtable]$statusEffectStrRefsByKey) {
    $errors = [System.Collections.Generic.List[string]]::new()

    # -AuditOnly reads the manifest CSV rather than rediscovering definitions, so a status effect
    # that never made it into the manifest is invisible to every per-row check below -- which is
    # exactly the state a newly added class is in. Reconcile the definitions on disk against the
    # manifest first, so a new effect cannot pass simply by being absent.
    $manifestStatusKeys = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($row in $rows) {
        if ($row.Type -eq "StatusEffect") {
            [void]$manifestStatusKeys.Add($row.Key)
        }
    }

    foreach ($discovered in Get-StatusEffectClasses (Resolve-RepoPath $StatusEffectPath)) {
        if (!$manifestStatusKeys.Contains($discovered.Key)) {
            $errors.Add("StatusEffect '$($discovered.Key)' has no gameplay icon manifest row. Every applied status effect must declare a real EffectIconType; run -RefreshManifest -UpdateStatusEffectCode and generate its icon, or model it as a static stat contribution instead of a status effect.") | Out-Null
        }
    }

    # NWN's 2DA parser cannot read a file that begins with a UTF-8 byte-order
    # mark: it fails to load the entire table and crashes clients that resolve
    # its rows (e.g. effect icons applied on rest). Editors that save as
    # "UTF-8 with BOM" reintroduce this silently, so guard the managed tables.
    foreach ($managed2da in @($Feat2daPath, $Spells2daPath, $EffectIcons2daPath)) {
        $resolved2da = Resolve-RepoPath $managed2da
        $prefix = [byte[]]::new(3)
        $stream = [System.IO.File]::OpenRead($resolved2da)
        try {
            $read = $stream.Read($prefix, 0, 3)
        }
        finally {
            $stream.Dispose()
        }
        if ($read -eq 3 -and $prefix[0] -eq 0xEF -and $prefix[1] -eq 0xBB -and $prefix[2] -eq 0xBF) {
            $errors.Add("$([System.IO.Path]::GetFileName($resolved2da)) starts with a UTF-8 byte-order mark; NWN cannot load a 2DA with a BOM. Re-save it as UTF-8 without a BOM.") | Out-Null
        }
    }

    $iconDirectory = Resolve-RepoPath $IconPath
    $enumText = Get-Content -Path (Resolve-RepoPath $EffectIconTypePath) -Raw
    $abilityRowsByKey = @{}
    foreach ($ability in Get-AbilityRows (Resolve-RepoPath $Feat2daPath) $GeneratedFeatStart $GeneratedFeatEnd) {
        $abilityRowsByKey[$ability.Key] = $ability
    }
    $featIconsByLabel = @{}
    foreach ($featRow in Import-2daRows (Resolve-RepoPath $Feat2daPath)) {
        $label = (Get-OptionalProperty $featRow "LABEL").Trim()
        $icon = (Get-OptionalProperty $featRow "ICON").Trim()
        if (![string]::IsNullOrWhiteSpace($label) -and
            $label -ne "****" -and
            $label -ne "DELETED" -and
            ![string]::IsNullOrWhiteSpace($icon) -and
            $icon -ne "****") {
            if (!$featIconsByLabel.ContainsKey($label)) {
                $featIconsByLabel[$label] = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
            }

            [void]$featIconsByLabel[$label].Add($icon)
        }
    }

    $spellIconsByLabel = @{}
    foreach ($spellRow in Import-2daRows (Resolve-RepoPath $Spells2daPath)) {
        $label = (Get-OptionalProperty $spellRow "Label").Trim()
        $icon = (Get-OptionalProperty $spellRow "IconResRef").Trim()
        if (![string]::IsNullOrWhiteSpace($label) -and
            $label -ne "****" -and
            $label -ne "DELETED" -and
            ![string]::IsNullOrWhiteSpace($icon) -and
            $icon -ne "****") {
            if (!$spellIconsByLabel.ContainsKey($label)) {
                $spellIconsByLabel[$label] = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
            }

            [void]$spellIconsByLabel[$label].Add($icon)
        }
    }

    $effectIcons2daResolved = Resolve-RepoPath $EffectIcons2daPath
    $effectIconRowsByResRef = @{}
    foreach ($line in Get-Content -Path $effectIcons2daResolved) {
        if ($line -match "^\s*(\d+)\s+(\S+)\s+(\S+)\s+(\S+)") {
            $row = [int]$Matches[1]
            if ($row -ge $StatusEffectIconStart) {
                $label = $Matches[2]
                $resRef = $Matches[3]
                $strRef = $Matches[4]
                if ($label -notmatch "^[A-Za-z][A-Za-z0-9]*$") {
                    $errors.Add("effecticons.2da row $row label '$label' must be compact PascalCase without underscores.") | Out-Null
                }

                $strRefNumber = 0
                if (![int]::TryParse($strRef, [ref]$strRefNumber)) {
                    $errors.Add("effecticons.2da row $row label '$label' has non-numeric StrRef '$strRef'.") | Out-Null
                }
                elseif ($strRefNumber -lt $CustomTlkOffset) {
                    $errors.Add("effecticons.2da row $row label '$label' must use a custom TLK StrRef, found '$strRef'.") | Out-Null
                }

                $effectIconRowsByResRef[$resRef.ToLowerInvariant()] = [pscustomobject]@{
                    Label = $label
                    StrRef = $strRef
                    Row = $row
                }
            }
        }
    }

    $iconHashes = @{}
    $manifestAbilityKeys = @{}

    foreach ($entry in $rows) {
        if ($ApprovedCategories -notcontains $entry.SemanticCategory) {
            $errors.Add("$($entry.Type) '$($entry.Key)' has unknown semantic category '$($entry.SemanticCategory)'.") | Out-Null
        }

        if ([string]::IsNullOrWhiteSpace($entry.IconResRef)) {
            $errors.Add("$($entry.Type) '$($entry.Key)' is missing IconResRef.") | Out-Null
            continue
        }

        if ($entry.IconResRef.Length -gt 16) {
            $errors.Add("$($entry.Type) '$($entry.Key)' icon resref '$($entry.IconResRef)' exceeds NWN's 16 character limit.") | Out-Null
        }

        if ($entry.IconResRef -notmatch "^[A-Za-z0-9_]+$") {
            $errors.Add("$($entry.Type) '$($entry.Key)' icon resref '$($entry.IconResRef)' contains invalid characters.") | Out-Null
        }

        if (Test-OpaqueGameplayIconResRef $entry) {
            $errors.Add("$($entry.Type) '$($entry.Key)' icon resref '$($entry.IconResRef)' uses an opaque generated suffix; use a short meaningful abbreviation.") | Out-Null
        }

        if ($entry.Type -eq "Ability") {
            $manifestAbilityKeys[$entry.Key] = $true
        }

        if ($entry.Type -eq "Ability" -or $entry.Type -eq "Feat") {
            if (!$featIconsByLabel.ContainsKey($entry.Key)) {
                $errors.Add("$($entry.Type) '$($entry.Key)' is missing from $Feat2daPath.") | Out-Null
            }
            elseif (!$featIconsByLabel[$entry.Key].Contains($entry.IconResRef)) {
                $expectedIcons = ($featIconsByLabel[$entry.Key] | Sort-Object) -join ", "
                $errors.Add("$($entry.Type) '$($entry.Key)' manifest icon '$($entry.IconResRef)' does not match any $Feat2daPath icon for that label: $expectedIcons.") | Out-Null
            }
        }

        if ($entry.Type -eq "Spell") {
            if (!$spellIconsByLabel.ContainsKey($entry.Key)) {
                $errors.Add("Spell '$($entry.Key)' is missing from $Spells2daPath.") | Out-Null
            }
            elseif (!$spellIconsByLabel[$entry.Key].Contains($entry.IconResRef)) {
                $expectedIcons = ($spellIconsByLabel[$entry.Key] | Sort-Object) -join ", "
                $errors.Add("Spell '$($entry.Key)' manifest icon '$($entry.IconResRef)' does not match any $Spells2daPath icon for that label: $expectedIcons.") | Out-Null
            }
        }

        if ($entry.Type -eq "Ability" -and
            $abilityRowsByKey.ContainsKey($entry.Key) -and
            $spellIconsByLabel.ContainsKey($entry.Key) -and
            !$spellIconsByLabel[$entry.Key].Contains($entry.IconResRef)) {
            $expectedIcons = ($spellIconsByLabel[$entry.Key] | Sort-Object) -join ", "
            $errors.Add("Ability '$($entry.Key)' manifest icon '$($entry.IconResRef)' does not match any $Spells2daPath icon for that label: $expectedIcons.") | Out-Null
        }

        $iconFile = Join-Path $iconDirectory "$($entry.IconResRef).tga"
        if (!(Test-Path -LiteralPath $iconFile)) {
            $errors.Add("$($entry.Type) '$($entry.Key)' is missing icon file '$iconFile'.") | Out-Null
        }
        else {
            Add-TgaValidationErrors $errors $iconFile "$($entry.Type) '$($entry.Key)'"
            if ($entry.Type -eq "Ability" -or $entry.Type -eq "Feat" -or $entry.Type -eq "Spell") {
                Add-SemanticFrameValidationErrors $errors $iconFile "$($entry.Type) '$($entry.Key)'" $entry.SemanticCategory
            }

            if ($entry.Type -eq "Ability" -or $entry.Type -eq "StatusEffect") {
                $hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $iconFile).Hash
                if ($iconHashes.ContainsKey($hash)) {
                    $other = $iconHashes[$hash]
                    $errors.Add("$($entry.Type) '$($entry.Key)' has identical icon pixels to $($other.Type) '$($other.Key)'.") | Out-Null
                }
                else {
                    $iconHashes[$hash] = $entry
                }
            }
        }

        $rankValue = 0
        if ($entry.Rank -and ((![int]::TryParse([string]$entry.Rank, [ref]$rankValue)) -or $rankValue -le 0)) {
            $errors.Add("$($entry.Type) '$($entry.Key)' has invalid rank '$($entry.Rank)'.") | Out-Null
        }

        if ($entry.Type -eq "StatusEffect") {
            if ($enumText -notmatch "\b$([regex]::Escape($entry.Key))\s*=") {
                $errors.Add("StatusEffect '$($entry.Key)' is missing from EffectIconType.") | Out-Null
            }

            # The manifest, enum, 2DA row and artwork can all be present and correct while the C#
            # definition still declares EffectIconType.Invalid -- which is the only thing the runtime
            # actually reads, and which disables icon linkage entirely. Generating icons without
            # -UpdateStatusEffectCode produces exactly that state, so the source declaration has to be
            # audited too rather than assumed.
            $statusSourcePath = Resolve-RepoPath $entry.SourcePath
            if (!(Test-Path -LiteralPath $statusSourcePath)) {
                $errors.Add("StatusEffect '$($entry.Key)' source file '$($entry.SourcePath)' was not found.") | Out-Null
            }
            else {
                $statusSourceText = Get-Content -Path $statusSourcePath -Raw
                $expectedIcon = "EffectIconType.$($entry.Key)"
                if ($statusSourceText -match "EffectIconType\s+Icon\s*=>\s*EffectIconType\.Invalid\b") {
                    $errors.Add("StatusEffect '$($entry.Key)' declares EffectIconType.Invalid, so no icon is linked at runtime and the player sees nothing while it is active. Declare '$expectedIcon', or model it as a static stat contribution instead of a status effect.") | Out-Null
                }
                elseif ($statusSourceText -notmatch "EffectIconType\s+Icon\s*=>\s*$([regex]::Escape($expectedIcon))\s*;") {
                    $errors.Add("StatusEffect '$($entry.Key)' should declare 'Icon => $expectedIcon;'.") | Out-Null
                }
            }

            $iconResRefKey = $entry.IconResRef.ToLowerInvariant()
            if (!$effectIconRowsByResRef.ContainsKey($iconResRefKey)) {
                $errors.Add("StatusEffect '$($entry.Key)' is missing from effecticons.2da.") | Out-Null
            }
            else {
                $expectedLabel = Get-EffectIconLabel $entry
                $actualRow = $effectIconRowsByResRef[$iconResRefKey]
                $actualLabel = $actualRow.Label
                if ($actualLabel -ne $expectedLabel) {
                    $errors.Add("StatusEffect '$($entry.Key)' effecticons.2da label '$actualLabel' should be '$expectedLabel'.") | Out-Null
                }

                # EffectIconType values ARE effecticons.2da row numbers: the runtime hands the enum
                # value to the engine as a row index. Checking that the name exists in the enum and
                # the resref exists in the 2DA is not enough -- a renumbered enum paired with a stale
                # Haks table passes both checks while every icon resolves to the wrong row.
                if ($enumText -match "\b$([regex]::Escape($entry.Key))\s*=\s*(\d+)") {
                    $enumValue = [int]$Matches[1]
                    if ($enumValue -ne $actualRow.Row) {
                        $errors.Add("StatusEffect '$($entry.Key)' has EffectIconType value $enumValue but occupies effecticons.2da row $($actualRow.Row); the enum value must equal the row number or the icon resolves to the wrong row at runtime.") | Out-Null
                    }
                }

                if (!$statusEffectStrRefsByKey.ContainsKey($entry.Key)) {
                    $errors.Add("StatusEffect '$($entry.Key)' has no resolved custom TLK StrRef.") | Out-Null
                }
                else {
                    $expectedStrRef = $statusEffectStrRefsByKey[$entry.Key]
                    if ($actualRow.StrRef -ne $expectedStrRef) {
                        $errors.Add("StatusEffect '$($entry.Key)' effecticons.2da StrRef '$($actualRow.StrRef)' should be '$expectedStrRef'.") | Out-Null
                    }
                }
            }
        }
    }

    foreach ($ability in ($abilityRowsByKey.Values | Sort-Object Key)) {
        if (!$manifestAbilityKeys.ContainsKey($ability.Key)) {
            $errors.Add("Ability '$($ability.Key)' in $Feat2daPath is missing from the gameplay icon manifest.") | Out-Null
        }
    }

    $duplicates = $rows | Group-Object IconResRef | Where-Object { $_.Name -and $_.Count -gt 1 }
    foreach ($duplicate in $duplicates) {
        $members = ($duplicate.Group | ForEach-Object { "$($_.Type):$($_.Key)" }) -join ", "
        $errors.Add("Duplicate gameplay icon '$($duplicate.Name)' used by $members.") | Out-Null
    }

    foreach ($entry in ($rows | Where-Object { $_.Type -eq "Ability" -and $_.IconResRef.StartsWith("ife_", [System.StringComparison]::OrdinalIgnoreCase) })) {
        $suffix = $entry.IconResRef.Substring(4)
        foreach ($stage in 0..5) {
            $cooldownFile = Join-Path $iconDirectory "pr$($stage)_$suffix.tga"
            if (!(Test-Path -LiteralPath $cooldownFile)) {
                $errors.Add("Ability '$($entry.Key)' is missing cooldown icon '$cooldownFile'.") | Out-Null
            }
            else {
                Add-TgaValidationErrors $errors $cooldownFile "Ability '$($entry.Key)' cooldown pr$stage"
            }
        }
    }

    if ($errors.Count -gt 0) {
        throw "Gameplay icon standards audit failed:`n$($errors -join "`n")"
    }

    Write-Host "Gameplay icon standards audit passed for $($rows.Count) manifest entries."
}

$manifestResolved = if (Test-Path -LiteralPath $ManifestPath) { (Resolve-Path -Path $ManifestPath).Path } else { Join-Path (Get-Location).Path $ManifestPath }
$existingManifest = Import-ExistingManifest $manifestResolved

if ($RefreshManifest -or !(Test-Path -LiteralPath $manifestResolved)) {
    $rows = @(Build-ManifestRows $existingManifest)
    Write-Manifest $rows $manifestResolved
    $existingManifest = Import-ExistingManifest $manifestResolved
    Write-Host "Wrote gameplay icon manifest with $($rows.Count) entries."
}

$rows = @(Build-ManifestRows $existingManifest)
$script:RankBadgeByResRef = Get-RankBadgeMap $rows
$statusRows = @($rows | Where-Object { $_.Type -eq "StatusEffect" } | Sort-Object Key)

if (![string]::IsNullOrWhiteSpace($SampleOutputPath)) {
    Export-StatusIconSamples $statusRows $SampleOutputPath $SampleIconResRefs
    return
}

$tlkTextToStrRef = Get-CustomTlkTextToStrRef (Resolve-RepoPath $TlkJsonPath)
$statusEffectStrRefsByKey = Get-StatusEffectStrRefsByKey $statusRows $tlkTextToStrRef

if ($UpdateStatusEffectCode) {
    Update-EffectIconTypeEnum $statusRows (Resolve-RepoPath $EffectIconTypePath)
    Update-EffectIcons2da $statusRows (Resolve-RepoPath $EffectIcons2daPath) $statusEffectStrRefsByKey
    Update-StatusEffectCode $statusRows
    Write-Host "Updated EffectIconType, effecticons.2da, and $($statusRows.Count) status effect icon properties."
}

if ($GenerateIcons) {
    $restoreAbilityIcons = Join-Path $PSScriptRoot "RestoreAbilityIconArtwork.ps1"
    & $restoreAbilityIcons `
        -ManifestPath $ManifestPath `
        -IconOutputPath $IconPath `
        -IconSize $IconSize
    if (!$?) {
        throw "$restoreAbilityIcons failed."
    }

    $linkCombatFeatSpells = Join-Path $PSScriptRoot "LinkCombatUpgradeFeatSpells.ps1"
    if (Test-Path -LiteralPath $linkCombatFeatSpells) {
        & $linkCombatFeatSpells `
            -Feat2daPath $Feat2daPath `
            -Spells2daPath $Spells2daPath `
            -GeneratedFeatStart $GeneratedFeatStart `
            -GeneratedFeatEnd $GeneratedFeatEnd
        if (!$?) {
            throw "$linkCombatFeatSpells failed."
        }
    }

    $rows = @(Build-ManifestRows (Import-ExistingManifest $manifestResolved))
    $statusRows = @($rows | Where-Object { $_.Type -eq "StatusEffect" } | Sort-Object Key)
    Generate-StatusIcons $statusRows (Resolve-RepoPath $IconPath)

    $generateCooldownIcons = Join-Path $PSScriptRoot "GenerateCooldownIcons.ps1"
    & $generateCooldownIcons `
        -Feat2daPath $Feat2daPath `
        -IconPath $IconPath `
        -GeneratedFeatStart $GeneratedFeatStart `
        -GeneratedFeatEnd $GeneratedFeatEnd `
        -IconSize $IconSize `
        -Force
    if (!$?) {
        throw "$generateCooldownIcons failed."
    }

    Write-Host "Generated ability, status effect, and cooldown icons."
}

if ($RefreshManifest -or $GenerateIcons -or $UpdateStatusEffectCode) {
    $rows = @(Build-ManifestRows (Import-ExistingManifest $manifestResolved))
    Write-Manifest $rows $manifestResolved
}
else {
    $rows = @(Import-Csv -Path $manifestResolved)
}

$statusRows = @($rows | Where-Object { $_.Type -eq "StatusEffect" } | Sort-Object Key)
$tlkTextToStrRef = Get-CustomTlkTextToStrRef (Resolve-RepoPath $TlkJsonPath)
$statusEffectStrRefsByKey = Get-StatusEffectStrRefsByKey $statusRows $tlkTextToStrRef

Test-GameplayIconStandards $rows $statusEffectStrRefsByKey
