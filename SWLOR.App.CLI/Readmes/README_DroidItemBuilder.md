# DroidItemBuilder

## Overview
The DroidItemBuilder generates JSON files for droid items (CPUs and parts) used in the SWLOR game's droid system. It processes TSV data to create structured item definitions with statistics and properties.

## Command
```bash
SWLOR.CLI.exe -d
# or
SWLOR.CLI.exe --droid
```

## Functionality
The DroidItemBuilder processes droid data from `./InputFiles/droid_item_list.tsv` and generates:

### 1. Droid CPU Items
Creates CPU items with the following properties:
- **Name and ResRef**: Item identification
- **Slot Type**: CPU slot (type 1)
- **Tier**: Quality level of the CPU
- **AI Slots**: Number of AI slots available
- **Statistics**: HP, STM, and attribute bonuses
- **Skill Bonuses**: Combat skill improvements (One-Handed, Two-Handed, Martial Arts, Ranged)

### 2. Droid Part Items
Creates part items with the following properties:
- **Name and ResRef**: Item identification
- **Slot Type**: Part slot (Head=2, Body=3, Arms=4, Legs=5)
- **Tier**: Quality level of the part
- **AI Slots**: Number of AI slots (if applicable)
- **Attribute Bonuses**: MGT, PER, VIT, WIL, AGI, SOC improvements
- **Icon Assignment**: Automatic icon assignment based on part type

## Input File Format
The tool expects a TSV file with the following columns:
- Column 0: Item name
- Column 1: ResRef
- Column 2: Part type (CPU, Head, Body, Arms, Legs)
- Column 3: Tier
- Column 4: AI slots
- Column 6: HP bonus
- Column 7: STM bonus
- Column 8-13: Attribute bonuses (MGT, PER, VIT, WIL, AGI, SOC)
- Column 14-17: Skill bonuses (One-Handed, Two-Handed, Martial Arts, Ranged)

## Output Structure
```
./OutputDroidItems/
├── [resref].uti.json
├── [resref].uti.json
└── ...
```

## Templates Used
- `droid_cpu_template.json`: Template for CPU items
- `droid_part_template.json`: Template for part items

## Generated Properties

### CPU Items
- **Item Properties**: Skill bonuses for combat abilities
- **Statistics**: HP, STM, and attribute bonuses
- **AI Slots**: Number of available AI slots
- **Tier**: Quality level affecting item performance

### Part Items
- **Item Properties**: Attribute bonuses and AI slots
- **Statistics**: Primary and secondary attribute improvements
- **Icon Assignment**: Automatic icon selection based on part type
- **Tier**: Quality level affecting item performance

## Slot Type Mapping
- **CPU**: Type 1, Icon -1 (no specific icon)
- **Head**: Type 2, Icon 110
- **Body**: Type 3, Icon 120
- **Arms**: Type 4, Icon 37
- **Legs**: Type 5, Icon 90

## Usage
This tool is used during development to:
- Generate droid item definitions from spreadsheet data
- Maintain consistency between game data and item definitions
- Automate the creation of droid-related items
- Update droid item statistics and properties

## Notes
- Uses random icon selection from a predefined array for visual variety
- Automatically calculates skill bonuses based on input data
- Handles both CPU and part items with different templates
- Generates JSON files compatible with the NWN module system 