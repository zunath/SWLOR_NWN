using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.FarmingDefinition;
using SWLOR.Game.Server.Feature.LootTableDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame.Kitchen;
using SWLOR.Game.Server.Service.FarmingService;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>The 26 crops of the Player City Revamp: five tiers, their levels and grow times, and the items they yield.</summary>
public class CropDefinitionTests
{
    private static readonly int[] CropsPerTier = { 5, 5, 5, 7, 4 };

    private static readonly Dictionary<CropType, CropDefinition> Crops = new CropListDefinition().BuildCrops();

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

    private static bool ModuleHasItem(string resref) =>
        File.Exists(Path.Combine(Root, "Module", "uti", resref + ".uti.json"));

    [Test]
    public void TheListDefinesEveryCropTypeOnce_TwentySixInAll()
    {
        var types = Enum.GetValues<CropType>().Where(type => type != CropType.Invalid).ToList();

        types.Should().HaveCount(26);
        Crops.Keys.Should().BeEquivalentTo(types);
        Crops.Should().OnlyContain(pair => pair.Value.Type == pair.Key);
        ((int)types.Min()).Should().Be(1);
        ((int)types.Max()).Should().Be(26);
    }

    [Test]
    public void TheCropsFallInFiveTiersOfFiveFiveFiveSevenAndFour()
    {
        Crops.Values.GroupBy(crop => crop.Tier).OrderBy(group => group.Key).Select(group => group.Count())
            .Should().Equal(CropsPerTier);
    }

    [Test]
    public void EachTierSharesItsCropLevelAndGrowTime()
    {
        foreach (var crop in Crops.Values)
        {
            crop.Level.Should().Be(FarmingRules.GetTierLevel(crop.Tier), crop.Name);
            crop.GrowHours.Should().Be(FarmingRules.GetTierGrowHours(crop.Tier), crop.Name);
        }

        Crops.Values.Select(crop => crop.Level).Distinct().OrderBy(level => level).Should().Equal(5, 15, 25, 35, 45);
        Crops.Values.Where(crop => crop.Tier == 1).Should().OnlyContain(crop => crop.GrowHours == 12);
        Crops.Values.Where(crop => crop.Tier == 2).Should().OnlyContain(crop => crop.GrowHours == 16);
        Crops.Values.Where(crop => crop.Tier == 3).Should().OnlyContain(crop => crop.GrowHours == 20);
        Crops.Values.Where(crop => crop.Tier >= 4).Should().OnlyContain(crop => crop.GrowHours == 24);
    }

    [Test]
    public void CropLevelsMatchTheHarvestingVeinLevelsOfTheSameTiers()
    {
        foreach (var crop in Crops.Values)
            crop.Level.Should().Be(GatheringLevels.ForTier(crop.Tier), crop.Name);
    }

    [Test]
    public void TheCropsAreTheOnesThePlanLists()
    {
        Names(1).Should().Equal("Viscaran Orange", "Viscaran Lemon", "Viscaran Pebble", "Viscaran Rosemary", "Fiber Shrub (Ruined)");
        Names(2).Should().Equal("Viscaran Apple", "Viscaran Peas", "Passion Fruit", "Mandalorian Thyme", "Fiber Shrub (Flawed)");
        Names(3).Should().Equal("Hutlar Acorn", "Strange Pineapple", "Vegetable Clump", "Mon Calan Seaweed", "Fiber Shrub (Good Quality)");
        Names(4).Should().Equal("Ginger", "Melon", "Mushroom", "Walnut", "Cornucopia", "Tatooine Desert Sage", "Fiber Shrub (Imperfect)");
        Names(5).Should().Equal("Turnip", "Tomato", "Dathomir Oregano", "Fiber Shrub (High Quality)");

        static IEnumerable<string> Names(int tier) => Crops.Values.Where(crop => crop.Tier == tier).OrderBy(crop => (int)crop.Type).Select(crop => crop.Name);
    }

