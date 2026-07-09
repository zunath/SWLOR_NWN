using System;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.CropDefinition;
using SWLOR.Game.Server.Feature.RecipeDefinition.CookingRecipeDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.FarmingService;

namespace SWLOR.Game.Server.Tests.Service;

public class FarmingTests
{
    [Test]
    [TestCase(0, 2)]
    [TestCase(9, 2)]
    [TestCase(10, 3)]
    [TestCase(25, 4)]
    [TestCase(50, 7)]
    public void GetMaxConcurrentCrops_ScalesWithAgricultureRank(int rank, int expected)
    {
        Farming.GetMaxConcurrentCrops(rank).Should().Be(expected);
    }

    [Test]
    public void CalculateStageDurationSeconds_NoBonus_ReturnsBaseDuration()
    {
        Farming.CalculateStageDurationSeconds(14400, 0).Should().Be(14400);
    }

    [Test]
    public void CalculateStageDurationSeconds_PositiveBonus_ShortensDuration()
    {
        Farming.CalculateStageDurationSeconds(14400, 100).Should().Be(7200);
        Farming.CalculateStageDurationSeconds(14400, 15).Should().Be(14400 * 100 / 115);
    }

    [Test]
    public void CalculateStageDurationSeconds_NegativeBonus_ClampsToBaseDuration()
    {
        Farming.CalculateStageDurationSeconds(14400, -50).Should().Be(14400);
    }

    [Test]
    public void GetCurrentStage_ProgressesThroughStagesAndCapsAtHarvest()
    {
        var planted = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        const int stageDuration = 100;

        Farming.GetCurrentStage(planted, stageDuration, planted).Should().Be(0);
        Farming.GetCurrentStage(planted, stageDuration, planted.AddSeconds(99)).Should().Be(0);
        Farming.GetCurrentStage(planted, stageDuration, planted.AddSeconds(100)).Should().Be(1);
        Farming.GetCurrentStage(planted, stageDuration, planted.AddSeconds(250)).Should().Be(2);
        Farming.GetCurrentStage(planted, stageDuration, planted.AddSeconds(300)).Should().Be(Farming.NumberOfStages);
        Farming.GetCurrentStage(planted, stageDuration, planted.AddSeconds(99999)).Should().Be(Farming.NumberOfStages);
    }

    [Test]
    public void GetCurrentStage_ClockBeforePlantingOrInvalidDuration_IsSafe()
    {
        var planted = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        Farming.GetCurrentStage(planted, 100, planted.AddSeconds(-500)).Should().Be(0);
        Farming.GetCurrentStage(planted, 0, planted).Should().Be(Farming.NumberOfStages);
    }

    [Test]
    public void CalculateYieldQuantity_AppliesTendAndCraftsmanshipBonuses()
    {
        Farming.CalculateYieldQuantity(6, 0, 0).Should().Be(6);
        Farming.CalculateYieldQuantity(6, 45, 0).Should().Be(6 * 145 / 100);
        Farming.CalculateYieldQuantity(6, 45, 40).Should().Be(6 * 155 / 100);
    }

    [Test]
    public void CalculateYieldQuantity_AlwaysYieldsAtLeastOne()
    {
        Farming.CalculateYieldQuantity(1, 0, 0).Should().Be(1);
        Farming.CalculateYieldQuantity(1, -50, -100).Should().Be(1);
    }

    [Test]
    public void CalculatePristineChancePercent_CombinesAllSources()
    {
        Farming.CalculatePristineChancePercent(0, 0, 0).Should().Be(5);
        Farming.CalculatePristineChancePercent(80, 0, 0).Should().Be(15);
        Farming.CalculatePristineChancePercent(80, 15, 9).Should().Be(39);
        Farming.CalculatePristineChancePercent(-10, -5, -5).Should().Be(5);
    }

    [Test]
    public void CalculateAcceleratedPlantDate_RemovesPercentOfRemainingTime()
    {
        var planted = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        const int stageDuration = 1000;

        // 3000 seconds total, 1000 elapsed, 2000 remaining. 15% of remaining = 300.
        var now = planted.AddSeconds(1000);
        var adjusted = Farming.CalculateAcceleratedPlantDate(planted, stageDuration, now, 15);

        adjusted.Should().Be(planted.AddSeconds(-300));
        (now - adjusted).TotalSeconds.Should().Be(1300);
    }

