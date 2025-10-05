# SWLOR Stat Calculation Formulas

This document outlines all the stat calculation formulas used in SWLOR, based on the analysis of the Game.Server Stat.cs file.

## Base Values

```csharp
public const int BaseHP = 70;
public const int BaseFP = 10;
public const int BaseSTM = 10;
```

## Core Stat Calculations

### 1. Ability Modifier Calculation
**Formula:** `(Stat - 10) / 2`

This is the standard D&D-style ability modifier calculation. For example:
- Stat 8-9: Modifier -1
- Stat 10-11: Modifier 0
- Stat 12-13: Modifier +1
- Stat 14-15: Modifier +2
- And so on...

### 2. Max HP Calculation
**Formula:** Uses native NWN `GetMaxHitPoints()` function

The HP calculation is handled by the NWN engine and stored in the `MaxHP` property. The base value is 70, but the actual calculation involves level, class, and other factors.

### 3. Max FP (Force Points) Calculation
**Formula:** `BaseFP + (Willpower Modifier × 10) + Food Bonus`

**Components:**
- Base FP: 10
- Willpower Modifier: Calculated from Willpower stat
- Food Bonus: Temporary bonus from food effects

**Example:**
- Willpower 14 = Modifier +2
- Max FP = 10 + (2 × 10) + 0 = 30

### 4. Max Stamina Calculation
**Formula:** `BaseSTM + (Agility Modifier × 5) + Food Bonus`

**Components:**
- Base Stamina: 10
- Agility Modifier: Calculated from Agility stat
- Food Bonus: Temporary bonus from food effects

**Example:**
- Agility 16 = Modifier +3
- Max Stamina = 10 + (3 × 5) + 0 = 25

## Combat Stat Calculations

### 5. Attack Calculation
**Formula:** `8 + (2 × Skill Level) + Stat + Equipment Bonus`

**Components:**
- Base Attack: 8
- Skill Level: Highest combat skill (OneHanded, TwoHanded, Ranged, Force)
- Stat: Highest combat stat (Might, Perception, Willpower)
- Equipment Bonus: Stored in `Attack` or `ForceAttack` property

**Example:**
- Skill Level 5, Stat 14 (+2), Equipment +3
- Attack = 8 + (2 × 5) + 2 + 3 = 23

### 6. Defense Calculation
**Formula:** `8 + (Vitality Stat × 1.5) + Armor Skill + Equipment Bonus`

**Components:**
- Base Defense: 8
- Vitality Stat: Raw Vitality stat value (not modifier)
- Armor Skill: Armor skill level
- Equipment Bonus: Sum of all defense bonuses from equipment

**Example:**
- Vitality 16, Armor Skill 3, Equipment +5
- Defense = 8 + (16 × 1.5) + 3 + 5 = 8 + 24 + 3 + 5 = 40

### 7. Evasion Calculation
**Formula:** `(Agility Stat × 3) + Armor Skill + Equipment Bonus`

**Components:**
- Agility Stat: Raw Agility stat value (not modifier)
- Armor Skill: Armor skill level
- Equipment Bonus: Stored in `Evasion` property

**Example:**
- Agility 14, Armor Skill 2, Equipment +4
- Evasion = (14 × 3) + 2 + 4 = 42 + 2 + 4 = 48

### 8. Accuracy Calculation
**Formula:** `8 + (2 × Skill Level) + Stat + Equipment Bonus`

**Components:**
- Base Accuracy: 8
- Skill Level: Relevant skill level
- Stat: Relevant ability stat
- Equipment Bonus: Equipment accuracy bonus

## Saving Throw Calculations

### 9. Base Saving Throw Calculation
**Formula:** `8 + (Stat Modifier × 2) + Level`

**Components:**
- Base Save: 8
- Stat Modifier: Ability modifier for the relevant stat
- Level: Character level

**Saving Throw Types:**
- **Fortitude:** Based on Vitality modifier
- **Reflex:** Based on Agility modifier
- **Will:** Based on Willpower modifier

**Example:**
- Vitality 14 (+2), Level 5
- Fortitude = 8 + (2 × 2) + 5 = 8 + 4 + 5 = 17

## Additional Calculations

### 10. Character Level Estimation
**Formula:** `Math.Max(1, TotalSkillPoints / 10)`

This is a rough estimation based on total skill points, as the actual level calculation is more complex.

### 11. Highest Combat Skill Level
**Formula:** `Max(OneHanded, TwoHanded, Ranged, Force)`

Returns the highest level among all combat skills.

### 12. Highest Combat Stat
**Formula:** `Max(Might, Perception, Willpower)`

Returns the highest stat among combat-relevant abilities.

## Food Effects

Food effects provide temporary bonuses to HP, FP, and Stamina. These are applied as additional bonuses to the base calculations.

**Example Food Effect:**
- HP: +20 temporary
- FP: +5 temporary
- Stamina: +3 temporary

## Equipment Bonuses

Equipment provides various bonuses that are stored in the player's properties:

- **Attack Bonus:** Stored in `Attack` property
- **Force Attack Bonus:** Stored in `ForceAttack` property
- **Defense Bonuses:** Stored in `Defenses` dictionary by damage type
- **Evasion Bonus:** Stored in `Evasion` property

## Implementation Notes

1. **HP Calculation:** Uses native NWN function, not calculated in C#
2. **Food Effects:** Temporary bonuses that don't persist
3. **Equipment Bonuses:** Stored separately and added to calculations
4. **Skill Levels:** Based on skill ranks, not character level
5. **Stat Modifiers:** Always calculated as (stat - 10) / 2

## Usage in Admin Interface

The stat calculation service (`StatCalculationService.cs`) provides methods to calculate all these values for display in the admin interface. The calculations are performed client-side for immediate feedback and verification of stored values.

## Verification

The admin interface shows both calculated and stored values to help identify discrepancies between what should be calculated and what is actually stored in the database. 