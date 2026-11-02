using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Feature.FarmingDefinition;
using SWLOR.Game.Server.Feature.LootTableDefinition;
using SWLOR.Game.Server.Feature.RecipeDefinition.CookingRecipeDefinition;
using SWLOR.Game.Server.Feature.RecipeDefinition.FabricationRecipeDefinition;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.FarmingService;
using SWLOR.Game.Server.Service.FishingService;
using SWLOR.Game.Server.Service.LivestockService;
using SWLOR.Game.Server.Service.LootService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>The items, recipes and drops of the farm: 26 seeds, manure, Fertilizer I-V, young animals, the crate and the city structures.</summary>
public class FarmItemContentTests
{
    private static readonly string[] Roman = { "", "I", "II", "III", "IV", "V" };

    /// <summary>The small stackable base items crops and seeds use: miscsmall_stackable and miscsmall2_stackable.</summary>
    private static readonly int[] SmallStackableBaseItems = { 536, 539 };

    private const int MiscSmall2Stackable = 539;
    private const int MiscMedium = 29;

    private static readonly Dictionary<CropType, CropDefinition> Crops = new CropListDefinition().BuildCrops();
    private static readonly Dictionary<LivestockType, LivestockDefinition> Animals = new LivestockListDefinition().BuildLivestock();
    private static readonly Dictionary<RecipeType, RecipeDetail> FertilizerRecipeList = new FertilizerRecipes().BuildRecipes();
    private static readonly Dictionary<RecipeType, RecipeDetail> StructureRecipeList = new CityStructureRecipes().BuildRecipes();
    private static readonly Dictionary<string, LootTable> HarvestTables = new HarvestingLootTableDefinition().BuildLootTables();

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

    private static string Palette => File.ReadAllText(Path.Combine(Root, "Module", "itp", "itempalcus.itp.json"));

    private static JsonDocument ReadItem(string resref)
    {
        var path = Path.Combine(Root, "Module", "uti", resref + ".uti.json");
        File.Exists(path).Should().BeTrue($"the item '{resref}' exists in the module");
        return JsonDocument.Parse(File.ReadAllText(path));
    }

    private static JsonElement Field(JsonDocument item, string field) => item.RootElement.GetProperty(field).GetProperty("value");

    private static int Number(JsonDocument item, string field) => Field(item, field).GetInt32();

    private static string Text(JsonDocument item, string field) => Field(item, field).GetString();

    private static string Name(JsonDocument item) => Field(item, "LocalizedName").GetProperty("0").GetString();

    private static string Description(JsonDocument item) => Field(item, "Description").GetProperty("0").GetString();

    private static bool HasNoEconomyFlag(JsonDocument item) =>
        item.RootElement.TryGetProperty("VarTable", out var table) &&
        table.GetProperty("value").EnumerateArray().Any(entry => entry.GetProperty("Name").GetProperty("value").GetString() == "NO_ECONOMY");

    private static void ShouldBeAWellFormedItem(string resref, JsonDocument item)
    {
        Text(item, "Tag").Should().Be(resref);
        Text(item, "TemplateResRef").Should().Be(resref);
        Number(item, "ModelPart1").Should().Be(Number(item, "xModelPart1"), $"'{resref}' keeps both model part fields equal");
        Number(item, "Cost").Should().Be(Number(item, "AddCost"), $"'{resref}' has one base cost");
        Name(item).Should().MatchRegex("^[\\x20-\\x7E]+$", $"the name of '{resref}' is plain ASCII");
        Description(item).Should().MatchRegex("^[\\x20-\\x7E]+$", $"the description of '{resref}' is plain ASCII");
        Palette.Should().Contain($"\"{resref}\"", $"the toolset palette lists '{resref}'");
    }

    private static int HarvestItemCost(CropDefinition crop)
    {
        using var item = ReadItem(crop.HarvestResref);
        return Number(item, "Cost");
    }

    // Seeds

    [Test]
    public void EverySeedIsASmallStackableItemNamedForItsCropAndPricedLikeIt()
    {
        foreach (var crop in Crops.Values)
        {
            using var seed = ReadItem(crop.SeedResref);
            ShouldBeAWellFormedItem(crop.SeedResref, seed);

            SmallStackableBaseItems.Should().Contain(Number(seed, "BaseItem"), crop.SeedResref);
            Name(seed).Should().EndWith(" Seeds", crop.SeedResref);
            Description(seed).Should().Contain($"Crop Management {Roman[crop.Tier]}.", crop.SeedResref);
            Number(seed, "Cost").Should().Be(HarvestItemCost(crop), $"{crop.SeedResref} costs what the crop it grows costs");
            HasNoEconomyFlag(seed).Should().BeFalse(crop.SeedResref);
        }
    }

