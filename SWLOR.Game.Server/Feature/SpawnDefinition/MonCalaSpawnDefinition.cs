using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.SpawnService;

namespace SWLOR.Game.Server.Feature.SpawnDefinition
{
    public class MonCalaSpawnDefinition: ISpawnListDefinition
    {
        private readonly SpawnTableBuilder _builder = new();
        public Dictionary<string, SpawnTable> BuildSpawnTables()
        {
            CoralIsles();
            EcoTerrorists();
            EcoTerroristLeader();
            SunkenhedgeSwamps();
            SharptoothCaverns();

            return _builder.Build();
        }

        private void CoralIsles()
        {
            _builder.Create("MONCALA_CORAL_ISLES", "Coral Isles")
                .AddSpawn(ObjectType.Creature, "viper")
                .WithFrequency(20)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "mc_aradile")
                .WithFrequency(40)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "mc_amphihydrus")
                .WithFrequency(10)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void EcoTerrorists()
        {
            _builder.Create("MONCALA_ECOTERRORISTS", "Eco-Terrorists")
                .AddSpawn(ObjectType.Creature, "ecoterr_1")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "ecoterr_2")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void EcoTerroristLeader()
        {
            _builder.Create("MONCALA_ECOTERRORIST_LEADER", "Eco-Terrorist Leader")
                .AddSpawn(ObjectType.Creature, "ecoterr_ldr")
                .WithFrequency(100)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void SunkenhedgeSwamps()
        {
            _builder.Create("MONCALA_SUNKENHEAD_SWAMPS", "Sunkenhead Swamps")
                .AddSpawn(ObjectType.Creature, "mc_octotench")
                .WithFrequency(10)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "mc_scorchys")
                .WithFrequency(30)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "mc_microtench")
                .WithFrequency(40)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void SharptoothCaverns()
        {
            _builder.Create("MONCALA_SHARPTOOTH_CAVERNS", "Sharptooth Caverns")
                .AddSpawn(ObjectType.Creature, "mc_microtench")
                .WithFrequency(20)
                .RandomlyWalks()
                .ReturnsHome();
        }
    }
}
