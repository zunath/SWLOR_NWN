using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.LootService;

namespace SWLOR.Game.Server.Tests.Feature;

public class LootTableDefinitionTests
{
    private static readonly HashSet<string> EngineItemTemplateResrefs = new(StringComparer.OrdinalIgnoreCase)
    {
        "nw_it_gold001"
    };

    [Test]
    public void LootTableItems_ResolveToModuleItemTemplates()
    {
        var root = FindRepositoryRoot();
        var itemTemplates = ReadModuleItemTemplateResrefs(root);
        var failures = new List<string>();

        foreach (var definitionType in GetLootTableDefinitionTypes())
        {
            var definition = (ILootTableDefinition)Activator.CreateInstance(definitionType)!;
            var tables = definition.BuildLootTables();

            foreach (var (tableId, table) in tables)
            {
                foreach (var item in table)
                {
                    if (!EngineItemTemplateResrefs.Contains(item.Resref) &&
                        !itemTemplates.Contains(item.Resref))
                    {
                        failures.Add($"{definitionType.Name}/{tableId}: '{item.Resref}' has no Module/uti TemplateResRef.");
                    }
                }
            }
        }

        failures.Should().BeEmpty(string.Join(Environment.NewLine, failures));
    }

    [Test]
    public void LootTables_NeverDropTrainingOrConstructedSabers()
    {
        var root = FindRepositoryRoot();
        var forbidden = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in Directory.EnumerateFiles(Path.Combine(root.FullName, "Module", "uti"), "*.uti.json"))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            if (document.RootElement.GetProperty("BaseItem").GetProperty("value").GetInt32()
                is (int)SWLOR.NWN.API.NWScript.Enum.Item.BaseItem.Lightsaber
                or (int)SWLOR.NWN.API.NWScript.Enum.Item.BaseItem.Saberstaff)
                forbidden.Add(document.RootElement.GetProperty("TemplateResRef").GetProperty("value").GetString()!);
        }

        foreach (var definitionType in GetLootTableDefinitionTypes())
        {
            var tables = ((ILootTableDefinition)Activator.CreateInstance(definitionType)!).BuildLootTables();
            foreach (var (tableId, table) in tables)
                table.Select(item => item.Resref).Should().NotIntersectWith(forbidden,
                    $"{tableId} must respect saber progression regardless of item name or rarity");
        }
    }

    private static IEnumerable<Type> GetLootTableDefinitionTypes()
    {
        return typeof(ILootTableDefinition)
            .Assembly
            .GetTypes()
            .Where(type =>
                typeof(ILootTableDefinition).IsAssignableFrom(type) &&
                !type.IsAbstract &&
                !type.IsInterface)
            .OrderBy(type => type.Name);
    }

    private static HashSet<string> ReadModuleItemTemplateResrefs(DirectoryInfo root)
    {
        var resrefs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Directory.EnumerateFiles(Path.Combine(root.FullName, "Module", "uti"), "*.uti.json"))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(file));
            if (document.RootElement.TryGetProperty("TemplateResRef", out var templateResRef) &&
                templateResRef.TryGetProperty("value", out var value))
            {
                var resref = value.GetString();
                if (!string.IsNullOrWhiteSpace(resref))
                    resrefs.Add(resref);
            }
        }

        return resrefs;
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        directory.Should().NotBeNull("the repository root should be discoverable from the test directory");
        return directory!;
    }
}
