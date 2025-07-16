# SWLOR.Game.Server Entities Documentation

This document provides an overview of the Entity layer in SWLOR.Game.Server, which represents the data models and database entities used throughout the game.

## Overview

The Entity layer contains the data models that represent game objects, player data, and persistent information. These entities are used by services and builders to manage game state and data persistence.

## Directory Structure

```
Entity/
├── Account.cs
├── AreaNote.cs
├── AuthorizedDM.cs
├── Beast.cs
├── BeastFood.cs
├── BeastLevel.cs
├── BeastMutation.cs
├── BeastStable.cs
├── BuildingPermission.cs
├── Character.cs
├── CharacterSkill.cs
├── CharacterXP.cs
├── CustomEffect.cs
├── Droid.cs
├── DroidCustomization.cs
├── DroidPersonality.cs
├── KeyItem.cs
├── PCBase.cs
├── PCCustomization.cs
├── PCDowntime.cs
├── PCFaction.cs
├── PCFame.cs
├── PCLog.cs
├── PCMigration.cs
├── PCOutfit.cs
├── PCPerk.cs
├── PCQuest.cs
├── PCQuestKillTarget.cs
├── PCQuestItem.cs
├── PCQuestProgress.cs
├── PCRegionalFame.cs
├── PCSearchSite.cs
├── PCSkill.cs
├── PCSpace.cs
├── PCStatusEffect.cs
├── PlayerMarketItem.cs
├── Property.cs
├── PropertyPermission.cs
├── Quest.cs
├── QuestItem.cs
├── QuestKillTarget.cs
├── QuestPrerequisite.cs
├── QuestState.cs
├── QuestType.cs
├── ServerConfiguration.cs
├── Space.cs
├── SpaceObject.cs
└── World.cs
```

## Core Entities

### 1. Account

**Purpose**: Represents a player account and authentication data.

**Key Properties**:
- `AccountID` - Unique account identifier
- `CDKey` - CD key for authentication
- `DateCreated` - Account creation date
- `IsActive` - Whether account is active

**Usage**:
```csharp
// Get account by ID
var account = DB.Get<Account>(accountId);

// Check if account is active
if (account.IsActive)
{
    // Allow login
}
```

### 2. Character

**Purpose**: Represents a player character with all associated data.

**Key Properties**:
- `CharacterID` - Unique character identifier
- `AccountID` - Associated account
- `Name` - Character name
- `HitPoints` - Current HP
- `ForcePoints` - Current FP
- `Stamina` - Current STM
- `Location` - Current location

**Usage**:
```csharp
// Get character by ID
var character = DB.Get<Character>(characterId);

// Update character stats
character.HitPoints = Math.Max(0, character.HitPoints - damage);
DB.Set(character);
```

### 3. PCPerk

**Purpose**: Tracks player character perks and levels.

**Key Properties**:
- `CharacterID` - Associated character
- `PerkID` - Perk type
- `PerkLevel` - Current perk level
- `DateAcquired` - When perk was acquired

**Usage**:
```csharp
// Check if character has perk
var perk = DB.Query<PCPerk>()
    .Where(x => x.CharacterID == characterId && x.PerkID == perkType)
    .FirstOrDefault();

if (perk != null && perk.PerkLevel >= requiredLevel)
{
    // Character has required perk level
}
```

### 4. PCQuest

**Purpose**: Tracks quest progress for player characters.

**Key Properties**:
- `CharacterID` - Associated character
- `QuestID` - Quest identifier
- `QuestState` - Current quest state
- `DateStarted` - When quest was started
- `DateCompleted` - When quest was completed

**Usage**:
```csharp
// Get quest state for character
var quest = DB.Query<PCQuest>()
    .Where(x => x.CharacterID == characterId && x.QuestID == questId)
    .FirstOrDefault();

if (quest == null)
{
    // Quest not started
    quest = new PCQuest
    {
        CharacterID = characterId,
        QuestID = questId,
        QuestState = QuestStateType.NotStarted,
        DateStarted = DateTime.UtcNow
    };
    DB.Set(quest);
}
```

### 5. Property

**Purpose**: Represents player-owned properties and buildings.

**Key Properties**:
- `PropertyID` - Unique property identifier
- `OwnerID` - Character who owns the property
- `PropertyType` - Type of property
- `Location` - Property location
- `CustomName` - Custom property name

**Usage**:
```csharp
// Get properties owned by character
var properties = DB.Query<Property>()
    .Where(x => x.OwnerID == characterId)
    .ToList();

// Create new property
var property = new Property
{
    OwnerID = characterId,
    PropertyType = PropertyType.Apartment,
    Location = location,
    CustomName = "My Apartment"
};
DB.Set(property);
```

