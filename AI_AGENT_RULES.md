# AI Agent Rules for SWLOR Codebase

## Overview
This document provides comprehensive rules and guidelines for AI agents working on the Star Wars: Legends of the Old Republic (SWLOR) codebase. These rules are designed to prevent common mistakes and ensure consistency with the established architecture patterns.

## Project Context
- **Game**: Neverwinter Nights: Enhanced Edition
- **Language**: C# (.NET 8)
- **Architecture**: Component-based with shared domain boundaries
- **Purpose**: Server-side replacement for NWScript using NWNX_DotNet plugin

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
- **CORRECT PATTERN**:
```csharp
services.AddSingleton<IPerkService, PerkService>();
services.AddSingleton<PerkEventHandler>();
services.AddTransient<IPerkBuilder, PerkBuilder>(); // Builder pattern
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

## Code Quality Rules

### 10. Dependency Direction
- **RULE**: Components depend on shared projects, NOT on each other
- **RULE**: Shared projects MUST NOT depend on components
- **RULE**: Infrastructure supports domain, does NOT drive it
- **RULE**: Always inject dependencies through constructor injection

### 11. Error Handling
- **RULE**: Use proper exception handling with meaningful messages
- **RULE**: Log errors using the shared logging infrastructure
- **RULE**: Never swallow exceptions silently
- **RULE**: Validate inputs at service boundaries

### 12. Performance Considerations
- **RULE**: Use caching for expensive operations (perk calculations, database queries)
- **RULE**: Avoid expensive operations in event handlers when possible
- **RULE**: Use async/await patterns for I/O operations
- **RULE**: Consider performance impact of singleton vs transient registrations

## Anti-Patterns to AVOID

### 13. Forbidden Patterns
- **NEVER**: Create circular dependencies between components
- **NEVER**: Put business logic in event handlers
- **NEVER**: Create wrapper methods in services for event handlers
- **NEVER**: Mix event handling and business logic in the same class
- **NEVER**: Reference other components directly
- **NEVER**: Put all entities in shared domain regardless of usage
- **NEVER**: Register services with inappropriate lifetimes
- **NEVER**: Create "God Components" that try to do everything

### 14. Common Mistakes to Avoid
- **MISTAKE**: `using SWLOR.Component.Inventory.Service;` in Perk component
- **CORRECT**: `using SWLOR.Shared.Domain.Inventory.Contracts;`
- **MISTAKE**: Putting `[ScriptHandler<>]` in service classes
- **CORRECT**: Separate event handler classes
- **MISTAKE**: Creating `OnModuleLoad()` wrapper in service
- **CORRECT**: Event handler calls `_service.LoadData()` directly

## Testing Rules

### 15. Testability Requirements
- **RULE**: Services MUST be testable without event infrastructure
- **RULE**: Use dependency injection for all external dependencies
- **RULE**: Mock interfaces, not concrete classes
- **RULE**: Write unit tests for business logic, integration tests for event handling

## Documentation Rules

### 16. Code Documentation
- **RULE**: All public methods MUST have XML documentation
- **RULE**: Include parameter descriptions and return value descriptions
- **RULE**: Document complex business logic with inline comments
- **RULE**: Update documentation when changing method signatures

## Migration and Refactoring Rules

### 17. When Adding New Features
- **RULE**: Start with component-specific implementation
- **RULE**: Only move to shared domain when second component needs it
- **RULE**: Create interfaces before implementations
- **RULE**: Register services in appropriate DI container
- **RULE**: Separate event handling from business logic from the start

### 18. When Modifying Existing Code
- **RULE**: Maintain existing architecture patterns
- **RULE**: Don't break component boundaries
- **RULE**: Update all references when moving entities
- **RULE**: Test thoroughly after architectural changes

## Specific Component Rules

### 19. Perk Component Specifics
- **RULE**: Perk services are split into focused services (Data, Level, Trigger, Cache)
- **RULE**: Main PerkService acts as a facade
- **RULE**: Use caching for expensive perk calculations
- **RULE**: Perk requirements are handled by dedicated factory

### 20. Event System Rules
- **RULE**: Use `[ScriptHandler<>]` attributes for event registration
- **RULE**: Event handlers MUST be in `EventHandlers/` folder
- **RULE**: One event handler class per component
- **RULE**: Event handlers MUST be registered as singletons

## Validation Checklist

Before submitting any changes, verify:
- [ ] **CRITICAL**: No shared projects reference component projects
- [ ] **CRITICAL**: No component projects reference other component projects
- [ ] **CRITICAL**: No shared/component projects reference test projects
- [ ] No cross-component dependencies
- [ ] Services registered as singletons (unless transient needed)
- [ ] Event handlers separated from services
- [ ] No wrapper methods in services for event handlers
- [ ] Proper namespace usage
- [ ] Entities in correct location (shared vs component-specific)
- [ ] All public methods documented
- [ ] Dependencies injected through constructor
- [ ] No circular dependencies
- [ ] Performance considerations addressed

## Emergency Override Rules

In exceptional circumstances where these rules cannot be followed:
1. Document the reason for the exception
2. Add TODO comments for future refactoring
3. Ensure the change doesn't break existing architecture
4. Plan to address the technical debt in the next sprint

## Conclusion

These rules are designed to maintain the architectural integrity of the SWLOR codebase while enabling productive development. When in doubt, follow the established patterns in the existing codebase and prioritize maintainability over quick fixes.

Remember: The goal is to create a modular, testable, and maintainable codebase that supports independent component development while maintaining clear domain boundaries.
