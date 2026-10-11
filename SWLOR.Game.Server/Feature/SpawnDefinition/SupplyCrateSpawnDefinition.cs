using System.Collections.Generic;
using SWLOR.Game.Server.Service.SpawnService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.SpawnDefinition
{
    public class SupplyCrateSpawnDefinition : ISpawnListDefinition
    {
        private readonly SpawnTableBuilder _builder = new();

        public Dictionary<string, SpawnTable> BuildSpawnTables()
        {
            SupplyCrateTier5();

            return _builder.Build();
        }

        private void SupplyCrateTier5()
        {
            _builder.Create("SUPPLY_CRATE_T5", "Supply Crate Tier 5")
                .ResourceDespawnDelay(60)
                .AddSpawn(ObjectType.Placeable, "supply_crate_5");
        }
    }
}
