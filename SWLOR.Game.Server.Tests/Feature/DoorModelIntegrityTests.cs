using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// A door whose generic type points past the end of genericdoors.2da has no model, and the
/// client hangs on the loading screen when entering its area.
/// </summary>
public class DoorModelIntegrityTests
{
    // doortypes.2da row 0 ("Generic") means the door's model comes from GenericType_New.
    private const int GenericDoorAppearance = 0;

    [Test]
    public void DoorBlueprints_UseValidModelRows()
    {
        var root = FindRepositoryRoot();
        var genericDoorModels = LoadModelColumn(root, "genericdoors.2da", "ModelName");
        var doorTypeModels = LoadModelColumn(root, "doortypes.2da", "Model");
        var failures = new List<string>();
        var blueprintCount = 0;

        foreach (var path in Directory.EnumerateFiles(Path.Combine(root.FullName, "Module", "utd"), "*.utd.json"))
        {
            blueprintCount++;
            using var blueprint = JsonDocument.Parse(File.ReadAllText(path));
            var failure = ValidateDoor(blueprint.RootElement, genericDoorModels, doorTypeModels);
            if (failure != null)
            {
                failures.Add($"{Path.GetFileName(path)}: {failure}");
            }
        }

        blueprintCount.Should().BeGreaterThan(0, "the blueprint scan must inspect at least one UTD");
        TestContext.Out.WriteLine($"Scanned {blueprintCount} door blueprints.");
        failures.Should().BeEmpty(FormatFailures(failures));
    }

    [Test]
    public void PlacedDoors_UseValidModelRows()
    {
        var root = FindRepositoryRoot();
        var genericDoorModels = LoadModelColumn(root, "genericdoors.2da", "ModelName");
        var doorTypeModels = LoadModelColumn(root, "doortypes.2da", "Model");
        var failures = new List<string>();
        var placedCount = 0;

        foreach (var path in Directory.EnumerateFiles(Path.Combine(root.FullName, "Module", "git"), "*.git.json"))
        {
            using var area = JsonDocument.Parse(File.ReadAllText(path));
            foreach (var door in area.RootElement.GetProperty("Door List").GetProperty("value").EnumerateArray())
            {
                placedCount++;
                var failure = ValidateDoor(door, genericDoorModels, doorTypeModels);
                if (failure != null)
                {
                    failures.Add($"{Path.GetFileName(path)} door {GetString(door, "Tag")} ({GetString(door, "TemplateResRef")}): {failure}");
                }
            }
        }

        placedCount.Should().BeGreaterThan(0, "the placed-door scan must inspect at least one instance");
        TestContext.Out.WriteLine($"Scanned {placedCount} placed doors.");
        failures.Should().BeEmpty(FormatFailures(failures));
    }

    private static string? ValidateDoor(JsonElement door, IReadOnlyList<string> genericDoorModels, IReadOnlyList<string> doorTypeModels)
    {
        var appearance = GetInt(door, "Appearance");
        if (appearance != GenericDoorAppearance)
        {
            return appearance < doorTypeModels.Count
                ? null
                : $"appearance {appearance} is past the last doortypes.2da row ({doorTypeModels.Count - 1}).";
        }

        if (!door.TryGetProperty("GenericType_New", out _))
        {
            return "generic door has no GenericType_New.";
        }

        var genericType = GetInt(door, "GenericType_New");
        if (genericType >= genericDoorModels.Count)
        {
            return $"generic type {genericType} is past the last genericdoors.2da row ({genericDoorModels.Count - 1}).";
        }

        return genericDoorModels[genericType] == "****"
            ? $"generic type {genericType} has no model."
            : null;
    }

    private static List<string> LoadModelColumn(DirectoryInfo root, string fileName, string columnName)
    {
        var lines = File.ReadAllLines(Path.Combine(root.FullName, "SWLOR_Haks", "sw_2da", fileName));
        var header = lines[2].Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        // Data rows carry a leading row-label cell that the header row does not.
        var column = Array.IndexOf(header, columnName) + 1;
        column.Should().BePositive($"{fileName} must have a {columnName} column");

        // NWN ignores the human-readable row label and assigns IDs by physical row order.
        return lines
            .Skip(3)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .Select(cells => column < cells.Length ? cells[column] : "****")
            .ToList();
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
            : $"Found {failures.Count} door model integrity failure(s):{Environment.NewLine}{string.Join(Environment.NewLine, failures.Take(100))}";

    private static DirectoryInfo FindRepositoryRoot()
    {
        var current = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (current != null && !Directory.Exists(Path.Combine(current.FullName, "Module")))
        {
            current = current.Parent;
        }

        return current ?? throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}
