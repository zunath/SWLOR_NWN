using System.Text;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Tests.Perks;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// A placed crafting station keeps its blueprint's scripts and skill. Area instances copy a blueprint's fields when they are
/// placed, so a station placed before its blueprint changed keeps the old values: the Mandalorian refineries kept the retired
/// container scripts and had no use script, so clicking them did nothing.
/// </summary>
public class CraftingStationPlacementTests
{
    private static readonly string[] StationBlueprints =
    {
        "smithery_bench", "engineering_term", "fabrication_term", "cookpot", "espionage_bench", "cft_forge", "research_term",
        "lightsaber_bench"
    };

    private static readonly string[] Scripts = { "OnUsed", "OnHeartbeat", "OnInvDisturbed", "OnOpen", "OnClosed" };

    private const string SkillVariable = "CRAFTING_SKILL_TYPE_ID";

    [Test]
    public void EveryPlacedCraftingStation_UsesItsBlueprintScriptsAndSkill()
    {
        var root = Path.Combine(PassivePerkIconTests.FindRepositoryRoot().FullName, "Module");
        var blueprints = StationBlueprints.ToDictionary(resref => resref, resref => Load(Path.Combine(root, "utp", $"{resref}.utp.json")));
        var failures = new List<string>();
        var stations = 0;

        foreach (var path in Directory.GetFiles(Path.Combine(root, "git"), "*.git.json").OrderBy(path => path))
        {
            var text = File.ReadAllText(path, Encoding.Latin1);
            if (!StationBlueprints.Any(resref => text.Contains($"\"{resref}\"", StringComparison.Ordinal)))
                continue;

            using var git = JsonDocument.Parse(text);
            if (!git.RootElement.TryGetProperty("Placeable List", out var list))
                continue;

            var index = 0;
            foreach (var placeable in list.GetProperty("value").EnumerateArray())
            {
                var resref = Value(placeable, "TemplateResRef");
                if (blueprints.TryGetValue(resref, out var blueprint))
                {
                    stations++;
                    var label = $"{Path.GetFileName(path)} Placeable List[{index}] {resref}";
                    foreach (var script in Scripts)
                    {
                        if (Value(placeable, script) != Value(blueprint.RootElement, script))
                            failures.Add($"{label}: {script} is '{Value(placeable, script)}', the blueprint's is '{Value(blueprint.RootElement, script)}'");
                    }

                    if (Value(placeable, "HasInventory") != Value(blueprint.RootElement, "HasInventory"))
                        failures.Add($"{label}: HasInventory differs from the blueprint");

                    if (Value(placeable, "Useable") != "1")
                        failures.Add($"{label}: is not usable");

                    var skill = Variable(blueprint.RootElement, SkillVariable);
                    if (skill != Variable(placeable, SkillVariable))
                        failures.Add($"{label}: {SkillVariable} is '{Variable(placeable, SkillVariable)}', the blueprint's is '{skill}'");
                }

                index++;
            }
        }

        stations.Should().BeGreaterThan(0);
        failures.Should().BeEmpty(string.Join(Environment.NewLine, failures));
    }

    private static JsonDocument Load(string path) => JsonDocument.Parse(File.ReadAllText(path, Encoding.Latin1));

    private static string Value(JsonElement element, string field) =>
        element.TryGetProperty(field, out var property) && property.TryGetProperty("value", out var value)
            ? value.ToString()
            : string.Empty;

    private static string Variable(JsonElement element, string name)
    {
        if (!element.TryGetProperty("VarTable", out var table))
            return string.Empty;

        foreach (var variable in table.GetProperty("value").EnumerateArray())
        {
            if (Value(variable, "Name") == name)
                return Value(variable, "Value");
        }

        return string.Empty;
    }
}
