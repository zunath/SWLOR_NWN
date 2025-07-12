# EnhancementItemBuilder

## Overview
The EnhancementItemBuilder generates JSON files for enhancement items used in the SWLOR game's crafting and enhancement system. It processes CSV data to create structured item definitions with various enhancement properties.

## Command
```bash
SWLOR.CLI.exe -e
# or
SWLOR.CLI.exe --enhancement
```

## Functionality
The EnhancementItemBuilder processes enhancement data from `./InputFiles/enhancement_list.csv` and generates:

### 1. Enhancement Categories
Supports the following enhancement categories:
- **Armor** (ID: 101): Armor-related enhancements
- **Weapon** (ID: 102): Weapon-related enhancements
- **Structure** (ID: 107): Structure-related enhancements
- **Cooking** (ID: 108): Cooking-related enhancements
- **Starship** (ID: 109): Starship-related enhancements
- **Module** (ID: 110): Module-related enhancements

### 2. Enhancement Types
Supports various enhancement subtypes including:
- **Control Enhancements**: Smithery, Engineering, Fabrication, Agriculture
- **Craftsmanship Enhancements**: Smithery, Engineering, Fabrication, Agriculture
- **Defense Enhancements**: Physical, Force, Poison, Fire, Ice, Electrical
- **Combat Enhancements**: Evasion, Accuracy, Damage types
- **Resource Enhancements**: HP, STM, FP bonuses and regeneration
- **Special Enhancements**: Duration, XP bonus, Structure bonus
- **Starship Enhancements**: Shield, Capacitor, Damage types, Defense types

## Input File Format
The tool expects a CSV file with the following columns:
- Column 0: Category
- Column 1: Name
- Column 2: ResRef
- Column 3: Level
- Column 5: Progress penalty
- Column 6: Property name
- Column 7: Bonus amount

## Output Structure
```
./OutputEnhancements/
├── [resref].uti.json
├── [resref].uti.json
└── ...
```

## Generated Properties

### Item Properties
- **Name and ResRef**: Item identification
- **Icon ID**: Randomly selected from predefined array
- **Enhancement Level**: Level requirement for the enhancement
- **Progress Penalty**: Crafting difficulty modifier
- **Item Property ID**: Category-based property identifier
- **Subtype ID**: Specific enhancement type identifier
- **Bonus Amount**: Numerical value of the enhancement
- **Price**: Calculated based on tier (Base Price × Tier)

### Tier Calculation
- **Tier 1**: Level 5 (Price: 250)
- **Tier 2**: Level 15 (Price: 500)
- **Tier 3**: Level 25 (Price: 750)
- **Tier 4**: Level 35 (Price: 1000)
- **Tier 5**: Level 45 (Price: 1250)

## Enhancement Type Mapping
The tool maps enhancement names to specific subtype IDs:
- Control and Craftsmanship enhancements: 14-15, 39-44
- Defense enhancements: 1-7
- Combat enhancements: 18-27
- Resource enhancements: 8-10, 29-38
- Starship enhancements: 46-58
- Special enhancements: 28, 33-35, 45, 83-84

## Usage
This tool is used during development to:
- Generate enhancement item definitions from spreadsheet data
- Maintain consistency between game data and enhancement definitions
- Automate the creation of enhancement-related items
- Update enhancement statistics and properties

## Notes
- Uses random icon selection from a predefined array for visual variety
- Automatically calculates prices based on enhancement tier
- Handles multiple enhancement categories and types
- Generates JSON files compatible with the NWN module system
- Clears the output directory before generating new files 