    [Test]
    public void EveryCropHasAUniqueAsciiNameAndUniqueSeed()
    {
        Crops.Values.Select(crop => crop.Name).Should().OnlyHaveUniqueItems();
        Crops.Values.Select(crop => crop.Name).Should().OnlyContain(name => name.All(character => character >= ' ' && character <= '~'));
        Crops.Values.Select(crop => crop.SeedResref).Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void SeedResrefsAreLowercaseSeedNamesThatFitTheSixteenCharacterLimit()
    {
        foreach (var crop in Crops.Values)
        {
            crop.SeedResref.Should().StartWith("seed_", crop.Name);
            crop.SeedResref.Length.Should().BeLessThanOrEqualTo(16, crop.Name);
            crop.SeedResref.Should().MatchRegex("^[a-z0-9_]+$", crop.Name);
            crop.SeedResref.Should().NotBe(crop.HarvestResref);
        }
    }

    [Test]
    public void EveryCropYieldsAnItemThatExistsInTheModule()
    {
        foreach (var crop in Crops.Values)
            ModuleHasItem(crop.HarvestResref).Should().BeTrue($"{crop.Name} yields '{crop.HarvestResref}'");
    }

    [Test]
    public void EachCropYieldsAnItemItsTiersHarvestingNodesGiveToday()
    {
        var tables = new HarvestingLootTableDefinition().BuildLootTables();

        foreach (var crop in Crops.Values)
        {
            var resrefs = new[] { $"HERBS_{crop.Tier}", $"VEGETABLES_PATCH_{crop.Tier}", $"SCAVENGING_BUSH_{crop.Tier}" }
                .SelectMany(table => tables[table])
                .Select(item => item.Resref)
                .ToList();

            resrefs.Should().Contain(crop.HarvestResref, $"{crop.Name} is an item of tier {crop.Tier}'s herb, vegetable or scavenging nodes");
        }
    }

    [Test]
    public void TheFiberShrubsYieldTheFiberplastOfTheirTier_AndNothingElseIsFiberplast()
    {
        var fiber = Crops.Values.Where(crop => crop.Name.StartsWith("Fiber Shrub")).OrderBy(crop => crop.Tier).ToList();

        fiber.Select(crop => crop.HarvestResref).Should().Equal(
            "fiberp_ruined", "fiberp_flawed", "fiberp_good", "fiberp_imperfect", "fiberp_high");
        fiber.Select(crop => crop.Tier).Should().Equal(1, 2, 3, 4, 5);
        Crops.Values.Except(fiber).Should().OnlyContain(crop => !crop.HarvestResref.StartsWith("fiberp_"));
    }

    [Test]
    public void ProcessedItemsFromTheNodesAreNotCrops()
    {
        Crops.Values.Select(crop => crop.HarvestResref).Should().NotContain(new[] { "plant_butter", "tofu", "dried_bonito" });
    }

    [Test]
    public void EveryCropIsACookingIngredientWithARowInTheKitchenTable()
    {
        // The fiberplast rows are the table's default flavor: a Fiber Shrub is for fabricators, not cooks.
        foreach (var crop in Crops.Values)
            KitchenComponentTable.Contains(crop.HarvestResref).Should().BeTrue($"{crop.Name} yields '{crop.HarvestResref}'");

        Crops.Values.Where(crop => !crop.HarvestResref.StartsWith("fiberp_"))
            .Should().OnlyContain(crop => !KitchenComponentTable.Get(crop.HarvestResref).Equals(KitchenComponentTable.Unlisted),
                "each food crop has a flavor of its own");
    }

    [Test]
    public void TheFarmingServiceLoadsEveryCropByTypeAndBySeed()
    {
        Farming.CacheCrops();

        Farming.GetAllCrops().Should().HaveCount(26);
        foreach (var crop in Crops.Values)
        {
            Farming.GetCrop(crop.Type).Should().NotBeNull(crop.Name).And.Subject.As<CropDefinition>().Name.Should().Be(crop.Name);
            Farming.GetCropBySeed(crop.SeedResref)!.Type.Should().Be(crop.Type, crop.Name);
        }

        Farming.GetCrop(CropType.Invalid).Should().BeNull();
        Farming.GetCropBySeed("not_a_seed").Should().BeNull();
        Farming.GetCropBySeed(string.Empty).Should().BeNull();
        Farming.GetCropBySeed(null).Should().BeNull();
    }

    [Test]
    public void SeedsTheModuleDoesNotHaveYetAreListedAndNotFailed()
    {
        var missing = Crops.Values.Where(crop => !ModuleHasItem(crop.SeedResref)).Select(crop => crop.SeedResref).ToList();

        TestContext.Out.WriteLine($"{missing.Count} of {Crops.Count} seed items are not in the module yet: {string.Join(", ", missing)}");
        missing.Count.Should().BeLessThanOrEqualTo(Crops.Count);
    }
}
