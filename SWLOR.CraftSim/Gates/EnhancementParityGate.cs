using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Legacy.Crafting;
using SWLOR.CraftSim.Rng;
using SWLOR.CraftSim.Scenario;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using static SWLOR.CraftSim.Report.MarkdownTable;

namespace SWLOR.CraftSim.Gates
{
    /// <summary>
    /// Gate 7, enhancement parity: at rank 50 with the best gear and at level, an item with one, two or three enhancements must transfer
    /// at least as many of them, on average, as the old window did. Today each enhancement transfers at a chance equal to the quality
    /// percentage; the new rules transfer the enhancement of a slot whenever the quality meets that slot's threshold
    /// (<see cref="CraftOutcomeRules"/>), so a crafter at 80% quality gets 2.4 of 3 mods today and exactly 1 under thresholds of 50/85/85.
    /// <para>
    /// Both sides carry the difficulty enhancements add: the old window raises the progress a piece needs by each enhancement's penalty,
    /// and the scenario maps the same penalty onto its engine's progress meter (<see cref="CraftEngineRequest.Enhancements"/>). Each recipe
    /// is played with the representative real enhancement item of its kind at the best level it accepts. The count is expected
    /// transferred enhancements per enhanced craft attempt, a craft that makes no item counting none. The thresholds in force are read from
    /// <see cref="CraftOutcomeRules"/>, and the gate also reports the thresholds that would meet parity for the quality it measured.
    /// </para>
    /// </summary>
    public sealed class EnhancementParityGate : IBalanceGate
    {
        private readonly CraftGateSuite _suite;

        internal EnhancementParityGate(CraftGateSuite suite)
        {
            _suite = suite;
        }

        public string Name => GateNames.EnhancementParity;

        /// <summary>One recipe played at one slot count: the recipe, its representative enhancement and the cells both sides played.</summary>
        private sealed record RecipePlay(
            RecipeEntry Recipe, EnhancementItem Item, int Slots, CraftEnhancementConfiguration Configuration, CohortCell New, LegacyEnhancementCell Today);

        /// <summary>One band at one slot count: everything its recipes played, with today's figure and the new system's.</summary>
        private sealed record Row(
            string Band, int Slots, IReadOnlyList<RecipePlay> Plays, QualityDistribution Distribution, double Today, double TodaySuccess,
            double TodayQuality, double New, double NewStandardError, double Ratio, bool Passed);

        public GateResult Evaluate(GateContext context)
        {
            var run = _suite.RunFor(context);
            var thresholds = run.Thresholds;
            var scenario = run.Scenario;

            switch (scenario.Enhancements)
            {
                case EnhancementSupport.NoSlots:
                    return GateResult.NotApplicable(Name, "The minigame has no enhancement slots, so there is nothing to transfer.");
                case EnhancementSupport.NotModeled:
                    return GateResult.NotRun(Name,
                        "The scenario does not apply CraftEngineRequest.Enhancements to its engine, so an enhanced craft would play as easy as a plain one. " +
                        "Map the request's total progress penalty onto the engine's progress meter and set ICraftScenario.Enhancements to Modeled (see SWLOR.CraftSim/README.md).");
            }

            if (context.Enhancements == null)
                return GateResult.NotRun(Name, "The gate context has no enhancement items; load them with EnhancementCatalog.Load.");

            var slotCounts = thresholds.EnhancementSlotCounts.Where(count => count >= 1).Distinct().OrderBy(count => count).ToList();
            if (slotCounts.Count == 0)
                return GateResult.NotRun(Name, "EnhancementSlotCounts names no slot count.");

            var rank = thresholds.EnhancementParityRank;
            var gearIndex = run.Gear.Count - 1;
            var gear = run.Gear[gearIndex];

            // Only recipes that can hold an enhancement are played. An item gets a third slot from research or a blueprint, so every
            // enhanceable recipe is played with each slot count.
            var enhancedBands = run.EnhancedBands(thresholds.EnhancementParityBands);
            var dropped = enhancedBands.SelectMany(band => band.Dropped).ToList();
            var played = enhancedBands.Where(band => band.Recipes.Count > 0).ToList();
            if (played.Count == 0)
            {
                return GateResult.NotRun(Name,
                    "The scenario's cohort has no recipe with an enhancement slot, and an enhancement item to use, in the at-level bands " +
                    $"({string.Join(", ", thresholds.EnhancementParityBands.Select(b => b.Name))}).");
            }

            // The scenario says it models enhancements; check that the engine really gets harder with them, or the gate would pass vacuously.
            var unmodeled = run.UnmodeledRecipes(played.SelectMany(band => band.Recipes), _ => rank, gearIndex, slotCounts[^1]);
            if (unmodeled.Count > 0)
            {
                return new GateResult(Name, GateVerdict.Failed,
                    $"The scenario says it models enhancements, but its engine's progress maximum does not rise with them for {unmodeled.Count} recipe(s), " +
                    $"so enhanced crafts would play as easy as plain ones: {string.Join("; ", unmodeled.Take(3))}.");
            }

            run.PrepareEnhanced(played.SelectMany(band => band.Recipes).SelectMany(entry =>
                slotCounts.Select(count => (entry.Recipe, rank, gearIndex, entry.Configuration(count)))));

            var rows = new List<Row>();
            foreach (var band in played)
            {
                foreach (var count in slotCounts)
                    rows.Add(BuildRow(run, band.Band.Band.Name, band.Recipes, count, rank, gearIndex, thresholds));
            }

            var currentThresholds = CraftOutcomeRules.GetEnhancementThresholds(slotCounts[^1]);
            var tables = new List<GateTable>
            {
                ComparisonTable(rows, rank, gear.Name, currentThresholds, thresholds),
                ThresholdTable(rows, thresholds),
                QualityTable(rows, rank, gear.Name),
                EnhancementTable(played.SelectMany(band => band.Recipes), context.Enhancements, dropped)
            };

            var failing = rows.Where(row => !row.Passed).ToList();
            var joint = EnhancementThresholdSolver.Solve(CasesOf(rows, thresholds));
            var summaryThresholds = joint.Reachable
                ? $" Thresholds that meet parity in every band for this quality: {joint.Describe()}."
                : " No thresholds meet parity in every band at this quality (see the thresholds table).";
            var lowest = rows.Where(row => !double.IsNaN(row.Ratio)).Select(row => row.Ratio).DefaultIfEmpty(double.NaN).Min();
            var lowestText = double.IsNaN(lowest) ? string.Empty : $" (lowest {Number(lowest, 2)} times today's)";
            var current = string.Join(" / ", currentThresholds.Select(t => t + "%"));

            if (failing.Count == 0)
            {
                return new GateResult(Name, GateVerdict.Passed,
                    $"At rank {rank} in {gear.Name}, the thresholds in force ({current}) transfer at least {Number(thresholds.EnhancementParityMinimumRatio, 2)} times today's " +
                    $"expected enhancements for {string.Join(", ", slotCounts)} slot(s) in every band{lowestText}." + summaryThresholds,
                    tables);
            }

            var shortfalls = string.Join("; ", failing.Select(row =>
                $"{row.Band} / {row.Slots} slot(s): {Number(row.New, 2)} against today's {Number(row.Today, 2)}"));
            return new GateResult(Name, GateVerdict.Failed,
                $"At rank {rank} in {gear.Name}, the thresholds in force ({current}) transfer fewer enhancements than today in {failing.Count} case(s)" +
                $"{lowestText}: {shortfalls}." + summaryThresholds,
                tables);
        }

