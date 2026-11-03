using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Feature.RecipeDefinition.CookingRecipeDefinition;
using SWLOR.Game.Server.Service.CityStoreService;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// The city-exclusive store items (slice 4e): the Cantina house drinks, the market stall dish and the ten decor structures
/// (banners, crests and the like). The expected table below pins the proposal the owner reviews, so a change to a name, price,
/// store, unlock level or weekly limit shows up as a failing test here, next to the generator that writes the blueprints
/// (tools/CityExclusives/catalog.py) and the catalog the server reads (CityStoreCatalog).
/// </summary>
public class CityExclusiveItemTests
{
    private const int FirstStructure = 5190;
    private const int LastStructure = 5199;

    /// <summary>A food or drink: its resref, name, the dish it copies, the tier it sits in, and where it is sold.</summary>
    private record Food(string Resref, string Name, string Copies, int Tier, CityStoreKind Store, int Level, int Limit);

    /// <summary>A decor structure: its type, name, placeable, price, and where it is sold.</summary>
    private record Decor(StructureType Type, string Name, string Placeable, int Price, CityStoreKind Store, int Level, int Limit);

    private static readonly Food[] Foods =
    {
        new("house_citrus", "House Citrus Cream", "o_aulait", 1, CityStoreKind.Cantina, 1, 12),
        new("house_sunrise", "House Sunrise Cream", "pine_aulait", 3, CityStoreKind.Cantina, 3, 8),
        new("house_reserve", "House Reserve Cream", "tomato_aulait", 5, CityStoreKind.Cantina, 5, 5),
        new("stall_pie", "Stallkeeper's Pot Pie", "cairn_potpie", 2, CityStoreKind.StallGeneralGoods, 1, 10)
    };

    private static readonly Decor[] DecorPieces =
    {
        new(StructureType.CityBannerBlue, "City Banner, Blue", "swlor_0117", 250, CityStoreKind.CityHall, 1, 6),
        new(StructureType.CityBannerGold, "City Banner, Gold", "swlor_0118", 250, CityStoreKind.CityHall, 1, 6),
        new(StructureType.CivicWallCrest, "Civic Wall Crest", "swlor_0880", 400, CityStoreKind.CityHall, 2, 4),
        new(StructureType.ArchiveCrest, "Archive Crest", "swlor_0121", 400, CityStoreKind.CityHall, 2, 4),
        new(StructureType.ClinicDisplayPanel, "Clinic Display Panel", "swlor_0643", 250, CityStoreKind.MedicalCenter, 1, 4),
        new(StructureType.ClinicBactaTank, "Clinic Bacta Tank", "swlor_1417", 400, CityStoreKind.MedicalCenter, 3, 3),
        new(StructureType.SpaceportSignpost, "Spaceport Signpost", "_mdrn_pl_signp14", 250, CityStoreKind.Starport, 1, 4),
        new(StructureType.SpaceportWallSign, "Spaceport Wall Sign", "swlor_1336", 250, CityStoreKind.Starport, 3, 4),
        new(StructureType.PavilionBanner, "Pavilion Banner", "swlor_0124", 250, CityStoreKind.StallDecor, 1, 4),
        new(StructureType.GuildBanner, "Artisans' Guild Banner", "swlor_0120", 250, CityStoreKind.StallCraftingSupplies, 1, 4)
    };

    private static string StructureResref(StructureType type) => $"structure_{(int)type}";

    #region The catalog

    [Test]
    public void EveryExclusiveItem_IsInTheCatalogOfItsStoreAtItsLevelAndLimit()
    {
        foreach (var food in Foods)
            AssertListed(food.Store, food.Resref, food.Level, food.Limit);

        foreach (var piece in DecorPieces)
            AssertListed(piece.Store, StructureResref(piece.Type), piece.Level, piece.Limit);
    }

    private static void AssertListed(CityStoreKind store, string resref, int level, int limit)
    {
        var item = CityStoreCatalog.Get(store).LimitedItems.SingleOrDefault(x => x.Resref == resref);
        item.Should().NotBeNull($"{store} sells {resref}");
        item!.MinLevel.Should().Be(level, $"{resref} unlocks at {store} level {level}");
        item.WeeklyLimit.Should().Be(limit, $"{resref} has {limit} a week");
    }

    [Test]
    public void TheCatalogHoldsNothingBesidesTheseItems_EachInOneStoreOnly()
    {
        var expected = Foods.Select(food => food.Resref).Concat(DecorPieces.Select(piece => StructureResref(piece.Type))).ToList();
        var listed = CityStoreCatalog.All.SelectMany(detail => detail.LimitedItems).Select(item => item.Resref).ToList();

        listed.Should().OnlyHaveUniqueItems("an item belongs to one store, so its allowance is clear");
        listed.Should().BeEquivalentTo(expected);
    }

    [Test]
    public void EveryStore_HasOneToFourExclusiveItems()
    {
        foreach (var detail in CityStoreCatalog.All)
            detail.LimitedItems.Count.Should().BeInRange(1, 4, $"{detail.Kind} sells a few exclusive items");
    }

