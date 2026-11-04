using SWLOR.CraftSim.Data;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Experiment;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Scenario.Experiment
{
    /// <summary>
    /// The research Experiment as a harness scenario, so <c>gates --scenario experiment --gates length</c> checks its 5-8 turn target.
    /// The cohort is every researchable recipe (weapons, armor and food of any crafting skill) in its research tier at the lowest
    /// Fabrication rank that can research it; each recipe is played at one of the ten research levels, cycling by recipe so the sample
    /// covers fixed and random levels evenly. The generic item-craft gates (fixed rotation, styles, perks, material parity and the
    /// legacy XP comparison) do not apply to an experiment, which has no quality, styles or legacy loop; the <c>research</c> command
    /// reports what does: magnitudes against today's dice, the time cut, and XP per real hour.
    /// </summary>
    public sealed class ExperimentScenario : ICraftScenario
    {
        /// <summary>The recipe tiers as bands: tier 1 is recipe levels 1-9, then ten levels each, tier 5 from level 40.</summary>
        public static readonly IReadOnlyList<RecipeBand> TierBands = new[]
        {
            new RecipeBand("Tier 1 (levels 1-9)", 1, 9),
            new RecipeBand("Tier 2 (10-19)", 10, 19),
            new RecipeBand("Tier 3 (20-29)", 20, 29),
            new RecipeBand("Tier 4 (30-39)", 30, 39),
            new RecipeBand("Tier 5 (40+)", 40, int.MaxValue)
        };

        public string Name => "experiment";

        public string Title => "Research Experiment";

        public SkillType Skill => SkillType.Fabrication;

        public CraftMinigameType Minigame => CraftMinigameType.Experiment;

        public double SecondsPerAction => 6;

        public LengthTarget LengthTarget => LengthTarget.ResearchExperiment;

        public ScenarioCohort Cohort => new(
            recipe => recipe.Detail != null && IsResearchable(recipe.Detail),
            RankFor,
            TierBands);

        public IReadOnlyList<string> Limitations => new[]
        {
            "The experiment has no quality, styles or perks yet, so gates 1, 2, 3 and 5 do not apply and gate 6 has no legacy loop to compare with; run `--gates length`.",
            "Each recipe is played at one research level, chosen from its identity so the sample covers all ten. A random level is played in its two-candidate form (credit and time reduction); when licensed runs are rolled it plays like a fixed level.",
            "The adaptive player values evidence (the leader's plus what Analyze spent) and ignores the time cut and the bonus size, so it is a floor on how long a skilled player stays in the experiment, not a model of what players choose."
        };

        public ICraftMinigameEngine CreateEngine(CraftEngineRequest request)
        {
            var level = ResearchLevelFor(request.Recipe);
            var setup = ExperimentSetup.ForRecipe(request.Recipe.Detail ?? throw new InvalidOperationException("The recipe has no definition."),
                level, request.Rank, request.Gear.CpBonus, extraBonus: false);
            return new ExperimentEngine(request.Random, setup);
        }

        /// <summary>The research level a recipe is played at: 1 to 10, cycling by the recipe's identity.</summary>
        public static int ResearchLevelFor(RecipeEntry recipe) => 1 + (int)recipe.Type % 10;

        /// <summary>The tier of a recipe, 1 to 5, as the research window computes it.</summary>
        public static int TierOf(int recipeLevel) => Math.Min(CraftRankMilestones.MaxResearchTier, recipeLevel / CraftRankMilestones.RanksPerTier + 1);

        /// <summary>The lowest Fabrication rank that can research a recipe.</summary>
        public static int RankFor(RecipeEntry recipe) => CraftRankMilestones.RequiredFabricationRankForResearchTier(TierOf(recipe.Level));

        /// <summary>The server's rule for researchable recipes: weapons, armor and food.</summary>
        public static bool IsResearchable(RecipeDetail recipe) =>
            recipe.EnhancementType is RecipeEnhancementType.Weapon or RecipeEnhancementType.Armor or RecipeEnhancementType.Food;
    }
}
