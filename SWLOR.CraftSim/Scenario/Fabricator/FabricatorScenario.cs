using SWLOR.CraftSim.Play;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Fabricator;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Scenario.Fabricator
{
    /// <summary>
    /// Fabrication's item-craft minigame, the Fabricator, for the balance gates. It builds the real engine from the real recipe
    /// definition through <see cref="FabricatorSetup.ForRecipe"/>, the same call the crafting window makes, so the gates play the
    /// rules the game ships and not a copy of them.
    /// </summary>
    public class FabricatorScenario : ICraftScenario
    {
        public virtual string Name => "fabricator";

        public virtual string Title => "Fabricator";

        public SkillType Skill => SkillType.Fabrication;

        public CraftMinigameType Minigame => CraftMinigameType.Fabricator;

        /// <summary>
        /// 8 seconds an action (<see cref="CraftMinigameXp.ActionSeconds"/>, the pace the server holds a craft's XP to): the Fabricator window
        /// shows nine actions and the previews of a three-head game take longer to read than the old window's eleven, which the legacy
        /// default sizes at 6 seconds. An estimate until client timing replaces it.
        /// </summary>
        public double SecondsPerAction => CraftMinigameXp.ActionSeconds;

        /// <summary>
        /// The default search with a heuristic that knows the item is made by Assemble and not by a full progress meter
        /// (<see cref="FabricatorLeafHeuristic"/>).
        /// </summary>
        public AdaptivePolicyOptions AdaptiveOptions => AdaptivePolicyOptions.Default with { Heuristic = new FabricatorLeafHeuristic() };

        /// <summary>The Fabricator's multiplier at the lowest level band, the largest; <see cref="XpMultiplierFor"/> gives each band's.</summary>
        public double XpMultiplier => CraftMinigameXp.MultiplierFor(Minigame);

        public double XpMultiplierFor(int recipeLevel) => CraftMinigameXp.MultiplierFor(Minigame, recipeLevel);

        /// <summary>
        /// The request's total progress penalty reaches the engine as the setup's enhancement progress penalty, the same way the crafting
        /// window passes the selected enhancements': it raises the progress every sub-assembly needs, in proportion to its size.
        /// </summary>
        public EnhancementSupport Enhancements => EnhancementSupport.Modeled;

        /// <summary>
        /// The two Builder styles, compared alone and with the rest of the line each build owns at the rank it is played at (the
        /// purchases that rank allows), from rank 20 where the style opens.
        /// </summary>
        public virtual IReadOnlyList<CraftStylePair> StylePairs => new[]
        {
            new CraftStylePair(FabricatorBuild.ProductionRunAlone.AsStyle(), FabricatorBuild.FineToleranceAlone.AsStyle()),
            new CraftStylePair(FabricatorBuild.ProductionRunLine.AsStyle(), FabricatorBuild.FineToleranceLine.AsStyle())
        };

        public virtual IReadOnlyList<string> Limitations => new[]
        {
            "Prime Feedstock is the only chance in the Fabricator, so the adaptive player's edge over a fixed rotation comes from it and from the recipe's state, not from luck at the actions themselves.",
            "A Builder is played with the numbers of the game's own perk definitions at the rank the recipe is played at. The unperked scenario plays no perks except in the style-balance gate, which compares the styles."
        };

        public ICraftMinigameEngine CreateEngine(CraftEngineRequest request)
        {
            var recipe = request.Recipe;
            var gear = request.Gear;
            var perks = PerkStatsFor(request);
            var setup = recipe.Detail != null
                ? FabricatorSetup.ForRecipe(
                    recipe.Detail, request.Rank, gear.Craftsmanship, gear.Control, gear.CpBonus, request.Enhancements.TotalProgressPenalty, perks)
                : new FabricatorSetup(
                    null,
                    recipe.Level,
                    SWLOR.Game.Server.Service.Craft.GetRecipeLevelDetail(recipe.Level).Progress,
                    request.Rank,
                    gear.Craftsmanship,
                    gear.Control,
                    gear.CpBonus,
                    request.Enhancements.TotalProgressPenalty,
                    perks);

            return new FabricatorEngine(request.Random, setup);
        }

        /// <summary>The Builder perks the engine is built with: the request's style, or none.</summary>
        protected virtual FabricatorPerkStats PerkStatsFor(CraftEngineRequest request) => request.Style?.Configuration switch
        {
            FabricatorBuild build => build.StatsAt(request.Rank),
            FabricatorPerkStats stats => stats,
            _ => FabricatorPerkStats.None
        };
    }
}