        private Row BuildRow(
            ScenarioRun run, string bandName, IReadOnlyList<EnhancedRecipe> recipes, int count, int rank, int gearIndex, GateThresholds thresholds)
        {
            var plays = recipes.Select(entry =>
            {
                var configuration = entry.Configuration(count);
                return new RecipePlay(
                    entry.Recipe, entry.Item, count, configuration,
                    run.AdaptiveEnhanced(entry.Recipe, rank, gearIndex, configuration),
                    run.LegacyEnhanced(entry.Recipe.Level, rank, gearIndex, configuration));
            }).ToList();

            var distribution = QualityDistribution.From(plays.Select(play => play.New));
            var today = plays.Average(play => play.Today.ExpectedTransfersPerAttempt);
            var todaySuccess = plays.Average(play => play.Today.SuccessRate);
            var madeToday = plays.Sum(play => play.Today.Successes);
            var todayQuality = madeToday == 0 ? 0 : plays.Sum(play => play.Today.QualitySum) / madeToday;
            var now = distribution.ExpectedTransfers(count);
            var passed = now + 1e-9 >= thresholds.EnhancementParityMinimumRatio * today;
            var ratio = today <= 0 ? double.NaN : now / today;
            return new Row(bandName, count, plays, distribution, today, todaySuccess, todayQuality, now, distribution.TransfersStandardError(count), ratio, passed);
        }

        private static IReadOnlyList<ParityCase> CasesOf(IEnumerable<Row> rows, GateThresholds thresholds) =>
            rows.Select(row => new ParityCase($"{row.Band} / {row.Slots}", row.Slots, row.Distribution, thresholds.EnhancementParityMinimumRatio * row.Today)).ToList();

        private static GateTable ComparisonTable(
            IReadOnlyList<Row> rows, int rank, string gear, IReadOnlyList<int> currentThresholds, GateThresholds thresholds)
        {
            var table = new EvidenceTable(
                "Band", "Slots filled", "Recipes", "Penalty each", "Extra progress", "Today: items made", "Today: avg quality",
                "Today: enhancements transferred", "New: items made", "New: avg quality", "New: enhancements transferred", "New / today", "Required", "Verdict");
            foreach (var row in rows)
            {
                var penalties = row.Plays.Select(play => play.Item.ProgressPenalty).ToList();
                table.Add(
                    row.Band,
                    row.Slots.ToString(),
                    row.Plays.Count.ToString(),
                    penalties.Min() == penalties.Max() ? penalties[0].ToString() : $"{penalties.Min()}-{penalties.Max()}",
                    Percent(ExtraProgress(row, rank)),
                    Percent(row.TodaySuccess),
                    Percent(row.TodayQuality),
                    Number(row.Today, 2),
                    Percent(row.Distribution.SuccessShare),
                    row.Distribution.Successes == 0 ? "n/a" : Number(row.Distribution.MeanPercent, 1) + "%",
                    $"{Number(row.New, 2)} (+/- {Number(row.NewStandardError, 2)})",
                    double.IsNaN(row.Ratio) ? "n/a" : Number(row.Ratio, 2),
                    ">= " + Number(thresholds.EnhancementParityMinimumRatio, 2),
                    row.Passed ? "Passed" : "Failed");
            }

            return table.ToGateTable(
                $"Rank {rank}, {gear}, at level: expected enhancements transferred per enhanced craft, today's chance roll against the thresholds in force ({string.Join(" / ", currentThresholds.Select(t => t + "%"))})");
        }

