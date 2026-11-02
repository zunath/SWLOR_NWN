using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Feature.DialogDefinition;
using SWLOR.Game.Server.Feature.FarmingDefinition;
using SWLOR.Game.Server.Feature.SituationDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CityAffairsService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SpawnService;
using SWLOR.Game.Server.Service.TownsfolkService;
using SWLOR.Game.Server.Service.TravelerService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The travelers and the Traveler's Lodge as the module and the server wire them: the structure and its item, the empty
/// store a traveler's stock goes on, the roster, the pools, the City Affairs situation, and the hooks that run the rotation.
/// </summary>
public class TravelerWiringTests
{
    private const int LodgeId = 5103;

    // The young animals and the tier 3 to 5 seeds belong to the farm items slice, so they may not be in the module yet.
    private static IEnumerable<string> FarmItems()
    {
        var young = new LivestockListDefinition().BuildLivestock().Values.Select(animal => animal.YoungResref);
        var seeds = new CropListDefinition().BuildCrops().Values.Select(crop => crop.SeedResref);
        return young.Concat(seeds);
    }

    #region The Lodge

    [Test]
    public void TheLodge_IsACityOnlyOpenGroundStructureWithTheReservedId()
    {
        ((int)StructureType.TravelersLodge).Should().Be(LodgeId);

        var lodge = StructureType.TravelersLodge.GetAttribute<StructureType, StructureAttribute>();
        lodge.Name.Should().Be("Traveler's Lodge");
        lodge.IsActive.Should().BeTrue();
        lodge.RestrictedPropertyTypes.Should().Be(PropertyType.City, "a city, or a station, is the only place a Lodge stands");
        lodge.LayoutType.Should().Be(PropertyLayoutType.Invalid, "the Lodge is a placeable with no interior, so it is outdoors");
        lodge.CanBeRetrieved.Should().BeTrue();
        lodge.Description.Should().MatchRegex("^[\\x20-\\x7E]+$", "player-facing text is plain ASCII");
    }

    [Test]
    public void TheLodge_CountsAgainstTheStructureLimit_NotTheBuildingLimit()
    {
        CityBuildingRules.CountsAsBuilding(StructureType.TravelersLodge).Should().BeFalse();
    }

    [Test]
    public void TheLodge_KeepsHostileSpawnsAndResourceNodesAwayLikeAnyBuilding()
    {
        SpawnExclusionRules.IsQualifyingStructure(StructureType.TravelersLodge).Should().BeTrue();
    }

    [Test]
    public void TheLodge_StandsOnAnExistingPlaceable()
    {
        var lodge = StructureType.TravelersLodge.GetAttribute<StructureType, StructureAttribute>();

        File.Exists(Path.Combine(Root, "Module", "utp", $"{lodge.Resref}.utp.json"))
            .Should().BeTrue($"the placeable '{lodge.Resref}' must exist in the module");
    }

    [Test]
    public void TheLodgesItem_IsTheStructureItemOfItsId_AndNpcOnlyForTheEconomy()
    {
        var path = Path.Combine(Root, "Module", "uti", $"structure_{LodgeId}.uti.json");
        File.Exists(path).Should().BeTrue();

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var item = document.RootElement;
        Text(item, "Tag").Should().Be($"structure_{LodgeId}");
        Text(item, "TemplateResRef").Should().Be($"structure_{LodgeId}");
        item.GetProperty("LocalizedName").GetProperty("value").GetProperty("0").GetString().Should().Be("Traveler's Lodge");
        item.GetProperty("Description").GetProperty("value").GetProperty("0").GetString().Should().MatchRegex("^[\\x20-\\x7E]+$");

        var locals = item.GetProperty("VarTable").GetProperty("value").EnumerateArray()
            .ToDictionary(local => Text(local, "Name"), local => local.GetProperty("Value").GetProperty("value").GetInt32());
        locals["NO_ECONOMY"].Should().Be(1, "like the ring, the item has no source yet: the recipe is a content follow-up");

        File.ReadAllText(Path.Combine(Root, "Module", "itp", "itempalcus.itp.json")).Should().Contain($"\"structure_{LodgeId}\"");
    }

    [Test]
    public void TheLodge_IsWiredIntoPlacement()
    {
        ReadServerSource("Service", "PropertyService", "StructureChangedAction.cs")
            .Should().Contain("Assign(StructureType.TravelersLodge, StructureChangeType.PositionChanged, Travelers.OnLodgePlaced);");

        var property = ReadServerSource("Service", "Property.cs");
        property.Should().Contain("TravelerRules.GetLodgePlacementError(");
        property.Should().Contain("property.StructureType == StructureType.TravelersLodge");
        property.Should().Contain("Travelers.OnLodgeDeleted(property);");
        property.Should().Contain("Travelers.OnCityDeleted(property.Id);");
    }

