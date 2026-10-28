using System.Text;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.PropertyLayoutDefinition;
using SWLOR.Game.Server.Feature.RecipeDefinition.FabricationRecipeDefinition;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.ShuttleService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.StationService;
using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Player stations (plan section 25, W13a): anchor sites, founding, upkeep, the deck, docking, exchanges and the shuttle.
/// </summary>
public class StationTests
{
    private static readonly string[] DockedOrbits =
    {
        "viscaraorbit", "tatooineorbit", "moncalaorbit", "hutlar_orbit", "space_dathomir", "space_korriban",
        "dantooineorbit", "narshadorbit",
    };

    // Anchor sites

    [Test]
    public void EveryOrbitWithAnNpcDockHasTwoAnchorSites()
    {
        foreach (var orbit in DockedOrbits)
            StationRules.GetAnchorsInArea(orbit).Should().HaveCount(2, orbit);
    }

    [Test]
    public void EveryWildSpaceSystemHasOneOrTwoAnchorSites()
    {
        foreach (var system in WildSpaceTuning.Systems)
        {
            var anchors = StationRules.GetAnchorsInArea(system.AreaResref).ToList();
            anchors.Count.Should().BeInRange(1, 2, system.AreaResref);
            anchors.Should().OnlyContain(x => x.IsWildSpace && x.SystemName == system.Name);
        }

        StationTuning.Anchors.Where(x => !x.IsWildSpace).Should().OnlyContain(x => DockedOrbits.Contains(x.AreaResref));
    }

    [Test]
    public void AnchorIdsAndTagsAreUnique()
    {
        StationTuning.Anchors.Select(x => x.Id).Should().OnlyHaveUniqueItems();
        StationTuning.Anchors.Select(x => x.Tag).Should().OnlyHaveUniqueItems();
        StationTuning.Anchors.Should().OnlyContain(x => x.Tag == StationTuning.AnchorTagPrefix + x.Id.ToUpperInvariant());
        StationRules.GetAnchor("viscara1").Should().BeSameAs(StationRules.GetAnchorByTag("STATION_ANCHOR_VISCARA1"));
        StationRules.GetAnchor("nowhere").Should().BeNull();
    }

    [Test]
    public void EveryAnchorIsAWaypointInItsArea()
    {
        foreach (var anchor in StationTuning.Anchors)
        {
            using var git = ReadModule("git", anchor.AreaResref);
            var waypoints = git.RootElement.GetProperty("WaypointList").GetProperty("value").EnumerateArray()
                .Where(w => Value(w, "Tag").GetString() == anchor.Tag)
                .ToList();
            waypoints.Should().ContainSingle(anchor.Tag);
            Value(waypoints[0], "XPosition").GetDouble().Should().BeApproximately(anchor.X, 0.01);
            Value(waypoints[0], "YPosition").GetDouble().Should().BeApproximately(anchor.Y, 0.01);
        }
    }

    [Test]
    public void AnchorsAndExteriorsAreClearOfTheAreasObjects()
    {
        foreach (var anchor in StationTuning.Anchors)
        {
            using var git = ReadModule("git", anchor.AreaResref);
            var root = git.RootElement;
            var points = root.GetProperty("Placeable List").GetProperty("value").EnumerateArray()
                .Where(p => !Value(p, "TemplateResRef").GetString()!.StartsWith("plc_dust") &&
                            !Value(p, "TemplateResRef").GetString()!.StartsWith("zep_bflame"))
                .Select(p => (Value(p, "X").GetDouble(), Value(p, "Y").GetDouble()))
                .Concat(root.GetProperty("WaypointList").GetProperty("value").EnumerateArray()
                    .Where(w => !Value(w, "Tag").GetString()!.StartsWith(StationTuning.AnchorTagPrefix))
                    .Select(w => (Value(w, "XPosition").GetDouble(), Value(w, "YPosition").GetDouble())))
                .ToList();
            var (ex, ey) = StationRules.GetExteriorPosition(anchor);

            foreach (var (x, y) in new[] { ((double)anchor.X, (double)anchor.Y), ((double)ex, (double)ey) })
            {
                x.Should().BeInRange(30, 290, anchor.Id);
                y.Should().BeInRange(30, 290, anchor.Id);
                points.Min(p => Math.Sqrt((p.Item1 - x) * (p.Item1 - x) + (p.Item2 - y) * (p.Item2 - y)))
                    .Should().BeGreaterThanOrEqualTo(40, anchor.Id);
            }
        }
    }

