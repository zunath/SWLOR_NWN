using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.LootTableDefinition;
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
    public void EshanEnemies_CanDropEveryEshanMap()
    {
        var expectedMaps = new HashSet<string>
        {
            "esh_map_orbit",
            "esh_map_height",
            "esh_map_silver",
            "esh_map_gate",
            "esh_map_peaks",
            "esh_map_high",
            "esh_map_shimmer",
            "esh_map_farms",
            "esh_map_battle",
            "esh_map_hearth",
            "esh_map_starport",
            "esh_map_verdant",
            "esh_map_river"
        };
        var tables = new EshanLootTableDefinition().BuildLootTables();

        tables.Keys.Should().BeEquivalentTo(
            "ESHAN_NEOCRUSADER",
            "ESHAN_DIRE_WOLF",
            "ESHAN_FROST_WOLF",
            "ESHAN_DIRE_WOLF_ALPHA",
            "ESHAN_SUN_GUARD",
            "ESHAN_SCRAPYARD_SMUGGLER");

        foreach (var (tableId, table) in tables)
        {
            var maps = table.Where(item => expectedMaps.Contains(item.Resref)).ToList();
            maps.Select(item => item.Resref).Should().BeEquivalentTo(expectedMaps,
                $"{tableId} should provide all Eshan maps");
            maps.Should().OnlyContain(item => item.Weight == 2 && item.MaxQuantity == 1 && item.IsRare,
                $"{tableId} maps should use the standard rare-map loot settings");
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
