using System.Collections.Generic;
using SWLOR.Game.Server.Service.LootService;

namespace SWLOR.Game.Server.Feature.LootTableDefinition
{
    public class EshanLootTableDefinition : ILootTableDefinition
    {
        private readonly LootTableBuilder _builder = new();

        public Dictionary<string, LootTable> BuildLootTables()
        {
            _builder.Create("ESHAN_NEOCRUSADER")
                .AddItem("esh_mando_salv", 10, 2)
                .AddGold(150, 5);

            _builder.Create("ESHAN_DIRE_WOLF")
                .AddItem("esh_wolf_pelt", 10, 2);

            _builder.Create("ESHAN_FROST_WOLF")
                .AddItem("esh_wolf_pelt", 10, 2)
                .AddItem("esh_frost_fang", 4, 1);

            _builder.Create("ESHAN_DIRE_WOLF_ALPHA")
                .AddItem("esh_wolf_pelt", 10, 3)
                .AddItem("esh_frost_fang", 10, 2);

            _builder.Create("ESHAN_SUN_GUARD")
                .AddItem("esh_sun_insignia", 10, 1)
                .AddItem("esh_mando_salv", 5, 2)
                .AddGold(500, 10);

            _builder.Create("ESHAN_SCRAPYARD_SMUGGLER")
                .AddItem("elec_flawed", 5, 1)
                .AddGold(250, 10);

            return _builder.Build();
        }
    }
}
