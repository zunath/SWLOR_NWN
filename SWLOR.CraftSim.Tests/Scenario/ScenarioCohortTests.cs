using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Scenario;
using SWLOR.CraftSim.Tests.Support;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Tests.Scenario
{
    [TestFixture]
    public class ScenarioCohortTests
    {
        private static RecipeEntry Recipe(int number, SkillType skill, int level) =>
            new((RecipeType)(95000 + number), skill, level, $"r{number}", RecipeCategoryType.Cloak, new Dictionary<string, int> { ["a"] = 1 });

        [Test]
        public void ForSkillTakesThatSkillsRecipesFromLevelOneInLevelOrder()
        {
            var catalog = RecipeCatalog.From(new[]
            {
                Recipe(1, SkillType.Smithery, 30), Recipe(2, SkillType.Smithery, 0), Recipe(3, SkillType.Engineering, 5),
                Recipe(4, SkillType.Smithery, 5), Recipe(5, SkillType.Smithery, 30)
            });

            var recipes = ScenarioCohort.ForSkill(SkillType.Smithery).Recipes(catalog);

            recipes.Select(r => (r.Level, (int)r.Type)).Should().Equal((5, 95004), (30, 95001), (30, 95005));
        }

        [Test]
        public void ForSkillCanNarrowTheRecipes()
        {
            var catalog = RecipeCatalog.From(new[] { Recipe(1, SkillType.Smithery, 10), Recipe(2, SkillType.Smithery, 20) });

            var recipes = ScenarioCohort.ForSkill(SkillType.Smithery, r => r.Level > 15).Recipes(catalog);

            recipes.Select(r => r.Level).Should().Equal(20);
        }

        [TestCase(1, 1)]
        [TestCase(30, 30)]
        [TestCase(50, 50)]
        [TestCase(53, 50)]
        public void TheDefaultRankIsTheRecipesLevelCappedAtFifty(int level, int rank)
        {
            new ScenarioCohort(_ => true).RankFor(Recipe(1, SkillType.Smithery, level)).Should().Be(rank);
        }

        [Test]
        public void ACohortCanChooseItsOwnRank()
        {
            var cohort = new ScenarioCohort(_ => true, recipe => 25);

            cohort.RankFor(Recipe(1, SkillType.Smithery, 40)).Should().Be(25);
        }

        [Test]
        public void SelectGroupsIntoBandsSamplesEvenlyAndDropsEmptyBands()
        {
            var catalog = RecipeCatalog.From(Enumerable.Range(1, 40).Select(i => Recipe(i, SkillType.Smithery, i <= 20 ? 3 : 45)));

            var bands = ScenarioCohort.ForSkill(SkillType.Smithery).Select(catalog, 4);

            bands.Select(b => b.Band.Name).Should().Equal("1-10", "41-53");
            bands.Select(b => (b.ActiveRecipes, b.Recipes.Count)).Should().Equal((20, 4), (20, 4));
            bands[0].Recipes.Select(r => (int)r.Type - 95000).Should().Equal(3, 8, 13, 18);
        }

        [Test]
        public void SelectWithoutALimitPlaysEveryRecipe()
        {
            var catalog = RecipeCatalog.From(Enumerable.Range(1, 7).Select(i => Recipe(i, SkillType.Smithery, 12)));

            var bands = ScenarioCohort.ForSkill(SkillType.Smithery).Select(catalog, null);

            bands.Single().Recipes.Should().HaveCount(7);
        }

        [Test]
        public void SelectCanGroupByOtherBands()
        {
            var catalog = RecipeCatalog.From(new[] { Recipe(1, SkillType.Smithery, 44), Recipe(2, SkillType.Smithery, 49) });

            var bands = ScenarioCohort.ForSkill(SkillType.Smithery).Select(
                catalog, null, new[] { new RecipeBand("40-45", 40, 45), new RecipeBand("46-50", 46, 50) });

            bands.Select(b => b.Band.Name).Should().Equal("40-45", "46-50");
        }

        [Test]
        public void SampleIsRepeatableAndSpreadsOverTheWholeList()
        {
            var list = Enumerable.Range(0, 100).ToList();

            var first = ScenarioCohort.Sample(list, 5);
            var second = ScenarioCohort.Sample(list, 5);

            first.Should().Equal(second);
            first.Should().Equal(10, 30, 50, 70, 90);
        }

        [Test]
        public void SampleReturnsEverythingWhenTheCountIsLargeEnoughAndRejectsZero()
        {
            var list = Enumerable.Range(0, 3).ToList();

            ScenarioCohort.Sample(list, 3).Should().Equal(list);
            ScenarioCohort.Sample(list, 50).Should().Equal(list);
            ScenarioCohort.Sample(list, null).Should().Equal(list);
            var act = () => ScenarioCohort.Sample(list, 0);
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void LengthTargetsAreThePlansAndContainTheirBounds()
        {
            LengthTarget.ItemCraft.Should().Be(new LengthTarget(10, 20, "item craft at level"));
            LengthTarget.GatheringNode.Contains(3).Should().BeTrue();
            LengthTarget.GatheringNode.Contains(8.1).Should().BeFalse();
            LengthTarget.FishingFight.ToString().Should().Be("3-6 actions (fishing fight)");
            LengthTarget.ResearchExperiment.Contains(5).Should().BeTrue();
            LengthTarget.ItemCraft.Contains(9.9).Should().BeFalse();
            LengthTarget.PerkedItemCraft.Should().Be(new LengthTarget(8, 20, "item craft at level, perked build"));
            LengthTarget.PerkedItemCraft.Contains(9.9).Should().BeTrue();
        }

        [Test]
        public void ACraftStyleNeedsANameAndAPairNamesBoth()
        {
            var act = () => new CraftStyle(" ");
            act.Should().Throw<ArgumentException>();

            new CraftStylePair(new CraftStyle("Hot Forging"), new CraftStyle("Cold Working")).Name.Should().Be("Hot Forging vs Cold Working");
            new CraftStylePair(new CraftStyle("A"), new CraftStyle("B")).MinimumRank.Should().Be(20, "the plan puts a line's style at rank 20");
        }

        [Test]
        public void RegistryFindsScenariosByNameIgnoringCaseAndListsThemInOrder()
        {
            var registry = new ScenarioRegistry(new ICraftScenario[] { new BenchScenario { Name = "zeta" }, new BenchScenario { Name = "Alpha" } });

            registry.Scenarios.Select(s => s.Name).Should().Equal("Alpha", "zeta");
            registry.Find("ALPHA").Name.Should().Be("Alpha");
        }

        [Test]
        public void RegistryRejectsAnUnknownNameAndListsWhatIsAvailable()
        {
            var registry = new ScenarioRegistry(new ICraftScenario[] { new BenchScenario() });

            var unknown = () => registry.Find("forge");
            var empty = () => new ScenarioRegistry(Array.Empty<ICraftScenario>()).Find("forge");

            unknown.Should().Throw<ArgumentException>().WithMessage("*Available: bench*");
            empty.Should().Throw<ArgumentException>().WithMessage("*no minigame has added one*");
        }

        [Test]
        public void RegistryRejectsTwoScenariosWithTheSameName()
        {
            var act = () => new ScenarioRegistry(new ICraftScenario[] { new BenchScenario(), new BenchScenario { Name = "BENCH" } });

            act.Should().Throw<ArgumentException>().WithMessage("*Two scenarios are named*");
        }

        [Test]
        public void DiscoverFindsTheSimulatorsOwnScenariosWithoutFailing()
        {
            var registry = ScenarioRegistry.Discover();

            registry.Scenarios.Should().OnlyContain(s => !string.IsNullOrWhiteSpace(s.Name) && !string.IsNullOrWhiteSpace(s.Title));
            registry.Scenarios.Select(s => s.Name).Should().OnlyHaveUniqueItems();
        }
    }
}
