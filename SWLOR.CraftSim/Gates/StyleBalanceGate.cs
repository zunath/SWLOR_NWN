using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Scenario;
using static SWLOR.CraftSim.Report.MarkdownTable;

namespace SWLOR.CraftSim.Gates
{
    /// <summary>
    /// Gate 2, style balance: for each pair of alternative styles, each must be the better choice on at least
    /// <see cref="GateThresholds.StyleMinimumWinShare"/> of the recipes, and neither may beat the other by more than
    /// <see cref="GateThresholds.StyleMaximumMeanDifferencePoints"/> quality points on average. Both are played by the adaptive player,
    /// on the same dice, at every gear tier. A scenario with no style pairs is not applicable.
    /// </summary>
    public sealed class StyleBalanceGate : IBalanceGate
    {
        private readonly CraftGateSuite _suite;

        internal StyleBalanceGate(CraftGateSuite suite)
        {
            _suite = suite;
        }

        public string Name => GateNames.StyleBalance;

        public GateResult Evaluate(GateContext context)
        {
            var run = _suite.RunFor(context);
            var pairs = run.Scenario.StylePairs;
            if (pairs.Count == 0)
                return GateResult.NotApplicable(Name, "The scenario offers no style configurations yet, so there is nothing to compare.");

            var recipes = run.Bands.SelectMany(band => band.Recipes).ToList();
            var table = new EvidenceTable("Style pair", "Gear", "Recipes", "First better", "Second better", "First avg quality", "Second avg quality", "Difference", "Verdict");
            var failures = new List<string>();

            foreach (var pair in pairs)
            {
                var eligible = recipes.Where(recipe => run.RankFor(recipe) >= pair.MinimumRank).ToList();
                if (eligible.Count == 0)
                    continue;

                run.PrepareAdaptive(eligible.SelectMany(recipe =>
                    Enumerable.Range(0, run.Gear.Count).SelectMany(gear => new[]
                    {
                        (recipe, run.RankFor(recipe), gear, pair.First),
                        (recipe, run.RankFor(recipe), gear, pair.Second)
                    })));

                for (var gear = 0; gear < run.Gear.Count; gear++)
                {
                    double firstWins = 0, secondWins = 0, firstTotal = 0, secondTotal = 0;
                    foreach (var recipe in eligible)
                    {
                        var rank = run.RankFor(recipe);
                        var first = run.Adaptive(recipe, rank, gear, pair.First).MeanScore * 100;
                        var second = run.Adaptive(recipe, rank, gear, pair.Second).MeanScore * 100;
                        firstTotal += first;
                        secondTotal += second;

                        var difference = first - second;
                        if (Math.Abs(difference) <= run.Thresholds.StyleTieMarginPoints)
                        {
                            firstWins += 0.5;
                            secondWins += 0.5;
                        }
                        else if (difference > 0)
                        {
                            firstWins++;
                        }
                        else
                        {
                            secondWins++;
                        }
                    }

                    var count = eligible.Count;
                    var firstShare = firstWins / count;
                    var secondShare = secondWins / count;
                    var meanDifference = (firstTotal - secondTotal) / count;
                    var passed = firstShare >= run.Thresholds.StyleMinimumWinShare &&
                                 secondShare >= run.Thresholds.StyleMinimumWinShare &&
                                 Math.Abs(meanDifference) <= run.Thresholds.StyleMaximumMeanDifferencePoints;
                    var label = $"{pair.Name} / {run.Gear[gear].Name}";
                    if (!passed)
                        failures.Add(label);

                    table.Add(
                        pair.Name,
                        run.Gear[gear].Name,
                        count.ToString(),
                        Percent(firstShare),
                        Percent(secondShare),
                        Number(firstTotal / count),
                        Number(secondTotal / count),
                        SignedPoints(meanDifference),
                        passed ? "Passed" : "Failed");
                }
            }

            if (table.RowCount == 0)
            {
                return GateResult.NotRun(Name, $"No cohort recipe is played at or above the styles' minimum rank ({string.Join(", ", pairs.Select(p => p.MinimumRank).Distinct())}).");
            }

            var tables = new[] { table.ToGateTable("Style pairs: how often each style is better, and the average quality (quality points; ties count half a win each)") };
            return failures.Count == 0
                ? new GateResult(Name, GateVerdict.Passed,
                    $"Every style is the better choice on at least {Percent(run.Thresholds.StyleMinimumWinShare)} of recipes and no pair differs by more than {Number(run.Thresholds.StyleMaximumMeanDifferencePoints, 0)} points on average.",
                    tables)
                : new GateResult(Name, GateVerdict.Failed,
                    $"{failures.Count} style comparison(s) are out of balance: {string.Join("; ", failures)}.",
                    tables);
        }

        private static string SignedPoints(double value) => (value > 0 ? "+" : string.Empty) + Number(value);
    }
}
