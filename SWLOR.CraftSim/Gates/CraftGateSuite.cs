using System.Text;
using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Scenario;
using static SWLOR.CraftSim.Report.MarkdownTable;

namespace SWLOR.CraftSim.Gates
{
    /// <summary>
    /// The six crafting gates for one scenario, sharing one set of plays: the adaptive player's cells that gates 1, 4, 5 and 6 read
    /// are played once, and so are the legacy cells that gates 5 and 6 compare against. Make one suite per scenario and run its
    /// <see cref="Gates"/> with <see cref="GateReport.Run"/> against one <see cref="GateContext"/>; a different context starts the plays over.
    /// </summary>
    public sealed class CraftGateSuite
    {
        /// <summary>The names the command line takes for each gate, in the order of <see cref="GateNames.All"/>.</summary>
        public static readonly IReadOnlyList<string> GateKeys = new[] { "rotation", "styles", "perks", "length", "materials", "xp" };

        private readonly object _gate = new();
        private ScenarioRun _run;

        public CraftGateSuite(ICraftScenario scenario, GateThresholds thresholds = null, IXpRateCohort xpCohort = null)
        {
            Scenario = scenario ?? throw new ArgumentNullException(nameof(scenario));
            Thresholds = thresholds ?? GateThresholds.Default;
            XpCohort = xpCohort ?? new CraftXpRateCohort(this);
            Gates = new IBalanceGate[]
            {
                new NoFixedRotationGate(this),
                new StyleBalanceGate(this),
                new PerksOptionalGate(this),
                new LengthGate(this),
                new MaterialParityGate(this),
                new XpRateGate(XpCohort, Thresholds)
            };
        }

        public ICraftScenario Scenario { get; }
        public GateThresholds Thresholds { get; }

        /// <summary>The cohort gate 6 compares: the plan's item-craft cohort unless a scenario brought its own.</summary>
        public IXpRateCohort XpCohort { get; }

        /// <summary>The six gates, in the order of <see cref="GateNames.All"/>.</summary>
        public IReadOnlyList<IBalanceGate> Gates { get; }

        /// <summary>The gates the command line named; an unknown name is an error.</summary>
        public IReadOnlyList<IBalanceGate> Select(IEnumerable<string> keys)
        {
            var selected = new List<IBalanceGate>();
            foreach (var key in keys)
            {
                var index = GateKeys.ToList().FindIndex(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
                if (index < 0)
                    throw new ArgumentException($"--gates names {string.Join(", ", GateKeys)}, not '{key}'.");

                if (!selected.Contains(Gates[index]))
                    selected.Add(Gates[index]);
            }

            if (selected.Count == 0)
                throw new ArgumentException($"--gates needs at least one of {string.Join(", ", GateKeys)}.");

            return selected.OrderBy(gate => Gates.ToList().IndexOf(gate)).ToList();
        }

        internal ScenarioRun RunFor(GateContext context)
        {
            lock (_gate)
            {
                if (_run == null || !ReferenceEquals(_run.Context, context))
                    _run = new ScenarioRun(Scenario, Thresholds, context);

                return _run;
            }
        }

        /// <summary>The report's "Inputs" lines: what was played, against what, and with which thresholds and estimates.</summary>
        public IReadOnlyList<string> Inputs(GateContext context)
        {
            var run = RunFor(context);
            var lines = new List<string>
            {
                $"- Scenario: {Scenario.Title} (`{Scenario.Name}`), {Scenario.Skill} recipes, minigame {Scenario.Minigame}.",
                "- Quality: the quality meter's share of its maximum when the item is made, in points (0-100). A broken or abandoned piece counts 0, so every average includes the crafts that failed.",
                "- Crafter: the rank the scenario's cohort gives each recipe (by default the recipe's level, at most 50, so a crafter works at their own level), with no food or status bonuses and no perks unless a gate names a style. " +
                "Gear levels are the plan's none, middle and best tier of the skill's crafting gear:"
            };

            foreach (var gear in run.Gear)
                lines.Add($"  - {gear}");

            lines.Add($"- Recipes: {RecipeLines(run)}");
            lines.Add($"- Adaptive player: {Scenario.AdaptiveOptions.Describe()}. Each attempt's dice come from its own stream of the seed.");
            lines.Add($"- Fixed-rotation solver: {run.SolverOptions.Describe()}. The winner is chosen on separate attempts and then scored on a third set (hold-out) with {context.Runs} attempts.");
            lines.Add("- Legacy comparator (gates 5 and 6): the old minigame's lookahead player (`SWLOR.CraftSim baseline`), replayed here with the same seed on the same recipes, ranks and gear.");
            lines.Add($"- Time: {Number(Scenario.SecondsPerAction)} s per action in the new window and {Number(Thresholds.LegacySecondsPerAction)} s in the old one, plus {Number(Thresholds.OverheadSecondsPerAttempt)} s per craft in both. These are estimates; the client evidence replaces them.");
            if (Math.Abs(Scenario.XpMultiplier - 1.0) > 1e-9)
                lines.Add($"- XP multiplier: the scenario scales every craft's base XP by {Number(Scenario.XpMultiplier, 2)}, as the game does (the legacy side is not scaled).");

            lines.Add("- Thresholds (starting values, all in `GateThresholds`):");
            foreach (var (name, value, meaning) in Thresholds.Describe())
                lines.Add($"  - `{name}` = {value}: {meaning}");

            lines.Add($"- Length target: {Scenario.LengthTarget}.");
            if (Scenario.StylePairs.Count == 0)
            {
                lines.Add("- Styles: none offered, so the style-balance gate is not applicable.");
            }
            else
            {
                lines.Add("- Styles compared: " + string.Join("; ", Scenario.StylePairs.Select(pair => $"{pair.Name} (from rank {pair.MinimumRank})")) + ".");
            }

            return lines;
        }

        /// <summary>The report's "Limitations" lines, ending with the scenario's own.</summary>
        public IReadOnlyList<string> Limitations()
        {
            var lines = new List<string>
            {
                "- The recipes are an even sample of each band (or all of them, as the inputs say), weighted equally. A recipe the sample skipped can behave differently.",
                "- The adaptive player is a bounded-depth expectimax over the previews with a generic heuristic. It is a floor on skilled play, not an optimal player; a deeper search or a scenario heuristic can only raise it.",
                "- The fixed-rotation solver is a beam search with local search, not a proof. A fixed rotation it did not find would narrow the margin; raise the solver options to look harder. The rotation is scored on separate attempts, so its figure is not flattered by the search.",
                "- Gate 5 compares at matched rank, gear and effort (both sides play to maximize quality) and requires the new minigame's average quality to be at least the legacy player's (within `MaterialQualityShortfallPoints`); it does not compare at an equal quality target. Enhancements are not committed in either model.",
                "- Gate 6 uses estimated seconds per action. The legacy comparator is the old minigame's lookahead player, one step of policy improvement over the rotation players report, not an optimal player.",
                "- Gathering, scavenging, fishing and research are not covered here; their cohorts implement `IXpRateCohort` and their economy checks belong to their own gates.",
                "- No attributes, food, status effects, blueprints, first-craft bonus or quick craft are modeled, and the client was not run."
            };

            lines.AddRange(Scenario.Limitations.Select(limitation => "- " + limitation));
            return lines;
        }

        private static string RecipeLines(ScenarioRun run)
        {
            var text = new StringBuilder();
            text.Append(string.Join("; ", run.Bands.Select(band =>
                $"{band.Band.Name}: {(band.Recipes.Count == band.ActiveRecipes ? band.ActiveRecipes.ToString() : $"{band.Recipes.Count} of {band.ActiveRecipes}")}")));
            text.Append(" recipes played.");
            return text.ToString();
        }
    }

