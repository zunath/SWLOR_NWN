using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Feature.FarmingDefinition;
using SWLOR.Game.Server.Feature.RecipeDefinition.CookingRecipeDefinition;
using SWLOR.Game.Server.Feature.RecipeDefinition.FabricationRecipeDefinition;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.RecipeParameters;
using SWLOR.Game.Server.Service.PlanetIdentityService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// The planet and station identity content of the Player City Revamp: each of the seven planets with areas open to cities and the player
/// stations has a signature dish, a signature drink and a four piece decor set, all ordinary recipes with one extra place requirement.
/// Nothing in them is a new number: a dish takes the stats of an existing dish of its tier, and a decor piece the level, slots and bill of
/// an existing furniture recipe.
/// </summary>
public class PlanetIdentityContentTests
{
    private static readonly PlanetType[] CityPlanets =
    {
        PlanetType.Dantooine, PlanetType.Dathomir, PlanetType.Hutlar, PlanetType.Korriban,
        PlanetType.MonCala, PlanetType.Tatooine, PlanetType.Viscara
    };

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

    private static Dictionary<RecipeType, RecipeDetail> Dishes => new PlanetSignatureRecipes().BuildRecipes();

    private static Dictionary<RecipeType, RecipeDetail> Decor => new PlanetDecorRecipes().BuildRecipes();

    private static IEnumerable<KeyValuePair<RecipeType, RecipeDetail>> Identity => Dishes.Concat(Decor);

    private static IdentityPlace PlaceOf(RecipeDetail recipe) =>
        recipe.Requirements.OfType<RecipeIdentityRequirement>().Single().Place;

