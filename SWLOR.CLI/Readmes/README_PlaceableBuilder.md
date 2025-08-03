# PlaceableBuilder

## Overview
The PlaceableBuilder generates JSON files for placeable objects used in the SWLOR game. It processes 2DA data to create structured placeable definitions with appropriate categories and properties.

## Command
```bash
SWLOR.CLI.exe -c
# or
SWLOR.CLI.exe --placeable
```

## Functionality
The PlaceableBuilder processes placeable data from `./InputFiles/placeables.2da` and generates:

### 1. Category-Based Processing
The tool automatically categorizes placeables based on their names and assigns appropriate palette IDs:

| Category | Palette ID | Description |
|----------|------------|-------------|
| Starships | 26 | Space vehicles and ships |
| Structure | 27 | Building components |
| Battlefield | 28 | Combat-related objects |
| Furniture | 29 | Interior furnishings |
| Celestial | 30 | Astronomical objects |
| Food | 31 | Food and consumables |
| Lighting | 32 | Light sources |
| Vehicles | 33 | Ground vehicles |
| WallCovering | 34 | Wall decorations |
| Building | 35 | Architectural elements |
| Sign | 36 | Signs and notices |
| Ceiling | 37 | Ceiling elements |
| Vegetation | 38 | Plants and flora |
| Mech | 39 | Mechanical devices |
| Wall | 40 | Wall structures |
| GroundCovering | 41 | Floor coverings |
| Electronics | 42 | Electronic devices |
| Container | 43 | Storage containers |

### 2. Name Processing
- **Category Detection**: Identifies the category from the first word of the placeable name
- **Name Cleaning**: Removes the category prefix from the final name
- **ResRef Generation**: Creates standardized resource references

## Input File Format
The tool expects a 2DA file with the following structure:
- **Column 0**: Appearance ID
- **Column 1**: Placeable name (with category prefix)

Example:
```
0 "Starships: X-Wing Fighter"
1 "Furniture: Comfortable Chair"
2 "Lighting: Wall Sconce"
```

## Output Structure
```
./OutputPlaceables/
├── xm_plc_0000.utp.json
├── xm_plc_0001.utp.json
├── xm_plc_0002.utp.json
└── ...
```

## Generated Properties
Each generated placeable includes:
- **Appearance ID**: Original appearance identifier
- **Name**: Cleaned name without category prefix
- **Tag**: Standardized resource reference (xm_plc_[ID])
- **ResRef**: Same as tag for consistency
- **Palette ID**: Category-based palette assignment

## Template Usage
The tool uses `./Templates/placeable_template.json` as the base template and replaces:
- `%%APPEARANCEID%%` → Original appearance ID
- `%%NAME%%` → Cleaned placeable name
- `%%TAG%%` → Generated resource reference
- `%%RESREF%%` → Same as tag
- `%%PALETTEID%%` → Category-based palette ID

## Usage
This tool is used during development to:
- Generate placeable definitions from 2DA data
- Maintain consistent categorization of placeables
- Automate the creation of placeable templates
- Ensure proper palette assignments for visual organization

## Notes
- Skips the first 3 lines of the 2DA file (header information)
- Only processes placeables with recognized category prefixes
- Clears the output directory before generating new files
- Uses standardized naming conventions for resource references
- Generates JSON files compatible with the NWN module system 