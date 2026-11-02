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
    /// A crafter plays without perks unless a gate names a style or the scenario was made with a <see cref="ForgeLoadout"/>.
    /// </summary>
    public class ForgeScenario : ICraftScenario
    {
        private static readonly AdaptivePolicyOptions Adaptive = new()
        {
            // The Forge is deterministic apart from one telegraphed opportunity, and Integrity is tight: a skilled player plans Thermal and
            // Integrity several actions ahead (ramp before the range is lost, rebond before the next press can crack the piece). A depth of
            // four sees that; the heuristic keeps the Integrity a Shape finish needs (a margin of 2.5) rather than honing to the last point.
            Depth = 4,
            SampleBudget = 6000,
            Heuristic = new DefaultLeafHeuristic(successBonus: 0.75, safetyMargin: 2.5, progressPull: 0.02)
        };

        private readonly ForgeLoadout _defaultLoadout;
        private readonly string _name;
        private readonly string _title;

        public ForgeScenario()
            : this("forge", "Plasma Forge", null)
        {
        }

        /// <summary>A scenario whose crafters all play <paramref name="defaultLoadout"/> unless a gate names a style, for the gates that run a perked crafter.</summary>
        protected ForgeScenario(string name, string title, ForgeLoadout defaultLoadout)
        {
            _name = name;
            _title = title;
            _defaultLoadout = defaultLoadout;
        }

        public string Name => _name;

        public string Title => _title;

        public SkillType Skill => SkillType.Smithery;

        public CraftMinigameType Minigame => CraftMinigameType.Forge;

        /// <summary>The pace the server holds a craft's XP to (<see cref="CraftMinigameXp.ActionSeconds"/>), an estimate until the client evidence measures it; the old window's is 6.</summary>
        public double SecondsPerAction => CraftMinigameXp.ActionSeconds;

        /// <summary>The Forge's multiplier at the lowest level band, the largest; <see cref="XpMultiplierFor"/> gives each band's.</summary>
        public double XpMultiplier => CraftMinigameXp.MultiplierFor(Minigame);

        public double XpMultiplierFor(int recipeLevel) => CraftMinigameXp.MultiplierFor(Minigame, recipeLevel);

        public EnhancementSupport Enhancements => EnhancementSupport.Modeled;

        /// <summary>A crafter who bought any Smithery perk (a line or a style) keeps the perked floor of 8 actions; the unperked Forge keeps 10.</summary>
        public LengthTarget LengthTarget => _defaultLoadout != null ? LengthTarget.PerkedItemCraft : LengthTarget.ItemCraft;

        public AdaptivePolicyOptions AdaptiveOptions => Adaptive;

        /// <summary>
        /// Gate 2 compares Hot Forging with Cold Working for a crafter who has bought only the styles (rank 20 is the first rank either is
        /// open) and for one who has bought both lines in full and toggles between the styles. Each is played by the adaptive player on the
        /// same dice.
        /// </summary>
        public IReadOnlyList<CraftStylePair> StylePairs => _defaultLoadout != null
            ? Array.Empty<CraftStylePair>()
            : new[]
            {
                new CraftStylePair(
                    new ForgeLoadout(ForgeStyle.HotForging, false, false).ToStyle("Hot Forging (no lines)"),
                    new ForgeLoadout(ForgeStyle.ColdWorking, false, false).ToStyle("Cold Working (no lines)"),
                    ForgePerkRules.StyleRank),
                new CraftStylePair(
                    new ForgeLoadout(ForgeStyle.HotForging, true, true).ToStyle("Hot Forging (both lines)"),
                    new ForgeLoadout(ForgeStyle.ColdWorking, true, true).ToStyle("Cold Working (both lines)"),
                    ForgePerkRules.StyleRank)
            };

        public IReadOnlyList<string> Limitations => new[]
        {
            "Enhancements raise the Shape the recipe needs by their progress penalty through ForgeRules.EnhancementShapePercent; their other effects are not played.",
            "Without a style the crafter plays without perks, which is what the perks-optional gate and every gate of the plain scenario play; the style-balance gate plays Hot Forging and Cold Working with no line perks and with both lines bought in full (every purchase the crafter's rank allows).",
            "The seconds per action (8) is an estimate for a window nobody has played; client timing replaces it.",
            "The adaptive player reads only the shared meters and previews, so it plans Thermal worse than a person who reads the range; gate 1 compares it with a solver that searches whole rotations."
        }.Concat(_defaultLoadout == null
            ? Array.Empty<string>()
            : new[] { $"Every craft in this scenario is played by a crafter who bought {_defaultLoadout.Describe()}, so no gate here is an unperked result." }).ToList();

        public ICraftMinigameEngine CreateEngine(CraftEngineRequest request)
        {
            var parameters = request.Recipe.Detail?.MinigameParameters as ForgeParameters
                             ?? throw new InvalidOperationException($"{request.Recipe.Type} has no Plasma Forge parameters.");

            var loadout = request.Style?.Configuration as ForgeLoadout ?? _defaultLoadout;
            var setup = new ForgeSetup(
                parameters,
                request.Recipe.Level,
                request.Rank,
                request.Gear.Craftsmanship,
                request.Gear.Control,
                request.Gear.CpBonus,
                ForgeRules.EnhancementShapePercent(request.Enhancements.TotalProgressPenalty, request.Recipe.Level),
                loadout?.StatsAt(request.Rank));

            return new ForgeEngine(setup, request.Random);
        }
    }
}
