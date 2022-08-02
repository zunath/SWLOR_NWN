using System.Collections.Generic;
using SWLOR.Game.Server.Service.LootService;

namespace SWLOR.Game.Server.Feature.LootTableDefinition
{
    public class CZ220LootTableDefinition: ILootTableDefinition
    {
        private readonly LootTableBuilder _builder = new LootTableBuilder();

        public Dictionary<string, LootTable> BuildLootTables()
        {
            Credits();
            JunkPile();
            SuppliesCache();
            Mynock();
            Droid();
            DroidRares();
            ColicoidExperiment();
            ColicoidExperimentRares();

            return _builder.Build();
        }

        private void Credits()
        {
            _builder.Create("CZ220_CREDITS")
                .AddGold(10, 10);
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
                .AddItem("elec_ruined", 5)
                .AddItem("lth_ruined", 5)
                .AddItem("fiberp_ruined", 50)
                .AddItem("wood", 50)
                .AddItem("v_pebble", 10)
                .AddGold(10, 15);
        }

        private void Mynock()
        {
            _builder.Create("CZ220_LOOT_MYNOCK")
                .AddItem("mynock_meat", 50)
                .AddItem("mynock_tooth", 20)
                .AddItem("lth_ruined", 5);

            _builder.Create("CZ220_LOOT_MYNOCK_WINGS")
                .AddItem("mynock_wing", 10);
        }

        private void Droid()
        {
            _builder.Create("CZ220_LOOT_DROID")
                .AddItem("elec_ruined", 50)
                .AddItem("scrap_metal", 10)
                .AddGold(10, 20);
        }

        private void DroidRares()
        {
            _builder.Create("CZ220_LOOT_DROID_RARES")
                .IsRare()
                .AddItem("map_22", 50, 1, true);
        }

        private void ColicoidExperiment()
        {
            _builder.Create("CZ220_LOOT_COLICOID")
                .AddItem("colicoid_cap_b", 1, 20)
                .AddItem("colicoid_cap_g", 1, 20)
                .AddItem("colicoid_cap_y", 1, 3)
                .AddItem("colicoid_cap_r", 1, 20)

                .AddItem("colicoid_leg_a", 1, 20)
                .AddItem("colicoid_leg_c", 1, 20)
                .AddItem("colicoid_leg_w", 1, 20)
                .AddItem("colicoid_leg_e", 1, 20)
                .AddItem("colicoid_leg_f", 1, 20)
                ;
        }

        private void ColicoidExperimentRares()
        {
            _builder.Create("CZ220_LOOT_COLICOID_RARES")
                .IsRare()
                .AddItem("bag_dirty", 1, 1, true)
                .AddItem("map_22", 3, 1, true);
        }

    }
}
