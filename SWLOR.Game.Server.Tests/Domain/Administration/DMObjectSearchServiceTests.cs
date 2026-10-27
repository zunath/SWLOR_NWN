using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Service.DMToolService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Area;
using SWLOR.NWN.API.Testing;
using static SWLOR.Game.Server.Tests.Support.DMPossessionEngine;

namespace SWLOR.Game.Server.Tests.Domain.Administration;

/// <summary>
/// Unit tests of the DM object search instance. A search is a scan the chooser continues across ticks; here it runs to
/// the end against a model of the areas and the objects in them.
/// </summary>
[TestFixture]
public class DMObjectSearchServiceTests
{
    private const uint DM = 1;
    private const uint Creature = 2;
    private const uint Cantina = 100;
    private const uint Spaceport = 101;
    private const uint Guard = 200;
    private const uint Thug = 201;
    private const uint Barkeep = 202;

    private DMPossessionEngine _world = null!;
    private DMObjectSearchService _service = null!;
    private readonly Dictionary<(uint Area, ObjectType Type), List<uint>> _objects = new();
    private readonly Dictionary<(uint Area, ObjectType Type), int> _cursor = new();

    [SetUp]
    public void SetUp()
    {
        _world = new DMPossessionEngine();
        _service = new DMObjectSearchService(new DMPresenceService());
        _objects.Clear();
        _cursor.Clear();
        _world.Engine.SetReturnSequence("GetFirstArea", Cantina);
        _world.Engine.SetReturnSequence("GetNextArea", Spaceport, Invalid);
        _world.Engine.SetReturnHandler("GetFirstObjectInArea", arguments =>
        {
            var key = ((uint)arguments[0]!, (ObjectType)arguments[1]!);
            _cursor[key] = 0;
            return NextObject(key);
        });
        _world.Engine.SetReturnHandler("GetNextObjectInArea", arguments => NextObject(((uint)arguments[0]!, (ObjectType)arguments[1]!)));
        _world.Name(Cantina, "Cantina");
        _world.Name(Spaceport, "Spaceport");
        _world.Name(Guard, "Guard");
        _world.Name(Thug, "Thug");
        _world.Name(Barkeep, "Barkeep");
    }

    [TearDown]
    public void TearDown()
    {
        _world.Dispose();
    }

    private uint NextObject((uint Area, ObjectType Type) key)
    {
        if (!_objects.TryGetValue(key, out var list))
            return Invalid;

        var index = _cursor.GetValueOrDefault(key);
        if (index >= list.Count)
            return Invalid;

        _cursor[key] = index + 1;
        return list[index];
    }

    private void Place(uint area, ObjectType type, params uint[] objects)
    {
        _objects[(area, type)] = objects.ToList();
        foreach (var obj in objects)
            _world.Place(obj, area);
    }

    private static List<uint> Run(DMObjectScan scan)
    {
        scan.Continue(int.MaxValue).Should().BeTrue("the scan reads every area it was given");
        return scan.Results.Select(result => result.Object).ToList();
    }

    [Test]
    public void StartSearch_ForAreasMatchesEveryAreaByName()
    {
        var scan = _service.StartSearch(DM, DMObjectCategory.Areas, DMObjectSearchScope.CurrentArea, "port");

        Run(scan).Should().Equal(Spaceport);
    }

    [Test]
    public void StartSearch_ForAreasIgnoresTheScope()
    {
        var scan = _service.StartSearch(DM, DMObjectCategory.Areas, DMObjectSearchScope.CurrentArea, string.Empty);

        Run(scan).Should().Equal(Cantina, Spaceport);
    }

    [Test]
    public void StartSearch_InTheCurrentAreaReadsTheAreaOfTheCreatureTheDMControls()
    {
        _world.AddPlayer(DM, isDM: true, area: Spaceport);
        _world.Possess(DM, Creature, DMPossess, Cantina);
        Place(Cantina, ObjectType.Creature, Guard, Thug);
        Place(Spaceport, ObjectType.Creature, Barkeep);

        var scan = _service.StartSearch(DM, DMObjectCategory.Creatures, DMObjectSearchScope.CurrentArea, string.Empty);

        Run(scan).Should().Equal(Guard, Thug);
        _world.Engine.CallsTo("GetFirstObjectInArea").Select(call => (uint)call.Arguments[0]!).Should().Equal(Cantina);
    }

