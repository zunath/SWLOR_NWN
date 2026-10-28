using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.ShipModuleDefinition;
using SWLOR.Game.Server.Feature.SpaceObjectDefinition;
using SWLOR.Game.Server.Feature.SpawnDefinition;
using SWLOR.Game.Server.Feature.TradeDefinition;
using SWLOR.Game.Server.Service.FactionService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Trade risk (plan section 13, W7b): customs scans and fines, the Smuggling Compartment, black markets, pirate
/// ambushes, trade convoys, freight contracts, the HoloNet board, lawful standing and the earning outcome after risk.
/// The numbers come from tools/SpaceRevampBalance/trade.py, which also writes the reference fixture these tests read.
/// </summary>
public class TradeRiskTests
{
    private static readonly DateTime Now = new(2026, 10, 27, 12, 0, 0, DateTimeKind.Utc);

    private static JObject Reference() => JObject.Parse(File.ReadAllText(
        Path.Combine(RepositoryRoot(), "SWLOR.Game.Server.Tests", "Fixtures", "Trade", "TradeRiskReference.json")));

    private static Dictionary<CommodityType, CargoLot> Cargo(params (CommodityType Commodity, int Units)[] lots) =>
        lots.ToDictionary(x => x.Commodity, x => new CargoLot { Units = x.Units, CostBasis = x.Units * 10 });

    private static Dictionary<CommodityType, CargoLot> Cargo(JObject cargo) =>
        cargo.Properties().ToDictionary(x => Enum.Parse<CommodityType>(x.Name), x => new CargoLot { Units = x.Value.Value<int>() });

    private static ShipNpcEntry Npc(string tag) => RevampNpcShipCatalog.Ships.SingleOrDefault(x => x.CreatureTag == tag);

    // Customs

    [Test]
    public void Customs_MatchesTheBalanceModel()
    {
        foreach (var token in Reference()["Cases"]!)
        {
            var authority = Enum.Parse<TradeAuthority>(token["Authority"]!.Value<string>()!);
            var cargo = Cargo((JObject)token["Cargo"]!);
            var rank = token["CompartmentRank"]!.Value<int>();
            var compartment = rank < 0 ? 0 : CustomsRules.GetCompartmentPercent(rank);
            var banned = CustomsRules.GetBannedValue(cargo, authority);

            banned.Should().Be(token["BannedValue"]!.Value<int>());
            CustomsRules.GetScanChance(banned, compartment).Should().BeApproximately(token["ScanChance"]!.Value<double>(), 1e-6);
            CustomsRules.GetFine(banned).Should().Be(token["Fine"]!.Value<int>());
        }
    }

    [Test]
    public void ScanChance_GrowsWithBannedValueAndIsCapped()
    {
        CustomsRules.GetScanChance(0, 0).Should().Be(0, "a clean ship is never stopped");
        CustomsRules.GetScanChance(5000, 0).Should().BeLessThan(CustomsRules.GetScanChance(50000, 0));
        CustomsRules.GetScanChance(int.MaxValue / 2, 0).Should().Be(TradeTuning.ScanMaximum);
        CustomsRules.GetScanChance(50000, 100).Should().Be(0);
    }

    [Test]
    public void Customs_PatrolsBanOnlyTheirAuthoritysGoods()
    {
        var cargo = Cargo((CommodityType.Moonspice, 10), (CommodityType.SithRelics, 5), (CommodityType.Grain, 40));

        CustomsRules.GetBannedValue(cargo, TradeAuthority.Sith).Should().Be(10 * 250, "the Sith ban spice only");
        CustomsRules.GetBannedValue(cargo, TradeAuthority.Republic).Should().Be(10 * 250 + 5 * 782);
        CustomsRules.GetBannedValue(cargo, TradeAuthority.None).Should().Be(0);
        CustomsRules.GetFine(1001).Should().Be((int)Math.Ceiling(1001 * TradeTuning.FineRate));
    }

