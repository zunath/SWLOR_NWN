using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.SpaceObjectDefinition;
using SWLOR.Game.Server.Feature.TradeDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.FactionService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.TradeService;
using SWLOR.Game.Server.Feature.SpawnDefinition;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// What faction standing unlocks in trade, city tariffs and player-city exchanges (plan section 13, W7c). The numbers
/// come from tools/SpaceRevampBalance/trade.py, which also writes the reference fixture these tests read.
/// </summary>
public class TradeStandingTests
{
    private static readonly DateTime Now = new(2026, 10, 27, 12, 0, 0, DateTimeKind.Utc);

    private static JObject Reference() => JObject.Parse(File.ReadAllText(
        Path.Combine(RepositoryRoot(), "SWLOR.Game.Server.Tests", "Fixtures", "Trade", "TradeRiskReference.json")));

    private static CommodityDetail Commodity(CommodityType type) => TradeLookup.GetCommodity(type);

    private static TradeMarketDetail Market(TradeExchangeType exchange, CommodityType commodity) =>
        TradeLookup.GetExchange(exchange).FindMarket(commodity);

    // Standing tiers

    [Test]
    public void Tiers_MatchTheBalanceModel()
    {
        foreach (var token in Reference()["StandingReference"]!["Tiers"]!)
        {
            var standing = token["Standing"]!.Value<int>();
            var tier = TradeStandingRules.GetTier(standing);

            ((int)tier).Should().Be(token["Tier"]!.Value<int>(), $"standing {standing}");
            TradeStandingRules.GetScanCutPercent(tier).Should().Be(token["ScanCut"]!.Value<int>(), $"standing {standing}");
            TradeStandingRules.GetPriceBonus(tier).Should().BeApproximately(token["PriceBonus"]!.Value<double>(), 1e-12, $"standing {standing}");
        }
    }

    [Test]
    public void Tiers_RiseWithStandingAndHostileIsTheBottom()
    {
        TradeStandingRules.GetTier(0).Should().Be(TradeStandingTier.Neutral);
        TradeStandingRules.GetTier(TradeTuning.StandingHostile).Should().Be(TradeStandingTier.Hostile);
        TradeStandingRules.GetTier(TradeTuning.StandingHostile + 1).Should().Be(TradeStandingTier.Neutral);
        TradeStandingRules.GetTier(Faction.MinimumFaction).Should().Be(TradeStandingTier.Hostile);
        TradeStandingRules.GetTier(Faction.MaximumFaction).Should().Be(TradeStandingTier.Exalted);

        var tiers = new[] { TradeStandingTier.Trusted, TradeStandingTier.Honored, TradeStandingTier.Exalted };
        for (var index = 0; index < tiers.Length; index++)
        {
            TradeStandingRules.GetTier(TradeTuning.StandingTierMinimums[index]).Should().Be(tiers[index]);
            TradeStandingRules.GetTier(TradeTuning.StandingTierMinimums[index] - 1).Should().Be(index == 0 ? TradeStandingTier.Neutral : tiers[index - 1]);
        }

        TradeTuning.StandingTierMinimums.Should().BeInAscendingOrder().And.OnlyHaveUniqueItems();
        TradeTuning.StandingScanCutPercents.Should().BeInAscendingOrder();
        TradeTuning.StandingPriceBonuses.Should().BeInAscendingOrder();
        TradeTuning.StandingScanCutPercents.Last().Should().Be(25, "the top tier cuts customs scans by a quarter");
        TradeTuning.StandingHostile.Should().BeLessThan(0);
        TradeTuning.StandingTierMinimums.Last().Should().BeLessThan(Faction.MaximumFaction);
        TradeStandingRules.GetScanCutPercent(TradeStandingTier.Hostile).Should().Be(0);
        TradeStandingRules.GetPriceBonus(TradeStandingTier.Neutral).Should().Be(0);
    }

    [Test]
    public void Tiers_AreReachableByDeliveringFreight()
    {
        var delivery = TradeStandingRules.GetAmount(TradeStandingEvent.DeliveredFreight);
        delivery.Should().BePositive();
        (TradeTuning.StandingTierMinimums.Last() / delivery).Should().BeLessThan(200, "the top tier is a long grind, not an impossible one");
        (TradeTuning.StandingTierMinimums.First() / delivery).Should().BeGreaterThan(5, "the first tier is not given away");

        // Five runs from customs make an authority hostile; a single offense does not.
        TradeStandingRules.GetTier(TradeStandingRules.GetAmount(TradeStandingEvent.RanFromCustoms) * 4)
            .Should().Be(TradeStandingTier.Neutral);
        TradeStandingRules.GetTier(TradeStandingRules.GetAmount(TradeStandingEvent.RanFromCustoms) * 5)
            .Should().Be(TradeStandingTier.Hostile);
    }

