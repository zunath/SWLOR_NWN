using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Legacy.Gathering;
using SWLOR.CraftSim.Progression;
using SWLOR.CraftSim.Tests.Support;
using SWLOR.Game.Server.Service;

namespace SWLOR.CraftSim.Tests.Progression
{
    [TestFixture]
    public class GatheringProgressionTests
    {
        private static readonly ProgressionAssumptions Assumptions = new();

        private static GatheringPath Path(GatheringStrategy strategy, ProgressionAssumptions assumptions = null) =>
            GatheringProgression.Simulate(strategy, ProgressionFixture.Supply, assumptions ?? Assumptions);

        [TestCase(GatheringStrategy.HarvestAndRefine)]
        [TestCase(GatheringStrategy.Scavenge)]
        [TestCase(GatheringStrategy.Best)]
        public void EveryStrategyClimbsRanksZeroToFortyNineAndCostsTheServersXp(GatheringStrategy strategy)
        {
            var path = Path(strategy);

            path.Steps.Select(s => s.Rank).Should().Equal(Enumerable.Range(0, 50));
            path.RequiredXp.Should().Be(411_125);
            path.Steps.Select(s => s.RequiredXp).Should().Equal(Enumerable.Range(0, 50).Select(Skill.GetRequiredXP));
            path.Steps.Should().OnlyContain(s => s.XpPerSecond > 0);
        }

        [Test]
        public void TheBetterOfTheTwoIsNeverSlowerThanEitherAlone()
        {
            var best = Path(GatheringStrategy.Best);

            best.TotalHours.Should().BeLessThanOrEqualTo(Path(GatheringStrategy.HarvestAndRefine).TotalHours);
            best.TotalHours.Should().BeLessThanOrEqualTo(Path(GatheringStrategy.Scavenge).TotalHours);
        }

        [Test]
        public void ARankZeroHarvesterWorksTheTierOneVeinAndRefinesIt()
        {
            var step = Path(GatheringStrategy.HarvestAndRefine).Steps[0];

            // veldite: the raw ore is 50 of the vein's 55 weight; a harvest pays GetDeltaXP(5 - 0) = 1050 plus 25 XP per refined raw unit.
            var share = 50 / 55.0;
            var seconds = ComponentSupply.SecondsPerHarvestUse(Assumptions) + share * Assumptions.RefineSecondsPerBatch / Assumptions.RefineBatchUnits;
            step.XpPerSecond.Should().BeApproximately((1050 + share * 25) / seconds, 1e-9);
            step.Activity.Should().Contain("raw_veldite");
            step.Tier.Should().Be(1);
        }

        [Test]
        public void ANewTierOfVeinOpensEveryTenRanksAndPaysTheFullPriceAgain()
        {
            var steps = Path(GatheringStrategy.HarvestAndRefine).Steps;

            steps[9].Activity.Should().Contain("raw_veldite", "rank 9 is still tier 1");
            steps[10].Activity.Should().Contain("raw_scordspar");
            steps[10].XpPerSecond.Should().BeGreaterThan(steps[9].XpPerSecond, "the new vein is five levels above the rank again");
            steps[40].Activity.Should().Match("*tier 5*");
        }

        [Test]
        public void ASiteLevelAboveTheTierIsNotSearched()
        {
            var steps = Path(GatheringStrategy.Scavenge).Steps;

            steps[0].Activity.Should().Contain("level 1");
            steps[9].Activity.Should().Contain("level 1");
            steps[10].Activity.Should().Contain("level 2");
            steps[49].Activity.Should().Contain("level 5");
        }

        [Test]
        public void ScavengingPaysTheServersExpectedXpPerSiteOverTheSiteTime()
        {
            var step = Path(GatheringStrategy.Scavenge).Steps[0];

            var xp = ScavengeModel.ExpectedXpPerSite(1, 0, 1, 0);
            var seconds = Assumptions.NodeTravelSeconds + ScavengeModel.ExpectedAttemptsPerSite(1, 0) * Assumptions.SecondsPerScavengeAttempt;
            step.XpPerSecond.Should().BeApproximately(xp / seconds, 1e-9);
        }

        [Test]
        public void LongerWalksBetweenNodesCostMoreHours()
        {
            var near = Path(GatheringStrategy.Best, Assumptions with { NodeTravelSeconds = 15 }).TotalHours;
            var far = Path(GatheringStrategy.Best, Assumptions with { NodeTravelSeconds = 120 }).TotalHours;

            far.Should().BeGreaterThan(near * 3);
        }
    }
}
