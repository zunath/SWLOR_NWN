# AI Agent Rules for SWLOR Codebase

> **⚠️ CRITICAL: AI AGENTS MUST READ THIS FILE BEFORE MAKING ANY CHANGES ⚠️**
> 
> This file contains MANDATORY rules that prevent architectural violations and maintain code quality.
> **FAILURE TO FOLLOW THESE RULES WILL RESULT IN BROKEN CODE AND ARCHITECTURAL VIOLATIONS.**

## 🚨 QUICK REFERENCE - MOST CRITICAL RULES 🚨
1. **NEVER** let shared projects reference component projects
2. **NEVER** let component projects reference other component projects
3. **NEVER** let shared/component projects reference test projects
4. **NEVER** use regular C# async/await (use SWLOR.Shared.Core.Async only)
5. **NEVER** hardcode data values (use config/database/enums)
6. **NEVER** modify TestBase (unit testing infrastructure)
7. **ALWAYS** separate event handlers from services
8. **ALWAYS** register services as singletons
9. **ALWAYS** use NSubstitute + NUnit for testing
10. **ALWAYS** inherit TestBase and call InitializeMockNWScript() in unit tests

## Overview
This document provides comprehensive rules and guidelines for AI agents working on the Star Wars: Legends of the Old Republic (SWLOR) codebase. These rules are designed to prevent common mistakes and ensure consistency with the established architecture patterns.

## Project Context
- **Game**: Neverwinter Nights: Enhanced Edition
- **Language**: C# (.NET 8)
- **Architecture**: Component-based with shared domain boundaries
- **Purpose**: Server-side replacement for NWScript using NWNX_DotNet plugin
- **Status**: All NWNX plugin services have been refactored from static classes to dependency-injected services

## Core Architecture Principles

### 1. Component Independence
- **RULE**: Components MUST NOT directly reference other components' implementation details
- **RULE**: Cross-component communication MUST happen through well-defined interfaces in shared projects
- **RULE**: Each component owns its specific business logic and entities
- **VIOLATION EXAMPLE**: `using SWLOR.Component.Inventory.Service;` in a Perk component
- **CORRECT APPROACH**: Use shared interfaces like `IItemService` from `SWLOR.Shared.Domain`

### 2. Shared Project Hierarchy
```
SWLOR.Shared.Abstractions    → Technical infrastructure abstractions
SWLOR.Shared.Core           → Core infrastructure services and utilities  
SWLOR.Shared.Domain         → Shared business domain models and contracts
SWLOR.Shared.Events         → Event system infrastructure
SWLOR.Shared.UI             → UI infrastructure and components
SWLOR.Shared.Dialog         → Dialog system infrastructure
SWLOR.Shared.Caching        → Caching infrastructure
```

### 3. CRITICAL DEPENDENCY RULES - NEVER VIOLATE
- **RULE #1**: Shared projects MUST NEVER reference Component projects
- **RULE #2**: Component projects MUST NEVER reference other Component projects  
- **RULE #3**: Shared and Component projects MUST NEVER reference Test projects
- **VIOLATION CONSEQUENCE**: These violations break the entire architectural foundation
- **ENFORCEMENT**: These rules are NON-NEGOTIABLE and must be checked before any commit

#### Examples of VIOLATIONS (DO NOT DO):
```csharp
// ❌ VIOLATION: Shared project referencing component
// In SWLOR.Shared.Domain/SomeService.cs
using SWLOR.Component.Perk.Service; // FORBIDDEN!

// ❌ VIOLATION: Component referencing another component  
// In SWLOR.Component.Inventory/SomeService.cs
using SWLOR.Component.Perk.Service; // FORBIDDEN!

// ❌ VIOLATION: Project referencing test project
// In SWLOR.Component.Perk/SomeService.cs
using SWLOR.Test.Component.Perk; // FORBIDDEN!
```

#### Examples of CORRECT APPROACH:
```csharp
// ✅ CORRECT: Component using shared interfaces
// In SWLOR.Component.Perk/SomeService.cs
using SWLOR.Shared.Domain.Inventory.Contracts; // CORRECT!

// ✅ CORRECT: Shared project using other shared projects
// In SWLOR.Shared.Domain/SomeService.cs
using SWLOR.Shared.Core.Contracts; // CORRECT!

// ✅ CORRECT: Test project using component (this is allowed)
// In SWLOR.Test.Component.Perk/SomeTest.cs
using SWLOR.Component.Perk.Service; // CORRECT!
```

