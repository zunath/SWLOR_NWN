using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.SpawnService;

namespace SWLOR.Game.Server.Feature.SpawnDefinition
{
    public class SpaceSpawnDefinition : ISpawnListDefinition
    {
        private readonly SpawnTableBuilder _builder = new();
        public Dictionary<string, SpawnTable> BuildSpawnTables()
        {
            ViscaraOrbit();
            MonCalaOrbit();
            HutlarOrbit();
            TatooineOrbit();
            KorribanOrbit();
            DathomirOrbit();

            return _builder.Build();
        }

        private void ViscaraOrbit()
        {
            _builder.Create("SPACE_VISCARA_ORBIT", "Space - Viscara Orbit")
                .AddSpawn(ObjectType.Creature, "pirate_sybil")
                .WithFrequency(35)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "pirate_terror")
                .WithFrequency(25)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "pirate_night")
                .WithFrequency(5)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void MonCalaOrbit()
        {
            _builder.Create("SPACE_MONCALA_ORBIT", "Space - Mon Cala Orbit")
                .AddSpawn(ObjectType.Creature, "pirate_night")
                .WithFrequency(30)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "pirate_ferron")
                .WithFrequency(20)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "pirate_storm")
                .WithFrequency(10)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void HutlarOrbit()
        {
            _builder.Create("SPACE_HUTLAR_ORBIT", "Space - Hutlar Orbit")
                .AddSpawn(ObjectType.Creature, "pirate_storm")
                .WithFrequency(30)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "pirate_ranger")
                .WithFrequency(25)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "pirate_hammer")
                .WithFrequency(10)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void TatooineOrbit()
        {
            _builder.Create("SPACE_TATOOINE_ORBIT", "Space - Tatooine Orbit")
                .AddSpawn(ObjectType.Creature, "pirate_hammer")
                .WithFrequency(20)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "pirate_drake")
                .WithFrequency(30)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "pirate_borealis")
                .WithFrequency(5)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "pirate_eleyna")
                .WithFrequency(5)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void KorribanOrbit()
        {
            _builder.Create("SPACE_KORRIBAN_ORBIT", "Space - Korriban Orbit")
                .AddSpawn(ObjectType.Creature, "pirate_night")
                .WithFrequency(20)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "pirate_ferron")
                .WithFrequency(20)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "pirate_storm")
                .WithFrequency(20)
                .RandomlyWalks()
                .ReturnsHome();

                .AddSpawn(ObjectType.Creature, "pirate_hammer")
                .WithFrequency(5)
                .RandomlyWalks()
                .ReturnsHome()
        }

        private void DathomirOrbit()
        {
            _builder.Create("SPACE_DATHOMIR_ORBIT", "Space - Dathomir Orbit")
                
                .AddSpawn(ObjectType.Creature, "pirate_drake")
                .WithFrequency(30)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "pirate_borealis")
                .WithFrequency(20)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "pirate_eleyna")
                .WithFrequency(20)
                .RandomlyWalks()
                .ReturnsHome();
        }
    }
}