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
    /// and maps enhancements the way the Kitchen window does: the total progress penalty becomes extra Doneness.
    /// </summary>
    public sealed class KitchenScenario : ICraftScenario
    {
        public string Name => "kitchen";

        public string Title => "Kitchen";

        public SkillType Skill => SkillType.Agriculture;

        public CraftMinigameType Minigame => CraftMinigameType.Kitchen;

        /// <summary>
        /// A starting estimate for a new minigame, until the client evidence measures it: the Kitchen asks for a choice among up to
        /// fourteen actions, each with a range to read, so a little more than the old window's 6 s.
        /// </summary>
        public double SecondsPerAction => 8;

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

        public ICraftMinigameEngine CreateEngine(CraftEngineRequest request)
        {
            var detail = request.Recipe.Detail
                         ?? throw new InvalidOperationException($"{request.Recipe.Type} has no recipe definition to read Kitchen parameters from.");
            if (detail.MinigameParameters is not KitchenParameters parameters)
                throw new InvalidOperationException($"{request.Recipe.Type} has no Kitchen parameters.");

            var recipe = KitchenRecipe.From(parameters, request.Recipe.Level, request.Recipe.Components);
            var stats = new KitchenStats(request.Rank, request.Gear.Craftsmanship, request.Gear.Control, request.Gear.CpBonus);
            return new KitchenEngine(
                request.Random,
                recipe,
                stats,
                KitchenTuning.EnhancementDoneness(request.Enhancements.TotalProgressPenalty));
        }

        public IReadOnlyList<string> Limitations => new[]
        {
            "The Kitchen has no perks yet: the Chef line is a draft, so gate 2 does not apply and gates run without styles.",
            "Seconds per action is an estimate (8 s) until the client evidence measures it.",
            "Ingredient names are the resrefs here; the window shows item names."
        };
    }
}
