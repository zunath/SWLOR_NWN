# Combat System Migration Plan: NWN.Xenomech to SWLOR_NWN

## Executive Summary

This document outlines the comprehensive migration plan for porting the combat system from NWN.Xenomech to SWLOR_NWN, specifically focusing on:
1. **OnAIActionAttackObject.cs** - Native AI attack handling
2. **Telegraph System** - Visual combat telegraphs for abilities

## Current State Analysis

### NWN.Xenomech Combat System

#### OnAIActionAttackObject.cs
- **Location**: `XM.Plugin.Combat\NativeOverride\OnAIActionAttackObject.cs`
- **Purpose**: Native hook override for AI attack behavior
- **Key Features**:
  - Custom attack delay system with `_creatureAttackDelays` dictionary
  - Integration with `CombatService` for attack calculations
  - Weapon ability processing via `AbilityService`
  - Paralyze handling
  - Custom attack range and line-of-sight logic
  - Integration with `XMEventService` for weapon skill events

#### Telegraph System
- **Location**: `XM.Shared.Progression\Ability\Telegraph\`
- **Components**:
  - `TelegraphService.cs` - Main service managing telegraphs
  - `TelegraphData.cs` - Data structure for telegraph information
  - `ActiveTelegraph.cs` - Active telegraph instance
  - `TelegraphType.cs` - Enum for telegraph shapes (Sphere, Cone, Line)
  - `TelegraphEvent.cs` - Event system integration
- **Key Features**:
  - Real-time shader-based visual telegraphs
  - Support for multiple telegraph shapes (Sphere, Cone, Line)
  - Color coding based on hostility and relationship
  - Area-based telegraph management
  - Integration with ability system via delegates

### SWLOR_NWN Combat System

#### Current Combat Implementation
- **Location**: `SWLOR.Game.Server\Service\Combat.cs`
- **Key Features**:
  - Damage calculation system
  - Hit rate calculations
  - Critical hit system
  - Combat log messaging
  - Ability damage bonuses
  - Saving throw calculations

#### Current AI System
- **Location**: `SWLOR.Game.Server\Service\AI.cs`
- **Key Features**:
  - Basic AI behavior management
  - Enmity system integration
  - Perk-based AI decisions
  - Creature ally management

#### Current Attack System
- **Location**: `SWLOR.Game.Server\Native\ResolveAttackRoll.cs`
- **Key Features**:
  - Native attack roll override
  - Custom hit/miss/critical logic
  - Weapon focus and critical hit bonuses
  - Range modifiers
  - Deflection system

## Major Differences and Similarities

### Similarities
1. **Native Hook Architecture**: Both systems use native function hooks for attack resolution
2. **Service-Based Design**: Both use dependency injection and service patterns
3. **Event System Integration**: Both integrate with event systems for combat events
4. **Combat Calculation Logic**: Both have sophisticated hit rate and damage calculation systems

### Key Differences

#### 1. AI Attack Handling
- **Xenomech**: Custom `OnAIActionAttackObject` with attack delays and weapon ability integration
- **SWLOR**: Basic AI system with enmity-based targeting, no custom attack delays

#### 2. Telegraph System
- **Xenomech**: Complete telegraph system with shader-based visuals
- **SWLOR**: No telegraph system exists

#### 3. Combat Service Architecture
- **Xenomech**: More comprehensive with TP gain, attack delays, and status effect integration
- **SWLOR**: Focused on basic combat calculations and damage

#### 4. Event System
- **Xenomech**: Uses `XMEventService` with custom event types
- **SWLOR**: Uses `NWNEventHandler` attributes

## Migration Requirements

### Phase 1: OnAIActionAttackObject Migration

#### 1.1 Create Native Override Structure
- **File**: `SWLOR.Game.Server\Native\OnAIActionAttackObject.cs`
- **Dependencies**: 
  - HookService (from Anvil)
  - VirtualMachine (from Anvil)
  - Combat service integration
  - AI service integration

#### 1.2 Required Changes to SWLOR
1. **Add Attack Delay System**:
   - Implement `_creatureAttackDelays` dictionary
   - Add `CalculateAttackDelay` method to Combat service
   - Integrate with existing attack resolution

2. **Enhance AI Service**:
   - Add weapon ability processing
   - Integrate paralyze handling
   - Add attack range validation

3. **Update Combat Service**:
   - Add `HandleParalyze` method
   - Add attack delay calculations
   - Integrate with weapon abilities

#### 1.3 Native Hook Implementation
```csharp
[ServiceBinding(typeof(OnAIActionAttackObject))]
internal sealed unsafe class OnAIActionAttackObject
{
    private readonly Dictionary<uint, DateTime> _creatureAttackDelays = new();
    private readonly CombatService _combat;
    private readonly VirtualMachine _vm;
    private readonly AI _ai;
    
