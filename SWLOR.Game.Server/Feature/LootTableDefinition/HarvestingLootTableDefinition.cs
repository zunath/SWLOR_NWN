using System.Collections.Generic;
using SWLOR.Game.Server.Service.LootService;

namespace SWLOR.Game.Server.Feature.LootTableDefinition
{
    public class HarvestingLootTableDefinition : ILootTableDefinition
    {
        private readonly LootTableBuilder _builder = new LootTableBuilder();

        public Dictionary<string, LootTable> BuildLootTables()
        {
            OreVeins();
            Trees();
            Bushes();
            Herbs();
            VegetablePatches();
            AsteroidMining();

            return _builder.Build();
        }

        private void OreVeins()
        {
            _builder.Create("HARVESTING_VELDITE")
                .AddItem("raw_veldite", 50)
                .AddItem("jade", 5);
            _builder.Create("HARVESTING_SCORDSPAR")
                .AddItem("raw_scordspar", 50)
                .AddItem("agate", 4);
            _builder.Create("HARVESTING_PLAGIONITE")
                .AddItem("raw_plagionite", 50)
                .AddItem("citrine", 3);
            _builder.Create("HARVESTING_KEROMBER")
                .AddItem("raw_keromber", 50)
                .AddItem("ruby", 2);
            _builder.Create("HARVESTING_JASIOCLASE")
                .AddItem("raw_jasioclase", 50)
                .AddItem("emerald", 1);
            _builder.Create("HARVESTING_ARKOXIT")
                .AddItem("raw_arkoxit", 50);
        }

        private void Trees()
        {
            _builder.Create("HARVESTING_TREE")
                .AddItem("wood", 50)
                .AddItem("fine_wood", 5)
                .AddItem("jade", 5);
            _builder.Create("HARVESTING_OAK_TREE")
                .AddItem("fine_wood", 50)
                .AddItem("wood", 10)
                .AddItem("agate", 4);
            _builder.Create("HARVESTING_ANCIENT_TREE")
                .AddItem("ancient_wood", 50)
                .AddItem("fine_wood", 20)
                .AddItem("wood", 2)
                .AddItem("citrine", 3);
            _builder.Create("HARVESTING_ARACIA_TREE")
                .AddItem("aracia_wood", 50)
                .AddItem("ancient_wood", 20)
                .AddItem("fine_wood", 2)
                .AddItem("ruby", 2);
            _builder.Create("HARVESTING_HYPHAE_TREE")
                .AddItem("hyphae_wood", 50)
                .AddItem("aracia_wood", 20)
                .AddItem("ancient_wood", 2)
                .AddItem("emerald", 1);
        }

        private void Bushes()
        {
            _builder.Create("SCAVENGING_BUSH_1")
                .AddItem("fiberp_ruined", 50)
                .AddItem("fiberp_flawed", 5);
            _builder.Create("SCAVENGING_BUSH_2")
                .AddItem("fiberp_flawed", 50)
                .AddItem("fiberp_ruined", 10);
            _builder.Create("SCAVENGING_BUSH_3")
                .AddItem("fiberp_good", 50)
                .AddItem("fiberp_flawed", 20)
                .AddItem("fiberp_ruined", 5);
            _builder.Create("SCAVENGING_BUSH_4")
                .AddItem("fiberp_imperfect", 50)
                .AddItem("fiberp_good", 20)
                .AddItem("fiberp_flawed", 5);
            _builder.Create("SCAVENGING_BUSH_5")
                .AddItem("fiberp_high", 50)
                .AddItem("fiberp_imperfect", 20)
                .AddItem("fiberp_good", 5);
        }
        private void Herbs()
        {
            _builder.Create("HERBS_1")
                .AddItem("herb_v", 50);
            _builder.Create("HERBS_2")
                .AddItem("herb_m", 50);
            _builder.Create("HERBS_3")
                .AddItem("herb_c", 50);
            _builder.Create("HERBS_4")
                .AddItem("herb_t", 50);
            _builder.Create("HERBS_5")
                .AddItem("herb_x", 50);
        }

        private void VegetablePatches()
        {
            _builder.Create("VEGETABLES_PATCH_1")
                .AddItem("v_orange", 20)
                .AddItem("v_lemon", 20)
                .AddItem("v_pebble", 20)
                .AddItem("herb_v", 5);

            _builder.Create("VEGETABLES_PATCH_2")
                .AddItem("v_apple", 20)
                .AddItem("v_peas", 20)
                .AddItem("passion_fruit", 20)
                .AddItem("herb_m", 5);

            _builder.Create("VEGETABLES_PATCH_3")
                .AddItem("herb_c", 5)
                .AddItem("h_acorn", 20)
                .AddItem("s_pineapple", 20)
                .AddItem("veggie_clump", 20);

            _builder.Create("VEGETABLES_PATCH_4")
                .AddItem("ginger", 10)
                .AddItem("melon", 10)
                .AddItem("mushroom", 10)
                .AddItem("plant_butter", 10)
                .AddItem("walnut", 10)
                .AddItem("cornucopia", 5)
                .AddItem("herb_t", 5);

            _builder.Create("VEGETABLES_PATCH_5")
                .AddItem("herb_x", 5)
                .AddItem("turnip", 20)
                .AddItem("tofu", 20)
                .AddItem("dried_bonito", 10)
                .AddItem("tomato", 10);
        }

        private void AsteroidMining()
        {
            _builder.Create("ASTEROID_TILARIUM")
                .AddItem("ore_tilarium", 30)
                .AddItem("aluminum", 2)
                .AddItem("quadrenium", 1);
            
            _builder.Create("ASTEROID_CURRIAN")
                .AddItem("ore_currian", 30)
                .AddItem("steel", 2)
                .AddItem("vintrium", 1);

            _builder.Create("ASTEROID_IDAILIA")
                .AddItem("ore_idailia", 30)
                .AddItem("obsidian", 2)
                .AddItem("ionite", 1);

            _builder.Create("ASTEROID_BARINIUM")
                .AddItem("ore_barinium", 30)
                .AddItem("crystal", 2)
                .AddItem("katrium", 1);

            _builder.Create("ASTEROID_GOSTIAN")
                .AddItem("ore_gostian", 30)
                .AddItem("diamond", 2)
                .AddItem("zinsiam", 1);
        }
    }
}