# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Response Style

Keep responses terse and direct. Skip preamble, restatements of the request, and closing summaries. Answer in as few words as the task allows; lead with the result. Use prose only where it adds information the user doesn't already have.

## Agent Rules (Required Reading)

The mandatory project rules — read-only areas, naming, stat-driven gameplay, player identity, Design Bible workflow, TLK entries, ability definitions/icons/VFX, and more — live in AGENTS.md and are imported here:

@AGENTS.md

## Project Overview

SWLOR (Star Wars: Legends of the Old Republic) is a Neverwinter Nights: Enhanced Edition server using C# for server-side logic via NWNX_DotNet. This replaces traditional NWScript with a modern .NET 10.0 architecture.

## Development Commands

### Building the Project
```bash
# Build entire solution (repo root)
dotnet build SWLOR.Game.Server.sln

# Clean and rebuild
dotnet clean && dotnet build

# Build in release mode
dotnet build --configuration Release
```

### Running the Server
```bash
# Set SWLOR.Runner as startup project in Visual Studio
# Run with F5 or the green run button
# OR via command line:
cd SWLOR.Runner
dotnet run
```

### Module and Asset Management
```bash
# Pack module (from Module directory)
PackModule.cmd

# Build HAK files (from SWLOR_Haks directory)  
BuildHaks.cmd

# CLI operations
cd Build
..\tools\SWLOR.CLI\RunCLI.cmd -o             # Deploy/build from Build working directory
cd ..\Module
PackModule.cmd                              # Pack module from Module working directory
```

### Docker Development
```bash
# Start development environment
cd debugserver
docker-compose up --build

# View logs
docker-compose logs -f
```

## Project Architecture

The codebase follows a layered architecture with these main components:

### Core Layer (`SWLOR.Game.Server/Core/`)
- **NWScript**: Abstractions over NWN engine functions
- **NWNX**: Plugin interfaces for extended functionality  
- **Async**: Asynchronous programming support (`NwTask`, awaiters)
- **Beamdog**: GUI/NUI system components
- **Extensions**: Utility extension methods

### Service Layer (`SWLOR.Game.Server/Service/`)
- Business logic and game mechanics (30+ services)
- Examples: `Combat.cs`, `Skill.cs`, `Quest.cs`, `Ability.cs`
- Services handle cross-cutting concerns and system integration

### Entity Layer (`SWLOR.Game.Server/Entity/`)
- Data models and database entities
- Examples: `Player.cs`, `Account.cs`, `ServerConfiguration.cs`
- Redis-based persistence with caching

### Feature Layer (`SWLOR.Game.Server/Feature/`)
- Game content definitions using builder patterns
- **AbilityDefinition**: Combat abilities and skills
- **PerkDefinition**: Character perks and progression
- **RecipeDefinition**: Crafting recipes
- **QuestDefinition**: Quest content
- **ItemDefinition**: Item behaviors
- Other: Dialogs, GUI definitions, spawn tables, etc.

### Additional Projects
- **SWLOR.CLI**: Command-line tools for module packing, HAK building
- **SWLOR.Runner**: Development server runner with Docker integration
- **SWLOR.Web**: Web components
- **SWLOR.Admin**: Administrative interface

## Key Development Patterns

### Builder Pattern (Heavily Used)
The project extensively uses builders for creating game content:

```csharp
// Ability creation example
public void OnImpact(uint activator, uint target, int level, Location targetLocation)
{
    var damage = level * 10;
    ApplyEffectToObject(DurationType.Instant, 
        EffectDamage(damage, DamageType.Fire), target);
}

// Perk creation example
public void OnPurchased(uint creature, int newLevel)
{
    // Apply perk benefits
}
```

### Service Integration
Services are accessed statically and provide game functionality:

```csharp
// Combat damage
Combat.ApplyDamage(attacker, target, damage, DamageType.Physical);

// Skill checks  
var skillLevel = Skill.GetSkillLevel(player, SkillType.OneHanded);

// Quest progress
Quest.AdvanceQuest(player, "my_quest", player);
```

### Event-Driven Architecture
Uses NWN events and custom event handlers:

```csharp
[NWNEventHandler("mod_enter")]
public static void OnPlayerEnter(uint player)
{
    // Handle player login
}
```

## File Organization Conventions

- **Definitions**: Game content organized by type in Feature subdirectories
- **Services**: Business logic in Service directory, one service per major system
- **Entities**: Data models with Redis serialization attributes
- **Extensions**: Utility methods in logical groupings
- **Builders**: Creation patterns for complex objects

## Dependencies and Technologies

- **.NET 10.0**: Main framework (see `TargetFramework` in `*.csproj` files - every project targets `net10.0` on this branch)
- **NWN.Native**: Neverwinter Nights engine integration
- **StackExchange.Redis**: Caching and persistence
- **Serilog**: Logging framework
- **Discord.Net**: Discord bot integration
- **Newtonsoft.Json**: JSON serialization
- **Docker**: Development and deployment

## Testing

Testing is split into two layers:

- **`SWLOR.Game.Server.Tests`** - an NUnit suite for pure logic and data (calculations, builder
  wiring, Bible/2DA consistency). It never touches the NWN engine.
- **In-engine integration tests** - methods tagged `[EngineTest]` that run inside the live NWN
  server via `scripts/run-engine-tests.ps1` / `.sh`, exercising real feat activation, status
  effects, and native combat hooks against spawned NPCs. See
  `SWLOR.Game.Server/Readmes/EngineTesting.md` for the full architecture, env var reference, and
  how to write/run tests.

## Common Development Tasks

1. **Adding new abilities**: Create class in `Feature/AbilityDefinition/` using builder pattern
2. **Adding new items**: Create class in `Feature/ItemDefinition/` with use handlers
3. **Adding new perks**: Create class in `Feature/PerkDefinition/` with progression logic
4. **Adding new services**: Create in `Service/` directory with static methods
5. **Adding new entities**: Create in `Entity/` with Redis attributes

## Troubleshooting

- **Build errors**: Ensure the .NET 10.0 SDK is installed and dependencies are restored
- **Module errors**: Run `Module/PackModule.cmd` to regenerate the .mod file  
- **Docker issues**: Check Docker Desktop is running and containers are started
- **Missing HAKs/TLK**: Copy files from `debugserver/` to NWN directories or update nwn.ini paths

## Important Notes

- This is a live game server - changes affect real players
- Follow existing patterns and conventions
- Use comprehensive logging via Serilog
- Test thoroughly before deployment
- The module system uses JSON for area/dialog/etc. definitions
- HAK files contain game assets and must be rebuilt when changed
- When a 2DA references a SWLOR custom TLK entry, use the NWN custom TLK StrRef value: `16777216 + tlk entry id`. For example, TLK entry `50003` must be referenced in 2DAs as `16827219`. Raw TLK entry IDs are only valid for base-game `dialog.tlk` references, not custom hak TLK rows.