### 4. Entity Placement Rules
- **RULE**: Entities used by 2+ components MUST be in `SWLOR.Shared.Domain`
- **RULE**: Entities used by only 1 component MUST be in that component's `Entity/` folder
- **RULE**: Never move entities to shared domain unless multiple components actually use them
- **EXAMPLES**:
  - `Player.cs` → `SWLOR.Shared.Domain` (used by almost all components)
  - `AreaNote.cs` → `SWLOR.Component.World/Entity/` (only World component uses it)

## Service Architecture Rules

### 5. Service Registration
- **RULE**: All services MUST be registered as singletons unless there's a specific reason for shorter lifetime
- **RULE**: Event handlers MUST be registered as singletons
- **RULE**: Builder pattern classes MAY be registered as transient
- **RULE**: Use dependency injection for all service dependencies
- **RULE**: Prefer using `RegisterInterfaceImplementations` in DI registrations when many implementations of a given interface must be added to the container
- **CORRECT PATTERN**:
```csharp
services.AddSingleton<IPerkService, PerkService>();
services.AddSingleton<PerkEventHandler>();
services.AddTransient<IPerkBuilder, PerkBuilder>(); // Builder pattern

// When registering multiple implementations of an interface:
services.RegisterInterfaceImplementations<ISomeInterface>(ServiceLifetime.Singleton);
```

### 6. Event Handler Separation
- **RULE**: Event handlers MUST be separate classes from services
- **RULE**: Event handlers MUST be thin - they only receive events and call business logic methods
- **RULE**: Event handlers MUST NOT contain business logic
- **RULE**: Services MUST NOT contain `[ScriptHandler<>]` attributes
- **RULE**: NEVER create wrapper methods in services just for event handlers
- **CORRECT PATTERN**:
```csharp
// Event Handler - Thin infrastructure layer
[ScriptHandler<OnModuleCacheBefore>]
public void CacheData()
{
    _perkService.CacheData(); // Direct call to business logic
}

// Service - Pure business logic
public void CacheData()
{
    _perkCacheService.CacheData();
    // Business logic implementation
}
```

### 6.1 NWNX Plugin Service Architecture - CRITICAL FOR AI AGENTS

#### 6.1.1 Dependency Injection Pattern
**IMPORTANT**: All NWNX plugin services have been refactored from static classes to dependency-injected services.

- **RULE**: NEVER use static plugin classes (e.g., `AdministrationPlugin`, `AreaPlugin`, etc.)
- **RULE**: ALWAYS inject plugin services through constructor injection
- **RULE**: Use the service interfaces (e.g., `IAdministrationPluginService`, `IAreaPluginService`)
- **RULE**: All plugin services are registered in `ServiceRegistration.cs` under `AddAPIServices()`

#### 6.1.2 Service Registration
All NWNX plugin services are registered as singletons in `ServiceRegistration.cs`:
```csharp
private static void AddAPIServices(IServiceCollection services)
{
    // Register NWNX Plugin Services
    services.AddSingleton<IAdministrationPluginService, AdministrationPluginService>();
    services.AddSingleton<IAreaPluginService, AreaPluginService>();
    services.AddSingleton<IChatPluginService, ChatPluginService>();
    services.AddSingleton<ICreaturePluginService, CreaturePluginService>();
    services.AddSingleton<IEventsPluginService, EventsPluginService>();
    services.AddSingleton<IFeatPluginService, FeatPluginService>();
    services.AddSingleton<IFeedbackPluginService, FeedbackPluginService>();
    services.AddSingleton<IItemPluginService, ItemPluginService>();
    services.AddSingleton<IItemPropertyPluginService, ItemPropertyPluginService>();
    services.AddSingleton<IObjectPluginService, ObjectPluginService>();
    services.AddSingleton<IPlayerPluginService, PlayerPluginService>();
    services.AddSingleton<IProfilerPluginService, ProfilerPluginService>();
    services.AddSingleton<IUtilPluginService, UtilPluginService>();
    services.AddSingleton<IVisibilityPluginService, VisibilityPluginService>();
    services.AddSingleton<IWeaponPluginService, WeaponPluginService>();
}
```

#### 6.1.3 Correct Usage Pattern
**✅ CORRECT**: Inject plugin services through constructor:
```csharp
public class MyService
{
    private readonly IAdministrationPluginService _administrationPlugin;
    private readonly IAreaPluginService _areaPlugin;
    
    public MyService(
        IAdministrationPluginService administrationPlugin,
        IAreaPluginService areaPlugin)
    {
        _administrationPlugin = administrationPlugin;
        _areaPlugin = areaPlugin;
    }
    
    public void DoSomething()
    {
        // Use injected services
        _administrationPlugin.BanPlayer(player, "Reason");
        _areaPlugin.SetAreaTransitionTarget(area, targetArea);
    }
}
```

