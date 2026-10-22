using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Legacy.Crafting;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Tests.Legacy.Crafting
{
    [TestFixture]
    public class CraftBandSummaryTests
    {
        private static RecipeEntry Recipe(int level, int stacks) =>
            new(default, SkillType.Smithery, level, "item", default, Enumerable.Range(0, stacks).ToDictionary(i => $"c{i}", _ => 1));

        private static CraftCell Cell(int level, int successes, double qualityPerSuccess, int full = 0, long actions = 0, int runs = 100, int offset = 0) => new()
        {
            Skill = SkillType.Smithery,
            RecipeLevel = level,
            Rank = level,
            RankOffset = offset,
            Gear = "no gear",
            Policy = "Dominant rotation",
            Runs = runs,
            Successes = successes,
            QualitySum = successes * qualityPerSuccess,
            FullQualitySuccesses = full,
            Actions = actions
        };

        [Test]
        public void ABandWeightsEachLevelByItsNumberOfRecipes()
        {
            var recipes = RecipeCatalog.From(new[] { Recipe(5, 2), Recipe(5, 2), Recipe(5, 2), Recipe(6, 2) });
            var cells = new[] { Cell(5, 100, 0.2, full: 0, actions: 1000), Cell(6, 50, 0.8, full: 50, actions: 2000) };

            var row = CraftBandSummary.Summarize(cells, recipes).Single(r => r.Band == "1-10");

            row.Recipes.Should().Be(4);
            row.SuccessRate.Should().BeApproximately((3 * 1.0 + 1 * 0.5) / 4, 1e-9);
            row.QualityFraction.Should().BeApproximately((3 * 1.0 * 0.2 + 0.5 * 0.8) / 3.5, 1e-9);
            row.FullQualityShare.Should().BeApproximately((0.5 * 1.0) / 3.5, 1e-9);
            row.ActionsPerCraft.Should().BeApproximately((3 * 10.0 + 20.0) / 4, 1e-9);
        }

        [Test]
        public void MaterialUseIsStacksConsumedPerItemMadeWhenTheCraftableRecipesAreAttemptedEqually()
        {
            var recipes = RecipeCatalog.From(new[] { Recipe(5, 2), Recipe(6, 4) });
            var cells = new[] { Cell(5, 100, 0.5), Cell(6, 50, 0.5) };

            var row = CraftBandSummary.Summarize(cells, recipes).Single();

            var used = 2 * 1.0 + 4 * (0.5 + 0.65 * 0.5);
            row.StacksPerSuccess.Should().BeApproximately(used / 1.5, 1e-9);
            row.NeverCraftedShare.Should().Be(0);
        }

        [Test]
        public void ARecipeThatNeverSucceedsIsLeftOutOfMaterialUseAndCounted()
        {
            var recipes = RecipeCatalog.From(new[] { Recipe(5, 3), Recipe(6, 3), Recipe(7, 3) });
            var cells = new[] { Cell(5, 100, 0.5), Cell(6, 0, 0), Cell(7, 100, 0.5) };

            var row = CraftBandSummary.Summarize(cells, recipes).Single();

            row.StacksPerSuccess.Should().BeApproximately(3.0, 1e-9);
            row.NeverCraftedShare.Should().BeApproximately(1 / 3.0, 1e-9);
            row.SuccessRate.Should().BeApproximately(2 / 3.0, 1e-9);
        }

        [Test]
        public void ABandWithNoSuccessHasNoMaterialFigureAndNoQuality()
        {
            var recipes = RecipeCatalog.From(new[] { Recipe(45, 3) });
            var cells = new[] { Cell(45, 0, 0) };

            var row = CraftBandSummary.Summarize(cells, recipes).Single();

            (row.SuccessRate, row.QualityFraction, row.FullQualityShare).Should().Be((0, 0, 0));
            row.StacksPerSuccess.Should().BeNull();
            row.NeverCraftedShare.Should().Be(1);
        }

        [Test]
        public void RecipesAreGroupedIntoTheBandsOfTheirLevels()
        {
            var recipes = RecipeCatalog.From(new[] { Recipe(10, 1), Recipe(11, 1), Recipe(40, 1), Recipe(41, 1), Recipe(53, 1) });
            var cells = new[] { 10, 11, 40, 41, 53 }.Select(level => Cell(level, 100, 0.1)).ToList();

            var rows = CraftBandSummary.Summarize(cells, recipes);

            rows.Select(r => (r.Band, r.Recipes)).Should().Equal(("1-10", 1), ("11-20", 1), ("31-40", 1), ("41-53", 2));
        }

        [Test]
        public void RankOffsetsGetTheirOwnRows()
        {
            var recipes = RecipeCatalog.From(new[] { Recipe(10, 1) });
            var cells = new[] { Cell(10, 100, 0.1), Cell(10, 40, 0.1, offset: -3) };

            var rows = CraftBandSummary.Summarize(cells, recipes);

            rows.Select(r => (r.RankOffset, r.SuccessRate)).Should().BeEquivalentTo(new[] { (0, 1.0), (-3, 0.4) });
        }

        [Test]
        public void PoolingWeightsEachSkillsRowByItsRecipes()
        {
            var smithery = new CraftBandRow
            {
                Skill = SkillType.Smithery, Band = "1-10", Gear = "no gear", Policy = "P", Recipes = 3, SuccessRate = 1.0, QualityFraction = 0.2,
                FullQualityShare = 0, ActionsPerCraft = 10, StacksUsed = 6, ItemsMade = 3, NeverCraftedShare = 0
            };
            var engineering = new CraftBandRow
            {
                Skill = SkillType.Engineering, Band = "1-10", Gear = "no gear", Policy = "P", Recipes = 1, SuccessRate = 0.5, QualityFraction = 0.8,
                FullQualityShare = 1.0, ActionsPerCraft = 20, StacksUsed = 2, ItemsMade = 0.5, NeverCraftedShare = 0.5
            };

            var pooled = CraftBandSummary.Pool(new[] { smithery, engineering });

            pooled.Recipes.Should().Be(4);
            pooled.SuccessRate.Should().BeApproximately(0.875, 1e-9);
            pooled.QualityFraction.Should().BeApproximately(1.0 / 3.5, 1e-9);
            pooled.FullQualityShare.Should().BeApproximately(0.5 / 3.5, 1e-9);
            pooled.ActionsPerCraft.Should().BeApproximately(12.5, 1e-9);
            pooled.StacksPerSuccess.Should().BeApproximately(8 / 3.5, 1e-9);
            pooled.NeverCraftedShare.Should().BeApproximately(0.125, 1e-9);
            (pooled.Band, pooled.Gear, pooled.Policy).Should().Be(("1-10", "no gear", "P"));
        }

        [Test]
        public void ALevelWithoutACell_IsSkipped()
        {
            var recipes = RecipeCatalog.From(new[] { Recipe(10, 1), Recipe(12, 1) });

            var row = CraftBandSummary.Summarize(new[] { Cell(10, 100, 0.1) }, recipes).Single();

            row.Recipes.Should().Be(1);
        }
    }
}
