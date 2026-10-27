using SWLOR.CraftSim.Play;
using SWLOR.CraftSim.Scenario;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Kitchen;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Scenario.Kitchen
{
    /// <summary>
    /// Plugs the Kitchen (Agriculture cooking, pet food and cooking enhancements) into the balance gates. It builds the real
    /// <see cref="KitchenEngine"/> from each recipe's stored <see cref="KitchenParameters"/>, the crafter's rank and the gear worn,
    /// and maps enhancements the way the Kitchen window does: the total progress penalty becomes extra Doneness. Played unperked, it
    /// compares the two Chef styles for gate 2; the Chef scenarios play a cook who owns a style and the rest of the line.
    /// </summary>
    public class KitchenScenario : ICraftScenario
    {
        public virtual string Name => "kitchen";

        public virtual string Title => "Kitchen";

        public SkillType Skill => SkillType.Agriculture;

        public CraftMinigameType Minigame => CraftMinigameType.Kitchen;

        /// <summary>
        /// A starting estimate for a new minigame, until the client evidence measures it: the Kitchen asks for a choice among up to
        /// fourteen actions, each with a range to read, so a little more than the old window's 6 s. It is the pace the server holds a
        /// craft's XP to (<see cref="CraftMinigameXp.ActionSeconds"/>).
        /// </summary>
        public double SecondsPerAction => CraftMinigameXp.ActionSeconds;

        /// <summary>The Kitchen's multiplier at the lowest level band, the largest; <see cref="XpMultiplierFor"/> gives each band's.</summary>
        public double XpMultiplier => CraftMinigameXp.MultiplierFor(Minigame);

        public double XpMultiplierFor(int recipeLevel) => CraftMinigameXp.MultiplierFor(Minigame, recipeLevel);

        public EnhancementSupport Enhancements => EnhancementSupport.Modeled;

        public AdaptivePolicyOptions AdaptiveOptions { get; } = new()
        {
            Depth = 3,
            ForecastSamples = 1,
            SampleBudget = 1500,
            Heuristic = new KitchenLeafHeuristic()
        };

        /// <summary>
        /// The two Chef styles, compared alone, with the rest of the line each cook owns at the rank the recipe is played at (the
        /// purchases that rank allows, Seasoned Palate switched off) and with Seasoned Palate switched on, from rank 20 where the style opens.
        /// </summary>
        public virtual IReadOnlyList<CraftStylePair> StylePairs => new[]
        {
            new CraftStylePair(KitchenBuild.SlowCookingAlone.AsStyle(), KitchenBuild.FlashCookingAlone.AsStyle()),
            new CraftStylePair(KitchenBuild.SlowCookingLine.AsStyle(), KitchenBuild.FlashCookingLine.AsStyle()),
            new CraftStylePair(KitchenBuild.SlowCookingLineSeasoned.AsStyle(), KitchenBuild.FlashCookingLineSeasoned.AsStyle())
        };

        public ICraftMinigameEngine CreateEngine(CraftEngineRequest request)
        {
            var detail = request.Recipe.Detail
                         ?? throw new InvalidOperationException($"{request.Recipe.Type} has no recipe definition to read Kitchen parameters from.");
            if (detail.MinigameParameters is not KitchenParameters parameters)
                throw new InvalidOperationException($"{request.Recipe.Type} has no Kitchen parameters.");

            var recipe = KitchenRecipe.From(parameters, request.Recipe.Level, request.Recipe.Components);
            var stats = new KitchenStats(request.Rank, request.Gear.Craftsmanship, request.Gear.Control, request.Gear.CpBonus, PerkStatsFor(request));
            return new KitchenEngine(
                request.Random,
                recipe,
                stats,
                KitchenTuning.EnhancementDoneness(request.Enhancements.TotalProgressPenalty));
        }

        /// <summary>The Chef perks the engine is built with: the request's style, or none.</summary>
        protected virtual KitchenPerkStats PerkStatsFor(CraftEngineRequest request) => request.Style?.Configuration switch
        {
            KitchenBuild build => build.StatsAt(request.Rank),
            KitchenPerkStats stats => stats,
            Func<int, KitchenPerkStats> atRank => atRank(request.Rank),
            _ => KitchenPerkStats.None
        };

        public virtual IReadOnlyList<string> Limitations => new[]
        {
            "The unperked scenario plays no perks except in the style-balance gate, which compares the two Chef styles. A Chef is played with the numbers of the game's own perk definitions at the rank the recipe is played at.",
            "Seasoned Palate is a switch the cook makes for each dish. The Chef scenarios play it off; the Seasoned Palate scenarios play it on for every dish. A real cook would choose per dish.",
            "Seconds per action is an estimate (8 s) until the client evidence measures it.",
            "Ingredient names are the resrefs here; the window shows item names."
        };
    }
}
