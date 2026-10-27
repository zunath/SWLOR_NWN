# SWLOR.Game.Server Core Systems Documentation

This document provides an overview of the Core systems in SWLOR.Game.Server, which provide fundamental functionality and abstractions used throughout the game.

## Overview

The Core layer contains fundamental systems, abstractions, and utilities that provide the foundation for the entire game. These systems handle low-level operations, provide abstractions over the NWN engine, and offer common functionality used by services and features.

## Directory Structure

```text
Core/
├── Async/
│   ├── Awaiters/
│   ├── MainThreadSynchronizationContext.cs
│   └── NwTask.cs
├── Beamdog/
│   ├── Nui.cs
│   ├── NuiAspect.cs
│   ├── NuiChartType.cs
│   └── [GUI Components]
├── Bioware/
│   ├── BiowarePosition.cs
│   ├── BiowareVector.cs
│   └── BiowareXP2.cs
├── Extensions/
│   ├── CollectionExtensions.cs
│   ├── DestroyObjectExtension.cs
│   ├── EffectExtension.cs
│   └── [Other Extensions]
├── NWNX/
│   ├── AdministrationPlugin.cs
│   ├── AreaPlugin.cs
│   ├── ChatPlugin.cs
│   ├── Enum/
│   └── [Plugin Interfaces]
├── NWScript/
│   ├── Alignment.cs
│   ├── Area.cs
│   ├── Associate.cs
│   ├── Enum/
│   └── [Script Functions]
├── Basetypes.cs
├── CoreGameManager.cs
├── ICoreEventHandler.cs
└── GlobalUsings.cs
```

## Core Systems

### 1. NWScript System

**Purpose**: Provides abstractions and wrappers for NWN script functions.

**Key Components**:
- `Area.cs` - Area-related functions
- `Alignment.cs` - Alignment system
- `Associate.cs` - Companion/follower system
- Various enum definitions

**Usage**:
```csharp
// Get area by object
var area = GetArea(player);

// Get first object in area
var firstObject = GetFirstObjectInArea(area);

// Get next object
var nextObject = GetNextObjectInArea(area);
```

**Engine seam.** Every static wrapper in `SWLOR.NWN.API/NWScript` and `NWScriptCustom` forwards to `NWScriptRuntime.Current`, an `INWScript`. By default that is `NwnCoreNWScript`, which holds the original wrapper bodies and calls the engine, so game code and call sites are unchanged. A unit test swaps the engine for `FakeNWScript` (project `SWLOR.NWN.API.Testing`), which records every call and returns configured or default values (zero, empty string, `OBJECT_INVALID` for every `uint` return, empty engine structures):

```csharp
var engine = new FakeNWScript();
engine.SetReturn("GetEnteringObject", player);          // also SetReturnSequence and SetReturnHandler
using var scope = NWScriptRuntime.Override(engine);     // per async flow; parallel tests do not see each other

Activity.WipeStatusOnEntry();

engine.Calls.Select(call => call.ToString()).Should().Contain("DeleteLocalBool(1001, \"IS_BUSY\")");
```

- Wrappers that contain logic (for example `GetLocalBool`, `ClearAllActions` resolving `OBJECT_SELF`, `RemoveEffectByTag`) are faked at their own level: the fake records the call and does not run the wrapper's body, so configure the wrapper's own return value.
- Write a new or changed wrapper as before (a static method whose body calls `global::NWN.Core.NWScript`), then run `tools/Refactor/Apply-EngineSeam.ps1`. It moves the body into `NwnCoreNWScript`, extends `INWScript` and `FakeNWScript`, and rewrites the wrapper to forward. Never edit `SWLOR.NWN.API/Generated/` or `SWLOR.NWN.API.Testing/Generated/` by hand. `-Check` writes nothing and fails when the checked-in output is out of date; `SWLOR.NWN.API.Tests` runs the same check.
- Public constants stay static (an interface cannot hold them). The generator rejects any other member shape (generics, `ref`/`out` parameters, attributes, fields, nested types), so nothing slips past the seam.

### 2. NWNX Plugin System

**Purpose**: Provides access to NWNX plugin functionality.

**Key Components**:
- `AdministrationPlugin.cs` - Admin functions
- `AreaPlugin.cs` - Area management
- `ChatPlugin.cs` - Chat system
- Various plugin interfaces