    [Test]
    public void PayingTheFine_ConfiscatesOnlyTheBannedGoods()
    {
        var cargo = Cargo((CommodityType.Moonspice, 10), (CommodityType.SithRelics, 5), (CommodityType.Grain, 40));

        var taken = CustomsRules.Confiscate(cargo, TradeAuthority.Czerka);

        taken.Should().Equal(new Dictionary<CommodityType, int> { [CommodityType.Moonspice] = 10 });
        cargo.Keys.Should().BeEquivalentTo(new[] { CommodityType.SithRelics, CommodityType.Grain });
        CustomsRules.Confiscate(cargo, TradeAuthority.Republic).Should().ContainKey(CommodityType.SithRelics);
        cargo.Keys.Should().Equal(CommodityType.Grain);
    }

    [Test]
    public void LawfulOrbits_HaveTheirPatrolsAndPickOne()
    {
        var viscara = TradeLookup.GetOrbit("viscaraorbit")!;
        viscara.CustomsAuthorities.Should().Equal(TradeAuthority.Republic, TradeAuthority.Czerka);
        CustomsRules.PickPatrol(viscara, 0.0).Should().Be(TradeAuthority.Republic);
        CustomsRules.PickPatrol(viscara, 0.99).Should().Be(TradeAuthority.Czerka);
        TradeLookup.GetOrbit("space_korriban")!.CustomsAuthorities.Should().Equal(TradeAuthority.Sith);
        CustomsRules.PickPatrol(TradeLookup.GetOrbit("hutlar_orbit"), 0.5).Should().Be(TradeAuthority.None, "open ports have no customs");
        TradeLookup.GetOrbit("SPACE_KORRIBAN").Should().NotBeNull("area resrefs match without case");

        foreach (var orbit in TradeCatalog.Orbits)
        {
            orbit.Tier.Should().BeInRange(1, 6, orbit.AreaResref);
            foreach (var authority in orbit.CustomsAuthorities)
            {
                var tags = CustomsRules.GetPatrolTags(authority, orbit.Tier);
                tags.Should().HaveCount(2);
                Npc(tags[0])!.Role.Should().Be("Gunship", tags[0]);
                Npc(tags[1])!.Role.Should().Be("Interceptor", tags[1]);
            }
        }
    }

    [Test]
    public void SmugglingCompartment_CutsTheScanChance_BestOneCounts()
    {
        var steps = RevampModuleCatalog.Steps.Where(x => x.Family == ShipModuleFamily.SmugglingCompartment).ToList();
        steps.Should().HaveCount(10);
        foreach (var step in steps)
        {
            step.Row.Should().Be(ShipModulePowerType.Low, step.Tag);
            step.Skill.Should().Be(SWLOR.Game.Server.Service.SkillService.SkillType.Piloting);
            step.ScanReduction.Should().Be(CustomsRules.GetCompartmentPercent(step.Rank), step.Tag);
        }

        CustomsRules.GetCompartmentPercent(new[] { "sm_laser_00", "sm_plate_10" }).Should().Be(0);
        CustomsRules.GetCompartmentPercent(new[] { "sm_smug_00", "sm_smug_20" }).Should().Be(21);
        CustomsRules.GetCompartmentPercent(new[] { "sm_smug_00" }).Should().Be(15);
        CustomsRules.GetScanChance(40000, CustomsRules.GetCompartmentPercent(new[] { "sm_smug_45" }))
            .Should().BeApproximately(CustomsRules.GetScanChance(40000, 0) * 0.70, 1e-9);
    }

    [Test]
    public void BlackMarkets_PayAPremiumForBannedGoodsTheyDoNotMake()
    {
        TradeCatalog.Exchanges.Where(x => x.IsBlackMarket).Select(x => x.Type).Should()
            .BeEquivalentTo(new[] { TradeExchangeType.Hutlar, TradeExchangeType.Tatooine, TradeExchangeType.SmugglersMoon });

        var crates = TradeLookup.GetExchange(TradeExchangeType.SmugglersMoon)!.FindMarket(CommodityType.WeaponsCrates)!;
        var hops = TradeLookup.GetHops(TradeExchangeType.CZ220, TradeExchangeType.SmugglersMoon);
        crates.PriceFactor.Should().BeApproximately(Math.Min(1.40, 1.05 + 0.04 * hops) * 1.10 * TradeTuning.BlackMarketPremium, 0.001);

        TradeLookup.GetExchange(TradeExchangeType.SmugglersMoon)!.FindMarket(CommodityType.Moonspice)!.PriceFactor
            .Should().Be(0.85, "the maker's own price has no premium");
        TradeLookup.GetExchange(TradeExchangeType.Hutlar)!.FindMarket(CommodityType.Grain)!.PriceFactor
            .Should().BeLessThan(1.0, "legal goods carry no premium");
    }

