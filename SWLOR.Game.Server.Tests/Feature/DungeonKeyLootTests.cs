using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.SpawnService;

namespace SWLOR.Game.Server.Tests.Feature;

public class DungeonKeyLootTests
{
    private Dictionary<string, string[]> _keyTablesByCreature = null!;
    private SpawnTable[] _spawnTables = null!;

    [OneTimeSetUp]
    public void LoadConfiguration()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "Module")))
            directory = directory.Parent;
        var root = directory?.FullName ?? throw new DirectoryNotFoundException("Module directory not found.");

        _keyTablesByCreature = Directory.EnumerateFiles(Path.Combine(root, "Module", "utc"), "*.utc.json")
            .ToDictionary(p => Path.GetFileName(p).Split('.')[0], KeyLootTables, StringComparer.OrdinalIgnoreCase);
        _spawnTables = typeof(ISpawnListDefinition).Assembly.GetTypes()
            .Where(t => typeof(ISpawnListDefinition).IsAssignableFrom(t) && t is { IsAbstract: false, IsInterface: false })
            .Select(t => (ISpawnListDefinition)Activator.CreateInstance(t)!)
            .SelectMany(d => d.BuildSpawnTables().Values)
            .ToArray();
    }

    [Test]
    public void EachDungeonKeyLootTable_IsCarriedByOneCreatureBlueprint()
    {
        var carriers = _keyTablesByCreature
            .SelectMany(c => c.Value.Select(table => (Table: table, Creature: c.Key)))
            .GroupBy(x => x.Table)
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key}: {string.Join(", ", g.Select(x => x.Creature).OrderBy(x => x))}");

        carriers.Should().BeEmpty("a dungeon key should drop only from the boss that gates it");
    }

    [Test]
    public void DungeonKeyCarriers_AreNotCommonSpawnsInSharedSpawnTables()
    {
        var keyCarriers = _keyTablesByCreature.Where(c => c.Value.Length > 0)
            .Select(c => c.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var sharedTables = _spawnTables
            .Where(t => t.Spawns.Select(s => s.Resref).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
            .SelectMany(t => t.Spawns.Where(s => !s.IsRare && keyCarriers.Contains(s.Resref)).Select(s => $"{t.Name}: {s.Resref}"));

        sharedTables.Should().BeEmpty("key carriers spawn alone from their own spawn table or as a rare spawn");
    }

    [Test]
    public void ByyskCryoAdept_UsesOverworldByyskLoot()
    {
        _keyTablesByCreature["byysk_cryoadept"].Should().BeEmpty();
    }

    private static string[] KeyLootTables(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        if (!document.RootElement.TryGetProperty("VarTable", out var vars))
            return Array.Empty<string>();

        return vars.GetProperty("value").EnumerateArray()
            .Where(v => v.GetProperty("Name").GetProperty("value").ToString().StartsWith("LOOT_TABLE_"))
            .Select(v => v.GetProperty("Value").GetProperty("value").ToString().Split(',')[0])
            .Where(table => table.EndsWith("_KEY", StringComparison.Ordinal))
            .ToArray();
    }
}
