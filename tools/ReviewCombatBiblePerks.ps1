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

    if ([string]::IsNullOrWhiteSpace($Name)) {
        return ""
    }

    $baseName = $Name -replace "\s+\([^)]*\)$", ""
    $baseName = $baseName -replace "\s+(I|II|III|IV|V|VI)$", ""
    return ($baseName -replace "[^A-Za-z0-9]", "").ToLowerInvariant()
}

function Get-RomanLevel {
    param([string]$Name)

    if ($Name -match "\s+(I|II|III|IV|V|VI)$") {
        switch ($Matches[1]) {
            "I" { return 1 }
            "II" { return 2 }
            "III" { return 3 }
            "IV" { return 4 }
            "V" { return 5 }
            "VI" { return 6 }
        }
    }

    return 1
}

function ConvertFrom-CSharpString {
    param([string]$Text)

    if ($null -eq $Text) {
        return ""
    }

    return $Text.Replace('\"', '"').Replace('\\', '\')
}

function Normalize-ReviewText {
    param([object]$Value)

    if ($null -eq $Value) {
        return ""
    }

    return ([string]$Value -replace "\s+", " ").Trim()
}

function ConvertTo-OptionalInt {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value) -or $Value -eq "-") {
        return $null
    }

    return [int][Math]::Round([decimal]$Value)
}

