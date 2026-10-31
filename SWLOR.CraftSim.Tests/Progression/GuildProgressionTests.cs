using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Progression;
using SWLOR.CraftSim.Tests.Support;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Tests.Progression
{
    [TestFixture]
    public class GuildProgressionTests
    {
        private static readonly ProgressionAssumptions Assumptions = new();
        private static GuildTaskCatalog Guilds => ProgressionFixture.Guilds;

        // Other: tasks for an item only a recipe that must be learned makes, which a new crafter cannot hand in.
        [TestCase(GuildType.SmitheryGuild, 185, 185, 0, 0)]
        [TestCase(GuildType.EngineeringGuild, 105, 105, 0, 0)]
        [TestCase(GuildType.FabricationGuild, 211, 211, 0, 0)]
        [TestCase(GuildType.AgricultureGuild, 185, 132, 51, 2)]
        public void TheCatalogReadsEveryCraftingGuildTask(GuildType guild, int tasks, int crafted, int caught, int other)
        {
            var all = Guilds.For(guild).ToList();

            all.Should().HaveCount(tasks);
            all.Count(t => t.Kind == GuildTaskKind.Crafted).Should().Be(crafted);
            all.Count(t => t.Kind == GuildTaskKind.Caught).Should().Be(caught);
            all.Count(t => t.Kind == GuildTaskKind.Other).Should().Be(other);
            all.Select(t => t.Tier).Distinct().OrderBy(t => t).Should().Equal(0, 1, 2, 3, 4);
            all.Where(t => t.Kind == GuildTaskKind.Crafted).Should().OnlyContain(t => t.Recipe != null && t.Level == t.Recipe.Level && t.Quantity == 1);
        }

        [Test]
        public void ACatchTaskUsesTheFishesLevelAndAsksForFourFish()
        {
            var task = Guilds.Tasks.Single(t => t.QuestId == "agr_tsk_017");

            task.Kind.Should().Be(GuildTaskKind.Caught);
            task.Resref.Should().Be("moat_carp");
            task.Level.Should().Be(0, "FishType.MoatCarp is level 0");
            task.Quantity.Should().Be(4);
            task.Recipe.Should().BeNull();
            Guilds.Tasks.Single(t => t.QuestId == "agr_tsk_826").Level.Should().Be(50, "FishType frigorifish is level 50");
        }

        [Test]
        public void EveryTierPaysOneRewardAndTheTiersClimb()
        {
            foreach (var guild in new[] { GuildType.SmitheryGuild, GuildType.EngineeringGuild, GuildType.FabricationGuild, GuildType.AgricultureGuild })
            {
                var rewards = Enumerable.Range(0, 5).Select(tier => Guilds.RewardOfTier(guild, tier)).ToList();
                rewards.Select(r => r.Credits).Should().Equal(1000, 2500, 4000, 5500, 7200);
                rewards.Select(r => r.GuildPoints).Should().Equal(21, 81, 117, 156, 195);
            }
        }

        [Test]
        public void GuildPointsFollowTheServersFormula()
        {
            GuildHandInPlan.GuildPointsFor(21, 0).Should().Be(21);
            GuildHandInPlan.GuildPointsFor(81, 1).Should().Be(101);
            GuildHandInPlan.GuildPointsFor(195, 4).Should().Be(390);
        }

        [Test]
        public void ARankFiftyCrafterRaisesAGuildToRankFiveInHoursNotWeeks()
        {
            foreach (var guild in new[] { GuildType.SmitheryGuild, GuildType.EngineeringGuild, GuildType.FabricationGuild, GuildType.AgricultureGuild })
            {
                var chore = GuildCohorts.Chore(Guilds, ProgressionFixture.Supply, guild, Assumptions);

                chore.Select(r => r.Tier).Should().Equal(0, 1, 2, 3, 4);
                chore.Sum(r => r.HandIns).Should().BeApproximately(238, 0.5, "45,000 GP at the guild points each tier pays");
                chore.Should().OnlyContain(r => r.ItemLevel + QuickCraftRules.RankMargin <= 50, "quick craft must allow the item at rank 50");
                chore.Sum(r => r.Hours).Should().BeInRange(0.1, 24, "the board used to take 14-21 days");
                chore.Sum(r => r.Credits).Should().BeApproximately(Enumerable.Range(0, 5).Sum(t => chore[t].HandIns * Guilds.RewardOfTier(guild, t).Credits), 1e-6);
            }
        }

        [Test]
        public void CatchTasksPayTheBiteXpOfTheirFishAndTheChanceFollowsTheServer()
        {
            var rows = GuildCohorts.CatchTasks(Guilds, Assumptions);

            rows.Select(r => r.Tier).Should().Equal(0, 1, 2, 3, 4);
            rows.Should().OnlyContain(r => r.Quantity == 4);
            rows.Should().OnlyContain(r => r.CatchChance == 0.6, "rank equal to level, Willpower score 10: 40 + 0 + 20 percent");
            rows.Should().OnlyContain(r => r.BiteXp == 600);
            rows[0].BitesPerTask.Should().BeApproximately(4 / 0.6, 1e-9);
            rows[0].SecondsPerTask.Should().BeApproximately(4 / 0.6 * Assumptions.FishingSecondsPerBite + Assumptions.HandInSeconds, 1e-9);
            rows[0].HandInXp(0.5, scaledByQuantity: true).Should().Be(1200);
            rows[0].HandInXp(0.5, scaledByQuantity: false).Should().Be(300);
        }

        [Test]
        public void AHandInPaysNoXpUntilTheShareIsSetAndNeverForALevelWithoutATask()
        {
            var model = ProgressionFixture.RealRecipes;
            var none = model.Path(SkillType.Smithery, ProgressionFixture.Gear, Assumptions, GuildHandInPlan.For(Guilds, GuildType.SmitheryGuild, 0));
            var half = model.Path(SkillType.Smithery, ProgressionFixture.Gear, Assumptions, GuildHandInPlan.For(Guilds, GuildType.SmitheryGuild, 0.5));

            none.GuildXp.Should().Be(0);
            half.GuildXp.Should().BeGreaterThan(0);
            half.TotalHours.Should().BeLessThan(none.TotalHours);

            // The Smithery guild has no task for recipes of level 10, 20, 30 or 40, so those ranks hand nothing in.
            foreach (var level in new[] { 10, 20, 30, 40 })
                half.Steps[level].HandIns.Should().Be(0, $"no Smithery task asks for a level {level} recipe");

            half.Steps[5].HandIns.Should().BeGreaterThan(0);
        }

        [Test]
        public void QuickCraftingCheapItemsRaisesTheGuildRankWithoutPayingXp()
        {
            var model = ProgressionFixture.RealRecipes;
            var plan = GuildHandInPlan.For(Guilds, GuildType.EngineeringGuild, 1.0);
            var withCatchUp = model.Path(SkillType.Engineering, ProgressionFixture.Gear, Assumptions, plan);
            var without = model.Path(SkillType.Engineering, ProgressionFixture.Gear, Assumptions, GuildHandInPlan.For(Guilds, GuildType.EngineeringGuild, 1.0, quickCraftCatchUp: false));

            withCatchUp.GuildRankReached.Should().Be(5);
            withCatchUp.CatchUpHandIns.Should().BeGreaterThan(0);
            withCatchUp.CatchUpHours.Should().BeGreaterThan(0);
            without.CatchUpHandIns.Should().Be(0);
            without.GuildRankReached.Should().BeLessThan(5, "crafting at level alone does not earn the guild points to climb");
            withCatchUp.GuildXp.Should().BeGreaterThan(without.GuildXp, "the higher tiers' tasks open once the rank is there");
        }

        [Test]
        public void UnlockedTasksAreTheUpperBoundOnWhatHandInsPay()
        {
            var model = ProgressionFixture.RealRecipes;
            var earned = model.Path(SkillType.Fabrication, ProgressionFixture.Gear, Assumptions, GuildHandInPlan.For(Guilds, GuildType.FabricationGuild, 0.5));
            var open = model.Path(SkillType.Fabrication, ProgressionFixture.Gear, Assumptions, GuildHandInPlan.For(Guilds, GuildType.FabricationGuild, 0.5, ignoreGuildRank: true));

            open.GuildXp.Should().BeGreaterThanOrEqualTo(earned.GuildXp);
            open.CatchUpHandIns.Should().Be(0);
        }

        [Test]
        public void TheGuildRankThresholdsAreTheServers()
        {
            Guild.LoadData();
            Guild.MaxRank.Should().Be(5);
            Guild.GetGPRequiredForRank(4).Should().Be(45_000);
        }
    }
}
