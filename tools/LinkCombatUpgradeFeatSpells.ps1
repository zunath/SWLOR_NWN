[CmdletBinding()]
param(
    [string]$Feat2daPath = "SWLOR_Haks\sw_2da\feat.2da",
    [string]$Spells2daPath = "SWLOR_Haks\sw_2da\spells.2da",
    [string]$ClassFeat2daPath = "SWLOR_Haks\sw_2da\CLS_FEAT_FIGHT.2da",
    [string]$SpellEnumPath = "SWLOR.NWN.API\NWScript\Enum\spell.cs",
    [string]$FeatEnumPath = "SWLOR.NWN.API\NWScript\Enum\FeatType.cs",
    [int]$GeneratedFeatStart = 2000,
    [int]$GeneratedFeatEnd = 2899,
    [int]$ManualHotbarClassFeatRowLimit = 1024,
    [string[]]$ManualHotbarFeatLabels = @(
        "ForceLightning3",
        "ForceJudgment1",
        "ForceJudgment2",
        "ForceJudgment3",
        "ForceBurst1",
        "PurifyingWave1",
        "RadiantLance1",
        "RadiantLance2",
        "RadiantLance3"
    )
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot

function Resolve-RepoPath {
    param([string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return $Path
    }

    return Join-Path $repoRoot $Path
}

function Get-HeaderLineIndex {
    param([string[]]$Lines)

    for ($i = 1; $i -lt $Lines.Count; $i++) {
        if (![string]::IsNullOrWhiteSpace($Lines[$i])) {
            return $i
        }
    }

    throw "Could not locate 2DA header line."
}

function Format-2DARow {
    param(
        [string[]]$Tokens,
        [int[]]$Widths
    )

    $parts = for ($i = 0; $i -lt $Tokens.Count; $i++) {
        $width = if ($i -lt $Widths.Count) { $Widths[$i] } else { 8 }
        $Tokens[$i].PadRight($width)
    }

    return ($parts -join "").TrimEnd()
}

function Get-RowNumber {
    param([System.Collections.Generic.IList[string]]$Tokens)

    $rowNumber = 0
    if ($Tokens.Count -eq 0 -or ![int]::TryParse($Tokens[0], [ref]$rowNumber)) {
        return $null
    }

    return $rowNumber
}

function Set-TokenByHeader {
    param(
        [System.Collections.Generic.IList[string]]$Tokens,
        [string[]]$Headers,
        [string]$Header,
        [string]$Value
    )

    $headerIndex = [array]::IndexOf($Headers, $Header)
    if ($headerIndex -lt 0) {
        throw "Could not find column '$Header'."
    }

    $Tokens[$headerIndex + 1] = $Value
}

function Get-TokenByHeader {
    param(
        [System.Collections.Generic.IList[string]]$Tokens,
        [string[]]$Headers,
        [string]$Header
    )

    $headerIndex = [array]::IndexOf($Headers, $Header)
    if ($headerIndex -lt 0) {
        throw "Could not find column '$Header'."
    }

    return $Tokens[$headerIndex + 1]
}

function Convert-ToStringList {
    param([string]$Line)

    $list = [System.Collections.Generic.List[string]]::new()
    $list.AddRange([string[]]($Line.Trim() -split "\s+"))

    return ,$list
}

$featPath = Resolve-RepoPath $Feat2daPath
$spellsPath = Resolve-RepoPath $Spells2daPath
$classFeatPath = Resolve-RepoPath $ClassFeat2daPath
$enumPath = Resolve-RepoPath $SpellEnumPath
$featEnumPath = Resolve-RepoPath $FeatEnumPath
$abilityDefinitionPath = Resolve-RepoPath "SWLOR.Game.Server\Feature\AbilityDefinition"
$npcAbilityDefinitionPath = Resolve-RepoPath "SWLOR.Game.Server\Feature\AbilityDefinition\NPC"

$npcAbilityLabels = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
if ([System.IO.Directory]::Exists($npcAbilityDefinitionPath)) {
    Get-ChildItem $npcAbilityDefinitionPath -Filter "*.cs" -File |
        ForEach-Object {
            $content = [System.IO.File]::ReadAllText($_.FullName)
            foreach ($match in [regex]::Matches($content, "\bFeatType\.(\w+)")) {
                $npcAbilityLabels.Add($match.Groups[1].Value) | Out-Null
            }
        }
}

$playerAbilityLabels = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
$mimicryTraitLabels = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
if ([System.IO.Directory]::Exists($abilityDefinitionPath)) {
    Get-ChildItem $abilityDefinitionPath -Filter "*.cs" -File -Recurse |
        Where-Object { $_.FullName -notlike "*\Feature\AbilityDefinition\NPC\*" } |
        ForEach-Object {
            $content = [System.IO.File]::ReadAllText($_.FullName)
            if ($content -notmatch "\bIAbilityListDefinition\b") {
                return
            }

            foreach ($match in [regex]::Matches($content, "\bFeatType\.(\w+)")) {
                $playerAbilityLabels.Add($match.Groups[1].Value) | Out-Null
            }

            if ($content -match "\.MimicryTrait\s*\(") {
                foreach ($match in [regex]::Matches($content, "\.Create\s*\(\s*FeatType\.(\w+)")) {
                    $mimicryTraitLabels.Add($match.Groups[1].Value) | Out-Null
                }
            }
        }
}

$selfTargetingLabels = @(
    "FlurryStance1",
    "GamblerStance1",
    "LaceratorStance1",
    "OrdnanceStance1",
    "ScrapperStance1",
    "ShadowflowStance1",
    "SuppressionStance1",
    "VigorStance1"
)
$hostileTargetingLabels = @("Hamstring1", "Hamstring2", "Hamstring3")

$featTargetSelfByLabel = @{}
$spellTargetingByLabel = @{}
$selfSpellTargetingProfile = @{
    Range = "P"
    TargetType = "0x01"
    HostileSetting = "0"
    TargetShape = "****"
    TargetSizeX = "****"
    TargetSizeY = "****"
    TargetFlags = "****"
}
$hostileSpellTargetingProfile = @{
    Range = "M"
    TargetType = "0x03"
    HostileSetting = "0"
    TargetShape = "****"
    TargetSizeX = "****"
    TargetSizeY = "****"
    TargetFlags = "****"
}

foreach ($label in $selfTargetingLabels) {
    $featTargetSelfByLabel[$label] = "1"
    $spellTargetingByLabel[$label] = $selfSpellTargetingProfile
}

foreach ($label in $hostileTargetingLabels) {
    $featTargetSelfByLabel[$label] = "****"
    $spellTargetingByLabel[$label] = $hostileSpellTargetingProfile
}

function Apply-SpellTargetingProfile {
    param(
        [System.Collections.Generic.IList[string]]$SpellTokens,
        [string[]]$Headers,
        [string]$Label
    )

    if (!$spellTargetingByLabel.ContainsKey($Label)) {
        return
    }

    $profile = $spellTargetingByLabel[$Label]
    foreach ($entry in $profile.GetEnumerator()) {
        Set-TokenByHeader $SpellTokens $Headers $entry.Key $entry.Value
    }
}

$featLines = [System.Collections.Generic.List[string]]::new()
$featLines.AddRange([System.IO.File]::ReadAllLines($featPath))
$featHeaderIndex = Get-HeaderLineIndex $featLines.ToArray()
$featHeaders = $featLines[$featHeaderIndex].Trim() -split "\s+"
$featExpectedTokens = $featHeaders.Count + 1
$featColumnWidths = @(
    7, 49, 11, 14, 19, 17, 9, 9, 9, 9, 9, 9, 13, 14, 14, 15, 15, 19,
    11, 8, 10, 12, 10, 13, 13, 13, 13, 13, 13, 13, 11, 18, 12, 20, 49,
    18, 14, 11, 16, 11, 13, 13, 12
)

$spellsLines = [System.Collections.Generic.List[string]]::new()
$spellsLines.AddRange([System.IO.File]::ReadAllLines($spellsPath))
$spellsHeaderIndex = Get-HeaderLineIndex $spellsLines.ToArray()
$spellsHeaders = $spellsLines[$spellsHeaderIndex].Trim() -split "\s+"
$spellsExpectedTokens = $spellsHeaders.Count + 1
$spellColumnWidths = @(
    7, 36, 11, 19, 9, 8, 7, 12, 13, 19, 7, 9, 8, 10, 9, 11, 9, 11,
    18, 18, 18, 19, 19, 19, 11, 11, 17, 17, 17, 19, 7, 19, 15, 15,
    19, 18, 17, 14, 14, 14, 14, 14, 14, 11, 9, 11, 12, 19, 20, 13,
    17, 12, 11, 11, 19, 13, 13, 13, 12
)
$classFeatColumnWidths = @(7, 49, 12, 7, 17, 9)

$spellRowsByLabel = @{}
$spellLineByRow = @{}
$blankGeneratedSpellRows = [System.Collections.Generic.Queue[object]]::new()
$maxSpellRow = 0
for ($i = $spellsHeaderIndex + 1; $i -lt $spellsLines.Count; $i++) {
    $tokens = Convert-ToStringList $spellsLines[$i]
    $rowNumber = Get-RowNumber $tokens
    if ($null -eq $rowNumber) {
        continue
    }

    if ($rowNumber -gt $maxSpellRow) {
        $maxSpellRow = $rowNumber
    }
    $spellLineByRow[$rowNumber] = $i

    if ($tokens.Count -gt 1 -and $tokens[1] -ne "****") {
        $spellRowsByLabel[$tokens[1]] = $rowNumber
    }
    elseif ($rowNumber -gt 1016) {
        $blankGeneratedSpellRows.Enqueue([pscustomobject]@{
            Row = $rowNumber
            LineIndex = $i
        })
    }
}

$generatedSpellRows = New-Object System.Collections.Generic.List[string]
$linkedFeatRows = 0
$createdSpellRows = 0
$spellIdsByLabel = [ordered]@{}
$linkedPlayerFeats = [ordered]@{}

for ($i = $featHeaderIndex + 1; $i -lt $featLines.Count; $i++) {
    $tokens = Convert-ToStringList $featLines[$i]
    $rowNumber = Get-RowNumber $tokens
    if ($null -eq $rowNumber -or $rowNumber -lt $GeneratedFeatStart -or $rowNumber -gt $GeneratedFeatEnd) {
        continue
    }
    if ($tokens.Count -ne $featExpectedTokens) {
        throw "Feat row $rowNumber has $($tokens.Count) tokens, expected $featExpectedTokens."
    }

    $label = Get-TokenByHeader $tokens $featHeaders "LABEL"
    if ($label -eq "****") {
        continue
    }
    if ($npcAbilityLabels.Contains($label)) {
        continue
    }
    if (!$playerAbilityLabels.Contains($label)) {
        Set-TokenByHeader $tokens $featHeaders "SPELLID" "****"
        $featLines[$i] = Format-2DARow $tokens.ToArray() $featColumnWidths
        continue
    }

    # Mimicry technique spell rows already own curated, meaningful icon resources. Keep the
    # feat/action-menu icon on that same artwork instead of allowing generated opaque resrefs to
    # overwrite the spell icon and point at nonexistent files.
    if ($label.EndsWith("Technique", [System.StringComparison]::Ordinal) -and
        $spellRowsByLabel.ContainsKey($label)) {
        $techniqueSpellId = $spellRowsByLabel[$label]
        $techniqueSpellLineIndex = $spellLineByRow[$techniqueSpellId]
        $techniqueSpellTokens = Convert-ToStringList $spellsLines[$techniqueSpellLineIndex]
        $techniqueIcon = Get-TokenByHeader $techniqueSpellTokens $spellsHeaders "IconResRef"
        if (![string]::IsNullOrWhiteSpace($techniqueIcon) -and $techniqueIcon -ne "****") {
            Set-TokenByHeader $tokens $featHeaders "ICON" $techniqueIcon
        }
    }

    # Learned passive traits appear in the Techniques menu but are never cast. They need their
    # feat and class-feat rows, while SPELLID must remain the blank sentinel.
    if ($mimicryTraitLabels.Contains($label)) {
        Set-TokenByHeader $tokens $featHeaders "SPELLID" "****"
        $featLines[$i] = Format-2DARow $tokens.ToArray() $featColumnWidths
        $linkedPlayerFeats[$label] = $rowNumber
        continue
    }

    if ($featTargetSelfByLabel.ContainsKey($label)) {
        Set-TokenByHeader $tokens $featHeaders "TARGETSELF" $featTargetSelfByLabel[$label]
    }

    $spellId = 0
    if ($spellRowsByLabel.ContainsKey($label)) {
        $spellId = $spellRowsByLabel[$label]
        $spellLineIndex = $spellLineByRow[$spellId]
        $spellTokens = Convert-ToStringList $spellsLines[$spellLineIndex]
        Set-TokenByHeader $spellTokens $spellsHeaders "Label" $label
        Set-TokenByHeader $spellTokens $spellsHeaders "Name" (Get-TokenByHeader $tokens $featHeaders "FEAT")
        $featIcon = Get-TokenByHeader $tokens $featHeaders "ICON"
        $currentIcon = Get-TokenByHeader $spellTokens $spellsHeaders "IconResRef"
        if ($featIcon -ne "default_perk" -or $currentIcon -eq "****") {
            Set-TokenByHeader $spellTokens $spellsHeaders "IconResRef" $featIcon
        }
        Set-TokenByHeader $spellTokens $spellsHeaders "SpellDesc" (Get-TokenByHeader $tokens $featHeaders "DESCRIPTION")
        Set-TokenByHeader $spellTokens $spellsHeaders "FeatID" $rowNumber.ToString()
        Apply-SpellTargetingProfile $spellTokens $spellsHeaders $label
        $spellsLines[$spellLineIndex] = Format-2DARow $spellTokens.ToArray() $spellColumnWidths
    }
    else {
        $reuseBlankRow = $blankGeneratedSpellRows.Count -gt 0
        $blankRow = $null
        if ($reuseBlankRow) {
            $blankRow = $blankGeneratedSpellRows.Dequeue()
            $spellId = $blankRow.Row
        }
        else {
            $maxSpellRow++
            $spellId = $maxSpellRow
        }

        $spellTokens = New-Object System.Collections.Generic.List[string]
        for ($j = 0; $j -lt $spellsExpectedTokens; $j++) {
            $spellTokens.Add("****") | Out-Null
        }

        $spellTokens[0] = $spellId.ToString()
        Set-TokenByHeader $spellTokens $spellsHeaders "Label" $label
        Set-TokenByHeader $spellTokens $spellsHeaders "Name" (Get-TokenByHeader $tokens $featHeaders "FEAT")
        Set-TokenByHeader $spellTokens $spellsHeaders "IconResRef" (Get-TokenByHeader $tokens $featHeaders "ICON")
        Set-TokenByHeader $spellTokens $spellsHeaders "School" "V"
        Set-TokenByHeader $spellTokens $spellsHeaders "Range" "M"
        Set-TokenByHeader $spellTokens $spellsHeaders "VS" "-"
        Set-TokenByHeader $spellTokens $spellsHeaders "TargetType" "0x03"
        Set-TokenByHeader $spellTokens $spellsHeaders "Innate" "1"
        Set-TokenByHeader $spellTokens $spellsHeaders "ConjTime" "0"
        Set-TokenByHeader $spellTokens $spellsHeaders "ConjAnim" "head"
        Set-TokenByHeader $spellTokens $spellsHeaders "CastAnim" "out"
        Set-TokenByHeader $spellTokens $spellsHeaders "CastTime" "0"
        Set-TokenByHeader $spellTokens $spellsHeaders "Proj" "0"
        Set-TokenByHeader $spellTokens $spellsHeaders "ItemImmunity" "0"
        Set-TokenByHeader $spellTokens $spellsHeaders "Category" "10"
        Set-TokenByHeader $spellTokens $spellsHeaders "UserType" "2"
        Set-TokenByHeader $spellTokens $spellsHeaders "SpellDesc" (Get-TokenByHeader $tokens $featHeaders "DESCRIPTION")
        Set-TokenByHeader $spellTokens $spellsHeaders "UseConcentration" "0"
        Set-TokenByHeader $spellTokens $spellsHeaders "SpontaneouslyCast" "0"
        Set-TokenByHeader $spellTokens $spellsHeaders "AltMessage" "53220"
        Set-TokenByHeader $spellTokens $spellsHeaders "HostileSetting" "0"
        Set-TokenByHeader $spellTokens $spellsHeaders "FeatID" $rowNumber.ToString()
        Set-TokenByHeader $spellTokens $spellsHeaders "HasProjectile" "0"
        Apply-SpellTargetingProfile $spellTokens $spellsHeaders $label

        $formattedSpellRow = Format-2DARow $spellTokens.ToArray() $spellColumnWidths
        if ($reuseBlankRow) {
            $spellsLines[$blankRow.LineIndex] = $formattedSpellRow
        }
        else {
            $generatedSpellRows.Add($formattedSpellRow) | Out-Null
        }

        $spellRowsByLabel[$label] = $spellId
        $createdSpellRows++
    }

    Set-TokenByHeader $tokens $featHeaders "SPELLID" $spellId.ToString()
    $featLines[$i] = Format-2DARow $tokens.ToArray() $featColumnWidths
    $spellIdsByLabel[$label] = $spellId
    $linkedPlayerFeats[$label] = $rowNumber
    $linkedFeatRows++
}

foreach ($row in $generatedSpellRows) {
    $spellsLines.Add($row) | Out-Null
}

[System.IO.File]::WriteAllLines($featPath, $featLines)
[System.IO.File]::WriteAllLines($spellsPath, $spellsLines)

$addedClassFeatRows = 0
if ([System.IO.File]::Exists($classFeatPath)) {
    $classFeatLines = [System.Collections.Generic.List[string]]::new()
    $classFeatLines.AddRange([System.IO.File]::ReadAllLines($classFeatPath))
    $classFeatHeaderIndex = Get-HeaderLineIndex $classFeatLines.ToArray()
    $classFeatHeaders = $classFeatLines[$classFeatHeaderIndex].Trim() -split "\s+"
    $classFeatExpectedTokens = $classFeatHeaders.Count + 1
    $classFeatLineByFeatIndex = @{}
    $availableManualClassFeatLines = [System.Collections.Generic.Queue[int]]::new()
    $activePlayerFeatIndexes = [System.Collections.Generic.HashSet[int]]::new()
    foreach ($featIndexValue in $linkedPlayerFeats.Values) {
        $activePlayerFeatIndexes.Add([int]$featIndexValue) | Out-Null
    }
    $maxClassFeatRow = 0

    for ($i = $classFeatHeaderIndex + 1; $i -lt $classFeatLines.Count; $i++) {
        $tokens = Convert-ToStringList $classFeatLines[$i]
        $rowNumber = Get-RowNumber $tokens
        if ($null -eq $rowNumber) {
            continue
        }

        if ($rowNumber -gt $maxClassFeatRow) {
            $maxClassFeatRow = $rowNumber
        }

        if ($tokens.Count -eq $classFeatExpectedTokens) {
            $featLabel = Get-TokenByHeader -Tokens $tokens -Headers $classFeatHeaders -Header "FeatLabel"
            $featIndex = Get-TokenByHeader -Tokens $tokens -Headers $classFeatHeaders -Header "FeatIndex"
            if ($featIndex -ne "****" -and !$classFeatLineByFeatIndex.ContainsKey($featIndex)) {
                $classFeatLineByFeatIndex[$featIndex] = $i
            }
            elseif ($featLabel -eq "****" -and
                    $featIndex -eq "****" -and
                    $rowNumber -lt $ManualHotbarClassFeatRowLimit) {
                $availableManualClassFeatLines.Enqueue($i)
            }
        }
    }

    foreach ($entry in @($classFeatLineByFeatIndex.GetEnumerator())) {
        $featIndex = 0
        if (![int]::TryParse($entry.Key, [ref]$featIndex)) {
            continue
        }

        if ($featIndex -lt $GeneratedFeatStart -or $featIndex -gt $GeneratedFeatEnd) {
            continue
        }

        if ($activePlayerFeatIndexes.Contains($featIndex)) {
            continue
        }

        $lineIndex = $entry.Value
        $tokens = Convert-ToStringList $classFeatLines[$lineIndex]
        Set-TokenByHeader -Tokens $tokens -Headers $classFeatHeaders -Header "List" -Value "0"
        Set-TokenByHeader -Tokens $tokens -Headers $classFeatHeaders -Header "OnMenu" -Value "0"
        $classFeatLines[$lineIndex] = Format-2DARow $tokens.ToArray() $classFeatColumnWidths
    }

    foreach ($entry in $linkedPlayerFeats.GetEnumerator()) {
        $featLabel = $entry.Key
        $featIndex = $entry.Value.ToString()
        $isManualHotbarFeat = $ManualHotbarFeatLabels -contains $featLabel

        if ($classFeatLineByFeatIndex.ContainsKey($featIndex)) {
            $lineIndex = $classFeatLineByFeatIndex[$featIndex]
            $tokens = Convert-ToStringList $classFeatLines[$lineIndex]
            $classFeatRowNumber = Get-RowNumber $tokens
            if ($isManualHotbarFeat -and $classFeatRowNumber -ge $ManualHotbarClassFeatRowLimit) {
                if ($availableManualClassFeatLines.Count -eq 0) {
                    throw "No empty class-feat row below $ManualHotbarClassFeatRowLimit is available for manual hotbar feat '$featLabel'."
                }

                $sourceLineIndex = $lineIndex
                $lineIndex = $availableManualClassFeatLines.Dequeue()
                $tokens = Convert-ToStringList $classFeatLines[$lineIndex]

                $sourceTokens = Convert-ToStringList $classFeatLines[$sourceLineIndex]
                for ($j = 1; $j -lt $classFeatExpectedTokens; $j++) {
                    $sourceTokens[$j] = "****"
                }
                $classFeatLines[$sourceLineIndex] = Format-2DARow $sourceTokens.ToArray() $classFeatColumnWidths
                $classFeatLineByFeatIndex[$featIndex] = $lineIndex
            }

            Set-TokenByHeader -Tokens $tokens -Headers $classFeatHeaders -Header "FeatLabel" -Value $featLabel
            Set-TokenByHeader -Tokens $tokens -Headers $classFeatHeaders -Header "FeatIndex" -Value $featIndex
            Set-TokenByHeader -Tokens $tokens -Headers $classFeatHeaders -Header "List" -Value "1"
            Set-TokenByHeader -Tokens $tokens -Headers $classFeatHeaders -Header "GrantedOnLevel" -Value "99"
            Set-TokenByHeader -Tokens $tokens -Headers $classFeatHeaders -Header "OnMenu" -Value "1"
            $classFeatLines[$lineIndex] = Format-2DARow $tokens.ToArray() $classFeatColumnWidths
            continue
        }

        $lineIndex = $null
        if ($isManualHotbarFeat) {
            if ($availableManualClassFeatLines.Count -eq 0) {
                throw "No empty class-feat row below $ManualHotbarClassFeatRowLimit is available for manual hotbar feat '$featLabel'."
            }

            $lineIndex = $availableManualClassFeatLines.Dequeue()
            $tokens = Convert-ToStringList $classFeatLines[$lineIndex]
        }
        else {
            $maxClassFeatRow++
            $tokens = New-Object System.Collections.Generic.List[string]
            for ($j = 0; $j -lt $classFeatExpectedTokens; $j++) {
                $tokens.Add("****") | Out-Null
            }
            $tokens[0] = $maxClassFeatRow.ToString()
        }

        Set-TokenByHeader -Tokens $tokens -Headers $classFeatHeaders -Header "FeatLabel" -Value $featLabel
        Set-TokenByHeader -Tokens $tokens -Headers $classFeatHeaders -Header "FeatIndex" -Value $featIndex
        Set-TokenByHeader -Tokens $tokens -Headers $classFeatHeaders -Header "List" -Value "1"
        Set-TokenByHeader -Tokens $tokens -Headers $classFeatHeaders -Header "GrantedOnLevel" -Value "99"
        Set-TokenByHeader -Tokens $tokens -Headers $classFeatHeaders -Header "OnMenu" -Value "1"
        if ($null -ne $lineIndex) {
            $classFeatLines[$lineIndex] = Format-2DARow $tokens.ToArray() $classFeatColumnWidths
        }
        else {
            $classFeatLines.Add((Format-2DARow $tokens.ToArray() $classFeatColumnWidths)) | Out-Null
            $addedClassFeatRows++
        }
    }

    [System.IO.File]::WriteAllLines($classFeatPath, $classFeatLines)
}

$enumLines = [System.Collections.Generic.List[string]]::new()
$enumLines.AddRange([System.IO.File]::ReadAllLines($enumPath))

$existingEnumText = [System.IO.File]::ReadAllText($enumPath)
$missingEnumEntries = New-Object System.Collections.Generic.List[string]
foreach ($entry in $spellIdsByLabel.GetEnumerator()) {
    if ($existingEnumText -notmatch "\b$($entry.Key)\s*=") {
        $missingEnumEntries.Add("        $($entry.Key) = $($entry.Value),") | Out-Null
    }
}

if ($missingEnumEntries.Count -gt 0) {
    $insertIndex = -1
    $enumStartIndex = -1
    for ($i = 0; $i -lt $enumLines.Count; $i++) {
        if ($enumLines[$i] -match "public\s+enum\s+Spell\b") {
            $enumStartIndex = $i
            break
        }
    }

    if ($enumStartIndex -lt 0) {
        throw "Could not find Spell enum declaration in $SpellEnumPath."
    }

    for ($i = $enumStartIndex + 1; $i -lt $enumLines.Count; $i++) {
        if ($enumLines[$i].Trim() -eq "}") {
            $insertIndex = $i
            break
        }
    }

    if ($insertIndex -lt 0) {
        throw "Could not find enum closing brace in $SpellEnumPath."
    }

    foreach ($line in $missingEnumEntries) {
        $enumLines.Insert($insertIndex, $line)
        $insertIndex++
    }

    [System.IO.File]::WriteAllLines($enumPath, $enumLines)
}

$featEnumLines = [System.Collections.Generic.List[string]]::new()
$featEnumLines.AddRange([System.IO.File]::ReadAllLines($featEnumPath))

$existingFeatEnumText = [System.IO.File]::ReadAllText($featEnumPath)
$missingFeatEnumEntries = New-Object System.Collections.Generic.List[string]
foreach ($entry in $linkedPlayerFeats.GetEnumerator()) {
    if ($existingFeatEnumText -notmatch "\b$($entry.Key)\s*=") {
        $missingFeatEnumEntries.Add("        $($entry.Key) = $($entry.Value),") | Out-Null
    }
}

if ($missingFeatEnumEntries.Count -gt 0) {
    $insertIndex = -1
    $enumStartIndex = -1
    for ($i = 0; $i -lt $featEnumLines.Count; $i++) {
        if ($featEnumLines[$i] -match "public\s+enum\s+FeatType\b") {
            $enumStartIndex = $i
            break
        }
    }

    if ($enumStartIndex -lt 0) {
        throw "Could not find FeatType enum declaration in $FeatEnumPath."
    }

    for ($i = $enumStartIndex + 1; $i -lt $featEnumLines.Count; $i++) {
        if ($featEnumLines[$i].Trim() -eq "}") {
            $insertIndex = $i
            break
        }
    }

    if ($insertIndex -lt 0) {
        throw "Could not find enum closing brace in $FeatEnumPath."
    }

    foreach ($line in $missingFeatEnumEntries) {
        $featEnumLines.Insert($insertIndex, $line)
        $insertIndex++
    }

    [System.IO.File]::WriteAllLines($featEnumPath, $featEnumLines)
}

Write-Host "Linked $linkedFeatRows generated feat rows."
Write-Host "Created $createdSpellRows spell rows."
Write-Host "Added $($missingEnumEntries.Count) spell enum entries."
Write-Host "Added $($missingFeatEnumEntries.Count) feat enum entries."
Write-Host "Added $addedClassFeatRows class feat rows."
