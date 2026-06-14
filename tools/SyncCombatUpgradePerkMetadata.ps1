[CmdletBinding()]
param(
    [string]$ManifestPath = "SWLOR.Game.Server\Readmes\CombatUpgradeBiblePerkManifest.csv",
    [string]$PerkDefinitionPath = "SWLOR.Game.Server\Feature\PerkDefinition"
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

    $baseName = [regex]::Replace($Name.Trim(), "\s+(I|II|III|IV|V|VI|VII|VIII|IX|X)$", "")
    return ($baseName -replace "[^A-Za-z0-9]", "").ToLowerInvariant()
}

function Get-RomanLevel {
    param([string]$Name)

    if ($Name -match "\s+(I|II|III|IV|V|VI|VII|VIII|IX|X)$") {
        switch ($Matches[1]) {
            "I" { return 1 }
            "II" { return 2 }
            "III" { return 3 }
            "IV" { return 4 }
            "V" { return 5 }
            "VI" { return 6 }
            "VII" { return 7 }
            "VIII" { return 8 }
            "IX" { return 9 }
            "X" { return 10 }
        }
    }

    return 1
}

function ConvertTo-CSharpString {
    param([string]$Text)

    if ($null -eq $Text) {
        return ""
    }

    $Text = [regex]::Replace($Text, "\s+", " ").Trim()
    return $Text.Replace("\", "\\").Replace('"', '\"')
}

function ConvertTo-OptionalInt {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value) -or $Value -eq "-") {
        return $null
    }

    return [int][Math]::Round([decimal]$Value)
}

function Get-ExpectedCategory {
    param([object]$Row)

    switch ($Row.Tab) {
        "Armor" { return "General" }
        "Piloting" { return "Piloting" }
        "First Aid" {
            switch ($Row.Style) {
                "Trauma Medic" { return "FirstAidTraumaMedic" }
                "Combat Pharmacology" { return "FirstAidCombatPharmacology" }
            }
        }
        "Leadership" {
            switch ($Row.Style) {
                "Vanguard Command" { return "LeadershipVanguardCommand" }
                "Field Steward" { return "LeadershipFieldSteward" }
                default { return "Leadership" }
            }
        }
        "Devices" {
            switch ($Row.Style) {
                "Grenadier" { return "DevicesGrenadier" }
                "Field Engineer" { return "DevicesFieldEngineer" }
                "Field Support" { return "DevicesFieldSupport" }
                "Assault Gadgets" { return "DevicesAssaultGadgets" }
            }
        }
        "Beast Mastery" {
            switch ($Row.Style) {
                "Training" { return "BeastMasteryTraining" }
                "Bioengineer" { return "BeastMasteryIncubation" }
                "Damage" { return "BeastDamage" }
                "Tank" { return "BeastTank" }
                "Balanced" { return "BeastBalanced" }
                "Bruiser" { return "BeastBruiser" }
                "Evasion" { return "BeastEvasion" }
                "Force" { return "BeastForce" }
            }
        }
        "Heavy Vibroblade" { return "HeavyVibroblade$($Row.Style.Replace(' ', ''))" }
        "Katar" { return "Katar$($Row.Style.Replace(' ', ''))" }
        "Lightsaber" { return "Lightsaber$($Row.Style.Replace(' ', ''))" }
        "Pistol" { return "Pistol$($Row.Style.Replace(' ', ''))" }
        "Rifle" { return "Rifle$($Row.Style.Replace(' ', ''))" }
        "Saberstaff" { return "Saberstaff$($Row.Style.Replace(' ', ''))" }
        "Spear" { return "Spear$($Row.Style.Replace(' ', ''))" }
        "Staff" { return "Staff$($Row.Style.Replace(' ', ''))" }
        "Throwing" { return "Throwing$($Row.Style.Replace(' ', ''))" }
        "Twin Blade" { return "TwinBlade$($Row.Style.Replace(' ', ''))" }
        "Vibroblade" { return "Vibroblade$($Row.Style.Replace(' ', ''))" }
        "Vibroknife" { return "Vibroknife$($Row.Style.Replace(' ', ''))" }
    }

    return ""
}

