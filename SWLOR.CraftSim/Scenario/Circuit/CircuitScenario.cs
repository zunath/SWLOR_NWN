using SWLOR.CraftSim.Play;
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
                request.Gear.CpBonus);

            return new CircuitEngine(request.Random, setup);
        }

        // EXPERIMENT-ONLY
        public AdaptivePolicyOptions AdaptiveOptions => new()
        {
            Depth = Env("ADEPTH", 3), ForecastSamples = Env("AFS", 2), SampleBudget = Env("ABUDGET", 4000), TimePreference = Env("ATIME", 5) / 1000.0,
            Heuristic = Env("AHEUR", 1) == 1 ? new CircuitLeafHeuristic(Env("APOT", 0) / 100.0, 0.5, Env("AMARGIN", 130) / 100.0) : DefaultLeafHeuristic.Instance
        };

        private static int Env(string name, int fallback) => int.TryParse(Environment.GetEnvironmentVariable("CIRCUIT_" + name), out var v) ? v : fallback;

        public IReadOnlyList<string> Limitations => new[]
        {
            "No enhancements are slotted, so no enhancement adds Assembly to the craft.",
            "The Circuit has no styles or perks yet, so the style-balance gate is not applicable and the perks-optional gate plays the unperked crafter.",
            "Seconds per action is an estimate of 8 s until the client evidence measures it."
        };
    }
}
