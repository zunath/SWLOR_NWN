using SWLOR.Game.Server.Service.CraftService.Minigame.Circuit;
using SWLOR.Game.Server.Service.CraftService.Minigame.Fabricator;
using SWLOR.Game.Server.Service.CraftService.Minigame.Kitchen;
using SWLOR.Game.Server.Service.CraftService.Minigame.Line;
using SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;
using SWLOR.Game.Server.Service.CraftService.Minigame.Reaction;
using SWLOR.Game.Server.Service.CraftService.Minigame.Search;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>
/// The base XP of a craft played in a new minigame: today's XP for the recipe, times the multiplier that keeps the minigame's XP per
/// hour at least the old window's (CraftSim gate 6). A minigame with no multiplier pays today's XP. A multiplier may differ by the
/// recipe's level band, because the old window is fastest in the lowest bands. The gathering minigames are not recipes: the Line's
/// level is the fish's, and the Search board's is the scavenge site's.
/// </summary>
public static class CraftMinigameXp
{
    /// <summary>
    /// The crafting pace multiplier (owner decision, October 2026, workstream 11): crafting XP in Smithery, Engineering, Fabrication and
    /// Agriculture's cooking is this much more than the recipe and minigame say. It is layered on top of a minigame's gate-6 multiplier, so the
    /// gate stays measured against today's XP. It does not apply to Gathering, Espionage, fishing, guild hand-ins, research, combat or Piloting.
    /// </summary>
    public const double CraftingPaceMultiplier = 1.05;

    /// <summary>True for the skills the crafting pace multiplier applies to.</summary>
    public static bool AppliesCraftingPace(SkillType skill) =>
        skill is SkillType.Smithery or SkillType.Engineering or SkillType.Fabrication or SkillType.Agriculture;

    /// <summary>The crafting pace multiplier for a skill's crafts: <see cref="CraftingPaceMultiplier"/>, or 1 for any other skill.</summary>
    public static double PaceMultiplierFor(SkillType skill) => AppliesCraftingPace(skill) ? CraftingPaceMultiplier : 1.0;

    /// <summary>
    /// The multiplier a minigame puts on its crafts' base XP at a recipe level; 1 when it needs none. A level of 0 or less asks for the
    /// lowest band's, which is the largest. For Prospecting the level is the vein's (<c>GatheringLevels.ForTier</c>), and the multiplier
    /// applies to the units a session gathers, never to Quick Harvest.
    /// </summary>
    public static double MultiplierFor(CraftMinigameType minigame, int recipeLevel = 0) => minigame switch
    {
        CraftMinigameType.Circuit => CircuitRules.XpMultiplierFor(recipeLevel),
        CraftMinigameType.Fabricator => FabricatorRules.XpMultiplierForLevel(recipeLevel),
        CraftMinigameType.Kitchen => KitchenTuning.XpMultiplierForLevel(recipeLevel),
        CraftMinigameType.Line => LineRules.XpMultiplierForLevel(recipeLevel),
        CraftMinigameType.Prospecting => ProspectingRules.XpMultiplierFor(recipeLevel),
        CraftMinigameType.Reaction => ReactionRules.XpMultiplier(recipeLevel),
        CraftMinigameType.Search => SearchRules.XpMultiplierForLevel(recipeLevel),
        _ => 1.0
    };

    /// <summary>Scales a base XP by a minigame's multiplier, rounding to the nearest whole XP.</summary>
    public static int ScaleBaseXp(int baseXp, double multiplier) =>
        (int)Math.Round(baseXp * multiplier, MidpointRounding.AwayFromZero);

    /// <summary>
    /// The XP a craft of the recipe is worth before bonuses: <c>Craft.GetBaseRecipeXP</c> times the multiplier of the minigame that
    /// crafts the recipe's skill, at the recipe's level, times the crafting pace multiplier of the recipe's skill.
    /// </summary>
    public static int BaseXp(RecipeDetail recipe, int skillRank)
    {
        var baseXp = Craft.GetBaseRecipeXP(recipe, skillRank);
        var gated = CraftMinigameRouting.TryGetRoute(recipe.Skill, out var route)
            ? ScaleBaseXp(baseXp, MultiplierFor(route.Minigame, recipe.Level))
            : baseXp;
        return ScaleBaseXp(gated, PaceMultiplierFor(recipe.Skill));
    }
}