    // Customs

    [Test]
    public void ScanChance_StandingCutsItAndMultipliesWithTheCompartment()
    {
        foreach (var token in Reference()["StandingReference"]!["Scans"]!)
        {
            CustomsRules.GetScanChance(token["Banned"]!.Value<int>(), token["Compartment"]!.Value<int>(), token["Standing"]!.Value<int>())
                .Should().BeApproximately(token["Chance"]!.Value<double>(), 1e-6);
        }

        var plain = CustomsRules.GetScanChance(10000, 0);
        CustomsRules.GetScanChance(10000, 0, 25).Should().BeApproximately(plain * 0.75, 1e-12);
        CustomsRules.GetScanChance(10000, 30, 25).Should().BeApproximately(plain * 0.70 * 0.75, 1e-12);
        CustomsRules.GetScanChance(10000, 0, 100).Should().Be(0);
        CustomsRules.GetScanChance(0, 0, 25).Should().Be(0, "a clean ship is never stopped");

        var previous = plain;
        foreach (var tier in new[] { TradeStandingTier.Trusted, TradeStandingTier.Honored, TradeStandingTier.Exalted })
        {
            var chance = CustomsRules.GetScanChance(10000, 0, TradeStandingRules.GetScanCutPercent(tier));
            chance.Should().BeLessThan(previous, tier.ToString());
            previous = chance;
        }
    }

    [Test]
    public void HostilePatrol_AttacksOnSightOncePerCooldown()
    {
        TradeStandingRules.ShouldPatrolAttack(TradeStandingTier.Hostile, null, Now).Should().BeTrue();
        TradeStandingRules.ShouldPatrolAttack(TradeStandingTier.Hostile, Now.AddSeconds(-10), Now).Should().BeFalse("a patrol just came");
        TradeStandingRules.ShouldPatrolAttack(TradeStandingTier.Hostile, Now.AddSeconds(-TradeTuning.HostilePatrolCooldownSeconds), Now).Should().BeTrue();

        foreach (var tier in new[] { TradeStandingTier.Neutral, TradeStandingTier.Trusted, TradeStandingTier.Honored, TradeStandingTier.Exalted })
            TradeStandingRules.ShouldPatrolAttack(tier, null, Now).Should().BeFalse(tier.ToString());
    }

    [Test]
    public void HostilePatrol_EveryLawfulOrbitHasPatrolShipsToSend()
    {
        var registered = RevampNpcShipCatalog.Ships.Select(x => x.CreatureTag).ToHashSet();
        foreach (var orbit in TradeCatalog.Orbits.Where(x => x.CustomsAuthorities.Length > 0))
        {
            foreach (var authority in orbit.CustomsAuthorities)
            {
                TradeStandingRules.GetFaction(authority).Should().NotBe(FactionType.Invalid, $"{orbit.Name} {authority}");
                CustomsRules.GetPatrolTags(authority, orbit.Tier).Should().NotBeEmpty($"{orbit.Name} {authority}")
                    .And.OnlyContain(tag => registered.Contains(tag), $"{orbit.Name} {authority} patrol ships exist");
            }
        }
    }

    // Prices

    [Test]
    public void Prices_MatchTheBalanceModel()
    {
        var market = new TradeMarketDetail { PriceFactor = 1.2, Depth = 2500 };
        foreach (var token in Reference()["StandingReference"]!["Prices"]!)
        {
            var social = token["Social"]!.Value<int>();
            var bonus = token["Bonus"]!.Value<double>();
            var units = token["Units"]!.Value<int>();

            TradePricing.GetBuyCost(100, market, 0, units, social, bonus).Should().Be(token["Buy"]!.Value<int>());
            TradePricing.GetSellRevenue(100, market, 0, units, social, bonus).Should().Be(token["Sell"]!.Value<int>());
        }
    }

    [Test]
    public void Prices_StandingImprovesBothSidesAndAddsToSocial()
    {
        var silk = Commodity(CommodityType.KinrathSilk);
        var market = Market(TradeExchangeType.MonCala, CommodityType.KinrathSilk);
        var exalted = TradeStandingRules.GetPriceBonus(TradeStandingTier.Exalted);

        TradePricing.GetUnitBuyPrice(silk.BasePrice, market, 0, 10, exalted).Should()
            .BeLessThan(TradePricing.GetUnitBuyPrice(silk.BasePrice, market, 0, 10));
        TradePricing.GetUnitSellPrice(silk.BasePrice, market, 0, 10, exalted).Should()
            .BeGreaterThan(TradePricing.GetUnitSellPrice(silk.BasePrice, market, 0, 10));

        // Social 20 (1%) and a 0.5% standing bonus equal Social 25 (1.5%).
        TradePricing.GetUnitSellPrice(silk.BasePrice, market, 0, 20, 0.005).Should()
            .BeApproximately(TradePricing.GetUnitSellPrice(silk.BasePrice, market, 0, 25), 1e-9);

        // Ladder: each tier pays a little better than the last.
        var previous = TradePricing.GetSellRevenue(silk.BasePrice, market, 0, 200, 10);
        foreach (var tier in new[] { TradeStandingTier.Trusted, TradeStandingTier.Honored, TradeStandingTier.Exalted })
        {
            var revenue = TradePricing.GetSellRevenue(silk.BasePrice, market, 0, 200, 10, TradeStandingRules.GetPriceBonus(tier));
            revenue.Should().BeGreaterThan(previous, tier.ToString());
            previous = revenue;
        }

        TradeTuning.StandingPriceBonuses.Last().Should().BeLessThanOrEqualTo(TradeTuning.SocialMaximum, "standing improves prices only slightly");
    }

