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
                .WithFrequency(20000)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "pirate_terror")
                .WithFrequency(20000)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "pirate_nightmare")
                .WithFrequency(10000)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "npc_courier")
                .WithFrequency(5000)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cap_corvette")
                .WithFrequency(200)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cap_frigate")
                .WithFrequency(100)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cap_cruiser")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cap_hvycrui")
                .WithFrequency(20)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cap_btlcrui")
                .WithFrequency(10)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cap_btlship")
                .WithFrequency(3)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cap_dread")
                .WithFrequency(1)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void MonCalaOrbit()
        {
            _builder.Create("SPACE_MONCALA_ORBIT", "Space - Mon Cala Orbit")
                .AddSpawn(ObjectType.Creature, "pirate_nightmare")
                .WithFrequency(17500)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "pirate_ferron")
                .WithFrequency(17500)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "pirate_storm")
                .WithFrequency(15000)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cap_corvette")
                .WithFrequency(200)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cap_frigate")
                .WithFrequency(100)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cap_cruiser")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cap_hvycrui")
                .WithFrequency(20)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cap_btlcrui")
                .WithFrequency(10)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cap_btlship")
                .WithFrequency(3)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cap_dread")
                .WithFrequency(1)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void HutlarOrbit()
        {
            _builder.Create("SPACE_HUTLAR_ORBIT", "Space - Hutlar Orbit")
                .AddSpawn(ObjectType.Creature, "pirate_storm")
                .WithFrequency(15000)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "pirate_ranger")
                .WithFrequency(15000)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "pirate_hammer")
                .WithFrequency(10000)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "npc_sithfig")
                .WithFrequency(10000)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "npc_freighter")
                .WithFrequency(5000)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cap_corvette")
                .WithFrequency(200)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cap_frigate")
                .WithFrequency(100)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cap_cruiser")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cap_hvycrui")
                .WithFrequency(20)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cap_btlcrui")
                .WithFrequency(10)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cap_btlship")
                .WithFrequency(3)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cap_dread")
                .WithFrequency(1)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void TatooineOrbit()
        {
            _builder.Create("SPACE_TATOOINE_ORBIT", "Space - Tatooine Orbit")
                .AddSpawn(ObjectType.Creature, "pirate_hammer")
                .WithFrequency(17500)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "pirate_drake")
                .WithFrequency(17500)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "pirate_borealis")
                .WithFrequency(5000)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "pirate_eleyna")
                .WithFrequency(5000)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "npc_bulkf")
                .WithFrequency(5000)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "npc_merchant")
                .WithFrequency(1000)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cap_corvette")
                .WithFrequency(200)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cap_frigate")
                .WithFrequency(100)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cap_cruiser")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cap_hvycrui")
                .WithFrequency(20)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cap_btlcrui")
                .WithFrequency(10)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cap_btlship")
                .WithFrequency(3)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cap_dread")
                .WithFrequency(1)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void KorribanOrbit()
        {
            _builder.Create("SPACE_KORRIBAN_ORBIT", "Space - Korriban Orbit")
                .AddSpawn(ObjectType.Creature, "pirate_nightmare")
                .WithFrequency(5000)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "pirate_ferron")
                .WithFrequency(5000)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "pirate_storm")
                .WithFrequency(5000)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "pirate_hammer")
                .WithFrequency(5000)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "npc_sithfig")
                .WithFrequency(25000)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cap_corvette")
                .WithFrequency(200)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cap_frigate")
                .WithFrequency(100)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cap_cruiser")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cap_hvycrui")
                .WithFrequency(20)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cap_btlcrui")
                .WithFrequency(10)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cap_btlship")
                .WithFrequency(3)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cap_dread")
                .WithFrequency(1)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void DathomirOrbit()
        {
            _builder.Create("SPACE_DATHOMIR_ORBIT", "Space - Dathomir Orbit")
                
                .AddSpawn(ObjectType.Creature, "pirate_drake")
                .WithFrequency(12500)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "pirate_borealis")
                .WithFrequency(15000)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "pirate_eleyna")
                .WithFrequency(15000)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "npc_merchant")
                .WithFrequency(2500)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cap_corvette")
                .WithFrequency(200)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cap_frigate")
                .WithFrequency(100)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cap_cruiser")
                .WithFrequency(50)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cap_hvycrui")
                .WithFrequency(20)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cap_btlcrui")
                .WithFrequency(10)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cap_btlship")
                .WithFrequency(3)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "cap_dread")
                .WithFrequency(1)
                .RandomlyWalks()
                .ReturnsHome();
        }
    }
}