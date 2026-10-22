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

        /// <summary>The Reaction's multiplier on a craft's base XP, the same constant the game applies.</summary>
        public double XpMultiplier => CraftMinigameXp.MultiplierFor(Minigame);

        public AdaptivePolicyOptions AdaptiveOptions => new() { Depth = 5, ForecastSamples = 1, SampleBudget = 6000 };

        public IReadOnlyList<string> Limitations => new[]
        {
            "Espionage has no crafting gear, so the Reaction reads Espionage rank only (Craftsmanship, Control and CP equivalents). The gear tier is ignored and every gear tier is the same rank-only setup; the harness offers only \"no gear\" for Espionage.",
            "The Reaction has no random event after the attempt starts. The only chance is the starting position, rolled anywhere in the cycle, so every attempt of a recipe differs only by its start.",
            "Espionage XP is practice XP (at least 150 below the recipe's SkillUpToRank limit), taken from `Craft.GetBaseRecipeXP` for both the legacy loop and the Reaction; the recipes are the 15 Espionage recipes, so each band has only a few and the sample is the whole band.",
            "The Reaction offers no styles (Espionage gets no perk lines), so the style-balance gate is not applicable. Gate 3 has recipes only in the 41-45 and 46-50 bands; the game has no Espionage recipe above level 50."
        };

        public virtual ICraftMinigameEngine CreateEngine(CraftEngineRequest request)
        {
            var detail = request.Recipe.Detail
                         ?? throw new InvalidOperationException($"{request.Recipe.Type} has no server definition, so it has no Reaction parameters.");
            return new ReactionEngine(request.Random, ReactionSettings.ForRecipe(detail, request.Rank));
        }
    }
}