### 6. Beast

**Purpose**: Represents beast companions owned by players.

**Key Properties**:
- `BeastID` - Unique beast identifier
- `OwnerID` - Character who owns the beast
- `BeastType` - Type of beast
- `Level` - Current beast level
- `Name` - Beast name
- `Appearance` - Beast appearance

**Usage**:
```csharp
// Get player's beasts
var beasts = DB.Query<Beast>()
    .Where(x => x.OwnerID == characterId)
    .ToList();

// Create new beast
var beast = new Beast
{
    OwnerID = characterId,
    BeastType = BeastType.Wolf,
    Level = 1,
    Name = "Fang",
    Appearance = AppearanceType.Wolf
};
DB.Set(beast);
```

### 7. Droid

**Purpose**: Represents droid companions owned by players.

**Key Properties**:
- `DroidID` - Unique droid identifier
- `OwnerID` - Character who owns the droid
- `DroidType` - Type of droid
- `PersonalityID` - Droid personality
- `CustomizationID` - Droid customization

**Usage**:
```csharp
// Get player's droids
var droids = DB.Query<Droid>()
    .Where(x => x.OwnerID == characterId)
    .ToList();

// Create new droid
var droid = new Droid
{
    OwnerID = characterId,
    DroidType = DroidType.Astromech,
    PersonalityID = DroidPersonalityType.Geeky,
    CustomizationID = 1
};
DB.Set(droid);
```

### 8. KeyItem

**Purpose**: Represents key items that unlock special content.

**Key Properties**:
- `KeyItemID` - Unique key item identifier
- `CharacterID` - Character who has the key item
- `KeyItemType` - Type of key item
- `DateAcquired` - When key item was acquired

**Usage**:
```csharp
// Check if character has key item
var keyItem = DB.Query<KeyItem>()
    .Where(x => x.CharacterID == characterId && x.KeyItemType == keyItemType)
    .FirstOrDefault();

if (keyItem != null)
{
    // Character has the key item
}
```

## Entity Relationships

### 1. One-to-Many Relationships

```csharp
// Account -> Characters
var characters = DB.Query<Character>()
    .Where(x => x.AccountID == accountId)
    .ToList();

// Character -> Perks
var perks = DB.Query<PCPerk>()
    .Where(x => x.CharacterID == characterId)
    .ToList();

// Character -> Quests
var quests = DB.Query<PCQuest>()
    .Where(x => x.CharacterID == characterId)
    .ToList();
```

### 2. Many-to-Many Relationships

```csharp
// Character -> Properties (through ownership)
var properties = DB.Query<Property>()
    .Where(x => x.OwnerID == characterId)
    .ToList();

// Character -> Beasts (through ownership)
var beasts = DB.Query<Beast>()
    .Where(x => x.OwnerID == characterId)
    .ToList();
```

### 3. Complex Relationships

```csharp
// Character with all related data
var character = DB.Get<Character>(characterId);
var perks = DB.Query<PCPerk>().Where(x => x.CharacterID == characterId).ToList();
var quests = DB.Query<PCQuest>().Where(x => x.CharacterID == characterId).ToList();
var properties = DB.Query<Property>().Where(x => x.OwnerID == characterId).ToList();
```

## Data Access Patterns

### 1. CRUD Operations

```csharp
// Create
var entity = new EntityType { /* properties */ };
DB.Set(entity);

// Read
var entity = DB.Get<EntityType>(id);

// Update
var entity = DB.Get<EntityType>(id);
entity.Property = newValue;
DB.Set(entity);

// Delete
DB.Delete<EntityType>(id);
```

### 2. Query Patterns

```csharp
// Simple query
var entities = DB.Query<EntityType>()
    .Where(x => x.Property == value)
    .ToList();

// Complex query
var entities = DB.Query<EntityType>()
    .Where(x => x.Property1 == value1 && x.Property2 == value2)
    .OrderBy(x => x.Property3)
    .Take(10)
    .ToList();

// Join query
var results = DB.Query<EntityType1>()
    .Join(DB.Query<EntityType2>(), 
          e1 => e1.ID, 
          e2 => e2.Entity1ID, 
          (e1, e2) => new { Entity1 = e1, Entity2 = e2 })
    .Where(x => x.Entity1.Property == value)
    .ToList();
```

### 3. Batch Operations

```csharp
// Batch update
var entities = DB.Query<EntityType>()
    .Where(x => x.Property == oldValue)
    .ToList();

foreach (var entity in entities)
{
    entity.Property = newValue;
    DB.Set(entity);
}

// Batch delete
var entitiesToDelete = DB.Query<EntityType>()
    .Where(x => x.Property == value)
    .ToList();

foreach (var entity in entitiesToDelete)
{
    DB.Delete<EntityType>(entity.ID);
}
```

