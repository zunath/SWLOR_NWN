# Ability Definitions Documentation

This document provides detailed information about how ability definitions work in SWLOR.Game.Server and how they use the AbilityBuilder pattern.

## Overview

Ability definitions are located in the `Feature/AbilityDefinition/` directory and are organized by category (Force, OneHanded, TwoHanded, etc.). Each ability definition implements the `IAbilityListDefinition` interface and uses the `AbilityBuilder` to create complex ability configurations.

## Directory Structure

```
Feature/AbilityDefinition/
├── Armor/
├── Beastmaster/
├── Beasts/
├── Devices/
├── FirstAid/
├── Force/
├── General/
├── Leadership/
├── MartialArts/
├── NPC/
├── OneHanded/
├── Ranged/
└── TwoHanded/
```

## How Ability Definitions Work

### 1. Interface Implementation

All ability definitions implement `IAbilityListDefinition`:

```csharp
public interface IAbilityListDefinition
{
    Dictionary<FeatType, AbilityDetail> BuildAbilities();
}
```

### 2. Builder Pattern Usage

Ability definitions use the `AbilityBuilder` to create abilities with a fluent interface:

```csharp
public class ForceLightningAbilityDefinition : IAbilityListDefinition
{
    public Dictionary<FeatType, AbilityDetail> BuildAbilities()
    {
        var builder = new AbilityBuilder();
        ForceLightning1(builder);
        ForceLightning2(builder);
        ForceLightning3(builder);
        ForceLightning4(builder);

        return builder.Build();
    }
}
```

## Ability Types

### 1. Casted Abilities

Casted abilities have a casting time and are typically used for Force powers and spells.

**Example: Force Lightning**

```csharp
private static void ForceLightning1(AbilityBuilder builder)
{
    builder.Create(FeatType.ForceLightning1, PerkType.ForceLightning)
        .Name("Force Lightning I")
        .Level(1)
        .HasRecastDelay(RecastGroup.ForceLightning, 30f)
        .HasActivationDelay(2f)
        .HasMaxRange(30.0f)
        .IsCastedAbility()
        .IsHostileAbility()
        .UsesAnimation(Animation.LoopingConjure1)
        .HasImpactAction(ImpactAction);
}
```

**Key Features:**
- `IsCastedAbility()` - Marks as a casted ability
- `HasActivationDelay(float)` - Sets casting time
- `HasRecastDelay(RecastGroup, float)` - Sets cooldown
- `HasMaxRange(float)` - Sets maximum range
- `UsesAnimation(Animation)` - Sets casting animation

### 2. Weapon Abilities

Weapon abilities trigger on the next weapon hit and are used for combat skills.

**Example: Power Attack**

```csharp
private static void PowerAttack1(AbilityBuilder builder)
{
    builder.Create(FeatType.PowerAttack1, PerkType.PowerAttack)
        .Name("Power Attack I")
        .Level(1)
        .HasRecastDelay(RecastGroup.PowerAttack, 12f)
        .IsWeaponAbility()
        .HasImpactAction(ImpactAction);
}
```

**Key Features:**
- `IsWeaponAbility()` - Marks as a weapon-triggered ability
- No activation delay (triggers on next hit)
- Typically shorter cooldowns

### 3. Concentration Abilities

Concentration abilities stay active and drain resources until turned off.

**Example: Force Shield**

```csharp
private static void ForceShield1(AbilityBuilder builder)
{
    builder.Create(FeatType.ForceShield1, PerkType.ForceShield)
        .Name("Force Shield I")
        .Level(1)
        .IsConcentrationAbility(StatusEffectType.ForceShield)
        .RequirementFP(1)
        .HasImpactAction(ImpactAction);
}
```

**Key Features:**
- `IsConcentrationAbility(StatusEffectType)` - Marks as concentration ability
- Requires corresponding status effect
- Drains resources over time

## Common Ability Builder Methods

### Basic Configuration

```csharp
builder.Create(FeatType.AbilityName, PerkType.AbilityPerk)
    .Name("Ability Name")
    .Level(1)
```

### Timing and Cooldowns

```csharp
.HasRecastDelay(RecastGroup.GroupName, 30f)  // Cooldown in seconds
.HasActivationDelay(2f)                       // Casting time in seconds
```

### Targeting and Range

```csharp
.HasMaxRange(30.0f)                           // Maximum range
.IsHostileAbility()                           // Targets enemies
.CanBeUsedInSpace()                           // Usable in space combat
```

### Visual and Audio

```csharp
.UsesAnimation(Animation.LoopingConjure1)     // Casting animation
.DisplaysVisualEffectWhenActivating(Vfx)      // Visual effect
.HideActivationMessage()                      // Hide activation message
```

### Resource Costs

```csharp
.RequirementFP(5)                             // Force Point cost
.RequirementStamina(10)                       // Stamina cost
```

### Custom Logic

```csharp
.HasImpactAction(ImpactAction)                // Main ability effect
.HasActivationAction(ActivationAction)        // Pre-delay logic
.HasCustomValidation(ValidationAction)        // Custom validation
```

## Impact Actions

Impact actions are the core logic of abilities. They define what happens when the ability is used.

### Example: Force Lightning Impact Action

```csharp
private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
{
    var dmg = 0;
    var willBonus = GetAbilityScore(activator, AbilityType.Willpower);

    switch (level)
    {
        case 1:
            dmg = willBonus;
            break;
        case 2:
            dmg = 10 + (willBonus * 3 / 2);
            break;
        case 3:
            dmg = 20 + (willBonus * 2);
            break;
        case 4:
            dmg = 30 + (willBonus * 3);
            break;
    }

    dmg += Combat.GetAbilityDamageBonus(activator, SkillType.Force);
    
    // Apply damage to target
    var damage = Combat.CalculateDamage(attack, dmg, attackerStat, defense, defenderStat, 0);
    ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Electrical), target);
    
    // Add combat points and enmity
    CombatPoint.AddCombatPoint(activator, target, SkillType.Force, 3);
    Enmity.ModifyEnmity(activator, target, 100 * level + damage);
}
```

