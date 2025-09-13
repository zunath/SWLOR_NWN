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
                .ResourceDespawnDelay(90) // 1.5 hours for space station junk
                .AddSpawn(ObjectType.Placeable, "cz220_junk")
                .WithFrequency(50);
        }

        private void Caches(SpawnTableBuilder builder)
        {
            builder.Create("CZ220_CACHES")
                .ResourceDespawnDelay(120) // 2 hours for valuable caches
                .AddSpawn(ObjectType.Placeable, "cz220_cache")
                .WithFrequency(50);
        }

        private void OreVeins(SpawnTableBuilder builder)
        {
            builder.Create("CZ220_VELDITE")
                .ResourceDespawnDelay(240) // 4 hours for basic tier veldite
                .AddSpawn(ObjectType.Placeable, "veldite_vein")
                .WithFrequency(50);
        }

    }
}
