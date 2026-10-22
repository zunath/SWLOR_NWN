using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Legacy.Crafting;
using SWLOR.CraftSim.Progression;
using SWLOR.CraftSim.Tests.Support;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Tests.Progression
{
    /// <summary>The old window's CP, progress and unlocked actions depend on the rank, so a craft is a (recipe level, rank) pair, not a level.</summary>
    [TestFixture]
    public class CraftResultsTests
    {
        private static readonly ProgressionAssumptions Assumptions = new();

        [Test]
        public void AnAtLevelPairComesFromTheTableAndAnyOtherPairIsPlayedOnceAndRemembered()
        {
            var atLevel = Enumerable.Range(1, 5).ToDictionary(level => level, level => ProgressionFixture.Cell(SkillType.Smithery, level, 0.9, 0.2, 10));
            var plays = new List<(int Level, int Rank)>();
            var results = new CraftResults(atLevel, (level, rank) =>
            {
                plays.Add((level, rank));
                return ProgressionFixture.Cell(SkillType.Smithery, level, 0.5, 0.1, 12);
            });

            results.At(3, 3).Should().BeSameAs(atLevel[3]);
            plays.Should().BeEmpty();

            var offLevel = results.At(1, 0);
            results.At(1, 0).Should().BeSameAs(offLevel);
            results.At(2, 9);
            results.At(2, 9);
            plays.Should().Equal((1, 0), (2, 9));
            results.PlayedOnDemand.Should().Be(2);
        }

        [Test]
        public void AnAtLevelPairWithNoResultsIsAnError()
        {
            var results = new CraftResults(new Dictionary<int, CraftCell>(), (_, _) => null);

            var act = () => results.At(7, 7);

            act.Should().Throw<InvalidDataException>().WithMessage("*level 7*");
        }

        [Test]
        public void TheFirstStepCraftsTheLevelOneRecipeAtRankZeroAndLaterStepsAtTheirOwnLevel()
        {
            var requested = new List<(int Level, int Rank)>();
            var model = ProgressionFixture.Model(
                SharedData.Recipes, ProgressionFixture.Supply, ProgressionFixture.Guilds, 0.95, 0.3, 15, new[] { SkillType.Smithery },
                (skill, gear, level, rank) =>
                {
                    requested.Add((level, rank));
                    return ProgressionFixture.Cell(skill, level, 0.4, 0.1, 6, gear);
                });

            var path = model.Path(SkillType.Smithery, ProgressionFixture.Gear, Assumptions);

            requested.Should().Equal(new[] { (1, 0) }, "only rank 0 is not an at-level pair");
            path.Steps[0].SuccessRate.Should().Be(0.4, "the rank-0 step reads the rank-0 craft");
            path.Steps[1].SuccessRate.Should().Be(0.95, "rank 1 reads the level-1 craft played at rank 1");
            path.Steps[0].ActionsPerCraft.Should().Be(6);
        }

        [Test]
        public void TheManualCraftThatUnlocksQuickCraftIsPlayedAtTheCurrentRank()
        {
            var requested = new List<(SkillType Skill, int Level, int Rank)>();
            var model = ProgressionFixture.Model(
                SharedData.Recipes, ProgressionFixture.Supply, ProgressionFixture.Guilds, 0.95, 0.3, 15, new[] { SkillType.Engineering },
                (skill, gear, level, rank) =>
                {
                    requested.Add((skill, level, rank));
                    return ProgressionFixture.Cell(skill, level, 0.6, 0.2, 14, gear);
                });
            var plan = GuildHandInPlan.For(ProgressionFixture.Guilds, GuildType.EngineeringGuild, 0.5);

            var path = model.Path(SkillType.Engineering, ProgressionFixture.Gear, Assumptions, plan);

            path.CatchUpHandIns.Should().BeGreaterThan(0);
            var manual = requested.Where(r => r.Rank > 0).ToList();
            manual.Should().NotBeEmpty();
            manual.Should().OnlyContain(r => r.Rank >= r.Level + QuickCraftRules.RankMargin, "quick craft needs the rank five above the recipe, and the craft is made at that rank");
            manual.Select(r => r.Rank).Distinct().Should().OnlyContain(rank => rank > 5);
        }

        [Test]
        public void ARealPlayOffLevelUsesThePairsRankAndRepeatsFromTheSeed()
        {
            var options = new ProgressionOptions { Runs = 4, OffLevelRuns = 6, Seed = 3, Skills = new[] { SkillType.Smithery } };
            var gear = CraftingGearCatalog.Build(SharedData.GearItems, SharedData.Recipes);

            var first = ProgressionModel.Run(SharedData.Recipes, gear, ProgressionFixture.Guilds, options);
            var second = ProgressionModel.Run(SharedData.Recipes, gear, ProgressionFixture.Guilds, options);
            var results = first.Results(SkillType.Smithery, CraftingGearCatalog.MidGearName);

            var rankZero = results.At(1, 0);
            rankZero.Rank.Should().Be(0);
            rankZero.RecipeLevel.Should().Be(1);
            rankZero.Runs.Should().Be(6);

            var unlock = results.At(10, 20);
            unlock.Rank.Should().Be(20);
            unlock.RecipeLevel.Should().Be(10);
            unlock.RankOffset.Should().Be(10);

            var again = second.Results(SkillType.Smithery, CraftingGearCatalog.MidGearName).At(10, 20);
            (again.Successes, again.Actions, again.QualitySum).Should().Be((unlock.Successes, unlock.Actions, unlock.QualitySum));

            // The at-level cell is untouched by the on-demand plays.
            results.At(10, 10).Rank.Should().Be(10);
            results.At(10, 10).Runs.Should().Be(4);
        }

        [Test]
        public void ARankZeroCrafterHasFewerActionsAvailableThanOneAtRankOne()
        {
            var options = new ProgressionOptions { Runs = 30, OffLevelRuns = 30, Seed = 11, Skills = new[] { SkillType.Smithery } };
            var gear = CraftingGearCatalog.Build(SharedData.GearItems, SharedData.Recipes);
            var model = ProgressionModel.Run(SharedData.Recipes, gear, ProgressionFixture.Guilds, options);
            var results = model.Results(SkillType.Smithery, CraftingGearCatalog.NoGearName);

            // CP is 0.75 per rank, so the rank-0 crafter has less to spend than the rank-1 one; the two results are not the same cell.
            results.At(1, 0).Should().NotBeSameAs(results.At(1, 1));
            results.At(1, 0).CpSpent.Should().BeLessThanOrEqualTo(results.At(1, 1).CpSpent + results.At(1, 0).Runs * 3);
        }
    }
}
