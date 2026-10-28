using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.TradeDefinition;
using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The trade core (plan section 13, W7a): commodities, the cargo bay, exchange pricing with flooding and recovery,
/// the arbitrage bound, trade XP and the cargo spill. The numbers come from tools/SpaceRevampBalance/trade.py.
/// </summary>
public class TradeTests
{
    private static readonly DateTime Now = new(2026, 10, 27, 12, 0, 0, DateTimeKind.Utc);

    private static CommodityDetail Commodity(CommodityType type) => TradeLookup.GetCommodity(type);

    private static TradeMarketDetail Market(TradeExchangeType exchange, CommodityType commodity) =>
        TradeLookup.GetExchange(exchange).FindMarket(commodity);

    // Catalog

    [Test]
    public void Catalog_EveryCommodityIsPricedAndMadeSomewhere()
    {
        TradeCatalog.Commodities.Select(x => x.Type).Should().OnlyHaveUniqueItems();
        TradeCatalog.Commodities.Select(x => x.Type).Should()
            .BeEquivalentTo(Enum.GetValues<CommodityType>().Where(x => x != CommodityType.Invalid));

        foreach (var commodity in TradeCatalog.Commodities)
        {
            commodity.BasePrice.Should().BePositive(commodity.Name);
            commodity.Volume.Should().BePositive(commodity.Name);
            (commodity.IsHoldingGood ? commodity.HoldingSources : commodity.Producers).Should()
                .NotBeEmpty($"{commodity.Name} needs a dock that exports it, or holdings that refine it");
            commodity.Name.Should().MatchRegex("^[A-Za-z' ]+$");
        }

        TradeCatalog.Commodities.Select(x => x.Legality).Distinct().Should()
            .BeEquivalentTo(new[] { CommodityLegality.Legal, CommodityLegality.Restricted, CommodityLegality.Contraband });
    }

    [Test]
    public void Catalog_MarketRolesMatchTheProducerAndConsumerLists()
    {
        foreach (var commodity in TradeCatalog.Commodities)
        {
            foreach (var exchange in TradeCatalog.Exchanges)
            {
                var market = exchange.FindMarket(commodity.Type);
                if (commodity.Producers.Contains(exchange.Type))
                    market!.Role.Should().Be(CommodityRole.Producer);
                else if (commodity.Consumers.Contains(exchange.Type))
                    market!.Role.Should().Be(CommodityRole.Consumer);
                else if (market != null)
                    market.Role.Should().Be(CommodityRole.Neutral);
            }
        }
    }

    [Test]
    public void Catalog_ExportsAreCheapestAndImportsPayMost()
    {
        foreach (var exchange in TradeCatalog.Exchanges)
        {
            exchange.Markets.Select(x => x.Commodity).Should().OnlyHaveUniqueItems(exchange.Name);
            foreach (var market in exchange.Markets)
            {
                market.Depth.Should().BePositive();
                if (TradeLookup.GetCommodity(market.Commodity).IsHoldingGood)
                    continue;

                var producer = TradeCatalog.Exchanges
                    .Select(x => x.FindMarket(market.Commodity))
                    .Where(x => x is { Role: CommodityRole.Producer })
                    .Min(x => x!.PriceFactor);
                if (market.Role != CommodityRole.Producer)
                    market.PriceFactor.Should().BeGreaterThan(producer, $"{exchange.Name} {market.Commodity}");
            }
        }
    }

    [Test]
    public void Catalog_LawfulDocksTradeBannedGoodsOnlyWhenTheyMakeOrNeedThem()
    {
        foreach (var exchange in TradeCatalog.Exchanges.Where(x => x.Authority != TradeAuthority.None))
        {
            foreach (var commodity in TradeCatalog.Commodities.Where(x => x.BannedBy.Contains(exchange.Authority)))
            {
                var market = exchange.FindMarket(commodity.Type);
                if (market != null)
                    market.Role.Should().NotBe(CommodityRole.Neutral, $"{exchange.Name} bans {commodity.Name}");
            }
        }

        Market(TradeExchangeType.Viscara, CommodityType.Moonspice).Should().BeNull();
        Market(TradeExchangeType.Hutlar, CommodityType.Moonspice).Should().NotBeNull("open ports trade contraband");
        Market(TradeExchangeType.Korriban, CommodityType.SithRelics)!.Role.Should().Be(CommodityRole.Producer);
    }

