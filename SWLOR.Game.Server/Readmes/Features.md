# SWLOR.Game.Server Features Documentation

This document provides an overview of the Feature layer in SWLOR.Game.Server, which contains game-specific implementations, configurations, and content definitions.

## Overview

The Feature layer contains all the game-specific content, implementations, and configurations. This is where the actual game content is defined, including abilities, items, quests, NPCs, and other game elements.

## Directory Structure

```
Feature/
├── AbilityDefinition/
│   ├── Armor/
│   ├── Beastmaster/
│   ├── Beasts/
│   ├── Devices/
│   ├── FirstAid/
│   ├── Force/
│   ├── General/
│   ├── Leadership/
│   ├── MartialArts/
│   ├── NPC/
│   ├── OneHanded/
│   ├── Ranged/
│   └── TwoHanded/
├── AchievementProgression.cs
├── AIDefinition/
├── AppearanceDefinition/
├── AreaConfiguration.cs
├── ArmorDisplay.cs
├── BeastDefinition/
├── ChatCommandDefinition/
├── DialogDefinition/
├── FishingLocationDefinition/
├── GuiDefinition/
├── ItemDefinition/
├── LootTableDefinition/
├── MigrationDefinition/
├── PerkDefinition/
├── PropertyLayoutDefinition/
├── QuestDefinition/
├── RecipeDefinition/
├── ShipDefinition/
├── ShipModuleDefinition/
├── SnippetDefinition/
├── SpaceObjectDefinition/
├── SpawnDefinition/
├── StatusEffectDefinition/
└── TrapDefinition/
```

## Feature Categories

### 1. Ability Definitions

**Purpose**: Define player abilities and combat skills.

**Location**: `Feature/AbilityDefinition/`

**Categories**:
- **Force** - Force powers and abilities
- **Combat** - OneHanded, TwoHanded, Ranged combat abilities
- **Support** - FirstAid, Leadership abilities
- **Special** - Beastmaster, Devices, MartialArts abilities

**Example Structure**:
```csharp
public class ForceLightningAbilityDefinition : IAbilityListDefinition
{
    public Dictionary<FeatType, AbilityDetail> BuildAbilities()
    {
        var builder = new AbilityBuilder();
        ForceLightning1(builder);
        ForceLightning2(builder);
        ForceLightning3(builder);
        ForceLightning4(builder);
        return builder.Build();
    }
}
```

### 2. Item Definitions

**Purpose**: Define item behavior and properties.

**Location**: `Feature/ItemDefinition/`

**Types**:
- **ConsumableItemDefinition.cs** - Items that can be used/consumed
- **DestroyItemDefinition.cs** - Items that destroy themselves when used
- **BeastEggItemDefinition.cs** - Special items for beast system

**Example**:
```csharp
public class HealthPotionItemDefinition : IItemListDefinition
{
    public Dictionary<string, ItemDetail> BuildItems()
    {
        var builder = new ItemBuilder();
        builder.Create("health_potion")
            .InitializationMessage("You drink the health potion.")
            .HasRecastDelay(RecastGroup.HealthPotion, 60f)
            .ApplyAction((user, item, target, location, itemPropertyIndex) => {
                Stat.RestoreHP(user, 50);
            });
        return builder.Build();
    }
}
```

### 3. Quest Definitions

**Purpose**: Define quest content and progression.

**Location**: `Feature/QuestDefinition/`

**Categories**:
- **AgricultureGuildQuestDefinition.cs** - Farming quests
- **CZ220QuestDefinition.cs** - Space station quests
- **DantooineQuestDefinition.cs** - Planet-specific quests

**Example**:
```csharp
public class KillRatsQuestDefinition : IQuestListDefinition
{
    public Dictionary<string, QuestDetail> BuildQuests()
    {
        var builder = new QuestBuilder();
        builder.Create("quest_kill_rats", "Kill the Rats")
            .IsRepeatable()
            .AddState("start")
            .AddState("complete");
        return builder.Build();
    }
}
```

### 4. Perk Definitions

**Purpose**: Define player perks and skill trees.

**Location**: `Feature/PerkDefinition/`

