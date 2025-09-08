# SWLOR.Game.Server Builders Documentation

This document provides comprehensive documentation for all the builder classes available in the SWLOR.Game.Server project. Builders are used throughout the codebase to create complex objects with a fluent interface pattern.

## Overview

Builders in SWLOR.Game.Server follow a consistent pattern:
1. **Create** - Initialize a new object with required parameters
2. **Configure** - Use fluent methods to set properties and behaviors
3. **Build** - Return the final constructed object or collection

All builders use a fluent interface, allowing method chaining for clean, readable code.

## Available Builders

### 1. AbilityBuilder

**Location**: `Service/AbilityService/AbilityBuilder.cs`

The AbilityBuilder is used to create player abilities and combat skills. It's one of the most complex builders, supporting various ability types including casted abilities, weapon abilities, and concentration abilities.

#### Basic Usage

```csharp
var builder = new AbilityBuilder();
builder.Create(FeatType.ForceLightning1, PerkType.ForceLightning)
    .Name("Force Lightning I")
    .Level(1)
    .HasRecastDelay(RecastGroup.ForceLightning, 30f)
    .HasActivationDelay(2f)
    .HasMaxRange(30.0f)
    .IsCastedAbility()
    .IsHostileAbility()
    .UsesAnimation(Animation.LoopingConjure1)
    .HasImpactAction(ImpactAction);
```

#### Key Methods

- **Create(FeatType, PerkType)** - Initialize a new ability
- **Name(string)** - Set the ability name
- **Level(int)** - Set the ability level
- **IsCastedAbility()** - Mark as a casted ability with delay
- **IsWeaponAbility()** - Mark as a weapon-triggered ability
- **IsConcentrationAbility(StatusEffectType)** - Mark as a concentration ability
- **HasRecastDelay(RecastGroup, float)** - Set cooldown timer
- **HasActivationDelay(float)** - Set casting time
- **HasMaxRange(float)** - Set maximum range
- **UsesAnimation(Animation)** - Set casting animation
- **IsHostileAbility()** - Mark as targeting enemies
- **HasImpactAction(AbilityImpactAction)** - Set the main ability effect
- **RequirementFP(int)** - Set Force Point cost
- **RequirementStamina(int)** - Set Stamina cost

#### Example from ForceLightningAbilityDefinition.cs

```csharp
private static void ForceLightning1(AbilityBuilder builder)
{
    builder.Create(FeatType.ForceLightning1, PerkType.ForceLightning)
        .Name("Force Lightning I")
        .Level(1)
        .HasRecastDelay(RecastGroup.ForceLightning, 30f)
        .HasActivationDelay(2f)
        .HasMaxRange(30.0f)
        .IsCastedAbility()
        .IsHostileAbility()
        .UsesAnimation(Animation.LoopingConjure1)
        .HasImpactAction(ImpactAction);
}
```

### 2. PerkBuilder

**Location**: `Service/PerkService/PerkBuilder.cs`

The PerkBuilder creates player perks that can be purchased with skill points. Perks have multiple levels with different costs and effects.

#### Basic Usage

```csharp
var builder = new PerkBuilder();
builder.Create(PerkCategoryType.Force, PerkType.ForceLightning)
    .Name("Force Lightning")
    .Description("Unleash devastating lightning from your fingertips.")
    .AddPerkLevel()
    .Price(1)
    .Description("Deals electrical damage to enemies.")
    .AddPerkLevel()
    .Price(2)
    .Description("Increased damage and range.");
```

#### Key Methods

- **Create(PerkCategoryType, PerkType)** - Initialize a new perk
- **Name(string)** - Set the perk name
- **Description(string)** - Set description (perk or level)
- **AddPerkLevel()** - Add a new level to the perk
- **Price(int)** - Set skill point cost for current level
- **Inactive()** - Disable the perk

### 3. ChatCommandBuilder

**Location**: `Service/ChatCommandService/ChatCommandBuilder.cs`

The ChatCommandBuilder creates chat commands that players can use in-game.

#### Basic Usage

```csharp
var builder = new ChatCommandBuilder();
builder.Create("heal", "cure")
    .Description("Heals your target for 50 HP")
    .RequiresTarget(ObjectType.Creature)
    .RequiresAuthorizationLevel(AuthorizationLevel.Player);
```

#### Key Methods

- **Create(string, params string[])** - Initialize command with aliases
- **Description(string)** - Set command description
- **RequiresTarget(ObjectType)** - Make command require a target
- **RequiresAuthorizationLevel(AuthorizationLevel)** - Set permission level
- **AvailableToAllOnTestEnvironment()** - Allow all players in test mode

### 4. RecipeBuilder

**Location**: `Service/CraftService/RecipeBuilder.cs`

The RecipeBuilder creates crafting recipes for the crafting system.

#### Basic Usage

