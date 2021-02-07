using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.SpawnService;

namespace SWLOR.Game.Server.Feature.SpawnDefinition
{
    public class CZ220ResourceSpawnDefinition: ISpawnListDefinition
    {
        public Dictionary<string, SpawnTable> BuildSpawnTables()
        {
            var builder = new SpawnTableBuilder();
            JunkPiles(builder);
            Caches(builder);
            OreVeins(builder);

            return builder.Build();
        }

        private void JunkPiles(SpawnTableBuilder builder)
        {
            builder.Create("CZ220_JUNKPILES")
                .AddSpawn(ObjectType.Placeable, "cz220_junk");
        }

        private void Caches(SpawnTableBuilder builder)
        {
            builder.Create("CZ220_CACHES")
                .AddSpawn(ObjectType.Placeable, "cz220_cache");
        }

        private void OreVeins(SpawnTableBuilder builder)
        {
            builder.Create("CZ220_VELDITE")
                .AddSpawn(ObjectType.Placeable, "veldite_vein");
        }

    }
}
