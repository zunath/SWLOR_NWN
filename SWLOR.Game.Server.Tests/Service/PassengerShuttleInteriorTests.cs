using System.Numerics;
using System.Reflection;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;

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

    private static string SelectInterior(IEnumerable<(string Tag, ObjectType Type)> objects) =>
        (string)typeof(Shuttle).GetMethod("SelectFlightInterior", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { objects })!;

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
    public void StatusConsoleStandsInTheSeatingCabinInsteadOfOnTheBoardingDivider()
    {
        var tag = typeof(Shuttle).GetField("ConsoleWaypointTag",
            BindingFlags.NonPublic | BindingFlags.Static)!.GetRawConstantValue()!.ToString();
        var waypoints = Objects(ReadResource("git", "shuttle"), "WaypointList").ToList();
        var console = waypoints.Single(point => Value<string>(point, "Tag") == tag);
        var entrance = waypoints.Single(point => Value<string>(point, "Tag") == "PROPERTY_ENTRANCE");

        // The boarding point sits on the boarding bay's center divider, which the console clipped into.
        var x = Value<float>(console, "XPosition");
        var y = Value<float>(console, "YPosition");
        x.Should().BeApproximately(45f, 0.01f);
        y.Should().BeApproximately(37f, 0.01f);
        Math.Abs(y - Value<float>(entrance, "YPosition")).Should().BeGreaterThan(10f);

        // The terminal model's screen is on its back, so facing north turns the screen
        // toward boarding passengers and the rear seats.
        Value<float>(console, "XOrientation").Should().BeApproximately(0f, 0.01f);
        Value<float>(console, "YOrientation").Should().BeApproximately(1f, 0.01f);
    }

    [Test]
    public void EveryPassengerBenchSeatHasAUsableSitPoint()
    {
        var placeables = Objects(ReadResource("git", "shuttle"), "Placeable List").ToList();
        var benches = placeables.Where(placeable => Value<int>(placeable, "Appearance") == DoubleBenchAppearance).ToList();
        var seats = placeables.Where(placeable => Value<string>(placeable, "Tag").StartsWith("shuttle_seat_")).ToList();

        benches.Should().HaveCount(4);
        seats.Should().HaveCount(benches.Count * 12, "each double bench has six seats on each side");
        seats.Select(seat => Value<string>(seat, "Tag")).Should().OnlyHaveUniqueItems();
        seats.Should().OnlyContain(seat =>
            Value<int>(seat, "Appearance") == InvisibleChairAppearance &&
            Value<int>(seat, "Static") == 0 &&
            Value<int>(seat, "Useable") == 1 &&
            Value<int>(seat, "Plot") == 1 &&
            Value<string>(seat, "OnUsed") == "zep_use_chair");

        foreach (var bench in benches)
        {
            var center = Position(bench);
            var benchBearing = Value<float>(bench, "Bearing");
            var length = new Vector2(MathF.Cos(benchBearing), MathF.Sin(benchBearing));
            var depth = new Vector2(-length.Y, length.X);
            var onBench = seats.Where(seat =>
            {
                var offset = Position(seat) - center;
                return MathF.Abs(Vector2.Dot(offset, length)) < 2f && MathF.Abs(Vector2.Dot(offset, depth)) < 0.5f;
            }).ToList();

            onBench.Should().HaveCount(12, "every seat on the bench at {0} needs its own sit point", center);
            foreach (var seat in onBench)
            {
                // The invisible chair's use node sits on its model -Y axis, so a sitter faces Bearing - 90 degrees.
                var bearing = Value<float>(seat, "Bearing") - MathF.PI / 2;
                var sitterFacing = new Vector2(MathF.Cos(bearing), MathF.Sin(bearing));
                var outward = Vector2.Normalize(Vector2.Dot(Position(seat) - center, depth) * depth);
                Vector2.Dot(sitterFacing, outward).Should().BeGreaterThan(0.99f,
                    "{0} must face away from the bench back", Value<string>(seat, "Tag"));
            }
        }

        var script = File.ReadAllText(Path.Combine(Root(), "Module", "nss", "zep_use_chair.nss"));
        script.Should().Contain("GetSittingCreature");
        script.Should().Contain("ActionSit(oChair)");
        File.Exists(Path.Combine(Root(), "Module", "ncs", "zep_use_chair.ncs")).Should().BeTrue();
    }

    private const int DoubleBenchAppearance = 20484;
    private const int InvisibleChairAppearance = 1604;

    private static Vector2 Position(JObject placeable) =>
        new(Value<float>(placeable, "X"), Value<float>(placeable, "Y"));

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
        var cache = (Dictionary<string, uint>)typeof(Area)
            .GetProperty("AreasByResref", BindingFlags.NonPublic | BindingFlags.Static)!
            .GetValue(null)!;
        var original = new Dictionary<string, uint>(cache);
        try
        {
            cache["shuttle"] = 123;
            cache["shuttle_test_landing"] = 456;

            Area.RemoveInstancesFromCache();

            cache.Should().NotContainKey("shuttle");
            cache["shuttle_test_landing"].Should().Be(456);
        }
        finally
        {
            cache.Clear();
            foreach (var entry in original)
                cache.Add(entry.Key, entry.Value);
        }
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