**❌ FORBIDDEN**: Using static plugin classes:
```csharp
// DON'T DO THIS - Static classes no longer exist
AdministrationPlugin.BanPlayer(player, "Reason"); // WRONG!
AreaPlugin.SetAreaTransitionTarget(area, targetArea); // WRONG!
```

#### 6.1.4 Lazy Loading for Circular Dependencies
When circular dependencies exist, use lazy loading:
```csharp
public class MyViewModel
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Lazy<IItemPluginService> _itemPlugin;
    
    public MyViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _itemPlugin = new Lazy<IItemPluginService>(() => 
            _serviceProvider.GetRequiredService<IItemPluginService>());
    }
    
    public void DoSomething()
    {
        // Use lazy-loaded service
        _itemPlugin.Value.CreateItem(itemType, 1);
    }
}
```

#### 6.1.5 Static Context Resolution
For static methods that cannot use constructor injection, use `ServiceContainer`:
```csharp
public static class StaticHelper
{
    public static void DoSomething()
    {
        var profilerPlugin = ServiceContainer.GetService<IProfilerPluginService>();
        profilerPlugin.PushPerfScope("MyScope");
        // ... do work
        profilerPlugin.PopPerfScope();
    }
}
```

### 7. Interface Design
- **RULE**: All services MUST have corresponding interfaces
- **RULE**: Interfaces MUST be in the `Contracts/` folder of the component
- **RULE**: Cross-component service interfaces MUST be in `SWLOR.Shared.Domain/Contracts/`
- **RULE**: Interface names MUST start with 'I' (e.g., `IPerkService`)
- **RULE**: Interface methods MUST NOT include event handler wrapper methods

## File Organization Rules

### 8. Component Structure
Each component MUST follow this structure:
```
SWLOR.Component.[Domain]/
├── Contracts/           → Service and repository interfaces
├── Entity/             → Component-specific entities
├── Models/             → Data models and view models
├── Service/            → Business logic implementations
├── EventHandlers/      → Event handling classes
├── Feature/            → Feature-specific implementations
├── UI/                 → UI components
├── Dialog/             → Dialog implementations
└── Infrastructure/     → DI registration extensions
```

### 9. Namespace Conventions
- **RULE**: Use consistent namespace patterns
- **RULE**: Component namespaces: `SWLOR.Component.[Domain].[Folder]`
- **RULE**: Shared namespaces: `SWLOR.Shared.[Project].[Folder]`
- **RULE**: Never use cross-component namespaces in using statements
- **CORRECT EXAMPLES**:
```csharp
using SWLOR.Component.Perk.Contracts;
using SWLOR.Shared.Domain.Perk.Contracts;
using SWLOR.Shared.Events.Attributes;
```

### 10. Using Statements Rules
- **RULE**: Add using statements for ALL types used in the file (interfaces, classes, enums, etc.)
- **RULE**: ALWAYS check for compilation errors and add missing using statements immediately
- **RULE**: Place using statements at the top of the file, before namespace declaration
- **RULE**: Group using statements in this order:
  1. System namespaces (e.g., `using System;`, `using System.Collections.Generic;`)
  2. Third-party namespaces (e.g., `using Microsoft.Extensions.DependencyInjection;`)
  3. SWLOR namespaces (e.g., `using SWLOR.Shared.Core;`)
- **RULE**: Remove unused using statements to keep code clean
- **RULE**: Use fully qualified names only when there are naming conflicts
- **RULE**: NWScript is available globally - DO NOT add `using SWLOR.NWN.API.Service;` for NWScript calls
- **VIOLATION EXAMPLES**:
```csharp
// ❌ FORBIDDEN: Missing using statements
public class PerkService
{
    public List<PerkType> GetPerks() // List<> needs using System.Collections.Generic;
    {
        return new List<PerkType>();
    }
}

// ❌ FORBIDDEN: Missing interface using statement (COMPILATION ERROR)
public class PlayerStatService : IPlayerStatService // IPlayerStatService needs using SWLOR.Component.Character.Contracts;
{
    // This will cause: "The type or namespace name 'IPlayerStatService' could not be found"
}

// ❌ FORBIDDEN: Using statements in wrong order
using SWLOR.Shared.Core;
using System; // Should be before SWLOR namespaces

// ❌ FORBIDDEN: Adding unnecessary NWScript using statement
using SWLOR.NWN.API.Service; // DON'T DO THIS - NWScript is global
public class MyService
{
    public void DoSomething()
    {
        var area = NWScript.CreateArea("", "", "Test"); // Works without the using
    }
}
```
- **CORRECT APPROACH**:
```csharp
// ✅ CORRECT: Proper using statement organization
using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Perk.Contracts;
using SWLOR.Shared.Core;
using SWLOR.Shared.Domain.Perk.Contracts;
// Note: NWScript is available globally - no using statement needed

namespace SWLOR.Component.Perk.Service
{
    public class PerkService
    {
        public List<PerkType> GetPerks()
        {
            return new List<PerkType>();
        }
        
        public void ProcessArea()
        {
            // NWScript works without any using statement
            var area = NWScript.CreateArea("", "", "Test Area");
        }
    }
}
```

