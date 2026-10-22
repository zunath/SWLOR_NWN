using System.Text;
using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Scenario;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using static SWLOR.CraftSim.Report.MarkdownTable;

namespace SWLOR.CraftSim.Gates
{
    /// <summary>
    /// The seven crafting gates for one scenario, sharing one set of plays: the adaptive player's cells that gates 1, 4, 5 and 6 read
    /// are played once, and so are the legacy cells that gates 5 and 6 compare against. Make one suite per scenario and run its
    /// <see cref="Gates"/> with <see cref="GateReport.Run"/> against one <see cref="GateContext"/>; a different context starts the plays over.
    /// </summary>
    public sealed class CraftGateSuite
    {
        /// <summary>The names the command line takes for each gate, in the order of <see cref="GateNames.All"/>. A gate's number, 1 to 7, names it too.</summary>
        public static readonly IReadOnlyList<string> GateKeys = new[] { "rotation", "styles", "perks", "length", "materials", "xp", "enhancements" };

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
                new XpRateGate(XpCohort, Thresholds),
                new EnhancementParityGate(this)
            };
        }

        public ICraftScenario Scenario { get; }
        public GateThresholds Thresholds { get; }

        /// <summary>The cohort gate 6 compares: the plan's item-craft cohort unless a scenario brought its own.</summary>
        public IXpRateCohort XpCohort { get; }

        /// <summary>The seven gates, in the order of <see cref="GateNames.All"/>.</summary>
        public IReadOnlyList<IBalanceGate> Gates { get; }

        /// <summary>The gates the command line named, by key or by number (1 to 7); an unknown name is an error.</summary>
        public IReadOnlyList<IBalanceGate> Select(IEnumerable<string> keys)
        {
            var selected = new List<IBalanceGate>();
            foreach (var key in keys)
            {
                var index = GateKeys.ToList().FindIndex(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
                if (index < 0 && int.TryParse(key, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var number) &&
                    number >= 1 && number <= GateKeys.Count)
                {
                    index = number - 1;
                }

                if (index < 0)
                    throw new ArgumentException($"--gates names {string.Join(", ", GateKeys)} (or a gate number, 1 to {GateKeys.Count}), not '{key}'.");

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
            lines.Add($"- Fixed-rotation solver: {run.SolverOptions.Describe()}. Gate 1 searches one sequence for each band and gear tier across the band's sampled recipes (every recipe weighing the same; an action a recipe's engine cannot take is skipped there), " +
                      $"and also each recipe's own best sequence as a reported metric. The winner is chosen on separate attempts and then scored on a third set (hold-out) with {context.Runs} attempts per recipe.");
            lines.Add("- Legacy comparator (gates 5 and 6): the old minigame's lookahead player (`SWLOR.CraftSim baseline`), replayed here with the same seed on the same recipes, ranks and gear.");
            lines.Add($"- Time: {Number(Scenario.SecondsPerAction)} s per action in the new window and {Number(Thresholds.LegacySecondsPerAction)} s in the old one, plus {Number(Thresholds.OverheadSecondsPerAttempt)} s per craft in both. These are estimates; the client evidence replaces them.");
            var multipliers = run.Bands.Select(band => (band.Band.Name, Multiplier: Scenario.XpMultiplierFor(band.Band.First))).ToList();
            lines.Add(multipliers.All(m => m.Multiplier == 1.0)
                ? "- XP multiplier: none; the minigame pays today's base XP (`CraftMinigameXp`)."
                : "- XP multiplier on the base XP of every craft, failures included, as the game server applies it (`CraftMinigameXp`): " +
                  string.Join(", ", multipliers.Select(m => $"{m.Name} {Number(m.Multiplier, 2)}x")) +
                  ". The new side's XP below includes it; the legacy loop's does not.");
            lines.Add("- Thresholds (starting values, all in `GateThresholds`):");
            foreach (var (name, value, meaning) in Thresholds.Describe())
                lines.Add($"  - `{name}` = {value}: {meaning}");

            lines.Add($"- Length target: {Scenario.LengthTarget}.");
            lines.Add("- Enhancement thresholds in force (`CraftOutcomeRules`, read by gate 7): " +
                      string.Join(" / ", CraftOutcomeRules.GetEnhancementThresholds(3).Select(t => t + "%")) + " for slots 1, 2 and 3. " +
                      Scenario.Enhancements switch
                      {
                          EnhancementSupport.Modeled => "The scenario applies enhancements to its engine's progress meter.",
                          EnhancementSupport.NoSlots => "The minigame has no enhancement slots.",
                          _ => "The scenario does not apply enhancements yet, so gate 7 is not run."
                      });
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
                "- Gate 1 asks whether one rotation wins every craft of a band (owner decision, October 2026), not whether each recipe has a rotation of its own: in a minigame with full state and almost no randomness a rotation per recipe always exists, so that figure is reported and not gated. The band's recipes are an even sample of at most `RecipesPerBand`, so a rotation that wins the unsampled recipes is not tried.",
                "- The fixed-rotation solver is a beam search with local search, not a proof. A fixed rotation it did not find would narrow the margin; raise the solver options to look harder. The single rotation starts from every recipe's own best sequence and the adaptive lines, and is scored on separate attempts, so its figure is not flattered by the search.",
                "- Gate 5 compares at matched rank, gear and effort (both sides play to maximize quality) and requires the new minigame's average quality to be at least the legacy player's (within `MaterialQualityShortfallPoints`); it does not compare at an equal quality target. Where the legacy policy succeeds in fewer than `MaterialTwoSidedMinimumLegacySuccess` of the crafts, its failures waste material, and lower use than today passes (owner decision, October 2026). Enhancements are not committed in either model.",
                "- Gate 6 uses estimated seconds per action. The legacy comparator is the old minigame's lookahead player, one step of policy improvement over the rotation players report, not an optimal player. Its enhanced cases play the cohort's recipes that have an enhancement slot (an even sample of each band, so they can differ from the plain cases' recipes) with gate 7's representative enhancement in every slot; XP does not depend on the enhancements except through the quality and failures they cause.",
                "- Gate 7 plays every enhanceable recipe of the at-level bands with the representative real enhancement item of its kind (the best level the recipe accepts, the lower-median progress penalty of that level), the same item in every filled slot, and counts expected transferred enhancements per craft attempt, a craft that makes no item counting none. Enhancement items differ in penalty, so the figure holds for the median item. Today's side draws no transfer rolls: a craft that is made adds slots x quality chance / 100. How a scenario maps the penalty onto its progress meter is that minigame's own rule, and the client has not measured it.",
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
    /// <para>
    /// Each band and gear tier is played plain and again with one, two and three enhancements (<see cref="GateThresholds.EnhancementSlotCounts"/>),
    /// the same representative enhancement as gate 7 on the recipes that can hold one, each side applying its own enhancement difficulty:
    /// the old window's progress penalty, and the scenario's mapping of it onto its progress meter. Every band, gear tier and enhancement
    /// count must pass on its own. A scenario that does not model enhancements leaves the enhanced cases unplayed, which
    /// <see cref="Incomplete"/> reports so the gate is not run rather than passed.
    /// </para>
    /// </summary>
    public sealed class CraftXpRateCohort : IXpRateCohort
    {
        private readonly CraftGateSuite _suite;
        private readonly object _planGate = new();
        private (GateContext Context, EnhancedPlan Plan)? _plan;

        public CraftXpRateCohort(CraftGateSuite suite)
        {
            _suite = suite ?? throw new ArgumentNullException(nameof(suite));
        }

        public string Name => "Item crafts";

        public string RunName => "craft";

        public XpLoopTiming LegacyTiming => new(_suite.Thresholds.LegacySecondsPerAction, _suite.Thresholds.OverheadSecondsPerAttempt);

        public XpLoopTiming ReplacementTiming => new(_suite.Scenario.SecondsPerAction, _suite.Thresholds.OverheadSecondsPerAttempt);

        /// <summary>The enhanced bands the cohort can play with the counts to play, or why it cannot play them.</summary>
        private sealed record EnhancedPlan(IReadOnlyList<EnhancedBand> Bands, IReadOnlyList<int> Counts, string Problem);

        /// <summary>The label of a gear tier played with <paramref name="count"/> enhancements: just the gear name when plain.</summary>
        public static string CaseLabel(string gearName, int count) =>
            count == 0 ? gearName : $"{gearName}, {count} enhancement{(count == 1 ? string.Empty : "s")}";

        public IReadOnlyList<XpRateCase> Cases(GateContext context)
        {
            var run = _suite.RunFor(context);
            var plan = PlanFor(context);
            var cases = new List<XpRateCase>();
            foreach (var band in run.Bands)
            {
                var enhanced = plan.Bands.Any(b => b.Band.Band.Name == band.Band.Name);
                foreach (var gear in run.Gear)
                {
                    cases.Add(new XpRateCase(band.Band.Name, CaseLabel(gear.Name, 0)));
                    if (!enhanced)
                        continue;

                    cases.AddRange(plan.Counts.Select(count => new XpRateCase(band.Band.Name, CaseLabel(gear.Name, count))));
                }
            }

            return cases;
        }

        public string Incomplete(GateContext context) => PlanFor(context).Problem;

        public XpLoopSample PlayLegacy(XpRateCase xpCase, GateContext context)
        {
            var run = _suite.RunFor(context);
            var (band, gearIndex, count) = Locate(run, xpCase, context);
            if (count == 0)
            {
                run.PrepareLegacy(band.Recipes.Select(recipe => (recipe.Level, run.RankFor(recipe), gearIndex)));

                double plainXp = 0, plainActions = 0;
                var plainRuns = 0;
                foreach (var recipe in band.Recipes)
                {
                    var rank = run.RankFor(recipe);
                    var cell = run.Legacy(recipe.Level, rank, gearIndex);
                    plainXp += cell.XpPerAttempt(ServerXp.ForRecipe(recipe, rank)) * cell.Runs;
                    plainActions += cell.Actions;
                    plainRuns += cell.Runs;
                }

                return new XpLoopSample(plainXp, plainActions, plainRuns);
            }

            var enhanced = EnhancedRecipes(run, xpCase, context);
            run.PrepareEnhanced(enhanced.Select(entry => (entry.Recipe, run.RankFor(entry.Recipe), gearIndex, entry.Configuration(count))));

            double xp = 0, actions = 0;
            var runs = 0;
            foreach (var entry in enhanced)
            {
                var rank = run.RankFor(entry.Recipe);
                var cell = run.LegacyEnhanced(entry.Recipe.Level, rank, gearIndex, entry.Configuration(count));
                xp += cell.XpPerAttempt(ServerXp.ForRecipe(entry.Recipe, rank)) * cell.Runs;
                actions += cell.Actions;
                runs += cell.Runs;
            }

            return new XpLoopSample(xp, actions, runs);
        }

        public XpLoopSample PlayReplacement(XpRateCase xpCase, GateContext context)
        {
            var run = _suite.RunFor(context);
            var (band, gearIndex, count) = Locate(run, xpCase, context);
            double xp = 0, actions = 0;
            var runs = 0;
            if (count == 0)
            {
                run.PrepareAdaptive(band.Recipes.Select(recipe => (recipe, run.RankFor(recipe), gearIndex, (CraftStyle)null)));
                foreach (var recipe in band.Recipes)
                {
                    var cell = run.Adaptive(recipe, run.RankFor(recipe), gearIndex);
                    xp += cell.XpSum;
                    actions += cell.Actions;
                    runs += cell.Runs;
                }

                return new XpLoopSample(xp, actions, runs);
            }

            var enhanced = EnhancedRecipes(run, xpCase, context);
            run.PrepareEnhanced(enhanced.Select(entry => (entry.Recipe, run.RankFor(entry.Recipe), gearIndex, entry.Configuration(count))));
            foreach (var entry in enhanced)
            {
                var cell = run.AdaptiveEnhanced(entry.Recipe, run.RankFor(entry.Recipe), gearIndex, entry.Configuration(count));
                xp += cell.XpSum;
                actions += cell.Actions;
                runs += cell.Runs;
            }

            return new XpLoopSample(xp, actions, runs);
        }

        private EnhancedPlan PlanFor(GateContext context)
        {
            lock (_planGate)
            {
                if (_plan is { } cached && ReferenceEquals(cached.Context, context))
                    return cached.Plan;

                var plan = BuildPlan(context);
                _plan = (context, plan);
                return plan;
            }
        }

        private EnhancedPlan BuildPlan(GateContext context)
        {
            var run = _suite.RunFor(context);
            var counts = _suite.Thresholds.EnhancementSlotCounts.Where(count => count >= 1).Distinct().OrderBy(count => count).ToList();
            var none = new EnhancedPlan(Array.Empty<EnhancedBand>(), counts, null);
            if (counts.Count == 0 || _suite.Scenario.Enhancements == EnhancementSupport.NoSlots || !run.HasEnhanceableRecipes)
                return none;

            const string Skipped = "the enhanced crafts (one, two and three enhancements) in every band and gear tier, ";
            if (_suite.Scenario.Enhancements == EnhancementSupport.NotModeled)
            {
                return none with
                {
                    Problem = Skipped + "because the scenario does not apply CraftEngineRequest.Enhancements to its engine " +
                              "(set ICraftScenario.Enhancements to Modeled once it does; see SWLOR.CraftSim/README.md)."
                };
            }

            if (context.Enhancements == null)
                return none with { Problem = Skipped + "because the gate context has no enhancement items (EnhancementCatalog.Load)." };

            var bands = run.EnhancedBands().Where(band => band.Recipes.Count > 0).ToList();
            if (bands.Count == 0)
                return none with { Problem = Skipped + "because no sampled recipe that can hold an enhancement has an enhancement item to use." };

            var unmodeled = run.UnmodeledRecipes(bands.SelectMany(band => band.Recipes), run.RankFor, run.Gear.Count - 1, counts[^1]);
            if (unmodeled.Count > 0)
            {
                return none with
                {
                    Problem = Skipped + $"because the scenario says it models enhancements but its engine's progress maximum does not rise with them for {unmodeled.Count} recipe(s), such as {unmodeled[0]}."
                };
            }

            return new EnhancedPlan(bands, counts, null);
        }

        private IReadOnlyList<EnhancedRecipe> EnhancedRecipes(ScenarioRun run, XpRateCase xpCase, GateContext context) =>
            PlanFor(context).Bands.FirstOrDefault(band => band.Band.Band.Name == xpCase.Group)?.Recipes
            ?? throw new ArgumentException($"The cohort has no enhanced band '{xpCase.Group}'.");

        private (CohortBand Band, int GearIndex, int Count) Locate(ScenarioRun run, XpRateCase xpCase, GateContext context)
        {
            var band = run.Bands.FirstOrDefault(b => b.Band.Name == xpCase.Group)
                       ?? throw new ArgumentException($"The cohort has no band '{xpCase.Group}'.");
            var counts = new[] { 0 }.Concat(PlanFor(context).Counts).ToList();
            for (var gearIndex = 0; gearIndex < run.Gear.Count; gearIndex++)
            {
                foreach (var count in counts)
                {
                    if (CaseLabel(run.Gear[gearIndex].Name, count) == xpCase.Label)
                        return (band, gearIndex, count);
                }
            }

            throw new ArgumentException($"The cohort has no case '{xpCase.Label}'.");
        }
    }
}
