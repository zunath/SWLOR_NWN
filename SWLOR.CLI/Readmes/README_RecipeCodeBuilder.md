# RecipeCodeBuilder

## Overview
The RecipeCodeBuilder generates C# code for crafting recipes used in the SWLOR game's crafting system. It processes TSV data to create structured recipe definitions with components, enhancements, and requirements.

## Command
```bash
SWLOR.CLI.exe -r
# or
SWLOR.CLI.exe --recipe
```

## Functionality
The RecipeCodeBuilder processes recipe data from `./InputFiles/recipes.tsv` and generates:

### 1. Recipe Structure
Each recipe includes:
- **Skill Requirement**: Crafting skill needed
- **Recipe Name**: Descriptive name of the recipe
- **Category**: Recipe category (e.g., Smithery, Engineering)
- **Level**: Required skill level
- **Perk Level**: Required perk level
- **Quantity**: Amount produced per craft
- **Enhancement Slots**: Number of enhancement slots available

### 2. Component System
Supports up to 8 components per recipe:
- **Component ResRef**: Resource reference of the component
- **Component Quantity**: Amount of component required
- **Dynamic Addition**: Only includes components that have valid data

### 3. Enhancement System
- **Enhancement Type**: Category of enhancement (e.g., Armor, Weapon)
- **Enhancement Slots**: Number of slots available for enhancements
- **Conditional Addition**: Only includes enhancements when valid data is present

### 4. Recipe Requirements
- **Unlocked Requirement**: Optional requirement for recipe unlocking
- **Skill-Based**: Tied to specific crafting skills

## Input File Format
The tool expects a TSV file with the following columns:
- Column 0: Skill requirement
- Column 1: Requires recipe flag
- Column 2: Category enum name
- Column 3: Recipe enum name
- Column 4: Recipe category
- Column 5: Perk level
- Column 6: Recipe name
- Column 7: Level requirement
- Column 8: Quantity produced
- Column 9: ResRef
- Column 10: Enhancement category
- Column 11: Enhancement slots
- Columns 12-27: Component data (resref and quantity pairs)

## Output Structure
```
./OutputRecipes/
└── Recipes.txt
```

## Generated Code Features

### Template-Based Generation
Uses `./Templates/RecipeTemplate.txt` as the base template with placeholders:
- `%%RECIPESKILL%%` → Crafting skill requirement
- `%%RECIPENAME%%` → Recipe name
- `%%CATEGORYENUMNAME%%` → Category enum name
- `%%RECIPEENUMNAME%%` → Recipe enum name
- `%%RESREF%%` → Resource reference
- `%%LEVEL%%` → Level requirement
- `%%PERKLEVEL%%` → Perk level requirement
- `%%RECIPECATEGORY%%` → Recipe category
- `%%QUANTITY%%` → Quantity produced

### Component Template
```csharp
.Component("%%COMPONENTRESREF%%", %%COMPONENTQUANTITY%%)
```

### Enhancement Template
```csharp
.EnhancementSlots(RecipeEnhancementType.%%ENHANCEMENTTYPE%%, %%ENHANCEMENTSLOTS%%)
```

### Tier Organization
Recipes are organized by perk level (tier) with separate functions:
```csharp
private void Tier1() { /* Tier 1 recipes */ }
private void Tier2() { /* Tier 2 recipes */ }
// ... etc
```

## Usage
This tool is used during development to:
- Generate recipe code from spreadsheet data
- Maintain consistency between game data and recipe definitions
- Automate the creation of crafting recipes
- Update recipe components and requirements

## Notes
- Skips empty lines in the input file
- Only includes components with valid resref and quantity data
- Only includes enhancements when category and slots are valid
- Organizes recipes by tier for better code organization
- Clears the output directory before generating new files
- Generates C# code compatible with the SWLOR crafting system 