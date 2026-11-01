using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Legacy.Gathering;
using SWLOR.CraftSim.Rng;

namespace SWLOR.CraftSim.Tests.Legacy.Gathering
{
    [TestFixture]
    public class ScavengeModelTests
    {
        [TestCase(0, 0, 0.0)]
        [TestCase(0, -1, 0.0)]
        [TestCase(2, 1, 0.25)]
        [TestCase(5, 5, 0.75)]
        [TestCase(5, 10, 1.0)]
        [TestCase(0, 1, 0.05)]
        public void SecondAttemptChance_IsTenPercentPerHardLookPlusFivePerPerceptionModifier(int hardLook, int perception, double expected)
        {
            ScavengeModel.SecondAttemptChance(hardLook, perception).Should().BeApproximately(expected, 1e-9);
        }

        [TestCase(6, 0, 0.75)]
        [TestCase(6, -1, 0.70)]
        [TestCase(6, 5, 1.00)]
        [TestCase(9, 5, 0.85)]
        [TestCase(9, 0, 0.60)]
        [TestCase(30, 0, 0.0)]
        public void SuccessChance_IsTheShareOfD20RollsThatReachTheDc(int dc, int perception, double expected)
        {
            ScavengeModel.SuccessChance(dc, perception).Should().BeApproximately(expected, 1e-9);
        }

        [Test]
        public void TheSecondAttemptFacesDc7To9WithEqualWeight()
        {
            ScavengeModel.SecondAttemptSuccessChance(0).Should().BeApproximately((0.70 + 0.65 + 0.60) / 3, 1e-9);
        }

        [Test]
        public void ExpectedSuccessesPerSite_AddsTheSecondAttemptByItsChance()
        {
            ScavengeModel.ExpectedSuccessesPerSite(0, 0).Should().BeApproximately(0.75, 1e-9);
            ScavengeModel.ExpectedSuccessesPerSite(5, 0).Should().BeApproximately(0.75 + 0.5 * 0.65, 1e-9);
            ScavengeModel.ExpectedAttemptsPerSite(5, 0).Should().BeApproximately(1.5, 1e-9);
        }

        [TestCase(1, 0, 1200)]
        [TestCase(2, 15, 1050)]
        [TestCase(1, 10, 600)]
        [TestCase(1, 14, 76)]
        [TestCase(1, 15, 0)]
        public void SuccessXp_FollowsTheServersXpChart(int siteLevel, int rank, int expected)
        {
            ScavengeModel.SuccessXp(siteLevel, rank).Should().Be(expected);
        }

        [Test]
        public void ExpectedXpPerSite_PaysAQuarterForEachFailedAttempt()
        {
            var success = ScavengeModel.SuccessXp(1, 10);
            var expected = 0.75 * success + 0.25 * (success / 4);

            ScavengeModel.ExpectedXpPerSite(1, 10, 0, 0).Should().BeApproximately(expected, 1e-9);
        }

        [Test]
        public void ASiteHasOneOrTwoAttemptsAndAHardLookOfZeroNeverAddsOneAtPerceptionZero()
        {
            var rng = new SimRandom(8);
            for (var site = 0; site < 500; site++)
                ScavengeModel.SearchSite(rng, 1, 0, 0, 0).Attempts.Should().Be(1);

            var attempts = Enumerable.Range(0, 500).Select(_ => ScavengeModel.SearchSite(rng, 1, 0, 5, 5).Attempts).ToList();
            attempts.Should().OnlyContain(a => a == 1 || a == 2);
            attempts.Should().Contain(2);
        }

        [TestCase(0, -1)]
        [TestCase(0, 0)]
        [TestCase(3, 2)]
        [TestCase(5, 5)]
        public void PlayedSitesMatchTheExpectedSuccessesAndAttempts(int hardLook, int perception)
        {
            var rng = new SimRandom(500 + hardLook * 10 + perception);
            const int Sites = 60_000;
            double successes = 0, attempts = 0, xp = 0;
            for (var site = 0; site < Sites; site++)
            {
                var result = ScavengeModel.SearchSite(rng, 3, 10, hardLook, perception);
                successes += result.Successes;
                attempts += result.Attempts;
                xp += result.Xp;
            }

            (successes / Sites).Should().BeApproximately(ScavengeModel.ExpectedSuccessesPerSite(hardLook, perception), 0.02);
            (attempts / Sites).Should().BeApproximately(ScavengeModel.ExpectedAttemptsPerSite(hardLook, perception), 0.02);
            (xp / Sites).Should().BeApproximately(ScavengeModel.ExpectedXpPerSite(3, 10, hardLook, perception), 15);
        }
    }
}