    [Test]
    public void AnchorsAreOnlyInSpaceAreasThatAdmitEveryHull()
    {
        foreach (var area in StationTuning.Anchors.Select(x => x.AreaResref).Distinct())
        {
            using var git = ReadModule("git", area);
            var locals = git.RootElement.GetProperty("VarTable").GetProperty("value").EnumerateArray()
                .ToDictionary(v => Value(v, "Name").GetString()!, v => Value(v, "Value").ToString());
            locals.Should().ContainKey("SPACE", area);
            locals.Should().NotContainKey("MAX_SHIP_SIZE", area);
            ShipAccessRules.AreaAdmits(ShipSize.Capital, ShipSize.Invalid).Should().BeTrue();
        }
    }

    [Test]
    public void OneStationPerSite()
    {
        StationRules.IsSiteFree("Viscara1", Array.Empty<string>()).Should().BeTrue();
        StationRules.IsSiteFree("Viscara1", new[] { "viscara1" }).Should().BeFalse();
        StationRules.IsSiteFree("Viscara1", new[] { "Viscara2" }).Should().BeTrue();
        StationRules.IsSiteFree("Nowhere", Array.Empty<string>()).Should().BeFalse();
    }

    [Test]
    public void TheCoreFindsTheNearestAnchorWithinFoundingRange()
    {
        var anchor = StationRules.GetAnchor("Viscara1");
        StationRules.FindAnchorInRange("viscaraorbit", anchor.X + 39f, anchor.Y).Should().BeSameAs(anchor);
        StationRules.FindAnchorInRange("viscaraorbit", anchor.X + 41f, anchor.Y).Should().BeNull();
        StationRules.FindAnchorInRange("tatooineorbit", anchor.X, anchor.Y).Should().NotBeSameAs(anchor);
        StationRules.FindAnchorInRange("space_midrim1", 160f, 160f).Should().BeNull();
    }

    // Founding

    private static StationFoundingRequest Founding(
        bool inSpace = true,
        bool anchor = true,
        ShipSize areaMax = ShipSize.Invalid,
        bool taken = false,
        bool citizen = false,
        int cityManagement = 1,
        int credits = StationTuning.InitialPrice) => new()
    {
        InSpace = inSpace,
        Anchor = anchor ? StationRules.GetAnchor("Hutlar1") : null,
        AreaMaxSize = areaMax,
        AnchorTaken = taken,
        IsCitizen = citizen,
        CityManagementLevel = cityManagement,
        Credits = credits,
    };