**Usage**:
```csharp
// Send chat message
ChatPlugin.SendMessage(0, "Hello World", ChatChannelType.PlayerTalk);

// Get area info
var areaInfo = AreaPlugin.GetAreaInfo(area);
```

### 3. Async System

**Purpose**: Provides asynchronous programming support for NWN.

**Key Components**:
- `NwTask.cs` - Task-like functionality
- `MainThreadSynchronizationContext.cs` - Thread synchronization
- Various awaiters for NWN operations

**Usage**:
```csharp
// Async delay
await NwTask.Delay(1000); // 1 second delay

// Async area transition
await NwTask.Run(() => {
    AssignCommand(player, () => {
        ActionJumpToLocation(location);
    });
});
```

### 4. Beamdog GUI System

**Purpose**: Provides GUI components and functionality.

**Key Components**:
- `Nui.cs` - NUI system
- `NuiAspect.cs` - Aspect ratios
- `NuiChartType.cs` - Chart types
- Various GUI components

**Usage**: Windows are NOT built against this low-level layer directly. Author them
with the MVVM wrapper: an `IGuiWindowDefinition` using `GuiWindowBuilder<TViewModel>`
plus a `GuiViewModelBase` ViewModel:

```csharp
var window = _builder.CreateWindow(GuiWindowType.Example)
    .SetInitialGeometry(0, 0, 900f, 560f)
    .SetTitle("Example");

window.AddStandardLayout(layout =>
{
    layout.SetContentPartialElement(ExampleViewModel.TabContentElement);
});

return _builder.Build();
```

See `Readmes/GuiWindowAuthoring.md` for the full authoring guide and
`Readmes/NuiLayoutRules.md` for the layout rules.

### 5. Bioware Extensions

**Purpose**: Provides extensions for Bioware-specific functionality.

**Key Components**:
- `BiowarePosition.cs` - Position handling
- `BiowareVector.cs` - Vector operations
- `BiowareXP2.cs` - XP2 format support

**Usage**:
```csharp
// Create position
var position = new BiowarePosition(x, y, z, facing);

// Create vector
var vector = new BiowareVector(x, y, z);
```

### 6. Extension Methods

**Purpose**: Provides extension methods for common operations.

**Key Components**:
- `CollectionExtensions.cs` - Collection utilities
- `DestroyObjectExtension.cs` - Object destruction
- `EffectExtension.cs` - Effect utilities

**Usage**:
```csharp
// Destroy object safely
object.Destroy();

// Apply effect with duration
target.ApplyEffect(DurationType.Temporary, effect, 10.0f);
```

## Core Game Manager

### 1. CoreGameManager

**Purpose**: Central management class for core game systems.

**Key Responsibilities**:
- Initialize core systems
- Manage game state
- Handle core events
- Coordinate between systems

**Usage**:
```csharp
// Initialize core systems
CoreGameManager.Initialize();

// Handle core events
CoreGameManager.OnPlayerLogin += (player) => {
    // Handle player login
};

// Shutdown core systems
CoreGameManager.Shutdown();
```

### 2. ICoreEventHandler

**Purpose**: Interface for core event handlers.

**Key Events**:
- Player login/logout
- Area transitions
- Combat events
- System events

**Usage**:
```csharp
public class MyEventHandler : ICoreEventHandler
{
    public void OnPlayerLogin(uint player)
    {
        // Handle player login
    }

    public void OnPlayerLogout(uint player)
    {
        // Handle player logout
    }
}
```

## Core Patterns

### 1. Plugin Pattern

```csharp
// Plugin interface
public interface IPlugin
{
    void Initialize();
    void Shutdown();
}

// Plugin implementation
public class MyPlugin : IPlugin
{
    public void Initialize()
    {
        // Initialize plugin
    }

    public void Shutdown()
    {
        // Shutdown plugin
    }
}
```

### 2. Extension Pattern

```csharp
// Extension method
public static class ObjectExtensions
{
    public static void SafeDestroy(this uint obj)
    {
        if (GetIsObjectValid(obj))
        {
            DestroyObject(obj);
        }
    }
}

// Usage
object.SafeDestroy();
```

### 3. Async Pattern

