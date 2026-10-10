using System.Reflection;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Tests.Service;

[NonParallelizable]
public sealed class PassengerShuttleInteriorTests
{
    [Test]
    public void FlightTemplateHasABoardingPointAndUsablePilotChair()
    {
        var resref = typeof(Shuttle).GetField("ShuttleInteriorResref",
            BindingFlags.NonPublic | BindingFlags.Static)!.GetRawConstantValue()!.ToString();
        var area = ReadResource("are", resref!);
        var contents = ReadResource("git", resref!);

        area["Name"]!["value"]!["0"]!.Value<string>()
            .Should().Be("Starship Shuttle - Interior");
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
    public void PassengerSeatsAreUsableAndUseTheOccupiedChairGuard()
    {
        var contents = ReadResource("git", "shuttle");
        var seats = Objects(contents, "Placeable List")
            .Where(placeable => Value<string>(placeable, "Tag").StartsWith("shuttle_seat_"))
            .ToArray();

        seats.Should().HaveCount(12);
        seats.Select(seat => Value<string>(seat, "Tag")).Should().OnlyHaveUniqueItems();
        seats.Should().OnlyContain(seat =>
            Value<int>(seat, "Static") == 0 &&
            Value<int>(seat, "Useable") == 1 &&
            Value<int>(seat, "Plot") == 1 &&
            Value<string>(seat, "OnUsed") == "zep_use_chair");

        var script = File.ReadAllText(Path.Combine(Root(), "Module", "nss", "zep_use_chair.nss"));
        script.Should().Contain("GetSittingCreature");
        script.Should().Contain("ActionSit(oChair)");
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