    [Test]
    public void Prices_BuyingAndSellingBackStillLosesAtTheBestStandingAndSocial()
    {
        var bonus = TradeStandingRules.GetPriceBonus(TradeStandingTier.Exalted);
        foreach (var exchange in TradeCatalog.Exchanges)
        {
            foreach (var market in exchange.Markets)
            {
                var basePrice = Commodity(market.Commodity).BasePrice;
                foreach (var units in new[] { 1, 50, 500 })
                {
                    var cost = TradePricing.GetBuyCost(basePrice, market, 0, units, 40, bonus);
                    var back = TradePricing.GetSellRevenue(basePrice, market, -units, units, 40, bonus);
                    back.Should().BeLessThan(cost, $"{exchange.Name} {market.Commodity} x{units}");
                }
            }
        }
    }

    [Test]
    public void Prices_OnlyExchangesWithAFactionPayTheBonus()
    {
        TradeLookup.GetExchange(TradeExchangeType.Dathomir)!.StandingFaction.Should().Be(FactionType.Invalid);
        foreach (var exchange in TradeCatalog.Exchanges.Where(x => x.Type != TradeExchangeType.Dathomir))
            exchange.StandingFaction.Should().NotBe(FactionType.Invalid, exchange.Name);
    }

    // Freight

    [Test]
    public void Freight_LargerLoadsNeedStandingWithTheOfferingFaction()
    {
        foreach (var pair in Reference()["StandingReference"]!["FreightMinimumStandings"]!.Cast<JProperty>())
            TradeStandingRules.GetFreightMinimumStanding(int.Parse(pair.Name)).Should().Be(pair.Value.Value<int>(), pair.Name);

        TradeStandingRules.GetFreightMinimumStanding(TradeTuning.FreightOfferVolumes[0]).Should().Be(0);
        TradeStandingRules.GetFreightMinimumStanding(TradeTuning.FreightOfferVolumes[1]).Should().Be(0);
        TradeTuning.FreightOfferMinimumStandings.Should().BeInAscendingOrder()
            .And.HaveCount(TradeTuning.FreightOfferVolumes.Length);
        TradeTuning.FreightOfferVolumes.Should().BeInAscendingOrder();

        foreach (var exchange in new[] { TradeExchangeType.Viscara, TradeExchangeType.Tatooine, TradeExchangeType.Korriban })
        {
            foreach (var offer in FreightContractRules.GetOffers(exchange, Now))
                offer.MinimumStanding.Should().Be(TradeStandingRules.GetFreightMinimumStanding(offer.Volume), $"{exchange} {offer.Volume}");
        }

        FreightContractRules.GetOffers(TradeExchangeType.Dathomir, Now).Should().OnlyContain(x => x.MinimumStanding == 0, "Dathomir has no faction to gate by");
    }

    [Test]
    public void Freight_AcceptingChecksStanding()
    {
        var offers = FreightContractRules.GetOffers(TradeExchangeType.Viscara, Now);
        var gated = offers.First(x => x.MinimumStanding > 0);
        var open = offers.First(x => x.MinimumStanding == 0);
        var none = new List<FreightContract>();
        var taken = new List<string>();

        FreightContractRules.CanAccept(gated, none, taken, 2000, 2000, 1_000_000, gated.MinimumStanding - 1).Should().Be(FreightRefusal.StandingTooLow);
        FreightContractRules.CanAccept(gated, none, taken, 2000, 2000, 1_000_000, gated.MinimumStanding).Should().Be(FreightRefusal.None);
        FreightContractRules.CanAccept(gated, none, taken, 2000, 2000, 1_000_000, Faction.MinimumFaction).Should().Be(FreightRefusal.StandingTooLow);
        FreightContractRules.CanAccept(open, none, taken, 2000, 2000, 1_000_000, Faction.MinimumFaction).Should().Be(FreightRefusal.None, "small loads are open to anyone");
        FreightContractRules.CanAccept(gated, none, taken, 2000, 2000, 1_000_000).Should().Be(FreightRefusal.None, "no standing given means no gate");
        FreightContractRules.GetRefusalMessage(FreightRefusal.StandingTooLow).Should().NotBeEmpty();
    }

