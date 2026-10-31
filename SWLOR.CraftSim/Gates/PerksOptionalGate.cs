using SWLOR.CraftSim.Scenario;
using static SWLOR.CraftSim.Report.MarkdownTable;

namespace SWLOR.CraftSim.Gates
{
    /// <summary>
    /// Gate 3, perks optional: a rank-50 crafter with the best gear and no perks must be able to reach the maximum quality on at-level
    /// recipes by skilled play. For each at-level band it reports the share of adaptive attempts that make the item with the quality
    /// meter full, and passes when each band reaches <see cref="GateThresholds.PerksOptionalMinimumFullQualityShare"/>. Perks may change
    /// how, and how efficiently, but not whether.
    /// </summary>
    public sealed class PerksOptionalGate : IBalanceGate
    {
        private readonly CraftGateSuite _suite;

        internal PerksOptionalGate(CraftGateSuite suite)
        {
            _suite = suite;
        }

        public string Name => GateNames.PerksOptional;

        public GateResult Evaluate(GateContext context)
        {
            var run = _suite.RunFor(context);
            var thresholds = run.Thresholds;
            var bands = run.Cohort.Select(context.Recipes, thresholds.RecipesPerBand, thresholds.PerksOptionalBands);
            if (bands.Count == 0)
                return GateResult.NotRun(Name, "The scenario's cohort has no recipes at the at-level bands.");

            var gearIndex = run.Gear.Count - 1;
            var rank = thresholds.PerksOptionalRank;
            run.PrepareAdaptive(bands.SelectMany(band => band.Recipes).Select(recipe => (recipe, rank, gearIndex, (CraftStyle)null)));

            var table = new EvidenceTable("Band", "Recipes played", "Attempts", "Reach maximum quality", "Make the item", "Avg quality of items made", "Required", "Verdict");
            var failures = new List<string>();
            var lowest = double.MaxValue;
            foreach (var band in bands)
            {
                var cells = band.Recipes.Select(recipe => run.Adaptive(recipe, rank, gearIndex)).ToList();
                var attempts = cells.Sum(cell => cell.Runs);
                var share = cells.Sum(cell => cell.FullQuality) / (double)attempts;
                var successes = cells.Sum(cell => cell.Successes);
                var passed = share >= thresholds.PerksOptionalMinimumFullQualityShare;
                if (!passed)
                    failures.Add(band.Band.Name);

                lowest = Math.Min(lowest, share);
                table.Add(
                    band.Band.Name,
                    cells.Count.ToString(),
                    attempts.ToString(),
                    Percent(share),
                    Percent(successes / (double)attempts),
                    successes == 0 ? "n/a" : Percent(cells.Sum(cell => cell.QualitySum) / successes),
                    ">= " + Percent(thresholds.PerksOptionalMinimumFullQualityShare),
                    passed ? "Passed" : "Failed");
            }

            var tables = new[]
            {
                table.ToGateTable($"Rank {rank}, {run.Gear[gearIndex].Name}, no perks: adaptive attempts that reach maximum quality, by recipe band")
            };

            return failures.Count == 0
                ? new GateResult(Name, GateVerdict.Passed,
                    $"An unperked rank-{rank} crafter in {run.Gear[gearIndex].Name} reaches maximum quality on at least {Percent(thresholds.PerksOptionalMinimumFullQualityShare)} of crafts in every at-level band (lowest {Percent(lowest)}).",
                    tables)
                : new GateResult(Name, GateVerdict.Failed,
                    $"Maximum quality is out of reach without perks in band(s) {string.Join(", ", failures)} (lowest {Percent(lowest)}, needs {Percent(thresholds.PerksOptionalMinimumFullQualityShare)}).",
                    tables);
        }
    }
}
