using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.FarmingService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Fertilizer in a planting: Fertilizer of the crop's tier or higher is used up and adds 1 to the harvest, a lower tier is refused
/// with a clear message, and every refusal leaves the fertilizer, and the seed, where they were.
/// </summary>
public class FertilizerRulesTests
{
    private static readonly DateTime Now = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);

    private static string Root
    {
        get
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                directory = directory.Parent;

            directory.Should().NotBeNull("the repository root should be discoverable from the test directory");
            return directory!.FullName;
        }
    }

    private static string Source(params string[] path) => File.ReadAllText(Path.Combine(new[] { Root, "SWLOR.Game.Server" }.Concat(path).ToArray()));

    private static FarmingRefusal Plant(string fertilizer, bool carried, int cropTier, bool isSapling = false) =>
        FarmingRules.CheckPlant(
            true, true, null, Now, true, 5, cropTier, 0, true,
            FarmingRefusal.None,
            WoodlotRules.GetFertilizerRefusal(isSapling, !string.IsNullOrEmpty(fertilizer)),
            FertilizerRules.GetRefusal(fertilizer, carried, cropTier));

    // The resrefs

    [TestCase(1, "fertilizer_1")]
    [TestCase(3, "fertilizer_3")]
    [TestCase(5, "fertilizer_5")]
    public void TheFiveFertilizersAreNamedByTier(int tier, string resref)
    {
        FertilizerRules.GetResref(tier).Should().Be(resref);
        FertilizerRules.GetTier(resref).Should().Be(tier);
    }

    [TestCase(0)]
    [TestCase(6)]
    [TestCase(-1)]
    public void ThereIsNoFertilizerOfAnotherTier(int tier)
    {
        var act = () => FertilizerRules.GetResref(tier);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("fertilizer_")]
    [TestCase("fertilizer_0")]
    [TestCase("fertilizer_6")]
    [TestCase("fertilizer_12")]
    [TestCase("fertilizer_a")]
    [TestCase("manure_t1")]
    [TestCase("seed_orange")]
    [TestCase("Fertilizer_1")]
    public void AnythingElseIsNotAFertilizer(string resref)
    {
        FertilizerRules.GetTier(resref).Should().Be(0);
    }

    // The tier rule

    [TestCase(1, 1, true)]
    [TestCase(2, 1, true)]
    [TestCase(5, 1, true)]
    [TestCase(3, 3, true)]
    [TestCase(5, 5, true)]
    [TestCase(1, 2, false)]
    [TestCase(2, 3, false)]
    [TestCase(4, 5, false)]
    [TestCase(1, 5, false)]
    public void FertilizerWorksOnACropOfItsTierOrLower(int fertilizerTier, int cropTier, bool works)
    {
        FertilizerRules.Works(fertilizerTier, cropTier).Should().Be(works);
        FertilizerRules.GetRefusal(FertilizerRules.GetResref(fertilizerTier), true, cropTier)
            .Should().Be(works ? FarmingRefusal.None : FarmingRefusal.FertilizerTooWeak);
    }

    [Test]
    public void EveryTierOfFertilizerAgainstEveryTierOfCrop_WorksExactlyWhenItIsAtLeastTheCropsTier()
    {
        for (var fertilizerTier = 1; fertilizerTier <= FertilizerRules.Tiers; fertilizerTier++)
        {
            for (var cropTier = 1; cropTier <= FarmingRules.Tiers; cropTier++)
            {
                Plant(FertilizerRules.GetResref(fertilizerTier), true, cropTier)
                    .Should().Be(fertilizerTier >= cropTier ? FarmingRefusal.None : FarmingRefusal.FertilizerTooWeak,
                        $"Fertilizer {fertilizerTier} on a tier {cropTier} crop");
            }
        }
    }

    // Refusals

    [Test]
    public void NoFertilizerOffered_IsNeverARefusal()
    {
        FertilizerRules.GetRefusal(string.Empty, false, 5).Should().Be(FarmingRefusal.None);
        FertilizerRules.GetRefusal(null, false, 1).Should().Be(FarmingRefusal.None);
        FertilizerRules.GetRefusal("  ", false, 3).Should().Be(FarmingRefusal.None);
        Plant(string.Empty, false, 3).Should().Be(FarmingRefusal.None);
    }

    [Test]
    public void AnItemThatIsNotAFertilizerIsRefused_BeforeThePlayerIsAskedWhetherTheyCarryIt()
    {
        FertilizerRules.GetRefusal("manure_t3", true, 3).Should().Be(FarmingRefusal.NotFertilizer);
        FertilizerRules.GetRefusal("manure_t3", false, 3).Should().Be(FarmingRefusal.NotFertilizer);
        Plant("seed_orange", true, 1).Should().Be(FarmingRefusal.NotFertilizer);
    }

    [Test]
    public void FertilizerThePlayerDoesNotCarryIsRefused_WhateverItsTier()
    {
        FertilizerRules.GetRefusal("fertilizer_5", false, 1).Should().Be(FarmingRefusal.NoFertilizer);
        FertilizerRules.GetRefusal("fertilizer_1", false, 5).Should().Be(FarmingRefusal.NoFertilizer, "carrying none comes before the tier is weighed");
    }

    [Test]
    public void ASaplingRefusesFertilizerBeforeTheTierIsWeighed()
    {
        Plant("fertilizer_5", true, 3, isSapling: true).Should().Be(FarmingRefusal.NoFertilizerOnSaplings);
        Plant("fertilizer_1", true, 5, isSapling: true).Should().Be(FarmingRefusal.NoFertilizerOnSaplings);
        Plant(string.Empty, false, 3, isSapling: true).Should().Be(FarmingRefusal.None);
    }

    [Test]
    public void EveryFertilizerRefusalIsReportedBeforeTheTierCapAndSeedChecks()
    {
        // A weak fertilizer is named even when the planter also lacks the tier, has no room or has no seed.
        FarmingRules.CheckPlant(true, true, null, Now, true, 0, 5, 10, false, FarmingRefusal.None, FarmingRefusal.None, FarmingRefusal.FertilizerTooWeak)
            .Should().Be(FarmingRefusal.FertilizerTooWeak);

        // Permission and the plot come first.
        FarmingRules.CheckPlant(true, true, null, Now, false, 5, 1, 0, true, FarmingRefusal.None, FarmingRefusal.None, FarmingRefusal.FertilizerTooWeak)
            .Should().Be(FarmingRefusal.NoPermission);
        FarmingRules.CheckPlant(true, false, null, Now, true, 5, 1, 0, true, FarmingRefusal.None, FarmingRefusal.None, FarmingRefusal.NoFertilizer)
            .Should().Be(FarmingRefusal.NoPlot);
    }

    [Test]
    public void ACorrectFertilizerDoesNotChangeAnyOtherRefusal()
    {
        FarmingRules.CheckPlant(true, true, null, Now, true, 0, 1, 0, true, FarmingRefusal.None, FarmingRefusal.None, FarmingRefusal.None)
            .Should().Be(FarmingRefusal.TierLocked);
        FarmingRules.CheckPlant(true, true, null, Now, true, 5, 1, FarmingRules.MaxCropsPerCdKey, true)
            .Should().Be(FarmingRefusal.CropCap);
        FarmingRules.CheckPlant(true, true, null, Now, true, 5, 1, 0, false)
            .Should().Be(FarmingRefusal.NoSeed);
    }

    [Test]
    public void TheNewRefusalsCarryTheirOwnNumbers_AndTellThePlayerWhatToDo()
    {
        ((int)FarmingRefusal.NotFertilizer).Should().Be(20);
        ((int)FarmingRefusal.NoFertilizer).Should().Be(21);
        ((int)FarmingRefusal.FertilizerTooWeak).Should().Be(22);

        foreach (var refusal in new[] { FarmingRefusal.NotFertilizer, FarmingRefusal.NoFertilizer, FarmingRefusal.FertilizerTooWeak })
            FarmingRules.Describe(refusal).Should().NotBeNullOrWhiteSpace().And.MatchRegex("^[\\x20-\\x7E]+$");

        FarmingRules.Describe(FarmingRefusal.FertilizerTooWeak).Should().Contain("tier or higher").And.Contain("was not used");
    }

    // The planting message and the list

    [Test]
    public void AFertilizedPlantingSaysSo_AndAPlainOneReadsAsItDid()
    {
        FarmingRules.DescribePlanted("Turnip", 24).Should().Be("You planted Turnip. It will be ripe in " + FarmingRules.FormatDuration(TimeSpan.FromHours(24)) + ".");
        FarmingRules.DescribePlanted("Turnip", 24, fertilized: true).Should().StartWith("You planted Turnip with fertilizer. It will be ripe in ");
    }

    [Test]
    public void TheWindowListsEachFertilizerWithItsCountAndWhatItWorksOn()
    {
        FertilizerRules.DescribeOption(1, 3).Should().Be("Fertilizer I x3 (tier 1 crops)");
        FertilizerRules.DescribeOption(2, 1).Should().Be("Fertilizer II x1 (tier 1 to 2 crops)");
        FertilizerRules.DescribeOption(5, 12).Should().Be("Fertilizer V x12 (tier 1 to 5 crops)");
    }

    [Test]
    public void AFertilizedCropYieldsOneMoreItem()
    {
        FarmingRules.GetYield(false, true, 0, false, false).Should().Be(FarmingRules.GetYield(false, false, 0, false, false) + 1);
    }

    // Plant uses the fertilizer up only after every check

    [Test]
    public void PlantUsesUpTheFertilizerAndMarksThePlotOnlyAfterTheChecks()
    {
        var source = Source("Service", "Farming.cs");
        var plant = source.IndexOf("public static FarmingResult Plant(", StringComparison.Ordinal);
        var check = source.IndexOf("FarmingRules.CheckPlant(", plant, StringComparison.Ordinal);
        var refusal = source.IndexOf("FertilizerRules.GetRefusal(", plant, StringComparison.Ordinal);
        var returned = source.IndexOf("return FarmingResult.Refused(refusal);", plant, StringComparison.Ordinal);
        var useSeed = source.IndexOf("UseUpOne(seed);", plant, StringComparison.Ordinal);
        var useFertilizer = source.IndexOf("UseUpOne(fertilizer);", plant, StringComparison.Ordinal);
        var record = source.IndexOf("Fertilized = fertilized,", plant, StringComparison.Ordinal);
        var end = source.IndexOf("public static FarmingResult Tend(", plant, StringComparison.Ordinal);

        plant.Should().BeGreaterThan(0);
        check.Should().BeGreaterThan(plant);
        refusal.Should().BeGreaterThan(check, "the fertilizer check is an argument of the plant check");
        returned.Should().BeGreaterThan(refusal, "a refusal returns before anything is used up");
        useSeed.Should().BeGreaterThan(returned);
        useFertilizer.Should().BeGreaterThan(returned, "a refused planting costs the planter nothing, the fertilizer included");
        record.Should().BeGreaterThan(useFertilizer).And.BeLessThan(end, "the plot's record says it was fertilized");
        source.Substring(plant, useSeed - plant).Should().NotContain("UseUpOne(fertilizer)");
    }

    [Test]
    public void PlantFertilizesOnlyWhenAFertilizerItemWasFoundAndUsed()
    {
        var source = Source("Service", "Farming.cs");
        var plant = source.IndexOf("public static FarmingResult Plant(", StringComparison.Ordinal);
        var end = source.IndexOf("public static FarmingResult Tend(", plant, StringComparison.Ordinal);
        var body = source.Substring(plant, end - plant);

        body.Should().Contain("var fertilized = GetIsObjectValid(fertilizer);");
        body.Should().Contain("if (fertilized)");
        body.Should().Contain("DescribePlanted(crop.Name, crop.GrowHours, fertilized)");
    }

    // The window

    [Test]
    public void TheGardenPlotWindowOffersTheFertilizerThePlayerCarries_AndPassesTheChoiceToPlant()
    {
        var model = Source("Feature", "GuiDefinition", "ViewModel", "GardenPlotViewModel.cs");
        var window = Source("Feature", "GuiDefinition", "GardenPlotDefinition.cs");

        window.Should().Contain("BindOptions(model => model.FertilizerOptions)");
        window.Should().Contain("BindSelectedIndex(model => model.SelectedFertilizerIndex)");
        model.Should().Contain("Farming.GetFertilizerCarried(Player)");
        model.Should().Contain("WatchOnClient(model => model.SelectedFertilizerIndex);");
        model.Should().Contain("Farming.Plant(Player, _plotId, _seeds[_selectedSeedIndex].SeedResref, GetSelectedFertilizer())");
        model.Should().Contain("FertilizerRules.GetRefusal(fertilizer, true, crop.Tier)", "the Plant button shows the refusal before the player clicks it");
    }
}
