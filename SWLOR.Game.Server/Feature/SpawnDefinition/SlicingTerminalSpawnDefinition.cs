using System.Collections.Generic;
using SWLOR.Game.Server.Service.SpawnService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.SpawnDefinition
{
    public class SlicingTerminalSpawnDefinition : ISpawnListDefinition
    {
        private readonly SpawnTableBuilder _builder = new();

        public Dictionary<string, SpawnTable> BuildSpawnTables()
        {
            for (var tier = 1; tier <= 5; tier++)
            {
                _builder.Create($"SLICING_TERMINAL_T{tier}", $"Slicing Terminal Tier {tier}")
                    .RespawnDelay(45, 75)
                    .ResourceDespawnDelay(180)
                    .AddSpawn(ObjectType.Placeable, $"slice_term_{tier}");
            }

            return _builder.Build();
        }
    }
}
