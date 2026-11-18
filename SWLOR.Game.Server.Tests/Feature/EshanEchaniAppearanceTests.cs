using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// Eshan NPCs described as Echani use the human model, so only their palette colors make them read as Echani.
/// </summary>
public class EshanEchaniAppearanceTests
{
    private const int HumanAppearance = 6;

    // Pale skin presets: 16 is the established Eshan resident preset; 164 and 166 are the pale blue-white tones.
    private static readonly int[] EchaniSkinColors = { 16, 164, 166 };

    // White and silver hair presets: 18 is the established Eshan resident preset.
    private static readonly int[] EchaniHairColors = { 16, 18, 62 };

    // Cloth and leather palette row 61 is the fully reflective black swatch: a garment in it renders as chrome.
    private const int MirrorClothColor = 61;

    private static readonly string[] ClothAndLeatherColorFields =
        { "Cloth1Color", "Cloth2Color", "Leather1Color", "Leather2Color" };

    [Test]
    public void EchaniBlueprints_UseEchaniSkinAndHairAndMatteCloth()
    {
        var root = FindRepositoryRoot();
        var blueprints = EchaniBlueprints(root);
        blueprints.Should().Contain(new[] { "esh_war_kestra", "esh_war_arven", "esh_war_nara", "esh_war_seris" });

        foreach (var resref in blueprints)
        {
            using var blueprint = Read(Path.Combine(root.FullName, "Module", "utc", $"{resref}.utc.json"));
            AssertEchaniColors(blueprint.RootElement, resref);
            foreach (var equipped in List(blueprint.RootElement, "Equip_ItemList"))
            {
                var itemResref = Value(equipped, "EquippedRes").GetString()!;
                using var item = Read(Path.Combine(root.FullName, "Module", "uti", $"{itemResref}.uti.json"));
                if (item.RootElement.TryGetProperty("Cloth1Color", out _))
                    AssertMatteCloth(item.RootElement, $"{resref}'s {itemResref}");
            }
        }
    }

    [Test]
    public void PlacedEchaniResidents_UseEchaniSkinAndHairAndMatteCloth()
    {
        var root = FindRepositoryRoot();
        var blueprints = EchaniBlueprints(root).ToHashSet();
        var placed = new List<string>();

        foreach (var path in Directory.EnumerateFiles(Path.Combine(root.FullName, "Module", "git"), "*.git.json")
                     .Where(path => Path.GetFileName(path).Contains("esh") || Path.GetFileName(path).Contains("_es_")))
        {
            using var area = Read(path);
            foreach (var npc in List(area.RootElement, "Creature List"))
            {
                var resref = Value(npc, "TemplateResRef").GetString()!;
                if (!blueprints.Contains(resref)) continue;

                var label = $"{resref} in {Path.GetFileName(path)}";
                placed.Add(resref);
                AssertEchaniColors(npc, label);
                foreach (var equipped in List(npc, "Equip_ItemList"))
                {
                    if (equipped.TryGetProperty("Cloth1Color", out _))
                        AssertMatteCloth(equipped, label);
                }
            }
        }

        placed.Should().Contain(new[] { "esh_war_kestra", "esh_war_arven", "esh_war_nara", "esh_war_seris" });
    }

    private static void AssertEchaniColors(JsonElement creature, string label)
    {
        Value(creature, "Color_Skin").GetInt32().Should().BeOneOf(EchaniSkinColors,
            $"{label} is described as Echani, who are pale-skinned");
        Value(creature, "Color_Hair").GetInt32().Should().BeOneOf(EchaniHairColors,
            $"{label} is described as Echani, who are white- or silver-haired");
    }

    private static void AssertMatteCloth(JsonElement item, string label)
    {
        foreach (var field in ClothAndLeatherColorFields)
        {
            Value(item, field).GetInt32().Should().NotBe(MirrorClothColor,
                $"{label}'s {field} would render the garment as polished metal");
        }
    }

    private static List<string> EchaniBlueprints(DirectoryInfo root)
    {
        var resrefs = new List<string>();
        foreach (var path in Directory.EnumerateFiles(Path.Combine(root.FullName, "Module", "utc"), "*.utc.json"))
        {
            var text = File.ReadAllText(path);
            if (!text.Contains("Echani")) continue;

            using var blueprint = JsonDocument.Parse(text);
            if (Value(blueprint.RootElement, "Appearance_Type").GetInt32() != HumanAppearance) continue;
            var description = Value(blueprint.RootElement, "Description");
            if (description.ValueKind != JsonValueKind.Object ||
                !description.EnumerateObject().Any(entry => entry.Value.GetString()!.Contains("Echani")))
                continue;

            resrefs.Add(Value(blueprint.RootElement, "TemplateResRef").GetString()!);
        }

        return resrefs;
    }

    private static JsonDocument Read(string path) => JsonDocument.Parse(File.ReadAllText(path));

    private static JsonElement Value(JsonElement element, string field) =>
        element.GetProperty(field).GetProperty("value");

    private static IEnumerable<JsonElement> List(JsonElement element, string field) =>
        element.TryGetProperty(field, out var list) ? list.GetProperty("value").EnumerateArray() : [];

    private static DirectoryInfo FindRepositoryRoot()
    {
        var current = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (current != null && !Directory.Exists(Path.Combine(current.FullName, "Module")))
            current = current.Parent;
        return current ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
