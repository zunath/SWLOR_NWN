# SWLOR.Game.Server Project Structure Documentation

This document provides comprehensive information about the overall project structure, build configuration, development setup, and project organization for SWLOR.Game.Server.

## Overview

SWLOR.Game.Server is a .NET 7.0 library project designed to run as a Neverwinter Nights server module. The project uses a layered architecture with clear separation of concerns, extensive use of the builder pattern, and comprehensive integration with the NWN engine.

## Project Structure

```
SWLOR.Game.Server/
├── ApplicationSettings.cs              # Application configuration
├── GlobalUsings.cs                    # Global using statements
├── SWLOR.Game.Server.csproj           # Project file
├── SWLOR.sln                          # Solution file
├── Core/                              # Core systems and abstractions
│   ├── Async/                         # Asynchronous programming
│   ├── Beamdog/                       # GUI and NUI systems
│   ├── Bioware/                       # Position and vector utilities
│   ├── Extensions/                    # Core extension methods
│   ├── NWNX/                          # NWNX plugin abstractions
│   ├── NWScript/                      # NWScript function wrappers
│   ├── Basetypes.cs                   # Base type definitions
│   ├── CoreGameManager.cs             # Core game management
│   ├── ICoreEventHandler.cs           # Core event handler interface
│   ├── Internal.cs                    # Internal utilities
│   ├── NWNCore.cs                     # NWN core functionality
│   ├── NWNCore.Native.cs             # Native NWN functions
│   ├── NWNCore.Native.Events.cs      # Native event handling
│   ├── NWNCore.Native.Globals.cs     # Native global variables
│   ├── NWNEventHandler.cs             # Event handler attributes
│   ├── ICoreFunctionHandler.cs        # Function handler interface
│   ├── Scheduler.cs                   # Task scheduling system
│   ├── ScheduledItem.cs               # Scheduled task items
│   ├── ScriptName.cs                  # Script name constants
│   └── VM.cs                          # Virtual machine utilities
├── Entity/                            # Data models and entities
├── Enumeration/                       # Enumeration types
├── Extension/                         # Extension methods
├── Feature/                           # Game-specific content
├── Native/                            # Native engine integration
├── Service/                           # Business logic services
├── Docker/                            # Deployment configuration
├── Properties/                        # Project properties
├── Readmes/                           # Documentation
└── obj/                               # Build artifacts
```

## Build Configuration

### 1. Project File

**Location**: `SWLOR.Game.Server.csproj`

**Key Configuration**:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net7.0</TargetFramework>
    <EnableDynamicLoading>true</EnableDynamicLoading>
    <LangVersion>10</LangVersion>
    <OutputType>Library</OutputType>
    <PlatformTarget>x64</PlatformTarget>
  </PropertyGroup>

  <PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Debug|AnyCPU'">
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
  </PropertyGroup>

  <PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Release|AnyCPU'">
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
  </PropertyGroup>
</Project>
```

**Key Features**:
- **Target Framework**: .NET 7.0
- **Dynamic Loading**: Enabled for plugin support
- **Language Version**: C# 10
- **Output Type**: Library (not executable)
- **Platform**: x64 only
- **Unsafe Blocks**: Allowed for native code

### 2. Dependencies

**NuGet Packages**:
```xml
<ItemGroup>
  <PackageReference Include="Discord.Net" Version="3.17.4" />
  <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
  <PackageReference Include="NRediSearch" Version="2.2.79" />
  <PackageReference Include="NReJSON" Version="4.0.0" />
  <PackageReference Include="NWN.Native" Version="8193.35.9" />
  <PackageReference Include="Serilog" Version="4.3.0" />
  <PackageReference Include="Serilog.Sinks.Console" Version="6.0.0" />
  <PackageReference Include="Serilog.Sinks.File" Version="7.0.0" />
  <PackageReference Include="StackExchange.Redis" Version="2.8.41" />
  <PackageReference Include="System.Reflection.Emit" Version="4.7.0" />
</ItemGroup>
```

**Key Dependencies**:
- **NWN.Native**: Neverwinter Nights engine integration
- **StackExchange.Redis**: Redis client for caching
- **Serilog**: Logging framework
- **Discord.Net**: Discord bot integration
- **Newtonsoft.Json**: JSON serialization

### 3. Build Targets

**Post-Build Processing**:
```xml
<Target Name="PostBuild" AfterTargets="PostBuildEvent">
  <Exec Command="cd $(SolutionDir)Build&#xD;&#xA;SWLOR.CLI.exe -o" />
