using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.SpawnService;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.SpawnDefinition
{
    public class DantooineSpawnDefinition : ISpawnListDefinition
    {
        private readonly SpawnTableBuilder _builder = new();

        public Dictionary<string, SpawnTable> BuildSpawnTables()
        {
            DantooineKinrathCaves();
            DantooineJungle();
            DantooineRuinFarm();
            DantooineLake();
            DantooineJanta();
            DantooineKinrathBoss();
            DantooineMountains();
            DantooineCanyon();
            DantooineBolBoss();
            DantooineSouthPlains();
            
            return _builder.Build();
        }

        private void DantooineKinrathCaves()
        {
            _builder.Create("DANTOOINE_KINRATH_CAVES")
                .AddSpawn(ObjectType.Creature, "hkinrath")
                .RandomlyWalks()
                .WithFrequency(100)
                .ReturnsHome();
        }

        private void DantooineJungle()
        {
            _builder.Create("DANTOOINE_JUNGLE")
                .AddSpawn(ObjectType.Creature, "gizka")
                .RandomlyWalks()
                .WithFrequency(100)

                .AddSpawn(ObjectType.Creature, "hkinrath")
                .RandomlyWalks()
                .WithFrequency(20)
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "iriaz")
                .RandomlyWalks()
                .WithFrequency(100)
                .ReturnsHome()  

                .AddSpawn(ObjectType.Creature, "thune")
                .RandomlyWalks()
                .WithFrequency(5)
                .ReturnsHome();
        }

        private void DantooineRuinFarm()
        {
            _builder.Create("DANTOOINE_RUIN_FARM")
                .AddSpawn(ObjectType.Creature, "pthune")
                .RandomlyWalks()
                .WithFrequency(100)

                .AddSpawn(ObjectType.Creature, "gizka")
                .RandomlyWalks()
                .WithFrequency(20)
                .ReturnsHome()  

                .AddSpawn(ObjectType.Creature, "thune")
                .RandomlyWalks()
                .WithFrequency(5)
                .ReturnsHome();
        }

        private void DantooineLake()
        {
            _builder.Create("DANTOOINE_LAKE")
                .AddSpawn(ObjectType.Creature, "thune")
                .RandomlyWalks()
                .WithFrequency(5)

                .AddSpawn(ObjectType.Creature, "iriaz")
                .RandomlyWalks()
                .WithFrequency(100)

                .AddSpawn(ObjectType.Creature, "hkinrath")
                .RandomlyWalks()
                .WithFrequency(100)
                .ReturnsHome();
        }

        private void DantooineJanta()
        {
            _builder.Create("DANTOOINE_JANTA")
                .AddSpawn(ObjectType.Creature, "gizka")
                .RandomlyWalks()
                .WithFrequency(10)

                .AddSpawn(ObjectType.Creature, "voritorlizard")
                .RandomlyWalks()
                .WithFrequency(50)
                .ReturnsHome();
        }

        private void DantooineKinrathBoss()
        {
            _builder.Create("DANTOOINE_KIN_BOSS")
                .AddSpawn(ObjectType.Creature, "vqueenkin")
                .RandomlyWalks()
                .WithFrequency(1)
                .RespawnDelay(60 + Random.D100(1))
                .ReturnsHome();
        }

        private void DantooineMountains()
        {
            _builder.Create("DANTOOINE_MOUNTAINS")
                .AddSpawn(ObjectType.Creature, "iriaz")
                .RandomlyWalks()
                .WithFrequency(100)
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "thune")
                .RandomlyWalks()
                .WithFrequency(100)
                .ReturnsHome();
        }

        private void DantooineCanyon()
        {
            _builder.Create("DANTOOINE_CANYON")
                .AddSpawn(ObjectType.Creature, "gizka")
                .RandomlyWalks()
                .WithFrequency(100)
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "iriaz")
                .RandomlyWalks()
                .WithFrequency(100)
                .ReturnsHome();
        }

        private void DantooineSouthPlains()
        {
            _builder.Create("DANTOOINE_SOUTH_PLAINS")
                .AddSpawn(ObjectType.Creature, "dantarihunter")
                .RandomlyWalks()
                .WithFrequency(100)

                .AddSpawn(ObjectType.Creature, "dantarishaman")
                .RandomlyWalks()
                .WithFrequency(100)
                .ReturnsHome();
        }

        private void DantooineBolBoss()
        {
            _builder.Create("DANTOOINE_BOL")
                .AddSpawn(ObjectType.Creature, "bolboss")
                .RandomlyWalks()
                .RespawnDelay(60 + Random.D100(1))
                .WithFrequency(100);

        }
    }
}
