# SWLOR.Game.Server Entities Documentation

This document provides an overview of the Entity layer in SWLOR.Game.Server, which represents the data models and database entities used throughout the game.

## Overview

The Entity layer contains the data models that represent game objects, player data, and persistent information. These entities are used by services and builders to manage game state and data persistence.

## Directory Structure

```
Entity/
├── Account.cs                         # Player account data
├── AreaNote.cs                        # Area-specific notes
├── AuthorizedDM.cs                    # DM authorization data
├── Beast.cs                           # Beast companion data
├── DMCreature.cs                      # DM creature data
├── Election.cs                        # Election data
├── EntityBase.cs                      # Base entity class
├── IncubationJob.cs                   # Beast incubation jobs
├── IndexedAttribute.cs                # Indexed attributes
├── InventoryItem.cs                   # Inventory item data
├── MarketItem.cs                      # Market item data
├── ModuleCache.cs                     # Module cache data
├── Player.cs                          # Player character data
├── PlayerBan.cs                       # Player ban data
├── PlayerNote.cs                      # Player notes
├── PlayerOutfit.cs                    # Player outfit data
├── PlayerShip.cs                      # Player ship data
├── ResearchJob.cs                     # Research job data
├── ServerConfiguration.cs             # Server configuration
├── WorldProperty.cs                   # World property data
├── WorldPropertyCategory.cs           # World property categories
└── WorldPropertyPermission.cs         # World property permissions
```

## Core Entities

### 1. Account

**Purpose**: Represents a player account and authentication data.

**Key Properties**:
- `AccountID` - Unique account identifier
- `CDKey` - CD key for authentication
- `DateCreated` - Account creation date
- `IsActive` - Whether account is active

**Usage**: See `Entity/Account.cs` for the complete entity definition and usage patterns.

### 2. Player

**Purpose**: Represents a player character with all associated data.

**Key Properties**:
- `PlayerID` - Unique player identifier
- `AccountID` - Associated account
- `Name` - Character name
- `HitPoints` - Current HP
- `ForcePoints` - Current FP
- `Stamina` - Current STM
- `Location` - Current location

**Usage**: See `Entity/Player.cs` for the complete entity definition and usage patterns.

### 3. Beast

**Purpose**: Represents beast companions owned by players.

**Key Properties**:
- `BeastID` - Unique beast identifier
- `OwnerID` - Player who owns the beast
- `BeastType` - Type of beast
- `Level` - Beast level
- `Experience` - Beast experience points

**Usage**: See `Entity/Beast.cs` for the complete entity definition and usage patterns.

### 4. WorldProperty

**Purpose**: Represents world properties and buildings.

**Key Properties**:
- `PropertyID` - Unique property identifier
- `OwnerID` - Player who owns the property
- `PropertyType` - Type of property
- `Location` - Property location
- `CustomName` - Custom property name

**Usage**: See `Entity/WorldProperty.cs` for the complete entity definition and usage patterns.

### 5. MarketItem

**Purpose**: Represents items in the player market.

**Key Properties**:
- `MarketItemID` - Unique market item identifier
- `SellerID` - Player selling the item
- `ItemTag` - Item tag/resref
- `Price` - Item price
- `Quantity` - Item quantity

**Usage**: See `Entity/MarketItem.cs` for the complete entity definition and usage patterns.

### 6. IncubationJob

**Purpose**: Represents beast incubation jobs.

**Key Properties**:
- `IncubationJobID` - Unique job identifier
- `PlayerID` - Player who started the job
- `BeastType` - Type of beast being incubated
- `StartTime` - When incubation started
- `EndTime` - When incubation will complete

**Usage**: See `Entity/IncubationJob.cs` for the complete entity definition and usage patterns.

### 7. Election

**Purpose**: Represents player elections and voting.

**Key Properties**:
- `ElectionID` - Unique election identifier
- `Title` - Election title
- `Description` - Election description
- `StartDate` - When voting starts
- `EndDate` - When voting ends

**Usage**: See `Entity/Election.cs` for the complete entity definition and usage patterns.

## Entity Relationships

### 1. One-to-Many Relationships

**Account to Player**: An account can have multiple player characters
**Player to Beast**: A player can own multiple beast companions
**Player to WorldProperty**: A player can own multiple properties

### 2. Many-to-Many Relationships

**Player to Market Items**: A player can sell multiple items in the market

## Data Access Patterns

### 1. CRUD Operations

**Create**: Create new entities using the DB service
**Read**: Query entities using the DB service with LINQ
**Update**: Modify entities and save using the DB service
**Delete**: Remove entities using the DB service

### 2. Batch Operations

**Batch Insert**: Insert multiple entities in a single operation
**Batch Update**: Update multiple entities efficiently
**Batch Delete**: Remove multiple entities in a single operation

### 3. Query Optimization

**Indexed Queries**: Use indexed properties for better performance
**Pagination**: Implement pagination for large datasets

## Validation and Business Rules

### 1. Entity Validation

**Required Fields**: Use data annotations for required fields
**Custom Validation**: Implement custom validation logic in entities

### 2. Business Rule Enforcement

**State Validation**: Check entity state before operations
**Permission Checking**: Verify access permissions for entity operations

## Performance Considerations

### 1. Caching

**Entity Caching**: Cache frequently accessed entities
**Query Result Caching**: Cache query results for expensive operations

### 2. Database Optimization

**Index Usage**: Use indexed properties in queries
**Batch Operations**: Use batch operations for multiple entities

### 3. Memory Management

**Dispose Pattern**: Properly dispose of database contexts
**Lazy Loading**: Load related data only when needed

## Testing Entities

### 1. Unit Testing

**Entity Creation**: Test entity creation with valid data
**Entity Validation**: Test validation with invalid data

### 2. Integration Testing

**Database Operations**: Test entity persistence and retrieval

## Best Practices

### 1. Entity Design

- Keep entities focused on data representation
- Use clear, descriptive property names
- Implement proper validation
- Follow naming conventions

### 2. Data Access

- Use appropriate query patterns
- Implement caching where beneficial
- Optimize database operations
- Handle transactions properly

### 3. Performance

- Use indexes for frequently queried properties
- Implement pagination for large datasets
- Cache frequently accessed data
- Monitor query performance

### 4. Maintenance

- Keep entities up-to-date with schema changes
- Document entity relationships
- Implement proper validation
- Add comprehensive logging

---

*This documentation should be updated when new entities are added or existing entities are modified.* 