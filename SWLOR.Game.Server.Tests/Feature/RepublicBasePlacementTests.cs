using System.Globalization;
using System.Text.Json;
using System.Text;
using System.Text.RegularExpressions;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.NWN.Formats.TwoDA;

namespace SWLOR.Game.Server.Tests.Feature;

public class RepublicBasePlacementTests
{
    [Test]
    public void NewStores_UseCurrentBlueprintStockAndItemData()
    {
        var area = ReadSource("git", "visredrepbase");
        var ignored = new HashSet<string>
        {
            "__data_type", "Comment", "PaletteID", "__struct_id", "Infinite", "Repos_PosX", "Repos_Posy",
            "XOrientation", "XPosition", "YOrientation", "YPosition", "ZPosition",
        };
        foreach (var store in area["StoreList"]!["value"]!.OfType<JObject>())
        {
            var source = ReadSource("utm", store["ResRef"]!["value"]!.Value<string>()!);
            var actualPanes = store["StoreList"]!["value"]!.OfType<JObject>().ToArray();
            var sourcePanes = source["StoreList"]!["value"]!.OfType<JObject>().ToArray();
            actualPanes.Should().HaveCount(sourcePanes.Length);
            for (var pane = 0; pane < sourcePanes.Length; pane++)
            {
                var actualItems = actualPanes[pane]["ItemList"]?["value"]?.OfType<JObject>().ToArray() ?? [];
                var sourceItems = sourcePanes[pane]["ItemList"]?["value"]?.OfType<JObject>().ToArray() ?? [];
                actualItems.Select(item => item["TemplateResRef"]!["value"]!.Value<string>()).Should()
                    .Equal(sourceItems.Select(item => item["InventoryRes"]!["value"]!.Value<string>()),
                        "new shops must use their source inventory instead of retired embedded variants");
                for (var index = 0; index < sourceItems.Length; index++)
                {
                    var resref = sourceItems[index]["InventoryRes"]!["value"]!.Value<string>()!;
                    var blueprint = ReadSource("uti", resref);
                    foreach (var field in blueprint.Properties().Where(field => !ignored.Contains(field.Name)))
                        JToken.DeepEquals(actualItems[index][field.Name], field.Value).Should().BeTrue(
                            $"{resref}.{field.Name} must match its current item definition");
                    var infinite = sourceItems[index]["Infinite"] ?? JObject.FromObject(new { type = "byte", value = 1 });
                    JToken.DeepEquals(actualItems[index]["Infinite"], infinite).Should().BeTrue();
                }
            }
        }
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Entrances_PreserveExistingRoomsAndKeylessHangarAccess(bool hasKey)
    {
        var names = new[]
        {
            "v_repubbase_ext", "v_repubbase_1", "v_repubbase_2", "v_repubbase_cd",
            "v_repubbase_hang", "v_repubbase_off", "v_repubbase_jnrm",
            "visredrepbase", "visrephanger",
        };
        var areas = names.ToDictionary(name => name, Read);
        try
        {
            var destinations = areas.SelectMany(area =>
                    List(area.Value, "WaypointList").Concat(List(area.Value, "Door List"))
                        .Select(obj => (Tag: Text(obj, "Tag"), Area: area.Key)))
                .Where(target => target.Tag.Length > 0)
                .ToLookup(target => target.Tag, target => target.Area);
            var connections = areas.ToDictionary(area => area.Key, area =>
                List(area.Value, "Placeable List").Where(obj => hasKey || Local(obj, "KEY_ITEM_ID") is "" or "0")
                    .Select(obj => Local(obj, "DESTINATION"))
                    .Concat(List(area.Value, "Door List").Concat(List(area.Value, "TriggerList"))
                        .Where(obj => Number(obj, "LinkedToFlags") > 0)
                        .Where(obj => hasKey ||
                            (!obj.TryGetProperty("Locked", out var locked) || locked.GetProperty("value").GetInt32() == 0) &&
                            (!obj.TryGetProperty("KeyRequired", out var required) || required.GetProperty("value").GetInt32() == 0))
                        .Select(obj => Text(obj, "LinkedTo")))
                    .SelectMany(tag => destinations[tag]).ToArray());
            var reached = new HashSet<string>();
            var pending = new Stack<string>(new[] { "v_repubbase_ext" });
            while (pending.TryPop(out var area))
            {
                if (!reached.Add(area)) continue;
                foreach (var destination in connections[area]) pending.Push(destination);
            }

            if (hasKey)
                reached.Should().BeEquivalentTo(names,
                    "replacing entrances must not strand existing rooms, NPCs or services");
            else
                reached.Should().Contain(new[] { "v_repubbase_hang", "visrephanger", "visredrepbase" },
                    "the old hangar's Contract Board and the new wing must remain reachable without a base key");
        }
        finally
        {
            foreach (var area in areas.Values) area.Dispose();
        }
    }

    [TestCase("visredrepbase", "v_repubbase_1", "RepBase_Enter1")]
    [TestCase("visrephanger", "v_repubbase_hang", "WP_pubbie_inter")]
    public void NewAreas_PreserveAreaMetadataAndProvideLocalRecovery(string name, string previous, string arrivalTag)
    {
        using var area = Read(name);
        using var original = Read(previous);
        foreach (var variable in List(original, "VarTable"))
        {
            var key = Text(variable, "Name");
            Local(area.RootElement, key).Should().Be(Local(original.RootElement, key),
                $"the replacement must preserve {key}");
        }

        var arrival = List(area, "WaypointList").Single(obj => Text(obj, "Tag") == arrivalTag);
        var recovery = List(area, "WaypointList").Should()
            .ContainSingle(obj => Text(obj, "Tag") == "STUCK_WAYPOINT").Subject;
        Text(recovery, "TemplateResRef").Should().Be("wp_stuck");
        foreach (var coordinate in new[] { "XPosition", "YPosition", "ZPosition" })
            Number(recovery, coordinate).Should().BeApproximately(Number(arrival, coordinate), .01f);
    }

    [Test]
    public void ExistingRooms_KeepTheirExteriorReturnRoutesAndHangarRecovery()
    {
        using var exterior = Read("v_repubbase_ext");
        using var entrance = Read("v_repubbase_1");
        using var hangar = Read("v_repubbase_hang");
        var exteriorTags = List(exterior, "WaypointList").Select(obj => Text(obj, "Tag")).ToArray();
        var exit = List(entrance, "Placeable List")
            .Single(obj => Local(obj, "DESTINATION") == "WP_V_RepBaseExt_to_RepBaseInt");
        exteriorTags.Should().Contain(Local(exit, "DESTINATION"));
        var hangarExit = List(hangar, "TriggerList")
            .Single(obj => Text(obj, "Tag") == "V_RepHanger_to_RepBaseExt");
        exteriorTags.Should().Contain(Text(hangarExit, "LinkedTo"));
        List(hangar, "WaypointList").Should().ContainSingle(obj => Text(obj, "Tag") == "STUCK_WAYPOINT");
    }

    [Test]
    public void FrontEntrance_LeadsToTheHeadquartersAndItsExitReturnsThere()
    {
        using var exterior = Read("v_repubbase_ext");
        using var headquarters = Read("visredrepbase");
        var entrance = List(exterior, "Placeable List").Single(obj => Text(obj, "Tag") == "RepBaseExtEntrance");
        List(headquarters, "WaypointList").Should().ContainSingle(obj =>
            Text(obj, "Tag") == Local(entrance, "DESTINATION"),
            "the base's front door must open into the Headquarters, not the original entrance hall");

        var exit = List(headquarters, "Placeable List").Single(obj =>
            Text(obj, "OnUsed") == "teleport" && Local(obj, "DESTINATION") == "RepBase_Exit");
        var arrival = List(exterior, "WaypointList").Single(obj => Text(obj, "Tag") == Local(exit, "DESTINATION"));
        var distance = MathF.Sqrt(MathF.Pow(Number(arrival, "XPosition") - Number(entrance, "X"), 2) +
                                  MathF.Pow(Number(arrival, "YPosition") - Number(entrance, "Y"), 2));
        distance.Should().BeLessThan(3f, "leaving the Headquarters must return players to its front door");
    }

    // The engine ignores a placeable walkmesh lying more than 0.1 m below the floor, so a sunk wall piece
    // with a flat walkmesh at its base stops blocking movement.
    [TestCase("visredrepbase")]
    [TestCase("visrephanger")]
    public void ModularWalls_KeepTheirWalkmeshWithinTheEngineCollisionTolerance(string name)
    {
        var root = RepositoryRoot();
        var appearances = TwoDAReader.Read(Path.Combine(root, "SWLOR_Haks", "sw_2da", "placeables.2da"));
        var meshes = Directory.EnumerateFiles(Path.Combine(root, "SWLOR_Haks"), "*.pwk", SearchOption.AllDirectories)
            .ToDictionary(Path.GetFileNameWithoutExtension, StringComparer.OrdinalIgnoreCase);
        using var area = Read(name);
        var checkedWalls = 0;
        foreach (var placeable in List(area, "Placeable List"))
        {
            var row = (int)Number(placeable, "Appearance");
            if (appearances.GetValue(row, "Label")?.StartsWith("[SWLOR] Wall, Modular") != true) continue;
            var z = Number(placeable, "Z");
            // Pieces sunk further are deliberately shortened and need a separate blocker.
            if (z < -.5f) continue;
            var walkmeshTop = ReadWalkmeshHeights(meshes[appearances.GetValue(row, "ModelName")!]).Max();
            (z + walkmeshTop).Should().BeGreaterThanOrEqualTo(-.09f,
                $"the wall at ({Number(placeable, "X")}, {Number(placeable, "Y")}) must keep its collision");
            checkedWalls++;
        }

        checkedWalls.Should().BePositive();
    }

    [Test]
    public void NewHangar_ReturnsToTheBaseThroughOneGroundLevelPortal()
    {
        using var hangar = Read("visrephanger");
        using var interior = Read("visredrepbase");
        var portal = List(hangar, "Placeable List")
            .Where(obj => Text(obj, "OnUsed") == "teleport" && Local(obj, "DESTINATION") == "RepBase_Inter2")
            .Should().ContainSingle().Subject;
        Local(portal, "DESTINATION").Should().Be("RepBase_Inter2");
        List(interior, "WaypointList").Should()
            .ContainSingle(obj => Text(obj, "Tag") == Local(portal, "DESTINATION"));
        Number(portal, "Z").Should().BeApproximately(0, .05f,
            "the return portal must be reachable from the hangar floor");
    }

    [Test]
    public void NewHangar_ProvidesAKeylessConnectionToTheOriginalHangar()
    {
        using var hangar = Read("visrephanger");
        using var original = Read("v_repubbase_hang");
        var portal = List(hangar, "Placeable List")
            .Single(obj => Text(obj, "Tag") == "RepBase_OriginalHangar");
        Text(portal, "OnUsed").Should().Be("teleport");
        Local(portal, "DESTINATION").Should().Be("WP_V_RepHanger_to_RepBaseExt");
        Local(portal, "KEY_ITEM_ID").Should().Be("0");
        Local(portal, "TELEPORT_PARTY_MEMBERS").Should().Be("0");
        Number(portal, "Z").Should().BeApproximately(0f, .01f);
        List(original, "WaypointList").Should().ContainSingle(obj =>
            Text(obj, "Tag") == Local(portal, "DESTINATION"));
        ReadSource("gic", "visrephanger")["Placeable List"]!["value"]!.Count().Should()
            .Be(List(hangar, "Placeable List").Length);
    }

    private static IEnumerable<float> ReadWalkmeshHeights(string file)
    {
        foreach (Match node in Regex.Matches(File.ReadAllText(file), @"(?is)node\s+trimesh\s+\S+\s+(.*?)endnode"))
        {
            var lines = node.Groups[1].Value.Split('\n')
                .Select(line => line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
                .Where(line => line.Length > 0).ToArray();
            var verts = Array.FindIndex(lines, line => line[0] == "verts");
            if (verts < 0) continue;
            var offset = lines.FirstOrDefault(line => line[0] == "position") is { } position
                ? float.Parse(position[3], CultureInfo.InvariantCulture)
                : 0f;
            foreach (var vertex in lines.Skip(verts + 1).Take(int.Parse(lines[verts][1])))
                yield return offset + float.Parse(vertex[2], CultureInfo.InvariantCulture);
        }
    }

    private static string Local(JsonElement element, string name) =>
        List(element, "VarTable").Where(variable => Text(variable, "Name") == name)
            .Select(variable => variable.GetProperty("Value").GetProperty("value").ToString())
            .SingleOrDefault() ?? string.Empty;

    private static JsonElement[] List(JsonDocument document, string name) => List(document.RootElement, name);
    private static JsonElement[] List(JsonElement element, string name) =>
        element.TryGetProperty(name, out var field)
            ? field.GetProperty("value").EnumerateArray().ToArray()
            : Array.Empty<JsonElement>();
    private static string Text(JsonElement element, string name) =>
        element.TryGetProperty(name, out var field) ? field.GetProperty("value").GetString() ?? string.Empty : string.Empty;
    private static float Number(JsonElement element, string name) => element.GetProperty(name).GetProperty("value").GetSingle();

    private static JObject ReadSource(string kind, string resref) =>
        JObject.Parse(Encoding.Latin1.GetString(File.ReadAllBytes(ModulePath(kind, resref))));

    private static JsonDocument Read(string area) => JsonDocument.Parse(File.ReadAllText(ModulePath("git", area)));

    private static string ModulePath(string kind, string resref) =>
        Path.Combine(RepositoryRoot(), "Module", kind, $"{resref}.{kind}.json");

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "Module"))) directory = directory.Parent;
        if (directory == null) throw new DirectoryNotFoundException("Module directory not found.");
        return directory.FullName;
    }
}
