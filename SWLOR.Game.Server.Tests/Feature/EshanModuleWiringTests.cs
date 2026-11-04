using System.Text.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.SpawnDefinition;

namespace SWLOR.Game.Server.Tests.Feature;

public class EshanModuleWiringTests
{
    [TestCase("pw_ar_sc_eshanto", "vc_access")]
    public void ShuttleAccess_HasPairedUsableExitsAndSeparateArrivals(string area, string prefix)
    {
        using var document = Read($"Module/git/{area}.git.json");
        var root = document.RootElement;
        for (var side = 0; side < 2; side++)
        {
            var destination = $"{prefix}_arrival_{side}";
            AssertTeleport(root, destination);
            AssertWaypoint(root, destination);
            var arrival = Value(root, "WaypointList").EnumerateArray().Single(item =>
                Value(item, "Tag").GetString() == destination);
            var exit = Value(root, "Placeable List").EnumerateArray().Single(item =>
                Value(item, "Tag").GetString() == $"{prefix}_exit_{side}");
            Assert.That(Value(arrival, "XPosition").GetDouble(), Is.EqualTo(Value(exit, "X").GetDouble()));
            Assert.That(Value(arrival, "YPosition").GetDouble(), Is.EqualTo(Value(exit, "Y").GetDouble() - 1.5).Within(0.001));
            Assert.That(Value(arrival, "ZPosition").GetDouble(), Is.EqualTo(Value(exit, "Z").GetDouble()));
        }
    }

    [TestCase("pw_ar_sc_eshancm", "pw_ar_sc_eshanto", "eshan_uppercity", "eshan_lowercity")]
    [TestCase("pw_sc_eskeshpeak", "pw_sc_eshspa", "ESHAN_INSIDE_SPA", "ESHAN_KESH_SPA_EXIT")]
    public void PublicTeleportEntrances_HaveUsableReturnRoutes(string outside, string inside, string entrance, string exit)
    {
        using var exterior = Read($"Module/git/{outside}.git.json");
        using var interior = Read($"Module/git/{inside}.git.json");
        AssertTeleport(exterior.RootElement, entrance);
        AssertWaypoint(interior.RootElement, entrance);
        AssertTeleport(interior.RootElement, exit);
        AssertWaypoint(exterior.RootElement, exit);
    }

    [Test]
    public void HighcrestStores_AreNotBlackMarketsInBlueprintsOrPlacedCopies()
    {
        foreach (var name in new[] { "hc_nema_shop", "hc_orel_shop", "hc_vesa_shop" })
        {
            using var store = Read($"Module/utm/{name}.utm.json");
            Assert.That(Value(store.RootElement, "BlackMarket").GetInt32(), Is.Zero);
        }
        using var area = Read("Module/git/pw_ar_sc_eshancm.git.json");
        var stores = Value(area.RootElement, "StoreList").EnumerateArray().ToArray();
        Assert.That(stores, Has.Length.EqualTo(3));
        foreach (var store in stores)
            Assert.That(Value(store, "BlackMarket").GetInt32(), Is.Zero);
    }

    [Test]
    public void Orbit_UsesRegisteredEshanSpawnTables()
    {
        using var area = Read("Module/git/eshanorbit.git.json");
        var locals = Value(area.RootElement, "VarTable").EnumerateArray()
            .Select(variable => Value(variable, "Value").ToString()).ToArray();
        foreach (var table in new[] { "SPACE_ESHAN_ORBIT", "SPACE_RESOURCES_ESHAN_ORBIT" })
            Assert.That(locals, Does.Contain(table));
        Assert.That(new SpaceSpawnDefinition().BuildSpawnTables()["SPACE_ESHAN_ORBIT"].Spawns, Is.Not.Empty);
        Assert.That(new SpaceResourceSpawnDefinition().BuildSpawnTables()["SPACE_RESOURCES_ESHAN_ORBIT"].Spawns, Is.Not.Empty);
    }

