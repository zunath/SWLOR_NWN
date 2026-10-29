using System.Text;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.LootTableDefinition;
using SWLOR.Game.Server.Service.AchievementService;

namespace SWLOR.Game.Server.Tests.Feature;

public sealed class EshanAuthoringReviewTests
{
    [Test]
    public void GorakVesh_UsesOneStandardWeightedGemRoll()
    {
        var tables = new EshanLootTableDefinition().BuildLootTables();
        var standard = new DathomirLootTableDefinition().BuildLootTables()["DATHOMIR_CHIRODACTYL_GEMS"];
        tables["ESHAN_GORAK_VESH_GEMS"].Select(item => (item.Resref, item.Weight, item.MaxQuantity, item.IsRare))
            .Should().BeEquivalentTo(standard.Select(item => (item.Resref, item.Weight, item.MaxQuantity, item.IsRare)));
        tables.Should().NotContainKey("ESHAN_GORAK_VESH_SHARD");
        tables.Should().NotContainKey("ESHAN_GORAK_VESH_EMERALD");
        var locals = Read("Module/utc/esh_gorakvesh.utc.json")["VarTable"]!["value"]!
            .Where(local => Value(local, "Name").StartsWith("LOOT_TABLE_", StringComparison.Ordinal))
            .Select(local => Value(local, "Value")).ToArray();
        locals.Should().ContainSingle(value => value == "ESHAN_GORAK_VESH_GEMS,100,1");
        locals.Should().NotContain(value => value.Contains("ESHAN_GORAK_VESH_SHARD") || value.Contains("ESHAN_GORAK_VESH_EMERALD"));
    }

