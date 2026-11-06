using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Scenario;
using static SWLOR.CraftSim.Report.MarkdownTable;

namespace SWLOR.CraftSim.Gates
{
    /// <summary>
    /// Whether gear is worth having, for a minigame tuned against the gear players really reach (<see cref="ICraftScenario.TunedAgainstFullMods"/>).
    /// At rank 50, with the kits the top recipes hold and no perks: every gear step raises average quality; on the top recipes a fully
    /// modded set usually reaches 85% but is not guaranteed 100%, and takes a real craft's length; and a fully perked crafter with no gear
    /// does not replace unmodded best gear. Each kind of recipe the scenario names must pass on its own, so one kind cannot hide the other.
    /// </summary>
    public sealed class GearLadderGate : IBalanceGate
    {
        public const string Key = "ladder";

        private readonly CraftGateSuite _suite;

        public GearLadderGate(CraftGateSuite suite)
        {
            _suite = suite ?? throw new ArgumentNullException(nameof(suite));
        }

        public string Name => GateNames.GearLadder;

        public GateResult Evaluate(GateContext context)
        {
            var run = _suite.RunFor(context);
            var thresholds = run.Thresholds;
            var rank = thresholds.PerksOptionalRank;
            var bands = run.EnhancedBands(thresholds.GearLadderBands).Where(band => band.Recipes.Count > 0).ToList();
            if (bands.Count == 0)
                return GateResult.NotRun(Name, "The scenario's cohort has no enhanceable recipe in the gear ladder's bands.");

            var count = thresholds.GearLadderEnhancements;
            run.PrepareEnhanced(bands.SelectMany(band => band.Recipes).SelectMany(entry =>
                Enumerable.Range(0, run.Gear.Count).Select(gear => (entry.Recipe, rank, gear, entry.Configuration(count)))));

            var failures = new List<string>();
            var table = new EvidenceTable("Band", "Recipes", "Kind", "Gear", "Avg quality", "Reach 85%", "Reach 100%", "Actions", "Verdict");
            var top = bands[^1].Band.Band.Name;
            foreach (var band in bands)
            {
                foreach (var kind in band.Recipes.GroupBy(entry => run.Scenario.GearLadderKind(entry.Recipe)).OrderBy(group => group.Key))
                {
                    double? below = null;
                    for (var gearIndex = 0; gearIndex < run.Gear.Count; gearIndex++)
                    {
                        var gear = run.Gear[gearIndex];
                        var index = gearIndex;
                        var cells = kind.Select(entry => run.AdaptiveEnhanced(entry.Recipe, rank, index, entry.Configuration(count))).ToList();
                        var attempts = (double)cells.Sum(cell => cell.Runs);
                        var quality = 100 * cells.Sum(cell => cell.QualitySum) / attempts;
                        var reach85 = cells.Sum(cell => cell.QualityPercentCounts.Skip(85).Sum()) / attempts;
                        var reach100 = cells.Sum(cell => cell.FullQuality) / attempts;
                        var actions = cells.Sum(cell => cell.Actions) / attempts;

                        var problems = new List<string>();
                        if (below is { } lower && lower < thresholds.GearLadderSaturatedQuality && quality < lower + thresholds.GearLadderMinimumStep)
                            problems.Add($"adds {Number(quality - lower)} points over the gear below it");

                        if (band.Band.Band.Name == top && gear.Name == CraftingGearCatalog.FullyModdedGearName)
                        {
                            if (reach85 < thresholds.GearLadderFullModsReach85Share)
                                problems.Add($"reaches 85% on only {Percent(reach85)}");
                            if (reach100 > thresholds.GearLadderFullModsMaxFullQualityShare)
                                problems.Add($"reaches 100% on {Percent(reach100)}");
                            if (!run.Scenario.LengthTarget.Contains(actions))
                                problems.Add($"takes {Number(actions)} actions");
                        }

                        failures.AddRange(problems.Select(problem => $"{band.Band.Band.Name} / {kind.Key} / {gear.Name} {problem}"));
                        table.Add(
                            band.Band.Band.Name, cells.Count.ToString(), kind.Key, gear.Name, Number(quality), Percent(reach85), Percent(reach100), Number(actions),
                            problems.Count == 0 ? "Passed" : "Failed");
                        below = quality;
                    }
                }
            }

            var tables = new[]
            {
                table.ToGateTable($"Rank {rank}, {count} kits, no perks: what each step of gear buys"),
                PerkTable(run, context, rank, failures)
            };

            return failures.Count == 0
                ? new GateResult(Name, GateVerdict.Passed, "Every gear step raises quality, fully modded gear makes the top recipes' target, and perks do not replace gear.", tables)
                : new GateResult(Name, GateVerdict.Failed, string.Join("; ", failures) + ".", tables);
        }

        /// <summary>A fully perked crafter with no gear against an unperked crafter in unmodded best gear, on plain crafts, for each both-lines style.</summary>
        private static GateTable PerkTable(ScenarioRun run, GateContext context, int rank, List<string> failures)
        {
            var thresholds = run.Thresholds;
            var table = new EvidenceTable("Band", "Style", "Fully perked, no gear", "No perks, unmodded best gear", "Allowance", "Verdict");
            var bestIndex = run.Gear.ToList().FindIndex(gear => gear.Name == CraftingGearCatalog.BestGearName);
            var styles = run.Scenario.StylePairs.LastOrDefault() is { } pair ? new[] { pair.First, pair.Second } : Array.Empty<CraftStyle>();
            var bands = run.Cohort.Select(context.Recipes, thresholds.RecipesPerBand, thresholds.GearLadderBands);
            run.PrepareAdaptive(bands.SelectMany(band => band.Recipes).SelectMany(recipe =>
                styles.Select(style => (recipe, rank, 0, style)).Append((recipe, rank, bestIndex, (CraftStyle)null))));

            foreach (var band in bands)
            {
                var unperked = 100 * band.Recipes.Average(recipe => run.Adaptive(recipe, rank, bestIndex).MeanScore);
                foreach (var style in styles)
                {
                    var perked = 100 * band.Recipes.Average(recipe => run.Adaptive(recipe, rank, 0, style).MeanScore);
                    var passed = perked <= unperked + thresholds.GearLadderPerkAllowance;
                    if (!passed)
                        failures.Add($"{band.Band.Name} / {style.Name} with no gear averages {Number(perked)} against unmodded best gear's {Number(unperked)}");

                    table.Add(band.Band.Name, style.Name, Number(perked), Number(unperked), "+" + Number(thresholds.GearLadderPerkAllowance, 0), passed ? "Passed" : "Failed");
                }
            }

            return table.ToGateTable("Perks do not replace gear: plain crafts, average quality");
        }
    }
}
