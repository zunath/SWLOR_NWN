using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>
/// Everything a craft session needs from the game engine. <see cref="CraftSessionLifecycle"/> holds the rules and calls only this,
/// so the rules run without the engine; <c>NwnCraftSessionWorld</c> is the engine implementation.
/// </summary>
public interface ICraftSessionWorld
{
    RecipeDetail GetRecipe(RecipeType recipe);

    /// <summary>True when the player meets the recipe's skill and other requirements. It does not check materials.</summary>
    bool CanCraft(uint player, RecipeType recipe);

    /// <summary>Returns the reason the blueprint cannot be used for the recipe now, or an empty string when it can.</summary>
    string ValidateBlueprint(uint player, uint blueprintItem, RecipeType recipe);

    /// <summary>Takes the blueprint's credit cost and one licensed run, and returns its details after the charge.</summary>
    BlueprintDetail ChargeBlueprint(uint player, uint blueprintItem);

    /// <summary>
    /// Takes exactly the recipe's components from the player's inventory, one serialized item per stack taken. When the
    /// inventory lacks any of them, it takes nothing and returns false.
    /// </summary>
    bool TryTakeComponents(uint player, RecipeDetail recipe, List<string> taken);

    /// <summary>Serializes and removes an enhancement the player carries. Returns false when the player no longer carries it.</summary>
    bool TryTakeEnhancement(uint player, uint item, out string serialized);

    /// <summary>Gives a serialized item back to the player. Returns false, leaving the item serialized, when the inventory is full.</summary>
    bool TryReturnItem(uint player, string serialized);

    /// <summary>Keeps the player at the station and walkable-through: the immobilize and collision ghost of a craft in progress.</summary>
    void BeginStationWork(uint player);

    /// <summary>
    /// Starts the looping work clip of the skill's station for as long as the station work lasts, where one can play (not for a
    /// rifle or cannon holder, who keeps the native pose). <see cref="EndStationWork"/> stops it.
    /// </summary>
    void PlayStationWork(uint player, SkillType skill);

    /// <summary>Releases the player from the station and stops the work clip.</summary>
    void EndStationWork(uint player);

    uint CreateCraftedItem(uint player, RecipeDetail recipe);

    void ApplyVendorValue(uint item, int addGoldPiece);

    /// <summary>Moves the properties of a serialized enhancement onto the crafted item.</summary>
    void ApplyEnhancement(uint item, string serializedEnhancement, RecipeDetail recipe);

    void ApplyFoodBonus(uint item, int durationSeconds, int bonusCharges);

    /// <summary>Applies the blueprint's random and guaranteed bonuses and marks the item with the blueprint level.</summary>
    void ApplyBlueprintBonuses(uint player, uint item, RecipeDetail recipe, BlueprintDetail blueprint);

    void GiveXp(uint player, SkillType skill, int xp);

    void SaveRecord(Player dbPlayer);

    void Message(uint player, string text);

    void LogCraft(uint player, string text);
}
