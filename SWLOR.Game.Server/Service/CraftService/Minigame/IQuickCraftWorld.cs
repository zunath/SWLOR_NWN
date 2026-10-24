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

    /// <summary>Takes exactly one craft's components. Returns false, taking nothing, when any are missing.</summary>
    bool TryConsumeComponents(uint player, RecipeDetail recipe);

    /// <summary>Creates the recipe's base item for the player.</summary>
    void CreateItem(uint player, RecipeDetail recipe);

    /// <summary>Runs <paramref name="action"/> after the delay.</summary>
    void Schedule(float seconds, Action action);

    void Message(uint player, string text);
}