```csharp
// Async operation
public static async NwTask<bool> MoveToLocation(uint creature, Location target)
{
    AssignCommand(creature, () => {
        ActionMoveToLocation(target);
    });

    // Wait for movement to complete
    await NwTask.Delay(100);

    return GetDistanceBetween(creature, target) < 2.0f;
}
```

## Core Utilities

### 1. Basetypes

**Purpose**: Defines basic types used throughout the system.

**Key Types**:
- `ObjectType` - Object type enumerations
- `DamageType` - Damage type enumerations
- `EffectType` - Effect type enumerations

**Usage**:
```csharp
// Check object type
if (GetObjectType(object) == ObjectType.Creature)
{
    // Handle creature
}

// Apply damage
ApplyEffectToObject(DurationType.Instant,
    EffectDamage(10, DamageType.Fire), target);
```

### 2. Global Usings

**Purpose**: Provides global using statements for common namespaces.

**Usage**:
```csharp
// Automatically available throughout the project
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Core.NWScript.Enum;
```

## Core Integration

### 1. Service Integration

Core systems integrate with services:

```csharp
// Service using core functionality
public static class CombatService
{
    public static void ApplyDamage(uint target, int damage, DamageType damageType)
    {
        // Use core damage system
        var effect = EffectDamage(damage, damageType);
        ApplyEffectToObject(DurationType.Instant, effect, target);

        // Use core logging
        Log.Write(LogGroup.Combat, $"Applied {damage} {damageType} damage to {target}");
    }
}
```

### 2. Builder Integration

Builders use core systems:

```csharp
// Builder using core functionality
public class AbilityBuilder
{
    public AbilityBuilder UsesAnimation(Animation animation)
    {
        // Use core animation system
        _activeAbility.AnimationType = animation;
        return this;
    }
}
```

### 3. Event Integration

Core systems handle events:

```csharp
// Core event handling
public static void OnPlayerDamaged(uint player, uint attacker, int damage)
{
    // Use core combat system
    var damageEffect = EffectDamage(damage, DamageType.Physical);
    ApplyEffectToObject(DurationType.Instant, damageEffect, player);

    // Use core logging
    Log.Write(LogGroup.Combat, $"Player {player} took {damage} damage from {attacker}");
}
```

### 4. Script Handler Order

At boot `ScriptRegistry.LoadHandlersFromAssembly` finds every `[NWNEventHandler(ScriptName.X)]` method by reflection and registers it under its script name. A script runs its handlers front to back (a failing action handler is logged and the rest still run; every conditional handler runs and the results are ANDed). **The order is declared in `SWLOR.Game.Server/Core/ScriptHandlerOrder.txt`, the script handler manifest, and nowhere else.** Reflection order follows namespaces and file paths, so it is never consulted for a handler the manifest lists: moving a file, renaming a namespace or adding an unrelated file cannot reorder handlers. Some order is load-bearing: `Service.DB.Load` must stay ahead of `Feature.EventRegistration.OnModulePreload` in `mod_preload`, because the first connects Redis and the second reads it.

**Format.** One block per script. The script name starts in column 0 (an optional `  # comment` may follow it), then one indented line per handler in run order: `action` (a `void` handler) or `conditional` (a `bool` handler), then `Namespace.Type.Method`. Blank lines, `#` comment lines and `== section ==` headings are ignored. Anything else is an error.

```text
mod_preload  # run by the engine before the module loads
  action      SWLOR.Game.Server.Service.DB.Load
  action      SWLOR.Game.Server.Feature.EventRegistration.OnModulePreload
```

**Adding a handler.**

1. Write the `[NWNEventHandler]` method.
2. Build, so the generator sees the new handler: `dotnet build SWLOR.Game.Server.Tests -p:RunPostBuildEvent=Never -v q -clp:ErrorsOnly`.
3. Run the manifest generator: `SWLOR_UPDATE_SNAPSHOTS=1 dotnet test SWLOR.Game.Server.Tests --no-build --filter "FullyQualifiedName~ScriptHandlerOrderTests"`
   (PowerShell: `$env:SWLOR_UPDATE_SNAPSHOTS='1'; dotnet test ...; Remove-Item Env:SWLOR_UPDATE_SNAPSHOTS`). It rewrites the manifest from what the server would load, so existing handlers keep their order and a new handler is **appended to the end of its script's block**.
