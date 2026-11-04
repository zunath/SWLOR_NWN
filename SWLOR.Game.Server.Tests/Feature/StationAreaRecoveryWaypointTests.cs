using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// Areas that hold crafting stations must keep exactly one /stuck recovery waypoint built from the wp_stuck template,
/// standing on a position the area already uses. A second one would split the "nearest" choice.
/// </summary>
public class StationAreaRecoveryWaypointTests
{
    private static readonly string[] Areas =
    {
        "dath_landingpad",
        "korribanlandingp",
        "pw_ar_nscrafting",
        "r_prax_interiors",
        "veles_shops",
    };

    [TestCaseSource(nameof(Areas))]
    public void Area_HasOneRecoveryWaypointFromTheTemplate(string area)
    {
        var waypoints = Waypoints(area);
        var stuck = waypoints.Where(w => Text(w, "Tag") == "STUCK_WAYPOINT").ToArray();

        stuck.Should().ContainSingle($"{area} needs exactly one recovery point for stuck players");
        Text(stuck[0], "TemplateResRef").Should().Be("wp_stuck");
    }

    [TestCaseSource(nameof(Areas))]
    public void RecoveryWaypoint_HasAFinitePosition(string area)
    {
        var stuck = Waypoints(area).Single(w => Text(w, "Tag") == "STUCK_WAYPOINT");

        foreach (var axis in new[] { "XPosition", "YPosition", "ZPosition" })
            double.IsFinite(Number(stuck, axis)).Should().BeTrue($"{area} {axis} must be a number");
    }

    private static JsonElement[] Waypoints(string area)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "Module")))
            directory = directory.Parent;
        var root = directory?.FullName ?? throw new DirectoryNotFoundException("Module directory not found.");
        var path = Path.Combine(root, "Module", "git", area + ".git.json");

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.GetProperty("WaypointList").GetProperty("value").EnumerateArray()
            .Select(w => w.Clone())
            .ToArray();
    }

    private static string Text(JsonElement o, string key) =>
        o.TryGetProperty(key, out var value) ? value.GetProperty("value").ToString() : string.Empty;

    private static double Number(JsonElement o, string key) => o.GetProperty(key).GetProperty("value").GetDouble();
}
