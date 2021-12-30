using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.SpawnService;

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
                .AddSpawn(ObjectType.Placeable, "veldite_vein")
                .WithFrequency(60)

                .AddSpawn(ObjectType.Placeable, "scordspar_vein")
                .WithFrequency(20);
        }

        private void CoxxionBase()
        {
            _builder.Create("COXXION_BASE_SCAVENGE")
                .AddSpawn(ObjectType.Placeable, "v_cox_scav");
        }

        private void DeepMountainsResources()
        {
            _builder.Create("RESOURCES_VISCARA_DEEPMOUNTAINS")
                .AddSpawn(ObjectType.Placeable, "veldite_vein")
                .WithFrequency(60)

                .AddSpawn(ObjectType.Placeable, "scordspar_vein")
                .WithFrequency(10);
        }

        private void MandalorianFacilityResources()
        {
            _builder.Create("RESOURCES_VISCARA_MANDALORIANFACILITY")
                .AddSpawn(ObjectType.Placeable, "veldite_vein");
        }

        private void MountainValleyResources()
        {
            _builder.Create("RESOURCES_VISCARA_MOUNTAINVALLEY")
                .AddSpawn(ObjectType.Placeable, "veldite_vein")
                .WithFrequency(50)

                .AddSpawn(ObjectType.Placeable, "tree")
                .WithFrequency(5);
        }

        private void WildlandsResources()
        {
            _builder.Create("RESOURCES_VISCARA_WILDLANDS")
                .AddSpawn(ObjectType.Placeable, "veldite_vein")
                .WithFrequency(20)

                .AddSpawn(ObjectType.Placeable, "tree")
                .WithFrequency(50);
        }

        private void WildwoodsResources()
        {
            _builder.Create("RESOURCES_VISCARA_WILDWOODS")
                .AddSpawn(ObjectType.Placeable, "veldite_vein")
                .WithFrequency(10)

                .AddSpawn(ObjectType.Placeable, "tree")
                .WithFrequency(70);
        }

        private void SwamplandResources()
        {
            _builder.Create("RESOURCES_VISCARA_SWAMPLANDS")
                .AddSpawn(ObjectType.Placeable, "veldite_vein")
                .WithFrequency(20)

                .AddSpawn(ObjectType.Placeable, "tree")
                .WithFrequency(50)

                .AddSpawn(ObjectType.Placeable, "oak_tree")
                .WithFrequency(20);
        }

        private void VelesSewersResources()
        {
            _builder.Create("RESOURCES_VISCARA_VELESSEWERS")
                .AddSpawn(ObjectType.Placeable, "cz220_junk");
        }
    }
}
