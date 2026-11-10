using System;

namespace SWLOR.Game.Server.Domain.Crafting.Minigame;

/// <summary>
/// Rules that apply only to recipes above the highest skill rank. Recipes at or below that level are unaffected.
/// </summary>
public static class TopRecipeRules
{
    /// <summary>The highest recipe level that plays by the ordinary rules.</summary>
    public const int LastOrdinaryLevel = 50;

    /// <summary>Control up to this much counts in full on a top recipe. Evenly split fully modded gear sits just under it.</summary>
    public const int ControlSoftCap = 120;

    /// <summary>What each point of Control above <see cref="ControlSoftCap"/> is worth on a top recipe.</summary>
    public const double ControlAboveSoftCapValue = 0.25;

    public static bool IsTopRecipe(int recipeLevel) => recipeLevel > LastOrdinaryLevel;

    /// <summary>The Control a minigame's output reads: the crafter's own, with the part above the soft cap discounted on a top recipe.</summary>
    public static double EffectiveControl(int control, int recipeLevel) =>
        !IsTopRecipe(recipeLevel) || control <= ControlSoftCap
            ? control
            : ControlSoftCap + (control - ControlSoftCap) * ControlAboveSoftCapValue;
}
