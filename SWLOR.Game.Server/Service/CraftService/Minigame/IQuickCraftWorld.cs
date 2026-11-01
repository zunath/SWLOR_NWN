using System.Collections.Generic;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>What a quick craft batch needs from the game engine. <c>NwnQuickCraftWorld</c> is the engine implementation.</summary>
public interface IQuickCraftWorld
{
    /// <summary>True while the player can keep crafting: still in the game, alive and not moved away.</summary>
    bool IsPlayerAvailable(uint player);

    /// <summary>Holds the player at the station for the whole batch.</summary>
    void BeginBatch(uint player);

    /// <summary>Starts the station's looping work clip for the whole batch, where one can play. <see cref="EndBatch"/> stops it.</summary>
    void PlayStationWork(uint player, SkillType skill);

    void EndBatch(uint player);

    /// <summary>
    /// Shows the progress of one item. The native crafting pose plays only when the batch has no station clip, as for a rifle
    /// holder.
    /// </summary>
    void PlayItemAnimation(uint player, float seconds);

    /// <summary>True when the player holds every component for one craft. Takes nothing.</summary>
    bool HasComponents(uint player, RecipeDetail recipe);

    /// <summary>
    /// Creates the recipe's base item in the player's inventory. Returns false, leaving no item anywhere and taking nothing, when it did
    /// not end up in the inventory because there was no room.
    /// </summary>
    bool CreateItem(uint player, RecipeDetail recipe);

    /// <summary>Takes exactly one craft's components. Returns false, taking nothing, when any are missing.</summary>
    bool TryConsumeComponents(uint player, RecipeDetail recipe);

    /// <summary>Removes the base item <see cref="CreateItem"/> just made, when the components could not be taken after all.</summary>
    void RemoveCreatedItem(uint player, RecipeDetail recipe);

    /// <summary>Writes a line to the crafting log.</summary>
    void LogCraft(uint player, string text);

    /// <summary>Runs <paramref name="action"/> after the delay.</summary>
    void Schedule(float seconds, Action action);

    void Message(uint player, string text);
}