    // Tariffs

    [Test]
    public void Tariff_MatchesTheBalanceModel()
    {
        foreach (var token in Reference()["StandingReference"]!["Tariffs"]!)
            TradeTariffRules.GetTariff(token["Credits"]!.Value<int>(), token["Percent"]!.Value<int>()).Should().Be(token["Tariff"]!.Value<int>());
    }

    [Test]
    public void Tariff_IsCappedRoundedUpAndNeverMoreThanTheMoney()
    {
        TradeTuning.TariffMaximumPercent.Should().Be(10);
        TradeTariffRules.ClampPercent(-3).Should().Be(0);
        TradeTariffRules.ClampPercent(7).Should().Be(7);
        TradeTariffRules.ClampPercent(25).Should().Be(10);
        TradeTariffRules.GetTariff(1000, 0).Should().Be(0);
        TradeTariffRules.GetTariff(0, 10).Should().Be(0);
        TradeTariffRules.GetTariff(-50, 10).Should().Be(0);
        TradeTariffRules.GetTariff(1, 10).Should().Be(1, "rounded up: a tariff on any trade is at least a credit");
        TradeTariffRules.GetTariff(999, 5).Should().Be(50);
        TradeTariffRules.GetTariff(1000, 99).Should().Be(100, "a typed tariff above the cap counts as the cap");
        TradeTariffRules.GetTariff(int.MaxValue, 10).Should().Be((int)Math.Ceiling(int.MaxValue / 10.0), "large orders do not overflow");

        for (var credits = 1; credits <= 400; credits++)
        {
            for (var percent = 0; percent <= TradeTuning.TariffMaximumPercent; percent++)
            {
                var tariff = TradeTariffRules.GetTariff(credits, percent);
                tariff.Should().BeInRange(0, credits);
                tariff.Should().BeInRange(0, (int)Math.Ceiling(credits * percent / 100.0));
            }
        }
    }

    [Test]
    public void Tariff_IsAddedToPurchasesAndTakenFromSales()
    {
        var grain = Commodity(CommodityType.Grain);
        var market = Market(TradeExchangeType.Viscara, CommodityType.Grain);

        var plainBuy = TradeRules.PlanBuy(grain, market, 0, 10, 20, 500, 500, 1_000_000);
        var taxedBuy = TradeRules.PlanBuy(grain, market, 0, 10, 20, 500, 500, 1_000_000, 0, 10);
        taxedBuy.Units.Should().Be(plainBuy.Units);
        taxedBuy.Tariff.Should().Be(TradeTariffRules.GetTariff(plainBuy.Credits, 10)).And.BePositive();
        taxedBuy.Credits.Should().Be(plainBuy.Credits + taxedBuy.Tariff);

        var plainSale = TradeRules.PlanSell(grain, market, 0, 10, 20, 50);
        var taxedSale = TradeRules.PlanSell(grain, market, 0, 10, 20, 50, 0, 10);
        taxedSale.Tariff.Should().Be(TradeTariffRules.GetTariff(plainSale.Credits, 10)).And.BePositive();
        taxedSale.Credits.Should().Be(plainSale.Credits - taxedSale.Tariff);

        plainBuy.Tariff.Should().Be(0);
        plainSale.Tariff.Should().Be(0, "an NPC dock charges none");
        TradeRules.PlanBuy(grain, market, 0, 10, 20, 500, 500, 1_000_000, 0, 50).Tariff.Should().Be(taxedBuy.Tariff, "the cap holds");
        TradeRules.PlanSell(grain, market, 0, 10, 20, 50, 0, 50).Tariff.Should().Be(taxedSale.Tariff, "the cap holds");
    }

    [Test]
    public void Tariff_AffordabilityCountsTheTariff()
    {
        var grain = Commodity(CommodityType.Grain);
        var market = Market(TradeExchangeType.Viscara, CommodityType.Grain);

        foreach (var credits in new[] { 40, 120, 333, 1000, 5000 })
        {
            var order = TradeRules.PlanBuy(grain, market, 0, 10, 10000, 100000, 100000, credits, 0, 10);
            if (order.Refusal != TradeRefusal.None)
                continue;

            order.Credits.Should().BeLessThanOrEqualTo(credits, $"{credits} credits");
            var one = TradeRules.PlanBuy(grain, market, 0, 10, order.Units + 1, 100000, 100000, credits, 0, 10);
            (one.Refusal != TradeRefusal.None || one.Units == order.Units).Should().BeTrue("one more unit would not be affordable");
        }

        var cost = TradePricing.GetBuyCost(grain.BasePrice, market, 0, 5, 10);
        TradeRules.PlanBuy(grain, market, 0, 10, 5, 100, 100, cost, 0, 10).Units.Should().BeLessThan(5, "the price alone is not enough once a tariff is added");
        TradeRules.PlanBuy(grain, market, 0, 10, 5, 100, 100, cost + TradeTariffRules.GetTariff(cost, 10), 0, 10).Units.Should().Be(5);
    }

