# Combat Upgrade Project - Considerations & Tasks

This document tracks items that need to be considered and implemented for the combat upgrade project.

## Current Branch
- **Branch**: `feature/project-refactor-combat-upgrade`

## High Priority Items

### 1. Item Property Migration
- **Task**: Migrate elemental defense item properties to a new "Resistance" item property
- **Status**: Pending
- **Details**: 
  - Need to identify all existing elemental defense item properties
  - Design new "Resistance" item property structure
  - Plan migration strategy for existing items

### 2. Server Migration - Defense Values
- **Task**: Wipe all existing defense values for Fire, Electrical, Ice, and Poison
- **Status**: Pending
- **Details**:
  - **KEEP**: Physical & Force defenses (do not wipe these)
  - **WIPE**: Fire, Electrical, Ice, and Poison defense values
  - Plan database migration script
  - Consider impact on existing player characters

### 3. Accuracy Item Property Replacement
- **Task**: Replace existing accuracy item property (default NWN one) with custom implementation
- **Status**: Pending
- **Details**:
  - Replace default NWN accuracy item property with custom one
  - Update StatGroupService to handle the new custom accuracy property
  - Ensure compatibility with existing items that use accuracy
  - Plan migration strategy for existing accuracy items

### 4. Player Entity Cleanup
- **Task**: Remove the "Defenses" property from the Player entity
- **Status**: Pending
- **Details**:
  - Remove the "Defenses" property from the Player entity
  - Update any code that references this property
  - Ensure new defense system doesn't rely on this property
  - Consider impact on existing player data and migration

### 5. Player Entity Stat Migration
- **Task**: Remove AbilityRecastReduction from Player entity in favor of the new stat dictionary
- **Status**: Pending
- **Details**:
  - Remove AbilityRecastReduction property from Player entity
  - Migrate to new stat dictionary system
  - Update any code that references AbilityRecastReduction
  - Ensure new stat system properly handles ability recast reduction
  - Plan migration strategy for existing player data

## Implementation Notes

### Database Considerations
- Need to identify which tables store defense values
- Plan migration scripts to preserve Physical & Force defenses
- Consider rollback strategy

### Code Impact
- Review existing defense calculation logic
- Update any hardcoded references to elemental defenses
- Ensure new Resistance system integrates properly

### Testing Requirements
- Test migration script on development environment
- Verify Physical & Force defenses are preserved
- Confirm elemental defenses are properly wiped
- Test new Resistance item property functionality

## Future Considerations
- Additional items will be added to this list as the project progresses
- Each item should include status, priority, and implementation details
- Consider impact on existing player data and gameplay balance

---
*Last Updated: [Current Date]*
*Branch: feature/project-refactor-combat-upgrade*
