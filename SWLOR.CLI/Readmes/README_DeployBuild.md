# DeployBuild

## Overview
The DeployBuild command automates the deployment process for the SWLOR game server by copying binaries, building hak files, and preparing the debug server environment.

## Command
```bash
SWLOR.CLI.exe -o
# or
SWLOR.CLI.exe --outputDeploy
```

## Functionality
The DeployBuild performs a complete deployment process:

### 1. Directory Creation
Creates the debug server directory structure:
```
../debugserver/
├── dotnet/          # Compiled binaries
├── hak/             # Built hak files
├── modules/         # Game modules
└── tlk/             # Talk files
```

### 2. Binary Deployment
- Copies all files from `../SWLOR.Game.Server/bin/Debug/net8.0/` to `../debugserver/dotnet/`
- Excludes the `swlor.env` file to preserve existing configuration
- Maintains directory structure during copy operations

### 3. Hak File Building
- Executes the HakBuilder process to compile all hak files
- Uses the same configuration as the standalone HakBuilder command
- Outputs hak files to the debug server's hak directory

### 4. Module Deployment
- Copies the main game module `../Module/Star Wars LOR v2.mod` to the debug server's modules directory
- Ensures the module is available for the server to load

## Dependencies
- **HakBuilder**: Requires the HakBuilder functionality to be available
- **Source Directories**: Requires the SWLOR.Game.Server project to be built
- **Module File**: Requires the main game module to exist

## File Structure
The tool expects the following structure:
```
SWLOR_NWN/
├── SWLOR.CLI/
├── SWLOR.Game.Server/
│   └── bin/Debug/net8.0/
├── SWLOR.Runner/
│   └── Docker/
└── Module/
    └── Star Wars LOR v2.mod
```

## Output
Creates a complete debug server environment at `../debugserver/` with:
- All necessary binaries and dependencies
- Compiled hak files
- Game module
- Docker configuration (excluding swlor.env)

## Usage
This tool is used during development to:
- Set up a complete debug server environment
- Deploy the latest build to the debug server
- Ensure all components are properly synchronized
- Prepare the server for testing and development

## Notes
- The tool preserves the `swlor.env` file to maintain existing configuration
- All operations are performed relative to the CLI tool's directory
- The debug server directory is created if it doesn't exist
- Existing files in the debug server directory are overwritten 