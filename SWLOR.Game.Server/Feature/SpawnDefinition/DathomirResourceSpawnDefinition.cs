using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.SpawnService;

namespace SWLOR.Game.Server.Feature.SpawnDefinition
{
    public class DathomirResourceSpawnDefinition : ISpawnListDefinition
    {
        private readonly SpawnTableBuilder _builder = new();

        public Dictionary<string, SpawnTable> BuildSpawnTables()
        {
            Desert();
            DesertWestSide();
            GrottoCaverns();
            Grottos();
            Mountains();
            TarnishedJungles();
            TarnishedJunglesNorth();
            TribeVillage();

            return _builder.Build();
        }
        
        private void Desert()
        {
            _builder.Create("DATHOMIR_DESERT_RESOURCES")
                .AddSpawn(ObjectType.Placeable, "keromber_vein")
                .WithFrequency(40)

                .AddSpawn(ObjectType.Placeable, "jasioclase_vein")
                .WithFrequency(20)

                .AddSpawn(ObjectType.Placeable, "arkoxit_vein")
                .WithFrequency(1)

                .AddSpawn(ObjectType.Placeable, "herbs_patch_4")
                .WithFrequency(10)
                .AddSpawn(ObjectType.Placeable, "herbs_patch_5")
                .WithFrequency(30)

                .AddSpawn(ObjectType.Placeable, "patch_veggies4")
                .WithFrequency(5)
                .AddSpawn(ObjectType.Placeable, "patch_veggies5")
                .WithFrequency(10)

                .AddSpawn(ObjectType.Placeable, "fiberp_bush_4")
                .WithFrequency(5)

                .AddSpawn(ObjectType.Placeable, "fiberp_bush_5")
                .WithFrequency(10);
        }

        private void DesertWestSide()
        {
            _builder.Create("DATHOMIR_DESERT_WEST_SIDE_RESOURCES")
                .AddSpawn(ObjectType.Placeable, "keromber_vein")
                .WithFrequency(40)

                .AddSpawn(ObjectType.Placeable, "jasioclase_vein")
                .WithFrequency(30)

                .AddSpawn(ObjectType.Placeable, "arkoxit_vein")
                .WithFrequency(1)

                .AddSpawn(ObjectType.Placeable, "herbs_patch_4")
                .WithFrequency(10)
                .AddSpawn(ObjectType.Placeable, "herbs_patch_5")
                .WithFrequency(30)

                .AddSpawn(ObjectType.Placeable, "patch_veggies4")
                .WithFrequency(5)
                .AddSpawn(ObjectType.Placeable, "patch_veggies5")
                .WithFrequency(15)

                .AddSpawn(ObjectType.Placeable, "fiberp_bush_4")
                .WithFrequency(5)

                .AddSpawn(ObjectType.Placeable, "fiberp_bush_5")
                .WithFrequency(15);
        }

        private void GrottoCaverns()
        {
            _builder.Create("DATHOMIR_GROTTO_CAVERNS_RESOURCES")
                .AddSpawn(ObjectType.Placeable, "keromber_vein")
                .WithFrequency(20)

                .AddSpawn(ObjectType.Placeable, "jasioclase_vein")
                .WithFrequency(40)

                .AddSpawn(ObjectType.Placeable, "arkoxit_vein")
                .WithFrequency(1)

                .AddSpawn(ObjectType.Placeable, "herbs_patch_4")
                .WithFrequency(10)
                .AddSpawn(ObjectType.Placeable, "herbs_patch_5")
                .WithFrequency(30)

                .AddSpawn(ObjectType.Placeable, "patch_veggies4")
                .WithFrequency(5)
                .AddSpawn(ObjectType.Placeable, "patch_veggies5")
                .WithFrequency(15)

                .AddSpawn(ObjectType.Placeable, "fiberp_bush_4")
                .WithFrequency(5)

                .AddSpawn(ObjectType.Placeable, "fiberp_bush_5")
                .WithFrequency(15);
        }

        private void Grottos()
        {
            _builder.Create("DATHOMIR_GROTTOS_RESOURCES")
                .AddSpawn(ObjectType.Placeable, "keromber_vein")
                .WithFrequency(20)

                .AddSpawn(ObjectType.Placeable, "jasioclase_vein")
                .WithFrequency(50)

                .AddSpawn(ObjectType.Placeable, "arkoxit_vein")
                .WithFrequency(1)

                .AddSpawn(ObjectType.Placeable, "herbs_patch_4")
                .WithFrequency(10)
                .AddSpawn(ObjectType.Placeable, "herbs_patch_5")
                .WithFrequency(30)

                .AddSpawn(ObjectType.Placeable, "patch_veggies4")
                .WithFrequency(5)
                .AddSpawn(ObjectType.Placeable, "patch_veggies5")
                .WithFrequency(15)

                .AddSpawn(ObjectType.Placeable, "fiberp_bush_4")
                .WithFrequency(5)

                .AddSpawn(ObjectType.Placeable, "fiberp_bush_5")
                .WithFrequency(15)
                
                .AddSpawn(ObjectType.Placeable, "hyphae_tree")
                .WithFrequency(5);
        }

