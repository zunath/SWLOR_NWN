using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Play;
using SWLOR.CraftSim.Rng;
using SWLOR.CraftSim.Tests.Support;
using SWLOR.Game.Server.Service.CraftService.Minigame;

namespace SWLOR.CraftSim.Tests.Play
{
    [TestFixture]
    public class FixedRotationSolverTests
    {
        private static readonly int[] Stream = { 1, 2, 3 };

        private static readonly FixedRotationSolverOptions Small = new()
        {
            BeamWidth = 4, MaximumLength = 20, TrainingRuns = 10, SelectionRuns = 10, Finalists = 3, PolishPasses = 2,
            PolishStarts = 2, CycleLength = 2, ScreeningRuns = 5, AdaptiveSeedLines = 3
        };

        private static ICraftMinigameEngine NewToy(IMinigameRandom random) => new ToyEngine(random);

        /// <summary>No CP, so D cannot be used: the beam's one finished sequence is the instant finish with no quality.</summary>
        private static ICraftMinigameEngine NewToyWithoutCp(IMinigameRandom random) => new ToyEngine(random, maxCp: 0);

        private static ICraftMinigameEngine NewBench(IMinigameRandom random) => new BenchEngine(random, new BenchTuning { MaxCp = 40 });

        private static ICraftMinigameEngine NewDominant(IMinigameRandom random) =>
            new BenchEngine(random, new BenchTuning { MaxCp = 40, HasPerfect = true });

        [Test]
        public void FindsTheRotationThatBuildsQualityBeforeItFinishes()
        {
            var solved = new FixedRotationSolver(Small).Solve(NewToy, 1, Stream);

            solved.SelectionScore.Should().Be(1.0);
            solved.TrainingScore.Should().Be(1.0);
            FixedRotationSolver.Score(NewToy, solved.Sequence, 1, SimStreams.FixedEvaluation, Stream, 5, CraftPlayer.DefaultActionLimit)
                .Should().Be(1.0);
        }

        [Test]
        public void DoesNotFallForTheActionThatFinishesAtOnceWithNoQuality()
        {
            var solved = new FixedRotationSolver(Small).Solve(NewToy, 1, Stream);

            solved.Sequence.Should().NotBeEmpty();
            solved.Sequence.Take(2).Should().NotContain(ToyEngine.C, "finishing first leaves the item with no quality");
            FixedRotationSolver.Score(NewToy, new[] { ToyEngine.C }, 1, SimStreams.FixedEvaluation, Stream, 5, CraftPlayer.DefaultActionLimit)
                .Should().Be(0);
        }

        [Test]
        public void FindsTheDominantRotationOfASingleAction()
        {
            var solved = new FixedRotationSolver(Small).Solve(NewDominant, 1, Stream);

            solved.SelectionScore.Should().Be(1.0);
            solved.Sequence.Should().Contain(BenchEngine.Perfect);
        }

        [Test]
        public void ARotationForAStochasticEngineBeatsEveryPlainRepeatedAction()
        {
            var solved = new FixedRotationSolver(Small).Solve(NewBench, 1, Stream);

            var holdOut = FixedRotationSolver.Score(NewBench, solved.Sequence, 1, SimStreams.FixedEvaluation, Stream, 40, CraftPlayer.DefaultActionLimit);
            var bestRepeat = new[] { BenchEngine.Stoke, BenchEngine.Press, BenchEngine.Polish, BenchEngine.Mend }
                .Max(action => FixedRotationSolver.Score(NewBench, new[] { action }, 1, SimStreams.FixedEvaluation, Stream, 40, CraftPlayer.DefaultActionLimit));

            holdOut.Should().BeGreaterThan(bestRepeat);
            holdOut.Should().BeGreaterThan(0.2, "a rotation that sets Heat up before it presses and polishes does produce items");
        }

        [Test]
        public void TheRotationIsScoredOnAttemptsItWasNotSearchedOn()
        {
            var solved = new FixedRotationSolver(Small).Solve(NewBench, 1, Stream);

            // The selection score is on different attempts from the training score, so the two differ for a stochastic engine.
            solved.SelectionScore.Should().NotBe(solved.TrainingScore);
        }

