# DeployBuild

## Overview
The DeployBuild command automates the deployment process for the SWLOR game server by copying binaries, building hak files, and preparing the debug server environment.

## Command
```bash
cd Build
..\tools\SWLOR.CLI\RunCLI.cmd -o
# or
..\tools\SWLOR.CLI\RunCLI.cmd --outputDeploy
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
- The layout follows `SWLOR_DOTNET_ENTRY` in `../debugserver/swlor.env` (`host` when the file does not name one). `host`: `SWLOR.Game.Host` goes to `../debugserver/dotnet/` and the game to `../debugserver/dotnet/game/`. `native`: the game goes to `../debugserver/dotnet/`. Both outputs carry `SWLOR.Game.NativeHooks.dll` (the native hook trampolines), so it lands beside the entry assembly in either layout. The `NWNX_DOTNET_ASSEMBLY`, `NWNX_DOTNET_ENTRYPOINT` and `NWNX_DOTNET_METHOD` lines of that file are written to match.
- Copies the game from the initiating build's `TargetDir`; standalone deployment defaults to `../SWLOR.Game.Server/bin/Release/net10.0/`. Use `--server-output` to select another output directory.
- Copies the host (host entry only) from `--host-output`, which the game's post-build sets to the host build of the same configuration; standalone deployment defaults to `../SWLOR.Game.Host/bin/Release/net10.0/`, which `RunCLI.cmd -o` builds first.
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
│   ├── bin/Release/net10.0/
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
- All operations are performed relative to the current working directory; use the repository `Build` directory
- The debug server directory is created if it doesn't exist
- Existing files in the debug server directory are overwritten 
- Missing content assets do not suppress binary or Docker deployment. Missing configured HAK directories or TLK skip only HAK/TLK deployment, preserving deployed assets. A missing packed module skips only module deployment. Each skip emits a build warning; a fresh debugserver still requires those assets before it can run. Initialize `SWLOR_Haks` and run `PackModule.cmd` from `Module`, then rebuild to complete deployment.
