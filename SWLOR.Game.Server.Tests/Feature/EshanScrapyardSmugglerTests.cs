using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.LootTableDefinition;
using SWLOR.Game.Server.Feature.SpawnDefinition;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Feature;

public class EshanScrapyardSmugglerTests
{
    private const string SpawnTableId = "ESHAN_SCRAPLAND_CAVES";

    [Test]
    public void ScrapyardSmugglers_SpawnInTheScraplandCavesInsteadOfTheScraplands()
    {
        var root = FindRepositoryRoot();
        using var caves = ReadArea(root, "pw_sc_es_scrcave");
        using var scraplands = ReadArea(root, "sc_esscraplands");

        GetLocalString(caves.RootElement, "CREATURE_SPAWN_TABLE_ID").Should().Be(SpawnTableId);
        GetLocalInt(caves.RootElement, "CREATURE_SPAWN_COUNT").Should().Be(10);
        GetLocalString(scraplands.RootElement, "CREATURE_SPAWN_TABLE_ID").Should().BeEmpty();

        var spawns = new EshanSpawnDefinition().BuildSpawnTables()[SpawnTableId].Spawns;
        spawns.Should().ContainSingle();
        spawns[0].Resref.Should().Be("esh_scrap_smug");
        spawns[0].Type.Should().Be(ObjectType.Creature);
    }

    [Test]
    public void ScrapyardSmugglers_DropFlawedAndGoodQualityElectronics()
    {
        var loot = new EshanLootTableDefinition().BuildLootTables()["ESHAN_SCRAPYARD_SMUGGLER"];

        loot.Should().ContainSingle(item =>
            item.Resref == "elec_flawed" && item.Weight == 5 && item.MaxQuantity == 1);
        loot.Should().ContainSingle(item =>
            item.Resref == "elec_good" && item.Weight == 5 && item.MaxQuantity == 1);
    }

    private static JsonDocument ReadArea(DirectoryInfo root, string resref)
    {
        return JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root.FullName, "Module", "git", $"{resref}.git.json")));
    }

    private static string GetLocalString(JsonElement area, string name)
    {
        return FindLocal(area, name)?.GetProperty("Value").GetProperty("value").GetString() ?? string.Empty;
    }

    private static int GetLocalInt(JsonElement area, string name)
    {
        return FindLocal(area, name)?.GetProperty("Value").GetProperty("value").GetInt32() ?? 0;
    }

    private static JsonElement? FindLocal(JsonElement area, string name)
    {
        foreach (var variable in area.GetProperty("VarTable").GetProperty("value").EnumerateArray())
        {
            if (variable.GetProperty("Name").GetProperty("value").GetString() == name)
                return variable;
        }

        return null;
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        return directory ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