    // Pirates

    [Test]
    public void Ambush_MatchesTheBalanceModel()
    {
        foreach (var token in Reference()["Cases"]!)
        {
            var value = token["CargoValue"]!.Value<int>();
            var tier = token["Tier"]!.Value<int>();
            PirateAmbushRules.GetCargoValue(Cargo((JObject)token["Cargo"]!), null).Should().Be(value);
            PirateAmbushRules.GetAmbushChance(value, tier).Should().BeApproximately(token["AmbushChance"]!.Value<double>(), 1e-6);
            PirateAmbushRules.GetSquadSize(value, tier).Should().Be(token["SquadSize"]!.Value<int>());
        }
    }

    [Test]
    public void Ambush_NeedsValuableCargoAndGrowsWithValueAndTier()
    {
        PirateAmbushRules.GetAmbushChance(TradeTuning.AmbushThreshold, 6).Should().Be(0);
        PirateAmbushRules.GetAmbushChance(TradeTuning.AmbushThreshold + 10000, 1).Should()
            .BeLessThan(PirateAmbushRules.GetAmbushChance(TradeTuning.AmbushThreshold + 10000, 6));
        PirateAmbushRules.GetAmbushChance(TradeTuning.AmbushThreshold + 10000, 3).Should()
            .BeLessThan(PirateAmbushRules.GetAmbushChance(TradeTuning.AmbushThreshold + 40000, 3));
        PirateAmbushRules.GetAmbushChance(int.MaxValue, 6).Should().BeApproximately(TradeTuning.AmbushMaximum * 1.4, 1e-9);

        var sealedFreight = new[] { new SealedFreightLot { ContractId = "a", Volume = 100 } };
        PirateAmbushRules.GetCargoValue(Cargo((CommodityType.Grain, 10)), sealedFreight)
            .Should().Be(10 * 60 + 100 * TradeTuning.SealedValuePerVolume, "pirates count sealed freight too");

        PirateAmbushRules.GetSquadSize(0, 1).Should().Be(TradeTuning.AmbushSquadMinimum);
        PirateAmbushRules.GetSquadSize(int.MaxValue, 6).Should().Be(TradeTuning.AmbushSquadMaximum);
        PirateAmbushRules.GetSquadSize(TradeTuning.AmbushThreshold + 1, 4).Should().Be(TradeTuning.AmbushSquadMinimum + 1);
    }

    [Test]
    public void EveryAmbushSquad_LeadsWithAnInterceptor()
    {
        for (var tier = 1; tier <= 6; tier++)
        {
            for (var size = TradeTuning.AmbushSquadMinimum; size <= TradeTuning.AmbushSquadMaximum; size++)
            {
                var squad = PirateAmbushRules.GetSquad(tier, size);
                squad.Should().HaveCount(size);
                squad[0].Should().Be($"t{tier}pirint");
                squad.Count(x => x.EndsWith("pirgun")).Should().Be(size >= 4 ? 1 : 0);
                foreach (var tag in squad)
                    Npc(tag).Should().NotBeNull(tag);
            }

            Npc($"t{tier}pirint")!.Role.Should().Be("Interceptor");
            Npc($"t{tier}pirftr")!.Role.Should().Be("Fighter");
            Npc($"t{tier}pirgun")!.Role.Should().Be("Gunship");
            Npc($"t{tier}pirint")!.Level.Should().Be(Npc($"t{tier}interceptor")!.Level, "raiders fight at their tier's level");
        }
    }

    // Convoys

