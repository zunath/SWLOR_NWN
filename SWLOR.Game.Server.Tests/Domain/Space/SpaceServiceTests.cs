using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Space;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Space;

/// <summary>
/// Unit tests of the space instance (P2-A9): the tables it owns, the slot-to-feat conversions and the landing point
/// registry, whose state used to be static and now belongs to the instance. The engine is a fake with local variables,
/// the planet service is scripted, and every other neighbour is unused by what these tests reach.
/// </summary>
[TestFixture]
public class SpaceServiceTests
{
    private const uint Waypoint1 = 2001;
    private const uint Waypoint2 = 2002;
    private const uint Waypoint3 = 2003;
    private const uint ViscaraArea = 3001;
    private const uint UnknownArea = 3002;

    private FakeNWScript _engine = null!;
    private LocalVariables _locals = null!;
    private IDisposable _scope = null!;
    private SpaceService _space = null!;

    [SetUp]
    public void SetUp()
    {
        _engine = new FakeNWScript();
        _locals = new LocalVariables(_engine);
        _engine.SetReturnHandler("GetLocation", arguments => EngineStructures.Create<Location>((nint)(uint)arguments[0]!));
        _engine.SetReturnHandler("GetName", arguments => "Dock " + arguments[0]);
        _scope = NWScriptRuntime.Override(_engine);
        _space = NewSpace();
    }

    [TearDown]
    public void TearDown() => _scope.Dispose();

    private static SpaceService NewSpace()
    {
        var planet = ScriptedProxy.Create<IPlanetService>(out var scripted);
        scripted.On("GetPlanetType", arguments => (uint)arguments[0]! == ViscaraArea ? PlanetType.Viscara : PlanetType.Invalid);

        return new SpaceService(
            ability: null!, abilityCooldownVisual: null!, ai: null!, area: null!, db: null!, enmity: null!, gui: null!,
            messaging: null!, perk: null!, planet: planet, playerName: null!, property: null!, race: null!, recast: null!,
            stat: null!, statusEffect: null!);
    }

    // ---- slot and feat conversions

    [TestCase(1, FeatType.ShipModule1)]
    [TestCase(5, FeatType.ShipModule5)]
    [TestCase(10, FeatType.ShipModule10)]
    public void HighSlotToFeat_MapsTheTenHighSlotsToTheFirstTenFeats(int slot, FeatType expected)
    {
        _space.HighSlotToFeat(slot).Should().Be(expected);
        _space.HighFeatToSlot(expected).Should().Be(slot);
    }

    [TestCase(1, FeatType.ShipModule11)]
    [TestCase(5, FeatType.ShipModule15)]
    [TestCase(10, FeatType.ShipModule20)]
    public void LowSlotToFeat_MapsTheTenLowSlotsAfterTheHighOnes(int slot, FeatType expected)
    {
        _space.LowSlotToFeat(slot).Should().Be(expected);
        _space.LowFeatToSlot(expected).Should().Be(slot);
    }

    [TestCase(FeatType.ShipModule1, 1)]
    [TestCase(FeatType.ShipModule17, 17)]
    [TestCase(FeatType.ShipModule30, 30)]
    public void GetFeatSlotNumber_CountsFromTheFirstShipModuleFeat(FeatType feat, int expected)
    {
        _space.GetFeatSlotNumber(feat).Should().Be(expected);
    }

    // ---- tables

    [Test]
    public void ShipModuleFeats_HoldThirtySlotsAndOnlyTheFirstTwentyHaveARecastGroup()
    {
        _space.ShipModuleFeats.Should().HaveCount(30);
        foreach (var (feat, detail) in _space.ShipModuleFeats)
        {
            var slot = _space.GetFeatSlotNumber(feat);
            detail.SlotName.Should().Be("Slot #" + slot);
            (detail.RecastGroup != RecastGroup.Invalid).Should().Be(slot <= 20, "slot " + slot);
        }
    }

    [Test]
    public void ShipModuleFeats_AreOwnedByTheInstance()
    {
        var other = NewSpace();

        other.ShipModuleFeats.Should().NotBeSameAs(_space.ShipModuleFeats);
        _space.ShipModuleFeats.Remove(FeatType.ShipModule1);
        other.ShipModuleFeats.Should().ContainKey(FeatType.ShipModule1);
    }

    [Test]
    public void Registries_AreEmptyUntilTheSpaceDataLoads()
    {
        _space.IsRegisteredShip("ShipDeedLightFreighter").Should().BeFalse();
        _space.IsRegisteredShipModule("ship_laser_1").Should().BeFalse();
        _space.Invoking(x => x.GetShipDetailByItemTag("ShipDeedLightFreighter")).Should().Throw<KeyNotFoundException>();
        _space.Invoking(x => x.GetShipModuleDetailByItemTag("ship_laser_1")).Should().Throw<KeyNotFoundException>();
    }