    [Test]
    public void EverySeedHasItsOwnNameAndItsOwnInventoryIcon()
    {
        var seeds = Crops.Values.Select(crop => ReadItem(crop.SeedResref)).ToList();
        try
        {
            seeds.Select(Name).Should().OnlyHaveUniqueItems();
            seeds.Select(seed => (Number(seed, "BaseItem"), Number(seed, "ModelPart1"))).Should().OnlyHaveUniqueItems();
        }
        finally
        {
            seeds.ForEach(seed => seed.Dispose());
        }
    }

    [Test]
    public void SeedsOfTierThreeToFiveDropRarelyFromTheTableOfTheirCrop()
    {
        foreach (var crop in Crops.Values.Where(crop => crop.Tier >= 3))
        {
            var tables = HarvestTables.Where(pair => pair.Value.Any(item => item.Resref == crop.SeedResref)).ToList();

            tables.Should().ContainSingle($"the seed of {crop.Name} drops from one table");
            var (id, table) = (tables[0].Key, tables[0].Value);
            new[] { $"HERBS_{crop.Tier}", $"VEGETABLES_PATCH_{crop.Tier}", $"SCAVENGING_BUSH_{crop.Tier}" }
                .Should().Contain(id, $"{crop.Name} is a plant of tier {crop.Tier}");
            table.Select(item => item.Resref).Should().Contain(crop.HarvestResref, $"{crop.Name} is harvested from {id}");

            var entry = table.Single(item => item.Resref == crop.SeedResref);
            entry.Weight.Should().Be(1, $"{crop.SeedResref} is as rare as the emerald");
            entry.MaxQuantity.Should().Be(1);
            entry.IsRare.Should().BeFalse();
            ((double)entry.Weight / table.Sum(item => item.Weight)).Should().BeInRange(0.01, 0.03, $"{crop.SeedResref} drops in about 1 roll in 50 to 75");
        }
    }

    [Test]
    public void SeedsOfTierOneAndTwoAreNotInTheHarvestingTables_BecauseStoresSellThem()
    {
        var seedResrefs = Crops.Values.Where(crop => crop.Tier <= 2).Select(crop => crop.SeedResref).ToList();

        seedResrefs.Should().HaveCount(10);
        HarvestTables.Values.SelectMany(table => table).Select(item => item.Resref).Should().NotContain(seedResrefs);
    }

    [Test]
    public void TheHarvestingTablesKeepTheirOtherEntriesAsTheyWere()
    {
        foreach (var crop in Crops.Values)
        {
            var tables = new[] { $"HERBS_{crop.Tier}", $"VEGETABLES_PATCH_{crop.Tier}", $"SCAVENGING_BUSH_{crop.Tier}" }
                .SelectMany(id => HarvestTables[id]).Select(item => item.Resref);

            tables.Should().Contain(crop.HarvestResref, crop.Name);
        }

        HarvestTables["HERBS_3"].Where(item => !item.Resref.StartsWith("seed_")).Select(item => (item.Resref, item.Weight))
            .Should().Equal(("herb_c", 50));
        HarvestTables["VEGETABLES_PATCH_5"].Where(item => !item.Resref.StartsWith("seed_")).Select(item => item.Resref)
            .Should().Equal("herb_x", "turnip", "tofu", "dried_bonito", "tomato");
    }

    // Manure and Fertilizer

    [Test]
    public void ManureIsFiveSmallStackableItemsThatCostAsMuchAsTheCropOfTheirTier()
    {
        var costs = new List<int>();
        for (var tier = 1; tier <= 5; tier++)
        {
            var resref = LivestockRules.GetManureResref(tier);
            using var manure = ReadItem(resref);
            ShouldBeAWellFormedItem(resref, manure);

            Number(manure, "BaseItem").Should().Be(MiscSmall2Stackable);
            Name(manure).Should().Be($"Manure {Roman[tier]}");
            Description(manure).Should().Contain($"Fertilizer {Roman[tier]}").And.Contain($"tier {tier}");
            HasNoEconomyFlag(manure).Should().BeFalse();

            var tierCrops = Crops.Values.Where(crop => crop.Tier == tier && !crop.HarvestResref.StartsWith("fiberp_")).ToList();
            using var cropItem = ReadItem(tierCrops.First().HarvestResref);
            Number(manure, "Cost").Should().Be(Number(cropItem, "Cost"), $"{resref} costs a tier {tier} crop");
            costs.Add(Number(manure, "Cost"));
        }

        costs.Should().BeInAscendingOrder().And.OnlyHaveUniqueItems();
    }