## Code Quality Rules

### 11. Dependency Direction
- **RULE**: Components depend on shared projects, NOT on each other
- **RULE**: Shared projects MUST NOT depend on components
- **RULE**: Infrastructure supports domain, does NOT drive it
- **RULE**: Always inject dependencies through constructor injection

### 12. Error Handling
- **RULE**: Use proper exception handling with meaningful messages
- **RULE**: Log errors using the shared logging infrastructure
- **RULE**: Never swallow exceptions silently
- **RULE**: Validate inputs at service boundaries

### 13. Performance Considerations
- **RULE**: Use caching for expensive operations (perk calculations, database queries)
- **RULE**: Avoid expensive operations in event handlers when possible
- **RULE**: Consider performance impact of singleton vs transient registrations

### 14. Async/Await Restrictions
- **RULE**: Regular C# async/await is NOT allowed in this codebase
- **RULE**: Async operations MUST ONLY be used through the Async folder in SWLOR.Shared.Core
- **RULE**: Use the established async infrastructure instead of standard .NET async patterns
- **VIOLATION EXAMPLE**: `public async Task<string> GetDataAsync()`
- **CORRECT APPROACH**: Use async utilities from `SWLOR.Shared.Core.Async`

### 15. Data Hardcoding Restrictions
- **RULE**: NO hardcoded data values in business logic or service implementations
- **RULE**: Data values MUST be stored in database, configuration, or constants files
- **ALLOWED EXCEPTIONS**: 
  - Enum values and their attributes
  - Magic numbers that are truly constant (like array indices, mathematical constants)
  - Default values in constructors or optional parameters
- **VIOLATION EXAMPLES**:
  ```csharp
  // ❌ FORBIDDEN: Hardcoded business values
  if (player.Level > 50) // Should be in config
  var maxItems = 25; // Should be in database/config
  var skillId = 15; // Should be enum or database lookup
  ```
- **CORRECT APPROACH**:
  ```csharp
  // ✅ CORRECT: Using configuration/constants
  if (player.Level > _config.MaxPlayerLevel)
  var maxItems = _config.MaxInventoryItems;
  var skillId = SkillType.OneHanded; // Using enum
  ```

## Anti-Patterns to AVOID

### 16. Forbidden Patterns
- **NEVER**: Create circular dependencies between components
- **NEVER**: Put business logic in event handlers
- **NEVER**: Create wrapper methods in services for event handlers
- **NEVER**: Mix event handling and business logic in the same class
- **NEVER**: Reference other components directly
- **NEVER**: Put all entities in shared domain regardless of usage
- **NEVER**: Register services with inappropriate lifetimes
- **NEVER**: Create "God Components" that try to do everything
- **NEVER**: Use regular C# async/await patterns
- **NEVER**: Hardcode data values in business logic (use config/database instead)

### 17. Common Mistakes to Avoid
- **MISTAKE**: `using SWLOR.Component.Inventory.Service;` in Perk component
- **CORRECT**: `using SWLOR.Shared.Domain.Inventory.Contracts;`
- **MISTAKE**: Putting `[ScriptHandler<>]` in service classes
- **CORRECT**: Separate event handler classes
- **MISTAKE**: Creating `OnModuleLoad()` wrapper in service
- **CORRECT**: Event handler calls `_service.LoadData()` directly
- **MISTAKE**: `public async Task<string> GetDataAsync()`
- **CORRECT**: Use async utilities from `SWLOR.Shared.Core.Async`
- **MISTAKE**: `if (player.Level > 50)` (hardcoded value)
- **CORRECT**: `if (player.Level > _config.MaxPlayerLevel)` (using config)
- **MISTAKE**: Missing using statements causing compilation errors
- **CORRECT**: Add all required using statements at the top of the file
- **MISTAKE**: Using Moq or other mocking frameworks instead of NSubstitute
- **CORRECT**: Use NSubstitute for all mocking in unit tests
- **MISTAKE**: Using MSTest or xUnit instead of NUnit
- **CORRECT**: Use NUnit for all unit testing

