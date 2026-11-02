using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Play;
using SWLOR.CraftSim.Rng;
using SWLOR.CraftSim.Scenario;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using static SWLOR.CraftSim.Report.MarkdownTable;

namespace SWLOR.CraftSim.Gates
{
    /// <summary>
    /// Gate 1, no single rotation (owner decision, October 2026): the player complaint was one rotation winning across every craft, so the
    /// adversary is one fixed action sequence per recipe band and gear tier, searched across the band's sampled recipes for the best average
    /// quality (an action that is unavailable when its turn comes is skipped, as for a player following the rotation). The adaptive player must
    /// beat it in average quality by <see cref="GateThresholds.RotationMarginPoints"/> in every band and gear tier.
    /// <para>
    /// The best fixed sequence for each recipe alone is reported, not gated. A minigame with full state and almost no randomness always has one
    /// per recipe that matches adaptive play, so passing on that figure would only be possible by adding the randomness players asked to
    /// reduce; the question that matters is whether one rotation serves every recipe.
    /// </para>
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

            var work = new List<(int BandIndex, int Gear, RecipeEntry Recipe)>();
            for (var bandIndex = 0; bandIndex < run.Bands.Count; bandIndex++)
            {
                foreach (var gear in Enumerable.Range(0, run.Gear.Count))
                    work.AddRange(run.Bands[bandIndex].Recipes.Select(recipe => (bandIndex, gear, recipe)));
            }

            run.PrepareAdaptive(run.CohortCells());
            var solver = new FixedRotationSolver(run.SolverOptions, actionLimit: run.Thresholds.ActionLimit);

            RotationCase CaseOf((int BandIndex, int Gear, RecipeEntry Recipe) item)
            {
                var rank = run.RankFor(item.Recipe);
                var gear = run.Gear[item.Gear];
                var recipe = item.Recipe;
                return new RotationCase(
                    random => run.CreateEngine(recipe, rank, gear, null, random),
                    new[] { ScenarioRun.StableId(recipe), rank, item.Gear });
            }

            // The reported metric: the best sequence for each recipe on its own, scored on attempts the search never saw.
            var alone = new (SolvedRotation Rotation, double HoldOut, IReadOnlyList<IReadOnlyList<string>> Lines)[work.Count];
            Parallel.For(0, work.Count, ScenarioRun.Parallelism, index =>
            {
                var recipeCase = CaseOf(work[index]);
                var lines = FixedRotationSolver.AdaptiveLines(
                    recipeCase.Factory, run.Scenario.AdaptiveOptions, context.Seed, recipeCase.Stream, run.SolverOptions.AdaptiveSeedLines,
                    run.Thresholds.ActionLimit);
                var rotation = solver.Solve(recipeCase.Factory, context.Seed, recipeCase.Stream, lines);
                var holdOut = FixedRotationSolver.Score(
                    recipeCase.Factory, rotation.Sequence, context.Seed, SimStreams.FixedEvaluation, recipeCase.Stream, context.Runs, run.Thresholds.ActionLimit);
                alone[index] = (rotation, holdOut, lines);
            });

            // The gated figure: one sequence for every recipe of a band at a gear tier. The search starts from every recipe's own best sequence
            // and the adaptive lines already found, so it is at least as strong as any of them applied to the whole band.
            var groups = new List<(int BandIndex, int Gear, List<int> Members)>();
            for (var bandIndex = 0; bandIndex < run.Bands.Count; bandIndex++)
            {
                for (var gearIndex = 0; gearIndex < run.Gear.Count; gearIndex++)
                {
                    groups.Add((bandIndex, gearIndex, Enumerable.Range(0, work.Count)
                        .Where(index => work[index].BandIndex == bandIndex && work[index].Gear == gearIndex)
                        .ToList()));
                }
            }

            var single = new (SolvedRotation Rotation, double[] HoldOutEach)[groups.Count];
            Parallel.For(0, groups.Count, ScenarioRun.Parallelism, index =>
            {
                var members = groups[index].Members;
                var cases = members.Select(member => CaseOf(work[member])).ToList();
                var seeds = members.SelectMany(member => new[] { alone[member].Rotation.Sequence }.Concat(alone[member].Lines)).ToList();
                var rotation = solver.SolveShared(cases, context.Seed, seeds);
                var holdOutEach = FixedRotationSolver.ScoreEach(
                    cases, rotation.Sequence, context.Seed, SimStreams.FixedEvaluation, context.Runs, run.Thresholds.ActionLimit);
                single[index] = (rotation, holdOutEach);
            });

