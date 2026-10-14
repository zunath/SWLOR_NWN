using System.Numerics;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

public class ViscaraMountainEntranceTests
{
    [Test]
    public void MountainAscentAndValley_KeepReciprocalTransitionTargets()
    {
        using var ascent = Read("git", "viscara_mountasc");
        using var valley = Read("git", "viscarawildwest");
        var outbound = List(ascent, "TriggerList").Single(t => Text(t, "Tag") == "V_MountAscent_to_MountValley");
        var inbound = List(valley, "TriggerList").Single(t => Text(t, "Tag") == "V_MountValley_to_MountAscent");

        Text(outbound, "LinkedTo").Should().Be("WP_V_MountValley_to_MountAscent");
        Text(inbound, "LinkedTo").Should().Be("WP_V_MountAscent_to_MountValley");
        Number(outbound, "LinkedToFlags").Should().Be(2);
        Number(inbound, "LinkedToFlags").Should().Be(2);
        List(valley, "WaypointList").Count(w => Text(w, "Tag") == Text(outbound, "LinkedTo")).Should().Be(1);
        List(ascent, "WaypointList").Count(w => Text(w, "Tag") == Text(inbound, "LinkedTo")).Should().Be(1);
    }

    [Test]
    public void ValleyArrivalAndRecovery_UseThePublicEntranceAwayFromTheMapEdge()
    {
        using var valley = Read("git", "viscarawildwest");
        using var area = Read("are", "viscarawildwest");
        var waypoints = List(valley, "WaypointList");
        var arrival = waypoints.Single(w => Text(w, "Tag") == "WP_V_MountValley_to_MountAscent");
        var recovery = waypoints.Should().ContainSingle(w => Text(w, "Tag") == "STUCK_WAYPOINT",
            "without a local recovery waypoint /stuck falls back to CZ220").Subject;

        Text(recovery, "TemplateResRef").Should().Be("wp_stuck");
        Vector3.Distance(Position(arrival), Position(recovery)).Should().BeLessThan(1,
            "recovery must return players to the verified entrance");
        Text(area.RootElement, "Tileset").Should().Be("wsf10");
        foreach (var point in new[] { arrival, recovery })
        {
            var position = Position(point);
            // The public forest entrance is west of the return trigger at X=313.
            // The previous arrival at (311.91,127.61) was against the map edge.
            position.X.Should().BeInRange(302, 308);
            position.Y.Should().BeInRange(120.5f, 122);
            position.Z.Should().BeApproximately(0, .1f);
            Number(point, "XOrientation").Should().BeLessThan(-.9f);
            var tile = List(area, "Tile_List")[(int)(position.Y / 10) * (int)Number(area.RootElement, "Width") + (int)(position.X / 10)];
            Number(tile, "Tile_ID").Should().Be(7);
            Number(tile, "Tile_Height").Should().Be(0);
            Number(tile, "Tile_Orientation").Should().Be(1);
        }
    }

    [Test]
    public void ValleyRecovery_HasAnAlignedToolsetComment()
    {
        using var valley = Read("git", "viscarawildwest");
        using var comments = Read("gic", "viscarawildwest");
        var waypoints = List(valley, "WaypointList");
        var recoveryIndex = Array.FindIndex(waypoints, w => Text(w, "Tag") == "STUCK_WAYPOINT");
        recoveryIndex.Should().BeGreaterThanOrEqualTo(0);
        var waypointComments = List(comments, "WaypointList");
        waypointComments.Should().HaveCount(waypoints.Length);
        Text(waypointComments[recoveryIndex], "Comment").Should().Contain("Mountain Ascent entrance");
    }

    private static Vector3 Position(JsonElement point) => new(
        Number(point, "XPosition"), Number(point, "YPosition"), Number(point, "ZPosition"));

    private static JsonElement[] List(JsonDocument document, string name) =>
        document.RootElement.GetProperty(name).GetProperty("value").EnumerateArray().ToArray();

    private static string Text(JsonElement element, string name) => element.GetProperty(name).GetProperty("value").GetString()!;
    private static float Number(JsonElement element, string name) => element.GetProperty(name).GetProperty("value").GetSingle();

    private static JsonDocument Read(string kind, string area)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "Module"))) directory = directory.Parent;
        if (directory == null) throw new DirectoryNotFoundException("Module directory not found.");
        return JsonDocument.Parse(File.ReadAllText(Path.Combine(directory.FullName, "Module", kind, $"{area}.{kind}.json")));
    }
}