```csharp
var builder = new RecipeBuilder();
builder.Create(RecipeType.Lightsaber, SkillType.Engineering)
    .Category(RecipeCategoryType.Weapons)
    .Level(10)
    .Quantity(1)
    .Resref("lightsaber_01");
```

#### Key Methods

- **Create(RecipeType, SkillType)** - Initialize recipe with type and skill
- **Category(RecipeCategoryType)** - Set recipe category
- **Level(int)** - Set recipe difficulty level
- **Quantity(int)** - Set items produced per craft
- **Resref(string)** - Set the item resref to create

### 5. BeastBuilder

**Location**: `Service/BeastMasteryService/BeastBuilder.cs`

The BeastBuilder creates beast companion configurations for the beastmaster system.

#### Basic Usage

```csharp
var builder = new BeastBuilder();
builder.Create(BeastType.Wolf)
    .Name("Wolf")
    .Appearance(AppearanceType.Wolf)
    .CombatStats(AbilityType.Perception, AbilityType.Might)
    .AddLevel()
    .HP(50)
    .FP(20)
    .STM(30)
    .DMG(10);
```

#### Key Methods

- **Create(BeastType)** - Initialize a new beast
- **Name(string)** - Set beast name
- **Appearance(AppearanceType)** - Set beast appearance
- **CombatStats(AbilityType, AbilityType)** - Set accuracy and damage stats
- **AddLevel()** - Add a new level to the beast
- **HP(int)** - Set HP for current level
- **FP(int)** - Set Force Points for current level
- **STM(int)** - Set Stamina for current level
- **DMG(int)** - Set damage for current level

### 6. ItemBuilder

**Location**: `Service/ItemService/ItemBuilder.cs`

The ItemBuilder creates item configurations that define how items behave when used.

#### Basic Usage

```csharp
var builder = new ItemBuilder();
builder.Create("health_potion")
    .InitializationMessage("You drink the health potion.")
    .HasRecastDelay(RecastGroup.HealthPotion, 60f)
    .ApplyAction((user, item, target, location, itemPropertyIndex) => {
        // Healing logic here
    });
```

#### Key Methods

- **Create(string, params string[])** - Initialize item with tags
- **InitializationMessage(string)** - Set use message
- **HasRecastDelay(RecastGroup, float)** - Set cooldown
- **ApplyAction(ApplyItemEffectsDelegate)** - Set use effect
- **ValidationAction(ValidateItemDelegate)** - Set validation logic

### 7. QuestBuilder

**Location**: `Service/QuestService/QuestBuilder.cs`

The QuestBuilder creates quest configurations for the quest system.

#### Basic Usage

```csharp
var builder = new QuestBuilder();
builder.Create("quest_kill_rats", "Kill the Rats")
    .IsRepeatable()
    .AddState("start")
    .AddState("complete");
```

#### Key Methods

- **Create(string, string)** - Initialize quest with ID and name
- **IsRepeatable()** - Mark quest as repeatable
- **AddState(string)** - Add a quest state

### 8. ShipBuilder

**Location**: `Service/SpaceService/ShipBuilder.cs`

The ShipBuilder creates ship configurations for the space combat system.

#### Basic Usage

```csharp
var builder = new ShipBuilder();
builder.Create("xwing_item")
    .Name("X-Wing Fighter")
    .Appearance(AppearanceType.XWing)
    .CapitalShip()
    .RequirePerk(PerkType.Pilot, 1);
```

#### Key Methods

- **Create(string)** - Initialize ship with item tag
- **Name(string)** - Set ship name
- **Appearance(AppearanceType)** - Set ship appearance
- **CapitalShip()** - Mark as capital ship
- **RequirePerk(PerkType, int)** - Set required pilot level

### 9. StatusEffectBuilder

**Location**: `Service/StatusEffectService/StatusEffectBuilder.cs`

The StatusEffectBuilder creates status effects that can be applied to creatures.

#### Basic Usage

```csharp
var builder = new StatusEffectBuilder();
builder.Create(StatusEffectType.ForceLightning)
    .Name("Force Lightning")
    .CanStack()
    .TickAction((target, effectData) => {
        // Damage over time logic
    });
```

#### Key Methods

- **Create(StatusEffectType)** - Initialize status effect
- **Name(string)** - Set effect name
- **CanStack()** - Allow multiple instances
- **TickAction(StatusEffectTickAction)** - Set periodic effect

### 10. DialogBuilder

**Location**: `Service/DialogService/DialogBuilder.cs`

The DialogBuilder creates dialog configurations for NPC conversations.

#### Basic Usage

```csharp
var builder = new DialogBuilder();
builder.AddPage("main", page => {
    page.AddResponse("Hello", "greeting");
    page.AddResponse("Goodbye", "farewell");
});
```

#### Key Methods

- **AddPage(string, Action<DialogPage>)** - Add dialog page
- **AddInitializationAction(Action)** - Set initialization logic
- **WithDataModel(object)** - Set data model for dialog

