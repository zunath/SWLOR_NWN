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
    public class CircuitScenario : ICraftScenario
    {
        private readonly CircuitLoadout _defaultLoadout;
        private readonly string _name;
        private readonly string _title;

        public CircuitScenario()
            : this("circuit", "Circuit", null)
        {
        }

        /// <summary>A scenario whose crafters all play <paramref name="defaultLoadout"/> unless a gate names a style, for the gates that run a perked crafter.</summary>
        protected CircuitScenario(string name, string title, CircuitLoadout defaultLoadout)
        {
            _name = name;
            _title = title;
            _defaultLoadout = defaultLoadout;
        }

        public string Name => _name;

        public string Title => _title;

        public SkillType Skill => SkillType.Engineering;

        public CraftMinigameType Minigame => CraftMinigameType.Circuit;

        /// <summary>An estimate: the window shows each action's result and risk, so a turn takes a little longer than the old window's 6 s. Client timing replaces it.</summary>
        public double SecondsPerAction => 8.0;

        /// <summary>The Circuit's multiplier depends on the recipe's level band: see <see cref="XpMultiplierFor"/>.</summary>
        public double XpMultiplier => 1.0;

        public double XpMultiplierFor(int recipeLevel) => CraftMinigameXp.MultiplierFor(Minigame, recipeLevel);

        /// <summary>The Assembly an enhancement adds to a craft is the old window's progress penalty, added to what the piece needs.</summary>
        public EnhancementSupport Enhancements => EnhancementSupport.Modeled;

        /// <summary>
        /// Gate 2 compares the two styles for a crafter who has bought only the styles (rank 20 is the first rank either is open) and for
        /// one who has bought both lines in full. Each is played by the adaptive player on the same dice.
        /// </summary>
        public IReadOnlyList<CraftStylePair> StylePairs => _defaultLoadout != null
            ? Array.Empty<CraftStylePair>()
            : new[]
            {
                new CraftStylePair(
                    new CircuitLoadout(CircuitStyle.Overclock, false, false).ToStyle("Overclock (no lines)"),
                    new CircuitLoadout(CircuitStyle.SafeMode, false, false).ToStyle("Safe Mode (no lines)"),
                    CircuitRules.StyleRank),
                new CraftStylePair(
                    new CircuitLoadout(CircuitStyle.Overclock, true, true).ToStyle("Overclock (both lines)"),
                    new CircuitLoadout(CircuitStyle.SafeMode, true, true).ToStyle("Safe Mode (both lines)"),
                    CircuitRules.StyleRank)
            };

        public ICraftMinigameEngine CreateEngine(CraftEngineRequest request)
        {
            var loadout = request.Style?.Configuration as CircuitLoadout ?? _defaultLoadout;
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
                request.Enhancements.TotalProgressPenalty,
                loadout?.StatsAt(request.Rank));

            return new CircuitEngine(request.Random, setup);
        }

        public IReadOnlyList<string> Limitations => new[]
        {
            $"An enhancement's difficulty is the old window's progress penalty added to the Assembly the piece needs, capped at {CircuitRules.MaxEnhancementAssemblyShare * 100:0}% of the Assembly the recipe itself needs; the Circuit's Wire, Tune and Discharge outputs do not change with it.",
            "Without a style the crafter plays without perks, which is what the perks-optional gate and every gate of the plain scenario play; the style-balance gate plays Overclock and Safe Mode with no line perks and with both lines bought in full (every purchase the crafter's rank allows).",
            "Seconds per action is an estimate of 8 s until the client evidence measures it."
        }.Concat(_defaultLoadout == null
            ? Array.Empty<string>()
            : new[] { $"Every craft in this scenario is played by a crafter who bought {_defaultLoadout.Describe()}, so no gate here is an unperked result." }).ToList();
    }
}
