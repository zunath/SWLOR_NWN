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

            return _builder.Build();
        }

        private void OreVeins()
        {
            _builder.Create("HARVESTING_VELDITE")
                .AddItem("raw_veldite", 50);
            _builder.Create("HARVESTING_SCORDSPAR")
                .AddItem("raw_scordspar", 50);
            _builder.Create("HARVESTING_PLAGIONITE")
                .AddItem("raw_plagionite", 50);
            _builder.Create("HARVESTING_KEROMBER")
                .AddItem("raw_keromber", 50);
            _builder.Create("HARVESTING_JASIOCLASE")
                .AddItem("raw_jasioclase", 50);
        }

        private void Trees()
        {
            _builder.Create("HARVESTING_TREE")
                .AddItem("wood", 50);
            _builder.Create("HARVESTING_OAK_TREE")
                .AddItem("fine_wood", 50);
            _builder.Create("HARVESTING_ANCIENT_TREE")
                .AddItem("ancient_wood", 50);
            _builder.Create("HARVESTING_ARACIA_TREE")
                .AddItem("aracia_wood", 50);
            _builder.Create("HARVESTING_HYPHAE_TREE")
                .AddItem("hyphae_wood", 50);
        }
    }
}