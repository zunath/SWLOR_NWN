using System.IO;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.LootTableDefinition;
using SWLOR.Game.Server.Feature.SpawnDefinition;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Feature;

public class CZ220SpawnDefinitionTests
{
    private static readonly (string Resref, string RareLootTable)[] RareElites =
    {
        ("bulwark", "CZ220_BULWARK_RARES"),
        ("slagborn", "CZ220_SLAGBORN_RARES"),
        ("demolisherzr9", "CZ220_DEMOLISHER_RARES"),
    };

    private const string RareSpawnTable = "CZ220_BREAKER_YARD_RARES";

    [Test]
    public void CZ220RareElites_UseWeightedRareEntriesInDedicatedTable()
    {
        var tables = new CZ220SpawnDefinition().BuildSpawnTables();
        tables.Should().ContainKey(RareSpawnTable);

        var spawns = tables[RareSpawnTable].Spawns;
        spawns.Select(s => s.Resref).Should().BeEquivalentTo(RareElites.Select(r => r.Resref));
        foreach (var spawn in spawns)
        {
            spawn.Type.Should().Be(ObjectType.Creature);
            spawn.Weight.Should().Be(1, "rare elites stay on the normal weighted frequency model");
            spawn.IsRare.Should().BeTrue();
        }
    }

    [Test]
    public void CZ220RareEliteLoot_IsUniqueRareGear()
    {
        var tables = new CZ220LootTableDefinition().BuildLootTables();
        foreach (var (_, tableId) in RareElites)
        {
            tables.Should().ContainKey(tableId);
            var table = tables[tableId];
            table.IsRare.Should().BeTrue();
            table.Should().NotBeEmpty();
            table.Should().OnlyContain(item => item.IsRare && item.MaxQuantity == 1 && item.Weight == 1);
        }
    }

    [Test]
    public void CZ220RareSpawnTable_IsReferencedInBreakerBayOnly()
    {
        var root = FindRepositoryRoot();
        var files = Directory.GetFiles(Path.Combine(root.FullName, "Module", "git"), "*.git.json")
            .Where(file => File.ReadAllText(file).Contains($"\"value\": \"{RareSpawnTable}\""))
            .Select(Path.GetFileName)
            .ToArray();
        files.Should().BeEquivalentTo(new[] { "cz220shipbreakin.git.json" });
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        return directory ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
