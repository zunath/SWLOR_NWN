using System.Text.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.SpawnDefinition;

namespace SWLOR.Game.Server.Tests.Feature;

public class EshanModuleWiringTests
{
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