    [Test]
    public void FoundingNeedsAFreeAnchorInSpaceCityManagementAndThePrice()
    {
        StationRules.GetFoundingRefusal(Founding()).Should().Be(StationFoundingRefusal.None);
        StationRules.GetFoundingRefusal(Founding(inSpace: false)).Should().Be(StationFoundingRefusal.NotInSpace);
        StationRules.GetFoundingRefusal(Founding(anchor: false)).Should().Be(StationFoundingRefusal.NoAnchorInRange);
        StationRules.GetFoundingRefusal(Founding(areaMax: ShipSize.Medium)).Should().Be(StationFoundingRefusal.AreaSizeLimit);
        StationRules.GetFoundingRefusal(Founding(taken: true)).Should().Be(StationFoundingRefusal.SiteTaken);
        StationRules.GetFoundingRefusal(Founding(citizen: true)).Should().Be(StationFoundingRefusal.AlreadyCitizen);
        StationRules.GetFoundingRefusal(Founding(cityManagement: 0)).Should().Be(StationFoundingRefusal.MissingCityManagement);
        StationRules.GetFoundingRefusal(Founding(credits: StationTuning.InitialPrice - 1)).Should().Be(StationFoundingRefusal.InsufficientCredits);

        // The order matters: a taken site is reported before the player's own shortcomings.
        StationRules.GetFoundingRefusal(Founding(taken: true, citizen: true, credits: 0)).Should().Be(StationFoundingRefusal.SiteTaken);

        foreach (var refusal in Enum.GetValues<StationFoundingRefusal>().Where(x => x != StationFoundingRefusal.None))
            StationRules.DescribeFoundingRefusal(refusal).Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public void FoundingMirrorsThePlanetCity()
    {
        var city = new CityLayoutDefinition().Build()[PropertyLayoutType.City];
        var station = new StationLayoutDefinition().Build()[PropertyLayoutType.Station];

        station.PropertyType.Should().Be(PropertyType.City);
        station.InitialPrice.Should().Be(city.InitialPrice);
        station.PricePerDay.Should().Be(city.PricePerDay);
        station.StructureLimit.Should().Be(city.StructureLimit);
        station.BuildingLimit.Should().Be(city.BuildingLimit);
        station.AreaInstanceResref.Should().Be(StationTuning.DeckAreaResref);
        StationTuning.CitizensRequired.Should().Equal(5, 10, 15, 20, 25);
        StationTuning.GraceHours.Should().Be(SWLOR.Game.Server.Service.Property.MinimumCitizensGracePeriodHours);
    }

    [Test]
    public void TheStationCoreIsAFabricationStructureForTheCityHallAndTheStarport()
    {
        var recipes = new StructureRecipes().BuildRecipes();
        var core = recipes[RecipeType.StationCore];
        core.Resref.Should().Be(StationTuning.CoreItemResref);
        core.Level.Should().Be(StationTuning.CoreRecipeLevel);
        core.Components.Should().BeEquivalentTo(StationTuning.CoreComponents.ToDictionary(x => x.Resref, x => x.Quantity));

        // The City Hall's and the Starport's components together.
        var together = new Dictionary<string, int>();
        foreach (var recipe in new[] { recipes[RecipeType.CityHallStyle1], recipes[RecipeType.StarportStyle1] })
        foreach (var (resref, quantity) in recipe.Components)
            together[resref] = together.GetValueOrDefault(resref) + quantity;
        core.Components.Should().BeEquivalentTo(together);
        core.Level.Should().Be(recipes[RecipeType.StarportStyle1].Level);

        StationTuning.CoreItemResref.Should().Be($"structure_{(int)StructureType.StationCore}");
        using var item = ReadModule("uti", StationTuning.CoreItemResref);
        Value(item.RootElement, "LocalizedName").GetProperty("0").GetString().Should().Be("Station Core");
        Value(item.RootElement, "TemplateResRef").GetString().Should().Be(StationTuning.CoreItemResref);
        File.ReadAllText(Path.Combine(RepositoryRoot, "Module", "itp", "itempalcus.itp.json")).Should().Contain($"\"{StationTuning.CoreItemResref}\"");
    }

    // Upkeep

    [TestCase(0, 1, 1, 1, 35000)]
    [TestCase(2, 1, 1, 1, 31500)]
    [TestCase(0, 3, 2, 1, 65000)]
    [TestCase(1, 5, 5, 5, 153250)]
    public void UpkeepIsThePlanetCitysWeeklyUpkeep(int perk, int medical, int starport, int cantina, int expected)
    {
        StationRules.GetWeeklyUpkeep(perk, medical, starport, cantina).Should().Be(expected);
        CityUpkeepRules.GetWeeklyUpkeep(new CityLayoutDefinition().Build()[PropertyLayoutType.City].PricePerDay, perk, medical, starport, cantina)
            .Should().Be(expected);
    }

    [Test]
    public void TheExteriorGrowsWithTheCityLevel()
    {
        StationRules.GetExteriorScale(1).Should().Be(1.2f);
        StationRules.GetExteriorScale(5).Should().Be(2.0f);
        StationRules.GetExteriorScale(0).Should().Be(1.2f);
        StationRules.GetExteriorScale(9).Should().Be(2.0f);
        Enumerable.Range(1, 4).Should().OnlyContain(level => StationRules.GetExteriorScale(level) < StationRules.GetExteriorScale(level + 1));

        var anchor = StationRules.GetAnchor("Viscara1");
        var (x, y) = StationRules.GetExteriorPosition(anchor);
        Math.Sqrt((x - anchor.X) * (x - anchor.X) + (y - anchor.Y) * (y - anchor.Y)).Should().BeApproximately(StationTuning.ExteriorOffset, 0.01);
    }

    // The deck

    [Test]
    public void EveryCivicBuildingHasADeckSlotAndTheStationOnlySlotsAreKept()
    {
        StationDeckRules.GetSlot(StructureType.StationCore).Should().Be(StationDeckSlotType.Core);
        StationDeckRules.GetSlot(StructureType.Starport).Should().Be(StationDeckSlotType.Starport);
        StationDeckRules.GetSlot(StructureType.MedicalCenter).Should().Be(StationDeckSlotType.MedicalCenter);
        StationDeckRules.GetSlot(StructureType.Bank).Should().Be(StationDeckSlotType.Bank);
        StationDeckRules.GetSlot(StructureType.Cantina).Should().Be(StationDeckSlotType.Cantina);
        StationDeckRules.GetSlot(StructureType.SmallHouseStyle1).Should().Be(StationDeckSlotType.Invalid);

        StationDeckRules.GetSlot(StructureType.Shipyard).Should().Be(StationDeckSlotType.Shipyard);
        StationDeckRules.GetSlot(StructureType.AsteroidDrill).Should().Be(StationDeckSlotType.AsteroidDrill);
        StationDeckRules.GetSlot(StructureType.JumpBeacon).Should().Be(StationDeckSlotType.JumpBeacon);
        StationTuning.DeckSlots[StationDeckSlotType.StationFuel].Building.Should().Be(StructureType.Invalid);

        StationTuning.DeckSlots.Keys.Should().BeEquivalentTo(Enum.GetValues<StationDeckSlotType>().Where(x => x != StationDeckSlotType.Invalid));
    }

    [Test]
    public void DeckSlotsDoNotOverlapAndLieOnTheDeck()
    {
        var slots = StationTuning.DeckSlots.Values.ToList();
        foreach (var a in slots)
        foreach (var b in slots.Where(b => b != a))
            Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y)).Should().BeGreaterThanOrEqualTo(StationTuning.DeckSlotClearance, $"{a.Type} {b.Type}");

