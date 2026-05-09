[CmdletBinding()]
param(
    [switch]$RefreshBible,
    [string]$SpreadsheetId = "1rppEkwp2dX0oGKY1ftSbDTcg7GhopODseqbDb4cpNSU",
    [string]$ManifestPath = "SWLOR.Game.Server\Readmes\CombatUpgradeBiblePerkManifest.csv",
    [string]$AuditPath = "SWLOR.Game.Server\Readmes\CombatUpgradePerkAudit.csv"
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

function Get-SanitizedName {
    param([string]$Name)

    if ([string]::IsNullOrWhiteSpace($Name)) {
        return ""
    }

    $baseName = $Name -replace "\s+\([^)]*\)$", ""
    $baseName = $baseName -replace "\s+(I|II|III|IV|V|VI)$", ""
    return ($baseName -replace "[^A-Za-z0-9]", "").ToLowerInvariant()
}

function Get-PropertyValue {
    param(
        [object]$Object,
        [string]$Name
    )

    $property = $Object.PSObject.Properties[$Name]
    if ($null -eq $property) {
        return $null
    }

    return $property.Value
}

function Get-FirstPropertyValue {
    param(
        [object]$Object,
        [string[]]$Names
    )

    foreach ($name in $Names) {
        $value = Get-PropertyValue $Object $name
        if ($null -ne $value) {
            return $value
        }
    }

    return $null
}

function Get-RepoRelativePath {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return ""
    }

    $fullPath = $Path
    if (![System.IO.Path]::IsPathRooted($fullPath)) {
        return $fullPath
    }

    if ($fullPath.StartsWith($repoRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $fullPath.Substring($repoRoot.Length).TrimStart("\", "/")
    }

    return $fullPath
}

function Add-AuditRow {
    param(
        [System.Collections.Generic.List[object]]$Rows,
        [string]$AuditType,
        [object]$BibleRow,
        [string]$File = "",
        [string]$Details = ""
    )

    function Normalize-AuditText {
        param([object]$Value)

        if ($null -eq $Value) {
            return ""
        }

        return ([string]$Value -replace "\s+", " ").Trim()
    }

    $Rows.Add([pscustomobject]@{
        AuditType = Normalize-AuditText $AuditType
        Tab = Normalize-AuditText $BibleRow.Tab
        Row = Normalize-AuditText $BibleRow.Row
        Name = Normalize-AuditText $BibleRow.PerkName
        PerkType = ""
        File = Get-RepoRelativePath $File
        Style = Normalize-AuditText $BibleRow.Style
        Price = Normalize-AuditText $BibleRow.Price
        Requirements = Normalize-AuditText $BibleRow.SkillRequirements
        CharType = Normalize-AuditText $BibleRow.CharacterType
        Type = Normalize-AuditText $BibleRow.Type
        DevStatus = Normalize-AuditText $BibleRow.DevStatus
        Description = Normalize-AuditText $BibleRow.Description
        Details = Normalize-AuditText $Details
    }) | Out-Null
}

function Import-CodeNameIndex {
    param(
        [string]$Path,
        [string]$Filter
    )

    $index = @{}
    $files = Get-ChildItem -Path $Path -Filter $Filter -Recurse

    foreach ($file in $files) {
        $content = Get-Content $file.FullName -Raw
        $matches = [regex]::Matches($content, '\.Name\("([^"]+)"\)')
        foreach ($match in $matches) {
            $name = $match.Groups[1].Value
            if (!$index.ContainsKey($name)) {
                $index[$name] = New-Object System.Collections.Generic.List[object]
            }

            $index[$name].Add([pscustomobject]@{
                File = $file.FullName
                Content = $content
            }) | Out-Null
        }
    }

    return $index
}

function Import-AbilityFileIndex {
    param([string]$Path)

    $index = @{}
    $files = Get-ChildItem -Path $Path -Filter "*AbilityDefinition.cs" -Recurse

    foreach ($file in $files) {
        $key = ($file.BaseName -replace "AbilityDefinition$", "").ToLowerInvariant()
        $index[$key] = [pscustomobject]@{
            File = $file.FullName
            Content = Get-Content $file.FullName -Raw
        }
    }

    return $index
}

function Get-AbilityFileForBibleRow {
    param(
        [hashtable]$AbilityFilesByName,
        [object]$BibleRow
    )

    $key = Get-SanitizedName $BibleRow.PerkName
    if ($AbilityFilesByName.ContainsKey($key)) {
        return $AbilityFilesByName[$key]
    }

    return $null
}

function Convert-CooldownToSeconds {
    param([string]$Cooldown)

    if ([string]::IsNullOrWhiteSpace($Cooldown) -or $Cooldown -eq "-") {
        return $null
    }

    $value = $Cooldown.Trim().ToLowerInvariant()
    if ($value -match "^(\d+)\s*seconds?$") {
        return [int]$matches[1]
    }
    if ($value -match "^(\d+)\s*minutes?$") {
        return [int]$matches[1] * 60
    }

    return $null
}

function Import-2DA {
    param([string]$Path)

    $lines = Get-Content $Path
    $headerLineIndex = -1
    for ($i = 1; $i -lt $lines.Count; $i++) {
        if (![string]::IsNullOrWhiteSpace($lines[$i])) {
            $headerLineIndex = $i
            break
        }
    }

    if ($headerLineIndex -lt 0) {
        return @()
    }

    $headers = $lines[$headerLineIndex].Trim() -split "\s+"
    $rows = New-Object System.Collections.Generic.List[object]

    for ($i = $headerLineIndex + 1; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        if ([string]::IsNullOrWhiteSpace($line)) {
            continue
        }

        $fields = $line.Trim() -split "\s+"
        if ($fields.Count -lt 2) {
            continue
        }

        $rowNumber = 0
        if (![int]::TryParse($fields[0], [ref]$rowNumber)) {
            continue
        }

        $rows.Add([pscustomobject]@{
            Number = $rowNumber
            Headers = $headers
            Fields = $fields
            SourceLine = $line
        }) | Out-Null
    }

    return $rows
}

function Import-BibleTabRows {
    param([string]$Csv)

    $lines = $Csv -split "\r?\n"
    $headerLineIndex = -1
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match "Perk Name|PerkName") {
            $headerLineIndex = $i
            break
        }
    }

    if ($headerLineIndex -lt 0) {
        return @()
    }

    $headers = @(
        "Style",
        "Price",
        "PerkName",
        "SkillRequirements",
        "CharacterType",
        "Type",
        "Description",
        "CrossSkill",
        "FP",
        "STM",
        "CastingTime",
        "CooldownTime",
        "DevStatus",
        "AdditionalRequirements",
        "Notes"
    )

    for ($i = 16; $i -le 40; $i++) {
        $headers += "Unused$i"
    }

    $rows = New-Object System.Collections.Generic.List[object]
    for ($i = $headerLineIndex + 1; $i -lt $lines.Count; $i++) {
        if ([string]::IsNullOrWhiteSpace($lines[$i])) {
            continue
        }

        $parsedRows = $lines[$i] | ConvertFrom-Csv -Header $headers
        foreach ($row in $parsedRows) {
            $rows.Add([pscustomobject]@{
                Row = $i + 1
                Style = $row.Style
                Price = $row.Price
                PerkName = $row.PerkName
                SkillRequirements = $row.SkillRequirements
                CharacterType = $row.CharacterType
                Type = $row.Type
                Description = $row.Description
                CrossSkill = $row.CrossSkill
                FP = $row.FP
                STM = $row.STM
                CastingTime = $row.CastingTime
                CooldownTime = $row.CooldownTime
                DevStatus = $row.DevStatus
                AdditionalRequirements = $row.AdditionalRequirements
                Notes = $row.Notes
            }) | Out-Null
        }
    }

    return $rows
}