    // ---- landing points

    [Test]
    public void RegisterLandingPoint_OnARecognizedPlanet_AddsADockPointNamedAfterTheWaypoint()
    {
        _space.RegisterLandingPoint(Waypoint1, ViscaraArea, true, string.Empty);

        var points = _space.GetDockPointsByPlanet(PlanetType.Viscara);
        points.Should().ContainSingle();
        var point = points.Values.Single();
        point.Name.Should().Be("Dock " + Waypoint1);
        point.IsNPC.Should().BeTrue();
        point.PropertyId.Should().BeEmpty();
        _locals.String(Waypoint1, "STARSHIP_DOCKPOINT_ID").Should().Be(points.Keys.Single());
    }

    [Test]
    public void RegisterLandingPoint_OnAnUnrecognizedPlanet_IsIgnored()
    {
        _space.RegisterLandingPoint(Waypoint1, UnknownArea, true, string.Empty);

        _space.GetDockPointsByPlanet(PlanetType.Viscara).Should().BeEmpty();
        _locals.Has(Waypoint1, "STARSHIP_DOCKPOINT_ID").Should().BeFalse();
    }

    [Test]
    public void RegisterLandingPoint_ForAWaypointAlreadyRegistered_KeepsOnePoint()
    {
        _space.RegisterLandingPoint(Waypoint1, ViscaraArea, true, string.Empty);
        var id = _space.GetDockPointsByPlanet(PlanetType.Viscara).Keys.Single();

        _space.RegisterLandingPoint(Waypoint1, ViscaraArea, true, string.Empty);

        _space.GetDockPointsByPlanet(PlanetType.Viscara).Keys.Should().Equal(id);
    }

    [Test]
    public void RegisterLandingPoint_ForAPlayerStarport_ReplacesTheEarlierDockOfThatProperty()
    {
        _space.RegisterLandingPoint(Waypoint1, ViscaraArea, false, "property-1");
        _space.RegisterLandingPoint(Waypoint2, ViscaraArea, false, "property-1");
        _space.RegisterLandingPoint(Waypoint3, ViscaraArea, false, "property-2");

        var points = _space.GetDockPointsByPlanet(PlanetType.Viscara).Values;
        points.Select(x => x.PropertyId).Should().BeEquivalentTo("property-1", "property-2");
        points.Should().OnlyContain(x => x.Name == string.Empty && !x.IsNPC, "a property dock is named by its property, not its waypoint");
    }

    [Test]
    public void RemoveLandingPoint_ForgetsThatWaypointOnly()
    {
        _space.RegisterLandingPoint(Waypoint1, ViscaraArea, true, string.Empty);
        _space.RegisterLandingPoint(Waypoint2, ViscaraArea, true, string.Empty);

        _space.RemoveLandingPoint(Waypoint1, ViscaraArea);

        _space.GetDockPointsByPlanet(PlanetType.Viscara).Values.Select(x => x.Name).Should().Equal("Dock " + Waypoint2);
    }

    [Test]
    public void RemoveLandingPointByPropertyId_RemovesEveryDockOfThatPropertyOnly()
    {
        _space.RegisterLandingPoint(Waypoint1, ViscaraArea, true, string.Empty);
        _space.RegisterLandingPoint(Waypoint2, ViscaraArea, false, "property-1");

        _space.RemoveLandingPointByPropertyId(string.Empty);
        _space.GetDockPointsByPlanet(PlanetType.Viscara).Should().HaveCount(2, "a blank id removes nothing");

        _space.RemoveLandingPointByPropertyId("property-1");
        _space.GetDockPointsByPlanet(PlanetType.Viscara).Values.Select(x => x.Name).Should().Equal("Dock " + Waypoint1);
    }

    [Test]
    public void GetDockPointsByPlanet_ReturnsACopyOfTheRegistry()
    {
        _space.RegisterLandingPoint(Waypoint1, ViscaraArea, true, string.Empty);

        _space.GetDockPointsByPlanet(PlanetType.Viscara).Clear();

        _space.GetDockPointsByPlanet(PlanetType.Viscara).Should().ContainSingle();
        _space.GetDockPointsByPlanet(PlanetType.Tatooine).Should().BeEmpty();
    }

    // ---- loading the landing points at boot (P4-8: AssignLandingPointIds, boot only, then LoadLandingPoints, rerun)

    private const uint Waypoint4 = 2004;
    private const uint ObjectInvalid = 0x7F000000;
    private const string DockIdVariable = "STARSHIP_DOCKPOINT_ID";

