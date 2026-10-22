using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Circuit;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Scenario.Circuit
{
    /// <summary>
    /// Plugs the Engineering Circuit into the balance gates. It builds the real <see cref="CircuitEngine"/> from the recipe's own
    /// parameters, the level chart, the crafter's rank and the gear worn, through <see cref="CircuitSetup"/>, so the gates play the
    /// rules the game plays.
    /// </summary>
    public sealed class CircuitScenario : ICraftScenario
    {
        public string Name => "circuit";

        public string Title => "Circuit";

        public SkillType Skill => SkillType.Engineering;

        public CraftMinigameType Minigame => CraftMinigameType.Circuit;

        /// <summary>An estimate: the window shows each action's result and risk, so a turn takes a little longer than the old window's 6 s. Client timing replaces it.</summary>
        public double SecondsPerAction => 8.0;

        /// <summary>The Circuit's multiplier depends on the recipe's level band: see <see cref="XpMultiplierFor"/>.</summary>
        public double XpMultiplier => 1.0;

        public double XpMultiplierFor(int recipeLevel) => CraftMinigameXp.MultiplierFor(Minigame, recipeLevel);

        /// <summary>The Assembly an enhancement adds to a craft is the old window's progress penalty, added to what the piece needs.</summary>
        public EnhancementSupport Enhancements => EnhancementSupport.Modeled;

        public ICraftMinigameEngine CreateEngine(CraftEngineRequest request)
        {
            var level = Craft.GetRecipeLevelDetail(request.Recipe.Level);
            var setup = new CircuitSetup(
                request.Recipe.Detail?.MinigameParameters as CircuitParameters,
                request.Recipe.Level,
                level.Progress,
                level.Quality,
                level.Durability,
                request.Rank,
                request.Gear.Craftsmanship,
                request.Gear.Control,
                request.Gear.CpBonus,
                request.Enhancements.TotalProgressPenalty);

            return new CircuitEngine(request.Random, setup);
        }

        public IReadOnlyList<string> Limitations => new[]
        {
            $"An enhancement's difficulty is the old window's progress penalty added to the Assembly the piece needs, capped at {CircuitRules.MaxEnhancementAssemblyShare * 100:0}% of the Assembly the recipe itself needs; the Circuit's Wire, Tune and Discharge outputs do not change with it.",
            "The Circuit has no styles or perks yet, so the style-balance gate is not applicable and the perks-optional gate plays the unperked crafter.",
            "Seconds per action is an estimate of 8 s until the client evidence measures it."
        };
    }
}