        /// <summary>The share of extra progress the enhancements ask for in the old window: their total penalty over the progress the recipe needs without them.</summary>
        private static double ExtraProgress(Row row, int rank) =>
            row.Plays.Average(play =>
            {
                var plain = new LegacyCraftContext(play.Recipe.Level, rank, 0, 0, 0);
                return plain.MaxProgress <= 0 ? 0 : play.Configuration.TotalProgressPenalty / (double)plain.MaxProgress;
            });

        private static GateTable ThresholdTable(IReadOnlyList<Row> rows, GateThresholds thresholds)
        {
            var table = new EvidenceTable("Scope", "Thresholds that meet parity", "In steps of 5", "Thresholds in force");
            var inForce = string.Join(" / ", CraftOutcomeRules.GetEnhancementThresholds(rows.Max(r => r.Slots)).Select(t => t + "%"));

            void AddScope(string scope, IReadOnlyList<Row> scoped)
            {
                var cases = CasesOf(scoped, thresholds);
                table.Add(scope, EnhancementThresholdSolver.Solve(cases).Describe(), EnhancementThresholdSolver.Solve(cases, 5).Describe(), inForce);
            }

            AddScope("Every band together (the shared thresholds)", rows);
            foreach (var band in rows.Select(r => r.Band).Distinct())
                AddScope($"Band {band} alone", rows.Where(r => r.Band == band).ToList());

            return table.ToGateTable(
                "Thresholds, first / second / third slot, that would meet parity for the quality measured (the strictest set that keeps every case at least at today's, never falling from one slot to the next)");
        }

        private static GateTable QualityTable(IReadOnlyList<Row> rows, int rank, string gear)
        {
            var table = new EvidenceTable("Band", "Slots filled", "Items made", "10th percentile", "25th percentile", "Median", "75th percentile", "90th percentile", "Made at 100%");
            foreach (var row in rows)
            {
                var distribution = row.Distribution;
                table.Add(
                    row.Band,
                    row.Slots.ToString(),
                    Percent(distribution.SuccessShare),
                    distribution.Successes == 0 ? "n/a" : distribution.Percentile(0.10) + "%",
                    distribution.Successes == 0 ? "n/a" : distribution.Percentile(0.25) + "%",
                    distribution.Successes == 0 ? "n/a" : distribution.Percentile(0.50) + "%",
                    distribution.Successes == 0 ? "n/a" : distribution.Percentile(0.75) + "%",
                    distribution.Successes == 0 ? "n/a" : distribution.Percentile(0.90) + "%",
                    Percent(distribution.Attempts == 0 ? 0 : distribution.CountsByPercent[^1] / (double)distribution.Attempts));
            }

            return table.ToGateTable($"Rank {rank}, {gear}: quality of the items the new minigame made, in whole percent, by band and slots filled");
        }

        private static GateTable EnhancementTable(
            IEnumerable<EnhancedRecipe> recipes, EnhancementCatalog catalog, IReadOnlyList<string> dropped)
        {
            var table = new EvidenceTable("Recipe kind", "Enhancement", "Level", "Progress penalty", "Items of that kind and level (min / median / max penalty)", "Recipes");
            foreach (var group in recipes.GroupBy(entry => entry.Item).OrderBy(g => g.Key.Type).ThenBy(g => g.Key.Level).ThenBy(g => g.Key.Resref, StringComparer.Ordinal))
            {
                var summary = catalog.Summarize(group.Key.Type, group.Key.Level);
                table.Add(
                    group.Key.Type.ToString(),
                    $"{group.Key.Name} (`{group.Key.Resref}`)",
                    $"{group.Key.Level} (tier {EnhancementItem.TierName(group.Key.Tier)})",
                    group.Key.ProgressPenalty.ToString(),
                    $"{summary.Count} items: {summary.Minimum} / {Number(summary.Median, 1)} / {summary.Maximum}",
                    group.Count().ToString());
            }

            return table.ToGateTable(
                "Representative enhancements: for each recipe kind the real item at the best level the recipe accepts (the old window refuses one more than 5 levels above the recipe) whose penalty is the lower median of that level" +
                (dropped.Count == 0 ? string.Empty : $"; {dropped.Count} sampled recipe(s) accept no enhancement of their kind and were left out"));
        }
    }
}
