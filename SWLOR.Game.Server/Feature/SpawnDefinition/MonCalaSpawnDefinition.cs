using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.SpawnService;

namespace SWLOR.Game.Server.Feature.SpawnDefinition
{
    public class MonCalaSpawnDefinition: ISpawnListDefinition
    {
        public Dictionary<string, SpawnTable> BuildSpawnTables()
        {
            var builder = new SpawnTableBuilder();
            CoralIsles(builder);
            EcoTerrorists(builder);
            EcoTerroristLeader(builder);

            return builder.Build();
        }

        private void CoralIsles(SpawnTableBuilder builder)
        {
            builder.Create("MONCALA_CORAL_ISLES", "Coral Isles")
                .AddSpawn(ObjectType.Creature, "viper")
                .WithFrequency(20)
                .RandomlyWalks()

                .AddSpawn(ObjectType.Creature, "mc_aradile")
                .WithFrequency(40)
                .RandomlyWalks()

                .AddSpawn(ObjectType.Creature, "mc_amphihydrus")
                .WithFrequency(10)
                .RandomlyWalks();
        }

        private void EcoTerrorists(SpawnTableBuilder builder)
        {
            builder.Create("MONCALA_ECOTERRORISTS", "Eco-Terrorists")
                .AddSpawn(ObjectType.Creature, "ecoterr_1")
                .WithFrequency(50)
                .RandomlyWalks()

                .AddSpawn(ObjectType.Creature, "ecoterr_2")
                .WithFrequency(50)
                .RandomlyWalks();
        }

        private void EcoTerroristLeader(SpawnTableBuilder builder)
        {
            builder.Create("MONCALA_ECOTERRORIST_LEADER", "Eco-Terrorist Leader")
                .AddSpawn(ObjectType.Creature, "ecoterr_ldr")
                .WithFrequency(100)
                .RandomlyWalks();
        }
    }
}