        [Test]
        public void ASeedGivesTheSameRotationEveryTime()
        {
            var first = new FixedRotationSolver(Small).Solve(NewBench, 5, Stream);
            var second = new FixedRotationSolver(Small).Solve(NewBench, 5, Stream);

            second.Sequence.Should().Equal(first.Sequence);
            second.SelectionScore.Should().Be(first.SelectionScore);
            second.TrainingScore.Should().Be(first.TrainingScore);
        }

        [Test]
        public void ARotationUsesOnlyTheEnginesActionsAndRespectsTheLongestLength()
        {
            var options = Small with { MaximumLength = 12 };

            var solved = new FixedRotationSolver(options).Solve(NewBench, 2, Stream);

            solved.Sequence.Should().OnlyContain(action => new[] { "stoke", "press", "polish", "mend" }.Contains(action));
            solved.Sequence.Count.Should().BeLessThanOrEqualTo(12);
        }

        [Test]
        public void ActionLinesThatAreNotTheEnginesAreIgnored()
        {
            var lines = new IReadOnlyList<string>[] { new[] { "nonsense", "a" }, Array.Empty<string>(), new[] { ToyEngine.B, ToyEngine.B, ToyEngine.B, ToyEngine.A, ToyEngine.A, ToyEngine.A } };

            var solved = new FixedRotationSolver(Small).Solve(NewToyWithoutCp, 1, Stream, lines);

            solved.Sequence.Should().OnlyContain(action => action == ToyEngine.A || action == ToyEngine.B || action == ToyEngine.C || action == ToyEngine.D);
            solved.SelectionScore.Should().Be(1.0);
        }

        [Test]
        public void LocalSearchImprovesASequenceThatIsNearlyRight()
        {
            // b, b, a, a, a finishes at 68% quality; the local search adds the missing b.
            var solver = new FixedRotationSolver(Small with { PolishPasses = 3 });
            double Train(IReadOnlyList<string> sequence) =>
                FixedRotationSolver.Score(NewToy, sequence, 1, SimStreams.SolverTraining, Stream, 5, CraftPlayer.DefaultActionLimit);
            var start = new List<string> { ToyEngine.B, ToyEngine.B, ToyEngine.A, ToyEngine.A, ToyEngine.A };

            var polished = solver.Polish(start, new List<string> { ToyEngine.A, ToyEngine.B, ToyEngine.C, ToyEngine.D }, _ => Train);

            Train(start).Should().BeApproximately(0.68, 1e-6);
            Train(polished).Should().Be(1.0);
            polished.Count(action => action == ToyEngine.B).Should().BeGreaterThanOrEqualTo(3);
        }

        [Test]
        public void LocalSearchNeverLeavesASequenceWorseThanItFound()
        {
            var solver = new FixedRotationSolver(Small);
            double Train(IReadOnlyList<string> sequence) =>
                FixedRotationSolver.Score(NewBench, sequence, 1, SimStreams.SolverTraining, Stream, 8, CraftPlayer.DefaultActionLimit);
            var start = new List<string> { "stoke", "stoke", "press", "polish", "press" };

            var polished = solver.Polish(start, new List<string> { "stoke", "press", "polish", "mend" }, _ => Train);

            Train(polished).Should().BeGreaterThanOrEqualTo(Train(start));
        }

        [Test]
        public void ACycleOfTwoActionsReachesFullQualityWithoutLocalSearchOrSeeds()
        {
            // "b, a" repeated is a full-quality rotation: the third b comes before the third a.
            var options = Small with { BeamWidth = 1, CycleLength = 2, PolishPasses = 0, AdaptiveSeedLines = 0 };

            var solved = new FixedRotationSolver(options).Solve(NewToy, 1, Stream);

            solved.SelectionScore.Should().Be(1.0);
        }

        [Test]
        public void AdaptiveLinesAreActionLinesOfTheRequestedNumberOfAttempts()
        {
            var lines = FixedRotationSolver.AdaptiveLines(NewBench, new AdaptivePolicyOptions { Depth = 2, ForecastSamples = 1 }, 1, Stream, 4);

            lines.Should().HaveCount(4);
            lines.Should().OnlyContain(line => line.Count > 0 && line.All(action => new[] { "stoke", "press", "polish", "mend" }.Contains(action)));
            FixedRotationSolver.AdaptiveLines(NewBench, AdaptivePolicyOptions.Default, 1, Stream, 0).Should().BeEmpty();
        }