$manifestFullPath = Resolve-RepoPath $ManifestPath
$auditFullPath = Resolve-RepoPath $AuditPath

if ($RefreshBible) {
    $sheetTabs = @(
        "Armor", "Vibroblade", "Vibroknife", "Lightsaber", "Heavy Vibroblade", "Spear",
        "Twin Blade", "Saberstaff", "Katar", "Staff", "Pistol", "Rifle", "Throwing",
        "Force", "Devices", "Beast Mastery", "Piloting", "Leadership", "First Aid",
        "Espionage", "Smithery", "Engineering", "Fabrication", "Research", "Agriculture", "Gathering"
    )

    $manifestRows = New-Object System.Collections.Generic.List[object]
    foreach ($tab in $sheetTabs) {
        $encodedTab = [System.Uri]::EscapeDataString($tab)
        $uri = "https://docs.google.com/spreadsheets/d/$SpreadsheetId/gviz/tq?tqx=out:csv&sheet=$encodedTab"
        $csv = Invoke-WebRequest -Uri $uri -UseBasicParsing | Select-Object -ExpandProperty Content
        $rows = Import-BibleTabRows -Csv $csv

        foreach ($row in $rows) {
            $name = Get-FirstPropertyValue $row @("PerkName", "Perk Name", "Name")
            if ([string]::IsNullOrWhiteSpace($name)) {
                $name = Get-PropertyValue $row "Name"
            }
            if ([string]::IsNullOrWhiteSpace($name)) {
                continue
            }

            $manifestRows.Add([pscustomobject]@{
                Tab = $tab
                Row = Get-FirstPropertyValue $row @("Row", "#")
                Style = Get-PropertyValue $row "Style"
                Price = Get-PropertyValue $row "Price"
                PerkName = $name
                SkillRequirements = Get-FirstPropertyValue $row @("SkillRequirements", "Skill Requirements", "Requirements")
                CharacterType = Get-FirstPropertyValue $row @("CharacterType", "Character Type", "CharType")
                Type = Get-PropertyValue $row "Type"
                Description = Get-PropertyValue $row "Description"
                CrossSkill = Get-FirstPropertyValue $row @("CrossSkill", "Cross Skill")
                FP = Get-PropertyValue $row "FP"
                STM = Get-PropertyValue $row "STM"
                CastingTime = Get-FirstPropertyValue $row @("CastingTime", "Casting Time")
                CooldownTime = Get-FirstPropertyValue $row @("CooldownTime", "Cooldown Time", "Cooldown")
                DevStatus = Get-FirstPropertyValue $row @("DevStatus", "Dev Status")
                AdditionalRequirements = Get-FirstPropertyValue $row @("AdditionalRequirements", "Additional Requirements")
                Notes = Get-PropertyValue $row "Notes"
            }) | Out-Null
        }
    }

    if ($manifestRows.Count -eq 0) {
        throw "No Bible perk rows were parsed. The sheet export format may have changed."
    }

    $manifestRows | Export-Csv -Path $manifestFullPath -NoTypeInformation
}