</Target>
```

**NWN Target**:
```xml
<Target Name="NWN" AfterTargets="Build">
  <ItemGroup>
    <AllOutputs Include="$(OutputPath)$(MSBuildProjectName).dll" />
    <AllOutputs Include="$(OutputPath)$(MSBuildProjectName).pdb" />
    <AllOutputs Include="$(OutputPath)$(MSBuildProjectName).runtimeconfig.json" />
    <AllOutputs Include="$(OutputPath)$(MSBuildProjectName).deps.json" />
  </ItemGroup>
</Target>
```

## Application Configuration

### 1. ApplicationSettings

**Location**: `ApplicationSettings.cs`

**Purpose**: Centralized application configuration management.

**Key Features**:
```csharp
public class ApplicationSettings
{
    public string LogDirectory { get; }
    public string RedisIPAddress { get; }
    public ServerEnvironmentType ServerEnvironment { get; }
    
    private static ApplicationSettings _settings;
    public static ApplicationSettings Get()
    {
        if (_settings == null)
            _settings = new ApplicationSettings();
        return _settings;
    }
}
```

**Environment Detection**:
```csharp
private ApplicationSettings()
{
    LogDirectory = Environment.GetEnvironmentVariable("SWLOR_APP_LOG_DIRECTORY");
    RedisIPAddress = Environment.GetEnvironmentVariable("NWNX_REDIS_HOST");
    
    var environment = Environment.GetEnvironmentVariable("SWLOR_ENVIRONMENT");
    ServerEnvironment = ParseEnvironment(environment);
}
```

### 2. Global Usings

**Location**: `GlobalUsings.cs`

**Purpose**: Provide global using statements for common namespaces.

```csharp
global using static SWLOR.Game.Server.Core.NWScript.NWScript;
```

## Architecture Layers

### 1. Core Layer

**Purpose**: Fundamental systems and abstractions.

**Components**:
- **Async**: Asynchronous programming support
- **Beamdog**: GUI and NUI systems
- **Bioware**: Position and vector utilities
- **Extensions**: Core extension methods
- **NWNX**: NWNX plugin abstractions
- **NWScript**: NWScript function wrappers

**Key Files**:
- `CoreGameManager.cs`: Central game management
- `Scheduler.cs`: Task scheduling system
- `VM.cs`: Virtual machine utilities

### 2. Entity Layer

**Purpose**: Data models and database entities.

**Components**:
- Player character data
- Game state entities
- Database models
- Entity relationships

### 3. Service Layer

**Purpose**: Business logic and game mechanics.

**Components**:
- Game system services
- Business logic implementation
- Cross-cutting concerns
- Integration points

### 4. Feature Layer

**Purpose**: Game-specific content and implementations.

**Components**:
- Ability definitions
- Item definitions
- Quest definitions
- Perk definitions
- Content builders

### 5. Native Layer

**Purpose**: Low-level engine integration.

**Components**:
- Custom attack resolution
- Damage calculation
- Saving throw systems
- Engine hooks

## Development Setup

### 1. Prerequisites

**Required Software**:
- .NET 7.0 SDK
- Visual Studio 2022 or VS Code
- Docker and Docker Compose
- Redis server
- NWN server environment

**System Requirements**:
- Windows 10/11 (x64)
- 8GB RAM minimum
- 20GB disk space
- SSD recommended

### 2. Development Environment

**Local Development**:
```bash
# Clone repository
git clone <repository-url>
cd SWLOR.Game.Server

# Restore dependencies
dotnet restore

# Build project
dotnet build

# Run tests
dotnet test
```

**Docker Development**:
```bash
# Start development environment
docker-compose up --build

# View logs
docker-compose logs -f swlor-server
```

### 3. Configuration

**Environment Variables**:
```bash
# Development
SWLOR_ENVIRONMENT=development
SWLOR_APP_LOG_DIRECTORY=./logs
NWNX_REDIS_HOST=localhost:6379