## Testing Rules

### 18. Testability Requirements
- **RULE**: Services MUST be testable without event infrastructure
- **RULE**: Use dependency injection for all external dependencies
- **RULE**: Mock interfaces, not concrete classes
- **RULE**: Write unit tests for business logic, integration tests for event handling
- **RULE**: Use NSubstitute for mocking and NUnit for unit testing framework
- **RULE**: All unit tests MUST be in corresponding Test projects (e.g., `SWLOR.Test.Component.Perk`)

### 19. Testing Framework Architecture - CRITICAL FOR AI AGENTS

#### 19.1 TestBase Class - Automatic Mocking System
The `TestBase` class provides a **COMPLETE AUTOMATIC MOCKING SYSTEM** for all NWScript and NWNX plugin services. This is the **ONLY** way to handle mocking in tests.

**KEY POINTS FOR AI AGENTS:**
- **TestBase.InitializeMockNWScript()** automatically replaces ALL NWScript and NWNX services with mock implementations
- **NO MANUAL MOCKING** of NWScript or NWNX services is needed or allowed
- **NO GetMockService() calls** should be made in tests
- **NO manual mock creation** for NWScript or NWNX services

#### 19.2 How the Mocking System Works
```csharp
[TestFixture]
public class MyServiceTests : TestBase
{
    [SetUp]
    public void SetUp()
    {
        // This ONE call sets up ALL NWScript and NWNX mocks automatically
        InitializeMockNWScript();
        
        // Only mock OTHER services (database, cache, etc.) with NSubstitute
        _mockDatabaseService = Substitute.For<IDatabaseService>();
        _service = new MyService(_mockDatabaseService);
    }
}
```

#### 19.3 Using NWScript in Tests - CORRECT PATTERN
**✅ CORRECT**: Use direct NWScript static calls - the mocking is handled automatically:
```csharp
[Test]
public void MyTest()
{
    // Arrange - Use NWScript directly, mocks handle the rest
    var area = NWScript.CreateArea("", "", "Test Area");
    NWScript.SetName(area, "Test Area");
    NWScript.SetLocalInt(area, "SOME_VAR", 123);
    
    // Act
    var result = _service.DoSomething(area);
    
    // Assert - Use NWScript directly to verify
    var storedValue = NWScript.GetLocalInt(area, "SOME_VAR");
    Assert.That(storedValue, Is.EqualTo(123));
}
```

#### 19.4 What TestBase.InitializeMockNWScript() Does
This method automatically:
1. **Replaces NWScript service** with `NWScriptServiceMock`
2. **Replaces ALL NWNX plugin services** with their mock implementations:
   - `AdministrationPlugin` → `AdministrationPluginMock`
   - `AreaPlugin` → `AreaPluginMock`
   - `ChatPlugin` → `ChatPluginMock`
   - `CreaturePlugin` → `CreaturePluginMock`
   - `EventsPlugin` → `EventsPluginMock`
   - `FeatPlugin` → `FeatPluginMock`
   - `FeedbackPlugin` → `FeedbackPluginMock`
   - `ItemPlugin` → `ItemPluginMock`
   - `ItemPropertyPlugin` → `ItemPropertyPluginMock`
   - `ObjectPlugin` → `ObjectPluginMock`
   - `PlayerPlugin` → `PlayerPluginMock`
   - `ProfilerPlugin` → `ProfilerPluginMock`
   - `UtilPlugin` → `UtilPluginMock`
   - `VisibilityPlugin` → `VisibilityPluginMock`
   - `WeaponPlugin` → `WeaponPluginMock`

**IMPORTANT**: All NWNX plugin services are now registered as dependency-injected services in `ServiceRegistration.cs` under the `AddAPIServices()` method. The static wrapper classes have been removed and replaced with proper service interfaces and implementations.

#### 19.5 Common AI Agent Mistakes - DO NOT DO THESE
**❌ WRONG**: Trying to mock NWScript manually
```csharp
// DON'T DO THIS
var mockNWScript = Substitute.For<INWScriptService>();
NWScript.SetService(mockNWScript); // WRONG!
```

