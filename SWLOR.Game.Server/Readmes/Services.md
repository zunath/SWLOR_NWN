# SWLOR.Game.Server Services Documentation

This document provides an overview of the Service layer in SWLOR.Game.Server, which handles the core business logic and game mechanics.

## Overview

The Service layer contains the main business logic for the game. Services are organized by functionality and provide the core game mechanics that builders and other components use.

## Directory Structure

```
Service/
├── AbilityService/
├── AchievementService/
├── ActivityService/
├── AIService/
├── AnimationService/
├── BeastMasteryService/
├── ChatCommandService/
├── CombatService/
├── CraftService/
├── CurrencyService/
├── DBService/
├── DialogService/
├── DroidService/
├── FactionService/
├── FishingService/
├── GuiService/
├── ItemService/
├── KeyItemService/
├── LanguageService/
├── LogService/
├── LootService/
├── MigrationService/
├── NPCService/
├── PerkService/
├── PlayerMarketService/
├── PropertyService/
├── QuestService/
├── SkillService/
├── SnippetService/
├── SpaceService/
├── SpawnService/
├── StatService/
├── StatusEffectService/
├── TaxiService/
└── WeatherService/
```

## Core Services

### 1. AbilityService

**Purpose**: Manages player abilities and combat skills.

**Key Components**:
- `AbilityBuilder` - Creates ability configurations
- `AbilityDetail` - Stores ability data
- `AbilityActivationType` - Defines how abilities activate

**Common Usage**:
```csharp
// Check if player can use ability
if (Ability.CanUseAbility(player, abilityType))
{
    Ability.UseAbility(player, target, abilityType);
}
```

### 2. CombatService

**Purpose**: Handles all combat-related calculations and mechanics.

**Key Components**:
- Damage calculation
- Attack rolls
- Defense calculations
- Combat points
- Enmity system

**Common Usage**:
```csharp
// Calculate damage
var damage = Combat.CalculateDamage(attack, baseDamage, attackerStat, defense, defenderStat, bonus);

// Add combat points
CombatPoint.AddCombatPoint(attacker, target, skillType, points);

// Modify enmity
Enmity.ModifyEnmity(attacker, target, amount);
```

### 3. PerkService

**Purpose**: Manages player perks and skill progression.

**Key Components**:
- `PerkBuilder` - Creates perk configurations
- `PerkDetail` - Stores perk data
- Perk requirements and costs

**Common Usage**:
```csharp
// Check if player has perk
if (Perk.HasPerk(player, perkType))
{
    var level = Perk.GetPerkLevel(player, perkType);
    // Use perk level
}
```

### 4. ItemService

**Purpose**: Handles item interactions and use effects.

**Key Components**:
- `ItemBuilder` - Creates item configurations
- Item validation
- Use effects
- Recast timers

**Common Usage**:
```csharp
// Use an item
if (Item.CanUseItem(player, item))
{
    Item.UseItem(player, item, target, location);
}
```

### 5. QuestService

**Purpose**: Manages quest progression and state.

**Key Components**:
- `QuestBuilder` - Creates quest configurations
- Quest state management
- Quest requirements
- Quest rewards

**Common Usage**:
```csharp
// Start a quest
Quest.StartQuest(player, questId);

// Check quest state
var state = Quest.GetQuestState(player, questId);

// Complete a quest
Quest.CompleteQuest(player, questId);
```

### 6. DialogService

**Purpose**: Handles NPC conversations and dialog trees.

**Key Components**:
- `DialogBuilder` - Creates dialog configurations
- Dialog page management
- Response handling
- Data model integration

**Common Usage**:
```csharp
// Start a dialog
var dialog = Dialog.StartDialog(player, npc, dialogId);

// Add dialog page
dialog.AddPage("main", page => {
    page.AddResponse("Hello", "greeting");
});
```

### 7. SpaceService

**Purpose**: Manages space combat and ship mechanics.

**Key Components**:
- `ShipBuilder` - Creates ship configurations
- `ShipModuleBuilder` - Creates module configurations
- Space combat mechanics
- Ship navigation

**Common Usage**:
```csharp
// Enter space mode
Space.EnterSpace(player, shipItem);

// Use ship module
Space.UseModule(player, moduleType);
```

### 8. BeastMasteryService

**Purpose**: Manages beast companions and taming.

**Key Components**:
- `BeastBuilder` - Creates beast configurations
- Beast leveling
- Beast mutations
- Beast commands

**Common Usage**:
```csharp
// Summon beast
Beast.SummonBeast(player, beastType);

// Command beast
Beast.CommandBeast(player, commandType);
```

### 9. CraftService

**Purpose**: Handles crafting and recipe management.

**Key Components**:
- `RecipeBuilder` - Creates recipe configurations
- Crafting success calculations
- Material requirements
- Crafting bonuses

**Common Usage**:
```csharp
// Check if player can craft
if (Craft.CanCraft(player, recipeType))
{
    var result = Craft.CraftItem(player, recipeType);
}
```

### 10. PropertyService

**Purpose**: Manages player properties and buildings.

**Key Components**:
- `PropertyLayoutBuilder` - Creates property layouts
- Property ownership
- Building management
- Property permissions