    [Test]
    public void Tariff_MakesRoundTripsAtACityExchangeLoseMore()
    {
        var silk = Commodity(CommodityType.KinrathSilk);
        var market = Market(TradeExchangeType.Viscara, CommodityType.KinrathSilk);
        var target = Market(TradeExchangeType.MonCala, CommodityType.KinrathSilk);

        int Profit(int tariffPercent)
        {
            var buy = TradeRules.PlanBuy(silk, market, 0, 10, 100, 1000, 1000, 10_000_000, 0, tariffPercent);
            var sell = TradeRules.PlanSell(silk, target, 0, 10, 100, 100, 0, tariffPercent);
            return sell.Credits - buy.Credits;
        }

        Profit(0).Should().BePositive("the untaxed pair pays");
        Profit(5).Should().BeLessThan(Profit(0));
        Profit(10).Should().BeLessThan(Profit(5));
        (Profit(0) - Profit(10)).Should().BeGreaterThan((int)(Profit(0) * 0.10), "a 10% tariff on both ends takes a real share");
    }

    [Test]
    public void Tariff_QuotedPricesIncludeIt()
    {
        var grain = Commodity(CommodityType.Grain);
        var market = Market(TradeExchangeType.Viscara, CommodityType.Grain);

        TradeRules.GetQuotedBuyPrice(grain, market, 0, 10, 0, 0).Should().Be(TradePricing.GetQuotedBuyPrice(grain.BasePrice, market, 0, 10));
        TradeRules.GetQuotedSellPrice(grain, market, 0, 10, 0, 0).Should().Be(TradePricing.GetQuotedSellPrice(grain.BasePrice, market, 0, 10));
        TradeRules.GetQuotedBuyPrice(grain, market, 0, 10, 0, 10).Should().BeGreaterThan(TradePricing.GetQuotedBuyPrice(grain.BasePrice, market, 0, 10));
        TradeRules.GetQuotedSellPrice(grain, market, 0, 10, 0, 10).Should().BeLessThan(TradePricing.GetQuotedSellPrice(grain.BasePrice, market, 0, 10));
    }

    [Test]
    public void Tariff_TheWindowTellsThePlayerWhatTheCityCharges()
    {
        TradeTariffRules.Describe(0, "Mos Vara").Should().Contain("no tariff");
        TradeTariffRules.Describe(5, "Mos Vara").Should().Contain("5%").And.Contain("Mos Vara");
        TradeTariffRules.Describe(40, "Mos Vara").Should().Contain($"{TradeTuning.TariffMaximumPercent}%");
    }

    // Player-city exchanges

    [Test]
    public void CityExchange_OpensAtTheStarportLevel()
    {
        TradeTuning.CityExchangeStarportLevel.Should().Be(2);
        TradeTariffRules.IsExchangeAvailable(0).Should().BeFalse();
        TradeTariffRules.IsExchangeAvailable(1).Should().BeFalse();
        TradeTariffRules.IsExchangeAvailable(2).Should().BeTrue();
        TradeTariffRules.IsExchangeAvailable(5).Should().BeTrue();

        var npc = TradeDock.Npc("viscara_landing", TradeExchangeType.Viscara);
        npc.IsCity.Should().BeFalse();
        npc.HasExchange.Should().BeTrue("an NPC dock always trades");
        npc.TariffPercent.Should().Be(0);

        var city = new TradeDock { DockId = "starport", IsCity = true, Exchange = TradeExchangeType.Viscara, StarportLevel = 1 };
        city.HasExchange.Should().BeFalse();
        city.StarportLevel = 2;
        city.HasExchange.Should().BeTrue();
        city.Exchange = TradeExchangeType.Invalid;
        city.HasExchange.Should().BeFalse("a city on no NPC planet has no exchange to trade through");
    }

    [Test]
    public void CityExchange_EveryNpcPlanetCityTradesThroughItsPlanetsExchange()
    {
        foreach (var exchange in TradeCatalog.Exchanges)
        {
            foreach (var planet in exchange.PlanetTypes)
                TradeLookup.GetExchangeForPlanet(planet).Should().Be(exchange.Type, planet.ToString());
        }

        Enum.IsDefined(PropertyTaxType.Exchange).Should().BeTrue();
        ((int)PropertyTaxType.Exchange).Should().Be(3);
        PropertyTaxType.Exchange.Should().NotBe(PropertyTaxType.Citizenship).And.NotBe(PropertyTaxType.Transportation);
    }

