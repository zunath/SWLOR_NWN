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
**Formula:** `BaseFP + (Willpower Stat × 3) + Food Bonus`

**Components:**
- Base FP: 10
- Willpower Stat: Raw Willpower stat value
- Food Bonus: Temporary bonus from food effects

**Example:**
- Willpower 14
- Max FP = 10 + (14 × 3) + 0 = 52

### 4. Max Stamina Calculation
**Formula:** `BaseSTM + floor(Might Stat × 1.5) + Food Bonus`

**Components:**
- Base Stamina: 10
- Might Stat: Raw Might stat value
- Food Bonus: Temporary bonus from food effects

**Example:**
- Might 16
- Max Stamina = 10 + floor(16 × 1.5) + 0 = 34

## Combat Stat Calculations

### 5. Attack Calculation
**Formula:** `8 + (2 × Skill Level) + Stat + Equipment Bonus`

**Components:**
- Base Attack: 8
- Skill Level: Highest combat skill (explicit weapon skills, Force)
- Stat: Highest raw combat stat (Might, Perception, Willpower)
- Equipment Bonus: Stored in `Attack` or `ForceAttack` property

**Example:**
- Skill Level 5, Stat 14, Equipment +3
- Attack = 8 + (2 × 5) + 14 + 3 = 35

### 6. Defense Calculation
**Formula:** `8 + floor(Armor Skill × 1.2) + Defense Stat + Equipment Bonus`

**Components:**
- Base Defense: 8
- Defense Stat: Raw Vitality for Physical Defense, raw Willpower for Force Defense
- Armor Skill: Armor skill rank, weighted at 1.2
- Equipment Bonus: Matching Physical or Force defense bonuses from equipment

**Example:**
- Vitality or Willpower 16, Armor Skill 3, matching equipment +5
- Defense = 8 + floor(3 × 1.2) + 16 + 5 = 8 + 3 + 16 + 5 = 32

### 7. Evasion Calculation
**Formula:** `8 + (2 × Armor Skill) + Agility Stat + Equipment Bonus`

**Components:**
- Base Evasion: 8
- Agility Stat: Raw Agility stat value (not modifier)
- Armor Skill: Armor skill rank, weighted twice as strongly as Agility
- Equipment Bonus: Stored in `Evasion` property

**Example:**
- Agility 14, Armor Skill 2, Equipment +4
- Evasion = 8 + (2 × 2) + 14 + 4 = 30

### 7a. Resistance Calculation
**Formula:** Resistance score is capped at 100 and uses the Xenomech reduction curve: `1 - (Resistance / (Resistance + 50))`, with a 10% minimum damage multiplier.

**Components:**
- Equipment Bonus: Matching elemental/status resistance bonuses from equipment
- Status/Perk/Food Bonus: Existing elemental defense stat bonuses now contribute to the matching elemental resistance
- Resistance Types: Fire, Poison, Electrical, Ice, Mind, Mobility, Trauma, Disruption

### 8. Accuracy Calculation
**Formula:** `8 + (2 × Skill Level) + Stat + Equipment Bonus`

**Components:**
- Base Accuracy: 8
- Skill Level: Relevant weapon skill rank, weighted twice as strongly as the stat
- Stat: Relevant ability stat
- Equipment Bonus: Equipment accuracy bonus

### 8a. Critical Hit Rate Calculation
**Formula:** `5 + floor(Weapon Skill Rank / 10) + clamp(floor((Perception - Target Vitality) / 5), 0, 3) + Critical Bonus`

**Components:**
- Base Critical Rate: 5%
- Weapon Skill Rank: Grants +1% per 10 ranks
- Perception vs Target Vitality: Grants up to +3% baseline critical chance
- Critical Bonus: Perks, status effects, and situational modifiers
- Final Result: Clamped between 5% and 50%

### 8b. Damage Stat Delta Calculation
**Formula:** `Base Damage + ((Attacker Stat - Defender Stat) × 0.35)`

The attacking stat and defending stat are both raw stat values. Each point matters, but the stat comparison is weighted so Vitality and Willpower do not act as full flat damage shields.

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
**Formula:** `Max(explicit weapon skills, Force)`

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
- **Defense Bonuses:** Physical/Force mitigation stored in `Defenses`
- **Resistance Bonuses:** Elemental/status mitigation stored in `Resistances` by resistance type
- **Evasion Bonus:** Stored in `Evasion` property

## Implementation Notes

1. **HP Calculation:** Uses native NWN function, not calculated in C#
2. **Food Effects:** Temporary bonuses that don't persist
3. **Equipment Bonuses:** Stored separately and added to calculations
4. **Skill Levels:** Based on skill ranks, not character level
5. **Stat Modifiers:** Still used for saving throws and native NWN systems, but custom combat resources and defense formulas use raw stat values unless noted otherwise.

## Usage in Admin Interface

The stat calculation service (`StatCalculationService.cs`) provides methods to calculate all these values for display in the admin interface. The calculations are performed client-side for immediate feedback and verification of stored values.

## Verification

The admin interface shows both calculated and stored values to help identify discrepancies between what should be calculated and what is actually stored in the database. 
