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
    public void EshanMaps_UseASeparateRareLootTableWiredToEveryEnemy()
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
            "esh_map_river",
            "esh_map_marches",
            "esh_map_oldquart",
            "esh_map_groves",
            "esh_map_proving",
            "esh_map_scrap",
            "esh_map_tunnels",
            "esh_map_vein",
            "esh_map_scrcave"
        };
        var enemyResrefs = new[]
        {
            "esh_direwolf", "esh_frostwolf", "esh_gorakvesh", "esh_nc_captain",
            "esh_nc_heavy", "esh_nc_hunter", "esh_nc_medic", "esh_nc_scout",
            "esh_nc_vanguard", "esh_scrap_smug", "esh_sunguard", "esh_wolfalpha"
        };
        var tables = new EshanLootTableDefinition().BuildLootTables();

        var mapTable = tables["ESHAN_MAP_RARES"];
        mapTable.IsRare.Should().BeTrue();
        mapTable.Select(item => item.Resref).Should().BeEquivalentTo(expectedMaps);
        mapTable.Should().OnlyContain(item => item.Weight == 2 && item.MaxQuantity == 1 && item.IsRare,
            "maps should use the standard rare-map loot settings");

        foreach (var (tableId, table) in tables.Where(entry => entry.Key != "ESHAN_MAP_RARES"))
        {
            table.Should().NotContain(item => expectedMaps.Contains(item.Resref),
                $"{tableId} should preserve its normal material and credit roll");
        }

        var root = FindRepositoryRoot();
        foreach (var enemyResref in enemyResrefs)
        {
            using var creature = JsonDocument.Parse(File.ReadAllText(Path.Combine(
                root.FullName, "Module", "utc", $"{enemyResref}.utc.json")));
            var lootVariables = creature.RootElement.GetProperty("VarTable").GetProperty("value")
                .EnumerateArray()
                .Where(variable => variable.GetProperty("Name").GetProperty("value").GetString()!
                    .StartsWith("LOOT_TABLE_"))
                .Select(variable => variable.GetProperty("Value").GetProperty("value").GetString())
                .ToList();

            lootVariables.Should().Contain("ESHAN_MAP_RARES,5,1",
                $"{enemyResref} should roll separately for an Eshan map");
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
