using System.Linq;

namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>When a recipe may be quick crafted and how many can be made in one batch.</summary>
public static class QuickCraftRules
{
    /// <summary>Quick craft opens at a skill rank this far above the recipe level, where crafting already gives no XP.</summary>
    public const int RankMargin = 5;

    /// <summary>The most items one batch makes.</summary>
    public const int MaxBatchSize = 10;

    /// <summary>
    /// Quick craft needs the skill rank to be at least the recipe level plus <see cref="RankMargin"/>, and one successful manual craft
    /// of the recipe on record.
    /// </summary>
    /// <param name="recipeLevel">The recipe's level.</param>
    /// <param name="skillName">The crafting skill's name, for the reason shown.</param>
    /// <param name="skillRank">The player's rank in that skill.</param>
    /// <param name="craftedByHand">True when <c>Player.CraftedRecipes</c> holds the recipe.</param>
    public static QuickCraftEligibility Evaluate(int recipeLevel, string skillName, int skillRank, bool craftedByHand)
    {
        var requiredRank = recipeLevel + RankMargin;
        if (skillRank < requiredRank)
            return QuickCraftEligibility.Refused($"Quick craft needs {skillName} rank {requiredRank}.");

        if (!craftedByHand)
            return QuickCraftEligibility.Refused("Craft this recipe by hand once to unlock quick craft.");

        return QuickCraftEligibility.Eligible;
    }

    /// <summary>
    /// How many items a batch can make: the quantity asked for, at most <see cref="MaxBatchSize"/>, and no more than the
    /// materials in the inventory cover.
    /// </summary>
    /// <param name="requested">The quantity the player asked for.</param>
    /// <param name="recipe">The recipe to craft.</param>
    /// <param name="countInInventory">Counts the player's inventory items of a resref, stack sizes included.</param>
    public static int ResolveBatchSize(int requested, RecipeDetail recipe, Func<string, int> countInInventory)
    {
        var size = Math.Clamp(requested, 0, MaxBatchSize);
        if (recipe.Components.Count == 0)
            return size;

        var affordable = recipe.Components.Min(component =>
            component.Value <= 0 ? MaxBatchSize : countInInventory(component.Key) / component.Value);

        return Math.Min(size, affordable);
    }
}