$outOfScopeTabs = @(
    "Espionage",
    "Farming",
    "Agriculture",
    "Smithery",
    "Engineering",
    "Fabrication",
    "Research",
    "Gathering"
)
$manifest = Import-Csv $manifestFullPath |
    Where-Object {
        $outOfScopeTabs -notcontains $_.Tab -and
        ![string]::IsNullOrWhiteSpace($_.PerkName) -and
        @("Combat", "Stance", "Toggle", "Trait") -contains $_.Type
    }

if (@($manifest).Count -eq 0) {
    throw "Combat upgrade manifest contains no scoped rows. Run with -RefreshBible or restore a valid manifest before auditing."
}

$perkIndex = Import-CodeNameIndex -Path (Resolve-RepoPath "SWLOR.Game.Server\Feature\PerkDefinition") -Filter "*PerkDefinition.cs"
$abilityNameIndex = Import-CodeNameIndex -Path (Resolve-RepoPath "SWLOR.Game.Server\Feature\AbilityDefinition") -Filter "*AbilityDefinition.cs"
$abilityFileIndex = Import-AbilityFileIndex -Path (Resolve-RepoPath "SWLOR.Game.Server\Feature\AbilityDefinition")
$statusDefinitionText = (Get-ChildItem (Resolve-RepoPath "SWLOR.Game.Server\Feature\StatusEffectDefinition") -Filter "*StatusEffect.cs" -Recurse |
    ForEach-Object { Get-Content $_.FullName -Raw }) -join "`n"

$perkBaseNameIndex = @{}
foreach ($key in $perkIndex.Keys) {
    $perkBaseNameIndex[(Get-SanitizedName $key)] = $true
}

$abilityBaseNameIndex = @{}
foreach ($key in $abilityNameIndex.Keys) {
    $abilityBaseNameIndex[(Get-SanitizedName $key)] = $true
}

