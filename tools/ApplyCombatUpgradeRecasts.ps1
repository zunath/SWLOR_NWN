param(
    [string]$AuditPath = "SWLOR.Game.Server\Readmes\CombatUpgradePerkAudit.csv",
    [string]$RecastGroupPath = "SWLOR.Game.Server\Service\AbilityService\RecastGroup.cs"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")

function Resolve-RepoPath {
    param([string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return $Path
    }

    return Join-Path $repoRoot $Path
}

function Get-CooldownSeconds {
    param([string]$Text)

    if ([string]::IsNullOrWhiteSpace($Text)) {
        return $null
    }

    if ($Text -match "([0-9]+(?:\.[0-9]+)?)\s*minutes?") {
        return [double]::Parse($matches[1], [System.Globalization.CultureInfo]::InvariantCulture) * 60.0
    }

    if ($Text -match "([0-9]+(?:\.[0-9]+)?)\s*seconds?") {
        return [double]::Parse($matches[1], [System.Globalization.CultureInfo]::InvariantCulture)
    }

    return $null
}

function Format-FloatLiteral {
    param([double]$Value)

    if ([Math]::Abs($Value - [Math]::Round($Value)) -lt 0.0001) {
        return "$([int][Math]::Round($Value))f"
    }

    return $Value.ToString("0.###", [System.Globalization.CultureInfo]::InvariantCulture) + "f"
}

function Escape-CSharpString {
    param([string]$Value)

    return $Value.Replace("\", "\\").Replace('"', '\"')
}

function Get-DisplayBaseName {
    param([string]$Name)

    return ($Name -replace "\s+(I|II|III|IV|V)$", "").Trim()
}

function Get-ShortName {
    param([string]$Name)

    $shortName = $Name
    if ($shortName.Length -gt 14) {
        $shortName = $shortName.Substring(0, 14).TrimEnd()
    }

    return $shortName
}

function Get-PerkTypeForAbility {
    param(
        [string]$Text,
        [string]$AbilityName
    )

    $escapedName = [regex]::Escape($AbilityName)
    $builderChainBody = "(?s:(?:(?!builder\.Create\().)*?)"
    $pattern = "builder\.Create\(\s*FeatType\.[^,]+,\s*PerkType\.(?<perk>[A-Za-z0-9_]+)\s*\)$builderChainBody\.Name\(`"$escapedName`"\)"
    $match = [regex]::Match($Text, $pattern)
    if (!$match.Success) {
        return $null
    }

    return $match.Groups["perk"].Value
}

function Add-RecastToAbilityChain {
    param(
        [string]$Text,
        [string]$AbilityName,
        [string]$PerkType,
        [double]$CooldownSeconds
    )

    $escapedName = [regex]::Escape($AbilityName)
    $builderChainBody = "(?s:(?:(?!builder\.Create\().)*?)"
    $pattern = "builder\.Create\(\s*FeatType\.[^,]+,\s*PerkType\.$PerkType\s*\)$builderChainBody\.Name\(`"$escapedName`"\)$builderChainBody;"
    $match = [regex]::Match($Text, $pattern)
    if (!$match.Success) {
        return [pscustomobject]@{
            Text = $Text
            Changed = $false
            Reason = "Could not locate builder chain"
        }
    }

    $chain = $match.Value
    if ($chain -match "HasRecastDelay") {
        return [pscustomobject]@{
            Text = $Text
            Changed = $false
            Reason = "Already has recast"
        }
    }

    if ($chain -notmatch "\.HasActivationDelay\([^)]+\)") {
        return [pscustomobject]@{
            Text = $Text
            Changed = $false
            Reason = "No activation delay anchor"
        }
    }

    $delayLiteral = Format-FloatLiteral $CooldownSeconds
    $insert = "`r`n                .HasRecastDelay(RecastGroup.$PerkType, $delayLiteral)"
    $newChain = [regex]::Replace($chain, "(\.HasActivationDelay\([^)]+\))", "`${1}$insert", 1)

    $newText = $Text.Remove($match.Index, $match.Length).Insert($match.Index, $newChain)
    return [pscustomobject]@{
        Text = $newText
        Changed = $true
        Reason = ""
    }
}

$auditFullPath = Resolve-RepoPath $AuditPath
$recastFullPath = Resolve-RepoPath $RecastGroupPath

$rows = Import-Csv $auditFullPath | Where-Object { $_.AuditType -eq "MissingAbilityRecast" }
$rowsByFile = $rows | Group-Object File
$requiredGroups = [ordered]@{}
$updatedAbilities = 0
$skippedRows = New-Object System.Collections.Generic.List[string]

foreach ($fileGroup in $rowsByFile) {
    $abilityPath = Resolve-RepoPath $fileGroup.Name
    if (!(Test-Path $abilityPath)) {
        $skippedRows.Add("$($fileGroup.Name): file not found") | Out-Null
        continue
    }

    $text = [System.IO.File]::ReadAllText($abilityPath)
    $originalText = $text

    foreach ($row in $fileGroup.Group) {
        $cooldownSeconds = Get-CooldownSeconds $row.Details
        if ($null -eq $cooldownSeconds) {
            $skippedRows.Add("$($row.Name): could not parse cooldown '$($row.Details)'") | Out-Null
            continue
        }

        $perkType = Get-PerkTypeForAbility -Text $text -AbilityName $row.Name
        if ([string]::IsNullOrWhiteSpace($perkType)) {
            $skippedRows.Add("$($row.Name): could not locate PerkType") | Out-Null
            continue
        }

        $result = Add-RecastToAbilityChain -Text $text -AbilityName $row.Name -PerkType $perkType -CooldownSeconds $cooldownSeconds
        $text = $result.Text
        if ($result.Changed) {
            $updatedAbilities++
            if (!$requiredGroups.Contains($perkType)) {
                $requiredGroups[$perkType] = Get-DisplayBaseName $row.Name
            }
        }
        elseif ($result.Reason -ne "Already has recast") {
            $skippedRows.Add("$($row.Name): $($result.Reason)") | Out-Null
        }
    }

    if ($text -ne $originalText) {
        [System.IO.File]::WriteAllText($abilityPath, $text)
    }
}

$recastLines = [System.Collections.Generic.List[string]]::new()
$recastLines.AddRange([System.IO.File]::ReadAllLines($recastFullPath))
$existingGroups = @{}
$maxGroupValue = 0
foreach ($line in $recastLines) {
    if ($line -match "^\s*(?<id>[A-Za-z0-9_]+)\s*=\s*(?<value>[0-9]+),") {
        $existingGroups[$matches["id"]] = $true
        $value = [int]$matches["value"]
        if ($value -gt $maxGroupValue) {
            $maxGroupValue = $value
        }
    }
}

$newGroupLines = New-Object System.Collections.Generic.List[string]
foreach ($entry in $requiredGroups.GetEnumerator()) {
    if ($existingGroups.ContainsKey($entry.Key)) {
        continue
    }

    $maxGroupValue++
    $displayName = Escape-CSharpString $entry.Value
    $shortName = Escape-CSharpString (Get-ShortName $entry.Value)
    $newGroupLines.Add("        [RecastGroup(`"$displayName`", `"$shortName`", true)]") | Out-Null
    $newGroupLines.Add("        $($entry.Key) = $maxGroupValue,") | Out-Null
}

if ($newGroupLines.Count -gt 0) {
    $enumStartIndex = -1
    for ($i = 0; $i -lt $recastLines.Count; $i++) {
        if ($recastLines[$i] -match "public\s+enum\s+RecastGroup\b") {
            $enumStartIndex = $i
            break
        }
    }

    if ($enumStartIndex -lt 0) {
        throw "Could not find RecastGroup enum declaration."
    }

    $insertIndex = -1
    for ($i = $enumStartIndex + 1; $i -lt $recastLines.Count; $i++) {
        if ($recastLines[$i].Trim() -eq "}") {
            $insertIndex = $i
            break
        }
    }

    if ($insertIndex -lt 0) {
        throw "Could not find RecastGroup enum closing brace."
    }

    $recastLines.InsertRange($insertIndex, [string[]]$newGroupLines)
    [System.IO.File]::WriteAllLines($recastFullPath, $recastLines)
}

Write-Host "Updated $updatedAbilities ability recast declarations."
Write-Host "Added $($newGroupLines.Count / 2) recast groups."
if ($skippedRows.Count -gt 0) {
    Write-Host "Skipped $($skippedRows.Count) rows:"
    foreach ($skip in $skippedRows) {
        Write-Host " - $skip"
    }
}