**❌ WRONG**: Calling GetMockService() in tests
```csharp
// DON'T DO THIS
var mockService = GetMockService(); // WRONG!
```

**❌ WRONG**: Creating manual NWScript mocks
```csharp
// DON'T DO THIS
var mockNWScript = new NWScriptServiceMock(); // WRONG!
```

**❌ WRONG**: Trying to setup NWScript return values
```csharp
// DON'T DO THIS
NWScript.GetFirstArea().Returns(1u); // WRONG! This won't work
```

**❌ WRONG**: Modifying TestBase class or adding methods to it
```csharp
// DON'T DO THIS - Never modify TestBase.cs
public class TestBase
{
    // DON'T ADD METHODS LIKE THIS
    protected static NWScriptServiceMock GetMockService() { ... } // WRONG!
    protected static void SetupMockData() { ... } // WRONG!
    protected static void ResetMocks() { ... } // WRONG!
}
```

**❌ WRONG**: Adding helper methods to TestBase for mock access
```csharp
// DON'T DO THIS - Never add these to TestBase
protected static void SetMockReturnValue(string method, object value) { ... } // WRONG!
protected static void VerifyMockCall(string method) { ... } // WRONG!
protected static T GetMockData<T>(string key) { ... } // WRONG!
```

#### 19.6 What You CAN Mock with NSubstitute
**✅ CORRECT**: Mock other services (database, cache, etc.) and NWNX plugin services
```csharp
[SetUp]
public void SetUp()
{
    InitializeMockNWScript(); // Handles NWScript automatically
    
    // Mock OTHER services with NSubstitute
    _mockDatabaseService = Substitute.For<IDatabaseService>();
    _mockCacheService = Substitute.For<ICacheService>();
    _mockConfigService = Substitute.For<IConfigService>();
    
    // Mock NWNX plugin services with NSubstitute
    _mockAdministrationPlugin = Substitute.For<IAdministrationPluginService>();
    _mockAreaPlugin = Substitute.For<IAreaPluginService>();
    
    _service = new MyService(
        _mockDatabaseService, 
        _mockCacheService, 
        _mockConfigService,
        _mockAdministrationPlugin,
        _mockAreaPlugin);
}
```

**IMPORTANT**: For tests that use NWNX plugin services, you MUST mock them with NSubstitute since the static classes no longer exist. The `InitializeMockNWScript()` method only handles NWScript mocking, not NWNX plugin services.

#### 19.7 Test Isolation and Cleanup
- **TestBase automatically resets mock state** between tests
- **No manual cleanup** of NWScript/NWNX mocks needed
- **Each test gets a fresh mock state** automatically
- **Mock data persists within a single test** but is reset between tests

#### 19.8 Mock Data Verification
The mock implementations store data that can be verified:
```csharp
[Test]
public void VerifyMockData()
{
    // Arrange
    var area = NWScript.CreateArea("", "", "Test Area");
    NWScript.SetName(area, "Test Area");
    
    // Act
    _service.ProcessArea(area);
    
    // Assert - The mock stores the data, so you can verify it
    var areaName = NWScript.GetName(area);
    Assert.That(areaName, Is.EqualTo("Test Area"));
}
```

### 20. Testing Best Practices
- **RULE**: Always inherit from `TestBase` for any test that uses NWScript or NWNX
- **RULE**: Call `InitializeMockNWScript()` in `[SetUp]` method
- **RULE**: Use direct NWScript static calls in tests - no manual mocking
- **RULE**: Mock only non-NWScript services with NSubstitute
- **RULE**: Test business logic, not NWScript functionality
- **RULE**: Verify mock state through NWScript calls, not direct mock access
- **RULE**: NEVER modify the `TestBase` class or add methods to it
- **RULE**: NEVER add helper methods like `GetMockService()` to TestBase
- **RULE**: TestBase is a complete, final implementation - do not extend it

### 21. Unit Testing Infrastructure Rules - CRITICAL FOR AI AGENTS

#### 21.1 NWScript Global Availability
- **RULE**: NWScript has a `GlobalUsings.cs` file which automatically imports NWScript to all other files
- **RULE**: NWScript is available globally in all test files - DO NOT add `using SWLOR.NWN.API.Service;` statements
- **RULE**: Direct NWScript static calls work in tests without any using statements or manual setup
- **CORRECT PATTERN**:
```csharp
[TestFixture]
public class MyServiceTests : TestBase
{
    [Test]
    public void MyTest()
    {
        // NWScript is globally available - no using statement needed
        var area = NWScript.CreateArea("", "", "Test Area");
        var player = NWScript.GetFirstPC();
        NWScript.SetLocalInt(area, "TEST_VAR", 42);
    }
}
```

