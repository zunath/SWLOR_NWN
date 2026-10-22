using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Progression;
using SWLOR.CraftSim.Tests.Support;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame.Experiment;
using SWLOR.Game.Server.Service.CraftService.Research;
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
        public void TheStartingXpIsTheResearchWorkstreamsOwnRule()
        {
            var rows = ResearchCohort.Rows(ProgressionFixture.RealRecipes, ProgressionFixture.Gear, Assumptions).Where(r => r.BaseXp > 0).ToList();

            foreach (var row in rows)
            {
                var expected = ResearchXpRules.ExperimentXp(row.BaseXp, 1, 100) + ResearchXpRules.CompletionXp(row.BaseXp, 1);
                row.StartingLevel1XpPerJob.Should().Be(expected);
                ResearchCohort.StartingXpPerJob(row.BaseXp, 1).Should().Be(expected);
                row.BaseXp.Should().Be(ResearchXpRules.BaseXp(row.RecipeLevel, row.FabricationRank));
            }

            // 450 chart XP: 100% x 1.1 and 50% x 1.1 for a level-1 job.
            ResearchCohort.StartingXpPerJob(450, 1).Should().Be(495 + 247);
            ResearchCohort.StartingXpPerJob(450, 10).Should().Be(900 + 450);
        }

        [Test]
        public void TheStartingXpPerHourFollowsTheJobsPerHourAndTheTimeCut()
        {
            var rows = ResearchCohort.Rows(ProgressionFixture.RealRecipes, ProgressionFixture.Gear, Assumptions).Where(r => r.BaseXp > 0).ToList();

            foreach (var row in rows)
            {
                // The better loop pays at least the fast loop's XP per hour, and never more jobs than the player's own time allows.
                row.StartingXpPerHour.Should().BeGreaterThanOrEqualTo(row.FastLoopJobsPerHour * row.StartingLevel1XpPerJob - 1e-6);
                row.FastLoopJobsPerHour.Should().BeLessThanOrEqualTo(3600.0 / row.ActiveSecondsPerJob + 1e-9);
                row.StartingToCraftingRatio.Should().BeApproximately(row.StartingXpPerHour / row.CraftingXpPerHour, 1e-12);
            }

            var first = rows.First();
            var cut = 1 - ExperimentRules.TimeCutCapPercent / 100.0;
            var byTime = first.ConcurrentJobs * 3600.0 / (first.FirstJobSeconds * cut);
            first.FastLoopJobsPerHour.Should().BeApproximately(Math.Min(byTime, 3600.0 / first.ActiveSecondsPerJob), 1e-9);
        }

        [Test]
        public void TheAssumedExperimentLengthIsTheServersTrialCount()
        {
            Assumptions.ResearchTrials.Should().Be(ExperimentRules.Trials);
        }

        [Test]
        public void TheProposedScaleKeepsResearchUnderCraftingPerElapsedHourAtEveryRank()
        {
            var rows = ResearchCohort.Rows(ProgressionFixture.RealRecipes, ProgressionFixture.Gear, Assumptions).Where(r => r.BaseXp > 0).ToList();
            var scale = rows.Min(r => r.CeilingShare.Value / r.StartingShare.Value);

            scale.Should().BeLessThan(1, "the starting values break the ceiling at the first rank of tier 1");
            foreach (var row in rows)
                (row.StartingToCraftingRatio.Value * scale).Should().BeLessThanOrEqualTo(1.0000001, $"rank {row.FabricationRank}");

            // The own-time yardstick is stricter still, which is why the report shows it as the owner's call.
            rows.Min(r => r.ActiveCeilingShare.Value / r.StartingShare.Value).Should().BeLessThan(scale);
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