## Ability Categories

### Force Abilities

Located in `Feature/AbilityDefinition/Force/`

**Examples:**
- Force Lightning
- Force Push
- Force Shield
- Force Speed

**Common Patterns:**
- Use Willpower for damage calculations
- Require Force Points
- Often have electrical damage type
- Use Force skill for bonuses

### Combat Abilities

Located in `Feature/AbilityDefinition/OneHanded/`, `TwoHanded/`, `Ranged/`

**Examples:**
- Power Attack
- Cleave
- Rapid Shot
- Called Shot

**Common Patterns:**
- Use weapon damage
- Require specific weapon types
- Often have physical damage type
- Use combat skills for bonuses

### Support Abilities

Located in `Feature/AbilityDefinition/FirstAid/`, `Leadership/`

**Examples:**
- Heal
- Inspire
- Rally
- First Aid

**Common Patterns:**
- Target allies
- Provide buffs or healing
- Use social skills
- Often have longer cooldowns

## Best Practices

### 1. Consistent Naming

```csharp
// Good
builder.Create(FeatType.ForceLightning1, PerkType.ForceLightning)
    .Name("Force Lightning I")

// Bad
builder.Create(FeatType.ForceLightning1, PerkType.ForceLightning)
    .Name("Lightning Bolt I")
```

### 2. Level-Specific Configuration

```csharp
private static void ConfigureAbility(AbilityBuilder builder, int level)
{
    var damage = level * 10;
    var cooldown = 30f - (level * 5f);
    
    builder.Create(GetFeatType(level), PerkType.AbilityName)
        .Name($"Ability Name {GetRomanNumeral(level)}")
        .Level(level)
        .HasRecastDelay(RecastGroup.AbilityName, cooldown)
        .HasImpactAction((activator, target, lvl, location) => {
            // Use damage variable in impact action
        });
}
```

### 3. Reusable Impact Actions

```csharp
private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
{
    // Common logic for all levels
    var baseDamage = CalculateBaseDamage(level);
    var finalDamage = ApplyBonuses(activator, baseDamage);
    
    // Apply damage
    ApplyDamage(activator, target, finalDamage);
    
    // Common effects
    AddCombatPoints(activator, target);
    ModifyEnmity(activator, target, finalDamage);
}
```

### 4. Proper Resource Management

```csharp
// Check resources before applying effects
if (Stat.GetCurrentFP(activator) < requiredFP)
{
    // Handle insufficient resources
    var darkBargain = CalculateDarkBargain(requiredFP, Stat.GetCurrentFP(activator));
    Stat.ReduceFP(activator, Stat.GetCurrentFP(activator));
    ApplyEffectToObject(DurationType.Instant, EffectDamage(darkBargain), activator);
}
else
{
    Stat.ReduceFP(activator, requiredFP);
}
```

## Common Patterns

### 1. Multi-Level Abilities

```csharp
public Dictionary<FeatType, AbilityDetail> BuildAbilities()
{
    var builder = new AbilityBuilder();
    
    // Create multiple levels
    for (int i = 1; i <= 4; i++)
    {
        CreateAbilityLevel(builder, i);
    }
    
    return builder.Build();
}

private static void CreateAbilityLevel(AbilityBuilder builder, int level)
{
    builder.Create(GetFeatType(level), PerkType.AbilityName)
        .Name($"Ability Name {GetRomanNumeral(level)}")
        .Level(level)
        .HasRecastDelay(RecastGroup.AbilityName, GetCooldown(level))
        .HasImpactAction(ImpactAction);
}
```

### 2. Conditional Abilities

```csharp
private static void CreateAbility(AbilityBuilder builder, bool isActive)
{
    if (!isActive) return;
    
    builder.Create(FeatType.AbilityName, PerkType.AbilityName)
        .Name("Ability Name")
        .Level(1)
        .HasImpactAction(ImpactAction);
}
```

### 3. Shared Logic

```csharp
private static void SharedImpactAction(uint activator, uint target, int level, Location targetLocation)
{
    // Common setup
    var damage = CalculateDamage(activator, level);
    
    // Apply effects
    ApplyDamage(activator, target, damage);
    AddCombatPoints(activator, target);
    ModifyEnmity(activator, target, damage);
}
```

## Testing Abilities

When creating new abilities, consider:

1. **Resource Costs** - Ensure proper FP/STM costs
2. **Cooldowns** - Balance cooldown times
3. **Range** - Set appropriate maximum ranges
4. **Targeting** - Verify correct target types
5. **Damage Scaling** - Test damage at different levels
6. **Visual Effects** - Ensure proper animations and VFX
7. **Sound Effects** - Add appropriate sound files

## Integration with Other Systems

### Perks

Abilities are linked to perks through the `PerkType` parameter:

```csharp
builder.Create(FeatType.AbilityName, PerkType.AbilityPerk)
```

### Status Effects

Concentration abilities require corresponding status effects:

```csharp
.IsConcentrationAbility(StatusEffectType.AbilityStatus)
```

### Combat System

Abilities integrate with the combat system for damage calculation and combat points.

### Skill System

Abilities can use skills for damage bonuses and other calculations.

This documentation provides a comprehensive guide to creating and configuring abilities in SWLOR.Game.Server using the builder pattern. 