4. If the new handler must run before others, move its line in the manifest. The generator never reorders lines it already has.
5. Build again (same command as step 2) and run `dotnet test SWLOR.Game.Server.Tests --no-build --filter "FullyQualifiedName~ScriptHandler"`. The server embeds the manifest, so the assembly carries the new one only after a rebuild.
6. Commit the manifest with the handler. `ScriptHandlerOrderTests` fails while the manifest is not complete or names a handler that no longer exists.

**Renaming or moving a handler method or class.** Edit its manifest line in place. Regenerating instead would drop the old line and append the new name at the end of the script, changing when it runs.

**Until a handler is listed** it still runs, after every listed handler of its script and in discovery order, and the server logs it once at boot under the `Server` log group ("Script handler order: N handler(s) are not listed"). A listed handler whose method no longer exists is ignored and logged the same way ("match no handler"). Handlers in the engine test assembly are never listed; they run after the game's and are summarised in one line.

**Deployment.** The manifest is an embedded resource of `SWLOR.Game.Server.dll` (`ScriptHandlerOrder.txt`), so every way of deploying the server ships it with the code it orders: `SWLOR.CLI` `DeployBuild` and the production deploy copy the whole build output into the `dotnet/` folder that the Docker compose files mount. A missing, unreadable, malformed or empty manifest stops the boot with an exception that names the problem; the server never falls back to reflection order. The embedded copy is what runs, so rebuild after editing the file.

## Core Best Practices

### 1. Error Handling

```csharp
// Proper error handling in core systems
public static void SafeApplyEffect(uint target, Effect effect)
{
    try
    {
        if (GetIsObjectValid(target))
        {
            ApplyEffectToObject(DurationType.Instant, effect, target);
        }
    }
    catch (Exception ex)
    {
        Log.Write(LogGroup.Error, $"Failed to apply effect: {ex.Message}");
    }
}
```

### 2. Performance Optimization

```csharp
// Cache frequently used values
private static readonly Dictionary<uint, Area> _areaCache = new();

public static Area GetCachedArea(uint object)
{
    var area = GetArea(object);
    if (!_areaCache.ContainsKey(area))
    {
        _areaCache[area] = area;
    }
    return _areaCache[area];
}
```

### 3. Thread Safety

```csharp
// Thread-safe operations
private static readonly object _lock = new object();

public static void ThreadSafeOperation()
{
    lock (_lock)
    {
        // Perform thread-safe operation
    }
}
```

## Core Extensions

### 1. Adding New Core Systems

To add a new core system:

1. Create system directory in `Core/`
2. Implement system interface
3. Add to CoreGameManager initialization
4. Document system usage

### 2. Extending Existing Systems

To extend existing systems:

1. Add new methods to existing classes
2. Follow existing naming conventions
3. Add appropriate error handling
4. Update documentation

### 3. Plugin Development

To develop new plugins:

1. Implement IPlugin interface
2. Add plugin to initialization
3. Handle plugin lifecycle
4. Provide plugin documentation

## Core Testing

### 1. Unit Testing

```csharp
[Test]
public void TestCoreFunctionality()
{
    // Test core system
    var result = CoreSystem.DoSomething();

    Assert.IsTrue(result);
}
```

### 2. Integration Testing

```csharp
[Test]
public void TestCoreIntegration()
{
    // Test core integration with services
    var service = new MyService();
    var result = service.UseCoreFunctionality();

    Assert.IsTrue(result);
}
```

### 3. Performance Testing

```csharp
[Test]
public void TestCorePerformance()
{
    var stopwatch = Stopwatch.StartNew();

    // Perform core operation
    CoreSystem.PerformOperation();

    stopwatch.Stop();
    Assert.Less(stopwatch.ElapsedMilliseconds, 100);
}
```

## Core Maintenance

### 1. System Updates

When updating core systems:

1. Maintain backward compatibility
2. Update documentation
3. Test thoroughly
4. Notify dependent systems

### 2. Plugin Updates

When updating plugins:

1. Check plugin compatibility
2. Update plugin interfaces
3. Test plugin functionality
4. Update plugin documentation

### 3. Performance Monitoring

Monitor core system performance:

1. Track execution times
2. Monitor memory usage
3. Check for bottlenecks
4. Optimize as needed

This documentation provides a comprehensive overview of the Core systems in SWLOR.Game.Server, covering the main systems, patterns, and best practices for working with core functionality.
