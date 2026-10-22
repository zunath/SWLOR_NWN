using SWLOR.Game.Server.Service.CraftService.Minigame.Fabricator;
using SWLOR.Game.Server.Service.CraftService.Minigame.Line;

namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>
/// The base XP of a craft played in a new minigame: today's XP for the recipe, times the multiplier that keeps the minigame's XP per
/// hour at least the old window's (CraftSim gate 6). A minigame with no multiplier pays today's XP. A multiplier may differ by the
/// recipe's level band, because the old window is fastest in the lowest bands.
/// </summary>
public static class CraftMinigameXp
{
    /// <summary>
    /// The multiplier a minigame puts on its crafts' base XP at a recipe level; 1 when it needs none. A level of 0 or less asks for the
    /// lowest band's, which is the largest.
    /// </summary>
    public static double MultiplierFor(CraftMinigameType minigame, int recipeLevel = 0) => minigame switch
    {
        CraftMinigameType.Fabricator => FabricatorRules.XpMultiplierForLevel(recipeLevel),
        CraftMinigameType.Line => LineRules.XpMultiplierForLevel(recipeLevel),
        _ => 1.0
    };

    /// <summary>Scales a base XP by a minigame's multiplier, rounding to the nearest whole XP.</summary>
    public static int ScaleBaseXp(int baseXp, double multiplier) =>
        (int)Math.Round(baseXp * multiplier, MidpointRounding.AwayFromZero);

    /// <summary>
    /// The XP a craft of the recipe is worth before bonuses: <c>Craft.GetBaseRecipeXP</c> times the multiplier of the minigame that
    /// crafts the recipe's skill, at the recipe's level.
    /// </summary>
    public static int BaseXp(RecipeDetail recipe, int skillRank)
    {
        var baseXp = Craft.GetBaseRecipeXP(recipe, skillRank);
        var route = CraftMinigameRouting.GetRoute(recipe.Skill);
        return route.Minigame is { } minigame ? ScaleBaseXp(baseXp, MultiplierFor(minigame, recipe.Level)) : baseXp;
    }
}
