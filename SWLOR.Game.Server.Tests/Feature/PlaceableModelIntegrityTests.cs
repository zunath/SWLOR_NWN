using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

public class PlaceableModelIntegrityTests
{
    private static readonly Regex PlaceableRowPattern = new(
        "^\\s*(?<row>\\d+)\\s+(?<label>\"[^\"]*\"|\\S+)\\s+\\S+\\s+(?<model>\\S+)",
        RegexOptions.Compiled);

    [Test]
    public void PlaceableBlueprints_UseValidAndConsistentModelRows()
    {
        var root = FindRepositoryRoot();
        var rows = LoadPlaceableRows(root);
        var rowsByModel = rows
            .Where(entry => entry.Value.ModelName != "****")
            .GroupBy(entry => entry.Value.ModelName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Select(entry => entry.Key).ToArray(), StringComparer.OrdinalIgnoreCase);
        var failures = new List<string>();

        foreach (var path in Directory.EnumerateFiles(Path.Combine(root.FullName, "Module", "utp"), "*.utp.json"))
        {
            using var blueprint = JsonDocument.Parse(File.ReadAllText(path));
            var appearance = GetInt(blueprint.RootElement, "Appearance");
            var resref = GetString(blueprint.RootElement, "TemplateResRef");
            var fileName = Path.GetFileName(path);

            if (!rows.TryGetValue(appearance, out var row) || row.ModelName == "****")
            {
                failures.Add($"{fileName}: appearance {appearance} has no model row.");
                continue;
            }

            if (rowsByModel.TryGetValue(resref, out var matchingRows) && matchingRows.Length == 1 && matchingRows[0] != appearance)
            {
                failures.Add($"{fileName}: appearance {appearance} resolves to {row.ModelName}, but model {resref} is row {matchingRows[0]}.");
            }
        }

        failures.Should().BeEmpty(FormatFailures(failures));
    }

    [Test]
    public void PlacedPlaceables_UseValidModelRows()
    {
        var root = FindRepositoryRoot();
        var rows = LoadPlaceableRows(root);
        var failures = new List<string>();

        foreach (var path in Directory.EnumerateFiles(Path.Combine(root.FullName, "Module", "git"), "*.git.json"))
        {
            using var area = JsonDocument.Parse(File.ReadAllText(path));
            var placeables = area.RootElement.GetProperty("Placeable List").GetProperty("value");
            var index = 0;
            foreach (var placeable in placeables.EnumerateArray())
            {
                var appearance = GetInt(placeable, "Appearance");
                if (!rows.TryGetValue(appearance, out var row) || row.ModelName == "****")
                {
                    failures.Add($"{Path.GetFileName(path)} placeable {index} ({GetString(placeable, "TemplateResRef")}): appearance {appearance} has no model row.");
                }

                index++;
            }
        }

        failures.Should().BeEmpty(FormatFailures(failures));
    }

    [Test]
    public void PlaceablePaletteEntries_HaveBlueprintFiles()
    {
        var root = FindRepositoryRoot();
        var palettePath = Path.Combine(root.FullName, "Module", "itp", "placeablepalcus.itp.json");
        using var palette = JsonDocument.Parse(File.ReadAllText(palettePath));
        var blueprintDirectory = Path.Combine(root.FullName, "Module", "utp");
        var failures = EnumeratePaletteResrefs(palette.RootElement)
            .Where(resref => !File.Exists(Path.Combine(blueprintDirectory, $"{resref}.utp.json")))
            .Select(resref => $"Palette resref {resref} has no UTP blueprint.")
            .ToList();

        failures.Should().BeEmpty(FormatFailures(failures));
    }

    private static Dictionary<int, PlaceableRow> LoadPlaceableRows(DirectoryInfo root)
    {
        var path = Path.Combine(root.FullName, "SWLOR_Haks", "sw_2da", "placeables.2da");
        var rows = new Dictionary<int, PlaceableRow>();

        foreach (var line in File.ReadLines(path))
        {
            var match = PlaceableRowPattern.Match(line);
            if (!match.Success)
            {
                continue;
            }

            var row = int.Parse(match.Groups["row"].Value);
            var label = match.Groups["label"].Value.Trim('"');
            rows[row] = new PlaceableRow(label, match.Groups["model"].Value);
        }

        return rows;
    }

    private static IEnumerable<string> EnumeratePaletteResrefs(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("RESREF", out var resref))
            {
                yield return resref.GetProperty("value").GetString() ?? string.Empty;
            }

            foreach (var property in element.EnumerateObject())
            {
                foreach (var nested in EnumeratePaletteResrefs(property.Value))
                {
                    yield return nested;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                foreach (var nested in EnumeratePaletteResrefs(item))
                {
                    yield return nested;
                }
            }
        }
    }

    private static int GetInt(JsonElement element, string propertyName) =>
        element.GetProperty(propertyName).GetProperty("value").GetInt32();

    private static string GetString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) &&
        property.TryGetProperty("value", out var value) &&
        value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;

    private static string FormatFailures(IReadOnlyCollection<string> failures) =>
        failures.Count == 0
            ? string.Empty
            : $"Found {failures.Count} placeable model integrity failure(s):{Environment.NewLine}{string.Join(Environment.NewLine, failures.Take(100))}";

    private static DirectoryInfo FindRepositoryRoot()
    {
        var current = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (current != null && !Directory.Exists(Path.Combine(current.FullName, "Module")))
        {
            current = current.Parent;
        }

        return current ?? throw new DirectoryNotFoundException("Could not locate the repository root.");
    }

    private sealed record PlaceableRow(string Label, string ModelName);
}
