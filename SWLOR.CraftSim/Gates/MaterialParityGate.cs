using SWLOR.CraftSim.Data;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using static SWLOR.CraftSim.Report.MarkdownTable;

namespace SWLOR.CraftSim.Gates
{
    /// <summary>The verdict of gate 5's stack comparison for one band and gear tier, before the never-made and quality checks.</summary>
    /// <param name="Passed">The stacks per item are acceptable.</param>
    /// <param name="TwoSided">The legacy policy succeeds often enough that the parity is two-sided.</param>
    /// <param name="UsesLess">The parity is one-sided and the new minigame uses more than the tolerance less material than today.</param>
    public readonly record struct MaterialParityJudgment(bool Passed, bool TwoSided, bool UsesLess)
    {
        /// <summary>The rule that applied, for the report; a one-sided pass on lower use says how much less.</summary>
        public string Describe(double ratio, GateThresholds thresholds)
        {
            var tolerance = Number(thresholds.MaterialTolerance * 100, 0) + "%";
            if (TwoSided)
                return $"within {tolerance}";

            return UsesLess
                ? $"at most +{tolerance}; uses {Number((1 - ratio) * 100, 0)}% less, passes"
                : $"at most +{tolerance}";
        }
    }

    /// <summary>
    /// Gate 5's one-sided rule (owner decision, October 2026). In a cell where the legacy policy succeeds at least
    /// <see cref="GateThresholds.MaterialTwoSidedMinimumLegacySuccess"/> of the time, the new stacks per item must be within
    /// <see cref="GateThresholds.MaterialTolerance"/> of the legacy figure either way. Where it succeeds less, today's failures waste
    /// material, and the cell fails only if the new minigame uses more than the tolerance more; lower use passes.
    /// </summary>
    public static class MaterialParityRule
    {
        // A figure that sits exactly on a threshold (a mean of recipes' rates, a ratio of 1.10) can land a rounding error on the wrong side of it.
        private const double Epsilon = 1e-9;

        /// <param name="ratio">New stacks per item divided by the legacy figure; infinite when the new minigame makes no item.</param>
        /// <param name="legacySuccessRate">The legacy policy's success rate in the cell, 0 to 1.</param>
        /// <param name="thresholds">The tolerance and the legacy success rate where the parity turns two-sided.</param>
        public static MaterialParityJudgment Judge(double ratio, double legacySuccessRate, GateThresholds thresholds)
        {
            var twoSided = legacySuccessRate + Epsilon >= thresholds.MaterialTwoSidedMinimumLegacySuccess;
            if (double.IsNaN(ratio) || double.IsInfinity(ratio))
                return new MaterialParityJudgment(false, twoSided, false);

            var passed = twoSided
                ? Math.Abs(ratio - 1) <= thresholds.MaterialTolerance + Epsilon
                : ratio <= 1 + thresholds.MaterialTolerance + Epsilon;
            return new MaterialParityJudgment(passed, twoSided, !twoSided && ratio < 1 - thresholds.MaterialTolerance - Epsilon);
        }
    }