    [Test]
    public void Convoys_CarryTheirTiersValueOfLegalExports()
    {
        for (var tier = 1; tier <= 6; tier++)
        {
            ConvoyRules.GetHaulerTier(ConvoyRules.GetHaulerTag(tier)).Should().Be(tier);
            ConvoyRules.GetHaulerTier(ConvoyRules.GetEscortTag(tier)).Should().Be(0);
            Npc(ConvoyRules.GetHaulerTag(tier))!.Size.Should().Be(ShipSize.Medium);
            Npc(ConvoyRules.GetEscortTag(tier))!.Role.Should().Be("Gunship");
            ConvoyRules.GetEscortCount(tier).Should().BeInRange(1, 3);

            var cargo = ConvoyRules.BuildCargo(tier, "viscaraorbit", new System.Random(tier));
            cargo.Should().NotBeEmpty();
            cargo.Keys.Should().OnlyContain(x => TradeLookup.GetCommodity(x)!.Legality == CommodityLegality.Legal);
            cargo.Keys.Should().OnlyContain(x => TradeCatalog.Exchanges.Any(e =>
                e.OrbitResref == "viscaraorbit" && e.FindMarket(x)!.Role == CommodityRole.Producer));
            var value = cargo.Sum(x => x.Value * TradeLookup.GetCommodity(x.Key)!.BasePrice);
            // Each pick rounds down to whole units, so it falls short by less than one unit of it.
            var rounding = cargo.Keys.Sum(x => TradeLookup.GetCommodity(x)!.BasePrice);
            value.Should().BeInRange(ConvoyRules.GetCargoValue(tier) - rounding, ConvoyRules.GetCargoValue(tier));
        }

        ConvoyRules.GetCandidateGoods("space_midrim1").Should().OnlyContain(x => TradeLookup.GetCommodity(x)!.Legality == CommodityLegality.Legal);
        ConvoyRules.GetAuthority(TradeLookup.GetOrbit("space_korriban")).Should().Be(TradeAuthority.Sith);
        ConvoyRules.GetAuthority(TradeLookup.GetOrbit("tatooineorbit")).Should().Be(TradeAuthority.Czerka);
    }

    [Test]
    public void RaidedConvoy_SpillsItsWholeCargo()
    {
        var cargo = ConvoyRules.BuildCargo(6, "space_dathomir", new System.Random(3));
        var lots = cargo.ToDictionary(x => x.Key, x => new CargoLot { Units = x.Value });

        var pods = CargoSpillRules.Split(lots, TradeTuning.SpillMaximumContainers);

        pods.SelectMany(x => x.Units).GroupBy(x => x.Key).ToDictionary(x => x.Key, x => x.Sum(y => y.Value))
            .Should().Equal(cargo);
    }

    // Freight contracts

    [Test]
    public void FreightOffers_AreTheSameForEveryoneUntilTheHourChanges()
    {
        var offers = FreightContractRules.GetOffers(TradeExchangeType.Viscara, Now);
        offers.Should().HaveCount(TradeTuning.FreightOffersPerExchange);
        offers.Select(x => x.Volume).Should().Equal(TradeTuning.FreightOfferVolumes);
        offers.Should().OnlyContain(x => x.Destination != TradeExchangeType.Viscara && x.Hops >= 1);
        foreach (var offer in offers)
        {
            offer.Fee.Should().Be(FreightContractRules.GetFee(offer.Volume, offer.Hops));
            offer.Deposit.Should().Be((int)Math.Ceiling(offer.Fee * TradeTuning.FreightDepositRate));
            offer.DeadlineMinutes.Should().Be(TradeTuning.FreightDeadlineBaseMinutes + TradeTuning.FreightDeadlinePerHopMinutes * offer.Hops);
        }

        FreightContractRules.GetOffers(TradeExchangeType.Viscara, Now.AddMinutes(10)).Select(x => x.Id)
            .Should().Equal(offers.Select(x => x.Id));
        FreightContractRules.GetOffers(TradeExchangeType.Viscara, FreightContractRules.GetNextOfferChange(Now)).Select(x => x.Id)
            .Should().NotIntersectWith(offers.Select(x => x.Id));
        FreightContractRules.GetFee(100, 3).Should().Be((int)Math.Round(100 * (0.70 + 1.05 * 3)));
    }

