using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.TradeDefinition;
using SWLOR.Game.Server.Service.IndustryService;
using SWLOR.Game.Server.Service.StationService;
using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Shadowport and the lawful port at a city door of the Trade Exchange (Player City Revamp, "Edict catalog"): what a lawful door buys
/// as contraband and at what price, the 15% on normal goods, the bust chance, and the bounds the space revamp's exchange contract
/// sets on a city feature. The owner approved the prices and chances on 2026-11-02.
/// </summary>
public class ShadowportTests
{
    private const int MaximumSocial = 25;
    private const double MaximumStandingBonus = 0.0075;

    private static IEnumerable<TradeExchangeDetail> LawfulExchanges => TradeCatalog.Exchanges.Where(x => x.Authority != TradeAuthority.None);

    private static IEnumerable<TradeExchangeDetail> OpenExchanges => TradeCatalog.Exchanges.Where(x => x.Authority == TradeAuthority.None);

    private static IEnumerable<TradeExchangeDetail> BlackMarkets => TradeCatalog.Exchanges.Where(x => x.IsBlackMarket);

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

    private static string Source(params string[] relative) =>
        File.ReadAllText(Path.Combine(new[] { Root, "SWLOR.Game.Server" }.Concat(relative).ToArray()));

    private static string Method(string source, string signature)
    {
        var start = source.IndexOf(signature, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0, $"{signature} should exist");
        var open = source.IndexOf('{', start);
        var depth = 0;
        for (var index = open; index < source.Length; index++)
        {
            if (source[index] == '{')
                depth++;
            else if (source[index] == '}' && --depth == 0)
                return source[start..(index + 1)];
        }

        throw new InvalidOperationException($"{signature} has no closing brace.");
    }

    private static double Sell(CommodityType commodity, TradeMarketDetail market, int social = 10, double standing = 0) =>
        TradePricing.GetUnitSellPrice(TradeLookup.GetCommodity(commodity).BasePrice, market, 0, social, standing);

    // The stance

    [Test]
    public void ALawfulExchange_IsALawfulPort_AndWithTheEdictAShadowport()
    {
        foreach (var authority in new[] { TradeAuthority.Republic, TradeAuthority.Czerka, TradeAuthority.Sith })
        {
            ShadowportRules.GetStance(authority, false).Should().Be(ShadowportStance.Lawful);
            ShadowportRules.GetStance(authority, true).Should().Be(ShadowportStance.Shadowport);
        }
    }

    [Test]
    public void AnOpenPort_KeepsItsMarketsRules_WhateverTheEdict()
    {
        OpenExchanges.Select(x => x.Type).Should().BeEquivalentTo(new[]
        {
            TradeExchangeType.Hutlar, TradeExchangeType.Tatooine, TradeExchangeType.SmugglersMoon,
            TradeExchangeType.Dathomir, TradeExchangeType.Eshan,
        });

        foreach (var exchange in OpenExchanges)
        {
            ShadowportRules.GetStance(exchange.Authority, false).Should().Be(ShadowportStance.OpenPort);
            ShadowportRules.GetStance(exchange.Authority, true).Should().Be(ShadowportStance.OpenPort);
            ShadowportRules.GetContrabandCommodities(exchange, ShadowportStance.Shadowport).Should().BeEmpty(exchange.Name);
            ShadowportRules.GetNormalGoodsSaleFactor(ShadowportStance.OpenPort).Should().Be(1.0);
            ShadowportRules.GetBustChance(60000, 0, 0, ShadowportStance.OpenPort).Should().Be(0.0);
            ShadowportRules.GetContrabandMarket(CommodityType.Moonspice, ShadowportStance.OpenPort).Should().BeNull();
        }
    }

    // What a lawful door buys as contraband