    [Test]
    public void FertilizerIsFiveItemsAtTheCostOfManureOfTheSameTier()
    {
        for (var tier = 1; tier <= 5; tier++)
        {
            var resref = $"fertilizer_{tier}";
            using var fertilizer = ReadItem(resref);
            using var manure = ReadItem(LivestockRules.GetManureResref(tier));
            ShouldBeAWellFormedItem(resref, fertilizer);

            Number(fertilizer, "BaseItem").Should().Be(MiscSmall2Stackable);
            Name(fertilizer).Should().Be($"Fertilizer {Roman[tier]}");
            Number(fertilizer, "Cost").Should().Be(Number(manure, "Cost"));
            HasNoEconomyFlag(fertilizer).Should().BeFalse();
        }
    }

    [Test]
    public void ManureAndFertilizerEachHaveTheirOwnInventoryIcon()
    {
        var items = Enumerable.Range(1, 5).Select(tier => ReadItem(LivestockRules.GetManureResref(tier)))
            .Concat(Enumerable.Range(1, 5).Select(tier => ReadItem($"fertilizer_{tier}"))).ToList();
        try
        {
            items.Select(item => (Number(item, "BaseItem"), Number(item, "ModelPart1"))).Should().OnlyHaveUniqueItems();
        }
        finally
        {
            items.ForEach(item => item.Dispose());
        }
    }

    [Test]
    public void FertilizerHasAFishRecipeAndAManureRecipeAtEachOfTheFiveCropLevels()
    {
        FertilizerRecipeList.Should().HaveCount(10);

        for (var tier = 1; tier <= 5; tier++)
        {
            var fish = FertilizerRecipeList[Enum.Parse<RecipeType>($"FertilizerFish{tier}")];
            var manure = FertilizerRecipeList[Enum.Parse<RecipeType>($"FertilizerManure{tier}")];

            foreach (var recipe in new[] { fish, manure })
            {
                recipe.Skill.Should().Be(SkillType.Agriculture);
                recipe.Category.Should().Be(RecipeCategoryType.Fertilizer);
                recipe.Level.Should().Be(FarmingRules.GetTierLevel(tier));
                recipe.Quantity.Should().Be(1);
                recipe.Resref.Should().Be($"fertilizer_{tier}");
                recipe.IsActive.Should().BeTrue();
                recipe.Requirements.Should().BeEmpty("fertilizer is known from its level, like pet food");
                recipe.Components.Should().HaveCount(2);
                recipe.Components["distilled_water"].Should().Be(1);
                recipe.MinigameParameters.Should().NotBeNull("the Kitchen parameters are generated");
                recipe.Name.Should().StartWith($"Fertilizer {Roman[tier]} (");
            }

            fish.Name.Should().EndWith("(Fish)");
            manure.Name.Should().EndWith("(Manure)");
            manure.Components[LivestockRules.GetManureResref(tier)].Should().Be(1);

            var fishResref = fish.Components.Keys.Single(resref => resref != "distilled_water");
            fish.Components[fishResref].Should().Be(1);
            Enum.GetValues<FishType>()
                .Select(type => type.GetAttribute<FishType, FishAttribute>())
                .Single(attribute => attribute.Resref == fishResref)
                .Level.Should().Be(FarmingRules.GetTierLevel(tier), $"{fishResref} is a fish of the tier's level");
        }
    }

    [Test]
    public void EveryFertilizerComponentAndProductIsAnItemInTheModule()
    {
        foreach (var recipe in FertilizerRecipeList.Values)
        {
            using var product = ReadItem(recipe.Resref);
            foreach (var component in recipe.Components.Keys)
                File.Exists(Path.Combine(Root, "Module", "uti", component + ".uti.json")).Should().BeTrue(component);
        }
    }

    // Young animals and the crate

    [Test]
    public void EveryYoungAnimalIsAnItemNamedForItsSpeciesAndWorthTwiceItsMeat()
    {
        foreach (var animal in Animals.Values)
        {
            using var young = ReadItem(animal.YoungResref);
            using var meat = ReadItem(animal.MeatResref);
            ShouldBeAWellFormedItem(animal.YoungResref, young);

            Number(young, "BaseItem").Should().Be(MiscSmall2Stackable);
            Name(young).Should().Be($"Young {animal.Name}", "the corral menu lists items by name");
            Description(young).Should().Contain($"tier {animal.Tier}").And.Contain($"Crop Management {Roman[animal.Tier]}.");
            Number(young, "Cost").Should().Be(2 * Number(meat, "Cost"), $"{animal.YoungResref} is worth half the butchered yield of 4 meat");
            HasNoEconomyFlag(young).Should().BeFalse(animal.YoungResref);
        }
    }

    [Test]
    public void EveryYoungAnimalHasItsOwnInventoryIcon()
    {
        var items = Animals.Values.Select(animal => ReadItem(animal.YoungResref)).ToList();
        try
        {
            items.Select(item => (Number(item, "BaseItem"), Number(item, "ModelPart1"))).Should().OnlyHaveUniqueItems();
        }
        finally
        {
            items.ForEach(item => item.Dispose());
        }
    }

