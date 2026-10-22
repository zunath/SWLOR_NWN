using SWLOR.CraftSim.Play;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Forge;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Scenario.Forge
{
    /// <summary>
    /// Smithery's Plasma Forge for the balance gates. Each attempt is the real <see cref="ForgeEngine"/> built from the recipe's own
    /// <see cref="ForgeParameters"/>, the crafter's rank and the gear's Craftsmanship, Control and CP bonus, exactly as the window builds it.
    /// </summary>
    public sealed class ForgeScenario : ICraftScenario
    {
        private static readonly AdaptivePolicyOptions Adaptive = new()
        {
            // The Forge ends the moment Shape is full, so a skilled player keeps the integrity to finish Shape rather than hone to the last point.
            Heuristic = new DefaultLeafHeuristic(successBonus: 0.85, safetyMargin: 3.0, progressPull: 0.02)
        };

        public string Name => "forge";

        public string Title => "Plasma Forge";

        public SkillType Skill => SkillType.Smithery;

        public CraftMinigameType Minigame => CraftMinigameType.Forge;

        /// <summary>An estimate for the new window until the client evidence measures it; the old window's is 6.</summary>
        public double SecondsPerAction => 8.0;

        /// <summary>The Forge's multiplier at the lowest level band, the largest; <see cref="XpMultiplierFor"/> gives each band's.</summary>
        public double XpMultiplier => CraftMinigameXp.MultiplierFor(Minigame);

        public double XpMultiplierFor(int recipeLevel) => CraftMinigameXp.MultiplierFor(Minigame, recipeLevel);

        public EnhancementSupport Enhancements => EnhancementSupport.Modeled;

        public AdaptivePolicyOptions AdaptiveOptions => Adaptive;

        public IReadOnlyList<string> Limitations => new[]
        {
            "Enhancements raise the Shape the recipe needs by their progress penalty through ForgeRules.EnhancementShapePercent; their other effects are not played.",
            "No perks or styles exist yet; Hot Forging and Cold Working are drafts, so the style-balance gate is not applicable.",
            "The seconds per action (8) is an estimate for a window nobody has played; client timing replaces it.",
            "The adaptive player reads only the shared meters and previews, so it plans Thermal worse than a person who reads the range; gate 1 compares it with a solver that searches whole rotations."
        };

        public ICraftMinigameEngine CreateEngine(CraftEngineRequest request)
        {
            var parameters = request.Recipe.Detail?.MinigameParameters as ForgeParameters
                             ?? throw new InvalidOperationException($"{request.Recipe.Type} has no Plasma Forge parameters.");

            var setup = new ForgeSetup(
                parameters,
                request.Recipe.Level,
                request.Rank,
                request.Gear.Craftsmanship,
                request.Gear.Control,
                request.Gear.CpBonus,
                ForgeRules.EnhancementShapePercent(request.Enhancements.TotalProgressPenalty, request.Recipe.Level));

            return new ForgeEngine(setup, request.Random);
        }
    }
}