function Get-ExpectedSkillType {
    param([string]$SkillText)

    $key = ($SkillText -replace "\s+", "")
    $skillMap = @{
        Armor = "Armor"
        BeastMastery = "BeastMastery"
        Devices = "Devices"
        FirstAid = "FirstAid"
        Force = "Force"
        HeavyVibroblade = "HeavyVibroblade"
        Katar = "Katar"
        Leadership = "Leadership"
        Lightsaber = "Lightsaber"
        Piloting = "Piloting"
        Pistol = "Pistol"
        Rifle = "Rifle"
        Saberstaff = "Saberstaff"
        Spear = "Spear"
        Staff = "Staff"
        Throwing = "Throwing"
        TwinBlade = "TwinBlade"
        Vibroblade = "Vibroblade"
        Vibroknife = "Vibroknife"
    }

    if ($skillMap.ContainsKey($key)) {
        return $skillMap[$key]
    }

    return $key
}

function Test-IsBeastPerkRow {
    param([object]$Row)

    return $Row.Tab -eq "Beast Mastery" -and $Row.CharacterType -eq "Beast"
}

function Get-ExpectedSkillRequirement {
    param([object]$Row)

    if ([string]::IsNullOrWhiteSpace($Row.SkillRequirements) -or $Row.SkillRequirements -eq "-") {
        return $null
    }

    if ($Row.SkillRequirements -match "(.+?)\s+([0-9]+)") {
        return [pscustomobject]@{
            Skill = Get-ExpectedSkillType $Matches[1]
            Rank = ConvertTo-OptionalInt $Matches[2]
        }
    }

    return $null
}

function Get-ExpectedBeastLevel {
    param([object]$Row)

    if (!(Test-IsBeastPerkRow $Row)) {
        return $null
    }

    if ($Row.SkillRequirements -match "^\s*([0-9]+(?:\.[0-9]+)?)\s*$") {
        return ConvertTo-OptionalInt $Matches[1]
    }

    return $null
}

function Get-ExpectedBeastRole {
    param([object]$Row)

    if (!(Test-IsBeastPerkRow $Row)) {
        return ""
    }

    switch ($Row.Style) {
        "Damage" { return "Damage" }
        "Tank" { return "Tank" }
        "Balanced" { return "Balanced" }
        "Bruiser" { return "Bruiser" }
        "Evasion" { return "Evasion" }
        "Force" { return "Force" }
        default { return "" }
    }
}

function Get-ExistingSkillRequirement {
    param([string]$Segment)

    if ($Segment -match "\.RequirementSkill\(SkillType\.(\w+)\s*,\s*(\d+)\)") {
        return [pscustomobject]@{
            Skill = $Matches[1]
            Rank = [int]$Matches[2]
        }
    }

    return $null
}

function Find-MatchingBibleRow {
    param(
        [object[]]$Rows,
        [string]$Category,
        [string]$BaseName,
        [int]$Level,
        [string]$Segment
    )

    $candidates = @($Rows | Where-Object {
        $_.BaseKey -eq $BaseName -and
        $_.LevelNumber -eq $Level
    })

    if ($candidates.Count -le 1) {
        return $(if ($candidates.Count -eq 1) { $candidates[0] } else { $null })
    }

    if ($Category -like "Force*") {
        $categoryCandidates = @($candidates | Where-Object { $_.Tab -eq "Force" })
    }
    else {
        $categoryCandidates = @($candidates | Where-Object { $_.ExpectedCategory -eq $Category })
    }

    if ($categoryCandidates.Count -eq 1) {
        return $categoryCandidates[0]
    }
    elseif ($categoryCandidates.Count -gt 1) {
        $candidates = $categoryCandidates
    }

    $existingSkill = Get-ExistingSkillRequirement $Segment
    if ($null -ne $existingSkill) {
        $skillCandidates = @($candidates | Where-Object {
            $null -ne $_.ExpectedSkill -and
            $_.ExpectedSkill.Skill -eq $existingSkill.Skill
        })
        if ($skillCandidates.Count -eq 1) {
            return $skillCandidates[0]
        }
    }

    return $null
}

function Replace-OrAddLine {
    param(
        [string]$Segment,
        [string]$Pattern,
        [string]$Replacement,
        [string]$InsertAfterPattern
    )

    if ($Segment -match $Pattern) {
        return [regex]::Replace($Segment, $Pattern, $Replacement, 1)
    }

    $insertReplacement = if ($Replacement -match "^\s") {
        $Replacement
    }
    else {
        "                $Replacement"
    }

    return [regex]::Replace(
        $Segment,
        $InsertAfterPattern,
        "`$0`r`n$insertReplacement",
        1)
}

function Remove-Line {
    param(
        [string]$Segment,
        [string]$Pattern
    )

    return [regex]::Replace($Segment, "\r?\n\s*$Pattern;?", "")
}

