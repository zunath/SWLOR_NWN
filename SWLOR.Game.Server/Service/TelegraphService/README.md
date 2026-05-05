# Telegraph System for SWLOR_NWN

The Telegraph System provides visual indicators for area-of-effect abilities, allowing players to see where attacks will land before they execute.

## Features

- **Multiple Shape Types**: Sphere, Cone, and Line telegraphs
- **Color Coding**: Different colors based on hostility and relationship
- **Real-time Rendering**: Shader-based visual effects
- **Area Management**: Efficient tracking of telegraphs by area
- **Event Integration**: Full integration with SWLOR's event system

## Core Components

### TelegraphService
The main service class that manages all telegraph functionality.

### Telegraph Types
- **Sphere**: Circular area of effect
- **Cone**: Triangular area extending from a point
- **Line**: Rectangular area extending in a direction

### Color Types
- **Hostile**: Red - indicates dangerous effects
- **Friendly**: Green - indicates beneficial effects
- **Self**: Blue - indicates effects from the player
- **Enemy Beneficial**: Gray - indicates beneficial effects from enemies

## Usage

### Basic Telegraph Creation

```csharp
// Create a simple sphere telegraph
var telegraphId = TelegraphHelper.CreateSphereTelegraph(
    attacker,           // Creator of the telegraph
    position,           // Center position
    5.0f,              // Radius in meters
    3.0f,              // Duration in seconds
    true,              // Is hostile
    (creator, affectedCreatures) =>
    {
        // Action to execute when telegraph completes
        foreach (var creature in affectedCreatures)
        {
            if (GetIsEnemy(creator, creature))
            {
                ApplyEffectToObject(DurationType.Instant, EffectDamage(50, DamageType.Fire), creature);
            }
        }
    });
```

### Cone Telegraph

```csharp
var telegraphId = TelegraphHelper.CreateConeTelegraph(
    attacker,
    position,
    facing,             // Direction in radians
    8.0f,              // Length in meters
    4.0f,              // Width at the end
    2.0f,              // Duration in seconds
    true,              // Is hostile
    action);
```

### Line Telegraph

```csharp
var telegraphId = TelegraphHelper.CreateLineTelegraph(
    attacker,
    position,
    facing,             // Direction in radians
    10.0f,             // Length in meters
    2.0f,              // Width in meters
    1.5f,              // Duration in seconds
    true,              // Is hostile
    action);
```

### Advanced Usage

```csharp
// Create telegraph at a specific creature's position
var telegraphId = TelegraphHelper.CreateTelegraphAtCreature(
    creator,
    target,
    TelegraphType.Sphere,
    new Vector2(5.0f, 5.0f), // Size
    3.0f,                    // Duration
    true,                    // Is hostile
    action);

// Create telegraph in front of a creature
var telegraphId = TelegraphHelper.CreateTelegraphInFrontOfCreature(
    creator,
    target,
    2.0f,                    // Distance in front
    TelegraphType.Cone,
    new Vector2(6.0f, 3.0f), // Size
    2.5f,                    // Duration
    true,                    // Is hostile
    action);
```

## API Reference

### TelegraphService Methods

- `CreateTelegraph()` - Create a custom telegraph
- `CancelTelegraph(string telegraphId)` - Cancel a telegraph before completion
- `GetTelegraphsInArea(uint area)` - Get all telegraphs in an area
- `IsCreatureInTelegraph(uint creature, string telegraphId)` - Check if creature is in telegraph
- `ClearAllTelegraphs()` - Clear all telegraphs (cleanup)

### TelegraphHelper Methods

- `CreateSphereTelegraph()` - Create a sphere telegraph
- `CreateConeTelegraph()` - Create a cone telegraph
- `CreateLineTelegraph()` - Create a line telegraph
- `CreateTelegraphAtCreature()` - Create telegraph at creature position
- `CreateTelegraphInFrontOfCreature()` - Create telegraph in front of creature

## Event System

The telegraph system integrates with SWLOR's event system:

- `TelegraphEvents.TelegraphApplied` - Fired when telegraph is applied
- `TelegraphEvents.TelegraphTicked` - Fired during telegraph duration
- `TelegraphEvents.TelegraphRemoved` - Fired when telegraph is removed

## Testing

Use the `TelegraphTest` class for testing telegraph functionality:

- `TestSphereTelegraph()` - Test sphere telegraphs
- `TestConeTelegraph()` - Test cone telegraphs
- `TestLineTelegraph()` - Test line telegraphs
- `TestBeneficialTelegraph()` - Test beneficial telegraphs
- `TestMultipleTelegraphs()` - Test multiple simultaneous telegraphs
- `ClearAllTelegraphs()` - Clear all active telegraphs

## Performance Considerations

- Maximum of 8 telegraphs rendered per player at once
- Telegraphs are automatically cleaned up when areas are unloaded
- Shader updates are limited to 30 FPS to maintain performance
- Telegraphs are tracked by area for efficient management

## Integration with Abilities

To integrate telegraphs with existing abilities:

1. Create the telegraph when the ability starts
2. Use the telegraph's action callback to execute the ability's effect
3. Cancel the telegraph if the ability is interrupted
4. Handle telegraph completion in the ability's logic

## Example: Ability with Telegraph

```csharp
public static void FireballAbility(uint caster, uint target)
{
    var position = GetPosition(target);
    
    // Create telegraph
    var telegraphId = TelegraphHelper.CreateSphereTelegraph(
        caster,
        position,
        5.0f,  // 5 meter radius
        3.0f,  // 3 second cast time
        true,  // Hostile
        (creator, affectedCreatures) =>
        {
            // Execute fireball damage
            foreach (var creature in affectedCreatures)
            {
                if (GetIsEnemy(creator, creature))
                {
                    var damage = RollDice(8, 6); // 8d6 fire damage
                    ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Fire), creature);
                }
            }
        });
    
    // Store telegraph ID for potential cancellation
    SetLocalString(caster, "FIREBALL_TELEGRAPH_ID", telegraphId);
}
```

## Troubleshooting

### Telegraphs Not Appearing
- Check that shader uniforms are being set correctly
- Verify the telegraph is created in the correct area
- Ensure the creator is a valid creature

### Performance Issues
- Limit the number of simultaneous telegraphs
- Use appropriate telegraph sizes
- Consider telegraph duration vs. performance impact

### Telegraphs Not Affecting Creatures
- Verify the telegraph action callback is working
- Check creature detection logic
- Ensure telegraph shape calculations are correct
