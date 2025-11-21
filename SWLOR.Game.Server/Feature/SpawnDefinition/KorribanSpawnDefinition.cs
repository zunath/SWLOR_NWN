using System.Collections.Generic;
using SWLOR.Game.Server.Service.SpawnService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.SpawnDefinition
{
    public class KorribanSpawnDefinition : ISpawnListDefinition
    {
        private readonly SpawnTableBuilder _builder = new();

        public Dictionary<string, SpawnTable> BuildSpawnTables()
        {
            Wastelands();
            Valley();
            Ravine();
            Caverns();
            Dunes();
            SithCrypt();
            SithTemples();
            KorribanFortress();
            

            return _builder.Build();
        }

        private void Wastelands()
        {
            _builder.Create("KORRIBAN_WASTELANDS", "Wastelands")
                .AddSpawn(ObjectType.Creature, "pelko")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "korr_klorslug")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome();

        }
        private void Valley()
        {
            _builder.Create("KORRIBAN_VALLEY", "Valley")
                .AddSpawn(ObjectType.Creature, "pelko")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "korr_klorslug")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithsnake")
                .WithFrequency(10)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void Ravine()
        {
            _builder.Create("KORRIBAN_RAVINE", "Ravine")
                .AddSpawn(ObjectType.Creature, "pelko")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "korr_klorslug")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithsnake")
                .WithFrequency(10)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void Caverns()
        {
            _builder.Create("KORRIBAN_CAVERNS", "Caverns")
                .AddSpawn(ObjectType.Creature, "shyrack")
                .WithFrequency(30)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithsnake")
                .WithFrequency(10)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void Dunes()
        {
            _builder.Create("KORRIBAN_DUNES", "Dunes")
                .AddSpawn(ObjectType.Creature, "pelko")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "korr_klorslug")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "korr_wraid")
                .WithFrequency(5)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void SithCrypt()
        {
            _builder.Create("KORRIBAN_SITH_CRYPT", "Sith Crypt")
                .AddSpawn(ObjectType.Creature, "s_app")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "s_app_m")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void KorribanFortress()
        {
            _builder.Create("KorribanDungeonMaster")
                .AddSpawn(ObjectType.Creature, "vkorrdun4boss")
                .WithFrequency(1)
                .RespawnDelay(120);

            _builder.Create("KorribanDungeonGuardian")
                .AddSpawn(ObjectType.Creature, "vkorrdungate")
                .WithFrequency(1)
                .RespawnDelay(20);

            _builder.Create("KorribanDungeonCouncilGuard")
                .AddSpawn(ObjectType.Creature, "vkorrduncouncilg")
                .WithFrequency(1)
                .RespawnDelay(20);

            _builder.Create("KorribanDungeonMarauder")
                .AddSpawn(ObjectType.Creature, "vkorrdunmarauder")
                .RandomlyWalks()
                .WithFrequency(1)
                .RespawnDelay(5)

                .AddSpawn(ObjectType.Creature, "vkorrdunsorc")
                .RandomlyWalks()
                .WithFrequency(1)
                .RespawnDelay(5);

            _builder.Create("KorribanDungeonTrooper")
                .AddSpawn(ObjectType.Creature, "vkorrdun1rifle")
                .RandomlyWalks()
                .WithFrequency(1)
                .RespawnDelay(2)

                .AddSpawn(ObjectType.Creature, "vkorrdun1sword")
                .RandomlyWalks()
                .WithFrequency(1)
                .RespawnDelay(2);

            _builder.Create("KorribanDungeonWarform")
                .AddSpawn(ObjectType.Creature, "vkorrdunwarform")
                .WithFrequency(2)
                .RespawnDelay(20);

            _builder.Create("KorribanDungeonInquisitor")
                .AddSpawn(ObjectType.Creature, "vkorrduninquis")
                .WithFrequency(1)
                .RespawnDelay(20);

            _builder.Create("KorribanDungeonIndustrial")
                .AddSpawn(ObjectType.Creature, "vkorrdundroidhvy")
                .RandomlyWalks()
                .WithFrequency(1)
                .RespawnDelay(10)

                .AddSpawn(ObjectType.Creature, "vkorrdunmarauder")
                .RandomlyWalks()
                .WithFrequency(1)
                .RespawnDelay(5)

                .AddSpawn(ObjectType.Creature, "vkorrdunsorc")
                .RandomlyWalks()
                .WithFrequency(1)
                .RespawnDelay(5);
        }
        private void SithTemples()
        {
            _builder.Create("KORRIBAN_TEMPLES")
                .AddSpawn(ObjectType.Creature, "s_app")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "s_app_m")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "shyrack")
                .WithFrequency(30)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithsnake")
                .WithFrequency(10)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "tukata")
                .WithFrequency(10)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "korriinitiate")
                .WithFrequency(10)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "korr_wraid")
                .WithFrequency(10)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "frogboss")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome();
        }
    }
}