### 11. SnippetBuilder

**Location**: `Service/SnippetService/SnippetBuilder.cs`

The SnippetBuilder creates conversation snippets for conditional dialog options.

#### Basic Usage

```csharp
var builder = new SnippetBuilder();
builder.Create("has_quest_item")
    .Description("Player has the required quest item")
    .AppearsWhenAction((player, npc) => {
        return HasItem(player, "quest_item");
    });
```

#### Key Methods

- **Create(string)** - Initialize snippet with key
- **Description(string)** - Set snippet description
- **AppearsWhenAction(SnippetConditionDelegate)** - Set condition logic

### 12. PropertyLayoutBuilder

**Location**: `Service/PropertyService/PropertyLayoutBuilder.cs`

The PropertyLayoutBuilder creates property layout configurations.

#### Basic Usage

```csharp
var builder = new PropertyLayoutBuilder();
builder.Create(PropertyLayoutType.Apartment)
    .PropertyType(PropertyType.Apartment)
    .Name("Standard Apartment")
    .Description("A basic apartment layout");
```

#### Key Methods

- **Create(PropertyLayoutType)** - Initialize layout
- **PropertyType(PropertyType)** - Set property type
- **Name(string)** - Set layout name
- **Description(string)** - Set layout description

### 13. FishingLocationBuilder

**Location**: `Service/FishingService/FishingLocationBuilder.cs`

The FishingLocationBuilder creates fishing location configurations.

#### Basic Usage

```csharp
var builder = new FishingLocationBuilder();
builder.Create(FishingLocationType.Dantooine)
    .DefaultFish(FishType.MoatCarp)
    .AddFish(FishType.Bass, 25);
```

#### Key Methods

- **Create(FishingLocationType)** - Initialize fishing location
- **DefaultFish(FishType)** - Set fallback fish
- **AddFish(FishType, int)** - Add fish with spawn chance

### 14. ShipModuleBuilder

**Location**: `Service/SpaceService/ShipModuleBuilder.cs`

The ShipModuleBuilder creates ship module configurations.

#### Basic Usage

```csharp
var builder = new ShipModuleBuilder();
builder.Create("laser_cannon")
    .Name("Laser Cannon")
    .ShortName("Laser")
    .ModuleType(ShipModuleType.Weapon);
```

#### Key Methods

- **Create(string)** - Initialize module with item tag
- **Name(string)** - Set module name
- **ShortName(string)** - Set short name
- **ModuleType(ShipModuleType)** - Set module type

### 15. SpaceObjectBuilder

**Location**: `Service/SpaceService/SpaceObjectBuilder.cs`

The SpaceObjectBuilder creates space object configurations for space combat.

#### Basic Usage

```csharp
var builder = new SpaceObjectBuilder();
builder.Create("asteroid_01")
    .ItemTag("asteroid_ship")
    .Name("Asteroid");
```

#### Key Methods

- **Create(string)** - Initialize space object
- **ItemTag(string)** - Set associated ship item
- **Name(string)** - Set object name

### 16. LootTableBuilder

**Location**: `Service/LootService/LootTableBuilder.cs`

The LootTableBuilder creates loot table configurations for creature drops.

#### Basic Usage

```csharp
var builder = new LootTableBuilder();
builder.Create("rat_loot")
    .IsRare()
    .AddItem("gold_coin", 100, 1, 5);
```

#### Key Methods

- **Create(string)** - Initialize loot table
- **IsRare()** - Mark as rare loot table
- **AddItem(string, int, int, int)** - Add item with chance and quantity range

## Best Practices

1. **Always call Build()** - Most builders require calling Build() to return the final result
2. **Use meaningful names** - Choose descriptive names for abilities, items, and other objects
3. **Group related configurations** - Use separate methods for different ability levels or item types
4. **Validate parameters** - Check that required parameters are provided
5. **Use enums consistently** - Use the provided enum types for categories, types, and other classifications

## Common Patterns

### Creating Multiple Related Objects

```csharp
var builder = new AbilityBuilder();
ForceLightning1(builder);
ForceLightning2(builder);
ForceLightning3(builder);
ForceLightning4(builder);
return builder.Build();
```

### Conditional Configuration

```csharp
var builder = new PerkBuilder();
builder.Create(PerkCategoryType.Force, PerkType.ForceLightning)
    .Name("Force Lightning");

if (isActive)
{
    builder.AddPerkLevel()
        .Price(1)
        .Description("Basic lightning attack");
}
```

### Reusing Builders

```csharp
var itemBuilder = new ItemBuilder();
ConfigureHealthPotion(itemBuilder);
ConfigureManaPotion(itemBuilder);
return itemBuilder.Build();
```

This documentation covers all the major builders in the SWLOR.Game.Server project. Each builder follows the fluent interface pattern and provides a clean, readable way to create complex game objects. 