using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Progression;
using SWLOR.CraftSim.Tests.Support;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Tests.Progression
{
    /// <summary>The board keeps ten random tasks per tier, so a task for a level is on it with a chance.</summary>
    [TestFixture]
    public class GuildBoardTests
    {
        [Test]
        public void ATierOfTenOrFewerTasksIsOnTheBoardInFull()
        {
            GuildBoard.ChanceAtLeastOne(10, 1).Should().Be(1);
            GuildBoard.ChanceAtLeastOne(7, 1).Should().Be(1);
        }

        [Test]
        public void TheChanceThatAnyOfSeveralTasksIsAmongTenDrawnFollowsTheHypergeometricLaw()
        {
            // One task in a tier of 35: 10 / 35.
            GuildBoard.ChanceAtLeastOne(35, 1).Should().BeApproximately(10 / 35.0, 1e-12);

            // Two tasks: 1 - (25/35) x (24/34).
            GuildBoard.ChanceAtLeastOne(35, 2).Should().BeApproximately(1 - 25.0 / 35 * 24 / 34, 1e-12);

            // More matching tasks than the tier can leave off the board are always on it.
            GuildBoard.ChanceAtLeastOne(16, 7).Should().Be(1);
            GuildBoard.ChanceAtLeastOne(35, 0).Should().Be(0);
        }

        [Test]
        public void TheChanceRisesWithMoreTasksForTheLevel()
        {
            Enumerable.Range(1, 6).Select(k => GuildBoard.ChanceAtLeastOne(36, k)).Should().BeInAscendingOrder();
        }

        [Test]
        public void ThePreferredTaskOnTheBoardChancesSumToTheChanceAnyEligibleOneIs()
        {
            var costs = new double[] { 10, 20, 30, 40 };

            var (chance, expected) = GuildBoard.PreferredOnBoard(costs, 35);

            // None of four tasks is drawn with probability (25/35)(24/34)(23/33)(22/32).
            chance.Should().BeApproximately(1 - 25.0 / 35 * 24 / 34 * 23 / 33 * 22 / 32, 1e-12);

            // The preferred task on the board is the i-th when it is drawn and none before it is: 10/35, then x 25/34, x 24/33, x 23/32.
            var first = 10 / 35.0;
            var second = first * 25 / 34;
            var third = second * 24 / 33;
            var fourth = third * 23 / 32;
            expected.Should().BeApproximately(
                (10 * first + 20 * second + 30 * third + 40 * fourth) / (first + second + third + fourth), 1e-9);
        }

        [Test]
        public void ThePreferredTaskIsTheCheapestWhenEveryTaskIsOnTheBoard()
        {
            var (chance, expected) = GuildBoard.PreferredOnBoard(new double[] { 7, 9, 11 }, 3);

            chance.Should().Be(1);
            expected.Should().Be(7);
        }

        [Test]
        public void EveryTaskOfATierIsEligibleSoTheChancesCoverEveryBoard()
        {
            var costs = Enumerable.Range(1, 35).Select(i => (double)i).ToList();

            var (chance, expected) = GuildBoard.PreferredOnBoard(costs, 35);

            chance.Should().BeApproximately(1.0, 1e-9, "some task is always on the board");
            expected.Should().BeGreaterThan(1).And.BeLessThan(10, "the cheapest of ten random tasks of 35 is cheap but rarely the very cheapest");
        }

        [Test]
        public void TheExpectedCostMatchesASimulatedDraw()
        {
            var costs = Enumerable.Range(1, 35).Select(i => (double)(i * i)).ToList();
            var (_, expected) = GuildBoard.PreferredOnBoard(costs.Take(20).ToList(), 35);

            var random = new Random(7);
            double total = 0;
            var counted = 0;
            for (var trial = 0; trial < 200_000; trial++)
            {
                var board = Enumerable.Range(0, 35).OrderBy(_ => random.Next()).Take(10).Where(i => i < 20).ToList();
                if (board.Count == 0)
                    continue;

                total += costs[board.Min()];
                counted++;
            }

            (total / counted).Should().BeApproximately(expected, expected * 0.01);
        }

        [Test]
        public void TheRealGuildsPutTheBoardOnEveryTier()
        {
            foreach (var guild in new[] { GuildType.SmitheryGuild, GuildType.EngineeringGuild, GuildType.FabricationGuild, GuildType.AgricultureGuild })
            {
                var plan = GuildHandInPlan.For(ProgressionFixture.Guilds, guild, 0.5);

                plan.TasksPerTier.Keys.OrderBy(t => t).Should().Equal(0, 1, 2, 3, 4);
                plan.TasksPerTier.Values.Should().OnlyContain(count => count > GuildBoard.TasksPerTier, "every tier has more tasks than the board keeps");
                plan.OfferByRecipeLevel.Values.Should().OnlyContain(o => o.ChanceOnBoard > 0 && o.ChanceOnBoard < 1);
            }
        }

        [Test]
        public void AgricultureCatchTasksTakeBoardPlacesFromItsCraftedOnes()
        {
            var plan = GuildHandInPlan.For(ProgressionFixture.Guilds, GuildType.AgricultureGuild, 0.5);

            plan.TasksPerTier[0].Should().Be(ProgressionFixture.Guilds.For(GuildType.AgricultureGuild).Count(t => t.Tier == 0));
            plan.TasksPerTier[0].Should().BeGreaterThan(plan.CraftedTasks.Count(t => t.Tier == 0));
        }

        [Test]
        public void ABoardChanceLowersWhatHandInsPay()
        {
            var model = ProgressionFixture.RealRecipes;
            var plan = GuildHandInPlan.For(ProgressionFixture.Guilds, GuildType.SmitheryGuild, 1.0);
            var path = model.Path(SkillType.Smithery, ProgressionFixture.Gear, new ProgressionAssumptions(), plan);

            // Every at-level craft that finds a task is handed in once, and each level's task is on the board with a chance below one.
            var step = path.Steps[5];
            step.HandIns.Should().BeGreaterThan(0).And.BeLessThan(step.Crafts * step.SuccessRate);
        }
    }
}