    [Test]
    public void ALawfulDoor_BuysTheGoodsItsAuthorityBansThatItsMarketDoesNotTrade()
    {
        foreach (var exchange in LawfulExchanges)
        {
            var expected = TradeCatalog.Commodities
                .Where(x => x.BannedBy.Contains(exchange.Authority) && exchange.FindMarket(x.Type) == null)
                .Select(x => x.Type)
                .ToList();

            ShadowportRules.GetContrabandCommodities(exchange, ShadowportStance.Lawful).Should().BeEquivalentTo(expected, exchange.Name);
            ShadowportRules.GetContrabandCommodities(exchange, ShadowportStance.Shadowport).Should().BeEquivalentTo(expected, exchange.Name);
        }

        var viscara = TradeLookup.GetExchange(TradeExchangeType.Viscara);
        ShadowportRules.GetContrabandCommodities(viscara, ShadowportStance.Lawful)
            .Should().Contain(new[] { CommodityType.Moonspice, CommodityType.EncryptedDataChips, CommodityType.SithRelics, CommodityType.WeaponsCrates });

        // Korriban's Sith authority bans only the spice trade, and it makes Sith relics itself.
        var korriban = TradeLookup.GetExchange(TradeExchangeType.Korriban);
        ShadowportRules.GetContrabandCommodities(korriban, ShadowportStance.Lawful)
            .Should().Contain(CommodityType.Moonspice).And.NotContain(CommodityType.SithRelics);
    }

    [Test]
    public void ALawfulDoorNeverSellsContraband_SoTheSharedMarketKeepsItsGoods()
    {
        foreach (var exchange in LawfulExchanges)
        {
            foreach (var commodity in ShadowportRules.GetContrabandCommodities(exchange, ShadowportStance.Shadowport))
            {
                exchange.FindMarket(commodity).Should().BeNull($"{exchange.Name} does not trade {commodity}");
                ShadowportRules.IsContrabandSale(exchange, commodity, ShadowportStance.Shadowport).Should().BeTrue();
            }

            foreach (var market in exchange.Markets)
            {
                ShadowportRules.IsContrabandSale(exchange, market.Commodity, ShadowportStance.Shadowport)
                    .Should().BeFalse($"{exchange.Name} trades {market.Commodity} itself");
            }
        }
    }

    // Prices

    [Test]
    public void TheBlackMarketReference_IsTheLowestPriceABlackMarketPaysThatDoesNotMakeTheGood()
    {
        foreach (var commodity in TradeCatalog.Commodities.Where(x => x.BannedBy.Length > 0))
        {
            var prices = BlackMarkets
                .Where(x => !commodity.Producers.Contains(x.Type) && !commodity.HoldingSources.Contains(x.Type))
                .Select(x => x.FindMarket(commodity.Type))
                .Where(x => x != null && x.Role != CommodityRole.Producer)
                .ToList();

            var reference = ShadowportRules.GetBlackMarketReference(commodity.Type);
            reference.Should().NotBeNull(commodity.Name);
            reference.PriceFactor.Should().Be(prices.Min(x => x.PriceFactor), commodity.Name);
        }
    }

    [Test]
    public void ShadowportPrice_IsCloseToButNeverAboveTheBlackMarketPrice()
    {
        foreach (var exchange in LawfulExchanges)
        {
            foreach (var commodity in ShadowportRules.GetContrabandCommodities(exchange, ShadowportStance.Shadowport))
            {
                var shadowport = ShadowportRules.GetContrabandMarket(commodity, ShadowportStance.Shadowport);
                var reference = ShadowportRules.GetBlackMarketReference(commodity);

                // The most a Shadowport can pay (best Social and standing) against the least a black market pays (none of either).
                var best = Sell(commodity, shadowport, MaximumSocial, MaximumStandingBonus);
                var blackMarket = Sell(commodity, reference);
                best.Should().BeLessThan(blackMarket, $"{exchange.Name} {commodity}");
                (best / blackMarket).Should().BeGreaterThan(0.85, $"{exchange.Name} {commodity} should be close to the black market");

                // Every black market that pays a premium for it (not one that makes the good itself).
                var detail = TradeLookup.GetCommodity(commodity);
                foreach (var market in BlackMarkets
                             .Where(x => !detail.Producers.Contains(x.Type) && !detail.HoldingSources.Contains(x.Type))
                             .Select(x => x.FindMarket(commodity))
                             .Where(x => x != null && x.Role != CommodityRole.Producer))
                {
                    best.Should().BeLessThan(Sell(commodity, market), $"{exchange.Name} {commodity}");
                }
            }
        }
    }

