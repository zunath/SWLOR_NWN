using SWLOR.Game.Server.Domain.Crafting;
using SWLOR.Game.Server.Domain.Crafting.Minigame;

namespace SWLOR.Game.Server.Domain.Crafting;

/// <summary>
/// Starts, tracks and cancels each player's quick craft batch. Static callers reach
/// the container's instance through <c>GameServices.CraftQuickCraft</c>.
/// </summary>
public interface ICraftQuickCraftService
{
    bool IsRunning(uint player);

    /// <summary>Whether the player may quick craft the recipe now, and why not when they may not.</summary>
    QuickCraftEligibility Evaluate(uint player, RecipeType recipeType);

    /// <summary>How many items a batch of <paramref name="requested"/> can make with the materials the player carries.</summary>
    int ResolveBatchSize(uint player, RecipeType recipeType, int requested);

    /// <summary>
    /// Starts a batch after checking everything again; the window's enabled state is not trusted. Returns the reason when it
    /// does not start. <paramref name="onEnded"/> runs when the batch ends for any reason.
    /// </summary>
    string Start(uint player, RecipeType recipeType, int requested, Action onEnded);

    void Cancel(uint player);

    /// <summary>Stops the batch because the player died or left.</summary>
    void Interrupt(uint player);
}