    [Test]
    public void TheLimitsAndLevels_StayInsideWhatAStoreCanHold()
    {
        foreach (var item in CityStoreCatalog.All.SelectMany(detail => detail.LimitedItems))
        {
            item.WeeklyLimit.Should().BeInRange(1, 12, item.Resref);
            item.MinLevel.Should().BeInRange(1, 5, item.Resref);
            item.Resref.Length.Should().BeLessThanOrEqualTo(16, "NWN archives omit resources with longer resrefs");
        }

        // A stall has no level, so everything a stall sells is stocked from level 1.
        foreach (var detail in CityStoreCatalog.All.Where(detail => CityStoreRules.IsStallKind(detail.Kind)))
            detail.LimitedItems.Should().OnlyContain(item => item.MinLevel == 1, $"{detail.Kind} is level 1");
    }

    [Test]
    public void NoExclusiveItem_IsInAStoreBlueprint_SoTheAllowanceIsTheOnlyWayToBuyIt()
    {
        var inBlueprints = Directory.EnumerateFiles(Path.Combine(Root, "Module", "utm"), "*.utm.json")
            .SelectMany(path => ((JArray)JObject.Parse(File.ReadAllText(path))["StoreList"]!["value"]!)
                .SelectMany(panel => panel["ItemList"]?["value"] as JArray ?? new JArray())
                .Select(entry => entry["InventoryRes"]!["value"]!.Value<string>()))
            .ToHashSet();

        foreach (var item in CityStoreCatalog.All.SelectMany(detail => detail.LimitedItems))
            inBlueprints.Should().NotContain(item.Resref, $"{item.Resref} is stocked from its weekly allowance, not infinitely");
    }

    #endregion

    #region The drinks and the dish

    [Test]
    public void EveryFood_CopiesTheStatsOfAnExistingDishOfItsTier()
    {
        var recipes = new CookingRecipes().BuildRecipes().Values.ToDictionary(recipe => recipe.Resref);

        foreach (var food in Foods)
        {
            var item = ReadItem(food.Resref);
            var source = ReadItem(food.Copies);

            recipes.Should().ContainKey(food.Copies, $"{food.Copies} is a cooking recipe's dish");
            var level = recipes[food.Copies].Level;
            ((level + 9) / 10).Should().Be(food.Tier, $"{food.Copies} is a tier {food.Tier} dish (recipe level {level})");

            item["BaseItem"]!["value"]!.Value<int>().Should().Be(source["BaseItem"]!["value"]!.Value<int>());
            item["ModelPart1"]!["value"]!.Value<int>().Should().Be(source["ModelPart1"]!["value"]!.Value<int>());
            item["xModelPart1"]!["value"]!.Value<int>().Should().Be(source["xModelPart1"]!["value"]!.Value<int>());
            item["Charges"]!["value"]!.Value<int>().Should().Be(source["Charges"]!["value"]!.Value<int>());
            item["Tag"]!["value"]!.Value<string>().Should().Be("FOOD", "the food use script runs on this tag");
            item["PropertiesList"]!.ToString().Should().Be(source["PropertiesList"]!.ToString(), "no number is new: the food bonus is the source dish's");
            item["TemplateResRef"]!["value"]!.Value<string>().Should().Be(food.Resref);
            Name(item).Should().Be(food.Name).And.NotBe(Name(source));
        }
    }

    [Test]
    public void TheCantinaDrinks_UnlockAtTheCantinaLevelOfTheirTier()
    {
        foreach (var food in Foods.Where(food => food.Store == CityStoreKind.Cantina))
            food.Level.Should().Be(food.Tier, $"{food.Resref}: a better Cantina serves a better drink");
    }

    [Test]
    public void EveryFood_IsPricedAtTwoHundredFiftyATier_AndIsAPlayerItem()
    {
        foreach (var food in Foods)
        {
            var item = ReadItem(food.Resref);
            item["Cost"]!["value"]!.Value<int>().Should().Be(250 * food.Tier, food.Resref);
            item["AddCost"]!["value"]!.Value<int>().Should().Be(250 * food.Tier, "the base item adds nothing, so the extra cost is the price");
            HasNoEconomyFlag(item).Should().BeFalse($"{food.Resref} is sold by a city store, so it is a player item");
        }
    }

    #endregion

    #region The decor structures

    [Test]
    public void TheDecorStructures_TakeTheIdsOfTheirSubRange()
    {
        DecorPieces.Select(piece => (int)piece.Type).Should().Equal(Enumerable.Range(FirstStructure, DecorPieces.Length));
        DecorPieces.Should().HaveCountLessThanOrEqualTo(LastStructure - FirstStructure + 1);

        var inRange = Enum.GetValues<StructureType>().Where(type => (int)type >= FirstStructure && (int)type <= LastStructure).ToList();
        inRange.Should().BeEquivalentTo(DecorPieces.Select(piece => piece.Type), "this slice owns 5190-5199");
    }