        using var are = ReadModule("are", StationTuning.DeckAreaResref);
        var width = Value(are.RootElement, "Width").GetInt32() * 10;
        var height = Value(are.RootElement, "Height").GetInt32() * 10;
        slots.Should().OnlyContain(s => s.X > 5 && s.X < width - 5 && s.Y > 5 && s.Y < height - 5);
    }

    [Test]
    public void CivicBuildingsSnapToTheirSlotOncePerStation()
    {
        var placed = StationDeckRules.GetPlacement(StructureType.Bank, 10f, 10f, Array.Empty<StationDeckSlotType>());
        placed.Refusal.Should().Be(StationDeckRefusal.None);
        placed.X.Should().Be(StationTuning.DeckSlots[StationDeckSlotType.Bank].X);
        placed.Y.Should().Be(StationTuning.DeckSlots[StationDeckSlotType.Bank].Y);

        StationDeckRules.GetPlacement(StructureType.Bank, 10f, 10f, new[] { StationDeckSlotType.Bank })
            .Refusal.Should().Be(StationDeckRefusal.SlotTaken);

        var occupied = StationDeckRules.GetOccupiedSlots(new[] { StructureType.StationCore, StructureType.Starport, StructureType.SmallHouseStyle1 });
        occupied.Should().BeEquivalentTo(new[] { StationDeckSlotType.Core, StationDeckSlotType.Starport });
    }

    [Test]
    public void OtherStructuresKeepClearOfEverySlot()
    {
        var shipyard = StationTuning.DeckSlots[StationDeckSlotType.Shipyard];
        var near = StationDeckRules.GetPlacement(StructureType.SmallHouseStyle1, shipyard.X + 5f, shipyard.Y, Array.Empty<StationDeckSlotType>());
        near.Refusal.Should().Be(StationDeckRefusal.TooCloseToSlot);
        near.Slot.Should().Be(StationDeckSlotType.Shipyard);
        StationDeckRules.DescribeRefusal(near).Should().Contain("Shipyard");

        var far = StationDeckRules.GetPlacement(StructureType.SmallHouseStyle1, shipyard.X + 30f, shipyard.Y + 30f, Array.Empty<StationDeckSlotType>());
        far.Refusal.Should().Be(StationDeckRefusal.None);
        far.X.Should().Be(shipyard.X + 30f);
    }

    [Test]
    public void TheDeckIsAnEmptyCopyWithOneEntrance()
    {
        using var git = ReadModule("git", StationTuning.DeckAreaResref);
        var root = git.RootElement;
        foreach (var list in new[] { "Creature List", "Door List", "Encounter List", "SoundList", "StoreList", "TriggerList" })
            root.GetProperty(list).GetProperty("value").GetArrayLength().Should().Be(0, list);
        root.GetProperty("Placeable List").GetProperty("value").EnumerateArray()
            .Should().OnlyContain(p => Value(p, "Static").GetInt32() == 1);
        root.GetProperty("WaypointList").GetProperty("value").EnumerateArray()
            .Select(w => Value(w, "Tag").GetString()).Should().Equal("PROPERTY_ENTRANCE");

        using var are = ReadModule("are", StationTuning.DeckAreaResref);
        Value(are.RootElement, "ResRef").GetString().Should().Be(StationTuning.DeckAreaResref);
        File.Exists(Path.Combine(RepositoryRoot, "Module", "gic", $"{StationTuning.DeckAreaResref}.gic.json")).Should().BeTrue();
        File.ReadAllText(Path.Combine(RepositoryRoot, "Module", "ifo", "module.ifo.json")).Should().Contain($"\"{StationTuning.DeckAreaResref}\"");
        File.ReadAllText(Path.Combine(RepositoryRoot, "toolset", "categories.json")).Should().Contain($"\"{StationTuning.DeckAreaResref}\"");
    }

    [Test]
    public void OnlyStationMedicalCentersTakeRespawnRegistrations()
    {
        // The shared interior holds the terminal; planet cities' instances remove it, so they behave as before stations.
        using var git = ReadModule("git", "medical_center");
        git.RootElement.GetProperty("Placeable List").GetProperty("value").EnumerateArray()
            .Should().Contain(p => Value(p, "TemplateResRef").GetString() == "cloning_reg");

        ReadServer("Feature", "PropertyLayoutDefinition", "MedicalCenterLayoutDefinition.cs")
            .Should().Contain(".OnSpawn(RemoveRegistrationOutsideStations)")
            .And.Contain("if (Station.IsStationInterior(Property.GetPropertyId(instance)))");
        ReadServer("Feature", "DialogDefinition", "MedicalRegistrationDialog.cs")
            .Should().Contain("if (!Station.IsStationInterior(property.Id))");
        ReadServer("Service", "Death.cs")
            .Should().Contain("Station.IsStationInterior(dbPlayer.RespawnPropertyId) &&");
    }

    // Docking

    private static StarportDockRefusal StationDock(ShipSize size, int level) =>
        ShipAccessRules.GetStarportRefusal(new StarportDockRequest
        {
            ShipSize = size,
            IsPlayerStarport = true,
            IsStationStarport = true,
            PlayerStarportLevel = level,
        });

    [TestCase(ShipSize.Small)]
    [TestCase(ShipSize.Medium)]
    [TestCase(ShipSize.Large)]
    public void AnyStationStarportTakesLargeHulls(ShipSize size)
    {
        StationDock(size, 0).Should().Be(StarportDockRefusal.None);
        StationDock(size, 1).Should().Be(StarportDockRefusal.None);
    }

    [TestCase(0, StarportDockRefusal.StationStarportTooSmall)]
    [TestCase(1, StarportDockRefusal.StationStarportTooSmall)]
    [TestCase(2, StarportDockRefusal.None)]
    [TestCase(5, StarportDockRefusal.None)]
    public void CapitalHullsNeedStarportLevelTwo(int level, StarportDockRefusal expected)
    {
        StationDock(ShipSize.Capital, level).Should().Be(expected);
        ShipAccessRules.DescribeStarportRefusal(StarportDockRefusal.StationStarportTooSmall, ShipSize.Invalid)
            .Should().Contain("station").And.Contain($"level {ShipAccessRules.CapitalStarportLevel}");
    }

    // Exchanges

    [Test]
    public void AnOrbitStationTradesThroughItsOrbitsNpcExchange()
    {
        foreach (var anchor in StationTuning.Anchors.Where(x => !x.IsWildSpace))
        {
            anchor.Exchange.Should().Be(TradeLookup.GetExchangeForPlanet(anchor.HomePlanet), anchor.Id);
            TradeLookup.GetExchange(anchor.Exchange).OrbitResref.Should().Be(anchor.AreaResref, anchor.Id);
            StationRules.GetExchange(anchor.Id).Should().Be(anchor.Exchange);
        }

        StationRules.GetExchange("Viscara2").Should().Be(TradeExchangeType.CZ220);
        StationRules.GetExchange("nowhere").Should().Be(TradeExchangeType.Invalid);
    }

    [Test]
    public void AWildSpaceStationTradesThroughTheNearestOpenPort()
    {
        foreach (var anchor in StationTuning.Anchors.Where(x => x.IsWildSpace))
        {
            var exchange = TradeLookup.GetExchange(anchor.Exchange);
            exchange.Authority.Should().Be(TradeAuthority.None, anchor.Id);
            anchor.Exchange.Should().Be(TradeLookup.GetExchangeForPlanet(anchor.HomePlanet), anchor.Id);
        }

        StationRules.GetExchange("VeshPulsar1").Should().Be(TradeExchangeType.Dathomir);
        StationRules.GetExchange("KorvaxMagnetar1").Should().Be(TradeExchangeType.Hutlar);
        StationRules.GetExchange("EmberGiant1").Should().Be(TradeExchangeType.Hutlar);
    }

    // The passenger shuttle

    private static ShuttleStop OrbitStation => ShuttleStop.ForStation("station-1", PlanetType.Tatooine, false);
    private static ShuttleStop WildStation => ShuttleStop.ForStation("station-2", PlanetType.Hutlar, true);

    [Test]
    public void AStationFliesOnlyToItsHomePort()
    {
        ShuttleRouteRules.IsValid(new ShuttleRoute(OrbitStation, ShuttleStop.ForPlanet(PlanetType.Tatooine))).Should().BeTrue();
        ShuttleRouteRules.IsValid(new ShuttleRoute(ShuttleStop.ForPlanet(PlanetType.Tatooine), OrbitStation)).Should().BeTrue();
        ShuttleRouteRules.IsValid(new ShuttleRoute(OrbitStation, ShuttleStop.ForPlanet(PlanetType.Hutlar))).Should().BeFalse();
        ShuttleRouteRules.IsValid(new ShuttleRoute(OrbitStation, WildStation)).Should().BeFalse();
        ShuttleRouteRules.IsValid(new ShuttleRoute(ShuttleStop.ForPlanet(PlanetType.Tatooine), ShuttleStop.ForPlanet(PlanetType.Tatooine))).Should().BeFalse();
        ShuttleRouteRules.IsValid(new ShuttleRoute(ShuttleStop.ForPlanet(PlanetType.Tatooine), ShuttleStop.ForPlanet(PlanetType.Hutlar))).Should().BeTrue();
    }

    [Test]
    public void APlanetTerminalListsTheStationsItServes()
    {
        var stations = new[] { OrbitStation, WildStation, ShuttleStop.ForStation("station-3", PlanetType.Tatooine, false) };
        ShuttleRouteRules.GetStationDestinations(ShuttleStop.ForPlanet(PlanetType.Tatooine), stations)
            .Select(x => x.StationId).Should().BeEquivalentTo("station-1", "station-3");
        ShuttleRouteRules.GetStationDestinations(ShuttleStop.ForPlanet(PlanetType.Hutlar), stations)
            .Select(x => x.StationId).Should().Equal("station-2");
        ShuttleRouteRules.GetStationDestinations(OrbitStation, stations).Should().BeEmpty();
    }

    [Test]
    public void AnOrbitStationIsAnOrbitalHopAndAWildSpaceStationAFullFlight()
    {
        var hop = new ShuttleRoute(OrbitStation, ShuttleStop.ForPlanet(PlanetType.Tatooine));
        ShuttleRouteRules.GetFare(hop).Should().Be(StationTuning.OrbitHopFare);
        ShuttleRouteRules.GetTransitSeconds(hop).Should().Be(StationTuning.OrbitHopSeconds);
        ShuttleRouteRules.GetPeriodSeconds(hop).Should().Be(StationTuning.OrbitHopPeriodSeconds);
        StationTuning.OrbitHopFare.Should().Be(GalaxyMap.GetFare(PlanetType.Viscara, PlanetType.CZ220));
        StationTuning.OrbitHopSeconds.Should().Be(GalaxyMap.GetTransitSeconds(PlanetType.Viscara, PlanetType.CZ220));

        var wild = new ShuttleRoute(ShuttleStop.ForPlanet(PlanetType.Hutlar), WildStation);
        ShuttleRouteRules.GetFare(wild).Should().Be(StationTuning.WildSpaceFare);
        ShuttleRouteRules.GetTransitSeconds(wild).Should().Be(StationTuning.WildSpaceSeconds);
        ShuttleRouteRules.GetPeriodSeconds(wild).Should().Be(StationTuning.WildSpacePeriodSeconds);

        var planets = new ShuttleRoute(ShuttleStop.ForPlanet(PlanetType.Hutlar), ShuttleStop.ForPlanet(PlanetType.Tatooine));
        ShuttleRouteRules.GetFare(planets).Should().Be(GalaxyMap.GetFare(PlanetType.Hutlar, PlanetType.Tatooine));
    }

    [Test]
    public void StationRoutesRunOnTheirOwnSchedule()
    {
        var hop = new ShuttleRoute(OrbitStation, ShuttleStop.ForPlanet(PlanetType.Tatooine));
        var start = new DateTime(2026, 10, 28, 0, 0, 0, DateTimeKind.Utc);
        var departures = ShuttleSchedule.GetDeparturesBetween(hop, start, start.AddMinutes(10)).ToList();
        departures.Should().HaveCount(10);
        departures.Zip(departures.Skip(1), (a, b) => (b - a).TotalSeconds).Should().OnlyContain(x => x == StationTuning.OrbitHopPeriodSeconds);

        var back = new ShuttleRoute(ShuttleStop.ForPlanet(PlanetType.Tatooine), OrbitStation);
        ShuttleSchedule.GetRouteHash(hop).Should().NotBe(ShuttleSchedule.GetRouteHash(back));
        ShuttleSchedule.BuildFlightId(hop, start).Should().Contain("station-1");

        // A planet route keeps its old hash, schedule and flight id.
        var planets = new ShuttleRoute(ShuttleStop.ForPlanet(PlanetType.Viscara), ShuttleStop.ForPlanet(PlanetType.Tatooine));
        ShuttleSchedule.GetRouteHash(planets).Should().Be(ShuttleSchedule.GetRouteHash(PlanetType.Viscara, PlanetType.Tatooine));
        ShuttleSchedule.GetNextDepartureUtc(planets, start).Should().Be(ShuttleSchedule.GetNextDepartureUtc(PlanetType.Viscara, PlanetType.Tatooine, start));
        ShuttleSchedule.BuildFlightId(planets, start).Should().Be(ShuttleSchedule.BuildFlightId(PlanetType.Viscara, PlanetType.Tatooine, start));
    }

    // Wiring

    [Test]
    public void StationsAreWiredIntoTheCitySystem()
    {
        var property = ReadServer("Service", "Property.cs");
        property.Should().Contain("structureType == StructureType.StationCore").And.Contain("Station.BeginFounding");
        property.Should().Contain("SpawnStationDeck(property)").And.Contain("StationDeckRules.GetPlacement");
        property.Should().Contain("CityUpkeepRules.GetWeeklyUpkeep");

        ReadServer("Service", "TradeCity.cs").Should().Contain("StationRules.GetExchange(city.StationAnchorId)");
        ReadServer("Service", "SpaceAccess.cs").Should().Contain("IsStationStarport =");
        ReadServer("Feature", "PropertyLayoutDefinition", "StarportLayoutDefinition.cs").Should().Contain("Station.RegisterDockPoint");
        ReadServer("Feature", "DialogDefinition", "StarportDockDialog.cs").Should().Contain("Station.GetDockPoints");
        ReadServer("Feature", "DialogDefinition", "StarportFlightsDialog.cs").Should().Contain("ShuttleRouteRules.GetStationDestinations");
        ReadServer("Service", "PropertyService", "StructureChangedAction.cs").Should().Contain("Assign(StructureType.StationCore, StructureChangeType.PositionChanged");
        ReadServer("Service", "Death.cs").Should().Contain("RespawnPropertyId");
    }

    // Files

    private static JsonElement Value(JsonElement element, string name) => element.GetProperty(name).GetProperty("value");

    private static JsonDocument ReadModule(string kind, string resref)
    {
        var bytes = File.ReadAllBytes(Path.Combine(RepositoryRoot, "Module", kind, $"{resref}.{kind}.json"));
        return JsonDocument.Parse(Encoding.Latin1.GetString(bytes));
    }

    private static string ReadServer(params string[] path) =>
        File.ReadAllText(Path.Combine(new[] { RepositoryRoot, "SWLOR.Game.Server" }.Concat(path).ToArray()));

    private static string RepositoryRoot
    {
        get
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                directory = directory.Parent;
            return directory!.FullName;
        }
    }
}
