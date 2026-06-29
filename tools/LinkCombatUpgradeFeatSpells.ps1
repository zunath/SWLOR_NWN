[CmdletBinding()]
param(
    [string]$Feat2daPath = "SWLOR_Haks\sw_2da\feat.2da",
    [string]$Spells2daPath = "SWLOR_Haks\sw_2da\spells.2da",
    [string]$SpellEnumPath = "SWLOR.NWN.API\NWScript\Enum\spell.cs",
    [int]$GeneratedFeatStart = 2000,
    [int]$GeneratedFeatEnd = 2578
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
$enumPath = Resolve-RepoPath $SpellEnumPath

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
    $linkedFeatRows++
}

foreach ($row in $generatedSpellRows) {
    $spellsLines.Add($row) | Out-Null
}

[System.IO.File]::WriteAllLines($featPath, $featLines)
[System.IO.File]::WriteAllLines($spellsPath, $spellsLines)

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

Write-Host "Linked $linkedFeatRows generated feat rows."
Write-Host "Created $createdSpellRows spell rows."
Write-Host "Added $($missingEnumEntries.Count) spell enum entries."
