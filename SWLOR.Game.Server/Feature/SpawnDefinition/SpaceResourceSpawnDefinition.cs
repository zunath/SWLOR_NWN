using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.SpawnService;

namespace SWLOR.Game.Server.Feature.SpawnDefinition
{
    public class SpaceResourceSpawnDefinition : ISpawnListDefinition
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
            _builder.Create("SPACE_RESOURCES_VISCARA_ORBIT", "Space Resources - Viscara Orbit")
                .AddSpawn(ObjectType.Placeable, "spc_asteroid_til")
                .WithFrequency(50)

                .AddSpawn(ObjectType.Placeable, "asteroid_currian")
                .WithFrequency(5);
        }

        private void MonCalaOrbit()
        {
            _builder.Create("SPACE_RESOURCES_MONCALA_ORBIT", "Space Resources - Mon Cala Orbit")
                .AddSpawn(ObjectType.Placeable, "spc_asteroid_til")
                .WithFrequency(50)

                .AddSpawn(ObjectType.Placeable, "asteroid_currian")
                .WithFrequency(30)

                .AddSpawn(ObjectType.Placeable, "asteroid_idailia")
                .WithFrequency(5);
        }

        private void HutlarOrbit()
        {
            _builder.Create("SPACE_RESOURCES_HUTLAR_ORBIT", "Space Resources - Hutlar Orbit")
                .AddSpawn(ObjectType.Placeable, "asteroid_currian")
                .WithFrequency(50)

                .AddSpawn(ObjectType.Placeable, "asteroid_idailia")
                .WithFrequency(30)

                .AddSpawn(ObjectType.Placeable, "asteroid_bariniu")
                .WithFrequency(5);
        }

        private void TatooineOrbit()
        {
            _builder.Create("SPACE_RESOURCES_TATOOINE_ORBIT", "Space Resources - Tatooine Orbit")
                .AddSpawn(ObjectType.Placeable, "asteroid_idailia")
                .WithFrequency(50)

                .AddSpawn(ObjectType.Placeable, "asteroid_bariniu")
                .WithFrequency(30)

                .AddSpawn(ObjectType.Placeable, "asteroid_gostian")
                .WithFrequency(5);
        }

        private void KorribanOrbit()
        {
            _builder.Create("SPACE_RESOURCES_KORRIBAN_ORBIT", "Space Resources - Korriban Orbit")
                .AddSpawn(ObjectType.Placeable, "spc_asteroid_til")
                .WithFrequency(50)

                .AddSpawn(ObjectType.Placeable, "asteroid_currian")
                .WithFrequency(30);
        }

        private void DathomirOrbit()
        {
            _builder.Create("SPACE_RESOURCES_DATHOMIR_ORBIT", "Space Resources - Dathomir Orbit")
                
                .AddSpawn(ObjectType.Placeable, "asteroid_bariniu")
                .WithFrequency(50)

                .AddSpawn(ObjectType.Placeable, "asteroid_gostian")
                .WithFrequency(10);
        }
        private void DantooineOrbit()
        { 
            _builder.Create("SPACE_RESOURCES_DANTOOINE_ORBIT", "Space Resources - Dantooine Orbit")

                .AddSpawn(ObjectType.Placeable, "asteroid_bariniu")
                .WithFrequency(50)

                .AddSpawn(ObjectType.Placeable, "asteroid_gostian")
                .WithFrequency(10);
        }
    }
}