    [Test]
    public void Catalog_EveryPlanetWithAStarportHasAnExchange()
    {
        foreach (var planet in Enum.GetValues<PlanetType>().Where(x => x != PlanetType.Invalid))
            TradeLookup.GetExchangeForPlanet(planet).Should().NotBe(TradeExchangeType.Invalid, planet.ToString());

        TradeLookup.GetExchangeForPlanet(PlanetType.SmugglersMoonStation).Should().Be(TradeExchangeType.SmugglersMoon);
        TradeLookup.GetExchangeForPlanet(PlanetType.Invalid).Should().Be(TradeExchangeType.Invalid);
    }

    // Cargo bay

    [Test]
    public void CargoBay_CountsVolumeNotUnits()
    {
        var cargo = new Dictionary<CommodityType, CargoLot>();
        CargoHoldRules.AddUnits(cargo, CommodityType.Grain, 10, 600);       // volume 2
        CargoHoldRules.AddUnits(cargo, CommodityType.KinrathSilk, 5, 500);  // volume 1

        CargoHoldRules.GetUsedVolume(cargo).Should().Be(25);
        CargoHoldRules.GetFreeVolume(40, cargo).Should().Be(15);
        CargoHoldRules.GetFreeVolume(20, cargo).Should().Be(0);
        CargoHoldRules.GetUnitsThatFit(15, CommodityType.Grain).Should().Be(7);
        CargoHoldRules.GetUnitsThatFit(15, CommodityType.SithRelics).Should().Be(2);
        CargoHoldRules.GetUnits(cargo, CommodityType.Grain).Should().Be(10);
        CargoHoldRules.GetLines(cargo).Should().Equal(("Grain", 10), ("Kinrath Silk", 5));
    }

    [Test]
    public void CargoBay_RemovingUnitsTakesTheirShareOfTheCost()
    {
        var cargo = new Dictionary<CommodityType, CargoLot>();
        CargoHoldRules.AddUnits(cargo, CommodityType.Seafood, 10, 400);
        CargoHoldRules.AddUnits(cargo, CommodityType.Seafood, 10, 600);

        CargoHoldRules.RemoveUnits(cargo, CommodityType.Seafood, 5).Should().Be(250);
        cargo[CommodityType.Seafood].Units.Should().Be(15);
        cargo[CommodityType.Seafood].CostBasis.Should().Be(750);
        CargoHoldRules.RemoveUnits(cargo, CommodityType.Seafood, 50).Should().Be(750);
        cargo.Should().NotContainKey(CommodityType.Seafood);
        CargoHoldRules.RemoveUnits(cargo, CommodityType.Seafood, 1).Should().Be(0);
    }

    [Test]
    public void CargoBay_OlderShipRecordsLoadEmpty()
    {
        new PlayerShip().Cargo.Should().BeEmpty();
        JsonConvert.DeserializeObject<PlayerShip>("{\"OwnerPlayerId\":\"abc\"}")!.Cargo.Should().NotBeNull().And.BeEmpty();

        var ship = new PlayerShip();
        CargoHoldRules.AddUnits(ship.Cargo, CommodityType.Grain, 3, 150);
        var loaded = JsonConvert.DeserializeObject<PlayerShip>(JsonConvert.SerializeObject(ship))!;
        loaded.Cargo[CommodityType.Grain].Units.Should().Be(3);
        loaded.Cargo[CommodityType.Grain].CostBasis.Should().Be(150);
    }

    // Buying and selling

    [Test]
    public void Buy_TakesWhatFitsAndWhatIsAffordable()
    {
        var grain = Commodity(CommodityType.Grain);
        var market = Market(TradeExchangeType.Dantooine, CommodityType.Grain);

        var full = TradeRules.PlanBuy(grain, market, 0, 10, 10, 100, 100, 1_000_000);
        full.Refusal.Should().Be(TradeRefusal.None);
        full.Units.Should().Be(10);
        full.WasReduced.Should().BeFalse();
        full.Credits.Should().Be(TradePricing.GetBuyCost(grain.BasePrice, market, 0, 10, 10));

        var room = TradeRules.PlanBuy(grain, market, 0, 10, 100, 100, 21, 1_000_000);
        room.Units.Should().Be(10, "grain fills 2 volume a unit");
        room.WasReduced.Should().BeTrue();

        var threeUnits = TradePricing.GetBuyCost(grain.BasePrice, market, 0, 3, 10);
        var credits = TradeRules.PlanBuy(grain, market, 0, 10, 100, 1000, 1000, threeUnits);
        credits.Units.Should().Be(3);
        credits.Credits.Should().Be(threeUnits);
        TradeRules.PlanBuy(grain, market, 0, 10, 100, 1000, 1000, threeUnits - 1).Units.Should().Be(2);
    }

