# SWLOR CLI Commands Overview

## Introduction
The SWLOR CLI tool provides various commands for building, generating, and managing game content. Each command serves a specific purpose in the development workflow.

## Available Commands

### Content Generation Commands

| Command | Flag | Description |
|---------|------|-------------|
| **AdHocTool** | `-a` or `--adhoc` | Database maintenance and testing utility |
| **BeastCodeBuilder** | `-b` or `--beast` | Generates C# code for beast definitions |
| **DroidItemBuilder** | `-d` or `--droid` | Generates JSON files for droid items |
| **EnhancementItemBuilder** | `-e` or `--enhancement` | Generates JSON files for enhancement items |
| **LanguageBuilder** | `-l` or `--language` | Generates randomized language substitution code |
| **PlaceableBuilder** | `-c` or `--placeable` | Generates JSON files for placeable objects |
| **RecipeCodeBuilder** | `-r` or `--recipe` | Generates C# code for crafting recipes |
| **StructureItemCreator** | `-s` or `--structure` | Generates JSON files for structure items |

### Build and Deployment Commands

| Command | Flag | Description |
|---------|------|-------------|
| **HakBuilder** | `-k` or `--hak` | Builds hakpak files from source directories |
| **DeployBuild** | `-o` or `--outputDeploy` | Deploys complete debug server environment |

### Module Management Commands

| Command | Flag | Description |
|---------|------|-------------|
| **ModulePacker** | `-p` or `--pack` | Packs JSON files into NWN module format |
| **ModulePacker** | `-u` or `--unpack` | Unpacks NWN module to JSON format |

## Quick Reference

### Content Generation
```bash
# Generate all content types
SWLOR.CLI.exe -a -b -d -e -l -c -r -s

# Generate specific content
SWLOR.CLI.exe -b    # Beast definitions
SWLOR.CLI.exe -d    # Droid items
SWLOR.CLI.exe -e    # Enhancement items
SWLOR.CLI.exe -c    # Placeables
SWLOR.CLI.exe -r    # Recipes
SWLOR.CLI.exe -s    # Structures
```

### Build and Deploy
```bash
# Build hak files
SWLOR.CLI.exe -k

# Deploy complete environment
SWLOR.CLI.exe -o
```

### Module Operations
```bash
# Pack module
SWLOR.CLI.exe -p "path/to/module.mod"

# Unpack module
SWLOR.CLI.exe -u "path/to/module.mod"
```

## Input Files

### Required Input Files
- `./InputFiles/beast_levels.tsv` - Beast data for BeastCodeBuilder
- `./InputFiles/droid_item_list.tsv` - Droid data for DroidItemBuilder
- `./InputFiles/enhancement_list.csv` - Enhancement data for EnhancementItemBuilder
- `./InputFiles/placeables.2da` - Placeable data for PlaceableBuilder
- `./InputFiles/recipes.tsv` - Recipe data for RecipeCodeBuilder

### Configuration Files
- `./hakbuilder.json` - HakBuilder configuration
- `./Templates/` - Template files for various generators

## Output Directories

### Generated Content
- `./OutputBeasts/` - Beast definition files
- `./OutputDroidItems/` - Droid item files
- `./OutputEnhancements/` - Enhancement item files
- `./OutputPlaceables/` - Placeable files
- `./OutputRecipes/` - Recipe code files
- `./structure_output/` - Structure item files

### Build Output
- `./output/` - HakBuilder output (hak files and TLK)
- `../debugserver/` - DeployBuild output

## Dependencies

### External Tools
- `nwn_gff.exe` - GFF/JSON conversion
- `nwn_erf.exe` - Module packing/unpacking

### Internal Dependencies
- SWLOR.Game.Server project (for enums and attributes)
- Redis database (for AdHocTool)
- Various template files in `./Templates/`

## Development Workflow

### Typical Development Cycle
1. **Content Creation**: Use content generation commands to create game assets
2. **Build Process**: Use HakBuilder to compile assets into hak files
3. **Module Management**: Use ModulePacker to create distributable modules
4. **Deployment**: Use DeployBuild to set up debug environment

### Content Updates
1. Modify input files (TSV, CSV, 2DA)
2. Run appropriate generation command
3. Rebuild hak files if needed
4. Deploy to debug environment

## Notes
- All commands can be run independently
- Most commands clear their output directories before generating new content
- Template files should be modified carefully as they affect all generated content
- External tools must be present for module operations
- Database operations require proper Redis configuration 