    [Test]
    public void TheCrateIsOneItemThatDoesNotStack_BecauseItsAnimalLivesInLocals()
    {
        using var crate = ReadItem(LivestockRules.CrateResref);
        ShouldBeAWellFormedItem(LivestockRules.CrateResref, crate);

        Number(crate, "BaseItem").Should().Be(MiscMedium, "a crate with different animals in it must not merge");
        Name(crate).Should().Be("Animal Crate");
        crate.RootElement.TryGetProperty("VarTable", out _).Should().BeFalse("the animal's state is written when it is crated");
        HasNoEconomyFlag(crate).Should().BeFalse("crated animals are sold and traded");
    }

    // City structures

    [Test]
    public void TheGardenPlotHasItsStructureItemOnThePaletteLikeTheOthers()
    {
        using var plot = ReadItem("structure_5100");
        using var reference = ReadItem("structure_5024");
        ShouldBeAWellFormedItem("structure_5100", plot);

        var attribute = StructureType.GardenPlot.GetAttribute<StructureType, StructureAttribute>();
        Name(plot).Should().Be(attribute.Name);
        Number(plot, "BaseItem").Should().Be(Number(reference, "BaseItem"));
        Number(plot, "ModelPart1").Should().Be(Number(reference, "ModelPart1"));
        Number(plot, "Cost").Should().Be(Number(reference, "Cost"));
        plot.RootElement.GetProperty("PropertiesList").GetRawText().Should().Be(reference.RootElement.GetProperty("PropertiesList").GetRawText(),
            "every structure item carries the same placement property");
        HasNoEconomyFlag(plot).Should().BeFalse();
    }

    [Test]
    public void TheDuelingRingAndTheCorralAreNoLongerFlaggedBecauseRecipesMakeThem()
    {
        foreach (var resref in new[] { "structure_5101", "structure_5104" })
        {
            using var item = ReadItem(resref);
            HasNoEconomyFlag(item).Should().BeFalse(resref);
        }
    }

    [Test]
    public void EachCityStructureHasOneFabricationRecipeBuiltFromStructureParts()
    {
        StructureRecipeList.Should().HaveCount(3);
        StructureRecipeList.Values.Select(recipe => recipe.Resref).Should().BeEquivalentTo("structure_5100", "structure_5101", "structure_5104");

        foreach (var recipe in StructureRecipeList.Values)
        {
            recipe.Skill.Should().Be(SkillType.Fabrication);
            recipe.Category.Should().Be(RecipeCategoryType.Structure);
            recipe.Quantity.Should().Be(1);
            recipe.IsActive.Should().BeTrue();
            recipe.MinigameParameters.Should().NotBeNull("the Fabricator parameters are generated");
            recipe.Components.Keys.Should().OnlyContain(component => component == "const_parts" || component == "r_const_parts" || component == "r_pow_supp_unit");
            foreach (var component in recipe.Components.Keys)
                File.Exists(Path.Combine(Root, "Module", "uti", component + ".uti.json")).Should().BeTrue(component);
        }

        var plot = StructureRecipeList[RecipeType.GardenPlot];
        plot.Level.Should().Be(26);
        plot.Components.Should().Equal(new Dictionary<string, int> { ["const_parts"] = 1 });

        var ring = StructureRecipeList[RecipeType.DuelingRing];
        ring.Level.Should().Be(35);
        ring.Components.Should().Equal(new Dictionary<string, int> { ["r_const_parts"] = 1, ["r_pow_supp_unit"] = 1 });

        var corral = StructureRecipeList[RecipeType.Corral];
        corral.Level.Should().Be(30);
        corral.Components.Should().Equal(new Dictionary<string, int> { ["const_parts"] = 2 });
    }

    [Test]
    public void EachStructureRecipeMakesTheItemOfItsStructureType()
    {
        StructureRecipeList[RecipeType.GardenPlot].Resref.Should().Be($"structure_{(int)StructureType.GardenPlot}");
        StructureRecipeList[RecipeType.DuelingRing].Resref.Should().Be($"structure_{(int)StructureType.DuelingRing}");
        StructureRecipeList[RecipeType.Corral].Resref.Should().Be($"structure_{(int)StructureType.Corral}");
    }

    [Test]
    public void TheNewRecipesTakeTheirIdsFromTheReservedRange()
    {
        var ids = FertilizerRecipeList.Keys.Concat(StructureRecipeList.Keys).Select(type => (int)type).ToList();

        ids.Should().HaveCount(13).And.OnlyHaveUniqueItems();
        ids.Should().OnlyContain(id => id >= 8000 && id <= 8039);
        ((int)RecipeCategoryType.Fertilizer).Should().Be(74);
    }
}
