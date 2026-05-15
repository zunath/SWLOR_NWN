param(
    [string]$Feat2daPath = "SWLOR_Haks\swlor2_2da\feat.2da",
    [string]$IconPath = "SWLOR_Haks\swlor2_tga",
    [int]$GeneratedFeatStart = 2000,
    [int]$GeneratedFeatEnd = 2558
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$WordAliases = @{
    Absolute = "abs"; Adamantine = "adam"; Aimed = "aim"; Shot = "shot"; Anger = "angr"; Arc = "arc"; Strike = "strk"
    Bastion = "bast"; Stance = "stnc"; Berserker = "bers"; Binding = "bind"; Cross = "crs"; Blade = "blade"; Vortex = "vort"
    Blazing = "blaz"; Spikes = "spk"; Bloodlust = "blood"; Bombardier = "bomb"; Bonecrusher = "bone"; Breach = "brch"; Round = "rnd"
    Breaker = "brkr"; Reversal = "rev"; Brutal = "brut"; Assault = "aslt"; Calming = "calm"; Carve = "carv"; Cascade = "casc"; Failure = "fail"
    Centering = "cent"; Cheap = "cheap"; Cluster = "clstr"; Storm = "strm"; Cobra = "cobra"; Concussive = "conc"; Toss = "toss"
    Conduit = "cond"; Flare = "flar"; Covering = "cov"; Claws = "claw"; Crippling = "crip"; Defense = "def"; Defensive = "def"
    Crusher = "crush"; Crushing = "crush"; Blow = "blow"; Current = "curr"; Overload = "ovld"; Cyclone = "cycl"; Deadeye = "eye"
    Deadly = "dead"; Precision = "prec"; Dead = "dead"; Man = "man"; Hand = "hand"; Debilitating = "debil"; Decoy = "decoy"
    Disabling = "disab"; Disarming = "disarm"; Disruption = "disrp"; Field = "fld"; Duelist = "duel"; Challenge = "chal"
    Earthshatter = "earth"; Edge = "edge"; Of = ""; Darkness = "dark"; Enfeebling = "enfb"; Essence = "ess"; Hunter = "hunt"
    Evasive = "evas"; Combat = "cmbt"; Expose = "xpose"; Weak = "weak"; Point = "pt"; Fan = "fan"; The = ""; Hammer = "hamr"
    Feinting = "feint"; Cut = "cut"; Ferocity = "feroc"; Final = "final"; Form = "form"; Finishing = "fin"; Fireburst = "fire"
    Flanking = "flank"; Barrage = "barr"; Flash = "flash"; Focused = "focus"; Forcebane = "fbane"; Force = "force"; Capacitor = "cap"
    Gyre = "gyre"; Lens = "lens"; Nullification = "null"; Suppression = "sup"; Fortress = "fort"; Fracture = "fract"
    Ground = "grnd"; Quake = "quake"; Guard = "grd"; Counter = "cntr"; Guarded = "grded"; Channel = "chan"; Guardian = "guard"
    Master = "mstr"; Influence = "infl"; Resolve = "res"; Guarding = "grd"; Step = "step"; Gunfighter = "gunf"; Gunslinger = "guns"
    Focus = "foc"; Hampering = "hamp"; Hamstring = "hamstr"; Headshot = "head"; Impenetrable = "impen"; Improved = "imp"
    Attentiveness = "atten"; Incapacitate = "incap"; Infinite = "inf"; Interrupting = "intr"; Interruption = "intr"; Invincible = "invin"
    Iron = "iron"; Elbows = "elbw"; Wall = "wall"; Kill = "kill"; Zone = "zone"; Last = "last"; Word = "word"; Leg = "leg"
    Slash = "slsh"; Life = "life"; Siphon = "siph"; Line = "line"; Low = "low"; Maelstrom = "mael"; Marked = "mark"; For = ""
    Death = "death"; Marking = "mark"; Nerve = "nerv"; Neural = "neural"; Shock = "shok"; Neutralizing = "neut"; One = "one"
    Overwatch = "ovrw"; Overwhelming = "ovrw"; Pacification = "pac"; Perceptive = "percp"; Perfect = "perf"; Throw = "throw"
    Piercing = "pierc"; Pinning = "pin"; Fire = "fire"; Blank = "blank"; Burst = "burst"; Punishing = "pun"; Purify = "pur"
    Rain = "rain"; Steel = "steel"; Rampart = "ramp"; Rending = "rend"; Rib = "rib"; Ricochet = "rico"; Ripple = "ripl"
    Saber = "sabr"; Sacrificial = "sac"; Sap = "sap"; Vitality = "vit"; Saturation = "sat"; Savage = "sav"; Cleave = "clv"
    Second = "sec"; Wind = "wind"; Sentinel = "sent"; Serpent = "serp"; Eclipse = "ecl"; Sever = "sev"; Severing = "sev"
    Shadow = "shdw"; Shelter = "shel"; Circle = "circ"; Shield = "shld"; Side = "side"; Skirmisher = "skirm"; Skull = "skul"
    Rattle = "ratl"; Smoke = "smok"; Snap = "snap"; Roll = "roll"; Sniper = "snip"; Soul = "soul"; Ascension = "asc"
    Devourer = "dev"; Sacrifice = "sac"; Split = "split"; Spotter = "spot"; Stasis = "stas"; Volley = "vol"; Static = "stat"
    Palm = "palm"; Systemic = "sys"; Shutdown = "shut"; Tactical = "tact"; Escape = "esc"; Taunting = "taunt"; Deflection = "defl"
    Tempest = "temp"; Release = "rel"; Total = "tot"; Denial = "den"; Toxic = "tox"; Coating = "coat"; Rush = "rush"
    Tranq = "tranq"; Cone = "cone"; Twin = "twin"; Fang = "fang"; Flurry = "flur"; Intercept = "intc"; Unmoving = "unmov"
    Venom = "ven"; Splash = "spl"; Versatile = "vers"; Vital = "vital"; Whirling = "whirl"; Whirlwind = "wwind"; Worldbreaker = "worldbrk"
}

function Split-LabelWords([string]$Label) {
    $base = $Label -replace "\d+$", ""
    $matches = [regex]::Matches($base, "[A-Z]+(?=[A-Z][a-z]|\d|$)|[A-Z]?[a-z]+")
    return @($matches | ForEach-Object { $_.Value } | Where-Object { $_ -and $_ -ne "S" })
}

function ConvertTo-Base36([int]$Value) {
    $digits = "0123456789abcdefghijklmnopqrstuvwxyz"
    if ($Value -eq 0) {
        return "0"
    }

    $remaining = $Value
    $result = ""
    while ($remaining -gt 0) {
        $index = [int]($remaining % 36)
        $result = $digits[$index] + $result
        $remaining = [int][Math]::Floor($remaining / 36)
    }

    return $result
}

function New-CombatUpgradeResref([string]$Label, [int]$Row, [hashtable]$Seen) {
    $level = ""
    if ($Label -match "(\d+)$") {
        $level = $Matches[1]
    }

    $chunks = foreach ($word in (Split-LabelWords $Label)) {
        if ($null -ne $WordAliases[$word]) {
            $WordAliases[$word]
        }
        else {
            ($word.ToLowerInvariant() -replace "[aeiou]", "")
        }
    }

    $chunks = @($chunks | Where-Object { $_ })
    if ($chunks.Count -eq 0) {
        $chunks = @($Label.ToLowerInvariant().Substring(0, [Math]::Min(8, $Label.Length)))
    }

    $body = ($chunks -join "")
    $maxBodyLength = 12 - $level.Length
    if ($maxBodyLength -lt 6) {
        $maxBodyLength = 6
    }

    if ($body.Length -gt $maxBodyLength) {
        $body = $body.Substring(0, $maxBodyLength)
    }

    $baseCandidate = "ife_$body$level".ToLowerInvariant()
    if ($baseCandidate.Length -gt 16) {
        $baseCandidate = $baseCandidate.Substring(0, 16)
    }

    $candidate = $baseCandidate
    $attempt = 0
    while ($Seen.ContainsKey($candidate)) {
        $suffix = ConvertTo-Base36 ($Row + ($attempt * 4096))
        $candidate = $baseCandidate.Substring(0, [Math]::Min($baseCandidate.Length, 16 - $suffix.Length)) + $suffix
        $attempt++
    }

    $Seen[$candidate] = $true
    return $candidate
}

$featResolved = Resolve-Path $Feat2daPath
$iconResolved = Resolve-Path $IconPath
$lines = [System.Collections.Generic.List[string]]::new()
$lines.AddRange([System.IO.File]::ReadAllLines($featResolved))
$seen = @{}
$renamed = 0
$updated = 0

for ($i = 0; $i -lt $lines.Count; $i++) {
    $line = $lines[$i]
    if ($line.Trim().Length -eq 0) {
        continue
    }

    $parts = $line -split "\s+"
    if ($parts.Count -lt 5) {
        continue
    }

    $row = 0
    if (-not [int]::TryParse($parts[0], [ref]$row)) {
        continue
    }

    if ($row -lt $GeneratedFeatStart -or $row -gt $GeneratedFeatEnd -or $parts[1] -eq "****") {
        continue
    }

    $oldResref = $parts[4]
    $newResref = New-CombatUpgradeResref $parts[1] $row $seen

    if ($oldResref -ne $newResref) {
        $oldFile = Join-Path $iconResolved "$oldResref.tga"
        $newFile = Join-Path $iconResolved "$newResref.tga"

        if ((Test-Path -LiteralPath $oldFile) -and -not (Test-Path -LiteralPath $newFile)) {
            Move-Item -LiteralPath $oldFile -Destination $newFile
            $renamed++
        }
        elseif ((Test-Path -LiteralPath $oldFile) -and (Test-Path -LiteralPath $newFile)) {
            Remove-Item -LiteralPath $newFile
            Move-Item -LiteralPath $oldFile -Destination $newFile
            $renamed++
        }
        elseif (-not (Test-Path -LiteralPath $newFile)) {
            throw "Missing icon for row $row. Expected either '$oldFile' or '$newFile'."
        }

        $parts[4] = $newResref
        $lines[$i] = ($parts -join " ")
        $updated++
    }
}

[System.IO.File]::WriteAllLines($featResolved, $lines)

Write-Host "Renamed $renamed files and updated $updated feat.2da rows."
