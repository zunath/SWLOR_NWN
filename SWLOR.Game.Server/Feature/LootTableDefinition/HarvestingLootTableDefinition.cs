﻿using System.Collections.Generic;
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
    }
}