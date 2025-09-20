# SWLOR.Game.Server Documentation

This folder contains comprehensive documentation for the SWLOR.Game.Server project, focusing on the various systems and patterns used throughout the codebase.

## Available Documentation

### [Builders.md](Builders.md)
Comprehensive documentation of all builder classes in the SWLOR.Game.Server project. This includes:

- **16 different builders** covering all major game systems
- **Detailed usage examples** for each builder
- **Key methods and parameters** for each builder
- **Best practices** and common patterns
- **Real-world examples** from the codebase

Builders documented include:
- AbilityBuilder - For creating player abilities and combat skills
- PerkBuilder - For creating player perks and skill trees
- ChatCommandBuilder - For creating in-game chat commands
- RecipeBuilder - For creating crafting recipes
- BeastBuilder - For creating beast companion configurations
- ItemBuilder - For creating item use configurations
- QuestBuilder - For creating quest configurations
- ShipBuilder - For creating ship configurations
- StatusEffectBuilder - For creating status effects
- DialogBuilder - For creating NPC conversations
- SnippetBuilder - For creating conditional dialog options
- PropertyLayoutBuilder - For creating property layouts
- FishingLocationBuilder - For creating fishing locations
- ShipModuleBuilder - For creating ship modules
- SpaceObjectBuilder - For creating space objects
- LootTableBuilder - For creating loot tables

### [AbilityDefinitions.md](AbilityDefinitions.md)
Detailed documentation specifically for ability definitions and how they use the builder pattern. This includes:

- **Directory structure** of ability definitions
- **Three main ability types**: Casted, Weapon, and Concentration abilities
- **Common patterns** and best practices
- **Impact action examples** with real code
- **Integration** with other game systems
- **Testing guidelines** for new abilities

### [Services.md](Services.md)
Comprehensive documentation of the Service layer, which handles the core business logic and game mechanics. This includes:

- **30+ different services** covering all major game systems
- **Service patterns** and integration methods
- **Cross-service communication** examples
- **Error handling** and performance considerations
- **Testing strategies** for services

### [Entities.md](Entities.md)
Detailed documentation of the Entity layer, which represents the data models and database entities. This includes:

- **Core entities** like Character, Account, PCPerk, PCQuest
- **Entity relationships** and data access patterns
- **CRUD operations** and query patterns
- **Validation** and business rule enforcement
- **Performance considerations** and caching strategies

### [CoreSystems.md](CoreSystems.md)
Documentation of the Core systems that provide fundamental functionality and abstractions. This includes:

- **NWScript system** for NWN engine abstractions
- **NWNX plugin system** for extended functionality
- **Async system** for asynchronous programming
- **Beamdog GUI system** for user interfaces
- **Extension methods** and utility patterns

### [Features.md](Features.md)
Comprehensive documentation of the Feature layer, which contains game-specific content and implementations. This includes:

- **Feature categories** like abilities, items, quests, perks
- **Definition interfaces** and builder pattern usage
- **Feature organization** and naming conventions
- **Implementation patterns** and best practices
- **Integration** with services and entities

### [Deployment.md](Deployment.md)
Comprehensive documentation for deploying and running the SWLOR.Game.Server project. This includes:

- **Docker configuration** and container setup
- **Environment management** (Development, Test, Production)
- **Monitoring and logging** with Grafana and Serilog
- **Security considerations** and best practices
- **Troubleshooting** and common issues
- **Scaling strategies** and performance optimization
- **Backup and recovery** procedures



### [ProjectStructure.md](ProjectStructure.md)
Detailed documentation of the overall project structure and development setup. This includes:

- **Project organization** and architecture layers
- **Build configuration** and dependencies
- **Development setup** and prerequisites
- **Application configuration** and environment management
- **Development workflow** and best practices
- **Deployment** and monitoring strategies

## Purpose

This documentation serves several purposes:

1. **Onboarding** - Help new developers understand the codebase structure
2. **Reference** - Provide quick access to builder patterns and usage
3. **Best Practices** - Document established patterns and conventions
4. **Examples** - Show real-world usage from the actual codebase

## Target Audience

- **New developers** joining the SWLOR project
- **Existing developers** looking for reference material
- **Contributors** wanting to understand the codebase patterns
- **Maintainers** needing to understand system interactions

## How to Use This Documentation

### For New Developers
1. Start with [Builders.md](Builders.md) to understand the core patterns
2. Read [AbilityDefinitions.md](AbilityDefinitions.md) to see how builders are used in practice
3. Use the examples as templates for your own implementations

### For Reference
- Use the search function to find specific builders or patterns
- Follow the links between documents for related information
- Use the code examples as starting points for your implementations

### For Contributing
- Follow the documented patterns when adding new features
- Use the provided examples as templates
- Maintain consistency with existing code style and patterns

## Contributing to Documentation

When adding new documentation:

1. **Follow the existing format** - Use the same structure and style
2. **Include real examples** - Use actual code from the codebase
3. **Update this README** - Add new documents to this list
4. **Cross-reference** - Link related documentation together

## Related Resources

- **Code Comments** - Many builders have detailed XML documentation
- **Interface Definitions** - Check the interfaces for additional context
- **Test Files** - Look for unit tests that demonstrate builder usage
- **Example Implementations** - Browse the Feature/ directory for real examples

## Maintenance

This documentation should be updated when:

- New builders are added to the codebase
- Existing builders have significant changes
- New patterns emerge in the codebase
- Examples become outdated due to code changes

---

*This documentation is maintained as part of the SWLOR.Game.Server project and should be kept up-to-date with the codebase.* 