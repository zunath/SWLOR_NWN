# ModulePacker

## Overview
The ModulePacker provides functionality to pack and unpack NWN module files (.mod). It converts JSON files to GFF format and handles script compilation for module creation and extraction.

## Commands

### Pack Module
```bash
SWLOR.CLI.exe -p "path/to/module.mod"
# or
SWLOR.CLI.exe --pack "path/to/module.mod"
```

### Unpack Module
```bash
SWLOR.CLI.exe -u "path/to/module.mod"
# or
SWLOR.CLI.exe --unpack "path/to/module.mod"
```

## Functionality

### Pack Module Process
1. **Temporary Directory Creation**: Creates `./packing/` directory for temporary files
2. **JSON to GFF Conversion**: Converts all JSON files in module folders to GFF format using `nwn_gff`
3. **Script File Copying**: Copies both `.nss` (source) and `.ncs` (compiled) script files
4. **Module Assembly**: Uses `nwn_erf` to create the final `.mod` file
5. **Cleanup**: Removes temporary files and directories

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
- **Parallel Processing**: Uses parallel processing for file operations
- **Progress Reporting**: Displays progress information during operations
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
- Waits for user input before completing operations 