    [Test]
    public void FreightContract_Lifecycle_DepositDeadlineAndSealedLoad()
    {
        var offer = FreightContractRules.GetOffers(TradeExchangeType.Hutlar, Now)[1];
        var active = new List<FreightContract>();
        var taken = new List<string>();

        FreightContractRules.CanAccept(offer, active, taken, 480, offer.Volume - 1, 100000).Should().Be(FreightRefusal.NotEnoughRoom);
        FreightContractRules.CanAccept(offer, active, taken, 0, 0, 100000).Should().Be(FreightRefusal.NoCargoBay);
        FreightContractRules.CanAccept(offer, active, taken, 480, 480, offer.Deposit - 1).Should().Be(FreightRefusal.NotEnoughCredits);
        FreightContractRules.CanAccept(offer, active, taken, 480, 480, offer.Deposit).Should().Be(FreightRefusal.None);

        var contract = FreightContractRules.Accept(offer, "ship-1", Now);
        active.Add(contract);
        taken.Add(offer.Id);
        contract.Deadline.Should().Be(Now.AddMinutes(offer.DeadlineMinutes));
        FreightContractRules.CanAccept(offer, active, taken, 480, 480, 100000).Should().Be(FreightRefusal.AlreadyTaken);

        // The sealed load fills volume, cannot be sold and is not scanned.
        var ship = new PlayerShip();
        ship.SealedFreight.Add(FreightContractRules.GetSealedLot(contract));
        FreightContractRules.GetSealedVolume(ship.SealedFreight).Should().Be(offer.Volume);
        CargoHoldRules.GetUsedVolume(ship.Cargo).Should().Be(0, "sealed freight is not a commodity");
        TradeRules.PlanSell(TradeLookup.GetCommodity(CommodityType.Grain), TradeLookup.GetExchange(offer.Destination)!.Markets[0], 0, 10, 10,
            CargoHoldRules.GetUnits(ship.Cargo, CommodityType.Grain)).Refusal.Should().Be(TradeRefusal.NoneAboard);
        CustomsRules.GetBannedValue(ship.Cargo, TradeAuthority.Republic).Should().Be(0);

        FreightContractRules.Deliver(contract, offer.Origin, ship.SealedFreight, Now).Refusal.Should().Be(FreightRefusal.WrongExchange);
        FreightContractRules.Deliver(contract, offer.Destination, new List<SealedFreightLot>(), Now).Refusal.Should().Be(FreightRefusal.LoadNotAboard);

        var onTime = FreightContractRules.Deliver(contract, offer.Destination, ship.SealedFreight, contract.Deadline);
        (onTime.Refusal, onTime.Fee, onTime.DepositReturned, onTime.IsLate).Should().Be((FreightRefusal.None, offer.Fee, offer.Deposit, false));

        var late = FreightContractRules.Deliver(contract, offer.Destination, ship.SealedFreight, contract.Deadline.AddSeconds(1));
        (late.Fee, late.DepositReturned, late.IsLate).Should().Be((offer.Fee, 0, true), "a late delivery forfeits the deposit");
    }

    [Test]
    public void FreightContracts_AreLimitedAndTakenOffersExpire()
    {
        var offers = FreightContractRules.GetOffers(TradeExchangeType.MonCala, Now);
        var active = offers.Take(TradeTuning.FreightMaximumActive).Select(x => FreightContractRules.Accept(x, "s", Now)).ToList();
        var another = FreightContractRules.GetOffers(TradeExchangeType.Dantooine, Now)[0];
        FreightContractRules.CanAccept(another, active, new List<string>(), 2000, 2000, 100000).Should().Be(FreightRefusal.TooManyContracts);

        var taken = new List<string> { offers[0].Id, $"MonCala-{FreightContractRules.GetOfferWindow(Now) - 2}-0", "bad" };
        FreightContractRules.PruneTaken(taken, Now);
        taken.Should().Equal(offers[0].Id);
    }

