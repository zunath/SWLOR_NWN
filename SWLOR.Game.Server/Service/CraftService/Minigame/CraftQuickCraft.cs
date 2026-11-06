using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Crafting;

namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>Starts, tracks and cancels each player's quick craft batch.</summary>
public static class CraftQuickCraft
{
    private static ICraftQuickCraftService Instance => FacadeAccessor.Resolve<ICraftQuickCraftService>();

    public static bool IsRunning(uint player)
    {
        return Instance.IsRunning(player);
    }

    /// <summary>Whether the player may quick craft the recipe now, and why not when they may not.</summary>
    public static QuickCraftEligibility Evaluate(uint player, RecipeType recipeType)
    {
        return Instance.Evaluate(player, recipeType);
    }

    /// <summary>How many items a batch of <paramref name="requested"/> can make with the materials the player carries.</summary>
    public static int ResolveBatchSize(uint player, RecipeType recipeType, int requested)
    {
        return Instance.ResolveBatchSize(player, recipeType, requested);
    }

    /// <summary>
    /// Starts a batch after checking everything again; the window's enabled state is not trusted. Returns the reason when it
    /// does not start. <paramref name="onEnded"/> runs when the batch ends for any reason.
    /// </summary>
    public static string Start(uint player, RecipeType recipeType, int requested, Action onEnded)
    {
        return Instance.Start(player, recipeType, requested, onEnded);
    }

    public static void Cancel(uint player)
    {
        Instance.Cancel(player);
    }

    /// <summary>Stops the batch because the player died or left.</summary>
    public static void Interrupt(uint player)
    {
        Instance.Interrupt(player);
    }
}
