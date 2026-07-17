using System.Collections.Generic;
using SWLOR.Game.Server.Service.LootService;

namespace SWLOR.Game.Server.Feature.LootTableDefinition
{
    /// <summary>
    /// Reward tables rolled when a player successfully cracks an Espionage lockbox (see
    /// LockboxItemDefinition). Each tier leans heavily on mundane crafting materials with a handful of
    /// low-weight valuable entries, mirroring the weighting convention used by the planetary "_RARES"
    /// tables (e.g. CZ220LootTableDefinition, DathomirLootTableDefinition).
    /// </summary>
    public class LockboxLootTableDefinition: ILootTableDefinition
    {
        private readonly LootTableBuilder _builder = new();

        public Dictionary<string, LootTable> BuildLootTables()
        {
            Tier1();
            Tier2();
            Tier3();
            Tier4();
            Tier5();

            return _builder.Build();
        }

        private void Tier1()
        {
            _builder.Create("ESPIONAGE_LOCKBOX_1")
                .AddItem("espn_ring_1", 3, 1, true)
                .AddItem("espn_neck_1", 3, 1, true)
                .AddItem("espn_belt_1", 3, 1, true)
                .AddItem("scrap_metal", 30)
                .AddItem("elec_ruined", 20)
                .AddItem("lth_ruined", 20)
                .AddItem("fiberp_ruined", 20)
                .AddItem("wood", 15)
                .AddItem("v_pebble", 10)
                .AddGold(15, 10)
                .AddItem("ruby", 2, 1, true);
        }

        private void Tier2()
        {
            _builder.Create("ESPIONAGE_LOCKBOX_2")
                .AddItem("espn_ring_2", 3, 1, true)
                .AddItem("espn_neck_2", 3, 1, true)
                .AddItem("espn_belt_2", 3, 1, true)
                .AddItem("elec_flawed", 25)
                .AddItem("lth_flawed", 25)
                .AddItem("fiberp_flawed", 20)
                .AddItem("scrap_metal", 15)
                .AddGold(30, 15)
                .AddItem("emerald", 3, 1, true)
                .AddItem("ruby", 2, 1, true)
                .AddItem("map_22", 2, 1, true);
        }

        private void Tier3()
        {
            _builder.Create("ESPIONAGE_LOCKBOX_3")
                .AddItem("espn_ring_3", 3, 1, true)
                .AddItem("espn_neck_3", 3, 1, true)
                .AddItem("espn_belt_3", 3, 1, true)
                .AddItem("elec_high", 20)
                .AddItem("lth_high", 20)
                .AddItem("fiberp_high", 20)
                .AddItem("ref_jasioclase", 15)
                .AddGold(60, 15)
                .AddItem("emerald", 4, 1, true)
                .AddItem("ruby", 3, 1, true)
                .AddItem("jade", 2, 1, true);
        }

        private void Tier4()
        {
            _builder.Create("ESPIONAGE_LOCKBOX_4")
                .AddItem("espn_ring_4", 3, 1, true)
                .AddItem("espn_neck_4", 3, 1, true)
                .AddItem("espn_belt_4", 3, 1, true)
                .AddItem("ref_gostian", 20)
                .AddItem("elec_high", 15)
                .AddItem("lth_high", 15)
                .AddGold(120, 15)
                .AddItem("jade", 5, 1, true)
                .AddItem("agate", 5, 1, true)
                .AddItem("ruby", 3, 1, true)
                .AddItem("chiro_shard", 2, 1, true);
        }

        private void Tier5()
        {
            _builder.Create("ESPIONAGE_LOCKBOX_5")
                .AddItem("espn_ring_5", 3, 1, true)
                .AddItem("espn_neck_5", 3, 1, true)
                .AddItem("espn_belt_5", 3, 1, true)
                .AddItem("ref_arkoxit", 15)
                .AddItem("fine_wood", 10)
                .AddGold(250, 15)
                .AddItem("jade", 8, 1, true)
                .AddItem("agate", 8, 1, true)
                .AddItem("ruby", 5, 1, true)
                .AddItem("chiro_shard", 4, 1, true)
                .AddItem("map_56", 2, 1, true);
        }
    }
}