    [Test]
    public void StartSearch_InAllAreasReadsEveryArea()
    {
        _world.AddPlayer(DM, isDM: true, area: Spaceport);
        Place(Cantina, ObjectType.Creature, Guard);
        Place(Spaceport, ObjectType.Creature, Barkeep);

        var scan = _service.StartSearch(DM, DMObjectCategory.Creatures, DMObjectSearchScope.AllAreas, string.Empty);

        Run(scan).Should().Equal(Barkeep, Guard);
        _world.Engine.CallsTo("GetFirstObjectInArea").Select(call => (uint)call.Arguments[0]!).Should().Equal(Cantina, Spaceport);
    }

    [Test]
    public void StartSearch_FiltersByTheSearchWords()
    {
        _world.AddPlayer(DM, isDM: true, area: Cantina);
        Place(Cantina, ObjectType.Creature, Guard, Thug);

        var scan = _service.StartSearch(DM, DMObjectCategory.Creatures, DMObjectSearchScope.CurrentArea, "thu");

        Run(scan).Should().Equal(Thug);
    }

    [TestCase(DMObjectCategory.Creatures, ObjectType.Creature)]
    [TestCase(DMObjectCategory.Placeables, ObjectType.Placeable)]
    [TestCase(DMObjectCategory.Items, ObjectType.Item)]
    [TestCase(DMObjectCategory.Doors, ObjectType.Door)]
    [TestCase(DMObjectCategory.Triggers, ObjectType.Trigger)]
    [TestCase(DMObjectCategory.Waypoints, ObjectType.Waypoint)]
    [TestCase(DMObjectCategory.Stores, ObjectType.Store)]
    [TestCase(DMObjectCategory.Encounters, ObjectType.Encounter)]
    public void StartSearch_ReadsTheObjectTypeOfTheCategory(DMObjectCategory category, ObjectType type)
    {
        _world.AddPlayer(DM, isDM: true, area: Cantina);
        Place(Cantina, type, Guard);

        var scan = _service.StartSearch(DM, category, DMObjectSearchScope.CurrentArea, string.Empty);

        Run(scan).Should().Equal(Guard);
        _world.Engine.CallsTo("GetFirstObjectInArea").Should().ContainSingle().Which.Arguments[1].Should().Be(type);
    }

    [Test]
    public void StartSearch_RefusesACategoryItDoesNotKnow()
    {
        _world.AddPlayer(DM, isDM: true, area: Cantina);

        var start = () => _service.StartSearch(DM, (DMObjectCategory)99, DMObjectSearchScope.CurrentArea, string.Empty);

        start.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void GetAreaJumpLocation_PrefersAWaypoint()
    {
        var waypointLocation = EngineStructures.Create<Location>(11);
        _world.Engine.SetReturnHandler("GetFirstObjectInArea", arguments =>
            (ObjectType)arguments[1]! == ObjectType.Waypoint ? Guard : Invalid);
        _world.Engine.SetReturn("GetLocation", waypointLocation);

        var location = _service.GetAreaJumpLocation(Cantina);

        location.Should().BeSameAs(waypointLocation);
        _world.Engine.CallsTo("GetLocation").Should().ContainSingle().Which.Arguments[0].Should().Be(Guard);
    }

    [Test]
    public void GetAreaJumpLocation_FallsBackToAnyPlacedObject()
    {
        var anchorLocation = EngineStructures.Create<Location>(12);
        _world.Engine.SetReturnHandler("GetFirstObjectInArea", arguments =>
            (ObjectType)arguments[1]! == (ObjectType.Door | ObjectType.Placeable | ObjectType.Creature) ? Thug : Invalid);
        _world.Engine.SetReturn("GetLocation", anchorLocation);

        var location = _service.GetAreaJumpLocation(Cantina);

        location.Should().BeSameAs(anchorLocation);
        _world.Engine.CallsTo("GetLocation").Should().ContainSingle().Which.Arguments[0].Should().Be(Thug);
    }

    [Test]
    public void GetAreaJumpLocation_FallsBackToTheCenterOfTheArea()
    {
        var center = EngineStructures.Create<Location>(13);
        _world.Engine.SetReturnHandler("GetAreaSize", arguments => (Dimension)arguments[0]! == Dimension.Width ? 4 : 3);
        _world.Engine.SetReturn("Location", center);

        var location = _service.GetAreaJumpLocation(Cantina);

        location.Should().BeSameAs(center);
        var call = _world.Engine.CallsTo("Location").Should().ContainSingle().Subject;
        call.Arguments[0].Should().Be(Cantina);
        call.Arguments[1].Should().Be(new Vector3(20f, 15f, 0f), "the area is 4 by 3 tiles of 10 meters");
        call.Arguments[2].Should().Be(0f);
    }
}
