using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// Ships that docked or registered at Eshan before its orbit waypoint moved saved the old spot, on the starlane
/// arrival point, as the orbit they launch to. Server migration 32 moves them to the new spot.
/// </summary>
public class EshanOrbitMigrationTests
{
    private static readonly PropertyLocation NewOrbit = new()
    {
        AreaResref = "eshanorbit", X = 227.8944f, Y = 242.8683f, Z = 3f, Orientation = 217.97f
    };

    private static PropertyLocation At(string area, float x, float y) => new()
    {
        AreaResref = area, X = x, Y = y, Z = 3f, Orientation = 35.9f
    };

    private static PropertyLocation OldOrbit() => At(
        "eshanorbit", _32_NextReleaseServerMigration.OldEshanOrbitX, _32_NextReleaseServerMigration.OldEshanOrbitY);

    [Test]
    public void OldOrbitSpot_IsTheStarlaneArrivalPointTheWaypointSatOn()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "Module")))
            directory = directory.Parent;
        var area = JObject.Parse(File.ReadAllText(Path.Combine(directory!.FullName, "Module", "git", "eshanorbit.git.json")));
        var waypoints = area["WaypointList"]!["value"]!;
        var starlane = waypoints.Single(w => w["Tag"]!["value"]!.Value<string>() == "STARLANE_LANDING_ESHAN_1");
        var orbit = waypoints.Single(w => w["Tag"]!["value"]!.Value<string>() == "Eshan_Orbit");

        starlane["XPosition"]!["value"]!.Value<float>().Should().Be(_32_NextReleaseServerMigration.OldEshanOrbitX);
        starlane["YPosition"]!["value"]!.Value<float>().Should().Be(_32_NextReleaseServerMigration.OldEshanOrbitY);
        orbit["XPosition"]!["value"]!.Value<float>().Should().NotBe(_32_NextReleaseServerMigration.OldEshanOrbitX,
            "the migration must move ships to a different spot than the one it replaces");
    }

    [Test]
    public void SavedOrbitsAtTheOldSpot_MoveToTheNewOrbit()
    {
        var current = At("eshanorbit", 40.49f, 26.04f);
        var dock = At("pwsc_eshstarport", 10f, 10f);
        var positions = new Dictionary<PropertyLocationType, PropertyLocation>
        {
            [PropertyLocationType.SpacePosition] = OldOrbit(),
            [PropertyLocationType.LastNPCSpacePosition] = OldOrbit(),
            [PropertyLocationType.CurrentPosition] = current,
            [PropertyLocationType.DockPosition] = dock,
        };

        _32_NextReleaseServerMigration.MoveToNewEshanOrbit(positions, NewOrbit).Should().BeTrue();

        positions[PropertyLocationType.SpacePosition].Should().BeEquivalentTo(NewOrbit);
        positions[PropertyLocationType.LastNPCSpacePosition].Should().BeEquivalentTo(NewOrbit);
        positions[PropertyLocationType.SpacePosition].Should().NotBeSameAs(positions[PropertyLocationType.LastNPCSpacePosition]);
        positions[PropertyLocationType.CurrentPosition].Should().BeSameAs(current, "a ship out in space stays where it is");
        positions[PropertyLocationType.DockPosition].Should().BeSameAs(dock);

        _32_NextReleaseServerMigration.MoveToNewEshanOrbit(positions, NewOrbit).Should().BeFalse("a second run changes nothing");
        positions[PropertyLocationType.SpacePosition].Should().BeEquivalentTo(NewOrbit);
    }

    [Test]
    public void OtherOrbits_AreLeftAlone()
    {
        var dantooine = At("dantooineorbit", _32_NextReleaseServerMigration.OldEshanOrbitX, _32_NextReleaseServerMigration.OldEshanOrbitY);
        var elsewhereInEshan = At("eshanorbit", 100f, 100f);
        var positions = new Dictionary<PropertyLocationType, PropertyLocation>
        {
            [PropertyLocationType.SpacePosition] = dantooine,
            [PropertyLocationType.LastNPCSpacePosition] = elsewhereInEshan,
        };

        _32_NextReleaseServerMigration.MoveToNewEshanOrbit(positions, NewOrbit).Should().BeFalse();
        positions[PropertyLocationType.SpacePosition].Should().BeSameAs(dantooine);
        positions[PropertyLocationType.LastNPCSpacePosition].Should().BeSameAs(elsewhereInEshan);

        _32_NextReleaseServerMigration.MoveToNewEshanOrbit(new Dictionary<PropertyLocationType, PropertyLocation>(), NewOrbit)
            .Should().BeFalse("a ship without saved orbits has nothing to move");
    }
}