    [Test]
    public void CalculateAcceleratedPlantDate_CompletedCrop_IsUnchanged()
    {
        var planted = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var now = planted.AddSeconds(5000);

        Farming.CalculateAcceleratedPlantDate(planted, 1000, now, 15).Should().Be(planted);
    }

    [Test]
    public void CropBuilder_BuildsCropWithAllValues()
    {
        var crops = new CropBuilder()
            .Create(CropType.CitrusVine)
            .Name("Citrus Vine")
            .Description("A vine.")
            .RequiredRank(6)
            .SeedResref("seed_citrus")
            .Yield("v_orange", 3)
            .Yield("v_lemon", 3)
            .SecondsPerStage(14400)
            .Build();

        var crop = crops[CropType.CitrusVine];
        crop.Name.Should().Be("Citrus Vine");
        crop.RequiredRank.Should().Be(6);
        crop.Level.Should().Be(9);
        crop.SeedResref.Should().Be("seed_citrus");
        crop.Yields.Should().HaveCount(2);
        crop.Yields["v_orange"].Should().Be(3);
        crop.SecondsPerStage.Should().Be(14400);
        crop.IsActive.Should().BeTrue();
    }

    [Test]
    public void CropDefinitions_CoverRanksZeroToFiftyWithNoGapLargerThanTwo()
    {
        var crops = new CropDefinitions().BuildCrops();
        var ranks = crops.Values
            .Where(x => x.IsActive)
            .Select(x => x.RequiredRank)
            .OrderBy(x => x)
            .ToList();

        ranks.First().Should().Be(0);
        ranks.Last().Should().Be(50);

        for (var i = 1; i < ranks.Count; i++)
        {
            (ranks[i] - ranks[i - 1]).Should().BeLessThanOrEqualTo(2,
                $"a new crop must unlock at least every 2 Agriculture ranks (gap between {ranks[i - 1]} and {ranks[i]})");
        }
    }

    [Test]
    public void CropDefinitions_AllCropsAreValid()
    {
        var crops = new CropDefinitions().BuildCrops();

        crops.Should().HaveCount(26);

        foreach (var (type, crop) in crops)
        {
            crop.Name.Should().NotBeNullOrWhiteSpace($"crop {type} must have a name");
            crop.Description.Should().NotBeNullOrWhiteSpace($"crop {type} must have a description");
            crop.SeedResref.Should().NotBeNullOrWhiteSpace($"crop {type} must have a seed resref");
            crop.SeedResref.Length.Should().BeLessThanOrEqualTo(16, $"crop {type} seed resref must fit NWN's resref limit");
            crop.Yields.Should().NotBeEmpty($"crop {type} must yield produce");
            crop.Yields.Values.Should().OnlyContain(x => x > 0, $"crop {type} yields must be positive");
            crop.SecondsPerStage.Should().BePositive($"crop {type} must have a growth duration");
            crop.RequiredRank.Should().BeInRange(0, 50, $"crop {type} must be plantable within the skill cap");
        }

        crops.Values.Select(x => x.SeedResref).Should().OnlyHaveUniqueItems("each crop needs a distinct seed item");
    }

    [Test]
    public void CropDefinitions_ExclusiveCropsHavePristineVariants()
    {
        var crops = new CropDefinitions().BuildCrops();
        var pristineCrops = crops.Values
            .Where(x => !string.IsNullOrWhiteSpace(x.PristineResref))
            .ToList();

        pristineCrops.Should().HaveCount(7, "each farming-exclusive crop has a pristine variant");

        foreach (var crop in pristineCrops)
        {
            crop.PristineResref.Length.Should().BeLessThanOrEqualTo(16,
                $"crop {crop.Name} pristine resref must fit NWN's resref limit");
        }

        pristineCrops.Select(x => x.PristineResref).Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void FarmToTableRecipes_AllRequireFarmingExclusiveProduce()
    {
        var crops = new CropDefinitions().BuildCrops();
        var exclusiveResrefs = crops.Values
            .Where(x => !string.IsNullOrWhiteSpace(x.PristineResref))
            .SelectMany(x => x.Yields.Keys.Append(x.PristineResref))
            .ToHashSet();

        var recipes = new FarmToTableRecipes().BuildRecipes();

        recipes.Should().HaveCount(15);

        foreach (var (type, recipe) in recipes)
        {
            recipe.Components.Keys.Should().Contain(
                x => exclusiveResrefs.Contains(x),
                $"recipe {type} must require at least one farming-exclusive ingredient");
        }
    }
}
