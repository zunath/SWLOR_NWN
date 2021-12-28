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

            return _builder.Build();
        }

        private void Cavern()
        {
            _builder.Create("RESOURCES_VISCARA_CAVERN")
                .AddSpawn(ObjectType.Placeable, "veldite_vein");
        }

        private void CoxxionBase()
        {
            _builder.Create("COXXION_BASE_SCAVENGE")
                .AddSpawn(ObjectType.Placeable, "v_cox_scav");
        }

        private void DeepMountainsResources()
        {
            _builder.Create("RESOURCES_VISCARA_DEEPMOUNTAINS")
                .AddSpawn(ObjectType.Placeable, "veldite_vein");
        }

        private void MandalorianFacilityResources()
        {
            _builder.Create("RESOURCES_VISCARA_MANDALORIANFACILITY")
                .AddSpawn(ObjectType.Placeable, "veldite_vein");
        }

        private void MountainValleyResources()
        {
            _builder.Create("RESOURCES_VISCARA_MOUNTAINVALLEY")
                .AddSpawn(ObjectType.Placeable, "veldite_vein");
        }

        private void WildlandsResources()
        {
            _builder.Create("RESOURCES_VISCARA_WILDLANDS")
                .AddSpawn(ObjectType.Placeable, "veldite_vein");
        }

        private void WildwoodsResources()
        {
            _builder.Create("RESOURCES_VISCARA_WILDWOODS")
                .AddSpawn(ObjectType.Placeable, "veldite_vein");
        }

        private void SwamplandResources()
        {
            _builder.Create("RESOURCES_VISCARA_SWAMPLANDS")
                .AddSpawn(ObjectType.Placeable, "veldite_vein");
        }
    }
}
