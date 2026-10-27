using System.Numerics;
using System.Reflection;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ShuttleService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Service;

[NonParallelizable]
public sealed class PassengerShuttleInteriorTests
{
    [TestCase(false, false, "starship1_int")]
    [TestCase(true, false, "starship1_int")]
    [TestCase(false, true, "starship1_int")]
    [TestCase(true, true, "shuttle")]
    public void TemplateSelectionSupportsPartialDeployments(bool entrance, bool pilot, string expected)
    {
        var objects = new List<(string Tag, ObjectType Type)>();
        if (entrance) objects.Add(("PROPERTY_ENTRANCE", ObjectType.Waypoint));
        if (pilot) objects.Add(("pilot_chair", ObjectType.Placeable));
        SelectInterior(objects).Should().Be(expected);
    }

    [Test]
    public void WrongObjectTypesCannotEnableTheNewInterior()
    {
        SelectInterior(new[] { ("PROPERTY_ENTRANCE", ObjectType.Placeable), ("pilot_chair", ObjectType.Creature) })
            .Should().Be("starship1_int");
    }

    [TestCase("shuttle")]
    [TestCase("starship1_int")]
    public void BothDeployedTemplatesSatisfyTheBoardingContract(string resref)
    {
        var contents = ReadResource("git", resref);
        var objects = Objects(contents, "WaypointList").Select(obj => (Value<string>(obj, "Tag"), ObjectType.Waypoint))
            .Concat(Objects(contents, "Placeable List").Select(obj => (Value<string>(obj, "Tag"), ObjectType.Placeable)));
        SelectInterior(objects).Should().Be("shuttle");
    }

    /// <summary>
    /// The interior the shuttle instance copies for a flight when the module's shuttle template holds these objects:
    /// a passenger logs in to a flight still in the air, so the instance creates the interior on demand.
    /// </summary>
    private static string SelectInterior(IEnumerable<(string Tag, ObjectType Type)> objects)
    {
        var now = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);
        using var bench = new ShuttleBench(now);
        var template = bench.AddArea("shuttle");
        foreach (var (tag, type) in objects)
            bench.AddObject(tag, type, template);
        var passenger = bench.AddPlayer("passenger", bench.AddArea("starport"), new Vector3(1f, 1f, 0f));
        bench.Db.Inner.With(new Player("passenger") { RebuildComplete = true });
        bench.Db.Inner.With(new ShuttleRide("passenger")
        {
            Status = ShuttleRideStatus.InTransit,
            Origin = PlanetType.Viscara,
            Destination = PlanetType.Tatooine,
            FlightId = "flight",
            ArrivalUtc = now.AddMinutes(5)
        });

        bench.Service.TryGetLoginRedirect(passenger.Id, out _).Should().BeTrue();

        return bench.Areas.Created.Should().ContainSingle().Subject.Source;
    }

    [Test]
    public void FlightTemplateHasABoardingPointAndUsablePilotChair()
    {
        var resref = typeof(Shuttle).GetField("ShuttleInteriorResref",
            BindingFlags.NonPublic | BindingFlags.Static)!.GetRawConstantValue()!.ToString();
        var area = ReadResource("are", resref!);
        var contents = ReadResource("git", resref!);

        area["Name"]!["value"]!["0"]!.Value<string>()
            .Should().Be("[Prefab] Starship Shuttle - Interior");
        var entrance = Objects(contents, "WaypointList")
            .Single(point => Value<string>(point, "Tag") == "PROPERTY_ENTRANCE");
        Value<float>(entrance, "XPosition").Should().BeInRange(40, 50);
        Value<float>(entrance, "YPosition").Should().BeInRange(20, 30);

        var pilot = Objects(contents, "Placeable List")
            .Single(placeable => Value<string>(placeable, "Tag") == "pilot_chair");
        Value<int>(pilot, "Static").Should().Be(0);
        Value<int>(pilot, "Useable").Should().Be(1);
    }

    [Test]
    public void CenterFacingPassengerSittingPlaceablesAreRemoved()
    {
        Objects(ReadResource("git", "shuttle"), "Placeable List")
            .Should().NotContain(placeable => Value<string>(placeable, "Tag").StartsWith("shuttle_seat_"));
    }

    [Test]
    public void ToolsetMetadataMatchesPlacedObjects()
    {
        var contents = ReadResource("git", "shuttle");
        var metadata = ReadResource("gic", "shuttle");
        foreach (var list in new[] { "Placeable List", "WaypointList" })
            Objects(metadata, list).Count().Should().Be(Objects(contents, list).Count());
    }

    [Test]
    public void FlightTemplateDoesNotCloneAnExtraPilot()
    {
        Objects(ReadResource("git", "shuttle"), "Creature List").Should().BeEmpty();
        Objects(ReadResource("gic", "shuttle"), "Creature List").Should().BeEmpty();
    }

    [Test]
    public void FlightTemplateIsExcludedFromPersistentLocationAreaCache()
    {
        const uint shuttleArea = 123;
        const uint landingArea = 456;
        var engine = new FakeNWScript();
        engine.SetReturnSequence("GetFirstArea", shuttleArea);
        engine.SetReturnSequence("GetNextArea", landingArea);
        engine.SetReturnHandler("GetIsObjectValid", arguments => (uint)arguments[0]! is shuttleArea or landingArea);
        engine.SetReturnHandler("GetResRef", arguments => (uint)arguments[0]! == shuttleArea ? "shuttle" : "shuttle_test_landing");
        var areas = new AreaService(new InMemoryDBService());

        using (NWScriptRuntime.Override(engine))
        {
            areas.CacheData();
            areas.GetAreas().Should().ContainKey("shuttle");

            areas.RemoveInstancesFromCache();
        }

        areas.GetAreas().Should().NotContainKey("shuttle");
        areas.GetAreas()["shuttle_test_landing"].Should().Be(landingArea);
    }

    private static IEnumerable<JObject> Objects(JObject resource, string list) =>
        resource[list]!["value"]!.Children<JObject>();

    private static T Value<T>(JObject resource, string field) => resource[field]!["value"]!.Value<T>()!;

    private static JObject ReadResource(string type, string resref) =>
        JObject.Parse(File.ReadAllText(Path.Combine(Root(), "Module", type, $"{resref}.{type}.json")));

    private static string Root()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "Module")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Module content was not found.");
    }
}