    /// <summary>
    /// Gate 5, material parity (the crafting item of the plan's economy-parity gate): the component stacks consumed per item made,
    /// counting the stacks a failed craft loses, against the legacy baseline in every recipe band and gear tier. Where the legacy policy
    /// succeeds at least <see cref="GateThresholds.MaterialTwoSidedMinimumLegacySuccess"/> of the time the parity is two-sided, within
    /// <see cref="GateThresholds.MaterialTolerance"/> either way. Where today's crafter fails more often (owner decision, October 2026:
    /// today's failures waste material, so a minigame that fails less is not held to them) the parity is one-sided: the cell fails only if
    /// the new minigame uses more than the tolerance more material than today, and lower use passes and is shown in the report
    /// (<see cref="MaterialParityRule"/>). Both sides play the same recipes at the same ranks and gear; the legacy side is the old minigame's
    /// lookahead player, recomputed here with the legacy model and the same seed. Stacks per item is the baseline's own figure: a success
    /// consumes every stack, a failure each stack at the 65% loss chance, so stacks x (p + 0.65 x (1 - p)) per attempt for a success
    /// chance p, divided by the items an attempt makes. "At matched quality": the new minigame's average quality must also be at least the
    /// legacy player's, within <see cref="GateThresholds.MaterialQualityShortfallPoints"/>, so a minigame that makes much worse items
    /// cannot match materials per item by being easy; the materials are then those of items at least as good as today's.
    /// <para>
    /// The comparison is over the recipes the legacy policy makes at least once in the cell. A recipe it never makes has no legacy
    /// figure, and counting it on the new side alone would inflate the new stacks per item with a cost the legacy side never pays; those
    /// recipes are reported separately (how many, and how the new minigame fares on them) and still count in the never-made check.
    /// </para>
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
                "New success", "Legacy success", "New avg quality", "Legacy avg quality", "Quality matched", "Never made (new / legacy)", "Parity rule", "Verdict");
            var unmatched = new EvidenceTable(
                "Band", "Gear", "Recipe", "Level", "New success", "New avg quality");
            var failures = new List<string>();
            var compared = 0;
            var usesLess = 0;

            foreach (var band in run.Bands)
            {
                for (var gearIndex = 0; gearIndex < run.Gear.Count; gearIndex++)
                {
                    // The stacks, success and quality figures are over the recipes the legacy policy makes at least once; the never-made
                    // shares are over every recipe played, so a recipe only one side can make still counts against it.
                    var newSide = new Side();
                    var legacySide = new Side();
                    var newAll = new Side();
                    var legacyAll = new Side();
                    var legacyNever = new List<(RecipeEntry Recipe, double SuccessRate, double MeanScore)>();
                    foreach (var recipe in band.Recipes)
                    {
                        var rank = run.RankFor(recipe);
                        var cell = run.Adaptive(recipe, rank, gearIndex);
                        var legacy = run.Legacy(recipe.Level, rank, gearIndex);
                        newAll.Add(recipe, cell.SuccessRate, cell.MeanScore);
                        legacyAll.Add(recipe, legacy.SuccessRate, legacy.MeanScore);
                        if (legacy.SuccessRate > 0)
                        {
                            newSide.Add(recipe, cell.SuccessRate, cell.MeanScore);
                            legacySide.Add(recipe, legacy.SuccessRate, legacy.MeanScore);
                        }
                        else
                        {
                            legacyNever.Add((recipe, cell.SuccessRate, cell.MeanScore));
                        }
                    }

                    foreach (var (recipe, successRate, meanScore) in legacyNever)
                    {
                        unmatched.Add(
                            band.Band.Name, run.Gear[gearIndex].Name, recipe.Type.ToString(), recipe.Level.ToString(), Percent(successRate), Percent(meanScore));
                    }

                    var label = $"{band.Band.Name} / {run.Gear[gearIndex].Name}";
                    if (legacySide.Items <= 0)
                    {
                        table.Add(band.Band.Name, run.Gear[gearIndex].Name, band.Recipes.Count.ToString(), newAll.Items <= 0 ? "never made" : Number(newAll.StacksPerItem, 2), "n/a", "n/a",
                            Percent(newAll.SuccessRate), Percent(legacyAll.SuccessRate), Percent(newAll.Quality), Percent(legacyAll.Quality), "n/a",
                            $"{Percent(newAll.NeverShare)} / {Percent(legacyAll.NeverShare)}", "n/a", "Skipped: the legacy baseline makes no item here");
                        continue;
                    }

                    compared++;
                    var ratio = newSide.Items <= 0 ? double.PositiveInfinity : newSide.StacksPerItem / legacySide.StacksPerItem;
                    var parity = MaterialParityRule.Judge(ratio, legacySide.SuccessRate, run.Thresholds);
                    var neverOk = newAll.NeverShare <= legacyAll.NeverShare + run.Thresholds.MaterialExtraNeverCraftedShare;
                    var qualityMatched = newSide.Quality * 100 + run.Thresholds.MaterialQualityShortfallPoints >= legacySide.Quality * 100;
                    var passed = parity.Passed && neverOk && qualityMatched;
                    if (passed && parity.UsesLess)
                        usesLess++;

                    if (!passed)
                    {
                        failures.Add(
                            $"{label} (ratio {(double.IsInfinity(ratio) ? "never made" : Number(ratio, 2))}" +
                            (qualityMatched ? ")" : $", quality {Percent(newSide.Quality)} against {Percent(legacySide.Quality)})"));
                    }

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
                        qualityMatched ? "yes" : "no",
                        $"{Percent(newAll.NeverShare)} / {Percent(legacyAll.NeverShare)}",
                        parity.Describe(ratio, run.Thresholds),
                        passed ? "Passed" : "Failed");
                }
            }

            var tables = new List<GateTable>
            {
                table.ToGateTable(
                    "Component stacks consumed per item made, failures included, over the recipes the legacy policy makes at least once in the cell " +
                    "(both sides play to maximize quality; quality is the mean over attempts, a failure counting 0)")
            };

            if (unmatched.RowCount > 0)
            {
                tables.Add(unmatched.ToGateTable(
                    "Recipes the legacy policy never makes in the cell, left out of the stack and quality comparison above (they still count in the never-made check)"));
            }

            if (compared == 0)
                return new GateResult(Name, GateVerdict.NotRun, "The legacy baseline makes no item in any band, so there is nothing to compare.", tables);

            var tolerance = Percent(run.Thresholds.MaterialTolerance);
            var twoSidedFrom = Percent(run.Thresholds.MaterialTwoSidedMinimumLegacySuccess);
            return failures.Count == 0
                ? new GateResult(Name, GateVerdict.Passed,
                    $"Component stacks per item are within {tolerance} of the legacy baseline where its success rate is at least {twoSidedFrom}, and at most {tolerance} above it where it is lower, " +
                    "at no less quality, in every band and gear tier." +
                    (usesLess == 0 ? string.Empty : $" {usesLess} case(s) use more than {tolerance} less material than today, which passes."),
                    tables)
                : new GateResult(Name, GateVerdict.Failed,
                    $"Outside {tolerance} of the legacy baseline (above it only, where its success rate is below {twoSidedFrom}), or below its quality: {string.Join("; ", failures)}.",
                    tables);
        }

        /// <summary>The materials a band's recipes use on one side of the comparison.</summary>
        private sealed class Side
        {
            private const double LossShare = CraftOutcomeRules.ChanceToLoseItemPercent / 100.0;

            private int _recipes;
            private int _never;
            private double _success;
            private double _score;
            private double _weight;

            public double Stacks { get; private set; }
            public double Items { get; private set; }

            /// <summary>Stacks consumed per item made, over the recipes this side ever makes; recipes it never makes are counted in <see cref="NeverShare"/>.</summary>
            public double StacksPerItem => Items <= 0 ? double.NaN : Stacks / Items;

            public double NeverShare => _recipes == 0 ? 0 : _never / (double)_recipes;
            public double SuccessRate => _recipes == 0 ? 0 : _success / _recipes;

            /// <summary>The average quality of an attempt, a failure counting 0, weighted by the items a craft of each recipe makes like the material figure is.</summary>
            public double Quality => _weight == 0 ? 0 : _score / _weight;

            public void Add(RecipeEntry recipe, double successRate, double meanScore)
            {
                _recipes++;
                _success += successRate;
                _score += meanScore * recipe.Quantity;
                _weight += recipe.Quantity;
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