**Categories**:
- **AgriculturePerkDefinition.cs** - Farming perks
- **ArmorPerkDefinition.cs** - Armor-related perks
- **BeastBalancedPerkDefinition.cs** - Beast companion perks

**Example**:
```csharp
public class ForceLightningPerkDefinition : IPerkListDefinition
{
    public Dictionary<PerkType, PerkDetail> BuildPerks()
    {
        var builder = new PerkBuilder();
        builder.Create(PerkCategoryType.Force, PerkType.ForceLightning)
            .Name("Force Lightning")
            .Description("Unleash devastating lightning from your fingertips.")
            .AddPerkLevel()
            .Price(1)
            .Description("Deals electrical damage to enemies.");
        return builder.Build();
    }
}
```

### 5. Recipe Definitions

**Purpose**: Define crafting recipes and requirements.

**Location**: `Feature/RecipeDefinition/`

**Categories**:
- **CookingRecipeDefinition/** - Food recipes
- **EngineeringRecipeDefinition/** - Engineering recipes
- **FabricationRecipeDefinition/** - Fabrication recipes
- **SmitheryRecipeDefinition/** - Weapon/armor recipes

**Example**:
```csharp
public class LightsaberRecipeDefinition : IRecipeListDefinition
{
    public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
    {
        var builder = new RecipeBuilder();
        builder.Create(RecipeType.Lightsaber, SkillType.Engineering)
            .Category(RecipeCategoryType.Weapons)
            .Level(10)
            .Quantity(1)
            .Resref("lightsaber_01");
        return builder.Build();
    }
}
```

### 6. Ship Definitions

**Purpose**: Define ship configurations and properties.

**Location**: `Feature/ShipDefinition/`

**Types**:
- **NPCShipDefinition.cs** - NPC ship configurations
- **PlayerShipDefinition.cs** - Player ship configurations

**Example**:
```csharp
public class XWingShipDefinition : IShipListDefinition
{
    public Dictionary<string, ShipDetail> BuildShips()
    {
        var builder = new ShipBuilder();
        builder.Create("xwing_item")
            .Name("X-Wing Fighter")
            .Appearance(AppearanceType.XWing)
            .CapitalShip()
            .RequirePerk(PerkType.Pilot, 1);
        return builder.Build();
    }
}
```

### 7. Dialog Definitions

**Purpose**: Define NPC conversations and dialog trees.

**Location**: `Feature/DialogDefinition/`

**Types**:
- **CoxxionTerminalDialog.cs** - Terminal conversations
- **DestroyItemDialog.cs** - Item destruction confirmations
- **DiceDialog.cs** - Dice rolling dialogs

**Example**:
```csharp
public class MerchantDialogDefinition : IDialogListDefinition
{
    public Dictionary<string, DialogDetail> BuildDialogs()
    {
        var builder = new DialogBuilder();
        builder.AddPage("main", page => {
            page.AddResponse("Buy", "buy");
            page.AddResponse("Sell", "sell");
            page.AddResponse("Goodbye", "farewell");
        });
        return builder.Build();
    }
}
```

### 8. Spawn Definitions

**Purpose**: Define creature and object spawning.

**Location**: `Feature/SpawnDefinition/`

**Categories**:
- **CZ220SpawnDefinition.cs** - Space station spawns
- **DantooineSpawnDefinition.cs** - Planet spawns
- **ResourceSpawnDefinition.cs** - Resource node spawns

**Example**:
```csharp
public class RatSpawnDefinition : ISpawnListDefinition
{
    public Dictionary<string, SpawnDetail> BuildSpawns()
    {
        var builder = new SpawnBuilder();
        builder.Create("rat_spawn")
            .CreatureResref("rat_01")
            .SpawnType(SpawnType.Creature)
            .MaxSpawns(5)
            .RespawnTime(300f);
        return builder.Build();
    }
}
```

## Feature Patterns

### 1. Definition Interface Pattern

All feature definitions implement specific interfaces:

```csharp
// Ability definitions
public interface IAbilityListDefinition
{
    Dictionary<FeatType, AbilityDetail> BuildAbilities();
}

// Item definitions
public interface IItemListDefinition
{
    Dictionary<string, ItemDetail> BuildItems();
}

// Quest definitions
public interface IQuestListDefinition
{
    Dictionary<string, QuestDetail> BuildQuests();
}
```

### 2. Builder Pattern Usage

Features use builders to create configurations:

```csharp
// Using builders in features
public Dictionary<FeatType, AbilityDetail> BuildAbilities()
{
    var builder = new AbilityBuilder();
    
    // Create multiple abilities
    CreateAbility1(builder);
    CreateAbility2(builder);
    CreateAbility3(builder);
    
    return builder.Build();
}

private static void CreateAbility1(AbilityBuilder builder)
{
    builder.Create(FeatType.Ability1, PerkType.Ability1)
        .Name("Ability Name")
        .Level(1)
        .HasImpactAction(ImpactAction);
}
```

### 3. Configuration Pattern

Features define configurations for game systems:

```csharp
// Configuration example
public class AreaConfiguration
{
    public string AreaResref { get; set; }
    public SpawnType[] SpawnTypes { get; set; }
    public bool IsPvPEnabled { get; set; }
    public int MaxPlayers { get; set; }
}
```

## Feature Organization

### 1. Category-Based Organization

Features are organized by category:

```
Feature/
├── AbilityDefinition/     # Player abilities
├── ItemDefinition/        # Item behaviors
├── QuestDefinition/       # Quest content
├── PerkDefinition/        # Player perks
├── RecipeDefinition/      # Crafting recipes
├── ShipDefinition/        # Ship configurations
├── DialogDefinition/      # NPC conversations
└── SpawnDefinition/       # Creature spawning
```

### 2. Subcategory Organization

Within categories, features are further organized:

```
AbilityDefinition/
├── Force/                 # Force abilities
├── OneHanded/            # One-handed combat
├── TwoHanded/            # Two-handed combat
├── Ranged/               # Ranged combat
├── FirstAid/             # Healing abilities
└── Leadership/           # Support abilities
```

### 3. File Naming Conventions

Feature files follow consistent naming:

```
[Category][SpecificName]Definition.cs
```

Examples:
- `ForceLightningAbilityDefinition.cs`
- `HealthPotionItemDefinition.cs`
- `KillRatsQuestDefinition.cs`
- `XWingShipDefinition.cs`

## Feature Implementation

### 1. Creating New Features

To create a new feature:

1. **Choose Category** - Select appropriate category directory
2. **Create Definition** - Create definition class implementing interface
3. **Use Builder** - Use appropriate builder for configuration
4. **Register** - Register with appropriate service
5. **Test** - Test the feature thoroughly

**Example: New Ability**
```csharp
public class MyNewAbilityDefinition : IAbilityListDefinition
{
    public Dictionary<FeatType, AbilityDetail> BuildAbilities()
    {
        var builder = new AbilityBuilder();
        
        builder.Create(FeatType.MyNewAbility, PerkType.MyNewAbility)
            .Name("My New Ability")
            .Level(1)
            .HasRecastDelay(RecastGroup.MyNewAbility, 30f)
            .IsCastedAbility()
            .HasImpactAction(ImpactAction);
            
        return builder.Build();
    }
    
    private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
    {
        // Ability logic here
    }
}
```

### 2. Feature Registration

Features must be registered with services:

```csharp
// Register ability definition
Ability.RegisterAbilityDefinition(new MyNewAbilityDefinition());

// Register item definition
Item.RegisterItemDefinition(new MyNewItemDefinition());

// Register quest definition
Quest.RegisterQuestDefinition(new MyNewQuestDefinition());
```

### 3. Feature Testing

Test features thoroughly:

```csharp
[Test]
public void TestMyNewAbility()
{
    // Setup test environment
    var player = CreateTestPlayer();
    var target = CreateTestTarget();
    
    // Test ability usage
    var result = Ability.UseAbility(player, target, FeatType.MyNewAbility);
    
    // Verify results
    Assert.IsTrue(result);
    Assert.IsTrue(Stat.GetCurrentHP(target) < Stat.GetMaxHP(target));
}
```

## Feature Best Practices

### 1. Consistent Naming

```csharp
// Good naming
public class ForceLightningAbilityDefinition : IAbilityListDefinition
public class HealthPotionItemDefinition : IItemListDefinition
public class KillRatsQuestDefinition : IQuestListDefinition

// Bad naming
public class MyAbility : IAbilityListDefinition
public class Item : IItemListDefinition
public class Quest : IQuestListDefinition
```

### 2. Proper Organization

```csharp
// Organize by category
Feature/AbilityDefinition/Force/ForceLightningAbilityDefinition.cs
Feature/AbilityDefinition/Force/ForcePushAbilityDefinition.cs
Feature/AbilityDefinition/OneHanded/PowerAttackAbilityDefinition.cs
```

### 3. Reusable Components

```csharp
// Create reusable impact actions
private static void SharedDamageAction(uint activator, uint target, int level, Location targetLocation)
{
    var damage = CalculateDamage(activator, level);
    ApplyDamage(target, damage);
    AddCombatPoints(activator, target);
    ModifyEnmity(activator, target, damage);
}

// Use in multiple abilities
private static void Ability1(AbilityBuilder builder)
{
    builder.Create(FeatType.Ability1, PerkType.Ability1)
        .HasImpactAction(SharedDamageAction);
}

private static void Ability2(AbilityBuilder builder)
{
    builder.Create(FeatType.Ability2, PerkType.Ability2)
        .HasImpactAction(SharedDamageAction);
}
```

### 4. Configuration Management

```csharp
// Use configuration classes
public class AbilityConfiguration
{
    public string Name { get; set; }
    public int Level { get; set; }
    public float Cooldown { get; set; }
    public float Range { get; set; }
}

// Apply configuration
private static void CreateAbilityFromConfig(AbilityBuilder builder, AbilityConfiguration config)
{
    builder.Create(config.FeatType, config.PerkType)
        .Name(config.Name)
        .Level(config.Level)
        .HasRecastDelay(config.RecastGroup, config.Cooldown)
        .HasMaxRange(config.Range);
}
```

## Feature Integration

### 1. Service Integration

Features integrate with services:

```csharp
// Feature uses service
public static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
{
    // Use combat service
    var damage = Combat.CalculateDamage(/* parameters */);
    
    // Use stat service
    Stat.ModifyStat(target, AbilityType.Constitution, -damage);
    
    // Use logging service
    Log.Write(LogGroup.Combat, $"Ability used: {damage} damage");
}
```

### 2. Builder Integration

Features use builders for configuration:

```csharp
// Feature uses builder
public Dictionary<FeatType, AbilityDetail> BuildAbilities()
{
    var builder = new AbilityBuilder();
    
    // Configure abilities using builder
    builder.Create(FeatType.Ability, PerkType.Ability)
        .Name("Ability Name")
        .Level(1)
        .HasImpactAction(ImpactAction);
        
    return builder.Build();
}
```

### 3. Entity Integration

Features work with entities:

```csharp
// Feature uses entities
public static void QuestComplete(uint player, string questId)
{
    // Update quest entity
    var quest = DB.Query<PCQuest>()
        .Where(x => x.CharacterID == player && x.QuestID == questId)
        .FirstOrDefault();
        
    if (quest != null)
    {
        quest.QuestState = QuestStateType.Completed;
        quest.DateCompleted = DateTime.UtcNow;
        DB.Set(quest);
    }
}
```

## Feature Maintenance

### 1. Adding New Features

When adding new features:

1. **Create Definition** - Create appropriate definition class
2. **Use Builder** - Use appropriate builder for configuration
3. **Register** - Register with appropriate service
4. **Test** - Test thoroughly
5. **Document** - Update documentation

### 2. Modifying Existing Features

When modifying existing features:

1. **Backup** - Backup existing implementation
2. **Test** - Test changes thoroughly
3. **Validate** - Ensure changes don't break other features
4. **Update** - Update related documentation

### 3. Feature Deprecation

When deprecating features:

1. **Mark** - Mark as deprecated
2. **Migrate** - Provide migration path
3. **Remove** - Remove after migration period
4. **Update** - Update documentation

This documentation provides a comprehensive overview of the Feature layer in SWLOR.Game.Server, covering the main feature categories, patterns, and best practices for working with game content. 