    [Test]
    public void FreightContracts_PersistOnThePlayerAndSealedLoadsOnTheShip()
    {
        var player = new Player();
        player.FreightContracts.Add(FreightContractRules.Accept(FreightContractRules.GetOffers(TradeExchangeType.Tatooine, Now)[2], "ship", Now));
        player.FreightOffersTaken.Add(player.FreightContracts[0].Id);
        var ship = new PlayerShip();
        ship.SealedFreight.Add(FreightContractRules.GetSealedLot(player.FreightContracts[0]));

        var loadedPlayer = JsonConvert.DeserializeObject<Player>(JsonConvert.SerializeObject(player))!;
        var loadedShip = JsonConvert.DeserializeObject<PlayerShip>(JsonConvert.SerializeObject(ship))!;

        loadedPlayer.FreightContracts.Should().BeEquivalentTo(player.FreightContracts);
        loadedPlayer.FreightOffersTaken.Should().Equal(player.FreightOffersTaken);
        loadedShip.SealedFreight.Should().BeEquivalentTo(ship.SealedFreight);

        JsonConvert.DeserializeObject<Player>("{}")!.FreightContracts.Should().BeEmpty("older records load with none");
        JsonConvert.DeserializeObject<PlayerShip>("{}")!.SealedFreight.Should().BeEmpty();
    }

    // HoloNet

    [Test]
    public void HoloNet_RunsStaleUntilItsRefreshPeriodPasses()
    {
        HoloNetRules.IsStale(default, Now).Should().BeTrue();
        HoloNetRules.IsStale(Now, Now.AddMinutes(TradeTuning.HoloNetRefreshMinutes - 1)).Should().BeFalse();
        HoloNetRules.IsStale(Now, Now.AddMinutes(TradeTuning.HoloNetRefreshMinutes)).Should().BeTrue();
        HoloNetRules.GetAgeMinutes(Now, Now.AddMinutes(12.5)).Should().Be(12);
    }

    [Test]
    public void HoloNet_ShowsTheCheapestAndBestPayingExchanges()
    {
        var board = HoloNetRules.Build((_, _) => 0, Now);
        board.CapturedAt.Should().Be(Now);
        board.Rows.Should().HaveCount(TradeCatalog.Commodities.Count);

        var silk = board.Rows.Single(x => x.Commodity == CommodityType.KinrathSilk);
        silk.BuyAt.Should().Be(TradeExchangeType.Viscara, "the exporter sells cheapest");
        silk.SellPrice.Should().BeGreaterThan(silk.BuyPrice);

        // Flooding an exchange moves it off the board.
        var flooded = HoloNetRules.Build((exchange, commodity) =>
            exchange == silk.SellAt && commodity == CommodityType.KinrathSilk ? 5000 : 0, Now);
        flooded.Rows.Single(x => x.Commodity == CommodityType.KinrathSilk).SellAt.Should().NotBe(silk.SellAt);
    }

    // Standing and outcome

    [Test]
    public void Standing_OffensesCostLawfulStandingAndFreightEarnsIt()
    {
        TradeStandingRules.GetFaction(TradeAuthority.Republic).Should().Be(FactionType.RepublicMilitary);
        TradeStandingRules.GetFaction(TradeAuthority.Czerka).Should().Be(FactionType.Czerka);
        TradeStandingRules.GetFaction(TradeAuthority.Sith).Should().Be(FactionType.SithMilitary);
        TradeStandingRules.GetFaction(TradeAuthority.None).Should().Be(FactionType.Invalid);

        foreach (var offense in new[] { TradeStandingEvent.CaughtSmuggling, TradeStandingEvent.RanFromCustoms,
                     TradeStandingEvent.AttackedLawfulShip, TradeStandingEvent.RaidedConvoy })
            TradeStandingRules.GetAmount(offense).Should().BeNegative(offense.ToString());

        TradeStandingRules.GetAmount(TradeStandingEvent.RanFromCustoms).Should()
            .BeLessThan(TradeStandingRules.GetAmount(TradeStandingEvent.CaughtSmuggling), "running costs more than paying");
        TradeStandingRules.GetAmount(TradeStandingEvent.DeliveredFreight).Should().BePositive();
        TradeLookup.GetExchange(TradeExchangeType.Dantooine)!.StandingFaction.Should().Be(FactionType.RepublicMilitary);
        TradeLookup.GetExchange(TradeExchangeType.SmugglersMoon)!.StandingFaction.Should().Be(FactionType.Smuggler);
    }

