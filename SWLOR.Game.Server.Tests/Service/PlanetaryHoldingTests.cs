using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.HoldingDefinition;
using SWLOR.Game.Server.Feature.TradeDefinition;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.HoldingService;
using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Planetary Holdings (plan section 26): the catalog, buying and the limit, upkeep and foreclosure, daily output and the
/// warehouse, collection into cargo, upgrades and their delivered goods, Processing Plant chains, the staff droid's
/// bonus, events, holding goods at the exchanges and the income check. The numbers come from
/// tools/SpaceRevampBalance/holdings.py.
/// </summary>
public class PlanetaryHoldingTests
{
    private static readonly DateTime Start = new(2026, 10, 28, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>A random source that always rolls the same number.</summary>
    private sealed class FixedRandom : System.Random
    {
        private readonly double _value;
        public FixedRandom(double value) => _value = value;
        protected override double Sample() => _value;
        public override double NextDouble() => _value;
    }

    private static readonly System.Random NoEvents = new FixedRandom(0.999);

    private static HoldingDetail Detail(HoldingType type) => HoldingRules.GetDetail(type);

    private static PlanetaryHolding New(HoldingType type) => HoldingRules.Create("player", Detail(type), "tatooine_dock", Start);

    private static HoldingAdvanceResult Advance(PlanetaryHolding holding, double days, System.Random random = null) =>
        HoldingRules.Advance(holding, Detail(holding.Type), Start.AddDays(days), random ?? NoEvents);

    private static int Units(PlanetaryHolding holding, CommodityType commodity) =>
        holding.Warehouse.TryGetValue(commodity, out var units) ? units : 0;

    private static string Root
    {
        get
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                directory = directory.Parent;
            return directory?.FullName ?? throw new DirectoryNotFoundException();
        }
    }

    private static string Source(string relative) => File.ReadAllText(Path.Combine(Root, "SWLOR.Game.Server", relative));

    // Catalog

    [Test]
    public void Catalog_HasEveryPlannedHoldingAtItsPlanet()
    {
        var expected = new Dictionary<HoldingType, (TradeExchangeType Exchange, CommodityType Output)>
        {
            { HoldingType.CrystalMine, (TradeExchangeType.Viscara, CommodityType.ViscaranCrystals) },
            { HoldingType.Apiary, (TradeExchangeType.Viscara, CommodityType.VelesHoney) },
            { HoldingType.AssemblyLine, (TradeExchangeType.CZ220, CommodityType.CzerkaMachineParts) },
            { HoldingType.KelpFishery, (TradeExchangeType.MonCala, CommodityType.Seafood) },
            { HoldingType.DesalinationPlant, (TradeExchangeType.MonCala, CommodityType.PurifiedWater) },
            { HoldingType.TimberCamp, (TradeExchangeType.Hutlar, CommodityType.AraciaTimber) },
            { HoldingType.ByyskRanch, (TradeExchangeType.Hutlar, CommodityType.ByyskHides) },
            { HoldingType.MoistureFarm, (TradeExchangeType.Tatooine, CommodityType.PurifiedWater) },
            { HoldingType.ScrapYard, (TradeExchangeType.Tatooine, CommodityType.JawaSalvage) },
            { HoldingType.SpiceLab, (TradeExchangeType.SmugglersMoon, CommodityType.Moonspice) },
            { HoldingType.SaltWorks, (TradeExchangeType.SmugglersMoon, CommodityType.NeonSalt) },
            { HoldingType.ExcavationDig, (TradeExchangeType.Korriban, CommodityType.SithRelics) },
            { HoldingType.HerbGrove, (TradeExchangeType.Dathomir, CommodityType.WitchHerbs) },
            { HoldingType.GrainFarm, (TradeExchangeType.Dantooine, CommodityType.Grain) },
            { HoldingType.BanthaRanch, (TradeExchangeType.Dantooine, CommodityType.BanthaMilk) },
            { HoldingType.RicePaddy, (TradeExchangeType.Eshan, CommodityType.EshanRice) },
        };

        HoldingCatalog.Holdings.Select(x => x.Type).Should().BeEquivalentTo(Enum.GetValues<HoldingType>().Where(x => x != HoldingType.Invalid));
        foreach (var (type, (exchange, output)) in expected)
        {
            var detail = Detail(type);
            detail.Exchange.Should().Be(exchange, type.ToString());
            detail.Output.Should().Be(output, type.ToString());
            detail.BatchUnits.Should().BePositive();
            detail.PurchasePrice.Should().BePositive();
            HoldingRules.GetDailyUpkeep(detail, 3, 0).Should().BeGreaterThan(HoldingRules.GetDailyUpkeep(detail, 1, 0));
            detail.Upgrades.Select(x => x.Level).Should().Equal(2, 3);
            detail.Name.Should().MatchRegex("^[A-Za-z' ]+$");
        }

        foreach (var exchange in TradeCatalog.Exchanges)
            HoldingRules.GetHoldingsAt(exchange.Type).Should().NotBeEmpty($"{exchange.Name} sells holdings");
    }

    [Test]
    public void Catalog_UpgradesInputsAndRepairsComeFromOtherPlanets()
    {
        foreach (var detail in HoldingCatalog.Holdings)
        {
            var goods = detail.Upgrades.SelectMany(x => x.Goods.Keys)
                .Concat(detail.Plant.Goods.Keys)
                .Append(detail.Chain.Input)
                .Append(detail.RepairGood);
            foreach (var good in goods)
            {
                var commodity = TradeLookup.GetCommodity(good);
                commodity.Producers.Should().NotContain(detail.Exchange, $"{detail.Name} needs {commodity.Name} hauled from another planet");
                commodity.HoldingSources.Should().NotContain(detail.Exchange);
            }

            detail.Upgrades.Single(x => x.Level == 3).Goods.Should().HaveCount(2, "a level 3 holding needs two hauled goods");
        }

        Detail(HoldingType.MoistureFarm).Upgrades.Single(x => x.Level == 3).Goods.Keys.Should()
            .BeEquivalentTo(new[] { CommodityType.CzerkaMachineParts, CommodityType.ReactorCores }, "the plan's example");
    }

