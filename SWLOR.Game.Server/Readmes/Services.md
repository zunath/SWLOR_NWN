# SWLOR.Game.Server Services Documentation

This document provides an overview of the Service layer in SWLOR.Game.Server, which handles the core business logic and game mechanics.

## Overview

The Service layer contains the main business logic for the game. Services are organized by functionality and provide the core game mechanics that builders and other components use.

Services are instance classes built by a dependency-injection container (the maintainability refactor, Phase 2). A service is an instance class `<Name>Service` behind the interface `I<Name>Service` under `Domain/<Domain>/`; static code reaches it through the property of its name on `GameServices` (`GameServices.Ability`, `GameServices.Craft`), and a class the container builds takes the interface through its constructor. There are no static facades left. See [DependencyInjection.md](DependencyInjection.md).

## Directory Structure

```
Domain/                               # Instance services, one folder per domain
├── Abilities/  Administration/  Character/  Combat/  Companions/
├── Core/  Crafting/  Creatures/  Economy/  Gameplay/
└── Items/  Quests/  Social/  UI/  World/
Composition/                          # The container registrations and the GameServices accessor
Service/                              # Support types of the services, and the static classes not yet converted
├── <Name>Service/                    # Builders, definitions, enums and records of one service
├── Space.cs  Droid.cs                # Unconverted static classes
└── Log.cs  Menu.cs  Random.cs  Time.cs  GameMath.cs  ColorToken.cs ...   # Static helpers and data types
```

`SWLOR.Architecture.Tests/DomainMap.json` says which domain each service is in, and `SWLOR.Architecture.Tests/ConvertedServices.json` lists every converted service with the extra classes and parts it is made of.

## Core Services

### 1. AbilityService

**Purpose**: Manages player abilities and combat skills.

**Key Components**:
- `AbilityBuilder` - Creates ability configurations
- `AbilityDetail` - Stores ability data
- `AbilityActivationType` - Defines how abilities activate

**Common Usage**: See `Domain/Abilities/IAbilityService.cs` for available methods; static code reaches them through `GameServices.Ability`.

### 2. CombatService

**Purpose**: Handles all combat-related calculations and mechanics.

**Key Components**:
- Damage calculation
- Attack rolls
- Defense calculations
- Combat points
- Enmity system

**Common Usage**: See `Domain/Combat/ICombatService.cs` and `Domain/Combat/ICombatPointService.cs` for combat mechanics; static code reaches them through `GameServices.Combat` and `GameServices.CombatPoint`.

### 3. PerkService

**Purpose**: Manages player perks and skill progression.

**Key Components**:
- `PerkBuilder` - Creates perk configurations
- `PerkDetail` - Stores perk data
- Perk requirements and costs

**Common Usage**: See `Domain/Character/IPerkService.cs` for perk management methods; static code reaches them through `GameServices.Perk`.

### 4. ItemService

**Purpose**: Handles item interactions and use effects.

**Key Components**:
- `ItemBuilder` - Creates item configurations
- Item validation
- Use effects
- Recast timers

**Common Usage**: See `Domain/Items/IItemService.cs` for item management methods; static code reaches them through `GameServices.Item`.

### 5. QuestService

**Purpose**: Manages quest progression and state.

**Key Components**:
- `QuestBuilder` - Creates quest configurations
- Quest state management
- Quest requirements
- Quest rewards

**Common Usage**: See `Domain/Quests/QuestService.cs` for quest management methods.

### 6. DialogService

**Purpose**: Handles NPC conversations and dialog trees.

**Key Components**:
- `DialogBuilder` - Creates dialog configurations
- Dialog page management
- Response handling
- Data model integration

**Common Usage**: See `Service/Dialog.cs` for dialog management methods.

### 7. SpaceService

**Purpose**: Manages space combat and ship mechanics.

**Key Components**:
- `ShipBuilder` - Creates ship configurations
- `ShipModuleBuilder` - Creates module configurations
- Space combat mechanics
- Ship navigation

**Common Usage**: See `Domain/Space/SpaceService.cs` for space combat methods.

### 8. BeastMasteryService

**Purpose**: Manages beast companions and taming.

**Key Components**:
- `BeastBuilder` - Creates beast configurations
- Beast leveling
- Beast mutations
- Beast food and care

**Common Usage**: See `Domain/Companions/BeastMasteryService.cs` for beast management methods.

### 9. CraftService

**Purpose**: Handles crafting and recipe systems.

**Key Components**:
- `RecipeBuilder` - Creates recipe configurations
- Crafting validation
- Resource management
- Quality system

**Common Usage**: See `Domain/Crafting/ICraftService.cs` for crafting methods; static code reaches them through `GameServices.Craft`.

### 10. PropertyService

**Purpose**: Manages player-owned properties and buildings.

**Key Components**:
- Property ownership
- Building permissions
- Property customization
- Property management

**Common Usage**: See `Domain/World/IPropertyService.cs` for property management methods; static code reaches them through `GameServices.Property`.

## Service Patterns

### 1. Builder Pattern

Most services use builder patterns for complex object creation. See the individual service directories for builder implementations:

- `Domain/Abilities/AbilityBuilder.cs`
- `Domain/Character/PerkBuilder.cs`
- `Domain/Social/ChatCommandBuilder.cs`
- `Domain/Crafting/RecipeBuilder.cs`
- `Domain/Companions/BeastBuilder.cs`
- `Domain/Items/ItemBuilder.cs`

### 2. Service Integration

Services often work together to provide complex functionality. See the individual service files for integration examples and cross-service communication patterns.

### 3. Error Handling

Services include comprehensive error handling. See the individual service files for error handling patterns and logging implementations.

## Performance Considerations

### 1. Caching

Services use caching to improve performance. See `Domain/Foundation/ICacheService.cs` for caching implementations and patterns.

### 2. Database Optimization

Database operations are optimized. See `Domain/Foundation/IDBService.cs` for database access patterns and optimization strategies.

### 3. Memory Management

Services manage memory efficiently. See individual service files for memory management patterns and resource disposal strategies.

## Testing Services

### 1. Unit Testing

Services should be unit tested. See the test files in the project for examples of service testing patterns.

### 2. Integration Testing

Test service interactions. See the integration test files for examples of cross-service testing.

## Best Practices

### 1. Service Design

- Keep services focused on single responsibility
- Use dependency injection where appropriate
- Implement proper error handling
- Add comprehensive logging

### 2. Performance

- Cache frequently accessed data
- Optimize database queries
- Use async operations where appropriate
- Monitor memory usage

### 3. Maintainability

- Add XML documentation
- Follow consistent naming conventions
- Use builder patterns for complex objects
- Implement proper validation

---

*This documentation should be updated when new services are added or existing services are modified.*