    /// <summary>
    /// The module's dock waypoints: 1 and 2 on Viscara (2 with a stale id an earlier boot left), 3 on an unknown planet and
    /// 4 the dock of a player starport, registered before (the property loader runs earlier in mod_load).
    /// </summary>
    private void PlaceTheDockWaypoints(SpaceService space)
    {
        var waypoints = new[] { Waypoint1, Waypoint2, Waypoint3, Waypoint4 };
        _engine.SetReturnHandler("GetObjectByTag", arguments =>
            (string)arguments[0]! == "STARSHIP_DOCKPOINT" && (int)arguments[1]! < waypoints.Length ? waypoints[(int)arguments[1]!] : ObjectInvalid);
        _engine.SetReturnHandler("GetIsObjectValid", arguments => (uint)arguments[0]! != ObjectInvalid);
        _engine.SetReturnHandler("GetArea", arguments => (uint)arguments[0]! == Waypoint3 ? UnknownArea : ViscaraArea);
        _engine.SetLocalString(Waypoint2, DockIdVariable, "stale-id");
        space.RegisterLandingPoint(Waypoint4, ViscaraArea, false, "property-1");
        _locals.ClearCalls();
    }

    /// <summary>The registry as text, in order, with each key checked against the id its waypoint carries.</summary>
    private List<string> Registry(SpaceService space)
    {
        return space.GetDockPointsByPlanet(PlanetType.Viscara)
            .Select(entry =>
            {
                var waypoint = new[] { Waypoint1, Waypoint2, Waypoint3, Waypoint4 }.Single(id => _locals.String(id, DockIdVariable) == entry.Key);
                return $"{waypoint} {entry.Value.Name}|{entry.Value.IsNPC}|{entry.Value.PropertyId}|{entry.Value.Location.Handle}";
            })
            .ToList();
    }

    [Test]
    public void AssignLandingPointIds_then_LoadLandingPoints_register_the_dock_waypoints_as_registering_each_one_as_an_npc_dock_did()
    {
        // What the single handler did: RegisterLandingPoint(waypoint, its area, NPC, no property) for every dock waypoint.
        PlaceTheDockWaypoints(_space);
        for (var nth = 0; nth < 4; nth++)
        {
            var waypoint = _engine.GetObjectByTag("STARSHIP_DOCKPOINT", nth);
            _space.RegisterLandingPoint(waypoint, _engine.GetArea(waypoint), true, string.Empty);
        }
        var expected = Registry(_space);
        _scope.Dispose();

        _engine = new FakeNWScript();
        _locals = new LocalVariables(_engine);
        _engine.SetReturnHandler("GetLocation", arguments => EngineStructures.Create<Location>((nint)(uint)arguments[0]!));
        _engine.SetReturnHandler("GetName", arguments => "Dock " + arguments[0]);
        _scope = NWScriptRuntime.Override(_engine);
        var split = NewSpace();
        PlaceTheDockWaypoints(split);

        split.AssignLandingPointIds();

        split.GetDockPointsByPlanet(PlanetType.Viscara).Should().ContainSingle("the boot-only part writes the ids and registers nothing");
        _locals.String(Waypoint2, DockIdVariable).Should().NotBe("stale-id", "a stale id is replaced by a new one");
        _locals.Has(Waypoint3, DockIdVariable).Should().BeFalse("a waypoint on an unknown planet is not tracked");

        split.LoadLandingPoints();

        Registry(split).Should().Equal(expected);
        expected.Should().HaveCount(3);
        expected[0].Should().StartWith(Waypoint4 + " |False|property-1", "the starport's dock stays as the property registered it");
    }

    [Test]
    public void LoadLandingPoints_alone_finds_the_ids_the_boot_gave_the_waypoints_and_writes_no_local()
    {
        PlaceTheDockWaypoints(_space);
        _space.AssignLandingPointIds();
        _space.LoadLandingPoints();
        var booted = Registry(_space);

        // A hot reload: a new build with an empty registry reruns only LoadLandingPoints over the same world.
        var reloaded = NewSpace();
        reloaded.RegisterLandingPoint(Waypoint4, ViscaraArea, false, "property-1");
        _locals.String(Waypoint4, DockIdVariable).Should().NotBeEmpty();
        var starportId = _locals.String(Waypoint4, DockIdVariable);
        _locals.ClearCalls();

        reloaded.LoadLandingPoints();

        _engine.CountOf("SetLocalString").Should().Be(0);
        _engine.CountOf("DeleteLocalString").Should().Be(0);
        Registry(reloaded).Where(line => !line.StartsWith(Waypoint4.ToString())).Should()
            .Equal(booted.Where(line => !line.StartsWith(Waypoint4.ToString())), "the NPC docks keep the ids ships hold");
        _locals.String(Waypoint4, DockIdVariable).Should().Be(starportId);
    }

    [Test]
    public void LandingPoints_AreOwnedByTheInstance()
    {
        var other = NewSpace();

        _space.RegisterLandingPoint(Waypoint1, ViscaraArea, true, string.Empty);

        other.GetDockPointsByPlanet(PlanetType.Viscara).Should().BeEmpty();
    }
}
