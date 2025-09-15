using System.Collections.Generic;
using SWLOR.Game.Server.Service.SpawnService;
using SWLOR.NWN.API.NWScript.Enum;

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
            DantooineOrbit();

            return _builder.Build();
        }

        private void ViscaraOrbit()
        {
            _builder.Create("SPACE_VISCARA_ORBIT", "Space - Viscara Orbit")
                .AddSpawn(ObjectType.Creature, "t1bomber")
                .WithFrequency(96)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t1cargo")
                .WithFrequency(2400)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t1platform")
                .WithFrequency(2400)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t1fighter")
                .WithFrequency(38400)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t1gunship")
                .WithFrequency(3840)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t1interceptor")
                .WithFrequency(19200)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t1capital")
                .WithFrequency(960)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap1")
                .WithFrequency(96)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap2")
                .WithFrequency(48)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap3")
                .WithFrequency(12)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap4")
                .WithFrequency(12)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap5")
                .WithFrequency(6)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap6")
                .WithFrequency(2)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap7")
                .WithFrequency(1)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "mandocap1")
                .WithFrequency(12)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "mandocap2")
                .WithFrequency(2)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "mandocap3")
                .WithFrequency(1)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void MonCalaOrbit()
        {
            _builder.Create("SPACE_MONCALA_ORBIT", "Space - Mon Cala Orbit")
                .AddSpawn(ObjectType.Creature, "t2bomber")
                .WithFrequency(96)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t2cargo")
                .WithFrequency(2400)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t2platform")
                .WithFrequency(2400)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t2fighter")
                .WithFrequency(38400)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t2gunship")
                .WithFrequency(3840)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t2interceptor")
                .WithFrequency(19200)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t2capital")
                .WithFrequency(960)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap1")
                .WithFrequency(96)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap2")
                .WithFrequency(48)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap3")
                .WithFrequency(12)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap4")
                .WithFrequency(12)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap5")
                .WithFrequency(6)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap6")
                .WithFrequency(2)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap7")
                .WithFrequency(1)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "mandocap1")
                .WithFrequency(12)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "mandocap2")
                .WithFrequency(2)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "mandocap3")
                .WithFrequency(1)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void HutlarOrbit()
        {
            _builder.Create("SPACE_HUTLAR_ORBIT", "Space - Hutlar Orbit")
                .AddSpawn(ObjectType.Creature, "t3bomber")
                .WithFrequency(96)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t3cargo")
                .WithFrequency(2400)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t3platform")
                .WithFrequency(2400)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t3fighter")
                .WithFrequency(38400)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t3gunship")
                .WithFrequency(3840)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t3interceptor")
                .WithFrequency(19200)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t3capital")
                .WithFrequency(960)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap1")
                .WithFrequency(96)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap2")
                .WithFrequency(48)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap3")
                .WithFrequency(12)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap4")
                .WithFrequency(12)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap5")
                .WithFrequency(6)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap6")
                .WithFrequency(2)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap7")
                .WithFrequency(1)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "mandocap1")
                .WithFrequency(12)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "mandocap2")
                .WithFrequency(2)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "mandocap3")
                .WithFrequency(1)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void TatooineOrbit()
        {
            _builder.Create("SPACE_TATOOINE_ORBIT", "Space - Tatooine Orbit")
                .AddSpawn(ObjectType.Creature, "t4bomber")
                .WithFrequency(96)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t4cargo")
                .WithFrequency(2400)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t4platform")
                .WithFrequency(2400)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t4fighter")
                .WithFrequency(38400)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t4gunship")
                .WithFrequency(3840)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t4interceptor")
                .WithFrequency(19200)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t4capital")
                .WithFrequency(960)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap1")
                .WithFrequency(96)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap2")
                .WithFrequency(48)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap3")
                .WithFrequency(12)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap4")
                .WithFrequency(12)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap5")
                .WithFrequency(6)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap6")
                .WithFrequency(2)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap7")
                .WithFrequency(1)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "mandocap1")
                .WithFrequency(12)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "mandocap2")
                .WithFrequency(2)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "mandocap3")
                .WithFrequency(1)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void KorribanOrbit()
        {
            _builder.Create("SPACE_KORRIBAN_ORBIT", "Space - Korriban Orbit")
                .AddSpawn(ObjectType.Creature, "t6bomber")
                .WithFrequency(96)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t6cargo")
                .WithFrequency(2400)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t6platform")
                .WithFrequency(2400)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t6fighter")
                .WithFrequency(38400)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t6gunship")
                .WithFrequency(3840)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t6interceptor")
                .WithFrequency(19200)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap1")
                .WithFrequency(300)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap2")
                .WithFrequency(700)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap3")
                .WithFrequency(12)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap4")
                .WithFrequency(200)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap5")
                .WithFrequency(6)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap6")
                .WithFrequency(2)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap7")
                .WithFrequency(1)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "mandocap1")
                .WithFrequency(12)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "mandocap2")
                .WithFrequency(2)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "mandocap3")
                .WithFrequency(1)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void DathomirOrbit()
        {
            _builder.Create("SPACE_DATHOMIR_ORBIT", "Space - Dathomir Orbit")
                .AddSpawn(ObjectType.Creature, "t5bomber")
                .WithFrequency(96)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t5cargo")
                .WithFrequency(2400)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t5platform")
                .WithFrequency(2400)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t5fighter")
                .WithFrequency(38400)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t5gunship")
                .WithFrequency(3840)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t5interceptor")
                .WithFrequency(19200)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t5capital")
                .WithFrequency(960)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap1")
                .WithFrequency(96)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap2")
                .WithFrequency(48)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap3")
                .WithFrequency(12)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap4")
                .WithFrequency(12)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap5")
                .WithFrequency(6)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap6")
                .WithFrequency(2)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap7")
                .WithFrequency(1)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "mandocap1")
                .WithFrequency(12)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "mandocap2")
                .WithFrequency(2)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "mandocap3")
                .WithFrequency(1)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void DantooineOrbit()
        {
               _builder.Create("SPACE_DANTOOINE_ORBIT", "Space - Dantooine Orbit")
                .AddSpawn(ObjectType.Creature, "t5bomber")
                .WithFrequency(96)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t5cargo")
                .WithFrequency(2400)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t5platform")
                .WithFrequency(2400)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t5fighter")
                .WithFrequency(38400)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t5gunship")
                .WithFrequency(3840)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t5interceptor")
                .WithFrequency(19200)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t5capital")
                .WithFrequency(960)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap1")
                .WithFrequency(96)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap2")
                .WithFrequency(48)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap3")
                .WithFrequency(12)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap4")
                .WithFrequency(12)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap5")
                .WithFrequency(6)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap6")
                .WithFrequency(2)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap7")
                .WithFrequency(1)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "mandocap1")
                .WithFrequency(12)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "mandocap2")
                .WithFrequency(2)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "mandocap3")
                .WithFrequency(1)
                .RandomlyWalks()
                .ReturnsHome();
        }
    }
}