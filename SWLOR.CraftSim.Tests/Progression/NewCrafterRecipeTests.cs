using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Progression;
using SWLOR.CraftSim.Tests.Support;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Tests.Progression
{
    /// <summary>The baseline is a new crafter's, so it only uses recipes that need no learning, and research follows the server's own rule.</summary>
    [TestFixture]
    public class NewCrafterRecipeTests
    {
        private static readonly SkillType[] Skills = { SkillType.Smithery, SkillType.Engineering, SkillType.Fabrication, SkillType.Agriculture };

        [Test]
        public void SomeRecipesNeedLearningAndAreLeftOutOfTheNewCrafterBaseline()
        {
            foreach (var skill in Skills)
            {
                var all = SharedData.Recipes.ForSkill(skill).ToList();
                var open = SharedData.Recipes.ForNewCrafter(skill).ToList();

                all.Count(r => r.RequiresLearning).Should().BeGreaterThan(0, $"{skill} has recipes marked RequirementUnlocked");
                open.Should().HaveCount(all.Count(r => !r.RequiresLearning));
                open.Should().OnlyContain(r => !r.RequiresLearning);
                open.Should().OnlyContain(r => r.Detail.Requirements.OfType<RecipeUnlockRequirement>().Count() == 0);
            }
        }

        [Test]
        public void EveryLevelAnAtLevelCrafterNeedsHasARecipeTheyCanMake()
        {
            foreach (var skill in Skills)
            {
                var levels = SharedData.Recipes.ForNewCrafter(skill).Select(r => r.Level).ToHashSet();
                Enumerable.Range(1, 49).Where(level => !levels.Contains(level)).Should().BeEmpty($"{skill} needs a recipe at every level 1-49");
            }
        }

        [Test]
        public void NoGuildTaskAsksForARecipeThatNeedsLearning()
        {
            ProgressionFixture.Guilds.Tasks.Where(t => t.Recipe != null).Should().OnlyContain(t => !t.Recipe.RequiresLearning);
        }

        [Test]
        public void ResearchIsOfferedForTheRecipesTheServerListsAndNoOthers()
        {
            // Craft.IsResearchableRecipe: weapons, armor and food. Structures, starships, modules and droids are not listed.
            foreach (var recipe in SharedData.Recipes.Recipes.Where(r => r.Detail != null))
            {
                var expected = recipe.EnhancementType is RecipeEnhancementType.Weapon or RecipeEnhancementType.Armor or RecipeEnhancementType.Food;
                ServerResearchRules.IsResearchable(recipe).Should().Be(expected, recipe.Type.ToString());
            }

            SharedData.Recipes.Recipes.Any(r => r.Detail != null && r.EnhancementType == RecipeEnhancementType.Structure)
                .Should().BeTrue("structure recipes exist and are not researchable");
        }

        [Test]
        public void TheResearchCohortOnlyPicksRecipesTheResearcherCanLearnAndTheServerLists()
        {
            var rows = ResearchCohort.Rows(ProgressionFixture.RealRecipes, ProgressionFixture.Gear, new ProgressionAssumptions());

            rows.Should().NotBeEmpty();
            var offered = SharedData.Recipes.Recipes
                .Where(r => !r.RequiresLearning && ServerResearchRules.IsResearchable(r))
                .Select(r => r.Level)
                .ToHashSet();
            rows.Select(r => r.RecipeLevel).Should().OnlyContain(level => offered.Contains(level));
        }
    }
}
