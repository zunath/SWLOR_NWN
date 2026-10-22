using SWLOR.CraftSim.Play;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Reaction;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Scenario.Reaction
{
    /// <summary>
    /// Plugs Espionage's Reaction into the balance gates. The engine is built by the game's own <see cref="ReactionSettings"/>: the
    /// recipe's parameters and the crafter's Espionage rank are all it reads, because Espionage has no crafting gear. Every gear tier
    /// the harness offers therefore plays the same rank-only setup (the harness lists only "no gear" for Espionage).
    /// </summary>
    public class ReactionScenario : ICraftScenario
    {
        public virtual string Name => "reaction";

        public virtual string Title => "Reaction";

        public SkillType Skill => SkillType.Espionage;

        public CraftMinigameType Minigame => CraftMinigameType.Reaction;

        /// <summary>The starting estimate for a new minigame's window; the client evidence replaces it.</summary>
        public double SecondsPerAction => 8.0;

        /// <summary>The Reaction's multiplier at the lowest level band, the largest; <see cref="XpMultiplierFor"/> gives each band's.</summary>
        public double XpMultiplier => CraftMinigameXp.MultiplierFor(Minigame);

        /// <summary>The Reaction's multiplier on a craft's base XP at a recipe level, the same one the game applies.</summary>
        public double XpMultiplierFor(int recipeLevel) => CraftMinigameXp.MultiplierFor(Minigame, recipeLevel);

        public EnhancementSupport Enhancements => EnhancementSupport.NoSlots;

        public AdaptivePolicyOptions AdaptiveOptions => new() { Depth = 5, ForecastSamples = 1, SampleBudget = 6000 };

        /// <summary>The crafter's rank against the recipe's level: 0 is the plan's at-level cohort, -3 is the lowest rank that can craft it.</summary>
        protected virtual int RankOffset => 0;

        /// <summary>False for an analysis variant that measures what Dampen is worth by taking it away.</summary>
        protected virtual bool OffersDampen => true;

        /// <summary>True when the player uses Dampen as a prudent crafter does (see <see cref="ReactionPlayerEngine"/>).</summary>
        protected virtual bool PrudentPlayer => true;

        public ScenarioCohort Cohort => new(
            recipe => recipe.Skill == Skill,
            recipe => Math.Clamp(ScenarioCohort.AtLevelRank(recipe) + RankOffset, 0, ScenarioCohort.MaximumRank));

        public IReadOnlyList<string> Limitations => new[]
        {
            "Espionage has no crafting gear, so the Reaction reads Espionage rank only (Craftsmanship, Control and CP equivalents). The gear tier is ignored and every gear tier is the same rank-only setup; the harness offers only \"no gear\" for Espionage.",
            "The Reaction has no random event after the attempt starts. The only chance is the starting position, rolled anywhere in the cycle, so every attempt of a recipe differs only by its start.",
            "Espionage XP is practice XP (at least 150 below the recipe's SkillUpToRank limit), taken from `Craft.GetBaseRecipeXP` for both the legacy loop and the Reaction; the recipes are the 15 Espionage recipes, so each band has only a few and the sample is the whole band.",
            "The Reaction offers no styles (Espionage gets no perk lines), so the style-balance gate is not applicable. Gate 3 has recipes only in the 41-45 and 46-50 bands; the game has no Espionage recipe above level 50.",
            "Espionage recipes have no enhancement slots, so the enhancement cases of the XP-rate gate and the enhancement-parity gate do not apply.",
            "The adaptive player values neither CP beyond its horizon nor a full Stability bar, so the scenario has it play Dampen as a prudent crafter does (`ReactionPlayerEngine`): it never spends the CP its next Stabilize needs on Dampen, and never Dampens while the Stability Dampen restores is not missing. Without those rules it spends the CP and then breaks the piece, or stalls."
        };

        public ICraftMinigameEngine CreateEngine(CraftEngineRequest request)
        {
            var detail = request.Recipe.Detail
                         ?? throw new InvalidOperationException($"{request.Recipe.Type} has no server definition, so it has no Reaction parameters.");
            var engine = new ReactionEngine(request.Random, ReactionSettings.ForRecipe(detail, request.Rank));
            return new ReactionPlayerEngine(engine, OffersDampen, PrudentPlayer);
        }
    }
}
