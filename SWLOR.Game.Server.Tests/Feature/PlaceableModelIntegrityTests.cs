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
        var blueprintCount = 0;

        foreach (var path in Directory.EnumerateFiles(Path.Combine(root.FullName, "Module", "utp"), "*.utp.json"))
        {
            blueprintCount++;
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

        rows.Should().NotBeEmpty("the placeables.2da scan must inspect at least one row");
        blueprintCount.Should().BeGreaterThan(0, "the blueprint scan must inspect at least one UTP");
        TestContext.Out.WriteLine($"Scanned {rows.Count} placeable appearances and {blueprintCount} UTP blueprints.");
        failures.Should().BeEmpty(FormatFailures(failures));
    }

    [Test]
    public void PlacedPlaceables_UseValidModelRows()
    {
        var root = FindRepositoryRoot();
        var rows = LoadPlaceableRows(root);
        var failures = new List<string>();
        var placedCount = 0;

        foreach (var path in Directory.EnumerateFiles(Path.Combine(root.FullName, "Module", "git"), "*.git.json"))
        {
            using var area = JsonDocument.Parse(File.ReadAllText(path));
            var placeables = area.RootElement.GetProperty("Placeable List").GetProperty("value");
            var index = 0;
            foreach (var placeable in placeables.EnumerateArray())
            {
                placedCount++;
                var appearance = GetInt(placeable, "Appearance");
                if (!rows.TryGetValue(appearance, out var row) || row.ModelName == "****")
                {
                    failures.Add($"{Path.GetFileName(path)} placeable {index} ({GetString(placeable, "TemplateResRef")}): appearance {appearance} has no model row.");
                }

                index++;
            }
        }

        rows.Should().NotBeEmpty("the placeables.2da scan must inspect at least one row");
        placedCount.Should().BeGreaterThan(0, "the placed-placeable scan must inspect at least one instance");
        TestContext.Out.WriteLine($"Scanned {rows.Count} placeable appearances and {placedCount} placed instances.");
        failures.Should().BeEmpty(FormatFailures(failures));
    }

    [Test]
    public void PlaceablePaletteEntries_HaveBlueprintFiles()
    {
        var root = FindRepositoryRoot();
        var palettePath = Path.Combine(root.FullName, "Module", "itp", "placeablepalcus.itp.json");
        using var palette = JsonDocument.Parse(File.ReadAllText(palettePath));
        var blueprintDirectory = Path.Combine(root.FullName, "Module", "utp");
        var paletteResrefs = EnumeratePaletteResrefs(palette.RootElement).ToList();
        var failures = paletteResrefs
            .Where(resref => !File.Exists(Path.Combine(blueprintDirectory, $"{resref}.utp.json")))
            .Select(resref => $"Palette resref {resref} has no UTP blueprint.")
            .ToList();

        paletteResrefs.Should().NotBeEmpty("the palette scan must inspect at least one entry");
        TestContext.Out.WriteLine($"Scanned {paletteResrefs.Count} placeable palette entries.");
        failures.Should().BeEmpty(FormatFailures(failures));
    }

    [Test]
    public void GateblockReferences_HaveBlueprintFiles()
    {
        var root = FindRepositoryRoot();
        var moduleDirectory = Path.Combine(root.FullName, "Module");
        var blueprintDirectory = Path.Combine(moduleDirectory, "utp");
        var failures = new List<string>();
        var scannedFileCount = 0;
        var gateblockReferenceCount = 0;

        foreach (var (directory, pattern) in new[]
                 {
                     (Path.Combine(moduleDirectory, "utp"), "*.utp.json"),
                     (Path.Combine(moduleDirectory, "git"), "*.git.json")
                 })
        {
            foreach (var path in Directory.EnumerateFiles(directory, pattern))
            {
                scannedFileCount++;
                var json = File.ReadAllText(path);
                if (!json.Contains("\"CEP_L_GATEBLOCK\"", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                using var document = JsonDocument.Parse(json);
                foreach (var resref in EnumerateLocalStringValues(document.RootElement, "CEP_L_GATEBLOCK"))
                {
                    gateblockReferenceCount++;
                    if (!File.Exists(Path.Combine(blueprintDirectory, $"{resref}.utp.json")))
                    {
                        failures.Add($"{Path.GetFileName(path)} references missing gateblock blueprint {resref}.");
                    }
                }
            }
        }

        scannedFileCount.Should().BeGreaterThan(0, "the gateblock scan must inspect at least one module file");
        TestContext.Out.WriteLine($"Scanned {scannedFileCount} module files and {gateblockReferenceCount} CEP_L_GATEBLOCK references.");
        failures.Should().BeEmpty(FormatFailures(failures));
    }

    private static Dictionary<int, PlaceableRow> LoadPlaceableRows(DirectoryInfo root)
    {
        var path = Path.Combine(root.FullName, "SWLOR_Haks", "sw_2da", "placeables.2da");
        var rows = new Dictionary<int, PlaceableRow>();
        var physicalRowIndex = 0;

        foreach (var line in File.ReadLines(path))
        {
            var match = PlaceableRowPattern.Match(line);
            if (!match.Success)
            {
                continue;
            }

            var label = match.Groups["label"].Value.Trim('"');
            // NWN ignores the human-readable row label and assigns IDs by physical row order.
            rows[physicalRowIndex] = new PlaceableRow(label, match.Groups["model"].Value);
            physicalRowIndex++;
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

    private static IEnumerable<string> EnumerateLocalStringValues(JsonElement element, string variableName)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("Name", out var name) &&
                GetWrappedString(name).Equals(variableName, StringComparison.OrdinalIgnoreCase) &&
                element.TryGetProperty("Value", out var value))
            {
                var localValue = GetWrappedString(value);
                if (!string.IsNullOrWhiteSpace(localValue))
                {
                    yield return localValue;
                }
            }

            foreach (var property in element.EnumerateObject())
            {
                foreach (var nested in EnumerateLocalStringValues(property.Value, variableName))
                {
                    yield return nested;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                foreach (var nested in EnumerateLocalStringValues(item, variableName))
                {
                    yield return nested;
                }
            }
        }
    }

    private static string GetWrappedString(JsonElement element) =>
        element.ValueKind == JsonValueKind.Object &&
        element.TryGetProperty("value", out var value) &&
        value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;

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
