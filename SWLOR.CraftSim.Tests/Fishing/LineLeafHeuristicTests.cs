using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Fishing;
using SWLOR.CraftSim.Play;
using SWLOR.Game.Server.Service.CraftService.Minigame;

namespace SWLOR.CraftSim.Tests.Fishing
{
    [TestFixture]
    public class LineLeafHeuristicTests
    {
        private static double Value(MinigameStatus status, int reeledIn, int wornDown, int lineStrength)
        {
            var meters = new MinigameMeters(
                new MinigameMeter(reeledIn, 30),
                new MinigameMeter(wornDown, 20),
                new MinigameMeter(lineStrength, 20),
                new MinigameMeter(0, 0));
            return LineLeafHeuristic.Instance.Evaluate(new LeafPosition(meters, status, FinishEstimate.None));
        }

        [Test]
        public void ALandedFishIsWorthOne_AndALostFishNothing()
        {
            Value(MinigameStatus.Succeeded, 30, 0, 0).Should().Be(1);
            Value(MinigameStatus.Failed, 5, 20, 20).Should().Be(0);
        }

        [Test]
        public void AFightStillGoing_IsWorthLessThanALandedFishAndMoreThanALostOne()
        {
            var going = Value(MinigameStatus.InProgress, 20, 10, 10);

            going.Should().BeGreaterThan(0).And.BeLessThan(1);
            Value(MinigameStatus.InProgress, 29, 20, 20).Should().BeLessThan(1, "even a nearly landed fish is not yet landed");
        }

        [Test]
        public void ACloserFish_AMoreWornDownFish_AndAStrongerLine_EachRaiseTheValue()
        {
            var baseline = Value(MinigameStatus.InProgress, 15, 10, 10);

            Value(MinigameStatus.InProgress, 20, 10, 10).Should().BeGreaterThan(baseline);
            Value(MinigameStatus.InProgress, 15, 15, 10).Should().BeGreaterThan(baseline);
            Value(MinigameStatus.InProgress, 15, 10, 15).Should().BeGreaterThan(baseline);
        }

        [Test]
        public void ItRefusesWeightsThatCannotScoreAPosition()
        {
            ((Action)(() => new LineLeafHeuristic(-1, 0.3, 0.2))).Should().Throw<ArgumentOutOfRangeException>();
            ((Action)(() => new LineLeafHeuristic(0, 0, 0))).Should().Throw<ArgumentException>();
            ((Action)(() => new LineLeafHeuristic(0.4, 0.3, 0.3, 1.0))).Should().Throw<ArgumentOutOfRangeException>();
            ((Action)(() => new LineLeafHeuristic(0.4, 0.3, 0.3, 0.0))).Should().Throw<ArgumentOutOfRangeException>();
        }
    }
}
