using System.Collections.Generic;
using SWLOR.Game.Server.Service.SpawnService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.SpawnDefinition
{
    public class AbandonedStationSpawnDefinition : ISpawnListDefinition
    {
        private readonly SpawnTableBuilder _builder = new();

        public Dictionary<string, SpawnTable> BuildSpawnTables()
        {
            MainLevel();
            RestrictedLevel();
            RancorContainment();
            DirectorsChambers();

            return _builder.Build();
        }

        private void MainLevel()
        {
            _builder.Create("ABANDONED_STATION_MAIN", "Abandoned Station - Main Level")
                .AddSpawn(ObjectType.Creature, "zomb_worker1")
                .WithFrequency(30)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "zomb_worker2")
                .WithFrequency(30)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "zomb_scientist1")
                .WithFrequency(20)
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "abs_rakghoul")
                .WithFrequency(25)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "zomb_guard")
                .WithFrequency(15)
                .ReturnsHome();
        }

        private void RestrictedLevel()
        {
            _builder.Create("ABANDONED_STATION_RESTRICTED", "Abandoned Station - Restricted Level")
                .AddSpawn(ObjectType.Creature, "abs_rakstalk")
                .WithFrequency(30)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "abs_zgenetic")
                .WithFrequency(25)
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "abs_zsubject")
                .WithFrequency(30)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "abs_rakbrute")
                .WithFrequency(15)
                .ReturnsHome();
        }

        private void RancorContainment()
        {
            _builder.Create("ABANDONED_STATION_RANCOR", "Abandoned Station - Rancor Containment")
                .AddSpawn(ObjectType.Creature, "zomb_rancor")
                .WithFrequency(1)
                .ReturnsHome()
                .RespawnDelay(20, 40);
        }

        private void DirectorsChambers()
        {
            _builder.Create("ABANDONED_STATION_CHAMBERS", "Abandoned Station - Director's Chambers")
                .AddSpawn(ObjectType.Creature, "abs_rakalpha")
                .WithFrequency(1)
                .ReturnsHome();

            _builder.Create("ABANDONED_STATION_DIRECTOR", "Abandoned Station - Station Director")
                .AddSpawn(ObjectType.Creature, "abs_zdirector")
                .WithFrequency(1)
                .ReturnsHome()
                .RespawnDelay(15, 30);
        }
    }
}
