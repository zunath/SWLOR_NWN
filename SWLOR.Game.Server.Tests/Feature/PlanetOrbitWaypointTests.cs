using System.Reflection;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// Launching a ship from a planet's starport places it at the planet's orbit waypoint,
/// so that waypoint must sit beside the planet's space dock, not at a starlane arrival point.
/// </summary>
public class PlanetOrbitWaypointTests
{
    // Every planet's orbit waypoint sits 1.5 to 3 m from its dock.
    private const double MaximumDockDistance = 5.0;

    // Orbit areas are small; skipping large ground areas keeps the scan fast.
    // An orbit area above this size fails loudly below because its dock is not found.
    private const long MaximumOrbitAreaBytes = 2 * 1024 * 1024;

    private static string Root
    {
        get
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "Module")))
                directory = directory.Parent;
            return directory?.FullName ?? throw new DirectoryNotFoundException("Module directory not found.");
        }
    }

    private static JToken Value(JToken o, string key) => o[key]!["value"]!;
    private static string Text(JToken o, string key) => Value(o, key).Value<string>()!;
    private static double Number(JToken o, string key) => Value(o, key).Value<double>();
    private static string? Local(JToken o, string name) => o["VarTable"]?["value"]?
        .FirstOrDefault(v => Text(v, "Name") == name)?["Value"]?["value"]?.Value<string>();

    private static IEnumerable<TestCaseData> Planets() => Enum.GetValues<PlanetType>()
        .Where(type => type != PlanetType.Invalid)
        .Select(type => new TestCaseData(typeof(PlanetType).GetField(type.ToString())!
                .GetCustomAttribute<PlanetAttribute>()!.SpaceOrbitWaypointTag)
            .SetName($"OrbitWaypoint_SitsBesideItsDock({type})"));

    private static readonly Lazy<List<JObject>> DockAreas = new(() => Directory
        .EnumerateFiles(Path.Combine(Root, "Module", "git"), "*.git.json")
        .Where(path => new FileInfo(path).Length <= MaximumOrbitAreaBytes)
        .Select(File.ReadAllText)
        .Where(text => text.Contains("\"spc_dock\""))
        .Select(JObject.Parse)
        .ToList());

    [TestCaseSource(nameof(Planets))]
    public void OrbitWaypoint_SitsBesideItsDock(string orbitTag)
    {
        var areas = DockAreas.Value
            .Where(area => Value(area, "WaypointList").Any(w => Text(w, "Tag") == orbitTag))
            .ToList();
        areas.Should().ContainSingle($"exactly one orbit area should hold the {orbitTag} waypoint");

        var area = areas[0];
        var waypoint = Value(area, "WaypointList").Single(w => Text(w, "Tag") == orbitTag);
        var dock = Value(area, "Placeable List")
            .Single(p => Local(p, "STARPORT_TELEPORT_WAYPOINT") == orbitTag);

        var distance = Math.Sqrt(
            Math.Pow(Number(waypoint, "XPosition") - Number(dock, "X"), 2) +
            Math.Pow(Number(waypoint, "YPosition") - Number(dock, "Y"), 2));
        distance.Should().BeLessThanOrEqualTo(MaximumDockDistance,
            $"{orbitTag} is where launched ships arrive and must sit beside its dock");
    }
}