    [Test]
    public void EveryDecorStructure_IsDescribedAndPlacesAnExistingPlaceableNoOtherStructureUses()
    {
        var allStructures = Enum.GetValues<StructureType>()
            .Where(type => type != StructureType.Invalid)
            .ToDictionary(type => type, type => type.GetAttribute<StructureType, StructureAttribute>());

        foreach (var piece in DecorPieces)
        {
            var attribute = allStructures[piece.Type];
            attribute.Name.Should().Be(piece.Name);
            attribute.Resref.Should().Be(piece.Placeable);
            attribute.IsActive.Should().BeTrue();
            attribute.Description.Should().MatchRegex("^[\\x20-\\x7E]+$", "player-facing text is plain ASCII").And.Contain("Sold only at");
            attribute.RestrictedPropertyTypes.Should().HaveFlag(PropertyType.House, "banners and crests decorate houses");
            attribute.CanBeRetrieved.Should().BeTrue();

            File.Exists(Path.Combine(Root, "Module", "utp", $"{piece.Placeable}.utp.json"))
                .Should().BeTrue($"the placeable '{piece.Placeable}' must exist in the module, so no new art is needed");

            allStructures.Where(pair => pair.Key != piece.Type && pair.Value.Resref == piece.Placeable)
                .Select(pair => pair.Key).Should().BeEmpty($"{piece.Name}'s placeable is used by no other structure");
        }

        DecorPieces.Select(piece => piece.Placeable).Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void EveryDecorItem_IsTheStructureItemOfItsIdAtItsPrice_AndAPlayerItem()
    {
        var template = ReadItem("structure_5022");

        foreach (var piece in DecorPieces)
        {
            var resref = StructureResref(piece.Type);
            var item = ReadItem(resref);

            item["Tag"]!["value"]!.Value<string>().Should().Be(resref);
            item["TemplateResRef"]!["value"]!.Value<string>().Should().Be(resref);
            Name(item).Should().Be(piece.Name);
            item["Description"]!["value"]!["0"]!.Value<string>().Should().Be(piece.Type.GetAttribute<StructureType, StructureAttribute>().Description);
            item["BaseItem"]!["value"]!.Value<int>().Should().Be(template["BaseItem"]!["value"]!.Value<int>());
            item["ModelPart1"]!["value"]!.Value<int>().Should().Be(template["ModelPart1"]!["value"]!.Value<int>());
            item["PropertiesList"]!.ToString().Should().Be(template["PropertiesList"]!.ToString(), "every structure item carries the same placement property");
            item["Cost"]!["value"]!.Value<int>().Should().Be(piece.Price, resref);
            item["AddCost"]!["value"]!.Value<int>().Should().Be(piece.Price, resref);
            HasNoEconomyFlag(item).Should().BeFalse($"{resref} is sold by a city store, so it is a player item");
        }
    }

    [Test]
    public void TheDecorPrices_AreTwoFiftyForSmallPiecesAndFourHundredForTheLargerOnes()
    {
        DecorPieces.Select(piece => piece.Price).Distinct().Should().BeEquivalentTo(new[] { 250, 400 });
        DecorPieces.Where(piece => piece.Type is StructureType.CivicWallCrest or StructureType.ArchiveCrest or StructureType.ClinicBactaTank)
            .Should().OnlyContain(piece => piece.Price == 400);
    }

    [Test]
    public void EveryDecorStructure_SitsInTheItemPalette_AndTheDrinksAndPieSitWithTheFood()
    {
        var palette = File.ReadAllText(Path.Combine(Root, "Module", "itp", "itempalcus.itp.json"));

        foreach (var piece in DecorPieces)
            palette.Should().Contain($"\"{StructureResref(piece.Type)}\"", $"the toolset palette must list {piece.Name}");

        foreach (var food in Foods)
            palette.Should().Contain($"\"{food.Resref}\"", $"the toolset palette must list {food.Name}");
    }

    #endregion

    #region Text

    [Test]
    public void EveryNameAndDescription_IsPlainAscii()
    {
        foreach (var resref in Foods.Select(food => food.Resref).Concat(DecorPieces.Select(piece => StructureResref(piece.Type))))
        {
            var item = ReadItem(resref);
            Name(item).Should().MatchRegex("^[\\x20-\\x7E]+$");
            item["Description"]!["value"]!["0"]!.Value<string>().Should().MatchRegex("^[\\x20-\\x7E]+$");
            item["DescIdentified"]!["value"]!["0"]!.Value<string>().Should().Be(item["Description"]!["value"]!["0"]!.Value<string>());
        }
    }

    #endregion

    private static string Name(JObject item) => item["LocalizedName"]!["value"]!["0"]!.Value<string>()!;

    private static bool HasNoEconomyFlag(JObject item)
    {
        return item["VarTable"]?["value"] is JArray variables &&
               variables.Any(variable => variable["Name"]!["value"]!.Value<string>() == "NO_ECONOMY");
    }

    private static JObject ReadItem(string resref)
    {
        var path = Path.Combine(Root, "Module", "uti", $"{resref}.uti.json");
        File.Exists(path).Should().BeTrue($"the item '{resref}' exists in the module");
        return JObject.Parse(File.ReadAllText(path));
    }

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
}