    [Test]
    public void TheRotation_RunsInCityProcessing_BeforeTheCitiesAreProcessed()
    {
        var property = ReadServerSource("Service", "Property.cs");
        var process = property.IndexOf("private static void ProcessCities()", StringComparison.Ordinal);
        var rotate = property.IndexOf("Travelers.Rotate(now);", process, StringComparison.Ordinal);
        var loop = property.IndexOf("foreach (var city in cities)", process, StringComparison.Ordinal);

        process.Should().BeGreaterThan(0);
        rotate.Should().BeGreaterThan(process).And.BeLessThan(loop, "a situation drawn this start sees who is where");
    }

    #endregion

    #region The store

    [Test]
    public void TheTravelersStoreBlueprint_IsEmptySellsOnlyAndIsInThePalette()
    {
        var resref = CityStores.TravelerStoreBlueprint;
        resref.Length.Should().BeLessThanOrEqualTo(16);

        var path = Path.Combine(Root, "Module", "utm", $"{resref}.utm.json");
        File.Exists(path).Should().BeTrue();

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var store = document.RootElement;
        Text(store, "ResRef").Should().Be(resref);
        Text(store, "Tag").Should().Be(resref);
        store.GetProperty("MarkUp").GetProperty("value").GetInt32().Should().Be(100, "the Sales tax is added to a store whose markup is 100");
        store.GetProperty("MarkDown").GetProperty("value").GetInt32().Should().Be(0, "a traveler never buys from players");
        store.GetProperty("StoreGold").GetProperty("value").GetInt32().Should().Be(-1);
        store.GetProperty("WillOnlyBuy").GetProperty("value").GetArrayLength().Should().Be(0);
        store.GetProperty("WillNotBuy").GetProperty("value").GetArrayLength().Should().Be(0);

        foreach (var panel in store.GetProperty("StoreList").GetProperty("value").EnumerateArray())
            panel.TryGetProperty("ItemList", out _).Should().BeFalse("the whole stock is put on the shelf from the weekly allowances");

        File.ReadAllText(Path.Combine(Root, "Module", "itp", "storepalcus.itp.json")).Should().Contain($"\"{resref}\"");
    }

    [Test]
    public void TheStoreHandlers_KnowTravelers()
    {
        var stores = ReadServerSource("Service", "CityStores.cs");

        stores.Should().Contain("TravelerRules.FindStockItem(traveler, DateTime.UtcNow, resref)", "a purchase is checked against the traveler's weekly allowance");
        stores.Should().Contain("CityStockAllowances.RecordTravelerSale(", "a sale counts against the traveler, whatever city it was in");
        stores.Should().Contain("if (city != null)", "a traveler at the hub has no city: no embargo and no tax");
        stores.Should().Contain("!string.IsNullOrWhiteSpace(cityId)", "the tax is credited to a host city only");
    }

    #endregion

    #region The roster

    [Test]
    public void EveryTraveler_HasADefinition_WithPlainAsciiText()
    {
        foreach (var type in Enum.GetValues<TravelerType>().Where(type => type != TravelerType.Invalid))
        {
            var detail = TravelerCatalog.Get(type);
            detail.Should().NotBeNull($"{type} needs a definition");
            detail.Type.Should().Be(type);
            detail.Key.Should().Be(type.ToString());
            TravelerCatalog.GetByKey(type.ToString()).Should().BeSameAs(detail);

            foreach (var text in new[] { detail.Name, detail.Title, detail.Story, detail.Greeting }.Concat(detail.Rumors))
                text.Should().NotBeNullOrWhiteSpace().And.MatchRegex("^[\\x20-\\x7E]+$", $"{type}: player-facing text is plain ASCII");
        }

        TravelerCatalog.All.Should().HaveCount(4);
        TravelerCatalog.Get(TravelerType.Invalid).Should().BeNull();
        TravelerCatalog.GetByKey("Nobody").Should().BeNull();
    }

