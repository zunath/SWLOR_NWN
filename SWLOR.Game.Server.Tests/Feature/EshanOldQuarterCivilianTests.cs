using System.Numerics;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

public class EshanOldQuarterCivilianTests
{
    private const string Area = "pw_sc_es_oldgate";

    [TestCase("es_oq_ilya", 71f, 76f, 21f, 27f)]
    [TestCase("es_oq_cerin", 71f, 75f, 44f, 52f)]
    [TestCase("es_oq_toren", 71f, 75f, 89f, 96f)]
    [TestCase("es_oq_sera", 67f, 73f, 106f, 114f)]
    public void CivilianRoutes_StayInTheirPublicAvenueSections(
        string resref, float minX, float maxX, float minY, float maxY)
    {
        var root = FindRepositoryRoot();
        using var area = Read(root, $"Module/git/{Area}.git.json");
        var npc = List(area.RootElement, "Creature List")
            .Single(creature => Value(creature, "TemplateResRef").GetString() == resref);
        var variables = List(npc, "VarTable").ToDictionary(
            variable => Value(variable, "Name").GetString()!,
            variable => Value(variable, "Value"));

        variables["AI_FLAGS"].GetInt32().Should().Be(0,
            "the global fifteen-metre random-walk leash permits leaving the audited street section");
        variables["ESHAN_PATROL_PAUSE_SECONDS"].GetInt32().Should().Be(6);
        Value(npc, "ScriptHeartbeat").GetString().Should().Be("crea_hb_aft");
        Value(npc, "ScriptSpawn").GetString().Should().Be("x2_def_spawn");

        var route = variables["ESHAN_PATROL_ROUTE"].GetString()!.Split(',');
        route.Should().HaveCount(4).And.OnlyHaveUniqueItems();
        var positions = route.Select(tag => List(area.RootElement, "WaypointList")
                .Single(waypoint => Value(waypoint, "Tag").GetString() == tag))
            .Select(Position).ToArray();
        Position(npc).Should().Be(positions[0], "the first target must not require crossing scenery");
        for (var i = 0; i < positions.Length; i++)
        {
            positions[i].X.Should().BeInRange(minX, maxX);
            positions[i].Y.Should().BeInRange(minY, maxY);
            positions[i].Z.Should().Be(0);
            var next = positions[(i + 1) % positions.Length];
            Vector3.Distance(positions[i], next).Should().BeInRange(1f, 8f);
            (positions[i].X == next.X || positions[i].Y == next.Y).Should().BeTrue(
                "each consecutive segment, including the closing segment, stays on its audited avenue loop");
        }
    }

    [Test]
    public void Civilians_HaveDistinctCustomOutfitsAndMatchingPlacedEquipment()
    {
        var root = FindRepositoryRoot();
        using var area = Read(root, $"Module/git/{Area}.git.json");
        var outfits = new List<string>();
        var appearances = new List<string>();
        foreach (var npc in List(area.RootElement, "Creature List")
                     .Where(npc => Value(npc, "TemplateResRef").GetString()!.StartsWith("es_oq_")))
        {
            var resref = Value(npc, "TemplateResRef").GetString()!;
            using var blueprint = Read(root, $"Module/utc/{resref}.utc.json");
            Value(npc, "Conversation").GetString().Should().Be(resref);
            Value(npc, "ScriptDialogue").GetString().Should().Be("dialog_start");
            Value(npc, "Color_Hair").GetInt32().Should().Be(18, "this is the existing silver-white Echani hair preset");

            var equipmentRef = List(blueprint.RootElement, "Equip_ItemList").Single();
            var outfitRef = Value(equipmentRef, "EquippedRes").GetString()!;
            outfits.Add(outfitRef);
            using var outfit = Read(root, $"Module/uti/{outfitRef}.uti.json");
            Value(outfit.RootElement, "LocalizedName").GetProperty("0").GetString()
                .Should().StartWith("[NPC]");
            Value(outfit.RootElement, "Plot").GetInt32().Should().Be(1);
            List(outfit.RootElement, "VarTable").Should().Contain(variable =>
                Value(variable, "Name").GetString() == "NO_ECONOMY" &&
                Value(variable, "Value").GetInt32() == 1);
            var placed = List(npc, "Equip_ItemList").Single();
            Value(placed, "Dropable").GetInt32().Should().Be(0);
            foreach (var field in outfit.RootElement.EnumerateObject().Where(field => field.Name != "__data_type"))
            {
                JsonElement.DeepEquals(field.Value, placed.GetProperty(field.Name)).Should().BeTrue(
                    $"{resref}'s placed clothing must match the current blueprint field {field.Name}");
            }

            appearances.Add(string.Join(',', new[] { "ArmorPart_Torso", "Cloth1Color", "Cloth2Color" }
                .Select(field => Value(outfit.RootElement, field).GetInt32())));
        }

        outfits.Should().HaveCount(4).And.OnlyHaveUniqueItems();
        appearances.Should().OnlyHaveUniqueItems("each resident has a distinct garment shape or palette");
    }

    private static JsonDocument Read(DirectoryInfo root, string path) =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(root.FullName, path)));

    private static JsonElement Value(JsonElement element, string field) =>
        element.GetProperty(field).GetProperty("value");

    private static JsonElement.ArrayEnumerator List(JsonElement element, string field) =>
        Value(element, field).EnumerateArray();

    private static Vector3 Position(JsonElement element) => new(
        Value(element, "XPosition").GetSingle(),
        Value(element, "YPosition").GetSingle(),
        Value(element, "ZPosition").GetSingle());

    private static DirectoryInfo FindRepositoryRoot()
    {
        var current = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (current != null && !Directory.Exists(Path.Combine(current.FullName, "Module")))
            current = current.Parent;
        return current ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