# Production
SWLOR_ENVIRONMENT=production
SWLOR_APP_LOG_DIRECTORY=/app/logs
NWNX_REDIS_HOST=redis:6379
```

## Build Process

### 1. Compilation

**Debug Build**:
```bash
dotnet build --configuration Debug
```

**Release Build**:
```bash
dotnet build --configuration Release
```

**Clean Build**:
```bash
dotnet clean
dotnet build
```

### 2. Post-Build Processing

**CLI Processing**:
- Builds complete NWN module
- Processes game content
- Generates final output files
- Validates module integrity

### 3. Output Files

**Generated Files**:
- `SWLOR.Game.Server.dll`: Main assembly
- `SWLOR.Game.Server.pdb`: Debug symbols
- `SWLOR.Game.Server.runtimeconfig.json`: Runtime configuration
- `SWLOR.Game.Server.deps.json`: Dependency information

## Project Organization

### 1. Naming Conventions

**Files and Directories**:
- Use PascalCase for file and directory names
- Use descriptive names that indicate purpose
- Group related functionality together

**Code Elements**:
- Use PascalCase for public members
- Use camelCase for private members
- Use descriptive names for variables and methods

### 2. File Organization

**By Layer**:
- Core systems in `Core/`
- Data models in `Entity/`
- Business logic in `Service/`
- Game content in `Feature/`
- Native code in `Native/`

**By Functionality**:
- Related files grouped in subdirectories
- Clear separation of concerns
- Logical file organization

### 3. Documentation

**Documentation Structure**:
- `Readmes/`: Comprehensive documentation
- XML comments in code
- Inline comments for complex logic
- Architecture documentation

## Development Workflow

### 1. Feature Development

**Process**:
1. Create feature branch
2. Implement feature in appropriate layer
3. Add tests for new functionality
4. Update documentation
5. Create pull request
6. Code review and merge

**Guidelines**:
- Follow existing patterns
- Use builder pattern for complex objects
- Add comprehensive logging
- Include error handling

### 2. Testing Strategy

**Unit Testing**:
- Test individual components
- Mock external dependencies
- Use appropriate test frameworks
- Maintain high test coverage

**Integration Testing**:
- Test system interactions
- Verify data flow
- Test error conditions
- Validate performance

### 3. Code Quality

**Standards**:
- Follow C# coding conventions
- Use meaningful variable names
- Add XML documentation
- Handle exceptions properly

**Tools**:
- Use static analysis tools
- Run code formatters
- Check for code smells
- Validate architecture compliance

## Deployment

### 1. Development Deployment

**Local Development**:
```bash
# Build and run locally
dotnet build
dotnet run

# Use Docker for consistency
docker-compose up --build
```

### 2. Production Deployment

**Docker Deployment**:
```bash
# Build production image
docker build -t swlor-game-server:latest .

# Deploy with production config
docker-compose -f docker-compose.prod.yml up -d
```

**Environment Configuration**:
- Use environment-specific settings
- Configure monitoring and logging
- Set up backup and recovery
- Implement security measures

## Monitoring and Maintenance

### 1. Logging

**Log Configuration**:
```csharp
// Serilog configuration
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/swlor-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();
```

**Log Groups**:
- `LogGroup.Attack`: Combat-related logs
- `LogGroup.Ability`: Ability usage logs
- `LogGroup.Quest`: Quest progression logs
- `LogGroup.Error`: Error and exception logs

### 2. Performance Monitoring

**Metrics**:
- Response times
- Memory usage
- CPU utilization
- Network activity

**Tools**:
- Grafana dashboards
- Performance profiling
- Memory leak detection
- Error tracking

### 3. Maintenance

**Regular Tasks**:
- Update dependencies
- Review and update documentation
- Monitor performance metrics
- Backup data and configurations

**Emergency Procedures**:
- Rollback procedures
- Data recovery processes
- Incident response plans
- Communication protocols

## Troubleshooting

### 1. Build Issues

**Common Problems**:
- Missing dependencies
- Version conflicts
- Platform compatibility
- Configuration errors

**Solutions**:
- Clean and rebuild
- Update dependencies
- Check platform requirements
- Verify configuration

### 2. Runtime Issues

**Common Problems**:
- Memory leaks
- Performance degradation
- Connection failures
- Data corruption

**Solutions**:
- Monitor resource usage
- Profile performance
- Check network connectivity
- Validate data integrity

### 3. Development Issues

**Common Problems**:
- Debugging difficulties
- Test failures
- Integration problems
- Documentation gaps

**Solutions**:
- Use debugging tools
- Review test coverage
- Check integration points
- Update documentation

---

*This documentation should be updated when project structure changes or new development processes are added.* 