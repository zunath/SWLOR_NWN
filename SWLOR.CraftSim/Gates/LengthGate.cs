using static SWLOR.CraftSim.Report.MarkdownTable;

namespace SWLOR.CraftSim.Gates
{
    /// <summary>
    /// Gate 4, length: the adaptive player's mean actions per attempt, in every recipe band and gear tier at level, must fall inside the
    /// scenario's <see cref="Scenario.LengthTarget"/> (10-20 for an item craft). An attempt that breaks the piece early counts with the
    /// actions it took; an attempt that reaches the action limit is reported as stalled.
    /// </summary>
    public sealed class LengthGate : IBalanceGate
    {
        private readonly CraftGateSuite _suite;

        internal LengthGate(CraftGateSuite suite)
        {
            _suite = suite;
        }

        public string Name => GateNames.Length;

        public GateResult Evaluate(GateContext context)
        {
            var run = _suite.RunFor(context);
            if (run.Bands.Count == 0)
                return GateResult.NotRun(Name, "The scenario's cohort has no recipes in the catalog.");

            var target = run.Scenario.LengthTarget;
            run.PrepareAdaptive(run.CohortCells());

            var table = new EvidenceTable("Band", "Gear", "Recipes played", "Mean actions per attempt", "Shortest recipe", "Longest recipe", "Stalled attempts", "Target", "Verdict");
            var failures = new List<string>();
            foreach (var band in run.Bands)
            {
                for (var gearIndex = 0; gearIndex < run.Gear.Count; gearIndex++)
                {
                    var cells = band.Recipes.Select(recipe => run.Adaptive(recipe, run.RankFor(recipe), gearIndex)).ToList();
                    var mean = cells.Average(cell => cell.ActionsPerAttempt);
                    var passed = target.Contains(mean);
                    if (!passed)
                        failures.Add($"{band.Band.Name} / {run.Gear[gearIndex].Name} ({Number(mean)})");

                    table.Add(
                        band.Band.Name,
                        run.Gear[gearIndex].Name,
                        cells.Count.ToString(),
                        Number(mean),
                        Number(cells.Min(cell => cell.ActionsPerAttempt)),
                        Number(cells.Max(cell => cell.ActionsPerAttempt)),
                        cells.Sum(cell => cell.Stalled).ToString(),
                        $"{Number(target.MinimumActions, 0)}-{Number(target.MaximumActions, 0)}",
                        passed ? "Passed" : "Failed");
                }
            }

            var tables = new[] { table.ToGateTable($"Actions per attempt of the adaptive player at level ({target.Label})") };
            return failures.Count == 0
                ? new GateResult(Name, GateVerdict.Passed, $"Every band and gear tier takes {target} on average.", tables)
                : new GateResult(Name, GateVerdict.Failed, $"Outside {target}: {string.Join("; ", failures)}.", tables);
        }
    }
}
