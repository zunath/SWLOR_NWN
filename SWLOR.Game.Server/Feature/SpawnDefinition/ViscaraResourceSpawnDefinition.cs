using System.Collections.Generic;
using SWLOR.Game.Server.Service.SpawnService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.SpawnDefinition
{
    public class ViscaraResourceSpawnDefinition: ISpawnListDefinition
    {
        private readonly SpawnTableBuilder _builder = new();
        public Dictionary<string, SpawnTable> BuildSpawnTables()
        {
            Cavern();
            CoxxionBase();
            DeepMountainsResources();
            MandalorianFacilityResources();
            MountainValleyResources();
            WildlandsResources();
            WildwoodsResources();
            SwamplandResources();
            VelesSewersResources();

            return _builder.Build();
        }

        private void Cavern()
        {
            _builder.Create("RESOURCES_VISCARA_CAVERN")
                .ResourceDespawnDelay(240) // 4 hours for tier 1 resources
                .AddSpawn(ObjectType.Placeable, "veldite_vein")
                .WithFrequency(60)

                .AddSpawn(ObjectType.Placeable, "scordspar_vein")
                .WithFrequency(20);
        }

        private void CoxxionBase()
        {
            _builder.Create("COXXION_BASE_SCAVENGE")
                .ResourceDespawnDelay(90) // 1.5 hours for scavenge items
                .AddSpawn(ObjectType.Placeable, "v_cox_scav");
        }

        private void DeepMountainsResources()
        {
            _builder.Create("RESOURCES_VISCARA_DEEPMOUNTAINS")
                .ResourceDespawnDelay(120) // 2 hours for higher tier resources
                .AddSpawn(ObjectType.Placeable, "veldite_vein")
                .WithFrequency(10)

                .AddSpawn(ObjectType.Placeable, "scordspar_vein")
                .WithFrequency(60)

                .AddSpawn(ObjectType.Placeable, "herbs_patch2")
                .WithFrequency(10)

                .AddSpawn(ObjectType.Placeable, "patch_veggies2")
                .WithFrequency(5)

                .AddSpawn(ObjectType.Placeable, "fiberp_bush_1")
                .WithFrequency(1)

                .AddSpawn(ObjectType.Placeable, "fiberp_bush_2")
                .WithFrequency(5);
        }

        private void MandalorianFacilityResources()
        {
            _builder.Create("RESOURCES_VISCARA_MANDALORIANFACILITY")
                .ResourceDespawnDelay(180) // 3 hours for special loot
                .AddSpawn(ObjectType.Placeable, "mando_crate");
        }

        private void MountainValleyResources()
        {
            _builder.Create("RESOURCES_VISCARA_MOUNTAINVALLEY")
                .AddSpawn(ObjectType.Placeable, "veldite_vein")
                .WithFrequency(50)

                .AddSpawn(ObjectType.Placeable, "scordspar_vein")
                .WithFrequency(25)

                .AddSpawn(ObjectType.Placeable, "herbs_patch2")
                .WithFrequency(10)

                .AddSpawn(ObjectType.Placeable, "patch_veggies2")
                .WithFrequency(5)

                .AddSpawn(ObjectType.Placeable, "tree")
                .WithFrequency(5)

                .AddSpawn(ObjectType.Placeable, "fiberp_bush_1")
                .WithFrequency(10)

                .AddSpawn(ObjectType.Placeable, "fiberp_bush_2")
                .WithFrequency(5);
        }

        private void WildlandsResources()
        {
            _builder.Create("RESOURCES_VISCARA_WILDLANDS")
                .ResourceDespawnDelay(300) // 5 hours for basic/starting resources
                .AddSpawn(ObjectType.Placeable, "veldite_vein")
                .WithFrequency(20)

                .AddSpawn(ObjectType.Placeable, "tree")
                .WithFrequency(50)

                .AddSpawn(ObjectType.Placeable, "herbs_patch")
                .WithFrequency(10)

                .AddSpawn(ObjectType.Placeable, "patch_veggies")
                .WithFrequency(10)

                .AddSpawn(ObjectType.Placeable, "fiberp_bush_1")
                .WithFrequency(30);
        }

        private void WildwoodsResources()
        {
            _builder.Create("RESOURCES_VISCARA_WILDWOODS")
                .AddSpawn(ObjectType.Placeable, "veldite_vein")
                .WithFrequency(10)

                .AddSpawn(ObjectType.Placeable, "tree")
                .WithFrequency(70)

                .AddSpawn(ObjectType.Placeable, "herbs_patch")
                .WithFrequency(30)

                .AddSpawn(ObjectType.Placeable, "patch_veggies")
                .WithFrequency(20)

                .AddSpawn(ObjectType.Placeable, "fiberp_bush_1")
                .WithFrequency(20);
        }

        private void SwamplandResources()
        {
            _builder.Create("RESOURCES_VISCARA_SWAMPLANDS")
                .AddSpawn(ObjectType.Placeable, "veldite_vein")
                .WithFrequency(1)

                .AddSpawn(ObjectType.Placeable, "scordspar_vein")
                .WithFrequency(5)

                .AddSpawn(ObjectType.Placeable, "tree")
                .WithFrequency(20)

                .AddSpawn(ObjectType.Placeable, "herbs_patch2")
                .WithFrequency(10)

                .AddSpawn(ObjectType.Placeable, "patch_veggies2")
                .WithFrequency(5)

                .AddSpawn(ObjectType.Placeable, "oak_tree")
                .WithFrequency(50)

                .AddSpawn(ObjectType.Placeable, "fiberp_bush_1")
                .WithFrequency(25)

                .AddSpawn(ObjectType.Placeable, "fiberp_bush_2")
                .WithFrequency(50);
        }

        private void VelesSewersResources()
        {
            _builder.Create("RESOURCES_VISCARA_VELESSEWERS")
                .ResourceDespawnDelay(120) // 2 hours for junk/scrap
                .AddSpawn(ObjectType.Placeable, "cz220_junk");
        }
    }
}
