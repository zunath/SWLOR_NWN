# SWLOR.Game.Server Core Systems Documentation

This document provides an overview of the Core systems in SWLOR.Game.Server, which provide fundamental functionality and abstractions used throughout the game.

## Overview

The Core layer contains fundamental systems, abstractions, and utilities that provide the foundation for the entire game. These systems handle low-level operations, provide abstractions over the NWN engine, and offer common functionality used by services and features.

## Directory Structure

```
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

**Usage**:
```csharp
// Create NUI window
var window = new NuiWindow("MyWindow", "My Window")
{
    Geometry = new NuiRect(0, 0, 400, 300)
};

// Add elements to window
window.Elements.Add(new NuiLabel("Hello World")
{
    Geometry = new NuiRect(10, 10, 100, 20)
});
```

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