    [Test]
    public void Catalog_ChainsMakeAGoodWorthMoreThanBothInputs()
    {
        foreach (var detail in HoldingCatalog.Holdings)
        {
            var chain = detail.Chain;
            var inputs = chain.OwnUnits * TradeLookup.GetCommodity(detail.Output).BasePrice +
                         chain.InputUnits * TradeLookup.GetCommodity(chain.Input).BasePrice;
            var refined = chain.RefinedUnits * TradeLookup.GetCommodity(chain.Refined).BasePrice;
            ((double)refined / inputs).Should().BeInRange(1.10, 1.25, detail.Name);
        }

        Detail(HoldingType.GrainFarm).Chain.Refined.Should().Be(CommodityType.RationPacks);
        Detail(HoldingType.GrainFarm).Chain.Input.Should().Be(CommodityType.PurifiedWater);
        Detail(HoldingType.CrystalMine).Chain.Refined.Should().Be(CommodityType.FocusingLenses);
        Detail(HoldingType.ByyskRanch).Chain.Input.Should().Be(CommodityType.WitchHerbs);
        Detail(HoldingType.ByyskRanch).Chain.Refined.Should().Be(CommodityType.MedicalSupplies);
        Detail(HoldingType.DesalinationPlant).Chain.Refined.Should().Be(CommodityType.StationFuel, "Station Fuel's second source");
        Detail(HoldingType.DesalinationPlant).Chain.Input.Should().Be(CommodityType.ReactorCores);
    }

    [Test]
    public void Catalog_SitesAreInstanceTemplatesWithAnEntranceAndATerminal()
    {
        var layouts = string.Join("\n", Directory.GetFiles(Path.Combine(Root, "SWLOR.Game.Server", "Feature", "PropertyLayoutDefinition"), "*.cs")
            .Select(File.ReadAllText));
        var areaCache = Source(Path.Combine("Service", "Area.cs"));
        areaCache.Should().Contain("HoldingService.HoldingTuning.OutdoorSiteResrefs", "the outdoor templates are kept out of saved locations");
        foreach (var resref in HoldingCatalog.Holdings.Select(x => x.SiteAreaResref).Distinct())
        {
            if (!HoldingTuning.OutdoorSiteResrefs.Contains(resref))
                layouts.Should().Contain($".AreaInstance(\"{resref}\")", $"{resref} is a property instance template, kept out of saved locations");
            var git = File.ReadAllText(Path.Combine(Root, "Module", "git", resref + ".git.json"));
            git.Should().Contain("\"building_exit\"", "the office terminal is the template's exit placeable");
            git.Should().Contain("\"PROPERTY_ENTRANCE\"");
        }
    }