    // Outcomes

    [Test]
    public void EarningsWithStanding_StayInsideThePlanTargets()
    {
        var outcome = Reference()["StandingOutcome"]!;
        var tiers = outcome["Tiers"]!.ToList();
        tiers.Should().HaveCount(TradeTuning.StandingTierMinimums.Length);

        var neutral = Reference()["Outcome"]!;
        var legal = neutral["LegalPercent"]!.Value<int>();
        var contraband = neutral["ContrabandPercent"]!.Value<int>();
        foreach (var tier in tiers)
        {
            var name = tier["Tier"]!.Value<string>();
            tier["LegalPercent"]!.Value<int>().Should().BeInRange(100, 125, $"{name}: open trading stays 100-125% at the best standing");
            tier["ContrabandPercent"]!.Value<int>().Should().BeInRange(150, 200, $"{name}: contraband stays 150-200% at the best standing");
            tier["LegalPercent"]!.Value<int>().Should().BeGreaterThanOrEqualTo(legal, name);
            tier["ContrabandPercent"]!.Value<int>().Should().BeGreaterThanOrEqualTo(contraband, name);
        }

        tiers.Select(x => x["LegalPercent"]!.Value<int>()).Should().BeInAscendingOrder();
        tiers.Select(x => x["ContrabandPercent"]!.Value<int>()).Should().BeInAscendingOrder();
    }

    [Test]
    public void EarningsWithStanding_FreightGrowsWithTheLoadsStandingUnlocks()
    {
        var outcome = Reference()["StandingOutcome"]!;
        var tiers = outcome["Tiers"]!.Select(x => x["FreightPercent"]!.Value<int>()).ToList();

        outcome["NeutralFreightPercent"]!.Value<int>().Should().BeLessThan(50, "a pilot with no standing is limited to the small loads");
        tiers.Should().BeInAscendingOrder();
        tiers.First().Should().BeInRange(65, 90, "a Trusted pilot hauls at about the plan's 75%");
        tiers.First().Should().BeGreaterThan(outcome["NeutralFreightPercent"]!.Value<int>());
        tiers.Last().Should().BeInRange(100, 125, "freight is legal trade: even the best-case Exalted pilot stays in the 100-125% legal band");
    }

    [Test]
    public void Freight_TheBulkLoadPaysLessPerUnit()
    {
        TradeTuning.FreightOfferFeeFactors.Should().HaveCount(TradeTuning.FreightOfferVolumes.Length);
        TradeTuning.FreightOfferFeeFactors.Take(3).Should().OnlyContain(x => x == 1.0);
        TradeTuning.FreightOfferFeeFactors.Last().Should().BeLessThan(1.0);

        FreightContractRules.GetFee(400, 3).Should().Be((int)Math.Round(400 * (0.70 + 1.05 * 3) * TradeTuning.FreightOfferFeeFactors.Last(), MidpointRounding.ToEven));
        FreightContractRules.GetFee(200, 3).Should().Be((int)Math.Round(200 * (0.70 + 1.05 * 3), MidpointRounding.ToEven));
        FreightContractRules.GetFee(300, 3).Should().Be((int)Math.Round(300 * (0.70 + 1.05 * 3), MidpointRounding.ToEven), "only the offer volumes carry a factor");
        FreightContractRules.GetFee(400, 3).Should().BeGreaterThan(FreightContractRules.GetFee(200, 3), "the bulk load still pays more in total");
    }

    // Wiring

    [Test]
    public void Wiring_StarportsManagerAndTradeUseTheTariff()
    {
        var starport = ReadServerSource("Feature", "DialogDefinition", "StarportDialog.cs");
        starport.Should().Contain("AddCityTradeExchangeResponse(page, player, starportId);");
        starport.Should().Contain("Trade.TryGetCityDock(starportId)");

        var city = ReadServerSource("Feature", "GuiDefinition", "ManageCityDefinition.cs");
        city.Should().Contain("Exchange Tariff:").And.Contain("model => model.ExchangeTariff");

        var cityModel = ReadServerSource("Feature", "GuiDefinition", "ViewModel", "ManageCityViewModel.cs");
        cityModel.Should().Contain("PropertyTaxType.Exchange");
        cityModel.Should().Contain("TradeTariffRules.ClampPercent");
        cityModel.Should().Contain("if (CanEditTaxes)", "only the leadership sets the tariff");

        ReadServerSource("Service", "Property.cs").Should().Contain("property.Taxes[PropertyTaxType.Exchange] = 0;");

        var trade = ReadServerSource("Service", "Trade.cs");
        trade.Split("PayTariff(dock, order.Tariff, player);").Length.Should().Be(3, "buying and selling both pay the treasury");
        trade.Should().Contain("dock.TariffPercent");
        ReadServerSource("Service", "TradeCity.cs").Should().Contain("city.Treasury += tariff;");

        var window = ReadServerSource("Feature", "GuiDefinition", "TradeExchangeDefinition.cs");
        window.Should().Contain("model => model.TariffText").And.Contain("model => model.StandingText");
        ReadServerSource("Feature", "GuiDefinition", "ViewModel", "TradeExchangeViewModel.cs")
            .Should().Contain("TradeTariffRules.Describe(_dock.TariffPercent, _dock.CityName)");
    }