    [Test]
    public void ALawfulPortPaysLessThanAShadowport_AndLessThanTheExporterAsksSoItCannotBeUsedForProfit()
    {
        foreach (var exchange in LawfulExchanges)
        {
            foreach (var commodity in ShadowportRules.GetContrabandCommodities(exchange, ShadowportStance.Lawful))
            {
                var lawful = ShadowportRules.GetContrabandMarket(commodity, ShadowportStance.Lawful);
                var shadowport = ShadowportRules.GetContrabandMarket(commodity, ShadowportStance.Shadowport);
                Sell(commodity, lawful).Should().BeLessThan(Sell(commodity, shadowport));

                var detail = TradeLookup.GetCommodity(commodity);
                var cheapestExporter = detail.Producers
                    .Select(x => TradeLookup.GetExchange(x).FindMarket(commodity))
                    .Where(x => x != null)
                    .Select(x => TradePricing.GetUnitBuyPrice(detail.BasePrice, x, 0, MaximumSocial, MaximumStandingBonus))
                    .DefaultIfEmpty(double.MaxValue)
                    .Min();
                Sell(commodity, lawful, MaximumSocial, MaximumStandingBonus).Should().BeLessThan(cheapestExporter, $"{exchange.Name} {commodity}");
            }
        }
    }

    [Test]
    public void ContrabandMarkets_ShareTheirDepthWithTheBlackMarketAndAreSaleOnly()
    {
        foreach (var commodity in TradeCatalog.Commodities.Where(x => x.BannedBy.Length > 0))
        {
            var market = ShadowportRules.GetContrabandMarket(commodity.Type, ShadowportStance.Shadowport);
            market.Depth.Should().Be(ShadowportRules.GetBlackMarketReference(commodity.Type).Depth);
            market.Commodity.Should().Be(commodity.Type);
            market.Role.Should().Be(CommodityRole.Consumer);
        }
    }

    [Test]
    public void SellingContrabandFloodsThePort_SoTheNextSaleFetchesLess_AndTheTariffIsTakenFromIt()
    {
        var market = ShadowportRules.GetContrabandMarket(CommodityType.Moonspice, ShadowportStance.Shadowport);
        var commodity = TradeLookup.GetCommodity(CommodityType.Moonspice);

        var fresh = TradeRules.PlanSell(commodity, market, 0, 10, 100, 100, 0, 0);
        var flooded = TradeRules.PlanSell(commodity, market, 200, 10, 100, 100, 0, 0);
        var taxed = TradeRules.PlanSell(commodity, market, 0, 10, 100, 100, 0, 10);

        fresh.Refusal.Should().Be(TradeRefusal.None);
        flooded.Credits.Should().BeLessThan(fresh.Credits);
        taxed.Tariff.Should().Be(TradeTariffRules.GetTariff(fresh.Credits, 10));
        (taxed.Credits + taxed.Tariff).Should().Be(fresh.Credits);
    }

    // Normal goods

    [Test]
    public void NormalGoods_SellFifteenPercentLowerUnderAShadowport_AndNothingElseChanges()
    {
        ShadowportRules.GetNormalGoodsSaleFactor(ShadowportStance.Shadowport).Should().Be(0.85);
        ShadowportRules.GetNormalGoodsSaleFactor(ShadowportStance.Lawful).Should().Be(1.0);

        foreach (var exchange in LawfulExchanges)
        {
            foreach (var market in exchange.Markets)
            {
                var lower = TradePricing.WithPriceMultiplier(market, ShadowportRules.GetNormalGoodsSaleFactor(ShadowportStance.Shadowport));
                var commodity = TradeLookup.GetCommodity(market.Commodity);

                TradePricing.GetUnitSellPrice(commodity.BasePrice, lower, 0, 10).Should()
                    .BeApproximately(TradePricing.GetUnitSellPrice(commodity.BasePrice, market, 0, 10) * 0.85, 1e-9);
                lower.Depth.Should().Be(market.Depth);
                lower.Role.Should().Be(market.Role);
            }
        }
    }

    [Test]
    public void ArbitrageBound_BuyingAtOneDoorAndSellingAtAShadowportNeverProfits()
    {
        // The space revamp's exchange contract: a sale modifier may only lower a door's sale price, so no pair of doors of
        // one market pays a profit that the shared market's spread does not already forbid.
        foreach (var exchange in LawfulExchanges)
        {
            foreach (var market in exchange.Markets.Where(x => !TradeLookup.GetCommodity(x.Commodity).IsHoldingGood))
            {
                var commodity = TradeLookup.GetCommodity(market.Commodity);
                var door = TradePricing.WithPriceMultiplier(market, ShadowportRules.GetNormalGoodsSaleFactor(ShadowportStance.Shadowport));
                var buy = TradePricing.GetUnitBuyPrice(commodity.BasePrice, market, 0, MaximumSocial, MaximumStandingBonus);
                var sell = TradePricing.GetUnitSellPrice(commodity.BasePrice, door, 0, MaximumSocial, MaximumStandingBonus);

                sell.Should().BeLessThan(buy, $"{exchange.Name} {market.Commodity}");
            }
        }
    }

