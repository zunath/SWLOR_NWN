# ModulePacker

## Overview
The ModulePacker provides functionality to pack and unpack NWN module files (.mod). It converts JSON files to GFF format and handles script compilation for module creation and extraction.

`RunCLI.cmd` performs an incremental Release build before invoking the CLI, preventing the documented pack command from silently using an outdated committed executable.

## Commands

### Pack Module
```bash
cd Module
..\tools\SWLOR.CLI\RunCLI.cmd -p ".\Star Wars LOR v2.mod"
# or
..\tools\SWLOR.CLI\RunCLI.cmd --pack ".\Star Wars LOR v2.mod"
```

### Unpack Module
```bash
cd Module
..\tools\SWLOR.CLI\RunCLI.cmd -u ".\Star Wars LOR v2.mod"
# or
..\tools\SWLOR.CLI\RunCLI.cmd --unpack ".\Star Wars LOR v2.mod"
```

## Functionality

### Pack Module Process
1. **Temporary Directory Creation**: Creates isolated packing and palette-refresh directories
2. **Palette Refresh**: Rebuilds temporary custom blueprint palette entries from the module's
   creature, door, encounter, item, placeable, sound, store, trigger, and waypoint blueprints,
   matching Aurora Toolset refresh behavior without modifying the source JSON
3. **JSON to GFF Conversion**: Converts all JSON files in module folders to GFF format using `nwn_gff`
4. **Script File Copying**: Copies both `.nss` (source) and `.ncs` (compiled) script files
5. **Module Assembly**: Uses `nwn_erf` to create the final `.mod` file
6. **Cleanup**: Removes temporary files and directories

### Unpack Module Process
1. **Directory Preparation**: Creates and cleans module folders
2. **Module Extraction**: Uses `nwn_erf` to extract the module contents
3. **GFF to JSON Conversion**: Converts extracted GFF files back to JSON format
4. **Script Organization**: Moves script files to appropriate directories
5. **File Cleanup**: Removes extracted GFF files after conversion

## Module Folders
The tool processes the following module folder types:
- `./are` - Area files
- `./dlg` - Dialogue files
- `./fac` - Faction files
- `./gic` - Generic item classes
- `./git` - Generic item templates
- `./ifo` - Module information
- `./itp` - Item properties
- `./jrl` - Journal entries
- `./utc` - Creature templates
- `./utd` - Door templates
- `./ute` - Encounter templates
- `./uti` - Item templates
- `./utm` - Merchant templates
- `./utp` - Placeable templates
- `./uts` - Sound templates
- `./utt` - Trigger templates
- `./utw` - Waypoint templates

## External Dependencies
- **nwn_gff.exe**: Converts between JSON and GFF formats
- **nwn_erf.exe**: Handles module packing and unpacking

## Performance
- **Bounded Parallel Processing**: Uses a limited worker pool for resource conversion so packing and unpacking do not saturate the machine. By default it reserves two processors for system responsiveness and caps conversion workers at 12.
- **Worker Count Override**: Set `SWLOR_RESOURCE_CONVERSION_WORKERS` to control the maximum number of concurrent `nwn_gff` conversions
- **Transient Failure Recovery**: Retries an individual `nwn_gff` conversion up to three times before failing the full pack or unpack operation
- **Progress Reporting**: Displays periodic progress information during operations
- **Timing**: Reports completion time for both pack and unpack operations

## Output Structure

### Packed Module
Creates a single `.mod` file containing all module data in NWN-compatible format.

### Unpacked Module
Creates the following directory structure:
```
./
├── are/          # Area files (JSON format)
├── dlg/          # Dialogue files (JSON format)
├── fac/          # Faction files (JSON format)
├── gic/          # Generic item classes (JSON format)
├── git/          # Generic item templates (JSON format)
├── ifo/          # Module information (JSON format)
├── itp/          # Item properties (JSON format)
├── jrl/          # Journal entries (JSON format)
├── ncs/          # Compiled scripts
├── nss/          # Source scripts
├── utc/          # Creature templates (JSON format)
├── utd/          # Door templates (JSON format)
├── uti/          # Item templates (JSON format)
├── utm/          # Merchant templates (JSON format)
├── utp/          # Placeable templates (JSON format)
├── uts/          # Sound templates (JSON format)
├── utt/          # Trigger templates (JSON format)
└── utw/          # Waypoint templates (JSON format)
```

## Usage
This tool is used during development to:
- Create distributable module files
- Extract and modify existing modules
- Convert between JSON and GFF formats
- Manage module content and scripts

## Notes
- Requires external NWN tools (`nwn_gff.exe`, `nwn_erf.exe`)
- Handles both source and compiled scripts
- Maintains file extensions in lowercase for compatibility
- Provides detailed progress and timing information
- Waits for user input before completing operations when launched interactively
