using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Play;
using SWLOR.CraftSim.Rng;
using SWLOR.CraftSim.Scenario;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using static SWLOR.CraftSim.Report.MarkdownTable;

namespace SWLOR.CraftSim.Gates
{
    /// <summary>
    /// Gate 1, no fixed rotation: a solver searches for the best fixed action sequence for each recipe, and the adaptive player must beat
    /// it in average quality by <see cref="GateThresholds.RotationMarginPoints"/> in every recipe band and gear tier. A rotation that
    /// works everywhere is the defect the revamp exists to remove, so this is the gate with the strongest adversary.
    /// </summary>
    public sealed class NoFixedRotationGate : IBalanceGate
    {
        private readonly CraftGateSuite _suite;

        internal NoFixedRotationGate(CraftGateSuite suite)
        {
            _suite = suite;
        }

        public string Name => GateNames.NoFixedRotation;

        public GateResult Evaluate(GateContext context)
        {
            var run = _suite.RunFor(context);
            if (run.Bands.Count == 0)
                return GateResult.NotRun(Name, "The scenario's cohort has no recipes in the catalog.");

            var work = new List<(CohortBand Band, int BandIndex, int Gear, RecipeEntry Recipe)>();
            for (var bandIndex = 0; bandIndex < run.Bands.Count; bandIndex++)
            {
                foreach (var gear in Enumerable.Range(0, run.Gear.Count))
                    work.AddRange(run.Bands[bandIndex].Recipes.Select(recipe => (run.Bands[bandIndex], bandIndex, gear, recipe)));
            }

            run.PrepareAdaptive(run.CohortCells());
            var solver = new FixedRotationSolver(run.Scenario.SolverOptions);
            var solved = new (SolvedRotation Rotation, double HoldOut)[work.Count];
            Parallel.For(0, work.Count, ScenarioRun.Parallelism, index =>
            {
                var (_, _, gearIndex, recipe) = work[index];
                var rank = run.RankFor(recipe);
                var gear = run.Gear[gearIndex];
                var id = ScenarioRun.StableId(recipe);
                Func<IMinigameRandom, ICraftMinigameEngine> factory = random => run.CreateEngine(recipe, rank, gear, null, random);

                var stream = new[] { id, rank, gearIndex };
                var lines = FixedRotationSolver.AdaptiveLines(
                    factory, run.Scenario.AdaptiveOptions, context.Seed, stream, run.Scenario.SolverOptions.AdaptiveSeedLines);
                var rotation = solver.Solve(factory, context.Seed, stream, lines);
                var holdOut = FixedRotationSolver.Score(
                    factory, rotation.Sequence, context.Seed, SimStreams.FixedEvaluation, stream, context.Runs, run.Thresholds.ActionLimit);
                solved[index] = (rotation, holdOut);
            });

            var summary = new EvidenceTable("Band", "Gear", "Recipes played", "Adaptive avg quality", "Best fixed avg quality", "Margin", "Required", "Verdict");
            var rotations = new EvidenceTable("Band", "Gear", "Recipe", "Best fixed rotation", "Training", "Selection", "Hold-out", "Adaptive on this recipe");
            var failures = new List<string>();
            var smallest = double.MaxValue;
            var smallestCase = string.Empty;

            for (var bandIndex = 0; bandIndex < run.Bands.Count; bandIndex++)
            {
                var band = run.Bands[bandIndex];
                for (var gearIndex = 0; gearIndex < run.Gear.Count; gearIndex++)
                {
                    var entries = Enumerable.Range(0, work.Count)
                        .Where(index => work[index].BandIndex == bandIndex && work[index].Gear == gearIndex)
                        .ToList();

                    var adaptive = entries.Average(index => run.Adaptive(work[index].Recipe, run.RankFor(work[index].Recipe), gearIndex).MeanScore) * 100;
                    var fixedBest = entries.Average(index => solved[index].HoldOut) * 100;
                    var margin = adaptive - fixedBest;
                    var passed = margin >= run.Thresholds.RotationMarginPoints;
                    var label = $"{band.Band.Name} / {run.Gear[gearIndex].Name}";
                    if (!passed)
                        failures.Add(label);

                    if (margin < smallest)
                    {
                        smallest = margin;
                        smallestCase = label;
                    }

                    summary.Add(
                        band.Band.Name,
                        run.Gear[gearIndex].Name,
                        band.Recipes.Count.ToString(),
                        Number(adaptive),
                        Number(fixedBest),
                        Number(margin),
                        ">= " + Number(run.Thresholds.RotationMarginPoints, 0),
                        passed ? "Passed" : "Failed");

                    var first = entries[0];
                    var recipe = work[first].Recipe;
                    rotations.Add(
                        band.Band.Name,
                        run.Gear[gearIndex].Name,
                        $"{recipe.Resref} (level {recipe.Level})",
                        FixedSequencePolicy.Describe(solved[first].Rotation.Sequence),
                        Number(solved[first].Rotation.TrainingScore * 100),
                        Number(solved[first].Rotation.SelectionScore * 100),
                        Number(solved[first].HoldOut * 100),
                        Number(run.Adaptive(recipe, run.RankFor(recipe), gearIndex).MeanScore * 100));
                }
            }

            var tables = new[]
            {
                summary.ToGateTable("Average quality: adaptive play against the best fixed rotation (quality points; a broken piece counts 0)"),
                rotations.ToGateTable("The best fixed rotation found for the first recipe played in each band and gear tier (average quality in points)")
            };

            return failures.Count == 0
                ? new GateResult(Name, GateVerdict.Passed,
                    $"Adaptive play beats the best fixed rotation by at least {Number(run.Thresholds.RotationMarginPoints, 0)} points in every band and gear tier (smallest margin {Number(smallest)} in {smallestCase}).",
                    tables)
                : new GateResult(Name, GateVerdict.Failed,
                    $"A fixed rotation comes within {Number(run.Thresholds.RotationMarginPoints, 0)} points of adaptive play in {failures.Count} case(s): {string.Join("; ", failures)} (smallest margin {Number(smallest)} in {smallestCase}).",
                    tables);
        }
    }
}
