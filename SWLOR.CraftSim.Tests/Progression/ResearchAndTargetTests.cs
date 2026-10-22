using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Progression;
using SWLOR.CraftSim.Tests.Support;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Tests.Progression
{
    [TestFixture]
    public class ResearchAndTargetTests
    {
        private static readonly ProgressionAssumptions Assumptions = new();

        [TestCase(1, 1, 0.0, 200)]
        [TestCase(9, 10, 0.0, 20_000)]
        [TestCase(10, 1, 0.0, 918)]
        [TestCase(45, 1, 0.0, 6_898)]
        [TestCase(53, 1, 0.0, 6_898)]
        [TestCase(1, 1, 0.2, 240)]
        public void AJobTakesTwoHundredSecondsTimesTierToTheTwoPointTwoTimesLevelSquared(int recipeLevel, int blueprintLevel, double modifier, int seconds)
        {
            ResearchCohort.JobSeconds(recipeLevel, blueprintLevel, modifier).Should().Be(seconds);
        }

        [Test]
        public void TheTierOfARecipeIsItsLevelOverTenPlusOneAndAtMostFive()
        {
            ResearchCohort.JobSeconds(39, 1, 0).Should().Be(ResearchCohort.JobSeconds(30, 1, 0));
            ResearchCohort.JobSeconds(40, 1, 0).Should().BeGreaterThan(ResearchCohort.JobSeconds(39, 1, 0));
            ResearchCohort.JobSeconds(40, 1, 0).Should().Be(ResearchCohort.JobSeconds(53, 1, 0));
        }

        [Test]
        public void TheCohortStartsAtTheFirstRankThatOpensATierAndGainsJobSlotsAtTwentyFiveAndFifty()
        {
            var rows = ResearchCohort.Rows(ProgressionFixture.RealRecipes, ProgressionFixture.Gear, Assumptions);

            rows.Select(r => r.FabricationRank).Should().Equal(Enumerable.Range(10, 40));
            rows.Should().OnlyContain(r => r.Tier == r.FabricationRank / 10 && r.Tier == CraftRankMilestones.ResearchTierForRank(r.FabricationRank));
            rows.Where(r => r.FabricationRank < 25).Should().OnlyContain(r => r.ConcurrentJobs == 1);
            rows.Where(r => r.FabricationRank >= 25).Should().OnlyContain(r => r.ConcurrentJobs == 2);
            CraftRankMilestones.MaxConcurrentResearchJobsForRank(50).Should().Be(3);
        }

        [Test]
        public void TheRecipeResearchedIsAllowedByTheTierAndPaysTheMost()
        {
            var rows = ResearchCohort.Rows(ProgressionFixture.RealRecipes, ProgressionFixture.Gear, Assumptions);

            rows.Should().OnlyContain(r => Math.Min(r.RecipeLevel / 10 + 1, 5) <= r.Tier);
            rows.Should().OnlyContain(r => r.BaseXp == Skill.GetDeltaXP(r.RecipeLevel - r.FabricationRank));
            rows.Single(r => r.FabricationRank == 10).RecipeLevel.Should().Be(9, "tier 1 allows recipes up to level 9");
            rows.Single(r => r.FabricationRank == 10).BaseXp.Should().Be(450);
        }

        [Test]
        public void TheChartPaysNothingForRecipesFiveLevelsBelowTheRank()
        {
            var rows = ResearchCohort.Rows(ProgressionFixture.RealRecipes, ProgressionFixture.Gear, Assumptions);

            rows.Where(r => r.FabricationRank % 10 >= 4 && r.FabricationRank < 50).Should().OnlyContain(r => r.BaseXp == 0);
            rows.Where(r => r.FabricationRank % 10 == 0).Should().OnlyContain(r => r.BaseXp > 0);
        }

        [Test]
        public void AFirstJobOnANewBlueprintCostsItsLengthInCredits()
        {
            var row = ResearchCohort.Rows(ProgressionFixture.RealRecipes, ProgressionFixture.Gear, Assumptions).First();

            row.FirstJobCredits.Should().Be(row.FirstJobSeconds);
            row.TenthJobSeconds.Should().BeApproximately(row.FirstJobSeconds * 100, 100, "a level-10 job takes 100 times a level-1 job, give or take the whole seconds");
        }

        [Test]
        public void TheCeilingKeepsResearchXpPerHourBelowCraftingXpPerHour()
        {
            var rows = ResearchCohort.Rows(ProgressionFixture.RealRecipes, ProgressionFixture.Gear, Assumptions).Where(r => r.BaseXp > 0).ToList();

            foreach (var row in rows)
            {
                var share = Math.Min(row.CeilingShare.Value, row.ActiveCeilingShare.Value);
                (row.ResearchXpPerHourAtFullShare * share).Should().BeLessThanOrEqualTo(row.CraftingXpPerHour * 1.0000001);
                (row.ResearchXpPerActiveHourAtFullShare * share).Should().BeLessThanOrEqualTo(row.CraftingXpPerHour * 1.0000001);
            }

            Enumerable.Range(10, 40).Select(rank => ResearchCohort.Rows(ProgressionFixture.RealRecipes, ProgressionFixture.Gear, Assumptions).Single(r => r.FabricationRank == rank))
                .Where(r => r.BaseXp == 0).Should().OnlyContain(r => r.CeilingShare == null && r.ActiveCeilingShare == null);
        }

        [Test]
        public void TheSolvedMultiplierJustMeetsTheTarget()
        {
            var solver = new XpTargetSolver(ProgressionFixture.RealRecipes, 0.5);
            var today = solver.SlowestHours(Assumptions);
            var target = today * 0.6;

            var multiplier = solver.SolveMultiplier(Assumptions, target);

            multiplier.Should().NotBeNull();
            multiplier.Value.Should().BeGreaterThan(1.0);
            solver.SlowestHours(Assumptions with { CraftXpMultiplier = multiplier.Value }).Should().BeLessThanOrEqualTo(target + 1e-6);
            solver.SlowestHours(Assumptions with { CraftXpMultiplier = multiplier.Value - 0.01 }).Should().BeGreaterThan(target);
        }

        [Test]
        public void TheMedianOfFourSkillsIsTheMeanOfTheMiddleTwo()
        {
            var solver = new XpTargetSolver(ProgressionFixture.RealRecipes, 0);
            var hours = solver.Skills.Select(skill => solver.Path(skill, Assumptions).TotalHours).OrderBy(h => h).ToList();

            solver.MedianHours(Assumptions).Should().BeApproximately((hours[1] + hours[2]) / 2, 1e-9);
            solver.SlowestHours(Assumptions).Should().Be(hours[3]);
            solver.MedianHours(Assumptions).Should().BeLessThan(solver.SlowestHours(Assumptions));
        }

        [Test]
        public void ATargetOnTheMedianNeedsASmallerMultiplierThanOneOnTheSlowestSkill()
        {
            var solver = new XpTargetSolver(ProgressionFixture.RealRecipes, 0.5);
            var target = solver.MedianHours(Assumptions) * 0.7;

            var onMedian = solver.SolveMultiplier(Assumptions, target, XpTargetMetric.MedianSkill);
            var onSlowest = solver.SolveMultiplier(Assumptions, target, XpTargetMetric.SlowestSkill);

            onMedian.Should().NotBeNull();
            onSlowest.Should().NotBeNull();
            onMedian.Value.Should().BeLessThan(onSlowest.Value);
            solver.MedianHours(Assumptions with { CraftXpMultiplier = onMedian.Value }).Should().BeLessThanOrEqualTo(target + 1e-6);

            var cap = solver.SolveCap(Assumptions, target, XpTargetMetric.MedianSkill);
            cap.Should().NotBeNull();
            solver.MedianHours(Assumptions with { RequiredXpCap = cap.Value }).Should().BeLessThanOrEqualTo(target);
            solver.MedianHours(Assumptions with { RequiredXpCap = cap.Value + 1 }).Should().BeGreaterThan(target);
        }

        [Test]
        public void ATargetAlreadyMetNeedsNoMultiplier()
        {
            var solver = new XpTargetSolver(ProgressionFixture.RealRecipes, 0);

            solver.SolveMultiplier(Assumptions, solver.SlowestHours(Assumptions) * 2).Should().Be(1.0);
        }

        [Test]
        public void TheSolvedCapIsTheLargestOneThatMeetsTheTarget()
        {
            var solver = new XpTargetSolver(ProgressionFixture.RealRecipes, 0.5);
            var target = solver.SlowestHours(Assumptions) * 0.8;

            var cap = solver.SolveCap(Assumptions, target);

            cap.Should().NotBeNull();
            solver.SlowestHours(Assumptions with { RequiredXpCap = cap.Value }).Should().BeLessThanOrEqualTo(target);
            solver.SlowestHours(Assumptions with { RequiredXpCap = cap.Value + 1 }).Should().BeGreaterThan(target);
        }

        [Test]
        public void ATargetNoCapCanMeetIsReportedAsSuch()
        {
            new XpTargetSolver(ProgressionFixture.RealRecipes, 0).SolveCap(Assumptions, 0.01).Should().BeNull();
        }
    }
}