    private static string Root()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "AGENTS.md"))) dir = dir.Parent;
        return dir!.FullName;
    }

    private static JObject Read(string path) => JObject.Parse(File.ReadAllText(Path.Combine(Root(), path), Encoding.Latin1));
    private static string Value(JToken obj, string name) => obj[name]?["value"]?.ToString() ?? "";

    [Test]
    public void EshanExplorationAchievements_DoNotSharePersistedIdsWithOtherAchievements()
    {
        var all = Enum.GetNames<AchievementType>();
        var eshan = all.Where(name => name.StartsWith("ExploreEshan", StringComparison.Ordinal)).ToArray();
        eshan.Should().HaveCount(21);
        var ids = eshan.Select(name => (int)Enum.Parse<AchievementType>(name)).ToArray();
        ids.Should().OnlyHaveUniqueItems();
        var others = all.Except(eshan).Select(name => (int)Enum.Parse<AchievementType>(name));
        ids.Intersect(others).Should().BeEmpty();
    }

    [TestCase("utc", "creaturepalcus")]
    [TestCase("uti", "itempalcus")]
    [TestCase("utp", "placeablepalcus")]
    [TestCase("utm", "storepalcus")]
    public void EshanBlueprints_AreDiscoverableInTheirToolsetPalettes(string extension, string palette)
    {
        var expected = Directory.EnumerateFiles(Path.Combine(Root(), "Module", extension), $"*.{extension}.json")
            .Select(Path.GetFileName).Select(name => name![..^($".{extension}.json".Length)])
            .Where(name => name.StartsWith("esh_", StringComparison.Ordinal) ||
                           name.StartsWith("es_", StringComparison.Ordinal) || name.StartsWith("hc_", StringComparison.Ordinal))
            .ToArray();
        expected.Should().NotBeEmpty();
        expected.Should().OnlyContain(name => name.Length <= 16, "NWN archives omit resources with longer resrefs");
        var entries = Read($"Module/itp/{palette}.itp.json").Descendants().OfType<JProperty>()
            .Where(property => property.Name == "RESREF").Select(property => property.Value["value"]!.Value<string>()).ToHashSet();
        expected.Where(name => !entries.Contains(name)).Should().BeEmpty("builders must be able to find every Eshan blueprint");
    }

    [Test]
    public void HighcrestResidents_PreserveDistinctPlacementAndBlueprintEquipment()
    {
        const string area = "pw_ar_sc_eshancm";
        var git = Read($"Module/git/{area}.git.json");
        var are = Read($"Module/are/{area}.are.json");
        var gic = Read($"Module/gic/{area}.gic.json");
        var creatures = (JArray)git["Creature List"]!["value"]!;
        creatures.Should().HaveCount(15);
        creatures.Select(npc => Value(npc, "Tag")).Should().OnlyHaveUniqueItems();
        foreach (var list in new[] { "Creature List", "StoreList" })
            ((JArray)git[list]!["value"]!).Count.Should().Be(((JArray)gic[list]!["value"]!).Count);

        var positions = new List<(double X, double Y, double Z)>();
        var greetings = new HashSet<string>();
        var guardGenders = new HashSet<int>();
        var wanderers = new HashSet<string>();
        foreach (var npc in creatures)
        {
            var resref = Value(npc, "TemplateResRef");
            var blueprint = Read($"Module/utc/{resref}.utc.json");
            foreach (var field in blueprint.Properties().Where(field =>
                         field.Name is not ("__data_type" or "Comment" or "PaletteID" or "Equip_ItemList")))
                JToken.DeepEquals(npc[field.Name], field.Value).Should().BeTrue($"{resref} must preserve its blueprint's {field.Name}");
            var flags = npc["VarTable"]!["value"]!.Single(local => Value(local, "Name") == "AI_FLAGS");
            if (Value(flags, "Value") != "0")
            {
                Value(flags, "Value").Should().Be("3");
                Value(npc, "ScriptSpawn").Should().Be("x2_def_spawn");
                wanderers.Add(resref);
            }
            var x = npc["XPosition"]!["value"]!.Value<double>();
            var y = npc["YPosition"]!["value"]!.Value<double>();
            var z = npc["ZPosition"]!["value"]!.Value<double>();
            x.Should().BeInRange(0.001, are["Width"]!["value"]!.Value<int>() * 10 - 0.001);
            y.Should().BeInRange(0.001, are["Height"]!["value"]!.Value<int>() * 10 - 0.001);
            positions.Should().OnlyContain(other =>
                Math.Pow(x - other.X, 2) + Math.Pow(y - other.Y, 2) + Math.Pow(z - other.Z, 2) > 1);
            positions.Add((x, y, z));
            var ox = npc["XOrientation"]!["value"]!.Value<double>();
            var oy = npc["YOrientation"]!["value"]!.Value<double>();
            (ox * ox + oy * oy).Should().BeApproximately(1, 0.00001);

            var graph = Read($"SWLOR.Game.Server/ConversationData/{resref}.conversation.json");
            Value(npc, "Conversation").Should().Be(resref);
            var entry = graph["EntryPoints"]![0]!["TargetNodeId"]!.Value<string>();
            greetings.Add(graph["Nodes"]![entry!]!["Text"]![0]!["Text"]!.Value<string>()!).Should().BeTrue();
            var equipment = (JArray)npc["Equip_ItemList"]!["value"]!;
            var templates = (JArray)blueprint["Equip_ItemList"]!["value"]!;
            equipment.Count.Should().Be(templates.Count);
            foreach (var item in equipment)
            {
                var template = templates.Single(t => t["__struct_id"]!.Value<int>() == item["__struct_id"]!.Value<int>());
                var itemRef = Value(template, "EquippedRes");
                var source = Read($"Module/uti/{itemRef}.uti.json");
                Value(item, "Dropable").Should().Be("0");
                foreach (var field in source.Properties().Where(field => field.Name != "__data_type"))
                    JToken.DeepEquals(item[field.Name], field.Value).Should().BeTrue($"{resref}'s {itemRef} must match its current source");
                if (itemRef == "hc_guard_helm") guardGenders.Add(npc["Gender"]!["value"]!.Value<int>());
            }
        }
        guardGenders.Should().BeEquivalentTo(new[] { 0, 1 });
        wanderers.Should().BeEquivalentTo(new[] { "hc_pell", "hc_mira" });
    }

    [TestCase("hc_nema_shop", 4, "distilled_water,butter_1,sugar,bread")]
    [TestCase("hc_vesa_shop", 0, "tran_tunic,tran_cap,tran_boots,tran_gloves")]
    [TestCase("hc_orel_shop", 1, "b_knife,b_longsword,b_spear")]
    public void HighcrestShops_KeepTheirAuthoredStockInTheCorrectPanels(string resref, int panel, string expected)
    {
        var store = Read($"Module/utm/{resref}.utm.json");
        var inventory = store["StoreList"]!["value"]!.Single(pane => pane["__struct_id"]!.Value<int>() == panel);
        inventory["ItemList"]!["value"]!.Select(item => Value(item, "InventoryRes"))
            .Should().BeEquivalentTo(expected.Split(','));
        if (resref == "hc_vesa_shop")
        {
            var misc = store["StoreList"]!["value"]!.Single(pane => pane["__struct_id"]!.Value<int>() == 4);
            misc["ItemList"]!["value"]!.Select(item => Value(item, "InventoryRes"))
                .Should().Equal("bag_b");
        }
    }
}