        [Test]
        public void TheActionLimitCapsEverySearchedRotation()
        {
            // Within two actions the best item is Quality 60 from D, then C to finish: no rotation that needs more actions can be chosen.
            var solved = new FixedRotationSolver(Small, actionLimit: 2).Solve(NewToy, 1, Stream);

            solved.SelectionScore.Should().BeApproximately(0.6, 1e-6);
            solved.Sequence.Count.Should().BeLessThanOrEqualTo(2);
            new FixedRotationSolver(Small).Solve(NewToy, 1, Stream).SelectionScore.Should().Be(1.0, "with the usual limit a full-quality rotation exists");
            ((Action)(() => new FixedRotationSolver(Small, actionLimit: 0))).Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void AdaptiveLinesStopAtTheActionLimit()
        {
            var lines = FixedRotationSolver.AdaptiveLines(NewBench, new AdaptivePolicyOptions { Depth = 2, ForecastSamples = 1 }, 1, Stream, 4, actionLimit: 5);

            lines.Should().HaveCount(4);
            lines.Should().OnlyContain(line => line.Count <= 5);
        }

        private static RotationCase Combo(string favored, int recipe, IReadOnlyList<string> offered = null) =>
            new(random => new ComboEngine(random, favored, offered), new[] { recipe, 1, 1 });

        private static readonly IReadOnlyList<string> EachRecipeItsOwnFavorite = new[] { ComboEngine.Red, ComboEngine.Green, ComboEngine.Blue };

        [Test]
        public void ASharedRotationScoresTheBestSingleSequenceAcrossRecipesThatFavorDifferentActions()
        {
            var cases = EachRecipeItsOwnFavorite.Select((favored, index) => Combo(favored, index + 1)).ToList();

            var shared = new FixedRotationSolver(Small).SolveShared(cases, 1);

            // Ten actions make the item whatever they are. Each is the favorite of one recipe in three: (100 + 20 + 20) / 3 whatever the order.
            shared.SelectionScore.Should().BeApproximately(0.4667, 0.001);
            FixedRotationSolver.ScoreShared(cases, shared.Sequence, 1, SimStreams.FixedEvaluation, 3, CraftPlayer.DefaultActionLimit)
                .Should().BeApproximately(0.4667, 0.001);
            foreach (var recipe in cases)
            {
                new FixedRotationSolver(Small).Solve(recipe.Factory, 1, recipe.Stream).SelectionScore
                    .Should().Be(1.0, "each recipe alone has a rotation that makes it at full quality");
            }
        }

        [Test]
        public void ASharedRotationOfRecipesThatFavorTheSameActionIsTheirCommonBest()
        {
            var cases = new[] { Combo(ComboEngine.Red, 1), Combo(ComboEngine.Red, 2), Combo(ComboEngine.Red, 3) };

            var shared = new FixedRotationSolver(Small).SolveShared(cases, 1);

            shared.SelectionScore.Should().Be(1.0);
            shared.Sequence.Should().OnlyContain(action => action == ComboEngine.Red);
        }

        [Test]
        public void ASharedSearchOfOneRecipeIsTheSearchOfThatRecipe()
        {
            var one = new[] { Combo(ComboEngine.Green, 7) };

            var shared = new FixedRotationSolver(Small).SolveShared(one, 3);
            var alone = new FixedRotationSolver(Small).Solve(one[0].Factory, 3, one[0].Stream);

            shared.Sequence.Should().Equal(alone.Sequence);
            shared.SelectionScore.Should().Be(alone.SelectionScore);
        }

        [Test]
        public void ASharedRotationIsRepeatableFromTheSeedAndTheSeedLinesOnlyHelp()
        {
            var cases = EachRecipeItsOwnFavorite.Select((favored, index) => Combo(favored, index + 1)).ToList();
            var lines = new IReadOnlyList<string>[] { new[] { ComboEngine.Green, ComboEngine.Green } };

            var first = new FixedRotationSolver(Small).SolveShared(cases, 5, lines);
            var second = new FixedRotationSolver(Small).SolveShared(cases, 5, lines);
            var without = new FixedRotationSolver(Small).SolveShared(cases, 5);

            second.Sequence.Should().Equal(first.Sequence);
            second.SelectionScore.Should().Be(first.SelectionScore);
            first.SelectionScore.Should().BeGreaterThanOrEqualTo(without.SelectionScore - 1e-9);
        }

        [Test]
        public void ASharedRotationWeighsEveryRecipeTheSameAndScoresEachOnItsOwn()
        {
            var cases = EachRecipeItsOwnFavorite.Select((favored, index) => Combo(favored, index + 1)).ToList();
            var allRed = Enumerable.Repeat(ComboEngine.Red, 10).ToList();

            var each = FixedRotationSolver.ScoreEach(cases, allRed, 1, SimStreams.FixedEvaluation, 2, CraftPlayer.DefaultActionLimit);
            var mean = FixedRotationSolver.ScoreShared(cases, allRed, 1, SimStreams.FixedEvaluation, 2, CraftPlayer.DefaultActionLimit);

            each.Should().HaveCount(3);
            each[0].Should().BeApproximately(1.0, 1e-6);
            each[1].Should().BeApproximately(0.2, 1e-6);
            each[2].Should().BeApproximately(0.2, 1e-6);
            mean.Should().BeApproximately(1.4 / 3, 1e-6);
        }

        [Test]
        public void ASharedRotationSkipsAnActionARecipeDoesNotOfferInsteadOfFailing()
        {
            var redAndGreenOnly = new[] { ComboEngine.Red, ComboEngine.Green };
            var cases = new[] { Combo(ComboEngine.Red, 1, redAndGreenOnly), Combo(ComboEngine.Blue, 2) };
            var sequence = new[] { ComboEngine.Blue, ComboEngine.Red, ComboEngine.Red, ComboEngine.Red, ComboEngine.Red, ComboEngine.Red, ComboEngine.Red, ComboEngine.Red, ComboEngine.Red, ComboEngine.Red, ComboEngine.Red };

            var each = FixedRotationSolver.ScoreEach(cases, sequence, 1, SimStreams.FixedEvaluation, 2, CraftPlayer.DefaultActionLimit);
            var shared = new FixedRotationSolver(Small).SolveShared(cases, 1);

            each[0].Should().Be(1.0, "the first recipe has no blue, so it skips that action and plays red");
            each[1].Should().BeApproximately(0.28, 1e-6, "one blue (10) and nine reds (2 each) for the recipe that favors blue");
            shared.Sequence.Should().Contain(ComboEngine.Blue, "the union of the recipes' actions is searched").And.Contain(ComboEngine.Red);
        }

        [Test]
        public void ASharedRotationNeedsAtLeastOneRecipe()
        {
            ((Action)(() => new FixedRotationSolver(Small).SolveShared(Array.Empty<RotationCase>(), 1))).Should().Throw<ArgumentException>();
            ((Action)(() => new FixedRotationSolver(Small).SolveShared(null, 1))).Should().Throw<ArgumentException>();
        }

        [Test]
        public void OptionsOutsideTheirRangesAreRejected()
        {
            ((Action)(() => new FixedRotationSolver(new FixedRotationSolverOptions { BeamWidth = 0 }))).Should().Throw<ArgumentOutOfRangeException>();
            ((Action)(() => new FixedRotationSolver(new FixedRotationSolverOptions { MaximumLength = 0 }))).Should().Throw<ArgumentOutOfRangeException>();
            ((Action)(() => new FixedRotationSolver(new FixedRotationSolverOptions { TrainingRuns = 0 }))).Should().Throw<ArgumentOutOfRangeException>();
            ((Action)(() => new FixedRotationSolver(new FixedRotationSolverOptions { Finalists = 0 }))).Should().Throw<ArgumentOutOfRangeException>();
            ((Action)(() => new FixedRotationSolver(new FixedRotationSolverOptions { PolishPasses = -1 }))).Should().Throw<ArgumentOutOfRangeException>();
            ((Action)(() => new FixedRotationSolver(new FixedRotationSolverOptions { CycleLength = 6 }))).Should().Throw<ArgumentOutOfRangeException>();
            FixedRotationSolverOptions.Default.Describe().Should().Contain("beam 8").And.Contain("cycles up to 3").And.Contain("12 finalists");
        }
    }
}
