using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.SpawnDefinition;
using SWLOR.Game.Server.Service.AIService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Feature;

public class HutlarSpawnDefinitionTests
{
    private static readonly string[] DungeonSpawnTables =
    {
        "HUTLAR_DUNGEON_BROODMOTHER",
        "HUTLAR_DUNGEON_CHIEFTAIN",
        "HUTLAR_DUNGEON_SHAMAN",
        "HUTLAR_DUNGEON_CHAMPION",
        "HUTLAR_DUNGEON_BYYSKGUARDIAN",
        "HUTLAR_DUNGEON_SLUG",
        "HUTLAR_DUNGEON_TUNNELER",
    };

    [Test]
    public void HutlarDungeonCreaturesReturnHome()
    {
        var tables = new HutlarSpawnDefinition().BuildSpawnTables();

        foreach (var tableId in DungeonSpawnTables)
        {
            tables.Should().ContainKey(tableId);
            var creatureSpawns = tables[tableId].Spawns
                .Where(spawn => spawn.Type == ObjectType.Creature)
                .ToList();

            creatureSpawns.Should().NotBeEmpty($"{tableId} must define at least one creature spawn");
            creatureSpawns.Should().OnlyContain(
                spawn => spawn.AIFlags.HasFlag(AIFlag.ReturnHome),
                $"{tableId} creatures must leash home instead of accumulating at area transitions");
        }
    }
}