    // The bust

    [Test]
    public void TheBustChance_IsAShareOrMultipleOfTheCustomsScanChanceOnTheSameCargo()
    {
        foreach (var value in new[] { 250, 5000, 60000, 400000 })
        {
            var scan = CustomsRules.GetScanChance(value, 0);
            ShadowportRules.GetBustChance(value, 0, 0, ShadowportStance.Shadowport).Should().BeApproximately(scan * 0.5, 1e-12);
            ShadowportRules.GetBustChance(value, 0, 0, ShadowportStance.Lawful).Should().BeApproximately(Math.Min(0.9, scan * 3.0), 1e-12);
        }
    }

    [Test]
    public void AShadowportBustIsSmall_AndALawfulPortsIsHigh()
    {
        // 1 unit, 20 units, and a full extra-small freighter's hold of Moonspice (240 units at 250 credits).
        foreach (var value in new[] { 250, 5000, 60000 })
        {
            var shadowport = ShadowportRules.GetBustChance(value, 0, 0, ShadowportStance.Shadowport);
            var lawful = ShadowportRules.GetBustChance(value, 0, 0, ShadowportStance.Lawful);
            shadowport.Should().BeLessThan(0.10, $"value {value}");
            lawful.Should().BeGreaterThan(shadowport * 5);
        }

        ShadowportRules.GetBustChance(60000, 0, 0, ShadowportStance.Lawful).Should().BeGreaterThan(0.40);
    }

    [Test]
    public void ASmugglingCompartmentAndStanding_LowerTheBustChance_AsTheyDoACustomsScan()
    {
        var bare = ShadowportRules.GetBustChance(60000, 0, 0, ShadowportStance.Shadowport);
        var compartment = ShadowportRules.GetBustChance(60000, CustomsRules.GetCompartmentPercent(27), 0, ShadowportStance.Shadowport);
        var both = ShadowportRules.GetBustChance(60000, CustomsRules.GetCompartmentPercent(27), 25, ShadowportStance.Shadowport);

        compartment.Should().BeLessThan(bare);
        both.Should().BeLessThan(compartment);
        compartment.Should().BeApproximately(bare * (1 - CustomsRules.GetCompartmentPercent(27) / 100.0), 1e-12);

        var lawfulBare = ShadowportRules.GetBustChance(60000, 0, 0, ShadowportStance.Lawful);
        ShadowportRules.GetBustChance(60000, CustomsRules.GetCompartmentPercent(27), 0, ShadowportStance.Lawful).Should().BeLessThan(lawfulBare);
    }

    [Test]
    public void NoBustWithoutBannedCargo_AndNeverACertainOne()
    {
        ShadowportRules.GetBustChance(0, 0, 0, ShadowportStance.Lawful).Should().Be(0.0);
        ShadowportRules.GetBustChance(-5, 0, 0, ShadowportStance.Shadowport).Should().Be(0.0);
        ShadowportRules.GetBustChance(100000000, 0, 0, ShadowportStance.Lawful).Should().BeLessThanOrEqualTo(ShadowportTuning.BustMaximum);
        ShadowportTuning.BustMaximum.Should().BeLessThan(1.0);
    }

    // Stations

    [Test]
    public void AnOrbitStation_TradesThroughItsOrbitsExchange_SoItIsALawfulPortOrAShadowportLikeThePlanetBelow()
    {
        foreach (var anchor in StationTuning.Anchors.Where(x => !x.IsWildSpace))
        {
            var exchange = TradeLookup.GetExchange(StationRules.GetExchange(anchor.Id));
            exchange.Should().NotBeNull(anchor.Id);

            var expected = exchange.Authority == TradeAuthority.None ? ShadowportStance.OpenPort : ShadowportStance.Lawful;
            ShadowportRules.GetStance(exchange.Authority, false).Should().Be(expected, anchor.Id);
            if (expected != ShadowportStance.OpenPort)
            {
                ShadowportRules.GetStance(exchange.Authority, true).Should().Be(ShadowportStance.Shadowport, anchor.Id);
                ShadowportRules.GetContrabandCommodities(exchange, ShadowportStance.Lawful).Should().NotBeEmpty(anchor.Id);
            }
        }

        // The orbits of Viscara, CZ-220, Mon Cala and Korriban have a customs authority.
        StationTuning.Anchors.Where(x => !x.IsWildSpace && TradeLookup.GetExchange(x.Exchange).Authority != TradeAuthority.None)
            .Select(x => x.Exchange).Should().Contain(new[] { TradeExchangeType.Viscara, TradeExchangeType.CZ220, TradeExchangeType.MonCala, TradeExchangeType.Korriban });
    }

