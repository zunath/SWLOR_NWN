using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.Engine;
using SWLOR.CombatSim.Report;

namespace SWLOR.CombatSim.Tests.Report
{
    [TestFixture]
    public class CombatantAccumulatorTests
    {
        [Test]
        public void ResourceRates_UseTheTimeActuallySpentInTheFight()
        {
            var accumulator = new CombatantAccumulator("Caster", 0);

            accumulator.Add(new CombatantStats { FpSpent = 10, StmSpent = 5 }, 0.5 / 60.0);

            var summary = accumulator.ToSummary();
            summary.AverageFpSpentPerMinute.Should().BeApproximately(1200, 1e-6, "10 FP in half a second");
            summary.AverageStmSpentPerMinute.Should().BeApproximately(600, 1e-6);
        }

        [Test]
        public void ACombatantThatNeverActed_AddsNoSpendingRate()
        {
            var accumulator = new CombatantAccumulator("Latecomer", 0);

            accumulator.Add(new CombatantStats(), 0);

            var summary = accumulator.ToSummary();
            double.IsFinite(summary.AverageFpSpentPerMinute).Should().BeTrue();
            summary.AverageFpSpentPerMinute.Should().Be(0);
        }
    }
}
