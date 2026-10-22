using SWLOR.CraftSim.Data;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using static SWLOR.CraftSim.Report.MarkdownTable;

namespace SWLOR.CraftSim.Gates
{
    /// <summary>
    /// Gate 5, material parity (the crafting item of the plan's economy-parity gate): the component stacks consumed per item made,
    /// counting the stacks a failed craft loses, must be within <see cref="GateThresholds.MaterialTolerance"/> of the legacy baseline in
    /// every recipe band and gear tier. Both sides play the same recipes at the same ranks and gear; the legacy side is the old minigame's
    /// lookahead player, recomputed here with the legacy model and the same seed. Stacks per item is the baseline's own figure: a success
    /// consumes every stack, a failure each stack at the 65% loss chance, so stacks x (p + 0.65 x (1 - p)) per attempt for a success
    /// chance p, divided by the items an attempt makes.
    /// </summary>
    public sealed class MaterialParityGate : IBalanceGate
    {
        private readonly CraftGateSuite _suite;

        internal MaterialParityGate(CraftGateSuite suite)
        {
            _suite = suite;
        }

        public string Name => GateNames.EconomyParity;

        public GateResult Evaluate(GateContext context)
        {
            var run = _suite.RunFor(context);
            if (run.Bands.Count == 0)
                return GateResult.NotRun(Name, "The scenario's cohort has no recipes in the catalog.");

            run.PrepareAdaptive(run.CohortCells());
            run.PrepareLegacy(run.CohortCells().Select(cell => (cell.Recipe.Level, cell.Rank, cell.GearIndex)));

            var table = new EvidenceTable(
                "Band", "Gear", "Recipes played", "New stacks per item", "Legacy stacks per item", "Ratio",
                "New success", "Legacy success", "New avg quality", "Legacy avg quality", "Never made (new / legacy)", "Verdict");
            var failures = new List<string>();
            var compared = 0;

            foreach (var band in run.Bands)
            {
                for (var gearIndex = 0; gearIndex < run.Gear.Count; gearIndex++)
                {
                    var newSide = new Side();
                    var legacySide = new Side();
                    foreach (var recipe in band.Recipes)
                    {
                        var rank = run.RankFor(recipe);
                        var cell = run.Adaptive(recipe, rank, gearIndex);
                        var legacy = run.Legacy(recipe.Level, rank, gearIndex);
                        newSide.Add(recipe, cell.SuccessRate, cell.MeanScore);
                        legacySide.Add(recipe, legacy.SuccessRate, legacy.MeanScore);
                    }

                    var label = $"{band.Band.Name} / {run.Gear[gearIndex].Name}";
                    if (legacySide.Items <= 0)
                    {
                        table.Add(band.Band.Name, run.Gear[gearIndex].Name, band.Recipes.Count.ToString(), newSide.Items <= 0 ? "never made" : Number(newSide.StacksPerItem, 2), "n/a", "n/a",
                            Percent(newSide.SuccessRate), Percent(legacySide.SuccessRate), Percent(newSide.Quality), Percent(legacySide.Quality),
                            $"{Percent(newSide.NeverShare)} / {Percent(legacySide.NeverShare)}", "Skipped: the legacy baseline makes no item here");
                        continue;
                    }

                    compared++;
                    var ratio = newSide.Items <= 0 ? double.PositiveInfinity : newSide.StacksPerItem / legacySide.StacksPerItem;
                    var withinTolerance = Math.Abs(ratio - 1) <= run.Thresholds.MaterialTolerance;
                    var neverOk = newSide.NeverShare <= legacySide.NeverShare + run.Thresholds.MaterialExtraNeverCraftedShare;
                    var passed = withinTolerance && neverOk;
                    if (!passed)
                        failures.Add($"{label} (ratio {(double.IsInfinity(ratio) ? "never made" : Number(ratio, 2))})");

                    table.Add(
                        band.Band.Name,
                        run.Gear[gearIndex].Name,
                        band.Recipes.Count.ToString(),
                        newSide.Items <= 0 ? "never made" : Number(newSide.StacksPerItem, 2),
                        Number(legacySide.StacksPerItem, 2),
                        double.IsInfinity(ratio) ? "n/a" : Number(ratio, 2),
                        Percent(newSide.SuccessRate),
                        Percent(legacySide.SuccessRate),
                        Percent(newSide.Quality),
                        Percent(legacySide.Quality),
                        $"{Percent(newSide.NeverShare)} / {Percent(legacySide.NeverShare)}",
                        passed ? "Passed" : "Failed");
                }
            }

            var tables = new[]
            {
                table.ToGateTable("Component stacks consumed per item made, failures included (both sides play to maximize quality; quality is the mean over attempts, a failure counting 0)")
            };

            if (compared == 0)
                return new GateResult(Name, GateVerdict.NotRun, "The legacy baseline makes no item in any band, so there is nothing to compare.", tables);

            var tolerance = Percent(run.Thresholds.MaterialTolerance);
            return failures.Count == 0
                ? new GateResult(Name, GateVerdict.Passed, $"Component stacks per item are within {tolerance} of the legacy baseline in every band and gear tier.", tables)
                : new GateResult(Name, GateVerdict.Failed, $"Outside {tolerance} of the legacy baseline: {string.Join("; ", failures)}.", tables);
        }

        /// <summary>The materials a band's recipes use on one side of the comparison.</summary>
        private sealed class Side
        {
            private const double LossShare = CraftOutcomeRules.ChanceToLoseItemPercent / 100.0;

            private int _recipes;
            private int _never;
            private double _success;
            private double _score;

            public double Stacks { get; private set; }
            public double Items { get; private set; }

            /// <summary>Stacks consumed per item made, over the recipes this side ever makes; recipes it never makes are counted in <see cref="NeverShare"/>.</summary>
            public double StacksPerItem => Items <= 0 ? double.NaN : Stacks / Items;

            public double NeverShare => _recipes == 0 ? 0 : _never / (double)_recipes;
            public double SuccessRate => _recipes == 0 ? 0 : _success / _recipes;
            public double Quality => _recipes == 0 ? 0 : _score / _recipes;

            public void Add(RecipeEntry recipe, double successRate, double meanScore)
            {
                _recipes++;
                _success += successRate;
                _score += meanScore;
                if (successRate <= 0)
                {
                    _never++;
                    return;
                }

                Stacks += recipe.ComponentStacks * (successRate + LossShare * (1 - successRate));
                Items += successRate * recipe.Quantity;
            }
        }
    }
}
