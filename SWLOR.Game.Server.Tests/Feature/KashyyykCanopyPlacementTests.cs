using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

public class KashyyykCanopyPlacementTests
{
    [Test]
    public void RecoveryWaypoint_UsesTheOpenPlatform_AndHasAnAlignedToolsetComment()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !Directory.Exists(Path.Combine(root.FullName, "Module"))) root = root.Parent;
        root.Should().NotBeNull();
        using var git = Read("git");
        using var gic = Read("gic");
        using var are = Read("are");
        var waypoints = git.RootElement.GetProperty("WaypointList").GetProperty("value");
        var recovery = waypoints.EnumerateArray().Single(w =>
            w.GetProperty("Tag").GetProperty("value").GetString() == "STUCK_WAYPOINT");
        recovery.GetProperty("TemplateResRef").GetProperty("value").GetString().Should().Be("wp_stuck");

        // thf02_a13_01 has a walkable wooden floor at Z=2.2. Keep the recovery
        // point inside the clear central part of this platform, away from its edges.
        var x = recovery.GetProperty("XPosition").GetProperty("value").GetSingle();
        var y = recovery.GetProperty("YPosition").GetProperty("value").GetSingle();
        x.Should().BeInRange(143f, 147f);
        y.Should().BeInRange(85.5f, 89.5f);
        recovery.GetProperty("ZPosition").GetProperty("value").GetSingle().Should().BeApproximately(2.2f, 0.01f);
        var width = are.RootElement.GetProperty("Width").GetProperty("value").GetInt32();
        var tile = are.RootElement.GetProperty("Tile_List").GetProperty("value")[(int)(y / 10) * width + (int)(x / 10)];
        are.RootElement.GetProperty("Tileset").GetProperty("value").GetString().Should().Be("thf02");
        tile.GetProperty("Tile_ID").GetProperty("value").GetInt32().Should().Be(14);
        tile.GetProperty("Tile_Height").GetProperty("value").GetInt32().Should().Be(0);
        tile.GetProperty("Tile_Orientation").GetProperty("value").GetInt32().Should().Be(1);
        gic.RootElement.GetProperty("WaypointList").GetProperty("value").GetArrayLength()
            .Should().Be(waypoints.GetArrayLength());

        JsonDocument Read(string kind) => JsonDocument.Parse(File.ReadAllText(
            Path.Combine(root!.FullName, "Module", kind, $"pw_ar_kashyk.{kind}.json")));
    }
}
