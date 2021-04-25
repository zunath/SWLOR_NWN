using System.Collections.Generic;
using SWLOR.Game.Server.Service.LootService;

namespace SWLOR.Game.Server.Feature.LootTableDefinition
{
    public class CZ220LootTableDefinition: ILootTableDefinition
    {
        private readonly LootTableBuilder _builder = new LootTableBuilder();

        public Dictionary<string, LootTable> BuildLootTables()
        {
            JunkPile();
            SuppliesCache();
            Mynock();
            Droid();
            ColicoidExperiment();

            return _builder.Build();
        }

        private void JunkPile()
        {
            _builder.Create("CZ220_LOOT_JUNK_PILES")
                .AddItem("scrap_metal", 50)
                .AddGold(5, 5);
        }

        private void SuppliesCache()
        {
            _builder.Create("CZ220_LOOT_SUPPLIES_CACHE")
                .AddItem("scrap_metal", 10)
                .AddItem("fiberp_destroyed", 50)
                .AddItem("wood", 50)
                .AddGold(10, 15);
        }

        private void Mynock()
        {
            _builder.Create("CZ220_LOOT_MYNOCK")
                .AddItem("mynock_meat", 50)
                .AddItem("mynock_tooth", 20)
                .AddItem("mynock_wing", 30);
        }

        private void Droid()
        {
            _builder.Create("CZ220_LOOT_DROID")
                .AddItem("elec_destroyed", 50)
                .AddItem("scrap_metal", 10)
                .AddGold(10, 20);
        }

        private void ColicoidExperiment()
        {
            _builder.Create("CZ220_LOOT_COLICOID")
                .AddItem("bag_dirty", 1, 1, true)
                .AddItem("a_imp_end1", 15)
                .AddItem("b_imp_end1", 15)
                .AddItem("h_imp_end1", 15)
                .AddItem("l_imp_end1", 15)
                .AddGold(40, 20);
        }


    }
}
