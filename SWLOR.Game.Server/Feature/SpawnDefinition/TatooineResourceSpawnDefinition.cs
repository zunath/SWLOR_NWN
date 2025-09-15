using System.Collections.Generic;
using SWLOR.Game.Server.Service.SpawnService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.SpawnDefinition
{
    public class TatooineResourceSpawnDefinition: ISpawnListDefinition
    {
        private readonly SpawnTableBuilder _builder = new();

        public Dictionary<string, SpawnTable> BuildSpawnTables()
        {
            AridHillyDesert();
            TatooineResources();
            TuskenCamp();

            return _builder.Build();
        }

        private void AridHillyDesert()
        {
            _builder.Create("TATOOINE_RESOURCE_ARID_HILLY_DESERT")
                .ResourceDespawnDelay(90) // 1.5 hours for higher tier desert resources
                .AddSpawn(ObjectType.Placeable, "plagionite_vein")
                .WithFrequency(40)

                .AddSpawn(ObjectType.Placeable, "keromber_vein")
                .WithFrequency(10)

                .AddSpawn(ObjectType.Placeable, "jasioclase_vein")
                .WithFrequency(10)

                .AddSpawn(ObjectType.Placeable, "herbs_patch_4")
                .WithFrequency(30)
                .AddSpawn(ObjectType.Placeable, "herbs_patch_5")
                .WithFrequency(10)

                .AddSpawn(ObjectType.Placeable, "patch_veggies4")
                .WithFrequency(10)
                .AddSpawn(ObjectType.Placeable, "patch_veggies5")
                .WithFrequency(5)

                .AddSpawn(ObjectType.Placeable, "fiberp_bush_3")
                .WithFrequency(40)

                .AddSpawn(ObjectType.Placeable, "fiberp_bush_4")
                .WithFrequency(20)

                .AddSpawn(ObjectType.Placeable, "fiberp_bush_5")
                .WithFrequency(5);
        }

        private void TatooineResources()
        {
            _builder.Create("TATOOINE_RESOURCE_GENERAL")
                .ResourceDespawnDelay(120) // 2 hours for standard higher tier resources
                .AddSpawn(ObjectType.Placeable, "plagionite_vein")
                .WithFrequency(40)

                .AddSpawn(ObjectType.Placeable, "keromber_vein")
                .WithFrequency(10)

                .AddSpawn(ObjectType.Placeable, "herbs_patch_4")
                .WithFrequency(30)

                .AddSpawn(ObjectType.Placeable, "fiberp_bush_4")
                .WithFrequency(10);
        }
        
        private void TuskenCamp()
        {
            _builder.Create("TATTOOINE_TUSKEN_CAMP")
                .ResourceDespawnDelay(90) // 1.5 hours for special enemy camp loot
                .AddSpawn(ObjectType.Placeable, "t_tusk_cmp");
        }
    }
}
