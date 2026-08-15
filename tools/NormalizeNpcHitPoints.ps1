param(
    [switch]$CheckOnly
)

$ErrorActionPreference = "Stop"

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$utcDirectory = Join-Path $repositoryRoot "Module\utc"
$utiDirectory = Join-Path $repositoryRoot "Module\uti"
$utf8NoBom = [Text.UTF8Encoding]::new($false)

function Get-EquippedStatSkinResref($utc) {
    $skin = @($utc.Equip_ItemList.value | Where-Object { $_.__struct_id -eq 131072 }) |
        Select-Object -First 1

    if ($null -eq $skin) {
        return ""
    }

    return [string]$skin.EquippedRes.value
}

function Get-NpcHpBudget($skin) {
    $hpProperties = @($skin.PropertiesList.value | Where-Object {
        $null -ne $_.PropertyName -and [int]$_.PropertyName.value -eq 96
    })

    if ($hpProperties.Count -eq 0) {
        return 0
    }

    $budget = 0
    foreach ($property in $hpProperties) {
        $budget += [int]$property.CostValue.value
    }

    return $budget
}

function Get-NativeHitPointAdjustment($utc) {
    $level = 0
    foreach ($classEntry in @($utc.ClassList.value)) {
        $level += [int]$classEntry.ClassLevel.value
    }

    $constitution = [int]$utc.Con.value
    $constitutionModifier = [int][Math]::Floor(($constitution - 10) / 2)
    $adjustment = $constitutionModifier * $level

    $featIds = @($utc.FeatList.value | ForEach-Object { [int]$_.Feat.value })
    if ($featIds -contains 40) {
        # Toughness grants one HP per character level.
        $adjustment += $level
    }

    # Each separately possessed Epic Toughness feat grants 20 HP.
    $adjustment += 20 * @($featIds | Where-Object { $_ -ge 754 -and $_ -le 763 }).Count

    return $adjustment
}

function Set-TypedShortValue([string]$text, [string]$fieldName, [int]$value) {
    $escapedFieldName = [Regex]::Escape($fieldName)
    $pattern = "(?ms)(^\s*`"$escapedFieldName`"\s*:\s*\{\s*`"type`"\s*:\s*`"short`"\s*,\s*`"value`"\s*:\s*)-?\d+"
    $matches = [Regex]::Matches($text, $pattern)

    if ($matches.Count -ne 1) {
        throw "Expected exactly one $fieldName short field, found $($matches.Count)."
    }

    return [Regex]::Replace(
        $text,
        $pattern,
        { param($match) $match.Groups[1].Value + $value },
        1)
}

$audited = 0
$changed = New-Object System.Collections.Generic.List[string]
$failures = New-Object System.Collections.Generic.List[string]

foreach ($utcFile in Get-ChildItem -LiteralPath $utcDirectory -Filter "*.utc.json" | Sort-Object Name) {
    $utcText = [IO.File]::ReadAllText($utcFile.FullName)
    $utc = $utcText | ConvertFrom-Json
    $skinResref = Get-EquippedStatSkinResref $utc
    if ([string]::IsNullOrWhiteSpace($skinResref)) {
        continue
    }

    $skinPath = Join-Path $utiDirectory "$skinResref.uti.json"
    if (-not (Test-Path -LiteralPath $skinPath)) {
        continue
    }

    $skin = [IO.File]::ReadAllText($skinPath) | ConvertFrom-Json
    $finalHp = Get-NpcHpBudget $skin
    if ($finalHp -le 0) {
        continue
    }

    $audited++
    $baseHp = $finalHp - (Get-NativeHitPointAdjustment $utc)
    if ($baseHp -lt 1 -or $baseHp -gt [int16]::MaxValue) {
        $failures.Add("$($utcFile.Name): calculated base HP $baseHp is outside the UTC short range.")
        continue
    }

    $normalizedText = Set-TypedShortValue $utcText "CurrentHitPoints" $finalHp
    $normalizedText = Set-TypedShortValue $normalizedText "HitPoints" $baseHp
    $normalizedText = Set-TypedShortValue $normalizedText "MaxHitPoints" $finalHp

    if ($normalizedText -ne $utcText) {
        $changed.Add($utcFile.Name)
        if (-not $CheckOnly) {
            [IO.File]::WriteAllText($utcFile.FullName, $normalizedText, $utf8NoBom)
        }
    }
}

if ($failures.Count -gt 0) {
    throw "NPC HP normalization failed:`n$($failures -join "`n")"
}

if ($CheckOnly -and $changed.Count -gt 0) {
    throw "$($changed.Count) of $audited NPCHP-backed UTCs are not normalized:`n$($changed -join "`n")"
}

$verb = if ($CheckOnly) { "Verified" } else { "Normalized" }
Write-Host "$verb $audited NPCHP-backed UTCs; $($changed.Count) required changes."
