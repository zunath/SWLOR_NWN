using System.Collections.Generic;
using SWLOR.Game.Server.Service.SpawnService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.SpawnDefinition
{
    public class MonCalaSpawnDefinition: ISpawnListDefinition
    {
        private readonly SpawnTableBuilder _builder = new();
        public Dictionary<string, SpawnTable> BuildSpawnTables()
        {
            CoralIsles();
            CoralIslesReefmaw();
            EcoTerrorists();
            EcoTerroristsSableQuarr();
            EcoTerroristLeader();
            EcoTerroristLeaderKaelDrox();
            SunkenhedgeSwamps();
            SunkenhedgeSwampsInkveil();
            SharptoothCaverns();
            SharptoothCavernsGlassjaw();

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

        private void CoralIslesReefmaw()
        {
            _builder.Create("MONCALA_CORAL_ISLES_REEFMAW", "Coral Isles - Reefmaw")
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
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "reefmaw")
                .WithFrequency(1)
                .AsRare()
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

        private void EcoTerroristsSableQuarr()
        {
            _builder.Create("MONCALA_ECOTERRORISTS_SABLE_QUARR", "Eco-Terrorists - Sable Quarr")
                .AddSpawn(ObjectType.Creature, "ecoterr_1")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "ecoterr_2")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sable_quarr")
                .WithFrequency(1)
                .AsRare()
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

        private void EcoTerroristLeaderKaelDrox()
        {
            _builder.Create("MONCALA_ECOTERRORIST_LEADER_KAEL_DROX", "Eco-Terrorist Leader - Kael Drox")
                .AddSpawn(ObjectType.Creature, "ecoterr_ldr")
                .WithFrequency(100)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "kael_drox")
                .WithFrequency(1)
                .AsRare()
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

        private void SunkenhedgeSwampsInkveil()
        {
            _builder.Create("MONCALA_SUNKENHEAD_SWAMPS_INKVEIL", "Sunkenhead Swamps - Inkveil")
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
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "inkveil")
                .WithFrequency(1)
                .AsRare()
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

        private void SharptoothCavernsGlassjaw()
        {
            _builder.Create("MONCALA_SHARPTOOTH_CAVERNS_GLASSJAW", "Sharptooth Caverns - Glassjaw")
                .AddSpawn(ObjectType.Creature, "mc_microtench")
                .WithFrequency(20)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "glassjaw")
                .WithFrequency(1)
                .AsRare()
                .RandomlyWalks()
                .ReturnsHome();
        }
    }
}
