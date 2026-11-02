using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.FarmingDefinition;
using SWLOR.Game.Server.Service.CityStoreService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The city store blueprints (<c>Module/utm/cs_*</c>) and the catalog that picks them: every blueprint exists, sells
/// only, holds only items existing NPC stores already sell (and the tier 1 and 2 crop seeds), and each higher level keeps
/// everything the lower ones sold.
/// </summary>
public class CityStoreBlueprintTests
{
    #region The catalog

    [Test]
    public void EveryStoreKind_HasADefinition()
    {
        foreach (var kind in Enum.GetValues<CityStoreKind>().Where(kind => kind != CityStoreKind.Invalid))
        {
            var detail = CityStoreCatalog.Get(kind);
            detail.Should().NotBeNull($"{kind} needs a store definition");
            detail.Kind.Should().Be(kind);
            detail.Name.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Test]
    public void ABlueprintLadder_StartsAtLevelOneAndRises()
    {
        foreach (var detail in CityStoreCatalog.All)
        {
            detail.Blueprints.Should().NotBeEmpty();
            detail.Blueprints[0].MinLevel.Should().Be(1, $"{detail.Kind} must have a store at its first level");
            detail.Blueprints.Select(x => x.MinLevel).Should().BeInAscendingOrder().And.OnlyHaveUniqueItems();
            detail.Blueprints.Select(x => x.Blueprint).Should().OnlyHaveUniqueItems();
        }
    }

    [TestCase(CityStoreKind.Starport, 1, "cs_port_1")]
    [TestCase(CityStoreKind.Starport, 3, "cs_port_3")]
    [TestCase(CityStoreKind.Starport, 5, "cs_port_5")]
    [TestCase(CityStoreKind.Cantina, 2, "cs_cantina_2")]
    [TestCase(CityStoreKind.Cantina, 5, "cs_cantina_3")]
    [TestCase(CityStoreKind.MedicalCenter, 4, "cs_medical_1")]
    [TestCase(CityStoreKind.CityHall, 2, "cs_hall_2")]
    [TestCase(CityStoreKind.StallDecor, 1, "cs_stall_decor")]
    public void ALevel_OpensTheHighestBlueprintItHasReached(CityStoreKind kind, int level, string expected)
    {
        CityStoreRules.GetBlueprint(CityStoreCatalog.Get(kind), level).Should().Be(expected);
    }

    [Test]
    public void ALevelBelowTheFirst_StillOpensTheFirstBlueprint()
    {
        CityStoreRules.GetBlueprint(CityStoreCatalog.Get(CityStoreKind.Starport), 0).Should().Be("cs_port_1");
    }

    [TestCase(MarketStallTheme.GeneralGoods, CityStoreKind.StallGeneralGoods)]
    [TestCase(MarketStallTheme.CraftingSupplies, CityStoreKind.StallCraftingSupplies)]
    [TestCase(MarketStallTheme.Decor, CityStoreKind.StallDecor)]
    [TestCase(MarketStallTheme.Invalid, CityStoreKind.StallGeneralGoods)]
    public void AStallTheme_OpensItsStore(MarketStallTheme theme, CityStoreKind expected)
    {
        CityStoreRules.GetKindForTheme(theme).Should().Be(expected);
        CityStoreRules.IsStallKind(expected).Should().BeTrue();
    }

    [Test]
    public void ABuildingStore_IsNotAStallStore()
    {
        CityStoreRules.IsStallKind(CityStoreKind.Cantina).Should().BeFalse();
    }

    [Test]
    public void TheExclusiveItems_AreALaterContentStep_SoNoStoreHasLimitedStockYet()
    {
        CityStoreCatalog.All.Should().OnlyContain(detail => detail.LimitedItems.Count == 0);
    }

    #endregion

    #region The blueprints

    [Test]
    public void EveryBlueprint_ExistsAndSellsOnly()
    {
        foreach (var resref in AllBlueprints())
        {
            resref.Length.Should().BeLessThanOrEqualTo(16, "NWN archives omit resources with longer resrefs");

            var store = ReadStore(resref);
            Value(store, "ResRef").Should().Be(resref);
            Value(store, "Tag").Should().Be(resref);
            store["MarkUp"]!["value"]!.Value<int>().Should().Be(SalesTaxRules.StandardMarkUpPercent,
                "the Sales tax is added to a store whose markup is 100, and the credited part is worked out from that");
            store["MarkDown"]!["value"]!.Value<int>().Should().Be(0, "city stores never buy from players");
            ((JArray)store["WillOnlyBuy"]!["value"]!).Should().BeEmpty();
            ((JArray)store["WillNotBuy"]!["value"]!).Should().BeEmpty();
            store["StoreGold"]!["value"]!.Value<int>().Should().Be(-1);
        }
    }

    [Test]
    public void EveryStoreItem_IsInfiniteAndHasABlueprint()
    {
        foreach (var resref in AllBlueprints())
        {
            var entries = Entries(ReadStore(resref)).ToList();
            entries.Should().NotBeEmpty(resref);
            entries.Select(x => x.Resref).Should().OnlyHaveUniqueItems($"{resref} lists an item once");

            foreach (var entry in entries)
            {
                entry.Infinite.Should().Be(1, $"{resref}: {entry.Resref} is stocked without limit; limited items come from allowances");
                File.Exists(Path.Combine(Root, "Module", "uti", $"{entry.Resref}.uti.json"))
                    .Should().BeTrue($"{resref} lists '{entry.Resref}', which has no item blueprint");
            }
        }
    }

    /// <summary>The tier 1 and 2 crop seeds, which the Cantina and the crafting supplies stall sell.</summary>
    private static readonly HashSet<string> FarmSeeds = new CropListDefinition().BuildCrops().Values
        .Where(crop => crop.Tier <= 2).Select(crop => crop.SeedResref).ToHashSet();

    [Test]
    public void EveryStoreItem_IsAnItemExistingNpcStoresAlreadySell()
    {
        var existing = Directory.EnumerateFiles(Path.Combine(Root, "Module", "utm"), "*.utm.json")
            .Where(path => !Path.GetFileName(path).StartsWith("cs_", StringComparison.Ordinal))
            .SelectMany(path => Entries(JObject.Parse(File.ReadAllText(path))))
            .Select(x => x.Resref)
            .ToHashSet();
        existing.UnionWith(FarmSeeds);

        foreach (var resref in AllBlueprints())
        {
            Entries(ReadStore(resref)).Select(x => x.Resref)
                .Where(item => !existing.Contains(item))
                .Should().BeEmpty($"{resref} must hold only existing items for now");
        }
    }

    [Test]
    public void EveryItemOnAShelf_FitsInsideTheStoreGridWithoutOverlap()
    {
        foreach (var resref in AllBlueprints())
        {
            var store = ReadStore(resref);
            foreach (var panel in (JArray)store["StoreList"]!["value"]!)
            {
                var occupied = new HashSet<(int X, int Y)>();
                var items = panel["ItemList"]?["value"] as JArray;
                if (items == null)
                    continue;

                foreach (var item in items)
                {
                    var x = item["Repos_PosX"]!["value"]!.Value<int>();
                    var y = item["Repos_Posy"]!["value"]!.Value<int>();
                    x.Should().BeInRange(0, 9);
                    occupied.Add((x, y)).Should().BeTrue($"{resref}: two items share the cell {x},{y}");
                }
            }
        }
    }

    [Test]
    public void AHigherLevel_KeepsEverythingTheLowerLevelsSold()
    {
        foreach (var detail in CityStoreCatalog.All)
        {
            HashSet<string> previous = null;
            foreach (var (_, blueprint) in detail.Blueprints)
            {
                var items = Entries(ReadStore(blueprint)).Select(x => x.Resref).ToHashSet();
                if (previous != null)
                    previous.Except(items).Should().BeEmpty($"{blueprint} must keep what the level below it sold");

                previous = items;
            }
        }
    }

    [Test]
    public void TheTopStarportLevel_SellsEverythingTheDockhandStoreDid()
    {
        var dockhand = Entries(JObject.Parse(File.ReadAllText(Path.Combine(Root, "Module", "utm", "dockhand_store.utm.json"))))
            .Select(x => x.Resref).ToHashSet();
        var top = Entries(ReadStore("cs_port_5")).Select(x => x.Resref).ToHashSet();

        dockhand.Except(top).Should().BeEmpty();
        top.Except(dockhand).Should().BeEmpty("a city starport sells the dockhand's ship supplies and nothing else");
    }

    [Test]
    public void TheCantinaSellsFoodDrinkAndTheFirstSeedsOnly_AndTheMedicalCenterOnlyMedicalSupplies()
    {
        Entries(ReadStore("cs_medical_1")).Select(x => x.Resref)
            .Should().BeEquivalentTo("med_supplies", "med_supplies_50", "stim_pack", "stim_pack_50");

        foreach (var (_, blueprint) in CityStoreCatalog.Get(CityStoreKind.Cantina).Blueprints)
        {
            foreach (var entry in Entries(ReadStore(blueprint)).Where(entry => !FarmSeeds.Contains(entry.Resref)))
            {
                var baseItem = ReadItem(entry.Resref)["BaseItem"]!["value"]!.Value<int>();
                baseItem.Should().BeOneOf(new[] { 29, 211, 311 }, $"{entry.Resref} is not food or drink");
            }
        }
    }

    [Test]
    public void EveryBlueprint_IsListedInTheStorePalette()
    {
        var palette = File.ReadAllText(Path.Combine(Root, "Module", "itp", "storepalcus.itp.json"));

        foreach (var resref in AllBlueprints())
            palette.Should().Contain($"\"{resref}\"", $"the toolset palette must list {resref}");
    }

    #endregion

    private static IEnumerable<string> AllBlueprints()
    {
        return CityStoreCatalog.All.SelectMany(detail => detail.Blueprints.Select(x => x.Blueprint)).Distinct();
    }

    private static JObject ReadStore(string resref)
    {
        var path = Path.Combine(Root, "Module", "utm", $"{resref}.utm.json");
        File.Exists(path).Should().BeTrue($"store blueprint '{resref}' must be in the module");
        return JObject.Parse(File.ReadAllText(path));
    }

    private static JObject ReadItem(string resref)
    {
        return JObject.Parse(File.ReadAllText(Path.Combine(Root, "Module", "uti", $"{resref}.uti.json")));
    }

    private static IEnumerable<(string Resref, int Infinite)> Entries(JObject store)
    {
        foreach (var panel in (JArray)store["StoreList"]!["value"]!)
        {
            if (panel["ItemList"]?["value"] is not JArray items)
                continue;

            foreach (var item in items)
            {
                var resref = item["InventoryRes"]?["value"]?.Value<string>();
                if (!string.IsNullOrEmpty(resref))
                    yield return (resref, item["Infinite"]?["value"]?.Value<int>() ?? 0);
            }
        }
    }

    private static string Value(JObject store, string key) => store[key]!["value"]!.Value<string>();

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
}
