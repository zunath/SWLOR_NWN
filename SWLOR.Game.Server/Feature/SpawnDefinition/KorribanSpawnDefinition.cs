using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.SpawnService;

namespace SWLOR.Game.Server.Feature.SpawnDefinition
{
    public class KorribanSpawnDefinition: ISpawnListDefinition
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
    }
}