#### 21.2 TestBase Inheritance Requirement
- **RULE**: Every unit test file MUST inherit from `TestBase`
- **RULE**: All unit tests that use NWScript or NWNX plugin services MUST inherit from `TestBase`
- **VIOLATION EXAMPLE**:
```csharp
// ❌ FORBIDDEN: Not inheriting from TestBase
[TestFixture]
public class MyServiceTests // WRONG!
{
    // This will cause NWScript calls to fail
}
```
- **CORRECT APPROACH**:
```csharp
// ✅ CORRECT: Inheriting from TestBase
[TestFixture]
public class MyServiceTests : TestBase // CORRECT!
{
    [SetUp]
    public void SetUp()
    {
        InitializeMockNWScript();
    }
}
```

#### 21.3 InitializeMockNWScript() Setup Requirement
- **RULE**: There MUST be a call to `InitializeMockNWScript()` in the `[SetUp]` method of each test file
- **RULE**: This call MUST be the first line in the `[SetUp]` method
- **RULE**: This method automatically sets up ALL NWScript and NWNX plugin mocking
- **VIOLATION EXAMPLE**:
```csharp
// ❌ FORBIDDEN: Missing InitializeMockNWScript() call
[TestFixture]
public class MyServiceTests : TestBase
{
    [SetUp]
    public void SetUp()
    {
        // Missing InitializeMockNWScript() call!
        // NWScript calls will fail
        _mockService = Substitute.For<IMyService>();
    }
}
```
- **CORRECT APPROACH**:
```csharp
// ✅ CORRECT: InitializeMockNWScript() called first in SetUp
[TestFixture]
public class MyServiceTests : TestBase
{
    private IMyService _mockService;

    [SetUp]
    public void SetUp()
    {
        // MUST be called first in SetUp method
        InitializeMockNWScript();

        // Then setup other mocks
        _mockService = Substitute.For<IMyService>();
    }
}
```

#### 21.4 TestBase Modification Restrictions
- **RULE**: TestBase MUST NOT be modified by anyone
- **RULE**: NO methods or properties should be added to TestBase
- **RULE**: NO modifications to TestBase.cs are allowed
- **RULE**: TestBase is a complete, sealed implementation
- **VIOLATION EXAMPLES**:
```csharp
// ❌ FORBIDDEN: Adding methods to TestBase
public class TestBase
{
    // DON'T ADD THESE!
    protected static NWScriptServiceMock GetMockService() { ... }
    protected static void SetupMockData() { ... }
    protected static void ResetMocks() { ... }
    protected static void SetMockReturnValue(string method, object value) { ... }
    protected static void VerifyMockCall(string method) { ... }
    protected static T GetMockData<T>(string key) { ... }
}
```

#### 21.5 NWScript.cs Modification Restrictions
- **RULE**: NOTHING should be added directly to NWScript.cs
- **RULE**: NWScript.cs is a generated file that should not be modified manually
- **RULE**: All NWScript extensions should be handled through proper service layers
- **VIOLATION EXAMPLE**:
```csharp
// ❌ FORBIDDEN: Adding methods to NWScript.cs
public static class NWScript
{
    // DON'T ADD METHODS HERE!
    public static void MyCustomMethod(uint obj) { ... }
}
```
- **CORRECT APPROACH**: Create extension methods in appropriate service layers or use the existing NWScript API.

#### 21.6 Reflection Method Access Restrictions in Unit Tests
- **RULE**: Using reflection to get methods is NOT allowed in unit tests
- **RULE**: Direct method calls through reflection (e.g., `GetMethod()`, `Invoke()`) MUST NOT be used in tests
- **RULE**: Test business logic through public APIs, not by bypassing encapsulation with reflection
- **VIOLATION EXAMPLE**:
```csharp
// ❌ FORBIDDEN: Using reflection to access methods in tests
[Test]
public void TestUsingReflection()
{
    var service = new MyService();
    var method = typeof(MyService).GetMethod("PrivateMethod", BindingFlags.NonPublic | BindingFlags.Instance);
    var result = method.Invoke(service, new object[] { "param" });
    // This violates encapsulation and makes tests brittle
}
```
- **CORRECT APPROACH**:
```csharp
// ✅ CORRECT: Test through public APIs
[Test]
public void TestThroughPublicAPI()
{
    var service = new MyService();
    var result = service.PublicMethod("param"); // Test the public interface
    Assert.That(result, Is.EqualTo(expectedValue));
}
```