    // Hook implementation similar to Xenomech but adapted for SWLOR
}
```

### Phase 2: Telegraph System Migration

#### 2.1 Create Telegraph Service Structure
- **Location**: `SWLOR.Game.Server\Service\TelegraphService\`
- **Files to Create**:
  - `TelegraphService.cs`
  - `TelegraphData.cs`
  - `ActiveTelegraph.cs`
  - `TelegraphType.cs`
  - `TelegraphColorType.cs`
  - `TelegraphDelegates.cs`

#### 2.2 Required Infrastructure
1. **Shader System Integration**:
   - Add shader uniform management
   - Implement shader update system
   - Add visual effect rendering

2. **Event System Integration**:
   - Create telegraph-specific events
   - Integrate with existing event system
   - Add telegraph lifecycle management

3. **Area Management**:
   - Implement area-based telegraph tracking
   - Add creature detection within telegraphs
   - Handle area transitions

#### 2.3 Telegraph Service Implementation
```csharp
[ServiceBinding(typeof(TelegraphService))]
public class TelegraphService : IInitializable, IDisposable
{
    private readonly Dictionary<uint, Dictionary<string, ActiveTelegraph>> _telegraphsByArea = new();
    private readonly Dictionary<string, ActiveTelegraph> _allTelegraphs = new();
    
    public string CreateTelegraph(uint creator, Vector3 position, float rotation, 
        Vector2 size, float duration, bool isHostile, TelegraphType type, 
        ApplyTelegraphEffect action);
    
    public void CancelTelegraph(string telegraphId);
}
```

### Phase 3: Integration and Testing

#### 3.1 Service Integration
1. **Update Service Registration**:
   - Add telegraph service to DI container
   - Register native hooks
   - Update service dependencies

2. **Event System Updates**:
   - Add telegraph events to existing event system
   - Integrate with combat events
   - Add telegraph cleanup on creature death

#### 3.2 Combat System Enhancements
1. **Attack System Updates**:
   - Integrate attack delays with existing combat
   - Add weapon ability processing
   - Enhance paralyze handling

2. **AI System Updates**:
   - Add telegraph-aware AI decisions
   - Integrate with existing enmity system
   - Add telegraph avoidance logic

## Implementation Steps

### Step 1: Prepare Infrastructure
1. Create telegraph service directory structure
2. Add required dependencies to project files
3. Update service registration

### Step 2: Implement OnAIActionAttackObject
1. Create native override class
2. Implement attack delay system
3. Add weapon ability integration
4. Test with existing combat system

### Step 3: Implement Telegraph System
1. Create telegraph data structures
2. Implement telegraph service
3. Add shader integration
4. Create telegraph event system

### Step 4: Integration
1. Integrate telegraph system with abilities
2. Add AI telegraph awareness
3. Update combat system integration
4. Add cleanup and lifecycle management

### Step 5: Testing and Optimization
1. Test telegraph rendering performance
2. Validate attack delay system
3. Test AI behavior with telegraphs
4. Optimize shader updates

## Dependencies and Prerequisites

### Required Services
- `HookService` (Anvil)
- `VirtualMachine` (Anvil)
- `CombatService` (SWLOR)
- `AIService` (SWLOR)
- `AbilityService` (SWLOR)

### Required Infrastructure
- Shader system for telegraph rendering
- Event system for telegraph lifecycle
- Area management for telegraph tracking
- Creature detection system

### Required Changes to Existing Code
1. **Combat.cs**: Add attack delay and paralyze handling
2. **AI.cs**: Add telegraph awareness and weapon ability integration
3. **ResolveAttackRoll.cs**: Integrate with attack delay system
4. **Service Registration**: Add new services to DI container

## Risk Assessment

### High Risk
- **Shader Integration**: Complex shader system integration may require significant changes
- **Performance Impact**: Real-time telegraph updates may impact performance
- **Native Hook Stability**: Native function hooks may cause stability issues

### Medium Risk
- **Event System Integration**: Telegraph events may conflict with existing events
- **AI Behavior Changes**: New AI logic may affect existing creature behavior
- **Combat Balance**: Attack delay system may affect combat balance

### Low Risk
- **Data Structure Migration**: Telegraph data structures are straightforward
- **Service Integration**: Service pattern is already established
- **Code Organization**: Well-defined structure for new components

## Success Criteria

1. **OnAIActionAttackObject**: Successfully integrated with attack delay system
2. **Telegraph System**: Fully functional with visual rendering
3. **AI Integration**: Creatures can create and respond to telegraphs
4. **Performance**: No significant performance degradation
5. **Stability**: No crashes or memory leaks
6. **Compatibility**: Works with existing combat and ability systems

## Timeline Estimate

- **Phase 1 (OnAIActionAttackObject)**: 2-3 weeks
- **Phase 2 (Telegraph System)**: 3-4 weeks
- **Phase 3 (Integration)**: 1-2 weeks
- **Testing and Optimization**: 1-2 weeks
- **Total**: 7-11 weeks

## Conclusion

This migration will significantly enhance SWLOR_NWN's combat system by adding sophisticated AI attack handling and visual telegraph system. The modular approach allows for incremental implementation and testing, reducing risk while providing immediate value at each phase.

The key to success will be careful integration with existing systems while maintaining the performance and stability that SWLOR_NWN currently enjoys.
