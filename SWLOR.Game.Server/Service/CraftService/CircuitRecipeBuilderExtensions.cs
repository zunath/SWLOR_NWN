namespace SWLOR.Game.Server.Service.CraftService;

/// <summary>The recipe builder method that sets a recipe's Circuit minigame values.</summary>
public static class CircuitRecipeBuilderExtensions
{
    /// <summary>Sets the Circuit minigame values of the recipe. See <see cref="CircuitParameters"/>.</summary>
    public static RecipeBuilder CircuitParameters(
        this RecipeBuilder builder,
        int overloadLine,
        int loadBoostPercent,
        int assemblySharePercent) =>
        builder.MinigameParameters(new CircuitParameters(overloadLine, loadBoostPercent, assemblySharePercent));
}
