using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Progression;
using SWLOR.CraftSim.Tests.Support;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Research;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;

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
            // Craft.IsResearchableRecipe: weapons, armor and food, and the space revamp's ship module and deed recipes
            // (ShipBlueprintRules.IsResearchable). Structures, the retired starships and modules, and droids are not listed.
            foreach (var recipe in SharedData.Recipes.Recipes.Where(r => r.Detail != null))
            {
                var expected = recipe.EnhancementType is RecipeEnhancementType.Weapon or RecipeEnhancementType.Armor or RecipeEnhancementType.Food ||
                               ShipBlueprintRules.IsResearchable(recipe.Detail);
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

        [Test]
        public void RecipesThatPayTheSameResearchXpChooseTheShorterJob()
        {
            RecipeEntry Weapon(string name, int level, float modifier) => new(
                default, SkillType.Fabrication, level, name, default, new Dictionary<string, int> { { "wood", 1 } },
                detail: new RecipeDetail
                {
                    Skill = SkillType.Fabrication,
                    Level = level,
                    Resref = name,
                    EnhancementType = RecipeEnhancementType.Weapon,
                    ResearchCostModifier = modifier,
                    Components = new Dictionary<string, int> { { "wood", 1 } }
                });

            // All three are level 39 (tier 4) and pay the same chart XP at Fabrication 40, but two have a cost modifier that lengthens their jobs.
            // The dear one is listed first, so a tie-break that ignored the job length would take it.
            var recipes = RecipeCatalog.From(
                Enumerable.Range(1, 53).Select(level => new RecipeEntry(
                        default, SkillType.Fabrication, level, $"item_{level}", default, new Dictionary<string, int> { { "wood", 1 } }))
                    .Concat(new[] { Weapon("dear_job", 39, 0.2f), Weapon("cheap_job", 39, 0f), Weapon("dear_job_2", 39, 0.2f) }));
            var model = ProgressionFixture.Model(recipes, ComponentSupply.Build(recipes), null, 0.95, 0.3, 15, new[] { SkillType.Fabrication });

            var row = ResearchCohort.Rows(model, ProgressionFixture.Gear, new ProgressionAssumptions()).Single(r => r.FabricationRank == 40);

            row.BaseXp.Should().Be(ResearchXpRules.BaseXp(39, 40));
            row.BaseXp.Should().BeGreaterThan(0);
            row.FirstJobSeconds.Should().Be(ResearchCohort.JobSeconds(39, 1, 0f), "the researcher picks the recipe whose job is shorter");
            ResearchCohort.JobSeconds(39, 1, 0.2f).Should().BeGreaterThan(ResearchCohort.JobSeconds(39, 1, 0f));
        }
    }
}