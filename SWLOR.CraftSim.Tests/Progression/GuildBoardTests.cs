using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Progression;
using SWLOR.CraftSim.Tests.Support;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
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

            var random = new System.Random(7);
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
                plan.OffersByRecipeLevel.Values.SelectMany(offers => offers).Should().OnlyContain(o => o.ChanceOnBoard > 0 && o.ChanceOnBoard < 1);
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
        public void ABatchOfHandInsEarnsTheHigherRankBonusOnceTheThresholdIsCrossed()
        {
            Guild.LoadData();

            // 900 points at rank 0 and 10 hand-ins of a 21-point task: the first 100/21 reach the 1,000-point threshold, the rest earn 26 each.
            var (rank, points) = GuildHandInPlan.AddHandIns(0, 900, 10, rank => GuildHandInPlan.GuildPointsFor(21, rank));

            var before = 100 / 21.0;
            rank.Should().Be(1);
            points.Should().BeApproximately(1000 + (10 - before) * 26, 1e-9);
            points.Should().BeGreaterThan(900 + 10 * 21, "pricing the whole batch at rank 0 would undercount");
        }

        [Test]
        public void AHandInBatchThatStaysInsideARankIsPricedAtThatRank()
        {
            Guild.LoadData();

            GuildHandInPlan.AddHandIns(2, 6000, 4, rank => GuildHandInPlan.GuildPointsFor(117, rank)).Should().Be((2, 6000 + 4 * 175.0));
        }

        [Test]
        public void ABatchCanCrossSeveralRanksAndStopsAtTheLastOne()
        {
            Guild.LoadData();

            var (rank, points) = GuildHandInPlan.AddHandIns(0, 0, 100_000, rank => GuildHandInPlan.GuildPointsFor(195, rank));

            rank.Should().Be(Guild.MaxRank);
            points.Should().BeInRange(Guild.GetGPRequiredForRank(Guild.MaxRank - 1), Guild.GetGPRequiredForRank(Guild.MaxRank) - 1);
        }

        [Test]
        public void AnItemTwoTiersAskForIsHandedInToTheHigherTierOnceItIsUnlocked()
        {
            // Agriculture's sliced sardine is a tier 0 and a tier 1 task (agr_tsk_037 and agr_tsk_226), so a crafter at guild rank 1 can
            // hand it in to either board and prefers the higher tier.
            var plan = GuildHandInPlan.For(ProgressionFixture.Guilds, GuildType.AgricultureGuild, 0.5);
            var tasks = ProgressionFixture.Guilds.Tasks.Where(t => t.Resref == "sliced_sardine").ToList();
            tasks.Select(t => t.Tier).Should().BeEquivalentTo(new[] { 0, 1 });

            var level = tasks[0].Level.Value;
            var offers = plan.OffersByRecipeLevel[level];
            offers.Select(o => o.Tier).Should().Contain(new[] { 0, 1 });

            var atRankZero = HandInOdds.For(plan.Unlocked(offers, 0));
            var atRankOne = HandInOdds.For(plan.Unlocked(offers, 1));

            atRankZero.Tiers.Select(t => t.Tier).Should().Equal(0);
            atRankOne.Tiers.Select(t => t.Tier).Should().Equal(1, 0);
            atRankOne.Chance.Should().BeGreaterThan(atRankZero.Chance, "a second board can hold a task for the level");
            atRankOne.Tiers.Sum(t => t.Probability).Should().BeApproximately(1, 1e-12);
            plan.ExpectedCredits(atRankOne).Should().BeGreaterThan(plan.ExpectedCredits(atRankZero), "the higher tier pays more");
            plan.ExpectedGuildPoints(atRankOne, 1).Should().BeGreaterThan(plan.ExpectedGuildPoints(atRankZero, 1));
        }

        [Test]
        public void TheOddsOfTwoBoardsCombineAsIndependentDraws()
        {
            var odds = HandInOdds.For(new[] { new LevelOffer(0, 1, 36), new LevelOffer(1, 2, 36) });

            var first = 10 / 36.0;
            var second = GuildBoard.ChanceAtLeastOne(36, 2);
            odds.Chance.Should().BeApproximately(1 - (1 - first) * (1 - second), 1e-12);
            odds.Tiers[0].Tier.Should().Be(1);
            odds.Tiers[0].Probability.Should().BeApproximately(second / odds.Chance, 1e-12);
            HandInOdds.For(Array.Empty<LevelOffer>()).Chance.Should().Be(0);
        }

        [Test]
        public void ACatchUpWaitsForABoardAndTheCraftsMeanwhileMissTheAward()
        {
            var model = ProgressionFixture.RealRecipes;
            var plan = GuildHandInPlan.For(ProgressionFixture.Guilds, GuildType.EngineeringGuild, 1.0);
            var quick = new ProgressionAssumptions { PlayHoursPerServerStart = 0 };
            var slow = new ProgressionAssumptions { PlayHoursPerServerStart = 200 };

            var noWait = model.Path(SkillType.Engineering, ProgressionFixture.Gear, quick, plan);
            var waits = model.Path(SkillType.Engineering, ProgressionFixture.Gear, slow, plan);

            noWait.ExpectedRestartsWaited.Should().Be(waits.ExpectedRestartsWaited);
            if (waits.ExpectedRestartsWaited > 0)
                waits.GuildXp.Should().BeLessThan(noWait.GuildXp, "crafts made while waiting for a board cannot be handed in");
        }

        [Test]
        public void TheFirstCraftByHandIsRetriedUntilItSucceeds()
        {
            var plan = GuildHandInPlan.For(ProgressionFixture.Guilds, GuildType.EngineeringGuild, 0.5);
            var perfect = ProgressionFixture.Model(SharedData.Recipes, ProgressionFixture.Supply, ProgressionFixture.Guilds, 1.0, 0.3, 15);
            var flaky = ProgressionFixture.Model(SharedData.Recipes, ProgressionFixture.Supply, ProgressionFixture.Guilds, 0.5, 0.3, 15);

            var easy = perfect.Path(SkillType.Engineering, ProgressionFixture.Gear, new ProgressionAssumptions(), plan);
            var hard = flaky.Path(SkillType.Engineering, ProgressionFixture.Gear, new ProgressionAssumptions(), plan);

            // The one craft by hand costs about twice as many attempts at a 50% chance, so the catch-up costs more per hand-in as well.
            hard.CatchUpHours.Should().BeGreaterThan(easy.CatchUpHours);
        }

        [Test]
        public void AHandInIsTheTaskRecipeNotTheLevelsAverageRecipe()
        {
            var model = ProgressionFixture.RealRecipes;
            var plan = GuildHandInPlan.For(ProgressionFixture.Guilds, GuildType.SmitheryGuild, 0.5);
            var plain = model.Path(SkillType.Smithery, ProgressionFixture.Gear, new ProgressionAssumptions());
            var guild = model.Path(SkillType.Smithery, ProgressionFixture.Gear, new ProgressionAssumptions(), plan);

            // Level 5 has a Smithery task, so a share of the crafts is the task's recipe and its gathering differs from the level's average;
            // level 10 has none, so it is unchanged.
            guild.Steps[5].GatherHours.Should().NotBeApproximately(plain.Steps[5].GatherHours, 1e-9);
            guild.Steps[10].GatherHours.Should().BeApproximately(plain.Steps[10].GatherHours, 1e-9);
            (guild.Steps[5].ModelledUnits + guild.Steps[5].UnmodelledUnits).Should().BeGreaterThan(0);
        }

        [Test]
        public void ACatchUpAndTheChoreReportTheChanceABoardHoldsAnEligibleItem()
        {
            var model = ProgressionFixture.RealRecipes;
            var path = model.Path(SkillType.Engineering, ProgressionFixture.Gear, new ProgressionAssumptions(),
                GuildHandInPlan.For(ProgressionFixture.Guilds, GuildType.EngineeringGuild, 0.5));

            path.CatchUpHandIns.Should().BeGreaterThan(0);
            path.LowestCatchUpBoardChance.Should().BeInRange(0.0001, 1.0);
            model.Path(SkillType.Engineering, ProgressionFixture.Gear, new ProgressionAssumptions()).LowestCatchUpBoardChance.Should().Be(1.0);

            foreach (var row in GuildCohorts.Chore(ProgressionFixture.Guilds, ProgressionFixture.Supply, GuildType.EngineeringGuild, new ProgressionAssumptions()))
                row.BoardChance.Should().BeInRange(0.0001, 1.0);
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
