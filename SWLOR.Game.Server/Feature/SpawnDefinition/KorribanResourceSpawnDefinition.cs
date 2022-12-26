using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.SpawnService;

namespace SWLOR.Game.Server.Feature.SpawnDefinition
{
    public class KorribanResourceSpawnDefinition : ISpawnListDefinition
    {
        private readonly SpawnTableBuilder _builder = new();

        public Dictionary<string, SpawnTable> BuildSpawnTables()
        {
            WastelandsResources();
            ValleyResources();
            RavineResources();
            CavernsResources();
            DunesResources();
            SithCryptResources();
            SithFortressResources();

            return _builder.Build();
        }

        private void WastelandsResources()
        {
            _builder.Create("KORRIBAN_WASTELANDS_RESOURCES", "Wastelands")
                .AddSpawn(ObjectType.Placeable, "scordspar_vein")
                .WithFrequency(50)

                .AddSpawn(ObjectType.Placeable, "veldite_vein")
                .WithFrequency(20)

                .AddSpawn(ObjectType.Placeable, "desert_shrub")
                .WithFrequency(5);

        }

        private void ValleyResources()
        {
            _builder.Create("KORRIBAN_VALLEY_RESOURCES", "Valley")
                .AddSpawn(ObjectType.Placeable, "scordspar_vein")
                .WithFrequency(50)

                .AddSpawn(ObjectType.Placeable, "veldite_vein")
                .WithFrequency(20)

                .AddSpawn(ObjectType.Placeable, "desert_shrub")
                .WithFrequency(5);
        }

        private void RavineResources()
        {
            _builder.Create("KORRIBAN_RAVINE_RESOURCES", "Ravine")
                .AddSpawn(ObjectType.Placeable, "scordspar_vein")
                .WithFrequency(50)

                .AddSpawn(ObjectType.Placeable, "veldite_vein")
                .WithFrequency(20)

                .AddSpawn(ObjectType.Placeable, "desert_shrub")
                .WithFrequency(5);
        }

        private void CavernsResources()
        {
            _builder.Create("KORRIBAN_CAVERNS_RESOURCES", "Caverns")
                .AddSpawn(ObjectType.Placeable, "scordspar_vein")
                .WithFrequency(30)

                .AddSpawn(ObjectType.Placeable, "veldite_vein")
                .WithFrequency(15)

                .AddSpawn(ObjectType.Placeable, "herbs_patch_2")
                .WithFrequency(10)

                .AddSpawn(ObjectType.Placeable, "patch_veggies2")
                .WithFrequency(10)

                .AddSpawn(ObjectType.Placeable, "plagionite_vein")
                .WithFrequency(2);
        }

        private void DunesResources()
        {
            _builder.Create("KORRIBAN_DUNES_RESOURCES", "Dunes")
                .AddSpawn(ObjectType.Placeable, "scordspar_vein")
                .WithFrequency(50)

                .AddSpawn(ObjectType.Placeable, "veldite_vein")
                .WithFrequency(20)

                .AddSpawn(ObjectType.Placeable, "desert_shrub")
                .WithFrequency(5)

                .AddSpawn(ObjectType.Placeable, "plagionite_vein")
                .WithFrequency(2);
        }

        private void SithCryptResources()
        {
            _builder.Create("KORRIBAN_SITH_CRYPT_RESOURCES", "Sith Crypt")
                .AddSpawn(ObjectType.Placeable, "sithcrypt_box_1")
                .WithFrequency(50)

                .AddSpawn(ObjectType.Placeable, "sithcrypt_box_2")
                .WithFrequency(10)

                .AddSpawn(ObjectType.Placeable, "sithcrypt_box_3")
                .WithFrequency(2);
        }

        private void SithFortressResources()
        {
            _builder.Create("KorribanDungeonLootTemple")
                .AddSpawn(ObjectType.Placeable, "korrduntemple")
                .WithFrequency(1);

            _builder.Create("KorribanDungeonLootForge")
                .AddSpawn(ObjectType.Placeable, "korrdunforge")
                .WithFrequency(1);
        }
    }
}
