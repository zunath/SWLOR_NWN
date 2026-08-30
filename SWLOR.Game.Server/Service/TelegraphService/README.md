# Telegraph System for SWLOR_NWN

The Telegraph System provides visual indicators for area-of-effect abilities, allowing players to see where attacks will land before they execute.

## Features

- **Multiple Shape Types**: Sphere, Cone, and Line telegraphs
- **Color Coding**: Different colors based on hostility and relationship
- **Real-time Rendering**: Shader-based visual effects
- **Area Management**: Efficient tracking of telegraphs by area
- **Event Integration**: Full integration with SWLOR's event system

## Core Components

### Telegraph
The main service class that manages all telegraph functionality.

### Telegraph Types
- **Sphere**: Circular area of effect
- **Cone**: Triangular area extending from a point
- **Line**: Rectangular area extending in a direction

### Color Types

Color selection uses this precedence so beneficial effects retain a consistent meaning while
offensive telegraphs still communicate ownership:

1. **Self**: Blue - any telegraph created by the observing player
2. **Beneficial**: Green - a beneficial telegraph from any other creator, including party members
3. **Party Member**: Gray - a hostile telegraph created by another member of the observer's party
4. **Hostile**: Red - a hostile telegraph from any other creator

Non-party allies and neutral NPCs use red or green based on the telegraph's hostile flag.
Hostile telegraphs from associates tracked as party members use gray; their beneficial effects
use green. The current ability integrations do not create a neutral, informational telegraph;
add a fifth semantic color only if that use case is introduced.

## Usage

### Basic Telegraph Creation

```csharp
// Create a simple sphere telegraph
var telegraphId = Telegraph.CreateSphereTelegraph(
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
var telegraphId = Telegraph.CreateConeTelegraph(
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
var telegraphId = Telegraph.CreateLineTelegraph(
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
var telegraphId = Telegraph.CreateTelegraphAtCreature(
    creator,
    target,
    TelegraphType.Sphere,
    new Vector2(5.0f, 5.0f), // Size
    3.0f,                    // Duration
    true,                    // Is hostile
    action);

// Create telegraph in front of a creature
var telegraphId = Telegraph.CreateTelegraphInFrontOfCreature(
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

### Telegraph Methods

- `CreateTelegraph()` - Create a custom telegraph
- `CancelTelegraph(string telegraphId)` - Cancel a telegraph before completion
- `GetTelegraphsInArea(uint area)` - Get all telegraphs in an area
- `IsCreatureInTelegraph(uint creature, string telegraphId)` - Check if creature is in telegraph
- `ClearAllTelegraphs()` - Clear all telegraphs (cleanup)

### Telegraph Shape Helpers

- `CreateSphereTelegraph()` - Create a sphere telegraph
- `CreateConeTelegraph()` - Create a cone telegraph
- `CreateLineTelegraph()` - Create a line telegraph
- `CreateTelegraphAtCreature()` - Create telegraph at creature position
- `CreateTelegraphInFrontOfCreature()` - Create telegraph in front of creature

## Event System

A telegraph is backed by a temporary `EffectRunScript` effect on its creator. When that
effect expires, the `telegraph_effect` handler (`ScriptName.TelegraphEffect`) fires
`Telegraph.OnRemoved`, which runs the telegraph's action against the creatures inside its
shape and then clears the entry. There is no separate applied/ticked event.

Shader uniforms are refreshed when a telegraph is created or removed in an area, and when a
player enters an area. There is no periodic tick.

## Pre-cast telegraphs vs the impact flash

Two distinct paths render a shape, and they are not interchangeable:

- **Pre-cast telegraph** — drawn by `UsePerkFeat` for the length of an ability's activation
  delay, or by `Ability.ApplyTelegraphedCombatImpact` when `telegraphDuration > 0`. This
  *gates* the effect: the action runs when the telegraph expires. Only use it for abilities
  the Design Bible grants a real casting time.
- **Impact flash** — drawn by `Ability.ApplyTelegraphedCombatImpact` when
  `telegraphDuration <= 0`. Purely visual, carries no action, and does not delay damage.
  This is what makes Bible-"Instant" area abilities visible without changing their
  activation time. Duration comes from `Ability.DefaultImpactFlashDuration`.

Do not set `GeneratedWeaponAbilityProfile.TelegraphDuration` from a Bible casting time: the
pre-cast telegraph already covers the activation delay, and this one applies at impact, so
the two would stack into a double delay and a double render.

## Testing

`SWLOR.Game.Server.Tests/Service/TelegraphTests.cs` covers the shader bit-packing, the
impact-flash default, and the double-delay invariant. Rendering itself needs an in-game
check; there is no headless harness for it.

## Performance Considerations

- Maximum of 16 telegraphs rendered per player at once
- Telegraphs are tracked by area, and shader updates only touch players in that area
- The impact flash fires on every instant area ability, so anything added to the
  create/remove path runs in the combat hot path

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
    var telegraphId = Telegraph.CreateSphereTelegraph(
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
