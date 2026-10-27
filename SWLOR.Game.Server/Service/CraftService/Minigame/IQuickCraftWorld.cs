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

    /// <summary>
    /// Takes exactly one craft's components and lists what was taken in <paramref name="taken"/>, each as a serialized item, so
    /// <see cref="RestoreComponents"/> can give them back. Returns false, taking nothing, when any are missing.
    /// </summary>
    bool TryConsumeComponents(uint player, RecipeDetail recipe, List<string> taken);

    /// <summary>
    /// Creates the recipe's base item in the player's inventory. Returns false, leaving no item anywhere, when it did not end up in the
    /// inventory because there was no room.
    /// </summary>
    bool CreateItem(uint player, RecipeDetail recipe);

    /// <summary>
    /// Gives back the components <see cref="TryConsumeComponents"/> took. What does not fit the inventory is held on the player's record
    /// until there is room (the same holding a craft session uses). Returns true when everything went back into the inventory.
    /// </summary>
    bool RestoreComponents(uint player, List<string> taken);

    /// <summary>Writes a line to the crafting log.</summary>
    void LogCraft(uint player, string text);

    /// <summary>Runs <paramref name="action"/> after the delay.</summary>
    void Schedule(float seconds, Action action);

    void Message(uint player, string text);
}
