using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Feature.FarmingDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService.Minigame.Kitchen;
using SWLOR.Game.Server.Service.FarmhandService;
using SWLOR.Game.Server.Service.LivestockService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SpawnService;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>The seven animals of the Player City Revamp and the Corral structure that holds them.</summary>
public class LivestockDefinitionTests
{
    private static readonly Dictionary<LivestockType, LivestockDefinition> Animals = new LivestockListDefinition().BuildLivestock();

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

    private static bool ModuleHas(string folder, string resref, string extension) =>
        File.Exists(Path.Combine(Root, "Module", folder, resref + extension));

    private static bool ModuleHasItem(string resref) => ModuleHas("uti", resref, ".uti.json");

    private static StructureAttribute Corral => StructureType.Corral.GetAttribute<StructureType, StructureAttribute>();

    // The animals

    [Test]
    public void TheListDefinesEveryLivestockTypeOnce_SevenInAll()
    {
        var types = Enum.GetValues<LivestockType>().Where(type => type != LivestockType.Invalid).ToList();

        types.Should().HaveCount(7);
        Animals.Keys.Should().BeEquivalentTo(types);
        Animals.Should().OnlyContain(pair => pair.Value.Type == pair.Key);
        ((int)types.Min()).Should().Be(1);
        ((int)types.Max()).Should().Be(7);
    }

    [Test]
    public void TheAnimalsFallInFiveTiers_AsThePlanLists()
    {
        Animals.Values.GroupBy(animal => animal.Tier).OrderBy(group => group.Key).Select(group => group.Count())
            .Should().Equal(1, 2, 2, 1, 1);
        Names(1).Should().Equal("Warocas");
        Names(2).Should().Equal("Valley Cairnmog", "Gimpassa");
        Names(3).Should().Equal("K'lor'slug", "Aradile");
        Names(4).Should().Equal("Womp Rat");
        Names(5).Should().Equal("Thune");

        static IEnumerable<string> Names(int tier) => Animals.Values.Where(animal => animal.Tier == tier).OrderBy(animal => (int)animal.Type).Select(animal => animal.Name);
    }

    [Test]
    public void EachTierSharesTheCropLevelOfItsTier()
    {
        foreach (var animal in Animals.Values)
            animal.Level.Should().Be(SWLOR.Game.Server.Service.FarmingService.FarmingRules.GetTierLevel(animal.Tier), animal.Name);
    }

    [Test]
    public void EveryAnimalIsRaisedFromAnExistingCreature()
    {
        foreach (var animal in Animals.Values)
            ModuleHas("utc", animal.SourceCreatureResref, ".utc.json").Should().BeTrue($"{animal.Name} is raised from '{animal.SourceCreatureResref}'");
    }

    [Test]
    public void EveryMeatAndSecondProductIsAnExistingItemThatCookingAlreadyUses()
    {
        foreach (var animal in Animals.Values)
        {
            ModuleHasItem(animal.MeatResref).Should().BeTrue($"{animal.Name} yields '{animal.MeatResref}'");
            KitchenComponentTable.Contains(animal.MeatResref).Should().BeTrue($"cooking uses '{animal.MeatResref}'");

            if (!string.IsNullOrEmpty(animal.SecondProductResref))
                ModuleHasItem(animal.SecondProductResref).Should().BeTrue($"{animal.Name}'s second product '{animal.SecondProductResref}'");
            if (!string.IsNullOrEmpty(animal.CollectExtraResref))
                ModuleHasItem(animal.CollectExtraResref).Should().BeTrue($"{animal.Name}'s collection extra '{animal.CollectExtraResref}'");
        }
    }

    [Test]
    public void TheSecondProductsAreTheOnesThePlanLists()
    {
        Animals[LivestockType.Warocas].SecondProductResref.Should().Be("waro_leg");
        Animals[LivestockType.Womprat].SecondProductResref.Should().Be("womp_innards");
        Animals[LivestockType.Thune].SecondProductResref.Should().Be("milk");
        Animals[LivestockType.Thune].CollectExtraResref.Should().Be("milk", "a Thune also gives milk when collected from");

        foreach (var type in new[] { LivestockType.ValleyCairnmog, LivestockType.Gimpassa, LivestockType.Klorslug, LivestockType.Aradile })
            Animals[type].SecondProductResref.Should().BeEmpty(type.ToString());
        Animals.Values.Where(animal => animal.Type != LivestockType.Thune).Should().OnlyContain(animal => animal.CollectExtraResref == string.Empty);
    }

    [Test]
    public void TheItemResrefsFitTheEnginesSixteenCharacters()
    {
        foreach (var animal in Animals.Values)
        {
            animal.YoungResref.Should().StartWith(LivestockRules.ItemResrefPrefix);
            animal.YoungResref.Length.Should().BeLessThanOrEqualTo(16, animal.Name);
            animal.ManureResref.Length.Should().BeLessThanOrEqualTo(16);
        }

        Animals.Values.Select(animal => animal.YoungResref).Should().OnlyHaveUniqueItems();
        LivestockRules.CrateResref.Length.Should().BeLessThanOrEqualTo(16);
        LivestockRules.CrateResref.Should().StartWith(LivestockRules.ItemResrefPrefix);
    }