    private static JsonDocument ReadUti(string resref) =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "Module", "uti", resref + ".uti.json")));

    private static string Text(JsonDocument document, string field) => document.RootElement.GetProperty(field).GetProperty("value").GetString()!;

    private static string LocalText(JsonDocument document, string field) =>
        document.RootElement.GetProperty(field).GetProperty("value").GetProperty("0").GetString()!;

    private static int Number(JsonElement element, string field) => element.GetProperty(field).GetProperty("value").GetInt32();

    private static string Palette => File.ReadAllText(Path.Combine(Root, "Module", "itp", "itempalcus.itp.json"));

    /// <summary>A food item's bonuses: the food bonus properties as (subtype, amount) pairs in sorted order.</summary>
    private static List<(int Subtype, int Amount)> FoodBonuses(string resref)
    {
        using var item = ReadUti(resref);
        return item.RootElement.GetProperty("PropertiesList").GetProperty("value").EnumerateArray()
            .Where(property => Number(property, "PropertyName") == 106)
            .Select(property => (Number(property, "Subtype"), Number(property, "CostValue")))
            .OrderBy(pair => pair.Item1).ThenBy(pair => pair.Item2).ToList();
    }

    private static IEnumerable<RecipeDetail> OrdinaryFood()
    {
        var identity = Dishes.Keys.ToHashSet();
        return typeof(IRecipeListDefinition).Assembly.GetTypes()
            .Where(type => typeof(IRecipeListDefinition).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
            .SelectMany(type => ((IRecipeListDefinition)Activator.CreateInstance(type)!).BuildRecipes())
            .Where(pair => pair.Value.IsActive && pair.Value.Category == RecipeCategoryType.Food && !identity.Contains(pair.Key))
            .Select(pair => pair.Value);
    }

    private static IEnumerable<RecipeDetail> OrdinaryFurniture() => new FurnitureRecipes().BuildRecipes().Values;

    // The sets

    [Test]
    public void EachCityPlanetAndTheStationsHaveADishADrinkAndFourDecorPieces()
    {
        var places = CityPlanets.Select(IdentityPlace.ForPlanet).Append(IdentityPlace.ForStation()).ToList();

        places.Should().HaveCount(8);
        foreach (var place in places)
        {
            var food = Dishes.Values.Where(recipe => PlaceOf(recipe) == place).ToList();
            var decor = Decor.Values.Where(recipe => PlaceOf(recipe) == place).ToList();

            food.Should().HaveCount(2, $"{place} has one signature dish and one signature drink");
            decor.Should().HaveCount(4, $"{place} has a four piece decor set");
        }

        Identity.Select(pair => PlaceOf(pair.Value)).Distinct().Should().BeEquivalentTo(places,
            "no other planet has identity content: the plan lists these seven planets, and the stations share one set");
    }

    [Test]
    public void EachPlaceHasOneDrinkAndOneDishByTheKitchensDishKinds()
    {
        foreach (var group in Dishes.GroupBy(pair => PlaceOf(pair.Value)))
        {
            var kinds = group.Select(pair => KitchenParameterProfile.Classify(
                RecipeParameterContext.From(pair.Key, pair.Value))).ToList();

            kinds.Count(kind => kind == KitchenDish.Sweet).Should().Be(1, $"{group.Key} has one drink");
            kinds.Count(kind => kind != KitchenDish.Sweet).Should().Be(1, $"{group.Key} has one dish");
        }
    }

    [Test]
    public void TheIdsTakeTheirSlicesOfTheReservedRanges()
    {
        Dishes.Keys.Select(type => (int)type).Should().OnlyContain(id => id >= 8100 && id <= 8119, "dish and drink pairs start at 8100");
        Decor.Keys.Select(type => (int)type).Should().OnlyContain(id => id >= 8120 && id <= 8199, "decor starts at 8120");

        var decorStructures = Enum.GetValues<StructureType>().Where(type => (int)type is >= 5130 and <= 5189).ToList();
        decorStructures.Should().HaveCount(32);
        Decor.Keys.Select(type => (int)type).Order().Should().Equal(decorStructures.Select(type => (int)type - 5130 + 8120).Order(),
            "a decor piece's recipe is its structure's id - 5130 + 8120");
        Decor.Keys.Select(type => type.ToString()).Order().Should().Equal(decorStructures.Select(type => type.ToString()).Order(),
            "a decor recipe and its structure share a name");
    }

    [Test]
    public void EveryRecipeIsOrdinaryApartFromItsPlace()
    {
        foreach (var (type, recipe) in Identity)
        {
            recipe.IsActive.Should().BeTrue(type.ToString());
            recipe.Quantity.Should().Be(1, type.ToString());
            recipe.Requirements.Should().ContainSingle(requirement => requirement is RecipeIdentityRequirement, type.ToString());
            recipe.Requirements.Should().HaveCount(1, $"{type} needs no unlock or perk: any crafter in the right place may make it");
            recipe.Components.Should().NotBeEmpty(type.ToString());
        }
    }

    // Dishes and drinks

    [Test]
    public void EveryDishAndDrinkIsAnAgricultureFoodRecipeCookedInTheKitchen()
    {
        foreach (var (type, recipe) in Dishes)
        {
            recipe.Skill.Should().Be(SkillType.Agriculture, type.ToString());
            recipe.Category.Should().Be(RecipeCategoryType.Food, type.ToString());
            recipe.EnhancementType.Should().Be(RecipeEnhancementType.Food, type.ToString());
            recipe.MinigameParameters.Should().BeOfType<KitchenParameters>(type.ToString());
            recipe.Resref.Should().MatchRegex("^sig_[a-z]{4}_(dish|drink)$", type.ToString());
        }
    }

    [Test]
    public void EveryDishAndDrinkTakesTheStatsLevelAndSlotsOfAnExistingDishOfTheSameTier()
    {
        var existing = OrdinaryFood().ToList();

        foreach (var (type, recipe) in Dishes)
        {
            var bonuses = FoodBonuses(recipe.Resref);
            bonuses.Should().NotBeEmpty($"{type} gives a food bonus");

            existing.Should().Contain(source =>
                    source.Level == recipe.Level &&
                    source.EnhancementSlots == recipe.EnhancementSlots &&
                    FoodBonuses(source.Resref).SequenceEqual(bonuses),
                $"{type} copies the stats, level and slots of an existing dish, so it adds no new number");
        }
    }

    [Test]
    public void EveryDishAndDrinkIsFarmFood_CropsMeatAndWater()
    {
        var crops = new CropListDefinition().BuildCrops().Values.Where(crop => !crop.IsSapling).Select(crop => crop.HarvestResref).ToHashSet();
        var livestock = new LivestockListDefinition().BuildLivestock().Values.ToList();
        var meat = livestock.Select(animal => animal.MeatResref).ToHashSet();
        var products = meat.Concat(livestock.Select(animal => animal.CollectExtraResref)).Where(resref => resref.Length > 0).ToHashSet();

        foreach (var (type, recipe) in Dishes)
        {
            recipe.Components.Keys.Should().OnlyContain(resref => resref == "distilled_water" || crops.Contains(resref) || products.Contains(resref),
                $"{type} uses only farm crops, livestock meat and milk, and water");
            recipe.Components.Keys.Should().Contain(resref => crops.Contains(resref), $"{type} uses a farm crop");
        }

        var kinds = Dishes.GroupBy(pair => KitchenParameterProfile.Classify(RecipeParameterContext.From(pair.Key, pair.Value)) == KitchenDish.Sweet);
        foreach (var dish in kinds.Where(group => !group.Key).SelectMany(group => group))
            dish.Value.Components.Keys.Should().Contain(resref => meat.Contains(resref), $"{dish.Key} is a dish made with livestock meat");
    }

    [Test]
    public void EveryDishAndDrinkItemIsATradeableFoodItemNamedAfterItsRecipe()
    {
        foreach (var (type, recipe) in Dishes)
        {
            using var item = ReadUti(recipe.Resref);

            Text(item, "Tag").Should().Be("FOOD", $"{type}: the tag the food use script reads");
            Text(item, "TemplateResRef").Should().Be(recipe.Resref);
            item.RootElement.TryGetProperty("VarTable", out _).Should().BeFalse($"{type} can be bought, sold and traded: players craft it");
            Number(item.RootElement, "Plot").Should().Be(0, type.ToString());
            LocalText(item, "LocalizedName").Should().MatchRegex("^[\\x20-\\x7E]+$").And.NotBeEmpty();
            LocalText(item, "Description").Should().MatchRegex("^[\\x20-\\x7E]+$").And.Be(LocalText(item, "DescIdentified"));
            LocalText(item, "Description").Should().Contain("can only be cooked", $"{type}'s description says where it is cooked");
            Palette.Should().Contain($"\"value\": \"{recipe.Resref}\"").And.Contain($"\"value\": \"{LocalText(item, "LocalizedName")}\"");
        }
    }

    [Test]
    public void EveryBillIsMadeOfItemsTheModuleHas()
    {
        foreach (var (type, recipe) in Identity)
            foreach (var resref in recipe.Components.Keys)
                File.Exists(Path.Combine(Root, "Module", "uti", resref + ".uti.json")).Should().BeTrue($"{type}: {resref}");
    }

    [Test]
    public void EveryFoodNameIsDifferentFromEveryExistingFoodItem()
    {
        var names = Dishes.Values.Select(recipe => { using var item = ReadUti(recipe.Resref); return LocalText(item, "LocalizedName"); }).ToList();
        var existing = OrdinaryFood().Select(recipe => { using var item = ReadUti(recipe.Resref); return LocalText(item, "LocalizedName"); }).ToHashSet();

        names.Should().OnlyHaveUniqueItems();
        names.Where(existing.Contains).Should().BeEmpty("a signature item is not mistaken for an ordinary dish");
    }

    // Decor

    [Test]
    public void EveryDecorStructureIsActiveFurnitureThatPlacesAnExistingPlaceableNoOtherStructureUses()
    {
        var all = Enum.GetValues<StructureType>().Select(type => (Type: type, Detail: type.GetAttribute<StructureType, StructureAttribute>())).ToList();
        var decor = all.Where(entry => (int)entry.Type is >= 5130 and <= 5189).ToList();
        var others = all.Where(entry => !decor.Contains(entry)).Select(entry => entry.Detail?.Resref).ToHashSet();

        decor.Should().HaveCount(32);
        foreach (var (type, detail) in decor)
        {
            detail.IsActive.Should().BeTrue(type.ToString());
            detail.ItemStorage.Should().Be(1, $"{type}: like every crafted furniture piece");
            detail.Category.Should().Be(StructureCategoryType.Structure, type.ToString());
            detail.LayoutType.Should().Be(PropertyLayoutType.Invalid, $"{type} has no interior");
            detail.CanBeRetrieved.Should().BeTrue(type.ToString());
            detail.RestrictedPropertyTypes.Should().HaveFlag(PropertyType.House).And.HaveFlag(PropertyType.City).And.HaveFlag(PropertyType.Starship);
            detail.Name.Should().MatchRegex("^[\\x20-\\x7E]+$");
            detail.Description.Should().MatchRegex("^[\\x20-\\x7E]+$").And.NotBeEmpty();
            File.Exists(Path.Combine(Root, "Module", "utp", detail.Resref + ".utp.json")).Should().BeTrue($"{type}: {detail.Resref}");
            others.Should().NotContain(detail.Resref, $"{type} looks like no other structure");
        }

        decor.Select(entry => entry.Detail.Resref).Should().OnlyHaveUniqueItems("no two decor pieces look alike");
        decor.Select(entry => entry.Detail.Name).Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void EveryDecorPlaceableIsStaticAndHasNoInventory()
    {
        foreach (var type in Enum.GetValues<StructureType>().Where(type => (int)type is >= 5130 and <= 5189))
        {
            var detail = type.GetAttribute<StructureType, StructureAttribute>();
            using var blueprint = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "Module", "utp", detail.Resref + ".utp.json")));

            Number(blueprint.RootElement, "Static").Should().Be(1, $"{type}: decor is for looking at");
            Number(blueprint.RootElement, "HasInventory").Should().Be(0, type.ToString());
        }
    }

    [Test]
    public void EveryDecorItemIsAStructureItemOfItsIdWithTheStructuresNameAndText()
    {
        foreach (var type in Enum.GetValues<StructureType>().Where(type => (int)type is >= 5130 and <= 5189))
        {
            var detail = type.GetAttribute<StructureType, StructureAttribute>();
            var resref = $"structure_{(int)type}";
            using var item = ReadUti(resref);
            using var template = ReadUti("structure_5022");

            Text(item, "Tag").Should().Be(resref);
            Text(item, "TemplateResRef").Should().Be(resref);
            LocalText(item, "LocalizedName").Should().Be(detail.Name);
            LocalText(item, "Description").Should().Be(detail.Description);
            LocalText(item, "DescIdentified").Should().Be(detail.Description);
            Number(item.RootElement, "BaseItem").Should().Be(Number(template.RootElement, "BaseItem"));
            Number(item.RootElement, "ModelPart1").Should().Be(202, "every structure item shares model 202");
            item.RootElement.GetProperty("PropertiesList").GetRawText().Should().Be(template.RootElement.GetProperty("PropertiesList").GetRawText());
            item.RootElement.TryGetProperty("VarTable", out _).Should().BeFalse($"{type} can be bought, sold and traded: players craft it");
            Palette.Should().Contain($"\"value\": \"{resref}\"").And.Contain($"\"value\": \"{detail.Name}\"");
        }
    }

    [Test]
    public void EveryDecorRecipeTakesTheCategoryLevelSlotsAndBillOfAnExistingFurnitureRecipe()
    {
        var furniture = OrdinaryFurniture().ToList();

        foreach (var (type, recipe) in Decor)
        {
            recipe.Skill.Should().Be(SkillType.Fabrication, type.ToString());
            recipe.Resref.Should().Be($"structure_{(int)type - 8120 + 5130}", type.ToString());
            recipe.EnhancementType.Should().Be(RecipeEnhancementType.Structure, type.ToString());
            recipe.MinigameParameters.Should().BeOfType<FabricatorParameters>(type.ToString());

            var copied = furniture.Any(source =>
                source.Category == recipe.Category &&
                source.Level == recipe.Level &&
                source.EnhancementSlots == recipe.EnhancementSlots &&
                source.Components.OrderBy(component => component.Key).SequenceEqual(recipe.Components.OrderBy(component => component.Key)));
            copied.Should().BeTrue($"{type} copies an existing furniture recipe, so it adds no new number");
        }
    }

    [Test]
    public void ADecorRecipeMakesTheStructureItemOfTheSameName()
    {
        foreach (var (type, recipe) in Decor)
        {
            var structure = Enum.Parse<StructureType>(type.ToString());

            recipe.Resref.Should().Be($"structure_{(int)structure}");
        }
    }

    // The words

    [Test]
    public void EverySignatureAndDecorNameAvoidsCurlyQuotesAndDashes()
    {
        var names = Enum.GetValues<StructureType>().Where(type => (int)type is >= 5130 and <= 5189)
            .SelectMany(type => new[] { type.GetAttribute<StructureType, StructureAttribute>().Name, type.GetAttribute<StructureType, StructureAttribute>().Description });

        names.Should().OnlyContain(text => text.All(character => character >= 0x20 && character <= 0x7E));
    }
}