$statusApplicationVerb = "(?:inflict(?:s|ed|ing)?|appl(?:y|ies|ied)|grant(?:s|ed|ing)?|gain(?:s|ed|ing)?|become(?:s)?|suffer(?:s|ed|ing)?|attempt(?:s|ed|ing)?\s+to\s+inflict)"
$statusChecks = @(
    @{ Pattern = "\b$statusApplicationVerb\b.{0,120}\bPoison\b"; Enum = "Poison" },
    @{ Pattern = "\b$statusApplicationVerb\b.{0,120}\bBleed(?:ing)?\b"; Enum = "Bleed" },
    @{ Pattern = "\b$statusApplicationVerb\b.{0,120}\bSunder(?:ed)?\b"; Enum = "Sunder" },
    @{ Pattern = "\b$statusApplicationVerb\b.{0,120}\bForce Disruption\b"; Enum = "ForceDisruption" },
    @{ Pattern = "\b$statusApplicationVerb\b.{0,120}\bBlind\b"; Enum = "Blind" },
    @{ Pattern = "\b$statusApplicationVerb\b.{0,120}\bToxin\b"; Enum = "Toxin" },
    @{ Pattern = "\b$statusApplicationVerb\b.{0,120}\bDisoriented\b"; Enum = "Disoriented" },
    @{ Pattern = "\b$statusApplicationVerb\b.{0,120}\bWeakened\b"; Enum = "Weakened" },
    @{ Pattern = "\b$statusApplicationVerb\b.{0,120}\bDazed\b"; Enum = "Dazed" },
    @{ Pattern = "\b$statusApplicationVerb\b.{0,120}\bStunned\b"; Enum = "Stunned" },
    @{ Pattern = "\b$statusApplicationVerb\b.{0,120}\bExposed\b"; Enum = "Exposed" },
    @{ Pattern = "\b$statusApplicationVerb\b.{0,120}\bHemorrhage\b"; Enum = "Hemorrhage" },
    @{ Pattern = "\b$statusApplicationVerb\b.{0,120}\bKnockdown\b"; Enum = "Knockdown" },
    @{ Pattern = "\b$statusApplicationVerb\b.{0,120}\bVital Strike\b"; Enum = "VitalStrike" },
    @{ Pattern = "\b$statusApplicationVerb\b.{0,120}\bHamstring\b"; Enum = "Hamstring" },
    @{ Pattern = "\b$statusApplicationVerb\b.{0,120}\bExhausted\b"; Enum = "Exhausted" },
    @{ Pattern = "\b$statusApplicationVerb\b.{0,120}\bHobble\b"; Enum = "Hobble" },
    @{ Pattern = "\b$statusApplicationVerb\b.{0,120}\bForce Erosion\b"; Enum = "ForceErosion" },
    @{ Pattern = "\b$statusApplicationVerb\b.{0,120}\bFractured Focus\b"; Enum = "FracturedFocus" },
    @{ Pattern = "\b$statusApplicationVerb\b.{0,120}\bForce Warding\b"; Enum = "ForceWarding" },
    @{ Pattern = "\b$statusApplicationVerb\b.{0,120}\bFoggy Mind\b"; Enum = "FoggyMind" }
)

$auditRows = New-Object System.Collections.Generic.List[object]

foreach ($row in $manifest) {
    $rowBaseName = Get-SanitizedName $row.PerkName
    if (!$perkBaseNameIndex.ContainsKey($rowBaseName)) {
        Add-AuditRow -Rows $auditRows -AuditType "MissingPerkName" -BibleRow $row
    }

    $isActiveType = @("Combat", "Stance", "Toggle") -contains $row.Type
    if ($isActiveType -and !$abilityBaseNameIndex.ContainsKey($rowBaseName)) {
        Add-AuditRow -Rows $auditRows -AuditType "MissingAbilityDefinition" -BibleRow $row
    }

    $abilityFile = Get-AbilityFileForBibleRow -AbilityFilesByName $abilityFileIndex -BibleRow $row
    $cooldownSeconds = Convert-CooldownToSeconds $row.CooldownTime
    if ($isActiveType -and $null -ne $cooldownSeconds -and $abilityFile -and $abilityFile.Content -notmatch "HasRecastDelay") {
        Add-AuditRow -Rows $auditRows -AuditType "MissingAbilityRecast" -BibleRow $row -File $abilityFile.File -Details "Expected cooldown: $($row.CooldownTime)"
    }

    foreach ($check in $statusChecks) {
        if ($row.Description -match $check.Pattern) {
            $statusEffectClass = "$($check.Enum)StatusEffect"
            if ($statusDefinitionText -notmatch "\b$statusEffectClass\b") {
                Add-AuditRow -Rows $auditRows -AuditType "MissingStatusEffectDefinition" -BibleRow $row -Details $statusEffectClass
            }
            elseif ($isActiveType -and $abilityFile -and $abilityFile.Content -notmatch "\b$statusEffectClass\b") {
                Add-AuditRow -Rows $auditRows -AuditType "StatusNotAppliedInAbility" -BibleRow $row -File $abilityFile.File -Details $statusEffectClass
            }
        }
    }
}

