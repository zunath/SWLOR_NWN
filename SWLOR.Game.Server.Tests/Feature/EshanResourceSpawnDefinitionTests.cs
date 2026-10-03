using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.SpawnDefinition;

namespace SWLOR.Game.Server.Tests.Feature;

public class EshanResourceSpawnDefinitionTests
{
    private static readonly Dictionary<string, string[]> ExpectedResources = new()
    {
        ["ESHAN_SILVERWOOD_EXPANSE_RESOURCES"] = ["oak_tree", "aracia_tree", "hyphae_tree"],
        ["ESHAN_BATTLEGROUNDS_RESOURCES"] = ["oak_tree", "aracia_tree", "hyphae_tree"],
        ["ESHAN_SHATTERVEIN_RESOURCES"] = ["keromber_vein", "jasioclase_vein", "arkoxit_vein"],
        ["ESHAN_SHIMMERDEEP_RESOURCES"] = ["keromber_vein", "jasioclase_vein", "arkoxit_vein"],
    };

    private static readonly Dictionary<string, string> ExpectedAreaTables = new()
    {
        ["pw_sc_eshanwilds.git.json"] = "ESHAN_SILVERWOOD_EXPANSE_RESOURCES",
        ["pw_sc_eshbattle.git.json"] = "ESHAN_BATTLEGROUNDS_RESOURCES",
        ["pw_sc_esvein.git.json"] = "ESHAN_SHATTERVEIN_RESOURCES",
        ["pw_sc_es_cavesh.git.json"] = "ESHAN_SHIMMERDEEP_RESOURCES",
    };

    [Test]
    public void EshanResourceTables_ContainTheExpectedHighLevelResources()
    {
        var tables = new EshanResourceSpawnDefinition().BuildSpawnTables();

        tables.Keys.Should().BeEquivalentTo(ExpectedResources.Keys);
        foreach (var (tableId, expectedResrefs) in ExpectedResources)
        {
            tables[tableId].Spawns.Select(spawn => spawn.Resref).Should().BeEquivalentTo(expectedResrefs);
            tables[tableId].Spawns.Should().OnlyContain(spawn => spawn.Weight > 0);
        }
    }

    [Test]
    public void EshanResourceAreas_ReferenceTheirResourceTables()
    {
        foreach (var (areaFile, expectedTable) in ExpectedAreaTables)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(FindRepositoryRoot().FullName, "Module", "git", areaFile)));
            var variables = document.RootElement.GetProperty("VarTable").GetProperty("value");
            var resourceTable = variables.EnumerateArray().Single(variable =>
                variable.GetProperty("Name").GetProperty("value").GetString() == "RESOURCE_SPAWN_TABLE_ID");

            resourceTable.GetProperty("Value").GetProperty("value").GetString().Should().Be(expectedTable);
        }
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "Module")))
        {
            directory = directory.Parent;
        }

        return directory ?? throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}
