using SWLOR.Component.World.Contracts;
using SWLOR.Component.World.Model;
using SWLOR.Component.World.Service;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.World.Contracts;
using SWLOR.Shared.Domain.World.ValueObjects;

namespace SWLOR.Component.World.Feature.SpawnDefinition
{
    public class CZ220ResourceSpawnDefinition: ISpawnListDefinition
    {
        private readonly ISpawnTableBuilder _builder;

        public CZ220ResourceSpawnDefinition(ISpawnTableBuilder spawnTableBuilder)
        {
            _builder = spawnTableBuilder;
        }

        public Dictionary<string, SpawnTable> BuildSpawnTables()
        {
            JunkPiles();
            Caches();
            OreVeins();

            return _builder.Build();
        }

        private void JunkPiles()
        {
            _builder.Create("CZ220_JUNKPILES")
                .ResourceDespawnDelay(90) // 1.5 hours for space station junk
                .AddSpawn(ObjectType.Placeable, "cz220_junk")
                .WithFrequency(50);
        }

        private void Caches()
        {
            _builder.Create("CZ220_CACHES")
                .ResourceDespawnDelay(120) // 2 hours for valuable caches
                .AddSpawn(ObjectType.Placeable, "cz220_cache")
                .WithFrequency(50);
        }

        private void OreVeins()
        {
            _builder.Create("CZ220_VELDITE")
                .ResourceDespawnDelay(240) // 4 hours for basic tier veldite
                .AddSpawn(ObjectType.Placeable, "veldite_vein")
                .WithFrequency(50);
        }

    }
}
