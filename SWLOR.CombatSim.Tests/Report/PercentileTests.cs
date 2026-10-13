using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.Report;

namespace SWLOR.CombatSim.Tests.Report
{
    [TestFixture]
    public class PercentileTests
    {
        [Test]
        public void Of_WithNoValues_IsNull()
        {
            Percentile.Of(null, 50).Should().BeNull();
            Percentile.Of(Array.Empty<double>(), 50).Should().BeNull();
        }

        [TestCase(0)]
        [TestCase(50)]
        [TestCase(100)]
        public void Of_WithOneValue_IsThatValue(double percentile)
        {
            Percentile.Of(new[] { 7.5 }, percentile).Should().Be(7.5);
        }

        [TestCase(0, 1)]
        [TestCase(25, 2)]
        [TestCase(50, 3)]
        [TestCase(75, 4)]
        [TestCase(100, 5)]
        public void Of_ReturnsTheValueAtTheRank(double percentile, double expected)
        {
            Percentile.Of(new[] { 1.0, 2.0, 3.0, 4.0, 5.0 }, percentile).Should().Be(expected);
        }

        [Test]
        public void Of_InterpolatesBetweenNeighbouringValues()
        {
            Percentile.Of(new[] { 10.0, 20.0 }, 50).Should().Be(15);
            Percentile.Of(new[] { 1.0, 2.0, 3.0, 4.0 }, 50).Should().Be(2.5);
            Percentile.Of(Enumerable.Range(1, 10).Select(i => (double)i).ToList(), 90).Should().BeApproximately(9.1, 1e-9);
        }

        [Test]
        public void Of_SortsItsInputWithoutChangingIt()
        {
            var values = new[] { 5.0, 1.0, 4.0, 2.0, 3.0 };

            Percentile.Of(values, 50).Should().Be(3);

            values.Should().Equal(5.0, 1.0, 4.0, 2.0, 3.0);
        }
    }
}