    [Test]
    public void EveryEshanArea_HasLocalStuckRecoveryAndMatchingEditorMetadata()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !Directory.Exists(Path.Combine(root.FullName, "Module")))
            root = root.Parent;
        var areas = Directory.GetFiles(Path.Combine(root!.FullName, "Module", "are"), "*.are.json")
            .Where(path =>
            {
                using var area = JsonDocument.Parse(File.ReadAllBytes(path));
                return Value(area.RootElement, "Name").TryGetProperty("0", out var name) &&
                       name.GetString()!.StartsWith("Eshan - ", StringComparison.Ordinal);
            }).ToArray();
        Assert.That(areas, Is.Not.Empty);
        foreach (var path in areas)
        {
            var area = Path.GetFileName(path).Replace(".are.json", "");
            using var git = Read($"Module/git/{area}.git.json");
            using var gic = Read($"Module/gic/{area}.gic.json");
            var waypoints = Value(git.RootElement, "WaypointList").EnumerateArray().ToArray();
            Assert.That(waypoints.Count(point => Value(point, "Tag").GetString() == "STUCK_WAYPOINT"),
                Is.EqualTo(1), area + " must recover locally rather than falling back to CZ-220");
            var editorWaypoints = Value(gic.RootElement, "WaypointList").EnumerateArray().ToArray();
            Assert.That(editorWaypoints.Length, Is.EqualTo(waypoints.Length), area);
            var stuckIndex = Array.FindIndex(waypoints,
                point => Value(point, "Tag").GetString() == "STUCK_WAYPOINT");
            var expectedComment = area == "esh_nc_redoubt"
                ? "Area recovery waypoint."
                : "Local /stuck recovery at the area entrance or medical terminal";
            Assert.That(Value(editorWaypoints[stuckIndex], "Comment").GetString(),
                Is.EqualTo(expectedComment), area);
        }
    }

    [Test]
    public void RaaloKes_WearsSmugglerArmorInBothBlueprintAndPlacedInstance()
    {
        using var blueprint = Read("Module/utc/esh_smug_raalo.utc.json");
        using var scraplands = Read("Module/git/sc_esscraplands.git.json");
        var raalo = Value(scraplands.RootElement, "Creature List").EnumerateArray()
            .Single(creature => Value(creature, "TemplateResRef").GetString() == "esh_smug_raalo");
        foreach (var creature in new[] { blueprint.RootElement, raalo })
        {
            var chest = Value(creature, "Equip_ItemList").EnumerateArray()
                .Single(item => item.GetProperty("__struct_id").GetInt32() == 2);
            Assert.That(Value(chest, "EquippedRes").GetString(), Is.EqualTo("pirate_armor"));
        }
        using var armor = Read("Module/uti/pirate_armor.uti.json");
        Assert.That(Value(armor.RootElement, "BaseItem").GetInt32(), Is.EqualTo(16));
    }

    private static void AssertTeleport(JsonElement area, string destination)
    {
        var matches = Value(area, "Placeable List").EnumerateArray().Where(item =>
            Value(item, "OnUsed").GetString() == "teleport" &&
            Value(item, "VarTable").EnumerateArray().Any(variable =>
                Value(variable, "Name").GetString() == "DESTINATION" &&
                Value(variable, "Value").GetString() == destination)).ToArray();
        Assert.That(matches, Has.Length.EqualTo(1));
        Assert.That(Value(matches[0], "Useable").GetInt32(), Is.EqualTo(1));
        Assert.That(Value(matches[0], "Static").GetInt32(), Is.Zero);
    }

    private static void AssertWaypoint(JsonElement area, string tag) =>
        Assert.That(Value(area, "WaypointList").EnumerateArray().Count(item => Value(item, "Tag").GetString() == tag), Is.EqualTo(1));

    private static JsonElement Value(JsonElement element, string name) => element.GetProperty(name).GetProperty("value");

    private static JsonDocument Read(string path)
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln")))
            root = root.Parent;
        return JsonDocument.Parse(File.ReadAllText(Path.Combine(root!.FullName, path)));
    }
}