function Get-RepoRelativePath {
    param([string]$Path)

    if ($Path.StartsWith($repoRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $Path.Substring($repoRoot.Length).TrimStart("\", "/")
    }

    return $Path
}

function Add-Issue {
    param(
        [System.Collections.Generic.List[object]]$Issues,
        [string]$Issue,
        [object]$BibleRow,
        [string]$Field,
        [object]$Bible,
        [object]$Code,
        [string]$File = ""
    )

    $Issues.Add([pscustomobject]@{
        Issue = $Issue
        Tab = $BibleRow.Tab
        Row = $BibleRow.Row
        Name = $BibleRow.PerkName
        Field = $Field
        Bible = Normalize-ReviewText $Bible
        Code = Normalize-ReviewText $Code
        File = Get-RepoRelativePath $File
    }) | Out-Null
}

function Get-ExpectedSkillType {
    param([string]$SkillText)

    $key = ($SkillText -replace "\s+", "")
    $skillMap = @{
        Armor = "Armor"
        Vibroblade = "Vibroblade"
        Vibroknife = "Vibroknife"
        Lightsaber = "Lightsaber"
        HeavyVibroblade = "HeavyVibroblade"
        Spear = "Spear"
        TwinBlade = "TwinBlade"
        Saberstaff = "Saberstaff"
        Katar = "Katar"
        Staff = "Staff"
        Pistol = "Pistol"
        Rifle = "Rifle"
        Throwing = "Throwing"
        Force = "Force"
        Devices = "Devices"
        Piloting = "Piloting"
        Leadership = "Leadership"
        FirstAid = "FirstAid"
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

    if ($Row.SkillRequirements -match "(.+?)\s+(\d+)") {
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

function Get-ExpectedCategory {
    param([object]$Row)

    switch ($Row.Tab) {
        "Armor" { return "General" }
        "Piloting" { return "Piloting" }
        "First Aid" {
            switch ($Row.Style) {
                "Trauma Medic" { return "FirstAidTraumaMedic" }
                "Combat Pharmacology" { return "FirstAidCombatPharmacology" }
                default { throw "No First Aid perk category mapped for style '$($Row.Style)'." }
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
                default { throw "No Devices perk category mapped for style '$($Row.Style)'." }
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
        "Force" {
            if ($Row.Style -like "Light*") { return "ForceLight" }
            if ($Row.Style -like "Dark*") { return "ForceDark" }
            return "ForceUniversal"
        }
        default {
            return (($Row.Tab -replace "\s+", "") + ($Row.Style -replace "\s+", ""))
        }
    }

    return ""
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

$scopedRows = @(
    Import-Csv $manifestFullPath |
        Where-Object {
            @("Aura", "Combat", "Stance", "Toggle", "Trait") -contains $_.Type -and
            $outOfScopeTabs -notcontains $_.Tab
        }
)

$codeRows = @{}
$createPattern = [regex]::new(
    '\.Create\(\s*PerkCategoryType\.(?<category>\w+)\s*,\s*PerkType\.(?<perkType>\w+)\s*\)\s*\.Name\("(?<name>(?:\\.|[^"\\])*)"\)(?<body>.*?)(?=\r?\n\s*(?:private|public|protected|internal)\s|\r?\n\s*_builder\.Create|\z)',
    [System.Text.RegularExpressions.RegexOptions]::Singleline)

foreach ($file in Get-ChildItem $perkDefinitionFullPath -Recurse -Filter "*PerkDefinition.cs") {
    $content = Get-Content $file.FullName -Raw

    foreach ($match in $createPattern.Matches($content)) {
        $name = ConvertFrom-CSharpString $match.Groups["name"].Value
        $baseName = Get-SanitizedName $name
        $parts = [regex]::Split($match.Groups["body"].Value, '\.AddPerkLevel\(\)') | Select-Object -Skip 1
        $level = 0

        foreach ($part in $parts) {
            $level++
            $description = ""
            if ($part -match '\.Description\("((?:\\.|[^"\\])*)"\)') {
                $description = ConvertFrom-CSharpString $Matches[1]
            }

            $price = $null
            if ($part -match '\.Price\((\d+)\)') {
                $price = [int]$Matches[1]
            }

            $skill = ""
            $rank = $null
            if ($part -match '\.RequirementSkill\(SkillType\.(\w+)\s*,\s*(\d+)\)') {
                $skill = $Matches[1]
                $rank = [int]$Matches[2]
            }

            $characterType = "All"
            if ($part -match '\.RequirementCharacterType\(CharacterType\.ForceSensitive\)') {
                $characterType = "Force"
            }
            elseif ($part -match '\.RequirementCharacterType\(CharacterType\.Standard\)') {
                $characterType = "Standard"
            }

            $beastLevel = $null
            if ($part -match '\.RequirementBeastLevel\((\d+)\)') {
                $beastLevel = [int]$Matches[1]
            }

            $beastRole = ""
            if ($part -match '\.RequirementBeastRole\(BeastRoleType\.(\w+)\)') {
                $beastRole = $Matches[1]
            }

            $key = "$baseName|$level"
            if (!$codeRows.ContainsKey($key)) {
                $codeRows[$key] = [System.Collections.Generic.List[object]]::new()
            }

            $codeRows[$key].Add([pscustomobject]@{
                Name = $name
                BaseName = $baseName
                Level = $level
                PerkType = $match.Groups["perkType"].Value
                Category = $match.Groups["category"].Value
                Description = $description
                Price = $price
                Skill = $skill
                Rank = $rank
                CharacterType = $characterType
                BeastLevel = $beastLevel
                BeastRole = $beastRole
                File = $file.FullName
            }) | Out-Null
        }
    }
}

$issues = [System.Collections.Generic.List[object]]::new()

foreach ($row in $scopedRows) {
    $baseName = Get-SanitizedName $row.PerkName
    $level = Get-RomanLevel $row.PerkName
    $key = "$baseName|$level"

    if (!$codeRows.ContainsKey($key)) {
        Add-Issue -Issues $issues -Issue "MissingPerkLevel" -BibleRow $row -Field "PerkLevel" -Bible "present" -Code "missing"
        continue
    }

    $expectedCategory = Get-ExpectedCategory $row
    $candidates = @($codeRows[$key])
    $matchingCandidates = @($candidates | Where-Object { $_.Category -eq $expectedCategory } | Select-Object -First 1)
    $code = if ($matchingCandidates.Count -gt 0) { $matchingCandidates[0] } else { $null }
    if ($null -eq $code -and $candidates.Count -eq 1) {
        $code = $candidates[0]
    }
    elseif ($null -eq $code) {
        Add-Issue -Issues $issues -Issue "AmbiguousPerkLevel" -BibleRow $row -Field "Category" -Bible $expectedCategory -Code (($candidates | ForEach-Object { "$($_.Category):$($_.File)" }) -join "; ")
        continue
    }

    if (![string]::IsNullOrWhiteSpace($expectedCategory) -and $code.Category -ne $expectedCategory) {
        Add-Issue -Issues $issues -Issue "CategoryMismatch" -BibleRow $row -Field "Category" -Bible $expectedCategory -Code $code.Category -File $code.File
    }

    $expectedPrice = ConvertTo-OptionalInt $row.Price
    if ($null -ne $expectedPrice -and $code.Price -ne $expectedPrice) {
        Add-Issue -Issues $issues -Issue "PriceMismatch" -BibleRow $row -Field "Price" -Bible $expectedPrice -Code $code.Price -File $code.File
    }

    if ((Normalize-ReviewText $row.Description) -ne (Normalize-ReviewText $code.Description)) {
        Add-Issue -Issues $issues -Issue "DescriptionMismatch" -BibleRow $row -Field "Description" -Bible $row.Description -Code $code.Description -File $code.File
    }

    if ($row.Tab -eq "Beast Mastery") {
        $expectedBeastLevel = Get-ExpectedBeastLevel $row
        if ($null -ne $expectedBeastLevel) {
            if ($code.BeastLevel -ne $expectedBeastLevel) {
                Add-Issue -Issues $issues -Issue "RequirementMismatch" -BibleRow $row -Field "BeastLevel" -Bible $expectedBeastLevel -Code $code.BeastLevel -File $code.File
            }
        }
        elseif ($null -ne $code.BeastLevel) {
            Add-Issue -Issues $issues -Issue "RequirementMismatch" -BibleRow $row -Field "BeastLevel" -Bible "none" -Code $code.BeastLevel -File $code.File
        }

        $roleMap = @{
            Damage = "Damage"
            Tank = "Tank"
            Balanced = "Balanced"
            Bruiser = "Bruiser"
            Evasion = "Evasion"
            Force = "Force"
        }

        if ((Test-IsBeastPerkRow $row) -and $roleMap.ContainsKey($row.Style)) {
            if ($code.BeastRole -ne $roleMap[$row.Style]) {
                Add-Issue -Issues $issues -Issue "RequirementMismatch" -BibleRow $row -Field "BeastRole" -Bible $roleMap[$row.Style] -Code $code.BeastRole -File $code.File
            }
        }
        elseif (![string]::IsNullOrWhiteSpace($code.BeastRole)) {
            Add-Issue -Issues $issues -Issue "RequirementMismatch" -BibleRow $row -Field "BeastRole" -Bible "none" -Code $code.BeastRole -File $code.File
        }

        $expectedSkillRequirement = Get-ExpectedSkillRequirement $row
        if ($null -ne $expectedSkillRequirement) {
            if ($code.Skill -ne $expectedSkillRequirement.Skill -or $code.Rank -ne $expectedSkillRequirement.Rank) {
                Add-Issue -Issues $issues -Issue "RequirementMismatch" -BibleRow $row -Field "SkillRequirement" -Bible "$($expectedSkillRequirement.Skill) $($expectedSkillRequirement.Rank)" -Code "$($code.Skill) $($code.Rank)" -File $code.File
            }
        }
        elseif (![string]::IsNullOrWhiteSpace($code.Skill)) {
            Add-Issue -Issues $issues -Issue "RequirementMismatch" -BibleRow $row -Field "SkillRequirement" -Bible "none" -Code "$($code.Skill) $($code.Rank)" -File $code.File
        }
    }
    else {
        $expectedSkillRequirement = Get-ExpectedSkillRequirement $row
        if ($null -ne $expectedSkillRequirement) {
            if ($code.Skill -ne $expectedSkillRequirement.Skill -or $code.Rank -ne $expectedSkillRequirement.Rank) {
                Add-Issue -Issues $issues -Issue "RequirementMismatch" -BibleRow $row -Field "SkillRequirement" -Bible "$($expectedSkillRequirement.Skill) $($expectedSkillRequirement.Rank)" -Code "$($code.Skill) $($code.Rank)" -File $code.File
            }
        }

        $expectedCharacterType = "All"
        if ($row.CharacterType -eq "Force") {
            $expectedCharacterType = "Force"
        }
        elseif ($row.CharacterType -eq "Standard") {
            $expectedCharacterType = "Standard"
        }

        if ($code.CharacterType -ne $expectedCharacterType) {
            Add-Issue -Issues $issues -Issue "RequirementMismatch" -BibleRow $row -Field "CharacterType" -Bible $expectedCharacterType -Code $code.CharacterType -File $code.File
        }
    }
}

[pscustomobject]@{
    ScopedBibleRows = $scopedRows.Count
    CodePerkLevels = $codeRows.Count
    Issues = $issues.Count
}

if ($issues.Count -gt 0) {
    $issues | Sort-Object Issue, Tab, @{ Expression = { [int]$_.Row } }, Name |
        Format-Table Issue, Tab, Row, Name, Field, Bible, Code, File -AutoSize -Wrap
}