function Remove-DuplicateCharacterRequirements {
    param([string]$Segment)

    $lines = [System.Collections.Generic.List[string]]::new()
    $seen = $false
    foreach ($line in [regex]::Split($Segment, "(\r?\n)")) {
        if ($line -match "^\r?\n$") {
            $lines.Add($line) | Out-Null
            continue
        }

        if ($line -match "^\s*\.RequirementCharacterType\(CharacterType\.\w+\);?\s*$") {
            if ($seen) {
                if ($lines.Count -gt 0 -and $lines[$lines.Count - 1] -match "^\r?\n$") {
                    $lines.RemoveAt($lines.Count - 1)
                }
                continue
            }

            $seen = $true
        }

        $lines.Add($line) | Out-Null
    }

    return ($lines -join "")
}

function Ensure-Terminator {
    param(
        [string]$Segment,
        [bool]$HadTerminator
    )

    if (!$HadTerminator -or $Segment -match ";\s*$") {
        return $Segment
    }

    return [regex]::Replace($Segment, "(\r?\n\s*\.[A-Za-z_][A-Za-z0-9_]*\([^;\r\n]*\))(\s*)$", '$1;$2')
}

function Update-LevelSegment {
    param(
        [string]$Segment,
        [object]$Row
    )

    $hadTerminator = $Segment -match ";\s*$"
    $description = ConvertTo-CSharpString $Row.Description
    $Segment = Replace-OrAddLine `
        -Segment $Segment `
        -Pattern '\.Description\("((?:\\.|[^"\\])*)"\)' `
        -Replacement ".Description(`"$description`")" `
        -InsertAfterPattern '\.AddPerkLevel\(\)'

    $price = ConvertTo-OptionalInt $Row.Price
    if ($null -ne $price) {
        $Segment = Replace-OrAddLine `
            -Segment $Segment `
            -Pattern '\.Price\(\d+\)' `
            -Replacement ".Price($price)" `
            -InsertAfterPattern '\.Description\("((?:\\.|[^"\\])*)"\)'
    }

    $expectedSkill = Get-ExpectedSkillRequirement $Row
    if ($null -eq $expectedSkill) {
        $Segment = Remove-Line -Segment $Segment -Pattern '\.RequirementSkill\(SkillType\.\w+\s*,\s*\d+\)'
    }
    else {
        $Segment = Replace-OrAddLine `
            -Segment $Segment `
            -Pattern '\.RequirementSkill\(SkillType\.\w+\s*,\s*\d+\)' `
            -Replacement ".RequirementSkill(SkillType.$($expectedSkill.Skill), $($expectedSkill.Rank))" `
            -InsertAfterPattern '\.Price\(\d+\)'
    }

    $expectedBeastLevel = Get-ExpectedBeastLevel $Row
    if ($null -eq $expectedBeastLevel) {
        $Segment = Remove-Line -Segment $Segment -Pattern '\.RequirementBeastLevel\(\d+\)'
    }
    else {
        $Segment = Replace-OrAddLine `
            -Segment $Segment `
            -Pattern '\.RequirementBeastLevel\(\d+\)' `
            -Replacement ".RequirementBeastLevel($expectedBeastLevel)" `
            -InsertAfterPattern '\.Price\(\d+\)'
    }

    if ($Row.Tab -eq "Beast Mastery") {
        $beastRole = Get-ExpectedBeastRole $Row
        if ([string]::IsNullOrWhiteSpace($beastRole)) {
            $Segment = Remove-Line -Segment $Segment -Pattern '\.RequirementBeastRole\(BeastRoleType\.\w+\)'
        }
        else {
            $beastRoleInsertAfter = if ($null -ne $expectedBeastLevel) {
                '\.RequirementBeastLevel\(\d+\)'
            }
            elseif ($null -ne $expectedSkill) {
                '\.RequirementSkill\(SkillType\.\w+\s*,\s*\d+\)'
            }
            else {
                '\.Price\(\d+\)'
            }

            $Segment = Replace-OrAddLine `
                -Segment $Segment `
                -Pattern '\.RequirementBeastRole\(BeastRoleType\.\w+\)' `
                -Replacement ".RequirementBeastRole(BeastRoleType.$beastRole)" `
                -InsertAfterPattern $beastRoleInsertAfter
        }
    }
    else {
        $Segment = Remove-Line -Segment $Segment -Pattern '\.RequirementCharacterType\(CharacterType\.\w+\)'
        $characterInsertAfter = if ($null -ne $expectedSkill) {
            '\.RequirementSkill\(SkillType\.\w+\s*,\s*\d+\)'
        }
        else {
            '\.Price\(\d+\)'
        }

        if ($Row.CharacterType -eq "Standard") {
            $Segment = Replace-OrAddLine `
                -Segment $Segment `
                -Pattern '\.RequirementCharacterType\(CharacterType\.Standard\)' `
                -Replacement "                .RequirementCharacterType(CharacterType.Standard)" `
                -InsertAfterPattern $characterInsertAfter
        }
        elseif ($Row.CharacterType -eq "Force") {
            $Segment = Replace-OrAddLine `
                -Segment $Segment `
                -Pattern '\.RequirementCharacterType\(CharacterType\.ForceSensitive\)' `
                -Replacement "                .RequirementCharacterType(CharacterType.ForceSensitive)" `
                -InsertAfterPattern $characterInsertAfter
        }
    }

    if ($Row.Type -eq "Trait") {
        $Segment = Remove-Line -Segment $Segment -Pattern '\.GrantsFeat\(FeatType\.\w+\)'
    }

    $Segment = Remove-DuplicateCharacterRequirements $Segment
    return Ensure-Terminator -Segment $Segment -HadTerminator $hadTerminator
}