**Common Usage**:
```csharp
// Purchase property
Property.PurchaseProperty(player, propertyType);

// Enter property
Property.EnterProperty(player, propertyId);
```

## Service Patterns

### 1. Builder Pattern

Many services use builders to create complex configurations:

```csharp
// Example: Creating a perk
var builder = new PerkBuilder();
builder.Create(PerkCategoryType.Force, PerkType.ForceLightning)
    .Name("Force Lightning")
    .AddPerkLevel()
    .Price(1);
```

### 2. Validation Pattern

Services often validate inputs before processing:

```csharp
// Example: Item validation
if (!Item.CanUseItem(player, item))
{
    SendMessageToPC(player, "You cannot use this item.");
    return;
}
```

### 3. State Management Pattern

Services manage game state and persistence:

```csharp
// Example: Quest state
var questState = Quest.GetQuestState(player, questId);
if (questState == QuestStateType.NotStarted)
{
    Quest.StartQuest(player, questId);
}
```

### 4. Event-Driven Pattern

Services often respond to game events:

```csharp
// Example: Combat event
public static void OnPlayerDamaged(uint player, uint attacker, int damage)
{
    // Update combat statistics
    Stat.ModifyStat(player, AbilityType.Constitution, -damage);
    
    // Check for status effects
    StatusEffect.ProcessDamage(player, damage);
}
```

## Service Integration

### 1. Cross-Service Communication

Services often work together:

```csharp
// Example: Using multiple services
public static void UseAbility(uint player, uint target, FeatType abilityType)
{
    // Check ability requirements
    if (!Ability.CanUseAbility(player, abilityType))
        return;
    
    // Calculate damage
    var damage = Combat.CalculateDamage(/* parameters */);
    
    // Apply damage
    Stat.ModifyStat(target, AbilityType.Constitution, -damage);
    
    // Add combat points
    CombatPoint.AddCombatPoint(player, target, SkillType.Force, 3);
    
    // Modify enmity
    Enmity.ModifyEnmity(player, target, damage);
}
```

### 2. Data Flow

Typical data flow through services:

1. **Input Validation** - Service validates inputs
2. **Business Logic** - Service processes the request
3. **State Update** - Service updates game state
4. **Event Triggering** - Service triggers related events
5. **Response** - Service returns result

## Best Practices

### 1. Service Organization

- Keep services focused on a single responsibility
- Use clear, descriptive service names
- Group related functionality together

### 2. Error Handling

```csharp
// Example: Proper error handling
public static bool UseAbility(uint player, uint target, FeatType abilityType)
{
    try
    {
        if (!Ability.CanUseAbility(player, abilityType))
            return false;
            
        // Process ability
        return true;
    }
    catch (Exception ex)
    {
        Log.Write(LogGroup.Error, $"Error using ability: {ex.Message}");
        return false;
    }
}
```

### 3. Performance Considerations

- Cache frequently accessed data
- Use efficient data structures
- Minimize database calls
- Batch operations when possible

### 4. Testing Services

```csharp
// Example: Service testing
[Test]
public void TestAbilityService()
{
    var player = CreateTestPlayer();
    var target = CreateTestTarget();
    
    var result = Ability.UseAbility(player, target, FeatType.ForceLightning1);
    
    Assert.IsTrue(result);
    Assert.IsTrue(Stat.GetCurrentHP(target) < Stat.GetMaxHP(target));
}
```

## Common Service Methods

### 1. Validation Methods

```csharp
CanUseAbility(uint player, FeatType abilityType)
CanUseItem(uint player, uint item)
CanCraft(uint player, RecipeType recipeType)
```

### 2. State Methods

```csharp
GetQuestState(uint player, string questId)
GetPerkLevel(uint player, PerkType perkType)
GetBeastLevel(uint player, BeastType beastType)
```

### 3. Action Methods

```csharp
UseAbility(uint player, uint target, FeatType abilityType)
UseItem(uint player, uint item, uint target, Location location)
CraftItem(uint player, RecipeType recipeType)
```

### 4. Query Methods

```csharp
GetAbilityDetails(FeatType abilityType)
GetItemDetails(string itemTag)
GetRecipeDetails(RecipeType recipeType)
```

## Service Dependencies

### 1. Core Dependencies

Most services depend on:
- **DBService** - Database operations
- **LogService** - Logging functionality
- **StatService** - Character statistics

### 2. Service-Specific Dependencies

- **CombatService** depends on **AbilityService** and **PerkService**
- **QuestService** depends on **ItemService** and **FactionService**
- **SpaceService** depends on **ShipService** and **CombatService**

## Extension Points

### 1. Adding New Services

To add a new service:

1. Create service directory in `Service/`
2. Implement service class with static methods
3. Add service to appropriate initialization
4. Document service methods and usage

### 2. Extending Existing Services

To extend existing services:

1. Add new methods to service class
2. Follow existing naming conventions
3. Add appropriate validation
4. Update documentation

### 3. Service Integration

To integrate with other services:

1. Identify required dependencies
2. Use service methods appropriately
3. Handle service failures gracefully
4. Maintain separation of concerns

This documentation provides a comprehensive overview of the Service layer in SWLOR.Game.Server, covering the main services, patterns, and best practices for working with the service architecture. 