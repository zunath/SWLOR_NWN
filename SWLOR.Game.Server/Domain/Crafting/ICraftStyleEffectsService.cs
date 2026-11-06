using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Domain.Crafting;

/// <summary>
/// The crafting style each crafter selected, as far as it needs the database and the engine: recording a selection, forgetting
/// one and applying the style effects again. The record rules are in <see cref="Service.CraftService.CraftingStyleSelection"/>. The
/// static facade <c>Service.CraftService.CraftStyleEffects</c> forwards to it.
/// </summary>
public interface ICraftStyleEffectsService
{
    /// <summary>
    /// Saves the style the crafter selected for <paramref name="skill"/>; <see cref="PerkType.Invalid"/> records no style.
    /// Creatures without a player record are ignored.
    /// </summary>
    void Record(uint creature, SkillType skill, PerkType perk);

    /// <summary>Forgets the selection of <paramref name="skill"/> when it names <paramref name="perk"/>, as a refund of that perk does.</summary>
    void ClearIfSelected(uint creature, SkillType skill, PerkType perk);

    /// <summary>
    /// Makes the player's style effects match the record: each selected style whose perk is still owned is applied, and any other
    /// style effect is removed. A dead player is left alone until they revive.
    /// </summary>
    void Restore(uint creature);
}