        private void Mountains()
        {
            _builder.Create("DATHOMIR_MOUNTAINS_RESOURCES")
                .AddSpawn(ObjectType.Placeable, "keromber_vein")
                .WithFrequency(30)

                .AddSpawn(ObjectType.Placeable, "jasioclase_vein")
                .WithFrequency(50)

                .AddSpawn(ObjectType.Placeable, "arkoxit_vein")
                .WithFrequency(1)

                .AddSpawn(ObjectType.Placeable, "herbs_patch_4")
                .WithFrequency(10)
                .AddSpawn(ObjectType.Placeable, "herbs_patch_5")
                .WithFrequency(10)

                .AddSpawn(ObjectType.Placeable, "patch_veggies4")
                .WithFrequency(5)
                .AddSpawn(ObjectType.Placeable, "patch_veggies5")
                .WithFrequency(5)

                .AddSpawn(ObjectType.Placeable, "fiberp_bush_4")
                .WithFrequency(5)

                .AddSpawn(ObjectType.Placeable, "fiberp_bush_5")
                .WithFrequency(5);
        }

        private void TarnishedJungles()
        {
            _builder.Create("DATHOMIR_TARNISHED_JUNGLES_RESOURCES")

                .AddSpawn(ObjectType.Placeable, "herbs_patch_4")
                .WithFrequency(10)
                .AddSpawn(ObjectType.Placeable, "herbs_patch_5")
                .WithFrequency(30)

                .AddSpawn(ObjectType.Placeable, "patch_veggies4")
                .WithFrequency(5)
                .AddSpawn(ObjectType.Placeable, "patch_veggies5")
                .WithFrequency(15)

                .AddSpawn(ObjectType.Placeable, "fiberp_bush_4")
                .WithFrequency(5)

                .AddSpawn(ObjectType.Placeable, "fiberp_bush_5")
                .WithFrequency(15)

                .AddSpawn(ObjectType.Placeable, "hyphae_tree")
                .WithFrequency(5);
        }

        private void TarnishedJunglesNorth()
        {
            _builder.Create("DATHOMIR_TARNISHED_JUNGLES_NORTH_RESOURCES")

                .AddSpawn(ObjectType.Placeable, "herbs_patch_4")
                .WithFrequency(10)
                .AddSpawn(ObjectType.Placeable, "herbs_patch_5")
                .WithFrequency(30)

                .AddSpawn(ObjectType.Placeable, "patch_veggies4")
                .WithFrequency(5)
                .AddSpawn(ObjectType.Placeable, "patch_veggies5")
                .WithFrequency(15)

                .AddSpawn(ObjectType.Placeable, "fiberp_bush_4")
                .WithFrequency(5)

                .AddSpawn(ObjectType.Placeable, "fiberp_bush_5")
                .WithFrequency(15)

                .AddSpawn(ObjectType.Placeable, "hyphae_tree")
                .WithFrequency(5);
        }

        private void TribeVillage()
        {
            _builder.Create("DATHOMIR_TRIBE_VILLAGE_RESOURCES")
                .AddSpawn(ObjectType.Placeable, "keromber_vein")
                .WithFrequency(40)

                .AddSpawn(ObjectType.Placeable, "jasioclase_vein")
                .WithFrequency(20)

                .AddSpawn(ObjectType.Placeable, "arkoxit_vein")
                .WithFrequency(1)

                .AddSpawn(ObjectType.Placeable, "herbs_patch_4")
                .WithFrequency(10)
                .AddSpawn(ObjectType.Placeable, "herbs_patch_5")
                .WithFrequency(30)

                .AddSpawn(ObjectType.Placeable, "patch_veggies4")
                .WithFrequency(5)
                .AddSpawn(ObjectType.Placeable, "patch_veggies5")
                .WithFrequency(10)

                .AddSpawn(ObjectType.Placeable, "fiberp_bush_4")
                .WithFrequency(5)

                .AddSpawn(ObjectType.Placeable, "fiberp_bush_5")
                .WithFrequency(10);
        }

    }
}
