param(
    [string]$WorkbookPath = "design\bible\SWLOR Design Bible - Combat Upgrade.xlsx",
    [string]$AbilityDirectory = "SWLOR.Game.Server\Feature\AbilityDefinition\NPC"
)

$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
Add-Type -AssemblyName System.Xml.Linq

$RepositoryRoot = Resolve-Path "."
$WorkbookFullPath = Join-Path $RepositoryRoot $WorkbookPath
$AbilityFullPath = Join-Path $RepositoryRoot $AbilityDirectory

function Find-CallArgs {
    param(
        [string]$Content,
        [string]$CallName
    )

    $index = $Content.IndexOf($CallName, [StringComparison]::Ordinal)
    if ($index -lt 0) {
        return $null
    }

    $open = $Content.IndexOf("(", $index + $CallName.Length)
    if ($open -lt 0) {
        return $null
    }

    $depth = 0
    $inString = $false

    for ($i = $open; $i -lt $Content.Length; $i++) {
        $character = $Content[$i]

        if ($character -eq '"' -and ($i -eq 0 -or $Content[$i - 1] -ne "\")) {
            $inString = -not $inString
        }

        if ($inString) {
            continue
        }

        if ($character -eq "(") {
            $depth++
        }
        elseif ($character -eq ")") {
            $depth--
            if ($depth -eq 0) {
                return $Content.Substring($open + 1, $i - $open - 1)
            }
        }
    }

    return $null
}

function Split-Arguments {
    param([string]$ArgumentsText)

    $arguments = New-Object System.Collections.Generic.List[string]
    $start = 0
    $parenDepth = 0
    $braceDepth = 0
    $bracketDepth = 0
    $inString = $false

    for ($i = 0; $i -lt $ArgumentsText.Length; $i++) {
        $character = $ArgumentsText[$i]

        if ($character -eq '"' -and ($i -eq 0 -or $ArgumentsText[$i - 1] -ne "\")) {
            $inString = -not $inString
        }

        if ($inString) {
            continue
        }

        switch ($character) {
            "(" { $parenDepth++ }
            ")" { $parenDepth-- }
            "{" { $braceDepth++ }
            "}" { $braceDepth-- }
            "[" { $bracketDepth++ }
            "]" { $bracketDepth-- }
            "," {
                if ($parenDepth -eq 0 -and $braceDepth -eq 0 -and $bracketDepth -eq 0) {
                    $arguments.Add($ArgumentsText.Substring($start, $i - $start).Trim())
                    $start = $i + 1
                }
            }
        }
    }

    if ($start -lt $ArgumentsText.Length) {
        $arguments.Add($ArgumentsText.Substring($start).Trim())
    }

    return $arguments
}

function Strip-NamedArgument {
    param([string]$Value)

    if ($null -eq $Value) {
        return ""
    }

    $trimmed = $Value.Trim()
    $match = [regex]::Match($trimmed, "^[A-Za-z_][A-Za-z0-9_]*\s*:\s*(.+)$", "Singleline")
    if ($match.Success) {
        return $match.Groups[1].Value.Trim()
    }

    return $trimmed
}

function Get-NamedArgument {
    param(
        $Arguments,
        [string]$Name
    )

    foreach ($argument in $Arguments) {
        $match = [regex]::Match($argument, "^" + [regex]::Escape($Name) + "\s*:\s*(.+)$", "Singleline")
        if ($match.Success) {
            return $match.Groups[1].Value.Trim()
        }
    }

    return $null
}

function Get-OptionalPositionalArgument {
    param(
        $Arguments,
        [int]$Index
    )

    if ($Arguments.Count -le $Index) {
        return $null
    }

    $argument = $Arguments[$Index].Trim()
    if ($argument -match "^[A-Za-z_][A-Za-z0-9_]*\s*:") {
        return $null
    }

    return $argument
}

function Convert-TokenToText {
    param([string]$Token)

    if ($null -eq $Token) {
        return ""
    }

    $tokenText = (Strip-NamedArgument $Token).Trim().TrimEnd(";")
    $stringMatch = [regex]::Match($tokenText, '"([^"]*)"')
    if ($stringMatch.Success) {
        return $stringMatch.Groups[1].Value
    }

    $typeofMatch = [regex]::Match($tokenText, "typeof\(([A-Za-z0-9_]+)\)")
    if ($typeofMatch.Success) {
        return $typeofMatch.Groups[1].Value
    }

    $enumMatch = [regex]::Match($tokenText, "([A-Za-z0-9_]+)\.([A-Za-z0-9_]+)")
    if ($enumMatch.Success) {
        return $enumMatch.Groups[2].Value
    }

    return $tokenText
}

function Convert-TokenToNumber {
    param([string]$Token)

    $numberText = (Strip-NamedArgument $Token).Trim() -replace "[fFdDmM]$", ""
    return [double]::Parse($numberText, [Globalization.CultureInfo]::InvariantCulture)
}

function Format-Number {
    param([double]$Value)

    if ([Math]::Abs($Value - [Math]::Round($Value)) -lt 0.0001) {
        return ([int][Math]::Round($Value)).ToString([Globalization.CultureInfo]::InvariantCulture)
    }

    return $Value.ToString("0.##", [Globalization.CultureInfo]::InvariantCulture)
}

function Convert-EnumToFriendlyText {
    param([string]$Token)

    $text = Convert-TokenToText $Token
    if ([string]::IsNullOrWhiteSpace($text) -or $text -eq "Invalid") {
        return "None"
    }

    return $text
}

function Convert-StatusEffectToFriendlyText {
    param([string]$TypeName)

    if ([string]::IsNullOrWhiteSpace($TypeName) -or $TypeName -eq "null") {
        return "None"
    }

    $name = $TypeName -replace "StatusEffect$", ""
    $friendlyNames = @{
        "Bleed" = "Bleed"
        "Vulnerable" = "Vulnerable"
        "Poison" = "Poison"
        "Burn" = "Burn"
        "Knockdown" = "Knockdown"
        "Terrified" = "Terrified"
        "Disoriented" = "Disoriented"
        "IronCarapace" = "Iron Carapace"
        "Sunder" = "Sunder"
        "Hamstring" = "Hamstring"
        "Dazed" = "Dazed"
        "Toxin" = "Toxin"
        "Freezing" = "Freezing"
        "Shock" = "Shock"
        "MarkedForDeath" = "Marked for Death"
        "Hemorrhage" = "Hemorrhage"
        "ExposeWeakPoint" = "Expose Weak Point"
        "ForceErosion" = "Force Erosion"
        "ForceSuppression" = "Force Suppression"
        "ForceDisruption" = "Force Disruption"
        "Stunned" = "Stunned"
        "Suppression" = "Suppression"
        "Exposed" = "Exposed"
        "Weakened" = "Weakened"
        "Hobble" = "Hobble"
        "Immobilized" = "Immobilized"
        "Marked" = "Marked"
        "FoggyMind" = "Foggy Mind"
    }

    if ($friendlyNames.ContainsKey($name)) {
        return $friendlyNames[$name]
    }

    return ([regex]::Replace($name, "(?<!^)([A-Z])", " `$1")).Trim()
}

function Get-StatusEffectFromSource {
    param(
        [string]$Content,
        [string]$FallbackToken
    )

    $fallback = Convert-TokenToText $FallbackToken
    if (-not [string]::IsNullOrWhiteSpace($fallback) -and $fallback -ne "null") {
        return $fallback
    }

    $factoryMatch = [regex]::Match($Content, "statusEffectFactory:\s*\(\)\s*=>\s*new\s+([A-Za-z0-9_]+StatusEffect)\s*\(", "Singleline")
    if ($factoryMatch.Success) {
        return $factoryMatch.Groups[1].Value
    }

    $newStatusMatch = [regex]::Match($Content, "new\s+([A-Za-z0-9_]+StatusEffect)\s*\(", "Singleline")
    if ($newStatusMatch.Success) {
        return $newStatusMatch.Groups[1].Value
    }

    return ""
}

function New-Notes {
    param($Row)

    $parts = New-Object System.Collections.Generic.List[string]

    if ($null -ne $Row.BaseDamage) {
        $parts.Add("Base damage $(Format-Number $Row.BaseDamage).")
    }

    if ($Row.Shape -eq "Cone" -and $Row.Width -gt 0) {
        $parts.Add("Cone width $(Format-Number $Row.Width)m.")
    }
    elseif ($Row.Shape -eq "Line" -and $Row.Width -gt 0) {
        $parts.Add("Line width $(Format-Number $Row.Width)m.")
    }

    if ($Row.CenterOnActivator) {
        $parts.Add("Originates on the caster.")
    }

    if ($Row.StatusEffect -ne "None" -and $Row.Resistance -ne "None") {
        $parts.Add("$($Row.Resistance) resistance reduces status duration and matching damage.")
    }
    elseif ($Row.Resistance -ne "None") {
        $parts.Add("$($Row.Resistance) resistance reduces matching damage.")
    }

    if ($Row.EnmityBonus -gt 0) {
        $parts.Add("Generates extra enmity (+$($Row.EnmityBonus)).")
    }

    if ($Row.UsesNpcScaling) {
        $parts.Add("Uses NPC stat scaling.")
    }

    if ($parts.Count -eq 0) {
        $parts.Add("NPC innate ability.")
    }

    return $parts -join " "
}

function New-AbilityRow {
    param(
        [string]$FilePath,
        [string]$Content
    )

    $row = [ordered]@{
        Ability = ""
        Feat = ""
        Targeting = ""
        Hostile = "No"
        Area = "No"
        RequiresTarget = "No"
        MaxRange = "Self"
        ActivationDelay = "0s"
        RecastGroup = ""
        Recast = "0s"
        STM = "0 STM"
        DamageResistance = "None / None"
        StatusEffect = "None"
        Duration = "0s"
        Notes = ""
        SourceFile = Split-Path $FilePath -Leaf
        BaseDamage = $null
        Damage = "None"
        Resistance = "None"
        Shape = ""
        Width = 0.0
        CenterOnActivator = $false
        EnmityBonus = 0
        UsesNpcScaling = $true
    }

    $arguments = $null
    $callKind = ""
    foreach ($candidate in @(
        "NPCSignatureAbility.BuildSingleTarget",
        "NPCSignatureAbility.BuildArea",
        "InnateAbility.BuildSingleTarget",
        "InnateAbility.BuildArea",
        "InnateAbility.BuildSelfBuff"
    )) {
        $argumentsText = Find-CallArgs $Content $candidate
        if ($argumentsText) {
            $arguments = Split-Arguments $argumentsText
            $callKind = $candidate
            break
        }
    }

    if ($arguments) {
        $isNpcSignature = $callKind.StartsWith("NPCSignatureAbility")
        $isSingleTarget = $callKind.EndsWith("BuildSingleTarget")
        $isArea = $callKind.EndsWith("BuildArea")
        $isSelfBuff = $callKind.EndsWith("BuildSelfBuff")

        if ($isSingleTarget) {
            if ($isNpcSignature) {
                $row.Feat = "FeatType." + (Convert-TokenToText $arguments[1])
                $row.Ability = Convert-TokenToText $arguments[2]
                $row.ActivationDelay = "$(Format-Number (Convert-TokenToNumber $arguments[5]))s"
                $row.RecastGroup = "Capstone"
                $row.Recast = "$(Format-Number (Convert-TokenToNumber $arguments[6]))s"
                $row.STM = "$(Format-Number (Convert-TokenToNumber $arguments[7])) STM"
                $row.BaseDamage = Convert-TokenToNumber $arguments[8]
                $row.Duration = "$(Format-Number (Convert-TokenToNumber $arguments[9]))s"
                $statusToken = $arguments[10]
                $row.Damage = Convert-EnumToFriendlyText $arguments[11]
                $row.Resistance = Convert-EnumToFriendlyText $arguments[12]
                $maxRange = Get-NamedArgument $arguments "maxRange"
                if (-not $maxRange) {
                    $maxRange = Get-OptionalPositionalArgument $arguments 14
                }
            }
            else {
                $row.Feat = "FeatType." + (Convert-TokenToText $arguments[1])
                $row.Ability = Convert-TokenToText $arguments[2]
                $row.RecastGroup = Convert-TokenToText $arguments[5]
                $row.ActivationDelay = "$(Format-Number (Convert-TokenToNumber $arguments[6]))s"
                $row.Recast = "$(Format-Number (Convert-TokenToNumber $arguments[7]))s"
                $row.STM = "$(Format-Number (Convert-TokenToNumber $arguments[8])) STM"
                $row.BaseDamage = Convert-TokenToNumber $arguments[9]
                $row.Duration = "$(Format-Number (Convert-TokenToNumber $arguments[10]))s"
                $statusToken = $arguments[11]
                $row.Damage = Convert-EnumToFriendlyText $arguments[12]
                $row.Resistance = Convert-EnumToFriendlyText $arguments[13]
                $maxRange = Get-NamedArgument $arguments "maxRange"
                if (-not $maxRange) {
                    $maxRange = Get-OptionalPositionalArgument $arguments 15
                }
            }

            $row.Targeting = "Single enemy target"
            $row.Hostile = "Yes"
            $row.Area = "No"
            $row.RequiresTarget = "Yes"
            $row.MaxRange = if ($maxRange) { "$(Format-Number (Convert-TokenToNumber $maxRange))m" } else { "5m default" }
            $row.StatusEffect = Convert-StatusEffectToFriendlyText (Get-StatusEffectFromSource $Content $statusToken)
        }
        elseif ($isArea) {
            if ($isNpcSignature) {
                $row.Feat = "FeatType." + (Convert-TokenToText $arguments[1])
                $row.Ability = Convert-TokenToText $arguments[2]
                $row.ActivationDelay = "$(Format-Number (Convert-TokenToNumber $arguments[5]))s"
                $row.RecastGroup = "Capstone"
                $row.Recast = "$(Format-Number (Convert-TokenToNumber $arguments[6]))s"
                $row.STM = "$(Format-Number (Convert-TokenToNumber $arguments[7])) STM"
                $row.BaseDamage = Convert-TokenToNumber $arguments[8]
                $row.Duration = "$(Format-Number (Convert-TokenToNumber $arguments[9]))s"
                $statusToken = $arguments[10]
                $shape = Convert-TokenToText $arguments[11]
                $lengthOrRadius = Convert-TokenToNumber $arguments[12]
                $width = Convert-TokenToNumber $arguments[13]
                $row.Damage = Convert-EnumToFriendlyText $arguments[14]
                $row.Resistance = Convert-EnumToFriendlyText $arguments[15]
                $maxRange = Get-NamedArgument $arguments "maxRange"
                if (-not $maxRange) {
                    $maxRange = Get-OptionalPositionalArgument $arguments 18
                }
                $centerOnActivator = Get-NamedArgument $arguments "centerOnActivator"
                if (-not $centerOnActivator) {
                    $centerOnActivator = Get-OptionalPositionalArgument $arguments 19
                }
            }
            else {
                $row.Feat = "FeatType." + (Convert-TokenToText $arguments[1])
                $row.Ability = Convert-TokenToText $arguments[2]
                $row.RecastGroup = Convert-TokenToText $arguments[5]
                $row.ActivationDelay = "$(Format-Number (Convert-TokenToNumber $arguments[6]))s"
                $row.Recast = "$(Format-Number (Convert-TokenToNumber $arguments[7]))s"
                $row.STM = "$(Format-Number (Convert-TokenToNumber $arguments[8])) STM"
                $row.BaseDamage = Convert-TokenToNumber $arguments[9]
                $row.Duration = "$(Format-Number (Convert-TokenToNumber $arguments[10]))s"
                $statusToken = $arguments[11]
                $shape = Convert-TokenToText $arguments[12]
                $lengthOrRadius = Convert-TokenToNumber $arguments[13]
                $width = Convert-TokenToNumber $arguments[14]
                $row.Damage = Convert-EnumToFriendlyText $arguments[15]
                $row.Resistance = Convert-EnumToFriendlyText $arguments[16]
                $maxRange = Get-NamedArgument $arguments "maxRange"
                if (-not $maxRange) {
                    $maxRange = Get-OptionalPositionalArgument $arguments 19
                }
                $centerOnActivator = Get-NamedArgument $arguments "centerOnActivator"
                if (-not $centerOnActivator) {
                    $centerOnActivator = Get-OptionalPositionalArgument $arguments 20
                }
            }

            $row.Shape = $shape
            $row.Width = $width
            $row.CenterOnActivator = $centerOnActivator -match "true"

            switch ($shape) {
                "Cone" { $row.Targeting = "$(Format-Number $lengthOrRadius)m x $(Format-Number $width)m cone" }
                "Line" { $row.Targeting = "$(Format-Number $lengthOrRadius)m x $(Format-Number $width)m line" }
                default {
                    $row.Targeting = if ($row.CenterOnActivator) {
                        "$(Format-Number $lengthOrRadius)m sphere centered on caster"
                    }
                    else {
                        "$(Format-Number $lengthOrRadius)m sphere"
                    }
                }
            }

            $row.Hostile = "Yes"
            $row.Area = "Yes"
            $row.RequiresTarget = if ($row.CenterOnActivator) { "No" } else { "Yes" }
            $row.MaxRange = if ($row.CenterOnActivator) {
                "Self"
            }
            elseif ($maxRange -and (Convert-TokenToNumber $maxRange) -gt 0) {
                "$(Format-Number (Convert-TokenToNumber $maxRange))m"
            }
            else {
                "5m default"
            }
            $row.StatusEffect = Convert-StatusEffectToFriendlyText (Get-StatusEffectFromSource $Content $statusToken)
        }
        elseif ($isSelfBuff) {
            $row.Feat = "FeatType." + (Convert-TokenToText $arguments[1])
            $row.Ability = Convert-TokenToText $arguments[2]
            $row.RecastGroup = Convert-TokenToText $arguments[5]
            $row.ActivationDelay = "$(Format-Number (Convert-TokenToNumber $arguments[6]))s"
            $row.Recast = "$(Format-Number (Convert-TokenToNumber $arguments[7]))s"
            $row.STM = "$(Format-Number (Convert-TokenToNumber $arguments[8])) STM"
            $row.Duration = "$(Format-Number (Convert-TokenToNumber $arguments[10]))s"
            $row.Targeting = "Self"
            $row.StatusEffect = Convert-StatusEffectToFriendlyText (Convert-TokenToText $arguments[9])
            $row.UsesNpcScaling = $false
        }
    }
    else {
        $row.Feat = "FeatType." + ([regex]::Match($Content, "\.Create\(\s*FeatType\.([A-Za-z0-9_]+)").Groups[1].Value)
        $row.Ability = [regex]::Match($Content, '\.Name\("([^"]+)"\)').Groups[1].Value
        $activationDelay = Convert-TokenToNumber ([regex]::Match($Content, "\.HasActivationDelay\(([^\)]+)\)").Groups[1].Value)
        $row.ActivationDelay = "$(Format-Number $activationDelay)s"

        $recastMatch = [regex]::Match($Content, "\.HasRecastDelay\(\s*RecastGroup\.([A-Za-z0-9_]+)\s*,\s*([^\)]+)\)")
        $row.RecastGroup = $recastMatch.Groups[1].Value
        $row.Recast = "$(Format-Number (Convert-TokenToNumber $recastMatch.Groups[2].Value))s"
        $stamina = Convert-TokenToNumber ([regex]::Match($Content, "\.RequirementStamina\(([^\)]+)\)").Groups[1].Value)
        $row.STM = "$(Format-Number $stamina) STM"
        $row.Hostile = if ($Content.Contains(".IsHostileAbility()")) { "Yes" } else { "No" }
        $row.Area = if ($Content.Contains(".IsAreaAbility()")) { "Yes" } else { "No" }
        $row.RequiresTarget = if ($row.CenterOnActivator) {
            "No"
        }
        elseif ($Content.Contains(".RequiresTarget()")) {
            "Yes"
        }
        else {
            "No"
        }

        $maxRangeMatch = [regex]::Match($Content, "\.HasMaxRange\(([^\)]+)\)")
        $maxRange = if ($maxRangeMatch.Success) { Convert-TokenToNumber $maxRangeMatch.Groups[1].Value } else { $null }

        if ($Content.Contains(".IsSingleTargetAbility()")) {
            $row.Targeting = "Single enemy target"
            $row.MaxRange = if ($null -ne $maxRange) { "$(Format-Number $maxRange)m" } else { "5m default" }
        }
        elseif ($Content.Contains(".HasActivationTargetingCone")) {
            $targetingArguments = Split-Arguments (Find-CallArgs $Content ".HasActivationTargetingCone")
            $lengthOrRadius = Convert-TokenToNumber $targetingArguments[0]
            $row.Width = Convert-TokenToNumber $targetingArguments[1]
            $row.Shape = "Cone"
            $row.Targeting = "$(Format-Number $lengthOrRadius)m x $(Format-Number $row.Width)m cone"
            $row.MaxRange = if ($null -ne $maxRange) { "$(Format-Number $maxRange)m" } else { "$(Format-Number $lengthOrRadius)m" }
        }
        elseif ($Content.Contains(".HasActivationTargetingLine")) {
            $targetingArguments = Split-Arguments (Find-CallArgs $Content ".HasActivationTargetingLine")
            $lengthOrRadius = Convert-TokenToNumber $targetingArguments[0]
            $row.Width = Convert-TokenToNumber $targetingArguments[1]
            $row.Shape = "Line"
            $row.Targeting = "$(Format-Number $lengthOrRadius)m x $(Format-Number $row.Width)m line"
            $row.MaxRange = if ($null -ne $maxRange) { "$(Format-Number $maxRange)m" } else { "$(Format-Number $lengthOrRadius)m" }
        }
        elseif ($Content.Contains(".HasActivationTargetingSphere")) {
            $targetingArguments = Split-Arguments (Find-CallArgs $Content ".HasActivationTargetingSphere")
            $lengthOrRadius = Convert-TokenToNumber $targetingArguments[0]
            $row.Shape = "Sphere"
            $row.CenterOnActivator = $Content.Contains("AbilityTargetingFlags.OriginOnSelf")
            $row.Targeting = if ($row.CenterOnActivator) {
                "$(Format-Number $lengthOrRadius)m sphere centered on caster"
            }
            else {
                "$(Format-Number $lengthOrRadius)m sphere"
            }
            $row.MaxRange = if ($row.CenterOnActivator) {
                "Self"
            }
            elseif ($null -ne $maxRange -and $maxRange -gt 0) {
                "$(Format-Number $maxRange)m"
            }
            else {
                "$(Format-Number $lengthOrRadius)m"
            }
        }
        else {
            $row.Targeting = "Self"
            $row.MaxRange = "Self"
        }

        if ($row.CenterOnActivator) {
            $row.RequiresTarget = "No"
        }

        $statusMatch = [regex]::Match($Content, "typeof\(([A-Za-z0-9_]+StatusEffect)\)|new\s+([A-Za-z0-9_]+StatusEffect)\s*\(", "Singleline")
        if ($statusMatch.Success) {
            if ($statusMatch.Groups[1].Success) {
                $statusName = $statusMatch.Groups[1].Value
            }
            else {
                $statusName = $statusMatch.Groups[2].Value
            }
        }
        else {
            $statusName = Get-StatusEffectFromSource $Content ""
        }
        $row.StatusEffect = Convert-StatusEffectToFriendlyText $statusName

        $damageMatch = [regex]::Match($Content, "damageType:\s*CombatDamageType\.([A-Za-z0-9_]+)", "Singleline")
        $resistanceMatch = [regex]::Match($Content, "statusResistanceType:\s*ResistanceType\.([A-Za-z0-9_]+)", "Singleline")
        $row.Damage = if ($damageMatch.Success) { Convert-EnumToFriendlyText $damageMatch.Groups[1].Value } else { "None" }
        $row.Resistance = if ($resistanceMatch.Success) { Convert-EnumToFriendlyText $resistanceMatch.Groups[1].Value } else { "None" }

        $impactNumbers = [regex]::Match($Content, "InnateAbility\.ResolveSkillType\([^\)]*\),\s*([0-9]+(?:\.[0-9]+)?f?),\s*([0-9]+(?:\.[0-9]+)?f?),", "Singleline")
        if ($impactNumbers.Success) {
            $row.BaseDamage = Convert-TokenToNumber $impactNumbers.Groups[1].Value
            $row.Duration = "$(Format-Number (Convert-TokenToNumber $impactNumbers.Groups[2].Value))s"
        }
        else {
            $durationMatch = [regex]::Match($Content, "StatusEffect\.ApplyStatusEffect\([^,]+,[^,]+,[^,]+,\s*([0-9]+(?:\.[0-9]+)?f?)\)", "Singleline")
            if ($durationMatch.Success) {
                $row.Duration = "$(Format-Number (Convert-TokenToNumber $durationMatch.Groups[1].Value))s"
                $row.BaseDamage = $null
                $row.Damage = "None"
                $row.Resistance = "None"
                $row.UsesNpcScaling = $false
            }
        }

        $enmityMatch = [regex]::Match($Content, "enmityBonus:\s*([0-9]+)")
        if ($enmityMatch.Success) {
            $row.EnmityBonus = [int]$enmityMatch.Groups[1].Value
        }
    }

    if (-not $row.Feat.StartsWith("FeatType.")) {
        $row.Feat = "FeatType." + $row.Feat
    }

    $row.DamageResistance = "$($row.Damage) / $($row.Resistance)"
    $row.Notes = New-Notes ([pscustomobject]$row)

    return [pscustomobject]$row
}

function Get-ZipText {
    param(
        [System.IO.Compression.ZipArchive]$Zip,
        [string]$Name
    )

    $entry = $Zip.GetEntry($Name)
    if (-not $entry) {
        throw "Missing zip entry $Name"
    }

    $stream = $entry.Open()
    try {
        $reader = New-Object System.IO.StreamReader($stream)
        try {
            return $reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }
    }
    finally {
        $stream.Dispose()
    }
}

function Get-WorksheetPath {
    param(
        [System.IO.Compression.ZipArchive]$Zip,
        [string]$SheetName
    )

    [xml]$workbook = Get-ZipText $Zip "xl/workbook.xml"
    [xml]$relationships = Get-ZipText $Zip "xl/_rels/workbook.xml.rels"
    $sheetNode = $workbook.workbook.sheets.sheet | Where-Object { $_.name -eq $SheetName }
    if (-not $sheetNode) {
        throw "Sheet '$SheetName' was not found."
    }

    $relationshipId = $sheetNode.GetAttribute("id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships")
    $relationship = $relationships.Relationships.Relationship | Where-Object { $_.Id -eq $relationshipId }
    $target = [string]$relationship.Target
    if ($target.StartsWith("/")) {
        return $target.TrimStart("/")
    }

    if ($target.StartsWith("xl/")) {
        return $target
    }

    return "xl/$target"
}

function Get-CellColumn {
    param([string]$Address)

    return ([regex]::Match($Address, "^[A-Z]+")).Value
}

function Get-CellText {
    param(
        [System.Xml.Linq.XElement]$Cell,
        [string[]]$SharedStrings,
        [System.Xml.Linq.XNamespace]$Namespace
    )

    if ($null -eq $Cell) {
        return ""
    }

    $typeAttribute = $Cell.Attribute("t")
    $type = if ($typeAttribute) { $typeAttribute.Value } else { "" }
    if ($type -eq "inlineStr") {
        return [string]::Concat(($Cell.Descendants($Namespace + "t") | ForEach-Object { $_.Value }))
    }

    $valueElement = $Cell.Element($Namespace + "v")
    $value = if ($valueElement) { $valueElement.Value } else { "" }
    if ($type -eq "s" -and $value -match "^\d+$") {
        return $SharedStrings[[int]$value]
    }

    return $value
}

function Read-SharedStrings {
    param([System.IO.Compression.ZipArchive]$Zip)

    if (-not $Zip.GetEntry("xl/sharedStrings.xml")) {
        return @()
    }

    [System.Xml.Linq.XDocument]$sharedStringDocument = [System.Xml.Linq.XDocument]::Parse((Get-ZipText $Zip "xl/sharedStrings.xml"))
    [System.Xml.Linq.XNamespace]$namespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
    return @($sharedStringDocument.Descendants($namespace + "si") | ForEach-Object {
        [string]::Concat(($_.Descendants($namespace + "t") | ForEach-Object { $_.Value }))
    })
}

function New-InlineStringCell {
    param(
        [System.Xml.Linq.XNamespace]$Namespace,
        [string]$Address,
        [string]$Style,
        [string]$Value
    )

    $cell = [System.Xml.Linq.XElement]::new($Namespace + "c")
    $cell.SetAttributeValue("r", $Address)
    if (-not [string]::IsNullOrWhiteSpace($Style)) {
        $cell.SetAttributeValue("s", $Style)
    }
    $cell.SetAttributeValue("t", "inlineStr")

    $inlineString = [System.Xml.Linq.XElement]::new($Namespace + "is")
    $text = [System.Xml.Linq.XElement]::new($Namespace + "t")
    $text.Value = $Value
    if ($Value.Trim() -ne $Value) {
        $text.SetAttributeValue([System.Xml.Linq.XNamespace]::Xml + "space", "preserve")
    }

    $inlineString.Add($text)
    $cell.Add($inlineString)
    return $cell
}

$abilityRows = Get-ChildItem -Path $AbilityFullPath -Filter "*AbilityDefinition.cs" -File |
    ForEach-Object {
        $file = $_
        try {
            New-AbilityRow $file.FullName (Get-Content -Path $file.FullName -Raw)
        }
        catch {
            throw "Failed to parse $($file.FullName): $($_.Exception.Message)"
        }
    }

$unparsedRows = @($abilityRows | Where-Object {
    [string]::IsNullOrWhiteSpace($_.Ability) -or
    [string]::IsNullOrWhiteSpace($_.Feat) -or
    [string]::IsNullOrWhiteSpace($_.SourceFile)
})
if ($unparsedRows.Count -gt 0) {
    throw "Failed to parse $($unparsedRows.Count) NPC ability rows."
}

$zip = [System.IO.Compression.ZipFile]::Open($WorkbookFullPath, [System.IO.Compression.ZipArchiveMode]::Update)
try {
    $worksheetPath = Get-WorksheetPath $zip "NPC Abilities"
    [System.Xml.Linq.XDocument]$worksheet = [System.Xml.Linq.XDocument]::Parse((Get-ZipText $zip $worksheetPath))
    [System.Xml.Linq.XNamespace]$namespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
    $sharedStrings = Read-SharedStrings $zip
    $sheetData = $worksheet.Root.Element($namespace + "sheetData")
    $headerRow = $sheetData.Elements($namespace + "row") | Where-Object { $_.Attribute("r").Value -eq "1" } | Select-Object -First 1
    $dataTemplate = $sheetData.Elements($namespace + "row") | Where-Object { $_.Attribute("r").Value -eq "2" } | Select-Object -First 1
    $stylesByColumn = @{}
    $existingOrder = @{}
    $orderIndex = 0

    foreach ($cell in $dataTemplate.Elements($namespace + "c")) {
        $column = Get-CellColumn $cell.Attribute("r").Value
        $styleAttribute = $cell.Attribute("s")
        $stylesByColumn[$column] = if ($styleAttribute) { $styleAttribute.Value } else { "" }
    }

    foreach ($row in $sheetData.Elements($namespace + "row")) {
        if ([int]$row.Attribute("r").Value -le 1) {
            continue
        }

        $abilityCell = $row.Elements($namespace + "c") |
            Where-Object { (Get-CellColumn $_.Attribute("r").Value) -eq "A" } |
            Select-Object -First 1
        $abilityName = Get-CellText $abilityCell $sharedStrings $namespace
        if (-not [string]::IsNullOrWhiteSpace($abilityName) -and -not $existingOrder.ContainsKey($abilityName)) {
            $existingOrder[$abilityName] = $orderIndex
            $orderIndex++
        }
    }

    $orderedRows = $abilityRows | Sort-Object `
        @{ Expression = { if ($existingOrder.ContainsKey($_.Ability)) { $existingOrder[$_.Ability] } else { 100000 } } },
        @{ Expression = { $_.Ability } }

    $sheetData.RemoveNodes()
    $sheetData.Add($headerRow)

    $columns = @("A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P")
    $properties = @(
        "Ability",
        "Feat",
        "Targeting",
        "Hostile",
        "Area",
        "RequiresTarget",
        "MaxRange",
        "ActivationDelay",
        "RecastGroup",
        "Recast",
        "STM",
        "DamageResistance",
        "StatusEffect",
        "Duration",
        "Notes",
        "SourceFile"
    )

    $rowNumber = 2
    foreach ($abilityRow in $orderedRows) {
        $rowElement = [System.Xml.Linq.XElement]::new($namespace + "row")
        $rowElement.SetAttributeValue("r", $rowNumber)
        $rowElement.SetAttributeValue("spans", "1:16")

        for ($index = 0; $index -lt $columns.Count; $index++) {
            $column = $columns[$index]
            $propertyName = $properties[$index]
            $cellValue = [string]$abilityRow.$propertyName
            $style = if ($stylesByColumn.ContainsKey($column)) { $stylesByColumn[$column] } else { "" }
            $rowElement.Add((New-InlineStringCell $namespace "$column$rowNumber" $style $cellValue))
        }

        $sheetData.Add($rowElement)
        $rowNumber++
    }

    $lastRow = $rowNumber - 1
    $dimension = $worksheet.Root.Element($namespace + "dimension")
    if ($dimension) {
        $dimension.SetAttributeValue("ref", "A1:P$lastRow")
    }

    $autoFilter = $worksheet.Root.Element($namespace + "autoFilter")
    if ($autoFilter) {
        $autoFilter.SetAttributeValue("ref", "A1:P$lastRow")
    }

    $entry = $zip.GetEntry($worksheetPath)
    $entry.Delete()
    $entry = $zip.CreateEntry($worksheetPath)
    $stream = $entry.Open()
    try {
        $settings = [System.Xml.XmlWriterSettings]::new()
        $settings.Encoding = [System.Text.UTF8Encoding]::new($false)
        $settings.OmitXmlDeclaration = $false
        $settings.Indent = $false
        $writer = [System.Xml.XmlWriter]::Create($stream, $settings)
        try {
            $worksheet.Save($writer)
        }
        finally {
            $writer.Dispose()
        }
    }
    finally {
        $stream.Dispose()
    }

    Write-Host "Updated NPC Abilities rows: $($orderedRows.Count)"
    Write-Host "Worksheet range: A1:P$lastRow"
}
finally {
    $zip.Dispose()
}
