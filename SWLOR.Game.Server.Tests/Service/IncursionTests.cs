using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.ShipDefinition;
using SWLOR.Game.Server.Feature.ShipRewardDefinition;
using SWLOR.Game.Server.Feature.SpaceObjectDefinition;
using SWLOR.Game.Server.Feature.TradeDefinition;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.StationService;
using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Incursions (plan section 30): the weekly schedule, region selection, what each area spawns, blockaded docks and
/// closed exchanges, the price spike, contribution tracking and the payout. Numbers come from
/// tools/SpaceRevampBalance/events.py (IncursionTuning).
/// </summary>
public class IncursionTests
{
    private static readonly DateTime Wednesday = new(2026, 10, 28, 12, 0, 0, DateTimeKind.Utc);

    private static string Root()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        directory.Should().NotBeNull();
        return directory!.FullName;
    }

    private static string ReadSource(params string[] relativePath) =>
        File.ReadAllText(relativePath.Aggregate(Path.Combine(Root(), "SWLOR.Game.Server"), Path.Combine));

    private static Func<int, int> Rolls(params int[] values)
    {
        var index = 0;
        return max => Math.Min(values[Math.Min(index++, values.Length - 1)], max - 1);
    }

    private static IncursionRegion Region(string center) => IncursionRules.FindRegion(center);

    // Schedule

    [Test]
    public void Schedule_IsTheWeeklySlot()
    {
        var next = IncursionRules.NextStart(Wednesday);
        next.Should().Be(new DateTime(2026, 10, 31, IncursionTuning.StartHourUtc, 0, 0, DateTimeKind.Utc));
        next.DayOfWeek.Should().Be(IncursionTuning.StartDay);
        IncursionRules.NextStart(next).Should().Be(next.AddDays(7), "a slot that has come schedules the week after");
        IncursionRules.NextStart(next.AddSeconds(-1)).Should().Be(next);

        IncursionRules.IsDue(default, Wednesday).Should().BeFalse("no schedule yet");
        IncursionRules.IsDue(next, next.AddSeconds(-1)).Should().BeFalse();
        IncursionRules.IsDue(next, next).Should().BeTrue();
        IncursionRules.IsMissed(next, next.AddHours(IncursionTuning.DurationHours - 1)).Should().BeFalse("a late boot still starts it");
        IncursionRules.IsMissed(next, next.AddHours(IncursionTuning.DurationHours)).Should().BeTrue("a whole window down skips the week");
    }

    [Test]
    public void Schedule_IsSavedAndTheRunningIncursionResumes()
    {
        var state = new IncursionState { NextStartAt = Wednesday, LastCenter = "viscaraorbit" };
        state.Active = IncursionRules.Create(Region("viscaraorbit"), IncursionInvader.Sith, Wednesday, new Dictionary<string, int> { ["viscaraorbit:Viscara_Orbit"] = 1 });
        state.Active.Areas["viscaraorbit"].WavesCleared = 3;
        IncursionRules.AddPoints(state.Active.Contributions["p1"] = new IncursionContribution { Name = "Pilot" }, 2, 12.5);
        state.PendingPayouts["p2"] = new IncursionPayout { Credits = 100, Pulls = { ["SPACE_ELITE_T1"] = 2 } };
        state.PendingBattlePayouts["p3"] = new IncursionPayout { Credits = 300, Pulls = { ["SPACE_ELITE_T5"] = 1 } };

        var copy = Newtonsoft.Json.JsonConvert.DeserializeObject<IncursionState>(Newtonsoft.Json.JsonConvert.SerializeObject(state))!;
        copy.Id.Should().Be(IncursionState.RecordId);
        copy.NextStartAt.Should().Be(Wednesday);
        copy.Active!.Invader.Should().Be(IncursionInvader.Sith);
        copy.Active.Areas["viscaraorbit"].WavesCleared.Should().Be(3);
        copy.Active.Contributions["p1"].Points[2].Should().Be(12.5);
        copy.Active.Blockades.Should().ContainKey("viscaraorbit:Viscara_Orbit");
        copy.PendingPayouts["p2"].Pulls["SPACE_ELITE_T1"].Should().Be(2);
        copy.PendingBattlePayouts["p3"].Pulls["SPACE_ELITE_T5"].Should().Be(1);

        var source = ReadSource("Service", "SpaceIncursions.cs");
        source.Should().Contain("IncursionRules.IsMissed(state.NextStartAt, now)").And.Contain("SpawnIncursion();")
            .And.Contain("DeliverPendingIncursionPayout");
    }

    // Regions

    [Test]
    public void Regions_AreAnOrbitWithADockAndItsNeighbours()
    {
        IncursionTuning.Regions.Select(x => x.Center).Should().BeEquivalentTo(TradeCatalog.Exchanges.Select(x => x.OrbitResref).Distinct());
        foreach (var region in IncursionTuning.Regions)
        {
            region.Areas[0].Resref.Should().Be(region.Center);
            region.Tier.Should().Be(TradeCatalog.Orbits.Single(x => x.AreaResref == region.Center).Tier);
            region.Areas.Should().HaveCountGreaterThan(1, region.Name);
            foreach (var area in region.Areas)
            {
                area.Tier.Should().Be(TradeCatalog.Orbits.Single(x => x.AreaResref == area.Resref).Tier, area.Resref);
                File.Exists(Path.Combine(Root(), "Module", "git", $"{area.Resref}.git.json")).Should().BeTrue(area.Resref);
            }
        }

        Region("space_korriban").Areas.Select(x => x.Resref).Should().BeEquivalentTo("space_korriban", "space_midrim2", "space_korrdebris");
    }

    [Test]
    public void RegionSelection_NeverRepeatsLastWeekAndPicksAnInvader()
    {
        for (var roll = 0; roll < IncursionTuning.Regions.Count; roll++)
            IncursionRules.PickRegion(IncursionTuning.Regions, "viscaraorbit", Rolls(roll))!.Center.Should().NotBe("viscaraorbit");

        IncursionRules.PickRegion(IncursionTuning.Regions, string.Empty, Rolls(0))!.Center.Should().Be(IncursionTuning.Regions[0].Center);
        IncursionRules.PickInvader(Rolls(0)).Should().Be(IncursionInvader.Sith);
        IncursionRules.PickInvader(Rolls(1)).Should().Be(IncursionInvader.Mandalorian);
    }

    // Spawns

    [Test]
    public void Spawns_AreTheInvadersArmadaShipsAndTheFactionCapitals()
    {
        var presets = RevampNpcShipCatalog.Ships.ToDictionary(x => x.CreatureTag);
        foreach (var invader in new[] { IncursionInvader.Sith, IncursionInvader.Mandalorian })
        {
            for (var tier = 1; tier <= 6; tier++)
            {
                foreach (var (tag, hull) in IncursionRules.WaveShips(tier, invader).Concat(IncursionRules.BlockadeShips(tier, invader))
                             .Concat(IncursionRules.CommandShips(tier, invader)))
                {
                    presets.Should().ContainKey(tag, $"{invader} tier {tier} {hull}");
                    var source = hull == "capital" && tier >= 6 ? tag : $"t{tier}{hull}";
                    presets[tag].Should().BeEquivalentTo(presets[source], o => o.Excluding(x => x.CreatureTag),
                        "an armada ship fights exactly as the tier's role ship it copies");
                }

                IncursionRules.BlockadeShips(tier, invader).Select(x => presets[x.Tag].Difficulty).Should().Contain("Elite", "the blockade's defense platform");
                presets[IncursionRules.CommandTag(tier, invader)].Difficulty.Should().Be("Boss");
            }
        }

        IncursionRules.WaveShips(3, IncursionInvader.Sith).Select(x => x.Tag).Should().Equal("t3sitharmftr", "t3sitharmftr", "t3sitharmint", "t3sitharmbmb");
        IncursionRules.BlockadeShips(1, IncursionInvader.Mandalorian).Select(x => x.Tag).Should().Equal("t1mandoarmgun", "t1mandoarmgun", "t1mandoarmplt");
        IncursionRules.CommandTag(6, IncursionInvader.Sith).Should().Be("sithcap3");
        IncursionRules.CommandTag(6, IncursionInvader.Mandalorian).Should().Be("mandocap1");
        IncursionRules.CommandTag(2, IncursionInvader.Mandalorian).Should().Be("t2mandoarmcmd");
        IncursionRules.ShipName(IncursionInvader.Sith, "fighter").Should().Be("Sith Armada Fighter");
        IncursionRules.ShipName(IncursionInvader.Mandalorian, "capital").Should().Be("Mandalorian Armada Command Ship");
    }

    [Test]
    public void ArmadaCreatures_CopyTheirRoleShipWithTheArmadasNameAndHulls()
    {
        IncursionTuning.ArmadaShips.Should().HaveCount(66);
        IncursionTuning.ArmadaShips.Select(x => x.Tag).Should().OnlyHaveUniqueItems();
        var definitions = new NPCSpaceObjectDefinition().BuildSpaceObjects();
        var appearances = new Dictionary<IncursionInvader, Dictionary<string, int>>
        {
            [IncursionInvader.Sith] = new() { ["ftr"] = 10092, ["int"] = 10091, ["bmb"] = 10078, ["gun"] = 10082, ["plt"] = 2152, ["cmd"] = 10141 },
            [IncursionInvader.Mandalorian] = new() { ["ftr"] = 10105, ["int"] = 10161, ["bmb"] = 10106, ["gun"] = 10107, ["plt"] = 2152, ["cmd"] = 10154 },
        };
        var hulls = IncursionTuning.ArmadaHullCodes.ToDictionary(x => x.Value, x => x.Key);
        foreach (var (tag, itemTag) in IncursionTuning.ArmadaShips)
        {
            tag.Length.Should().BeLessThanOrEqualTo(16);
            definitions.Should().ContainKey(tag);
            var invader = IncursionTuning.Invaders.Single(x => tag.Substring(2).StartsWith(x.TagPrefix));
            var code = tag.Substring(2 + invader.TagPrefix.Length);
            var source = $"{tag.Substring(0, 2)}{hulls[code]}";
            using var utc = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root(), "Module", "utc", $"{tag}.utc.json")));
            using var original = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root(), "Module", "utc", $"{source}.utc.json")));
            var root = utc.RootElement;
            root.GetProperty("TemplateResRef").GetProperty("value").GetString().Should().Be(tag);
            root.GetProperty("FirstName").GetProperty("value").GetProperty("0").GetString()
                .Should().Be(IncursionRules.ShipName(invader.Invader, hulls[code]));
            root.GetProperty("Appearance_Type").GetProperty("value").GetInt32().Should().Be(appearances[invader.Invader][code]);
            foreach (var field in new[] { "Equip_ItemList", "VarTable", "FactionID", "ScriptSpawn", "ScriptDeath" })
                root.GetProperty(field).GetRawText().Should().Be(original.RootElement.GetProperty(field).GetRawText(), $"{tag} {field}");
            definitions[tag].ShipItemTag.Should().Be(definitions[source].ShipItemTag);
            itemTag.Should().Be(definitions[source].ShipItemTag);
        }

        var spawner = ReadSource("Service", "SpaceIncursions.cs");
        spawner.Should().NotContain("SetName(ship", "the armada creatures carry their own names");
        spawner.Should().NotContain("SetCreatureAppearanceType", "the armada creatures carry their own hulls");
    }

    [Test]
    public void CapitalBattles_DropAnArmadaCommandGroupIntoATier5Or6Orbit()
    {
        IncursionTuning.CapitalBattleAreas.Select(x => x.Resref).Should().Equal("space_dathomir", "dantooineorbit", "eshanorbit", "space_korriban");
        IncursionTuning.CapitalBattleAreas.Should().OnlyContain(x => x.Tier >= 5);
        foreach (var area in IncursionTuning.CapitalBattleAreas)
            TradeCatalog.Orbits.Single(x => x.AreaResref == area.Resref).Tier.Should().Be(area.Tier);
        CapitalBattleRules.PickArea(_ => 2).Resref.Should().Be("eshanorbit");
        CapitalBattleRules.PickArea(_ => 3).Resref.Should().Be("space_korriban");
        CapitalBattleRules.PickArea(_ => 99).Resref.Should().Be("space_korriban", "a roll past the end is clamped");

        var korriban = IncursionTuning.CapitalBattleAreas[3];
        CapitalBattleRules.Ships(korriban, IncursionInvader.Sith).Select(x => x.Tag).Should().Equal("sithcap3", "t6sitharmgun", "t6sitharmgun");
        CapitalBattleRules.Ships(IncursionTuning.CapitalBattleAreas[0], IncursionInvader.Mandalorian).Select(x => x.Tag)
            .Should().Equal("t5mandoarmcmd", "t5mandoarmgun", "t5mandoarmgun");

        var now = Wednesday;
        CapitalBattleRules.NextStart(now).Should().Be(now.AddHours(4));
        CapitalBattleRules.GetStep(3, now, now.AddMinutes(30)).Should().Be(CapitalBattleStep.Running);
        CapitalBattleRules.GetStep(0, now, now.AddMinutes(30)).Should().Be(CapitalBattleStep.Destroyed);
        CapitalBattleRules.GetStep(1, now.AddMinutes(30), now.AddMinutes(30)).Should().Be(CapitalBattleStep.Withdraw);
        IncursionTuning.CapitalBattleMinutes.Should().Be(30);

        var source = ReadSource("Service", "SpaceCapitalBattles.cs");
        source.Should().Contain("if (GetActiveIncursion() != null)", "no capital battle while an incursion runs");
        source.Should().Contain("CapitalBattleRules.Ships(area, invader)");
        source.Should().Contain("var paid = PayCapitalBattle();", "destroying the group pays its contributors (W11p; CapitalBattlePayoutTests)");
        ReadSource("Service", "SpaceIncursions.cs").Should().Contain("ProcessCapitalBattle(now);");
    }

    // Blockades, docking and prices

    [Test]
    public void Blockades_RefuseDockingAndCloseTheExchangeUntilBroken()
    {
        var viscara = IncursionRules.DockKey("viscaraorbit", "Viscara_Orbit");
        var czerka = IncursionRules.DockKey("viscaraorbit", "CZ220_Orbit");
        var incursion = IncursionRules.Create(Region("viscaraorbit"), IncursionInvader.Sith, Wednesday,
            new Dictionary<string, int> { [viscara] = (int)PlanetType.Viscara, [czerka] = (int)PlanetType.CZ220 });

        IncursionRules.IsBlockadeHolding(incursion, viscara).Should().BeTrue();
        IncursionRules.IsBlockadeHolding(incursion, IncursionRules.DockKey("hutlar_orbit", "Hutlar_Orbit")).Should().BeFalse();
        IncursionRules.IsBlockadeHolding(null, viscara).Should().BeFalse();
        IncursionRules.IsExchangeClosed(incursion, TradeExchangeType.Viscara).Should().BeTrue();
        IncursionRules.IsExchangeClosed(incursion, TradeExchangeType.CZ220).Should().BeTrue();
        IncursionRules.IsExchangeClosed(incursion, TradeExchangeType.MonCala).Should().BeFalse("Mon Cala's dock is in the region but holds no blockade here");

        incursion.BrokenBlockades.Add(viscara);
        IncursionRules.IsBlockadeHolding(incursion, viscara).Should().BeFalse();
        IncursionRules.IsExchangeClosed(incursion, TradeExchangeType.Viscara).Should().BeFalse();
        IncursionRules.IsExchangeClosed(incursion, TradeExchangeType.CZ220).Should().BeTrue();
        IncursionRules.DockRefusal(IncursionInvader.Sith).Should().Contain("Sith Armada blockades this dock");

        ReadSource("Feature", "DialogDefinition", "StarportDockDialog.cs").Should().Contain("Space.GetIncursionDockRefusal(self)");
        ReadSource("Feature", "DialogDefinition", "StarportDialog.cs").Should().Contain("Space.GetIncursionExchangeRefusal(exchange)")
            .And.Contain("Space.GetIncursionExchangeRefusal(dock)");
        ReadSource("Service", "Trade.cs").Should().Contain("Space.GetIncursionExchangeRefusal(dock)");
        var docks = ReadSource("Service", "SpaceIncursions.cs");
        docks.Should().Contain("DockLandingWaypointVariable").And.Contain("Station.StationCityVariable", "ship-only docks hold none; station docks are listed apart");
    }

    [Test]
    public void StationBlockades_CloseTheStationOnlyUntilBrokenOrTheStationIsGone()
    {
        var anchor = StationRules.GetAnchorsInArea("viscaraorbit").First();
        var stationKey = IncursionRules.StationDockKey(anchor);
        stationKey.Should().Be($"viscaraorbit:{anchor.Tag}", "a station's docking point teleports to its anchor");
        IncursionRules.StationDockKey(null).Should().BeEmpty();

        var viscara = IncursionRules.DockKey("viscaraorbit", "Viscara_Orbit");
        var incursion = IncursionRules.Create(Region("viscaraorbit"), IncursionInvader.Sith, Wednesday,
            new Dictionary<string, int> { [viscara] = (int)PlanetType.Viscara },
            new Dictionary<string, string> { [stationKey] = "station-1" });

        incursion.Blockades.Should().ContainKey(stationKey).WhoseValue.Should().Be((int)PlanetType.Invalid, "a station dock serves no planet");
        IncursionRules.IsBlockadeHolding(incursion, stationKey).Should().BeTrue("docking at the station is refused through its dock key");
        IncursionRules.IsStationBlockaded(incursion, "station-1").Should().BeTrue();
        IncursionRules.IsStationBlockaded(incursion, "station-2").Should().BeFalse();
        IncursionRules.IsStationBlockaded(null, "station-1").Should().BeFalse();

        // Breaking the planet dock's blockade reopens the planet exchange even while the station's holds.
        incursion.BrokenBlockades.Add(viscara);
        IncursionRules.IsExchangeClosed(incursion, TradeExchangeType.Viscara).Should().BeFalse();
        IncursionRules.IsStationBlockaded(incursion, "station-1").Should().BeTrue();
        IncursionRules.IsCleared(incursion).Should().BeFalse("every blockade, the station's included, must break");

        incursion.BrokenBlockades.Add(stationKey);
        IncursionRules.IsStationBlockaded(incursion, "station-1").Should().BeFalse();

        // Older saved incursions read no station blockades.
        var saved = JsonSerializer.Deserialize<ActiveIncursion>("{\"Center\":\"viscaraorbit\"}")!;
        saved.StationBlockades.Should().BeEmpty();

        var source = ReadSource("Service", "SpaceIncursions.cs");
        source.Should().Contain("GetIncursionStationDocks(area.Resref)").And.Contain("IncursionRules.Create(region, invader, DateTime.UtcNow, docks, stationDocks)");
        source.Should().Contain("ReleaseLostStationBlockades(incursion)", "a retrieved or lost station never holds the region open");
        source.Should().Contain("dock.IsCity && IncursionRules.IsStationBlockaded(incursion, dock.CityId)");
        ReadSource("Service", "TradeFreight.cs").Should().Contain("Space.GetIncursionExchangeRefusal(GetDock(dockId, exchange))");

        // A Jump Beacon cannot jump a ship into a blockaded station; leaving one is allowed, as launching is.
        var request = new JumpRequest
        {
            ShipDocked = true, CanPilot = true, OriginPermits = true, DestinationPermits = true, OriginBeaconLevel = 3,
            DestinationBeaconLevel = 3, ShipSize = ShipSize.Small, FuelAboard = 99, DestinationBlockaded = true,
        };
        StationBuildingRules.GetJumpRefusal(request).Should().Be(JumpRefusal.DestinationBlockaded);
        StationBuildingRules.DescribeJumpRefusal(JumpRefusal.DestinationBlockaded, request).Should().Contain("blockades that station");
        ReadSource("Service", "StationBuildings.cs").Should().Contain("DestinationBlockaded = destination != null && Space.IsStationBlockaded(destination.Id)");
        ((int)JumpRefusal.DestinationBlockaded).Should().Be(10);
    }

    [Test]
    public void PriceSpike_AppliesToTheRegionsExchangesOnly()
    {
        var incursion = IncursionRules.Create(Region("viscaraorbit"), IncursionInvader.Mandalorian, Wednesday, new Dictionary<string, int>());
        IncursionRules.GetPriceMultiplier(incursion, TradeExchangeType.Viscara).Should().Be(IncursionTuning.PriceMultiplier);
        IncursionRules.GetPriceMultiplier(incursion, TradeExchangeType.CZ220).Should().Be(IncursionTuning.PriceMultiplier);
        IncursionRules.GetPriceMultiplier(incursion, TradeExchangeType.MonCala).Should().Be(IncursionTuning.PriceMultiplier, "a neighbour orbit");
        IncursionRules.GetPriceMultiplier(incursion, TradeExchangeType.Korriban).Should().Be(1.0);
        IncursionRules.GetPriceMultiplier(null, TradeExchangeType.Viscara).Should().Be(1.0);

        var market = TradeLookup.GetExchange(TradeExchangeType.Viscara)!.Markets[0];
        var spiked = TradePricing.WithPriceMultiplier(market, 1.25);
        TradePricing.WithPriceMultiplier(market, 1.0).Should().BeSameAs(market);
        spiked.PriceFactor.Should().BeApproximately(market.PriceFactor * 1.25, 1e-9);
        market.PriceFactor.Should().NotBe(spiked.PriceFactor, "the catalog row is untouched");
        TradePricing.GetUnitBuyPrice(1000, spiked, 0, 10).Should().BeApproximately(1.25 * TradePricing.GetUnitBuyPrice(1000, market, 0, 10), 1e-6);
        TradePricing.GetUnitSellPrice(1000, spiked, 0, 10).Should().BeApproximately(1.25 * TradePricing.GetUnitSellPrice(1000, market, 0, 10), 1e-6);

        ReadSource("Service", "Trade.cs").Should().Contain("Space.GetIncursionPriceMultiplier(exchangeType)");
        ReadSource("Feature", "GuiDefinition", "ViewModel", "TradeExchangeViewModel.cs").Should().Contain("Space.GetIncursionPriceMultiplier(");
    }

    [Test]
    public void HoloNet_PricesTheIncursionSpike_AndRebuildsWhenAnIncursionStartsOrEnds()
    {
        var plain = HoloNetRules.Build((_, _) => 0, Wednesday);
        var spiked = HoloNetRules.Build((_, _) => 0, Wednesday, _ => IncursionTuning.PriceMultiplier);
        spiked.Rows.Should().HaveCount(plain.Rows.Count);
        foreach (var (before, after) in plain.Rows.Zip(spiked.Rows))
        {
            after.Commodity.Should().Be(before.Commodity);
            after.BuyPrice.Should().BeGreaterThanOrEqualTo(before.BuyPrice, after.Name);
            after.SellPrice.Should().BeGreaterThan(before.SellPrice, after.Name);
        }

        // Only the region's exchanges spike: a good's best buyer can move into the region.
        var incursion = IncursionRules.Create(Region("viscaraorbit"), IncursionInvader.Sith, Wednesday, new Dictionary<string, int>());
        var region = HoloNetRules.Build((_, _) => 0, Wednesday, exchange => IncursionRules.GetPriceMultiplier(incursion, exchange));
        region.Rows.Should().Contain(x => x.SellAt == TradeExchangeType.Viscara || x.SellAt == TradeExchangeType.CZ220 ||
                                          x.SellAt == TradeExchangeType.MonCala);

        HoloNetRules.GetPriceKey(null).Should().BeEmpty();
        HoloNetRules.GetPriceKey(incursion).Should().Be(HoloNetRules.GetPriceKey(incursion));
        var later = IncursionRules.Create(Region("viscaraorbit"), IncursionInvader.Sith, Wednesday.AddDays(7), new Dictionary<string, int>());
        HoloNetRules.GetPriceKey(later).Should().NotBe(HoloNetRules.GetPriceKey(incursion), "a new incursion rebuilds the board");

        var board = ReadSource("Service", "TradeHoloNet.cs");
        board.Should().Contain("HoloNetRules.GetPriceKey(Space.GetActiveIncursion())");
        board.Should().Contain("Space.GetIncursionPriceMultiplier);");
    }

    [Test]
    public void FreightContracts_AreRefusedAtAnExchangeABlockadeClosed()
    {
        var freight = ReadSource("Service", "TradeFreight.cs").Replace("\r\n", "\n");
        freight.Should().Contain("var closed = Space.GetIncursionExchangeRefusal(GetDock(dockId, exchange));");
        System.Text.RegularExpressions.Regex.Matches(freight, @"if \(IsFreightExchangeClosed\(player, exchange, dockId\)\)\n\s+return;")
            .Count.Should().Be(2, "both accepting and delivering are refused");
        freight.IndexOf("IsFreightExchangeClosed(player, exchange, dockId)", StringComparison.Ordinal)
            .Should().BeLessThan(freight.IndexOf("TakeGoldFromCreature(offer.Deposit", StringComparison.Ordinal), "no deposit is taken first");
        IncursionRules.ExchangeRefusal(IncursionInvader.Sith).Should().Contain("The exchange is closed");
    }

    // Contribution

    [Test]
    public void Contribution_SplitsKillsByDamageAndSharesThemAboard()
    {
        IncursionRules.KillPoints("Normal").Should().Be(1.0);
        IncursionRules.KillPoints("Elite").Should().Be(2.5);
        IncursionRules.KillPoints("Boss").Should().Be(9.0);

        var shares = IncursionRules.SplitKill(new Dictionary<uint, double> { [1] = 300, [2] = 100, [3] = 0 }, 2.0, 2u);
        shares.Should().HaveCount(2);
        shares[1].Should().BeApproximately(1.5, 1e-9);
        shares[2].Should().BeApproximately(0.5 + 2.0 * IncursionTuning.KillBonus, 1e-9, "the killing blow's bonus");
        IncursionRules.SplitKill(new Dictionary<uint, double>(), 1.0, 1u).Should().BeEmpty();

        IncursionRules.RepairPoints(IncursionTuning.FighterPool[3], 3).Should().BeApproximately(IncursionTuning.RepairPerPool, 1e-9);
        IncursionRules.RepairPoints(0, 3).Should().Be(0);
    }

    [Test]
    public void RepairContribution_CountsOnlyDamageIncursionShipsDealt_AndOnlyOnce()
    {
        var ledger = IncursionRules.AddDamageTaken(0, 300);
        IncursionRules.CreditableRepair(ledger, 500).Should().Be(300, "only the 300 incursion damage counts");
        IncursionRules.CreditableRepair(ledger - 300, 500).Should().Be(0, "a repeated repair of the same damage earns nothing");
        IncursionRules.CreditableRepair(0, 500).Should().Be(0, "damage from players or anything else earns nothing");
        IncursionRules.CreditableRepair(ledger, 120).Should().Be(120);

        var aboard = IncursionRules.ShareAboard(3.0, new[] { "pilot", "gunner", "gunner", "engineer" });
        aboard.Should().HaveCount(3).And.OnlyContain(x => Math.Abs(x.Value - 1.0) < 1e-9);

        var contribution = new IncursionContribution();
        IncursionRules.AddPoints(contribution, 2, 3);
        IncursionRules.AddPoints(contribution, 2, 1.5);
        IncursionRules.AddPoints(contribution, 4, 0.5);
        IncursionRules.AddPoints(contribution, 4, -1);
        contribution.Points.Should().BeEquivalentTo(new Dictionary<int, double> { [2] = 4.5, [4] = 0.5 });
        IncursionRules.QualifiesForRegionReward(contribution).Should().BeTrue();
        IncursionRules.QualifiesForRegionReward(new IncursionContribution { Points = { [1] = IncursionTuning.MinimumRegionPoints - 0.1 } }).Should().BeFalse();

        var space = ReadSource("Service", "Space.cs");
        space.Should().Contain("RecordIncursionDamage(attacker, target, shieldDamage + hullDamage);")
            .And.Contain("RecordIncursionDamage(attacker, target, hullBefore - targetShipStatus.Hull);");
        ReadSource("Service", "SpaceThreat.cs").Should().Contain("RecordIncursionRepair(source, repaired, restored);");
        ReadSource("Service", "SpaceIncursions.cs").Should().Contain("[NWNEventHandler(ScriptName.OnCreatureDeathBefore)]")
            .And.Contain("GetShipCrewPlayers(pilot)");
    }

    [Test]
    public void Payout_PaysPointsByTierAndTheRegionReward()
    {
        var region = Region("dantooineorbit");
        var contribution = new IncursionContribution { Points = { [5] = 45, [2] = 10 } };
        var payout = IncursionRules.BuildPayout(contribution, region, IncursionInvader.Mandalorian, Rolls(99));
        payout.Credits.Should().Be((int)Math.Round(45 * IncursionTuning.CreditsPerPoint[5] + 10 * IncursionTuning.CreditsPerPoint[2] +
                                                   IncursionTuning.RegionCredits[5], MidpointRounding.AwayFromZero));
        payout.Pulls["SPACE_ELITE_T5"].Should().Be(2 + IncursionTuning.RegionPulls, "45 points make two pulls, plus the region reward's");
        payout.Pulls.Should().NotContainKey("SPACE_ELITE_T2", "10 points make no pull");
        payout.Items.Should().BeEmpty("the schematic roll missed");
        payout.RegionName.Should().Be("Dantooine");
        payout.Points.Should().Be(55);

        var lucky = IncursionRules.BuildPayout(contribution, region, IncursionInvader.Mandalorian, Rolls(0, 0));
        lucky.Items.Should().ContainSingle().Which.Should().BeOneOf(IncursionRules.InvaderSchematics(IncursionInvader.Mandalorian, 5));
        var makers = RevampShipCatalog.Hulls.ToDictionary(x => x.Tag, x => x.Manufacturer);
        IncursionRules.InvaderSchematics(IncursionInvader.Sith, 6).Should().NotBeEmpty()
            .And.OnlyContain(r => makers[RevampRewardCatalog.ShipSchematics.Single(s => s.Resref == r).ShipTag] == ShipManufacturer.Sith);

        var small = IncursionRules.BuildPayout(new IncursionContribution { Points = { [1] = 2 } }, region, IncursionInvader.Sith, Rolls(0));
        small.Credits.Should().Be((int)Math.Round(2 * IncursionTuning.CreditsPerPoint[1], MidpointRounding.AwayFromZero), "below the region reward's minimum");
        small.Pulls.Should().BeEmpty();

        var capped = IncursionRules.BuildPayout(new IncursionContribution { Points = { [3] = 5000 } }, region, IncursionInvader.Sith, Rolls(99));
        capped.Pulls["SPACE_ELITE_T3"].Should().Be(IncursionTuning.MaximumPulls);
        capped.Credits.Should().Be((int)Math.Round(IncursionTuning.MaximumPaidPoints * IncursionTuning.CreditsPerPoint[3] + IncursionTuning.RegionCredits[5],
            MidpointRounding.AwayFromZero));
    }

    [Test]
    public void Clearing_NeedsEveryAreaLiberatedAndEveryBlockadeBroken()
    {
        var key = IncursionRules.DockKey("space_dathomir", "Dathomir_Orbit");
        var incursion = IncursionRules.Create(Region("space_dathomir"), IncursionInvader.Sith, Wednesday, new Dictionary<string, int> { [key] = (int)PlanetType.Dathomir });
        incursion.EndsAt.Should().Be(Wednesday.AddHours(IncursionTuning.DurationHours));
        IncursionRules.IsCleared(incursion).Should().BeFalse();

        foreach (var progress in incursion.Areas.Values)
        {
            progress.WavesCleared = IncursionTuning.WavesPerArea;
            IncursionRules.IsAreaLiberated(progress).Should().BeFalse("the command group is left");
            progress.CommandDestroyed = true;
        }

        IncursionRules.IsCleared(incursion).Should().BeFalse("the blockade holds");
        incursion.BrokenBlockades.Add(key);
        IncursionRules.IsCleared(incursion).Should().BeTrue();
    }

    [Test]
    public void Rewards_LandInTheEarningBand()
    {
        for (var tier = 1; tier <= 6; tier++)
        {
            IncursionTuning.CreditsPerPoint[tier].Should().BePositive();
            IncursionTuning.RegionCredits[tier].Should().BePositive();
            IncursionTuning.FighterPool[tier].Should().BePositive();
        }
    }

    // The DM tools, status and docs

    [Test]
    public void DmToolsAndStatus_AreWired()
    {
        var definition = ReadSource("Feature", "GuiDefinition", "DMToolsDefinition.cs");
        definition.Should().Contain("model.OnStartIncursion()");
        definition.Substring(definition.IndexOf("Start Incursion", StringComparison.Ordinal), 300).Should().NotContain("SetIsEnabled(false)");
        ReadSource("Feature", "GuiDefinition", "ViewModel", "DMToolsViewModel.cs").Should().Contain("Space.StartIncursionAt(Player)");
        ReadSource("Feature", "ChatCommandDefinition", "SpaceChatCommand.cs").Should().Contain("_builder.Create(\"incursion\")");
        ReadSource("Service", "SpaceHud.cs").Should().Contain("Space.GetIncursionStatusText(player)");

        var rows = ShipHudRules.BuildStatRows(new ShipHudStats { IncursionText = "wave 2/5", RiftRunText = "chamber 1/3" });
        rows.Should().Contain(x => x.Label == "Incursion" && x.Value == "wave 2/5");
        rows.Should().Contain(x => x.Label == "Rift Run" && x.Value == "chamber 1/3");
        ShipHudRules.BuildStatRows(new ShipHudStats()).Should().NotContain(x => x.Label == "Incursion" || x.Label == "Rift Run");
    }

    [Test]
    public void TheNumbers_AreGeneratedAndThePlanListsTheUpdate()
    {
        var tool = File.ReadAllText(Path.Combine(Root(), "tools", "SpaceRevampBalance", "events.py"));
        tool.Should().Contain("def events_tab(Sheet):").And.Contain("PRICE_MULTIPLIER = 1.25");
        ReadSource("Service", "SpaceService", "IncursionTuning.cs").Should().Contain("auto-generated");
        File.ReadAllText(Path.Combine(Root(), "tools", "SpaceRevampBalance", "bible_tabs.py")).Should().Contain("events.events_tab(Sheet)");

        var plan = ReadSource("Readmes", "SpaceRevampPlan.md");
        var section = plan.Substring(plan.IndexOf("## 30. Incursions", StringComparison.Ordinal));
        section.Substring(0, section.IndexOf("## 31.", StringComparison.Ordinal)).Should().Contain("Added by the incursions and Rift Runs update (W15b)");
    }
}