    [Test]
    public void AWildSpaceStation_TradesAtAnOpenPort_SoShadowportChangesNothingThere()
    {
        var wildSpace = StationTuning.Anchors.Where(x => x.IsWildSpace).ToList();
        wildSpace.Should().NotBeEmpty();

        foreach (var anchor in wildSpace)
        {
            var exchange = TradeLookup.GetExchange(StationRules.GetExchange(anchor.Id));
            exchange.Should().NotBeNull(anchor.Id);
            exchange.Authority.Should().Be(TradeAuthority.None, anchor.Id);
            ShadowportRules.GetStance(exchange.Authority, true).Should().Be(ShadowportStance.OpenPort, anchor.Id);
        }
    }

    // Wiring

    [Test]
    public void TheCityDock_ReadsTheEdictFromTheCitysEdicts()
    {
        var dock = Method(Source("Service", "TradeCity.cs"), "public static TradeDock TryGetCityDock(string starportPropertyId)");
        dock.Should().Contain("ShadowportActive = EdictEffects.IsShadowportActive(city)");

        Source("Service", "TradeService", "TradeDock.cs").Should().Contain("public bool ShadowportActive { get; set; }");
    }

    [Test]
    public void OnlyASaleReachesTheContrabandMarket_AndTheBustRollsBeforeAnyCreditsMove()
    {
        var trade = Source("Service", "Trade.cs");

        var sell = Method(trade, "public static void Sell(");
        sell.Should().Contain("selling: true");
        sell.Should().Contain("GetSaleMarket(dock, exchangeType, commodity, market)");
        sell.IndexOf("TryBustContrabandSale(", StringComparison.Ordinal).Should().BeGreaterThan(sell.IndexOf("order.Refusal", StringComparison.Ordinal))
            .And.BeLessThan(sell.IndexOf("GiveGoldToCreature(", StringComparison.Ordinal));

        var buy = Method(trade, "public static void Buy(");
        buy.Should().NotContain("selling: true");
        buy.Should().NotContain("Shadowport");

        var lookup = Method(trade, "private static bool TryGetTrade(");
        lookup.Should().Contain("if (sharedMarket == null && selling)");
        lookup.Should().Contain("GetContrabandSaleMarket(dock, exchangeType, commodity)");
    }

    [Test]
    public void TheBustIsTheCustomsResult_FineConfiscationAndLawfulStanding()
    {
        var bust = Method(Source("Service", "TradeShadowport.cs"), "private static bool TryBustContrabandSale(");

        bust.Should().Contain("CollectCustomsFine(player, shipId, authority, fineDue, true)");
        bust.Should().Contain("RecordStanding(player, authority, TradeStandingEvent.CaughtSmuggling)");
        bust.Should().Contain("CustomsRules.GetBannedValue(ship, Space.GetDockedShips(ship.Id), authority)");
        bust.Should().Contain("CustomsRules.GetCompartmentPercent(GetFittedModuleTags(ship))");
        bust.Should().Contain("CustomsRules.GetFine(bannedValue)");
        bust.Should().Contain("ShadowportRules.GetBustChance(");
    }

    [Test]
    public void TheExchangeWindow_ListsContrabandAsSaleOnly_AndQuotesTheDoorsSalePrice()
    {
        var window = Source("Feature", "GuiDefinition", "ViewModel", "TradeExchangeViewModel.cs");

        window.Should().Contain("ShadowportRules.GetContrabandCommodities(exchange, Trade.GetShadowportStance(_dock, _exchange))");
        window.Should().Contain("Trade.GetSaleMarket(_dock, _exchange, type, market)");
        window.Should().Contain("canBuy.Add(hasShip && capacity > 0 && !commodity.IsHoldingGood && !saleOnly)");
        window.Should().Contain("ShadowportRules.Describe(Trade.GetShadowportStance(_dock, _exchange))");
    }
}
