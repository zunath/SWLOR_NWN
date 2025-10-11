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

### 6. New Item Properties Implementation
- **Task**: Add the new item properties introduced to the 2das and tlk
- **Status**: Pending
- **Details**:
  - Identify all new item properties needed for the combat upgrade
  - Add corresponding entries to the 2da files
  - Add appropriate string references to the TLK file
  - Ensure proper integration with the NWN item property system
  - Update any code that references these new properties

### 7. DMG Item Property Simplification
- **Task**: Change DMG to be a single value, no sub-type. Migrate all existing items to this new single value.
- **Status**: Pending
- **Details**:
  - Modify DMG item property to remove sub-type functionality
  - Design new single-value DMG property structure
  - Create migration script to convert existing DMG properties
  - Update all code that handles DMG calculations
  - Test compatibility with existing items and combat system

### 8. Damage Type Item Property
- **Task**: Add an item property which specifies the damage type (Fire, Ice, Force, Poison, etc.) to the 2das and tlk
- **Status**: Pending
- **Details**:
  - Design new damage type item property structure
  - Add damage type constants (Fire, Ice, Force, Poison, etc.) to the system
  - Create corresponding 2da entries for the new property
  - Add TLK string references for damage types
  - Implement logic to apply damage type effects in combat
  - Ensure compatibility with existing damage type handling code

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

### 1. Update iprp_resperk.2da
- **Task**: Update iprp_resperk.2da to reflect changes in the perk list
- **Status**: Pending
- **Details**:
  - Review changes made to perk definitions during combat upgrade
  - Update the 2da file to include any new perks or modified perk references
  - Ensure proper indexing and naming conventions are maintained
  - Test that perk item properties function correctly after update

### 2. Weapon Proficiency Restrictions
- **Task**: Implement equipment restrictions to prevent standard characters from equipping Force-specific weapons (lightsabers)
- **Status**: Pending
- **Details**:
  - Review all Force-specific weapons that should be restricted
  - Implement skill-based equipment validation system
  - Add checks during item equipping to verify character has appropriate skills/perks
  - Provide clear feedback to players when equipment is restricted
  - Test with various character builds to ensure proper restrictions

### 3. Skill Level Equipment Checks
- **Task**: Add skill level validation when equipping items
- **Status**: Pending
- **Details**:
  - Implement skill requirement checking for equipment
  - Add minimum skill level thresholds for various item types
  - Update equipment validation logic to include skill checks
  - Ensure proper error messaging when skill requirements aren't met
  - Test skill progression and equipment availability

### 4. Item Property Migration - Perk to Skill Requirements
- **Task**: Replace perk requirement item properties with skill requirement item properties
- **Status**: Pending
- **Details**:
  - Identify all items with perk-based requirements
  - Design new skill requirement item property structure
  - Create migration script to convert existing perk requirements to skill requirements
  - Update item property handling code
  - Test that migrated items work correctly with new skill system
  - Ensure backward compatibility during transition period

- Additional items will be added to this list as the project progresses
- Each item should include status, priority, and implementation details
- Consider impact on existing player data and gameplay balance

---
*Last Updated: [Current Date]*
*Branch: feature/project-refactor-combat-upgrade*