    [Test]
    public void OutdoorSites_AreStrippedCopiesWithOneEntranceAndTheOfficeTerminal()
    {
        HoldingTuning.OutdoorSiteResrefs.Should().BeEquivalentTo("holding_plains", "holding_forest", "holding_desert", "holding_cave");
        Detail(HoldingType.GrainFarm).SiteAreaResref.Should().Be("holding_plains");
        Detail(HoldingType.TimberCamp).SiteAreaResref.Should().Be("holding_forest");
        Detail(HoldingType.MoistureFarm).SiteAreaResref.Should().Be("holding_desert");
        Detail(HoldingType.CrystalMine).SiteAreaResref.Should().Be("holding_cave");
        Detail(HoldingType.AssemblyLine).SiteAreaResref.Should().Be("player_rnd", "industrial holdings keep their interior");
        HoldingCatalog.Holdings.Count(x => HoldingTuning.OutdoorSiteResrefs.Contains(x.SiteAreaResref)).Should().Be(12);

        var ifo = File.ReadAllText(Path.Combine(Root, "Module", "ifo", "module.ifo.json"), System.Text.Encoding.Latin1);
        var categories = File.ReadAllText(Path.Combine(Root, "toolset", "categories.json"));
        foreach (var resref in HoldingTuning.OutdoorSiteResrefs)
        {
            using var git = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "Module", "git", resref + ".git.json"), System.Text.Encoding.Latin1));
            var g = git.RootElement;
            foreach (var list in new[] { "Creature List", "Door List", "Encounter List", "SoundList", "StoreList", "TriggerList" })
                g.GetProperty(list).GetProperty("value").GetArrayLength().Should().Be(0, $"{resref} {list}");
            g.TryGetProperty("VarTable", out _).Should().BeFalse("no spawn tables, exploration or map locals");
            var placeables = g.GetProperty("Placeable List").GetProperty("value").EnumerateArray().ToList();
            placeables.Where(p => p.GetProperty("Tag").GetProperty("value").GetString() == "building_exit").Should().ContainSingle(resref);
            placeables.Where(p => p.GetProperty("Tag").GetProperty("value").GetString() != "building_exit")
                .Should().OnlyContain(p => p.GetProperty("Static").GetProperty("value").GetInt32() == 1, "scenery only");
            g.GetProperty("WaypointList").GetProperty("value").EnumerateArray()
                .Select(w => w.GetProperty("Tag").GetProperty("value").GetString()).Should().Equal("PROPERTY_ENTRANCE");

            using var are = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "Module", "are", resref + ".are.json"), System.Text.Encoding.Latin1));
            are.RootElement.GetProperty("ResRef").GetProperty("value").GetString().Should().Be(resref);
            File.Exists(Path.Combine(Root, "Module", "gic", resref + ".gic.json")).Should().BeTrue();
            ifo.Should().Contain($"\"{resref}\"");
            categories.Should().Contain($"\"{resref}\"");
        }
    }

    // Buying

    [Test]
    public void Buying_ChecksTheDockTheLimitAndCredits()
    {
        var farm = Detail(HoldingType.MoistureFarm);
        HoldingRules.GetPurchaseRefusal(farm, TradeExchangeType.Tatooine, 0, farm.PurchasePrice).Should().Be(HoldingRefusal.None);
        HoldingRules.GetPurchaseRefusal(farm, TradeExchangeType.Hutlar, 0, farm.PurchasePrice).Should().Be(HoldingRefusal.NotSoldHere);
        HoldingRules.GetPurchaseRefusal(farm, TradeExchangeType.Tatooine, 2, farm.PurchasePrice).Should().Be(HoldingRefusal.None);
        HoldingRules.GetPurchaseRefusal(farm, TradeExchangeType.Tatooine, 3, farm.PurchasePrice).Should().Be(HoldingRefusal.AtLimit);
        HoldingRules.GetPurchaseRefusal(farm, TradeExchangeType.Tatooine, 0, farm.PurchasePrice - 1).Should().Be(HoldingRefusal.NotEnoughCredits);
        HoldingTuning.MaximumHoldings.Should().Be(3);
        HoldingRules.GetRefusalMessage(HoldingRefusal.AtLimit).Should().Contain("account");

        var holding = New(HoldingType.MoistureFarm);
        holding.Level.Should().Be(1);
        holding.PricePaid.Should().Be(farm.PurchasePrice);
        holding.LastProcessedAt.Should().Be(Start);
        holding.Warehouse.Should().BeEmpty();
        holding.RefineOutput.Should().BeTrue();
    }

    [Test]
    public void Abandoning_RefundsHalfThePriceLessTheUpkeepOwed()
    {
        var holding = New(HoldingType.GrainFarm);
        HoldingRules.GetSellBackRefund(holding).Should().Be((int)Math.Round(holding.PricePaid * 0.5, MidpointRounding.AwayFromZero));
        holding.UpkeepOwed = 1000;
        HoldingRules.GetSellBackRefund(holding).Should().Be((int)Math.Round(holding.PricePaid * 0.5, MidpointRounding.AwayFromZero) - 1000);
        holding.UpkeepOwed = holding.PricePaid;
        HoldingRules.GetSellBackRefund(holding).Should().Be(0);
    }

    // Output and upkeep

    [Test]
    public void Output_OneBatchADayIntoTheWarehouse()
    {
        var holding = New(HoldingType.MoistureFarm);
        var detail = Detail(HoldingType.MoistureFarm);

        Advance(holding, 0.9).Days.Should().Be(0, "a part day makes nothing yet");
        holding.Warehouse.Should().BeEmpty();

        Advance(holding, 3.5).Days.Should().Be(3);
        Units(holding, CommodityType.PurifiedWater).Should().Be(3 * detail.BatchUnits);
        holding.LastProcessedAt.Should().Be(Start.AddDays(3));
    }

    [Test]
    public void Upkeep_AccruesDailyAndLosesTheHoldingAtTheLimit()
    {
        var holding = New(HoldingType.SaltWorks);
        var detail = Detail(HoldingType.SaltWorks);

        Advance(holding, 2).Foreclosed.Should().BeFalse();
        holding.UpkeepOwed.Should().Be(2 * HoldingRules.GetDailyUpkeep(holding, detail));
        HoldingRules.GetDailyUpkeep(detail, 1, 0).Should().Be(
            (int)Math.Round(detail.BatchUnits * 50 * HoldingTuning.UpkeepShare, MidpointRounding.AwayFromZero), "a share of the day's output");

        var result = Advance(holding, 30);
        result.Foreclosed.Should().BeTrue();
        holding.UpkeepOwed.Should().Be(HoldingRules.GetForeclosureLimit(holding, detail));
        HoldingRules.GetForeclosureLimit(holding, detail).Should().Be(HoldingTuning.ForeclosureDays * HoldingRules.GetDailyUpkeep(holding, detail));

        holding.DroidItemId = "droid";
        holding.DroidIndustry = 100;
        HoldingRules.GetDailyUpkeep(holding, detail).Should().BeGreaterThan(HoldingRules.GetDailyUpkeep(detail, 1, 0), "staff wages grow with the output");
    }

    [Test]
    public void Warehouse_FullStopsOutput()
    {
        var holding = New(HoldingType.GrainFarm);
        var detail = Detail(HoldingType.GrainFarm);

        Advance(holding, HoldingTuning.StorageDays + 3);
        Units(holding, CommodityType.Grain).Should().Be(HoldingTuning.StorageDays * detail.BatchUnits);
        HoldingRules.GetStoredVolume(holding.Warehouse).Should().Be(HoldingRules.GetStorageVolume(holding, detail));
    }

    // Collection

    [Test]
    public void Collection_LoadsWhatFitsTheMostValuableFirstAtBasePrice()
    {
        var warehouse = new Dictionary<CommodityType, int> { { CommodityType.Grain, 50 }, { CommodityType.RationPacks, 10 } };

        // Ration Packs (150, volume 2) before Grain (60, volume 2); 60 volume takes 10 packs and 20 grain.
        var plan = HoldingRules.PlanCollection(warehouse, 60);
        plan[CommodityType.RationPacks].Should().Be(10);
        plan[CommodityType.Grain].Should().Be(20);

        HoldingRules.PlanCollection(warehouse, 1).Should().BeEmpty("one volume fits no unit of either");
        HoldingRules.PlanCollection(warehouse, 1000).Should().BeEquivalentTo(warehouse);

        HoldingRules.GetCostBasis(CommodityType.Grain, 20).Should().Be(20 * 60, "collected goods give trade XP only for the haul");
        HoldingRules.RemoveStock(warehouse, CommodityType.RationPacks, 10);
        warehouse.Should().NotContainKey(CommodityType.RationPacks);
    }

    [Test]
    public void Collection_GoesIntoTheCargoBayAsCommodityUnits()
    {
        var holding = New(HoldingType.ScrapYard);
        Advance(holding, 2);
        var cargo = new Dictionary<CommodityType, CargoLot>();
        foreach (var (commodity, units) in HoldingRules.PlanCollection(holding.Warehouse, 500))
        {
            CargoHoldRules.AddUnits(cargo, commodity, units, HoldingRules.GetCostBasis(commodity, units));
            HoldingRules.RemoveStock(holding.Warehouse, commodity, units);
        }

        var batch = Detail(HoldingType.ScrapYard).BatchUnits;
        CargoHoldRules.GetUnits(cargo, CommodityType.JawaSalvage).Should().Be(2 * batch);
        cargo[CommodityType.JawaSalvage].CostBasis.Should().Be(2 * batch * 50);
        holding.Warehouse.Should().BeEmpty();
    }

    // Upgrades and deliveries

    [Test]
    public void Upgrades_NeedTheirGoodsDeliveredAndCredits()
    {
        var holding = New(HoldingType.MoistureFarm);
        var detail = Detail(HoldingType.MoistureFarm);
        var level2 = detail.Upgrades.Single(x => x.Level == 2);

        HoldingRules.GetUpgradeRefusal(holding, detail, 1_000_000).Should().Be(HoldingRefusal.MissingGoods);
        HoldingRules.GetNeeds(holding, detail).Select(x => x.Commodity).Should().Contain(level2.Goods.Keys);

        foreach (var (commodity, units) in level2.Goods)
        {
            HoldingRules.GetDeliverableUnits(holding, detail, commodity).Should().BeGreaterThanOrEqualTo(units);
            HoldingRules.Deliver(holding, detail, commodity, units, Start, new HoldingAdvanceResult());
        }

        HoldingRules.GetUpgradeRefusal(holding, detail, level2.Credits - 1).Should().Be(HoldingRefusal.NotEnoughCredits);
        HoldingRules.GetUpgradeRefusal(holding, detail, level2.Credits).Should().Be(HoldingRefusal.None);
        HoldingRules.ApplyUpgrade(holding, detail);
        holding.Level.Should().Be(2);
        holding.Supplies.Should().BeEmpty("the level's goods are used up");
        HoldingRules.GetDailyUnits(holding, detail).Should().Be((int)Math.Round(detail.BatchUnits * 1.25, MidpointRounding.AwayFromZero));
        HoldingRules.GetDailyUpkeep(detail, 2, 0).Should().BeGreaterThan(HoldingRules.GetDailyUpkeep(detail, 1, 0));

        foreach (var (commodity, units) in detail.Upgrades.Single(x => x.Level == 3).Goods)
            HoldingRules.Deliver(holding, detail, commodity, units, Start, new HoldingAdvanceResult());
        HoldingRules.ApplyUpgrade(holding, detail);
        holding.Level.Should().Be(3);
        HoldingRules.GetUpgradeRefusal(holding, detail, 1_000_000).Should().Be(HoldingRefusal.MaximumLevel);
        HoldingRules.GetDailyUnits(holding, detail).Should().Be((int)Math.Round(detail.BatchUnits * 1.45, MidpointRounding.AwayFromZero));
    }

    [Test]
    public void Deliveries_StopAtWhatTheHoldingStillTakes()
    {
        var holding = New(HoldingType.GrainFarm);
        var detail = Detail(HoldingType.GrainFarm);

        HoldingRules.GetDeliverableUnits(holding, detail, CommodityType.SithRelics).Should().Be(0, "it needs no relics");
        var wanted = HoldingRules.GetDeliverableUnits(holding, detail, CommodityType.CzerkaMachineParts);
        wanted.Should().Be(detail.Upgrades.Single(x => x.Level == 2).Goods[CommodityType.CzerkaMachineParts]);
        HoldingRules.Deliver(holding, detail, CommodityType.CzerkaMachineParts, wanted, Start, new HoldingAdvanceResult());
        HoldingRules.GetDeliverableUnits(holding, detail, CommodityType.CzerkaMachineParts).Should().Be(0);
    }

    // Processing Plant chains

    [Test]
    public void Chains_RefineTheOutputWithTheDeliveredInput()
    {
        var holding = New(HoldingType.GrainFarm);
        var detail = Detail(HoldingType.GrainFarm);
        HoldingRules.GetPlantRefusal(holding, detail, 1_000_000).Should().Be(HoldingRefusal.MissingGoods);
        foreach (var (commodity, units) in detail.Plant.Goods)
            HoldingRules.Deliver(holding, detail, commodity, units, Start, new HoldingAdvanceResult());
        HoldingRules.GetPlantRefusal(holding, detail, detail.Plant.Credits).Should().Be(HoldingRefusal.None);
        HoldingRules.ApplyPlant(holding, detail);
        holding.HasProcessingPlant.Should().BeTrue();
        HoldingRules.GetPlantRefusal(holding, detail, 1_000_000).Should().Be(HoldingRefusal.PlantBuilt);

        // The plant takes its share of each day's output (35% since W11e), and its input up to some days of recipes.
        var plantUnits = HoldingRules.GetPlantUnits(detail.BatchUnits);
        plantUnits.Should().Be((int)(detail.BatchUnits * 0.35));
        HoldingRules.GetDeliverableUnits(holding, detail, CommodityType.PurifiedWater).Should().Be(HoldingTuning.InputStockDays * plantUnits);

        // Grain 1 + Water 1 make 1 Ration Pack, up to the plant's share of the output; the rest stays grain.
        HoldingRules.Deliver(holding, detail, CommodityType.PurifiedWater, plantUnits + 5, Start, new HoldingAdvanceResult());
        Advance(holding, 1);
        Units(holding, CommodityType.RationPacks).Should().Be(plantUnits);
        Units(holding, CommodityType.Grain).Should().Be(detail.BatchUnits - plantUnits);
        HoldingRules.GetSupply(holding, CommodityType.PurifiedWater).Should().Be(5);

        // Short of input the plant refines what it has; without input, or with refining off, the output stays raw.
        Advance(holding, 2);
        Units(holding, CommodityType.RationPacks).Should().Be(plantUnits + 5);
        Units(holding, CommodityType.Grain).Should().Be(2 * detail.BatchUnits - plantUnits - 5);
        HoldingRules.Deliver(holding, detail, CommodityType.PurifiedWater, 10, Start, new HoldingAdvanceResult());
        holding.RefineOutput = false;
        Advance(holding, 3);
        Units(holding, CommodityType.RationPacks).Should().Be(plantUnits + 5);
        HoldingRules.GetSupply(holding, CommodityType.PurifiedWater).Should().Be(10);
    }

    [Test]
    public void Chains_RunWholeRecipesOnly()
    {
        var chain = Detail(HoldingType.CrystalMine).Chain; // 5 crystals + 2 machine parts make 2 lenses
        HoldingRules.Refine(chain, 51, 100).Should().Be((20, 50, 20));
        HoldingRules.Refine(chain, 51, 5).Should().Be((4, 10, 4));
        HoldingRules.Refine(chain, 4, 100).Should().Be((0, 0, 0));
    }

    // Droid staff

    [Test]
    public void Droid_RaisesOutputByItsIndustry()
    {
        var holding = New(HoldingType.HerbGrove);
        var detail = Detail(HoldingType.HerbGrove);
        HoldingRules.GetDroidPercent(holding).Should().Be(0);

        holding.DroidItemId = "droid";
        holding.DroidIndustry = 50;
        HoldingRules.GetDroidPercent(holding).Should().Be(25, "+0.5% per Industry");
        holding.DroidIndustry = 100;
        holding.DroidEfficiencyPercent = 40;
        HoldingRules.GetDroidPercent(holding).Should().Be(70, "Efficiency Protocol II raises the Industry read");
        HoldingTuning.BestDroidPercent.Should().Be(70);

        var expected = (int)Math.Round(detail.BatchUnits * 1.7, MidpointRounding.AwayFromZero);
        HoldingRules.GetDailyUnits(holding, detail).Should().Be(expected);
        Advance(holding, 1);
        Units(holding, CommodityType.WitchHerbs).Should().Be(expected);
    }

    [Test]
    public void Droid_PostingIdsAreApartFromShipPostings()
    {
        var posted = HoldingRules.GetDroidPostingId("abc");
        HoldingRules.IsDroidPosting(posted).Should().BeTrue();
        HoldingRules.GetHoldingIdFromPosting(posted).Should().Be("abc");
        HoldingRules.IsDroidPosting(Guid.NewGuid().ToString()).Should().BeFalse("ship postings keep the ship id");
        HoldingRules.IsDroidPosting(string.Empty).Should().BeFalse();

        var droidCrew = Source(Path.Combine("Service", "SpaceDroidCrew.cs"));
        droidCrew.Should().Contain("HoldingRules.IsDroidPosting(shipId)", "a login check must not end a holding posting as a lost ship's");
        droidCrew.Should().Contain("HoldingRules.IsDroidPosting(Droid.GetPostedShipId(controller))");
    }

    // Events

    [Test]
    public void Events_RollEveryFewDaysWithContrabandOnlyCustoms()
    {
        var farm = Detail(HoldingType.GrainFarm);
        var lab = Detail(HoldingType.SpiceLab);
        HoldingRules.RollEvent(farm, 0.0).Should().Be(HoldingEventType.Boom);
        HoldingRules.RollEvent(farm, HoldingTuning.BoomChance).Should().Be(HoldingEventType.Setback);
        HoldingRules.RollEvent(farm, HoldingTuning.BoomChance + HoldingTuning.SetbackChance).Should().Be(HoldingEventType.Raid);
        var customs = HoldingTuning.BoomChance + HoldingTuning.SetbackChance + HoldingTuning.RaidChance;
        HoldingRules.RollEvent(farm, customs).Should().Be(HoldingEventType.None, "only contraband holdings are inspected");
        HoldingRules.RollEvent(lab, customs).Should().Be(HoldingEventType.Customs);
        HoldingRules.RollEvent(lab, 0.999).Should().Be(HoldingEventType.None);
        HoldingCatalog.Holdings.Where(x => x.CustomsInspections).Select(x => x.Type).Should()
            .BeEquivalentTo(new[] { HoldingType.SpiceLab, HoldingType.ExcavationDig });

        // One roll every EventIntervalDays: a boom on day 3 doubles that day's output.
        var holding = New(HoldingType.GrainFarm);
        Advance(holding, HoldingTuning.EventIntervalDays, new FixedRandom(0.0));
        Units(holding, CommodityType.Grain).Should().Be((HoldingTuning.EventIntervalDays + 1) * farm.BatchUnits);
        holding.Log.Should().ContainSingle(x => x.Contains("Boom"));
    }

    [Test]
    public void Events_ASetbackStopsOutputUntilTheRepairGoodsArrive()
    {
        var holding = New(HoldingType.MoistureFarm);
        var detail = Detail(HoldingType.MoistureFarm);
        var setback = new FixedRandom(HoldingTuning.BoomChance);

        var result = Advance(holding, HoldingTuning.EventIntervalDays, setback);
        holding.Setback.Should().Be(HoldingSetbackType.Sandstorm);
        holding.RepairUnitsNeeded.Should().Be(detail.RepairUnits);
        result.Events.Should().ContainSingle(x => x.StartsWith("Sandstorm"));
        Units(holding, CommodityType.PurifiedWater).Should().Be((HoldingTuning.EventIntervalDays - 1) * detail.BatchUnits);

        Advance(holding, 10, setback);
        Units(holding, CommodityType.PurifiedWater).Should().Be((HoldingTuning.EventIntervalDays - 1) * detail.BatchUnits, "no output and no new rolls");
        HoldingRules.GetNeeds(holding, detail).Should().Contain(x => x.Commodity == detail.RepairGood && x.Purpose.Contains("Repairs"));

        var repaired = new HoldingAdvanceResult();
        HoldingRules.Deliver(holding, detail, detail.RepairGood, detail.RepairUnits, Start.AddDays(10), repaired);
        holding.Setback.Should().Be(HoldingSetbackType.Invalid);
        repaired.Events.Should().ContainSingle(x => x.StartsWith("Repaired"));
        HoldingRules.GetSupply(holding, detail.RepairGood).Should().Be(0);

        Advance(holding, 11);
        Units(holding, CommodityType.PurifiedWater).Should().Be(HoldingTuning.EventIntervalDays * detail.BatchUnits);
    }

    [Test]
    public void Events_AnUndefendedRaidCarriesOffHalfTheStockWhenItsWindowRunsOut()
    {
        var warehouse = new Dictionary<CommodityType, int> { { CommodityType.Grain, 11 }, { CommodityType.RationPacks, 1 } };
        HoldingRules.Raid(warehouse).Should().Be(7);
        warehouse[CommodityType.Grain].Should().Be(5);
        warehouse.Should().NotContainKey(CommodityType.RationPacks);

        // W11o: the raiders hold the site; nothing is taken until the answer window runs out.
        var holding = New(HoldingType.GrainFarm);
        var raid = new FixedRandom(HoldingTuning.BoomChance + HoldingTuning.SetbackChance);
        var result = Advance(holding, HoldingTuning.EventIntervalDays, raid);
        var batch = Detail(HoldingType.GrainFarm).BatchUnits;
        holding.RaidPending.Should().BeTrue();
        holding.RaidDeadline.Should().Be(Start.AddDays(HoldingTuning.EventIntervalDays + HoldingTuning.RaidAnswerDays));
        result.Events.Should().Contain(x => x.Contains("raiders hold the site"));
        Units(holding, CommodityType.Grain).Should().Be(HoldingTuning.EventIntervalDays * batch);

        // No other event rolls while the raiders hold the site; at the deadline they take half.
        Advance(holding, HoldingTuning.EventIntervalDays + HoldingTuning.RaidAnswerDays, raid);
        holding.RaidPending.Should().BeFalse();
        var before = (HoldingTuning.EventIntervalDays + HoldingTuning.RaidAnswerDays - 1) * batch;
        Units(holding, CommodityType.Grain).Should().Be(before - (int)Math.Round(before * 0.5, MidpointRounding.AwayFromZero) + batch);
        holding.Log.Should().Contain(x => x.Contains("nobody drove the raiders off"));
    }

    [Test]
    public void Events_ARaidDrivenOffAtTheSiteKeepsTheStock()
    {
        var holding = New(HoldingType.GrainFarm);
        Advance(holding, HoldingTuning.EventIntervalDays, new FixedRandom(HoldingTuning.BoomChance + HoldingTuning.SetbackChance));
        var stock = Units(holding, CommodityType.Grain);

        var result = new HoldingAdvanceResult();
        HoldingRules.RepelRaid(holding, Start.AddDays(HoldingTuning.EventIntervalDays + 1), result).Should().BeTrue();
        holding.RaidPending.Should().BeFalse();
        result.Events.Should().ContainSingle().Which.Should().Contain("driven off");
        HoldingRules.RepelRaid(holding, Start, new HoldingAdvanceResult()).Should().BeFalse("no raid is pending");

        Advance(holding, HoldingTuning.EventIntervalDays + HoldingTuning.RaidAnswerDays);
        Units(holding, CommodityType.Grain).Should().BeGreaterThan(stock, "nothing was taken");
    }

    [TestCase(HoldingType.CrystalMine, 1)]
    [TestCase(HoldingType.KelpFishery, 2)]
    [TestCase(HoldingType.TimberCamp, 3)]
    [TestCase(HoldingType.MoistureFarm, 4)]
    [TestCase(HoldingType.GrainFarm, 5)]
    [TestCase(HoldingType.ExcavationDig, 6)]
    public void Raiders_AreTheDockTiersRoster(HoldingType type, int tier)
    {
        var raiders = HoldingRules.PlanRaiders(Detail(type), max => max - 1);
        raiders.Should().HaveCount(HoldingTuning.RaidersPerRaid);
        raiders.Should().OnlyContain(x => HoldingTuning.RaiderRosters[tier].Contains(x));
    }

    [Test]
    public void Raiders_AreExistingCreaturesWithTheirOwnLoot()
    {
        foreach (var resref in HoldingTuning.RaiderRosters.SelectMany(x => x).Distinct())
        {
            using var utc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "Module", "utc", resref + ".utc.json"), System.Text.Encoding.Latin1));
            utc.RootElement.GetProperty("VarTable").GetProperty("value").EnumerateArray()
                .Should().Contain(x => x.GetProperty("Name").GetProperty("value").GetString()!.StartsWith("LOOT_TABLE_"), resref);
        }

        var service = Source(Path.Combine("Service", "PlanetaryHoldings.cs"));
        service.Should().Contain("SpawnRaiders(holding, detail, site);").And.Contain("NWNEventHandler(ScriptName.OnCreatureDeathAfter)");
        service.Should().Contain("HoldingRules.RepelRaid(holding, DateTime.UtcNow, result)");
        service.Should().Contain("DeleteLocalInt(raider, QuestNpcGroupVariable)", "borrowed creatures give no quest credit");
        Source(Path.Combine("Feature", "GuiDefinition", "ViewModel", "PlanetaryHoldingsViewModel.cs")).Should().Contain("if (holding.RaidPending)");
    }

    [Test]
    public void Guests_ArePartyMembersJoiningTheOwnersOpenSiteAtItsDock()
    {
        HoldingRules.CanJoinAsGuest(true, true, true).Should().BeTrue();
        HoldingRules.CanJoinAsGuest(false, true, true).Should().BeFalse("not in the owner's party");
        HoldingRules.CanJoinAsGuest(true, false, true).Should().BeFalse("the owner has not opened the site");
        HoldingRules.CanJoinAsGuest(true, true, false).Should().BeFalse("another dock");

        Source(Path.Combine("Feature", "DialogDefinition", "StarportDialog.cs")).Should().Contain("PlanetaryHoldings.GetGuestSites(player, dockId)")
            .And.Contain("PlanetaryHoldings.JoinSite(player, holdingId, dockId)");
        Source(Path.Combine("Feature", "DialogDefinition", "HoldingSiteDialog.cs")).Should().Contain("PlanetaryHoldings.LoadHolding(player, holdingId)",
            "only the owner manages the holding at the terminal; guests can only leave");
    }

    [Test]
    public void CityAndStationStarports_SellTheirPlanetsHoldings()
    {
        HoldingRules.GetCityDockHoldingsExchange(TradeExchangeType.Tatooine, false).Should().Be(TradeExchangeType.Tatooine);
        HoldingRules.GetCityDockHoldingsExchange(TradeExchangeType.Hutlar, true).Should().Be(TradeExchangeType.Invalid, "a Wild Space station has no planet below");
        HoldingRules.GetCityDockHoldingsExchange(TradeExchangeType.Invalid, false).Should().Be(TradeExchangeType.Invalid);
        HoldingRules.IsCityDockId(Guid.NewGuid().ToString()).Should().BeTrue();
        HoldingRules.IsCityDockId("tatooine_dock").Should().BeFalse();

        var dialog = Source(Path.Combine("Feature", "DialogDefinition", "StarportDialog.cs"));
        dialog.Should().Contain("AddCityHoldingsResponse(page, player, starportId)")
            .And.Contain("new HoldingsPayload(exchange, starportId)")
            .And.Contain("PlanetaryHoldings.RehomeStrandedHoldings(player, exchange, landingAreaResref)");
        Source(Path.Combine("Service", "PlanetaryHoldings.cs")).Should().Contain("Trade.TryGetCityDock(holding.DockId) != null",
            "a holding whose starport is gone ships from its planet's NPC dock");
    }

    [Test]
    public void Events_CustomsFinesOrConfiscatesTheBannedStock()
    {
        var lab = Detail(HoldingType.SpiceLab);
        var customs = new FixedRandom(HoldingTuning.BoomChance + HoldingTuning.SetbackChance + HoldingTuning.RaidChance);

        var holding = New(HoldingType.SpiceLab);
        Advance(holding, HoldingTuning.EventIntervalDays, customs);
        var stock = (HoldingTuning.EventIntervalDays - 1) * lab.BatchUnits;
        holding.CustomsFine.Should().Be((int)Math.Round(stock * 250 * HoldingTuning.CustomsFineRate, MidpointRounding.AwayFromZero));
        holding.CustomsDeadline.Should().Be(Start.AddDays(HoldingTuning.EventIntervalDays + HoldingTuning.CustomsAnswerDays));

        // Unpaid by the deadline: the banned stock is confiscated before that day's batch.
        Advance(holding, HoldingTuning.EventIntervalDays + HoldingTuning.CustomsAnswerDays, customs);
        holding.CustomsFine.Should().Be(0);
        Units(holding, CommodityType.Moonspice).Should().Be(lab.BatchUnits);
        holding.Log.Should().Contain(x => x.Contains("confiscated"));

        HoldingRules.GetBannedValue(new Dictionary<CommodityType, int> { { CommodityType.Grain, 10 }, { CommodityType.SithRelics, 2 } })
            .Should().Be(2 * 782, "only restricted and contraband goods count");
    }

    [Test]
    public void Log_KeepsTheLatestEntries()
    {
        var holding = New(HoldingType.Apiary);
        for (var index = 0; index < 20; index++)
            HoldingRules.AddLog(holding, Start, $"entry {index}");

        holding.Log.Should().HaveCount(HoldingTuning.LogEntries);
        holding.Log.Last().Should().EndWith("entry 19");
    }

    // Holding goods at the exchanges

    [Test]
    public void HoldingGoods_AreBoughtButNeverSoldByExchanges()
    {
        var holdingGoods = TradeCatalog.Commodities.Where(x => x.IsHoldingGood).ToList();
        holdingGoods.Select(x => x.Type).Should().BeEquivalentTo(new[]
        {
            CommodityType.RationPacks, CommodityType.FocusingLenses, CommodityType.PrefabShelters, CommodityType.Glitterstim,
            CommodityType.SithHolocrons, CommodityType.AgedBanthaCheese,
        });

        foreach (var commodity in holdingGoods)
        {
            commodity.Producers.Should().BeEmpty();
            commodity.Consumers.Should().NotBeEmpty();
            foreach (var source in commodity.HoldingSources)
                HoldingCatalog.Holdings.Should().Contain(x => x.Exchange == source && x.Chain.Refined == commodity.Type);

            var consumer = TradeLookup.GetExchange(commodity.Consumers[0]).FindMarket(commodity.Type);
            TradeRules.PlanBuy(commodity, consumer, 0, 10, 10, 100, 100, 100_000).Refusal.Should().Be(TradeRefusal.HoldingsOnly);
            TradeRules.PlanSell(commodity, consumer, 0, 10, 10, 10).Refusal.Should().Be(TradeRefusal.None);
        }

        ConvoyRules.GetCandidateGoods("space_midrim1").Intersect(holdingGoods.Select(x => x.Type)).Should().BeEmpty("convoys carry exported goods");
        var board = HoloNetRules.Build((_, _) => 0, Start);
        board.Rows.Where(x => TradeLookup.GetCommodity(x.Commodity).IsHoldingGood).Should().OnlyContain(x => x.BuyAt == TradeExchangeType.Invalid);
    }

    /// <summary>
    /// The limit is per account (review 2026-10-30): every character on one public CD key counts, so more characters do
    /// not multiply the passive income. A deleted character's holdings no longer count.
    /// </summary>
    [Test]
    public void Buying_TheLimitIsPerAccount()
    {
        var farm = Detail(HoldingType.MoistureFarm);
        var mine = HoldingRules.Create("player", farm, "tatooine_dock", Start, "ACCOUNT1");
        mine.OwnerCDKey.Should().Be("ACCOUNT1");
        New(HoldingType.MoistureFarm).OwnerCDKey.Should().BeEmpty("older callers and records have no key until the owner's holdings load");

        var alt = HoldingRules.Create("alt", farm, "tatooine_dock", Start, "ACCOUNT1");
        var deleted = HoldingRules.Create("deleted", farm, "tatooine_dock", Start, "ACCOUNT1");
        bool Live(string playerId) => playerId != "deleted";
        var records = new[] { mine, HoldingRules.Create("player", farm, "x", Start, "ACCOUNT1"), alt, deleted };

        // The buyer's own records come from their loaded holdings and are never counted twice.
        var owned = HoldingRules.CountAccountHoldings("player", 2, records, Live);
        owned.Should().Be(3, "two on this character and one on another of the account's characters");
        HoldingRules.GetPurchaseRefusal(farm, TradeExchangeType.Tatooine, owned, farm.PurchasePrice).Should().Be(HoldingRefusal.AtLimit);

        HoldingRules.CountAccountHoldings("player", 2, new[] { mine, deleted }, Live).Should().Be(2, "a deleted character's holdings do not count");
        HoldingRules.CountAccountHoldings("alt", 1, records, Live).Should().Be(3, "the alt sees the same account total");
        HoldingRules.CountAccountHoldings("player", 1, null, Live).Should().Be(1);

        var service = Source(Path.Combine("Service", "PlanetaryHoldings.cs"));
        service.Should().Contain("var owned = CountAccountHoldings(player, LoadHoldings(player).Count);");
        service.Should().Contain("AddFieldSearch(nameof(PlanetaryHolding.OwnerCDKey), cdKey, false)");
        service.Should().Contain("GetPCPublicCDKey(player)");
        Source(Path.Combine("Feature", "GuiDefinition", "ViewModel", "PlanetaryHoldingsViewModel.cs"))
            .Should().Contain("PlanetaryHoldings.CountAccountHoldings(Player, _holdingIds.Count)");
    }

    // Income

    /// <summary>Daily net minutes of the dock tier's rate at level 3 with the best droid and a Processing Plant.</summary>
    private static double BestMinutes(HoldingDetail detail)
    {
        var rate = HoldingTuning.ReferenceRateByTier[TradeLookup.GetExchange(detail.Exchange).OrbitTier];
        var price = TradeLookup.GetCommodity(detail.Output).BasePrice;
        var units = HoldingRules.GetDailyUnits(detail, 3, HoldingTuning.BestDroidPercent);
        var (refined, used, input) = HoldingRules.Refine(detail.Chain, HoldingRules.GetPlantUnits(units), int.MaxValue);
        var value = (units - used) * price + refined * TradeLookup.GetCommodity(detail.Chain.Refined).BasePrice -
                    input * TradeLookup.GetCommodity(detail.Chain.Input).BasePrice -
                    HoldingRules.GetDailyUpkeep(detail, 3, HoldingTuning.BestDroidPercent);
        return value * 60.0 / rate;
    }

    /// <summary>
    /// An account's best legal trio, event-free, stays within the 90-minute passive cap (review 2026-10-30): three of
    /// the best type and the three best types.
    /// </summary>
    [Test]
    public void Income_AnAccountsBestThreeHoldingsStayWithinThePassiveCap()
    {
        HoldingTuning.MaximumAccountMinutes.Should().Be(90);
        var best = HoldingCatalog.Holdings.Select(BestMinutes).OrderByDescending(x => x).ToList();
        (3 * best[0]).Should().BeLessThanOrEqualTo(HoldingTuning.MaximumAccountMinutes + 1e-6, "three holdings of the best type");
        best.Take(3).Sum().Should().BeLessThanOrEqualTo(HoldingTuning.MaximumAccountMinutes + 1e-6, "the three best types");
    }

    [Test]
    public void Income_IsAModestPassiveIncomeAtTheDockTier()
    {
        foreach (var detail in HoldingCatalog.Holdings)
        {
            var rate = HoldingTuning.ReferenceRateByTier[TradeLookup.GetExchange(detail.Exchange).OrbitTier];
            var price = TradeLookup.GetCommodity(detail.Output).BasePrice;
            double Minutes(int level, int droidPercent) =>
                (HoldingRules.GetDailyUnits(detail, level, droidPercent) * price - HoldingRules.GetDailyUpkeep(detail, level, droidPercent)) * 60.0 / rate;

            Minutes(1, 0).Should().BeInRange(8, 12, $"{detail.Name} at level 1 nets about 10 minutes of active play a day");
            Minutes(3, 0).Should().BeInRange(13, 18, detail.Name);
            Minutes(3, HoldingTuning.BestDroidPercent).Should().BeInRange(22, HoldingTuning.MaximumBestDroidMinutes,
                $"{detail.Name} with the best droid nets about 25 minutes");

            // With a Processing Plant, buying its input at base price: about 30 minutes, so three holdings pay about 90.
            var units = HoldingRules.GetDailyUnits(detail, 3, HoldingTuning.BestDroidPercent);
            var (refined, used, input) = HoldingRules.Refine(detail.Chain, HoldingRules.GetPlantUnits(units), int.MaxValue);
            var value = (units - used) * price + refined * TradeLookup.GetCommodity(detail.Chain.Refined).BasePrice -
                        input * TradeLookup.GetCommodity(detail.Chain.Input).BasePrice -
                        HoldingRules.GetDailyUpkeep(detail, 3, HoldingTuning.BestDroidPercent);
            (value * 60.0 / rate).Should().BeInRange(25, HoldingTuning.MaximumPlantMinutes, $"{detail.Name} at its best");
            detail.PurchasePrice.Should().Be((int)(Math.Round(detail.BatchUnits * price * 10 / 100.0, MidpointRounding.AwayFromZero) * 100));
        }
    }

    // Wiring

    [Test]
    public void Wiring_DockMasterWindowAndSite()
    {
        ((int)GuiWindowType.PlanetaryHoldings).Should().Be(104);
        Source(Path.Combine("Feature", "DialogDefinition", "StarportDialog.cs")).Should().Contain("\"Planetary Holdings\"");
        Source(Path.Combine("Feature", "DialogDefinition", "HoldingSiteDialog.cs")).Should().Contain("PlanetaryHoldings.LeaveSite");
        var service = Source(Path.Combine("Service", "PlanetaryHoldings.cs"));
        service.Should().Contain("NWNEventHandler(ScriptName.OnModuleEnter)");
        service.Should().Contain("NWNEventHandler(ScriptName.OnModuleUnacquire)");
        service.Should().Contain("CargoHoldRules.AddUnits(ship.Cargo");
    }
}