            var summary = new EvidenceTable(
                "Band", "Gear", "Recipes played", "Adaptive avg quality", "Best single rotation avg quality", "Margin", "Required",
                "Best rotation per recipe avg quality (reported)", "Verdict");
            var rotations = new EvidenceTable(
                "Band", "Gear", "Best single rotation for the band", "Training", "Selection", "Hold-out", "Adaptive on these recipes");
            var perRecipe = new EvidenceTable(
                "Band", "Gear", "Recipe", "Best rotation for this recipe alone (reported)", "Its hold-out", "The single rotation on this recipe", "Adaptive on this recipe");
            var failures = new List<string>();
            var smallest = double.MaxValue;
            var smallestCase = string.Empty;
            var smallestAlone = double.MaxValue;
            var largestAlone = double.MinValue;

            for (var index = 0; index < groups.Count; index++)
            {
                var (bandIndex, gearIndex, members) = groups[index];
                var band = run.Bands[bandIndex];
                var adaptiveEach = members.Select(member => run.Adaptive(work[member].Recipe, run.RankFor(work[member].Recipe), gearIndex).MeanScore).ToList();
                var adaptive = adaptiveEach.Average() * 100;
                var singleBest = single[index].HoldOutEach.Average() * 100;
                var aloneBest = members.Average(member => alone[member].HoldOut) * 100;
                var margin = adaptive - singleBest;
                var passed = margin >= run.Thresholds.RotationMarginPoints;
                var label = $"{band.Band.Name} / {run.Gear[gearIndex].Name}";
                if (!passed)
                    failures.Add(label);

                if (margin < smallest)
                {
                    smallest = margin;
                    smallestCase = label;
                }

                smallestAlone = Math.Min(smallestAlone, adaptive - aloneBest);
                largestAlone = Math.Max(largestAlone, adaptive - aloneBest);

                summary.Add(
                    band.Band.Name,
                    run.Gear[gearIndex].Name,
                    band.Recipes.Count.ToString(),
                    Number(adaptive),
                    Number(singleBest),
                    Number(margin),
                    ">= " + Number(run.Thresholds.RotationMarginPoints, 0),
                    Number(aloneBest),
                    passed ? "Passed" : "Failed");

                rotations.Add(
                    band.Band.Name,
                    run.Gear[gearIndex].Name,
                    FixedSequencePolicy.Describe(single[index].Rotation.Sequence),
                    Number(single[index].Rotation.TrainingScore * 100),
                    Number(single[index].Rotation.SelectionScore * 100),
                    Number(singleBest),
                    Number(adaptive));

                for (var position = 0; position < members.Count; position++)
                {
                    var member = members[position];
                    var recipe = work[member].Recipe;
                    perRecipe.Add(
                        band.Band.Name,
                        run.Gear[gearIndex].Name,
                        $"{recipe.Resref} (level {recipe.Level})",
                        FixedSequencePolicy.Describe(alone[member].Rotation.Sequence),
                        Number(alone[member].HoldOut * 100),
                        Number(single[index].HoldOutEach[position] * 100),
                        Number(adaptiveEach[position] * 100));
                }
            }

            var tables = new[]
            {
                summary.ToGateTable("Average quality: adaptive play against the best single fixed rotation of each band and gear tier (quality points; a broken piece counts 0)"),
                rotations.ToGateTable("The single rotation found for each band and gear tier, searched across the band's sampled recipes (average quality in points)"),
                perRecipe.ToGateTable("Reported metric, not gated: the best fixed rotation for each recipe alone, and what the band's single rotation scores on the same recipe (average quality in points)")
            };

            var required = Number(run.Thresholds.RotationMarginPoints, 0);
            var reported = $"The best rotation for each recipe alone, a reported metric, is {Number(smallestAlone)} to {Number(largestAlone)} points behind adaptive play.";
            return failures.Count == 0
                ? new GateResult(Name, GateVerdict.Passed,
                    $"Adaptive play beats the best single fixed rotation by at least {required} points in every band and gear tier (smallest margin {Number(smallest)} in {smallestCase}). {reported}",
                    tables)
                : new GateResult(Name, GateVerdict.Failed,
                    $"One fixed rotation comes within {required} points of adaptive play in {failures.Count} case(s): {string.Join("; ", failures)} (smallest margin {Number(smallest)} in {smallestCase}). {reported}",
                    tables);
        }
    }
}
