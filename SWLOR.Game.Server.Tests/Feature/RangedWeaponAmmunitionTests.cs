using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

public class RangedWeaponAmmunitionTests
{
    [TestCase("uti")]
    [TestCase("utc")]
    [TestCase("utm")]
    [TestCase("utp")]
    [TestCase("git")]
    public void ModuleBlasters_HaveUnlimitedAmmunition(string folder)
    {
        var root = FindRepositoryRoot();
        var blasterTypes = Item.RifleBaseItemTypes.Concat(Item.PistolBaseItemTypes)
            .Select(type => (int)type)
            .ToHashSet();
        var failures = new List<string>();

        foreach (var path in Directory.EnumerateFiles(Path.Combine(root.FullName, "Module", folder), "*.json"))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            foreach (var item in EnumerateJsonObjects(document.RootElement))
            {
                if (!item.TryGetProperty("BaseItem", out var baseItem) ||
                    !blasterTypes.Contains(baseItem.GetProperty("value").GetInt32()))
                    continue;

                // Store purchase filters also contain BaseItem, but are not inventory items.
                if (!item.TryGetProperty("TemplateResRef", out var template) &&
                    (!item.TryGetProperty("__data_type", out var dataType) || dataType.GetString() != "UTI "))
                    continue;

                var hasAmmo = item.TryGetProperty("PropertiesList", out var properties) &&
                              properties.GetProperty("value").EnumerateArray().Any(property =>
                                  property.GetProperty("PropertyName").GetProperty("value").GetInt32() ==
                                  (int)ItemPropertyType.UnlimitedAmmunition);
                if (!hasAmmo)
                {
                    var resref = template.ValueKind == JsonValueKind.Undefined
                        ? "<unnamed>"
                        : template.GetProperty("value").GetString();
                    failures.Add($"{Path.GetRelativePath(root.FullName, path)}: {resref}");
                }
            }
        }

        failures.Should().BeEmpty("rifles and pistols require unlimited ammunition to fire, including embedded store and NPC items");
    }

    private static IEnumerable<JsonElement> EnumerateJsonObjects(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            yield return element;
            foreach (var property in element.EnumerateObject())
            foreach (var child in EnumerateJsonObjects(property.Value))
                yield return child;
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            foreach (var child in EnumerateJsonObjects(item))
                yield return child;
        }
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        return directory ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