$manifestFullPath = Resolve-RepoPath $ManifestPath
$perkDefinitionFullPath = Resolve-RepoPath $PerkDefinitionPath
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
$scopedTypes = @("Aura", "Combat", "Stance", "Toggle", "Trait")
$rows = @(
    Import-Csv $manifestFullPath |
        Where-Object {
            $outOfScopeTabs -notcontains $_.Tab -and
            $scopedTypes -contains $_.Type -and
            @("Implemented", "Design Added") -contains $_.DevStatus
        } |
        ForEach-Object {
            $_ | Add-Member -NotePropertyName BaseKey -NotePropertyValue (Get-SanitizedName $_.PerkName) -Force
            $_ | Add-Member -NotePropertyName LevelNumber -NotePropertyValue (Get-RomanLevel $_.PerkName) -Force
            $_ | Add-Member -NotePropertyName ExpectedCategory -NotePropertyValue (Get-ExpectedCategory $_) -Force
            $_ | Add-Member -NotePropertyName ExpectedSkill -NotePropertyValue (Get-ExpectedSkillRequirement $_) -Force
            $_
        }
)

$createPattern = [regex]::new(
    '\.Create\(\s*PerkCategoryType\.(?<category>\w+)\s*,\s*PerkType\.(?<perkType>\w+)\s*\)\s*\.Name\("(?<name>(?:\\.|[^"\\])*)"\)(?<body>.*?)(?=\r?\n\s*(?:private|public|protected|internal)\s|\z)',
    [System.Text.RegularExpressions.RegexOptions]::Singleline)

$updatedFiles = 0
$updatedLevels = 0
$unmatched = [System.Collections.Generic.List[string]]::new()

foreach ($file in Get-ChildItem $perkDefinitionFullPath -Recurse -Filter "*PerkDefinition.cs") {
    $content = Get-Content $file.FullName -Raw
    $changed = $false
    $newContent = $createPattern.Replace($content, {
        param($match)

        $category = $match.Groups["category"].Value
        $baseKey = Get-SanitizedName $match.Groups["name"].Value
        $body = $match.Groups["body"].Value
        $parts = [regex]::Split($body, '(\r?\n\s*\.AddPerkLevel\(\))')
        if ($parts.Count -le 1) {
            return $match.Value
        }

        $rebuilt = [System.Text.StringBuilder]::new()
        [void]$rebuilt.Append($match.Value.Substring(0, $match.Value.Length - $body.Length))
        [void]$rebuilt.Append($parts[0])

        $level = 0
        for ($i = 1; $i -lt $parts.Count; $i += 2) {
            $level++
            $segment = $parts[$i] + $parts[$i + 1]
            $row = Find-MatchingBibleRow -Rows $rows -Category $category -BaseName $baseKey -Level $level -Segment $segment
            if ($null -eq $row) {
                $unmatched.Add("$($file.FullName): $category/$baseKey level $level") | Out-Null
                [void]$rebuilt.Append($segment)
                continue
            }

            $updated = Update-LevelSegment -Segment $segment -Row $row
            if ($updated -ne $segment) {
                $script:updatedLevels++
                $script:changed = $true
            }
            [void]$rebuilt.Append($updated)
        }

        return $rebuilt.ToString()
    })

    if ($newContent -ne $content) {
        [System.IO.File]::WriteAllText($file.FullName, $newContent)
        $updatedFiles++
    }
}

[pscustomobject]@{
    UpdatedFiles = $updatedFiles
    UpdatedLevels = $updatedLevels
    UnmatchedLevels = $unmatched.Count
}

if ($unmatched.Count -gt 0) {
    $unmatched | Sort-Object | Select-Object -First 100
}