    [Test]
    public void EarningsAfterRisk_MeetThePlanTargets()
    {
        var outcome = Reference()["Outcome"]!;

        outcome["ContrabandPercent"]!.Value<int>().Should().BeInRange(150, 200, "contraband after customs is 150-200% of T3 combat");
        outcome["CompartmentPercent"]!.Value<int>().Should().BeInRange(outcome["ContrabandPercent"]!.Value<int>(), 200);
        outcome["LegalPercent"]!.Value<int>().Should().BeInRange(100, 125, "open trading is 100-125%");
        outcome["FreightPercent"]!.Value<int>().Should().BeInRange(65, 85, "freight contracts are about 75%");
    }

    // Wiring

    [Test]
    public void Wiring_OrbitEntryDamageSpawnsAndWindows()
    {
        var risk = ReadServerSource("Service", "TradeOrbitRisk.cs");
        risk.Should().Contain("[NWNEventHandler(ScriptName.OnAreaEnter)]");

        var space = ReadServerSource("Service", "Space.cs");
        space.Split("Trade.OnShipAttacked(attacker, target);").Length.Should().Be(3, "both ship damage paths report attacks");

        var convoys = ReadServerSource("Service", "TradeConvoys.cs");
        convoys.Should().Contain("[NWNEventHandler(ScriptName.OnCreatureSpawnAfter)]");
        convoys.Should().Contain("[NWNEventHandler(ScriptName.OnCreatureDeathAfter)]");
        convoys.Should().Contain("SpillUnits(units, GetLocation(hauler))");

        ReadServerSource("Service", "TradeCargoSpill.cs").Should().Contain("FailSealedFreight(ship);");
        ReadServerSource("Feature", "GuiDefinition", "DMToolsDefinition.cs").Should().Contain("model.OnStartConvoy()");
        ReadServerSource("Feature", "ChatCommandDefinition", "SpaceChatCommand.cs").Should().Contain("GuiWindowType.HoloNetBoard");
        ReadServerSource("Feature", "GuiDefinition", "ViewModel", "ShipManagementViewModel.cs").Should().Contain("dbShip.SealedFreight.Count > 0");

        var spawns = new SpaceSpawnDefinition().BuildSpawnTables();
        foreach (var table in new[] { "SPACE_VISCARA_ORBIT", "SPACE_MONCALA_ORBIT", "SPACE_HUTLAR_ORBIT", "SPACE_TATOOINE_ORBIT",
                     "SPACE_KORRIBAN_ORBIT", "SPACE_DATHOMIR_ORBIT", "SPACE_DANTOOINE_ORBIT", "SPACE_VISCARA_BELT",
                     "SPACE_MIDRIM_NEBULA", "SPACE_KORRIBAN_DEBRIS", "SPACE_NARSHADDAA_ORBIT", "SPACE_MIDRIM_1", "SPACE_MIDRIM_2" })
        {
            var objects = spawns[table].Spawns;
            var convoy = objects.Where(x => x.Resref.EndsWith("convoy")).ToList();
            convoy.Should().NotBeEmpty(table);
            convoy.Sum(x => x.Weight).Should().BeLessThan(objects.Sum(x => x.Weight) / 50, $"{table} spawns convoys at a low rate");
        }

        var objectsDefined = new NPCSpaceObjectDefinition().BuildSpaceObjects();
        foreach (var tag in RevampNpcShipCatalog.Ships.Select(x => x.CreatureTag).Where(x =>
                     x.Contains("pir") || x.Contains("cus") || x.EndsWith("convoy") || x.EndsWith("escort")))
        {
            objectsDefined.Should().ContainKey(tag);
            File.Exists(Path.Combine(RepositoryRoot(), "Module", "utc", $"{tag}.utc.json")).Should().BeTrue(tag);
        }
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
