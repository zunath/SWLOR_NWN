using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Fishing;
using SWLOR.CraftSim.Gates;
using SWLOR.CraftSim.Scenario;

namespace SWLOR.CraftSim.Tests.Fishing
{
    [TestFixture]
    public class FishingLengthGateTests
    {
        private static readonly FishingParityOptions Options = FishingParityOptions.Default with
        {
            ReferenceRanks = new[] { 20, 40 }
        };

        [Test]
        public void TheFightsOfFishNearTheAnglersLevelLastThreeToSixTurns()
        {
            var gate = new FishingLengthGate(new FishingParity(40, 1), Options);

            var result = gate.Evaluate(new GateContext(null, null, 40, 1, AttributeSweep.Default));

            result.Gate.Should().Be(GateNames.Length);
            result.Verdict.Should().Be(GateVerdict.Passed, result.Summary);
            var table = result.Tables.Single();
            table.Rows.Select(row => row[0]).Should().Equal("-6", "-5", "-4", "-3", "-2", "-1", "0", "+1", "+2", "+3", "+4");
            table.Rows.Should().OnlyContain(row => double.Parse(row[1]) >= 3 && double.Parse(row[1]) <= 6);
            table.Title.Should().Contain("3-6 actions (fishing fight)");
        }

        [Test]
        public void TheGateJudgesTheFishWhoseCatchChanceIsNeitherZeroNorCapped()
        {
            FishingLengthGate.LowestDelta.Should().Be(-6);
            FishingLengthGate.HighestDelta.Should().Be(4);
        }
    }
}
