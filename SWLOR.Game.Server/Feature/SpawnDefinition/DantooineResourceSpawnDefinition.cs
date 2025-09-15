using System.Collections.Generic;
using SWLOR.Game.Server.Service.SpawnService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.SpawnDefinition
{
    public class DantooineResourceSpawnDefinition : ISpawnListDefinition
    {
        private readonly SpawnTableBuilder _builder = new();

        public Dictionary<string, SpawnTable> BuildSpawnTables()
        {
            DantooineLake();
            DanPiles();
            DanHay();
            DanHerbs();
            DantooineWildPlains();
            DantooineWareHouse();
            DanTribeVillage();
            DantooineCrystalFields();
            DantooineJantaCaves();
            DantooineForsakenJungles();
            DantooineDestroy();

            return _builder.Build();
        }

        private void DantooineLake()
        {
            _builder.Create("DANTOOINE_LAKE_RESOURCES")
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
        private void DanPiles()
        {
            _builder.Create("DANTOOINE_JUNKPILES")
                .AddSpawn(ObjectType.Placeable, "dan_junk")
                .WithFrequency(50);
        }
        private void DanHay()
        {
            _builder.Create("DANTOOINE_HAY")
                .AddSpawn(ObjectType.Placeable, "dan_hay")
                .WithFrequency(50);
        }
        private void DanHerbs()
        {
            _builder.Create("DANTOOINE_HERB")
                .AddSpawn(ObjectType.Placeable, "dant_starwort")
                .WithFrequency(50);
        }
        private void DantooineWildPlains()
        {
            _builder.Create("DANTOOINE_WILD_PLAINS_RESOURCES")
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

        private void DantooineCrystalFields()
        {
            _builder.Create("DANTOOINE_CRYSTAL_FIELDS_RESOURCES")
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

        private void DantooineDestroy()
        {
            _builder.Create("DANTOOINE_RUIN_FARM_RESOURCES")
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

        private void DantooineJantaCaves()
        {
            _builder.Create("DANTOOINE_JANTA_CAVES_RESOURCES")
                .AddSpawn(ObjectType.Placeable, "keromber_vein")
                .WithFrequency(30)

                .AddSpawn(ObjectType.Placeable, "jasioclase_vein")
                .WithFrequency(50)

                .AddSpawn(ObjectType.Placeable, "arkoxit_vein")
                .WithFrequency(1)

                .AddSpawn(ObjectType.Placeable, "patch_veggies4")
                .WithFrequency(5)
                .AddSpawn(ObjectType.Placeable, "patch_veggies5")
                .WithFrequency(5);
        }

        private void DantooineForsakenJungles()
        {
            _builder.Create("DANTOOINE_FORSAKEN_JUNGLES_RESOURCES")

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

        private void DantooineWareHouse()
        {
            _builder.Create("DANTOOINE_WARE_HOUSE_RESOURCES")

                .AddSpawn(ObjectType.Placeable, "jasioclase_vein")
                .WithFrequency(20)
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

        private void DanTribeVillage()
        {
            _builder.Create("DANTOOINE_TRIBE_VILLAGE_RESOURCES")
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
                .WithFrequency(10)

                .AddSpawn(ObjectType.Placeable, "hyphae_tree")
                .WithFrequency(5);
        }

    }
}
