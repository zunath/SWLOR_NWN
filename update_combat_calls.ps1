# PowerShell script to update CombatService.CalculateDamage calls to use ICombatCalculationService methods

# Files that need to use CalculateForceDamage (Force abilities)
$forceFiles = @(
    # All Force abilities already updated
)

# Files that need to use CalculateAbilityDamage (regular abilities)
$abilityFiles = @(
    "SWLOR.Component.Ability\Definitions\Beasts\SpinningClawAbilityDefinition.cs",
    "SWLOR.Component.Ability\Definitions\Beasts\ShockingSlashAbilityDefinition.cs",
    "SWLOR.Component.Ability\Definitions\Beasts\PoisonBreathAbilityDefinition.cs",
    "SWLOR.Component.Ability\Definitions\Beasts\IceBreathAbilityDefinition.cs",
    "SWLOR.Component.Ability\Definitions\Beasts\ClipAbilityDefinition.cs",
    "SWLOR.Component.Ability\Definitions\Beasts\FlameBreathAbilityDefinition.cs"
)

# Pattern to match the old CombatService injection
$oldServicePattern = 'private ICombatService CombatService => _serviceProvider\.GetRequiredService<ICombatService>\(\);'
$newServicePattern = 'private ICombatCalculationService CombatCalculationService => _serviceProvider.GetRequiredService<ICombatCalculationService>();'

# Pattern to match the old CalculateDamage calls
$oldDamagePattern = 'CombatService\.CalculateDamage\(\s*([^,]+),\s*([^,]+),\s*([^,]+),\s*([^,]+),\s*([^,]+),\s*([^)]+)\)'

# Function to determine which method to use based on file content
function Get-DamageMethod {
    param([string]$filePath)

    $content = Get-Content $filePath -Raw

    # Check if it's a force ability (uses CalculateForceDefense or Willpower)
    if ($content -match "CalculateForceDefense" -or ($content -match "AbilityType\.Willpower" -and $content -match "CalculateAttack.*Willpower")) {
        return "CalculateForceDamage"
    }
    else {
        return "CalculateAbilityDamage"
    }
}

# Function to determine ability parameters based on file content
function Get-AbilityParams {
    param([string]$filePath)

    $content = Get-Content $filePath -Raw

    # Extract attacker stat type
    if ($content -match "CalculateAttack\(activator, (AbilityType\.\w+),") {
        $attackerStat = $matches[1]
    } else {
        $attackerStat = "AbilityType.Might"  # default
    }

    # Extract defender stat type (usually Vitality for physical)
    if ($content -match "GetAbilityScore\(target, (AbilityType\.\w+)\)") {
        $defenderStat = $matches[1]
    } else {
        $defenderStat = "AbilityType.Vitality"  # default
    }

    # Determine damage type (usually Physical)
    $damageType = "CombatDamageType.Physical"

    # Skill type (usually Invalid for beasts)
    $skillType = "SkillType.Invalid"

    return @{
        AttackerStat = $attackerStat
        DefenderStat = $defenderStat
        DamageType = $damageType
        SkillType = $skillType
    }
}

# Update service injections
foreach ($file in ($forceFiles + $abilityFiles)) {
    if (Test-Path $file) {
        Write-Host "Updating service injection in $file"
        (Get-Content $file) -replace $oldServicePattern, $newServicePattern | Set-Content $file
    }
}

# Add CombatDamageType import where needed
$importPattern = 'using SWLOR\.Shared\.Domain\.Combat\.Contracts;'
$importReplacement = 'using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Combat.Enums;'

foreach ($file in $abilityFiles) {
    if (Test-Path $file) {
        $content = Get-Content $file -Raw
        if ($content -match $importPattern -and -not ($content -match 'using SWLOR\.Shared\.Domain\.Combat\.Enums;')) {
            Write-Host "Adding CombatDamageType import to $file"
            $content -replace $importPattern, $importReplacement | Set-Content $file
        }
    }
}

# Update CalculateDamage calls
foreach ($file in ($forceFiles + $abilityFiles)) {
    if (Test-Path $file) {
        $method = Get-DamageMethod $file
        Write-Host "Updating $file to use $method"

        if ($method -eq "CalculateForceDamage") {
            # Replace with CalculateForceDamage(attacker, defender, dmg)
            $newCall = 'CombatCalculationService.CalculateForceDamage(activator, target, dmg)'
            (Get-Content $file) -replace $oldDamagePattern, $newCall | Set-Content $file
        }
        elseif ($method -eq "CalculateAbilityDamage") {
            $params = Get-AbilityParams $file
            $newCall = "CombatCalculationService.CalculateAbilityDamage(activator, target, dmg, $($params.DamageType), $($params.SkillType), $($params.AttackerStat), $($params.DefenderStat))"
            (Get-Content $file) -replace $oldDamagePattern, $newCall | Set-Content $file
        }
    }
}

Write-Host "Update complete!"