    [Test]
    public void Wiring_StandingEffectsAreAppliedWhereTheyHappen()
    {
        var customs = ReadServerSource("Service", "TradeCustoms.cs");
        customs.Should().Contain("TradeStandingRules.GetScanCutPercent(tier)");
        customs.Should().Contain("TradeStandingTier.Hostile");

        var risk = ReadServerSource("Service", "TradeOrbitRisk.cs");
        risk.Should().Contain("RollHostilePatrols(player, area, orbit)");
        risk.Should().Contain("TradeStandingRules.ShouldPatrolAttack(tier, last == default ? null : last, now)");

        var trade = ReadServerSource("Service", "Trade.cs");
        trade.Split("GetPriceBonus(player, exchangeType)").Length.Should().Be(3, "buying and selling both use standing");

        ReadServerSource("Service", "TradeFreight.cs").Should().Contain("GetFreightStanding(player, exchange)");
        ReadServerSource("Feature", "GuiDefinition", "ViewModel", "TradeExchangeViewModel.cs")
            .Should().Contain("CanAccept(offer, contracts, taken, capacity, freeVolume, credits, standing)");
    }

    [Test]
    public void Wiring_TheDmPickerSpawnsPiratesAndConvoys()
    {
        var hulls = SpaceSquadCatalog.Roles.Select(x => x.Hull).ToList();
        hulls.Should().Contain(new[] { "pirint", "pirftr", "pirgun", "convoy", "escort" });

        // The six faction tiers; the Rift Raiders (index 6) fly as pirates themselves and run no convoys.
        for (var faction = 0; faction < 6; faction++)
        {
            foreach (var hull in new[] { "pirint", "pirftr", "pirgun", "convoy", "escort" })
            {
                var role = hulls.IndexOf(hull);
                SpaceSquadCatalog.CreatureTag(faction, role).Should().Be($"t{faction + 1}{hull}", $"tier {faction + 1} {hull}");
            }
        }
    }

    // Hutt Cartel enforcers (W11b)

    [Test]
    public void HuttEnforcers_AttackOnlyPilotsHostileToTheCartel()
    {
        HuttEnforcerRules.Faction.Should().Be(FactionType.HuttCartel);
        HuttEnforcerRules.AttacksOnSight(TradeStandingTier.Hostile).Should().BeTrue();
        foreach (var tier in new[] { TradeStandingTier.Neutral, TradeStandingTier.Trusted, TradeStandingTier.Honored, TradeStandingTier.Exalted })
            HuttEnforcerRules.AttacksOnSight(tier).Should().BeFalse(tier.ToString());

        HuttEnforcerRules.AttacksOnSight(TradeStandingRules.GetTier(TradeTuning.StandingHostile)).Should().BeTrue();
        HuttEnforcerRules.AttacksOnSight(TradeStandingRules.GetTier(TradeTuning.StandingHostile + 1)).Should().BeFalse();

        HuttEnforcerRules.IsEnforcer("t4huttenfgun").Should().BeTrue();
        HuttEnforcerRules.IsEnforcer("t4huttenfint").Should().BeTrue();
        HuttEnforcerRules.IsEnforcer("t4gunship").Should().BeFalse("Clan Zel Arun is a hostile faction tier, not the Cartel");
        HuttEnforcerRules.IsEnforcer(null).Should().BeFalse();
        HuttEnforcerRules.GetGroup("area-1").Should().NotBe(HuttEnforcerRules.GetGroup("area-2"), "each orbit's enforcers are one group");
    }

