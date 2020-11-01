using System.Collections.Generic;
using SWLOR.Game.Server.Service.LootService;

namespace SWLOR.Game.Server.Feature.LootTableDefinition
{
    public class TestLootTableDefinition: ILootTableDefinition
    {
        public Dictionary<string, LootTable> BuildLootTables()
        {
            var builder = new LootTableBuilder()
                .Create("myLoot")
                .AddItem("nw_waxgr001", 50)

                .Create("steal_test")
                .AddItem("nw_it_mpotion001", 50);



            return builder.Build();
        }
    }
}
