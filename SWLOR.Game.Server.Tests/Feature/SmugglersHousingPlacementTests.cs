using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

public class SmugglersHousingPlacementTests
{
    [TestCase("smug_housings")]
    [TestCase("pw_ar_narcatwalk")]
    [TestCase("pw_ar_nars_canhd")]
    [TestCase("pw_ar_nsficlub")]
    public void UpdatedAreas_KeepOneRecoveryWaypointAndAlignedToolsetMetadata(string area)
    {
        using var git = Read("git", area);
        using var gic = Read("gic", area);
        var waypoints = git.RootElement.GetProperty("WaypointList").GetProperty("value").EnumerateArray().ToArray();
        var recovery = waypoints.Single(waypoint => Text(waypoint, "Tag") == "STUCK_WAYPOINT");
        Text(recovery, "TemplateResRef").Should().Be("wp_stuck");
        gic.RootElement.GetProperty("WaypointList").GetProperty("value").GetArrayLength()
            .Should().Be(waypoints.Length, "toolset comments must remain aligned with waypoint instances");
    }

    [Test]
    public void HousingRecovery_ReturnsToThePublicArrivalPoint()
    {
        using var git = Read("git", "smug_housings");
        using var gic = Read("gic", "smug_housings");
        var waypoints = git.RootElement.GetProperty("WaypointList").GetProperty("value").EnumerateArray().ToArray();
        var arrival = waypoints.Single(waypoint => Text(waypoint, "Tag") == "SMUGGLERS_HOUSING");
        var recoveryIndex = Array.FindIndex(waypoints, waypoint => Text(waypoint, "Tag") == "STUCK_WAYPOINT");
        foreach (var field in new[] { "XPosition", "YPosition", "ZPosition", "XOrientation", "YOrientation" })
            waypoints[recoveryIndex].GetProperty(field).GetProperty("value").GetDouble()
                .Should().BeApproximately(arrival.GetProperty(field).GetProperty("value").GetDouble(), .0001);
        Text(gic.RootElement.GetProperty("WaypointList").GetProperty("value")[recoveryIndex], "Comment")
            .Should().Contain("STUCK_WAYPOINT").And.Contain("public arrival point");
    }

    [TestCase("smug_housings")]
    [TestCase("hut_fdy_intake")]
    [TestCase("hut_fdy_assembly")]
    [TestCase("hut_fdy_core")]
    public void HousingAndFoundryAreas_RemainRegistered(string area)
    {
        using var module = Read("ifo", "module");
        module.RootElement.GetProperty("Mod_Area_list").GetProperty("value").EnumerateArray()
            .Should().ContainSingle(entry => Text(entry, "Area_Name") == area);
        File.Exists(Path.Combine(FindRoot(), "Module", "are", area + ".are.json")).Should().BeTrue();
    }

    private static JsonDocument Read(string kind, string area) => JsonDocument.Parse(File.ReadAllText(
        Path.Combine(FindRoot(), "Module", kind, area + "." + kind + ".json")));

    private static string Text(JsonElement element, string field) => element.GetProperty(field).GetProperty("value").GetString()!;

    private static string FindRoot()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln"))) root = root.Parent;
        return root?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