    [Test]
    public void TheRoster_IsTheFourTheCityPlanNames()
    {
        TravelerCatalog.Get(TravelerType.BeastTrader).Title.Should().Contain("beast trader");
        TravelerCatalog.Get(TravelerType.Prospector).Title.Should().Contain("prospector");
        TravelerCatalog.Get(TravelerType.FashionMerchant).Title.Should().Contain("fashion");
        TravelerCatalog.Get(TravelerType.InformationBroker).Title.Should().Contain("information broker");
        TravelerCatalog.Get(TravelerType.InformationBroker).Rumors.Should().NotBeEmpty();
    }

    [Test]
    public void EveryTraveler_HasItsOwnNameAndLook()
    {
        TravelerCatalog.All.Values.Select(detail => detail.Name).Should().OnlyHaveUniqueItems();
        TravelerCatalog.All.Values.Select(detail => detail.LookResref).Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void EveryLook_IsAnExistingScriptFreeColonistOfAPlayableSpecies()
    {
        var wanderers = TownsfolkLooks.Wanderers.Select(look => look.Resref).ToHashSet();

        foreach (var detail in TravelerCatalog.All.Values)
        {
            wanderers.Should().Contain(detail.LookResref, "street life already uses this script-free Veles colonist template");

            var path = Path.Combine(Root, "Module", "utc", $"{detail.LookResref}.utc.json");
            File.Exists(path).Should().BeTrue();

            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var utc = document.RootElement;
            Text(utc, "ScriptDialogue").Should().Be("dialog_start", "the template opens its conversation through the dialogue event");
            foreach (var script in new[] { "ScriptAttacked", "ScriptDamaged", "ScriptDeath", "ScriptHeartbeat", "ScriptOnNotice", "ScriptSpawn", "ScriptUserDefine" })
                Text(utc, script).Should().BeEmpty($"{detail.LookResref} {script}");

            utc.GetProperty("Phenotype").GetProperty("value").GetInt32().Should().Be(0, "the normal phenotype only");
        }
    }

    [Test]
    public void TheConversation_IsTheTravelerDialog()
    {
        Travelers.ConversationName.Should().Be(nameof(TravelerDialog));
        new TravelerDialog().Build().DefaultPageId.Should().Be("MAIN_PAGE");
    }

    #endregion

    #region The pools

    private static IEnumerable<(TravelerDetail Traveler, TravelerStockGroup Group, TravelerStockItem Item)> AllItems() =>
        TravelerCatalog.All.Values.SelectMany(traveler => traveler.Groups.SelectMany(group => group.Items.Select(item => (traveler, group, item))));

    [Test]
    public void EveryPool_HasPicksItCanFill_AndPositiveLimits()
    {
        foreach (var traveler in TravelerCatalog.All.Values)
        {
            traveler.Groups.Should().NotBeEmpty();
            foreach (var group in traveler.Groups)
            {
                group.Name.Should().NotBeNullOrWhiteSpace();
                group.Picks.Should().BeGreaterThan(0);
                group.Items.Count.Should().BeGreaterThan(group.Picks, $"{traveler.Key} {group.Name}: a pool no bigger than its picks would never rotate");
                group.Items.Should().OnlyContain(item => item.WeeklyLimit >= 1 && item.WeeklyLimit <= 10);
            }
        }
    }

    [Test]
    public void NoItemIsInTwoPools_SoNoTravelerSellsWhatAnotherDoes()
    {
        AllItems().Select(x => x.Item.Resref).Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void EveryPoolItem_IsAModuleItemOrAFarmItem_AndNothingNpcOnly()
    {
        var farm = FarmItems().ToHashSet();
        var missing = new List<string>();

        foreach (var (_, _, item) in AllItems())
        {
            item.Resref.Length.Should().BeLessThanOrEqualTo(16, item.Resref);

            var path = Path.Combine(Root, "Module", "uti", $"{item.Resref}.uti.json");
            if (!File.Exists(path))
            {
                missing.Add(item.Resref);
                continue;
            }

            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var locals = document.RootElement.TryGetProperty("VarTable", out var table)
                ? table.GetProperty("value").EnumerateArray().Select(local => Text(local, "Name")).ToList()
                : new List<string>();
            locals.Should().NotContain("NO_ECONOMY", $"{item.Resref} is an NPC-only item, which a traveler should not sell");
        }

        missing.Should().OnlyContain(resref => farm.Contains(resref), "only the farm items slice's items may be missing: " + string.Join(", ", missing));
    }

    [Test]
    public void TheBeastTraders_Pool_IsTheSevenYoungAnimals_WithLimitsThatFallWithTheTier()
    {
        var animals = new LivestockListDefinition().BuildLivestock().Values.ToList();
        var pool = TravelerCatalog.Get(TravelerType.BeastTrader).Groups.Single().Items;

        pool.Select(item => item.Resref).Should().BeEquivalentTo(animals.Select(animal => animal.YoungResref));

        var limits = animals.OrderBy(animal => animal.Tier).Select(animal => pool.Single(item => item.Resref == animal.YoungResref).WeeklyLimit).ToList();
        limits.Should().BeInDescendingOrder("the higher the tier, the fewer the trader brings");
        pool.Single(item => item.Resref == animals.Single(animal => animal.Tier == 5).YoungResref).WeeklyLimit.Should().Be(1);
    }

    [Test]
    public void TheProspectors_Seeds_AreExactlyTheTierThreeToFiveSeeds_WithLimitsThatFallWithTheTier()
    {
        var crops = new CropListDefinition().BuildCrops().Values.Where(crop => crop.Tier >= 3).ToList();
        var seeds = TravelerCatalog.Get(TravelerType.Prospector).Groups.Single(group => group.Name == "Seeds").Items;

        seeds.Select(item => item.Resref).Should().BeEquivalentTo(crops.Select(crop => crop.SeedResref));

        foreach (var tier in new[] { 3, 4, 5 })
        {
            var limits = crops.Where(crop => crop.Tier == tier).Select(crop => seeds.Single(item => item.Resref == crop.SeedResref).WeeklyLimit).Distinct().ToList();
            limits.Should().HaveCount(1, $"tier {tier} seeds share one limit");
        }

        var byTier = new[] { 3, 4, 5 }.Select(tier => seeds.Single(item => item.Resref == crops.First(crop => crop.Tier == tier).SeedResref).WeeklyLimit).ToList();
        byTier.Should().BeInDescendingOrder();
    }

    [Test]
    public void TheProspectors_RareMaterials_AreThoseHarvestingOnlyGivesAsRareFinds()
    {
        var harvesting = ReadServerSource("Feature", "LootTableDefinition", "HarvestingLootTableDefinition.cs");
        var rare = Regex.Matches(harvesting, "AddItem\\(\"([a-z0-9_]+)\",\\s*\\d+,\\s*isRare:\\s*true").Select(match => match.Groups[1].Value).ToHashSet();
        var pool = TravelerCatalog.Get(TravelerType.Prospector).Groups.Single(group => group.Name == "Rare materials").Items;

        pool.Select(item => item.Resref).Where(resref => resref != "ore_arda").Should().OnlyContain(resref => rare.Contains(resref));
        pool.Should().OnlyContain(item => item.WeeklyLimit <= 4);
    }

    [Test]
    public void TheBrokersMaps_AreAreaMapsNoStoreSells()
    {
        var sold = Directory.EnumerateFiles(Path.Combine(Root, "Module", "utm"), "*.utm.json")
            .SelectMany(path => Regex.Matches(File.ReadAllText(path), "\"InventoryRes\":\\s*\\{\\s*\"type\":\\s*\"resref\",\\s*\"value\":\\s*\"([^\"]+)\"").Select(match => match.Groups[1].Value))
            .ToHashSet();
        var maps = TravelerCatalog.Get(TravelerType.InformationBroker).Groups.Single().Items;

        maps.Should().OnlyContain(item => item.Resref.StartsWith("map_"));
        maps.Select(item => item.Resref).Where(sold.Contains).Should().BeEmpty("a traveler sells what no store does");
    }

    [Test]
    public void TheFashionMerchant_SellsCosmeticOutfitsWithNoStats()
    {
        var pool = TravelerCatalog.Get(TravelerType.FashionMerchant).Groups.Single().Items;

        foreach (var item in pool)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "Module", "uti", $"{item.Resref}.uti.json")));
            document.RootElement.GetProperty("BaseItem").GetProperty("value").GetInt32().Should().Be(16, $"{item.Resref} is an armor-slot outfit");
            document.RootElement.GetProperty("PropertiesList").GetProperty("value").GetArrayLength().Should().Be(0, $"{item.Resref} carries no stats");
        }
    }

    #endregion

    #region The City Affairs situation

    [Test]
    public void TheBeastTradersRequest_IsTheNextSituationAfterTheFarmOnes()
    {
        ((int)SituationType.BeastTraderPens).Should().Be(14);
        ((int)SituationType.BeastTraderPens).Should().BeGreaterThan((int)SituationType.PredatorsAtTheCorrals);

        var detail = SituationCatalog.Get(SituationType.BeastTraderPens);
        detail.Should().NotBeNull();
        detail.Title.Should().NotBeNullOrWhiteSpace();
        detail.Choices.Should().HaveCount(3);
        detail.IsFollowUp.Should().BeFalse("the trader asks on his own");
    }

    [Test]
    public void TheRequest_ComesOnlyInTheWeekACityHostsTheBeastTrader()
    {
        var detail = SituationCatalog.Get(SituationType.BeastTraderPens);

        detail.Requirements.Should().ContainSingle().Which.Type.Should().Be(SituationRequirementType.TravelerHost);
        SituationRules.AreMet(detail.Requirements, new SituationContext { HostedTravelers = new[] { "BeastTrader" } }).Should().BeTrue();
        SituationRules.AreMet(detail.Requirements, new SituationContext { HostedTravelers = new[] { "Prospector" } }).Should().BeFalse();
        SituationRules.AreMet(detail.Requirements, new SituationContext()).Should().BeFalse();
        SituationRules.Describe(detail.Requirements.Single()).Should().Be("The beast trader visiting");
    }

    [Test]
    public void TheRequests_PayAndGain_StayInsideTheBandsOfTheOtherSituations()
    {
        var detail = SituationCatalog.Get(SituationType.BeastTraderPens);

        foreach (var choice in detail.Choices)
        {
            choice.Cost.Should().BeLessThanOrEqualTo(SituationRules.MaxCost);
            SituationRules.GetGain(choice.Effects).Should().BeLessThanOrEqualTo(SituationRules.MaxGain);
            (choice.Cost > 0 && SituationRules.GetGain(choice.Effects) > 0).Should().BeFalse(choice.Text);
            choice.GazetteText.Should().NotBeNullOrWhiteSpace();
        }

        detail.Choices.Max(choice => choice.Cost).Should().BeLessThanOrEqualTo(10000, "the dearest existing choices cost 10,000 to 15,000");
        SituationRules.GetGain(detail.IgnoredEffects).Should().Be(0, "ignoring a situation never pays");
        SituationRules.GetCost(detail.IgnoredEffects).Should().BeLessThanOrEqualTo(5000);
        detail.IgnoredGazetteText.Should().NotBeNullOrWhiteSpace();
        new TravelerSituationDefinition().Build().Should().ContainKey(SituationType.BeastTraderPens);
    }

    [Test]
    public void TheCitysContext_ListsTheTravelersItHosts()
    {
        ReadServerSource("Service", "CityAffairs.cs").Should().Contain("HostedTravelers = Travelers.GetHostedBy(city.Id)");
    }

    #endregion

    #region Announcements

    [Test]
    public void ArrivalsAreAnnouncedInTheHostsGazette_AndEveryPlayerSeesTheLoginNotice()
    {
        var travelers = ReadServerSource("Service", "Travelers.cs");
        travelers.Should().Contain("CityGazette.Post(city.Id, CityGazetteEntryType.Notice, TravelerText.BuildArrival(detail, city.CustomName));", "Post also sends the line to Discord");

        var notices = ReadServerSource("Feature", "TravelerNotices.cs");
        notices.Should().Contain("[NWNEventHandler(ScriptName.OnModuleEnter)]");
        notices.Should().Contain("TravelerText.BuildLoginNotice(Travelers.GetLocations())");
        notices.Should().NotContain("CitizenPropertyId", "every player is told, citizen or not");
    }

    [Test]
    public void TheTravelerService_UsesTheTownsfolkPatternForAppearingAndLeaving()
    {
        var travelers = ReadServerSource("Service", "Travelers.cs");

        travelers.Should().Contain("[NWNEventHandler(ScriptName.OnAreaEnter)]");
        travelers.Should().Contain("[NWNEventHandler(ScriptName.OnSwlorHeartbeat)]");
        travelers.Should().Contain("TownsfolkRules.ShouldDespawnEmptyArea(");
        travelers.Should().Contain("SetPlotFlag(creature, true);");
        travelers.Should().Contain("TownsfolkRules.NeutralFactionId");
    }

    #endregion

    private static string Text(JsonElement item, string key) => item.GetProperty(key).GetProperty("value").GetString();

    private static string Root
    {
        get
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                directory = directory.Parent;
            return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
        }
    }

    private static string ReadServerSource(params string[] path)
    {
        var full = Path.Combine(new[] { Root, "SWLOR.Game.Server" }.Concat(path).ToArray());
        return File.ReadAllText(full).Replace("\r\n", "\n");
    }
}
