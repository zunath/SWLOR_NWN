using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Play;
using SWLOR.CraftSim.Rng;
using SWLOR.CraftSim.Tests.Support;
using SWLOR.Game.Server.Service.CraftService.Minigame;

namespace SWLOR.CraftSim.Tests.Play
{
    [TestFixture]
    public class LeafHeuristicTests
    {
        private static MinigameMeters Meters(int progress = 0, int quality = 0, int integrity = 50, int cp = 50) =>
            MinigameMeters.Start(100, 100, 50, 50).Apply(new MinigameMeterDelta(progress, quality, integrity - 50, cp - 50));

        [Test]
        public void FinishEstimateListsTheAvailableActionsThatAddProgress()
        {
            var engine = new BenchEngine(SimMinigameRandom.ForRun(1), new BenchTuning());

            var estimate = FinishEstimate.From(engine.PreviewAll());

            // Only Press adds progress: 3 outside the working window at 6 integrity and no CP a turn.
            estimate.Routes.Should().Equal(new FinishRoute(3, 6, 0));
        }

        [Test]
        public void FinishEstimateAveragesAnActionsResultsByTheirChance()
        {
            var engine = new BenchEngine(SimMinigameRandom.ForRun(1), new BenchTuning { HasPerfect = true });

            var estimate = FinishEstimate.From(engine.PreviewAll());

            estimate.Routes.Should().Contain(new FinishRoute(20, 1, 0), "the Perfect action adds 20 progress for 1 integrity");
        }

        [Test]
        public void FinishEstimateIgnoresActionsThatCannotBeUsed()
        {
            var engine = new BenchEngine(SimMinigameRandom.ForRun(1), new BenchTuning { HasPerfect = true, MaxCp = 0 });

            var estimate = FinishEstimate.From(engine.PreviewAll());

            estimate.Routes.Select(route => route.CpPerTurn).Should().OnlyContain(cp => cp == 0);
        }

        [Test]
        public void SafetyIsTheShareOfTheSpareIntegrityTheFinishNeeds()
        {
            var estimate = new FinishEstimate(new[] { new FinishRoute(10, 5, 0) });

            // 10 turns, the last of which costs nothing that matters: 9 x 5 = 45 integrity, with a 1.5 margin 67.5.
            estimate.Safety(Meters(integrity: 50), 1.5).Should().BeApproximately(50 / 67.5, 1e-9);
            estimate.Safety(Meters(integrity: 50), 1.0).Should().Be(1);
            estimate.Safety(Meters(integrity: 10), 1.0).Should().BeApproximately(10 / 45.0, 1e-9);
            estimate.Safety(Meters(integrity: 0), 1.0).Should().Be(0);
        }

        [Test]
        public void SafetyCountsTheCpTheFinishWouldSpend()
        {
            var estimate = new FinishEstimate(new[] { new FinishRoute(10, 0, 4) });

            // 10 uses at 4 CP: 40 CP.
            estimate.Safety(Meters(cp: 20), 1.5).Should().BeApproximately(0.5, 1e-9);
            estimate.Safety(Meters(cp: 50), 1.5).Should().Be(1);
        }

        [Test]
        public void SafetyTakesTheBestRouteAndIsZeroWithNone()
        {
            var cheap = new FinishRoute(5, 1, 0);
            var costly = new FinishRoute(10, 20, 0);

            new FinishEstimate(new[] { costly, cheap }).Safety(Meters(integrity: 30), 1.0).Should().Be(1, "the cheap route finishes with integrity to spare");
            FinishEstimate.None.Safety(Meters(), 1.5).Should().Be(0);
            FinishEstimate.None.Safety(Meters(progress: 100), 1.5).Should().Be(1, "nothing is left to finish");
        }

        [Test]
        public void AFinishedItemIsWorthTheBonusPlusItsQualityAndABrokenPieceNothing()
        {
            var heuristic = new DefaultLeafHeuristic();

            heuristic.Evaluate(new LeafPosition(Meters(100, 40), MinigameStatus.Succeeded, FinishEstimate.None)).Should().BeApproximately(0.5 + 0.5 * 0.4, 1e-6);
            heuristic.Evaluate(new LeafPosition(Meters(100, 100), MinigameStatus.Succeeded, FinishEstimate.None)).Should().Be(1);
            heuristic.Evaluate(new LeafPosition(Meters(50, 100), MinigameStatus.Failed, FinishEstimate.None)).Should().Be(0);
            new DefaultLeafHeuristic(successBonus: 0).FinishedValue(0.4).Should().BeApproximately(0.4, 1e-6);
        }

        [Test]
        public void AnUnfinishedAttemptIsWorthLessThanTheSameItemFinishedAndLessWhenFinishingIsRisky()
        {
            var heuristic = new DefaultLeafHeuristic();
            var safe = new FinishEstimate(new[] { new FinishRoute(10, 1, 0) });
            var risky = new FinishEstimate(new[] { new FinishRoute(10, 20, 0) });

            var finished = heuristic.Evaluate(new LeafPosition(Meters(100, 40), MinigameStatus.Succeeded, FinishEstimate.None));
            var safeValue = heuristic.Evaluate(new LeafPosition(Meters(50, 40), MinigameStatus.InProgress, safe));
            var riskyValue = heuristic.Evaluate(new LeafPosition(Meters(50, 40), MinigameStatus.InProgress, risky));

            safeValue.Should().BeLessThan(finished);
            riskyValue.Should().BeLessThan(safeValue);
        }

        [Test]
        public void MoreProgressAndMoreQualityRaiseAnUnfinishedAttemptsValue()
        {
            var heuristic = new DefaultLeafHeuristic();
            var estimate = new FinishEstimate(new[] { new FinishRoute(10, 1, 0) });

            double Value(int progress, int quality) =>
                heuristic.Evaluate(new LeafPosition(Meters(progress, quality), MinigameStatus.InProgress, estimate));

            Value(60, 30).Should().BeGreaterThan(Value(30, 30));
            Value(30, 60).Should().BeGreaterThan(Value(30, 30));
        }

        [Test]
        public void LeafPositionOfAnEngineReadsItsMetersStatusAndPreviews()
        {
            var engine = new ToyEngine(SimMinigameRandom.ForRun(1));

            var position = LeafPosition.Of(engine);
            engine.Perform(ToyEngine.C);
            var finished = LeafPosition.Of(engine);

            position.Status.Should().Be(MinigameStatus.InProgress);
            position.Finish.Routes.Should().NotBeEmpty();
            finished.Status.Should().Be(MinigameStatus.Succeeded);
            finished.Finish.Routes.Should().BeEmpty();
        }

        [Test]
        public void TheHeuristicRejectsValuesOutsideItsRanges()
        {
            ((Action)(() => new DefaultLeafHeuristic(successBonus: 1))).Should().Throw<ArgumentOutOfRangeException>();
            ((Action)(() => new DefaultLeafHeuristic(successBonus: -0.1))).Should().Throw<ArgumentOutOfRangeException>();
            ((Action)(() => new DefaultLeafHeuristic(safetyMargin: 0.5))).Should().Throw<ArgumentOutOfRangeException>();
            ((Action)(() => new DefaultLeafHeuristic(progressPull: 1))).Should().Throw<ArgumentOutOfRangeException>();
        }
    }
}
