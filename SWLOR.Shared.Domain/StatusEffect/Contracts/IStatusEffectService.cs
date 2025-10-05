using SWLOR.Shared.Domain.Character.ValueObjects;
using SWLOR.Shared.Domain.StatusEffect.Enums;

namespace SWLOR.Shared.Domain.StatusEffect.Contracts;

/// <summary>
/// Service responsible for managing status effects on creatures in the game world.
/// Handles application, removal, and tracking of various status effects including buffs, debuffs, and temporary conditions.
/// </summary>
public interface IStatusEffectService
{
    /// <summary>
    /// Gets the combined stat group for a creature, including all active status effect modifiers.
    /// </summary>
    /// <param name="creature">The creature object ID to get stats for</param>
    /// <returns>The combined stat group with all status effect modifiers applied</returns>
    StatGroup GetCreatureStatGroup(uint creature);

    /// <summary>
    /// Applies a permanent status effect to a creature that persists until manually removed.
    /// </summary>
    /// <param name="source">The object ID of the source applying the effect</param>
    /// <param name="creature">The creature object ID to apply the effect to</param>
    /// <param name="type">The type of status effect to apply</param>
    void ApplyPermanentStatusEffect(uint source, uint creature, StatusEffectType type);

    /// <summary>
    /// Applies a temporary status effect to a creature with a specified duration.
    /// </summary>
    /// <param name="source">The object ID of the source applying the effect</param>
    /// <param name="creature">The creature object ID to apply the effect to</param>
    /// <param name="type">The type of status effect to apply</param>
    /// <param name="durationTicks">The duration of the effect in game ticks (negative values indicate permanent)</param>
    void ApplyStatusEffect(uint source, uint creature, StatusEffectType type, int durationTicks);

    /// <summary>
    /// Removes a specific status effect from a creature.
    /// </summary>
    /// <param name="creature">The creature object ID to remove the effect from</param>
    /// <param name="type">The type of status effect to remove</param>
    void RemoveStatusEffect(uint creature, StatusEffectType type);

    /// <summary>
    /// Removes all status effects from a creature that were applied by a specific source type.
    /// </summary>
    /// <param name="creature">The creature object ID to remove effects from</param>
    /// <param name="sourceType">The source type of effects to remove</param>
    void RemoveStatusEffectBySourceType(uint creature, StatusEffectSourceType sourceType);

    /// <summary>
    /// Checks if a creature currently has a specific status effect active.
    /// </summary>
    /// <param name="creature">The creature object ID to check</param>
    /// <param name="type">The type of status effect to check for</param>
    /// <returns>True if the creature has the specified status effect, false otherwise</returns>
    bool HasEffect(uint creature, StatusEffectType type);
}