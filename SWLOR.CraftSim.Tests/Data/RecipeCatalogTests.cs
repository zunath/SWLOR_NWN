using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Tests.Support;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Tests.Data
{
    [TestFixture]
    public class RecipeCatalogTests
    {
        [Test]
        public void LoadsTheGameServersActiveRecipesWithoutErrors()
        {
            SharedData.Recipes.LoadErrors.Should().BeEmpty();
            SharedData.Recipes.Recipes.Should().HaveCountGreaterThan(3000);
            SharedData.Recipes.Recipes.Select(r => r.Type).Should().OnlyHaveUniqueItems();
        }

        [Test]
        public void EveryRecipeIsOfACraftingSkillAtALevelTheChartCovers()
        {
            SharedData.Recipes.Recipes.Select(r => r.Skill).Distinct().Should().BeEquivalentTo(
                new[] { SkillType.Smithery, SkillType.Engineering, SkillType.Fabrication, SkillType.Agriculture, SkillType.Espionage });
            SharedData.Recipes.Recipes.Should().OnlyContain(r => r.Level >= 1 && r.Level <= 80);
        }

        [Test]
        public void ForSkill_ListsRecipesInLevelOrder()
        {
            var levels = SharedData.Recipes.ForSkill(SkillType.Smithery).Select(r => r.Level).ToList();

            levels.Should().BeInAscendingOrder();
            levels.Should().NotBeEmpty();
        }

        [Test]
        public void ComponentStacksAreTheDistinctLinesAndUnitsTheirTotal()
        {
            var recipe = new RecipeEntry(default, SkillType.Smithery, 10, "x", default, new Dictionary<string, int> { ["a"] = 5, ["b"] = 3 });

            (recipe.ComponentStacks, recipe.ComponentUnits).Should().Be((2, 8));
        }

        [Test]
        public void Bands_CoverEveryLevelExactlyOnce()
        {
            for (var level = 1; level <= 60; level++)
                RecipeBand.Default.Count(b => b.Contains(level)).Should().Be(1, $"level {level}");

            RecipeBand.Default.Select(b => b.Name).Should().Equal("1-10", "11-20", "21-30", "31-40", "41-53");
        }
    }
}