## Documentation Rules

### 22. Code Documentation
- **RULE**: All public methods MUST have XML documentation
- **RULE**: Include parameter descriptions and return value descriptions
- **RULE**: Document complex business logic with inline comments
- **RULE**: Update documentation when changing method signatures
- **RULE**: Method comments should be placed on the interface whenever possible
- **RULE**: Concrete implementations should use the `<inheritdoc/>` tag so that comments are not duplicated

## Migration and Refactoring Rules

### 23. When Adding New Features
- **RULE**: Start with component-specific implementation
- **RULE**: Only move to shared domain when second component needs it
- **RULE**: Create interfaces before implementations
- **RULE**: Register services in appropriate DI container
- **RULE**: Separate event handling from business logic from the start

### 24. When Modifying Existing Code
- **RULE**: Maintain existing architecture patterns
- **RULE**: Don't break component boundaries
- **RULE**: Update all references when moving entities
- **RULE**: Test thoroughly after architectural changes

## Specific Component Rules

### 25. Perk Component Specifics
- **RULE**: Perk services are split into focused services (Data, Level, Trigger, Cache)
- **RULE**: Main PerkService acts as a facade
- **RULE**: Use caching for expensive perk calculations
- **RULE**: Perk requirements are handled by dedicated factory

### 26. Event System Rules
- **RULE**: Use `[ScriptHandler<>]` attributes for event registration
- **RULE**: Event handlers MUST be in `EventHandlers/` folder
- **RULE**: One event handler class per component
- **RULE**: Event handlers MUST be registered as singletons

## Validation Checklist

Before submitting any changes, verify:
- [ ] **CRITICAL**: No shared projects reference component projects
- [ ] **CRITICAL**: No component projects reference other component projects
- [ ] **CRITICAL**: No shared/component projects reference test projects
- [ ] **CRITICAL**: No static plugin class usage (use dependency injection instead)
- [ ] No cross-component dependencies
- [ ] No regular C# async/await usage (use SWLOR.Shared.Core.Async only)
- [ ] No hardcoded data values (use config/database/enums instead)
- [ ] All using statements added correctly and in proper order
- [ ] Unit tests use NSubstitute for mocking and NUnit for testing framework
- [ ] All unit test files inherit from TestBase and call InitializeMockNWScript() in [SetUp]
- [ ] TestBase class is never modified or extended
- [ ] Nothing is added directly to NWScript.cs (use proper service layers instead)
- [ ] No reflection used to access methods in unit tests (test through public APIs)
- [ ] Services registered as singletons (unless transient needed)
- [ ] Event handlers separated from services
- [ ] No wrapper methods in services for event handlers
- [ ] Proper namespace usage
- [ ] Entities in correct location (shared vs component-specific)
- [ ] All public methods documented
- [ ] Dependencies injected through constructor
- [ ] No circular dependencies
- [ ] Performance considerations addressed
- [ ] NWNX plugin services injected through constructor (not static calls)
- [ ] Plugin services properly mocked in unit tests with NSubstitute

## Emergency Override Rules

In exceptional circumstances where these rules cannot be followed:
1. Document the reason for the exception
2. Add TODO comments for future refactoring
3. Ensure the change doesn't break existing architecture
4. Plan to address the technical debt in the next sprint

## Conclusion

These rules are designed to maintain the architectural integrity of the SWLOR codebase while enabling productive development. When in doubt, follow the established patterns in the existing codebase and prioritize maintainability over quick fixes.

Remember: The goal is to create a modular, testable, and maintainable codebase that supports independent component development while maintaining clear domain boundaries.

---

## 📁 Related Files
- **`RULES_SUMMARY.md`** - Quick reference for critical rules
- **`.cursorrules`** - Cursor AI specific rules
- **`README.md`** - Project overview with AI agent warning

## 🔄 How to Ensure AI Agents Read This File

### For Cursor Users:
1. The `.cursorrules` file will automatically load these rules
2. Always mention "Read AI_AGENT_RULES.md" in your prompts
3. The rules are now referenced in README.md

### For Other AI Tools:
1. Always start prompts with: "Before making changes, read AI_AGENT_RULES.md"
2. Reference the RULES_SUMMARY.md for quick checks
3. Use the validation checklist before committing changes

### For Code Reviews:
1. Check that changes follow the architectural rules
2. Verify no cross-component dependencies
3. Ensure proper service registration patterns
4. Validate using statement organization
