# SWLOR Component Refactoring Plan

## Overview

This document outlines the systematic refactoring plan for the SWLOR codebase to achieve proper component separation and domain boundaries. The goal is to eliminate cross-component dependencies and create a maintainable, modular architecture.

## Core Principles

### 1. Component Independence
- Each component should be self-contained with minimal external dependencies
- Components should not directly reference other components' implementation details
- Cross-component communication should happen through well-defined interfaces

### 2. Domain Separation
- Business logic should be separated from infrastructure concerns
- Domain models should be shared only when truly needed by multiple components
- Component-specific logic should remain within that component

### 3. Dependency Direction
- Components depend on shared projects, not on each other
- Shared projects should not depend on components
- Infrastructure should support domain, not drive it

## Project Structure

### Shared Projects (Infrastructure & Cross-Cutting Concerns)

#### **SWLOR.Shared.Abstractions**
- **Purpose**: Technical infrastructure abstractions
- **Contains**:
  - Base classes (`EntityBase`, `IndexedAttribute`)
  - Infrastructure interfaces (`IDatabaseService`, `ILogger`, `IEventService`)
  - Technical delegates (`ConditionalScriptDelegate`)
  - Core enums (`ServerEnvironmentType`)

#### **SWLOR.Shared.Core**
- **Purpose**: Core infrastructure services and utilities
- **Contains**:
  - Database infrastructure (`DB.cs`, `DBQuery.cs`)
  - Core services (`RandomService`, `Time`, `LogService`)
  - Extension methods
  - Configuration (`AppSettings`)
  - Async/await infrastructure

#### **SWLOR.Shared.Domain**
- **Purpose**: Shared business domain models and contracts
- **Contains**:
  - Cross-component entities (`Player`, `Account`)
  - Shared domain models (used by 2+ components)
  - Domain enums (`ItemType`, `CurrencyType`)
  - Domain contracts/interfaces
  - Value objects

#### **SWLOR.Shared.Events**
- **Purpose**: Event system infrastructure
- **Contains**:
  - Event definitions
  - Event aggregator
  - Event handler attributes
  - Event service implementations

#### **SWLOR.Shared.UI**
- **Purpose**: UI infrastructure and components
- **Contains**:
  - UI component definitions
  - UI service interfaces and implementations
  - UI models and payloads
  - UI-related entities

#### **SWLOR.Shared.Dialog**
- **Purpose**: Dialog system infrastructure
- **Contains**:
  - Dialog service interfaces
  - Dialog models and navigation
  - Dialog service implementations

#### **SWLOR.Shared.Caching**
- **Purpose**: Caching infrastructure
- **Contains**:
  - Cache service interfaces
  - Cache service implementations
  - Cache entities (`ModuleCache`)

### Component Projects (Business Logic)

Each component follows this structure:
```
SWLOR.Component.[Domain]/
├── Contracts/
│   ├── I[Domain]Service.cs
│   └── I[Domain]Repository.cs
├── Entity/
│   ├── [Domain]Entity.cs
│   └── [Domain]Definition.cs
├── Models/
│   ├── [Domain]Model.cs
│   └── [Domain]ViewModel.cs
├── Service/
│   ├── [Domain]Service.cs
│   └── [Domain]Repository.cs
├── Feature/
│   ├── [Domain]Definition/
│   └── [Domain]Progression/
├── UI/
│   ├── View/
│   └── ViewModel/
└── Dialog/
    └── [Domain]Dialog.cs
```

## Entity Placement Rules

### **SWLOR.Shared.Domain** (2+ components use it)
- `Player.cs` - Used by almost all components
- `Account.cs` - Used by Player + Admin components
- `Item.cs` - Used by Inventory + Crafting components
- `SkillModifier.cs` - Used by Skill + Perk components

### **Component-Level** (Only 1 component uses it)
- `AreaNote.cs` → `SWLOR.Component.World/Entity/`
- `InventoryItem.cs` → `SWLOR.Component.Inventory/Entity/`
- `MarketItem.cs` → `SWLOR.Component.Market/Entity/`
- `PlayerBan.cs` → `SWLOR.Component.Admin/Entity/`
- `PlayerNote.cs` → `SWLOR.Component.Player/Entity/`
- `PlayerOutfit.cs` → `SWLOR.Component.Player/Entity/`
- `PlayerShip.cs` → `SWLOR.Component.Space/Entity/`
- `ResearchJob.cs` → `SWLOR.Component.Crafting/Entity/`
- `ServerConfiguration.cs` → `SWLOR.Component.Migration/Entity/`

## Cross-Component Communication

### **Interface-Only Dependencies**
```csharp
// SWLOR.Component.Perk references SWLOR.Component.Skill.Contracts
using SWLOR.Component.Skill.Contracts;

public class PerkValidationService
{
    private readonly ISkillService _skillService;
    // Implementation uses interface, not concrete class
}
```

### **Shared Domain Models**
```csharp
// SWLOR.Shared.Domain/Models/SkillModifier.cs
public class SkillModifier
{
    public int SkillId { get; set; }
    public int ModifierValue { get; set; }
}
```

## Refactoring Steps

### Phase 1: Create SWLOR.Shared.Domain
1. Create new project `SWLOR.Shared.Domain`
2. Move cross-component entities from existing locations
3. Update all references to use new namespace
4. Update project dependencies

### Phase 2: Move Component-Specific Entities
1. Move single-component entities to their respective components
2. Update namespace declarations
3. Update using statements across the codebase
4. Verify no cross-component dependencies remain

### Phase 3: Clean Up Shared Projects
1. Move misplaced files to correct projects:
   - `CommunicationConstants.cs` → `SWLOR.Component.Communication/Constants/`
   - `ColorToken.cs` → `SWLOR.Shared.UI/Service/`
2. Remove empty folders and unused references
3. Update project dependencies

### Phase 4: Establish Component Boundaries
1. Review each component for external dependencies
2. Create interfaces for cross-component communication
3. Implement dependency injection for component services
4. Add integration tests to verify component isolation

### Phase 5: Validation
1. Ensure no component directly references another component's implementation
2. Verify all cross-component communication goes through interfaces
3. Confirm shared domain contains only truly shared entities
4. Test that components can be developed and tested independently

## Success Criteria

- [ ] No component directly references another component's concrete classes
- [ ] All cross-component communication happens through interfaces
- [ ] Shared domain contains only entities used by 2+ components
- [ ] Each component owns its specific business logic and entities
- [ ] Infrastructure concerns are separated from business logic
- [ ] Components can be developed and tested independently

## Anti-Patterns to Avoid

1. **God Components**: Components that try to do everything
2. **Circular Dependencies**: Component A depending on Component B, which depends on Component A
3. **Shared Everything**: Putting all entities in shared domain regardless of usage
4. **Infrastructure in Domain**: Mixing technical concerns with business logic
5. **Direct Component References**: Components importing other components' implementation details

## Maintenance Guidelines

- When adding new entities, start in the component that owns them
- Only move to shared domain when a second component needs the entity
- Use interfaces for cross-component communication
- Keep domain models focused on business logic, not infrastructure
- Regularly review component dependencies to prevent coupling

This plan ensures consistent, maintainable component architecture that supports independent development and testing while maintaining clear domain boundaries.