$iconRoot = Resolve-RepoPath "SWLOR_Haks\swlor2_tga"
$iconNames = New-Object "System.Collections.Generic.HashSet[string]" ([System.StringComparer]::OrdinalIgnoreCase)
Get-ChildItem $iconRoot -Filter "*.tga" | ForEach-Object {
    $iconNames.Add([System.IO.Path]::GetFileNameWithoutExtension($_.Name)) | Out-Null
}

$featRows = Import-2DA (Resolve-RepoPath "SWLOR_Haks\swlor2_2da\feat.2da")
$combatUpgradeSpellIds = New-Object "System.Collections.Generic.HashSet[int]"
foreach ($row in $featRows) {
    $iconIndex = [array]::IndexOf($row.Headers, "ICON") + 1
    $spellIndex = [array]::IndexOf($row.Headers, "SPELLID") + 1
    $isCombatUpgradeFeat = $row.Number -ge 2000 -or $row.Fields[1] -match "^KoltoRecovery[123]$"
    if ($isCombatUpgradeFeat -and $iconIndex -gt 0 -and $row.Fields.Count -gt $iconIndex) {
        $icon = $row.Fields[$iconIndex]
        if ($icon -ne "****" -and !$iconNames.Contains($icon)) {
            $auditRows.Add([pscustomobject]@{
                AuditType = "MissingFeatIconResource"
                Tab = "2DA"
                Row = $row.Number
                Name = $row.Fields[1]
                PerkType = ""
                File = "SWLOR_Haks\swlor2_2da\feat.2da"
                Style = ""
                Price = ""
                Requirements = ""
                CharType = ""
                Type = "FeatIcon"
                DevStatus = ""
                Description = ""
                Details = $icon
            }) | Out-Null
        }
    }

    if (!$isCombatUpgradeFeat -or $spellIndex -le 0 -or $row.Fields.Count -le $spellIndex) {
        continue
    }

    $spellId = 0
    if ([int]::TryParse($row.Fields[$spellIndex], [ref]$spellId)) {
        $combatUpgradeSpellIds.Add($spellId) | Out-Null
    }

    if ($row.Number -ge 2000 -and $row.Fields[$spellIndex] -eq "****") {
        $auditRows.Add([pscustomobject]@{
            AuditType = "GeneratedFeatMissingSpellLink"
            Tab = "2DA"
            Row = $row.Number
            Name = $row.Fields[1]
            PerkType = ""
            File = "SWLOR_Haks\swlor2_2da\feat.2da"
            Style = ""
            Price = ""
            Requirements = ""
            CharType = ""
            Type = "FeatSpell"
            DevStatus = ""
            Description = ""
            Details = "SPELLID is ****"
        }) | Out-Null
    }
}

$spellRows = Import-2DA (Resolve-RepoPath "SWLOR_Haks\swlor2_2da\spells.2da")
foreach ($row in $spellRows) {
    if (!$combatUpgradeSpellIds.Contains($row.Number)) {
        continue
    }

    $iconIndex = [array]::IndexOf($row.Headers, "IconResRef") + 1
    if ($iconIndex -le 0 -or $row.Fields.Count -le $iconIndex) {
        continue
    }

    $icon = $row.Fields[$iconIndex]
    if ($icon -ne "****" -and !$iconNames.Contains($icon)) {
        $auditRows.Add([pscustomobject]@{
            AuditType = "MissingSpellIconResource"
            Tab = "2DA"
            Row = $row.Number
            Name = $row.Fields[1]
            PerkType = ""
            File = "SWLOR_Haks\swlor2_2da\spells.2da"
            Style = ""
            Price = ""
            Requirements = ""
            CharType = ""
            Type = "SpellIcon"
            DevStatus = ""
            Description = ""
            Details = $icon
        }) | Out-Null
    }
}

if ($auditRows.Count -gt 0) {
    $auditRows | Sort-Object AuditType, Tab, Row, Name | Export-Csv -Path $auditFullPath -NoTypeInformation
}
else {
    '"AuditType","Tab","Row","Name","PerkType","File","Style","Price","Requirements","CharType","Type","DevStatus","Description","Details"' |
        Set-Content -Path $auditFullPath
}

$summary = $auditRows | Group-Object AuditType | Sort-Object Name | ForEach-Object {
    [pscustomobject]@{
        AuditType = $_.Name
        Count = $_.Count
    }
}

$summary | Format-Table -AutoSize
