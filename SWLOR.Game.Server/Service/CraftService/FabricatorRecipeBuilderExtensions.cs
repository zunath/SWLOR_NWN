namespace SWLOR.Game.Server.Service.CraftService;

/// <summary>The recipe builder method that sets a recipe's Fabricator minigame values.</summary>
public static class FabricatorRecipeBuilderExtensions
{
    /// <summary>Sets the Fabricator minigame values of the recipe. See <see cref="FabricatorParameters"/>.</summary>
    public static RecipeBuilder FabricatorParameters(
        this RecipeBuilder builder,
        int frameSizePercent,
        int shellSizePercent,
        int frameQualityPercent,
        int shellQualityPercent,
        int wearRate) =>
        builder.MinigameParameters(
            new FabricatorParameters(frameSizePercent, shellSizePercent, frameQualityPercent, shellQualityPercent, wearRate));
}