    [Test]
    public void HuttEnforcers_PatrolSmugglersMoonAndTheOrbitsItsLanesReach()
    {
        TradeTuning.HuttEnforcerTier.Should().Be(4);
        TradeTuning.HuttEnforcerOrbits.Should().BeEquivalentTo("narshadorbit", "hutlar_orbit", "tatooineorbit");
        foreach (var orbit in TradeTuning.HuttEnforcerOrbits)
        {
            HuttEnforcerRules.PatrolsOrbit(orbit).Should().BeTrue(orbit);
            TradeLookup.GetOrbit(orbit).Should().NotBeNull(orbit);
        }

        HuttEnforcerRules.PatrolsOrbit("viscaraorbit").Should().BeFalse();
        TradeLookup.GetExchange(TradeExchangeType.Hutlar).StandingFaction.Should().Be(FactionType.HuttCartel);
        TradeLookup.GetExchange(TradeExchangeType.Tatooine).StandingFaction.Should().Be(FactionType.HuttCartel);

        var spawns = new SpaceSpawnDefinition().BuildSpawnTables();
        foreach (var table in new[] { "SPACE_NARSHADDAA_ORBIT", "SPACE_HUTLAR_ORBIT", "SPACE_TATOOINE_ORBIT" })
        {
            foreach (var tag in TradeTuning.HuttEnforcerTags)
                spawns[table].Spawns.Where(x => x.Resref == tag).Select(x => x.Weight)
                    .Should().Equal(new[] { TradeTuning.HuttEnforcerSpawnWeight }, $"{table} {tag}");
        }

        foreach (var (name, table) in spawns)
        {
            if (name is "SPACE_NARSHADDAA_ORBIT" or "SPACE_HUTLAR_ORBIT" or "SPACE_TATOOINE_ORBIT")
                continue;
            table.Spawns.Should().NotContain(x => HuttEnforcerRules.IsEnforcer(x.Resref), name);
        }
    }

    [Test]
    public void HuttEnforcers_AreNeutralTierFourCopiesOfTheGunshipAndInterceptor()
    {
        var registered = new NPCSpaceObjectDefinition().BuildSpaceObjects();
        foreach (var (tag, source, appearance) in new[] { ("t4huttenfgun", "t4gunship", 10107), ("t4huttenfint", "t4interceptor", 10153) })
        {
            registered.Should().ContainKey(tag);
            var copy = JObject.Parse(File.ReadAllText(Path.Combine(RepositoryRoot(), "Module", "utc", $"{tag}.utc.json")));
            var original = JObject.Parse(File.ReadAllText(Path.Combine(RepositoryRoot(), "Module", "utc", $"{source}.utc.json")));
            copy["Tag"]!["value"]!.Value<string>().Should().Be(tag);
            copy["TemplateResRef"]!["value"]!.Value<string>().Should().Be(tag);
            copy["FactionID"]!["value"]!.Value<int>().Should().Be(6, "Neutral, like the customs patrols, until a pilot crosses the Cartel");
            copy["Appearance_Type"]!["value"]!.Value<int>().Should().Be(appearance);
            copy["FirstName"]!["value"]!["0"]!.Value<string>().Should().StartWith("Hutt Cartel Enforcer");
            copy["Equip_ItemList"]!.ToString().Should().Be(original["Equip_ItemList"]!.ToString(), "the tier 4 skin sets its level");
            copy["VarTable"]!.ToString().Should().Be(original["VarTable"]!.ToString(), "it carries its source's loot");

            var npc = RevampNpcShipCatalog.Ships.Single(x => x.CreatureTag == tag);
            var sourceNpc = RevampNpcShipCatalog.Ships.Single(x => x.CreatureTag == source);
            npc.Level.Should().Be(sourceNpc.Level);
            npc.Role.Should().Be(sourceNpc.Role);
            npc.Hull.Should().Be(sourceNpc.Hull);
            ShipManufacturerRules.WreckManufacturer(tag).Should().Be(ShipManufacturer.Hutt, "Appendix D: Hutt Cargo Coupling from the enforcers' wrecks");
        }

        var palette = File.ReadAllText(Path.Combine(RepositoryRoot(), "Module", "itp", "creaturepalcus.itp.json"));
        palette.Should().Contain("\"t4huttenfgun\"").And.Contain("\"t4huttenfint\"");
    }

    [Test]
    public void Wiring_HuttEnforcersJoinTheirOrbitAndAnswerToTheCartel()
    {
        var enforcers = ReadServerSource("Service", "TradeEnforcers.cs");
        enforcers.Should().Contain("[NWNEventHandler(ScriptName.OnCreatureSpawnAfter)]");
        enforcers.Should().Contain("MarkLawful(ship, group, TradeAuthority.None, LawfulShipKind.HuttEnforcer);");
        enforcers.Should().Contain("HuttEnforcerRules.AttacksOnSight(GetStandingTier(player, HuttEnforcerRules.Faction))");
        ReadServerSource("Service", "TradeOrbitRisk.cs").Should().Contain("RollHuttEnforcers(player, area)");

        var convoys = ReadServerSource("Service", "TradeConvoys.cs");
        convoys.Should().Contain("if (kind == LawfulShipKind.HuttEnforcer)");
        convoys.Should().Contain("RecordStanding(attacker, HuttEnforcerRules.Faction, TradeStandingEvent.AttackedLawfulShip);");
        ((int)LawfulShipKind.HuttEnforcer).Should().Be(3);
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