## Entity Validation

### 1. Data Validation

```csharp
// Example: Character validation
public static bool ValidateCharacter(Character character)
{
    if (string.IsNullOrWhiteSpace(character.Name))
        return false;
        
    if (character.HitPoints < 0)
        return false;
        
    if (character.ForcePoints < 0)
        return false;
        
    return true;
}
```

### 2. Business Rule Validation

```csharp
// Example: Perk validation
public static bool CanAcquirePerk(uint characterId, PerkType perkType)
{
    var character = DB.Get<Character>(characterId);
    var existingPerk = DB.Query<PCPerk>()
        .Where(x => x.CharacterID == characterId && x.PerkID == perkType)
        .FirstOrDefault();
        
    // Check if already has perk
    if (existingPerk != null)
        return false;
        
    // Check requirements
    var requirements = GetPerkRequirements(perkType);
    return CheckRequirements(character, requirements);
}
```

## Entity Lifecycle

### 1. Creation

```csharp
// Create new entity
var entity = new EntityType
{
    ID = Guid.NewGuid(),
    CreatedDate = DateTime.UtcNow,
    // Set other properties
};

// Validate before saving
if (ValidateEntity(entity))
{
    DB.Set(entity);
}
```

### 2. Updates

```csharp
// Update existing entity
var entity = DB.Get<EntityType>(id);
if (entity != null)
{
    entity.ModifiedDate = DateTime.UtcNow;
    entity.Property = newValue;
    
    if (ValidateEntity(entity))
    {
        DB.Set(entity);
    }
}
```

### 3. Deletion

```csharp
// Soft delete (recommended)
var entity = DB.Get<EntityType>(id);
if (entity != null)
{
    entity.IsDeleted = true;
    entity.DeletedDate = DateTime.UtcNow;
    DB.Set(entity);
}

// Hard delete
DB.Delete<EntityType>(id);
```

## Performance Considerations

### 1. Indexing

```csharp
// Use indexed properties for queries
var characters = DB.Query<Character>()
    .Where(x => x.AccountID == accountId)  // Indexed
    .ToList();

// Avoid queries on non-indexed properties
var characters = DB.Query<Character>()
    .Where(x => x.Name.Contains("John"))   // Not indexed
    .ToList();
```

### 2. Lazy Loading

```csharp
// Load related data only when needed
var character = DB.Get<Character>(characterId);

// Load perks only when needed
if (needPerks)
{
    var perks = DB.Query<PCPerk>()
        .Where(x => x.CharacterID == characterId)
        .ToList();
}
```

### 3. Caching

```csharp
// Cache frequently accessed data
private static Dictionary<uint, Character> _characterCache = new();

public static Character GetCharacter(uint characterId)
{
    if (_characterCache.TryGetValue(characterId, out var cached))
        return cached;
        
    var character = DB.Get<Character>(characterId);
    if (character != null)
        _characterCache[characterId] = character;
        
    return character;
}
```

## Entity Extensions

### 1. Adding New Entities

To add a new entity:

1. Create entity class in `Entity/` directory
2. Define properties and relationships
3. Add validation logic
4. Update database schema
5. Add to appropriate services

### 2. Entity Inheritance

```csharp
// Base entity class
public abstract class BaseEntity
{
    public Guid ID { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public bool IsDeleted { get; set; }
}

// Derived entity
public class Character : BaseEntity
{
    public string Name { get; set; }
    public int HitPoints { get; set; }
    // Additional properties
}
```

### 3. Entity Interfaces

```csharp
// Interface for entities with ownership
public interface IOwnedEntity
{
    uint OwnerID { get; set; }
}

// Implement in entities
public class Property : IOwnedEntity
{
    public uint OwnerID { get; set; }
    // Other properties
}
```

## Best Practices

### 1. Entity Design

- Keep entities focused on a single responsibility
- Use meaningful property names
- Include audit fields (CreatedDate, ModifiedDate)
- Implement proper validation

### 2. Data Access

- Use appropriate indexes for query performance
- Implement caching for frequently accessed data
- Use batch operations for multiple updates
- Handle concurrency appropriately

### 3. Validation

- Validate data before saving
- Implement business rule validation
- Use consistent validation patterns
- Provide meaningful error messages

### 4. Relationships

- Define clear relationships between entities
- Use appropriate foreign keys
- Handle cascading operations carefully
- Consider performance implications of joins

This documentation provides a comprehensive overview of the Entity layer in SWLOR.Game.Server, covering the main entities, relationships, and best practices for working with data models. 