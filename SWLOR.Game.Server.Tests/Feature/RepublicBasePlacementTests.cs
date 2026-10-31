using System.Text.Json;
using System.Text;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

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

    [Test]
    public void PublicEntrances_KeepEveryExistingRoomAndTheNewWingReachable()
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
                List(area.Value, "Placeable List").Select(obj => Local(obj, "DESTINATION"))
                    .Concat(List(area.Value, "Door List").Concat(List(area.Value, "TriggerList"))
                        .Where(obj => Number(obj, "LinkedToFlags") > 0)
                        .Select(obj => Text(obj, "LinkedTo")))
                    .SelectMany(tag => destinations[tag]).ToArray());
            var reached = new HashSet<string>();
            var pending = new Stack<string>(new[] { "v_repubbase_ext" });
            while (pending.TryPop(out var area))
            {
                if (!reached.Add(area)) continue;
                foreach (var destination in connections[area]) pending.Push(destination);
            }

            reached.Should().BeEquivalentTo(names,
                "replacing public entrances must not strand existing rooms, NPCs or services");
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
    public void NewHangar_ReturnsToTheBaseThroughOneGroundLevelPortal()
    {
        using var hangar = Read("visrephanger");
        using var interior = Read("visredrepbase");
        var portal = List(hangar, "Placeable List")
            .Where(obj => Text(obj, "OnUsed") == "teleport").Should().ContainSingle().Subject;
        Local(portal, "DESTINATION").Should().Be("RepBase_Inter2");
        List(interior, "WaypointList").Should()
            .ContainSingle(obj => Text(obj, "Tag") == Local(portal, "DESTINATION"));
        Number(portal, "Z").Should().BeApproximately(0, .05f,
            "the return portal must be reachable from the hangar floor");
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

    private static string ModulePath(string kind, string resref)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "Module"))) directory = directory.Parent;
        if (directory == null) throw new DirectoryNotFoundException("Module directory not found.");
        return Path.Combine(directory.FullName, "Module", kind, $"{resref}.{kind}.json");
    }
}
