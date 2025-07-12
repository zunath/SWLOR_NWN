# StructureItemCreator

## Overview
The StructureItemCreator generates JSON files for structure items used in the SWLOR game's property system. It creates item definitions for all structure types defined in the `StructureType` enum.

## Command
```bash
SWLOR.CLI.exe -s
# or
SWLOR.CLI.exe --structure
```

## Functionality
The StructureItemCreator processes structure types from the `StructureType` enum and generates:

### 1. Enum-Based Generation
- **Automatic Processing**: Iterates through all values in the `StructureType` enum
- **Attribute Extraction**: Uses `StructureAttribute` to get structure details
- **ID Generation**: Creates padded 4-digit IDs for each structure type

### 2. Structure Properties
Each generated structure item includes:
- **Name**: Structure name from the `StructureAttribute`
- **Tag**: Standardized tag format (`structure_[ID]`)
- **ResRef**: Same as tag for consistency
- **ID Format**: Zero-padded 4-digit identifier

## Input Source
The tool uses the `StructureType` enum from the SWLOR.Game.Server project:
```csharp
public enum StructureType
{
    [Structure("Structure Name")]
    StructureName = 0,
    // ... additional structure types
}
```

## Output Structure
```
./structure_output/
├── structure_0000.uti.json
├── structure_0001.uti.json
├── structure_0002.uti.json
└── ...
```

## Template Usage
The tool uses `./Templates/structure_0000.uti.json` as the base template and replaces:
- `%%NAME%%` → Structure name from attribute
- `%%TAG%%` → Generated tag (`structure_[ID]`)
- `%%RESREF%%` → Same as tag

## Generated Properties
Each structure item includes:
- **Name**: Descriptive name from the structure attribute
- **Tag**: Unique identifier in format `structure_[ID]`
- **ResRef**: Resource reference matching the tag
- **ID**: Zero-padded 4-digit identifier based on enum value

## Usage
This tool is used during development to:
- Generate structure item definitions from enum values
- Maintain consistency between code and item definitions
- Automate the creation of structure-related items
- Ensure all structure types have corresponding item definitions

## Dependencies
- **StructureType Enum**: Must be defined in SWLOR.Game.Server
- **StructureAttribute**: Must be applied to enum values
- **Template File**: Requires `structure_0000.uti.json` template

## Notes
- Clears the output directory before generating new files
- Uses zero-padded 4-digit IDs for consistent naming
- Requires proper attribute definitions on enum values
- Generates JSON files compatible with the NWN module system
- Automatically processes all enum values without manual configuration 