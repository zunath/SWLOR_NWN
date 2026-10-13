using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

public class ItemDescriptionReferenceTests
{
    // Forgotten Realms equipment and cloth-dye descriptions inherited by SWLOR templates.
    private static readonly HashSet<long> InheritedDescriptionStrRefs =
    [
        13336, 13469, 14084, 14175, 48931, 56605, 56620, 56640,
        64104, 64105, 66560, 67475, 67520, 90753, 100926, 110846
    ];

    // These equipment rows have no baseitems.2da description to fall back to.
    private static readonly HashSet<int> MissingBaseDescriptionTypes =
    [
        16, 36, 40, 45, 78, 211, 304, 309, 314, 325, 327, 330
    ];

    [TestCase("uti")]
    [TestCase("utm")]
    [TestCase("utc")]
    [TestCase("utp")]
    [TestCase("git")]
    public void Items_DoNotRetainInheritedDescriptionReferences(string folder)
    {
        var root = FindRepositoryRoot();
        var offenders = new List<string>();
        foreach (var path in Directory.EnumerateFiles(Path.Combine(root.FullName, "Module", folder), "*.json"))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            InspectItems(document.RootElement, Path.GetRelativePath(root.FullName, path), offenders);
        }

        offenders.Should().BeEmpty(
            "blueprints and embedded inventory copies must use their local description, " +
            "rather than a stale template string reference:\n" + string.Join('\n', offenders));
    }

    [Test]
    public void PulsegripGloves_HaveLocalDescriptions()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            FindRepositoryRoot().FullName, "Module", "uti", "alpharhy_l8.uti.json")));
        foreach (var field in new[] { "Description", "DescIdentified" })
        {
            var description = document.RootElement.GetProperty(field);
            description.TryGetProperty("id", out _).Should().BeFalse();
            description.GetProperty("value").GetProperty("0").GetString()
                .Should().NotBeNullOrWhiteSpace().And.Contain("gloves");
        }
    }

    [TestCase("agate", "Description", 13293)]
    [TestCase("ruby", "Description", 13301)]
    [TestCase("emerald", "DescIdentified", 13313)]
    public void OriginalGemstones_KeepTheirValidDescriptions(string resref, string field, long strref)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            FindRepositoryRoot().FullName, "Module", "uti", $"{resref}.uti.json")));
        document.RootElement.GetProperty(field).GetProperty("id").GetInt64().Should().Be(strref);
    }

    private static void InspectItems(JsonElement node, string source, List<string> offenders)
    {
        if (node.ValueKind == JsonValueKind.Object)
        {
            if (node.TryGetProperty("BaseItem", out _))
            {
                foreach (var field in new[] { "Description", "DescIdentified" })
                {
                    if (node.TryGetProperty(field, out var description) &&
                        description.TryGetProperty("id", out var id) &&
                        InheritedDescriptionStrRefs.Contains(id.GetInt64()))
                    {
                        var resref = node.TryGetProperty("TemplateResRef", out var template)
                            ? template.GetProperty("value").GetString()
                            : "embedded item";
                        offenders.Add($"{source}: {resref} {field} references {id}");
                    }
                }

                if (MissingBaseDescriptionTypes.Contains(node.GetProperty("BaseItem").GetProperty("value").GetInt32()) &&
                    node.TryGetProperty("DescIdentified", out var identified) &&
                    (!identified.TryGetProperty("id", out var reference) || reference.GetInt64() is -1 or 4294967295) &&
                    !identified.GetProperty("value").EnumerateObject()
                        .Any(value => !string.IsNullOrWhiteSpace(value.Value.GetString())))
                {
                    offenders.Add($"{source}: an item without a base description also has no local identified text");
                }
            }

            foreach (var property in node.EnumerateObject())
                InspectItems(property.Value, source, offenders);
        }
        else if (node.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in node.EnumerateArray())
                InspectItems(child, source, offenders);
        }
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "Module")))
            directory = directory.Parent;
        return directory ?? throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}