    /// <summary>
    /// The plan's item-craft cohort for the XP-rate gate: each recipe band with rank equal to recipe level (levels 51-53 at rank 50), each
    /// gear tier, the band's recipes weighted equally, no blueprint and not a first craft. Both sides play as each policy actually plays,
    /// failures included at their 15% XP: the legacy side is the old minigame's lookahead player, the new side CraftSim's adaptive player.
    /// </summary>
    public sealed class CraftXpRateCohort : IXpRateCohort
    {
        private readonly CraftGateSuite _suite;

        public CraftXpRateCohort(CraftGateSuite suite)
        {
            _suite = suite ?? throw new ArgumentNullException(nameof(suite));
        }

        public string Name => "Item crafts";

        public string RunName => "craft";

        public XpLoopTiming LegacyTiming => new(_suite.Thresholds.LegacySecondsPerAction, _suite.Thresholds.OverheadSecondsPerAttempt);

        public XpLoopTiming ReplacementTiming => new(_suite.Scenario.SecondsPerAction, _suite.Thresholds.OverheadSecondsPerAttempt);

        public IReadOnlyList<XpRateCase> Cases(GateContext context)
        {
            var run = _suite.RunFor(context);
            return run.Bands
                .SelectMany(band => run.Gear.Select(gear => new XpRateCase(band.Band.Name, gear.Name)))
                .ToList();
        }

        public XpLoopSample PlayLegacy(XpRateCase xpCase, GateContext context)
        {
            var run = _suite.RunFor(context);
            var (band, gearIndex) = Locate(run, xpCase);
            run.PrepareLegacy(band.Recipes.Select(recipe => (recipe.Level, run.RankFor(recipe), gearIndex)));

            double xp = 0, actions = 0;
            var runs = 0;
            foreach (var recipe in band.Recipes)
            {
                var rank = run.RankFor(recipe);
                var cell = run.Legacy(recipe.Level, rank, gearIndex);
                xp += cell.XpPerAttempt(ServerXp.ForRecipe(recipe, rank)) * cell.Runs;
                actions += cell.Actions;
                runs += cell.Runs;
            }

            return new XpLoopSample(xp, actions, runs);
        }

        public XpLoopSample PlayReplacement(XpRateCase xpCase, GateContext context)
        {
            var run = _suite.RunFor(context);
            var (band, gearIndex) = Locate(run, xpCase);
            run.PrepareAdaptive(band.Recipes.Select(recipe => (recipe, run.RankFor(recipe), gearIndex, (CraftStyle)null)));

            double xp = 0, actions = 0;
            var runs = 0;
            foreach (var recipe in band.Recipes)
            {
                var cell = run.Adaptive(recipe, run.RankFor(recipe), gearIndex);
                xp += cell.XpSum;
                actions += cell.Actions;
                runs += cell.Runs;
            }

            return new XpLoopSample(xp, actions, runs);
        }

        private static (CohortBand Band, int GearIndex) Locate(ScenarioRun run, XpRateCase xpCase)
        {
            var band = run.Bands.FirstOrDefault(b => b.Band.Name == xpCase.Group)
                       ?? throw new ArgumentException($"The cohort has no band '{xpCase.Group}'.");
            var gearIndex = run.Gear.ToList().FindIndex(gear => gear.Name == xpCase.Label);
            if (gearIndex < 0)
                throw new ArgumentException($"The cohort has no gear level '{xpCase.Label}'.");

            return (band, gearIndex);
        }
    }
}
