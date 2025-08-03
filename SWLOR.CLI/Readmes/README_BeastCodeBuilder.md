# BeastCodeBuilder

## Overview
The BeastCodeBuilder generates C# code files for beast definitions used in the SWLOR game's beast mastery system. It processes TSV data to create structured beast definition classes with level-based statistics.

## Command
```bash
SWLOR.CLI.exe -b
# or
SWLOR.CLI.exe --beast
```

## Functionality
The BeastCodeBuilder processes beast data from `./InputFiles/beast_levels.tsv` and generates:

1. **Beast Definition Classes**: Creates C# classes for each beast type with properties like:
   - Name and appearance settings
   - Sound set and portrait IDs
   - Accuracy and damage statistics
   - Role and scaling information
   - Mutation templates

2. **Level-Based Statistics**: Generates level-specific data including:
   - HP, STM, FP values
   - Attribute scores (MGT, PER, VIT, WIL, AGI, SOC)
   - Combat bonuses (attack, accuracy, evasion)
   - Defense values (physical, force, fire, poison, electrical, ice)
   - Saving throw bonuses (will, fortitude, reflex)

3. **Output Organization**: Separates beasts into two categories:
   - **TamableBeastDefinition**: Regular beasts that can be tamed
   - **IncubationBeastDefinition**: Beasts that can be incubated

## Input File Format
The tool expects a TSV file with the following key columns:
- Column 0: Beast name
- Column 1: Role
- Column 2: Level
- Columns 3-5: HP, STM, FP values
- Columns 6-11: Attribute scores
- Column 12-13: Accuracy and damage stats
- Column 26: Beast type enum
- Column 27-30: Appearance, portrait, sound set, scaling
- Column 31: Incubation flag
- Columns 32-49: Mutation data

## Output Structure
```
./OutputBeasts/
├── TamableBeastDefinition/
│   ├── [BeastType]BeastDefinition.cs
│   └── ...
└── IncubationBeastDefinition/
    ├── [BeastType]BeastDefinition.cs
    └── ...
```

## Generated Code Features
- **Template-Based Generation**: Uses templates from `./Templates/` directory
- **Enum Integration**: Maps to `BeastType` and `AbilityType` enums
- **Level Functions**: Creates individual functions for each level
- **Mutation Support**: Handles complex mutation data structures
- **Day-of-Week Mapping**: Converts day codes to `DayOfWeek` enum values

## Templates Used
- `beast_builder_template.txt`: Main beast definition template
- `beast_level_template.txt`: Level-specific statistics template

## Usage
This tool is used during development to:
- Generate beast definition code from spreadsheet data
- Maintain consistency between game data and code
- Automate the creation of beast-related C# classes
- Update beast statistics across all levels

## Notes
- Skips the first 4 lines of the input file (header information)
- Requires proper enum definitions in the SWLOR.Game.Server project
- Generated code follows the existing beast mastery system architecture 