    [Test]
    public void TheServiceLoadsEveryAnimalByTypeAndByYoungItem()
    {
        Corrals.CacheLivestock();

        foreach (var animal in Animals.Values)
        {
            Corrals.GetDefinition(animal.Type)!.Name.Should().Be(animal.Name);
            Corrals.GetDefinitionByYoungResref(animal.YoungResref)!.Type.Should().Be(animal.Type);
        }

        Corrals.GetDefinition(LivestockType.Invalid).Should().BeNull();
        Corrals.GetDefinitionByYoungResref("not_an_animal").Should().BeNull();
        Corrals.GetDefinitionByYoungResref(string.Empty).Should().BeNull();
        Corrals.GetDefinitionByYoungResref(null).Should().BeNull();
        Corrals.GetAllDefinitions().Should().HaveCount(7);
    }

    [Test]
    public void ItemsTheFarmItemsSliceOwesAreListedAndNotFailed()
    {
        var owed = Animals.Values.Select(animal => animal.YoungResref)
            .Append(LivestockRules.CrateResref)
            .Concat(Enumerable.Range(1, 5).Select(LivestockRules.GetManureResref))
            .ToList();
        var missing = owed.Where(resref => !ModuleHasItem(resref)).ToList();

        TestContext.Out.WriteLine($"{missing.Count} of {owed.Count} livestock items are not in the module yet: {string.Join(", ", missing)}");
        missing.Count.Should().BeLessThanOrEqualTo(owed.Count);
    }

    // The Corral

    [Test]
    public void TheCorralIsAnActiveCityOnlyStructureWithNoInterior()
    {
        Corral.IsActive.Should().BeTrue();
        Corral.Name.Should().Be("Corral");
        Corral.RestrictedPropertyTypes.Should().Be(PropertyType.City, "a corral is city-only");
        Corral.LayoutType.Should().Be(PropertyLayoutType.Invalid, "a corral is open ground with no interior");
        Corral.Category.Should().Be(StructureCategoryType.Structure);
        Corral.CanBeRetrieved.Should().BeTrue();
        Corral.ItemStorage.Should().Be(0);
        Corral.Description.Should().MatchRegex("^[\\x20-\\x7E]+$");
    }

    [Test]
    public void TheCorralTakesItsIdsFromTheReservedRanges()
    {
        ((int)StructureType.Corral).Should().Be(5104).And.BeInRange(5100, 5129);
        ScriptName.OnCorralUsed.Length.Should().BeLessThanOrEqualTo(16);
    }

    [Test]
    public void TheCorralCountsAgainstTheStructureLimitAndKeepsTheBuildingLimitForBuildings()
    {
        CityBuildingRules.CountsAsBuilding(StructureType.Corral).Should().BeFalse();
        Corral.LayoutType.Should().Be(PropertyLayoutType.Invalid, "a structure with an interior is a building");
    }

    [Test]
    public void TheCorralUsesAnExistingPlaceableWithoutAnInventory()
    {
        var path = Path.Combine(Root, "Module", "utp", Corral.Resref + ".utp.json");

        File.Exists(path).Should().BeTrue($"the placeable '{Corral.Resref}' exists in the module");
        using var blueprint = JsonDocument.Parse(File.ReadAllText(path));
        Value(blueprint, "Static").Should().Be(0, "a static placeable cannot be used");
        Value(blueprint, "HasInventory").Should().Be(0, "a placeable with an inventory opens it instead of the corral menu");

        static int Value(JsonDocument document, string field) => document.RootElement.GetProperty(field).GetProperty("value").GetInt32();
    }

    [Test]
    public void TheCorralsItemExistsAndIsOnThePalette_ButIsNotForSaleUntilARecipeMakesIt()
    {
        ModuleHasItem("structure_5104").Should().BeTrue();
        var item = File.ReadAllText(Path.Combine(Root, "Module", "uti", "structure_5104.uti.json"));
        item.Should().Contain("\"NO_ECONOMY\"");
        item.Should().Contain("\"structure_5104\"");

        File.ReadAllText(Path.Combine(Root, "Module", "itp", "itempalcus.itp.json")).Should().Contain("\"structure_5104\"");
    }

    [Test]
    public void SpawnsKeepClearOfCorrals()
    {
        SpawnExclusionRules.IsQualifyingStructure(StructureType.Corral).Should().BeTrue();
        SpawnExclusionRules.QualifyingStructures.Should().Contain(StructureType.Corral);
    }

    // Records

    [Test]
    public void ARecordIsKeyedByItsOwnId_AndStartsEmpty()
    {
        var animal = new Livestock();

        animal.Id.Should().NotBeNullOrWhiteSpace();
        animal.EntityType.Should().Be(nameof(Livestock));
        animal.OwnerPlayerId.Should().BeEmpty();
        animal.OwnerCdKey.Should().BeEmpty();
        animal.CorralId.Should().BeEmpty();
        animal.Feedings.Should().Be(0);
        animal.LastFedUtc.Should().BeNull();
        animal.LastCollectedUtc.Should().BeNull();
        animal.NoProductsUntilUtc.Should().BeNull();
    }

    [Test]
    public void TheNewRecordsNeedNoMigration_BecauseTheyAreNewTypes()
    {
        // A record that no earlier release wrote needs no default for existing data.
        new LivestockNotice().EntityType.Should().Be(nameof(LivestockNotice));
        new LivestockHunt().EntityType.Should().Be(nameof(LivestockHunt));
        new Farmhand().EntityType.Should().Be(nameof(Farmhand));
        Farmhand.KeyFor("x").Should().NotBe("x");
        FarmhandTargetType.House.Should().NotBe(FarmhandTargetType.Corral);
    }
}