    [Test]
    public void Buy_RefusesWithAReason()
    {
        var grain = Commodity(CommodityType.Grain);
        var market = Market(TradeExchangeType.Dantooine, CommodityType.Grain);

        TradeRules.PlanBuy(grain, market, 0, 10, 0, 100, 100, 1000).Refusal.Should().Be(TradeRefusal.InvalidAmount);
        TradeRules.PlanBuy(grain, null, 0, 10, 1, 100, 100, 1000).Refusal.Should().Be(TradeRefusal.NotTradedHere);
        TradeRules.PlanBuy(grain, market, 0, 10, 1, 0, 0, 1000).Refusal.Should().Be(TradeRefusal.NoCargoBay);
        TradeRules.PlanBuy(grain, market, 0, 10, 1, 100, 1, 1000).Refusal.Should().Be(TradeRefusal.CargoBayFull);
        TradeRules.PlanBuy(grain, market, 0, 10, 1, 100, 100, 1).Refusal.Should().Be(TradeRefusal.NotEnoughCredits);

        foreach (var refusal in Enum.GetValues<TradeRefusal>().Where(x => x != TradeRefusal.None))
            TradeRules.GetRefusalMessage(refusal).Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public void Sell_SellsWhatIsAboard()
    {
        var grain = Commodity(CommodityType.Grain);
        var market = Market(TradeExchangeType.Tatooine, CommodityType.Grain);

        var order = TradeRules.PlanSell(grain, market, 0, 10, 50, 20);
        order.Units.Should().Be(20);
        order.WasReduced.Should().BeTrue();
        order.Credits.Should().Be(TradePricing.GetSellRevenue(grain.BasePrice, market, 0, 20, 10));
        TradeRules.PlanSell(grain, market, 0, 10, 5, 0).Refusal.Should().Be(TradeRefusal.NoneAboard);
    }

    // Flooding and recovery

    [Test]
    public void Selling_FloodsThePriceAndBuyingPushesItUp()
    {
        var silk = Commodity(CommodityType.KinrathSilk);
        var market = Market(TradeExchangeType.MonCala, CommodityType.KinrathSilk);

        var fresh = TradePricing.GetQuotedSellPrice(silk.BasePrice, market, 0, 10);
        TradePricing.GetQuotedSellPrice(silk.BasePrice, market, 500, 10).Should().BeLessThan(fresh);
        TradePricing.GetQuotedBuyPrice(silk.BasePrice, market, -500, 10)
            .Should().BeGreaterThan(TradePricing.GetQuotedBuyPrice(silk.BasePrice, market, 0, 10));

        // Each unit of a big sale fetches less than the last.
        TradePricing.GetSellRevenue(silk.BasePrice, market, 0, 1000, 10).Should().BeLessThan(fresh * 1000);
        TradePricing.GetBuyCost(silk.BasePrice, market, 0, 1000, 10)
            .Should().BeGreaterThan(TradePricing.GetQuotedBuyPrice(silk.BasePrice, market, 0, 10) * 990);
    }

    [Test]
    public void Flooding_StaysInsideItsLimits()
    {
        TradePricing.GetPressureMultiplier(0, 100).Should().Be(1);
        TradePricing.GetPressureMultiplier(1_000_000, 100).Should().Be(TradeTuning.FloodMinimum);
        TradePricing.GetPressureMultiplier(-1_000_000, 100).Should().Be(TradeTuning.FloodMaximum);
        TradePricing.GetPressureMultiplier(100, 100).Should().BeApproximately(Math.Exp(-1), 1e-12);
    }

    [Test]
    public void Pressure_RecoversWithAOneHourHalfLife()
    {
        TradePricing.Recover(400, TimeSpan.FromHours(1)).Should().BeApproximately(200, 1e-9);
        TradePricing.Recover(-400, TimeSpan.FromHours(2)).Should().BeApproximately(-100, 1e-9);
        TradePricing.Recover(400, TimeSpan.Zero).Should().Be(400);
        TradePricing.Recover(400, TimeSpan.FromDays(2)).Should().Be(0);

        var state = new TradeMarketState { Pressure = 300, UpdatedAt = Now };
        TradePricing.Advance(state, Now.AddHours(1));
        state.Pressure.Should().BeApproximately(150, 1e-9);
        state.UpdatedAt.Should().Be(Now.AddHours(1));

        var fresh = new TradeMarketState();
        TradePricing.Advance(fresh, Now);
        fresh.UpdatedAt.Should().Be(Now);
        fresh.Pressure.Should().Be(0);
    }

    [Test]
    public void SplittingAnOrder_DoesNotChangeItsPrice()
    {
        var silk = Commodity(CommodityType.KinrathSilk);
        var market = Market(TradeExchangeType.MonCala, CommodityType.KinrathSilk);

        var whole = TradePricing.GetSellRevenue(silk.BasePrice, market, 0, 200, 10);
        var halves = TradePricing.GetSellRevenue(silk.BasePrice, market, 0, 100, 10) +
                     TradePricing.GetSellRevenue(silk.BasePrice, market, 100, 100, 10);
        halves.Should().BeInRange(whole - 1, whole + 1);
    }

    [Test]
    public void Social_ImprovesPricesSlightly()
    {
        TradePricing.GetSocialBonus(10).Should().Be(0);
        TradePricing.GetSocialBonus(8).Should().Be(0);
        TradePricing.GetSocialBonus(20).Should().BeApproximately(0.01, 1e-12);
        TradePricing.GetSocialBonus(26).Should().Be(TradeTuning.SocialMaximum);

        var silk = Commodity(CommodityType.KinrathSilk);
        var market = Market(TradeExchangeType.MonCala, CommodityType.KinrathSilk);
        TradePricing.GetUnitSellPrice(silk.BasePrice, market, 0, 26).Should()
            .BeGreaterThan(TradePricing.GetUnitSellPrice(silk.BasePrice, market, 0, 10));
        TradePricing.GetUnitBuyPrice(silk.BasePrice, market, 0, 26).Should()
            .BeLessThan(TradePricing.GetUnitBuyPrice(silk.BasePrice, market, 0, 10));
    }

    // Arbitrage bound

    [Test]
    public void BuyingAndSellingBackAtOneExchange_AlwaysLoses()
    {
        foreach (var exchange in TradeCatalog.Exchanges)
        {
            foreach (var market in exchange.Markets)
            {
                var basePrice = Commodity(market.Commodity).BasePrice;
                foreach (var units in new[] { 1, 50, 500 })
                {
                    var cost = TradePricing.GetBuyCost(basePrice, market, 0, units, 26);
                    var back = TradePricing.GetSellRevenue(basePrice, market, -units, units, 26);
                    back.Should().BeLessThan(cost, $"{exchange.Name} {market.Commodity} x{units}");
                }
            }
        }
    }

    [Test]
    public void RepeatedRoundTrips_EarnLessEachTimeAndStopPaying()
    {
        // The best legal pair: Viscara silk to Mon Cala, with no time for the markets to recover.
        var silk = Commodity(CommodityType.KinrathSilk);
        var source = Market(TradeExchangeType.Viscara, CommodityType.KinrathSilk);
        var target = Market(TradeExchangeType.MonCala, CommodityType.KinrathSilk);
        double sourcePressure = 0, targetPressure = 0;
        var profits = new List<int>();
        const int load = 100;

        for (var trip = 0; trip < 60; trip++)
        {
            var cost = TradePricing.GetBuyCost(silk.BasePrice, source, sourcePressure, load, 26);
            var revenue = TradePricing.GetSellRevenue(silk.BasePrice, target, targetPressure, load, 26);
            profits.Add(revenue - cost);
            sourcePressure -= load;
            targetPressure += load;
        }

        profits.First().Should().BePositive("a fresh pair pays");
        profits.Should().BeInDescendingOrder();
        profits.Last().Should().BeNegative("flooding ends the arbitrage");
        profits.Where(x => x > 0).Sum().Should().BeLessThan(silk.BasePrice * 1000,
            "the profit one pair can give before it floods is bounded by its depth");
    }

    // Trade XP

    [Test]
    public void TradeXp_FollowsProfitUpToACap()
    {
        TradeRules.GetSaleXp(-100).Should().Be(0);
        TradeRules.GetSaleXp(0).Should().Be(0);
        TradeRules.GetSaleXp(250).Should().Be(250);
        TradeRules.GetSaleXp(1_000_000).Should().Be(TradeTuning.XpMaximumPerSale);
    }

    // Cargo spill

    [Test]
    public void Spill_PutsEachCommodityInAPodLargestFirst()
    {
        var cargo = new Dictionary<CommodityType, CargoLot>
        {
            [CommodityType.Grain] = new() { Units = 40 },
            [CommodityType.Seafood] = new() { Units = 12 },
        };

        var pods = CargoSpillRules.Split(cargo, TradeTuning.SpillMaximumContainers);
        pods.Should().HaveCount(2);
        pods[0].Units.Should().Equal(new Dictionary<CommodityType, int> { [CommodityType.Grain] = 40 });
        pods[1].Units.Should().Equal(new Dictionary<CommodityType, int> { [CommodityType.Seafood] = 12 });
        CargoSpillRules.GetPodName(pods[0].Units).Should().Be("Cargo Pod: 40 Grain");
        CargoSpillRules.Split(new Dictionary<CommodityType, CargoLot>(), 6).Should().BeEmpty();
    }

    [Test]
    public void Spill_KeepsEveryUnitWhenThereAreMoreCommoditiesThanPods()
    {
        var cargo = TradeCatalog.Commodities.Take(10).ToDictionary(x => x.Type, x => new CargoLot { Units = 5 + (int)x.Type });

        var pods = CargoSpillRules.Split(cargo, 6);
        pods.Should().HaveCount(6);
        pods.SelectMany(x => x.Units).Sum(x => x.Value).Should().Be(cargo.Values.Sum(x => x.Units));
        pods.SelectMany(x => x.Units.Keys).Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void Scoop_TakesWhatFitsAndLeavesTheRest()
    {
        var pod = new Dictionary<CommodityType, int> { [CommodityType.Grain] = 10, [CommodityType.Seafood] = 4 };

        var taken = CargoSpillRules.Scoop(pod, 15);
        taken.Should().Equal(new Dictionary<CommodityType, int> { [CommodityType.Grain] = 7, [CommodityType.Seafood] = 1 });
        pod.Should().Equal(new Dictionary<CommodityType, int> { [CommodityType.Grain] = 3, [CommodityType.Seafood] = 3 });

        CargoSpillRules.Scoop(pod, 0).Should().BeEmpty();
        CargoSpillRules.Scoop(pod, 100).Should().HaveCount(2);
        pod.Should().BeEmpty();
    }

    // Wiring

    [Test]
    public void Wiring_DeathSpillsTheCargoAndTheDockMasterOpensTheExchange()
    {
        var death = ReadServerSource("Service", "SpaceDeath.cs");
        death.Should().Contain("Trade.SpillCargo(dbPlayerShip, deathLocation);");
        death.IndexOf("Trade.SpillCargo(", StringComparison.Ordinal)
            .Should().BeLessThan(death.IndexOf("DB.Set(dbPlayerShip);", StringComparison.Ordinal));

        var spill = ReadServerSource("Service", "TradeCargoSpill.cs");
        spill.Should().Contain("[NWNEventHandler(ScriptName.OnCargoPodUsed)]");
        spill.Should().Contain("SetEventScript(pod, EventScript.Placeable_OnUsed, ScriptName.OnCargoPodUsed);");

        var dialog = ReadServerSource("Feature", "DialogDefinition", "StarportDialog.cs");
        dialog.Should().Contain("GuiWindowType.TradeExchange");
        dialog.Should().Contain("StarshipDockReturn.ShipOnlyLocationVariable");

        ReadServerSource("Feature", "GuiDefinition", "ViewModel", "ShipManagementViewModel.cs")
            .Should().Contain("if (dbShip.Cargo.Count > 0)");

        var pod = File.ReadAllText(Path.Combine(RepositoryRoot(), "Module", "utp", "spc_cargo_pod.utp.json"));
        pod.Should().Contain("\"spc_cargo_pod\"");
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        return directory!.FullName;
    }

    private static string ReadServerSource(params string[] path)
    {
        var full = Path.Combine(new[] { RepositoryRoot(), "SWLOR.Game.Server" }.Concat(path).ToArray());
        return File.ReadAllText(full).Replace("\r\n", "\n");
    }
}
