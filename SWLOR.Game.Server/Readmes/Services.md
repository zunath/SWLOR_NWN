# SWLOR.Game.Server Services Documentation

This document provides an overview of the Service layer in SWLOR.Game.Server, which handles the core business logic and game mechanics.

## Overview

The Service layer contains the main business logic for the game. Services are organized by functionality and provide the core game mechanics that builders and other components use.

## Directory Structure

```
Service/
├── Ability.cs                         # Ability management
├── Achievement.cs                     # Achievement system
├── Activity.cs                        # Activity tracking
├── AI.cs                             # AI behavior
├── AnimationPlayer.cs                 # Animation system
├── Area.cs                           # Area management
├── Authorization.cs                   # Authorization system
├── BeastMastery.cs                   # Beast companion system
├── Cache.cs                          # Caching system
├── ChatCommand.cs                    # Chat command handling
├── ColorToken.cs                     # Color token system
├── Combat.cs                         # Combat mechanics
├── CombatPoint.cs                    # Combat point system
├── Communication.cs                  # Communication system
├── Craft.cs                          # Crafting system
├── Currency.cs                       # Currency management
├── DB.cs                             # Database operations
├── Death.cs                          # Death handling
├── Dialog.cs                         # Dialog system
├── Droid.cs                          # Droid system
├── Enmity.cs                         # Enmity system
├── Faction.cs                        # Faction system
├── Fishing.cs                        # Fishing system
├── Guild.cs                          # Guild system
├── HoloCom.cs                        # HoloCom system
├── Item.cs                           # Item management
├── KeyItem.cs                        # Key item system
├── Language.cs                       # Language system
├── Loot.cs                           # Loot system
├── Log.cs                            # Logging system
├── Menu.cs                           # Menu system
├── Messaging.cs                      # Messaging system
├── Migration.cs                      # Migration system
├── Music.cs                          # Music system
├── NPCGroup.cs                       # NPC group management
├── ObjectVisibility.cs               # Object visibility
├── Party.cs                          # Party system
├── Perk.cs                           # Perk system
├── Planet.cs                         # Planet management
├── PlayerMarket.cs                   # Player market
├── Property.cs                       # Property system
├── Quest.cs                          # Quest system
├── Race.cs                           # Race system
├── Random.cs                         # Random number generation
├── Recast.cs                         # Recast timer system
├── Snippet.cs                        # Snippet system
├── Space.cs                          # Space combat system
├── Spawn.cs                          # Spawn system
├── Stat.cs                           # Stat management
├── StatusEffect.cs                   # Status effect system
├── Targeting.cs                      # Targeting system
├── Taxi.cs                           # Taxi system
├── TileMagic.cs                      # Tile magic system
├── Time.cs                           # Time management
├── Walkmesh.cs                       # Walkmesh system
├── Weather.cs                        # Weather system
├── AbilityService/                   # Ability service components
├── AchievementService/               # Achievement service components
├── ActivityService/                  # Activity service components
├── AIService/                        # AI service components
├── AnimationService/                 # Animation service components
├── BeastMasteryService/              # Beast mastery service components
├── ChatCommandService/               # Chat command service components
├── CombatService/                    # Combat service components
├── CraftService/                     # Craft service components
├── CurrencyService/                  # Currency service components
├── DBService/                        # Database service components
├── DialogService/                    # Dialog service components
├── DroidService/                     # Droid service components
├── FactionService/                   # Faction service components
├── FishingService/                   # Fishing service components
├── GuiService/                       # GUI service components
├── ItemService/                      # Item service components
├── KeyItemService/                   # Key item service components
├── LanguageService/                  # Language service components
├── LogService/                       # Log service components
├── LootService/                      # Loot service components
├── MigrationService/                 # Migration service components
├── NPCService/                       # NPC service components
├── PerkService/                      # Perk service components
├── PlayerMarketService/              # Player market service components
├── PropertyService/                  # Property service components
├── QuestService/                     # Quest service components
├── SkillService/                     # Skill service components
├── SnippetService/                   # Snippet service components
├── SpaceService/                     # Space service components
├── SpawnService/                     # Spawn service components
├── StatService/                      # Stat service components
├── StatusEffectService/              # Status effect service components
├── TaxiService/                      # Taxi service components
└── WeatherService/                   # Weather service components
```

## Core Services

### 1. AbilityService

**Purpose**: Manages player abilities and combat skills.

**Key Components**:
- `AbilityBuilder` - Creates ability configurations
- `AbilityDetail` - Stores ability data
- `AbilityActivationType` - Defines how abilities activate

**Common Usage**: See `Service/Ability.cs` for available methods and usage patterns.

### 2. CombatService

**Purpose**: Handles all combat-related calculations and mechanics.

**Key Components**:
- Damage calculation
- Attack rolls
- Defense calculations
- Combat points
- Enmity system

**Common Usage**: See `Service/Combat.cs` and `Service/CombatPoint.cs` for combat mechanics.

### 3. PerkService

**Purpose**: Manages player perks and skill progression.

**Key Components**:
- `PerkBuilder` - Creates perk configurations
- `PerkDetail` - Stores perk data
- Perk requirements and costs

**Common Usage**: See `Service/Perk.cs` for perk management methods.

### 4. ItemService

**Purpose**: Handles item interactions and use effects.

**Key Components**:
- `ItemBuilder` - Creates item configurations
- Item validation
- Use effects
- Recast timers

**Common Usage**: See `Service/Item.cs` for item management methods.

### 5. QuestService

**Purpose**: Manages quest progression and state.

**Key Components**:
- `QuestBuilder` - Creates quest configurations
- Quest state management
- Quest requirements
- Quest rewards

**Common Usage**: See `Service/Quest.cs` for quest management methods.

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

**Common Usage**: See `Service/Space.cs` for space combat methods.

### 8. BeastMasteryService

**Purpose**: Manages beast companions and taming.

**Key Components**:
- `BeastBuilder` - Creates beast configurations
- Beast leveling
- Beast mutations
- Beast food and care

**Common Usage**: See `Service/BeastMastery.cs` for beast management methods.

### 9. CraftService

**Purpose**: Handles crafting and recipe systems.

**Key Components**:
- `RecipeBuilder` - Creates recipe configurations
- Crafting validation
- Resource management
- Quality system

**Common Usage**: See `Service/Craft.cs` for crafting methods.

### 10. PropertyService

**Purpose**: Manages player-owned properties and buildings.

**Key Components**:
- Property ownership
- Building permissions
- Property customization
- Property management

**Common Usage**: See `Service/Property.cs` for property management methods.

## Service Patterns

### 1. Builder Pattern

Most services use builder patterns for complex object creation. See the individual service directories for builder implementations:

- `Service/AbilityService/AbilityBuilder.cs`
- `Service/PerkService/PerkBuilder.cs`
- `Service/ChatCommandService/ChatCommandBuilder.cs`
- `Service/CraftService/RecipeBuilder.cs`
- `Service/BeastMasteryService/BeastBuilder.cs`
- `Service/ItemService/ItemBuilder.cs`

### 2. Service Integration

Services often work together to provide complex functionality. See the individual service files for integration examples and cross-service communication patterns.

### 3. Error Handling

Services include comprehensive error handling. See the individual service files for error handling patterns and logging implementations.

## Performance Considerations

### 1. Caching

Services use caching to improve performance. See `Service/Cache.cs` for caching implementations and patterns.

### 2. Database Optimization

Database operations are optimized. See `Service/DB.cs` for database access patterns and optimization strategies.

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