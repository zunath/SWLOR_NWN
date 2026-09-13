using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

public class VelesMilitiaAnnexPlacementTests
{
    private const string Annex = "pw_sc_velesannx";
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

    private static JsonElement Load(string type, string name)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "Module", type, $"{name}.{type}.json")));
        return document.RootElement.Clone();
    }
    private static JsonElement Value(JsonElement o, string key) => o.GetProperty(key).GetProperty("value");
    private static string Text(JsonElement o, string key) => Value(o, key).GetString()!;
    private static double Number(JsonElement o, string key) => Value(o, key).GetDouble();
    private static JsonElement[] List(JsonElement o, string key) => Value(o, key).EnumerateArray().ToArray();
    private static JsonElement Local(JsonElement o, string key) => Value(List(o, "VarTable").Single(v => Text(v, "Name") == key), "Value");
    private static JsonElement Tagged(JsonElement o, string list, string tag) => List(o, list).Single(v => Text(v, "Tag") == tag);
    private static double Distance(JsonElement a, JsonElement b) => Math.Sqrt(
        Math.Pow(Number(a, "X") - Number(b, "X"), 2) + Math.Pow(Number(a, "Y") - Number(b, "Y"), 2));

    [Test]
    public void Annex_IsRegisteredCategorizedAndSizedLikeExistingDungeons()
    {
        var area = Load("are", Annex);
        Value(area, "Name").GetProperty("0").GetString().Should().Be("Viscara - Veles Militia Annex");
        Text(area, "Tileset").Should().Be("tjsb0");
        Number(area, "Width").Should().Be(Number(Load("are", "manda_facility"), "Width"));
        Number(area, "Height").Should().Be(Number(Load("are", "manda_facility"), "Height"));
        List(area, "Tile_List").Should().HaveCount(256);
        List(Load("ifo", "module"), "Mod_Area_list").Count(a => Text(a, "Area_Name") == Annex).Should().Be(1);
        using var categories = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "toolset", "categories.json")));
        IEnumerable<JsonElement> Folders(JsonElement folder)
        {
            yield return folder;
            if (folder.TryGetProperty("children", out var children))
                foreach (var child in children.EnumerateArray().SelectMany(Folders)) yield return child;
        }
        var folders = categories.RootElement.GetProperty("sections").GetProperty("are").GetProperty("folders");
        folders.EnumerateArray().SelectMany(Folders).Single(folder =>
                folder.TryGetProperty("members", out var members) && members.EnumerateArray().Any(m => m.GetString() == Annex))
            .GetProperty("name").GetString().Should().Be("Veles");
    }

    [Test]
    public void Annex_UsesThreeSeparateBloodDressedWardenAltarsAndInvisibleActivators()
    {
        var git = Load("git", Annex);
        var props = List(git, "Placeable List");
        var altars = props.Where(p => Number(p, "Appearance") == 2174).ToArray();
        altars.Should().HaveCount(3);
        props.Count(p => Text(p, "OnUsed") == "quest_enc").Should().Be(3);
        foreach (var (shortName, quest) in new[] { ("invinc", "invincible"), ("vitrupt", "vital_rupture"), ("sysshut", "systemic_shutdown") })
        {
            var tag = $"{shortName}_wd_call";
            var activator = Tagged(git, "Placeable List", tag);
            Value(activator, "LocName").GetProperty("0").GetString().Should().Be("???");
            Number(activator, "Appearance").Should().Be(479);
            Number(activator, "Static").Should().Be(0);
            Number(activator, "Useable").Should().Be(1);
            Number(activator, "Plot").Should().Be(1);
            Text(activator, "OnUsed").Should().Be("quest_enc");
            Local(activator, "QUEST_ID").GetString().Should().Be($"{quest}_breach");
            Local(activator, "QUEST_STATE").GetInt32().Should().Be(1);
            Local(activator, "VISIBILITY_OBJECT_ID").GetString().Should().Be(tag);
            Local(activator, "VISIBILITY_HIDDEN_DEFAULT").GetInt32().Should().Be(1);
            Local(activator, "QUEST_ENCOUNTER_ID").GetString().Should().Be($"{quest}_breach_warden");
            Local(activator, "QUEST_ENCOUNTER_RESREF").GetString().Should().Be($"cp_{shortName}_wd");
            Local(activator, "QUEST_ENCOUNTER_COOLDOWN_MINUTES").GetInt32().Should().Be(60);
            var spawn = Tagged(git, "WaypointList", $"CAPSTONE_{shortName.ToUpperInvariant()}_WD_SPAWN");
            Local(activator, "QUEST_ENCOUNTER_WAYPOINT").GetString().Should().Be(Text(spawn, "Tag"));
            var altar = altars.Single(a => Distance(a, activator) < 3);
            Distance(altar, activator).Should().BeInRange(1.5, 2.5);
            var bearing = Number(altar, "Bearing");
            var forwardDistance = (Number(activator, "X") - Number(altar, "X")) * Math.Sin(bearing)
                                  - (Number(activator, "Y") - Number(altar, "Y")) * Math.Cos(bearing);
            forwardDistance.Should().BeGreaterThan(1.5, "the activator belongs on the approach side of its altar");
            var blood = props.Where(p => new[] { 2711d, 2714d }.Contains(Number(p, "Appearance")) && Distance(p, altar) < 3).ToArray();
            blood.Count(p => Math.Abs(Number(p, "Z") - Number(altar, "Z") - .74) < .01).Should().Be(2);
            blood.Count(p => Math.Abs(Number(p, "Z") - Number(altar, "Z")) < .01).Should().Be(2);
        }
    }

    [Test]
    public void Travel_UsesKeyGatedHubAccessAndOpposingDoorwaysWithClearArrivals()
    {
        var hub = Load("git", "veles_sheriff");
        var dungeon = Load("git", Annex);
        var boss = Load("git", "pw_sc_velescmd");
        var gate = List(hub, "Placeable List").Single(p => Text(p, "OnUsed") == "teleport" && Local(p, "DESTINATION").GetString() == "VELES_ANNEX_ENTRY");
        Local(gate, "KEY_ITEM_ID").GetInt32().Should().Be(107);
        Local(gate, "TELEPORT_PARTY_MEMBERS").GetInt32().Should().Be(1);
        Local(gate, "MISSING_KEY_ITEM_MESSAGE").GetString().Should().Contain("key");
        var entry = Tagged(dungeon, "WaypointList", "VELES_ANNEX_ENTRY");
        var exit = Tagged(dungeon, "Door List", "velesannx_exit");
        Text(exit, "LinkedTo").Should().Be("VELES_ANNEX_HUB_RETURN");
        Number(exit, "Y").Should().BeLessThan(Number(entry, "YPosition"));
        Number(entry, "YOrientation").Should().Be(1);
        Number(Tagged(hub, "WaypointList", Text(exit, "LinkedTo")), "YOrientation").Should().Be(-1);
        var command = Tagged(dungeon, "Door List", "velesannx_command");
        Text(command, "LinkedTo").Should().Be("pw_sc_velescmd_entry");
        var arenaEntry = Tagged(boss, "WaypointList", Text(command, "LinkedTo"));
        var arenaExit = Tagged(boss, "Placeable List", "pw_sc_velescmd_exit");
        Number(arenaExit, "Y").Should().BeLessThan(Number(arenaEntry, "YPosition"));
        Text(arenaExit, "OnUsed").Should().Be("teleport");
        Local(arenaExit, "DESTINATION").GetString().Should().Be("VELES_ANNEX_COMMAND_RETURN");
        var returning = Tagged(dungeon, "WaypointList", "VELES_ANNEX_COMMAND_RETURN");
        Number(returning, "YOrientation").Should().Be(-1);
        Number(command, "Y").Should().BeGreaterThan(Number(returning, "YPosition"));
        foreach (var door in List(dungeon, "Door List"))
        {
            Number(door, "LinkedToFlags").Should().Be(2);
            Number(door, "Locked").Should().Be(0);
            Number(door, "Plot").Should().Be(1);
        }
    }

    [Test]
    public void AmbientSpawns_UseExistingTableAndKeepArrivalsAndAltarActivatorsClear()
    {
        var git = Load("git", Annex);
        Local(git, "IS_DUNGEON").GetInt32().Should().Be(1);
        Local(git, "MINI_MAP_DISABLED").GetInt32().Should().Be(1);
        Local(git, "PLANET_TYPE_ID").GetInt32().Should().Be(1);
        Local(git, "MAP_KEY_ITEM_ID").GetInt32().Should().Be(40);
        var spawns = List(git, "WaypointList").Where(w => Text(w, "Tag") == "CAPSTONE_VELES_MILITIA_ANNEX").ToArray();
        spawns.Should().HaveCount(20);
        var entry = Tagged(git, "WaypointList", "VELES_ANNEX_ENTRY");
        foreach (var spawn in spawns)
        {
            var distance = Math.Sqrt(Math.Pow(Number(spawn, "XPosition") - Number(entry, "XPosition"), 2) + Math.Pow(Number(spawn, "YPosition") - Number(entry, "YPosition"), 2));
            distance.Should().BeGreaterThan(15);
        }
        var comments = Load("gic", Annex);
        foreach (var property in comments.EnumerateObject().Where(p => p.Value.ValueKind == JsonValueKind.Object && p.Value.GetProperty("type").GetString() == "list"))
            List(comments, property.Name).Length.Should().Be(List(git, property.Name).Length);
    }
}
